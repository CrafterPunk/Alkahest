using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Alkahest.Sim;
// (CONTRATO_RONDA50.md §4b, ENCARGO M) SEMILLA CERO COMPARTIDA: este archivo
// lee/escribe `Alkahest.Game.AlkahestGameBootstrap.ModoSemillaCero` (el
// mismo patrón que ya usaba Game/AlkahestSim.cs desde antes de esta ronda,
// ver su propio comentario de `using Alkahest.Game;`) para replicar el modo
// al invitado y para resetearlo al terminar la sesión.
using Alkahest.Game;

namespace Alkahest.Net
{
    /// <summary>
    /// EL TALLER COMPARTIDO (playtest 28, POC multiplayer) — la costura entre
    /// la simulación y la red. Vive en un GameObject propio de la escena MULTI
    /// (con su <see cref="NetworkObject"/>: un NetworkBehaviour NO puede
    /// compartir GameObject con el NetworkManager) y hace CUATRO cosas:
    ///
    ///  1) DECIDE QUIÉN SIMULA. La sim vive SOLO en el anfitrión (backlog
    ///     histórico del proyecto: "sim solo-host + deltas"). El invitado lleva
    ///     un ESPEJO: mismo `CellGrid` y mismo `SimRenderer`, pero su
    ///     `SimStepper` no existe (ver <see cref="AlkahestSim.ModoEspejo"/>).
    ///
    ///  2) DIFUNDE EL MUNDO POR CHUNKS. Cada <see cref="TicksPorDifusion"/>
    ///     ticks de sim (~5 Hz a 30 Hz de simulación) el anfitrión recorre los
    ///     864 chunks (48x18), detecta cuáles cambiaron desde el último envío
    ///     comparando `CellGrid.chunkTouchedTick` contra su propia copia, y
    ///     manda su `mat[]` comprimido RLE por un mensaje nombrado del
    ///     CustomMessagingManager. Es EXACTAMENTE el mismo mecanismo de
    ///     "chunk sucio" que ya usaba `SimRenderer` para no redibujar de más
    ///     (ver su campo `_chunkLastRenderTick`): reutilizarlo en vez de
    ///     inventar otro rastreo garantiza que la red no pueda perderse un
    ///     cambio que el render sí ve.
    ///
    ///  3) MANDA UN SNAPSHOT COMPLETO AL CONECTAR. El mundo recién construido
    ///     es casi todo piedra: el grid entero comprime a unos pocos KB. El
    ///     snapshot lo PIDE el cliente (<see cref="SolicitarSnapshotServerRpc"/>) en
    ///     cuanto tiene su handler de mensaje registrado, en vez de mandárselo
    ///     el servidor al recibir OnClientConnectedCallback: así no hay carrera
    ///     posible entre "el servidor manda" y "el cliente sabe escuchar".
    ///
    ///  4) REENVÍA LAS MUTACIONES DE LOS INVITADOS. Un invitado que aspira o
    ///     vierte no escribe en la sim autoritativa: `AlkahestSim.Paint*`
    ///     encola aquí la petición y este componente la manda al servidor en
    ///     UN SOLO RPC por frame (ver <see cref="VaciarLotePintura"/>). Sin el
    ///     lote habría un RPC por CELDA — el frasco aspira 30 celdas por tick.
    ///
    /// LO QUE **NO** SE SINCRONIZA EN EL POC (decisión del contrato, anotada
    /// aquí para que nadie la lea como un olvido): `temp[]` y `morph[]`. Los
    /// invitados ven los materiales correctos pero SIN incandescencia (el
    /// color de temperatura de `SimRenderer`) y con el campo morfológico
    /// congelado en su semilla de nacimiento (nadie corre `MorphTick` en el
    /// espejo). Es el 90% de la imagen por el 30% del ancho de banda; cuando
    /// haga falta, `temp` cabe en el MISMO formato de mensaje añadiendo un
    /// segundo bloque RLE por chunk.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class SimSync : NetworkBehaviour
    {
        // =================================================================
        // Identidad estática. `EnEscena` la lee AlkahestSim en su Start()
        // para NO construir el mundo por su cuenta (en la escena MULTI el
        // mundo lo crea este componente: el anfitrión con una seed nueva, el
        // invitado con la seed que llegue en el snapshot). Se pone en Awake
        // y Unity garantiza TODOS los Awake antes de CUALQUIER Start, así que
        // el orden está asegurado sin ejecutar orden explícito.
        // La escena Lab CLÁSICA no tiene SimSync: `EnEscena` se queda en
        // false y absolutamente nada de este archivo se activa.
        // =================================================================

        /// <summary>Instancia única en la escena (null en la escena Lab clásica).</summary>
        public static SimSync Instancia { get; private set; }

        /// <summary>¿Hay un SimSync en la escena? (= "esta es la escena MULTI"). No implica que haya sesión.</summary>
        public static bool EnEscena { get; private set; }

        /// <summary>¿Hay una sesión de red viva (NGO arrancado y este objeto spawneado)?</summary>
        public static bool EnSesion => Instancia != null && Instancia.IsSpawned;

        /// <summary>¿Somos el anfitrión de la sesión? (false sin sesión).</summary>
        public static bool EsServidor => Instancia != null && Instancia.IsSpawned && Instancia.IsServer;

        /// <summary>Nombre del mensaje nombrado del CustomMessagingManager. Constante: lo registran los dos extremos.</summary>
        public const string MensajeChunks = "AlkChunks";

        // ---- Cadencia y presupuesto ------------------------------------

        /// <summary>Ticks de simulación entre difusiones DEL RESTO DEL MUNDO (sin avatar cerca). 6 a 30 Hz = ~5 envíos por segundo. SIN CAMBIOS respecto al playtest 36.</summary>
        private const int TicksPorDifusion = 6;

        /// <summary>
        /// (playtest 43, LA PARIDAD VIVA, CONTRATO_PARIDAD.md §3b, "EL FRASCO
        /// FLUIDO") Ticks de simulación entre difusiones DE LA ZONA
        /// PRIORITARIA (chunks sucios a ≤<see cref="RadioPrioridadCeldas"/>
        /// de CUALQUIER avatar, ver <see cref="ChunkCercaDeAlgunAvatar"/> y
        /// <see cref="DifundirChunksSucios"/>). 2 a 30 Hz = ~15 envíos por
        /// segundo -- 3x más rápido que el resto del mundo, que sigue a
        /// <see cref="TicksPorDifusion"/> SIN CAMBIOS: el diagnóstico del
        /// contrato (§0.4) medía hasta ~200ms de retardo VISUAL sobre el RTT
        /// en todo lo que el invitado vierte/aspira porque la única cadencia
        /// que existía era esta (a 5Hz, ~200ms entre difusiones); triplicar
        /// SOLO la zona donde de verdad hay alguien mirando baja ese peor
        /// caso a ~66ms de cadencia (el resto del retardo es RTT puro, que
        /// es justo lo que pide el encargo: "verter y ver el chorro caer
        /// AHÍ, con retardo de red puro (~RTT + 66ms), no de cadencia").
        /// PRESUPUESTO (ver el informe de la ronda para el cálculo completo):
        /// <see cref="MaxBytesCarga"/> y <see cref="MaxChunksPorDifusion"/>
        /// NO CAMBIAN -- los chunks prioritarios son pocos por construcción
        /// (un puñado de chunks sucios cerca de cada avatar, no el mundo
        /// entero), así que triplicar SOLO su cadencia no dispara el pico
        /// por mensaje, solo la frecuencia con la que ese puñado se manda.
        /// </summary>
        private const int TicksPorDifusionPrioridad = 2;

        /// <summary>
        /// Techo de chunks por difusión. Con agua corriendo por medio taller
        /// pueden ensuciarse cientos de chunks a la vez (y `WakeChunk`
        /// despierta el vecindario 3x3, así que la cuenta se infla x9): sin
        /// techo, una sola difusión podría pasar de 100 KB. Lo que no entra
        /// se manda en la siguiente pasada — el barrido arranca donde lo dejó
        /// (<see cref="_cursorBarrido"/>) para que ningún chunk se quede
        /// esperando indefinidamente detrás de los mismos vecinos ruidosos.
        /// </summary>
        private const int MaxChunksPorDifusion = 96;

        /// <summary>Celdas de un chunk (16x16 = 256): tope de parejas RLE de un chunk.</summary>
        private const int CeldasPorChunk = CellGrid.CHUNK * CellGrid.CHUNK;

        /// <summary>Cabecera por chunk dentro de la carga: ushort índice + ushort nº de parejas.</summary>
        private const int BytesCabeceraChunk = 4;

        /// <summary>Peor caso de un chunk: cabecera + 256 parejas (valor,cuenta) sin ninguna repetición.</summary>
        private const int MaxBytesPorChunk = BytesCabeceraChunk + CeldasPorChunk * 2;

        /// <summary>
        /// Tope de carga útil por mensaje. 16 KB entra de sobra en el pipeline
        /// fragmentado y confiable del transporte, y deja sitio para 31 chunks
        /// del peor caso o para cientos de chunks de piedra maciza (que
        /// comprimen a 6 bytes cada uno). El snapshot completo se trocea en
        /// tantos mensajes como haga falta.
        /// </summary>
        private const int MaxBytesCarga = 16 * 1024;

        /// <summary>Cabecera del mensaje: byte tipo + int seed + ushort nº de chunks.</summary>
        private const int BytesCabeceraMensaje = 1 + 4 + 2;

        private const byte TipoDelta = 0;
        private const byte TipoSnapshot = 1;

        // ---- Lote de pintura (cliente -> servidor) ----------------------

        /// <summary>Bytes por petición de pintura: ushort x, ushort y, byte radio, byte material, byte modo, byte tempRaw.</summary>
        private const int BytesPorPintura = 8;

        /// <summary>Tope de peticiones por lote (por frame). 192*8 = 1536 bytes: por encima de lo que el frasco puede generar en un frame (30 celdas/tick, 2 ticks/frame como mucho).</summary>
        private const int MaxPinturasPorLote = 192;

        /// <summary>Modos de pintura, en el mismo orden que la API de <see cref="AlkahestSim"/>.</summary>
        public const byte ModoPaint = 0;
        public const byte ModoPaintStable = 1;
        public const byte ModoPaintCell = 2;
        public const byte ModoPaintRect = 3;

        // ---- Estado ------------------------------------------------------

        private AlkahestSim _sim;

        /// <summary>Copia del `chunkTouchedTick` del grid en el último envío, por chunk. Un chunk está sucio si difieren (mismo criterio que SimRenderer).</summary>
        private uint[] _ultimoTickEnviado;

        /// <summary>Dónde arrancó el último barrido de chunks sucios (reparto justo bajo el techo de <see cref="MaxChunksPorDifusion"/>).</summary>
        private int _cursorBarrido;

        /// <summary>
        /// (playtest 43) Tick de sim en el que se difundió por última vez
        /// CADA pasada -- ahora dos cadencias independientes (ver
        /// <see cref="TicksPorDifusionPrioridad"/>/<see cref="TicksPorDifusion"/>),
        /// así que hace falta un reloj propio por pasada: la prioritaria
        /// puede disparar 2-3 veces por cada vez que dispara la del resto.
        /// </summary>
        private uint _tickUltimaDifusionPrioridad;
        private uint _tickUltimaDifusionResto;
        private bool _difundidoAlgunaVez;

        /// <summary>Carga útil en construcción (servidor). Preasignada una vez: cero allocs gestionadas en el camino caliente del sync.</summary>
        private byte[] _carga;

        /// <summary>Buffer de recepción de las parejas RLE de UN chunk (cliente). Preasignado.</summary>
        private byte[] _rleRx;

        /// <summary>Destinatarios de una difusión: todos los clientes MENOS el nuestro (el anfitrión ya tiene la sim de verdad).</summary>
        private readonly List<ulong> _destinos = new List<ulong>();

        /// <summary>Lote de peticiones de pintura pendientes de mandar al servidor (cliente).</summary>
        private byte[] _lotePintura;
        private int _pinturasPendientes;

        private bool _handlerRegistrado;

        // =================================================================
        // Ciclo de vida
        // =================================================================

        private void Awake()
        {
            // (fix del primer build, red de seguridad simétrica al editor)
            // Reintenta registrar el PlayerPrefab en runtime: en editor la
            // lista interna de NetworkPrefabs puede no existir aún y el
            // registro de AlkahestNetSceneBuilder queda pospuesto a aquí.
            // AddNetworkPrefab lanza si ya está registrado: se ignora.
            // (fix playtest 29, "No hay una referencia a NetworkManager
            // configurada en SessionCoordinator") RED DE SEGURIDAD SIMÉTRICA
            // al cableado del builder: si la escena llegó con el coordinador
            // sin cablear (generación interrumpida a mitad, escena guardada a
            // medias), se recablea AQUÍ en runtime por reflexión -- el campo
            // es [SerializeField] private del template (FriendsLoop, "no
            // tocar salvo integración": preferimos reflexión desde NUESTRO
            // archivo a modificar el suyo). Si ya está cableado, no se toca.
            var coordinador = FindAnyObjectByType<FriendsLoop.Networking.SessionCoordinator>();
            var nmParaCoordinador = FindAnyObjectByType<Unity.Netcode.NetworkManager>();
            if (coordinador != null && nmParaCoordinador != null)
            {
                var campoNm = typeof(FriendsLoop.Networking.SessionCoordinator)
                    .GetField("networkManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (campoNm != null && campoNm.GetValue(coordinador) == null)
                {
                    campoNm.SetValue(coordinador, nmParaCoordinador);
                    var campoTransporte = typeof(FriendsLoop.Networking.SessionCoordinator)
                        .GetField("unityTransport", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (campoTransporte != null && campoTransporte.GetValue(coordinador) == null)
                        campoTransporte.SetValue(coordinador, nmParaCoordinador.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>());
                    UnityEngine.Debug.LogWarning("[TenThousandYears] SessionCoordinator venía sin cablear (escena generada a medias): recableado en runtime. Regenera la escena MULTI (menú Alkahest) cuando puedas.");
                }
            }

            var nmPrefab = FindAnyObjectByType<Unity.Netcode.NetworkManager>();
            if (nmPrefab != null && nmPrefab.NetworkConfig != null && nmPrefab.NetworkConfig.PlayerPrefab != null)
            {
                // (fix 2) SOLO si no está ya en la lista: AddNetworkPrefab a
                // ciegas duplicaba la entrada cuando la escena venía con el
                // registro del editor, y NGO ante un duplicate
                // GlobalObjectIdHash invalida el registro entero (visto en el
                // arranque de Cesar: 'ANFITRIÓN no hacía nada'). Este es EL
                // único punto de registro (el builder ya no puebla la lista).
                bool yaRegistrado = false;
                var listaPrefabs = nmPrefab.NetworkConfig.Prefabs;
                if (listaPrefabs != null && listaPrefabs.Prefabs != null)
                {
                    foreach (var entrada in listaPrefabs.Prefabs)
                    {
                        if (entrada != null && entrada.Prefab == nmPrefab.NetworkConfig.PlayerPrefab) { yaRegistrado = true; break; }
                    }
                }
                if (!yaRegistrado)
                {
                    try { nmPrefab.AddNetworkPrefab(nmPrefab.NetworkConfig.PlayerPrefab); }
                    catch (System.Exception) { /* carrera improbable: ya registrado */ }
                }
            }

            if (Instancia != null && Instancia != this)
            {
                Debug.LogWarning("[TenThousandYears][Red] Ya existe un SimSync en la escena; se destruye el duplicado.");
                Destroy(this);
                return;
            }

            Instancia = this;
            EnEscena = true;

            // (playtest 36, EL CAMINO DEL INVITADO) SaberSync (Net/SaberSync.cs,
            // el saber compartido -- hermano de este archivo) necesita un
            // GameObject con NetworkObject ya spawneado por NGO, y
            // Editor/AlkahestNetSceneBuilder.cs (el único sitio donde nace el
            // GameObject "AlkahestSimSync") está fuera de la lista de
            // archivos permitidos de esta ronda. Se añade AQUÍ, al propio
            // GameObject de este componente: es seguro porque Awake() de
            // TODA la escena corre mucho antes de que NetworkManager.
            // StartHost()/StartClient() se llame (el jugador todavía tiene
            // que pasar por el lobby), que es cuando NGO de verdad recopila
            // los NetworkBehaviour de un objeto de escena para spawnearlo --
            // ver el docblock de SaberSync para el resto del razonamiento.
            if (GetComponent<SaberSync>() == null) gameObject.AddComponent<SaberSync>();

            _carga = new byte[MaxBytesCarga];
            _rleRx = new byte[CeldasPorChunk * 2];
            _lotePintura = new byte[MaxPinturasPorLote * BytesPorPintura];
            _ultimoTickEnviado = new uint[CellGrid.ChunksX * CellGrid.ChunksY];
            for (int i = 0; i < _ultimoTickEnviado.Length; i++) _ultimoTickEnviado[i] = uint.MaxValue;
        }

        public override void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
                EnEscena = false;
            }

            base.OnDestroy();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _sim = FindAnyObjectByType<AlkahestSim>();
            if (_sim == null)
            {
                Debug.LogError("[TenThousandYears][Red] SimSync no encontró un AlkahestSim en la escena: la sesión no puede sincronizar nada.");
                return;
            }

            // DUDA-API: NetworkManager.CustomMessagingManager está disponible
            // en cuanto NGO arranca; se registra aquí (y no en Awake) porque
            // antes de OnNetworkSpawn el NetworkManager puede no estar
            // escuchando todavía y la propiedad sería null.
            var mensajeria = NetworkManager != null ? NetworkManager.CustomMessagingManager : null;
            if (mensajeria != null)
            {
                mensajeria.RegisterNamedMessageHandler(MensajeChunks, AlRecibirChunks);
                _handlerRegistrado = true;
            }
            else
            {
                Debug.LogError("[TenThousandYears][Red] No hay CustomMessagingManager: no se podrán recibir chunks.");
            }

            if (IsServer)
            {
                // EL ANFITRIÓN CONSTRUYE EL MUNDO DE VERDAD. Seed 0 = "elige
                // una aleatoria", exactamente el mismo camino que la escena
                // clásica (ver AlkahestSim.CrearMundo).
                //
                // (CONTRATO_RONDA50.md §4b, ENCARGO M) SEMILLA CERO
                // COMPARTIDA: si el lobby (Net/TallerSesionHud.cs, botón
                // "ANFITRIÓN — SEMILLA CERO compartida") dejó el flag en
                // `true` ANTES de StartHost, la seed pasa a ser la de autor
                // (`Universe.SemillaCero` = 777002) en vez de 0/aleatoria —
                // el resto de la magia (overrides, veta, salas destapadas)
                // la aplica Game/AlkahestSim.cs::CrearMundoInterno leyendo el
                // MISMO flag estático, ver sus comentarios.
                int seedDeLaSesion = AlkahestGameBootstrap.ModoSemillaCero ? (int)Universe.SemillaCero : 0;

                // (RONDA 69g) LA FUGA GEMELA, LADO DEL ANFITRIÓN: un host que
                // venía de jugar "EL INICIO — fundación" (título, un jugador)
                // conservaba `ModoFundacion=true` y CrearMundoInterno le
                // habría construido EL PLANO DE LA FUNDACIÓN (mundo casi
                // vacío) como mundo de la sesión compartida, con overrides de
                // autor sobre una seed aleatoria. El lobby multi no ofrece la
                // fundación: SIEMPRE false aquí. Ver el bloque espejo en
                // AlRecibirChunks para el resto de la historia.
                AlkahestGameBootstrap.ModoFundacion = false;

                // (HANDOFF.md, Playtest 48, deuda "SimSync:330 CrearMundoAnfitrion
                // sin try/catch, candidato #1 del fallo original") CERRADA
                // esta ronda: antes, cualquier excepción real dentro de
                // CrearMundoInterno (Universe.Create/BuildCuartoIntimo/etc.)
                // se tragaba en el try/catch GENÉRICO de NGO alrededor del
                // arranque del host (el mismo patrón mudo que R diagnosticó y
                // cerró para StartHost en el pt48) — el jugador veía "el
                // ANFITRIÓN no hace nada" sin ninguna pista de la causa real.
                // Con log ruidoso aquí, la excepción de verdad queda en la
                // consola ANTES de que nada más intente leer `_sim.Universe`/
                // `_sim.Grid` (que se quedarían en `null`, ver
                // AlkahestSim.CrearMundo: "CrearMundo llamado dos veces" NO
                // aplica aquí porque la excepción corta a media construcción,
                // así que un reintento posterior de StartHost SÍ podría
                // volver a llamar a CrearMundoAnfitrion con `_grid` todavía
                // null — comportamiento correcto, no hace falta guarda extra).
                try
                {
                    _sim.CrearMundoAnfitrion(seedDeLaSesion);
                    Debug.Log("[TenThousandYears][Red] Anfitrión: mundo creado, seed " +
                              (_sim.Universe != null ? _sim.Universe.Seed.ToString() : "?") +
                              (AlkahestGameBootstrap.ModoSemillaCero ? " (SEMILLA CERO compartida)." : "."));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError("[TenThousandYears][Red] CrearMundoAnfitrion reventó construyendo el mundo (seed pedida " +
                                    seedDeLaSesion + "): " + ex + " — el anfitrión se queda SIN mundo (_sim.Grid/_sim.Universe " +
                                    "en null), la sesión no puede continuar. Esta es la costura que el playtest 48 dejó anotada " +
                                    "como candidato #1 del multi roto: ahora la excepción real queda en la consola en vez de " +
                                    "perderse en el try/catch mudo de NGO alrededor de StartHost.");
                }
            }
            else
            {
                // EL INVITADO NO CONSTRUYE NADA TODAVÍA: no puede, no sabe la
                // seed del universo del anfitrión — y sin la MISMA seed los
                // materiales generados por semilla (colores, firmas visuales,
                // química) serían otros y el espejo enseñaría un mundo con los
                // colores equivocados. Se marca el modo espejo y se pide el
                // snapshot: la seed viaja en su cabecera.
                _sim.PrepararEspejo();
                SolicitarSnapshotServerRpc(NetworkManager.LocalClientId);
                Debug.Log("[TenThousandYears][Red] Invitado: espejo preparado, snapshot solicitado al anfitrión.");
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_handlerRegistrado && NetworkManager != null && NetworkManager.CustomMessagingManager != null)
            {
                NetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(MensajeChunks);
                _handlerRegistrado = false;
            }

            _pinturasPendientes = 0;

            // (CONTRATO_RONDA50.md §4b, ENCARGO M) LA FUGA DE ESTADO CLÁSICA:
            // `ModoSemillaCero` es un flag ESTÁTICO (sobrevive a este objeto y
            // a la sesión de red entera). Este despawn es el evento que
            // marca "la sesión multi terminó" en LOS DOS LADOS (anfitrión al
            // dejar de hospedar, invitado al desconectarse de una) — se
            // resetea aquí SIEMPRE, incondicionalmente, sin comprobar si
            // valía `true`: es tan barato como una asignación de bool y así
            // ningún camino de salida puede olvidarlo. Sin este reseteo, un
            // anfitrión que jugó una SEMILLA CERO compartida y luego abre un
            // taller caótico normal (o un invitado que salió de ese lobby y
            // entra a uno normal) seguiría viendo nombres reales/cruces
            // habilitados en un universo donde no pintan nada — el mismo tipo
            // de bug de "estado que sobrevive a la partida" contra el que ya
            // avisa Game/DayCycle.cs (que resetea el flag en sus propios tres
            // caminos de salida del lado de un jugador, ver "SEMILLA CERO —
            // tu primer taller"/"MODO CAÓTICO"/"Nuevo universo": ninguno de
            // esos tres se toca aquí, viven en un archivo que no es mío).
            // Los botones del lobby (Net/TallerSesionHud.cs) YA ponen el
            // flag en su valor correcto ANTES de cada StartHost nuevo, así
            // que este reseteo no les hace falta para arrancar bien — es la
            // red de seguridad para el jugador que NUNCA vuelve a pulsar
            // ningún botón de host/join y solo cierra la sesión.
            AlkahestGameBootstrap.ModoSemillaCero = false;
            AlkahestGameBootstrap.ModoFundacion = false; // (ronda 69g) la fuga gemela -- ver el bloque junto a CrearMundoEspejo.

            base.OnNetworkDespawn();
        }

        /// <summary>
        /// (playtest 43) DOS CADENCIAS INDEPENDIENTES en vez de una: la
        /// pasada prioritaria (ver <see cref="TicksPorDifusionPrioridad"/>)
        /// puede estar "debida" en un tick en el que la del resto todavía
        /// no lo está, y viceversa nunca pasa (6 es múltiplo de 2, así que
        /// cada vez que el resto toca, la prioritaria también). Se calculan
        /// los dos booleanos y se le pasan a <see cref="DifundirChunksSucios"/>,
        /// que decide QUÉ pasadas ejecutar esta llamada -- si ninguna de las
        /// dos toca, se sale sin ni siquiera mirar la sim.
        /// </summary>
        private void Update()
        {
            if (!IsSpawned || _sim == null) return;
            if (!IsServer) return;

            var stepper = _sim.Stepper;
            if (stepper == null) return;

            uint tick = stepper.Tick;

            bool prioridadDebida = !_difundidoAlgunaVez || tick - _tickUltimaDifusionPrioridad >= TicksPorDifusionPrioridad;
            bool restoDebido = !_difundidoAlgunaVez || tick - _tickUltimaDifusionResto >= TicksPorDifusion;
            if (!prioridadDebida && !restoDebido) return;

            _difundidoAlgunaVez = true;
            if (prioridadDebida) _tickUltimaDifusionPrioridad = tick;
            if (restoDebido) _tickUltimaDifusionResto = tick;

            DifundirChunksSucios(prioridadDebida, restoDebido);
        }

        /// <summary>
        /// (playtest 43, CONTRATO_PARIDAD.md §3b, "medir el camino del
        /// verter del invitado") MEDIDO, NO TOCADO: este lote se vacía en
        /// CADA <c>LateUpdate</c>, es decir, hasta una vez por FRAME de
        /// render del cliente (típicamente 60+ Hz) -- NO espera a acumularse
        /// varios ticks de sim (30Hz) como sí hace la difusión de chunks del
        /// servidor. Un invitado que empieza a aspirar/verter manda su
        /// primer <see cref="SolicitarPinturaServerRpc"/> en el frame
        /// siguiente al gesto (≤~16ms a 60fps), muy por debajo del ≤2 ticks
        /// (~66ms) que pedía el encargo como techo si hubiera que bajarlo --
        /// así que <c>Game/Flask.cs</c> NO SE TOCÓ en esta ronda (fuera de
        /// alcance salvo que la medición lo pidiera, y no lo pide): el cuello
        /// de botella que describía el diagnóstico (§0.4) vivía ENTERO en la
        /// difusión de vuelta del servidor (la que sí se acelera arriba,
        /// <see cref="TicksPorDifusionPrioridad"/>), no en este camino de
        /// ida.
        /// </summary>
        private void LateUpdate()
        {
            // El lote de pintura se vacía en LateUpdate (no en Update) para
            // recoger TODO lo que el frasco haya pedido este frame,
            // independientemente del orden de Update entre componentes.
            if (_pinturasPendientes > 0) VaciarLotePintura();
        }

        // =================================================================
        // SERVIDOR: difusión de chunks
        // =================================================================

        /// <summary>
        /// (playtest 36, EL CAMINO DEL INVITADO) Radio, en celdas, alrededor
        /// de CUALQUIER avatar conectado (<see cref="AprendizNet.Todos"/>,
        /// host incluido) dentro del cual un chunk sucio se prioriza en la
        /// difusión -- ver el docblock de <see cref="DifundirChunksSucios"/>,
        /// "LA PRIORIDAD POR AVATAR". Un poco más que una pantalla (viewport
        /// típico bastante por debajo de esto): de sobra para que "lo que
        /// cualquiera tiene delante" nunca se quede esperando detrás de
        /// actividad lejana cuando el presupuesto de la difusión aprieta.
        /// </summary>
        private const float RadioPrioridadCeldas = 60f;

        /// <summary>
        /// Recorre los chunks buscando los que cambiaron desde el último
        /// envío y los mete en mensajes de hasta <see cref="MaxBytesCarga"/>.
        ///
        /// (playtest 36, auditoría P3, "¿la cadencia de sync prioriza chunks
        /// cerca de los JUGADORES REMOTOS o solo del host?") ANTES: un único
        /// barrido circular por ÍNDICE de chunk, sin ninguna noción de
        /// dónde está nadie -- ni del host ni de los invitados. Con el
        /// presupuesto de <see cref="MaxChunksPorDifusion"/> ya cubierto por
        /// actividad lejana (p. ej. el host tallando/vertiendo en su zona),
        /// un chorro de grifo recién abierto por un invitado en OTRA punta
        /// del taller podía quedar esperando varias pasadas detrás de
        /// chunks que a NADIE le importaban todavía -- el "hueco" real detrás
        /// de "a veces no se ve que sale el líquido de su botellita".
        ///
        /// AHORA: dos pasadas. La PRIMERA (<see cref="ChunkCercaDeAlgunAvatar"/>)
        /// recorre TODO el grid (864 comparaciones de distancia al cuadrado,
        /// aritmética pura, ver el coste ya aceptado por
        /// <see cref="Net.MaquinaSync"/> para su propio sondeo) priorizando
        /// lo sucio cerca de CUALQUIER avatar conectado, sin importar el
        /// orden del índice. La SEGUNDA es el barrido circular de siempre
        /// (<see cref="_cursorBarrido"/>, SIN TOCAR su semántica: solo
        /// avanza sobre chunks que de verdad se mandaron) para lo que sobre
        /// de presupuesto -- así lo lejano de TODOS los avatares nunca se
        /// queda esperando para siempre, solo cede el turno cuando hay
        /// actividad cerca de alguien.
        ///
        /// (playtest 43, LA PARIDAD VIVA) Las dos pasadas ahora tienen
        /// CADENCIAS INDEPENDIENTES (<see cref="TicksPorDifusionPrioridad"/>
        /// = 2 ticks/~15Hz vs <see cref="TicksPorDifusion"/> = 6 ticks/~5Hz,
        /// ver <see cref="Update"/>): `incluirPrioridad`/`incluirResto`
        /// dicen cuál de las dos toca ESTA llamada. El presupuesto
        /// (<see cref="MaxChunksPorDifusion"/>) sigue siendo UNO SOLO
        /// compartido entre las dos cuando coinciden en el mismo tick (igual
        /// que antes de esta ronda: la prioritaria consume primero, la del
        /// resto se queda con lo que sobre) -- sin cambios de presupuesto,
        /// solo de frecuencia, tal como pide el contrato §3b.
        /// </summary>
        private void DifundirChunksSucios(bool incluirPrioridad, bool incluirResto)
        {
            var grid = _sim.Grid;
            if (grid == null) return;

            if (!PrepararDestinos())
            {
                // Nadie conectado todavía: el anfitrión juega solo. Se pone al
                // día el registro de "último enviado" igual, porque si no, en
                // cuanto entrase el primer invitado el barrido encontraría
                // TODOS los chunks que hayan cambiado desde el arranque como
                // sucios y los reenviaría durante segundos — información que
                // ese invitado ya recibió entera en su snapshot.
                MarcarTodoComoEnviado(grid);
                return;
            }

            int total = CellGrid.ChunksX * CellGrid.ChunksY;
            int longitud = 0;
            int chunksEnMensaje = 0;
            int enviadosEstaPasada = 0;

            // PASADA 1: prioridad por avatar (ver el docblock de arriba). No
            // toca `_cursorBarrido` -- es un barrido aparte, siempre desde el
            // índice 0, que solo importa MIENTRAS haya presupuesto libre.
            // (playtest 43) Gateada por `incluirPrioridad`: en un tick en el
            // que solo toca la cadencia rápida, esta es la ÚNICA pasada que
            // corre.
            if (incluirPrioridad)
            {
                for (int ci = 0; ci < total && enviadosEstaPasada < MaxChunksPorDifusion; ci++)
                {
                    if (grid.chunkTouchedTick[ci] == _ultimoTickEnviado[ci]) continue;
                    if (!ChunkCercaDeAlgunAvatar(ci)) continue;
                    if (IntentarCodificarChunk(grid, ci, ref longitud, ref chunksEnMensaje))
                    {
                        enviadosEstaPasada++;
                    }
                }
            }

            // PASADA 2: el barrido circular de siempre, para lo que sobre de
            // presupuesto. El punto de arranque se congela en un local:
            // `_cursorBarrido` se mueve DENTRO del bucle (para dejar apuntado
            // dónde seguir la próxima vez) y usarlo también como origen del
            // recorrido haría que el barrido se saltara chunks.
            // (playtest 43) Gateada por `incluirResto`: en los ticks
            // "intermedios" (solo la cadencia prioritaria tocaba) esta
            // pasada NO corre -- el barrido circular sigue avanzando a sus
            // 6 ticks de siempre, sin acelerar (el contrato solo pide
            // acelerar la zona cercana a un avatar).
            if (incluirResto)
            {
                int inicio = _cursorBarrido;

                for (int paso = 0; paso < total && enviadosEstaPasada < MaxChunksPorDifusion; paso++)
                {
                    int ci = inicio + paso;
                    if (ci >= total) ci -= total;

                    // Ya sucio-y-mandado por la pasada 1 (o ya limpio): el mismo
                    // chequeo de siempre lo descarta sin duplicar el envío,
                    // porque la pasada 1 ya actualizó `_ultimoTickEnviado`.
                    if (!IntentarCodificarChunk(grid, ci, ref longitud, ref chunksEnMensaje)) continue;

                    enviadosEstaPasada++;
                    _cursorBarrido = ci + 1;
                    if (_cursorBarrido >= total) _cursorBarrido = 0;
                }
            }

            if (chunksEnMensaje > 0)
            {
                EnviarCarga(TipoDelta, (ushort)chunksEnMensaje, longitud, null);
            }
        }

        /// <summary>
        /// ¿Hay algún avatar conectado (host o invitado, ver
        /// <see cref="AprendizNet.Todos"/>) a menos de
        /// <see cref="RadioPrioridadCeldas"/> del CENTRO de este chunk? Pura
        /// aritmética (distancia al cuadrado, sin raíz), cero allocs -- se
        /// llama hasta 864 veces por difusión, el mismo presupuesto que ya
        /// paga <see cref="Net.MaquinaSync"/> por su propio sondeo.
        /// </summary>
        private static bool ChunkCercaDeAlgunAvatar(int ci)
        {
            var avatares = AprendizNet.Todos;
            if (avatares.Count == 0) return false;

            int cx = ci % CellGrid.ChunksX;
            int cy = ci / CellGrid.ChunksX;
            CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);

            float c = SimRenderer.CellWorldSize;
            float centroX = (x0 + x1) * 0.5f * c;
            float centroY = (y0 + y1) * 0.5f * c;
            float radioMundo = RadioPrioridadCeldas * c;
            float r2 = radioMundo * radioMundo;

            for (int i = 0; i < avatares.Count; i++)
            {
                var a = avatares[i];
                if (a == null) continue;
                Vector3 p = a.transform.position;
                float dx = p.x - centroX;
                float dy = p.y - centroY;
                if (dx * dx + dy * dy <= r2) return true;
            }
            return false;
        }

        /// <summary>
        /// Codifica el chunk `ci` en <see cref="_carga"/> si está sucio
        /// (compara <c>chunkTouchedTick</c> contra <see cref="_ultimoTickEnviado"/>,
        /// el MISMO criterio en las dos pasadas de <see cref="DifundirChunksSucios"/>),
        /// cerrando el mensaje actual y abriendo otro si no cabe otro chunk
        /// del peor caso (nunca se trunca un chunk a medias). Devuelve true
        /// si se codificó de verdad (y en ese caso ya actualizó
        /// <see cref="_ultimoTickEnviado"/>, así que la pasada 2 nunca puede
        /// reenviar lo que ya mandó la pasada 1).
        /// </summary>
        private bool IntentarCodificarChunk(CellGrid grid, int ci, ref int longitud, ref int chunksEnMensaje)
        {
            if (grid.chunkTouchedTick[ci] == _ultimoTickEnviado[ci]) return false;

            if (longitud + MaxBytesPorChunk > MaxBytesCarga && chunksEnMensaje > 0)
            {
                EnviarCarga(TipoDelta, (ushort)chunksEnMensaje, longitud, null);
                longitud = 0;
                chunksEnMensaje = 0;
            }

            int escritos = CodificarChunk(grid, ci, _carga, longitud);
            if (escritos <= 0) return false;

            longitud += escritos;
            chunksEnMensaje++;
            _ultimoTickEnviado[ci] = grid.chunkTouchedTick[ci];
            return true;
        }

        /// <summary>
        /// Manda el grid ENTERO a un cliente recién llegado. Casi todo el
        /// mundo es piedra maciza, así que 864 chunks caben en unos pocos KB
        /// (un chunk de un solo material son 6 bytes).
        /// </summary>
        private void EnviarSnapshot(ulong clientId)
        {
            var grid = _sim.Grid;
            if (grid == null)
            {
                Debug.LogWarning("[TenThousandYears][Red] Se pidió un snapshot antes de que el mundo existiera; se ignora.");
                return;
            }

            int total = CellGrid.ChunksX * CellGrid.ChunksY;
            int longitud = 0;
            int chunksEnMensaje = 0;
            int mensajes = 0;

            for (int ci = 0; ci < total; ci++)
            {
                if (longitud + MaxBytesPorChunk > MaxBytesCarga && chunksEnMensaje > 0)
                {
                    EnviarCarga(TipoSnapshot, (ushort)chunksEnMensaje, longitud, clientId);
                    mensajes++;
                    longitud = 0;
                    chunksEnMensaje = 0;
                }

                int escritos = CodificarChunk(grid, ci, _carga, longitud);
                if (escritos <= 0) continue;

                longitud += escritos;
                chunksEnMensaje++;
            }

            if (chunksEnMensaje > 0)
            {
                EnviarCarga(TipoSnapshot, (ushort)chunksEnMensaje, longitud, clientId);
                mensajes++;
            }

            Debug.Log("[TenThousandYears][Red] Snapshot completo enviado al cliente " + clientId +
                      " (" + mensajes + " mensaje(s), seed " + _sim.Universe.Seed + ").");
        }

        /// <summary>
        /// Codifica un chunk en `destino` a partir de `offset`:
        /// ushort índiceChunk, ushort nºParejas, y luego las parejas
        /// (byte material, byte cuenta) recorriendo el chunk por filas.
        /// Devuelve los bytes escritos.
        /// </summary>
        private static int CodificarChunk(CellGrid grid, int ci, byte[] destino, int offset)
        {
            int cx = ci % CellGrid.ChunksX;
            int cy = ci / CellGrid.ChunksX;
            CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);

            int p = offset + BytesCabeceraChunk;
            int parejas = 0;

            byte actual = grid.mat[CellGrid.Idx(x0, y0)];
            int cuenta = 0;

            for (int y = y0; y < y1; y++)
            {
                int fila = y * CellGrid.W;
                for (int x = x0; x < x1; x++)
                {
                    byte m = grid.mat[fila + x];
                    if (m == actual && cuenta < 255)
                    {
                        cuenta++;
                        continue;
                    }

                    destino[p++] = actual;
                    destino[p++] = (byte)cuenta;
                    parejas++;
                    actual = m;
                    cuenta = 1;
                }
            }

            if (cuenta > 0)
            {
                destino[p++] = actual;
                destino[p++] = (byte)cuenta;
                parejas++;
            }

            destino[offset] = (byte)(ci & 0xFF);
            destino[offset + 1] = (byte)((ci >> 8) & 0xFF);
            destino[offset + 2] = (byte)(parejas & 0xFF);
            destino[offset + 3] = (byte)((parejas >> 8) & 0xFF);

            return p - offset;
        }

        /// <summary>Da por enviado el estado actual de todos los chunks (ver la llamada en <see cref="DifundirChunksSucios"/>).</summary>
        private void MarcarTodoComoEnviado(CellGrid grid)
        {
            for (int ci = 0; ci < _ultimoTickEnviado.Length; ci++)
            {
                _ultimoTickEnviado[ci] = grid.chunkTouchedTick[ci];
            }
        }

        /// <summary>Rellena <see cref="_destinos"/> con todos los clientes menos nosotros. Devuelve false si no hay nadie.</summary>
        private bool PrepararDestinos()
        {
            _destinos.Clear();
            if (NetworkManager == null) return false;

            // DUDA-API: NetworkManager.ConnectedClientsIds (IReadOnlyList<ulong>,
            // solo válida en el servidor — que es el único sitio desde donde se
            // llama a este método). Alternativa equivalente si la firma no
            // cuadrara: recorrer ConnectedClientsList y leer .ClientId, que es
            // lo que hace FriendsLoop.Demo.DemoHud.
            var lista = NetworkManager.ConnectedClientsIds;
            for (int i = 0; i < lista.Count; i++)
            {
                ulong id = lista[i];
                if (id == NetworkManager.LocalClientId) continue; // el anfitrión ya tiene la sim de verdad
                _destinos.Add(id);
            }

            return _destinos.Count > 0;
        }

        /// <summary>
        /// Empaqueta la cabecera + la carga en un <see cref="FastBufferWriter"/>
        /// del tamaño EXACTO y lo manda. `destinoUnico` null = difusión a
        /// todos los clientes menos el nuestro.
        ///
        /// El writer se crea con <c>Allocator.Temp</c> en cada envío (5 veces
        /// por segundo): es el asignador de pila nativo de Unity, no toca el
        /// GC, y es la forma canónica de usar FastBufferWriter en NGO. Lo que
        /// sí está preasignado y se reutiliza es el buffer de carga
        /// (<see cref="_carga"/>), que es el que se toca por celda.
        /// </summary>
        private void EnviarCarga(byte tipo, ushort numChunks, int longitud, ulong? destinoUnico)
        {
            if (longitud <= 0) return;

            var mensajeria = NetworkManager != null ? NetworkManager.CustomMessagingManager : null;
            if (mensajeria == null) return;

            int seed = _sim.Universe != null ? _sim.Universe.Seed : 0;

            using (var writer = new FastBufferWriter(BytesCabeceraMensaje + longitud, Allocator.Temp))
            {
                // DUDA-API: WriteValueSafe<T> para primitivas + WriteBytesSafe
                // (byte[], nºbytes). El writer se dimensiona EXACTO, así que
                // nunca depende de que crezca solo.
                writer.WriteValueSafe(tipo);
                writer.WriteValueSafe(seed);
                writer.WriteValueSafe(numChunks);
                writer.WriteBytesSafe(_carga, longitud);

                // DUDA-API: los dos overloads de SendNamedMessage — a un
                // cliente (ulong) y a una lista (IReadOnlyList<ulong>), ambos
                // con FastBufferWriter y NetworkDelivery. El fragmentado
                // confiable es el único que admite mensajes de varios KB.
                if (destinoUnico.HasValue)
                {
                    mensajeria.SendNamedMessage(MensajeChunks, destinoUnico.Value, writer,
                        NetworkDelivery.ReliableFragmentedSequenced);
                }
                else
                {
                    mensajeria.SendNamedMessage(MensajeChunks, _destinos, writer,
                        NetworkDelivery.ReliableFragmentedSequenced);
                }
            }
        }

        // =================================================================
        // CLIENTE: recepción
        // =================================================================

        /// <summary>
        /// Handler del mensaje nombrado. Firma fija de NGO
        /// (<c>CustomMessagingManager.HandleNamedMessageDelegate</c>): id del
        /// emisor + lector.
        ///
        /// DUDA-API (la más importante de este archivo): el template
        /// `Assets/FriendsLoop` NO USA el CustomMessagingManager en ninguna
        /// parte, así que TODO el mensaje nombrado —registro, firma del
        /// delegado, envío y lectura— es la única pieza de este POC que no se
        /// pudo calcar de código ya probado en el proyecto. Si algo de NGO no
        /// compila a la primera, empezar por aquí y por
        /// <see cref="EnviarCarga"/>.
        /// </summary>
        private void AlRecibirChunks(ulong emisor, FastBufferReader lector)
        {
            // El anfitrión nunca se manda chunks a sí mismo (ver
            // PrepararDestinos), pero la guarda se queda: si algún día alguien
            // cambia el envío a SendNamedMessageToAll, el anfitrión no debe
            // pisarse su propia sim con una copia de sí misma.
            if (IsServer || _sim == null) return;

            lector.ReadValueSafe(out byte tipo);
            lector.ReadValueSafe(out int seed);
            lector.ReadValueSafe(out ushort numChunks);

            if (_sim.Grid == null)
            {
                if (tipo != TipoSnapshot)
                {
                    // Delta antes del snapshot: no hay dónde aplicarlo. Se
                    // descarta a propósito — el snapshot que ya viene en
                    // camino trae el mundo entero, este delta incluido.
                    return;
                }

                // (CONTRATO_RONDA50.md §4b, ENCARGO M) SEMILLA CERO
                // COMPARTIDA, LADO DEL INVITADO: la seed viaja en la cabecera
                // de TODO mensaje de chunks (ver EnviarCarga) desde antes de
                // esta ronda — este es el ÚNICO sitio donde el invitado la ve
                // por primera vez, así que es el ÚNICO sitio donde puede
                // detectar "el anfitrión está en el laboratorio compartido".
                // Se fija el flag ANTES de CrearMundoEspejo (no después): ese
                // método construye `_universe`/`_grid` con
                // Game/AlkahestSim.cs::CrearMundoInterno, que lee
                // `AlkahestGameBootstrap.ModoSemillaCero` para aplicar
                // Universe.AplicarOverridesSemillaCero SOBRE ESTE MISMO
                // Universe local (ver el comentario nuevo de ese método) —
                // si el flag llegara tarde, el invitado tendría los ids/
                // colores correctos (misma seed) pero NINGUNA identidad real
                // ni umbral de autor, y SubstanceKnowledge/AlbumReal locales
                // (que SÍ leen el flag en tiempo real, no solo al construir
                // el mundo) mostrarían nombres provisionales mientras el
                // anfitrión ya muestra los reales.
                //
                // El `else` es LA MITAD QUE SE OLVIDA FÁCIL (fuga de estado):
                // sin él, un invitado que primero visitó un lobby Semilla
                // Cero y luego se une a uno caótico normal (sin recargar la
                // app) conservaría el flag en `true` de la sesión anterior —
                // exactamente el bug que este contrato pide cerrar "en AMBOS
                // LADOS". `OnNetworkDespawn` (más abajo) ya cubre "salir de
                // la sesión"; este `else` cubre el caso más fino de "entrar
                // directo a una sesión distinta sin pasar por un despawn de
                // por medio" (imposible en la práctica con este template —
                // no se puede unir dos veces sin desconectar — pero es la
                // misma comprobación barata que hace este método para la
                // seed en la rama de abajo, cero costo real).
                AlkahestGameBootstrap.ModoSemillaCero = seed == (int)Universe.SemillaCero;
                // (RONDA 69g, "el multi se rompió" -- captura del invitado de
                // Cesar) LA FUGA GEMELA que el bloque de arriba no cubría:
                // `ModoFundacion` es TAN estático como ModoSemillaCero, lo
                // enciende el botón "EL INICIO — fundación" del título (el
                // PRIMER botón que pulsa un jugador nuevo, ronda 60) y NADIE
                // lo apagaba en el camino multi -- ni el lobby, ni este
                // snapshot, ni el despawn. Un invitado que pasó por la
                // fundación y luego se une: CrearMundoInterno le construía
                // BuildFundacion como BASE del espejo y -- mucho peor --
                // aplicaba Universe.AplicarOverridesSemillaCero SOBRE la seed
                // del anfitrión: paleta, identidades y umbrales de OTRO
                // universo. El snapshot corregía la geometría (mat[] entero,
                // fiable) pero los colores/identidades quedaban rotos para
                // toda la sesión: "todo desincronizado y rotísimo" (textual).
                // La fundación es una experiencia DE UN JUGADOR: el lobby
                // multi no la ofrece, así que aquí SIEMPRE false. Si algún
                // día existe fundación co-op, su botón de lobby pondrá el
                // flag igual que hoy lo hacen los de Semilla Cero.
                AlkahestGameBootstrap.ModoFundacion = false;

                _sim.CrearMundoEspejo(seed);
                if (_sim.Grid == null) return;
            }
            else if (_sim.Universe != null && _sim.Universe.Seed != seed)
            {
                Debug.LogError("[TenThousandYears][Red] La seed del anfitrión (" + seed + ") no coincide con la del espejo (" +
                               _sim.Universe.Seed + "): los materiales generados por semilla no son los mismos. " +
                               "Se ignora el mensaje — reconéctate.");
                return;
            }

            for (int i = 0; i < numChunks; i++)
            {
                lector.ReadValueSafe(out ushort ci);
                lector.ReadValueSafe(out ushort parejas);

                int bytes = parejas * 2;
                if (bytes > _rleRx.Length) return; // paquete corrupto: mejor abandonar que escribir basura en el espejo

                // DUDA-API: FastBufferReader.ReadBytesSafe(ref byte[], int) —
                // el buffer destino se pasa por referencia y NGO lo rellena
                // desde el offset 0. Forma conservadora: buffer preasignado al
                // peor caso, nunca redimensionado por el lector.
                lector.ReadBytesSafe(ref _rleRx, bytes);

                _sim.AplicarChunkRemoto(ci, _rleRx, parejas);
            }
        }

        // =================================================================
        // CLIENTE -> SERVIDOR: pintura y snapshot
        // =================================================================

        /// <summary>
        /// Encola una mutación del espejo para mandarla al anfitrión. La llama
        /// <see cref="AlkahestSim"/> desde Paint/PaintStable/PaintCell/PaintRect
        /// cuando está en modo espejo. No manda nada todavía: el lote entero
        /// sale en un solo RPC al final del frame.
        /// </summary>
        public static void ReenviarPintura(int x, int y, int radio, byte material, byte modo, byte tempRaw)
        {
            var s = Instancia;
            if (s == null || !s.IsSpawned || s.IsServer) return;
            s.EncolarPintura(x, y, radio, material, modo, tempRaw);
        }

        private void EncolarPintura(int x, int y, int radio, byte material, byte modo, byte tempRaw)
        {
            if (_pinturasPendientes >= MaxPinturasPorLote)
            {
                // El lote se llenó este frame: se vacía ya y se sigue
                // encolando en el siguiente hueco (mejor dos RPC que perder
                // celdas del gesto del jugador).
                VaciarLotePintura();
                if (_pinturasPendientes >= MaxPinturasPorLote) return;
            }

            if (x < 0) x = 0; if (x > 0xFFFF) x = 0xFFFF;
            if (y < 0) y = 0; if (y > 0xFFFF) y = 0xFFFF;
            if (radio < 0) radio = 0; if (radio > 255) radio = 255;

            int p = _pinturasPendientes * BytesPorPintura;
            _lotePintura[p + 0] = (byte)(x & 0xFF);
            _lotePintura[p + 1] = (byte)((x >> 8) & 0xFF);
            _lotePintura[p + 2] = (byte)(y & 0xFF);
            _lotePintura[p + 3] = (byte)((y >> 8) & 0xFF);
            _lotePintura[p + 4] = (byte)radio;
            _lotePintura[p + 5] = material;
            _lotePintura[p + 6] = modo;
            _lotePintura[p + 7] = tempRaw;
            _pinturasPendientes++;
        }

        /// <summary>
        /// Manda el lote acumulado. Se copia a un array del tamaño exacto
        /// porque un parámetro de RPC se serializa entero: mandar siempre el
        /// buffer completo de 1.5 KB costaría más que la copia. Solo asigna
        /// mientras el invitado está aspirando o vertiendo de verdad.
        /// </summary>
        private void VaciarLotePintura()
        {
            if (_pinturasPendientes <= 0) return;
            if (!IsSpawned || IsServer) { _pinturasPendientes = 0; return; }

            int bytes = _pinturasPendientes * BytesPorPintura;
            var lote = new byte[bytes];
            System.Array.Copy(_lotePintura, lote, bytes);
            _pinturasPendientes = 0;

            SolicitarPinturaServerRpc(lote);
        }

        /// <summary>
        /// El anfitrión aplica las mutaciones que le pide un invitado. Mismo
        /// estilo de atributo que `FriendsLoop.Demo.SharedInteractable`
        /// (InvokePermission = Everyone: el dueño de este objeto de escena es
        /// el servidor, así que sin eso ningún cliente podría invocarlo).
        ///
        /// DUDA-API: parámetro `byte[]` en un Rpc de NGO 2.x — los arrays de
        /// tipos no gestionados están soportados por el generador de código;
        /// es la forma más conservadora de mandar un lote de tamaño variable
        /// sin inventar un INetworkSerializable propio.
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SolicitarPinturaServerRpc(byte[] lote)
        {
            if (_sim == null || _sim.Grid == null || lote == null) return;

            for (int p = 0; p + BytesPorPintura <= lote.Length; p += BytesPorPintura)
            {
                int x = lote[p] | (lote[p + 1] << 8);
                int y = lote[p + 2] | (lote[p + 3] << 8);
                int radio = lote[p + 4];
                byte material = lote[p + 5];
                byte modo = lote[p + 6];
                byte tempRaw = lote[p + 7];

                switch (modo)
                {
                    case ModoPaintStable: _sim.PaintStable(x, y, radio, material); break;
                    case ModoPaintCell: _sim.PaintCell(x, y, material, tempRaw); break;
                    case ModoPaintRect: _sim.PaintRect(x, y, radio, tempRaw, material); break;
                    default: _sim.Paint(x, y, radio, material); break;
                }
            }
        }

        /// <summary>
        /// El invitado pide su snapshot en cuanto tiene el handler registrado.
        /// El id viaja como parámetro explícito en vez de leerse de
        /// `RpcParams.Receive.SenderClientId`: es la forma que este proyecto
        /// puede calcar del template sin apostar por una API que no aparece en
        /// él (aquí no hay nada que proteger — un cliente que mintiera solo
        /// conseguiría mandarle un snapshot a otro).
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SolicitarSnapshotServerRpc(ulong clientId)
        {
            if (!IsServer || _sim == null) return;
            EnviarSnapshot(clientId);
        }
    }
}
