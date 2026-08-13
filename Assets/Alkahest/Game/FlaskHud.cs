using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// HUD del frasco. REDISEÑADO tras el playtest 3 ("la indicación del frasco
    /// me tapa mi principal lugar de juego, abajo a la izquierda").
    ///
    /// Decisiones de disposición:
    ///  · La zona baja de la pantalla es INTOCABLE: ahí están el suelo, las
    ///    cubas y los charcos de los grifos. Todo el HUD del frasco se muda a
    ///    una barra compacta ARRIBA-IZQUIERDA, sobre cielo vacío.
    ///  · Ya no es una GUILayout.Window arrastrable (se podía dejar encima del
    ///    juego sin querer): es un panel fijo, dibujado con rects absolutos, así
    ///    que ningún texto se puede "correr" ni recortar.
    ///  · El feedback ("frasco vacío", "demasiado lejos") ya no vive en la
    ///    esquina: aparece como globo JUNTO AL CURSOR, que es donde el jugador
    ///    está mirando cuando falla la acción.
    ///  · La retícula sustituye al glifo "◯": marca el punto exacto de
    ///    aspirado/vertido y cambia de color según la acción y el alcance
    ///    (rojo = fuera del alcance del aprendiz).
    ///
    /// (fix playtest 10, reporte 8) BLOQUEO DE MATERIAL: desde que Flask.cs
    /// fija un único material por pulsación de aspirado (ver
    /// Flask.BloquearMaterialBajoElCursor), la regla es invisible si nadie la
    /// enseña. <see cref="DibujarMaterialBloqueado"/> pone un chip discreto
    /// junto al cursor —color + nombre— SOLO mientras se está aspirando: nada
    /// permanente, mismo criterio que ya sigue el resto de este HUD (el
    /// jugador se quejó antes de elementos fijos en pantalla).
    ///
    /// (fix playtest 13) FIRMA VISUAL EN VEZ DE CUADRADITO PLANO: el reporte
    /// del jugador fue literal — "al llenar las botellas estos patrones no se
    /// notan ni se animan sus contenidos, lo que lo hace más dependiente del
    /// nombre" — y este panel es "el sitio de mayor impacto, porque es donde
    /// el jugador mira mientras trabaja". Tanto la fila de cada material del
    /// FRASCO (<see cref="DibujarPanel"/>) como el chip de material bloqueado
    /// junto al cursor (<see cref="DibujarMaterialBloqueado"/>) sustituyen su
    /// cuadradito de <c>UiStyles.Rellenar(..., def.baseColor)</c> por una
    /// miniatura generada por código con el mismo lenguaje visual que ya usa
    /// <c>JournalHud.CrearMiniatura</c> para el catálogo (color+patrón+borde),
    /// vía <see cref="FirmaVisualFabrica"/> (StorageRack.cs, compartida con
    /// las redomas — ver el comentario largo ahí sobre por qué esto duplica
    /// parte de JournalHud). Generada UNA VEZ POR MATERIAL, cacheada en
    /// <see cref="_firmaTexturas"/>, liberada en <see cref="OnDestroy"/>; SIN
    /// AGRANDAR NINGUNA fila (el rect del swatch es el MISMO que ya ocupaba
    /// el cuadradito plano, ver <see cref="SwatchLado"/>) — el playtest 10 ya
    /// se había quejado de apiñamiento en este panel, así que aquí no se
    /// añade superficie nueva, solo se mejora lo que ya había.
    ///
    /// Aquí SÍ vale bajar alfa para el borde Difuso (a diferencia de
    /// StorageRack, donde el contenido se pinta sobre el MUNDO): este swatch
    /// va sobre <c>UiStyles.Panel</c>, un panel IMGUI opaco, así que
    /// <see cref="FirmaVisualFabrica.GenerarPixeles"/> se llama con
    /// <c>sobreMundo: false</c> (regla 19 de CLAUDE.md).
    ///
    /// (fix playtest 16) LA LÍNEA DE AYUDA ENCOGE A MEDIDA QUE SE APRENDE:
    /// mismo criterio que ya usa el proyecto en todas partes (el prompt "E"
    /// de <see cref="MachineFocus.MostrarPromptE"/> desaparece tras dos usos,
    /// da igual en qué aparato) -- un contador de gestos reales de frasco
    /// (aspirar/verter/vaciar, CUALQUIER combinación de los tres, igual que
    /// MachineFocus no distingue de qué aparato vino el uso) apaga
    /// <see cref="TextoAyuda"/> a partir de <see cref="UsosParaAprender"/>.
    /// Ver <see cref="ActualizarUsosAyuda"/>. También se aprieta el relleno
    /// del panel ("doble espaciado innecesario", reporte del jugador): SIN
    /// volver al apiñamiento que ya se quejó en el playtest 10 -- el ancho de
    /// fila y el tamaño del swatch de firma NO se tocan (están validados,
    /// "el punto de luz del color de material está increíble"), solo se
    /// aprietan los márgenes/huecos entre secciones.
    /// </summary>
    public sealed class FlaskHud : MonoBehaviour
    {
        private const int MaxSwatches = 4;
        private const string TextoAyuda = "clic izq. aspirar · clic der. verter · Q vaciar";

        // -----------------------------------------------------------------
        // Aprendizaje de la ayuda (fix playtest 16, ver docblock de la
        // clase). MachineFocus usa 2 usos para "E"; aquí el gesto es más
        // sutil (tres acciones distintas bajo una sola línea de texto, no
        // una tecla), así que se dan 3 antes de callarlo -- mismo espíritu,
        // umbral algo más generoso porque hay más que aprender de un vistazo.
        // -----------------------------------------------------------------
        private const int UsosParaAprender = 3;
        private int _usosAyuda;
        private bool _aspirandoPrevio; // Flask.EstaAspirando es un NIVEL (true mientras se mantiene el botón), no un flanco: hace falta guardar el frame anterior para contar UNA vez por pulsación, no una vez por frame mantenido.

        private bool MostrarAyuda => _usosAyuda < UsosParaAprender;

        private AlkahestSim _sim;
        private Flask _flask;
        private SubstanceKnowledge _knowledge;

        // Buffers reutilizados cada frame para no asignar memoria al calcular el top-4.
        private readonly byte[] _topIds = new byte[MaxSwatches];
        private readonly int[] _topCounts = new int[MaxSwatches];

        // -----------------------------------------------------------------
        // FIRMA VISUAL DE LOS SWATCHES (fix playtest 13, ver docblock de la
        // clase). Un único tamaño de lienzo para TODOS los swatches de este
        // HUD (filas del panel + chip de bloqueo): más sencillo de cachear
        // que un tamaño por sitio, y FilterMode.Point + GUI.DrawTexture ya
        // estira sin problema a lo que pida cada rect real.
        // -----------------------------------------------------------------
        private const int SwatchLado = 18;
        private readonly Texture2D[][] _firmaTexturas = new Texture2D[MaterialId.Count][];
        private bool[] _esBordeSwatch;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Flask flask, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _flask = flask;
            _knowledge = knowledge;
        }

        /// <summary>
        /// (fix playtest 16) Cuenta gestos REALES de frasco para apagar
        /// <see cref="TextoAyuda"/> -- ver <see cref="UsosParaAprender"/>.
        /// Vive en Update() (una vez por frame de verdad), NUNCA en OnGUI:
        /// OnGUI se ejecuta más de una vez por frame (Layout + Repaint como
        /// mínimo) y <c>wasPressedThisFrame</c> sigue en true durante las dos
        /// pasadas, así que contar ahí duplicaría cada pulsación.
        /// Mismas guardas que <c>Flask.Update</c> para no contar entrada que
        /// no es en realidad una acción de frasco (bautizando un nombre, con
        /// el diario abierto o bajo un overlay de jornada).
        /// </summary>
        private void Update()
        {
            ActualizarUsosAyuda();
        }

        private void ActualizarUsosAyuda()
        {
            if (_usosAyuda >= UsosParaAprender || _flask == null) return;
            if (DayCycle.InputLocked || UiStyles.EscribiendoTexto || JournalHud.Abierto) return;
            if (Alkahest.Dev.DevPalette.IsOpen) return; // con la paleta dev abierta el frasco no actúa (ver Flask.Update): tampoco cuenta como "uso".

            bool aspirando = _flask.EstaAspirando;
            bool aspirarUsado = aspirando && !_aspirandoPrevio; // flanco de subida: una vez por pulsación, no por frame mantenido.
            _aspirandoPrevio = aspirando;

            var mouse = Mouse.current;
            var kb = Keyboard.current;
            bool verterUsado = mouse != null && mouse.rightButton.wasPressedThisFrame && _flask.Total > 0;
            bool vaciarUsado = kb != null && kb.qKey.wasPressedThisFrame;

            if (aspirarUsado || verterUsado || vaciarUsado) _usosAyuda++;
        }

        /// <summary>
        /// (fix playtest 13) Misma disciplina de memoria que
        /// JournalHud.OnDestroy/StorageRack.OnDestroy: un Texture2D creado por
        /// código no se libera solo con destruir este GameObject, y este
        /// componente se recrea entero en cada universo nuevo (recarga de
        /// escena de DayCycle.RestartRun) -- sin este bucle se acumularían
        /// huérfanas, hasta MaterialId.Count * AnimFrames texturas por partida.
        /// </summary>
        private void OnDestroy()
        {
            for (int m = 0; m < _firmaTexturas.Length; m++)
            {
                var texturas = _firmaTexturas[m];
                if (texturas == null) continue;
                for (int f = 0; f < texturas.Length; f++)
                {
                    if (texturas[f] != null) Destroy(texturas[f]);
                }
            }
        }

        /// <summary>
        /// Qué téxeles del lienzo SwatchLado x SwatchLado cuentan como "borde"
        /// para BordeMorfologico (ver FirmaVisualFabrica.GenerarPixeles):
        /// aquí el swatch es un cuadrado macizo, así que "borde" es
        /// simplemente "cerca del canto" -- igual criterio que
        /// JournalHud.ApplyBordeMini. Calculado UNA vez (no depende del
        /// material, solo del tamaño del lienzo) y reutilizado por todos.
        /// </summary>
        private void PrepararBordeSwatch()
        {
            if (_esBordeSwatch != null) return;

            const int bandaBorde = 2; // ~11% del lado, mismo orden que el 10% de JournalHud.
            _esBordeSwatch = new bool[SwatchLado * SwatchLado];
            for (int y = 0; y < SwatchLado; y++)
            {
                for (int x = 0; x < SwatchLado; x++)
                {
                    int distBorde = Mathf.Min(Mathf.Min(x, SwatchLado - 1 - x), Mathf.Min(y, SwatchLado - 1 - y));
                    _esBordeSwatch[y * SwatchLado + x] = distBorde < bandaBorde;
                }
            }
        }

        /// <summary>
        /// Fotogramas de firma visual cacheados para `matId`: generados LA
        /// PRIMERA VEZ que hacen falta, nunca más (ver docblock de la clase).
        /// Longitud 1 si ritmoAnim==0 (swatch quieto de verdad).
        /// </summary>
        private Texture2D[] ObtenerFirmaTexturas(byte matId)
        {
            if (matId >= _firmaTexturas.Length || _sim == null || _sim.Universe == null) return null;

            var existente = _firmaTexturas[matId];
            if (existente != null) return existente;

            PrepararBordeSwatch();
            var def = _sim.Universe.Get(matId);
            int frames = def.ritmoAnim > 0 ? FirmaVisualFabrica.AnimFrames : 1;
            var texturas = new Texture2D[frames];

            for (int f = 0; f < frames; f++)
            {
                var px = FirmaVisualFabrica.GenerarPixeles(SwatchLado, SwatchLado, def, f,
                    null, _esBordeSwatch, sobreMundo: false);

                var tex = new Texture2D(SwatchLado, SwatchLado, TextureFormat.RGBA32, false, false)
                {
                    filterMode = FilterMode.Point, // mismo criterio que toda textura generada del proyecto.
                    wrapMode = TextureWrapMode.Clamp,
                    name = "FirmaFlaskHud_" + def.devName + "_" + f,
                };
                tex.SetPixels32(px);
                tex.Apply(false, true); // makeNoLongerReadable=true: solo se pinta con GUI.DrawTexture.
                texturas[f] = tex;
            }

            _firmaTexturas[matId] = texturas;
            return texturas;
        }

        /// <summary>
        /// Textura del fotograma que toca mostrar AHORA MISMO para `matId`
        /// (ver FirmaVisualFabrica.AnimFps). Barato: solo un módulo sobre un
        /// array ya cacheado, nunca reconstruye nada -- llamado desde OnGUI,
        /// que ya se redibuja cada frame por sí solo (IMGUI inmediato), así
        /// que no hace falta trackear "el último fotograma mostrado" aquí
        /// como sí hace StorageRack con sus SpriteRenderer persistentes.
        /// </summary>
        private Texture2D ObtenerFirmaFrameActual(byte matId)
        {
            var texturas = ObtenerFirmaTexturas(matId);
            if (texturas == null || texturas.Length == 0) return null;
            int idx = Mathf.FloorToInt(Time.time * FirmaVisualFabrica.AnimFps) % texturas.Length;
            return texturas[idx];
        }

        private void OnGUI()
        {
            if (_sim == null || _flask == null || _sim.Universe == null) return;
            if (DayCycle.InputLocked) return; // bajo los overlays de jornada el HUD estorba.

            UiStyles.Preparar();
            ComputeTopContents();
            DibujarPanel();
            DibujarReticulaYFeedback();
            DibujarMaterialBloqueado();
        }

        // -----------------------------------------------------------------
        // Panel compacto arriba-izquierda.
        // -----------------------------------------------------------------
        private void DibujarPanel()
        {
            // (fix playtest 16) Márgenes/huecos apretados un escalón --
            // "doble espaciado innecesario" del reporte. altoFila (y por
            // tanto el swatch de firma, `lado` más abajo) se deja EXACTAMENTE
            // igual: eso está validado y no se toca. El punto medio buscado:
            // menos aire ENTRE secciones, no menos aire DENTRO de cada fila
            // (que es donde vivía la queja de apiñamiento del playtest 10).
            float margen = UiStyles.S(10f);
            float pad = UiStyles.S(6f);           // antes 8.
            float ancho = UiStyles.S(300f);
            float altoLinea = UiStyles.S(17f);    // antes 19.
            float altoBarra = UiStyles.S(11f);    // antes 13.
            float altoFila = UiStyles.S(16f);     // SIN CAMBIOS: aquí vive el swatch validado.
            float gapChico = UiStyles.S(3f);      // antes 4 (bajo el título).
            float gapSeccion = UiStyles.S(4f);    // antes 6 (antes de las filas y antes de la ayuda).

            bool mostrarAyuda = MostrarAyuda;
            // La línea de ayuda se MIDE (podría necesitar dos líneas a resoluciones
            // raras): así el panel siempre la contiene entera. Si ya se aprendió
            // (fix playtest 16) ni se mide ni se reserva alto para ella.
            float altoAyuda = mostrarAyuda ? UiStyles.Alto(UiStyles.CuerpoTenue, TextoAyuda, ancho - pad * 2f) : 0f;

            int filas = 0;
            for (int i = 0; i < MaxSwatches; i++) if (_topCounts[i] > 0) filas++;

            float alto = pad + altoLinea + gapChico + altoBarra
                       + (filas > 0 ? gapSeccion + filas * altoFila : 0f)
                       + (mostrarAyuda ? gapSeccion + altoAyuda : 0f)
                       + pad;

            var panel = new Rect(margen, margen, ancho, alto);
            UiStyles.Panel(panel);

            float x = panel.x + pad;
            float y = panel.y + pad;
            float anchoInterior = ancho - pad * 2f;

            int total = _flask.Total;
            GUI.Label(new Rect(x, y, anchoInterior * 0.5f, altoLinea), "FRASCO", UiStyles.Titulo);
            GUI.Label(new Rect(x + anchoInterior * 0.4f, y, anchoInterior * 0.6f, altoLinea),
                total + " / " + Flask.Capacity, UiStyles.Numero);
            y += altoLinea + gapChico;

            // La barra se tiñe con la MEZCLA de lo que llevas: de un vistazo sabes
            // si cargas agua, aceite o el mejunje verde de turno.
            UiStyles.Barra(new Rect(x, y, anchoInterior, altoBarra),
                (float)total / Flask.Capacity, ColorMezcla(total));
            y += altoBarra;

            if (filas > 0)
            {
                y += gapSeccion;
                float lado = altoFila - UiStyles.S(4f);
                for (int i = 0; i < MaxSwatches; i++)
                {
                    if (_topCounts[i] <= 0) continue;
                    var def = _sim.Universe.Get(_topIds[i]);

                    // (fix playtest 13) Miniatura de firma real en vez de un
                    // cuadradito de color plano -- ver docblock de la clase.
                    var swatchRect = new Rect(x, y + UiStyles.S(2f), lado, lado);
                    var firma = ObtenerFirmaFrameActual(_topIds[i]);
                    if (firma != null) GUI.DrawTexture(swatchRect, firma);
                    else UiStyles.Rellenar(swatchRect, def.baseColor); // defensivo, ver ObtenerFirmaTexturas.

                    float xTexto = x + lado + UiStyles.S(6f);
                    GUI.Label(new Rect(xTexto, y, anchoInterior - lado - UiStyles.S(60f), altoFila),
                        NombreDe(_topIds[i]), UiStyles.CuerpoLinea);
                    GUI.Label(new Rect(x, y, anchoInterior, altoFila),
                        _topCounts[i].ToString(), UiStyles.CuerpoDer);
                    y += altoFila;
                }
            }

            // (fix playtest 16) A partir de UsosParaAprender gestos reales de
            // frasco, la línea desaparece del todo -- ni se dibuja ni deja
            // hueco (ver el cálculo de `alto` más arriba): el rectángulo
            // encoge de verdad, no solo se queda con una línea en blanco.
            if (mostrarAyuda)
            {
                y += gapSeccion;
                GUI.Label(new Rect(x, y, anchoInterior, altoAyuda), TextoAyuda, UiStyles.CuerpoTenue);
            }
        }

        /// <summary>Nombre a mostrar: el que le puso el jugador, si no el nombre común de taller, si no "???".</summary>
        private string NombreDe(byte matId)
        {
            if (_knowledge != null) return _knowledge.NombreParaHud(matId);
            return SubstanceKnowledge.NombreComun(matId) ?? "???";
        }

        /// <summary>Media ponderada de los colores del top-4 (aproximación barata del contenido real).</summary>
        private Color ColorMezcla(int total)
        {
            if (total <= 0) return UiStyles.OroTenue;

            float r = 0f, g = 0f, b = 0f, peso = 0f;
            for (int i = 0; i < MaxSwatches; i++)
            {
                if (_topCounts[i] <= 0) continue;
                Color c = _sim.Universe.Get(_topIds[i]).baseColor;
                float w = _topCounts[i];
                r += c.r * w; g += c.g * w; b += c.b * w; peso += w;
            }
            if (peso <= 0f) return UiStyles.OroTenue;
            return new Color(r / peso, g / peso, b / peso, 1f);
        }

        /// <summary>Selección simple de los 4 materiales con mayor conteo (N=256, se ejecuta solo mientras el HUD está visible).</summary>
        private void ComputeTopContents()
        {
            for (int i = 0; i < MaxSwatches; i++) { _topIds[i] = 0; _topCounts[i] = 0; }

            for (int mat = 1; mat < 256; mat++)
            {
                int c = _flask.GetCount((byte)mat);
                if (c <= 0) continue;

                int minIdx = 0;
                for (int i = 1; i < MaxSwatches; i++)
                {
                    if (_topCounts[i] < _topCounts[minIdx]) minIdx = i;
                }
                if (c > _topCounts[minIdx])
                {
                    _topCounts[minIdx] = c;
                    _topIds[minIdx] = (byte)mat;
                }
            }
        }

        // -----------------------------------------------------------------
        // Retícula + globo de feedback junto al cursor.
        // -----------------------------------------------------------------
        private void DibujarReticulaYFeedback()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            // Mouse.current.position usa origen abajo-izquierda; IMGUI usa origen arriba-izquierda.
            var gui = new Vector2(screenPos.x, Screen.height - screenPos.y);

            bool enAlcance = CursorEnAlcance(screenPos);
            Color color;
            if (!enAlcance) color = UiStyles.Peligro;
            else if (mouse.leftButton.isPressed) color = UiStyles.Frio;
            else if (mouse.rightButton.isPressed) color = UiStyles.Oro;
            else color = new Color(0.92f, 0.90f, 0.86f, 0.85f);

            float grosor = Mathf.Max(1f, Mathf.Round(UiStyles.Escala));
            float hueco = UiStyles.S(5f);
            float largo = UiStyles.S(9f);

            UiStyles.Rellenar(new Rect(gui.x - hueco - largo, gui.y - grosor * 0.5f, largo, grosor), color);
            UiStyles.Rellenar(new Rect(gui.x + hueco, gui.y - grosor * 0.5f, largo, grosor), color);
            UiStyles.Rellenar(new Rect(gui.x - grosor * 0.5f, gui.y - hueco - largo, grosor, largo), color);
            UiStyles.Rellenar(new Rect(gui.x - grosor * 0.5f, gui.y + hueco, grosor, largo), color);
            UiStyles.Rellenar(new Rect(gui.x - grosor, gui.y - grosor, grosor * 2f, grosor * 2f), color);

            if (Time.time < _flask.FeedbackUntil && !string.IsNullOrEmpty(_flask.Feedback))
            {
                UiStyles.Globo(new Vector2(gui.x, gui.y - UiStyles.S(30f)), _flask.Feedback, UiStyles.Aviso);
            }
        }

        /// <summary>¿Está el cursor dentro del alcance del aprendiz? Mismo criterio que Flask (radio ReachWorld en unidades de mundo).</summary>
        private bool CursorEnAlcance(Vector2 screenPos)
        {
            var cam = Camera.main;
            if (cam == null || _flask == null) return true;

            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var plano = new Plane(Vector3.forward, Vector3.zero);
            if (!plano.Raycast(ray, out float enter)) return true;

            Vector3 mundo = ray.GetPoint(enter);
            Vector3 delta = mundo - _flask.transform.position;
            delta.z = 0f;
            return delta.sqrMagnitude <= Flask.ReachWorld * Flask.ReachWorld;
        }

        // -----------------------------------------------------------------
        // Chip de "qué se está bloqueando" (fix playtest 10, reporte 8).
        // -----------------------------------------------------------------

        /// <summary>
        /// Cuadradito de color + nombre del material fijado para la pulsación de
        /// aspirado actual (ver doc de clase). Vive junto al cursor, por debajo
        /// del globo de feedback para no pisarlo, y SOLO mientras
        /// <see cref="Flask.EstaAspirando"/> es true: en cuanto se suelta el
        /// botón, desaparece — es una explicación de la regla en el momento en
        /// que aplica, no un HUD permanente más.
        /// </summary>
        private void DibujarMaterialBloqueado()
        {
            if (_flask == null || !_flask.EstaAspirando) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            string texto;
            Color32 color;
            byte matBloqueado = MaterialId.Empty;
            bool tieneMatConcreto;
            // ModoIndiscriminado (Shift) se comprueba PRIMERO: si a mitad de una
            // pulsación con material ya bloqueado el jugador mantiene Shift,
            // Flask.TickSuck ignora ese bloqueo (aspira todo) -- el chip debe
            // reflejar la regla que se está aplicando de verdad este frame, no
            // el material que se dejó de discriminar.
            if (_flask.ModoIndiscriminado)
            {
                color = new Color32(200, 190, 170, 255);
                texto = "todo (Mayús.)";
                tieneMatConcreto = false; // "todo" no es UN material: no hay firma que dibujar, se queda en chip plano.
            }
            else if (_flask.TieneMaterialBloqueado)
            {
                matBloqueado = _flask.MaterialBloqueado;
                color = _sim.Universe.Get(matBloqueado).baseColor;
                texto = NombreDe(matBloqueado);
                tieneMatConcreto = true;
            }
            else
            {
                return; // esta pulsación no bloqueó nada (nada aspirable cerca): no hay nada que explicar todavía.
            }

            Vector2 screenPos = mouse.position.ReadValue();
            var gui = new Vector2(screenPos.x, Screen.height - screenPos.y);

            float lado = UiStyles.S(11f);
            float padX = UiStyles.S(6f);
            float anchoTexto = UiStyles.Ancho(UiStyles.CuerpoLinea, texto);
            float ancho = padX + lado + padX + anchoTexto + padX;
            float alto = Mathf.Max(lado, UiStyles.CuerpoLinea.lineHeight) + UiStyles.S(6f);

            // Debajo de la retícula/globo de feedback (que vive por ENCIMA del
            // cursor): así los dos avisos conviven sin superponerse.
            float x = Mathf.Clamp(gui.x - ancho * 0.5f, UiStyles.S(4f), Mathf.Max(UiStyles.S(4f), Screen.width - ancho - UiStyles.S(4f)));
            float y = gui.y + UiStyles.S(16f);
            var r = new Rect(x, y, ancho, alto);

            Color colorUi = new Color(color.r / 255f, color.g / 255f, color.b / 255f, 1f);
            UiStyles.Panel(r, UiStyles.TintaFuerte, new Color(colorUi.r, colorUi.g, colorUi.b, 0.55f));

            var swatchRect = new Rect(r.x + padX, r.y + (r.height - lado) * 0.5f, lado, lado);
            // (fix playtest 13) Miniatura de firma real cuando hay UN material
            // concreto bloqueado -- "todo (Mayús.)" no es un material, se queda
            // con el cuadradito plano de siempre (no hay firma que mostrar).
            Texture2D firma = tieneMatConcreto ? ObtenerFirmaFrameActual(matBloqueado) : null;
            if (firma != null) GUI.DrawTexture(swatchRect, firma);
            else UiStyles.Rellenar(swatchRect, colorUi);

            GUI.Label(new Rect(r.x + padX + lado + padX, r.y + (r.height - UiStyles.CuerpoLinea.lineHeight) * 0.5f, anchoTexto + UiStyles.S(4f), UiStyles.CuerpoLinea.lineHeight), texto, UiStyles.CuerpoLinea);
        }
    }
}
