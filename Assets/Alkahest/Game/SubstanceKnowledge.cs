using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>Transformaciones "notables" que el aprendiz puede haber presenciado, una por SimEventType.</summary>
    [Flags]
    public enum WitnessFlags : byte
    {
        None = 0,
        Arder = 1 << 0,       // SimEventType.Ignite
        Cristalizar = 1 << 1, // SimEventType.Crystallize
        Crecer = 1 << 2,      // SimEventType.Grow
        Disolverse = 1 << 3,  // SimEventType.Dissolve
        Hervir = 1 << 4,      // SimEventType.Boil
        Congelarse = 1 << 5,  // SimEventType.Freeze
    }

    /// <summary>
    /// "Qué sabe el aprendiz" sobre cada material del universo, por
    /// materialId: si lo ha descubierto, qué nombre le ha puesto (null =
    /// "???" todavía) y qué transformaciones le ha visto sufrir.
    ///
    /// Descubrimiento: cualquier material que entre en el Frasco (sondeo de
    /// los conteos de <see cref="Flask"/> cada <see cref="FlaskPollInterval"/>
    /// segundos) O que el jugador mantenga bajo el cursor
    /// <see cref="HoverDiscoverSeconds"/> segundos seguidos.
    ///
    /// Presenciar: se consume el ring buffer de eventos notables de
    /// SimStepper cada frame (con un "lastSeenHead" propio, igual que
    /// recomienda su doc-comment), traduciendo cada SimEventType al
    /// material FUENTE del evento. Nota: "dentro de la vista de cámara" es
    /// trivialmente cierto siempre en esta build (una única cámara fija
    /// ortográfica que encuadra todo el nivel), así que no se comprueba.
    ///
    /// =====================================================================
    /// (fix playtest 10) "¿POR QUÉ BAUTIZARÍA VIVIUM SI EL JUEGO YA LE LLAMA
    /// ASÍ?" -- el reporte tenía razón: la fantasía (docs/DECISIONS.md §2/§11/
    /// §12, "no haces pociones: aprendes física alienígena... y le pones
    /// nombre") se rompía porque casi todo llegaba YA bautizado por el
    /// juego. Aquí se reparte el roster en dos clases y <see cref="NombreComun"/>
    /// es la ÚNICA fuente de verdad de a cuál pertenece cada material (null
    /// = lo innominado, hay que descubrirlo y bautizarlo).
    ///
    ///   VOCABULARIO DEL TALLER (nombre común desde la celda 1, el Maestro ya
    ///   os lo enseñó -- nadie bautiza el agua):
    ///     Stone     "piedra"     -- arquitectura del taller (muros/cubas), no es una sustancia que se descubra.
    ///     Sand      "arena"      -- grifo del banco.
    ///     Water     "agua"       -- grifo del banco.
    ///     Oil       "aceite"     -- grifo del banco.
    ///     Nutrient  "nutriente"  -- grifo del banco.
    ///     Steam     "vapor"      -- fase mundana del agua al hervir, cualquiera la reconoce.
    ///     Smoke     "humo"       -- fase mundana de la combustión, cualquiera la reconoce.
    ///     Fire      "fuego"      -- fenómeno mundano, cualquiera lo reconoce (aunque en este taller se FABRIQUE, no se compre).
    ///     Ash       "ceniza"     -- residuo mundano de arder, cualquiera lo reconoce.
    ///     Ice       "hielo"      -- fase mundana del agua al congelarse, cualquiera lo reconoce.
    ///
    ///   LO INNOMINADO (NombreComun devuelve null -&gt; "???" hasta bautizar;
    ///   nadie, ni el Maestro, tiene un nombre para esto):
    ///     Azoth        -- líquido de la reserva del Maestro: SÍ sale de un grifo (el quinto, sellado
    ///                     hasta la jornada 2), pero es justo lo que "él tampoco sabe qué es" -- por
    ///                     eso lo guarda aparte y lo suelta como muestra, no como básico de taller.
    ///     CrystalSeed  -- semilla rara que el Maestro os entrega SIN nombre (ver Game/MasterSupplies.cs):
    ///                     es la definición misma de "lo innominado" del diseño.
    ///     Crystal      -- producto de una reacción exótica de este universo (Azoth+semilla en frío,
    ///                     ver Sim/Universe.cs), nadie lo ha clasificado todavía.
    ///     Vivium       -- semilla orgánica que el Maestro entrega sin nombre; el mínimo exigido por
    ///                     el diseño y el ejemplo textual del reporte que originó este cambio.
    ///     Slime        -- sin grifo propio; solo aparece al neutralizar Ácido con Agua (reacción de
    ///                     Sim/Universe.cs) -- un subproducto exótico de una reacción, de libro.
    ///     Acid         -- sin grifo propio; no hay fuente normal de partida en esta build (roster
    ///                     reservado para extensiones futuras/paleta dev F3) -- por eso mismo nunca
    ///                     puede ser "vocabulario de taller": nadie del taller lo usa todavía.
    ///
    ///   NOTA DE ALCANCE: Game/MasterSupplies.cs y Game/Dispenser.cs son de SOLO LECTURA en esta
    ///   ronda y siguen nombrando "Azoth" en su diálogo/chapa de grifo -- es una inconsistencia
    ///   residual conocida (ver informe de esta ronda), no un descuido: arreglarla exige tocar esos
    ///   dos archivos, fuera del alcance permitido aquí.
    /// =====================================================================
    /// </summary>
    public sealed class SubstanceKnowledge : MonoBehaviour
    {
        private const float FlaskPollInterval = 0.5f;
        private const float HoverDiscoverSeconds = 1f;

        // ---------------------------------------------------------------------------------
        // (fix playtest 9) "LEY DESCUBIERTA": el jugador reportó que llevaba horas sin
        // entender que las muestras del Maestro son SEMILLAS (catalizadores que no se
        // gastan), no ingredientes. El malentendido es invisible porque las dos leyes que
        // lo desmentirían (Azoth->Cristal sin gastar el cristal; Vivium creciendo sin
        // gastarse) ocurren en celdas diminutas, lejos del cursor, sin ningún aviso: la
        // primera vez que pasan de verdad, nadie las ve pasar. Aquí se anuncian UNA vez
        // cada una, la primera vez que el ring buffer de SimStepper reporta el evento
        // correspondiente (Crystallize/Grow), con la frase ejecutable de la ley, no una
        // descripción poética. Cola de 2 (como mucho hay 2 leyes de este tipo en el
        // roster fijo) para que si ambas se disparan el mismo tick no se pisen.
        // ---------------------------------------------------------------------------------
        private const float LeyBannerDuracionSeg = 7f;
        private const int LeyBannerCapacidad = 2;

        private AlkahestSim _sim;
        private Flask _flask;

        private readonly bool[] _discovered = new bool[MaterialId.Count];
        private readonly string[] _playerName = new string[MaterialId.Count];
        private readonly WitnessFlags[] _witness = new WitnessFlags[MaterialId.Count];

        private float _flaskPollTimer;
        private int _lastEventHead;

        private byte _hoverMatId;
        private float _hoverTimer;

        // Cuántas veces se ha bautizado/renombrado algo (nunca decrece): JournalHud lo usa
        // como parte de su firma de "¿hay que reconstruir el texto cacheado?" -- CountNamed()
        // por sí solo no detecta un RE-bautizo (mismo material, nombre nuevo), porque el
        // conteo de nombrados no cambia.
        public int NamingVersion { get; private set; }

        // Estado de la ley "descubierta" (ver doc arriba): dos banderas de una sola vez
        // (Cristalizar, Crecer) más una cola FIFO fija de textos ya construidos (se
        // construyen una única vez, al disparar el evento -- nunca en OnGUI/Update).
        // _leyBannerColaMat es paralelo a _leyBannerCola: guarda el matId PRODUCTO de
        // cada ley (Crystal / Vivium) para poder encadenar el aviso de bautizo justo
        // cuando el banner de esa ley termina (fix playtest 10, ver
        // DispararAvisoBautizoTrasLey).
        private bool _leyCristalDescubierta;
        private bool _leyVivumDescubierta;
        private readonly string[] _leyBannerCola = new string[LeyBannerCapacidad];
        private readonly byte[] _leyBannerColaMat = new byte[LeyBannerCapacidad];
        private int _leyBannerColaCount;
        private int _leyBannerColaLeidos;
        private string _leyBannerActual;
        private byte _leyBannerActualMat;
        private float _leyBannerHasta;

        // ---------------------------------------------------------------------------------
        // (fix playtest 10) INVITACIÓN A BAUTIZAR: "esto no tiene nombre — T para
        // bautizarlo". Mismo criterio de "avisar y luego callar" que ya usa
        // Game/DeliveryChute.cs con su chatarra (_scrapWarned: un bool por material, el
        // aviso completo sale UNA vez por material y no vuelve a repetirse para ESE
        // material) -- aquí con el mismo patrón: un aviso por cada sustancia innominada
        // distinta que el jugador llegue a apuntar/cargar, nunca más de una vez cada
        // una. Con 6 sustancias innominadas en el roster (ver tabla de arriba) esto da,
        // de forma natural, "las primeras veces y calla" sin inventar un contador nuevo.
        // Se dispara desde dos sitios: ActualizarAvisoBautizo (apuntar/cargar) y
        // DispararAvisoBautizoTrasLey (encadenado al final del banner "LEY
        // DESCUBIERTA", para que descubrir la ley y la invitación a nombrar su
        // producto se sientan un mismo momento, no dos).
        // ---------------------------------------------------------------------------------
        private const float AvisoBautizoDuracionSeg = 3.5f;
        private readonly bool[] _avisoBautizoMostrado = new bool[MaterialId.Count];
        private float _avisoBautizoHasta;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Flask flask)
        {
            _sim = sim;
            _flask = flask;
        }

        public bool EsDescubierto(byte matId) => matId < MaterialId.Count && _discovered[matId];

        /// <summary>Nombre puesto por el jugador, o "???" si todavía no se ha bautizado (o el id es inválido).</summary>
        public string NombreDe(byte matId)
        {
            if (matId >= MaterialId.Count) return "???";
            return _playerName[matId] ?? "???";
        }

        /// <summary>
        /// Nombre "de taller" en español de los materiales MUNDANOS -- los que
        /// salen de los grifos básicos o son fenómenos que cualquiera reconoce
        /// (ver la tabla de clasificación completa en el doc-comment de la
        /// clase). El Maestro ya los tiene catalogados, así que mostrarlos no
        /// rompe la fantasía de descubrir.
        ///
        /// Devuelve null para TODO lo innominado (Slime, Azoth, CrystalSeed,
        /// Crystal, Vivium, Acid): esas hay que descubrirlas y bautizarlas --
        /// es el contrato que usa el resto del HUD (frasco, redomas, diario,
        /// tolva, encargos) para decidir cuándo enseñar "???".
        ///
        /// (fix playtest 10) Azoth y CrystalSeed SALÍAN de aquí antes ("el
        /// Maestro os las entrega en mano, así que ya las conocéis"): el
        /// reporte que motiva este cambio señaló justo el problema de fondo
        /// -- si todo llega bautizado, bautizar es un trámite vacío. El
        /// Maestro las entrega precisamente PORQUE tampoco sabe qué son (ver
        /// MasterSupplies.TextoEntrega); aquí vuelven a ser innominadas.
        ///
        /// Existe para que la UI NUNCA enseñe los devName internos en inglés
        /// ("Water", "Nutrient"), que era lo que hacían el HUD del frasco y los
        /// rótulos de los grifos.
        /// </summary>
        public static string NombreComun(byte matId)
        {
            switch (matId)
            {
                case MaterialId.Stone: return "piedra";
                case MaterialId.Sand: return "arena";
                case MaterialId.Water: return "agua";
                case MaterialId.Oil: return "aceite";
                case MaterialId.Nutrient: return "nutriente";
                case MaterialId.Steam: return "vapor";
                case MaterialId.Smoke: return "humo";
                case MaterialId.Fire: return "fuego";
                case MaterialId.Ash: return "ceniza";
                case MaterialId.Ice: return "hielo";
                default: return null; // lo innominado: Slime, Azoth, CrystalSeed, Vivium, Crystal, Acid.
            }
        }

        /// <summary>Nombre para los HUD: el que le puso el jugador &gt; el común de taller &gt; "???".</summary>
        public string NombreParaHud(byte matId)
        {
            if (matId >= MaterialId.Count) return "???";
            string propio = _playerName[matId];
            if (!string.IsNullOrEmpty(propio)) return propio;
            return NombreComun(matId) ?? "???";
        }

        /// <summary>
        /// True si `matId` es de "lo innominado" (ver tabla de clasificación
        /// en el doc-comment de la clase) Y todavía nadie le ha puesto
        /// nombre -- es decir, si de verdad hace falta invitar a bautizarlo
        /// (fix playtest 10, invitación "T para bautizarlo" de
        /// <see cref="ActualizarAvisoBautizo"/>). Falso para el vocabulario
        /// del taller (nunca hace falta bautizar el agua) y falso en cuanto
        /// ya tiene nombre propio.
        /// </summary>
        private bool NecesitaBautizo(byte matId)
        {
            if (matId == MaterialId.Empty || matId >= MaterialId.Count) return false;
            if (NombreComun(matId) != null) return false;
            return string.IsNullOrEmpty(_playerName[matId]);
        }

        /// <summary>Pone/quita el nombre de un material. Nombre vacío o solo espacios equivale a "olvidarlo" (vuelve a mostrar "???").</summary>
        public void Bautizar(byte matId, string nombre)
        {
            if (matId >= MaterialId.Count) return;
            _playerName[matId] = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
            _discovered[matId] = true; // bautizar implica conocerlo.
            NamingVersion++; // ver doc de la clase: JournalHud detecta re-bautizos con esto.
        }

        /// <summary>
        /// Nombre "de ley": como NombreParaHud, pero con una descripción de
        /// respaldo (nunca "???") para materiales aún sin bautizar -- las
        /// leyes del diario/el aviso de descubrimiento tienen que leerse
        /// aunque el jugador no haya puesto nombre todavía.
        ///
        /// (fix playtest 10) `respaldo` YA NO puede ser el nombre genérico
        /// interno ("azoth", "semilla de cristal"...): con la reclasificación
        /// de arriba esos materiales son innominados de verdad, así que
        /// escribir su identidad aquí sería la MISMA circularidad que se
        /// acaba de arreglar en las pistas (un texto dice "Vivium" y el HUD
        /// enseña "???" al lado). El llamante pasa una descripción de
        /// ORIGEN/efecto en su lugar (ver ConstruirTextoLey*): esta función
        /// solo decide EN QUÉ ORDEN de prioridad se usa (nombre propio &gt;
        /// nombre común de taller -- ninguno de los dos es circular -- &gt;
        /// descripción de respaldo).
        /// </summary>
        private string NombreLey(byte matId, string respaldo)
        {
            if (matId >= MaterialId.Count) return respaldo;
            string propio = _playerName[matId];
            if (!string.IsNullOrEmpty(propio)) return propio;
            return NombreComun(matId) ?? respaldo;
        }

        public WitnessFlags WitnessOf(byte matId) => matId < MaterialId.Count ? _witness[matId] : WitnessFlags.None;

        public bool Vio(byte matId, WitnessFlags flag) => (WitnessOf(matId) & flag) != 0;

        public int CountDiscovered()
        {
            int n = 0;
            for (int m = 1; m < MaterialId.Count; m++) if (_discovered[m]) n++;
            return n;
        }

        public int CountNamed()
        {
            int n = 0;
            for (int m = 1; m < MaterialId.Count; m++) if (_playerName[m] != null) n++;
            return n;
        }

        private void Update()
        {
            if (_sim == null || _sim.Stepper == null) return;

            PollFlask();
            PollHover();
            ConsumeEvents();
            ActualizarBannerLey();
            if (!DayCycle.InputLocked) ActualizarAvisoBautizo();
        }

        /// <summary>
        /// (fix playtest 10) Si lo que el jugador tiene apuntado o cargado en el frasco
        /// ahora mismo (mismo criterio que <see cref="NamingUi.ResolveTarget"/>: cursor
        /// primero, frasco de respaldo -- así el aviso siempre coincide con lo que T
        /// abriría) es innominado y sin bautizar, y es la PRIMERA vez que esta sustancia
        /// concreta dispara el aviso, lo enciende. Nunca construye el texto aquí (es
        /// literal, ver DrawAvisoBautizo): solo decide CUÁNDO mostrarlo.
        /// </summary>
        private void ActualizarAvisoBautizo()
        {
            byte objetivo = NamingUi.ResolveTarget(_sim, _flask);
            if (objetivo == MaterialId.Empty) return;
            if (!NecesitaBautizo(objetivo)) return;
            if (_avisoBautizoMostrado[objetivo]) return;

            _avisoBautizoMostrado[objetivo] = true;
            _avisoBautizoHasta = Time.time + AvisoBautizoDuracionSeg;
        }

        /// <summary>
        /// (fix playtest 10) "Descubrir la ley y nombrar a su producto deberían
        /// encadenarse de forma natural": se llama justo cuando el banner "LEY
        /// DESCUBIERTA" de <paramref name="matId"/> termina de mostrarse. Si ese
        /// material sigue innominado, encadena la invitación a bautizar (respetando el
        /// mismo "una vez por material" que el resto del sistema -- si el jugador ya lo
        /// vio por otra vía, p.ej. lo apuntó mientras esperaba el banner, no se repite).
        /// </summary>
        private void DispararAvisoBautizoTrasLey(byte matId)
        {
            if (!NecesitaBautizo(matId)) return;
            if (_avisoBautizoMostrado[matId]) return;

            _avisoBautizoMostrado[matId] = true;
            _avisoBautizoHasta = Time.time + AvisoBautizoDuracionSeg;
        }

        private void PollFlask()
        {
            if (_flask == null) return;

            _flaskPollTimer += Time.deltaTime;
            if (_flaskPollTimer < FlaskPollInterval) return;
            _flaskPollTimer = 0f;

            for (int m = 1; m < MaterialId.Count; m++)
            {
                if (_discovered[m]) continue;
                if (_flask.GetCount((byte)m) > 0) _discovered[m] = true;
            }
        }

        private void PollHover()
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null || _sim == null)
            {
                _hoverTimer = 0f;
                return;
            }

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter))
            {
                _hoverTimer = 0f;
                return;
            }

            Vector3 world = ray.GetPoint(enter);
            Vector2Int cell = _sim.WorldToCell(world);
            if (!CellGrid.InBounds(cell.x, cell.y))
            {
                _hoverTimer = 0f;
                return;
            }

            byte matId = (byte)_sim.SampleMaterial(cell.x, cell.y);
            if (matId == MaterialId.Empty || matId == MaterialId.Stone)
            {
                _hoverTimer = 0f;
                return;
            }

            if (matId != _hoverMatId)
            {
                _hoverMatId = matId;
                _hoverTimer = 0f;
            }

            _hoverTimer += Time.deltaTime;
            if (_hoverTimer >= HoverDiscoverSeconds)
            {
                _discovered[matId] = true;
            }
        }

        private void ConsumeEvents()
        {
            var stepper = _sim.Stepper;
            var events = stepper.Events;
            int head = stepper.EventHead;

            int i = _lastEventHead;
            int steps = 0;
            while (i != head && steps < SimStepper.EventBufferSize)
            {
                var e = events[i];
                ApplyWitness(e.type, e.matId);
                i = (i + 1) & (SimStepper.EventBufferSize - 1);
                steps++;
            }
            _lastEventHead = head;
        }

        private void ApplyWitness(SimEventType type, byte matId)
        {
            if (matId >= MaterialId.Count) return;

            WitnessFlags flag;
            switch (type)
            {
                case SimEventType.Ignite: flag = WitnessFlags.Arder; break;
                case SimEventType.Boil: flag = WitnessFlags.Hervir; break;
                case SimEventType.Freeze: flag = WitnessFlags.Congelarse; break;
                case SimEventType.Crystallize: flag = WitnessFlags.Cristalizar; break;
                case SimEventType.Grow: flag = WitnessFlags.Crecer; break;
                case SimEventType.Dissolve: flag = WitnessFlags.Disolverse; break;
                default: return;
            }

            _witness[matId] |= flag;

            // (fix playtest 9) Disparo de "LEY DESCUBIERTA": solo la PRIMERA vez que cada
            // una de las dos leyes de multiplicación ocurre de verdad en la sim. matId de
            // Crystallize es SIEMPRE Azoth (ver SimStepper.NotifyReactionEvent) y el de
            // Grow SIEMPRE Vivium (ver SimStepper.GrowthTick) -- no hace falta comprobarlo,
            // pero se deja explícito por claridad.
            if (type == SimEventType.Crystallize && !_leyCristalDescubierta)
            {
                _leyCristalDescubierta = true;
                EncolarLeyBanner(ConstruirTextoLeyCristal(), MaterialId.Crystal);
            }
            else if (type == SimEventType.Grow && !_leyVivumDescubierta)
            {
                _leyVivumDescubierta = true;
                EncolarLeyBanner(ConstruirTextoLeyVivium(), MaterialId.Vivium);
            }
        }

        private void EncolarLeyBanner(string texto, byte matId)
        {
            if (_leyBannerColaCount >= LeyBannerCapacidad) return; // defensivo: no debería pasar (solo 2 leyes de este tipo existen).
            _leyBannerCola[_leyBannerColaCount] = texto;
            _leyBannerColaMat[_leyBannerColaCount] = matId;
            _leyBannerColaCount++;
        }

        /// <summary>
        /// Construido UNA vez, al disparar el evento (ver ApplyWitness) -- nunca en
        /// Update/OnGUI. Usa los nombres bautizados o comunes de taller si los hay
        /// (ver NombreLey); si no, una descripción de ORIGEN -- nunca la identidad
        /// interna del material (fix playtest 10: eso sería la misma circularidad
        /// que rompía las pistas, ver doc de NombreLey).
        /// </summary>
        private string ConstruirTextoLeyCristal()
        {
            string azoth = NombreLey(MaterialId.Azoth, "el líquido reservado del Maestro");
            string semilla = NombreLey(MaterialId.CrystalSeed, "su semilla sin nombre");
            string cristal = NombreLey(MaterialId.Crystal, "la piedra que nace del frío");
            // "que ya haya cuajado" en vez de un adjetivo con género (crecido/a): el
            // nombre bautizado puede ser cualquier palabra y no sabemos su género.
            return $"LEY DESCUBIERTA — En frío, {azoth} se vuelve {cristal} al tocar {semilla} o al tocar {cristal} que ya haya cuajado. " +
                   "La semilla no se gasta: siembra una y aliméntala.";
        }

        private string ConstruirTextoLeyVivium()
        {
            string vivium = MayusculaInicial(NombreLey(MaterialId.Vivium, "lo vivo que os dejó el Maestro"));
            string nutriente = NombreLey(MaterialId.Nutrient, "nutriente");
            return $"LEY DESCUBIERTA — {vivium}, asentado, con {nutriente} al lado y calor TEMPLADO, crea más de sí mismo. " +
                   "No se consume: cada célula que nace es otra semilla.";
        }

        private static string MayusculaInicial(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>Avanza la cola FIFO de leyes pendientes cuando la actual caduca o no hay ninguna mostrándose todavía.</summary>
        private void ActualizarBannerLey()
        {
            if (_leyBannerActual != null)
            {
                if (Time.time < _leyBannerHasta) return;
                // (fix playtest 10) El banner que acaba de terminar YA enseñó la ley:
                // este es el momento justo para encadenar "¿cómo lo llamáis?" -- ver
                // doc de DispararAvisoBautizoTrasLey.
                DispararAvisoBautizoTrasLey(_leyBannerActualMat);
                _leyBannerActual = null;
            }
            if (_leyBannerColaLeidos >= _leyBannerColaCount) return;

            _leyBannerActual = _leyBannerCola[_leyBannerColaLeidos];
            _leyBannerActualMat = _leyBannerColaMat[_leyBannerColaLeidos];
            _leyBannerColaLeidos++;
            _leyBannerHasta = Time.time + LeyBannerDuracionSeg;
        }

        private void OnGUI()
        {
            if (DayCycle.InputLocked) return;

            bool hayLey = _leyBannerActual != null;
            // El aviso de bautizo nunca compite por atención con el banner de ley (que
            // ya cubre el mismo centro-superior de pantalla) ni con el campo de texto
            // abierto -- reaparecerá solo (DispararAvisoBautizoTrasLey) en cuanto el
            // banner de ley termine, si sigue haciendo falta.
            bool hayAviso = !hayLey && !UiStyles.EscribiendoTexto && Time.time < _avisoBautizoHasta;
            if (!hayLey && !hayAviso) return;

            UiStyles.Preparar();
            if (hayLey) DrawLeyBanner();
            if (hayAviso) DrawAvisoBautizo();
        }

        /// <summary>
        /// Aviso cálido y destacado (estilo UiStyles, sin HUD permanente): título en el
        /// color de aviso del taller + cuerpo centrado con la ley en frase ejecutable.
        /// El texto ya viene cacheado (ConstruirTextoLey*): aquí solo se mide/dibuja, igual
        /// que hace Game/HintSystem.cs con su pista activa.
        /// </summary>
        private void DrawLeyBanner()
        {
            const string titulo = "LEY DESCUBIERTA";
            string texto = _leyBannerActual;

            float pad = UiStyles.S(14f);
            float acento = UiStyles.S(4f);
            float ancho = Mathf.Clamp(Screen.width - UiStyles.S(160f), UiStyles.S(360f), UiStyles.S(640f));
            float interior = ancho - pad * 2f - acento;

            float altoTitulo = UiStyles.Alto(UiStyles.Alerta, titulo, interior);
            float altoCuerpo = UiStyles.Alto(UiStyles.CuerpoCentrado, texto, interior);
            float alto = pad + altoTitulo + UiStyles.S(4f) + altoCuerpo + pad;

            // Zona propia del centro-superior de pantalla: por debajo del reloj/HintSystem
            // (que vive pegado a S(54f)) y por encima del área de juego habitual, para no
            // pisar ni el panel de pistas ni los encargos/frasco de los laterales.
            var panel = new Rect((Screen.width - ancho) * 0.5f, Screen.height * 0.30f, ancho, alto);
            UiStyles.Panel(panel, UiStyles.TintaFuerte, UiStyles.Oro);
            UiStyles.Rellenar(new Rect(panel.x, panel.y, acento, panel.height), UiStyles.Oro);

            GUI.Label(new Rect(panel.x + acento + pad, panel.y + pad, interior, altoTitulo), titulo, UiStyles.Alerta);
            GUI.Label(new Rect(panel.x + acento + pad, panel.yMax - pad - altoCuerpo, interior, altoCuerpo), texto, UiStyles.CuerpoCentrado);
        }

        /// <summary>Texto de la invitación: literal, no se construye nunca por frame (regla del proyecto: nada de strings en OnGUI).</summary>
        private const string TextoAvisoBautizo = "esto no tiene nombre — T para bautizarlo";

        /// <summary>
        /// (fix playtest 10) Globo breve y discreto junto al cursor, mismo widget que ya
        /// usa FlaskHud para su feedback ("frasco vacío", "demasiado lejos") -- ningún
        /// HUD nuevo, solo otro uso del mismo <see cref="UiStyles.Globo"/>.
        /// </summary>
        private void DrawAvisoBautizo()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            var gui = new Vector2(screenPos.x, Screen.height - screenPos.y - UiStyles.S(34f));
            UiStyles.Globo(gui, TextoAvisoBautizo, UiStyles.Oro);
        }

        /// <summary>Chip corto en español para una única flag de presenciado (usado por JournalHud).</summary>
        public static string ChipLabel(WitnessFlags flag)
        {
            switch (flag)
            {
                case WitnessFlags.Arder: return "arde";
                case WitnessFlags.Cristalizar: return "cristaliza";
                case WitnessFlags.Crecer: return "crece";
                case WitnessFlags.Disolverse: return "se disuelve";
                case WitnessFlags.Hervir: return "hierve";
                case WitnessFlags.Congelarse: return "se congela";
                default: return "";
            }
        }
    }
}
