using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 73, el prólogo rehecho — espec de Cesar, literal) EL TUTORIAL
    /// CONTEXTUAL: "feedback funcional del juego, no la voz del Maestro. Más
    /// liviano y más cercano al área de atención/jugador. Visualmente
    /// distinto del texto narrativo."
    ///
    /// Una fila de FICHAS DE TECLA en blanco opaco, flotando bajo el
    /// aprendiz, con una leyenda corta ("Muévete"). Reglas de la espec:
    ///  · Cuando el jugador PULSA una tecla, esa ficha se ilumina (estado
    ///    físico, lo lee este widget).
    ///  · Cuando el jugador CUMPLE la acción (resultado REAL validado por
    ///    quien dirige — desplazamiento medido, materia realmente aspirada —
    ///    nunca la mera pulsación), quien dirige llama <see cref="Confirmar"/>:
    ///    la ficha queda ENCENDIDA con un destello y un blip discreto.
    ///  · Con todas confirmadas: pequeño feedback positivo (pulso + blip un
    ///    pelo más agudo), ~0.5 s en pantalla, fade out suave.
    ///  La intención es "Sí. Eso era." — JAMÁS "objetivo 1/7": aquí no hay
    ///  contadores, ni checklist, ni texto de progreso.
    ///
    /// El widget es tonto a propósito: valida QUIEN LO USA (el director del
    /// prólogo hoy; cualquier otro mañana). Aquí solo viven el dibujo, los
    /// estados y el blip.
    /// </summary>
    public sealed class TutorialContextual : MonoBehaviour
    {
        // ---- Los números del gusto (afinables a ojo) ----
        private const float FadeInSeg = 0.28f;
        private const float FadeOutSeg = 0.45f;
        private const float HoldTrasCompletarSeg = 0.55f; // "permanece aproximadamente 0,5 s".
        private const float DestelloSeg = 0.5f;           // vida del flash de una confirmación.
        private const float BlipVolumen = 0.5f;
        private const float BlipPitchFinal = 1.22f;       // la confirmación del GRUPO sube un pelo.

        // Blanco opaco que se ilumina (mandato de Cesar):
        private static readonly Color FichaFondo = new Color(0.93f, 0.93f, 0.90f, 0.62f);
        private static readonly Color FichaPulsada = new Color(1f, 1f, 1f, 0.92f);
        private static readonly Color FichaConfirmada = new Color(1f, 1f, 0.97f, 0.97f);
        private static readonly Color FichaBorde = new Color(0.22f, 0.20f, 0.18f, 0.85f);
        private static readonly Color FichaTexto = new Color(0.13f, 0.12f, 0.11f, 1f);
        private static readonly Color LeyendaColor = new Color(0.95f, 0.94f, 0.90f, 0.95f);

        public sealed class Paso
        {
            public string Etiqueta;                 // "W", "CLIC IZQ", ...
            public System.Func<bool> Presionada;    // estado FÍSICO (solo ilumina; jamás confirma).
            public bool Confirmada;
            public float Destello;                  // 1 -> 0 tras confirmar.
        }

        private Transform _aprendiz;
        private Paso[] _pasos;
        private string _leyenda;
        private float _alfa;            // fade global.
        private bool _completado;
        private float _holdRestante;
        private AudioSource _voz;

        private static GUIStyle _ficha, _leyendaStyle;
        private static int _stylesAlto;

        /// <summary>Sigue vivo en pantalla (mostrándose o desvaneciéndose).</summary>
        public bool Visible => _pasos != null;
        /// <summary>Todas confirmadas Y el fade-out terminó: quien dirige puede avanzar.</summary>
        public bool Terminado => _pasos == null && _completado;

        public void Init(Transform aprendiz, float offsetPx = 64f)
        {
            _aprendiz = aprendiz;
            _offsetPx = offsetPx; // (R75) del guion (asset): dónde flotan las fichas respecto al jugador.
            _voz = gameObject.AddComponent<AudioSource>();
            _voz.playOnAwake = false;
            _voz.spatialBlend = 0f;
        }
        private float _offsetPx = 64f;

        /// <summary>Enseña una fila nueva de fichas. Resetea cualquier grupo anterior.</summary>
        public void Mostrar(string leyenda, params Paso[] pasos)
        {
            _leyenda = string.IsNullOrEmpty(leyenda) ? null : "— " + leyenda; // cacheado aquí: OnGUI no concatena por frame.
            _pasos = pasos;
            _completado = false;
            _holdRestante = 0f;
            _alfa = 0f;
        }

        /// <summary>La acción REAL se cumplió (lo valida quien dirige): la ficha queda encendida.</summary>
        public void Confirmar(int idx)
        {
            if (_pasos == null || idx < 0 || idx >= _pasos.Length) return;
            var p = _pasos[idx];
            if (p.Confirmada) return;
            p.Confirmada = true;
            p.Destello = 1f;

            bool todas = true;
            for (int i = 0; i < _pasos.Length; i++) todas &= _pasos[i].Confirmada;
            Blip(todas ? BlipPitchFinal : 1f);
            if (todas)
            {
                _completado = true;
                _holdRestante = HoldTrasCompletarSeg;
                for (int i = 0; i < _pasos.Length; i++) _pasos[i].Destello = 1f; // el pulso positivo del grupo.
            }
        }

        public void Ocultar()
        {
            _pasos = null;
            _completado = false;
        }

        /// <summary>
        /// (R79b) Retirada SUAVE sin celebración: entra al fade-out normal
        /// pero SIN blip ni destello — para fichas-recuerdo que caducan solas
        /// ("reaparece una vez, discreta, y se va sola", Cesar). Ocultar()
        /// corta en seco; esto respira. Nota: deja Terminado en true, igual
        /// que un grupo completado — quien lo use como señal de avance debe
        /// consumirla antes (hoy solo el Despertar la lee, mucho antes).
        /// </summary>
        public void Desvanecer()
        {
            if (_pasos == null) return;
            _completado = true;
            _holdRestante = 0f; // directo al fade-out, sin el hold de la celebración.
        }

        private void Blip(float pitch)
        {
            if (_voz == null) return;
            _voz.pitch = pitch;
            _voz.PlayOneShot(Audio.SintetizadorSfx.TutorialConfirma,
                BlipVolumen * Audio.DirectorDeAudio.VolumenEfectos);
        }

        private void Update()
        {
            if (_pasos == null) return;
            if (DayCycle.InputLocked) return; // (revisión Opus 73 #12) la pausa no se come el hold ni el fade de la confirmación.
            float dt = Time.deltaTime;

            for (int i = 0; i < _pasos.Length; i++)
                if (_pasos[i].Destello > 0f) _pasos[i].Destello = Mathf.Max(0f, _pasos[i].Destello - dt / DestelloSeg);

            if (_completado)
            {
                if (_holdRestante > 0f) { _holdRestante -= dt; _alfa = Mathf.Min(1f, _alfa + dt / FadeInSeg); }
                else
                {
                    _alfa -= dt / FadeOutSeg;
                    if (_alfa <= 0f) _pasos = null; // Terminado (la bandera _completado queda alta hasta el próximo Mostrar).
                }
            }
            else
            {
                _alfa = Mathf.Min(1f, _alfa + dt / FadeInSeg);
            }
        }

        private static void PrepararEstilos()
        {
            if (_ficha != null && _stylesAlto == Screen.height) return;
            _stylesAlto = Screen.height;
            UiStyles.Preparar();
            float h = Screen.height;
            _ficha = new GUIStyle(UiStyles.Cuerpo)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = Mathf.RoundToInt(h * 0.019f),
            };
            _ficha.normal.textColor = FichaTexto;
            _leyendaStyle = new GUIStyle(UiStyles.Cuerpo)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                // (R116, Cesar: "las letras blancas tienen que ser más grandes
                // y legibles... que se mueva con él pero con algún borde") Un
                // tercio más grandes; la placa oscura va en OnGUI.
                fontSize = Mathf.RoundToInt(h * 0.026f),
            };
            _leyendaStyle.normal.textColor = LeyendaColor;
        }

        private void OnGUI()
        {
            if (_pasos == null || _aprendiz == null || _alfa <= 0f) return;
            if (DayCycle.InputLocked) return; // con el menú de pausa delante, las fichas esperan.
            var cam = Camera.main;
            if (cam == null) return;

            PrepararEstilos();
            GUI.depth = 8; // por delante de la viñeta del prólogo (50), por detrás de los paneles modales (0).

            // Anclado BAJO el aprendiz ("más cercano al área de atención"),
            // con abrazadera a los bordes para que nunca salga de pantalla.
            Vector3 sp = cam.WorldToScreenPoint(_aprendiz.position);
            float cx = sp.x;
            float cy = Screen.height - sp.y + UiStyles.S(_offsetPx);

            float fichaH = UiStyles.S(24f);
            float pad = UiStyles.S(9f);
            float gap = UiStyles.S(5f);

            // Medir ancho total: fichas + leyenda (UiStyles.Ancho reutiliza un
            // GUIContent interno — revisión Opus 73 #18: cero asignaciones por
            // frame, como el resto de los HUD del proyecto).
            float total = 0f;
            for (int i = 0; i < _pasos.Length; i++)
                total += UiStyles.Ancho(_ficha, _pasos[i].Etiqueta) + pad * 2f + (i > 0 ? gap : 0f);
            float leyendaW = _leyenda == null ? 0f : UiStyles.Ancho(_leyendaStyle, _leyenda) + gap * 2f;
            total += leyendaW;

            float x = Mathf.Clamp(cx - total * 0.5f, UiStyles.S(12f), Screen.width - total - UiStyles.S(12f));
            float y = Mathf.Clamp(cy, UiStyles.S(12f), Screen.height - fichaH - UiStyles.S(12f));

            var blanco = Texture2D.whiteTexture;
            var prev = GUI.color;

            for (int i = 0; i < _pasos.Length; i++)
            {
                var p = _pasos[i];
                float w = UiStyles.Ancho(_ficha, p.Etiqueta) + pad * 2f;
                var r = new Rect(x, y, w, fichaH);

                bool pulsada = p.Presionada != null && p.Presionada();
                Color fondo = p.Confirmada ? FichaConfirmada : (pulsada ? FichaPulsada : FichaFondo);

                // El DESTELLO de la confirmación: un halo blanco que se
                // expande y muere (el "sí. eso era.").
                if (p.Destello > 0f)
                {
                    float d = 1f - p.Destello;
                    float grow = UiStyles.S(3f) + d * UiStyles.S(6f);
                    GUI.color = new Color(1f, 1f, 1f, 0.55f * p.Destello * _alfa);
                    GUI.DrawTexture(new Rect(r.x - grow, r.y - grow, r.width + grow * 2f, r.height + grow * 2f), blanco);
                }

                GUI.color = new Color(fondo.r, fondo.g, fondo.b, fondo.a * _alfa);
                GUI.DrawTexture(r, blanco);
                GUI.color = new Color(FichaBorde.r, FichaBorde.g, FichaBorde.b, FichaBorde.a * _alfa);
                GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), blanco);
                GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), blanco);
                GUI.DrawTexture(new Rect(r.x, r.y, 1f, r.height), blanco);
                GUI.DrawTexture(new Rect(r.xMax - 1f, r.y, 1f, r.height), blanco);

                GUI.color = new Color(1f, 1f, 1f, _alfa);
                GUI.Label(r, p.Etiqueta, _ficha);

                x += w + gap;
            }

            if (leyendaW > 0f)
            {
                // (R116) LA PLACA DE LA LEYENDA: el texto blanco flotaba
                // desnudo y se perdía según el fondo. Ahora viaja con el
                // muñeco sobre su propia placa casi negra (la opción limpia:
                // legible en CUALQUIER parte sin regalarle a la UI una zona
                // fija de pantalla).
                var rl = new Rect(x + gap, y, leyendaW, fichaH);
                GUI.color = new Color(0.04f, 0.04f, 0.05f, 0.78f * _alfa);
                GUI.DrawTexture(new Rect(rl.x - UiStyles.S(4f), rl.y, rl.width + UiStyles.S(8f), rl.height), blanco);
                GUI.color = new Color(1f, 1f, 1f, _alfa);
                GUI.Label(rl, _leyenda, _leyendaStyle);
            }

            GUI.color = prev;
        }
    }
}
