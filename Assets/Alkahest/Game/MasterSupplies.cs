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

        /// <summary>
        /// Frase que el Maestro dice en la intro de la jornada, o null si ese día no
        /// entrega nada.
        ///
        /// (fix playtest 9) REESCRITO DE RAÍZ. Reporte literal de un jugador tras varias
        /// horas de partida: "no consigo multiplicar la cantidad del producto... está raro
        /// que me den ingredientes que también son la meta". Lo entendía todo bien salvo UNA
        /// palabra -- el texto viejo decía "os confía" y enumeraba los tres regalos como si
        /// fueran INGREDIENTES ("toma esto"), que es exactamente el malentendido: el jugador
        /// los gastaba en la Tolva pensando que así se completaba el encargo, y se quedaba
        /// sin nada con lo que seguir. Son SEMILLAS -- catalizadores que NO se consumen al
        /// reaccionar (ver Sim/ReactionEngine.cs: "si un producto es igual al material
        /// original, esa celda no cambia"; y Sim/SimStepper.cs GrowthTick, donde el vivium
        /// asentado nunca se transforma, solo el Nutrient vecino). Este texto es LO PRIMERO
        /// que el jugador lee ese día (ver Game/DayCycle.cs DrawDayIntro): es el sitio de
        /// mayor impacto de todo este encargo, así que dice la palabra "semilla" tres veces
        /// y "no se gasta" explícitamente, y responde de frente a la pregunta del jugador --
        /// el encargo de hoy pide 120 celdas de vivium y solo se entregan ~81: A PROPÓSITO,
        /// porque no se puede entregar la muestra tal cual, hay que cultivarla.
        ///
        /// (fix playtest 10, reclasificación de sustancias) REESCRITO DE NUEVO: otro agente
        /// reclasificó Azoth/CrystalSeed/Crystal/Vivium como "innominados" (ver Game/
        /// SubstanceKnowledge.cs, NombreComun) -- el HUD entero ahora los muestra como "???"
        /// hasta que el jugador los bautiza, pero este texto seguía nombrándolos "Azoth",
        /// "vivium" y "semilla de cristal" en plata, contradiciendo al propio juego el mismo
        /// día en que aparecen por primera vez. No tiene sentido resolver el nombre vía
        /// SubstanceKnowledge aquí (NombreParaHud daría "???" las tres veces sin excepción --
        /// nadie ha visto ni tocado estas semillas todavía cuando se lee este texto, ver
        /// EnterDayIntro: AlEmpezarJornada se llama ANTES de que exista ninguna oportunidad de
        /// bautizar nada), así que se describen por ORIGEN/procedimiento -- el mismo
        /// vocabulario que ya usan Game/HintSystem.cs (Jornada 2) y los banners de "LEY
        /// DESCUBIERTA" de SubstanceKnowledge ("el líquido del grifo alto", "la semilla de la
        /// bandeja fría", "el retoño de la cuba") -- nunca la identidad interna del material.
        /// Y de paso la ficción encaja MEJOR que antes, no peor: el Maestro entrega estas tres
        /// semillas precisamente PORQUE él tampoco sabe qué son, y espera que seáis vosotros
        /// quienes les pongáis nombre -- se dice así, explícito, en la primera frase.
        /// 352 caracteres (antes 330; el panel de DayCycle.DrawDayIntro se comprobó/ajustó
        /// para este tamaño, ver AbrirPanel allí).
        /// </summary>
        public static string TextoEntrega(int dia)
        {
            if (dia != 2) return null;
            return "El Maestro os deja tres semillas SIN NOMBRE: ni él sabe qué son, y espera que " +
                   "vosotros les pongáis uno. No se gastan, se ALIMENTAN -- el líquido del grifo alto " +
                   "es infinito, el retoño de la cuba crece con nutriente y calor templado, y la " +
                   "semilla de la bandeja fría se riega con ese mismo líquido helado. Pocas celdas: " +
                   "se cultivan, no se entregan hechas.";
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
