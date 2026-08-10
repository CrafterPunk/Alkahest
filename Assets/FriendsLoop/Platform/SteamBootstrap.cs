using UnityEngine;

#if !DISABLESTEAMWORKS && STEAMWORKSNET
using Steamworks;
#endif

namespace FriendsLoop.Platform
{
    /// <summary>
    /// Punto único de arranque y apagado de la capa nativa de Steamworks.
    /// Ninguna otra clase del proyecto debe llamar a SteamAPI.Init / RunCallbacks / Shutdown:
    /// todo pasa por este singleton para evitar inicializaciones duplicadas o llamadas fuera de orden.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class SteamBootstrap : MonoBehaviour
    {
        private static SteamBootstrap s_Instance;

        /// <summary>Instancia única del bootstrap. Null si aún no se ha creado o si la escena no lo incluye.</summary>
        public static SteamBootstrap Instance => s_Instance;

        /// <summary>True si SteamAPI se inicializó correctamente y está disponible para usarse.</summary>
        public bool IsSteamReady { get; private set; }

        /// <summary>SteamID local del jugador (64 bits), 0 si Steam no está listo.</summary>
        public ulong LocalSteamId { get; private set; }

        /// <summary>Nombre de perfil de Steam del jugador local, o un nombre por defecto si Steam no está disponible.</summary>
        public string LocalPlayerName { get; private set; } = "Jugador";

        private void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                Debug.LogWarning("[FriendsLoop] Ya existe una instancia de SteamBootstrap, destruyendo el duplicado.");
                Destroy(gameObject);
                return;
            }

            s_Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSteam();
        }

        private void InitializeSteam()
        {
#if !DISABLESTEAMWORKS && STEAMWORKSNET
            try
            {
                ESteamAPIInitResult initResult = SteamAPI.InitEx(out string steamErrorMsg);
                if (initResult != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    IsSteamReady = false;
                    Debug.LogWarning(
                        "[FriendsLoop] No se pudo inicializar Steamworks (" + initResult + "): " + steamErrorMsg +
                        ". Verifica que el cliente de Steam esté abierto y que exista steam_appid.txt junto al ejecutable. " +
                        "La plantilla seguirá funcionando en modo LocalLoopback sin Steam.");
                    return;
                }

                IsSteamReady = true;
                LocalSteamId = SteamUser.GetSteamID().m_SteamID;
                LocalPlayerName = SteamFriends.GetPersonaName();
                Debug.Log("[FriendsLoop] Steamworks inicializado correctamente. Jugador: " + LocalPlayerName + " (" + LocalSteamId + ")");
            }
            catch (System.Exception ex)
            {
                IsSteamReady = false;
                Debug.LogWarning("[FriendsLoop] Excepción al inicializar Steamworks: " + ex.Message +
                    ". La plantilla seguirá funcionando en modo LocalLoopback sin Steam.");
            }
#else
            IsSteamReady = false;
            Debug.Log("[FriendsLoop] Steamworks.NET no está disponible en esta compilación (definir STEAMWORKSNET). Modo solo LocalLoopback.");
#endif
        }

        private void Update()
        {
#if !DISABLESTEAMWORKS && STEAMWORKSNET
            if (IsSteamReady)
            {
                SteamAPI.RunCallbacks();
            }
#endif
        }

        private void ShutdownSteam()
        {
#if !DISABLESTEAMWORKS && STEAMWORKSNET
            if (IsSteamReady)
            {
                SteamAPI.Shutdown();
                IsSteamReady = false;
                Debug.Log("[FriendsLoop] Steamworks apagado correctamente.");
            }
#endif
        }

        private void OnDestroy()
        {
            if (s_Instance == this)
            {
                ShutdownSteam();
                s_Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            ShutdownSteam();
        }
    }
}
