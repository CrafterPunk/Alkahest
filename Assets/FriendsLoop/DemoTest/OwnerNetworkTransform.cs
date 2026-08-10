using Unity.Netcode.Components;

namespace FriendsLoop.Demo
{
    /// <summary>
    /// NetworkTransform con autoridad del propietario (owner-authoritative) en lugar de autoridad del servidor.
    /// Necesario para que cada jugador mueva su propio personaje localmente sin depender de un tick del servidor.
    /// </summary>
    public sealed class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
