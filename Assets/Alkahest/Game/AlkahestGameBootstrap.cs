using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Punto de entrada de la capa de interacción: busca el AlkahestSim de la
    /// escena y genera al aprendiz, su frasco, las máquinas del taller (placas
    /// ígneas, piedra gélida, grifos, estantería de redomas) y todo el bucle de
    /// partida — conocimiento de sustancias, encargos del Maestro, la Tolva de
    /// entrega, las muestras del Maestro y la máquina de estados de jornada.
    ///
    /// NINGUNA COORDENADA VIVE AQUÍ (reingeniería del espacio, playtest 4).
    /// Antes esta clase duplicaba a mano las constantes del nivel ("si el layout
    /// de M1 cambia, hay que actualizar también estos números"), y bastaba con
    /// mover una cuba para que las placas se quedasen colgadas en el aire. Todo
    /// se lee de Sim/SimLevelBuilder.cs, que es EL PLANO.
    ///
    /// Nota de orden de ejecución: no hay garantía de que AlkahestSim.Start()
    /// (que crea Universe/Grid) se ejecute antes que este Start(), al vivir en
    /// GameObjects distintos, así que reintentamos en Update() hasta que
    /// Universe/Grid existan antes de generar nada — el mismo patrón defensivo
    /// que ya usa Dev/DevPalette.cs. Dentro de TrySpawn() el orden de las
    /// llamadas de Init(...) SÍ importa (son invocaciones directas, no Update()
    /// de Unity), pero el orden relativo en el que Unity vaya a llamar a
    /// Update() en los componentes ya creados NO debe importarle a ninguno de
    /// ellos (cada uno lee del otro por referencia guardada, no asume haberse
    /// actualizado ya en este mismo frame).
    /// </summary>
    public sealed class AlkahestGameBootstrap : MonoBehaviour
    {
        private AlkahestSim _sim;
        private bool _spawned;

        private void Start()
        {
            _sim = FindAnyObjectByType<AlkahestSim>();
            if (_sim == null)
            {
                Debug.LogError("[ChaosAlchemy] AlkahestGameBootstrap no encontró un AlkahestSim en la escena.");
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

            // El árbitro de foco es estático y sobrevive a la recarga de escena
            // que hace DayCycle entre partidas: hay que vaciarlo antes de
            // registrar las máquinas nuevas.
            MachineFocus.Limpiar();

            var apprentice = SpawnApprentice();
            var flask = apprentice.GetComponent<Flask>();
            var knowledge = apprentice.GetComponent<SubstanceKnowledge>();

            SpawnHeatPlates(apprentice.transform);
            SpawnChillStone(apprentice.transform);

            var orderSystem = SpawnOrderSystem(knowledge);
            var grifoAzoth = SpawnDispensers(apprentice.transform, orderSystem);
            SpawnDeliveryChute(orderSystem);
            SpawnStorageRack(apprentice.transform, flask, knowledge);

            SpawnNamingUi(flask, knowledge);
            SpawnJournalHud(knowledge);
            SpawnOrdersHud(orderSystem);

            // Fondo del taller + pistas + muestras del Maestro se crean ANTES que
            // el ciclo de jornadas: DayCycle avisa a los dos últimos en cuanto
            // entra en la intro de la jornada 1 (ver DayCycle.Init).
            new GameObject("WorkshopBackdrop").AddComponent<WorkshopBackdrop>();
            var hints = new GameObject("HintSystem").AddComponent<HintSystem>();
            var supplies = new GameObject("MasterSupplies").AddComponent<MasterSupplies>();
            supplies.Init(_sim, grifoAzoth);

            SpawnDayCycle(orderSystem, knowledge, supplies, hints);

            _spawned = true;
            Debug.Log("[ChaosAlchemy] Capa de interacción inicializada (taller 256x144).");
        }

        private ApprenticeController SpawnApprentice()
        {
            var go = new GameObject("Apprentice");
            // Arranca flotando sobre la cuba izquierda, entre el banco de grifos
            // y la Tolva: desde ahí se ve el taller entero sin volar a ninguna
            // parte (antes aparecía en el centro geométrico de un mundo el doble
            // de ancho, lejos de todo).
            float celda = SimRenderer.CellWorldSize;
            float x = (SimLevelBuilder.VatAX0 + SimLevelBuilder.VatWidth * 0.5f) * celda;
            float y = (SimLevelBuilder.VatInteriorY1 + 10) * celda;
            go.transform.position = new Vector3(x, y, 0f);

            var apprentice = go.AddComponent<ApprenticeController>();
            var flask = go.AddComponent<Flask>();
            flask.Init(_sim);

            // El conocimiento se crea ANTES que el HUD: el HUD del frasco lo
            // necesita para mostrar el nombre que el jugador le puso a cada
            // sustancia (o el nombre común) en vez del devName interno.
            var knowledge = go.AddComponent<SubstanceKnowledge>();
            knowledge.Init(_sim, flask);

            var hud = go.AddComponent<FlaskHud>();
            hud.Init(_sim, flask, knowledge);

            return apprentice;
        }

        /// <summary>Una placa ígnea bajo cada una de las dos cubas centrales.</summary>
        private void SpawnHeatPlates(Transform player)
        {
            SpawnOneHeatPlate(player, 0, SimLevelBuilder.VatAX0);
            SpawnOneHeatPlate(player, 1, SimLevelBuilder.VatBX0);
        }

        private void SpawnOneHeatPlate(Transform player, int indice, int vatX0)
        {
            var go = new GameObject($"HeatPlate_{indice}");
            var plate = go.AddComponent<HeatPlate>();
            plate.Init(_sim, player,
                SimLevelBuilder.VatInteriorX0(vatX0),
                SimLevelBuilder.VatInteriorX1(vatX0),
                SimLevelBuilder.VatPlateRow);
        }

        /// <summary>La piedra gélida vive bajo la bandeja fría del estante superior.</summary>
        private void SpawnChillStone(Transform player)
        {
            var go = new GameObject("ChillStone_Bandeja");
            var stone = go.AddComponent<ChillStone>();
            stone.Init(_sim, player,
                SimLevelBuilder.ChillTrayInteriorX0,
                SimLevelBuilder.ChillTrayInteriorX1,
                SimLevelBuilder.ChillPlateRow);
        }

        /// <summary>
        /// Los cinco grifos, en COLUMNA VERTICAL sobre el pilar del banco, todos
        /// vertiendo en la misma pila de recogida. Devuelve el de Azoth, que
        /// nace sellado y lo abre el Maestro en la jornada 2.
        ///
        /// Coste de Favor por activación: los básicos son gratis, los versátiles
        /// cuestan — fijado aquí, no en el propio Dispenser, para tener toda la
        /// economía en un sitio.
        /// </summary>
        private Dispenser SpawnDispensers(Transform player, OrderSystem orderSystem)
        {
            SpawnOneDispenser(player, "Water", MaterialId.Water, 0, orderSystem, 0, false);
            SpawnOneDispenser(player, "Sand", MaterialId.Sand, 1, orderSystem, 0, false);
            SpawnOneDispenser(player, "Oil", MaterialId.Oil, 2, orderSystem, 2, false);
            SpawnOneDispenser(player, "Nutrient", MaterialId.Nutrient, 3, orderSystem, 5, false);
            return SpawnOneDispenser(player, "Azoth", MaterialId.Azoth, 4, orderSystem, 4, true);
        }

        private Dispenser SpawnOneDispenser(Transform player, string label, byte matId, int fila,
            OrderSystem orderSystem, int favorCost, bool bloqueado)
        {
            var go = new GameObject($"Dispenser_{label}");
            var dispenser = go.AddComponent<Dispenser>();
            dispenser.Init(_sim, player,
                SimLevelBuilder.TapMountX,
                SimLevelBuilder.TapFirstY + fila * SimLevelBuilder.TapStepY,
                matId, orderSystem, favorCost, bloqueado);
            return dispenser;
        }

        private void SpawnStorageRack(Transform player, Flask flask, SubstanceKnowledge knowledge)
        {
            var go = new GameObject("StorageRack");
            var rack = go.AddComponent<StorageRack>();
            rack.Init(_sim, flask, knowledge, player,
                SimLevelBuilder.RackX0, SimLevelBuilder.RackX1, SimLevelBuilder.RackTopY);
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

        private void SpawnDayCycle(OrderSystem orderSystem, SubstanceKnowledge knowledge,
            MasterSupplies supplies, HintSystem hints)
        {
            var go = new GameObject("DayCycle");
            var cycle = go.AddComponent<DayCycle>();
            cycle.Init(_sim, orderSystem, knowledge, supplies, hints);
        }
    }
}
