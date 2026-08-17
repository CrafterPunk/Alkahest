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
        /// <summary>Mismo orden que las constantes Tipo* de MaquinariaSprites.ConstruirVisualEstatico (ver el docblock de esas constantes para el porqué de la duplicación).</summary>
        public enum TipoMaquina : byte
        {
            Crisol = 0,
            Prensa = 1,
            BancoChispa = 2,
            ColumnaEnsayo = 3,
            EnsayoMaestro = 4,
            Dispenser = 5,
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
            }

            public bool Equals(EntradaMaquina o) =>
                tipo == o.tipo && indice == o.indice && anclaX == o.anclaX && anclaY == o.anclaY &&
                centroX == o.centroX && centroY == o.centroY && tamanoX == o.tamanoX && tamanoY == o.tamanoY;
        }

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

            _acumuladorSondeo += Time.deltaTime;
            if (_acumuladorSondeo < IntervaloSondeoSeg) return;
            _acumuladorSondeo -= IntervaloSondeoSeg; // resta, no reset a 0: evita deriva si un frame tarda más que el intervalo.
            SondearCambiosDePosicion();
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

            if (crisoles.Length < 1 || prensas.Length < 1 || chispas.Length < 1 ||
                columnas.Length < 1 || ensayos.Length < 1 || grifos.Length < 2)
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
            for (int i = 0; i < _fuentes.Count; i++)
            {
                _registro.Add(ConstruirEntrada(_fuentes[i]));
            }
        }

        private static EntradaMaquina ConstruirEntrada(Fuente f)
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
            };
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

                if (i < _registro.Count) _registro[i] = ConstruirEntrada(f); // dispara la réplica en todos los clientes.
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
        private void SolicitarMudanzaRpc(byte tipo, byte indice, ushort anclaX, ushort anclaY)
        {
            if (!IsServer) return;

            int i = BuscarFuente(tipo, indice);
            if (i < 0) return;

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
            if (i < _registro.Count) _registro[i] = ConstruirEntrada(f);
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

        /// <summary>Wrapper público estático para que <see cref="MaquinaReplica"/> (un MonoBehaviour normal, no un NetworkBehaviour: no puede invocar un [Rpc] directamente) pida una mudanza. Mismo patrón que SimSync.ReenviarPintura.</summary>
        public static void PedirMudanza(byte tipo, byte indice, Vector2Int nuevaAncla)
        {
            var s = Instancia;
            if (s == null || !s.IsSpawned || s.IsServer) return;
            s.SolicitarMudanzaRpc(tipo, indice, (ushort)Mathf.Clamp(nuevaAncla.x, 0, ushort.MaxValue), (ushort)Mathf.Clamp(nuevaAncla.y, 0, ushort.MaxValue));
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
