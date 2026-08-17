using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy] Onboarding suave: pistas rotatorias arriba-centro, bajo el
    /// reloj de la jornada. H las oculta/muestra (no "para siempre": es un
    /// interruptor, ver <see cref="_oculto"/>); N salta ya a la siguiente sin
    /// esperar el reloj (playtest 26, ver <see cref="_offsetManual"/>); y todo lo
    /// que ya se mostró se puede releer para siempre en la sección CONSEJOS del
    /// diario (Game/JournalHud.cs), vía <see cref="PistasMostradas"/>.
    ///
    /// PISTAS POR JORNADA (playtest 4). Antes eran 7 frases que salían una sola
    /// vez, durante los ~2.5 primeros minutos de la partida entera: para cuando
    /// el jugador llegaba a la jornada 2 —donde aparecen el azoth, el cultivo y
    /// la cristalización, que son las mecánicas DIFÍCILES— ya no había ninguna
    /// ayuda, y nada le decía que el fuego no se compra sino que se fabrica.
    /// Ahora cada jornada reactiva las pistas con el contenido de ESE día
    /// (<see cref="ReiniciarParaJornada"/>, llamado por Game/DayCycle.cs).
    ///
    /// (fix playtest 10) "LAS INDICACIONES SON SÚPER LARGAS, YA ME COSTÓ
    /// RECORDAR CÓMO HACER LA SEGUNDA PARTE" -- las pistas de la jornada 2/3
    /// metían un PROCEDIMIENTO ENTERO (varios pasos, con sitios y verbos
    /// distintos) en una sola frase larga, así que para cuando el jugador
    /// llegaba al tercer paso ya no recordaba el primero. REGLA NUEVA: una
    /// pista = una línea ejecutable = un paso, un sitio, un verbo. Un
    /// procedimiento de 3 pasos son ahora 3 pistas cortas SEGUIDAS, no un
    /// párrafo -- el detalle largo (por qué funciona, qué pasa si te
    /// equivocas) vive en el diario, no aquí. Como hay más pistas por jornada
    /// que antes pero cada una se lee en un vistazo, el tiempo por pista
    /// (<see cref="SegundosPorPistaJornada1"/>/<see cref="SegundosPorPistaOtras"/>)
    /// ya NO se deriva de repartir una duración fija entre "las pistas que
    /// haya" (eso encogía el tiempo de lectura cada vez que se añadía una
    /// pista más) -- es al revés: la duración total sale de cuántas pistas
    /// hay, cada una con su propio tiempo de lectura fijo y generoso.
    ///
    /// (fix playtest 10) CIRCULARIDAD: Azoth, Vivium, la semilla de cristal y
    /// el propio Cristal son ahora "lo innominado" (ver la tabla de
    /// clasificación en Game/SubstanceKnowledge.cs) -- decir su identidad
    /// aquí mientras el HUD enseña "???" para lo mismo sería contradecir al
    /// propio juego. Estas pistas los describen por ORIGEN/procedimiento
    /// ("el líquido nuevo del grifo alto", "la semilla de la bandeja fría"),
    /// nunca por nombre. Lo mundano (agua, aceite, nutriente) y la
    /// arquitectura del taller (cuba derecha, bandeja fría, placa, Tolva) se
    /// siguen nombrando sin problema: ahí no hay "???" posible.
    ///
    /// El alto del panel se MIDE con word-wrap real (CalcHeight): ninguna frase
    /// se corta, por larga que sea, y el ancho se acota para no pisar el panel
    /// del frasco (izquierda) ni el de encargos (derecha). (playtest 26) La fila
    /// de progreso "consejo N/M" entra en esa misma suma de alturas -- nunca es
    /// una fila "gratis" que el panel no contaba.
    /// </summary>
    public sealed class HintSystem : MonoBehaviour
    {
        /// <summary>
        /// Segundos de lectura por pista. (fix playtest 26, CONTRATO_LEGIBILIDAD.md §3.1)
        /// Cesar, literal: "los consejos están pasando muy rápido y aturde". Antes 9s/8s
        /// (jornada1/otras) -- la distinción venía de que la jornada 1 "va más despacio,
        /// primera vez con los controles", pero 8-9s ya era insuficiente incluso para las
        /// jornadas veteranas, así que el problema no era la DIFERENCIA entre las dos, era
        /// que las dos se quedaban cortas. Unificadas a 12s: se dejan las dos constantes
        /// separadas (no una sola) porque el día que haga falta volver a diferenciarlas el
        /// sitio ya existe, documentado, en vez de tener que reinventarlo.
        /// </summary>
        private const float SegundosPorPistaJornada1 = 12f;
        private const float SegundosPorPistaOtras = 12f;

        // (playtest 25, CONTRATO_PERSISTE.md §6.5) REEMPLAZADAS ENTERAS: la
        // dirección "LO QUE PERSISTE" ya no abre en el taller clásico (grifos
        // de agua/aceite, placa, Tolva de siempre) sino en el laboratorio del
        // cuarto íntimo (limo, crisol, prensa, banco de chispa) -- las 12
        // líneas viejas describían un guion que este modo ni siquiera
        // recorre (no hay "aceite que arde junto a la placa" en la primera
        // jornada de este pivot). Las 10 líneas de aquí son LITERALES del
        // contrato (verbatim, no parafraseadas): un paso ejecutable cada una,
        // en el orden en que el jugador puede de verdad encontrárselas.
        // PistasJornada2/PistasJornada3 NO se tocan: siguen sirviendo al modo
        // CLÁSICO (EnterDayIntro, día 2/3), que este encargo no toca -- ver
        // DayCycle.EnterCuartoIntimoSilencioso, que solo llama a
        // ReiniciarParaJornada(1), nunca a día 2 ni 3 en este modo.
        private static readonly string[] PistasJornada1 =
        {
            // (playtest 27) Guion reescrito para EL CRISOL POR HORNADAS: el
            // guion del 26 describía el crisol continuo ("hierve solo", el
            // recocido por dejarlo morir) que ya no existe. Cada línea sigue
            // siendo UN paso ejecutable, ahora del modelo real: cargar,
            // encender (E), esperar la hornada, RECOGER, volver a pasar.
            "El caño turbio gotea LIMO PRIMORDIAL: todo lo que existe aquí desciende de él.",
            "Vierte limo en la boca del crisol y enciéndelo con E: una hornada.",
            "Cada hornada hace UNA sola cosa; recoge el resultado antes de la siguiente.",
            "Con su fuego bajo, el crisol solo extrae la arena más dócil del limo.",
            "Tuesta un polvo sin fundirlo: algunos se vuelven combustible.",
            "Con combustible en el brasero, el fuego sube: el limo suelta OTRAS arenas.",
            "Lo fundido, vertido fuera, se templa: duro pero frágil.",
            "Lo fundido, recocido en otra hornada, sale dócil a la prensa.",
            "La prensa compacta lo dócil y revienta lo frágil.",
            "La lámpara del banco delata lo que el ojo no ve.",
            "La columna de vidrio no transforma nada: deja caer y OBSERVA.",
            "Pulsa T para bautizar; el libro (J) guarda tus procedimientos.",
        };

        // (fix playtest 9, recortadas playtest 10) El jugador reportó llevar horas sin
        // conseguir MULTIPLICAR cristal ni vivium a pesar de "hacer combinaciones": el
        // hecho que de verdad desbloquea el nivel es que la muestra del Maestro es una
        // SEMILLA/CATALIZADOR que NO SE GASTA. Cada línea de aquí abajo es un paso
        // ejecutable con la ubicación real del taller (Sim/SimLevelBuilder es la única
        // fuente de verdad de esas ubicaciones), nunca un párrafo con varios pasos a la vez.
        private static readonly string[] PistasJornada2 =
        {
            "Las tres muestras del Maestro son SEMILLAS: no se gastan.",
            "El líquido nuevo del grifo más alto es infinito: úsalo sin miedo.",
            "Enciende la piedra gélida (E) hasta que ponga HELANDO.",
            "Vierte el líquido del grifo alto sobre la semilla de la bandeja fría.",
            "Esa semilla no se gasta: repite el chorro cuantas veces quieras.",
            "En la CUBA DERECHA, pon la placa en TEMPLADA (E).",
            "Vierte nutriente junto al retoño de la cuba para que crezca.",
            "El fuego no sale de ningún grifo: nace del calor sobre aceite.",
            "Congela agua en la bandeja fría y viértela helada en la Tolva.",
        };

        private static readonly string[] PistasJornada3 =
        {
            "Llena una redoma del estante con clic derecho.",
            "Recupera lo guardado en una redoma con clic izquierdo.",
            "Deja el criadero de la cuba derecha trabajando solo.",
            "Riega TODO el frente de piedra de la bandeja fría, no solo un punto.",
            "Bautiza (T) lo que aún no tenga nombre.",
            "Los encargos usarán el nombre que le pongas a cada cosa.",
        };

        private string[] _pistas = PistasJornada1;
        private float _segundosPista = SegundosPorPistaJornada1;
        private float _duracion;
        private bool[] _registrada = new bool[0];

        private float _playSeconds;
        private bool _everUnlocked;
        private bool _oculto;

        /// <summary>
        /// (fix playtest 26, CONTRATO_LEGIBILIDAD.md §3.2) "Saltar: tecla N = siguiente
        /// consejo YA... desapareció lo de poder saltar a otro". El índice mostrado NO se
        /// SUSTITUYE por uno manual (eso lo dejaría "congelado" ahí, sin volver a avanzar
        /// solo, contradiciendo el punto 3 -- "el reloj sigue corriendo" incluso al
        /// ocultar/mostrar): el offset se SUMA al índice por tiempo en OnGUI (ver el cálculo
        /// de `i`). Efecto práctico: cada N adelanta el reloj efectivo en un tramo entero de
        /// <see cref="_segundosPista"/> -- de ahí en adelante el jugador sigue leyendo al
        /// mismo ritmo de siempre, solo que _segundosPista más adelantado que si no hubiera
        /// pulsado nada; el offset NO se "gasta" ni se "recupera" con el paso del tiempo, es
        /// un desplazamiento permanente hasta que ReiniciarParaJornada lo resetea o hasta
        /// que `i` toca el último índice y el clamp absorbe cualquier offset de sobra.
        /// Clampeado (en el propio Update, y otra vez en OnGUI por seguridad) al último
        /// índice real de la jornada: pulsar N en el último consejo no hace nada, no lo saca
        /// fuera de rango.
        /// Se reinicia en ReiniciarParaJornada (cada jornada trae su propia lista de pistas,
        /// un offset de la jornada anterior no significa nada en la nueva).
        /// </summary>
        private int _offsetManual;

        // ---------------------------------------------------------------------------------
        // (fix playtest 10) API ESTÁTICA DE SOLO LECTURA para que el diario (Game/
        // JournalHud.cs, en reescritura en paralelo esta misma ronda -- ver
        // docs/HANDOFF.md) pueda archivar qué pistas ya se le mostraron al jugador, sin
        // que este archivo tenga que saber nada del diario. Estática porque JournalHud
        // no recibe (ni necesita recibir) una referencia a esta instancia por Init;
        // así puede consultarla sin cablear una dependencia nueva en
        // AlkahestGameBootstrap.cs (fuera del alcance de esta ronda).
        //
        // Contrato: orden de primera aparición, sin duplicados, nunca null. Se reinicia
        // en Awake() (una partida nueva = pistas nuevas). Nadie fuera de esta clase debe
        // mutarla -- por eso se expone como IReadOnlyList, no como List directamente.
        //
        // (playtest 26, CONTRATO_LEGIBILIDAD.md §3.5) YA TIENE CONSUMIDOR: la sección
        // CONSEJOS de Game/JournalHud.cs (ver su ConstruirEntradasConsejos) lee esta lista
        // directamente, sin ninguna API nueva expuesta aquí -- el TODO que dejó el
        // playtest 10 quedaba resuelto con lo que ya existía.
        // ---------------------------------------------------------------------------------
        private static readonly List<string> _pistasMostradas = new List<string>();

        /// <summary>Pistas ya mostradas al jugador esta partida, en el orden en que aparecieron por primera vez. Consumida por la sección CONSEJOS de Game/JournalHud.cs (ver nota de arriba).</summary>
        public static IReadOnlyList<string> PistasMostradas => _pistasMostradas;

        private void Awake()
        {
            _pistasMostradas.Clear(); // partida nueva (o reinicio sin recarga de dominio): pistas nuevas.
            ReiniciarParaJornada(1);
        }

        /// <summary>
        /// Reactiva las pistas al empezar una jornada, con el guion de ese día.
        /// Si el jugador pulsó H, respeta su decisión y no vuelve a aparecer.
        /// </summary>
        public void ReiniciarParaJornada(int dia)
        {
            _pistas = dia >= 3 ? PistasJornada3 : (dia == 2 ? PistasJornada2 : PistasJornada1);
            _segundosPista = dia == 1 ? SegundosPorPistaJornada1 : SegundosPorPistaOtras;
            // (fix playtest 10) La duración total SALE del número de pistas x su tiempo de
            // lectura fijo -- ya no al revés (una duración fija repartida entre "las que
            // haya", que encogía el tiempo de lectura cada vez que se añadía una pista).
            _duracion = _segundosPista * _pistas.Length;
            _registrada = new bool[_pistas.Length];
            _playSeconds = 0f;
            _offsetManual = 0; // pista nueva: el salto manual de la jornada anterior no aplica aquí.
            _progresoIndiceCache = -1; // fuerza reconstruir el string "consejo N/M" con el M de la jornada nueva.
        }

        private void Update()
        {
            if (!DayCycle.InputLocked)
            {
                _everUnlocked = true;
                _playSeconds += Time.deltaTime;

                var kb = Keyboard.current;
                // (fix playtest 10) H es un atajo de una tecla como cualquier otro del
                // proyecto: debe respetar UiStyles.EscribiendoTexto (regla nueva, ver su
                // doc-comment) para que escribir un nombre que contenga "h" no oculte las
                // pistas sin querer -- el mismo bug que ya se arregló para M (mute) y T
                // (Game/NamingUi.cs), aplicado aquí.
                // (playtest 26) NO se le añade además el guard de JournalHud.Abierto: H es
                // un interruptor de PREFERENCIA de HUD que no toca el mundo, exactamente el
                // mismo caso que ya documenta Game/OrdersHud.cs para su tecla O -- el propio
                // libro ya tapa la placa visualmente (GUI.depth) mientras está abierto (ver
                // el guard nuevo en OnGUI, punto 6 del contrato), así que no hace falta
                // impedir el toggle en sí, solo su dibujado.
                if (kb != null && kb.hKey.wasPressedThisFrame && !UiStyles.EscribiendoTexto) _oculto = !_oculto;

                // (playtest 26, CONTRATO_LEGIBILIDAD.md §3.2) N = saltar al siguiente
                // consejo YA. SÍ lleva el guard de JournalHud.Abierto además de
                // EscribiendoTexto -- a diferencia de H, la regla 12 de CLAUDE.md lista N
                // explícitamente entre los atajos del MUNDO que lo necesitan (comparte tecla
                // con Dev/DevPalette.StepOnce, que SÍ es una acción de mundo).
                //
                // OJO, CONFLICTO CONOCIDO FUERA DE ESTE ENCARGO (archivos M/H disjuntos, ver
                // contrato §4): Dev/DevPalette.cs también escucha N (avanza la sim un tick)
                // en builds de dev/editor, sin comprobar si la paleta F3 está abierta. Con
                // BuildOptions.Development activo (regla 14 de CLAUDE.md, build actual) las
                // dos N conviven: saltar de consejo Y avanzar un tick de la sim ocurren en la
                // misma pulsación. No se puede arreglar aquí (DevPalette.cs no es un archivo
                // de este encargo) -- queda anotado para quien reparta la próxima ronda.
                if (kb != null && kb.nKey.wasPressedThisFrame && !UiStyles.EscribiendoTexto && !JournalHud.Abierto)
                {
                    _offsetManual = Mathf.Min(_offsetManual + 1, Mathf.Max(0, _pistas.Length - 1));
                }
            }
        }

        private void OnGUI()
        {
            // (playtest 26, CONTRATO_LEGIBILIDAD.md §3.6) "La placa de consejos se OCULTA
            // mientras el diario está abierto -- dos capas de texto a la vez aturden": el
            // libro ya se dibuja encima de todo (GUI.depth=-1000 en JournalHud), así que sin
            // este guard la placa quedaría invisible DETRÁS del libro pero seguiría
            // existiendo por debajo, sin aportar nada -- este return además evita el trabajo
            // de medir/dibujarla en vano.
            if (!_everUnlocked || DayCycle.InputLocked || DayCycle.HudSilenciado || _oculto || JournalHud.Abierto) return; // (playtest 21) HudSilenciado, hermano de InputLocked.
            if (_playSeconds >= _duracion) return;

            UiStyles.Preparar();

            // (playtest 26) Índice por tiempo (como siempre) + offset manual de N (ver doc
            // de _offsetManual): el offset se SUMA, nunca sustituye, y el resultado se
            // clampea otra vez aquí por si _pistas cambiara de tamaño entre Update y OnGUI
            // (no debería, pero el clamp es gratis y defensivo).
            int baseIndice = Mathf.Min((int)(_playSeconds / _segundosPista), _pistas.Length - 1);
            int i = Mathf.Clamp(baseIndice + _offsetManual, 0, _pistas.Length - 1);
            string texto = _pistas[i];

            // Archivo para el diario (ver PistasMostradas arriba): una sola vez por
            // índice, no un Contains() por frame -- barato incluso a 60+ FPS.
            // (playtest 26) CLAVE: esto archiva SOLO el índice `i` que de verdad se DIBUJA
            // esta vez -- si N (o el tiempo transcurrido oculto/con el diario abierto) hace
            // que la placa salte de, p.ej., el consejo 2 al 5 sin pasar por 3 y 4 visiblemente,
            // esos dos NUNCA se marcan _registrada ni entran en _pistasMostradas: no se
            // destripan en el diario consejos que el jugador no llegó a leer.
            if (i >= 0 && i < _registrada.Length && !_registrada[i])
            {
                _registrada[i] = true;
                if (!_pistasMostradas.Contains(texto)) _pistasMostradas.Add(texto);
            }

            // (playtest 26, CONTRATO_LEGIBILIDAD.md §3.4) "consejo 3/10" pequeño y tenue en
            // la esquina de la placa: string cacheado, solo se reconstruye cuando `i` cambia
            // (cero allocs por frame mientras el jugador lee el mismo consejo).
            if (i != _progresoIndiceCache)
            {
                _progresoIndiceCache = i;
                _progresoTexto = "consejo " + (i + 1) + "/" + _pistas.Length;
            }
            var estiloProgreso = EstiloProgreso();

            float pad = UiStyles.S(9f);
            float acento = UiStyles.S(3f);
            // Ancho máximo 560 px de diseño, pero siempre dejando libres los ~700 px
            // de diseño que ocupan el panel del frasco y el de encargos.
            float ancho = Mathf.Clamp(Screen.width - UiStyles.S(700f), UiStyles.S(300f), UiStyles.S(560f));
            float interior = ancho - pad * 2f - acento;

            // (playtest 26) La placa MIDE su alto con CalcHeight (vía UiStyles.Alto) para
            // cada fila que dibuja, incluida la nueva del progreso -- así el panel nunca
            // desborda el texto que contiene, ni al revés (nunca sobra hueco muerto).
            float altoProgreso = estiloProgreso.lineHeight;
            float altoTexto = UiStyles.Alto(UiStyles.CuerpoCentrado, texto, interior);
            float altoPie = UiStyles.TenueCentrado.lineHeight;
            float alto = pad + altoProgreso + UiStyles.S(2f) + altoTexto + UiStyles.S(3f) + altoPie + pad;

            // Justo debajo del reloj de la jornada (ver DayCycle.DrawPlayingHud).
            var panel = new Rect((Screen.width - ancho) * 0.5f, UiStyles.S(54f), ancho, alto);
            UiStyles.Panel(panel);
            UiStyles.Rellenar(new Rect(panel.x, panel.y, acento, panel.height), UiStyles.Oro);

            float xTexto = panel.x + acento + pad;
            float y = panel.y + pad;

            // (playtest 26) Esquina superior derecha del interior de la placa: ancla
            // UpperRight sobre el ancho completo, no una segunda columna estrecha -- así no
            // hay que calcular dónde empieza "la esquina" aparte del resto de la maqueta.
            GUI.Label(new Rect(xTexto, y, interior, altoProgreso), _progresoTexto, estiloProgreso);
            y += altoProgreso + UiStyles.S(2f);

            GUI.Label(new Rect(xTexto, y, interior, altoTexto), texto, UiStyles.CuerpoCentrado);

            GUI.Label(new Rect(xTexto, panel.yMax - pad - altoPie, interior, altoPie),
                "H — ocultar · N — siguiente", UiStyles.TenueCentrado);
        }

        // -----------------------------------------------------------------
        // (playtest 26, CONTRATO_LEGIBILIDAD.md §3.4) Estilo del "consejo N/M": propio de
        // esta clase (no vive en UiStyles, que no tiene ningún nivel tenue anclado a la
        // derecha) pero cacheado con el mismo criterio que UiStyles.Preparar -- se
        // reconstruye SOLO si cambia la escala del HUD, nunca por frame.
        // -----------------------------------------------------------------
        private GUIStyle _estiloProgreso;
        private float _escalaEstiloProgreso = -1f;
        private string _progresoTexto = "";
        private int _progresoIndiceCache = -1;

        private GUIStyle EstiloProgreso()
        {
            if (_estiloProgreso == null || _escalaEstiloProgreso != UiStyles.Escala)
            {
                _escalaEstiloProgreso = UiStyles.Escala;
                // Clon de TenueCentrado (mismo color/tenue que pide el contrato, "estilo
                // UiStyles.CuerpoTenue o similar") con ancla a la derecha y un punto menos de
                // tamaño: se lee como un contador de página, no como una segunda frase.
                _estiloProgreso = new GUIStyle(UiStyles.TenueCentrado)
                {
                    alignment = TextAnchor.UpperRight,
                    fontSize = UiStyles.F(10),
                };
            }
            return _estiloProgreso;
        }
    }
}
