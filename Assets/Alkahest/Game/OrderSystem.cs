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
    /// - VivGrowChancePct = 60% de crear célula nueva al consumir un
    ///   Nutrient vecino en banda de temperatura.
    /// - Cadencia de intento: throttle de 1 de cada 4 ticks por celda
    ///   asentada = 7.5 intentos/s (a 30Hz) SI tiene un vecino Nutrient
    ///   disponible ese intento.
    /// - Cuba B (Sim/SimLevelBuilder.cs): interior (VatWidth58-2*3) x
    ///   (VatHeight40-3) = 52*37 = 1924 celdas -- NUNCA es el límite real.
    /// - El crecimiento es EXPONENCIAL mientras haya Nutrient y sitio (cada
    ///   célula nueva se vuelve, unos ticks después, una célula-frontera
    ///   más), así que el cuello de botella real es el ARRANQUE: el retoño
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
                    // SIN CAMBIOS -- jornada de referencia (Cesar la completó
                    // con margen en el playtest real). Con el grifo de Aceite
                    // dando 20-150 celdas/s (ver medición de cabecera) 60
                    // celdas de Flammable son un par de viajes de frasco, muy
                    // por debajo del 60-70% de los 360s de la jornada.
                    AddOrder(OrderType.Flammable, 60, 25,
                        "Trae algo que arda de verdad -- 60 celdas de material inflamable.");
                    // La placa ígnea (fuera del alcance de este archivo) es
                    // lo bastante rápida en el playtest real como para que
                    // 80 celdas a 80°C tampoco fuera el cuello de botella;
                    // se deja igual por la misma razón que el anterior.
                    AddOrder(OrderType.Hot, 80, 25,
                        "Algo que queme al tacto -- 80 celdas a 80°C o más.", minTempC: 80);
                    break;

                case 2:
                    // SIN CAMBIOS -- también referencia buena (completada).
                    // Los tres encargos de hoy usan EXACTAMENTE las tres muestras
                    // que el Maestro acaba de dejar (ver Game/MasterSupplies.cs):
                    // el retoño de vivium, la piedra gélida y el azoth + semilla.
                    AddOrder(OrderType.Grows, 120, 35,
                        "El Maestro quiere ver crecer algo vivo -- 120 celdas de Vivium.");
                    AddOrder(OrderType.Cold, 60, 30,
                        "Algo helado -- 60 celdas a -5°C o menos.", minTempC: -5);
                    // 70 celdas de Crystal es coherente con la medición de
                    // cabecera (~1-2 cel/s sostenidas con un frente recién
                    // sembrado): ronda 35-70s de conversión activa, primer
                    // contacto del jugador con la mecánica, sin exigir aún
                    // dominarla.
                    AddOrder(OrderType.CrystalSolid, 70, 40,
                        "Cristal, ni más ni menos -- 70 celdas.");
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
                    AddOrder(OrderType.CrystalSolid, 90, 45,
                        "El Maestro quiere una gran veta de cristal -- 90 celdas.");
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
                    AddOrder(OrderType.Grows, 130, 50,
                        "Que el taller rebose de vida -- 130 celdas de Vivium.");
                    break;
            }
        }

        private void AddNamedOrFallback(System.Random rng)
        {
            byte target = PickNamedMaterial(rng);
            if (target != MaterialId.Empty)
            {
                string nombre = _knowledge.NombreDe(target);
                // NamedMaterial 100->70 (-30%): reutiliza lo que el grupo ya
                // sabe reproducir (por definición, está bautizado porque ya
                // se descubrió y se sabe recrear) -- el grifo/mezcla que lo
                // produce está muy por debajo del techo de 20-150 cel/s
                // medido en cabecera, así que el coste real es de viajes de
                // frasco, no de producción.
                AddOrder(OrderType.NamedMaterial, 70, 35,
                    $"Trae 70 celdas de lo que llamáis \"{nombre}\".", targetMat: target);
            }
            else
            {
                // Nadie ha bautizado nada todavía: encargo de reserva.
                // 150->100 (-33%), misma proporción que el resto del día 3;
                // sigue siendo mayor que el NamedMaterial equivalente (100 vs
                // 70) porque verter un básico sin descubrir nada es más fácil
                // por celda que cumplir el encargo "de verdad".
                AddOrder(OrderType.Flammable, 100, 35,
                    "Nada tiene nombre todavía -- trae 100 celdas de algo inflamable.");
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
