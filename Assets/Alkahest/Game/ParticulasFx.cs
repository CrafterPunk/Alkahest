using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [ENCARGO F, Playtest 39 — CONTRATO_MOTOR.md §2] LA CAPA DE PARTÍCULAS
    /// DESPRENDIDAS: motas decorativas que saltan/flotan/se disipan cuando la
    /// grilla hace algo interesante cerca de la cámara (un líquido aterriza,
    /// el fuego chispea, el crisol respira motas de aire caliente, el polvo
    /// levanta una nubecita, el vapor exhala vaho).
    ///
    /// NATURALEZA: decorativas, NO-SIM. Este archivo NUNCA escribe en
    /// <see cref="CellGrid"/> ni participa del determinismo — solo LEE
    /// `mat`/`temp`/`aux`/`touchedTick` para decidir dónde nacer. Por eso
    /// puede usar <c>UnityEngine.Random</c> sin culpa (regla 7 de CLAUDE.md
    /// prohíbe Random en el HOT PATH DE LA SIM, no aquí) y en multiplayer es
    /// puramente CLIENT-LOCAL: cada cliente genera sus propias motas de lo
    /// que VE en su grilla replicada (host o espejo, da igual), así que esto
    /// no manda ni un byte por red — coherente con el veredicto de Cesar
    /// contra los "accidentes" invisibles (regla 35): aquí lo único que
    /// varía entre clientes es el AZAR VISUAL, nunca el estado del mundo.
    ///
    /// ARQUITECTURA (las tres piezas):
    ///  1) RING BUFFER preasignado de <see cref="Capacidad"/> structs
    ///     <see cref="Particula"/> (~4096, ver el contrato) — <see cref="Spawn"/>
    ///     escribe en un cursor circular y lo avanza; si el cursor da la
    ///     vuelta antes de que una partícula vieja muriera sola, esa
    ///     partícula se recicla a la fuerza (es un RING, no una cola FIFO
    ///     estricta) — con el presupuesto de nacimientos por frame (ver 3) y
    ///     vidas de 0.3-1.5s eso es un caso raro, no el camino normal.
    ///  2) OVERLAY: una <see cref="Texture2D"/> 768x288 (EXACTAMENTE
    ///     <see cref="CellGrid.W"/>x<see cref="CellGrid.H"/>, un téxel por
    ///     celda, igual que <see cref="SimRenderer"/>) sobre un
    ///     SpriteRenderer con la MISMA escala/pivote que el sprite del mundo
    ///     (ver <see cref="InicializarOverlay"/>: copia literalmente
    ///     `ppu = 1/CellWorldSize`, pivote (0,0), posición de mundo cero) y
    ///     `sortingOrder` = <see cref="SortingOrderOverlay"/> = -4: justo
    ///     ENCIMA del sprite de la sim (-5, ver <see cref="SimRenderer"/>)
    ///     y muy por debajo de cualquier maquinaria (15..21) o del aprendiz
    ///     (50) — mismo hueco que ya usa <see cref="Criatura"/> para su halo
    ///     "justo encima de la sim". Buffer <c>Color32[]</c> del tamaño del
    ///     mundo REUTILIZADO frame a frame; solo se tocan los téxeles sucios
    ///     (los que tenía una partícula el frame anterior, para borrarlos, y
    ///     los que tiene una partícula este frame, para pintarlos) — nunca
    ///     se recorre el mundo entero para limpiar. <c>Apply()</c> se llama
    ///     COMO MUCHO una vez por frame, y NUNCA si no hubo nada sucio (un
    ///     taller sin actividad no sube ni un byte a la GPU por este
    ///     archivo).
    ///  3) EMISIÓN POR OBSERVACIÓN: cada frame se sondea una FRANJA de filas
    ///     (acumulador, nunca el mundo entero) dentro de una ventana
    ///     alrededor de la cámara, mirando `touchedTick`/`mat`/`temp`. Ver
    ///     <see cref="SondearFranja"/> para el porqué de cada umbral.
    ///
    /// CERO ALLOCS POR FRAME: todos los arrays (partículas, los dos buffers
    /// de "téxeles sucios", el buffer de píxeles) se crean UNA vez en
    /// <see cref="Init"/> y se reutilizan para siempre; los dos buffers de
    /// sucio se intercambian por REFERENCIA (swap de dos campos), nunca se
    /// reasignan. Ni un <c>List&lt;T&gt;</c>, ni un <c>new Particula[]</c>,
    /// ni un <c>new Color32[]</c> en <see cref="Update"/>.
    ///
    /// SIN DEPENDENCIAS NUEVAS (mandato del contrato): este archivo NO llama
    /// a ninguna API que no exista hoy en `main`. En particular, NO usa el
    /// futuro ring de eventos no-destructivo que el Encargo S está
    /// construyendo en paralelo (`LeerEventosDesde` o como se acabe
    /// llamando) — eso es integración de Fable, ver la nota de deuda al
    /// final de este docblock. Toda la emisión de aquí sale de LEER el
    /// estado actual de la grilla, no de consumir eventos.
    /// </summary>
    public sealed class ParticulasFx : MonoBehaviour
    {
        // =================================================================
        // TIPOS DE PARTÍCULA (contrato §2, la lista de disparadores)
        // =================================================================
        private enum TipoParticula : byte
        {
            Salpicadura = 0, // líquido que acaba de aterrizar
            Chispa = 1,      // celdas Fire
            Mota = 2,        // aire caliente sobre material caliente (motas del crisol)
            Polvo = 3,       // polvo que acaba de aterrizar
            Vaho = 4,        // Steam recién nacido
        }

        private const int CantidadTipos = 5;

        private struct Particula
        {
            public float x, y;   // posición en COORDENADAS DE CELDA (float: sub-celda, no world-units)
            public float vx, vy; // velocidad en celdas/segundo
            public float vida;   // segundos restantes
            public float vidaMax;
            public Color32 color;
            public byte tipo;
        }

        /// <summary>Tamaño del ring buffer (contrato: "~4096"). Ver el docblock de la clase.</summary>
        private const int Capacidad = 4096;

        private Particula[] _particulas;
        private int _cursor; // próximo slot a escribir (ring circular)

        // -----------------------------------------------------------------
        // FÍSICA POR TIPO (gravedad + fricción, contrato: "física simple").
        // Tablas indexadas por (byte)TipoParticula -- evita un switch por
        // partícula en el hot path del Update. Todo en celdas/segundo(²):
        // la física vive en el mismo espacio que la posición de la
        // partícula, así que no hace falta convertir a unidades de mundo
        // hasta el momento de pintar el téxel.
        // -----------------------------------------------------------------
        // Gravedad: positiva tira hacia ABAJO (vy -= gravedad*dt); negativa
        // hace que el tipo ASCIENDA (motas de aire caliente, vaho) -- ambos
        // son físicamente "cosas menos densas que el aire de alrededor".
        private static readonly float[] GravedadPorTipo =
        {
            55f,  // Salpicadura: gotita de líquido, cae con ganas.
            30f,  // Chispa: sube al nacer (vy inicial positiva) y la gravedad la vence enseguida, como una brasa.
            -6f,  // Mota: aire caliente, asciende despacio.
            70f,  // Polvo: se posa rápido, poco vuelo.
            -7f,  // Vaho: vapor. Bajado de -10 a -7 en el playtest 41: con el gas REAL ya convectando (encargo S), un vaho que ascendía más rápido que la propia columna de Steam se despegaba de ella y se leía como otra cosa. Ahora acompaña, no adelanta.
        };

        // Fricción exponencial sobre (vx,vy), en 1/segundo -- 0 = sin
        // fricción, valores altos = frena casi de inmediato. Se aplica como
        // `factor = e^(-friccion*dt)` (mismo criterio independiente-del-
        // framerate que SimRenderer.CameraFollowSharpness: a cualquier
        // fps converge al mismo movimiento en el mismo tiempo real).
        private static readonly float[] FriccionPorTipo =
        {
            1.2f, // Salpicadura: casi balística, apenas frena.
            0.6f, // Chispa: vuela casi libre, es lo que la hace leerse "viva".
            2.5f, // Mota: se frena rápido, deriva perezosa.
            3.0f, // Polvo: se frena casi al instante, nubecita que no viaja.
            1.8f, // Vaho: voluta que se demora un poco más que el polvo.
        };

        // =================================================================
        // OVERLAY DE RENDER
        // =================================================================
        /// <summary>
        /// Justo ENCIMA del sprite de la sim (-5) y muy por debajo de la
        /// maquinaria (15..21) y del aprendiz (50) -- mismo hueco que usa
        /// Game/Criatura.cs para su halo (-4/-3, "justo encima de la sim").
        /// </summary>
        private const int SortingOrderOverlay = -4;

        private Texture2D _texture;
        private Color32[] _pixels; // W*H, reutilizado -- SOLO se tocan los téxeles sucios (ver Update).

        // Dos buffers de índices "sucios" (téxeles con una partícula
        // dibujada), intercambiados por REFERENCIA cada frame: el de "antes"
        // se usa para BORRAR al principio del frame, el de "ahora" se
        // rellena al pintar y pasa a ser el "antes" del frame siguiente.
        // Tamaño = Capacidad porque como mucho una partícula viva escribe UN
        // téxel por frame.
        private int[] _dirtyPrev;
        private int[] _dirtyCurr;
        private int _dirtyPrevCount;
        private int _dirtyCurrCount;

        private AlkahestSim _sim;
        private Universe _universe;
        private CellGrid _grid;
        private Camera _mainCam;

        // =================================================================
        // SONDEO POR OBSERVACIÓN (contrato §2: ventana alrededor de la
        // cámara, acumulador de franjas, presupuesto de nacimientos)
        // =================================================================
        /// <summary>Colchón en celdas alrededor del rectángulo visible de la cámara (igual de espíritu que SimRenderer.ViewMarginChunks, aquí en celdas porque no hay chunks que alinear).</summary>
        private const int VentanaMargenCeldas = 20;

        /// <summary>Filas que se sondean por frame (la "franja"): acotar esto es lo que evita recorrer el mundo entero cada frame.</summary>
        private const int FilasPorFrame = 6;

        /// <summary>
        /// Presupuesto de NACIMIENTOS por frame (contrato: "p.ej. máx 64").
        /// Cuenta partículas nacidas, no celdas sondeadas -- un diluvio con
        /// mil celdas "recién tocadas" seguirá naciendo como mucho esto por
        /// frame, así que nunca puede fundir el overlay.
        /// </summary>
        private const int BudgetNacimientosPorFrame = 64;

        /// <summary>
        /// VENTANA DE TICKS "RECIENTE" (DECISIÓN FUERA DE CONTRATO ESTRICTO,
        /// documentada aquí): <see cref="CellGrid.touchedTick"/> se pone al
        /// tick actual en CADA celda procesada de un chunk despierto (ver el
        /// docblock del campo en CellGrid.cs), no solo cuando la celda se
        /// MUEVE -- así que "== tick actual" filtra "está activa ahora
        /// mismo", no "acaba de aterrizar". Como este sondeo es DISPERSO (una
        /// franja de filas por frame, no el mundo entero), exigir el tick
        /// EXACTO haría que casi nunca coincidiéramos con la celda en el
        /// instante correcto. Se usa una ventana de los últimos N ticks en
        /// su lugar -- lo que sacrifica precisión de "instante exacto de
        /// aterrizaje" se compensa con la PROBABILIDAD baja por candidato en
        /// cada Try* de más abajo: una celda que lleva ~10 ticks activa
        /// (un charco asentándose) sale sondeada varias veces mientras dura
        /// esa ventana, pero solo dispara con una probabilidad pequeña cada
        /// vez, así que el efecto visual sigue siendo "un puñado de gotas al
        /// aterrizar", no un géiser continuo. En cuanto el chunk se duerme
        /// (30 ticks sin cambios, CellGrid.SleepTicks) touchedTick deja de
        /// avanzar y la ventana expira sola.
        /// </summary>
        private const uint VentanaTicksRecientes = 10;

        private int _filaSondeo; // fila absoluta (coordenada Y del mundo) que toca sondear a continuación.
        private int _nacidosEsteFrame;

        // -----------------------------------------------------------------
        // PROBABILIDADES POR CANDIDATO (ver el porqué en VentanaTicksRecientes):
        // cada celda candidata dispara con esta probabilidad POR SONDEO, no
        // una vez por evento -- son deliberadamente bajas para que un charco
        // asentándose durante ~10 ticks produzca un puñado de gotas, no una
        // fuente continua.
        // -----------------------------------------------------------------
        private const float ProbSalpicadura = 0.10f;
        private const float ProbChispa = 0.35f; // el fuego chispea más seguido: es lo que lo hace leerse "vivo".
        private const float ProbMota = 0.06f;
        private const float ProbPolvo = 0.08f;
        private const float ProbVaho = 0.10f;
        private const float ProbAscuaBrasa = 0.04f; // (integración pt39) la brasa respira despacio: más rara que cualquier otro emisor.

        /// <summary>Cuánto por encima de AmbientRaw (70 = 20°C, 1 raw = 2°C) cuenta como "aire caliente" para las motas del crisol. +14 raw = +28°C sobre ambiente: hace falta calor de verdad, no la deriva térmica normal.</summary>
        private const byte UmbralAireCalienteRaw = 14;

        /// <summary>Cuántas celdas hacia abajo se busca la fuente de calor real (para derivar el color de una mota) antes de rendirse y usar el tinte de emergencia.</summary>
        private const int ProfundidadBusquedaFuente = 4;

        /// <summary>
        /// TINTE DE EMERGENCIA (contrato: "CarbonEmergencia como tinte de
        /// emergencia"): la regla del taller es que NINGÚN color de partícula
        /// sea blanco puro y que SIEMPRE derive de un material real. Para las
        /// motas de aire caliente eso normalmente sale de mirar hacia abajo
        /// hasta encontrar el material que las calienta (ver
        /// <see cref="ColorFuenteCalor"/>) -- pero si esa búsqueda no
        /// encuentra ningún material en <see cref="ProfundidadBusquedaFuente"/>
        /// celdas (el calor llegó por difusión lateral, no vertical), hace
        /// falta un color de todos modos: un ascua apagada, nunca blanco.
        /// </summary>
        private static readonly Color32 CarbonEmergencia = new Color32(96, 60, 40, 255);

        /// <summary>
        /// Crea el overlay y engancha esta capa a la sim indicada. Lo llama
        /// <c>Game/AlkahestGameBootstrap.cs</c> UNA vez por partida (un
        /// jugador o multi, ver TrySpawn/TrySpawnRed) tras que exista
        /// Universe/Grid.
        /// </summary>
        public void Init(AlkahestSim sim)
        {
            _sim = sim;
            _universe = sim.Universe;
            _grid = sim.Grid;

            _particulas = new Particula[Capacidad];
            _dirtyPrev = new int[Capacidad];
            _dirtyCurr = new int[Capacidad];

            InicializarOverlay();

            _mainCam = Camera.main;
        }

        private void InicializarOverlay()
        {
            _pixels = new Color32[CellGrid.W * CellGrid.H];
            _texture = new Texture2D(CellGrid.W, CellGrid.H, TextureFormat.RGBA32, false, false)
            {
                // MISMO criterio que SimRenderer.Init: un téxel por celda,
                // Point para que no se difumine contra la textura del mundo
                // (que también es Point, ver la regla de la trampa del borde
                // Difuso en CLAUDE.md).
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "AlkahestParticulasFxTexture",
            };
            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = default; // transparente
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);

            var sr = gameObject.AddComponent<SpriteRenderer>();
            // COPIA LITERAL del posicionamiento/escala de SimRenderer.BuildQuad:
            // mismo ppu (1/CellWorldSize), mismo pivote (0,0), textura del
            // mismo tamaño exacto (W x H) -- así el overlay cae EXACTAMENTE
            // sobre la textura del mundo, celda a celda, sin ningún ajuste
            // adicional de escala/offset.
            float ppu = 1f / SimRenderer.CellWorldSize;
            sr.sprite = Sprite.Create(_texture, new Rect(0, 0, CellGrid.W, CellGrid.H),
                Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = SortingOrderOverlay;

            // Mismo GameObject "Alkahest" que aloja SimRenderer nace en el
            // origen del mundo (ver Editor/AlkahestSceneBuilder.BuildAlkahestObject)
            // y su quad de sim también vive en (0,0) local -- se replica
            // aquí sin parentar (este componente vive en su PROPIO
            // GameObject, creado por AlkahestGameBootstrap) fijando la
            // posición de mundo directamente a cero, que es donde cae el
            // pivote (0,0) de la textura de la sim.
            transform.position = Vector3.zero;
        }

        // =================================================================
        // (playtest 44, pedido directo de Cesar) LA CAPA ENTERA, APAGADA POR
        // DEFECTO: "de momento desactiva las partículas baratas que generas
        // por encima... es difícil calcular qué harán animaciones baratas
        // sin máquinas definidas al 100%. Dependamos únicamente de las
        // animaciones que generan las partículas del algoritmo tipo Noita."
        // El código se queda ÍNTEGRO (regla 15): cuando las máquinas estén
        // definidas al 100%, esta capa vuelve con un flag. Reactivable en
        // caliente desde la paleta dev (F3) para compararla con ojos.
        // =================================================================
        public static bool Activas = false;
        private bool _overlayLimpio;

        private void Update()
        {
            if (_grid == null || _universe == null) return;

            // Capa apagada: borra lo que quedara pintado UNA vez y duerme.
            if (!Activas)
            {
                if (!_overlayLimpio)
                {
                    for (int i = 0; i < _dirtyPrevCount; i++) _pixels[_dirtyPrev[i]] = default;
                    for (int i = 0; i < _dirtyCurrCount; i++) _pixels[_dirtyCurr[i]] = default;
                    if (_dirtyPrevCount > 0 || _dirtyCurrCount > 0)
                    {
                        _texture.SetPixels32(_pixels);
                        _texture.Apply(false);
                    }
                    _dirtyPrevCount = 0;
                    _dirtyCurrCount = 0;
                    _cursorEventosFx = ulong.MaxValue; // se re-engancha al reactivar (LeerEventosDesde lo clampa solo).
                    _overlayLimpio = true;
                }
                return;
            }
            _overlayLimpio = false;

            if (_mainCam == null) _mainCam = Camera.main;

            // Clamp del dt: un hitch grande (carga de escena, GC spike) no
            // debe catapultar una partícula fuera del mundo de un salto --
            // puramente cosmético, no hace falta el acumulador de 30Hz de la
            // sim real (esto no es determinista ni necesita serlo).
            float dt = Mathf.Min(Time.deltaTime, 0.05f);

            _nacidosEsteFrame = 0;
            SondearFranja();
            ConsumirEventosSimParaFx();

            // 1) Borra los téxeles que pintó el frame ANTERIOR (antes de
            //    pintar los de este frame: si una partícula sigue en el
            //    mismo téxel dos frames seguidos, se borra y se repinta
            //    igual, sin coste extra real).
            for (int i = 0; i < _dirtyPrevCount; i++) _pixels[_dirtyPrev[i]] = default;

            // 2) Física + pintado de este frame (rellena _dirtyCurr).
            _dirtyCurrCount = 0;
            ActualizarParticulas(dt);

            // Solo se sube a GPU si de verdad hubo algo que borrar o pintar
            // -- un taller sin ninguna partícula viva no toca la textura.
            if (_dirtyPrevCount > 0 || _dirtyCurrCount > 0)
            {
                _texture.SetPixels32(_pixels);
                _texture.Apply(false);
            }

            // 3) Lo pintado ahora es lo que habrá que borrar el frame que
            //    viene: swap de REFERENCIA, cero allocs.
            var tmp = _dirtyPrev; _dirtyPrev = _dirtyCurr; _dirtyCurr = tmp;
            _dirtyPrevCount = _dirtyCurrCount;
        }

        // =================================================================
        // FÍSICA + PINTADO
        // =================================================================
        private void ActualizarParticulas(float dt)
        {
            for (int i = 0; i < Capacidad; i++)
            {
                ref Particula p = ref _particulas[i];
                if (p.vida <= 0f) continue;

                float gravedad = GravedadPorTipo[p.tipo];
                float friccion = FriccionPorTipo[p.tipo];

                p.vy -= gravedad * dt;
                float amortiguacion = Mathf.Exp(-friccion * dt);
                p.vx *= amortiguacion;
                p.vy *= amortiguacion;
                p.x += p.vx * dt;
                p.y += p.vy * dt;
                p.vida -= dt;

                int px = Mathf.FloorToInt(p.x);
                int py = Mathf.FloorToInt(p.y);
                bool fueraDelMundo = px < 0 || px >= CellGrid.W || py < 0 || py >= CellGrid.H;

                if (p.vida <= 0f || fueraDelMundo)
                {
                    p.vida = 0f; // muerta: el ring buffer reutiliza este slot cuando el cursor vuelva a pasar por aquí.
                    continue;
                }

                // Alfa se desvanece linealmente con la vida restante -- la
                // partícula nace opaca (salvo el propio alfa del color base,
                // p.ej. el vapor ya nace semitransparente) y se apaga sola.
                float fraccionVida = Mathf.Clamp01(p.vida / p.vidaMax);
                byte a = (byte)(p.color.a * fraccionVida);

                int idxPixel = CellGrid.Idx(px, py);
                _pixels[idxPixel] = new Color32(p.color.r, p.color.g, p.color.b, a);
                _dirtyCurr[_dirtyCurrCount++] = idxPixel;
            }
        }

        private void Spawn(TipoParticula tipo, float x, float y, float vx, float vy, float vidaSegundos, Color32 color)
        {
            ref Particula p = ref _particulas[_cursor];
            p.x = x; p.y = y;
            p.vx = vx; p.vy = vy;
            p.vida = vidaSegundos;
            p.vidaMax = vidaSegundos;
            p.color = color;
            p.tipo = (byte)tipo;

            _cursor++;
            if (_cursor >= Capacidad) _cursor = 0;
            _nacidosEsteFrame++;
        }

        // =================================================================
        // SONDEO: LA FRANJA DE FILAS ALREDEDOR DE LA CÁMARA
        // =================================================================
        private void SondearFranja()
        {
            ComputeVentanaSondeo(out int x0, out int y0, out int x1, out int y1);
            if (x1 <= x0 || y1 <= y0) return;

            // La cámara pudo moverse fuera del rango que veníamos barriendo
            // (teletransporte, cambio de escena): si la fila del cursor ya
            // no cae dentro de la ventana actual, se reinicia arriba en vez
            // de seguir sondeando filas que ya no se ven.
            if (_filaSondeo < y0 || _filaSondeo >= y1) _filaSondeo = y0;

            uint tickActual = TickActualDelMundo();
            uint tickCorte = tickActual > VentanaTicksRecientes ? tickActual - VentanaTicksRecientes : 0;

            for (int f = 0; f < FilasPorFrame && _nacidosEsteFrame < BudgetNacimientosPorFrame; f++)
            {
                int y = _filaSondeo;
                int filaBase = y * CellGrid.W;
                for (int x = x0; x < x1; x++)
                {
                    if (_nacidosEsteFrame >= BudgetNacimientosPorFrame) break;
                    int idx = filaBase + x;
                    if (_grid.touchedTick[idx] < tickCorte) continue; // no está "activa recientemente"
                    ProcesarCelda(x, y, idx);
                }

                _filaSondeo++;
                if (_filaSondeo >= y1) _filaSondeo = y0;
            }
        }

        /// <summary>Rectángulo de celdas alrededor de la cámara actual (+margen), clampado al mundo. Mismo espíritu que SimRenderer.ComputeVisibleChunkRange, pero en celdas (aquí no hay chunks que alinear).</summary>
        private void ComputeVentanaSondeo(out int x0, out int y0, out int x1, out int y1)
        {
            if (_mainCam == null)
            {
                x0 = 0; y0 = 0; x1 = CellGrid.W; y1 = CellGrid.H;
                return;
            }

            float halfH = _mainCam.orthographicSize;
            float halfW = halfH * (_mainCam.aspect > 0.01f ? _mainCam.aspect : 16f / 9f);
            Vector3 p = _mainCam.transform.position;
            float celda = SimRenderer.CellWorldSize;

            x0 = Mathf.Clamp(Mathf.FloorToInt((p.x - halfW) / celda) - VentanaMargenCeldas, 0, CellGrid.W);
            x1 = Mathf.Clamp(Mathf.CeilToInt((p.x + halfW) / celda) + VentanaMargenCeldas, 0, CellGrid.W);
            y0 = Mathf.Clamp(Mathf.FloorToInt((p.y - halfH) / celda) - VentanaMargenCeldas, 0, CellGrid.H);
            y1 = Mathf.Clamp(Mathf.CeilToInt((p.y + halfH) / celda) + VentanaMargenCeldas, 0, CellGrid.H);
        }

        /// <summary>Tick "actual" del mundo, venga de un stepper real (anfitrión/un jugador) o del reloj del espejo (invitado) -- ambos campos públicos ya existentes en AlkahestSim, ninguna API nueva.</summary>
        private uint TickActualDelMundo()
        {
            return _sim.Stepper != null ? _sim.Stepper.Tick : _sim.TickEspejo;
        }

        // =================================================================
        // (integración pt39) EVENTOS DE LA SIM COMO DISPARADORES PUNTUALES
        // =================================================================
        // El sondeo por franjas de arriba es un heurístico continuo (barre
        // filas, puede llegar tarde a un episodio). El ring de eventos del
        // stepper (lector NO destructivo LeerEventosDesde, cursor propio de
        // esta capa: SubstanceKnowledge conserva intacto su ConsumeEvents)
        // da el complemento EPISÓDICO exacto: el instante de una ignición,
        // un hervor o un fuego muriendo en brasa dispara su ráfaga en la
        // celda justa, aunque la franja anduviera barriendo otra fila.
        // Solo existe en quien corre la sim (anfitrión/un jugador): el
        // invitado no tiene stepper y se queda con la observación pura,
        // que ya cubre lo continuo (chispas, vaho, ascuas por celda).
        private ulong _cursorEventosFx;
        private readonly SimNotableEvent[] _bufEventosFx = new SimNotableEvent[64]; // preasignado en el campo: cero allocs por frame.

        private void ConsumirEventosSimParaFx()
        {
            if (_sim.Stepper == null) return;
            ComputeVentanaSondeo(out int vx0, out int vy0, out int vx1, out int vy1);

            int n = _sim.Stepper.LeerEventosDesde(ref _cursorEventosFx, _bufEventosFx);
            for (int i = 0; i < n && _nacidosEsteFrame < BudgetNacimientosPorFrame; i++)
            {
                ref var e = ref _bufEventosFx[i];
                if (e.x < vx0 || e.x >= vx1 || e.y < vy0 || e.y >= vy1) continue; // fuera de cámara: nadie lo ve, no se paga.

                switch (e.type)
                {
                    case SimEventType.Ignite:
                        // El instante de prender: dos chispas enérgicas hacia
                        // arriba, del color del fuego joven (amarillo).
                        for (int k = 0; k < 2 && _nacidosEsteFrame < BudgetNacimientosPorFrame; k++)
                            Spawn(TipoParticula.Chispa, e.x + 0.5f, e.y + 0.5f,
                                  Random.Range(-0.8f, 0.8f), Random.Range(1.6f, 3.0f),
                                  Random.Range(0.4f, 0.9f), new Color32(255, 205, 63, 255));
                        break;
                    case SimEventType.Boil:
                        // El instante de hervir: una voluta puntual, además
                        // del vaho continuo que ya emite el Steam resultante.
                        // (playtest 41) Mismo ritmo perezoso que TrySpawnVaho:
                        // las volutas de un hervor y las del vapor ya formado
                        // tienen que moverse igual o se leen como dos efectos
                        // distintos ocurriendo en el mismo sitio.
                        Spawn(TipoParticula.Vaho, e.x + 0.5f, e.y + 0.5f,
                              Random.Range(-0.25f, 0.25f), Random.Range(0.3f, 0.8f),
                              Random.Range(0.9f, 2.0f), new Color32(224, 228, 232, 130));
                        break;
                    case SimEventType.Ember:
                        // Un fuego acaba de morir en brasa: ráfaga de 3
                        // ascuas -- el "último suspiro" que marca dónde quedó
                        // rescoldo reencendible (información jugable, no solo
                        // adorno).
                        for (int k = 0; k < 3 && _nacidosEsteFrame < BudgetNacimientosPorFrame; k++)
                            Spawn(TipoParticula.Chispa, e.x + 0.5f, e.y + 0.5f,
                                  Random.Range(-0.5f, 0.5f), Random.Range(0.8f, 1.8f),
                                  Random.Range(0.5f, 1.1f), new Color32(205, 92, 32, 255));
                        break;
                    // Los demás tipos (Freeze/Crystallize/Grow/Dissolve/Ley)
                    // no piden partícula por ahora: default silencioso, misma
                    // convención que SubstanceKnowledge con tipos que ignora.
                }
            }
        }

        // =================================================================
        // DISPARADORES (contrato §2, la lista exacta)
        // =================================================================
        private void ProcesarCelda(int x, int y, int idx)
        {
            byte matId = _grid.mat[idx];

            if (matId == MaterialId.Empty)
            {
                TrySpawnMotaCrisol(x, y, idx);
                return;
            }
            if (matId == MaterialId.Fire)
            {
                TrySpawnChispa(x, y, idx);
                return;
            }
            if (matId == MaterialId.Steam)
            {
                TrySpawnVaho(x, y, idx);
                return;
            }
            // (integración pt39) La BRASA respira: ascuas tenues muy
            // ocasionales que flotan un instante y mueren -- es lo que
            // separa visualmente "rescoldo vivo" de "ceniza muerta" para el
            // jugador que decide si sopla combustible fresco encima.
            if (matId == MaterialId.Brasa)
            {
                TrySpawnAscuaBrasa(x, y);
                return;
            }

            // Salpicaduras/polvo: la celda debe estar EN LA SUPERFICIE de un
            // montón/charco -- "abajo" no vacío (algo la sostiene) y "arriba"
            // vacío (aire libre encima). Interpretación deliberada de
            // "sólido debajo" (contrato): CUALQUIER material no-vacío vale
            // de apoyo, no solo StaticSolid -- un líquido posado sobre OTRO
            // líquido más denso (p.ej. agua sobre limo) también cuenta como
            // "aterrizó en una superficie" a efectos puramente visuales; no
            // hay ninguna noción de sim real que dependa de esta distinción
            // aquí.
            //
            // idx+W = celda de ARRIBA (y+1), idx-W = celda de ABAJO (y-1):
            // misma convención que SimRenderer/SimStepper (ver sus
            // comentarios junto al chequeo de superficie de líquidos).
            bool arribaVacia = y < CellGrid.H - 1 && _grid.mat[idx + CellGrid.W] == MaterialId.Empty;
            if (!arribaVacia) return;
            bool abajoOcupada = y > 0 && _grid.mat[idx - CellGrid.W] != MaterialId.Empty;
            if (!abajoOcupada) return;

            var def = _universe.Get(matId);
            if (def.archetype == MaterialArchetype.Liquid) TrySpawnSalpicadura(x, y, def);
            else if (def.archetype == MaterialArchetype.Powder) TrySpawnPolvo(x, y, def);
        }

        private void TrySpawnSalpicadura(int x, int y, MaterialDef def)
        {
            if (Random.value > ProbSalpicadura) return;
            Color32 col = def.baseColor; // SIEMPRE el color real del líquido que salpica.
            int n = Random.Range(1, 4); // 1..3 gotitas (contrato).
            for (int i = 0; i < n && _nacidosEsteFrame < BudgetNacimientosPorFrame; i++)
            {
                float vx = Random.Range(-1.6f, 1.6f);
                float vy = Random.Range(0.8f, 2.2f);
                float vida = Random.Range(0.3f, 0.8f);
                Spawn(TipoParticula.Salpicadura, x + 0.5f, y + 1f, vx, vy, vida, col);
            }
        }

        private void TrySpawnChispa(int x, int y, int idx)
        {
            if (Random.value > ProbChispa) return;

            // MISMA fórmula de color que SimRenderer.ComputeCellColor para
            // Fire (joven=amarillo brillante, vieja=rojo profundo según
            // aux/gasLifetime): así una chispa desprendida se ve del MISMO
            // tono que la llama que la escupió, nunca blanco.
            var defFuego = _universe.Get(MaterialId.Fire);
            float vida01 = defFuego.gasLifetime > 0
                ? Mathf.Clamp01(_grid.aux[idx] / (float)defFuego.gasLifetime)
                : 1f;
            byte r = (byte)Mathf.Clamp(205 + 50f * vida01, 0, 255);
            byte g = (byte)Mathf.Clamp(55 + 150f * vida01, 0, 255);
            byte b = (byte)Mathf.Clamp(8 + 55f * vida01, 0, 255);
            Color32 col = new Color32(r, g, b, 255);

            float vx = Random.Range(-0.6f, 0.6f);
            float vy = Random.Range(1.0f, 2.6f); // sube al nacer, la gravedad (positiva para Chispa) la frena y la hace caer apagándose.
            float vidaSeg = Random.Range(0.3f, 0.9f);
            Spawn(TipoParticula.Chispa, x + 0.5f, y + 0.5f, vx, vy, vidaSeg, col);
        }

        /// <summary>(integración pt39) Ascua tenue de una celda de Brasa viva: mucho más rara y perezosa que la chispa del Fire (la brasa DESCANSA, no arde) -- flota apenas y muere corta. La lee ProcesarCelda cuando la celda observada es MaterialId.Brasa.</summary>
        private void TrySpawnAscuaBrasa(int x, int y)
        {
            if (Random.value > ProbAscuaBrasa) return;

            // Del color base de la brasa (ámbar apagado) con un empujón
            // cálido aleatorio hacia el naranja: rescoldo, nunca llama.
            var defBrasa = _universe.Get(MaterialId.Brasa);
            byte extra = (byte)Random.Range(0, 46);
            Color32 col = new Color32((byte)Mathf.Min(255, defBrasa.baseColor.r + extra),
                                      (byte)Mathf.Min(255, defBrasa.baseColor.g + extra / 2),
                                      defBrasa.baseColor.b, 255);

            float vx = Random.Range(-0.25f, 0.25f);
            float vy = Random.Range(0.4f, 1.0f); // apenas flota: es rescoldo cansado, no chispa de incendio.
            float vidaSeg = Random.Range(0.3f, 0.7f);
            Spawn(TipoParticula.Chispa, x + 0.5f, y + 0.5f, vx, vy, vidaSeg, col);
        }

        private void TrySpawnMotaCrisol(int x, int y, int idx)
        {
            byte t = _grid.temp[idx];
            if (t <= CellGrid.AmbientRaw + UmbralAireCalienteRaw) return;
            if (Random.value > ProbMota) return;

            Color32 col = ColorFuenteCalor(x, y);
            float vx = Random.Range(-0.3f, 0.3f);
            float vy = Random.Range(0.6f, 1.4f);
            float vidaSeg = Random.Range(0.6f, 1.5f);
            Spawn(TipoParticula.Mota, x + 0.5f, y + 0.5f, vx, vy, vidaSeg, col);
        }

        /// <summary>Busca hacia abajo el primer material real para derivar el color de una mota de aire caliente; sin fuente identificable, cae al tinte de emergencia (nunca blanco). Ver CarbonEmergencia.</summary>
        private Color32 ColorFuenteCalor(int x, int y)
        {
            for (int dy = 1; dy <= ProfundidadBusquedaFuente; dy++)
            {
                int yy = y - dy;
                if (yy < 0) break;
                byte m = _grid.mat[CellGrid.Idx(x, yy)];
                if (m != MaterialId.Empty) return _universe.Get(m).baseColor;
            }
            return CarbonEmergencia;
        }

        private void TrySpawnPolvo(int x, int y, MaterialDef def)
        {
            if (Random.value > ProbPolvo) return;
            Color32 col = def.baseColor;
            int n = Random.Range(1, 3); // nubecita pequeña, menos volumen que una salpicadura.
            for (int i = 0; i < n && _nacidosEsteFrame < BudgetNacimientosPorFrame; i++)
            {
                float vx = Random.Range(-0.4f, 0.4f);
                float vy = Random.Range(0.1f, 0.5f);
                float vidaSeg = Random.Range(0.3f, 0.7f); // se disipa rápido (contrato).
                Spawn(TipoParticula.Polvo, x + 0.5f, y + 1f, vx, vy, vidaSeg, col);
            }
        }

        /// <summary>
        /// Voluta de vapor. RECALIBRADO EN EL PLAYTEST 41
        /// (CONTRATO_VAPOR.md §2), tras mirar en pantalla una columna de
        /// hervor real del crisol con la convección nueva del encargo S:
        ///
        /// DENTRO de una masa densa de Steam estas motas eran INVISIBLES --
        /// un téxel blanquecino sobre otro téxel blanquecino no aporta nada y
        /// solo gasta presupuesto de nacimientos. Donde el vaho SÍ se lee es
        /// en el BORDE del penacho: ahí una voluta que se desprende y se
        /// demora es exactamente lo que dibuja la silueta del gas y lo hace
        /// parecer materia y no un bloque de píxeles. Así que ahora el vaho
        /// nace SOLO en celdas de vapor con aire libre encima o al lado.
        ///
        /// La decisión de fondo (contrato: "sprite vs gas") es que el gas real
        /// es el protagonista y esta capa pasa a ACENTO: menos volutas, mejor
        /// colocadas, más lentas y más largas -- acompañan la corriente en vez
        /// de taparla.
        /// </summary>
        private void TrySpawnVaho(int x, int y, int idx)
        {
            if (!GasEnLaSuperficie(x, y, idx)) return; // dentro de la masa no se ve: no se paga.
            if (Random.value > ProbVaho) return;
            // El color real del Steam del roster YA es blanco-azulado
            // semitransparente (224,228,232,130 en Universe.cs), no blanco
            // puro -- se usa tal cual, derivado del material real.
            Color32 col = _universe.Get(MaterialId.Steam).baseColor;
            float vx = Random.Range(-0.25f, 0.25f);
            float vy = Random.Range(0.25f, 0.7f);  // más perezosa que antes (0.4-1.0): acompaña al gas, no lo adelanta.
            float vidaSeg = Random.Range(0.9f, 2.0f); // y se demora más: una voluta que se deshace, no un parpadeo.
            Spawn(TipoParticula.Vaho, x + 0.5f, y + 0.5f, vx, vy, vidaSeg, col);
        }

        /// <summary>
        /// ¿Esta celda de gas está en el CONTORNO de su masa (aire libre
        /// arriba o a un lado)? Lo consume <see cref="TrySpawnVaho"/> para
        /// nacer solo donde una voluta se puede ver. idx+W = arriba, idx±1 =
        /// laterales (misma convención que SimStepper/SimRenderer). No mira
        /// hacia abajo a propósito: una voluta que se desprende del vientre
        /// de una nube y cae no existe.
        /// </summary>
        private bool GasEnLaSuperficie(int x, int y, int idx)
        {
            if (y < CellGrid.H - 1 && _grid.mat[idx + CellGrid.W] == MaterialId.Empty) return true;
            if (x > 0 && _grid.mat[idx - 1] == MaterialId.Empty) return true;
            if (x < CellGrid.W - 1 && _grid.mat[idx + 1] == MaterialId.Empty) return true;
            return false;
        }
    }
}
