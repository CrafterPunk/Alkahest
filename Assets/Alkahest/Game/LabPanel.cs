using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (R130) EL PANEL DEL LABORATORIO DE LEYES — docs/LAB/DISENO_LABORATORIO.md §9.
    /// Esqueleto de Fable (tiempo, parámetros por pestaña, libro mayor,
    /// teletransportes) completado por Opus 5 en el hito H2 (R131): presets
    /// JSON con nombre/nota/comparación, snapshot (preset + PNG + libro),
    /// pincel de materia y vistas de depuración.
    ///
    /// Teclas (guardas de la regla 12):
    ///   F8         abre/cierra el panel.
    ///   Ctrl+1..6  teletransporta a las zonas del plano.
    ///   -, +       radio del pincel (solo con el pincel activo).
    /// Ninguna pisa las del juego (F3/F6/F7/G/C/V/E/Q/R): la lista está en el
    /// pie de la pantalla. Solo existe en ModoLaboratorio (lo crea
    /// SpawnLaboratorio). Con el panel abierto, frasco/cincel/termómetro ceden
    /// los clics cuando el ratón está sobre la ventana O el pincel está armado
    /// (<see cref="BloqueaHerramientas"/>): se puede afinar un número y seguir
    /// vertiendo, pero no tallar y pintar a la vez.
    /// </summary>
    public sealed class LabPanel : MonoBehaviour
    {
        private const int WindowId = 918274; // constante, jamás GetInstanceID (guía del proyecto). 918273 es el curador.

        public static bool Abierto => _instancia != null && _instancia._abierto;
        public static bool RatonSobrePanel { get; private set; }
        /// <summary>(R131) El pincel está armado: el clic es del pincel, no del cincel/frasco.</summary>
        /// <summary>(R143, H7) Qué pestaña mira el jugador, para que el diario de sesión pueda anotarlo. Vacío si el panel está cerrado.</summary>
        public static string PestanaAbierta { get; private set; } = "";
        /// <summary>(R143, H7) Índice del pincel armado, o −1. Lo lee el diario: lo que alguien PINTA dice qué le falta como herramienta.</summary>
        public static int PincelSeleccionado { get; private set; } = -1;
        /// <summary>(R143, H7) Nombre legible de un pincel del catálogo.</summary>
        public static string NombrePincel(int i) => i >= 0 && i < Catalogo.Length ? Catalogo[i].Nombre : "?";

        public static bool PincelActivo { get; private set; }
        /// <summary>Guarda para Flask/Cincel/Termometro: el panel captura el ratón mientras está encima de él o mientras el pincel está armado.</summary>
        public static bool BloqueaHerramientas => Abierto && (RatonSobrePanel || PincelActivo);
        private static LabPanel _instancia;

        private AlkahestSim _sim;
        private ApprenticeController _aprendiz;
        private bool _abierto;
        private Rect _ventana = new Rect(12f, 60f, 400f, 640f);
        private Vector2 _scroll;
        private int _pestana;
        private string[] _pestanas;
        private GUIStyle _estiloTitulo, _estiloPie, _estiloBoton, _estiloBotonSel, _estiloAyuda, _estiloCampo;
        private GUIStyle _estiloAgarre, _estiloLectura;
        /// <summary>(R140) Reutilizado por el lector de celda: se dibuja cada frame y el proyecto no admite allocs por frame.</summary>
        private readonly System.Text.StringBuilder _sbLectura = new System.Text.StringBuilder(160);
        /// <summary>(R142, R19-6) Lo que el lector mostró la última vez. Si nada de esto cambió, no se reconstruye el texto.</summary>
        private int _lecturaIdx = -1;
        private byte _lecturaMat, _lecturaTemp, _lecturaHum, _lecturaCarga, _lecturaLuz, _lecturaReposo;
        private int _lecturaUmbHum = -1, _lecturaUmbTurb = -1;
        private readonly GUIContent _lecturaTexto = new GUIContent("");
        private Vector2 _lecturaTam;
        private readonly HashSet<string> _ayudaAbierta = new HashSet<string>();
        private float _conteoHasta;
        private int _nAgua, _nSedimento, _nArcilla, _nPlanta, _nVapor; private long _vaporAire;

        // ---- H2: presets ---------------------------------------------------------
        private string _nombrePreset = "prueba";
        private string _notaPreset = "";
        private string _presetElegido = "";
        private List<string> _presets;
        private bool _ayudaGeneral;
        private bool _escribiendo;

        // ---- H2: pincel ----------------------------------------------------------
        private int _pincelSel = -1;
        private int _radio = 2;

        /// <summary>Una entrada del pincel. `Turbia` pinta agua con los finos del manantial; `Caliente` nace a 220 raw (regla 22: PaintCell fija temperatura, PaintStable la deja estable).</summary>
        private struct Pintable
        {
            public string Grupo, Nombre;
            public byte Mat;
            public bool Turbia, Caliente;
        }

        // El catálogo del pincel: los materiales del laboratorio, las cuatro
        // leyes del lugar (hogar, frío, manantial, sumidero: colocar una FUENTE
        // es el gesto más potente del sandbox), la materia del mundo y los
        // fluidos. El agua TURBIA está aparte a propósito: la diferencia entre
        // agua limpia y agua cargada es media tesis del laboratorio (una
        // colmata un filtro y la otra no).
        private static readonly Pintable[] Catalogo =
        {
            new Pintable { Grupo = "SUELO",  Nombre = "sedimento",   Mat = MaterialId.Sedimento },
            new Pintable { Grupo = "SUELO",  Nombre = "arcilla",     Mat = MaterialId.Arcilla },
            new Pintable { Grupo = "SUELO",  Nombre = "terracota",   Mat = MaterialId.Terracota },
            new Pintable { Grupo = "SUELO",  Nombre = "grava",       Mat = MaterialId.Grava },
            new Pintable { Grupo = "SUELO",  Nombre = "arenisca",    Mat = MaterialId.Arenisca },
            new Pintable { Grupo = "SUELO",  Nombre = "arena",       Mat = MaterialId.Sand },
            new Pintable { Grupo = "SUELO",  Nombre = "roca",        Mat = MaterialId.Stone },
            new Pintable { Grupo = "SUELO",  Nombre = "roca suelta", Mat = MaterialId.RocaSuelta },
            new Pintable { Grupo = "SUELO",  Nombre = "vidrio",      Mat = MaterialId.VidrioVerde }, // (R140) el producto del horno, impintable hasta ahora.
            new Pintable { Grupo = "VIDA",   Nombre = "planta",      Mat = MaterialId.Planta },
            new Pintable { Grupo = "VIDA",   Nombre = "fibra",       Mat = MaterialId.Fibra },
            new Pintable { Grupo = "VIDA",   Nombre = "semilla",     Mat = MaterialId.Semilla },
            new Pintable { Grupo = "VIDA",   Nombre = "ceniza",      Mat = MaterialId.Ash },
            new Pintable { Grupo = "VIDA",   Nombre = "carbón",      Mat = MaterialId.Carbon },      // (R140) faltaba desde que nació en R135. Es POLVO: cae y se apila (ayuda del panel).
            new Pintable { Grupo = "LEYES",  Nombre = "hogar",       Mat = MaterialId.Hogar },
            new Pintable { Grupo = "LEYES",  Nombre = "núcleo frío", Mat = MaterialId.NucleoFrio },
            new Pintable { Grupo = "LEYES",  Nombre = "manantial",   Mat = MaterialId.Manantial },
            new Pintable { Grupo = "LEYES",  Nombre = "sumidero",    Mat = MaterialId.Sumidero },
            new Pintable { Grupo = "FLUIDOS",Nombre = "agua limpia", Mat = MaterialId.Water },
            new Pintable { Grupo = "FLUIDOS",Nombre = "agua turbia", Mat = MaterialId.Water, Turbia = true },
            new Pintable { Grupo = "FLUIDOS",Nombre = "aceite",      Mat = MaterialId.Oil },
            new Pintable { Grupo = "FLUIDOS",Nombre = "hielo",       Mat = MaterialId.Ice },
            new Pintable { Grupo = "FLUIDOS",Nombre = "brasa",       Mat = MaterialId.Brasa, Caliente = true },
            new Pintable { Grupo = "FLUIDOS",Nombre = "fuego",       Mat = MaterialId.Fire, Caliente = true },
        };

        public static LabPanel Crear(AlkahestSim sim, ApprenticeController aprendiz)
        {
            var go = new GameObject("LabPanel");
            var p = go.AddComponent<LabPanel>();
            p._sim = sim;
            p._aprendiz = aprendiz;
            _instancia = p;
            return p;
        }

        private void Start()
        {
            // (H2) Deja en el disco los valores de fábrica la primera vez, para
            // que «comparar contra defaults» funcione incluso desde fuera del
            // juego (un diff de git contra _defaults.json cuenta la historia).
            LabPresets.EscribirDefaultsSiFalta();
            _presets = LabPresets.Listar();
        }

        private void OnDestroy()
        {
            if (_instancia == this) _instancia = null;
            RatonSobrePanel = false;
            PincelActivo = false;
            // VistaLab es ESTÁTICA (vive en SimRenderer y sobrevive a la escena):
            // sin esto, salir del laboratorio con una vista puesta dejaría el
            // overlay encendido en el juego normal. El panel solo existe en
            // ModoLaboratorio, así que su muerte es el sitio exacto para apagarla.
            SimRenderer.VistaLab = VistaLaboratorio.Ninguna;
            if (_escribiendo) { _escribiendo = false; UiStyles.EscribiendoTexto = false; }
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _sim == null) return;

            // El ratón se evalúa SIEMPRE: mientras se escribe el nombre de un
            // preset el teclado está tomado, pero el panel sigue capturando los
            // clics que caen encima de él (si no, se talla a través del panel).
            var mouse = Mouse.current;
            if (_abierto && mouse != null)
            {
                Vector2 p = mouse.position.ReadValue();
                RatonSobrePanel = _ventana.Contains(new Vector2(p.x, Screen.height - p.y));
            }
            else RatonSobrePanel = false;

            PincelActivo = _abierto && _pincelSel >= 0;
            // (R143, H7) Espejo público para el diario de sesión: es la única forma de que el
            // registro sepa qué mira y qué pinta el jugador sin que el diario hurgue aquí dentro.
            PincelSeleccionado = _abierto ? _pincelSel : -1;
            PestanaAbierta = _abierto && _pestanas != null && _pestana >= 0 && _pestana < _pestanas.Length ? _pestanas[_pestana] : "";

            bool tecladoLibre = !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto && !DayCycle.InputLocked;
            if (tecladoLibre)
            {
                if (kb.f8Key.wasPressedThisFrame)
                {
                    _abierto = !_abierto;
                    if (!_abierto) { _pincelSel = -1; PincelActivo = false; }
                }

                bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
                if (ctrl && _aprendiz != null)
                {
                    for (int i = 0; i < SimLevelBuilder.LabAnclaX.Length; i++)
                    {
                        Key tecla = (Key)((int)Key.Digit1 + i);
                        if (kb[tecla].wasPressedThisFrame) { TeleportarA(i); break; }
                    }
                }

                if (PincelActivo)
                {
                    if (kb.minusKey.wasPressedThisFrame) _radio = Mathf.Max(0, _radio - 1);
                    if (kb.equalsKey.wasPressedThisFrame) _radio = Mathf.Min(8, _radio + 1);
                }
            }

            if (LabParams.VaporVidaCambiado && _sim.Universe != null) Universe.ReaplicarVapor(_sim.Universe);

            if (PincelActivo && !RatonSobrePanel) PincelTick(mouse);

            if (Time.unscaledTime >= _conteoHasta) { Contar(); _conteoHasta = Time.unscaledTime + 1f; }
        }

        // =================================================================
        // (H2) EL PINCEL DE MATERIA
        // =================================================================
        private void PincelTick(Mouse mouse)
        {
            if (mouse == null || _sim == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            bool izq = mouse.leftButton.isPressed, der = mouse.rightButton.isPressed;
            if (!izq && !der) return;

            Vector2 pantalla = mouse.position.ReadValue();
            Vector3 mundo = cam.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, 10f));
            int cx = Mathf.FloorToInt(mundo.x / SimRenderer.CellWorldSize);
            int cy = Mathf.FloorToInt(mundo.y / SimRenderer.CellWorldSize);
            if (!CellGrid.InBounds(cx, cy)) return;

            if (der) { _sim.Paint(cx, cy, _radio, MaterialId.Empty); return; } // derecho: borrar.

            var c = Catalogo[_pincelSel];
            if (c.Turbia)
            {
                // El agua turbia NO puede nacer de PaintStable (nace limpia y llena):
                // los finos son un campo, y PaintLab es la única puerta que los escribe.
                byte carga = (byte)Mathf.Clamp(LabParams.TurbidezFuente, 0, 255);
                byte temp = (byte)Mathf.Clamp(LabParams.FuenteTempRaw, 0, 255);
                for (int dy = -_radio; dy <= _radio; dy++)
                    for (int dx = -_radio; dx <= _radio; dx++)
                        if (dx * dx + dy * dy <= _radio * _radio)
                            _sim.PaintLab(cx + dx, cy + dy, MaterialId.Water, temp, 255, carga);
            }
            else if (c.Caliente)
            {
                // Regla 22: lo que nace caliente entra por PaintCell (PaintStable
                // lo haría nacer a la temperatura estable de su material, o sea apagado).
                for (int dy = -_radio; dy <= _radio; dy++)
                    for (int dx = -_radio; dx <= _radio; dx++)
                        if (dx * dx + dy * dy <= _radio * _radio)
                            _sim.PaintCell(cx + dx, cy + dy, c.Mat, 220);
            }
            else _sim.PaintStable(cx, cy, _radio, c.Mat);
        }

        private void TeleportarA(int i)
        {
            float celda = SimRenderer.CellWorldSize;
            var destino = new Vector3((SimLevelBuilder.LabAnclaX[i] + 0.5f) * celda, (SimLevelBuilder.LabAnclaY[i] + 0.5f) * celda, 0f);
            _aprendiz.transform.position = destino;
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(destino.x, destino.y, cam.transform.position.z);
        }

        private void Contar()
        {
            var g = _sim.Grid; if (g == null) return;
            int a = 0, s = 0, ar = 0, pl = 0, va = 0; long vap = 0;
            var mat = g.mat; var hum = g.humedad;
            for (int i = 0; i < mat.Length; i++)
            {
                switch (mat[i])
                {
                    case MaterialId.Water: a++; break;
                    case MaterialId.Sedimento: s++; break;
                    case MaterialId.Arcilla: ar++; break;
                    case MaterialId.Planta: pl++; break;
                    case MaterialId.Steam: va++; break;
                    case MaterialId.Empty: vap += hum[i]; break;
                }
            }
            _nAgua = a; _nSedimento = s; _nArcilla = ar; _nPlanta = pl; _nVapor = va; _vaporAire = vap;
        }

        private void OnGUI()
        {
            if (DayCycle.InputLocked || !_abierto)
            {
                if (_escribiendo) { _escribiendo = false; UiStyles.EscribiendoTexto = false; }
                return;
            }
            PrepararEstilos();
            GUI.depth = 5;
            _ventana = GUILayout.Window(WindowId, _ventana, DibujarVentana, "LABORATORIO DE LEYES (F8)", GUI.skin.window);

            // (regla 12) El campo de texto se come TODAS las letras: mientras
            // tenga el foco, los atajos de una tecla del juego callan.
            string foco = GUI.GetNameOfFocusedControl();
            bool escribiendo = foco == "labNombre" || foco == "labNota";
            if (escribiendo != _escribiendo) { _escribiendo = escribiendo; UiStyles.EscribiendoTexto = escribiendo; }

            string pie = PincelActivo
                ? "PINCEL ARMADO (" + Catalogo[_pincelSel].Nombre + ", radio " + _radio + ") · izq pinta · der borra · -/+ radio · F8: panel"
                : "F8: panel · Ctrl+1..6: zonas · G: termómetro · C: cincel · F6: movimiento · F7: piel de roca";
            float w = UiStyles.Ancho(_estiloPie, pie) + 16f;
            GUI.Label(new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyles.S(26f), w, UiStyles.S(22f)), pie, _estiloPie);

            // (R140) EL LECTOR DE CELDA. Un rótulo junto al cursor que dice QUÉ hay debajo, con
            // su nombre y su estado en palabras. Sin esto el laboratorio se miraba a ciegas: la
            // grilla tiene cuatro campos por celda y ninguno se ve, así que un sedimento
            // empapado y uno seco eran el mismo píxel pardo, y el carbón que sale de una
            // carbonera no se distinguía de la ceniza que lo rodea salvo cambiando de vista.
            // Nombrar lo que se ve es la mitad de poder investigarlo.
            DibujarLectura();

            // (H2) El anillo del radio en el cursor, como el del curador: sin él
            // no se sabe cuánto se va a pintar hasta después de pintarlo.
            if (PincelActivo && !RatonSobrePanel && Event.current.type == EventType.Repaint)
            {
                var camGui = Camera.main;
                var m = Mouse.current;
                if (camGui != null && m != null && camGui.orthographic)
                {
                    float pxPorCelda = Screen.height / (camGui.orthographicSize * 2f) * SimRenderer.CellWorldSize;
                    float lado = (_radio * 2 + 1) * pxPorCelda;
                    Vector2 p = m.position.ReadValue();
                    var r = new Rect(p.x - lado * 0.5f, Screen.height - p.y - lado * 0.5f, lado, lado);
                    var antes = GUI.color;
                    GUI.color = new Color(1f, 0.85f, 0.5f, 0.35f);
                    GUI.Box(r, GUIContent.none);
                    GUI.color = antes;
                }
            }
        }

        private void DibujarVentana(int id)
        {
            if (_pestanas == null)
            {
                var lista = new List<string> { "TIEMPO", "LIBRO", "PRESETS", "PINCEL", "VISTAS" };
                foreach (var p in LabParams.Registro) if (!lista.Contains(p.Grupo)) lista.Add(p.Grupo);
                _pestanas = lista.ToArray();
            }
            // Pestañas en dos filas.
            for (int fila = 0; fila < 2; fila++)
            {
                GUILayout.BeginHorizontal();
                for (int i = fila * ((_pestanas.Length + 1) / 2); i < Mathf.Min(_pestanas.Length, (fila + 1) * ((_pestanas.Length + 1) / 2)); i++)
                {
                    if (GUILayout.Button(_pestanas[i], i == _pestana ? _estiloBotonSel : _estiloBoton)) _pestana = i;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(4f);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(UiStyles.S(520f)));
            string grupo = _pestanas[_pestana];
            if (grupo == "TIEMPO") DibujarTiempo();
            else if (grupo == "LIBRO") DibujarLibro();
            else if (grupo == "PRESETS") DibujarPresets();
            else if (grupo == "PINCEL") DibujarPincel();
            else if (grupo == "VISTAS") DibujarVistas();
            else DibujarParametros(grupo);
            GUILayout.EndScrollView();

            // (R140) LA BARRA DE AGARRE. El `GUI.DragWindow` de antes cubría los 20 primeros
            // píxeles SIN ESCALAR de la ventana, y ahí es justo donde caen los botones de la
            // primera fila de pestañas: se comían el clic y el panel no se podía mover. Ahora el
            // arrastre cubre el título (escalado, no 20 píxeles fijos) y además esta franja
            // explícita al pie, que se ve y dice para qué sirve. Mover el panel importa porque
            // tapa un tercio del mundo: el laboratorio se mira por zonas y la que quieras ver
            // puede estar justo debajo.
            var rAgarre = GUILayoutUtility.GetRect(GUIContent.none, _estiloAgarre,
                GUILayout.ExpandWidth(true), GUILayout.Height(UiStyles.S(18f)));
            GUI.Box(rAgarre, "≡   arrastra aquí (o la barra del título) para mover el panel", _estiloAgarre);
            GUI.DragWindow(new Rect(0f, 0f, 10000f, UiStyles.S(26f)));
            GUI.DragWindow(rAgarre);
        }

        private void DibujarTiempo()
        {
            var st = _sim.Stepper;
            GUILayout.Label("VELOCIDAD DEL MUNDO", _estiloTitulo);
            GUILayout.BeginHorizontal();
            int[] vel = { 1, 5, 10, 50, 100 };
            foreach (int v in vel)
                if (GUILayout.Button(v + "x", _sim.LabMultiplicador == v ? _estiloBotonSel : _estiloBoton)) _sim.LabMultiplicador = v;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_sim.Paused ? "REANUDAR" : "PAUSA", _estiloBoton)) _sim.Paused = !_sim.Paused;
            if (GUILayout.Button("UN TICK", _estiloBoton)) _sim.StepOnce();
            GUILayout.EndHorizontal();
            GUILayout.Label($"pedido {_sim.LabMultiplicador}x · real {_sim.LabMultiplicadorReal:F1}x · presupuesto {LabParams.PresupuestoMs} ms/frame", _estiloPie);
            _sim.LabPresupuestoMs = LabParams.PresupuestoMs;
            GUILayout.Space(6f);
            if (st != null)
            {
                GUILayout.Label("COSTE DEL ÚLTIMO TICK (ms)", _estiloTitulo);
                GUILayout.Label($"total {st.LastStepMs:F2} · difusión {st.MsDifusion:F2} · barrido {st.MsBarrido:F2} · chunks {st.MsChunks:F2} · morph {st.MsMorph:F2}", _estiloPie);
                GUILayout.Label($"campos {st.MsCampos:F2} · presión {st.MsPresion:F2} · luz {st.MsLuz:F2} · cuerpos {st.MsCuerpos:F2}", _estiloPie);
                GUILayout.Label($"tick {st.Tick} · chunks despiertos {st.ActiveChunks}/{CellGrid.ChunksX * CellGrid.ChunksY} · celdas activas {st.ActiveCells} · FPS {1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):F0}", _estiloPie);
            }
            GUILayout.Space(6f);
            DibujarParametros("TIEMPO");
        }

        private void DibujarLibro()
        {
            var st = _sim.Stepper; if (st == null) return;
            GUILayout.Label("CENSO (cada segundo)", _estiloTitulo);
            GUILayout.Label($"agua {_nAgua} · vapor visible {_nVapor} · vapor en el aire {(_vaporAire / 255f):F1} celdas eq. · sedimento {_nSedimento} · arcilla {_nArcilla} · plantas {_nPlanta}", _estiloPie);
            GUILayout.Label("LIBRO MAYOR (celdas o unidades/255)", _estiloTitulo);
            GUILayout.Label($"manantial emitió {st.LabAguaEmitida} · sumidero tragó {st.LabAguaSumida} celdas ({st.LabAguaSumidaU / 255f:F1} llenas)", _estiloPie);
            GUILayout.Label($"evaporado {st.LabEvaporado / 255f:F1} · condensado {st.LabCondensado / 255f:F1} · goteos {st.LabGoteos}", _estiloPie);
            GUILayout.Label($"infiltrado {st.LabInfiltrado / 255f:F1} · exudado {st.LabExudado} · depositado {st.LabDepositado} · erosionado {st.LabErosionado}", _estiloPie);
            GUILayout.Label($"compactado {st.LabCompactado} · ablandado {st.LabAblandado} · cocido {st.LabCocido} · abonado {st.LabAbonado}", _estiloPie);
            GUILayout.Label($"plantas nacidas {st.LabPlantasNacidas} · muertas {st.LabPlantasMuertas} · presión movió {st.LabPresionMovidas} · cuerpos caídos {st.LabCuerposCaidos} · fracturas {st.LabFracturas}", _estiloPie);
            GUILayout.Space(6f);
            GUILayout.Label("LIBRO DEL COMBUSTIBLE (nominal: es lo que se conserva)", _estiloTitulo);
            long uFibra = st.LabCombustibleQuemado - st.LabCombustibleCarbon;
            long calFibra = st.LabCalorFuego - st.LabCalorCarbon;
            GUILayout.Label($"quemado {st.LabCombustibleQuemado} u ({uFibra} de combustible, {st.LabCombustibleCarbon} de carbón) · de ellas RESPIRANDO {st.LabUnidadesRespiradas}" +
                            (st.LabCombustibleQuemado > 0 ? $" ({100 * st.LabUnidadesRespiradas / st.LabCombustibleQuemado} %)" : ""), _estiloPie);
            GUILayout.Label($"calor nominal {st.LabCalorFuego} (de ellos {st.LabCalorCarbon} los puso el carbón al re-arder) · NO SOLTADO en sordina {st.LabCalorNoSoltado}, de eso volvió como carbón {st.LabEnergiaCarbon} y se perdió {st.LabCalorNoSoltado - st.LabEnergiaCarbon}", _estiloPie);
            GUILayout.Label($"identidad de la carbonera: combustible {calFibra} + carbón {st.LabEnergiaCarbon} = {calFibra + st.LabEnergiaCarbon} raw · " +
                            (uFibra > 0 ? $"{(calFibra / (float)uFibra):F1} raw/u solo del combustible original" : "—"), _estiloPie);
            GUILayout.Label("(R136 C2/C3, R138) Este libro cuenta calor NOMINAL por unidad de reserva, que es la magnitud que se conserva: de aquí sale la identidad de la carbonera, y por eso el carbón se descuenta — su energía se cuenta al nacer y otra vez al arder. El raw/u de la última línea es solo del combustible original, sin mezclar los 22 del carbón con los 14 de la fibra: al aire da 14 y en sordina 7, así que 10 quiere decir «la mitad ardió ahogada». Lo NO SOLTADO es real: una combustión incompleta pierde energía en gases sin quemar, y solo una parte vuelve como carbón. Una carbonera cambia cantidad por calidad y pierde por el camino.", _estiloAyuda);
            GUILayout.Space(6f);
            GUILayout.Label("LIBRO DEL CALOR ENTREGADO (raw escritos de verdad en la grilla)", _estiloTitulo);
            long entregado = st.LabRawFuego + st.LabRawLlama + st.LabRawBrasa + st.LabRawHogar + st.LabRawFrio;
            GUILayout.Label($"combustión {st.LabRawFuego} · LLAMA {st.LabRawLlama} · brasa {st.LabRawBrasa} · hogar {st.LabRawHogar} · frío {st.LabRawFrio} · TOTAL {entregado}", _estiloPie);
            GUILayout.Label($"(el TOTAL son esas CINCO fuentes de fuego y frío, pines propios incluidos; no entra la difusión, ni el ambiente, ni el calor latente del vapor)", _estiloPie);
            GUILayout.Label($"apagado por agua: {st.LabReservaApagada} u de reserva que se fueron sin arder", _estiloPie);
            GUILayout.Label($"(índice de llama: {st.LabCalorLlama} nominales, 40 por celda y tick)", _estiloPie);
            GUILayout.Label("(R138, B) Este es el único libro que admite un TOTAL, porque todos sus sumandos son la misma cosa: raw que acabaron escritos en temp[] después del recorte a 0-255. El de arriba NO se puede sumar — mezcla calor nominal por unidad de reserva con un índice de llama — y sumarlo daba un total de nada. Y la diferencia entre los dos libros no es un detalle: en una hoguera al aire la llama INTENTA unos 2 488 160 raw (40 a cada uno de sus cuatro vecinos más el pin a 255, cada tick) y ENTREGA 105 579 — un 4 % —, porque lo que ya está a 255 no admite más. Cuanto más caliente el sitio, menos entrega la llama, y por eso la fuente que más escribe es siempre la combustión (medido: 51 % al aire, 67 % en carbonera sellada, 48 % en el horno) con la llama entre el 6 y el 29 %. El frío resta, que es lo que hace un núcleo frío.", _estiloAyuda);
            GUILayout.Space(6f);
            GUILayout.Label("BALANCE DE AGUA", _estiloTitulo);
            GUILayout.Label($"el laboratorio ha creado/destruido {st.LabBalanceU / 255f:F1} celdas netas de agua (LabBalanceU = {st.LabBalanceU} u).", _estiloPie);
            GUILayout.Label("(R131) La suma de humedad[] de todo el mundo menos la que había al construirlo tiene que dar EXACTAMENTE ese número: es la auditoría de conservación. Toda regla que no sea una transferencia emparejada pasa por LabNacerAgua o LabTransformar, y las dos se cuentan aquí.", _estiloAyuda);
        }

        // =================================================================
        // (H2) PRESETS Y SNAPSHOTS
        // =================================================================
        private void DibujarPresets()
        {
            GUILayout.Label("GUARDAR", _estiloTitulo);
            GUILayout.BeginHorizontal();
            GUILayout.Label("nombre", _estiloPie, GUILayout.Width(UiStyles.S(52f)));
            GUI.SetNextControlName("labNombre");
            _nombrePreset = GUILayout.TextField(_nombrePreset ?? "", 48, _estiloCampo);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("nota", _estiloPie, GUILayout.Width(UiStyles.S(52f)));
            GUI.SetNextControlName("labNota");
            _notaPreset = GUILayout.TextField(_notaPreset ?? "", 160, _estiloCampo);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("GUARDAR PRESET", _estiloBoton))
            {
                LabPresets.Guardar(_nombrePreset, _notaPreset);
                _presets = LabPresets.Listar();
                _presetElegido = LabPresets.Sanear(_nombrePreset);
            }
            if (GUILayout.Button("SNAPSHOT (+png +libro)", _estiloBoton))
            {
                LabPresets.GuardarSnapshot(_nombrePreset, _notaPreset, _sim, _aprendiz != null ? _aprendiz.transform : null);
                _presets = LabPresets.Listar();
                _presetElegido = LabPresets.Sanear(_nombrePreset);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Un snapshot deja el preset, la foto de lo que se ve y el libro mayor del instante (censo, contadores, inventario de agua, tick y dónde estaba el muñeco). Es lo que hace que una medida se pueda repetir.", _estiloAyuda);

            GUILayout.Space(6f);
            GUILayout.Label("CARGAR / COMPARAR", _estiloTitulo);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("REFRESCAR LISTA", _estiloBoton)) _presets = LabPresets.Listar();
            if (GUILayout.Button("TODO A DEFAULTS", _estiloBoton)) { LabParams.RestaurarDefaults(); LabPresets.UltimoMensaje = "registro restaurado a los valores de fábrica"; }
            GUILayout.EndHorizontal();

            if (_presets == null) _presets = LabPresets.Listar();
            if (_presets.Count == 0) GUILayout.Label("(no hay presets todavía en Laboratorio/presets/)", _estiloAyuda);
            foreach (string n in _presets)
            {
                GUILayout.BeginHorizontal();
                bool sel = n == _presetElegido;
                if (GUILayout.Button((sel ? "▸ " : "  ") + n, sel ? _estiloBotonSel : _estiloBoton)) _presetElegido = sel ? "" : n;
                if (GUILayout.Button("CARGAR", _estiloBoton, GUILayout.Width(UiStyles.S(70f))))
                {
                    LabPresets.Cargar(n, out int desc, out int aus);
                    _presetElegido = n;
                }
                GUILayout.EndHorizontal();
            }

            if (!string.IsNullOrEmpty(LabPresets.UltimoMensaje))
            {
                GUILayout.Space(4f);
                GUILayout.Label("→ " + LabPresets.UltimoMensaje, _estiloAyuda);
            }

            GUILayout.Space(6f);
            var difs = LabPresets.Comparar(_presetElegido);
            GUILayout.Label(string.IsNullOrEmpty(_presetElegido)
                ? "QUÉ HE TOCADO (contra los valores de fábrica)"
                : "QUÉ HE TOCADO (contra fábrica y contra «" + _presetElegido + "»)", _estiloTitulo);
            if (difs.Count == 0) GUILayout.Label("nada: el registro está exactamente como su referencia.", _estiloAyuda);
            foreach (var d in difs)
            {
                string linea = d.Nombre + ": " + Num(d.Actual) + " " + d.Unidad + "  (fábrica " + Num(d.Def);
                if (d.HayOtro) linea += " · preset " + Num(d.Otro);
                linea += ")";
                GUILayout.Label(linea, _estiloPie);
            }

            GUILayout.Space(8f);
            if (GUILayout.Button(_ayudaGeneral ? "AYUDA GENERAL ▾" : "AYUDA GENERAL ▸", _estiloBoton)) _ayudaGeneral = !_ayudaGeneral;
            if (_ayudaGeneral) GUILayout.Label(AyudaGeneral, _estiloAyuda);
        }

        private static string Num(float v) => Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("F2");

        private const string AyudaGeneral =
            "CÓMO SE LEEN LOS NÚMEROS DE ESTE PANEL\n\n" +
            "· UNA VISITA es una pasada de la simulación lenta sobre una celda: ocurre cada 8 ticks, " +
            "o sea 0,27 s de mundo a velocidad 1x. Todo lo que dice «por visita» va a ese ritmo, " +
            "y acelerar el tiempo lo acelera igual.\n\n" +
            "· 255 UNIDADES = UNA CELDA LLENA. La humedad, el volumen del agua, los finos en " +
            "suspensión y la savia de una planta se miden todos en la misma escala: 255 es «lleno». " +
            "Por eso el libro mayor divide entre 255 para hablar en celdas.\n\n" +
            "· RAW es la temperatura interna. °C = raw × 2 − 120. El ambiente de la cueva es 70 raw " +
            "(20 °C), el agua hierve a 110 (100 °C) y el hogar está a 220 (320 °C).\n\n" +
            "· El punto ● delante de un parámetro dice que no está en su valor de fábrica. La «D» de " +
            "su fila lo devuelve; «TODO A DEFAULTS» devuelve los 85 de golpe.\n\n" +
            "· Los presets se guardan en Laboratorio/presets/ como JSON legible y editable a mano. " +
            "Cargar un preset viejo al que le faltan parámetros nuevos NO rompe nada: lo que no " +
            "menciona se queda como está, y el panel lo cuenta.\n\n" +
            "· Las VISTAS pintan un campo por celda encima del mundo. Ojo: los chunks DORMIDOS no " +
            "repintan, así que un campo que cambia sin cambiar la materia puede ir hasta 30 frames " +
            "por detrás. No es un fallo de la vista: es que el mundo, ahí, está dormido.";

        // =================================================================
        // (H2) EL PINCEL
        // =================================================================
        private void DibujarPincel()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(PincelActivo ? "PINCEL ARMADO — desarmar" : "PINCEL DESARMADO", PincelActivo ? _estiloBotonSel : _estiloBoton))
                _pincelSel = PincelActivo ? -1 : 0;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("radio " + _radio + " (" + (_radio * 2 + 1) + " celdas)", _estiloPie, GUILayout.Width(UiStyles.S(150f)));
            if (GUILayout.Button("−", _estiloBoton, GUILayout.Width(UiStyles.S(30f)))) _radio = Mathf.Max(0, _radio - 1);
            if (GUILayout.Button("+", _estiloBoton, GUILayout.Width(UiStyles.S(30f)))) _radio = Mathf.Min(8, _radio + 1);
            GUILayout.EndHorizontal();
            GUILayout.Label("Con el pincel armado el clic es SUYO: el cincel y el frasco ceden. Izquierdo pinta, derecho borra. Las teclas − y + cambian el radio sin abrir el panel.", _estiloAyuda);

            string grupoActual = null;
            int enGrupo = 0; // columnas contadas DENTRO del grupo: si se cuenta sobre el
                             // índice global, cada grupo hereda el desfase del anterior y
                             // las filas salen cojas (un botón suelto, tres apretados).
            for (int i = 0; i < Catalogo.Length; i++)
            {
                if (Catalogo[i].Grupo != grupoActual)
                {
                    if (grupoActual != null) GUILayout.EndHorizontal();
                    grupoActual = Catalogo[i].Grupo;
                    enGrupo = 0;
                    GUILayout.Space(4f);
                    GUILayout.Label(grupoActual, _estiloTitulo);
                    GUILayout.BeginHorizontal();
                }
                else if (enGrupo % 3 == 0) { GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
                if (GUILayout.Button(Catalogo[i].Nombre, i == _pincelSel ? _estiloBotonSel : _estiloBoton)) _pincelSel = i;
                enGrupo++;
            }
            if (grupoActual != null) GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("El agua TURBIA nace con los finos del manantial (sed.turbidezFuente) porque los finos son un campo, no un material: es la única forma de pintar agua cargada. Un canal regado con agua turbia se sella solo por colmatación; con agua limpia, nunca. Esa diferencia es media tesis del laboratorio.", _estiloAyuda);
        }

        // =================================================================
        // (H2) LAS VISTAS
        // =================================================================
        private static readonly string[] NombresVista = { "ninguna", "temperatura", "humedad", "carga", "reposo", "luz", "chunks" };
        private static readonly string[] LeyendasVista =
        {
            "El mundo tal cual: materia y tinte del laboratorio (turbidez, mojado, savia).",
            "Diferencia con el ambiente DE CADA CELDA (el laboratorio tiene clima por zonas): gris = a su temperatura, rojo = más caliente, azul = más frío. Satura a ±80 °C.",
            "Negro → cian. En el aire es VAPOR; en el agua, el volumen de la celda (una celda a medio evaporar se ve más oscura); en un poroso, el agua que contiene; en la roca, el rocío; en una planta, la savia.",
            "Negro → ámbar. En el agua son los FINOS en suspensión (turbidez); en un poroso, los finos atrapados (colmatación: cuanto más ámbar, menos infiltra); en el sedimento, la fertilidad.",
            "Negro → violeta. Visitas que la celda lleva sin moverse. Es la quietud del agua (decide dónde deposita) y la edad del sedimento (decide dónde se compacta en arcilla).",
            "Negro → blanco. La luz que recibe cada celda: la del cielo bajando por la boca y la de los fuegos. Es lo que decide dónde puede germinar una planta.",
            "Verde = chunk DESPIERTO. Es el mapa del gasto: cada tick solo cuesta lo que está verde. Un mundo que se queda quieto se apaga solo.",
        };

        private void DibujarVistas()
        {
            GUILayout.Label("QUÉ PINTAR ENCIMA DEL MUNDO", _estiloTitulo);
            var actual = SimRenderer.VistaLab;
            for (int i = 0; i < NombresVista.Length; i++)
            {
                if (i % 3 == 0) { if (i > 0) GUILayout.EndHorizontal(); GUILayout.BeginHorizontal(); }
                var v = (VistaLaboratorio)i;
                if (GUILayout.Button(NombresVista[i], v == actual ? _estiloBotonSel : _estiloBoton)) SimRenderer.VistaLab = v;
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2f);
            GUILayout.Space(6f);
            GUILayout.Label(LeyendasVista[(int)actual], _estiloPie);
            GUILayout.Space(6f);
            GUILayout.Label("Los chunks DORMIDOS no repintan: un campo que cambia sin cambiar la materia (la temperatura de una roca, el vapor del aire quieto) puede ir hasta 30 frames por detrás. Es el refresco completo del renderer, no un fallo de la vista.", _estiloAyuda);
        }

        private void DibujarParametros(string grupo)
        {
            foreach (var p in LabParams.Registro)
            {
                if (p.Grupo != grupo) continue;
                float v = p.Leer();
                bool esDef = p.EsDefault;
                GUILayout.BeginHorizontal();
                GUILayout.Label((esDef ? "" : "● ") + p.Nombre, _estiloPie, GUILayout.Width(UiStyles.S(190f)));
                string valor = p.Entero ? ((int)v).ToString() : v.ToString("F2");
                GUILayout.Label(valor + " " + p.Unidad, _estiloPie, GUILayout.Width(UiStyles.S(120f)));
                if (GUILayout.Button("D", _estiloBoton, GUILayout.Width(UiStyles.S(24f)))) p.Escribir(p.Def);
                if (GUILayout.Button("?", _estiloBoton, GUILayout.Width(UiStyles.S(24f))))
                {
                    if (_ayudaAbierta.Contains(p.Clave)) _ayudaAbierta.Remove(p.Clave); else _ayudaAbierta.Add(p.Clave);
                }
                GUILayout.EndHorizontal();
                float nv = GUILayout.HorizontalSlider(v, p.Min, p.Max);
                if (p.Entero) nv = Mathf.Round(nv);
                if (!Mathf.Approximately(nv, v)) p.Escribir(nv);
                if (_ayudaAbierta.Contains(p.Clave))
                {
                    GUILayout.Label($"[{p.Clave}] default {p.Def} · rango {p.Min}..{p.Max}" + (p.RequiereReconstruir ? " · aplica al reconstruir" : " · en vivo"), _estiloAyuda);
                    if (!string.IsNullOrEmpty(p.Ayuda)) GUILayout.Label(p.Ayuda, _estiloAyuda);
                }
            }
        }

        /// <summary>
        /// (R140) Rótulo junto al cursor con el material bajo el ratón, su estado en palabras y
        /// sus cuatro campos. Solo con el panel abierto y el ratón fuera de él.
        /// (R142, R19-6) El texto se reconstruye SOLO cuando cambia lo que muestra: se dibuja cada
        /// frame, y el proyecto no admite allocs por frame. El `GUIContent` es de instancia y el
        /// nombre viene ya en mayúsculas de la tabla, así que un cursor quieto no asigna nada.
        /// </summary>
        private void DibujarLectura()
        {
            if (!_abierto || RatonSobrePanel || Event.current.type != EventType.Repaint) return;
            var cam = Camera.main; var m = Mouse.current; var g = _sim != null ? _sim.Grid : null;
            if (cam == null || m == null || g == null) return;

            Vector2 p = m.position.ReadValue();
            Vector3 mundo = cam.ScreenToWorldPoint(new Vector3(p.x, p.y, 10f));
            int cx = Mathf.FloorToInt(mundo.x / SimRenderer.CellWorldSize);
            int cy = Mathf.FloorToInt(mundo.y / SimRenderer.CellWorldSize);
            if (!CellGrid.InBounds(cx, cy)) return;

            int i = CellGrid.Idx(cx, cy);
            byte mat = g.mat[i], temp = g.temp[i], hum = g.humedad[i], car = g.carga[i], luz = g.luz[i], rep = g.reposo[i];
            // (R145, R23-11) Los dos parámetros que el rótulo LEE entran en la tupla: si no, mover
            // el slider no repinta el texto y el lector contradice a la física hasta que el cursor
            // se mueva de celda.
            int umbHum = LabParams.PlantaHumedadMin, umbTurb = LabParams.TurbidezFuente;
            if (i != _lecturaIdx || mat != _lecturaMat || temp != _lecturaTemp || hum != _lecturaHum
                || car != _lecturaCarga || luz != _lecturaLuz || rep != _lecturaReposo
                || umbHum != _lecturaUmbHum || umbTurb != _lecturaUmbTurb)
            {
                _lecturaUmbHum = umbHum; _lecturaUmbTurb = umbTurb;
                _lecturaIdx = i; _lecturaMat = mat; _lecturaTemp = temp; _lecturaHum = hum;
                _lecturaCarga = car; _lecturaLuz = luz; _lecturaReposo = rep;

                _sbLectura.Length = 0;
                _sbLectura.Append(LabMateriales.NombreMayus(mat));
                string estado = LabMateriales.Estado(mat, hum, car);
                if (estado != null) _sbLectura.Append("  ·  ").Append(estado);
                _sbLectura.Append('\n').Append('(').Append(cx).Append(", ").Append(cy).Append(")   ")
                          .Append(temp * 2 - 120).Append(" °C");
                if (hum > 0) _sbLectura.Append("   humedad ").Append(hum);
                if (car > 0) _sbLectura.Append("   carga ").Append(car);
                if (luz > 0) _sbLectura.Append("   luz ").Append(luz);
                if (rep > 0) _sbLectura.Append("   reposo ").Append(rep);
                _lecturaTexto.text = _sbLectura.ToString();
                _lecturaTam = _estiloLectura.CalcSize(_lecturaTexto);
            }

            float ancho = _lecturaTam.x + UiStyles.S(10f), alto = UiStyles.S(38f);
            // A la derecha del cursor, salvo que no quepa; siempre dentro de la pantalla.
            float rx = p.x + UiStyles.S(18f);
            if (rx + ancho > Screen.width) rx = p.x - UiStyles.S(18f) - ancho;
            float ry = Mathf.Clamp(Screen.height - p.y - alto - UiStyles.S(8f), 0f, Screen.height - alto);
            var r = new Rect(rx, ry, ancho, alto);

            var antes = GUI.color;
            GUI.color = new Color(0.06f, 0.06f, 0.07f, 0.82f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = antes;
            GUI.Label(r, _lecturaTexto, _estiloLectura);
        }

        private void PrepararEstilos()
        {
            if (_estiloTitulo != null) return;
            UiStyles.Preparar();
            _estiloTitulo = new GUIStyle(UiStyles.Cuerpo) { fontSize = UiStyles.F(11), fontStyle = FontStyle.Bold };
            _estiloTitulo.normal.textColor = new Color(0.78f, 0.72f, 0.62f, 1f);
            _estiloPie = new GUIStyle(UiStyles.Cuerpo) { fontSize = UiStyles.F(12), wordWrap = true };
            _estiloPie.normal.textColor = new Color(0.92f, 0.88f, 0.80f, 0.9f);
            _estiloAyuda = new GUIStyle(_estiloPie) { fontSize = UiStyles.F(11), fontStyle = FontStyle.Italic };
            _estiloAyuda.normal.textColor = new Color(0.75f, 0.80f, 0.70f, 0.9f);
            _estiloBoton = new GUIStyle(GUI.skin.button) { fontSize = UiStyles.F(11), alignment = TextAnchor.MiddleCenter };
            _estiloBotonSel = new GUIStyle(_estiloBoton) { fontStyle = FontStyle.Bold };
            _estiloBotonSel.normal.textColor = new Color(1f, 0.82f, 0.55f, 1f);
            _estiloCampo = new GUIStyle(GUI.skin.textField) { fontSize = UiStyles.F(12) };
            _estiloAgarre = new GUIStyle(GUI.skin.box) { fontSize = UiStyles.F(10), alignment = TextAnchor.MiddleCenter };
            _estiloAgarre.normal.textColor = new Color(0.70f, 0.66f, 0.58f, 0.85f);
            _estiloLectura = new GUIStyle(UiStyles.Cuerpo) { fontSize = UiStyles.F(12), alignment = TextAnchor.UpperLeft, padding = new RectOffset(6, 6, 3, 3) };
            _estiloLectura.normal.textColor = new Color(1f, 0.94f, 0.82f, 1f);
        }
    }
}
