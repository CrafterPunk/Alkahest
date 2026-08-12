using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Ventana IMGUI para "bautizar" materiales: el juego nunca revela los
    /// devName internos, así que el jugador les pone el nombre que quiera.
    /// T abre/cierra; ESC también cierra. El objetivo es el material bajo
    /// el cursor si no es Empty/Stone; si no, el material con mayor conteo
    /// en el frasco.
    /// </summary>
    public sealed class NamingUi : MonoBehaviour
    {
        private const int WindowId = 837480;
        private const float WindowWidth = 260f;
        private const float WindowHeight = 170f;

        private AlkahestSim _sim;
        private Flask _flask;
        private SubstanceKnowledge _knowledge;

        private bool _open;
        private byte _targetMat;
        private string _nameField = "";
        private Rect _windowRect;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Flask flask, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _flask = flask;
            _knowledge = knowledge;
            _windowRect = new Rect((Screen.width - WindowWidth) * 0.5f, (Screen.height - WindowHeight) * 0.5f, WindowWidth, WindowHeight);
        }

        private void Update()
        {
            if (DayCycle.InputLocked)
            {
                _open = false;
                return;
            }

            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.tKey.wasPressedThisFrame)
            {
                if (_open) Close();
                else Open();
            }
            else if (_open && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        private void Open()
        {
            byte target = ResolveTarget();
            if (target == MaterialId.Empty) return; // nada que bautizar todavía (frasco vacío y cursor sobre nada).

            _targetMat = target;
            string current = _knowledge != null ? _knowledge.NombreDe(_targetMat) : "???";
            _nameField = current == "???" ? "" : current;
            _open = true;
        }

        private void Close()
        {
            _open = false;
            GUI.FocusControl(null);
        }

        private byte ResolveTarget()
        {
            // (fix playtest 7 — causa raíz del bautizo que "no se ve en la
            // botella" y que "pisó" otro nombre) Las redomas del estante NO
            // ocupan celdas de la simulación: son un mueble visual que guarda
            // (Mat, Cantidad) por su cuenta (ver StorageRack.Redoma). Por eso
            // apuntar con el ratón a una redoma llena SIEMPRE muestreaba la
            // PIEDRA del listón bajo ella y esta función se replegaba a "lo
            // que más llevas en el frasco" — que casi nunca es lo que hay
            // realmente en la redoma señalada. Consultarla PRIMERO hace que
            // "T" sobre una redoma bautice de verdad su contenido.
            byte enRedoma = StorageRack.MaterialBajoCursor();
            if (enRedoma != MaterialId.Empty) return enRedoma;

            byte underCursor = SampleUnderCursor();
            if (underCursor != MaterialId.Empty && underCursor != MaterialId.Stone) return underCursor;
            return LargestInFlask();
        }

        private byte SampleUnderCursor()
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null || _sim == null) return MaterialId.Empty;

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter)) return MaterialId.Empty;

            Vector3 world = ray.GetPoint(enter);
            Vector2Int cell = _sim.WorldToCell(world);
            if (!CellGrid.InBounds(cell.x, cell.y)) return MaterialId.Empty;

            return (byte)_sim.SampleMaterial(cell.x, cell.y);
        }

        private byte LargestInFlask()
        {
            if (_flask == null) return MaterialId.Empty;

            byte best = MaterialId.Empty;
            int bestCount = 0;
            for (int m = 1; m < MaterialId.Count; m++)
            {
                int c = _flask.GetCount((byte)m);
                if (c > bestCount)
                {
                    bestCount = c;
                    best = (byte)m;
                }
            }
            return best;
        }

        private void OnGUI()
        {
            if (!_open || _sim == null || _sim.Universe == null || _knowledge == null) return;
            _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindow, "Bautizar material");
        }

        private void DrawWindow(int id)
        {
            var def = _sim.Universe.Get(_targetMat);

            GUILayout.BeginHorizontal();
            Rect swatch = GUILayoutUtility.GetRect(20f, 20f, GUILayout.Width(20f));
            var prevColor = GUI.color;
            GUI.color = def.baseColor;
            GUI.DrawTexture(swatch, Texture2D.whiteTexture);
            GUI.color = prevColor;
            GUILayout.Label($"Nombre actual: {_knowledge.NombreDe(_targetMat)}");
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Nuevo nombre:");
            GUI.SetNextControlName("NamingUiField");
            _nameField = GUILayout.TextField(_nameField, 40);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bautizar"))
            {
                _knowledge.Bautizar(_targetMat, _nameField);
            }
            if (GUILayout.Button("Cerrar")) Close();
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
            GUILayout.Label("T / ESC para cerrar");

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }
    }
}
