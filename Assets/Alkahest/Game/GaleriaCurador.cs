using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 127, Meta 1 de docs/PLAN_GALERIA_DE_ESTILO.md) EL CURADOR DE LA
    /// GALERÍA: el acceso dev para vestir el banco de imagen sin ensuciar la
    /// pantalla. Solo existe en ModoGaleria (lo crea SpawnGaleria) y su regla
    /// de oro es que CERRADO NO SE VE NADA: ni icono, ni rótulo.
    ///
    /// Teclas (todas con las guardas de la regla 12):
    ///   F8          abre/cierra el curador (G era del termómetro/gesto — R128).
    ///   Ctrl+1..9   teletransporta a las 9 áreas (siempre, aun cerrado).
    ///   F10         LA RONDA DE CAPTURAS: recorre las 9 áreas y guarda un PNG
    ///               por área en Galeria/capturas/&lt;fecha-hora&gt;/ — el
    ///               comparador objetivo entre versiones de arte.
    /// Con el curador ABIERTO:
    ///   clic izq.       coloca el elemento seleccionado (mantener = pintar).
    ///   clic der.       quita (talla aire con el radio del pincel).
    ///   C + clic izq.   copia la MATERIA bajo el pincel (radio -/+; la roca
    ///                   madre y los sólidos fabricados no viajan) y el clic
    ///                   izq. la estampa (duplicar composiciones).
    ///   teclas - y +    radio del pincel (0..6).
    ///
    /// El MOVER no se reinventa: las máquinas se mudan con la MUDANZA de
    /// siempre (botón central). Las máquinas reales entran al catálogo en la
    /// Meta 1b (su Init ancla la cota al plano del taller — ver el plan §4).
    /// Regla del plan: nada de la Galería migra al juego sin su propia ronda.
    /// </summary>
    public sealed class GaleriaCurador : MonoBehaviour
    {
        private const int WindowId = 918273; // constante, jamás GetInstanceID (guía del proyecto).

        /// <summary>(R128) Con el curador abierto, frasco y cincel CEDEN los clics (misma familia de guardas que DevPalette.IsOpen — regla 37 de exclusión simétrica).</summary>
        public static bool Abierto => _instancia != null && _instancia._abierto;
        private static GaleriaCurador _instancia;

        private AlkahestSim _sim;
        private ApprenticeController _aprendiz;
        private bool _abierto;
        private int _sel;
        private int _radio = 2;
        private float _proximaAplicacion;
        private bool _capturando;
        private Rect _ventana = new Rect(12f, 60f, 250f, 100f);
        private GUIStyle _estiloTitulo, _estiloPie, _estiloBoton, _estiloBotonSel;

        // La estampa (duplicar): materia copiada con C+clic. (R128) Ahora es
        // POR RADIO (el mismo del pincel, -/+) y SOLO materia suelta: la roca
        // madre y el piso no viajan en la estampa — copiaba "mucho de todo" y
        // restaba utilidad (feedback de Cesar).
        private byte[,] _estampa;
        private float _avisoHasta, _avisoRadioHasta;
        private Texture2D _texAnillo;

        // (R128) FOGATAS PERSISTENTES: la brasa real se consume a ceniza (por
        // eso Cesar solo encontró "un puñado de cenizas"). El curador recuerda
        // dónde puso fogatas y las reaviva cada pocos segundos — salvo que la
        // hayan QUITADO a mano (área vaciada sin ceniza: se olvida).
        private readonly List<Vector2Int> _fogatas = new List<Vector2Int>();
        private float _fogataAcc;

        private struct Colocable
        {
            public string Nombre;
            public byte Mat;          // material principal
            public bool Estable;      // PaintStable (nace a temperatura estable) vs Paint
            public bool Fuego;        // además, llama encima (fogatas)
            public string Maquina;    // si no es null, coloca un aparato real (grupo MÁQUINAS)
            public string Grupo;
        }
        private static readonly Colocable[] Catalogo =
        {
            new Colocable { Grupo = "FUEGO",   Nombre = "fogata",           Mat = MaterialId.Brasa, Estable = true, Fuego = true },
            new Colocable { Grupo = "FUEGO",   Nombre = "lecho de brasas",  Mat = MaterialId.Brasa, Estable = true },
            new Colocable { Grupo = "FUEGO",   Nombre = "llama suelta",     Mat = MaterialId.Fire },
            new Colocable { Grupo = "MATERIA", Nombre = "agua",             Mat = MaterialId.Water, Estable = true },
            new Colocable { Grupo = "MATERIA", Nombre = "arena",            Mat = MaterialId.Sand, Estable = true },
            new Colocable { Grupo = "MATERIA", Nombre = "aceite",           Mat = MaterialId.Oil, Estable = true },
            new Colocable { Grupo = "MATERIA", Nombre = "lodo",             Mat = MaterialId.Limo, Estable = true },
            new Colocable { Grupo = "MATERIA", Nombre = "ceniza",           Mat = MaterialId.Ash, Estable = true },
            new Colocable { Grupo = "MATERIA", Nombre = "nutriente",        Mat = MaterialId.Nutrient, Estable = true },
            new Colocable { Grupo = "MATERIA", Nombre = "hielo",            Mat = MaterialId.Ice, Estable = true },
            new Colocable { Grupo = "ROCA",    Nombre = "piedra",           Mat = MaterialId.Stone, Estable = true },
            new Colocable { Grupo = "ROCA",    Nombre = "piso estructural", Mat = MaterialId.PisoEstructural, Estable = true },
            new Colocable { Grupo = "MÁQUINAS", Nombre = "crisol",          Maquina = "crisol" },
            new Colocable { Grupo = "MÁQUINAS", Nombre = "alambique",       Maquina = "alambique" },
            new Colocable { Grupo = "MÁQUINAS", Nombre = "prensa",          Maquina = "prensa" },
            new Colocable { Grupo = "MÁQUINAS", Nombre = "banco de chispa", Maquina = "chispa" },
            new Colocable { Grupo = "MÁQUINAS", Nombre = "placa ígnea",     Maquina = "placaIgnea" },
            new Colocable { Grupo = "MÁQUINAS", Nombre = "placa gélida",    Maquina = "placaGelida" },
            new Colocable { Grupo = "MÁQUINAS", Nombre = "caño de agua (naciente)", Maquina = "cano" },
            new Colocable { Grupo = "MÁQUINAS", Nombre = "balda (repisa)",  Maquina = "balda" },
            new Colocable { Grupo = "MÁQUINAS", Nombre = "quitar máquina (a la bodega)", Maquina = "quitar" },
        };

        // Las máquinas que EL CURADOR colocó (solo esas se pueden quitar/llevar
        // a la bodega — las del juego real no existen en la Galería).
        private readonly List<Component> _misMaquinas = new List<Component>();
        private int _slotBodega;

        public static GaleriaCurador Crear(AlkahestSim sim, ApprenticeController aprendiz)
        {
            var go = new GameObject("GaleriaCurador");
            var c = go.AddComponent<GaleriaCurador>();
            c._sim = sim;
            c._aprendiz = aprendiz;
            _instancia = c;
            return c;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _sim == null) return;
            bool tecladoLibre = !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto && !DayCycle.InputLocked;
            if (!tecladoLibre) return;

            // (R128) F8, no G: la G ya la usan el termómetro y el gesto de
            // prueba — Cesar la pulsó y se le abrieron dos cosas a la vez.
            if (kb.f8Key.wasPressedThisFrame) _abierto = !_abierto;

            // Teletransporte por áreas (siempre disponible en la Galería).
            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (ctrl && _aprendiz != null)
            {
                for (int i = 0; i < 9; i++)
                {
                    Key tecla = (Key)((int)Key.Digit1 + i);
                    if (kb[tecla].wasPressedThisFrame)
                    {
                        TeleportarA(i);
                        break;
                    }
                }
            }

            if (kb.f10Key.wasPressedThisFrame && !_capturando) StartCoroutine(RondaDeCapturas());
            // (R128) La R funciona con o sin curador (Cesar la probó cerrada y
            // "no vio el cambio"): en la Galería la R del cincel no aplica, y
            // ahora el resultado se AVISA en pantalla 4 segundos.
            if (kb.rKey.wasPressedThisFrame && !Cincel.ModoActivo) { RecargarTexturas(); _avisoHasta = Time.unscaledTime + 4f; }

            RefrescarFogatas();

            if (!_abierto) return;

            if (kb.minusKey.wasPressedThisFrame) { _radio = Mathf.Max(0, _radio - 1); _avisoRadioHasta = Time.unscaledTime + 1.2f; }
            if (kb.equalsKey.wasPressedThisFrame) { _radio = Mathf.Min(6, _radio + 1); _avisoRadioHasta = Time.unscaledTime + 1.2f; }

            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return;
            Vector2 pantalla = mouse.position.ReadValue();
            // No pintar a través de la ventana del catálogo.
            if (_ventana.Contains(new Vector2(pantalla.x, Screen.height - pantalla.y))) return;
            Vector3 mundo = cam.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, 10f));
            int cx = Mathf.FloorToInt(mundo.x / SimRenderer.CellWorldSize);
            int cy = Mathf.FloorToInt(mundo.y / SimRenderer.CellWorldSize);
            if (!CellGrid.InBounds(cx, cy)) return;

            bool copiar = kb.cKey.isPressed;
            if (mouse.leftButton.isPressed && copiar)
            {
                if (mouse.leftButton.wasPressedThisFrame) CopiarEstampa(cx, cy);
                return;
            }
            var seleccion = Catalogo[Mathf.Clamp(_sel < 0 ? 0 : _sel, 0, Catalogo.Length - 1)];
            bool esMaquina = _sel >= 0 && seleccion.Maquina != null;
            if (esMaquina)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    if (seleccion.Maquina == "quitar") QuitarMaquinaCercana(cx, cy);
                    else ColocarMaquina(seleccion.Maquina, cx, cy);
                }
            }
            else if (mouse.leftButton.isPressed && Time.unscaledTime >= _proximaAplicacion)
            {
                _proximaAplicacion = Time.unscaledTime + 0.06f;
                if (_estampa != null && _sel == -1) EstamparEn(cx, cy);
                else Aplicar(seleccion, cx, cy);
            }
            if (mouse.rightButton.isPressed && Time.unscaledTime >= _proximaAplicacion)
            {
                _proximaAplicacion = Time.unscaledTime + 0.06f;
                _sim.Paint(cx, cy, _radio, MaterialId.Empty); // quitar = tallar aire, mismo gesto que el cincel
            }
        }

        // =================================================================
        // HOT-LOAD DE TEXTURAS (Meta 2, primer peldaño): Cesar suelta
        // Galeria/roca_superficie.png (teselable, 512 o 1024) junto al
        // proyecto y pulsa R con el curador abierto — la piel entera cambia
        // de piel sin recompilar. El resto de la Meta 2 (masa/cantos, A/B,
        // perillas) se apoya en este mismo camino.
        // =================================================================
        private string _estadoTextura = "procedural (suelta Galeria/roca_superficie.png y pulsa R)";
        private void RecargarTexturas()
        {
            const string ruta = "Galeria/roca_superficie.png";
            if (!System.IO.File.Exists(ruta)) { _estadoTextura = "no encontré " + ruta; return; }
            var datos = System.IO.File.ReadAllBytes(ruta);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true);
            if (!tex.LoadImage(datos)) { Destroy(tex); _estadoTextura = "el PNG no se pudo leer"; return; }
            var piel = PielDeRoca.Instancia;
            if (piel == null) { Destroy(tex); _estadoTextura = "sin piel de roca activa (F7)"; return; }
            piel.CargarTexturaDeRoca(tex);
            _estadoTextura = "cargada " + ruta + " (" + tex.width + "x" + tex.height + ")";
        }

        // =================================================================
        // MÁQUINAS EN EL CATÁLOGO (Meta 1b) — "el resto de las cosas que
        // hicimos antes". El truco: cada estación sabe mudarse (IMovible,
        // regla 36: jamás re-Init), así que colocar = Init en su ancla
        // clásica + Reposicionar al cursor. Como en la Galería el ancla
        // clásica es roca maciza (el primer Reposicionar "borra" una
        // mampostería que nunca existió y tallaría un hueco fantasma), se
        // toma una FOTO de esas celdas antes y se restauran después.
        // =================================================================
        private void ColocarMaquina(string tipo, int cx, int cy)
        {
            var conocimiento = _aprendiz != null ? _aprendiz.GetComponent<SubstanceKnowledge>() : null;
            var go = new GameObject("Galeria_" + tipo + "_" + _misMaquinas.Count);
            Component comp = null;
            switch (tipo)
            {
                case "crisol":
                {
                    var m = go.AddComponent<Crisol>();
                    m.Init(_sim, _aprendiz != null ? _aprendiz.transform : null, conocimiento, SimLevelBuilder.CrisolX);
                    comp = m; break;
                }
                case "alambique":
                {
                    var m = go.AddComponent<Alambique>();
                    m.Init(_sim, _aprendiz != null ? _aprendiz.transform : null, SimLevelBuilder.AlambiqueX);
                    comp = m; break;
                }
                case "prensa":
                {
                    var m = go.AddComponent<Prensa>();
                    m.Init(_sim, _aprendiz != null ? _aprendiz.transform : null, SimLevelBuilder.PrensaX);
                    comp = m; break;
                }
                case "chispa":
                {
                    var m = go.AddComponent<BancoChispa>();
                    m.Init(_sim, _aprendiz != null ? _aprendiz.transform : null, conocimiento, SimLevelBuilder.BancoChispaX);
                    comp = m; break;
                }
                // (R128) Los de abajo se COLOCAN DIRECTO en el cursor (su Init
                // acepta coordenadas): sin ancla clásica, sin foto, sin fantasma.
                case "placaIgnea":
                {
                    var m = go.AddComponent<HeatPlate>();
                    m.Init(_sim, _aprendiz != null ? _aprendiz.transform : null, cx - 3, cx + 3, cy);
                    _misMaquinas.Add(m); return;
                }
                case "placaGelida":
                {
                    var m = go.AddComponent<ChillStone>();
                    m.Init(_sim, _aprendiz != null ? _aprendiz.transform : null, cx - 3, cx + 3, cy);
                    _misMaquinas.Add(m); return;
                }
                case "cano":
                {
                    var m = go.AddComponent<Dispenser>();
                    m.Init(_sim, _aprendiz != null ? _aprendiz.transform : null, cx, cy, MaterialId.Water);
                    _misMaquinas.Add(m); return;
                }
                case "balda":
                {
                    var m = go.AddComponent<Balda>();
                    m.Init(_sim, cx - 4, cx + 4, cy);
                    _misMaquinas.Add(m); return;
                }
            }
            var mov = comp as IMovible;
            if (mov == null) { Destroy(go); return; }
            if (!MoverMaquinaConFoto(mov, new Vector2Int(cx, cy))) { Destroy(go); return; }
            _misMaquinas.Add(comp);
        }

        /// <summary>Reposiciona tomando antes una foto de la huella vieja y restaurándola después (el hueco fantasma del docblock de arriba). Devuelve false si no cabe.</summary>
        private bool MoverMaquinaConFoto(IMovible mov, Vector2Int destino)
        {
            if (!mov.CabeEnAncla(destino)) return false;
            var ancla = mov.AnclaCelda;
            int w = Mathf.CeilToInt(mov.TamanoMundo.x / SimRenderer.CellWorldSize) + 8;
            int h = Mathf.CeilToInt(mov.TamanoMundo.y / SimRenderer.CellWorldSize) + 12;
            int x0 = Mathf.Max(1, ancla.x - 4), y0 = Mathf.Max(1, ancla.y - 8);
            int x1 = Mathf.Min(CellGrid.W - 2, x0 + w), y1 = Mathf.Min(CellGrid.H - 2, y0 + h);
            var foto = new byte[(x1 - x0 + 1) * (y1 - y0 + 1)];
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    foto[(y - y0) * (x1 - x0 + 1) + x - x0] = _sim.Grid.GetMat(x, y);
            mov.Reposicionar(destino);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    byte quiere = foto[(y - y0) * (x1 - x0 + 1) + x - x0];
                    if (_sim.Grid.GetMat(x, y) != quiere) _sim.PaintCell(x, y, quiere, CellGrid.AmbientRaw);
                }
            return true;
        }

        private void QuitarMaquinaCercana(int cx, int cy)
        {
            Vector3 punto = new Vector3((cx + 0.5f) * SimRenderer.CellWorldSize, (cy + 0.5f) * SimRenderer.CellWorldSize, 0f);
            Component mejor = null; float mejorDist = 2.5f; // radio de gracia: 25 celdas
            foreach (var c in _misMaquinas)
            {
                if (c == null) continue;
                var mv = c as IMovible;
                if (mv == null) continue; // (R128) algunos colocables no son movibles: se quitan tallándolos o quedan.
                float d = Vector3.Distance(mv.CentroMundo, punto);
                if (d < mejorDist) { mejorDist = d; mejor = c; }
            }
            if (mejor == null) return;
            // A la bodega (Sala 30..300 x 262..284 del plano): fila de estantes de 55 celdas.
            var destino = new Vector2Int(36 + (_slotBodega % 5) * 52, 263);
            _slotBodega++;
            ((IMovible)mejor).Reposicionar(destino);
        }

        private void TeleportarA(int i)
        {
            float celda = SimRenderer.CellWorldSize;
            var destino = new Vector3(
                (SimLevelBuilder.GaleriaAnclaX[i] + 0.5f) * celda,
                (SimLevelBuilder.GaleriaAnclaY[i] + 0.5f) * celda, 0f);
            _aprendiz.transform.position = destino;
            // La cámara sigue al aprendiz con suavizado: en un teletransporte
            // queremos el ENCUADRE ya puesto (la ronda de capturas depende de
            // esto), así que se planta de golpe en el destino.
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(destino.x, destino.y, cam.transform.position.z);
        }

        private void Aplicar(Colocable c, int cx, int cy)
        {
            if (c.Estable) _sim.PaintStable(cx, cy, _radio, c.Mat);
            else
            {
                // Llama suelta: nace CALIENTE (regla 22: Paint mueve, PaintCell fija temperatura).
                for (int dy = -_radio; dy <= _radio; dy++)
                    for (int dx = -_radio; dx <= _radio; dx++)
                        if (dx * dx + dy * dy <= _radio * _radio)
                            _sim.PaintCell(cx + dx, cy + dy, c.Mat, 220);
            }
            if (c.Fuego)
            {
                for (int dx = -1; dx <= 1; dx++)
                    _sim.PaintCell(cx + dx, cy + _radio + 1, MaterialId.Fire, 220);
                if (_fogatas.Count < 64) _fogatas.Add(new Vector2Int(cx, cy));
            }
        }

        /// <summary>(R128) Reaviva las fogatas colocadas: la brasa se consume sola y sin esto la fogata muere en cenizas en un minuto.</summary>
        private void RefrescarFogatas()
        {
            _fogataAcc += Time.deltaTime;
            if (_fogataAcc < 5f) return;
            _fogataAcc = 0f;
            for (int i = _fogatas.Count - 1; i >= 0; i--)
            {
                var f = _fogatas[i];
                int brasas = 0, cenizas = 0;
                for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        if (!CellGrid.InBounds(f.x + dx, f.y + dy)) continue;
                        byte m = _sim.Grid.GetMat(f.x + dx, f.y + dy);
                        if (m == MaterialId.Brasa || m == MaterialId.Fire) brasas++;
                        else if (m == MaterialId.Ash) cenizas++;
                    }
                if (brasas == 0 && cenizas == 0) { _fogatas.RemoveAt(i); continue; } // la quitaron a mano: se respeta.
                if (brasas < 6)
                {
                    _sim.PaintStable(f.x, f.y, 2, MaterialId.Brasa);
                    for (int dx = -1; dx <= 1; dx++) _sim.PaintCell(f.x + dx, f.y + 3, MaterialId.Fire, 220);
                }
            }
        }

        private void CopiarEstampa(int cx, int cy)
        {
            int lado = _radio * 2 + 1;
            _estampa = new byte[lado, lado];
            int copiadas = 0;
            for (int dy = 0; dy < lado; dy++)
                for (int dx = 0; dx < lado; dx++)
                {
                    int x = cx - _radio + dx, y = cy - _radio + dy;
                    byte m = CellGrid.InBounds(x, y) ? _sim.Grid.GetMat(x, y) : MaterialId.Empty;
                    // Solo MATERIA SUELTA viaja: ni roca madre ni sólidos fabricados.
                    if (m == MaterialId.Stone || (m != MaterialId.Empty && _universoEstatico(m))) m = MaterialId.Empty;
                    _estampa[dy, dx] = m;
                    if (m != MaterialId.Empty) copiadas++;
                }
            if (copiadas == 0) { _estampa = null; return; } // no había materia bajo el pincel: no cambiar de modo.
            _sel = -1; // el pincel pasa a ser la estampa.
        }

        private bool _universoEstatico(byte m) => _sim.Universe.Get(m).archetype == MaterialArchetype.StaticSolid;

        private void EstamparEn(int cx, int cy)
        {
            int lado = _estampa.GetLength(0), medio = lado / 2;
            for (int dy = 0; dy < lado; dy++)
                for (int dx = 0; dx < lado; dx++)
                {
                    byte m = _estampa[dy, dx];
                    if (m == MaterialId.Empty) continue; // la estampa no borra: solo añade lo copiado.
                    _sim.PaintStable(cx - medio + dx, cy - medio + dy, 0, m);
                }
        }

        // =================================================================
        // LA RONDA DE CAPTURAS (F10): 9 áreas, mismo encuadre siempre, PNG
        // con fecha — el comparador objetivo entre versiones de arte.
        // =================================================================
        private IEnumerator RondaDeCapturas()
        {
            _capturando = true;
            bool estabaAbierto = _abierto;
            _abierto = false; // la captura sale limpia
            string carpeta = System.IO.Path.Combine("Galeria", "capturas", System.DateTime.Now.ToString("yyyy-MM-dd_HHmm"));
            System.IO.Directory.CreateDirectory(carpeta);
            Vector3 posOriginal = _aprendiz != null ? _aprendiz.transform.position : Vector3.zero;
            for (int i = 0; i < 9; i++)
            {
                TeleportarA(i);
                for (int f = 0; f < 4; f++) yield return null; // cámara plantada + repintado del mundo.
                Capturar(System.IO.Path.Combine(carpeta, $"area{i + 1}_{SimLevelBuilder.GaleriaAnclaNombre[i].Replace(' ', '_')}.png"));
            }
            if (_aprendiz != null) _aprendiz.transform.position = posOriginal;
            _abierto = estabaAbierto;
            _capturando = false;
            Debug.Log($"[TenThousandYears] GALERÍA: ronda de capturas guardada en {carpeta}");
        }

        private static void Capturar(string ruta)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            // URP: el camino soportado es SubmitRenderRequest (Camera.Render "a pelo" no rinde en SRP).
            var peticion = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, peticion)) RenderPipeline.SubmitRenderRequest(cam, peticion);
            else { var prevT = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = prevT; }
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Destroy(tex);
            rt.Release();
            Destroy(rt);
        }

        // =================================================================
        // LA VENTANA — solo existe mientras el curador está abierto.
        // =================================================================
        private void OnGUI()
        {
            if (DayCycle.InputLocked) return;
            PrepararEstilos();
            // (R128) Aviso del hot-load visible AUNQUE el curador esté cerrado.
            if (Time.unscaledTime < _avisoHasta && !string.IsNullOrEmpty(_estadoTextura))
            {
                float wA = UiStyles.Ancho(_estiloPie, "textura: " + _estadoTextura) + 16f;
                GUI.color = new Color(0.06f, 0.05f, 0.04f, 0.8f);
                GUI.DrawTexture(new Rect((Screen.width - wA) * 0.5f, UiStyles.S(48f), wA, UiStyles.S(22f)), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect((Screen.width - wA) * 0.5f + 8f, UiStyles.S(48f), wA, UiStyles.S(22f)), "textura: " + _estadoTextura, _estiloPie);
            }
            if (!_abierto) return;
            // (R128) EL ANILLO DEL PINCEL: el radio se VE en el cursor (Cesar
            // no tuvo evidencia del -/+). Se dibuja en pantalla, no en el mundo.
            var mouseGui = Mouse.current;
            var camGui = Camera.main;
            if (mouseGui != null && camGui != null)
            {
                if (_texAnillo == null) _texAnillo = CrearAnillo();
                float pxPorCelda = Screen.height / (camGui.orthographicSize * 2f) * SimRenderer.CellWorldSize;
                float diam = Mathf.Max(6f, (_radio * 2 + 1) * pxPorCelda);
                Vector2 mp = mouseGui.position.ReadValue();
                float brillo = Time.unscaledTime < _avisoRadioHasta ? 1f : 0.55f;
                GUI.color = new Color(1f, 0.92f, 0.75f, brillo);
                GUI.DrawTexture(new Rect(mp.x - diam * 0.5f, Screen.height - mp.y - diam * 0.5f, diam, diam), _texAnillo);
                GUI.color = Color.white;
            }
            GUI.depth = 5;
            _ventana = GUILayout.Window(WindowId, _ventana, DibujarVentana, "EL CURADOR", GUI.skin.window);

            // Una sola línea de ayuda abajo, y solo con el curador abierto.
            string pie = "clic: colocar · clic der.: quitar · C+clic: copia materia (radio) · -/+: radio (" + _radio + ") · Ctrl+1..9: áreas · R: textura · F10: capturas · F8: cerrar";
            float w = UiStyles.Ancho(_estiloPie, pie) + 16f;
            GUI.Label(new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyles.S(26f), w, UiStyles.S(22f)), pie, _estiloPie);
        }

        private void DibujarVentana(int id)
        {
            string grupo = null;
            for (int i = 0; i < Catalogo.Length; i++)
            {
                if (Catalogo[i].Grupo != grupo)
                {
                    grupo = Catalogo[i].Grupo;
                    GUILayout.Label(grupo, _estiloTitulo);
                }
                if (GUILayout.Button((_sel == i ? "· " : "  ") + Catalogo[i].Nombre, _sel == i ? _estiloBotonSel : _estiloBoton))
                {
                    _sel = i;
                    _estampa = null;
                }
            }
            GUILayout.Label("TEXTURA DE ROCA (R recarga)", _estiloTitulo);
            GUILayout.Label(_estadoTextura, _estiloPie);
            GUILayout.Label("ESTAMPA", _estiloTitulo);
            GUILayout.Label(_estampa != null && _sel == -1 ? "lista (clic para estampar)" : "C+clic sobre materia para copiarla", _estiloPie);
            GUILayout.Space(6f);
            if (GUILayout.Button("RONDA DE CAPTURAS (F10)", _estiloBoton) && !_capturando) StartCoroutine(RondaDeCapturas());
            GUI.DragWindow();
        }

        private static Texture2D CrearAnillo()
        {
            const int N = 64;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color32[N * N];
            float c = (N - 1) * 0.5f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - 0.94f) * 14f);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px); t.Apply(false, true);
            return t;
        }

        private void PrepararEstilos()
        {
            if (_estiloTitulo != null) return;
            UiStyles.Preparar();
            _estiloTitulo = new GUIStyle(UiStyles.Cuerpo) { fontSize = UiStyles.F(11), fontStyle = FontStyle.Bold };
            _estiloTitulo.normal.textColor = new Color(0.78f, 0.72f, 0.62f, 1f);
            _estiloPie = new GUIStyle(UiStyles.Cuerpo) { fontSize = UiStyles.F(12) };
            _estiloPie.normal.textColor = new Color(0.92f, 0.88f, 0.80f, 0.9f);
            _estiloBoton = new GUIStyle(GUI.skin.button) { fontSize = UiStyles.F(12), alignment = TextAnchor.MiddleLeft };
            _estiloBotonSel = new GUIStyle(_estiloBoton) { fontStyle = FontStyle.Bold };
            _estiloBotonSel.normal.textColor = new Color(1f, 0.82f, 0.55f, 1f);
        }
    }
}
