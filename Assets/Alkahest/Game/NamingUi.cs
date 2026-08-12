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
    ///
    /// (fix playtest 10) BUG DEL SILENCIADO, VERSIÓN "T": el mismo problema que
    /// silenciaba el audio al escribir una "m" en el nombre (Input System nuevo +
    /// atajo de una tecla escuchando en paralelo al campo de texto IMGUI) le pasaba
    /// a esta propia clase con su PROPIA tecla -- escribir un nombre que contuviera
    /// una "t" (p.ej. "musgo hambriento") cerraba la ventana a mitad de escritura,
    /// porque <see cref="Update"/> mira Keyboard.current.tKey SIN saber que el campo
    /// de texto también se estaba comiendo esa misma pulsación. Arreglo: mientras el
    /// campo está abierto se levanta <see cref="UiStyles.EscribiendoTexto"/> (regla
    /// nueva del proyecto, ver su doc-comment: "todos los atajos de una tecla deben
    /// consultarla") y el propio toggle de T la respeta -- así T solo abre/cierra
    /// cuando NO hay nada que escribir, y mientras se escribe, T escribe.
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
                if (_open) Close(); // (fix playtest 10) no solo _open=false: hay que bajar también EscribiendoTexto.
                return;
            }

            var kb = Keyboard.current;
            if (kb == null) return;

            // (fix playtest 10) Mientras se escribe, T teclea, no cierra -- ver doc de
            // clase. Solo se comprueba en la rama de ABRIR/CERRAR: Escape sigue
            // funcionando siempre, es la convención universal de "cancelar" y no es
            // un carácter que pueda aparecer sin querer en un nombre.
            // Con el diario a pantalla completa (JournalHud.Abierto) tampoco tiene
            // sentido abrir este campo: quedaría dibujado detrás del libro (que
            // fuerza GUI.depth por debajo de todo) pero seguiría robando el teclado
            // -- mismo criterio que ya siguen Flask/HeatPlate/ChillStone/Dispenser/
            // StorageRack/ApprenticeController/DevPalette con este mismo atajo.
            if (kb.tKey.wasPressedThisFrame && !UiStyles.EscribiendoTexto && !JournalHud.Abierto)
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
            UiStyles.EscribiendoTexto = true; // (fix playtest 10) ver doc de clase y de UiStyles.EscribiendoTexto.
        }

        private void Close()
        {
            _open = false;
            UiStyles.EscribiendoTexto = false; // (fix playtest 10) simétrico con Open(): nunca se queda "atascada" en true.
            GUI.FocusControl(null);
        }

        private byte ResolveTarget() => ResolveTarget(_sim, _flask);

        /// <summary>
        /// Versión estática del criterio de objetivo (cursor primero, frasco de
        /// respaldo), para que <see cref="SubstanceKnowledge"/> pueda saber, sin
        /// duplicar esta lógica, exactamente qué material abriría T ahora mismo --
        /// es lo que decide cuándo mostrar "esto no tiene nombre" (fix playtest 10,
        /// ver SubstanceKnowledge.ActualizarAvisoBautizo). No requiere una instancia:
        /// ambos MonoBehaviour reciben (AlkahestSim, Flask) por Init desde
        /// AlkahestGameBootstrap, así que no hace falta cablear una referencia nueva.
        /// </summary>
        public static byte ResolveTarget(AlkahestSim sim, Flask flask)
        {
            byte underCursor = SampleUnderCursor(sim);
            if (underCursor != MaterialId.Empty && underCursor != MaterialId.Stone) return underCursor;
            return LargestInFlask(flask);
        }

        private static byte SampleUnderCursor(AlkahestSim sim)
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null || sim == null) return MaterialId.Empty;

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter)) return MaterialId.Empty;

            Vector3 world = ray.GetPoint(enter);
            Vector2Int cell = sim.WorldToCell(world);
            if (!CellGrid.InBounds(cell.x, cell.y)) return MaterialId.Empty;

            return (byte)sim.SampleMaterial(cell.x, cell.y);
        }

        private static byte LargestInFlask(Flask flask)
        {
            if (flask == null) return MaterialId.Empty;

            byte best = MaterialId.Empty;
            int bestCount = 0;
            for (int m = 1; m < MaterialId.Count; m++)
            {
                int c = flask.GetCount((byte)m);
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
