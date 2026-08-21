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
        [SerializeField] private float moveSpeed = 11.2f;
        [SerializeField] private float acceleration = 44f; // unidades/s^2 de suavizado hacia la velocidad objetivo (escalado con la velocidad nueva para que el arranque siga siendo igual de nítido)

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

        private SpriteRenderer _bodySr;
        private Sprite _bodySpriteOpen;
        private Sprite _bodySpriteClosed;

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
        public Vector3 CarryAnchor => transform.position + new Vector3(_facingRight ? 0.28f : -0.28f, -0.35f, 0f);

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

            Vector2 target = input * moveSpeed;
            _velocity = Vector2.MoveTowards(_velocity, target, acceleration * Time.deltaTime);

            Vector3 pos = transform.position;
            MoverConColision(ref pos, _velocity * Time.deltaTime);
            pos.z = 0f;
            transform.position = pos;

            if (input.x > 0.01f) _facingRight = true;
            else if (input.x < -0.01f) _facingRight = false;
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
        //  · Caja de ~2x3 celdas resuelta POR EJE (deslizas por las paredes
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

        private const float MedioAnchoColision = 0.10f;  // 1 celda a cada lado del centro.
        private const float MedioAltoColision = 0.14f;   // ~3 celdas de alto total.
        private const float SubPaso = 0.06f;             // < 1 celda por subpaso: sin túneles.

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
                    if (CajaChoca(px, pos.y)) { pasoX = 0f; _velocity.x = 0f; }
                    else pos.x = px;
                }
                if (pasoY != 0f)
                {
                    float py = pos.y + pasoY;
                    if (CajaChoca(pos.x, py)) { pasoY = 0f; _velocity.y = 0f; }
                    else pos.y = py;
                }
                if (pasoX == 0f && pasoY == 0f) break;
            }
        }

        /// <summary>true si la caja del imp centrada en (cx,cy) pisa algún sólido estructural. Muestrea las 4 esquinas + 2 puntos medios laterales (alto 3 celdas: el centro lateral evita colarse por un diente de 1 celda).</summary>
        private bool CajaChoca(float cx, float cy)
        {
            return PuntoChoca(cx - MedioAnchoColision, cy - MedioAltoColision)
                || PuntoChoca(cx + MedioAnchoColision, cy - MedioAltoColision)
                || PuntoChoca(cx - MedioAnchoColision, cy + MedioAltoColision)
                || PuntoChoca(cx + MedioAnchoColision, cy + MedioAltoColision)
                || PuntoChoca(cx - MedioAnchoColision, cy)
                || PuntoChoca(cx + MedioAnchoColision, cy);
        }

        private bool PuntoChoca(float wx, float wy)
        {
            int x = Mathf.FloorToInt(wx / SimRenderer.CellWorldSize);
            int y = Mathf.FloorToInt(wy / SimRenderer.CellWorldSize);
            if (!CellGrid.InBounds(x, y)) return false;
            int m = _simColision.SampleMaterial(x, y);
            return m == MaterialId.Stone || m == MaterialId.PisoEstructural;
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
                if (_bodySr != null) _bodySr.flipX = !_facingRight;
                if (_visualTransform != null)
                {
                    float bobC = Mathf.Sin(Time.time * BobFrequency) * BobAmplitude;
                    _visualTransform.localPosition = new Vector3(0f, bobC, VisualZOffset);
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
            _bodySr.sprite = Time.time < _blinkUntil ? _bodySpriteClosed : _bodySpriteOpen;

            // --- 5) El frasco que lleva: persigue con inercia leve el mismo
            // punto que usa Flask.cs (CarryAnchor) para su propio indicador de
            // contenido, así el tarro y la mancha de color quedan alineados. ---
            Vector3 flaskTarget = new Vector3(dirSign * 0.28f, -0.35f, VisualZOffset - 0.01f);
            _carriedFlaskTr.localPosition = Vector3.SmoothDamp(_carriedFlaskTr.localPosition, flaskTarget, ref _carriedFlaskVel, CarriedFlaskLag);
            _carriedFlaskSr.flipX = !_facingRight;
        }

        private void ScheduleNextBlink()
        {
            // Irregular a propósito ("barato y hace que el personaje parezca
            // vivo"): entre 2.2 y 6.5 s, calculado SOLO al reprogramar, nunca
            // por frame.
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

            if (customSprite != null)
            {
                _usingCustomSprite = true;
                _bodySr = visualGo.AddComponent<SpriteRenderer>();
                _bodySr.sprite = customSprite;
                _bodySr.sortingOrder = sortingOrder;
                RegistrarCapasConTinte(_bodySr);
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
            _bodySr = CrearCapa(_tiltPivot, "Cuerpo", _bodySpriteOpen, sortingOrder);

            var flaskSprite = CrearSprite(GenerateCarriedFlaskTexture(), new Vector2(0.5f, 0.5f));
            // El tarro cargado cuelga del aprendiz DIRECTAMENTE (no del nodo
            // "Visual" con bobbing/inclinación): así su propia inercia
            // (SmoothDamp) es la única fuente de su movimiento relativo, y no
            // se le suma el cabeceo del cuerpo.
            _carriedFlaskSr = CrearCapa(transform, "FrascoCargado", flaskSprite, sortingOrder + 2);
            _carriedFlaskTr = _carriedFlaskSr.transform;

            // (playtest 28) Las cuatro capas del CUERPO que lleva la librea de
            // color del jugador. Se registran DESPUÉS de que el ala trasera
            // reciba su color apagado, para que ese matiz sea parte del "color
            // de fábrica" sobre el que multiplica el tinte.
            RegistrarCapasConTinte(_bodySr, _wingFrontSr, _wingBackSr, _tailSr);

            _rng = new System.Random();
            ScheduleNextBlink();
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
        private static Texture2D GenerateBodyTexture(bool eyesClosed)
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
            const float eyeY = 57f, eyeOffX = 10f, eyeRx = 7.2f, eyeRy = 8.2f;
            DrawEye(px, w, h, cx - eyeOffX, eyeY, eyeRx, eyeRy, eyesClosed);
            DrawEye(px, w, h, cx + eyeOffX, eyeY, eyeRx, eyeRy, eyesClosed);

            AplicarContorno(px, w, h, ColOutline);
            return CrearTextura(px, w, h, eyesClosed ? "AlkahestApprenticeCuerpoCerrado" : "AlkahestApprenticeCuerpoAbierto");
        }

        private static void DrawEye(Color32[] px, int w, int h, float ex, float ey, float rx, float ry, bool closed)
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
            FillEllipse(px, w, h, ex + 1.6f, ey - 0.4f, rx * 0.46f, ry * 0.5f, ColEyePupil);
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
