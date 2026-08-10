namespace FriendsLoop.Networking
{
    /// <summary>
    /// Transporte de red a utilizar para la sesión: loopback local (mismo equipo, para pruebas)
    /// o Steam (a través de SteamNetworkingSockets, con NAT punch-through vía los relés de Steam).
    /// </summary>
    public enum TransportMode
    {
        LocalLoopback,
        Steam
    }
}
