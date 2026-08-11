using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Grifo de pared: al activarlo (E cerca, alterna ON/OFF), emite un
    /// caudal constante de un material base en su celda de salida, solo
    /// sobre celdas vacías.
    ///
    /// M4: algunos materiales tienen un coste de Favor POR ACTIVACIÓN
    /// (<see cref="favorCostPerActivation"/>, fijado desde AlkahestGameBootstrap:
    /// Agua/Arena 0, Aceite 2, Nutrient 5). Se cobra una única vez al pasar
    /// de OFF a ON (no por tick); si no hay Favor suficiente el grifo se
    /// niega a encenderse y el label muestra "(sin Favor)" un momento.
    /// </summary>
    public sealed class Dispenser : MonoBehaviour
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const float ProximityRange = 2.5f;
        private const int EmitRatePerTick = 12;
        private const int SpoutRadius = 1;
        private const int SpoutOffsetCells = 3; // separa el caño de la pared para no emitir dentro del muro.
        private const float InsufficientFavorFlashSeconds = 1.5f;

        [Tooltip("Coste en Favor de encender este grifo (una sola vez por activación). 0 = gratis.")]
        [SerializeField] private int favorCostPerActivation = 0;

        private AlkahestSim _sim;
        private Transform _player;
        private OrderSystem _orderSystem;
        private int _spoutX, _spoutY;
        private byte _matId;
        private bool _on;
        private float _accumulator;

        private float _insufficientFavorTimer;

        private SpriteRenderer _dropIcon;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Transform player, int mountCellX, int mountCellY, byte materialId,
            OrderSystem orderSystem = null, int favorCost = 0)
        {
            _sim = sim;
            _player = player;
            _spoutX = mountCellX + SpoutOffsetCells;
            _spoutY = mountCellY;
            _matId = materialId;
            _orderSystem = orderSystem;
            favorCostPerActivation = favorCost;

            BuildVisual(mountCellX, mountCellY);
        }

        private void BuildVisual(int mountCellX, int mountCellY)
        {
            transform.position = _sim.CellToWorld(new Vector2Int(mountCellX, mountCellY));

            // Soporte: una barrita gris pegada a la pared.
            var bracketGO = new GameObject("Bracket");
            bracketGO.transform.SetParent(transform, false);
            var bracket = bracketGO.AddComponent<SpriteRenderer>();
            bracket.sprite = SolidSprite();
            bracket.color = new Color(0.45f, 0.42f, 0.4f, 1f);
            bracket.sortingOrder = 19;
            bracketGO.transform.localScale = new Vector3(0.5f, 0.18f, 1f);

            // Icono de gota, coloreado con el material que dispensa.
            var dropGO = new GameObject("Drop");
            dropGO.transform.SetParent(transform, false);
            dropGO.transform.localPosition = new Vector3(0.3f, -0.15f, 0f);
            _dropIcon = dropGO.AddComponent<SpriteRenderer>();
            _dropIcon.sprite = SolidSprite();
            _dropIcon.sortingOrder = 20;
            dropGO.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
            _dropIcon.color = _sim.Universe.Get(_matId).baseColor;
        }

        private static Sprite SolidSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "AlkahestDispenserTex" };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan el grifo.

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && IsPlayerNear())
            {
                ToggleRequested();
            }

            if (_insufficientFavorTimer > 0f)
            {
                _insufficientFavorTimer -= Time.deltaTime;
            }

            if (_on)
            {
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
                {
                    EmitTick();
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;
            }
        }

        private void ToggleRequested()
        {
            if (_on)
            {
                _on = false;
                Debug.Log($"[Alkahest] Grifo de {_sim.Universe.Get(_matId).devName} -> OFF");
                return;
            }

            if (TryPayActivationCost())
            {
                _on = true;
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName} -> ON (coste {favorCostPerActivation} Favor).");
            }
            else
            {
                _insufficientFavorTimer = InsufficientFavorFlashSeconds;
                Debug.Log($"[ChaosAlchemy] Grifo de {_sim.Universe.Get(_matId).devName}: sin Favor suficiente ({favorCostPerActivation} requerido).");
            }
        }

        private bool TryPayActivationCost()
        {
            if (favorCostPerActivation <= 0) return true;
            if (_orderSystem == null) return true; // defensivo: sin OrderSystem conectado no bloqueamos el grifo.
            return _orderSystem.SpendFavor(favorCostPerActivation);
        }

        private bool IsPlayerNear()
        {
            if (_player == null) return false;
            return (_player.position - transform.position).sqrMagnitude <= ProximityRange * ProximityRange;
        }

        private void EmitTick()
        {
            int budget = EmitRatePerTick;
            for (int dy = -SpoutRadius; dy <= SpoutRadius && budget > 0; dy++)
            {
                int y = _spoutY + dy;
                for (int dx = -SpoutRadius; dx <= SpoutRadius && budget > 0; dx++)
                {
                    if (dx * dx + dy * dy > SpoutRadius * SpoutRadius) continue;
                    int x = _spoutX + dx;
                    if (!CellGrid.InBounds(x, y)) continue;
                    if (_sim.SampleMaterial(x, y) != MaterialId.Empty) continue;

                    _sim.Paint(x, y, 0, _matId);
                    budget--;
                }
            }
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || !IsPlayerNear()) return;
            Vector3 screen = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position) : Vector3.zero;
            if (screen.z <= 0f) return;

            string devName = _sim.Universe.Get(_matId).devName;
            string label = $"E: grifo de {devName} [{(_on ? "ON" : "OFF")}]";
            if (favorCostPerActivation > 0) label += $" -- coste {favorCostPerActivation} Favor";
            if (_insufficientFavorTimer > 0f) label += " (sin Favor)";

            Rect r = new Rect(screen.x - 110f, Screen.height - screen.y - 30f, 260f, 22f);
            GUI.Label(r, label);
        }
    }
}
