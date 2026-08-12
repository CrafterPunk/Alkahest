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
        /// <summary>Radio de interacción con E (fix playtest 6: bajado de 3.6 a 2.8, misma escala que ChillStone/HeatPlate).</summary>
        private const float ProximityRange = 2.8f;

        // ---------------------------------------------------------------
        // ESCALA COMPARTIDA DE CERCANÍA DEL TALLER (fix playtest 6). Los
        // MISMOS valores viven, duplicados a propósito, en ChillStone y
        // HeatPlate: un único criterio de "cerca" para todo el taller.
        // Sustituye al viejo RangoVisible=7f (playtest 5: "no pude acceder a
        // los grifos" — un radio fijo enorme para que no fueran invisibles).
        // Se compensa así en vez de reintroducir un radio grande: un grifo
        // ABIERTO se anuncia desde RangoEstado (igual que una placa
        // encendida), y los cinco grifos están en columna, así que acercarse
        // a cualquiera revela ya a sus vecinos cerrados.
        //  · RangoEstado: de lejos, SOLO el estado (abierto/rebosa/sellado).
        //  · RangoNombre: de cerca, además el nombre del material.
        // (fix playtest 7: estos dos anillos de PlacaMundo -desplazamiento
        // VERTICAL- eran justo la causa del bug del jugador y se sustituyen
        // por la chapa lateral única de abajo; ver ChapaCercaniaPleno/Lejos.)
        // ---------------------------------------------------------------

        // ---------------------------------------------------------------
        // CHAPA PERMANENTE POR CARRIL (fix playtest 7): "'cerrar' del agua
        // está escrito sobre el grifo de arena". Los cinco grifos están en
        // columna a solo 1 unidad de mundo unos de otros, así que CUALQUIER
        // rótulo con desplazamiento VERTICAL desde el ancla de un grifo cae
        // sobre el cuerpo del vecino de arriba o abajo. La solución no es
        // desplazar más (a esa distancia ya no hay margen: 1 unidad = ~55px
        // a 720p, y una chapa mide ~20px de alto) sino no desplazar en
        // vertical NUNCA: la chapa vive a la DERECHA del propio grifo, a SU
        // misma altura (desplazarYPx = 0). Con eso cada grifo tiene su
        // propio carril horizontal y dos chapas de grifos vecinos jamás se
        // cruzan verticalmente por diseño, sin depender de que quede hueco
        // arriba o abajo.
        //
        // El jugador sugirió ponerla en la pared IZQUIERDA (donde vive la
        // columna de grifos). No cabe: el pilar ocupa las columnas de mundo
        // x=1..8, pegado al borde izquierdo de la pantalla (ver
        // Sim/SimLevelBuilder.cs) — cualquier chapa a la izquierda del caño
        // se saldría de cuadro. A la derecha del caño, en cambio, hay pared
        // vacía de sobra. El objetivo real del jugador (que cada grifo
        // tenga SU rótulo fijo en SU sitio, sin pisar al vecino) se cumple
        // igual poniendo la chapa en la fila de su propio grifo.
        // (medidas "de diseño" a 720p; se escalan con UiStyles.S() en cada
        // llamada, igual que el resto del HUD -- ver UiStyles.S).
        private const float ChapaSeparacionPx = 16f;
        private const float PromptDesplazarYPx = -15f;
        /// <summary>Cercanía a la que la chapa en reposo pasa de discreta (0.45) a plena (1.0). Ver OnGUI.</summary>
        private const float ChapaCercaniaPleno = 2.6f;
        private const float ChapaCercaniaLejos = 5.5f;

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

        // Textos fijos del rótulo (fix playtest 6: "no hace falta que la
        // nomenclatura incluya la palabra grifo" — el jugador ya lo ve, es un
        // caño; el nombre del material basta). Los Debug.Log internos siguen
        // diciendo "grifo", esos no son cara al jugador.
        // (fix playtest 7): "SELLADO" a secas — es el texto de la chapa
        // PERMANENTE ahora (ver sección 1 de la clase), no un aviso puntual.
        private const string ChapaSellada = "SELLADO";
        private const string AvisoSinFavor = "¡sin Favor suficiente!";
        private const string AvisoBloqueo = "el Maestro aún no os confía esto";

        [Tooltip("Coste en Favor de encender este grifo (una sola vez por activación). 0 = gratis.")]
        [SerializeField] private int favorCostPerActivation = 0;

        private AlkahestSim _sim;
        private Transform _player;
        private OrderSystem _orderSystem;
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

        /// <summary>
        /// (fix playtest 7) Capa de RESALTE: una copia agrandada y teñida de
        /// oro del mismo sprite del caño, dibujada DETRÁS de él (sortingOrder
        /// menor), que asoma por los bordes como un halo. Sustituye al prompt
        /// "E — ..." permanente como señal de "estás lo bastante cerca para
        /// actuar aquí": se ve desde el otro lado del taller sin ocupar texto
        /// en pantalla ni parpadear (ver <see cref="_haloAlfaActual"/>).
        /// </summary>
        private SpriteRenderer _halo;
        private float _haloAlfaActual;

        // Chapas del rótulo, cacheadas: el material y el coste de Favor son
        // fijos tras Init, así que se construyen UNA vez en BuildVisual y OnGUI
        // solo elige cuál mostrar (cero asignaciones de string por frame).
        private string _chapaCerrado;   // "AGUA" o "ACEITE  3★" - chapa permanente en reposo.
        private string _chapaAbierto;   // "AGUA · abierto" - chapa permanente, grifo ON.
        private string _chapaRebosando; // "AGUA · rebosa" - chapa permanente, rebosando.
        private string _promptAbrir;    // "E — abrir" o "E — abrir (N Favor)".

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

            BuildVisual(mountCellX, mountCellY);
            MachineFocus.Registrar(this);
        }

        private void OnDestroy() => MachineFocus.Olvidar(this);

        /// <summary>Rompe el sello del Maestro: el grifo pasa a ser usable (jornada 2).</summary>
        public void Desbloquear()
        {
            if (!Bloqueado) return;
            Bloqueado = false;
            UpdateVisual();
            Debug.Log($"[ChaosAlchemy] El Maestro abre el grifo de {SubstanceKnowledge.NombreComun(_matId) ?? _sim.Universe.Get(_matId).devName}.");
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

            // (fix playtest 7) HALO de resalte: la misma silueta del caño,
            // ~1.22x más grande y DETRÁS de todo (sortingOrder 15 < 19 del
            // propio caño), teñida de oro. Al ser una copia más grande detrás
            // de la real, asoma por los bordes como un contorno luminoso.
            // Nace invisible (alfa 0); Update()/UpdateHalo() la enciende SOLO
            // cuando este grifo es el foco, con un latido suave. Sustituye al
            // prompt "E — ..." permanente (que "estorba" según el jugador)
            // como señal de "estás lo bastante cerca para actuar aquí": se
            // lee desde el otro lado del taller sin ocupar texto en pantalla.
            _halo = MaquinariaSprites.CrearCapa(transform, "Halo", MaquinariaSprites.CanoGrifo(), 15,
                8f * celda * 1.22f, 5f * celda * 1.22f);
            _halo.transform.localPosition = _cano.transform.localPosition;
            _halo.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);

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

            // (fix playtest 7) El ancla de la chapa (y del punto de foco) ya
            // NO va por encima del caño (eso era lo que hacía que el rótulo
            // de un grifo cayera sobre el vecino de la columna, a solo 1
            // unidad de mundo de distancia vertical). Va junto al borde
            // DERECHO del cuerpo del caño —localPosition.x=2.5*celda más la
            // mitad del ancho del sprite (8*celda/2=4*celda) = 6.5*celda— a
            // la MISMA altura que el propio caño (y=0, el mount). Con esto la
            // chapa se dibuja SIEMPRE en el carril horizontal de su propio
            // grifo (ver PlacaMundoLateral en OnGUI) y dos chapas de grifos
            // vecinos jamás se cruzan, sin depender de cuánto hueco quede
            // libre arriba o abajo.
            _anclaRotulo = transform.position + new Vector3(6.5f * celda, 0f, 0f);

            // Chapas cacheadas (fix playtest 6/7): material, coste de Favor y
            // el sufijo de coste son fijos tras Init, así que se construyen
            // aquí UNA sola vez, nunca dentro de OnGUI. Nombre en MAYÚSCULAS y
            // sin la palabra "grifo" (playtest 6: "no hace falta que la
            // nomenclatura incluya la palabra grifo").
            string nombreCorto = (SubstanceKnowledge.NombreComun(_matId) ?? _sim.Universe.Get(_matId).devName).ToUpperInvariant();
            // (fix playtest 7) La chapa en reposo también informa del coste en
            // Favor de encenderlo, para que el jugador no lo descubra a
            // ciegas pulsando E: "ACEITE  3★".
            string sufijoCoste = favorCostPerActivation > 0 ? $"  {favorCostPerActivation}★" : "";
            _chapaCerrado = nombreCorto + sufijoCoste;
            _chapaAbierto = nombreCorto + " · abierto";
            _chapaRebosando = nombreCorto + " · rebosa";
            _promptAbrir = favorCostPerActivation > 0 ? $"E — abrir ({favorCostPerActivation} Favor)" : "E — abrir";

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

            // (fix playtest 7) Brillo extra en el propio caño cuando es el
            // foco: un pequeño empujón hacia el dorado, además del halo
            // detrás. Ni Shader.Find ni material nuevo, solo el tinte del
            // SpriteRenderer (regla de oro del repo: solo SpriteRenderer).
            if (_cano != null)
            {
                _cano.color = EstaEnfocado() ? Color.Lerp(Color.white, UiStyles.Oro, 0.18f) : Color.white;
            }

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

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && EstaEnfocado())
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
            UpdateHalo();
        }

        /// <summary>
        /// (fix playtest 7) Latido del halo de resalte: sube a un pulso
        /// suave (0.45..0.85) mientras el grifo es el foco, y baja a 0 en
        /// cuanto deja de serlo. SIEMPRE a través de MoveTowards (no un
        /// salto directo al valor objetivo) para que la entrada/salida del
        /// foco sea un fundido y no un parpadeo. Cero asignaciones por
        /// frame más allá del Color struct (no es un alloc de heap).
        /// </summary>
        private void UpdateHalo()
        {
            if (_halo == null) return;

            bool enfocado = !Bloqueado && EstaEnfocado();
            float objetivo = enfocado ? 0.65f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
            _haloAlfaActual = Mathf.MoveTowards(_haloAlfaActual, objetivo, 6f * Time.deltaTime);
            _halo.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _haloAlfaActual);
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
                // (fix playtest 7) Cuenta como "uso enseñado" de la E: apagar
                // el grifo es una acción con efecto, igual que encenderlo.
                MachineFocus.RegistrarUsoE();
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName} -> OFF");
                return;
            }

            if (TryPayActivationCost())
            {
                _on = true;
                MachineFocus.RegistrarUsoE();
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName} -> ON (coste {favorCostPerActivation} Favor).");
            }
            else
            {
                // (fix playtest 7) NO se registra uso: un intento fallido por
                // falta de Favor no enseña nada sobre cómo usar la E.
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

        /// <summary>
        /// (fix playtest 7) Reescrito por completo. Antes esta clase dibujaba
        /// dos "anillos" (estado/nombre) con <see cref="UiStyles.PlacaMundo"/>,
        /// que ancla el rótulo ENCIMA del punto con un desplazamiento
        /// vertical — con los cinco grifos en columna a 1 unidad de mundo
        /// unos de otros, ese desplazamiento vertical caía sistemáticamente
        /// sobre el caño vecino ("'cerrar' del agua está escrito sobre el
        /// grifo de arena"). Ahora hay UNA sola chapa PERMANENTE por grifo,
        /// anclada al LADO (nunca encima, ver <see cref="UiStyles.PlacaMundoLateral"/>)
        /// en el carril horizontal de su propio caño, así que dos chapas de
        /// grifos vecinos son geométricamente incapaces de solaparse. El
        /// prompt "E — ..." ya no es permanente (MachineFocus.MostrarPromptE
        /// lo apaga tras las dos primeras veces del taller entero) y el
        /// resalte del foco lo asume el halo dorado (ver UpdateHalo).
        /// </summary>
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            UiStyles.Preparar();

            // Cercanía de la chapa en reposo: discreta de lejos (0.45), plena
            // de cerca (1.0). PlacaMundoLateral ya descarta barato el dibujo
            // si alfa<=0.02 o si el punto queda detrás de cámara, así que no
            // hace falta una salida temprana propia aquí.
            float cercania = UiStyles.Cercania(_anclaRotulo, _player, ChapaCercaniaPleno, ChapaCercaniaLejos);

            // Contenido de la chapa PERMANENTE, por prioridad (solo una línea
            // a la vez, igual que hacía el viejo "anillo de estado"). Los
            // avisos puntuales (Favor insuficiente / intento sobre un grifo
            // sellado) ocupan el MISMO carril lateral en vez de un rótulo
            // aparte con su propio desplazamiento — si tuvieran uno propio
            // reintroducirían el mismo bug de solape que motiva este fix.
            string texto;
            Color color;
            float alfa;

            if (_insufficientFavorTimer > 0f)
            {
                texto = AvisoSinFavor;
                color = UiStyles.Peligro;
                alfa = 1f; // el jugador acaba de pulsar E aquí mismo: siempre cerca.
            }
            else if (_bloqueoAvisoTimer > 0f)
            {
                texto = AvisoBloqueo;
                color = UiStyles.Peligro;
                alfa = 1f;
            }
            else if (Bloqueado)
            {
                texto = ChapaSellada;
                color = UiStyles.TextoTenue;
                alfa = 0.45f + 0.55f * cercania;
            }
            else if (_on)
            {
                texto = _rebosando ? _chapaRebosando : _chapaAbierto;
                color = _rebosando ? UiStyles.Aviso : UiStyles.Oro;
                alfa = 1f; // un grifo abierto es información urgente a cualquier distancia.
            }
            else
            {
                texto = _chapaCerrado;
                color = UiStyles.TextoTenue;
                alfa = 0.45f + 0.55f * cercania;
            }

            UiStyles.PlacaMundoLateral(_anclaRotulo, texto, color, UiStyles.S(ChapaSeparacionPx), 0f, alfa, aLaIzquierda: false);

            // PROMPT "E — ...": solo mientras el taller aún lo está enseñando
            // (MachineFocus.MostrarPromptE), solo sobre el caño enfocado y
            // solo con las manos libres. Va DEBAJO de la chapa, en el MISMO
            // carril lateral (misma separación horizontal, desplazamiento en
            // Y pequeño hacia abajo) — nunca centrado sobre el aparato.
            if (!Bloqueado && MachineFocus.MostrarPromptE && EstaEnfocado() && !UiStyles.RatonOcupado)
            {
                string prompt = _on ? "E — cerrar" : _promptAbrir;
                UiStyles.PlacaMundoLateral(_anclaRotulo, prompt, UiStyles.Oro,
                    UiStyles.S(ChapaSeparacionPx), UiStyles.S(PromptDesplazarYPx), 1f, aLaIzquierda: false);
            }
        }
    }
}
