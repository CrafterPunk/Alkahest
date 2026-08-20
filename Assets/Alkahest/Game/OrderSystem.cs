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
    ///
    /// =====================================================================
    /// (fix playtest 10) LOS ENCARGOS HABLAN POR EFECTO Y ORIGEN, NO POR
    /// NOMBRE INTERNO (docs/DECISIONS.md §12: "el nombre es del grupo... el
    /// interno visible solo en modo dev"). Antes "220 celdas de Vivium" leía
    /// el devName aunque el jugador jamás hubiera bautizado nada. Ahora:
    ///  · Grows/CrystalSolid describen su objetivo por EFECTO/ORIGEN mientras
    ///    esté innominado ("algo vivo que crezca solo", "la piedra que crece
    ///    en la bandeja fría") -- ver <see cref="DescribirGrows"/> y
    ///    <see cref="DescribirCrystalSolid"/>. Sin ambigüedad posible: en
    ///    TODO el roster fijo (Sim/Universe.cs) solo Vivium tiene arquetipo
    ///    Organic y solo la reacción Azoth+semilla produce Crystal, así que
    ///    la descripción por efecto identifica una única sustancia real del
    ///    taller, no un grupo -- si el roster llega a cambiar (más de un
    ///    material Organic, por ejemplo) esta suposición HAY QUE
    ///    revalidarla, es la garantía de la regla de seguridad "un encargo
    ///    nunca puede quedar ambiguo".
    ///  · En cuanto el jugador bautiza esa sustancia, el texto pasa a usar SU
    ///    nombre ("130 celdas de lo que llamáis \"musgo hambriento\"") -- el
    ///    "el mundo empieza a hablar tu idioma" que pedía el reporte.
    ///  · Flammable/Hot/Cold no dependen de una identidad concreta (cualquier
    ///    material que cumpla la propiedad vale) así que su texto es
    ///    literal y no se recalcula nunca -- no hay circularidad que evitar.
    ///  · El recálculo lo dispara <see cref="SubstanceKnowledge.NamingVersion"/>
    ///    (ver Update): como Order.Descripcion es readonly (Game/Order.cs,
    ///    de solo lectura en esta ronda) no se muta in-place -- se sustituye
    ///    la instancia entera en <see cref="ActiveOrders"/> conservando
    ///    Progreso/Completado, así que OrdersHud (que lee el campo cada
    ///    frame, sin cachear) lo recoge solo en el siguiente OnGUI. Nunca se
    ///    construye texto dentro de OnGUI: todo el trabajo de string pasa
    ///    por aquí, una vez por cambio de NamingVersion, no por frame.
    /// =====================================================================
    /// BALANCE PLAYTEST 8 -- TASAS MEDIDAS (Fase 1, antes de tocar números)
    /// =====================================================================
    /// Todo lo de abajo sale de leer Sim/ y Game/ (solo lectura para este
    /// pase), nunca de "a ojo". Cifras entre paréntesis = archivo:constante.
    ///
    /// GRIFO (Game/Dispenser.cs): EmitTick pinta como mucho un rombo de radio
    /// 1 alrededor de la boquilla (SpoutRadius=1 -&gt; dx²+dy²&lt;=1 -&gt; 5 celdas:
    /// centro + 4 ortogonales), UNA vez por tick de 1/30s (TickDt); el
    /// "presupuesto" EmitRatePerTick=12 nunca se agota porque el rombo ya
    /// tiene menos celdas que eso. Techo TEÓRICO con la boca vacía: 5 cel/tick
    /// x 30 Hz = 150 celdas/s. En régimen real (una vez la pila de recogida
    /// empieza a acumularse hay que esperar a que el material se aleje) baja
    /// mucho, pero incluso una estimación pesimista de 20-30 celdas/s sigue
    /// siendo 30-90x más rápido que lo que exige CUALQUIER encargo de este
    /// archivo (60-250 celdas / 360s de jornada = 0.17-0.7 celdas/s de
    /// media). Conclusión: el grifo NUNCA es el cuello de botella de ningún
    /// encargo alimentado por un básico (Flammable/Hot/Cold vía Agua/Aceite/
    /// Arena/Nutriente) -- coincide con que el día 1 y el día 2 ya se
    /// completaron en el playtest real.
    ///
    /// CRISTALIZACIÓN (Sim/Universe.cs Create + Sim/SimStepper.cs):
    /// - Probabilidad por comprobación: crystallizeChancePct = 12 (base) ó 27
    ///   bajo el Edicto "Frío Fértil" (+15).
    /// - Cadencia de comprobación (SimStepper.MaybeReact): si la celda se ha
    ///   movido ESTE tick se comprueba cada tick; si está quieta (asentada
    ///   contra el cristal/semilla, que es el caso normal en la bandeja), se
    ///   comprueba solo 1 de cada 8 ticks (~0.267s a 30Hz).
    /// - Tiempo medio de conversión de UNA celda de contacto quieta:
    ///   8 ticks/comprobación ÷ p = 66.7 ticks (~2.22s) al 12%, 29.6 ticks
    ///   (~0.99s) al 27%.
    /// - Bandeja fría (Sim/SimLevelBuilder.cs): ChillTrayWidth=52, restando
    ///   2*WallThickness(3) de paredes -&gt; 46 columnas útiles; ChillTrayHeight=9
    ///   menos WallThickness(3) del suelo (es una U abierta arriba) -&gt; 6 filas
    ///   -&gt; 46*6 = 276 celdas útiles totales de capacidad.
    /// - La reacción Azoth+Crystal-&gt;Crystal (además de Azoth+CrystalSeed-&gt;
    ///   Crystal) hace que el propio cristal ya formado siga siendo semilla:
    ///   el frente se expande mientras se siga vertiendo Azoth encima, no es
    ///   "una sola tirada y ya". Con un frente activo de 10-20 celdas de
    ///   contacto (ancho del saquito de semilla que deja el Maestro, 20
    ///   celdas) la producción sostenida ronda 1-2 celdas/s en condiciones
    ///   realistas (contando reposiciones de Azoth, viajes y que el jugador
    ///   reparte su atención con las otras dos estaciones) -- lento POR
    ///   DISEÑO, tal y como pide docs/SIM_NOTES.md.
    ///
    /// CRECIMIENTO DE VIVIUM (Sim/SimStepper.cs GrowthTick + Universe.cs):
    /// - (playtest 19, ACTUALIZADO) `VivGrowChancePct` = **75%** (antes 60) de
    ///   crear célula nueva al consumir un Nutrient vecino en banda. La subida
    ///   NO es un rebalanceo de dificultad: compensa exactamente el freno que
    ///   introdujo el crecimiento DENDRÍTICO, para que cultivar cueste el mismo
    ///   tiempo que antes. Medido en el modelo de esa ronda: ~46 ticks/120
    ///   celdas con la regla vieja, ~52 sin compensar, ~40 compensada.
    /// - (playtest 19) **YA NO CRECE COMO UNA MANCHA.** Antes cualquier célula
    ///   asentada con Nutrient al lado podía engendrar, y por eso el
    ///   crecimiento era EXPONENCIAL en toda la masa. Ahora solo engendran las
    ///   PUNTAS (las que tienen pocos vecinos vivos, umbral por semilla):
    ///   la colonia ramifica en vez de engordar entera. La consecuencia para
    ///   ESTE archivo es que el número de células capaces de crecer ya no es
    ///   proporcional a la masa sino a su PERÍMETRO ÚTIL -- sigue sin ser el
    ///   cuello de botella (ver la medición de arriba), pero si alguien vuelve
    ///   a calibrar umbrales, que no razone con "exponencial".
    /// - Cadencia de intento: throttle de 1 de cada 4 ticks por celda
    ///   asentada = 7.5 intentos/s (a 30Hz) SI tiene un vecino Nutrient
    ///   disponible ese intento Y sigue siendo punta.
    /// - Cuba B (Sim/SimLevelBuilder.cs): el interior real se deriva de
    ///   `VatInteriorX0/X1` y ronda las 58x37 celdas (la cifra "52x37" que
    ///   decía esta línea llevaba obsoleta desde el playtest 15) -- en
    ///   cualquier caso, NUNCA es el límite real.
    /// - El cuello de botella real es el ARRANQUE: el retoño
    ///   inicial que deja el Maestro (disco r=5 = 81 celdas, Game/
    ///   MasterSupplies.cs) nace DORMIDO (fuera de banda) y hace falta
    ///   templar la placa Y seguir vertiendo Nutrient a mano para que la
    ///   frontera crezca -- eso consume minutos de atención real, no la
    ///   propia tasa de crecimiento (que una vez alimentada es rapidísima).
    ///
    /// FRASCO Y TRANSPORTE (Game/Flask.cs, Game/ApprenticeController.cs):
    /// - Capacity=900 (nunca limita: ningún encargo pide más de 250).
    /// - SuckRatePerTick=30, PourRatePerTick=20 -&gt; techos teóricos de 900 y
    ///   600 celdas/s a 30Hz (muy por encima de lo necesario).
    /// - ReachWorld=6 unidades de mundo = 60 celdas de radio SIN moverse
    ///   (SimRenderer.CellWorldSize=0.1).
    /// - ApprenticeController.moveSpeed=11.2 u/s = 112 celdas/s. Ninguna
    ///   estación del taller (grifos, cubas, bandeja fría, Tolva) está a más
    ///   de ~230 celdas en línea recta, y el radio de alcance ya cubre 60 de
    ///   esa distancia por cada lado -&gt; un viaje completo aspirar-&gt;volar-&gt;
    ///   verter cuesta 1-2s de vuelo real. CONCLUSIÓN: la LOGÍSTICA NUNCA es
    ///   el cuello de botella; todo el tiempo de un encargo se va en
    ///   PRODUCCIÓN (grifo/cristalización/cultivo/calor), no en transporte
    ///   -- por eso NO hace falta tocar la duración de la jornada.
    ///
    /// JORNADA (Game/DayCycle.cs): DayDurationSeconds = 360s (6:00). NO se
    /// toca (ver límites del encargo); los umbrales de abajo se han
    /// dimensionado para caber en el 60-70% de esos 360s (216-252s) de
    /// trabajo activo, dejando 108-144s de margen para experimentar.
    /// </summary>
    public sealed class OrderSystem : MonoBehaviour
    {
        public const int StartingFavor = 20;

        // =================================================================
        // DESENLACES (balance playtest 8): antes había una única meta
        // (WinFavorTarget=120) que cortaba la partida en cuanto se
        // alcanzaba -- y con Favor inicial(20) + jornada1(25+25=50) +
        // jornada2(35+30+40=105) = 175, se alcanzaba SOLA sin haber tocado
        // la jornada 3 (ver captura real: 149★ con meta en 120, día 3 en
        // curso). DISEÑO NUEVO: la partida SIEMPRE juega las 3 jornadas
        // completas (ver Game/DayCycle.cs, ya no hay corte por victoria
        // anticipada) y el resultado se GRADÚA por el Favor final en 4
        // escalones. Derivación de cada umbral:
        //
        //  · AprendizFavorTarget = 120: es la antigua meta única (el
        //    "aprobado" del juego). Con los encargos de día 1+2 sin tocar
        //    (validados por el playtest real: Cesar los completó) el máximo
        //    antes de la jornada 3 sigue siendo 175 -- así que llegar a
        //    Aprendiz sigue siendo un logro real (exige buena parte de las
        //    dos primeras jornadas) pero, a propósito, YA NO corta la
        //    partida: por debajo de 120 el desenlace es Despedido.
        //
        //  · OficialFavorTarget = 180: el máximo teórico ANTES de la
        //    jornada 3 (175) queda justo por debajo -- así que Oficial exige
        //    haber entregado además AL MENOS un encargo de la jornada 3 (el
        //    más barato del día 3 rebalanceado, ver AddOrder de abajo, ya
        //    vale +35, más que suficiente para cruzar el hueco). Para un
        //    jugador que NO completó todo el día 1/2, Oficial exige bastante
        //    más trabajo real de la jornada 3.
        //
        //  · MaestroFavorTarget = 260: el máximo teórico TOTAL de la partida
        //    (Start 20 + día1 50 + día2 105 + día3 rebalanceado 130 = 305,
        //    ver AddOrder de la jornada 3) menos margen para lo que de
        //    verdad se gasta jugando -- los grifos de Aceite/Nutriente/Azoth
        //    cuestan Favor por activación (2/4/5, fijado en
        //    AlkahestGameBootstrap, fuera de este archivo) y ese gasto SÍ
        //    resta del Favor final que se compara aquí. 260 exige haber
        //    completado (o casi) los TRES encargos de la jornada 3 --
        //    "completa TODO y además entrega excedente" tal y como pide el
        //    encargo de rebalanceo -- y no solo asomarse a ella. El pequeño
        //    colchón entre 260 y el máximo teórico (305) es justo el que
        //    absorbe el coste normal de haber usado los grifos caros durante
        //    la partida: gastar Favor en material ahora compite de verdad
        //    con llegar a Maestro, que es exactamente lo que pedía el
        //    encargo de "Favor como recurso, no solo puntuación" -- sin
        //    tocar Dispenser.cs ni AlkahestGameBootstrap.cs.
        //
        //  · (fix playtest 9) El máximo teórico de 305 antes de esta ronda
        //    NO era un techo real: el Favor por "chatarra" de DeliveryChute
        //    (ver ScrapPerFavor, eliminado) era ilimitado -- verter agua sin
        //    parar durante 6 minutos sumaba Favor sin tocar un solo encargo,
        //    así que 305 era solo "el mínimo para llegar a Maestro jugando
        //    limpio", no el máximo real alcanzable. Con la chatarra
        //    eliminada, 305 (menos lo gastado en grifos) SÍ es ahora el
        //    techo real de toda la partida: el Favor solo puede venir de
        //    completar encargos, así que los umbrales por fin miden lo que
        //    dicen medir.
        // =================================================================
        public const int AprendizFavorTarget = 120;
        public const int OficialFavorTarget = 180;
        public const int MaestroFavorTarget = 260;

        /// <summary>Los cuatro desenlaces posibles al cierre de la partida (jornada 3 completa, siempre).</summary>
        public enum Desenlace { Despedido, Aprendiz, Oficial, Maestro }

        /// <summary>Desenlace que corresponde a un Favor final concreto.</summary>
        public static Desenlace DesenlaceParaFavor(int favor)
        {
            if (favor >= MaestroFavorTarget) return Desenlace.Maestro;
            if (favor >= OficialFavorTarget) return Desenlace.Oficial;
            if (favor >= AprendizFavorTarget) return Desenlace.Aprendiz;
            return Desenlace.Despedido;
        }

        /// <summary>Nombre en español (minúsculas, para incrustar en frases) del desenlace.</summary>
        public static string NombreDesenlace(Desenlace d)
        {
            switch (d)
            {
                case Desenlace.Maestro: return "maestro";
                case Desenlace.Oficial: return "oficial";
                case Desenlace.Aprendiz: return "aprendiz";
                default: return "despedido";
            }
        }

        /// <summary>
        /// Umbral y nombre del PRÓXIMO escalón todavía no alcanzado para un
        /// Favor dado. Devuelve false si ya se llegó a Maestro (el máximo:
        /// no hay escalón siguiente que mostrar). Usado tanto por OrdersHud
        /// (qué meta mostrar mientras se juega) como por DayCycle (cuánto
        /// faltó en la pantalla final).
        /// </summary>
        public static bool TryGetNextTier(int favor, out int umbral, out string nombre)
        {
            if (favor < AprendizFavorTarget) { umbral = AprendizFavorTarget; nombre = "aprendiz"; return true; }
            if (favor < OficialFavorTarget) { umbral = OficialFavorTarget; nombre = "oficial"; return true; }
            if (favor < MaestroFavorTarget) { umbral = MaestroFavorTarget; nombre = "maestro"; return true; }
            umbral = 0; nombre = null;
            return false;
        }

        private AlkahestSim _sim;
        private SubstanceKnowledge _knowledge;

        public int Favor { get; private set; } = StartingFavor;
        public List<Order> ActiveOrders { get; } = new List<Order>(4);

        private int _nextOrderId;

        // (fix playtest 10) Última NamingVersion ya aplicada a las descripciones -- ver
        // Update/RefreshDescripciones. -1 fuerza un primer refresco inofensivo nada más
        // arrancar (no hay nada que recalcular todavía si no hay encargos, pero así no
        // hace falta razonar sobre "0 es también un valor válido de NamingVersion").
        private int _ultimaNamingVersionAplicada = -1;

        /// <summary>
        /// (playtest 25, CONTRATO_PERSISTE.md §6.1) EL ARCO DE 5 DE "LO QUE
        /// PERSISTE": -1 mientras no está activo (modo clásico, o antes de
        /// llamar a GenerateOrdersPersiste). 0..4 = qué pedido del arco fijo
        /// está mostrándose ahora mismo en <see cref="ActiveOrders"/> (que en
        /// este modo SIEMPRE tiene como mucho UNO: "de uno en uno", ver
        /// AvanzarArcoPersisteSiToca). 5 = arco terminado.
        /// </summary>
        private int _arcoPersisteIndex = -1;

        private const int ArcoPersisteCount = 5;

        /// <summary>
        /// (playtest 51, ronda 51, EL RECETARIO DEL LABORATORIO) Mismo patrón que
        /// <see cref="_arcoPersisteIndex"/> pero para
        /// <see cref="GenerateOrdersSemillaCompartida"/>: -1 mientras no está
        /// activo, 0..4 = qué pedido del arco fijo de la Semilla Cero compartida
        /// está mostrándose, <see cref="ArcoRecetarioCount"/> = arco terminado.
        /// Los dos arcos nunca están activos a la vez en la misma partida (uno lo
        /// arranca el laboratorio de un jugador/multi clásico, el otro solo el
        /// botón "SEMILLA CERO compartida" del lobby), pero se mantienen en
        /// campos disjuntos a propósito: comparten el mecanismo de avance
        /// (uno-en-uno, ver AvanzarArcoRecetarioSiToca) sin compartir estado, así
        /// que ninguno puede pisar la cuenta del otro si algún día coexistieran.
        /// </summary>
        private int _arcoRecetarioIndex = -1;

        private const int ArcoRecetarioCount = 5;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _knowledge = knowledge;
        }

        /// <summary>
        /// (fix playtest 10) Único trabajo por frame: comprobar si el jugador bautizó o
        /// re-bautizó algo desde el último frame (NamingVersion sube en CADA Bautizar,
        /// incluso un re-bautizo del mismo material -- ver doc de esa propiedad) y, si
        /// es así, recalcular las descripciones que dependen de un nombre. Barato: casi
        /// siempre es una comparación de enteros que no hace nada.
        /// </summary>
        private void Update()
        {
            if (_knowledge == null) return;
            int version = _knowledge.NamingVersion;
            if (version == _ultimaNamingVersionAplicada) return;
            _ultimaNamingVersionAplicada = version;
            RefreshDescripciones();
        }

        /// <summary>
        /// Recalcula la Descripcion de todo encargo activo cuyo texto dependa de un
        /// nombre (TargetMat.HasValue -- ver AddOrder de Grows/CrystalSolid/
        /// NamedMaterial más abajo). Order.Descripcion es readonly (Game/Order.cs es de
        /// solo lectura en esta ronda), así que no se muta el campo: se sustituye la
        /// instancia entera en <see cref="ActiveOrders"/>, conservando Id/Progreso/
        /// Completado -- OrdersHud lee <c>orders[i].Descripcion</c> fresco cada OnGUI
        /// (no lo cachea), así que el cambio se ve en el siguiente frame sin más.
        /// </summary>
        private void RefreshDescripciones()
        {
            for (int i = 0; i < ActiveOrders.Count; i++)
            {
                var o = ActiveOrders[i];
                if (!o.TargetMat.HasValue) continue; // Flammable/Hot/Cold: texto literal, nunca depende de un nombre.

                string nuevaDescripcion = o.Tipo switch
                {
                    OrderType.Grows => DescribirGrows(o.MinCells),
                    OrderType.CrystalSolid => DescribirCrystalSolid(o.MinCells),
                    OrderType.NamedMaterial => DescribirNamedMaterial(o.MinCells, o.TargetMat.Value),
                    _ => o.Descripcion,
                };
                if (nuevaDescripcion == o.Descripcion) continue;

                var actualizado = new Order(o.Id, nuevaDescripcion, o.Tipo, o.MinCells, o.Recompensa, o.MinTempC, o.TargetMat)
                {
                    Progreso = o.Progreso,
                    Completado = o.Completado,
                };
                ActiveOrders[i] = actualizado;
            }
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

        /// <summary>
        /// EL VOLUMEN DE UN ENCARGO (playtest 19). Reporte de Cesar, tras la
        /// build: *"aún estoy atorado con los niveles de cosas que me pide el
        /// mago, en especial el nivel 1, que creo que siempre es el mismo"*.
        ///
        /// Dos quejas en una frase, y las dos son ciertas:
        ///  (1) SE PIDE DEMASIADO. Todos los umbrales de este archivo se
        ///      calibraron contra el TIEMPO de jornada (¿cabe en el 60-70% de
        ///      los 360s?) y esa cuenta sigue siendo correcta -- pero medía la
        ///      pregunta equivocada. El juego no va de producir en cantidad,
        ///      va de EXPERIMENTAR; y un umbral que "cabe en el tiempo" te
        ///      obliga igualmente a pasarte ese tiempo acarreando frasco en
        ///      vez de probando cosas. Cumplir tiene que ser el peaje corto
        ///      que te deja seguir jugando, no la jornada entera.
        ///  (2) LA JORNADA 1 ES LITERALMENTE IDÉNTICA cada partida: sus dos
        ///      encargos estaban escritos con constantes, sin tocar el `rng`
        ///      sembrado con (semilla, día) que este método ya construía y que
        ///      hasta ahora solo usaba la jornada 3.
        ///
        /// Esta función responde a las dos: recorta al <see cref="FactorVolumen"/>
        /// y añade un temblor por semilla, así que dos partidas distintas nunca
        /// piden exactamente lo mismo. Se aplica a TODAS las jornadas.
        ///
        /// LO QUE NO TOCA, A PROPÓSITO: las RECOMPENSAS. Toda la aritmética de
        /// desenlaces de la cabecera de esta clase (120/180/260 de Favor, y el
        /// máximo teórico de 175 antes de la jornada 3) depende de las
        /// recompensas, no de las celdas. Bajar el volumen sin tocar el Favor
        /// deja esa cuenta intacta y solo hace el camino más corto.
        /// </summary>
        private static int Volumen(int celdasOriginales, System.Random rng)
        {
            int recortado = Mathf.RoundToInt(celdasOriginales * FactorVolumen);
            // Temblor de +/-12%: suficiente para que no se lea igual dos
            // partidas seguidas, pequeño para no descalibrar el equilibrio.
            float temblor = 0.88f + (float)rng.NextDouble() * 0.24f;
            int final = Mathf.RoundToInt(recortado * temblor);
            return Mathf.Max(20, final); // nunca por debajo de un frasco largo: un encargo trivial no enseña nada.
        }

        /// <summary>Cuánto se recorta cada umbral respecto al valor calibrado por tiempo. Ver <see cref="Volumen"/>.</summary>
        private const float FactorVolumen = 0.6f;

        /// <summary>Genera y activa los encargos de la jornada `day` (1-based), reemplazando los de la jornada anterior.</summary>
        public void GenerateOrdersForDay(int day)
        {
            ActiveOrders.Clear();
            int universeSeed = (_sim != null && _sim.Universe != null) ? _sim.Universe.Seed : 0;
            var rng = new System.Random(universeSeed * 31 + day);

            switch (day)
            {
                case 1:
                    // (playtest 19) La jornada de la que Cesar dijo "en especial
                    // el nivel 1, que creo que siempre es el mismo". Tenía razón
                    // por partida doble: pedía mucho Y era idéntica cada vez.
                    // Ahora las dos cantidades pasan por Volumen(), así que se
                    // recortan y tiemblan con la semilla. Los textos se
                    // construyen con la cifra REAL -- si se vuelve a escribir a
                    // mano un número en la frase, mentirá en cuanto tiemble.
                    {
                        int celdasArde = Volumen(60, rng);
                        AddOrder(OrderType.Flammable, celdasArde, 25,
                            "Trae algo que arda de verdad -- " + celdasArde + " celdas de material inflamable.");
                        int celdasQuema = Volumen(80, rng);
                        AddOrder(OrderType.Hot, celdasQuema, 25,
                            "Algo que queme al tacto -- " + celdasQuema + " celdas a 80°C o más.", minTempC: 80);
                    }
                    break;

                case 2:
                    // Umbrales SIN CAMBIOS -- también referencia buena (completada).
                    // Los tres encargos de hoy usan EXACTAMENTE las tres muestras
                    // que el Maestro acaba de dejar (ver Game/MasterSupplies.cs):
                    // el retoño de vivium, la piedra gélida y el azoth + semilla.
                    // (fix playtest 10) Descripción por EFECTO/nombre, no literal fija
                    // -- ver DescribirGrows/DescribirCrystalSolid y el docblock de la
                    // clase: es justo lo que ambas eran ANTES de este cambio, un texto
                    // fijo que decía "Vivium"/"Cristal" a las claras.
                    {
                        int celdasVivo = Volumen(120, rng);
                        AddOrder(OrderType.Grows, celdasVivo, 35, DescribirGrows(celdasVivo), targetMat: MaterialId.Vivium);
                        int celdasFrio = Volumen(60, rng);
                        AddOrder(OrderType.Cold, celdasFrio, 30,
                            "Algo helado -- " + celdasFrio + " celdas a -5°C o menos.", minTempC: -5);
                    }
                    // 70 celdas de Crystal es coherente con la medición de
                    // cabecera (~1-2 cel/s sostenidas con un frente recién
                    // sembrado): ronda 35-70s de conversión activa, primer
                    // contacto del jugador con la mecánica, sin exigir aún
                    // dominarla.
                    {
                        int celdasCristal = Volumen(70, rng);
                        AddOrder(OrderType.CrystalSolid, celdasCristal, 40, DescribirCrystalSolid(celdasCristal), targetMat: MaterialId.Crystal);
                    }
                    break;

                case 3:
                default:
                    // REBALANCEADOS (balance playtest 8). Antes: Crystal 150 +
                    // Named 100 (o Flammable 150 de reserva) + Grows 220 = 470
                    // celdas combinadas en 360s, cuando cristalizar y cultivar
                    // son lentos POR DISEÑO (ver medición de cabecera). Nuevos
                    // umbrales, cada uno con margen para <60-70% de la jornada>
                    // sumados entre los tres (y con hueco de sobra para
                    // experimentar, que es el corazón del juego):
                    //
                    // CrystalSolid 150->90 (-40%): a 1-2 cel/s sostenidas
                    // (medición de cabecera) son 45-90s de conversión activa;
                    // la MISMA bandeja y el MISMO frente de cristal de la
                    // jornada 2 (70 celdas ya sembradas) siguen ahí al empezar
                    // el día 3 -- no se arranca de cero, así que el margen real
                    // es aún mayor. 276 celdas de capacidad de bandeja dejan un
                    // 67% de la bandeja libre incluso al llegar al umbral.
                    {
                        int celdasCristal3 = Volumen(90, rng);
                        AddOrder(OrderType.CrystalSolid, celdasCristal3, 45, DescribirCrystalSolid(celdasCristal3), targetMat: MaterialId.Crystal);
                    }
                    AddNamedOrFallback(rng);
                    // Grows 220->130 (-41%): la MISMA cuba y el MISMO cultivo
                    // de vivium de la jornada 2 (que ya entregó 120) siguen
                    // creciendo si se mantuvo la placa en banda -- +10 celdas
                    // sobre el umbral de ayer es una escalada suave, no un
                    // segundo arranque desde cero. Si el jugador SÍ tiene que
                    // arrancar de cero (cultivo perdido/quemado), el ritmo de
                    // 7.5 intentos/s x 60% de la medición de cabecera hace que,
                    // una vez alimentado con Nutriente, 130 celdas caigan en
                    // bastante menos de un minuto de crecimiento puro -- el
                    // tiempo real se va en el arranque (atender la placa y
                    // verter Nutriente), no en esperar a que crezca.
                    {
                        int celdasVivo3 = Volumen(130, rng);
                        AddOrder(OrderType.Grows, celdasVivo3, 50, DescribirGrows(celdasVivo3), targetMat: MaterialId.Vivium);
                    }
                    break;
            }
        }

        /// <summary>
        /// (fix playtest 10) Descripción del encargo Grows: EFECTO/origen mientras
        /// Vivium siga innominado, su nombre en cuanto se bautice. Sin ambigüedad: en
        /// el roster fijo (Sim/Universe.cs) SOLO Vivium tiene arquetipo Organic (ver
        /// OrderSystem.MatchesOrder), así que "algo vivo que crezca solo" señala una
        /// única sustancia real del taller, nunca una familia difusa.
        /// </summary>
        private string DescribirGrows(int minCells)
        {
            string nombre = _knowledge != null ? _knowledge.NombreDe(MaterialId.Vivium) : "???";
            if (nombre == "???")
                return $"El Maestro quiere ver crecer algo vivo -- {minCells} celdas de lo que crece solo, sin que lo alimenten a mano.";
            return $"El Maestro quiere ver crecer algo vivo -- {minCells} celdas de lo que llamas \"{nombre}\".";
        }

        /// <summary>
        /// (fix playtest 10) Descripción del encargo CrystalSolid: origen mientras
        /// Crystal siga innominado, su nombre en cuanto se bautice. Deliberadamente NO
        /// menciona "azoth" (también innominado por defecto en esta ronda): decir su
        /// identidad aquí sería la misma circularidad que se corrigió en las pistas, así
        /// que el origen se describe por LUGAR ("la bandeja fría"), no por sustancia.
        /// Sin ambigüedad: OrderType.CrystalSolid solo hace match con matId==Crystal
        /// (ver MatchesOrder), es la única piedra que nace ahí.
        /// </summary>
        private string DescribirCrystalSolid(int minCells)
        {
            string nombre = _knowledge != null ? _knowledge.NombreDe(MaterialId.Crystal) : "???";
            if (nombre == "???")
                return $"El Maestro quiere una gran veta de la piedra que crece en la bandeja fría -- {minCells} celdas.";
            return $"El Maestro quiere una gran veta de \"{nombre}\" -- {minCells} celdas.";
        }

        /// <summary>(fix playtest 10) Descripción del encargo NamedMaterial: SIEMPRE por nombre (solo se genera para material ya bautizado, ver PickNamedMaterial), pero recalculable si el jugador lo re-bautiza (ver Update/RefreshDescripciones).</summary>
        private string DescribirNamedMaterial(int minCells, byte targetMat)
        {
            string nombre = _knowledge != null ? _knowledge.NombreDe(targetMat) : "???";
            return $"Trae {minCells} celdas de lo que llamas \"{nombre}\".";
        }

        /// <summary>
        /// (playtest 23) LOS PRIMEROS ENCARGOS DEL PIVOT -- los que aparecen
        /// cuando el jugador cava hasta la Tolva. NO son los de la jornada 1
        /// clásica: aquellos (Flammable + Hot 80°C) eran IMPOSIBLES en el
        /// cuarto íntimo -- no hay aceite sin excavar el taller, y el anillo
        /// cálido de la criatura empuja como mucho hacia ~54-84°C según
        /// semilla, nunca 80°C garantizados. El jugador habría cavado 23
        /// celdas de roca para encontrarse dos peticiones incumplibles: el
        /// premio de cavar convertido en un muro (regla 43: un encargo que no
        /// puedes cumplir tampoco se distingue de un juego roto).
        ///
        /// Los dos de ahora CIERRAN el bucle del slice, cada uno apuntando a
        /// una capacidad que el jugador ACABA de ganar:
        ///  · ALGO HELADO (-5°C): solo se fabrica con la cría FRÍA del
        ///    capullo (el ambiente jamás congela nada, ver regla 31; el hielo
        ///    conserva su temperatura en el frasco, ver Flask). Es la
        ///    validación externa de la capacidad nueva.
        ///  · LO QUE VOSOTROS BAUTIZASTEIS: si ya nombró algo (el producto de
        ///    la digestión, típicamente), el Maestro se lo pide POR SU
        ///    NOMBRE -- bautizar deja de ser decorativo y pasa a tener valor
        ///    mecánico. Si aún no nombró nada, este encargo simplemente no
        ///    aparece (un solo encargo es mejor que uno imposible).
        /// </summary>
        public void GenerateOrdersPivot()
        {
            ActiveOrders.Clear();
            int universeSeed = (_sim != null && _sim.Universe != null) ? _sim.Universe.Seed : 0;
            var rng = new System.Random(universeSeed * 31 + 1);

            int celdasFrio = Volumen(30, rng);
            AddOrder(OrderType.Cold, celdasFrio, 30,
                "Algo helado -- " + celdasFrio + " celdas a -5°C o menos. (Nada aquí abajo se congela solo...)",
                minTempC: -5);

            byte target = PickNamedMaterial(rng);
            if (target != MaterialId.Empty)
            {
                int celdasNombrado = Volumen(50, rng);
                AddOrder(OrderType.NamedMaterial, celdasNombrado, 35,
                    DescribirNamedMaterial(celdasNombrado, target), targetMat: target);
            }
        }

        // =================================================================
        // (playtest 25, CONTRATO_PERSISTE.md §6.1) EL ARCO DE "LO QUE
        // PERSISTE" -- reemplaza a GenerateOrdersPivot en el laboratorio
        // (ver Game/DayCycle.cs): CINCO pedidos de UNO EN UNO, textos e
        // ÍNDICES LITERALES del contrato ("el arco ES el tutorial", no se
        // reordena ni se sortea). La temperatura del nº2 SIEMPRE sale de
        // Universe.TempEnsayoCalorRaw (calibrada por el solver de A) --
        // jamás un número inventado aquí: "se acabaron los pedidos
        // imposibles" es la lección del playtest 22 que este arco no puede
        // repetir.
        //
        // RECOMPENSAS: crecientes, la 5ª paga el DOBLE (contrato, "el
        // conocimiento vale más que la sustancia"). Los números concretos
        // (30/40/45/55/110) NO están fijados por el contrato más allá de esa
        // forma -- DECISIÓN de este encargo, calibrada contra
        // StartingFavor=20 para que el arco entero sea alcanzable sin volver
        // a la Tolva vacía: 30+40+45+55+110 = 280 Favor solo con este arco,
        // muy por encima de MaestroFavorTarget=260 (con margen para lo que
        // se gaste en grifos/experimentar).
        // =================================================================
        private static readonly string[] ArcoPersisteTextos =
        {
            "Sepárame el limo primordial: tráeme una sola de sus arenas, pura.",
            // (playtest 51, ENCARGO OrderSystem, feedback de Cesar en el playtest
            // 50b) ANTES decía "Algo que aguante el rojo del crisol sin ceder" y
            // Cesar, jugando este MISMO arco clásico en la SEMILLA CERO
            // COMPARTIDA del multi (ver GenerateOrdersSemillaCompartida más abajo
            // para por qué cae aquí: TrySpawnRed nunca instancia el director de
            // beats de Game/SemillaCero.cs) reportó no entender ni el texto ni lo
            // que pedía. Reescrito con la MISMA frase que ya usa
            // Game/SemillaCero.cs beat 5.4 (línea "sobreviva al rojo sin arder ni
            // fundirse -- lo bien cocido aguanta", validada por Cesar en el
            // playtest 49/50): dice DÓNDE se ensaya (el Ensayo del Maestro, no la
            // Tolva -- MatchesOrder nunca deja este tipo coincidir ahí) y CÓMO se
            // gana (lo cocido, con ejemplos concretos), en vez del acertijo
            // "aguante... sin ceder" que no daba ninguna pista accionable.
            "Trae al ENSAYO del Maestro algo que sobreviva al rojo sin arder ni fundirse -- lo bien cocido aguanta (cerámica, ladrillo).",
            "Algo que encienda mi lámpara.",
            "Algo que flote en el agua sin deshacerse en ella.",
            "El cómo del nº2, por escrito en tu libro.",
        };

        private static readonly OrderType[] ArcoPersisteTipos =
        {
            OrderType.Pureza, OrderType.AguantaCalor, OrderType.Conduce, OrderType.FlotaInsoluble, OrderType.Procedimiento,
        };

        // MinCells: Pureza/FlotaInsoluble cuentan celdas de verdad en la
        // Tolva; AguantaCalor/Conduce/Procedimiento son "de un solo golpe"
        // (el Ensayo o la primera celda tras patentar los completa entero),
        // así que su MinCells es 1 -- OrdersHud (fuera de este encargo) ya
        // sabe leer "0/1" -> "hecho" sin ningún cambio ahí.
        private static readonly int[] ArcoPersisteMinCells = { 25, 1, 1, 20, 1 };
        private static readonly int[] ArcoPersisteRecompensa = { 30, 40, 45, 55, 110 };

        /// <summary>
        /// (playtest 25) DayCycle la llama UNA vez, al primer momento
        /// jugable del laboratorio (ver su docblock en Game/DayCycle.cs).
        /// Arranca el arco en el pedido 0 -- el resto se encadena solo
        /// (ver AvanzarArcoPersisteSiToca, llamado desde TryDeliverCell y
        /// CompletarEnsayo cada vez que un pedido se completa de verdad).
        /// </summary>
        public void GenerateOrdersPersiste()
        {
            ActiveOrders.Clear();
            _arcoPersisteIndex = 0;
            AddArcoPersisteOrder(_arcoPersisteIndex);
        }

        private void AddArcoPersisteOrder(int i)
        {
            AddOrder(ArcoPersisteTipos[i], ArcoPersisteMinCells[i], ArcoPersisteRecompensa[i], ArcoPersisteTextos[i]);
        }

        /// <summary>
        /// Llamado tras marcar Completado=true en CUALQUIER pedido mientras
        /// el arco de LO QUE PERSISTE está activo. Como el arco solo tiene
        /// UN pedido vivo en <see cref="ActiveOrders"/> a la vez (se vacía y
        /// se repone aquí mismo), no hace falta comprobar CUÁL se completó:
        /// solo puede ser el actual.
        /// </summary>
        private void AvanzarArcoPersisteSiToca()
        {
            if (_arcoPersisteIndex < 0) return;
            _arcoPersisteIndex++;
            ActiveOrders.Clear();
            if (_arcoPersisteIndex < ArcoPersisteCount) AddArcoPersisteOrder(_arcoPersisteIndex);
            // >= ArcoPersisteCount: arco terminado -- se deja ActiveOrders vacío
            // a propósito (el arco no se repite, v0: el laboratorio no tiene
            // "jornada siguiente" que lo reponga).
        }

        /// <summary>
        /// (playtest 25) Completa un pedido AguantaCalor/Conduce del arco --
        /// los ÚNICOS dos tipos que <see cref="MatchesOrder"/> nunca deja
        /// coincidir en la Tolva (contrato §6.1: "se resuelve en el Ensayo").
        /// La llama <see cref="EnsayoMaestro"/> tras un ensayo con éxito.
        /// `factorFavor` es el multiplicador de estrellas (x1/x1.5/x2, ver
        /// contrato §6.2); el Favor final se redondea al entero más cercano.
        /// Devuelve false si no hay un pedido INCOMPLETO de ese tipo activo
        /// ahora mismo (nada que completar -- el Ensayo no debería llamar
        /// aquí sin comprobar antes, pero la API se defiende igual).
        /// </summary>
        public bool CompletarEnsayo(OrderType tipo, float factorFavor)
        {
            for (int i = 0; i < ActiveOrders.Count; i++)
            {
                var order = ActiveOrders[i];
                if (order.Tipo != tipo || order.Completado) continue;

                order.Progreso = order.MinCells;
                order.Completado = true;
                int favor = Mathf.RoundToInt(order.Recompensa * factorFavor);
                AddFavor(favor);
                Debug.Log($"[ChaosAlchemy] Ensayo superado: {order.Descripcion} (+{favor} Favor, x{factorFavor:0.0}).");
                AvanzarArcoPersisteSiToca();
                return true;
            }
            return false;
        }

        // =================================================================
        // (playtest 51, EL RECETARIO DEL LABORATORIO -- CONTRATO_RONDA51.md,
        // feedback de Cesar en el playtest 50b) LA SEMILLA CERO COMPARTIDA DEL
        // MULTI (botón "ANFITRIÓN -- SEMILLA CERO compartida" en
        // Net/TallerSesionHud.cs) fija el mundo a Universe.SemillaCero=777002 +
        // overrides + veta + TODAS las salas destapadas de una vez, SIN el
        // director de beats (Game/AlkahestGameBootstrap.cs::TrySpawnRed NUNCA
        // instancia Game/SemillaCero.cs -- pedido textual de Cesar, "un
        // laboratorio para pruebas en simultáneo", no el arco guiado). Con el
        // director ausente, TrySpawnRed llamaba a GenerateOrdersPersiste() --
        // el arco clásico genérico de "LO QUE PERSISTE" -- que Cesar reportó
        // no entender ("¿qué significa 'algo que aguante el calor del crisol
        // sin ceder'?... debería pedirme la transformación del carbón, o la
        // arena con agua, no algo que obtenga después de 3-4 procesos").
        //
        // Tenía razón: el arco clásico no sabe NADA de la seed 777002 (habla
        // por EFECTO/tabla, nunca por identidad real, porque en el caótico
        // nada tiene nombre real) -- en Semilla Cero, en cambio, TODO el
        // retículo base×estado YA tiene nombre real desde el primer instante
        // (Universe.TieneIdentidadReal, ver SubstanceKnowledge.cs). Este
        // método reemplaza GenerateOrdersPersiste SOLO para el botón de la
        // Semilla Cero compartida (ver la línea que elige el generador en
        // AlkahestGameBootstrap.TrySpawnRed) con un arco FIJO de 5 pedidos que
        // enseña la cadena temprana REAL de esa seed, de lo simple a lo
        // complejo, CON NOMBRES REALES -- exactamente lo que Cesar pidió.
        //
        // Cadena verificada leyendo Sim/Universe.cs (ConstruirIdentidadReal +
        // AplicarOverridesSemillaCero) y Sim/SimLevelBuilder.cs (BuildVetaTurba):
        //  1. ARENA DE SÍLICE = MatDe(0,Polvo): la extracción "a fuego propio"
        //     (Universe.SemillaCeroBaseIdx=0, banda MUY por debajo del rescoldo
        //     tier0) -- ninguna otra base sale del limo sin combustible. Es el
        //     mismo material del beat 1/2 del director single-player.
        //  2. TURBA = MatDe(3,Polvo): YA NO sale del limo en esta seed (D1 del
        //     playtest 48, ver el override 1) -- se TALLA de la veta parda del
        //     muro con el Cincel (tecla C), disponible desde el arranque. Es el
        //     "algo simple" que Cesar pedía como segundo paso, no un acertijo.
        //  3. CARBÓN VEGETAL = MatDe(3,Calcinado): calcinar la PROPIA turba en
        //     el crisol (turba cruda ya es combustible por sí misma a partir
        //     del override 1b, así que se autoalimenta) -- la "transformación
        //     del carbón" que Cesar pidió textualmente, UN solo proceso desde
        //     el material del pedido 2.
        //  4. ARENA TOSTADA = MatDe(0,Calcinado): calcinar arena usando turba
        //     como combustible en el brasero -- el MISMO material y mecánica
        //     que el beat 4 real del director single-player ("Más de eso, pero
        //     TOSTADO"), así que el arco de Semilla Cero compartida enseña
        //     exactamente la misma lección que el arco guiado, con las piezas
        //     que este pedido ya dejó en el frasco (pedidos 1 y 2).
        //  5. BARBOTINA = MatDe(1,Solucion): arcilla (extraída a más fuego,
        //     combustible turba/carbón) disuelta en agua. LEÍDO EL CÓDIGO antes
        //     de fijar el target (regla 50 de CLAUDE.md): Game/DeliveryChute.cs
        //     dice explícito que la Tolva "engulle -- sólido, líquido, polvo,
        //     da igual" (ArrastreTick no distingue arquetipo) y MatchesOrder
        //     para OrderType.Guiado solo compara matId, sin filtrar por
        //     arquetipo -- Solucion (líquido) SÍ es entregable en la Tolva HOY,
        //     así que no hace falta el sustituto de Mortero/Adobe que
        //     contemplaba el encargo: barbotina es el target real, y cierra el
        //     arco con la ÚNICA transición del retículo que pasa por AGUA (el
        //     eje que las 4 anteriores no tocan).
        //
        // TIPO: OrderType.Guiado (no NamedMaterial) A PROPÓSITO -- mismo motivo
        // que Game/SemillaCero.cs: RefreshDescripciones() SOLO recalcula
        // Grows/CrystalSolid/NamedMaterial al subir NamingVersion, así que un
        // NamedMaterial aquí se reescribiría con la plantilla genérica "Trae N
        // celdas de lo que llamas..." en cuanto CUALQUIER jugador de la partida
        // compartida bautizara CUALQUIER cosa (los 6 clásicos innominados siguen
        // vivos en Semilla Cero) -- Guiado es inmune a ese refresco (ver el
        // switch de RefreshDescripciones, default: conserva la Descripcion), así
        // que el texto a mano de este recetario nunca se pisa.
        //
        // CANTIDADES CHICAS (regla 43 de CLAUDE.md): 10/8/6/6/6 -- el mismo
        // orden de magnitud que ya validó Cesar en el arco guiado de un jugador
        // (Beat2Cantidad=10, Beat3Cantidad=8, Beat4Cantidad=8, Beat5*Cantidad=6
        // en Game/SemillaCero.cs), no un rediseño de balance nuevo. Recompensas
        // crecientes 20/20/30/30/40 (140 total): el laboratorio compartido no
        // tiene desenlace (Semilla Cero no pasa por DayCycle.TerminarPartida
        // igual que el resto del multi), así que no hay umbral de Favor que
        // cuadrar -- solo importa que la progresión SE LEA como una escalera.
        // =================================================================
        private static readonly string[] ArcoRecetarioTextos =
        {
            "Sepárame la primera arena del limo: enciende el crisol (E) y tráeme 10 de arena de sílice.",
            "Esa veta parda del muro es turba: tállala con el cincel (C) y tráeme 8.",
            "Tuesta esa turba en el crisol, sin nada más -- tráeme 6 de carbón vegetal.",
            "Ahora calcina tu arena: aliméntala con turba en el brasero -- tráeme 6 de arena tostada.",
            "Saca arcilla del limo a más fuego y disuélvela en agua -- tráeme 6 de barbotina.",
        };

        private static readonly byte[] ArcoRecetarioTargets =
        {
            MaterialId.MatDe(0, EstadoMateria.Polvo),      // arena de sílice
            MaterialId.MatDe(3, EstadoMateria.Polvo),      // turba (base3 = veta vegetal)
            MaterialId.MatDe(3, EstadoMateria.Calcinado),  // carbón vegetal
            MaterialId.MatDe(0, EstadoMateria.Calcinado),  // arena tostada
            MaterialId.MatDe(1, EstadoMateria.Solucion),   // barbotina (base1 = arcilla)
        };

        private static readonly int[] ArcoRecetarioMinCells = { 10, 8, 6, 6, 6 };
        private static readonly int[] ArcoRecetarioRecompensa = { 20, 20, 30, 30, 40 };

        /// <summary>
        /// (playtest 51) Llamada desde AlkahestGameBootstrap.TrySpawnRed SOLO
        /// cuando <c>ModoSemillaCero</c> está activo (el botón "SEMILLA CERO
        /// compartida" del lobby) -- reemplaza a <see cref="GenerateOrdersPersiste"/>
        /// en ese único camino; el arco clásico de "LO QUE PERSISTE" sigue
        /// intacto para el laboratorio de un jugador y el multi normal. Arranca
        /// el arco en el pedido 0; el resto se encadena solo (ver
        /// AvanzarArcoRecetarioSiToca, llamado desde TryDeliverCell).
        /// </summary>
        public void GenerateOrdersSemillaCompartida()
        {
            ActiveOrders.Clear();
            _arcoRecetarioIndex = 0;
            AddArcoRecetarioOrder(_arcoRecetarioIndex);
        }

        private void AddArcoRecetarioOrder(int i)
        {
            AddOrder(OrderType.Guiado, ArcoRecetarioMinCells[i], ArcoRecetarioRecompensa[i], ArcoRecetarioTextos[i],
                targetMat: ArcoRecetarioTargets[i]);
        }

        /// <summary>
        /// Llamado tras marcar Completado=true en CUALQUIER pedido mientras el
        /// recetario de la Semilla Cero compartida está activo. Mismo criterio
        /// que <see cref="AvanzarArcoPersisteSiToca"/>: el arco solo tiene UN
        /// pedido vivo en <see cref="ActiveOrders"/> a la vez, así que no hace
        /// falta comprobar CUÁL se completó.
        /// </summary>
        private void AvanzarArcoRecetarioSiToca()
        {
            if (_arcoRecetarioIndex < 0) return;
            _arcoRecetarioIndex++;
            ActiveOrders.Clear();
            if (_arcoRecetarioIndex < ArcoRecetarioCount) AddArcoRecetarioOrder(_arcoRecetarioIndex);
            // >= ArcoRecetarioCount: arco terminado -- ActiveOrders vacío a
            // propósito, mismo criterio que el arco de LO QUE PERSISTE (el
            // laboratorio compartido no repone "jornada siguiente").
        }

        private void AddNamedOrFallback(System.Random rng)
        {
            byte target = PickNamedMaterial(rng);
            if (target != MaterialId.Empty)
            {
                // NamedMaterial 100->70 (-30%): reutiliza lo que el grupo ya
                // sabe reproducir (por definición, está bautizado porque ya
                // se descubrió y se sabe recrear) -- el grifo/mezcla que lo
                // produce está muy por debajo del techo de 20-150 cel/s
                // medido en cabecera, así que el coste real es de viajes de
                // frasco, no de producción.
                int celdasNombrado = Volumen(70, rng);
                AddOrder(OrderType.NamedMaterial, celdasNombrado, 35, DescribirNamedMaterial(celdasNombrado, target), targetMat: target);
            }
            else
            {
                // Nadie ha bautizado nada todavía: encargo de reserva.
                // 150->100 (-33%), misma proporción que el resto del día 3;
                // sigue siendo mayor que el NamedMaterial equivalente (100 vs
                // 70) porque verter un básico sin descubrir nada es más fácil
                // por celda que cumplir el encargo "de verdad".
                int celdasReserva = Volumen(100, rng);
                AddOrder(OrderType.Flammable, celdasReserva, 35,
                    "Nada tiene nombre todavía -- trae " + celdasReserva + " celdas de algo inflamable.");
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

        // =================================================================
        // (Encargo G, SEMILLA CERO, CONTRATO_SEMILLA.md §2) EL PEDIDO GUIADO.
        // =================================================================
        /// <summary>
        /// En modo Semilla 0, <see cref="OrderSystem"/> NO genera pedidos procedurales:
        /// <c>Game/SemillaCero.cs</c> (el director del arco) dicta la secuencia del
        /// contrato §1 beat a beat, uno en uno, con sus textos EXACTOS -- este método es
        /// esa vía. Reemplaza SIEMPRE el pedido activo entero (nunca se acumulan dos:
        /// Semilla 0 enseña una sola pregunta/petición cada vez, mismo criterio que ya usa
        /// <see cref="GenerateOrdersPersiste"/> para "LO QUE PERSISTE"), y usa
        /// <see cref="OrderType.Guiado"/> por defecto para que
        /// <see cref="RefreshDescripciones"/> nunca reescriba el texto del guion con la
        /// plantilla genérica de <see cref="NamedMaterial"/> (ver el docblock de
        /// <see cref="OrderType.Guiado"/> en Game/Order.cs). El llamante puede pasar otro
        /// `tipo` (p. ej. <see cref="OrderType.Conduce"/>/<see cref="OrderType.AguantaCalor"/>
        /// para las preguntas del banco de chispa/Ensayo, o
        /// <see cref="OrderType.FlotaInsoluble"/> para la de la columna, contrato §1 beat 5)
        /// cuando el pedido deba resolverse por la vía YA existente de esos tipos.
        /// </summary>
        public void EncolarPedidoGuiado(OrderType tipo, int minCells, int recompensa, string descripcion,
            byte? targetMat = null, int? minTempC = null)
        {
            ActiveOrders.Clear();
            AddOrder(tipo, minCells, recompensa, descripcion, minTempC, targetMat);
            // (Semilla 0 no usa el arco de "LO QUE PERSISTE": _arcoPersisteIndex se queda
            // como esté -- normalmente -1, nunca activado en este modo -- así que
            // AvanzarArcoPersisteSiToca sigue siendo un no-op cuando este pedido se
            // complete. La propia SemillaCero.cs sondea ActiveOrders para avanzar su beat.)
        }

        /// <summary>
        /// Resultado de intentar entregar una celda en la Tolva (fix playtest 9).
        /// Antes esto era un bool: "encajó / no encajó (=chatarra, pagaba Favor
        /// igualmente)". Chatarra desaparece del todo (ver docblock de la
        /// clase y CHANGELOG en DeliveryChute), así que ahora hace falta
        /// distinguir DOS motivos de "no cuenta" -- son experiencias muy
        /// distintas para quien juega: uno dice "te has equivocado de bote",
        /// el otro dice "ya no hacía falta esto, has malgastado tiempo".
        /// </summary>
        public enum DeliveryOutcome
        {
            /// <summary>La celda hizo avanzar (o completó) un encargo incompleto.</summary>
            Progressed,
            /// <summary>El material SÍ es el que pide un encargo, pero ese encargo ya está completo -- el trabajo se ha desperdiciado.</summary>
            OrderAlreadyComplete,
            /// <summary>El material no lo pide ningún encargo, completo o no.</summary>
            NoMatch,
        }

        /// <summary>
        /// Evalúa una celda consumida en la Tolva del Maestro (ver
        /// DeliveryChute) contra TODOS los encargos activos, EN EL ORDEN en
        /// que aparecen en <see cref="ActiveOrders"/>. Si el primero que
        /// coincide está incompleto, avanza su progreso (completándolo y
        /// otorgando Favor si llega al mínimo) y devuelve
        /// <see cref="DeliveryOutcome.Progressed"/>. Si coincide solo con
        /// encargos YA completos, devuelve
        /// <see cref="DeliveryOutcome.OrderAlreadyComplete"/> (el material era
        /// correcto, pero sobra). Si
        /// no coincide con ninguno, <see cref="DeliveryOutcome.NoMatch"/>.
        ///
        /// (fix playtest 9) Antes devolvía bool y, si no encajaba con ningún
        /// encargo INCOMPLETO, DeliveryChute lo contaba como "chatarra" y
        /// otorgaba 1 Favor cada N celdas -- daba igual que el motivo fuera
        /// "material equivocado" o "encargo ya cumplido": la Tolva decía "esto
        /// no lo necesito" y en el mismo gesto pagaba por ello. Eliminado
        /// (Favor SOLO sale de <see cref="AddOrder"/> al completar un
        /// encargo, nunca de aquí) porque:
        ///  1. Rompe la ficción: el Maestro paga por lo que ENCARGÓ, no por
        ///     escombros que caen en su Tolva.
        ///  2. Rompía los cuatro desenlaces: si cualquier basura sumaba
        ///     Favor sin límite, los umbrales de arriba (120/180/260) dejaban
        ///     de medir "cuánto encargo real completaste" -- medían "cuánto
        ///     rato llevas vertiendo cosas", que no es lo que el juego quiere
        ///     puntuar.
        ///  3. Y sobre todo, contradecía al propio juego: un mensaje ("esto
        ///     no lo necesito") y una recompensa (+1 Favor) que se llevan la
        ///     contraria enseñan a ignorar los dos. El jugador dejaba de
        ///     fiarse tanto del aviso como del contador.
        /// </summary>
        public DeliveryOutcome TryDeliverCell(Universe universe, byte matId, byte tempRaw)
        {
            bool matchedCompletedOnly = false;
            for (int i = 0; i < ActiveOrders.Count; i++)
            {
                var order = ActiveOrders[i];
                if (!MatchesOrder(universe, order, matId, tempRaw)) continue;

                if (order.Completado)
                {
                    // Material correcto, pero este encargo concreto ya no lo
                    // necesita -- puede que otro encargo, más abajo en la
                    // lista, SÍ siga incompleto y también acepte este
                    // material (p.ej. NamedMaterial ya completo + Flammable
                    // aún activo con el mismo material inflamable), así que
                    // no se corta el bucle: se sigue buscando un incompleto.
                    matchedCompletedOnly = true;
                    continue;
                }

                // (playtest 25) PUREZA fija su objetivo con la PRIMERA celda
                // válida que recibe: MatchesOrder ya comprobó que matId es un
                // Polvo de base×estado (o que coincide con el LockedMat ya
                // fijado); aquí, y SOLO aquí (TryDeliverCell es el único
                // sitio que puede mutar Order, MatchesOrder es estático), se
                // fija el candado la primera vez. "Traedme una sola de sus
                // arenas, PURA" -- a partir de este punto, solo esa base
                // exacta cuenta para el resto del pedido.
                if (order.Tipo == OrderType.Pureza && !order.LockedMat.HasValue)
                {
                    order.LockedMat = matId;
                }

                order.Progreso++;
                if (order.Progreso >= order.MinCells)
                {
                    order.Completado = true;
                    AddFavor(order.Recompensa);
                    Debug.Log($"[ChaosAlchemy] Encargo completado: {order.Descripcion} (+{order.Recompensa} Favor).");
                    AvanzarArcoPersisteSiToca(); // no-op si el arco de LO QUE PERSISTE no está activo (_arcoPersisteIndex==-1).
                    AvanzarArcoRecetarioSiToca(); // no-op si el recetario de la Semilla Cero compartida no está activo (_arcoRecetarioIndex==-1).
                }
                return DeliveryOutcome.Progressed;
            }
            return matchedCompletedOnly ? DeliveryOutcome.OrderAlreadyComplete : DeliveryOutcome.NoMatch;
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

                // =============================================================
                // (playtest 25, CONTRATO_PERSISTE.md §6.1) LOS CINCO TIPOS NUEVOS.
                // =============================================================
                case OrderType.Pureza:
                    // "N celdas del MISMO Polvo base -- cualquiera": cualquier
                    // base×estado en su estado NATAL (Polvo) vale como primera
                    // celda; a partir de ahí, order.LockedMat (fijado en
                    // TryDeliverCell, MatchesOrder es estático y no puede
                    // mutar) exige la MISMA base exacta -- "pura", no mezcla.
                    if (!MaterialId.EsBaseEstado(matId)) return false;
                    if (MaterialId.EstadoDe(matId) != EstadoMateria.Polvo) return false;
                    return !order.LockedMat.HasValue || matId == order.LockedMat.Value;

                case OrderType.FlotaInsoluble:
                    // Por TABLA (contrato §6.1): densidad menor que la del
                    // agua Y no soluble. No se restringe a base×estado a
                    // propósito -- el criterio es literal, cualquier material
                    // de este universo que lo cumpla vale (v0: sin teatro,
                    // solo la comprobación de tabla, tal y como pide el
                    // contrato -- "teatro en v2").
                    return universe.Get(matId).density < universe.Get(MaterialId.Water).density
                        && !universe.SolubleEnAgua(matId);

                case OrderType.Procedimiento:
                    // Se autocompleta al entregar CUALQUIER celda mientras
                    // haya ≥1 patente registrada -- "el cómo, por escrito",
                    // no una sustancia concreta (contrato §6.1).
                    return Hornada.TieneAlMenosUnaPatente();

                case OrderType.AguantaCalor:
                case OrderType.Conduce:
                    // NUNCA coinciden aquí: se resuelven en el Ensayo del
                    // Maestro (ver EnsayoMaestro.cs + OrderSystem.CompletarEnsayo),
                    // nunca vertiendo en la Tolva (contrato §6.1).
                    return false;

                // (Encargo G, SEMILLA CERO, CONTRATO_SEMILLA.md §2) EL PEDIDO GUIADO: mismo
                // criterio que NamedMaterial (matId exacto), ver el docblock de
                // OrderType.Guiado en Game/Order.cs para por qué es un tipo aparte.
                case OrderType.Guiado:
                    return order.TargetMat.HasValue && matId == order.TargetMat.Value;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Nombre legible de un material para mensajes de <see cref="DeliveryChute"/>
        /// cuando una entrega NO cuenta para ningún encargo (fix playtest 8: el
        /// jugador reportó "problema para entregar sólidos de combinaciones
        /// raras" -- investigando, esas entregas SÍ se tragaban, pero la tolva
        /// solo decía "chatarra" sin decir DE QUÉ, así que se sentía como un
        /// agujero negro en vez de una regla legible). Reutiliza el mismo
        /// nombre que ya muestran el HUD del frasco y la estantería (bautizado
        /// del jugador &gt; nombre común de taller &gt; "???"), nunca el devName
        /// interno en inglés.
        /// </summary>
        public string NombreParaMensaje(byte matId)
        {
            if (_knowledge != null) return _knowledge.NombreParaHud(matId);
            return SubstanceKnowledge.NombreComun(matId) ?? "???";
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
