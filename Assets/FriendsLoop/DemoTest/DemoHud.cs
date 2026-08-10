using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using FriendsLoop.Networking;
using FriendsLoop.Platform;

namespace FriendsLoop.Demo
{
    /// <summary>
    /// HUD de demostración en IMGUI puro (sin Canvas), pensado para probar rápidamente
    /// host/unión local y por Steam, e inspeccionar el estado de la sesión. Todos los textos en español.
    /// </summary>
    public sealed class DemoHud : MonoBehaviour
    {
        [SerializeField] private SessionCoordinator sessionCoordinator;

        private Rect m_WindowRect = new Rect(10f, 10f, 340f, 360f);
        private string m_LobbyIdInput = string.Empty;

        private void Reset()
        {
            if (sessionCoordinator == null)
            {
                sessionCoordinator = FindAnyObjectByType<SessionCoordinator>();
            }
        }

        private void OnGUI()
        {
            m_WindowRect = GUILayout.Window(0x0F71D5, m_WindowRect, DrawWindow, "FriendsLoop - Demo");
        }

        private void DrawWindow(int windowId)
        {
            if (sessionCoordinator == null)
            {
                GUILayout.Label("No hay SessionCoordinator asignado en DemoHud.");
                GUI.DragWindow();
                return;
            }

            SessionCoordinator.ConnectionState state = sessionCoordinator.CurrentState;

            if (state == SessionCoordinator.ConnectionState.Offline || state == SessionCoordinator.ConnectionState.Starting)
            {
                DrawOfflineControls(state);
            }
            else
            {
                DrawConnectedControls(state);
            }

            if (!string.IsNullOrEmpty(sessionCoordinator.LastError))
            {
                GUILayout.Space(6f);
                GUILayout.Label("Error: " + sessionCoordinator.LastError);
            }

            GUI.DragWindow();
        }

        private void DrawOfflineControls(SessionCoordinator.ConnectionState state)
        {
            bool starting = state == SessionCoordinator.ConnectionState.Starting;

            GUILayout.Label(starting ? "Conectando..." : "Desconectado");
            GUI.enabled = !starting;

            if (GUILayout.Button("Host (Steam)"))
            {
                sessionCoordinator.StartHost(TransportMode.Steam);
            }

            if (GUILayout.Button("Host (Local)"))
            {
                sessionCoordinator.StartHost(TransportMode.LocalLoopback);
            }

            if (GUILayout.Button("Unirse (Local)"))
            {
                sessionCoordinator.JoinLocal();
            }

            GUILayout.Space(6f);
            GUILayout.Label("Id de lobby de Steam:");
            m_LobbyIdInput = GUILayout.TextField(m_LobbyIdInput);

            if (GUILayout.Button("Unirse (Steam)"))
            {
                if (ulong.TryParse(m_LobbyIdInput.Trim(), out ulong lobbyId))
                {
                    sessionCoordinator.JoinSteamLobby(lobbyId);
                }
            }

            GUI.enabled = true;
        }

        private void DrawConnectedControls(SessionCoordinator.ConnectionState state)
        {
            GUILayout.Label("Estado: " + state);
            GUILayout.Label("Transporte: " + sessionCoordinator.CurrentTransportMode);

            string localName = SteamBootstrap.Instance != null ? SteamBootstrap.Instance.LocalPlayerName : "Jugador";
            GUILayout.Label("Tu nombre: " + localName);

            bool isSteamHost = sessionCoordinator.CurrentTransportMode == TransportMode.Steam
                && state == SessionCoordinator.ConnectionState.Hosting;

            if (isSteamHost)
            {
                GUILayout.Label("Id de lobby (compártelo o usa el botón de invitar):");
                GUILayout.TextField(sessionCoordinator.CurrentLobbyId.ToString());

                if (GUILayout.Button("Invitar amigos (overlay)"))
                {
                    InviteFriendsViaOverlay();
                }
            }

            GUILayout.Space(6f);
            GUILayout.Label("Jugadores conectados:");
            foreach (string playerLabel in GetConnectedPlayerLabels())
            {
                GUILayout.Label(" - " + playerLabel);
            }

            GUILayout.Space(6f);
            if (GUILayout.Button("Alternar cubo"))
            {
                ToggleFirstInteractable();
            }

            GUILayout.Label("(E cerca del cubo también funciona)");

            GUILayout.Space(6f);
            if (GUILayout.Button("Desconectar"))
            {
                sessionCoordinator.Disconnect();
            }
        }

        private void InviteFriendsViaOverlay()
        {
            // SessionCoordinator no expone SteamLobbyService directamente por diseño (fachada estricta);
            // en la demo delegamos la apertura del overlay al propio servicio de lobby de la escena.
            var lobbyService = FindAnyObjectByType<FriendsLoop.Session.SteamLobbyService>();
            if (lobbyService != null)
            {
                lobbyService.InviteFriends();
            }
        }

        private void ToggleFirstInteractable()
        {
            SharedInteractable interactable = FindAnyObjectByType<SharedInteractable>();
            if (interactable != null)
            {
                interactable.RequestToggle();
            }
        }

        private static IEnumerable<string> GetConnectedPlayerLabels()
        {
            var labels = new List<string>();

            if (NetworkManager.Singleton == null)
            {
                return labels;
            }

            if (NetworkManager.Singleton.IsServer)
            {
                foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    string label = "Cliente " + client.ClientId;
                    if (client.PlayerObject != null && client.PlayerObject.TryGetComponent(out PlayerIdentity identity))
                    {
                        label = identity.DisplayName.Value.ToString();
                        if (string.IsNullOrEmpty(label))
                        {
                            label = "Cliente " + client.ClientId;
                        }
                    }

                    labels.Add(label);
                }
            }
            else
            {
                PlayerIdentity[] identities = Object.FindObjectsByType<PlayerIdentity>();
                foreach (PlayerIdentity identity in identities)
                {
                    string label = identity.DisplayName.Value.ToString();
                    labels.Add(string.IsNullOrEmpty(label) ? "Jugador" : label);
                }
            }

            return labels;
        }
    }
}
