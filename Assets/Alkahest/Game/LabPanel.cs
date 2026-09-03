using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (R130) EL PANEL DEL LABORATORIO DE LEYES — docs/LAB/DISENO_LABORATORIO.md §9.
    /// ESQUELETO de Fable (arquitecto): tiempo, parámetros del registro por
    /// pestaña, libro mayor y teletransportes. Lo que falta lo implementa
    /// Opus 5 en el hito H2 (HANDOFF_OPUS.md): presets JSON con nombre/
    /// comparación/defaults por parámetro, snapshot PNG+JSON, ayuda [?]
    /// plegable por parámetro, pincel de materia, vistas de depuración.
    ///
    /// Teclas (guardas de la regla 12):
    ///   F8         abre/cierra el panel.
    ///   Ctrl+1..6  teletransporta a las zonas del plano.
    /// Solo existe en ModoLaboratorio (lo crea SpawnLaboratorio). Con el
    /// panel abierto, frasco/cincel/termómetro ceden los clics SOLO cuando el
    /// ratón está sobre la ventana (<see cref="BloqueaHerramientas"/>): se
    /// puede afinar un número y seguir vertiendo.
    /// </summary>
    public sealed class LabPanel : MonoBehaviour
    {
        private const int WindowId = 918274; // constante, jamás GetInstanceID (guía del proyecto). 918273 es el curador.

        public static bool Abierto => _instancia != null && _instancia._abierto;
        public static bool RatonSobrePanel { get; private set; }
        /// <summary>Guarda para Flask/Cincel/Termometro: el panel captura el ratón mientras está encima de él.</summary>
        public static bool BloqueaHerramientas => Abierto && RatonSobrePanel;
        private static LabPanel _instancia;

        private AlkahestSim _sim;
        private ApprenticeController _aprendiz;
        private bool _abierto;
        private Rect _ventana = new Rect(12f, 60f, 400f, 640f);
        private Vector2 _scroll;
        private int _pestana;
        private string[] _pestanas;
        private GUIStyle _estiloTitulo, _estiloPie, _estiloBoton, _estiloBotonSel, _estiloAyuda;
        private readonly HashSet<string> _ayudaAbierta = new HashSet<string>();
        private float _conteoHasta;
        private int _nAgua, _nSedimento, _nArcilla, _nPlanta, _nVapor; private long _vaporAire;

        public static LabPanel Crear(AlkahestSim sim, ApprenticeController aprendiz)
        {
            var go = new GameObject("LabPanel");
            var p = go.AddComponent<LabPanel>();
            p._sim = sim;
            p._aprendiz = aprendiz;
            _instancia = p;
            return p;
        }

        private void OnDestroy() { if (_instancia == this) _instancia = null; RatonSobrePanel = false; }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || _sim == null) return;
            bool tecladoLibre = !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto && !DayCycle.InputLocked;
            if (!tecladoLibre) { RatonSobrePanel = false; return; }

            if (kb.f8Key.wasPressedThisFrame) _abierto = !_abierto;

            bool ctrl = kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed;
            if (ctrl && _aprendiz != null)
            {
                for (int i = 0; i < SimLevelBuilder.LabAnclaX.Length; i++)
                {
                    Key tecla = (Key)((int)Key.Digit1 + i);
                    if (kb[tecla].wasPressedThisFrame) { TeleportarA(i); break; }
                }
            }

            if (LabParams.VaporVidaCambiado && _sim.Universe != null) Universe.ReaplicarVapor(_sim.Universe);

            var mouse = Mouse.current;
            if (_abierto && mouse != null)
            {
                Vector2 p = mouse.position.ReadValue();
                RatonSobrePanel = _ventana.Contains(new Vector2(p.x, Screen.height - p.y));
            }
            else RatonSobrePanel = false;

            if (Time.unscaledTime >= _conteoHasta) { Contar(); _conteoHasta = Time.unscaledTime + 1f; }
        }

        private void TeleportarA(int i)
        {
            float celda = SimRenderer.CellWorldSize;
            var destino = new Vector3((SimLevelBuilder.LabAnclaX[i] + 0.5f) * celda, (SimLevelBuilder.LabAnclaY[i] + 0.5f) * celda, 0f);
            _aprendiz.transform.position = destino;
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(destino.x, destino.y, cam.transform.position.z);
        }

        private void Contar()
        {
            var g = _sim.Grid; if (g == null) return;
            int a = 0, s = 0, ar = 0, pl = 0, va = 0; long vap = 0;
            var mat = g.mat; var hum = g.humedad;
            for (int i = 0; i < mat.Length; i++)
            {
                switch (mat[i])
                {
                    case MaterialId.Water: a++; break;
                    case MaterialId.Sedimento: s++; break;
                    case MaterialId.Arcilla: ar++; break;
                    case MaterialId.Planta: pl++; break;
                    case MaterialId.Steam: va++; break;
                    case MaterialId.Empty: vap += hum[i]; break;
                }
            }
            _nAgua = a; _nSedimento = s; _nArcilla = ar; _nPlanta = pl; _nVapor = va; _vaporAire = vap;
        }

        private void OnGUI()
        {
            if (DayCycle.InputLocked || !_abierto) return;
            PrepararEstilos();
            GUI.depth = 5;
            _ventana = GUILayout.Window(WindowId, _ventana, DibujarVentana, "LABORATORIO DE LEYES (F8)", GUI.skin.window);
            string pie = "F8: panel · Ctrl+1..6: zonas · G: termómetro · C: cincel · F6: movimiento · F7: piel de roca";
            float w = UiStyles.Ancho(_estiloPie, pie) + 16f;
            GUI.Label(new Rect((Screen.width - w) * 0.5f, Screen.height - UiStyles.S(26f), w, UiStyles.S(22f)), pie, _estiloPie);
        }

        private void DibujarVentana(int id)
        {
            if (_pestanas == null)
            {
                var lista = new List<string> { "TIEMPO", "LIBRO" };
                foreach (var p in LabParams.Registro) if (!lista.Contains(p.Grupo)) lista.Add(p.Grupo);
                _pestanas = lista.ToArray();
            }
            // Pestañas en dos filas.
            for (int fila = 0; fila < 2; fila++)
            {
                GUILayout.BeginHorizontal();
                for (int i = fila * ((_pestanas.Length + 1) / 2); i < Mathf.Min(_pestanas.Length, (fila + 1) * ((_pestanas.Length + 1) / 2)); i++)
                {
                    if (GUILayout.Button(_pestanas[i], i == _pestana ? _estiloBotonSel : _estiloBoton)) _pestana = i;
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(4f);

            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(UiStyles.S(520f)));
            string grupo = _pestanas[_pestana];
            if (grupo == "TIEMPO") DibujarTiempo();
            else if (grupo == "LIBRO") DibujarLibro();
            else DibujarParametros(grupo);
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DibujarTiempo()
        {
            var st = _sim.Stepper;
            GUILayout.Label("VELOCIDAD DEL MUNDO", _estiloTitulo);
            GUILayout.BeginHorizontal();
            int[] vel = { 1, 5, 10, 50, 100 };
            foreach (int v in vel)
                if (GUILayout.Button(v + "x", _sim.LabMultiplicador == v ? _estiloBotonSel : _estiloBoton)) _sim.LabMultiplicador = v;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_sim.Paused ? "REANUDAR" : "PAUSA", _estiloBoton)) _sim.Paused = !_sim.Paused;
            if (GUILayout.Button("UN TICK", _estiloBoton)) _sim.StepOnce();
            GUILayout.EndHorizontal();
            GUILayout.Label($"pedido {_sim.LabMultiplicador}x · real {_sim.LabMultiplicadorReal:F1}x · presupuesto {LabParams.PresupuestoMs} ms/frame", _estiloPie);
            _sim.LabPresupuestoMs = LabParams.PresupuestoMs;
            GUILayout.Space(6f);
            if (st != null)
            {
                GUILayout.Label("COSTE DEL ÚLTIMO TICK (ms)", _estiloTitulo);
                GUILayout.Label($"total {st.LastStepMs:F2} · difusión {st.MsDifusion:F2} · barrido {st.MsBarrido:F2} · chunks {st.MsChunks:F2} · morph {st.MsMorph:F2}", _estiloPie);
                GUILayout.Label($"campos {st.MsCampos:F2} · presión {st.MsPresion:F2} · luz {st.MsLuz:F2} · cuerpos {st.MsCuerpos:F2}", _estiloPie);
                GUILayout.Label($"tick {st.Tick} · chunks despiertos {st.ActiveChunks}/{CellGrid.ChunksX * CellGrid.ChunksY} · celdas activas {st.ActiveCells} · FPS {1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f):F0}", _estiloPie);
            }
            GUILayout.Space(6f);
            DibujarParametros("TIEMPO");
        }

        private void DibujarLibro()
        {
            var st = _sim.Stepper; if (st == null) return;
            GUILayout.Label("CENSO (cada segundo)", _estiloTitulo);
            GUILayout.Label($"agua {_nAgua} · vapor visible {_nVapor} · vapor en el aire {(_vaporAire / 255f):F1} celdas eq. · sedimento {_nSedimento} · arcilla {_nArcilla} · plantas {_nPlanta}", _estiloPie);
            GUILayout.Label("LIBRO MAYOR (celdas o unidades/255)", _estiloTitulo);
            GUILayout.Label($"manantial emitió {st.LabAguaEmitida} · sumidero tragó {st.LabAguaSumida}", _estiloPie);
            GUILayout.Label($"evaporado {st.LabEvaporado / 255f:F1} · condensado {st.LabCondensado / 255f:F1} · goteos {st.LabGoteos}", _estiloPie);
            GUILayout.Label($"infiltrado {st.LabInfiltrado / 255f:F1} · exudado {st.LabExudado} · depositado {st.LabDepositado} · erosionado {st.LabErosionado}", _estiloPie);
            GUILayout.Label($"compactado {st.LabCompactado} · ablandado {st.LabAblandado} · cocido {st.LabCocido} · abonado {st.LabAbonado}", _estiloPie);
            GUILayout.Label($"plantas nacidas {st.LabPlantasNacidas} · muertas {st.LabPlantasMuertas} · presión movió {st.LabPresionMovidas} · cuerpos caídos {st.LabCuerposCaidos} · fracturas {st.LabFracturas}", _estiloPie);
            GUILayout.Space(6f);
            GUILayout.Label("BALANCE DE AGUA: emitido + goteos + exudado + erosionado − tragado − depositado ≈ agua + vapor + humedad del suelo (lo que falte lo tiene el frasco o se fue por el borde).", _estiloAyuda);
        }

        private void DibujarParametros(string grupo)
        {
            foreach (var p in LabParams.Registro)
            {
                if (p.Grupo != grupo) continue;
                float v = p.Leer();
                bool esDef = p.EsDefault;
                GUILayout.BeginHorizontal();
                GUILayout.Label((esDef ? "" : "● ") + p.Nombre, _estiloPie, GUILayout.Width(UiStyles.S(190f)));
                string valor = p.Entero ? ((int)v).ToString() : v.ToString("F2");
                GUILayout.Label(valor + " " + p.Unidad, _estiloPie, GUILayout.Width(UiStyles.S(120f)));
                if (GUILayout.Button("D", _estiloBoton, GUILayout.Width(UiStyles.S(24f)))) p.Escribir(p.Def);
                if (GUILayout.Button("?", _estiloBoton, GUILayout.Width(UiStyles.S(24f))))
                {
                    if (_ayudaAbierta.Contains(p.Clave)) _ayudaAbierta.Remove(p.Clave); else _ayudaAbierta.Add(p.Clave);
                }
                GUILayout.EndHorizontal();
                float nv = GUILayout.HorizontalSlider(v, p.Min, p.Max);
                if (p.Entero) nv = Mathf.Round(nv);
                if (!Mathf.Approximately(nv, v)) p.Escribir(nv);
                if (_ayudaAbierta.Contains(p.Clave))
                {
                    GUILayout.Label($"[{p.Clave}] default {p.Def} · rango {p.Min}..{p.Max}" + (p.RequiereReconstruir ? " · aplica al reconstruir" : " · en vivo"), _estiloAyuda);
                    if (!string.IsNullOrEmpty(p.Ayuda)) GUILayout.Label(p.Ayuda, _estiloAyuda);
                }
            }
        }

        private void PrepararEstilos()
        {
            if (_estiloTitulo != null) return;
            UiStyles.Preparar();
            _estiloTitulo = new GUIStyle(UiStyles.Cuerpo) { fontSize = UiStyles.F(11), fontStyle = FontStyle.Bold };
            _estiloTitulo.normal.textColor = new Color(0.78f, 0.72f, 0.62f, 1f);
            _estiloPie = new GUIStyle(UiStyles.Cuerpo) { fontSize = UiStyles.F(12), wordWrap = true };
            _estiloPie.normal.textColor = new Color(0.92f, 0.88f, 0.80f, 0.9f);
            _estiloAyuda = new GUIStyle(_estiloPie) { fontSize = UiStyles.F(11), fontStyle = FontStyle.Italic };
            _estiloAyuda.normal.textColor = new Color(0.75f, 0.80f, 0.70f, 0.9f);
            _estiloBoton = new GUIStyle(GUI.skin.button) { fontSize = UiStyles.F(11), alignment = TextAnchor.MiddleCenter };
            _estiloBotonSel = new GUIStyle(_estiloBoton) { fontStyle = FontStyle.Bold };
            _estiloBotonSel.normal.textColor = new Color(1f, 0.82f, 0.55f, 1f);
        }
    }
}
