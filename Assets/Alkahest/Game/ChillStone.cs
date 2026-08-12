using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Piedra gélida: el aparato empotrado bajo la bandeja fría del estante
    /// superior. Enfría (raw 20, ~-80 °C) las filas de celdas justo encima
    /// suya. Dos estados: APAGADA / HELANDO, alternados con E.
    ///
    /// Comparte con <see cref="HeatPlate"/> las tres decisiones del playtest 4:
    ///  · IDENTIDAD VISUAL PROPIA — bloque de roca escarchada con AGUJAS DE
    ///    CRISTAL azules que brotan de él y laten cuando trabaja (sprites
    ///    generados en Game/MaquinariaSprites.cs), en lugar de una barra de un
    ///    píxel tintada de azul.
    ///  · RÓTULO FIJO Y PEQUEÑO, atornillado bajo el aparato y nunca dentro de
    ///    la bandeja (que es donde el jugador aspira).
    ///  · El prompt "E — ..." solo aparece cerca Y con las manos libres.
    ///
    /// Es la máquina clave de dos encargos: "algo helado" (congela agua aquí y
    /// entrégala — el Frasco ahora conserva el frío, ver Game/Flask.cs) y
    /// "cristal" (azoth + semilla de cristal en FRÍO, ver Universe.Create).
    ///
    /// LIMITACIÓN: igual que HeatPlate, escribe _sim.Grid.temp[] directamente.
    /// TODO(ChaosAlchemy): canalizar por una API del sim de cara a netcode.
    /// </summary>
    public sealed class ChillStone : MonoBehaviour, IMaquinaInteractiva
    {
        private enum State { Off = 0, Frio = 1 }

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const float ProximityRange = 3.2f;
        private const byte ColdRaw = 20;
        private const int TempStepPerTick = 5;
        /// <summary>Filas enfriadas por encima del aparato (2 -> 3, misma razón que en HeatPlate: la bandeja solo tiene 6 filas útiles).</summary>
        private const int RowsAffected = 3;

        private AlkahestSim _sim;
        private Transform _player;
        private int _cellX0, _cellX1, _plateRow;
        private State _state = State.Off;
        private float _accumulator;

        private SpriteRenderer _cristales;
        private Vector3 _centroBloque;

        // Foco de interacción: ver Game/MachineFocus.cs.
        public Vector3 PuntoFoco => _centroBloque;
        public float RangoFoco => ProximityRange;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Transform player, int cellX0, int cellX1, int plateRow)
        {
            _sim = sim;
            _player = player;
            _cellX0 = cellX0;
            _cellX1 = cellX1;
            _plateRow = plateRow;

            BuildVisual();
            UpdateVisualTint();
            MachineFocus.Registrar(this);
        }

        private void OnDestroy() => MachineFocus.Olvidar(this);

        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;
            int spanCeldas = _cellX1 - _cellX0 + 1;

            int filaInferior = _plateRow - SimLevelBuilder.WallThickness + 1;
            float anchoMundo = spanCeldas * celda;
            float altoMundo = (_plateRow + 1 - filaInferior) * celda;

            float centroX = (_cellX0 + spanCeldas * 0.5f) * celda;
            float centroY = (filaInferior + (_plateRow + 1 - filaInferior) * 0.5f) * celda;
            _centroBloque = new Vector3(centroX, centroY, 0f);
            transform.position = _centroBloque;

            MaquinariaSprites.CrearCapa(transform, "Bloque", MaquinariaSprites.BloqueGelido(spanCeldas), 18,
                anchoMundo, altoMundo);
            _cristales = MaquinariaSprites.CrearCapa(transform, "Cristales",
                MaquinariaSprites.CristalesGelidos(spanCeldas), 19, anchoMundo, altoMundo);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan la piedra.

            // (fix playtest 10) E es un atajo de una sola tecla como cualquier otro del
            // proyecto: no puede robarle letras al campo de bautizar (UiStyles.
            // EscribiendoTexto) ni competir con el diario a pantalla completa, que posee
            // el input del MUNDO mientras está abierto (JournalHud.Abierto) -- el tick de
            // frío de más abajo NO se toca (el mundo sigue vivo con el libro abierto),
            // solo se calla el TOGGLE de encendido mientras se escribe o se lee.
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                _state = _state == State.Off ? State.Frio : State.Off;
                UpdateVisualTint();
                Debug.Log($"[ChaosAlchemy] Piedra gélida -> {StateLabel()}");
            }

            if (_state != State.Off)
            {
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
                {
                    ApplyColdTick();
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

                AnimarCristales();
            }
        }

        /// <summary>¿Es ESTE el aparato que el aprendiz tiene delante? (ver Game/MachineFocus.cs)</summary>
        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        private void ApplyColdTick()
        {
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
                    int next = cur > ColdRaw ? Mathf.Max(ColdRaw, cur - TempStepPerTick) : Mathf.Min(ColdRaw, cur + TempStepPerTick);
                    grid.temp[idx] = (byte)next;
                    grid.WakeChunk(x, y, tick);
                }
            }
        }

        private void UpdateVisualTint()
        {
            if (_cristales == null) return;
            _cristales.color = _state == State.Frio
                ? new Color(0.62f, 0.90f, 1f, 1f)
                : new Color(0.42f, 0.46f, 0.52f, 0.75f); // apagada: cristal mate, sin luz propia
        }

        /// <summary>Latido lento y frío mientras hiela (opuesto al latido rápido y cálido de la placa ígnea).</summary>
        private void AnimarCristales()
        {
            if (_cristales == null) return;
            float pulso = 0.80f + 0.20f * Mathf.Sin(Time.time * 2.2f);
            _cristales.color = new Color(0.62f * pulso + 0.20f, 0.90f * pulso, 1f, 1f);
        }

        private string StateLabel() => _state == State.Frio ? "HELANDO" : "APAGADA";

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            bool cerca = EstaEnfocada();
            if (!cerca && _state == State.Off) return;

            UiStyles.Preparar();
            Color color = _state == State.Frio ? UiStyles.Frio : UiStyles.TextoTenue;

            string chapa = _state == State.Frio
                ? $"piedra gélida · HELANDO {CellGrid.RawToC(ColdRaw)}°"
                : "piedra gélida";
            // (fix playtest 10) EL RÓTULO VA DEBAJO, COMO LAS PLACAS. Playtest 7 lo había
            // subido (offset positivo) porque entonces caía dentro de la bandeja -- decisión
            // equivocada según el jugador, que pide la MISMA convención que HeatPlate: mismo
            // punto de anclaje (_centroBloque = centro del bloque de piedra, calculado con la
            // idéntica fórmula que _centroChasis de HeatPlate) y mismo signo de desplazamiento
            // (NEGATIVO = abajo, ver UiStyles.PlacaMundo). Verificado con las constantes reales
            // de Sim/SimLevelBuilder.cs que SÍ hay aire libre de sobra por debajo:
            //   _centroBloque cae en la fila-celda 89.5 (ChillTrayY0=88 + 3 filas de suelo/2);
            //   el bloque ocupa las filas 88-90 (su base real, ChillTrayY0..+WallThickness-1).
            //   Por debajo, la franja bajo el rótulo (centrado en x=62, dentro de la meseta del
            //   banco BenchX0..BenchX1=1..64, techo BenchTopY=39) tiene 48 celdas de aire libre
            //   (filas 40-87) antes de tocar piedra; incluso en la franja más estrecha, bajo el
            //   muro de la Cuba A (VatAX0=72, pared hasta VatInteriorY1=53), quedan 34 celdas
            //   libres (filas 54-87). El desplazamiento de -17px/-34px (S(17f)/S(34f)) equivale
            //   a solo 3.4/6.8 celdas -- un margen de 10x-14x sobre lo que hace falta, y muy
            //   lejos (filas 88-86.1 y 88-82.7) de la bandeja (interior en filas 91-96, POR
            //   ENCIMA del bloque, nunca puede chocar bajando) o de la cuba de abajo (labio en
            //   fila 53). Nada que ajustar: los mismos números de HeatPlate ya quedan limpios.
            UiStyles.PlacaMundo(_centroBloque, chapa, color, -UiStyles.S(17f));

            if (cerca && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centroBloque, "E — encender el frío", UiStyles.Oro, -UiStyles.S(34f));
            }
        }
    }
}
