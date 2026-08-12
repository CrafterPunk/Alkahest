using UnityEngine;
using UnityEngine.InputSystem;

namespace Alkahest.Game
{
    /// <summary>
    /// Diario del aprendiz: lista compacta IMGUI de los materiales
    /// descubiertos hasta ahora (ver SubstanceKnowledge), con su nombre (o
    /// "???" si no bautizado todavía) y chips cortos de qué le han visto
    /// hacer ("arde", "cristaliza", "crece", "se disuelve", "hierve", "se
    /// congela"). J alterna visibilidad.
    /// </summary>
    public sealed class JournalHud : MonoBehaviour
    {
        private const int WindowId = 837481;
        private const float WindowWidth = 300f;
        private const float WindowHeight = 340f;

        private AlkahestSim _sim;
        private SubstanceKnowledge _knowledge;
        private bool _visible;
        private Rect _windowRect;
        private Vector2 _scroll;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _knowledge = knowledge;
            _windowRect = new Rect(Screen.width - WindowWidth - 12f, Screen.height - WindowHeight - 12f, WindowWidth, WindowHeight);
        }

        private void Update()
        {
            if (DayCycle.InputLocked) return;

            var kb = Keyboard.current;
            if (kb != null && kb.jKey.wasPressedThisFrame) _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || DayCycle.InputLocked) return;
            if (_sim == null || _sim.Universe == null || _knowledge == null) return;

            _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindow, "Diario de materiales (J)");
        }

        private void DrawWindow(int id)
        {
            _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.Height(WindowHeight - 46f));

            var mats = _sim.Universe.Materials;
            int shown = 0;
            for (int m = 1; m < mats.Length; m++)
            {
                byte matId = (byte)m;
                if (!_knowledge.EsDescubierto(matId)) continue;
                shown++;

                var def = mats[m];
                GUILayout.BeginHorizontal();

                Rect swatch = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f));
                var prevColor = GUI.color;
                GUI.color = def.baseColor;
                GUI.DrawTexture(swatch, Texture2D.whiteTexture);
                GUI.color = prevColor;

                // NombreParaHud (no NombreDe): el agua se llama "agua" aunque no
                // la hayas bautizado; solo lo exótico sigue siendo "???".
                GUILayout.Label(_knowledge.NombreParaHud(matId), GUILayout.Width(110f));
                GUILayout.Label(BuildChips(matId));

                GUILayout.EndHorizontal();
            }

            if (shown == 0) GUILayout.Label("(nada descubierto todavía)");

            GUILayout.EndScrollView();
            GUILayout.Label("LMB/verter/hover ≥1s descubre materiales · T bautiza");

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private string BuildChips(byte matId)
        {
            var flags = _knowledge.WitnessOf(matId);
            if (flags == WitnessFlags.None) return "";

            string s = "";
            s = AppendChip(s, flags, WitnessFlags.Arder);
            s = AppendChip(s, flags, WitnessFlags.Cristalizar);
            s = AppendChip(s, flags, WitnessFlags.Crecer);
            s = AppendChip(s, flags, WitnessFlags.Disolverse);
            s = AppendChip(s, flags, WitnessFlags.Hervir);
            s = AppendChip(s, flags, WitnessFlags.Congelarse);
            return s;
        }

        private static string AppendChip(string s, WitnessFlags flags, WitnessFlags flag)
        {
            if ((flags & flag) == 0) return s;
            string chip = SubstanceKnowledge.ChipLabel(flag);
            return s.Length == 0 ? chip : s + " · " + chip;
        }
    }
}
