using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Piedra fría: mismo patrón que HeatPlate pero enfriando (raw 20) las
    /// celdas justo encima suya. Dos estados: Off / Fría, alternados con E.
    ///
    /// LIMITACIÓN: igual que HeatPlate, escribe _sim.Grid.temp[] directamente
    /// porque no podemos editar Sim/. TODO(Alkahest): canalizar por una API
    /// del sim de cara a netcode.
    /// </summary>
    public sealed class ChillStone : MonoBehaviour
    {
        private enum State { Off = 0, Frio = 1 }

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const float ProximityRange = 3f;
        private const byte ColdRaw = 20;
        private const int TempStepPerTick = 5;

        private AlkahestSim _sim;
        private Transform _player;
        private int _cellX0, _cellX1, _plateRow;
        private State _state = State.Off;
        private float _accumulator;

        private SpriteRenderer _bar;

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
        }

        private void BuildVisual()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "AlkahestChillStoneTex" };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            var go = new GameObject("Bar");
            go.transform.SetParent(transform, false);
            _bar = go.AddComponent<SpriteRenderer>();
            _bar.sprite = sprite;
            _bar.sortingOrder = 20;

            Vector3 worldA = _sim.CellToWorld(new Vector2Int(_cellX0, _plateRow));
            Vector3 worldB = _sim.CellToWorld(new Vector2Int(_cellX1, _plateRow));
            float width = worldB.x - worldA.x + SimRenderer.CellWorldSize;
            float centerX = (worldA.x + worldB.x) * 0.5f;
            transform.position = new Vector3(centerX, worldA.y, 0f);
            go.transform.localScale = new Vector3(width, 0.06f, 1f);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan la piedra.

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && IsPlayerNear())
            {
                _state = _state == State.Off ? State.Frio : State.Off;
                UpdateVisualTint();
                Debug.Log($"[Alkahest] Piedra fría -> {StateLabel()}");
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
        }

        private bool IsPlayerNear()
        {
            if (_player == null) return false;
            return (_player.position - transform.position).sqrMagnitude <= ProximityRange * ProximityRange;
        }

        private void ApplyColdTick()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            for (int x = _cellX0; x <= _cellX1; x++)
            {
                for (int dy = 1; dy <= 2; dy++)
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
            if (_bar == null) return;
            _bar.color = _state == State.Frio ? new Color(0.55f, 0.85f, 1f, 1f) : new Color(0.4f, 0.4f, 0.42f, 1f);
        }

        private string StateLabel() => _state == State.Frio ? "FRÍA" : "APAGADA";

        private void OnGUI()
        {
            if (_sim == null || !IsPlayerNear()) return;
            Vector3 screen = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position) : Vector3.zero;
            if (screen.z <= 0f) return;
            Rect r = new Rect(screen.x - 80f, Screen.height - screen.y - 30f, 160f, 22f);
            GUI.Label(r, $"E: piedra [{StateLabel()}]");
        }
    }
}
