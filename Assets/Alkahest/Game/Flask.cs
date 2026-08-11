using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// El frasco del aprendiz: aspira (LMB) y vierte (RMB) celdas de
    /// material de la simulación, y puede vaciarse de golpe (Q / botón
    /// central). Guarda hasta <see cref="Capacity"/> celdas como conteos
    /// por materialId.
    ///
    /// Nota de determinismo/netcode: TODA mutación de la grilla pasa por
    /// AlkahestSim.Paint (nunca acceso directo a CellGrid), tal y como
    /// exige el resto del proyecto.
    /// </summary>
    [RequireComponent(typeof(ApprenticeController))]
    public sealed class Flask : MonoBehaviour
    {
        public const int Capacity = 900;

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;

        private const int SuckRadius = 4;
        private const int SuckRatePerTick = 30;
        private const int PourRadius = 2;
        private const int PourRatePerTick = 20;
        private const int DumpRadius = 4;
        private const float ReachWorld = 6f; // unidades de mundo de alcance máximo desde el aprendiz.

        private AlkahestSim _sim;
        private ApprenticeController _apprentice;

        private readonly int[] _counts = new int[256];
        private int _total;
        private byte[] _pourOrder; // ids 1..255 del universo, ordenados por densidad descendente (calculado una sola vez).

        private float _accumulator;
        private bool _hasCursor;
        private Vector2Int _cursorCell;

        private SpriteRenderer _carryVisual;

        public int Total => _total;
        public int GetCount(byte matId) => _counts[matId];

        private void Awake()
        {
            _apprentice = GetComponent<ApprenticeController>();
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim)
        {
            _sim = sim;
            BuildPourOrder();
            BuildCarryVisual();
        }

        private void BuildPourOrder()
        {
            int n = MaterialId.Count - 1;
            _pourOrder = new byte[n];
            for (int i = 0; i < n; i++) _pourOrder[i] = (byte)(i + 1); // salta Empty (0)

            // Insertion sort por densidad descendente: N es pequeño (~12) y esto
            // se ejecuta una única vez, así que no hace falta nada más elaborado.
            for (int i = 1; i < n; i++)
            {
                byte key = _pourOrder[i];
                short keyDensity = _sim.Universe.Get(key).density;
                int j = i - 1;
                while (j >= 0 && _sim.Universe.Get(_pourOrder[j]).density < keyDensity)
                {
                    _pourOrder[j + 1] = _pourOrder[j];
                    j--;
                }
                _pourOrder[j + 1] = key;
            }
        }

        private void BuildCarryVisual()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "AlkahestFlaskCarryTex" };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            var go = new GameObject("FlaskCarryVisual");
            go.transform.SetParent(transform, false);
            _carryVisual = go.AddComponent<SpriteRenderer>();
            _carryVisual.sprite = sprite;
            _carryVisual.sortingOrder = 60;
            _carryVisual.color = new Color(1f, 1f, 1f, 0f);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan el frasco.

            var mouse = Mouse.current;
            var kb = Keyboard.current;
            bool wantSuck = mouse != null && mouse.leftButton.isPressed;
            bool wantPour = mouse != null && mouse.rightButton.isPressed;
            bool wantDump = (mouse != null && mouse.middleButton.wasPressedThisFrame)
                            || (kb != null && kb.qKey.wasPressedThisFrame);

            _hasCursor = TryGetCursorCell(out _cursorCell);

            if (wantDump && _hasCursor)
            {
                DumpAll(_cursorCell);
            }

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                if (_hasCursor)
                {
                    if (wantSuck) TickSuck(_cursorCell);
                    else if (wantPour) TickPour(_cursorCell);
                }
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            UpdateCarryVisual();
        }

        private bool TryGetCursorCell(out Vector2Int cell)
        {
            cell = default;
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return false;

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter)) return false;

            Vector3 world = ray.GetPoint(enter);
            cell = _sim.WorldToCell(world);
            return CellGrid.InBounds(cell.x, cell.y);
        }

        // ---------------------------------------------------------------------------------
        // Aspirar (LMB mantenido).
        // ---------------------------------------------------------------------------------
        private void TickSuck(Vector2Int cursor)
        {
            if (_total >= Capacity) return;

            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();
            int budget = SuckRatePerTick;

            // Anillos de distancia entera creciente desde el cursor: sensación de
            // "aspirado" que vacía primero las celdas más cercanas al centro.
            for (int r = 0; r <= SuckRadius && budget > 0; r++)
            {
                for (int dy = -r; dy <= r && budget > 0; dy++)
                {
                    int y = cursor.y + dy;
                    for (int dx = -r; dx <= r && budget > 0; dx++)
                    {
                        int d2 = dx * dx + dy * dy;
                        if (Mathf.RoundToInt(Mathf.Sqrt(d2)) != r) continue;

                        int x = cursor.x + dx;
                        if (!CellGrid.InBounds(x, y)) continue;

                        int cdx = x - apprenticeCell.x, cdy = y - apprenticeCell.y;
                        if (cdx * cdx + cdy * cdy > reachCellsSq) continue;

                        byte matId = (byte)_sim.SampleMaterial(x, y);
                        if (matId == MaterialId.Empty) continue;
                        if (_sim.Universe.Get(matId).archetype == MaterialArchetype.StaticSolid) continue;

                        _sim.Paint(x, y, 0, MaterialId.Empty);
                        _counts[matId]++;
                        _total++;
                        budget--;

                        if (_total >= Capacity) return;
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Verter (RMB mantenido).
        // ---------------------------------------------------------------------------------
        private void TickPour(Vector2Int cursor)
        {
            if (_total <= 0) return;

            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();
            int budget = PourRatePerTick;

            // Materiales más "pesados" (mayor densidad) primero, como pide el diseño.
            for (int i = 0; i < _pourOrder.Length && budget > 0; i++)
            {
                byte matId = _pourOrder[i];
                if (_counts[matId] <= 0) continue;
                PourMaterial(matId, cursor, apprenticeCell, reachCellsSq, PourRadius, ref budget);
            }
        }

        private void PourMaterial(byte matId, Vector2Int cursor, Vector2Int apprenticeCell, float reachCellsSq, int radius, ref int budget)
        {
            for (int r = 0; r <= radius && budget > 0 && _counts[matId] > 0; r++)
            {
                for (int dy = -r; dy <= r && budget > 0 && _counts[matId] > 0; dy++)
                {
                    int y = cursor.y + dy;
                    for (int dx = -r; dx <= r && budget > 0 && _counts[matId] > 0; dx++)
                    {
                        int d2 = dx * dx + dy * dy;
                        if (Mathf.RoundToInt(Mathf.Sqrt(d2)) != r) continue;

                        int x = cursor.x + dx;
                        if (!CellGrid.InBounds(x, y)) continue;

                        int cdx = x - apprenticeCell.x, cdy = y - apprenticeCell.y;
                        if (cdx * cdx + cdy * cdy > reachCellsSq) continue;

                        if (_sim.SampleMaterial(x, y) != MaterialId.Empty) continue;

                        _sim.Paint(x, y, 0, matId);
                        _counts[matId]--;
                        _total--;
                        budget--;
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Vaciar de golpe (Q / botón central).
        // ---------------------------------------------------------------------------------
        private void DumpAll(Vector2Int cursor)
        {
            if (_total <= 0) return;

            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();
            int cdx0 = cursor.x - apprenticeCell.x, cdy0 = cursor.y - apprenticeCell.y;
            if (cdx0 * cdx0 + cdy0 * cdy0 > reachCellsSq) return; // fuera de alcance: no se derrama nada.

            for (int i = 0; i < _pourOrder.Length; i++)
            {
                byte matId = _pourOrder[i];
                if (_counts[matId] <= 0) continue;
                int budget = int.MaxValue;
                PourMaterial(matId, cursor, apprenticeCell, reachCellsSq, DumpRadius, ref budget);
            }

            // Vaciado instantáneo garantizado: lo que no cupo en celdas vacías cercanas se pierde.
            ClearFlask();
        }

        private void ClearFlask()
        {
            for (int i = 0; i < _pourOrder.Length; i++) _counts[_pourOrder[i]] = 0;
            _total = 0;
        }

        private float ReachCellsSq()
        {
            float reachCells = ReachWorld / SimRenderer.CellWorldSize;
            return reachCells * reachCells;
        }

        // ---------------------------------------------------------------------------------
        // Visual: un pequeño punto de color en CarryAnchor con lo que se lleva.
        // ---------------------------------------------------------------------------------
        private void UpdateCarryVisual()
        {
            if (_carryVisual == null) return;

            if (_total <= 0)
            {
                _carryVisual.color = new Color(1f, 1f, 1f, 0f);
                return;
            }

            Vector3 anchor = _apprentice != null ? _apprentice.CarryAnchor : transform.position;
            _carryVisual.transform.position = new Vector3(anchor.x, anchor.y, anchor.z - 0.02f);

            float frac = Mathf.Clamp01((float)_total / Capacity);
            _carryVisual.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.28f, frac);

            float r = 0f, g = 0f, b = 0f;
            for (int i = 0; i < _pourOrder.Length; i++)
            {
                byte matId = _pourOrder[i];
                int c = _counts[matId];
                if (c <= 0) continue;
                Color32 col = _sim.Universe.Get(matId).baseColor;
                float wgt = (float)c / _total;
                r += col.r / 255f * wgt;
                g += col.g / 255f * wgt;
                b += col.b / 255f * wgt;
            }
            _carryVisual.color = new Color(r, g, b, 0.9f);
        }
    }
}
