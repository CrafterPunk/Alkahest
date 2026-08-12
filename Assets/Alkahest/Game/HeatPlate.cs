using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Placa ígnea: el aparato empotrado bajo una cuba que inyecta calor en las
    /// filas de celdas justo encima suyo. Tres intensidades (APAGADA / TEMPLADA /
    /// ARDIENTE) que se ciclan pulsando E cerca de ella.
    ///
    /// ---------------------------------------------------------------------
    /// CAMBIOS DEL PLAYTEST 4
    /// ---------------------------------------------------------------------
    /// 1. IDENTIDAD VISUAL PROPIA. Antes la placa era literalmente una barra de
    ///    0.06 unidades de alto que cambiaba de color: no parecía un aparato,
    ///    parecía una raya. Ahora es un CHASIS de metal remachado (generado por
    ///    código, sin assets) con una ventana por la que se ven sus RESISTENCIAS
    ///    naranjas serpenteando; las resistencias se encienden y laten según la
    ///    intensidad, así que el estado se lee desde el otro lado del taller.
    ///
    /// 2. RÓTULO FIJO Y PEQUEÑO ("el label de las placas tapa la interacción al
    ///    aspirar"). La chapa de identificación va SIEMPRE debajo del chasis,
    ///    sobre la piedra del suelo — nunca dentro de la cuba, que es donde se
    ///    aspira — y es diminuta (UiStyles.PlacaMundo). El prompt "E — regular"
    ///    solo aparece si estás cerca, con las manos libres, y solo las dos
    ///    primeras veces del taller (fix playtest 7: MachineFocus.MostrarPromptE
    ///    — a partir de ahí lo sustituye el RESALTE dorado del aparato
    ///    enfocado, ver ActualizarResalte). VERIFICADO (fix playtest 7): a
    ///    diferencia de ChillStone, _centroChasis SÍ está donde el jugador
    ///    trabaja — el chasis se apoya en el suelo, al pie de la cuba, que es
    ///    justo donde el aprendiz se planta para pulsar E; no hizo falta un
    ///    ancla de labio aparte.
    ///
    /// 3. TEMPLADA = LA BANDA DE CRECIMIENTO DE ESTE UNIVERSO. Antes "Tibia"
    ///    fijaba raw 140 (¡160 °C!) y "Caliente" raw 220 (320 °C): el Vivium
    ///    muere carbonizado por encima de 120 °C y crece entre ~30 y ~60 °C, así
    ///    que NINGUNA posición de la placa permitía cultivar — el arco de
    ///    domesticación entero (decisión §14) era inalcanzable salvo por
    ///    accidente en el gradiente térmico. Ahora TEMPLADA apunta al centro
    ///    exacto de la banda de crecimiento de la seed (Universe.VivGrowMinRaw/
    ///    MaxRaw) y ARDIENTE sigue siendo el fuego de verdad (320 °C, por encima
    ///    de cualquier temperatura de ignición sorteable).
    ///
    /// LIMITACIÓN: escribe _sim.Grid.temp[] directamente en vez de pasar por una
    /// API dedicada del simulador. TODO(ChaosAlchemy): canalizar por
    /// AlkahestSim.InjectHeat de cara al netcode.
    /// </summary>
    public sealed class HeatPlate : MonoBehaviour, IMaquinaInteractiva
    {
        private enum State { Off = 0, Templada = 1, Ardiente = 2 }

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        /// <summary>Radio de interacción con E (fix playtest 6: bajado de 3.2 a 2.8 — el prompt asomaba antes de estar realmente al lado del aparato).</summary>
        private const float ProximityRange = 2.8f;

        // ---------------------------------------------------------------
        // ESCALA COMPARTIDA DE CERCANÍA DEL TALLER (fix playtest 6: "los
        // labels... se activan aunque esté lejos"). Los MISMOS valores
        // viven, duplicados a propósito, en ChillStone y Dispenser: un único
        // criterio de "cerca" para todo el taller.
        //  · RangoEstado: de lejos, SOLO el estado de trabajo (si lo hay).
        //  · RangoNombre: de cerca, además el nombre del aparato — pero solo
        //    hasta que el aprendiz ya lo conoce (ver _yaConocida).
        // ---------------------------------------------------------------
        private const float RangoEstadoPleno = 5.0f;
        private const float RangoEstadoDesvanece = 6.5f;
        private const float RangoNombrePleno = 2.6f;
        private const float RangoNombreDesvanece = 3.6f;

        private const byte ArdienteRaw = 220; // ~320 °C
        private const int TempStepPerTick = 5;

        /// <summary>
        /// Filas por encima de la placa que reciben calor. Subido de 2 a 3 en el
        /// pase de reingeniería: las cubas son más profundas y la bandeja fría
        /// solo tiene 6 filas útiles; con 2 filas el gradiente tardaba tanto en
        /// subir que "calentar la cuba" no se sentía como una acción.
        /// </summary>
        private const int RowsAffected = 3;

        private AlkahestSim _sim;
        private Transform _player;
        private int _cellX0, _cellX1, _plateRow;
        private State _state = State.Off;
        private float _accumulator;

        /// <summary>Objetivo de TEMPLADA: centro de la banda de crecimiento del Vivium de ESTA seed (ver doc de la clase).</summary>
        private byte _templadaRaw = 82;

        /// <summary>
        /// Aprendizaje del taller (fix playtest 6): el aprendiz ya ha estado lo
        /// bastante cerca como para saber qué es este aparato, así que su rótulo
        /// de NOMBRE no vuelve a dibujarse en lo que dure la partida. Campo de
        /// instancia a propósito — NO estático, NO PlayerPrefs: cada partida
        /// nueva empieza sin nada aprendido, que es lo correcto.
        /// </summary>
        private bool _yaConocida;

        /// <summary>Chapa del anillo de ESTADO, cacheada: solo se reconstruye al cambiar de estado (nunca dentro de OnGUI).</summary>
        private string _chapaEstado;

        private const string ChapaNombre = "placa ígnea";

        private SpriteRenderer _resistencias;
        private Vector3 _centroChasis;

        private SpriteRenderer _resalte;
        private float _alfaResalte;

        // Foco de interacción: solo el aparato MÁS CERCANO responde a E y
        // muestra su prompt (ver Game/MachineFocus.cs).
        public Vector3 PuntoFoco => _centroChasis;
        public float RangoFoco => ProximityRange;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Transform player, int cellX0, int cellX1, int plateRow)
        {
            _sim = sim;
            _player = player;
            _cellX0 = cellX0;
            _cellX1 = cellX1;
            _plateRow = plateRow;

            if (_sim != null && _sim.Universe != null)
            {
                int centro = (_sim.Universe.VivGrowMinRaw + _sim.Universe.VivGrowMaxRaw) / 2;
                _templadaRaw = (byte)Mathf.Clamp(centro, 1, 254);
            }

            BuildVisual();
            UpdateVisualTint();
            MachineFocus.Registrar(this);
        }

        private void OnDestroy() => MachineFocus.Olvidar(this);

        // -----------------------------------------------------------------
        // Visual: chasis remachado + resistencias serpenteantes, generados.
        // -----------------------------------------------------------------
        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;
            int spanCeldas = _cellX1 - _cellX0 + 1;

            // El chasis ocupa las filas de piedra del SUELO de la cuba (las
            // WallThickness filas que terminan en _plateRow): la cuba se apoya
            // encima del aparato, que es exactamente lo que cuenta la fantasía.
            int filaInferior = _plateRow - SimLevelBuilder.WallThickness + 1;
            float anchoMundo = spanCeldas * celda;
            float altoMundo = (_plateRow + 1 - filaInferior) * celda;

            float centroX = (_cellX0 + spanCeldas * 0.5f) * celda;
            float centroY = (filaInferior + (_plateRow + 1 - filaInferior) * 0.5f) * celda;
            _centroChasis = new Vector3(centroX, centroY, 0f);
            transform.position = _centroChasis;

            // Resalte de foco (fix playtest 7, ver ActualizarResalte): capa
            // DETRÁS de las demás (sortingOrder menor que Chasis=18), copia del
            // sprite principal agrandada ~15%/35% y teñida de oro; al ser mayor
            // asoma por los bordes del chasis como un halo. Se crea UNA vez
            // aquí; en Update solo se le cambia el color (cero allocs/frame).
            _resalte = MaquinariaSprites.CrearCapa(transform, "Resalte", MaquinariaSprites.ChasisPlaca(spanCeldas), 16,
                anchoMundo * 1.15f, altoMundo * 1.35f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);

            MaquinariaSprites.CrearCapa(transform, "Chasis", MaquinariaSprites.ChasisPlaca(spanCeldas), 18,
                anchoMundo, altoMundo);
            _resistencias = MaquinariaSprites.CrearCapa(transform, "Resistencias",
                MaquinariaSprites.ResistenciasPlaca(spanCeldas), 19, anchoMundo, altoMundo);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan la placa.

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && EstaEnfocada())
            {
                CycleState();
            }

            if (_state != State.Off)
            {
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
                {
                    ApplyHeatTick();
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;
            }

            AnimarResistencias();
        }

        /// <summary>¿Es ESTE el aparato que el aprendiz tiene delante? (ver Game/MachineFocus.cs)</summary>
        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        private void CycleState()
        {
            _state = (State)(((int)_state + 1) % 3);
            UpdateVisualTint();
            RebuildChapaEstado();
            MachineFocus.RegistrarUsoE(); // (fix playtest 7) el estado cambió de verdad: cuenta como un uso aprendido de E.
            Debug.Log($"[ChaosAlchemy] Placa ígnea -> {StateLabel()} ({CellGrid.RawToC(TargetRaw())} °C)");
        }

        /// <summary>
        /// Reconstruye la chapa del anillo de ESTADO. Se llama SOLO al cambiar de
        /// estado (nunca desde OnGUI): el raw objetivo de cada estado es
        /// constante, así que el texto no cambia frame a frame.
        /// </summary>
        private void RebuildChapaEstado()
        {
            _chapaEstado = _state == State.Off
                ? null // apagada: nada que anunciar de lejos.
                : $"{StateLabel()} {CellGrid.RawToC(TargetRaw())}°";
        }

        private byte TargetRaw() => _state == State.Ardiente ? ArdienteRaw : _templadaRaw;

        private void ApplyHeatTick()
        {
            byte target = TargetRaw();
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            for (int x = _cellX0; x <= _cellX1; x++)
            {
                for (int dy = 1; dy <= RowsAffected; dy++)
                {
                    int y = _plateRow + dy;
                    if (!CellGrid.InBounds(x, y)) continue;
                    int idx = CellGrid.Idx(x, y);
                    int cur = grid.temp[idx];
                    int next = cur < target ? Mathf.Min(target, cur + TempStepPerTick) : Mathf.Max(target, cur - TempStepPerTick);
                    grid.temp[idx] = (byte)next;
                    grid.WakeChunk(x, y, tick);
                }
            }
        }

        private void UpdateVisualTint()
        {
            if (_resistencias == null) return;
            _resistencias.color = ColorResistencia(1f);
        }

        /// <summary>
        /// Las resistencias respiran: apagadas son metal frío, templadas laten
        /// ámbar, ardientes laten blanco-naranja. (fix playtest 7) Ya se
        /// llamaba en TODOS los frames (a diferencia de ChillStone, aquí no
        /// vivía dentro de la rama "encendida"), así que basta con colgarle el
        /// resalte de foco al final para que también lata siempre.
        /// </summary>
        private void AnimarResistencias()
        {
            if (_resistencias != null && _state != State.Off)
            {
                float pulso = 0.82f + 0.18f * Mathf.Sin(Time.time * (_state == State.Ardiente ? 8f : 3.4f));
                _resistencias.color = ColorResistencia(pulso);
            }

            ActualizarResalte();
        }

        /// <summary>
        /// RESALTE del aparato enfocado (fix playtest 7: sustituye al prompt de
        /// texto permanente como señal de "puedes actuar aquí" — ver
        /// MachineFocus.MostrarPromptE). Alfa 0 sin foco; con foco, late entre
        /// 0.40 y 0.80. Se interpola con MoveTowards en vez de asignar el
        /// objetivo directamente para que un objetivo que oscila en cada frame
        /// (el propio latido) y las entradas/salidas de foco no produzcan
        /// parpadeos bruscos. Sin allocs: Color es struct.
        /// </summary>
        private void ActualizarResalte()
        {
            if (_resalte == null) return;
            float objetivo = EstaEnfocada() ? 0.60f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
            _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
        }

        private Color ColorResistencia(float pulso)
        {
            switch (_state)
            {
                case State.Ardiente: return new Color(1f, 0.52f * pulso, 0.22f * pulso, 1f);
                case State.Templada: return new Color(1f * pulso, 0.58f * pulso, 0.16f * pulso, 1f);
                default: return new Color(0.30f, 0.26f, 0.25f, 1f);
            }
        }

        private string StateLabel()
        {
            if (_state == State.Ardiente) return "ARDIENTE";
            if (_state == State.Templada) return "TEMPLADA";
            return "APAGADA";
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            // (fix playtest 6) Salida temprana: si el aprendiz está fuera de los
            // dos anillos, no hay nada que dibujar — ni siquiera Preparar().
            float cercaniaEstado = UiStyles.Cercania(_centroChasis, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercaniaNombre = UiStyles.Cercania(_centroChasis, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercaniaEstado <= 0f && cercaniaNombre <= 0f) return;

            // Aprendizaje: una vez el aprendiz entra de lleno en el anillo de
            // nombre, la placa queda "conocida" para el resto de la partida y
            // su chapa de nombre deja de dibujarse (fix playtest 6).
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();

            Color color = _state == State.Ardiente ? UiStyles.Peligro
                        : _state == State.Templada ? UiStyles.Aviso
                        : UiStyles.TextoTenue;

            // 1) Anillo de ESTADO: solo mientras trabaja, y SOLO el estado — nunca
            //    el nombre del aparato aquí (eso es información de reconocimiento,
            //    no de "¿dejé esto encendido?").
            if (_state != State.Off && _chapaEstado != null)
            {
                UiStyles.PlacaMundo(_centroChasis, _chapaEstado, color, -UiStyles.S(17f), cercaniaEstado);
            }

            // 2) Anillo de NOMBRE: solo hasta que el aprendiz ya sabe qué es esto.
            if (!_yaConocida)
            {
                UiStyles.PlacaMundo(_centroChasis, ChapaNombre, UiStyles.TextoTenue, -UiStyles.S(34f), cercaniaNombre);
            }

            // 3) PROMPT: (fix playtest 7) además de foco + manos libres, solo
            //    las dos primeras veces del taller (MachineFocus.MostrarPromptE);
            //    a partir de ahí la única señal de "puedes actuar aquí" es el
            //    RESALTE dorado (ver ActualizarResalte), no un texto permanente.
            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centroChasis, "E — regular el fuego", UiStyles.Oro, -UiStyles.S(51f), cercaniaNombre);
            }
        }
    }
}
