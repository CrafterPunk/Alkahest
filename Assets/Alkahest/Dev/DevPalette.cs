using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;
using Alkahest.Game;

namespace Alkahest.Dev
{
    /// <summary>
    /// Overlay de desarrollo (IMGUI) para pintar materiales en la grilla y
    /// depurar la simulación en tiempo real. Activo siempre en el Editor o
    /// en builds de desarrollo (Application.isEditor || Debug.isDebugBuild),
    /// sin necesidad de un define de compilación aparte.
    ///
    /// F3 = mostrar/ocultar ventana (empieza CERRADA y recuerda tu elección).
    /// P  = pausa/reanuda (Time.timeScale 0 &lt;-&gt; anterior).
    /// N  = un solo tick de simulación (útil en pausa).
    /// LMB = pintar el material seleccionado. RMB = borrar (Empty).
    ///
    /// El pincel SOLO existe con la ventana abierta: con la paleta cerrada esta
    /// clase no toca la grilla bajo ningún concepto (ver UpdateHoverAndPaint),
    /// porque si no se mezcla con el Frasco del jugador.
    ///
    /// Usa exclusivamente Keyboard.current / Mouse.current del nuevo Input
    /// System; UnityEngine.Input (legacy) NUNCA se usa en este proyecto.
    /// </summary>
    [RequireComponent(typeof(AlkahestSim))]
    public sealed class DevPalette : MonoBehaviour
    {
        // Id constante (NO GetInstanceID) para GUILayout.Window, tal y como
        // exige la guía del proyecto.
        private const int WindowId = 837465;

        private AlkahestSim _sim;
        private bool _visible;
        /// <summary>Abierta = el ratón pinta materiales; el Frasco debe ignorar sus clics.</summary>
        public static bool IsOpen { get; private set; }
        private const string PrefKey = "ChaosAlchemy_DevPalette";

        // Arranca por debajo del panel del frasco (arriba-izquierda) para no
        // taparlo al abrirse; sigue siendo arrastrable.
        private Rect _windowRect = new Rect(12, 180, 300, 480);

        private byte _selectedMaterial = MaterialId.Sand;
        private float _brushRadius = 3f;
        private float _lastTimeScale = 1f;

        private Vector2Int _hoverCell;
        private bool _hoverValid;

        private void Awake()
        {
            _sim = GetComponent<AlkahestSim>();

            // (fix playtest 2) Antes empezaba visible y su pincel (p.ej. Empty/Sand) se mezclaba
            // con el Frasco: "aspirar borra todo", "tiro arena sin querer". Ahora empieza cerrada
            // y recuerda tu última elección entre sesiones.
            _visible = PlayerPrefs.GetInt(PrefKey, 0) == 1;
            IsOpen = _visible && IsDevBuild();
        }

        private void Update()
        {
            if (!IsDevBuild()) return;

            var kb = Keyboard.current;
            if (kb != null)
            {
                // (fix playtest 10) F3/P/N son atajos de una sola tecla como cualquier otro
                // del proyecto: no pueden robarle letras al campo de bautizar
                // (UiStyles.EscribiendoTexto), y son atajos del MUNDO -- con el diario abierto
                // a pantalla completa (JournalHud.Abierto) tampoco tiene sentido abrir/cerrar
                // la paleta ni pausar/step-ear la sim por debajo del libro. IsOpen se sigue
                // actualizando fuera del guard: solo refleja _visible, que no cambia mientras
                // el toggle está callado.
                bool bloqueado = UiStyles.EscribiendoTexto || JournalHud.Abierto;
                if (!bloqueado)
                {
                    if (kb.f3Key.wasPressedThisFrame)
                    {
                        _visible = !_visible;
                        PlayerPrefs.SetInt(PrefKey, _visible ? 1 : 0);
                    }
                    if (kb.pKey.wasPressedThisFrame) TogglePause();
                    if (kb.nKey.wasPressedThisFrame) _sim.StepOnce();
                }
                IsOpen = _visible && IsDevBuild();
            }

            UpdateHoverAndPaint();
        }

        private static bool IsDevBuild() => Application.isEditor || Debug.isDebugBuild;

        private void TogglePause()
        {
            if (Time.timeScale > 0f)
            {
                _lastTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                Time.timeScale = _lastTimeScale > 0f ? _lastTimeScale : 1f;
            }
        }

        private void UpdateHoverAndPaint()
        {
            _hoverValid = false;

            // (fix playtest 3) EL BUG DE "TIRA ARENA COMO LOCO": esto se ejecutaba
            // SIEMPRE, también con la paleta cerrada, así que el clic izquierdo
            // pintaba arena a la vez que el Frasco aspiraba ("aspirar rompe el
            // mundo") y el clic derecho borraba celdas mientras se vertía. Con la
            // paleta cerrada el pincel no existe: ni pinta, ni borra, ni hace hover.
            if (!_visible) return;

            // (fix playtest 10) El pincel de dev también es input del MUNDO: se calla
            // mientras se escribe un nombre o con el diario abierto a pantalla completa
            // (mismo criterio que arriba en Update, ver su comentario).
            if (UiStyles.EscribiendoTexto || JournalHud.Abierto) return;

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null || _sim == null || _sim.Grid == null) return;

            Vector2 screenPos = mouse.position.ReadValue();

            // No pintar/interactuar con la grilla si el ratón está sobre la ventana IMGUI.
            if (IsOverWindow(screenPos)) return;

            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter)) return;

            Vector3 world = ray.GetPoint(enter);
            Vector2Int cell = _sim.WorldToCell(world);
            if (!CellGrid.InBounds(cell.x, cell.y)) return;

            _hoverCell = cell;
            _hoverValid = true;

            int radius = Mathf.Clamp(Mathf.RoundToInt(_brushRadius), 1, 10);
            if (mouse.leftButton.isPressed)
            {
                _sim.Paint(cell.x, cell.y, radius, _selectedMaterial);
            }
            else if (mouse.rightButton.isPressed)
            {
                _sim.Paint(cell.x, cell.y, radius, MaterialId.Empty);
            }
        }

        private bool IsOverWindow(Vector2 screenPos)
        {
            // Mouse.current.position usa origen abajo-izquierda; IMGUI usa origen arriba-izquierda.
            Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
            return _windowRect.Contains(guiPos);
        }

        private void OnGUI()
        {
            if (!IsDevBuild() || !_visible || _sim == null || _sim.Universe == null) return;
            _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindow, "ChaosAlchemy — Dev (F3)");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label($"FPS: {1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):F0}   Sim: {_sim.Stepper.LastStepMs:F2} ms");
            GUILayout.Label($"Chunks activos: {_sim.Stepper.ActiveChunks}/{CellGrid.ChunksX * CellGrid.ChunksY}   Celdas activas: {_sim.Stepper.ActiveCells}");
            GUILayout.Label($"Seed: {_sim.Universe.Seed}   Tick: {_sim.Stepper.Tick}");

            GUILayout.Space(6);
            GUILayout.Label("Materiales:");
            var mats = _sim.Universe.Materials;
            const int perRow = 3;
            for (int i = 0; i < mats.Length; i += perRow)
            {
                GUILayout.BeginHorizontal();
                int rowEnd = Mathf.Min(i + perRow, mats.Length);
                for (int j = i; j < rowEnd; j++)
                {
                    var def = mats[j];
                    bool selected = def.id == _selectedMaterial;
                    var prevColor = GUI.backgroundColor;
                    GUI.backgroundColor = selected ? Color.yellow : Color.white;
                    if (GUILayout.Button(def.devName, GUILayout.Height(24)))
                    {
                        _selectedMaterial = def.id;
                    }
                    GUI.backgroundColor = prevColor;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6);
            GUILayout.Label($"Radio de pincel: {Mathf.RoundToInt(_brushRadius)}");
            _brushRadius = GUILayout.HorizontalSlider(_brushRadius, 1f, 10f);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Time.timeScale > 0f ? "Pause (P)" : "Play (P)")) TogglePause();
            if (GUILayout.Button("Step (N)")) _sim.StepOnce();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Velocidad:", GUILayout.Width(70));
            if (GUILayout.Button("0.5x")) Time.timeScale = 0.5f;
            if (GUILayout.Button("1x")) Time.timeScale = 1f;
            if (GUILayout.Button("2x")) Time.timeScale = 2f;
            if (GUILayout.Button("4x")) Time.timeScale = 4f;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            if (_hoverValid)
            {
                var grid = _sim.Grid;
                int idx = CellGrid.Idx(_hoverCell.x, _hoverCell.y);
                byte matId = grid.GetMat(idx);
                var def = _sim.Universe.Get(matId);
                int c = CellGrid.RawToC(grid.temp[idx]);
                GUILayout.Label($"Celda ({_hoverCell.x},{_hoverCell.y}): {def.devName} [id {matId}]  {c}°C  aux={grid.aux[idx]}");
            }
            else
            {
                GUILayout.Label("Celda: -");
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}
