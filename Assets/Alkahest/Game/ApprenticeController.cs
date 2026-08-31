using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// El aprendiz: un imp volador (sin plataformeo) que el jugador mueve
    /// libremente por el taller con WASD/flechas. Si no se le asigna un
    /// sprite en el inspector, genera su propio rig procedimental para no
    /// depender de assets externos.
    ///
    /// (M5 aprendiz) REDISEÑO DE PERSONAJE — hasta ahora era un cuadrado
    /// morado con dos ojos (una silueta que no comunicaba nada) y es el ÚNICO
    /// personaje del juego, en pantalla el 100% del tiempo: se lo merecía.
    /// Ficción: un pequeño imp del taller, ayudante del Maestro — CURIOSO y
    /// capaz, no un monstruo ni una mascota. Decisiones de arte:
    ///  · Cabeza grande / cuerpo pequeño + dos cuernecillos + alas de polilla
    ///    + cola fina: la silueta que dice "imp volador", legible incluso
    ///    pequeño en pantalla (la regla de oro del brief: "silueta antes que
    ///    detalle").
    ///  · Morado CLARO y desaturado (más luminoso y menos saturado que el
    ///    ciruela del fondo de WorkshopBackdrop.cs) + contorno de 1 téxel casi
    ///    negro alrededor de TODA la silueta (<see cref="AplicarContorno"/>):
    ///    así nunca se pierde contra la mampostería oscura ni contra materia
    ///    muy saturada (agua/fuego/arena).
    ///  · Acentos de latón (UiStyles.Oro es de UI, no de mundo — aquí se usa
    ///    el latón que pide el brief) en el collar, la gema de la frente, las
    ///    puntas de los cuernos y el tapón del frasco que carga.
    ///  · Ojos grandes con pupila y punto de luz: el foco de la lectura.
    ///
    /// RESOLUCIÓN: el sprite ORIGINAL medía 24x28 téxeles a 33 téxeles/unidad
    /// (~3.3 téxeles/celda de simulación, CellWorldSize=0.1). Aquí se triplica
    /// la densidad (72x84 téxeles a 99 téxeles/unidad, ~9.9 téxeles/celda,
    /// coherente con la resolución de detalle de MaquinariaSprites) SIN tocar
    /// el tamaño en unidades de mundo: 72/99 == 24/33 y 84/99 == 28/33
    /// exactamente. El tamaño de colisión/alcance y la física del movimiento
    /// no cambian — esto es un pase puramente VISUAL.
    ///
    /// ANIMACIÓN, todo por código y sin Animator (ver <see cref="HandleVisual"/>):
    ///  · Flotar: bobbing vertical (igual que antes, sin tocar sus valores).
    ///  · Aleteo: rotación de dos capas de ala alrededor de su punto de
    ///    anclaje (un "gozne"), con la FRECUENCIA (no la amplitud) escalando
    ///    con la velocidad — quieto aletea perezoso, a toda marcha bombea.
    ///  · Inclinación: el cuerpo se bandea unos grados hacia donde acelera
    ///    (lo que da "peso" a un personaje que flota).
    ///  · Parpadeo: se alternan dos sprites de cuerpo pre-generados (ojos
    ///    abiertos/cerrados) en intervalos irregulares programados con
    ///    System.Random SOLO al reprogramar el siguiente parpadeo (nunca en
    ///    cada frame) — la técnica de "fotogramas sintetizados" del brief.
    ///  · El frasco que lleva: una capa decorativa (tarro de vidrio con tapón
    ///    de latón) que persigue con Vector3.SmoothDamp el mismo punto que ya
    ///    usa Flask.cs para su indicador de contenido (<see cref="CarryAnchor"/>),
    ///    así ambos quedan alineados y el tarro se retrasa un pelín al
    ///    arrancar y al frenar (inercia leve).
    ///
    /// Todas las texturas/sprites se generan UNA vez en <see cref="BuildVisual"/>;
    /// Update() solo mueve transforms, cambia colores y alterna referencias de
    /// sprite ya creadas — cero asignaciones por frame.
    /// </summary>
    public sealed class ApprenticeController : MonoBehaviour
    {
        // =====================================================================
        // EL TALLER COMPARTIDO (playtest 28, POC multiplayer)
        // =====================================================================
        // Tres añadidos, todos INERTES en la escena de un jugador:
        //
        //  · `AprendizLocal`: quién es "mi" aprendiz cuando hay cuatro en el
        //    taller. Lo fija Net/AprendizNet.cs en el avatar del dueño; en la
        //    escena Lab clásica NADIE lo fija y se queda en null, así que
        //    SimRenderer sigue cayendo a su búsqueda de siempre y la cámara se
        //    comporta exactamente igual que antes.
        //  · `ControlDelJugador`: los avatares de los OTROS jugadores no leen
        //    el teclado (su posición llega por el NetworkTransform), pero sí
        //    siguen animándose — ver HandleMovement.
        //  · `AplicarTinte`: la librea de color de cada jugador (mandato de
        //    Cesar). Multiplica el color de las capas del cuerpo; sin llamarla,
        //    el tinte es blanco y el imp se ve como siempre.
        // =====================================================================

        /// <summary>El aprendiz que controla ESTE jugador. Null fuera de una sesión de red.</summary>
        public static ApprenticeController AprendizLocal;

        /// <summary>
        /// ¿Lee este aprendiz el teclado? False en los avatares de los demás
        /// jugadores. No se usa `enabled = IsOwner` (el patrón del
        /// PlayerController del template) porque un componente apagado dejaría
        /// a los otros aprendices como calcomanías rígidas: sin aleteo, sin
        /// cabeceo, sin mirar hacia donde van.
        /// </summary>
        [System.NonSerialized] public bool ControlDelJugador = true;

        /// <summary>Tinte actual (blanco = sin sesión / sin color asignado).</summary>
        private Color _tinte = Color.white;

        /// <summary>Capas del cuerpo que se tiñen, con su color de fábrica (el tinte MULTIPLICA sobre él, nunca lo sustituye).</summary>
        private SpriteRenderer[] _capasConTinte;
        private Color[] _coloresDeFabrica;

        /// <summary>Posición del frame anterior, para deducir la velocidad de un avatar remoto (que no tiene input propio).</summary>
        private Vector3 _posicionAnterior;
        private bool _tienePosicionAnterior;

        // =====================================================================
        // VELOCIDAD DE VUELO CONTRA EL TALLER x6 (playtest 15) -- MEDIDO, NO
        // SUBIDO A CIEGAS
        // =====================================================================
        // El encargo pedía revisar si moveSpeed seguía siendo adecuado con el
        // mundo a 768x288 (antes 256x144) y explícitamente avisaba de NO
        // subirla sin medir: el jugador ya validó este manejo, y con
        // Sim/SimRenderer.cs seguiendo a la cámara (en vez de encuadrar el
        // mundo entero), un aprendiz más rápido puede marear -- la cámara
        // reacciona a la velocidad REAL del personaje, no a una fracción del
        // mundo visible.
        //
        // MEDICIÓN (a moveSpeed=11.2, sin contar la rampa de aceleración,
        // CellWorldSize=0.1u/celda):
        //  · Cruzar UNA PANTALLA (256 celdas = 25.6u, lo que la cámara
        //    encuadra de verdad la mayor parte del tiempo -- ver
        //    SimRenderer.FitMainCamera): 25.6/11.2 ≈ 2.3s. IDÉNTICO al de
        //    ANTES del playtest 15 (el mundo entero medía una pantalla, así
        //    que "cruzar el taller" y "cruzar una pantalla" eran la misma
        //    medida) -- el manejo que el jugador validó, en la escala en la
        //    que de verdad lo experimenta, NO CAMBIÓ.
        //  · Cruzar una ZONA de la superficie (LABORATORIO, LabX0..X1, 244
        //    celdas = 24.4u): ≈2.2s -- mismo orden que una pantalla, esperable
        //    (las zonas se diseñaron del tamaño de una pantalla, ver el
        //    docblock de Sim/SimLevelBuilder.cs).
        //  · Cruzar el MUNDO ENTERO de punta a punta (768 celdas = 76.8u,
        //    CULTIVO a ENTREGA): 76.8/11.2 ≈ 6.9s -- ANTES (mundo de 256
        //    celdas) esto tardaba los mismos ≈2.3s que cruzar una pantalla, de
        //    donde sale el "3x" (el mundo creció 3x de ancho en x, 2x en y).
        //
        // DECISIÓN: NO SUBIR moveSpeed. El único número que de verdad se
        // triplicó es el de un trayecto de punta a punta del taller ENTERO --
        // y eso es LITERALMENTE lo que pidió Cesar al motivar el tamaño nuevo
        // ("un laboratorio de 2-3 pantallas... para que dos personas puedan
        // estar trabajando en cosas distintas sin verse constantemente", ver
        // Sim/CellGrid.cs): que cruzar de un extremo a otro cueste más no es
        // una regresión de manejo, es la separación de zonas funcionando como
        // se pidió. El manejo que el jugador SÍ validó -- aproximarse a un
        // grifo, apuntar con precisión al frasco, maniobrar dentro de una
        // cuba -- ocurre a la escala de "una pantalla", que no cambió ni un
        // texel: subir moveSpeed "arreglaría" un problema que no existe
        // (nadie se quejó de que cruzar un taller de 2.3s fuera lento) a costa
        // de empeorar SÍ un problema real (el mareo con cámara de seguimiento
        // que el propio encargo advierte). Si en un playtest futuro el
        // trayecto largo entre zonas se reporta como pesado, la palanca
        // correcta no es esta constante global (que también acelera la
        // maniobra fina) sino algo específico del viaje largo -- p.ej. un
        // acelerón extra tras Nx segundos en línea recta, o un atajo de
        // teletransporte entre zonas ya visitadas -- deliberadamente NO
        // implementado aquí por quedar fuera de lo que este encargo pidió.
        [Header("Movimiento")]
        // (RONDA 71, pedido de Cesar) VELOCIDAD -40%: 11.2 -> 6.7. Con las
        // esquinas suaves de la ronda 70 el imp ya no se atora, y a 11.2 se
        // sentía disparado. Cruzar una pantalla pasa de ~2.3s a ~3.8s.
        // Y SU INTUICIÓN DE "ACELERA" TENÍA SUSTENTO: MoveTowards con
        // acceleration=44 tardaba 11.2/44 = 0.25s en llegar a tope -- un
        // cuarto de segundo de rampa perceptible. Ahora 6.7/96 = 0.07s: el
        // arranque conserva un pelo de suavizado (cero rampa se siente
        // robótico) pero por debajo del umbral en que se LEE como acelerar.
        // (RONDA 71b, "aún siento el mono muy rápido... ¿se aplicó en ambos
        // lados?") LA TRAMPA DEL [SerializeField]: estos dos campos estaban
        // serializados, y el prefab del avatar de red
        // (Net/AlkahestAprendizRed.prefab) guardaba los valores VIEJOS
        // (11.2/44) de cuando se creó -- un valor serializado en
        // prefab/escena PISA el default del código, así que la escena MULTI
        // (editor Y build) volaba a la velocidad antigua por mucho que el
        // código dijera 6.7. Se retira la serialización: el CÓDIGO es la
        // única fuente de verdad de estos dos números en todos los modos y
        // en todas las builds. (El dato viejo del prefab queda huérfano e
        // ignorado; se afina aquí, no en el inspector -- como todo en este
        // proyecto.)
        private float moveSpeed = 4.8f; // (R111: el 4.0 "se sintio muy lento" -- +20% exacto sobre la prueba anterior; historial: 6.7 (origen) -> 4.0 (R110) -> 4.8.
        private float acceleration = 96f; // unidades/s^2 -- rampa de ~0.07s, imperceptible (ver bloque de arriba).

        [Header("Visual")]
        [SerializeField] private Sprite customSprite; // si se asigna, se usa en vez del rig generado (sin animación de capas: se respeta el sprite manual del inspector)
        [SerializeField] private int sortingOrder = 50;

        // Límites del mundo, derivados del tamaño real de la grilla de simulación
        // (CellGrid.W/H * SimRenderer.CellWorldSize == 76.8 x 28.8 desde el
        // playtest 15, antes 25.6 x 14.4). Nunca hardcodear: se derivan solos,
        // así que el clamp ya cubre el mundo x6 sin tocar nada aquí -- el
        // aprendiz vuela y puede llegar a cualquier esquina, SÓTANO incluido.
        private const float WorldMinX = 0f;
        private const float WorldMinY = 0f;
        private const float WorldMaxX = CellGrid.W * SimRenderer.CellWorldSize;
        private const float WorldMaxY = CellGrid.H * SimRenderer.CellWorldSize;

        private const float BobFrequency = 2.4f;
        private const float BobAmplitude = 0.04f;  // playtest 4: la mitad que antes (sin tocar en el pase visual M5)
        private const float VisualZOffset = -0.05f; // más cerca de la cámara que el quad de la sim (z=0), para quedar siempre por encima.

        // (M5 aprendiz) 33 téxeles/unidad * 3 = 99: el TRIPLE de densidad de
        // téxeles que antes, con el MISMO tamaño de mundo (ver doc de clase).
        // Una única constante para TODAS las texturas generadas (cuerpo, alas,
        // cola, frasco) para que su escala relativa entre sí sea correcta.
        private const float SpritePixelsPerUnit = 99f;

        private const int BodyTexW = 72, BodyTexH = 84;
        private const int WingTexW = 40, WingTexH = 34;
        private const int TailTexW = 32, TailTexH = 16;
        private const int CarryTexW = 16, CarryTexH = 22;

        // Anclaje del gozne del ala dentro de su propia textura (ver
        // GenerateWingTexture): el pivote del sprite se pone AHÍ para que
        // rotarlo bata el ala desde el hombro, no desde su centro geométrico.
        private static readonly Vector2 WingHingePx = new Vector2(WingTexW - 6f, 6f);
        private static readonly Vector2 TailHingePx = new Vector2(2f, 9f);

        // -----------------------------------------------------------------
        // Paleta (ver doc de clase): morado claro/desaturado + contorno casi
        // negro + latón cálido. UiStyles.Oro es UI, no mundo: aquí se usa
        // latón propio, RGB(168,126,58) medio / (214,176,96) luz / (86,62,28)
        // sombra, tal y como pide el brief.
        // -----------------------------------------------------------------
        private static readonly Color32 ColOutline = new Color32(0x16, 0x10, 0x1E, 255);
        private static readonly Color32 ColBodyBase = new Color32(0xA8, 0x96, 0xC4, 255);
        private static readonly Color32 ColBodyLight = new Color32(0xD2, 0xC4, 0xEC, 255);
        private static readonly Color32 ColBodyShadow = new Color32(0x6E, 0x5E, 0x8E, 255);
        private static readonly Color32 ColEyeWhite = new Color32(0xF2, 0xF6, 0xFF, 255);
        private static readonly Color32 ColEyePupil = new Color32(0x24, 0x1C, 0x30, 255);
        private static readonly Color32 ColEyeHighlight = new Color32(0xFF, 0xFF, 0xFF, 255);
        private static readonly Color32 ColBrassMed = new Color32(168, 126, 58, 255);
        private static readonly Color32 ColBrassLight = new Color32(214, 176, 96, 255);
        private static readonly Color32 ColBrassShadow = new Color32(86, 62, 28, 255);
        private static readonly Color32 ColGlass = new Color32(0xCB, 0xE4, 0xEE, 255);
        private static readonly Color32 ColGlassRim = new Color32(0xE8, 0xF6, 0xFF, 255);
        private static readonly Color32 ColLiquid = new Color32(0xE0, 0xA8, 0x4E, 255); // licor ámbar decorativo del frasco cargado

        // -----------------------------------------------------------------
        // Ajuste de la animación (M5 aprendiz).
        // -----------------------------------------------------------------
        private const float TiltDegPerSpeed = 1.5f; // grados de bandeo por cada u/s de velocidad horizontal
        private const float TiltMaxDeg = 16f;
        private const float TiltSmooth = 7f;

        private const float FlapHzIdle = 1.1f;  // aleteo perezoso en reposo
        private const float FlapHzMax = 5.4f;   // aleteo urgente a máxima velocidad
        private const float FlapAmplitudeDeg = 36f;
        private const float FlapSmooth = 6f;    // suavizado exponencial del cambio de frecuencia (sin tirones)

        private const float BlinkDuration = 0.12f;
        private const float CarriedFlaskLag = 0.16f; // segundos de retraso del frasco cargado (inercia leve)

        private Vector2 _velocity;
        private bool _facingRight = true;

        private Transform _visualTransform;
        private Transform _tiltPivot;
        private bool _usingCustomSprite;

        // (R118) LA ESTAMPA CAMINA: primera hoja de cuadros salida del arnés
        // (Wan Animate 2 sobre un ciclo Walking de Mixamo, 16 fps). Se
        // reproduce mientras hay velocidad horizontal; quieto o subiendo,
        // vuelve la estampa base. Null si el asset no existe: nada cambia.
        private Sprite _estampaBase;
        private HojaDeCuadros _hojaCaminar;
        private HojaDeCuadros _hojaReposo;   // (R118f) reposo calmado (Happy Idle), ciclo; solo a pie
        private HojaDeCuadros _hojaRecoger;  // (R118f) gesto de una sola pasada (Picking Up de perfil); tecla G a pie para verlo
        private HojaDeCuadros _gestoActivo;
        private float _cuadroGesto;

        /// <summary>(R118f) Reproduce una hoja UNA vez (agacharse a recoger, etc.). Solo a pie y quieto; ignora si ya hay un gesto en curso.</summary>
        public void ReproducirGesto(HojaDeCuadros hoja)
        {
            if (hoja == null || _gestoActivo != null || !EnSuelo) return;
            _gestoActivo = hoja; _cuadroGesto = 0f; _faseCaminar = 0; _cuadroCaminar = 0f;
        }

        /// <summary>true si el gesto puso el sprite este frame (y la caminata/reposo no deben tocarlo).</summary>
        private bool TickGesto(bool aPie, float velX)
        {
            var kb = Keyboard.current;
            if (_gestoActivo == null && _hojaRecoger != null && aPie && velX < UmbralCaminar && kb != null
                && kb.gKey.wasPressedThisFrame && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto && !Mudanza.ModoActivo)
                ReproducirGesto(_hojaRecoger);
            if (_gestoActivo == null) return false;
            if (!aPie) { _gestoActivo = null; return false; } // despegó a mitad: el vuelo manda
            _cuadroGesto += Time.deltaTime * _gestoActivo.Fps;
            int i = (int)_cuadroGesto;
            if (i >= _gestoActivo.Cuadros.Length) { _gestoActivo = null; return false; }
            if (_bodySr != null) _bodySr.sprite = _gestoActivo.Cuadros[i];
            return true;
        }
        private float _cuadroReposo;
        private float _cuadroCaminar;
        private int _faseCaminar;   // 0 quieto · 1 arrancando (intro) · 2 ciclo · 3 frenando (intro al revés)
        private const float RitmoArranque = 0.7f;      // (R118d) arranque al 70% del ritmo del ciclo
        private const int FrenadoCuadros = 4;           // (R118e) al parar solo se deshacen los últimos 4 cuadros del arranque
        private const float AjusteVisualAPie = 0.04f; // = el margen de bob que la caja lleva por abajo (MedioAltoAbajo 0.64 vs pies -0.598)

        /// <summary>
        /// (R118c, Cesar: "la cabeza se achica al empezar a caminar... le
        /// falta una animación 'empezar a caminar'") La hoja trae un ARRANQUE:
        /// sus primeros <see cref="HojaDeCuadros.Intro"/> cuadros van de la
        /// pose de la estampa (cuadro 0 del video = la referencia) al ciclo.
        /// Se tocan al empezar y AL REVÉS al parar, así el giro de la cabeza
        /// es un movimiento y no un corte. Sin intro, entra directo al ciclo.
        /// </summary>
        private void TickCaminar(bool quiere, float velX)
        {
            if (_hojaCaminar == null || _bodySr == null) return;
            var cu = _hojaCaminar.Cuadros;
            int intro = _hojaCaminar.Intro, ciclo = _hojaCaminar.CuadrosDelCiclo;
            float ritmo = Mathf.Clamp(Mathf.Max(velX, VelocidadPaso * 0.5f) / VelocidadPaso, 0.5f, 1.6f);
            float paso = Time.deltaTime * _hojaCaminar.Fps * ritmo;
            // (R118d) el arranque y el frenado van más lentos que el ciclo
            // (Cesar: "le falta un frame o dos de intermedio"): mismo material,
            // más tiempo en pantalla = transición que se lee.
            float pasoIntro = Time.deltaTime * _hojaCaminar.Fps * RitmoArranque;
            if (quiere)
            {
                if (_faseCaminar == 0) { _faseCaminar = intro > 0 ? 1 : 2; _cuadroCaminar = 0f; }
                else if (_faseCaminar == 3) { _faseCaminar = 1; } // se arrepintió a medio frenar: sigue el arranque desde donde iba
                if (_faseCaminar == 1)
                {
                    _cuadroCaminar += pasoIntro;
                    if (_cuadroCaminar >= intro) { _faseCaminar = 2; _cuadroCaminar -= intro; }
                    else { _bodySr.sprite = cu[(int)_cuadroCaminar]; return; }
                }
                _cuadroCaminar += paso;
                _bodySr.sprite = cu[intro + ((int)_cuadroCaminar) % Mathf.Max(1, ciclo)];
            }
            else
            {
                // (R118e, Cesar: "cuando dejo de caminar la animación sigue
                // unos frames") El frenado es CORTO: solo los últimos
                // FrenadoCuadros del arranque, al revés y a ritmo pleno
                // (~0.25 s): lo justo para que la cabeza gire de vuelta, sin
                // que parezca que sigue caminando.
                if (_faseCaminar == 2) { _faseCaminar = intro > 0 ? 3 : 0; _cuadroCaminar = Mathf.Min(intro, FrenadoCuadros) - 0.001f; }
                if (_faseCaminar == 3 || _faseCaminar == 1)
                {
                    _faseCaminar = 3;
                    _cuadroCaminar = Mathf.Min(_cuadroCaminar, FrenadoCuadros - 0.001f);
                    _cuadroCaminar -= Time.deltaTime * _hojaCaminar.Fps;
                    if (_cuadroCaminar > 0f) { _bodySr.sprite = cu[Mathf.Min(intro - 1, (int)_cuadroCaminar)]; return; }
                    _faseCaminar = 0;
                }
                _cuadroCaminar = 0f;
                if (_hojaReposo != null && EnSuelo)
                {
                    // (R118f) A pie y quieto: el reposo respira (ida y vuelta,
                    // nunca corta). Volando o sin hoja: la pose base.
                    _cuadroReposo += Time.deltaTime * _hojaReposo.Fps;
                    _bodySr.sprite = _hojaReposo.CuadroDelCiclo(_cuadroReposo);
                    return;
                }
                _cuadroReposo = _hojaReposo != null ? Mathf.Max(0, _hojaReposo.Base - _hojaReposo.Intro) : 0f; // al volver a pie arranca desde la pose base (o el inicio del ciclo si la base es el arranque)
                if (_estampaBase != null && _bodySr.sprite != _estampaBase) _bodySr.sprite = _estampaBase;
            }
        }
        private const float UmbralCaminar = 0.12f;      // u/s de velocidad horizontal a partir de la cual "camina" (R118b: a pie, en unidades reales)

        private SpriteRenderer _bodySr;
        private Sprite _bodySpriteOpen;
        private Sprite _bodySpriteClosed;
        private Sprite _bodySpriteFrontal; // (R98, placeholder a conciencia) la pose de capataz del modo mudanza: mismo lienzo, ojos al centro.
        private SpriteRenderer _planoSr;   // (R98) el plano enrollado bajo el brazo — un rect crema, nada más (Cesar: "placeholder que sirva para entender").
        private Transform _planoTr;

        private SpriteRenderer _wingBackSr, _wingFrontSr;
        private Transform _wingBackTr, _wingFrontTr;

        private SpriteRenderer _tailSr;
        private Transform _tailTr;

        private SpriteRenderer _carriedFlaskSr;
        private Transform _carriedFlaskTr;
        private Vector3 _carriedFlaskVel;

        private float _tiltDeg;
        private float _flapHz = FlapHzIdle;
        private float _wingPhase;
        private float _nextBlinkAt;
        private float _blinkUntil;
        private System.Random _rng;

        /// <summary>Punto ~0.5 unidades por delante/abajo del aprendiz, donde el frasco muestra su contenido. SIN CAMBIOS: Flask.cs depende de este cálculo exacto para su indicador.</summary>
        public Vector3 CarryAnchor => transform.position + (_usingCustomSprite
            ? new Vector3(_facingRight ? 0.42f : -0.42f, -0.16f, 0f)   // (R116) la MANO del muñeco de 12 celdas: el mismo punto que persigue el tarro visual — el círculo de contenido y la botella vuelven a ser UNA cosa.
            : new Vector3(_facingRight ? 0.28f : -0.28f, -0.35f, 0f)); // el imp procedimental de siempre.

        // (RONDA 69c, el juice del frasco) EL TARRO RESPIRA: Flask empuja
        // aquí su muelle de "pop" (ver Flask.ActualizarJuice) y HandleVisual
        // lo aplica a la escala del tarro en mano -- pop al recibir una mota,
        // encogida breve al verter. Flask es quien INTEGRA el muelle (una
        // sola fuente de verdad); este campo es solo el último valor recibido.
        private float _pulsoFrasco;
        /// <summary>Lo llama Flask cada frame con su muelle de pop (ya clampeado a [-0.25, 0.35]).</summary>
        public void PulsoDelFrasco(float valor) => _pulsoFrasco = Mathf.Clamp(valor, -0.25f, 0.35f);

        // (RONDA 69d) EL TARRO SE LADEA AL VERTER: Flask empuja aquí los
        // grados (ya suavizados por su MoveTowards) y HandleVisual los aplica
        // a la rotación del tarro en mano -- la inclinación llega ANTES que
        // la primera mota del chorro: es la anticipación del vertido.
        private float _inclinacionFrasco;
        /// <summary>Lo llama Flask cada frame con la inclinación del vertido en grados (0 = vertical).</summary>
        public void InclinacionDelFrasco(float grados) => _inclinacionFrasco = Mathf.Clamp(grados, -20f, 20f);

        private void Awake()
        {
            BuildVisual();
        }

        private void Update()
        {
            HandleMovement();
            HandleVisual();
        }

        private void HandleMovement()
        {
            // (playtest 28) AVATAR DE OTRO JUGADOR: su transform lo mueve el
            // OwnerNetworkTransform del template, no este método. Se deduce la
            // velocidad del desplazamiento real para que la animación (aleteo,
            // bandeo, hacia dónde mira) siga viva y el resto de HandleVisual
            // funcione sin enterarse de que hay red de por medio.
            if (!ControlDelJugador)
            {
                DeducirVelocidadRemota();
                return;
            }

            var kb = Keyboard.current;
            Vector2 input = Vector2.zero;
            // (fix playtest 10) WASD/flechas son un atajo de teclado como cualquier otro del
            // proyecto: mientras se ESCRIBE un nombre (UiStyles.EscribiendoTexto) esas mismas
            // letras no deben mover al aprendiz a la vez que rellenan el campo, y con el
            // diario abierto a pantalla completa (JournalHud.Abierto) el aprendiz no debe
            // salir volando porque el jugador toque una flecha pensando en pasar de página.
            // Se ignora el input (no se "congela" en seco): la velocidad ya acumulada decae
            // con la MISMA física de siempre unas líneas más abajo, así que el personaje
            // frena con naturalidad en vez de detenerse en un frame.
            // (integración pt46) Y lo mismo con el ÁLBUM a pantalla completa
            // (AlbumReal.Abierto) -- mismo motivo, misma física de frenado:
            // deuda anotada por la ronda visual del álbum (las flechas movían
            // al aprendiz detrás del velo mientras pasabas de página).
            if (kb != null && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            }
            if (input.sqrMagnitude > 1f) input.Normalize();
            bool quiereDespegar = kb != null && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto
                && (input.y > 0.5f || kb.spaceKey.isPressed);

            Vector3 pos = transform.position;
            _enSuelo = SobreSuelo(pos);

            // (R118b, Cesar: "que tenga física y camine lento hasta que con
            // espaciador salte y ahí empiece a volar, y no se pierda lo que
            // ya hay ni la velocidad de vuelo") DOS MODOS, UNA FÍSICA:
            //  · A PIE: gravedad, y A/D caminan a VelocidadPaso (la zancada
            //    real del ciclo de la hoja: 4.8 u/s con esos pasos era un
            //    patinaje). W o ESPACIO = despegar.
            //  · VOLANDO: exactamente lo de siempre (WASD a moveSpeed, sin
            //    gravedad, hover). Se ATERRIZA bajando con S hasta tocar
            //    suelo: nunca por rozar el piso de pasada, así el vuelo bajo
            //    del prólogo sigue intacto.
            // Se arranca a pie (cayendo hasta el primer suelo).
            if (!_volando)
            {
                if (quiereDespegar)
                {
                    _volando = true;
                    _velocity.y = Mathf.Max(_velocity.y, ImpulsoDespegue);
                }
                else
                {
                    _velocity.x = Mathf.MoveTowards(_velocity.x, input.x * VelocidadPaso, acceleration * Time.deltaTime);
                    if (_enSuelo && _velocity.y <= 0f) _velocity.y = 0f;
                    else _velocity.y = Mathf.Max(_velocity.y - Gravedad * Time.deltaTime, -CaidaMax);
                }
            }
            if (_volando)
            {
                Vector2 target = input * moveSpeed;
                _velocity = Vector2.MoveTowards(_velocity, target, acceleration * Time.deltaTime);
                if (_enSuelo && input.y < -0.5f)
                {
                    _volando = false;
                    _velocity.y = 0f;
                }
            }

            MoverConColision(ref pos, _velocity * Time.deltaTime);
            if (!_volando && _velocity.y <= 0f) AsentarEnSuelo(ref pos);
            pos.z = 0f;
            transform.position = pos;

            if (input.x > 0.01f) _facingRight = true;
            else if (input.x < -0.01f) _facingRight = false;
        }

        /// <summary>
        /// (R118c, Cesar: "camina no sobre la bedrock sino un poco por el
        /// aire") MoverConColision resuelve en subpasos de 0.06u y deja al
        /// personaje flotando hasta esa medida sobre el piso. A pie se cierra
        /// el hueco: se baja en pasos finos hasta que la caja toque.
        /// </summary>
        private void AsentarEnSuelo(ref Vector3 pos)
        {
            if (_simColision == null || CajaChoca(pos.x, pos.y)) return;
            if (!CajaChoca(pos.x, pos.y - SondaSuelo)) return; // no hay suelo cerca: está cayendo
            const float fino = 0.005f;
            for (int i = 0; i < 16; i++)
            {
                if (CajaChoca(pos.x, pos.y - fino)) break;
                pos.y -= fino;
            }
        }

        // (R118b) LOS PIES: caminar/volar. La sonda de suelo mira 0.08u bajo la
        // caja (MoverConColision resuelve en subpasos de 0.06u, así que al
        // apoyarse puede quedar un hueco de hasta esa medida). Público de
        // lectura para HandleVisual y para quien quiera saber si está a pie.
        private bool _volando;
        private bool _enSuelo;
        public bool EnSuelo => _enSuelo && !_volando;
        public bool Volando => _volando;
        private const float VelocidadPaso = 1.1f;   // u/s a pie: la zancada de la hoja `caminar` (17 cuadros a 16 fps, personaje de 1.2u) da ~1.0-1.2 u por ciclo.
        private const float Gravedad = 22f;         // u/s^2: cae con peso de muñeco relleno, no de pluma.
        private const float CaidaMax = 9f;
        private const float ImpulsoDespegue = 2.6f; // el brinco con que despega antes de que el vuelo tome el mando.
        private const float SondaSuelo = 0.08f;

        private bool SobreSuelo(Vector3 pos)
        {
            if (!ColisionConEstructura) return false;
            if (_simColision == null)
            {
                _simColision = FindAnyObjectByType<AlkahestSim>();
                if (_simColision == null) return false;
            }
            // Si ya está DENTRO de sólido no cuenta como apoyado (la colisión
            // está suspendida ese frame y saldría nadando, como siempre).
            if (CajaChoca(pos.x, pos.y)) return false;
            return CajaChoca(pos.x, pos.y - SondaSuelo);
        }

        // =================================================================
        // (RONDA 66, dirección 2.5D de Cesar) COLISIÓN CON LA ARQUITECTURA:
        // "a partir de esta dirección, el personaje ya no atraviesa roca
        // madre; colisiona con roca y plataformas estructurales; puede seguir
        // volando". El imp voló como fantasma desde el día 1 (solo el Clamp a
        // los bordes del MUNDO lo sujetaba) -- a oscuras eso era un tester
        // perdido dentro de la piedra (visto en vivo, ronda 64). Diseño:
        //  · SOLO bloquean los sólidos ESTRUCTURALES (roca madre y piso
        //    estructural, la obra del taller incluida por ser piedra):
        //    polvos, líquidos, gases y productos se atraviesan igual que
        //    siempre -- puedes zambullirte en el agua o cruzar el humo.
        //  · Caja AABB (medidas: ver MedioAnchoColision) resuelta POR EJE (deslizas por las paredes
        //    en vez de frenar en seco) y en SUBPASOS de 0.06u: a la velocidad
        //    máxima (11.2 u/s) un frame recorre casi 2 celdas y sin subpasos
        //    se tunelaría un piso de 1 celda.
        //  · Si el frame ARRANCA con la caja ya dentro de sólido (teleport de
        //    debug, spawn legado, roca construida encima), la colisión se
        //    SUSPENDE ese frame: siempre puedes salir nadando de la piedra,
        //    jamás quedas clavado (pariente de la regla 38: la vuelta atrás
        //    barata antes que la trampa).
        // La bandera estática permite apagarla de un plumazo si una escena
        // vieja la necesitara fuera (hoy nadie la apaga).
        // =================================================================
        public static bool ColisionConEstructura = true;

        // (afinado pt68, segunda pasada -- Cesar: "mejoró el colider pero AÚN
        // es insuficiente, se sobrepone a la pared... incluso podría quedar
        // un milímetro por detrás; al menos no debería atravesarla"). La
        // pasada del pt67 subió la caja "al tamaño visual" A OJO (3x4 celdas)
        // sin medir el sprite -- medido ahora contra GenerateBodyTexture: el
        // CUERPO dibujado ocupa 44px de ancho (cabeza rx=22) por ~76px de
        // alto a 99 ppu con pivote centrado = 4.4 x 7.7 CELDAS, más el bob
        // vertical de ±0.04u. La caja de 3x4 dejaba ~0.7 celdas de cabeza
        // dentro de la pared por lado y ~1.6 por arriba/abajo: exactamente lo
        // que Cesar veía. Ahora la caja cubre el cuerpo macizo (4.2 x 6.0
        // celdas); solo pueden rozar las PUNTAS blandas (cuernos ~1.2 celdas,
        // la barbilla del cuerpo bajo ~0.8 con el bob) -- doctrina del pt67,
        // pero ahora sobre medidas leídas, no prosa (regla 39).
        // COSTE ASUMIDO (reportado a Cesar): un túnel HORIZONTAL de una sola
        // pasada del cincel (disco radio 2 = 5 celdas de luz) ya no da la
        // talla en vertical: hay que ensancharlo con una segunda pasada.
        // Los pozos VERTICALES de una pasada sí siguen dando (4.2 < 5).
        // Cesar eligió explícitamente este lado del trato.
        //
        // (pt69, TERCERA pasada -- Cesar capturó los puntos MÁS NORTE Y MÁS
        // SUR del bob del idle contra techo y suelo: "el borde inferior y el
        // superior aún están jodidos"; los verticales ya le valen). Lo que
        // faltaba era EL BOB: el visual sube y baja ±0.04u alrededor de
        // transform.position (BobAmplitude) y la caja de 0.30 no lo cubría
        // -- coronilla 0.364 + bob = 0.404 por arriba, barbilla 0.343 + bob
        // = 0.383 por abajo. La caja pasa a ASIMÉTRICA y cubre AMBOS
        // extremos del bob: solo las antenas (1 px, puntas blandas por
        // doctrina) pueden asomar en el pico del bob. El trato del túnel no
        // cambia: la pasada horizontal única ya estaba bloqueada desde 6.0.
        private const float MedioAnchoColision = 0.32f;  // (R110, medido sobre MunhecoRemiendos.png con alfa>32: cubo 0.677u de ancho) 6.4 celdas de caja para 6.8 de cubo -- doctrina pt68: el macizo cubierto, los bordes blandos pueden rozar.
        private const float MedioAltoArriba = 0.48f;     // (R110) tapa del cubo +0.437 + bob 0.04 = 0.477: solo el BROTE (punta blanda, hasta +0.599) asoma en el pico del bob -- el heredero de las antenas del imp.
        private const float MedioAltoAbajo = 0.64f;      // (R110, Cesar: "invade el terreno como un 15% de su dimension") pies -0.598 + bob 0.04 = 0.638: los pies tocan suelo DE VERDAD, ya no se hunden 2.2 celdas.
        private const float SubPaso = 0.06f;             // < 1 celda por subpaso: sin túneles.
        // (RONDA 70, domingo de ajustes de Cesar) DOS SUAVIZADOS DE LA CAJA:
        //  · CHAFLÁN (ChaflanCeldas): las 4 esquinas de la AABB se recortan
        //    en diagonal (la caja pasa a OCTÁGONO). Cesar: "en las esquinas
        //    es muy pixel perfect -- si toca un poquito ya no pasa el mono".
        //    Rozar un diente de 1-2 celdas con la esquina ya no bloquea.
        //    NOTA: aquí no hay colliders de Unity que fusionar (la colisión
        //    es la GRILLA leída directo, cero colliders) -- el Composite
        //    Collider que sugería no aplica: el problema eran las esquinas
        //    afiladas, y se resuelve en la forma de la caja.
        //  · ASISTENCIA DE ESQUINA (AsistenciaEsquina, ver MoverConColision):
        //    si un paso se bloquea pero a <=1.5 celdas de deslizamiento
        //    lateral hay hueco, el imp RESBALA alrededor del borde en vez de
        //    frenar en seco -- la "corner correction" clásica de los
        //    plataformeros, aplicada a un volador.
        private const int ChaflanCeldas = 3; // (R110) proporcional a la caja nueva (antes 2 de 4.2 de ancho; ahora 3 de 6.4): las esquinas siguen igual de blandas.
        private static readonly float[] AsistenciaEsquina = { 0.05f, 0.10f, 0.15f };

        private AlkahestSim _simColision;

        private void MoverConColision(ref Vector3 pos, Vector2 delta)
        {
            float nx = Mathf.Clamp(pos.x + delta.x, WorldMinX, WorldMaxX);
            float ny = Mathf.Clamp(pos.y + delta.y, WorldMinY, WorldMaxY);

            if (!ColisionConEstructura)
            {
                pos.x = nx; pos.y = ny;
                return;
            }

            if (_simColision == null)
            {
                _simColision = FindAnyObjectByType<AlkahestSim>();
                if (_simColision == null) { pos.x = nx; pos.y = ny; return; }
            }

            // Ya-dentro-de-sólido: colisión suspendida este frame (ver doc).
            if (CajaChoca(pos.x, pos.y)) { pos.x = nx; pos.y = ny; return; }

            float restX = nx - pos.x, restY = ny - pos.y;
            int pasos = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(restX), Mathf.Abs(restY)) / SubPaso));
            float pasoX = restX / pasos, pasoY = restY / pasos;
            for (int i = 0; i < pasos; i++)
            {
                if (pasoX != 0f)
                {
                    float px = pos.x + pasoX;
                    if (!CajaChoca(px, pos.y)) pos.x = px;
                    else if (!IntentarAsistencia(ref pos, px, pos.y, vertical: true))
                    {
                        pasoX = 0f; _velocity.x = 0f;
                    }
                }
                if (pasoY != 0f)
                {
                    float py = pos.y + pasoY;
                    if (!CajaChoca(pos.x, py)) pos.y = py;
                    else if (!IntentarAsistencia(ref pos, pos.x, py, vertical: false))
                    {
                        pasoY = 0f; _velocity.y = 0f;
                    }
                }
                if (pasoX == 0f && pasoY == 0f) break;
            }
        }

        /// <summary>
        /// (ronda 70) DESLIZAMIENTO ASISTIDO: el paso hacia (px,py) chocó,
        /// pero si desplazándose hasta ±1.5 celdas en el eje PERPENDICULAR
        /// hay hueco, el imp resbala alrededor del borde y el avance NO se
        /// pierde -- es lo que convierte "rocé la esquina del túnel y me
        /// clavé" en "entré resbalando". Prueba primero los desplazamientos
        /// chicos (0.5/1.0/1.5 celdas) y los dos lados; devuelve false si
        /// ninguno libra, y entonces el llamador frena el eje como siempre.
        /// La velocidad NO se toca en un paso asistido: el gesto sigue vivo.
        /// </summary>
        private bool IntentarAsistencia(ref Vector3 pos, float px, float py, bool vertical)
        {
            for (int a = 0; a < AsistenciaEsquina.Length; a++)
            {
                float d = AsistenciaEsquina[a];
                if (vertical)
                {
                    if (!CajaChoca(px, py + d)) { pos.x = px; pos.y = py + d; return true; }
                    if (!CajaChoca(px, py - d)) { pos.x = px; pos.y = py - d; return true; }
                }
                else
                {
                    if (!CajaChoca(px + d, py)) { pos.x = px + d; pos.y = py; return true; }
                    if (!CajaChoca(px - d, py)) { pos.x = px - d; pos.y = py; return true; }
                }
            }
            return false;
        }

        /// <summary>
        /// true si la caja del imp centrada en (cx,cy) pisa algún sólido
        /// estructural. (pt68, segunda pasada) Ya NO muestrea puntos sueltos:
        /// recorre TODAS las celdas de la grid que la AABB toca (con la caja
        /// a 4.2x6.0 celdas, el muestreo de 8 puntos del pt67 dejaba huecos
        /// de hasta 3 celdas por arista -- un diente de 1-2 celdas se colaba
        /// entre dos muestras y el imp lo "tragaba" sin chocar). Son ~5x7=35
        /// lecturas por consulta, bucle plano sin allocs: nada para un solo
        /// personaje, y es EXACTO por construcción, no por densidad.
        /// </summary>
        private bool CajaChoca(float cx, float cy)
        {
            int x0 = Mathf.FloorToInt((cx - MedioAnchoColision) / SimRenderer.CellWorldSize);
            int x1 = Mathf.FloorToInt((cx + MedioAnchoColision) / SimRenderer.CellWorldSize);
            int y0 = Mathf.FloorToInt((cy - MedioAltoAbajo) / SimRenderer.CellWorldSize);
            int y1 = Mathf.FloorToInt((cy + MedioAltoArriba) / SimRenderer.CellWorldSize);
            for (int y = y0; y <= y1; y++)
            {
                int dyBorde = Mathf.Min(y - y0, y1 - y);
                for (int x = x0; x <= x1; x++)
                {
                    // (ronda 70) CHAFLÁN: las celdas del rincón de la caja no
                    // cuentan (octágono, no rectángulo) -- rozar una esquina
                    // contra un saliente de 1-2 celdas ya no frena al imp.
                    int dxBorde = Mathf.Min(x - x0, x1 - x);
                    if (dxBorde + dyBorde < ChaflanCeldas) continue;

                    if (!CellGrid.InBounds(x, y)) continue;
                    int m = _simColision.SampleMaterial(x, y);
                    if (m != MaterialId.Stone && m != MaterialId.PisoEstructural) continue;

                    // (ronda 70, mandato de Cesar: "el personaje NO puede
                    // colisionar con las máquinas -- que la gestión del
                    // laboratorio no se convierta en una trampa") La
                    // mampostería de las máquinas es Stone en la grilla, pero
                    // está registrada en ObraDelTaller: se atraviesa. Solo
                    // bloquean la ROCA MADRE y el PISO ESTRUCTURAL de verdad.
                    // No hizo falta tocar capas ni sorting: la colisión es la
                    // grilla, así que la excepción vive aquí, en la lectura.
                    // (R113, Cesar: "tienen que tener colision, actualmente
                    // lo atravieso") EXCEPCION DE LA EXCEPCION: las repisas de
                    // la cascada son cornisas de a pie y SI sostienen; las
                    // maquinas siguen francas (mandato r70 intacto).
                    if (SimLevelBuilder.EsObraDelTaller(x, y) && !SimLevelBuilder.EsRepisaDeCascada(x, y)) continue;

                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Velocidad de un avatar remoto a partir de cuánto se ha movido su
        /// transform desde el frame anterior. Se suaviza con la MISMA
        /// aceleración que el movimiento local para que el aleteo (que escala
        /// con `speedFrac`) no dé saltos con cada paquete de red.
        /// </summary>
        private void DeducirVelocidadRemota()
        {
            Vector3 pos = transform.position;

            if (!_tienePosicionAnterior)
            {
                _posicionAnterior = pos;
                _tienePosicionAnterior = true;
                return;
            }

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector2 medida = new Vector2(pos.x - _posicionAnterior.x, pos.y - _posicionAnterior.y) / dt;
            _posicionAnterior = pos;

            if (medida.sqrMagnitude > moveSpeed * moveSpeed)
            {
                medida = medida.normalized * moveSpeed;
            }

            _velocity = Vector2.MoveTowards(_velocity, medida, acceleration * Time.deltaTime);

            // (R118b) el avatar remoto no manda su modo: se deduce -- apoyado
            // y sin velocidad vertical = a pie (camina); si no, vuela.
            _enSuelo = SobreSuelo(pos);
            _volando = !(_enSuelo && Mathf.Abs(_velocity.y) < 0.3f);

            if (medida.x > 0.05f) _facingRight = true;
            else if (medida.x < -0.05f) _facingRight = false;
        }

        /// <summary>
        /// LA LIBREA DEL JUGADOR (playtest 28): tiñe las capas del cuerpo
        /// (cuerpo, las dos alas y la cola) MULTIPLICANDO sobre su color de
        /// fábrica — el ala trasera, que nace un punto más apagada que la
        /// delantera, conserva esa diferencia; el sprite procedural sigue
        /// siendo el mismo imp, vestido del color de su jugador. El tarro que
        /// carga se deja SIN teñir a propósito: es vidrio, y el jugador lo usa
        /// para leer de qué color es lo que lleva dentro.
        /// </summary>
        public void AplicarTinte(Color tinte)
        {
            _tinte = tinte;
            if (_capasConTinte == null) return;

            for (int i = 0; i < _capasConTinte.Length; i++)
            {
                var sr = _capasConTinte[i];
                if (sr == null) continue;
                Color baseC = _coloresDeFabrica[i];
                sr.color = new Color(baseC.r * tinte.r, baseC.g * tinte.g, baseC.b * tinte.b, baseC.a * tinte.a);
            }
        }

        /// <summary>Registra las capas que reciben tinte con su color de fábrica y aplica el tinte que hubiera pendiente (AprendizNet puede llamarlo antes de que exista el rig).</summary>
        private void RegistrarCapasConTinte(params SpriteRenderer[] capas)
        {
            _capasConTinte = capas;
            _coloresDeFabrica = new Color[capas.Length];
            for (int i = 0; i < capas.Length; i++)
            {
                _coloresDeFabrica[i] = capas[i] != null ? capas[i].color : Color.white;
            }

            AplicarTinte(_tinte);
        }

        private void HandleVisual()
        {
            if (_usingCustomSprite)
            {
                // (R108) LA ESTAMPA DEL MUÑECO DE REMIENDOS: una sola imagen,
                // pero viva — bob, bandeo al acelerar, espejo al mirar, y sus
                // dos herramientas (frasco y plano) en la mano. Sin parpadeo:
                // los ojos del muñeco son lámparas, no párpados.
                float dirSignC = _facingRight ? 1f : -1f;
                // (R118) ¿camina? Solo con velocidad horizontal real y fuera
                // del modo capataz (ahí mira a cámara y la hoja es de perfil 3/4).
                bool frontalC = Mudanza.ModoActivo;
                // (R118b) A PIE la hoja manda: camina con velocidad horizontal
                // real (u/s, no fracción del vuelo) y el ciclo corre al ritmo
                // de la zancada. Volando o quieto, la estampa base.
                bool aPie = EnSuelo;
                float velX = Mathf.Abs(_velocity.x);
                bool quiereCaminar = _hojaCaminar != null && !frontalC && aPie && velX > UmbralCaminar;
                if (!TickGesto(aPie, velX)) TickCaminar(quiereCaminar, velX);
                // A pie NO hay bob ni bandeo: los pies están en el suelo (Cesar:
                // "parece que levita"). El flotar es del vuelo. Y a pie el
                // visual baja lo que la caja de colisión le reservaba al bob
                // (R118c: "camina un poco por el aire"): los pies pisan.
                float bobC = aPie ? -AjusteVisualAPie : Mathf.Sin(Time.time * BobFrequency) * BobAmplitude;
                if (_visualTransform != null)
                    _visualTransform.localPosition = new Vector3(0f, bobC, VisualZOffset);

                float targetTiltC = aPie ? 0f : Mathf.Clamp(-_velocity.x * TiltDegPerSpeed, -TiltMaxDeg, TiltMaxDeg);
                _tiltDeg = Mathf.Lerp(_tiltDeg, targetTiltC, 1f - Mathf.Exp(-TiltSmooth * Time.deltaTime));
                if (_tiltPivot != null) _tiltPivot.localRotation = Quaternion.Euler(0f, 0f, _tiltDeg);

                // En mudanza el capataz mira a cámara: el arte YA es un 3/4
                // frontal, así que basta con no espejarlo.
                if (_bodySr != null) _bodySr.flipX = !frontalC && !_facingRight;

                if (_planoSr != null)
                {
                    _planoSr.enabled = frontalC;
                    if (frontalC)
                        _planoTr.localPosition = new Vector3(dirSignC * 0.40f, -0.10f, VisualZOffset - 0.02f);
                        _planoTr.localScale = new Vector3(0.11f, 0.16f, 1f); // (R116) el mapita crece con el muñeco (antes 0.06x0.09, talla del imp).
                }
                TickSombraDeContacto();
                if (_carriedFlaskTr != null)
                {
                    // (R116, Cesar: "recalcula la botellita... quedó
                    // descuadrado") El tarro persigue LA MISMA mano que
                    // CarryAnchor (0.42, -0.16) y crece x1.9: a escala del
                    // muñeco, no del imp. El muelle de pop multiplica encima.
                    Vector3 flaskTargetC = new Vector3(dirSignC * 0.42f, -0.16f, VisualZOffset - 0.01f);
                    _carriedFlaskTr.localPosition = Vector3.SmoothDamp(_carriedFlaskTr.localPosition, flaskTargetC, ref _carriedFlaskVel, CarriedFlaskLag);
                    _carriedFlaskTr.localScale = Vector3.one * 1.9f * (1f + _pulsoFrasco);
                    _carriedFlaskTr.localRotation = Quaternion.Euler(0f, 0f, _inclinacionFrasco);
                    _carriedFlaskSr.enabled = !FundacionDirector.FrascoBloqueado;
                    _carriedFlaskSr.flipX = !_facingRight;
                }
                return;
            }

            float dirSign = _facingRight ? 1f : -1f;
            float speedFrac = Mathf.Clamp01(_velocity.magnitude / moveSpeed);

            // --- 1) Flotar: bobbing vertical suave (valores intactos del playtest 4). ---
            float bob = Mathf.Sin(Time.time * BobFrequency) * BobAmplitude;
            _visualTransform.localPosition = new Vector3(0f, bob, VisualZOffset);

            // --- 3) Inclinación: el cuerpo se bandea hacia donde acelera — lo
            // que da "peso" a un personaje que flota en vez de caminar. ---
            float targetTilt = Mathf.Clamp(-_velocity.x * TiltDegPerSpeed, -TiltMaxDeg, TiltMaxDeg);
            _tiltDeg = Mathf.Lerp(_tiltDeg, targetTilt, 1f - Mathf.Exp(-TiltSmooth * Time.deltaTime));
            _tiltPivot.localRotation = Quaternion.Euler(0f, 0f, _tiltDeg);

            // --- 2) Aleteo: la FRECUENCIA comunica el esfuerzo (la amplitud
            // se mantiene, solo cambia lo rápido que bate). La trasera va
            // ligeramente desfasada: rompe la simetría perfecta y da vida. ---
            float targetHz = Mathf.Lerp(FlapHzIdle, FlapHzMax, speedFrac);
            _flapHz = Mathf.Lerp(_flapHz, targetHz, 1f - Mathf.Exp(-FlapSmooth * Time.deltaTime));
            _wingPhase += _flapHz * Time.deltaTime * Mathf.PI * 2f;
            float flapMain = Mathf.Sin(_wingPhase) * FlapAmplitudeDeg;
            float flapBack = Mathf.Sin(_wingPhase - 0.5f) * FlapAmplitudeDeg * 0.85f;
            _wingFrontTr.localRotation = Quaternion.Euler(0f, 0f, flapMain);
            _wingBackTr.localRotation = Quaternion.Euler(0f, 0f, flapBack);

            // Anclajes que dependen de a qué lado mira: solo mover transforms
            // ya creados, nunca regenerar nada (regla de "cero asignaciones
            // por frame" del brief).
            _tailTr.localPosition = new Vector3(-0.20f * dirSign, -0.10f, 0f);
            _wingBackTr.localPosition = new Vector3(-0.05f * dirSign, -0.01f, 0f);
            _wingFrontTr.localPosition = new Vector3(-0.05f * dirSign, 0.02f, 0f);

            _bodySr.flipX = !_facingRight;
            _tailSr.flipX = !_facingRight;
            _wingBackSr.flipX = !_facingRight;
            _wingFrontSr.flipX = !_facingRight;

            // --- 4) Parpadeo: se alternan dos sprites pre-generados (nunca se
            // crea textura en Update), en intervalos irregulares. ---
            if (Time.time >= _nextBlinkAt)
            {
                _blinkUntil = Time.time + BlinkDuration;
                ScheduleNextBlink();
            }
            // (R98) EL CAPATAZ: en modo mudanza el imp mira A CÁMARA (pose
            // frontal, sin espejo — el frente no tiene lado) con su plano
            // bajo el brazo. Alas y cola siguen batiendo: es un imp volador
            // dirigiendo, no una estatua.
            bool frontalMudanza = Mudanza.ModoActivo;
            if (frontalMudanza) _bodySr.flipX = false;
            _bodySr.sprite = frontalMudanza ? _bodySpriteFrontal
                : (Time.time < _blinkUntil ? _bodySpriteClosed : _bodySpriteOpen);
            if (_planoSr != null)
            {
                _planoSr.enabled = frontalMudanza;
                if (frontalMudanza)
                    _planoTr.localPosition = new Vector3(dirSign * 0.28f, -0.29f, VisualZOffset - 0.02f);
            }

            // --- 5) El frasco que lleva: persigue con inercia leve el mismo
            // punto que usa Flask.cs (CarryAnchor) para su propio indicador de
            // contenido, así el tarro y la mancha de color quedan alineados. ---
            Vector3 flaskTarget = new Vector3(dirSign * 0.28f, -0.35f, VisualZOffset - 0.01f);
            _carriedFlaskTr.localPosition = Vector3.SmoothDamp(_carriedFlaskTr.localPosition, flaskTarget, ref _carriedFlaskVel, CarriedFlaskLag);
            _carriedFlaskTr.localScale = Vector3.one * (1f + _pulsoFrasco); // (ronda 69c) el tarro respira con el muelle del frasco.
            _carriedFlaskTr.localRotation = Quaternion.Euler(0f, 0f, _inclinacionFrasco); // (ronda 69d) y se ladea al verter.
            _carriedFlaskSr.enabled = !FundacionDirector.FrascoBloqueado; // (ronda 73) antes del TOMA. del prólogo no llevas frasco: el tarro decorativo tampoco existe.
            _carriedFlaskSr.flipX = !_facingRight;
        }

        private void ScheduleNextBlink()
        {
            // Irregular a propósito ("barato y hace que el personaje parezca
            // vivo"): entre 2.2 y 6.5 s, calculado SOLO al reprogramar, nunca
            // por frame.
            // (R74) ??=: en un hot-reload del editor Unity reconstruye el
            // objeto y _rng (System.Random, no serializable) resucita en null
            // — el mismo patrón del diálogo de la ronda 64b. Solo cosmética
            // del parpadeo (jamás la sim), así que basta con reponerla.
            _rng ??= new System.Random();
            _nextBlinkAt = Time.time + 2.2f + (float)_rng.NextDouble() * 4.3f;
        }

        // ===================================================================
        // Construcción del rig (una única vez, en Awake).
        // ===================================================================

        private void BuildVisual()
        {
            var visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(transform, false);
            _visualTransform = visualGo.transform;

            // (R108) EL MUÑECO DE REMIENDOS: si el arte real vive en Resources
            // (o hay un sprite manual en el inspector, que sigue mandando), el
            // aprendiz se viste con la estampa a TALLA 12 CELDAS (el PNG se
            // importa a 1000 px/unidad con 1200 px de alto — ver su .meta).
            // El rig procedimental del imp queda como retén honesto para
            // cuando falte el asset. La colisión NO se toca (decreto R108):
            // solo crece el cuerpo visual.
            var estampa = customSprite != null
                ? customSprite
                : Resources.Load<Sprite>("Personaje/MunhecoRemiendos");
            if (estampa != null)
            {
                _usingCustomSprite = true;
                var tiltEstampaGo = new GameObject("Tilt");
                tiltEstampaGo.transform.SetParent(visualGo.transform, false);
                _tiltPivot = tiltEstampaGo.transform;
                _bodySr = CrearCapa(_tiltPivot, "Cuerpo", estampa, sortingOrder);
                // (R113, integracion barata que ya vale la pena) BAÑO TONAL:
                // la ilustracion viene a plena luz de estudio y el taller es
                // una cueva — un multiplicador calido-oscuro sutil la sienta
                // en la escena sin tocar el arte. Es color de fabrica: el
                // tinte de jugador multiplica ENCIMA (patron del ala trasera).
                _bodySr.color = new Color(0.94f, 0.90f, 0.86f, 1f);
                RegistrarCapasConTinte(_bodySr);
                CrearSombraDeContacto();
                CrearFrascoYPlano();
                // (R118) la hoja de caminar, si el arnés ya la dejó en Resources.
                _hojaCaminar = HojaDeCuadros.Cargar("caminar");
                _hojaReposo = HojaDeCuadros.Cargar("reposo");
                _hojaRecoger = HojaDeCuadros.Cargar("recoger");
                // (R118d, Cesar: "cuando termina de caminar la cabeza popea...
                // se ve mejor caminando que de frente con la cabeza más
                // grande") LA POSE QUIETA SALE DEL MISMO VIDEO: el cuadro
                // BASE de la hoja de reposo (o el 0 de caminar) es la estampa
                // tal como la re-dibujó el modelo (misma talla, misma luz que
                // los ciclos). (R118f) Todas las hojas se generan desde la
                // misma REFERENCIA CANÓNICA (muneco_canon.png = ese cuadro),
                // así reposo↔caminar↔recoger arrancan en la misma pose.
                // La estampa original queda de retén si no hay hoja.
                _estampaBase = _hojaReposo != null ? _hojaReposo.CuadroBase : (_hojaCaminar != null ? _hojaCaminar.Cuadros[0] : estampa);
                if (_bodySr != null) _bodySr.sprite = _estampaBase;
                return;
            }

            var tiltGo = new GameObject("Tilt");
            tiltGo.transform.SetParent(visualGo.transform, false);
            _tiltPivot = tiltGo.transform;

            // Orden de dibujo (todo en el mismo plano; la "profundidad" la da
            // sortingOrder, no la Z).
            //
            // (fix playtest 9) "el ala del nuevo personaje está por encima del resto del
            // cuerpo": AlaDelantera vivía en sortingOrder+1, POR ENCIMA del cuerpo
            // (sortingOrder) -- en cada aleteo el ala delantera tapaba la cara/torso del
            // imp. Vista de perfil, las DOS alas nacen del lomo (ninguna sale "por
            // delante" de la cara): ambas van DETRÁS del cuerpo. Orden final, de más
            // atrás a más adelante: Cola < AlaTrasera < AlaDelantera < Cuerpo. Entre las
            // dos alas, la delantera queda un pelín más cerca del cuerpo que la trasera
            // (para no perder sensación de profundidad al ponerlas ambas detrás), y ya
            // se distinguían por tono (AlaTrasera apagada) y por el desfase de fase del
            // aleteo (ver HandleVisual: flapBack usa fase y amplitud distintas) -- eso no
            // se toca.
            var tailSprite = CrearSprite(GenerateTailTexture(), new Vector2(TailHingePx.x / TailTexW, TailHingePx.y / TailTexH));
            _tailSr = CrearCapa(_tiltPivot, "Cola", tailSprite, sortingOrder - 3);
            _tailTr = _tailSr.transform;

            var wingSprite = CrearSprite(GenerateWingTexture(), new Vector2(WingHingePx.x / WingTexW, WingHingePx.y / WingTexH));
            _wingBackSr = CrearCapa(_tiltPivot, "AlaTrasera", wingSprite, sortingOrder - 2);
            _wingBackTr = _wingBackSr.transform;
            _wingBackSr.color = new Color(0.74f, 0.70f, 0.82f, 1f); // un pelín más apagada: queda más atrás que la otra ala.

            // Misma textura que la trasera: dos instancias de UN sprite, sin
            // duplicar la generación (dos SpriteRenderer distintos, un solo
            // Texture2D/Sprite compartido). sortingOrder-1: por delante de
            // AlaTrasera (más cerca del cuerpo, para no perder profundidad),
            // pero SIEMPRE por detrás del Cuerpo (sortingOrder).
            _wingFrontSr = CrearCapa(_tiltPivot, "AlaDelantera", wingSprite, sortingOrder - 1);
            _wingFrontTr = _wingFrontSr.transform;

            _bodySpriteOpen = CrearSprite(GenerateBodyTexture(false), new Vector2(0.5f, 0.5f));
            _bodySpriteClosed = CrearSprite(GenerateBodyTexture(true), new Vector2(0.5f, 0.5f));
            _bodySpriteFrontal = CrearSprite(GenerateBodyTexture(false, frontal: true), new Vector2(0.5f, 0.5f)); // (R98) ojos juntos y pupilas centradas: a 8 px/celda ya se lee "volteó a cámara".
            _bodySr = CrearCapa(_tiltPivot, "Cuerpo", _bodySpriteOpen, sortingOrder);

            CrearFrascoYPlano();

            // (playtest 28) Las cuatro capas del CUERPO que lleva la librea de
            // color del jugador. Se registran DESPUÉS de que el ala trasera
            // reciba su color apagado, para que ese matiz sea parte del "color
            // de fábrica" sobre el que multiplica el tinte.
            RegistrarCapasConTinte(_bodySr, _wingFrontSr, _wingBackSr, _tailSr);

            _rng = new System.Random();
            ScheduleNextBlink();
        }

        // (R113) LA SOMBRA DE CONTACTO: la segunda integracion barata. Una
        // elipse suave en el PRIMER suelo bajo los pies (sondeo de la grilla,
        // max 8 celdas), que se encoge y desvanece con la altura de vuelo —
        // el truco de los levitadores de siempre: ata al personaje al terreno
        // sin fisica nueva. Se retirara cuando el pipeline 3D traiga sombra
        // propia, si la trae.
        private SpriteRenderer _sombraSr;
        private Transform _sombraTr;

        private void CrearSombraDeContacto()
        {
            var tex = new Texture2D(48, 16, TextureFormat.RGBA32, false);
            var px = new Color32[48 * 16];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 48; x++)
                {
                    float dx = (x - 23.5f) / 23.5f, dy = (y - 7.5f) / 7.5f;
                    float d = dx * dx + dy * dy;
                    byte a = (byte)(d >= 1f ? 0 : Mathf.RoundToInt(200f * (1f - d) * (1f - d)));
                    px[y * 48 + x] = new Color32(0, 0, 0, a);
                }
            tex.SetPixels32(px); tex.filterMode = FilterMode.Bilinear; tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, 48, 16), new Vector2(0.5f, 0.5f), 60f);
            var go = new GameObject("SombraDeContacto");
            go.transform.SetParent(transform, false);
            _sombraTr = go.transform;
            _sombraSr = go.AddComponent<SpriteRenderer>();
            _sombraSr.sprite = sp;
            _sombraSr.sortingOrder = sortingOrder - 4;
            _sombraSr.enabled = false;
        }

        private void TickSombraDeContacto()
        {
            if (_sombraSr == null) return;
            if (_simColision == null) _simColision = FindAnyObjectByType<AlkahestSim>();
            if (_simColision == null) { _sombraSr.enabled = false; return; }
            float c = SimRenderer.CellWorldSize;
            int cx = Mathf.FloorToInt(transform.position.x / c);
            int piesY = Mathf.FloorToInt((transform.position.y - MedioAltoAbajo) / c);
            int suelo = -1;
            for (int d = 0; d <= 8; d++)
            {
                int y = piesY - d;
                if (!CellGrid.InBounds(cx, y)) break;
                int m = _simColision.SampleMaterial(cx, y);
                if (m == MaterialId.Stone || m == MaterialId.PisoEstructural) { suelo = y; break; }
            }
            if (suelo < 0) { _sombraSr.enabled = false; return; }
            float dist = (transform.position.y - MedioAltoAbajo) - (suelo + 1) * c;
            float t = Mathf.Clamp01(dist / (8f * c));
            _sombraSr.enabled = true;
            _sombraTr.position = new Vector3(transform.position.x, (suelo + 1) * c + 0.02f, 0f);
            // (R115, Cesar: "muy tímida, no se nota") Presencia real: más
            // grande y bastante más opaca — sobre piedra oscura, 0.55 de alfa
            // se comía la gamma. Sigue desvaneciéndose al volar alto.
            // (R116: "acentúa la sombra todavía un 20% más y con eso ya
            // queda") 0.85 -> 1.0 de alfa pegado al suelo (el tope físico) y
            // un pelín más de cuerpo lejos.
            _sombraTr.localScale = Vector3.one * Mathf.Lerp(1.45f, 0.80f, t);
            var col = _sombraSr.color; col.a = Mathf.Lerp(1.0f, 0.30f, t);
            _sombraSr.color = col;
        }

        /// <summary>
        /// (R108) Las dos herramientas de mano, comunes al imp procedimental y
        /// a la estampa del muñeco. El tarro cargado cuelga del aprendiz
        /// DIRECTAMENTE (no del nodo "Visual" con bobbing/inclinación): así su
        /// propia inercia (SmoothDamp) es la única fuente de su movimiento
        /// relativo, y no se le suma el cabeceo del cuerpo. El plano enrollado
        /// (R98) sigue siendo un tubo crema placeholder a conciencia — sin
        /// remates de latón (mandato de Cesar).
        /// </summary>
        private void CrearFrascoYPlano()
        {
            var flaskSprite = CrearSprite(GenerateCarriedFlaskTexture(), new Vector2(0.5f, 0.5f));
            _carriedFlaskSr = CrearCapa(transform, "FrascoCargado", flaskSprite, sortingOrder + 2);
            _carriedFlaskTr = _carriedFlaskSr.transform;

            var planoGo = new GameObject("PlanoEnMano");
            planoGo.transform.SetParent(transform, false);
            _planoTr = planoGo.transform;
            _planoSr = planoGo.AddComponent<SpriteRenderer>();
            _planoSr.sprite = MaquinariaSprites.Solido();
            _planoSr.sortingOrder = sortingOrder + 3;
            _planoSr.color = new Color(226f / 255f, 214f / 255f, 182f / 255f, 1f);
            _planoTr.localScale = new Vector3(0.06f, 0.09f, 1f);
            _planoTr.localRotation = Quaternion.Euler(0f, 0f, -24f);
            _planoSr.enabled = false;
        }

        private static Sprite CrearSprite(Texture2D tex, Vector2 pivot01) =>
            Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot01, SpritePixelsPerUnit);

        private static SpriteRenderer CrearCapa(Transform padre, string nombre, Sprite sprite, int orden)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = orden;
            return sr;
        }

        // ===================================================================
        // Generación de texturas (M5 aprendiz): misma técnica que
        // MaquinariaSprites — Color32[] rellenado a mano + Sprite.Create +
        // FilterMode.Point. Cero Shader.Find, cero assets.
        // ===================================================================

        /// <summary>
        /// Cuerpo del imp: cabeza grande + cuerpo pequeño (silueta antes que
        /// detalle), collar y gema de latón, dos cuernecillos con punta
        /// pulida, y los ojos (el foco de la lectura del personaje).
        /// </summary>
        private static Texture2D GenerateBodyTexture(bool eyesClosed, bool frontal = false)
        {
            const int w = BodyTexW, h = BodyTexH;
            var px = new Color32[w * h];
            float cx = w * 0.5f;

            // Dos elipses solapadas un par de filas (para no pellizcarse a
            // cero de ancho en la costura del cuello): cabeza grande arriba,
            // cuerpo pequeño abajo — el imp FLOTA, no necesita piernas.
            const float headCy = 55f, headRx = 22f, headRy = 23f;
            const float bodyCy = 21f, bodyRx = 10f, bodyRy = 13f;

            // Una única fuente de luz (arriba-izquierda) para toda la figura,
            // calculada sobre un centro "global" y no por elipse: así no hay
            // costura de sombreado en el cuello.
            const float shadeCx = w * 0.5f, shadeCy = 40f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool inHead = InEllipse(x, y, cx, headCy, headRx, headRy);
                    bool inBody = InEllipse(x, y, cx, bodyCy, bodyRx, bodyRy);
                    if (!inHead && !inBody) continue;

                    float dx = (x + 0.5f - shadeCx) / headRx;
                    float dy = (y + 0.5f - shadeCy) / (headRy + bodyRy);

                    Color32 c;
                    if (dy < -0.30f) c = ColBodyShadow;
                    else if (dx < -0.05f && dy > -0.05f) c = ColBodyLight;
                    else c = ColBodyBase;

                    px[y * w + x] = c;
                }
            }

            // Collar de latón: la costura cuello-cuerpo se convierte en un
            // acento cálido pequeño, tal y como pide el brief.
            for (int y = 32; y <= 34; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i = y * w + x;
                    if (px[i].a == 0) continue;
                    px[i] = (y == 34) ? ColBrassLight : ColBrassMed;
                }
            }

            // Cuernecillos: dos ganchos finos con punta de latón pulido — lo
            // que dice "imp" y no "fantasma morado".
            StampSweep(px, w, h, new Vector2(cx - 9f, 74f), new Vector2(cx - 13f, 80f), new Vector2(cx - 16f, 83f), 2.4f, 0f, 0.30f, ColBodyShadow, 16);
            FillEllipse(px, w, h, cx - 16f, 83f, 1.1f, 1.1f, ColBrassLight);
            StampSweep(px, w, h, new Vector2(cx + 8f, 75f), new Vector2(cx + 12f, 80f), new Vector2(cx + 15f, 83f), 2.4f, 0f, 0.30f, ColBodyShadow, 16);
            FillEllipse(px, w, h, cx + 15f, 83f, 1.1f, 1.1f, ColBrassLight);

            // Gema de latón en la frente: el detalle de "aparato del taller"
            // que el imp lleva encima, no un adorno de mascota.
            FillEllipse(px, w, h, cx, 63f, 2.1f, 2.1f, ColBrassMed);
            FillEllipse(px, w, h, cx - 0.6f, 63.6f, 0.9f, 0.9f, ColBrassLight);

            // Ojos: grandes, claros, con punto de luz — el foco de la lectura.
            // La pupila mira ligeramente hacia el frente (curiosidad, hacia
            // donde el jugador va a llevar al personaje).
            const float eyeY = 57f, eyeRx = 7.2f, eyeRy = 8.2f;
            float eyeOffX = frontal ? 7f : 10f;          // (R98) frontal: ojos más juntos —
            float pupilBias = frontal ? 0f : 1.6f;       // — y pupilas centradas: te mira a TI.
            DrawEye(px, w, h, cx - eyeOffX, eyeY, eyeRx, eyeRy, eyesClosed, pupilBias);
            DrawEye(px, w, h, cx + eyeOffX, eyeY, eyeRx, eyeRy, eyesClosed, pupilBias);

            AplicarContorno(px, w, h, ColOutline);
            return CrearTextura(px, w, h, eyesClosed ? "AlkahestApprenticeCuerpoCerrado" : "AlkahestApprenticeCuerpoAbierto");
        }

        private static void DrawEye(Color32[] px, int w, int h, float ex, float ey, float rx, float ry, bool closed, float pupilBias = 1.6f)
        {
            if (closed)
            {
                // Párpado: un trazo curvo fino en vez de otra textura entera
                // — más barato y se lee perfectamente a este tamaño.
                int half = Mathf.RoundToInt(rx);
                for (int dxp = -half; dxp <= half; dxp++)
                {
                    float t = dxp / rx;
                    int ix = Mathf.RoundToInt(ex) + dxp;
                    int iy = Mathf.RoundToInt(ey - ry * 0.15f + Mathf.Abs(t) * 1.6f);
                    SetPixel(px, w, h, ix, iy, ColOutline);
                    SetPixel(px, w, h, ix, iy - 1, ColOutline);
                }
                return;
            }

            FillEllipse(px, w, h, ex, ey, rx, ry, ColEyeWhite);
            FillEllipse(px, w, h, ex + pupilBias, ey - 0.4f, rx * 0.46f, ry * 0.5f, ColEyePupil);
            FillEllipse(px, w, h, ex + 0.2f, ey + 2.0f, rx * 0.16f, ry * 0.16f, ColEyeHighlight);
        }

        /// <summary>
        /// Ala de polilla/murciélago: un barrido curvo (gozne -> punta) con
        /// bulto central y remate en punta, más una vena de latón central que
        /// convierte "mancha morada" en "ala". El pivote del sprite se pone en
        /// el gozne (ver <see cref="WingHingePx"/>) para que rotarla la bata
        /// desde el hombro.
        /// </summary>
        private static Texture2D GenerateWingTexture()
        {
            const int w = WingTexW, h = WingTexH;
            var px = new Color32[w * h];

            Vector2 hinge = WingHingePx;
            Vector2 control = new Vector2(w - 11f, h * 0.66f);
            Vector2 tip = new Vector2(3f, h - 3f);

            StampSweep(px, w, h, hinge, control, tip, 3f, 6.4f, 0.80f, ColBodyBase, 60);
            StampSweep(px, w, h, hinge, control, tip, 0.9f, 0f, 0.88f, ColBrassMed, 70);

            AplicarContorno(px, w, h, ColOutline);
            return CrearTextura(px, w, h, "AlkahestApprenticeAla");
        }

        /// <summary>Cola fina que se curva hacia abajo, con una diminuta cuenta de latón en la punta.</summary>
        private static Texture2D GenerateTailTexture()
        {
            const int w = TailTexW, h = TailTexH;
            var px = new Color32[w * h];

            Vector2 hinge = TailHingePx;
            Vector2 control = new Vector2(14f, 2f);
            Vector2 tip = new Vector2(29f, 8f);

            StampSweep(px, w, h, hinge, control, tip, 2.6f, 0.9f, 0.55f, ColBodyBase, 50);
            FillEllipse(px, w, h, tip.x, tip.y, 1.1f, 1.1f, ColBrassLight);

            AplicarContorno(px, w, h, ColOutline);
            return CrearTextura(px, w, h, "AlkahestApprenticeCola");
        }

        /// <summary>Frasco decorativo que carga el imp: vidrio con licor ámbar y tapón de latón.</summary>
        private static Texture2D GenerateCarriedFlaskTexture()
        {
            const int w = CarryTexW, h = CarryTexH;
            var px = new Color32[w * h];
            float cx = w * 0.5f;
            const float bodyCy = 8f, bodyRx = 6f, bodyRy = 7.4f;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!InEllipse(x, y, cx, bodyCy, bodyRx, bodyRy)) continue;
                    px[y * w + x] = (y <= 6) ? ColLiquid : ColGlass;
                }
            }
            FillEllipse(px, w, h, cx - 2.5f, 10f, 1.1f, 3.2f, ColGlassRim);

            for (int y = h - 5; y < h; y++)
            {
                float t = (y - (h - 5)) / 4f;
                int half = Mathf.RoundToInt(Mathf.Lerp(3f, 2f, t));
                Color32 c = (y == h - 1) ? ColBrassLight : (y == h - 5 ? ColBrassShadow : ColBrassMed);
                for (int x = Mathf.RoundToInt(cx) - half; x <= Mathf.RoundToInt(cx) + half; x++)
                {
                    SetPixel(px, w, h, x, y, c);
                }
            }

            AplicarContorno(px, w, h, ColOutline);
            return CrearTextura(px, w, h, "AlkahestApprenticeFrascoCargado");
        }

        // -------------------------------------------------------------------
        // Utilidades de dibujo compartidas.
        // -------------------------------------------------------------------

        /// <summary>Barrido curvo (Bézier cuadrática de `a` a `b` vía `control`) estampando discos de radio variable: la técnica que dibuja cuernos, cola y alas con el mismo código.</summary>
        private static void StampSweep(Color32[] px, int w, int h, Vector2 a, Vector2 control, Vector2 b,
            float baseRadius, float bulgeRadius, float taperStartT, Color32 color, int steps)
        {
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                Vector2 p = Bezier(a, control, b, t);
                float bulge = bulgeRadius * Mathf.Sin(t * Mathf.PI);
                float taper = 1f - 0.92f * Mathf.SmoothStep(taperStartT, 1f, t);
                float r = (baseRadius + bulge) * taper;
                if (r > 0.05f) FillEllipse(px, w, h, p.x, p.y, r, r, color);
            }
        }

        private static Vector2 Bezier(Vector2 a, Vector2 control, Vector2 b, float t)
        {
            float u = 1f - t;
            return u * u * a + 2f * u * t * control + t * t * b;
        }

        private static void FillEllipse(Color32[] px, int w, int h, float cx, float cy, float rx, float ry, Color32 color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx));
            int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + rx));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry));
            int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + ry));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (InEllipse(x, y, cx, cy, rx, ry)) px[y * w + x] = color;
                }
            }
        }

        private static void SetPixel(Color32[] px, int w, int h, int x, int y, Color32 c)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return;
            px[y * w + x] = c;
        }

        private static bool InEllipse(float px, float py, float cx, float cy, float rx, float ry)
        {
            float dx = (px + 0.5f - cx) / rx;
            float dy = (py + 0.5f - cy) / ry;
            return dx * dx + dy * dy <= 1f;
        }

        /// <summary>
        /// Contorno de 1 téxel: para cada píxel TRANSPARENTE con al menos un
        /// vecino opaco (4-vecindad), lo pinta del color de contorno. Es lo
        /// que garantiza silueta legible contra CUALQUIER fondo (mampostería
        /// oscura o materia muy saturada) — regla obligatoria del brief.
        /// Opera sobre una copia para no contaminar el criterio de "opaco"
        /// mientras recorre la textura.
        /// </summary>
        private static void AplicarContorno(Color32[] px, int w, int h, Color32 colorContorno)
        {
            var original = (Color32[])px.Clone();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (original[y * w + x].a != 0) continue;
                    bool vecinoOpaco =
                        (x > 0 && original[y * w + x - 1].a != 0) ||
                        (x < w - 1 && original[y * w + x + 1].a != 0) ||
                        (y > 0 && original[(y - 1) * w + x].a != 0) ||
                        (y < h - 1 && original[(y + 1) * w + x].a != 0);
                    if (vecinoOpaco) px[y * w + x] = colorContorno;
                }
            }
        }

        private static Texture2D CrearTextura(Color32[] px, int w, int h, string nombre)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = nombre,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }
    }
}
