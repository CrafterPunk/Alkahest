using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL TERMÓMETRO (CONTRATO_TERMICA.md §3a, ENCARGO I, ronda "LA FÍSICA
    /// HONESTA"): la herramienta de VALIDAR lo que las placas de calor/frío
    /// (encargo T, en paralelo) ya hacen de verdad. Mandato de Cesar (dormido,
    /// ronda autónoma): *"termómetros en grados centígrados ligeros para
    /// tomar temperatura en distintos puntos y validar"*.
    ///
    /// TECLA G (mnemónico "grados"; verificada libre con el mismo barrido que
    /// documenta Game/Cincel.cs: grep de <c>*Key</c> en Assets/Alkahest, sin
    /// ningún <c>gKey</c> previo). Alterna el MODO TERMÓMETRO:
    /// <see cref="ModoActivo"/> es, calcado de <see cref="Cincel.ModoActivo"/>,
    /// un flag estático de solo lectura hacia fuera que ADEMÁS es el
    /// indicador de "qué llevas en la mano" (ver el docblock de esa clase).
    ///
    /// QUÉ HACE:
    ///  · READOUT VIVO: con el modo activo, un chip junto al cursor con la
    ///    temperatura de la celda apuntada, en °C (<see cref="CellGrid.RawToC"/>,
    ///    que ya devuelve un entero -- "redondeado al grado" es gratis).
    ///  · SONDAS: clic izq. PINCHA una sonda en la celda apuntada (hasta
    ///    <see cref="MaxSondas"/> = 3, FIFO REAL por orden de plantado -- si
    ///    las tres están vivas se sustituye la más VIEJA, no una posición de
    ///    anillo ciega, ver <see cref="SlotParaNuevaSonda"/>). Cada sonda es
    ///    una etiqueta fija en el mundo, re-muestreada con acumulador PROPIO
    ///    a <see cref="SondeoSondaHz"/> (~4 Hz, nunca por frame) y que solo
    ///    reconstruye su string cuando el GRADO cambia de verdad (ver
    ///    <see cref="MuestrearSonda"/>) contra una tabla de 256 strings -- una
    ///    por byte raw posible -- construida UNA sola vez
    ///    (<see cref="AsegurarTablaGrados"/>): de ahí en adelante, tanto las
    ///    sondas como el readout del cursor son puro indexado de array, cero
    ///    allocs por frame.
    ///  · Clic der. QUITA la sonda que esté EXACTAMENTE en la celda apuntada
    ///    (silencio si no hay ninguna ahí, mismo criterio que
    ///    Flask.BloquearMaterialBajoElCursor cuando no encuentra nada).
    ///  · LAS SONDAS SOBREVIVEN AL MODO: apagar el termómetro (G otra vez) no
    ///    las borra ni deja de remuestrearlas -- son instrumentos plantados
    ///    en el mundo, no un overlay del modo (siguen leyendo temperatura
    ///    solas, como un termómetro de verdad). Apagar el modo solo deja de
    ///    aceptar clics nuevos (plantar/quitar) hasta volver a activarlo. G
    ///    otra vez para gestionarlas; H (Game/HintSystem.cs, pistas) no las
    ///    toca -- son archivos sin relación.
    ///
    /// EXCLUSIÓN DE MODOS (contrato: "como Cincel.ModoActivo") -- MISMA
    /// LIMITACIÓN DE PROPIEDAD DE ARCHIVOS que Game/Cincel.cs ya documentó en
    /// su día para Flask.cs, y que Game/Mudanza.cs documentó para Cincel.cs
    /// (CLAUDE.md regla 37, "tres modos excluyentes"): Termometro.cs SÍ puede
    /// CEDER el turno a Cincel/Mudanza (los comprueba y no actúa si
    /// cualquiera de los dos está activo, calcado del mismo chequeo que ya
    /// hace Flask.Update) y SÍ puede forzar la salida de Mudanza al
    /// activarse (<see cref="Mudanza.ForzarSalida"/>, la única puerta pública
    /// que existe hoy para un cuarto modo) -- pero NO puede forzar la salida
    /// de Cincel (no expone una puerta equivalente) NI impedir que
    /// Game/Flask.cs siga leyendo clic izq./der. en paralelo (ninguno de los
    /// dos archivos está en la lista de este encargo, ver CONTRATO_TERMICA.md
    /// §3). Efecto práctico: con el termómetro activo, apuntar a algo
    /// ASPIRABLE y hacer clic izq. planta una sonda Y ADEMÁS aspira ese
    /// material a la vez -- el mismo solapamiento estrecho que Cincel.cs
    /// documentó para Piedra en el playtest 16, aquí más ancho porque una
    /// sonda no filtra por material. Pendiente para una ronda futura con
    /// Flask.cs/Cincel.cs en alcance: añadir ahí
    /// <c>if (Termometro.ModoActivo) { ...; return; }</c> junto al resto de
    /// guardas de la regla 12 de CLAUDE.md.
    ///
    /// INVITADO EN MULTI (contrato: "la temperatura NO se replica, solo
    /// mat"): <see cref="AlkahestSim.Stepper"/> es <c>null</c> SOLO en el
    /// espejo de un invitado -- mismo criterio YA establecido por
    /// Audio/DirectorDeAudio.cs y Game/SubstanceKnowledge.cs ("MODO ESPEJO"),
    /// nunca un flag nuevo. Con <c>Stepper</c> nulo nadie escribe jamás
    /// <c>CellGrid.temp[]</c> en el espejo del invitado, así que leerlo
    /// mentiría con lo que sea que haya ahí (el ambiente de arranque, no la
    /// temperatura real de la placa del anfitrión). El readout y toda sonda
    /// muestran "—" en vez de un número, con una nota más larga la primera
    /// vez que se activa el modo en esa sesión ("solo el anfitrión mide, por
    /// ahora") que luego se simplifica a "—" a secas para no perseguir al
    /// cursor con un texto largo -- documentado, no un bug pendiente.
    /// </summary>
    [RequireComponent(typeof(ApprenticeController))]
    public sealed class Termometro : MonoBehaviour
    {
        /// <summary>¿Lleva el aprendiz el termómetro en la mano ahora mismo? Análogo a Cincel.ModoActivo/Mudanza.ModoActivo -- ver doc de clase.</summary>
        public static bool ModoActivo { get; private set; }

        private const int MaxSondas = 3;
        private const float SondeoSondaHz = 4f;
        private const float SondeoSondaDt = 1f / SondeoSondaHz;

        private AlkahestSim _sim;
        private ApprenticeController _apprentice;
        private Flask _flask; // solo para Avisar() -- mismo canal compartido que usan Cincel/Mudanza/StorageRack.

        private bool _hasCursorWorld;
        private Vector3 _cursorWorld;
        private bool _hasCursor;
        private Vector2Int _cursorCell;

        // -----------------------------------------------------------------
        // SONDAS: arrays paralelos de tamaño FIJO (MaxSondas=3) -- cero
        // Listas, cero allocs de colección. Ver doc de clase.
        // -----------------------------------------------------------------
        private readonly bool[] _sondaActiva = new bool[MaxSondas];
        private readonly int[] _sondaX = new int[MaxSondas];
        private readonly int[] _sondaY = new int[MaxSondas];
        private readonly byte[] _sondaRaw = new byte[MaxSondas];
        private readonly string[] _sondaLabel = new string[MaxSondas];
        /// <summary>Orden de plantado (contador creciente) -- FIFO real: al sustituir con las tres ocupadas, se elige el de valor MÁS BAJO aquí, no una posición de anillo ciega que un clic derecho intermedio podría desordenar.</summary>
        private readonly int[] _sondaOrdenPlantado = new int[MaxSondas];
        private readonly float[] _sondaAcumulador = new float[MaxSondas];
        private readonly SpriteRenderer[] _sondaPin = new SpriteRenderer[MaxSondas];
        private int _contadorPlantado;

        /// <summary>La nota larga de "solo el anfitrión mide" se dice UNA vez por activación del modo -- ver doc de clase.</summary>
        private bool _notaInvitadoMostrada;

        // -----------------------------------------------------------------
        // TABLA DE LABELS POR GRADO (cero allocs desde el primer uso, ver
        // doc de clase): 256 strings, una por byte raw posible, construida
        // UNA vez. Estática porque el mapeo raw->°C (CellGrid.RawToC) es
        // universal, no depende de la instancia ni de la semilla.
        // -----------------------------------------------------------------
        private static string[] _gradoLabels;

        private static void AsegurarTablaGrados()
        {
            if (_gradoLabels != null) return;
            _gradoLabels = new string[256];
            for (int raw = 0; raw < 256; raw++)
                _gradoLabels[raw] = CellGrid.RawToC((byte)raw) + "°";
        }

        // -----------------------------------------------------------------
        // Visual: icono de modo (qué llevas en la mano, mismo lenguaje que
        // Game/Cincel.cs) + un pin por sonda. Todo creado UNA vez en
        // BuildVisuals(); Update()/OnGUI() solo mueven/tiñen lo ya creado.
        // -----------------------------------------------------------------
        private const int PinSortingOrder = 41; // por encima del haz del frasco (40) y del cincel (38/39): la sonda es un objeto físico plantado, debe leerse por delante.
        private const int ModeIconSortingOrder = 62; // justo por encima del icono del cincel (61).
        private const float PinSizeWorld = 0.10f;
        private const float PinAlpha = 0.92f;
        private const float ModeIconAlpha = 0.95f;

        // Vidrio/metal frío de instrumento -- deliberadamente NO el latón de
        // Cincel/Flask (BrassBase) ni ningún color de UiStyles ya cargado de
        // significado (Frio/Aviso/Peligro son del ACENTO por temperatura, no
        // del icono de modo): un tono neutro de vidrio esmerilado para que el
        // icono se lea como "instrumento de medir", no como "calor" ni "frío".
        private static readonly Color32 ColorInstrumento = new Color32(200, 214, 222, 255);

        private SpriteRenderer _modeIconSr;

        private void Awake()
        {
            _apprentice = GetComponent<ApprenticeController>();
            _flask = GetComponent<Flask>();
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap, mismo patrón que Flask.Init/Cincel.Init/Mudanza.Init.</summary>
        public void Init(AlkahestSim sim)
        {
            _sim = sim;
            AsegurarTablaGrados();
            BuildVisuals();
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;

            // Mismas guardas y mismo orden que Cincel.Update/Flask.Update
            // (regla 12 de CLAUDE.md): un atajo de mundo nunca debe colarse
            // con el título/intro/fin de jornada bloqueando input, la paleta
            // dev abierta, un campo de texto escribiéndose, o el diario a
            // pantalla completa.
            if (DayCycle.InputLocked) { OcultarModoVisuales(); return; }
            if (Alkahest.Dev.DevPalette.IsOpen) { OcultarModoVisuales(); return; }
            if (UiStyles.EscribiendoTexto) { OcultarModoVisuales(); return; }
            if (JournalHud.Abierto) { OcultarModoVisuales(); return; }

            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (kb != null && kb.gKey.wasPressedThisFrame)
            {
                if (!ModoActivo)
                {
                    Mudanza.ForzarSalida(); // única puerta que puedo forzar -- ver doc de clase.
                    _notaInvitadoMostrada = false; // recordar la nota larga cada vez que se vuelve a sacar el termómetro.
                }
                ModoActivo = !ModoActivo;
                if (_flask != null)
                {
                    _flask.Avisar(ModoActivo
                        ? "termómetro en mano — clic izq. pincha, clic der. quita"
                        : "frasco en mano");
                }
                if (!ModoActivo) OcultarModoVisuales();
            }

            _hasCursorWorld = TryGetCursorWorld(out _cursorWorld);
            _hasCursor = _hasCursorWorld && CeldaDesdeCursorMundo(out _cursorCell);

            // Las sondas se remuestrean SIEMPRE, con el modo activo o no
            // (ver doc de clase: "son instrumentos plantados, no un overlay
            // del modo").
            ActualizarSondas();

            if (!ModoActivo) { OcultarModoVisuales(); return; } // el frasco/lo que sea manda: el termómetro no acepta clics nuevos.

            // (playtest 16/19, ver doc de clase) CEDE a Cincel/Mudanza -- el
            // termómetro no puede forzar la salida de Cincel (sin puerta
            // pública) pero sí puede negarse a actuar mientras cualquiera de
            // los dos esté activo, mismo patrón que ya usa Flask.Update.
            if (Cincel.ModoActivo || Mudanza.ModoActivo) { OcultarModoVisuales(); return; }

            // Mismo criterio que Cincel/Flask: sobre una redoma del estante
            // los clics son "guardar/recuperar", nunca una acción de mundo.
            bool ratonCapturado = StorageRack.RatonSobreRedoma();
            bool clicPlantar = mouse != null && mouse.leftButton.wasPressedThisFrame && !ratonCapturado;
            bool clicQuitar = mouse != null && mouse.rightButton.wasPressedThisFrame && !ratonCapturado;

            if (_hasCursor)
            {
                if (clicPlantar) PlantarSonda(_cursorCell);
                else if (clicQuitar) QuitarSondaApuntada(_cursorCell);
            }

            ActualizarModeIcon();
        }

        /// <summary>Mismo raycast puro cámara-&gt;plano z=0 que Cincel/Flask (privado en ambos, así que se repite aquí).</summary>
        private bool TryGetCursorWorld(out Vector3 world)
        {
            world = default;
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return false;

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter)) return false;

            world = ray.GetPoint(enter);
            return true;
        }

        private bool CeldaDesdeCursorMundo(out Vector2Int cell)
        {
            cell = _sim.WorldToCell(_cursorWorld);
            return CellGrid.InBounds(cell.x, cell.y);
        }

        private float ReachCellsSq()
        {
            float reachCells = Flask.ReachWorld / SimRenderer.CellWorldSize;
            return reachCells * reachCells;
        }

        // ===================================================================
        // SONDAS
        // ===================================================================

        private void PlantarSonda(Vector2Int cell)
        {
            Vector2Int apprenticeCell = _sim.WorldToCell(transform.position);
            float reachCellsSq = ReachCellsSq();
            int cdx = cell.x - apprenticeCell.x, cdy = cell.y - apprenticeCell.y;
            if (cdx * cdx + cdy * cdy > reachCellsSq)
            {
                if (_flask != null) _flask.Avisar("demasiado lejos — acércate");
                return;
            }

            int slot = SlotParaNuevaSonda();
            _sondaActiva[slot] = true;
            _sondaX[slot] = cell.x;
            _sondaY[slot] = cell.y;
            _sondaOrdenPlantado[slot] = ++_contadorPlantado;
            _sondaAcumulador[slot] = 0f;
            _sondaLabel[slot] = null; // fuerza reconstrucción en MuestrearSonda aunque el raw por defecto (0) coincida con lo que quedara del slot reciclado.
            _sondaRaw[slot] = 0;
            MuestrearSonda(slot);
            PosicionarPin(slot);
        }

        /// <summary>Slot para una sonda nueva: el primer hueco libre, o -- con las tres ocupadas -- la más VIEJA por orden real de plantado (ver el doc del campo <see cref="_sondaOrdenPlantado"/>).</summary>
        private int SlotParaNuevaSonda()
        {
            for (int i = 0; i < MaxSondas; i++) if (!_sondaActiva[i]) return i;

            int viejo = 0;
            for (int i = 1; i < MaxSondas; i++)
                if (_sondaOrdenPlantado[i] < _sondaOrdenPlantado[viejo]) viejo = i;
            return viejo;
        }

        private void QuitarSondaApuntada(Vector2Int cell)
        {
            for (int i = 0; i < MaxSondas; i++)
            {
                if (!_sondaActiva[i] || _sondaX[i] != cell.x || _sondaY[i] != cell.y) continue;
                _sondaActiva[i] = false;
                _sondaLabel[i] = null;
                if (_sondaPin[i] != null) _sondaPin[i].color = new Color(0f, 0f, 0f, 0f);
                return; // silencio si no hay ninguna en esa celda -- mismo criterio que Flask.BloquearMaterialBajoElCursor.
            }
        }

        private void ActualizarSondas()
        {
            for (int i = 0; i < MaxSondas; i++)
            {
                if (!_sondaActiva[i]) continue;
                _sondaAcumulador[i] += Time.deltaTime;
                if (_sondaAcumulador[i] < SondeoSondaDt) continue;
                _sondaAcumulador[i] -= SondeoSondaDt;
                MuestrearSonda(i);
                PosicionarPin(i); // barato (un array + un color, sin allocs): reafirma color/posición aunque no se haya movido.
            }
        }

        /// <summary>Relee la temperatura de la sonda `slot` y solo reconstruye su label si el GRADO cambió de verdad (cero allocs por frame -- ver doc de clase). En el invitado (Stepper == null) marca "—" siempre, sin tocar SampleTempRaw (ver doc de clase, "INVITADO EN MULTI").</summary>
        private void MuestrearSonda(int slot)
        {
            if (_sim.Stepper == null) { _sondaLabel[slot] = "—"; return; }

            byte raw = _sim.SampleTempRaw(_sondaX[slot], _sondaY[slot]);
            if (_sondaLabel[slot] != null && raw == _sondaRaw[slot]) return;
            _sondaRaw[slot] = raw;
            _sondaLabel[slot] = _gradoLabels[raw];
        }

        private void PosicionarPin(int slot)
        {
            var sr = _sondaPin[slot];
            if (sr == null) return;
            Vector3 world = _sim.CellToWorld(new Vector2Int(_sondaX[slot], _sondaY[slot]));
            sr.transform.position = new Vector3(world.x, world.y, -0.01f);
            Color c = _sim.Stepper == null ? UiStyles.TextoTenue : ColorPorC(CellGrid.RawToC(_sondaRaw[slot]));
            sr.color = new Color(c.r, c.g, c.b, PinAlpha);
        }

        /// <summary>Acento por temperatura (contrato: "frío azulado / ambiente neutro / caliente cálido"), compartido por sondas y readout. Umbrales fijos en °C -- simples y legibles, no dependen de la seed (a diferencia de la banda de crecimiento del Vivium, esto es SOLO lectura, no una regla de juego).</summary>
        private static Color ColorPorC(int c)
        {
            if (c <= 0) return UiStyles.Frio;
            if (c >= 90) return UiStyles.Peligro; // hirviendo o más allá.
            if (c >= 40) return UiStyles.Aviso;   // cálido de verdad.
            return UiStyles.Texto;                // ambiente/templado: neutro.
        }

        // ===================================================================
        // VISUAL
        // ===================================================================
        private void BuildVisuals()
        {
            var pinSprite = CrearSpriteBlanco1x1();
            for (int i = 0; i < MaxSondas; i++)
            {
                // Root aparte (NO hijo del aprendiz): una sonda queda
                // plantada en una celda del MUNDO, no viaja con el jugador.
                var go = new GameObject("TermometroSonda_" + i);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = pinSprite;
                sr.sortingOrder = PinSortingOrder;
                sr.color = new Color(0f, 0f, 0f, 0f);
                go.transform.localScale = Vector3.one * PinSizeWorld;
                _sondaPin[i] = sr;
            }

            var iconoSprite = CrearSpriteBlanco1x1();
            var iconoGo = new GameObject("TermometroIconoModo");
            iconoGo.transform.SetParent(transform, false);
            iconoGo.transform.localRotation = Quaternion.identity; // a diferencia del diamante del Cincel (45°): un icono redondeado de "instrumento", no de herramienta de picar.
            iconoGo.transform.localScale = Vector3.one * 0.12f;
            _modeIconSr = iconoGo.AddComponent<SpriteRenderer>();
            _modeIconSr.sprite = iconoSprite;
            _modeIconSr.sortingOrder = ModeIconSortingOrder;
            _modeIconSr.color = new Color(0f, 0f, 0f, 0f);
        }

        private static Sprite CrearSpriteBlanco1x1()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "AlkahestTermometroPunto", filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private void OcultarModoVisuales()
        {
            if (_modeIconSr != null) _modeIconSr.color = new Color(0f, 0f, 0f, 0f);
            // Las sondas NO se ocultan aquí -- ver doc de clase, sobreviven al modo.
        }

        private void ActualizarModeIcon()
        {
            if (_modeIconSr == null || _apprentice == null) return;
            // Un pelín por encima del punto que usa el icono del Cincel
            // (0.22f) por si algún día coinciden los dos modos en pantalla
            // (nunca a la vez ACTIVOS, pero un jugador puede alternar rápido
            // y ver el resto de un fundido) -- así nunca se pisan.
            Vector3 iconPos = _apprentice.CarryAnchor + new Vector3(0f, 0.34f, -0.03f);
            _modeIconSr.transform.position = iconPos;
            _modeIconSr.color = new Color(ColorInstrumento.r / 255f, ColorInstrumento.g / 255f, ColorInstrumento.b / 255f, ModeIconAlpha);
        }

        // ===================================================================
        // HUD: sondas (siempre) + readout del cursor (solo con el modo activo).
        // ===================================================================
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;
            if (Alkahest.Dev.DevPalette.IsOpen || UiStyles.EscribiendoTexto || JournalHud.Abierto) return;

            UiStyles.Preparar();

            // Las etiquetas de las sondas se dibujan SIEMPRE, con el modo
            // activo o no (ver doc de clase).
            for (int i = 0; i < MaxSondas; i++)
            {
                if (!_sondaActiva[i] || _sondaLabel[i] == null) continue;
                Vector3 world = _sim.CellToWorld(new Vector2Int(_sondaX[i], _sondaY[i]));
                Color color = _sim.Stepper == null ? UiStyles.TextoTenue : ColorPorC(CellGrid.RawToC(_sondaRaw[i]));
                UiStyles.PlacaMundo(world, _sondaLabel[i], color, UiStyles.S(20f));
            }

            if (!ModoActivo || !_hasCursor) return;
            DibujarReadoutCursor();
        }

        /// <summary>El chip que sigue al cursor con la lectura viva de la celda apuntada -- ver doc de clase. Dibujado a mano (no UiStyles.PlacaMundo/Globo, que anclan a un punto de MUNDO): esto ancla a la posición de PANTALLA del ratón, que es justo el punto del contrato ("junto al cursor").</summary>
        private void DibujarReadoutCursor()
        {
            var mouse = Mouse.current;
            if (mouse == null || UiStyles.ChipMini == null) return;

            bool invitado = _sim.Stepper == null;
            string texto;
            Color color;
            if (invitado)
            {
                texto = _notaInvitadoMostrada ? "—" : "— (solo el anfitrión mide, por ahora)";
                _notaInvitadoMostrada = true;
                color = UiStyles.TextoTenue;
            }
            else
            {
                byte raw = _sim.SampleTempRaw(_cursorCell.x, _cursorCell.y);
                texto = _gradoLabels[raw];
                color = ColorPorC(CellGrid.RawToC(raw));
            }

            Vector2 screenPos = mouse.position.ReadValue();
            float guiX = screenPos.x;
            float guiY = Screen.height - screenPos.y; // Mouse usa origen abajo-izquierda; IMGUI arriba-izquierda (mismo giro que UiStyles.EtiquetaMundo).

            float w = UiStyles.Ancho(UiStyles.ChipMini, texto) + UiStyles.S(14f);
            float h = UiStyles.ChipMini.lineHeight + UiStyles.S(8f);
            var r = new Rect(guiX + UiStyles.S(18f), guiY + UiStyles.S(12f), w, h);

            UiStyles.Panel(r, UiStyles.TintaFuerte, new Color(color.r, color.g, color.b, 0.6f));
            var previo = UiStyles.ChipMini.normal.textColor;
            UiStyles.ChipMini.normal.textColor = color;
            GUI.Label(r, texto, UiStyles.ChipMini);
            UiStyles.ChipMini.normal.textColor = previo;
        }
    }
}
