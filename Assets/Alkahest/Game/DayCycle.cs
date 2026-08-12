using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    /// Derrota anticipada: si dos jornadas SEGUIDAS terminan sin un solo
    /// encargo completado, la jornada 3 (si se llega a jugar) desemboca
    /// igualmente en la pantalla de derrota. Victoria: Favor &gt;=
    /// OrderSystem.WinFavorTarget tras la jornada 3 (y sin derrota
    /// anticipada).
    /// </summary>
    public sealed class DayCycle : MonoBehaviour
    {
        private enum Phase { Title, DayIntro, Playing, DayEnd, EndScreen }

        public const int TotalDays = 3;
        private const float DayDurationSeconds = 6f * 60f; // "6:00" en el HUD.

        // (fix playtest 6: "al acabar las metas no termino el nivel") Cuando
        // OrderSystem confirma que ya no queda NINGÚN encargo activo por
        // entregar, la jornada no se corta en seco -- el jugador puede seguir
        // experimentando -- pero arranca esta cuenta atrás y un aviso en
        // pantalla; a los 12 s (o antes si el jugador pulsa ENTER) se cierra
        // la jornada por el MISMO camino que el fin por temporizador
        // (EnterDayEnd), así que no hay dos formas distintas de terminar el día.
        private const float GoalsMetCountdownSeconds = 12f;
        private const string GoalsMetMensajeBase = "TODOS LOS ENCARGOS ENTREGADOS · pulsa ENTER para cerrar la jornada (";

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

        private AlkahestSim _sim;
        private OrderSystem _orderSystem;
        private SubstanceKnowledge _knowledge;
        private MasterSupplies _supplies;
        private HintSystem _hints;

        private Phase _phase;
        private int _day = 1;
        private float _timeRemaining;
        private int _consecutiveZeroOrderDays;
        private bool _earlyLose;

        private string _seedField = "";

        // Caché del reloj de la jornada (ver DrawPlayingHud).
        private int _relojSegundos = -1;
        private string _relojTexto = "";

        // Estado de "metas cumplidas" (fix playtest 6) y caché de su texto,
        // igual que el reloj: la cuenta atrás es en segundos enteros y solo
        // cambia una vez por segundo, así que no hace falta reconstruir la
        // cadena en cada frame de OnGUI.
        private bool _goalsMet;
        private float _goalsMetCountdown;
        private int _goalsMetSegundos = -1;
        private string _goalsMetTexto = "";

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, OrderSystem orderSystem, SubstanceKnowledge knowledge,
            MasterSupplies supplies = null, HintSystem hints = null)
        {
            _sim = sim;
            _orderSystem = orderSystem;
            _knowledge = knowledge;
            _supplies = supplies;
            _hints = hints;

            if (_skipTitleOnLoad)
            {
                _skipTitleOnLoad = false;
                EnterDayIntro(1);
            }
            else
            {
                EnterTitle();
            }
        }

        private void Update()
        {
            ApplyPause();

            if (_phase != Phase.Playing) return;

            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                EnterDayEnd();
                return; // el reloj manda: si llega a cero este frame, no hace
                        // falta evaluar encima la cuenta atrás de metas cumplidas.
            }

            UpdateGoalsMetCountdown();
        }

        /// <summary>
        /// (fix playtest 6) Detecta que OrderSystem ya no tiene ningún encargo
        /// activo pendiente (AllOrdersCompleted, la API pública que ya expone
        /// OrderSystem para esto -- no hizo falta tocar ese archivo) y arranca
        /// la cuenta atrás de cierre. Reutiliza EnterDayEnd() -- el mismo
        /// método que usa el fin de jornada por temporizador -- tanto si el
        /// jugador pulsa ENTER como si se agotan los 12 s, para que exista un
        /// único camino de código hacia el fin de día.
        /// </summary>
        private void UpdateGoalsMetCountdown()
        {
            if (!_goalsMet)
            {
                // El guard de Count > 0 evita que un día sin encargos activos
                // (no debería darse, pero AllOrdersCompleted() de una lista
                // vacía es trivialmente true) dispare el aviso desde el segundo 0.
                bool todosEntregados = _orderSystem != null
                    && _orderSystem.ActiveOrders.Count > 0
                    && _orderSystem.AllOrdersCompleted();
                if (!todosEntregados) return;

                _goalsMet = true;
                _goalsMetCountdown = GoalsMetCountdownSeconds;
                _goalsMetSegundos = -1; // fuerza reconstruir el texto cacheado ya.
            }

            // Regla de diseño: si entregas en el último momento con menos
            // tiempo de jornada que de cuenta atrás, gana el que ocurra antes
            // -- nunca alargamos la jornada por culpa de este aviso. Basta con
            // acotar la cuenta atrás al tiempo que de verdad queda; si ambas
            // llegan a cero en el mismo frame, el chequeo de _timeRemaining de
            // más arriba en Update() ya cerró el día antes de llegar aquí.
            _goalsMetCountdown -= Time.deltaTime;
            if (_goalsMetCountdown > _timeRemaining) _goalsMetCountdown = _timeRemaining;

            var kb = Keyboard.current;
            bool enterPulsado = kb != null
                && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);

            if (enterPulsado || _goalsMetCountdown <= 0f)
            {
                EnterDayEnd();
            }
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
            _timeRemaining = DayDurationSeconds;

            // Reinicio del estado de "metas cumplidas" (fix playtest 6): cada
            // jornada nueva debe volver a evaluarse desde cero, si no el aviso
            // de la jornada anterior (o su cuenta atrás ya agotada) se
            // arrastraría al día siguiente.
            _goalsMet = false;
            _goalsMetCountdown = 0f;
            _goalsMetSegundos = -1;

            _phase = Phase.Playing;
        }

        private void EnterDayEnd()
        {
            _phase = Phase.DayEnd;

            int completed = _orderSystem != null ? _orderSystem.CompletedCount() : 0;
            if (completed == 0) _consecutiveZeroOrderDays++;
            else _consecutiveZeroOrderDays = 0;

            if (_consecutiveZeroOrderDays >= 2) _earlyLose = true;
        }

        private void AdvanceAfterDayEnd()
        {
            if (_earlyLose || _day >= TotalDays)
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
            AbrirPanel(540f, MasterSupplies.TextoEntrega(_day) != null ? 490f : 420f);

            GUILayout.Label($"Jornada {_day} de {TotalDays}", UiStyles.TituloGrande, GUILayout.Height(UiStyles.S(38f)));

            // Universe.EdictoDescripcion ya incluye el prefijo "El Maestro
            // murmura: ..." horneado (ver Universe.DescribeEdicto), así que
            // se muestra tal cual, sin volver a envolverlo.
            if (_day == 1 && _sim != null && _sim.Universe != null)
            {
                GUILayout.Label(_sim.Universe.EdictoDescripcion, UiStyles.Subtitulo);
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

            DrawGoalsMetBanner();
        }

        /// <summary>
        /// (fix playtest 6) Aviso sobrio bajo el reloj cuando ya no queda
        /// ningún encargo activo por entregar: por qué existe, ver
        /// UpdateGoalsMetCountdown. Solo puede llegar a dibujarse desde
        /// DrawPlayingHud (caso Phase.Playing del switch de OnGUI), fase en la
        /// que InputLocked siempre es false -- así que el aviso nunca puede
        /// coincidir con Título/intro/DayEnd/EndScreen; el chequeo explícito de
        /// abajo es solo un cinturón de seguridad si algún día se llama desde
        /// otro sitio.
        /// </summary>
        private void DrawGoalsMetBanner()
        {
            if (!_goalsMet || InputLocked) return;

            int segundos = Mathf.CeilToInt(_goalsMetCountdown);
            if (segundos != _goalsMetSegundos)
            {
                _goalsMetSegundos = segundos;
                _goalsMetTexto = GoalsMetMensajeBase + Mathf.Max(0, segundos) + "s)";
            }

            float w = UiStyles.S(460f), h = UiStyles.S(28f);
            var r = new Rect((Screen.width - w) * 0.5f, UiStyles.S(52f), w, h);
            UiStyles.Panel(r, UiStyles.TintaFuerte, UiStyles.Oro);
            GUI.Label(r, _goalsMetTexto, UiStyles.Alerta);
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

            if (_earlyLose)
            {
                GUILayout.Space(UiStyles.S(6f));
                GUILayout.Label("Dos jornadas sin un solo encargo cumplido — el Maestro pierde la paciencia.",
                    UiStyles.Alerta);
            }

            GUILayout.FlexibleSpace();
            string nextLabel = (_earlyLose || _day >= TotalDays) ? "Ver desenlace" : "Siguiente jornada";
            if (GUILayout.Button(nextLabel, UiStyles.Boton, GUILayout.Height(UiStyles.S(34f))))
            {
                AdvanceAfterDayEnd();
            }

            GUILayout.EndArea();
        }

        private void DrawEndScreen()
        {
            DrawFullscreenDim();

            bool win = !_earlyLose && _orderSystem != null && _orderSystem.Favor >= OrderSystem.WinFavorTarget;

            UiStyles.Preparar();
            AbrirPanel(520f, 400f);

            var titulo = UiStyles.TituloGrande;
            var previo = titulo.normal.textColor;
            titulo.normal.textColor = win ? UiStyles.Exito : UiStyles.Peligro;
            GUILayout.Label(win ? "VICTORIA" : "DERROTA", titulo, GUILayout.Height(UiStyles.S(42f)));
            titulo.normal.textColor = previo;

            GUILayout.Label(win
                ? "El Maestro asiente. El universo os pertenece."
                : "El Maestro os expulsa del taller.", UiStyles.Subtitulo);

            GUILayout.Space(UiStyles.S(10f));
            GUILayout.Label($"Seed: {(_sim != null && _sim.Universe != null ? _sim.Universe.Seed.ToString() : "?")}", UiStyles.Cuerpo);
            GUILayout.Label($"Materiales descubiertos: {(_knowledge != null ? _knowledge.CountDiscovered() : 0)}", UiStyles.Cuerpo);
            GUILayout.Label($"Materiales bautizados: {(_knowledge != null ? _knowledge.CountNamed() : 0)}", UiStyles.Cuerpo);
            GUILayout.Label($"Favor final: {(_orderSystem != null ? _orderSystem.Favor : 0)} ★", UiStyles.Titulo);

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
