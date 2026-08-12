using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// La Tolva del Maestro: el pozo excavado en el contrafuerte de piedra del
    /// muro derecho (su geometría vive en Sim/SimLevelBuilder.BuildDeliveryNiche,
    /// que es la única fuente de verdad de dónde está la boca). Solo se
    /// consume en el FONDO del pozo (ver <see cref="ZoneFloorY1"/>) y se
    /// evalúa contra los encargos activos de <see cref="OrderSystem"/>; lo que
    /// no encaja cuenta como "chatarra" y da 1 de Favor cada
    /// <see cref="ScrapPerFavor"/> celdas, para que experimentar nunca sea del
    /// todo inútil.
    ///
    /// REDISEÑO VISUAL (playtest 6: "la tolva no se ve muy bien, el cuadro
    /// amarillo con flecha no dice 'tolva', no hay animación de volcado y el
    /// rótulo está todo el rato en pantalla"). Tres cambios de fondo:
    ///  1. Ya NO hay jambas+labio+flecha-marco: hay un EMBUDO METÁLICO de latón
    ///     (ancho arriba, garganta abajo, remachado, con la cara interior
    ///     degradada a negro) generado por código en
    ///     <see cref="SpriteEmbudoMetal"/>. Es la silueta lo que dice "tolva",
    ///     no un tinte amarillo.
    ///  2. El rótulo "TOLVA DEL MAESTRO" solo aparece por CERCANÍA (ver
    ///     UiStyles.Cercania/PlacaMundo) y solo hasta que el jugador se ha
    ///     acercado una vez (<see cref="_yaConocida"/>): después nunca más.
    ///     "vierte AQUÍ" desaparece como texto; sobrevive como una flecha que
    ///     solo se enciende si además el frasco lleva algo (Flask.Total).
    ///  3. Cada trago dispara ~0.55s de animación física (sacudida del embudo,
    ///     garganta que se enciende en verde/ámbar, anillo de onda que se
    ///     expande) en vez del pulso de alfa infinito de antes.
    ///
    /// FIX PLAYTEST 7 ("no sé si está de cabeza"/"el contenido desaparece"):
    ///  1. El embudo SE RENDERIZABA INVERTIDO: en un Texture2D de Unity la
    ///     fila y=0 es la de ABAJO del sprite, y el bucle de
    ///     <see cref="SpriteEmbudoMetal"/> pintaba el ancho de las ALAS en
    ///     y=0 (pegado al labio del pozo, por el pivote) y la GARGANTA
    ///     estrecha en y=h-1 (flotando arriba) — justo al revés de un embudo.
    ///     Se invirtió la interpolación (ver el comentario en el bucle) y se
    ///     movieron labio y remaches al borde ancho (fila alta), que es el de
    ///     verdad arriba.
    ///  2. La garganta dibujada (antes 58% del ancho del pozo) no cubría la
    ///     boca real excavada en SimLevelBuilder: se ensanchó a
    ///     FactorGarganta=1.0 para que case exactamente.
    ///  3. Se añadió <see cref="SpriteConductoInterior"/> (fondo oscuro
    ///     degradado + costillas, sortingOrder -7) para que el pozo se lea
    ///     como el interior de un conducto, y ConsumeTick ahora solo traga en
    ///     el fondo (<see cref="ZoneFloorY1"/>) para que lo vertido CAIGA
    ///     visiblemente antes de desaparecer.
    ///
    /// LIMITACIÓN: lee _sim.Grid.temp[] directamente para evaluar los encargos
    /// Hot/Cold (mismo patrón que HeatPlate/ChillStone).
    /// TODO(ChaosAlchemy): canalizar por una API de lectura del sim.
    ///
    /// NOTA: Init() no recibe al aprendiz (la firma la fija
    /// AlkahestGameBootstrap, que no se toca en este pase), así que la
    /// referencia se busca una vez con FindAnyObjectByType y se cachea — mismo
    /// patrón defensivo que ya usa el propio bootstrap mientras espera a que
    /// exista AlkahestSim.
    /// </summary>
    public sealed class DeliveryChute : MonoBehaviour
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        private const int ScrapPerFavor = 10;

        /// <summary>Cuánto dura en pantalla el texto de resultado ("¡ENTREGA ACEPTADA!" / chatarra). Subido de 0.5 a 1.1s (playtest 6): antes no daba tiempo a leerlo.</summary>
        private const float FlashSeconds = 1.1f;
        /// <summary>Duración de la animación FÍSICA de volcado (sacudida + garganta + onda). Deliberadamente más corta que FlashSeconds: es un golpe seco, no un cartel.</summary>
        private const float AnimSeconds = 0.55f;
        private const float HundimientoMax = 0.06f;

        private const float RangoPleno = 3.0f;
        private const float RangoDesvanece = 4.2f;

        // Geometría de la boca, tomada del constructor de nivel (nunca duplicada aquí).
        private const int ZoneX0 = SimLevelBuilder.ChuteMouthX0;
        private const int ZoneX1 = SimLevelBuilder.ChuteMouthX1;
        private const int ZoneY0 = SimLevelBuilder.ChuteMouthY0;
        private const int ZoneY1 = SimLevelBuilder.ChuteMouthY1;

        /// <summary>
        /// (fix playtest 7) Última fila que SÍ consume: solo el FONDO del
        /// pozo (las <see cref="SimLevelBuilder.ChuteSillRows"/> filas más
        /// bajas), no el pozo entero. El resto del hueco (hasta ZoneY1) queda
        /// como aire: lo que se vierte cae de verdad por gravedad a través de
        /// él antes de desaparecer, en vez de evaporarse pegado al labio.
        /// </summary>
        private const int ZoneFloorY1 = SimLevelBuilder.ChuteMouthY0 + SimLevelBuilder.ChuteSillRows - 1;

        private static readonly Color ColorGargantaReposo = new Color(0.035f, 0.028f, 0.045f, 0.95f);

        private AlkahestSim _sim;
        private OrderSystem _orderSystem;
        private ApprenticeController _apprentice;
        private Flask _flask;

        private float _accumulator;
        private int _scrap;

        // --- Visual: embudo (sacudible) + garganta + sombra + anillo + flecha ---
        private Transform _embudoTr;
        private SpriteRenderer _garganta;
        private SpriteRenderer _anillo;
        private Transform _anilloTr;
        private float _anilloEscalaInicial;
        private float _anilloEscalaFinal;
        private SpriteRenderer _flecha;
        private Transform _flechaTr;
        private float _flechaYBase;

        private bool _yaConocida;
        private float _cercaniaLabio;

        private float _flashHasta;
        private bool _flashAceptado;

        private float _animInicio = -10f;
        private bool _animAceptado;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, OrderSystem orderSystem)
        {
            _sim = sim;
            _orderSystem = orderSystem;

            // El transform se ancla al CENTRO DEL LABIO de la boca: el embudo
            // apoya justo ahí la garganta (ver BuildVisual), y es el punto al
            // que se ancla el rótulo por proximidad.
            transform.position = new Vector3(
                (ZoneX0 + ZoneX1 + 1) * 0.5f * SimRenderer.CellWorldSize,
                (ZoneY1 + 1) * SimRenderer.CellWorldSize,
                0f);

            BuildVisual();
        }

        // -----------------------------------------------------------------
        // Visual: embudo de latón generado por código, sin assets.
        // -----------------------------------------------------------------
        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;
            float bocaIzq = ZoneX0 * celda;
            float bocaDer = (ZoneX1 + 1) * celda;
            float anchoBoca = bocaDer - bocaIzq;

            // --- INTERIOR DEL POZO (fix playtest 7, punto 3: "el contenido
            // desaparece"): fondo oscuro degradado + costillas tenues, para
            // que el hueco se lea como el CONDUCTO de la tolva y no como una
            // ventana transparente al fondo del taller. Va DETRÁS de todo lo
            // demás de este componente (sortingOrder -7): por debajo del
            // sprite de la simulación (-5, así lo que cae por dentro se ve
            // por encima de este fondo) y por encima del fondo del taller
            // (-10). Cuelga desde el labio (mismo anchor que el resto) hacia
            // abajo, cubriendo exactamente la profundidad del pozo excavado
            // en SimLevelBuilder.BuildDeliveryNiche -> nunca duplicamos su
            // altura, se deriva de ZoneY0/ZoneY1.
            float altoPozoMundo = (ZoneY1 - ZoneY0 + 1) * celda;
            var pozoGO = new GameObject("PozoInterior");
            pozoGO.transform.SetParent(transform, false);
            pozoGO.transform.localPosition = Vector3.zero; // pivote (0.5,1) ya cae en el labio.
            var pozoSr = pozoGO.AddComponent<SpriteRenderer>();
            pozoSr.sprite = SpriteConductoInterior(anchoBoca, altoPozoMundo);
            pozoSr.sortingOrder = -7;

            // --- EL EMBUDO: metal + garganta, agrupados para que la sacudida
            // de la entrega mueva las dos piezas juntas sin tocar sombra,
            // anillo ni flecha (esos no "son" el metal, no deben saltar). ---
            var embudoGO = new GameObject("EmbudoRoot");
            embudoGO.transform.SetParent(transform, false);
            _embudoTr = embudoGO.transform;
            _embudoTr.localPosition = Vector3.zero;

            Sprite spriteMetal = SpriteEmbudoMetal(anchoBoca,
                out float anchoAlasMundo, out float altoEmbudoMundo, out float anchoGargantaMundo);

            var metalGO = new GameObject("EmbudoMetal");
            metalGO.transform.SetParent(_embudoTr, false);
            // Pivote del sprite en su base (garganta): con localPosition cero
            // la garganta queda EXACTAMENTE sobre el labio y las alas
            // sobresalen hacia arriba, como un ala de recogida.
            metalGO.transform.localPosition = Vector3.zero;
            var metalSr = metalGO.AddComponent<SpriteRenderer>();
            metalSr.sprite = spriteMetal;
            metalSr.sortingOrder = 20;

            // Garganta: mancha oscura (se enciende verde/ámbar al tragar) justo
            // en el punto más hondo de la silueta, sobre el metal (orden mayor
            // para que se vea "encenderse dentro" del hueco).
            var gargantaGO = new GameObject("Garganta");
            gargantaGO.transform.SetParent(_embudoTr, false);
            gargantaGO.transform.localPosition = new Vector3(0f, altoEmbudoMundo * 0.16f, -0.01f);
            gargantaGO.transform.localScale = new Vector3(anchoGargantaMundo * 0.85f, anchoGargantaMundo * 0.5f, 1f);
            _garganta = gargantaGO.AddComponent<SpriteRenderer>();
            _garganta.sprite = SpriteBlobSuave();
            _garganta.sortingOrder = 21;
            _garganta.color = ColorGargantaReposo;

            // Sombra proyectada del ala sobre la piedra: fija (no se sacude —
            // un charco de sombra no bota con el golpe del metal), para que el
            // embudo no "flote" sobre el contrafuerte.
            var sombraGO = new GameObject("Sombra");
            sombraGO.transform.SetParent(transform, false);
            sombraGO.transform.localPosition = new Vector3(0.05f, -0.05f, 0.02f);
            sombraGO.transform.localScale = new Vector3(anchoAlasMundo * 0.9f, anchoAlasMundo * 0.24f, 1f);
            var sombraSr = sombraGO.AddComponent<SpriteRenderer>();
            sombraSr.sprite = SpriteBlobSuave();
            sombraSr.sortingOrder = 14;
            sombraSr.color = new Color(0f, 0f, 0f, 0.42f);

            // Anillo de onda: creado UNA vez aquí; en Update solo se anima
            // escala + alfa (cero asignaciones en el hot path).
            var anilloGO = new GameObject("Anillo");
            anilloGO.transform.SetParent(transform, false);
            anilloGO.transform.localPosition = new Vector3(0f, 0f, -0.02f);
            _anilloTr = anilloGO.transform;
            _anillo = anilloGO.AddComponent<SpriteRenderer>();
            _anillo.sprite = SpriteAnillo();
            _anillo.sortingOrder = 22;
            _anillo.color = new Color(0f, 0f, 0f, 0f);
            _anilloEscalaInicial = Mathf.Max(0.05f, anchoGargantaMundo * 1.1f);
            _anilloEscalaFinal = anchoAlasMundo * 2.0f;
            _anilloTr.localScale = new Vector3(_anilloEscalaInicial, _anilloEscalaInicial, 1f);

            // Flecha: invitación condicional (playtest 6: ya no es un marco
            // permanente). Arranca invisible; ActualizarProximidad decide.
            var flechaGO = new GameObject("Flecha");
            flechaGO.transform.SetParent(transform, false);
            _flechaTr = flechaGO.transform;
            _flechaYBase = altoEmbudoMundo + 0.28f;
            _flechaTr.localPosition = new Vector3(0f, _flechaYBase, 0f);
            _flecha = flechaGO.AddComponent<SpriteRenderer>();
            _flecha.sprite = SpriteFlecha(0.85f);
            _flecha.sortingOrder = 23;
            _flecha.color = new Color(1f, 1f, 1f, 0f);
        }

        // -----------------------------------------------------------------
        // Generación de sprites (todo por código, cero assets).
        // -----------------------------------------------------------------

        /// <summary>
        /// Embudo metálico: ANCHO ARRIBA (ala de recogida) y ESTRECHO ABAJO
        /// (garganta), con labio iluminado, remaches, banda de refuerzo y cara
        /// interior en degradado hacia negro. Resolución: TexelsPorCelda(8)
        /// por celda de simulación (celda = 0.1u) -&gt; muy por encima del
        /// mínimo de 6 que pide el diseño, para que se vea nítido con la
        /// cámara pegada.
        ///
        /// (fix playtest 7, punto 2): la garganta ahora mide EXACTAMENTE
        /// anchoBocaMundo (FactorGarganta=1) en vez de un 58% de ese ancho:
        /// antes la garganta dibujada era mucho más estrecha que la boca real
        /// del pozo (Sim/SimLevelBuilder.ChuteMouthX0..X1), así que el embudo
        /// no tapaba los cantos del pozo y se veía "descuadrado". Con la
        /// garganta = ancho del pozo, el pivote (base del sprite) casa px a
        /// px con el labio de la excavación; el ala (FactorAlas=1.32) sigue
        /// sobresaliendo a los lados, sobre la piedra del contrafuerte.
        /// </summary>
        private static Sprite SpriteEmbudoMetal(float anchoBocaMundo,
            out float anchoAlasMundo, out float altoEmbudoMundo, out float anchoGargantaMundo)
        {
            const float FactorAlas = 1.32f;
            const float FactorGarganta = 1.00f;
            const float FactorAlto = 0.60f;
            const int TexelsPorCelda = 8;

            float celda = SimRenderer.CellWorldSize;
            anchoAlasMundo = anchoBocaMundo * FactorAlas;
            anchoGargantaMundo = anchoBocaMundo * FactorGarganta;
            altoEmbudoMundo = anchoBocaMundo * FactorAlto;

            float ppu = TexelsPorCelda / celda; // texeles por unidad de mundo (80 con los valores por defecto).
            int w = Mathf.Max(16, Mathf.RoundToInt(anchoAlasMundo * ppu));
            int h = Mathf.Max(16, Mathf.RoundToInt(altoEmbudoMundo * ppu));

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "ChaosAlchemyChuteFunnelTex",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[w * h];

            // Paleta de LATÓN (nunca UiStyles.Oro puro como relleno: el oro es
            // el color de la interfaz, no del mundo). El destello del labio sí
            // usa un dorado cálido, como brillo especular sobre el metal.
            Color32 latonSombra = new Color32(86, 62, 28, 255);
            Color32 latonMedio = new Color32(168, 126, 58, 255);
            Color32 latonLuz = new Color32(214, 176, 96, 255);
            Color32 destelloLabio = new Color32(255, 219, 145, 255);
            Color32 negroGarganta = new Color32(9, 7, 11, 255);

            int grosorPared = Mathf.Max(3, Mathf.RoundToInt(w * 0.05f));
            int altoLabio = Mathf.Max(2, Mathf.RoundToInt(h * 0.11f));
            int bandaY = Mathf.RoundToInt(h * 0.50f);
            int bandaAlto = Mathf.Max(2, Mathf.RoundToInt(h * 0.045f));

            float mitadAlas = w * 0.5f;
            float mitadGarganta = mitadAlas * (anchoGargantaMundo / anchoAlasMundo);

            for (int y = 0; y < h; y++)
            {
                // (fix playtest 7, punto 1) EN UN Texture2D DE UNITY, LA FILA
                // y=0 ES LA DE ABAJO del sprite renderizado, no la de arriba
                // (el jugador reportó el embudo "de cabeza": esta fila era
                // justo la causa). El pivote del sprite está en la BASE
                // (Vector2(0.5,0), ver el Sprite.Create de más abajo) y esa
                // base se ancla al labio del pozo (ver BuildVisual). Antes
                // "t = y/(h-1)" hacía que y=0 (la fila de ABAJO, pegada al
                // labio) llevara el ANCHO de las alas y y=h-1 (la fila de
                // ARRIBA) llevara la garganta estrecha: el resultado era un
                // embudo con la boca ancha apoyada en el pozo y la garganta
                // flotando estrecha por encima — literalmente al revés.
                // Invirtiendo t, y=0 (abajo, en el labio) lleva la GARGANTA
                // estrecha y y=h-1 (arriba) lleva las ALAS anchas: ahora el
                // embudo recoge ancho por arriba y vacía estrecho por abajo,
                // como cualquier embudo real.
                float t = 1f - y / (float)(h - 1);
                float mitad = Mathf.Lerp(mitadAlas, mitadGarganta, t);

                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - w * 0.5f);
                    if (dx > mitad) continue; // fuera de la silueta: transparente.

                    float profundidadBorde = mitad - dx; // 0 en el canto exterior, crece hacia el centro.
                    Color32 c;

                    if (y >= h - altoLabio)
                    {
                        // Labio SUPERIOR (fila alta de la textura = borde
                        // ancho del embudo, fix playtest 7): canto iluminado,
                        // la primera señal de "esto es metal, y es la boca de
                        // recogida" (playtest 6: "no parece tolva").
                        c = profundidadBorde < grosorPared * 0.7f ? destelloLabio : latonLuz;
                    }
                    else if (profundidadBorde < grosorPared)
                    {
                        // Pared visible del embudo: se ensombrece según baja.
                        c = Color32.Lerp(latonMedio, latonSombra, Mathf.Clamp01(t * 0.7f));
                    }
                    else
                    {
                        // Cara interior EN SOMBRA -> degradado hacia negro
                        // conforme baja y se acerca al centro: es lo que dice
                        // "aquí traga algo" sin necesidad de ningún texto.
                        float hueco = Mathf.Clamp01(t * 1.1f + (1f - dx / mitad) * 0.4f);
                        c = Color32.Lerp(latonSombra, negroGarganta, hueco);
                    }

                    // Banda de refuerzo horizontal a media altura.
                    if (y >= bandaY && y < bandaY + bandaAlto && dx < mitad - 1f)
                    {
                        c = Color32.Lerp(c, latonLuz, 0.5f);
                    }

                    px[y * w + x] = c;
                }
            }

            // Remaches: una fila en el labio (fix playtest 7: ahora cerca de
            // y=h-1, la fila ancha de arriba, no de y=0) y otra en la banda de
            // refuerzo. Es lo que convierte "trapecio de color" en "objeto
            // fabricado".
            int radioRemache = Mathf.Max(1, grosorPared / 3);
            int pasoRemache = Mathf.Max(8, w / 14);
            MarcarFilaRemaches(px, w, h, h - 1 - altoLabio / 2, pasoRemache, radioRemache, mitadAlas, mitadGarganta, latonSombra);
            MarcarFilaRemaches(px, w, h, bandaY + bandaAlto / 2, pasoRemache, radioRemache, mitadAlas, mitadGarganta, latonSombra);

            tex.SetPixels32(px);
            tex.Apply(false, false);

            // Pivote en la BASE (garganta, fila y=0 tras el fix del punto 1):
            // así el objeto se coloca por el punto exacto donde encaja con el
            // labio de la boca del pozo.
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0f), ppu);
        }

        private static void MarcarFilaRemaches(Color32[] px, int w, int h, int filaY, int paso, int radio,
            float mitadAlas, float mitadGarganta, Color32 color)
        {
            if (filaY < 0 || filaY >= h) return;
            // Misma inversión que en el bucle principal (fix playtest 7,
            // punto 1): t=0 en la fila de ABAJO (garganta), t=1 en la de
            // ARRIBA (alas), para que el ancho del remache case con el
            // contorno real del embudo en esa fila.
            float t = 1f - filaY / (float)(h - 1);
            float mitadFila = Mathf.Lerp(mitadAlas, mitadGarganta, t);

            for (int cx = paso / 2; cx < w; cx += paso)
            {
                float dxc = Mathf.Abs(cx + 0.5f - w * 0.5f);
                if (dxc > mitadFila - radio - 1f) continue; // que el remache quepa dentro de la silueta.

                for (int oy = -radio; oy <= radio; oy++)
                {
                    int y = filaY + oy;
                    if (y < 0 || y >= h) continue;
                    for (int ox = -radio; ox <= radio; ox++)
                    {
                        if (ox * ox + oy * oy > radio * radio) continue;
                        int x = cx + ox;
                        if (x < 0 || x >= w) continue;
                        px[y * w + x] = color;
                    }
                }
            }
        }

        /// <summary>
        /// (fix playtest 7, punto 3) Interior del pozo: degradado vertical de
        /// gris muy oscuro (arriba, cerca del labio, algo de luz ambiente
        /// cuela) a negro (abajo, donde traga el fondo) más un par de
        /// costillas/aros metálicos tenues, para que el hueco excavado en
        /// SimLevelBuilder se lea como el CONDUCTO de una tolva y no como un
        /// boquete transparente sobre el fondo del taller. Misma resolución
        /// que el embudo (8 téxeles/celda) y mismo FilterMode.Point.
        /// </summary>
        private static Sprite SpriteConductoInterior(float anchoMundo, float altoMundo)
        {
            const int TexelsPorCelda = 8;
            float celda = SimRenderer.CellWorldSize;
            float ppu = TexelsPorCelda / celda;
            int w = Mathf.Max(8, Mathf.RoundToInt(anchoMundo * ppu));
            int h = Mathf.Max(8, Mathf.RoundToInt(altoMundo * ppu));

            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "ChaosAlchemyChuteWellTex",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[w * h];

            Color32 negroFondo = new Color32(4, 3, 5, 255);
            Color32 grisOscuro = new Color32(46, 42, 50, 255);
            Color32 costilla = new Color32(70, 64, 72, 255);

            // Dos costillas a alturas fijas (fracción de la altura). Grosor
            // mínimo 1 téxel para que no desaparezcan por redondeo en pozos
            // bajos.
            int costilla1Y = Mathf.RoundToInt(h * 0.62f);
            int costilla2Y = Mathf.RoundToInt(h * 0.30f);
            int grosorCostilla = Mathf.Max(1, Mathf.RoundToInt(h * 0.018f));

            for (int y = 0; y < h; y++)
            {
                // y=0 es la fila de ABAJO de la textura (el fondo del pozo,
                // donde se traga el material -> más oscura); y=h-1 es la fila
                // de ARRIBA (justo bajo el labio, algo de luz ambiente cuela
                // -> gris oscuro, nunca negro puro). Sin inversión: aquí el
                // degradado ya nace correcto porque "más alto en textura" y
                // "más arriba en el mundo" son la misma dirección (no hay
                // pivote invertido como en el embudo).
                float t = y / (float)(h - 1);
                Color32 fila = Color32.Lerp(negroFondo, grisOscuro, t);

                bool enCostilla = Mathf.Abs(y - costilla1Y) <= grosorCostilla || Mathf.Abs(y - costilla2Y) <= grosorCostilla;
                if (enCostilla) fila = Color32.Lerp(fila, costilla, 0.55f);

                for (int x = 0; x < w; x++)
                {
                    px[y * w + x] = fila;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, false);

            // Pivote arriba-centro: se ancla al mismo punto que la garganta
            // del embudo (transform.position = centro del labio) y cuelga
            // hacia abajo cubriendo exactamente la profundidad del pozo
            // (ZoneY0..ZoneY1 en SimLevelBuilder, nunca duplicada aquí).
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 1f), ppu);
        }

        /// <summary>Mancha blanda circular (alfa con caída suave hacia el borde): base reutilizable para la garganta y la sombra, tintadas luego vía SpriteRenderer.color.</summary>
        private static Sprite SpriteBlobSuave()
        {
            const int w = 32, h = 32;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "ChaosAlchemyChuteBlobTex",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = (y + 0.5f) / h * 2f - 1f;
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w * 2f - 1f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01(1f - d);
                    a *= a; // caída suave hacia el borde.
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            // ppu = w -> el sprite mide 1x1 unidad a escala 1: localScale pasa
            // a representar directamente el tamaño en mundo (ver usos).
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        /// <summary>Aro delgado con caída suave a ambos lados: nace en el labio y se expande perdiendo opacidad al tragar algo.</summary>
        private static Sprite SpriteAnillo()
        {
            const int w = 48, h = 48;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "ChaosAlchemyChuteRingTex",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[w * h];
            float rExt = w * 0.5f - 1f;
            float rInt = rExt * 0.72f;
            float rMid = (rExt + rInt) * 0.5f;
            float banda = (rExt - rInt) * 0.5f;
            for (int y = 0; y < h; y++)
            {
                float ny = y + 0.5f - h * 0.5f;
                for (int x = 0; x < w; x++)
                {
                    float nx = x + 0.5f - w * 0.5f;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01(1f - Mathf.Abs(d - rMid) / banda);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w);
        }

        /// <summary>Triángulo apuntando hacia ABAJO, dibujado a mano (sin assets).</summary>
        private static Sprite SpriteFlecha(float anchoMundo)
        {
            const int w = 24, h = 18;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "ChaosAlchemyChuteArrowTex",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                // y = 0 es la punta (abajo), y = h-1 la base (arriba).
                float mitad = (y / (float)(h - 1)) * (w * 0.5f);
                for (int x = 0; x < w; x++)
                {
                    bool dentro = Mathf.Abs(x + 0.5f - w * 0.5f) <= mitad;
                    px[y * w + x] = dentro ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);

            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w / anchoMundo);
        }

        // -----------------------------------------------------------------
        // Lógica
        // -----------------------------------------------------------------
        private void Update()
        {
            if (_sim == null || _sim.Grid == null || _orderSystem == null) return;
            if (DayCycle.InputLocked) return;

            AsegurarJugador();

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                ConsumeTick();
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            AnimarEmbudo();
        }

        /// <summary>
        /// Busca al aprendiz UNA sola vez (Init no lo recibe: ver doc de la
        /// clase) y cachea también su Flask, que vive en el mismo GameObject.
        /// Sin coste una vez encontrado.
        /// </summary>
        private void AsegurarJugador()
        {
            if (_apprentice != null) return;
            _apprentice = FindAnyObjectByType<ApprenticeController>();
            if (_apprentice != null) _flask = _apprentice.GetComponent<Flask>();
        }

        private static float EaseOutQuad(float x)
        {
            float inv = 1f - x;
            return 1f - inv * inv;
        }

        private void AnimarEmbudo()
        {
            float t = Time.time;
            float u = (t - _animInicio) / AnimSeconds;
            bool animando = u >= 0f && u < 1f;
            u = Mathf.Clamp01(u);

            // --- Sacudida: una sola vez por entrega, ease-out (baja rápido,
            // vuelve despacio), nunca un seno infinito. ---
            float hundimiento = 0f;
            if (animando)
            {
                const float fraccionBajada = 0.30f;
                if (u < fraccionBajada)
                {
                    hundimiento = -HundimientoMax * EaseOutQuad(u / fraccionBajada);
                }
                else
                {
                    float us = (u - fraccionBajada) / (1f - fraccionBajada);
                    hundimiento = -HundimientoMax * (1f - EaseOutQuad(us));
                }
            }
            if (_embudoTr != null)
            {
                Vector3 p = _embudoTr.localPosition;
                p.y = hundimiento;
                _embudoTr.localPosition = p;
            }

            // --- Garganta: se enciende desde el reposo hacia verde (encajó) o
            // ámbar (chatarra) y se apaga; un único pulso ligado a la entrega,
            // NO un pulso permanente (eso es justo lo que se quejó el jugador). ---
            if (_garganta != null)
            {
                float intensidad = animando ? Mathf.Sin(u * Mathf.PI) : 0f;
                Color destino = _animAceptado ? UiStyles.Exito : UiStyles.Aviso;
                _garganta.color = Color.Lerp(ColorGargantaReposo, destino, intensidad);
            }

            // --- Anillo de onda: nace en el labio y se expande perdiendo
            // opacidad; invisible fuera de la ventana de animación. ---
            if (_anillo != null)
            {
                if (animando)
                {
                    float e = EaseOutQuad(u);
                    float escala = Mathf.Lerp(_anilloEscalaInicial, _anilloEscalaFinal, e);
                    _anilloTr.localScale = new Vector3(escala, escala, 1f);
                    Color destino = _animAceptado ? UiStyles.Exito : UiStyles.Aviso;
                    _anillo.color = new Color(destino.r, destino.g, destino.b, (1f - u) * 0.85f);
                }
                else
                {
                    _anillo.color = new Color(0f, 0f, 0f, 0f);
                }
            }

            ActualizarProximidad();
        }

        /// <summary>
        /// Rótulo por cercanía y flecha condicional (playtest 6). La curva de
        /// cercanía compartida (UiStyles.Cercania) decide cuánto se ve el
        /// rótulo; la flecha además exige que el frasco lleve algo
        /// (Flask.Total &gt; 0, la única API pública que expone contenido).
        /// </summary>
        private void ActualizarProximidad()
        {
            Transform jugadorTr = _apprentice != null ? _apprentice.transform : null;
            float cerc = UiStyles.Cercania(transform.position, jugadorTr, RangoPleno, RangoDesvanece);
            if (cerc >= 0.98f) _yaConocida = true;
            _cercaniaLabio = cerc;

            bool tieneContenido = _flask != null && _flask.Total > 0;
            float alfaFlecha = tieneContenido ? cerc : 0f;

            if (_flechaTr != null)
            {
                Vector3 p = _flechaTr.localPosition;
                p.y = _flechaYBase + (alfaFlecha > 0.02f ? Mathf.Sin(Time.time * 2.6f) * 0.08f : 0f);
                _flechaTr.localPosition = p;
            }
            if (_flecha != null)
            {
                Color oro = UiStyles.Oro;
                _flecha.color = new Color(oro.r, oro.g, oro.b, alfaFlecha);
            }
        }

        private void ConsumeTick()
        {
            // (fix playtest 7, punto 3) Solo se consume en las filas del
            // FONDO del pozo (ZoneY0..ZoneFloorY1, ver la constante para el
            // porqué): antes se barría TODO el pozo (hasta ZoneY1, el labio)
            // y cualquier cosa se tragaba el mismo tick en que entraba, así
            // que el material se evaporaba pegado a la boca y el resto del
            // hueco -visualmente ya vestido de conducto por SpriteConductoInterior-
            // nunca recibía nada que cayera por dentro. Ahora lo vertido cae
            // por gravedad (regla normal de la sim) a través del aire del
            // pozo antes de tragarse, así que SE VE caer.
            for (int x = ZoneX0; x <= ZoneX1; x++)
            {
                for (int y = ZoneY0; y <= ZoneFloorY1; y++)
                {
                    byte matId = (byte)_sim.SampleMaterial(x, y);
                    if (matId == MaterialId.Empty) continue;

                    // Solo la PIEDRA se ignora (es el propio nicho). Antes se
                    // ignoraba todo sólido estático, lo que hacía IMPOSIBLE
                    // entregar Cristal o Hielo — justo lo que piden los encargos
                    // de cristal y de "algo helado" de las jornadas 2 y 3.
                    if (matId == MaterialId.Stone) continue;

                    byte tempRaw = _sim.Grid.temp[CellGrid.Idx(x, y)];
                    bool matched = _orderSystem.TryDeliverCell(_sim.Universe, matId, tempRaw);
                    if (!matched)
                    {
                        _scrap++;
                        if (_scrap >= ScrapPerFavor)
                        {
                            _scrap -= ScrapPerFavor;
                            _orderSystem.AddFavor(1);
                        }
                    }

                    // Prioridad al verde: si en el mismo chorro entra algo que SÍ
                    // encaja, el jugador ve "aceptado" y no "chatarra" (texto).
                    if (matched) _flashAceptado = true;
                    else if (Time.time >= _flashHasta) _flashAceptado = false;
                    _flashHasta = Time.time + FlashSeconds;

                    // Misma prioridad al verde para la animación FÍSICA, pero
                    // con su propia ventana (más corta) — cada trago reinicia
                    // la sacudida: una sola vez por entrega, nunca un bucle.
                    if (matched) _animAceptado = true;
                    else if (Time.time >= _animInicio + AnimSeconds) _animAceptado = false;
                    _animInicio = Time.time;

                    _sim.Paint(x, y, 0, MaterialId.Empty);
                }
            }
        }

        // -----------------------------------------------------------------
        // Rótulo
        // -----------------------------------------------------------------
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked) return;

            UiStyles.Preparar();

            // Mensajes de RESULTADO: feedback de acción, no decoración. Se ven
            // SIEMPRE al entregar, sin importar la distancia (playtest 6).
            if (Time.time < _flashHasta)
            {
                string texto = _flashAceptado ? "¡ENTREGA ACEPTADA!" : "no encaja en ningún encargo (chatarra)";
                Color color = _flashAceptado ? UiStyles.Exito : UiStyles.Aviso;
                UiStyles.EtiquetaMundo(new Vector3(transform.position.x, transform.position.y, 0f), texto, color, UiStyles.S(26f));
                return;
            }

            // Rótulo fijo "TOLVA DEL MAESTRO": solo por cercanía y solo hasta
            // la PRIMERA vez que el jugador se acerca (_yaConocida); después
            // nunca vuelve a aparecer, ni de lejos ni de cerca.
            if (!_yaConocida)
            {
                UiStyles.PlacaMundo(new Vector3(transform.position.x, transform.position.y, 0f),
                    "TOLVA DEL MAESTRO", UiStyles.Oro, UiStyles.S(30f), _cercaniaLabio);
            }
        }
    }
}
