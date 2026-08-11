using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Placa calefactora: colocada bajo una cubeta, inyecta calor cada tick
    /// en las dos filas de celdas justo encima suyo. Tres intensidades
    /// (Off/Tibia/Caliente) que se ciclan pulsando E cerca de ella.
    ///
    /// LIMITACIÓN: no podemos editar Sim/, así que escribimos
    /// _sim.Grid.temp[] directamente en vez de pasar por una API dedicada
    /// del simulador. TODO(Alkahest): canalizar esta escritura por una API
    /// del sim (p.ej. AlkahestSim.InjectHeat) para que quede lista para netcode.
    /// </summary>
    public sealed class HeatPlate : MonoBehaviour
    {
        private enum State { Off = 0, Warm = 1, Hot = 2 }

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const float ProximityRange = 3f;
        private const byte WarmRaw = 140;
        private const byte HotRaw = 220;
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
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "AlkahestHeatPlateTex" };
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
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan la placa.

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && IsPlayerNear())
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
        }

        private bool IsPlayerNear()
        {
            if (_player == null) return false;
            return (_player.position - transform.position).sqrMagnitude <= ProximityRange * ProximityRange;
        }

        private void CycleState()
        {
            _state = (State)(((int)_state + 1) % 3);
            UpdateVisualTint();
            Debug.Log($"[Alkahest] Placa calefactora -> {StateLabel()}");
        }

        private void ApplyHeatTick()
        {
            byte target = _state == State.Hot ? HotRaw : WarmRaw;
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
                    int next = cur < target ? Mathf.Min(target, cur + TempStepPerTick) : Mathf.Max(target, cur - TempStepPerTick);
                    grid.temp[idx] = (byte)next;
                    grid.WakeChunk(x, y, tick);
                }
            }
        }

        private void UpdateVisualTint()
        {
            if (_bar == null) return;
            Color c;
            if (_state == State.Hot) c = new Color(1f, 0.2f, 0.1f, 1f);
            else if (_state == State.Warm) c = new Color(0.95f, 0.55f, 0.15f, 1f);
            else c = new Color(0.4f, 0.4f, 0.42f, 1f);
            _bar.color = c;
        }

        private string StateLabel()
        {
            if (_state == State.Hot) return "CALIENTE";
            if (_state == State.Warm) return "TIBIA";
            return "APAGADA";
        }

        private void OnGUI()
        {
            if (_sim == null || !IsPlayerNear()) return;
            Vector3 screen = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position) : Vector3.zero;
            if (screen.z <= 0f) return;
            Rect r = new Rect(screen.x - 80f, Screen.height - screen.y - 30f, 160f, 22f);
            GUI.Label(r, $"E: placa [{StateLabel()}]");
        }
    }
}
