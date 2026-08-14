using UnityEngine;
using Alkahest.Sim;
using Alkahest.Audio;

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
    ///
    /// =====================================================================
    /// EL CUARTO ÍNTIMO (playtest 21, EL PIVOT): TrySpawn() ES UNA LISTA
    /// PLANA DE LLAMADAS, Y ESO YA BASTABA PARA BIFURCARLA.
    /// =====================================================================
    /// `Sim/AlkahestSim.cs` ya no construye el taller clásico
    /// (`SimLevelBuilder.BuildTestLevel`) sino el cuarto íntimo excavado en
    /// piedra (`SimLevelBuilder.BuildCuartoIntimo`, ver su docblock). Este
    /// archivo sigue esa misma decisión SIN BORRAR ni una línea: las placas
    /// ígneas, la piedra gélida, los grifos, el estante y las muestras del
    /// Maestro no tienen sitio en una sala "grande y casi vacía" -- sus
    /// métodos (`SpawnHeatPlates`/`SpawnChillStone`/`SpawnDispensers`/
    /// `SpawnStorageRack`, más la creación de `MasterSupplies`) se QUEDAN
    /// definidos, tal cual, para el día en que el jugador excave hasta el
    /// taller clásico enterrado y haga falta volver a llamarlos -- solo se
    /// SALTAN sus llamadas dentro de `TrySpawn()`, comentadas ahí mismo con
    /// el porqué. Lo que SÍ se instancia: el aprendiz (frasco+cincel+
    /// mudanza), el conocimiento de sustancias, el diario, la Tolva
    /// (`DeliveryChute` -- su boca existe aunque esté sellada tras la roca,
    /// ver `SimLevelBuilder.BuildDeliveryNiche`), el sistema de encargos, y
    /// LAS DOS CRIATURAS (`Criatura`/`Capullo`, `Game/Criatura.cs` y
    /// `Game/Capullo.cs`, propiedad del otro encargo de esta ronda -- API
    /// congelada en CONTRATO_PIVOT.md, copiada VERBATIM más abajo).
    /// </summary>
    public sealed class AlkahestGameBootstrap : MonoBehaviour
    {
        private AlkahestSim _sim;
        private bool _spawned;

        // (M5 audio) Los cinco grifos, guardados aquí al crearlos en
        // SpawnDispensers: Audio/DirectorDeAudio.cs necesita sus referencias
        // (posición + material) para poner una voz de bucle por grifo, y el
        // patrón de todo este archivo es inyección de dependencias explícita
        // -- nunca un Find* -- así que se le pasan directamente en vez de
        // que el director tenga que buscarlos por su cuenta.
        private Dispenser[] _dispensers;

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

            // (playtest 21, EL PIVOT) LA MAQUINARIA DEL TALLER CLÁSICO SE
            // SALTA, NO SE BORRA -- ver el docblock de la clase. Placas
            // ígneas, piedra gélida, grifos y estante no tienen sitio en el
            // cuarto íntimo (SimLevelBuilder.BuildCuartoIntimo no excava
            // hueco para ninguno de los cuatro): sus métodos siguen
            // definidos más abajo, intactos, para cuando el jugador excave
            // hasta el taller enterrado.
            //   SpawnHeatPlates(apprentice.transform);
            //   SpawnChillStone(apprentice.transform);

            var orderSystem = SpawnOrderSystem(knowledge);
            // SpawnDispensers necesitaba ir aquí (grifoAzoth alimentaba
            // MasterSupplies.Init más abajo) -- ambos se saltan juntos, ver
            // el bloque de "muestras del Maestro" unas líneas más abajo.
            //   var grifoAzoth = SpawnDispensers(apprentice.transform, orderSystem);
            SpawnDeliveryChute(orderSystem); // la Tolva SIGUE EXISTIENDO, sellada tras la roca (ver BuildDeliveryNiche).
            //   SpawnStorageRack(apprentice.transform, flask, knowledge);

            SpawnNamingUi(flask, knowledge);
            SpawnJournalHud(knowledge);
            SpawnOrdersHud(orderSystem);

            // Fondo del taller + pistas se crean ANTES que el ciclo de
            // jornadas: DayCycle avisa a HintSystem en cuanto entra en la
            // intro de la jornada 1 (ver DayCycle.Init). LAS MUESTRAS DEL
            // MAESTRO NO SE INSTANCIAN EN ESTE MODO (contrato: "nadie te la
            // dio", no hay Maestro al principio) -- `supplies` se queda en
            // `null` y se pasa así a DayCycle/SpawnDayCycle, que ya
            // contemplaba ese caso (`Init(..., supplies = null, ...)`).
            // EFECTO COLATERAL (comprobado, ver el informe de la ronda): con
            // la sesión sin reloj (Game/DayCycle.cs) la partida nunca avanza
            // a la jornada 2, así que MasterSupplies.AlEmpezarJornada(2)
            // JAMÁS se dispararía aunque `supplies` existiera -- da igual en
            // este modo (la criatura es la vida del juego, no el grifo de
            // Azoth), pero no dejarlo al azar: es la razón real, no una
            // casualidad de que el campo se haya quedado en null.
            new GameObject("WorkshopBackdrop").AddComponent<WorkshopBackdrop>();
            var hints = new GameObject("HintSystem").AddComponent<HintSystem>();
            MasterSupplies supplies = null;
            //   var supplies = new GameObject("MasterSupplies").AddComponent<MasterSupplies>();
            //   supplies.Init(_sim, grifoAzoth);

            // LAS DOS CRIATURAS (contrato CONTRATO_PIVOT.md, firma VERBATIM
            // -- ver Game/Criatura.cs/Capullo.cs, propiedad del otro
            // encargo de esta ronda). Después del aprendiz (necesitan su
            // Transform) y antes del ciclo de jornadas, mismo criterio que
            // el resto de este método: todo lo que otro sistema pueda leer
            // por referencia se crea antes que ese sistema.
            var criaturaGo = new GameObject("Criatura");
            var criatura = criaturaGo.AddComponent<Criatura>();
            criatura.Init(_sim, apprentice.transform, SimLevelBuilder.CunaCriaturaX, SimLevelBuilder.CunaCriaturaY);

            var capulloGo = new GameObject("Capullo");
            var capullo = capulloGo.AddComponent<Capullo>();
            capullo.Init(_sim, apprentice.transform, SimLevelBuilder.CapulloX, SimLevelBuilder.CapulloY);

            SpawnDayCycle(orderSystem, knowledge, supplies, hints);

            // (M5 audio) EL TALLER SUENA: se instancia AL FINAL, cuando ya
            // existen todas las dependencias que necesita (frasco, grifos,
            // conocimiento, encargos, jugador). Ver Audio/DirectorDeAudio.cs.
            SpawnDirectorDeAudio(orderSystem, knowledge, flask, apprentice.transform);

            _spawned = true;
            Debug.Log("[ChaosAlchemy] Capa de interacción inicializada (cuarto íntimo, playtest 21 -- taller clásico 768x288 enterrado bajo la roca).");
        }

        private ApprenticeController SpawnApprentice()
        {
            var go = new GameObject("Apprentice");
            // (playtest 21, EL PIVOT) Nace en SimLevelBuilder.AprendizX/Y --
            // una celda de aire del CUARTO ÍNTIMO, entre la cuna y la
            // repisa, con las dos a la vista sin que ninguna la tape (ver el
            // docblock de BuildCuartoIntimo). REEMPLAZA el criterio de
            // playtest 15 de abajo (flotar sobre la pila de recogida de
            // LABORATORIO, "el centro de operaciones" del taller CLÁSICO):
            // se conserva el párrafo, sin borrar, porque sigue siendo el
            // criterio correcto para cuando el jugador excave hasta ese
            // taller y el spawn vuelva a vivir ahí.
            //
            // (playtest 15, taller clásico) "arrancaba flotando sobre la cuba
            // izquierda de CULTIVO -- tenía sentido cuando la cámara
            // encuadraba el mundo ENTERO (una pantalla) y 'estar sobre la
            // cuba' bastaba para ver el taller entero de un vistazo. Con
            // SimRenderer siguiendo al aprendiz y mostrando solo ~una
            // pantalla (Tab la amplía), lo que importa ya no es 'verlo todo'
            // sino EMPEZAR donde están las herramientas: el encargo pide
            // explícitamente que arranque en LABORATORIO, 'el centro de
            // operaciones'. Se posiciona flotando sobre el centro de la pila
            // de recogida (BasinInterior)... Cultivo/Entrega quedan a un
            // vuelo corto a cada lado."
            float celda = SimRenderer.CellWorldSize;
            float x = (SimLevelBuilder.AprendizX + 0.5f) * celda;
            float y = (SimLevelBuilder.AprendizY + 0.5f) * celda;
            go.transform.position = new Vector3(x, y, 0f);

            var apprentice = go.AddComponent<ApprenticeController>();
            var flask = go.AddComponent<Flask>();
            flask.Init(_sim);

            // EL CINCEL (playtest 16): segunda herramienta que lleva el
            // aprendiz, en el MISMO GameObject que el frasco (comparte
            // ApprenticeController para alcance/CarryAnchor, ver Game/Cincel.cs).
            // Se alterna con C; mientras está inactivo no toca la grilla ni
            // pinta nada, así que crearlo aquí no cambia el comportamiento por
            // defecto (el frasco sigue mandando desde el primer frame).
            var cincel = go.AddComponent<Cincel>();
            cincel.Init(_sim);

            // LA MUDANZA (playtest 19, "taller movible"): tercera herramienta,
            // MISMO patrón que el Cincel de arriba -- mismo GameObject, mismo
            // Init(sim), se alterna con V y mientras está inactiva no toca
            // nada (ver Game/Mudanza.cs). Los aparatos movibles (HeatPlate/
            // ChillStone/Dispenser) se registran solos en su propio Init, así
            // que no hace falta pasarles nada más aquí.
            var mudanza = go.AddComponent<Mudanza>();
            mudanza.Init(_sim);

            // El conocimiento se crea ANTES que el HUD: el HUD del frasco lo
            // necesita para mostrar el nombre que el jugador le puso a cada
            // sustancia (o el nombre común) en vez del devName interno.
            var knowledge = go.AddComponent<SubstanceKnowledge>();
            knowledge.Init(_sim, flask);

            var hud = go.AddComponent<FlaskHud>();
            hud.Init(_sim, flask, knowledge);

            return apprentice;
        }

        /// <summary>
        /// Una placa ígnea bajo cada una de las dos cubas de CULTIVO (playtest
        /// 15: SIN CAMBIOS de fondo -- VatAX0/VatBX0 ya viven dentro de
        /// SimLevelBuilder.CultivoX0..X1, es la propia BuildCultivo la que las
        /// coloca ahí). Coordenadas 100% dinámicas (VatInteriorX0/X1 por cuba),
        /// así que el rediseño del plano no exige tocar nada aquí -- exactamente
        /// donde tienen que estar: es donde se cría el Vivium.
        /// (playtest 17: aquí decía además "Y donde el clima ya nace templado
        /// (CultivoAmbientRaw), así que la placa solo tiene que EMPUJAR desde
        /// ese punto de partida en vez de pelear contra un ambiente frío como
        /// haría en LABORATORIO/ENTREGA". ESO YA NO ES CIERTO: el clima por
        /// zona se retiró y el mundo entero nace a CellGrid.AmbientRaw, 20°C
        /// -- ver el docblock de SimLevelBuilder. La placa ahora empuja desde
        /// los mismos 20°C aquí que en cualquier otro sitio, que era justo el
        /// objetivo: que la ventaja la dé el APARATO, no la casilla. Coste
        /// medido: 6°C más de salto sobre una banda de crecimiento que empieza
        /// en ~30°C y que los 26°C de CULTIVO no alcanzaban de todos modos.)
        /// Se mantienen CENTRADAS en cada
        /// cuba (FootprintFraction de HeatPlate.Init ya las recorta al 40% del
        /// interior recibido) -- no hay razón de diseño para descentrarlas: la
        /// cuba es simétrica y la placa debe calentar su centro.
        /// </summary>
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

        /// <summary>
        /// La piedra gélida vive bajo la bandeja fría del estante superior, EN
        /// LABORATORIO -- decisión tomada y no cambiada en el playtest 15 tras
        /// comprobar el SÓTANO explícitamente (el encargo lo pedía):
        ///
        /// (playtest 17: el argumento de abajo empezaba por "el SÓTANO nace
        /// frío de verdad (SotanoAmbientRaw ~4°C) y sería el sitio gratis para
        /// cristalizar". Esa premisa YA NO EXISTE -- el clima por zona se
        /// retiró y el sótano nace a los mismos 20°C que el resto, ver el
        /// docblock de SimLevelBuilder. La conclusión NO cambia, y de hecho se
        /// refuerza: si ni siquiera queda el frío gratis, menos razón todavía
        /// para mover el aparato a una sala sin cubeta. El razonamiento
        /// geométrico completo se conserva porque sigue siendo el motivo real:)
        ///
        /// El SÓTANO sería, si tuviera clima frío, el sitio "gratis" para
        /// cristalizar -- PERO SimLevelBuilder.BuildSotano solo levanta una
        /// PLATAFORMA MACIZA bajo el pozo (SotanoPlinthX0..X1, construida con
        /// DrawSolidRect: un bloque SIN PAREDES), no una cubeta como
        /// ChillTray/las cubas de Cultivo (esas sí usan DrawUShape: paredes +
        /// suelo). CrystalSeed es Powder (cae, se puede desparramar) y Azoth es
        /// Liquid (fluye libremente): vertidos sobre una losa plana sin muros,
        /// en medio de una sala abierta por los cuatro costados, se saldrían
        /// del alcance de la piedra (3 filas de empuje térmico, FilaEmpujePct)
        /// en cuanto tocaran el borde de la plataforma -- Azoth en particular
        /// no tiene NADA que lo retenga y se derramaría de inmediato fuera de
        /// la zona fría, justo donde SÍ hace falta para que la reacción
        /// Azoth+CrystalSeed->Crystal ocurra. SimLevelBuilder no expone ninguna
        /// cubeta en el sótano y es de solo lectura en este encargo, así que
        /// mover el aparato ahí rompería la mecánica que debería facilitar en
        /// vez de mejorarla. Se queda en LABORATORIO, en ChillTray (SÍ es una
        /// cubeta con paredes, ver SimLevelBuilder.BuildLaboratorio) -- y
        /// FRESCA/HELANDO (ChillStone.cs) ya se calibran por seed para cruzar
        /// los umbrales de congelación/cristalización con margen de sobra
        /// desde CUALQUIER ambiente de partida, así que el coste real es solo
        /// "unos ticks más para llegar", no "no llega". El sótano queda como
        /// destino de una FASE FUTURA (backlog: "taller movible", CLAUDE.md)
        /// para cuando SimLevelBuilder pueda darle una cubeta propia.
        /// </summary>
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
        /// (playtest 15) SIN CAMBIOS de fondo: TapMountX/TapFirstY/TapStepY se
        /// leen de SimLevelBuilder (TapMountX=TapPillarX1, dentro de
        /// LabX0..LabX1) igual que antes -- la columna de grifos, la pila de
        /// recogida y el pilar que los sostiene siguen siendo, todos, del
        /// mismo LABORATORIO que el encargo pide ("centro de operaciones").
        ///
        /// Coste de Favor por activación: los básicos son gratis, los versátiles
        /// cuestan — fijado aquí, no en el propio Dispenser, para tener toda la
        /// economía en un sitio.
        /// </summary>
        private Dispenser SpawnDispensers(Transform player, OrderSystem orderSystem)
        {
            var agua = SpawnOneDispenser(player, "Water", MaterialId.Water, 0, orderSystem, 0, false);
            var arena = SpawnOneDispenser(player, "Sand", MaterialId.Sand, 1, orderSystem, 0, false);
            var aceite = SpawnOneDispenser(player, "Oil", MaterialId.Oil, 2, orderSystem, 2, false);
            var nutriente = SpawnOneDispenser(player, "Nutrient", MaterialId.Nutrient, 3, orderSystem, 5, false);
            var azoth = SpawnOneDispenser(player, "Azoth", MaterialId.Azoth, 4, orderSystem, 4, true);
            _dispensers = new[] { agua, arena, aceite, nutriente, azoth };
            return azoth;
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

        /// <summary>(playtest 15) SIN CAMBIOS de fondo: RackX0/X1/TopY viven dentro de LabX0..LabX1 -- el estante de redomas sigue en LABORATORIO.</summary>
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

        /// <summary>(playtest 15) SIN CAMBIOS de fondo: DeliveryChute lee su propia copia de las constantes de la boca (ChuteMouthX0..Y1, ver Game/DeliveryChute.cs) directamente de SimLevelBuilder, que las sitúa dentro de EntregaX0..X1 -- la Tolva sigue en ENTREGA sin que este método tenga que pasarle ninguna coordenada.</summary>
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

        /// <summary>(M5 audio) Ver Audio/DirectorDeAudio.cs -- el interruptor SistemaActivo de ese archivo apaga esto entero de un solo sitio si hiciera falta.</summary>
        private void SpawnDirectorDeAudio(OrderSystem orderSystem, SubstanceKnowledge knowledge, Flask flask, Transform player)
        {
            var go = new GameObject("DirectorDeAudio");
            var director = go.AddComponent<DirectorDeAudio>();
            director.Init(_sim, orderSystem, knowledge, flask, player, _dispensers);
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
