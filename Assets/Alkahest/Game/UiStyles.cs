using UnityEngine;
using UnityEngine.InputSystem;

namespace Alkahest.Game
{
    /// <summary>
    /// [TenThousandYears · pase visual M5] Mini-sistema de estilo para TODA la UI
    /// IMGUI del juego: una única paleta, una tipografía que escala con la
    /// resolución, y primitivas de dibujo (panel, barra, chip, etiqueta anclada
    /// al mundo) que comparten todos los HUD.
    ///
    /// POR QUÉ EXISTE: antes cada HUD creaba sus GUIStyle DENTRO de OnGUI (una
    /// asignación por frame) y se dibujaba con el skin por defecto de Unity, con
    /// un tamaño fijo en píxeles que a 1080p/1440p quedaba diminuto — de ahí las
    /// quejas del playtest ("tipografía mal", "los textos se cortan"). Aquí los
    /// estilos se construyen UNA sola vez y solo se reconstruyen si cambia la
    /// altura de la pantalla (p.ej. al entrar en pantalla completa).
    ///
    /// REGLAS DE USO:
    ///  · Llamar a <see cref="Preparar"/> al principio de cada OnGUI, antes de
    ///    usar cualquier estilo o primitiva.
    ///  · Nunca llamar desde Update: GUI.skin solo es válido dentro de OnGUI
    ///    (Preparar() se protege igualmente comprobando Event.current).
    ///  · Todas las medidas en píxeles se pasan por <see cref="S"/> para que el
    ///    HUD ocupe la misma fracción de pantalla en cualquier resolución.
    /// </summary>
    public static class UiStyles
    {
        // -----------------------------------------------------------------
        // Paleta del juego. Deriva del fondo del taller (ciruela oscuro) y del
        // dorado de la Tolva: tinta casi negra para los paneles, oro para lo
        // accionable/importante, y tres acentos semánticos (aviso, éxito, frío).
        // -----------------------------------------------------------------
        public static readonly Color Tinta = new Color(0.055f, 0.045f, 0.075f, 0.88f);
        public static readonly Color TintaFuerte = new Color(0.035f, 0.028f, 0.048f, 0.96f);
        public static readonly Color Borde = new Color(0.58f, 0.47f, 0.30f, 0.65f);
        public static readonly Color Oro = new Color(1.00f, 0.82f, 0.35f, 1f);
        public static readonly Color OroTenue = new Color(0.70f, 0.57f, 0.28f, 1f);
        public static readonly Color Texto = new Color(0.94f, 0.91f, 0.85f, 1f);
        public static readonly Color TextoTenue = new Color(0.70f, 0.66f, 0.62f, 1f);
        public static readonly Color Aviso = new Color(1.00f, 0.62f, 0.32f, 1f);
        public static readonly Color Peligro = new Color(1.00f, 0.40f, 0.34f, 1f);
        // (RONDA 64, directiva Opus 4.5) De verde neón (0.52,0.92,0.60) a verde
        // MUSGO: era el único saturado en un taller entero de sepia y ámbar --
        // "el verde chillón" delataba el borrador. Sigue leyendo "éxito".
        public static readonly Color Exito = new Color(0.55f, 0.70f, 0.42f, 1f);
        public static readonly Color Frio = new Color(0.55f, 0.85f, 1.00f, 1f);
        public static readonly Color Hueco = new Color(0f, 0f, 0f, 0.55f);

        // -----------------------------------------------------------------
        // (playtest 31, LA IDENTIDAD) LOS COLORES DEL TALLER. Cesar: "el menú
        // de bautizar tiene que dejar de parecer un menú de Windows XP".
        // Un menú de sistema se reconoce por tres cosas: fondo gris neutro,
        // borde de widget del SO y tipografía de sistema. Las tres se
        // sustituyen aquí de una vez: CARBONCILLO como fondo de todo lo que
        // el jugador escribe o pulsa, LATÓN como el metal de los filos (el
        // mismo material del que están hechas las guías del crisol y el labio
        // de las bocas -- no un azul de widget), y las dos fuentes reales
        // (ver CargarFuentes).
        // -----------------------------------------------------------------
        /// <summary>#1a1a1f — el carboncillo del taller: fondo de campos, botones y paneles ceremoniales.</summary>
        public static readonly Color Carboncillo = new Color(0.102f, 0.102f, 0.122f, 1f);
        /// <summary>#a87e3a — latón: el metal de los filos y los marcos. Más apagado que <see cref="Oro"/>, que se reserva para el TEXTO importante.</summary>
        public static readonly Color Laton = new Color(0.659f, 0.494f, 0.227f, 1f);
        /// <summary>Latón viejo, para las líneas interiores y los remaches (un filo nunca es de un solo tono).</summary>
        public static readonly Color LatonOscuro = new Color(0.36f, 0.27f, 0.13f, 1f);
        /// <summary>Pergamino-oscuro: el "papel" de los paneles de rito y del diario. NO es un papel claro: es vitela ahumada a la luz de un fuego.</summary>
        public static readonly Color Pergamino = new Color(0.145f, 0.128f, 0.128f, 1f);

        // -----------------------------------------------------------------
        // (playtest 31) TIPOGRAFÍA = ALMA. Las dos únicas fuentes del
        // proyecto viven en Assets/Alkahest/Resources/Fuentes:
        //   · Cinzel   — lapidaria romana: TÍTULOS y nada más ("CHAOS
        //                ALCHEMY", "BAUTIZO", secciones del diario,
        //                desenlaces). Respira: pide espaciado entre letras
        //                (ver Espaciar) y NUNCA se usa para párrafos.
        //   · Alegreya — humanista: TODO el cuerpo. Tiene la x pequeña, así
        //                que los cuerpos suben un punto respecto a la fuente
        //                de sistema que había antes.
        // REGLA DURA: si Resources.Load devuelve null (build sin las fuentes,
        // o carpeta Resources no incluida), TODO sigue funcionando con la
        // fuente por defecto -- por eso jamás se comprueba != null antes de
        // dibujar: `GUIStyle.font = null` significa "usa la del skin", que es
        // exactamente el comportamiento anterior a esta ronda.
        // -----------------------------------------------------------------

        /// <summary>Cinzel (lapidaria) — solo títulos. Null si no se pudo cargar: el juego sigue, con la fuente por defecto.</summary>
        public static Font FuenteTitulos { get; private set; }
        /// <summary>Alegreya (humanista) — todo el cuerpo. Null si no se pudo cargar.</summary>
        public static Font FuenteCuerpo { get; private set; }
        private static bool _fuentesPedidas;

        private static void CargarFuentes()
        {
            if (_fuentesPedidas) return;
            _fuentesPedidas = true; // se intenta UNA vez por sesión, tanto si sale bien como si no (Resources.Load no es gratis y un fallo es permanente).
            FuenteTitulos = Resources.Load<Font>("Fuentes/Cinzel");
            FuenteCuerpo = Resources.Load<Font>("Fuentes/Alegreya");
        }

        /// <summary>Factor de escala del HUD respecto a 720p (1.0 a 720p, 1.5 a 1080p, 2.0 a 1440p).</summary>
        public static float Escala { get; private set; } = 1f;

        public static GUIStyle Titulo { get; private set; }
        public static GUIStyle Cuerpo { get; private set; }
        public static GUIStyle CuerpoDer { get; private set; }
        /// <summary>Cuerpo de UNA sola línea (sin word-wrap): para filas de alto fijo donde un salto rompería la maqueta.</summary>
        public static GUIStyle CuerpoLinea { get; private set; }
        public static GUIStyle CuerpoCentrado { get; private set; }
        public static GUIStyle CuerpoTenue { get; private set; }
        public static GUIStyle TenueCentrado { get; private set; }
        public static GUIStyle Numero { get; private set; }
        public static GUIStyle Chip { get; private set; }
        /// <summary>Chip DIMINUTO para los rótulos fijos pegados a las máquinas (ver <see cref="PlacaMundo"/>). Deliberadamente pequeño: es un grabado en el aparato, no un aviso.</summary>
        public static GUIStyle ChipMini { get; private set; }
        public static GUIStyle Alerta { get; private set; }
        public static GUIStyle Reloj { get; private set; }
        public static GUIStyle TituloGrande { get; private set; }
        public static GUIStyle Subtitulo { get; private set; }
        public static GUIStyle Boton { get; private set; }
        public static GUIStyle Campo { get; private set; }

        /// <summary>(ENCARGO M, AJUSTES) Riel horizontal del slider de volumen -- ver <see cref="SliderThumb"/> para el porqué falta este par en UiStyles hasta ahora (nadie había pedido un control continuo antes de esta ronda).</summary>
        public static GUIStyle Slider { get; private set; }
        /// <summary>(ENCARGO M) "El thumb de latón" pedido por el encargo: un chip macizo del mismo metal que <see cref="Laton"/>/<see cref="LatonOscuro"/> que ya visten botones y campos -- nunca el grabber gris por defecto de IMGUI, que rompería la coherencia del taller igual que el skin de sistema que <see cref="VestirSkin"/> ya corrigió para botones/campos/ventanas.</summary>
        public static GUIStyle SliderThumb { get; private set; }

        /// <summary>(playtest 31) TÍTULO DE RITO: Cinzel, oro, centrado, grande. Para "BAUTIZO" y los encabezados ceremoniales que no son el título del juego.</summary>
        public static GUIStyle TituloRito { get; private set; }
        /// <summary>(playtest 31) Línea ceremonial: cursiva tenue centrada, el susurro bajo un título ("El nombre que le des lo verá todo el taller").</summary>
        public static GUIStyle Ceremonial { get; private set; }
        /// <summary>(playtest 31) Nombre grande de una sustancia dentro de un rito (el que el jugador acaba de escribir, o "sin nombre").</summary>
        public static GUIStyle NombreGrande { get; private set; }

        private static int _alturaConstruida = -1;
        private static readonly GUIContent _medida = new GUIContent();

        /// <summary>
        /// Construye (o reconstruye, si cambió la resolución) los estilos
        /// cacheados. Barata y idempotente: llamarla al principio de cada OnGUI.
        /// </summary>
        public static void Preparar()
        {
            if (Event.current == null) return; // fuera de OnGUI GUI.skin no es válido.
            if (_alturaConstruida == Screen.height && Cuerpo != null) return;

            _alturaConstruida = Screen.height;
            Escala = Mathf.Clamp(Screen.height / 720f, 1f, 2.4f);

            CargarFuentes();
            VestirSkin(); // (playtest 31) ANTES de derivar nada de GUI.skin.label: los estilos copian el skin tal como esté en ese instante.

            var raiz = GUI.skin.label;

            // (playtest 31) Los CUERPOS suben un punto: Alegreya tiene la x
            // más pequeña que la fuente de sistema que había antes, así que a
            // igualdad de puntos se lee más chica. Los títulos NO suben --
            // Cinzel es una capital lapidaria, ya ocupa todo el cuerpo.
            Titulo = Etiqueta(raiz, 16, FontStyle.Bold, TextAnchor.UpperLeft, Oro, false, FuenteTitulos);
            Cuerpo = Etiqueta(raiz, 14, FontStyle.Normal, TextAnchor.UpperLeft, Texto, true);
            CuerpoDer = Etiqueta(raiz, 14, FontStyle.Normal, TextAnchor.UpperRight, Texto, false);
            CuerpoLinea = Etiqueta(raiz, 14, FontStyle.Normal, TextAnchor.UpperLeft, Texto, false);
            CuerpoCentrado = Etiqueta(raiz, 15, FontStyle.Normal, TextAnchor.UpperCenter, Texto, true);
            CuerpoTenue = Etiqueta(raiz, 13, FontStyle.Normal, TextAnchor.UpperLeft, TextoTenue, true);
            TenueCentrado = Etiqueta(raiz, 12, FontStyle.Normal, TextAnchor.UpperCenter, TextoTenue, false);
            Numero = Etiqueta(raiz, 16, FontStyle.Bold, TextAnchor.UpperRight, Oro, false);
            Chip = Etiqueta(raiz, 13, FontStyle.Bold, TextAnchor.MiddleCenter, Texto, false);
            ChipMini = Etiqueta(raiz, 11, FontStyle.Bold, TextAnchor.MiddleCenter, Texto, false);
            Alerta = Etiqueta(raiz, 14, FontStyle.Bold, TextAnchor.UpperCenter, Aviso, true);
            Reloj = Etiqueta(raiz, 21, FontStyle.Bold, TextAnchor.MiddleCenter, Texto, false, FuenteTitulos);
            TituloGrande = Etiqueta(raiz, 30, FontStyle.Normal, TextAnchor.MiddleCenter, Oro, false, FuenteTitulos);
            Subtitulo = Etiqueta(raiz, 15, FontStyle.Italic, TextAnchor.MiddleCenter, TextoTenue, true);

            TituloRito = Etiqueta(raiz, 22, FontStyle.Normal, TextAnchor.MiddleCenter, Oro, false, FuenteTitulos);
            Ceremonial = Etiqueta(raiz, 13, FontStyle.Italic, TextAnchor.UpperCenter, OroTenue, true);
            NombreGrande = Etiqueta(raiz, 19, FontStyle.Normal, TextAnchor.MiddleLeft, Texto, false, FuenteTitulos);

            Boton = new GUIStyle(GUI.skin.button) { fontSize = F(14), fontStyle = FontStyle.Bold };
            Boton.normal.textColor = Texto;
            Boton.hover.textColor = Oro;
            Boton.active.textColor = Oro;

            Campo = new GUIStyle(GUI.skin.textField) { fontSize = F(15) };
            Campo.normal.textColor = Texto;
            Campo.focused.textColor = Texto;

            // (ENCARGO M, AJUSTES) MISMO PATRÓN que Boton/Campo dos líneas
            // arriba: se copian del skin (ya vestido por VestirSkin, que
            // corrió justo antes) y se reconstruyen solo cuando cambia la
            // resolución -- las texturas en sí (_texCampo para el riel,
            // _texSliderThumb para el grabber) se construyen UNA vez en
            // VestirSkin, no aquí. Riel: reutiliza _texCampo (carboncillo +
            // filo de latón, la misma "ranura" que ya usan los campos de
            // texto) en vez de inventar una tercera textura solo para esto.
            Slider = new GUIStyle(GUI.skin.horizontalSlider);
            Slider.fixedHeight = S(8f);
            Slider.border = new RectOffset(MarcoBorde, MarcoBorde, MarcoBorde, MarcoBorde);
            Slider.normal.background = _texCampo;
            Slider.hover.background = _texCampo;
            Slider.active.background = _texCampo;
            Slider.focused.background = _texCampo;

            SliderThumb = new GUIStyle(GUI.skin.horizontalSliderThumb);
            SliderThumb.fixedWidth = S(16f);
            SliderThumb.fixedHeight = S(20f);
            SliderThumb.border = new RectOffset(MarcoBorde, MarcoBorde, MarcoBorde, MarcoBorde);
            SliderThumb.normal.background = _texSliderThumb;
            SliderThumb.hover.background = _texSliderThumb;
            SliderThumb.active.background = _texSliderThumb;
            SliderThumb.focused.background = _texSliderThumb;
        }

        private static GUIStyle Etiqueta(GUIStyle raiz, int tam, FontStyle fuente, TextAnchor anclaje, Color color, bool ajustar, Font tipografia = null)
        {
            var s = new GUIStyle(raiz)
            {
                fontSize = F(tam),
                fontStyle = fuente,
                alignment = anclaje,
                wordWrap = ajustar,
                richText = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            // null = "hereda la del skin" (Alegreya, ver VestirSkin) -- es el
            // caso de TODOS los cuerpos; solo los títulos piden Cinzel.
            if (tipografia != null) s.font = tipografia;
            s.normal.textColor = color;
            return s;
        }

        // -----------------------------------------------------------------
        // (playtest 31) EL SKIN VESTIDO: por qué esto arregla TODA la UI de
        // una vez. Un GUIStyle con `font == null` resuelve su tipografía
        // contra `GUI.skin.font` EN EL MOMENTO DE DIBUJAR -- así que basta
        // con poner Alegreya ahí para que hereden JournalHud (que construye
        // sus estilos propios copiando GUI.skin.label), OrdersHud, HintSystem,
        // FlaskHud, DevPalette y cualquier HUD futuro, sin tocar sus archivos.
        // El mismo argumento vale para los FONDOS: reestilando
        // GUI.skin.textField/button/window aquí, el campo de texto del
        // BAUTIZO, el de la seed en el título y el de bautizar procedimientos
        // del diario dejan de ser widgets del sistema los tres a la vez.
        //
        // Las texturas se generan UNA vez por sesión y llevan
        // HideFlags.HideAndDontSave: sin eso, la primera recarga de escena
        // (DayCycle.RestartRun) las destruiría dejando al skin -- que es un
        // objeto compartido y sobrevive a la recarga -- apuntando a texturas
        // muertas, o sea recuadros en blanco donde antes había campos.
        // -----------------------------------------------------------------
        private static bool _skinVestido;
        private static Texture2D _texCampo, _texCampoFoco, _texBoton, _texBotonHover, _texVentana, _texSliderThumb;

        private static void VestirSkin()
        {
            if (_skinVestido) return;
            _skinVestido = true;

            _texCampo = TexturaMarco(Carboncillo, LatonOscuro, new Color(0f, 0f, 0f, 0.35f));
            _texCampoFoco = TexturaMarco(new Color(0.135f, 0.125f, 0.115f, 1f), Laton, new Color(0f, 0f, 0f, 0.30f));
            _texBoton = TexturaMarco(new Color(0.125f, 0.118f, 0.128f, 1f), LatonOscuro, new Color(0f, 0f, 0f, 0.25f));
            _texBotonHover = TexturaMarco(new Color(0.20f, 0.17f, 0.13f, 1f), Laton, new Color(0f, 0f, 0f, 0.20f));
            _texVentana = TexturaMarco(Pergamino, Laton, new Color(0f, 0f, 0f, 0.40f));
            // (ENCARGO M, AJUSTES) "el thumb de latón": fondo Laton macizo con
            // filo LatonOscuro -- mismo TexturaMarco que ya usan botones/
            // campos/ventana, solo con el metal como RELLENO en vez de como
            // borde, para que el grabber lea como una pieza sólida, no como
            // un hueco.
            _texSliderThumb = TexturaMarco(Laton, LatonOscuro, new Color(0f, 0f, 0f, 0.35f));

            var skin = GUI.skin;
            if (skin == null) return;

            if (FuenteCuerpo != null) skin.font = FuenteCuerpo;

            // CARET VISIBLE (encargo explícito): el cursor de escritura del
            // skin por defecto es blanco fino sobre gris; sobre carboncillo
            // se perdía. Oro y con parpadeo lento -- se ve que el taller
            // espera a que escribas.
            skin.settings.cursorColor = Oro;
            skin.settings.cursorFlashSpeed = 0.9f;
            skin.settings.selectionColor = new Color(Laton.r, Laton.g, Laton.b, 0.45f);

            VestirEstilo(skin.textField, _texCampo, _texCampoFoco, Texto, 8, 6);
            VestirEstilo(skin.textArea, _texCampo, _texCampoFoco, Texto, 8, 6);
            VestirEstilo(skin.button, _texBoton, _texBotonHover, Texto, 10, 7);
            skin.button.hover.textColor = Oro;
            skin.button.active.textColor = Oro;
            VestirEstilo(skin.box, _texVentana, _texVentana, Texto, 8, 6);

            skin.window.normal.background = _texVentana;
            skin.window.onNormal.background = _texVentana;
            skin.window.border = new RectOffset(MarcoBorde, MarcoBorde, MarcoBorde, MarcoBorde);
            skin.window.padding = new RectOffset(14, 14, 20, 14);
            skin.window.normal.textColor = Oro;
            skin.window.onNormal.textColor = Oro;
            if (FuenteTitulos != null) skin.window.font = FuenteTitulos;
        }

        private static void VestirEstilo(GUIStyle s, Texture2D fondo, Texture2D fondoActivo, Color texto, int padX, int padY)
        {
            if (s == null) return;
            s.normal.background = fondo;
            s.hover.background = fondoActivo;
            s.active.background = fondoActivo;
            s.focused.background = fondoActivo;
            s.onNormal.background = fondoActivo;
            s.onHover.background = fondoActivo;
            s.onActive.background = fondoActivo;
            s.onFocused.background = fondoActivo;
            s.normal.textColor = texto;
            s.focused.textColor = texto;
            s.border = new RectOffset(MarcoBorde, MarcoBorde, MarcoBorde, MarcoBorde);
            s.padding = new RectOffset(padX, padX, padY, padY);
        }

        private const int MarcoLado = 16;
        private const int MarcoBorde = 5;

        /// <summary>
        /// Textura 9-slice de "chapa del taller": relleno plano + filo de
        /// latón de 1 téxel + línea de sombra interior. El centro es plano a
        /// propósito (es lo que se estira): todo el carácter vive en los 5
        /// téxeles de borde que el 9-slice conserva sin deformar.
        /// </summary>
        private static Texture2D TexturaMarco(Color fondo, Color filo, Color sombraInterior)
        {
            var tex = new Texture2D(MarcoLado, MarcoLado, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave, // ver el bloque de arriba: el skin sobrevive a la recarga de escena, la textura también debe hacerlo.
                name = "UiStylesMarco",
            };

            var px = new Color32[MarcoLado * MarcoLado];
            for (int y = 0; y < MarcoLado; y++)
            {
                for (int x = 0; x < MarcoLado; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, MarcoLado - 1 - x), Mathf.Min(y, MarcoLado - 1 - y));
                    Color c;
                    if (d == 0) c = filo;                                     // filo de latón
                    else if (d == 1) c = Color.Lerp(fondo, Color.black, 0.55f); // canto oscuro que hunde el marco
                    else if (d == 2) c = Color.Lerp(fondo, sombraInterior, sombraInterior.a); // sombra interior proyectada
                    else c = fondo;
                    c.a = 1f;
                    px[y * MarcoLado + x] = c;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        // -----------------------------------------------------------------
        // (playtest 31) ESPACIADO DE CAPITALES. Cinzel es una lapidaria: en
        // piedra las capitales van separadas, y pegadas se leen apelmazadas.
        // IMGUI no tiene letter-spacing, así que se intercala un espacio fino
        // entre caracteres -- pero UNA sola vez por cadena, cacheado: hacerlo
        // en OnGUI asignaría un string por frame (regla de cero allocs).
        // -----------------------------------------------------------------
        private static readonly System.Collections.Generic.Dictionary<string, string> _espaciados =
            new System.Collections.Generic.Dictionary<string, string>(8);

        /// <summary>"CHAOS ALCHEMY" → "C H A O S   A L C H E M Y". Cacheado por cadena: no asigna nada a partir de la segunda llamada.</summary>
        public static string Espaciar(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return texto;
            if (_espaciados.TryGetValue(texto, out string ya)) return ya;

            var sb = new System.Text.StringBuilder(texto.Length * 2);
            for (int i = 0; i < texto.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(texto[i]);
            }
            string r = sb.ToString();
            _espaciados[texto] = r;
            return r;
        }

        // -----------------------------------------------------------------
        // Medidas
        // -----------------------------------------------------------------

        /// <summary>Convierte píxeles "de diseño" (pensados a 720p) a píxeles reales.</summary>
        public static float S(float px) => px * Escala;

        /// <summary>Igual que <see cref="S"/> pero para tamaños de fuente (entero).</summary>
        public static int F(int px) => Mathf.Max(9, Mathf.RoundToInt(px * Escala));

        /// <summary>Alto que ocupará `texto` con `estilo` en un ancho dado (con word-wrap real). Sin asignaciones: reutiliza un GUIContent interno.</summary>
        public static float Alto(GUIStyle estilo, string texto, float ancho)
        {
            _medida.text = texto ?? "";
            return estilo.CalcHeight(_medida, ancho);
        }

        /// <summary>Ancho de una línea de texto con `estilo` (sin word-wrap).</summary>
        public static float Ancho(GUIStyle estilo, string texto)
        {
            _medida.text = texto ?? "";
            return estilo.CalcSize(_medida).x;
        }

        // -----------------------------------------------------------------
        // Primitivas de dibujo
        // -----------------------------------------------------------------

        /// <summary>
        /// (fix playtest 14: "recuadros negros vacíos") Alfa por debajo del
        /// cual un rótulo desvanecido NO SE DIBUJA -- ni panel ni borde ni
        /// texto, `return` inmediato. El valor anterior de
        /// <see cref="PlacaMundoLateral"/> (0.02) era tan bajo que dejaba
        /// pasar rótulos con texto YA ILEGIBLE (alfa lineal 0.02-0.1) cuyo
        /// panel se seguía dibujando -- y <see cref="PlacaMundo"/> ni
        /// siquiera TENÍA un corte, dibujaba el panel a cualquier alfa,
        /// incluido 0 exacto (ver caso real más abajo). 0.12 es
        /// aproximadamente el punto por debajo del cual `Texto`/`TextoTenue`
        /// sobre `TintaFuerte` ya no se lee en la práctica a 720p -- por
        /// debajo de eso no compensa dibujar nada.
        /// </summary>
        private const float AlfaMinimaVisible = 0.12f;

        /// <summary>
        /// (fix playtest 14) Curva de desvanecimiento del FONDO de un rótulo,
        /// separada de la del TEXTO. Causa real de los "recuadros negros
        /// vacíos" reportados (dos al arrancar + el rótulo de HELANDO
        /// quedándose en negro un instante al alejarse del frío):
        /// `TintaFuerte` es casi negra con alfa 0.96 -- perceptualmente un
        /// panel a alfa lineal 0.3 SIGUE leyéndose como una caja opaca,
        /// mientras que el texto claro sobre él a esa misma alfa ya es
        /// ilegible. Elevar el fondo al CUBO (mucho más rápido que la caída
        /// lineal del texto, que no cambia) hace que el panel se apague
        /// bastante ANTES de que el texto deje de leerse, así que durante
        /// TODO el desvanecimiento hay letra dentro de la caja o no hay caja
        /// -- nunca una caja vacía. (Antes de este fix, PlacaMundo ni
        /// siquiera aplicaba ESTA curva al panel: lo dibujaba siempre a la
        /// opacidad fija de TintaFuerte, sin importar la alfa del texto que
        /// recibía -- ver el caso real de las dos cubetas en el doc de
        /// HeatPlate.cs/ChillStone.cs.)
        /// </summary>
        private static float AlfaPanel(float alfa) => alfa * alfa * alfa;

        /// <summary>
        /// (fix playtest 16: "el título de la Tolva me sigue por la pantalla al
        /// moverme a la izquierda") Margen, en píxeles de pantalla, alrededor
        /// del rectángulo visible dentro del cual un rótulo de mundo TODAVÍA se
        /// dibuja aunque su ancla haya cruzado el borde -- solo para que un
        /// objeto justo en el borde de cuadro no parpadee al entrar y salir por
        /// un píxel de más o de menos con cada frame de movimiento de cámara.
        /// Fuera de este margen no se dibuja NADA: ni panel, ni texto, ni un
        /// clamp que lo "traiga" de vuelta a la pantalla.
        ///
        /// CAUSA RAÍZ del bug: hasta la fase de mundo grande la cámara
        /// enmarcaba el mundo ENTERO (ver AlkahestSceneBuilder), así que todo
        /// punto de mundo estaba siempre en pantalla y el Mathf.Clamp que
        /// tenían <see cref="Globo"/> y <see cref="PlacaMundo"/> (en X) nunca
        /// se disparaba -- era una salvaguarda inocua contra un rótulo que se
        /// dibujara un pelín fuera del área de juego, no una regla de
        /// visibilidad. Con la cámara SIGUIENDO al aprendiz (pantalla de tres)
        /// ese clamp se convirtió en el bug real: la Tolva, en cuanto queda
        /// fuera de cuadro por moverse hacia la izquierda, tenía su rótulo
        /// clavado en el borde derecho de la pantalla en vez de desaparecer --
        /// el "objeto fuera de cuadro" no importaba, el clamp lo dibujaba
        /// siempre en alguna parte visible, así que parecía perseguir al
        /// jugador. La regla correcta con cámara móvil es la contraria: fuera
        /// de vista es fuera de vista, punto.
        /// </summary>
        private static float MargenFueraDeCuadro => S(24f);

        /// <summary>
        /// ¿Cae (x, y) -- un punto ya proyectado a coordenadas de PANTALLA,
        /// cualquiera de las dos convenciones de origen sirve porque el
        /// rectángulo [0,ancho]x[0,alto] es el mismo volteado en Y -- dentro de
        /// la pantalla, con <paramref name="margenPx"/> de colchón? Único sitio
        /// donde vive el criterio de "¿el ancla de este rótulo de mundo sigue
        /// siendo visible?", compartido por TODA la familia de rótulos de mundo
        /// (<see cref="Globo"/> vía <see cref="EtiquetaMundo"/>,
        /// <see cref="PlacaMundo"/>, <see cref="PlacaMundoLateral"/>) -- ver
        /// <see cref="MargenFueraDeCuadro"/> para la causa raíz del fix
        /// playtest 16.
        /// </summary>
        private static bool DentroDePantalla(float x, float y, float margenPx)
        {
            return x >= -margenPx && x <= Screen.width + margenPx
                && y >= -margenPx && y <= Screen.height + margenPx;
        }

        /// <summary>Rellena un rectángulo con un color plano (usa la textura blanca de Unity tintada con GUI.color).</summary>
        public static void Rellenar(Rect r, Color c)
        {
            var previo = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = previo;
        }

        /// <summary>Panel estándar del juego: tinta oscura translúcida + filete dorado.</summary>
        public static void Panel(Rect r) => Panel(r, Tinta, Borde);

        public static void Panel(Rect r, Color fondo, Color borde)
        {
            Rellenar(r, fondo);
            float t = Mathf.Max(1f, Mathf.Round(Escala));
            Rellenar(new Rect(r.x, r.y, r.width, t), borde);
            Rellenar(new Rect(r.x, r.yMax - t, r.width, t), borde);
            Rellenar(new Rect(r.x, r.y, t, r.height), borde);
            Rellenar(new Rect(r.xMax - t, r.y, t, r.height), borde);
        }

        // -----------------------------------------------------------------
        // (playtest 31) EL PANEL DE RITO. La diferencia entre "una caja de
        // diálogo" y "un momento del juego" no es el contenido: es que el
        // fondo tenga PROFUNDIDAD (no un gris plano) y que el filo sea de un
        // MATERIAL reconocible del mundo (latón, no un borde de widget).
        // Aquí se dibuja a mano, con Rellenar, porque un 9-slice estirado no
        // puede tener degradado ni esquinas de latón sin deformarlas.
        // Coste: ~20 GUI.DrawTexture por frame en UNA ventana modal -- nada
        // frente a lo que ya cuesta el diario, y cero asignaciones.
        // -----------------------------------------------------------------

        /// <summary>Bandas del degradado del pergamino. 10 basta para que no se vea escalonado a ninguna resolución y es barato.</summary>
        private const int BandasPergamino = 10;

        /// <summary>
        /// Panel ceremonial: vitela ahumada (más cálida y clara arriba, casi
        /// negra abajo, como iluminada por un fuego que está en el suelo del
        /// taller), doble filete de latón y cantoneras. Es el fondo del
        /// BAUTIZO y de cualquier rito que venga después.
        /// </summary>
        public static void PanelRito(Rect r)
        {
            // 1) Sombra proyectada: el panel FLOTA sobre el mundo.
            float sombra = S(6f);
            Rellenar(new Rect(r.x + sombra, r.y + sombra, r.width, r.height), new Color(0f, 0f, 0f, 0.45f));

            // 2) La vitela, en bandas horizontales de arriba (cálida) a abajo (oscura).
            for (int i = 0; i < BandasPergamino; i++)
            {
                float t = i / (float)(BandasPergamino - 1);
                float y0 = r.y + r.height * (i / (float)BandasPergamino);
                float y1 = r.y + r.height * ((i + 1) / (float)BandasPergamino);
                Color c = Color.Lerp(new Color(0.175f, 0.152f, 0.140f, 0.985f),
                                     new Color(0.072f, 0.062f, 0.068f, 0.985f), t * t);
                Rellenar(new Rect(r.x, y0, r.width, y1 - y0 + 1f), c);
            }

            MarcoLaton(r);
        }

        /// <summary>
        /// Marco de latón de dos filetes con cantoneras: filo exterior
        /// grueso, aire, hilo interior fino, y cuatro escuadras en las
        /// esquinas. Es EL gesto que separa un objeto del taller de un
        /// rectángulo de sistema, y por eso vive suelto: lo usan el panel de
        /// rito, el swatch del bautizo y el marco del diario.
        /// </summary>
        public static void MarcoLaton(Rect r) => MarcoLaton(r, Laton, 1f);

        public static void MarcoLaton(Rect r, Color laton, float intensidad)
        {
            float g = Mathf.Max(2f, Mathf.Round(S(2f)));
            float h = Mathf.Max(1f, Mathf.Round(S(1f)));
            var fuerte = new Color(laton.r, laton.g, laton.b, laton.a * intensidad);
            var tenue = new Color(laton.r * 0.62f, laton.g * 0.62f, laton.b * 0.62f, laton.a * 0.75f * intensidad);

            // Filete exterior.
            Rellenar(new Rect(r.x, r.y, r.width, g), fuerte);
            Rellenar(new Rect(r.x, r.yMax - g, r.width, g), fuerte);
            Rellenar(new Rect(r.x, r.y, g, r.height), fuerte);
            Rellenar(new Rect(r.xMax - g, r.y, g, r.height), fuerte);

            // Hilo interior, separado por aire: dos líneas leen como metal labrado; una sola, como un borde de ventana.
            float m = S(5f);
            var inte = new Rect(r.x + m, r.y + m, r.width - m * 2f, r.height - m * 2f);
            Rellenar(new Rect(inte.x, inte.y, inte.width, h), tenue);
            Rellenar(new Rect(inte.x, inte.yMax - h, inte.width, h), tenue);
            Rellenar(new Rect(inte.x, inte.y, h, inte.height), tenue);
            Rellenar(new Rect(inte.xMax - h, inte.y, h, inte.height), tenue);

            // Cantoneras: escuadras macizas en las cuatro esquinas.
            float l = S(14f);
            Rellenar(new Rect(r.x, r.y, l, g * 2f), fuerte);
            Rellenar(new Rect(r.x, r.y, g * 2f, l), fuerte);
            Rellenar(new Rect(r.xMax - l, r.y, l, g * 2f), fuerte);
            Rellenar(new Rect(r.xMax - g * 2f, r.y, g * 2f, l), fuerte);
            Rellenar(new Rect(r.x, r.yMax - g * 2f, l, g * 2f), fuerte);
            Rellenar(new Rect(r.x, r.yMax - l, g * 2f, l), fuerte);
            Rellenar(new Rect(r.xMax - l, r.yMax - g * 2f, l, g * 2f), fuerte);
            Rellenar(new Rect(r.xMax - g * 2f, r.yMax - l, g * 2f, l), fuerte);
        }

        /// <summary>
        /// Filete separador con rombo central: la regla de un manuscrito.
        /// Una línea recta a secas es una regla de CSS; con el rombo es una
        /// página. `y` es la línea, `ancho` el tramo centrado en `cx`.
        /// </summary>
        public static void FileteRombo(float cx, float y, float ancho, Color color)
        {
            float h = Mathf.Max(1f, Mathf.Round(S(1f)));
            float mitad = ancho * 0.5f;
            float hueco = S(9f);
            Rellenar(new Rect(cx - mitad, y, mitad - hueco, h), color);
            Rellenar(new Rect(cx + hueco, y, mitad - hueco, h), color);

            // Rombo: cuatro filas que crecen y decrecen (un cuadrado girado 45º dibujado con rectángulos).
            float paso = Mathf.Max(1f, Mathf.Round(S(1.5f)));
            for (int i = 0; i < 3; i++)
            {
                float w = paso * (i + 1);
                Rellenar(new Rect(cx - w * 0.5f, y - paso * (2 - i), w, paso), color);
                Rellenar(new Rect(cx - w * 0.5f, y + paso * (2 - i), w, paso), color);
            }
            Rellenar(new Rect(cx - paso * 1.5f, y, paso * 3f, paso), color);
        }

        /// <summary>Barra de progreso: hueco oscuro + relleno del color indicado.</summary>
        public static void Barra(Rect r, float fraccion, Color relleno)
        {
            Rellenar(r, Hueco);
            fraccion = Mathf.Clamp01(fraccion);
            float t = Mathf.Max(1f, Mathf.Round(Escala));
            if (fraccion > 0f)
            {
                Rellenar(new Rect(r.x + t, r.y + t, (r.width - t * 2f) * fraccion, r.height - t * 2f), relleno);
            }
        }

        /// <summary>
        /// Chip flotante centrado en una posición de PANTALLA IMGUI (origen
        /// arriba-izquierda). `centroGui` puede venir de dos sitios muy
        /// distintos: un punto de MUNDO ya proyectado (vía
        /// <see cref="EtiquetaMundo"/> -- máquinas, Tolva) o una posición de
        /// PANTALLA pura, como el cursor del ratón (FlaskHud/SubstanceKnowledge,
        /// que por definición nunca sale del rectángulo visible). El
        /// Mathf.Clamp de más abajo SOLO existe para el segundo caso: mantener
        /// una burbuja diminuta pegada al cursor totalmente legible aunque el
        /// ratón esté justo en el borde. NUNCA decide si algo se ve -- eso lo
        /// decide <see cref="DentroDePantalla"/> primero (fix playtest 16, ver
        /// <see cref="MargenFueraDeCuadro"/>): si el ancla ya viene de fuera
        /// del rectángulo con su margen, no se dibuja nada, ni recortado al
        /// borde ni de ninguna otra forma -- así un rótulo de mundo cuyo
        /// objeto está fuera de cuadro (la Tolva al desplazarse la cámara)
        /// desaparece de verdad en vez de quedarse pegado al borde siguiendo
        /// al jugador.
        /// </summary>
        public static void Globo(Vector2 centroGui, string texto, Color color)
        {
            Preparar();
            if (Chip == null || string.IsNullOrEmpty(texto)) return; // defensivo: fuera de OnGUI no hay estilos.
            // (fix playtest 14) mismo umbral/curva que PlacaMundo -- ver
            // AlfaMinimaVisible/AlfaPanel. Hoy TODOS los llamantes de Globo
            // pasan alfa 1 (no hay fundido), así que esto es un no-op en la
            // práctica; se deja preparado para que si algún día alguien
            // desvanece un Globo, no reintroduzca el mismo bug de "recuadro
            // negro vacío" que PlacaMundo/PlacaMundoLateral sí tenían.
            if (color.a <= AlfaMinimaVisible) return;
            // (fix playtest 16) ver doc de la clase justo arriba -- causa raíz
            // del rótulo de la Tolva persiguiendo al jugador por el borde.
            if (!DentroDePantalla(centroGui.x, centroGui.y, MargenFueraDeCuadro)) return;

            float w = Ancho(Chip, texto) + S(16f);
            float h = Chip.lineHeight + S(10f);
            float x = Mathf.Clamp(centroGui.x - w * 0.5f, S(4f), Mathf.Max(S(4f), Screen.width - w - S(4f)));
            float y = Mathf.Clamp(centroGui.y - h * 0.5f, S(4f), Mathf.Max(S(4f), Screen.height - h - S(4f)));
            var r = new Rect(x, y, w, h);

            float alfaPanel = AlfaPanel(color.a);
            Panel(r, new Color(TintaFuerte.r, TintaFuerte.g, TintaFuerte.b, TintaFuerte.a * alfaPanel),
                new Color(color.r, color.g, color.b, 0.55f * alfaPanel));
            var previo = Chip.normal.textColor;
            Chip.normal.textColor = color;
            GUI.Label(r, texto, Chip);
            Chip.normal.textColor = previo;
        }

        /// <summary>
        /// Etiqueta anclada a un punto del MUNDO (máquinas, Tolva): se convierte
        /// a pantalla, se centra sobre el objeto y se desplaza `subirPx` píxeles
        /// hacia arriba. Devuelve sin dibujar si no hay cámara, si el punto
        /// queda detrás de la cámara (`s.z <= 0`), o si el ancla cae fuera del
        /// rectángulo visible (fix playtest 16, ver
        /// <see cref="UiStyles.MargenFueraDeCuadro"/>): con la cámara siguiendo
        /// al aprendiz, "fuera de cuadro" ya no es un caso raro (era imposible
        /// con la cámara fija que enmarcaba el mundo entero), así que un objeto
        /// fuera de pantalla debe dejar de anunciarse, no quedarse clavado en
        /// el borde -- eso es justo lo que hacía antes <see cref="Globo"/> con
        /// su Mathf.Clamp, y era la causa del rótulo de la Tolva "persiguiendo"
        /// al jugador reportada en el playtest 16. <see cref="Globo"/> repite
        /// esta misma comprobación (defensa en profundidad si algún día se le
        /// llama con un punto de mundo sin pasar por aquí), así que este
        /// rechazo temprano es sobre todo documentación de la causa.
        /// </summary>
        public static void EtiquetaMundo(Vector3 posicionMundo, string texto, Color color, float subirPx)
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 s = cam.WorldToScreenPoint(posicionMundo);
            if (s.z <= 0f) return;
            if (!DentroDePantalla(s.x, s.y, MargenFueraDeCuadro)) return; // (fix playtest 16)

            // Mouse/cámara usan origen abajo-izquierda; IMGUI arriba-izquierda.
            Globo(new Vector2(s.x, Screen.height - s.y - subirPx), texto, color);
        }

        /// <summary>
        /// RÓTULO FIJO DE APARATO. Pequeño, sin fondo opaco de aviso, anclado a
        /// un punto del mundo con un desplazamiento en píxeles (positivo = hacia
        /// arriba, NEGATIVO = hacia abajo) y SIN recortarse contra ningún borde
        /// de la pantalla, ni en X ni en Y (fix playtest 16, ver más abajo).
        ///
        /// POR QUÉ EXISTE (playtest 4: "el label de las placas tapa la
        /// interacción al aspirar"): <see cref="Globo"/> centra su chip sobre el
        /// objeto y lo empuja dentro de la pantalla si se sale, así que el
        /// rótulo de una placa —que vive en el FONDO de una cuba, justo donde el
        /// jugador aspira— acababa bajo el cursor. Estos rótulos son chapas
        /// atornilladas al aparato: van SIEMPRE en el mismo sitio relativo a él
        /// (típicamente por debajo, sobre la piedra, fuera de la zona de trabajo),
        /// son diminutos y no se mueven nunca.
        ///
        /// ACOTADO A BORDES, HERENCIA DE LA CÁMARA FIJA (fix playtest 16): este
        /// método SÍ acotaba antes la X con Mathf.Clamp ("para que una chapa
        /// junto al muro derecho siguiera siendo legible"), mientras que en Y
        /// nunca se acotó a propósito. Esa asimetría solo era inocua con la
        /// cámara fija de antes (enmarcaba el mundo entero: el clamp en X nunca
        /// llegaba a dispararse de verdad). Con la cámara siguiendo al aprendiz
        /// el clamp en X reproducía EXACTAMENTE el mismo bug que <see cref="Globo"/>
        /// tenía con el rótulo de la Tolva: una chapa cuyo aparato queda fuera
        /// de cuadro por la izquierda o la derecha se quedaba clavada en ese
        /// borde en vez de desaparecer. Se quita el clamp en X y se añade el
        /// mismo rechazo temprano que ya tenía Y de forma implícita (dibujar
        /// fuera del rectángulo de pantalla ya no se veía, pero ahora es
        /// explícito y con margen, ver <see cref="DentroDePantalla"/>): las dos
        /// coordenadas comparten HOY el mismo criterio -- si el aparato está
        /// fuera de cuadro, su chapa también lo está, punto.
        /// </summary>
        public static void PlacaMundo(Vector3 posicionMundo, string texto, Color color, float desplazarPx)
        {
            Preparar();
            var cam = Camera.main;
            if (cam == null || ChipMini == null || string.IsNullOrEmpty(texto)) return;

            // (fix playtest 14: "recuadros negros vacíos") ANTES este método no
            // comprobaba `color.a` en absoluto: el panel se dibujaba SIEMPRE a
            // la opacidad fija de TintaFuerte, sin importar lo transparente que
            // viniera `color` (que es donde HeatPlate/ChillStone codifican el
            // desvanecimiento por cercanía, `color.a * cercania`). Caso real
            // reproducido con las constantes de SimLevelBuilder: al arrancar,
            // el aprendiz spawnea a ~4.75 UNIDADES DE MUNDO de HeatPlate_0
            // (dentro de RangoEstadoDesvanece=6.5, cercaniaEstado>0 así que el
            // OnGUI del aparato NO retorna pronto) pero a ~4.75 >
            // RangoNombreDesvanece=3.6 (cercaniaNombre=0 EXACTO); el anillo de
            // NOMBRE llamaba a PlacaMundo con texto NO vacío ("placa ígnea") y
            // alfa 0 -- texto invisible, panel opaco: un recuadro negro sin
            // letra. Lo mismo le pasaba a ChillStone_Bandeja (~5.17 unidades,
            // mismo patrón) -- los DOS recuadros reportados. Ahora el panel se
            // corta con el mismo umbral que el texto (return si color.a es
            // demasiado bajo para leerse) y además se apaga MÁS RÁPIDO que el
            // texto mientras SÍ es visible (ver AlfaPanel) -- por eso también
            // ya no queda una caja residual un instante al alejarse (HELANDO).
            if (color.a <= AlfaMinimaVisible) return;

            Vector3 s = cam.WorldToScreenPoint(posicionMundo);
            if (s.z <= 0f) return;
            if (!DentroDePantalla(s.x, s.y, MargenFueraDeCuadro)) return; // (fix playtest 16)

            float w = Ancho(ChipMini, texto) + S(10f);
            float h = ChipMini.lineHeight + S(6f);
            // (fix playtest 16) Ni X ni Y se acotan ya al borde -- ver doc de
            // la clase. El rechazo de arriba ya garantiza que solo llegamos
            // aquí con un ancla dentro de pantalla (o dentro del margen de
            // colchón), así que dibujar sin clamp no puede "salirse" de forma
            // perceptible.
            float x = s.x - w * 0.5f;
            float y = Screen.height - s.y - desplazarPx - h * 0.5f;
            var r = new Rect(x, y, w, h);

            float alfaPanel = AlfaPanel(color.a);
            Panel(r, new Color(TintaFuerte.r, TintaFuerte.g, TintaFuerte.b, TintaFuerte.a * alfaPanel),
                new Color(color.r, color.g, color.b, 0.45f * alfaPanel));
            var previo = ChipMini.normal.textColor;
            ChipMini.normal.textColor = color;
            GUI.Label(r, texto, ChipMini);
            ChipMini.normal.textColor = previo;
        }

        // -----------------------------------------------------------------
        // RESTAURADO (playtest 14). Estos dos miembros se escribieron en el
        // playtest 7 y DESAPARECIERON en el commit e3fed6f (playtest 10), al
        // desplegar sobre una copia de trabajo obsoleta del sandbox. Nadie lo
        // notó durante tres rondas porque el juego seguía compilando: los
        // consumidores se habían perdido en el mismo golpe. Si vuelven a
        // desaparecer, es el mismo fallo de proceso — ver CLAUDE.md.
        // -----------------------------------------------------------------

        /// <summary>
        /// (fix playtest 7: "el rótulo del agua está escrito sobre el grifo de
        /// arena") Chapa anclada a un LADO del punto de mundo en vez de encima.
        /// Los cinco grifos están en columna a 1 unidad de mundo unos de otros,
        /// así que cualquier desplazamiento vertical cae inevitablemente sobre
        /// el aparato vecino. Anclando la chapa a la IZQUIERDA (contra el pilar
        /// de piedra, que es espacio muerto) cada grifo tiene su propio carril y
        /// no hay colisión posible.
        ///
        /// `aLaIzquierda` = true pone el borde DERECHO de la chapa a
        /// `separacionPx` de la posición; false pone el borde IZQUIERDO.
        /// A diferencia de <see cref="PlacaMundo"/> (antes del fix playtest 16)
        /// esta nunca acotó en X -- ya tenía el criterio correcto de "si el
        /// aparato está fuera de cuadro su chapa también debe estarlo". Lo que
        /// le faltaba, y ahora comparte con el resto de la familia, es el
        /// rechazo TEMPRANO y explícito por estar fuera del rectángulo visible
        /// (ver <see cref="DentroDePantalla"/>): antes confiaba en que dibujar
        /// un Rect fuera de pantalla simplemente "no se viera", lo cual es
        /// cierto pero no documenta la regla ni ahorra el trabajo de medir/
        /// pintar un rótulo que nadie va a ver.
        /// </summary>
        public static void PlacaMundoLateral(Vector3 posicionMundo, string texto, Color color,
                                             float separacionPx, float desplazarYPx, float alfa, bool aLaIzquierda)
        {
            // (fix playtest 14: "recuadros negros vacíos") El umbral anterior
            // (0.02) era demasiado bajo -- a esa alfa el texto lineal ya
            // llevaba un buen rato siendo ilegible mientras el panel (que
            // desvanecía con el MISMO factor lineal que el texto, no más
            // rápido) todavía se leía como una caja sólida. Subido a
            // AlfaMinimaVisible (0.12, ver doc del campo): por debajo de eso
            // ni se dibuja el panel ni el texto.
            if (alfa <= AlfaMinimaVisible) return;
            alfa = alfa > 1f ? 1f : alfa;

            Preparar();
            var cam = Camera.main;
            if (cam == null || ChipMini == null || string.IsNullOrEmpty(texto)) return;

            Vector3 s = cam.WorldToScreenPoint(posicionMundo);
            if (s.z <= 0f) return;
            if (!DentroDePantalla(s.x, s.y, MargenFueraDeCuadro)) return; // (fix playtest 16)

            float w = Ancho(ChipMini, texto) + S(10f);
            float h = ChipMini.lineHeight + S(6f);
            float x = aLaIzquierda ? s.x - separacionPx - w : s.x + separacionPx;
            float y = Screen.height - s.y - desplazarYPx - h * 0.5f;
            var r = new Rect(x, y, w, h);

            // (fix playtest 14) El panel se apaga con `alfa` AL CUBO
            // (AlfaPanel) mientras el texto sigue en LINEAL (color.a * alfa,
            // sin cambios) -- a mitad de desvanecimiento el panel ya es
            // mucho más tenue que la letra, así que nunca sobrevive una caja
            // negra sin texto legible dentro.
            float alfaPanel = AlfaPanel(alfa);
            Panel(r,
                new Color(TintaFuerte.r, TintaFuerte.g, TintaFuerte.b, TintaFuerte.a * alfaPanel),
                new Color(color.r, color.g, color.b, 0.45f * alfaPanel));
            var previo = ChipMini.normal.textColor;
            ChipMini.normal.textColor = new Color(color.r, color.g, color.b, color.a * alfa);
            GUI.Label(r, texto, ChipMini);
            ChipMini.normal.textColor = previo;
        }

        /// <summary>
        /// CURVA DE CERCANÍA compartida por todos los aparatos del taller
        /// (fix playtest 6). Devuelve 1 cuando el aprendiz está dentro de
        /// `rangoPleno`, baja suavemente hasta 0 al llegar a `rangoDesvanece`,
        /// y 0 más allá. Un único sitio donde vive el criterio de "cerca", para
        /// que placa ígnea, piedra gélida, grifos y Tolva se comporten igual.
        /// </summary>
        public static float Cercania(Vector3 puntoMundo, Transform jugador, float rangoPleno, float rangoDesvanece)
        {
            if (jugador == null) return 0f;
            float d2 = (puntoMundo - jugador.position).sqrMagnitude;
            float pleno2 = rangoPleno * rangoPleno;
            if (d2 <= pleno2) return 1f;
            float fuera2 = rangoDesvanece * rangoDesvanece;
            if (d2 >= fuera2 || fuera2 <= pleno2) return 0f;
            // Suavizado en distancia real (no cuadrada): la aparición se siente lineal.
            float t = (Mathf.Sqrt(d2) - rangoPleno) / (rangoDesvanece - rangoPleno);
            return Mathf.SmoothStep(1f, 0f, t);
        }

        /// <summary>
        /// (fix playtest 10) ¿Está el jugador ESCRIBIENDO en un campo de texto
        /// ahora mismo? Mientras bautizaba una sustancia, la "m" de su nombre
        /// silenciaba el juego: la tecla la leían a la vez el campo de texto y
        /// el atajo global.
        ///
        /// REGLA DEL PROYECTO: todo atajo de una sola tecla (M silenciar, H
        /// pistas, T bautizar, E interactuar, Q vaciar, J diario, flechas,
        /// F3 paleta dev) debe comprobar esta propiedad y NO hacer nada
        /// mientras valga true. Un campo de texto se come TODAS las letras.
        ///
        /// La levanta y la baja quien abre y cierra el campo (Game/NamingUi.cs).
        /// Se guarda el frame en que se bajó para que el atajo tampoco se
        /// dispare en el mismo frame en que se confirma el nombre con Enter.
        /// </summary>
        public static bool EscribiendoTexto
        {
            get => _escribiendoTexto || Time.frameCount <= _frameFinEscritura + 1;
            set
            {
                if (_escribiendoTexto && !value) _frameFinEscritura = Time.frameCount;
                _escribiendoTexto = value;
            }
        }

        private static bool _escribiendoTexto;
        private static int _frameFinEscritura = -10;

        /// <summary>
        /// True si el jugador tiene algún botón del ratón PULSADO (está
        /// aspirando, vertiendo o vaciando). Las máquinas usan esto para callar
        /// su prompt "E — ..." mientras se trabaja: el prompt es una invitación,
        /// y una invitación no debe aparecer encima de las manos de nadie.
        /// </summary>
        public static bool RatonOcupado
        {
            get
            {
                var m = Mouse.current;
                if (m == null) return false;
                return m.leftButton.isPressed || m.rightButton.isPressed || m.middleButton.isPressed;
            }
        }
    }
}
