namespace FriendsLoop.Voice
{
    /// <summary>
    /// Localizador estático (service locator) del proveedor de voz activo.
    /// Por defecto usa <see cref="NullVoiceService"/>, así el template funciona sin ninguna
    /// dependencia de voz instalada.
    ///
    /// Punto de extensión futuro: cuando se quiera añadir voz real, crear una clase que implemente
    /// <see cref="IVoiceService"/> (por ejemplo SteamVoiceService usando ISteamUser.GetVoice /
    /// ISteamFriends.SetInGameVoiceSpeaking, o un wrapper de Vivox / Dissonance) y registrarla aquí
    /// con <see cref="Register"/> antes de que cualquier sistema de gameplay llame a <see cref="Current"/>
    /// (por ejemplo desde el Awake de un bootstrap de la escena de arranque). El resto del código de
    /// juego debe seguir consumiendo únicamente la interfaz IVoiceService, nunca el tipo concreto.
    /// </summary>
    public static class VoiceServices
    {
        private static IVoiceService s_Current = new NullVoiceService();

        /// <summary>Proveedor de voz activo actualmente. Nunca es null.</summary>
        public static IVoiceService Current => s_Current;

        /// <summary>
        /// Reemplaza el proveedor de voz activo. Pensado para llamarse una única vez, al arrancar el juego,
        /// antes de que otros sistemas empiecen a usar <see cref="Current"/>.
        /// </summary>
        public static void Register(IVoiceService service)
        {
            s_Current = service ?? new NullVoiceService();
        }
    }
}
