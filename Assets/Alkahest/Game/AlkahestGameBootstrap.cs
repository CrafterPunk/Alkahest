using UnityEngine;
using Alkahest.Sim;
using Alkahest.Audio;
using Alkahest.Net;

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
    ///
    /// =====================================================================
    /// LO QUE PERSISTE (playtest 25, CONTRATO_PERSISTE.md): EL LABORATORIO
    /// REAMUEBLA EL MISMO CUARTO, LA CRIATURA SE APARCA.
    /// =====================================================================
    /// Mismo cuarto íntimo, mismo criterio de "TrySpawn() es una lista plana
    /// de llamadas" del bloque de arriba -- esta vez la bifurcación es al
    /// revés: la Criatura y el Capullo (contrato §1, "quedan APARCADOS: sus
    /// archivos no se tocan y NO se spawnean en esta versión") pasan a ser
    /// las llamadas COMENTADAS (estilo regla 15 de CLAUDE.md, con la nota
    /// "criatura aparcada — LO QUE PERSISTE" en el propio comentario, más
    /// abajo en `TrySpawn()`), y lo que se instancia de nuevo son las TRES
    /// MÁQUINAS de este contrato (`Crisol`/`Prensa`/`BancoChispa`, §5, en las
    /// constantes de `SimLevelBuilder` §4.5 que define el encargo A EN
    /// PARALELO) y el `EnsayoMaestro` del encargo C (§6.2/§6.5, firma
    /// congelada). El caño que emitía Nutrient pasa a emitir
    /// `MaterialId.Limo` (regla 47: sigue siendo la boca del CUARTO ÍNTIMO,
    /// `SimLevelBuilder.CanoNutrienteY`, no la del taller clásico). Ninguna
    /// de las tres máquinas nuevas necesitó tocar `Sim/SimLevelBuilder.cs`
    /// (fuera del alcance de este archivo): cada una talla su propia
    /// mampostería en `Init()` a partir del ancla de una celda que sí define
    /// el contrato -- ver el docblock de `Game/Crisol.cs` para el porqué.
    /// </summary>
    public sealed class AlkahestGameBootstrap : MonoBehaviour
    {
        // =====================================================================
        // SEMILLA CERO (playtest 40, CONTRATO_SEMILLA.md §3). El flag estático
        // congelado que decide TODO lo demás de esta ronda: la pantalla de
        // entrada lo fija ANTES de cargar la escena (mismo patrón que
        // `AlkahestSim.NextRunSeed`, se lee y se consume aquí, nunca se
        // resetea solo -- quien entra "MODO CAÓTICO" o recarga sin pasar por
        // el título de nuevo debe dejarlo en `false` explícitamente, ver
        // `Game/DayCycle.cs`/la pantalla de título). `Game/AlkahestSim.cs` lo
        // lee para elegir la seed y aplicar los overrides de `Universe`;
        // este archivo lo lee para tapiar/destapar; `Game/Crisol.cs` lo lee
        // para la trampa del beat 4. En MULTI (`TrySpawnRed`) NUNCA se toca:
        // se queda en `false`, Semilla Cero no existe ahí (contrato §3).
        // </summary>
        public static bool ModoSemillaCero;

        private AlkahestSim _sim;
        private bool _spawned;

        // (playtest 40, SEMILLA CERO) Referencias cacheadas para poder
        // spawnear las cuatro estaciones tapiables MÁS TARDE, cuando el otro
        // encargo (Game/SemillaCero.cs) llame a
        // `Sim/SimLevelBuilder.DestaparSala` -- ver `PollDestapesSemillaCero`,
        // sondeado desde `Update()` con el mismo criterio barato que el resto
        // del proyecto (CLAUDE.md, "probes con acumulador"/"polling barato de
        // estado público").
        private Transform _playerSemillaCero;
        private SubstanceKnowledge _knowledgeSemillaCero;
        private OrderSystem _orderSystemSemillaCero;
        private readonly bool[] _salaSpawneadaSemillaCero = new bool[4];

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
            if (!_spawned) { TrySpawn(); return; }
            // (playtest 40, SEMILLA CERO) Cuatro comparaciones de bool por
            // frame, cero allocs -- el mismo presupuesto que cualquier otro
            // polling barato del proyecto. En caótico/multi `ModoSemillaCero`
            // es `false` y esto no hace nada.
            if (ModoSemillaCero) PollDestapesSemillaCero();
        }

        /// <summary>
        /// (playtest 40, SEMILLA CERO) Spawnea la estación de la sala `n` la
        /// PRIMERA vez que `Sim/SimLevelBuilder.SalaDestapada(n)` da `true`
        /// -- lo dispara `Game/SemillaCero.cs` (el otro encargo) llamando a
        /// `SimLevelBuilder.DestaparSala`, que solo toca el CellGrid; este
        /// método es quien de verdad hace aparecer la máquina (sprites, foco
        /// de interacción) que hasta ese momento ni siquiera existía como
        /// GameObject -- ver el docblock de `TapiarSalasSemillaCero`.
        /// </summary>
        private void PollDestapesSemillaCero()
        {
            if (!_salaSpawneadaSemillaCero[SimLevelBuilder.SalaPrensa] && SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaPrensa))
            {
                SpawnPrensa(_playerSemillaCero);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaPrensa] = true;
            }
            if (!_salaSpawneadaSemillaCero[SimLevelBuilder.SalaColumna] && SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaColumna))
            {
                SpawnColumnaEnsayo(_playerSemillaCero, _knowledgeSemillaCero);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaColumna] = true;
            }
            if (!_salaSpawneadaSemillaCero[SimLevelBuilder.SalaChispa] && SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaChispa))
            {
                SpawnBancoChispa(_playerSemillaCero, _knowledgeSemillaCero);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaChispa] = true;
            }
            if (!_salaSpawneadaSemillaCero[SimLevelBuilder.SalaEnsayo] && SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaEnsayo))
            {
                SpawnEnsayoMaestro(_orderSystemSemillaCero, _playerSemillaCero);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaEnsayo] = true;
            }
        }

        private void TrySpawn()
        {
            // (playtest 28, EL TALLER COMPARTIDO) LA BIFURCACIÓN, en la
            // PRIMERA línea y con un solo `if`: si la escena tiene un SimSync
            // (= es la escena MULTI, ver Net/SimSync.cs) el reparto de lo que
            // se instancia cambia por completo y vive en TrySpawnRed, más
            // abajo. La escena Lab CLÁSICA no tiene SimSync: `EnEscena` es
            // false, este `if` no entra nunca, y de aquí para abajo NO CAMBIÓ
            // NI UNA LÍNEA.
            if (SimSync.EnEscena) { TrySpawnRed(); return; }

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
            // (playtest 21) SpawnDispensers ENTERO se saltaba aquí -- los cinco
            // grifos del banco clásico (agua/arena/aceite/nutriente/azoth) están
            // enterrados con el taller. `grifoAzoth` alimentaba MasterSupplies.Init,
            // que tampoco se instancia (ver el bloque de "muestras del Maestro").
            //   var grifoAzoth = SpawnDispensers(apprentice.transform, orderSystem);
            //
            // (playtest 22) PERO LOS DOS BÁSICOS SÍ VUELVEN, montados en el muro
            // izquierdo de la cámara. Cesar, tras jugar: *"quizás necesite los
            // caños más básicos al inicio; hay un charquito de agua pero si por
            // lo que sea lo pierdo ya no hay mucho más que hacer, y así evitamos
            // dejar cositas en el suelo que se pueden perder"*. Su razón es la
            // correcta: en un juego que te pide experimentar, un recurso
            // perdible para siempre es una trampa -- una fuente infinita es lo
            // que te permite equivocarte. Solo AGUA y LIMO (el material
            // primigenio del que descienden las 40 variantes de esta ronda), a
            // coste 0 de Favor y sin sellar; arena, aceite y azoth se quedan
            // enterrados y aparecen al excavar, que es su recompensa.
            // Coordenadas del plano (regla de este archivo: NINGUNA vive aquí),
            // ver SimLevelBuilder.CanoMontajeX/CanoAguaY/CanoNutrienteY.
            //
            // (LO QUE PERSISTE, playtest 25, contrato §5.4 / regla 47 de
            // CLAUDE.md): el caño que emitía Nutrient pasa a emitir
            // MaterialId.Limo -- MISMO SpawnCanoBasico, misma boca
            // (CanoNutrienteY sigue siendo la constante correcta: es la del
            // CUARTO ÍNTIMO, no la del banco de grifos del taller clásico que
            // advierte la regla 47), solo cambia el material que emite. La
            // criatura queda aparcada esta ronda (ver más abajo), así que el
            // nutriente ya no tiene consumidor -- el limo sí: es el primer
            // gesto del juego entero (diseño §9, "hervir limo en el crisol: el
            // agua se va, sus arenas quedan").
            // (playtest 27) Cada caño monta en SU columna: el de agua en la
            // pared del cuarto, el de limo en el machón de piedra de la
            // estación de fuentes. Es lo que separa los dos chorros SIN
            // deformar el sprite del caño (mandato 7, Cesar: "no estires el
            // tamaño del caño de Limo").
            var canoAgua = SpawnCanoBasico(apprentice.transform, "Water", MaterialId.Water,
                SimLevelBuilder.CanoAguaX, SimLevelBuilder.CanoAguaY, orderSystem);
            // (playtest 26, fix integración) alcanceCano=12: los dos caños
            // comparten pared y con el voladizo default (5) sus chorros caían
            // por la MISMA columna -- el limo desembocaba en la pila del agua.
            // Con 12, la boquilla del limo queda sobre SU pila
            // (SimLevelBuilder.PilaLimoX0..+5) y cada chorro aterriza a la
            // vista en su recipiente: la primera imagen del juego ya enseña
            // "cada boca, su pila".
            // (playtest 27) alcanceCano vuelve al default 5: Cesar sobre el
            // caño estirado del 26: "no estires el tamaño del caño de Limo,
            // vuélvelo a su dimensión normal". La separación de los dos
            // chorros pasa a resolverse con GEOMETRÍA (la estación de fuentes
            // de la reconstrucción 6x), no deformando el aparato.
            var canoLimo = SpawnCanoBasico(apprentice.transform, "Limo", MaterialId.Limo,
                SimLevelBuilder.CanoLimoX, SimLevelBuilder.CanoLimoY, orderSystem);
            _dispensers = new[] { canoAgua, canoLimo };
            SpawnDeliveryChute(orderSystem); // la Tolva SIGUE EXISTIENDO, sellada tras la roca (ver BuildDeliveryNiche).
            // (playtest 30, "LA ALQUIMIA VISIBLE", tarea 5) EL ESTANTE DE
            // REDOMAS VUELVE: comentado desde el playtest 21 (el pivot al
            // cuarto íntimo lo dejó sin sitio propio). Sitio NUEVO --
            // `SimLevelBuilder.EstanteX0/X1/BaseY`, NO los viejos
            // RackX0/RackX1/RackTopY (esos son del taller CLÁSICO y hoy caen
            // dentro del cuarto íntimo, ver el docblock junto a esas
            // constantes en Sim/SimLevelBuilder.cs). Game/StorageRack.cs no
            // necesita ningún cambio: deriva sus medidas del ancho real que
            // recibe (regla 39 de CLAUDE.md), y no talla mampostería.
            SpawnStorageRack(apprentice.transform, flask, knowledge);

            SpawnNamingUi(flask, knowledge);
            SpawnJournalHud(knowledge);
            SpawnOrdersHud(orderSystem);

            // (integración pt40, SEMILLA CERO) EL DIRECTOR DEL ARCO: solo en
            // modo Semilla Cero (su Init además se auto-veta con
            // SimSync.EnEscena: jamás en multi). Va DESPUÉS de
            // knowledge/orderSystem (los escucha) y ANTES de que el jugador
            // toque nada -- las cuatro máquinas tapiadas las spawnea
            // PollDestapesSemillaCero cuando este director destape su sala.
            if (ModoSemillaCero)
            {
                var semillaCero = new GameObject("SemillaCero").AddComponent<SemillaCero>();
                semillaCero.Init(_sim, knowledge, orderSystem);
            }

            // LAS TRES MÁQUINAS DE LO QUE PERSISTE (contrato §5.4): en las
            // constantes de SimLevelBuilder §4.5 (CrisolX/PrensaX/BancoChispaX,
            // suelo CuartoY0+2 -- las define el encargo A EN PARALELO, se
            // referencian aquí por su nombre EXACTO del contrato). Después del
            // conocimiento (el Crisol y el Banco lo necesitan para las
            // condiciones/observaciones legibles del diario) y antes del ciclo
            // de jornadas.
            // (playtest 40, SEMILLA CERO) Referencias cacheadas para el
            // spawn TARDÍO de las cuatro estaciones tapiables -- ver
            // `PollDestapesSemillaCero`. Se guardan aquí (ya existen las
            // tres) da igual el modo: en caótico/multi nunca se leen.
            _playerSemillaCero = apprentice.transform;
            _knowledgeSemillaCero = knowledge;
            _orderSystemSemillaCero = orderSystem;

            SpawnCrisol(apprentice.transform, knowledge); // NUNCA tapiado (contrato §3: "el jugador nace junto a las fuentes y el crisol").
            // (playtest 40, SEMILLA CERO) LAS CUATRO SALAS POR PREGUNTA: en
            // modo Semilla Cero, con el mundo recién tapiado
            // (`Sim/SimLevelBuilder.TapiarSalasSemillaCero`, llamado desde
            // `Game/AlkahestSim.cs` antes de que este método corra),
            // NINGUNA sala empieza destapada -- así que las cuatro se saltan
            // aquí y `PollDestapesSemillaCero` las spawnea la primera vez que
            // `Game/SemillaCero.cs` (el otro encargo) llame a
            // `SimLevelBuilder.DestaparSala`. Decisión de M (ver el docblock
            // de `TapiarSalasSemillaCero`): así nunca existe una
            // `MachineFocus.Registrar` para una máquina detrás del muro, ni
            // una chapa "E — usar" que se vea a través del tapiado.
            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaPrensa))
            {
                SpawnPrensa(apprentice.transform);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaPrensa] = true;
            }
            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaChispa))
            {
                SpawnBancoChispa(apprentice.transform, knowledge);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaChispa] = true;
            }
            // (playtest 27, mandato 5) La COLUMNA por fin tiene clase propia
            // (Game/ColumnaEnsayo.cs): su mampostería la sigue tallando el
            // plano, pero su vidrio, sus zunchos y su VERBO ("observar")
            // necesitaban un MonoBehaviour que los dibujara.
            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaColumna))
            {
                SpawnColumnaEnsayo(apprentice.transform, knowledge);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaColumna] = true;
            }
            // (playtest 30, "LA ALQUIMIA VISIBLE", tarea 3) EL ALAMBIQUE:
            // primer instrumento del taller que el jugador FABRICA con
            // materiales -- ver Game/Alambique.cs. Junto al resto de la línea
            // del taller, después de que exista `_sim` con su Grid ya
            // tallado (el plinto lo talló Sim/SimLevelBuilder.BuildCuartoIntimo
            // antes de que este método corra).
            SpawnAlambique(apprentice.transform);

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
            // (ENCARGO F, Playtest 39) LA CAPA DE PARTÍCULAS DESPRENDIDAS
            // (Game/ParticulasFx.cs, TODO vive ahí): decorativa, NO-SIM,
            // client-local -- se crea aquí igual que el resto de capas
            // puramente visuales de este método (WorkshopBackdrop, un par de
            // líneas arriba), sin depender del aprendiz ni de ninguna otra
            // máquina.
            new GameObject("ParticulasFx").AddComponent<ParticulasFx>().Init(_sim);
            var hints = new GameObject("HintSystem").AddComponent<HintSystem>();
            MasterSupplies supplies = null;
            //   var supplies = new GameObject("MasterSupplies").AddComponent<MasterSupplies>();
            //   supplies.Init(_sim, grifoAzoth);

            // LAS DOS CRIATURAS -- CRIATURA APARCADA — LO QUE PERSISTE
            // (contrato CONTRATO_PERSISTE.md §1/§5.4, playtest 25, mismo
            // estilo que la regla 15 de CLAUDE.md: documentar en el código lo
            // que se retira, no solo dejar de llamarlo). Sus archivos
            // (Game/Criatura.cs/Capullo.cs) NO SE TOCAN y quedan APARCADOS
            // INTACTOS (diseño §8.6: "toda la infraestructura Criatura/Vivium
            // queda APARCADA INTACTA... es el escalón 'materia → materia
            // viva' y merece su propia fase") -- esta ronda es sobre qué
            // PERSISTE ante calor/frío/presión/agua/chispa, no sobre criar.
            // Se comentan sus llamadas de siembra (firma VERBATIM conservada
            // abajo, sin borrar, para cuando la criatura vuelva):
            //   var criaturaGo = new GameObject("Criatura");
            //   var criatura = criaturaGo.AddComponent<Criatura>();
            //   criatura.Init(_sim, apprentice.transform, SimLevelBuilder.CunaCriaturaX, SimLevelBuilder.CunaCriaturaY);
            //
            //   var capulloGo = new GameObject("Capullo");
            //   var capullo = capulloGo.AddComponent<Capullo>();
            //   capullo.Init(_sim, apprentice.transform, SimLevelBuilder.CapulloX, SimLevelBuilder.CapulloY);

            // EL ENSAYO DEL MAESTRO (contrato §6.2/§6.5, firma congelada
            // `EnsayoMaestro.Init(AlkahestSim sim, OrderSystem orders,
            // Transform jugador)` -- propiedad del encargo C de esta ronda,
            // implementado en paralelo). Junto a la boca del pasillo
            // (`SimLevelBuilder.EnsayoPlintoX`, la lee la propia clase). Antes
            // del ciclo de jornadas, mismo criterio que el resto de este
            // método.
            // (playtest 40, SEMILLA CERO) El atrio del Ensayo es la cuarta
            // sala tapiable -- mismo criterio que Prensa/Chispa/Columna más
            // arriba.
            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaEnsayo))
            {
                SpawnEnsayoMaestro(orderSystem, apprentice.transform);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaEnsayo] = true;
            }

            SpawnDayCycle(orderSystem, knowledge, supplies, hints);

            // (M5 audio) EL TALLER SUENA: se instancia AL FINAL, cuando ya
            // existen todas las dependencias que necesita (frasco, grifos,
            // conocimiento, encargos, jugador). Ver Audio/DirectorDeAudio.cs.
            SpawnDirectorDeAudio(orderSystem, knowledge, flask, apprentice.transform);

            _spawned = true;
            Debug.Log("[ChaosAlchemy] Capa de interacción inicializada (cuarto íntimo, playtest 25 -- LO QUE PERSISTE: crisol/prensa/banco de chispa, criatura aparcada).");
        }

        /// <summary>
        /// =================================================================
        /// EL TALLER COMPARTIDO (playtest 28): la misma lista plana, repartida
        /// entre el anfitrión y los invitados.
        /// =================================================================
        /// Tres diferencias con <see cref="TrySpawn"/>, todas del contrato:
        ///
        ///  1) EL APRENDIZ NO SE INSTANCIA AQUÍ. Llega por NGO como
        ///     PlayerObject (prefab generado por
        ///     `Editor/AlkahestNetSceneBuilder.cs`), y con él su frasco, su
        ///     conocimiento y su HUD, que cablea `Net/AprendizNet.cs`. Este
        ///     método ESPERA a que exista el avatar local (`AprendizNet.Local`)
        ///     porque casi todo lo de abajo necesita su Transform (el foco de
        ///     máquinas, el audio) o su SubstanceKnowledge (los encargos).
        ///
        ///  2) LAS MÁQUINAS Y LOS ENCARGOS SOLO EXISTEN EN EL ANFITRIÓN. No es
        ///     una limitación técnica que haya que levantar más adelante: es la
        ///     división de trabajo del POC. La sim vive en el host, así que
        ///     todo lo que LEE Y ESCRIBE la sim cada tick (crisol, prensa,
        ///     banco de chispa, caños, tolva, ensayo) vive donde vive la sim.
        ///     Los invitados acarrean materia; el anfitrión hornea.
        ///
        ///  3) NO HAY CICLO DE JORNADAS. La escena MULTI no tiene Título ni
        ///     reloj, así que no se instancia `DayCycle`; en su lugar se hace a
        ///     mano lo único que su arranque silencioso hacía y que aquí sigue
        ///     haciendo falta: soltar los cerrojos de input/HUD
        ///     (<see cref="DayCycle.ForzarDesbloqueoSesion"/>), reiniciar las
        ///     pistas de la jornada 1 y generar los encargos.
        /// </summary>
        private void TrySpawnRed()
        {
            if (_spawned || _sim == null || _sim.Universe == null || _sim.Grid == null) return;

            // En el invitado, el mundo no existe hasta que llega el snapshot
            // con la seed del anfitrión; en los dos extremos, el avatar puede
            // tardar un frame más que el mundo. Se reintenta en Update, igual
            // que hace TrySpawn con Universe/Grid.
            var avatarLocal = AprendizNet.Local;
            if (avatarLocal == null || !avatarLocal.Cableado) return;

            bool anfitrion = SimSync.EsServidor;

            MachineFocus.Limpiar();
            DayCycle.ForzarDesbloqueoSesion();

            // El fondo del cuarto es puramente visual y se genera por código:
            // lo tienen los dos lados, o el invitado vería el taller flotando
            // sobre el vacío.
            new GameObject("WorkshopBackdrop").AddComponent<WorkshopBackdrop>();
            // (ENCARGO F, Playtest 39) LA CAPA DE PARTÍCULAS, TAMBIÉN EN LOS
            // DOS LADOS: es client-local por diseño (Game/ParticulasFx.cs) --
            // cada cliente (anfitrión O invitado) genera sus propias motas de
            // lo que VE en su propia grilla (real o espejo, da igual), así
            // que se crea ANTES de la bifurcación anfitrión/invitado de más
            // abajo, junto al resto de lo puramente visual.
            new GameObject("ParticulasFx").AddComponent<ParticulasFx>().Init(_sim);

            var apprentice = avatarLocal.GetComponent<ApprenticeController>();
            var flask = avatarLocal.GetComponent<Flask>();
            var knowledge = avatarLocal.GetComponent<SubstanceKnowledge>();

            if (!anfitrion)
            {
                // (fix Cesar playtest 36, EL CAMINO DEL INVITADO) ANTES el
                // invitado no recibía NADA de aquí para abajo -- "falta de
                // menús" del reporte de Cesar: sin diario, sin bautizo, sin
                // pistas, sin panel de encargos. Los cuatro son HUD -- ninguno
                // escribe en la sim autoritativa, así que los cuatro pueden
                // vivir en el invitado sin mover un ápice de autoridad:
                //   · HintSystem: texto local puro (Game/HintSystem.cs no
                //     depende de nada de red), mismo arranque que el
                //     anfitrión (jornada 1, no hay jornada 2/3 en esta
                //     escena).
                //   · NamingUi/JournalHud: hablan con el SubstanceKnowledge
                //     de ESTE avatar, que ya recibe lo mínimo del anfitrión
                //     vía Net/SaberSync.cs (ver el fix de esa clase y de
                //     SubstanceKnowledge.Update) -- el rito de bautizo es
                //     local, con la autoridad resuelta por
                //     Game/NamingUi.cs::Consagrar (SaberSync.PedirBautizo).
                //   · OrdersHud: sin OrderSystem local (null a propósito),
                //     cae en su rama read-only replicada (ver el docblock de
                //     Game/OrdersHud.cs, "ESTADO REPLICADO").
                var hintsInvitado = new GameObject("HintSystem").AddComponent<HintSystem>();
                hintsInvitado.ReiniciarParaJornada(1);
                SpawnNamingUi(flask, knowledge);
                SpawnJournalHud(knowledge);
                SpawnOrdersHud(null);

                _spawned = true;
                Debug.Log("[ChaosAlchemy][Red] Invitado listo: espejo + avatar + menús (diario/bautizo/pistas/encargos replicados). Las máquinas y la sim las lleva el anfitrión.");
                return;
            }

            var orderSystem = SpawnOrderSystem(knowledge);

            var canoAgua = SpawnCanoBasico(apprentice.transform, "Water", MaterialId.Water,
                SimLevelBuilder.CanoAguaX, SimLevelBuilder.CanoAguaY, orderSystem);
            var canoLimo = SpawnCanoBasico(apprentice.transform, "Limo", MaterialId.Limo,
                SimLevelBuilder.CanoLimoX, SimLevelBuilder.CanoLimoY, orderSystem);
            _dispensers = new[] { canoAgua, canoLimo };

            SpawnDeliveryChute(orderSystem);

            // (fix Cesar playtest 34, causa raíz confirmada del reporte "EN
            // MULTI no aparecen las redomas ni el alambique") EL PLAYTEST 30
            // wireó SpawnStorageRack/SpawnAlambique SOLO dentro de TrySpawn
            // (modo un jugador) -- TrySpawnRed nunca los llamaba, así que ni
            // el anfitrión del multi los veía. Se spawnean aquí, en el
            // ANFITRIÓN (la sim -- y con ella toda mampostería real -- solo
            // vive donde vive el host, ver el docblock de esta clase, punto
            // 2), con el mismo patrón que el resto de esta lista: los
            // invitados los ven vía réplica (Net/MaquinaSync.cs, tipos Rack/
            // Alambique añadidos en esta misma ronda).
            SpawnStorageRack(apprentice.transform, flask, knowledge);
            SpawnAlambique(apprentice.transform);

            SpawnNamingUi(flask, knowledge);
            SpawnJournalHud(knowledge);
            SpawnOrdersHud(orderSystem);

            SpawnCrisol(apprentice.transform, knowledge);
            SpawnPrensa(apprentice.transform);
            SpawnBancoChispa(apprentice.transform, knowledge);
            SpawnColumnaEnsayo(apprentice.transform, knowledge);

            var hints = new GameObject("HintSystem").AddComponent<HintSystem>();
            SpawnEnsayoMaestro(orderSystem, apprentice.transform);

            // Lo que hacía DayCycle.EnterCuartoIntimoSilencioso y aquí no
            // tiene quién lo haga (ver punto 3 del docblock). Sin
            // MasterSupplies, igual que en la escena de un jugador.
            hints.ReiniciarParaJornada(1);
            orderSystem.GenerateOrdersPersiste();

            SpawnDirectorDeAudio(orderSystem, knowledge, flask, apprentice.transform);

            _spawned = true;
            Debug.Log("[ChaosAlchemy][Red] Anfitrión listo: sim, máquinas, encargos y avatar propio.");
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

        /// <summary>
        /// (playtest 22) Un caño básico del cuarto íntimo, montado en el muro
        /// izquierdo. NO reutiliza <see cref="SpawnOneDispenser"/> a propósito:
        /// aquel deriva su posición de `TapMountX`/`TapFirstY`/`TapStepY`, que
        /// son las coordenadas del BANCO DE GRIFOS del taller clásico (hoy
        /// enterrado, a 30 celdas de aquí). Reutilizarlo habría plantado los dos
        /// caños dentro de la roca, fuera de la cámara -- el mismo tipo de fallo
        /// que la regla 39 del proyecto (calibrar contra la medida real, nunca
        /// contra la constante que "suena" bien).
        ///
        /// Coste 0 de Favor y sin sellar: son la base de la que parte todo, no
        /// una recompensa. El Favor ni siquiera se gana todavía en este modo
        /// (los encargos no existen hasta que se excave hasta la Tolva).
        ///
        /// (fix Cesar playtest 34, "GRIFOS Y PILAS SE MUDAN POR SEPARADO")
        /// YA NO recibe `pilaX0` ni pasa ningún dato de pila a
        /// <see cref="Dispenser.Init"/>: el grifo dejó de dibujar el marco de
        /// su pila (ver el docblock de <c>Dispenser.BuildPilaEnmarcada</c>),
        /// esa pieza la crea por su cuenta <c>Game/Pila.cs::SpawnTodas</c>
        /// (llamado desde Game/Mudanza.cs, mismo patrón que Balda/Anclaje) a
        /// partir de <see cref="SimLevelBuilder.PilaPlanes"/> -- este método
        /// ya no necesita saber que las pilas existen.
        /// </summary>
        private Dispenser SpawnCanoBasico(Transform player, string label, byte matId, int columnaX, int filaY,
            OrderSystem orderSystem, int alcanceCano = 5)
        {
            var go = new GameObject($"CanoBasico_{label}");
            var dispenser = go.AddComponent<Dispenser>();
            // (playtest 26, LA RACIÓN) racionCeldas=45: ~una pila colmada por
            // apertura y el grifo se cierra solo ("· servido — E para más").
            // Verificado con capturas en esta misma ronda: sin ración, 20s de
            // grifo abierto sobre el suelo corrido de la línea inundaban el
            // laboratorio entero. Solo afecta a los DOS caños del laboratorio
            // (este método); los grifos del taller clásico siguen infinitos.
            dispenser.Init(_sim, player,
                columnaX, filaY,
                matId, orderSystem, 0, false, alcanceCano, racionCeldas: 45);
            return dispenser;
        }

        /// <summary>
        /// LO QUE PERSISTE (contrato §5.4): el Crisol, en
        /// <see cref="SimLevelBuilder.CrisolX"/> -- la constante la define el
        /// encargo A en paralelo, se usa por su nombre EXACTO del contrato.
        /// `knowledge` alimenta las condiciones legibles del diario
        /// ("combustible:&lt;nombre&gt;", ver Crisol.CondicionCalor).
        /// </summary>
        private void SpawnCrisol(Transform player, SubstanceKnowledge knowledge)
        {
            var go = new GameObject("Crisol");
            var crisol = go.AddComponent<Crisol>();
            crisol.Init(_sim, player, knowledge, SimLevelBuilder.CrisolX);
        }

        /// <summary>LO QUE PERSISTE (contrato §5.4): la Prensa, en <see cref="SimLevelBuilder.PrensaX"/>.</summary>
        private void SpawnPrensa(Transform player)
        {
            var go = new GameObject("Prensa");
            var prensa = go.AddComponent<Prensa>();
            prensa.Init(_sim, player, SimLevelBuilder.PrensaX);
        }

        /// <summary>
        /// LO QUE PERSISTE (contrato §5.4): el Banco de Chispa, en
        /// <see cref="SimLevelBuilder.BancoChispaX"/>. `knowledge` alimenta el
        /// hook de observaciones (contrato §5.3/§6.4,
        /// SubstanceKnowledge.RegistrarObservacionPropiedad).
        /// </summary>
        private void SpawnBancoChispa(Transform player, SubstanceKnowledge knowledge)
        {
            var go = new GameObject("BancoChispa");
            var banco = go.AddComponent<BancoChispa>();
            banco.Init(_sim, player, knowledge, SimLevelBuilder.BancoChispaX);
        }

        /// <summary>(playtest 27, mandato 5) La Columna de Ensayo. No recibe ancla: su sitio son las constantes `SimLevelBuilder.ColumnaX0/Ancho/Muro/Alto`, igual que el Ensayo del Maestro lee `EnsayoPlintoX` por su cuenta.</summary>
        private void SpawnColumnaEnsayo(Transform player, SubstanceKnowledge knowledge)
        {
            var go = new GameObject("ColumnaEnsayo");
            var columna = go.AddComponent<ColumnaEnsayo>();
            columna.Init(_sim, player, knowledge);
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

        /// <summary>
        /// (playtest 30, "LA ALQUIMIA VISIBLE", tarea 5) REACTIVADO tras el
        /// pivot al cuarto íntimo (playtest 21). Sitio nuevo:
        /// <see cref="SimLevelBuilder.EstanteX0"/>/<see cref="SimLevelBuilder.EstanteX1"/>/
        /// <see cref="SimLevelBuilder.EstanteBaseY"/> -- ver el docblock junto
        /// a esas constantes para por qué no son las viejas
        /// RackX0/RackX1/RackTopY. Game/StorageRack.cs::Init tiene la MISMA
        /// firma de siempre (`(sim, frasco, saber, jugador, cellX0, cellX1,
        /// cellYBase)`), verificada contra el archivo actual: no hace falta
        /// tocarlo, solo darle coordenadas nuevas.
        /// </summary>
        private void SpawnStorageRack(Transform player, Flask flask, SubstanceKnowledge knowledge)
        {
            var go = new GameObject("StorageRack");
            var rack = go.AddComponent<StorageRack>();
            rack.Init(_sim, flask, knowledge, player,
                SimLevelBuilder.EstanteX0, SimLevelBuilder.EstanteX1, SimLevelBuilder.EstanteBaseY);
        }

        /// <summary>
        /// (playtest 30, "LA ALQUIMIA VISIBLE", tarea 3) EL ALAMBIQUE: nace
        /// como obra pendiente (ver el docblock de Game/Alambique.cs) sobre
        /// el plinto que ya talló Sim/SimLevelBuilder.cs en el génesis del
        /// mundo.
        ///
        /// (fix Cesar playtest 34) YA NO ES "Solo en <see cref="TrySpawn"/>
        /// (modo un jugador)" -- ese límite dejó fuera al multi durante tres
        /// rondas (el mismo hueco que las redomas, ver <see cref="TrySpawnRed"/>,
        /// que ahora también llama a este método en el ANFITRIÓN). No hizo
        /// falta tocar Game/Alambique.cs: `Init` ya tenía la firma correcta
        /// para llamarse desde cualquier lado, el gap vivía enteramente en
        /// este archivo (qué método llamaba a quién), no en el aparato.
        /// </summary>
        private void SpawnAlambique(Transform player)
        {
            var go = new GameObject("Alambique");
            var alambique = go.AddComponent<Alambique>();
            alambique.Init(_sim, player, SimLevelBuilder.AlambiqueX);
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

        /// <summary>
        /// LO QUE PERSISTE (contrato §6.2/§6.5): el Ensayo del Maestro, con la
        /// firma congelada `EnsayoMaestro.Init(AlkahestSim sim, OrderSystem
        /// orders, Transform jugador)` -- "nada más necesita Init nuevo", la
        /// propia clase lee su posición de `SimLevelBuilder.EnsayoPlintoX`
        /// (junto a la boca del pasillo), igual que DeliveryChute lee la suya.
        /// Propiedad del encargo C de esta ronda, implementado en paralelo.
        /// </summary>
        private void SpawnEnsayoMaestro(OrderSystem orderSystem, Transform player)
        {
            var go = new GameObject("EnsayoMaestro");
            var ensayo = go.AddComponent<EnsayoMaestro>();
            ensayo.Init(_sim, orderSystem, player);
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
