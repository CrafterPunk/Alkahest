using UnityEngine;
using Alkahest.Game;
using FriendsLoop.Networking;
using FriendsLoop.Platform;
// (integración pt48) `using Netcode.Transports;` RETIRADO: ese namespace vive
// en el asmdef del transporte Steam vendorizado, que Alkahest.Runtime NO
// referencia (solo FriendsLoop.Runtime lo hace) -- compilaba en el rig del
// sandbox (un solo ensamblado con todos los refs, punto ciego de la regla 53)
// y reventaba en Unity con CS0246. La sonda del transporte se consulta ahora
// vía SessionCoordinator.TransporteSteamSoportado, que vive del lado correcto
// de la frontera de ensamblados.

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

        /// <summary>
        /// (playtest 48, CONTRATO_RONDA48.md D3/§2c: "el error caduca y
        /// ofrece salida") ANTES `sessionCoordinator.LastError` se pintaba
        /// SIN caducidad y solo se limpiaba dentro de los tres métodos
        /// públicos de <see cref="SessionCoordinator"/> (StartHost/JoinLocal/
        /// JoinSteamLobby, que ponen `LastError = string.Empty` al ENTRAR) --
        /// así que un error viejo podía convivir en pantalla con el panel de
        /// "conectado" (invitado) si el jugador reintentaba y esta vez SÍ
        /// entraba, dando el efecto de "error fantasma" que describe D3. Este
        /// HUD lleva su PROPIA ventana de vida sobre una COPIA local del
        /// mensaje (mismo trato que <see cref="_avisoLocal"/>/
        /// <see cref="_avisoLocalHasta"/>, líneas de arriba): se arma solo
        /// cuando el mensaje CAMBIA (nunca allocs nuevas -- se compara la
        /// referencia/contenido que ya llegó de <see cref="SessionCoordinator.LastError"/>,
        /// no se construye ningún string aquí) y se apaga también, sin
        /// esperar a que caduque, en cuanto <see cref="DibujarVentana"/> ve
        /// el estado Hosting o Client (ver ahí).
        /// </summary>
        private string _errorMostrado;
        private float _errorHasta;
        private const float ErrorVentanaSeg = 14f; // mismo orden que _avisoLocalHasta (14f) -- de sobra para leerlo, no permanente.

        /// <summary>Jugadores máximos del lobby. Cuatro: el mandato de Cesar para este POC.</summary>
        private const int MaxJugadores = 4;

        [SerializeField] private SessionCoordinator sessionCoordinator;

        private Rect _ventana = new Rect(16f, 16f, 330f, 260f);
        private string _lobbyIdEscrito = "";
        private bool _plegado;
        private bool _autoPlegadoHecho;

        /// <summary>
        /// (fix Cesar playtest 34, "ajustes finales") ANTES, con el panel
        /// oculto (<see cref="_plegado"/>==true) OnGUI seguía dibujando una
        /// ventanita permanente ("F9 · sesión (N en el taller)") en
        /// (16,16) -- justo donde vive el panel del FRASCO (arriba-
        /// izquierda), así que "ocultar" en realidad dejaba un recuadro
        /// superpuesto para siempre. Cesar: *"F9 cierra del todo el panel...
        /// ya sé que vuelve con F9, no hace falta recordatorio en
        /// pantalla"*. F9 pasa a alternar VISIBLE COMPLETO <-> NADA EN
        /// ABSOLUTO: <see cref="Time.time"/> &lt; este valor es la única
        /// ventana en la que OnGUI dibuja algo mientras _plegado es true (un
        /// recordatorio de <see cref="RecordatorioTrasOcultarSeg"/> segundos
        /// justo tras ocultar, ni uno más) -- ver OnGUI/Update.
        /// </summary>
        private float _recordatorioHasta;
        private const float RecordatorioTrasOcultarSeg = 3f;

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
                // (fix Cesar playtest 34) Solo al pasar a OCULTO se arma el
                // recordatorio breve -- al volver a mostrar (_plegado=false)
                // no hace falta nada, la ventana entera ya está a la vista.
                if (_plegado) _recordatorioHasta = Time.time + RecordatorioTrasOcultarSeg;
            }

            // Autoplegado: en cuanto hay al menos dos aprendices en el taller,
            // el panel ya cumplió su función y estorba. Una sola vez — si el
            // jugador lo reabre con F9, se queda abierto.
            if (!_autoPlegadoHecho && AprendizNet.Todos.Count >= 2)
            {
                _autoPlegadoHecho = true;
                _plegado = true;
                _recordatorioHasta = Time.time + RecordatorioTrasOcultarSeg;
            }
        }

        private void OnGUI()
        {
            if (_plegado)
            {
                // (fix Cesar playtest 34) F9 OCULTO = NADA EN ABSOLUTO, salvo
                // los primeros segundos justo tras ocultar (ver el docblock
                // de _recordatorioHasta) -- ANTES esta rama dibujaba una
                // ventanita PERMANENTE que se superponía al panel del frasco
                // arriba-izquierda; ahora, pasado el recordatorio, ni
                // siquiera se llama a UiStyles.Preparar().
                if (Time.time < _recordatorioHasta)
                {
                    UiStyles.Preparar();
                    var r = new Rect(16f, 16f, UiStyles.S(220f), UiStyles.S(24f));
                    UiStyles.Panel(r);
                    GUI.Label(new Rect(r.x + UiStyles.S(8f), r.y, r.width, r.height),
                        "F9 · sesión (" + AprendizNet.Todos.Count + " en el taller)", UiStyles.CuerpoTenue);
                }
                return;
            }

            UiStyles.Preparar();
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

            // (playtest 48, §2c) LA VENTANA DE CADUCIDAD del error: se arma
            // SOLO cuando el mensaje cambia (sin allocs -- el propio string
            // ya viene de SessionCoordinator, no se construye ninguno
            // nuevo aquí) y se apaga de golpe al entrar en una sesión real
            // (Hosting/Client), que es justo el "limpiado también al entrar
            // en Hosting/Client" del contrato: sin esto, un jugador que
            // reintenta y esta vez SÍ conecta seguiría viendo el error
            // viejo debajo del panel de "conectado".
            string errorActual = sessionCoordinator.LastError;
            if (!string.IsNullOrEmpty(errorActual) && !ReferenceEquals(errorActual, _errorMostrado))
            {
                _errorMostrado = errorActual;
                _errorHasta = Time.time + ErrorVentanaSeg;
            }
            if (estado == SessionCoordinator.ConnectionState.Hosting || estado == SessionCoordinator.ConnectionState.Client)
            {
                _errorMostrado = null;
            }
            bool errorVigente = !string.IsNullOrEmpty(_errorMostrado) && Time.time < _errorHasta;

            switch (estado)
            {
                case SessionCoordinator.ConnectionState.Offline:
                case SessionCoordinator.ConnectionState.Starting:
                    DibujarDesconectado(estado, errorVigente);
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
            if (errorVigente)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Algo falló: " + _errorMostrado, UiStyles.CuerpoTenue);
                if (GUILayout.Button("entendido", UiStyles.Boton)) _errorMostrado = null;
            }

            GUILayout.Space(4f);
            // (playtest 48, D4/§2e) BOTÓN AJUSTES CHICO: el mismo panel de
            // Game/DayCycle.cs, para que el lobby (y cualquier estado de
            // fallo) tenga una salida sin depender del avatar -- ver
            // AbrirAjustesDayCycle() para el porqué de la reflexión (DayCycle.cs
            // no está en la lista de archivos de este encargo).
            if (GUILayout.Button("AJUSTES", UiStyles.Boton, GUILayout.Height(UiStyles.S(22f))))
            {
                AbrirAjustesDayCycle();
            }
            GUILayout.Label("F9 esconde este panel.", UiStyles.CuerpoTenue);

            GUI.DragWindow();
        }

        /// <summary>
        /// (playtest 48, D4/§2e) Abre el panel de AJUSTES de
        /// <see cref="Game.DayCycle"/> desde el HUD del lobby, SIN duplicar
        /// UI (el contrato lo pide explícitamente: "sin duplicar UI: exponer
        /// un `AbrirAjustes()` público si hace falta"). `Game/DayCycle.cs`
        /// NO está en la lista de archivos EXCLUSIVOS de este encargo (ver
        /// CONTRATO_RONDA48.md §2, "Archivos de R"), así que no se le puede
        /// añadir ese método público sin salirse del contrato de propiedad
        /// de archivos disjunta (regla 41 de CLAUDE.md). En su lugar se
        /// reutiliza el MISMO patrón que ya usa este proyecto para cruzar
        /// esa frontera sin editar el archivo ajeno
        /// (Net/SimSync.cs::Awake ya cablea por reflexión un campo privado
        /// de FriendsLoop.Networking.SessionCoordinator, otro archivo fuera
        /// de su propia lista, con el mismo razonamiento): se garantiza que
        /// exista la instancia de pausa del lobby con el método público
        /// idempotente <see cref="DayCycle.ForzarDesbloqueoSesion"/>, y se
        /// marca su campo privado `_ajustesAbiertos` por reflexión -- el
        /// MISMO campo que ya leen/escriben <c>ManejarEscape</c>/
        /// <c>DrawTitle</c>/<c>DrawPausa</c> de esa clase, así que el panel
        /// que se abre es literalmente el mismo, no una copia.
        /// COSTURA DOCUMENTADA (para el informe de la ronda): el día que
        /// Game/DayCycle.cs entre en el alcance de este encargo, la forma
        /// correcta es reemplazar esto por un `DayCycle.AbrirAjustes()`
        /// público de verdad.
        /// </summary>
        private static void AbrirAjustesDayCycle()
        {
            DayCycle.ForzarDesbloqueoSesion(); // idempotente: no-op si ya existe una instancia (título clásico) o crea la de pausa del lobby multi.
            var dayCycle = FindAnyObjectByType<DayCycle>();
            if (dayCycle == null)
            {
                Debug.LogWarning("[TenThousandYears][Red] AJUSTES: no se encontró ningún DayCycle en la escena tras ForzarDesbloqueoSesion.");
                return;
            }

            var campo = typeof(DayCycle).GetField("_ajustesAbiertos",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (campo == null)
            {
                Debug.LogWarning("[TenThousandYears][Red] AJUSTES: DayCycle._ajustesAbiertos no existe (¿renombrado?) -- botón sin efecto.");
                return;
            }

            campo.SetValue(dayCycle, true);
        }

        private void DibujarDesconectado(SessionCoordinator.ConnectionState estado, bool errorVigente)
        {
            bool arrancando = estado == SessionCoordinator.ConnectionState.Starting;

            GUILayout.Label(arrancando ? "Abriendo el taller..." : "Todavía trabajas solo.", UiStyles.Cuerpo);
            GUILayout.Space(4f);

            GUI.enabled = !arrancando;

            // (playtest 48, D3/§2c) "JUGAR SOLO EN ESTE PC": cuando hay un
            // error vigente, la salida más honesta no es pedirle al jugador
            // que reintente el mismo camino que acaba de fallar -- es
            // ofrecerle DE INMEDIATO el camino que YA funciona (el mismo
            // StartHost(LocalLoopback) del botón "ANFITRIÓN en local" de más
            // abajo, ver ese botón). Grande y arriba de todo, justo donde
            // aparece el error: es la respuesta directa a "el multi en
            // solitario sale roto" del feedback de Cesar.
            if (errorVigente)
            {
                if (GUILayout.Button("JUGAR SOLO EN ESTE PC", UiStyles.Boton, GUILayout.Height(UiStyles.S(30f))))
                {
                    _errorMostrado = null;
                    // (CONTRATO_RONDA50.md §4b, ENCARGO M) Reseteo explícito:
                    // este botón es un camino de recuperación tras un fallo
                    // de OTRO intento de host (puede haber sido el botón
                    // SEMILLA CERO compartida, más abajo, el que dejó el flag
                    // en `true` antes de reventar) — sin esto, "jugar solo"
                    // podría heredar el modo equivocado de un intento previo.
                    AlkahestGameBootstrap.ModoSemillaCero = false;
                    sessionCoordinator.StartHost(TransportMode.LocalLoopback, MaxJugadores);
                }
                GUILayout.Space(6f);
            }

            // ANFITRIÓN: abre el taller. El modo de transporte por defecto lo
            // decide el propio coordinador leyendo `-transport local|steam` de
            // la línea de comandos (así se prueban dos instancias en el mismo
            // PC sin tocar la UI); si no hay override, Steam, que es como se
            // juega con amigos de verdad.
            if (GUILayout.Button("ANFITRIÓN — abre tu taller (hasta 4)", UiStyles.Boton))
            {
                IniciarAnfitrion(semillaCero: false);
            }

            // (CONTRATO_RONDA50.md §4b, ENCARGO M, "LA TERCERA SECCIÓN")
            // "necesitamos esa tercera sección para hacer más pruebas en
            // simultáneo" (Cesar, textual, sobre el 49) -- mismo flujo de
            // ANFITRIÓN de arriba (Steam con sonda + fallback local, ver
            // IniciarAnfitrion), pero el mundo nace con la seed de autor
            // (Universe.SemillaCero = 777002) + los overrides + la veta +
            // TODAS las salas destapadas de una vez (Net/SimSync.cs::
            // OnNetworkSpawn y Game/AlkahestSim.cs::CrearMundoInterno leen el
            // flag que este botón deja en `true` ANTES de StartHost) — SIN
            // el director de beats (Game/AlkahestGameBootstrap.cs::TrySpawnRed
            // nunca instancia Game/SemillaCero.cs, contrato pt40 §2, no
            // tocado). Es un LABORATORIO compartido para probar la química de
            // autor entre varios a la vez, no el arco guiado: "quizás luego
            // lo quitemos" (Cesar, textual).
            GUILayout.Space(4f);
            if (GUILayout.Button("ANFITRIÓN — SEMILLA CERO compartida", UiStyles.Boton))
            {
                IniciarAnfitrion(semillaCero: true);
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
                    Debug.LogWarning("[TenThousandYears][Red] Pega primero el id del lobby de tu amigo, o acepta su invitación desde Steam.");
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
                // (CONTRATO_RONDA50.md §4b, ENCARGO M) Mismo reseteo
                // explícito que "JUGAR SOLO EN ESTE PC" arriba: este botón de
                // prueba local es otro camino de host que NO pasa por
                // IniciarAnfitrion, así que también necesita limpiar
                // cualquier `true` que hubiera quedado de un intento anterior
                // de SEMILLA CERO compartida.
                AlkahestGameBootstrap.ModoSemillaCero = false;
                sessionCoordinator.StartHost(TransportMode.LocalLoopback, MaxJugadores);
            }
            if (GUILayout.Button("UNIRME en local (127.0.0.1)", UiStyles.Boton))
            {
                sessionCoordinator.JoinLocal();
            }

            GUI.enabled = true;
        }

        /// <summary>
        /// (CONTRATO_RONDA50.md §4b, ENCARGO M) Extraído de dentro del botón
        /// "ANFITRIÓN — abre tu taller" para que el botón nuevo de SEMILLA
        /// CERO compartida use EXACTAMENTE el mismo flujo (sonda Steam del
        /// pt48 + fallback local + el mismo aviso de "Steam no respondió"):
        /// el contrato lo pide explícito ("el botón nuevo respeta el flujo
        /// ANFITRIÓN existente, incluida la sonda Steam del pt48 y el
        /// fallback local"), y duplicar el cuerpo entero a mano habría sido
        /// la forma más fácil de que los dos botones divergieran en un fix
        /// futuro (uno se arregla, el otro no). Sin cambios de comportamiento
        /// respecto al botón original salvo el parámetro nuevo.
        /// </summary>
        private void IniciarAnfitrion(bool semillaCero)
        {
            // (fix del atasco de Cesar) Si Steam NO está abierto, este botón
            // moría con un error y "no ocurría nada": ahora cae SOLO a taller
            // local -- jugar en solitario dándole a ANFITRIÓN es un camino
            // válido. El aviso queda a la vista.
            var modo = sessionCoordinator.GetDefaultTransportMode(TransportMode.Steam);
            bool steamListo = FriendsLoop.Platform.SteamBootstrap.Instance != null
                && FriendsLoop.Platform.SteamBootstrap.Instance.IsSteamReady;
            // (playtest 48, §2c) LA SONDA BARATA, ANTES del StartHost de
            // Steam: SteamBootstrap.IsSteamReady solo dice "el cliente de
            // Steam respondió al arrancar la app" -- no dice nada del
            // TRANSPORTE en sí (SteamNetworkingSocketsTransport.IsSupported
            // existía desde antes, nadie lo consultaba desde este HUD).
            // Combinar las dos reduce el número de veces que se llega a
            // intentar StartHost(Steam) para acabar cayendo en la rama de
            // fallo de SessionCoordinator (D3/§2a) -- más barato detectar
            // aquí que dejar que NGO/el transporte lo descubran solos.
            if (steamListo)
                steamListo = sessionCoordinator.TransporteSteamSoportado; // (integración pt48) la sonda vive en SessionCoordinator: Alkahest.Runtime no referencia el asmdef del transporte.
            bool cayoALocal = modo == TransportMode.Steam && !steamListo;
            if (cayoALocal) modo = TransportMode.LocalLoopback;

            // (CONTRATO_RONDA50.md §4b) EL FLAG SE FIJA ANTES DE StartHost:
            // Net/SimSync.cs::OnNetworkSpawn lo lee ahí (síncrono para
            // LocalLoopback, poco después para Steam) para decidir la seed
            // del mundo del anfitrión -- ver ese archivo. `semillaCero: false`
            // (el botón de siempre) también escribe el flag explícitamente
            // en vez de dejarlo como estaba: es el mismo reseteo defensivo
            // que "JUGAR SOLO EN ESTE PC"/"ANFITRIÓN en local" más abajo,
            // así que CUALQUIER botón de host de este panel deja el flag en
            // el valor correcto, sin depender de qué se pulsó antes.
            AlkahestGameBootstrap.ModoSemillaCero = semillaCero;

            sessionCoordinator.StartHost(modo, MaxJugadores);

            // (playtest 42, hotfix de la captura de Cesar: el panel decía
            // "Abrí tu taller en modo LOCAL: puedes jugar" y JUSTO DEBAJO
            // "Algo falló: StartHost devolvió false" -- dos mensajes
            // contradictorios a la vez) El aviso del fallback se redacta
            // DESPUÉS de intentar el arranque, según lo que de verdad pasó:
            // el modo local es síncrono, así que aquí ya se sabe si Hosting
            // o no. El diagnóstico fino del fallo (puerto ocupado por otra
            // ventana, sesión a medio cerrar) lo pone ahora el propio
            // coordinador en LastError -- este aviso solo evita prometer un
            // taller que no abrió.
            if (cayoALocal)
            {
                bool abrio = sessionCoordinator.CurrentState == SessionCoordinator.ConnectionState.Hosting;
                _avisoLocal = abrio
                    ? "Steam no respondió (cliente cerrado, o falta steam_appid.txt junto al .exe).\nAbrí tu taller en modo LOCAL: puedes jugar; para amigos, rehaz la build o abre Steam y reinicia."
                    : "Steam no respondió (cliente cerrado, o falta steam_appid.txt junto al .exe).\nY el modo LOCAL tampoco pudo abrir — mira el motivo aquí abajo.";
                _avisoLocalHasta = Time.time + 14f;
            }
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
                // (integración pt55, LA FUGA DE RE-HOST del pt53) Salir de la
                // sesión RECARGA la escena activa: es la única forma barata y
                // completa de resetear el estado por-escena (bootstrap._spawned,
                // registro de MaquinaSync, HUDs, réplicas huérfanas) — el fix
                // fino "por piezas" está anotado como deuda desde el pt53 y
                // nadie lo cerró; mientras tanto, re-hostear sin recargar
                // dejaba un taller fantasma (y hasta el haz del frasco muerto,
                // ver Flask.cs:241). Las estáticas que deben sobrevivir ya se
                // resetean solas (ModoSemillaCero en SimSync.OnNetworkDespawn,
                // guardas de Balda/Anclaje/Pila y MachineFocus en el arranque
                // del bootstrap, AlbumReal.Abierto en su OnDestroy).
                UnityEngine.SceneManagement.SceneManager.LoadScene(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
