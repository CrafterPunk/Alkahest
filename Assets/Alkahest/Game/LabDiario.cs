using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (R143, H7) EL DIARIO DE SESIÓN.
    ///
    /// H7 pide observar a alguien jugar y anotar veinte cosas por sesión: cuánto tardó en
    /// descubrir cada máquina, cuántas veces se atascó, qué pestañas abrió, qué pintó, cuánto
    /// anduvo, cuándo aceleró el tiempo. Nada de eso se puede recordar con honestidad después, y
    /// transcribirlo de un vídeo cuesta más que jugarlo.
    ///
    /// Así que el juego lo anota solo. El reparto es: **la máquina mide, la persona interpreta.**
    /// Lo objetivo —ticks, contadores, teclas, distancia— sale aquí sin que nadie tenga que
    /// acordarse; lo subjetivo —qué creías que iba a pasar, por qué te rendiste— lo dice el
    /// jugador en voz alta sobre el vídeo, y estas marcas le ponen la hora exacta para no tener
    /// que buscarlo.
    ///
    /// LAS TECLAS (F1/F2/F4 estaban libres; M y N ya son del audio y del mundo):
    ///  · F9  abre y cierra la sesión. Al abrir da una CLAQUETA —rótulo en pantalla con la hora—
    ///        para que el vídeo y el diario se puedan alinear después sin adivinar.
    ///  · F1  «¡anda!»  (S de sorpresa: algo salió mejor o más raro de lo esperado)
    ///  · F2  «¿por qué?» (C de confusión: algo pasó y no se entiende)
    ///  · F4  nota suelta (I de intervención, o cualquier cosa que merezca una marca)
    /// Cada marca guarda tick, reloj de sesión, posición y una CAPTURA, y lo anota en el archivo.
    ///
    /// EL ARCHIVO: `Laboratorio/h7/sesion_AAAA-MM-DD_HHMM.md`, en Markdown, con la tabla que pide
    /// el protocolo ya montada al cerrar. Se escribe línea a línea (append) para que un cierre a
    /// lo bruto —cerrar el juego con la X— no se lleve la sesión por delante.
    /// </summary>
    public sealed class LabDiario : MonoBehaviour
    {
        public static LabDiario Instancia { get; private set; }

        private AlkahestSim _sim;
        private Transform _jugador;

        private bool _abierta;
        private string _ruta;
        private float _t0;
        private uint _tick0;
        private int _marcasS, _marcasC, _marcasI, _marcasTotales;
        private readonly StringBuilder _sb = new StringBuilder(256);

        // ---- lo que se vigila para anotarlo solo ----
        private float _proximoMuestreo;
        private const float IntervaloMuestreo = 1f;   // 1 Hz: de sobra para todo esto y coste nulo.
        private int _multiplicadorAnterior = -1;
        private string _pestanaAnterior = "";
        private int _pincelAnterior = -2;
        private Vector3 _posAnterior;
        private float _distanciaCeldas;
        private int _teletransportes;
        /// <summary>
        /// (R145, R23-3) La LÍNEA BASE al pulsar F9. Los hitos comparaban el contador contra CERO,
        /// y en el nivel de referencia el hogar ya está quemando fibra cuando el jugador aparece:
        /// «PRIMER FUEGO» saltaba en el segundo 1 de toda sesión, midiendo el mundo en vez de al
        /// jugador. Lo enseñó mi propia sesión de prueba y no lo vi.
        /// </summary>
        private long _baseVidrio, _baseCarbon, _basePlantas, _baseGoteos, _baseCocido, _baseQuemado;
        private bool _hitoVidrio, _hitoCarbon, _hitoPlanta, _hitoGoteo, _hitoCocido, _hitoFuego;
        private float _avisoHasta;
        private string _avisoTexto = "";
        private GUIStyle _estiloAviso;

        public static void Crear(AlkahestSim sim, Transform jugador)
        {
            var go = new GameObject("LabDiario");
            var d = go.AddComponent<LabDiario>();
            d._sim = sim;
            d._jugador = jugador;
            d._posAnterior = jugador != null ? jugador.position : Vector3.zero;
            Instancia = d;
        }

        private void OnDestroy() { if (_abierta) Cerrar(); if (Instancia == this) Instancia = null; }

        // =================================================================
        // TECLAS
        // =================================================================
        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            // (R12) Mismo contrato que el resto de atajos del mundo.
            if (UiStyles.EscribiendoTexto || JournalHud.Abierto || AlbumReal.Abierto) return;

            if (kb.f9Key.wasPressedThisFrame) { if (_abierta) Cerrar(); else Abrir(); }
            if (!_abierta) return;
            if (kb.f1Key.wasPressedThisFrame) Marcar("S", "¡anda!");
            if (kb.f2Key.wasPressedThisFrame) Marcar("C", "¿por qué?");
            if (kb.f4Key.wasPressedThisFrame) Marcar("I", "nota");

            if (_jugador != null)
            {
                // (R145, R23-8) Un salto grande en un cuadro no es andar: es un teletransporte
                // (Ctrl+1..6) o el reposicionado del arranque. Sumarlo inflaba «distancia
                // recorrida», que en el informe se lee como cuánto EXPLORÓ el jugador.
                float d = Vector3.Distance(_jugador.position, _posAnterior) / SimRenderer.CellWorldSize;
                if (d > 20f) { _teletransportes++; Anotar("teletransporte (" + Mathf.RoundToInt(d) + " celdas de salto)"); }
                else _distanciaCeldas += d;
                _posAnterior = _jugador.position;
            }

            _proximoMuestreo -= Time.deltaTime;
            if (_proximoMuestreo <= 0f) { _proximoMuestreo = IntervaloMuestreo; Muestrear(); }
        }

        // =================================================================
        // ABRIR / CERRAR
        // =================================================================
        private void Abrir()
        {
            var ahora = System.DateTime.Now;
            string carpeta = CarpetaH7();
            try { Directory.CreateDirectory(carpeta); }
            catch (System.Exception e)
            {
                // (R145, R23-6) Antes esto fallaba EN SILENCIO: F9 no hacía nada visible y la
                // sesión se jugaba entera creyendo que se estaba grabando.
                Aviso("NO SE PUEDE ESCRIBIR EL DIARIO en " + carpeta, 10f);
                Debug.LogError("[TenThousandYears] Diario: no se pudo crear la carpeta: " + e.Message);
                return;
            }
            // Segundos en el nombre, y sufijo si ya existe: con resolución de minuto, dos F9
            // seguidos se pisaban la sesión anterior (WriteAllText trunca).
            string bas = "sesion_" + ahora.ToString("yyyy-MM-dd_HHmmss", CultureInfo.InvariantCulture);
            _ruta = Path.Combine(carpeta, bas + ".md");
            for (int n = 2; File.Exists(_ruta) && n < 100; n++) _ruta = Path.Combine(carpeta, bas + "_" + n + ".md");
            _t0 = Time.unscaledTime;
            _tick0 = _sim != null && _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            _abierta = true;
            _marcasS = _marcasC = _marcasI = _marcasTotales = 0;
            _distanciaCeldas = 0f; _teletransportes = 0;
            _hitoVidrio = _hitoCarbon = _hitoPlanta = _hitoGoteo = _hitoCocido = _hitoFuego = false;
            _multiplicadorAnterior = -1; _pestanaAnterior = ""; _pincelAnterior = -2;

            var stBase = _sim != null ? _sim.Stepper : null;
            _baseVidrio = stBase != null ? stBase.LabVidrio : 0;
            _baseCarbon = stBase != null ? stBase.LabCarbonizado : 0;
            _basePlantas = stBase != null ? stBase.LabPlantasNacidas : 0;
            _baseGoteos = stBase != null ? stBase.LabGoteos : 0;
            _baseCocido = stBase != null ? stBase.LabCocido : 0;
            _baseQuemado = stBase != null ? stBase.LabCombustibleQuemado : 0;

            var cab = new StringBuilder();
            cab.Append("# Sesión de H7 — ").Append(ahora.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).Append("\n\n");
            cab.Append("Registro automático de `Game/LabDiario.cs`. La máquina anota lo objetivo; lo que\n");
            cab.Append("pensaba el jugador va sobre el vídeo, alineado con la CLAQUETA de abajo.\n\n");
            cab.Append("**CLAQUETA · ").Append(ahora.ToString("HH:mm:ss", CultureInfo.InvariantCulture))
               .Append(" · tick ").Append(_tick0).Append("** — este instante es el 00:00 del diario.\n\n");
            cab.Append("| reloj | tick | qué pasó |\n|---|---:|---|\n");
            try { File.WriteAllText(_ruta, cab.ToString()); }
            catch (System.Exception e)
            {
                Aviso("NO SE PUEDE ESCRIBIR EL DIARIO: " + e.Message, 10f);
                Debug.LogError("[TenThousandYears] Diario: " + e);
                _abierta = false;
                return;
            }

            Aviso("SESIÓN ABIERTA · " + ahora.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " · F1 ¡anda! · F2 ¿por qué? · F4 nota", 6f);
            Anotar("comienza la sesión");
            Debug.Log("[TenThousandYears] Diario de sesión abierto: " + _ruta);
        }

        private void Cerrar()
        {
            if (!_abierta) return;
            var st = _sim != null ? _sim.Stepper : null;
            var sb = new StringBuilder();
            sb.Append("\n## Resumen\n\n| métrica | valor |\n|---|---|\n");
            sb.Append("| duración | ").Append(Reloj()).Append(" |\n");
            sb.Append("| ticks de mundo | ").Append(st != null ? (st.Tick - _tick0) : 0).Append(" |\n");
            sb.Append("| «¡anda!» (S) | ").Append(_marcasS).Append(" |\n");
            sb.Append("| «¿por qué?» (C) | ").Append(_marcasC).Append(" |\n");
            sb.Append("| notas / intervenciones (I) | ").Append(_marcasI).Append(" |\n");
            sb.Append("| distancia recorrida | ").Append(Mathf.RoundToInt(_distanciaCeldas)).Append(" celdas (")
              .Append((_distanciaCeldas / CellGrid.W).ToString("F1")).Append(" mundos de ancho) |\n");
            sb.Append("| teletransportes | ").Append(_teletransportes).Append(" |\n");
            if (st != null)
            {
                sb.Append("| goteos | ").Append(st.LabGoteos).Append(" |\n");
                sb.Append("| vidrio hecho | ").Append(st.LabVidrio).Append(" |\n");
                sb.Append("| celdas carbonizadas | ").Append(st.LabCarbonizado).Append(" |\n");
                sb.Append("| plantas nacidas / muertas | ").Append(st.LabPlantasNacidas).Append(" / ").Append(st.LabPlantasMuertas).Append(" |\n");
                sb.Append("| arcilla cocida | ").Append(st.LabCocido).Append(" |\n");
                sb.Append("| combustible quemado | ").Append(st.LabCombustibleQuemado).Append(" u |\n");
                sb.Append("| balance de agua | ").Append(st.LabBalanceU).Append(" u |\n");
            }
            sb.Append("\n*(Lo que falta de la tabla del protocolo —qué pensaba, dónde se atascó, qué\n");
            sb.Append("modelo mental tenía— va aquí a mano, con los tiempos de las marcas de arriba.)*\n");
            File.AppendAllText(_ruta, sb.ToString());

            Aviso("SESIÓN CERRADA · " + Path.GetFileName(_ruta), 5f);
            Debug.Log("[TenThousandYears] Diario cerrado: " + _ruta);
            _abierta = false;
        }

        // =================================================================
        // ANOTAR
        // =================================================================
        private string Reloj()
        {
            float t = Time.unscaledTime - _t0;
            return Mathf.FloorToInt(t / 60f).ToString("00") + ":" + Mathf.FloorToInt(t % 60f).ToString("00");
        }

        /// <summary>
        /// (R145, R23-6) La carpeta REAL. En el editor es la del repo; en una build,
        /// `Application.dataPath` es `Builds/<juego>_Data`, así que el diario cae junto al .exe y
        /// no donde prometía la guía. Se resuelve igual en los dos casos y la guía dice la verdad.
        /// </summary>
        public static string CarpetaH7()
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Laboratorio", "h7"));

        private void Anotar(string texto)
        {
            if (!_abierta) return;
            var st = _sim != null ? _sim.Stepper : null;
            _sb.Length = 0;
            _sb.Append("| ").Append(Reloj()).Append(" | ").Append(st != null ? (st.Tick - _tick0) : 0)
               .Append(" | ").Append(texto).Append(" |\n");
            try { File.AppendAllText(_ruta, _sb.ToString()); }
            catch (System.Exception e) { Debug.LogError("[TenThousandYears] Diario: " + e.Message); }
        }

        private void Marcar(string clase, string etiqueta)
        {
            if (clase == "S") _marcasS++; else if (clase == "C") _marcasC++; else _marcasI++;
            int cx = 0, cy = 0;
            if (_jugador != null)
            {
                cx = Mathf.RoundToInt(_jugador.position.x / SimRenderer.CellWorldSize);
                cy = Mathf.RoundToInt(_jugador.position.y / SimRenderer.CellWorldSize);
            }
            // (R143) Con el contador en el nombre: dos marcas en el mismo segundo se pisaban el
            // PNG, y en un momento intenso —que es justo cuando se marca— pasa de verdad.
            _marcasTotales++;
            string sello = _marcasTotales.ToString("00") + "_" + clase + "_" + Reloj().Replace(":", "");
            string nombre = "h7_" + sello + ".png";
            ScreenCapture.CaptureScreenshot(Path.Combine(CarpetaH7(), nombre));

            // (R145, R23-7) Y el SNAPSHOT del mundo, que es lo que pide el protocolo: una captura
            // enseña cómo se veía, un snapshot deja volver a ese estado y medirlo con el banco.
            string snap = "h7_" + sello;
            try { LabPresets.GuardarSnapshot(snap, "marca " + clase + " de la sesión", _sim, _jugador); }
            catch (System.Exception e) { Debug.LogError("[TenThousandYears] Diario, snapshot: " + e.Message); }

            Anotar("**" + clase + " · " + etiqueta + "** — en (" + cx + ", " + cy + ") · captura `" + nombre
                + "` · snapshot `" + snap + "`");
            Aviso(clase == "S" ? "¡ANDA! anotado" : clase == "C" ? "¿POR QUÉ? anotado" : "nota anotada", 1.6f);
        }

        /// <summary>
        /// Lo que se anota solo, una vez por segundo. Los hitos usan los contadores del libro que
        /// ya se llevaban desde R131: el «tiempo hasta descubrir cada máquina» que pide el
        /// protocolo no es una impresión, es el tick en que ese contador se movió por primera vez.
        /// </summary>
        private void Muestrear()
        {
            var st = _sim != null ? _sim.Stepper : null;
            if (st == null) return;

            // Contra la línea base de ESTA sesión, no contra cero: lo que importa es lo que hizo
            // el jugador, no lo que el mundo ya venía haciendo cuando llegó.
            if (!_hitoGoteo && st.LabGoteos > _baseGoteos) { _hitoGoteo = true; Anotar("**PRIMER GOTEO** — el alambique destila"); }
            if (!_hitoCocido && st.LabCocido > _baseCocido) { _hitoCocido = true; Anotar("**PRIMERA ARCILLA COCIDA** — hay calor domesticado"); }
            if (!_hitoFuego && st.LabCombustibleQuemado > _baseQuemado + 200) { _hitoFuego = true; Anotar("**PRIMER FUEGO** — algo arde de verdad"); }
            if (!_hitoCarbon && st.LabCarbonizado > _baseCarbon) { _hitoCarbon = true; Anotar("**PRIMER CARBÓN** — hay carbonera"); }
            if (!_hitoVidrio && st.LabVidrio > _baseVidrio) { _hitoVidrio = true; Anotar("**PRIMER VIDRIO** — hay horno"); }
            if (!_hitoPlanta && st.LabPlantasNacidas > _basePlantas) { _hitoPlanta = true; Anotar("**PRIMERA PLANTA** — el huerto germina"); }

            if (_sim != null)
            {
                int mult = _sim.LabMultiplicador;
                if (mult != _multiplicadorAnterior)
                {
                    if (_multiplicadorAnterior >= 0) Anotar("velocidad del mundo → ×" + mult);
                    _multiplicadorAnterior = mult;
                }
            }

            string pest = LabPanel.PestanaAbierta;
            if (pest != _pestanaAnterior)
            {
                if (!string.IsNullOrEmpty(pest)) Anotar("abre la pestaña **" + pest + "** del panel");
                else if (!string.IsNullOrEmpty(_pestanaAnterior)) Anotar("cierra el panel");
                _pestanaAnterior = pest;
            }

            int pincel = LabPanel.PincelSeleccionado;
            if (pincel != _pincelAnterior)
            {
                if (pincel >= 0) Anotar("arma el pincel: **" + LabPanel.NombrePincel(pincel) + "**");
                else if (_pincelAnterior >= 0) Anotar("desarma el pincel");
                _pincelAnterior = pincel;
            }
        }

        // =================================================================
        // EL AVISO EN PANTALLA (y la claqueta)
        // =================================================================
        private void Aviso(string texto, float segundos) { _avisoTexto = texto; _avisoHasta = Time.unscaledTime + segundos; }

        private void OnGUI()
        {
            if (Time.unscaledTime > _avisoHasta) return;
            if (_estiloAviso == null)
            {
                UiStyles.Preparar();
                _estiloAviso = new GUIStyle(UiStyles.Cuerpo) { fontSize = UiStyles.F(16), alignment = TextAnchor.MiddleCenter };
                _estiloAviso.normal.textColor = new Color(1f, 0.86f, 0.55f, 1f);
            }
            float w = UiStyles.Ancho(_estiloAviso, _avisoTexto) + UiStyles.S(28f);
            var r = new Rect((Screen.width - w) * 0.5f, UiStyles.S(24f), w, UiStyles.S(30f));
            var antes = GUI.color;
            GUI.color = new Color(0.05f, 0.05f, 0.06f, 0.85f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = antes;
            GUI.Label(r, _avisoTexto, _estiloAviso);
        }
    }
}
