using System.Collections.Generic;
using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Favor (moneda de progreso del taller, empieza en <see cref="StartingFavor"/>)
    /// más la generación y el seguimiento de los encargos ("orders") del
    /// Maestro para cada jornada.
    ///
    /// La lista de encargos por jornada es fija en tipo/umbral/recompensa
    /// (ver tabla de diseño M4, también documentada en docs/SIM_NOTES.md);
    /// lo único que varía por seed+día es la frase de sabor y, en el día 3,
    /// qué material bautizado concreto pide el encargo NamedMaterial si hay
    /// varios candidatos -- para eso (y SOLO para eso) se usa un
    /// System.Random(seed*31+día) local a esta clase, nunca en Sim/, tal y
    /// como exige el proyecto para toda aleatoriedad de capa de juego.
    /// </summary>
    public sealed class OrderSystem : MonoBehaviour
    {
        public const int StartingFavor = 20;
        public const int WinFavorTarget = 120;

        private AlkahestSim _sim;
        private SubstanceKnowledge _knowledge;

        public int Favor { get; private set; } = StartingFavor;
        public List<Order> ActiveOrders { get; } = new List<Order>(4);

        private int _nextOrderId;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _knowledge = knowledge;
        }

        public void AddFavor(int amount)
        {
            if (amount == 0) return;
            Favor += amount;
        }

        /// <summary>Intenta gastar Favor; no hace nada y devuelve false si no hay suficiente.</summary>
        public bool SpendFavor(int amount)
        {
            if (amount <= 0) return true;
            if (Favor < amount) return false;
            Favor -= amount;
            return true;
        }

        /// <summary>Genera y activa los encargos de la jornada `day` (1-based), reemplazando los de la jornada anterior.</summary>
        public void GenerateOrdersForDay(int day)
        {
            ActiveOrders.Clear();
            int universeSeed = (_sim != null && _sim.Universe != null) ? _sim.Universe.Seed : 0;
            var rng = new System.Random(universeSeed * 31 + day);

            switch (day)
            {
                case 1:
                    AddOrder(OrderType.Flammable, 60, 25,
                        "Trae algo que arda de verdad -- 60 celdas de material inflamable.");
                    AddOrder(OrderType.Hot, 80, 25,
                        "Algo que queme al tacto -- 80 celdas a 80°C o más.", minTempC: 80);
                    break;

                case 2:
                    // Los tres encargos de hoy usan EXACTAMENTE las tres muestras
                    // que el Maestro acaba de dejar (ver Game/MasterSupplies.cs):
                    // el retoño de vivium, la piedra gélida y el azoth + semilla.
                    AddOrder(OrderType.Grows, 120, 35,
                        "El Maestro quiere ver crecer algo vivo -- 120 celdas de Vivium.");
                    AddOrder(OrderType.Cold, 60, 30,
                        "Algo helado -- 60 celdas a -5°C o menos.", minTempC: -5);
                    AddOrder(OrderType.CrystalSolid, 70, 40,
                        "Cristal, ni más ni menos -- 70 celdas.");
                    break;

                case 3:
                default:
                    // Rebajados de 200/250 a 150/220: la bandeja fría del taller
                    // nuevo tiene ~276 celdas útiles y cristalizar es lento por
                    // diseño (12-27% por comprobación). Con 150 el encargo sigue
                    // exigiendo montar una producción, no un milagro.
                    AddOrder(OrderType.CrystalSolid, 150, 50,
                        "El Maestro quiere una gran veta de cristal -- 150 celdas.");
                    AddNamedOrFallback(rng);
                    AddOrder(OrderType.Grows, 220, 55,
                        "Que el taller rebose de vida -- 220 celdas de Vivium.");
                    break;
            }
        }

        private void AddNamedOrFallback(System.Random rng)
        {
            byte target = PickNamedMaterial(rng);
            if (target != MaterialId.Empty)
            {
                string nombre = _knowledge.NombreDe(target);
                AddOrder(OrderType.NamedMaterial, 100, 45,
                    $"Trae 100 celdas de lo que llamáis \"{nombre}\".", targetMat: target);
            }
            else
            {
                // Nadie ha bautizado nada todavía: encargo de reserva.
                AddOrder(OrderType.Flammable, 150, 45,
                    "Nada tiene nombre todavía -- trae 150 celdas de algo inflamable.");
            }
        }

        /// <summary>Elige, de forma determinista (seed+día), un material entre los bautizados y descubiertos. MaterialId.Empty si no hay ninguno.</summary>
        private byte PickNamedMaterial(System.Random rng)
        {
            if (_knowledge == null) return MaterialId.Empty;

            var candidates = new List<byte>(MaterialId.Count);
            for (int m = 1; m < MaterialId.Count; m++)
            {
                byte matId = (byte)m;
                if (_knowledge.EsDescubierto(matId) && _knowledge.NombreDe(matId) != "???")
                {
                    candidates.Add(matId);
                }
            }
            if (candidates.Count == 0) return MaterialId.Empty;
            return candidates[rng.Next(candidates.Count)];
        }

        private void AddOrder(OrderType tipo, int minCells, int recompensa, string descripcion,
            int? minTempC = null, byte? targetMat = null)
        {
            ActiveOrders.Add(new Order(_nextOrderId++, descripcion, tipo, minCells, recompensa, minTempC, targetMat));
        }

        /// <summary>
        /// Evalúa una celda consumida en la Tolva del Maestro (ver
        /// DeliveryChute) contra los encargos activos incompletos, EN EL
        /// ORDEN en que aparecen en <see cref="ActiveOrders"/>. Si coincide
        /// con el primero que aplica, avanza su progreso (completándolo y
        /// otorgando Favor si llega al mínimo). Devuelve true si coincidió
        /// con algún encargo (para que DeliveryChute no la cuente como
        /// "chatarra").
        /// </summary>
        public bool TryDeliverCell(Universe universe, byte matId, byte tempRaw)
        {
            for (int i = 0; i < ActiveOrders.Count; i++)
            {
                var order = ActiveOrders[i];
                if (order.Completado) continue;
                if (!MatchesOrder(universe, order, matId, tempRaw)) continue;

                order.Progreso++;
                if (order.Progreso >= order.MinCells)
                {
                    order.Completado = true;
                    AddFavor(order.Recompensa);
                    Debug.Log($"[ChaosAlchemy] Encargo completado: {order.Descripcion} (+{order.Recompensa} Favor).");
                }
                return true;
            }
            return false;
        }

        private static bool MatchesOrder(Universe universe, Order order, byte matId, byte tempRaw)
        {
            switch (order.Tipo)
            {
                case OrderType.Flammable:
                    return universe.Get(matId).flammable;
                case OrderType.Grows:
                    return universe.Get(matId).archetype == MaterialArchetype.Organic;
                case OrderType.CrystalSolid:
                    return matId == MaterialId.Crystal;
                case OrderType.Hot:
                    return order.MinTempC.HasValue && CellGrid.RawToC(tempRaw) >= order.MinTempC.Value;
                case OrderType.Cold:
                    return order.MinTempC.HasValue && CellGrid.RawToC(tempRaw) <= order.MinTempC.Value;
                case OrderType.NamedMaterial:
                    return order.TargetMat.HasValue && matId == order.TargetMat.Value;
                default:
                    return false;
            }
        }

        public int CompletedCount()
        {
            int n = 0;
            for (int i = 0; i < ActiveOrders.Count; i++)
                if (ActiveOrders[i].Completado) n++;
            return n;
        }

        public bool AllOrdersCompleted()
        {
            for (int i = 0; i < ActiveOrders.Count; i++)
                if (!ActiveOrders[i].Completado) return false;
            return true;
        }
    }
}
