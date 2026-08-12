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
    /// </summary>
    public sealed class FlaskHud : MonoBehaviour
    {
        private const int MaxSwatches = 4;
        private const string TextoAyuda = "clic izq. aspirar · clic der. verter · Q vaciar";

        private AlkahestSim _sim;
        private Flask _flask;
        private SubstanceKnowledge _knowledge;

        // Buffers reutilizados cada frame para no asignar memoria al calcular el top-4.
        private readonly byte[] _topIds = new byte[MaxSwatches];
        private readonly int[] _topCounts = new int[MaxSwatches];

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Flask flask, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _flask = flask;
            _knowledge = knowledge;
        }

        private void OnGUI()
        {
            if (_sim == null || _flask == null || _sim.Universe == null) return;
            if (DayCycle.InputLocked) return; // bajo los overlays de jornada el HUD estorba.

            UiStyles.Preparar();
            ComputeTopContents();
            DibujarPanel();
            DibujarReticulaYFeedback();
        }

        // -----------------------------------------------------------------
        // Panel compacto arriba-izquierda.
        // -----------------------------------------------------------------
        private void DibujarPanel()
        {
            float margen = UiStyles.S(10f);
            float pad = UiStyles.S(8f);
            float ancho = UiStyles.S(300f);
            float altoLinea = UiStyles.S(19f);
            float altoBarra = UiStyles.S(13f);
            float altoFila = UiStyles.S(16f);
            // La línea de ayuda se MIDE (podría necesitar dos líneas a resoluciones
            // raras): así el panel siempre la contiene entera.
            float altoAyuda = UiStyles.Alto(UiStyles.CuerpoTenue, TextoAyuda, ancho - pad * 2f);

            int filas = 0;
            for (int i = 0; i < MaxSwatches; i++) if (_topCounts[i] > 0) filas++;

            float alto = pad + altoLinea + UiStyles.S(4f) + altoBarra
                       + (filas > 0 ? UiStyles.S(6f) + filas * altoFila : 0f)
                       + UiStyles.S(6f) + altoAyuda + pad;

            var panel = new Rect(margen, margen, ancho, alto);

            // (fix playtest 7) Cuando un aviso ya "aprendido" no se muestra como
            // texto (ver Flask.SetFeedback), la acción fallida sigue teniendo
            // ALGUNA respuesta: el borde del panel destella un instante en
            // UiStyles.Aviso en vez de quedarse mudo del todo — "hice clic y no
            // pasó nada" seguiría sintiendo el juego roto.
            float destello = _flask.DestelloIntensidad;
            Color borde = destello > 0f ? Color.Lerp(UiStyles.Borde, UiStyles.Aviso, destello) : UiStyles.Borde;
            UiStyles.Panel(panel, UiStyles.Tinta, borde);

            float x = panel.x + pad;
            float y = panel.y + pad;
            float anchoInterior = ancho - pad * 2f;

            int total = _flask.Total;
            GUI.Label(new Rect(x, y, anchoInterior * 0.5f, altoLinea), "FRASCO", UiStyles.Titulo);
            GUI.Label(new Rect(x + anchoInterior * 0.4f, y, anchoInterior * 0.6f, altoLinea),
                total + " / " + Flask.Capacity, UiStyles.Numero);
            y += altoLinea + UiStyles.S(4f);

            // La barra se tiñe con la MEZCLA de lo que llevas: de un vistazo sabes
            // si cargas agua, aceite o el mejunje verde de turno.
            UiStyles.Barra(new Rect(x, y, anchoInterior, altoBarra),
                (float)total / Flask.Capacity, ColorMezcla(total));
            y += altoBarra;

            if (filas > 0)
            {
                y += UiStyles.S(6f);
                float lado = altoFila - UiStyles.S(4f);
                for (int i = 0; i < MaxSwatches; i++)
                {
                    if (_topCounts[i] <= 0) continue;
                    var def = _sim.Universe.Get(_topIds[i]);

                    UiStyles.Rellenar(new Rect(x, y + UiStyles.S(2f), lado, lado), def.baseColor);

                    float xTexto = x + lado + UiStyles.S(6f);
                    GUI.Label(new Rect(xTexto, y, anchoInterior - lado - UiStyles.S(60f), altoFila),
                        NombreDe(_topIds[i]), UiStyles.CuerpoLinea);
                    GUI.Label(new Rect(x, y, anchoInterior, altoFila),
                        _topCounts[i].ToString(), UiStyles.CuerpoDer);
                    y += altoFila;
                }
            }

            y += UiStyles.S(6f);
            GUI.Label(new Rect(x, y, anchoInterior, altoAyuda), TextoAyuda, UiStyles.CuerpoTenue);
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
    }
}
