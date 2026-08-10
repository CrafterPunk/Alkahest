using System;

namespace FriendsLoop.Voice
{
    /// <summary>
    /// Contrato mínimo para un proveedor de voz por chat (Steam Voice, Vivox, Dissonance, etc.).
    /// El template incluye únicamente NullVoiceService: esta interfaz es el punto de extensión
    /// para conectar un proveedor real más adelante sin tocar el resto del código de juego.
    /// </summary>
    public interface IVoiceService
    {
        /// <summary>True si el proveedor de voz está disponible y listo para usarse en esta plataforma/compilación.</summary>
        bool IsAvailable { get; }

        /// <summary>Se dispara cuando un jugador (identificado por su id, p.ej. SteamID) empieza o deja de hablar.</summary>
        event Action<ulong, bool> OnSpeakingChanged;

        /// <summary>Inicializa el proveedor de voz. Debe poder llamarse aunque el proveedor no esté disponible.</summary>
        void Initialize();

        /// <summary>Libera los recursos del proveedor de voz.</summary>
        void Shutdown();

        /// <summary>Une al jugador local al canal de voz asociado a una sesión (p.ej. id de lobby).</summary>
        void JoinSessionChannel(string sessionId);

        /// <summary>Saca al jugador local del canal de voz actual.</summary>
        void LeaveSessionChannel();

        /// <summary>Silencia o des-silencia el micrófono local.</summary>
        void SetMuted(bool muted);

        /// <summary>Activa o desactiva el modo "pulsar para hablar" del jugador local.</summary>
        void SetPushToTalkActive(bool active);
    }
}
