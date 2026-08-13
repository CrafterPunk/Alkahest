using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL CINCEL (playtest 16, primer paso del "taller editable"). Cesar, dos
    /// citas que son la misma idea en dos momentos: *"he observado que en el
    /// modo creativo la piedra estilo bedrock es destructible y que los
    /// ingredientes fluyen, quizás la clave es dejar que la gente organice
    /// como quiera"*, y más tarde, literal: *"no olvides la parte de tener un
    /// cincel o algo así que permita editar el bedrock, quedará feo y sin
    /// recortar pero luego le daremos forma"*.
    ///
    /// POR QUÉ IMPORTA: ChaosAlchemy va de DOMESTICAR LEYES -- un juego con esa
    /// premisa tiene que dejarte construir tu propio aparato, no solo operar
    /// el que puso el diseñador. Hoy el taller es un escenario clavado
    /// (SimLevelBuilder es "EL PLANO", de solo lectura para el jugador); el
    /// Cincel es lo primero que empieza a convertirlo en un banco de trabajo
    /// DEL JUGADOR. Es el paso 5 de la fase acordada con Cesar tras el
    /// diagnóstico "falta morfología" (ver docs/HANDOFF.md "Playtest 14" §4-5:
    /// cámara -> taller a pantallas -> química por semilla -> comportamiento
    /// por semilla -> TALLER MOVIBLE -> mundo persistente), adelantado aquí
    /// porque tallar bedrock es barato (una sola clase, sin tocar la sim) y ya
    /// cambia la relación del jugador con el espacio -- no hace falta esperar
    /// a mover grifos para que "esto es MI taller" empiece a sentirse cierto.
    ///
    /// DISEÑO: una herramienta más que el aprendiz lleva encima, como el
    /// frasco. Dos acciones -- TALLAR (quitar piedra) y RELLENAR (poner
    /// piedra) -- pero NO se le da una tecla nueva a cada una. El frasco ya
    /// ocupa clic izq. (aspirar), clic der. (verter) y Q (vaciar), y el botón
    /// CENTRAL está reservado -- Cesar lo sugirió para "mover cosas" (paso 5
    /// del backlog, el taller movible), así que esta clase no lo toca. En vez
    /// de pelear por botones, el Cincel es un MODO: <see cref="ModoActivo"/>
    /// se alterna con una tecla y, dentro del modo, los MISMOS gestos del
    /// frasco (clic izq./der. mantenidos) cambian de significado. Así el
    /// jugador solo necesita recordar "qué llevo en la mano", no una tabla de
    /// atajos nueva.
    ///
    /// TECLA ELEGIDA: **C** (mnemónico de "Cincel"). Comprobado contra la
    /// tabla de atajos de docs/HANDOFF.md sección "Playtest 10" (M, F3/P/N,
    /// E, WASD/flechas, Enter, clics de redoma, T/ESC, J/RePág/AvPág, H, Q +
    /// clics/Shift del frasco) más el barrido de teclas usadas en el propio
    /// código (grep de *Key en Assets/Alkahest): quedaban libres, entre otras,
    /// B/C/G/I/K/L/R/U/V/X/Y/Z. Se descartó **O** a propósito: acaba de
    /// ocuparse en esta misma ronda para plegar OrdersHud (ver
    /// Game/OrdersHud.cs, kb.oKey) -- un encargo en paralelo con propiedad de
    /// archivos disjunta, así que la única red de seguridad real es comprobar
    /// el código a la vez que el HANDOFF, no fiarse solo del documento.
    ///
    /// RITMO: mismo patrón que Flask -- acumulador de Time.deltaTime a 30Hz
    /// fijos (<see cref="TickDt"/>), radio pequeño en CELDAS
    /// (<see cref="CarveRadius"/>/<see cref="FillRadius"/> = 2, contra el
    /// SuckRadius=4 del frasco) y una TASA por tick deliberadamente baja
    /// (<see cref="CarveRatePerTick"/>/<see cref="FillRatePerTick"/> = 3, con
    /// budget por tick igual que TickSuck/TickPour). El objetivo explícito de
    /// Cesar es que TALLAR se sienta como picar piedra, no como borrar con una
    /// goma gigante: un disco de radio 2 tarda varios ticks en vaciarse del
    /// todo aunque se mantenga el clic, así que recortar una forma exige
    /// pasadas, no un solo golpe.
    ///
    /// ALCANCE: literalmente <see cref="Flask.ReachWorld"/> -- es la MISMA
    /// mano del MISMO personaje, así que reutilizar la constante (no
    /// duplicarla con un número propio) es la única forma de que "hasta dónde
    /// llego" no diverja entre herramientas si algún día cambia. El aviso de
    /// "demasiado lejos" también se reutiliza literalmente: <see
    /// cref="Flask.Avisar"/> es el canal público que Game/StorageRack.cs ya
    /// usa para avisar "junto al cursor y no en su propia esquina" -- el
    /// Cincel es el segundo consumidor de ese canal, exactamente para lo que
    /// se diseñó.
    ///
    /// SOLO PIEDRA, Y NUNCA EL BORDE: TALLAR solo actúa si la celda bajo el
    /// disco es EXACTAMENTE MaterialId.Stone (nunca agua/aceite/cristal/
    /// vivium/etc. -- para borrar ESA materia ya está el frasco, con su propio
    /// filtro de aspirado); RELLENAR solo actúa sobre celdas MaterialId.Empty.
    /// Ninguna de las dos toca el marco exterior que pinta
    /// SimLevelBuilder.FillBorder (fila 0, fila H-1, columna 0, columna W-1):
    /// romperlo dejaría escapar la materia de la simulación entera. La
    /// protección es DOBLE a propósito (defensa en profundidad, no
    /// redundancia perezosa): AlkahestSim.Paint/PaintStable YA descartan ese
    /// marco internamente (comprueban px/py contra 0 y W-1/H-1 antes de
    /// escribir), pero aquí se repite la misma comprobación de forma EXPLÍCITA
    /// antes de llamar a esos métodos -- nunca hay que fiarse en silencio de
    /// un efecto colateral de OTRO archivo para una garantía tan crítica
    /// (perder el marco = el universo entero se derrama).
    ///
    /// TODA mutación de la grilla pasa por AlkahestSim (regla de oro del
    /// proyecto, y la base del netcode futuro): TALLAR usa
    /// <see cref="AlkahestSim.Paint"/> con radio 0 celda a celda (igual que
    /// Flask.TickSuck cuando aspira: precisión de una celda, presupuesto por
    /// tick), y RELLENAR usa <see cref="AlkahestSim.PaintStable"/> (regla 22
    /// de CLAUDE.md) en vez de Paint/PaintCell -- PaintStable hace nacer la
    /// celda a la temperatura en la que el material es ESTABLE, así que la
    /// piedra tallada de la nada no depende de qué hubiera antes en esa celda
    /// (el mismo motivo por el que pintar Hielo con la paleta dev dejó de
    /// fundirse solo, playtest 13). Piedra no tiene transiciones de fase, así
    /// que en la práctica nace a ambiente -- pero usar PaintStable documenta
    /// la intención ("esto nace de la nada, no hereda nada") y es gratis.
    ///
    /// LÍMITE BLANDO: NO se impide tallar el taller entero hasta dejarlo
    /// inservible -- Cesar fue explícito ("quedará feo... luego le daremos
    /// forma") y limitar el alcance ya lo hace Flask.ReachWorld (hay que
    /// volar hasta cada zona y picar rato para desmontarla). Se DEJA ANOTADO
    /// como propuesta, no implementada: si en un playtest futuro el jugador
    /// se destroza el propio taller sin darse cuenta (p.ej. talla el muro que
    /// sostiene la Tolva y el pedido deja de ser entregable), la palanca
    /// correcta no sería un límite duro sino una pista/aviso contextual del
    /// estilo de Game/HintSystem.cs, no una restricción silenciosa de esta
    /// clase.
    ///
    /// LIMITACIÓN DOCUMENTADA -- Game/Flask.cs es de SOLO LECTURA en este
    /// encargo (lista de archivos del playtest 16): el diseño ideal es que
    /// activar el Cincel apague a Flask "limpiamente", igual que Flask ya
    /// apaga sus propios visuales de mundo en sus `return` tempranos (ver
    /// Flask.OcultarVisualesDeMundo). Sin poder tocar Flask.Update() para
    /// añadirle esa guarda, esta clase NO puede desactivar al frasco de
    /// verdad: mientras <see cref="ModoActivo"/> está encendido, Flask sigue
    /// leyendo clic izq./der. en paralelo. En la práctica el solapamiento es
    /// estrecho, no total: el propio filtro de aspirado de Flask
    /// (Flask.EsAspirable) YA excluye Piedra, así que apuntar el Cincel a
    /// piedra sólida no dispara nada en Flask salvo que haya OTRO material
    /// aspirable a <= SuckRadius=4 celdas de ese punto (Flask.
    /// BloquearMaterialBajoElCursor busca en anillos crecientes si lo que hay
    /// bajo el cursor no es aspirable) -- p.ej. tallar justo al borde de una
    /// cuba con agua SÍ puede hacer que el frasco empiece a aspirar esa agua a
    /// la vez. Pendiente para una ronda futura con propiedad de Flask.cs:
    /// añadir ahí `if (Cincel.ModoActivo) { OcultarVisualesDeMundo(); return; }`
    /// justo con el resto de guardas de la regla 12 de CLAUDE.md.
    /// </summary>
    [RequireComponent(typeof(ApprenticeController))]
    public sealed class Cincel : MonoBehaviour
    {
        /// <summary>¿Lleva el aprendiz el cincel en la mano ahora mismo? Análogo a JournalHud.Abierto / Dev/DevPalette.IsOpen: un flag estático de solo lectura hacia fuera, que además ES el indicador de modo (ver doc de clase) -- si el icono/haz del cincel se ve, estás en modo cincel; si no, llevas el frasco.</summary>
        public static bool ModoActivo { get; private set; }

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;

        // Radio pequeño y tasa baja A PROPÓSITO (ver doc de clase): "picar
        // piedra", no "borrar con una goma gigante".
        private const int CarveRadius = 2;
        private const int CarveRatePerTick = 3;
        private const int FillRadius = 2;
        private const int FillRatePerTick = 3;

        private AlkahestSim _sim;
        private ApprenticeController _apprentice;
        private Flask _flask; // solo para Avisar(): el canal de feedback compartido (ver doc de clase).

        private float _accumulator;
        private bool _hasCursorWorld;
        private Vector3 _cursorWorld;
        private bool _hasCursor;
        private Vector2Int _cursorCell;

        private void Awake()
        {
            _apprentice = GetComponent<ApprenticeController>();
            _flask = GetComponent<Flask>();
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap, mismo patrón que Flask.Init.</summary>
        public void Init(AlkahestSim sim)
        {
            _sim = sim;
            BuildVisuals();
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;

            // Mismas guardas que Flask.Update, en el mismo orden, por la misma
            // razón (regla 12 de CLAUDE.md): un atajo de mundo nunca debe
            // colarse mientras el título/intro/fin de jornada tiene el input
            // bloqueado, la paleta dev manda, se está escribiendo un nombre, o
            // el diario tapa la pantalla.
            if (DayCycle.InputLocked) { OcultarVisuales(); return; }
            if (Alkahest.Dev.DevPalette.IsOpen) { OcultarVisuales(); return; }
            if (UiStyles.EscribiendoTexto) { OcultarVisuales(); return; }
            if (JournalHud.Abierto) { OcultarVisuales(); return; }

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            // Alternar modo (C). Ya pasamos las cuatro guardas de arriba, así
            // que esto cumple "todo atajo de una sola tecla comprueba
            // EscribiendoTexto, y los del MUNDO además JournalHud.Abierto"
            // (regla 12) sin repetir la comprobación.
            if (kb != null && kb.cKey.wasPressedThisFrame)
            {
                ModoActivo = !ModoActivo;
                if (_flask != null)
                {
                    _flask.Avisar(ModoActivo
                        ? "cincel en mano — clic izq. talla, clic der. rellena"
                        : "frasco en mano");
                }
                if (!ModoActivo) OcultarVisuales();
            }

            if (!ModoActivo) { OcultarVisuales(); return; } // el frasco manda: el Cincel no toca la grilla ni pinta nada.

            // Misma razón que Flask: sobre una redoma del estante los clics
            // son "guardar/recuperar", nunca una acción de mundo (si no, se
            // podría tallar piedra justo debajo del mueble sin querer).
            bool ratonCapturado = StorageRack.RatonSobreRedoma();

            bool wantCarve = mouse != null && mouse.leftButton.isPressed && !ratonCapturado;
            bool wantFill = mouse != null && mouse.rightButton.isPressed && !ratonCapturado;

            _hasCursorWorld = TryGetCursorWorld(out _cursorWorld);
            _hasCursor = _hasCursorWorld && CeldaDesdeCursorMundo(out _cursorCell);

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                if (_hasCursor)
                {
                    if (wantCarve) TallarTick(_cursorCell);
                    else if (wantFill) RellenarTick(_cursorCell);
                }
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            UpdateWorldVisuals();
        }

        /// <summary>Mismo raycast puro cámara-&gt;plano z=0 que Flask.TryGetCursorWorld (privado allí, así que se repite aquí: es de solo lectura en este encargo).</summary>
        private bool TryGetCursorWorld(out Vector3 world)
        {
            world = default;
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return false;

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter)) return false;

            world = ray.GetPoint(enter);
            return true;
        }

        private bool CeldaDesdeCursorMundo(out Vector2Int cell)
        {
            cell = _sim.WorldToCell(_cursorWorld);
            return CellGrid.InBounds(cell.x, cell.y);
        }

        private float ReachCellsSq()
        {
            float reachCells = Flask.ReachWorld / SimRenderer.CellWorldSize;
            return reachCells * reachCells;
        }

        // ---------------------------------------------------------------------------------
        // TALLAR (clic izq. mantenido en modo cincel): quita piedra, celda a celda.
        // ---------------------------------------------------------------------------------
        private void TallarTick(Vector2Int cursor)
        {
            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();

            int cdxCursor = cursor.x - apprenticeCell.x, cdyCursor = cursor.y - apprenticeCell.y;
            if (cdxCursor * cdxCursor + cdyCursor * cdyCursor > reachCellsSq)
            {
                if (_flask != null) _flask.Avisar("demasiado lejos — acércate");
                return;
            }

            int budget = CarveRatePerTick;

            // Anillos de distancia entera creciente desde el cursor, igual que
            // Flask.TickSuck: se talla primero lo más cercano al centro del disco.
            for (int r = 0; r <= CarveRadius && budget > 0; r++)
            {
                for (int dy = -r; dy <= r && budget > 0; dy++)
                {
                    int y = cursor.y + dy;
                    for (int dx = -r; dx <= r && budget > 0; dx++)
                    {
                        if (Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy)) != r) continue;

                        int x = cursor.x + dx;
                        if (!CellGrid.InBounds(x, y)) continue;

                        // Protección EXPLÍCITA del marco exterior (ver doc de
                        // clase): fila 0, fila H-1, columna 0, columna W-1 las
                        // pinta SimLevelBuilder.FillBorder y romperlas dejaría
                        // escapar la materia de toda la simulación.
                        // AlkahestSim.Paint ya las descarta por su cuenta, pero
                        // esta guarda no se deja como un efecto colateral
                        // silencioso de otro archivo.
                        if (x <= 0 || x >= CellGrid.W - 1 || y <= 0 || y >= CellGrid.H - 1) continue;

                        int cdx = x - apprenticeCell.x, cdy = y - apprenticeCell.y;
                        if (cdx * cdx + cdy * cdy > reachCellsSq) continue;

                        // SOLO piedra: nunca materia de la simulación (agua,
                        // aceite, cristal, vivium...) -- para eso está el
                        // frasco, con su propio filtro de aspirado.
                        if (_sim.SampleMaterial(x, y) != MaterialId.Stone) continue;

                        _sim.Paint(x, y, 0, MaterialId.Empty);
                        budget--;
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // RELLENAR (clic der. mantenido en modo cincel): pone piedra, solo sobre vacío.
        // ---------------------------------------------------------------------------------
        private void RellenarTick(Vector2Int cursor)
        {
            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();

            int cdxCursor = cursor.x - apprenticeCell.x, cdyCursor = cursor.y - apprenticeCell.y;
            if (cdxCursor * cdxCursor + cdyCursor * cdyCursor > reachCellsSq)
            {
                if (_flask != null) _flask.Avisar("demasiado lejos — acércate");
                return;
            }

            int budget = FillRatePerTick;

            for (int r = 0; r <= FillRadius && budget > 0; r++)
            {
                for (int dy = -r; dy <= r && budget > 0; dy++)
                {
                    int y = cursor.y + dy;
                    for (int dx = -r; dx <= r && budget > 0; dx++)
                    {
                        if (Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy)) != r) continue;

                        int x = cursor.x + dx;
                        if (!CellGrid.InBounds(x, y)) continue;

                        // Misma protección explícita del marco exterior que en TallarTick.
                        if (x <= 0 || x >= CellGrid.W - 1 || y <= 0 || y >= CellGrid.H - 1) continue;

                        int cdx = x - apprenticeCell.x, cdy = y - apprenticeCell.y;
                        if (cdx * cdx + cdy * cdy > reachCellsSq) continue;

                        // Solo sobre celdas VACÍAS: rellenar no debe poder
                        // tragarse agua/aceite/etc. -- solo cierra huecos.
                        if (_sim.SampleMaterial(x, y) != MaterialId.Empty) continue;

                        // PaintStable, no Paint/PaintCell (regla 22 de
                        // CLAUDE.md): la celda nace a la temperatura en la que
                        // Piedra es estable en vez de heredar lo que hubiera
                        // antes ahí.
                        _sim.PaintStable(x, y, 0, MaterialId.Stone);
                        budget--;
                    }
                }
            }
        }

        // ===================================================================
        // VISUAL: mismo lenguaje que el haz del frasco (latón, sprites de
        // código, sin Shader.Find) -- ver Flask.cs sección "EL HAZ". Todo
        // creado UNA vez en BuildVisuals(); Update() solo mueve/tiñe lo ya
        // creado, cero asignaciones por frame.
        //
        // Sirve DOBLE propósito: (1) retroalimentación de "qué se va a tallar
        // antes de tallarlo" -- el anillo se ve mientras el modo está activo y
        // el cursor apunta dentro de alcance, no solo mientras se hace clic
        // (a diferencia del haz del frasco, que solo existe mientras se
        // aspira/vierte de verdad); y (2) INDICADOR DE MODO sin panel
        // permanente: el proyecto acaba de encoger el HUD a propósito (playtest
        // 15/16), así que "qué llevas en la mano" se lee en si este anillo y
        // el icono junto al aprendiz están encendidos o apagados, no en un
        // rótulo fijo en pantalla.
        // ===================================================================

        private const int BeamSortingOrder = 38;  // justo por debajo del haz del frasco (40/41): si algún día coinciden, el del frasco gana.
        private const int RingSortingOrder = 39;
        private const int ModeIconSortingOrder = 61; // justo por encima del punto de contenido del frasco (60).

        private const float BeamThicknessWorld = 0.035f;
        private const float BeamAlpha = 0.30f;
        private const float RingAlpha = 0.70f;
        private const float ModeIconAlpha = 0.95f;

        // Mismos valores de latón que ya usan Flask.cs/ApprenticeController.cs
        // para el mundo (UiStyles.Oro es de UI, no de mundo) -- duplicados
        // aquí porque son campos privados de esos archivos, de solo lectura en
        // este encargo; es el MISMO lenguaje visual, no una paleta nueva.
        private static readonly Color32 BrassBase = new Color32(168, 126, 58, 255);
        private static readonly Color32 ColorAviso = new Color32(219, 84, 71, 255); // fuera de alcance.
        private static readonly Color32 ColorTallar = new Color32(196, 118, 64, 255); // apuntando a piedra: "esto sale" -- óxido/ladrillo, en la familia del latón pero más cálido.
        private static readonly Color32 ColorRellenar = new Color32(150, 150, 150, 255); // apuntando a vacío: "aquí nace piedra" -- gris piedra.
        private static readonly Color32 ColorNeutro = new Color32(120, 120, 120, 255); // ni piedra ni vacío (agua/cristal/vivium...): el cincel no actúa aquí.

        private Transform _beamRoot;
        private SpriteRenderer _beamLineSr;
        private Transform _ringTr;
        private SpriteRenderer _ringSr;
        private SpriteRenderer _modeIconSr;

        private void BuildVisuals()
        {
            var lineaSprite = CrearSpriteBlanco1x1("AlkahestCincelHazLinea", new Vector2(0f, 0.5f));

            var rootGo = new GameObject("CincelHaz");
            rootGo.transform.SetParent(transform, false);
            _beamRoot = rootGo.transform;

            var lineaGo = new GameObject("Linea");
            lineaGo.transform.SetParent(_beamRoot, false);
            _beamLineSr = lineaGo.AddComponent<SpriteRenderer>();
            _beamLineSr.sprite = lineaSprite;
            _beamLineSr.sortingOrder = BeamSortingOrder;
            _beamLineSr.color = new Color(0f, 0f, 0f, 0f);

            var ringSprite = CrearSpriteAnillo();
            var ringGo = new GameObject("Anillo");
            ringGo.transform.SetParent(transform, false);
            _ringTr = ringGo.transform;
            _ringSr = ringGo.AddComponent<SpriteRenderer>();
            _ringSr.sprite = ringSprite;
            _ringSr.sortingOrder = RingSortingOrder;
            _ringSr.color = new Color(0f, 0f, 0f, 0f);

            // Icono de modo: una esquirla de latón (un cuadrado rotado 45°,
            // "diamante" mínimo) anclada al mismo punto que Flask.CarryAnchor
            // usa para su mancha de contenido -- SIN sustituirla, coexisten
            // (Flask sigue siendo de solo lectura, ver doc de clase); el
            // icono solo se enciende en modo cincel, así que en modo frasco
            // no añade ruido visual ninguno.
            var iconoSprite = CrearSpriteBlanco1x1("AlkahestCincelIcono", new Vector2(0.5f, 0.5f));
            var iconoGo = new GameObject("CincelIconoModo");
            iconoGo.transform.SetParent(transform, false);
            iconoGo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            iconoGo.transform.localScale = Vector3.one * 0.14f;
            _modeIconSr = iconoGo.AddComponent<SpriteRenderer>();
            _modeIconSr.sprite = iconoSprite;
            _modeIconSr.sortingOrder = ModeIconSortingOrder;
            _modeIconSr.color = new Color(0f, 0f, 0f, 0f);
        }

        private static Sprite CrearSpriteBlanco1x1(string nombre, Vector2 pivot01)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = nombre, filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), pivot01, 1f);
        }

        /// <summary>Anillo suave (no relleno): comunica "este es el radio del disco" sin tapar lo que hay debajo, a diferencia de un disco sólido. Bilinear a propósito (mismo criterio que Flask.CrearSpritePulso): es un contorno difuso, no una silueta que deba leerse nítida.</summary>
        private static Sprite CrearSpriteAnillo()
        {
            const int n = 32;
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            const float targetPx = 13f;   // radio del anillo en téxeles.
            const float thicknessPx = 4.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x - c, dy = y - c;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = 1f - Mathf.Clamp01(Mathf.Abs(dist - targetPx) / thicknessPx);
                    a *= a; // caída más suave.
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "AlkahestCincelAnillo" };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        /// <summary>Apaga todos los visuales de golpe (mismo patrón que Flask.OcultarVisualesDeMundo): se llama en los `return` tempranos de Update para que nada se quede pegado en pantalla mientras el Cincel no está activo.</summary>
        private void OcultarVisuales()
        {
            if (_beamLineSr != null) _beamLineSr.color = new Color(0f, 0f, 0f, 0f);
            if (_ringSr != null) _ringSr.color = new Color(0f, 0f, 0f, 0f);
            if (_modeIconSr != null) _modeIconSr.color = new Color(0f, 0f, 0f, 0f);
        }

        private void UpdateWorldVisuals()
        {
            // El icono de modo se enciende SOLO por estar en modo cincel
            // (no depende de apuntar a nada): es el indicador permanente y
            // discreto de "qué llevas en la mano" que pide el encargo.
            if (_modeIconSr != null)
            {
                Vector3 anchor = _apprentice != null ? _apprentice.CarryAnchor : transform.position;
                Vector3 iconPos = anchor + new Vector3(0f, 0.22f, -0.03f);
                _modeIconSr.transform.position = iconPos;
                _modeIconSr.color = new Color(BrassBase.r / 255f, BrassBase.g / 255f, BrassBase.b / 255f, ModeIconAlpha);
            }

            if (!_hasCursorWorld || _apprentice == null || _beamRoot == null)
            {
                if (_beamLineSr != null) _beamLineSr.color = new Color(0f, 0f, 0f, 0f);
                if (_ringSr != null) _ringSr.color = new Color(0f, 0f, 0f, 0f);
                return;
            }

            Vector3 origen = _apprentice.CarryAnchor;
            Vector3 alcanceOrigen = transform.position;
            Vector3 delta = _cursorWorld - alcanceOrigen; delta.z = 0f;
            float distDesdeAprendiz = delta.magnitude;
            bool fueraDeAlcance = distDesdeAprendiz > Flask.ReachWorld;

            Vector3 destino = fueraDeAlcance
                ? alcanceOrigen + delta.normalized * Flask.ReachWorld
                : _cursorWorld;

            Vector3 tramo = destino - origen; tramo.z = 0f;
            float largo = tramo.magnitude;
            if (largo < 0.0005f)
            {
                if (_beamLineSr != null) _beamLineSr.color = new Color(0f, 0f, 0f, 0f);
                if (_ringSr != null) _ringSr.color = new Color(0f, 0f, 0f, 0f);
                return;
            }

            float anguloDeg = Mathf.Atan2(tramo.y, tramo.x) * Mathf.Rad2Deg;
            _beamRoot.position = origen;
            _beamRoot.rotation = Quaternion.Euler(0f, 0f, anguloDeg);
            _beamLineSr.transform.localScale = new Vector3(largo, BeamThicknessWorld, 1f);

            byte matBajoDestino = _hasCursor ? (byte)_sim.SampleMaterial(_cursorCell.x, _cursorCell.y) : MaterialId.Empty;
            Color32 colorBase = ColorDelAnillo(fueraDeAlcance, matBajoDestino);

            _beamLineSr.color = new Color(colorBase.r / 255f, colorBase.g / 255f, colorBase.b / 255f, BeamAlpha);

            // El anillo se centra en la celda apuntada (o en el borde de
            // alcance si el cursor se sale) y su tamaño refleja el radio real
            // del disco que va a tallar/rellenar (CarveRadius == FillRadius).
            _ringTr.position = destino;
            float radioMundo = (CarveRadius + 0.5f) * SimRenderer.CellWorldSize;
            _ringTr.localScale = Vector3.one * (radioMundo * 2f);
            _ringSr.color = new Color(colorBase.r / 255f, colorBase.g / 255f, colorBase.b / 255f, RingAlpha);
        }

        /// <summary>Color del anillo/haz: aviso si el cursor está fuera de alcance; si no, según lo que haya bajo el punto apuntado -- piedra (se va a TALLAR), vacío (se va a RELLENAR), o ninguno de los dos (el Cincel no actúa ahí, es la respuesta visual a "solo piedra").</summary>
        private static Color32 ColorDelAnillo(bool fueraDeAlcance, byte matBajoDestino)
        {
            if (fueraDeAlcance) return ColorAviso;
            if (matBajoDestino == MaterialId.Stone) return ColorTallar;
            if (matBajoDestino == MaterialId.Empty) return ColorRellenar;
            return ColorNeutro;
        }
    }
}
