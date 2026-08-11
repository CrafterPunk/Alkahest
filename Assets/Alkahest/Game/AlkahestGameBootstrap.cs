using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Punto de entrada de la capa de interacción: busca el AlkahestSim de
    /// la escena y genera al aprendiz, su frasco, las máquinas del
    /// laboratorio (placas calefactoras, piedra fría, grifos), y (M4) todo
    /// el bucle de partida -- conocimiento de sustancias, encargos del
    /// Maestro, la Tolva de entrega, y la máquina de estados de jornada con
    /// sus overlays.
    ///
    /// Nota de orden de ejecución: no hay garantía de que AlkahestSim.Start()
    /// (que crea Universe/Grid) se ejecute antes que este Start(), al vivir
    /// en GameObjects distintos, así que reintentamos en Update() hasta que
    /// Universe/Grid existan antes de generar nada — el mismo patrón
    /// defensivo que ya usa Dev/DevPalette.cs. Dentro de TrySpawn() el orden
    /// de las llamadas de Init(...) SÍ importa (son invocaciones directas,
    /// no Update() de Unity), pero el orden relativo en el que Unity vaya a
    /// llamar a Update() en los componentes ya creados NO debe importarle a
    /// ninguno de ellos (cada uno lee del otro por referencia guardada, no
    /// asume haberse actualizado ya en este mismo frame).
    /// </summary>
    public sealed class AlkahestGameBootstrap : MonoBehaviour
    {
        // Constantes de layout duplicadas de Sim/SimLevelBuilder.cs (ver
        // BuildVats/BuildShelves ahí): si el layout de M1 cambia, hay que
        // actualizar también estos números.
        private const int VatWidth = 56;
        private const int VatWallThickness = 3;
        private const int FloorHeight = 8;
        private const int VatCount = 3;

        private const int ShelfRightX0 = 250;
        private const int ShelfRightY0 = 132;
        private const int ShelfRightWidth = 58;
        private const int ShelfRightHeight = 3;

        private AlkahestSim _sim;
        private bool _spawned;

        private void Start()
        {
            _sim = FindAnyObjectByType<AlkahestSim>();
            if (_sim == null)
            {
                Debug.LogError("[Alkahest] AlkahestGameBootstrap no encontró un AlkahestSim en la escena.");
                enabled = false;
                return;
            }

            TrySpawn();
        }

        private void Update()
        {
            if (!_spawned) TrySpawn();
        }

        private void TrySpawn()
        {
            if (_spawned || _sim == null || _sim.Universe == null || _sim.Grid == null) return;

            var apprentice = SpawnApprentice();
            var flask = apprentice.GetComponent<Flask>();
            var knowledge = apprentice.GetComponent<SubstanceKnowledge>();

            SpawnHeatPlates(apprentice.transform);
            SpawnChillStone(apprentice.transform);

            var orderSystem = SpawnOrderSystem(knowledge);
            SpawnDispensers(apprentice.transform, orderSystem);
            SpawnDeliveryChute(orderSystem);

            SpawnNamingUi(flask, knowledge);
            SpawnJournalHud(knowledge);
            SpawnOrdersHud(orderSystem);
            SpawnDayCycle(orderSystem, knowledge);

            // M5: presentación y onboarding (fondo del taller + pistas de la primera partida).
            new GameObject("WorkshopBackdrop").AddComponent<WorkshopBackdrop>();
            new GameObject("HintSystem").AddComponent<HintSystem>();

            _spawned = true;
            Debug.Log("[ChaosAlchemy] Capa de interacción (M2-M4) inicializada.");
        }

        private ApprenticeController SpawnApprentice()
        {
            var go = new GameObject("Apprentice");
            go.transform.position = new Vector3(19.2f, 12f, 0f);

            var apprentice = go.AddComponent<ApprenticeController>();
            var flask = go.AddComponent<Flask>();
            flask.Init(_sim);
            var hud = go.AddComponent<FlaskHud>();
            hud.Init(_sim, flask);

            var knowledge = go.AddComponent<SubstanceKnowledge>();
            knowledge.Init(_sim, flask);

            return apprentice;
        }

        private void SpawnHeatPlates(Transform player)
        {
            int totalWidth = VatWidth * VatCount;
            int gap = (CellGrid.W - totalWidth) / (VatCount + 1);

            for (int i = 0; i < VatCount; i++)
            {
                int x0 = gap + i * (VatWidth + gap);
                int cellX0 = x0 + VatWallThickness;
                int cellX1 = x0 + VatWidth - 1 - VatWallThickness;
                int plateRow = FloorHeight + VatWallThickness - 1;

                var go = new GameObject($"HeatPlate_{i}");
                var plate = go.AddComponent<HeatPlate>();
                plate.Init(_sim, player, cellX0, cellX1, plateRow);
            }
        }

        private void SpawnChillStone(Transform player)
        {
            int plateRow = ShelfRightY0 + ShelfRightHeight - 1;

            var go = new GameObject("ChillStone_Shelf");
            var stone = go.AddComponent<ChillStone>();
            stone.Init(_sim, player, ShelfRightX0, ShelfRightX0 + ShelfRightWidth - 1, plateRow);
        }

        private void SpawnDispensers(Transform player, OrderSystem orderSystem)
        {
            // Coste de Favor por activación (M4): los básicos son gratis, los
            // más versátiles/potentes cuestan Favor -- fijado aquí, no en el
            // propio Dispenser, para tener toda la economía en un sitio.
            SpawnOneDispenser(player, "Water", MaterialId.Water, 40, orderSystem, 0);
            SpawnOneDispenser(player, "Sand", MaterialId.Sand, 70, orderSystem, 0);
            SpawnOneDispenser(player, "Oil", MaterialId.Oil, 110, orderSystem, 2);
            SpawnOneDispenser(player, "Nutrient", MaterialId.Nutrient, 150, orderSystem, 5);
        }

        private void SpawnOneDispenser(Transform player, string label, byte matId, int cellY, OrderSystem orderSystem, int favorCost)
        {
            var go = new GameObject($"Dispenser_{label}");
            var dispenser = go.AddComponent<Dispenser>();
            dispenser.Init(_sim, player, 3, cellY, matId, orderSystem, favorCost);
        }

        private OrderSystem SpawnOrderSystem(SubstanceKnowledge knowledge)
        {
            var go = new GameObject("OrderSystem");
            var orderSystem = go.AddComponent<OrderSystem>();
            orderSystem.Init(_sim, knowledge);
            return orderSystem;
        }

        private void SpawnDeliveryChute(OrderSystem orderSystem)
        {
            var go = new GameObject("DeliveryChute");
            var chute = go.AddComponent<DeliveryChute>();
            chute.Init(_sim, orderSystem);
        }

        private void SpawnNamingUi(Flask flask, SubstanceKnowledge knowledge)
        {
            var go = new GameObject("NamingUi");
            var naming = go.AddComponent<NamingUi>();
            naming.Init(_sim, flask, knowledge);
        }

        private void SpawnJournalHud(SubstanceKnowledge knowledge)
        {
            var go = new GameObject("JournalHud");
            var journal = go.AddComponent<JournalHud>();
            journal.Init(_sim, knowledge);
        }

        private void SpawnOrdersHud(OrderSystem orderSystem)
        {
            var go = new GameObject("OrdersHud");
            var hud = go.AddComponent<OrdersHud>();
            hud.Init(orderSystem);
        }

        private void SpawnDayCycle(OrderSystem orderSystem, SubstanceKnowledge knowledge)
        {
            var go = new GameObject("DayCycle");
            var cycle = go.AddComponent<DayCycle>();
            cycle.Init(_sim, orderSystem, knowledge);
        }
    }
}
