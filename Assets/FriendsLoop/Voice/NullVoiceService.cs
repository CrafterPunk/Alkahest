using System;

namespace FriendsLoop.Voice
{
    /// <summary>
    /// Implementación nula de IVoiceService: no hace nada y reporta IsAvailable = false.
    /// Sirve como valor por defecto seguro mientras el template no incluye un proveedor de voz real.
    /// </summary>
    public sealed class NullVoiceService : IVoiceService
    {
        public bool IsAvailable => false;

        public event Action<ulong, bool> OnSpeakingChanged
        {
            add { /* no-op: nunca se dispara */ }
            remove { /* no-op */ }
        }

        public void Initialize() { }

        public void Shutdown() { }

        public void JoinSessionChannel(string sessionId) { }

        public void LeaveSessionChannel() { }

        public void SetMuted(bool muted) { }

        public void SetPushToTalkActive(bool active) { }
    }
}
