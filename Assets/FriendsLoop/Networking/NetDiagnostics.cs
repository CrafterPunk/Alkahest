using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

#if !DISABLESTEAMWORKS && STEAMWORKSNET
using Steamworks;
#endif

namespace FriendsLoop.Networking
{
    /// <summary>
    /// Registro diagnóstico LIGERO, solo para desarrollo (editor y development builds).
    /// No modifica el comportamiento del networking: únicamente observa y escribe en el log
    /// la información necesaria para diagnosticar cortes de conexión que NGO o el transporte
    /// no reportan por defecto:
    ///  - cambios de estado de SteamNetworkingSockets con motivo de cierre
    ///    (m_eEndReason + m_szEndDebug de Valve, que el transporte vendorizado no registra);
    ///  - callbacks de conexión/desconexión de NGO y OnTransportFailure;
    ///  - SteamIDs remotos y relay POP;
    ///  - timestamp absoluto en cada línea (Player.log no lo incluye);
    ///  - sondeo periódico del estado real de cada conexión Steam
    ///    (ping, calidad local/remota, bytes pendientes) para saber si el transporte
    ///    todavía consideraba conectado al peer cuando algo se congela.
    /// En builds de release queda inactivo salvo que se marque enableInReleaseBuilds.
    /// </summary>
    public class NetDiagnostics : MonoBehaviour
    {
        [Tooltip("Activar también en builds de release (por defecto solo editor y development builds).")]
        [SerializeField] private bool enableInReleaseBuilds = false;

        [Tooltip("Cada cuántos segundos se registra el estado real de las conexiones Steam activas.")]
        [SerializeField] private float statusPollSeconds = 15f;

        private NetworkManager networkManager;
        private Coroutine pollRoutine;

        private bool Active => enableInReleaseBuilds || Debug.isDebugBuild || Application.isEditor;

        private static string Stamp()
        {
            return DateTime.Now.ToString("HH:mm:ss.fff");
        }

        private static void Log(string message)
        {
            Debug.Log("[FriendsLoop][DIAG " + Stamp() + "] " + message);
        }

        private void OnEnable()
        {
            if (!Active)
            {
                return;
            }

            networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                networkManager = NetworkManager.Singleton;
            }

            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback += HandleClientConnected;
                networkManager.OnClientDisconnectCallback += HandleClientDisconnected;
                networkManager.OnTransportFailure += HandleTransportFailure;
            }

#if !DISABLESTEAMWORKS && STEAMWORKSNET
            try
            {
                c_connectionStatus = Callback<SteamNetConnectionStatusChangedCallback_t>.Create(HandleSteamConnectionStatus);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[FriendsLoop][DIAG] No se pudo registrar el callback de SteamNetworkingSockets: " + ex.Message);
            }
#endif

            pollRoutine = StartCoroutine(PollConnectionStatus());
            Log("Diagnóstico de red activo (solo desarrollo).");
        }

        private void OnDisable()
        {
            if (networkManager != null)
            {
                networkManager.OnClientConnectedCallback -= HandleClientConnected;
                networkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
                networkManager.OnTransportFailure -= HandleTransportFailure;
            }

            if (pollRoutine != null)
            {
                StopCoroutine(pollRoutine);
                pollRoutine = null;
            }

#if !DISABLESTEAMWORKS && STEAMWORKSNET
            c_connectionStatus?.Dispose();
            c_connectionStatus = null;
            liveConnections.Clear();
#endif
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (networkManager == null)
            {
                return;
            }

            Log("NGO OnClientConnected: clientId=" + clientId
                + " | rol=" + DescribeRole()
                + " | clientesConectados=" + (networkManager.IsServer ? networkManager.ConnectedClientsList.Count.ToString() : "n/d"));
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            string reason = networkManager != null ? networkManager.DisconnectReason : string.Empty;
            Log("NGO OnClientDisconnect: clientId=" + clientId
                + " | rol=" + DescribeRole()
                + (string.IsNullOrEmpty(reason) ? " | motivo NGO: (vacío)" : " | motivo NGO: " + reason));
        }

        private void HandleTransportFailure()
        {
            Log("NGO OnTransportFailure: el transporte reportó un fallo irrecuperable. rol=" + DescribeRole());
        }

        private string DescribeRole()
        {
            if (networkManager == null)
            {
                return "sin NetworkManager";
            }

            if (networkManager.IsHost) return "host";
            if (networkManager.IsServer) return "server";
            if (networkManager.IsClient) return "client";
            return "offline";
        }

#if !DISABLESTEAMWORKS && STEAMWORKSNET
        private Callback<SteamNetConnectionStatusChangedCallback_t> c_connectionStatus;
        private readonly Dictionary<HSteamNetConnection, ulong> liveConnections = new Dictionary<HSteamNetConnection, ulong>();

        private void HandleSteamConnectionStatus(SteamNetConnectionStatusChangedCallback_t param)
        {
            ulong remoteId = param.m_info.m_identityRemote.GetSteamID64();
            ESteamNetworkingConnectionState newState = param.m_info.m_eState;

            string line = "SNS estado: " + param.m_eOldState + " -> " + newState
                + " | peer=" + remoteId
                + " | conn=" + param.m_hConn.m_HSteamNetConnection
                + " | relayPOP=" + param.m_info.m_idPOPRelay;

            switch (newState)
            {
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_Connected:
                    liveConnections[param.m_hConn] = remoteId;
                    break;

                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ClosedByPeer:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_ProblemDetectedLocally:
                case ESteamNetworkingConnectionState.k_ESteamNetworkingConnectionState_None:
                    liveConnections.Remove(param.m_hConn);
                    line += " | endReason=" + param.m_info.m_eEndReason
                        + " (" + (ESteamNetConnectionEnd)param.m_info.m_eEndReason + ")"
                        + " | endDebug=\"" + param.m_info.m_szEndDebug + "\"";
                    break;
            }

            Log(line);
        }
#endif

        private IEnumerator PollConnectionStatus()
        {
            var wait = new WaitForSecondsRealtime(Mathf.Max(5f, statusPollSeconds));
            while (true)
            {
                yield return wait;
#if !DISABLESTEAMWORKS && STEAMWORKSNET
                if (liveConnections.Count == 0)
                {
                    continue;
                }

                foreach (var pair in liveConnections)
                {
                    SteamNetConnectionRealTimeStatus_t status = default;
                    SteamNetConnectionRealTimeLaneStatus_t lanes = default;
                    EResult result = SteamNetworkingSockets.GetConnectionRealTimeStatus(pair.Key, ref status, 0, ref lanes);
                    if (result == EResult.k_EResultOK)
                    {
                        Log("SNS sondeo: peer=" + pair.Value
                            + " | estado=" + status.m_eState
                            + " | ping=" + status.m_nPing + "ms"
                            + " | calidadLocal=" + status.m_flConnectionQualityLocal.ToString("0.00")
                            + " | calidadRemota=" + status.m_flConnectionQualityRemote.ToString("0.00")
                            + " | pendReliable=" + status.m_cbPendingReliable
                            + " | sinAck=" + status.m_cbSentUnackedReliable);
                    }
                    else
                    {
                        Log("SNS sondeo: peer=" + pair.Value + " | GetConnectionRealTimeStatus devolvió " + result
                            + " (el transporte ya no considera viva esta conexión).");
                    }
                }
#endif
            }
        }
    }
}
