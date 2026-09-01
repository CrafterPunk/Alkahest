using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alkahest.Game
{
    /// <summary>
    /// (R120) EL ATRIL DE EMOTES POR ACORDES — estilo quick-chat de Rocket League,
    /// pedido de Cesar: nada radial; números. Pulsas un dígito (1-4) y aparece
    /// unos segundos, discreta, la lista de ese grupo ("1 Reposo · 2 Caminar ·
    /// 3 Recoger · 4 Flotar"); pulsas el segundo dígito y el muñeco lo hace.
    /// Dos teclas, hasta 16 gestos, y con el tiempo se memorizan.
    ///
    /// Es la CAPA 1 del plan (PLAN_ANIMACION_R119 §5): herramienta de prueba
    /// con el juego de fondo real. Los grupos se arman SOLOS con toda hoja que
    /// exista en Resources/Personaje/Anim (por sus manifiestos): copiar una
    /// hoja nueva del arnés = aparece en el atril. La capa 2 (red, acordes
    /// sociales, ritual del maestro) se vestirá encima después de las dos
    /// máquinas; esta clase no se tira, se extiende.
    ///
    /// Solo el jugador local, a pie y sin UI de texto delante (regla 12).
    /// </summary>
    public sealed class AtrilDeEmotes : MonoBehaviour
    {
        private const float VentanaSeg = 2.6f;      // cuánto queda abierto el grupo esperando el segundo dígito
        private const float AvisoSeg = 1.4f;        // cuánto se muestra "▶ Gesto" tras dispararlo
        private const int PorGrupo = 4;

        private ApprenticeController _aprendiz;
        private readonly List<string> _nombres = new List<string>();
        private readonly Dictionary<string, HojaDeCuadros> _hojas = new Dictionary<string, HojaDeCuadros>();
        private readonly Dictionary<string, string> _etiquetas = new Dictionary<string, string>();

        private int _grupoAbierto = -1;   // -1 cerrado
        private float _abiertoHasta;
        private string _aviso;
        private float _avisoHasta;
        private GUIStyle _estilo, _estiloTenue;
        private string[] _lineasGrupo;    // cacheadas al abrir (OnGUI no concatena por frame)

        public static AtrilDeEmotes Crear(ApprenticeController aprendiz)
        {
            var a = aprendiz.gameObject.AddComponent<AtrilDeEmotes>();
            a._aprendiz = aprendiz;
            a.Descubrir();
            return a;
        }

        /// <summary>Enumera los manifiestos de Resources/Personaje/Anim (sin cargar texturas todavía).</summary>
        private void Descubrir()
        {
            _nombres.Clear();
            var textos = Resources.LoadAll<TextAsset>("Personaje/Anim");
            foreach (var t in textos)
            {
                if (t == null || !t.name.EndsWith("_manifiesto")) continue;
                string nombre = t.name.Substring(0, t.name.Length - "_manifiesto".Length);
                _nombres.Add(nombre);
                _etiquetas[nombre] = Etiqueta(nombre, t.text);
            }
            _nombres.Sort(System.StringComparer.Ordinal);
            // Orden con sentido: los del prólogo primero, el resto alfabético.
            string[] primero = { "reposo", "caminar", "recoger", "flotar" };
            for (int i = primero.Length - 1; i >= 0; i--)
            {
                int k = _nombres.IndexOf(primero[i]);
                if (k > 0) { _nombres.RemoveAt(k); _nombres.Insert(0, primero[i]); }
            }
            Debug.Log($"[TenThousandYears] Atril de emotes: {_nombres.Count} gestos ({string.Join(", ", _nombres)}).");
        }

        private static string Etiqueta(string nombre, string json)
        {
            // el manifiesto puede traer "etiqueta": "Media vuelta"; si no, el nombre capitalizado.
            int i = json.IndexOf("\"etiqueta\"", System.StringComparison.Ordinal);
            if (i >= 0)
            {
                int a = json.IndexOf('"', json.IndexOf(':', i) + 1);
                int b = a >= 0 ? json.IndexOf('"', a + 1) : -1;
                if (a >= 0 && b > a) return json.Substring(a + 1, b - a - 1);
            }
            return nombre.Length == 0 ? nombre : char.ToUpperInvariant(nombre[0]) + nombre.Substring(1);
        }

        private HojaDeCuadros Hoja(string nombre)
        {
            if (!_hojas.TryGetValue(nombre, out var h))
            {
                h = HojaDeCuadros.Cargar(nombre);
                _hojas[nombre] = h;
            }
            return h;
        }

        private int Grupos => (_nombres.Count + PorGrupo - 1) / PorGrupo;

        private void Update()
        {
            if (_aprendiz == null || !_aprendiz.ControlDelJugador) return;
            var kb = Keyboard.current;
            if (kb == null) return;
            if (UiStyles.EscribiendoTexto || JournalHud.Abierto || AlbumReal.Abierto || DayCycle.InputLocked || Mudanza.ModoActivo)
            {
                _grupoAbierto = -1;
                return;
            }
            if (_grupoAbierto >= 0 && Time.unscaledTime > _abiertoHasta) _grupoAbierto = -1;

            int digito = DigitoPulsado(kb);
            if (digito < 0)
            {
                if (_grupoAbierto >= 0 && kb.escapeKey.wasPressedThisFrame) _grupoAbierto = -1;
                return;
            }

            if (_grupoAbierto < 0)
            {
                if (digito - 1 < Grupos) AbrirGrupo(digito - 1);
                return;
            }

            int idx = _grupoAbierto * PorGrupo + (digito - 1);
            _grupoAbierto = -1;
            if (idx < 0 || idx >= _nombres.Count) return;
            Disparar(_nombres[idx]);
        }

        private static int DigitoPulsado(Keyboard kb)
        {
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return 1;
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return 2;
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return 3;
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) return 4;
            return -1;
        }

        private void AbrirGrupo(int g)
        {
            _grupoAbierto = g;
            _abiertoHasta = Time.unscaledTime + VentanaSeg;
            int n = Mathf.Min(PorGrupo, _nombres.Count - g * PorGrupo);
            _lineasGrupo = new string[n + 1];
            _lineasGrupo[0] = Grupos > 1 ? $"emotes {g + 1}/{Grupos}" : "emotes";
            for (int i = 0; i < n; i++)
                _lineasGrupo[i + 1] = $"{i + 1}  {_etiquetas[_nombres[g * PorGrupo + i]]}";
        }

        private void Disparar(string nombre)
        {
            var h = Hoja(nombre);
            if (h == null) { Aviso($"{_etiquetas[nombre]}: sin hoja"); return; }
            if (!_aprendiz.EnSuelo) { Aviso("los gestos, a pie"); return; }
            bool ok = _aprendiz.ReproducirGesto(h, h.Loop ? 2.2f : 0f);
            Aviso(ok ? "▶ " + _etiquetas[nombre] : "ocupado");
        }

        private void Aviso(string texto)
        {
            _aviso = texto;
            _avisoHasta = Time.unscaledTime + AvisoSeg;
        }

        private void PrepararEstilos()
        {
            if (_estilo != null) return;
            UiStyles.Preparar();
            _estilo = new GUIStyle(UiStyles.Cuerpo) { fontSize = UiStyles.F(15), alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold };
            _estilo.normal.textColor = new Color(0.96f, 0.93f, 0.86f, 1f);
            _estiloTenue = new GUIStyle(_estilo) { fontStyle = FontStyle.Normal, fontSize = UiStyles.F(12) };
            _estiloTenue.normal.textColor = new Color(0.80f, 0.76f, 0.68f, 1f);
        }

        private void OnGUI()
        {
            bool grupo = _grupoAbierto >= 0 && _lineasGrupo != null;
            bool aviso = _aviso != null && Time.unscaledTime < _avisoHasta;
            if (!grupo && !aviso) return;
            if (DayCycle.InputLocked) return;
            PrepararEstilos();
            GUI.depth = 7;

            // Arriba a la izquierda, discreto (Cesar: "letras en pantalla que no distraen mucho").
            float x = UiStyles.S(18f), y = UiStyles.S(18f);
            float lineaH = UiStyles.S(20f), pad = UiStyles.S(8f);
            var blanco = Texture2D.whiteTexture;
            var prev = GUI.color;

            if (grupo)
            {
                float w = 0f;
                for (int i = 0; i < _lineasGrupo.Length; i++) w = Mathf.Max(w, UiStyles.Ancho(i == 0 ? _estiloTenue : _estilo, _lineasGrupo[i]));
                float h = lineaH * _lineasGrupo.Length + pad * 2f;
                float resto = Mathf.Clamp01((_abiertoHasta - Time.unscaledTime) / VentanaSeg);
                GUI.color = new Color(0.06f, 0.05f, 0.04f, 0.72f);
                GUI.DrawTexture(new Rect(x, y, w + pad * 2f, h), blanco);
                GUI.color = new Color(0.85f, 0.70f, 0.35f, 0.9f);
                GUI.DrawTexture(new Rect(x, y + h - UiStyles.S(2f), (w + pad * 2f) * resto, UiStyles.S(2f)), blanco); // la ventana que se agota
                GUI.color = prev;
                for (int i = 0; i < _lineasGrupo.Length; i++)
                    GUI.Label(new Rect(x + pad, y + pad + i * lineaH, w, lineaH), _lineasGrupo[i], i == 0 ? _estiloTenue : _estilo);
                y += h + UiStyles.S(6f);
            }
            if (aviso)
            {
                float w = UiStyles.Ancho(_estilo, _aviso) + pad * 2f;
                float a = Mathf.Clamp01((_avisoHasta - Time.unscaledTime) / 0.4f);
                GUI.color = new Color(0.06f, 0.05f, 0.04f, 0.72f * a);
                GUI.DrawTexture(new Rect(x, y, w, lineaH + pad), blanco);
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(x + pad, y + pad * 0.5f, w, lineaH), _aviso, _estilo);
                GUI.color = prev;
            }
        }
    }
}
