using UnityEngine;
using UnityEngine.InputSystem;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy · pase visual M5] Mini-sistema de estilo para TODA la UI
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
        public static readonly Color Exito = new Color(0.52f, 0.92f, 0.60f, 1f);
        public static readonly Color Frio = new Color(0.55f, 0.85f, 1.00f, 1f);
        public static readonly Color Hueco = new Color(0f, 0f, 0f, 0.55f);

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

            var raiz = GUI.skin.label;

            Titulo = Etiqueta(raiz, 15, FontStyle.Bold, TextAnchor.UpperLeft, Oro, false);
            Cuerpo = Etiqueta(raiz, 13, FontStyle.Normal, TextAnchor.UpperLeft, Texto, true);
            CuerpoDer = Etiqueta(raiz, 13, FontStyle.Normal, TextAnchor.UpperRight, Texto, false);
            CuerpoLinea = Etiqueta(raiz, 13, FontStyle.Normal, TextAnchor.UpperLeft, Texto, false);
            CuerpoCentrado = Etiqueta(raiz, 14, FontStyle.Normal, TextAnchor.UpperCenter, Texto, true);
            CuerpoTenue = Etiqueta(raiz, 12, FontStyle.Normal, TextAnchor.UpperLeft, TextoTenue, true);
            TenueCentrado = Etiqueta(raiz, 11, FontStyle.Normal, TextAnchor.UpperCenter, TextoTenue, false);
            Numero = Etiqueta(raiz, 15, FontStyle.Bold, TextAnchor.UpperRight, Oro, false);
            Chip = Etiqueta(raiz, 12, FontStyle.Bold, TextAnchor.MiddleCenter, Texto, false);
            ChipMini = Etiqueta(raiz, 10, FontStyle.Bold, TextAnchor.MiddleCenter, Texto, false);
            Alerta = Etiqueta(raiz, 13, FontStyle.Bold, TextAnchor.UpperCenter, Aviso, true);
            Reloj = Etiqueta(raiz, 21, FontStyle.Bold, TextAnchor.MiddleCenter, Texto, false);
            TituloGrande = Etiqueta(raiz, 30, FontStyle.Bold, TextAnchor.MiddleCenter, Oro, false);
            Subtitulo = Etiqueta(raiz, 14, FontStyle.Italic, TextAnchor.MiddleCenter, TextoTenue, true);

            Boton = new GUIStyle(GUI.skin.button) { fontSize = F(14), fontStyle = FontStyle.Bold };
            Boton.normal.textColor = Texto;
            Boton.hover.textColor = Oro;
            Boton.active.textColor = Oro;

            Campo = new GUIStyle(GUI.skin.textField) { fontSize = F(14) };
            Campo.normal.textColor = Texto;
        }

        private static GUIStyle Etiqueta(GUIStyle raiz, int tam, FontStyle fuente, TextAnchor anclaje, Color color, bool ajustar)
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
            s.normal.textColor = color;
            return s;
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

        /// <summary>Chip flotante centrado en una posición de PANTALLA IMGUI (origen arriba-izquierda), recortado a los bordes.</summary>
        public static void Globo(Vector2 centroGui, string texto, Color color)
        {
            Preparar();
            if (Chip == null || string.IsNullOrEmpty(texto)) return; // defensivo: fuera de OnGUI no hay estilos.

            float w = Ancho(Chip, texto) + S(16f);
            float h = Chip.lineHeight + S(10f);
            float x = Mathf.Clamp(centroGui.x - w * 0.5f, S(4f), Mathf.Max(S(4f), Screen.width - w - S(4f)));
            float y = Mathf.Clamp(centroGui.y - h * 0.5f, S(4f), Mathf.Max(S(4f), Screen.height - h - S(4f)));
            var r = new Rect(x, y, w, h);

            Panel(r, TintaFuerte, new Color(color.r, color.g, color.b, 0.55f));
            var previo = Chip.normal.textColor;
            Chip.normal.textColor = color;
            GUI.Label(r, texto, Chip);
            Chip.normal.textColor = previo;
        }

        /// <summary>
        /// Etiqueta anclada a un punto del MUNDO (máquinas, Tolva): se convierte
        /// a pantalla, se centra sobre el objeto y se desplaza `subirPx` píxeles
        /// hacia arriba. Devuelve sin dibujar si no hay cámara o queda detrás.
        /// </summary>
        public static void EtiquetaMundo(Vector3 posicionMundo, string texto, Color color, float subirPx)
        {
            var cam = Camera.main;
            if (cam == null) return;

            Vector3 s = cam.WorldToScreenPoint(posicionMundo);
            if (s.z <= 0f) return;

            // Mouse/cámara usan origen abajo-izquierda; IMGUI arriba-izquierda.
            Globo(new Vector2(s.x, Screen.height - s.y - subirPx), texto, color);
        }

        /// <summary>
        /// RÓTULO FIJO DE APARATO. Pequeño, sin fondo opaco de aviso, anclado a
        /// un punto del mundo con un desplazamiento en píxeles (positivo = hacia
        /// arriba, NEGATIVO = hacia abajo) y SIN recortarse contra los bordes
        /// verticales de la pantalla.
        ///
        /// POR QUÉ EXISTE (playtest 4: "el label de las placas tapa la
        /// interacción al aspirar"): <see cref="Globo"/> centra su chip sobre el
        /// objeto y lo empuja dentro de la pantalla si se sale, así que el
        /// rótulo de una placa —que vive en el FONDO de una cuba, justo donde el
        /// jugador aspira— acababa bajo el cursor. Estos rótulos son chapas
        /// atornilladas al aparato: van SIEMPRE en el mismo sitio relativo a él
        /// (típicamente por debajo, sobre la piedra, fuera de la zona de trabajo),
        /// son diminutos y no se mueven nunca.
        /// </summary>
        public static void PlacaMundo(Vector3 posicionMundo, string texto, Color color, float desplazarPx)
        {
            Preparar();
            var cam = Camera.main;
            if (cam == null || ChipMini == null || string.IsNullOrEmpty(texto)) return;

            Vector3 s = cam.WorldToScreenPoint(posicionMundo);
            if (s.z <= 0f) return;

            float w = Ancho(ChipMini, texto) + S(10f);
            float h = ChipMini.lineHeight + S(6f);
            // Solo se acota en X (para que una chapa junto al muro derecho siga
            // siendo legible); en Y NUNCA se mueve: si el aparato está fuera de
            // cuadro su chapa también debe estarlo.
            float x = Mathf.Clamp(s.x - w * 0.5f, S(2f), Mathf.Max(S(2f), Screen.width - w - S(2f)));
            float y = Screen.height - s.y - desplazarPx - h * 0.5f;
            var r = new Rect(x, y, w, h);

            Panel(r, TintaFuerte, new Color(color.r, color.g, color.b, 0.45f));
            var previo = ChipMini.normal.textColor;
            ChipMini.normal.textColor = color;
            GUI.Label(r, texto, ChipMini);
            ChipMini.normal.textColor = previo;
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
