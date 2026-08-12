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
        private bool _leyCristalDescubierta;
        private bool _leyVivumDescubierta;
        private readonly string[] _leyBannerCola = new string[LeyBannerCapacidad];
        private int _leyBannerColaCount;
        private int _leyBannerColaLeidos;
        private string _leyBannerActual;
        private float _leyBannerHasta;

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
        /// Nombre "de taller" en español de los materiales mundanos: los que
        /// salen de los grifos y sus derivados obvios. El Maestro ya los tiene
        /// catalogados, así que mostrarlos no rompe la fantasía de descubrir.
        /// Devuelve null para las sustancias exóticas (Slime, Azoth, Vivium,
        /// Cristal, Ácido...): esas hay que descubrirlas y bautizarlas.
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
                // El Maestro entrega estos dos EN MANO y por su nombre al empezar
                // la jornada 2 (ver Game/MasterSupplies.cs y la intro de jornada),
                // así que ocultarlos tras "???" en el grifo y en las redomas sería
                // absurdo. Lo verdaderamente desconocido (vivium, cristal, limo,
                // ácido) sigue sin nombre hasta que lo bauticéis.
                case MaterialId.Azoth: return "azoth";
                case MaterialId.CrystalSeed: return "semilla de cristal";
                default: return null;
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

        /// <summary>Pone/quita el nombre de un material. Nombre vacío o solo espacios equivale a "olvidarlo" (vuelve a mostrar "???").</summary>
        public void Bautizar(byte matId, string nombre)
        {
            if (matId >= MaterialId.Count) return;
            _playerName[matId] = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();
            _discovered[matId] = true; // bautizar implica conocerlo.
            NamingVersion++; // ver doc de la clase: JournalHud detecta re-bautizos con esto.
        }

        /// <summary>Nombre "de ley": como NombreParaHud, pero con un genérico en minúscula (nunca "???") para materiales aún sin bautizar -- las leyes del diario/el aviso de descubrimiento tienen que leerse aunque el jugador no haya puesto nombre todavía.</summary>
        private string NombreLey(byte matId, string generico)
        {
            if (matId >= MaterialId.Count) return generico;
            string propio = _playerName[matId];
            if (!string.IsNullOrEmpty(propio)) return propio;
            return NombreComun(matId) ?? generico;
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
                EncolarLeyBanner(ConstruirTextoLeyCristal());
            }
            else if (type == SimEventType.Grow && !_leyVivumDescubierta)
            {
                _leyVivumDescubierta = true;
                EncolarLeyBanner(ConstruirTextoLeyVivium());
            }
        }

        private void EncolarLeyBanner(string texto)
        {
            if (_leyBannerColaCount >= LeyBannerCapacidad) return; // defensivo: no debería pasar (solo 2 leyes de este tipo existen).
            _leyBannerCola[_leyBannerColaCount++] = texto;
        }

        /// <summary>Construido UNA vez, al disparar el evento (ver ApplyWitness) -- nunca en Update/OnGUI. Usa los nombres bautizados si los hay (ver NombreLey).</summary>
        private string ConstruirTextoLeyCristal()
        {
            string azoth = NombreLey(MaterialId.Azoth, "azoth").ToUpperInvariant();
            string semilla = NombreLey(MaterialId.CrystalSeed, "semilla de cristal").ToUpperInvariant();
            string cristal = NombreLey(MaterialId.Crystal, "cristal").ToUpperInvariant();
            return $"LEY DESCUBIERTA — El {azoth} que toca {semilla} (o {cristal} ya formado) se vuelve {cristal}. " +
                   "La semilla no se gasta: siembra una y aliméntala.";
        }

        private string ConstruirTextoLeyVivium()
        {
            string vivium = NombreLey(MaterialId.Vivium, "vivium").ToUpperInvariant();
            string nutriente = NombreLey(MaterialId.Nutrient, "nutriente").ToUpperInvariant();
            return $"LEY DESCUBIERTA — El {vivium} asentado, con {nutriente} al lado y calor TEMPLADO, crea {vivium} nuevo. " +
                   "No se consume: cada célula que nace es otra semilla.";
        }

        /// <summary>Avanza la cola FIFO de leyes pendientes cuando la actual caduca o no hay ninguna mostrándose todavía.</summary>
        private void ActualizarBannerLey()
        {
            if (_leyBannerActual != null)
            {
                if (Time.time < _leyBannerHasta) return;
                _leyBannerActual = null;
            }
            if (_leyBannerColaLeidos >= _leyBannerColaCount) return;

            _leyBannerActual = _leyBannerCola[_leyBannerColaLeidos++];
            _leyBannerHasta = Time.time + LeyBannerDuracionSeg;
        }

        private void OnGUI()
        {
            if (_leyBannerActual == null || DayCycle.InputLocked) return;
            UiStyles.Preparar();
            DrawLeyBanner();
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
