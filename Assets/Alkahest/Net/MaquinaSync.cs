using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Alkahest.Game;
using Alkahest.Sim;

namespace Alkahest.Net
{
    /// <summary>
    /// LAS MÁQUINAS EN RED (siguiente ronda del playtest 29, "Máquinas como
    /// objetos de red (mudanza para invitados): pospuesto... por decisión de
    /// Cesar" -- ver docs/HANDOFF.md). Mandato de Cesar para el playtest de
    /// mañana con amigos: *"lo ideal es poder mudarlas para que cada quien se
    /// organice como quiera"*.
    ///
    /// LA ARQUITECTURA SIM-SOLO-HOST NO SE TOCA. Las cinco estaciones
    /// (Crisol/Prensa/BancoChispa/ColumnaEnsayo/EnsayoMaestro) y los dos
    /// grifos (Dispenser) siguen viviendo EXCLUSIVAMENTE en el anfitrión --
    /// leen y escriben la sim cada tick, y la sim solo existe ahí (ver
    /// Net/SimSync.cs). Este componente no las mueve a otra parte: publica un
    /// REGISTRO de dónde está cada una (tipo + celda de anclaje + tamaño +
    /// centro de mundo) para que los invitados puedan dibujar una RÉPLICA
    /// puramente visual (<see cref="MaquinaReplica"/>) en su propio espejo, y
    /// acepta que un invitado PIDA mover una máquina real -- pero quien la
    /// mueve de verdad sigue siendo, siempre, el anfitrión.
    ///
    /// =====================================================================
    /// EL REGISTRO: NetworkList&lt;EntradaMaquina&gt;
    /// =====================================================================
    /// Fuente de verdad: los <see cref="IMovible"/> ya registrados por cada
    /// estación en su propio Init (ver Game/Mudanza.cs, `RegistrarMovible`) --
    /// pero este componente NO reutiliza esa lista de Mudanza (privada, y
    /// pensada para "qué puede agarrar el jugador", no para "qué existe").
    /// En vez de eso escanea la escena con `FindObjectsByType` de cada tipo
    /// concreto UNA vez, tan pronto como las máquinas existen (el anfitrión
    /// las crea con un frame o más de retraso respecto al spawn de este
    /// objeto, ver <see cref="IntentarEscanear"/>), y a partir de ahí SONDEA
    /// el `AnclaCelda` de cada una cada <see cref="IntervaloSondeoSeg"/>
    /// (0.5s) -- sondeo barato: son 5+2 aparatos, comparar un Vector2Int no
    /// cuesta nada, y machacar el registro entero cada frame sí notaría en
    /// el ancho de banda (el mismo razonamiento de "cuota" que ya aplica
    /// Net/SimSync.cs a los chunks de la sim, aquí sin necesidad de cuota
    /// porque son 7 elementos, no 864).
    ///
    /// =====================================================================
    /// EL PROTOCOLO DE MUDANZA DE UN INVITADO
    /// =====================================================================
    ///  1. El invitado agarra una <see cref="MaquinaReplica"/> con Mudanza
    ///     (Game/Mudanza.cs, sin cambios de lógica: una réplica ES un
    ///     IMovible más, ver el docblock de esa clase) y la suelta en un
    ///     sitio.
    ///  2. `MaquinaReplica.Reposicionar` (la única llamada que hace Mudanza)
    ///     mueve la réplica AHÍ MISMO, de forma optimista -- el "fantasma
    ///     local mientras carga" del encargo -- y llama a
    ///     <see cref="PedirMudanza"/>, que dispara
    ///     <see cref="SolicitarMudanzaRpc"/> hacia el servidor.
    ///  3. El anfitrión valida con <see cref="IMovible.CabeEnAncla"/> DE LA
    ///     MÁQUINA REAL (la autoridad de siempre, la misma que usa su propio
    ///     Mudanza) y, si cabe, llama a `Reposicionar` de verdad.
    ///  4. Si se aceptó, la entrada del registro se actualiza YA (no espera
    ///     al siguiente sondeo de 0.5s: es la reacción directa a un gesto del
    ///     jugador, no un cambio ambiental) y el `NetworkList` la replica
    ///     sola a todos -- incluido el solicitante, que ve su fantasma
    ///     "confirmado" en el mismo sitio donde ya estaba.
    ///  5. Si se rechazó, nada cambia en el registro: <see cref="MudanzaRechazadaRpc"/>
    ///     avisa a los invitados para que la réplica que se movió de más
    ///     vuelva a su última posición CONFIRMADA (ver
    ///     <see cref="MaquinaReplica.AlRechazar"/>).
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class MaquinaSync : NetworkBehaviour
    {
        /// <summary>
        /// Mismo orden que las constantes Tipo* de
        /// MaquinariaSprites.ConstruirVisualEstatico (ver el docblock de esas
        /// constantes para el porqué de la duplicación) -- SOLO para los
        /// valores 0..7. (fix Cesar playtest 33) Balda/Anclaje se suman al
        /// final -- el sistema de baldas como construcción entra al MISMO
        /// registro que las cinco estaciones y los grifos, así que un
        /// invitado ve/puede mover las baldas y los anclajes tal cual
        /// ve/mueve el resto (réplica visual + petición de mudanza vía
        /// Game/Mudanza.cs, sin ningún camino nuevo).
        ///
        /// (fix Cesar playtest 34, causa raíz confirmada del reporte "EN
        /// MULTI no aparecen las redomas ni el alambique") Rack/Alambique/
        /// Pila se suman DESPUÉS de esa correspondencia -- MaquinariaSprites.cs
        /// (Game/) no está en la lista de archivos permitidos de esta ronda,
        /// así que <c>ConstruirVisualEstatico</c> no tiene un caso dedicado
        /// para 8/9/10 y cae a su <c>default: Solido()</c> (un rectángulo
        /// genérico, "nunca null" según su propio comentario) -- una réplica
        /// menos fiel que las de 0..7, pero agarrable, movible y con chapa
        /// correcta (ver Net/MaquinaReplica.cs), que es lo que pedía el
        /// encargo. Consolidar un sprite propio para cada uno queda como
        /// deuda para una ronda con MaquinariaSprites.cs en alcance.
        /// </summary>
        public enum TipoMaquina : byte
        {
            Crisol = 0,
            Prensa = 1,
            BancoChispa = 2,
            ColumnaEnsayo = 3,
            EnsayoMaestro = 4,
            Dispenser = 5,
            Balda = 6,
            Anclaje = 7,
            Rack = 8,
            Alambique = 9,
            Pila = 10,
        }

        /// <summary>Instancia única en la escena (mismo patrón que SimSync/AprendizNet).</summary>
        public static MaquinaSync Instancia { get; private set; }

        // -----------------------------------------------------------------
        // Una entrada del registro. INetworkSerializable a mano (no hay
        // generador de código para tipos "plain data" como este en NGO) --
        // patrón estándar de la documentación de Netcode for GameObjects,
        // igual en todas las versiones 1.x/2.x.
        // -----------------------------------------------------------------
        public struct EntradaMaquina : INetworkSerializable, System.IEquatable<EntradaMaquina>
        {
            public byte tipo;
            public byte indice; // ordinal DENTRO del tipo (0 para las cinco estaciones únicas; 0/1 para los dos grifos) -- ver AgregarTipo.

            /// <summary>Celda de anclaje real (IMovible.AnclaCelda) -- es lo único que necesita <see cref="SolicitarMudanzaRpc"/>, la semántica exacta (esquina vs. boquilla) es cosa de cada máquina, nunca de este archivo.</summary>
            public ushort anclaX;
            public ushort anclaY;

            /// <summary>
            /// Centro de mundo (IMovible.CentroMundo) EN VEZ DE derivarlo de
            /// anclaX/anclaY en el cliente: las cinco estaciones anclan por
            /// la esquina inferior izquierda de su rect exterior pero el
            /// Dispenser ancla por la celda de la boquilla con un offset
            /// propio (ver Game/Dispenser.cs, AnclaCelda) -- reconstruir esa
            /// aritmética aquí para cada tipo sería exactamente el
            /// acoplamiento a detalles privados que este archivo evita en
            /// otros sitios. Mandar el centro ya resuelto cuesta 8 bytes más
            /// por entrada (7 entradas, nada) y es EXACTO para cualquier
            /// convención de anclaje presente o futura.
            /// </summary>
            public float centroX;
            public float centroY;

            /// <summary>IMovible.TamanoMundo, para escalar la réplica (MaquinariaSprites.ConstruirVisualEstatico) y para la aproximación de CabeEnAncla del lado del invitado (ver MaquinaReplica).</summary>
            public float tamanoX;
            public float tamanoY;

            /// <summary>
            /// (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §1/§2b) EL ESTADO
            /// VIVO replicado -- bits en <see cref="EstadoVivoBits"/>. Solo
            /// tiene sentido para las 7 estaciones que implementan
            /// <see cref="IMaquinaUsableRemota"/> (Balda/Anclaje/Rack/Pila se
            /// quedan siempre en 0, ver <see cref="SondearEstadoVivo"/>).
            /// Escrito SOLO por el servidor, igual que el resto de la
            /// entrada -- el sondeo (<see cref="SondearEstadoVivo"/>) es el
            /// único escritor.
            /// </summary>
            public byte estadoVivo;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref tipo);
                serializer.SerializeValue(ref indice);
                serializer.SerializeValue(ref anclaX);
                serializer.SerializeValue(ref anclaY);
                serializer.SerializeValue(ref centroX);
                serializer.SerializeValue(ref centroY);
                serializer.SerializeValue(ref tamanoX);
                serializer.SerializeValue(ref tamanoY);
                serializer.SerializeValue(ref estadoVivo);
            }

            public bool Equals(EntradaMaquina o) =>
                tipo == o.tipo && indice == o.indice && anclaX == o.anclaX && anclaY == o.anclaY &&
                centroX == o.centroX && centroY == o.centroY && tamanoX == o.tamanoX && tamanoY == o.tamanoY &&
                estadoVivo == o.estadoVivo;
        }

        /// <summary>
        /// (ENCARGO N, playtest 43) Evento estático disparado EN AMBOS LADOS
        /// cuando cambia el <see cref="EntradaMaquina.estadoVivo"/> de una
        /// entrada del registro -- API congelada del contrato §1, la
        /// consume el ENCARGO A (audio del invitado) para disparar sus
        /// one-shots en las transiciones. En el anfitrión lo dispara
        /// <see cref="SondearEstadoVivo"/> justo al escribir el
        /// `NetworkList`; en el cliente lo dispara
        /// <see cref="MaquinaReplica.ActualizarDesdeRegistro"/> vía
        /// <see cref="NotificarCambioEstado"/> (un evento solo se puede
        /// invocar desde dentro de la clase que lo declara -- este método
        /// `internal` es la puerta para el otro archivo de este mismo
        /// encargo, mismo ensamblado, ver Alkahest.Runtime.asmdef).
        /// </summary>
        public static event System.Action<byte, byte, byte, byte> AlCambiarEstadoMaquina;

        internal static void NotificarCambioEstado(byte tipo, byte indice, byte antes, byte ahora) =>
            AlCambiarEstadoMaquina?.Invoke(tipo, indice, antes, ahora);

        /// <summary>
        /// DUDA-API (única de este archivo, en el mismo espíritu que
        /// Net/SimSync.cs con CustomMessagingManager): el template
        /// `Assets/FriendsLoop` no usa `NetworkList` en ningún sitio, así que
        /// no hay una llamada ya probada en el proyecto que calcar. El
        /// constructor con `readPerm`/`writePerm` nombrados replica
        /// literalmente el de `NetworkVariable&lt;T&gt;` que sí usan
        /// AprendizNet.IndiceColor y FriendsLoop.PlayerIdentity.DisplayName
        /// -- si NGO 2.13 expusiera una firma distinta, empezar a depurar
        /// aquí. El permiso es el mismo criterio que esos dos: todos leen,
        /// SOLO el servidor escribe (es la fuente de verdad del taller).
        /// </summary>
        private readonly NetworkList<EntradaMaquina> _registro = new NetworkList<EntradaMaquina>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server);

        /// <summary>
        /// (fix Cesar playtest 33, MULTI: "no es necesario que otros vean el
        /// movimiento; basta con que IMPIDA que otro mueva algo que alguien
        /// ya está moviendo, con un aviso; que vean el sitio final") EL
        /// CERROJO DE MUDANZA. Paralelo por ÍNDICE a <see cref="_registro"/>
        /// (mismo índice, crecen juntos en <see cref="PublicarRegistroInicial"/>):
        /// <see cref="SinBloqueo"/> = nadie lo lleva; cualquier otro valor es
        /// el `ClientId` de quien lo tiene agarrado ahora mismo. Escrito SOLO
        /// por el servidor (igual que <see cref="_registro"/>): el cliente
        /// que agarra algo PIDE el cerrojo (<see cref="PedirBloqueo"/>), no lo
        /// toma él mismo -- exactamente el mismo patrón de autoridad que ya
        /// usa la mudanza real (<see cref="SolicitarMudanzaRpc"/>).
        ///
        /// DELIBERADAMENTE NO sincroniza la POSICIÓN mientras se arrastra
        /// (decisión literal de Cesar, "no es necesario que otros vean el
        /// movimiento"): solo bloquea el gesto y avisa. Los demás ven el
        /// sitio FINAL cuando <see cref="SolicitarMudanzaRpc"/> confirma la
        /// mudanza, como siempre.
        /// </summary>
        private readonly NetworkList<ulong> _bloqueos = new NetworkList<ulong>(
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server);

        private const ulong SinBloqueo = ulong.MaxValue;

        // ---- HOST: la fuente cruda -----------------------------------------

        private struct Fuente
        {
            public byte tipo;
            public byte indice;
            public IMovible movible;
            public Vector2Int anclaAnterior; // último valor SONDEADO -- ver SondearCambiosDePosicion.
        }

        /// <summary>En paralelo por índice con <see cref="_registro"/> (host) -- ver PublicarRegistroInicial/SondearCambiosDePosicion.</summary>
        private readonly List<Fuente> _fuentes = new List<Fuente>(8);

        private bool _escaneado;

        private const float IntervaloSondeoSeg = 0.5f;
        private float _acumuladorSondeo;

        /// <summary>
        /// (ENCARGO N, playtest 43) Sondeo del ESTADO VIVO -- ~4Hz, el
        /// contrato §2b pide "jamás por frame" y da la cadencia exacta.
        /// Acumulador PROPIO, independiente del de posición (0.5s): el
        /// estado vivo (hornada en curso, brasero ardiendo...) cambia mucho
        /// más rápido que la posición de un aparato (que solo cambia por
        /// mudanza), así que compartir un único acumulador habría forzado a
        /// elegir entre "posición lenta y de sobra" o "estado a 2Hz", y
        /// ninguna de las dos cadencias es la que pide cada dato.
        /// </summary>
        private const float IntervaloEstadoSeg = 0.25f;
        private float _acumuladorEstado;

        // ---- CLIENTE: las réplicas ------------------------------------------

        /// <summary>En paralelo por índice con <see cref="_registro"/> (cliente) -- ver CrearOActualizarReplica.</summary>
        private readonly List<MaquinaReplica> _replicas = new List<MaquinaReplica>(8);

        private AlkahestSim _sim;

        // =================================================================
        // Ciclo de vida
        // =================================================================

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Debug.LogWarning("[ChaosAlchemy][Red] Ya existe un MaquinaSync en la escena; se destruye el duplicado.");
                Destroy(this);
                return;
            }

            Instancia = this;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _sim = FindAnyObjectByType<AlkahestSim>();
            _registro.OnListChanged += AlCambiarRegistro;

            if (!IsServer)
            {
                // Red de seguridad simétrica a SimSync/AprendizNet: si el
                // NetworkList ya llegó con su estado inicial completo ANTES
                // de que este método se ejecute (los NetworkVariable/List de
                // NGO se deserializan antes de invocar OnNetworkSpawn), un
                // invitado que se conecta TARDE no debe depender de que NGO
                // repita un evento Add por cada elemento preexistente --
                // reconstruimos a mano lo que ya haya.
                for (int i = 0; i < _registro.Count; i++) CrearOActualizarReplica(i, _registro[i]);
            }
        }

        public override void OnNetworkDespawn()
        {
            _registro.OnListChanged -= AlCambiarRegistro;
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            base.OnDestroy();
        }

        private void Update()
        {
            if (!IsSpawned || !IsServer || _sim == null) return;

            if (!_escaneado)
            {
                IntentarEscanear();
                return; // hasta que el escaneo tenga éxito no hay nada que sondear.
            }

            // (ENCARGO N, playtest 43) DOS acumuladores INDEPENDIENTES -- ver
            // el docblock de _acumuladorEstado para el porqué de no compartir
            // uno solo con la posición.
            _acumuladorSondeo += Time.deltaTime;
            if (_acumuladorSondeo >= IntervaloSondeoSeg)
            {
                _acumuladorSondeo -= IntervaloSondeoSeg; // resta, no reset a 0: evita deriva si un frame tarda más que el intervalo.
                SondearCambiosDePosicion();
            }

            _acumuladorEstado += Time.deltaTime;
            if (_acumuladorEstado >= IntervaloEstadoSeg)
            {
                _acumuladorEstado -= IntervaloEstadoSeg;
                SondearEstadoVivo();
            }
        }

        // =================================================================
        // ANFITRIÓN: descubrimiento (UNA vez, en cuanto exista qué escanear)
        // =================================================================

        /// <summary>
        /// AlkahestGameBootstrap.TrySpawnRed crea las siete máquinas en varias
        /// llamadas repartidas a lo largo de unos cuantos Update tras el
        /// spawn de este objeto (espera a que el avatar local esté cableado,
        /// ver ese archivo) -- así que un escaneo único en OnNetworkSpawn
        /// encontraría la escena vacía. Se reintenta aquí hasta que las
        /// SIETE existan (cinco estaciones + dos grifos): publicar un
        /// registro a medias no tiene arreglo después, porque el registro
        /// solo se llena UNA vez (ver PublicarRegistroInicial).
        /// </summary>
        private void IntentarEscanear()
        {
            var crisoles = FindObjectsByType<Crisol>();
            var prensas = FindObjectsByType<Prensa>();
            var chispas = FindObjectsByType<BancoChispa>();
            var columnas = FindObjectsByType<ColumnaEnsayo>();
            var ensayos = FindObjectsByType<EnsayoMaestro>();
            var grifos = FindObjectsByType<Dispenser>();
            // (fix Cesar playtest 33) Baldas/anclajes: los crea
            // Game/Mudanza.cs en su propio Init (host-only, ver el docblock
            // de ese archivo) -- igual que las siete de arriba, pueden tardar
            // uno o más Update en existir, así que este escaneo se reintenta
            // hasta que también estén. `>=1` basta como señal de "ya
            // terminó de crearlas": el spawn es una sola pasada síncrona
            // (Balda.SpawnTodas/Anclaje.SpawnDeposito), nunca a medias.
            var baldas = FindObjectsByType<Balda>();
            var anclajes = FindObjectsByType<Anclaje>();
            // (fix Cesar playtest 34) Estante de redomas + Alambique: los crea
            // AlkahestGameBootstrap.TrySpawnRed (ver ese archivo, fix de esta
            // misma ronda -- antes NUNCA se llamaban ahí, la causa raíz del
            // reporte "en multi no aparecen las redomas ni el alambique").
            // Pilas: las crea Game/Mudanza.cs, mismo sitio y mismo guardián
            // que Balda/Anclaje (ver Game/Pila.cs::SpawnTodas). Mismo criterio
            // de longitud fija que `grifos.Length < 2`: siempre hay
            // exactamente 1 estante, 1 alambique y 2 pilas (agua/limo).
            var estantes = FindObjectsByType<StorageRack>();
            var alambiques = FindObjectsByType<Alambique>();
            var pilas = FindObjectsByType<Pila>();

            if (crisoles.Length < 1 || prensas.Length < 1 || chispas.Length < 1 ||
                columnas.Length < 1 || ensayos.Length < 1 || grifos.Length < 2 ||
                baldas.Length < 1 || anclajes.Length < 1 ||
                estantes.Length < 1 || alambiques.Length < 1 || pilas.Length < 2)
            {
                return; // el taller del anfitrión sigue a mitad de construir -- se reintenta el próximo Update.
            }

            _fuentes.Clear();
            AgregarTipo(TipoMaquina.Crisol, crisoles);
            AgregarTipo(TipoMaquina.Prensa, prensas);
            AgregarTipo(TipoMaquina.BancoChispa, chispas);
            AgregarTipo(TipoMaquina.ColumnaEnsayo, columnas);
            AgregarTipo(TipoMaquina.EnsayoMaestro, ensayos);
            AgregarTipo(TipoMaquina.Dispenser, grifos);
            AgregarTipo(TipoMaquina.Balda, baldas);
            AgregarTipo(TipoMaquina.Anclaje, anclajes);
            AgregarTipo(TipoMaquina.Rack, estantes);
            AgregarTipo(TipoMaquina.Alambique, alambiques);
            AgregarTipo(TipoMaquina.Pila, pilas);

            PublicarRegistroInicial();
            _escaneado = true;
            Debug.Log("[ChaosAlchemy][Red] MaquinaSync: registro publicado (" + _fuentes.Count + " máquinas).");
        }

        /// <summary>
        /// Añade todas las instancias de un tipo a <see cref="_fuentes"/>,
        /// ordenadas por celda de anclaje (X, luego Y) para que el `indice`
        /// asignado sea DETERMINISTA -- importante sobre todo para los dos
        /// grifos, que si no podrían intercambiar de índice entre partidas
        /// según el orden (no documentado como estable) en que Unity los
        /// devuelva.
        /// </summary>
        private void AgregarTipo<T>(TipoMaquina tipo, T[] instancias) where T : Component, IMovible
        {
            System.Array.Sort(instancias, (a, b) =>
            {
                var ea = a.AnclaCelda; var eb = b.AnclaCelda;
                return ea.x != eb.x ? ea.x - eb.x : ea.y - eb.y;
            });

            for (int i = 0; i < instancias.Length; i++)
            {
                _fuentes.Add(new Fuente
                {
                    tipo = (byte)tipo,
                    indice = (byte)i,
                    movible = instancias[i],
                    anclaAnterior = instancias[i].AnclaCelda,
                });
            }
        }

        private void PublicarRegistroInicial()
        {
            _registro.Clear();
            _bloqueos.Clear();
            for (int i = 0; i < _fuentes.Count; i++)
            {
                _registro.Add(ConstruirEntrada(_fuentes[i], 0)); // estadoVivo=0: el primer sondeo (SondearEstadoVivo) lo pone al día en ≤0.25s.
                _bloqueos.Add(SinBloqueo); // nace libre -- ver el docblock de _bloqueos.
            }
        }

        /// <summary>
        /// (ENCARGO N, playtest 43) `estadoVivo` viaja como parámetro EXPLÍCITO
        /// en vez de leerse de `f` porque <see cref="Fuente"/> no lo guarda
        /// (el estado vivo no participa del sondeo de POSICIÓN, ver
        /// <see cref="SondearEstadoVivo"/>, que es quien de verdad lo escribe)
        /// -- los llamantes que reconstruyen una entrada por un cambio de
        /// posición (<see cref="SondearCambiosDePosicion"/>,
        /// <see cref="SolicitarMudanzaRpc"/>) tienen que PRESERVAR el último
        /// estado conocido pasándolo aquí, o una mudanza borraría en silencio
        /// "hornada en curso" del registro replicado.
        /// </summary>
        private static EntradaMaquina ConstruirEntrada(Fuente f, byte estadoVivo)
        {
            var ancla = f.movible.AnclaCelda;
            var centro = f.movible.CentroMundo;
            var tamano = f.movible.TamanoMundo;
            return new EntradaMaquina
            {
                tipo = f.tipo,
                indice = f.indice,
                anclaX = (ushort)Mathf.Clamp(ancla.x, 0, ushort.MaxValue),
                anclaY = (ushort)Mathf.Clamp(ancla.y, 0, ushort.MaxValue),
                centroX = centro.x,
                centroY = centro.y,
                tamanoX = tamano.x,
                tamanoY = tamano.y,
                estadoVivo = estadoVivo,
            };
        }

        // =================================================================
        // (ENCARGO N, playtest 43) ANFITRIÓN: sondeo del ESTADO VIVO (~4Hz,
        // acumulador -- ver IntervaloEstadoSeg). Solo escribe al registro
        // cuando el byte CAMBIA (regla dura del contrato §2b): un
        // NetworkList.Value gratis en el caso común (nada cambió) es la
        // misma disciplina que ya usa SondearCambiosDePosicion.
        // =================================================================
        private void SondearEstadoVivo()
        {
            for (int i = 0; i < _fuentes.Count && i < _registro.Count; i++)
            {
                // Balda/Anclaje/Rack/Pila (mobiliario, no de las 7 del
                // contrato) no implementan la interfaz -- se quedan en 0
                // para siempre, sin coste: un solo `as` por fuente, no una
                // lista aparte que mantener en paralelo.
                if (!(_fuentes[i].movible is IMaquinaUsableRemota usable)) continue;

                byte antes = _registro[i].estadoVivo;
                byte ahora = usable.EstadoVivoRed();
                if (ahora == antes) continue;

                var entrada = _registro[i];
                entrada.estadoVivo = ahora;
                _registro[i] = entrada; // dispara NetworkList.Value -> réplicas del invitado.

                NotificarCambioEstado(entrada.tipo, entrada.indice, antes, ahora); // §1: EN AMBOS LADOS -- este es el lado anfitrión.
            }
        }

        /// <summary>
        /// (ENCARGO N, playtest 43) Lectura pública del estado vivo -- API
        /// congelada del contrato §1, "válido en los dos lados". En el
        /// anfitrión lee directamente del registro que él mismo escribe; en
        /// el invitado lee la copia replicada por el NetworkList -- el mismo
        /// código sirve para ambos porque <see cref="_registro"/> es legible
        /// (readPerm Everyone) en los dos.
        /// </summary>
        public static bool TryGetEstado(byte tipo, byte indice, out byte estado)
        {
            estado = 0;
            var s = Instancia;
            if (s == null || !s.IsSpawned) return false;
            int i = s.BuscarIndiceRegistro(tipo, indice);
            if (i < 0 || i >= s._registro.Count) return false;
            estado = s._registro[i].estadoVivo;
            return true;
        }

        // =================================================================
        // ANFITRIÓN: sondeo de posición (0.5s, barato -- 5+2 aparatos)
        // =================================================================
        private void SondearCambiosDePosicion()
        {
            for (int i = 0; i < _fuentes.Count; i++)
            {
                var f = _fuentes[i];
                var comoObjeto = f.movible as UnityEngine.Object;
                if (comoObjeto == null) continue; // las siete máquinas viven toda la partida: no debería pasar, pero mejor no reventar el sondeo si pasara.

                var anclaActual = f.movible.AnclaCelda;
                if (anclaActual == f.anclaAnterior) continue;

                f.anclaAnterior = anclaActual;
                _fuentes[i] = f; // struct: hay que reescribir el elemento de la lista.

                // (ENCARGO N) preserva el estadoVivo ya conocido -- este sondeo es de POSICIÓN, no de estado.
                if (i < _registro.Count) _registro[i] = ConstruirEntrada(f, _registro[i].estadoVivo); // dispara la réplica en todos los clientes.
            }
        }

        // =================================================================
        // ANFITRIÓN: mudanza pedida por un invitado
        // =================================================================

        /// <summary>
        /// El anfitrión encuentra en <see cref="_fuentes"/> la máquina real
        /// que corresponde a (tipo, indice) y valida/ejecuta la mudanza --
        /// exactamente lo que haría su propio <see cref="Mudanza"/> al
        /// soltar, con la misma autoridad (<see cref="IMovible.CabeEnAncla"/>
        /// de la máquina real, nunca la aproximación del cliente).
        ///
        /// InvokePermission = Everyone: mismo criterio que
        /// Net/SimSync.SolicitarPinturaServerRpc -- el dueño de este objeto
        /// de escena es el servidor, así que sin este permiso ningún
        /// invitado podría llamarlo nunca.
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SolicitarMudanzaRpc(byte tipo, byte indice, ushort anclaX, ushort anclaY, ulong clienteId)
        {
            if (!IsServer) return;

            int i = BuscarFuente(tipo, indice);
            if (i < 0) return;

            // (fix Cesar playtest 33, MULTI: cierre de la carrera) autoridad
            // de verdad -- si otro cliente tiene el cerrojo de este aparato,
            // la petición se ignora en silencio. El cliente legítimo ya vio
            // el aviso local "lo está moviendo otro alquimista" ANTES de
            // poder llegar aquí en el caso normal (ver Game/Mudanza.cs,
            // IntentarAgarrar); esto solo cierra la carrera de dos gestos
            // casi simultáneos que el chequeo local no puede ver a tiempo.
            ulong bloqueoActual = i < _bloqueos.Count ? _bloqueos[i] : SinBloqueo;
            if (bloqueoActual != SinBloqueo && bloqueoActual != clienteId) return;

            var f = _fuentes[i];
            var comoObjeto = f.movible as UnityEngine.Object;
            if (comoObjeto == null) return;

            var anclaCandidata = new Vector2Int(anclaX, anclaY);
            if (!f.movible.CabeEnAncla(anclaCandidata))
            {
                // Rechazada: el registro NO cambia (sigue con el ancla vieja),
                // así que hay que avisar EXPLÍCITAMENTE -- si no, el fantasma
                // optimista del invitado (ver MaquinaReplica.Reposicionar) se
                // quedaría colgado en el sitio candidato para siempre, porque
                // nunca llegaría un cambio de NetworkList que lo corrigiera.
                MudanzaRechazadaRpc(tipo, indice);
                return;
            }

            f.movible.Reposicionar(anclaCandidata);

            // No se espera al sondeo de 0.5s: esto es la reacción directa a
            // un gesto del jugador, y la mano de Mudanza.IntentarSoltar ya
            // avisa "colocado" del lado del invitado que la pidió -- que el
            // registro tarde hasta medio segundo en ponerse al día se leería
            // como una réplica rota, no como una réplica lenta.
            f.anclaAnterior = anclaCandidata;
            _fuentes[i] = f;
            // (ENCARGO N) preserva el estadoVivo ya conocido -- una mudanza no cierra ninguna hornada en curso.
            if (i < _registro.Count) _registro[i] = ConstruirEntrada(f, _registro[i].estadoVivo);
        }

        private int BuscarFuente(byte tipo, byte indice)
        {
            for (int i = 0; i < _fuentes.Count; i++)
            {
                if (_fuentes[i].tipo == tipo && _fuentes[i].indice == indice) return i;
            }
            return -1;
        }

        /// <summary>
        /// Avisa a los invitados de que una mudanza se rechazó. Difusión a
        /// TODOS los clientes (no solo al que la pidió) en vez de un RPC
        /// dirigido a un único cliente: el proyecto no tiene ningún precedente
        /// de RPC a un cliente concreto (SimSync resuelve su equivalente con
        /// CustomMessagingManager, que sí soporta un destinatario único, pero
        /// aquí el volumen es minúsculo -- una mudanza rechazada de vez en
        /// cuando, nunca un chunk de sim) y un cliente que NO estuviera
        /// arrastrando esa máquina simplemente no encuentra una réplica que
        /// coincida y lo ignora (ver MaquinaReplica.AlRechazar). SendTo.NotServer:
        /// el anfitrión no tiene réplicas que corregir.
        /// </summary>
        [Rpc(SendTo.NotServer)]
        private void MudanzaRechazadaRpc(byte tipo, byte indice)
        {
            if (IsServer) return;
            for (int i = 0; i < _replicas.Count; i++)
            {
                var r = _replicas[i];
                if (r != null && r.Coincide(tipo, indice)) { r.AlRechazar(); return; }
            }
        }

        /// <summary>
        /// Wrapper público estático para que <see cref="MaquinaReplica"/> (un
        /// MonoBehaviour normal, no un NetworkBehaviour: no puede invocar un
        /// [Rpc] directamente) pida una mudanza. Mismo patrón que
        /// SimSync.ReenviarPintura. El `clientId` viaja como parámetro
        /// EXPLÍCITO del RPC (no `RpcParams.Receive.SenderClientId`): mismo
        /// criterio ya documentado en Net/SimSync.SolicitarSnapshotServerRpc
        /// -- este proyecto no tiene precedente de esa API y aquí tampoco hay
        /// nada que proteger de un cliente que mintiera sobre su propio id
        /// (como mucho conseguiría soltar su PROPIO cerrojo, nunca el de
        /// otro, porque <see cref="SolicitarMudanzaRpc"/> compara contra el
        /// cerrojo real).
        /// </summary>
        public static void PedirMudanza(byte tipo, byte indice, Vector2Int nuevaAncla)
        {
            var s = Instancia;
            if (s == null || !s.IsSpawned || s.IsServer) return;
            s.SolicitarMudanzaRpc(tipo, indice, (ushort)Mathf.Clamp(nuevaAncla.x, 0, ushort.MaxValue), (ushort)Mathf.Clamp(nuevaAncla.y, 0, ushort.MaxValue), s.NetworkManager.LocalClientId);
        }

        // =================================================================
        // (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §2a) E REMOTO: EL
        // INVITADO PIDE USAR UNA MÁQUINA
        // =================================================================

        /// <summary>
        /// Radio de validación de cordura del servidor -- "generoso, anti-
        /// teleuso, no precisión de píxel" (contrato §2a). Bastante por
        /// encima de cualquier `RangoFoco` real de las siete estaciones
        /// (2.6..4.0 celdas, ver sus archivos de Game/): esto no reemplaza el
        /// filtro de foco del invitado (que ya solo deja pulsar E cerca, ver
        /// MaquinaReplica), solo descarta el caso "un cliente llama al Rpc a
        /// mano sin estar ni remotamente cerca" -- un margen de sobra evita
        /// falsos rechazos mientras el avatar remoto todavía converge por
        /// Lerp (ver AprendizNet) o si la réplica local iba unos frames por
        /// delante de la posición real replicada del propio invitado.
        /// </summary>
        private const float RadioUsoRemotoCeldas = 14f;

        /// <summary>
        /// El invitado pide ejecutar el E de la máquina (tipo,indice). Mismo
        /// idioma Rpc que <see cref="SolicitarMudanzaRpc"/>
        /// (InvokePermission=Everyone: el dueño de este objeto de escena es
        /// el servidor). Autoridad completa del lado del servidor:
        /// <see cref="BuscarFuente"/> encuentra la máquina REAL, se valida la
        /// cercanía del avatar solicitante (anti-teleuso) y solo entonces se
        /// invoca <see cref="IMaquinaUsableRemota.UsarPorRed"/> -- exactamente
        /// la misma acción que dispararía el E local de esa máquina, sin el
        /// chequeo de proximidad del ANFITRIÓN (que no aplica aquí, ver el
        /// docblock de <see cref="IMaquinaUsableRemota"/>).
        /// </summary>
        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SolicitarUsoServerRpc(byte tipo, byte indice, ulong clienteId)
        {
            if (!IsServer) return;

            int i = BuscarFuente(tipo, indice);
            if (i < 0) return;

            if (!(_fuentes[i].movible is IMaquinaUsableRemota usable)) return; // tipo sin gancho remoto (mobiliario): nada que hacer.

            if (!AvatarCercaDe(clienteId, _fuentes[i].movible.CentroMundo, RadioUsoRemotoCeldas)) return;

            usable.UsarPorRed(); // el resultado (true/false) no viaja de vuelta: el propio estadoVivo replicado (§2b) y la chapa del invitado ya cuentan qué pasó, sin un segundo camino de red para un simple "no procedía".
        }

        /// <summary>
        /// ¿El avatar de `clienteId` está a ≤`radioCeldas` de `centroMundo`?
        /// <see cref="AprendizNet.Todos"/> es el mismo registro que ya usa
        /// Net/SimSync.cs para su propia pasada de prioridad por avatar (ver
        /// `ChunkCercaDeAlgunAvatar`) -- mismo criterio de "cerca de quién",
        /// aquí filtrado a UN avatar concreto en vez de "cualquiera". Un
        /// `clienteId` que no resuelve a ningún avatar vivo (desconexión a
        /// mitad de vuelo) rechaza por precaución: sin avatar no hay cordura
        /// que validar.
        /// </summary>
        private static bool AvatarCercaDe(ulong clienteId, Vector3 centroMundo, float radioCeldas)
        {
            var avatares = AprendizNet.Todos;
            for (int i = 0; i < avatares.Count; i++)
            {
                var a = avatares[i];
                if (a == null || a.OwnerClientId != clienteId) continue;
                float celda = SimRenderer.CellWorldSize;
                float distCeldas = Vector3.Distance(a.transform.position, centroMundo) / celda;
                return distCeldas <= radioCeldas;
            }
            return false;
        }

        /// <summary>
        /// Wrapper público estático para <see cref="MaquinaReplica"/> (mismo
        /// patrón que <see cref="PedirMudanza"/>: un MonoBehaviour normal no
        /// puede invocar un [Rpc] directamente). El anfitrión nunca lo llama
        /// (no tiene réplicas, tiene las máquinas reales con su propio E
        /// local).
        /// </summary>
        public static void PedirUso(byte tipo, byte indice)
        {
            var s = Instancia;
            if (s == null || !s.IsSpawned || s.IsServer) return;
            s.SolicitarUsoServerRpc(tipo, indice, s.NetworkManager.LocalClientId);
        }

        // =================================================================
        // (fix Cesar playtest 33, MULTI) EL CERROJO DE MUDANZA
        // =================================================================

        /// <summary>Índice compartido por <see cref="_registro"/> y <see cref="_bloqueos"/> para (tipo,indice), buscando en el propio NetworkList -- a diferencia de <see cref="BuscarFuente"/> (solo tiene sentido en el anfitrión, que es quien tiene <see cref="_fuentes"/>), esto funciona en los dos lados porque `_registro` está replicado a todo el mundo.</summary>
        private int BuscarIndiceRegistro(byte tipo, byte indice)
        {
            for (int i = 0; i < _registro.Count; i++)
                if (_registro[i].tipo == tipo && _registro[i].indice == indice) return i;
            return -1;
        }

        /// <summary>
        /// Resuelve (tipo,indice) para un <see cref="IMovible"/> concreto,
        /// sea el APARATO REAL (anfitrión, busca en <see cref="_fuentes"/>) o
        /// la RÉPLICA visual de un invitado (busca en <see cref="_replicas"/>,
        /// que es paralela por índice a <see cref="_registro"/> -- ver
        /// CrearOActualizarReplica). No hace falta ningún getter nuevo en
        /// Net/MaquinaReplica.cs (fuera del alcance de este encargo): basta
        /// con encontrar SU posición en la lista y leer tipo/indice del
        /// registro en ese MISMO índice.
        /// </summary>
        private bool ResolverTipoIndice(IMovible m, out byte tipo, out byte indice)
        {
            tipo = 0; indice = 0;
            if (m == null) return false;

            if (IsServer)
            {
                for (int i = 0; i < _fuentes.Count; i++)
                {
                    if (!ReferenceEquals(_fuentes[i].movible, m)) continue;
                    tipo = _fuentes[i].tipo; indice = _fuentes[i].indice;
                    return true;
                }
                return false;
            }

            for (int i = 0; i < _replicas.Count && i < _registro.Count; i++)
            {
                if (!ReferenceEquals(_replicas[i], m)) continue;
                tipo = _registro[i].tipo; indice = _registro[i].indice;
                return true;
            }
            return false;
        }

        /// <summary>
        /// ¿Lo tiene agarrado OTRO cliente ahora mismo? Consultado por
        /// Game/Mudanza.cs::IntentarAgarrar ANTES de dejar que el jugador se
        /// lo lleve. `m` que no resuelve a (tipo,indice) -- cualquier
        /// IMovible fuera del registro de MaquinaSync (HeatPlate/ChillStone/
        /// Criatura/Capullo, los únicos que quedan fuera tras el playtest 34
        /// -- StorageRack/Alambique/Pila SÍ están dentro desde esta ronda,
        /// ver <see cref="TipoMaquina"/>) -- siempre devuelve false: el
        /// cerrojo solo existe para los tipos que SÍ están en este registro
        /// (decisión documentada en el resumen del encargo).
        /// </summary>
        public static bool EstaBloqueadoPorOtro(IMovible m)
        {
            var s = Instancia;
            if (s == null || !s.IsSpawned) return false;
            if (!s.ResolverTipoIndice(m, out byte tipo, out byte indice)) return false;

            int i = s.BuscarIndiceRegistro(tipo, indice);
            if (i < 0 || i >= s._bloqueos.Count) return false;

            ulong ocupante = s._bloqueos[i];
            if (ocupante == SinBloqueo) return false;
            return ocupante != s.NetworkManager.LocalClientId;
        }

        /// <summary>Pide el cerrojo al agarrar. El anfitrión lo aplica directamente (nunca se manda un RPC a sí mismo, mismo criterio que <see cref="PedirMudanza"/>); un invitado lo pide por RPC.</summary>
        public static void PedirBloqueo(IMovible m)
        {
            var s = Instancia;
            if (s == null || !s.IsSpawned) return;
            if (!s.ResolverTipoIndice(m, out byte tipo, out byte indice)) return;

            ulong yo = s.NetworkManager.LocalClientId;
            if (s.IsServer) s.AplicarBloqueo(tipo, indice, yo);
            else s.SolicitarBloqueoRpc(tipo, indice, yo);
        }

        /// <summary>Libera el cerrojo al soltar/cancelar. Mismo criterio host/invitado que <see cref="PedirBloqueo"/>.</summary>
        public static void PedirLiberar(IMovible m)
        {
            var s = Instancia;
            if (s == null || !s.IsSpawned) return;
            if (!s.ResolverTipoIndice(m, out byte tipo, out byte indice)) return;

            ulong yo = s.NetworkManager.LocalClientId;
            if (s.IsServer) s.AplicarLiberacion(tipo, indice, yo);
            else s.SolicitarLiberarRpc(tipo, indice, yo);
        }

        private void AplicarBloqueo(byte tipo, byte indice, ulong clienteId)
        {
            int i = BuscarIndiceRegistro(tipo, indice);
            if (i < 0 || i >= _bloqueos.Count) return;
            if (_bloqueos[i] != SinBloqueo && _bloqueos[i] != clienteId) return; // ya lo tiene otro: se ignora, no se roba un cerrojo ajeno.
            _bloqueos[i] = clienteId;
        }

        private void AplicarLiberacion(byte tipo, byte indice, ulong clienteId)
        {
            int i = BuscarIndiceRegistro(tipo, indice);
            if (i < 0 || i >= _bloqueos.Count) return;
            if (_bloqueos[i] != clienteId) return; // solo quien lo agarró puede soltarlo.
            _bloqueos[i] = SinBloqueo;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SolicitarBloqueoRpc(byte tipo, byte indice, ulong clienteId)
        {
            if (!IsServer) return;
            AplicarBloqueo(tipo, indice, clienteId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SolicitarLiberarRpc(byte tipo, byte indice, ulong clienteId)
        {
            if (!IsServer) return;
            AplicarLiberacion(tipo, indice, clienteId);
        }

        // =================================================================
        // CLIENTE: recepción del registro -> réplicas
        // =================================================================

        private void AlCambiarRegistro(NetworkListEvent<EntradaMaquina> ev)
        {
            if (IsServer) return; // el anfitrión no construye réplicas de sus propias máquinas.

            switch (ev.Type)
            {
                case NetworkListEvent<EntradaMaquina>.EventType.Add:
                case NetworkListEvent<EntradaMaquina>.EventType.Value:
                    CrearOActualizarReplica(ev.Index, ev.Value);
                    break;
                // Insert/Remove/RemoveAt/Clear/Full no se usan: el registro
                // solo CRECE una vez al arrancar (PublicarRegistroInicial) y
                // luego solo cambia POR VALOR -- ninguna máquina se destruye
                // en mitad de una partida. Se ignoran a propósito.
            }
        }

        /// <summary>
        /// Crea la réplica en `index` si es la primera vez que se ve esa
        /// posición (siempre <c>index == _replicas.Count</c>, porque el
        /// registro solo añade al final) o actualiza su objetivo si ya
        /// existía. Idempotente a propósito: la red de seguridad de
        /// OnNetworkSpawn y los eventos Add en vivo pueden solaparse sin que
        /// eso duplique nada.
        /// </summary>
        private void CrearOActualizarReplica(int index, EntradaMaquina e)
        {
            if (index < _replicas.Count)
            {
                var existente = _replicas[index];
                if (existente != null) existente.ActualizarDesdeRegistro(e);
                return;
            }

            if (index != _replicas.Count)
            {
                Debug.LogWarning("[ChaosAlchemy][Red] MaquinaSync: hueco inesperado en el registro de máquinas (index " + index + ", esperaba " + _replicas.Count + "); se ignora la entrada.");
                return;
            }

            var go = new GameObject("MaquinaReplica_" + (TipoMaquina)e.tipo + "_" + e.indice);
            go.transform.SetParent(transform, false);
            var replica = go.AddComponent<MaquinaReplica>();
            replica.Inicializar(e);
            _replicas.Add(replica);
        }
    }
}
