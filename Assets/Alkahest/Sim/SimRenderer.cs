using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Game;

namespace Alkahest.Sim
{
    /// <summary>
    /// Traduce el estado de <see cref="CellGrid"/> a una textura y la
    /// muestra sobre un quad en espacio de mundo. Solo redibuja los chunks
    /// despiertos DENTRO DEL RANGO QUE TOCA LA CÁMARA (más un refresco
    /// completo periódico de esa misma vista, por si algo se desincronizase
    /// -- ver RenderFrame) para mantener el coste de subida a GPU proporcional
    /// a la PANTALLA, no al mundo entero (playtest 15: el mundo pasó a medir
    /// 3x2 pantallas). También posee la cámara: la sigue con zona muerta y
    /// suavizado (ver UpdateCameraFollow).
    /// </summary>
    public sealed class SimRenderer : MonoBehaviour
    {
        /// <summary>Tamaño en unidades de mundo de una celda de simulación.</summary>
        public const float CellWorldSize = 0.1f;

        /// <summary>Color de fondo de cámara recomendado (charcoal cálido oscuro).</summary>
        public static readonly Color BackgroundColor = new Color32(0x1A, 0x14, 0x18, 0xFF);

        /// <summary>
        /// (playtest 31, ILUMINACIÓN DE ÁNIMO) TINTE GLOBAL DEL CUADRO. El
        /// taller tenía que sentirse CUEVA CÁLIDA y se sentía "sala de
        /// exposición": todo igual de iluminado en todas partes, así que
        /// ninguna zona podía destacar por tener luz propia. Este multiplicador
        /// baja el brillo general un ~16% y lo enfría un punto -- no porque la
        /// oscuridad sea bonita por sí sola, sino porque LOS HALOS DE LAS
        /// MÁQUINAS (Game/MaquinariaSprites.Luz) solo pueden leerse como luz
        /// si hay penumbra alrededor contra la que destacar. Es un tinte del
        /// SpriteRenderer (una multiplicación en la GPU sobre el sprite ya
        /// compuesto), NO un cambio en ComputeCellColor: coste cero por celda,
        /// y la textura de la sim sigue siendo exactamente la misma, así que
        /// nada de lo que dependa del color de una celda (firma visual,
        /// patrones, bordes) cambia de significado. Si algún día hace falta un
        /// "amanecer/anochecer", este es el único punto que hay que animar.
        /// </summary>
        /// (SEGUNDA PASADA, VISTO JUGANDO) El primer tinte era neutro-frío
        /// (0.845, 0.815, 0.865) y el taller seguía leyéndose GRIS LAVANDA:
        /// la piedra ocupa el 70% del cuadro y su color base tira a ciruela,
        /// así que un tinte neutro conserva esa frialdad y la penumbra
        /// resultante es "sótano", no "cueva cálida". El tinte pasa a estar
        /// SESGADO EN TEMPERATURA (rojo casi intacto, azul bajado el doble):
        /// la piedra se vuelve parda, los halos naranjas del crisol se
        /// integran en vez de flotar sobre un fondo de otro tono, y el frío
        /// (agua, hielo, la lámpara del banco) DESTACA más por contraste,
        /// que es lo que se quería de esa familia de materiales.
        public static readonly Color TinteGlobal = new Color(0.930f, 0.845f, 0.775f, 1f);

        /// <summary>
        /// Mismo color que <see cref="BackgroundColor"/> pero como Color32 fijo
        /// (playtest 12): lo usa el borde Difuso de ComputeCellColor para
        /// oscurecer hacia el fondo con LerpByte (que trabaja en bytes) sin
        /// reconvertir el Color de punto flotante en cada celda.
        /// </summary>
        private static readonly Color32 BgColor32 = new Color32(0x1A, 0x14, 0x18, 0xFF);

        /// <summary>
        /// Tabla de seno de 256 entradas (índice = byte de fase 0..255, valor =
        /// sin(fase) escalado a ±127), construida UNA VEZ aquí como inicializador
        /// estático (playtest 12). La regla del encargo prohíbe Mathf.Sin en el
        /// bucle de render -- este es exactamente el patrón idiomático que pide:
        /// precalcular la curva una vez y solo indexarla por celda. La usan Pulso
        /// (fase -> brillo) y Vetas (fase de banda -> brillo).
        /// </summary>
        private static readonly short[] SineTable256 = BuildSineTable256();

        private static short[] BuildSineTable256()
        {
            var t = new short[256];
            for (int i = 0; i < 256; i++)
            {
                double rad = i * (Math.PI * 2.0 / 256.0);
                t[i] = (short)Math.Round(Math.Sin(rad) * 127.0);
            }
            return t;
        }

        private const int FullRefreshEveryFrames = 30;

        /// <summary>
        /// (playtest 20, "necesito mucho material para ver las formas") Ancho del
        /// recipiente MÁS ESTRECHO donde el jugador acumula materia -- LEÍDO de las
        /// constantes reales de <see cref="SimLevelBuilder"/> (regla 24 de
        /// CLAUDE.md: nunca copiar la medida a mano). Es un `const int` porque
        /// ambos operandos son `public const int` en SimLevelBuilder -- el
        /// compilador lo pliega en tiempo de compilación, pero sigue siendo una
        /// referencia simbólica al plano real: si el equipo del plano cambia
        /// <see cref="SimLevelBuilder.ChillTrayWidth"/> o el grosor de pared, este
        /// valor se actualiza solo, sin que nadie tenga que acordarse de venir
        /// aquí a corregir un literal. Usado solo para verificar por assert (ver
        /// Init) que el periodo máximo de Vetas/Celdas sigue dando >=3 repeticiones
        /// aquí -- el propio bandeja fría, no una cifra de comentario que se
        /// queda vieja (la regla 24 cita "46x6"; medido de nuevo esta ronda con
        /// las constantes reales tras el playtest 19 son 44x7, ver el aviso del
        /// encargo -- por eso se lee del código, no de la prosa).
        /// </summary>
        private const int RecipienteMasEstrechoAncho =
            SimLevelBuilder.ChillTrayInteriorX1 - SimLevelBuilder.ChillTrayInteriorX0 + 1;

        // =====================================================================
        // CÁMARA QUE SIGUE AL APRENDIZ (playtest 15)
        // =====================================================================
        // Hasta esta ronda FitMainCamera encajaba el mundo ENTERO en pantalla.
        // Con el taller a 768x288 (3x2 pantallas) eso dejaría cada celda a una
        // sexta parte de su tamaño en pantalla -- inservible. La cámara pasa a
        // encuadrar aproximadamente UNA pantalla (CellGrid.PantallaW x
        // PantallaH, el encuadre que el jugador lleva 14 rondas validando) y a
        // seguir al aprendiz con zona muerta + suavizado exponencial.

        /// <summary>
        /// Fracción del MEDIO ancho/alto de pantalla que el jugador puede
        /// recorrer desde el centro antes de que la cámara empiece a
        /// corregir (zona muerta). 0.30 = el aprendiz puede moverse en un
        /// rectángulo central del 60% de la pantalla sin que la cámara se
        /// entere; solo al salirse de ese rectángulo la cámara empieza a
        /// perseguirlo. Un juego de observar (falling-sand) se lee mejor con
        /// una cámara que NO reacciona a cada pixel de movimiento del
        /// aprendiz -- eso es exactamente lo que evita la zona muerta.
        /// </summary>
        private const float DeadZoneHalfFraction = 0.30f;

        /// <summary>
        /// Constante de suavizado exponencial (más alto = más rígido/menos
        /// inercia). Fórmula independiente del framerate:
        /// t = 1 - e^(-k*dt); pos = Lerp(pos, objetivo, t). A 30fps y 144fps
        /// converge a la MISMA posición en el MISMO tiempo real -- a
        /// diferencia de un Lerp con un factor fijo por frame (que depende
        /// del framerate y "tiembla" distinto según la máquina).
        /// </summary>
        private const float CameraFollowSharpness = 6f;

        /// <summary>
        /// Multiplicador del tamaño ortográfico base mientras se mantiene
        /// pulsado Tab (vista ampliada, ver Update()). x2.2 muestra sobre 2
        /// pantallas de ancho -- suficiente para orientarse en el taller de
        /// 3x2 sin llegar a encuadrar el mundo entero (que volvería a hacer
        /// cada celda diminuta, el problema original de esta ronda).
        /// </summary>
        private const float WideViewMultiplier = 2.475f; // (R109) La base bajó de 90 a 80 celdas: 2.475 x 80 = las MISMAS 198 celdas de plano que dio siempre Tab (antes 2.2 x 90). El gesto no cambia ni un pelo.
        private const float ZoomRuedaMinCerca = 0.8f; // (R111: "un tick más de cercanía por si alguno lo quiere") 64 celdas en el tope; el defecto sigue en 80.

        // -----------------------------------------------------------------
        // ZOOM CON LA RUEDA (playtest 29, pedido de Cesar: "zoom out con el
        // scroll un poco, y tener la posición actual como máxima cercanía").
        // La posición ACTUAL (factor 1) es el tope de cercanía -- nunca se
        // acerca más que hoy; la rueda solo ALEJA, hasta el mismo techo que
        // la vista ampliada de Tab (comparten multiplicador: un solo "lejos"
        // en todo el juego, no dos). Coste: cero de verdad -- Tab ya probó
        // que todo el camino de render (refresco viewport-aware, rótulos,
        // alcance del frasco) se adapta al orthographicSize cambiante.
        // -----------------------------------------------------------------
        private const float ZoomRuedaPaso = 0.065f;  // (R111, Cesar: "el zoom es muy sensible... incapaz de volver al default") una muesca = ~0.16 de factor: ~9 muescas cubren el alejar, 1-2 el acercar. El 0.28 del playtest 29 saltaba 0.69 por muesca -- imposible aterrizar.
        private float _zoomRueda = 1f;               // 1 = máxima cercanía (la vista de siempre).

        /// <summary>Colchón de chunks fuera del rectángulo visible que el barrido de render sigue considerando "cerca de la vista" (ver RenderFrame). En chunks, no celdas: 2 = 32 celdas de margen a cada lado.</summary>
        private const int ViewMarginChunks = 2;

        /// <summary>
        /// (playtest 21, EL PIVOT) Acerca la cámara respecto al encuadre de
        /// "una pantalla" de siempre -- pedido por el contrato del cuarto
        /// íntimo (768x288 con TODO el mundo enterrado en piedra salvo una
        /// cámara excavada, ver `Sim/SimLevelBuilder.BuildCuartoIntimo`): una
        /// sala pequeña se lee mejor con la cámara más cerca, y el zoom vive
        /// en UNA sola multiplicación sobre `_baseOrthoSize` (ver
        /// <see cref="FitMainCamera"/>) para que quede expuesto como un
        /// número con nombre, no un literal suelto en medio de la fórmula.
        ///
        /// TERCERA RONDA -- CESAR LO VIO CORRER EN SU UNITY: "dos tercios de
        /// la pantalla son roca". El valor anterior (0.7) mostraba
        /// 144*0.7=100.8 celdas de alto para una sala interior de 42 --
        /// menos de la mitad del encuadre era la sala, el resto piedra
        /// maciza sin nada, y la criatura un puntito. Baja a **5/16 =
        /// 0.3125** -- con `CellGrid.PantallaH`=144 celdas de referencia,
        /// eso son EXACTAMENTE 144*5/16 = **45 celdas de alto** visibles
        /// (dentro del 45-55 pedido; se eligió el extremo BAJO del rango a
        /// propósito, ver el porqué dos párrafos más abajo). El ancho sigue
        /// la misma proporción 16:9 que el resto del archivo (256*5/16=80
        /// celdas), no se toca ninguna otra fórmula.
        ///
        /// POR QUÉ NO BAJA A CERO LA ROCA VISIBLE (documentado en vez de
        /// fingido): <see cref="UpdateCameraFollow"/> usa una cámara de
        /// SEGUIMIENTO con zona muerta del 30% (<see cref="DeadZoneHalfFraction"/>),
        /// y en el primer frame (snap=true) eso ancla al aprendiz al 65% de
        /// la altura del encuadre (más margen por DEBAJO que por encima --
        /// mismo comportamiento en TODA cámara de seguimiento del juego, no
        /// algo nuevo de esta ronda). El aprendiz nace a solo 12 celdas del
        /// suelo real de la sala (`SimLevelBuilder.AprendizY` = `CunaTopY`+1
        /// = 180, suelo en `CuartoY0`=168) porque tiene que seguir leyéndose
        /// "junto a la criatura", así que ese 65%-por-debajo consume parte
        /// de esa franja en piedra de verdad, sin excavar. Calculado exacto
        /// (réplica en Python de esta misma aritmética, ver el informe de la
        /// ronda) desde el punto de aparición real: de las 45 celdas de
        /// alto, 16.75 son roca por DEBAJO del suelo de la sala y 16.25 son
        /// sala excavada VACÍA por encima del grupo cuna/repisa -- las
        /// ~12 restantes son el propio grupo cuna/charco/repisa/criatura.
        /// Bajar más el zoom (fuera del 45-55 pedido) reduciría esa franja
        /// de roca en términos absolutos pero también recortaría la sala
        /// entera -- el extremo bajo del rango (45, no 50 ni 55) es
        /// precisamente el que MINIMIZA la roca visible sin salirse de lo
        /// pedido (ver la tabla de la ronda: a zoom más alto la franja de
        /// roca crece en proporción con `_baseOrthoSize`). TODO lo demás que
        /// depende de `_baseOrthoSize` (zona muerta, acotado al mundo,
        /// rótulos de UiStyles, alcance del Frasco -- que no lee tamaño de
        /// cámara en absoluto) se deriva de él y se adapta solo, sin tocar
        /// nada más en este archivo.
        /// </summary>
        /// (playtest 21, TERCERA CALIBRACIÓN — la definitiva la dio Cesar mirándolo)
        /// Cronología, porque este número ya ha girado dos veces y conviene que no
        /// gire una tercera a ciegas:
        ///  · 0.7 (100 celdas de alto): el director lo vio correr y diagnosticó
        ///    "dos tercios de la pantalla son roca". El diagnóstico era correcto
        ///    PERO la causa que le atribuyó no: no sobraba zoom, sobraba roca
        ///    DEBAJO — la sala quedaba en la franja de arriba y el hueco estaba
        ///    todo en el mismo lado. Era un problema de ENCUADRE, no de distancia.
        ///  · 5/16 = 0.3125 (45 celdas): la sobrecorrección. Cesar, al verlo:
        ///    *"la camara me quedo super zoomeada y se ve horrible, necesito como
        ///    el doble de distancia al menos"*.
        ///  · 5/8 = 0.625 (90 celdas): exactamente el doble que pidió, y muy cerca
        ///    del 0.7 original — porque el original nunca fue el problema.
        /// LA LECCIÓN, para quien toque esto: si media pantalla es roca, mira
        /// primero DÓNDE está el hueco antes de tocar el zoom. Aquí la roca se
        /// reparte ahora arriba y abajo (ver el anclaje vertical más abajo) y a
        /// esta distancia la cámara lee como "una cámara excavada en la montaña",
        /// que es justo lo que el pivot quiere.
        private const float CuartoIntimoZoomFactor = 5f / 9f; // (R109) 80 celdas de alto: Cesar jugó la R108 pegado al tope de la rueda ("ahí apenas alcancé a sentir que yo era el personaje") y lo que se juega siempre es el defecto. Antes: 5/8 = 90 celdas (playtest 21).

        private Camera _mainCam;
        private ApprenticeController _apprentice;
        private float _baseOrthoSize;

        private Universe _universe;
        private CellGrid _grid;

        private Texture2D _texture;
        private Color32[] _pixels;
        // (reingeniería del espacio, playtest 4) Con la grilla 256x144, CHUNK=16
        // divide EXACTAMENTE los dos ejes (16x9 chunks): todos los chunks miden
        // 16x16 y basta UN único buffer scratch preasignado. Antes (384x216) la
        // última fila de chunks medía 16x8 y había que mantener dos buffers y
        // elegir uno por chunk en el hot-path de render.
        private Color32[] _chunkScratch;   // CHUNK*CHUNK
        // (playtest 12) Un chunk por su índice: true si CUALQUIER celda de ese
        // chunk lleva un patrón que se recalcula del `tick` en vivo (Vetas,
        // Celdas, Pulso -- no usan CellGrid.morph) con ritmoAnim>0. Lo escribe
        // RenderChunk cada vez que efectivamente redibuja el chunk; lo lee
        // RenderFrame para decidir si vale la pena saltárselo aunque esté
        // dormido. Ver el porqué completo en RenderFrame.
        private bool[] _chunkContinuousAnim;
        // =====================================================================
        // CHUNKS SUCIOS FUERA DE CÁMARA (playtest 15)
        // =====================================================================
        // Con el barrido de RenderFrame acotado al rango de chunks que toca la
        // vista (ver RenderFrame), un chunk que cambia MIENTRAS está fuera de
        // ese rango nunca se visita -- si además le da tiempo a dormirse otra
        // vez (SleepTicks=30) antes de que la cámara llegue, entraría en vista
        // con "awake=false" y pintado con colores VIEJOS para siempre (nadie
        // volvería a redibujarlo). Mecanismo: CellGrid.chunkTouchedTick YA se
        // actualiza en CADA WakeChunk para TODOS los chunks, estén o no en
        // cámara (lo necesita el propio stepper) -- así que basta con que este
        // renderer recuerde, por chunk, en qué "touchedTick" lo pintó por
        // última vez. Si el valor actual de CellGrid difiere del recordado (o
        // el chunk no se ha pintado nunca), el chunk está sucio y se fuerza su
        // redibujado la PRIMERA vez que el barrido vuelve a visitarlo -- sin
        // coste alguno mientras permanece fuera de rango (no se evalúa hasta
        // que entra). Ver el chequeo completo en RenderFrame.
        private uint[] _chunkLastRenderTick;
        private bool[] _chunkEverRendered;

        /// <summary>(R124) La piel de roca (Game/PielDeRoca) pide que la sim NO pinte Stone. Cambiarlo obliga a <see cref="MarcarTodoSucio"/>.</summary>
        public static bool OcultarRoca;

        /// <summary>(R124) Fuerza el repintado de todos los chunks en el próximo RenderFrame (mismo camino que "sucio fuera de cámara").</summary>
        public void MarcarTodoSucio()
        {
            if (_chunkEverRendered == null) return;
            for (int i = 0; i < _chunkEverRendered.Length; i++) _chunkEverRendered[i] = false;
        }

        /// <summary>(R124) El tinte que lleva el quad de la sim ahora mismo (TinteGlobal → TintePlano en la mudanza): la piel de roca lo copia para no desentonar.</summary>
        public Color TinteActual => _quadSr != null ? _quadSr.color : TinteGlobal;
        private Transform _quad;
        private SpriteRenderer _quadSr; // (R98, direccion Opus) guardado para el tinte de la VISTA DE PLANO.

        // =================================================================
        // (R98, modo mudanza — dirección Opus) LA VISTA DE PLANO: mientras
        // la mudanza está activa, el QUAD de la sim (solo el sustrato: la
        // roca y la materia, sortingOrder -5) se enfría hacia TintePlano —
        // el multiply no puede desaturar, pero SÍ comprime el eje
        // cálido-frío (dispersión entre canales 0.155 → 0.070) y baja la
        // luminancia -23%: con la piedra ocupando ~70% del cuadro, se lee
        // pizarra. Las máquinas, halos y el aprendiz (órdenes 14..60) quedan
        // A TODO COLOR: el "plano" nace del contraste, no de un lavado. La
        // viñeta IMGUI (encima de todo) conserva su autoridad gratis.
        // TinteGlobal ya lo invitaba: "si algún día hace falta un
        // amanecer/anochecer, este es el único punto que hay que animar".
        // =================================================================
        public static float TinteMudanza; // 0..1, lo escribe Game/Mudanza con su EstadoT suavizado.
        private static readonly Color TintePlano = new Color(0.630f, 0.640f, 0.700f, 1f); // el sesgo cálido de TinteGlobal, invertido exacto.
        private float _tinteAplicado = -1f;
        // =============================================================
        // (RONDA 66-68) EL LABIO FRONTAL: APAGADO (regla 15 de CLAUDE.md).
        // Vivió UNA ronda: una segunda textura que oscurecía el anillo de
        // aire pegado a la roca, dibujada por delante del aprendiz para
        // vender profundidad. Cesar lo tumbó con captura en el playtest 67:
        // "los bordes pintan tiles oscuros... entran mucho en el personaje y
        // con cosas detrás se van a ver claramente" -- a 1 téxel/celda el
        // anillo es inevitablemente BLOCKY y se lee como suciedad, no como
        // profundidad. El volumen queda a cargo del sombreado de masa
        // (interior/esquinas, que sí gustó) y de las capas de decoración
        // futuras. La infraestructura se conserva tras esta bandera por si
        // una versión suavizada (medio téxel, sprites de borde reales)
        // vuelve a intentarse -- NO reactivar sin arte de borde de verdad.
        // =============================================================
        private static readonly bool LabioFrontalActivo = false; // static readonly (no const): evita el CS0162 de "código inalcanzable" en las ramas guardadas.
        private Texture2D _frontTexture;
        private Color32[] _frontScratch;
        // =============================================================
        // (R129) EL VELO DE LÍQUIDOS: tercera textura, siempre activa.
        // Los LÍQUIDOS se repintan por delante del aprendiz (orden 52:
        // sobre Personaje=50, bajo ArquitecturaFrente=55 y CarryEnMano=60)
        // con el MISMO color que ya calculó ComputeCellColor pero alfa
        // parcial: el muñeco metido en la poza queda TEÑIDO por el agua
        // en vez de flotar nítido delante de ella (observación de Cesar,
        // playtest 128: "personaje nítido delante del agua"). A diferencia
        // del labio frontal (tumbado en R67 por blocky), aquí no se
        // inventa ningún borde: es la misma agua de la sim, dos veces —
        // el pixel cuadrado del líquido delante es idéntico al de atrás,
        // así que no puede "cantar" más que la propia sim. Se rellena en
        // el mismo barrido por chunks (RenderChunk) y sube a GPU al mismo
        // ritmo que la textura principal.
        // =============================================================
        private Texture2D _veloTexture;
        private Color32[] _veloScratch;
        private SpriteRenderer _veloSr;
        /// <summary>Alfa del líquido delantero. 115/255 ≈ 45%: tiñe sin borrar al personaje. Subirlo esconde al muñeco; bajarlo vuelve al "nítido delante del agua".</summary>
        private const byte VeloAlfa = 115;

        private int _frameCounter;

        public Texture2D Texture => _texture;

        public void Init(Universe universe, CellGrid grid)
        {
            _universe = universe;
            _grid = grid;

            _texture = new Texture2D(CellGrid.W, CellGrid.H, TextureFormat.RGBA32, false, false)
            {
                // (fix playtest 6: baja resolución) La textura del sim es 1 téxel por
                // celda (256x144) estirada a toda la pantalla: con la cámara actual eso
                // son varios píxeles de pantalla por celda. Bilinear sobre la piedra
                // (casi plana) producía exactamente el "borroso" que reportó el
                // jugador, en contraste con los sprites de maquinaria (Point). Ahora que
                // ComputeCellColor mete detalle real por celda (sillería + canto +
                // grano, ver más abajo) Point da una imagen coherente y nítida en todo
                // el juego: la celda cuadrada se lee como pixel-art deliberado, no como
                // blur. Para revertir (p.ej. si algún día se sube la resolución de la
                // sim), basta volver a poner FilterMode.Bilinear aquí.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "AlkahestSimTexture",
            };

            _pixels = new Color32[CellGrid.W * CellGrid.H];
            _chunkScratch = new Color32[CellGrid.CHUNK * CellGrid.CHUNK];

            // (RONDA 66, dirección 2.5D) EL LABIO FRONTAL DE LA ROCA: segunda
            // textura del mismo tamaño, dibujada POR DELANTE del aprendiz
            // (ver BuildQuad). Solo pinta el anillo de aire pegado a la roca
            // madre / piso estructural -- la "cara" de la masa más cercana a
            // cámara. Volar pegado al muro = el labio te tapa un poco =
            // profundidad, sin tocar una sola regla de la sim (la colisión
            // sigue viniendo de la grilla, mandato de Cesar). Se rellena en el
            // MISMO barrido por chunks que la textura principal: mismo coste
            // de viewport, una subida extra por chunk sucio.
            if (LabioFrontalActivo)
            {
                _frontTexture = new Texture2D(CellGrid.W, CellGrid.H, TextureFormat.RGBA32, false, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "AlkahestRocaFrenteTexture",
                };
                _frontScratch = new Color32[CellGrid.CHUNK * CellGrid.CHUNK];
            }
            // (R129) El velo de líquidos: ver el docblock junto a _veloTexture.
            _veloTexture = new Texture2D(CellGrid.W, CellGrid.H, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "AlkahestVeloLiquidoTexture",
            };
            _veloScratch = new Color32[CellGrid.CHUNK * CellGrid.CHUNK];
            _chunkContinuousAnim = new bool[CellGrid.ChunksX * CellGrid.ChunksY];
            _chunkLastRenderTick = new uint[CellGrid.ChunksX * CellGrid.ChunksY];
            // (playtest 15) `_chunkEverRendered` arranca todo en false por
            // defecto -- exactamente la semántica que hace falta ("nunca se
            // pintó, fuerza el primer redibujado sin importar el tick"), sin
            // necesidad de un valor centinela para el array de ticks.
            _chunkEverRendered = new bool[CellGrid.ChunksX * CellGrid.ChunksY];

            // Guardia explícita: todo el render por chunks asume que CHUNK divide
            // los dos ejes. Si alguien vuelve a cambiar CellGrid.W/H a un tamaño
            // que no sea múltiplo de 16, que salte aquí y no en un SetPixels32
            // desalineado imposible de depurar.
#pragma warning disable 0162 // guard intencional sobre constantes (CS0162 si W/H son múltiplos exactos)
            if ((CellGrid.W % CellGrid.CHUNK) != 0 || (CellGrid.H % CellGrid.CHUNK) != 0)
            {
                Debug.LogError($"[TenThousandYears] CellGrid {CellGrid.W}x{CellGrid.H} no es múltiplo de CHUNK={CellGrid.CHUNK}: " +
                               "SimRenderer necesita chunks completos (ver el buffer scratch único).");
            }
#pragma warning restore 0162

            // (playtest 20) Guardia de la regla 24: el periodo MÁXIMO de Vetas/
            // Celdas (ver PatronPeriodoCeldas, compartido por las dos) debe seguir
            // cabiendo >=3 veces en el recipiente más estrecho del taller,
            // LEÍDO arriba de SimLevelBuilder -- si el plano vuelve a estrechar la
            // bandeja fría (o alguien sube el techo de patronEscala) sin que nadie
            // revise esto, que salte aquí y no en un reporte de "no se ve el
            // patrón" tres rondas después.
            int peorPeriodo = PatronPeriodoCeldas(8); // patronEscala tope, ver Universe.Create.
            if (RecipienteMasEstrechoAncho / peorPeriodo < 3)
            {
                Debug.LogError($"[TenThousandYears] El recipiente más estrecho ({RecipienteMasEstrechoAncho} celdas) ya no cabe " +
                                $"3 repeticiones del periodo máximo de Vetas/Celdas ({peorPeriodo}): revisar PatronPeriodoCeldas " +
                                "o la medida real de SimLevelBuilder (regla 24 de CLAUDE.md).");
            }

            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = default;
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);
            if (_frontTexture != null)
            {
                _frontTexture.SetPixels32(_pixels); // mismo buffer limpio: arranca transparente.
                _frontTexture.Apply(false);
            }
            _veloTexture.SetPixels32(_pixels); // mismo buffer limpio: arranca transparente.
            _veloTexture.Apply(false);

            BuildQuad();

            // (playtest 15) El aprendiz puede no existir todavía en este punto
            // (AlkahestGameBootstrap.Start() y AlkahestSim.Start() viven en
            // objetos distintos, sin orden garantizado -- ver el comentario de
            // AlkahestGameBootstrap). UpdateCameraFollow ya sabe caer al centro
            // del mundo si _apprentice sigue null; Update() reintentará la
            // búsqueda cada frame SOLO mientras siga sin encontrarlo (ver el
            // comentario de Update()), así que esto se autocorrige sin más
            // intervención en cuanto el aprendiz aparece.
            _mainCam = Camera.main;
            FitMainCamera();
            UpdateCameraFollow(snap: true);
            RenderFrame(0, true);
        }

        /// <summary>
        /// (playtest 15, reemplaza el fix de playtest 5) Antes esto encajaba el
        /// mundo ENTERO en pantalla -- correcto mientras el mundo medía una
        /// pantalla, inservible con el taller a 768x288 (cada celda a 1/6 de su
        /// tamaño). Ahora solo calcula el TAMAÑO base: una pantalla
        /// (CellGrid.PantallaW x PantallaH, NUNCA hardcodeado), encajando la
        /// dimensión limitante exactamente igual que el fix de playtest 6 (ese
        /// arreglo costó dos playtests -- se conserva intacto, solo cambia qué
        /// medidas de mundo alimentan la fórmula: pantalla en vez de mundo
        /// entero). La POSICIÓN de la cámara ya no se toca aquí -- la fija
        /// UpdateCameraFollow cada frame seguido al aprendiz.
        /// </summary>
        private void FitMainCamera()
        {
            var cam = _mainCam;
            if (cam == null) return;
            float pantallaW = CellGrid.PantallaW * CellWorldSize;
            float pantallaH = CellGrid.PantallaH * CellWorldSize;
            cam.orthographic = true;
            // (fix playtest 6, intacto) Si el viewport es más ESTRECHO que el
            // aspecto de la pantalla objetivo, encajar solo la altura RECORTA
            // los lados. Se encaja la dimensión limitante: sobra arriba/abajo
            // antes que cortar.
            float aspect = cam.aspect > 0.01f ? cam.aspect : 16f / 9f;
            float sizeForHeight = pantallaH * 0.5f;
            float sizeForWidth = (pantallaW * 0.5f) / aspect;
            // (playtest 21) La única línea del zoom del cuarto íntimo -- ver
            // CuartoIntimoZoomFactor. Zona muerta, acotado al mundo, rótulos
            // y alcance del Frasco leen `_baseOrthoSize`/`orthographicSize`
            // ya multiplicados, así que se adaptan solos sin tocar nada más.
            _baseOrthoSize = Mathf.Max(sizeForHeight, sizeForWidth) * CuartoIntimoZoomFactor;
            cam.backgroundColor = BackgroundColor;
        }

        /// <summary>
        /// Cámara con seguimiento (playtest 15): zona muerta + suavizado
        /// exponencial + vista ampliada opcional (Tab) + acotado a los bordes
        /// del mundo. Se llama cada frame visual desde Update() (independiente
        /// del tick de simulación, para que el suavizado no dependa de a qué
        /// framerate cae el acumulador de AlkahestSim -- ver su comentario) y
        /// una vez con snap=true desde Init().
        /// </summary>
        /// <param name="snap">true = coloca la cámara EXACTAMENTE en el
        /// objetivo sin suavizar (arranque, o el primer frame en que el
        /// aprendiz aparece tras no haberse encontrado -- evitar un paneo
        /// lento desde el centro del mundo hasta el aprendiz al empezar la
        /// partida).</param>
        private void UpdateCameraFollow(bool snap)
        {
            if (_mainCam == null) return;

            // (playtest 15) Búsqueda perezosa: el orden Start() entre
            // AlkahestGameBootstrap (crea al aprendiz) y AlkahestSim (crea
            // este SimRenderer) NO está garantizado. Se reintenta cada frame
            // SOLO mientras siga sin encontrarlo -- en cuanto aparece, se
            // cachea para siempre y esta rama deja de ejecutarse (nunca busca
            // por frame en el caso normal, que es el 100% de la partida salvo
            // el primer frame o dos).
            // (playtest 28, POC multiplayer) Con cuatro aprendices en el
            // taller, "el primero que encuentre" ya no sirve: la cámara tiene
            // que seguir al MÍO. `ApprenticeController.AprendizLocal` lo fija
            // Net/AprendizNet.cs en el avatar del dueño y es null en la escena
            // Lab clásica -- ahí esta rama no hace nada y la búsqueda perezosa
            // de siempre (justo debajo) se comporta EXACTAMENTE igual que
            // antes. La comparación por frame son dos chequeos de nulidad.
            bool aprendizNuevo = false;
            var aprendizLocal = ApprenticeController.AprendizLocal;
            if (aprendizLocal != null)
            {
                if (_apprentice != aprendizLocal)
                {
                    _apprentice = aprendizLocal;
                    aprendizNuevo = true;
                }
            }
            else if (_apprentice == null)
            {
                _apprentice = FindAnyObjectByType<ApprenticeController>();
                aprendizNuevo = _apprentice != null;
            }
            bool snapAhora = snap || aprendizNuevo;

            // VISTA AMPLIADA (Tab, mantener pulsado): atajo del MUNDO -- debe
            // respetar la regla 12 de CLAUDE.md (UiStyles.EscribiendoTexto +
            // JournalHud.Abierto). Comprobado libre en docs/HANDOFF.md sección
            // "Playtest 10" (tabla de atajos: no aparece ninguna T-A-B). Solo
            // ORIENTA -- no cambia gameplay ni desbloquea nada -- así que basta
            // con las dos guardas estándar, sin InputLocked (no es una acción
            // de juego, es un "alejar la cámara para mirar el plano").
            var kb = Keyboard.current;
            bool wide = kb != null && kb.tabKey.isPressed
                        && !UiStyles.EscribiendoTexto && !JournalHud.Abierto;

            // ZOOM CON LA RUEDA (ver el bloque de constantes): mismas guardas
            // que Tab (escribiendo texto / diario abierto no tocan cámara).
            // Rueda abajo = alejar, rueda arriba = acercar, clampeado entre
            // la vista de siempre (1) y el techo compartido con Tab.
            var raton = UnityEngine.InputSystem.Mouse.current;
            if (raton != null && !UiStyles.EscribiendoTexto && !JournalHud.Abierto)
            {
                // (ajuste playtest 29, "tengo que mover mucho la ruedita")
                // El Input System reporta la rueda en DOS escalas según
                // dispositivo/plataforma: ±120 por muesca (Windows clásico) o
                // ±1 (editor y muchos ratones). La normalización fija /120
                // convertía la escala ±1 en pasos microscópicos -- el "va muy
                // lento" de Cesar. Adaptativa: si llega grande se divide, si
                // llega pequeña se usa tal cual.
                float crudo = raton.scroll.ReadValue().y;
                float muescas = Mathf.Abs(crudo) >= 100f ? crudo / 120f : crudo;
                if (muescas != 0f)
                {
                    // (R108) La rueda ahora también ACERCA. (R109) El defecto
                    // pasó a 80 celdas. (R111) EL RETÉN DEL DEFECTO: si el paso
                    // CRUZA el 1.0, la rueda se detiene EXACTO en la vista por
                    // defecto — un clic más y sigue. Volver a casa deja de ser
                    // puntería: es soltar la rueda donde ella misma frena.
                    float previo = _zoomRueda;
                    float nuevo = Mathf.Clamp(previo - muescas * ZoomRuedaPaso * WideViewMultiplier,
                        ZoomRuedaMinCerca, WideViewMultiplier);
                    if ((previo - 1f) * (nuevo - 1f) < 0f) nuevo = 1f;
                    _zoomRueda = nuevo;
                }
            }

            // Tab manda mientras se mantiene (es el gesto de "ver el plano
            // entero"); al soltarlo se vuelve al zoom de rueda del jugador.
            float factorZoom = wide ? WideViewMultiplier : _zoomRueda;
            float targetSize = _baseOrthoSize * factorZoom;

            float dt = Time.deltaTime;
            float t = snapAhora ? 1f : (1f - Mathf.Exp(-CameraFollowSharpness * dt));

            // El tamaño se suaviza con la MISMA curva que la posición: entrar y
            // salir de la vista ampliada de golpe sería un latigazo tan brusco
            // como una cámara que salta de posición.
            _mainCam.orthographicSize = Mathf.Lerp(_mainCam.orthographicSize, targetSize, t);

            float orthoH = _mainCam.orthographicSize;
            float aspect = _mainCam.aspect > 0.01f ? _mainCam.aspect : 16f / 9f;
            float orthoW = orthoH * aspect;

            Vector3 playerPos = _apprentice != null
                ? _apprentice.transform.position
                : new Vector3(CellGrid.W * CellWorldSize * 0.5f, CellGrid.H * CellWorldSize * 0.5f, 0f);

            // (RONDA 73) FOCO CINEMATOGRÁFICO: mientras esté fijado (director
            // del prólogo: derrumbe, nacimiento del depósito), la cámara mira
            // AHÍ en vez de al aprendiz, con el MISMO suavizado exponencial de
            // siempre (el traveling de ida y de vuelta salen gratis del Lerp).
            // La zona muerta se salta a propósito: un plano cinematográfico
            // centra su sujeto, no lo deja vagar por el 60% del encuadre.
            bool cine = FocoCinematico.HasValue;
            if (cine) playerPos = FocoCinematico.Value;

            Vector3 camPos = _mainCam.transform.position;

            // ZONA MUERTA: el aprendiz se mueve libre dentro del rectángulo
            // central sin que la cámara reaccione; solo al cruzar el borde la
            // cámara se desplaza lo justo para devolver al aprendiz al borde
            // de la zona (nunca lo "recentra" de golpe -- eso sería el propio
            // salto brusco que la zona muerta existe para evitar).
            float halfDeadW = orthoW * DeadZoneHalfFraction;
            float halfDeadH = orthoH * DeadZoneHalfFraction;
            float dx = playerPos.x - camPos.x;
            float dy = playerPos.y - camPos.y;
            float targetX = camPos.x;
            float targetY = camPos.y;
            if (cine) { targetX = playerPos.x; targetY = playerPos.y; } // sin zona muerta: el plano centra su sujeto.
            else
            {
                if (dx > halfDeadW) targetX = playerPos.x - halfDeadW;
                else if (dx < -halfDeadW) targetX = playerPos.x + halfDeadW;
                if (dy > halfDeadH) targetY = playerPos.y - halfDeadH;
                else if (dy < -halfDeadH) targetY = playerPos.y + halfDeadH;
            }

            float newX = Mathf.Lerp(camPos.x, targetX, t);
            float newY = Mathf.Lerp(camPos.y, targetY, t);

            // ACOTADO AL MUNDO: la cámara nunca debe enseñar fuera de los
            // bordes -- salvo que el propio eje del mundo sea más ESTRECHO que
            // el viewport (el mundo mide 768x288, no siempre 16:9 exacto según
            // aspect/vista ampliada), en cuyo caso no hay "dentro" posible y se
            // centra en ese eje en vez de forzar un clamp que oscilaría.
            float worldW = CellGrid.W * CellWorldSize;
            float worldH = CellGrid.H * CellWorldSize;
            float clampedX = worldW <= orthoW * 2f ? worldW * 0.5f : Mathf.Clamp(newX, orthoW, worldW - orthoW);
            float clampedY = worldH <= orthoH * 2f ? worldH * 0.5f : Mathf.Clamp(newY, orthoH, worldH - orthoH);

            // (RONDA 73) LA SACUDIDA (derrumbe del prólogo): ruido Perlin en
            // ambos ejes — continuo y sin RNG — con amplitud que decae al
            // cuadrado del tiempo restante. Se aplica DESPUÉS del clamp: es un
            // temblor de encuadre, no un desplazamiento real de la cámara (a
            // amplitudes de ~0.2 unidades enseñar 2 celdas de fuera del mundo
            // durante 3 frames es invisible; recortarlo sí se notaría como un
            // temblor "aplastado" contra el borde).
            if (Sacudida > 0f)
            {
                Sacudida = Mathf.Max(0f, Sacudida - dt);
                float f = Sacudida / SacudidaDuracion; // 1 -> 0.
                float amp = SacudidaAmplitud * f * f;
                float tt = Time.time * 23f;
                clampedX += (Mathf.PerlinNoise(tt, 0.37f) - 0.5f) * 2f * amp;
                clampedY += (Mathf.PerlinNoise(0.83f, tt) - 0.5f) * 2f * amp * 0.7f;
            }

            _mainCam.transform.position = new Vector3(clampedX, clampedY, -10f);
        }

        // (RONDA 73, el prólogo rehecho) Controles cinematográficos que el
        // FundacionDirector usa para el derrumbe y el nacimiento del depósito.
        // Estáticos y auto-limpiables: quien fija el foco es responsable de
        // volverlo null (el director lo hace en sus transiciones y en
        // OnDestroy); la sacudida se apaga sola al llegar a 0.
        /// <summary>Mientras tenga valor, la cámara encuadra ESTE punto del mundo en vez de al aprendiz (mismo suavizado, sin zona muerta).</summary>
        public static Vector3? FocoCinematico;
        /// <summary>Segundos restantes de temblor de cámara. Fijar a <see cref="SacudidaDuracion"/> para un derrumbe completo.</summary>
        public static float Sacudida;
        public const float SacudidaDuracion = 1.6f;
        private const float SacudidaAmplitud = 0.22f; // ~2 celdas de mundo en el pico.

        /// <summary>
        /// Update() de Unity, INDEPENDIENTE del tick de simulación (playtest
        /// 15): AlkahestSim solo llama a RenderFrame cuando el acumulador de
        /// Time.deltaTime completa un paso de 30Hz, así que a framerates altos
        /// se saltan frames de render -- moviendo la cámara solo ahí se vería
        /// a tirones. Aquí se actualiza SIEMPRE, cada frame visual, igual que
        /// se movería cualquier cámara de seguimiento en un juego que no fuera
        /// de simulación a tick fijo.
        /// </summary>
        private void Update()
        {
            // (R98) LA VISTA DE PLANO: solo asigna cuando el valor cambia —
            // un mundo quieto fuera de la mudanza cuesta exactamente cero.
            if (_quadSr != null && !Mathf.Approximately(_tinteAplicado, TinteMudanza))
            {
                _quadSr.color = Color.Lerp(TinteGlobal, TintePlano, TinteMudanza);
                if (_veloSr != null) _veloSr.color = _quadSr.color; // (R129) el velo acompaña al sustrato.
                _tinteAplicado = TinteMudanza;
            }

            if (_texture == null) return; // Init() todavía no ha corrido.
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            float aspectNow = _mainCam.aspect;
            if (!Mathf.Approximately(aspectNow, _lastAspect))
            {
                _lastAspect = aspectNow;
                FitMainCamera();
            }
            UpdateCameraFollow(snap: false);
        }

        private void BuildQuad()
        {
            // (fix build) Antes esto era un Quad con Shader.Find("URP/Unlit"): Unity ELIMINA
            // ese shader de las builds si ningún asset lo referencia, y toda la materia era
            // invisible en el .exe (en editor sí existía). Un SpriteRenderer usa el shader de
            // sprites por defecto, que sí se incluye (las barras de las máquinas lo demuestran).
            var go = new GameObject("AlkahestSimSprite");
            var sr = go.AddComponent<SpriteRenderer>();
            float ppu = 1f / CellWorldSize; // 10 px por unidad -> celda = 0.1 unidades.
            sr.sprite = Sprite.Create(_texture, new Rect(0, 0, CellGrid.W, CellGrid.H),
                Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = -5;
            sr.color = TinteGlobal; // (playtest 31) ver TinteGlobal: la cueva se oscurece AQUÍ, no celda a celda.
            _quadSr = sr; // (R98) la vista de plano anima este color en Update (solo cuando cambia).

            _quad = go.transform;
            _quad.SetParent(transform, false);
            _quad.position = Vector3.zero; // pivot en (0,0): el sprite cubre 25.6 x 14.4 exacto (256x144 celdas de 0.1).

            // (RONDA 66-68) El sprite del labio frontal solo nace con la
            // bandera encendida -- ver el docblock de LabioFrontalActivo.
            if (LabioFrontalActivo)
            {
                var goFrente = new GameObject("AlkahestRocaFrenteSprite");
                var srFrente = goFrente.AddComponent<SpriteRenderer>();
                srFrente.sprite = Sprite.Create(_frontTexture, new Rect(0, 0, CellGrid.W, CellGrid.H),
                    Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
                srFrente.sortingOrder = 55; // = Game/Capas.ArquitecturaFrente (literal a propósito: Sim/ no depende de Game/).
                goFrente.transform.SetParent(transform, false);
                goFrente.transform.position = Vector3.zero;
            }

            // (R129) El sprite del velo de líquidos — ver el docblock de
            // _veloTexture. Mismo tinte que el quad principal (lo anima
            // Update junto a _quadSr: el agua delantera también se enfría
            // en la vista de plano de la mudanza).
            var goVelo = new GameObject("AlkahestVeloLiquidoSprite");
            _veloSr = goVelo.AddComponent<SpriteRenderer>();
            _veloSr.sprite = Sprite.Create(_veloTexture, new Rect(0, 0, CellGrid.W, CellGrid.H),
                Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
            _veloSr.sortingOrder = 52; // entre Personaje (50) y ArquitecturaFrente (55) — literal a propósito: Sim/ no depende de Game/.
            _veloSr.color = TinteGlobal;
            goVelo.transform.SetParent(transform, false);
            goVelo.transform.position = Vector3.zero;
        }

        /// <summary>
        /// Rango de chunks [cx0,cx1) x [cy0,cy1) que la cámara actual puede
        /// ver, más <see cref="ViewMarginChunks"/> chunks de colchón a cada
        /// lado (para que un chunk ya esté fresco al llegar el jugador, en
        /// vez de "popear" justo en el borde de pantalla). Clampado a los
        /// límites reales de la grilla. Si no hay cámara (herramientas de
        /// Editor sin escena de juego), cae al rango completo -- mismo
        /// espíritu que el fallback de UpdateCameraFollow al centro del
        /// mundo: nunca dejar el render roto por falta de cámara.
        /// </summary>
        private void ComputeVisibleChunkRange(out int cx0, out int cy0, out int cx1, out int cy1)
        {
            if (_mainCam == null)
            {
                cx0 = 0; cy0 = 0; cx1 = CellGrid.ChunksX; cy1 = CellGrid.ChunksY;
                return;
            }

            float halfH = _mainCam.orthographicSize;
            float halfW = halfH * (_mainCam.aspect > 0.01f ? _mainCam.aspect : 16f / 9f);
            Vector3 p = _mainCam.transform.position;

            // Clampar en CELDAS antes de dividir por CHUNK: con el mundo más
            // estrecho que el viewport en algún eje (cámara centrada, ver
            // UpdateCameraFollow) el rectángulo de cámara puede sobresalir por
            // fuera de la grilla, y la división entera de un negativo trunca
            // hacia cero en vez de hacer floor -- se evita el caso especial
            // recortando a [0,W]/[0,H] primero, donde la división entera ya
            // coincide con floor.
            int cellX0 = Mathf.Clamp(Mathf.FloorToInt((p.x - halfW) / CellWorldSize), 0, CellGrid.W);
            int cellX1 = Mathf.Clamp(Mathf.CeilToInt((p.x + halfW) / CellWorldSize), 0, CellGrid.W);
            int cellY0 = Mathf.Clamp(Mathf.FloorToInt((p.y - halfH) / CellWorldSize), 0, CellGrid.H);
            int cellY1 = Mathf.Clamp(Mathf.CeilToInt((p.y + halfH) / CellWorldSize), 0, CellGrid.H);

            cx0 = Mathf.Max(0, cellX0 / CellGrid.CHUNK - ViewMarginChunks);
            cy0 = Mathf.Max(0, cellY0 / CellGrid.CHUNK - ViewMarginChunks);
            int lastCellX = Mathf.Max(cellX0, cellX1 - 1);
            int lastCellY = Mathf.Max(cellY0, cellY1 - 1);
            cx1 = Mathf.Min(CellGrid.ChunksX, lastCellX / CellGrid.CHUNK + 1 + ViewMarginChunks);
            cy1 = Mathf.Min(CellGrid.ChunksY, lastCellY / CellGrid.CHUNK + 1 + ViewMarginChunks);
        }

        /// <summary>
        /// Redibuja los chunks que tocan la vista actual (despiertos, con
        /// animación puramente posicional, o sucios de un cambio mientras
        /// estaban fuera de cámara -- ver el mecanismo en la cabecera de
        /// _chunkLastRenderTick), o TODOS los que tocan la vista si toca
        /// refresco completo periódico. Sube la textura a GPU solo si de
        /// verdad se pintó algo.
        /// </summary>
        private float _lastAspect;
        private uint _ultimoTick;
        /// <summary>(R124) Repinta TODA la vista ahora mismo, aunque la sim esté en pausa (la piel de roca lo usa al encenderse/apagarse).</summary>
        public void RepintarAhora()
        {
            if (_texture == null) return;
            RenderFrame(_ultimoTick, true);
        }

        public void RenderFrame(uint tick, bool forceFull = false)
        {
            _ultimoTick = tick;
            _frameCounter++;
            bool full = forceFull || (_frameCounter % FullRefreshEveryFrames) == 0;

            // (playtest 15) EL BARRIDO YA NO RECORRE LOS 48x18=864 CHUNKS DEL
            // MUNDO: se acota al rectángulo que toca la vista (más margen).
            // Antes, con el mundo a una pantalla, barrer todo era barrer lo
            // visible -- ahora son cosas distintas y barrer de más cuesta
            // proporcional al MUNDO en vez de a la PANTALLA, justo lo que este
            // encargo pide evitar.
            // (playtest 39, contrato ENCARGO S 1d) La pátina se actualiza SIEMPRE
            // (independiente de la vista: el taller recuerda incendios que
            // ocurrieron fuera de cámara) -- por eso vive fuera del recorte de
            // ComputeVisibleChunkRange, con su propio acumulador de franjas.
            ActualizarPatinaFranja();

            ComputeVisibleChunkRange(out int cx0, out int cy0, out int cx1, out int cy1);

            bool anyDrawn = false;
            for (int cy = cy0; cy < cy1; cy++)
            {
                for (int cx = cx0; cx < cx1; cx++)
                {
                    int ci = CellGrid.ChunkIndex(cx, cy);

                    // (playtest 12) "Un patrón animado en un chunk DORMIDO no se
                    // verá animar" -- el problema real y por qué NO basta con
                    // subir FullRefreshEveryFrames:
                    //   · Manchas/Laberinto/Dendritas/Motas viven en CellGrid.morph,
                    //     que SimStepper.MorphTick SIGUE evolucionando en chunks
                    //     dormidos (a 1/8 de frecuencia, ~1s por paso -- ver la
                    //     cabecera de MorphTick). El refresco periódico de aquí
                    //     (cada 30 frames, ~1s) ya va prácticamente a la par de esa
                    //     cadencia: perderse algún frame intermedio no se nota
                    //     porque el propio DATO tampoco cambiaba en ese hueco.
                    //   · Vetas y Celdas son DISTINTOS: el contrato dice que el
                    //     stepper NO les toca morph -- son puramente posicionales y
                    //     los recalcula ComputeCellColor del `tick` en cada llamada
                    //     (ver más abajo). No tienen NINGÚN throttling propio -- si
                    //     el chunk no se redibuja, el patrón no "avanza despacio",
                    //     se queda CONGELADO en el frame exacto en que el chunk se
                    //     durmió, y al llegar el refresco completo salta de golpe al
                    //     instante actual. (Pulso NO entra en este grupo aunque
                    //     también "respire": Pulso sí vive en morph y ya hereda el
                    //     throttle de MorphTick -- ver el comentario junto a
                    //     `continuousAnim` en ComputeCellColor.)
                    //   · Subir la frecuencia de refresco completo "arreglaría" esto
                    //     a costa de redibujar TODA LA VISTA más a menudo por un
                    //     puñado de sustancias innominadas que quizá ocupen 2 o 3
                    //     chunks -- caro de más para el problema real.
                    // Solución elegida: marcar por chunk (RenderChunk, abajo) si
                    // contiene materia con patrón puramente temporal + ritmoAnim>0,
                    // y eximir SOLO esos chunks del sueño para el REDIBUJADO (la
                    // física de esa celda se queda dormida igual; solo se repinta).
                    // Al vivir DENTRO del rango ya acotado a la vista, un chunk
                    // animado fuera de cámara simplemente no se evalúa -- no se
                    // anima nada que nadie ve (la otra mitad del encargo).
                    bool awake = _grid.IsChunkAwake(cx, cy);
                    bool animado = _chunkContinuousAnim[ci];

                    // CHUNKS SUCIOS FUERA DE CÁMARA (ver la cabecera de
                    // _chunkLastRenderTick para el mecanismo completo): un
                    // chunk que cambió mientras el barrido no lo visitaba y ya
                    // volvió a dormirse (30 ticks sin cambios) entraría en
                    // vista con awake=false y colores viejos si no fuera por
                    // este chequeo -- se compara contra chunkTouchedTick, que
                    // CellGrid mantiene al día para TODOS los chunks siempre,
                    // los visite o no este renderer.
                    bool sucioFueraDeCamara = !_chunkEverRendered[ci]
                        || _grid.chunkTouchedTick[ci] != _chunkLastRenderTick[ci];

                    if (!full && !awake && !animado && !sucioFueraDeCamara) continue;
                    RenderChunk(cx, cy, tick, ci);
                    anyDrawn = true;
                }
            }

            // (playtest 15) Texture2D.Apply() sube la textura ENTERA (no solo
            // los rectángulos tocados por SetPixels32) cada vez que se llama --
            // antes se llamaba en TODOS los frames con al menos un tick de sim,
            // incluso los que no pintaban ni un chunk (mundo entero dormido,
            // cámara quieta). Con el mundo asentado eso es tirar ~864KB de
            // ancho de banda a GPU por nada, cada frame. Ahora solo sube si
            // este mismo RenderFrame pintó de verdad algo.
            if (anyDrawn)
            {
                _texture.Apply(false);
                if (_frontTexture != null) _frontTexture.Apply(false); // el labio (si está activo) va al mismo ritmo.
                _veloTexture.Apply(false); // (R129) el velo de líquidos va al mismo ritmo.
            }
        }

        // =================================================================
        // PÁTINA — LA MEMORIA SUPERFICIAL (playtest 39, contrato ENCARGO S 1d)
        // =================================================================
        // La escribe y la lee SOLO este archivo (JAMÁS SimStepper, ver el
        // docblock de CellGrid.patina): cero coste en el tick de sim, y en
        // multi cada cliente la reconstruye sola de lo que ve replicado.
        // Sondeo por ACUMULADOR DE FRANJAS (contrato: "no todo el mundo"):
        // unas pocas filas por frame, TODO el ancho del mundo (no se recorta
        // a cámara -- el taller recuerda incendios que ocurrieron fuera de
        // vista), así que una vuelta completa tarda H/PatinaRowsPerFrame
        // frames (288/12 = 24 frames, bajo medio segundo a 60fps).
        // =================================================================
        // INCANDESCENCIA (playtest 41, CONTRATO_VAPOR.md §2)
        // Los cuatro números que gobiernan "cómo se ve un material caliente".
        // Los consume ComputeCellColor (busca "INCANDESCENCIA LEGIBLE" para
        // el razonamiento completo y la cronología del bug que arreglan).
        // Se dejan aquí, con nombre, para que recalibrarlos sea cambiar una
        // cifra y volver a mirar -- no bucear en el hot path.
        // =================================================================
        /// <summary>Temperatura raw a partir de la cual una celda empieza a verse caliente. 150 raw = ~180°C. Sin cambios respecto al playtest 40: el problema era la profundidad del tinte, no cuándo arranca.</summary>
        private const byte IncandInicioRaw = 150;
        /// <summary>Techo de MEZCLA hacia el ámbar, incluso a raw 255. 0.45 = a fuego pleno sobrevive el 55% del matiz del material. Subirlo por encima de ~0.55 reabre el bug de "polvo azul que se ve amarillo" (regla 52: se juzga en pantalla, no en el hex).</summary>
        private const float IncandTechoMezcla = 0.45f;
        /// <summary>Brasa ADITIVA a fuego pleno, canal rojo. Sumar (en vez de mezclar) conserva intactas las diferencias entre materiales: es la capa que hace que el calor se lea como luz propia y no como repintado.</summary>
        private const float IncandBrasaR = 72f;
        /// <summary>Brasa aditiva, canal verde: bastante menor que el rojo para que el rescoldo tire a naranja, no a blanco.</summary>
        private const float IncandBrasaG = 30f;
        /// <summary>Brasa aditiva, canal azul: casi nula -- una brasa no emite azul. Deja que el azul propio del material sobreviva entero al calor.</summary>
        private const float IncandBrasaB = 6f;

        private int _patinaRow;
        private int _patinaDecayCounter;
        private const int PatinaRowsPerFrame = 12;
        private const byte PatinaMojadoTecho = 90;   // techo de "húmedo": se seca rápido.
        private const byte PatinaTizneTecho = 220;   // techo de "tizne": casi permanente.
        private const int PatinaTizneIncremento = 14;     // junto a Fire/Brasa.
        private const int PatinaTizneHumoIncremento = 6;  // bajo una bóveda con Smoke pegado -- más lento que el contacto directo con la llama.
        private const int PatinaMojadoIncremento = 10;     // junto a un líquido.
        private const int PatinaDecayMojado = 2;           // por franja procesada, sin contacto: "se seca solo".
        private const int PatinaDecayTizneCadaFranjas = 6; // el tizne decae 1 unidad cada N pasadas de ESA fila (casi permanente, no "se seca").

        private void ActualizarPatinaFranja()
        {
            if (_grid == null || _universe == null) return;
            _patinaDecayCounter++;
            bool decayTizneEstaVuelta = (_patinaDecayCounter % PatinaDecayTizneCadaFranjas) == 0;

            for (int r = 0; r < PatinaRowsPerFrame; r++)
            {
                int y = _patinaRow;
                _patinaRow++;
                if (_patinaRow >= CellGrid.H) _patinaRow = 0;

                int rowBase = y * CellGrid.W;
                for (int x = 0; x < CellGrid.W; x++)
                {
                    int idx = rowBase + x;
                    byte matId = _grid.mat[idx];
                    // La pátina solo vive en superficies SÓLIDAS (piedra, obra
                    // del taller) -- es lo que el contrato describe en los tres
                    // casos (tizne junto a fuego, mojado junto a sólido, bóveda
                    // tiznada), nunca la propia sustancia líquida/gas/fuego.
                    // (integración pt39) Y si la celda DEJÓ de ser sólida (el
                    // cincel talló, la mudanza se llevó su mampostería), la
                    // mancha muere aquí mismo: así la piedra que se re-talle o
                    // re-pinte después nace LIMPIA, sin manchas fantasma, y
                    // Cincel/Mudanza no necesitan saber que la pátina existe
                    // (este barrido por franjas es el único punto de verdad).
                    if (_universe.Get(matId).archetype != MaterialArchetype.StaticSolid)
                    {
                        if (_grid.patina[idx] != 0) _grid.patina[idx] = 0;
                        continue;
                    }

                    bool juntoATizne = TieneVecinoOrtogonal(x, y, MaterialId.Fire) || TieneVecinoOrtogonal(x, y, MaterialId.Brasa);
                    bool humoEncima = !juntoATizne && y > 0 && _grid.mat[idx - CellGrid.W] == MaterialId.Smoke;
                    bool juntoALiquido = !juntoATizne && !humoEncima && TieneVecinoLiquido(x, y);

                    byte pat = _grid.patina[idx];
                    if (juntoATizne)
                    {
                        int v = pat + PatinaTizneIncremento;
                        _grid.patina[idx] = (byte)(v > PatinaTizneTecho ? PatinaTizneTecho : v);
                    }
                    else if (humoEncima)
                    {
                        int v = pat + PatinaTizneHumoIncremento;
                        _grid.patina[idx] = (byte)(v > PatinaTizneTecho ? PatinaTizneTecho : v);
                    }
                    else if (juntoALiquido)
                    {
                        // (playtest 44, pedido directo de Cesar) EL MOJADO SE
                        // APAGA: "eso de que el agua moje la superficie está
                        // raro, parece que se va a filtrar pero no se filtra
                        // -- de momento evitarlo". Tenía razón: oscurecer la
                        // piedra junto al líquido PROMETE una filtración que
                        // la sim no hace -- pátina mintiendo sobre física. El
                        // tizne (fuego/humo) se queda: ese sí cuenta una
                        // historia verdadera. Su idea del goteo-que-se-seca
                        // queda anotada en HANDOFF para cuando haya
                        // filtración real. (regla 15: rama conservada)
                        // int v = pat + PatinaMojadoIncremento;
                        // _grid.patina[idx] = (byte)(v > PatinaMojadoTecho ? PatinaMojadoTecho : v);
                    }
                    else if (pat > 0)
                    {
                        if (pat <= PatinaMojadoTecho)
                        {
                            int v = pat - PatinaDecayMojado;
                            _grid.patina[idx] = (byte)(v < 0 ? 0 : v);
                        }
                        else if (decayTizneEstaVuelta)
                        {
                            _grid.patina[idx] = (byte)(pat - 1);
                        }
                    }
                }
            }
        }

        private bool TieneVecinoOrtogonal(int x, int y, byte materialId)
        {
            if (x > 0 && _grid.mat[CellGrid.Idx(x - 1, y)] == materialId) return true;
            if (x < CellGrid.W - 1 && _grid.mat[CellGrid.Idx(x + 1, y)] == materialId) return true;
            if (y > 0 && _grid.mat[CellGrid.Idx(x, y - 1)] == materialId) return true;
            if (y < CellGrid.H - 1 && _grid.mat[CellGrid.Idx(x, y + 1)] == materialId) return true;
            return false;
        }

        private bool TieneVecinoLiquido(int x, int y)
        {
            if (x > 0 && _universe.Get(_grid.mat[CellGrid.Idx(x - 1, y)]).archetype == MaterialArchetype.Liquid) return true;
            if (x < CellGrid.W - 1 && _universe.Get(_grid.mat[CellGrid.Idx(x + 1, y)]).archetype == MaterialArchetype.Liquid) return true;
            if (y > 0 && _universe.Get(_grid.mat[CellGrid.Idx(x, y - 1)]).archetype == MaterialArchetype.Liquid) return true;
            if (y < CellGrid.H - 1 && _universe.Get(_grid.mat[CellGrid.Idx(x, y + 1)]).archetype == MaterialArchetype.Liquid) return true;
            return false;
        }

        private void RenderChunk(int cx, int cy, uint tick, int chunkIndex)
        {
            CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);
            int w = x1 - x0;
            int h = y1 - y0;
            var scratch = _chunkScratch; // 256x144: todos los chunks son 16x16 (ver Init).

            int t = (int)tick;
            int scratchI = 0;
            bool chunkAnimado = false;
            for (int y = y0; y < y1; y++)
            {
                int rowBase = y * CellGrid.W;
                for (int x = x0; x < x1; x++)
                {
                    int idx = rowBase + x;
                    Color32 c = ComputeCellColor(x, y, idx, t, out bool continuo);
                    if (continuo) chunkAnimado = true;
                    _pixels[idx] = c;
                    scratch[scratchI] = c;
                    if (_frontScratch != null) _frontScratch[scratchI] = ComputeFrontRim(x, y, idx);
                    // (R129) El velo: la MISMA celda ya calculada, delante y
                    // translúcida — solo si es líquido; todo lo demás, hueco.
                    byte matVelo = _grid.mat[idx];
                    _veloScratch[scratchI] = (matVelo != MaterialId.Empty &&
                        _universe.Get(matVelo).archetype == MaterialArchetype.Liquid)
                        ? new Color32(c.r, c.g, c.b, VeloAlfa)
                        : default;
                    scratchI++;
                }
            }
            _chunkContinuousAnim[chunkIndex] = chunkAnimado;

            // (playtest 15) Marca este chunk como "al día" con el estado que
            // CellGrid conoce en este instante -- ver la cabecera de
            // _chunkLastRenderTick para el porqué (mecanismo de chunks sucios
            // fuera de cámara).
            _chunkLastRenderTick[chunkIndex] = _grid.chunkTouchedTick[chunkIndex];
            _chunkEverRendered[chunkIndex] = true;

            _texture.SetPixels32(x0, y0, w, h, scratch);
            if (_frontTexture != null) _frontTexture.SetPixels32(x0, y0, w, h, _frontScratch);
            _veloTexture.SetPixels32(x0, y0, w, h, _veloScratch);
        }

        /// <summary>
        /// (RONDA 66) EL LABIO FRONTAL: para una celda de AIRE pegada a roca
        /// madre o piso estructural, devuelve la sombra de la "cara" de esa
        /// masa hacia cámara; para todo lo demás, transparente. SOLO sobre
        /// vacío a propósito: en cuanto materia real entra a la celda, el
        /// labio desaparece ahí -- las reacciones son protagonistas y nada
        /// las tapa (mandato de Cesar). El aprendiz (sprite, no celda) sí
        /// queda parcialmente detrás: esa oclusión ES la profundidad.
        /// </summary>
        private Color32 ComputeFrontRim(int x, int y, int idx)
        {
            if (_grid.mat[idx] != MaterialId.Empty) return default;

            bool junto =
                (x > 0 && EsEstructuraFrontal(_grid.mat[idx - 1])) ||
                (x < CellGrid.W - 1 && EsEstructuraFrontal(_grid.mat[idx + 1])) ||
                (y > 0 && EsEstructuraFrontal(_grid.mat[idx - CellGrid.W])) ||
                (y < CellGrid.H - 1 && EsEstructuraFrontal(_grid.mat[idx + CellGrid.W]));
            if (!junto) return default;

            // Pardo profundo con variación estable por celda (nunca animado):
            // se lee como el canto en sombra de la masa, no como un contorno
            // dibujado. Alfa moderado: oscurece, no tapa.
            int j = (int)(Hash2D(x, y) % 7) - 3;
            return new Color32(ClampByte(24 + j), ClampByte(19 + j), ClampByte(15 + j), 150);
        }

        private static bool EsEstructuraFrontal(byte mat)
            => mat == MaterialId.Stone || mat == MaterialId.PisoEstructural;

        private Color32 ComputeCellColor(int x, int y, int idx, int tick, out bool continuousAnim)
        {
            // (playtest 12) Se asigna en la primera línea porque hay un `return`
            // temprano un poco más abajo (celda vacía) y C# exige que un
            // parámetro `out` quede asignado en TODOS los caminos. Por defecto
            // "no": solo lo pone a `true` el bloque de patrón (más abajo), y solo
            // para Vetas/Celdas/Pulso con ritmoAnim>0 -- el porqué completo está
            // en RenderFrame, donde se consume.
            continuousAnim = false;

            byte matId = _grid.mat[idx];
            if (matId == MaterialId.Empty) return default;
            // (R124, PRUEBA "piel de roca") Con la piel activa la ROCA MADRE no
            // se pinta en la textura de la sim: la dibuja Game/PielDeRoca como
            // malla de contorno orgánico (marching squares) POR DEBAJO (-6).
            // Téxel a alfa 0 = agujero limpio, no semitransparencia (la regla
            // 19 del mosaico no aplica). La FÍSICA no cambia: la celda sigue
            // siendo Stone para el stepper y para la colisión.
            if (OcultarRoca && matId == MaterialId.Stone) return default;

            var def = _universe.Get(matId);
            Color32 baseColor = def.baseColor;

            // FUEGO: color de llama dedicado (no pasa por jitter ni tinte de temperatura,
            // que lo lavaban hacia beige). Llama joven = amarillo brillante; al agotarse
            // su vida (aux) baja hacia rojo profundo, con parpadeo estable por celda.
            if (matId == MaterialId.Fire)
            {
                float life = def.gasLifetime > 0
                    ? Mathf.Clamp01(_grid.aux[idx] / (float)def.gasLifetime)
                    : 1f;
                int fl = (int)(Hash3D(x, y, tick / 2) % 70);
                byte fr = ClampByte(205 + (int)(50f * life));
                byte fg = ClampByte(55 + (int)(150f * life) + fl / 2);
                byte fb = ClampByte(8 + (int)(55f * life));
                return new Color32(fr, fg, fb, 255);
            }

            byte r = baseColor.r, g = baseColor.g, b = baseColor.b;

            if (def.colorJitter > 0)
            {
                int j = (int)(Hash2D(x, y) % (uint)(def.colorJitter * 2 + 1)) - def.colorJitter;
                r = ClampByte(r + j);
                g = ClampByte(g + j);
                b = ClampByte(b + j);
            }

            // =================================================================
            // FIRMA VISUAL: PATRÓN MORFOLÓGICO (playtest 12)
            // =================================================================
            // "después del color base y su jitter" -- exactamente aquí, antes de
            // cualquier ajuste de arquetipo (sillería, canto de polvo, líquidos...)
            // para que ambas capas se sumen igual que ya se suman colorJitter y la
            // sillería de StaticSolid: el patrón MODULA lo que venga después, no lo
            // sustituye.
            //
            // Gate único: patronFuerza>0. Universe.Create fuerza patronFuerza=0
            // SIEMPRE que patron==Liso (y Liso es lo único que puede llevar el
            // vocabulario del taller -- ver la cabecera de MaterialDef). Con esto
            // el vocabulario del taller ni siquiera evalúa el switch de ApplyPatron:
            // un único branch barato, cero cambio de imagen, la garantía que pide
            // el encargo ("debe verse EXACTAMENTE igual que hoy").
            if (def.patronFuerza > 0)
            {
                byte morphVal = _grid.morph[idx];
                ApplyPatron(x, y, morphVal, def, tick, ref r, ref g, ref b);

                // SOLO Vetas y Celdas fuerzan redibujo en chunk dormido (ver el
                // porqué completo en RenderFrame). Deliberadamente NO se incluye
                // Pulso aquí aunque sea "lo que más se nota respirar": Pulso SÍ usa
                // CellGrid.morph (SimStepper.MorphTick lo recalcula de tick+posición
                // cada vez que le toca turno), y ese turno YA se throttlea 8x en
                // chunks dormidos (ver MorphTick, "dormantActiveRound"). Redibujar
                // Pulso cada frame no arreglaría nada: el valor guardado en morph
                // seguiría siendo el mismo entre esos turnos throttleados, así que
                // solo se pagaría el coste de subir a GPU un píxel IDÉNTICO. El
                // refresco periódico de más abajo (cada ~1s) ya va a la par de ese
                // throttle de 1/8 (~1.07s) -- ahí no hay nada que el renderer pueda
                // arreglar sin tocar SimStepper (fuera de alcance de este archivo).
                // Vetas y Celdas son el caso distinto: no usan morph en absoluto,
                // así que NO tienen throttling de ningún tipo -- si no se redibujan,
                // se congelan de verdad, no "avanzan despacio".
                if (def.ritmoAnim > 0 &&
                    (def.patron == PatronMorfologico.Vetas || def.patron == PatronMorfologico.Celdas))
                {
                    continuousAnim = true;
                }
            }

            // PIEDRA Y SÓLIDOS ESTÁTICOS (fix playtest 6: baja resolución): el color
            // base es casi plano y colorJitter es mínimo, así que el suelo, los muros
            // de las cubas y el contrafuerte de la tolva ocupaban áreas enormes sin
            // ninguna estructura -- se leían como una mancha borrosa, en contraste con
            // los sprites de maquinaria (que sí tienen patrón). Se añade, en este
            // orden y con aritmética entera pura (sin Mathf.Pow, sin allocs -- corre
            // por celda de chunk despierto):
            //  1) Aparejo de sillería: bloques de 8x4 celdas con las hiladas impares
            //     desplazadas medio bloque, tono ±6% estable por bloque (hash) y
            //     juntas (borde de bloque) oscurecidas ~22%. Esto es lo que rompe la
            //     mancha plana.
            //  2) Iluminación de canto (lo que más rinde): cara superior contra el
            //     vacío +28% (luz), canto inferior contra el vacío -20% (sombra),
            //     canto izquierdo contra el vacío +10% (más sutil). Labios de cubas,
            //     bordes de plataformas y el contrafuerte se leen como arquitectura
            //     tallada.
            //  3) Grano fino por celda (hash DISTINTO al del bloque, no animado):
            //     ±4 por canal, para que ninguna zona quede perfectamente lisa.
            // Todo estable por (x,y) vía Hash2D/Hash3D -- nada de UnityEngine.Random.
            if (def.archetype == MaterialArchetype.StaticSolid)
            {
                bool esPiso = matId == MaterialId.PisoEstructural;
                if (esPiso)
                {
                    // (RONDA 66) EL PISO ESTRUCTURAL SE DIBUJA FABRIL: viguetas
                    // horizontales de 3 filas con junta oscura y un remache cada
                    // 6 columnas -- recto y repetido, el contraste exacto contra
                    // la sillería orgánica de la roca ("recto y estructural, en
                    // contraste con la roca madre", mandato de Cesar). Cero
                    // tono por bloque: lo fabricado es uniforme.
                    if ((y % 3) == 0)
                    {
                        r = (byte)(r * 80 / 100); g = (byte)(g * 80 / 100); b = (byte)(b * 80 / 100);
                    }
                    else if ((x % 6) == 3 && (y % 3) == 2)
                    {
                        r = (byte)(r * 68 / 100); g = (byte)(g * 68 / 100); b = (byte)(b * 68 / 100); // remache.
                    }

                    // (RONDA 68, idea de Cesar: "preseteado cómo se ve la
                    // madera sobre piedra madre y cómo se ve al aire") ACABADO
                    // POR CONTEXTO -- cada cara del piso se termina según lo
                    // que toque:
                    //  · cara al AIRE: canto NETO de pieza fabricada (arriba
                    //    superficie clara +30%; abajo/lados silueta firme).
                    //  · cara contra ROCA: junta de asiento (-10%): se lee
                    //    encastrado en el muro, no flotando delante de él.
                    byte mArr = y < CellGrid.H - 1 ? _grid.mat[idx + CellGrid.W] : MaterialId.Stone;
                    byte mAbj = y > 0 ? _grid.mat[idx - CellGrid.W] : MaterialId.Stone;
                    byte mIzq = x > 0 ? _grid.mat[idx - 1] : MaterialId.Stone;
                    byte mDer = x < CellGrid.W - 1 ? _grid.mat[idx + 1] : MaterialId.Stone;

                    if (mArr == MaterialId.Empty)
                    {
                        r = ClampByte(r + r * 30 / 100); g = ClampByte(g + g * 30 / 100); b = ClampByte(b + b * 30 / 100);
                    }
                    else if (mArr == MaterialId.Stone)
                    {
                        r = (byte)(r * 90 / 100); g = (byte)(g * 90 / 100); b = (byte)(b * 90 / 100);
                    }
                    if (mAbj == MaterialId.Empty)
                    {
                        r = (byte)(r * 74 / 100); g = (byte)(g * 74 / 100); b = (byte)(b * 74 / 100);
                    }
                    else if (mAbj == MaterialId.Stone)
                    {
                        r = (byte)(r * 88 / 100); g = (byte)(g * 88 / 100); b = (byte)(b * 88 / 100);
                    }
                    if (mIzq == MaterialId.Empty || mDer == MaterialId.Empty)
                    {
                        r = (byte)(r * 84 / 100); g = (byte)(g * 84 / 100); b = (byte)(b * 84 / 100);
                    }
                    else if (mIzq == MaterialId.Stone || mDer == MaterialId.Stone)
                    {
                        r = (byte)(r * 92 / 100); g = (byte)(g * 92 / 100); b = (byte)(b * 92 / 100);
                    }
                }
                else
                {
                    const int BlockW = 8, BlockH = 4;
                    int hilada = y / BlockH;
                    int sx = x + ((hilada & 1) != 0 ? BlockW / 2 : 0); // hiladas impares desplazadas medio bloque
                    int localX = sx % BlockW;
                    int blockX = sx / BlockW;

                    int tono = (int)(Hash2D(blockX, hilada) % 13) - 6; // ±6%
                    r = ClampByte(r + r * tono / 100);
                    g = ClampByte(g + g * tono / 100);
                    b = ClampByte(b + b * tono / 100);

                    if (localX == 0 || (y % BlockH) == 0) // junta de sillería (borde del bloque): -22%
                    {
                        r = (byte)(r * 78 / 100);
                        g = (byte)(g * 78 / 100);
                        b = (byte)(b * 78 / 100);
                    }
                }

                // (RONDA 68) Los cantos/interior/esquinas/grano genéricos son
                // SOLO de la roca y demás sólidos orgánicos: el piso ya trae
                // su propio acabado por contexto arriba (aplicar los dos lo
                // ensuciaba el doble).
                if (!esPiso)
                {
                    // idx+W = celda de ARRIBA (y+1), idx-W = celda de ABAJO (y-1): ver
                    // SimStepper (belowIdx = idx - W, aboveIdx = idx + W) y el check de
                    // superficie de líquidos más abajo en este mismo archivo.
                    bool arribaVacia = y < CellGrid.H - 1 && _grid.mat[idx + CellGrid.W] == MaterialId.Empty;
                    bool abajoVacia = y > 0 && _grid.mat[idx - CellGrid.W] == MaterialId.Empty;
                    bool izqVacia = x > 0 && _grid.mat[idx - 1] == MaterialId.Empty;
                    bool derVacia = x < CellGrid.W - 1 && _grid.mat[idx + 1] == MaterialId.Empty; // (ronda 66) el canto derecho existía sin sombra: masa asimétrica.
                    if (arribaVacia)
                    {
                        r = ClampByte(r + r * 28 / 100);
                        g = ClampByte(g + g * 28 / 100);
                        b = ClampByte(b + b * 28 / 100);
                    }
                    if (abajoVacia)
                    {
                        r = (byte)(r * 80 / 100);
                        g = (byte)(g * 80 / 100);
                        b = (byte)(b * 80 / 100);
                    }
                    if (izqVacia)
                    {
                        r = ClampByte(r + r * 10 / 100);
                        g = ClampByte(g + g * 10 / 100);
                        b = ClampByte(b + b * 10 / 100);
                    }
                    if (derVacia)
                    {
                        r = (byte)(r * 88 / 100);
                        g = (byte)(g * 88 / 100);
                        b = (byte)(b * 88 / 100);
                    }

                    // (RONDA 66, la clave del "autotiling" percibido) VOLUMEN DE
                    // MASA: el interior sin caras al aire se hunde un 8% -- el
                    // borde queda como aro iluminado y varias celdas chicas se
                    // leen como UNA roca grande ("lógica pequeña, apariencia más
                    // grande"). Y una celda con 2+ caras expuestas (esquina) se
                    // apaga un 12%: el ojo la redondea, marching-squares gratis.
                    int carasAlAire = (arribaVacia ? 1 : 0) + (abajoVacia ? 1 : 0) + (izqVacia ? 1 : 0) + (derVacia ? 1 : 0);
                    if (carasAlAire == 0)
                    {
                        r = (byte)(r * 92 / 100);
                        g = (byte)(g * 92 / 100);
                        b = (byte)(b * 92 / 100);
                    }
                    else if (carasAlAire >= 2)
                    {
                        r = (byte)(r * 88 / 100);
                        g = (byte)(g * 88 / 100);
                        b = (byte)(b * 88 / 100);
                    }

                    int grano = (int)(Hash3D(x, y, 97) % 9) - 4; // ±4, hash distinto al del bloque y sin componente de tiempo (la piedra no se anima)
                    r = ClampByte(r + grano);
                    g = ClampByte(g + grano);
                    b = ClampByte(b + grano);
                }
            }

            // POWDERS (fix playtest 6: baja resolución): aclarado de canto superior
            // más suave que el de la piedra (+15%, sin sillería -- no son bloques
            // tallados) cuando la celda de arriba está vacía. Da volumen al montón de
            // arena/ceniza y rompe la mancha plana. NO toca fuego ni líquidos (rutas
            // de color propias, ya validadas por el jugador -- ver más abajo).
            if (def.archetype == MaterialArchetype.Powder
                && y < CellGrid.H - 1 && _grid.mat[idx + CellGrid.W] == MaterialId.Empty)
            {
                r = ClampByte(r + r * 15 / 100);
                g = ClampByte(g + g * 15 / 100);
                b = ClampByte(b + b * 15 / 100);
            }

            // Resplandor animado (fuego, materiales emisivos): parpadeo estable por celda+frame.
            if (def.emitsGlow)
            {
                int flicker = (int)(Hash3D(x, y, tick / 3) % 60);
                r = ClampByte(r + flicker);
                g = ClampByte(g + flicker / 2);
            }

            // Vivium "dormido" (fuera de su banda de crecimiento, ver SimStepper.GrowthTick):
            // ligera desaturación hacia gris para leerse como "en pausa" sin un shader aparte.
            if (def.archetype == MaterialArchetype.Organic && (_grid.aux[idx] & CellGrid.OrganicDormantAux) != 0)
            {
                int gray = (r + g + b) / 3;
                r = LerpByte(r, (byte)gray, 0.55f);
                g = LerpByte(g, (byte)gray, 0.55f);
                b = LerpByte(b, (byte)gray, 0.55f);
            }

            // LÍQUIDOS: sensación de "mojado" barata — shimmer lento por celda y una
            // línea de superficie más clara donde el líquido toca aire (celda vacía encima).
            if (def.archetype == MaterialArchetype.Liquid)
            {
                int s = (int)(Hash3D(x, y, tick / 8) % 15) - 7;
                r = ClampByte(r + s);
                g = ClampByte(g + s);
                b = ClampByte(b + s + 4);
                if (y < CellGrid.H - 1 && _grid.mat[idx + CellGrid.W] == MaterialId.Empty)
                {
                    r = ClampByte(r + 30);
                    g = ClampByte(g + 34);
                    b = ClampByte(b + 40);
                }
            }

            // =================================================================
            // BORDE MORFOLÓGICO (playtest 12): lo primero que el ojo compara
            // entre dos sustancias, y es barato -- se reutiliza el mismo chequeo
            // de "vecino vacío" que ya paga StaticSolid arriba, generalizado a
            // los 4 vecinos ortogonales (arriba/abajo/izq/der) para cualquier
            // arquetipo, no solo piedra.
            // =================================================================
            // Gate: borde!=Neto. Neto (0) es el valor por defecto de MaterialDef
            // y NUNCA se toca para el vocabulario del taller (Universe.Create solo
            // sortea `borde` para lo innominado) -- otro branch barato, cero
            // cambio de imagen para lo ya validado.
            if (def.borde != BordeMorfologico.Neto)
            {
                bool esBorde = (y < CellGrid.H - 1 && _grid.mat[idx + CellGrid.W] == MaterialId.Empty)
                            || (y > 0 && _grid.mat[idx - CellGrid.W] == MaterialId.Empty)
                            || (x > 0 && _grid.mat[idx - 1] == MaterialId.Empty)
                            || (x < CellGrid.W - 1 && _grid.mat[idx + 1] == MaterialId.Empty);
                if (esBorde)
                {
                    switch (def.borde)
                    {
                        case BordeMorfologico.Halo:
                            // Aureola tenue del propio color: brillo + el empuje de
                            // saturación de ModulatePattern (mismo lenguaje que el
                            // patrón interno, así el borde "rima" con el resto de la
                            // firma visual en vez de ser un efecto aparte). Fuerza
                            // fija (no depende de patronFuerza): el borde es una
                            // propiedad de SILUETA, no del patrón interno -- una
                            // sustancia con patronFuerza=0 pero borde=Halo (caso
                            // raro pero posible si algún día se desacoplan) debe
                            // seguir teniendo aureola.
                            ModulatePattern(ref r, ref g, ref b, 34);
                            break;

                        case BordeMorfologico.Escarcha:
                            // Cristalitos claros DISPERSOS, no una línea continua:
                            // ~1 de cada 3 celdas de contorno se enciende. Hash
                            // estable por celda (no por tick) -- es un rasgo de
                            // SILUETA, no debe titilar.
                            if ((Hash3D(x, y, 211 + def.semillaPatron) % 3) == 0)
                            {
                                r = ClampByte(r + 70);
                                g = ClampByte(g + 70);
                                b = ClampByte(b + 70);
                            }
                            break;

                        case BordeMorfologico.Difuso:
                            // (decisión de arte, playtest 12) La lectura obvia de
                            // "el borde pierde opacidad" es bajar alfa. Se descarta
                            // A PROPÓSITO tras comprobarlo: el sprite del sim es 1
                            // téxel por celda con FilterMode.Point (ver Init, arriba)
                            // estirado a pantalla completa, y detrás vive
                            // Game/WorkshopBackdrop -- OTRA textura, a triple
                            // resolución (x3, ver su cabecera), TAMBIÉN en Point. Un
                            // téxel semitransparente ahí no se funde con nada: dos
                            // texturas Point de resoluciones distintas componiendo
                            // alfa producen un mosaico duro del ladrillo de fondo
                            // asomando por bloques de ~7.5px sin ningún suavizado --
                            // exactamente la lectura de "recorte roto" que advertía
                            // el encargo, no "deshilachado orgánico". Se consigue la
                            // MISMA sensación (el borde "se pierde" contra el fondo)
                            // oscureciendo hacia BackgroundColor en la mitad de las
                            // celdas de contorno (mismo hash disperso que Escarcha,
                            // pero hacia oscuro en vez de hacia claro) -- el borde se
                            // funde visualmente con el taller sin tocar el canal
                            // alfa ni depender de qué haya detrás en ese frame.
                            if ((Hash3D(x, y, 217 + def.semillaPatron) % 2) == 0)
                            {
                                r = LerpByte(r, BgColor32.r, 0.55f);
                                g = LerpByte(g, BgColor32.g, 0.55f);
                                b = LerpByte(b, BgColor32.b, 0.55f);
                            }
                            break;
                    }
                }
            }

            // (nota playtest 13, investigación de "el rosa es transparente") Este
            // branch de Difuso está limpio: nunca toca alfa, solo r/g/b, tal como
            // manda la regla 19. La transparencia que reportó el jugador para una
            // sustancia rosa (se veían los ladrillos del fondo DENTRO de la masa,
            // no solo en el contorno) no salía de aquí -- salía de
            // `baseColor.a`, el único origen del alfa final de esta función (ver
            // el `return` al fondo de ComputeCellColor). Universe.Create heredaba
            // ese alfa del roster incluso tras el resorteo de firma visual de lo
            // innominado, y los tres Liquid innominados (Azoth/Slime/Acid)
            // arrancan con alfa 215/220/235 en el roster -- mismo bug de fondo
            // que esta regla 19 advierte (mosaico duro contra WorkshopBackdrop),
            // pero aplicado a la sustancia ENTERA en vez de solo al contorno.
            // Fix real en Sim/Universe.cs (SortearFirmasVisuales, no aquí): lo
            // innominado ahora fuerza alfa 255 siempre. El vocabulario del taller
            // (Water/Oil, con su propio alfa <255 de diseño) no pasa por ese
            // sorteo y no se ha tocado -- sigue viéndose exactamente igual.

            // EMISIÓN (playtest 12): luz propia, CONSTANTE e independiente del
            // patrón -- distinta de emitsGlow (el parpadeo heredado del fuego,
            // arriba, que sigue exactamente igual). def.emision es 0 para todo el
            // vocabulario del taller (nunca se sortea ahí, ver Universe.Create):
            // otro no-op de un branch para lo ya validado.
            if (def.emision > 0)
            {
                int amt = def.emision * 2 / 5; // 0..255 -> 0..~102: aporta sin quemar a blanco de golpe en el tope.
                r = ClampByte(r + amt);
                g = ClampByte(g + amt);
                b = ClampByte(b + amt);
            }

            // INCANDESCENCIA LEGIBLE (playtest 41, CONTRATO_VAPOR.md §2 -- fix
            // del "amarillo engañoso"). Traduce la temperatura a color PARA EL
            // JUGADOR que tiene que reconocer qué material está mirando
            // mientras arde; el consumidor es su ojo, no la sim (esta función
            // no escribe nada en el grid).
            //
            // QUÉ HABÍA Y POR QUÉ SE CAMBIÓ (regla 15): antes era
            //     t01 = (raw-150)/105;  r=Lerp(r,255,t01); g=Lerp(g,214,t01); b=Lerp(b,140,t01);
            // es decir, una mezcla LINEAL que llegaba al 100% en raw 255. A
            // fuego de brasero (raw 220 = 320°C, medido en pantalla con el
            // hover de F3) eso ya fundía el 67% del color: un polvo azul se
            // veía AMARILLO y, al recogerlo con el frasco, volvía a ser azul.
            // Cesar lo reportó tras el playtest 40 con esas palabras exactas
            // ("me confundió el cambio de color del limo que al calentarlo se
            // pone amarillo pero al recogerlo resulta que es azul"). El fallo
            // no era el tinte: era que el tinte BORRABA la identidad.
            //
            // CÓMO SE ARREGLA, en dos capas deliberadamente distintas:
            //  (1) BRASA ADITIVA -- una suma constante por canal, sesgada al
            //      rojo. Sumar NO cambia la diferencia entre dos materiales
            //      (si A y B distaban 90 en el canal verde, siguen distando
            //      90): es lo que hace que el calor se lea como LUZ PROPIA
            //      encima de la sustancia, no como repintarla. Es también lo
            //      físicamente honesto: un cuerpo caliente EMITE, no cambia
            //      de pigmento.
            //  (2) MEZCLA ACOTADA hacia el ámbar con TECHO 0.45 -- a raw 255
            //      (390°C) sobrevive el 55% del matiz original. Es el techo
            //      que pide el contrato (45-55%) y el que garantiza que dos
            //      materiales distintos SIGAN siendo dos colores distintos en
            //      el punto más caliente que el taller puede alcanzar.
            //
            // CURVA: el arranque sigue en raw 150 (~180°C) a propósito -- lo
            // que sobraba no era el aviso temprano de calor sino su
            // PROFUNDIDAD -- pero la respuesta pasa a ser CUADRÁTICA. A raw
            // 200 (~280°C) la mezcla es 0.10 (un rescoldo tenue, como manda
            // el diagnóstico §0.2) y a raw 220 (~320°C, el caso que Cesar
            // vio) es 0.20 con un empujón rojo de +32: el azul sigue
            // leyéndose azul, pero caliente. Verificado con capturas en el
            // taller real, no calculado a ciegas.
            byte raw = _grid.temp[idx];
            if (raw > IncandInicioRaw)
            {
                float t01 = (raw - IncandInicioRaw) / (float)(255 - IncandInicioRaw);
                if (t01 > 1f) t01 = 1f;
                // Curva ~t^1.5 escrita con dos multiplicaciones (nada de Pow en
                // el hot path del refresco de textura). CALIBRADA MIRANDO, en
                // dos pasadas: la primera usó t*t y el matiz se salvaba, pero
                // en la banda 180-270°C el calor dejaba de NOTARSE -- cambiar
                // el bug de "miente" por el de "no avisa" no es arreglarlo.
                // Con esta curva, a 320°C la mezcla es 0.25 (se ve caliente de
                // lejos) y el azul sigue siendo azul.
                float suave = t01 * (0.45f + 0.55f * t01);

                // (1) brasa aditiva: conserva íntegras las diferencias entre materiales.
                r = ClampByte(r + (int)(IncandBrasaR * suave));
                g = ClampByte(g + (int)(IncandBrasaG * suave));
                b = ClampByte(b + (int)(IncandBrasaB * suave));

                // (2) mezcla hacia el ámbar, con techo: nunca borra el matiz.
                float mezcla = suave * IncandTechoMezcla;
                r = LerpByte(r, 255, mezcla);
                g = LerpByte(g, 214, mezcla);
                b = LerpByte(b, 150, mezcla);
            }

            // PÁTINA (playtest 39, contrato ENCARGO S 1d): última capa, encima
            // de todo lo demás -- es literalmente SUCIEDAD sobre la
            // superficie. Ver ActualizarPatinaFranja para quién la escribe;
            // aquí solo se LEE y se traduce a color. Un solo canal para dos
            // lecturas (decisión de S, ver el docblock de CellGrid.patina):
            // valores bajos = húmedo (oscurece un poco, tira a frío), valores
            // altos = tizne (oscurece mucho, tira a carbón).
            byte pat = _grid.patina[idx];
            if (pat > 0)
            {
                if (pat <= PatinaMojadoTecho)
                {
                    float t01 = (pat / (float)PatinaMojadoTecho) * 0.7f;
                    r = LerpByte(r, ClampByte(r - 35), t01);
                    g = LerpByte(g, ClampByte(g - 25), t01);
                    b = LerpByte(b, ClampByte(b + 10), t01);
                }
                else
                {
                    float t01 = (pat - PatinaMojadoTecho) / (float)(PatinaTizneTecho - PatinaMojadoTecho);
                    if (t01 > 1f) t01 = 1f;
                    r = LerpByte(r, 18, t01 * 0.75f);
                    g = LerpByte(g, 16, t01 * 0.75f);
                    b = LerpByte(b, 15, t01 * 0.75f);
                }
            }

            return new Color32(r, g, b, baseColor.a);
        }

        private static byte ClampByte(int v) => (byte)(v < 0 ? 0 : (v > 255 ? 255 : v));

        private static byte LerpByte(byte a, byte b, float t) => (byte)(a + (b - a) * t);

        private static uint Hash2D(int x, int y)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return h;
            }
        }

        private static uint Hash3D(int x, int y, int z)
        {
            unchecked
            {
                uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)z * 2147483647u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return h;
            }
        }

        // =====================================================================
        // FIRMA VISUAL: patrón morfológico -- despacho por familia (playtest 12)
        // =====================================================================
        // Aritmética entera pura, hashes estables, CERO Mathf.Sin/Pow/PerlinNoise
        // en el bucle (regla del encargo) -- la única curva suave que hace falta
        // (Pulso, Vetas) sale de SineTable256, construida UNA VEZ arriba, no en
        // cada celda.
        private static void ApplyPatron(int x, int y, byte morphVal, MaterialDef def, int tick, ref byte r, ref byte g, ref byte b)
        {
            switch (def.patron)
            {
                case PatronMorfologico.Vetas:
                    ApplyVetas(x, y, def, tick, ref r, ref g, ref b);
                    break;
                case PatronMorfologico.Celdas:
                    ApplyCeldas(x, y, def, tick, ref r, ref g, ref b);
                    break;
                case PatronMorfologico.Manchas:
                case PatronMorfologico.Laberinto:
                    // (playtest 20, CORRECCIÓN IMPORTANTE) Estas dos líneas decían
                    // que "la FORMA (puntos vs. bandas) ya la produce
                    // SimStepper.MorphReactionDiffusion en el propio campo morph".
                    // **ERA FALSO, y llevaba siéndolo desde el playtest 12.** El
                    // campo morph es de UN SOLO valor por celda, y una
                    // reacción-difusión biestable de un solo campo NO produce
                    // patrones de Turing: se homogeneiza siempre (engrosamiento
                    // tipo Allen-Cahn), así que en un charco acotado colapsaba a un
                    // tinte casi plano hiciera lo que hiciera `patronEscala`. Ni
                    // había puntos, ni había bandas, ni había diferencia entre
                    // Manchas y Laberinto: había un degradado.
                    //
                    // El playtest 20 arregló el COLAPSO (anclaje de ruido estático
                    // por bloque en MorphReactionDiffusion: ahora sí hay estructura
                    // visible, y estable, en un charco pequeño), pero NO la
                    // distinción entre las dos familias — hoy se separan por brillo
                    // medio (Manchas ~15-35/255 más oscura), no por forma. La
                    // diferencia de forma exige DOS campos (Gray-Scott de verdad,
                    // U y V), que es un cambio de `CellGrid` y está en el backlog.
                    // Aquí solo se traduce concentración a brillo, igual en ambas.
                    //
                    // (investigación playtest 13, revisada playtest 20) A diferencia
                    // de Vetas/Celdas, Manchas/Laberinto/Dendritas NO usan
                    // patronEscala como un "periodo en celdas" explícito: son un
                    // proceso de reacción-difusión (MorphReactionDiffusion) cuyo
                    // tamaño de rasgo emerge de feed=8+(fuerza>>4) [8..23] y
                    // diffDiv=max(4,20-escala*2) [4..18], y de la LONGITUD DE
                    // DIFUSIÓN del propio sistema -- NO del tamaño del recipiente.
                    // Medidas reales tras el playtest 19 (regla 24, "44x7"/"58x37",
                    // ver RecipienteMasEstrechoAncho arriba -- las cifras "46x6"/
                    // "52x37" de este comentario y de la regla 24 quedaron viejas
                    // en posición pero el orden de magnitud sigue siendo el mismo).
                    // Un patrón de Turing se auto-organiza en manchas/bandas que se
                    // repiten solas mientras el medio sea mayor que unas pocas veces
                    // su longitud característica; con diffDiv en el rango 4..18 el
                    // rasgo emergente es de un orden de magnitud por debajo del
                    // recipiente, así que YA se repite varias veces por construcción
                    // SIN remapeo -- validado esta ronda con una réplica en Python
                    // del propio Gray-Scott simplificado (ver el informe): a
                    // patronEscala bajo (diffDiv grande, ~18) Manchas/Laberinto se
                    // leen bien incluso en un charco de ~30 celdas. A patronEscala
                    // ALTO (diffDiv=4, el extremo de más difusión) la réplica en
                    // Python colapsó a un tinte casi plano dentro de una mancha
                    // AISLADA pequeña (sin masa vecina de la que arrastrar reactivo)
                    // -- no es una réplica bit-exacta (SimStepper corre sobre la
                    // grilla entera con vecinos de cualquier material, no sobre un
                    // parche recortado), así que esto NO es una confirmación de bug,
                    // pero sí una pista para quien toque SimStepper.cs a
                    // continuación: comprobar Manchas a patronEscala=8 en un charco
                    // pequeño y aislado de verdad, en el editor. Dendritas: longitud
                    // de rama = arranque(200..255) / decayStep(10+escala, 11..18)
                    // ≈ 14..23 celdas -- del mismo orden que el recipiente más
                    // pequeño, y con semillas deliberadamente raras (1 entre
                    // ~600..3000 por turno de celda) para que se lean como agujas
                    // aisladas, no una alfombra -- eso ya es "reconocible" sin
                    // necesitar repetición (una sola aguja se lee como Dendritas).
                    // Ninguna de las tres coincide con el reporte de Cesar de esta
                    // ronda (que señaló los frascos/redomas, resuelto en
                    // StorageRack.cs/FlaskHud.cs) ni es un archivo tocable aquí --
                    // se deja constancia en vez de tocar SimStepper.cs.
                    ApplyReactionDiffusion(morphVal, def, ref r, ref g, ref b);
                    break;
                case PatronMorfologico.Dendritas:
                    ApplyDendritas(morphVal, def, ref r, ref g, ref b);
                    break;
                case PatronMorfologico.Pulso:
                    ApplyPulso(morphVal, def, ref r, ref g, ref b);
                    break;
                case PatronMorfologico.Motas:
                    ApplyMotas(morphVal, def, ref r, ref g, ref b);
                    break;
                // Liso no llega aquí: el gate patronFuerza>0 en ComputeCellColor lo
                // descarta siempre (Universe.Create fuerza patronFuerza=0 cuando
                // patron==Liso).
            }
        }

        /// <summary>
        /// Periodo (en celdas) compartido por Vetas (banda senoidal) y Celdas
        /// (lado de tesela Voronoi) -- las DOS ÚNICAS familias que en SimRenderer
        /// dependen de patronEscala como un tamaño de rasgo explícito (Manchas/
        /// Laberinto/Dendritas son reacción-difusión/crecimiento en SimStepper.cs,
        /// que no es archivo modificable esta ronda; Pulso/Motas ni siquiera leen
        /// patronEscala en ApplyPatron, ver el switch de arriba).
        ///
        /// HISTORIA (por qué el número ha cambiado dos veces):
        ///  · Antes del playtest 13: 11+escala*3 (14..35 celdas) -- una
        ///    sobrecorrección contra el miedo a que un rasgo de 1-2 celdas se
        ///    leyera como ruido a 7.5px/celda de pantalla. Con periodo 14..35
        ///    hacía falta MEDIA PANTALLA de materia para ver una sola repetición
        ///    (regla 24: "un patrón se reconoce por su REPETICIÓN, no por su
        ///    tamaño") -- el jugador solo lo reconocía en masas enormes.
        ///  · Playtest 13: 4+escala (5..12 celdas), calibrado para 3-4
        ///    repeticiones mínimo en el recipiente más estrecho del taller
        ///    (bandeja fría). Corrección real, pero justa: en la escala más
        ///    gruesa (12) apenas llegaba al mínimo (46/12≈3.8 con la medida de
        ///    entonces) y en un charco pequeño (unas pocas decenas de celdas,
        ///    muy por debajo del recipiente entero) 12 celdas de periodo seguían
        ///    leyéndose como un borrón sin repetición -- exactamente lo que
        ///    Cesar reportó la noche antes de este playtest: *"aún siento que
        ///    necesito mucho material para ver las formas"*.
        ///  · Playtest 20 (esta ronda): 3+(escala-1)/2 (3..6 celdas). Mismo suelo
        ///    de "no confundir rasgo fino con ruido de un píxel" que ya validó el
        ///    playtest 13 (3 sigue siendo varias veces mayor que 1 téxel), techo
        ///    bajado de 12 a 6 -- la mitad. Verificado a OJO (no hay compilador
        ///    en este entorno) con una réplica en Python de esta misma aritmética
        ///    (hash/seno/Voronoi bit a bit iguales, ver el informe de la ronda):
        ///    en un charco de ~30 celdas la escala más gruesa pasó de "un borrón
        ///    sin repetición" a mostrar 1-2 repeticiones reales, y en el
        ///    recipiente más estrecho (<see cref="RecipienteMasEstrechoAncho"/>,
        ///    44 celdas, LEÍDO de SimLevelBuilder -- la cifra "46x6" que cita la
        ///    regla 24 quedó vieja tras el playtest 19, que movió bandeja/estante
        ///    sin cambiar su interior) pasa de 3.8-9.2 a 7.3-14.7 repeticiones:
        ///    más margen, no menos. La guarda de <see cref="RecipienteMasEstrechoAncho"/>
        ///    en Init() re-verifica esto en cada arranque, no solo en este
        ///    comentario.
        /// </summary>
        private static int PatronPeriodoCeldas(byte patronEscala) => 3 + (patronEscala - 1) / 2;

        /// <summary>
        /// Vetas: mármol veteado. PURAMENTE POSICIONAL (el contrato prohíbe que
        /// SimStepper toque morph aquí) -- se recalcula del todo cada vez que se
        /// pide, con (x, y, semillaPatron, patronEscala, tick). Técnica: bandas
        /// senoidales (SineTable256) DEFORMADAS por un campo de ruido de baja
        /// frecuencia (LatticeNoise, bilinear sobre una rejilla de hash) -- eso es
        /// literalmente "ruido deformado": sin el warp sería una franja recta y
        /// aburrida; con él, la franja serpentea como una veta mineral real.
        /// </summary>
        private static void ApplyVetas(int x, int y, MaterialDef def, int tick, ref byte r, ref byte g, ref byte b)
        {
            int veinScale = PatronPeriodoCeldas(def.patronEscala);

            int warp = LatticeNoise(x, y, veinScale, 220 + def.semillaPatron) - 128; // -128..127, deformación suave.

            // Orientación de la veta: variada por sustancia (semillaPatron), no
            // siempre vertical -- dos materiales con Vetas no deben calcarse.
            int tiltY = 1 + (def.semillaPatron % 3); // 1..3

            // Deriva de tiempo: Vetas es "quieto y mineral" por definición (ver el
            // enum en MaterialDef.cs) -- Universe.Create ya limita su ritmoAnim a
            // 0..20 (muy por debajo del resto de familias), así que aquí basta un
            // desplazamiento de fase minúsculo por tick; a ritmoAnim=20 tarda
            // minutos en dar una vuelta completa de fase -- un asentamiento apenas
            // perceptible, no una animación.
            int drift = def.ritmoAnim > 0 ? (int)(((uint)tick * (uint)def.ritmoAnim) >> 10) : 0;

            int stripePos = ((x * 4 + y * tiltY) * 256 / (veinScale * 4)) + warp * 3 + drift;
            int wave = SineTable256[stripePos & 0xFF]; // -127..127

            int amt = wave * def.patronFuerza / 255;
            ModulatePattern(ref r, ref g, ref b, amt);
        }

        /// <summary>
        /// Celdas: teselas tipo Voronoi con borde marcado (espuma, tejido
        /// celular). PURAMENTE POSICIONAL igual que Vetas -- feature points de un
        /// diagrama de Voronoi hasheados por rejilla (VoronoiEdge, 3x3 celdas
        /// vecinas) en vez de guardar nada en morph.
        /// </summary>
        private static void ApplyCeldas(int x, int y, MaterialDef def, int tick, ref byte r, ref byte g, ref byte b)
        {
            int cellSize = PatronPeriodoCeldas(def.patronEscala);

            // Deriva de las teselas si la sustancia fluye (Liquid/Gas -- StaticSolid
            // siempre trae ritmoAnim=0 por Universe.Create, así que este bloque es
            // un no-op automático para lo mineral): desplaza el MUESTREO en X, no
            // los feature points -- más barato y visualmente idéntico (todo el
            // campo de Voronoi se desliza como una balsa de espuma).
            int driftX = def.ritmoAnim > 0 ? (int)(((uint)tick * (uint)def.ritmoAnim) >> 9) : 0;
            int sx = x + driftX;

            int edgeDiff = VoronoiEdge(sx, y, cellSize, 230 + def.semillaPatron, out uint cellId);
            int edgeBand = cellSize * 2; // banda de borde ~2 celdas de ancho (ver la nota de unidades en VoronoiEdge).

            int amt;
            if (edgeDiff < edgeBand)
            {
                // Borde de tesela: oscurece, más cuanto más cerca de la costura.
                int t01 = edgeDiff * 100 / (edgeBand > 0 ? edgeBand : 1); // 0 (costura)..100 (ya interior)
                amt = -((100 - t01) * def.patronFuerza / 100);
            }
            else
            {
                // Interior: variación de tono estable POR TESELA (mismo lenguaje
                // que el hash de sillería de StaticSolid, pero por celda de
                // Voronoi en vez de por bloque rectangular).
                int tono = (int)(cellId % 41) - 20; // ±20
                amt = tono * def.patronFuerza / 255;
            }

            ModulatePattern(ref r, ref g, ref b, amt);
        }

        /// <summary>
        /// Manchas / Laberinto: morph = concentración de reacción-difusión
        /// (0..255, ver SimStepper.MorphReactionDiffusion). Se centra en 128 para
        /// que el punto medio del campo sea "color base sin modular" y los
        /// extremos aclaren/oscurezcan simétricamente -- así la mancha/banda se
        /// lee como relieve sobre el color, no como un tinte plano.
        /// </summary>
        private static void ApplyReactionDiffusion(byte morphVal, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            int centered = morphVal - 128; // -128..127
            int amt = centered * def.patronFuerza / 255;
            ModulatePattern(ref r, ref g, ref b, amt);
        }

        /// <summary>
        /// Dendritas: morph = fuerza de rama (0 = sin rama). A diferencia de
        /// Manchas/Laberinto NO se centra: v=0 debe devolver el color base
        /// intacto (no hay rama ahí, nada que dibujar) y v alto ilumina hacia
        /// arriba -- así se lee como aguja que brilla sobre el fondo, nunca como
        /// una sombra que "muerde" la sustancia entre ramas.
        /// </summary>
        private static void ApplyDendritas(byte morphVal, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            if (morphVal == 0) return;
            int amt = morphVal * def.patronFuerza / 255;
            ModulatePattern(ref r, ref g, ref b, amt);
        }

        /// <summary>
        /// Pulso: morph = fase 0..255 (SimStepper.MorphPulse). Curva suave vía
        /// SineTable256 -- "respira": aclara y oscurece simétricamente alrededor
        /// del color base según la fase, sin ningún Mathf.Sin en este bucle.
        /// </summary>
        private static void ApplyPulso(byte morphVal, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            int wave = SineTable256[morphVal]; // -127..127
            int amt = wave * def.patronFuerza / 255;
            ModulatePattern(ref r, ref g, ref b, amt);
        }

        /// <summary>
        /// Motas: morph = intensidad de chispa (0 = apagada, ver
        /// SimStepper.MorphSparkle). A propósito NO pasa por ModulatePattern: un
        /// destello debe leerse como luz que se AÑADE (hacia blanco-caliente), no
        /// como un tinte más saturado del propio color -- aditivo puro y solo
        /// hacia arriba, nunca resta brillo cuando está apagada.
        /// </summary>
        private static void ApplyMotas(byte morphVal, MaterialDef def, ref byte r, ref byte g, ref byte b)
        {
            if (morphVal == 0) return;
            int amt = morphVal * def.patronFuerza / 255;
            r = ClampByte(r + amt);
            g = ClampByte(g + amt);
            b = ClampByte(b + amt);
        }

        /// <summary>
        /// Aplica un desplazamiento de brillo con un empujón de saturación
        /// "gratis" cuando aclara (signedAmt&gt;0): aleja cada canal de la media de
        /// los tres, así el pico del patrón se lee más VIVO, no solo más claro
        /// (la regla de arte del encargo: "el patrón modula brillo y algo de
        /// saturación"). Al oscurecer (signedAmt&lt;0) no se toca la saturación --
        /// se lee como sombra/humedad del mismo color, no como un tono distinto.
        /// Compartida por Vetas, Celdas, Manchas/Laberinto, Dendritas, Pulso y el
        /// borde Halo: un único sitio donde ajustar el "carácter" del sistema
        /// entero si hace falta retocarlo.
        /// </summary>
        private static void ModulatePattern(ref byte r, ref byte g, ref byte b, int signedAmt)
        {
            int mean = (r + g + b) / 3;
            int sat = signedAmt > 0 ? signedAmt / 3 : 0;
            r = ClampByte(r + signedAmt + (r - mean) * sat / 128);
            g = ClampByte(g + signedAmt + (g - mean) * sat / 128);
            b = ClampByte(b + signedAmt + (b - mean) * sat / 128);
        }

        /// <summary>
        /// Ruido de valor (tipo Perlin barato) sobre una rejilla de hash con
        /// interpolación BILINEAR entera: sin la interpolación, cualquier escala
        /// pequeña se leería como estática pura (un hash por celda sin relación
        /// con sus vecinos). Con ella, el campo es continuo y liso entre nodos de
        /// la rejilla aunque la rejilla misma sea gruesa -- exactamente lo que
        /// hace falta para deformar una veta sin que parezca ruido de televisión.
        /// x,y siempre &gt;=0 en esta grilla (0..255, 0..143): la división entera
        /// trunca igual que un floor, sin casos especiales de signo.
        /// </summary>
        private static int LatticeNoise(int x, int y, int scale, int salt)
        {
            if (scale < 1) scale = 1;
            int gx = x / scale, gy = y / scale;
            int fx = x - gx * scale; // 0..scale-1
            int fy = y - gy * scale;

            int h00 = (int)(Hash3D(gx, gy, salt) & 0xFF);
            int h10 = (int)(Hash3D(gx + 1, gy, salt) & 0xFF);
            int h01 = (int)(Hash3D(gx, gy + 1, salt) & 0xFF);
            int h11 = (int)(Hash3D(gx + 1, gy + 1, salt) & 0xFF);

            int tx = scale > 1 ? fx * 256 / scale : 0;
            int ty = scale > 1 ? fy * 256 / scale : 0;

            int top = h00 + ((h10 - h00) * tx >> 8);
            int bot = h01 + ((h11 - h01) * tx >> 8);
            return top + ((bot - top) * ty >> 8); // 0..255 aprox.
        }

        /// <summary>
        /// Distancia (al cuadrado) a la tesela más cercana MENOS la distancia a la
        /// segunda más cercana, de un diagrama de Voronoi barato: 9 puntos
        /// candidatos (la celda de rejilla de (x,y) y sus 8 vecinas), cada uno
        /// hasheado a una posición fija dentro de su celda (jitter de rejilla, la
        /// técnica estándar para que el resultado no se vea como una rejilla
        /// cuadrada disfrazada). El valor devuelto es pequeño cerca de una costura
        /// entre dos teselas (las dos distancias casi empatan) y grande en el
        /// centro de una tesela (la más cercana gana con claridad) -- eso es
        /// literalmente "borde marcado" sin dibujar ninguna línea aparte.
        /// cellId identifica la tesela ganadora (para un tono estable por tesela).
        /// </summary>
        private static int VoronoiEdge(int x, int y, int cellSize, int salt, out uint cellId)
        {
            int gx = x / cellSize, gy = y / cellSize;
            int best = int.MaxValue, second = int.MaxValue;
            uint bestId = 0;
            for (int oy = -1; oy <= 1; oy++)
            {
                int cyg = gy + oy;
                for (int ox = -1; ox <= 1; ox++)
                {
                    int cxg = gx + ox;
                    uint hh = Hash3D(cxg, cyg, salt);
                    int jx = (int)(hh & 0xFF) * cellSize / 256;
                    int jy = (int)((hh >> 8) & 0xFF) * cellSize / 256;
                    int fpx = cxg * cellSize + jx;
                    int fpy = cyg * cellSize + jy;
                    int dx = x - fpx, dy = y - fpy;
                    int d2 = dx * dx + dy * dy;
                    if (d2 < best)
                    {
                        second = best;
                        best = d2;
                        bestId = hh;
                    }
                    else if (d2 < second)
                    {
                        second = d2;
                    }
                }
            }
            cellId = bestId;
            return second - best;
        }
    }
}
