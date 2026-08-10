using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FriendsLoop.Demo
{
    /// <summary>
    /// Movimiento simple de demostración: solo el propietario del NetworkObject se mueve.
    /// Usa exclusivamente el nuevo Input System (Keyboard.current); nunca UnityEngine.Input.
    /// También permite alternar el cubo compartido más cercano (SharedInteractable) con la tecla E.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerController : NetworkBehaviour
    {
        private const float MoveSpeed = 4f;
        private const float PlaneBounds = 12f;
        private const float InteractRange = 3f;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            enabled = IsOwner;
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 input = Vector2.zero;

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                input.y += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                input.y -= 1f;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                input.x -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                input.x += 1f;
            }

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            if (input.sqrMagnitude > 0f)
            {
                Vector3 delta = new Vector3(input.x, 0f, input.y) * (MoveSpeed * Time.deltaTime);
                transform.Translate(delta, Space.World);

                Vector3 position = transform.position;
                position.x = Mathf.Clamp(position.x, -PlaneBounds, PlaneBounds);
                position.z = Mathf.Clamp(position.z, -PlaneBounds, PlaneBounds);
                transform.position = position;
            }

            if (keyboard.eKey.wasPressedThisFrame)
            {
                TryToggleNearestInteractable();
            }
        }

        private void TryToggleNearestInteractable()
        {
            SharedInteractable[] interactables = Object.FindObjectsByType<SharedInteractable>();

            SharedInteractable nearest = null;
            float nearestSqrDistance = InteractRange * InteractRange;

            foreach (SharedInteractable candidate in interactables)
            {
                if (candidate == null)
                {
                    continue;
                }

                float sqrDistance = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sqrDistance <= nearestSqrDistance)
                {
                    nearestSqrDistance = sqrDistance;
                    nearest = candidate;
                }
            }

            if (nearest != null)
            {
                nearest.RequestToggle();
            }
        }
    }
}
