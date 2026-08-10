using System;
using UnityEngine;

#if !DISABLESTEAMWORKS && STEAMWORKSNET
using Steamworks;
#endif

namespace FriendsLoop.Session
{
    /// <summary>
    /// Envoltorio delgado sobre ISteamMatchmaking / ISteamFriends para crear, unirse e invitar a lobbies de Steam.
    /// Es la única clase (junto a SteamBootstrap) que debe conocer los tipos de Steamworks relacionados con lobbies.
    /// SessionCoordinator consume esta clase a través de sus eventos públicos, nunca directamente los tipos de Steam.
    /// </summary>
    public sealed class SteamLobbyService : MonoBehaviour
    {
        [Tooltip("Identificador de juego guardado en los metadatos del lobby, útil para filtrar lobbies ajenos en el futuro.")]
        [SerializeField] private string gameKey = "com.friendsloop.template";

        private const string LobbyDataGameKey = "FL_GAME";
        private const string LobbyDataHostKey = "FL_HOST";

        /// <summary>Se dispara cuando el lobby propio terminó de crearse (éxito). Parámetro: id de lobby.</summary>
        public event Action<ulong> OnLobbyCreated;

        /// <summary>Se dispara cuando, como cliente, se entró a un lobby ajeno y ya se conoce el SteamID del host.</summary>
        public event Action<ulong> OnLobbyJoined;

        /// <summary>Se dispara cuando se abandonó el lobby actual.</summary>
        public event Action OnLobbyLeft;

        /// <summary>Se dispara ante cualquier fallo de matchmaking, con un mensaje legible en español.</summary>
        public event Action<string> OnLobbyError;

#if !DISABLESTEAMWORKS && STEAMWORKSNET
        /// <summary>Id del lobby actual. CSteamID.Nil si no hay ninguno activo.</summary>
        public CSteamID CurrentLobbyId { get; private set; } = CSteamID.Nil;

        private bool m_IsHosting;

        private Callback<LobbyCreated_t> m_LobbyCreatedCallback;
        private Callback<LobbyEnter_t> m_LobbyEnteredCallback;
        private Callback<GameLobbyJoinRequested_t> m_GameLobbyJoinRequestedCallback;

        private void Awake()
        {
            m_LobbyCreatedCallback = Callback<LobbyCreated_t>.Create(OnLobbyCreatedCallback);
            m_LobbyEnteredCallback = Callback<LobbyEnter_t>.Create(OnLobbyEnteredCallback);
            m_GameLobbyJoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequestedCallback);
        }

        private void OnDestroy()
        {
            m_LobbyCreatedCallback?.Dispose();
            m_LobbyEnteredCallback?.Dispose();
            m_GameLobbyJoinRequestedCallback?.Dispose();
        }

        /// <summary>Crea un lobby de Steam visible solo para amigos, con capacidad para maxPlayers jugadores.</summary>
        public void HostLobby(int maxPlayers)
        {
            if (!SteamManagerReady())
            {
                return;
            }

            m_IsHosting = true;
            Debug.Log("[FriendsLoop] Creando lobby de Steam (FriendsOnly) para " + maxPlayers + " jugadores...");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, maxPlayers);
        }

        /// <summary>Solicita unirse a un lobby de Steam existente a partir de su id de 64 bits.</summary>
        public void JoinLobby(ulong lobbyId)
        {
            if (!SteamManagerReady())
            {
                return;
            }

            m_IsHosting = false;
            Debug.Log("[FriendsLoop] Uniéndose al lobby de Steam " + lobbyId + "...");
            SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
        }

        /// <summary>Abandona el lobby actual, si hay alguno activo.</summary>
        public void LeaveLobby()
        {
            if (!SteamManagerReady())
            {
                return;
            }

            if (CurrentLobbyId.IsValid())
            {
                SteamMatchmaking.LeaveLobby(CurrentLobbyId);
                Debug.Log("[FriendsLoop] Lobby de Steam abandonado (" + CurrentLobbyId + ").");
            }

            CurrentLobbyId = CSteamID.Nil;
            m_IsHosting = false;
            OnLobbyLeft?.Invoke();
        }

        /// <summary>Abre el overlay de Steam para invitar amigos al lobby actual.</summary>
        public void InviteFriends()
        {
            if (!SteamManagerReady())
            {
                return;
            }

            if (!CurrentLobbyId.IsValid())
            {
                Debug.LogWarning("[FriendsLoop] No se puede invitar amigos: no hay un lobby activo.");
                return;
            }

            SteamFriends.ActivateGameOverlayInviteDialog(CurrentLobbyId);
        }

        private void OnLobbyCreatedCallback(LobbyCreated_t callback)
        {
            if (callback.m_eResult != EResult.k_EResultOK)
            {
                m_IsHosting = false;
                string msg = "No se pudo crear el lobby de Steam: " + callback.m_eResult;
                Debug.LogError("[FriendsLoop] " + msg);
                OnLobbyError?.Invoke(msg);
                return;
            }

            CurrentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

            ulong localSteamId = SteamUser.GetSteamID().m_SteamID;
            SteamMatchmaking.SetLobbyData(CurrentLobbyId, LobbyDataGameKey, gameKey);
            SteamMatchmaking.SetLobbyData(CurrentLobbyId, LobbyDataHostKey, localSteamId.ToString());

            Debug.Log("[FriendsLoop] Lobby de Steam creado correctamente: " + CurrentLobbyId);
            OnLobbyCreated?.Invoke(CurrentLobbyId.m_SteamID);
        }

        private void OnLobbyEnteredCallback(LobbyEnter_t callback)
        {
            var enteredLobbyId = new CSteamID(callback.m_ulSteamIDLobby);

            if (callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                string msg = "No se pudo entrar al lobby de Steam: " + (EChatRoomEnterResponse)callback.m_EChatRoomEnterResponse;
                Debug.LogError("[FriendsLoop] " + msg);
                OnLobbyError?.Invoke(msg);
                return;
            }

            CurrentLobbyId = enteredLobbyId;

            if (m_IsHosting)
            {
                // Este LobbyEnter_t corresponde a nuestro propio lobby recién creado (el host también "entra" a su lobby).
                return;
            }

            string hostIdText = SteamMatchmaking.GetLobbyData(CurrentLobbyId, LobbyDataHostKey);
            if (string.IsNullOrEmpty(hostIdText) || !ulong.TryParse(hostIdText, out ulong hostSteamId))
            {
                string msg = "El lobby de Steam no contiene datos de host válidos (" + LobbyDataHostKey + ").";
                Debug.LogError("[FriendsLoop] " + msg);
                OnLobbyError?.Invoke(msg);
                return;
            }

            Debug.Log("[FriendsLoop] Se entró al lobby de Steam " + CurrentLobbyId + ", host: " + hostSteamId);
            OnLobbyJoined?.Invoke(hostSteamId);
        }

        private void OnGameLobbyJoinRequestedCallback(GameLobbyJoinRequested_t callback)
        {
            Debug.Log("[FriendsLoop] Solicitud de unión a lobby recibida desde la lista de amigos de Steam: " + callback.m_steamIDLobby);
            JoinLobby(callback.m_steamIDLobby.m_SteamID);
        }

        private bool SteamManagerReady()
        {
            if (!SteamAPI.IsSteamRunning())
            {
                string msg = "El cliente de Steam no está en ejecución. No es posible usar lobbies de Steam.";
                Debug.LogError("[FriendsLoop] " + msg);
                OnLobbyError?.Invoke(msg);
                return false;
            }

            return true;
        }
#else
        /// <summary>Id del lobby actual (no disponible sin Steamworks.NET). Siempre 0.</summary>
        public ulong CurrentLobbyId => 0UL;

        public void HostLobby(int maxPlayers)
        {
            string msg = "Steamworks.NET no está disponible en esta compilación; no se puede crear un lobby de Steam.";
            Debug.LogWarning("[FriendsLoop] " + msg);
            OnLobbyError?.Invoke(msg);
        }

        public void JoinLobby(ulong lobbyId)
        {
            string msg = "Steamworks.NET no está disponible en esta compilación; no se puede unir a un lobby de Steam.";
            Debug.LogWarning("[FriendsLoop] " + msg);
            OnLobbyError?.Invoke(msg);
        }

        public void LeaveLobby()
        {
            OnLobbyLeft?.Invoke();
        }

        public void InviteFriends()
        {
            Debug.LogWarning("[FriendsLoop] Steamworks.NET no está disponible en esta compilación; no se puede invitar amigos.");
        }
#endif
    }
}
