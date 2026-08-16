using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy] Onboarding suave: pistas rotatorias arriba-centro, bajo el
    /// reloj de la jornada. H las oculta para siempre en esta partida.
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
    /// del frasco (izquierda) ni el de encargos (derecha).
    /// </summary>
    public sealed class HintSystem : MonoBehaviour
    {
        /// <summary>Segundos de lectura por pista: la jornada 1 va algo más despacio (primera vez con los controles), las siguientes ya conocen el ritmo.</summary>
        private const float SegundosPorPistaJornada1 = 9f;
        private const float SegundosPorPistaOtras = 8f;

        private static readonly string[] PistasJornada1 =
        {
            "Muévete con WASD.",
            "Aspira con clic izquierdo.",
            "Vierte con clic derecho.",
            "Pulsa E junto a un grifo para abrir el caudal.",
            "Mira los ENCARGOS arriba a la derecha: el Maestro paga por efectos.",
            "El aceite arde: acércalo a la placa encendida.",
            "Vierte en la TOLVA dorada del muro derecho para entregar.",
            "Pulsa E en una placa para regular su calor.",
            "La piedra gélida del estante enfría lo que toca.",
            "Aspira hielo y viértelo helado en la Tolva: el frasco conserva el frío.",
            "Pulsa T para bautizar lo que apuntas o llevas en el frasco.",
            "Pulsa J para abrir tu diario.",
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
            "Los encargos usarán el nombre que le pongáis a cada cosa.",
        };

        private string[] _pistas = PistasJornada1;
        private float _segundosPista = SegundosPorPistaJornada1;
        private float _duracion;
        private bool[] _registrada = new bool[0];

        private float _playSeconds;
        private bool _everUnlocked;
        private bool _oculto;

        // -----------------------------------------------------------------
        // [playtest 24, LA MAREA -- CONTRATO_MAREA.md §4.5] CANAL DE
        // PRIORIDAD: ver EncolarPistaDeMarea más abajo.
        // -----------------------------------------------------------------
        /// <summary>Cuánto se ve una pista prioritaria antes de devolver el turno a la rotación normal de jornada -- mismo tiempo de lectura que las pistas de jornada 2/3 (SegundosPorPistaOtras): son igual de cortas, una línea ejecutable cada una.</summary>
        private const float SegundosPistaPrioritaria = SegundosPorPistaOtras;

        private string _pistaPrioritaria;
        private float _pistaPrioritariaSegundosRestantes;
        private bool _pistaPrioritariaArchivada;

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
        // TODO (próxima ronda, otro agente): enganchar esto en JournalHud para listar
        // "lo que ya os han dicho" en una sección del diario. Todavía NADIE la lee.
        // ---------------------------------------------------------------------------------
        private static readonly List<string> _pistasMostradas = new List<string>();

        /// <summary>Pistas ya mostradas al jugador esta partida, en el orden en que aparecieron por primera vez. Ver nota de arriba: todavía sin consumidor, lista para engancharse.</summary>
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
        }

        /// <summary>
        /// [playtest 24, LA MAREA -- CONTRATO_MAREA.md §4.5] Canal de
        /// PRIORIDAD para Game/MareaDirector.cs (mismo playtest): INTERRUMPE
        /// la cola normal de jornada y muestra esta línea con la MISMA placa
        /// y el MISMO estilo -- las tres pistas del arco de la marea
        /// (despertar / primer Rocío / marea subiendo) no pueden esperar su
        /// turno en una rotación que va a 8-9s por frase y puede llevar
        /// minutos en dar la vuelta. NO toca `_playSeconds`/`_registrada`
        /// (la rotación normal de jornada sigue corriendo por debajo y
        /// retoma exactamente donde le tocaría estar en cuanto la prioridad
        /// expira, ver OnGUI) ni pasa por la cola: es un interruptor
        /// aparte, no una entrada más de <see cref="_pistas"/>.
        /// </summary>
        public void EncolarPistaDeMarea(string pista)
        {
            _pistaPrioritaria = pista;
            _pistaPrioritariaSegundosRestantes = SegundosPistaPrioritaria;
            _pistaPrioritariaArchivada = false;
        }

        private void Update()
        {
            if (!DayCycle.InputLocked)
            {
                _everUnlocked = true;
                _playSeconds += Time.deltaTime;

                if (_pistaPrioritariaSegundosRestantes > 0f)
                    _pistaPrioritariaSegundosRestantes -= Time.deltaTime;

                var kb = Keyboard.current;
                // (fix playtest 10) H es un atajo de una tecla como cualquier otro del
                // proyecto: debe respetar UiStyles.EscribiendoTexto (regla nueva, ver su
                // doc-comment) para que escribir un nombre que contenga "h" no oculte las
                // pistas sin querer -- el mismo bug que ya se arregló para M (mute) y T
                // (Game/NamingUi.cs), aplicado aquí.
                if (kb != null && kb.hKey.wasPressedThisFrame && !UiStyles.EscribiendoTexto) _oculto = !_oculto;
            }
        }

        private void OnGUI()
        {
            if (!_everUnlocked || DayCycle.InputLocked || DayCycle.HudSilenciado || _oculto) return; // (playtest 21) HudSilenciado, hermano de InputLocked.

            // [playtest 24, LA MAREA] La pista PRIORITARIA (ver
            // EncolarPistaDeMarea) se dibuja EN VEZ de la rotación normal
            // mientras esté viva -- incluso si `_playSeconds >= _duracion`
            // ya apagó la rotación normal hace rato (el arco de la marea
            // puede disparar sus tres avisos bien entrada la partida, mucho
            // después de que la cola de jornada 1/2/3 haya terminado de
            // rotar).
            bool prioritaria = _pistaPrioritariaSegundosRestantes > 0f;
            if (!prioritaria && _playSeconds >= _duracion) return;

            UiStyles.Preparar();

            string texto;
            if (prioritaria)
            {
                texto = _pistaPrioritaria;
                // Archivo para el diario, una sola vez por pista prioritaria
                // (mismo criterio de coste que el bloque normal de abajo).
                if (!_pistaPrioritariaArchivada)
                {
                    _pistaPrioritariaArchivada = true;
                    if (!_pistasMostradas.Contains(texto)) _pistasMostradas.Add(texto);
                }
            }
            else
            {
                int i = Mathf.Min((int)(_playSeconds / _segundosPista), _pistas.Length - 1);
                texto = _pistas[i];

                // Archivo para el diario (ver PistasMostradas arriba): una sola vez por
                // índice, no un Contains() por frame -- barato incluso a 60+ FPS.
                if (i >= 0 && i < _registrada.Length && !_registrada[i])
                {
                    _registrada[i] = true;
                    if (!_pistasMostradas.Contains(texto)) _pistasMostradas.Add(texto);
                }
            }

            float pad = UiStyles.S(9f);
            float acento = UiStyles.S(3f);
            // Ancho máximo 560 px de diseño, pero siempre dejando libres los ~700 px
            // de diseño que ocupan el panel del frasco y el de encargos.
            float ancho = Mathf.Clamp(Screen.width - UiStyles.S(700f), UiStyles.S(300f), UiStyles.S(560f));
            float interior = ancho - pad * 2f - acento;

            float altoTexto = UiStyles.Alto(UiStyles.CuerpoCentrado, texto, interior);
            float altoPie = UiStyles.TenueCentrado.lineHeight;
            float alto = pad + altoTexto + UiStyles.S(3f) + altoPie + pad;

            // Justo debajo del reloj de la jornada (ver DayCycle.DrawPlayingHud).
            var panel = new Rect((Screen.width - ancho) * 0.5f, UiStyles.S(54f), ancho, alto);
            UiStyles.Panel(panel);
            UiStyles.Rellenar(new Rect(panel.x, panel.y, acento, panel.height), UiStyles.Oro);

            GUI.Label(new Rect(panel.x + acento + pad, panel.y + pad, interior, altoTexto), texto, UiStyles.CuerpoCentrado);
            GUI.Label(new Rect(panel.x + acento + pad, panel.yMax - pad - altoPie, interior, altoPie),
                "H — ocultar consejos", UiStyles.TenueCentrado);
        }
    }
}
