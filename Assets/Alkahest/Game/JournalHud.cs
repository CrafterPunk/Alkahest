using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Diario del aprendiz: lista compacta IMGUI de los materiales
    /// descubiertos hasta ahora (ver SubstanceKnowledge), con su nombre (o
    /// "???" si no bautizado todavía) y chips cortos de qué le han visto
    /// hacer ("arde", "cristaliza", "crece", "se disuelve", "hierve", "se
    /// congela"). J alterna visibilidad.
    ///
    /// (fix playtest 9) SECCIÓN "LEYES" -- el jugador reportó llevar horas sin
    /// entender por qué el cristal/vivium no "se multiplicaban": el diario
    /// solo registraba OBSERVACIONES sueltas ("lo vi arder"), nunca el
    /// PROCEDIMIENTO completo ("esto + esto -> esto, y no se gasta"). Esta
    /// sección lee la tabla de reacciones REAL del universo activo
    /// (<see cref="AlkahestSim.Universe"/>.Reactions, horneada por seed en
    /// Sim/Universe.cs -- NUNCA se toca Sim/, solo se lee su API pública) más
    /// la ley de crecimiento del Vivium (que no vive en esa tabla: es una
    /// regla propia de Sim/SimStepper.cs/GrowthTick, así que se añade a mano
    /// pero con los NÚMEROS REALES de esta seed, nunca inventados) y las
    /// traduce a filas "qué + qué -> qué". Como la lista sale de datos y no de
    /// texto fijo, sigue siendo correcta si el universo cambia por seed.
    ///
    /// CATALIZADOR/PROPAGACIÓN, la propiedad que de verdad cambia el juego:
    /// una reacción es "catalítica" cuando UNO de los dos lados no cambia
    /// (productX == x) y el otro sí -- exactamente la semántica que ya
    /// documenta Sim/ReactionEngine.cs ("si un producto es igual al material
    /// original, esa celda no cambia"). Con otras palabras: "la semilla no se
    /// gasta". Se marca con un distintivo bien visible (★ SE PROPAGA).
    ///
    /// Nota de glifos: "->" en vez de una flecha Unicode y "★" (no "⟳") a
    /// propósito -- el resto del proyecto solo ha probado en la fuente IMGUI
    /// real "·"/"—"/"★" (ver OrdersHud.cs, DayCycle.cs, HintSystem.cs); una
    /// flecha o símbolo de reciclaje sin uso previo en UI de verdad (solo en
    /// comentarios, que no se renderizan) se arriesga a salir como "tofu".
    /// </summary>
    public sealed class JournalHud : MonoBehaviour
    {
        private const int WindowId = 837481;
        private const float WindowWidth = 300f;
        private const float WindowHeight = 340f;

        // Cota generosa para el array fijo de leyes: 17 materiales -> como
        // mucho 17*16/2=136 pares posibles, pero la tabla real de Universe.cs
        // tiene ~6 entradas; 24 deja margen de sobra sin listas dinámicas.
        private const int MaxLeyes = 24;

        private struct LeyDatos
        {
            public byte a, b, productA, productB;
            public bool catalitica;
            public bool soloFrio;
            public bool soloCalor;
            public bool esCrecimiento; // true solo para la ley especial de Vivium (no viene de ReactionEngine).
        }

        private AlkahestSim _sim;
        private SubstanceKnowledge _knowledge;
        private bool _visible;
        private Rect _windowRect;
        private Vector2 _scroll;

        // Estructura de leyes: se calcula UNA sola vez en Init (la tabla de
        // reacciones de este universo no cambia durante la partida). El TEXTO
        // de cada fila sí depende de nombres bautizables, así que se cachea
        // aparte y solo se reconstruye cuando cambia el estado de
        // conocimiento del jugador (ver ActualizarCacheLeyes) -- nunca se
        // reconstruyen strings en cada frame de OnGUI si nada cambió.
        private readonly LeyDatos[] _leyes = new LeyDatos[MaxLeyes];
        private int _leyesCount;

        private readonly string[] _leyesTexto = new string[MaxLeyes];
        private readonly bool[] _leyesVisibles = new bool[MaxLeyes];
        private int _leyesFirmaCache = int.MinValue;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _knowledge = knowledge;
            _windowRect = new Rect(Screen.width - WindowWidth - 12f, Screen.height - WindowHeight - 12f, WindowWidth, WindowHeight);
            ConstruirLeyesDesdeUniverso();
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

            ActualizarCacheLeyes();
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

            DrawLeyes();

            GUILayout.EndScrollView();
            GUILayout.Label("LMB/verter/hover ≥1s descubre materiales · T bautiza");

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        /// <summary>
        /// Filas "qué + qué -> qué" de las leyes ya descubiertas (ambos lados
        /// tienen que estar descubiertos: un diario no revela leyes de
        /// materiales que el jugador aún no ha visto -- ver doc de la clase).
        /// </summary>
        private void DrawLeyes()
        {
            int visibles = 0;
            for (int i = 0; i < _leyesCount; i++) if (_leyesVisibles[i]) visibles++;
            if (visibles == 0) return;

            GUILayout.Space(6f);
            GUILayout.Label("LEYES (repetibles)");

            for (int i = 0; i < _leyesCount; i++)
            {
                if (!_leyesVisibles[i]) continue;

                GUILayout.BeginHorizontal();
                GUILayout.Label(_leyesTexto[i]);
                if (_leyes[i].catalitica)
                {
                    // Verde éxito (mismo tono que UiStyles.Exito): esta ventana usa
                    // el skin IMGUI por defecto desde siempre (nunca importó
                    // UiStyles), así que se replica el color a mano en vez de
                    // arrastrar todo el sistema de estilos a una ventana que nunca
                    // lo necesitó para lo demás.
                    var prev = GUI.color;
                    GUI.color = new Color(0.52f, 0.92f, 0.60f, 1f);
                    GUILayout.Label("★ SE PROPAGA", GUILayout.Width(92f));
                    GUI.color = prev;
                }
                GUILayout.EndHorizontal();
            }
        }

        /// <summary>
        /// Vuelca la tabla de reacciones real de este universo (más la ley de
        /// crecimiento de Vivium) a <see cref="_leyes"/>. Llamado una única
        /// vez desde Init: la tabla de reacciones de un universo ya creado no
        /// cambia jamás durante la partida (Sim/Universe.cs la hornea una sola
        /// vez en Create), así que no hace falta recalcular esto en Update.
        /// </summary>
        private void ConstruirLeyesDesdeUniverso()
        {
            _leyesCount = 0;
            if (_sim == null || _sim.Universe == null) return;

            var universe = _sim.Universe;
            var reactions = universe.Reactions;

            // Recorre todos los pares (a,b) con a<b: ReactionEngine.TryGet es
            // simétrico (registra la misma reacción en (a,b) y (b,a)), así que
            // basta un sentido para no duplicar filas.
            for (byte a = 1; a < MaterialId.Count && _leyesCount < MaxLeyes; a++)
            {
                for (byte b = (byte)(a + 1); b < MaterialId.Count && _leyesCount < MaxLeyes; b++)
                {
                    if (!reactions.TryGet(a, b, out Reaction r)) continue;

                    // r.a/r.b pueden venir en cualquier orden respecto a (a,b) --
                    // se reordena para que productA/productB correspondan siempre
                    // a (a,b), no a como se registró internamente la Reaction.
                    byte pa = r.a == a ? r.productA : r.productB;
                    byte pb = r.a == a ? r.productB : r.productA;

                    // Catalítica = exactamente un lado no cambia (ver doc de la
                    // clase): la misma semántica que documenta ReactionEngine.
                    bool catalitica = (pa == a) != (pb == b);

                    _leyes[_leyesCount] = new LeyDatos
                    {
                        a = a,
                        b = b,
                        productA = pa,
                        productB = pb,
                        catalitica = catalitica,
                        soloFrio = r.maxTempRaw < 255,
                        soloCalor = r.minTempRaw > 0,
                        esCrecimiento = false,
                    };
                    _leyesCount++;
                }
            }

            // Ley de crecimiento del Vivium: no vive en ReactionEngine (es la
            // regla propia de Sim/SimStepper.cs GrowthTick -- un Nutrient
            // vecino se consume y, con VivGrowChancePct de probabilidad, nace
            // Vivium nuevo ahí), así que se añade a mano, pero SIN inventar
            // ningún número: solo se usa como marcador estructural (a=Vivium
            // no cambia, b=Nutrient se convierte en Vivium), la banda de
            // temperatura real de esta seed se muestra en el texto (ver
            // BuildLeyTexto).
            if (_leyesCount < MaxLeyes && universe.Get(MaterialId.Vivium).archetype == MaterialArchetype.Organic)
            {
                _leyes[_leyesCount] = new LeyDatos
                {
                    a = MaterialId.Vivium,
                    b = MaterialId.Nutrient,
                    productA = MaterialId.Vivium,
                    productB = MaterialId.Vivium,
                    catalitica = true,
                    soloFrio = false,
                    soloCalor = false,
                    esCrecimiento = true,
                };
                _leyesCount++;
            }
        }

        /// <summary>
        /// Reconstruye los textos de las leyes SOLO si el conocimiento del
        /// jugador cambió desde la última vez (nuevo material descubierto o
        /// un (re)bautizo -- ver SubstanceKnowledge.NamingVersion, que a
        /// diferencia de CountNamed() sí detecta un renombrado). Evita
        /// reconstruir strings en cada frame de OnGUI cuando el texto no
        /// cambia, tal y como exige el resto del proyecto.
        /// </summary>
        private void ActualizarCacheLeyes()
        {
            int firma = _knowledge.CountDiscovered() * 1000003 + _knowledge.NamingVersion;
            if (firma == _leyesFirmaCache) return;
            _leyesFirmaCache = firma;

            for (int i = 0; i < _leyesCount; i++)
            {
                var ley = _leyes[i];
                bool visible = _knowledge.EsDescubierto(ley.a) && _knowledge.EsDescubierto(ley.b);
                _leyesVisibles[i] = visible;
                _leyesTexto[i] = visible ? BuildLeyTexto(ley) : null;
            }
        }

        private string BuildLeyTexto(LeyDatos ley)
        {
            string nombreA = _knowledge.NombreParaHud(ley.a);
            string nombreB = _knowledge.NombreParaHud(ley.b);
            string nombrePb = _knowledge.NombreParaHud(ley.productB);
            string cond = ley.soloFrio ? " (en frío)" : (ley.soloCalor ? " (en calor)" : "");

            if (ley.esCrecimiento)
            {
                return $"{nombreA} asentado + {nombreB} cerca, TEMPLADO -> {nombrePb} nuevo";
            }

            string nombrePa = _knowledge.NombreParaHud(ley.productA);
            return $"{nombreA} + {nombreB} -> {nombrePa} + {nombrePb}{cond}";
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
