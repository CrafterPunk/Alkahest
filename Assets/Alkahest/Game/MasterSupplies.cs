using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// LAS MUESTRAS DEL MAESTRO — arreglo de progresión del playtest 4.
    ///
    /// PROBLEMA REAL DETECTADO ("¿puedo conseguir todo con 4 caños? ¿el fuego
    /// aparece después?"): NO se podía. Con los cuatro grifos originales
    /// (agua, arena, aceite, nutriente) el taller no tenía NINGUNA fuente de
    /// Azoth, de semilla de cristal ni de Vivium. Sin Azoth no hay cristal
    /// (Universe.Create: la única reacción que produce Crystal es Azoth+semilla
    /// en frío) y sin una primera célula de Vivium no hay cultivo posible
    /// (SimStepper.GrowthTick solo hace crecer Vivium ya existente). Es decir:
    /// los encargos de cristal de las jornadas 2 y 3 y el de "algo vivo" eran
    /// IMPOSIBLES fuera de la paleta de dev (F3). En un playtest real el
    /// jugador se estrellaba contra un muro invisible.
    ///
    /// SOLUCIÓN, en la ficción del juego: al empezar la JORNADA 2 el Maestro,
    /// satisfecho con el primer día, confía material al taller:
    ///   · abre el GRIFO DE AZOTH del banco (sellado hasta entonces; 4 Favor
    ///     por uso: es su reserva, no un básico),
    ///   · deja un RETOÑO DE VIVIUM (~80 celdas) vivo en la cuba derecha,
    ///   · y un SAQUITO DE SEMILLA DE CRISTAL (60 celdas) sobre la bandeja
    ///     fría del estante, que es justo donde hay que cristalizarlo.
    ///
    /// El FUEGO sigue sin tener grifo, y eso es deliberado: el fuego se CREA
    /// (placa ARDIENTE sobre aceite o vivium), no se compra. Lo que faltaba era
    /// decirlo — ahora lo dice una pista de la jornada 2 (Game/HintSystem.cs).
    ///
    /// Idempotente: cada jornada entrega como mucho una vez, así que reentrar en
    /// una jornada (o un Update tardío) no duplica material.
    /// </summary>
    public sealed class MasterSupplies : MonoBehaviour
    {
        /// <summary>Radio del retoño de Vivium: un disco de r=5 son 81 celdas ("~80").</summary>
        private const int RadioRetonoVivium = 5;
        private const int AnchoSaquito = 20;
        private const int AltoSaquito = 3; // 20x3 = 60 celdas exactas de semilla

        private AlkahestSim _sim;
        private Dispenser _grifoAzoth;
        private readonly bool[] _entregado = new bool[DayCycle.TotalDays + 1];

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Dispenser grifoAzoth)
        {
            _sim = sim;
            _grifoAzoth = grifoAzoth;
        }

        /// <summary>Frase que el Maestro dice en la intro de la jornada, o null si ese día no entrega nada.</summary>
        public static string TextoEntrega(int dia)
        {
            if (dia != 2) return null;
            return "El Maestro os confía: azoth del alambique (grifo nuevo en el banco), " +
                   "un retoño de su cultivo en la cuba derecha y semilla de cristal sobre la bandeja fría.";
        }

        /// <summary>Llamado por Game/DayCycle.cs al entrar en la intro de cada jornada.</summary>
        public void AlEmpezarJornada(int dia)
        {
            if (dia < 1 || dia >= _entregado.Length) return;
            if (_entregado[dia]) return;
            _entregado[dia] = true;

            if (dia != 2) return;
            if (_sim == null || _sim.Grid == null) return;

            // 1) El grifo de Azoth deja de estar sellado.
            if (_grifoAzoth != null) _grifoAzoth.Desbloquear();

            // 2) Retoño de Vivium en el fondo de la cuba derecha. Nace a
            //    temperatura ambiente y DORMIDO (fuera de su banda de
            //    crecimiento): para que crezca hay que templar la placa — que es
            //    exactamente la lección de la jornada.
            int centroCubaB = (SimLevelBuilder.VatInteriorX0(SimLevelBuilder.VatBX0)
                             + SimLevelBuilder.VatInteriorX1(SimLevelBuilder.VatBX0)) / 2;
            _sim.Paint(centroCubaB, SimLevelBuilder.VatInteriorY0 + RadioRetonoVivium, RadioRetonoVivium, MaterialId.Vivium);

            // 3) Saquito de semilla de cristal sobre la bandeja fría.
            int centroBandeja = (SimLevelBuilder.ChillTrayInteriorX0 + SimLevelBuilder.ChillTrayInteriorX1) / 2;
            _sim.PaintRect(centroBandeja - AnchoSaquito / 2, SimLevelBuilder.ChillTrayInteriorY0,
                AnchoSaquito, AltoSaquito, MaterialId.CrystalSeed);

            Debug.Log("[ChaosAlchemy] Muestras del Maestro entregadas (jornada 2): azoth, vivium y semilla de cristal.");
        }
    }
}
