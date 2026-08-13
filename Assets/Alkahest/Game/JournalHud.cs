using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Diario del aprendiz, REDISEÑADO POR COMPLETO en el playtest 10: "la
    /// presentación gráfica es muy pobre, incómoda de leer" + "el diario
    /// tiene que ser el sitio donde vuelvo a consultar lo que ya aprendí,
    /// para no depender de mi memoria ni de una captura de pantalla".
    ///
    /// (fix playtest 10) EL DIARIO ES UN LIBRO Y OCUPA LA PANTALLA. Ya no es
    /// una GUILayout.Window de 300x340 arrastrable compitiendo por espacio
    /// con el resto del HUD -- el jugador dio permiso explícito para tapar
    /// todo el taller mientras lee. Al abrirse: velo casi negro a pantalla
    /// completa + un libro de DOS páginas de pergamino apagado, con
    /// GUI.depth forzado por debajo de cualquier otro HUD para que gane
    /// SIEMPRE el orden de dibujado, sea cual sea el orden real (no
    /// garantizado por Unity) en que se ejecuten los OnGUI de los demás
    /// componentes. OJO CONTRAINTUITIVO: en IMGUI, GUI.depth MÁS BAJO se
    /// dibuja MÁS DELANTE (no al revés) -- de ahí -1000 y no +1000.
    ///
    /// El mundo NO se pausa (AlkahestSim.Paused no se toca aquí): es un
    /// juego de observar la sim, y el jugador puede querer dejar algo
    /// cociéndose mientras consulta el libro.
    ///
    /// SECCIONES (pestañas arriba, LEYES/SUSTANCIAS/PROCEDIMIENTOS):
    ///  · LEYES -- (playtest 18, CONTRATO_FASE3.md) REESCRITA para química
    ///    sorteada por semilla: lee `AlkahestSim.Universe.Leyes` DIRECTAMENTE
    ///    (horneado por seed en Sim/Universe.cs -- NUNCA se toca Sim/, solo se
    ///    lee su API pública), un array ya resuelto con forma/condición/banda/
    ///    esDelNucleo por índice ESTABLE, el mismo índice que usan los eventos
    ///    SimEventType.Ley. Ya NO rastrea pares (a,b) con Reactions.TryGet ni
    ///    añade la ley del Vivium a mano (ver ConstruirLeyesDesdeUniverso).
    ///    VISIBILIDAD (decisión de Cesar, "solo lo presenciado, con hueco
    ///    visible"): una ley se enseña sii `SubstanceKnowledge.LeyDescubierta`
    ///    -- el jugador la ha visto OCURRIR, no basta con conocer sus dos
    ///    materiales (criterio viejo, ver ActualizarCache). Las leyes que
    ///    faltan OCUPAN SITIO como huecos idénticos entre sí, en el mismo
    ///    orden estable del array -- el diario es una pregunta ("¿cuántas te
    ///    faltan?"), no un manual. Contador "N de M" en la cabecera de la
    ///    sección (ver _tituloLeyesConContador). Formato "de un vistazo":
    ///    fórmula (entrada) + condición de temperatura (detalle) + distintivo
    ///    ★ SE PROPAGA cuando aplica.
    ///  · SUSTANCIAS -- REHECHA EN EL PLAYTEST 12 como fichas de catálogo:
    ///    "en esta ronda cada sustancia innominada recibe una firma visual
    ///    sorteada por seed... el diario pasa a ser el catálogo de este
    ///    universo concreto". Cada ficha muestra una MINIATURA real (textura
    ///    generada por código que reproduce color+patrón+borde de la firma,
    ///    ver ObtenerMiniatura/CrearMiniatura más abajo, NUNCA un cuadradito
    ///    de color plano), el nombre (bautizado o "???"), la descripción de
    ///    firma de Universe.DescribirFirma, las observaciones que guarda
    ///    SubstanceKnowledge (WitnessOf) y, si sigue innominada, una
    ///    invitación discreta a bautizarla.
    ///  · PROCEDIMIENTOS -- LA SECCIÓN NUEVA que ataca la queja de la
    ///    memoria/capturas de pantalla. Ver el bloque de comentario grande
    ///    sobre HintSystem más abajo (ConstruirEntradaProcedimiento) para la
    ///    explicación completa de por qué esto NO archiva literalmente las
    ///    pistas ya mostradas por Game/HintSystem.cs (ese archivo no expone
    ///    NADA consultable desde fuera) y qué se hizo en su lugar.
    ///
    /// CATALIZADOR/PROPAGACIÓN, la propiedad que de verdad cambia el juego:
    /// una reacción es "catalítica" cuando UNO de los dos lados no cambia
    /// (productX == x) y el otro sí -- exactamente la semántica que ya
    /// documenta Sim/ReactionEngine.cs ("si un producto es igual al material
    /// original, esa celda no cambia"). Con otras palabras: "la semilla no
    /// se gasta". Se marca con un distintivo bien visible (★ SE PROPAGA).
    ///
    /// Nota de glifos: "->" en vez de una flecha Unicode y "★" (no "⟳") a
    /// propósito -- el resto del proyecto solo ha probado en la fuente IMGUI
    /// real "·"/"—"/"★" (ver OrdersHud.cs, DayCycle.cs, HintSystem.cs); un
    /// símbolo sin uso previo en UI de verdad se arriesga a salir como
    /// "tofu". Por la misma razón se evitan "◀"/"▶" para paginar: se usan
    /// las palabras "anterior"/"siguiente".
    ///
    /// NO SE USA GUILayout.Window aquí (a diferencia de la versión anterior
    /// de este archivo): un Window trae cromado gris arrastrable de Unity
    /// que no pega con "un libro que posee la pantalla", y toda la
    /// composición (dos páginas + lomo + paginación real) necesita rects
    /// absolutos para medir con precisión, igual que ya hacen OrdersHud.cs y
    /// FlaskHud.cs.
    /// </summary>
    public sealed class JournalHud : MonoBehaviour
    {
        private enum Seccion { Leyes = 0, Sustancias = 1, Procedimientos = 2 }

        // (playtest 18) Coincide a propósito con Sim/Universe.MaxLeyes ("El
        // diario ya reserva este tamaño", ver el doc de esa constante en el
        // contrato): con química por seed hay entre 13 y 16 leyes reales
        // (7 núcleo + 5-8 sorteadas + 1 crecimiento), y 24 es el tope duro
        // que Universe.Create ya no puede superar (asserted ahí). Antes esta
        // cota se justificaba por "136 pares posibles de 17 materiales" --
        // ya no aplica: ya no se rastrean pares, se lee Universe.Leyes tal cual.
        private const int MaxLeyes = 24;

        private struct LeyDatos
        {
            public byte a, b, productA, productB;
            public bool catalitica;
            public bool soloFrio;
            public bool soloCalor;
            public bool esCrecimiento; // true solo para la ley especial de Vivium (no viene de ReactionEngine).
        }

        /// <summary>
        /// Una fila de cualquier página del libro: dos niveles tipográficos
        /// (Titulo = "entrada", Cuerpo = "detalle") más los adornos que solo
        /// aplican a algunas secciones (miniatura + firma en SUSTANCIAS,
        /// distintivo de propagación en LEYES). Reutilizada por las tres
        /// secciones para que la paginación (ComputePages/FillColumn) sea un
        /// único algoritmo, no tres copias.
        ///
        /// (playtest 12) `Swatch` (Color plano) se sustituye por `MatId` +
        /// `Firma`: la ficha de SUSTANCIAS ya no rellena un cuadradito de
        /// color, dibuja la miniatura cacheada de ese material (ver
        /// DrawEntradaSustancia) y añade la línea de descripción de firma que
        /// expone Universe.DescribirFirma.
        /// </summary>
        private struct Entrada
        {
            public string Titulo;
            public string Cuerpo;
            public bool Propaga;
            public bool TieneSwatch;
            public byte MatId;
            public string Firma;
        }

        // -----------------------------------------------------------------
        // Estado / dependencias.
        // -----------------------------------------------------------------
        private AlkahestSim _sim;
        private SubstanceKnowledge _knowledge;

        private bool _visible;
        private Seccion _seccion = Seccion.Leyes;
        private int _pagina;

        /// <summary>
        /// (fix playtest 10) True mientras el libro tapa la pantalla. Público
        /// para que OTROS sistemas (ApprenticeController, Flask, máquinas...)
        /// puedan dejar de reaccionar a los atajos del mundo mientras se lee
        /// -- ninguno de esos archivos se toca en este encargo (fuera de la
        /// lista de ARCHIVOS MODIFICABLES), así que hoy es un contrato
        /// expuesto y no consumido todavía: dejarlo enganchado en cada uno
        /// de ellos queda pendiente para quien SÍ pueda tocarlos.
        /// </summary>
        public static bool Abierto { get; private set; }

        // Estructura de leyes: se calcula UNA sola vez en Init (playtest 18:
        // Universe.Leyes de este universo no cambia durante la partida). El
        // TEXTO de cada entrada sí depende de nombres bautizables y de si la
        // ley se ha presenciado, así que se cachea aparte y solo se
        // reconstruye cuando cambia el estado de conocimiento del jugador
        // (ver ActualizarCache) -- nunca se reconstruyen strings en cada
        // frame de OnGUI si nada cambió.
        private readonly LeyDatos[] _leyes = new LeyDatos[MaxLeyes];
        private int _leyesCount;

        private readonly Entrada[] _entradasLeyes = new Entrada[MaxLeyes];
        private int _entradasLeyesCount;
        private readonly Entrada[] _entradasProcedimientos = new Entrada[MaxLeyes];
        private int _entradasProcedimientosCount;
        private readonly Entrada[] _entradasSustancias = new Entrada[MaterialId.Count];
        private int _entradasSustanciasCount;
        private int _cacheFirma = int.MinValue;

        // (playtest 18) "N de M" de la sección LEYES: cabecera bien visible
        // (DrawContenido la usa en vez de la constante "LEYES" cuando
        // _seccion==Leyes), NO el pie de página -- el pie ya está ocupado por
        // la paginación real (DrawPie) y solo se dibuja si hay más de una
        // página; el contador tiene que verse SIEMPRE que estés en la sección,
        // aunque quepa entera en una página. Se reconstruye en ActualizarCache
        // (mismo disparador que el resto de texto cacheado: ahí es donde
        // CountLeyesDescubiertas() ya se necesita para decidir qué huecos
        // dibujar, así que el contador sale gratis del mismo cálculo).
        private string _tituloLeyesConContador = "LEYES";

        // -----------------------------------------------------------------
        // Paginación real (fix playtest 10): se recalcula en cada OnGUI
        // mientras el libro está visible -- igual que OrdersHud/FlaskHud
        // MIDEN su contenido cacheado cada frame con UiStyles.Alto (que no
        // asigna: reutiliza un GUIContent estático). Lo único que se cachea
        // agresivamente son los STRINGS (ActualizarCache); la geometría de
        // qué entrada cae en qué página es barata de recalcular (<=24
        // entradas) y así siempre está sincronizada con la resolución
        // actual sin un segundo mecanismo de invalidación.
        // -----------------------------------------------------------------
        private readonly int[] _pageLeftStart = new int[MaxLeyes + 2];
        private readonly int[] _pageRightStart = new int[MaxLeyes + 2];
        private readonly int[] _pageRightEnd = new int[MaxLeyes + 2];
        private int _pageCount = 1;

        private int _pieFirma = int.MinValue;
        private string _pieTexto = "";

        // -----------------------------------------------------------------
        // FIRMA VISUAL DEL UNIVERSO (playtest 12): "que se note que este
        // universo es OTRO". Construido UNA vez en Init (CaracterDelUniverso
        // y Seed son inmutables durante toda la partida, igual que _leyes),
        // nunca reconstruido en OnGUI. Dibujado sobrio, una línea, bajo el
        // título de tapa (ver DrawCabecera) -- "una línea, no una portada".
        // -----------------------------------------------------------------
        private string _tituloUniverso = "";

        // -----------------------------------------------------------------
        // MINIATURAS DE CATÁLOGO (playtest 12): una Texture2D por material,
        // generada UNA sola vez (la primera vez que se pide, ver
        // ObtenerMiniatura) y cacheada aquí para siempre -- igual criterio
        // que _entradasSustancias: nunca se reconstruye una textura en
        // OnGUI/por frame. Tamaño de índice MaterialId.Count, igual que el
        // resto de arrays por-material de SubstanceKnowledge.
        //
        // MEMORIA (playtest 12, encargo): un Texture2D creado con `new
        // Texture2D(...)` vive FUERA del árbol de la escena -- destruir este
        // GameObject (o el reload de escena que DayCycle.RestartRun dispara
        // con SceneManager.LoadScene al empezar cada universo nuevo, ver
        // AlkahestGameBootstrap.SpawnJournalHud: crea un JournalHud NUEVO
        // cada vez, no sobrevive entre partidas) NO libera la textura sola.
        // Sin el Destroy() explícito de OnDestroy (más abajo) se acumularían
        // huérfanas, una tanda de hasta MaterialId.Count texturas más por
        // cada "empezar otro universo" de la sesión. Son pocas y pequeñas
        // (17 materiales * MiniLado² RGBA32 ≈ 53 KB en total) pero la
        // disciplina es la misma que si fueran grandes.
        // -----------------------------------------------------------------
        private readonly Texture2D[] _miniaturas = new Texture2D[MaterialId.Count];

        // -----------------------------------------------------------------
        // Estilos propios (cacheados, reconstruidos solo si cambia la
        // resolución -- mismo criterio que UiStyles.Preparar). El libro
        // necesita niveles que UiStyles no tiene ya hechos (título de tapa,
        // pestaña, título de sección/capítulo), así que se crean aquí, NUNCA
        // dentro de OnGUI en cada frame.
        // -----------------------------------------------------------------
        private GUIStyle _estiloTituloLibro;
        private GUIStyle _estiloSubtituloUniverso;
        private GUIStyle _estiloPestana;
        private GUIStyle _estiloTituloSeccion;
        private GUIStyle _estiloEntradaTitulo;
        private GUIStyle _estiloFirma;
        private GUIStyle _estiloDetalle;
        private GUIStyle _estiloBadgePropaga;
        private GUIStyle _estiloPie;
        private GUIStyle _estiloAyudaPie;
        private GUIStyle _estiloBotonPagina;
        private int _alturaEstilos = -1;

        /// <summary>Lado (px de diseño, ver UiStyles.S) de la miniatura de catálogo en la ficha de SUSTANCIAS.</summary>
        private const float MiniSwatchLado = 34f;

        /// <summary>Lado en píxeles REALES de la textura generada (pequeña a propósito: es un icono de catálogo, no arte de detalle).</summary>
        private const int MiniLado = 30;

        private const string TextoInvitaBautizo = "todavía sin nombre — cierra el diario y bautízala con T";

        // Pergamino apagado y lomo: coherentes con la paleta ciruela/latón
        // del taller (UiStyles.Tinta/Oro), NO blanco puro -- el reporte pide
        // explícitamente evitar quemar los ojos en un juego oscuro.
        private static readonly Color _velo = new Color(0.02f, 0.015f, 0.03f, 0.86f);
        private static readonly Color _papel = new Color(0.30f, 0.24f, 0.18f, 1f);
        private static readonly Color _papelBorde = new Color(0.58f, 0.47f, 0.30f, 0.30f);
        private static readonly Color _lomo = new Color(0.09f, 0.07f, 0.06f, 1f);

        private static readonly string[] _tituloSeccion = { "LEYES", "SUSTANCIAS", "PROCEDIMIENTOS" };

        // (playtest 18) TextoVacioLeyes queda como fallback puramente DEFENSIVO: con el
        // criterio de huecos (ver ConstruirLeyesDesdeUniverso/ActualizarCache) la sección
        // LEYES siempre pinta Universe.Leyes.Length renglones -- reales o huecos idénticos
        // "??? -- ley aún no presenciada" -- así que count==0 solo puede darse si el
        // universo todavía no ha cargado ninguna ley (un frame muy temprano). El texto ya
        // no habla de "combinar materiales para desvelarlas": el criterio nuevo es
        // PRESENCIAR la ley, no conocer sus dos materiales por separado (ver punto 6).
        private const string TextoVacioLeyes = "(el libro todavía no conoce las leyes de este universo)";
        private const string TextoVacioSustancias = "(nada descubierto todavía: aspira, vierte o mantén el cursor un instante sobre algo)";

        // (playtest 18) PROCEDIMIENTOS usa el MISMO criterio nuevo que LEYES
        // (LeyDescubierta), pero SIN huecos -- una "receta" en blanco no genera curiosidad,
        // solo ruido (el catálogo de LEYES ya es el sitio que cuenta "cuántas te faltan").
        // El texto viejo ("aparecen solos en cuanto descubres los dos materiales de una
        // ley") describía el criterio ANTERIOR y ya no es cierto: ahora hace falta
        // PRESENCIAR la ley, no solo conocer sus dos materiales por separado.
        private const string TextoVacioProcedimientos = "(sin procedimientos archivados todavía: aparecen solos en cuanto presencias esa ley ocurrir)";

        // (playtest 18) EL HUECO: renglón que ocupa el sitio de una ley que el jugador
        // TODAVÍA no ha presenciado (ver ConstruirLeyesDesdeUniverso/ActualizarCache,
        // decisión de Cesar "solo lo presenciado, con hueco visible"). Un mismo texto
        // SIEMPRE, sin excepción -- si el hueco cambiara según la ley oculta (aunque fuera
        // solo la longitud o la forma), estaría filtrando información de una ley que el
        // jugador no ha visto ocurrir. Sin distintivo ★ SE PROPAGA tampoco (revelaría si es
        // catalítica): Propaga se deja explícitamente en false donde se construye.
        private const string TextoHuecoLeyTitulo = "??? — ley aún no presenciada";
        private const string TextoHuecoLeyCuerpo = "Ocurre en algún punto de este universo. Tendrás que verla pasar.";

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _knowledge = knowledge;
            ConstruirLeyesDesdeUniverso();

            // (playtest 12) Ver doc del campo _tituloUniverso: construido aquí,
            // una única vez, nunca en OnGUI -- CaracterDelUniverso y Seed no
            // cambian durante la partida.
            if (_sim != null && _sim.Universe != null)
            {
                _tituloUniverso = $"{_sim.Universe.CaracterDelUniverso} · seed {_sim.Universe.Seed}";
            }
        }

        /// <summary>
        /// (playtest 12) Ver doc del campo _miniaturas: un Texture2D creado por
        /// código no se libera solo con destruir este GameObject. Este
        /// componente se recrea entero (AlkahestGameBootstrap.SpawnJournalHud)
        /// cada vez que DayCycle.RestartRun recarga la escena para un universo
        /// nuevo, así que sin esto cada partida nueva dejaría huérfana la tanda
        /// de miniaturas de la anterior.
        /// </summary>
        private void OnDestroy()
        {
            for (int i = 0; i < _miniaturas.Length; i++)
            {
                if (_miniaturas[i] != null) Destroy(_miniaturas[i]);
            }
        }

        private void Update()
        {
            if (DayCycle.InputLocked)
            {
                // (fix playtest 10) Si una jornada termina con el libro
                // abierto, no lo dejamos "fantasma" tapando medio dibujo bajo
                // el overlay de DayCycle: se cierra solo. Así Abierto nunca
                // miente diciendo que el diario posee la pantalla cuando en
                // realidad el overlay de jornada es quien manda.
                _visible = false;
                Abierto = false;
                return;
            }

            // (fix playtest 10, regla del proyecto) Ningún atajo de una sola
            // tecla puede robarle letras al campo de bautizar (T). J/ESC/
            // AvPág/RePág se callan mientras se escribe un nombre.
            if (UiStyles.EscribiendoTexto)
            {
                Abierto = _visible;
                return;
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.jKey.wasPressedThisFrame)
                {
                    _visible = !_visible;
                    if (_visible) _pagina = 0; // recién abierto: siempre la primera página de la sección actual.
                }
                else if (_visible && kb.escapeKey.wasPressedThisFrame)
                {
                    _visible = false;
                }
                else if (_visible)
                {
                    if (kb.pageDownKey.wasPressedThisFrame) CambiarPagina(1);
                    else if (kb.pageUpKey.wasPressedThisFrame) CambiarPagina(-1);
                }
            }

            Abierto = _visible;
        }

        private void CambiarPagina(int delta)
        {
            // El límite superior real (_pageCount de la sección actual) solo
            // se conoce tras medir en OnGUI; aquí solo se evita bajar de 0.
            // DrawPie/DrawContenido acotan el resultado contra _pageCount.
            _pagina = Mathf.Max(0, _pagina + delta);
        }

        private void CambiarSeccion(Seccion s)
        {
            if (s == _seccion) return;
            _seccion = s;
            _pagina = 0; // cambiar de capítulo siempre vuelve a su primera página.
        }

        private void OnGUI()
        {
            if (DayCycle.InputLocked) return; // los overlays de jornada ya tapan la pantalla entera; no competir con ellos.
            if (!_visible) return;
            if (_sim == null || _sim.Universe == null || _knowledge == null) return;

            UiStyles.Preparar();
            ConstruirEstilosPropios();
            ActualizarCache();

            // (fix playtest 10) Ver docblock de la clase: valor BAJO = se
            // dibuja DELANTE. El resto de HUD (Ordenes/Frasco/Pistas/banner
            // de ley/overlays de DayCycle) nunca toca GUI.depth, así que se
            // quedan en el 0 por defecto -- cualquier valor negativo aquí
            // basta, pero se deja un margen amplio (-1000) por si en el
            // futuro alguien más decide competir por primer plano.
            GUI.depth = -1000;

            UiStyles.Rellenar(new Rect(0f, 0f, Screen.width, Screen.height), _velo);
            DrawBook();
        }

        // ===================================================================
        // MAQUETA DEL LIBRO
        // ===================================================================

        private void DrawBook()
        {
            // Margen generoso: el libro ocupa la MAYOR PARTE de la pantalla,
            // no toda -- un margen visible es lo que vende "hay un mundo
            // detrás, oscurecido" en vez de "esto es una pantalla de menú".
            float margenX = UiStyles.S(72f);
            float margenY = UiStyles.S(50f);
            var libro = new Rect(margenX, margenY, Screen.width - margenX * 2f, Screen.height - margenY * 2f);

            // Tapa: mismo lenguaje visual que el resto del HUD (Tinta +
            // filete dorado), pero es el ÚNICO elemento en pantalla mientras
            // el libro está abierto, así que se permite un panel de fondo
            // más opaco que los paneles satélite de Ordenes/Frasco.
            UiStyles.Panel(libro, UiStyles.TintaFuerte, UiStyles.Oro);

            float padTapa = UiStyles.S(20f);
            var interior = new Rect(libro.x + padTapa, libro.y + padTapa, libro.width - padTapa * 2f, libro.height - padTapa * 2f);

            float altoCabecera = _estiloTituloLibro.lineHeight + UiStyles.S(2f) + _estiloSubtituloUniverso.lineHeight +
                                  UiStyles.S(6f) + _estiloPestana.lineHeight + UiStyles.S(10f);
            float altoPie = _estiloPie.lineHeight + UiStyles.S(4f) + _estiloAyudaPie.lineHeight + UiStyles.S(10f);
            float anchoLomo = UiStyles.S(20f);

            var cabecera = new Rect(interior.x, interior.y, interior.width, altoCabecera);
            var pie = new Rect(interior.x, interior.yMax - altoPie, interior.width, altoPie);
            var paginas = new Rect(interior.x, cabecera.yMax + UiStyles.S(8f), interior.width, pie.y - cabecera.yMax - UiStyles.S(16f));

            float anchoColumna = (paginas.width - anchoLomo) * 0.5f;
            var paginaIzq = new Rect(paginas.x, paginas.y, anchoColumna, paginas.height);
            var paginaDer = new Rect(paginas.x + anchoColumna + anchoLomo, paginas.y, anchoColumna, paginas.height);

            DrawCabecera(cabecera);

            UiStyles.Panel(paginaIzq, _papel, _papelBorde);
            UiStyles.Panel(paginaDer, _papel, _papelBorde);
            DrawLomo(new Rect(paginaIzq.xMax, paginas.y, anchoLomo, paginas.height));

            // Orden de llamada importa: DrawContenido calcula _pageCount y
            // acota _pagina para la sección visible ANTES de que DrawPie los
            // lea. El orden VISUAL (pie abajo del todo) no depende de esto,
            // cada uno dibuja en su propio rect sin solaparse.
            DrawContenido(paginaIzq, paginaDer);
            DrawPie(pie);
        }

        private void DrawCabecera(Rect r)
        {
            float altoTitulo = _estiloTituloLibro.lineHeight;
            GUI.Label(new Rect(r.x, r.y, r.width, altoTitulo), "DIARIO DEL APRENDIZ", _estiloTituloLibro);

            // (playtest 12) "Que se note que este universo es OTRO": una línea
            // sobria, cacheada en Init (_tituloUniverso), nunca reconstruida
            // aquí -- carácter del universo + seed, para que el jugador pueda
            // decir "en la partida del mundo carmín pasaba esto".
            float altoSubtitulo = _estiloSubtituloUniverso.lineHeight;
            float ySubtitulo = r.y + altoTitulo + UiStyles.S(2f);
            GUI.Label(new Rect(r.x, ySubtitulo, r.width, altoSubtitulo), _tituloUniverso, _estiloSubtituloUniverso);

            float yTabs = ySubtitulo + altoSubtitulo + UiStyles.S(6f);
            float altoTabs = r.yMax - yTabs;
            float anchoTab = r.width / 3f;

            for (int s = 0; s < 3; s++)
            {
                var tabRect = new Rect(r.x + anchoTab * s, yTabs, anchoTab, altoTabs);
                bool activa = (int)_seccion == s;

                if (activa)
                {
                    float grosor = Mathf.Max(2f, UiStyles.S(2f));
                    UiStyles.Rellenar(new Rect(tabRect.x + UiStyles.S(14f), tabRect.yMax - grosor, tabRect.width - UiStyles.S(28f), grosor), UiStyles.Oro);
                }

                // Mismo truco que UiStyles.Globo/PlacaMundo: se pinta el
                // color a mano sobre el estilo compartido justo antes de
                // usarlo, en vez de mantener 3 GUIStyle idénticos salvo el
                // color (uno por pestaña) solo para evitar esta mutación.
                _estiloPestana.normal.textColor = activa ? UiStyles.Oro : UiStyles.TextoTenue;
                if (GUI.Button(tabRect, _tituloSeccion[s], _estiloPestana))
                {
                    CambiarSeccion((Seccion)s);
                }
            }
        }

        private void DrawLomo(Rect r)
        {
            UiStyles.Rellenar(r, _lomo);
            float grosor = Mathf.Max(1f, Mathf.Round(UiStyles.Escala));
            float centro = r.x + r.width * 0.5f;
            UiStyles.Rellenar(new Rect(centro - grosor * 0.5f, r.y, grosor, r.height), UiStyles.OroTenue);
        }

        private void DrawPie(Rect r)
        {
            int total = Mathf.Max(1, _pageCount);
            int actual = Mathf.Clamp(_pagina, 0, total - 1);
            _pagina = actual;

            // Texto de paginación: SÍ cambia cada vez que el jugador pasa de
            // página, pero solo entonces -- se cachea contra una firma
            // barata (actual*1000+total) para no reconstruir el string en
            // los frames en que no cambia nada, igual que ActualizarCache.
            int firma = actual * 1000 + total;
            if (firma != _pieFirma)
            {
                _pieFirma = firma;
                _pieTexto = total <= 1 ? "página única" : "página " + (actual + 1) + " de " + total;
            }

            float altoFila = _estiloPie.lineHeight;
            var filaNav = new Rect(r.x, r.y, r.width, altoFila);
            var filaAyuda = new Rect(r.x, filaNav.yMax + UiStyles.S(4f), r.width, _estiloAyudaPie.lineHeight);

            float anchoBoton = UiStyles.S(96f);
            bool puedeAtras = actual > 0;
            bool puedeAdelante = actual < total - 1;

            GUI.enabled = puedeAtras;
            if (GUI.Button(new Rect(filaNav.x, filaNav.y, anchoBoton, altoFila), "< anterior", _estiloBotonPagina) && puedeAtras)
            {
                CambiarPagina(-1);
            }
            GUI.enabled = puedeAdelante;
            if (GUI.Button(new Rect(filaNav.xMax - anchoBoton, filaNav.y, anchoBoton, altoFila), "siguiente >", _estiloBotonPagina) && puedeAdelante)
            {
                CambiarPagina(1);
            }
            GUI.enabled = true;

            GUI.Label(new Rect(filaNav.x + anchoBoton, filaNav.y, filaNav.width - anchoBoton * 2f, altoFila), _pieTexto, _estiloPie);
            GUI.Label(filaAyuda, "J / ESC — cerrar · Re Pág / Av Pág — cambiar de página", _estiloAyudaPie);
        }

        private void DrawContenido(Rect izq, Rect der)
        {
            ObtenerSeccionActual(out Entrada[] entradas, out int count, out string vacio);

            // Márgenes REALES dentro de cada página (fix playtest 10: el
            // reporte pide "aire", no menos contenido) + un encabezado de
            // capítulo repetido en las dos páginas del tramo, como en un
            // libro de verdad con cabecera corrida.
            float padPagina = UiStyles.S(16f);
            var colIzq = new Rect(izq.x + padPagina, izq.y + padPagina, izq.width - padPagina * 2f, izq.height - padPagina * 2f);
            var colDer = new Rect(der.x + padPagina, der.y + padPagina, der.width - padPagina * 2f, der.height - padPagina * 2f);

            float altoEncabezado = _estiloTituloSeccion.lineHeight + UiStyles.S(10f);
            // (playtest 18) La sección LEYES sustituye el título fijo por la versión con
            // contador "N de M" cacheada en ActualizarCache (ver doc de
            // _tituloLeyesConContador) -- bien visible, en la cabecera de la sección, no en
            // el pie de página (que solo aparece con más de una página).
            string tituloSeccion = _seccion == Seccion.Leyes ? _tituloLeyesConContador : _tituloSeccion[(int)_seccion];
            GUI.Label(new Rect(colIzq.x, colIzq.y, colIzq.width, _estiloTituloSeccion.lineHeight), tituloSeccion, _estiloTituloSeccion);
            GUI.Label(new Rect(colDer.x, colDer.y, colDer.width, _estiloTituloSeccion.lineHeight), tituloSeccion, _estiloTituloSeccion);

            var cuerpoIzq = new Rect(colIzq.x, colIzq.y + altoEncabezado, colIzq.width, colIzq.height - altoEncabezado);
            var cuerpoDer = new Rect(colDer.x, colDer.y + altoEncabezado, colDer.width, colDer.height - altoEncabezado);

            if (count == 0)
            {
                GUI.Label(cuerpoIzq, vacio, _estiloDetalle);
                _pageCount = 1;
                return;
            }

            // El distintivo ★ SE PROPAGA se muestra como línea propia SOLO en
            // LEYES (la fórmula compacta no lo dice de otra forma): en
            // PROCEDIMIENTOS ya viene dicho en prosa dentro del propio paso
            // ("★ SE PROPAGA — el X no se gasta..."), repetirlo como
            // distintivo aparte sería ruido, no aire.
            bool mostrarBadge = _seccion == Seccion.Leyes;

            ComputePages(entradas, count, cuerpoIzq.width, cuerpoIzq.height, mostrarBadge);

            int pagina = Mathf.Clamp(_pagina, 0, _pageCount - 1);
            _pagina = pagina;

            DrawColumna(cuerpoIzq, entradas, _pageLeftStart[pagina], _pageRightStart[pagina], mostrarBadge);
            DrawColumna(cuerpoDer, entradas, _pageRightStart[pagina], _pageRightEnd[pagina], mostrarBadge);
        }

        private void ObtenerSeccionActual(out Entrada[] entradas, out int count, out string vacio)
        {
            switch (_seccion)
            {
                case Seccion.Sustancias:
                    entradas = _entradasSustancias;
                    count = _entradasSustanciasCount;
                    vacio = TextoVacioSustancias;
                    break;
                case Seccion.Procedimientos:
                    entradas = _entradasProcedimientos;
                    count = _entradasProcedimientosCount;
                    vacio = TextoVacioProcedimientos;
                    break;
                default:
                    entradas = _entradasLeyes;
                    count = _entradasLeyesCount;
                    vacio = TextoVacioLeyes;
                    break;
            }
        }

        // -----------------------------------------------------------------
        // Paginación real: empaqueta entradas en columnas de alto fijo
        // (medido con word-wrap real, UiStyles.Alto) hasta que la siguiente
        // no cabe, y de ahí en páginas de dos columnas. Garantiza SIEMPRE
        // progreso (al menos una entrada por columna) para no entrar en
        // bucle si una entrada aislada fuera más alta que la columna entera.
        // -----------------------------------------------------------------
        private void ComputePages(Entrada[] entradas, int count, float ancho, float altoDisponible, bool badge)
        {
            _pageCount = 0;
            int idx = 0;
            while (idx < count && _pageCount < _pageLeftStart.Length)
            {
                int leftStart = idx;
                int leftEnd = FillColumn(entradas, leftStart, count, ancho, altoDisponible, badge);
                int rightStart = leftEnd;
                int rightEnd = FillColumn(entradas, rightStart, count, ancho, altoDisponible, badge);

                _pageLeftStart[_pageCount] = leftStart;
                _pageRightStart[_pageCount] = rightStart;
                _pageRightEnd[_pageCount] = rightEnd;
                _pageCount++;

                idx = rightEnd;
            }
            if (_pageCount == 0) _pageCount = 1; // defensivo: no debería pasar con count>0, pero nunca 0 páginas.
        }

        private int FillColumn(Entrada[] entradas, int start, int count, float ancho, float altoDisponible, bool badge)
        {
            if (start >= count) return start;

            float usado = EntradaAltura(entradas[start], ancho, badge);
            int i = start + 1;
            float espacio = UiStyles.S(14f); // "aire" entre entradas -- el problema del playtest era de aire, no de contenido.

            while (i < count)
            {
                float extra = espacio + EntradaAltura(entradas[i], ancho, badge);
                if (usado + extra > altoDisponible) break;
                usado += extra;
                i++;
            }
            return i;
        }

        private float EntradaAltura(Entrada e, float ancho, bool badge)
        {
            // (playtest 12) Ficha de catálogo (SUSTANCIAS): miniatura a la
            // izquierda + columna de texto (título/firma/observaciones) a la
            // derecha -- la fila mide lo que sea más alto de los dos (el
            // swatch tiene lado fijo, el texto crece con el contenido).
            if (e.TieneSwatch)
            {
                float ladoSwatch = UiStyles.S(MiniSwatchLado);
                float anchoTexto = ancho - ladoSwatch - UiStyles.S(10f);

                float h = UiStyles.Alto(_estiloEntradaTitulo, e.Titulo, anchoTexto);
                if (!string.IsNullOrEmpty(e.Firma)) h += UiStyles.S(2f) + UiStyles.Alto(_estiloFirma, e.Firma, anchoTexto);
                if (!string.IsNullOrEmpty(e.Cuerpo)) h += UiStyles.S(3f) + UiStyles.Alto(_estiloDetalle, e.Cuerpo, anchoTexto);
                return Mathf.Max(h, ladoSwatch);
            }

            float hOtros = UiStyles.Alto(_estiloEntradaTitulo, e.Titulo, ancho);
            if (badge && e.Propaga) hOtros += UiStyles.S(2f) + _estiloBadgePropaga.lineHeight;
            if (!string.IsNullOrEmpty(e.Cuerpo)) hOtros += UiStyles.S(3f) + UiStyles.Alto(_estiloDetalle, e.Cuerpo, ancho);
            return hOtros;
        }

        private void DrawColumna(Rect r, Entrada[] entradas, int start, int end, bool badge)
        {
            // BeginGroup recorta cualquier desbordamiento residual: la
            // paginación ya está pensada para que nunca haga falta, pero un
            // clip defensivo asegura "nada de texto que se sale" incluso en
            // un caso límite no previsto.
            GUI.BeginGroup(r);
            float y = 0f;
            float espacio = UiStyles.S(14f);
            for (int i = start; i < end; i++)
            {
                float alto = EntradaAltura(entradas[i], r.width, badge);
                DrawEntrada(new Rect(0f, y, r.width, alto), entradas[i], badge);
                y += alto + espacio;
            }
            GUI.EndGroup();
        }

        private void DrawEntrada(Rect r, Entrada e, bool badge)
        {
            // (playtest 12) La ficha de SUSTANCIAS tiene maqueta propia
            // (miniatura + columna de texto): se despacha aparte en vez de
            // meter más ramas condicionales en este método, que sigue
            // sirviendo tal cual a LEYES/PROCEDIMIENTOS.
            if (e.TieneSwatch)
            {
                DrawEntradaSustancia(r, e);
                return;
            }

            float y = r.y;

            float altoTitulo = UiStyles.Alto(_estiloEntradaTitulo, e.Titulo, r.width);
            GUI.Label(new Rect(r.x, y, r.width, altoTitulo), e.Titulo, _estiloEntradaTitulo);
            y += altoTitulo;

            if (badge && e.Propaga)
            {
                y += UiStyles.S(2f);
                GUI.Label(new Rect(r.x, y, r.width, _estiloBadgePropaga.lineHeight), "★ SE PROPAGA", _estiloBadgePropaga);
                y += _estiloBadgePropaga.lineHeight;
            }

            if (!string.IsNullOrEmpty(e.Cuerpo))
            {
                y += UiStyles.S(3f);
                float altoDetalle = UiStyles.Alto(_estiloDetalle, e.Cuerpo, r.width);
                GUI.Label(new Rect(r.x, y, r.width, altoDetalle), e.Cuerpo, _estiloDetalle);
            }
        }

        /// <summary>
        /// Ficha de catálogo de una sustancia (playtest 12): miniatura real de
        /// su firma visual a la izquierda (ver ObtenerMiniatura -- generada y
        /// cacheada una única vez, jamás por frame) + columna de texto a la
        /// derecha con nombre / firma descrita / observaciones (con la
        /// invitación a bautizar ya integrada en Cuerpo, ver ActualizarCache).
        /// </summary>
        private void DrawEntradaSustancia(Rect r, Entrada e)
        {
            float ladoSwatch = UiStyles.S(MiniSwatchLado);
            float gap = UiStyles.S(10f);
            float anchoTexto = r.width - ladoSwatch - gap;

            var swatchRect = new Rect(r.x, r.y, ladoSwatch, ladoSwatch);
            var miniatura = ObtenerMiniatura(e.MatId);
            if (miniatura != null)
            {
                GUI.DrawTexture(swatchRect, miniatura);
            }
            else
            {
                // Defensivo: no debería pasar nunca en el flujo normal (todo
                // matId de una Entrada con TieneSwatch viene de un material
                // real del universo), pero un color plano de repuesto es
                // mejor que un hueco vacío si algún día algo falla aquí.
                UiStyles.Rellenar(swatchRect, UiStyles.TextoTenue);
            }

            float xTexto = r.x + ladoSwatch + gap;
            float y = r.y;

            float altoTitulo = UiStyles.Alto(_estiloEntradaTitulo, e.Titulo, anchoTexto);
            GUI.Label(new Rect(xTexto, y, anchoTexto, altoTitulo), e.Titulo, _estiloEntradaTitulo);
            y += altoTitulo;

            if (!string.IsNullOrEmpty(e.Firma))
            {
                y += UiStyles.S(2f);
                float altoFirma = UiStyles.Alto(_estiloFirma, e.Firma, anchoTexto);
                GUI.Label(new Rect(xTexto, y, anchoTexto, altoFirma), e.Firma, _estiloFirma);
                y += altoFirma;
            }

            if (!string.IsNullOrEmpty(e.Cuerpo))
            {
                y += UiStyles.S(3f);
                float altoCuerpo = UiStyles.Alto(_estiloDetalle, e.Cuerpo, anchoTexto);
                GUI.Label(new Rect(xTexto, y, anchoTexto, altoCuerpo), e.Cuerpo, _estiloDetalle);
            }
        }

        // ===================================================================
        // ESTILOS PROPIOS (cacheados; ver docs del campo _alturaEstilos)
        // ===================================================================
        private void ConstruirEstilosPropios()
        {
            if (_alturaEstilos == Screen.height && _estiloTituloLibro != null) return;
            _alturaEstilos = Screen.height;

            var raiz = GUI.skin.label;

            _estiloTituloLibro = NuevoEstilo(raiz, 21, FontStyle.Bold, TextAnchor.UpperCenter, UiStyles.Oro, false);
            // (playtest 12) Línea de carácter del universo, bajo el título de
            // tapa -- ver DrawCabecera. Tenue a propósito: es un dato de
            // contexto, no compite con "DIARIO DEL APRENDIZ".
            _estiloSubtituloUniverso = NuevoEstilo(raiz, 12, FontStyle.Italic, TextAnchor.UpperCenter, UiStyles.OroTenue, false);
            _estiloPestana = NuevoEstilo(raiz, 14, FontStyle.Bold, TextAnchor.MiddleCenter, UiStyles.TextoTenue, false);
            // Nivel 1: título de sección/capítulo, repetido como cabecera
            // corrida en ambas páginas del tramo.
            _estiloTituloSeccion = NuevoEstilo(raiz, 15, FontStyle.Bold, TextAnchor.UpperLeft, UiStyles.OroTenue, false);
            // Nivel 2: entrada (una ley, un procedimiento, una sustancia).
            _estiloEntradaTitulo = NuevoEstilo(raiz, 15, FontStyle.Bold, TextAnchor.UpperLeft, UiStyles.Texto, true);
            // (playtest 12) Línea de firma visual en la ficha de SUSTANCIAS
            // ("carmín, manchas lentas, borde escarchado") -- entre el título y
            // las observaciones, un nivel tipográfico propio pero tenue: es
            // descripción de catálogo, no el dato más importante de la fila.
            _estiloFirma = NuevoEstilo(raiz, 12, FontStyle.Italic, TextAnchor.UpperLeft, UiStyles.OroTenue, true);
            // Nivel 3: detalle (condición, pasos, observaciones).
            _estiloDetalle = NuevoEstilo(raiz, 13, FontStyle.Normal, TextAnchor.UpperLeft, UiStyles.TextoTenue, true);
            _estiloBadgePropaga = NuevoEstilo(raiz, 12, FontStyle.Bold, TextAnchor.UpperRight, UiStyles.Exito, false);
            _estiloPie = NuevoEstilo(raiz, 13, FontStyle.Normal, TextAnchor.MiddleCenter, UiStyles.TextoTenue, false);
            _estiloAyudaPie = NuevoEstilo(raiz, 11, FontStyle.Normal, TextAnchor.MiddleCenter, UiStyles.TextoTenue, false);
            _estiloBotonPagina = NuevoEstilo(raiz, 13, FontStyle.Bold, TextAnchor.MiddleCenter, UiStyles.TextoTenue, false);
        }

        /// <summary>Mismo patrón que el helper privado UiStyles.Etiqueta (no accesible desde aquí): estilo limpio, sin padding/margin heredado del skin, wordWrap explícito.</summary>
        private static GUIStyle NuevoEstilo(GUIStyle raiz, int tam, FontStyle fuente, TextAnchor anclaje, Color color, bool ajustar)
        {
            var s = new GUIStyle(raiz)
            {
                fontSize = UiStyles.F(tam),
                fontStyle = fuente,
                alignment = anclaje,
                wordWrap = ajustar,
                richText = false,
                clipping = TextClipping.Overflow,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            s.normal.textColor = color;
            s.hover.textColor = color;
            s.active.textColor = color;
            return s;
        }

        // ===================================================================
        // DATOS: leyes desde Universe.Leyes (playtest 18, reescrito para
        // química sorteada por semilla -- ver doc de ConstruirLeyesDesdeUniverso).
        // ===================================================================

        /// <summary>
        /// (playtest 18, CONTRATO_FASE3.md) Vuelca <c>_sim.Universe.Leyes</c> a
        /// <see cref="_leyes"/> UNA vez desde Init: el array de leyes de un
        /// universo ya creado no cambia jamás durante la partida (Sim/Universe.cs
        /// lo hornea una sola vez en Create), así que no hace falta recalcular
        /// esto en Update.
        ///
        /// ANTES esta función rastreaba TODOS los pares (a,b) con
        /// Reactions.TryGet (dos bucles anidados sobre 17 materiales, ~136
        /// pares comprobados para encontrar ~6 reacciones reales) y añadía la
        /// ley del Vivium a mano al final, con productB=Vivium hardcodeado
        /// como truco de presentación (no era el dato real: GrowthTick
        /// consume Nutrient a Empty, no lo convierte en Vivium). Con el
        /// contrato de Fase 3, Sim/Universe.cs YA ENTREGA el array resuelto
        /// -- forma, condición, banda, esDelNucleo, y el índice ESTABLE que
        /// usan los eventos SimEventType.Ley -- así que esto pasa a ser una
        /// única pasada de copia. Es una simplificación real, no un parche:
        /// con 13-16 leyes por seed, rastrear pares habría escalado peor
        /// (17*16/2=136 comprobaciones) sin aportar nada que Universe.Leyes
        /// no diera ya resuelto.
        ///
        /// INVARIANTE que el resto de la clase asume: `_leyes[i]` describe
        /// EXACTAMENTE `Universe.Leyes[i]` para todo i -- ni se salta ni se
        /// reordena ninguna entrada -- porque ActualizarCache usa ESE MISMO
        /// índice `i` para preguntarle a SubstanceKnowledge.LeyDescubierta.
        /// </summary>
        private void ConstruirLeyesDesdeUniverso()
        {
            _leyesCount = 0;
            if (_sim == null || _sim.Universe == null) return;

            var leyes = _sim.Universe.Leyes;
            int n = Mathf.Min(leyes.Length, MaxLeyes); // defensivo: Universe.MaxLeyes(24) == este MaxLeyes, nunca debería recortar.

            for (int i = 0; i < n; i++)
            {
                var l = leyes[i];

                // Catalítica = exactamente un lado no cambia (ver doc de la
                // clase): la misma semántica que documenta ReactionEngine, ahora
                // leída directamente del descriptor en vez de inferida de una
                // Reaction reordenada a mano.
                bool catalitica = (l.productoA == l.a) != (l.productoB == l.b);

                _leyes[_leyesCount] = new LeyDatos
                {
                    a = l.a,
                    b = l.b,
                    productA = l.productoA,
                    productB = l.productoB,
                    catalitica = catalitica,
                    soloFrio = l.condicion == CondicionTermica.Frio,
                    soloCalor = l.condicion == CondicionTermica.Calor,
                    esCrecimiento = l.forma == FormaDeLey.Crecimiento,
                };
                _leyesCount++;
            }
        }

        /// <summary>
        /// Reconstruye TODAS las entradas del libro (leyes, sustancias,
        /// procedimientos) SOLO si el conocimiento del jugador cambió desde
        /// la última vez (nuevo material descubierto, una ley presenciada, o
        /// un (re)bautizo -- ver SubstanceKnowledge.NamingVersion/LeyesVersion,
        /// que a diferencia de CountDiscovered()/CountLeyesDescubiertas()
        /// solos sí detectan un renombrado / una ley repetida ya vista).
        /// Nunca se reconstruyen strings en cada frame de OnGUI cuando el
        /// texto no cambia.
        /// </summary>
        private void ActualizarCache()
        {
            // (playtest 18) LeyesVersion se suma a la firma: sin este término,
            // descubrir una ley (que no cambia CountDiscovered() ni
            // NamingVersion -- presenciar una ley no descubre ni bautiza un
            // material nuevo) no invalidaría la caché y el diario no
            // repintaría el hueco recién revelado. Es la trampa más fácil de
            // este cambio: el diario "funcionaría" en todo menos en lo nuevo,
            // sin ningún error visible, solo un hueco que nunca se rellena.
            //
            // Los tres factores son POTENCIAS SEPARADAS a propósito, no primos
            // sueltos: con multiplicadores arbitrarios (estaba en 1000003/1009/1)
            // existe una colisión real -- 991 rebautizos compensan exactamente un
            // material descubierto de más, y la caché se comería una
            // actualización. Es inalcanzable en una partida de tres jornadas, pero
            // es justo la clase de fallo que aparece el día que alguien alargue la
            // partida y que nadie relacionaría jamás con esta línea. Con
            // desplazamientos por rangos (leyes <= 24 -> 5 bits, bautizos <= 17
            // materiales pero la versión sube también al REbautizar -> se le dan
            // 16 bits) no hay colisión posible hasta 65.535 rebautizos.
            int firma = (_knowledge.CountDiscovered() << 21)
                      ^ ((_knowledge.NamingVersion & 0xFFFF) << 5)
                      ^ (_knowledge.LeyesVersion & 0x1F);
            if (firma == _cacheFirma) return;
            _cacheFirma = firma;

            _entradasLeyesCount = 0;
            _entradasProcedimientosCount = 0;
            for (int i = 0; i < _leyesCount; i++)
            {
                var ley = _leyes[i];

                // (playtest 18) CRITERIO VIEJO (hasta playtest 17): una ley se
                // mostraba si el jugador conocía sus DOS reactivos
                // (EsDescubierto(a) && EsDescubierto(b)). Era razonable
                // mientras no había otra señal: era un PROXY de "esto ya te lo
                // he explicado", derivado de datos que ya existían (descubrir
                // un material no requiere haber visto ninguna reacción suya).
                // El problema real: el diario podía revelar una ley entera
                // -- fórmula, condición térmica, si se propaga -- de una
                // reacción que el jugador NUNCA VIO OCURRIR, solo por haber
                // aspirado o mirado fijamente sus dos ingredientes por
                // separado. Ahora que SubstanceKnowledge registra la ley en
                // sí (evento SimEventType.Ley, ver LeyDescubierta), ese proxy
                // queda sustituido por la señal directa: "¿la presenció?".
                bool descubierta = _knowledge.LeyDescubierta(i); // `i` ES el índice de Universe.Leyes -- ver invariante en ConstruirLeyesDesdeUniverso.

                if (descubierta)
                {
                    ConstruirEntradaLey(ley, out string tituloL, out string detalleL);
                    _entradasLeyes[_entradasLeyesCount++] = new Entrada { Titulo = tituloL, Cuerpo = detalleL, Propaga = ley.catalitica };

                    ConstruirEntradaProcedimiento(ley, out string tituloP, out string detalleP);
                    _entradasProcedimientos[_entradasProcedimientosCount++] = new Entrada { Titulo = tituloP, Cuerpo = detalleP, Propaga = ley.catalitica };
                }
                else
                {
                    // (playtest 18) EL HUECO (decisión de Cesar, "solo lo
                    // presenciado, con hueco visible"): la ley NO desaparece de
                    // la lista, ocupa su sitio en el mismo orden estable que
                    // Universe.Leyes -- es lo que convierte "N de M" en algo
                    // que se puede CONTAR mirando la página, no solo leer como
                    // número. Texto IDÉNTICO siempre (ver doc de
                    // TextoHuecoLeyTitulo): no puede filtrar nada de la ley que
                    // esconde, ni materiales ni forma ni condición térmica.
                    // Propaga=false explícito: un ★ SE PROPAGA en el hueco
                    // filtrarías justo lo que se supone que se esconde.
                    //
                    // PROCEDIMIENTOS no recibe hueco a propósito: una "receta"
                    // en blanco no genera curiosidad como sí lo hace un
                    // renglón vacío en el catálogo de leyes, solo ruido -- ese
                    // trabajo ya lo hace la sección LEYES. Procedimientos
                    // simplemente omite la entrada, como siempre hizo.
                    _entradasLeyes[_entradasLeyesCount++] = new Entrada { Titulo = TextoHuecoLeyTitulo, Cuerpo = TextoHuecoLeyCuerpo, Propaga = false };
                }
            }

            // (playtest 18) Contador "N de M" de la cabecera de LEYES (ver
            // DrawContenido/_tituloLeyesConContador): se recalcula aquí, en el
            // mismo disparador que ya gobierna cuándo hace falta reconstruir
            // texto -- no hace falta un segundo mecanismo de invalidación.
            _tituloLeyesConContador = _leyesCount > 0
                ? "LEYES — " + _knowledge.CountLeyesDescubiertas() + " de " + _leyesCount
                : "LEYES";

            _entradasSustanciasCount = 0;
            var universe = _sim.Universe;
            var mats = universe.Materials;
            for (int m = 1; m < mats.Length; m++)
            {
                byte matId = (byte)m;
                if (!_knowledge.EsDescubierto(matId)) continue;

                string chips = BuildChips(matId);
                if (string.IsNullOrEmpty(chips)) chips = "(sin transformaciones presenciadas todavía)";

                // (playtest 12) Invitación discreta a bautizar, integrada en la
                // propia ficha (distinta del globo junto al cursor de
                // SubstanceKnowledge.DrawAvisoBautizo, que solo aparece al
                // apuntar/cargar: esta vive en el diario, pasiva, para quien
                // vuelve a consultar el catálogo). "Sin nombre de verdad" =
                // ni bautizado por el jugador NI vocabulario de taller (mismo
                // criterio que SubstanceKnowledge.NecesitaBautizo, recompuesto
                // aquí con la API pública existente para no tocar esa clase).
                bool sinNombre = SubstanceKnowledge.NombreComun(matId) == null && _knowledge.NombreDe(matId) == "???";
                if (sinNombre) chips = chips + "\n" + TextoInvitaBautizo;

                _entradasSustancias[_entradasSustanciasCount++] = new Entrada
                {
                    Titulo = _knowledge.NombreParaHud(matId),
                    Cuerpo = chips,
                    TieneSwatch = true,
                    MatId = matId,
                    Firma = universe.DescribirFirma(matId),
                };
            }
        }

        /// <summary>Formato "de un vistazo" (nivel 2 = fórmula, nivel 3 = condición): igual que antes del playtest 10.</summary>
        private void ConstruirEntradaLey(LeyDatos ley, out string titulo, out string detalle)
        {
            string nombreA = _knowledge.NombreParaHud(ley.a);
            string nombreB = _knowledge.NombreParaHud(ley.b);

            if (ley.esCrecimiento)
            {
                // (playtest 18) Antes `productB` venía hardcodeado a Vivium a mano
                // (truco de presentación de la LeyDatos escrita a mano). El dato REAL
                // de Universe.Leyes para esta ley es productoB=Empty (el Nutrient se
                // consume) -- usarlo aquí habría mostrado "??? nuevo" (NombreParaHud
                // de Empty es "???"). Lo que nace es OTRA CÉLULA DE nombreA, no algo
                // relacionado con productB: se usa nombreA directamente.
                titulo = $"{nombreA} + {nombreB}, templado -> {nombreA} nuevo";
                detalle = "Requiere temperatura TEMPLADA (ni fría ni ardiente).";
                return;
            }

            string nombrePa = _knowledge.NombreParaHud(ley.productA);
            string nombrePb = _knowledge.NombreParaHud(ley.productB);
            titulo = $"{nombreA} + {nombreB} -> {nombrePa} + {nombrePb}";
            detalle = ley.soloFrio ? "Solo ocurre en frío." : (ley.soloCalor ? "Solo ocurre con calor." : "Sin condición de temperatura.");
        }

        /// <summary>
        /// (fix playtest 10) SECCIÓN NUEVA. El jugador se quejó de dos cosas
        /// a la vez: "las indicaciones son súper largas y ya me costó
        /// recordar cómo hacer la segunda parte" y "tengo que descubrirlo
        /// porque no tomé una captura de pantalla". Lo que hace falta no es
        /// más texto sino un sitio PERMANENTE al que volver.
        ///
        /// EL PLAN ERA: archivar aquí, tal cual, las pistas que
        /// Game/HintSystem.cs ya le mostró (para no duplicar contenido y que
        /// "lo que ya viste" fuera literal). NO SE PUDO HACER ASÍ: HintSystem
        /// no expone NADA consultable desde fuera -- ni un getter de qué
        /// pista/jornada está activa, ni una bandera de "esta pista ya se
        /// mostró", ni acceso a sus arrays PistasJornadaN (todo es private,
        /// incluido el día actual). Y no se puede tocar ese archivo: otro
        /// agente lo está editando en paralelo en este mismo encargo.
        ///
        /// ALTERNATIVA IMPLEMENTADA (sin tocar HintSystem, sin adivinar su
        /// estado interno): en vez de re-leer sus pistas, esta sección
        /// SINTETIZA procedimientos ejecutables a partir de la MISMA fuente
        /// de verdad que ya usa la sección LEYES (playtest 18:
        /// <c>Universe.Leyes</c>, vía <see cref="_leyes"/> -- ver
        /// ConstruirLeyesDesdeUniverso) pero con un nivel de detalle de
        /// "receta paso a paso" en vez de "fórmula compacta".
        ///
        /// (playtest 18) VISIBILIDAD: se gobierna con el MISMO criterio nuevo
        /// que LEYES (<see cref="SubstanceKnowledge.LeyDescubierta"/> -- la
        /// ley PRESENCIADA, no solo sus dos materiales conocidos por
        /// separado; ver el comentario largo en ActualizarCache). A
        /// diferencia de LEYES, aquí NO se dibuja un hueco por cada
        /// procedimiento que falta: una "receta" en blanco no genera
        /// curiosidad, solo ruido -- la entrada simplemente se omite, como
        /// siempre. El criterio VIEJO (playtest 10-17, "los dos materiales ya
        /// descubiertos") era un proxy razonable de "esto ya te lo he
        /// explicado antes" mientras no existía una señal directa; ahora que
        /// SÍ existe (la propia ley registrada), el proxy queda sustituido.
        /// </summary>
        private void ConstruirEntradaProcedimiento(LeyDatos ley, out string titulo, out string detalle)
        {
            string nombreA = _knowledge.NombreParaHud(ley.a);
            string nombreB = _knowledge.NombreParaHud(ley.b);

            if (ley.esCrecimiento)
            {
                titulo = $"Multiplicar {nombreA}";
                detalle =
                    $"1. Deja un {nombreA} asentado (quieto, no cayendo) con {nombreB} tocándolo.\n" +
                    "2. Mantén esa zona a temperatura TEMPLADA (ni fría ni ardiente).\n" +
                    $"3. ★ SE PROPAGA — nace {nombreA} nuevo junto al original SIN gastarlo: cada célula nacida es otra semilla, no hace falta vigilar la primera.";
                return;
            }

            string nombrePa = _knowledge.NombreParaHud(ley.productA);
            string nombrePb = _knowledge.NombreParaHud(ley.productB);
            string cond = ley.soloFrio ? " (debe estar FRÍO)" : (ley.soloCalor ? " (debe estar CALIENTE)" : "");

            if (ley.catalitica)
            {
                // Determina cuál de los dos lados es el catalizador (el que
                // NO cambia: productX == x) -- la misma comprobación que ya
                // usa ConstruirLeyesDesdeUniverso para decidir "catalitica".
                bool aEsCatalizador = ley.productA == ley.a;
                string catalizador = aEsCatalizador ? nombreA : nombreB;
                string reactivo = aEsCatalizador ? nombreB : nombreA;
                string resultado = aEsCatalizador ? nombrePb : nombrePa;

                titulo = $"Obtener {resultado} a partir de {catalizador}";
                detalle =
                    $"1. Consigue un poco de {catalizador}: no hace falta más de una vez.\n" +
                    $"2. Vierte o pon en contacto {reactivo}{cond} con el {catalizador}: se convierte en {resultado}.\n" +
                    $"3. ★ SE PROPAGA — el {catalizador} no se gasta: repite el paso 2 sobre el mismo punto (o sobre lo ya formado) todas las veces que quieras.";
            }
            else
            {
                titulo = $"Obtener {nombrePa}" + (nombrePa != nombrePb ? $" y {nombrePb}" : "");
                detalle =
                    $"1. Junta {nombreA} con {nombreB} en la misma celda{cond} (viértelos juntos o deja que se toquen).\n" +
                    "2. Ambos lados se CONSUMEN al transformarse: para repetirlo hace falta traer más de los dos.";
            }
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

        // ===================================================================
        // MINIATURAS DE CATÁLOGO (playtest 12)
        // ===================================================================
        // "Genera una miniatura que reproduzca su firma... por código
        // (Texture2D + GUI.DrawTexture, FilterMode.Point), una sola vez por
        // material, cacheada." Réplica SIMPLIFICADA Y ESTÁTICA de la lógica
        // real de Sim/SimRenderer.cs (ApplyPatron/ComputeCellColor, archivo
        // de solo lectura en este encargo): mismo lenguaje visual (color base
        // + patrón que modula brillo/saturación con ModulatePattern, borde
        // que reacciona sobre un anillo de contorno), pero recalculado sobre
        // los MiniLado² téxeles de la propia miniatura en vez de sobre celdas
        // de la simulación real -- no hay `tick` de sim que leer aquí y no
        // hace falta: una ficha de catálogo es una FOTO fija de la firma, no
        // una ventana en vivo al material (para eso ya está el propio taller
        // al fondo del velo). No es idéntica píxel a píxel a como se ve en
        // juego -- no tiene por qué serlo -- pero usa el MISMO despacho por
        // PatronMorfologico/BordeMorfologico, así que la familia se reconoce
        // de un vistazo, que es el contrato del encargo.
        // ===================================================================

        /// <summary>
        /// Miniatura cacheada del material (ver doc de <see cref="_miniaturas"/>):
        /// se genera la PRIMERA vez que se pide (la primera vez que ese
        /// material tiene una Entrada de SUSTANCIAS que dibujar, ya filtrado
        /// por descubierto en ActualizarCache) y nunca se reconstruye después.
        /// </summary>
        private Texture2D ObtenerMiniatura(byte matId)
        {
            if (matId >= _miniaturas.Length || _sim == null || _sim.Universe == null) return null;

            var existente = _miniaturas[matId];
            if (existente != null) return existente;

            var tex = CrearMiniatura(_sim.Universe.Get(matId));
            _miniaturas[matId] = tex;
            return tex;
        }

        private static Texture2D CrearMiniatura(MaterialDef def)
        {
            var tex = new Texture2D(MiniLado, MiniLado, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point, // mismo criterio que SimRenderer: pixel-art nítido, nunca borroso.
                wrapMode = TextureWrapMode.Clamp,
                name = "MiniaturaCatalogo_" + def.devName,
            };

            var pixeles = new Color32[MiniLado * MiniLado];
            Color32 baseColor = def.baseColor;

            for (int y = 0; y < MiniLado; y++)
            {
                for (int x = 0; x < MiniLado; x++)
                {
                    byte r = baseColor.r, g = baseColor.g, b = baseColor.b;
                    byte a = baseColor.a;

                    if (def.colorJitter > 0)
                    {
                        int j = (int)(MiniHash2D(x, y, def.semillaPatron) % (uint)(def.colorJitter * 2 + 1)) - def.colorJitter;
                        r = ClampByteMini(r + j);
                        g = ClampByteMini(g + j);
                        b = ClampByteMini(b + j);
                    }

                    // Gate patronFuerza>0: idéntico al de ComputeCellColor -- el
                    // vocabulario del taller (Liso, patronFuerza siempre 0 por
                    // contrato de Universe.Create) ni evalúa el switch.
                    if (def.patronFuerza > 0) ApplyPatronMini(x, y, def, ref r, ref g, ref b);

                    // Gate borde!=Neto: igual que ComputeCellColor. Aquí el
                    // "vecino vacío" de la sim se sustituye por "cerca del
                    // borde de la propia miniatura" (ver ApplyBordeMini): en
                    // una ficha de catálogo la silueta ES el contorno del
                    // swatch, no hay vacío de sim que consultar.
                    if (def.borde != BordeMorfologico.Neto) ApplyBordeMini(x, y, def, ref r, ref g, ref b, ref a);

                    if (def.emision > 0)
                    {
                        int amt = def.emision * 2 / 5;
                        r = ClampByteMini(r + amt);
                        g = ClampByteMini(g + amt);
                        b = ClampByteMini(b + amt);
                    }

                    pixeles[y * MiniLado + x] = new Color32(r, g, b, a);
                }
            }

            tex.SetPixels32(pixeles);
            // makeNoLongerReadable=true: esta textura nunca se vuelve a leer
            // desde CPU (solo se pinta con GUI.DrawTexture), así que Unity
            // puede soltar la copia duplicada en RAM de sistema tras subirla.
            tex.Apply(false, true);
            return tex;
        }

        /// <summary>Despacho por familia morfológica, réplica simplificada de SimRenderer.ApplyPatron (ver cabecera de esta región).</summary>
        private static void ApplyPatronMini(int x, int y, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            switch (def.patron)
            {
                case PatronMorfologico.Vetas: ApplyVetasMini(x, y, def, ref r, ref g, ref b); break;
                case PatronMorfologico.Manchas: ApplyManchasMini(x, y, def, ref r, ref g, ref b); break;
                case PatronMorfologico.Laberinto: ApplyLaberintoMini(x, y, def, ref r, ref g, ref b); break;
                case PatronMorfologico.Celdas: ApplyCeldasMini(x, y, def, ref r, ref g, ref b); break;
                case PatronMorfologico.Dendritas: ApplyDendritasMini(x, y, def, ref r, ref g, ref b); break;
                case PatronMorfologico.Pulso: ApplyPulsoMini(x, y, def, ref r, ref g, ref b); break;
                case PatronMorfologico.Motas: ApplyMotasMini(x, y, def, ref r, ref g, ref b); break;
                // Liso no llega aquí (patronFuerza siempre 0 para Liso).
            }
        }

        /// <summary>Vetas: bandas senoidales deformadas -- mármol veteado, quieto.</summary>
        private static void ApplyVetasMini(int x, int y, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            int periodo = 5 + def.patronEscala; // 6..13px: banda siempre legible en 30px de miniatura.
            int warp = (int)(MiniHash2D(x / 3, y / 3, def.semillaPatron) % 41) - 20; // deformación suave por bloques de 3px.
            int tiltY = 1 + (def.semillaPatron % 3); // orientación variada por sustancia.
            double fase = (x + y * tiltY + warp) * (Math.PI * 2.0 / periodo);
            int onda = (int)Math.Round(Math.Sin(fase) * 127.0);
            ModulateMini(ref r, ref g, ref b, onda * def.patronFuerza / 255);
        }

        /// <summary>Manchas: discos de concentración alrededor de puntos jitterados -- lunares, no costuras.</summary>
        private static void ApplyManchasMini(int x, int y, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            int celda = 4 + def.patronEscala;
            int d2 = DistanciaMinimaAPunto2(x, y, celda, def.semillaPatron + 30, out _);
            int radio = Mathf.Max(1, celda / 2);
            int d = (int)Math.Sqrt(d2);
            int t01 = Mathf.Clamp(100 - d * 100 / radio, -60, 100); // >0 dentro de la mancha, negativo lejos de cualquiera.
            ModulateMini(ref r, ref g, ref b, t01 * def.patronFuerza / 200);
        }

        /// <summary>Laberinto: dos ondas perpendiculares entrelazadas -- serpentinas, no bandas rectas (lo que lo distingue de Vetas).</summary>
        private static void ApplyLaberintoMini(int x, int y, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            int periodo = 5 + def.patronEscala;
            int warp = (int)(MiniHash2D(x / 4, y / 4, def.semillaPatron + 60) % 61) - 30;
            double fx = (x + warp * 0.2) * (Math.PI * 2.0 / periodo);
            double fy = (y - warp * 0.2) * (Math.PI * 2.0 / periodo);
            double banda = Math.Sin(fx) * Math.Cos(fy); // entrelazado: ni horizontal ni vertical puro.
            int onda = (int)Math.Round(banda * 127.0);
            ModulateMini(ref r, ref g, ref b, onda * def.patronFuerza / 255);
        }

        /// <summary>Celdas: teselas tipo Voronoi con borde marcado -- espuma/tejido celular.</summary>
        private static void ApplyCeldasMini(int x, int y, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            int celda = 5 + def.patronEscala;
            int mejorD2 = DistanciaMinimaAPunto2(x, y, celda, def.semillaPatron + 90, out uint mejorId);
            int segundoD2 = SegundaDistanciaAPunto2(x, y, celda, def.semillaPatron + 90);
            int diff = (int)(Math.Sqrt(segundoD2) - Math.Sqrt(mejorD2));
            int bandaBorde = Mathf.Max(2, celda / 3);

            int amt;
            if (diff < bandaBorde)
            {
                int t01 = diff * 100 / bandaBorde;
                amt = -((100 - t01) * def.patronFuerza / 100);
            }
            else
            {
                int tono = (int)(mejorId % 41) - 20;
                amt = tono * def.patronFuerza / 255;
            }
            ModulateMini(ref r, ref g, ref b, amt);
        }

        /// <summary>Dendritas: ramas/agujas radiales desde el centro -- crecimiento ramificado, nunca un medio continuo.</summary>
        private static void ApplyDendritasMini(int x, int y, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            float cx = MiniLado * 0.5f, cy = MiniLado * 0.5f;
            float dx = x - cx, dy = y - cy;
            float radio = Mathf.Sqrt(dx * dx + dy * dy);
            if (radio < 0.5f) { ModulateMini(ref r, ref g, ref b, def.patronFuerza / 3); return; } // núcleo, siempre algo de brillo.

            float radioMax = MiniLado * 0.5f;
            float anguloDeg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg; // -180..180
            int sectores = 7 + (def.semillaPatron % 5); // 7..11 ramas, variado por semilla.
            float anguloSector = 360f / sectores;
            int sector = Mathf.FloorToInt((anguloDeg + 180f) / anguloSector);
            uint h = MiniHash2D(sector, 0, def.semillaPatron + 120); // "y"=0: solo hace falta una dimensión (el sector) más la sal.
            float largoRama = radioMax * (0.35f + (h % 100) / 100f * 0.65f); // 35%..100% del radio.
            float centroSectorDeg = sector * anguloSector - 180f + anguloSector * 0.5f;
            float distAngular = Mathf.Abs(Mathf.DeltaAngle(anguloDeg, centroSectorDeg));

            if (radio <= largoRama && distAngular <= 9f)
            {
                float t01 = 1f - radio / Mathf.Max(1f, largoRama); // brilla cerca del centro, se apaga hacia la punta.
                ModulateMini(ref r, ref g, ref b, (int)(t01 * def.patronFuerza));
            }
            else
            {
                ModulateMini(ref r, ref g, ref b, -(def.patronFuerza / 6)); // entre ramas: vacío mineral, algo más oscuro.
            }
        }

        /// <summary>Pulso: anillos concéntricos -- "late" alrededor del centro de la miniatura.</summary>
        private static void ApplyPulsoMini(int x, int y, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            float cx = MiniLado * 0.5f, cy = MiniLado * 0.5f;
            float radio = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            int periodo = 4 + def.patronEscala;
            double fase = radio * (Math.PI * 2.0 / periodo);
            int onda = (int)Math.Round(Math.Sin(fase) * 127.0);
            ModulateMini(ref r, ref g, ref b, onda * def.patronFuerza / 255);
        }

        /// <summary>Motas: destellos dispersos, aditivo puro (nunca resta) -- igual criterio que SimRenderer.ApplyMotas.</summary>
        private static void ApplyMotasMini(int x, int y, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            uint h = MiniHash2D(x, y, def.semillaPatron + 150);
            if ((h % 11) != 0) return; // ~1 de cada 11 téxeles lleva mota.
            r = ClampByteMini(r + def.patronFuerza);
            g = ClampByteMini(g + def.patronFuerza);
            b = ClampByteMini(b + def.patronFuerza);
        }

        /// <summary>
        /// Borde morfológico sobre un anillo de contorno de la propia
        /// miniatura (los `bandaBorde` téxeles más cercanos a cualquiera de
        /// los 4 lados del swatch) -- la silueta que en la sim real da el
        /// vecino vacío, aquí la da el borde del icono. Difuso baja alfa (no
        /// oscurece hacia un color de fondo fijo, ver el porqué en la
        /// cabecera de la región): GUI.DrawTexture compone con blending alfa
        /// normal contra el pergamino de la página, así que "se pierde"
        /// igual sin depender de qué color tenga la página en cada tema.
        /// </summary>
        private static void ApplyBordeMini(int x, int y, MaterialDef def, ref byte r, ref byte g, ref byte b, ref byte a)
        {
            const int bandaBorde = 3;
            int distBorde = Mathf.Min(Mathf.Min(x, MiniLado - 1 - x), Mathf.Min(y, MiniLado - 1 - y));
            if (distBorde >= bandaBorde) return;

            switch (def.borde)
            {
                case BordeMorfologico.Halo:
                    ModulateMini(ref r, ref g, ref b, 40);
                    break;
                case BordeMorfologico.Escarcha:
                    if ((MiniHash2D(x, y, def.semillaPatron + 211) % 3) == 0)
                    {
                        r = ClampByteMini(r + 80);
                        g = ClampByteMini(g + 80);
                        b = ClampByteMini(b + 80);
                    }
                    break;
                case BordeMorfologico.Difuso:
                    if ((MiniHash2D(x, y, def.semillaPatron + 217) % 2) == 0)
                    {
                        a = (byte)(a * 55 / 100);
                    }
                    break;
            }
        }

        /// <summary>Igual lenguaje que SimRenderer.ModulatePattern: desplaza brillo con un empujón de saturación "gratis" al aclarar, sin tocarla al oscurecer.</summary>
        private static void ModulateMini(ref byte r, ref byte g, ref byte b, int signedAmt)
        {
            int mean = (r + g + b) / 3;
            int sat = signedAmt > 0 ? signedAmt / 3 : 0;
            r = ClampByteMini(r + signedAmt + (r - mean) * sat / 128);
            g = ClampByteMini(g + signedAmt + (g - mean) * sat / 128);
            b = ClampByteMini(b + signedAmt + (b - mean) * sat / 128);
        }

        /// <summary>Distancia al cuadrado al punto-semilla más cercano de una rejilla jitterada (feature point de Voronoi barato). Usada por Manchas y Celdas.</summary>
        private static int DistanciaMinimaAPunto2(int x, int y, int celda, int semilla, out uint idGanador)
        {
            int gx = x / celda, gy = y / celda;
            int mejorD2 = int.MaxValue;
            uint mejorId = 0;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = gx + ox, cy = gy + oy;
                    uint h = MiniHash2D(cx, cy, semilla);
                    int px = cx * celda + (int)(h % (uint)celda);
                    int py = cy * celda + (int)((h >> 8) % (uint)celda);
                    int dx = x - px, dy = y - py;
                    int d2 = dx * dx + dy * dy;
                    if (d2 < mejorD2) { mejorD2 = d2; mejorId = h; }
                }
            }
            idGanador = mejorId;
            return mejorD2;
        }

        /// <summary>Distancia al cuadrado al SEGUNDO punto-semilla más cercano (para marcar la costura entre teselas de Celdas). Recorre la misma rejilla 3x3 que DistanciaMinimaAPunto2, aparte porque el `out` de esa función solo trae el primero.</summary>
        private static int SegundaDistanciaAPunto2(int x, int y, int celda, int semilla)
        {
            int gx = x / celda, gy = y / celda;
            int mejorD2 = int.MaxValue, segundoD2 = int.MaxValue;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cx = gx + ox, cy = gy + oy;
                    uint h = MiniHash2D(cx, cy, semilla);
                    int px = cx * celda + (int)(h % (uint)celda);
                    int py = cy * celda + (int)((h >> 8) % (uint)celda);
                    int dx = x - px, dy = y - py;
                    int d2 = dx * dx + dy * dy;
                    if (d2 < mejorD2) { segundoD2 = mejorD2; mejorD2 = d2; }
                    else if (d2 < segundoD2) segundoD2 = d2;
                }
            }
            return segundoD2;
        }

        private static byte ClampByteMini(int v) => (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));

        /// <summary>Hash entero estable de 2 coordenadas + sal, réplica local de SimRenderer.Hash2D (privado en Sim/, archivo de solo lectura aquí) para no depender de él.</summary>
        private static uint MiniHash2D(int x, int y, int salt)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)salt * 2654435761u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return h;
            }
        }

    }
}
