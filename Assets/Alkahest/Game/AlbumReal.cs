using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [LA QUÍMICA CON NOMBRE REAL, ENCARGO A · REHECHO EN EL PLAYTEST 46]
    /// EL ÁLBUM: la pantalla que convierte el retículo base×estado (5 bases ×
    /// 8 estados, "LO QUE PERSISTE" desde el playtest 25) en una colección
    /// que se completa -- mandato literal de Cesar: "un árbol de figuritas
    /// que quieran completar... al descubrir algo, un indicador pulsante y el
    /// menú bonito con el material, el nombre y una mini reseña".
    ///
    /// TRES PIEZAS EN UN SOLO ARCHIVO:
    ///  1) <see cref="DibujarPaginaDoble"/> -- EL ÁLBUM PAGINADO, método
    ///     estático sin estado propio (recibe `sim`/`knowledge`/`pagina` del
    ///     llamante): lo usa esta misma clase en su pantalla completa (tecla
    ///     B) Y Game/JournalHud.cs en su quinta pestaña ÁLBUM, así que hay un
    ///     único dibujo del álbum en todo el proyecto (el encargo lo pide
    ///     explícito: "reutiliza el render de AlbumReal -- un solo código").
    ///  2) EL LIBRITO + LA COLA: al recibir <see cref="SubstanceKnowledge.AlDescubrir"/>
    ///     (API congelada del encargo Q, dispara solo en el PRIMER
    ///     descubrimiento de cada material) se encola el material si es del
    ///     retículo base×estado -- ver <see cref="OnAlgoDescubierto"/> para
    ///     por qué los clásicos (agua, vapor...) NO generan cola: ya se
    ///     conocían desde el día 1, no hay sorpresa que celebrar. Mientras la
    ///     cola no esté vacía, un LIBRITO CERRADO late suave (~1 Hz) junto al
    ///     panel de encargos (borde superior derecho).
    ///  3) LA FICHA-VITRINA: pulsar B (o clic en el librito) abre, uno a uno,
    ///     el panel de descubrimiento -- MISMA ANATOMÍA que el rito de
    ///     Game/NamingUi.cs (PanelRito + doble filete de latón + Cinzel
    ///     dorado + muestra viva con la FIRMA VISUAL real + línea ceremonial),
    ///     pero SIN campo de texto (el nombre ya es el real, o el provisional
    ///     del jugador en caótico -- nada que escribir aquí). "Anotado en tu
    ///     álbum" cierra y, si quedan pendientes, abre la siguiente.
    ///
    /// =====================================================================
    /// (PLAYTEST 46) LOS TRES REPROCHES DE CESAR Y QUÉ SE HIZO CON CADA UNO
    /// =====================================================================
    /// (a) "La imagen de notificación está horrible, no puede ser algo rojo
    ///     así -- quizás un libro brillando ligeramente". Era literalmente un
    ///     CUADRADO de oro plano (relleno OroTenue + relleno Oro dentro) que
    ///     ESCALABA un 12% con el pulso: sin dibujo, sin significado, y con
    ///     el numeral encima leía como el badge de error de una aplicación.
    ///     Ahora es <see cref="DrawLibrito"/>: un libro cerrado de canto
    ///     (tapa de cuero oscuro, lomo de latón con dos nervios, canto de
    ///     páginas color vitela, rombo en la tapa) con un HALO que respira a
    ///     ~1 Hz. El libro NO escala -- lo que late es el halo y el brillo
    ///     del metal; un objeto que cambia de tamaño se lee como alarma, uno
    ///     que cambia de luz se lee como que está vivo.
    /// (b) "La ficha no es tan hermosa como la pestaña de bautizo, tiene que
    ///     estar a ese nivel". Se calcó la anatomía de NamingUi.DrawWindow
    ///     paso a paso (ver <see cref="DrawFicha"/>): mismo pad de 22, mismo
    ///     título Cinzel espaciado + FileteRombo a S(9), misma muestra de 92
    ///     px con MarcoLaton al 0.9, misma columna derecha ("SE LLAMA" tenue
    ///     + nombre grande + firma visual EN PALABRAS en cursiva ceremonial),
    ///     misma línea ceremonial y misma línea de ayuda al pie. Lo único que
    ///     cambia respecto al bautizo es lo que TIENE que cambiar: no hay
    ///     campo de texto, y en su lugar va la reseña de trivia en Alegreya.
    ///     La muestra deja de ser un color plano y pasa a ser la FIRMA VISUAL
    ///     real generada por FirmaVisualFabrica (la misma que pinta las
    ///     redomas, el frasco y el bautizo), cacheada por material -- ver
    ///     <see cref="ObtenerMuestra"/> para por qué eso NO rompe "cero
    ///     allocs por frame".
    /// (c) "El álbum no está a la altura... no tiene que caber todo en una
    ///     hoja: tienes PÁGINAS, como un álbum". Antes las 5 familias + los
    ///     clásicos se apretaban en un solo lienzo (40 cajas de 30 px con
    ///     "?" y los verbos pisándose, ver el bug de abajo). Ahora hay
    ///     <see cref="PaginasTotales"/> páginas: UNA FAMILIA POR DOBLE
    ///     PÁGINA -- izquierda las ocho figuritas grandes en mini-vitrinas de
    ///     latón, derecha el árbol de verbos de ESA familia -- más una página
    ///     final para los clásicos del arco. El lujo de un álbum es el
    ///     espacio: la misma información ocupa ahora 6 páginas y cada
    ///     figurita es 3-4 veces más grande.
    ///
    /// =====================================================================
    /// (PLAYTEST 46) CAUSA RAÍZ DEL SOLAPAMIENTO ("se me juntaron dos
    /// descubrimientos y se sobrepusieron algunos nombres") -- VERIFICADA EN
    /// EL JUEGO, NO DEDUCIDA
    /// =====================================================================
    /// Se comprobaron LAS DOS hipótesis del encargo y las dos eran falsas:
    ///  · NO era "dos OnGUI dibujando la misma cola": la cola es una sola
    ///    (`_cola`) y `_fichaAbierta` solo puede mostrar UN material.
    ///  · NO era el banner "ALGO NUEVO" de Game/SubstanceKnowledge.cs
    ///    pisando la ficha (que geométricamente PODRÍA: su panel vive en
    ///    `Screen.height * 0.30f` y la ficha está centrada, así que se
    ///    solapan). En SEMILLA CERO ese banner no llega a verse nunca: todo
    ///    el retículo base×estado tiene identidad real, así que
    ///    `NecesitaBautizo` es false y `ActualizarBannerLey` lo descarta en
    ///    su PRIMER frame (rama `yaBautizado`). Se disparraron 3 y 2
    ///    descubrimientos en ráfaga en el editor y no apareció ningún banner
    ///    en pantalla. (En modo CAÓTICO sí saldría y sí se solaparía -- ver
    ///    la DEUDA al final de este bloque.)
    ///
    /// LA CAUSA REAL, vista en pantalla: `_visible` (el álbum a pantalla
    /// completa) y `_fichaAbierta` (la vitrina) NO SON EXCLUYENTES.
    /// `AbrirSiguienteFicha()` no bajaba `_visible`, y `OnGUI` dibujaba
    /// PRIMERO `DrawPantallaCompleta()` y DESPUÉS `DrawFicha()`: como el velo
    /// de la ficha era de alfa 0.90, el álbum entero seguía leyéndose por
    /// debajo al 10% -- los nombres de las cinco familias y de las figuritas
    /// quedaban impresos alrededor y por detrás de la tarjeta, que a su vez
    /// enseña OTRO nombre en grande. Dos capas de nombres a la vez. Se llega
    /// ahí con el gesto más natural del mundo: abres el álbum con B, cae un
    /// descubrimiento mientras lo miras, y vuelves a pulsar B (que con cola
    /// pendiente significa "enséñame la ficha", no "cierra").
    /// SEGUNDA CAUSA, independiente y visible SIEMPRE (no hacía falta ninguna
    /// ráfaga): dentro del propio árbol, los cuatro verbos de la fila 1 se
    /// dibujaban centrados en el punto medio de su arco, todos a la MISMA
    /// altura, con rects más anchos que la separación entre hermanos y con
    /// `clipping = Overflow` -- salía "fundirprensacalcinardisolver" y
    /// "templarecocer" impresos unos encima de otros en las CINCO familias.
    /// Eso es, literalmente, "se sobrepusieron algunos nombres".
    ///
    /// LOS TRES ARREGLOS (cinturón y tirantes):
    ///  1) `AbrirSiguienteFicha()` cierra el álbum a pantalla completa y
    ///     recuerda que estaba abierto (<see cref="_restaurarArbolAlCerrar"/>)
    ///     para devolvértelo cuando termines de anotar: una sola cosa visible
    ///     a la vez, sin perderte el sitio.
    ///  2) `OnGUI` no puede dibujar los dos aunque alguien vuelva a
    ///     desincronizar el estado (`if (_visible && !_fichaAbierta)`), y el
    ///     velo de la ficha sube a 0.94 para que nada de debajo se lea.
    ///  3) Los verbos se dibujan pegados a SU nodo hijo (no en el punto medio
    ///     del arco), con el ancho acotado a la mitad de la separación entre
    ///     hermanos y con `clipping = Clip` en el estilo: aunque un día
    ///     alguien meta un verbo larguísimo o encoja la página, la tipografía
    ///     se recorta antes que pisar a la vecina.
    /// DEUDA CONOCIDA: en modo CAÓTICO el banner "ALGO NUEVO" de
    /// SubstanceKnowledge SÍ vive sus 7 s en `Screen.height*0.30f` y la ficha
    /// se abriría encima. No se puede arreglar desde aquí (ese archivo no
    /// entra en este encargo y no expone si hay banner en curso); la ficha al
    /// menos se dibuja DELANTE (GUI.depth) y con velo casi opaco, así que se
    /// vería la tarjeta limpia y el banner apagado detrás, no dos textos
    /// mezclados. Lo correcto sería que SubstanceKnowledge consultara
    /// <see cref="Abierto"/> en su OnGUI, una línea, cuando alguien pueda
    /// tocarlo.
    ///
    /// SEMILLA CERO vs. CAÓTICO (decisión documentada, fuera de la letra
    /// literal del contrato): el álbum se dibuja EXACTAMENTE IGUAL en los dos
    /// modos -- mismas 5 familias, mismos 8 estados, mismas flechas -- porque
    /// el retículo es universal (CONTRATO_PERSISTE.md, no es exclusivo de la
    /// seed 777002). Lo único que cambia por material es la ETIQUETA: si
    /// <see cref="Universe.TieneIdentidadReal"/> es true (hoy, solo en
    /// Semilla Cero) se usa el nombre real y la reseña de trivia; si no, cae
    /// a <see cref="SubstanceKnowledge.NombreDe"/> (el nombre provisional o
    /// bautizado de siempre) y "aún por estudiar" en vez de reseña -- ver
    /// <see cref="EtiquetaDe"/>/<see cref="ResenaDe"/>. Así el álbum nunca
    /// queda vacío en un universo caótico: solo pierde la trivia real que es
    /// exclusiva de este mundo concreto.
    ///
    /// CERO ALLOCS POR FRAME: la FORMA del árbol (qué nodo es hijo de cuál,
    /// su verbo, su posición fraccional) es una tabla `static readonly`
    /// calculada una vez al cargar la clase; las posiciones en PANTALLA se
    /// recalculan cada frame a partir del Rect recibido (aritmética de
    /// structs, sin asignar), y los ÚNICOS strings que se ensamblan con `+`
    /// (los contadores "N / M", "N de M") se cachean contra el último valor
    /// mostrado, mismo patrón que <c>JournalHud._pieTexto</c>. Las texturas
    /// de muestra se generan UNA vez por material la primera vez que se ven
    /// (mismo patrón exacto que NamingUi/FlaskHud/StorageRack) y se liberan
    /// en <see cref="OnDestroy"/>.
    /// </summary>
    public sealed class AlbumReal : MonoBehaviour
    {
        // -----------------------------------------------------------------
        // LA FORMA DEL ÁRBOL (idéntica para las 5 familias): un nodo por
        // EstadoMateria, indexado por su propio valor de enum a propósito
        // (Polvo=0 ... Solucion=7) para no necesitar una tabla de búsqueda
        // aparte. Solo se dibujan los arcos "de cabecera" del retículo real
        // (Sim/Universe.cs::AddEdgesFrom) -- Polvo es la raíz con 4 salidas
        // directas (fundir/prensar/calcinar/disolver) y Fundido/Compacto
        // ramifican una vez más (templar+recocer / ceramizar). El grafo real
        // tiene MÁS aristas (p.ej. Recocido->Compacto, Calcinado->Solución,
        // Solución->Polvo por evaporar) que aquí NO se dibujan a propósito:
        // el árbol dice QUE existe un vidrio y el verbo que lo alcanza,
        // JAMÁS la receta completa (mandato del diseño, §3) -- mostrar cada
        // atajo posible sería enseñar de más.
        //
        // (playtest 46) Las XFrac de la fila 1 se reparten ahora sobre UNA
        // PÁGINA ENTERA (antes, sobre un quinto del ancho de la pantalla):
        // los mismos números valen, pero la separación real entre hermanos
        // pasa de ~70 px a ~160 px, que es lo que por fin deja respirar a los
        // verbos. Ver la CAUSA RAÍZ en el docblock de la clase.
        // -----------------------------------------------------------------
        private struct NodoArbol
        {
            public int Padre;    // índice en _nodos (== valor de EstadoMateria del padre), -1 = raíz.
            public string Verbo; // null en la raíz.
            public float XFrac;  // 0..1 dentro del ancho de LA PÁGINA de la familia.
            public int Fila;     // 0 (raíz), 1, 2.
        }

        private static readonly NodoArbol[] _nodos =
        {
            /* Polvo     */ new NodoArbol { Padre = -1, Verbo = null,        XFrac = 0.50f, Fila = 0 },
            /* Fundido   */ new NodoArbol { Padre = 0,  Verbo = "fundir",    XFrac = 0.13f, Fila = 1 },
            /* Templado  */ new NodoArbol { Padre = 1,  Verbo = "templar",   XFrac = 0.06f, Fila = 2 },
            /* Recocido  */ new NodoArbol { Padre = 1,  Verbo = "recocer",   XFrac = 0.28f, Fila = 2 },
            /* Compacto  */ new NodoArbol { Padre = 0,  Verbo = "prensar",   XFrac = 0.41f, Fila = 1 },
            /* Ceramico  */ new NodoArbol { Padre = 4,  Verbo = "ceramizar", XFrac = 0.53f, Fila = 2 },
            /* Calcinado */ new NodoArbol { Padre = 0,  Verbo = "calcinar",  XFrac = 0.69f, Fila = 1 },
            /* Solucion  */ new NodoArbol { Padre = 0,  Verbo = "disolver",  XFrac = 0.93f, Fila = 1 },
        };

        /// <summary>Rótulo de cada estado, en el orden del enum <see cref="EstadoMateria"/> -- literales YA en la caja tipográfica final (cero allocs: nada de ToUpperInvariant() por frame). Es lo que se lee bajo cada mini-vitrina incluso cuando la figurita sigue sin descubrir: enseña que ESE hueco existe sin revelar qué hay dentro.</summary>
        private static readonly string[] _rotuloEstado =
        {
            "POLVO", "FUNDIDO", "TEMPLADO", "RECOCIDO", "COMPACTO", "CERÁMICO", "CALCINADO", "SOLUCIÓN",
        };

        /// <summary>Los "clásicos del arco" (diseño §2, tabla final): página propia, siempre revelada -- son vocabulario del taller desde el día 1, no hay sorpresa que ocultar (regla 13 de CLAUDE.md). NO cuentan en el progreso N/M (ver TotalFiguritas): ese contador es del retículo, no del vocabulario que el jugador ya trae puesto.</summary>
        private static readonly byte[] _clasicos =
        {
            MaterialId.Limo, MaterialId.Water, MaterialId.Steam, MaterialId.Ice,
            MaterialId.Fire, MaterialId.Smoke, MaterialId.Ash, MaterialId.Brasa, MaterialId.Stone,
        };

        // -----------------------------------------------------------------
        // (playtest 47, ENCARGO C, CONTRATO_FASE_A.md §1e) PÁGINA "MEZCLAS DEL
        // OFICIO": las 5+1 figuritas de las recetas cruzadas (mortero,
        // clínker, hormigón, vidrio de botella, lejía, esmaltado), NO cuentan
        // en el progreso N/M del retículo (mismo criterio que _clasicos: son
        // otra colección, con su propio "6/6" -- ver DibujarPaginaMezclas).
        // Orden: el de la tabla del contrato §1b (mortero, clínker,
        // hormigón, vidrio, lejía, esmaltado).
        // -----------------------------------------------------------------
        private static readonly byte[] _cruces =
        {
            MaterialId.Mortero, MaterialId.Clinker, MaterialId.Hormigon,
            MaterialId.VidrioVerde, MaterialId.Lejia, MaterialId.Esmaltado,
        };

        /// <summary>Las recetas COMO PREGUNTAS (contrato §1e, literal: "¿cal + arena?"), mismo orden que <see cref="_cruces"/> -- se muestran tal cual mientras el producto sigue sin descubrir; ver <see cref="_cruceRespuestas"/> para lo que las sustituye al revelarse.</summary>
        private static readonly string[] _crucePreguntas =
        {
            "¿cal apagada + arena?", "¿caliza + arcilla?", "¿clínker + arena?",
            "¿arena + ceniza?", "¿ceniza + agua?", "¿bizcocho + arena?",
        };

        /// <summary>La receta YA RESUELTA (mismo orden que _cruces), mostrada solo tras descubrir el producto -- el nombre real del resultado se añade en tiempo de dibujo (EtiquetaDe), nunca concatenado aquí (cero allocs).</summary>
        private static readonly string[] _cruceIngredientes =
        {
            "cal apagada + arena", "caliza + arcilla", "clínker + arena",
            "arena + ceniza", "ceniza + agua", "bizcocho + arena",
        };

        /// <summary>Cabecera de familia mientras su Polvo (la raíz) sigue sin descubrir -- literales YA en mayúsculas (cero allocs), indexadas por baseIdx (0..4).</summary>
        private static readonly string[] _familiaFallback = { "FAMILIA 1", "FAMILIA 2", "FAMILIA 3", "FAMILIA 4", "FAMILIA 5" };

        /// <summary>Total de figuritas del retículo (5 bases × 8 estados) -- la M de "N/M". Fijo: NO se reduce por casos "sin entrada" (p.ej. la arena no se disuelve, diseño §2) -- esa celda simplemente queda para siempre como "?", una verdad del universo tan legítima como cualquier otra ficha sin completar.</summary>
        private const int TotalFiguritas = MaterialId.BasesCount * 8;

        /// <summary>Páginas del álbum: una por familia + la de los clásicos del arco + (playtest 47, ENCARGO C) la de MEZCLAS DEL OFICIO, al final de todo. Lo lee Game/JournalHud.cs para su paginación real (botones "anterior/siguiente" + Re Pág/Av Pág del libro), así que el álbum de la pestaña y el de pantalla completa pasan página con el MISMO criterio.</summary>
        public static int PaginasTotales => MaterialId.BasesCount + 2;

        /// <summary>Índice de página de los clásicos del arco (la sexta, tras las 5 familias).</summary>
        private const int PaginaClasicos = MaterialId.BasesCount;

        /// <summary>(playtest 47, ENCARGO C) Índice de página de MEZCLAS DEL OFICIO -- la séptima, tras los clásicos (contrato §1e, literal).</summary>
        private const int PaginaMezclas = MaterialId.BasesCount + 1;

        // -----------------------------------------------------------------
        // Dependencias / estado.
        // -----------------------------------------------------------------
        private AlkahestSim _sim;
        private SubstanceKnowledge _knowledge;

        private bool _visible;   // pantalla completa del álbum (tecla B).
        private int _pagina;     // página abierta en la pantalla completa (la pestaña del diario lleva la suya, ver JournalHud._pagina).

        /// <summary>True mientras el álbum a pantalla completa O la ficha-vitrina tapan la pantalla -- mismo contrato que JournalHud.Abierto, para que futuros modos (regla 37 de CLAUDE.md) puedan cederse el paso mutuamente.</summary>
        public static bool Abierto { get; private set; }

        // Cola de descubrimientos pendientes de vitrina (mismo criterio de
        // capacidad y de "se pierde el AVISO, no el registro" que
        // SubstanceKnowledge._leyBannerCola -- ver OnAlgoDescubierto).
        private const int ColaCapacidad = 8;
        private readonly byte[] _cola = new byte[ColaCapacidad];
        private int _colaCount;

        private bool _fichaAbierta;
        private byte _fichaMat;
        /// <summary>Firma visual EN PALABRAS del material de la ficha, cacheada AL ABRIR (DescribirFirma construye un string: llamarlo desde OnGUI sería una asignación por frame) -- mismo patrón que NamingUi._firmaTexto.</summary>
        private string _fichaFirma;
        /// <summary>Cuántas fichas van vistas en esta RÁFAGA (1-based). Con el total (este número + lo que quede en cola) sale el "1 de 3" del titular.</summary>
        private int _fichaIndiceRafaga;
        /// <summary>(playtest 46) ¿Había un álbum a pantalla completa abierto cuando saltó la vitrina? Se cierra para que no se vean los dos a la vez (CAUSA RAÍZ, ver docblock) y se devuelve al terminar de anotar: cerrar el libro por ti sería castigarte por mirar.</summary>
        private bool _restaurarArbolAlCerrar;

        // Numeral del librito (ver DrawLibrito) y contador de ráfaga de la
        // ficha, cacheados -- cero allocs por frame.
        private int _numeralCacheN = -1;
        private string _numeralTexto = "";
        private int _rafagaCacheFirma = -1;
        private string _rafagaTexto = "";

        /// <summary>Fase 0..1 del pulso del librito (~1 Hz), acumulada en Update -- Time.time crudo bastaría, pero acumular en un campo propio deja la puerta abierta a pausar el pulso sin tocar Time.timeScale si algún día hiciera falta.</summary>
        private float _pulsoT;

        /// <summary>
        /// (RONDA 49, LA COLA CON RESPIRO -- ver el docblock largo en
        /// Game/SubstanceKnowledge.cs junto a <see cref="SubstanceKnowledge.PuedeAnunciarTeatro"/>)
        /// True desde el frame en que el librito YA pidió y obtuvo su turno en el reloj
        /// compartido de teatro de descubrimiento, hasta que la cola de vitrinas se vacía del
        /// todo. Mientras sea false y haya algo en <see cref="_cola"/>, `OnGUI` sigue
        /// reintentando cada frame (barato: una comparación de `Time.time`) en vez de dibujar
        /// el librito -- así el pulso NO arranca en el mismo instante que un banner "ALGO
        /// NUEVO" de Game/SubstanceKnowledge.cs sobre el MISMO descubrimiento (en Semilla
        /// Cero los dos canales reaccionan al mismo evento `AlDescubrir`). Se resetea a false
        /// en cuanto <see cref="_colaCount"/> vuelve a 0: la próxima vez que llegue algo será
        /// una ráfaga nueva y debe volver a pedir su hueco, no heredar el turno de la anterior.
        /// </summary>
        private bool _libritoAnunciado;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap (TrySpawn/TrySpawnRed).</summary>
        public void Init(AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            _sim = sim;
            _knowledge = knowledge;
        }

        private void OnEnable() => SubstanceKnowledge.AlDescubrir += OnAlgoDescubierto;
        private void OnDisable() => SubstanceKnowledge.AlDescubrir -= OnAlgoDescubierto;

        /// <summary>
        /// EL MOMENTO: se dispara UNA vez por material, la primera vez que
        /// se descubre (contrato del evento). Solo el retículo base×estado
        /// entra en la cola de vitrinas -- los clásicos ya se conocían.
        /// </summary>
        private void OnAlgoDescubierto(byte matId)
        {
            if (!MaterialId.EsBaseEstado(matId)) return;
            if (_colaCount >= ColaCapacidad) return; // cola llena: se pierde el AVISO de esta vez, no el descubrimiento -- el jugador ya lo tiene marcado y puede repasarlo entrando con B (mismo criterio que SubstanceKnowledge.EncolarLeyBanner).
            _cola[_colaCount++] = matId;
        }

        private void Update()
        {
            if (DayCycle.InputLocked)
            {
                // (mismo criterio que JournalHud.Update) un overlay de jornada manda: no dejar nada nuestro fantasma debajo.
                _visible = false;
                _fichaAbierta = false;
                _restaurarArbolAlCerrar = false;
                Abierto = false;
                return;
            }

            if (UiStyles.EscribiendoTexto)
            {
                Abierto = _visible || _fichaAbierta;
                return; // regla 12 de CLAUDE.md: ningún atajo de una tecla roba letras a un campo de texto.
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                if (_fichaAbierta)
                {
                    // ESC (regla 12: universal) SOLO cierra -- no avanza la cola: es el gesto de
                    // "ahora no", no de "leído". B/Enter SÍ son "anotado en tu álbum": confirman
                    // y, si queda cola, encadenan la siguiente vitrina sin que el jugador tenga
                    // que volver a pulsar B por cada figurita nueva de una misma hornada.
                    if (kb.escapeKey.wasPressedThisFrame) CerrarFicha();
                    else if (kb.bKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                        ConfirmarFicha();
                }
                else if (kb.bKey.wasPressedThisFrame && !JournalHud.Abierto)
                {
                    // B es LA MISMA tecla para dos gestos (diseño §3): si hay algo pendiente,
                    // manda verlo (la vitrina es más urgente que el álbum completo); si no,
                    // abre/cierra el álbum a pantalla completa.
                    if (_colaCount > 0) AbrirSiguienteFicha();
                    else { _visible = !_visible; if (_visible) _pagina = 0; }
                }
                else if (_visible)
                {
                    if (kb.escapeKey.wasPressedThisFrame) _visible = false;
                    // PASAR PÁGINA: las MISMAS teclas que el libro de Game/JournalHud.cs
                    // (Re Pág / Av Pág) más las flechas, que aquí son el gesto natural de
                    // "pasar hoja". DEUDA CONOCIDA: Game/ApprenticeController.cs solo
                    // comprueba JournalHud.Abierto (nunca AlbumReal.Abierto, hueco
                    // preexistente que ese archivo no entra en este encargo), así que con
                    // el álbum a pantalla completa las flechas TAMBIÉN mueven al aprendiz
                    // detrás del velo. Un toque lo desplaza un par de píxeles; quien pueda
                    // tocar ese archivo solo tiene que sumar `&& !AlbumReal.Abierto` a su
                    // guarda de teclado.
                    else if (kb.pageDownKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) CambiarPagina(1);
                    else if (kb.pageUpKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) CambiarPagina(-1);
                }
            }

            Abierto = _visible || _fichaAbierta;
        }

        private void CambiarPagina(int delta)
        {
            int total = PaginasTotales;
            _pagina = Mathf.Clamp(_pagina + delta, 0, total - 1);
        }

        private void AbrirSiguienteFicha()
        {
            if (_colaCount <= 0) return;
            _fichaMat = _cola[0];
            for (int i = 1; i < _colaCount; i++) _cola[i - 1] = _cola[i]; // cola diminuta (≤8): un desplazamiento manual es más barato y más claro que traer List<T>/Queue<T> para esto.
            _colaCount--;

            // (playtest 46, LA CAUSA RAÍZ) UNA SOLA COSA VISIBLE A LA VEZ: si el álbum a
            // pantalla completa estaba abierto, se cierra AHORA (no basta con dibujar la
            // ficha encima: el velo dejaba leerse el árbol entero por debajo) y se anota
            // para devolverlo cuando el jugador termine de anotar.
            if (_visible) { _restaurarArbolAlCerrar = true; _visible = false; }

            _fichaIndiceRafaga++;
            _fichaFirma = _sim != null && _sim.Universe != null ? _sim.Universe.DescribirFirma(_fichaMat) : null;
            _fichaAbierta = true;
        }

        /// <summary>ESC: "ahora no". Cierra la vitrina SIN consumir la cola (lo pendiente sigue pendiente y el librito sigue latiendo) y devuelve el álbum a pantalla completa si estaba abierto.</summary>
        private void CerrarFicha()
        {
            _fichaAbierta = false;
            _fichaIndiceRafaga = 0;
            if (_restaurarArbolAlCerrar) { _restaurarArbolAlCerrar = false; _visible = true; }
        }

        /// <summary>"Anotado en tu álbum": confirma y encadena la siguiente de la ráfaga; cuando no queda ninguna, devuelve el álbum a pantalla completa si de ahí veníamos.</summary>
        private void ConfirmarFicha()
        {
            _fichaAbierta = false;
            if (_colaCount > 0) { AbrirSiguienteFicha(); return; }
            _fichaIndiceRafaga = 0;
            if (_restaurarArbolAlCerrar) { _restaurarArbolAlCerrar = false; _visible = true; }
        }

        // ===================================================================
        // RENDER
        // ===================================================================

        private void OnGUI()
        {
            if (_sim == null || _sim.Universe == null || _knowledge == null) return;
            if (DayCycle.InputLocked) return;

            UiStyles.Preparar();
            PrepararEstilosPropios();
            PrepararEstilosInstancia();

            _pulsoT += Time.deltaTime;

            // (decisión explícita) profundidad MÁS ADELANTE que JournalHud (-1000): un
            // descubrimiento puede llegar mientras el jugador tiene el diario abierto leyendo
            // otra cosa ("también desde la pestaña del diario", diseño §3) -- la vitrina no debe
            // quedar tapada por el libro. GUI.depth solo se toca si de verdad vamos a dibujar
            // algo nuestro este frame (igual disciplina que JournalHud.OnGUI).
            if (_visible || _fichaAbierta) GUI.depth = -2000;

            // (playtest 46, arreglo 2 del solapamiento) LOS DOS NUNCA A LA VEZ. El estado ya
            // garantiza que no puede pasar (AbrirSiguienteFicha baja _visible), pero la
            // condición se repite aquí a propósito: es la línea que hace IMPOSIBLE el bug
            // reportado aunque alguien vuelva a tocar el estado en el futuro.
            if (_visible && !_fichaAbierta) DrawPantallaCompleta();

            // El librito solo tiene sentido si NO hay ya una vitrina abierta y el jugador no
            // está leyendo el álbum completo (ahí ya se ve todo lo nuevo con sus propios ojos).
            if (_colaCount == 0)
            {
                _libritoAnunciado = false; // (RONDA 49) cola vacía: la próxima ráfaga es nueva, vuelve a pedir su turno.
            }
            else if (!_fichaAbierta && !_visible && !DayCycle.HudSilenciado && !JournalHud.Abierto)
            {
                // (RONDA 49, LA COLA CON RESPIRO) El pulso NO arranca solo porque haya algo
                // pendiente: tiene que pedir turno en el reloj compartido con el banner "ALGO
                // NUEVO" de Game/SubstanceKnowledge.cs -- ver el docblock de `_libritoAnunciado`
                // y de `SubstanceKnowledge.PuedeAnunciarTeatro`. Una vez concedido el turno se
                // queda pulsando sin volver a pedirlo (abrir/cerrar la ficha no lo reinicia,
                // solo vaciar la cola entera lo hace).
                if (!_libritoAnunciado && SubstanceKnowledge.PuedeAnunciarTeatro())
                {
                    _libritoAnunciado = true;
                    SubstanceKnowledge.RegistrarAnuncioTeatro();
                }
                if (_libritoAnunciado) DrawLibrito();
            }

            if (_fichaAbierta) DrawFicha();
        }

        // -------------------------------------------------------------
        // PANTALLA COMPLETA (tecla B): mismo velo + panel de latón que
        // JournalHud, y desde el playtest 46 la MISMA doble página (dos hojas
        // + lomo) que el libro, para que el álbum sea el mismo objeto se
        // llegue por donde se llegue.
        // -------------------------------------------------------------
        private static readonly Color _velo = new Color(0.02f, 0.015f, 0.03f, 0.94f); // (playtest 46) 0.90 -> 0.94: a 0.90 el HUD y el árbol de debajo seguían leyéndose (ver CAUSA RAÍZ).
        private static readonly Color _papel = new Color(0.085f, 0.072f, 0.062f, 0.96f);
        private static readonly Color _papelBorde = new Color(0.40f, 0.32f, 0.19f, 0.55f);
        private static readonly Color _lomo = new Color(0.05f, 0.04f, 0.035f, 0.98f);

        private void DrawPantallaCompleta()
        {
            UiStyles.Rellenar(new Rect(0f, 0f, Screen.width, Screen.height), _velo);

            float margenX = UiStyles.S(56f), margenY = UiStyles.S(40f);
            var panel = new Rect(margenX, margenY, Screen.width - margenX * 2f, Screen.height - margenY * 2f);
            UiStyles.Panel(panel, UiStyles.TintaFuerte, UiStyles.LatonOscuro);
            UiStyles.MarcoLaton(panel);

            float pad = UiStyles.S(20f);
            var interior = new Rect(panel.x + pad, panel.y + pad, panel.width - pad * 2f, panel.height - pad * 2f);

            float altoTitulo = _estiloTitulo.lineHeight;
            GUI.Label(new Rect(interior.x, interior.y, interior.width, altoTitulo), UiStyles.Espaciar("ÁLBUM DE FIGURITAS"), _estiloTitulo);

            float altoPie = _estiloAyuda.lineHeight + UiStyles.S(8f);
            var cuerpo = new Rect(interior.x, interior.y + altoTitulo + UiStyles.S(10f), interior.width,
                interior.height - altoTitulo - UiStyles.S(10f) - altoPie);

            // Dos hojas y su lomo, exactamente como el libro del diario (JournalHud.DrawBook).
            float anchoLomo = UiStyles.S(20f);
            float anchoHoja = (cuerpo.width - anchoLomo) * 0.5f;
            var hojaIzq = new Rect(cuerpo.x, cuerpo.y, anchoHoja, cuerpo.height);
            var hojaDer = new Rect(cuerpo.x + anchoHoja + anchoLomo, cuerpo.y, anchoHoja, cuerpo.height);
            UiStyles.Panel(hojaIzq, _papel, _papelBorde);
            UiStyles.Panel(hojaDer, _papel, _papelBorde);
            DrawLomo(new Rect(hojaIzq.xMax, cuerpo.y, anchoLomo, cuerpo.height));

            _pagina = Mathf.Clamp(_pagina, 0, PaginasTotales - 1);
            float padHoja = UiStyles.S(16f);
            DibujarPaginaDoble(
                new Rect(hojaIzq.x + padHoja, hojaIzq.y + padHoja, hojaIzq.width - padHoja * 2f, hojaIzq.height - padHoja * 2f),
                new Rect(hojaDer.x + padHoja, hojaDer.y + padHoja, hojaDer.width - padHoja * 2f, hojaDer.height - padHoja * 2f),
                _sim, _knowledge, _pagina);

            // Pie: los mismos dos gestos que el libro (palabras, no glifos "◀"/"▶" -- ver la
            // nota de glifos en el docblock de JournalHud) y la ayuda de teclado.
            var pie = new Rect(interior.x, interior.yMax - _estiloAyuda.lineHeight, interior.width, _estiloAyuda.lineHeight);
            float anchoBoton = UiStyles.S(96f);
            bool puedeAtras = _pagina > 0, puedeAdelante = _pagina < PaginasTotales - 1;

            GUI.enabled = puedeAtras;
            if (GUI.Button(new Rect(pie.x, pie.y, anchoBoton, pie.height), "< anterior", _estiloBotonPagina) && puedeAtras) CambiarPagina(-1);
            GUI.enabled = puedeAdelante;
            if (GUI.Button(new Rect(pie.xMax - anchoBoton, pie.y, anchoBoton, pie.height), "siguiente >", _estiloBotonPagina) && puedeAdelante) CambiarPagina(1);
            GUI.enabled = true;

            GUI.Label(pie, TextoAyudaAlbum, _estiloAyuda);
        }

        private const string TextoAyudaAlbum = "B / ESC — cerrar · Re Pág / Av Pág o ← → — pasar página";

        private static void DrawLomo(Rect r)
        {
            UiStyles.Rellenar(r, _lomo);
            float grosor = Mathf.Max(1f, Mathf.Round(UiStyles.Escala));
            float centro = r.x + r.width * 0.5f;
            UiStyles.Rellenar(new Rect(centro - grosor * 0.5f, r.y, grosor, r.height), UiStyles.OroTenue);
        }

        // -------------------------------------------------------------
        // EL LIBRITO QUE LATE (antes: un cuadrado dorado, ver reproche (a)
        // en el docblock). Vive junto al panel de encargos (OrdersHud está en
        // Screen.width - S(300) - S(10)) y se ancla A SU IZQUIERDA para no
        // competir por el mismo rincón.
        //
        // ANATOMÍA, de atrás hacia delante:
        //   halo (3 aureolas concéntricas que respiran) · sombra proyectada ·
        //   tapa de cuero · lomo de latón con dos nervios · canto de páginas
        //   de vitela · filete de latón alrededor de la tapa · rombo central
        //   (el único elemento que cambia de BRILLO con el pulso) · numeral.
        // Lo que late es la LUZ, no el tamaño: el medallón anterior escalaba
        // un 12% y eso es el lenguaje de una alarma, no el de un objeto vivo.
        // -------------------------------------------------------------
        // (VISTO EN EL EDITOR) 40x46 -> 46x54 y el cuero un punto más claro: a tamaño de
        // icono y con el muro de piedra oscuro detrás, el libro se leía como una mancha.
        // Sigue siendo MÁS PEQUEÑO que el panel de encargos que tiene al lado -- es un
        // aviso, no un segundo HUD.
        private const float LibritoAncho = 46f, LibritoAlto = 54f;
        private static readonly Color _cuero = new Color(0.215f, 0.150f, 0.112f, 1f);
        private static readonly Color _cueroClaro = new Color(0.285f, 0.205f, 0.150f, 1f);
        private static readonly Color _vitela = new Color(0.82f, 0.775f, 0.655f, 1f);
        private static readonly Color _vitelaSombra = new Color(0.58f, 0.535f, 0.435f, 1f);

        private void DrawLibrito()
        {
            float w = UiStyles.S(LibritoAncho), h = UiStyles.S(LibritoAlto);
            float margen = UiStyles.S(10f);
            float xOrdenes = Screen.width - UiStyles.S(300f) - margen; // borde izquierdo real del panel de OrdersHud.
            var r = new Rect(xOrdenes - margen - w, margen + UiStyles.S(2f), w, h);

            // Botón invisible PRIMERO: el hit-test no depende de qué se dibuje encima ni del
            // pulso (el rect de clic es estable; solo el DIBUJO respira).
            if (GUI.Button(r, GUIContent.none, GUIStyle.none)) AbrirSiguienteFicha();

            float onda = 0.5f + 0.5f * Mathf.Sin(_pulsoT * Mathf.PI * 2f); // ~1 Hz, 0..1.

            // 1) HALO: tres aureolas cada vez más grandes y más tenues. Nada de rojo, nada
            // estridente -- es la luz que sale de entre las páginas de un libro entreabierto.
            for (int i = 0; i < 3; i++)
            {
                float crece = UiStyles.S(3f + i * 3.5f);
                float alfa = (0.20f - i * 0.055f) * (0.35f + 0.65f * onda);
                UiStyles.Rellenar(new Rect(r.x - crece, r.y - crece, r.width + crece * 2f, r.height + crece * 2f),
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, alfa));
            }

            // 2) Sombra propia: el libro apoya sobre el HUD, no flota recortado.
            float desp = Mathf.Max(1f, UiStyles.S(2f));
            UiStyles.Rellenar(new Rect(r.x + desp, r.y + desp, r.width, r.height), new Color(0f, 0f, 0f, 0.45f));

            // 3) La tapa (cuero) con su canto superior más claro: un plano de color sin
            // ningún matiz se lee como un icono de sistema, no como un objeto.
            UiStyles.Rellenar(r, _cuero);
            UiStyles.Rellenar(new Rect(r.x, r.y, r.width, Mathf.Max(1f, UiStyles.S(2f))), _cueroClaro);

            // 4) El LOMO, a la izquierda, en latón, con dos nervios (las costuras de un
            // libro encuadernado a mano) -- el brillo del metal sube con el pulso.
            float anchoLomo = Mathf.Max(3f, r.width * 0.22f);
            var lomo = new Rect(r.x, r.y, anchoLomo, r.height);
            var laton = Color.Lerp(UiStyles.LatonOscuro, UiStyles.Laton, 0.35f + 0.65f * onda);
            UiStyles.Rellenar(lomo, laton);
            float nervio = Mathf.Max(1f, UiStyles.S(1.5f));
            UiStyles.Rellenar(new Rect(lomo.x, lomo.y + r.height * 0.28f, lomo.width, nervio), UiStyles.LatonOscuro);
            UiStyles.Rellenar(new Rect(lomo.x, lomo.y + r.height * 0.68f, lomo.width, nervio), UiStyles.LatonOscuro);
            UiStyles.Rellenar(new Rect(lomo.xMax - nervio, lomo.y, nervio, lomo.height), new Color(0f, 0f, 0f, 0.35f));

            // 5) El CANTO DE PÁGINAS a la derecha: tres filos de vitela. Es lo que hace que
            // se lea "libro CERRADO" y no "tarjeta".
            float anchoCanto = Mathf.Max(2f, r.width * 0.13f);
            var canto = new Rect(r.xMax - anchoCanto, r.y + UiStyles.S(2f), anchoCanto, r.height - UiStyles.S(4f));
            UiStyles.Rellenar(canto, _vitela);
            float hoja = Mathf.Max(1f, UiStyles.S(1f));
            UiStyles.Rellenar(new Rect(canto.x, canto.y + canto.height * 0.30f, canto.width, hoja), _vitelaSombra);
            UiStyles.Rellenar(new Rect(canto.x, canto.y + canto.height * 0.62f, canto.width, hoja), _vitelaSombra);

            // 6) Filete de latón alrededor de la tapa (el mismo gesto que MarcoLaton, a
            // escala de icono: aquí las cantoneras de MarcoLaton se comerían el dibujo).
            float filo = Mathf.Max(1f, UiStyles.S(1f));
            var filete = new Color(UiStyles.Laton.r, UiStyles.Laton.g, UiStyles.Laton.b, 0.85f);
            UiStyles.Rellenar(new Rect(r.x, r.y, r.width, filo), filete);
            UiStyles.Rellenar(new Rect(r.x, r.yMax - filo, r.width, filo), filete);
            UiStyles.Rellenar(new Rect(r.xMax - filo, r.y, filo, r.height), filete);

            // 7) EL ROMBO de la tapa: el sello del libro, y lo único cuyo COLOR late de
            // OroTenue a Oro. Cuatro rectángulos = un cuadrado girado 45º (misma técnica
            // que UiStyles.FileteRombo, que no sirve aquí porque trae su propia línea).
            var sello = Color.Lerp(UiStyles.OroTenue, UiStyles.Oro, onda);
            float cx = r.x + anchoLomo + (r.width - anchoLomo - anchoCanto) * 0.5f;
            float cy = r.y + r.height * 0.44f;
            float paso = Mathf.Max(1f, Mathf.Round(UiStyles.S(1.6f)));
            for (int i = 0; i < 3; i++)
            {
                float ancho = paso * (i + 1);
                UiStyles.Rellenar(new Rect(cx - ancho * 0.5f, cy - paso * (2 - i), ancho, paso), sello);
                UiStyles.Rellenar(new Rect(cx - ancho * 0.5f, cy + paso * (2 - i), ancho, paso), sello);
            }
            UiStyles.Rellenar(new Rect(cx - paso * 1.5f, cy, paso * 3f, paso), sello);

            // 8) NUMERAL SOBRIO: solo si hay MÁS DE UNO esperando, en una chapita de latón
            // en la esquina inferior -- cacheado contra el último valor mostrado (cero
            // allocs: ToString() solo corre cuando _colaCount cambió de verdad, mismo
            // criterio que JournalHud._pieTexto).
            if (_colaCount > 1)
            {
                if (_colaCount != _numeralCacheN) { _numeralCacheN = _colaCount; _numeralTexto = _colaCount.ToString(); }
                float lado = UiStyles.S(15f);
                var chapa = new Rect(r.xMax - lado * 0.55f, r.yMax - lado * 0.75f, lado, lado);
                UiStyles.Rellenar(new Rect(chapa.x - filo, chapa.y - filo, chapa.width + filo * 2f, chapa.height + filo * 2f), UiStyles.LatonOscuro);
                UiStyles.Rellenar(chapa, UiStyles.Oro);
                GUI.Label(chapa, _numeralTexto, _estiloNumeral);
            }
        }

        // -------------------------------------------------------------
        // LA FICHA-VITRINA: la anatomía de Game/NamingUi.cs, paso a paso
        // (ver reproche (b) en el docblock de la clase). El ALTO se calcula a
        // partir del contenido en vez de ser una constante: la reseña de
        // trivia varía de una línea a cuatro, y con alto fijo o sobraba medio
        // panel vacío o el texto pisaba el botón.
        // -------------------------------------------------------------
        private const float FichaAncho = 460f;

        private void DrawFicha()
        {
            UiStyles.Rellenar(new Rect(0f, 0f, Screen.width, Screen.height), _velo);

            float w = UiStyles.S(FichaAncho);
            float pad = UiStyles.S(22f);
            float ancho = w - pad * 2f;

            string nombre = EtiquetaDe(_fichaMat);
            string resena = ResenaDe(_fichaMat);
            float ladoMuestra = UiStyles.S(92f);

            // --- Medición previa (sin dibujar): el panel se ajusta al contenido.
            int total = _fichaIndiceRafaga + _colaCount;
            bool hayContador = total >= 2;
            float altoTitulo = UiStyles.TituloRito.lineHeight;
            float altoContador = hayContador ? UiStyles.Ceremonial.lineHeight + UiStyles.S(2f) : 0f;
            float altoResena = UiStyles.Alto(UiStyles.Cuerpo, resena, ancho);
            float altoCeremonia = UiStyles.Alto(UiStyles.Ceremonial, LineaCeremonial, ancho);
            float altoBoton = UiStyles.S(32f);
            float altoAyuda = UiStyles.TenueCentrado.lineHeight;

            float h = UiStyles.S(18f) + altoTitulo + altoContador + UiStyles.S(9f) + UiStyles.S(14f)
                    + ladoMuestra + UiStyles.S(16f)
                    + altoResena + UiStyles.S(12f)
                    + altoCeremonia + UiStyles.S(12f)
                    + altoBoton + UiStyles.S(6f) + altoAyuda + UiStyles.S(18f);

            var r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            UiStyles.PanelRito(r);

            float x = r.x + pad;
            float y = r.y + UiStyles.S(18f);

            // ---- TÍTULO: capital lapidaria espaciada. "ALGO NUEVO" a secas -- el
            // anterior ("ALGO NUEVO EN TU ÁLBUM") medía casi el ancho entero del panel
            // y un título que toca los dos márgenes no respira, que es justo lo que
            // separa esta tarjeta del rito del bautizo.
            GUI.Label(new Rect(x, y, ancho, altoTitulo), UiStyles.Espaciar("ALGO NUEVO"), UiStyles.TituloRito);
            y += altoTitulo;

            // ---- "1 de 3": SOLO cuando de verdad llegaron varias a la vez. (Decisión
            // explícita fuera de la letra del contrato, que pedía el contador a partir de
            // 3: se muestra ya con 2, porque el reproche de Cesar nació justamente de una
            // ráfaga de DOS y saber que queda otra detrás es la mitad del arreglo.)
            if (hayContador)
            {
                int firma = _fichaIndiceRafaga * 100 + total;
                if (firma != _rafagaCacheFirma)
                {
                    _rafagaCacheFirma = firma;
                    _rafagaTexto = _fichaIndiceRafaga + " de " + total;
                }
                GUI.Label(new Rect(x, y, ancho, UiStyles.Ceremonial.lineHeight), _rafagaTexto, UiStyles.Ceremonial);
                y += altoContador;
            }

            y += UiStyles.S(9f);
            UiStyles.FileteRombo(r.x + r.width * 0.5f, y, ancho * 0.80f, UiStyles.Laton); // (igual que NamingUi) el filete es del mismo metal que el marco: LatonOscuro sobre vitela oscura no se ve.
            y += UiStyles.S(14f);

            // ---- LA MUESTRA, EN GRANDE, con su marco de latón: el jugador mira LA
            // SUSTANCIA mientras lee su nombre, no un cuadradito de leyenda.
            var marco = new Rect(x, y, ladoMuestra, ladoMuestra);
            UiStyles.Rellenar(marco, new Color(0f, 0f, 0f, 0.55f));
            var dentro = new Rect(marco.x + UiStyles.S(4f), marco.y + UiStyles.S(4f), marco.width - UiStyles.S(8f), marco.height - UiStyles.S(8f));
            var tex = ObtenerMuestra(_fichaMat, _sim);
            if (tex != null) GUI.DrawTexture(dentro, tex);
            else UiStyles.Rellenar(dentro, _sim.Universe.Get(_fichaMat).baseColor);
            UiStyles.MarcoLaton(marco, UiStyles.Laton, 0.9f);

            // ---- A la derecha de la muestra: rótulo tenue, nombre en Cinzel dorado y la
            // firma visual descrita en palabras (el mismo "retrato hablado" del bautizo).
            float xTexto = marco.xMax + UiStyles.S(16f);
            float anchoTexto = r.xMax - pad - xTexto;
            GUI.Label(new Rect(xTexto, y + UiStyles.S(2f), anchoTexto, UiStyles.CuerpoTenue.lineHeight), "SE LLAMA", UiStyles.CuerpoTenue);

            var estiloNombre = UiStyles.NombreGrande;
            var previo = estiloNombre.normal.textColor;
            estiloNombre.normal.textColor = UiStyles.Oro; // el nombre de algo recién descubierto SIEMPRE es oro: es el premio de la tarjeta.
            GUI.Label(new Rect(xTexto, y + UiStyles.S(18f), anchoTexto, UiStyles.S(30f)), nombre, estiloNombre);
            estiloNombre.normal.textColor = previo;

            if (!string.IsNullOrEmpty(_fichaFirma))
            {
                float yFirma = y + UiStyles.S(48f);
                float altoFirma = UiStyles.Alto(UiStyles.Ceremonial, _fichaFirma, anchoTexto);
                GUI.Label(new Rect(xTexto, yFirma, anchoTexto, altoFirma), _fichaFirma, UiStyles.Ceremonial);
            }

            y = marco.yMax + UiStyles.S(16f);

            // ---- LA RESEÑA, en Alegreya (el cuerpo del proyecto), a ancho completo.
            GUI.Label(new Rect(x, y, ancho, altoResena), resena, UiStyles.Cuerpo);
            y += altoResena + UiStyles.S(12f);

            // ---- LA LÍNEA CEREMONIAL (el equivalente exacto de "El nombre que le des lo
            // verá todo el taller"): dice qué acaba de pasar en el mundo, no en la UI.
            GUI.Label(new Rect(x, y, ancho, altoCeremonia), LineaCeremonial, UiStyles.Ceremonial);
            y += altoCeremonia + UiStyles.S(12f);

            // ---- EL GESTO ÚNICO. Centrado y estrecho: un botón a todo lo ancho es una
            // barra de diálogo de sistema, justo lo que este panel viene a no ser.
            float anchoBoton = ancho * 0.62f;
            if (GUI.Button(new Rect(r.x + (r.width - anchoBoton) * 0.5f, y, anchoBoton, altoBoton), "Anotado en tu álbum", UiStyles.Boton))
                ConfirmarFicha();
            y += altoBoton + UiStyles.S(6f);

            GUI.Label(new Rect(x, y, ancho, altoAyuda), TextoAyudaFicha, UiStyles.TenueCentrado);
        }

        private const string LineaCeremonial = "Queda anotado en tu álbum: el taller ya sabe que existe.";
        private const string TextoAyudaFicha = "B / Enter — anotar · ESC — dejarlo para luego";

        /// <summary>
        /// (Encargo Q, docblock de <see cref="Universe.TieneIdentidadReal"/>) LA TABLA ES
        /// PURA: describe SIEMPRE la seed 777002 y no se autoprotege -- es responsabilidad
        /// de cada llamante restringirla a Semilla Cero. Único punto de esta clase donde
        /// se hace esa comprobación, para no repetir el `&amp;&amp; AlkahestGameBootstrap.ModoSemillaCero`
        /// en cada sitio que consulta identidad.
        /// </summary>
        private static bool TieneIdentidad(byte matId) => AlkahestGameBootstrap.ModoSemillaCero && Universe.TieneIdentidadReal(matId);

        /// <summary>Nombre a mostrar para `matId`: el real de trivia si este universo lo tiene (contrato del encargo Q), o el de siempre (provisional/bautizado) si no -- ver docblock de clase.</summary>
        private static string EtiquetaDe(byte matId, SubstanceKnowledge knowledge)
        {
            if (TieneIdentidad(matId)) return Universe.NombreReal(matId);
            return knowledge != null ? knowledge.NombreDe(matId) : "???";
        }
        private string EtiquetaDe(byte matId) => EtiquetaDe(matId, _knowledge);

        private const string SinResena = "aún por estudiar";

        /// <summary>Reseña de trivia si la hay, o la invitación honesta a seguir jugando si no (diseño §3: "sin reseña" en caótico -- el modo caótico conserva su variante).</summary>
        private static string ResenaDe(byte matId) => TieneIdentidad(matId) ? Universe.ResenaReal(matId) : SinResena;

        // ===================================================================
        // EL ÁLBUM PAGINADO (estático: sin estado propio, para que
        // JournalHud lo llame sin necesitar una referencia a esta instancia).
        //
        // (playtest 46) SUSTITUYE al antiguo `DibujarArbol(Rect, sim,
        // knowledge)`, que metía las 5 familias + los clásicos en UN lienzo
        // continuo. La idea que se DESCARTA -- y por qué, para que nadie la
        // reimplante creyendo que es nueva (regla 15 de CLAUDE.md): aquel
        // lienzo único se justificaba diciendo que "el árbol es una figura,
        // no un texto largo, así que no pagina". Es cierto para UNA familia y
        // falso para cinco: a 30 px de caja y con los verbos pisándose, la
        // figura dejaba de leerse como figura. Un álbum de cromos tampoco
        // enseña las 400 casillas a la vez -- enseña una hoja, y pasar hoja
        // ES el gesto que hace que quieras llenarla.
        // ===================================================================

        /// <summary>
        /// Dibuja UNA doble página del álbum. `pagina` en [0, <see cref="PaginasTotales"/>):
        /// 0..BasesCount-1 = una familia (izquierda las ocho figuritas, derecha su árbol de
        /// verbos), la última = los clásicos del arco. Usado por la pantalla completa de
        /// esta clase Y por la pestaña ÁLBUM de Game/JournalHud.cs -- un único dibujo del
        /// álbum en todo el proyecto.
        /// </summary>
        public static void DibujarPaginaDoble(Rect izq, Rect der, AlkahestSim sim, SubstanceKnowledge knowledge, int pagina)
        {
            if (sim == null || sim.Universe == null || knowledge == null) return;
            PrepararEstilosPropios();

            int descubiertas = 0;
            for (int b = 0; b < MaterialId.BasesCount; b++)
                for (int e = 0; e < 8; e++)
                    if (knowledge.EsDescubierto(MaterialId.MatDe(b, (EstadoMateria)e))) descubiertas++;

            if (descubiertas != _progresoCacheN)
            {
                _progresoCacheN = descubiertas;
                _progresoTexto = descubiertas + " / " + TotalFiguritas;
            }

            pagina = Mathf.Clamp(pagina, 0, PaginasTotales - 1);
            if (pagina == PaginaMezclas) DibujarPaginaMezclas(izq, der, sim, knowledge); // (playtest 47, ENCARGO C)
            else if (pagina == PaginaClasicos) DibujarPaginaClasicos(izq, der, sim, knowledge);
            else DibujarPaginaFamilia(izq, der, pagina, sim, knowledge);

            // PROGRESO TOTAL en la esquina de la página derecha, siempre presente y siempre
            // discreto: es el marcador del álbum entero, no el de esta hoja.
            var esquina = new Rect(der.x, der.yMax - _estiloProgresoTotal.lineHeight, der.width, _estiloProgresoTotal.lineHeight);
            GUI.Label(esquina, "ÁLBUM " + _progresoTexto, _estiloProgresoTotal);
        }

        private static int _progresoCacheN = -1;
        private static string _progresoTexto = "";
        private static readonly int[] _progresoFamCacheN = new int[MaterialId.BasesCount];
        private static readonly string[] _progresoFamTexto = new string[MaterialId.BasesCount];

        // -------------------------------------------------------------
        // PÁGINA DE FAMILIA
        // -------------------------------------------------------------
        private static void DibujarPaginaFamilia(Rect izq, Rect der, int baseIdx, AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            // ---- CABECERA (hoja izquierda): el nombre real de la base en grande, con su
            // filete. El nombre solo aparece si su Polvo (la raíz) ya se descubrió --
            // enseñarlo antes sería un spoiler igual de grave que enseñar el nombre de una
            // ley que el jugador no ha presenciado (regla 13 de CLAUDE.md).
            byte matPolvo = MaterialId.MatDe(baseIdx, EstadoMateria.Polvo);
            string cabecera = knowledge.EsDescubierto(matPolvo) ? EtiquetaDe(matPolvo, knowledge) : _familiaFallback[baseIdx];

            float altoCabecera = _estiloCabeceraFamilia.lineHeight;
            GUI.Label(new Rect(izq.x, izq.y, izq.width, altoCabecera), cabecera, _estiloCabeceraFamilia);

            // Progreso DE ESTA FAMILIA (3 / 8) bajo su nombre: el número que dice "me faltan
            // cinco" sin tener que contar cuadros grises.
            int hechas = 0;
            for (int e = 0; e < 8; e++) if (knowledge.EsDescubierto(MaterialId.MatDe(baseIdx, (EstadoMateria)e))) hechas++;
            if (_progresoFamCacheN[baseIdx] != hechas + 1) // +1 para que el 0 inicial del array no pase por "ya cacheado".
            {
                _progresoFamCacheN[baseIdx] = hechas + 1;
                _progresoFamTexto[baseIdx] = hechas + " / 8";
            }
            float yProg = izq.y + altoCabecera + UiStyles.S(2f);
            GUI.Label(new Rect(izq.x, yProg, izq.width, _estiloProgresoFamilia.lineHeight), _progresoFamTexto[baseIdx], _estiloProgresoFamilia);

            float yFilete = yProg + _estiloProgresoFamilia.lineHeight + UiStyles.S(7f);
            UiStyles.FileteRombo(izq.x + izq.width * 0.5f, yFilete, izq.width * 0.86f, UiStyles.Laton);

            // ---- LAS OCHO FIGURITAS, 2 columnas × 4 filas de mini-vitrinas.
            var rejilla = new Rect(izq.x, yFilete + UiStyles.S(10f), izq.width, izq.yMax - yFilete - UiStyles.S(10f));
            float anchoCelda = rejilla.width / 2f;
            float altoCelda = rejilla.height / 4f;
            for (int e = 0; e < 8; e++)
            {
                int col = e % 2, fila = e / 2;
                var celda = new Rect(rejilla.x + anchoCelda * col, rejilla.y + altoCelda * fila, anchoCelda, altoCelda);
                DibujarVitrina(celda, MaterialId.MatDe(baseIdx, (EstadoMateria)e), _rotuloEstado[e], sim, knowledge);
            }

            // ---- HOJA DERECHA: el árbol de verbos DE ESTA FAMILIA, con la página entera
            // para él. Aquí los nodos no llevan nombre a propósito: los nombres ya están,
            // grandes y ordenados, en la hoja de al lado -- repetirlos era exactamente lo
            // que hacía que se pisaran.
            float altoTituloDer = _estiloCabeceraFamilia.lineHeight;
            GUI.Label(new Rect(der.x, der.y, der.width, altoTituloDer), "CÓMO SE LLEGA", _estiloCabeceraFamilia);
            float yFileteDer = der.y + altoTituloDer + UiStyles.S(7f);
            UiStyles.FileteRombo(der.x + der.width * 0.5f, yFileteDer, der.width * 0.86f, UiStyles.Laton);

            float altoPieDer = _estiloNotaArbol.lineHeight + _estiloProgresoTotal.lineHeight + UiStyles.S(8f);
            var arbol = new Rect(der.x, yFileteDer + UiStyles.S(12f), der.width, der.height - (yFileteDer - der.y) - UiStyles.S(12f) - altoPieDer);
            DibujarArbolFamilia(arbol, baseIdx, sim, knowledge);

            GUI.Label(new Rect(der.x, arbol.yMax + UiStyles.S(4f), der.width, _estiloNotaArbol.lineHeight), NotaArbol, _estiloNotaArbol);
        }

        private const string NotaArbol = "el verbo dice por dónde se pasa, no cuánto calor hace falta";

        private static void DibujarArbolFamilia(Rect area, int baseIdx, AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            // Tres filas: 0 (Polvo, la raíz), 1 y 2. La raíz se lleva menos alto porque solo
            // tiene una caja; las otras dos reparten el resto a partes casi iguales.
            float fila0H = area.height * 0.26f;
            float fila1H = area.height * 0.37f;
            float fila2H = area.height - fila0H - fila1H;
            float y0 = area.y, y1 = area.y + fila0H, y2 = y1 + fila1H;

            float ladoCaja = Mathf.Clamp(Mathf.Min(area.width * 0.11f, fila1H * 0.46f), UiStyles.S(20f), UiStyles.S(40f));

            // Centros de cada nodo, en coordenadas de pantalla -- se calculan primero (sin
            // dibujar nada) para poder trazar los conectores DEBAJO de las cajas.
            Vector2 CentroDe(int idx)
            {
                var n = _nodos[idx];
                float cy = n.Fila == 0 ? y0 + fila0H * 0.5f : n.Fila == 1 ? y1 + fila1H * 0.5f : y2 + fila2H * 0.42f;
                return new Vector2(area.x + area.width * n.XFrac, cy);
            }

            // 1) Conectores + verbo (debajo de las figuritas).
            //    El ancho máximo del rótulo es la MITAD de la separación mínima entre
            //    hermanos de esa fila: es la cota que hace imposible el solapamiento que
            //    reportó Cesar (ver CAUSA RAÍZ en el docblock de la clase).
            float anchoMaxFila1 = area.width * 0.26f;
            float anchoMaxFila2 = area.width * 0.20f;
            for (int i = 1; i < _nodos.Length; i++)
            {
                var n = _nodos[i];
                DibujarConector(CentroDe(n.Padre), CentroDe(i), n.Verbo, ladoCaja, n.Fila == 1 ? anchoMaxFila1 : anchoMaxFila2);
            }

            // 2) Figuritas encima (sin etiqueta: los nombres viven en la hoja izquierda).
            for (int i = 0; i < _nodos.Length; i++)
            {
                byte matId = MaterialId.MatDe(baseIdx, (EstadoMateria)i);
                Vector2 c = CentroDe(i);
                var box = new Rect(c.x - ladoCaja * 0.5f, c.y - ladoCaja * 0.5f, ladoCaja, ladoCaja);
                DibujarFigurita(box, matId, sim, knowledge, siempreRevelado: false);
            }
        }

        private static void DibujarConector(Vector2 desde, Vector2 hasta, string verbo, float ladoCaja, float anchoMaxVerbo)
        {
            float grosor = Mathf.Max(1f, UiStyles.S(1.5f));
            float ini = ladoCaja * 0.5f; // salir/entrar desde el borde de la caja, no desde su centro.
            float py = desde.y + ini, cy = hasta.y - ini;
            float midY = py + (cy - py) * 0.5f;

            var color = new Color(UiStyles.Laton.r, UiStyles.Laton.g, UiStyles.Laton.b, 0.55f);
            UiStyles.Rellenar(new Rect(desde.x - grosor * 0.5f, py, grosor, Mathf.Max(0f, midY - py)), color);
            UiStyles.Rellenar(new Rect(Mathf.Min(desde.x, hasta.x), midY - grosor * 0.5f, Mathf.Abs(hasta.x - desde.x) + grosor, grosor), color);
            UiStyles.Rellenar(new Rect(hasta.x - grosor * 0.5f, midY, grosor, Mathf.Max(0f, cy - midY)), color);

            if (string.IsNullOrEmpty(verbo)) return;

            // (playtest 46, ARREGLO DEL SOLAPAMIENTO) El verbo se ancla a SU HIJO, no al
            // punto medio del arco: en la fila 1 los cuatro puntos medios caían casi
            // encima unos de otros (todos cuelgan de la misma raíz), así que los cuatro
            // rótulos se imprimían superpuestos. Sobre el hijo, cada uno vive en su propia
            // columna. Además el rect se acota a `anchoMaxVerbo` y el estilo recorta
            // (clipping = Clip), así que ni un verbo largo ni una página estrecha pueden
            // volver a producir el amasijo.
            float w = Mathf.Min(UiStyles.Ancho(_estiloVerbo, verbo) + UiStyles.S(8f), anchoMaxVerbo);
            var rv = new Rect(hasta.x - w * 0.5f, midY - _estiloVerbo.lineHeight - UiStyles.S(1f), w, _estiloVerbo.lineHeight);
            // (VISTO EN EL EDITOR) NADA de chapa oscura detrás del verbo: se probó, y sobre el
            // papel claro del diario (JournalHud._papel) cada rótulo quedaba dentro de un
            // recuadro negro, como si estuviera seleccionado. El verbo va SOBRE la línea
            // horizontal, no encima de ella, así que no hay nada de lo que protegerlo.
            GUI.Label(rv, verbo, _estiloVerbo);
        }

        // -------------------------------------------------------------
        // PÁGINA DE CLÁSICOS
        // -------------------------------------------------------------
        private static void DibujarPaginaClasicos(Rect izq, Rect der, AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            float altoCabecera = _estiloCabeceraFamilia.lineHeight;
            GUI.Label(new Rect(izq.x, izq.y, izq.width, altoCabecera), "CLÁSICOS DEL ARCO", _estiloCabeceraFamilia);
            GUI.Label(new Rect(der.x, der.y, der.width, altoCabecera), "CLÁSICOS DEL ARCO", _estiloCabeceraFamilia);

            float yFilete = izq.y + altoCabecera + UiStyles.S(7f);
            UiStyles.FileteRombo(izq.x + izq.width * 0.5f, yFilete, izq.width * 0.86f, UiStyles.Laton);
            UiStyles.FileteRombo(der.x + der.width * 0.5f, yFilete, der.width * 0.86f, UiStyles.Laton);

            float yNota = yFilete + UiStyles.S(8f);
            GUI.Label(new Rect(izq.x, yNota, izq.width, _estiloNotaArbol.lineHeight), NotaClasicos, _estiloNotaArbol);

            // 5 en la hoja izquierda, 4 en la derecha: el reparto natural de nueve piezas en
            // dos hojas (no hay ninguna razón para que sobre una columna vacía).
            float yRejilla = yNota + _estiloNotaArbol.lineHeight + UiStyles.S(10f);
            var rejillaIzq = new Rect(izq.x, yRejilla, izq.width, izq.yMax - yRejilla);
            var rejillaDer = new Rect(der.x, yRejilla, der.width, der.yMax - yRejilla - _estiloProgresoTotal.lineHeight);

            // (VISTO EN EL EDITOR) LAS DOS HOJAS COMPARTEN EL ALTO DE FILA. Antes cada hoja
            // dividía SU alto entre SUS propias filas (3 a la izquierda, 2 a la derecha), y
            // las piezas de la derecha quedaban flotando a distinta altura que las de la
            // izquierda -- una doble página desalineada se lee como un error de maqueta, no
            // como una composición. Ahora las dos usan las filas de la hoja MÁS LLENA.
            const int enIzq = 5;
            int enDer = _clasicos.Length - enIzq;
            int filasMax = (enIzq + 1) / 2;
            float altoCelda = rejillaIzq.height / filasMax;

            for (int i = 0; i < enIzq; i++)
            {
                int col = i % 2, fila = i / 2;
                var celda = new Rect(rejillaIzq.x + rejillaIzq.width * 0.5f * col, rejillaIzq.y + altoCelda * fila,
                    rejillaIzq.width * 0.5f, altoCelda);
                DibujarVitrina(celda, _clasicos[i], null, sim, knowledge);
            }
            for (int i = 0; i < enDer; i++)
            {
                int col = i % 2, fila = i / 2;
                var celda = new Rect(rejillaDer.x + rejillaDer.width * 0.5f * col, rejillaDer.y + altoCelda * fila,
                    rejillaDer.width * 0.5f, altoCelda);
                DibujarVitrina(celda, _clasicos[enIzq + i], null, sim, knowledge);
            }
        }

        private const string NotaClasicos = "el vocabulario del taller: lo conoces desde el primer día";

        // -------------------------------------------------------------
        // (playtest 47, ENCARGO C, CONTRATO_FASE_A.md §1e) PÁGINA "MEZCLAS
        // DEL OFICIO" -- SOLO ADITIVO: nueva página, misma anatomía de doble
        // hoja que el resto del álbum, tocando lo MÍNIMO del código
        // existente (dos líneas en PaginasTotales/DibujarPaginaDoble, ver
        // arriba). Izquierda: las 6 figuritas en mini-vitrinas (mismo
        // DibujarVitrina que ya usan clásicos y familias -- ningún dibujo
        // nuevo). Derecha: EN VEZ del árbol de familia, las recetas COMO
        // PREGUNTAS que se revelan al descubrir (contrato, literal) -- una
        // fila de texto por cruce, no una figura.
        // -------------------------------------------------------------
        private static void DibujarPaginaMezclas(Rect izq, Rect der, AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            float altoCabecera = _estiloCabeceraFamilia.lineHeight;
            GUI.Label(new Rect(izq.x, izq.y, izq.width, altoCabecera), "MEZCLAS DEL OFICIO", _estiloCabeceraFamilia);
            GUI.Label(new Rect(der.x, der.y, der.width, altoCabecera), "LAS RECETAS", _estiloCabeceraFamilia);

            // Progreso DE ESTA COLECCIÓN (no cuenta en el N/M del retículo,
            // mismo criterio que _clasicos -- ver TotalFiguritas).
            int hechas = 0;
            for (int i = 0; i < _cruces.Length; i++) if (knowledge.EsDescubierto(_cruces[i])) hechas++;
            if (_progresoMezclasCacheN != hechas + 1)
            {
                _progresoMezclasCacheN = hechas + 1;
                _progresoMezclasTexto = hechas + " / " + _cruces.Length;
            }
            float yProg = izq.y + altoCabecera + UiStyles.S(2f);
            GUI.Label(new Rect(izq.x, yProg, izq.width, _estiloProgresoFamilia.lineHeight), _progresoMezclasTexto, _estiloProgresoFamilia);

            float yFilete = yProg + _estiloProgresoFamilia.lineHeight + UiStyles.S(7f);
            UiStyles.FileteRombo(izq.x + izq.width * 0.5f, yFilete, izq.width * 0.86f, UiStyles.Laton);
            UiStyles.FileteRombo(der.x + der.width * 0.5f, yFilete, der.width * 0.86f, UiStyles.Laton);

            // ---- HOJA IZQUIERDA: las 6 figuritas, 2 columnas x 3 filas. ----
            var rejilla = new Rect(izq.x, yFilete + UiStyles.S(10f), izq.width, izq.yMax - yFilete - UiStyles.S(10f));
            float anchoCelda = rejilla.width / 2f;
            float altoCelda = rejilla.height / 3f;
            for (int i = 0; i < _cruces.Length; i++)
            {
                int col = i % 2, fila = i / 2;
                var celda = new Rect(rejilla.x + anchoCelda * col, rejilla.y + altoCelda * fila, anchoCelda, altoCelda);
                DibujarVitrina(celda, _cruces[i], null, sim, knowledge);
            }

            // ---- HOJA DERECHA: las recetas como preguntas, una fila por cruce. ----
            float yNota = yFilete + UiStyles.S(8f);
            GUI.Label(new Rect(der.x, yNota, der.width, _estiloNotaArbol.lineHeight), NotaMezclas, _estiloNotaArbol);

            float yFilas = yNota + _estiloNotaArbol.lineHeight + UiStyles.S(12f);
            float altoFilaTexto = der.yMax - yFilas - _estiloProgresoTotal.lineHeight; // deja sitio al "ÁLBUM N/M" de la esquina.
            float altoPorFila = Mathf.Max(UiStyles.S(20f), altoFilaTexto / _cruces.Length);

            for (int i = 0; i < _cruces.Length; i++)
            {
                bool revelado = knowledge.EsDescubierto(_cruces[i]);
                if (_mezclaCacheRevelado[i] != revelado || _mezclaTextoCache[i] == null)
                {
                    _mezclaCacheRevelado[i] = revelado;
                    _mezclaTextoCache[i] = revelado
                        ? _cruceIngredientes[i] + " → " + EtiquetaDe(_cruces[i], knowledge)
                        : _crucePreguntas[i];
                }
                var fila = new Rect(der.x, yFilas + altoPorFila * i, der.width, altoPorFila);
                GUI.Label(fila, _mezclaTextoCache[i], revelado ? _estiloRecetaRevelada : _estiloRecetaPregunta);
            }
        }

        private const string NotaMezclas = "el crisol también mezcla: dos cosas en la cubeta pueden ser una tercera";
        private static int _progresoMezclasCacheN = -1;
        private static string _progresoMezclasTexto = "";
        // Cero allocs por frame: el texto de cada fila solo se reconstruye cuando el estado
        // revelado/pregunta de ESE cruce cambia (mismo patrón que _progresoFamTexto).
        private static readonly bool[] _mezclaCacheRevelado = new bool[_cruces.Length];
        private static readonly string[] _mezclaTextoCache = new string[_cruces.Length];

        // -------------------------------------------------------------
        // PIEZAS COMPARTIDAS
        // -------------------------------------------------------------

        /// <summary>
        /// UNA MINI-VITRINA: la muestra (firma visual real si ya se descubrió, silueta gris
        /// con "?" si no) dentro de un marco de latón fino, con el rótulo del estado encima
        /// y el nombre debajo. `rotuloEstado` puede ser null (los clásicos no son estados de
        /// ninguna familia). El marco se dibuja a mano y no con UiStyles.MarcoLaton porque
        /// las cantoneras de ese (S(14)) se comerían una caja de 60 px: aquí el gesto
        /// correcto es un filete fino de dos tonos, no una escuadra.
        /// </summary>
        private static void DibujarVitrina(Rect celda, byte matId, string rotuloEstado, AlkahestSim sim, SubstanceKnowledge knowledge)
        {
            bool revelado = rotuloEstado == null || knowledge.EsDescubierto(matId); // rotuloEstado==null solo lo pasan los clásicos, que nunca se ocultan (regla 13).

            float altoRotulo = rotuloEstado != null ? _estiloRotuloEstado.lineHeight : 0f;
            float altoNombre = _estiloNombreFigurita.lineHeight;
            float margen = UiStyles.S(4f);
            // margen * 5 y no * 3: los dos márgenes de más son el AIRE ENTRE CELDAS. Sin
            // ellos la vitrina crecía hasta tocar el rótulo de la de abajo y la rejilla se
            // leía como una tabla apretada -- justo lo contrario del encargo ("que respire").
            float ladoDisponible = celda.height - altoRotulo - altoNombre - margen * 5f;
            float lado = Mathf.Clamp(Mathf.Min(celda.width * 0.62f, ladoDisponible), UiStyles.S(22f), UiStyles.S(74f));

            float cx = celda.x + celda.width * 0.5f;
            float y = celda.y;

            if (rotuloEstado != null)
            {
                GUI.Label(new Rect(celda.x, y, celda.width, altoRotulo), rotuloEstado, _estiloRotuloEstado);
                y += altoRotulo + margen;
            }

            var box = new Rect(cx - lado * 0.5f, y, lado, lado);
            DibujarFigurita(box, matId, sim, knowledge, siempreRevelado: rotuloEstado == null);

            y = box.yMax + margen;
            string etiqueta = revelado ? EtiquetaDe(matId, knowledge) : "—";
            GUI.Label(new Rect(celda.x, y, celda.width, altoNombre), etiqueta, _estiloNombreFigurita);
        }

        /// <summary>Una figurita: muestra con su firma visual + marco de latón si está revelada, silueta gris + "?" si no.</summary>
        private static void DibujarFigurita(Rect box, byte matId, AlkahestSim sim, SubstanceKnowledge knowledge, bool siempreRevelado)
        {
            bool revelado = siempreRevelado || knowledge.EsDescubierto(matId);
            float borde = Mathf.Max(1f, UiStyles.S(1.5f));

            if (revelado)
            {
                UiStyles.Rellenar(box, new Color(0f, 0f, 0f, 0.5f));
                var dentro = new Rect(box.x + borde, box.y + borde, box.width - borde * 2f, box.height - borde * 2f);
                var tex = ObtenerMuestra(matId, sim);
                if (tex != null) GUI.DrawTexture(dentro, tex);
                else UiStyles.Rellenar(dentro, sim.Universe.Get(matId).baseColor);
                // Filete de latón de dos tonos: fuerte arriba/izquierda, viejo abajo/derecha
                // (la luz del taller cae desde arriba, como en PanelRito).
                UiStyles.Rellenar(new Rect(box.x, box.y, box.width, borde), UiStyles.Laton);
                UiStyles.Rellenar(new Rect(box.x, box.y, borde, box.height), UiStyles.Laton);
                UiStyles.Rellenar(new Rect(box.x, box.yMax - borde, box.width, borde), UiStyles.LatonOscuro);
                UiStyles.Rellenar(new Rect(box.xMax - borde, box.y, borde, box.height), UiStyles.LatonOscuro);
            }
            else
            {
                UiStyles.Rellenar(box, _grisApagado);
                UiStyles.Rellenar(new Rect(box.x, box.y, box.width, borde), _grisBorde);
                UiStyles.Rellenar(new Rect(box.x, box.yMax - borde, box.width, borde), _grisBorde);
                UiStyles.Rellenar(new Rect(box.x, box.y, borde, box.height), _grisBorde);
                UiStyles.Rellenar(new Rect(box.xMax - borde, box.y, borde, box.height), _grisBorde);
                GUI.Label(box, "?", _estiloSigno);
            }

            // Repaso de la reseña al pasar el cursor: la única forma de releer la trivia de
            // una figurita YA anotada sin tener que esperar a que vuelva a "descubrirse" (la
            // ficha-vitrina solo aparece la primera vez). Barato: un Contains() y, si toca, el
            // mismo Globo que ya usa el resto del HUD (UiStyles.Globo no asigna nada nuevo).
            if (revelado && Event.current != null && Event.current.type == EventType.Repaint
                && box.Contains(Event.current.mousePosition))
            {
                string resena = ResenaDe(matId);
                if (!string.IsNullOrEmpty(resena) && resena != SinResena)
                {
                    UiStyles.Globo(Event.current.mousePosition + new Vector2(0f, -UiStyles.S(26f)), resena, UiStyles.Oro);
                }
            }
        }

        // ===================================================================
        // LAS MUESTRAS CON FIRMA VISUAL REAL
        // ===================================================================
        // Mismo mecanismo EXACTO que Game/NamingUi.cs y Game/FlaskHud.cs:
        // FirmaVisualFabrica genera los fotogramas UNA vez por material y aquí
        // solo se elige cuál toca mostrar. Nunca se genera nada dentro de
        // OnGUI más allá de la primera vez que se ve cada sustancia -- que es
        // justo lo que permite que el swatch deje de ser un color plano sin
        // romper la regla de oro del proyecto ("cero allocs POR FRAME", no
        // "cero allocs nunca").
        //
        // La caché es ESTÁTICA porque el dibujo del álbum también lo es (lo
        // llama JournalHud sin instancia). Se libera en OnDestroy de este
        // componente, que es el mismo momento en que NamingUi libera la suya:
        // DayCycle.RestartRun recrea la escena entera y las Texture2D creadas
        // por código no se recogen solas.
        // ===================================================================
        private const int MuestraLienzo = 40;
        private static readonly Texture2D[][] _muestras = new Texture2D[MaterialId.Count][];
        private static bool[] _esBordeMuestra;

        private static Texture2D ObtenerMuestra(byte matId, AlkahestSim sim)
        {
            if (matId >= _muestras.Length || sim == null || sim.Universe == null) return null;
            var texturas = _muestras[matId];
            if (texturas == null)
            {
                if (_esBordeMuestra == null)
                {
                    const int banda = 4; // ~10% del lienzo, misma proporción que NamingUi (4/44) y FlaskHud (3/28).
                    _esBordeMuestra = new bool[MuestraLienzo * MuestraLienzo];
                    for (int yy = 0; yy < MuestraLienzo; yy++)
                        for (int xx = 0; xx < MuestraLienzo; xx++)
                        {
                            int d = Mathf.Min(Mathf.Min(xx, MuestraLienzo - 1 - xx), Mathf.Min(yy, MuestraLienzo - 1 - yy));
                            _esBordeMuestra[yy * MuestraLienzo + xx] = d < banda;
                        }
                }

                var def = sim.Universe.Get(matId);
                int frames = def.ritmoAnim > 0 ? FirmaVisualFabrica.AnimFrames : 1;
                texturas = new Texture2D[frames];
                for (int f = 0; f < frames; f++)
                {
                    var px = FirmaVisualFabrica.GenerarPixeles(MuestraLienzo, MuestraLienzo, def, f, null, _esBordeMuestra, sobreMundo: false);
                    var tex = new Texture2D(MuestraLienzo, MuestraLienzo, TextureFormat.RGBA32, false, false)
                    {
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp,
                        name = "MuestraAlbum_" + def.devName + "_" + f,
                    };
                    tex.SetPixels32(px);
                    tex.Apply(false, true);
                    texturas[f] = tex;
                }
                _muestras[matId] = texturas;
            }

            if (texturas.Length == 0) return null;
            int idx = Mathf.FloorToInt(Time.time * FirmaVisualFabrica.AnimFps) % texturas.Length;
            return texturas[idx];
        }

        /// <summary>Misma disciplina que NamingUi/FlaskHud/JournalHud: las Texture2D creadas por código no se liberan solas al recargar la escena (DayCycle.RestartRun recrea este componente).</summary>
        private void OnDestroy()
        {
            for (int m = 0; m < _muestras.Length; m++)
            {
                var t = _muestras[m];
                if (t == null) continue;
                for (int f = 0; f < t.Length; f++) if (t[f] != null) Destroy(t[f]);
                _muestras[m] = null;
            }
        }

        // -----------------------------------------------------------------
        // Colores / estilos propios (cacheados; solo la parte compartida por
        // DibujarPaginaDoble vive en campos static -- la pantalla completa y
        // la ficha, que son de instancia y no las llama JournalHud, usan sus
        // propios estilos de instancia igual que hace Game/JournalHud.cs).
        // -----------------------------------------------------------------
        private static readonly Color _grisApagado = new Color(0.16f, 0.15f, 0.17f, 1f);
        private static readonly Color _grisBorde = new Color(0.32f, 0.30f, 0.33f, 0.8f);

        private static GUIStyle _estiloCabeceraFamilia, _estiloProgresoFamilia, _estiloProgresoTotal,
                                _estiloVerbo, _estiloNombreFigurita, _estiloRotuloEstado, _estiloSigno, _estiloNotaArbol,
                                _estiloRecetaPregunta, _estiloRecetaRevelada;
        private static int _alturaEstilosEstaticos = -1;

        private static void PrepararEstilosPropios()
        {
            if (Event.current == null) return;
            if (_alturaEstilosEstaticos == Screen.height && _estiloCabeceraFamilia != null) return;
            _alturaEstilosEstaticos = Screen.height;

            _estiloCabeceraFamilia = Nuevo(UiStyles.S(19f), FontStyle.Normal, TextAnchor.UpperCenter, UiStyles.Oro, false, UiStyles.FuenteTitulos);
            _estiloProgresoFamilia = Nuevo(UiStyles.S(12f), FontStyle.Normal, TextAnchor.UpperCenter, UiStyles.OroTenue, false, UiStyles.FuenteTitulos);
            _estiloProgresoTotal = Nuevo(UiStyles.S(11f), FontStyle.Normal, TextAnchor.LowerRight, UiStyles.OroTenue, false, UiStyles.FuenteTitulos);
            _estiloVerbo = Nuevo(UiStyles.S(12f), FontStyle.Italic, TextAnchor.MiddleCenter, UiStyles.Texto, false);
            _estiloNombreFigurita = Nuevo(UiStyles.S(12f), FontStyle.Normal, TextAnchor.UpperCenter, UiStyles.Texto, false);
            _estiloRotuloEstado = Nuevo(UiStyles.S(10f), FontStyle.Normal, TextAnchor.UpperCenter, UiStyles.TextoTenue, false, UiStyles.FuenteTitulos);
            _estiloSigno = Nuevo(UiStyles.S(18f), FontStyle.Bold, TextAnchor.MiddleCenter, UiStyles.TextoTenue, false);
            _estiloNotaArbol = Nuevo(UiStyles.S(11f), FontStyle.Italic, TextAnchor.UpperCenter, UiStyles.TextoTenue, false);
            // (playtest 47, ENCARGO C) Las filas de "LAS RECETAS": pregunta tenue en cursiva
            // mientras siga sin descubrir, respuesta clara y derecha una vez revelada -- mismo
            // contraste tenue/claro que el resto del álbum usa para "sin descubrir"/"descubierto".
            _estiloRecetaPregunta = Nuevo(UiStyles.S(13f), FontStyle.Italic, TextAnchor.UpperLeft, UiStyles.TextoTenue, false);
            _estiloRecetaRevelada = Nuevo(UiStyles.S(13f), FontStyle.Normal, TextAnchor.UpperLeft, UiStyles.Texto, false);
        }

        /// <summary>
        /// Todos los estilos del álbum RECORTAN (clipping = Clip) en vez de desbordar. Es
        /// la salvaguarda que hace estructuralmente imposible el solapamiento de rótulos
        /// que reportó Cesar: con Overflow (lo que había antes) un texto más ancho que su
        /// rect se imprime igual, invadiendo al vecino, y ni la maqueta más generosa te
        /// protege de un nombre largo o de una resolución rara.
        /// </summary>
        private static GUIStyle Nuevo(float tamPx, FontStyle fuente, TextAnchor anclaje, Color color, bool ajustar, Font tipografia = null)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.Max(8, Mathf.RoundToInt(tamPx)),
                fontStyle = fuente,
                alignment = anclaje,
                wordWrap = ajustar,
                richText = false,
                clipping = TextClipping.Clip,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };
            if (tipografia != null) s.font = tipografia;
            s.normal.textColor = color;
            return s;
        }

        // Estilos de INSTANCIA para la pantalla completa / la ficha (no los necesita el
        // dibujo estático que reutiliza JournalHud) -- mismo patrón, altura propia.
        private GUIStyle _estiloTitulo, _estiloAyuda, _estiloNumeral, _estiloBotonPagina;
        private int _alturaEstilosInstancia = -1;

        private void PrepararEstilosInstancia()
        {
            if (Event.current == null) return;
            if (_alturaEstilosInstancia == Screen.height && _estiloTitulo != null) return;
            _alturaEstilosInstancia = Screen.height;

            _estiloTitulo = Nuevo(UiStyles.S(24f), FontStyle.Normal, TextAnchor.UpperCenter, UiStyles.Oro, false, UiStyles.FuenteTitulos);
            _estiloAyuda = Nuevo(UiStyles.S(12f), FontStyle.Normal, TextAnchor.UpperCenter, UiStyles.TextoTenue, false);
            _estiloNumeral = Nuevo(UiStyles.S(11f), FontStyle.Bold, TextAnchor.MiddleCenter, UiStyles.TintaFuerte, false);
            _estiloBotonPagina = Nuevo(UiStyles.S(13f), FontStyle.Bold, TextAnchor.MiddleCenter, UiStyles.TextoTenue, false);
        }
    }
}
