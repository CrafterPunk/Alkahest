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
    /// Ahora cada jornada reactiva las pistas 60 s con el contenido de ESE día
    /// (<see cref="ReiniciarParaJornada"/>, llamado por Game/DayCycle.cs).
    ///
    /// NAVEGACIÓN MANUAL (fix playtest 7): "debería salir una flechita de leer
    /// siguiente... para poder leer todas antes de ocultarlas". IZQUIERDA/
    /// DERECHA mueven una pista por pulsación (con tope en los extremos, no
    /// cíclico). En cuanto el jugador toca una flecha, la rotación automática
    /// por tiempo se para para el resto de la tanda Y la tanda deja de
    /// caducar: el jugador ha tomado el control, así que las pistas se quedan
    /// hasta que las oculte con H. Es justo lo que pedía el playtest: poder
    /// leerlas todas sin la presión del reloj interno. ReiniciarParaJornada
    /// vuelve a poner el modo automático para la nueva tanda.
    ///
    /// El alto del panel se MIDE con word-wrap real (CalcHeight): ninguna frase
    /// se corta, por larga que sea, y el ancho se acota para no pisar el panel
    /// del frasco (izquierda) ni el de encargos (derecha).
    /// </summary>
    public sealed class HintSystem : MonoBehaviour
    {
        /// <summary>Duración de la tanda de pistas: la jornada 1 enseña a jugar (larga), las siguientes recuerdan lo nuevo (1 minuto).</summary>
        private const float DuracionJornada1 = 150f;
        private const float DuracionOtrasJornadas = 60f;

        private static readonly string[] PistasJornada1 =
        {
            "Muévete con WASD · aspira con CLIC IZQUIERDO · vierte con CLIC DERECHO",
            "Pulsa E junto a un GRIFO del banco (columna de la izquierda) para abrir el caudal: todo cae en la PILA de recogida",
            "El Maestro paga por EFECTOS, no por recetas: mira los encargos (arriba a la derecha)",
            "¿\"Algo que arda\"? El aceite arde... y lo vivo también, si lo provocas",
            "Se entrega VERTIENDO en la TOLVA DEL MAESTRO: el hueco dorado del muro derecho",
            "La PLACA ÍGNEA bajo cada cuba calienta (E la regula: TEMPLADA cultiva, ARDIENTE prende) · la PIEDRA GÉLIDA del estante hiela",
            "El frasco conserva la TEMPERATURA de lo que aspiras: el hielo sigue helado al verterlo en la Tolva",
            "Pulsa T para BAUTIZAR una sustancia con vuestro nombre · J abre tu diario",
        };

        private static readonly string[] PistasJornada2 =
        {
            "El Maestro os deja muestras: AZOTH en el grifo nuevo, un retoño de VIVIUM en la cuba derecha y SEMILLA DE CRISTAL en la bandeja fría",
            "El AZOTH cristaliza al tocar SEMILLA DE CRISTAL... pero solo en FRÍO: enciende la piedra gélida antes de verterlo",
            "El VIVIUM crece comiendo NUTRIENTE cuando está TEMPLADO — y arde si te pasas de calor",
            "No hay grifo de FUEGO: el fuego se CREA, poniendo la placa ARDIENTE bajo aceite (o bajo tu cultivo, si te atreves)",
            "Congela agua en la bandeja fría y entrégala: el frasco no pierde el frío por el camino",
        };

        private static readonly string[] PistasJornada3 =
        {
            "Guarda tus mejores mezclas en las REDOMAS del estante: clic derecho para llenar, clic izquierdo para recuperar",
            "Los encargos grandes piden PRODUCCIÓN: monta un criadero (vivium + nutriente + placa TEMPLADA) y déjalo trabajar solo",
            "El cristal se extiende: cada trozo nuevo sirve de semilla para el siguiente. Riega azoth sobre lo que ya cristalizó",
            "Bautiza (T) lo que descubras: el Maestro acabará pidiendo material POR EL NOMBRE que le pusisteis",
        };

        private string[] _pistas = PistasJornada1;
        private float _duracion = DuracionJornada1;
        private float _segundosPista = 11f;

        private float _playSeconds;
        private bool _everUnlocked;
        private bool _oculto;

        /// <summary>Índice de la pista mostrada (fuente de verdad tanto en modo automático como manual).</summary>
        private int _indice;
        /// <summary>True en cuanto el jugador pulsa una flecha: para la rotación automática y la caducidad por tiempo (ver doc de la clase).</summary>
        private bool _modoManual;

        // Caché del pie del panel ("◀ ▶  3/8   ·   H — ocultar consejos"):
        // solo se reconstruye si cambia el índice mostrado o el número total de
        // pistas (cambia de jornada), no en cada OnGUI.
        private int _cachePieIndice = -1;
        private int _cachePieTotal = -1;
        private string _piePos = "";

        private void Awake()
        {
            ReiniciarParaJornada(1);
        }

        /// <summary>
        /// Reactiva las pistas al empezar una jornada, con el guion de ese día.
        /// Si el jugador pulsó H, respeta su decisión y no vuelve a aparecer.
        /// También vuelve a poner el modo automático: cada jornada empieza
        /// rotando sola hasta que el jugador vuelva a tomar el control.
        /// </summary>
        public void ReiniciarParaJornada(int dia)
        {
            _pistas = dia >= 3 ? PistasJornada3 : (dia == 2 ? PistasJornada2 : PistasJornada1);
            _duracion = dia == 1 ? DuracionJornada1 : DuracionOtrasJornadas;
            _segundosPista = _duracion / Mathf.Max(1, _pistas.Length);
            _playSeconds = 0f;
            _indice = 0;
            _modoManual = false;
            _cachePieIndice = -1; // fuerza reconstruir el pie con la nueva tanda.
        }

        private void Update()
        {
            if (DayCycle.InputLocked) return;
            _everUnlocked = true;

            var kb = Keyboard.current;
            if (kb != null && kb.hKey.wasPressedThisFrame) _oculto = !_oculto;

            if (kb != null)
            {
                bool pulsaIzq = kb.leftArrowKey.wasPressedThisFrame;
                bool pulsaDer = kb.rightArrowKey.wasPressedThisFrame;
                if (pulsaIzq || pulsaDer)
                {
                    // (fix playtest 7) El jugador ha tomado el control: se acabó
                    // la rotación automática Y la caducidad por tiempo de esta
                    // tanda (ver doc de la clase). Una pista por pulsación, tope
                    // en los extremos (NO cíclico: llegar al final debe sentirse
                    // como el final, no dar la vuelta a la primera).
                    _modoManual = true;
                    int delta = (pulsaDer ? 1 : 0) - (pulsaIzq ? 1 : 0);
                    _indice = Mathf.Clamp(_indice + delta, 0, _pistas.Length - 1);
                }
            }

            if (!_modoManual)
            {
                _playSeconds += Time.deltaTime;
                _indice = Mathf.Min((int)(_playSeconds / _segundosPista), _pistas.Length - 1);
            }
        }

        private void OnGUI()
        {
            if (!_everUnlocked || DayCycle.InputLocked || _oculto) return;
            // En modo manual la tanda ya no caduca por tiempo: el jugador pidió
            // quedarse con las pistas hasta ocultarlas con H (ver doc de clase).
            if (!_modoManual && _playSeconds >= _duracion) return;

            UiStyles.Preparar();

            string texto = _pistas[_indice];
            ActualizarPieCache();

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
                _piePos, UiStyles.TenueCentrado);
        }

        /// <summary>
        /// Reconstruye "◀ ▶  3/8   ·   H — ocultar consejos" solo cuando cambia
        /// el índice o el tamaño de la tanda (nunca por frame). La flecha que no
        /// se puede usar (◀ en la primera pista, ▶ en la última) se OMITE del
        /// string en vez de dibujarse deshabilitada: es la forma más barata de
        /// "atenuarla" sin un estilo aparte.
        /// </summary>
        private void ActualizarPieCache()
        {
            if (_indice == _cachePieIndice && _pistas.Length == _cachePieTotal) return;
            _cachePieIndice = _indice;
            _cachePieTotal = _pistas.Length;

            string flechas = "";
            if (_indice > 0) flechas += "◀ ";
            if (_indice < _pistas.Length - 1) flechas += "▶";
            flechas = flechas.TrimEnd();

            string separador = flechas.Length > 0 ? "  " : "";
            _piePos = flechas + separador + (_indice + 1) + "/" + _pistas.Length + "   ·   H — ocultar consejos";
        }
    }
}
