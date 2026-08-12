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
    ///  · RÓTULO FIJO Y PEQUEÑO, anclado al labio de la bandeja (fix playtest 7:
    ///    antes colgaba de <c>_centroBloque</c>, el bloque EMPOTRADO BAJO EL
    ///    SUELO de la bandeja, así que quedaba por debajo y a un lado de donde
    ///    el jugador realmente mira) y nunca dentro de ella (que es donde el
    ///    jugador aspira).
    ///  · El prompt "E — ..." solo aparece cerca, con las manos libres, y solo
    ///    las dos primeras veces del taller (fix playtest 7: a partir de ahí lo
    ///    sustituye el RESALTE dorado del aparato enfocado, ver ActualizarResalte).
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
        /// <summary>Radio de interacción con E (fix playtest 6: bajado de 3.2 a 2.8 — el prompt asomaba antes de estar realmente al lado del aparato).</summary>
        private const float ProximityRange = 2.8f;

        // ---------------------------------------------------------------
        // ESCALA COMPARTIDA DE CERCANÍA DEL TALLER (fix playtest 6: "los
        // labels de piedra gélida... se activan aunque esté lejos"). Los
        // MISMOS valores viven, duplicados a propósito, en HeatPlate y
        // Dispenser: un único criterio de "cerca" para todo el taller.
        //  · RangoEstado: de lejos, SOLO el estado de trabajo (si lo hay).
        //  · RangoNombre: de cerca, además el nombre del aparato — pero solo
        //    hasta que el aprendiz ya lo conoce (ver _yaConocida).
        // ---------------------------------------------------------------
        private const float RangoEstadoPleno = 5.0f;
        private const float RangoEstadoDesvanece = 6.5f;
        private const float RangoNombrePleno = 2.6f;
        private const float RangoNombreDesvanece = 3.6f;

        private const byte ColdRaw = 20;
        private const int TempStepPerTick = 5;
        /// <summary>Filas enfriadas por encima del aparato (2 -> 3, misma razón que en HeatPlate: la bandeja solo tiene 6 filas útiles).</summary>
        private const int RowsAffected = 3;

        private AlkahestSim _sim;
        private Transform _player;
        private int _cellX0, _cellX1, _plateRow;
        private State _state = State.Off;
        private float _accumulator;

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

        private const string ChapaNombre = "piedra gélida";

        private SpriteRenderer _cristales;
        private Vector3 _centroBloque;

        /// <summary>
        /// (fix playtest 7) Punto medio del LABIO SUPERIOR de la bandeja fría —
        /// la superficie visible con la que el jugador trabaja — calculado UNA
        /// vez en <see cref="BuildVisual"/> a partir de las constantes de
        /// <see cref="SimLevelBuilder"/> (única fuente de verdad del plano del
        /// taller), NO de <c>_centroBloque</c>: ese es el bloque de piedra
        /// gélida empotrado bajo el suelo de la bandeja, que queda por debajo y
        /// a un lado de donde el jugador realmente mira. Todos los rótulos de
        /// <see cref="OnGUI"/> cuelgan de aquí.
        /// </summary>
        private Vector3 _anclaRotulo;

        private SpriteRenderer _resalte;
        private float _alfaResalte;

        // Foco de interacción: se deja en _centroBloque (no en el labio) A
        // PROPÓSITO. La distancia entre ambos puntos es <1 unidad de mundo —
        // muy por debajo de ProximityRange (2.8) — así que acercarse al labio
        // para trabajar la bandeja activa la máquina exactamente igual; mover
        // el foco no cambiaba nada del comportamiento reportado (el bug era
        // puramente visual, del RÓTULO) y sí arriesgaba desajustar el árbitro
        // de MachineFocus frente a aparatos vecinos sin necesidad.
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

            // (fix playtest 7) Labio superior de la bandeja fría, EN MUNDO, a
            // partir del plano del taller (SimLevelBuilder), no de las celdas
            // que recibe este componente por parámetro (que son las mismas por
            // construcción, pero el plano es la fuente de verdad declarada).
            // Punto medio en X del interior; borde superior en Y (la fila del
            // labio ocupa [ChillTrayY0+ChillTrayHeight-1, +1) en celdas, así
            // que su borde de arriba está en (ChillTrayY0+ChillTrayHeight)).
            float anclaX = (SimLevelBuilder.ChillTrayInteriorX0
                + (SimLevelBuilder.ChillTrayInteriorX1 - SimLevelBuilder.ChillTrayInteriorX0 + 1) * 0.5f) * celda;
            float anclaY = (SimLevelBuilder.ChillTrayY0 + SimLevelBuilder.ChillTrayHeight) * celda;
            _anclaRotulo = new Vector3(anclaX, anclaY, 0f);

            // Resalte de foco (fix playtest 7, ver ActualizarResalte): capa
            // DETRÁS de las demás (sortingOrder menor que Bloque=18), copia del
            // sprite principal agrandada ~15%/35% y teñida de oro; al ser mayor
            // asoma por los bordes del bloque como un halo. Se crea UNA vez
            // aquí; en Update solo se le cambia el color (cero allocs/frame).
            _resalte = MaquinariaSprites.CrearCapa(transform, "Resalte", MaquinariaSprites.BloqueGelido(spanCeldas), 16,
                anchoMundo * 1.15f, altoMundo * 1.35f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);

            MaquinariaSprites.CrearCapa(transform, "Bloque", MaquinariaSprites.BloqueGelido(spanCeldas), 18,
                anchoMundo, altoMundo);
            _cristales = MaquinariaSprites.CrearCapa(transform, "Cristales",
                MaquinariaSprites.CristalesGelidos(spanCeldas), 19, anchoMundo, altoMundo);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan la piedra.

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && EstaEnfocada())
            {
                _state = _state == State.Off ? State.Frio : State.Off;
                UpdateVisualTint();
                RebuildChapaEstado();
                MachineFocus.RegistrarUsoE(); // (fix playtest 7) el estado cambió de verdad: cuenta como un uso aprendido de E.
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
            }

            // (fix playtest 7) Antes AnimarCristales() vivía DENTRO de la rama
            // "_state != Off": el resalte de foco (que ahora gestiona esta misma
            // función) necesita latir SIEMPRE, esté la piedra encendida o no —
            // si no, acercarse a una piedra APAGADA no mostraría ninguna señal
            // de que se puede interactuar con ella.
            AnimarCristales();
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

        /// <summary>
        /// Latido lento y frío mientras hiela (opuesto al latido rápido y cálido
        /// de la placa ígnea). (fix playtest 7) Ahora se llama en TODOS los
        /// frames, así que el pulso del cristal se guarda para cuando de verdad
        /// está helando (si no, sobreescribiría el tinte mate de apagada fijado
        /// por UpdateVisualTint); el resalte del foco, en cambio, se actualiza
        /// siempre, esté o no encendida.
        /// </summary>
        private void AnimarCristales()
        {
            if (_cristales != null && _state == State.Frio)
            {
                float pulso = 0.80f + 0.20f * Mathf.Sin(Time.time * 2.2f);
                _cristales.color = new Color(0.62f * pulso + 0.20f, 0.90f * pulso, 1f, 1f);
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

        private string StateLabel() => _state == State.Frio ? "HELANDO" : "APAGADA";

        /// <summary>
        /// Reconstruye la chapa del anillo de ESTADO. Se llama SOLO al cambiar de
        /// estado (nunca desde OnGUI): ColdRaw es constante, así que el texto no
        /// cambia frame a frame y no hace falta reconstruirlo cada vez.
        /// </summary>
        private void RebuildChapaEstado()
        {
            _chapaEstado = _state == State.Frio
                ? $"HELANDO {CellGrid.RawToC(ColdRaw)}°"
                : null; // apagada: nada que anunciar de lejos.
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            // (fix playtest 6) Salida temprana: si el aprendiz está fuera de los
            // dos anillos, no hay nada que dibujar — ni siquiera Preparar().
            // (fix playtest 7) Medidas desde _anclaRotulo (el labio), no desde
            // _centroBloque: es el mismo punto del que cuelgan los rótulos, así
            // que "cerca" tiene que significar "cerca de la bandeja", no "cerca
            // del bloque empotrado bajo ella".
            float cercaniaEstado = UiStyles.Cercania(_anclaRotulo, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercaniaNombre = UiStyles.Cercania(_anclaRotulo, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercaniaEstado <= 0f && cercaniaNombre <= 0f) return;

            // Aprendizaje: una vez el aprendiz entra de lleno en el anillo de
            // nombre, la piedra queda "conocida" para el resto de la partida y
            // su chapa de nombre deja de dibujarse (fix playtest 6).
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();
            Color color = _state == State.Frio ? UiStyles.Frio : UiStyles.TextoTenue;

            // 1) Anillo de ESTADO: solo mientras hiela, y SOLO el estado — nunca
            //    el nombre del aparato aquí (eso es información de reconocimiento,
            //    no de "¿dejé esto encendido?"). (fix playtest 7) Desplazamiento
            //    HACIA ARRIBA (positivo) desde el labio: la bandeja es poco
            //    profunda (0.6 unidades de interior) y un desplazamiento hacia
            //    abajo, aunque pequeño, caía DENTRO de ella; hacia arriba cae
            //    siempre en el aire libre sobre la bandeja.
            if (_state == State.Frio && _chapaEstado != null)
            {
                UiStyles.PlacaMundo(_anclaRotulo, _chapaEstado, color, UiStyles.S(17f), cercaniaEstado);
            }

            // 2) Anillo de NOMBRE: solo hasta que el aprendiz ya sabe qué es esto.
            if (!_yaConocida)
            {
                UiStyles.PlacaMundo(_anclaRotulo, ChapaNombre, UiStyles.TextoTenue, UiStyles.S(34f), cercaniaNombre);
            }

            // 3) Prompt E: (fix playtest 7) además de foco + manos libres, solo
            //    las dos primeras veces del taller (MachineFocus.MostrarPromptE);
            //    a partir de ahí la única señal de "puedes actuar aquí" es el
            //    RESALTE dorado (ver ActualizarResalte), no un texto permanente.
            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_anclaRotulo, "E — encender el frío", UiStyles.Oro, UiStyles.S(51f), cercaniaNombre);
            }
        }
    }
}
