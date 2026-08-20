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
        // que decide gran parte de esta ronda: la pantalla de entrada lo fija
        // ANTES de cargar la escena (mismo patrón que `AlkahestSim.NextRunSeed`,
        // se lee y se consume aquí, nunca se resetea solo -- quien entra "MODO
        // CAÓTICO" o recarga sin pasar por el título de nuevo debe dejarlo en
        // `false` explícitamente, ver `Game/DayCycle.cs`/la pantalla de
        // título). `Game/AlkahestSim.cs` lo lee para elegir la seed y aplicar
        // los overrides de `Universe`; este archivo lo lee para tapiar/
        // destapar; `Game/Crisol.cs` lo lee para la trampa del beat 4.
        //
        // (playtest 50, SEMILLA CERO COMPARTIDA; ENMENDADO playtest 52, CO-OP
        // GUIADO) YA NO es "en MULTI nunca se toca": desde el playtest 50 el
        // lobby (`Net/TallerSesionHud.cs`) puede ponerlo en `true` también en
        // la escena MULTI, replicado al invitado por `Net/SimSync.cs` (que lo
        // resetea a `false` en `OnNetworkDespawn`, anti-fuga). Esta ronda
        // suma que, cuando es `true` en multi, `TrySpawnRed` instancia
        // `Game/SemillaCero.cs` EN EL ANFITRIÓN (nunca en el invitado, ver el
        // gate de esa clase) -- el arco guiado deja de ser exclusivo del modo
        // un jugador.
        // </summary>
        public static bool ModoSemillaCero;

        private AlkahestSim _sim;
        private bool _spawned;

        // (playtest 48, CONTRATO_RONDA48.md D4/§2e: "la pausa existe desde
        // el primer frame del lobby") Guarda de una sola vez para que
        // `DayCycle.ForzarDesbloqueoSesion()` se dispare en la PRIMERA
        // pasada de `TrySpawn()` que detecta `SimSync.EnEscena` -- no tras
        // el avatar (ver `TrySpawnRed`, que antes lo llamaba recién después
        // de las dos puertas de `avatarLocal`). El método en sí ya es
        // idempotente (comprueba `FindAnyObjectByType<DayCycle>() == null`
        // antes de crear la instancia), pero este flag evita reevaluar esa
        // búsqueda cada Update mientras el lobby sigue esperando mundo/
        // avatar -- Update() llama a TrySpawn() en cada frame hasta que
        // `_spawned` sea true (ver Update() más abajo).
        private bool _desbloqueoSesionMultiForzado;

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
        // (CONTRATO_TERMICA.md §3b/§3c, ENCARGO I) De 4 a 5: la quinta
        // entrada es `SimLevelBuilder.SalaFria` (la alcoba del ChillStone),
        // que se destapa con el beat del frío de Game/SemillaCero.cs -- el
        // mismo mecanismo de las otras cuatro, sin tocar el patrón.
        private readonly bool[] _salaSpawneadaSemillaCero = new bool[5];

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
            // (playtest 40, SEMILLA CERO; ENMENDADO playtest 52, CO-OP
            // GUIADO; REVERTIDO playtest 53) Comparaciones de bool por frame,
            // cero allocs -- el mismo presupuesto que cualquier otro polling
            // barato del proyecto. En caótico (`ModoSemillaCero` false) esto
            // no hace nada, sea cual sea la escena. `!SimSync.EnEscena` cubre
            // el modo un jugador (siempre "anfitrión" de su propia partida,
            // donde este poll SIEMPRE hizo el spawn tardío real);
            // `SimSync.EsServidor` cubre el anfitrión del multi -- desde el
            // playtest 53 (con `Net/MaquinaSync.cs` admitiendo altas tardías,
            // ver su docblock) esta llamada vuelve a hacer trabajo REAL ahí
            // también: `TrySpawnRed` deja de spawnear las seis estaciones
            // tapiables todas de una (revertida la costura del playtest 52),
            // así que `_salaSpawneadaSemillaCero` llega con lo que de verdad
            // esté destapado al arrancar (normalmente nada) y este poll las
            // va completando sala a sala, igual que en un jugador. El
            // INVITADO nunca entra aquí: no tiene
            // `_playerSemillaCero`/`_knowledgeSemillaCero`/
            // `_orderSystemSemillaCero` propios (esta clase no los cachea en
            // la rama invitado de TrySpawnRed, ver ese método) y sus
            // estaciones las ve por RÉPLICA (Net/MaquinaSync.cs), nunca las
            // crea él mismo -- llamar aquí en su lado intentaría spawnear con
            // referencias null.
            if (ModoSemillaCero && (!SimSync.EnEscena || SimSync.EsServidor)) PollDestapesSemillaCero();
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
                SpawnPrensa(_playerSemillaCero, _knowledgeSemillaCero);
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
            // (CONTRATO_TERMICA.md §3c) LA ALCOBA FRÍA: el beat del frío de
            // Game/SemillaCero.cs (entre PreguntaChispa y PreguntaEnsayo)
            // llama a `SimLevelBuilder.DestaparSala(SalaFria)` cuando el
            // Maestro pregunta "¿Y si lo ENFRÍAS?" -- este poll la spawnea la
            // primera vez que eso ocurre, igual que las otras cuatro salas.
            //
            // (CONTRATO_RONDA50.md §4a, ENCARGO M) EL PAR TÉRMICO LLEGA
            // JUNTO: `SpawnHeatPlates` se mudó aquí desde `TrySpawn` (ver su
            // docblock más abajo) -- las dos máquinas nacen en el MISMO
            // frame, disparadas por el MISMO destape, así que el jugador las
            // descubre a la vez ("una placa que solo calienta y una piedra
            // que solo enfría, lado a lado" -- la lección de temperatura por
            // zonas del contrato). El orden de las dos llamadas no importa
            // (ninguna lee a la otra), se listan calor-antes-que-frío por
            // simetría con el orden de siempre en `TrySpawn`.
            if (!_salaSpawneadaSemillaCero[SimLevelBuilder.SalaFria] && SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaFria))
            {
                SpawnHeatPlates(_playerSemillaCero);
                SpawnChillStone(_playerSemillaCero);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaFria] = true;
            }
        }

        // =====================================================================
        // (playtest 54, ENCARGO "PARIDAD SOLO<->MULTI") INVESTIGACIÓN: ¿POR QUÉ
        // DIFIEREN LAS DOS SEMILLA 0?
        // =====================================================================
        // Cesar, comparando capturas (multi co-op seed 0 vs. un jugador seed
        // 0): "no son iguales -- me parece mejor que estén los soportes
        // movibles y decorados [los del multi] que los del solo player".
        //
        // VEREDICTO: el PLANO y el CÓDIGO DE SPAWN de este archivo YA ESTÁN
        // UNIFICADOS -- no hay ninguna decisión de diseño que separe los dos
        // modos en la geometría fija de las baldas/anclajes/pilas/estante:
        //   1) `Sim/SimLevelBuilder.BuildCuartoIntimo` se llama UNA sola vez
        //      por proceso, desde `AlkahestSim.CrearMundoInterno`, sin
        //      ninguna rama por `SimSync.EnEscena` más allá de los gates de
        //      `ModoSemillaCero` (que valen igual en los dos modos, ver el
        //      grep documentado en el docblock de
        //      `SimLevelBuilder._galeriasOriginales`) -- el plano tallado es
        //      BIT A BIT el mismo en un jugador y en el anfitrión del multi.
        //   2) `TrySpawn()` (un jugador) y `TrySpawnRed()` (anfitrión del
        //      multi, más abajo) llaman a `SpawnStorageRack`/`SpawnCrisol`/
        //      etc. con los MISMOS argumentos, y las dos rutas de creación
        //      del avatar (`SpawnApprentice()` aquí abajo, para un jugador;
        //      `Net/AprendizNet.cs::Cablear`, para el multi) llaman al MISMO
        //      `Mudanza.Init(_sim)`.
        //
        // CAUSA REAL, ENCONTRADA Y NO CORREGIBLE DESDE AQUÍ: `Mudanza.Init`
        // llama a `SpawnBaldasYAnclajesSiCorresponde()` (`Game/Mudanza.cs`),
        // que llama a `Balda.SpawnTodas`/`Anclaje.SpawnDeposito`/
        // `Pila.SpawnTodas` -- las tres guardadas por una bandera ESTÁTICA DE
        // PROCESO sin ningún reset (`Game/Balda.cs:144`, `Game/Anclaje.cs:195`,
        // `Game/Pila.cs:190`). Ninguno de esos tres archivos está en la lista
        // de archivos exclusivos de este encargo (`Sim/SimLevelBuilder.cs`,
        // `Game/DeliveryChute.cs`, `Game/AlkahestGameBootstrap.cs`,
        // `Game/MaquinariaSprites.cs`, `Game/StorageRack.cs`), así que el fix
        // de raíz (resetear las tres banderas, mismo sitio que
        // `MachineFocus.Limpiar()` ya limpia para OTRO registro estático,
        // ver la primera línea de `TrySpawn`/`TrySpawnRed`) queda anotado
        // como DEUDA PRIORITARIA para quien posea esos archivos, no
        // resuelto en esta ronda. Encaja con el reporte: sin recarga de
        // dominio entre escenas (una build de reparto, o una sesión de
        // Editor que navegue entre modos sin detener Play), quien entra
        // SEGUNDO a un modo dentro del MISMO proceso encuentra las banderas
        // ya en `true` y se queda sin baldas/anclajes/pilas -- si Cesar jugó
        // multi antes que un jugador en la misma sesión, es EXACTAMENTE el
        // patrón que reportó.
        //
        // LO QUE SÍ SE HIZO en esta ronda, dentro de mis archivos: reducir a
        // la MITAD el catálogo de galerías de baldas en
        // `Sim/SimLevelBuilder._galeriasOriginales` (17 -> 8 `BaldaPlan`
        // troceados, ver el conteo exacto en su docblock) -- una vez
        // corregida la fuga de arriba, las dos versiones tallarán/spawnearán
        // EXACTAMENTE lo mismo, con la mitad de soportes que antes.
        // =====================================================================
        private void TrySpawn()
        {
            // (playtest 28, EL TALLER COMPARTIDO) LA BIFURCACIÓN, en la
            // PRIMERA línea y con un solo `if`: si la escena tiene un SimSync
            // (= es la escena MULTI, ver Net/SimSync.cs) el reparto de lo que
            // se instancia cambia por completo y vive en TrySpawnRed, más
            // abajo. La escena Lab CLÁSICA no tiene SimSync: `EnEscena` es
            // false, este `if` no entra nunca, y de aquí para abajo NO CAMBIÓ
            // NI UNA LÍNEA.
            if (SimSync.EnEscena)
            {
                // (playtest 48, D4/§2e) EL LOBBY DEBE PODER PAUSARSE DESDE EL
                // PRIMER FRAME: antes, `DayCycle.ForzarDesbloqueoSesion()`
                // vivía dentro de `TrySpawnRed`, DESPUÉS de esperar a
                // `_sim.Universe`/`_sim.Grid` Y al avatar local cableado --
                // así que mientras la sesión estaba en el lobby (o tras
                // CUALQUIER fallo de host/cliente, ver SessionCoordinator),
                // no existía ningún DayCycle en escena: ni Escape ni AJUSTES
                // funcionaban ahí. Se dispara aquí, en la primerísima pasada
                // en la que se detecta la escena MULTI (`SimSync.EnEscena`),
                // sin esperar a nada más -- el propio método es
                // autosuficiente (crea su GameObject de pausa sin `_sim` a
                // propósito, ver su docblock en Game/DayCycle.cs).
                if (!_desbloqueoSesionMultiForzado)
                {
                    _desbloqueoSesionMultiForzado = true;
                    DayCycle.ForzarDesbloqueoSesion();
                }

                TrySpawnRed();
                return;
            }

            if (_spawned || _sim == null || _sim.Universe == null || _sim.Grid == null) return;

            // El árbitro de foco es estático y sobrevive a la recarga de escena
            // que hace DayCycle entre partidas: hay que vaciarlo antes de
            // registrar las máquinas nuevas.
            MachineFocus.Limpiar();
            Balda.ResetGuardaEstatica(); Anclaje.ResetGuardaEstatica(); Pila.ResetGuardaEstatica(); // (integración pt54) la fuga de paridad solo/multi -- ver esos métodos.

            var apprentice = SpawnApprentice();
            var flask = apprentice.GetComponent<Flask>();
            var knowledge = apprentice.GetComponent<SubstanceKnowledge>();

            // (playtest 21, EL PIVOT -- historia) LA MAQUINARIA DEL TALLER
            // CLÁSICO se saltó entera aquí en su día: placas ígneas, piedra
            // gélida, grifos y estante no tenían sitio en el cuarto íntimo
            // recién excavado. Grifos (SpawnCanoBasico) y estante
            // (SpawnStorageRack) ya volvieron en rondas posteriores, con
            // sitio NUEVO propio -- ver más abajo. Placas ígnea y gélida son
            // las DOS ÚLTIMAS piezas de esa lista sin des-bifurcar: vuelven
            // ahora (CONTRATO_TERMICA.md §3b, playtest 44), también con
            // sitio nuevo (ver SpawnHeatPlates/SpawnChillStone) -- la llamada
            // vive más abajo, junto a Crisol/las cuatro salas tapiables
            // (misma zona del método, mismo criterio de "des-bifurcar sin
            // reordenar el resto").

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
            SpawnAlbumReal(knowledge);
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

            // (CONTRATO_TERMICA.md §3b, ENCARGO I) LA PLACA ÍGNEA: vuelve a
            // llamarse tras quedar comentada desde pt21/25 (regla 15
            // cumplida en su docblock). NUNCA tapiada, igual que el Crisol
            // -- Cesar, textual: "podemos iniciar con la placa de calor
            // también" (en Semilla Cero, desde el beat 1).
            //
            // (CONTRATO_RONDA50.md §4a, ENCARGO M, D2 "DOS FUEGOS AL
            // ARRANCAR") ESO CAMBIÓ: Cesar vio el crisol Y la placa como DOS
            // fuegos simultáneos en el minuto 0 sin poder distinguir sus
            // oficios ("¿por qué tengo dos máquinas de fuego?"). En modo
            // Semilla Cero la placa YA NO nace aquí -- nace junto a la piedra
            // gélida cuando `Game/SemillaCero.cs` destapa `SalaFria` (el beat
            // del frío), ver `PollDestapesSemillaCero`/`SpawnHeatPlates` más
            // abajo: el arranque se queda con el CRISOL como único fuego, y
            // el par frío/calor llega completo y junto, como pidió el
            // mandato original ("el PAR frío/calor como aparatos
            // didácticos — no dos fuegos simultáneos en el minuto 0"). En
            // CAÓTICO (`!ModoSemillaCero`) NO CAMBIA NADA: la placa sigue
            // naciendo aquí mismo, en el mismo instante que el resto de esta
            // lista (regla dura del contrato §3e-M: "el caótico no cambia").
            if (!ModoSemillaCero) SpawnHeatPlates(apprentice.transform);
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
                SpawnPrensa(apprentice.transform, knowledge);
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
            // (CONTRATO_TERMICA.md §3b/§3c, ENCARGO I) LA ALCOBA FRÍA
            // (ChillStone): quinta sala tapiable, en la misma familia que la
            // Columna (ambas viven en la zona "de instrumentos" del taller).
            // En Semilla Cero solo aparece cuando Game/SemillaCero.cs destape
            // `SalaFria` (el beat del frío, entre PreguntaChispa y
            // PreguntaEnsayo); en caótico/multi, desde el arranque como el
            // resto.
            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaFria))
            {
                SpawnChillStone(apprentice.transform);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaFria] = true;
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
        ///     reloj, así que no se instancia `DayCycle` por el camino normal;
        ///     en su lugar se hace a mano lo único que su arranque silencioso
        ///     hacía. (playtest 48, D4/§2e) <see cref="DayCycle.ForzarDesbloqueoSesion"/>
        ///     YA NO se llama desde AQUÍ dentro -- se dispara en la
        ///     PRIMERÍSIMA pasada de <see cref="TrySpawn"/> que detecta
        ///     `SimSync.EnEscena` (antes de este método siquiera arrancar,
        ///     ver `_desbloqueoSesionMultiForzado`), para que Escape y
        ///     AJUSTES funcionen en el LOBBY desde el primer frame -- antes
        ///     y después de cualquier fallo de sesión -- y no solo tras el
        ///     avatar. Lo que SÍ sigue haciéndose aquí: reiniciar las pistas
        ///     de la jornada 1 y generar los encargos.
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
            Balda.ResetGuardaEstatica(); Anclaje.ResetGuardaEstatica(); Pila.ResetGuardaEstatica(); // (integración pt54) la fuga de paridad solo/multi -- ver esos métodos.
            // (playtest 48, D4/§2e) DayCycle.ForzarDesbloqueoSesion() YA NO
            // se llama aquí -- se movió a la primerísima pasada de
            // TrySpawn() que detecta SimSync.EnEscena (antes de este método
            // siquiera arrancar), para que el lobby pueda pausarse y abrir
            // AJUSTES desde el primer frame, no solo tras el avatar. Ver el
            // docblock de `_desbloqueoSesionMultiForzado`.

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

            // (CONTRATO_TERMICA.md §3a, ENCARGO I) EL TERMÓMETRO EN LOS DOS
            // LADOS: mismo componente, mismo Init(_sim) -- en el anfitrión
            // `_sim.Stepper` existe y lee grados reales; en el invitado
            // `_sim` es el espejo (`AlkahestSim.ModoEspejo`/`Stepper == null`)
            // y el propio Termometro decide mostrar "—" (contrato: "solo el
            // anfitrión mide, por ahora") en vez de mentir con datos locales
            // que aquí no existen (la temperatura NO se replica, solo mat).
            var termometro = avatarLocal.gameObject.AddComponent<Termometro>();
            termometro.Init(_sim);

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
                SpawnAlbumReal(knowledge); // (ENCARGO A, LA QUÍMICA CON NOMBRE REAL) el invitado también descubre -- vía SaberSync/SubstanceKnowledge.AplicarDescubrimientoRemoto, que dispara AlDescubrir igual que un descubrimiento local.
                SpawnOrdersHud(null);

                // (playtest 43, LA PARIDAD VIVA, ENCARGO A) EL INVITADO OYE:
                // antes esta rama terminaba sin director de audio (§0.3 del
                // contrato -- "SpawnDirectorDeAudio solo se llama en las
                // ramas de anfitrión/un jugador"). orderSystem=null (sin
                // encargos locales aquí -- OrdersHud ya cae a su rama
                // read-only replicada dos líneas arriba, y DirectorDeAudio
                // ya tolera orderSystem null: sin stingers de "encargo
                // completado" para el invitado, documentado en el informe
                // de la ronda). _dispensers NUNCA se asigna en esta rama
                // (los Dispenser reales solo existen en el anfitrión) y
                // queda en su valor por defecto null -- SpawnDirectorDeAudio
                // ya lo pasa tal cual a Init, que ya tolera dispensers=null
                // (ConstruirVocesGrifo, ver Audio/DirectorDeAudio.cs) desde
                // antes de esta ronda. El MODO ESPEJO del propio director
                // (Stepper == null, ver su docblock) es lo que sustituye a
                // los Dispenser/eventos de sim que aquí faltan.
                SpawnDirectorDeAudio(null, knowledge, flask, apprentice.transform);

                _spawned = true;
                Debug.Log("[ChaosAlchemy][Red] Invitado listo: espejo + avatar + menús (diario/bautizo/pistas/encargos replicados) + audio (modo espejo). Las máquinas y la sim las lleva el anfitrión.");
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
            SpawnAlbumReal(knowledge);
            SpawnOrdersHud(orderSystem);

            // (playtest 52, CO-OP GUIADO; REVERTIDO playtest 53) Referencias
            // cacheadas -- mismos campos que usa TrySpawn (modo un jugador)
            // para PollDestapesSemillaCero. Desde el playtest 53 estas TRES
            // referencias vuelven a usarse DE VERDAD en la rama anfitrión del
            // multi (ver el bloque "REVERSIÓN DE LA COSTURA" más abajo): ya
            // no es solo `Game/SemillaCero.cs::Init` quien las consumía.
            _playerSemillaCero = apprentice.transform;
            _knowledgeSemillaCero = knowledge;
            _orderSystemSemillaCero = orderSystem;

            SpawnCrisol(apprentice.transform, knowledge); // NUNCA tapiado (contrato §3), en multi igual que en un jugador.

            // =============================================================
            // (playtest 53, LA CURA DE RAÍZ) REVERSIÓN DE LA COSTURA DEL
            // PLAYTEST 52. Aquella ronda documentó, con las SEIS estaciones
            // tapiables naciendo TODAS DE UNA en vez de una por beat, que
            // `Net/MaquinaSync.cs` (archivo ajeno a ese encargo) escaneaba
            // UNA SOLA VEZ y exigía las siete estaciones+grifos completas
            // antes de publicar NADA -- si las seis hubieran nacido tarde,
            // el registro replicado nunca se habría publicado, dejando al
            // invitado sin una sola máquina replicada. El costo aceptado
            // entonces era puramente cosmético ("una chapa 'E — usar'
            // pegándose mucho a un muro sellado", las salas SEGUÍAN
            // tapiadas de piedra) -- pero Cesar probó el arco guiado en
            // co-op y lo reportó igual: los SPRITES de las seis estaciones
            // se dibujaban por encima de la piedra sellada (la Prensa con su
            // "E — prensar", el banco de chispa, el arco del Ensayo, el
            // alambique flotando sobre muros que el jugador ni había
            // destapado) -- rompía la progresión visual del arco.
            //
            // Esta ronda (playtest 53) cierra la deuda que el playtest 52
            // dejó anotada ("extender Net/MaquinaSync.cs con un escaneo
            // INCREMENTAL... desharía esta costura"): `Net/MaquinaSync.cs`
            // ahora admite ALTAS TARDÍAS (ver su docblock,
            // `SondearAltasTardias`/`RegistrarAltaUnica`) -- publica el
            // registro con lo que ya existe y va AÑADIENDO cada estación
            // tapiable al registro (nunca borra ni reordena lo ya publicado)
            // en cuanto `IntentarEscanear`/`SondearAltasTardias` la
            // encuentran, con el mismo sondeo barato de 0.5s que ya tenía.
            // Eso deshace la costura de raíz: esta rama vuelve a ser
            // EXACTAMENTE el patrón de `TrySpawn` (un jugador) --
            // `PollDestapesSemillaCero` spawnea cada estación al destaparse
            // su sala, y el registro replicado la recoge en ≤0.5s después
            // (ver la verificación de timing en el informe de esta ronda).
            // El multi CAÓTICO (`ModoSemillaCero` false) no cambia: las seis
            // siguen naciendo todas de una, como siempre (mismo `if
            // (!ModoSemillaCero || ...)` que usa TrySpawn).
            // =============================================================
            if (!ModoSemillaCero) SpawnHeatPlates(apprentice.transform); // en Semilla Cero nace junto a la piedra gélida (ver PollDestapesSemillaCero, SalaFria) -- mismo criterio que TrySpawn.

            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaPrensa))
            {
                SpawnPrensa(apprentice.transform, knowledge);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaPrensa] = true;
            }
            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaChispa))
            {
                SpawnBancoChispa(apprentice.transform, knowledge);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaChispa] = true;
            }
            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaColumna))
            {
                SpawnColumnaEnsayo(apprentice.transform, knowledge);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaColumna] = true;
            }
            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaFria))
            {
                SpawnChillStone(apprentice.transform);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaFria] = true;
            }

            var hints = new GameObject("HintSystem").AddComponent<HintSystem>();
            if (!ModoSemillaCero || SimLevelBuilder.SalaDestapada(SimLevelBuilder.SalaEnsayo))
            {
                SpawnEnsayoMaestro(orderSystem, apprentice.transform);
                _salaSpawneadaSemillaCero[SimLevelBuilder.SalaEnsayo] = true;
            }

            // Lo que hacía DayCycle.EnterCuartoIntimoSilencioso y aquí no
            // tiene quién lo haga (ver punto 3 del docblock). Sin
            // MasterSupplies, igual que en la escena de un jugador.
            hints.ReiniciarParaJornada(1);

            // (playtest 52, CO-OP GUIADO) EL DIRECTOR, TAMBIÉN EN EL
            // ANFITRIÓN DEL MULTI: mandato literal de Cesar tras el playtest
            // 51 -- "que la Semilla 0 en multiplayer escale igual como tienes
            // pensado para la versión solo player, así puedo probar más
            // cosas con mi amigo". `Game/SemillaCero.cs::Init` se auto-veta
            // en el invitado (ver su docblock) -- aquí solo se instancia
            // dentro de la rama `anfitrion` de este método, así que nunca
            // hay riesgo de crearla dos veces (una por lado). El director
            // encola SUS PROPIOS pedidos guiados (uno a la vez, con nombres
            // reales de la seed 777002) -- por eso `GenerateOrdersSemillaCompartida()`
            // (playtest 51, el "recetario" de 5 pedidos fijos) SE RETIRA de
            // este camino: sigue DEFINIDA en Game/OrderSystem.cs (archivo
            // ajeno, intacto -- regla 15 de CLAUDE.md, documentar lo retirado
            // sin borrarlo) como reserva para un futuro "modo laboratorio
            // libre" sin arco, si alguna vez vuelve a hacer falta. El multi
            // CAÓTICO (ModoSemillaCero false) no cambia: sigue generando "LO
            // QUE PERSISTE" como siempre.
            if (ModoSemillaCero)
            {
                // (playtest 53) YA NO se fuerza `_salaSpawneadaSemillaCero`
                // entera a `true` aquí -- esa era la mitad "silenciar
                // PollDestapesSemillaCero" de la costura del playtest 52,
                // necesaria SOLO porque las seis estaciones ya habían nacido
                // todas arriba. Revertido el spawn a "condicional por sala"
                // (ver el bloque de arriba, idéntico a `TrySpawn`), cada
                // entrada de `_salaSpawneadaSemillaCero[n]` ya quedó en su
                // valor correcto (`true` solo si esa sala YA estaba destapada
                // al arrancar -- normalmente ninguna, recién tapiadas por
                // `TapiarSalasSemillaCero`); `PollDestapesSemillaCero` (mismo
                // guardián `!_salaSpawneadaSemillaCero[n]` de siempre, regla
                // 36 de CLAUDE.md contra el doble `BuildVisual`) vuelve a
                // spawnear cada estación pendiente la primera vez que el
                // director destape su sala, EXACTAMENTE como en un jugador.
                var semillaCero = new GameObject("SemillaCero").AddComponent<SemillaCero>();
                semillaCero.Init(_sim, knowledge, orderSystem);
            }
            else
            {
                orderSystem.GenerateOrdersPersiste();
            }

            SpawnDirectorDeAudio(orderSystem, knowledge, flask, apprentice.transform);

            _spawned = true;
            Debug.Log("[ChaosAlchemy][Red] Anfitrión listo: sim, máquinas, encargos y avatar propio" +
                      (ModoSemillaCero ? " (SEMILLA CERO compartida: arco guiado por Game/SemillaCero.cs)." : "."));
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

            // (CONTRATO_TERMICA.md §3a, ENCARGO I) EL TERMÓMETRO: cuarta
            // herramienta, mismo patrón que Cincel/Mudanza -- mismo
            // GameObject, mismo Init(sim), se alterna con G y mientras está
            // inactivo no dibuja nada (ver Game/Termometro.cs). Las sondas
            // plantadas SÍ persisten fuera del modo (instrumentos, no un
            // overlay), así que el componente sigue vivo siempre.
            var termometro = go.AddComponent<Termometro>();
            termometro.Init(_sim);

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
        /// HISTORIA (playtest 4-17, taller CLÁSICO, hoy enterrado): dos
        /// placas, una bajo cada cuba de CULTIVO (VatA/VatB), CENTRADAS
        /// (FootprintFraction de HeatPlate.Init las recorta al 40% del
        /// interior). Esas coordenadas (VatInteriorX0/X1, VatPlateRow) SIGUEN
        /// existiendo en Sim/SimLevelBuilder.cs -- el taller clásico no se ha
        /// borrado, solo dejó de construirse (ver el docblock de esta clase,
        /// "EL CUARTO ÍNTIMO") -- pero ya NO son el sitio: se llegará a ellas
        /// el día en que el jugador excave hasta el taller enterrado, no hoy.
        ///
        /// SITIO ACTUAL (CONTRATO_TERMICA.md §3b, playtest 44): UNA sola
        /// placa (antes eran dos, una por cuba de un taller que ya no se
        /// construye -- decisión fuera de contrato, documentada: 1 placa
        /// basta para el cuarto íntimo, que solo tiene una zona húmeda), bajo
        /// la Pila de AGUA de la estación de fuentes -- "hervir agua es su
        /// primer uso natural" (contrato, textual). Reutiliza el suelo YA
        /// TALLADO por Game/Pila.cs (<see cref="SimLevelBuilder.PilaPlanes"/>[0]
        /// -- índice 0 es la del agua, ver el orden de ese array) en vez de
        /// tallar nada nuevo: el suelo de la pila (2 filas, `Muro`) más el
        /// suelo general del cuarto (`WallThickness`=3 filas, Y=136..138,
        /// bajo `PilaBaseY`=138) suman 3 filas sólidas justo donde
        /// HeatPlate.RecalcularCentro las necesita (`plateRow - WallThickness
        /// + 1`) -- comprobado a mano, no a ojo (regla 39 de CLAUDE.md): CERO
        /// carving nuevo hace falta en SimLevelBuilder para este sitio.
        ///
        /// EL SITIO EN SEMILLA CERO (CONTRATO_RONDA50.md §4a, ENCARGO M) SE
        /// QUEDA AQUÍ, SOLO CAMBIA EL MOMENTO -- decisión de M, documentada
        /// (el contrato ofrecía dos opciones: "la alcoba fría o junto a
        /// ella"). Medido en el plano real ANTES de decidir (regla 39): la
        /// alcoba fría (<see cref="SimLevelBuilder.AlcobaFriaX0"/>..
        /// <see cref="SimLevelBuilder.AlcobaFriaX1"/>, 8 celdas EXACTAS) la
        /// consume ENTERA la piedra gélida sola -- tanto HeatPlate como
        /// ChillStone fuerzan un ancho MÍNIMO de 8 celdas
        /// (<c>Mathf.Max(8, spanTotal*FootprintFraction)</c> en las dos
        /// clases), así que no cabe una segunda máquina dentro. Y no hay
        /// margen "junto a ella" tampoco: por el oeste la Columna la pisa
        /// hasta su propio borde (huella hasta 280, la alcoba empieza en
        /// 281) y por el este el Banco de Chispa hace lo mismo (huella desde
        /// 289, la alcoba termina en 288) -- CERO celdas de margen a los dos
        /// lados, verificado contra las constantes reales de
        /// Sim/SimLevelBuilder.cs (SOLO LEÍDAS, no tocadas). Ensanchar la
        /// alcoba para que quepan las dos máquinas es un cambio de PLANO
        /// (fuera de los archivos de este encargo) -- queda anotado para el
        /// director/el encargo que posea SimLevelBuilder.cs. Mientras tanto,
        /// el objeto CONSERVA el sitio de la Pila de Agua (probado, sin
        /// riesgo de colisión) y lo que cambia es SOLO cuándo aparece: ver la
        /// llamada nueva en <see cref="PollDestapesSemillaCero"/>, disparada
        /// por el mismo destape de <see cref="SimLevelBuilder.SalaFria"/> que
        /// revela la piedra gélida, así que el jugador las descubre juntas EN
        /// EL TIEMPO aunque no compartan la misma pared -- cumple la parte
        /// que sí está en mis archivos (D2: un solo fuego en el minuto 0) sin
        /// inventar mampostería nueva por mi cuenta. El nombre del objeto NO
        /// cambia (sigue siendo la Pila de Agua de verdad).
        /// </summary>
        private void SpawnHeatPlates(Transform player)
        {
            var pilaAgua = SimLevelBuilder.PilaPlanes[0]; // [0]=agua, [1]=limo -- ver SimLevelBuilder.PilaPlanes.
            var go = new GameObject("HeatPlate_PilaAgua");
            var plate = go.AddComponent<HeatPlate>();
            plate.Init(_sim, player,
                pilaAgua.X0 + pilaAgua.Muro,
                pilaAgua.X0 + pilaAgua.Ancho - 1 - pilaAgua.Muro,
                pilaAgua.Y0 + pilaAgua.Muro - 1);
        }

        /// <summary>
        /// HISTORIA (playtest 4-17, taller CLÁSICO, hoy enterrado): la piedra
        /// gélida vivía bajo la bandeja fría del estante superior de
        /// LABORATORIO (ChillTrayInteriorX0/X1, ChillPlateRow), decisión
        /// tomada tras descartar el SÓTANO explícitamente (sin cubeta con
        /// paredes, Azoth/CrystalSeed se habrían derramado fuera del alcance
        /// térmico). Esas constantes SIGUEN existiendo en
        /// Sim/SimLevelBuilder.cs para cuando se excave hasta ese taller.
        ///
        /// SITIO ACTUAL (CONTRATO_TERMICA.md §3b, playtest 44): "la FRÍA en
        /// la alcoba de la columna, la zona más 'de instrumentos' del
        /// taller" -- ver el bloque de constantes junto a
        /// <see cref="SimLevelBuilder.AlcobaFriaX0"/> para el porqué exacto
        /// del sitio (el hueco "columna|chispa", a la misma cota que sus dos
        /// vecinas) y la decisión documentada de tallar solo dos muretes de
        /// contención en vez de una "U" completa (el hueco disponible son 8
        /// celdas exactas, el mínimo que exige el propio recorte
        /// FootprintFraction de ChillStone.Init).
        /// </summary>
        private void SpawnChillStone(Transform player)
        {
            var go = new GameObject("ChillStone_AlcobaFria");
            var stone = go.AddComponent<ChillStone>();
            stone.Init(_sim, player,
                SimLevelBuilder.AlcobaFriaX0,
                SimLevelBuilder.AlcobaFriaX1,
                SimLevelBuilder.BaseYDeEstacion(SimLevelBuilder.AlcobaFriaX0));
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
        private void SpawnPrensa(Transform player, SubstanceKnowledge knowledge)
        {
            var go = new GameObject("Prensa");
            var prensa = go.AddComponent<Prensa>();
            prensa.Init(_sim, player, SimLevelBuilder.PrensaX);
            // (integración pt47) la resistencia anotada (CONTRATO_FASE_A §1d)
            // -- conectado APARTE de Init para respetar su firma congelada.
            prensa.ConectarConocimiento(knowledge);
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
        /// RackX0/RackX1/RackTopY.
        ///
        /// (playtest 54, ENCARGO "LAS REDOMAS FUERA DE SEMILLA CERO") Cesar,
        /// sobre las capturas de Semilla Cero: "no entiendo qué hacen ahí
        /// los frascos -- más ruido visual, que es lo que menos
        /// necesitamos". El estante de redomas es mobiliario del CAÓTICO
        /// (donde el diario ya lleva rondas de descubrimientos que guardar);
        /// en Semilla Cero, con el arco guiado del director
        /// (Game/SemillaCero.cs) recién empezando, no tiene ningún papel
        /// todavía -- vuelve con el caótico, o el día que el diseño del arco
        /// le dé uno (p. ej. un beat que pida "guarda esto para más
        /// tarde").
        ///
        /// GATE, NO BORRADO: `visible=false` (nuevo parámetro de
        /// Game/StorageRack.cs::Init, archivo tocado SOLO porque este gate
        /// lo exige -- ver su propio docblock) en vez de saltarse el spawn
        /// entero. RAZÓN, no elección: `Net/MaquinaSync.cs::IntentarEscanear`
        /// (archivo AJENO, no tocable en este encargo) exige `estantes.Length
        /// >= 1` antes de publicar el registro replicado ENTERO -- crisol y
        /// grifos incluidos, no solo el estante. Si `TrySpawnRed` (el
        /// anfitrión del multi) dejara de instanciar el componente en
        /// Semilla Cero, el invitado se quedaría SIN NINGUNA máquina
        /// replicada, para siempre, en ese modo -- una regresión mucho peor
        /// que el "ruido visual" que se está corrigiendo. Con `visible=false`
        /// el componente EXISTE (el escaneo lo encuentra) pero
        /// `StorageRack.Init` corta antes de `BuildVisual`/
        /// `Mudanza.RegistrarMovible`: cero sprites, cero redomas, cero
        /// entrada en el registro de movibles, `Update`/`OnGUI` cortan en su
        /// primera línea. MISMO gate en los dos caminos (`TrySpawn` de un
        /// jugador y `TrySpawnRed` del anfitrión, este mismo método sirve a
        /// los dos) -- coherente con el punto 1 de esta ronda (paridad
        /// solo/multi): ambos modos ven EXACTAMENTE lo mismo (nada) en
        /// Semilla Cero.
        /// </summary>
        private void SpawnStorageRack(Transform player, Flask flask, SubstanceKnowledge knowledge)
        {
            var go = new GameObject("StorageRack");
            var rack = go.AddComponent<StorageRack>();
            rack.Init(_sim, flask, knowledge, player,
                SimLevelBuilder.EstanteX0, SimLevelBuilder.EstanteX1, SimLevelBuilder.EstanteBaseY,
                visible: !ModoSemillaCero);
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

        /// <summary>(ENCARGO A, LA QUÍMICA CON NOMBRE REAL) El árbol de figuritas: mismo criterio de inyección que NamingUi/JournalHud, siempre junto a ellos (los tres leen el mismo SubstanceKnowledge del avatar). Spawneado en los TRES sitios de esta clase que ya llaman a SpawnJournalHud(knowledge) -- el invitado también descubre vía SaberSync, así que también necesita su propio álbum local.</summary>
        private void SpawnAlbumReal(SubstanceKnowledge knowledge)
        {
            var go = new GameObject("AlbumReal");
            var album = go.AddComponent<AlbumReal>();
            album.Init(_sim, knowledge);
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
