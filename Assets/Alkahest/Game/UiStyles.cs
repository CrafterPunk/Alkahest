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
