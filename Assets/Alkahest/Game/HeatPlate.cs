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
    ///    solo aparece si estás cerca Y no tienes ningún botón del ratón pulsado.
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
    ///
    /// ---------------------------------------------------------------------
    /// MEDICIÓN CONTRA ChillStone (fix playtest 13): "la placa fría parece
    /// irradiar más fuerte, tardar más en recuperarse y tener más alcance".
    /// Comprobado con números reales (detalle completo en la doc de
    /// ChillStone.cs, que es donde se hizo el fix): RowsAffected (3) y
    /// TempStepPerTick (5) YA eran IDÉNTICOS a los de ChillStone -- no hay
    /// nada que igualar aquí en alcance/velocidad de empuje. El ancho real
    /// del área calentada (VatInteriorX1-X0+1 = 52 celdas por cuba) es
    /// incluso ALGO MAYOR que el de la bandeja fría (ChillTrayInteriorX1-X0+1
    /// = 46 celdas) — en celdas absolutas, esta placa cubre más área, no
    /// menos. La asimetría real estaba en que ChillStone SOLO tenía un
    /// estado activo (el extremo, -80 °C) mientras esta clase ya ofrecía
    /// TEMPLADA como opción cercana a ambiente para el uso diario; se
    /// corrigió añadiendo FRESCA a ChillStone (ver esa clase), NO tocando
    /// nada de esta.
    ///
    /// Aparte, y esto NO se toca porque es geometría de nivel (Sim/
    /// SimLevelBuilder.cs, fuera de estos dos archivos): la bandeja fría
    /// (9 filas de alto, 6 útiles) es mucho más DELGADA que una cuba (40
    /// filas de alto, 36 útiles), así que estas 3 filas de calor cubren un
    /// 8% de la profundidad útil de la cuba (156 de ~1872 celdas) frente al
    /// 50% que las mismas 3 filas de frío cubren en la bandeja (138 de 276
    /// celdas). Es la diferencia física esperable entre "bandeja fina apoyada
    /// sobre un enfriador" y "caldero hondo sobre una hornilla que calienta
    /// desde el fondo" — no es un desequilibrio entre estos dos scripts, es
    /// el contenedor que Sim/SimLevelBuilder.cs les da a cada uno.
    ///
    /// Y algo geométricamente IMPOSIBLE que el jugador esperaba sin saberlo:
    /// esta placa calienta la cuba en la que vive (y=14..53); la bandeja fría
    /// está en y=88..96, 35 filas de aire vacío más arriba, y el material
    /// Empty no participa en la difusión de temperatura (docs/SIM_NOTES.md).
    /// Ninguna hornilla puede combatir el frío de la bandeja por difusión —
    /// son cubetas sin contacto térmico entre sí.
    /// </summary>
    public sealed class HeatPlate : MonoBehaviour, IMaquinaInteractiva
    {
        private enum State { Off = 0, Templada = 1, Ardiente = 2 }

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const float ProximityRange = 3.2f;
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

        private SpriteRenderer _resistencias;
        private Vector3 _centroChasis;

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

            MaquinariaSprites.CrearCapa(transform, "Chasis", MaquinariaSprites.ChasisPlaca(spanCeldas), 18,
                anchoMundo, altoMundo);
            _resistencias = MaquinariaSprites.CrearCapa(transform, "Resistencias",
                MaquinariaSprites.ResistenciasPlaca(spanCeldas), 19, anchoMundo, altoMundo);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan la placa.

            // (fix playtest 10) E es un atajo de una sola tecla: no puede robarle letras al
            // campo de bautizar ni competir con el diario a pantalla completa (ver el mismo
            // comentario en Game/ChillStone.cs). El calor de más abajo sigue su curso igual
            // con el libro abierto -- solo se calla el ciclo de intensidad.
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
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
            Debug.Log($"[ChaosAlchemy] Placa ígnea -> {StateLabel()} ({CellGrid.RawToC(TargetRaw())} °C)");
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

        /// <summary>Las resistencias respiran: apagadas son metal frío, templadas laten ámbar, ardientes laten blanco-naranja.</summary>
        private void AnimarResistencias()
        {
            if (_resistencias == null || _state == State.Off) return;
            float pulso = 0.82f + 0.18f * Mathf.Sin(Time.time * (_state == State.Ardiente ? 8f : 3.4f));
            _resistencias.color = ColorResistencia(pulso);
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

            bool cerca = EstaEnfocada();
            // De lejos solo se anuncia si está trabajando (una placa encendida es
            // información que el jugador necesita aunque esté al otro lado).
            if (!cerca && _state == State.Off) return;

            UiStyles.Preparar();

            Color color = _state == State.Ardiente ? UiStyles.Peligro
                        : _state == State.Templada ? UiStyles.Aviso
                        : UiStyles.TextoTenue;

            // 1) CHAPA FIJA: siempre debajo del chasis, sobre la piedra. Nunca
            //    dentro de la cuba, que es la zona de trabajo del jugador.
            // (fix playtest 13) "(alcanza N filas)" solo cerca y encendida, igual que
            // ChillStone.cs -- mismo texto, mismo número (3), para que el jugador pueda
            // comprobar por sí mismo que el alcance de ambos aparatos es idéntico.
            string chapa = _state == State.Off
                ? "placa ígnea"
                : cerca
                    ? $"placa ígnea · {StateLabel()} {CellGrid.RawToC(TargetRaw())}° (alcanza {RowsAffected} filas)"
                    : $"placa ígnea · {StateLabel()} {CellGrid.RawToC(TargetRaw())}°";
            UiStyles.PlacaMundo(_centroChasis, chapa, color, -UiStyles.S(17f));

            // 2) PROMPT: solo cerca y con las manos libres (ver doc de la clase).
            if (cerca && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centroChasis, "E — regular el fuego", UiStyles.Oro, -UiStyles.S(34f));
            }
        }
    }
}
