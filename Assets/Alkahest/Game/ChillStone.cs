using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Piedra gélida: el aparato empotrado bajo la bandeja fría del estante
    /// superior. Enfría las filas de celdas justo encima suya. Tres estados
    /// (APAGADA / FRESCA / HELANDO), ciclados con E — mismo patrón que
    /// <see cref="HeatPlate"/> (APAGADA / TEMPLADA / ARDIENTE).
    ///
    /// -----------------------------------------------------------------
    /// FRESCA (fix playtest 13)
    /// -----------------------------------------------------------------
    /// Reporte del jugador: "la placa fría parece irradiar más fuerte el frío
    /// que el calor, y tardar más en recuperar su temperatura a 0, además de
    /// tener más alcance". MEDIDO antes de tocar nada:
    ///  · Antes de este fix, ChillStone tenía UN SOLO estado activo: raw 20
    ///    (-80 °C), 50 unidades raw (100 °C) por DEBAJO de ambiente (raw 70,
    ///    20 °C). HeatPlate en cambio SIEMPRE tuvo un estado moderado
    ///    (TEMPLADA, calibrado al centro de la banda de crecimiento del
    ///    Vivium de la seed — típicamente raw ~82, solo 12 unidades / 24 °C
    ///    POR ENCIMA de ambiente) además del extremo ARDIENTE (raw 220,
    ///    +320 °C, 150 unidades / 300 °C por encima). En uso normal el
    ///    jugador cicla HeatPlate a TEMPLADA para casi todo y reserva
    ///    ARDIENTE para encender/hervir de verdad -- pero ChillStone SOLO
    ///    tenía la opción extrema, cada vez que "quería algo de frío".
    ///  · SimStepper.DiffuseTemperature (NO TOCADA, regla 9 de CLAUDE.md)
    ///    atrae cada celda hacia ambiente con un paso FIJO de ±1 raw cada
    ///    ~32 ticks (~1.07 s), no proporcional a la distancia. Eso significa
    ///    que el tiempo de vuelta a ambiente escala LINEALMENTE con cuán
    ///    lejos se empujó la celda: ~53 s desde -80 °C (raw 20, 50
    ///    unidades) frente a ~13 s desde TEMPLADA (raw 82, 12 unidades) --
    ///    el frío, en su único modo, tardaba SIEMPRE ~4x más en apagarse que
    ///    el calor en su modo de uso diario. Eso es justo lo que el jugador
    ///    describe como "tarda más en recuperar su temperatura a 0" y
    ///    "tiene más alcance" (un gradiente 4x más profundo se nota más
    ///    lejos de la fuente antes de fundirse con el ambiente).
    ///  · El propio juego define "frío" como -5 °C o menos (encargo Cold de
    ///    OrderSystem, día 2) -- -80 °C es 16x más frío que lo que el juego
    ///    considera "cumplir el encargo". Y el punto de congelación del agua
    ///    de esta seed nunca baja de -20 °C (Universe.Create, rango
    ///    acotado). -80 °C sigue siendo necesario para GARANTIZAR la
    ///    congelación/cristalización con margen amplio en cualquier seed,
    ///    pero no hacía falta como ÚNICA opción.
    ///  · VEREDICTO: asimetría real pero NO en las mecánicas compartidas
    ///    (TempStepPerTick=5 y RowsAffected=3 ya eran IGUALES en ambos
    ///    archivos, ver comentario en HeatPlate.cs) -- estaba en que a
    ///    ChillStone le faltaba el equivalente a TEMPLADA. FRESCA lo cierra:
    ///    se calibra por seed (igual que TEMPLADA) al mínimo entre el punto
    ///    de congelación del agua y el umbral de cristalización de ESTA
    ///    seed, con margen de 10 raw (20 °C) por fiabilidad frente al tirón
    ///    de vuelta a ambiente -- sigue congelando agua y permitiendo
    ///    cristalizar, pero típicamente ronda raw ~50 (-20 °C, seed neutra),
    ///    ~2.5x más cerca de ambiente que HELANDO en vez de forzar siempre
    ///    el extremo. HELANDO se conserva intacto (raw 20 / -80 °C) para
    ///    cuando el jugador SÍ quiere el resultado instantáneo y garantizado
    ///    (ver casos de uso en la doc de la clase, más abajo).
    ///
    /// LO QUE NO SE TOCÓ, a propósito: el ALCANCE geométrico (RowsAffected=3
    /// en ambos archivos, MISMO valor) y la VELOCIDAD de empuje
    /// (TempStepPerTick=5 en ambos, MISMA velocidad) ya eran simétricos.
    /// La sensación de "la placa ígnea no combate el frío tan rápido como
    /// esperaría" con las dos hornillas a ARDIENTE cerca de la bandeja fría
    /// es, medida la geometría real (Sim/SimLevelBuilder.cs, NO TOCADO), una
    /// EXPECTATIVA IMPOSIBLE: la bandeja fría vive en y=88..96 y las cubas
    /// de las hornillas terminan en su labio en y=53 -- 35 filas de aire
    /// vacío de por medio, y el material Empty NO participa en la difusión
    /// de temperatura (ver docs/SIM_NOTES.md, "Límites conocidos"). Ninguna
    /// hornilla puede calentar la bandeja fría por difusión salvo que gas o
    /// fuego de verdad vuele físicamente hasta allí arriba. Es información
    /// de diseño, no un bug: las hornillas calientan SU cuba, no la bandeja
    /// de al lado.
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
        private enum State { Off = 0, Fresca = 1, Helando = 2 }

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const float ProximityRange = 3.2f;
        private const byte HelandoRaw = 20; // ~-80 °C, extremo garantizado (ver doc de la clase).
        private const int TempStepPerTick = 5;
        /// <summary>Filas enfriadas por encima del aparato (2 -> 3, misma razón que en HeatPlate: la bandeja solo tiene 6 filas útiles).</summary>
        private const int RowsAffected = 3;

        /// <summary>
        /// (fix playtest 13) Margen de fiabilidad, en raw, entre FRESCA y el
        /// umbral real (punto de congelación del agua / cristalización) de
        /// esta seed: sin margen, el tirón hacia ambiente de
        /// SimStepper.DiffuseTemperature (±1 raw cada ~32 ticks) podría dejar
        /// una celda oscilando justo encima del umbral en vez de cruzarlo de
        /// forma fiable. 10 raw = 20 °C, mismo orden de magnitud que la
        /// histéresis de +5 °C que ya usa Ice.meltsAt y que el margen de 8 °C
        /// que ARDIENTE deja sobre la ignición máxima sorteable (ver
        /// HeatPlate.cs).
        /// </summary>
        private const int FrescaMarginRaw = 10;

        private AlkahestSim _sim;
        private Transform _player;
        private int _cellX0, _cellX1, _plateRow;
        private State _state = State.Off;
        private float _accumulator;

        /// <summary>Objetivo de FRESCA: calibrado por seed en Init() (ver doc de la clase). Valor por defecto plausible si Universe no está listo aún.</summary>
        private byte _frescaRaw = 45;

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

            // (fix playtest 13) FRESCA: mínimo entre el punto de congelación
            // del agua y el umbral de cristalización de ESTA seed, menos el
            // margen de fiabilidad -- calibrado por seed igual que
            // HeatPlate._templadaRaw se calibra a VivGrowMinRaw/MaxRaw.
            if (_sim != null && _sim.Universe != null)
            {
                int freezesAt = _sim.Universe.Get(MaterialId.Water).freezesAt;
                int limite = Mathf.Min(freezesAt, _sim.Universe.CrystallizeMaxTempRaw);
                int fresca = limite - FrescaMarginRaw;
                _frescaRaw = (byte)Mathf.Clamp(fresca, HelandoRaw + 1, CellGrid.AmbientRaw - 1);
            }

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
                CycleState();
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

        /// <summary>(fix playtest 13) Ciclo de 3 estados, mismo patrón que HeatPlate.CycleState.</summary>
        private void CycleState()
        {
            _state = (State)(((int)_state + 1) % 3);
            UpdateVisualTint();
            Debug.Log($"[ChaosAlchemy] Piedra gélida -> {StateLabel()} ({CellGrid.RawToC(TargetRaw())} °C)");
        }

        private byte TargetRaw() => _state == State.Helando ? HelandoRaw : _frescaRaw;

        private void ApplyColdTick()
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
                    int next = cur > target ? Mathf.Max(target, cur - TempStepPerTick) : Mathf.Min(target, cur + TempStepPerTick);
                    grid.temp[idx] = (byte)next;
                    grid.WakeChunk(x, y, tick);
                }
            }
        }

        private void UpdateVisualTint()
        {
            if (_cristales == null) return;
            _cristales.color = ColorCristal(1f);
        }

        /// <summary>Latido lento y frío mientras hiela (opuesto al latido rápido y cálido de la placa ígnea); FRESCA late un poco más rápido, menos urgente que HELANDO.</summary>
        private void AnimarCristales()
        {
            if (_cristales == null || _state == State.Off) return;
            float pulso = 0.80f + 0.20f * Mathf.Sin(Time.time * (_state == State.Helando ? 2.2f : 3.4f));
            _cristales.color = ColorCristal(pulso);
        }

        /// <summary>(fix playtest 13) Tres tintes, mismo patrón que HeatPlate.ColorResistencia: apagada mate, FRESCA azul suave, HELANDO azul intenso.</summary>
        private Color ColorCristal(float pulso)
        {
            switch (_state)
            {
                case State.Helando: return new Color(0.62f * pulso + 0.20f, 0.90f * pulso, 1f, 1f);
                case State.Fresca: return new Color(0.50f * pulso + 0.16f, 0.72f * pulso, 0.88f * pulso + 0.08f, 1f);
                default: return new Color(0.42f, 0.46f, 0.52f, 0.75f); // apagada: cristal mate, sin luz propia
            }
        }

        private string StateLabel()
        {
            if (_state == State.Helando) return "HELANDO";
            if (_state == State.Fresca) return "FRESCA";
            return "APAGADA";
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            bool cerca = EstaEnfocada();
            if (!cerca && _state == State.Off) return;

            UiStyles.Preparar();
            Color color = _state != State.Off ? UiStyles.Frio : UiStyles.TextoTenue;

            // (fix playtest 13) El jugador dudaba de si el frío "tiene más alcance" que el
            // calor -- RowsAffected es una constante IGUAL en este archivo y en HeatPlate.cs
            // (ambas 3), pero él no tenía forma de comprobarlo. Se añade "(alcanza N filas)"
            // SOLO cuando está cerca Y encendida (nunca un elemento permanente, regla 15) para
            // que pueda leerlo y compararlo con el mismo texto en la placa ígnea.
            string chapa = _state == State.Off
                ? "piedra gélida"
                : cerca
                    ? $"piedra gélida · {StateLabel()} {CellGrid.RawToC(TargetRaw())}° (alcanza {RowsAffected} filas)"
                    : $"piedra gélida · {StateLabel()} {CellGrid.RawToC(TargetRaw())}°";
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
