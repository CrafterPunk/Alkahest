using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Alkahest.Sim;

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

        /// <summary>
        /// [playtest 24, LA MAREA] El desenlace del ARCO DE LA MAREA (ver
        /// <see cref="TerminarPartida"/>), independiente del desenlace
        /// clásico por Favor (<see cref="OrderSystem.Desenlace"/>, que sigue
        /// intacto -- ver <see cref="DrawEndScreen"/>). `Ninguno` es el
        /// default: mientras nadie llame a TerminarPartida, EndScreen sigue
        /// dibujando el desenlace clásico de siempre.
        /// </summary>
        private enum DesenlaceMarea { Ninguno, Victoria, Derrota }

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

        private AlkahestSim _sim;
        private OrderSystem _orderSystem;
        private SubstanceKnowledge _knowledge;
        private MasterSupplies _supplies;
        private HintSystem _hints;

        private Phase _phase;

        /// <summary>[playtest 24, LA MAREA] Ver <see cref="TerminarPartida"/>/<see cref="DesenlaceMarea"/>. Ninguno mientras el arco de la marea no haya decidido nada.</summary>
        private DesenlaceMarea _desenlaceMarea = DesenlaceMarea.Ninguno;

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
            HudSilenciado = true;
            // (playtest 24, LA MAREA) igual de explícito, aunque una escena
            // recién cargada ya nace con el enum en Ninguno por defecto: una
            // partida nueva no puede heredar el desenlace de la anterior.
            _desenlaceMarea = DesenlaceMarea.Ninguno;

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
            ApplyPause();

            if (_phase == Phase.Playing)
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
                // Lo único que SÍ sigue corriendo cada frame de Playing: la
                // detección de la primera acción del jugador, que apaga
                // HudSilenciado (ver su docblock), y el desbloqueo de
                // encargos al cavar hasta la Tolva (tercera ronda del pivot,
                // ver ActualizarDesbloqueoDeEncargos -- este último SÍ está
                // throttleado internamente, no corre de verdad cada frame).
                DetectarPrimeraAccion();
                ActualizarDesbloqueoDeEncargos();
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
            bool locked = _phase != Phase.Playing;
            InputLocked = locked;
            if (_sim != null) _sim.Paused = locked;
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

        /// <summary>
        /// [playtest 24, LA MAREA -- CONTRATO_MAREA.md §4.4] Cierre de
        /// partida del arco de la marea: lo llama Game/MareaDirector.cs (el
        /// otro archivo de este mismo encargo) cuando el Rocío alcanza el
        /// corazón (victoria) o la marea despierta engulle a la última
        /// criatura (derrota). Salta DIRECTO a Phase.EndScreen -- el cuarto
        /// íntimo ya vive fuera del ciclo clásico Título-&gt;3 jornadas-&gt;
        /// final (ver EnterCuartoIntimoSilencioso), así que no hay ninguna
        /// jornada que cerrar antes de mostrar el desenlace. DrawEndScreen
        /// dibuja el desenlace de LA MAREA en vez del de Favor mientras
        /// <see cref="_desenlaceMarea"/> no sea Ninguno -- el desenlace
        /// clásico se conserva íntegro (regla 26 de CLAUDE.md) para cuando
        /// la sesión cronometrada vuelva.
        /// </summary>
        public void TerminarPartida(bool victoria)
        {
            _desenlaceMarea = victoria ? DesenlaceMarea.Victoria : DesenlaceMarea.Derrota;
            _phase = Phase.EndScreen;
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
            AbrirPanel(460f, 300f);

            GUILayout.Label("CHAOS ALCHEMY", UiStyles.TituloGrande, GUILayout.Height(UiStyles.S(50f)));
            GUILayout.Label("Domestica las leyes de un universo extraño", UiStyles.Subtitulo);

            GUILayout.Space(UiStyles.S(16f));
            GUILayout.Label("Seed (vacía = aleatoria):", UiStyles.CuerpoTenue);
            _seedField = GUILayout.TextField(_seedField, 12, UiStyles.Campo);

            GUILayout.Space(UiStyles.S(12f));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Entrar al taller", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                RestartRun(ParseSeedField());
            }
            if (GUILayout.Button("Salir", UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                QuitGame();
            }
            GUILayout.EndHorizontal();

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
            GUI.Label(r, "El Maestro os ha oído cavar -- primeros encargos disponibles.", UiStyles.CuerpoCentrado);
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

            // [playtest 24, LA MAREA] El desenlace del ARCO DE LA MAREA
            // manda sobre el clásico por Favor en cuanto TerminarPartida lo
            // fija -- ver el docblock de ese método. El resto de este
            // método (desenlace por Favor, 4 escalones) se queda íntegro
            // debajo, sin tocar, para el modo clásico.
            if (_desenlaceMarea != DesenlaceMarea.Ninguno)
            {
                DrawEndScreenMarea();
                return;
            }

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
                    subtitulo = "El Maestro se inclina ante vosotros: sois maestros por derecho propio.";
                    colorTitulo = UiStyles.Oro;
                    break;
                case OrderSystem.Desenlace.Oficial:
                    tituloTexto = "OFICIAL";
                    subtitulo = "El Maestro os asciende a Oficial del taller: dominad el oficio, y volved a por más.";
                    colorTitulo = UiStyles.Exito;
                    break;
                case OrderSystem.Desenlace.Aprendiz:
                    tituloTexto = "APRENDIZ";
                    subtitulo = "El Maestro os concede el título de Aprendiz: un comienzo sólido.";
                    colorTitulo = UiStyles.Exito;
                    break;
                default:
                    tituloTexto = "DESPEDIDO";
                    subtitulo = "El Maestro os despide del taller: esperaba más disciplina de vosotros.";
                    colorTitulo = UiStyles.Peligro;
                    break;
            }

            var titulo = UiStyles.TituloGrande;
            var previo = titulo.normal.textColor;
            titulo.normal.textColor = colorTitulo;
            GUILayout.Label(tituloTexto, titulo, GUILayout.Height(UiStyles.S(42f)));
            titulo.normal.textColor = previo;

            GUILayout.Label(subtitulo, UiStyles.Subtitulo);

            // Cuánto faltaba para el siguiente escalón (nada si ya se llegó a
            // Maestro, el máximo): tono justo, nunca humillante.
            if (OrderSystem.TryGetNextTier(favorFinal, out int siguienteUmbral, out string siguienteNombre))
            {
                int faltan = siguienteUmbral - favorFinal;
                GUILayout.Space(UiStyles.S(4f));
                GUILayout.Label($"Os faltaron {faltan} ★ para el siguiente escalón ({siguienteNombre}).",
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
                RestartRun(null);
            }
            if (GUILayout.Button("Salir", UiStyles.Boton, GUILayout.Height(UiStyles.S(32f))))
            {
                QuitGame();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        /// <summary>
        /// [playtest 24, LA MAREA -- CONTRATO_MAREA.md §4.4] Las DOS
        /// pantallas finales del arco de la marea. TEXTOS LITERALES del
        /// contrato, palabra por palabra -- la voz de bitácora de este
        /// proyecto no reescribe ni "mejora" un texto que ya viene cerrado
        /// entre los dos encargos paralelos de esta ronda. Debajo, las
        /// MISMAS stats (seed, descubiertos, bautizados -- sin Favor, que no
        /// significa nada en este arco) y los MISMOS botones de reinicio que
        /// ya usa el desenlace clásico, mismo AbrirPanel/mismo idioma visual.
        /// </summary>
        private void DrawEndScreenMarea()
        {
            UiStyles.Preparar();
            AbrirPanel(520f, 380f);

            bool victoria = _desenlaceMarea == DesenlaceMarea.Victoria;
            string tituloTexto = victoria ? "EL MUNDO SE AQUIETA" : "LA MAREA OS TRAGÓ";
            string subtitulo = victoria
                ? "El Rocío alcanzó el corazón. La marea se retira a dormir, y por primera vez el taller respira. Vosotros, y lo que criasteis, sois la razón."
                : "La última criatura se apagó bajo la marea. El mundo terminó de digerirse a sí mismo, y nadie quedó para masticar en dirección contraria.";
            Color colorTitulo = victoria ? UiStyles.Oro : UiStyles.Peligro;

            var titulo = UiStyles.TituloGrande;
            var previo = titulo.normal.textColor;
            titulo.normal.textColor = colorTitulo;
            GUILayout.Label(tituloTexto, titulo, GUILayout.Height(UiStyles.S(42f)));
            titulo.normal.textColor = previo;

            GUILayout.Label(subtitulo, UiStyles.Subtitulo);

            GUILayout.Space(UiStyles.S(10f));
            GUILayout.Label($"Seed: {(_sim != null && _sim.Universe != null ? _sim.Universe.Seed.ToString() : "?")}", UiStyles.Cuerpo);
            GUILayout.Label($"Materiales descubiertos: {(_knowledge != null ? _knowledge.CountDiscovered() : 0)}", UiStyles.Cuerpo);
            GUILayout.Label($"Materiales bautizados: {(_knowledge != null ? _knowledge.CountNamed() : 0)}", UiStyles.Cuerpo);

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reintentar mismo universo", UiStyles.Boton, GUILayout.Height(UiStyles.S(32f))))
            {
                int? seed = (_sim != null && _sim.Universe != null) ? _sim.Universe.Seed : (int?)null;
                RestartRun(seed);
            }
            if (GUILayout.Button("Nuevo universo", UiStyles.Boton, GUILayout.Height(UiStyles.S(32f))))
            {
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
