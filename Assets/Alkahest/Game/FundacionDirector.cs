using System.Collections.Generic;
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
        // (R83, capítulo 2 — FASE A) Recompensa2/LlenarDeposito2: el SILO del
        // lodo emerge tras llenarse el tanque de agua, y llenarlo de barro es
        // la nueva penúltima tarea. El REORDEN y LA OBRA (fases B/C del plan,
        // docs/PLAN_PROLOGO_CAP2.md) se insertarán entre LlenarDeposito2 y
        // Fin; hasta entonces el arco cierra igual que siempre (amanecer +
        // Trueque) para que el juego nunca quede a medias entre fases.
        private enum Beat { Despertar, Ven, Toma, Agua, EntregaAgua, Derrumbe, Lodo, EntregaLodo, Recompensa, LlenarDeposito, Recompensa2, LlenarDeposito2, Reorden, Obra, Acomodo, Adios, Fin }
        private enum AguaSub { Ir, Aspirar, Verter, Libre }

        // ==================================================================
        // EL GUION vive ahora en un ASSET (ronda 75, la escenificación):
        // GuionDelPrologo.asset — textos, cantidades, tiempos, triggers,
        // radios de luz, caudales y layout de la UI se editan desde el
        // INSPECTOR, sin pedir código. Los DEFAULTS (y la documentación de
        // cada número) viven en Game/GuionDelPrologo.cs; si el asset falta,
        // GuionEfectivo entrega esos mismos defaults en memoria y el prólogo
        // corre igual (fallback invisible, escenificación reversible).
        // `_g` es de SOLO LECTURA para el director: el código jamás escribe
        // en el guion.
        // ==================================================================
        private GuionDelPrologo _g;
        private PrologoEscenografia _escena;

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
        private ApprenticeController _aprendizCtrl; // (R81) para CarryAnchor (la mano real).
        private TutorialContextual _tutorial;
        private DepositoDeAgua _deposito;
        private DepositoDeAgua _silo; // (R83) el segundo recipiente: lodo.
        private AudioSource _audio;

        // (R81, revisión Opus #15) LA VERDAD DE INSTANCIA del frasco: un
        // hot-reload de scripts en Play (domain reload) resetea las
        // ESTÁTICAS a su default — FrascoBloqueado caía a false y la mira
        // aparecía sola a mitad de la intro. Los campos de instancia de un
        // MonoBehaviour sí sobreviven al reload: este bool es la verdad y
        // Update re-afirma la estática desde él cada frame.
        private bool _frascoEntregado;

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

        // (R77) El rumor de la cascada: bucle GrifoLiquido anclado al
        // manantial, con caída cuadrática por distancia (el mismo perfil que
        // las voces de grifo de DirectorDeAudio) × VolumenEfectos. Vive aquí
        // y no en DirectorDeAudio porque es materia del prólogo: nace y muere
        // con el director.
        private AudioSource _cascadaFuente;

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
            // (RONDA 76) EL PRÓLOGO ES SOLO-SINGLE: si alguien llegara a
            // instanciar este director en la escena de red (hoy es imposible
            // por la bifurcación del bootstrap; esta guarda protege el
            // mañana), se autodestruye con acuse en vez de dirigir un
            // tutorial sobre una sim espejada que no le pertenece.
            if (Alkahest.Net.SimSync.EnEscena)
            {
                Debug.LogError("[TenThousandYears] FundacionDirector en escena MULTI: el prólogo es solo-single. Director abortado.");
                Destroy(gameObject);
                return;
            }

            // (orders/saber se aceptan por compatibilidad de firma con el
            // bootstrap; el prólogo rehecho no encola pedidos — el receptor
            // es físico — ni bautiza nada todavía.)
            _sim = sim;
            _flask = flask;
            _aprendiz = aprendiz;
            _aprendizCtrl = aprendiz.GetComponent<ApprenticeController>(); // (R81) el haz de presentación sale de la MANO real (CarryAnchor).
            _posAnterior = aprendiz.position;

            // (RONDA 75) LA ESCENOGRAFÍA Y EL GUION, ANTES QUE NADA: todo lo
            // de abajo lee de `_g`, y los marcadores de escena deciden dónde
            // vive el Maestro. Sin escenografía en la escena (sandbox,
            // escena vieja) los fallbacks reconstruyen el prólogo histórico.
            _escena = PrologoEscenografia.Buscar();
            _g = PrologoEscenografia.GuionEfectivo(_escena);

            SpawnMaestro();
            // (R93, Cesar: "una figura etérea, con algo de movimiento") El
            // sprite del Maestro (el propio o el que vistió la escena) queda
            // en manos de TickMaestroEtereo: flote, aliento y presencia.
            _maestroSr = _maestroTr != null ? _maestroTr.GetComponentInChildren<SpriteRenderer>() : null;
            if (_maestroSr != null)
            {
                _maestroSrBase = _maestroSr.transform.localPosition;
                _maestroColorBase = _maestroSr.color;
            }
            HudPermitido = false;
            FrascoBloqueado = true;

            _tutorial = new GameObject("TutorialContextual").AddComponent<TutorialContextual>();
            _tutorial.Init(aprendiz, _g.fichasOffsetPx);

            _deposito = new GameObject("DepositoDeAgua").AddComponent<DepositoDeAgua>();
            _deposito.Init(sim);

            // (R83, capítulo 2) EL SILO DEL LODO: nace oculto como el tanque;
            // lo despierta el beat Recompensa2 al completarse el agua.
            _silo = new GameObject("SiloDeLodo").AddComponent<DepositoDeAgua>();
            _silo.InitSilo(sim);

            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;

            // (R77) El rumor de la cascada arranca con el mundo — el agua
            // corre desde el minuto cero (TickAgua: "la cascada ya corría"),
            // así que su sonido también: es la pista sonora que INVITA a
            // acercarse antes de que la voz la nombre. Volumen 0 de inicio;
            // TickCascadaAudio lo lleva por distancia.
            _cascadaFuente = gameObject.AddComponent<AudioSource>();
            _cascadaFuente.playOnAwake = false;
            _cascadaFuente.spatialBlend = 0f;
            _cascadaFuente.loop = true;
            _cascadaFuente.clip = Audio.SintetizadorSfx.GrifoLiquido;
            _cascadaFuente.volume = 0f;
            _cascadaFuente.Play();

            _radioObjetivo = UiStyles.S(_g.radioDespertar);
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
            if (_lucecitaTex != null) Destroy(_lucecitaTex); // (R79) la lucecita muere con el director (el haz de presentación comparte esta textura: nada más que liberar — revisión Opus R81 #11 resuelto por diseño).
            if (_aroTex != null) Destroy(_aroTex);           // (R81) y el aro de la boca.
            if (_maestroVisualPropio && _maestroGo != null) Destroy(_maestroGo); // el visual que creó el director; un marcador de ESCENA jamás se destruye.
            if (_maestroTex != null) Destroy(_maestroTex);
            if (_frascoVuelo != null) Destroy(_frascoVuelo.gameObject);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;

            // (R81, revisión Opus #15) Re-afirmar la estática desde la verdad
            // de instancia — ANTES de la guarda de pausa, para que el estado
            // sea correcto también con el menú delante tras un hot-reload.
            FrascoBloqueado = !_frascoEntregado;

            // (revisión Opus 73 #1) CON EL MENÚ DE PAUSA DELANTE, EL DIRECTOR
            // ESPERA ENTERO: la sim está congelada (DayCycle.ApplyPause) pero
            // Time.deltaTime no — sin esta guarda los timers seguían corriendo
            // contra un mundo quieto: el rezumado vaciaba la poza en 0.6 s, el
            // reventón del derrumbe repintaba 3 celdas en vez de verter 26, y
            // el beat avanzaba con la cinemática sin público. Un ESC en mal
            // momento se comía el derrumbe completo.
            if (DayCycle.InputLocked)
            {
                _posAnterior = _aprendiz.position;
                if (_cascadaFuente != null) _cascadaFuente.volume = 0f; // (R77) el menú de pausa congela el mundo; su rumor también.
                return;
            }

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
            TickVueloDelCincel();  // (R93) el regalo de la herramienta, si está en el aire.
            TickMaestroEtereo();   // (R93) la figura FLOTA y respira: etérea, no clavada.
            TickCascadaAudio();
            TickHazPresentacion(); // (R81) el gesto de nacimiento del frasco, si está en curso.
            if (_motas.Count > 0) TickMotas(); // (R87) los destellos del barrido en vuelo a sus reservorios.
            if (_flashT > 0f) _flashT -= Time.deltaTime; // (R88) el flash del pulso decae.
            if (_beat != Beat.Reorden && _maestroHalo > 0f) _maestroHalo = Mathf.MoveTowards(_maestroHalo, 0f, Time.deltaTime); // el halo no sobrevive a la cinemática.
            if (_aroActivo) _aroVida += Time.deltaTime; // (R81) el latido del aro se calma con la edad (revisión Opus #13).

            _tBeat += Time.deltaTime;
            switch (_beat)
            {
                case Beat.Despertar: TickDespertar(); break;
                case Beat.Ven: TickVen(); break;
                case Beat.Toma: TickToma(); break;
                case Beat.Agua: TickAgua(); break;
                case Beat.EntregaAgua: TickEntrega(MaterialId.Water, _g.entregaAguaMeta, Beat.Derrumbe); break;
                case Beat.Derrumbe: TickDerrumbe(); break;
                case Beat.Lodo: TickLodoBeat(); break;
                case Beat.EntregaLodo: TickEntrega(Lodo, _g.entregaLodoMeta, Beat.Recompensa); break;
                case Beat.Recompensa: TickRecompensa(); break;
                case Beat.LlenarDeposito: TickLlenarDeposito(); break;
                case Beat.Recompensa2: TickRecompensa2(); break;
                case Beat.LlenarDeposito2: TickLlenarDeposito2(); break;
                case Beat.Reorden: TickReorden(); break;
                case Beat.Obra: TickObra(); break;       // (R93) ALZA.: la plataforma y el techo, con el cincel regalado.
                case Beat.Acomodo: TickAcomodo(); break; // (R93) ACOMODA.: la mudanza de los reservorios al centro nuevo.
                case Beat.Adios: TickAdios(); break;     // (R93) el ORDEN repetido, el vano, DIEZ MIL AÑOS., y el Maestro deja de mirar.
                case Beat.Fin: break; // greybox libre: el prólogo dijo lo suyo.
            }

            _posAnterior = _aprendiz.position;
            // (R88, dirección Opus) Durante la cinemática del ORDEN el radio
            // lo dicta una curva explícita (_radioForzado): el borde de la
            // LUZ es el frente del anillo y tiene que estar donde el guion
            // dice, no donde un suavizado exponencial lo deje.
            if (_radioForzado.HasValue) _radio = _radioForzado.Value;
            else _radio = Mathf.Lerp(_radio, _radioObjetivo, Time.deltaTime * 1.6f);
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
            _vozDur = 0.45f + _g.vozHoldSeg + VozFadeOutSeg; // fade in + hold + fade out.
            if (_audio != null)
            {
                // El pitch varía un pelo por palabra (determinista): la voz
                // respira sin cambiar de timbre.
                _audio.pitch = 0.96f + 0.03f * (palabra.Length % 3);
                _audio.PlayOneShot(Audio.SintetizadorSfx.VozDelMaestro,
                    0.8f * Audio.DirectorDeAudio.VolumenEfectos);
            }
        }

        /// <summary>
        /// (R88, dirección Opus #3) La palabra con TONO Y DURACIÓN propios:
        /// el pitch por longitud (0.96+len%3) dejaba a ORDEN. más neutro que
        /// TOMA. por accidente aritmético. ORDEN. = 0.82 (la nota más baja
        /// del prólogo, reservada) con hold 3.6 (la palabra sobrevive al acto
        /// entero); TOMA. = 0.90 (más cálida que el decreto, más grave que el
        /// resto: un regalo). Entra DIRECTO, sin cola: en el clímax no hay
        /// otra palabra viva que respetar.
        /// </summary>
        private void DecirConTono(string palabra, float pitch, float holdSeg)
        {
            _vozTexto = palabra;
            _vozT = 0f;
            _vozDur = 0.45f + holdSeg + VozFadeOutSeg;
            _vozPendiente = null;
            if (_audio != null)
            {
                _audio.pitch = pitch;
                _audio.PlayOneShot(Audio.SintetizadorSfx.VozDelMaestro, 0.9f * Audio.DirectorDeAudio.VolumenEfectos);
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
            if (_tBeat > _g.despertarPausaSeg && !_tutorial.Visible && !_tutorial.Terminado)
            {
                _tutorial.Mostrar(_g.leyendaMover,
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
                    if (!_moverOk[i] && _progMover[i] >= _g.moverMetaMundo)
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
            if (_tutorialCerrado && _tBeat > _g.trasTutorialSeg)
            {
                CambiarBeat(Beat.Ven);
                Decir(_g.vozVen);
                _radioObjetivo = UiStyles.S(_g.radioVen);
                // (R79, feedback de Cesar) Antes: _focoBias = 0.62 ("la luz se
                // estira hacia el fuego"). En juego real ese 38% prestado al
                // fuego dejaba al jugador en el borde de su propio óvalo en
                // cuanto usaba el WASD recién aprendido — "la luz pierde muy
                // rápido el track del personaje". Ahora la luz es casi toda
                // suya (luzBiasVen≈0.92) y el RUMBO lo señala la LUCECITA del
                // área del Maestro (DibujarLucecitaMaestro), que es además el
                // indicador que Cesar pidió: "algo está ocurriendo ahí".
                _focoBias = Mathf.Clamp01(_g.luzBiasVen);
            }
        }
        private bool _tutorialCerrado;

        /// <summary>2) VEN.: la luz insinúa el rumbo del fuego; llegar a la presencia dispara el encuentro.</summary>
        private void TickVen()
        {
            // (revisión Opus 73 #6) La palabra VEN. se dice entera aunque el
            // jugador ya esté pegado al Maestro (terminó el tutorial a sus
            // pies): el encuentro espera a que la voz suelte la palabra.
            if (_tBeat < _g.vozHoldSeg) return;
            if (DistAlMaestro() < _g.distCharla)
            {
                CambiarBeat(Beat.Toma);
                Decir(_g.vozToma);
                _radioObjetivo = UiStyles.S(_g.radioToma);
                _focoBias = 0.85f;
                LanzarVueloDelFrasco();
            }
        }

        /// <summary>3) TOMA.: el frasco vuela de la mesa a tu mano; con él llegan el HUD y el verbo.</summary>
        private void TickToma()
        {
            if (_frascoVuelo != null) return; // el vuelo sigue en el aire (TickVueloDelFrasco).
            if (!FrascoBloqueado && _tBeat > _g.trasTomaRespiroSeg)
            {
                CambiarBeat(Beat.Agua);
                _aguaSub = AguaSub.Ir;
                _radioObjetivo = UiStyles.S(_g.radioAguaLuz);
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
                    if (DistAPoza() < _g.distZonaAgua)
                    {
                        Decir(_g.vozAgua);
                        _aguaSub = AguaSub.Aspirar;
                        _aguaBase = _flask.GetCount(MaterialId.Water);
                        // (R79b) Arranque limpio del detector de mantén:
                        _aguaPrevAspirar = _aguaBase;
                        _sinSuccionSeg = 1f; _rachaManten = 0f; _mantenLogrado = false;
                        _aroActivo = true; _aroVida = 0f; // (R81, revisión Opus #2) el aro acompaña SOLO el aspirar: su radio es el de succión, y en el verter mentiría (PourRadius=2). Al verter, el tarro ya se ladea hacia el cursor — señal de sobra.
                        _tutorial.Mostrar(_g.leyendaAspirar,
                            new TutorialContextual.Paso { Etiqueta = "CLIC IZQ", Presionada = () => Mouse.current != null && Mouse.current.leftButton.isPressed });
                    }
                    break;

                case AguaSub.Aspirar:
                {
                    // (R79b, aprobado por Cesar: "que la ficha no confirme con
                    // un toque suelto sino con un MANTÉN real") El detector:
                    // una racha de succión CONTINUA — botón sostenido Y agua
                    // entrando de verdad (con 0.3 s de gracia entre celdas,
                    // porque el frasco no traga en cada frame) — de al menos
                    // aspirarHoldSeg. Se LATCHEA: lograste el mantén una vez,
                    // vale aunque el resto lo taponees a clics. La ficha solo
                    // confirma con meta CUMPLIDA + mantén LOGRADO: el gesto
                    // que celebra es el gesto que hay que aprender.
                    int cuentaA = _flask.GetCount(MaterialId.Water);
                    if (cuentaA > _aguaPrevAspirar) _sinSuccionSeg = 0f;
                    else _sinSuccionSeg += Time.deltaTime;
                    _aguaPrevAspirar = cuentaA;
                    bool succionando = Mouse.current != null && Mouse.current.leftButton.isPressed && _sinSuccionSeg < 0.3f;
                    _rachaManten = succionando ? _rachaManten + Time.deltaTime : 0f;
                    if (_rachaManten >= _g.aspirarHoldSeg) _mantenLogrado = true;

                    if (_mantenLogrado && cuentaA >= _aguaBase + _g.aspirarMeta)
                    {
                        _tutorial.Confirmar(0);
                        _aroActivo = false; // (R81, revisión Opus #2) la lección del radio de succión quedó dada; en el verter el aro mentiría.
                        _aguaSub = AguaSub.Verter;
                        _aguaBase = _flask.GetCount(MaterialId.Water);
                        _tBeat = 0f;
                    }
                    break;
                }

                case AguaSub.Verter:
                    if (!_vertidoEnsenado)
                    {
                        // Espera a que la ficha anterior termine su fade antes
                        // de enseñar la siguiente (respiración entre gestos).
                        if (_tutorial.Visible || _tBeat < 0.5f) break;
                        _vertidoEnsenado = true;
                        _tutorial.Mostrar(_g.leyendaVerter,
                            new TutorialContextual.Paso { Etiqueta = "CLIC DER", Presionada = () => Mouse.current != null && Mouse.current.rightButton.isPressed });
                        break;
                    }
                    if (_flask.GetCount(MaterialId.Water) <= _aguaBase - _g.verterMeta)
                    {
                        _tutorial.Confirmar(0);
                        _aguaSub = AguaSub.Libre;
                        _tBeat = 0f;
                        // (R79b) Arranque limpio del reloj de la ficha-recuerdo.
                        _aguaUltimaCuenta = _flask.GetCount(MaterialId.Water);
                        _sinAspirarSeg = 0f;
                    }
                    break;

                case AguaSub.Libre:
                {
                    // (R79b, aprobado por Cesar) LA FICHA-RECUERDO, una sola
                    // vez: quien aspiró a clics rápidos y luego se quedó sin
                    // saber repetir el gesto ("no sabía cómo volver a
                    // absorber") recibe, tras recordatorioAspirarSeg sin que
                    // entre agua al frasco Y todavía en la zona del agua, la
                    // ficha "CLIC IZQ — mantén" de nuevo. Si aspira, confirma
                    // con su blip; si no, se DESVANECE sola (sin celebración)
                    // a los recordatorioDuraSeg. Nunca reaparece.
                    int cuentaL = _flask.GetCount(MaterialId.Water);
                    bool aspiro = cuentaL > _aguaUltimaCuenta;
                    if (aspiro) _sinAspirarSeg = 0f; else _sinAspirarSeg += Time.deltaTime;
                    _aguaUltimaCuenta = cuentaL;

                    if (_recordatorioActivo)
                    {
                        if (aspiro)
                        {
                            _tutorial.Confirmar(0); // "sí. eso era." — y la ficha se va por su camino feliz.
                            _recordatorioActivo = false;
                            _aroActivo = false; // (R81, revisión Opus #14) el aro del recuerdo se va con su ficha.
                        }
                        else
                        {
                            _recordatorioTimer -= Time.deltaTime;
                            if (_recordatorioTimer <= 0f) { _tutorial.Desvanecer(); _recordatorioActivo = false; _aroActivo = false; }
                        }
                    }
                    else if (!_recordatorioMostrado && !_tutorial.Visible
                        && _sinAspirarSeg > _g.recordatorioAspirarSeg
                        && DistAPoza() < _g.distZonaAgua)
                    {
                        _recordatorioMostrado = true;
                        _recordatorioActivo = true;
                        _recordatorioTimer = _g.recordatorioDuraSeg;
                        // (R81, revisión Opus #14) El aro VUELVE con la ficha-recuerdo:
                        // esta ficha existe justo para quien NO interiorizó el gesto —
                        // sin el anillo que dice dónde vive la boca, volvía media lección.
                        _aroActivo = true; _aroVida = 0f;
                        _tutorial.Mostrar(_g.leyendaAspirar,
                            new TutorialContextual.Paso { Etiqueta = "CLIC IZQ", Presionada = () => Mouse.current != null && Mouse.current.leftButton.isPressed });
                    }

                    // (R77) EL JUEGO LIBRE CIERRA POR CONDUCTA, no por reloj:
                    // tras un mínimo de juego (juegoLibreMinSeg), ALEJARSE de
                    // la poza es la frase "terminé de jugar" — y ahí el
                    // Maestro pide. El reloj (juegoLibreTopeSeg) queda como tope
                    // de seguridad: si el jugador ni juega ni se va, el arco
                    // no se cuelga esperándolo para siempre.
                    if ((_tBeat > _g.juegoLibreMinSeg && DistAPoza() > _g.juegoLibreAlejarseCeldas)
                        || _tBeat > _g.juegoLibreTopeSeg)
                    {
                        if (_recordatorioActivo) { _tutorial.Desvanecer(); _recordatorioActivo = false; _aroActivo = false; } // el arco sigue; ni ficha ni aro se quedan huérfanos.
                        CambiarBeat(Beat.EntregaAgua);
                        Decir(_g.vozTraela);
                        _radioObjetivo = UiStyles.S(_g.radioTaller);
                    }
                    break;
                }
            }
        }
        private bool _vertidoEnsenado;

        // (R79b) El mantén real del aspirar + la ficha-recuerdo del juego libre:
        private int _aguaPrevAspirar;
        private float _sinSuccionSeg;
        private float _rachaManten;
        private bool _mantenLogrado;
        private int _aguaUltimaCuenta;
        private float _sinAspirarSeg;
        private bool _recordatorioMostrado;
        private bool _recordatorioActivo;
        private float _recordatorioTimer;

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
            if (_tBeat < _g.vozHoldSeg + 0.5f) return;

            if (ContarEnCuenco(mat) >= meta)
            {
                Decir(_g.vozBien);
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

            if (_tBeat < _g.derrumbePausaSeg) return; // respiro tras la entrega del agua.
            float t = _tBeat - _g.derrumbePausaSeg;

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
                _burstRestante = _g.lodoBurstCeldas;
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
                Decir(_g.vozLodo);
            }

            if (t > 3.4f)
            {
                SimRenderer.FocoCinematico = null; // la cámara vuelve a ti.
                // (R87, Cesar: "la luz debería dejar de ser focal en algún
                // momento… propongo que sea después de que cae el lodo, para
                // que se ponga a jugar") LA LUZ SE ABRE AQUÍ y no vuelve a
                // encogerse: el jugador ya vio la penumbra dramática entera
                // (despertar → voz → agua → derrumbe) y entiende que no es
                // el look permanente del juego. El amanecer PLENO sigue
                // siendo del final: esto es la sala jugable, no el sol.
                _radioObjetivo = UiStyles.S(_g.radioLodoJuego);
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
            if (_flask.GetCount(Lodo) >= _g.lodoProbarMeta || _tBeat > _g.lodoLibreSeg)
            {
                CambiarBeat(Beat.EntregaLodo);
                Decir(_g.vozTraelo);
            }
        }

        /// <summary>6) LA RECOMPENSA: el mundo responde — el depósito emerge, se llena a la vista, y amanece. El grifo antiguo queda conceptualmente sustituido.</summary>
        private void TickRecompensa()
        {
            float celda = SimRenderer.CellWorldSize;
            if (!_recompensaArrancada && _tBeat > 1.0f)
            {
                _recompensaArrancada = true;
                Decir(_g.vozObserva);
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
                Decir(_g.vozLlenalo);
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
            if (_tBeat < _g.vozHoldSeg) return; // el LLÉNALO. se dice entero.
            if (_deposito.AguaDentro() >= _g.llenarDepositoMeta)
            {
                // (R83, capítulo 2) El agua ya no cierra el arco: convoca al
                // SILO. El amanecer y el Trueque esperan al final del lodo.
                Decir(_g.vozBien);
                CambiarBeat(Beat.Recompensa2);
            }
        }

        /// <summary>
        /// (R83, FASE A) EL SILO EMERGE: misma cinemática que el tanque
        /// (OBSERVA. + foco + sacudida + salida de la tierra), junto al
        /// cráter — recolectas donde brota. Espera a `Asentado` (nace VACÍO:
        /// no hay carga que esperar) con tope `emergerTopeSeg` (Opus A9), y
        /// si el jugador estorba los muros a los 3 s, se lo dice.
        /// </summary>
        private void TickRecompensa2()
        {
            float celda = SimRenderer.CellWorldSize;
            if (!_recompensa2Arrancada && _tBeat > 1.2f)
            {
                _recompensa2Arrancada = true;
                Decir(_g.vozObserva);
                SimRenderer.FocoCinematico = _silo != null ? _silo.CentroMundo()
                    : new Vector3((SimLevelBuilder.FundacionSiloX0 + SimLevelBuilder.FundacionSiloX1 + 1) * 0.5f * celda,
                        (SimLevelBuilder.FundacionSiloY0 + 5) * celda, 0f);
                SimRenderer.Sacudida = 0.8f;
                _silo?.Aparecer();
            }

            if (_recompensa2Arrancada && !_avisoApartateDado && _tBeat > 4.2f && _silo != null && !_silo.Asentado)
            {
                // La caja anti-emparedamiento del silo está esperando al
                // jugador (el beat lo plantó ahí recogiendo lodo): decirlo.
                _avisoApartateDado = true;
                _flask.Avisar("apártate — algo sube");
            }

            if (_recompensa2Arrancada && ((_silo != null && _silo.Asentado) || _tBeat > _g.emergerTopeSeg))
            {
                SimRenderer.FocoCinematico = null;
                CambiarBeat(Beat.LlenarDeposito2);
                Decir(_g.vozLlenalo);
            }
        }
        private bool _recompensa2Arrancada;
        private bool _avisoApartateDado;

        /// <summary>(R83) LLÉNALO. de barro: la gotera del cráter es la fuente; el silo, el destino. Al lograrlo, el REORDEN (R84).</summary>
        private void TickLlenarDeposito2()
        {
            if (_tBeat < _g.vozHoldSeg) return;
            if (_silo != null && _silo.DelDueno() >= _g.llenarDeposito2Meta)
            {
                Decir(_g.vozBien);
                CambiarBeat(Beat.Reorden);
            }
        }

        // =================================================================
        // (R88 — LA DIRECCIÓN DE ESCENA DE OPUS, encargo de Cesar: "no está
        // a la altura… necesitamos lo espectacular, o discreto pero como
        // DECLARACIÓN DE OMNIPOTENCIA") EL ORDEN, reescrito entero.
        //
        // El hallazgo que tumbó la versión anterior: el barrido corría de
        // izquierda a derecha — un wipe horizontal ES una transición de
        // PowerPoint, y encima el poder viajaba HACIA el Maestro. Ahora la
        // orden NACE EN EL MAESTRO y se expande como ANILLO: el borde de la
        // LUZ es el frente — lo que la luz alcanza, obedece en ese instante.
        // La cámara se planta al centro ANTES de la palabra y no se mueve
        // hasta que todo terminó; la luz es un instrumento aparte.
        //
        // El guion (t desde la entrada al beat, BIEN. ya dicho):
        //   3.65  el VACÍO: muere el sonido del mundo (la cascada cae MUDA).
        //   4.05  la cámara se planta al centro. No se moverá.
        //   4.25  la luz se CIERRA sobre el Maestro (0.7 s): la sala se apaga
        //         desde los bordes; el único punto iluminado es su silueta.
        //   4.95  ORDEN. — pitch 0.82, la nota más baja del prólogo. CERO
        //         sacudida: el mundo no se asusta, acata.
        //   5.40  EL PULSO: la luz chasquea 320→400, flash blanco 0.22→0,
        //         SUB de 38 Hz. El tanque del Maestro (lo más cercano) se
        //         ANULA ahí mismo (drenado 0.5 s). El fondo ruina→castillo
        //         arranca AQUÍ y termina con el anillo. Fuentes muertas.
        //   5.40→ EL ANILLO (4.2 s): la luz se abre del Maestro hacia
        //         afuera (~22 c/s). Cada cosa obedece cuando el filo la
        //         toca: el cráter entrega su lodo, el silo se anula (d=50),
        //         LA POZA se levanta en sábana (el anillo frena a 15 c/s),
        //         las repisas se deshacen, EL FRENTE SE DETIENE 0.4 s con la
        //         cascada cayendo sola en el borde de la luz — lo último
        //         natural del mundo — y al final la boca del manantial SE
        //         SELLA con piedra. Ni un solo "whoosh": solo el sub.
        //   9.60  EL DESPUÉS: la caverna limpia, entera, iluminada. ORDEN.
        //         recién ahora se despide (hold 3.6: el texto muere después
        //         que el mundo).
        //  10.40  EL IRIS: la luz se cierra a S(520) sobre los dos sitios y
        //         la cámara viaja ahí — el primer plano del pixel art es la
        //         viñeta usada como lente. Dos resplandores respiran en el
        //         suelo: la carga esperando (las motas llevan 4 s cayendo).
        //  10.95  polvo ANTES que metal; el suelo avisa.
        //  11.15  EL RENACER: ambos suben JUNTOS y YA LLENOS (la espera del
        //         llenado era un progress bar; fuera). Sacudida al ASENTAR —
        //         ahí sí hay causa física.
        //   +0.7  quietud. "Ya terminó"… y no terminó.
        //         LOS TUBOS: dos columnas de cobre EMPUJAN desde el suelo y
        //         encajan. UN clank para los dos (es un acto, no dos piezas).
        //   +0.45 nada. El cobre puesto, inerte.
        //         TOMA. — pitch 0.90, el verbo del frasco: un regalo.
        //   +0.35 LA PRIMERA GOTA, sola, real, cayendo desde el tope. El 80%
        //         del "sí vi que me pusieron el refill" (dirección Opus #5).
        //         Luego la segunda, la tercera, y el ritmo de 0.8 s: la
        //         cadencia estableciéndose ES la promesa de infinito.
        //   +3.0  AMANECER: la luz se libera, la cámara vuelve, Trueque.
        //         El paisaje sonoro nuevo — goteo y fuego, sin cascada — es
        //         la prueba de que el mundo cambió de dueño.
        // =================================================================
        private int _reordenPaso;
        private float _tPaso;
        private int _bancoAgua, _bancoLodo;
        private bool _barridoEnCurso;          // compat: pausa manantial/rezumadero mientras el anillo trabaja.
        private float? _radioForzado;          // el guion manda sobre el suavizado de la viñeta.
        private Vector3? _focoDeLuz;           // la luz desacoplada de la cámara.
        private bool _cascadaMuda;             // el vacío sonoro previo a la palabra.
        private float _flashT = -1f;           // el flash del pulso (0.18 s).
        private int _colIzq, _colDer;          // las dos cabezas de limpieza del anillo.
        private float _radioCierreDesde;       // radio al empezar el cierre sobre el Maestro.
        private bool _siloAnulado, _sonRepisaB, _sonRepisaA, _manantialSellado, _asentadoMarcado, _clankHecho, _tomaDicha, _gotaActivada;
        private float _tAsentado = -1f, _tClank = -1f;
        private bool _flashazoHecho, _avisoPolvoHecho; // (R90) el trago final y su polvo, una sola vez.
        private float _tEsperaMotas = -1f, _tNegro = -1f;
        private float _flashDur = 0.18f, _flashAlfa = 0.22f, _flashHold; // (R90) el flash es paramétrico: pulso chico, flashazo grande.

        private int ColMaestro() { return Mathf.RoundToInt(PosMaestro().x / SimRenderer.CellWorldSize); }

        /// <summary>Radio de viñeta (px) que cubre `celdas` horizontales de mundo con la cámara actual.</summary>
        private float PxDeCeldas(float celdas)
        {
            var cam = Camera.main;
            float pxPorU = cam != null ? Screen.height / (2f * cam.orthographicSize) : 100f;
            return celdas * SimRenderer.CellWorldSize * pxPorU;
        }

        // (R89) EL ANILLO SE MIDE CONTRA EL PLANO, no contra una curva
        // horneada (regla 24: el ensanche del mundo la habría dejado corta).
        // Velocidades de la dirección Opus: 22 c/s de crucero, 15 sobre la
        // poza (el trago en sábana), PAUSA de 0.4 s ante la cascada (lo
        // último natural del mundo, solo en el borde de la luz) y 14 hasta
        // sellar el manantial. Con la caverna de 141, el anillo respira ~5 s.
        private float _anilloD;
        private float _anilloPausaT;
        private bool _anilloPausaHecha;

        private void AvanzarAnillo(float dt, int colM)
        {
            float dPozaIni = colM - SimLevelBuilder.FundacionCharcoX1;
            float dPozaFin = colM - SimLevelBuilder.FundacionCharcoX0;
            float dPausa = colM - (SimLevelBuilder.FundacionManantialX + 10); // la mitad de la caída: la cascada sola.
            if (!_anilloPausaHecha && _anilloD >= dPausa)
            {
                _anilloPausaT += dt;
                if (_anilloPausaT < 0.40f) return;
                _anilloPausaHecha = true;
            }
            float v = 22f;
            if (_anilloD >= dPozaIni - 1f && _anilloD <= dPozaFin + 1f) v = 15f;
            else if (_anilloPausaHecha) v = 14f;
            _anilloD += v * dt;
        }

        private void TickReorden()
        {
            float celda = SimRenderer.CellWorldSize;
            _tPaso += Time.deltaTime;
            int colM = ColMaestro();

            switch (_reordenPaso)
            {
                case 0: // EL VACÍO → el cierre sobre el Maestro → ORDEN.
                    if (_tBeat >= 3.65f && !_cascadaMuda) _cascadaMuda = true; // el mundo enmudece; la cascada cae MUDA.
                    if (_tBeat >= 4.05f && SimRenderer.FocoCinematico == null)
                        SimRenderer.FocoCinematico = new Vector3(
                            (SimLevelBuilder.FundacionX0 + SimLevelBuilder.FundacionX1) * 0.5f * celda,
                            168f * celda, 0f); // la cámara se planta en el centro REAL. No se moverá.
                    if (_tBeat >= 4.25f)
                    {
                        if (!_focoDeLuz.HasValue) { _focoDeLuz = PosMaestro(); _radioCierreDesde = _radio; }
                        float tc = Mathf.Clamp01((_tBeat - 4.25f) / 0.70f);
                        _radioForzado = Mathf.Lerp(_radioCierreDesde, UiStyles.S(320f), tc * tc); // ease-in: la sala se apaga hacia él.
                    }
                    if (_tBeat < 4.95f) return;
                    DecirConTono(_g.vozOrden, 0.82f, 3.6f); // CERO sacudida: el mundo no se asusta — acata.
                    Debug.Log("[TenThousandYears] ORDEN dicho (anillo Opus R88).");
                    _reordenPaso = 1; _tPaso = 0f;
                    break;

                case 1: // la palabra llega a alfa 1 → EL PULSO.
                    if (_tPaso < 0.45f) return;
                    _flashT = 0.18f; _flashDur = 0.18f; _flashAlfa = 0.22f; _flashHold = 0f;
                    _tEsperaMotas = -1f; _tNegro = -1f; _flashazoHecho = false; _avisoPolvoHecho = false; _maestroHalo = 0f;
                    if (_audio != null) _audio.PlayOneShot(Audio.SintetizadorSfx.SubGrave, 0.9f * Audio.DirectorDeAudio.VolumenEfectos);
                    _deposito?.RetirarRapido(); // lo más cercano y lo más suyo obedece PRIMERO, a plena luz.
                    var backdrop = FindAnyObjectByType<WorkshopBackdrop>();
                    if (backdrop != null) backdrop.TransicionAFondoTaller(4.2f); // la ruina cede CON la palabra, no después.
                    _fuentesApagadas = true;
                    _lodoActivo = false;
                    _barridoEnCurso = true;
                    _colIzq = colM; _colDer = colM + 1;
                    _anilloD = 32f; _anilloPausaT = 0f; _anilloPausaHecha = false;
                    Debug.Log("[TenThousandYears] EL PULSO: fuentes muertas, anillo en marcha desde x" + colM + ".");
                    _reordenPaso = 2; _tPaso = 0f;
                    break;

                case 2: // EL ANILLO: la luz se abre desde el Maestro; lo que toca, obedece.
                {
                    AvanzarAnillo(Time.deltaTime, colM);
                    float d = _anilloD;
                    float dFin = colM - SimLevelBuilder.FundacionX0 + 2;
                    _focoDeLuz = PosMaestro();
                    _radioForzado = Mathf.Max(UiStyles.S(400f), PxDeCeldas(d));

                    // Las dos cabezas de limpieza marchan con el filo (el corte
                    // es columnar, pero fuera del óvalo todo es negro: nadie
                    // puede ver que no es circular — coste cero, lectura circular).
                    // (Opus R90 BLOQUEA #1) Hasta X0-2/X1+2: las mordidas de
                    // muro llegan a profundidad 2 y con el tope viejo el
                    // remate quedaba chueco sin visitar.
                    while (colM - _colIzq <= d && _colIzq >= SimLevelBuilder.FundacionX0 - 2) { LimpiarColumna(_colIzq); PerfilarColumna(_colIzq); _colIzq--; }
                    while (_colDer - colM <= d && _colDer <= SimLevelBuilder.FundacionX1 + 2) { LimpiarColumna(_colDer); PerfilarColumna(_colDer); _colDer++; }
                    RetaguardiaDelAnillo(); // (R92) lo que FLUYE de vuelta detrás del frente, también obedece.

                    if (!_siloAnulado && d >= colM - SimLevelBuilder.FundacionSiloX1)
                    {
                        _siloAnulado = true;
                        _silo?.RetirarRapido();
                        if (_audio != null) { _audio.pitch = 0.7f; _audio.PlayOneShot(Audio.SintetizadorSfx.TolvaTraga, 0.6f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; }
                    }
                    if (!_sonRepisaB && _colIzq <= SimLevelBuilder.FundacionRepisaBX1)
                    {
                        _sonRepisaB = true;
                        if (_audio != null) { _audio.pitch = 1.3f; _audio.PlayOneShot(Audio.SintetizadorSfx.Derrumbe, 0.3f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; }
                    }
                    if (!_sonRepisaA && _colIzq <= SimLevelBuilder.FundacionRepisaAX1)
                    {
                        _sonRepisaA = true;
                        if (_audio != null) { _audio.pitch = 1.3f; _audio.PlayOneShot(Audio.SintetizadorSfx.Derrumbe, 0.3f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; }
                    }
                    if (!_manantialSellado && d >= dFin - 1f)
                    {
                        // La boca del manantial SE SELLA con piedra: fin de las fuentes.
                        _manantialSellado = true;
                        _sim.PaintStable(SimLevelBuilder.FundacionManantialX - 1, SimLevelBuilder.FundacionManantialY, 0, MaterialId.Stone);
                        _sim.PaintStable(SimLevelBuilder.FundacionManantialX - 1, SimLevelBuilder.FundacionManantialY + 1, 0, MaterialId.Stone);
                    }

                    if (d < dFin || _colIzq >= SimLevelBuilder.FundacionX0 - 2 || _colDer <= SimLevelBuilder.FundacionX1 + 2)
                        if (_tPaso < 9f) return; // tope de seguridad del anillo.
                    // Cierre del anillo: seguridad + bancos + obra de las repisas.
                    if (_deposito != null && !_deposito.Enterrado) _deposito.RetirarDeGolpe();
                    if (_silo != null && !_silo.Enterrado) _silo.RetirarDeGolpe();
                    _bancoAgua = _deposito != null ? _deposito.DrenadoDelDueno + _bancoAgua : _bancoAgua;
                    _bancoLodo = _silo != null ? _silo.DrenadoDelDueno + _bancoLodo : _bancoLodo;
                    if (_deposito != null) Destroy(_deposito.gameObject);
                    if (_silo != null) Destroy(_silo.gameObject);
                    _deposito = null; _silo = null;
                    _barridoEnCurso = false;
                    SimLevelBuilder.ActualizarObra(SimLevelBuilder.ObraRepisaA, 0, 0, -1, -1);
                    SimLevelBuilder.ActualizarObra(SimLevelBuilder.ObraRepisaB, 0, 0, -1, -1);
                    Debug.Log("[TenThousandYears] ANILLO completo: agua=" + _bancoAgua + " lodo=" + _bancoLodo + " al banco; repisas y manantial retirados.");
                    _reordenPaso = 3; _tPaso = 0f;
                    break;
                }

                case 3: // el trago final: ESPERA de motas → NEGRO → FLASHAZO → apertura → claro breve → IRIS → polvo → renacer.
                {
                    RetaguardiaDelAnillo(); // (R92) las colas que aterrizan durante el trago se absorben EN escena, no en el repaso.
                    // (R90, dirección Opus #5) El flashazo NO se dispara al
                    // terminar el anillo: las últimas motas siguen en vuelo.
                    // La luz se queda en el tamaño del anillo hasta que la
                    // última entra al Maestro (tope 1.3 s) — el negro cae en
                    // el fotograma del último trago.
                    if (_tEsperaMotas < 0f) _tEsperaMotas = _tPaso;
                    if (_motas.Count > 0 && _tPaso - _tEsperaMotas < 1.3f)
                    {
                        _radioForzado = PxDeCeldas(colM - SimLevelBuilder.FundacionX0 + 2);
                        _focoDeLuz = PosMaestro();
                        return;
                    }
                    if (_tNegro < 0f)
                    {
                        // EL NEGRO (0.12 s): la oscuridad total que Cesar pidió
                        // de vuelta ("me lo daba la oscuridad") — solo el sub.
                        _tNegro = _tPaso;
                        if (_audio != null) { _audio.pitch = 0.85f; _audio.PlayOneShot(Audio.SintetizadorSfx.SubGrave, 1.0f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; }
                    }
                    float tn = _tPaso - _tNegro;
                    if (tn < 0.12f)
                    {
                        _radioForzado = UiStyles.S(6f); // negro total: el mundo entero dentro del Maestro.
                        _focoDeLuz = PosMaestro();
                        return;
                    }
                    if (!_flashazoHecho)
                    {
                        // EL FLASHAZO ÉPICO: el halo del Maestro reventando —
                        // blanco 0.55 sostenido 0.06 s + decaimiento 0.30
                        // (el doble de alto y de largo que el del pulso, o
                        // leería como repetición — Opus R90). Sacudida GANADA:
                        // acaba de tragarse la geología entera.
                        _flashazoHecho = true;
                        _flashT = 0.36f; _flashDur = 0.36f; _flashAlfa = 0.55f; _flashHold = 0.06f;
                        _maestroHalo = 0f;
                        SimRenderer.Sacudida = 0.35f;
                    }
                    if (tn < 0.37f)
                    {
                        _radioForzado = Mathf.Lerp(UiStyles.S(6f), UiStyles.S(2400f), Mathf.Clamp01((tn - 0.12f) / 0.25f));
                        _focoDeLuz = null;
                        return;
                    }
                    if (tn < 1.17f) { _radioForzado = UiStyles.S(2400f); return; } // la caverna LIMPIA, perfilada: 0.8 s de revelación — no 3.
                    float tiIris = tn - 1.17f;
                    if (tiIris < 0.55f)
                    {
                        // EL IRIS: la viñeta usada como LENTE — el primer plano del pixel art.
                        SimRenderer.FocoCinematico = new Vector3(
                            (SimLevelBuilder.FundacionSiloX0 + SimLevelBuilder.FundacionDepositoX1 + 1) * 0.5f * celda,
                            (SimLevelBuilder.FundacionY0 + 9) * celda, 0f);
                        _radioForzado = Mathf.Lerp(UiStyles.S(2400f), UiStyles.S(520f), (tiIris / 0.55f) * (tiIris / 0.55f));
                        return;
                    }
                    if (tiIris < 0.75f)
                    {
                        // Polvo ANTES que metal: el suelo avisa.
                        if (!_avisoPolvoHecho && _audio != null)
                        {
                            _avisoPolvoHecho = true;
                            _audio.pitch = 0.6f; _audio.PlayOneShot(Audio.SintetizadorSfx.Derrumbe, 0.4f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f;
                            SimRenderer.Sacudida = 0.15f;
                        }
                        return;
                    }
                    // EL RENACER: ambos juntos, a 1/5 MÁS el eco del banco
                    // (Opus R90 #6: con carga fija el "lo que derramaste
                    // vuelve" de la R84 moría — banco/8 con tope 30 conserva
                    // ambas promesas: arrancan bajos Y lo tuyo pesa).
                    int cargaAgua = Mathf.Clamp(14 + _bancoAgua / 8, 14, 30);
                    int cargaLodo = Mathf.Clamp(14 + _bancoLodo / 8, 14, 30);
                    _deposito = new GameObject("DepositoDeAgua").AddComponent<DepositoDeAgua>();
                    _deposito.Init(_sim, cargaAgua, conTubo: true, cargaInstantanea: true);
                    _deposito.Aparecer();
                    _silo = new GameObject("SiloDeLodo").AddComponent<DepositoDeAgua>();
                    _silo.InitSilo(_sim, cargaLodo, conTubo: true, cargaInstantanea: true);
                    _silo.Aparecer();
                    Debug.Log("[TenThousandYears] RENACER: ambos a ~1/5 (agua=" + cargaAgua + ", lodo=" + cargaLodo + "); el refill los COMPLETARÁ cada vez más lento (todo toma tiempo).");
                    _reordenPaso = 4; _tPaso = 0f;
                    break;
                }

                case 4: // asentar (sacudida GANADA) → quietud 0.7 → LOS TUBOS.
                {
                    RetaguardiaDelAnillo(); // (R92) el fogón sigue humeando: su humo también es del Maestro hasta el amanecer.
                    bool asentados = _deposito != null && _deposito.Asentado && _silo != null && _silo.Asentado;
                    if (!_asentadoMarcado && (asentados || _tPaso > 12f))
                    {
                        _asentadoMarcado = true;
                        _tAsentado = _tPaso;
                        SimRenderer.Sacudida = 0.35f; // cayeron cuarenta toneladas: la sacudida está ganada.
                    }
                    if (!_asentadoMarcado || _tPaso < _tAsentado + 0.70f) return;
                    _deposito?.InstalarTubo();
                    _silo?.InstalarTubo();
                    if (_audio != null) { _audio.pitch = 0.5f; _audio.PlayOneShot(Audio.SintetizadorSfx.Derrumbe, 0.35f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; } // el gemido del metal subiendo.
                    Debug.Log("[TenThousandYears] LOS TUBOS empujan desde el suelo.");
                    _reordenPaso = 5; _tPaso = 0f;
                    break;
                }

                case 5: // CLANK único → 0.45 de nada → TOMA. → la primera gota → amanecer.
                {
                    RetaguardiaDelAnillo(); // (R92) hasta el último frame antes del amanecer: el repaso debe encontrar CERO.
                    bool instalados = (_deposito == null || _deposito.TuboInstalado) && (_silo == null || _silo.TuboInstalado);
                    if (!_clankHecho && (instalados || _tPaso > 5f))
                    {
                        _clankHecho = true;
                        _tClank = _tPaso;
                        SimRenderer.Sacudida = 0.5f;
                        if (_audio != null) { _audio.pitch = 1f; _audio.PlayOneShot(Audio.SintetizadorSfx.Clank, 0.85f * Audio.DirectorDeAudio.VolumenEfectos); }
                    }
                    if (!_clankHecho) return;
                    if (!_tomaDicha && _tPaso >= _tClank + 0.90f)
                    {
                        _tomaDicha = true;
                        DecirConTono(_g.vozToma, 0.90f, _g.vozHoldSeg); // el verbo del frasco: un regalo, no un decreto.
                    }
                    if (!_gotaActivada && _tPaso >= _tClank + 1.25f)
                    {
                        _gotaActivada = true;
                        _deposito?.ActivarRefill(); // LA PRIMERA GOTA, sola, cayendo desde el tope.
                        _silo?.ActivarRefill();
                    }
                    if (_tPaso < _tClank + 3.0f) return;
                    // (R91, Cesar: "asegura que absorba todas las partículas —
                    // siempre quedan restos… y eso mancha el reset") EL REPASO
                    // DEL AMANECER: una pasada silenciosa por la caverna
                    // entera que se lleva cualquier resto suelto (agua, lodo,
                    // humo, vapor) que haya caído DETRÁS del anillo — colas
                    // en vuelo, salpicaduras del drenado. Respeta lo
                    // protegido, los experimentos y el interior de los
                    // recipientes: solo borra la mancha, no lo tuyo.
                    RepasoDelAmanecer();
                    // AMANECER: la luz se libera, la cámara vuelve, el mundo suena distinto.
                    _radioForzado = null;
                    _focoDeLuz = null;
                    SimRenderer.FocoCinematico = null;
                    _radioObjetivo = UiStyles.S(_g.radioAmanecer);
                    // (R93) El Trueque ya NO se activa aquí: el canal se abre
                    // con el ADIÓS, cuando el Maestro deja su atención en el
                    // tablón. El prólogo sigue: la OBRA.
                    Debug.Log("[TenThousandYears] AMANECER: tanque=" + (_deposito != null ? _deposito.DelDueno() : -1) + " silo=" + (_silo != null ? _silo.DelDueno() : -1) + " — el goteo es el paisaje nuevo. Sigue la OBRA.");
                    _obraFase = 0;
                    CambiarBeat(Beat.Obra);
                    break;
                }
            }
        }

        /// <summary>
        /// (R91) EL REPASO DEL AMANECER: la pasada final silenciosa — todo
        /// resto de agua/lodo/humo/vapor FUERA de los recipientes y de las
        /// zonas protegidas se va. El reset queda sin una mancha.
        /// </summary>
        private void RepasoDelAmanecer()
        {
            var grid = _sim.Grid;
            int limpiadas = 0;
            for (int x = SimLevelBuilder.FundacionX0; x <= SimLevelBuilder.FundacionX1; x++)
                for (int y = SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo; y <= SimLevelBuilder.FundacionY1 + 3; y++)
                {
                    if (ProteccionDelBarrido(x, y)) continue;
                    if (_deposito != null && x >= _deposito.X0 && x <= _deposito.X1 && y >= _deposito.Y0 && y <= _deposito.Y1 + 1) continue;
                    if (_silo != null && x >= _silo.X0 && x <= _silo.X1 && y >= _silo.Y0 && y <= _silo.Y1 + 1) continue;
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Water || m == Lodo || m == LodoMojado || m == MaterialId.Smoke || m == MaterialId.Steam)
                    {
                        _sim.Paint(x, y, 0, MaterialId.Empty);
                        limpiadas++;
                    }
                }
            if (limpiadas > 0) Debug.Log("[TenThousandYears] REPASO del amanecer: " + limpiadas + " restos borrados — el reset sin una mancha.");
        }

        /// <summary>
        /// (R92, Cesar: "las partículas a ras del suelo y hasta algunas
        /// líneas por encima no se limpian; idealmente tendrían que
        /// limpiarse con la absorción del Maestro") LA RETAGUARDIA DEL
        /// ANILLO: el frente pasa UNA vez por columna, pero los charcos
        /// FLUYEN de vuelta al terreno ya barrido detrás de él y quedaban
        /// hasta el repaso tardío — que se leía como "limpieza por
        /// hardcodeo". Cada frame de la ceremonia (del anillo al último
        /// frame antes del amanecer), el intervalo ya conquistado se
        /// re-barre a ALTURA COMPLETA: lo que reaparece — charcos que
        /// fluyen de vuelta Y el humo del fogón protegido, que no deja de
        /// arder — se absorbe EN la coreografía, mota al Maestro incluida.
        /// El RepasoDelAmanecer queda como red de seguridad que debe
        /// reportar 0. Respeta lo protegido y el interior de los recipientes
        /// (mientras existan: en el paso 2 aún se están drenando).
        /// </summary>
        private void RetaguardiaDelAnillo()
        {
            var grid = _sim.Grid;
            float celdaM = SimRenderer.CellWorldSize;
            int x0 = Mathf.Max(SimLevelBuilder.FundacionX0, _colIzq + 1);
            int x1 = Mathf.Min(SimLevelBuilder.FundacionX1, _colDer - 1);
            int yBajo = SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo;
            int yAlto = SimLevelBuilder.FundacionY1 + 3; // ALTURA COMPLETA: el fogón protegido sigue humeando DURANTE la ceremonia — ese humo también vuela al Maestro, en chorro continuo, en vez de esperar al repaso.
            for (int x = x0; x <= x1; x++)
                for (int y = yBajo; y <= yAlto; y++)
                {
                    if (ProteccionDelBarrido(x, y)) continue;
                    if (_deposito != null && x >= _deposito.X0 && x <= _deposito.X1 && y >= _deposito.Y0 && y <= _deposito.Y1 + 1) continue;
                    if (_silo != null && x >= _silo.X0 && x <= _silo.X1 && y >= _silo.Y0 && y <= _silo.Y1 + 1) continue;
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Water) { _bancoAgua++; _sim.Paint(x, y, 0, MaterialId.Empty); SoltarMota(x, y, celdaM, tipo: 0); }
                    else if (m == Lodo || m == LodoMojado) { _bancoLodo++; _sim.Paint(x, y, 0, MaterialId.Empty); SoltarMota(x, y, celdaM, tipo: 1); }
                    else if (m == MaterialId.Smoke || m == MaterialId.Steam) { _sim.Paint(x, y, 0, MaterialId.Empty); SoltarMota(x, y, celdaM, tipo: 2); }
                }
        }

        /// <summary>
        /// (R84→R88) LA OBEDIENCIA DE UNA COLUMNA: lo que el filo de la luz
        /// alcanza, obedece en ese instante — agua y lodo/barbotina al banco
        /// (con su mota volando al sitio del renacer), las repisas de la
        /// cascada deshechas, y NADA más tocado: ni piedra, ni piso, ni los
        /// experimentos del jugador, ni cuenco/hogar/fogón (fuego y entregas
        /// no son derrames). Sin whoosh: el poder no hace ruido de esfuerzo.
        /// </summary>
        private void LimpiarColumna(int x)
        {
            if (x < SimLevelBuilder.FundacionX0 || x > SimLevelBuilder.FundacionX1) return; // las orillas son del perfilado.
            var grid = _sim.Grid;
            for (int y = SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo; y <= SimLevelBuilder.FundacionY1 + 3; y++)
            {
                if (ProteccionDelBarrido(x, y)) continue;
                byte m = grid.GetMat(x, y);
                if (EsCeldaDeRepisa(x, y) && (m == MaterialId.Stone || m == MaterialId.PisoEstructural))
                {
                    _sim.Paint(x, y, 0, MaterialId.Empty);
                    continue;
                }
                // (R90) Las motas vuelan EN VIVO hacia EL MAESTRO — desde el
                // borde oscuro hacia el centro iluminado: siempre visibles.
                // (La cosecha diferida de la R89 se RETIRÓ, regla 15: abrir
                // la luz 3 s para verla desinflaba el clímax — "solo
                // necesitaba un flashazo épico que sí me lo daba la
                // oscuridad".)
                float celdaM = SimRenderer.CellWorldSize;
                if (m == MaterialId.Water) { _bancoAgua++; _sim.Paint(x, y, 0, MaterialId.Empty); SoltarMota(x, y, celdaM, tipo: 0); }
                else if (m == Lodo || m == LodoMojado) { _bancoLodo++; _sim.Paint(x, y, 0, MaterialId.Empty); SoltarMota(x, y, celdaM, tipo: 1); }
                // (R91, Cesar: "incluidas las de humo que existan") El humo y
                // el vapor también obedecen: no van a ningún banco — el
                // Maestro se los traga y ya (esquirla gris).
                else if (m == MaterialId.Smoke || m == MaterialId.Steam) { _sim.Paint(x, y, 0, MaterialId.Empty); SoltarMota(x, y, celdaM, tipo: 2); }
            }
        }

        /// <summary>
        /// (R90, Cesar: "que absorba INCLUSO las rocas madre — las paredes
        /// más chuecas, para dejarlas PERFILADAS") EL PERFILADO: al paso del
        /// anillo, las MORDIDAS de la destrucción (R74: bocados en bóveda y
        /// muros, huecos y escombros del ala izquierda) SE ENDEREZAN — la
        /// roca vuelve a su línea (PaintStable: regla 22) y cada celda
        /// corregida suelta esquirlas GRISES hacia el Maestro (3 por celda;
        /// 5 en los muros: las que "caen de los lados", Opus R90 — así el
        /// ORDEN es espectacular aunque el jugador no haya tocado nada:
        /// ~400 destellos de pura geología obedeciendo). La grieta del
        /// DERRUMBE queda intacta: esa herida es de la Fase C.
        /// </summary>
        private void PerfilarColumna(int x)
        {
            var grid = _sim.Grid;
            float celdaM = SimRenderer.CellWorldSize;

            // La bóveda (mordidas de 0-2 hacia arriba), saltando la grieta del derrumbe.
            if (x >= SimLevelBuilder.FundacionX0 && x <= SimLevelBuilder.FundacionX1 &&
                (x < SimLevelBuilder.FundacionDerrumbeX - 2 || x > SimLevelBuilder.FundacionDerrumbeX + 2))
            {
                for (int y = SimLevelBuilder.FundacionY1 + 1; y <= SimLevelBuilder.FundacionY1 + 2; y++)
                    if (grid.GetMat(x, y) == MaterialId.Empty)
                    {
                        _sim.PaintStable(x, y, 0, MaterialId.Stone);
                        for (int k = 0; k < 3; k++) SoltarMota(x, y, celdaM, tipo: 2, stagger: 0.05f * k);
                    }
            }

            // Los muros (mordidas de 0-2 hacia adentro): las esquirlas que "caen de los lados".
            bool muro = (x >= SimLevelBuilder.FundacionX0 - 2 && x < SimLevelBuilder.FundacionX0)
                     || (x > SimLevelBuilder.FundacionX1 && x <= SimLevelBuilder.FundacionX1 + 2);
            if (muro)
            {
                for (int y = SimLevelBuilder.FundacionY0 + 1; y <= SimLevelBuilder.FundacionY1 - 1; y++)
                    if (grid.GetMat(x, y) == MaterialId.Empty)
                    {
                        _sim.PaintStable(x, y, 0, MaterialId.Stone);
                        for (int k = 0; k < 5; k++) SoltarMota(x, y, celdaM, tipo: 2, stagger: 0.05f * k);
                    }
            }

            // (R91, Cesar: "el pozo que traga el agua y el que traga el lodo
            // deberían quedar SELLADOS después del ORDEN… la animación debe
            // incluir el APLANAMIENTO del hueco de agua") LA POZA Y EL
            // CRÁTER SE APLANAN al paso del anillo, columna a columna — el
            // suelo vuelve a su línea y los pozos dejan de tragar PARA
            // SIEMPRE. De regalo mata el reflujo: la piedra que sube columna
            // a columna impide que el agua vecina fluya de vuelta al hueco
            // recién vaciado (la "cola" que antes bebía el desagüe).
            if (x >= SimLevelBuilder.FundacionCharcoX0 && x <= SimLevelBuilder.FundacionCharcoX1)
            {
                for (int y = SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo; y <= SimLevelBuilder.FundacionY0 - 1; y++)
                {
                    byte mp = grid.GetMat(x, y);
                    // Solo lo vaciable se aplana: un EXPERIMENTO del jugador
                    // hundido en la poza no se entierra en piedra (regla 38)
                    // — deja su muesca honesta.
                    if (mp != MaterialId.Empty && mp != MaterialId.Water && mp != Lodo && mp != LodoMojado) continue;
                    _sim.PaintStable(x, y, 0, MaterialId.Stone);
                    SoltarMota(x, y, celdaM, tipo: 2);
                }
            }
            if (x >= SimLevelBuilder.FundacionCraterX0 && x <= SimLevelBuilder.FundacionCraterX1)
            {
                for (int y = SimLevelBuilder.FundacionY0 - 2; y <= SimLevelBuilder.FundacionY0 - 1; y++)
                    if (grid.GetMat(x, y) == MaterialId.Empty || grid.GetMat(x, y) == Lodo || grid.GetMat(x, y) == LodoMojado)
                    {
                        _sim.PaintStable(x, y, 0, MaterialId.Stone);
                        SoltarMota(x, y, celdaM, tipo: 2);
                    }
            }

            // (R92) EL CUENCO también se aplana (mismo trato respetuoso que
            // la poza: solo lo vaciable — un experimento hundido deja muesca).
            if (x >= SimLevelBuilder.FundacionCuencoX0 && x <= SimLevelBuilder.FundacionCuencoX1)
            {
                for (int y = SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo; y <= SimLevelBuilder.FundacionY0 - 1; y++)
                {
                    byte mc = grid.GetMat(x, y);
                    if (mc == MaterialId.Water) _bancoAgua++;
                    else if (mc == Lodo || mc == LodoMojado) _bancoLodo++;
                    else if (mc != MaterialId.Empty) continue;
                    _sim.PaintStable(x, y, 0, MaterialId.Stone);
                    SoltarMota(x, y, celdaM, tipo: 2);
                }
            }

            // El terreno roto del ala izquierda (R74/R89b): huecos rellenados, escombros absorbidos.
            if (x >= 322 && x <= 349)
            {
                if (grid.GetMat(x, SimLevelBuilder.FundacionY0 - 1) == MaterialId.Empty)
                {
                    _sim.PaintStable(x, SimLevelBuilder.FundacionY0 - 1, 0, MaterialId.Stone);
                    for (int k = 0; k < 3; k++) SoltarMota(x, SimLevelBuilder.FundacionY0 - 1, celdaM, tipo: 2, stagger: 0.05f * k);
                }
                if (grid.GetMat(x, SimLevelBuilder.FundacionY0) == MaterialId.Stone && !SimLevelBuilder.EsObraDelTaller(x, SimLevelBuilder.FundacionY0))
                {
                    _sim.Paint(x, SimLevelBuilder.FundacionY0, 0, MaterialId.Empty);
                    for (int k = 0; k < 3; k++) SoltarMota(x, SimLevelBuilder.FundacionY0, celdaM, tipo: 2, stagger: 0.05f * k);
                }
            }
        }

        /// <summary>(R87) ¿(x,y) es parte del andamio de la cascada? Las dos repisas y el labio, con las MISMAS medidas con que las talló BuildFundacion (regla 24: contra constantes, no prosa).</summary>
        private static bool EsCeldaDeRepisa(int x, int y)
        {
            if (x >= SimLevelBuilder.FundacionRepisaAX0 && x <= SimLevelBuilder.FundacionRepisaAX1 &&
                y >= SimLevelBuilder.FundacionRepisaAY && y <= SimLevelBuilder.FundacionRepisaAY + 1) return true;
            if (x >= SimLevelBuilder.FundacionRepisaBX0 && x <= SimLevelBuilder.FundacionRepisaBX1 &&
                y >= SimLevelBuilder.FundacionRepisaBY && y <= SimLevelBuilder.FundacionRepisaBY + 1) return true;
            if (x == SimLevelBuilder.FundacionRepisaBX0 - 1 &&
                y >= SimLevelBuilder.FundacionRepisaBY && y <= SimLevelBuilder.FundacionRepisaBY + 3) return true; // el labio.
            return false;
        }

        // =================================================================
        // (R87→R90) LAS MOTAS DEL ORDEN: cada celda que el anillo absorbe
        // suelta un destello que vuela AL MAESTRO ("debería absorberlas
        // TODAS el Maestro… y le caen cosas de todos lados" — Cesar R90; el
        // destino a los tanques de la R87-89 se retiró con la cosecha
        // diferida: desinflaba el clímax). Tres tipos: agua azul, lodo
        // pardo, y ROCA gris — la del perfilado. Vuelan desde el borde
        // oscuro hacia el centro iluminado: siempre visibles, sin diferir
        // nada. Velocidad ~110 c/s (8× el frente: la luz sale, la materia
        // entra — Opus R90); las nacidas en el remate llegan apretadas
        // (dur fija) para que el trago final caiga con la última.
        // Prioridad: LA ROCA NUNCA SE DESCARTA (es el espectáculo
        // garantizado); agua/lodo saltean 1 de cada 2 sobre 240 vivas.
        // =================================================================
        private struct Mota { public Vector3 desde, hasta; public float t, dur; public byte tipo; } // tipo: 0=agua, 1=lodo, 2=roca.
        private readonly List<Mota> _motas = new List<Mota>();
        private const int MotasTope = 300; // (Opus R90: un mapa inundado emite ~840 celdas/s; con 160 la roca se perdía muda detrás del agua.)
        private int _motasSalteo;
        private float _maestroHalo; // (R90) el Maestro SE HINCHA tragando: +0.012 por mota que llega; el flashazo es este halo reventando.

        private void SoltarMota(int x, int y, float celda, byte tipo, float stagger = 0f)
        {
            if (tipo != 2)
            {
                if (_motas.Count >= MotasTope) return;
                if (_motas.Count > 240 && ((_motasSalteo++) & 1) == 1) return; // el banco cuenta igual; el destello se saltea.
            }
            else if (_motas.Count >= MotasTope + 40) return; // la roca solo cede ante el colapso total.

            var desde = new Vector3((x + 0.5f) * celda, (y + 0.5f) * celda, 0f);
            var hasta = PosMaestro();
            float dCeldas = Vector3.Distance(desde, hasta) / celda;
            float dRemate = (ColMaestro() - SimLevelBuilder.FundacionX0 + 2) - 15f;
            _motas.Add(new Mota
            {
                desde = desde,
                hasta = hasta,
                t = -stagger, // (R90) escalonadas: esquirlas y polvo, no un píxel duplicado.
                dur = dCeldas > dRemate ? 0.45f : Mathf.Clamp(0.30f + dCeldas / 110f, 0.30f, 1.15f),
                tipo = tipo
            });
        }

        private void TickMotas()
        {
            for (int i = _motas.Count - 1; i >= 0; i--)
            {
                var mo = _motas[i];
                mo.t += Time.deltaTime;
                if (mo.t >= mo.dur)
                {
                    _maestroHalo = Mathf.Min(1.3f, _maestroHalo + 0.012f); // (R90) el Maestro traga: su halo crece.
                    _motas.RemoveAt(i);
                    continue;
                }
                _motas[i] = mo;
            }
        }

        private void DibujarMotas()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (_lucecitaTex == null) ConstruirLucecita();
            var prev = GUI.color;
            for (int i = 0; i < _motas.Count; i++)
            {
                var mo = _motas[i];
                if (mo.t < 0f) continue; // aún en su escalón.
                float tt = Mathf.Clamp01(mo.t / mo.dur);
                float ease = tt * tt * (3f - 2f * tt);
                var pos = Vector3.Lerp(mo.desde, mo.hasta, ease);
                pos.y += Mathf.Sin(tt * Mathf.PI) * 0.55f;      // el arco del vuelo.
                // Jitter determinista por índice: esquirlas, no un punto clonado.
                pos.x += (((i * 37) & 7) - 3.5f) * 0.012f;
                pos.y += (((i * 53) & 7) - 3.5f) * 0.012f;
                Vector3 sp = cam.WorldToScreenPoint(pos);
                if (sp.z < 0f) continue;
                float gy = Screen.height - sp.y;
                float lado = UiStyles.S(9f) * (1f - 0.35f * tt); // encoge al llegar.
                float alfa = 0.8f * (0.4f + 0.6f * Mathf.Sin(tt * Mathf.PI));
                GUI.color = mo.tipo == 0 ? new Color(0.55f, 0.8f, 1f, alfa)
                          : mo.tipo == 1 ? new Color(0.85f, 0.62f, 0.4f, alfa)
                          : new Color(0.62f, 0.60f, 0.58f, alfa); // la roca.
                GUI.DrawTexture(new Rect(sp.x - lado, gy - lado, lado * 2f, lado * 2f), _lucecitaTex);
            }
            GUI.color = prev;
        }

        /// <summary>(R90) El halo del Maestro tragando: crece con cada mota que llega, respira, y el FLASHAZO del cierre es este halo reventando.</summary>
        private void DibujarHaloMaestro()
        {
            if (_maestroHalo <= 0.01f) return;
            var cam = Camera.main;
            if (cam == null) return;
            if (_lucecitaTex == null) ConstruirLucecita();
            Vector3 sp = cam.WorldToScreenPoint(PosMaestro());
            if (sp.z < 0f) return;
            float gy = Screen.height - sp.y;
            float resp = 1f + 0.08f * Mathf.Sin(Time.time * 5f);
            float r = UiStyles.S(26f + 46f * Mathf.Min(1f, _maestroHalo)) * resp;
            var prev = GUI.color;
            GUI.color = new Color(0.95f, 0.88f, 0.7f, Mathf.Min(0.55f, 0.18f + 0.3f * _maestroHalo));
            GUI.DrawTexture(new Rect(sp.x - r, gy - r, r * 2f, r * 2f), _lucecitaTex);
            GUI.color = prev;
        }

        /// <summary>
        /// true = intocable para el barrido. (R86) La poza y el cráter SE
        /// FUERON de esta lista (protegían la mugre del jugador en R84, se
        /// volvieron protección-del-dueño en R85, y el guion nuevo de Cesar
        /// los ABSORBE enteros: las fuentes mueren en el ORDEN). Quedan el
        /// cuenco, el hogar y el fogón: fuego y entregas no son derrames.
        /// </summary>
        private static bool ProteccionDelBarrido(int x, int y)
        {
            // (R92, Cesar: "lo mismo para su pocito donde ANTES pedía las
            // cosas") El CUENCO salió de la lista: el Maestro también se
            // traga su propio receptor — su contenido va al banco y el
            // aplanamiento lo sella con el resto de los pozos. Post-ORDEN
            // las entregas viven en el tablón del Trueque, no en un hoyo.
            // El hogar de brasas del Maestro (brasas + tiro).
            if (x >= SimLevelBuilder.FundacionBrasasX0 - 1 && x <= SimLevelBuilder.FundacionBrasasX1 + 1 &&
                y >= SimLevelBuilder.FundacionY0 && y <= SimLevelBuilder.FundacionY0 + 4) return true;
            // El lecho del fogón del jugador.
            if (x >= SimLevelBuilder.FundacionFogonX0 - 2 && x <= SimLevelBuilder.FundacionFogonX1 + 2 &&
                y >= SimLevelBuilder.FundacionY0 && y <= SimLevelBuilder.FundacionY0 + 2) return true;
            return false;
        }
        private bool _fuentesApagadas; // (R86) el ORDEN apaga manantial/rezumadero/gotera PARA SIEMPRE: los reservorios con refill son la fuente de ahora en más.

        // (R88, regla 15) `DibujarFrenteDelBarrido` — la columna de luz
        // pulsante a 6 Hz de la R85 — SE RETIRÓ por dirección de Opus: "un
        // frente que jadea es una máquina, y el poder omnipotente no se
        // esfuerza". El frente del anillo es ahora EL BORDE DE LA VIÑETA,
        // quieto, sin latido. En su lugar entran el FLASH del pulso y los
        // RESPLANDORES de la carga esperando en el suelo.

        /// <summary>(R88) El flash del PULSO: blanco pleno 0.22 → 0 en 0.18 s, curva cuadrática — el chasquido de la palabra tocando el mundo.</summary>
        private void DibujarFlash()
        {
            if (_flashT <= 0f) return;
            float a;
            if (_flashT > _flashDur - _flashHold) a = _flashAlfa; // el sostenido del flashazo.
            else
            {
                float t = _flashT / Mathf.Max(0.01f, _flashDur - _flashHold);
                a = _flashAlfa * t * t;
            }
            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }

        // (R90, regla 15) `DibujarResplandoresDeCarga`/`DibujarPool` (los
        // brillos en los sitios del renacer, R88) SE RETIRARON: las motas ya
        // no aterrizan ahí — TODO vuela al Maestro ("debería absorberlas
        // todas él", Cesar R90) y su lugar lo ocupa el HALO del Maestro
        // tragando (DibujarHaloMaestro) que revienta en el flashazo.

        // (R86, regla 15) `ConstruirEstanteria` — el mueble central con        // (R86, regla 15) `ConstruirEstanteria` — el mueble central con        // (R86, regla 15) `ConstruirEstanteria` — el mueble central con
        // bahías apiladas de la R85 — SE RETIRÓ ENTERO: Cesar vetó el
        // apilado y el guion nuevo apaga las fuentes en el ORDEN, así que
        // la premisa del mueble (atrapar la gotera en la bahía alta) murió.
        // Los recipientes renacen EN SUS SITIOS con el tubo grueso
        // integrado (DepositoDeAgua, conTubo). Las constantes
        // FundacionEstanteria*/FundacionBahia* se fueron del plano con él.

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
            if (_fuentesApagadas) return; // (R86) el ORDEN apagó el manantial PARA SIEMPRE: el refill del tanque es la fuente ahora.
            if (_barridoEnCurso) return; // (R84, Opus A10) el barrido no pelea contra la cascada.
            _manantialTimer -= Time.deltaTime;
            if (_manantialTimer > 0f) return;
            _manantialTimer = _g.manantialSeg;
            for (int i = 0; i < _g.manantialCeldas; i++)
                _sim.PaintStable(SimLevelBuilder.FundacionManantialX,
                    SimLevelBuilder.FundacionManantialY + i, 0, MaterialId.Water);
        }

        /// <summary>
        /// (R77) El rumor de la cascada: volumen por distancia al manantial
        /// (caída cuadrática, el perfil de los grifos de DirectorDeAudio) ×
        /// volumen base del guion × VolumenEfectos del jugador. Cada frame —
        /// es una multiplicación, no una búsqueda.
        /// </summary>
        private void TickCascadaAudio()
        {
            if (_cascadaFuente == null || _aprendiz == null) return;
            // (R88, dirección Opus: EL VACÍO) 1.3 s ANTES de la palabra, el
            // sonido del mundo muere — la cascada sigue cayendo A LA VISTA,
            // muda. Ese descuadre es la primera prueba de que algo ya
            // decidió. El efecto más barato del pliego y el más grande.
            if (_cascadaMuda)
            {
                _cascadaFuente.volume = Mathf.MoveTowards(_cascadaFuente.volume, 0f, Time.deltaTime * 1.6f);
                return;
            }
            // (R86) La cascada murió con el ORDEN: su rumor se desvanece en
            // un par de segundos y la fuente se detiene del todo (no un mute:
            // el AudioSource deja de girar).
            if (_fuentesApagadas)
            {
                if (!_cascadaFuente.isPlaying) return;
                _cascadaFuente.volume = Mathf.MoveTowards(_cascadaFuente.volume, 0f, Time.deltaTime * 0.25f);
                if (_cascadaFuente.volume <= 0.0001f) _cascadaFuente.Stop();
                return;
            }
            float celda = SimRenderer.CellWorldSize;
            // (R81, revisión Opus #3, MEDIDO) El ancla estaba en el MANANTIAL
            // (340,176) con radio 55: el volumen era 0 en el spawn, 0 junto
            // al Maestro y 0.0016 CON LOS PIES EN LA POZA — el "rumor que
            // invita" de la R77 no sonó jamás. El ancla pasa a la MITAD DE LA
            // CAÍDA (entre el manantial y el centro de la poza, ~x359 y la
            // repisa baja) y el radio del guion sube a 95: audible desde el
            // spawn (~0.13), pleno en la poza (~0.38), direccional siempre.
            var punto = new Vector2(
                (SimLevelBuilder.FundacionManantialX + (SimLevelBuilder.FundacionCharcoX0 + SimLevelBuilder.FundacionCharcoX1 + 1) * 0.5f) * 0.5f * celda,
                SimLevelBuilder.FundacionRepisaBY * celda);
            float dCeldas = Vector2.Distance((Vector2)_aprendiz.position, punto) / celda;
            float t = Mathf.Clamp01(1f - dCeldas / Mathf.Max(1f, _g.cascadaRadioAudibleCeldas));
            _cascadaFuente.volume = t * t * _g.cascadaVolumen * Audio.DirectorDeAudio.VolumenEfectos;
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
            if (_fuentesApagadas) return; // (R86) sin cascada no hay exceso que beber: la poza quedó vacía y quieta.
            if (_barridoEnCurso) return; // (R84) mismo respiro que el manantial.
            // (revisión Opus 73 #11) A CADENCIA PROPIA, no por frame: era el
            // único proceso del director atado al framerate — el nivel de la
            // poza dependía de los fps de la máquina.
            _rezumaTimer -= Time.deltaTime;
            if (_rezumaTimer > 0f) return;
            _rezumaTimer = 1f / 30f; // una celda por tick de sim, como mucho.

            int agua = ContarEnPoza(MaterialId.Water);
            if (agua <= _g.pozaLlenaCeldas) return;

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

        // (R91, regla 15) `TickDesagueFinal` (el "trago final" R86-R89 que
        // bebía el residuo de poza y cráter tras el ORDEN) SE RETIRÓ: el
        // APLANAMIENTO del anillo sella ambos pozos con piedra — ya no hay
        // hueco que trague ni residuo que beber ("deberían quedar sellados
        // para que no sigan tragando", Cesar R91). El barrido final del
        // amanecer (abajo) cubre los restos sueltos.


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
            _lodoTimer = _g.lodoSeepSeg;

            int monticulo = ContarLodoEnCrater();
            if (_lodoPausado) { if (monticulo <= _g.lodoMonticuloResume) _lodoPausado = false; else return; }
            else if (monticulo >= _g.lodoMonticuloTope) { _lodoPausado = true; return; }

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
                {
                    // (R83, revisión Opus A4) EL LODO ATESORADO NO ES
                    // MONTÍCULO: la caja de este conteo solapa el silo
                    // (x389-391), y sin esta exclusión la histéresis
                    // (lodoMonticuloTope/Resume) pausaba la gotera con el
                    // cráter vacío en cuanto el jugador GUARDABA lodo —
                    // softlock lento, silencioso (reglas 38/43).
                    if (_silo != null && x >= _silo.X0 && x <= _silo.X1 && y >= _silo.Y0 && y <= _silo.Y1) continue;
                    if (grid.GetMat(x, y) == Lodo) n++;
                }
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

        // ==================================================================
        // (R93 — EL FINAL DEL PRÓLOGO, mandato nocturno de Cesar) Tres beats
        // nuevos construidos DESDE el lenguaje del prólogo (voz de una
        // palabra, fichas contextuales, placas de mundo, luz con causa):
        //
        //   OBRA    — el Maestro regala el CINCEL (vuela a tu mano como voló
        //             el frasco) y pide ALZA.: una PLATAFORMA de piedra en el
        //             centro (silueta blanca que hay que cubrir) y EL TECHO —
        //             sellar la grieta del derrumbe, la única herida que el
        //             ORDEN dejó a propósito. Tu primera edición del mundo.
        //   ACOMODO — ACOMODA.: la MUDANZA (V) se enseña y los dos
        //             reservorios se llevan a la plataforma, uno al lado del
        //             otro — el centro del taller lo defines TÚ. "Eso se
        //             parece más a la presentación final del mapa al iniciar
        //             semilla cero" (Cesar).
        //   ADIOS   — el Maestro repite ORDEN. (el poder es un verbo suyo,
        //             no un evento único): la luz se cierra sobre TU obra,
        //             abre EL VANO en el muro oeste (el espacio crece), se
        //             revela el mapa entero, dice DIEZ MIL AÑOS. — y su
        //             figura etérea se DESVANECE hacia el tablón del Trueque:
        //             no te abandona, deja de prestarte atención ("ya te
        //             enseñó, ya estás listo") y su atención queda viva en el
        //             canal abierto: el tablón. Su fuego sigue ardiendo.
        // ==================================================================

        // La geometría del final (regla 47: coordenadas explícitas del plano
        // de la Fundación, medidas contra SimLevelBuilder R89):
        private const int ObraPlatX0 = 378, ObraPlatX1 = 395;  // la plataforma: entre el sitio del silo (369-376) y el fogón del jugador (396-406), sobre el cráter sellado.
        private const int ObraPlatY0 = 140, ObraPlatY1 = 141;  // dos filas: "un poco de altura".
        private const int ObraTechoX0 = 388, ObraTechoX1 = 392; // la grieta del derrumbe (DerrumbeX±2) en la bóveda.
        private const int ObraTechoY0 = 201, ObraTechoY1 = 202;
        private const int SlotAX0 = 379, SlotBX0 = 387;        // anclas exactas de los dos reservorios (8 de ancho, adyacentes).
        private const int SlotY0 = 142;                        // sobre la plataforma (top y141).
        private const int VanoX0 = 310, VanoX1 = 319;          // el vano del muro oeste: el umbral del mañana.
        private const int VanoY0 = 140, VanoY1 = 146;

        private int _obraFase;                 // 0=regalo del cincel, 1=construyendo, 2=respiro.
        private Transform _cincelVuelo;
        private float _tVueloCincel;
        private readonly List<Vector2Int> _obraPlatObjetivo = new List<Vector2Int>();
        private readonly List<Vector2Int> _obraTechoObjetivo = new List<Vector2Int>();
        private GameObject _silPlataforma, _silTecho, _silSlotA, _silSlotB;
        private bool _fichaColocaMostrada, _fichaLlevalosMostrada;
        private int _acomodoFase;              // 0=palabra+fichas, 1=mudando, 2=respiro.
        private bool _slotAOcupado, _slotBOcupado;
        private int _adiosPaso;
        private float _motaAdiosTimer;
        private SpriteRenderer _maestroSr;
        private Vector3 _maestroSrBase;
        private Color _maestroColorBase = Color.white;
        private float _maestroPresencia = 1f;  // 1 = presente; el adiós la lleva a 0 (la figura se va; el fuego y el tablón quedan).

        /// <summary>(R93) Una silueta blanca de mundo: el rectángulo que la voz pide cubrir. Solido() estirado, alfa lo late el tick del beat.</summary>
        private GameObject CrearSilueta(string nombre, int x0, int y0, int x1, int y1)
        {
            float c = SimRenderer.CellWorldSize;
            var go = new GameObject(nombre);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MaquinariaSprites.Solido();
            sr.sortingOrder = 42; // bajo el aprendiz (47+) y la silueta de Mudanza (44).
            sr.color = new Color(1f, 1f, 1f, 0.14f);
            go.transform.position = new Vector3((x0 + x1 + 1) * 0.5f * c, (y0 + y1 + 1) * 0.5f * c, 0f);
            go.transform.localScale = new Vector3((x1 - x0 + 1) * c, (y1 - y0 + 1) * c, 1f);
            return go;
        }

        private void LatirSilueta(GameObject go, float baseAlfa)
        {
            if (go == null) return;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(1f, 1f, 1f, baseAlfa + 0.06f * Mathf.Sin(Time.time * 2.4f));
        }

        private int ContarCubiertas(List<Vector2Int> objetivo)
        {
            var grid = _sim.Grid;
            int n = 0;
            for (int i = 0; i < objetivo.Count; i++)
            {
                byte m = grid.GetMat(objetivo[i].x, objetivo[i].y);
                if (m == MaterialId.Stone || m == MaterialId.PisoEstructural) n++;
            }
            return n;
        }

        private Vector3 CentroPlataformaMundo()
        {
            float c = SimRenderer.CellWorldSize;
            return new Vector3((ObraPlatX0 + ObraPlatX1 + 1) * 0.5f * c, (SlotY0 + 7) * c, 0f);
        }

        private Vector3 TablonMundo()
        {
            float c = SimRenderer.CellWorldSize;
            return new Vector3((SimLevelBuilder.FundacionSalidaX0 + SimLevelBuilder.FundacionSalidaX1) * 0.5f * c,
                (SimLevelBuilder.FundacionY0 + 3) * c, 0f);
        }

        /// <summary>(R93) EL REGALO DE LA HERRAMIENTA: el cincel vuela de la mesa a tu mano, con la comba del frasco — "que se note que me lo dio el Maestro".</summary>
        private void LanzarVueloDelCincel()
        {
            var go = new GameObject("CincelEnVuelo");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MaquinariaSprites.CincelHerramienta();
            sr.sortingOrder = Capas.CarryEnMano;
            float celda = SimRenderer.CellWorldSize;
            go.transform.localScale = Vector3.one * (0.9f * celda * 6f / Mathf.Max(1f, sr.sprite.rect.width));
            go.transform.position = PosMaestro();
            _cincelVuelo = go.transform;
            _tVueloCincel = 0f;
        }

        private void TickVueloDelCincel()
        {
            if (_cincelVuelo == null) return;
            _tVueloCincel += Time.deltaTime / Mathf.Max(0.4f, _g.entregaFrascoSeg);
            float t = Mathf.Clamp01(_tVueloCincel);
            float ease = t * t * (3f - 2f * t);
            Vector3 a = PosMaestro();
            Vector3 b = _aprendiz.position + new Vector3(0f, -0.2f, 0f);
            Vector3 m = Vector3.Lerp(a, b, 0.5f) + new Vector3(0f, 0.7f, 0f);
            _cincelVuelo.position = Vector3.Lerp(Vector3.Lerp(a, m, ease), Vector3.Lerp(m, b, ease), ease);
            _cincelVuelo.rotation = Quaternion.Euler(0f, 0f, -35f * ease); // se ladea al llegar: pieza que se acomoda, no proyectil.
            if (t >= 1f)
            {
                Destroy(_cincelVuelo.gameObject);
                _cincelVuelo = null;
                if (_audio != null) { _audio.pitch = 1.5f; _audio.PlayOneShot(Audio.SintetizadorSfx.Clank, 0.25f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; } // el toque metálico chico: herramienta en mano.
            }
        }

        /// <summary>(R93, Cesar: "una figura etérea, con algo de movimiento") El Maestro FLOTA (solo hacia arriba: los pies nunca se hunden) y su alfa RESPIRA; _maestroPresencia lo desvanece en el adiós.</summary>
        private void TickMaestroEtereo()
        {
            if (_maestroSr == null) return;
            float flote = Mathf.Max(0f, Mathf.Sin(Time.time * 0.85f)) * 0.06f;
            _maestroSr.transform.localPosition = _maestroSrBase + new Vector3(0f, flote, 0f);
            float aliento = 0.84f + 0.12f * Mathf.Sin(Time.time * 0.6f + 1.7f);
            var c = _maestroColorBase;
            c.a = _maestroColorBase.a * aliento * Mathf.Clamp01(_maestroPresencia);
            _maestroSr.color = c;
        }

        /// <summary>(R93) Mota con destino EXPLÍCITO (el adiós las manda al tablón, no al Maestro).</summary>
        private void SoltarMotaHacia(Vector3 desde, Vector3 hasta, byte tipo, float stagger = 0f)
        {
            if (_motas.Count >= MotasTope + 40) return;
            float dCeldas = Vector3.Distance(desde, hasta) / SimRenderer.CellWorldSize;
            _motas.Add(new Mota
            {
                desde = desde,
                hasta = hasta,
                t = -stagger,
                dur = Mathf.Clamp(0.35f + dCeldas / 90f, 0.35f, 1.1f),
                tipo = tipo
            });
        }

        /// <summary>14) LA OBRA (ALZA.): el cincel llega volando, la silueta blanca dice dónde, y el jugador ALZA su plataforma + sella el techo herido.</summary>
        private void TickObra()
        {
            switch (_obraFase)
            {
                case 0:
                    if (_tBeat < 0.9f) return;
                    if (_cincelVuelo == null && _tVueloCincel <= 0f) { LanzarVueloDelCincel(); return; }
                    if (_cincelVuelo != null) return; // el vuelo sigue en el aire.
                    // El cincel aterrizó: la palabra, las siluetas y la ficha.
                    DecirConTono("ALZA.", 0.95f, 2.4f);
                    _obraPlatObjetivo.Clear();
                    _obraTechoObjetivo.Clear();
                    var grid0 = _sim.Grid;
                    for (int x = ObraPlatX0; x <= ObraPlatX1; x++)
                        for (int y = ObraPlatY0; y <= ObraPlatY1; y++)
                            if (grid0.GetMat(x, y) != MaterialId.Stone && grid0.GetMat(x, y) != MaterialId.PisoEstructural)
                                _obraPlatObjetivo.Add(new Vector2Int(x, y));
                    for (int x = ObraTechoX0; x <= ObraTechoX1; x++)
                        for (int y = ObraTechoY0; y <= ObraTechoY1; y++)
                            if (grid0.GetMat(x, y) == MaterialId.Empty)
                                _obraTechoObjetivo.Add(new Vector2Int(x, y));
                    _silPlataforma = CrearSilueta("SiluetaPlataforma", ObraPlatX0, ObraPlatY0, ObraPlatX1, ObraPlatY1);
                    if (_obraTechoObjetivo.Count > 0)
                        _silTecho = CrearSilueta("SiluetaTecho", ObraTechoX0, ObraTechoY0, ObraTechoX1, ObraTechoY1);
                    _tutorial.Mostrar("el cincel talla y construye",
                        new TutorialContextual.Paso { Etiqueta = "C", Presionada = () => Cincel.ModoActivo });
                    _obraFase = 1;
                    _tBeat = 0f;
                    break;

                case 1:
                {
                    LatirSilueta(_silPlataforma, 0.13f);
                    LatirSilueta(_silTecho, 0.13f);
                    if (!_fichaColocaMostrada)
                    {
                        if (Cincel.ModoActivo && _tutorial.Visible) _tutorial.Confirmar(0);
                        if (!_tutorial.Visible && _tBeat > 0.6f && Cincel.ModoActivo)
                        {
                            _fichaColocaMostrada = true;
                            _tutorial.Mostrar("cubre lo blanco con piedra",
                                new TutorialContextual.Paso { Etiqueta = "CLIC DER", Presionada = () => Mouse.current != null && Mouse.current.rightButton.isPressed });
                        }
                        if (!_fichaColocaMostrada) return;
                    }
                    int hechasP = ContarCubiertas(_obraPlatObjetivo);
                    int hechasT = ContarCubiertas(_obraTechoObjetivo);
                    if (_fichaColocaMostrada && _tutorial.Visible && hechasP + hechasT >= 6) _tutorial.Confirmar(0);
                    if (hechasP >= _obraPlatObjetivo.Count && _silPlataforma != null)
                    {
                        Destroy(_silPlataforma); _silPlataforma = null;
                        if (_audio != null) { _audio.pitch = 1.3f; _audio.PlayOneShot(Audio.SintetizadorSfx.Clank, 0.3f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; }
                    }
                    if (_obraTechoObjetivo.Count > 0 && hechasT >= _obraTechoObjetivo.Count && _silTecho != null)
                    {
                        Destroy(_silTecho); _silTecho = null;
                        if (_audio != null) { _audio.pitch = 1.15f; _audio.PlayOneShot(Audio.SintetizadorSfx.Clank, 0.3f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; }
                    }
                    if (hechasP >= _obraPlatObjetivo.Count && hechasT >= _obraTechoObjetivo.Count)
                    {
                        Debug.Log("[TenThousandYears] OBRA completa: plataforma " + hechasP + "/" + _obraPlatObjetivo.Count + ", techo " + hechasT + "/" + _obraTechoObjetivo.Count + ".");
                        _obraFase = 2;
                        _tBeat = 0f;
                    }
                    break;
                }

                case 2:
                    if (_tBeat < 1.1f) return;
                    _acomodoFase = 0;
                    _slotAOcupado = _slotBOcupado = false;
                    CambiarBeat(Beat.Acomodo);
                    break;
            }
        }

        /// <summary>15) EL ACOMODO (ACOMODA.): la mudanza se enseña y los dos reservorios se llevan a la plataforma — el taller queda con SU centro.</summary>
        private void TickAcomodo()
        {
            switch (_acomodoFase)
            {
                case 0:
                    if (_tBeat < 0.5f) return;
                    DecirConTono("ACOMODA.", 0.95f, 2.6f);
                    _deposito?.HabilitarMudanza();
                    _silo?.HabilitarMudanza();
                    _silSlotA = CrearSilueta("SiluetaSlotA", SlotAX0, SlotY0, SlotAX0 + 7, SlotY0 + 13);
                    _silSlotB = CrearSilueta("SiluetaSlotB", SlotBX0, SlotY0, SlotBX0 + 7, SlotY0 + 13);
                    _tutorial.Mostrar("la mudanza recoloca el taller",
                        new TutorialContextual.Paso { Etiqueta = "V", Presionada = () => Mudanza.ModoActivo });
                    _acomodoFase = 1;
                    _tBeat = 0f;
                    break;

                case 1:
                {
                    LatirSilueta(_silSlotA, _slotAOcupado ? 0f : 0.11f);
                    LatirSilueta(_silSlotB, _slotBOcupado ? 0f : 0.11f);
                    if (!_fichaLlevalosMostrada)
                    {
                        if (Mudanza.ModoActivo && _tutorial.Visible) _tutorial.Confirmar(0);
                        if (!_tutorial.Visible && Mudanza.ModoActivo)
                        {
                            _fichaLlevalosMostrada = true;
                            _tutorial.Mostrar("llévalos a la plataforma",
                                new TutorialContextual.Paso { Etiqueta = "CLIC IZQ", Presionada = () => Mouse.current != null && Mouse.current.leftButton.isPressed });
                        }
                    }
                    // El IMÁN de los slots: un reservorio soltado a ±2 celdas
                    // de un ancla libre ENCAJA solo (Reposicionar exacto) —
                    // la mudanza enseña el gesto, el slot perdona el pulso.
                    IntentarEncajar(_deposito);
                    IntentarEncajar(_silo);
                    bool aEn = HayReservorioEn(SlotAX0), bEn = HayReservorioEn(SlotBX0);
                    if (aEn && !_slotAOcupado) { _slotAOcupado = true; BlipSlot(); }
                    if (bEn && !_slotBOcupado) { _slotBOcupado = true; BlipSlot(); }
                    _slotAOcupado = aEn; _slotBOcupado = bEn;
                    if (aEn && bEn)
                    {
                        if (_fichaLlevalosMostrada && _tutorial.Visible) _tutorial.Confirmar(0);
                        if (_silSlotA != null) { Destroy(_silSlotA); _silSlotA = null; }
                        if (_silSlotB != null) { Destroy(_silSlotB); _silSlotB = null; }
                        Debug.Log("[TenThousandYears] ACOMODO completo: los dos reservorios en la plataforma.");
                        _acomodoFase = 2;
                        _tBeat = 0f;
                    }
                    break;
                }

                case 2:
                    if (_tBeat < 1.3f) return;
                    _adiosPaso = 0;
                    _tPaso = 0f;
                    CambiarBeat(Beat.Adios);
                    break;
            }
        }

        private void BlipSlot()
        {
            if (_audio != null) { _audio.pitch = 1.25f; _audio.PlayOneShot(Audio.SintetizadorSfx.Clank, 0.35f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; }
        }

        private bool HayReservorioEn(int slotX0)
        {
            bool En(DepositoDeAgua r) => r != null && r.X0 == slotX0 && r.Y0 == SlotY0;
            return En(_deposito) || En(_silo);
        }

        private void IntentarEncajar(DepositoDeAgua r)
        {
            if (r == null || r.Y0 < SlotY0 - 1 || r.Y0 > SlotY0 + 1) return;
            foreach (int slot in new[] { SlotAX0, SlotBX0 })
            {
                if (r.X0 == slot && r.Y0 == SlotY0) return;         // ya encajado.
                if (Mathf.Abs(r.X0 - slot) > 2) continue;            // lejos de este slot.
                if (HayReservorioEn(slot)) continue;                 // ocupado por el otro.
                r.Reposicionar(slot, SlotY0);
                return;
            }
        }

        /// <summary>16) EL ADIÓS: ORDEN. repetido sobre TU obra → el VANO → la revelación del mapa → DIEZ MIL AÑOS. → la figura deja su atención en el tablón. El fuego queda.</summary>
        private void TickAdios()
        {
            _tPaso += Time.deltaTime;
            switch (_adiosPaso)
            {
                case 0: // quietud → la palabra, otra vez. El poder es un verbo, no un evento.
                    if (_tPaso < 0.9f) return;
                    DecirConTono(_g.vozOrden, 0.82f, 2.4f);
                    _adiosPaso = 1; _tPaso = 0f;
                    break;

                case 1: // el pulso chico + la luz se CIERRA — esta vez sobre TU plataforma, no sobre él.
                    if (_tPaso < 0.75f) return;
                    _flashT = 0.18f; _flashDur = 0.18f; _flashAlfa = 0.22f; _flashHold = 0f;
                    if (_audio != null) _audio.PlayOneShot(Audio.SintetizadorSfx.SubGrave, 0.8f * Audio.DirectorDeAudio.VolumenEfectos);
                    _focoDeLuz = CentroPlataformaMundo();
                    _radioCierreDesde = _radio;
                    _adiosPaso = 2; _tPaso = 0f;
                    break;

                case 2: // el cierre (0.7 s) → EL VANO se abre en el muro oeste: el espacio crece.
                {
                    float tc = Mathf.Clamp01(_tPaso / 0.7f);
                    _radioForzado = Mathf.Lerp(_radioCierreDesde, UiStyles.S(340f), tc * tc);
                    if (tc < 1f) return;
                    AbrirVano();
                    SimRenderer.Sacudida = 0.25f;
                    if (_audio != null) { _audio.pitch = 0.7f; _audio.PlayOneShot(Audio.SintetizadorSfx.Derrumbe, 0.5f * Audio.DirectorDeAudio.VolumenEfectos); _audio.pitch = 1f; }
                    _adiosPaso = 3; _tPaso = 0f;
                    break;
                }

                case 3: // LA APERTURA: la luz revienta hacia afuera y la cámara se planta en TU centro — la presentación del mapa.
                    SimRenderer.FocoCinematico = CentroPlataformaMundo();
                    _radioForzado = Mathf.Lerp(UiStyles.S(340f), UiStyles.S(2400f), Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_tPaso / 1.3f)));
                    if (_tPaso < 2.6f) return;
                    _adiosPaso = 4; _tPaso = 0f;
                    break;

                case 4: // la palabra final: EL TÍTULO.
                    DecirConTono("DIEZ MIL AÑOS.", 0.75f, 3.4f);
                    _adiosPaso = 5; _tPaso = 0f;
                    break;

                case 5: // el desvanecer: la figura se apaga y sus motas MIGRAN al tablón — la atención cambia de sitio, no desaparece.
                {
                    _maestroPresencia = 1f - Mathf.Clamp01(_tPaso / 3.2f);
                    _motaAdiosTimer -= Time.deltaTime;
                    if (_motaAdiosTimer <= 0f && _maestroPresencia > 0f)
                    {
                        _motaAdiosTimer = 0.14f;
                        var desde = PosMaestro() + new Vector3(
                            Mathf.Sin(_tPaso * 7.3f) * 0.22f,
                            0.2f + Mathf.Abs(Mathf.Sin(_tPaso * 4.1f)) * 0.45f, 0f);
                        SoltarMotaHacia(desde, TablonMundo(), 2);
                    }
                    if (_tPaso < 3.6f) return;
                    _maestroPresencia = 0f;
                    _adiosPaso = 6; _tPaso = 0f;
                    break;
                }

                case 6: // el canal queda ABIERTO: el Trueque despierta y el mundo es tuyo.
                    if (_tPaso < 0.7f) return;
                    Trueque.Activar();
                    _radioForzado = null;
                    _focoDeLuz = null;
                    SimRenderer.FocoCinematico = null;
                    _radioObjetivo = UiStyles.S(_g.radioAmanecer);
                    Debug.Log("[TenThousandYears] EL ADIÓS: el Maestro deja su atención en el tablón; el fuego queda; el prólogo TERMINÓ.");
                    CambiarBeat(Beat.Fin);
                    break;
            }
        }

        /// <summary>(R93) EL VANO: el umbral tallado en el muro oeste — arco de 10 de fondo por 7 de alto; la piedra que sale vuela al Maestro (él la absorbe: mismo lenguaje del anillo).</summary>
        private void AbrirVano()
        {
            float celdaM = SimRenderer.CellWorldSize;
            int k = 0;
            for (int x = VanoX0; x <= VanoX1; x++)
                for (int y = VanoY0; y <= VanoY1; y++)
                {
                    // El arco: las esquinas altas se quedan de piedra.
                    if (y == VanoY1 && (x <= VanoX0 + 1 || x >= VanoX1 - 1)) continue;
                    if (y == VanoY1 - 1 && (x == VanoX0 || x == VanoX1)) continue;
                    if (_sim.Grid.GetMat(x, y) == MaterialId.Empty) continue;
                    _sim.Paint(x, y, 0, MaterialId.Empty);
                    SoltarMota(x, y, celdaM, tipo: 2, stagger: 0.018f * (k++));
                }
            Debug.Log("[TenThousandYears] EL VANO abierto (" + k + " celdas al Maestro): el espacio creció hacia el oeste.");
        }

        // ------------------------------------------------------------------
        // El frasco volando de la mesa a la mano (el TOMA. hecho imagen).
        // ------------------------------------------------------------------
        private void LanzarVueloDelFrasco()
        {
            var go = new GameObject("FrascoEnVuelo");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MaquinariaSprites.TarroDeMano(); // (R93, Cesar: "debería entregarle algo que se parezca más a lo que lleva en la mano") — el tarro redondo del aprendiz, no la redoma antigua del estante.
            sr.sortingOrder = Capas.CarryEnMano;
            float celda = SimRenderer.CellWorldSize;
            go.transform.localScale = Vector3.one * (1.4f * celda * 6f / Mathf.Max(1f, sr.sprite.rect.width));
            go.transform.position = PosMaestro();
            _frascoVuelo = go.transform;
            _tVuelo = 0f;
        }

        private void TickVueloDelFrasco()
        {
            if (_frascoVuelo == null) return;
            _tVuelo += Time.deltaTime / _g.entregaFrascoSeg;
            float t = Mathf.Clamp01(_tVuelo);
            float ease = t * t * (3f - 2f * t); // smoothstep: sale suave, llega suave.

            Vector3 a = PosMaestro();
            Vector3 b = _aprendiz.position + new Vector3(0f, -0.2f, 0f);
            Vector3 m = Vector3.Lerp(a, b, 0.5f) + new Vector3(0f, 0.7f, 0f); // comba por arriba.
            Vector3 p = Vector3.Lerp(Vector3.Lerp(a, m, ease), Vector3.Lerp(m, b, ease), ease);
            _frascoVuelo.position = p;

            if (t >= 1f)
            {
                Destroy(_frascoVuelo.gameObject);
                _frascoVuelo = null;
                _frascoEntregado = true;      // (R81, revisión Opus #15) la VERDAD de instancia: sobrevive al hot-reload; la estática es su espejo (ver Update).
                FrascoBloqueado = false;      // el tarro del aprendiz aparece: AHORA llevas frasco.
                HudPermitido = true;
                DayCycle.DespertarHudFundacion();
                _tBeat = 0f; // (revisión Opus 73 #9) _g.trasTomaRespiroSeg cuenta desde AQUÍ: es el respiro con el frasco ya en la mano, no menos el vuelo.
                _hazPresRetraso = 0.35f; // (R81) la herramienta se presenta — tras un respiro, para no competir con el HUD naciendo (revisión Opus #8).
            }
        }

        // =================================================================
        // (R81, opción 2 aprobada por Cesar; REHECHO tras la revisión Opus
        // R81 #1) EL HAZ DE PRESENTACIÓN: al aterrizar el frasco en tu mano
        // (con 0.35 s de respiro para no competir con el HUD naciendo —
        // hallazgo #8), su haz se estira UNA única vez desde la mano hasta
        // donde esté el cursor vivo, sostiene y se recoge (~2 s,
        // hazPresentacionSeg). Es "la línea jugador→cursor" que los testers
        // vetaron como permanente, convertida en gesto de nacimiento: la
        // herramienta enseña sola dónde actúa (el cursor) y hasta dónde
        // llega (se detiene en Flask.ReachWorld).
        //
        // POR QUÉ IMGUI Y NO SPRITES DE MUNDO (hallazgo #1, BLOQUEA-OBJETIVO,
        // medido): la VIÑETA de oscuridad es IMGUI y tapa por completo los
        // sprites de mundo — con radioToma≈330 el negro opaco empezaba a
        // ~3.07 u del jugador y el haz mide hasta 6 u: su punta (la parte
        // que ENSEÑA) nacía sepultada en lo negro la mayor parte de las
        // veces. Dibujado aquí, DESPUÉS de DibujarVineta en el mismo OnGUI,
        // el haz queda SOBRE la oscuridad: la lección se ve siempre. La
        // paleta sigue siendo el latón del haz real (Flask.BrassBase) y la
        // punta reutiliza la textura radial de la lucecita (misma familia
        // de resplandores del prólogo).
        // =================================================================
        private float _hazPresT = -1f;     // <0 = inactivo; [0..1] = curso.
        private float _hazPresRetraso;     // respiro entre el aterrizaje y el gesto.
        private static readonly Color HazPresColor = new Color(168f / 255f, 126f / 255f, 58f / 255f); // Flask.BrassBase.

        /// <summary>Avanza SOLO los relojes (Update; congelado por la guarda de InputLocked de arriba — en pausa el gesto espera, no se pierde).</summary>
        private void TickHazPresentacion()
        {
            if (_hazPresRetraso > 0f)
            {
                _hazPresRetraso -= Time.deltaTime;
                if (_hazPresRetraso <= 0f) _hazPresT = 0f;
                return;
            }
            if (_hazPresT < 0f) return;
            _hazPresT += Time.deltaTime / Mathf.Max(0.2f, _g.hazPresentacionSeg);
            if (_hazPresT >= 1f) _hazPresT = -1f; // se recogió: nunca vuelve.
        }

        /// <summary>El dibujo (OnGUI, tras la viñeta). Curso: 0→0.35 se ESTIRA · 0.35→0.72 SOSTIENE (~0.74 s con el default) · 0.72→1 se RECOGE.</summary>
        private void DibujarHazPresentacion()
        {
            var cam = Camera.main;
            var mouse = Mouse.current;
            if (cam == null || mouse == null) return;
            if (_lucecitaTex == null) ConstruirLucecita(); // la punta comparte el resplandor de la casa.

            float ext;
            if (_hazPresT < 0.35f) ext = Mathf.SmoothStep(0f, 1f, _hazPresT / 0.35f);
            else if (_hazPresT < 0.72f) ext = 1f;
            else ext = Mathf.SmoothStep(1f, 0f, (_hazPresT - 0.72f) / 0.28f);

            // Origen: la MANO real (CarryAnchor — hallazgo #12: el haz real
            // sale de ahí; que la presentación salga del mismo sitio).
            Vector3 origen = _aprendizCtrl != null ? _aprendizCtrl.CarryAnchor : _aprendiz.position + new Vector3(0f, -0.35f, 0f);

            // Destino: el cursor VIVO, recortado al alcance real del frasco.
            Vector2 mp = mouse.position.ReadValue();
            Vector3 destino = origen + Vector3.right;
            var ray = cam.ScreenPointToRay(new Vector3(mp.x, mp.y, 0f));
            var plano = new Plane(Vector3.forward, Vector3.zero);
            if (plano.Raycast(ray, out float enter)) destino = ray.GetPoint(enter);
            Vector3 delta = destino - origen;
            float largoMax = Mathf.Clamp(delta.magnitude, 0.15f, Flask.ReachWorld);
            Vector3 dir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.right;
            Vector3 puntaMundo = origen + dir * (largoMax * ext);

            // A pantalla (IMGUI: Y invertida).
            Vector3 o = cam.WorldToScreenPoint(origen);
            Vector3 p = cam.WorldToScreenPoint(puntaMundo);
            var oGui = new Vector2(o.x, Screen.height - o.y);
            var pGui = new Vector2(p.x, Screen.height - p.y);
            float largoPx = Vector2.Distance(oGui, pGui);
            if (largoPx < 1f) return;
            float grosorPx = Mathf.Max(2f, UiStyles.S(4f));
            float alfa = 0.85f * Mathf.SmoothStep(0f, 1f, Mathf.Min(1f, _hazPresT / 0.1f));

            float angGui = Mathf.Atan2(pGui.y - oGui.y, pGui.x - oGui.x) * Mathf.Rad2Deg;
            var prevM = GUI.matrix;
            var prevC = GUI.color;
            GUIUtility.RotateAroundPivot(angGui, oGui);
            GUI.color = new Color(HazPresColor.r, HazPresColor.g, HazPresColor.b, alfa * 0.8f);
            GUI.DrawTexture(new Rect(oGui.x, oGui.y - grosorPx * 0.5f, largoPx, grosorPx), Texture2D.whiteTexture);
            GUI.matrix = prevM;

            // La punta: el resplandor radial (no un cuadrado — hallazgo #10),
            // latiendo despacio (4.5 rad/s: respira, no tiembla).
            float lado = UiStyles.S(16f) * (1f + 0.18f * Mathf.Sin(Time.time * 4.5f));
            GUI.color = new Color(1f, 1f, 1f, alfa);
            GUI.DrawTexture(new Rect(pGui.x - lado, pGui.y - lado, lado * 2f, lado * 2f), _lucecitaTex);
            GUI.color = prevC;
        }

        // ------------------------------------------------------------------
        // La silueta del Maestro y su fuego real (conservados de la v1).
        // ------------------------------------------------------------------
        private GameObject _maestroGo;       // el visual que ESTE director creó (si lo creó).
        private Transform _maestroTr;        // dónde vive el Maestro: el marcador de escena, o el GO propio de fallback.
        private bool _maestroVisualPropio;
        private Texture2D _maestroTex;

        /// <summary>El punto del Maestro para triggers, vuelo del frasco y placas. AUTORIDAD: el marcador de ESCENA si existe; si no, el rincón histórico de la mesa.</summary>
        private Vector3 PosMaestro()
        {
            if (_maestroTr != null) return _maestroTr.position + new Vector3(0f, 0.35f, 0f);
            float celda = SimRenderer.CellWorldSize;
            return new Vector3((SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda,
                (SimLevelBuilder.FundacionMesaTopY + 4) * celda, 0f);
        }

        /// <summary>La silueta encapuchada, como PÍXELES (pura; también la consume la herramienta de horneado del editor).</summary>
        public static Texture2D ConstruirTexturaMaestro()
        {
            const int W = 12, H = 16; // 2 px por celda: 6x8 celdas en el mundo.
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
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
            tex.SetPixels32(px);
            tex.Apply(false, false); // legible: la herramienta de horneado lo exporta a PNG.
            return tex;
        }

        /// <summary>
        /// (RONDA 75) EL MAESTRO SOBRE SU MARCADOR: si la escena trae el
        /// marcador, ÉL es la autoridad de posición y escala — moverlo en el
        /// editor mueve la silueta Y los triggers, sin código. Si el marcador
        /// ya tiene un SpriteRenderer hijo (arte horneado, colocado a mano),
        /// el director no pinta nada: la escena ya vistió al Maestro. Sin
        /// marcador (escena vieja/sandbox): el GO histórico junto a la mesa.
        /// </summary>
        private void SpawnMaestro()
        {
            float celda = SimRenderer.CellWorldSize;

            if (_escena != null && _escena.maestro != null)
            {
                _maestroTr = _escena.maestro;
                if (_maestroTr.GetComponentInChildren<SpriteRenderer>() != null)
                {
                    _maestroVisualPropio = false; // la escena ya lo vistió.
                    return;
                }
            }
            else
            {
                var raiz = new GameObject("Maestro_Silueta");
                float mx = (SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * celda;
                raiz.transform.position = new Vector3(mx, (SimLevelBuilder.FundacionMesaTopY + 1) * celda, 0f);
                _maestroTr = raiz.transform;
            }

            _maestroTex = ConstruirTexturaMaestro();
            var go = new GameObject("Silueta");
            go.transform.SetParent(_maestroTr, false); // hijo: hereda posición Y escala del marcador.
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(_maestroTex, new Rect(0, 0, 12, 16), new Vector2(0.5f, 0f), 12f / (6f * celda));
            sr.sortingOrder = 30;
            _maestroGo = _escena != null && _escena.maestro != null ? go : _maestroTr.gameObject;
            _maestroVisualPropio = true;
        }

        private float DistAlMaestro()
        {
            float celda = SimRenderer.CellWorldSize;
            Vector3 p = PosMaestro();
            return Vector2.Distance(_aprendiz.position, new Vector2(p.x, p.y)) / celda;
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

            // (R81) El haz de presentación — SOBRE la oscuridad de la viñeta
            // (revisión Opus #1: como sprites de mundo nacía enterrado en lo
            // negro). En pausa no se dibuja y su reloj tampoco corre: el
            // gesto espera entero al otro lado del menú (revisión Opus #9).
            if (_hazPresT >= 0f && !DayCycle.InputLocked) DibujarHazPresentacion();

            // (R85) EL FRENTE DEL BARRIDO, VISIBLE: una columna de luz que
            // cruza la caverna de izquierda a derecha con el frente real.
            // Cesar jugó el REORDEN y "no vio el barrido": lo que la sim
            // recogía era indistinguible de nada (regla 43) — ahora el ORDEN
            // se VE pasar, aunque no hubiera una sola celda que recoger.
            if (_motas.Count > 0 && !DayCycle.InputLocked) DibujarMotas(); // (R87) la materia recogida VIAJA a los reservorios.
            if (_beat == Beat.Reorden && !DayCycle.InputLocked)
                DibujarHaloMaestro();         // (R90) el Maestro se hincha tragando.
            if ((_beat == Beat.Reorden || _beat == Beat.Adios) && !DayCycle.InputLocked)
                DibujarFlash();               // el chasquido del pulso — también en el ORDEN repetido del adiós (R93).

            // (R81) El aro de la boca del frasco: acompaña el paso de aspirar
            // (y la ficha-recuerdo). Calla ante TODO modo que apague el
            // frasco (revisión Opus #5: cincel, mudanza, álbum, paleta dev,
            // bautizo — si el frasco no actúa, el aro mentiría).
            if (_aroActivo && !FrascoBloqueado && !DayCycle.InputLocked
                && !Alkahest.Dev.DevPalette.IsOpen && !UiStyles.EscribiendoTexto
                && !JournalHud.Abierto && !AlbumReal.Abierto
                && !Cincel.ModoActivo && !Mudanza.ModoActivo)
                DibujarAroDeLaBoca();

            // (R79) La lucecita del área del Maestro durante el VEN.: el
            // indicador pedido por Cesar ("una luz diminuta desde esa área,
            // como que algo está ocurriendo ahí") — sustituye al bias de la
            // viñeta como señal de rumbo. Sobre la capa oscura, así se ve
            // aunque el jugador esté lejos.
            if (_beat == Beat.Ven && !DayCycle.InputLocked) DibujarLucecitaMaestro();
            // (R82) La lucecita de la poza se RETIRÓ — ver la nota junto a
            // DibujarLucecitaMaestro (enseñaba una mecánica inexistente).

            // La chapa "EL MAESTRO" sobre la mesa (se oculta de cerca: de ahí
            // en adelante hablan la silueta y la voz).
            // (R79, feedback de Cesar: "no se debería leer 'el Maestro' como
            // texto hasta después que hable") También se oculta durante el
            // DESPERTAR entero: la primera vez que lees su nombre es cuando
            // su voz ya sonó (el VEN. entra junto con la lucecita de arriba).
            if (_beat != Beat.Despertar && _beat != Beat.Fin && DistAlMaestro() >= 14f && !DayCycle.InputLocked && _maestroPresencia > 0.5f)
            {
                var ancla = PosMaestro() + new Vector3(0f, 7f * celda, 0f); // sobre la capucha, siga donde siga el marcador.
                float alfa = LuzEn(ancla) * 0.85f;
                UiStyles.PlacaMundo(ancla, "EL MAESTRO", new Color(0.92f, 0.86f, 0.7f, alfa), UiStyles.S(10f));
            }

            // La placa del CUENCO: solo mientras hay una entrega pendiente —
            // el gesto ya lo sabes; esta placa solo dice DÓNDE y CUÁNTO.
            if ((_beat == Beat.EntregaAgua || _beat == Beat.EntregaLodo) && !_drenando && !DayCycle.InputLocked)
            {
                byte mat = _beat == Beat.EntregaAgua ? MaterialId.Water : Lodo;
                int meta = _beat == Beat.EntregaAgua ? _g.entregaAguaMeta : _g.entregaLodoMeta;
                int n = Mathf.Min(ContarEnCuenco(mat), meta);
                var ancla = new Vector3((SimLevelBuilder.FundacionCuencoX0 + SimLevelBuilder.FundacionCuencoX1) * 0.5f * celda,
                    (SimLevelBuilder.FundacionY0 + 3) * celda, 0f);
                float alfa = Mathf.Max(LuzEn(ancla), 0.6f);
                UiStyles.PlacaMundo(ancla, "AQUÍ — " + n + " / " + meta, new Color(0.95f, 0.88f, 0.68f, alfa), UiStyles.S(9f));
            }

            // (R74) La placa del DEPÓSITO durante el LLÉNALO. final: mismo
            // lenguaje que la del cuenco — dónde y cuánto, nada más.
            // (R83, revisión Opus A5) ...salvo cuando la meta está BLOQUEADA
            // por materia ajena: entonces la placa dice la verdad y la
            // salida ("sobra X — aspíralo": la purga ES el frasco). Solo
            // habla si la meta es matemáticamente imposible con el hueco que
            // queda — gateado por guion (placaAvisaEstorbo) por si Cesar
            // decide apagarlo también en esta forma mínima.
            if (_beat == Beat.LlenarDeposito && !DayCycle.InputLocked)
                DibujarPlacaRecipiente(_deposito, _g.llenarDepositoMeta, "sobra lo que NO es agua — aspíralo");

            // (R83) La placa del SILO durante su LLÉNALO. de barro.
            if (_beat == Beat.LlenarDeposito2 && !DayCycle.InputLocked)
                DibujarPlacaRecipiente(_silo, _g.llenarDeposito2Meta, "sobra lo que NO es lodo — aspíralo");

            // (R93) La placa de la OBRA: dónde y cuánto, el lenguaje del
            // cuenco y los recipientes — una para la plataforma, otra para
            // el techo herido si sigue pendiente.
            if (_beat == Beat.Obra && _obraFase == 1 && !DayCycle.InputLocked)
            {
                int hp = ContarCubiertas(_obraPlatObjetivo);
                if (hp < _obraPlatObjetivo.Count)
                {
                    var anclaP = CentroPlataformaMundo() + new Vector3(0f, -3f * celda, 0f);
                    UiStyles.PlacaMundo(anclaP, "ALZA — " + hp + " / " + _obraPlatObjetivo.Count,
                        new Color(0.95f, 0.88f, 0.68f, 0.85f), UiStyles.S(9f));
                }
                int ht = ContarCubiertas(_obraTechoObjetivo);
                if (_obraTechoObjetivo.Count > 0 && ht < _obraTechoObjetivo.Count)
                {
                    var anclaT = new Vector3((ObraTechoX0 + ObraTechoX1 + 1) * 0.5f * celda, (ObraTechoY0 - 2) * celda, 0f);
                    UiStyles.PlacaMundo(anclaT, "EL TECHO — " + ht + " / " + _obraTechoObjetivo.Count,
                        new Color(0.95f, 0.88f, 0.68f, 0.85f), UiStyles.S(9f));
                }
            }

            // (R93) La placa del ACOMODO: cuántos reservorios ya están en casa.
            if (_beat == Beat.Acomodo && _acomodoFase == 1 && !DayCycle.InputLocked)
            {
                int n = (_slotAOcupado ? 1 : 0) + (_slotBOcupado ? 1 : 0);
                var ancla = CentroPlataformaMundo() + new Vector3(0f, 9f * celda, 0f);
                UiStyles.PlacaMundo(ancla, "AQUÍ — " + n + " / 2",
                    new Color(0.95f, 0.88f, 0.68f, 0.85f), UiStyles.S(9f));
            }

            if (!DayCycle.InputLocked) DibujarVoz();
        }

        /// <summary>
        /// (R83) La placa de un recipiente con meta: "LLÉNALO — n/m", y la
        /// línea honesta SOLO si la meta quedó imposible por estorbo (el
        /// hueco libre + lo del dueño no alcanza la meta). Compartida por el
        /// tanque y el silo — un solo lenguaje para toda la familia.
        /// </summary>
        private void DibujarPlacaRecipiente(DepositoDeAgua rec, int meta, string lineaEstorbo)
        {
            if (rec == null) return;
            float celda = SimRenderer.CellWorldSize;
            int n = Mathf.Min(rec.DelDueno(), meta);
            var ancla = rec.CentroMundo() + new Vector3(0f, 7f * celda, 0f);
            float alfa = Mathf.Max(LuzEn(ancla), 0.6f);
            var color = new Color(0.95f, 0.88f, 0.68f, alfa);
            UiStyles.PlacaMundo(ancla, "LLÉNALO — " + n + " / " + meta, color, UiStyles.S(9f));

            if (_g.placaAvisaEstorbo)
            {
                int hueco = rec.Capacidad() - rec.Ocupado();
                if (rec.DelDueno() + hueco < meta)
                {
                    var ancla2 = ancla + new Vector3(0f, -3.2f * celda, 0f);
                    UiStyles.PlacaMundo(ancla2, lineaEstorbo, new Color(0.95f, 0.7f, 0.55f, alfa), UiStyles.S(8f));
                }
            }
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
            var r = new Rect(0f, Screen.height * _g.vozAlturaFrac - deriva, Screen.width, Screen.height * 0.12f);

            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f * alfa);
            GUI.Label(new Rect(r.x + UiStyles.S(2f), r.y + UiStyles.S(3f), r.width, r.height), texto, _vozStyle);
            GUI.color = new Color(0.94f, 0.89f, 0.78f, alfa);
            GUI.Label(r, texto, _vozStyle);
            GUI.color = prev;
        }

        private static GUIStyle _vozStyle;
        private static int _vozAltoPx;
        private void PrepararEstiloVoz()
        {
            int objetivo = Mathf.RoundToInt(Screen.height * _g.vozTamFrac);
            if (_vozStyle != null && _vozAltoPx == objetivo) return;
            _vozAltoPx = objetivo;
            UiStyles.Preparar();
            _vozStyle = new GUIStyle(UiStyles.Titulo)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = objetivo,
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
            // (R88) _focoDeLuz desacopla la LUZ de la CÁMARA: en el anillo
            // del ORDEN la cámara se planta al centro y la luz nace del
            // MAESTRO — dos instrumentos, dos manos (dirección Opus).
            _focoActual = _focoDeLuz
                ?? SimRenderer.FocoCinematico
                ?? Vector3.Lerp(brasas, _aprendiz.position, _focoBias);
            Vector3 p = cam.WorldToScreenPoint(_focoActual);
            float cx = p.x, cy = Screen.height - p.y;

            float r = _radio * (_radioForzado.HasValue ? 1f : _luzFuego); // (R88) el filo del anillo no tirita con la llama.
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

        // =================================================================
        // (R81, opción 3 aprobada por Cesar; acotado por la revisión Opus
        // #2) EL ARO DE LA BOCA: un anillo tenue en el cursor, SOLO durante
        // el paso de ASPIRAR (y su ficha-recuerdo, #14) — en el verter
        // mentiría: el radio real del vertido es otro (PourRadius=2) y el
        // tarro ladeándose hacia el cursor ya es señal de sobra. Marca el
        // anillo de succión REAL (Flask.SuckRadiusWorld, leído — no
        // duplicado) convertido a pantalla: el aro dice la verdad, no
        // decora. Late los primeros segundos y se aquieta; desaparece al
        // confirmarse el aspirar (la lección quedó dada: enseñar y callar).
        // La retícula del FlaskHud (depth 0, encima) queda anillada por él:
        // mira + aro = "la acción es AQUÍ y abarca ESTO".
        // =================================================================
        private bool _aroActivo;
        private float _aroVida; // segundos desde la activación: el latido se calma con ella (revisión Opus #13).
        private Texture2D _aroTex;

        private void DibujarAroDeLaBoca()
        {
            var cam = Camera.main;
            var mouse = Mouse.current;
            if (cam == null || mouse == null) return;
            if (_aroTex == null) ConstruirAro();

            Vector2 mp = mouse.position.ReadValue();
            var gui = new Vector2(mp.x, Screen.height - mp.y);

            // Radio en pantalla = el anillo COMPLETO de succión real
            // (Flask.SuckRadiusWorld: radio+0.5 celdas — el disco euclídeo
            // aspira hasta d≈4.49) medido con la cámara de hoy, corregido
            // para que la BANDA visible de la textura (centro en d≈0.92 del
            // rect) caiga exactamente sobre ese anillo (revisión Opus #7).
            Vector3 a = cam.WorldToScreenPoint(Vector3.zero);
            Vector3 b = cam.WorldToScreenPoint(new Vector3(Flask.SuckRadiusWorld / 0.92f, 0f, 0f));
            float r = Mathf.Abs(b.x - a.x);
            if (r < 8f) r = 8f;

            // ¿La boca está donde el frasco DE VERDAD llega? Fuera de alcance
            // el aro se enfría y apaga a juego con la retícula roja del
            // FlaskHud (revisión Opus #6: dos señales, un solo mensaje).
            bool enAlcance = true;
            var ray = cam.ScreenPointToRay(new Vector3(mp.x, mp.y, 0f));
            var plano = new Plane(Vector3.forward, Vector3.zero);
            if (plano.Raycast(ray, out float enter))
            {
                Vector3 mundo = ray.GetPoint(enter);
                enAlcance = (mundo - _aprendiz.position).sqrMagnitude <= Flask.ReachWorld * Flask.ReachWorld;
            }

            // El latido llama la atención los primeros segundos y luego se
            // AQUIETA (amplitud con decaimiento): un anillo pulsando minutos
            // sería la línea permanente que los testers vetaron, por la
            // puerta de atrás (revisión Opus #13; mandato pt44).
            float amp = 0.15f * Mathf.Exp(-_aroVida / 4f);
            float lat = (1f - amp) + amp * Mathf.Sin(Time.time * 3.2f);

            var prev = GUI.color;
            GUI.color = enAlcance
                ? new Color(1f, 1f, 1f, _g.aroAlfa * lat)
                : new Color(1f, 0.55f, 0.5f, _g.aroAlfa * lat * 0.45f);
            GUI.DrawTexture(new Rect(gui.x - r, gui.y - r, r * 2f, r * 2f), _aroTex);
            GUI.color = prev;
        }

        private void ConstruirAro()
        {
            const int N = 128;
            _aroTex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            _aroTex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[N * N];
            float half = N * 0.5f;
            // Anillo cálido de bordes suaves: banda en d∈[0.86, 0.98] del radio.
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.84f, 0.90f, d))
                            * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.94f, 1f, d)));
                    px[y * N + x] = new Color32(255, 235, 190, (byte)(a * 255f)); // el mismo ámbar de la casa.
                }
            }
            _aroTex.SetPixels32(px);
            _aroTex.Apply(false, true);
        }

        // =================================================================
        // (R79) LA LUCECITA DEL MAESTRO: un resplandor diminuto y cálido
        // sobre el hogar de brasas, visible DURANTE el VEN. La viñeta pinta
        // oscuridad encima del mundo, así que un "segundo agujero" real no
        // se puede recortar por alfa — pero componer un glow cálido ENCIMA
        // de la capa oscura lee igual: un ascua lejana en la penumbra.
        // Parpadea con _luzFuego (la misma llama que ya calcula el fuego) y
        // entra en fade con la palabra. Radio y alfa viven en el guion.
        // =================================================================
        private Texture2D _lucecitaTex;

        private void DibujarLucecitaMaestro()
        {
            // (R82, Cesar tras jugar) La lucecita es un DESTELLO, no una
            // lámpara: nace con la palabra, vive ~lucecitaVidaSeg y se
            // desvanece — después la chapa "EL MAESTRO" queda de referencia.
            // (Y a mitad de intensidad: lucecitaAlfa 0.6→0.3 en el guion.)
            float subida = Mathf.Clamp01(_tBeat / 0.25f);
            float bajada = 1f - Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((_tBeat - 0.3f) / Mathf.Max(0.2f, _g.lucecitaVidaSeg - 0.3f)));
            float envolvente = subida * bajada;
            if (envolvente <= 0.01f) return;

            float celda = SimRenderer.CellWorldSize;
            var brasas = new Vector3((SimLevelBuilder.FundacionBrasasX0 + 2.5f) * celda,
                (SimLevelBuilder.FundacionBrasasY + 2) * celda, 0f);
            DibujarLucecitaEn(brasas, envolvente);
        }

        // (R82, RETIRADA — regla 15) LA LUCECITA DE LA POZA (R81, revisión
        // Opus #3) se quitó tras el playtest de Cesar: el vuelo del frasco
        // ya lee como "atrapar una luz que agranda tu visión", y una SEGUNDA
        // luz sobre el agua enseñaba una mecánica inexistente ("es un
        // plataformero de atrapar luces"). El hueco TOMA.→AGUA. queda
        // cubierto por el rumor de la cascada (audible desde el spawn tras
        // el recalibrado R81) y por el radio de luz ya crecido. Si el
        // playtest final muestra que falta guía, la alternativa NO es otra
        // luz: es un reflejo/brillo EN el agua misma (materia, no premio).

        private void DibujarLucecitaEn(Vector3 mundo, float entrada01)
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (_lucecitaTex == null) ConstruirLucecita();

            Vector3 p = cam.WorldToScreenPoint(mundo);
            if (p.z < 0f) return;
            float cx = p.x, cy = Screen.height - p.y;

            float entrada = Mathf.SmoothStep(0f, 1f, entrada01);
            float alfa = _g.lucecitaAlfa * entrada * Mathf.Lerp(0.75f, 1f, _luzFuego);
            float r = UiStyles.S(_g.lucecitaRadioPx) * Mathf.Lerp(0.92f, 1f, _luzFuego);

            var prev = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, alfa);
            GUI.DrawTexture(new Rect(cx - r, cy - r * 0.8f, r * 2f, r * 1.6f), _lucecitaTex);
            GUI.color = prev;
        }

        private void ConstruirLucecita()
        {
            const int N = 96;
            _lucecitaTex = new Texture2D(N, N, TextureFormat.RGBA32, false);
            _lucecitaTex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[N * N];
            float half = N * 0.5f;
            // Ámbar de brasa: núcleo casi blanco, halo naranja que muere en 0.
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                    float a = 1f - Mathf.SmoothStep(0f, 1f, d);        // cae suave hacia el borde.
                    a *= a;                                             // núcleo concentrado, halo tenue.
                    Color c = Color.Lerp(new Color(1f, 0.93f, 0.72f), new Color(1f, 0.55f, 0.2f), Mathf.Clamp01(d * 1.4f));
                    px[y * N + x] = new Color32((byte)(c.r * 255f), (byte)(c.g * 255f), (byte)(c.b * 255f), (byte)(a * 255f));
                }
            }
            _lucecitaTex.SetPixels32(px);
            _lucecitaTex.Apply(false, true);
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
