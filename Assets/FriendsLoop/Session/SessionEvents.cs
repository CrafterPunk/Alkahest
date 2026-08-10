using System;

namespace FriendsLoop.Session
{
    /// <summary>
    /// Información de visualización de un jugador para uso en UI (nombre, id, si es el jugador local).
    /// Estructura plana en C# puro, sin dependencias de Steamworks ni de Netcode.
    /// </summary>
    public readonly struct PlayerDisplayInfo
    {
        public readonly ulong PlayerId;
        public readonly string DisplayName;
        public readonly bool IsLocal;

        public PlayerDisplayInfo(ulong playerId, string displayName, bool isLocal)
        {
            PlayerId = playerId;
            DisplayName = displayName;
            IsLocal = isLocal;
        }
    }

    /// <summary>
    /// Bus de eventos de sesión, independiente de la plataforma de red concreta.
    /// La UI y la lógica de juego se suscriben aquí en lugar de acoplarse a Steamworks o a Netcode directamente.
    /// </summary>
    public static class SessionEvents
    {
        /// <summary>Se dispara cuando se creó una sesión (host) correctamente. Parámetro: id de lobby (0 si es local).</summary>
        public static event Action<ulong> OnSessionCreated;

        /// <summary>Se dispara cuando el jugador local se unió a una sesión existente. Parámetro: id de lobby (0 si es local).</summary>
        public static event Action<ulong> OnSessionJoined;

        /// <summary>Se dispara cuando el jugador local abandonó la sesión, ya sea voluntariamente o por desconexión.</summary>
        public static event Action OnSessionLeft;

        /// <summary>Se dispara cuando falló la creación o unión a una sesión. Parámetro: mensaje de error legible.</summary>
        public static event Action<string> OnSessionFailed;

        public static void RaiseSessionCreated(ulong lobbyId) => OnSessionCreated?.Invoke(lobbyId);
        public static void RaiseSessionJoined(ulong lobbyId) => OnSessionJoined?.Invoke(lobbyId);
        public static void RaiseSessionLeft() => OnSessionLeft?.Invoke();
        public static void RaiseSessionFailed(string reason) => OnSessionFailed?.Invoke(reason);
    }
}
