using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using FriendsLoop.Session;

#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
using Netcode.Transports;
#endif

namespace FriendsLoop.Networking
{
    /// <summary>
    /// Fachada pública única de red del template. La UI y el gameplay solo deben hablar con esta clase:
    /// nunca deben tocar NetworkManager, los transportes o SteamLobbyService directamente.
    /// Encapsula el arranque de host/cliente tanto en modo local (loopback, para pruebas) como en modo Steam.
    /// </summary>
    public sealed class SessionCoordinator : MonoBehaviour
    {
        /// <summary>Estado de conexión de alto nivel expuesto a la UI.</summary>
        public enum ConnectionState
        {
            Offline,
            Starting,
            Hosting,
            Client
        }

        private const string LocalHostAddress = "127.0.0.1";
        private const ushort LocalPort = 7777;

        [Header("Referencias de red")]
        [SerializeField] private NetworkManager networkManager;
        [SerializeField] private UnityTransport unityTransport;
#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
        [SerializeField] private SteamNetworkingSocketsTransport steamTransport;
#endif
        [SerializeField] private SteamLobbyService steamLobbyService;

        /// <summary>Estado de conexión actual.</summary>
        public ConnectionState CurrentState { get; private set; } = ConnectionState.Offline;

        /// <summary>Modo de transporte activo en la sesión actual.</summary>
        public TransportMode CurrentTransportMode { get; private set; } = TransportMode.LocalLoopback;

        /// <summary>Id del lobby de Steam actual, 0 si no aplica (modo local o sin sesión).</summary>
        public ulong CurrentLobbyId { get; private set; }

        /// <summary>Último mensaje de error legible, útil para mostrarlo en la UI. Cadena vacía si no hay error.</summary>
        public string LastError { get; private set; } = string.Empty;

        /// <summary>Se dispara cuando cambia el estado de conexión.</summary>
        public event Action<ConnectionState> OnStateChanged;

        /// <summary>Se dispara cuando ocurre un error de conexión o de sesión, con un mensaje en español.</summary>
        public event Action<string> OnError;

        /// <summary>
        /// Override de modo de transporte por defecto, leído de los argumentos de línea de comandos
        /// (-transport local | -transport steam). Útil para automatizar pruebas con dos instancias locales.
        /// Null si no se especificó ningún override válido.
        /// </summary>
        public TransportMode? CommandLineTransportOverride { get; private set; }

        private bool m_PendingHostAfterLobby;
        private int m_PendingHostMaxPlayers;

        private void Awake()
        {
            CommandLineTransportOverride = ParseTransportArg();

            if (steamLobbyService != null)
            {
#if !DISABLESTEAMWORKS && STEAMWORKSNET
                steamLobbyService.OnLobbyCreated += HandleLobbyCreated;
                steamLobbyService.OnLobbyJoined += HandleLobbyJoined;
                steamLobbyService.OnLobbyError += HandleSessionError;
#else
                steamLobbyService.OnLobbyError += HandleSessionError;
#endif
            }
        }

        private void OnEnable()
        {
            if (networkManager != null)
            {
                networkManager.OnClientDisconnectCallback += HandleClientDisconnect;
                networkManager.OnClientConnectedCallback += HandleClientConnected;
            }
        }

        private void OnDisable()
        {
            if (networkManager != null)
            {
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnect;
                networkManager.OnClientConnectedCallback -= HandleClientConnected;
            }
        }

        private void OnDestroy()
        {
            if (steamLobbyService != null)
            {
#if !DISABLESTEAMWORKS && STEAMWORKSNET
                steamLobbyService.OnLobbyCreated -= HandleLobbyCreated;
                steamLobbyService.OnLobbyJoined -= HandleLobbyJoined;
#endif
                steamLobbyService.OnLobbyError -= HandleSessionError;
            }
        }

        /// <summary>Devuelve el modo de transporte por defecto: el override de línea de comandos si existe, o el valor pasado como fallback.</summary>
        public TransportMode GetDefaultTransportMode(TransportMode fallback = TransportMode.LocalLoopback)
        {
            return CommandLineTransportOverride ?? fallback;
        }

        /// <summary>
        /// Inicia la sesión como host, en modo local (loopback en 127.0.0.1:7777) o modo Steam
        /// (crea un lobby y arranca el host cuando el lobby está listo).
        /// </summary>
        public void StartHost(TransportMode mode, int maxPlayers = 8)
        {
            if (networkManager == null)
            {
                RaiseError("No hay una referencia a NetworkManager configurada en SessionCoordinator.");
                return;
            }

            if (CurrentState != ConnectionState.Offline)
            {
                RaiseError("Ya hay una sesión activa; desconéctate antes de iniciar otra.");
                return;
            }

            LastError = string.Empty;
            CurrentTransportMode = mode;
            SetState(ConnectionState.Starting);

            switch (mode)
            {
                case TransportMode.LocalLoopback:
                    if (!AssignUnityTransport())
                    {
                        return;
                    }

                    // (playtest 42, hotfix del reporte de Cesar: "StartHost
                    // devolvió false" SIN decir por qué) DOS guardas de
                    // diagnóstico ANTES de intentar nada, porque el false de
                    // NGO es mudo y el jugador no puede actuar sobre "algo
                    // falló":
                    // 1) SESIÓN ANTERIOR A MEDIO CERRAR: si NGO sigue
                    //    escuchando aunque este coordinador se crea Offline
                    //    (un Shutdown() asíncrono que no terminó, o un error
                    //    a mitad de arranque), StartHost devolvería false.
                    //    Se cierra aquí y se pide UN reintento -- el Shutdown
                    //    de NGO tarda un frame, no se puede encadenar en la
                    //    misma llamada.
                    if (networkManager.IsListening || networkManager.IsServer || networkManager.IsClient)
                    {
                        networkManager.Shutdown();
                        RaiseError("Quedaba una sesión anterior a medio cerrar; ya la cerré -- pulsa ANFITRIÓN otra vez.");
                        SetState(ConnectionState.Offline);
                        return;
                    }

                    // 2) PUERTO OCUPADO: la causa más probable del false en
                    //    una prueba de dos ventanas -- la OTRA ventana ya es
                    //    anfitriona del 7777 (o un proceso viejo del juego lo
                    //    retiene). Se sondea con un bind UDP de usar-y-tirar
                    //    (UTP corre sobre UDP): barato, exacto, y convierte
                    //    un "false" mudo en una instrucción accionable.
                    if (!PuertoUdpLibre(LocalPort))
                    {
                        RaiseError("El puerto " + LocalPort + " ya está en uso. Si esta es tu SEGUNDA ventana, en esta pulsa UNIRME en local (solo una ventana puede ser ANFITRIÓN). Si no hay otra ventana, un proceso viejo del juego retiene el puerto: ciérralo desde el Administrador de tareas o reinicia el PC.");
                        SetState(ConnectionState.Offline);
                        return;
                    }

                    unityTransport.SetConnectionData(LocalHostAddress, LocalPort);
                    if (!networkManager.StartHost())
                    {
                        RaiseError("No se pudo iniciar el host local (StartHost devolvió false).");
                        SetState(ConnectionState.Offline);
                        return;
                    }

                    SetState(ConnectionState.Hosting);
                    Debug.Log("[FriendsLoop] Host local iniciado en " + LocalHostAddress + ":" + LocalPort);
                    SessionEvents.RaiseSessionCreated(0);
                    break;

                case TransportMode.Steam:
                    if (steamLobbyService == null)
                    {
                        RaiseError("No hay una referencia a SteamLobbyService configurada en SessionCoordinator.");
                        SetState(ConnectionState.Offline);
                        return;
                    }

                    m_PendingHostAfterLobby = true;
                    m_PendingHostMaxPlayers = maxPlayers;
                    steamLobbyService.HostLobby(maxPlayers);
                    break;
            }
        }

        /// <summary>Se une como cliente a un host local en 127.0.0.1:7777.</summary>
        public void JoinLocal()
        {
            if (networkManager == null)
            {
                RaiseError("No hay una referencia a NetworkManager configurada en SessionCoordinator.");
                return;
            }

            if (CurrentState != ConnectionState.Offline)
            {
                RaiseError("Ya hay una sesión activa; desconéctate antes de unirte a otra.");
                return;
            }

            LastError = string.Empty;
            CurrentTransportMode = TransportMode.LocalLoopback;
            SetState(ConnectionState.Starting);

            if (!AssignUnityTransport())
            {
                return;
            }

            unityTransport.SetConnectionData(LocalHostAddress, LocalPort);
            if (!networkManager.StartClient())
            {
                RaiseError("No se pudo iniciar el cliente local (StartClient devolvió false).");
                SetState(ConnectionState.Offline);
                return;
            }

            SetState(ConnectionState.Client);
            Debug.Log("[FriendsLoop] Cliente local conectando a " + LocalHostAddress + ":" + LocalPort);
        }

        /// <summary>Se une, como cliente, a un lobby de Steam existente a partir de su id.</summary>
        public void JoinSteamLobby(ulong lobbyId)
        {
            if (networkManager == null)
            {
                RaiseError("No hay una referencia a NetworkManager configurada en SessionCoordinator.");
                return;
            }

            if (steamLobbyService == null)
            {
                RaiseError("No hay una referencia a SteamLobbyService configurada en SessionCoordinator.");
                return;
            }

            if (CurrentState != ConnectionState.Offline)
            {
                RaiseError("Ya hay una sesión activa; desconéctate antes de unirte a otra.");
                return;
            }

            LastError = string.Empty;
            CurrentTransportMode = TransportMode.Steam;
            SetState(ConnectionState.Starting);
            steamLobbyService.JoinLobby(lobbyId);
        }

        /// <summary>Cierra la sesión actual (host o cliente) y abandona el lobby de Steam si corresponde.</summary>
        public void Disconnect()
        {
            if (networkManager != null && (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer))
            {
                networkManager.Shutdown();
                Debug.Log("[FriendsLoop] NetworkManager apagado por el usuario.");
            }

            if (steamLobbyService != null)
            {
                steamLobbyService.LeaveLobby();
            }

            CurrentLobbyId = 0;
            m_PendingHostAfterLobby = false;
            SetState(ConnectionState.Offline);
            SessionEvents.RaiseSessionLeft();
        }

        /// <summary>
        /// (playtest 42) ¿Está libre el puerto UDP dado en el loopback? Sonda
        /// de usar-y-tirar para diagnosticar el "StartHost devolvió false"
        /// mudo de NGO/UTP ANTES de intentarlo -- la consume solo
        /// <see cref="StartHost"/> en modo local. El socket se cierra en el
        /// finally: no retiene nada, y un bind UDP fallido con
        /// AddressAlreadyInUse es exactamente la firma de "otra ventana ya es
        /// anfitriona" (UTP corre sobre UDP, mismo protocolo que sondeamos).
        /// Cualquier OTRA excepción se trata como "libre": mejor dejar que
        /// UTP lo intente de verdad que bloquear el arranque por una sonda
        /// paranoica.
        /// </summary>
        private static bool PuertoUdpLibre(ushort puerto)
        {
            System.Net.Sockets.Socket sonda = null;
            try
            {
                sonda = new System.Net.Sockets.Socket(
                    System.Net.Sockets.AddressFamily.InterNetwork,
                    System.Net.Sockets.SocketType.Dgram,
                    System.Net.Sockets.ProtocolType.Udp);
                sonda.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, puerto));
                return true;
            }
            catch (System.Net.Sockets.SocketException e)
            {
                return e.SocketErrorCode != System.Net.Sockets.SocketError.AddressAlreadyInUse;
            }
            catch
            {
                return true;
            }
            finally
            {
                sonda?.Close();
            }
        }

        private bool AssignUnityTransport()
        {
            if (unityTransport == null)
            {
                RaiseError("No hay una referencia a UnityTransport configurada en SessionCoordinator.");
                SetState(ConnectionState.Offline);
                return false;
            }

            networkManager.NetworkConfig.NetworkTransport = unityTransport;
            return true;
        }

        private void HandleLobbyCreated(ulong lobbyId)
        {
            CurrentLobbyId = lobbyId;

            if (!m_PendingHostAfterLobby)
            {
                return;
            }

            m_PendingHostAfterLobby = false;

#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
            if (steamTransport == null)
            {
                RaiseError("No hay una referencia a SteamNetworkingSocketsTransport configurada en SessionCoordinator.");
                SetState(ConnectionState.Offline);
                return;
            }

            networkManager.NetworkConfig.NetworkTransport = steamTransport;
            if (!networkManager.StartHost())
            {
                RaiseError("No se pudo iniciar el host de Steam (StartHost devolvió false).");
                SetState(ConnectionState.Offline);
                return;
            }

            SetState(ConnectionState.Hosting);
            Debug.Log("[FriendsLoop] Host de Steam iniciado. Lobby: " + lobbyId);
            SessionEvents.RaiseSessionCreated(lobbyId);
#else
            RaiseError("Steamworks.NET no está disponible en esta compilación; no se puede completar el host de Steam.");
            SetState(ConnectionState.Offline);
#endif
        }

        private void HandleLobbyJoined(ulong hostSteamId)
        {
#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
            if (steamTransport == null)
            {
                RaiseError("No hay una referencia a SteamNetworkingSocketsTransport configurada en SessionCoordinator.");
                SetState(ConnectionState.Offline);
                return;
            }

            steamTransport.ConnectToSteamID = hostSteamId;
            networkManager.NetworkConfig.NetworkTransport = steamTransport;

            if (!networkManager.StartClient())
            {
                RaiseError("No se pudo iniciar el cliente de Steam (StartClient devolvió false).");
                SetState(ConnectionState.Offline);
                return;
            }

            CurrentLobbyId = steamLobbyService != null ? steamLobbyService.CurrentLobbyId.m_SteamID : 0;
            SetState(ConnectionState.Client);
            Debug.Log("[FriendsLoop] Cliente de Steam conectando al host " + hostSteamId);
#else
            RaiseError("Steamworks.NET no está disponible en esta compilación; no se puede completar la unión al lobby de Steam.");
            SetState(ConnectionState.Offline);
#endif
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (networkManager != null && networkManager.IsServer)
            {
                Debug.Log("[FriendsLoop] Cliente conectado: " + clientId);
            }
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (networkManager == null)
            {
                return;
            }

            // Si el ID que se desconectó es el nuestro (o ya no somos ni host ni cliente), la sesión terminó por completo.
            bool weAreStillConnected = networkManager.IsHost || networkManager.IsServer || networkManager.IsClient;
            bool weAreTheOneDisconnecting = clientId == networkManager.LocalClientId;

            if (weAreTheOneDisconnecting || !weAreStillConnected)
            {
                Debug.Log("[FriendsLoop] Sesión finalizada (desconexión local o del host).");
                if (steamLobbyService != null)
                {
                    steamLobbyService.LeaveLobby();
                }

                CurrentLobbyId = 0;
                SetState(ConnectionState.Offline);
                SessionEvents.RaiseSessionLeft();
            }
            else if (networkManager.IsServer)
            {
                Debug.Log("[FriendsLoop] Cliente desconectado: " + clientId);
            }
        }

        private void HandleSessionError(string message)
        {
            RaiseError(message);
            SetState(ConnectionState.Offline);
        }

        private void SetState(ConnectionState newState)
        {
            if (CurrentState == newState)
            {
                return;
            }

            CurrentState = newState;
            OnStateChanged?.Invoke(CurrentState);
        }

        private void RaiseError(string message)
        {
            LastError = message;
            Debug.LogError("[FriendsLoop] " + message);
            OnError?.Invoke(message);
            SessionEvents.RaiseSessionFailed(message);
        }

        private static TransportMode? ParseTransportArg()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-transport", StringComparison.OrdinalIgnoreCase))
                {
                    string value = args[i + 1].Trim().ToLowerInvariant();
                    if (value == "local")
                    {
                        return TransportMode.LocalLoopback;
                    }

                    if (value == "steam")
                    {
                        return TransportMode.Steam;
                    }
                }
            }

            return null;
        }
    }
}
