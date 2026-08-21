using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 60, GDD v0.3 §5 -- EL INICIO OSCURO, greybox; REDISEÑADO EN LA
    /// RONDA 64 sobre el primer playtest con la escena real) El director de la
    /// FUNDACIÓN: los primeros minutos de TEN THOUSAND YEARS.
    ///
    /// LO QUE CAMBIÓ EN LA RONDA 64 (feedback textual de Cesar, pt64):
    ///  - "La gotera no es gotera, hay pozos de agua y agua hirviendo más
    ///    vapor": caía SOBRE las brasas (GoteraX=426). Ahora cae en la POZA
    ///    seca (x379), lejos del fuego; el objetivo es recoger la inundación,
    ///    y el goteo se PAUSA solo si la poza se llena (guarda con
    ///    histéresis, prima de la RACIÓN del pt26) y MUERE por script al
    ///    nacer el grifo.
    ///  - "Los textos son largos, desaparecen muy rápido y arriba": la banda
    ///    temporizada se sustituye por DIÁLOGO POR PÁGINAS estilo novela
    ///    visual -- panel abajo-centro en la línea del bautizo (PanelRito),
    ///    frases cortas, avanza con CLIC/E/ESPACIO y NO desaparece solo.
    ///    Mientras está abierto, UiStyles.EscribiendoTexto bloquea el mundo
    ///    entero (regla 12: todos los consumidores ya la respetan) para que
    ///    el clic de "continuar" jamás aspire nada.
    ///  - "Garantizar la primera experiencia, a prueba de burros": OBJETIVO
    ///    PERSISTENTE -- una banda fina abajo que dice el gesto exacto
    ///    ("MANTÉN CLIC IZQUIERDO sobre el agua") con progreso, siempre
    ///    visible hasta completar el beat.
    ///  - "Material tirado por ahí, no puede ser": el plano ya no coloca
    ///    arcilla/arena/charco (SimLevelBuilder ronda 64); el Maestro los
    ///    ENTREGA y caen a la vista (TickVertidoDelMaestro). La veta perdió
    ///    su asomo derramable: la señala un cartel de mundo desde el beat 4.
    ///  - "Esa animación horrible de luz... no queremos animaciones, tenemos
    ///    física real": FUERA el glow radial pintado (textura _glow, RIP
    ///    directiva 11 del 62b). El hogar del Maestro pasa a ser BRASA REAL
    ///    (MaterialId.Brasa, el material del motor: inyecta calor de verdad)
    ///    con lenguas de Fire REALES mantenidas por el director; la única luz
    ///    del arranque es ese fuego, y el titileo de la viñeta se DERIVA del
    ///    número de celdas de Fire vivas -- luz con causa física, cero
    ///    animación decorativa.
    ///
    /// PENDIENTE ANOTADO (pedido de Cesar para más adelante): botón "omitir
    /// intro" para partidas repetidas.
    ///
    /// DISEÑO DE COMPLETADO: sin Buzón de Entrada todavía -- el director
    /// espeja Order.Progreso con el contenido real del frasco y completa por
    /// PROXIMIDAD al Maestro + contenido (Flask.Extraer). Cuando la fundación
    /// gane su Buzón (GDD §6), este atajo se retira.
    /// </summary>
    public sealed class FundacionDirector : MonoBehaviour
    {
        private enum Beat { Mirar, Saludo, Gotera, Barro, Fogon, Vidrio, Estante, Fin }

        // (gate de DayCycle.DetectarPrimeraAccion) Mientras sea false, moverse
        // NO despierta el HUD en ModoFundacion. Se sube al cerrar el diálogo
        // del saludo (cuando el Maestro ya PRESTÓ el frasco). Estática porque
        // DayCycle la lee sin referencia; se baja en OnDestroy.
        public static bool HudPermitido;

        private const byte BrasaRaw = 165;          // piso térmico del lecho (la Brasa real además inyecta el suyo).
        // (directiva Opus 64 #2-3, confirmada con runtime) La primera versión
        // PAUSABA la gotera con la poza llena: a los 39s el hilo moría y el
        // que llegaba tarde veía "un rectángulo azul inmóvil" -- el reclamo
        // original de Cesar, reintroducido por la guarda. Ahora la gota NUNCA
        // deja de caer: la poza REZUMA (el exceso de agua se filtra a la roca
        // por el fondo, TickGotera) y el nivel se estabiliza con la gota viva.
        // La gota además pasa de 1 celda a un hilo de 3 (masa visible) y cae
        // cada 0.95s.
        private const float DripSegundos = 0.95f;
        private const int CeldasPorGota = 3;
        private const int PozaLlenaCeldas = 34;     // nivel de equilibrio del rezumado (capacidad física ~42).
        private const int CeldasGotera = 15;
        private const int CeldasBarro = 12;
        private const float DistCharla = 16f;       // celdas al centro de la mesa para "estar con el Maestro".
        private const int CeldasFogon = 12;
        private const int CeldasVidrio = 8;
        private const int TurbaPrestada = 20;
        private const int ArcillaEntregada = 30;    // el costal del Maestro (pedido: 12 de barbotina -- margen 2.5x, a prueba de burros).
        private const int ArenaEntregada = 20;      // ídem (pedido: 8 de vidrio, cruce 1:1 con ceniza).
        // (VERIFICADO EN VIVO ronda 61, TRES iteraciones -- lección regla 50
        // en cascada) La combustión ABIERTA de la turba en pila no es domable:
        // a 175/152 se funde, y una celda de Fire de ignición (~240 estables)
        // funde la pila entera por cascada térmica. El fuego del hogar del
        // JUGADOR es consumo dirigido turba->ceniza + lecho tibio (118) +
        // cima de llama constante (152) para TryCruce. POR ESO el hogar del
        // jugador NO lleva lenguas de Fire pintadas dentro (ronda 64: se
        // evaluó y se descartó -- volvería a fundir la pila); su "arder" es
        // el consumo visible. Las lenguas reales viven SOLO en el hogar del
        // Maestro, donde no hay pila fundible (el lecho es Brasa, que ninguna
        // temperatura transforma).
        private const byte FogonRaw = 118;
        private const byte FogonCimaRaw = 152;
        private const float ConsumoTurbaSegundos = 0.7f;
        private const float CrisolPrimitivoSegundos = 0.5f;
        // El fuego real del hogar del Maestro:
        private const byte BrasaVida = 90;          // vida repuesta cada frame: la brasa del Maestro no muere (narrativa: él la atiende desde antes que tú nacieras).
        private const float FlamaSegundos = 0.12f;  // hueco máximo sin llama viva antes de reponer una (el Fire real vive ~16 ticks y muere solo: el parpadeo ES la sim).
        private const float Flama2Segundos = 1.7f;  // cadencia de la segunda lengua ocasional (variedad real, no bucle).

        private AlkahestSim _sim;
        private OrderSystem _orders;
        private Flask _flask;
        private SubstanceKnowledge _saber;
        private Transform _aprendiz;

        private Beat _beat = Beat.Mirar;
        private float _tBeat;
        private float _dripTimer;
        private bool _goteraActiva = true;
        private bool _pedidoEncolado;
        private bool _fogonEncendido;
        private float _crisolTimer, _consumoTimer;
        private float _flamaTimer, _flama2Timer;
        private int _flamaIdx;
        private static readonly byte Barbotina = MaterialId.MatDe(1, EstadoMateria.Solucion);
        private static readonly byte ArcillaPolvo = MaterialId.MatDe(1, EstadoMateria.Polvo);
        private static readonly byte ArenaSilice = MaterialId.MatDe(0, EstadoMateria.Polvo);
        private static readonly byte Turba = MaterialId.MatDe(Universe.SemillaCeroBaseTurbaIdx, EstadoMateria.Polvo);

        // ------------------------------------------------------------------
        // EL DIÁLOGO (ronda 64): páginas cortas, clic para continuar.
        // ------------------------------------------------------------------
        // [NonSerialized]: en un hot-reload del editor, Unity serializa hasta
        // los campos privados y RESUCITA un array null como array VACÍO --
        // el diálogo quedaba "abierto" con 0 páginas e indexaba -1 en cada
        // OnGUI (visto en vivo, ronda 64b: 50 IndexOutOfRange por segundo
        // tras el primer Ctrl+R con el director vivo). El diálogo es estado
        // de runtime puro: no debe sobrevivir a ningún reload.
        [System.NonSerialized] private string[] _paginas;
        [System.NonSerialized] private int _pagina;
        [System.NonSerialized] private System.Action _alCerrar;
        private bool DialogoAbierto => _paginas != null && _paginas.Length > 0;

        // El VERTIDO del Maestro (ronda 64): material entregándose, cayendo.
        private byte _vertidoMat;
        private int _vertidoRestante;
        private int _vertidoX;
        private float _vertidoTimer;

        // La viñeta.
        private Texture2D _vineta;
        private float _radio;
        private float _radioObjetivo;
        private float _luzFuego = 1f; // titileo DERIVADO de las celdas de Fire reales (suavizado).

        public void Init(AlkahestSim sim, OrderSystem orders, Flask flask, SubstanceKnowledge saber, Transform aprendiz)
        {
            _sim = sim;
            _orders = orders;
            _flask = flask;
            _saber = saber;
            _aprendiz = aprendiz;
            SpawnMaestroSilueta();
            HudPermitido = false;
            // S(280): con 240 el spawn quedaba en el anillo oscuro del óvalo
            // (medido con capturas, ronda 64c) -- el jugador debe verse a sí
            // mismo como silueta desde el frame cero. A prueba de burros.
            _radioObjetivo = UiStyles.S(280f);
            _radio = _radioObjetivo;
        }

        private void OnDestroy()
        {
            HudPermitido = false;
            if (_instancia == this) _instancia = null;
            if (_paginas != null) { _paginas = null; UiStyles.EscribiendoTexto = false; }
            if (_vineta != null) Destroy(_vineta);
            if (_maestroGo != null) Destroy(_maestroGo);
            if (_maestroTex != null) Destroy(_maestroTex);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;

            // La física de fondo corre SIEMPRE, con o sin diálogo abierto: el
            // fuego arde, la gotera cae, el costal se derrama. El mundo no se
            // congela para hablar -- solo el input del jugador.
            MantenerFuegoDelMaestro();
            MantenerFogonPropio();
            TickGotera();
            TickVertidoDelMaestro();
            if (_fogonEncendido) TickCrisolPrimitivo();

            if (_paginas != null && _paginas.Length == 0) _paginas = null; // cinturón extra anti-reload.
            if (DialogoAbierto)
            {
                TickDialogo();
            }
            else
            {
                _tBeat += Time.deltaTime;
                switch (_beat)
                {
                    case Beat.Mirar: TickMirar(); break;
                    case Beat.Saludo: break; // transición ocurre al cerrar el diálogo del saludo.
                    case Beat.Gotera: TickPedidoDeEntrega(MaterialId.Water, CeldasGotera, CompletarGotera); break;
                    case Beat.Barro: TickPedidoDeEntrega(Barbotina, CeldasBarro, CompletarBarro); break;
                    case Beat.Fogon: TickFogon(); break;
                    case Beat.Vidrio: TickPedidoDeEntrega(MaterialId.VidrioVerde, CeldasVidrio, CompletarVidrio); break;
                    case Beat.Estante: TickEstante(); break;
                    case Beat.Fin: break;
                }
            }

            _radio = Mathf.Lerp(_radio, _radioObjetivo, Time.deltaTime * 1.6f);

            // (RONDA 66) EL CORRAL DEL GREYBOX SE RETIRÓ: existió una ronda
            // (64) porque el imp atravesaba la piedra y a oscuras eso era un
            // tester perdido dentro de la roca. Desde la ronda 66 el aprendiz
            // COLISIONA de verdad con la arquitectura (ver
            // ApprenticeController.ColisionConEstructura) -- el corral no solo
            // sobraba: prohibía la fantasía nueva de TALLAR túneles y salir de
            // la caverna (regla 15: idea retirada, documentada).
        }

        // ------------------------------------------------------------------
        // EL FUEGO REAL del Maestro (ronda 64).
        // ------------------------------------------------------------------
        /// <summary>
        /// El lecho del hogar del Maestro es BRASA REAL (MaterialId.Brasa):
        /// el material del motor que inyecta calor de verdad
        /// (SimStepper.ProcessBrasa) y moriría a ceniza si nadie lo cuidara.
        /// El director lo cuida: repone la vida (aux) cada frame y re-siembra
        /// cualquier celda del lecho que haya dejado de ser Brasa
        /// (autocurativo -- cubre el primer frame, en que SetCell nace con
        /// aux=0 y un tick muestreado la haría ceniza). Encima, mantiene 1-2
        /// lenguas de Fire REALES: el Fire del motor vive ~16 ticks y muere
        /// solo, así que el parpadeo que se ve es la sim matando y el
        /// director reponiendo -- física, no animación (mandato pt64).
        /// </summary>
        private void MantenerFuegoDelMaestro()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            // 1) El lecho de brasas (2 filas entre los muretes).
            for (int x = SimLevelBuilder.FundacionBrasasX0; x <= SimLevelBuilder.FundacionBrasasX1; x++)
            {
                for (int y = SimLevelBuilder.FundacionY0; y <= SimLevelBuilder.FundacionBrasasY; y++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.mat[idx] != MaterialId.Brasa)
                    {
                        // Ceniza, vacío o lo que sea: vuelve a ser brasa (el
                        // Maestro atiza). Fuera del lecho nada se toca.
                        grid.SetCell(x, y, MaterialId.Brasa);
                    }
                    grid.aux[idx] = BrasaVida;
                    if (grid.temp[idx] < BrasaRaw) grid.temp[idx] = BrasaRaw;
                }
            }
            grid.WakeChunk(SimLevelBuilder.FundacionBrasasX0, SimLevelBuilder.FundacionBrasasY, tick);
            grid.WakeChunk(SimLevelBuilder.FundacionBrasasX1, SimLevelBuilder.FundacionBrasasY, tick);

            // 2) Las lenguas de Fire reales, y el titileo de la luz DERIVADO
            // de ellas (la única luz del arranque es este fuego, mandato pt64).
            int nFire = ContarFuego(SimLevelBuilder.FundacionBrasasX0, SimLevelBuilder.FundacionBrasasX1,
                SimLevelBuilder.FundacionBrasasY + 1, SimLevelBuilder.FundacionBrasasY + 3);

            // (verificado en vivo, primera captura de la ronda 64) Con UNA
            // lengua el hogar leía como tres píxeles tímidos, no como "la
            // única fuente de luz". Objetivo: ~3 lenguas vivas -- el Fire del
            // motor muere solo a los ~16 ticks, así que el número real
            // oscila 1-3 y ESA oscilación es el titileo.
            _flamaTimer -= Time.deltaTime;
            if (nFire < 3 && _flamaTimer <= 0f)
            {
                _flamaTimer = FlamaSegundos;
                _flamaIdx = (_flamaIdx + 1) % 5;
                _sim.PaintStable(SimLevelBuilder.FundacionBrasasX0 + _flamaIdx,
                    SimLevelBuilder.FundacionBrasasY + 1, 0, MaterialId.Fire);
            }
            _flama2Timer -= Time.deltaTime;
            if (nFire < 2 && _flama2Timer <= 0f)
            {
                _flama2Timer = Flama2Segundos;
                _sim.PaintStable(SimLevelBuilder.FundacionBrasasX0 + ((_flamaIdx + 2) % 5),
                    SimLevelBuilder.FundacionBrasasY + 2, 0, MaterialId.Fire);
            }

            // Titileo: pocas llamas = luz baja, 3 = plena. Suavizado para que
            // respire en vez de estroboscopear.
            float objetivo = 0.92f + 0.03f * Mathf.Min(nFire, 3);
            _luzFuego = Mathf.Lerp(_luzFuego, objetivo, Time.deltaTime * 9f);

            // EL TIRO DEL HOGAR (directiva Opus #8): una lengua que sube más
            // de 6 filas ya no lee como fuego -- a esa altura el Fire del
            // motor vira a su tono pálido de agonía y parece "arena subiendo".
            // El hogar tiene tiro corto: por encima de +6, la llama se apaga.
            for (int x = SimLevelBuilder.FundacionBrasasX0 - 1; x <= SimLevelBuilder.FundacionBrasasX1 + 1; x++)
                for (int y = SimLevelBuilder.FundacionBrasasY + 7; y <= SimLevelBuilder.FundacionBrasasY + 14; y++)
                    if (grid.GetMat(x, y) == MaterialId.Fire)
                        grid.SetCell(x, y, MaterialId.Empty);
        }

        private int ContarFuego(int x0, int x1, int y0, int y1)
        {
            int n = 0;
            var grid = _sim.Grid;
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (grid.GetMat(x, y) == MaterialId.Fire) n++;
            return n;
        }

        /// <summary>El fuego PROPIO (beat 4+): lecho tibio + cima constante, SIN lenguas dentro (ver docblock de FogonRaw).</summary>
        private void MantenerFogonPropio()
        {
            if (!_fogonEncendido) return;
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            for (int x = SimLevelBuilder.FundacionFogonX0; x <= SimLevelBuilder.FundacionFogonX1; x++)
            {
                int idx = CellGrid.Idx(x, SimLevelBuilder.FundacionFogonY);
                if (grid.temp[idx] < FogonRaw) grid.temp[idx] = FogonRaw;
            }
            grid.WakeChunk(SimLevelBuilder.FundacionFogonX0, SimLevelBuilder.FundacionFogonY, tick);
            grid.WakeChunk(SimLevelBuilder.FundacionFogonX1, SimLevelBuilder.FundacionFogonY, tick);
        }

        /// <summary>Cuenta celdas de `mat` dentro del hogar del jugador (lecho + 5 filas encima).</summary>
        private int ContarEnFogon(byte mat, out int cx, out int cy)
        {
            cx = -1; cy = -1;
            int n = 0;
            var grid = _sim.Grid;
            for (int x = SimLevelBuilder.FundacionFogonX0; x <= SimLevelBuilder.FundacionFogonX1; x++)
                for (int y = SimLevelBuilder.FundacionFogonY; y <= SimLevelBuilder.FundacionFogonY + 5; y++)
                    if (grid.GetMat(x, y) == mat)
                    {
                        n++;
                        if (cx < 0) { cx = x; cy = y; }
                    }
            return n;
        }

        /// <summary>Cuenta celdas de `mat` en la poza (el cuenco tallado + 2 filas sobre el borde, por si el agua amontona).</summary>
        private int ContarEnPoza(byte mat)
        {
            int n = 0;
            var grid = _sim.Grid;
            for (int x = SimLevelBuilder.FundacionCharcoX0; x <= SimLevelBuilder.FundacionCharcoX1; x++)
                for (int y = SimLevelBuilder.FundacionY0 - 3; y <= SimLevelBuilder.FundacionY0 + 1; y++)
                    if (grid.GetMat(x, y) == mat) n++;
            return n;
        }

        /// <summary>
        /// (ronda 61) EL CRISOL PRIMITIVO: el hogar encendido consume su turba
        /// a ceniza por cadencia (la combustión del hogar, regla 54: deja
        /// residuo) y aplica el cruce REAL arena+ceniza -> vidrio
        /// (Universe.TryCruce, la misma tabla del Crisol de verdad). Una celda
        /// por pulso: la transformación SE VE trabajar.
        /// </summary>
        private void TickCrisolPrimitivo()
        {
            _consumoTimer -= Time.deltaTime;
            if (_consumoTimer <= 0f)
            {
                _consumoTimer = ConsumoTurbaSegundos;
                if (ContarEnFogon(Turba, out int tx, out int ty) > 0)
                {
                    var g = _sim.Grid;
                    g.SetCell(tx, ty, MaterialId.Ash);
                    g.WakeChunk(tx, ty, _sim.Stepper != null ? _sim.Stepper.Tick : 0u);
                }
            }

            _crisolTimer -= Time.deltaTime;
            if (_crisolTimer > 0f) return;
            _crisolTimer = CrisolPrimitivoSegundos;

            int nArena = ContarEnFogon(ArenaSilice, out int ax, out int ay);
            int nCeniza = ContarEnFogon(MaterialId.Ash, out int cx, out int cy);
            if (nArena == 0 || nCeniza == 0) return;

            if (!Universe.TryCruce(ArenaSilice, MaterialId.Ash, FogonCimaRaw, out byte producto, out _, out _)) return;

            var grid = _sim.Grid;
            grid.SetCell(ax, ay, producto);
            grid.SetCell(cx, cy, MaterialId.Empty);
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            grid.WakeChunk(ax, ay, tick);
            grid.WakeChunk(cx, cy, tick);
        }

        /// <summary>
        /// (ronda 64, corregida por la directiva Opus #2) La gotera cae en la
        /// POZA, lejos del fuego, y NUNCA se detiene sola -- muere solo por
        /// script al nacer el grifo. La poza REZUMA: el agua que exceda el
        /// nivel de equilibrio se filtra por el fondo a la roca (RezumarPoza),
        /// así el hilo sigue vivo eternamente sin desbordar jamás -- misma
        /// filosofía que la RACIÓN de los caños (pt26), pero con la fuente
        /// siempre visible. La gota es un hilo de 3 celdas (masa que se ve
        /// caer y salpica); PaintStable (regla 29: crea materia).
        /// </summary>
        private void TickGotera()
        {
            RezumarPoza();

            if (!_goteraActiva) return;

            _dripTimer -= Time.deltaTime;
            if (_dripTimer <= 0f)
            {
                _dripTimer = DripSegundos;
                for (int i = 0; i < CeldasPorGota; i++)
                    _sim.PaintStable(SimLevelBuilder.FundacionGoteraX, SimLevelBuilder.FundacionGoteraDripY - i, 0, MaterialId.Water);
            }
        }

        /// <summary>
        /// El fondo de la poza se bebe el exceso: si hay más agua que el
        /// nivel de equilibrio, una celda del FONDO desaparece por frame
        /// hasta volver a él. Corre SIEMPRE (también con el grifo abierto:
        /// la poza es incapaz de desbordar, es propiedad de la cubeta, no de
        /// la fuente). Solo se filtra AGUA -- la crema de barbotina no.
        /// </summary>
        private void RezumarPoza()
        {
            int agua = ContarEnPoza(MaterialId.Water);
            if (agua <= PozaLlenaCeldas) return;

            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            for (int y = SimLevelBuilder.FundacionY0 - 3; y <= SimLevelBuilder.FundacionY0 + 1 && agua > PozaLlenaCeldas; y++)
                for (int x = SimLevelBuilder.FundacionCharcoX0; x <= SimLevelBuilder.FundacionCharcoX1 && agua > PozaLlenaCeldas; x++)
                    if (grid.GetMat(x, y) == MaterialId.Water)
                    {
                        grid.SetCell(x, y, MaterialId.Empty);
                        grid.WakeChunk(x, y, tick);
                        return; // UNA celda por frame: el nivel baja respirando, no de golpe.
                    }
        }

        /// <summary>
        /// (ronda 64; POR PROXIMIDAD desde la directiva Opus #7) EL VERTIDO
        /// DEL MAESTRO: el material que él entrega CAE a la vista en ese
        /// momento -- nada existe antes ("si me lo da el Maestro, que caiga
        /// en ese momento", pt64). La primera versión vertía al CERRAR el
        /// diálogo: 3 segundos de caída en una zona a oscuras a 66 celdas del
        /// jugador -- "mírala caer" no mostraba nada, jamás (confirmado con
        /// runtime por Opus). Ahora el costal ESPERA: cae cuando el aprendiz
        /// está a ≤25 celdas del punto de entrega, una celda cada 0.15s
        /// (~5s de chorro): un costal vaciándose delante de ti.
        /// </summary>
        private const float VertidoCadencia = 0.15f;
        private const float VertidoDistCeldas = 25f;

        private void TickVertidoDelMaestro()
        {
            if (_vertidoRestante <= 0) return;

            float celda = SimRenderer.CellWorldSize;
            float distX = Mathf.Abs(_aprendiz.position.x / celda - _vertidoX);
            if (distX > VertidoDistCeldas) return; // el costal espera a que llegues.

            _vertidoTimer -= Time.deltaTime;
            if (_vertidoTimer > 0f) return;
            _vertidoTimer = VertidoCadencia;
            int jitter = (_vertidoRestante % 3) - 1; // -1,0,+1 determinista: el chorro serpentea.
            _sim.PaintStable(_vertidoX + jitter, SimLevelBuilder.FundacionDropY, 0, _vertidoMat);
            _vertidoRestante--;
        }

        private void VerterDelMaestro(byte mat, int cantidad, int x)
        {
            _vertidoMat = mat;
            _vertidoRestante = cantidad;
            _vertidoX = x;
            _vertidoTimer = 0f;
        }

        // ------------------------------------------------------------------
        // Beats.
        // ------------------------------------------------------------------
        /// <summary>
        /// (ronda 64, directiva Opus #4) LA SILUETA DEL MAESTRO: sin cuerpo,
        /// el rótulo "EL MAESTRO" señalaba ladrillo vacío y el tester creía
        /// que el fuego era el Maestro, o que el Maestro era él. Greybox
        /// honesto: una túnica encapuchada de 6x8 celdas en pardo oscuro,
        /// sentada a la mesa, con dos puntos de brasa por ojos mirando al
        /// fuego. Sprite generado por código, como toda la maquinaria.
        /// </summary>
        private GameObject _maestroGo;
        private Texture2D _maestroTex;

        private void SpawnMaestroSilueta()
        {
            float celda = SimRenderer.CellWorldSize;
            const int W = 12, H = 16; // 2 px por celda: 6x8 celdas en el mundo.
            _maestroTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[W * H];
            var tunica = new Color32(54, 39, 26, 255);
            var pliegue = new Color32(92, 66, 41, 255); // luz de borde del lado del fuego: separa la silueta del muro (afinado 64f con captura).
            var ojo = new Color32(255, 176, 96, 255);
            for (int y = 0; y < H; y++)
            {
                float t = y / (float)(H - 1); // 0 = base, 1 = capucha.
                float semiancho = Mathf.Lerp(5.4f, 2.2f, t * t); // túnica acampanada, capucha en domo.
                for (int x = 0; x < W; x++)
                {
                    bool dentro = Mathf.Abs(x - 5.5f) <= semiancho;
                    if (!dentro) continue;
                    bool flancoAlFuego = x <= 3 && y > 2 && y < H - 4; // el fuego queda a su IZQUIERDA (x424-428 < mesa).
                    px[y * W + x] = flancoAlFuego ? pliegue : tunica;
                }
            }
            px[10 * W + 3] = ojo; // dos brasas por ojos, mirando al fuego.
            px[10 * W + 5] = ojo;
            _maestroTex.SetPixels32(px);
            _maestroTex.Apply(false, true);

            _maestroGo = new GameObject("Maestro_Silueta");
            var sr = _maestroGo.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(_maestroTex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), W / (6f * celda));
            sr.sortingOrder = 30; // sobre la textura del sim, bajo todo HUD (que es screen-space).
            float mx = (SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda;
            _maestroGo.transform.position = new Vector3(mx, (SimLevelBuilder.FundacionMesaTopY + 1) * celda, 0f);
        }

        private float DistAlMaestro()
        {
            float celda = SimRenderer.CellWorldSize;
            float mx = (SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda;
            float my = (SimLevelBuilder.FundacionMesaTopY + 2) * celda;
            return Vector2.Distance(_aprendiz.position, new Vector2(mx, my)) / celda;
        }

        private void TickMirar()
        {
            if (_tBeat > 1.5f && DistAlMaestro() < DistCharla)
            {
                _beat = Beat.Saludo; _tBeat = 0f;
                AbrirDialogo(() =>
                {
                    HudPermitido = true;
                    DayCycle.DespertarHudFundacion();
                    _radioObjetivo = UiStyles.S(340f);
                    _beat = Beat.Gotera; _tBeat = 0f; _pedidoEncolado = false;
                },
                    "Llegaste. Toma mi frasco. Te lo PRESTO — no lo pierdas.",
                    "Esa gotera está inundando la poza. Tráeme " + CeldasGotera + " sorbos de agua. Ya.");
            }
        }

        /// <summary>
        /// El patrón de los beats de entrega: encolar el pedido una vez,
        /// espejar el progreso con el contenido REAL del frasco, y completar
        /// por proximidad + contenido (el Maestro toma el material).
        /// </summary>
        private void TickPedidoDeEntrega(byte mat, int cantidad, System.Action completar)
        {
            if (!_pedidoEncolado)
            {
                _pedidoEncolado = true;
                string texto = _beat switch
                {
                    // (ronda 65) Órdenes CORTAS: el gesto vive en la banda de
                    // objetivo; este panel solo dice QUÉ y CUÁNTO.
                    Beat.Gotera => "Agua de la poza — tráele " + cantidad + ".",
                    Beat.Vidrio => "Vidrio del hogar encendido — tráele " + cantidad + ".",
                    _ => "Crema de arcilla y agua — tráele " + cantidad + ".",
                };
                _orders.EncolarPedidoGuiado(OrderType.Guiado, cantidad, 0, texto, targetMat: mat);
            }

            int enFrasco = _flask.GetCount(mat);
            if (_orders.ActiveOrders.Count > 0)
            {
                var o = _orders.ActiveOrders[0];
                int progreso = Mathf.Min(enFrasco, cantidad);
                if (o.Progreso != progreso && !o.Completado) o.Progreso = progreso;
            }

            if (enFrasco >= cantidad && DistAlMaestro() < DistCharla)
            {
                _flask.Extraer(mat, cantidad, out _);
                if (_orders.ActiveOrders.Count > 0) _orders.ActiveOrders[0].Completado = true;
                completar();
            }
        }

        private void CompletarGotera()
        {
            _beat = Beat.Barro; _tBeat = 0f; _pedidoEncolado = false;
            _goteraActiva = false;

            // LA GOTERA DOMESTICADA: nace TU GRIFO sobre la poza. Ración 24
            // (ronda 64: la poza aguanta ~42 celdas; la ración vieja de 45 la
            // desbordaba sola -- regla 52, la geometría define a la fuente; y
            // el rezumado del fondo absorbe cualquier exceso). Montado a +8
            // (directiva Opus #4.2: a +12 levitaba con 10 celdas de aire
            // entre el pico y el agua).
            var go = new GameObject("Grifo_Fundacion");
            var grifo = go.AddComponent<Dispenser>();
            grifo.Init(_sim, _aprendiz, SimLevelBuilder.FundacionCharcoX0 + 2, SimLevelBuilder.FundacionY0 + 8,
                MaterialId.Water, _orders, 0, false, 5, racionCeldas: 24);

            AbrirDialogo(() => VerterDelMaestro(ArcillaPolvo, ArcillaEntregada, SimLevelBuilder.FundacionDropArcillaX),
                "La gotera ya es grifo. Sobre la poza. Se abre con E.",
                "Ahora, barro: junto a la poza cae tu arcilla. Viértela en el agua y tráeme " + CeldasBarro + " de crema.");
            _radioObjetivo = UiStyles.S(480f);
        }

        private void CompletarBarro()
        {
            _beat = Beat.Fogon; _tBeat = 0f; _pedidoEncolado = false;
            _flask.Guardar(Turba, TurbaPrestada, 70);

            AbrirDialogo(null,
                "Sirve. Toma turba: ya está en tu frasco. Hay más en la VETA del muro — se talla con C.",
                "Carga el hogar vacío con " + CeldasFogon + " de turba. El fuego lo pongo yo. Una sola vez.");
            _radioObjetivo = UiStyles.S(560f);
        }

        private void TickFogon()
        {
            if (!_pedidoEncolado)
            {
                _pedidoEncolado = true;
                _orders.EncolarPedidoGuiado(OrderType.Guiado, CeldasFogon, 0,
                    "Turba al hogar vacío — " + CeldasFogon + ".",
                    targetMat: Turba);
            }

            int enFogon = ContarEnFogon(Turba, out _, out _);
            if (_orders.ActiveOrders.Count > 0)
            {
                var o = _orders.ActiveOrders[0];
                int progreso = Mathf.Min(enFogon, CeldasFogon);
                if (o.Progreso != progreso && !o.Completado) o.Progreso = progreso;
            }

            if (enFogon >= CeldasFogon)
            {
                if (_orders.ActiveOrders.Count > 0) _orders.ActiveOrders[0].Completado = true;
                _fogonEncendido = true;
                _beat = Beat.Vidrio; _tBeat = 0f; _pedidoEncolado = false;
                AbrirDialogo(() => VerterDelMaestro(ArenaSilice, ArenaEntregada, SimLevelBuilder.FundacionDropArenaX),
                    "Arde. La turba deja CENIZA — no la tires.",
                    "Arena y ceniza JUNTAS en el fuego dan VIDRIO. Ahí cae tu arena. Tráeme " + CeldasVidrio + ".");
                _radioObjetivo = UiStyles.S(650f);
            }
        }

        private void CompletarVidrio()
        {
            _beat = Beat.Estante; _tBeat = 0f;
            _orders.LimpiarPedidos();

            var go = new GameObject("Estante_Fundacion");
            var estante = go.AddComponent<StorageRack>();
            estante.Init(_sim, _flask, _saber, _aprendiz,
                SimLevelBuilder.FundacionEstanteX0, SimLevelBuilder.FundacionEstanteX1,
                SimLevelBuilder.FundacionEstanteBaseY, visible: true, numRedomas: 3);

            AbrirDialogo(null,
                "Vidrio. Aceptable.",
                "Ese estante junto a mi mesa es tuyo. Guarda ahí lo que valga.");
        }

        /// <summary>Beat 6: un respiro para ver el estante, y el cierre -- que ABRE la economía (F2): el tablón queda vivo y el greybox gana final abierto.</summary>
        private void TickEstante()
        {
            if (_tBeat < 9f) return;
            _beat = Beat.Fin; _tBeat = 0f;
            AbrirDialogo(() =>
            {
                Trueque.Activar();
                _radioObjetivo = UiStyles.S(2400f); // amanece.
            },
                "Agua, barro, fuego, vidrio. Empiezas a servir.",
                "Mi TABLÓN queda abierto, junto a la mesa. Lo que produzcas, lo cambio. Lo que pidas, tarda.");
        }

        // ------------------------------------------------------------------
        // El diálogo por páginas (ronda 64).
        // ------------------------------------------------------------------
        private void AbrirDialogo(System.Action alCerrar, params string[] paginas)
        {
            _paginas = paginas;
            _pagina = 0;
            _alCerrar = alCerrar;
            // Bloquea TODO el input del mundo (movimiento, aspirar, atajos):
            // regla 12 -- todos los consumidores ya respetan esta bandera, y
            // se queda alta un frame extra al bajar, así que el clic que
            // cierra la última página jamás aspira nada.
            UiStyles.EscribiendoTexto = true;
        }

        private void TickDialogo()
        {
            if (DayCycle.InputLocked) return; // con el menú de pausa abierto, el diálogo espera.

            var mouse = Mouse.current;
            var kb = Keyboard.current;
            bool avanzar = (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame));
            if (!avanzar) return;

            _pagina++;
            if (_pagina >= _paginas.Length)
            {
                _paginas = null;
                UiStyles.EscribiendoTexto = false;
                var accion = _alCerrar;
                _alCerrar = null;
                accion?.Invoke();
            }
        }

        // ------------------------------------------------------------------
        // El OBJETIVO persistente (ronda 64): el gesto exacto, siempre a la
        // vista, hasta que el beat se completa. A prueba de burros.
        // ------------------------------------------------------------------
        /// <summary>
        /// (directiva Opus #6) PATRÓN ÚNICO: `GESTO — objeto · n/m`. El gesto
        /// SIEMPRE primero, en dorado y negrita (richText del estilo), para
        /// que se lea como botón y el jugador aprenda dónde mirar. El estado
        /// del barro va en orden PEDAGÓGICO: primero enseña el clic derecho
        /// (verter), no la aspiración de una crema que aún no existe.
        /// </summary>
        private static string Gesto(string gesto) => "<color=#FFD159><b>" + gesto + "</b></color>";

        private string TextoObjetivo()
        {
            switch (_beat)
            {
                case Beat.Mirar:
                    return _tBeat > 1.2f ? Gesto("WASD / FLECHAS") + " — ve con el Maestro, junto al fuego" : null;
                case Beat.Gotera:
                {
                    int n = _flask.GetCount(MaterialId.Water);
                    if (n >= CeldasGotera) return Gesto("ACÉRCATE AL MAESTRO") + " — entrégale el agua";
                    return Gesto("MANTÉN CLIC IZQUIERDO") + " — aspira el agua de la poza · " + n + "/" + CeldasGotera;
                }
                case Beat.Barro:
                {
                    int crema = _flask.GetCount(Barbotina);
                    if (crema >= CeldasBarro) return Gesto("ACÉRCATE AL MAESTRO") + " — entrégale la crema";
                    if (_flask.GetCount(ArcillaPolvo) > 0) return Gesto("CLIC DERECHO") + " — vierte la arcilla en el agua de la poza";
                    if (ContarEnPoza(Barbotina) > 0) return Gesto("MANTÉN CLIC IZQUIERDO") + " — aspira la crema parda · " + crema + "/" + CeldasBarro;
                    return Gesto("MANTÉN CLIC IZQUIERDO") + " — aspira la arcilla del montón, junto a la poza";
                }
                case Beat.Fogon:
                {
                    int n = Mathf.Min(ContarEnFogonSolo(Turba), CeldasFogon);
                    return Gesto("CLIC DERECHO") + " — vierte turba DENTRO del hogar vacío · " + n + "/" + CeldasFogon;
                }
                case Beat.Vidrio:
                {
                    int n = _flask.GetCount(MaterialId.VidrioVerde);
                    if (n >= CeldasVidrio) return Gesto("ACÉRCATE AL MAESTRO") + " — entrégale el vidrio";
                    if (ContarEnFogonSolo(MaterialId.VidrioVerde) > 0) return Gesto("MANTÉN CLIC IZQUIERDO") + " — aspira el vidrio del hogar · " + n + "/" + CeldasVidrio;
                    if (ContarEnFogonSolo(ArenaSilice) > 0) return Gesto("ESPERA") + " — arena + ceniza se hacen vidrio · " + n + "/" + CeldasVidrio;
                    return Gesto("CLIC DERECHO") + " — echa la arena dentro del fuego encendido";
                }
                case Beat.Estante:
                    return Gesto("CLIC DERECHO") + " — guarda algo en una redoma del estante";
                default:
                    return null;
            }
        }

        private int ContarEnFogonSolo(byte mat) => ContarEnFogon(mat, out _, out _);

        // ------------------------------------------------------------------
        // La viñeta + los paneles.
        // ------------------------------------------------------------------
        // (directiva Opus 64 #1) La noche aclara un paso: con #0d0906 el
        // aprendiz DESAPARECÍA por completo fuera del cono del fuego -- y el
        // guion lo manda justo ahí (la poza). Con #150f0b el mundo lejano
        // queda en silueta legible y la luz sigue siendo la protagonista.
        private static readonly Color VinetaExterior = new Color(0.082f, 0.059f, 0.043f, 1f);
        private static readonly Color VinetaMedia = new Color(0.125f, 0.086f, 0.052f, 1f);
        private const float VinetaSquashY = 0.82f; // óvalo, no círculo.

        /// <summary>Alfa de "cuánta luz hay" en un punto del mundo (0.25..1). Lo consume también Game/Trueque.cs.</summary>
        public static float LuzEn(Vector3 posMundo)
        {
            var inst = _instancia;
            if (inst == null || inst._radio > Screen.width + Screen.height) return 1f;
            var cam = Camera.main; if (cam == null) return 1f;
            Vector3 f = cam.WorldToScreenPoint(inst._focoActual);
            Vector3 p = cam.WorldToScreenPoint(posMundo);
            float d = Vector2.Distance(new Vector2(p.x, p.y), new Vector2(f.x, f.y));
            return Mathf.Clamp(1.3f - d / Mathf.Max(1f, inst._radio), 0.25f, 1f);
        }
        private static FundacionDirector _instancia;
        private Vector3 _focoActual;
        private void Awake() { _instancia = this; }

        /// <summary>
        /// LA BANDA DEL MAESTRO (arriba-centro, línea del bautizo): la sigue
        /// usando Game/Trueque.cs para los avisos cortos del tendero. El
        /// diálogo guiado de la fundación ya NO pasa por aquí (ronda 64:
        /// panel por páginas abajo-centro, DibujarDialogo).
        /// </summary>
        public static void DibujarBandaMaestro(string texto)
        {
            UiStyles.Preparar();
            if (_bandaTitulo == null)
            {
                _bandaTitulo = new GUIStyle(UiStyles.Titulo) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(UiStyles.S(9f)) };
                _bandaTitulo.normal.textColor = UiStyles.Oro;
                _bandaCuerpo = new GUIStyle(UiStyles.Cuerpo) { alignment = TextAnchor.UpperCenter, wordWrap = true, fontSize = Mathf.RoundToInt(UiStyles.S(11f)) };
            }
            float w = Screen.width * 0.46f;
            float x = (Screen.width - w) * 0.5f;
            float altoTexto = _bandaCuerpo.CalcHeight(new GUIContent(texto), w - UiStyles.S(24f));
            float h = UiStyles.S(19f) + altoTexto + UiStyles.S(9f);
            var r = new Rect(x, UiStyles.S(18f), w, h);

            var blanco = Texture2D.whiteTexture; var prev = GUI.color;
            GUI.color = new Color(UiStyles.Pergamino.r, UiStyles.Pergamino.g, UiStyles.Pergamino.b, 0.92f);
            GUI.DrawTexture(r, blanco);
            GUI.color = UiStyles.Laton;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), blanco);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), blanco);
            GUI.color = prev;

            GUI.Label(new Rect(r.x, r.y + UiStyles.S(3f), r.width, UiStyles.S(13f)), UiStyles.Espaciar("EL MAESTRO"), _bandaTitulo);
            GUI.Label(new Rect(r.x + UiStyles.S(12f), r.y + UiStyles.S(18f), r.width - UiStyles.S(24f), altoTexto), texto, _bandaCuerpo);
        }
        private static GUIStyle _bandaTitulo, _bandaCuerpo;

        private void OnGUI()
        {
            GUI.depth = 50; // detrás de todos los HUD (depth 0), encima del mundo.

            DibujarVineta();

            // La chapa "EL MAESTRO" sobre la mesa, atenuada con la luz local.
            // Se OCULTA con el aprendiz a <14 celdas (directiva Opus #4: de
            // cerca la etiqueta caía sobre la cabeza del jugador y el tester
            // creía que el Maestro era ÉL; a esa distancia la silueta y el
            // diálogo ya dicen quién es quién).
            float celda = SimRenderer.CellWorldSize;
            if (_beat != Beat.Fin && DistAlMaestro() >= 14f)
            {
                var ancla = new Vector3((SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda,
                    (SimLevelBuilder.FundacionMesaTopY + 11) * celda, 0f); // +11 (64f): por ENCIMA de la capucha de la silueta -- a +6 la chapa le tapaba la cabeza.
                float alfa = LuzEn(ancla) * 0.85f;
                UiStyles.PlacaMundo(ancla, "EL MAESTRO", new Color(0.92f, 0.86f, 0.7f, alfa), UiStyles.S(10f));
            }

            // (ronda 64) El cartel de la VETA: sustituye al asomo derramable.
            // Visible desde que el Maestro habla de ella hasta el amanecer.
            if (_beat == Beat.Fogon || _beat == Beat.Vidrio || _beat == Beat.Estante)
            {
                var anclaVeta = new Vector3((SimLevelBuilder.FundacionVetaX + 2) * celda,
                    (SimLevelBuilder.FundacionVetaY0 + 2) * celda, 0f);
                float alfaVeta = LuzEn(anclaVeta) * 0.9f;
                UiStyles.PlacaMundo(anclaVeta, "VETA — talla con C", new Color(0.85f, 0.72f, 0.5f, alfaVeta), UiStyles.S(9f));
            }

            if (DialogoAbierto) DibujarDialogo();
            else if (!DayCycle.InputLocked)
            {
                string objetivo = TextoObjetivo();
                if (objetivo != null) DibujarObjetivo(objetivo);
            }
        }

        /// <summary>
        /// (ronda 64) EL PANEL DE DIÁLOGO, estilo novela visual: abajo-centro,
        /// en la línea del bautizo (PanelRito: vitela + latón + cantoneras),
        /// rótulo EL MAESTRO lapidario, UNA frase corta por página, y el pie
        /// "clic -- continuar · n/m". No desaparece solo: espera al jugador.
        /// </summary>
        /// <summary>
        /// (directiva Opus #5, estructural) Los seis estilos del director se
        /// dimensionan RELATIVOS a Screen.height y se reconstruyen si la
        /// resolución cambia -- eran píxeles absolutos y en 2560x1248 el
        /// cuerpo del diálogo era el 1.8% del alto (una novela visual quiere
        /// ~2.7%); en 4K habría sido ilegible.
        /// </summary>
        private static int _stylesAlto;
        private static void PrepararEstilosPropios()
        {
            if (_dlgTitulo != null && _stylesAlto == Screen.height) return;
            _stylesAlto = Screen.height;
            float h = Screen.height;
            _dlgTitulo = new GUIStyle(UiStyles.Titulo) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(h * 0.016f) };
            _dlgTitulo.normal.textColor = UiStyles.Oro;
            // Cuerpo a la IZQUIERDA (Opus #2.3): centrado, cada página
            // arrancaba en un x distinto y el ojo tenía que reencontrar el
            // inicio cinco veces seguidas.
            _dlgCuerpo = new GUIStyle(UiStyles.Cuerpo) { alignment = TextAnchor.UpperLeft, wordWrap = true, fontSize = Mathf.RoundToInt(h * 0.027f) };
            _dlgPie = new GUIStyle(UiStyles.Cuerpo) { alignment = TextAnchor.MiddleRight, fontSize = Mathf.RoundToInt(h * 0.0155f) };
            _dlgPie.normal.textColor = new Color(0.847f, 0.784f, 0.604f, 1f); // #D8C89A (Opus #2.4: el pie no se veía).
            _objCuerpo = new GUIStyle(UiStyles.Cuerpo) { alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true, fontSize = Mathf.RoundToInt(h * 0.021f) };
        }

        private void DibujarDialogo()
        {
            UiStyles.Preparar();
            PrepararEstilosPropios();

            string texto = _paginas[Mathf.Clamp(_pagina, 0, _paginas.Length - 1)];
            float w = Mathf.Max(UiStyles.S(460f), Screen.width * 0.44f);
            float x = (Screen.width - w) * 0.5f;
            float pad = Mathf.Max(UiStyles.S(18f), w * 0.035f);
            float altoTexto = _dlgCuerpo.CalcHeight(new GUIContent(texto), w - pad * 2f);
            float h = UiStyles.S(34f) + altoTexto + UiStyles.S(30f);
            var r = new Rect(x, Screen.height - h - UiStyles.S(30f), w, h);

            UiStyles.PanelRito(r);
            GUI.Label(new Rect(r.x, r.y + UiStyles.S(7f), r.width, UiStyles.S(16f)), UiStyles.Espaciar("EL MAESTRO"), _dlgTitulo);
            UiStyles.FileteRombo(r.x + r.width * 0.5f, r.y + UiStyles.S(27f), r.width * 0.4f, UiStyles.Laton);
            GUI.Label(new Rect(r.x + pad, r.y + UiStyles.S(34f), r.width - pad * 2f, altoTexto), texto, _dlgCuerpo);
            bool ultima = _pagina >= _paginas.Length - 1;
            GUI.Label(new Rect(r.x + pad, r.yMax - UiStyles.S(26f), r.width - pad * 2f, UiStyles.S(16f)),
                "clic izquierdo — " + (ultima ? "cerrar" : "continuar") + "  ·  " + (_pagina + 1) + "/" + _paginas.Length, _dlgPie);
        }

        /// <summary>
        /// (ronda 64) LA BANDA DE OBJETIVO: fina, abajo-centro, siempre
        /// visible mientras haya tarea. El gesto exacto y el progreso.
        /// </summary>
        private void DibujarObjetivo(string texto)
        {
            UiStyles.Preparar();
            PrepararEstilosPropios();
            float w = Mathf.Max(UiStyles.S(420f), Screen.width * 0.4f);
            float x = (Screen.width - w) * 0.5f;
            float altoTexto = _objCuerpo.CalcHeight(new GUIContent(texto), w - UiStyles.S(20f));
            float h = altoTexto + UiStyles.S(12f);
            var r = new Rect(x, Screen.height - h - UiStyles.S(26f), w, h);

            // Marco de latón de 1px por los CUATRO lados (Opus #5.1): la banda
            // es de la familia del panel de diálogo, no un letrero suelto.
            var blanco = Texture2D.whiteTexture; var prev = GUI.color;
            GUI.color = new Color(UiStyles.Pergamino.r, UiStyles.Pergamino.g, UiStyles.Pergamino.b, 0.9f);
            GUI.DrawTexture(r, blanco);
            GUI.color = UiStyles.Laton;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), blanco);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), blanco);
            GUI.DrawTexture(new Rect(r.x, r.y, 1f, r.height), blanco);
            GUI.DrawTexture(new Rect(r.xMax - 1f, r.y, 1f, r.height), blanco);
            GUI.color = prev;

            GUI.Label(new Rect(r.x + UiStyles.S(10f), r.y + UiStyles.S(6f), r.width - UiStyles.S(20f), altoTexto), texto, _objCuerpo);
        }
        private static GUIStyle _dlgTitulo, _dlgCuerpo, _dlgPie, _objCuerpo;

        private void DibujarVineta()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (_vineta == null) ConstruirVineta();

            float celda = SimRenderer.CellWorldSize;
            var brasas = new Vector3((SimLevelBuilder.FundacionBrasasX0 + 2.5f) * celda,
                (SimLevelBuilder.FundacionBrasasY + 2) * celda, 0f);

            if (_radio > Screen.width + Screen.height) return; // ya amaneció.

            // Centro sesgado al fuego en el encuadre cero; después, la luz
            // SIGUE al jugador (0.65 -> 0.85, directiva Opus #1: el trayecto
            // fuego->poza es obligatorio y el aprendiz quedaba a oscuras en
            // todo su recorrido -- tu atención es tu lámpara).
            _focoActual = Vector3.Lerp(brasas, _aprendiz.position, _beat == Beat.Mirar ? 0.25f : 0.85f);
            Vector3 p = cam.WorldToScreenPoint(_focoActual);
            float cx = p.x, cy = Screen.height - p.y;

            // (ronda 64) El radio TITILA con el fuego real: _luzFuego lo
            // empujan las celdas de Fire vivas del hogar (MantenerFuegoDelMaestro).
            // Cero glow pintado: la luz tiene causa física o no existe.
            float r = _radio * _luzFuego;
            float ry = r * VinetaSquashY;
            var agujero = new Rect(cx - r, cy - ry, r * 2f, ry * 2f);
            GUI.DrawTexture(agujero, _vineta);

            // Los cuatro rectángulos que completan la noche: PARDOS (#0d0906)
            // y con 2px de solape sobre el óvalo (el blackTexture-alfa-cero
            // ya cayó en la 62a; la costura de redondeo, en la 62b).
            var blanco = Texture2D.whiteTexture;
            var prev = GUI.color; GUI.color = VinetaExterior;
            float ov = 2f;
            if (agujero.yMin > -ov) GUI.DrawTexture(new Rect(0, 0, Screen.width, agujero.yMin + ov), blanco);
            if (agujero.yMax < Screen.height + ov) GUI.DrawTexture(new Rect(0, agujero.yMax - ov, Screen.width, Screen.height - agujero.yMax + ov), blanco);
            if (agujero.xMin > -ov) GUI.DrawTexture(new Rect(0, agujero.yMin, agujero.xMin + ov, agujero.height), blanco);
            if (agujero.xMax < Screen.width + ov) GUI.DrawTexture(new Rect(agujero.xMax - ov, agujero.yMin, Screen.width - agujero.xMax + ov, agujero.height), blanco);
            GUI.color = prev;
        }

        private void ConstruirVineta()
        {
            // Dos paradas CÁLIDAS: transparente hasta 0.42, pardo medio
            // #1a1109 en la transición, pardo exterior #0d0906 pleno desde
            // 0.78 -- caída corta: la noche abraza, no ahoga.
            const int N = 256;
            _vineta = new Texture2D(N, N, TextureFormat.RGBA32, false);
            _vineta.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[N * N];
            float half = N * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.78f, d));
                    float tCol = Mathf.InverseLerp(0.42f, 1f, d);
                    Color c = Color.Lerp(VinetaMedia, VinetaExterior, tCol);
                    px[y * N + x] = new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(a * 255f));
                }
            }
            _vineta.SetPixels32(px);
            _vineta.Apply(false, true);
        }
    }
}
