using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using FriendsLoop.Platform;

namespace FriendsLoop.Demo
{
    /// <summary>
    /// Identidad visible de un jugador en la escena de demostración: nombre sincronizado en red
    /// y una etiqueta de texto flotante sobre la cápsula que siempre mira hacia la cámara.
    /// Sirve como prueba mínima de un NetworkVariable de escritura exclusiva del servidor.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerIdentity : NetworkBehaviour
    {
        private static readonly Color OwnerLabelColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color RemoteLabelColor = Color.white;

        [SerializeField] private Vector3 labelLocalOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private int labelFontSize = 32;

        /// <summary>Nombre para mostrar del jugador. Solo el servidor puede escribirlo; todos pueden leerlo.</summary>
        public readonly NetworkVariable<FixedString64Bytes> DisplayName =
            new NetworkVariable<FixedString64Bytes>(
                default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private TextMesh m_LabelTextMesh;
        private Transform m_LabelTransform;
        private Camera m_MainCamera;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            EnsureLabel();
            DisplayName.OnValueChanged += HandleDisplayNameChanged;
            ApplyLabelText(DisplayName.Value.ToString());
            ApplyLabelColor();

            if (IsOwner)
            {
                string localName = ResolveLocalPlayerName();
                SetNameServerRpc(localName);
            }
        }

        public override void OnNetworkDespawn()
        {
            DisplayName.OnValueChanged -= HandleDisplayNameChanged;
            base.OnNetworkDespawn();
        }

        private string ResolveLocalPlayerName()
        {
            SteamBootstrap bootstrap = SteamBootstrap.Instance;
            if (bootstrap != null && bootstrap.IsSteamReady && !string.IsNullOrEmpty(bootstrap.LocalPlayerName))
            {
                return bootstrap.LocalPlayerName;
            }

            return "Jugador " + OwnerClientId;
        }

        [Rpc(SendTo.Server)]
        private void SetNameServerRpc(FixedString64Bytes newName)
        {
            DisplayName.Value = newName;
        }

        private void HandleDisplayNameChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
        {
            ApplyLabelText(newValue.ToString());
        }

        private void EnsureLabel()
        {
            if (m_LabelTransform != null)
            {
                return;
            }

            var labelGo = new GameObject("NameLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = labelLocalOffset;

            m_LabelTextMesh = labelGo.AddComponent<TextMesh>();
            m_LabelTextMesh.characterSize = 0.15f;
            m_LabelTextMesh.fontSize = labelFontSize;
            m_LabelTextMesh.anchor = TextAnchor.MiddleCenter;
            m_LabelTextMesh.alignment = TextAlignment.Center;

            m_LabelTransform = labelGo.transform;
        }

        private void ApplyLabelText(string text)
        {
            if (m_LabelTextMesh != null)
            {
                m_LabelTextMesh.text = string.IsNullOrEmpty(text) ? "Jugador" : text;
            }
        }

        private void ApplyLabelColor()
        {
            if (m_LabelTextMesh != null)
            {
                m_LabelTextMesh.color = IsOwner ? OwnerLabelColor : RemoteLabelColor;
            }
        }

        private void LateUpdate()
        {
            if (m_LabelTransform == null)
            {
                return;
            }

            if (m_MainCamera == null)
            {
                m_MainCamera = Camera.main;
                if (m_MainCamera == null)
                {
                    return;
                }
            }

            m_LabelTransform.rotation = m_MainCamera.transform.rotation;
        }
    }
}
