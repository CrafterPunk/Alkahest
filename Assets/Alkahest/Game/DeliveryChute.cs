using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// La Tolva del Maestro: un hueco de piedra fijo cerca de la pared
    /// derecha del taller. Cualquier material que caiga dentro se consume
    /// automáticamente cada tick (30Hz, throttled como el resto de máquinas)
    /// y se evalúa contra los encargos activos de <see cref="OrderSystem"/>
    /// (ver OrderSystem.TryDeliverCell); lo que no encaja en ningún encargo
    /// se cuenta como "chatarra" y da 1 de Favor cada
    /// <see cref="ScrapPerFavor"/> celdas desperdiciadas, para que probar
    /// cosas nunca sea del todo inútil.
    ///
    /// El marco de piedra se pinta UNA vez en Init con AlkahestSim.Paint
    /// (nunca acceso directo a CellGrid para mutar materiales, igual que el
    /// resto del proyecto): paredes laterales + suelo, dejando el techo
    /// deliberadamente abierto como abertura de vertido.
    ///
    /// LIMITACIÓN: lee _sim.Grid.temp[] directamente para evaluar los
    /// encargos Hot/Cold (mismo patrón que HeatPlate/ChillStone) porque no
    /// podemos editar Sim/ para exponer una API de lectura de temperatura
    /// por celda. TODO(ChaosAlchemy): canalizar por una API del sim.
    /// </summary>
    public sealed class DeliveryChute : MonoBehaviour
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const int ScrapPerFavor = 10;

        // Coordenadas de celda consistentes con el layout de SimLevelBuilder:
        // un bolsillo estrecho pegado a la pared derecha del nivel (x=W-1 es
        // el borde de Stone de FillBorder), por encima de la altura de las
        // cubetas/estantes existentes para no solaparse con ellos.
        private const int ZoneX0 = CellGrid.W - 8;
        private const int ZoneX1 = CellGrid.W - 4;
        private const int ZoneY0 = 60;
        private const int ZoneY1 = 80;

        private AlkahestSim _sim;
        private OrderSystem _orderSystem;
        private float _accumulator;
        private int _scrap;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, OrderSystem orderSystem)
        {
            _sim = sim;
            _orderSystem = orderSystem;

            transform.position = _sim.CellToWorld(new Vector2Int((ZoneX0 + ZoneX1) / 2, ZoneY1 + 3));
            BuildFrame();
            BuildVisual();
        }

        private void BuildFrame()
        {
            for (int y = ZoneY0; y <= ZoneY1; y++)
            {
                _sim.Paint(ZoneX0, y, 0, MaterialId.Stone);
                _sim.Paint(ZoneX1, y, 0, MaterialId.Stone);
            }
            for (int x = ZoneX0; x <= ZoneX1; x++)
            {
                _sim.Paint(x, ZoneY0, 0, MaterialId.Stone);
            }
            // El techo (y = ZoneY1) queda sin pared a propósito: es la
            // abertura por la que se vierte el material a entregar.
        }

        private void BuildVisual()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "ChaosAlchemyChuteTex" };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            var go = new GameObject("Bracket");
            go.transform.SetParent(transform, false);
            var bracket = go.AddComponent<SpriteRenderer>();
            bracket.sprite = sprite;
            bracket.color = new Color(0.5f, 0.42f, 0.3f, 1f);
            bracket.sortingOrder = 19;

            float widthWorld = (ZoneX1 - ZoneX0 + 1) * SimRenderer.CellWorldSize;
            go.transform.localScale = new Vector3(widthWorld, 0.08f, 1f);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null || _orderSystem == null) return;
            if (DayCycle.InputLocked) return;

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                ConsumeTick();
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;
        }

        private void ConsumeTick()
        {
            for (int x = ZoneX0 + 1; x <= ZoneX1 - 1; x++)
            {
                for (int y = ZoneY0 + 1; y <= ZoneY1; y++)
                {
                    byte matId = (byte)_sim.SampleMaterial(x, y);
                    if (matId == MaterialId.Empty) continue;

                    var def = _sim.Universe.Get(matId);
                    if (def.archetype == MaterialArchetype.StaticSolid) continue; // no debería darse (es el marco), defensivo.

                    byte tempRaw = _sim.Grid.temp[CellGrid.Idx(x, y)];
                    bool matched = _orderSystem.TryDeliverCell(_sim.Universe, matId, tempRaw);
                    if (!matched)
                    {
                        _scrap++;
                        if (_scrap >= ScrapPerFavor)
                        {
                            _scrap -= ScrapPerFavor;
                            _orderSystem.AddFavor(1);
                        }
                    }

                    _sim.Paint(x, y, 0, MaterialId.Empty);
                }
            }
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            Vector3 screen = Camera.main != null ? Camera.main.WorldToScreenPoint(transform.position) : Vector3.zero;
            if (screen.z <= 0f) return;

            Rect r = new Rect(screen.x - 90f, Screen.height - screen.y - 40f, 220f, 22f);
            GUI.Label(r, "TOLVA DEL MAESTRO");
        }
    }
}
