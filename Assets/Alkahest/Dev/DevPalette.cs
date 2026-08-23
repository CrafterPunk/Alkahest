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
        // (integración pt40) cache del director de Semilla Cero para la
        // línea de autonomía del panel -- ver su uso en DrawWindow.
        private SemillaCero _semillaCero;
        private float _semillaCeroBusqueda;
        private bool _visible;
        /// <summary>Abierta = el ratón pinta materiales; el Frasco debe ignorar sus clics.</summary>
        public static bool IsOpen { get; private set; }
        private const string PrefKey = "TenThousandYears_DevPalette";

        // Arranca por debajo del panel del frasco (arriba-izquierda) para no
        // taparlo al abrirse; sigue siendo arrastrable.
        private Rect _windowRect = new Rect(12, 180, 300, 480);

        private byte _selectedMaterial = MaterialId.Sand;
        private float _brushRadius = 3f;
        private float _lastTimeScale = 1f;

        private Vector2Int _hoverCell;
        private bool _hoverValid;

        // =====================================================================
        // (fix playtest 13) "El tirar humo es como si fuera un borrador."
        // =====================================================================
        // El jugador tenía razón en su propia lectura: Vapor/Humo/Fuego/Ceniza
        // no son un INSUMO que el jugador manipula, son la CONSECUENCIA de una
        // reacción (hervir, arder, disolver -- ver SimStepper.ApplyPhase/
        // ProcessFire) y se disipan solos (gasLifetime, o condensan/mueren por
        // temperatura). Pintar Humo desplaza lo que hubiera y desaparece solo
        // un rato después: de ahí la sensación de goma de borrar. Esto NO es
        // un bug de la simulación -- es correcto que un subproducto no se
        // comporte como un bloque de construcción -- así que el arreglo no es
        // tocar Sim/, es que la paleta DISTINGA visualmente las dos clases y
        // avise qué va a pasar cuando el jugador selecciona un subproducto.
        // Ningún material se quita de la paleta: pintar Humo sigue siendo
        // útil para depurar reacciones.
        //
        // Lista fija a propósito (no derivada de MaterialArchetype: Fuego es
        // Fire, Humo/Vapor son Gas, Ceniza es Powder -- el arquetipo no
        // distingue "consecuencia de una reacción" de "insumo", es una
        // propiedad de DISEÑO, no de simulación).
        // =====================================================================
        private static readonly byte[] SubproductoIds =
        {
            MaterialId.Steam, MaterialId.Smoke, MaterialId.Fire, MaterialId.Ash,
        };

        /// <summary>Tinte cálido y sobrio para distinguir el botón de un subproducto sin gritar (regla del encargo: "la presentación más sobria que puedas").</summary>
        private static readonly Color SubproductoTint = new Color(0.95f, 0.68f, 0.42f);

        /// <summary>Ids del resto del roster (todo lo que NO es subproducto), calculado una única vez a nivel de tipo -- nunca en OnGUI.</summary>
        private static readonly byte[] InsumoIds = BuildInsumoIds();

        private static byte[] BuildInsumoIds()
        {
            var ids = new System.Collections.Generic.List<byte>(MaterialId.Count - SubproductoIds.Length);
            for (int id = 0; id < MaterialId.Count; id++)
            {
                if (System.Array.IndexOf(SubproductoIds, (byte)id) < 0) ids.Add((byte)id);
            }
            return ids.ToArray();
        }

        private static bool EsSubproducto(byte id) => System.Array.IndexOf(SubproductoIds, id) >= 0;

        /// <summary>Estilo de párrafo con word-wrap para la línea explicativa del subproducto seleccionado. Lazy: GUIStyle no se puede construir fuera de OnGUI/editor context de forma fiable.</summary>
        private GUIStyle _explicacionStyle;

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
                    // (playtest 26, fix integración) N solo actúa con la paleta
                    // ABIERTA: HintSystem estrenó N como "siguiente consejo" y
                    // en dev-builds (regla 14) una sola pulsación hacía las dos
                    // cosas a la vez -- saltaba consejo Y avanzaba un tick de
                    // sim. El paso-a-paso es una herramienta de la paleta:
                    // que exija tenerla delante es lo natural.
                    if (kb.nKey.wasPressedThisFrame && _visible) _sim.StepOnce();
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
                // (fix playtest 13) PaintStable en vez de Paint: nace a la
                // temperatura de estabilidad del material, no a la heredada
                // de la celda (normalmente ambiente) -- ver AlkahestSim para
                // el porqué completo ("al seleccionar hielo, tiro agua").
                _sim.PaintStable(cell.x, cell.y, radius, _selectedMaterial);
            }
            else if (mouse.rightButton.isPressed)
            {
                // Borrar SÍ sigue siendo Paint(Empty): Empty no tiene ninguna
                // transición de fase que corregir, y así no se introduce un
                // segundo camino de comportamiento para el mismo botón.
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
            _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindow, "TenThousandYears — Dev (F3)");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label($"FPS: {1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):F0}   Sim: {_sim.Stepper.LastStepMs:F2} ms");
            GUILayout.Label($"Chunks activos: {_sim.Stepper.ActiveChunks}/{CellGrid.ChunksX * CellGrid.ChunksY}   Celdas activas: {_sim.Stepper.ActiveCells}");
            GUILayout.Label($"Seed: {_sim.Universe.Seed}   Tick: {_sim.Stepper.Tick}");

            // (integración pt40, SEMILLA CERO) LA MÉTRICA REINA a la vista
            // del dev: acciones del jugador DESPUÉS del final abierto
            // (contrato SEMILLA §1 beat 6). El lookup se cachea y solo se
            // reintenta cada ~2s mientras no haya director (jamás
            // FindAnyObjectByType por frame).
            if (_semillaCero == null && AlkahestGameBootstrap.ModoSemillaCero)
            {
                _semillaCeroBusqueda -= Time.unscaledDeltaTime;
                if (_semillaCeroBusqueda <= 0f)
                {
                    _semillaCero = FindAnyObjectByType<SemillaCero>();
                    _semillaCeroBusqueda = 2f;
                }
            }
            if (_semillaCero != null)
                GUILayout.Label($"Autonomía (post-final abierto): {_semillaCero.AccionesPostFinalAbierto} acciones");

            GUILayout.Space(6);
            var mats = _sim.Universe.Materials;

            // (fix playtest 13) Dos grupos en vez de una sola grilla: insumo
            // (lo que el jugador manipula como bloque de construcción) vs
            // subproducto (lo que nace de una reacción y se disipa solo). Ver
            // el bloque de comentario grande junto a SubproductoIds arriba.
            GUILayout.Label("Insumos (los manipulas tú):");
            DrawMaterialGrid(mats, InsumoIds, tinted: false);

            GUILayout.Space(4);
            GUILayout.Label("Subproductos (nacen de una reacción y se disipan solos):");
            DrawMaterialGrid(mats, SubproductoIds, tinted: true);

            if (EsSubproducto(_selectedMaterial))
            {
                // Herramienta de dev: aquí SÍ conviene ser explícito (regla
                // del encargo), a diferencia del resto del juego que nunca
                // revela mecánica interna al jugador final.
                _explicacionStyle ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
                GUILayout.Label(SubproductoExplicacion(_selectedMaterial), _explicacionStyle);
            }

            GUILayout.Space(6);
            GUILayout.Label($"Radio de pincel: {Mathf.RoundToInt(_brushRadius)}");
            _brushRadius = GUILayout.HorizontalSlider(_brushRadius, 1f, 10f);

            // (playtest 44) La capa decorativa de partículas quedó APAGADA por
            // pedido de Cesar (ver Game/ParticulasFx.cs, flag Activas) -- este
            // toggle dev existe para poder compararla con ojos sin recompilar.
            ParticulasFx.Activas = GUILayout.Toggle(ParticulasFx.Activas, " partículas decorativas (apagadas por defecto, pt44)");

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
                // (playtest 43, diagnóstico del "bedrock que no se puede
                // quitar") El sufijo "· OBRA" delata que la celda está
                // dentro de un rect de SimLevelBuilder.ObraDelTaller (lo que
                // el cincel respeta): si un "resto imborrable" NO lo lleva,
                // la causa es otra y este dato lo dice al instante.
                string obra = SimLevelBuilder.EsObraDelTaller(_hoverCell.x, _hoverCell.y) ? "  · OBRA" : "";
                GUILayout.Label($"Celda ({_hoverCell.x},{_hoverCell.y}): {def.devName} [id {matId}]  {c}°C  aux={grid.aux[idx]}{obra}");
            }
            else
            {
                GUILayout.Label("Celda: -");
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        /// <summary>Dibuja una grilla de botones de 3 en 3 para el subconjunto `ids` del roster. `tinted` marca el grupo subproducto con <see cref="SubproductoTint"/> (seleccionado sigue siendo amarillo en ambos grupos, coherente con el resto del panel).</summary>
        private void DrawMaterialGrid(MaterialDef[] mats, byte[] ids, bool tinted)
        {
            const int perRow = 3;
            for (int i = 0; i < ids.Length; i += perRow)
            {
                GUILayout.BeginHorizontal();
                int rowEnd = Mathf.Min(i + perRow, ids.Length);
                for (int j = i; j < rowEnd; j++)
                {
                    var def = mats[ids[j]];
                    bool selected = def.id == _selectedMaterial;
                    var prevColor = GUI.backgroundColor;
                    GUI.backgroundColor = selected ? Color.yellow : (tinted ? SubproductoTint : Color.white);
                    if (GUILayout.Button(def.devName, GUILayout.Height(24)))
                    {
                        _selectedMaterial = def.id;
                    }
                    GUI.backgroundColor = prevColor;
                }
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Línea explicativa mostrada bajo la paleta cuando el material
        /// seleccionado es un subproducto (fix playtest 13, respuesta directa
        /// a "¿es una consecuencia y no un material a manipular?" -- sí).
        /// Strings literales fijas: no se concatena nada aquí, así que no hay
        /// coste de construir texto por frame pese a llamarse desde OnGUI.
        /// </summary>
        private static string SubproductoExplicacion(byte id)
        {
            switch (id)
            {
                case MaterialId.Steam:
                    return "Se disipa solo: por debajo de su temperatura de condensación vuelve a ser Agua (MaterialDef.condensesAt). Nace al hervir Agua o al apagarse Fuego con agua cerca.";
                case MaterialId.Smoke:
                    return "Se disipa solo (vida corta, gasLifetime). Nace cuando Fuego se apaga sin brasa debajo, o cuando Ácido disuelve Arena/Ceniza/Hielo/Cristal.";
                case MaterialId.Fire:
                    return "Se apaga solo (vida corta, gasLifetime). Nace al cruzar la temperatura de ignición de un material inflamable, o por contacto con otro Fuego.";
                case MaterialId.Ash:
                    return "Residuo sólido que se posa y NO se disipa. Nace cuando Fuego se apaga sobre una superficie sólida debajo.";
                default:
                    return string.Empty;
            }
        }
    }
}
