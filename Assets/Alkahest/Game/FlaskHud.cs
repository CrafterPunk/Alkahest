using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// HUD IMGUI mínimo: barra de llenado del frasco, hasta 4 contenidos más
    /// grandes como swatches de color, texto de ayuda, y una mira en la
    /// posición del cursor. Ventana con id constante en la esquina
    /// inferior izquierda.
    /// </summary>
    public sealed class FlaskHud : MonoBehaviour
    {
        // Id constante (distinto del de DevPalette, 837465), tal y como exige la guía del proyecto.
        private const int WindowId = 837470;
        private const int MaxSwatches = 4;
        private const float WindowWidth = 260f;
        private const float WindowHeight = 150f;

        private AlkahestSim _sim;
        private Flask _flask;
        private Rect _windowRect;
        private GUIStyle _crosshairStyle;

        // Buffers reutilizados cada frame para no asignar memoria al calcular el top-4.
        private readonly byte[] _topIds = new byte[MaxSwatches];
        private readonly int[] _topCounts = new int[MaxSwatches];

        public void Init(AlkahestSim sim, Flask flask)
        {
            _sim = sim;
            _flask = flask;
        }

        private void OnGUI()
        {
            if (_sim == null || _flask == null || _sim.Universe == null) return;

            if (_crosshairStyle == null)
            {
                _crosshairStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 18 };
            }

            _windowRect = new Rect(12f, Screen.height - WindowHeight - 12f, WindowWidth, WindowHeight);
            _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindow, "Frasco");

            DrawCrosshair();
        }

        private void DrawWindow(int id)
        {
            int total = _flask.Total;
            GUILayout.Label($"Llenado: {total}/{Flask.Capacity}");

            Rect barOuter = GUILayoutUtility.GetRect(220f, 14f);
            GUI.Box(barOuter, GUIContent.none);
            float frac = (float)total / Flask.Capacity;
            Rect barInner = new Rect(barOuter.x + 2f, barOuter.y + 2f, (barOuter.width - 4f) * Mathf.Clamp01(frac), barOuter.height - 4f);
            var prevColor = GUI.color;
            GUI.color = new Color(0.5f, 0.75f, 1f, 1f);
            GUI.DrawTexture(barInner, Texture2D.whiteTexture);
            GUI.color = prevColor;

            GUILayout.Space(6f);
            ComputeTopContents();
            for (int i = 0; i < MaxSwatches; i++)
            {
                if (_topCounts[i] <= 0) continue;
                var def = _sim.Universe.Get(_topIds[i]);
                GUILayout.BeginHorizontal();
                Rect swatch = GUILayoutUtility.GetRect(16f, 16f, GUILayout.Width(16f));
                var prev = GUI.color;
                GUI.color = def.baseColor;
                GUI.DrawTexture(swatch, Texture2D.whiteTexture);
                GUI.color = prev;
                GUILayout.Label($"{def.devName}: {_topCounts[i]}");
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(6f);
            GUILayout.Label("LMB aspirar · RMB verter · Q vaciar");

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
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

        private void DrawCrosshair()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            // Mouse.current.position usa origen abajo-izquierda; IMGUI usa origen arriba-izquierda.
            Vector2 guiPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
            GUI.Label(new Rect(guiPos.x - 12f, guiPos.y - 12f, 24f, 24f), "◯", _crosshairStyle);
        }
    }
}
