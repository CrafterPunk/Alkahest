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

        // (fix playtest 9) REESCRITAS COMO PROCEDIMIENTOS. El jugador reportó llevar horas
        // sin conseguir MULTIPLICAR cristal ni vivium a pesar de "hacer combinaciones":
        // las pistas viejas describían SABOR ("el azoth cristaliza al tocar semilla...")
        // pero nunca el hecho que de verdad desbloquea el nivel -- que la muestra del
        // Maestro es una SEMILLA/CATALIZADOR que NO SE GASTA, así que basta con sembrar
        // una vez y alimentarla. Cada pista de aquí abajo es un paso ejecutable con la
        // ubicación real del taller (Sim/SimLevelBuilder es la única fuente de verdad de
        // esas ubicaciones), no una descripción poética.
        private static readonly string[] PistasJornada2 =
        {
            "Las muestras del Maestro son SEMILLAS, no ingredientes que se gasten: azoth en el grifo más alto de la columna, vivium en el fondo de la CUBA DERECHA, semilla de cristal sobre la BANDEJA FRÍA (estante de arriba a la izquierda)",
            "PROCEDIMIENTO cristal: enciende la piedra gélida de la bandeja fría (E encima) hasta que HELANDO se lea en su chapa, luego vierte AZOTH sobre la semilla con el frasco — la semilla NO se consume, sigue ahí para el siguiente chorro",
            "PROCEDIMIENTO vivium: pon la placa ígnea de la CUBA DERECHA en TEMPLADA (E sobre ella, nunca ARDIENTE) y vierte NUTRIENTE alrededor del retoño — cada célula que nace es vivium NUEVO, el retoño original sigue vivo y sigue creciendo",
            "No hay grifo de FUEGO: se CREA. Placa ARDIENTE bajo aceite (o bajo tu vivium, si quieres perderlo) prende sola con el calor, sin ninguna chispa",
            "Congela agua en la bandeja fría (piedra gélida encendida) y viértela en la Tolva: el frasco conserva el frío por el camino",
        };

        private static readonly string[] PistasJornada3 =
        {
            "Guarda tus mejores mezclas en las REDOMAS del estante de redomas (junto a la bandeja fría): clic derecho para llenar, clic izquierdo para recuperar",
            "Los encargos grandes piden PRODUCCIÓN, no ingrediente en crudo: deja el criadero de la CUBA DERECHA (vivium + nutriente + placa TEMPLADA) trabajando solo mientras haces otra cosa — no hace falta vigilarlo",
            "El cristal SE PROPAGA: cada trozo ya formado en la bandeja fría es semilla tan buena como la original. Riega azoth frío sobre el FRENTE de cristal entero, no solo sobre el punto donde empezaste",
            "Bautiza (T) lo que descubras: el Maestro puede acabar pidiendo material POR EL NOMBRE que le pusisteis, no por su fórmula",
        };

        private string[] _pistas = PistasJornada1;
        private float _duracion = DuracionJornada1;
        private float _segundosPista = 11f;

        private float _playSeconds;
        private bool _everUnlocked;
        private bool _oculto;

        private void Awake()
        {
            ReiniciarParaJornada(1);
        }

        /// <summary>
        /// Reactiva las pistas al empezar una jornada, con el guion de ese día.
        /// Si el jugador pulsó H, respeta su decisión y no vuelve a aparecer.
        /// </summary>
        public void ReiniciarParaJornada(int dia)
        {
            _pistas = dia >= 3 ? PistasJornada3 : (dia == 2 ? PistasJornada2 : PistasJornada1);
            _duracion = dia == 1 ? DuracionJornada1 : DuracionOtrasJornadas;
            _segundosPista = _duracion / Mathf.Max(1, _pistas.Length);
            _playSeconds = 0f;
        }

        private void Update()
        {
            if (!DayCycle.InputLocked)
            {
                _everUnlocked = true;
                _playSeconds += Time.deltaTime;

                var kb = Keyboard.current;
                if (kb != null && kb.hKey.wasPressedThisFrame) _oculto = !_oculto;
            }
        }

        private void OnGUI()
        {
            if (!_everUnlocked || DayCycle.InputLocked || _oculto) return;
            if (_playSeconds >= _duracion) return;

            UiStyles.Preparar();

            int i = Mathf.Min((int)(_playSeconds / _segundosPista), _pistas.Length - 1);
            string texto = _pistas[i];

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
