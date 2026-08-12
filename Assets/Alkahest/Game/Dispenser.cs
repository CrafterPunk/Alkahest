using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Grifo del banco de trabajo: al activarlo (E cerca, alterna ON/OFF), emite
    /// un caudal constante de un material base por su boquilla, que cae en la
    /// PILA DE RECOGIDA del banco (ver Sim/SimLevelBuilder: los cinco grifos
    /// están en columna vertical sobre el mismo pilar y vierten todos al mismo
    /// sitio — antes colgaban del muro a cuatro alturas distintas y regaban el
    /// suelo hasta el borde inferior de la pantalla, "los caños llevan el juego
    /// al borde inferior").
    ///
    /// M4: algunos materiales tienen un coste de Favor POR ACTIVACIÓN
    /// (<see cref="favorCostPerActivation"/>, fijado desde AlkahestGameBootstrap:
    /// Agua/Arena 0, Aceite 2, Nutriente 5, Azoth 4). Se cobra una única vez al
    /// pasar de OFF a ON (no por tick).
    ///
    /// PROGRESIÓN (playtest 4: "¿puedo conseguir todo con 4 caños?"). No: sin
    /// Azoth no hay cristal, y sin cristal los encargos de las jornadas 2 y 3
    /// eran imposibles fuera de la paleta de dev. El grifo de Azoth existe desde
    /// el principio pero nace SELLADO (<see cref="Bloqueado"/>): lo abre el
    /// Maestro al empezar la jornada 2, junto con las otras muestras (ver
    /// Game/MasterSupplies.cs).
    /// </summary>
    public sealed class Dispenser : MonoBehaviour, IMaquinaInteractiva
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const float ProximityRange = 3.6f;
        /// <summary>
        /// Radio (más generoso que el de interacción) dentro del cual el grifo
        /// enseña su chapa aunque NO sea el aparato enfocado. Sin esto, un grifo
        /// cerrado era invisible hasta estar justo encima de él: en el playtest 5
        /// Cesar directamente "no pudo acceder a los grifos" porque no los
        /// encontraba.
        /// </summary>
        private const float RangoVisible = 7f;
        private const int EmitRatePerTick = 12;
        private const int SpoutRadius = 1;
        /// <summary>
        /// Voladizo del caño sobre el pilar (que es piedra maciza hasta la celda
        /// 8). 5 celdas: la boquilla sobresale CLARAMENTE del muro y la gota de
        /// color cuelga en aire libre, dentro de la pila de recogida
        /// (interior x 9..56). Antes eran 3 y el caño quedaba lamiendo la piedra.
        /// </summary>
        private const int SpoutOffsetCells = 5;
        /// <summary>Filas que baja el caudal respecto a la celda de anclaje: la boquilla dibujada cuelga por debajo del eje del caño, y el chorro tiene que nacer DE ELLA.</summary>
        private const int SpoutDropCells = 2;
        private const float InsufficientFavorFlashSeconds = 1.5f;
        /// <summary>Celdas que se miran hacia arriba buscando la superficie del charco cuando el caño queda sumergido.</summary>
        private const int OverflowSearchUp = 8;

        [Tooltip("Coste en Favor de encender este grifo (una sola vez por activación). 0 = gratis.")]
        [SerializeField] private int favorCostPerActivation = 0;

        private AlkahestSim _sim;
        private Transform _player;
        private OrderSystem _orderSystem;
        /// <summary>
        /// (fix reclasificación de sustancias) Necesario para resolver el NOMBRE de verdad
        /// del material que dispensa este grifo -- ver ResolverNombre. No llega por Init()
        /// (cambiar esa firma exigiría tocar Game/AlkahestGameBootstrap.cs, fuera de las
        /// ARCHIVOS MODIFICABLES de este encargo), así que se resuelve con
        /// FindAnyObjectByType UNA sola vez en Init (nunca en Update/OnGUI): mismo patrón ya
        /// endorsado por el proyecto para localizar dependencias de escena (ver
        /// AlkahestGameBootstrap.Start, que hace lo mismo con AlkahestSim). Para cuando
        /// SpawnDispensers() corre, SubstanceKnowledge ya vive en el aprendiz (creado antes
        /// en TrySpawn), así que esto nunca falla en el flujo normal del juego.
        /// </summary>
        private SubstanceKnowledge _knowledge;
        private int _spoutX, _spoutY;
        private byte _matId;
        private bool _on;
        private float _accumulator;

        private float _insufficientFavorTimer;
        private float _bloqueoAvisoTimer;
        private bool _rebosando;

        private SpriteRenderer _cano;
        private SpriteRenderer _gota;
        private Transform _gotaTr;
        private Color _matColor = Color.white;
        private Vector3 _anclaRotulo;

        /// <summary>Sellado por el Maestro: no se puede abrir todavía. Ver doc de la clase.</summary>
        public bool Bloqueado { get; private set; }

        /// <summary>Material que dispensa (lo consulta MasterSupplies para localizar el grifo de Azoth).</summary>
        public byte Material => _matId;

        // Foco de interacción: CRÍTICO aquí — los cinco grifos están en columna
        // a una unidad de mundo unos de otros, y sin árbitro una sola E abriría
        // varios a la vez (ver Game/MachineFocus.cs).
        public Vector3 PuntoFoco => _anclaRotulo;
        public float RangoFoco => ProximityRange;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Transform player, int mountCellX, int mountCellY, byte materialId,
            OrderSystem orderSystem = null, int favorCost = 0, bool bloqueado = false)
        {
            _sim = sim;
            _player = player;
            _spoutX = mountCellX + SpoutOffsetCells;
            _spoutY = mountCellY - SpoutDropCells;
            _matId = materialId;
            _orderSystem = orderSystem;
            favorCostPerActivation = favorCost;
            Bloqueado = bloqueado;
            _knowledge = FindAnyObjectByType<SubstanceKnowledge>(); // ver doc del campo.

            BuildVisual(mountCellX, mountCellY);
            MachineFocus.Registrar(this);
        }

        private void OnDestroy() => MachineFocus.Olvidar(this);

        /// <summary>
        /// Nombre de verdad de lo que da este grifo: bautizado &gt; común de taller &gt; "???"
        /// (fix reclasificación de sustancias). ANTES caía en `_sim.Universe.Get(_matId).devName`
        /// cuando NombreComun devolvía null, así que el grifo de Azoth mostraba literalmente
        /// "Azoth" -- el nombre INTERNO en inglés del devName -- para siempre, sin importar si
        /// el jugador ya lo había bautizado. Azoth es justo uno de los materiales reclasificados
        /// como "innominado" (ver Game/SubstanceKnowledge.cs): su chapa tiene que decir "???"
        /// hasta que se bautice, exactamente como el resto del HUD.
        /// </summary>
        private string ResolverNombre() => _knowledge != null
            ? _knowledge.NombreParaHud(_matId)
            : (SubstanceKnowledge.NombreComun(_matId) ?? "???"); // defensivo: nunca debería faltar en el flujo normal.

        /// <summary>Rompe el sello del Maestro: el grifo pasa a ser usable (jornada 2).</summary>
        public void Desbloquear()
        {
            if (!Bloqueado) return;
            Bloqueado = false;
            UpdateVisual();
            Debug.Log($"[ChaosAlchemy] El Maestro abre el grifo de {ResolverNombre()}.");
        }

        private void BuildVisual(int mountCellX, int mountCellY)
        {
            transform.position = _sim.CellToWorld(new Vector2Int(mountCellX, mountCellY));

            // Caño de latón generado (brida + tubo + boquilla + volante).
            //
            // TAMAÑO (fix del playtest 5, "no pude acceder a los grifos"): medía
            // 3.4 x 2.0 celdas = 17 x 10 px a 720p, seis veces más estrecho que
            // cualquier otra máquina del taller (la placa ígnea mide 260 px) y en
            // latón oscuro sobre piedra oscura, pegado al borde izquierdo. Era
            // literalmente invisible. Ahora mide 8 x 5 celdas (40 x 25 px) y sale
            // EN VOLADIZO: la brida muerde el pilar en la celda 8 y la boquilla
            // llega hasta la 15, con el caudal cayendo desde la 13.
            float celda = SimRenderer.CellWorldSize;
            _cano = MaquinariaSprites.CrearCapa(transform, "Cano", MaquinariaSprites.CanoGrifo(), 19,
                8f * celda, 5f * celda);
            _cano.transform.localPosition = new Vector3(2.5f * celda, 0f, 0f);

            // Gota de color: es lo que permite saber de un vistazo (y desde lejos)
            // qué da cada grifo y cuál está abierto -- ver UpdateVisual.
            var gotaGO = new GameObject("Gota");
            gotaGO.transform.SetParent(transform, false);
            // La gota cuelga justo bajo la boquilla, en aire libre: es la marca
            // de color que dice QUÉ da este grifo desde el otro lado del taller.
            gotaGO.transform.localPosition = new Vector3(SpoutOffsetCells * celda, -3.2f * celda, 0f);
            _gotaTr = gotaGO.transform;
            _gota = gotaGO.AddComponent<SpriteRenderer>();
            _gota.sprite = MaquinariaSprites.Solido();
            _gota.sortingOrder = 20;
            _gotaTr.localScale = new Vector3(0.26f, 0.26f, 1f);
            _matColor = _sim.Universe.Get(_matId).baseColor;
            _gota.color = _matColor;

            // El rótulo (y el punto de foco) van sobre el cuerpo del caño, no
            // pegados al muro: así la chapa se lee fuera de la piedra y la
            // distancia de interacción se mide desde el aparato de verdad.
            _anclaRotulo = transform.position + new Vector3(2.5f * celda, 3.4f * celda, 0f);
            UpdateVisual();
        }

        /// <summary>
        /// Estado del grifo legible a distancia: sellado = latón apagado y gota
        /// gris; cerrado = gota pequeña y mate; abierto = gota brillante que late.
        /// </summary>
        private void UpdateVisual()
        {
            if (_gota == null || _gotaTr == null) return;

            if (Bloqueado)
            {
                if (_cano != null) _cano.color = new Color(0.42f, 0.40f, 0.38f, 1f);
                _gota.color = new Color(0.35f, 0.34f, 0.36f, 0.7f);
                _gotaTr.localScale = new Vector3(0.18f, 0.18f, 1f);
                return;
            }

            if (_cano != null) _cano.color = Color.white;

            if (_on)
            {
                float pulso = 0.85f + 0.15f * Mathf.Sin(Time.time * 9f);
                _gota.color = new Color(
                    Mathf.Clamp01(_matColor.r * 1.25f), Mathf.Clamp01(_matColor.g * 1.25f),
                    Mathf.Clamp01(_matColor.b * 1.25f), 1f);
                float s = 0.34f * pulso;
                _gotaTr.localScale = new Vector3(s, s, 1f);
            }
            else
            {
                _gota.color = new Color(_matColor.r, _matColor.g, _matColor.b, 0.8f);
                _gotaTr.localScale = new Vector3(0.24f, 0.24f, 1f);
            }
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan el grifo.

            // (fix playtest 10) E es un atajo de una sola tecla: no puede robarle letras al
            // campo de bautizar ni competir con el diario a pantalla completa (ver el mismo
            // comentario en Game/ChillStone.cs/HeatPlate.cs). El caudal ya abierto sigue
            // emitiendo igual con el libro abierto -- solo se calla el toggle ON/OFF.
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocado())
            {
                ToggleRequested();
            }

            if (_insufficientFavorTimer > 0f) _insufficientFavorTimer -= Time.deltaTime;
            if (_bloqueoAvisoTimer > 0f) _bloqueoAvisoTimer -= Time.deltaTime;

            if (_on)
            {
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
                {
                    EmitTick();
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;
            }

            UpdateVisual();
        }

        private void ToggleRequested()
        {
            if (Bloqueado)
            {
                _bloqueoAvisoTimer = InsufficientFavorFlashSeconds;
                return;
            }

            if (_on)
            {
                _on = false;
                _rebosando = false;
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName} -> OFF");
                return;
            }

            if (TryPayActivationCost())
            {
                _on = true;
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName} -> ON (coste {favorCostPerActivation} Favor).");
            }
            else
            {
                _insufficientFavorTimer = InsufficientFavorFlashSeconds;
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName}: sin Favor suficiente ({favorCostPerActivation} requerido).");
            }
        }

        private bool TryPayActivationCost()
        {
            if (favorCostPerActivation <= 0) return true;
            if (_orderSystem == null) return true; // defensivo: sin OrderSystem conectado no bloqueamos el grifo.
            return _orderSystem.SpendFavor(favorCostPerActivation);
        }

        /// <summary>¿Es ESTE el grifo que el aprendiz tiene delante? (ver Game/MachineFocus.cs)</summary>
        private bool EstaEnfocado() => MachineFocus.EsFoco(this, _player);

        /// <summary>¿Está el aprendiz lo bastante cerca como para que valga la pena anunciarse (aunque no sea el grifo enfocado)?</summary>
        private bool JugadorAlaVista()
        {
            if (_player == null) return false;
            return (_player.position - _anclaRotulo).sqrMagnitude <= RangoVisible * RangoVisible;
        }

        /// <summary>
        /// (playtest 3: "el grifo de agua deja de funcionar cuando se llena")
        /// El caño emitía SOLO en las celdas vacías de su boca, así que en cuanto
        /// el charco subía hasta taparla el grifo parecía averiado. Ahora, si la
        /// boca está sumergida, busca la SUPERFICIE del charco unas pocas celdas
        /// más arriba y deja caer ahí una gota por tick: el nivel sigue subiendo
        /// (más despacio, como un rebose real) y el grifo nunca parece roto.
        /// </summary>
        private void EmitTick()
        {
            int budget = EmitRatePerTick;
            for (int dy = -SpoutRadius; dy <= SpoutRadius && budget > 0; dy++)
            {
                int y = _spoutY + dy;
                for (int dx = -SpoutRadius; dx <= SpoutRadius && budget > 0; dx++)
                {
                    if (dx * dx + dy * dy > SpoutRadius * SpoutRadius) continue;
                    int x = _spoutX + dx;
                    if (!CellGrid.InBounds(x, y)) continue;
                    if (_sim.SampleMaterial(x, y) != MaterialId.Empty) continue;

                    _sim.Paint(x, y, 0, _matId);
                    budget--;
                }
            }

            if (budget < EmitRatePerTick)
            {
                _rebosando = false;
                return;
            }

            for (int up = 1; up <= OverflowSearchUp; up++)
            {
                int y = _spoutY + up;
                if (!CellGrid.InBounds(_spoutX, y)) break;
                if (_sim.SampleMaterial(_spoutX, y) != MaterialId.Empty) continue;

                _sim.Paint(_spoutX, y, 0, _matId);
                _rebosando = false;
                return;
            }

            _rebosando = true;
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            bool cerca = EstaEnfocado();
            bool visible = cerca || _on || JugadorAlaVista();
            if (!visible) return;

            UiStyles.Preparar();

            // (fix reclasificación de sustancias) Nombre RESUELTO (bautizado > común de taller >
            // "???"), nunca el devName interno en inglés -- ver ResolverNombre. Antes caía en
            // devName para lo innominado y el grifo de Azoth decía "Azoth" para siempre.
            string nombre = ResolverNombre();

            // 1) CHAPA FIJA sobre el caño (fuera de la pila de recogida, que es
            //    donde el jugador aspira): qué da y en qué estado está.
            string chapa;
            Color color;
            if (Bloqueado)
            {
                chapa = "grifo sellado por el Maestro";
                color = UiStyles.TextoTenue;
            }
            else if (_on)
            {
                chapa = _rebosando ? nombre + " · rebosando" : nombre + " · ABIERTO";
                color = _rebosando ? UiStyles.Aviso : UiStyles.Oro;
            }
            else
            {
                chapa = "grifo de " + nombre;
                color = UiStyles.TextoTenue;
            }
            if (_insufficientFavorTimer > 0f) { chapa = "¡sin Favor suficiente!"; color = UiStyles.Peligro; }
            else if (_bloqueoAvisoTimer > 0f) { chapa = "el Maestro aún no os confía esto"; color = UiStyles.Peligro; }

            UiStyles.PlacaMundo(_anclaRotulo, chapa, color, UiStyles.S(6f));

            // 2) PROMPT: solo cerca y con las manos libres (playtest 4).
            if (cerca && !Bloqueado && !UiStyles.RatonOcupado)
            {
                string prompt = _on ? "E — cerrar" : "E — abrir";
                if (!_on && favorCostPerActivation > 0) prompt += " (" + favorCostPerActivation + " Favor)";
                UiStyles.PlacaMundo(_anclaRotulo, prompt, UiStyles.Oro, UiStyles.S(23f));
            }
        }
    }
}
