using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;
using Alkahest.Game;

namespace Alkahest.Audio
{
    /// <summary>
    /// EL TALLER SUENA: MonoBehaviour que decide QUÉ suena y CUÁNDO, con un
    /// pool FIJO de AudioSource creado en <see cref="Init"/> (llamado desde
    /// Game/AlkahestGameBootstrap.cs, mismo patrón de inyección de
    /// dependencias que el resto de Game/). Cero AudioSource nuevos en
    /// runtime, cero asignaciones por frame: todo el estado vive en arrays y
    /// campos fijados de antemano.
    ///
    /// =====================================================================
    /// APAGADO TOTAL DE UN SOLO SITIO
    /// =====================================================================
    /// <see cref="SistemaActivo"/> es el ÚNICO interruptor: si el primer
    /// playtest suena mal, cambiarlo a `false` desactiva TODO el componente
    /// en Awake (no se crea ni un AudioSource) sin tocar ningún otro archivo
    /// ni revertir nada.
    ///
    /// =====================================================================
    /// CÓMO SE ENTERA DE LO QUE PASA (sin tocar Sim/ ni los archivos de solo
    /// lectura de Game/)
    /// =====================================================================
    ///  · IGNICIÓN / CRISTALIZACIÓN / CONGELACIÓN: se consume el ring buffer
    ///    de <see cref="SimStepper.Events"/> con un puntero DE LECTURA
    ///    PROPIO (<see cref="_ultimoEventoLeido"/>), exactamente igual que
    ///    hace Game/SubstanceKnowledge.cs (que ya lo consume). El array es
    ///    de solo lectura para ambos consumidores -- SimStepper.PushEvent
    ///    solo AVANZA la cabeza y sobrescribe lo más viejo si el buffer se
    ///    llena, nunca "borra" ni descuenta nada -- así que dos lectores
    ///    con su propio índice NO se roban eventos entre sí. No hace falta
    ///    ninguna alternativa: el consumo NO es destructivo.
    ///  · ASPIRAR/VERTER: Flask.Total es público; se observa su DELTA entre
    ///    frames (sube = aspirar, baja = verter) en vez de interceptar el
    ///    ratón -- Flask es de solo lectura para esta tarea.
    ///  · GRIFO ABIERTO: Dispenser no expone si está encendido (campo
    ///    privado, archivo de solo lectura), así que en vez de preguntarle
    ///    se OBSERVA EL ESTADO DE LA SIMULACIÓN: se muestrea si el material
    ///    del grifo sigue apareciendo en su boquilla (misma idea que
    ///    Game/SubstanceKnowledge.cs con el hover). La posición de la
    ///    boquilla replica la geometría privada de Dispenser
    ///    (SpoutOffsetCells/SpoutDropCells/SpoutRadius, ver comentario en
    ///    <see cref="ConstruirVocesGrifo"/>): acoplamiento aceptado y
    ///    documentado, degrada con gracia (el grifo se queda mudo, nada
    ///    revienta) si esa geometría cambia algún día.
    ///  · FUEGO: contar las 36.864 celdas cada frame está prohibido (ver
    ///    encargo), así que se muestrea un conjunto FIJO de sondas
    ///    aleatorias (<see cref="ConstruirSondasFuego"/>, calculado una vez)
    ///    y se extrapola una intensidad 0..1 a partir de cuántas sondas caen
    ///    sobre Fire.
    ///  · LA TOLVA TRAGA: se observa si el "sillar" de la boca (constantes
    ///    públicas de Sim/SimLevelBuilder, la única fuente de verdad del
    ///    plano del taller) pasa de vacío a ocupado -- flanco de subida,
    ///    sin tocar DeliveryChute/OrderSystem.
    ///  · BAUTIZAR: SubstanceKnowledge.CountNamed() es público; se dispara
    ///    al subir.
    ///  · ENCARGO COMPLETADO: OrderSystem.CompletedCount() es público; se
    ///    dispara al subir (y se resincroniza solo al cambiar de jornada,
    ///    ver comentario en <see cref="ActualizarPollerEncargos"/>).
    ///  · FIN DE JORNADA: DayCycle.InputLocked es público y pasa a `true`
    ///    exactamente cuando Playing termina (entra en DayEnd) -- se usa el
    ///    FLANCO DE SUBIDA de ese booleano. No distingue "fin de jornada 3"
    ///    de "fin de jornada 1/2" (ambas cruzan DayEnd), que es justo lo que
    ///    queremos: una campana de cierre cada vez.
    ///
    /// =====================================================================
    /// LIMITACIÓN DE RITMO
    /// =====================================================================
    /// Ver <see cref="Limitador"/> y <see cref="DispararLimitado"/>: como
    /// mucho N sonidos por segundo de cada tipo; los eventos de más en esa
    /// ventana NO se pierden del todo, sino que empujan el volumen del
    /// SIGUIENTE sonido permitido un poco hacia arriba (nunca más fuerte que
    /// el tope de mezcla del propio clip) -- una avalancha de cristalización
    /// se lee como "avalancha" (un puñado de campanillas algo más
    /// presentes), no como una ametralladora de 200 disparos/s.
    /// </summary>
    public sealed class DirectorDeAudio : MonoBehaviour
    {
        // === INTERRUPTOR MAESTRO — ver doc de la clase. ===
        private const bool SistemaActivo = true;

        // -----------------------------------------------------------------
        // Mezcla / pool
        // -----------------------------------------------------------------
        private const int VocesOneShot = 8;
        private const float VolumenMaestroPorDefecto = 0.5f; // (encargo) "Volumen maestro por defecto 0.5".
        private const string PrefKeySilenciado = "ChaosAlchemy_AudioSilenciado";

        // ===================================================================
        // PRESUPUESTO DE MEZCLA (fix playtest 9, causa 1c del popeo)
        // ===================================================================
        // Unity SUMA todas las voces (bucles + one-shots) y recorta a [-1,1]. El
        // jugador reportó que el popeo "se nota más al cruzar con otro sonido" --
        // eso es la firma de un recorte por saturación, no (solo) una costura de
        // bucle. Estas son las cifras reales, PRE-factor-maestro (0.5 por
        // defecto: margen extra, no parte del presupuesto), de "pico de clip
        // normalizado (en SintetizadorSfx.cs) × volumen de diseño (aquí)" --
        // el máximo instantáneo que cada voz puede aportar a la suma:
        //
        //   Ambiente (bucle, "la sala")....... 0.28 × VolAmbienteObjetivo 0.30 = 0.084
        //   Fuego a tope (bucle)............... 0.50 × VolFuegoMax       0.42 = 0.210
        //   Grifo líquido abierto (bucle)...... 0.42 × VolGrifoBase      0.55 = 0.231
        //   Grifo gas abierto (bucle).......... 0.26 × VolGrifoBase      0.55 = 0.143
        //     -- suma de bucles (ambiente + fuego a tope + 2 grifos a la vez,
        //        el caso realista más cargado) ................................ 0.668
        //   One-shot típico más fuerte (Ignición, sin avalancha)
        //     0.62 × volumenBase 0.30 × (hasta ×1.1 de variación) ~........... 0.205
        //     -- total sin esquivar (ver más abajo) ~0.87: por debajo de 1.0,
        //        NO satura, pero por encima del margen de 0.8 pedido.
        //
        // ESQUIVE (ducking, causa 1c): en vez de bajar aún más los bucles a un
        // volumen insípido en el caso normal, se agachan solo MIENTRAS suena un
        // one-shot (ver EsquiveOneShotNivel/_esquiveSuavizado) -- con el esquive
        // activo, "suma de bucles" baja a 0.668×0.55=0.367, y 0.367+0.205=0.572,
        // sobra margen incluso frente a una avalancha de 3 Cristalizar/Congelar
        // solapadas con boost MÁXIMO del limitador (DispararLimitado ya deja el
        // volumenBase de diseño 0.15 en 0.15×2=0.30 con la avalancha a tope; por
        // voz: pico 0.42 × volumenBase-ya-boosteado 0.30 × variación ×1.1 ≈
        // 0.139; tres solapadas ≈ 0.417): 0.367+0.417=0.784, justo bajo el tope
        // de ~0.8 pedido en el encargo.
        // ===================================================================

        // Volúmenes de diseño (0..1) ANTES del factor maestro/silencio.
        // Deliberadamente bajos: "muy por debajo del umbral de molestia" y,
        // ahora, dentro del presupuesto de mezcla de arriba.
        private const float VolAmbienteObjetivo = 0.30f; // (fix playtest 9) bajado de 0.55: "la sala", casi subliminal -- ver presupuesto.
        private const float VolFuegoMax = 0.42f;          // (fix playtest 9) bajado de 0.5.
        private const float VolGrifoBase = 0.55f;         // (fix playtest 9) bajado de 0.6.

        // Ducking (esquive) de los BUCLES mientras suena un one-shot (fix
        // playtest 9, causa 1c "considera además bajar un poco el volumen de los
        // bucles..."): sin plugins, solo interpolación hacia un nivel objetivo
        // que se re-arma en cada one-shot. Ver ActualizarEsquive/ReproducirOneShot.
        private const float EsquiveOneShotNivel = 0.55f;   // cuánto bajan los bucles mientras hay un one-shot reciente.
        private const float EsquiveOneShotDuracionSeg = 0.30f; // cuánto se mantiene agachado tras el ÚLTIMO one-shot.

        // Rampa compartida (fix playtest 9, causa 1d): tanto el esquive como la
        // respuesta del factor maestro (tecla M) a los BUCLES se mueven con esta
        // velocidad en vez de saltar de golpe -- un salto de volumen instantáneo
        // en una fuente que sigue sonando (nunca se para/arranca en seco, pero
        // SÍ podía cambiar de volumen en seco) también suena a clic, aunque el
        // AudioSource nunca se pare. 8f cubre el rango típico (0..1 esquive,
        // 0..VolumenMaestroPorDefecto factor maestro) en 60-70ms: dentro de la
        // ventana de 40-80ms pedida en el encargo.
        private const float RampaVolumenBuclesPorSeg = 8f;

        private const float IntervaloSondeo = 1f / 12f; // ~12Hz: barato y suficientemente responsivo para audio.

        // -----------------------------------------------------------------
        // Detección de fuego (fix playtest 9, causa 3 "¿llega a sonar?"): con
        // solo 220 sondas fijas sobre 256x144=36864 celdas, y sabiendo que
        // Sim/SimStepper.ProcessFire apaga el fuego que no toca combustible en
        // 1 solo tick (ver CLAUDE.md regla del fuego), la probabilidad de que
        // CUALQUIER sonda caiga sobre Fire en un instante dado era casi
        // siempre cero salvo con un incendio literalmente descomunal (para que
        // SaturacionFuego=20 se cumpliera hacían falta ~3300 celdas en llamas
        // a la vez, ~9% de la grilla ENTERA) -- por eso "no sonaba a fuego":
        // no es que el timbre estuviera mal, es que el volumen casi nunca
        // salía de 0. Subir NumSondasFuego ayuda, pero el fix real es no
        // exigir tantos impactos (SaturacionFuego mucho más bajo) y no tirar
        // el objetivo a 0 en cuanto un sondeo puntual no pilla nada (ver
        // SondearFuego: ataque instantáneo con piso audible + liberación lenta).
        // -----------------------------------------------------------------
        private const int NumSondasFuego = 700; // subido de 220: sigue siendo barato (12Hz × 700 lecturas de array = nada).
        private const float SaturacionFuego = 5f; // (fix playtest 9) bajado de 20: un fuego de tamaño normal ya satura el "extra" por encima del piso audible.
        private const float PisoAudibleFuego = 0.42f; // (fix playtest 9) en cuanto se detecta AUNQUE SEA una sola celda de Fire, el objetivo salta ya a un nivel audible en vez de arrastrarse desde 0 -- el fuego real casi nunca "crece" para las sondas, o está vivo o no se pilla (ver comentario de arriba).
        private const float LiberacionFuegoSeg = 2.0f; // tiempo en pasar de objetivo 1 a 0 cuando deja de detectarse -- evita que el volumen parpadee entre sondeos consecutivos que no pillan nada por puro muestreo.

        // Réplica de la geometría PRIVADA de Game/Dispenser.cs (archivo de
        // solo lectura para esta tarea): SpoutOffsetCells, SpoutDropCells,
        // SpoutRadius. Ver doc de la clase, apartado "GRIFO ABIERTO".
        private const int GrifoSpoutOffsetCells = 5;
        private const int GrifoSpoutDropCells = 2;

        // Radio audible de un grifo (unidades de mundo) para la caída de
        // volumen por distancia al aprendiz.
        private const float GrifoRadioAudible = 7f;

        // -----------------------------------------------------------------
        // Referencias inyectadas (ver Init).
        // -----------------------------------------------------------------
        private AlkahestSim _sim;
        private OrderSystem _orderSystem;
        private SubstanceKnowledge _knowledge;
        private Flask _flask;
        private Transform _jugador;

        // -----------------------------------------------------------------
        // Pool de voces.
        // -----------------------------------------------------------------
        private AudioSource[] _vocesOneShot;
        private int _siguienteVoz;

        private AudioSource _fuenteAmbiente;
        private AudioSource _fuenteFuego;
        private AudioLowPassFilter _filtroFuego;

        private struct VozGrifo
        {
            public Dispenser dispenser;
            public AudioSource fuente;
            public byte matId;
            public int spoutX, spoutY;
            public bool objetivoFluyendo;
            public float volumenSuavizado;
        }
        private VozGrifo[] _grifos;

        // Sondas fijas de muestreo de fuego (ver doc de la clase).
        private int[] _sondaX;
        private int[] _sondaY;
        private float _intensidadFuegoObjetivo;
        private float _intensidadFuegoSuavizada;

        // -----------------------------------------------------------------
        // Estado de los "pollers" (comparar con el frame/sondeo anterior).
        // -----------------------------------------------------------------
        private int _totalFrascoAnterior;
        private int _materialesBautizadosAnterior;
        private int _encargosCompletadosAnterior;
        private bool _entradaBloqueadaAnterior;
        private bool _tolvaOcupadaAnterior;
        private float _acumuladorSondeo;

        private float _volumenAmbienteSuavizado;

        // -----------------------------------------------------------------
        // Esquive (ducking) de bucles + rampa del factor maestro -- fix
        // playtest 9, causas 1c/1d. Ver ActualizarEsquive/ReproducirOneShot.
        // -----------------------------------------------------------------
        private float _esquiveSuavizado = 1f; // 1 = bucles a su volumen normal; EsquiveOneShotNivel = agachado.
        private float _esquiveHasta;           // Time.time hasta el que se mantiene agachado (se re-arma en cada one-shot).
        private float _factorMaestroSuavizado; // arranca en 0 a propósito (fade-in natural al arrancar la partida, coherente con _volumenAmbienteSuavizado).

        // -----------------------------------------------------------------
        // Limitador de ritmo por tipo de evento (ver doc de la clase).
        // -----------------------------------------------------------------
        private struct Limitador
        {
            public float proximoPermitido;
            public int suprimidos;
        }
        private Limitador _limIgnicion;
        private Limitador _limCristalizarCongelar;
        private Limitador _limAspirar;
        private Limitador _limVerter;
        private Limitador _limTolva;

        private int _ultimoEventoLeido;

        // Variación de pitch/volumen de one-shots: System.Random LOCAL (capa
        // de presentación, ver SintetizadorSfx.cs; nunca UnityEngine.Random,
        // pero tampoco hace falta que sea determinista entre sesiones).
        private readonly System.Random _rngVariacion = new System.Random(unchecked(System.Environment.TickCount * 31 + 7));

        // Aviso breve en pantalla al silenciar/restaurar (M5 audio).
        private bool _silenciado;
        private string _avisoTexto;
        private float _avisoHasta;

        private float FactorMaestro => _silenciado ? 0f : VolumenMaestroPorDefecto;

        // Guard intencional sobre una constante (mismo criterio que el guard de
        // CHUNK en Sim/SimRenderer.cs): con SistemaActivo=true el compilador
        // marca el bloque de abajo como inalcanzable (CS0162), pero es
        // justamente EL INTERRUPTOR que alguien cambiará a `false` si el primer
        // playtest suena mal -- no es código muerto de verdad.
#pragma warning disable 0162
        private void Awake()
        {
            if (!SistemaActivo)
            {
                enabled = false;
                return;
            }
            _silenciado = PlayerPrefs.GetInt(PrefKeySilenciado, 0) == 1;
        }

        /// <summary>
        /// Inyección de dependencias desde Game/AlkahestGameBootstrap.cs
        /// (mismo patrón que TODO el resto de Game/). Se hace aquí y no en
        /// Awake porque, como el resto del proyecto, necesitamos referencias
        /// que el bootstrap crea en el mismo frame (grifos, frasco...) --
        /// Awake ya corrió, pero eso da igual: nada de lo de abajo depende
        /// de haber corrido antes que otro Awake.
        /// </summary>
        public void Init(AlkahestSim sim, OrderSystem orderSystem, SubstanceKnowledge knowledge,
            Flask flask, Transform jugador, Dispenser[] dispensers)
        {
            if (!SistemaActivo) return;

            _sim = sim;
            _orderSystem = orderSystem;
            _knowledge = knowledge;
            _flask = flask;
            _jugador = jugador;

            EnsureListener();
            ConstruirVocesOneShot();
            ConstruirVocesBucle();
            ConstruirVocesGrifo(dispensers);
            ConstruirSondasFuego();

            // Línea base: evita disparar sonidos "de bienvenida" falsos si el
            // jugador ya llevaba algo bautizado/completado al crear el
            // director (no debería pasar en el flujo normal, pero es gratis
            // protegerse).
            _totalFrascoAnterior = _flask != null ? _flask.Total : 0;
            _materialesBautizadosAnterior = _knowledge != null ? _knowledge.CountNamed() : 0;
            _encargosCompletadosAnterior = _orderSystem != null ? _orderSystem.CompletedCount() : 0;
            _entradaBloqueadaAnterior = DayCycle.InputLocked;
        }
#pragma warning restore 0162

        /// <summary>El AudioListener normalmente vive en Camera.main (SimRenderer ya la configura); si por lo que sea no hay ninguno, se añade uno defensivamente. Nada debe petar si tampoco hay cámara.</summary>
        private void EnsureListener()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (cam.GetComponent<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();
        }

        private void ConstruirVocesOneShot()
        {
            _vocesOneShot = new AudioSource[VocesOneShot];
            for (int i = 0; i < VocesOneShot; i++)
            {
                var go = new GameObject("VozOneShot_" + i);
                go.transform.SetParent(transform, false);
                var fuente = go.AddComponent<AudioSource>();
                fuente.playOnAwake = false;
                fuente.loop = false;
                fuente.spatialBlend = 0f; // 2D: cámara ortográfica fija que encuadra el taller entero (mismo criterio que SubstanceKnowledge sobre "vista de cámara").
                _vocesOneShot[i] = fuente;
            }
        }

        private AudioSource CrearFuenteBucle(string nombre, AudioClip clip)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            var fuente = go.AddComponent<AudioSource>();
            fuente.clip = clip;
            fuente.loop = true;
            fuente.playOnAwake = false;
            fuente.spatialBlend = 0f;
            fuente.volume = 0f; // arranca en silencio: se anima desde Update en cuanto haya algo que contar.
            fuente.Play();
            return fuente;
        }

        private void ConstruirVocesBucle()
        {
            _fuenteAmbiente = CrearFuenteBucle("Bucle_Ambiente", SintetizadorSfx.LechoAmbiental);

            _fuenteFuego = CrearFuenteBucle("Bucle_Fuego", SintetizadorSfx.FuegoBucle);
            _filtroFuego = _fuenteFuego.gameObject.AddComponent<AudioLowPassFilter>();
            _filtroFuego.cutoffFrequency = 800f;
        }

        /// <summary>Un AudioSource en bucle por grifo, con el timbre según el arquetipo de su material (líquido/polvo/gas -- ver doc de la clase). `dispensers` puede venir vacío o con huecos null sin que nada rompa.</summary>
        private void ConstruirVocesGrifo(Dispenser[] dispensers)
        {
            if (dispensers == null || _sim == null || _sim.Universe == null)
            {
                _grifos = new VozGrifo[0];
                return;
            }

            _grifos = new VozGrifo[dispensers.Length];
            for (int i = 0; i < dispensers.Length; i++)
            {
                var d = dispensers[i];
                if (d == null) continue;

                var arquetipo = _sim.Universe.Get(d.Material).archetype;
                AudioClip clip = arquetipo switch
                {
                    MaterialArchetype.Powder => SintetizadorSfx.GrifoPolvo,
                    MaterialArchetype.Gas => SintetizadorSfx.GrifoGas,
                    _ => SintetizadorSfx.GrifoLiquido,
                };

                var fuente = CrearFuenteBucle("Bucle_Grifo_" + i, clip);

                Vector2Int celdaAncla = _sim.WorldToCell(d.transform.position);
                _grifos[i] = new VozGrifo
                {
                    dispenser = d,
                    fuente = fuente,
                    matId = d.Material,
                    spoutX = celdaAncla.x + GrifoSpoutOffsetCells,
                    spoutY = celdaAncla.y - GrifoSpoutDropCells,
                    objetivoFluyendo = false,
                    volumenSuavizado = 0f,
                };
            }
        }

        /// <summary>Posiciones fijas (System.Random local, capa de presentación) usadas para estimar barato cuánto fuego hay sin recorrer la grilla entera cada frame.</summary>
        private void ConstruirSondasFuego()
        {
            _sondaX = new int[NumSondasFuego];
            _sondaY = new int[NumSondasFuego];
            var rngSondas = new System.Random(unchecked(System.Environment.TickCount * 17 + 3));
            for (int i = 0; i < NumSondasFuego; i++)
            {
                _sondaX[i] = 1 + rngSondas.Next(CellGrid.W - 2);
                _sondaY[i] = 1 + rngSondas.Next(CellGrid.H - 2);
            }
        }

        // ===================================================================
        // UPDATE
        // ===================================================================
        private void Update()
        {
            ManejarTeclaSilencio();

            // (M5 audio) Igual que Flask/Dispenser/HeatPlate/ChillStone: título,
            // intro de jornada, fin de día y pantalla final congelan la sim
            // (AlkahestSim.Paused=true) y no hay nada que "reaccionar" -- el
            // lecho ambiental/fuego/grifos simplemente MANTIENEN su último
            // volumen (nada de cortes ni clics) hasta que Playing vuelve.
            if (DayCycle.InputLocked)
            {
                _entradaBloqueadaAnterior = true;
                return;
            }

            // Flanco DayIntro/DayEnd -> Playing: nada especial que hacer aquí,
            // pero si alguna vez se necesita un "sonido de inicio de jornada"
            // este es el sitio (flanco descendente de InputLocked).
            _entradaBloqueadaAnterior = false;

            ActualizarEsquive();
            ActualizarBucleAmbiente();
            ActualizarPollerFrasco();
            ActualizarPollerBautizar();
            ActualizarPollerEncargos();

            _acumuladorSondeo += Time.deltaTime;
            if (_acumuladorSondeo >= IntervaloSondeo)
            {
                _acumuladorSondeo -= IntervaloSondeo;
                SondearFuego();
                SondearGrifos();
                SondearTolva();
            }
            ActualizarBucleFuego();
            ActualizarBuclesGrifo();

            ConsumirEventosSim();
        }

        // -----------------------------------------------------------------
        // Tecla M: silenciar/restaurar. SIEMPRE activa (incluso con
        // DayCycle.InputLocked), porque es una preferencia del jugador, no
        // una reacción al estado de juego -- justo lo que sí se congela más
        // abajo. F3 (paleta dev), H (pistas), T (bautizar), E (interactuar),
        // Q (vaciar), WASD/flechas (mover) están todas ocupadas: M está
        // libre (comprobado contra el resto del proyecto).
        // -----------------------------------------------------------------
        private void ManejarTeclaSilencio()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.mKey.wasPressedThisFrame) return;

            _silenciado = !_silenciado;
            PlayerPrefs.SetInt(PrefKeySilenciado, _silenciado ? 1 : 0);
            PlayerPrefs.Save();

            _avisoTexto = _silenciado ? "Sonido silenciado (M)" : "Sonido restaurado (M)";
            _avisoHasta = Time.time + 1.6f;
        }

        /// <summary>
        /// Rampa el esquive (ducking) de los BUCLES mientras suena un one-shot y la
        /// respuesta del factor maestro a esos mismos bucles -- fix playtest 9, causas
        /// 1c/1d. `_esquiveHasta` se re-arma en <see cref="ReproducirOneShot"/> cada vez
        /// que suena algo, así que una ráfaga de one-shots mantiene el esquive agachado
        /// de forma continua (nunca sube y baja entre disparos individuales, que sonaría
        /// a bombeo). Los one-shots en sí NO pasan por este esquive -- ya nacen y mueren
        /// en silencio por su propia envolvente (ver SintetizadorSfx.cs).
        /// </summary>
        private void ActualizarEsquive()
        {
            float esquiveObjetivo = Time.time < _esquiveHasta ? EsquiveOneShotNivel : 1f;
            _esquiveSuavizado = Mathf.MoveTowards(_esquiveSuavizado, esquiveObjetivo, Time.deltaTime * RampaVolumenBuclesPorSeg);

            // (fix playtest 9, causa 1d) la tecla M cambia FactorMaestro de golpe (0<->0.5);
            // los BUCLES (que nunca se paran, solo bajan a 0) rampan ese cambio en vez de
            // aplicarlo en seco -- un salto de ganancia instantáneo a mitad de forma de
            // onda también suena a clic aunque el AudioSource siga "vivo". Los one-shots
            // siguen usando FactorMaestro sin rampa (ver ReproducirOneShot): son voces
            // NUEVAS que ya arrancan en silencio, no tienen nada que suavizar.
            _factorMaestroSuavizado = Mathf.MoveTowards(_factorMaestroSuavizado, FactorMaestro, Time.deltaTime * RampaVolumenBuclesPorSeg);
        }

        /// <summary>Factor combinado que deben usar TODOS los bucles (ambiente/fuego/grifos): maestro rampado × esquive de one-shot. Ver ActualizarEsquive.</summary>
        private float FactorBucles => _factorMaestroSuavizado * _esquiveSuavizado;

        private void ActualizarBucleAmbiente()
        {
            _volumenAmbienteSuavizado = Mathf.MoveTowards(_volumenAmbienteSuavizado, VolAmbienteObjetivo, Time.deltaTime * 0.30f);
            if (_fuenteAmbiente != null) _fuenteAmbiente.volume = _volumenAmbienteSuavizado * FactorBucles;
        }

        // -----------------------------------------------------------------
        // Aspirar/verter: se observa el DELTA de Flask.Total (público) en
        // vez de leer el ratón -- ver doc de la clase.
        // -----------------------------------------------------------------
        private void ActualizarPollerFrasco()
        {
            if (_flask == null) return;
            int total = _flask.Total;
            int delta = total - _totalFrascoAnterior;
            if (delta > 0) DispararLimitado(ref _limAspirar, SintetizadorSfx.Aspirar, 4f, 0.18f);
            else if (delta < 0) DispararLimitado(ref _limVerter, SintetizadorSfx.Verter, 4f, 0.18f);
            _totalFrascoAnterior = total;
        }

        // -----------------------------------------------------------------
        // Bautizar: SubstanceKnowledge.CountNamed() público, se dispara al
        // subir (bautizar dos veces seguidas el mismo material sin cambiar
        // nombre, o "olvidarlo", no cuenta -- solo nos importa que HAYA un
        // nombre nuevo puesto).
        // -----------------------------------------------------------------
        private void ActualizarPollerBautizar()
        {
            if (_knowledge == null) return;
            int nombrados = _knowledge.CountNamed();
            if (nombrados > _materialesBautizadosAnterior)
            {
                ReproducirOneShot(SintetizadorSfx.Bautizar, 0.32f);
            }
            _materialesBautizadosAnterior = nombrados;
        }

        // -----------------------------------------------------------------
        // Encargo completado: OrderSystem.CompletedCount() público. Al
        // cambiar de jornada, OrderSystem.GenerateOrdersForDay limpia
        // ActiveOrders (completados vuelve a 0) DURANTE DayIntro, fase en la
        // que este Update ni siquiera llega aquí (InputLocked=true) -- así
        // que el primer poll de la jornada nueva simplemente ve "0 < valor
        // de ayer" y NO dispara nada (0 no es mayor), y a partir de ahí el
        // contador queda resincronizado solo, sin código especial.
        // -----------------------------------------------------------------
        private void ActualizarPollerEncargos()
        {
            if (_orderSystem == null) return;
            int completados = _orderSystem.CompletedCount();
            if (completados > _encargosCompletadosAnterior)
            {
                ReproducirOneShot(SintetizadorSfx.EncargoCompletado, 0.34f);
            }
            _encargosCompletadosAnterior = completados;
        }

        /// <summary>
        /// (fix playtest 9, causa 3 "¿llega a sonar?") Con NumSondasFuego sondas fijas
        /// repartidas sobre las 36.864 celdas de la grilla, y el fuego real muriendo en
        /// 1 tick en cuanto no toca combustible (Sim/SimStepper.ProcessFire), un sondeo
        /// puntual puede dar 0 coincidencias mientras hay fuego real ardiendo, por puro
        /// muestreo -- ANTES esto ponía el objetivo a 0 en cada sondeo así, y el fuego
        /// casi nunca se oía. Ahora: ATAQUE instantáneo con un piso ya audible en cuanto
        /// se detecta UNA sola celda (el fuego real "está vivo" o no se pilla, rara vez
        /// se ve "crecer" gradualmente para tan pocas sondas), y LIBERACIÓN lenta cuando
        /// un sondeo no encuentra nada (en vez de silencio inmediato) para no parpadear
        /// entre sondeos consecutivos de un fuego que sigue activo.
        /// </summary>
        private void SondearFuego()
        {
            if (_sim == null || _sondaX == null) return;
            int coincidencias = 0;
            for (int i = 0; i < _sondaX.Length; i++)
            {
                if (_sim.SampleMaterial(_sondaX[i], _sondaY[i]) == MaterialId.Fire) coincidencias++;
            }

            if (coincidencias > 0)
            {
                float golpe = PisoAudibleFuego + (1f - PisoAudibleFuego) * Mathf.Clamp01(coincidencias / SaturacionFuego);
                _intensidadFuegoObjetivo = Mathf.Max(_intensidadFuegoObjetivo, golpe);
            }
            else
            {
                _intensidadFuegoObjetivo = Mathf.MoveTowards(_intensidadFuegoObjetivo, 0f, IntervaloSondeo / LiberacionFuegoSeg);
            }
        }

        private void ActualizarBucleFuego()
        {
            if (_fuenteFuego == null) return;
            _intensidadFuegoSuavizada = Mathf.MoveTowards(_intensidadFuegoSuavizada, _intensidadFuegoObjetivo, Time.deltaTime * 0.9f);
            _fuenteFuego.volume = VolFuegoMax * _intensidadFuegoSuavizada * FactorBucles;
            if (_filtroFuego != null) _filtroFuego.cutoffFrequency = Mathf.Lerp(700f, 3200f, _intensidadFuegoSuavizada);
        }

        private void SondearGrifos()
        {
            if (_grifos == null || _sim == null) return;
            for (int i = 0; i < _grifos.Length; i++)
            {
                ref var g = ref _grifos[i];
                if (g.dispenser == null || g.fuente == null) { g.objetivoFluyendo = false; continue; }

                bool fluyendo = false;
                for (int dy = -1; dy <= 1 && !fluyendo; dy++)
                {
                    for (int dx = -1; dx <= 1 && !fluyendo; dx++)
                    {
                        if (dx * dx + dy * dy > 1) continue; // mismo rombo de radio 1 que Dispenser.EmitTick.
                        if (_sim.SampleMaterial(g.spoutX + dx, g.spoutY + dy) == g.matId) fluyendo = true;
                    }
                }
                g.objetivoFluyendo = fluyendo;
            }
        }

        private void ActualizarBuclesGrifo()
        {
            if (_grifos == null) return;
            for (int i = 0; i < _grifos.Length; i++)
            {
                ref var g = ref _grifos[i];
                if (g.fuente == null) continue;

                float volumenPorDistancia = 1f;
                if (_jugador != null && g.dispenser != null)
                {
                    float d = Vector3.Distance(_jugador.position, g.dispenser.transform.position);
                    float t = Mathf.Clamp01(1f - d / GrifoRadioAudible);
                    volumenPorDistancia = t * t; // caída suave, no lineal.
                }

                float objetivo = g.objetivoFluyendo ? volumenPorDistancia : 0f;
                g.volumenSuavizado = Mathf.MoveTowards(g.volumenSuavizado, objetivo, Time.deltaTime * 3f);
                g.fuente.volume = g.volumenSuavizado * VolGrifoBase * FactorBucles;
            }
        }

        /// <summary>
        /// Zona del "sillar" de la Tolva (constantes públicas de
        /// Sim/SimLevelBuilder, EL PLANO del taller -- nunca duplicadas a
        /// mano en otro sitio salvo el nº de filas del sillar, que es una
        /// decisión de Game/DeliveryChute.cs no expuesta públicamente y se
        /// replica aquí solo para esta estimación de audio, sin más
        /// pretensión que "detectar cuándo entra algo").
        /// </summary>
        private const int ChuteSillFilasEspejo = 3;

        private void SondearTolva()
        {
            if (_sim == null) return;
            int ocupadas = 0;
            int yMax = SimLevelBuilder.ChuteMouthY0 + ChuteSillFilasEspejo - 1;
            for (int x = SimLevelBuilder.ChuteMouthX0; x <= SimLevelBuilder.ChuteMouthX1; x++)
            {
                for (int y = SimLevelBuilder.ChuteMouthY0; y <= yMax; y++)
                {
                    byte m = (byte)_sim.SampleMaterial(x, y);
                    if (m != MaterialId.Empty && m != MaterialId.Stone) ocupadas++;
                }
            }

            bool ocupadaAhora = ocupadas > 0;
            if (ocupadaAhora && !_tolvaOcupadaAnterior)
            {
                float volumen = Mathf.Clamp01(0.20f + ocupadas * 0.01f);
                DispararLimitado(ref _limTolva, SintetizadorSfx.TolvaTraga, 4f, volumen);
            }
            _tolvaOcupadaAnterior = ocupadaAhora;
        }

        // -----------------------------------------------------------------
        // Ring buffer de eventos notables de la simulación (Ignite,
        // Crystallize, Freeze). Puntero de lectura PROPIO -- ver doc de la
        // clase, "CÓMO SE ENTERA DE LO QUE PASA".
        // -----------------------------------------------------------------
        private void ConsumirEventosSim()
        {
            if (_sim == null || _sim.Stepper == null) return;
            var stepper = _sim.Stepper;
            var eventos = stepper.Events;
            int head = stepper.EventHead;

            int i = _ultimoEventoLeido;
            int pasos = 0;
            while (i != head && pasos < SimStepper.EventBufferSize)
            {
                ManejarEventoSim(eventos[i].type);
                i = (i + 1) & (SimStepper.EventBufferSize - 1);
                pasos++;
            }
            _ultimoEventoLeido = head;
        }

        private void ManejarEventoSim(SimEventType tipo)
        {
            switch (tipo)
            {
                case SimEventType.Ignite:
                    DispararLimitado(ref _limIgnicion, SintetizadorSfx.Ignicion, 4f, 0.30f); // (fix playtest 9) bajado de 0.35, ver presupuesto de mezcla.
                    break;
                case SimEventType.Crystallize:
                case SimEventType.Freeze:
                    // (encargo) "empieza por 6/s para cristalizar y congelar":
                    // comparten sonido y limitador -- son la misma sensación
                    // ("algo se solidifica de golpe"), y si sonaran con
                    // limitadores independientes una racha mixta de ambos
                    // podría sumar hasta 12 campanillas/seg, justo lo que el
                    // limitador existe para evitar.
                    // (fix playtest 9) volumenBase bajado de 0.22 a 0.15: es el sonido con
                    // más riesgo de solapar VARIAS instancias a la vez (avalancha), así que
                    // su contribución al presupuesto de mezcla pesa varias veces -- ver
                    // comentario "PRESUPUESTO DE MEZCLA" junto a las constantes de volumen.
                    DispararLimitado(ref _limCristalizarCongelar, SintetizadorSfx.CristalizarCongelar, 6f, 0.15f);
                    break;
                default:
                    // Boil/Grow/Dissolve: sin sonido dedicado en este pase (no
                    // pedido por el encargo de dirección de sonido); se
                    // consumen igualmente para no dejar nunca de avanzar el
                    // puntero de lectura.
                    break;
            }
        }

        /// <summary>
        /// LIMITADOR DE RITMO por tipo de evento -- ver doc de la clase. Si
        /// `ahora` está dentro de la ventana de silencio del tipo, el evento
        /// se cuenta como "suprimido" (no suena) pero SUMA una avalancha:
        /// cuando por fin se permite sonar de nuevo, el volumen sube un
        /// poco (nunca la cadencia) en proporción a cuántos se perdieron.
        /// </summary>
        private void DispararLimitado(ref Limitador lim, AudioClip clip, float vecesPorSegundo, float volumenBase)
        {
            float ahora = Time.time;
            if (ahora < lim.proximoPermitido)
            {
                lim.suprimidos++;
                return;
            }

            float boost = Mathf.Clamp01(lim.suprimidos * 0.05f); // hasta +100% de volumen de diseño en avalanchas grandes, nunca más veces.
            float volumen = Mathf.Clamp01(volumenBase * (1f + boost));
            ReproducirOneShot(clip, volumen);

            lim.proximoPermitido = ahora + 1f / Mathf.Max(0.1f, vecesPorSegundo);
            lim.suprimidos = 0;
        }

        /// <summary>
        /// Voz de pool round-robin + variación de pitch (±6%) y volumen
        /// (±10%) para que la tercera repetición de un one-shot no canse
        /// (ver encargo). `volumenBase` es un valor de DISEÑO (0..1, antes
        /// del factor maestro/silencio), aplicado aquí de forma centralizada
        /// para que ningún llamador tenga que acordarse de multiplicarlo.
        ///
        /// (fix playtest 9, causa 1c) Cada vez que esto suena de verdad, re-arma
        /// `_esquiveHasta` para que <see cref="ActualizarEsquive"/> agache los
        /// BUCLES un rato -- es el "ducking sencillo por interpolación" del
        /// presupuesto de mezcla: el momento en que más voces coinciden (un
        /// one-shot sonando encima de ambiente+fuego+grifos) es exactamente
        /// cuando más falta hace bajar el resto un poco.
        /// </summary>
        private void ReproducirOneShot(AudioClip clip, float volumenBase)
        {
            if (clip == null || _vocesOneShot == null || _vocesOneShot.Length == 0) return;
            float factorMaestro = FactorMaestro;
            if (factorMaestro <= 0f) return; // silenciado: ni calcular la variación, no hay para qué.

            _siguienteVoz = (_siguienteVoz + 1) % _vocesOneShot.Length;
            var fuente = _vocesOneShot[_siguienteVoz];
            if (fuente == null) return;

            float pitch = 1f + ((float)_rngVariacion.NextDouble() * 2f - 1f) * 0.06f;
            float volVariacion = 1f + ((float)_rngVariacion.NextDouble() * 2f - 1f) * 0.10f;

            fuente.pitch = pitch;
            float vol = Mathf.Clamp01(volumenBase * volVariacion) * factorMaestro;
            fuente.PlayOneShot(clip, vol);

            _esquiveHasta = Time.time + EsquiveOneShotDuracionSeg;
        }

        // -----------------------------------------------------------------
        // Aviso breve en pantalla al (des)silenciar -- estilo UiStyles.Globo,
        // sin HUD permanente (ver encargo).
        // -----------------------------------------------------------------
        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_avisoTexto) || Time.time >= _avisoHasta) return;
            UiStyles.Preparar();
            Color color = _silenciado ? UiStyles.TextoTenue : UiStyles.Oro;
            UiStyles.Globo(new Vector2(Screen.width * 0.5f, UiStyles.S(90f)), _avisoTexto, color);
        }
    }
}
