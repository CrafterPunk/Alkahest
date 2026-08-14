using System;
using UnityEngine;
using Alkahest.Sim;

namespace Alkahest
{
    /// <summary>
    /// Orquestador de la simulación: crea el Universe/CellGrid/SimStepper,
    /// hace avanzar la simulación a 30Hz fijos (con un acumulador de
    /// Time.deltaTime, máx. 2 pasos por frame para no entrar en espiral de
    /// muerte si el frame tarda demasiado) y expone la API pública que
    /// usará el resto del juego (pintar materiales, samplear celdas,
    /// convertir mundo↔celda).
    /// </summary>
    [RequireComponent(typeof(SimRenderer))]
    public sealed class AlkahestSim : MonoBehaviour
    {
        [Tooltip("0 = elegir una seed aleatoria al arrancar.")]
        [SerializeField] private int seed = 0;

        /// <summary>
        /// Seed a usar en el PRÓXIMO Start() de este componente, fijada por
        /// Game/DayCycle.cs justo antes de recargar la escena (Título ->
        /// "Entrar al taller", o "Reintentar mismo universo"/"Nuevo
        /// universo" desde la pantalla final). Se consume una sola vez
        /// (vuelve a null tras leerse) para no afectar futuras recargas que
        /// no la fijen explícitamente. null = usar el campo `seed` del
        /// inspector (0 en ese caso = aleatoria, comportamiento de siempre).
        /// </summary>
        public static int? NextRunSeed;

        private const float FixedDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;

        private Universe _universe;
        private CellGrid _grid;
        private SimStepper _stepper;
        private SimRenderer _renderer;

        private float _accumulator;

        public Universe Universe => _universe;
        public CellGrid Grid => _grid;
        public SimStepper Stepper => _stepper;
        public SimRenderer Renderer => _renderer;

        /// <summary>
        /// Pausa la simulación (deja de consumir el acumulador de tiempo /
        /// dar pasos de <see cref="SimStepper"/>) sin tocar Time.timeScale.
        /// Usado por Game/DayCycle.cs durante los overlays de jornada
        /// (Título, intro de día, fin de día, pantalla final) para congelar
        /// el mundo mientras se muestra un menú. El renderizado (RenderFrame)
        /// también se salta mientras está en pausa: la textura simplemente
        /// deja de refrescarse, congelando el último frame visible.
        /// </summary>
        public bool Paused { get; set; }

        private void Awake()
        {
            _renderer = GetComponent<SimRenderer>();
        }

        private void Start()
        {
            if (NextRunSeed.HasValue)
            {
                seed = NextRunSeed.Value;
                NextRunSeed = null;
                Debug.Log($"[ChaosAlchemy] Seed fijada por DayCycle para esta run: {seed}");
            }
            else if (seed == 0)
            {
                seed = Environment.TickCount;
                Debug.Log($"[Alkahest] Seed no especificada, usando seed aleatoria: {seed}");
            }

            _universe = Universe.Create(seed);
            _grid = new CellGrid();
            // (playtest 21, EL PIVOT) La partida arranca en el CUARTO ÍNTIMO,
            // no en el taller clásico -- "el cuarto íntimo pasa a ser EL
            // juego", decisión de Cesar, CONTRATO_PIVOT.md. `BuildTestLevel`
            // NO se borra (el taller grande sigue entero, solo que ahora
            // ENTERRADO bajo la piedra que rellena `BuildCuartoIntimo`): la
            // rama existe aquí, en el ÚNICO sitio del proyecto donde se
            // decide qué plano construir, para el día en que el taller
            // clásico vuelva a excavarse de verdad en vez de generarse de
            // fábrica ya abierto.
            SimLevelBuilder.BuildCuartoIntimo(_grid);
            _stepper = new SimStepper(_universe, _grid);

            if (_renderer == null)
            {
                Debug.LogError("[Alkahest] AlkahestSim requiere un componente SimRenderer en el mismo GameObject.");
                enabled = false;
                return;
            }

            _renderer.Init(_universe, _grid);

            Debug.Log($"[Alkahest] Universo creado con seed {seed}. Grid {CellGrid.W}x{CellGrid.H}, chunks {CellGrid.ChunksX}x{CellGrid.ChunksY}.");
            Debug.Log($"[Alkahest] Edicto de este universo ({_universe.ActiveEdicto}): {_universe.EdictoDescripcion}");
        }

        private void Update()
        {
            if (_stepper == null || Paused) return;

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= FixedDt && steps < MaxStepsPerFrame)
            {
                _stepper.Step();
                _accumulator -= FixedDt;
                steps++;
            }

            // Si nos quedamos muy atrás (editor pausado, spike grande...) no
            // dejamos que el acumulador crezca sin límite.
            if (_accumulator > FixedDt * MaxStepsPerFrame)
            {
                _accumulator = FixedDt * MaxStepsPerFrame;
            }

            if (steps > 0)
            {
                _renderer.RenderFrame(_stepper.Tick);
            }
        }

        // ---------------------------------------------------------------------------------
        // API pública para gameplay / dev tools.
        // ---------------------------------------------------------------------------------

        /// <summary>Id de material en (x,y), o Empty si está fuera de rango.</summary>
        public int SampleMaterial(int x, int y)
        {
            if (_grid == null || !CellGrid.InBounds(x, y)) return MaterialId.Empty;
            return _grid.GetMat(x, y);
        }

        /// <summary>Temperatura "raw" (0..255) en (x,y), o la ambiente si está fuera de rango. Ver CellGrid.RawToC.</summary>
        public byte SampleTempRaw(int x, int y)
        {
            if (_grid == null || !CellGrid.InBounds(x, y)) return CellGrid.AmbientRaw;
            return _grid.temp[CellGrid.Idx(x, y)];
        }

        /// <summary>Pinta un disco de radio `radius` centrado en (x,y) con el material indicado.</summary>
        public void Paint(int x, int y, int radius, byte materialId)
        {
            if (_grid == null || _stepper == null) return;
            if (radius < 0) radius = 0;
            int r2 = radius * radius;

            for (int dy = -radius; dy <= radius; dy++)
            {
                int py = y + dy;
                if (py <= 0 || py >= CellGrid.H - 1) continue; // no pintar sobre el borde
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > r2) continue;
                    int px = x + dx;
                    if (px <= 0 || px >= CellGrid.W - 1) continue;

                    _grid.SetCell(px, py, materialId);
                    _grid.WakeChunk(px, py, _stepper.Tick);
                }
            }
        }

        /// <summary>
        /// Pinta UNA celda con material Y temperatura. Existe porque el Frasco
        /// del aprendiz conserva el frío/calor de lo que aspira (ver Game/Flask.cs):
        /// sin esto, verter hielo en la Tolva entregaba una celda a temperatura
        /// AMBIENTE y los encargos "algo helado" / "algo que queme al tacto"
        /// eran literalmente imposibles de cumplir.
        ///
        /// Igual que <see cref="Paint"/>, nunca escribe sobre el borde del mundo
        /// y despierta el chunk afectado.
        ///
        /// NO TOCAR el comportamiento de este método para arreglar el fix de
        /// playtest 13 de más abajo (<see cref="PaintStable"/>): Flask ya pasa
        /// la temperatura MEDIA correcta aquí, y esa ruta está validada desde
        /// el playtest 4. El fix de "materia creada de la nada nace inestable"
        /// vive en un método aparte a propósito.
        /// </summary>
        public void PaintCell(int x, int y, byte materialId, byte tempRaw)
        {
            if (_grid == null || _stepper == null) return;
            if (x <= 0 || x >= CellGrid.W - 1 || y <= 0 || y >= CellGrid.H - 1) return;

            int idx = CellGrid.Idx(x, y);
            _grid.SetCell(idx, materialId);
            _grid.temp[idx] = tempRaw;
            _grid.WakeChunk(x, y, _stepper.Tick);
        }

        // =====================================================================
        // (fix playtest 13) "Al seleccionar hielo, tiro agua."
        // =====================================================================
        // Diagnóstico confirmado leyendo Sim/SimStepper.cs (ApplyPhase) y
        // Sim/Universe.cs (mats[MaterialId.Ice]):
        //   - `Paint`/`SetCell` NUNCA tocan CellGrid.temp: una celda pintada
        //     hereda la temperatura que hubiera antes ahí, que en la enorme
        //     mayoría de casos es CellGrid.AmbientRaw (70 raw = 20°C, valor de
        //     partida de TODO el grid en el constructor de CellGrid).
        //   - El Hielo define `meltsAt = CToRaw(waterFreezeC + 5)`, y
        //     `waterFreezeC` varía por seed en [-20, 15] (Universe.Create).
        //     Eso deja `meltsAt` en el rango raw [52, 70] en CUALQUIER seed.
        //   - `ApplyPhase` funde con `t >= meltsAt` (SimStepper.cs línea ~397).
        //     Como AmbientRaw = 70 es el EXTREMO SUPERIOR de ese rango, la
        //     condición `70 >= meltsAt` es SIEMPRE verdadera, sin excepción de
        //     seed: el Hielo pintado con la paleta se funde a Agua en el
        //     primerísimo tick. Diagnóstico del reporte CONFIRMADO con números
        //     reales, no solo plausible.
        //
        // Arreglo GENERAL (no un parche solo para Hielo): al pintar materia de
        // la nada, la celda debe nacer a una temperatura en la que ESE
        // material sea estable, derivada de su propia MaterialDef. Ver
        // StableBirthTempRaw para el cálculo completo y por qué NO hacía
        // falta tocar Agua/Cristal/Vivium/etc (ya son estables en ambiente en
        // todo seed, la cuenta está en el comentario del método).
        //
        // Vive en un método NUEVO (PaintStable) y no en Paint/PaintCell a
        // propósito: Flask.cs usa Paint (para vaciar, MaterialId.Empty no
        // tiene transiciones así que da igual) y PaintCell (para verter,
        // donde la temperatura correcta es la del Frasco, no la de
        // estabilidad del material) -- ninguno de los dos caminos que usa
        // Flask debía cambiar de comportamiento. Comprobado con grep que los
        // únicos llamantes de Paint/PaintCell/PaintRect son Flask,
        // MasterSupplies, DeliveryChute, Dispenser y DevPalette (este último,
        // el único que pasa a usar PaintStable para el pincel; su borrador
        // sigue en Paint(..., MaterialId.Empty), sin cambios).
        // =====================================================================

        /// <summary>Colchón, en raw (1 raw = 2°C, ver CellGrid.RawToC), entre la temperatura de nacimiento corregida y el umbral de transición que la disparó. 10 raw = 20°C: de sobra para que el redondeo de CToRaw o el primer paso de difusión no la devuelvan al otro lado en el mismo tick.</summary>
        private const int StableBirthMarginRaw = 10;

        /// <summary>
        /// Temperatura "raw" a la que debe nacer una celda pintada de la nada
        /// para que <paramref name="def"/> sea ESTABLE justo al nacer (fix
        /// playtest 13, ver el bloque de comentario de arriba). Vale para
        /// CUALQUIER material, no solo Hielo:
        ///
        ///  - Sin ninguna transición de fase activa (meltsAt/boilsAt/
        ///    freezesAt/condensesAt en su sentinel "nunca"): AMBIENTE. Caso de
        ///    Stone/Sand/Oil/Nutrient/Vivium/Azoth/CrystalSeed/Slime/Acid y de
        ///    los subproductos Fire/Smoke/Ash (vida corta por gasLifetime,
        ///    pero sin transición de fase que ambiente pueda disparar).
        ///  - Con una cota SUPERIOR activa (meltsAt y/o boilsAt: ApplyPhase
        ///    las dispara con `t >= umbral`) que AMBIENTE ya cruzaría: nace
        ///    holgadamente por DEBAJO de la más baja de esas cotas. Único caso
        ///    real en el roster: Hielo (meltsAt en raw [52,70], ambiente=70
        ///    siempre lo cruza) -- nace en `meltsAt - margen`, es decir,
        ///    siempre 20°C por debajo de SU PROPIO punto de fusión, sea cual
        ///    sea la seed.
        ///  - Con una cota INFERIOR activa (freezesAt y/o condensesAt:
        ///    `t <= umbral`) que AMBIENTE ya cruzaría: nace holgadamente por
        ///    ENCIMA de la más alta. Único caso real: Vapor (condensesAt =
        ///    CToRaw(waterBoilC-40), raw [80,99] según seed, siempre por
        ///    encima de ambiente=70) -- nace en `condensesAt + margen`.
        ///  - Si AMBIENTE ya cae DENTRO de la banda (el caso normal: Agua
        ///    entre freezesAt[50,67] y boilsAt[100,119], ambiente=70 nunca
        ///    cruza ninguno de los dos en ningún seed; Cristal con
        ///    meltsAt=CToRaw(300)=210, muy por encima de 70): se deja AMBIENTE
        ///    tal cual. No hay razón para mover una temperatura que ya
        ///    funciona, y el panel de hover de DevPalette sigue leyendo "20°C"
        ///    para casi todo, como siempre.
        ///
        /// La comparación de "¿ambiente cruza el umbral?" es la MISMA que usa
        /// SimStepper.ApplyPhase (`>=`/`<=`, no una versión "por si acaso" con
        /// margen ya incluido): así el margen solo se aplica cuando hace falta
        /// corregir, y Agua/Cristal no se mueven un solo raw en ningún seed.
        /// </summary>
        private static byte StableBirthTempRaw(MaterialDef def)
        {
            int lower = int.MinValue; // cota inferior activa MÁS ALTA (freezesAt / condensesAt)
            if (def.freezesAt != short.MinValue) lower = Math.Max(lower, def.freezesAt);
            if (def.condensesAt != short.MinValue) lower = Math.Max(lower, def.condensesAt);

            int upper = int.MaxValue; // cota superior activa MÁS BAJA (meltsAt / boilsAt)
            if (def.meltsAt != short.MaxValue) upper = Math.Min(upper, def.meltsAt);
            if (def.boilsAt != short.MaxValue) upper = Math.Min(upper, def.boilsAt);

            int candidate = CellGrid.AmbientRaw;

            bool violaSuperior = upper != int.MaxValue && candidate >= upper;
            bool violaInferior = lower != int.MinValue && candidate <= lower;

            if (violaSuperior) candidate = upper - StableBirthMarginRaw;
            else if (violaInferior) candidate = lower + StableBirthMarginRaw;

            if (candidate < 0) candidate = 0;
            if (candidate > 255) candidate = 255;
            return (byte)candidate;
        }

        /// <summary>
        /// Igual que <see cref="Paint"/> (disco de radio `radius`), pero la
        /// celda nace a la temperatura de estabilidad de <paramref
        /// name="materialId"/> (ver <see cref="StableBirthTempRaw"/>) en vez
        /// de heredar lo que hubiera antes en la celda: materia creada DE LA
        /// NADA debe nacer siendo lo que la creó pretendía, no otra cosa un
        /// tick después (fix playtest 13, "pintar hielo produce agua").
        ///
        /// (playtest 17) YA NO ES "SOLO PARA LA PALETA DE DEV", como decía
        /// esta línea: `Game/Dispenser.EmitTick` es ahora el segundo
        /// consumidor, y por el mismo motivo — un grifo también crea materia
        /// de la nada, y con `Paint` el agua recién salida heredaba la
        /// temperatura del hueco (si la boquilla se había enfriado alguna vez,
        /// nacía congelada). REGLA GENERAL: si algo INTRODUCE materia en el
        /// mundo en vez de moverla, usa esto, no `Paint`. `Paint`/`PaintCell`/
        /// `PaintRect` siguen siendo lo correcto para lo que MUEVE materia que
        /// ya existía y lleva su propia temperatura consigo (Flask al verter,
        /// DeliveryChute, MasterSupplies).
        /// </summary>
        public void PaintStable(int x, int y, int radius, byte materialId)
        {
            if (_grid == null || _stepper == null || _universe == null) return;
            if (radius < 0) radius = 0;
            int r2 = radius * radius;
            byte tempRaw = StableBirthTempRaw(_universe.Get(materialId));

            for (int dy = -radius; dy <= radius; dy++)
            {
                int py = y + dy;
                if (py <= 0 || py >= CellGrid.H - 1) continue; // no pintar sobre el borde
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx * dx + dy * dy > r2) continue;
                    int px = x + dx;
                    if (px <= 0 || px >= CellGrid.W - 1) continue;

                    int idx = CellGrid.Idx(px, py);
                    _grid.SetCell(idx, materialId);
                    _grid.temp[idx] = tempRaw;
                    _grid.WakeChunk(px, py, _stepper.Tick);
                }
            }
        }

        /// <summary>
        /// Rellena un rectángulo de celdas con un material. Usado por las
        /// "muestras del Maestro" de la jornada 2 (Game/MasterSupplies.cs) para
        /// dejar un saquito de semilla de cristal sobre el estante con una
        /// cantidad exacta y predecible (un disco de <see cref="Paint"/> no
        /// permite pedir "60 celdas").
        /// </summary>
        public void PaintRect(int x0, int y0, int width, int height, byte materialId)
        {
            if (_grid == null || _stepper == null) return;
            for (int y = y0; y < y0 + height; y++)
            {
                if (y <= 0 || y >= CellGrid.H - 1) continue;
                for (int x = x0; x < x0 + width; x++)
                {
                    if (x <= 0 || x >= CellGrid.W - 1) continue;
                    _grid.SetCell(CellGrid.Idx(x, y), materialId);
                    _grid.WakeChunk(x, y, _stepper.Tick);
                }
            }
        }

        public Vector2Int WorldToCell(Vector3 worldPos)
        {
            int cx = Mathf.FloorToInt(worldPos.x / SimRenderer.CellWorldSize);
            int cy = Mathf.FloorToInt(worldPos.y / SimRenderer.CellWorldSize);
            return new Vector2Int(cx, cy);
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            float wx = (cell.x + 0.5f) * SimRenderer.CellWorldSize;
            float wy = (cell.y + 0.5f) * SimRenderer.CellWorldSize;
            return new Vector3(wx, wy, 0f);
        }

        /// <summary>Fuerza un único tick de simulación + redibujado, ignorando el acumulador de Time.deltaTime. Pensado para el modo "single-step" de las dev tools.</summary>
        public void StepOnce()
        {
            if (_stepper == null || _renderer == null) return;
            _stepper.Step();
            _renderer.RenderFrame(_stepper.Tick);
        }
    }
}
