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
            SimLevelBuilder.BuildTestLevel(_grid);
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
