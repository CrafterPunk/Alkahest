using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 73 — EL PRÓLOGO REHECHO, espec completa de Cesar) El director
    /// de la FUNDACIÓN: los primeros minutos de TEN THOUSAND YEARS.
    ///
    /// EL MANDATO: "la prioridad no es explicar muchos sistemas, sino
    /// presentar bien el VERBO principal del juego: absorber y verter
    /// materia". Todo lo demás está al servicio de eso. El arco:
    ///
    ///   1. INICIO OSCURO   — naces en penumbra; la voz te llama (VEN.);
    ///                        aprendes a moverte/volar (tutorial contextual
    ///                        que valida DESPLAZAMIENTO REAL, no pulsaciones).
    ///   2. ENCUENTRO       — llegas a la presencia. TOMA. Recibes el frasco
    ///                        (vuela de su mesa a tu mano).
    ///   3. AGUA            — una CASCADA fluye por el escenario (mana del
    ///                        muro, corre por dos repisas, cae a la poza:
    ///                        física celular a la vista, ganas de tocarla).
    ///                        AGUA. Aspirar (validado: materia que de verdad
    ///                        entró), verter (ídem), y un rato de juego libre.
    ///   4. ENTREGA         — TRÁELA. Viertes lo pedido en el CUENCO del
    ///                        Maestro (receptor placeholder: la entrega usa
    ///                        el verbo, no un menú). Él lo toma (drenado).
    ///   5. LODO            — un DERRUMBE abre una gotera de lodo en el techo
    ///                        (no estaba en el suelo antes; el Maestro NO
    ///                        entrega materiales — el mundo sí). LODO. Se
    ///                        comporta distinto al agua (polvo: se apila).
    ///                        TRÁELO.
    ///   6. RECOMPENSA      — el mundo responde: el DEPÓSITO DE AGUA emerge
    ///                        del suelo (Game/DepositoDeAgua.cs, según la
    ///                        referencia de Cesar) y queda usable con
    ///                        rellenado lento. Amanece. Fin de esta versión.
    ///
    /// LAS DOS VOCES (separación pedida): el texto del MAESTRO es narrativo
    /// (palabras sueltas, enormes, centro-alto, con su tono subgrave) y el
    /// TUTORIAL es funcional (fichas blancas junto al jugador,
    /// Game/TutorialContextual.cs). Jamás se mezclan.
    ///
    /// ESQUELETO PARA ITERAR (pedido expreso): textos, cantidades, tiempos y
    /// triggers viven juntos en el bloque EL GUION de aquí abajo.
    ///
    /// RETIRADO EN ESTA RONDA (regla 15 — la v1 completa vive en git y en
    /// docs/archivo/HISTORIAL_RONDAS.md, rondas 60-64): el arco de 6 beats
    /// barro→fogón→vidrio→estante→tablón (volverá más adelante, después del
    /// verbo), el diálogo por páginas estilo novela visual (palabras sueltas
    /// no lo necesitan), la banda de objetivo persistente (la sustituyen el
    /// tutorial contextual y la placa del cuenco), la gotera de estalactita
    /// (ahora cascada), el vertido de materiales del Maestro (ahora el mundo
    /// los trae: cascada, derrumbe) y los pedidos guiados de OrderSystem (el
    /// receptor es físico). Se CONSERVAN: la viñeta de oscuridad con luz de
    /// causa física, la silueta del Maestro, su fuego de Brasa real, LuzEn y
    /// DibujarBandaMaestro (los consume Game/Trueque.cs).
    /// </summary>
    public sealed class FundacionDirector : MonoBehaviour
    {
        private enum Beat { Despertar, Ven, Toma, Agua, EntregaAgua, Derrumbe, Lodo, EntregaLodo, Recompensa, LlenarDeposito, Fin }
        private enum AguaSub { Ir, Aspirar, Verter, Libre }

        // ==================================================================
        // EL GUION — todo lo que Cesar querrá tocar, junto y a la vista.
        // ==================================================================
        // -- Las palabras del Maestro (una por golpe de voz):
        private const string VozVen = "VEN.";
        private const string VozToma = "TOMA.";
        private const string VozAgua = "AGUA.";
        private const string VozTraela = "TRÁELA.";
        private const string VozBien = "BIEN.";
        private const string VozLodo = "LODO.";
        private const string VozTraelo = "TRÁELO.";
        private const string VozObserva = "OBSERVA.";
        private const string VozLlenalo = "LLÉNALO."; // (R74) la tarea final: el depósito llega con un culo de agua y llenarlo es tuyo.

        // -- Las leyendas del tutorial contextual (fichas blancas):
        private const string LeyendaMover = "muévete"; // (revisión Opus 73 #21) minúscula como sus hermanas: las leyendas son susurros funcionales, no títulos.
        private const string LeyendaAspirar = "mantén — aspira el agua";
        private const string LeyendaVerter = "mantén — viértela donde quieras";

        // -- Cantidades (celdas de materia REAL):
        private const int AspirarMeta = 10;      // agua que debe ENTRAR al frasco para confirmar el gesto.
        private const int VerterMeta = 6;        // agua que debe SALIR para confirmar verter.
        private const int EntregaAguaMeta = 20;  // agua en el cuenco para completar la entrega.
        private const int LodoProbarMeta = 8;    // lodo aspirado que dispara el TRÁELO.
        private const int EntregaLodoMeta = 16;  // lodo en el cuenco.
        private const int LlenarDepositoMeta = 48; // (R74) agua dentro del tanque para cerrar el prólogo (llega con ~14; interior 6 de ancho: ~8 filas al completar).
        private const float MoverMetaMundo = 0.5f; // desplazamiento real (unidades de mundo) por dirección para confirmar cada tecla.

        // -- Tiempos:
        private const float DespertarPausaSeg = 1.4f;  // oscuridad a solas antes de las fichas WASD.
        private const float TrasTutorialSeg = 0.8f;    // respiro entre el tutorial completado y la voz VEN.
        private const float EntregaFrascoSeg = 0.95f;  // vuelo del frasco de la mesa a tu mano.
        private const float TrasTomaSeg = 1.3f;        // respiro tras recibir el frasco.
        private const float JuegoLibreSeg = 14f;       // rato de jugar con el agua antes del TRÁELA.
        private const float LodoLibreSeg = 22f;        // tope de experimentación con el lodo antes del TRÁELO.
        private const float VozHoldSeg = 2.1f;         // cuánto sostiene cada palabra en pantalla.
        private const float DerrumbePausaSeg = 2.2f;   // respiro entre la entrega del agua y el techo abriéndose.

        // -- Triggers de distancia (en celdas):
        private const float DistCharla = 16f;    // "estar con el Maestro".
        private const float DistZonaAgua = 26f;  // radio alrededor de la poza que dispara AGUA.

        // -- La luz (radios de viñeta por tramo, en px escalados):
        private const float RadioDespertar = 180f;
        private const float RadioVen = 260f;
        private const float RadioToma = 330f;
        private const float RadioAgua = 440f;
        private const float RadioTaller = 540f;
        private const float RadioAmanecer = 2400f;

        // -- La cascada (el caudal lo mantiene el director; el camino lo talla SimLevelBuilder):
        private const float ManantialSeg = 0.14f;   // cadencia del brote (0.24 daba un hilo demasiado ralo — visto en captura R73).
        private const int ManantialCeldas = 2;      // hilo de 2 celdas por pulso: caudal visible, no diluvio.
        private const int PozaLlenaCeldas = 48;     // nivel de equilibrio del rezumado (R74: la poza es más honda — capacidad física ~70; el nivel llena unas 3.5 filas).

        // -- El derrumbe y la gotera de lodo:
        private const int LodoBurstCeldas = 26;     // el reventón inicial que cae con el derrumbe.
        private const float LodoSeepSeg = 0.4f;     // cadencia del goteo permanente posterior.
        private const int LodoMonticuloTope = 70;   // el goteo se pausa si el montículo crece hasta aquí...
        private const int LodoMonticuloResume = 50; // ...y vuelve cuando el jugador se lo lleva (histéresis).

        // -- El cuenco (drenado = "el Maestro lo toma"):
        private const float DrenarCadaSeg = 0.05f;

        private static readonly byte Lodo = MaterialId.MatDe(1, EstadoMateria.Polvo);

        // ==================================================================

        // (gate de DayCycle.DetectarPrimeraAccion) Mientras sea false, moverse
        // NO despierta el HUD en ModoFundacion. Se sube al recibir el frasco.
        public static bool HudPermitido;

        /// <summary>
        /// (RONDA 73) Antes del TOMA. no llevas frasco: Game/Flask.cs se guarda
        /// con esta bandera (ni haz, ni aspirar, ni HUD de mano) y
        /// ApprenticeController esconde el tarro decorativo. Solo la sube el
        /// prólogo; en cualquier otro modo (multi incluido) nace y muere en
        /// false.
        /// </summary>
        public static bool FrascoBloqueado;

        private AlkahestSim _sim;
        private Flask _flask;
        private Transform _aprendiz;
        private TutorialContextual _tutorial;
        private DepositoDeAgua _deposito;
        private AudioSource _audio;

        private Beat _beat = Beat.Despertar;
        private AguaSub _aguaSub = AguaSub.Ir;
        private float _tBeat;

        // Validación de movimiento (resultados, no teclas):
        private Vector3 _posAnterior;
        private readonly float[] _progMover = new float[4]; // W A S D.
        private readonly bool[] _moverOk = new bool[4];

        // Validación de aspirar/verter:
        private int _aguaBase;

        // El frasco volando de la mesa a la mano:
        private Transform _frascoVuelo;
        private float _tVuelo;

        // La voz:
        private string _vozTexto;
        private float _vozT;      // edad de la palabra actual.
        private float _vozDur;    // vida total (fade in + hold + fade out).
        

        // El manantial / la poza:
        private float _manantialTimer;

        // El derrumbe / el lodo:
        private bool _lodoActivo;
        private float _lodoTimer;
        private int _lodoIdx;
        private bool _craterAbierto;
        private int _burstRestante;
        private float _burstTimer;

        // El cuenco:
        private byte _drenandoMat;
        private float _drenarTimer;
        private bool _drenando;

        // La viñeta (conservada de la v1: luz con causa física).
        private Texture2D _vineta;
        private float _radio;
        private float _radioObjetivo;
        private float _luzFuego = 1f;
        private float _focoBias = 1f; // 1 = la luz es del jugador; hacia 0 = sesgada al fuego del Maestro.

        private const byte BrasaRaw = 165;
        private const byte BrasaVida = 90;
        private const float FlamaSegundos = 0.12f;
        private const float Flama2Segundos = 1.7f;
        private float _flamaTimer, _flama2Timer;
        private int _flamaIdx;

        public void Init(AlkahestSim sim, OrderSystem orders, Flask flask, SubstanceKnowledge saber, Transform aprendiz)
        {
            // (orders/saber se aceptan por compatibilidad de firma con el
            // bootstrap; el prólogo rehecho no encola pedidos — el receptor
            // es físico — ni bautiza nada todavía.)
            _sim = sim;
            _flask = flask;
            _aprendiz = aprendiz;
            _posAnterior = aprendiz.position;

            SpawnMaestroSilueta();
            HudPermitido = false;
            FrascoBloqueado = true;

            _tutorial = new GameObject("TutorialContextual").AddComponent<TutorialContextual>();
            _tutorial.Init(aprendiz);

            _deposito = new GameObject("DepositoDeAgua").AddComponent<DepositoDeAgua>();
            _deposito.Init(sim);

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            _radioObjetivo = UiStyles.S(RadioDespertar);
            _radio = _radioObjetivo;
        }

        private void Awake() { _instancia = this; }

        private void OnDestroy()
        {
            HudPermitido = false;
            FrascoBloqueado = false;
            SimRenderer.FocoCinematico = null;
            if (_instancia == this) _instancia = null;
            if (_vineta != null) Destroy(_vineta);
            if (_maestroGo != null) Destroy(_maestroGo);
            if (_maestroTex != null) Destroy(_maestroTex);
            if (_frascoVuelo != null) Destroy(_frascoVuelo.gameObject);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;

            // (revisión Opus 73 #1) CON EL MENÚ DE PAUSA DELANTE, EL DIRECTOR
            // ESPERA ENTERO: la sim está congelada (DayCycle.ApplyPause) pero
            // Time.deltaTime no — sin esta guarda los timers seguían corriendo
            // contra un mundo quieto: el rezumado vaciaba la poza en 0.6 s, el
            // reventón del derrumbe repintaba 3 celdas en vez de verter 26, y
            // el beat avanzaba con la cinemática sin público. Un ESC en mal
            // momento se comía el derrumbe completo.
            if (DayCycle.InputLocked) { _posAnterior = _aprendiz.position; return; }

            // La física de fondo corre SIEMPRE: el fuego arde, el manantial
            // brota, la poza rezuma, el lodo gotea. El mundo no se congela
            // para hablar.
            MantenerFuegoDelMaestro();
            TickManantial();
            RezumarPoza();
            TickLodo();
            TickDrenarCuenco();
            TickVoz();
            TickVueloDelFrasco();

            _tBeat += Time.deltaTime;
            switch (_beat)
            {
                case Beat.Despertar: TickDespertar(); break;
                case Beat.Ven: TickVen(); break;
                case Beat.Toma: TickToma(); break;
                case Beat.Agua: TickAgua(); break;
                case Beat.EntregaAgua: TickEntrega(MaterialId.Water, EntregaAguaMeta, Beat.Derrumbe); break;
                case Beat.Derrumbe: TickDerrumbe(); break;
                case Beat.Lodo: TickLodoBeat(); break;
                case Beat.EntregaLodo: TickEntrega(Lodo, EntregaLodoMeta, Beat.Recompensa); break;
                case Beat.Recompensa: TickRecompensa(); break;
                case Beat.LlenarDeposito: TickLlenarDeposito(); break;
                case Beat.Fin: break; // greybox libre: el prólogo dijo lo suyo.
            }

            _posAnterior = _aprendiz.position;
            _radio = Mathf.Lerp(_radio, _radioObjetivo, Time.deltaTime * 1.6f);
        }

        private void CambiarBeat(Beat b)
        {
            _beat = b;
            _tBeat = 0f;
        }

        // ------------------------------------------------------------------
        // LA VOZ DEL MAESTRO: una palabra, enorme, con su tono subgrave.
        // ------------------------------------------------------------------
        /// <summary>
        /// (revisión Opus 73 #7) DOS PALABRAS NUNCA SE PISAN: si la palabra
        /// viva aún no empezó a despedirse, la nueva ESPERA en cola y la
        /// vieja adelanta su fade — cada palabra del Maestro termina de
        /// decirse. Solo cabe una en cola (la última manda: sus palabras son
        /// órdenes, no un guion largo).
        /// </summary>
        private void Decir(string palabra)
        {
            if (_vozTexto != null && _vozT < _vozDur - VozFadeOutSeg)
            {
                _vozT = _vozDur - VozFadeOutSeg; // la viva empieza a despedirse ya.
                _vozPendiente = palabra;
                return;
            }
            if (_vozTexto != null) { _vozPendiente = palabra; return; } // ya se despide sola.
            DecirAhora(palabra);
        }
        private string _vozPendiente;
        private const float VozFadeOutSeg = 0.8f;

        private void DecirAhora(string palabra)
        {
            _vozTexto = palabra;
            _vozT = 0f;
            _vozDur = 0.45f + VozHoldSeg + VozFadeOutSeg; // fade in + hold + fade out.
            if (_audio != null)
            {
                // El pitch varía un pelo por palabra (determinista): la voz
                // respira sin cambiar de timbre.
                _audio.pitch = 0.96f + 0.03f * (palabra.Length % 3);
                _audio.PlayOneShot(Audio.SintetizadorSfx.VozDelMaestro,
                    0.8f * Audio.DirectorDeAudio.VolumenEfectos);
            }
        }

        private void TickVoz()
        {
            if (_vozTexto != null)
            {
                _vozT += Time.deltaTime;
                if (_vozT >= _vozDur) _vozTexto = null;
            }
            if (_vozTexto == null && _vozPendiente != null)
            {
                string p = _vozPendiente;
                _vozPendiente = null;
                DecirAhora(p);
            }
        }

        // ------------------------------------------------------------------
        // Beats.
        // ------------------------------------------------------------------
        /// <summary>
        /// 1) EL INICIO OSCURO: penumbra, silencio, y las fichas WASD que se
        /// confirman con DESPLAZAMIENTO REAL por dirección (no con la
        /// pulsación: si vuelas contra la roca, la ficha no se enciende — la
        /// roca delimita, y eso también se aprende aquí).
        /// </summary>
        private void TickDespertar()
        {
            if (_tBeat > DespertarPausaSeg && !_tutorial.Visible && !_tutorial.Terminado)
            {
                _tutorial.Mostrar(LeyendaMover,
                    new TutorialContextual.Paso { Etiqueta = "W", Presionada = () => Keyboard.current != null && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) },
                    new TutorialContextual.Paso { Etiqueta = "A", Presionada = () => Keyboard.current != null && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) },
                    new TutorialContextual.Paso { Etiqueta = "S", Presionada = () => Keyboard.current != null && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) },
                    new TutorialContextual.Paso { Etiqueta = "D", Presionada = () => Keyboard.current != null && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) });
            }

            if (_tutorial.Visible)
            {
                // Desplazamiento real por eje y sentido, acumulado — Y CON LA
                // TECLA PULSADA: el resultado y el gesto, juntos. (Sin la
                // guarda de tecla, el bob de vuelo del imp — ±0.04 por ciclo,
                // solo las mitades positivas hacia W y las negativas hacia S —
                // confirmaría "moverse" a un jugador quieto en ~10 s.)
                var kb = Keyboard.current;
                Vector3 d = _aprendiz.position - _posAnterior;
                if (kb != null)
                {
                    if (d.y > 0f && (kb.wKey.isPressed || kb.upArrowKey.isPressed)) _progMover[0] += d.y;
                    if (d.x < 0f && (kb.aKey.isPressed || kb.leftArrowKey.isPressed)) _progMover[1] -= d.x;
                    if (d.y < 0f && (kb.sKey.isPressed || kb.downArrowKey.isPressed)) _progMover[2] -= d.y;
                    if (d.x > 0f && (kb.dKey.isPressed || kb.rightArrowKey.isPressed)) _progMover[3] += d.x;
                }
                for (int i = 0; i < 4; i++)
                {
                    if (!_moverOk[i] && _progMover[i] >= MoverMetaMundo)
                    {
                        _moverOk[i] = true;
                        _tutorial.Confirmar(i);
                    }
                }
            }

            // (revisión Opus 73 #9) El respiro se mide desde el CIERRE del
            // tutorial, no desde el inicio del beat: _tBeat se re-arma en el
            // instante del evento para que la constante signifique lo que dice.
            if (_tutorial.Terminado && !_tutorialCerrado) { _tutorialCerrado = true; _tBeat = 0f; }
            if (_tutorialCerrado && _tBeat > TrasTutorialSeg)
            {
                CambiarBeat(Beat.Ven);
                Decir(VozVen);
                _radioObjetivo = UiStyles.S(RadioVen);
                _focoBias = 0.62f; // la luz se estira hacia el fuego: la única señal de hacia dónde.
            }
        }
        private bool _tutorialCerrado;

        /// <summary>2) VEN.: la luz insinúa el rumbo del fuego; llegar a la presencia dispara el encuentro.</summary>
        private void TickVen()
        {
            // (revisión Opus 73 #6) La palabra VEN. se dice entera aunque el
            // jugador ya esté pegado al Maestro (terminó el tutorial a sus
            // pies): el encuentro espera a que la voz suelte la palabra.
            if (_tBeat < VozHoldSeg) return;
            if (DistAlMaestro() < DistCharla)
            {
                CambiarBeat(Beat.Toma);
                Decir(VozToma);
                _radioObjetivo = UiStyles.S(RadioToma);
                _focoBias = 0.85f;
                LanzarVueloDelFrasco();
            }
        }

        /// <summary>3) TOMA.: el frasco vuela de la mesa a tu mano; con él llegan el HUD y el verbo.</summary>
        private void TickToma()
        {
            if (_frascoVuelo != null) return; // el vuelo sigue en el aire (TickVueloDelFrasco).
            if (!FrascoBloqueado && _tBeat > TrasTomaSeg)
            {
                CambiarBeat(Beat.Agua);
                _aguaSub = AguaSub.Ir;
                _radioObjetivo = UiStyles.S(RadioAgua);
                _focoBias = 1f;
            }
        }

        private float DistAPoza()
        {
            float celda = SimRenderer.CellWorldSize;
            float px = (SimLevelBuilder.FundacionCharcoX0 + SimLevelBuilder.FundacionCharcoX1) * 0.5f * celda;
            float py = (SimLevelBuilder.FundacionY0 + 1) * celda;
            return Vector2.Distance(_aprendiz.position, new Vector2(px, py)) / celda;
        }

        /// <summary>
        /// 4) AGUA.: la cascada ya corría desde el minuto cero; al entrar en
        /// su zona, la voz la nombra y el tutorial enseña el verbo — validado
        /// por MATERIA REAL (celdas que entraron/salieron del frasco), jamás
        /// por el clic. Después, juego libre: el agua se deja tocar sin que
        /// nadie pida nada.
        /// </summary>
        private void TickAgua()
        {
            switch (_aguaSub)
            {
                case AguaSub.Ir:
                    if (DistAPoza() < DistZonaAgua)
                    {
                        Decir(VozAgua);
                        _aguaSub = AguaSub.Aspirar;
                        _aguaBase = _flask.GetCount(MaterialId.Water);
                        _tutorial.Mostrar(LeyendaAspirar,
                            new TutorialContextual.Paso { Etiqueta = "CLIC IZQ", Presionada = () => Mouse.current != null && Mouse.current.leftButton.isPressed });
                    }
                    break;

                case AguaSub.Aspirar:
                    if (_flask.GetCount(MaterialId.Water) >= _aguaBase + AspirarMeta)
                    {
                        _tutorial.Confirmar(0);
                        _aguaSub = AguaSub.Verter;
                        _aguaBase = _flask.GetCount(MaterialId.Water);
                        _tBeat = 0f;
                    }
                    break;

                case AguaSub.Verter:
                    if (!_vertidoEnsenado)
                    {
                        // Espera a que la ficha anterior termine su fade antes
                        // de enseñar la siguiente (respiración entre gestos).
                        if (_tutorial.Visible || _tBeat < 0.5f) break;
                        _vertidoEnsenado = true;
                        _tutorial.Mostrar(LeyendaVerter,
                            new TutorialContextual.Paso { Etiqueta = "CLIC DER", Presionada = () => Mouse.current != null && Mouse.current.rightButton.isPressed });
                        break;
                    }
                    if (_flask.GetCount(MaterialId.Water) <= _aguaBase - VerterMeta)
                    {
                        _tutorial.Confirmar(0);
                        _aguaSub = AguaSub.Libre;
                        _tBeat = 0f;
                    }
                    break;

                case AguaSub.Libre:
                    if (_tBeat > JuegoLibreSeg)
                    {
                        CambiarBeat(Beat.EntregaAgua);
                        Decir(VozTraela);
                        _radioObjetivo = UiStyles.S(RadioTaller);
                    }
                    break;
            }
        }
        private bool _vertidoEnsenado;

        /// <summary>
        /// 4b/5b) LA ENTREGA: viertes lo pedido en el cuenco junto a la mesa
        /// (se cuenta materia REAL dentro del tallado). Al completarse, el
        /// Maestro lo TOMA: el cuenco se drena a la vista, celda a celda.
        /// </summary>
        private void TickEntrega(byte mat, int meta, Beat siguiente)
        {
            if (_drenando) return; // esperando a que el Maestro termine de tomar.
            // (revisión Opus 73 #5) Si el cuenco ya estaba lleno (jugador
            // previsor durante el juego libre), el TRÁELA. igual se dice
            // entero antes de aceptarse la entrega.
            if (_tBeat < VozHoldSeg + 0.5f) return;

            if (ContarEnCuenco(mat) >= meta)
            {
                Decir(VozBien);
                _drenando = true;
                _drenandoMat = mat;
                _drenarTimer = 0.6f; // medio respiro con el cuenco lleno antes de tomarlo.
                _beatTrasDrenar = siguiente;
            }
        }
        private Beat _beatTrasDrenar;

        /// <summary>5) EL DERRUMBE: el techo se abre delante del jugador — plano cinematográfico, sacudida, reventón de lodo, cráter. La secuencia entera es tiempo real de la sim: lo que cae, cae de verdad.</summary>
        private void TickDerrumbe()
        {
            float celda = SimRenderer.CellWorldSize;
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            int gx = SimLevelBuilder.FundacionDerrumbeX;

            if (_tBeat < DerrumbePausaSeg) return; // respiro tras la entrega del agua.
            float t = _tBeat - DerrumbePausaSeg;

            if (!_derrumbeArrancado)
            {
                _derrumbeArrancado = true;
                SimRenderer.FocoCinematico = new Vector3(gx * celda, (SimLevelBuilder.FundacionY0 + 24) * celda, 0f);
                SimRenderer.Sacudida = SimRenderer.SacudidaDuracion;
                if (_audio != null)
                {
                    _audio.pitch = 1f;
                    _audio.PlayOneShot(Audio.SintetizadorSfx.Derrumbe, 0.9f * Audio.DirectorDeAudio.VolumenEfectos);
                }
                // La grieta: un cono coherente en la bóveda (revisión Opus 73
                // #13: la versión anterior con dx*2 dejaba dos celdas
                // huérfanas flotando dentro de la roca). 5 de boca, 3 arriba —
                // más angosta que la caja del imp: nadie se fuga por el techo.
                for (int dx = -2; dx <= 2; dx++)
                    grid.SetCell(gx + dx, SimLevelBuilder.FundacionY1 + 1, MaterialId.Empty);
                for (int dx = -1; dx <= 1; dx++)
                    grid.SetCell(gx + dx, SimLevelBuilder.FundacionY1 + 2, MaterialId.Empty);
                grid.WakeChunk(gx, SimLevelBuilder.FundacionY1, tick);
                _burstRestante = LodoBurstCeldas;
            }

            // El reventón: los primeros terrones caen en tromba por la grieta.
            if (_burstRestante > 0)
            {
                _burstTimer -= Time.deltaTime;
                if (_burstTimer <= 0f)
                {
                    _burstTimer = 0.045f;
                    int jitter = (_burstRestante % 3) - 1;
                    _sim.PaintStable(gx + jitter, SimLevelBuilder.FundacionY1, 0, Lodo);
                    _burstRestante--;
                }
            }

            // El impacto excava el cráter (una sola vez, cuando la tromba ya
            // está tocando el suelo): el lodo se recoge en su propia herida.
            if (!_craterAbierto && t > 1.1f)
            {
                _craterAbierto = true;
                for (int x = SimLevelBuilder.FundacionCraterX0; x <= SimLevelBuilder.FundacionCraterX1; x++)
                    for (int y = SimLevelBuilder.FundacionY0 - 2; y < SimLevelBuilder.FundacionY0; y++)
                        grid.SetCell(x, y, MaterialId.Empty);
                grid.WakeChunk(SimLevelBuilder.FundacionCraterX0, SimLevelBuilder.FundacionY0, tick);
                grid.WakeChunk(SimLevelBuilder.FundacionCraterX1, SimLevelBuilder.FundacionY0, tick);
            }

            if (t > 2.6f && !_lodoActivo)
            {
                _lodoActivo = true; // el goteo permanente queda vivo (TickLodo).
                Decir(VozLodo);
            }

            if (t > 3.4f)
            {
                SimRenderer.FocoCinematico = null; // la cámara vuelve a ti.
                CambiarBeat(Beat.Lodo);
            }
        }
        private bool _derrumbeArrancado;

        /// <summary>5c) Experimentar con el lodo (sin repetir tutorial: ya sabes el verbo). El TRÁELO llega cuando de verdad lo probaste — o si te entretienes demasiado.</summary>
        private void TickLodoBeat()
        {
            // (revisión Opus 73 #5) El LODO. termina de decirse antes de que
            // el umbral pueda disparar el TRÁELO. (si el jugador ya aspiró en
            // plena tromba, las dos palabras se pisaban).
            if (_vozTexto != null) return;
            if (_flask.GetCount(Lodo) >= LodoProbarMeta || _tBeat > LodoLibreSeg)
            {
                CambiarBeat(Beat.EntregaLodo);
                Decir(VozTraelo);
            }
        }

        /// <summary>6) LA RECOMPENSA: el mundo responde — el depósito emerge, se llena a la vista, y amanece. El grifo antiguo queda conceptualmente sustituido.</summary>
        private void TickRecompensa()
        {
            float celda = SimRenderer.CellWorldSize;
            if (!_recompensaArrancada && _tBeat > 1.0f)
            {
                _recompensaArrancada = true;
                Decir(VozObserva);
                SimRenderer.FocoCinematico = new Vector3(
                    (SimLevelBuilder.FundacionDepositoX0 + SimLevelBuilder.FundacionDepositoX1 + 1) * 0.5f * celda,
                    (SimLevelBuilder.FundacionDepositoY0 + 8) * celda, 0f);
                SimRenderer.Sacudida = 0.9f; // temblor corto: algo se mueve bajo el suelo.
                _deposito.Aparecer();
            }

            // (R74) La cámara y el arco esperan a que el tanque asiente y
            // reciba su CULO de agua — poca a propósito: la tarea final es
            // llenarlo tú (ya no hay autofill; la corrección de Cesar).
            if (_recompensaArrancada && (_deposito.CargaLista || _tBeat > 10f))
            {
                SimRenderer.FocoCinematico = null;
                CambiarBeat(Beat.LlenarDeposito);
                Decir(VozLlenalo);
            }
        }
        private bool _recompensaArrancada;

        /// <summary>
        /// (R74) LA TAREA FINAL — LLÉNALO.: el verbo por última vez, ahora al
        /// revés: verter DENTRO de algo tuyo. La cascada sigue viva al otro
        /// lado de la caverna: ida y vuelta con el frasco hasta que el vidrio
        /// marque el nivel. Al lograrlo: BIEN., amanece, y el tablón del
        /// Maestro despierta (Trueque) — el greybox queda abierto.
        /// </summary>
        private void TickLlenarDeposito()
        {
            if (_tBeat < VozHoldSeg) return; // el LLÉNALO. se dice entero.
            if (_deposito.AguaDentro() >= LlenarDepositoMeta)
            {
                Decir(VozBien);
                _radioObjetivo = UiStyles.S(RadioAmanecer); // amanece: el mundo respondió.
                Trueque.Activar(); // el tablón despierta con el amanecer, como promete el bootstrap (regla 49).
                CambiarBeat(Beat.Fin);
            }
        }

        // ------------------------------------------------------------------
        // El mundo vivo: manantial, poza, lodo, cuenco.
        // ------------------------------------------------------------------
        /// <summary>
        /// EL MANANTIAL (ronda 73): brota de la grieta del muro izquierdo y
        /// baja FLUYENDO por las dos repisas hasta la poza — el agua se
        /// presenta en movimiento, con física a la vista, desde el minuto
        /// cero. PaintStable (regla 22: lo que CREA materia). El caudal nunca
        /// se detiene: el rezumado de la poza (abajo) es quien cierra el
        /// ciclo, igual que la RACIÓN de los caños del taller.
        /// </summary>
        private void TickManantial()
        {
            _manantialTimer -= Time.deltaTime;
            if (_manantialTimer > 0f) return;
            _manantialTimer = ManantialSeg;
            for (int i = 0; i < ManantialCeldas; i++)
                _sim.PaintStable(SimLevelBuilder.FundacionManantialX,
                    SimLevelBuilder.FundacionManantialY + i, 0, MaterialId.Water);
        }

        /// <summary>Cuenta celdas de `mat` en la poza (cuenco + 2 filas sobre el borde).</summary>
        private int ContarEnPoza(byte mat)
        {
            int n = 0;
            var grid = _sim.Grid;
            for (int x = SimLevelBuilder.FundacionCharcoX0; x <= SimLevelBuilder.FundacionCharcoX1; x++)
                for (int y = SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo; y <= SimLevelBuilder.FundacionY0 + 1; y++)
                    if (grid.GetMat(x, y) == mat) n++;
            return n;
        }

        /// <summary>
        /// (v1, conservado) El fondo de la poza se bebe el exceso: una celda
        /// del fondo por frame hasta el nivel de equilibrio. La poza es
        /// incapaz de desbordar; la cascada es incapaz de agotarse.
        /// </summary>
        private void RezumarPoza()
        {
            // (revisión Opus 73 #11) A CADENCIA PROPIA, no por frame: era el
            // único proceso del director atado al framerate — el nivel de la
            // poza dependía de los fps de la máquina.
            _rezumaTimer -= Time.deltaTime;
            if (_rezumaTimer > 0f) return;
            _rezumaTimer = 1f / 30f; // una celda por tick de sim, como mucho.

            int agua = ContarEnPoza(MaterialId.Water);
            if (agua <= PozaLlenaCeldas) return;

            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            for (int y = SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo; y <= SimLevelBuilder.FundacionY0 + 1; y++)
                for (int x = SimLevelBuilder.FundacionCharcoX0; x <= SimLevelBuilder.FundacionCharcoX1; x++)
                    if (grid.GetMat(x, y) == MaterialId.Water)
                    {
                        grid.SetCell(x, y, MaterialId.Empty);
                        grid.WakeChunk(x, y, tick);
                        return; // UNA celda por pulso: el nivel respira.
                    }
        }
        private float _rezumaTimer;

        /// <summary>
        /// LA GOTERA DE LODO: viva desde el derrumbe, para siempre (fuente no
        /// agotable, como pidió Cesar para los materiales básicos) — pero con
        /// tope de montículo e histéresis: si nadie se lo lleva, el goteo
        /// espera; en cuanto el jugador aspira, vuelve.
        /// </summary>
        private void TickLodo()
        {
            if (!_lodoActivo) return;
            _lodoTimer -= Time.deltaTime;
            if (_lodoTimer > 0f) return;
            _lodoTimer = LodoSeepSeg;

            int monticulo = ContarLodoEnCrater();
            if (_lodoPausado) { if (monticulo <= LodoMonticuloResume) _lodoPausado = false; else return; }
            else if (monticulo >= LodoMonticuloTope) { _lodoPausado = true; return; }

            _lodoIdx++;
            _sim.PaintStable(SimLevelBuilder.FundacionDerrumbeX + (_lodoIdx % 3) - 1,
                SimLevelBuilder.FundacionY1, 0, Lodo);
        }
        private bool _lodoPausado;

        private int ContarLodoEnCrater()
        {
            int n = 0;
            var grid = _sim.Grid;
            for (int x = SimLevelBuilder.FundacionCraterX0 - 3; x <= SimLevelBuilder.FundacionCraterX1 + 3; x++)
                for (int y = SimLevelBuilder.FundacionY0 - 2; y <= SimLevelBuilder.FundacionY0 + 6; y++)
                    if (grid.GetMat(x, y) == Lodo) n++;
            return n;
        }

        /// <summary>
        /// Cuenta celdas de `mat` dentro del cuenco del Maestro (+2 filas
        /// sobre el borde). (revisión Opus 73 #2) Para el LODO también vale su
        /// forma MOJADA: la arcilla es soluble por decreto de la seed de
        /// autor, así que el lodo vertido sobre agua residual se vuelve
        /// barbotina EN el cuenco — sin este crédito, celdas entregadas
        /// desaparecían del contador delante del jugador.
        /// </summary>
        private static readonly byte LodoMojado = MaterialId.MatDe(1, EstadoMateria.Solucion);
        private int ContarEnCuenco(byte mat)
        {
            int n = 0;
            var grid = _sim.Grid;
            for (int x = SimLevelBuilder.FundacionCuencoX0; x <= SimLevelBuilder.FundacionCuencoX1; x++)
                for (int y = SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo; y <= SimLevelBuilder.FundacionY0 + 1; y++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == mat || (mat == Lodo && m == LodoMojado)) n++;
                }
            return n;
        }

        /// <summary>El Maestro TOMA la entrega: el cuenco se vacía celda a celda (visible), y al quedar limpio el arco avanza.</summary>
        private void TickDrenarCuenco()
        {
            if (!_drenando) return;
            _drenarTimer -= Time.deltaTime;
            if (_drenarTimer > 0f) return;
            _drenarTimer = DrenarCadaSeg;

            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            for (int y = SimLevelBuilder.FundacionY0 + 1; y >= SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo; y--)
                for (int x = SimLevelBuilder.FundacionCuencoX0; x <= SimLevelBuilder.FundacionCuencoX1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == _drenandoMat || (_drenandoMat == Lodo && m == LodoMojado)) // el Maestro toma también el lodo mojado (ver ContarEnCuenco).
                    {
                        grid.SetCell(x, y, MaterialId.Empty);
                        grid.WakeChunk(x, y, tick);
                        return;
                    }
                }

            // Nada que drenar: el Maestro terminó.
            _drenando = false;
            CambiarBeat(_beatTrasDrenar);
        }

        // ------------------------------------------------------------------
        // El frasco volando de la mesa a la mano (el TOMA. hecho imagen).
        // ------------------------------------------------------------------
        private void LanzarVueloDelFrasco()
        {
            var go = new GameObject("FrascoEnVuelo");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MaquinariaSprites.VidrioRedoma();
            sr.sortingOrder = Capas.CarryEnMano;
            float celda = SimRenderer.CellWorldSize;
            go.transform.localScale = Vector3.one * (1.4f * celda * 6f / Mathf.Max(1f, sr.sprite.rect.width));
            go.transform.position = PosMaestro() + new Vector3(0f, 0.35f, 0f);
            _frascoVuelo = go.transform;
            _tVuelo = 0f;
        }

        private void TickVueloDelFrasco()
        {
            if (_frascoVuelo == null) return;
            _tVuelo += Time.deltaTime / EntregaFrascoSeg;
            float t = Mathf.Clamp01(_tVuelo);
            float ease = t * t * (3f - 2f * t); // smoothstep: sale suave, llega suave.

            Vector3 a = PosMaestro() + new Vector3(0f, 0.35f, 0f);
            Vector3 b = _aprendiz.position + new Vector3(0f, -0.2f, 0f);
            Vector3 m = Vector3.Lerp(a, b, 0.5f) + new Vector3(0f, 0.7f, 0f); // comba por arriba.
            Vector3 p = Vector3.Lerp(Vector3.Lerp(a, m, ease), Vector3.Lerp(m, b, ease), ease);
            _frascoVuelo.position = p;

            if (t >= 1f)
            {
                Destroy(_frascoVuelo.gameObject);
                _frascoVuelo = null;
                FrascoBloqueado = false;      // el tarro del aprendiz aparece: AHORA llevas frasco.
                HudPermitido = true;
                DayCycle.DespertarHudFundacion();
                _tBeat = 0f; // (revisión Opus 73 #9) TrasTomaSeg cuenta desde AQUÍ: es el respiro con el frasco ya en la mano, no menos el vuelo.
            }
        }

        // ------------------------------------------------------------------
        // La silueta del Maestro y su fuego real (conservados de la v1).
        // ------------------------------------------------------------------
        private GameObject _maestroGo;
        private Texture2D _maestroTex;

        private Vector3 PosMaestro()
        {
            float celda = SimRenderer.CellWorldSize;
            return new Vector3((SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda,
                (SimLevelBuilder.FundacionMesaTopY + 4) * celda, 0f);
        }

        private void SpawnMaestroSilueta()
        {
            float celda = SimRenderer.CellWorldSize;
            const int W = 12, H = 16; // 2 px por celda: 6x8 celdas en el mundo.
            _maestroTex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[W * H];
            var tunica = new Color32(54, 39, 26, 255);
            var pliegue = new Color32(92, 66, 41, 255); // luz de borde del lado del fuego.
            var ojo = new Color32(255, 176, 96, 255);
            for (int y = 0; y < H; y++)
            {
                float t = y / (float)(H - 1);
                float semiancho = Mathf.Lerp(5.4f, 2.2f, t * t);
                for (int x = 0; x < W; x++)
                {
                    bool dentro = Mathf.Abs(x - 5.5f) <= semiancho;
                    if (!dentro) continue;
                    bool flancoAlFuego = x <= 3 && y > 2 && y < H - 4;
                    px[y * W + x] = flancoAlFuego ? pliegue : tunica;
                }
            }
            px[10 * W + 3] = ojo;
            px[10 * W + 5] = ojo;
            _maestroTex.SetPixels32(px);
            _maestroTex.Apply(false, true);

            _maestroGo = new GameObject("Maestro_Silueta");
            var sr = _maestroGo.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(_maestroTex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), W / (6f * celda));
            sr.sortingOrder = 30;
            float mx = (SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda;
            _maestroGo.transform.position = new Vector3(mx, (SimLevelBuilder.FundacionMesaTopY + 1) * celda, 0f);
        }

        private float DistAlMaestro()
        {
            float celda = SimRenderer.CellWorldSize;
            float mx = (SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda;
            float my = (SimLevelBuilder.FundacionMesaTopY + 2) * celda;
            return Vector2.Distance(_aprendiz.position, new Vector2(mx, my)) / celda;
        }

        /// <summary>El lecho de brasas y las lenguas de Fire reales del hogar del Maestro (v1 intacta: la luz tiene causa física o no existe).</summary>
        private void MantenerFuegoDelMaestro()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            for (int x = SimLevelBuilder.FundacionBrasasX0; x <= SimLevelBuilder.FundacionBrasasX1; x++)
            {
                for (int y = SimLevelBuilder.FundacionY0; y <= SimLevelBuilder.FundacionBrasasY; y++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.mat[idx] != MaterialId.Brasa)
                    {
                        grid.SetCell(x, y, MaterialId.Brasa);
                    }
                    grid.aux[idx] = BrasaVida;
                    if (grid.temp[idx] < BrasaRaw) grid.temp[idx] = BrasaRaw;
                }
            }
            grid.WakeChunk(SimLevelBuilder.FundacionBrasasX0, SimLevelBuilder.FundacionBrasasY, tick);
            grid.WakeChunk(SimLevelBuilder.FundacionBrasasX1, SimLevelBuilder.FundacionBrasasY, tick);

            int nFire = ContarFuego(SimLevelBuilder.FundacionBrasasX0, SimLevelBuilder.FundacionBrasasX1,
                SimLevelBuilder.FundacionBrasasY + 1, SimLevelBuilder.FundacionBrasasY + 3);

            _flamaTimer -= Time.deltaTime;
            if (nFire < 3 && _flamaTimer <= 0f)
            {
                _flamaTimer = FlamaSegundos;
                _flamaIdx = (_flamaIdx + 1) % 5;
                _sim.PaintStable(SimLevelBuilder.FundacionBrasasX0 + _flamaIdx,
                    SimLevelBuilder.FundacionBrasasY + 1, 0, MaterialId.Fire);
            }
            _flama2Timer -= Time.deltaTime;
            if (nFire < 2 && _flama2Timer <= 0f)
            {
                _flama2Timer = Flama2Segundos;
                _sim.PaintStable(SimLevelBuilder.FundacionBrasasX0 + ((_flamaIdx + 2) % 5),
                    SimLevelBuilder.FundacionBrasasY + 2, 0, MaterialId.Fire);
            }

            float objetivo = 0.92f + 0.03f * Mathf.Min(nFire, 3);
            _luzFuego = Mathf.Lerp(_luzFuego, objetivo, Time.deltaTime * 9f);

            // EL TIRO DEL HOGAR: por encima de +6 filas la llama se apaga.
            for (int x = SimLevelBuilder.FundacionBrasasX0 - 1; x <= SimLevelBuilder.FundacionBrasasX1 + 1; x++)
                for (int y = SimLevelBuilder.FundacionBrasasY + 7; y <= SimLevelBuilder.FundacionBrasasY + 14; y++)
                    if (grid.GetMat(x, y) == MaterialId.Fire)
                        grid.SetCell(x, y, MaterialId.Empty);
        }

        private int ContarFuego(int x0, int x1, int y0, int y1)
        {
            int n = 0;
            var grid = _sim.Grid;
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (grid.GetMat(x, y) == MaterialId.Fire) n++;
            return n;
        }

        // ------------------------------------------------------------------
        // La viñeta + la luz (conservadas de la v1) y el dibujo del director.
        // ------------------------------------------------------------------
        private static readonly Color VinetaExterior = new Color(0.082f, 0.059f, 0.043f, 1f);
        private static readonly Color VinetaMedia = new Color(0.125f, 0.086f, 0.052f, 1f);
        private const float VinetaSquashY = 0.82f;

        /// <summary>Alfa de "cuánta luz hay" en un punto del mundo (0.25..1). Lo consume también Game/Trueque.cs.</summary>
        public static float LuzEn(Vector3 posMundo)
        {
            var inst = _instancia;
            if (inst == null || inst._radio > Screen.width + Screen.height) return 1f;
            var cam = Camera.main; if (cam == null) return 1f;
            Vector3 f = cam.WorldToScreenPoint(inst._focoActual);
            Vector3 p = cam.WorldToScreenPoint(posMundo);
            float d = Vector2.Distance(new Vector2(p.x, p.y), new Vector2(f.x, f.y));
            return Mathf.Clamp(1.3f - d / Mathf.Max(1f, inst._radio), 0.25f, 1f);
        }
        private static FundacionDirector _instancia;
        private Vector3 _focoActual;

        /// <summary>LA BANDA DEL MAESTRO (arriba-centro): la sigue usando Game/Trueque.cs para los avisos del tendero.</summary>
        public static void DibujarBandaMaestro(string texto)
        {
            UiStyles.Preparar();
            if (_bandaTitulo == null)
            {
                _bandaTitulo = new GUIStyle(UiStyles.Titulo) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(UiStyles.S(9f)) };
                _bandaTitulo.normal.textColor = UiStyles.Oro;
                _bandaCuerpo = new GUIStyle(UiStyles.Cuerpo) { alignment = TextAnchor.UpperCenter, wordWrap = true, fontSize = Mathf.RoundToInt(UiStyles.S(11f)) };
            }
            float w = Screen.width * 0.46f;
            float x = (Screen.width - w) * 0.5f;
            float altoTexto = _bandaCuerpo.CalcHeight(new GUIContent(texto), w - UiStyles.S(24f));
            float h = UiStyles.S(19f) + altoTexto + UiStyles.S(9f);
            var r = new Rect(x, UiStyles.S(18f), w, h);

            var blanco = Texture2D.whiteTexture; var prev = GUI.color;
            GUI.color = new Color(UiStyles.Pergamino.r, UiStyles.Pergamino.g, UiStyles.Pergamino.b, 0.92f);
            GUI.DrawTexture(r, blanco);
            GUI.color = UiStyles.Laton;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width, 1f), blanco);
            GUI.DrawTexture(new Rect(r.x, r.yMax - 1f, r.width, 1f), blanco);
            GUI.color = prev;

            GUI.Label(new Rect(r.x, r.y + UiStyles.S(3f), r.width, UiStyles.S(13f)), UiStyles.Espaciar("EL MAESTRO"), _bandaTitulo);
            GUI.Label(new Rect(r.x + UiStyles.S(12f), r.y + UiStyles.S(18f), r.width - UiStyles.S(24f), altoTexto), texto, _bandaCuerpo);
        }
        private static GUIStyle _bandaTitulo, _bandaCuerpo;

        private void OnGUI()
        {
            GUI.depth = 50; // detrás de todos los HUD (0), encima del mundo.

            DibujarVineta();

            float celda = SimRenderer.CellWorldSize;

            // La chapa "EL MAESTRO" sobre la mesa (se oculta de cerca: de ahí
            // en adelante hablan la silueta y la voz).
            if (_beat != Beat.Fin && DistAlMaestro() >= 14f && !DayCycle.InputLocked)
            {
                var ancla = new Vector3((SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda,
                    (SimLevelBuilder.FundacionMesaTopY + 11) * celda, 0f);
                float alfa = LuzEn(ancla) * 0.85f;
                UiStyles.PlacaMundo(ancla, "EL MAESTRO", new Color(0.92f, 0.86f, 0.7f, alfa), UiStyles.S(10f));
            }

            // La placa del CUENCO: solo mientras hay una entrega pendiente —
            // el gesto ya lo sabes; esta placa solo dice DÓNDE y CUÁNTO.
            if ((_beat == Beat.EntregaAgua || _beat == Beat.EntregaLodo) && !_drenando && !DayCycle.InputLocked)
            {
                byte mat = _beat == Beat.EntregaAgua ? MaterialId.Water : Lodo;
                int meta = _beat == Beat.EntregaAgua ? EntregaAguaMeta : EntregaLodoMeta;
                int n = Mathf.Min(ContarEnCuenco(mat), meta);
                var ancla = new Vector3((SimLevelBuilder.FundacionCuencoX0 + SimLevelBuilder.FundacionCuencoX1) * 0.5f * celda,
                    (SimLevelBuilder.FundacionY0 + 3) * celda, 0f);
                float alfa = Mathf.Max(LuzEn(ancla), 0.6f);
                UiStyles.PlacaMundo(ancla, "AQUÍ — " + n + " / " + meta, new Color(0.95f, 0.88f, 0.68f, alfa), UiStyles.S(9f));
            }

            // (R74) La placa del DEPÓSITO durante el LLÉNALO. final: mismo
            // lenguaje que la del cuenco — dónde y cuánto, nada más.
            if (_beat == Beat.LlenarDeposito && !DayCycle.InputLocked)
            {
                int n = Mathf.Min(_deposito.AguaDentro(), LlenarDepositoMeta);
                var ancla = _deposito.CentroMundo() + new Vector3(0f, 7f * celda, 0f);
                float alfa = Mathf.Max(LuzEn(ancla), 0.6f);
                UiStyles.PlacaMundo(ancla, "LLÉNALO — " + n + " / " + LlenarDepositoMeta, new Color(0.95f, 0.88f, 0.68f, alfa), UiStyles.S(9f));
            }

            if (!DayCycle.InputLocked) DibujarVoz();
        }

        /// <summary>
        /// LA VOZ EN PANTALLA: una palabra enorme, serena, centro-alto, con
        /// sombra — narrativa pura, sin panel: no es UI, es una presencia.
        /// Sube unos píxeles mientras vive y se desvanece sola. Nunca bloquea
        /// el input (las palabras del Maestro son órdenes, no trámites).
        /// </summary>
        private void DibujarVoz()
        {
            if (_vozTexto == null) return;
            PrepararEstiloVoz();

            float inDur = 0.45f, outDur = 0.8f;
            float alfa;
            if (_vozT < inDur) alfa = _vozT / inDur;
            else if (_vozT > _vozDur - outDur) alfa = (_vozDur - _vozT) / outDur;
            else alfa = 1f;
            alfa = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(alfa));

            float deriva = UiStyles.S(10f) * (_vozT / _vozDur); // sube despacio: respira.
            string texto = UiStyles.Espaciar(_vozTexto);
            var r = new Rect(0f, Screen.height * 0.24f - deriva, Screen.width, Screen.height * 0.12f);

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f * alfa);
            GUI.Label(new Rect(r.x + UiStyles.S(2f), r.y + UiStyles.S(3f), r.width, r.height), texto, _vozStyle);
            GUI.color = new Color(0.94f, 0.89f, 0.78f, alfa);
            GUI.Label(r, texto, _vozStyle);
            GUI.color = prev;
        }

        private static GUIStyle _vozStyle;
        private static int _vozAlto;
        private static void PrepararEstiloVoz()
        {
            if (_vozStyle != null && _vozAlto == Screen.height) return;
            _vozAlto = Screen.height;
            UiStyles.Preparar();
            _vozStyle = new GUIStyle(UiStyles.Titulo)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(Screen.height * 0.056f),
            };
        }

        private void DibujarVineta()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (_vineta == null) ConstruirVineta();

            float celda = SimRenderer.CellWorldSize;
            var brasas = new Vector3((SimLevelBuilder.FundacionBrasasX0 + 2.5f) * celda,
                (SimLevelBuilder.FundacionBrasasY + 2) * celda, 0f);

            if (_radio > Screen.width + Screen.height) return; // ya amaneció.

            // Tu atención es tu lámpara — salvo en el VEN., donde la luz se
            // estira hacia el fuego del Maestro (la única señal del rumbo), y
            // en los planos cinematográficos, donde la luz acompaña a la
            // cámara (revisión Opus 73 #17: el derrumbe ocurría a media luz
            // fuera del óvalo del jugador).
            _focoActual = SimRenderer.FocoCinematico.HasValue
                ? SimRenderer.FocoCinematico.Value
                : Vector3.Lerp(brasas, _aprendiz.position, _focoBias);
            Vector3 p = cam.WorldToScreenPoint(_focoActual);
            float cx = p.x, cy = Screen.height - p.y;

            float r = _radio * _luzFuego;
            float ry = r * VinetaSquashY;
            var agujero = new Rect(cx - r, cy - ry, r * 2f, ry * 2f);
            GUI.DrawTexture(agujero, _vineta);

            var blanco = Texture2D.whiteTexture;
            var prev = GUI.color; GUI.color = VinetaExterior;
            float ov = 2f;
            if (agujero.yMin > -ov) GUI.DrawTexture(new Rect(0, 0, Screen.width, agujero.yMin + ov), blanco);
            if (agujero.yMax < Screen.height + ov) GUI.DrawTexture(new Rect(0, agujero.yMax - ov, Screen.width, Screen.height - agujero.yMax + ov), blanco);
            if (agujero.xMin > -ov) GUI.DrawTexture(new Rect(0, agujero.yMin, agujero.xMin + ov, agujero.height), blanco);
            if (agujero.xMax < Screen.width + ov) GUI.DrawTexture(new Rect(agujero.xMax - ov, agujero.yMin, Screen.width - agujero.xMax + ov, agujero.height), blanco);
            GUI.color = prev;
        }

        private void ConstruirVineta()
        {
            const int N = 256;
            _vineta = new Texture2D(N, N, TextureFormat.RGBA32, false);
            _vineta.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[N * N];
            float half = N * 0.5f;
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.42f, 0.78f, d));
                    float tCol = Mathf.InverseLerp(0.42f, 1f, d);
                    Color c = Color.Lerp(VinetaMedia, VinetaExterior, tCol);
                    px[y * N + x] = new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(a * 255f));
                }
            }
            _vineta.SetPixels32(px);
            _vineta.Apply(false, true);
        }
    }
}
