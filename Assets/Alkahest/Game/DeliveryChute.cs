using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// La Tolva del Maestro: el pozo excavado en el contrafuerte de piedra del
    /// muro derecho (su geometría vive en Sim/SimLevelBuilder.BuildDeliveryNiche,
    /// que es la única fuente de verdad de dónde está la boca). Solo se CONSUME
    /// en las filas del fondo del pozo (el "sillar", <see cref="ChuteSillRows"/>
    /// filas junto al suelo de piedra) y se evalúa contra los encargos activos
    /// de <see cref="OrderSystem"/>; lo que no encaja cuenta como "chatarra" y
    /// da 1 de Favor cada <see cref="ScrapPerFavor"/> celdas, para que
    /// experimentar nunca sea del todo inútil.
    ///
    /// LA GARGANTA ARRASTRA (fix playtest 8: "aún hay algún problema para
    /// entregar sólidos en la tolva, los que resultan de combinaciones raras").
    /// CAUSA RAÍZ: consumir solo en el fondo (en vez de en las 29 filas del
    /// pozo, como antes de la ronda pasada) fue un acierto para que lo vertido
    /// se VEA CAER -- pero los materiales de arquetipo StaticSolid (Cristal,
    /// Hielo: precisamente lo que sale de "combinaciones raras" como la
    /// cristalización de Azoth o la congelación) NO tienen regla de caída en
    /// SimStepper.ProcessIfNeeded (su case está vacío a propósito, ver el fix
    /// de hielo-inyecta-frío ahí mismo): si el jugador los vierte a media
    /// altura del pozo -- y Flask.TickPour/PourMaterial pinta en el punto
    /// exacto donde apunta el cursor, a CUALQUIER altura dentro del alcance,
    /// no solo en el labio -- se quedan flotando ahí para siempre y nunca
    /// llegan al sillar. Revertir a "consumir en todo el pozo" perdería la
    /// caída visible que el jugador ya valoró en el playtest anterior.
    ///
    /// SOLUCIÓN DE DISEÑO, no parche: la Tolva es un APARATO del taller y
    /// "engullir" es su verbo propio. Que arrastre hacia su garganta todo lo
    /// que le eches -- sólido, líquido, polvo, da igual -- es coherente con la
    /// ficción (una tolva no deja las cosas flotando a mitad de tubo) y hace
    /// VISIBLE lo que pasa con cada entrega. <see cref="ArrastreTick"/> tira
    /// de toda celda no vacía del pozo un paso hacia su fila inferior si esa
    /// fila está libre, arquetipo aparte; al llegar al sillar, el consumo de
    /// siempre hace su trabajo. Es puramente posicional y determinista (nada
    /// de aleatoriedad: el orden de barrido decide todo), y vive aquí -- en
    /// Game/, que ya muta el grid vía AlkahestSim.Paint -- y NUNCA en Sim/,
    /// que debe permanecer agnóstico de la Tolva.
    ///
    /// REDISEÑO VISUAL (playtest 3: "la tolva quedó fatal, no entiendo dónde
    /// dejar las cosas"). Cuatro señales redundantes, para que se entienda de un
    /// vistazo y desde el otro extremo del taller:
    ///  1. Un HUECO REAL en la pared (no un bolsillo de 3 celdas invisible).
    ///  2. Un MARCO DORADO grueso (jambas + labio) alrededor de la boca.
    ///  3. Una FLECHA que flota sobre la boca, cabeceando hacia ella.
    ///  4. Un PULSO de alfa en el labio + rótulo con fondo oscuro; al tragar
    ///     algo, destello verde ("entrega aceptada") o ámbar ("chatarra").
    ///
    /// LIMITACIÓN: lee _sim.Grid.temp[] directamente para evaluar los encargos
    /// Hot/Cold (mismo patrón que HeatPlate/ChillStone).
    /// TODO(ChaosAlchemy): canalizar por una API de lectura del sim.
    /// </summary>
    public sealed class DeliveryChute : MonoBehaviour
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const int ScrapPerFavor = 10;

        private const float FlashSeconds = 0.5f;

        /// <summary>
        /// Cuánto tiempo se muestra el aviso "esto no cuenta para nada" la
        /// PRIMERA vez que se entrega un material que no encaja en ningún
        /// encargo (fix playtest 8, ver "Además" del reporte). Más largo que
        /// FlashSeconds a propósito: es la única vez que se lee el nombre del
        /// material, así que necesita tiempo de lectura, no solo de pulso.
        /// </summary>
        private const float ScrapEducationSeconds = 2.5f;

        // Geometría de la boca, tomada del constructor de nivel (nunca duplicada aquí).
        private const int ZoneX0 = SimLevelBuilder.ChuteMouthX0;
        private const int ZoneX1 = SimLevelBuilder.ChuteMouthX1;
        private const int ZoneY0 = SimLevelBuilder.ChuteMouthY0;
        private const int ZoneY1 = SimLevelBuilder.ChuteMouthY1;

        /// <summary>
        /// Filas del "sillar" (junto al suelo de piedra del pozo, ZoneY0 hacia
        /// arriba) donde de verdad se CONSUME. SimLevelBuilder es de solo
        /// lectura para esta tarea y no expone esta constante, así que vive
        /// aquí -- es una decisión de Game/, no de la geometría del taller.
        /// 3 filas: bastan para que el jugador vea el material posarse un
        /// instante antes de desaparecer (feedback de "esto SÍ ha llegado"),
        /// sin alargar la espera de un encargo grande. Deja
        /// ChuteMouthY1 - ChuteMouthY0 + 1 - 3 = 26 filas de pozo real donde
        /// arrastrar (22 columnas x 26 filas: barato de sobra para un tick).
        /// </summary>
        private const int ChuteSillRows = 3;
        private const int ZoneFloorY1 = ZoneY0 + ChuteSillRows - 1;

        private AlkahestSim _sim;
        private OrderSystem _orderSystem;
        private float _accumulator;
        private int _scrap;

        // Aviso educativo "una vez por material" (fix playtest 8, ver
        // "Además"): un array plano indexado por MaterialId, sin listas ni
        // asignaciones en el hot path de ConsumeTick.
        private readonly bool[] _scrapWarned = new bool[MaterialId.Count];
        private string _scrapMsg;
        private float _scrapMsgHasta;

        private SpriteRenderer _jambaIzq;
        private SpriteRenderer _jambaDer;
        private SpriteRenderer _labio;
        private SpriteRenderer _flecha;
        private Transform _flechaTr;
        private float _flechaY;

        private float _flashHasta;
        private bool _flashAceptado;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, OrderSystem orderSystem)
        {
            _sim = sim;
            _orderSystem = orderSystem;

            // El transform se ancla al CENTRO DEL LABIO de la boca: es el punto
            // al que apuntan flecha y rótulo.
            transform.position = new Vector3(
                (ZoneX0 + ZoneX1 + 1) * 0.5f * SimRenderer.CellWorldSize,
                (ZoneY1 + 1) * SimRenderer.CellWorldSize,
                0f);

            BuildVisual();
        }

        // -----------------------------------------------------------------
        // Visual: marco dorado + flecha, todo generado por código.
        // -----------------------------------------------------------------
        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;
            float bocaIzq = ZoneX0 * celda;
            float bocaDer = (ZoneX1 + 1) * celda;
            float bocaAlto = (ZoneY1 + 1 - ZoneY0) * celda;
            float centroY = (ZoneY0 * celda + (ZoneY1 + 1) * celda) * 0.5f;
            float grosor = 0.26f;

            var solido = SpriteSolido();

            // Jambas: dos pilastras doradas pegadas a los cantos de piedra de la boca.
            _jambaIzq = CrearSprite("JambaIzq", solido, 19,
                new Vector3(bocaIzq - grosor * 0.5f, centroY, 0f),
                new Vector3(grosor, bocaAlto + grosor, 1f));
            _jambaDer = CrearSprite("JambaDer", solido, 19,
                new Vector3(bocaDer + grosor * 0.5f, centroY, 0f),
                new Vector3(grosor, bocaAlto + grosor, 1f));

            // Labio: la línea que cruza la boca. Es el elemento que PULSA — marca
            // el plano exacto donde hay que soltar el material.
            _labio = CrearSprite("Labio", solido, 20,
                new Vector3((bocaIzq + bocaDer) * 0.5f, (ZoneY1 + 1) * celda, 0f),
                new Vector3(bocaDer - bocaIzq + grosor * 2f, 0.10f, 1f));

            // Flecha cabeceando sobre la boca.
            var flechaGO = new GameObject("Flecha");
            flechaGO.transform.SetParent(transform, false);
            _flechaTr = flechaGO.transform;
            _flechaY = (ZoneY1 + 1) * celda + 0.75f;
            _flechaTr.position = new Vector3((bocaIzq + bocaDer) * 0.5f, _flechaY, 0f);
            _flecha = flechaGO.AddComponent<SpriteRenderer>();
            _flecha.sprite = SpriteFlecha(0.95f);
            _flecha.sortingOrder = 21;
            _flecha.color = UiStyles.Oro;
        }

        private SpriteRenderer CrearSprite(string nombre, Sprite sprite, int orden, Vector3 posicion, Vector3 escala)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            go.transform.position = posicion;
            go.transform.localScale = escala;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = orden;
            sr.color = UiStyles.Oro;
            return sr;
        }

        private static Sprite SpriteSolido()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "ChaosAlchemyChuteTex" };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>Triángulo apuntando hacia ABAJO, dibujado a mano (sin assets).</summary>
        private static Sprite SpriteFlecha(float anchoMundo)
        {
            const int w = 24, h = 18;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "ChaosAlchemyChuteArrowTex",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                // y = 0 es la punta (abajo), y = h-1 la base (arriba).
                float mitad = (y / (float)(h - 1)) * (w * 0.5f);
                for (int x = 0; x < w; x++)
                {
                    bool dentro = Mathf.Abs(x + 0.5f - w * 0.5f) <= mitad;
                    px[y * w + x] = dentro ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);

            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w / anchoMundo);
        }

        // -----------------------------------------------------------------
        // Lógica
        // -----------------------------------------------------------------
        private void Update()
        {
            if (_sim == null || _sim.Grid == null || _orderSystem == null) return;
            if (DayCycle.InputLocked) return;

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                // Misma cadencia que el consumo (30Hz, igual que SimStepper):
                // la zona de arrastre es como mucho 22x26 = 572 celdas, así
                // que barrerla entera cada tick es barato, y hacerlo a la
                // cadencia del propio sim es lo que hace que un sólido caiga
                // "una fila por tick" igual de fluido que un polvo -- si
                // arrastrase cada 2 ticks se vería a mitad de velocidad que
                // todo lo demás en el pozo, un cojeo perceptible sin motivo
                // de rendimiento que lo justifique. Arrastrar ANTES de
                // consumir: lo que este mismo tick llega al sillar ya se
                // evalúa, en vez de esperar un tick extra sin motivo.
                ArrastreTick();
                ConsumeTick();
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            AnimarMarco();
        }

        private void AnimarMarco()
        {
            float t = Time.time;
            bool destello = t < _flashHasta;

            // Pulso lento y constante: "esto está vivo, esto espera algo".
            float pulso = 0.55f + 0.45f * Mathf.Sin(t * 3.2f);

            Color oro = UiStyles.Oro;
            Color acento = destello ? (_flashAceptado ? UiStyles.Exito : UiStyles.Aviso) : oro;

            if (_jambaIzq != null) _jambaIzq.color = destello ? acento : new Color(oro.r, oro.g, oro.b, 0.85f);
            if (_jambaDer != null) _jambaDer.color = _jambaIzq != null ? _jambaIzq.color : oro;
            if (_labio != null) _labio.color = new Color(acento.r, acento.g, acento.b, destello ? 1f : 0.35f + 0.55f * pulso);

            if (_flechaTr != null)
            {
                Vector3 p = _flechaTr.position;
                p.y = _flechaY + Mathf.Sin(t * 2.6f) * 0.16f;
                _flechaTr.position = p;
            }
            if (_flecha != null) _flecha.color = new Color(acento.r, acento.g, acento.b, 0.55f + 0.45f * pulso);
        }

        /// <summary>
        /// Consume SOLO en el sillar (ZoneY0..ZoneFloorY1, ver constante):
        /// aquí, y no en todo el pozo, es donde vive la caída visible que
        /// pidió el playtest anterior. Lo que hay más arriba lo trae
        /// <see cref="ArrastreTick"/> hasta aquí, arquetipo aparte -- por eso
        /// esta función ya no necesita distinguir sólidos de líquidos.
        /// </summary>
        private void ConsumeTick()
        {
            for (int x = ZoneX0; x <= ZoneX1; x++)
            {
                for (int y = ZoneY0; y <= ZoneFloorY1; y++)
                {
                    byte matId = (byte)_sim.SampleMaterial(x, y);
                    if (matId == MaterialId.Empty) continue;

                    // Solo la PIEDRA se ignora (es el propio nicho). Antes se
                    // ignoraba todo sólido estático, lo que hacía IMPOSIBLE
                    // entregar Cristal o Hielo — justo lo que piden los encargos
                    // de cristal y de "algo helado" de las jornadas 2 y 3.
                    if (matId == MaterialId.Stone) continue;

                    byte tempRaw = _sim.Grid.temp[CellGrid.Idx(x, y)];
                    bool matched = _orderSystem.TryDeliverCell(_sim.Universe, matId, tempRaw);
                    if (!matched)
                    {
                        _scrap++;
                        if (_scrap >= ScrapPerFavor)
                        {
                            _scrap -= ScrapPerFavor;
                            _orderSystem.AddFavor(1);
                        }

                        // (fix playtest 8, "Además") Primera vez que ESTE
                        // material concreto sale como chatarra: se lo decimos
                        // por su nombre, una sola vez -- reutilizando el mismo
                        // rótulo de mundo que ya usa el resto de la Tolva, no
                        // un sistema de mensajes nuevo. Las siguientes veces
                        // vuelve al "no encaja (chatarra)" genérico de abajo:
                        // ya lo sabe, no hace falta repetírselo cada entrega.
                        if (!_scrapWarned[matId])
                        {
                            _scrapWarned[matId] = true;
                            string nombre = _orderSystem.NombreParaMensaje(matId);
                            _scrapMsg = $"\"{nombre}\" no cuenta para ningún encargo activo -- queda como chatarra (Favor cada {ScrapPerFavor} celdas).";
                            _scrapMsgHasta = Time.time + ScrapEducationSeconds;
                        }
                    }

                    // Prioridad al verde: si en el mismo chorro entra algo que SÍ
                    // encaja, el jugador ve "aceptado" y no "chatarra".
                    if (matched) _flashAceptado = true;
                    else if (Time.time >= _flashHasta) _flashAceptado = false;
                    _flashHasta = Time.time + FlashSeconds;

                    _sim.Paint(x, y, 0, MaterialId.Empty);
                }
            }
        }

        /// <summary>
        /// LA GARGANTA ARRASTRA (fix playtest 8): dentro del pozo, tira de
        /// TODA celda no vacía una fila hacia su suelo si esa fila está
        /// libre -- sin mirar arquetipo. Es lo que hace que un sólido
        /// estático (Cristal, Hielo) vertido a media altura no se quede
        /// flotando ahí para siempre esperando una regla de caída que
        /// SimStepper nunca le va a dar (StaticSolid no tiene Move()).
        ///
        /// Recorre filas de ABAJO HACIA ARRIBA (de ZoneFloorY1+1, la primera
        /// fuera del sillar, hasta ZoneY1, el labio): así, cuando una celda
        /// baja a la fila que se acaba de procesar, esa fila ya no se vuelve
        /// a visitar este tick para ESA celda original -- ninguna celda
        /// concreta cae más de 1 fila en esta llamada. Una columna entera
        /// apilada SÍ se ve descender 1 fila de golpe (cada celda, la suya
        /// propia), que es sedimentación normal; lo que se evita es que una
        /// única celda "teletransporte" varias filas en un solo tick, que es
        /// justo lo que pasaría recorriendo de arriba hacia abajo (la celda
        /// de arriba encontraría, en la misma pasada, el hueco que acaba de
        /// dejar libre la de abajo, y seguiría cayendo sin parar).
        ///
        /// Usa CellGrid.SwapCells (mismo helper que SimStepper.Move) para
        /// mover mat+temp+aux juntos de una sola vez, y WakeChunk en origen y
        /// destino para que el chunk se despierte igual que si lo hubiera
        /// movido el propio sim. Cero asignaciones: todo son índices e ints.
        /// </summary>
        private void ArrastreTick()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper.Tick;

            for (int x = ZoneX0; x <= ZoneX1; x++)
            {
                for (int y = ZoneFloorY1 + 1; y <= ZoneY1; y++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.mat[idx] == MaterialId.Empty) continue;

                    int belowIdx = CellGrid.Idx(x, y - 1);
                    if (grid.mat[belowIdx] != MaterialId.Empty) continue;

                    grid.SwapCells(idx, belowIdx);
                    grid.WakeChunk(x, y, tick);
                    grid.WakeChunk(x, y - 1, tick);
                }
            }
        }

        // -----------------------------------------------------------------
        // Rótulo
        // -----------------------------------------------------------------
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            UiStyles.Preparar();

            string texto;
            Color color;
            if (Time.time < _scrapMsgHasta)
            {
                // (fix playtest 8) El aviso educativo "una vez por material"
                // pisa al destello normal mientras dura: es más largo a
                // propósito (ver ScrapEducationSeconds) y solo se dispara una
                // vez por material, así que merece prioridad sobre el pulso
                // genérico de aceptado/chatarra.
                texto = _scrapMsg;
                color = UiStyles.Aviso;
            }
            else if (Time.time < _flashHasta)
            {
                texto = _flashAceptado ? "¡ENTREGA ACEPTADA!" : "no encaja en ningún encargo (chatarra)";
                color = _flashAceptado ? UiStyles.Exito : UiStyles.Aviso;
            }
            else
            {
                texto = "TOLVA DEL MAESTRO — vierte AQUÍ";
                color = UiStyles.Oro;
            }

            // Anclado sobre la flecha; UiStyles recorta el globo al borde de la
            // pantalla (la boca está pegadísima al muro derecho).
            UiStyles.EtiquetaMundo(new Vector3(transform.position.x, _flechaY, 0f), texto, color, UiStyles.S(26f));
        }
    }
}
