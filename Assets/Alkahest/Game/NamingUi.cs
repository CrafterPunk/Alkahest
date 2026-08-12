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
    ///
    /// =====================================================================
    /// (fix playtest 12) "LA T ESTUVO BLOQUEADA HASTA QUE QUITÉ LAS PISTAS CON LA
    /// H" -- reporte literal, investigado a fondo. CAUSA RAÍZ CONFIRMADA: no era la
    /// H. El antiguo <c>Open()</c> (sustituido por <see cref="TryOpen"/>, ver más
    /// abajo) hacía `return` MUDO en cuanto <see cref="ResolveTarget()"/> devolvía <see cref="MaterialId.Empty"/>
    /// -- indistinguible, para quien pulsa T, de "la tecla no responde". Eso pasa
    /// con el frasco vacío y el cursor sobre algo sin material sampleable (aire,
    /// Piedra, o -- ver más abajo -- una redoma de la estantería). H (Game/
    /// HintSystem.cs) no toca ningún estado que esta clase lea: no comparten más
    /// que <see cref="UiStyles.EscribiendoTexto"/>, y H solo la CONSULTA, nunca la
    /// escribe. La correlación que vio el jugador es real pero CASUAL, no causal:
    /// el panel de pistas vive arriba-centro (Game/HintSystem.cs, y = S(54f)) justo
    /// donde el jugador tiene el cursor mientras LEE la pista -- y esa franja alta
    /// de pantalla, en coordenadas de mundo, suele caer sobre aire (encima del
    /// taller). Con el cursor ahí Y el frasco recién vaciado, T caía justo en el
    /// caso mudo; al ocultar las pistas (H) el jugador bajó el cursor a apuntar
    /// materia de verdad, y T "volvió a funcionar" -- sin que H hiciera nada por
    /// ello. NO SE TOCA HintSystem.cs para esto: no hay nada que arreglar ahí.
    ///
    /// "NO PUDE ACTIVARLA EN OTRO FRASCO": solo existe UN Flask de verdad (el del
    /// aprendiz, inyectado aquí). Lo que el jugador llama "otro frasco" son las
    /// REDOMAS de Game/StorageRack.cs (la estantería) -- que NO viven en la grilla
    /// de la sim: son atrezzo (SpriteRenderer) + un conteo `Redoma.Mat`/`Cantidad`
    /// privado, sin getter público. <see cref="SampleUnderCursor"/> solo sabe leer
    /// `AlkahestSim.SampleMaterial`, así que sobre una redoma siempre ve Empty (o
    /// la Piedra del listón), y `ResolveTarget` cae al frasco de verdad -- que
    /// suele estar vacío justo cuando algo se acaba de guardar en una redoma. Es
    /// el MISMO bug del párrafo de arriba (silencio en target Empty), con el
    /// agravante de que aquí no hay forma de arreglarlo del todo sin tocar
    /// StorageRack.cs (fuera de la lista de archivos modificables de este
    /// encargo): <see cref="TryOpen"/> al menos distingue este caso con
    /// <see cref="StorageRack.RatonSobreRedoma"/> (API pública ya existente) para
    /// explicar POR QUÉ, en vez de callar.
    ///
    /// ARREGLO REAL (este playtest): T nunca vuelve a no hacer NADA. Toda rama sin
    /// éxito da un aviso corto junto al cursor por el mismo canal que ya usa
    /// Game/StorageRack.cs (<see cref="Flask.Avisar"/>), distinguiendo motivo. De
    /// paso, aprovechando la reclasificación del playtest 10 (dos clases de
    /// material, ver Game/SubstanceKnowledge.cs regla 13 de CLAUDE.md): apuntar a
    /// VOCABULARIO DEL TALLER (agua, arena, aceite...) YA NO abre la ventana --
    /// antes sí lo hacía (`ResolveTarget` solo excluía Empty/Stone, nunca el resto
    /// del vocabulario mundano), dejando "bautizar" el agua, que el diseño prohíbe
    /// explícitamente. La invitación discreta "esto no tiene nombre -- T para
    /// bautizarlo" YA EXISTÍA antes de esta ronda (Game/SubstanceKnowledge.cs,
    /// <see cref="ActualizarAvisoBautizo"/>/<see cref="DrawAvisoBautizo"/>, mismo
    /// <see cref="UiStyles.Globo"/>, mismo criterio de una vez por material) y
    /// sigue funcionando sin cambios: reutiliza este mismo <see cref="ResolveTarget()"/>,
    /// así que el callejón sigue evitándose igual que antes.
    /// =====================================================================
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
                else TryOpen();
            }
            else if (_open && kb.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        /// <summary>
        /// Mensaje corto para el aviso que sale junto al cursor cuando T no
        /// abre nada (mismo canal que Flask.Avisar, ver Game/StorageRack.cs) --
        /// se autolimpia solo (Flask.SetFeedback), así que no hace falta
        /// throttle propio aquí: solo se dispara una vez por PULSACIÓN real de
        /// T, nunca por frame.
        /// </summary>
        private void Aviso(string msg) => _flask?.Avisar(msg);

        /// <summary>
        /// (fix playtest 12) Sustituye al antiguo <c>Open()</c> mudo -- ver el
        /// bloque grande de la CAUSA RAÍZ en el doc-comment de la clase. T
        /// SIEMPRE responde: o abre la ventana, o explica en un aviso breve
        /// por qué no hay nada que bautizar ahora mismo, distinguiendo los
        /// tres motivos reales (no hay objetivo en absoluto / es vocabulario
        /// del taller, no se bautiza / está en una redoma de la estantería,
        /// fuera de alcance de esta ventana). El cuarto caso -- "esto ya lo
        /// bautizaste tú" -- NO necesita aviso: ResolveTarget lo devuelve como
        /// un objetivo válido normal y la ventana se abre YA con el nombre
        /// actual precargado (ver más abajo), que es la propia respuesta
        /// ("ofrecer renombrar").
        /// </summary>
        private void TryOpen()
        {
            byte target = ResolveTarget();
            if (target == MaterialId.Empty)
            {
                // (fix playtest 12) Distingue el caso de la estantería (ver doc
                // de clase, "NO PUDE ACTIVARLA EN OTRO FRASCO"): StorageRack.cs
                // es de solo lectura este encargo y no expone el material de
                // cada redoma, pero SÍ expone si el cursor está sobre una --
                // suficiente para explicar la causa sin adivinar el contenido.
                if (StorageRack.RatonSobreRedoma())
                    Aviso("eso está en la estantería -- recupéralo (clic izq.) al frasco para bautizarlo");
                else
                    Aviso("no apuntas a nada -- señala una sustancia o llévala en el frasco");
                return;
            }

            // (fix playtest 12, regla 13 de CLAUDE.md) VOCABULARIO DEL TALLER:
            // agua/arena/aceite/vapor/humo/fuego/ceniza/hielo ya tienen nombre
            // desde el día 1 -- nadie los bautiza. Antes de esta ronda
            // ResolveTarget solo excluía Empty/Stone, así que apuntar a agua
            // SÍ abría esta ventana (con "Nombre actual: ???" porque NombreDe
            // no consulta el vocabulario común -- habría dejado ponerle un
            // nombre de jugador al agua). NombreComun() es la fuente de verdad
            // de esa clasificación (SubstanceKnowledge.cs, no se toca aquí,
            // solo se consulta su API pública).
            string comun = SubstanceKnowledge.NombreComun(target);
            if (comun != null)
            {
                Aviso("eso ya se llama " + comun + " -- el vocabulario del taller no se bautiza");
                return;
            }

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
