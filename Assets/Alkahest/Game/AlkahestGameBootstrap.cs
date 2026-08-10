using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Punto de entrada de la capa de interacción M2: busca el AlkahestSim
    /// de la escena y genera al aprendiz, su frasco, y las máquinas del
    /// laboratorio (placas calefactoras, piedra fría, grifos) en las
    /// posiciones del nivel M1.
    ///
    /// Nota de orden de ejecución: no hay garantía de que AlkahestSim.Start()
    /// (que crea Universe/Grid) se ejecute antes que este Start(), al vivir
    /// en GameObjects distintos, así que reintentamos en Update() hasta que
    /// Universe/Grid existan antes de generar nada — el mismo patrón
    /// defensivo que ya usa Dev/DevPalette.cs.
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
            SpawnHeatPlates(apprentice.transform);
            SpawnChillStone(apprentice.transform);
            SpawnDispensers(apprentice.transform);

            _spawned = true;
            Debug.Log("[Alkahest] Capa de interacción (M2) inicializada.");
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

        private void SpawnDispensers(Transform player)
        {
            SpawnOneDispenser(player, "Water", MaterialId.Water, 40);
            SpawnOneDispenser(player, "Sand", MaterialId.Sand, 70);
            SpawnOneDispenser(player, "Oil", MaterialId.Oil, 110);
            SpawnOneDispenser(player, "Nutrient", MaterialId.Nutrient, 150);
        }

        private void SpawnOneDispenser(Transform player, string label, byte matId, int cellY)
        {
            var go = new GameObject($"Dispenser_{label}");
            var dispenser = go.AddComponent<Dispenser>();
            dispenser.Init(_sim, player, 3, cellY, matId);
        }
    }
}
