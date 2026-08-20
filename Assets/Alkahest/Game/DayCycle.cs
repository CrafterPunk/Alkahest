using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Alkahest.Sim;
using Alkahest.Audio;
using FriendsLoop.Networking;

namespace Alkahest.Game
{
    /// <summary>
    /// Máquina de estados de la partida + todos sus overlays de pantalla
    /// completa: Título -&gt; DayIntro(día) -&gt; Playing (cuenta atrás de
    /// 6:00) -&gt; DayEnd -&gt; (día &lt; 3 ? siguiente DayIntro : EndScreen).
    ///
    /// Mientras cualquier fase que no sea Playing está activa, congela la
    /// simulación (AlkahestSim.Paused = true) y expone
    /// <see cref="InputLocked"/> para que Flask/Dispenser/HeatPlate/
    /// ChillStone/NamingUi ignoren por completo el input del jugador.
    ///
    /// DISEÑO (balance playtest 8): la partida SIEMPRE juega las 3 jornadas
    /// completas -- el arco de tres días es la forma del juego, y llegar
    /// pronto a un Favor alto (con los encargos de día 1+2 solos se llegaba
    /// a 175, ver derivación en OrderSystem) ya NO corta la partida. El
    /// desenlace se decide SOLO al final de la jornada 3, graduado en 4
    /// escalones por <see cref="OrderSystem.DesenlaceParaFavor"/>. El aviso
    /// de "dos jornadas sin entregar nada" se conserva como advertencia de
    /// sabor (el Maestro pierde la paciencia) pero YA NO fuerza el final
    /// anticipado: solo el Favor acumulado al cierre de la jornada 3 decide.
    ///
    /// OJO: esto es el fin de la JORNADA (un día de los tres), no el fin de
    /// la PARTIDA -- la partida siempre dura las tres jornadas completas
    /// (párrafo de arriba) y el desenlace se gradúa al terminar la tercera.
    /// Eso no cambia aquí.
    ///
    /// CIERRE ANTICIPADO DE JORNADA (restaurado en playtest 9): si se
    /// entregan TODOS los encargos del día antes de que se acabe el reloj de
    /// 6:00, no tiene sentido obligar a esperar sin nada que hacer -- se
    /// avisa y se puede cerrar ya (ENTER) o se cierra solo a los
    /// <see cref="DayEndAutoCloseSeconds"/>. Esto se implementó en el
    /// playtest 6 y se perdió al reescribir esta clase en el playtest 8 para
    /// el sistema de los cuatro desenlaces (el reporte del playtest 9, "al
    /// completar las tareas no termino la partida", era justo esta
    /// regresión). Reutiliza <see cref="EnterDayEnd"/> -- el MISMO camino de
    /// transición que el fin por temporizador -- para que no haya dos rutas
    /// de salida de Playing con reglas distintas. Ver
    /// <see cref="UpdateAllOrdersDoneEarlyClose"/>.
    /// </summary>
    public sealed class DayCycle : MonoBehaviour
    {
        private enum Phase { Title, DayIntro, Playing, DayEnd, EndScreen }

        public const int TotalDays = 3;
        private const float DayDurationSeconds = 6f * 60f; // "6:00" en el HUD.

        /// <summary>
        /// Cuánto tarda en cerrarse SOLA la jornada tras completar todos los
        /// encargos si el jugador no pulsa ENTER antes (restaurado playtest 9,
        /// visto originalmente en playtest 6). 12s: tiempo de sobra para leer
        /// el aviso y decidir, sin dejar la pantalla congelada mucho rato.
        /// </summary>
        private const float DayEndAutoCloseSeconds = 12f;

        // =================================================================
        // TERCERA RONDA DEL PIVOT -- "LA APERTURA NO PUEDE EMPEZAR CON
        // 'JORNADA 1 DE 3' Y UNA LISTA DE ENCARGOS" (Cesar, tras jugarlo en
        // su Unity). Dos piezas, las dos aquí:
        //  (a) La primera pantalla del cuarto íntimo NO pasa por
        //      Phase.DayIntro -- ver <see cref="EnterCuartoIntimoSilencioso"/>,
        //      que bifurca <see cref="EnterDayIntro"/>/<see cref="DrawDayIntro"/>
        //      (SIN TOCARLOS, regla 26 de CLAUDE.md: siguen íntegros para el
        //      día en que la sesión cronometrada clásica vuelva).
        //  (b) Los encargos no EXISTEN hasta que el jugador cava un camino
        //      real hasta la Tolva -- "el Maestro no puede pedirte nada
        //      mientras no haya un agujero por donde hablarte". Ver
        //      <see cref="TolvaAlcanzable"/>/<see cref="ActualizarDesbloqueoDeEncargos"/>.
        // =================================================================

        /// <summary>
        /// Cada cuánto se comprueba si la Tolva es alcanzable (ver
        /// <see cref="TolvaAlcanzable"/>) -- NUNCA cada frame: mientras nadie
        /// ha cavado nada la respuesta no cambia, así que barrer el anillo de
        /// la boca 30 veces por segundo sería gastar ciclos en una pregunta
        /// que ya se sabe la respuesta. 1.5s: rápido de sobra para que "que
        /// se note" no tarde perceptiblemente después del golpe de cincel que
        /// abre el último muro.
        /// </summary>
        private const float TolvaCheckIntervalSeconds = 1.5f;

        /// <summary>Cuánto dura en pantalla el aviso de "los encargos han llegado" (ver <see cref="DrawEncargosDesbloqueadosBanner"/>) -- de sobra para leerlo una vez, sin quedarse clavado para siempre.</summary>
        private const float EncargosDesbloqueadosBannerSeconds = 6f;

        // Nota IMGUI: los overlays de esta clase usan GUILayout.BeginArea
        // (no GUILayout.Window), así que no necesitan un id constante --
        // BeginArea no lo pide. Los HUD que sí usan Window (DevPalette,
        // NamingUi, JournalHud) siguen la convención de ids constantes del
        // proyecto. FlaskHud/OrdersHud/HintSystem se dibujan con rects
        // absolutos y estilos de Game/UiStyles.cs (pase visual M5).

        /// <summary>
        /// True si el próximo Init() debe saltarse el Título e ir directo a
        /// la intro de la jornada 1 -- fijado justo antes de un reload de
        /// escena disparado por "Entrar al taller" o por los botones de la
        /// pantalla final (ver RestartRun). La seed en sí vive en
        /// AlkahestSim.NextRunSeed, que es quien la consume en su Start().
        /// </summary>
        private static bool _skipTitleOnLoad;

        /// <summary>
        /// True mientras cualquier overlay de jornada (todo salvo Playing)
        /// está activo. Consultado por Flask/Dispenser/HeatPlate/ChillStone/
        /// NamingUi para ignorar completamente el input del jugador durante
        /// Título/intro/fin de día/pantalla final. Empieza en true a
        /// propósito: hasta que este componente corre su primer Update() el
        /// juego debe considerarse "congelado" (estamos en Título).
        /// </summary>
        public static bool InputLocked { get; private set; } = true;

        /// <summary>
        /// (playtest 21, EL PIVOT) Silencia TODO el HUD de mundo para el
        /// arranque del cuarto íntimo -- HERMANO de <see cref="InputLocked"/>,
        /// se comprueba en la MISMA línea (contrato CONTRATO_PIVOT.md):
        /// <c>if (DayCycle.InputLocked || DayCycle.HudSilenciado) return;</c>,
        /// añadido a la guarda que YA tenía la primera línea de cada
        /// <c>OnGUI</c> en Game/OrdersHud.cs, FlaskHud.cs, HintSystem.cs,
        /// SubstanceKnowledge.cs, StorageRack.cs, HeatPlate.cs, ChillStone.cs,
        /// Dispenser.cs, DeliveryChute.cs.
        ///
        /// Arranca en `true` A PROPÓSITO: la primera pantalla del cuarto
        /// íntimo tiene que estar completamente limpia -- sin reloj (ya no
        /// existe, ver <see cref="DrawPlayingHud"/>), sin encargos, sin
        /// frasco, sin pistas, sin rótulos de máquina -- hasta que el
        /// jugador haga algo de verdad. Se apaga sola en
        /// <see cref="DetectarPrimeraAccion"/> (primer movimiento o primer
        /// clic de aspirar/verter), NO con una tecla dedicada: no es un
        /// atajo nuevo que aprender, es la propia partida empezando. Se
        /// reafirma a `true` en <see cref="Init"/> (no solo el inicializador
        /// estático de arriba, que en Unity NO se re-ejecuta entre recargas
        /// de escena dentro de la misma sesión de Play) para que un
        /// "Reintentar" desde la pantalla final vuelva a arrancar en
        /// silencio, igual que la primera vez.
        /// </summary>
        public static bool HudSilenciado { get; private set; } = true;

        /// <summary>
        /// (playtest 28, POC multiplayer) Abre el taller de la escena MULTI,
        /// que NO tiene ciclo de jornadas: ni Título, ni intro, ni reloj, ni
        /// pantalla final. Los dos cerrojos de arriba arrancan en `true` a
        /// propósito ("hasta que este componente corra su primer Update, el
        /// juego está congelado") y en la escena MULTI ese Update no llega
        /// nunca, porque no hay DayCycle en escena: sin esto, el frasco
        /// ignoraría todos los clics y el HUD no se dibujaría jamás.
        ///
        /// Es lo ÚNICO que este archivo aporta a la sesión en red. No toca la
        /// máquina de estados (`_phase` sigue donde estaba) porque en esa
        /// escena no hay ninguna instancia que la haga girar; en la escena
        /// clásica nadie llama a este método y el ciclo sigue mandando igual
        /// que siempre a través de <c>ApplyPause</c>.
        /// </summary>
        public static void ForzarDesbloqueoSesion()
        {
            InputLocked = false;
            HudSilenciado = false;

            // =============================================================
            // (ENCARGO M, CONTRATO_FASE_A.md §2) PAUSA/AJUSTES TAMBIÉN EN
            // MULTI -- pero la escena MULTI, por diseño (ver el docblock de
            // arriba y AlkahestGameBootstrap.TrySpawnRed), NUNCA instancia
            // DayCycle por el camino normal: sin un DayCycle vivo, Escape no
            // tiene quién lo escuche, y el panel de AJUSTES del Título
            // tampoco existiría ahí. Se AUTO-INSTANCIA aquí, la única vez
            // que TrySpawnRed llama a este método por sesión (protegido por
            // su propio `_spawned`; el FindAnyObjectByType de abajo es una
            // segunda defensa barata, no el mecanismo principal).
            //
            // ESTA INSTANCIA NUNCA RECIBE Init(sim, orderSystem, ...) --
            // arranca con <see cref="ArrancarSoloParaPausaMulti"/>, que la
            // deja en Phase.Playing con el reloj infinito (mismo camino que
            // ya usa la sesión sin reloj de un jugador, ver EnterPlaying/
            // DrawPlayingHud) y CON _sim EN NULL A PROPÓSITO: es la garantía
            // ESTRUCTURAL (no una condición en tiempo de ejecución que
            // alguien pueda romper mañana) de que "en multi la pausa no
            // congela la sim compartida" -- ver ApplyPause, que solo toca
            // `_sim.Paused` si `_sim != null`, y aquí `_sim` nunca se asigna.
            // Todos los demás campos de la jornada clásica (_orderSystem,
            // _knowledge, _hints...) se quedan en null también; sus
            // null-checks ya existentes en Update/OnGUI los vuelven no-op.
            // =============================================================
            if (FindAnyObjectByType<DayCycle>() == null)
            {
                var go = new GameObject("DayCycle_PausaMulti");
                go.AddComponent<DayCycle>().ArrancarSoloParaPausaMulti();
            }
        }

        /// <summary>
        /// (ENCARGO M) Arranque MÍNIMO para la escena MULTI -- ver el
        /// docblock de <see cref="ForzarDesbloqueoSesion"/>, que es el único
        /// llamador. Entra directo en Phase.Playing con el reloj infinito
        /// (mismo estado que deja <see cref="EnterPlaying"/> en la sesión
        /// sin reloj de un jugador): cero dibujo nuevo que mantener, porque
        /// <see cref="DrawPlayingHud"/> ya sabe quedarse callado con
        /// `_timeRemaining` en infinito. Lo único que este modo aporta de
        /// verdad es que `Update()`/`OnGUI()` vuelven a correr en la escena
        /// MULTI (nadie más lo hacía) para poder escuchar Escape.
        /// </summary>
        public void ArrancarSoloParaPausaMulti()
        {
            _modoMulti = true;
            EnterPlaying();
        }

        private AlkahestSim _sim;
        private OrderSystem _orderSystem;
        private SubstanceKnowledge _knowledge;
        private MasterSupplies _supplies;
        private HintSystem _hints;

        private Phase _phase;
        private int _day = 1;
        private float _timeRemaining;
        private int _consecutiveZeroOrderDays;
        /// <summary>
        /// Aviso de sabor (balance playtest 8): dos jornadas seguidas sin
        /// entregar nada. YA NO fuerza el final anticipado (la partida
        /// siempre juega las 3 jornadas) -- solo se muestra como advertencia
        /// en <see cref="DrawDayEnd"/>, coherente con un Maestro exigente
        /// pero justo que avisa antes de graduar mal, no que expulsa a
        /// media partida.
        /// </summary>
        private bool _avisoDesatencion;

        private string _seedField = "";

        // Caché del reloj de la jornada (ver DrawPlayingHud).
        private int _relojSegundos = -1;
        private string _relojTexto = "";

        // Cierre anticipado de jornada (restaurado playtest 9, ver
        // UpdateAllOrdersDoneEarlyClose): true en cuanto AllOrdersCompleted()
        // se detecta durante la jornada actual; se reinicia en EnterPlaying
        // para que cada día empiece "sin anunciar". La cuenta atrás se
        // ACOTA cada frame a _timeRemaining (ver el método) para que este
        // aviso nunca alargue la jornada ni un segundo más de lo que ya
        // tenía el reloj normal.
        private bool _allOrdersDoneAnnounced;
        private float _allOrdersDoneCountdown;
        private int _cierreSegundos = -1;
        private string _cierreTexto = "";

        // Desbloqueo de encargos al cavar hasta la Tolva (tercera ronda del
        // pivot, ver el bloque de constantes de arriba): _encargosDesbloqueados
        // corta la comprobación periódica para siempre en cuanto se dispara
        // una vez (cavar es permanente en Sim/ -- nunca vuelve a sellarse
        // solo). _tolvaCheckTimer cae a <=0 en el primer Update() de Playing
        // a propósito (arranca en 0f): la primera comprobación es barata (el
        // mundo nace sellado, así que sale que "no" al instante) y así no
        // hay que esperar el primer intervalo completo sin motivo.
        private bool _encargosDesbloqueados;
        private float _tolvaCheckTimer;
        private float _encargosBannerCountdown;

        // =================================================================
        // (ENCARGO M, CONTRATO_FASE_A.md §2) PAUSA + AJUSTES.
        // =================================================================
        /// <summary>True SOLO en la instancia auto-creada de la escena MULTI (ver <see cref="ArrancarSoloParaPausaMulti"/>). Decide, en <see cref="VolverAlTitulo"/>, si "volver al título" recarga esta misma escena (un jugador) o desconecta y carga la escena CLÁSICA (multi, que no tiene título propio).</summary>
        private bool _modoMulti;
        /// <summary>True mientras el overlay de PAUSA está activo (Escape durante Playing). Ortogonal a `_phase` a propósito: la pausa NO es una fase nueva de la jornada, es una interrupción de Playing -- ver <see cref="ApplyPause"/>.</summary>
        private bool _pausado;
        /// <summary>True mientras el panel de AJUSTES está sobre pantalla -- se abre desde el Título O desde la Pausa (mismo panel, ver <see cref="DrawAjustes"/>); al cerrarlo con "listo" (o Escape) se vuelve a lo que hubiera debajo (Título o Pausa) sin que este archivo tenga que recordar de dónde vino.</summary>
        private bool _ajustesAbiertos;

        private const string PrefKeyVolGeneral = "ChaosAlchemy_VolGeneral";
        /// <summary>
        /// Volumen general (AudioListener.volume), 0..1. CARGA PEREZOSA
        /// (hotfix pt47, visto EN VIVO): la versión anterior lo cargaba en el
        /// inicializador de campo estático, y Unity PROHÍBE PlayerPrefs ahí
        /// -- el .cctor lanzaba, el TIPO DayCycle quedaba envenenado
        /// (TypeInitializationException) y TODO lo que consultaba
        /// DayCycle.InputLocked (cada OnGUI del juego) explotaba en cascada:
        /// sin título, sin HUD, "me sale roto". El centinela -1 marca "aún no
        /// leído"; la primera lectura real ocurre desde Awake/OnGUI, que son
        /// contextos permitidos. NUNCA volver a llamar API de Unity en un
        /// inicializador estático (vale también para AudioListener,
        /// Application.*, etc.).
        /// </summary>
        private static float _volGeneral = -1f;
        private static float VolGeneral
        {
            get
            {
                if (_volGeneral < 0f) _volGeneral = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefKeyVolGeneral, 1f));
                return _volGeneral;
            }
        }

        /// <summary>Nombre de la escena CLÁSICA (Título -> 3 jornadas), para "VOLVER AL TÍTULO" desde la escena MULTI -- ver <see cref="VolverAlTitulo"/>. Decisión fuera de contrato: no existía una constante compartida para este nombre; si la escena se renombrara algún día sin tocar este archivo, `SceneManager.LoadScene` falla con un solo error de consola, nada revienta en juego.</summary>
        private const string SceneNameClasica = "AlkahestLab";

        private void Awake()
        {
            // (ENCARGO M) Aplicar el volumen general guardado ANTES de que
            // suene nada. AudioListener.volume es API de motor -- no se
            // puede fijar desde un inicializador estático de campo con
            // garantías -- así que se aplica aquí, una vez por instancia de
            // DayCycle, que en las dos escenas (clásica vía
            // AlkahestGameBootstrap.SpawnDayCycle, MULTI vía
            // ArrancarSoloParaPausaMulti) es literalmente el primer
            // componente en existir de la partida. Si alguna vez hubiera dos
            // instancias a la vez (no debería), reaplicar el mismo valor es
            // un no-op inocuo.
            AudioListener.volume = VolGeneral;
        }

        private static void SetVolGeneral(float v)
        {
            v = Mathf.Clamp01(v);
            if (Mathf.Approximately(v, VolGeneral)) return;
            _volGeneral = v;
            AudioListener.volume = _volGeneral;
            PlayerPrefs.SetFloat(PrefKeyVolGeneral, _volGeneral);
            PlayerPrefs.Save();
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, OrderSystem orderSystem, SubstanceKnowledge knowledge,
            MasterSupplies supplies = null, HintSystem hints = null)
        {
            _sim = sim;
            _orderSystem = orderSystem;
            _knowledge = knowledge;
            _supplies = supplies;
            _hints = hints;

            // (playtest 21) Cada partida nueva (primera carga o "Reintentar"
            // tras un reload de escena, ver RestartRun) arranca con el HUD
            // silenciado -- ver el docblock de HudSilenciado para el porqué.
            // (playtest 25) "LO QUE PERSISTE" apaga esto de inmediato en
            // EnterCuartoIntimoSilencioso, pero se deja el arranque en true
            // aquí tal cual: sigue siendo el estado correcto para el primer
            // frame real (Título) y para el modo clásico, que este encargo
            // no toca.
            HudSilenciado = true;

            // (playtest 25, CONTRATO_PERSISTE.md §6.3) Hornada es ESTÁTICA
            // (sobrevive a un reload de escena si el dominio no se recarga,
            // ver el docblock de Hornada.Limpiar) -- AlkahestGameBootstrap.cs
            // ya llama a MachineFocus.Limpiar() en su TrySpawn() pero ese
            // archivo está fuera del alcance de este encargo, así que el
            // reinicio de Hornada se ancla aquí en su lugar: DayCycle.Init
            // es, igual que MachineFocus.Limpiar, lo primero que corre en
            // CUALQUIER partida nueva (título o "Reintentar"). DECISIÓN
            // fuera del contrato literal (que solo especifica DÓNDE vive
            // Hornada, no quién la limpia).
            Hornada.Limpiar();

            if (_skipTitleOnLoad)
            {
                _skipTitleOnLoad = false;
                // (tercera ronda del pivot) EnterDayIntro(1) -- que dibuja
                // "Jornada 1 de 3" + "ENCARGOS DE HOY" -- SE BIFURCA aquí en
                // vez de llamarse: ver el docblock de
                // EnterCuartoIntimoSilencioso. EnterDayIntro/DrawDayIntro
                // siguen íntegros más abajo, sin tocar, para cuando la
                // sesión cronometrada clásica vuelva.
                EnterCuartoIntimoSilencioso(1);
            }
            else
            {
                EnterTitle();
            }
        }

        private void Update()
        {
            ManejarEscape();
            ApplyPause();

            if (_phase == Phase.Playing && !_pausado)
            {
                // (playtest 21, EL PIVOT) SESIÓN SIN RELOJ (CONTRATO_PIVOT.md,
                // decisión de Cesar: "el cuarto íntimo pasa a ser EL juego").
                // Los CUATRO cambios que pide ese contrato, todos aquí:
                //  1) `_timeRemaining` YA NO SE DECREMENTA -- se queda en
                //     `float.PositiveInfinity`, fijado en EnterPlaying.
                //  2) NO se comprueba `<= 0f` ni se llama a EnterDayEnd desde
                //     aquí (con el reloj infinito, esa rama nunca dispararía
                //     de todos modos, pero se retira la comprobación entera
                //     en vez de dejar una condición que nunca es cierta).
                //  3) NO se llama a UpdateAllOrdersDoneEarlyClose() -- el
                //     cierre anticipado de jornada NO SE BORRA (regla 11 de
                //     CLAUDE.md: ya se perdió una vez al reescribir esta
                //     clase, playtest 8->9), el método sigue definido
                //     íntegro más abajo, solo deja de invocarse: sin reloj
                //     que cerrar antes de tiempo, "anticipado" no significa
                //     nada.
                //  4) (el cuarto punto, "no dibujar el reloj", vive en
                //     DrawPlayingHud -- ver ese método).
                //
                // (playtest 25, CONTRATO_PERSISTE.md §6.5) DetectarPrimeraAccion
                // sigue corriendo -- es barata (solo teclado/ratón) y
                // completamente inerte ahora: con HudSilenciado ya puesto a
                // false desde EnterCuartoIntimoSilencioso, su única línea de
                // trabajo (`if (!HudSilenciado) return;`) vuelve
                // inmediatamente cada vez, así que dejarla no cuesta nada y
                // evita otro hueco de "por qué se llama esto si ya no hace
                // nada" sin explicar. ActualizarDesbloqueoDeEncargos() YA NO
                // SE LLAMA aquí a propósito: "LO QUE PERSISTE" no tiene gate
                // de cavado (ver el docblock grande de
                // EnterCuartoIntimoSilencioso) -- el método y TolvaAlcanzable
                // se CONSERVAN íntegros más abajo (regla 11/26 de CLAUDE.md)
                // para cuando el gate de cavado vuelva en otra dirección.
                DetectarPrimeraAccion();
            }
        }

        /// <summary>
        /// (playtest 21) Apaga <see cref="HudSilenciado"/> en cuanto el
        /// jugador hace algo de verdad: el primer movimiento (WASD/flechas,
        /// las mismas teclas que ya escucha ApprenticeController por su
        /// cuenta) o el primer clic de aspirar/verter (las mismas teclas que
        /// ya escucha Flask). Es una LECTURA PASIVA -- no dispara ninguna
        /// acción de juego, solo revela el HUD que ya estaba a punto de
        /// aparecer por sí solo -- así que NO necesita las guardas de la
        /// regla 12 de CLAUDE.md (EscribiendoTexto/JournalHud.Abierto): no
        /// es un atajo nuevo que pueda "dispararse sin querer" mientras el
        /// jugador escribe un nombre, solo confirma algo que ApprenticeController
        /// o Flask ya iban a hacer con la misma pulsación.
        /// </summary>
        private void DetectarPrimeraAccion()
        {
            if (!HudSilenciado) return;

            var kb = Keyboard.current;
            bool movimiento = kb != null && (
                kb.wKey.isPressed || kb.aKey.isPressed || kb.sKey.isPressed || kb.dKey.isPressed ||
                kb.upArrowKey.isPressed || kb.downArrowKey.isPressed ||
                kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed);

            var mouse = Mouse.current;
            bool clic = mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed);

            if (movimiento || clic) HudSilenciado = false;
        }

        /// <summary>
        /// Cierre anticipado de jornada (restaurado playtest 9, ver docblock
        /// de la clase). (playtest 21) YA NO SE LLAMA desde <see cref="Update"/>
        /// -- ver el comentario de ahí, punto 3 -- pero se conserva íntegro
        /// (regla 11 de CLAUDE.md) para cuando la sesión cronometrada vuelva.
        /// Se llamaba solo mientras _phase == Playing y el reloj normal
        /// todavía no había llegado a 0. En cuanto detecta que ya no quedan
        /// encargos pendientes arma una cuenta atrás de
        /// <see cref="DayEndAutoCloseSeconds"/>; ENTER la corta antes.
        /// </summary>
        private void UpdateAllOrdersDoneEarlyClose()
        {
            if (!_allOrdersDoneAnnounced)
            {
                if (_orderSystem == null || !_orderSystem.AllOrdersCompleted()) return;
                _allOrdersDoneAnnounced = true;
                _allOrdersDoneCountdown = DayEndAutoCloseSeconds;
            }

            _allOrdersDoneCountdown -= Time.deltaTime;
            // Acotar SIEMPRE a _timeRemaining: si al último encargo le
            // quedaban, digamos, 5s de reloj, el aviso de cierre no debe
            // "prestarle" tiempo extra a la jornada -- nunca debe durar más
            // que lo que ya le quedaba al día por su propio temporizador.
            if (_allOrdersDoneCountdown > _timeRemaining) _allOrdersDoneCountdown = _timeRemaining;

            var kb = Keyboard.current;
            // (fix playtest 10) ENTER es un atajo de una sola tecla como cualquier otro:
            // mientras el jugador ESCRIBE un nombre (UiStyles.EscribiendoTexto) no puede
            // colarse y cerrar la jornada a mitad de bautizo -- la cuenta atrás en pantalla
            // sigue corriendo igual (esto solo calla la TECLA, no el temporizador), así que
            // el cierre automático a los DayEndAutoCloseSeconds sigue llegando si hace falta.
            bool enterPulsado = kb != null && !UiStyles.EscribiendoTexto
                && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
            if (_allOrdersDoneCountdown <= 0f || enterPulsado)
            {
                EnterDayEnd();
            }
        }

        /// <summary>
        /// (tercera ronda del pivot) Arranque SILENCIOSO del cuarto íntimo:
        /// bifurca <see cref="EnterDayIntro"/> en vez de tocarlo -- ese
        /// método y <see cref="DrawDayIntro"/> se CONSERVAN íntegros (regla
        /// 26 de CLAUDE.md) para el día en que el taller clásico vuelva a
        /// excavarse y la sesión cronometrada regrese. La primera pantalla
        /// del pivot no puede decir "Jornada 1 de 3" ni listar encargos, ni
        /// mencionar una Tolva sepultada tras 23 celdas de roca que el
        /// jugador ni siquiera puede ver -- "el primer contacto es
        /// silencioso: un cuarto oscuro y algo vivo" (encargo). Dos
        /// diferencias con <see cref="EnterDayIntro"/>:
        ///  1) NO genera encargos aquí -- eso lo hace
        ///     <see cref="ActualizarDesbloqueoDeEncargos"/> cuando el
        ///     jugador cava un camino real hasta la Tolva, nunca antes ("el
        ///     Maestro no puede pedirte nada mientras no haya un agujero por
        ///     donde hablarte").
        ///  2) NO pasa por Phase.DayIntro -- entra directo en Phase.Playing,
        ///     en silencio (HudSilenciado ya arranca en true, ver su
        ///     docblock, así que ningún HUD de mundo se enseña hasta que el
        ///     jugador haga algo).
        /// Las muestras del Maestro y las pistas de la jornada SÍ se dejan
        /// igual que siempre (mismo comportamiento que EnterDayIntro): son
        /// estado del MUNDO, no del panel que se quita.
        /// </summary>
        private void EnterCuartoIntimoSilencioso(int day)
        {
            _day = day;
            if (_supplies != null) _supplies.AlEmpezarJornada(day);
            if (_hints != null) _hints.ReiniciarParaJornada(day);
            EnterPlaying();

            // (playtest 25, CONTRATO_PERSISTE.md §6.5) "LO QUE PERSISTE" ya
            // NO tiene gate de cavado: el túnel hasta la Tolva llega
            // PRE-TALLADO desde SimLevelBuilder (encargo A, contrato §4.5),
            // así que ni "esperar a que el jugador cave" ni "silenciar el
            // HUD hasta el primer movimiento" describen ya esta dirección --
            // el Maestro habla desde el minuto uno, con el HUD entero
            // visible. Sustituye a los dos mecanismos DEL PIVOT anterior que
            // vivían más abajo en este archivo (ActualizarDesbloqueoDeEncargos/
            // TolvaAlcanzable, que generaban con GenerateOrdersPivot solo tras
            // detectar el primer golpe de cincel): se CONSERVAN íntegros,
            // sin llamarse desde Update (regla 26/11 de CLAUDE.md, "no tocar
            // lo que puede volver"), para el día en que el gate de cavado
            // regrese en otra dirección de diseño.
            HudSilenciado = false;
            if (_orderSystem != null) _orderSystem.GenerateOrdersPersiste();
        }

        /// <summary>
        /// (tercera ronda del pivot) Desbloquea los encargos del día en
        /// cuanto <see cref="TolvaAlcanzable"/> dice que sí, UNA sola vez
        /// (cavar es permanente en Sim/: el mundo nunca vuelve a sellarse
        /// solo, así que no hace falta volver a comprobar tras el primer
        /// "sí" -- <see cref="_encargosDesbloqueados"/> corta la
        /// comprobación para siempre). Throttleada a
        /// <see cref="TolvaCheckIntervalSeconds"/>, nunca por frame.
        /// </summary>
        private void ActualizarDesbloqueoDeEncargos()
        {
            if (_encargosDesbloqueados)
            {
                if (_encargosBannerCountdown > 0f) _encargosBannerCountdown -= Time.deltaTime;
                return;
            }

            _tolvaCheckTimer -= Time.deltaTime;
            if (_tolvaCheckTimer > 0f) return;
            _tolvaCheckTimer = TolvaCheckIntervalSeconds;

            if (!TolvaAlcanzable()) return;

            _encargosDesbloqueados = true;
            // (playtest 23) Los encargos del pivot, NO los de la jornada
            // clásica: aquellos pedían inflamable y 80°C, imposibles en el
            // cuarto íntimo -- ver el docblock de GenerateOrdersPivot.
            if (_orderSystem != null) _orderSystem.GenerateOrdersPivot();
            // "Que se note (es la recompensa de cavar)": aviso breve, ver
            // DrawEncargosDesbloqueadosBanner. OrdersHud (encargo aparte,
            // guardado por HudSilenciado, no por esto) recogerá la lista
            // nueva en su próximo OnGUI sin que este archivo tenga que
            // tocarlo -- ActiveOrders se lee en vivo.
            _encargosBannerCountdown = EncargosDesbloqueadosBannerSeconds;
        }

        /// <summary>
        /// CRITERIO BARATO Y HONESTO para "la Tolva es alcanzable" (pedido
        /// explícito: "no hace falta un pathfinding"). La boca de la Tolva
        /// (<see cref="SimLevelBuilder.ChuteMouthX0"/>..Y1) nace TOTALMENTE
        /// rodeada de <see cref="MaterialId.Stone"/> maciza -- se talla
        /// DESPUÉS de rellenar el mundo entero de piedra, ver
        /// `Sim/SimLevelBuilder.BuildDeliveryNiche`/`FillWorldStone`. Basta
        /// con mirar el ANILLO de celdas inmediatamente FUERA de ese
        /// rectángulo (un margen de una celda a cada lado): mientras siga
        /// siendo Stone puro, nadie ha llegado hasta ahí con el cincel. En
        /// cuanto <see cref="Cincel"/> convierte UNA sola celda de ese
        /// anillo en Empty (TALLAR solo actúa sobre Stone exacto, ver su
        /// docblock), esta comprobación pasa a true -- no demuestra que
        /// exista un camino de aire TERMINADO hasta la boca, pero si el
        /// jugador ya tocó el anillo que la rodea, está lo bastante cerca
        /// para que el Maestro empiece a hablar: es literalmente el
        /// disparador alternativo que el encargo dejaba aceptar ("el
        /// jugador ha tallado piedra a menos de N celdas de la boca").
        /// O(perímetro) ~144 celdas (2*(24+52) aprox.), solo lecturas de
        /// <see cref="CellGrid.GetMat(int,int)"/> -- cero allocs, y ni
        /// siquiera eso corre cada frame (ver
        /// <see cref="ActualizarDesbloqueoDeEncargos"/>).
        /// </summary>
        private bool TolvaAlcanzable()
        {
            if (_sim == null || _sim.Grid == null) return false;
            var grid = _sim.Grid;

            int x0 = SimLevelBuilder.ChuteMouthX0 - 1;
            int x1 = SimLevelBuilder.ChuteMouthX1 + 1;
            int y0 = SimLevelBuilder.ChuteMouthY0 - 1;
            int y1 = SimLevelBuilder.ChuteMouthY1 + 1;

            for (int x = x0; x <= x1; x++)
            {
                if (CellGrid.InBounds(x, y0) && grid.GetMat(x, y0) != MaterialId.Stone) return true;
                if (CellGrid.InBounds(x, y1) && grid.GetMat(x, y1) != MaterialId.Stone) return true;
            }
            for (int y = y0 + 1; y < y1; y++)
            {
                if (CellGrid.InBounds(x0, y) && grid.GetMat(x0, y) != MaterialId.Stone) return true;
                if (CellGrid.InBounds(x1, y) && grid.GetMat(x1, y) != MaterialId.Stone) return true;
            }
            return false;
        }

        private void ApplyPause()
        {
            // (ENCARGO M) `_pausado` (Escape durante Playing, ver
            // ManejarEscape) bloquea el input exactamente igual que
            // cualquier otra fase que no sea Playing -- para Flask/
            // Dispenser/HeatPlate/ChillStone/NamingUi, "pausado" y "estamos
            // en el Título" son la misma cosa: no se puede tocar el mundo.
            bool locked = _phase != Phase.Playing || _pausado;
            InputLocked = locked;
            // UN JUGADOR: `_sim` es real y esta línea congela la simulación
            // (AlkahestSim.Paused) tanto en pausa como en el resto de fases
            // que no son Playing -- comportamiento SIN CAMBIOS respecto a
            // antes de este encargo.
            // MULTI: la instancia auto-creada por ArrancarSoloParaPausaMulti
            // NUNCA recibe `_sim` (ver su docblock) -- así que esta línea es
            // ESTRUCTURALMENTE incapaz de congelar la sim compartida. "En
            // multi la pausa no congela la sim" no depende de una condición
            // en tiempo de ejecución que alguien pueda romper mañana; depende
            // de que a esta instancia nunca se le pasa la referencia.
            if (_sim != null) _sim.Paused = locked;
        }

        /// <summary>
        /// (ENCARGO M) ESCALERA DE GUARDAS DE ESCAPE, en orden -- cada peldaño
        /// documenta POR QUÉ va antes que el siguiente:
        ///
        ///  1) <see cref="UiStyles.EscribiendoTexto"/>: mientras el jugador
        ///     escribe (el rito de NamingUi, o el campo de patente de
        ///     Hornada dentro de JournalHud) Escape le PERTENECE a ese campo
        ///     -- las dos clases ya escuchan Escape ELLAS MISMAS para
        ///     cerrarse (ver NamingUi.Update, que comprueba
        ///     `_open &amp;&amp; kb.escapeKey...` SIN mirar EscribiendoTexto, a
        ///     propósito: Escape es la única tecla que sigue funcionando
        ///     mientras se escribe). Si esta clase TAMBIÉN reaccionara a la
        ///     misma pulsación, un Escape cerraría el rito Y abriría la
        ///     pausa en el mismo frame -- un efecto doble que nadie pidió.
        ///     No hacer nada y dejar que el campo se cierre solo.
        ///
        ///  2) <see cref="JournalHud.Abierto"/> / <see cref="AlbumReal.Abierto"/>:
        ///     el diario y el álbum son ventanas de pantalla completa que
        ///     YA consumen su propio Escape para cerrarSE (mismo criterio
        ///     que el punto 1, ver sus respectivos Update). Dejarlas
        ///     cerrarse primero: nunca competir por la misma pulsación.
        ///
        ///  3) <see cref="_ajustesAbiertos"/>: el panel de AJUSTES (propio
        ///     de esta clase, se abre desde el Título o desde la Pausa) es
        ///     la capa modal más externa que SÍ gestiona este método --
        ///     Escape lo cierra y NADA MÁS (ni abre ni cierra pausa en el
        ///     mismo gesto). Funciona en cualquier fase (Título incluido),
        ///     no solo en Playing.
        ///
        ///  4) Con el mundo "limpio" (ninguna de las tres capas de arriba
        ///     activa) Escape abre/cierra la PAUSA -- pero SOLO durante
        ///     Playing: Título/DayIntro/DayEnd/EndScreen ya son sus propios
        ///     modales con sus propios botones ("Salir", "Comenzar
        ///     jornada"...) y "pausar" no significa nada ahí.
        /// </summary>
        private void ManejarEscape()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

            if (UiStyles.EscribiendoTexto) return; // (1)
            if (JournalHud.Abierto || AlbumReal.Abierto) return; // (2)

            if (_ajustesAbiertos) { _ajustesAbiertos = false; return; } // (3)

            if (_phase != Phase.Playing) return; // (4a) fuera de Playing, Escape no hace nada.
            _pausado = !_pausado; // (4b)
        }

        /// <summary>
        /// (ENCARGO M) "VOLVER AL TÍTULO" desde la Pausa.
        /// UN JUGADOR: recarga la MISMA escena SIN fijar
        /// <see cref="_skipTitleOnLoad"/> (a diferencia de <see cref="RestartRun"/>)
        /// -- Init() lo lee en false y cae directo a EnterTitle(), que es
        /// justo la pantalla que este botón promete. No hace falta pasar por
        /// RestartRun/una seed: esto no es "empezar una partida nueva", es
        /// "quiero salir de esta a la pantalla de inicio".
        /// MULTI: la escena MULTI no tiene título propio (regla del POC, ver
        /// AlkahestGameBootstrap.TrySpawnRed) -- desconectar primero (MISMO
        /// gesto que el botón "SALIR de la sesión" de
        /// Net/TallerSesionHud.cs: <see cref="SessionCoordinator.Disconnect"/>,
        /// que apaga el NetworkManager y abandona el lobby de Steam) y CARGAR
        /// LA ESCENA CLÁSICA por nombre (<see cref="SceneNameClasica"/>).
        /// </summary>
        private void VolverAlTitulo()
        {
            if (_modoMulti)
            {
                var coordinador = FindAnyObjectByType<SessionCoordinator>();
                if (coordinador != null) coordinador.Disconnect();
                SceneManager.LoadScene(SceneNameClasica);
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }

        // -----------------------------------------------------------------
        // Transiciones
        // -----------------------------------------------------------------
        private void EnterTitle()
        {
            _phase = Phase.Title;
        }

        private void EnterDayIntro(int day)
        {
            _day = day;
            if (_orderSystem != null) _orderSystem.GenerateOrdersForDay(day);

            // Las MUESTRAS DEL MAESTRO se dejan en el taller ANTES de que el
            // jugador vea la intro (la sim está pausada, así que nada se cae
            // hasta que pulse "Comenzar jornada" y lo vea ocurrir): el grifo de
            // azoth, el retoño de vivium y la semilla de cristal de la jornada 2.
            // Sin esto, cristal y "algo vivo" son inalcanzables — ver
            // Game/MasterSupplies.cs.
            if (_supplies != null) _supplies.AlEmpezarJornada(day);

            // Pistas nuevas para el día que empieza (60 s), con el contenido de
            // lo que se desbloquea hoy.
            if (_hints != null) _hints.ReiniciarParaJornada(day);

            _phase = Phase.DayIntro;
        }

        private void EnterPlaying()
        {
            // (playtest 21, EL PIVOT) `DayDurationSeconds` (la vieja "6:00")
            // NO SE BORRA -- se queda arriba, sin usar, para cuando la
            // sesión cronometrada vuelva. `_timeRemaining` nace en
            // `+Infinity` en vez de ese valor: un valor SANO para cualquier
            // código que lo lea o lo acote (Mathf.Clamp/comparaciones con
            // infinito se comportan bien; lo que NO se hace en este modo es
            // decrementarlo ni compararlo contra 0, ver Update()).
            _timeRemaining = float.PositiveInfinity;
            // Reinicio del aviso de cierre anticipado (playtest 9): cada
            // jornada nueva empieza "sin anunciar", aunque la anterior se
            // cerrase por completar todos los encargos. (playtest 21: en
            // sesión sin reloj UpdateAllOrdersDoneEarlyClose ya no se llama,
            // así que estos dos campos no vuelven a leerse, pero se dejan
            // reiniciados igual -- coste cero, y es lo que ya hacía este
            // método antes de esta ronda.)
            _allOrdersDoneAnnounced = false;
            _cierreSegundos = -1;
            _phase = Phase.Playing;
        }

        private void EnterDayEnd()
        {
            _phase = Phase.DayEnd;

            int completed = _orderSystem != null ? _orderSystem.CompletedCount() : 0;
            if (completed == 0) _consecutiveZeroOrderDays++;
            else _consecutiveZeroOrderDays = 0;

            // Solo aviso de sabor (ver doc de _avisoDesatencion): ya no
            // fuerza el salto a EndScreen.
            if (_consecutiveZeroOrderDays >= 2) _avisoDesatencion = true;
        }

        /// <summary>
        /// Balance playtest 8: la partida SIEMPRE juega las 3 jornadas
        /// completas -- antes saltaba a EndScreen en cuanto _earlyLose se
        /// activaba, cortando el arco de tres días que es la forma del
        /// juego. Ahora solo el día alcanzado decide si sigue.
        /// </summary>
        private void AdvanceAfterDayEnd()
        {
            if (_day >= TotalDays)
            {
                _phase = Phase.EndScreen;
            }
            else
            {
                EnterDayIntro(_day + 1);
            }
        }

        private void RestartRun(int? seed)
        {
            AlkahestSim.NextRunSeed = seed;
            _skipTitleOnLoad = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static void QuitGame()
        {
            Application.Quit();
        }

        private int? ParseSeedField()
        {
            if (string.IsNullOrWhiteSpace(_seedField)) return null;
            if (int.TryParse(_seedField.Trim(), out int v) && v != 0) return v;
            return null;
        }

        // -----------------------------------------------------------------
        // OnGUI: un overlay fullscreen distinto por fase (salvo Playing, que
        // solo muestra la cuenta atrás compacta arriba en el centro).
        // -----------------------------------------------------------------
        private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.72f);

        private void OnGUI()
        {
            // (ENCARGO M) AJUSTES y PAUSA son overlays MODALES por encima de
            // cualquier fase -- se comprueban ANTES del switch, y cada uno
            // hace `return` para no dibujar la fase de debajo en el mismo
            // frame. Orden: AJUSTES gana sobre PAUSA (si se abrió AJUSTES
            // desde dentro de la Pausa, se ve el panel de AJUSTES solo, no
            // los dos superpuestos); al cerrar AJUSTES con "listo"/Escape
            // (ver ManejarEscape) se vuelve a lo que hubiera debajo (Pausa o
            // el propio Título) sin código extra: el siguiente OnGUI ya no
            // entra en esta rama y cae al switch de siempre.
            if (_ajustesAbiertos) { DrawAjustes(); return; }
            if (_pausado) { DrawPausa(); return; }

            switch (_phase)
            {
                case Phase.Title: DrawTitle(); break;
                case Phase.DayIntro: DrawDayIntro(); break;
                case Phase.Playing: DrawPlayingHud(); break;
                case Phase.DayEnd: DrawDayEnd(); break;
                case Phase.EndScreen: DrawEndScreen(); break;
            }
        }

        private static void DrawFullscreenDim()
        {
            var prevColor = GUI.color;
            GUI.color = OverlayColor;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prevColor;
        }

        /// <summary>
        /// Marco común de todos los overlays: panel de tinta con filete dorado
        /// (Game/UiStyles.cs) y un área interior con margen, en vez del
        /// GUI.skin.box por defecto. Devuelve el área donde escribir.
        /// </summary>
        private static Rect AbrirPanel(float anchoDiseno, float altoDiseno)
        {
            float w = UiStyles.S(anchoDiseno), h = UiStyles.S(altoDiseno);
            var caja = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyles.Panel(caja, UiStyles.TintaFuerte, UiStyles.Borde);

            float pad = UiStyles.S(18f);
            var interior = new Rect(caja.x + pad, caja.y + pad, caja.width - pad * 2f, caja.height - pad * 2f);
            GUILayout.BeginArea(interior);
            return interior;
        }

        private void DrawTitle()
        {
            DrawFullscreenDim();
            UiStyles.Preparar();
            // (playtest 40, SEMILLA CERO) 320 -> 380: el panel gana el botón
            // principal nuevo + su filete separador -- ver el bloque de abajo.
            // (ENCARGO M) 480 -> 560 de ancho: la fila de botones pasa de dos
            // a tres (MODO CAÓTICO / AJUSTES / Salir, ver más abajo) y a 480
            // quedaban apretados contra el texto largo de MODO CAÓTICO --
            // decisión fuera de contrato, documentada aquí.
            var interior = AbrirPanel(560f, 380f);

            // (playtest 31, TIPOGRAFÍA = ALMA) El título es lo PRIMERO que ve
            // quien enciende el juego. Es una capital lapidaria (Cinzel) con
            // espaciado -- una inscripción, no una etiqueta -- y lleva su
            // filete con rombo debajo, como la portada de un tratado.
            //
            // (fix Cesar playtest 33, tarea 1, "TÍTULO") "CHAOS ALCHEMY" ->
            // "LIMO PRIMORDIAL": el nombre en inglés no encajaba con el resto
            // del juego (texto en español latino, regla 53 de CLAUDE.md) ni
            // con la propia ficción -- el limo primordial ES la sustancia de
            // la que se separan las cinco bases del retículo (Sim/Universe.cs,
            // "LO QUE PERSISTE"), el hilo que atraviesa toda la partida desde
            // el primer caño. Mini-descripción nueva, sobria, sin explicar de
            // más: la fuente/espaciado (Cinzel + UiStyles.Espaciar) y el
            // filete con rombo se conservan EXACTOS, solo cambia el texto.
            GUILayout.Space(UiStyles.S(6f));
            GUILayout.Label(UiStyles.Espaciar("LIMO PRIMORDIAL"), UiStyles.TituloGrande, GUILayout.Height(UiStyles.S(46f)));
            var filete = GUILayoutUtility.GetRect(10f, UiStyles.S(12f));
            if (Event.current.type == EventType.Repaint)
                UiStyles.FileteRombo(interior.width * 0.5f, filete.y + filete.height * 0.5f, interior.width * 0.55f, UiStyles.LatonOscuro);
            GUILayout.Label("Todo lo que existe desciende del limo.", UiStyles.Subtitulo);

            // =================================================================
            // (playtest 40, SEMILLA CERO, CONTRATO_SEMILLA.md §3) DOS MODOS DE
            // ENTRADA. El principal es el arco de autor ("SEMILLA CERO — tu
            // primer taller"): seed congelada (Universe.SemillaCero) + el flag
            // `AlkahestGameBootstrap.ModoSemillaCero` en `true`, que el resto
            // del mundo (Universe/SimLevelBuilder/Crisol/Game/SemillaCero.cs)
            // lee para tapiar salas, aplicar los overrides de autor y dirigir
            // el arco de beats. Debajo, INTACTO, el camino de siempre (campo
            // de seed + botón), ahora etiquetado "MODO CAÓTICO" y con el flag
            // explícitamente en `false` -- el modo de siempre NO CAMBIA DE
            // COMPORTAMIENTO (regla dura del contrato): solo cambió el texto
            // del botón y el flag que deja apagado.
            // =================================================================
            GUILayout.Space(UiStyles.S(18f));
            if (GUILayout.Button("SEMILLA CERO — tu primer taller", UiStyles.Boton, GUILayout.Height(UiStyles.S(38f))))
            {
                AlkahestGameBootstrap.ModoSemillaCero = true;
                RestartRun((int)Universe.SemillaCero);
            }

            GUILayout.Space(UiStyles.S(14f));
            var fileteModos = GUILayoutUtility.GetRect(10f, UiStyles.S(8f));
            if (Event.current.type == EventType.Repaint)
                UiStyles.FileteRombo(interior.width * 0.5f, fileteModos.y + fileteModos.height * 0.5f, interior.width * 0.4f, UiStyles.LatonOscuro);

            GUILayout.Label("Seed (vacía = aleatoria):", UiStyles.CuerpoTenue);
            _seedField = GUILayout.TextField(_seedField, 12, UiStyles.Campo);

            GUILayout.Space(UiStyles.S(10f));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("MODO CAÓTICO — entrar con esta semilla", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                AlkahestGameBootstrap.ModoSemillaCero = false;
                RestartRun(ParseSeedField());
            }
            // (ENCARGO M, CONTRATO_FASE_A.md §2) "botón AJUSTES bajo el
            // filete (entre MODO CAÓTICO y Salir)": literal, en la misma
            // fila, en medio de los otros dos.
            if (GUILayout.Button("AJUSTES", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                _ajustesAbiertos = true;
            }
            if (GUILayout.Button("Salir", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                QuitGame();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// (ENCARGO M) EL PANEL DE AJUSTES: un único método, reutilizado
        /// tanto desde el Título como desde la Pausa (ver <see cref="_ajustesAbiertos"/>)
        /// -- "el mismo panel" que pide el contrato. Dos sliders sobre los
        /// dos únicos números que este encargo controla: el volumen general
        /// del motor (<see cref="AudioListener.volume"/>, vía
        /// <see cref="SetVolGeneral"/>) y el multiplicador de efectos del
        /// taller (<see cref="DirectorDeAudio.VolumenEfectos"/>, propiedad
        /// estática -- funciona igual haya o no una instancia de
        /// DirectorDeAudio viva, ver su doc). Los dos se PERSISTEN en
        /// PlayerPrefs en el momento en que cambian (no solo al pulsar
        /// "listo"): cerrar el juego a media sesión no pierde el ajuste.
        /// </summary>
        private void DrawAjustes()
        {
            DrawFullscreenDim();
            UiStyles.Preparar();
            var interior = AbrirPanel(420f, 260f);

            GUILayout.Label(UiStyles.Espaciar("AJUSTES"), UiStyles.TituloGrande, GUILayout.Height(UiStyles.S(38f)));
            var filete = GUILayoutUtility.GetRect(10f, UiStyles.S(10f));
            if (Event.current.type == EventType.Repaint)
                UiStyles.FileteRombo(interior.width * 0.5f, filete.y + filete.height * 0.5f, interior.width * 0.55f, UiStyles.LatonOscuro);

            GUILayout.Space(UiStyles.S(14f));
            GUILayout.Label("Volumen general — " + Mathf.RoundToInt(VolGeneral * 100f) + "%", UiStyles.Cuerpo);
            float nuevoGeneral = GUILayout.HorizontalSlider(VolGeneral, 0f, 1f, UiStyles.Slider, UiStyles.SliderThumb,
                GUILayout.Height(UiStyles.S(20f)));
            if (!Mathf.Approximately(nuevoGeneral, _volGeneral)) SetVolGeneral(nuevoGeneral);

            GUILayout.Space(UiStyles.S(16f));
            float efectosActual = DirectorDeAudio.VolumenEfectos;
            GUILayout.Label("Efectos del taller — " + Mathf.RoundToInt(efectosActual * 100f) + "%", UiStyles.Cuerpo);
            float nuevoEfectos = GUILayout.HorizontalSlider(efectosActual, 0f, 1f, UiStyles.Slider, UiStyles.SliderThumb,
                GUILayout.Height(UiStyles.S(20f)));
            if (!Mathf.Approximately(nuevoEfectos, efectosActual)) DirectorDeAudio.VolumenEfectos = nuevoEfectos;

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("listo", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                _ajustesAbiertos = false;
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// (ENCARGO M) EL OVERLAY DE PAUSA: solo alcanzable durante Playing
        /// (ver <see cref="ManejarEscape"/>). Tres gestos -- REANUDAR (cierra
        /// la pausa sin más), AJUSTES (abre <see cref="DrawAjustes"/> sin
        /// cerrar la pausa: al volver de AJUSTES seguimos en pausa) y VOLVER
        /// AL TÍTULO (<see cref="VolverAlTitulo"/>, con su propia rama de un
        /// jugador/multi).
        /// </summary>
        private void DrawPausa()
        {
            DrawFullscreenDim();
            UiStyles.Preparar();
            AbrirPanel(360f, 240f);

            GUILayout.Label(UiStyles.Espaciar("PAUSA"), UiStyles.TituloGrande, GUILayout.Height(UiStyles.S(38f)));
            GUILayout.Space(UiStyles.S(6f));
            GUILayout.Label(_modoMulti
                ? "El taller compartido sigue corriendo para el resto -- esto solo te pausa a ti."
                : "La simulación está congelada.", UiStyles.CuerpoTenue);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("REANUDAR", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                _pausado = false;
            }
            GUILayout.Space(UiStyles.S(8f));
            if (GUILayout.Button("AJUSTES", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                _ajustesAbiertos = true;
            }
            GUILayout.Space(UiStyles.S(8f));
            if (GUILayout.Button("VOLVER AL TÍTULO", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                VolverAlTitulo();
            }

            GUILayout.EndArea();
        }

        private void DrawDayIntro()
        {
            DrawFullscreenDim();
            UiStyles.Preparar();
            // El panel crece los días en que el Maestro deja muestras: su párrafo
            // ocupa 2-3 líneas y no puede quedar apretado contra los encargos.
            //
            // (fix playtest 10, reclasificación de sustancias) MasterSupplies.TextoEntrega(2)
            // creció de 330 a 352 caracteres al describir las semillas del Maestro por ORIGEN
            // en vez de por nombre (ver el porqué en el doc-comment de ese método) -- con el
            // ancho interior real (540 - 2*18 de padding = 504px de diseño, Cuerpo a 13pt
            // ajustado con word-wrap) eso son ~5-6 líneas, una más que antes. GUILayout.
            // BeginArea RECORTA lo que no cabe (es un GUI.BeginGroup por debajo), así que un
            // desbordamiento no "se sale", se PIERDE en silencio -- 490 ya llevaba margen de
            // sobra (título 38 + entrega ~85 + "ENCARGOS DE HOY" ~22 + 1-3 encargos ~50 + aviso
            // de entrega ~14 + botón 34 + paddings ronda los ~260px, dejando ~230px de aire para
            // FlexibleSpace), así que 490 seguía sobrando; se sube a 510 solo como margen extra
            // de seguridad para ese texto más largo, no porque hiciera falta.
            AbrirPanel(540f, MasterSupplies.TextoEntrega(_day) != null ? 510f : 420f);

            GUILayout.Label($"Jornada {_day} de {TotalDays}", UiStyles.TituloGrande, GUILayout.Height(UiStyles.S(38f)));

            // Universe.EdictoDescripcion ya incluye el prefijo "El Maestro
            // murmura: ..." horneado (ver Universe.DescribeEdicto), así que
            // se muestra tal cual, sin volver a envolverlo.
            if (_day == 1 && _sim != null && _sim.Universe != null)
            {
                GUILayout.Label(_sim.Universe.EdictoDescripcion, UiStyles.Subtitulo);

                // (playtest 12) "AL ESCOGER OTRO UNIVERSO SOLO TUVE MÁS DE LO
                // MISMO": Sim/Universe.cs ya sortea por seed una frase corta de
                // carácter (Universe.CaracterDelUniverso, p.ej. "Un mundo de
                // carmines que serpentea.") -- horneada una vez en Create() y
                // hasta ahora solo consumida por Game/JournalHud.cs (cabecera
                // del diario). Se muestra AQUÍ, junto a la seed, y no en la
                // pantalla de Título: el Título puede tener cargado un Universe
                // "de usar y tirar" (si el campo de seed se deja en blanco,
                // RestartRun(null) sortea OTRA seed distinta al recargar la
                // escena -- ver AlkahestSim.Start), así que enseñar su carácter
                // ahí sería prometer un mundo que luego no es el que se juega.
                // La intro de la jornada 1 SÍ es el universo real de la partida
                // (AlkahestGameBootstrap.TrySpawn espera a que Universe/Grid
                // existan antes de crear este mismo DayCycle) -- es el momento
                // exacto de arranque de partida, la promesa "este mundo no es
                // el anterior". Estilo tenue (no Subtitulo, que ya usa el
                // Edicto): es un dato de contexto, no la voz del Maestro en sí.
                // Una sola línea corta -- comprobado que cabe: el panel de la
                // jornada 1 (420px de diseño, ver AbrirPanel unas líneas más
                // arriba) tiene ~230px de aire de sobra según el cálculo
                // documentado justo ahí (sin el bloque de "entrega" de la
                // jornada 2, que es el que obligó a subir el panel a 510 la
                // vez pasada); esta línea añade como mucho ~20-24px, muy por
                // debajo de ese margen.
                GUILayout.Space(UiStyles.S(3f));
                GUILayout.Label($"{_sim.Universe.CaracterDelUniverso} — seed {_sim.Universe.Seed}", UiStyles.CuerpoTenue);

                GUILayout.Space(UiStyles.S(8f));
            }

            // Anuncio de las muestras que el Maestro deja hoy (jornada 2): el
            // jugador tiene que ENTERARSE de que ahora tiene azoth, vivium y
            // semilla, o no buscará ninguno de los tres.
            string entrega = MasterSupplies.TextoEntrega(_day);
            if (entrega != null)
            {
                var estilo = UiStyles.Cuerpo;
                var previo = estilo.normal.textColor;
                estilo.normal.textColor = UiStyles.Oro;
                GUILayout.Label(entrega, estilo);
                estilo.normal.textColor = previo;
                GUILayout.Space(UiStyles.S(8f));
            }

            GUILayout.Label("ENCARGOS DE HOY", UiStyles.Titulo);
            GUILayout.Space(UiStyles.S(4f));
            if (_orderSystem != null)
            {
                var orders = _orderSystem.ActiveOrders;
                for (int i = 0; i < orders.Count; i++)
                {
                    GUILayout.Label($"• {orders[i].Descripcion}  (+{orders[i].Recompensa} Favor)", UiStyles.Cuerpo);
                    GUILayout.Space(UiStyles.S(4f));
                }
            }

            GUILayout.Space(UiStyles.S(6f));
            GUILayout.Label("Se entrega VERTIENDO en la Tolva: el hueco dorado del muro derecho.", UiStyles.CuerpoTenue);

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Comenzar jornada", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                EnterPlaying();
            }

            GUILayout.EndArea();
        }

        private void DrawPlayingHud()
        {
            // (integración pt48, VISTO EN VIVO) EL BOTÓN MENÚ: en el EDITOR,
            // la vista Game en modo "Play Focused" se COME la tecla Escape
            // antes de que llegue al Input System (verificado en la cabina:
            // la 'a' de moverse llega, Escape no) -- así que un jugador que
            // prueba en el editor, como Cesar, JAMÁS podía abrir la pausa
            // por teclado, y "los menús nuevos" del playtest 47 eran
            // invisibles para él. Un botón chico y clicable en la esquina
            // inferior derecha abre EXACTAMENTE la misma pausa; Escape sigue
            // funcionando donde el editor no estorba (la build del jugador).
            // Se dibuja SIEMPRE en Playing (también en multi, donde este
            // DayCycle vive solo para la pausa): que el camino al menú nunca
            // dependa de una tecla que alguien más puede comerse.
            {
                UiStyles.Preparar();
                float bw = UiStyles.S(96f), bh = UiStyles.S(26f);
                var rMenu = new Rect(Screen.width - bw - UiStyles.S(10f), Screen.height - bh - UiStyles.S(10f), bw, bh);
                if (GUI.Button(rMenu, "MENÚ · Esc", UiStyles.Boton)) _pausado = true;
            }

            // (tercera ronda del pivot) El aviso de "los encargos han
            // llegado" (ver ActualizarDesbloqueoDeEncargos/
            // DrawEncargosDesbloqueadosBanner) vive FUERA del `return` de
            // abajo a propósito: es independiente del reloj (no existe
            // reloj en este modo, ver el punto 4 más abajo) y tiene que
            // poder dibujarse aunque _timeRemaining sea infinito. Sigue
            // respetando HudSilenciado -- no se cuela antes de la primera
            // acción del jugador, igual que el resto del HUD de mundo.
            if (!HudSilenciado && _encargosBannerCountdown > 0f)
            {
                UiStyles.Preparar();
                DrawEncargosDesbloqueadosBanner();
            }

            // (playtest 21, EL PIVOT, punto 4 de "sesión sin reloj" del
            // contrato: "no dibujar el reloj"). `_timeRemaining` vive en
            // `+Infinity` en este modo (ver EnterPlaying) -- calcular
            // `Mathf.CeilToInt` de infinito y trocearlo en minutos:segundos
            // más abajo no tiene ningún sentido, así que se corta aquí. El
            // cuerpo del método NO SE BORRA (regla 26 de CLAUDE.md): sigue
            // íntegro debajo de este `return`, listo para cuando la sesión
            // cronometrada vuelva y `_timeRemaining` vuelva a ser un número
            // finito de verdad.
            if (float.IsPositiveInfinity(_timeRemaining)) return;

            UiStyles.Preparar();

            // El texto del reloj solo se reconstruye cuando cambia el segundo
            // (antes se creaba una cadena Y UN GUIStyle en cada frame).
            int totalSeconds = Mathf.CeilToInt(_timeRemaining);
            if (totalSeconds != _relojSegundos)
            {
                _relojSegundos = totalSeconds;
                _relojTexto = (totalSeconds / 60) + ":" + (totalSeconds % 60).ToString("00");
            }

            bool urgente = _timeRemaining <= 30f;
            float w = UiStyles.S(140f), h = UiStyles.S(38f);
            var r = new Rect((Screen.width - w) * 0.5f, UiStyles.S(8f), w, h);

            UiStyles.Panel(r, UiStyles.TintaFuerte, urgente ? UiStyles.Peligro : UiStyles.Borde);

            var estilo = UiStyles.Reloj;
            var previo = estilo.normal.textColor;
            estilo.normal.textColor = urgente ? UiStyles.Peligro : UiStyles.Texto;
            GUI.Label(r, _relojTexto, estilo);
            estilo.normal.textColor = previo;

            if (_allOrdersDoneAnnounced) DrawAllOrdersDoneBanner(r);
        }

        /// <summary>
        /// Aviso de cierre anticipado (restaurado playtest 9), justo debajo
        /// del reloj: legible desde cualquier punto del taller, con la cuenta
        /// atrás en vivo. El texto solo se reconstruye cuando cambia el
        /// segundo entero (mismo patrón que el reloj de arriba).
        /// </summary>
        private void DrawAllOrdersDoneBanner(Rect relojRect)
        {
            int segundos = Mathf.CeilToInt(Mathf.Max(0f, _allOrdersDoneCountdown));
            if (segundos != _cierreSegundos)
            {
                _cierreSegundos = segundos;
                _cierreTexto = "Todos los encargos entregados -- ENTER para cerrar la jornada (" + segundos + "s)";
            }

            float w = UiStyles.S(380f), h = UiStyles.S(28f);
            var r = new Rect((Screen.width - w) * 0.5f, relojRect.yMax + UiStyles.S(6f), w, h);

            UiStyles.Panel(r, UiStyles.TintaFuerte, UiStyles.Exito);
            GUI.Label(r, _cierreTexto, UiStyles.CuerpoCentrado);
        }

        /// <summary>
        /// (tercera ronda del pivot) "Que se note (es la recompensa de
        /// cavar)": aviso breve, arriba centrado (no hay reloj del que
        /// colgarse en este modo, así que va solo, en la misma posición que
        /// ocuparía el reloj clásico), cuando <see cref="TolvaAlcanzable"/>
        /// pasa a true por primera vez y el Maestro empieza a pedir
        /// encargos. Mismo idioma visual que <see cref="DrawAllOrdersDoneBanner"/>
        /// (panel de tinta con filete dorado, texto centrado), con su propia
        /// cuenta atrás (<see cref="EncargosDesbloqueadosBannerSeconds"/>)
        /// para no quedarse clavado en pantalla para siempre.
        /// </summary>
        private void DrawEncargosDesbloqueadosBanner()
        {
            float w = UiStyles.S(460f), h = UiStyles.S(30f);
            var r = new Rect((Screen.width - w) * 0.5f, UiStyles.S(8f), w, h);

            UiStyles.Panel(r, UiStyles.TintaFuerte, UiStyles.Oro);
            GUI.Label(r, "El Maestro te ha oído cavar -- primeros encargos disponibles.", UiStyles.CuerpoCentrado);
        }

        private void DrawDayEnd()
        {
            DrawFullscreenDim();
            UiStyles.Preparar();
            AbrirPanel(500f, 380f);

            GUILayout.Label($"Fin de la jornada {_day}", UiStyles.TituloGrande, GUILayout.Height(UiStyles.S(38f)));

            if (_orderSystem != null)
            {
                var orders = _orderSystem.ActiveOrders;
                for (int i = 0; i < orders.Count; i++)
                {
                    var o = orders[i];
                    string mark = o.Completado ? "✓" : "✗";
                    GUILayout.Label($"{mark} {o.Descripcion}  ({o.Progreso}/{o.MinCells})",
                        o.Completado ? UiStyles.Cuerpo : UiStyles.CuerpoTenue);
                    GUILayout.Space(UiStyles.S(3f));
                }

                GUILayout.Space(UiStyles.S(10f));
                GUILayout.Label($"Favor total: {_orderSystem.Favor} ★", UiStyles.Titulo);
            }

            if (_avisoDesatencion)
            {
                GUILayout.Space(UiStyles.S(6f));
                GUILayout.Label("Dos jornadas sin un solo encargo cumplido — el Maestro pierde la paciencia.",
                    UiStyles.Alerta);
            }

            GUILayout.FlexibleSpace();
            // Balance playtest 8: la partida siempre juega las 3 jornadas, así
            // que el botón solo cambia de texto en la última (ya no depende
            // de _avisoDesatencion, que dejó de cortar la partida).
            string nextLabel = _day >= TotalDays ? "Ver desenlace" : "Siguiente jornada";
            if (GUILayout.Button(nextLabel, UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                AdvanceAfterDayEnd();
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// Pantalla final (balance playtest 8): ya no es un binario victoria/
        /// derrota, sino uno de los 4 <see cref="OrderSystem.Desenlace"/>
        /// graduados por el Favor final (ver derivación de umbrales en
        /// OrderSystem). Colores: Despedido en rojo (Peligro), Aprendiz/
        /// Oficial en verde (Exito), Maestro en dorado (Oro) como remate
        /// visual del escalón máximo. Si no se llegó al máximo, se dice
        /// cuánto faltaba para el siguiente -- tono exigente pero justo, sin
        /// humillar: nunca "has fallado", siempre "os faltaron N ★".
        /// </summary>
        private void DrawEndScreen()
        {
            DrawFullscreenDim();

            int favorFinal = _orderSystem != null ? _orderSystem.Favor : 0;
            var desenlace = OrderSystem.DesenlaceParaFavor(favorFinal);

            UiStyles.Preparar();
            AbrirPanel(520f, 420f);

            string tituloTexto;
            string subtitulo;
            Color colorTitulo;
            switch (desenlace)
            {
                case OrderSystem.Desenlace.Maestro:
                    tituloTexto = "MAESTRO";
                    subtitulo = "El Maestro se inclina ante ti: eres maestro por derecho propio.";
                    colorTitulo = UiStyles.Oro;
                    break;
                case OrderSystem.Desenlace.Oficial:
                    tituloTexto = "OFICIAL";
                    subtitulo = "El Maestro te asciende a Oficial del taller: domina el oficio, y vuelve a por más.";
                    colorTitulo = UiStyles.Exito;
                    break;
                case OrderSystem.Desenlace.Aprendiz:
                    tituloTexto = "APRENDIZ";
                    subtitulo = "El Maestro te concede el título de Aprendiz: un comienzo sólido.";
                    colorTitulo = UiStyles.Exito;
                    break;
                default:
                    tituloTexto = "DESPEDIDO";
                    subtitulo = "El Maestro te despide del taller: esperaba más disciplina de ti.";
                    colorTitulo = UiStyles.Peligro;
                    break;
            }

            // (playtest 31) El desenlace se graba, no se imprime: Cinzel
            // espaciado, igual que el título del juego -- son las dos únicas
            // veces que el juego habla en mayúsculas de piedra.
            var titulo = UiStyles.TituloGrande;
            var previo = titulo.normal.textColor;
            titulo.normal.textColor = colorTitulo;
            GUILayout.Label(UiStyles.Espaciar(tituloTexto), titulo, GUILayout.Height(UiStyles.S(42f)));
            titulo.normal.textColor = previo;

            GUILayout.Label(subtitulo, UiStyles.Subtitulo);

            // Cuánto faltaba para el siguiente escalón (nada si ya se llegó a
            // Maestro, el máximo): tono justo, nunca humillante.
            if (OrderSystem.TryGetNextTier(favorFinal, out int siguienteUmbral, out string siguienteNombre))
            {
                int faltan = siguienteUmbral - favorFinal;
                GUILayout.Space(UiStyles.S(4f));
                GUILayout.Label($"Te faltaron {faltan} ★ para el siguiente escalón ({siguienteNombre}).",
                    UiStyles.CuerpoTenue);
            }

            GUILayout.Space(UiStyles.S(10f));
            GUILayout.Label($"Seed: {(_sim != null && _sim.Universe != null ? _sim.Universe.Seed.ToString() : "?")}", UiStyles.Cuerpo);
            GUILayout.Label($"Materiales descubiertos: {(_knowledge != null ? _knowledge.CountDiscovered() : 0)}", UiStyles.Cuerpo);
            GUILayout.Label($"Materiales bautizados: {(_knowledge != null ? _knowledge.CountNamed() : 0)}", UiStyles.Cuerpo);
            GUILayout.Label($"Favor final: {favorFinal} ★", UiStyles.Titulo);

            // (playtest 12) "AL ESCOGER OTRO UNIVERSO SOLO TUVE MÁS DE LO
            // MISMO": la promesa se cumple ahora en Sim/Universe.cs (seed nueva
            // -> Edicto/leyes/firma visual/CaracterDelUniverso nuevos, ver
            // DrawDayIntro), pero el jugador tiene que SABER que "Nuevo
            // universo" cambia algo antes de pulsarlo, sin destriparle el
            // sorteo (ni siquiera se nombra el Edicto ni la firma aquí, solo
            // se promete variación). Una línea sobria, junto al botón que la
            // cumple -- no se toca "Reintentar mismo universo", que
            // deliberadamente NO varía (misma seed, mismo Universe.Create).
            GUILayout.Space(UiStyles.S(6f));
            GUILayout.Label("\"Nuevo universo\" sortea otra seed y otro carácter -- nunca repite este mundo.",
                UiStyles.CuerpoTenue);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reintentar mismo universo", UiStyles.Boton, GUILayout.Height(UiStyles.S(32f))))
            {
                int? seed = (_sim != null && _sim.Universe != null) ? _sim.Universe.Seed : (int?)null;
                RestartRun(seed);
            }
            if (GUILayout.Button("Nuevo universo", UiStyles.Boton, GUILayout.Height(UiStyles.S(32f))))
            {
                // (integración pt40, SEMILLA CERO) "Nuevo universo" = seed
                // sorteada = el arco de autor ya no aplica: se apaga el modo
                // aquí para que la partida nueva sea CAÓTICO limpio (sin
                // tapiados de un guion que no va a correr). "Reintentar
                // mismo universo" NO lo toca a propósito: misma seed = el
                // arco entero se puede volver a jugar.
                AlkahestGameBootstrap.ModoSemillaCero = false;
                RestartRun(null);
            }
            if (GUILayout.Button("Salir", UiStyles.Boton, GUILayout.Height(UiStyles.S(32f))))
            {
                QuitGame();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}
