using UnityEngine;
using Alkahest.Game;
using FriendsLoop.Networking;
using FriendsLoop.Platform;

namespace Alkahest.Net
{
    /// <summary>
    /// EL PANEL DE SESIÓN (playtest 28, POC multiplayer): IMGUI puro, con los
    /// estilos del taller (<see cref="UiStyles"/>), para abrir la partida a
    /// amigos y ver quién hay dentro.
    ///
    /// REGLA DEL TEMPLATE, RESPETADA AL PIE DE LA LETRA: este HUD habla SOLO
    /// con <see cref="SessionCoordinator"/>. No toca el NetworkManager, ni los
    /// transportes, ni SteamLobbyService — esa es la fachada única de red del
    /// template (ver su docblock) y el precio de saltársela es tener la lógica
    /// de conexión repartida por la UI. Lo único que se lee fuera de la
    /// fachada son cosas de PRESENTACIÓN: el nombre de Steam del jugador local
    /// (`SteamBootstrap`) y el registro de avatares vivos
    /// (<see cref="AprendizNet.Todos"/>) para pintar la lista de colores.
    ///
    /// Se esconde solo en cuanto la partida está en marcha y hay más de un
    /// jugador: el taller es el juego, no el panel. Se vuelve a abrir con F9.
    /// </summary>
    public sealed class TallerSesionHud : MonoBehaviour
    {
        /// <summary>(fix del atasco) Aviso persistente de la caída a modo local sin Steam -- se muestra en el panel hasta que la sesión arranca.</summary>
        private string _avisoLocal;
        private float _avisoLocalHasta; // (fix legibilidad) el aviso caduca solo y desaparece al arrancar la sesión.
        private const int VentanaId = 0x414C4B4E; // "ALKN"

        /// <summary>Jugadores máximos del lobby. Cuatro: el mandato de Cesar para este POC.</summary>
        private const int MaxJugadores = 4;

        [SerializeField] private SessionCoordinator sessionCoordinator;

        private Rect _ventana = new Rect(16f, 16f, 330f, 260f);
        private string _lobbyIdEscrito = "";
        private bool _plegado;
        private bool _autoPlegadoHecho;

        private void Reset()
        {
            if (sessionCoordinator == null) sessionCoordinator = FindAnyObjectByType<SessionCoordinator>();
        }

        private void Awake()
        {
            if (sessionCoordinator == null) sessionCoordinator = FindAnyObjectByType<SessionCoordinator>();
        }

        private void Update()
        {
            // F9 alterna el panel. Es un atajo de META (como M de silenciar,
            // regla 12 de CLAUDE.md), no una acción de juego: solo respeta
            // "estoy escribiendo un nombre", nada más.
            var teclado = UnityEngine.InputSystem.Keyboard.current;
            if (teclado != null && teclado.f9Key.wasPressedThisFrame && !UiStyles.EscribiendoTexto)
            {
                _plegado = !_plegado;
            }

            // Autoplegado: en cuanto hay al menos dos aprendices en el taller,
            // el panel ya cumplió su función y estorba. Una sola vez — si el
            // jugador lo reabre con F9, se queda abierto.
            if (!_autoPlegadoHecho && AprendizNet.Todos.Count >= 2)
            {
                _autoPlegadoHecho = true;
                _plegado = true;
            }
        }

        private void OnGUI()
        {
            UiStyles.Preparar();

            if (_plegado)
            {
                var r = new Rect(16f, 16f, UiStyles.S(220f), UiStyles.S(24f));
                UiStyles.Panel(r);
                GUI.Label(new Rect(r.x + UiStyles.S(8f), r.y, r.width, r.height),
                    "F9 · sesión (" + AprendizNet.Todos.Count + " en el taller)", UiStyles.CuerpoTenue);
                return;
            }

            _ventana.width = UiStyles.S(330f);
            _ventana = GUILayout.Window(VentanaId, _ventana, DibujarVentana, "EL TALLER COMPARTIDO");
        }

        private void DibujarVentana(int id)
        {
            if (sessionCoordinator == null)
            {
                GUILayout.Label("Falta el SessionCoordinator en la escena.", UiStyles.Cuerpo);
                GUI.DragWindow();
                return;
            }

            SessionCoordinator.ConnectionState estado = sessionCoordinator.CurrentState;

            switch (estado)
            {
                case SessionCoordinator.ConnectionState.Offline:
                case SessionCoordinator.ConnectionState.Starting:
                    DibujarDesconectado(estado);
                    break;
                default:
                    DibujarConectado(estado);
                    break;
            }

            if (!string.IsNullOrEmpty(_avisoLocal) && Time.time < _avisoLocalHasta)
            {
                GUILayout.Space(4f);
                // Cuerpo (no CuerpoTenue): Cesar reportó que "apenas se lee".
                GUILayout.Label(_avisoLocal, UiStyles.Cuerpo);
                if (GUILayout.Button("entendido", UiStyles.Boton)) _avisoLocal = null;
            }
            if (!string.IsNullOrEmpty(sessionCoordinator.LastError))
            {
                GUILayout.Space(6f);
                GUILayout.Label("Algo falló: " + sessionCoordinator.LastError, UiStyles.CuerpoTenue);
            }

            GUILayout.Space(4f);
            GUILayout.Label("F9 esconde este panel.", UiStyles.CuerpoTenue);

            GUI.DragWindow();
        }

        private void DibujarDesconectado(SessionCoordinator.ConnectionState estado)
        {
            bool arrancando = estado == SessionCoordinator.ConnectionState.Starting;

            GUILayout.Label(arrancando ? "Abriendo el taller..." : "Todavía trabajas solo.", UiStyles.Cuerpo);
            GUILayout.Space(4f);

            GUI.enabled = !arrancando;

            // ANFITRIÓN: abre el taller. El modo de transporte por defecto lo
            // decide el propio coordinador leyendo `-transport local|steam` de
            // la línea de comandos (así se prueban dos instancias en el mismo
            // PC sin tocar la UI); si no hay override, Steam, que es como se
            // juega con amigos de verdad.
            if (GUILayout.Button("ANFITRIÓN — abre tu taller (hasta 4)", UiStyles.Boton))
            {
                // (fix del atasco de Cesar) Si Steam NO está abierto, este
                // botón moría con un error y "no ocurría nada": ahora cae
                // SOLO a taller local -- jugar en solitario dándole a
                // ANFITRIÓN es un camino válido. El aviso queda a la vista.
                var modo = sessionCoordinator.GetDefaultTransportMode(TransportMode.Steam);
                bool steamListo = FriendsLoop.Platform.SteamBootstrap.Instance != null
                    && FriendsLoop.Platform.SteamBootstrap.Instance.IsSteamReady;
                if (modo == TransportMode.Steam && !steamListo)
                {
                    modo = TransportMode.LocalLoopback;
                    // (fix, reporte de Cesar: "apenas se lee" + "sí estoy
                    // conectado") El diagnóstico real puede ser DOS cosas:
                    // cliente cerrado O steam_appid.txt ausente junto al exe
                    // (el caso de Cesar: Steam abierto y aun así Init falla).
                    // El texto nombra ambas, se dibuja GRANDE y caduca solo.
                    _avisoLocal = "Steam no respondió (cliente cerrado, o falta steam_appid.txt junto al .exe).\nAbrí tu taller en modo LOCAL: puedes jugar; para amigos, rehaz la build o abre Steam y reinicia.";
                    _avisoLocalHasta = Time.time + 14f;
                }
                sessionCoordinator.StartHost(modo, MaxJugadores);
            }

            if (GUILayout.Button("UNIRME al taller de un amigo (Steam)", UiStyles.Boton))
            {
                if (ulong.TryParse(_lobbyIdEscrito.Trim(), out ulong lobbyId) && lobbyId != 0UL)
                {
                    sessionCoordinator.JoinSteamLobby(lobbyId);
                }
                else
                {
                    // Sin id escrito no hay nada que hacer aquí: en Steam la
                    // vía normal es la invitación del overlay, que entra sola
                    // por SteamLobbyService (GameLobbyJoinRequested).
                    Debug.LogWarning("[ChaosAlchemy][Red] Pega primero el id del lobby de tu amigo, o acepta su invitación desde Steam.");
                }
            }

            GUILayout.Label("Id de lobby del anfitrión:", UiStyles.CuerpoTenue);
            _lobbyIdEscrito = GUILayout.TextField(_lobbyIdEscrito, UiStyles.Campo);

            GUILayout.Space(4f);
            GUILayout.Label("Prueba local (dos ventanas en este PC):", UiStyles.CuerpoTenue);
            // (fix del smoke test en el editor, playtest 28) ANFITRIÓN LOCAL:
            // el botón ANFITRIÓN de arriba usa Steam (correcto para la prueba
            // con amigos), pero la prueba de DOS VENTANAS en un mismo PC no
            // puede usar Steam (un solo cliente por cuenta) -- y en el editor
            // no hay -transport local. Sin este botón, el primer arranque
            // real del POC murió con "el cliente de Steam no está en
            // ejecución". El anfitrión local usa loopback puro.
            if (GUILayout.Button("ANFITRIÓN en local (127.0.0.1)", UiStyles.Boton))
            {
                sessionCoordinator.StartHost(TransportMode.LocalLoopback, MaxJugadores);
            }
            if (GUILayout.Button("UNIRME en local (127.0.0.1)", UiStyles.Boton))
            {
                sessionCoordinator.JoinLocal();
            }

            GUI.enabled = true;
        }

        private void DibujarConectado(SessionCoordinator.ConnectionState estado)
        {
            bool anfitrion = estado == SessionCoordinator.ConnectionState.Hosting;
            GUILayout.Label(anfitrion ? "Eres el ANFITRIÓN del taller." : "Estás en el taller de otro.", UiStyles.Cuerpo);

            string nombre = SteamBootstrap.Instance != null ? SteamBootstrap.Instance.LocalPlayerName : "Aprendiz";
            GUILayout.Label("Tú: " + nombre, UiStyles.CuerpoTenue);

            if (anfitrion && sessionCoordinator.CurrentTransportMode == TransportMode.Steam)
            {
                GUILayout.Label("Pásale este id a tus amigos:", UiStyles.CuerpoTenue);
                GUILayout.TextField(sessionCoordinator.CurrentLobbyId.ToString(), UiStyles.Campo);
            }

            GUILayout.Space(6f);
            GUILayout.Label("En el taller:", UiStyles.Cuerpo);
            for (int i = 0; i < AprendizNet.Todos.Count; i++)
            {
                var avatar = AprendizNet.Todos[i];
                if (avatar == null) continue;

                Color anterior = GUI.color;
                GUI.color = avatar.ColorActual;
                string quien = avatar.IsOwner ? "tú" : "aprendiz " + avatar.OwnerClientId;
                GUILayout.Label(" ■ " + quien + " (" + avatar.DescribirColor() + ")", UiStyles.Cuerpo);
                GUI.color = anterior;
            }

            if (!anfitrion)
            {
                GUILayout.Space(4f);
                GUILayout.Label("Aquí vuelas, aspiras y viertes: las máquinas y los encargos los lleva el anfitrión.",
                    UiStyles.CuerpoTenue);
            }

            GUILayout.Space(6f);
            if (GUILayout.Button("SALIR de la sesión", UiStyles.Boton))
            {
                sessionCoordinator.Disconnect();
            }
        }
    }
}
