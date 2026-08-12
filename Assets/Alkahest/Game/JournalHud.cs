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
    ///  · LEYES -- igual que antes del playtest 10, lee la tabla de
    ///    reacciones REAL del universo activo (AlkahestSim.Universe.Reactions,
    ///    horneada por seed en Sim/Universe.cs -- NUNCA se toca Sim/, solo se
    ///    lee su API pública) más la ley de crecimiento del Vivium (que no
    ///    vive en esa tabla: es una regla propia de Sim/SimStepper.cs/
    ///    GrowthTick, así que se añade a mano pero con los NÚMEROS REALES de
    ///    esta seed). Formato "de un vistazo": fórmula (entrada) + condición
    ///    de temperatura (detalle) + distintivo ★ SE PROPAGA cuando aplica.
    ///  · SUSTANCIAS -- lo descubierto, con su color real, su nombre
    ///    (bautizado si lo hay) y las observaciones que guarda
    ///    SubstanceKnowledge (WitnessOf).
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

        /// <summary>
        /// Una fila de cualquier página del libro: dos niveles tipográficos
        /// (Titulo = "entrada", Cuerpo = "detalle") más los adornos que solo
        /// aplican a algunas secciones (swatch de color en SUSTANCIAS,
        /// distintivo de propagación en LEYES). Reutilizada por las tres
        /// secciones para que la paginación (ComputePages/FillColumn) sea un
        /// único algoritmo, no tres copias.
        /// </summary>
        private struct Entrada
        {
            public string Titulo;
            public string Cuerpo;
            public bool Propaga;
            public bool TieneSwatch;
            public Color Swatch;
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

        // Estructura de leyes: se calcula UNA sola vez en Init (la tabla de
        // reacciones de este universo no cambia durante la partida). El TEXTO
        // de cada entrada sí depende de nombres bautizables, así que se
        // cachea aparte y solo se reconstruye cuando cambia el estado de
        // conocimiento del jugador (ver ActualizarCache) -- nunca se
        // reconstruyen strings en cada frame de OnGUI si nada cambió.
        private readonly LeyDatos[] _leyes = new LeyDatos[MaxLeyes];
        private int _leyesCount;

        private readonly Entrada[] _entradasLeyes = new Entrada[MaxLeyes];
        private int _entradasLeyesCount;
        private readonly Entrada[] _entradasProcedimientos = new Entrada[MaxLeyes];
        private int _entradasProcedimientosCount;
        private readonly Entrada[] _entradasSustancias = new Entrada[MaterialId.Count];
        private int _entradasSustanciasCount;
        private int _cacheFirma = int.MinValue;

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
        // Estilos propios (cacheados, reconstruidos solo si cambia la
        // resolución -- mismo criterio que UiStyles.Preparar). El libro
        // necesita niveles que UiStyles no tiene ya hechos (título de tapa,
        // pestaña, título de sección/capítulo), así que se crean aquí, NUNCA
        // dentro de OnGUI en cada frame.
        // -----------------------------------------------------------------
        private GUIStyle _estiloTituloLibro;
        private GUIStyle _estiloPestana;
        private GUIStyle _estiloTituloSeccion;
        private GUIStyle _estiloEntradaTitulo;
        private GUIStyle _estiloDetalle;
        private GUIStyle _estiloBadgePropaga;
        private GUIStyle _estiloPie;
        private GUIStyle _estiloAyudaPie;
        private GUIStyle _estiloBotonPagina;
        private int _alturaEstilos = -1;

        // Pergamino apagado y lomo: coherentes con la paleta ciruela/latón
        // del taller (UiStyles.Tinta/Oro), NO blanco puro -- el reporte pide
        // explícitamente evitar quemar los ojos en un juego oscuro.
        private static readonly Color _velo = new Color(0.02f, 0.015f, 0.03f, 0.86f);
        private static readonly Color _papel = new Color(0.30f, 0.24f, 0.18f, 1f);
        private static readonly Color _papelBorde = new Color(0.58f, 0.47f, 0.30f, 0.30f);
        private static readonly Color _lomo = new Color(0.09f, 0.07f, 0.06f, 1f);

        private static readonly string[] _tituloSeccion = { "LEYES", "SUSTANCIAS", "PROCEDIMIENTOS" };
        private const string TextoVacioLeyes = "(ninguna ley descubierta todavía: combina materiales para desvelarlas)";
        private const string TextoVacioSustancias = "(nada descubierto todavía: aspira, vierte o mantén el cursor un instante sobre algo)";
        private const string TextoVacioProcedimientos = "(sin procedimientos archivados todavía: aparecen solos en cuanto descubres los dos materiales de una ley)";

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _knowledge = knowledge;
            ConstruirLeyesDesdeUniverso();
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

            float altoCabecera = _estiloTituloLibro.lineHeight + UiStyles.S(8f) + _estiloPestana.lineHeight + UiStyles.S(10f);
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

            float yTabs = r.y + altoTitulo + UiStyles.S(8f);
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
            string tituloSeccion = _tituloSeccion[(int)_seccion];
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
            float sangriaTitulo = e.TieneSwatch ? (_estiloEntradaTitulo.lineHeight * 0.72f) + UiStyles.S(8f) : 0f;
            float h = UiStyles.Alto(_estiloEntradaTitulo, e.Titulo, ancho - sangriaTitulo);
            if (badge && e.Propaga) h += UiStyles.S(2f) + _estiloBadgePropaga.lineHeight;
            if (!string.IsNullOrEmpty(e.Cuerpo)) h += UiStyles.S(3f) + UiStyles.Alto(_estiloDetalle, e.Cuerpo, ancho);
            return h;
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
            float y = r.y;

            if (e.TieneSwatch)
            {
                float lado = _estiloEntradaTitulo.lineHeight * 0.72f;
                var sw = new Rect(r.x, y + UiStyles.S(2f), lado, lado);
                UiStyles.Rellenar(sw, e.Swatch);
            }

            float sangriaTitulo = e.TieneSwatch ? (_estiloEntradaTitulo.lineHeight * 0.72f) + UiStyles.S(8f) : 0f;
            float anchoTitulo = r.width - sangriaTitulo;
            float altoTitulo = UiStyles.Alto(_estiloEntradaTitulo, e.Titulo, anchoTitulo);
            GUI.Label(new Rect(r.x + sangriaTitulo, y, anchoTitulo, altoTitulo), e.Titulo, _estiloEntradaTitulo);
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

        // ===================================================================
        // ESTILOS PROPIOS (cacheados; ver docs del campo _alturaEstilos)
        // ===================================================================
        private void ConstruirEstilosPropios()
        {
            if (_alturaEstilos == Screen.height && _estiloTituloLibro != null) return;
            _alturaEstilos = Screen.height;

            var raiz = GUI.skin.label;

            _estiloTituloLibro = NuevoEstilo(raiz, 21, FontStyle.Bold, TextAnchor.UpperCenter, UiStyles.Oro, false);
            _estiloPestana = NuevoEstilo(raiz, 14, FontStyle.Bold, TextAnchor.MiddleCenter, UiStyles.TextoTenue, false);
            // Nivel 1: título de sección/capítulo, repetido como cabecera
            // corrida en ambas páginas del tramo.
            _estiloTituloSeccion = NuevoEstilo(raiz, 15, FontStyle.Bold, TextAnchor.UpperLeft, UiStyles.OroTenue, false);
            // Nivel 2: entrada (una ley, un procedimiento, una sustancia).
            _estiloEntradaTitulo = NuevoEstilo(raiz, 15, FontStyle.Bold, TextAnchor.UpperLeft, UiStyles.Texto, true);
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
        // DATOS: leyes desde Universe.Reactions (sin cambios de fondo desde
        // antes del playtest 10, solo se reparte en Entrada de dos formas).
        // ===================================================================

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
            // no cambia, b=Nutrient se convierte en Vivium).
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
        /// Reconstruye TODAS las entradas del libro (leyes, sustancias,
        /// procedimientos) SOLO si el conocimiento del jugador cambió desde
        /// la última vez (nuevo material descubierto o un (re)bautizo -- ver
        /// SubstanceKnowledge.NamingVersion, que a diferencia de
        /// CountNamed() sí detecta un renombrado). Nunca se reconstruyen
        /// strings en cada frame de OnGUI cuando el texto no cambia.
        /// </summary>
        private void ActualizarCache()
        {
            int firma = _knowledge.CountDiscovered() * 1000003 + _knowledge.NamingVersion;
            if (firma == _cacheFirma) return;
            _cacheFirma = firma;

            _entradasLeyesCount = 0;
            _entradasProcedimientosCount = 0;
            for (int i = 0; i < _leyesCount; i++)
            {
                var ley = _leyes[i];
                // Un libro no revela leyes de materiales que el jugador aún
                // no ha visto: los dos lados tienen que estar descubiertos.
                if (!_knowledge.EsDescubierto(ley.a) || !_knowledge.EsDescubierto(ley.b)) continue;

                ConstruirEntradaLey(ley, out string tituloL, out string detalleL);
                _entradasLeyes[_entradasLeyesCount++] = new Entrada { Titulo = tituloL, Cuerpo = detalleL, Propaga = ley.catalitica };

                ConstruirEntradaProcedimiento(ley, out string tituloP, out string detalleP);
                _entradasProcedimientos[_entradasProcedimientosCount++] = new Entrada { Titulo = tituloP, Cuerpo = detalleP, Propaga = ley.catalitica };
            }

            _entradasSustanciasCount = 0;
            var mats = _sim.Universe.Materials;
            for (int m = 1; m < mats.Length; m++)
            {
                byte matId = (byte)m;
                if (!_knowledge.EsDescubierto(matId)) continue;

                string chips = BuildChips(matId);
                if (string.IsNullOrEmpty(chips)) chips = "(sin transformaciones presenciadas todavía)";

                _entradasSustancias[_entradasSustanciasCount++] = new Entrada
                {
                    Titulo = _knowledge.NombreParaHud(matId),
                    Cuerpo = chips,
                    TieneSwatch = true,
                    Swatch = mats[m].baseColor,
                };
            }
        }

        /// <summary>Formato "de un vistazo" (nivel 2 = fórmula, nivel 3 = condición): igual que antes del playtest 10.</summary>
        private void ConstruirEntradaLey(LeyDatos ley, out string titulo, out string detalle)
        {
            string nombreA = _knowledge.NombreParaHud(ley.a);
            string nombreB = _knowledge.NombreParaHud(ley.b);
            string nombrePb = _knowledge.NombreParaHud(ley.productB);

            if (ley.esCrecimiento)
            {
                titulo = $"{nombreA} + {nombreB}, templado -> {nombrePb} nuevo";
                detalle = "Requiere temperatura TEMPLADA (ni fría ni ardiente).";
                return;
            }

            string nombrePa = _knowledge.NombreParaHud(ley.productA);
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
        /// de verdad que ya usa la sección LEYES (Universe.Reactions +
        /// SimStepper.GrowthTick, vía _leyes) pero con un nivel de detalle
        /// de "receta paso a paso" en vez de "fórmula compacta". La
        /// visibilidad se gobierna con el MISMO criterio que LEYES (los dos
        /// materiales ya descubiertos), que es un proxy razonable de "esto
        /// ya te lo he explicado antes" sin depender de HintSystem. Esto
        /// cubre EXACTAMENTE el caso que motivó la queja -- multiplicar
        /// vivium y cristalizar azoth son las dos únicas leyes catalíticas/
        /// de crecimiento del roster fijo -- explicitando el paso que se
        /// olvida ("no se gasta, repite el paso 2").
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
    }
}
