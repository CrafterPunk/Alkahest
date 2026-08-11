using UnityEngine;
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

        // Nota IMGUI: los overlays de esta clase usan GUILayout.BeginArea
        // (no GUILayout.Window), así que no necesitan un id constante --
        // BeginArea no lo pide. Los HUD que sí usan Window (FlaskHud,
        // DevPalette, NamingUi, JournalHud) siguen la convención de ids
        // constantes del proyecto.

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

        private Phase _phase;
        private int _day = 1;
        private float _timeRemaining;
        private int _consecutiveZeroOrderDays;
        private bool _earlyLose;

        private string _seedField = "";

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, OrderSystem orderSystem, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _orderSystem = orderSystem;
            _knowledge = knowledge;

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

            if (_phase == Phase.Playing)
            {
                _timeRemaining -= Time.deltaTime;
                if (_timeRemaining <= 0f)
                {
                    _timeRemaining = 0f;
                    EnterDayEnd();
                }
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
            _phase = Phase.DayIntro;
        }

        private void EnterPlaying()
        {
            _timeRemaining = DayDurationSeconds;
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

        private void DrawTitle()
        {
            DrawFullscreenDim();

            const float w = 460f, h = 300f;
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUILayout.BeginArea(box, GUI.skin.box);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 34,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            GUILayout.Label("CHAOS ALCHEMY", titleStyle, GUILayout.Height(48f));

            var taglineStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic,
            };
            GUILayout.Label("Domestica las leyes de un universo extraño", taglineStyle);

            GUILayout.Space(16f);
            GUILayout.Label("Seed (vacía = aleatoria):");
            _seedField = GUILayout.TextField(_seedField, 12);

            GUILayout.Space(12f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Entrar al taller", GUILayout.Height(32f)))
            {
                RestartRun(ParseSeedField());
            }
            if (GUILayout.Button("Salir", GUILayout.Height(32f)))
            {
                QuitGame();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawDayIntro()
        {
            DrawFullscreenDim();

            const float w = 520f, h = 420f;
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUILayout.BeginArea(box, GUI.skin.box);

            var headStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            GUILayout.Label($"Jornada {_day} de {TotalDays}", headStyle, GUILayout.Height(36f));

            // Universe.EdictoDescripcion ya incluye el prefijo "El Maestro
            // murmura: ..." horneado (ver Universe.DescribeEdicto), así que
            // se muestra tal cual, sin volver a envolverlo.
            if (_day == 1 && _sim != null && _sim.Universe != null)
            {
                var rumorStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Italic, wordWrap = true };
                GUILayout.Label(_sim.Universe.EdictoDescripcion, rumorStyle);
                GUILayout.Space(8f);
            }

            GUILayout.Label("Encargos de hoy:");
            if (_orderSystem != null)
            {
                var orders = _orderSystem.ActiveOrders;
                for (int i = 0; i < orders.Count; i++)
                {
                    GUILayout.Label($"• {orders[i].Descripcion}  (+{orders[i].Recompensa} Favor)");
                }
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Comenzar jornada", GUILayout.Height(34f)))
            {
                EnterPlaying();
            }

            GUILayout.EndArea();
        }

        private void DrawPlayingHud()
        {
            int totalSeconds = Mathf.CeilToInt(_timeRemaining);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            Rect r = new Rect(Screen.width * 0.5f - 80f, 10f, 160f, 34f);
            GUI.Box(r, GUIContent.none);
            GUI.Label(r, $"{minutes}:{seconds:00}", style);
        }

        private void DrawDayEnd()
        {
            DrawFullscreenDim();

            const float w = 480f, h = 380f;
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUILayout.BeginArea(box, GUI.skin.box);

            var headStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            GUILayout.Label($"Fin de la jornada {_day}", headStyle, GUILayout.Height(36f));

            if (_orderSystem != null)
            {
                var orders = _orderSystem.ActiveOrders;
                for (int i = 0; i < orders.Count; i++)
                {
                    var o = orders[i];
                    string mark = o.Completado ? "✓" : "✗";
                    GUILayout.Label($"{mark} {o.Descripcion}  ({o.Progreso}/{o.MinCells})");
                }

                GUILayout.Space(10f);
                GUILayout.Label($"Favor total: {_orderSystem.Favor} ★");
            }

            if (_earlyLose)
            {
                GUILayout.Space(6f);
                var warnStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, wordWrap = true };
                GUILayout.Label("Dos jornadas sin un solo encargo cumplido -- el Maestro pierde la paciencia.", warnStyle);
            }

            GUILayout.FlexibleSpace();
            string nextLabel = (_earlyLose || _day >= TotalDays) ? "Ver desenlace" : "Siguiente jornada";
            if (GUILayout.Button(nextLabel, GUILayout.Height(34f)))
            {
                AdvanceAfterDayEnd();
            }

            GUILayout.EndArea();
        }

        private void DrawEndScreen()
        {
            DrawFullscreenDim();

            bool win = !_earlyLose && _orderSystem != null && _orderSystem.Favor >= OrderSystem.WinFavorTarget;

            const float w = 520f, h = 400f;
            Rect box = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUILayout.BeginArea(box, GUI.skin.box);

            var headStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };
            GUILayout.Label(win ? "VICTORIA" : "DERROTA", headStyle, GUILayout.Height(40f));

            var msgStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Italic,
                wordWrap = true,
                alignment = TextAnchor.MiddleCenter,
            };
            GUILayout.Label(win
                ? "El Maestro asiente. El universo os pertenece."
                : "El Maestro os expulsa del taller.", msgStyle);

            GUILayout.Space(10f);
            GUILayout.Label($"Seed: {(_sim != null && _sim.Universe != null ? _sim.Universe.Seed.ToString() : "?")}");
            GUILayout.Label($"Materiales descubiertos: {(_knowledge != null ? _knowledge.CountDiscovered() : 0)}");
            GUILayout.Label($"Materiales bautizados: {(_knowledge != null ? _knowledge.CountNamed() : 0)}");
            GUILayout.Label($"Favor final: {(_orderSystem != null ? _orderSystem.Favor : 0)} ★");

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reintentar mismo universo", GUILayout.Height(32f)))
            {
                int? seed = (_sim != null && _sim.Universe != null) ? _sim.Universe.Seed : (int?)null;
                RestartRun(seed);
            }
            if (GUILayout.Button("Nuevo universo", GUILayout.Height(32f)))
            {
                RestartRun(null);
            }
            if (GUILayout.Button("Salir", GUILayout.Height(32f)))
            {
                QuitGame();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}
