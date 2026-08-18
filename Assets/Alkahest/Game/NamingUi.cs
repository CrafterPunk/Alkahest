using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;
using Alkahest.Net;

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
        // =================================================================
        // (playtest 31, LA IDENTIDAD) EL BAUTIZO DEJA DE SER UN DIÁLOGO Y
        // PASA A SER UN RITO
        // =================================================================
        // Encargo literal de Cesar: "el menú de bautizar tiene que dejar de
        // parecer un menú de Windows XP". Lo era, literalmente: una
        // GUILayout.Window con el skin por defecto, título de barra del
        // sistema, un swatch de 20x20 px y dos botones grises.
        //
        // Lo que se cambia y POR QUÉ (cada punto es una razón, no un gusto):
        //  · FONDO: UiStyles.PanelRito -- vitela ahumada con degradado y
        //    marco de latón con cantoneras. Un gris plano con borde de 1px es
        //    la firma visual de un widget de sistema; el degradado + el metal
        //    es la de un objeto que vive en el taller.
        //  · TÍTULO "BAUTIZO" en Cinzel espaciado: es el único momento del
        //    juego en que el jugador AÑADE algo permanente al mundo. Merece
        //    una capital lapidaria, no una barra de título arrastrable.
        //  · LA MUESTRA EN GRANDE: el swatch pasa de 20 px a 92 px de diseño
        //    y deja de ser un color plano -- se genera con la FIRMA VISUAL
        //    real del material (FirmaVisualFabrica, la misma que pinta las
        //    redomas y el frasco), con su patrón y su animación. El jugador
        //    tiene que estar mirando LA SUSTANCIA mientras la nombra, no un
        //    cuadradito de leyenda.
        //  · LA LÍNEA CEREMONIAL ("El nombre que le des lo verá todo el
        //    taller"): dice la verdad del sistema -- los encargos del Maestro
        //    pasan a pedir el material POR ESE NOMBRE (regla 13) -- y de paso
        //    convierte el acto en una promesa.
        //  · ENTER bautiza. Un rito no se cierra buscando el botón con el
        //    ratón.
        // El campo de texto ya no se reestila aquí: lo hace UiStyles.VestirSkin
        // para TODA la UI de una vez (carboncillo + filo de latón + caret de
        // oro), así que el de la seed del título y el de bautizar
        // procedimientos del diario cambian con él.
        // =================================================================
        private const int WindowId = 837480;
        private const float AnchoDiseno = 420f;
        private const float AltoDiseno = 330f;

        /// <summary>Lado en TÉXELES del lienzo de la muestra grande. Mismo criterio que FlaskHud.SwatchLado (28) pero mayor: aquí el swatch se ve a 92 px de diseño, así que un lienzo pequeño se notaría estirado.</summary>
        private const int MuestraLienzo = 44;

        private AlkahestSim _sim;
        private Flask _flask;
        private SubstanceKnowledge _knowledge;

        private bool _open;
        private byte _targetMat;
        private string _nameField = "";
        private Rect _windowRect;
        private bool _rectColocado;
        private bool _pedirFoco;
        /// <summary>Firma visual EN PALABRAS del material que se está bautizando, cacheada al abrir (DescribirFirma construye un string: llamarlo desde OnGUI sería una asignación por frame).</summary>
        private string _firmaTexto;

        // Muestra: fotogramas cacheados por material (nunca por frame), mismo
        // patrón exacto que FlaskHud.ObtenerFirmaTexturas.
        private readonly Texture2D[][] _muestraTexturas = new Texture2D[MaterialId.Count][];
        private bool[] _esBordeMuestra;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Flask flask, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _flask = flask;
            _knowledge = knowledge;
        }

        /// <summary>Misma disciplina que FlaskHud/JournalHud: las Texture2D creadas por código no se liberan solas al recargar la escena (DayCycle.RestartRun recrea este componente).</summary>
        private void OnDestroy()
        {
            for (int m = 0; m < _muestraTexturas.Length; m++)
            {
                var t = _muestraTexturas[m];
                if (t == null) continue;
                for (int f = 0; f < t.Length; f++) if (t[f] != null) Destroy(t[f]);
            }
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
            _firmaTexto = _sim != null && _sim.Universe != null ? _sim.Universe.DescribirFirma(_targetMat) : null;
            _open = true;
            _pedirFoco = true; // (playtest 31) el rito empieza con el cursor YA dentro del campo: nadie debería tener que hacer clic para escribir.
            UiStyles.EscribiendoTexto = true; // (fix playtest 10) ver doc de clase y de UiStyles.EscribiendoTexto.
        }

        /// <summary>
        /// Confirma el nombre y cierra: el gesto único del rito (botón o
        /// Enter llevan aquí). EL RITO nunca cambia (mismo campo, mismo
        /// Enter, mismo cierre) -- lo único que cambia con la red es A QUIÉN
        /// le pertenece la última palabra.
        ///
        /// (playtest 36, EL CAMINO DEL INVITADO) BAUTIZO DEL INVITADO: en la
        /// escena clásica o si somos el anfitrión, <see cref="SubstanceKnowledge.Bautizar"/>
        /// local sigue siendo la ÚNICA autoridad (ni SimSync ni SaberSync
        /// existen en la escena clásica; el anfitrión ES la autoridad, no
        /// necesita pedirse permiso a sí mismo). Un INVITADO aplica el
        /// bautizo local de inmediato (el "fantasma optimista" de siempre en
        /// este proyecto, ver Net/MaquinaReplica.cs::Reposicionar) para que
        /// el rito se sienta instantáneo, y ADEMÁS manda
        /// <see cref="SaberSync.PedirBautizo"/> -- el anfitrión lo aplica
        /// sobre SU conocimiento (la autoridad real del taller compartido) y
        /// lo devuelve por el registro replicado a todos, este invitado
        /// incluido, que ve su propio nombre "confirmado" en el mismo valor
        /// que ya tenía puesto.
        /// </summary>
        private void Consagrar()
        {
            if (_knowledge == null) return;
            if (string.IsNullOrEmpty(_nameField) || _nameField.Trim().Length == 0)
            {
                Aviso("un nombre vacío no bautiza nada");
                return;
            }
            _knowledge.Bautizar(_targetMat, _nameField);
            if (SimSync.EnSesion && !SimSync.EsServidor) SaberSync.PedirBautizo(_targetMat, _nameField);
            Close();
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

            UiStyles.Preparar();

            float w = UiStyles.S(AnchoDiseno), h = UiStyles.S(AltoDiseno);
            if (!_rectColocado || !Mathf.Approximately(_windowRect.width, w))
            {
                // Se coloca aquí y no en Init porque UiStyles.Escala solo se
                // conoce dentro de OnGUI (depende de Screen.height, que en
                // Init todavía puede ser el del editor sin maximizar).
                _windowRect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
                _rectColocado = true;
            }

            // ENTER = bautizar. Se lee ANTES de la ventana para que el evento
            // no lo consuma el campo de texto (que lo trata como "fin de
            // línea" y lo descarta sin avisar a nadie).
            var e = Event.current;
            if (e != null && e.type == EventType.KeyDown &&
                (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter))
            {
                e.Use();
                Consagrar();
                return;
            }

            // GUIContent.none + estilo propio: sin barra de título del skin
            // (era literalmente la barra de título de un menú de sistema).
            _windowRect = GUI.Window(WindowId, _windowRect, DrawWindow, GUIContent.none, GUIStyle.none);
        }

        private void DrawWindow(int id)
        {
            // (segunda pasada, VISTO JUGANDO) EL ENTER NO LLEGABA. La
            // comprobación de Return vivía SOLO en OnGUI, antes de
            // GUI.Window: con el campo de texto enfocado, IMGUI entrega el
            // KeyDown DENTRO del ámbito de la ventana, así que fuera nunca se
            // veía y el rito solo se podía cerrar con el ratón -- justo lo que
            // este panel venía a evitar. Se comprueba en LOS DOS sitios (el de
            // fuera cubre el caso de ventana sin foco de campo) y se consume
            // el evento en cuanto se atiende.
            var ev = Event.current;
            if (ev != null && ev.type == EventType.KeyDown &&
                (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter))
            {
                ev.Use();
                Consagrar();
                return;
            }

            var r = new Rect(0f, 0f, _windowRect.width, _windowRect.height);
            UiStyles.PanelRito(r);

            float pad = UiStyles.S(22f);
            float x = pad, ancho = r.width - pad * 2f;
            float y = UiStyles.S(18f);

            // ---- TÍTULO: capital lapidaria espaciada, con su filete.
            float altoTitulo = UiStyles.TituloRito.lineHeight;
            GUI.Label(new Rect(x, y, ancho, altoTitulo), UiStyles.Espaciar("BAUTIZO"), UiStyles.TituloRito);
            y += altoTitulo + UiStyles.S(9f);
            UiStyles.FileteRombo(r.width * 0.5f, y, ancho * 0.80f, UiStyles.Laton); // (segunda pasada) LatonOscuro sobre vitela oscura no se veía: el filete es del mismo metal que el marco.

            y += UiStyles.S(14f);

            // ---- LA MUESTRA, EN GRANDE, con su marco de latón: el jugador
            // mira la sustancia mientras la nombra.
            float lado = UiStyles.S(92f);
            var marco = new Rect(x, y, lado, lado);
            UiStyles.Rellenar(marco, new Color(0f, 0f, 0f, 0.55f));
            var dentro = new Rect(marco.x + UiStyles.S(4f), marco.y + UiStyles.S(4f),
                                  marco.width - UiStyles.S(8f), marco.height - UiStyles.S(8f));

            var tex = ObtenerMuestraFrame(_targetMat);
            if (tex != null) GUI.DrawTexture(dentro, tex);
            else UiStyles.Rellenar(dentro, _sim.Universe.Get(_targetMat).baseColor);
            UiStyles.MarcoLaton(marco, UiStyles.Laton, 0.9f);

            // ---- A la derecha de la muestra: nombre actual + firma visual.
            float xTexto = marco.xMax + UiStyles.S(16f);
            float anchoTexto = r.width - pad - xTexto;
            string actual = _knowledge.NombreDe(_targetMat);
            bool sinNombre = actual == "???";

            GUI.Label(new Rect(xTexto, y + UiStyles.S(2f), anchoTexto, UiStyles.TenueCentrado.lineHeight),
                      sinNombre ? "SIN NOMBRE" : "SE LLAMA", UiStyles.CuerpoTenue);

            var estiloNombre = UiStyles.NombreGrande;
            var previo = estiloNombre.normal.textColor;
            estiloNombre.normal.textColor = sinNombre ? UiStyles.TextoTenue : UiStyles.Oro;
            GUI.Label(new Rect(xTexto, y + UiStyles.S(18f), anchoTexto, UiStyles.S(30f)),
                      sinNombre ? "—" : actual, estiloNombre);
            estiloNombre.normal.textColor = previo;

            // La firma visual descrita en palabras ("carmín, manchas lentas,
            // borde escarchado"): la misma línea que enseña el diario, aquí
            // como retrato hablado de lo que se está bautizando.
            // (_firmaTexto se calcula UNA vez al abrir, en TryOpen:
            // DescribirFirma construye un string y llamarlo desde OnGUI sería
            // una asignación por frame -- la regla de oro del proyecto.)
            if (!string.IsNullOrEmpty(_firmaTexto))
            {
                // Anclado ARRIBA (no centrado en un rect alto: quedaba
                // flotando en mitad del hueco, sin relación con el nombre).
                float yFirma = y + UiStyles.S(46f);
                float altoFirma = UiStyles.Alto(UiStyles.Ceremonial, _firmaTexto, anchoTexto);
                GUI.Label(new Rect(xTexto, yFirma, anchoTexto, altoFirma), _firmaTexto, UiStyles.Ceremonial);
            }

            y = marco.yMax + UiStyles.S(16f);

            // ---- EL CAMPO. El estilo (carboncillo, filo de latón, caret de
            // oro) lo pone UiStyles.VestirSkin para toda la UI.
            GUI.Label(new Rect(x, y, ancho, UiStyles.CuerpoTenue.lineHeight), "EL NOMBRE QUE LE PONES", UiStyles.CuerpoTenue);
            y += UiStyles.CuerpoTenue.lineHeight + UiStyles.S(4f);

            float altoCampo = UiStyles.S(30f);
            GUI.SetNextControlName("NamingUiField");
            _nameField = GUI.TextField(new Rect(x, y, ancho, altoCampo), _nameField, 40, UiStyles.Campo);
            if (_pedirFoco)
            {
                _pedirFoco = false;
                GUI.FocusControl("NamingUiField");
            }
            y += altoCampo + UiStyles.S(10f);

            // ---- LA LÍNEA CEREMONIAL.
            float altoCeremonia = UiStyles.Alto(UiStyles.Ceremonial, LineaCeremonial, ancho);
            GUI.Label(new Rect(x, y, ancho, altoCeremonia), LineaCeremonial, UiStyles.Ceremonial);
            y += altoCeremonia + UiStyles.S(12f);

            // ---- LOS DOS GESTOS.
            float altoBoton = UiStyles.S(32f);
            float sep = UiStyles.S(10f);
            float anchoBoton = (ancho - sep) * 0.5f;
            if (GUI.Button(new Rect(x, y, anchoBoton, altoBoton), "Bautizar (Enter)", UiStyles.Boton)) Consagrar();
            if (GUI.Button(new Rect(x + anchoBoton + sep, y, anchoBoton, altoBoton), "Dejarlo así", UiStyles.Boton)) Close();
            y += altoBoton + UiStyles.S(6f);

            GUI.Label(new Rect(x, y, ancho, UiStyles.TenueCentrado.lineHeight), "T / ESC para cerrar", UiStyles.TenueCentrado);

            GUI.DragWindow(new Rect(0f, 0f, r.width, UiStyles.S(46f))); // el título sigue siendo el asa, aunque ya no parezca una barra de sistema.
        }

        private const string LineaCeremonial = "El nombre que le des lo verá todo el taller.";

        // =================================================================
        // LA MUESTRA CON FIRMA VISUAL REAL
        // =================================================================
        // Mismo mecanismo que Game/FlaskHud.cs (que ya lo hacía para sus
        // swatches de 28 téxeles): FirmaVisualFabrica genera los fotogramas
        // UNA vez por material y aquí solo se elige cuál toca mostrar. Nunca
        // se genera nada dentro de OnGUI más allá de la primera vez que se
        // bautiza cada sustancia.
        // =================================================================

        private void PrepararBordeMuestra()
        {
            if (_esBordeMuestra != null) return;
            const int banda = 4; // ~9% del lienzo, misma proporción que FlaskHud (3/28) y JournalHud (3/34).
            _esBordeMuestra = new bool[MuestraLienzo * MuestraLienzo];
            for (int yy = 0; yy < MuestraLienzo; yy++)
            {
                for (int xx = 0; xx < MuestraLienzo; xx++)
                {
                    int d = Mathf.Min(Mathf.Min(xx, MuestraLienzo - 1 - xx), Mathf.Min(yy, MuestraLienzo - 1 - yy));
                    _esBordeMuestra[yy * MuestraLienzo + xx] = d < banda;
                }
            }
        }

        private Texture2D[] ObtenerMuestraTexturas(byte matId)
        {
            if (matId >= _muestraTexturas.Length || _sim == null || _sim.Universe == null) return null;
            var ya = _muestraTexturas[matId];
            if (ya != null) return ya;

            PrepararBordeMuestra();
            var def = _sim.Universe.Get(matId);
            int frames = def.ritmoAnim > 0 ? FirmaVisualFabrica.AnimFrames : 1;
            var texturas = new Texture2D[frames];

            for (int f = 0; f < frames; f++)
            {
                var px = FirmaVisualFabrica.GenerarPixeles(MuestraLienzo, MuestraLienzo, def, f,
                    null, _esBordeMuestra, sobreMundo: false);
                var tex = new Texture2D(MuestraLienzo, MuestraLienzo, TextureFormat.RGBA32, false, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "MuestraBautizo_" + def.devName + "_" + f,
                };
                tex.SetPixels32(px);
                tex.Apply(false, true);
                texturas[f] = tex;
            }

            _muestraTexturas[matId] = texturas;
            return texturas;
        }

        private Texture2D ObtenerMuestraFrame(byte matId)
        {
            var texturas = ObtenerMuestraTexturas(matId);
            if (texturas == null || texturas.Length == 0) return null;
            int idx = Mathf.FloorToInt(Time.time * FirmaVisualFabrica.AnimFps) % texturas.Length;
            return texturas[idx];
        }
    }
}
