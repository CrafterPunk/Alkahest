using Unity.Netcode;
using UnityEngine;

namespace FriendsLoop.Demo
{
    /// <summary>
    /// Cubo compartido de demostración: cualquier jugador puede solicitar alternar su estado,
    /// pero el valor real vive únicamente en el servidor y se replica a todos los clientes.
    /// Sirve como prueba mínima de estado compartido autoritativo (NetworkVariable + Rpc).
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class SharedInteractable : NetworkBehaviour
    {
        private const float ActivatedHeightOffset = 0.75f;
        private static readonly Color ActivatedColor = Color.green;
        private static readonly Color DeactivatedColor = Color.red;

        /// <summary>Estado activado/desactivado del cubo. Solo el servidor puede escribirlo; todos pueden leerlo.</summary>
        public readonly NetworkVariable<bool> IsActivated =
            new NetworkVariable<bool>(
                false,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private Renderer m_Renderer;
        private MaterialPropertyBlock m_PropertyBlock;
        private Vector3 m_BaseLocalPosition;

        private void Awake()
        {
            m_Renderer = GetComponent<Renderer>();
            m_PropertyBlock = new MaterialPropertyBlock();
            m_BaseLocalPosition = transform.localPosition;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            IsActivated.OnValueChanged += HandleActivatedChanged;
            ApplyVisualState(IsActivated.Value);
        }

        public override void OnNetworkDespawn()
        {
            IsActivated.OnValueChanged -= HandleActivatedChanged;
            base.OnNetworkDespawn();
        }

        /// <summary>
        /// Solicita alternar el estado del cubo. Puede llamarse desde cualquier cliente;
        /// la decisión final la toma el servidor a través de ToggleServerRpc.
        /// </summary>
        public void RequestToggle()
        {
            if (!IsSpawned)
            {
                return;
            }

            ToggleServerRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ToggleServerRpc()
        {
            IsActivated.Value = !IsActivated.Value;
        }

        private void HandleActivatedChanged(bool previousValue, bool newValue)
        {
            ApplyVisualState(newValue);
        }

        private void ApplyVisualState(bool activated)
        {
            if (m_Renderer != null)
            {
                m_Renderer.GetPropertyBlock(m_PropertyBlock);
                m_PropertyBlock.SetColor("_BaseColor", activated ? ActivatedColor : DeactivatedColor);
                m_PropertyBlock.SetColor("_Color", activated ? ActivatedColor : DeactivatedColor);
                m_Renderer.SetPropertyBlock(m_PropertyBlock);
            }

            Vector3 localPosition = m_BaseLocalPosition;
            localPosition.y = m_BaseLocalPosition.y + (activated ? ActivatedHeightOffset : 0f);
            transform.localPosition = localPosition;
        }
    }
}
