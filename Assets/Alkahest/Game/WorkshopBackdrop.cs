using System.Collections;
using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [TenThousandYears] Pared de fondo del taller: un sprite opaco DETRÁS del
    /// sprite de la simulación, con la textura generada por código (cero
    /// assets, cero Shader.Find — ver la regla del playtest 2).
    ///
    /// REESCRITO EN LA REINGENIERÍA DEL ESPACIO (playtest 4: "pantallón negro").
    /// Antes era un gradiente ciruela + viñeta + grano: sin ninguna estructura,
    /// las zonas del taller sin materia se leían como un vacío negro y el ojo no
    /// tenía dónde apoyarse. Ahora la pared es ARQUITECTURA:
    ///   · MAMPOSTERÍA: hiladas de ladrillo/sillar a soga corrida, con junta y
    ///     variación de tono por pieza (hash estable) — la "variación de tono"
    ///     que pedía el brief. Muy tenue: la pared debe RETROCEDER.
    ///   · VIGAS: dos vigas de madera horizontales cruzando el taller (una a la
    ///     altura de los estantes, otra bajo el techo) con sus ménsulas — dan
    ///     escala, explican los estantes flotantes y rompen el vacío superior.
    ///   · ZÓCALO: una banda de sillar más oscuro y más grande al nivel del
    ///     suelo, para que el suelo de piedra "nazca" de algo.
    ///   · LUZ DE FRAGUA: halo cálido tenue a la altura de las cubas, y viñeta
    ///     en las esquinas para cerrar el encuadre.
    ///
    /// RETEXTURIZADO EN EL PLAYTEST 7 ("la textura es horrible y está como
    /// descuadrada"). Diagnóstico: la textura vivía a 1 téxel por celda
    /// (256x144, ~7.5 px de pantalla por celda a 1080p) con FilterMode.Bilinear,
    /// mientras el sim y la maquinaria ya usaban Point. Resultado: ladrillos de
    /// 1.6x0.7 unidades (ENORMES, planos, sin textura interior), juntas de 1
    /// téxel que en pantalla se veían como una banda gorda, y un fondo borroso
    /// contra un primer plano nítido — de ahí el "descuadrado". Fix:
    ///   · Escala x3 (768x432 EN AQUEL MUNDO DE 1 PANTALLA): 9 téxeles por
    ///     celda en vez de 1, sitio de sobra para bisel + grano + esquinas
    ///     mordidas dentro de cada pieza.
    ///   · FilterMode.Point: casa con Sim/SimRenderer.cs y con MaquinariaSprites.
    ///   · Pieza más pequeña (10x5 celdas en vez de 16x7) con el MISMO lenguaje
    ///     de iluminación de canto que SimRenderer.ComputeCellColor usa para
    ///     StaticSolid (bisel superior claro / inferior oscuro): fondo y
    ///     primer plano ahora "riman" en vez de contradecirse.
    ///   · Junta de 1 TÉXEL (no 1 celda) y menos oscura (55% en vez de 75%):
    ///     mortero fino, no rejilla dura.
    /// La textura sigue midiendo EXACTAMENTE el tamaño de la grilla en téxeles
    /// (Escala téxeles por celda), así que las hiladas y vigas se pueden
    /// seguir situando en coordenadas del plano (Sim/SimLevelBuilder.cs)
    /// multiplicadas por Escala. Deliberadamente NO se toca Escala en esta
    /// ronda (sigue siendo x3, ver el porqué en "COSTE Y GENERACIÓN POR
    /// TROZOS" más abajo): SimRenderer.ComputeCellColor documenta el caso
    /// Difuso asumiendo "OTRA textura a triple resolución" — bajar Escala
    /// dejaría ese comentario ajeno mintiendo sobre un archivo que no es
    /// editable en este encargo.
    ///
    /// =====================================================================
    /// COSTE Y GENERACIÓN POR TROZOS (playtest 15, "el mundo creció 6x")
    /// =====================================================================
    /// Con CellGrid pasando de 256x144 a 768x288 (SEIS veces más celdas) y
    /// Escala sin tocar, la textura pasó de 768x432 = 331.776 téxeles a
    /// 2304x864 = 1.990.656 téxeles (~6x, exactamente proporcional al
    /// crecimiento del mundo — Escala es un factor multiplicativo constante).
    /// Eso era un ÚNICO bucle de ~2 millones de iteraciones dentro de Start(),
    /// cada una con varios hashes/lerps: de un coste que antes cabía cómodo en
    /// un frame a uno que puede colgar el primer frame de la partida un buen
    /// rato (el Player.log de una build de reparto no tiene margen para eso).
    ///
    /// SE DESCARTÓ bajar Escala (perdería nitidez ya validada en el playtest 7
    /// y dejaría mintiendo el comentario de SimRenderer.cs citado arriba) y
    /// también un tile pequeño repetido (SE DESCARTA por la razón que pide el
    /// propio encargo: vigas y zócalo están posicionados con coordenadas REALES
    /// del plano — SimLevelBuilder.ChillTrayY0/SurfaceFloorY0 — no con un
    /// patrón periódico; un tile que se repite solo sabe dibujar mampostería
    /// genérica, no "una viga exactamente donde está el estante" ni "un
    /// zócalo exactamente donde empieza el suelo de la superficie").
    ///
    /// SOLUCIÓN: el TOTAL de trabajo no baja (sigue siendo ~2M iteraciones,
    /// el coste real de pintar 1.990.656 téxeles a mano), pero deja de
    /// pagarse en UN frame. La corrutina de fondo pinta el array de píxeles
    /// en TROZOS de <see cref="RowsPerBatch"/> filas
    /// (~47 filas ≈ 110.000 téxeles por fragmento, calibrado para acercarse a
    /// un presupuesto de frame cómodo) y cede el control (`yield return null`)
    /// entre fragmento y fragmento -- Texture2D/Sprite.Create solo se llaman
    /// UNA VEZ, al final, con el array ya completo (nadie ve un fondo a medio
    /// pintar: la textura ni existe hasta entonces). Con 864 filas totales y
    /// ~47 filas por fragmento, la pared tarda ~19 frames en completarse
    /// (≈0.32 s a 60 fps, ≈0.63 s a 30 fps) -- imperceptible además porque
    /// AlkahestGameBootstrap crea este objeto ANTES que DayCycle entre en la
    /// pantalla de Título, que ya cubre la pantalla entera con un panel
    /// (DayCycle.DrawFullscreenDim) mientras el jugador escribe la seed.
    /// ANTES (mundo de 1 pantalla, sin trocear): ~331.776 iteraciones en un
    /// único frame, sin queja de congelación reportada. AHORA sin trocear
    /// habría sido ~1.990.656 iteraciones en un único frame (6x ese coste,
    /// justo lo que el encargo pide arreglar). CON el troceo: el mismo total
    /// de ~1.990.656 iteraciones, pero repartido en ~19 fragmentos de
    /// ~110.000 cada uno -- ningún frame paga más que una fracción del coste
    /// que antes preocupaba, y el arranque deja de notarse.
    ///
    /// =====================================================================
    /// EL CUARTO ÍNTIMO (playtest 21, EL PIVOT) -- AJUSTE 2
    /// =====================================================================
    /// El pivot mueve la cámara jugable a Sim/SimLevelBuilder.CuartoX0..Y1,
    /// muy lejos de CULTIVO/LABORATORIO/ChillTrayY0/SurfaceFloorY0 -- las
    /// coordenadas que anclaban la mampostería, las dos vigas, el zócalo y el
    /// halo de fragua de este archivo. Dibujar esa arquitectura ahí seguiría
    /// siendo geométricamente válido (el lienzo mide lo mismo, 768x288), pero
    /// se vería A TRAVÉS de las celdas vacías del cuarto nuevo -- vigas y
    /// zócalo que no sostienen nada real en el plano vigente, y un halo de
    /// fragua flotando sobre CULTIVO, una zona que en el pivot no existe como
    /// tal (no hay cubas ni placas ígneas: el único calor activo ahora es el
    /// que el propio Rescoldo genera, ver Game/Criatura.cs). El encargo es
    /// explícito: "la única luz de la escena tiene que venir de la criatura".
    ///
    /// El contrato del pivot también es explícito en que no hay que mantener
    /// dos modos ("El cuarto íntimo pasa a ser EL juego"): no existe un
    /// interruptor en tiempo de ejecución entre "taller clásico" y "cuarto
    /// íntimo", así que este archivo no necesita leer ningún flag para
    /// decidir qué pintar. Aun así, por la regla 26 (ningún archivo encoge
    /// sin justificación), la corrutina clásica de arriba NO se borra: se
    /// renombra a <see cref="PintarFondoTallerClasico"/> y se queda
    /// completa, compilando, simplemente sin que nadie la llame -- si algún
    /// día se recupera el taller como modo jugable, el fondo ya está aquí,
    /// intacto, en vez de tener que reconstruirlo de memoria.
    ///
    /// <see cref="Start"/> ahora llama a <see cref="PintarFondoCuartoIntimo"/>:
    /// un fondo oscuro y PLANO (un único color de base, sin hiladas de
    /// ladrillo, sin vigas, sin zócalo, sin halo de fragua) con una viñeta de
    /// encuadre opcional (permitida explícitamente por el encargo) que solo
    /// oscurece las esquinas -- no añade ninguna fuente de luz propia, así
    /// que sigue siendo cierto que la única luz "cálida" que puede aparecer
    /// en la escena es la que pinte la propia criatura sobre la simulación
    /// (sortingOrder -5, delante de este fondo en -10). Sigue troceada en
    /// <see cref="RowsPerBatch"/> filas por fragmento por coherencia con el
    /// resto del archivo, aunque al no haber hashes de mampostería el coste
    /// por téxel es mucho menor que antes -- el margen de sobra es bienvenido,
    /// no un problema.
    /// </summary>
    public sealed class WorkshopBackdrop : MonoBehaviour
    {
        // (fix playtest 7) 1 téxel/celda hacía ladrillos de pantalla ENORMES y sin
        // detalle interior. A x3 hay 9 téxeles por celda: suficiente para bisel,
        // grano de alta frecuencia y esquinas mordidas. Con el mundo a 768x288
        // (playtest 15) esto ya no es "una sola vez, barato de sobra" -- ver el
        // bloque "COSTE Y GENERACIÓN POR TROZOS" arriba para por qué se mantiene
        // igualmente (no se baja) y cómo se paga el coste sin congelar el arranque.
        private const int Escala = 3;
        private const int TexW = CellGrid.W * Escala;  // 2304
        private const int TexH = CellGrid.H * Escala;  // 864

        // =================================================================
        // (RONDA 70, domingo de ajustes de Cesar) PARALLAX LEVE DEL FONDO:
        // "Fondo 0 puede moverse una cantidad minúscula respecto al plano
        // jugable. Incluso 2-5% puede vender profundidad. No exageres o
        // parecerá teatro de cartón." Elegido 3%: el muro se desplaza un 3%
        // del recorrido de la cámara EN LA MISMA dirección -- en pantalla se
        // mueve más lento que el mundo y se lee más LEJOS. Para que el
        // desplazamiento no descubra los bordes del sprite en los extremos
        // del mundo, el fondo se ESCALA un 3% extra y se recentra (el margen
        // sobrante absorbe el vaivén: excursión máx de cámara ~26u x 3% =
        // ~0.8u, contra ~1.1u de margen por lado). SOLO el muro (-10): los
        // herrajes (baldas de piedra, pilastras, cadenas) están anclados a
        // geometría REAL de la grilla -- moverlos los desalinearía de sus
        // celdas y se leería como bug, no como profundidad.
        // =================================================================
        // (RONDA 71b, Cesar: "no vi nada que se moviera atrás en ningún
        // caso") SEGUNDA MIRADA, causa hallada: el 3% ERA IMPERCEPTIBLE.
        // Tras cruzar UNA PANTALLA entera de vuelo, el muro solo diverge un
        // 3% del ancho visible -- contra un patrón de ladrillo uniforme y
        // repetitivo, el ojo no separa eso del movimiento propio. 8% sigue
        // siendo "muy leve" (los fondos cercanos de los pixel-art clásicos
        // van de 10-20%) pero cruza el umbral de percepción; su banda "2-5%"
        // era estimación, la decisión analítica quedó en mí (textual). Si a
        // Cesar le parece teatro de cartón, se baja aquí con un número.
        private const float FactorParallax = 0.08f;
        private const float MargenParallax = 1.06f; // margen de escala acorde: excursión máx ~2.1u contra ~2.3u de sobra por lado.
        private Transform _fondoTr;
        private Vector3 _fondoBase;

        private void LateUpdate()
        {
            if (_fondoTr == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            float worldW = CellGrid.W * SimRenderer.CellWorldSize;
            float worldH = CellGrid.H * SimRenderer.CellWorldSize;
            Vector3 centro = new Vector3(worldW * 0.5f, worldH * 0.5f, 0f);
            Vector3 off = (cam.transform.position - centro) * FactorParallax;
            off.z = 0f;
            _fondoTr.position = _fondoBase + off;
        }

        /// <summary>
        /// (RONDA 75) EL FONDO DE RUINA HORNEADO: si la escena lo aporta (el
        /// menú "6. Hornear arte del prólogo" lo asigna aquí), el prólogo lo
        /// monta TAL CUAL en vez de pintar los 2 millones de téxeles por
        /// código — y Cesar lo ve y lo retoca en el editor (posición, escala,
        /// tinte del hijo "Fondo_Horneado"). AUTORIDAD: la escena. Si falta,
        /// el pintor procedural de siempre (fallback reversible).
        /// </summary>
        [SerializeField] private Sprite fondoRuinaHorneado;

        /// <summary>Variante "tal cual" para fondos AUTORADOS EN ESCENA: guarda la base del parallax SIN reescalar ni recentrar — la transform del hijo es de la escena y la escena manda; el código solo anima el offset.</summary>
        private void RegistrarFondoParallaxTalCual(GameObject go)
        {
            _fondoTr = go.transform;
            _fondoBase = go.transform.position;
        }

        /// <summary>Registra el sprite del muro para el parallax: lo escala con margen, lo recentra y guarda su base. Lo llaman los DOS pintores de fondo (cuarto íntimo y taller clásico) en su único punto de creación.</summary>
        private void RegistrarFondoParallax(GameObject go)
        {
            float worldW = CellGrid.W * SimRenderer.CellWorldSize;
            float worldH = CellGrid.H * SimRenderer.CellWorldSize;
            go.transform.localScale = Vector3.one * MargenParallax;
            go.transform.position = new Vector3(
                -worldW * (MargenParallax - 1f) * 0.5f,
                -worldH * (MargenParallax - 1f) * 0.5f, 0f);
            _fondoTr = go.transform;
            _fondoBase = go.transform.position;
        }

        // Mampostería: pieza de 10x5 CELDAS (antes 16x7 celdas a 1 téxel/celda,
        // es decir, ladrillos casi del doble de grandes y sin margen para
        // detalle interior). En téxeles: 30x15.
        private const int PiezaAnchoCeldas = 10;
        private const int PiezaAltoCeldas = 5;
        private const int PiezaAncho = PiezaAnchoCeldas * Escala; // 30
        private const int PiezaAlto = PiezaAltoCeldas * Escala;   // 15

        /// <summary>
        /// (playtest 15) Presupuesto de téxeles por fragmento de la corrutina de
        /// <see cref="Start"/>: ~110.000 téxeles/frame, calibrado para que el
        /// coste por frame se acerque al que ya se pagaba cómodamente ANTES de
        /// que el mundo creciera x6 (331.776 téxeles de una sola vez), sin
        /// pasarse mucho por arriba. Se expresa en FILAS (no en téxeles sueltos)
        /// porque el bucle interior ya está organizado por filas y así no hace
        /// falta romper esa estructura ni contar téxeles uno a uno.
        /// </summary>
        private const int TexelBudgetPerFrame = 110_000;
        private static readonly int RowsPerBatch = Mathf.Max(1, TexelBudgetPerFrame / TexW); // 47

        /// <summary>
        /// (playtest 15) LUZ DE FRAGUA: antes centrada en fracciones fijas
        /// (0.46, 0.22) del lienzo ENTERO -- correcto mientras el mundo medía
        /// una pantalla y las cubas/hornillas vivían cerca del centro de ese
        /// lienzo. Con el taller a 3 pantallas de ancho, CULTIVO (las dos
        /// cubas + las placas ígneas -- el ÚNICO calor activo real del
        /// taller) vive en el TERCIO IZQUIERDO (Sim/SimLevelBuilder.CultivoX0
        /// .. CultivoX1), no en el centro -- con las fracciones viejas el halo
        /// cálido flotaba sobre LABORATORIO, que no tiene ningún fuego. Se
        /// deriva del plano (en celdas, convertido a fracción de textura) en
        /// vez de mantener números fijos, así que sigue "iluminando" la zona
        /// que de verdad calienta.
        /// </summary>
        private const float FraguaCxFrac = (SimLevelBuilder.CultivoX0 + SimLevelBuilder.CultivoX1) * 0.5f / CellGrid.W; // ~0.173
        private const float FraguaCyFrac = (SimLevelBuilder.VatBaseY0 + SimLevelBuilder.VatInteriorY1) * 0.5f / CellGrid.H; // ~0.61, altura media de las cubas
        /// <summary>Radio del halo en fracción de textura -- del orden del ancho/alto de CULTIVO, no del taller entero (antes 0.55/0.42, pensados para cubrir un mundo de una pantalla).</summary>
        private const float FraguaRadioX = 0.19f;
        private const float FraguaRadioY = 0.21f;

        /// <summary>
        /// (playtest 21, AJUSTE 2) Punto de entrada real: pinta el fondo del
        /// cuarto íntimo. Ver el bloque "EL CUARTO ÍNTIMO" en el doc de clase
        /// para por qué ya no se llama a <see cref="PintarFondoTallerClasico"/>.
        /// </summary>
        private IEnumerator Start()
        {
            // (RONDA 74, pedido de Cesar sobre el prólogo) EL FONDO DE LA
            // FUNDACIÓN NO ES EL DEL TALLER: "no el de ladrillos sino algo
            // que haga pensar vacío, ligeramente que hubo una destrucción
            // previa — estamos reconstruyendo porque algo pasó". La fundación
            // pinta su propia ruina y NO monta los herrajes (baldas, cadenas:
            // son el taller vestido, y aquí todavía no hay taller).
            if (AlkahestGameBootstrap.ModoFundacion || AlkahestGameBootstrap.ModoGaleria || AlkahestGameBootstrap.ModoLaboratorio) // (R127) la Galería también: ruina limpia, sin herrajes del taller clásico.
            {
                // (R75) Prioridad: 1) un hijo "Fondo_Horneado" ya colocado en
                // la escena (lo que Cesar ve en el editor ES lo que sale);
                // 2) el sprite horneado asignado en el Inspector (se monta en
                // su sitio estándar); 3) el pintor procedural.
                var horneado = transform.Find("Fondo_Horneado");
                if (horneado == null && fondoRuinaHorneado != null)
                {
                    var go = new GameObject("Fondo_Horneado");
                    go.transform.SetParent(transform, false);
                    var srH = go.AddComponent<SpriteRenderer>();
                    srH.sprite = fondoRuinaHorneado;
                    srH.sortingOrder = -10;
                    srH.color = new Color(0.74f, 0.72f, 0.76f, 1f); // mismo hundimiento 2.5D.
                    horneado = go.transform;
                }
                if (horneado != null)
                {
                    RegistrarFondoParallaxTalCual(horneado.gameObject);
                    yield break;
                }
                yield return PintarFondoRuina();
                yield break;
            }
            yield return PintarFondoCuartoIntimo();
            MontarHerrajesDelTaller(); // (playtest 33) las ménsulas de las baldas y las cadenas -- ver su docblock.
        }

        /// <summary>
        /// (RONDA 74) EL FONDO DE LA RUINA — el telón del prólogo. Cuatro
        /// capas, todas tenues y deterministas (HashRoca, cero Random):
        ///  1) VACÍO: degradado casi negro, frío arriba (la nada se traga la
        ///     bóveda) y apenas cálido abajo (polvo asentado).
        ///  2) PAÑOS SUPERVIVIENTES: islas de la mampostería VIEJA con bordes
        ///     mordidos que se funden en el vacío — lo que quedó en pie de un
        ///     muro que ya no existe. Mismo lenguaje de sillería que el
        ///     taller (hiladas a soga corrida) para que, cuando el taller
        ///     real se construya, se lea que es EL MISMO lugar restaurado.
        ///  3) VIGAS CAÍDAS: tres trazos diagonales en silueta dentro de la
        ///     ventana de la caverna — la carpintería del techo que se vino
        ///     abajo.
        ///  4) ESCOMBRO AL PIE: montículos bajos e irregulares a ras del
        ///     suelo de la fundación.
        /// Sin fuentes de luz propias: la única luz del prólogo sigue siendo
        /// el fuego del Maestro (mandato pt64, intacto).
        /// </summary>

        // (R75) Los ingredientes de la ruina, a nivel de clase: los comparte
        // el pintor por filas con la herramienta de horneado del editor.
        private static readonly Color RuinaAlta = new Color(0.052f, 0.050f, 0.062f); // arriba: frío, vacío.
        private static readonly Color RuinaBaja = new Color(0.118f, 0.098f, 0.082f); // abajo: polvo tibio.
        private static readonly int[] VigaX0 = { 352, 396, 428 };
        private static readonly int[] VigaY0 = { 170, 182, 162 };
        private static readonly float[] VigaPend = { -0.38f, 0.30f, -0.22f };
        private static readonly int[] VigaLargo = { 24, 30, 20 };
        /// <summary>Ancho/alto del lienzo de la ruina (los usa también la herramienta de horneado).</summary>
        public static int RuinaTexW => TexW;
        public static int RuinaTexH => TexH;
        /// <summary>Escala de téxeles por celda del fondo (para el pixelsPerUnit del PNG horneado).</summary>
        public static int RuinaEscala => Escala;

        /// <summary>
        /// (R75) UNA fila del fondo de ruina, pura y determinista — la llaman
        /// la corrutina de runtime (por lotes, cediendo frames) y el
        /// horneador del editor (todas de golpe, a PNG). Una sola verdad de
        /// píxeles: el fondo horneado y el procedural son idénticos.
        /// </summary>
        public static void PintarFilaRuina(Color32[] px, int y)
        {
            const int piezaWc = 8, piezaHc = 4; // la sillería vieja, en celdas.
            int piezaW = piezaWc * Escala, piezaH = piezaHc * Escala;

            float ty = y / (float)(TexH - 1);
            int celdaY = y / Escala;
            Color baseFila = Color.Lerp(RuinaBaja, RuinaAlta, Mathf.Pow(ty, 0.8f));
            int hilada = y / piezaH;
            int desfase = (hilada & 1) == 1 ? piezaW / 2 : 0;
            int ly = y % piezaH;

            for (int x = 0; x < TexW; x++)
            {
                int celdaX = x / Escala;
                Color c = baseFila;

                // 1) Grano fino del vacío.
                uint g = HashRoca(x, y, 9313u);
                c *= 1f + ((g & 31u) / 31f - 0.5f) * 0.05f;

                // 2) PAÑOS SUPERVIVIENTES: rejilla de parches de 34x16
                // celdas; ~2 de cada 5 conservan muro. La máscara es una
                // elipse con el borde COMIDO por ruido — nada de recortes
                // rectos: esto se cayó, no se recortó.
                int pX = celdaX / 34, pY = celdaY / 16;
                uint hp = HashRoca(pX, pY, 7717u);
                if ((hp & 255u) < 104u)
                {
                    float dcx = (celdaX - (pX + 0.5f) * 34f) / 17f;
                    float dcy = (celdaY - (pY + 0.5f) * 16f) / 8f;
                    float d = dcx * dcx + dcy * dcy;
                    float mordida = ((HashRoca(celdaX, celdaY, 3559u) & 63u) / 63f) * 0.55f;
                    float fade = Mathf.Clamp01((0.95f - d - mordida) * 1.6f);
                    if (fade > 0f)
                    {
                        Color m = baseFila * 1.62f;
                        int lx = ((x + desfase) % piezaW + piezaW) % piezaW;
                        uint hpz = HashRoca((x + desfase) / piezaW, hilada, 5171u);
                        m *= 1f + ((hpz & 63u) / 63f - 0.5f) * 0.10f;
                        if (lx < Escala || ly == 0) m *= 0.85f;      // junta.
                        else if (ly >= piezaH - 1) m *= 1.06f;       // canto con luz.
                        c = Color.Lerp(c, m, fade);
                    }
                }

                // 3) VIGAS CAÍDAS: silueta oscura de 2 celdas de grosor.
                for (int k = 0; k < 3; k++)
                {
                    int rel = celdaX - VigaX0[k];
                    if (rel < 0 || rel > VigaLargo[k]) continue;
                    float yLinea = VigaY0[k] + rel * VigaPend[k];
                    if (Mathf.Abs(celdaY - yLinea) <= 1f) c *= 0.66f;
                }

                // 4) ESCOMBRO AL PIE: altura por tramos de 5 celdas +
                // jitter fino de 1 — montículos, no un peine.
                int hRel = celdaY - SimLevelBuilder.FundacionY0;
                if (hRel >= 0 && hRel < 7)
                {
                    int tramo = celdaX / 5;
                    int altura = (int)(HashRoca(tramo, 0, 8117u) % 5u)
                               + (int)(HashRoca(celdaX, 0, 6329u) % 2u);
                    if (hRel < altura)
                    {
                        uint he = HashRoca(celdaX, celdaY, 4441u);
                        c = baseFila * (1.32f + ((he & 31u) / 31f) * 0.22f);
                        if (hRel == altura - 1) c *= 1.14f; // el lomo del montículo recibe luz.
                    }
                }

                px[y * TexW + x] = new Color(
                    Mathf.Clamp01(c.r), Mathf.Clamp01(c.g), Mathf.Clamp01(c.b), 1f);
            }
        }

        // =================================================================
        // (R84, FASE B1 del capítulo 2 — el REORDEN) LA RUINA CEDE AL TALLER:
        // el fondo del castillo profundo (el MISMO PintarFondoCuartoIntimo
        // del juego completo — una sola verdad de píxeles) se pinta encima de
        // la ruina (orden -9 sobre -10) y entra en fundido. La promesa de la
        // R74 ("estamos reconstruyendo porque algo pasó") se paga aquí: el
        // espacio se ORDENA a la vista. Los herrajes (baldas/cadenas) NO se
        // montan: son el taller vestido, y eso llega con sus fases.
        // =================================================================
        private bool _transicionHecha;

        /// <summary>Arranca el fundido ruina→taller una única vez. La llama el director del prólogo durante el REORDEN.</summary>
        public void TransicionAFondoTaller(float seg)
        {
            if (_transicionHecha) return;
            _transicionHecha = true;
            StartCoroutine(TransicionAFondoTallerCo(Mathf.Max(0.2f, seg)));
        }

        private IEnumerator TransicionAFondoTallerCo(float seg)
        {
            // Bombear el pintor A MANO: al terminar seguimos en el MISMO
            // frame en que creó su GO — así se le baja el alfa a 0 antes de
            // que se renderice una sola vez (sin bombeo habría un frame de
            // fondo nuevo a plena luz: un flash).
            var pintor = PintarFondoCuartoIntimo();
            while (pintor.MoveNext()) yield return pintor.Current;

            var go = GameObject.Find("WorkshopBackdrop_Sprite");
            if (go == null) yield break; // imposible hoy; si el pintor cambia de nombre, el fundido simplemente no ocurre (y la ruina se queda — reversible).
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sortingOrder = -9; // delante de la ruina (-10), detrás de la sim (-5).
            var meta = sr.color;
            sr.color = new Color(meta.r, meta.g, meta.b, 0f);

            float t = 0f;
            while (t < seg)
            {
                t += Time.deltaTime;
                float a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / seg));
                sr.color = new Color(meta.r, meta.g, meta.b, a * meta.a);
                yield return null;
            }
            sr.color = meta;
        }

        private IEnumerator PintarFondoRuina()
        {
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
            {
                name = "TenThousandYearsRuinaBackdrop",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            var px = new Color32[TexW * TexH];

            int rowsSinCeder = 0;
            for (int y = 0; y < TexH; y++)
            {
                PintarFilaRuina(px, y);
                rowsSinCeder++;
                if (rowsSinCeder >= RowsPerBatch)
                {
                    rowsSinCeder = 0;
                    yield return null;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);

            var go = new GameObject("WorkshopBackdrop_Ruina");
            var sr = go.AddComponent<SpriteRenderer>();
            float worldW = CellGrid.W * SimRenderer.CellWorldSize;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, TexW, TexH), Vector2.zero, TexW / worldW, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = -10; // detrás del sprite de la simulación (-5).
            sr.color = new Color(0.74f, 0.72f, 0.76f, 1f); // mismo hundimiento 2.5D que el fondo del taller.
            RegistrarFondoParallax(go);
        }

        // =================================================================
        // (playtest 33) EL HERRAJE DEL TALLER -- las ménsulas de las baldas
        // =================================================================
        // Encargo de Cesar: *"créale como BRACITOS INCLINADOS para que pueda
        // sostener más cosas, y en el techo puedo tener MÁS de estos lugares
        // para ir acomodando lo que encuentro"*.
        //
        // POR QUÉ VIVE AQUÍ Y NO EN UNA MÁQUINA: las baldas no tienen dueño.
        // Son SALA, igual que las pilastras, la bóveda y las hornacinas, y
        // esta clase es la que ya se ocupa de vestir la sala. Ninguna es
        // movible (no implementan IMovible ni se registran en Mudanza): son
        // arquitectura, y la arquitectura del cuarto no se muda -- por eso
        // tampoco necesitan ser hijas de nada que se mueva.
        //
        // LA PIEDRA MANDA: la posición, el ancho y la altura de cada herraje
        // se LEEN de Sim/SimLevelBuilder.Repisas (regla 39 de CLAUDE.md: nunca
        // calibrar contra prosa). Si el plano mueve una balda, su ménsula y su
        // filo se mueven con ella sin tocar este archivo.
        // =================================================================
        private void MontarHerrajesDelTaller()
        {
            float c = SimRenderer.CellWorldSize;
            var raiz = new GameObject("TallerHerrajes").transform;
            raiz.position = Vector3.zero;

            var repisas = SimLevelBuilder.Repisas;
            for (int i = 0; i < repisas.Length; i++)
            {
                var r = repisas[i];
                int ancho = r.X1 - r.X0 + 1;
                float cxMundo = (r.X0 + ancho * 0.5f) * c;

                // 1) LA SOMBRA PROYECTADA, lo primero: sin ella una balda es
                //    una roca pegada al fondo. Va justo debajo del canto, algo
                //    más ancha que la piedra.
                MaquinariaSprites.Sombra(raiz,
                    new Vector3(cxMundo, (r.Y - 2.6f) * c, 0f),
                    (ancho + 4) * c, 4.2f * c, 0.38f);

                // 2) (SEGUNDA PASADA, visto jugando) LA LOSA, que antes no se
                //    veía -- ver MaquinariaSprites.BaldaPiedra. Va justo sobre
                //    las 2 filas de Stone reales, con su mismo tamaño, así que
                //    el dibujo y lo que sostiene son la misma cosa.
                var losa = MaquinariaSprites.CrearCapa(raiz, "BaldaPiedra_" + i,
                    MaquinariaSprites.BaldaPiedra(ancho, 2), OrdenHerraje, ancho * c, 2f * c);
                losa.transform.position = new Vector3(cxMundo, (r.Y - 0.5f) * c, 0f);

                // 3) LOS DOS BRACITOS. (SEGUNDA PASADA) Más pequeños y METIDOS
                //    HACIA DENTRO: pegados al extremo y a tamaño 5-6 celdas
                //    parecían las patas de un caballete, y una balda con patas
                //    es una mesa flotando. Un apoyo que arranca dentro del
                //    vuelo, y más corto que alto el propio canto, se lee como
                //    ménsula.
                int mensulaAncho = Mathf.Clamp(ancho / 6, 3, 5);
                int mensulaAlto = Mathf.Clamp(mensulaAncho, 3, 5);
                int adentro = ancho >= 16 ? 2 : 1;
                var mensula = MaquinariaSprites.MensulaInclinada(mensulaAncho, mensulaAlto);
                float yMensula = (r.Y - 1.5f - mensulaAlto * 0.5f) * c; // colgando de la cara inferior de la piedra.

                var izq = MaquinariaSprites.CrearCapa(raiz, "MensulaIzq_" + i, mensula, OrdenHerraje,
                    mensulaAncho * c, mensulaAlto * c);
                izq.transform.position = new Vector3((r.X0 + adentro + mensulaAncho * 0.5f) * c, yMensula, 0f);

                var der = MaquinariaSprites.CrearCapa(raiz, "MensulaDer_" + i, mensula, OrdenHerraje,
                    mensulaAncho * c, mensulaAlto * c);
                der.transform.position = new Vector3((r.X1 + 1 - adentro - mensulaAncho * 0.5f) * c, yMensula, 0f);
                var e = der.transform.localScale; e.x = -e.x; der.transform.localScale = e; // espejo: el bracito de la derecha mira al otro lado.

                // Una balda larga necesita un apoyo INTERMEDIO o el ojo la ve
                // pandear -- es la misma intuición estructural que hace que
                // una estantería real lleve un montante cada metro.
                if (ancho >= 22)
                {
                    var med = MaquinariaSprites.CrearCapa(raiz, "MensulaMed_" + i, mensula, OrdenHerraje,
                        mensulaAncho * c, mensulaAlto * c);
                    med.transform.position = new Vector3(cxMundo, yMensula, 0f);
                }

                // 4) EL FILO DE LATÓN sobre el canto: la línea brillante que
                //    dice "esto es un mueble donde se posan cosas".
                var filo = MaquinariaSprites.CrearCapa(raiz, "FiloBalda_" + i,
                    MaquinariaSprites.FiloBalda(ancho), OrdenHerraje + 2, ancho * c, 1.1f * c);
                filo.transform.position = new Vector3(cxMundo, (r.Y + 0.55f) * c, 0f);
            }

            // 5) (SEGUNDA PASADA, visto jugando) LAS PILASTRAS, VESTIDAS. La
            //    piedra colgante que talla el plano existe, pero contra una
            //    pared que también es sillería oscura no se leía: cinco
            //    sombras verticales que podían ser textura. Se les pone encima
            //    el mismo `Sillar` con el que la Columna de Ensayo viste sus
            //    machones (playtest 27) y una ménsula-capitel al pie -- y
            //    entonces sí son pilares.
            var cols = SimLevelBuilder.PilastraColumnas;
            var caidas = SimLevelBuilder.PilastraCaidas;
            for (int i = 0; i < cols.Length; i++)
            {
                int caida = i < caidas.Length ? caidas[i] : 14;
                int anchoP = 4;
                float cxP = (cols[i] - anchoP / 2 + anchoP * 0.5f) * c;
                float yBot = SimLevelBuilder.CuartoY1 - caida;

                var fuste = MaquinariaSprites.CrearCapa(raiz, "PilastraSillar_" + i,
                    MaquinariaSprites.Sillar(anchoP, caida), OrdenHerraje, anchoP * c, caida * c);
                fuste.transform.position = new Vector3(cxP, (yBot + caida * 0.5f) * c, 0f);

                // El capitel invertido del pie: dos ménsulas espejadas bajo el
                // vuelo que ya talla el plano.
                //
                // (playtest 34, BUG VISTO JUGANDO -- regla 52) Estas dos
                // líneas MEZCLABAN UNIDADES: `cxP` ya viene multiplicado por
                // `c` (unidades de mundo) y se le sumaba `anchoP*0.5f+0.5f`
                // SIN multiplicar, o sea 2.5 unidades de mundo = **25
                // CELDAS**. Resultado en pantalla: diez ménsulas de latón
                // flotando en mitad del aire, repartidas por toda la bóveda,
                // cada una a 25 celdas de la pilastra a la que pertenecía --
                // se veían como calcomanías sueltas y no había forma de
                // adivinar de dónde salían leyendo el código a ojo, porque
                // la fórmula "parece" correcta. Sobrevivió a la ronda
                // anterior porque el 33 tenía la sala llena de herrajes y dos
                // chevrones más no llamaban la atención; con la sala 32
                // celdas más alta y medio vacía, saltan a la vista.
                var cap = MaquinariaSprites.MensulaInclinada(3, 3);
                float vueloCap = (anchoP * 0.5f + 0.5f) * c;
                var ci = MaquinariaSprites.CrearCapa(raiz, "PilastraCapIzq_" + i, cap, OrdenHerraje + 1, 3f * c, 3f * c);
                ci.transform.position = new Vector3(cxP - vueloCap, (yBot + 1.4f) * c, 0f);
                var ei = ci.transform.localScale; ei.x = -ei.x; ci.transform.localScale = ei;
                var cd = MaquinariaSprites.CrearCapa(raiz, "PilastraCapDer_" + i, cap, OrdenHerraje + 1, 3f * c, 3f * c);
                cd.transform.position = new Vector3(cxP + vueloCap, (yBot + 1.4f) * c, 0f);
            }

            // 6) LOS HACES DE LAS CLARABOYAS -- **RETIRADOS** (playtest 34,
            //    regla 15 de CLAUDE.md: se documenta lo que se quita, no se
            //    borra en silencio). Cesar, jugando el 33: *"la LUZ de las
            //    claraboyas no está lista: rompo bedrock y queda EN EL AIRE
            //    -- QUÍTALA por ahora, luego volvemos"*. Tenía razón y el
            //    motivo es estructural, no de calibración: estos sprites se
            //    dibujaban DELANTE de la sim (OrdenHalo-1) en una posición
            //    fija derivada de `CuartoY1`, sin ninguna relación con la
            //    piedra que tenían debajo -- en cuanto el jugador cincelaba
            //    la bóveda, el cono seguía ahí, colgado del aire, señalando
            //    un pozo que ya no existía. Un haz de luz volumétrico honesto
            //    necesita saber por dónde pasa la piedra (y, como el propio
            //    Cesar apuntó en el 33, PARTÍCULAS que lo habiten); las dos
            //    cosas son la ronda siguiente. El pozo ciego SIGUE tallado en
            //    Sim/SimLevelBuilder.TallarBoveda y la claraboya sigue
            //    DIBUJADA como ventana ciega en el propio fondo (ver
            //    `ClasificarVano`), que es lo que Cesar sí pidió conservar:
            //    lo que se va es la luz, no la ventana.
            //    El código, para cuando se retome:
            //      var haz = MaquinariaSprites.CrearCapa(raiz, "HazClaraboya_" + i,
            //          MaquinariaSprites.HazClaraboya(hazAncho, hazAlto), ...);
            //      haz.color = new Color(0.62f, 0.74f, 0.95f, 0.17f);

            // 7) LAS CADENAS, COLGADAS DE LAS VIGAS DEL PROPIO FONDO
            //    (playtest 34, obra C). Cesar: *"las cadenas están bien pero
            //    deben COLGAR DE ALGO del DIBUJO DE FONDO (vigas del fondo en
            //    el tercio superior), porque si agrando el taller quedan
            //    cubiertas por bedrock o flotando"*.
            //
            //    EL DIAGNÓSTICO, QUE ES EL QUE MANDA: hasta el 33 las cadenas
            //    nacían en `yTop = CuartoY1`, o sea colgando del TECHO DE
            //    PIEDRA DE LA SIM. Eso tiene dos fallos que Cesar vio a la
            //    primera: (a) esa piedra es excavable -- el jugador pica la
            //    bóveda y la cadena se queda flotando de nada; (b) el techo
            //    de piedra se dibuja DELANTE del fondo, así que la primera
            //    celda de la cadena quedaba tapada y la cadena parecía
            //    empezar en el aire de todos modos.
            //
            //    LA CORRECCIÓN: nacen de las VIGAS que pinta la textura de
            //    fondo (<see cref="_vigasY"/>), en el tercio superior de la
            //    sala. Esas vigas son PINTURA, no celdas: no se pueden picar,
            //    no se pueden tapar por bedrock roto, y siguen ahí aunque el
            //    jugador excave la sala entera -- que es literalmente lo que
            //    pedía el encargo. La cadena arranca en `vigaY - 3`, el canto
            //    inferior de la viga (ver `FactorViga`: el cuerpo de la viga
            //    ocupa `v-3..v+1`), así que se ve nacer DE la madera.
            for (int i = 0; i < _cadenasX.Length; i++)
            {
                int largo = _cadenasLargo[i];
                int viga = _vigasY[_cadenasViga[i]];
                float yTop = viga - 3f; // el canto inferior de la viga: de ahí cuelga.

                // El GANCHO: una ménsula diminuta entre la viga y el primer
                // eslabón. Sin ella la cadena "toca" la viga; con ella se
                // AGARRA, que es la diferencia entre un sprite superpuesto y
                // una pieza montada.
                var gancho = MaquinariaSprites.CrearCapa(raiz, "CadenaGancho_" + i,
                    MaquinariaSprites.MensulaInclinada(2, 2), OrdenHerraje + 1, 2f * c, 2f * c);
                gancho.transform.position = new Vector3((_cadenasX[i] + 0.5f) * c, (yTop + 0.6f) * c, 0f);

                var sr = MaquinariaSprites.CrearCapa(raiz, "Cadena_" + i,
                    MaquinariaSprites.CadenaColgante(largo), OrdenHerraje, 1.6f * c, largo * c);
                sr.transform.position = new Vector3((_cadenasX[i] + 0.5f) * c, (yTop - largo * 0.5f) * c, 0f);
                sr.color = new Color(1f, 1f, 1f, 0.9f);
            }
        }

        /// <summary>Orden de dibujo del herraje: delante del sprite de la sim (-5) y de la piedra, detrás de los halos (40) y del aprendiz (50).</summary>
        private const int OrdenHerraje = 16;
        // (playtest 34) COLUMNAS NUEVAS, para el plano central. Comprobadas
        // una a una contra `SimLevelBuilder.PilastraColumnas`
        // (133/185/244/284/331, con ménsula ocupan cx-3..cx+3) para que
        // ninguna cadena nazca DENTRO de una pilastra -- el clásico "sprite
        // pegado" que ya costó una iteración en el 33. La separación mínima
        // real de esta tabla a cualquier pilastra es de 5 columnas.
        private static readonly int[] _cadenasX = { 162, 208, 252, 296, 338 };
        /// <summary>Índice en <see cref="_vigasY"/> del que cuelga cada cadena. Solo las DOS vigas altas (1 y 2) sostienen cadenas: el encargo pide el TERCIO SUPERIOR y la viga 0 es la imposta baja, a la altura de las baldas.</summary>
        private static readonly int[] _cadenasViga = { 2, 1, 2, 1, 2 };
        /// <summary>Longitudes distintas por la misma razón que las pilastras tienen caídas distintas: un ritmo regular se deja de mirar.</summary>
        private static readonly int[] _cadenasLargo = { 16, 24, 12, 28, 18 };

        /// <summary>
        /// (playtest 21, AJUSTE 2) Fondo del cuarto íntimo: plano, oscuro,
        /// uniforme. Sin mampostería con hiladas/piezas, sin vigas, sin
        /// zócalo, sin halo de fragua -- "la única luz de la escena tiene que
        /// venir de la criatura". Incluye una viñeta de encuadre suave
        /// (permitida explícitamente por el encargo): oscurece las esquinas
        /// sin introducir ninguna fuente de luz propia, así que no compite
        /// con el resplandor que la propia Criatura dibuja sobre el sprite
        /// de la simulación.
        /// </summary>
        private IEnumerator PintarFondoCuartoIntimo()
        {
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
            {
                name = "TenThousandYearsWorkshopBackdrop",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var px = new Color32[TexW * TexH];

            // =============================================================
            // (playtest 31, ILUMINACIÓN DE ÁNIMO) LA ROCA DEL FONDO
            // =============================================================
            // Antes: un color plano + viñeta. Correcto para el "cuarto
            // íntimo" del playtest 21 (donde la única luz debía venir de la
            // criatura) pero, con el taller grande de vuelta y las máquinas
            // dando luz propia (Game/MaquinariaSprites.Luz), un fondo
            // absolutamente plano se lee como TELÓN: no hay nada detrás de
            // las estaciones, solo vacío.
            //
            // Ahora la pared es ROCA, con tres capas MUY tenues (ninguna
            // supera el 8% de desviación de brillo: la pared tiene que
            // RETROCEDER, no competir con la materia, que es lo único
            // saturado del cuadro):
            //   1) gradiente vertical -- más fría arriba (la bóveda se pierde
            //      en sombra) y más cálida abajo (cerca del suelo y de los
            //      fuegos);
            //   2) veta de roca de baja frecuencia (bloques de ~8 celdas con
            //      hash estable): da la escala de la caverna;
            //   3) grano fino, para que a la resolución de pantalla no se vea
            //      ninguna banda plana.
            // Más la viñeta que ya había, un punto más cerrada.
            // El conjunto queda ~15% MÁS OSCURO que el fondo anterior: la
            // penumbra es lo que permite que un halo cálido se lea como luz.
            // =============================================================
            // =============================================================
            // (playtest 34) EL FONDO ÚNICO, DE COBERTURA TOTAL
            // =============================================================
            // Tres frases de Cesar jugando el 33, y las tres apuntan al mismo
            // sitio:
            //  (3.2) *"el fondo tiene 3 diseños: quiero UNO SOLO -- el que
            //        está detrás de la PRENSA es el que más me gusta; el
            //        trabado de ladrillos NO debe atravesar las ventanas;
            //        menos definido, que se sienta FONDO y no sature"*.
            //  (3.3) *"el fondo no abarca todo el mapa: cuando rompo en otras
            //        partes se ve negro"*.
            //  (5)   *"hay unas LÍNEAS NEGRAS raras en la parte superior del
            //        fondo: quítalas"*.
            //
            // LO QUE CAMBIA, PUNTO POR PUNTO:
            //
            // 1) UN SOLO APAREJO. Los tres aparejos por zona del 33 (sillar
            //    grande ahumado / hilada baja apretada / sillar noble frío)
            //    se retiran a favor del que Cesar eligió por su nombre: el de
            //    la zona de la PRENSA, hilada baja y apretada (7x3 celdas).
            //    Ver <see cref="AparejoDelTaller"/>, que conserva el porqué
            //    de los otros dos por la regla 15 de CLAUDE.md.
            //
            // 2) COBERTURA TOTAL. Antes había un `if (enCuarto)` con un
            //    `else` de "roca profunda" casi negra: en cuanto el jugador
            //    cincelaba fuera del rect del cuarto, el agujero enseñaba
            //    NEGRO. Ahora la sillería se pinta en las 768x288 celdas del
            //    mundo, sin excepción -- no hay un solo téxel del lienzo que
            //    no sea muro. Lo que distingue el taller de la roca lejana ya
            //    no es "haber o no haber dibujo", sino la PROFUNDIDAD:
            //    `profundidad` (0 dentro del taller, 1 a 110 celdas de él)
            //    oscurece la pared Y, a la vez, DESVANECE el propio aparejo
            //    (ver `defin`), así que la piedra se pierde en la penumbra en
            //    vez de cortarse de golpe. Es la variación tenue por
            //    profundidad que pedía el encargo, y de paso el borde duro
            //    que separaba cuarto de roca desaparece solo.
            //
            // 3) MENOS DEFINIDO. Todos los contrastes del aparejo bajan a la
            //    mitad larga: junta 0.55 -> 0.82, canto alto 1.16 -> 1.07,
            //    canto bajo 0.80 -> 0.92, pátina por pieza +-13% -> +-7%,
            //    grano +-3% -> +-1.6%. La pared tiene que RETROCEDER; la
            //    materia de la sim es lo único que puede saturar el cuadro.
            //
            // 4) LAS LÍNEAS NEGRAS DE ARRIBA. Eran dos, las dos de aquí:
            //    (a) la SOMBRA de las vigas de piedra, que multiplicaba por
            //    0.58-0.66 en cuatro filas seguidas y a esa altura, sobre una
            //    pared ya oscura, se leía como una raya negra de punta a
            //    punta del taller; y (b) las DOVELAS, cuya junta radial
            //    multiplicaba por 0.62 dibujando un abanico de rayas negras
            //    justo por encima del arranque de la bóveda -- que es
            //    exactamente "la parte superior del fondo". Las dos se
            //    conservan como FORMA (dan la bóveda y la escala) pero con
            //    contraste de fondo: 0.86 y 0.90.
            //
            // 5) LAS VENTANAS TIENEN MARCO PROPIO Y EL TRABADO NO LAS CRUZA.
            //    Ver <see cref="ClasificarVano"/>: hornacinas y claraboyas
            //    ciegas dejan de ser un multiplicador de brillo aplicado
            //    ENCIMA de la sillería (que es lo que hacía que las hiladas
            //    las atravesaran de lado a lado, el defecto que Cesar
            //    describe) y pasan a ser un hueco de verdad -- jamba, dintel,
            //    alféizar y un plano ciego liso dentro, SIN una sola junta.
            // =============================================================
            // ROCA PROFUNDA: el tono al que tiende la pared cuando se aleja
            // del taller. Ya NO es "lo que se pinta fuera del cuarto" (no hay
            // fuera: ver el punto 2 de arriba), sino el extremo lejano de un
            // degradado.
            //
            // (playtest 34, TERCERA PASADA -- MEDIDO JUGANDO, regla 52) LA
            // PRUEBA DEL CINCEL. Se pintó `Empty` con F3 en un paño de roca a
            // ~200 celdas del taller, que es literalmente el gesto de Cesar
            // -- *"cuando rompo en otras partes se ve negro"* --, y el agujero
            // SEGUÍA saliendo negro pese a que la cobertura total ya estaba
            // hecha: la culpa no era de que faltara pared, sino de que a
            // `prof`=1 esta pareja de colores (0.030/0.052) por la viñeta
            // (0.72) daba 0.02-0.04, o sea negro a efectos prácticos. Se
            // suben a ~55% del tono de la pared del taller: sigue siendo
            // "más oscuro cuanto más lejos" -- que es lo que pedía el encargo
            // -- pero lo que aparece al romper es PIEDRA en penumbra, no un
            // agujero. Estos dos números son el resultado de la prueba, no
            // una estimación: es la diferencia entre pintar el fondo y que el
            // fondo se VEA.
            // (CUARTA PASADA, segunda prueba del cincel) 0.086/0.120 ->
            // 0.118/0.158: con la primera corrección el agujero ya no era
            // negro, pero seguía a ~25% del brillo de la piedra de la sim que
            // lo rodea -- se leía "oscuro", no "de piedra". A ~45% se lee lo
            // que tiene que leerse: la MISMA pared del taller, en penumbra,
            // 200 celdas más lejos.
            var rocaLejosAlta = new Color(0.118f, 0.107f, 0.118f);
            var rocaLejosBaja = new Color(0.158f, 0.130f, 0.109f);

            // LA PARED DEL TALLER. Mucho más clara que la roca lejana -- no
            // por realismo, sino porque ANTES DEL PLAYTEST 31 el taller
            // entero flotaba sobre un vacío negro (visto jugando): las
            // máquinas no tenían pared detrás, así que ninguna parecía estar
            // DENTRO de un sitio. Una habitación se lee cuando tiene fondo.
            // (playtest 34, SEGUNDA PASADA -- VISTO JUGANDO, regla 52) +50% y
            // +30%. Con la sala 32 celdas más alta, los dos tercios de arriba
            // son PARED, y a 0.101 (por 0.72 de viñeta = 0.073 real) esa pared
            // no se leía como piedra: se leía como el agujero negro que Cesar
            // describía en el 33 -- solo que ahora ocupando media pantalla. El
            // contraste contra la roca de la sim (que SimRenderer dibuja
            // mucho más clara, en primer plano) era tan brutal que el taller
            // parecía recortado sobre un vacío. La pared sigue estando MUY
            // por debajo de la piedra del primer plano -- retrocede, que es su
            // trabajo -- pero ahora se ve que es piedra.
            var paredAlta = new Color(0.152f, 0.136f, 0.148f);  // arriba: la bóveda se apaga.
            var paredBaja = new Color(0.225f, 0.178f, 0.144f);  // abajo: pardo cálido, la luz de los fuegos rebota en el zócalo.

            // El aparejo ÚNICO, leído una sola vez fuera de los dos bucles
            // (antes se resolvía por columna con tres ramas: ahora es
            // constante en todo el lienzo, que es justo lo que pidió Cesar).
            AparejoDelTaller(out int piezaAnchoC, out int piezaAltoC, out float patina);
            int piezaW = piezaAnchoC * Escala, piezaH = piezaAltoC * Escala;

            int rowsSinCeder = 0;

            for (int y = 0; y < TexH; y++)
            {
                float ty = y / (float)(TexH - 1);
                int celdaY = y / Escala;

                // Degradado vertical del taller (se usa a todas las alturas:
                // fuera del rango del cuarto simplemente satura en sus
                // extremos, que es lo que hace que el techo y el subsuelo
                // lejanos sigan teniendo una dirección de luz coherente).
                float tCuarto = Mathf.Clamp01((celdaY - SimLevelBuilder.CuartoY0)
                    / (float)(SimLevelBuilder.CuartoY1 - SimLevelBuilder.CuartoY0));
                Color paredFila = Color.Lerp(paredBaja, paredAlta, Mathf.Pow(tCuarto, 0.75f));
                Color rocaFila = Color.Lerp(rocaLejosBaja, rocaLejosAlta, Mathf.Pow(ty, 0.85f));

                // Distancia vertical FUERA del volumen del taller (0 dentro).
                int dyCuarto = celdaY < SimLevelBuilder.CuartoY0 ? SimLevelBuilder.CuartoY0 - celdaY
                             : celdaY > TechoMaximo ? celdaY - TechoMaximo : 0;
                // Lo mismo para el vestíbulo de la Tolva (pasillo + pozo), que
                // es un volumen construido aparte, a otra cota.
                int dyVest = celdaY < SimLevelBuilder.ChuteMouthY0 - 6 ? SimLevelBuilder.ChuteMouthY0 - 6 - celdaY
                           : celdaY > SimLevelBuilder.ChuteMouthY1 + 8 ? celdaY - (SimLevelBuilder.ChuteMouthY1 + 8) : 0;

                int hilada = y / piezaH;
                int desfase = (hilada & 1) == 1 ? piezaW / 2 : 0;
                int ly = y % piezaH;

                for (int x = 0; x < TexW; x++)
                {
                    float tx = x / (float)(TexW - 1);
                    int celdaX = x / Escala;

                    // ---- PROFUNDIDAD: a qué distancia del taller estamos.
                    int dxCuarto = celdaX < SimLevelBuilder.CuartoX0 ? SimLevelBuilder.CuartoX0 - celdaX
                                 : celdaX > SimLevelBuilder.CuartoX1 ? celdaX - SimLevelBuilder.CuartoX1 : 0;
                    int distCuarto = dxCuarto > dyCuarto ? dxCuarto : dyCuarto;

                    int dxVest = celdaX < SimLevelBuilder.CuartoX1 ? SimLevelBuilder.CuartoX1 - celdaX
                               : celdaX > SimLevelBuilder.ChuteMouthX1 + 3 ? celdaX - (SimLevelBuilder.ChuteMouthX1 + 3) : 0;
                    int distVest = dxVest > dyVest ? dxVest : dyVest;

                    int dist = distCuarto < distVest ? distCuarto : distVest;
                    float prof = Mathf.Clamp01(dist / 110f);
                    // La pared se aleja: se oscurece hacia la roca profunda...
                    Color c = Color.Lerp(paredFila, rocaFila, Mathf.SmoothStep(0f, 1f, prof));
                    // ...y el DIBUJO se desvanece con ella. `defin` escala
                    // TODA desviación respecto a 1 de las capas de aparejo:
                    // cerca del taller la sillería se lee, a 110 celdas es
                    // casi un tono liso. Es lo que hace que la cobertura total
                    // no convierta el mundo entero en una pared de ladrillo
                    // gritona.
                    float defin = Mathf.Lerp(1f, 0.55f, prof); // (3ª/4ª pasada) 0.30 -> 0.45 -> 0.55: con 0.30 el aparejo lejano desaparecía y el agujero del cincel se leía liso, no de piedra.

                    // ---- ¿ESTAMOS EN UNA VENTANA? Se resuelve ANTES del
                    // aparejo, y por eso el trabado no puede atravesarla: si
                    // esto devuelve marco o vano, la sillería no llega a
                    // dibujarse en ese téxel (punto 5 del bloque de arriba).
                    int vano = ClasificarVano(celdaX, celdaY, out float vanoK);

                    if (vano == 0)
                    {
                        // --- SILLERÍA a soga corrida (hiladas alternas
                        // desplazadas media pieza), con junta fina y bisel --
                        // el mismo lenguaje de canto claro arriba / oscuro
                        // abajo que usa SimRenderer.ComputeCellColor para la
                        // piedra de la sim, para que fondo y primer plano
                        // rimen en vez de contradecirse (lección del playtest
                        // 7), pero con la MITAD de contraste que en el 33.
                        int lx = ((x + desfase) % piezaW + piezaW) % piezaW;
                        uint hp = HashRoca((x + desfase) / piezaW, hilada, 5171u);
                        float tono = 1f + ((hp & 63u) / 63f - 0.5f) * patina * defin;
                        c *= tono;

                        if (lx == 0 || ly == 0) c *= 1f - 0.18f * defin;          // junta de mortero (era 0.55: una rejilla dura).
                        else if (ly >= piezaH - 1) c *= 1f + 0.07f * defin;       // canto superior: le da la luz.
                        else if (ly <= 1) c *= 1f - 0.08f * defin;                // canto inferior: en sombra.

                        // --- LAS DOVELAS DE LA BÓVEDA: por encima del
                        // arranque, la junta VERTICAL deja de ser vertical y
                        // se abre en abanico hacia la clave de su tramo. Es la
                        // diferencia entre "una pared que sigue hacia arriba"
                        // y "una bóveda": un muro tiene juntas a plomo, una
                        // bóveda las tiene radiales. (playtest 34) 0.62 ->
                        // 0.90: era la mitad de "las líneas negras raras de la
                        // parte superior" que Cesar mandó quitar.
                        if (celdaY > ArranqueBoveda)
                        {
                            int centro = CentroDeTramo(celdaX);
                            int subida = celdaY - ArranqueBoveda;
                            int sesgo = ((celdaX - centro) * subida) / 26;
                            int lxDov = (((x - sesgo * Escala) % piezaW) + piezaW) % piezaW;
                            if (lxDov < Escala) c *= 1f - 0.10f * defin;
                        }

                        // --- LAS VIGAS DE PIEDRA: tres bandas horizontales
                        // que cruzan el taller entero. Van en el FONDO a
                        // propósito (no son celdas de la sim): dan escala y
                        // horizontal a una sala de 127 celdas de alto sin
                        // poner ni un obstáculo en el volumen por donde se
                        // vuela -- y son de lo que cuelgan las cadenas (obra C
                        // del playtest 34, ver MontarHerrajesDelTaller).
                        c *= FactorViga(celdaY, defin);

                        // --- ZÓCALO: las 5 primeras celdas sobre el suelo,
                        // más oscuras y sin sillería fina: el arranque del
                        // muro, que es lo que hace que el suelo "nazca de
                        // algo".
                        int sobreSuelo = celdaY - (SimLevelBuilder.CuartoY0 + 3);
                        if (sobreSuelo >= 0 && sobreSuelo < 5)
                            c *= Mathf.Lerp(1f - 0.30f * defin, 1f, sobreSuelo / 5f);

                        // --- CORNISA: la imposta del arranque de la bóveda.
                        if (celdaY >= ArranqueBoveda - 1 && celdaY <= ArranqueBoveda + 1)
                            c *= 1f + 0.14f * defin;
                    }
                    else if (vano == 1)
                    {
                        // MARCO de la ventana: piedra labrada, lisa, un punto
                        // más clara que el paño -- el canto que recibe la luz.
                        // Sin junta ni hiladas: una jamba es UNA pieza.
                        c *= 1f + 0.20f * defin;
                    }
                    else
                    {
                        // EL VANO CIEGO: un plano liso que se HUNDE (nunca
                        // negro -- la lección de las hornacinas del playtest
                        // 31: un hueco se lee por el contraste de su canto, no
                        // por ser un agujero), con su propio degradado interno
                        // para que no sea una mancha plana.
                        c *= Mathf.Lerp(1f, 0.70f, vanoK * defin); // (3ª pasada) 0.62 -> 0.70: mismo criterio que la tercera pasada del playtest 31 -- un hueco se hunde, no desaparece.
                        c.b *= 1f + 0.06f * vanoK; // la luz que se cuela por un vano es fría.
                    }

                    // --- EL REBOTE DE LA FRAGUA: la pared detrás del crisol
                    // recibe su luz. Anclado a una coordenada REAL del plano
                    // (SimLevelBuilder.CrisolX), no a una fracción del lienzo,
                    // así que se mudó SOLO con el crisol cuando el playtest 34
                    // se lo llevó a la izquierda. Es un rebote ESTÁTICO y
                    // flojo: la luz que late la ponen los halos de la máquina
                    // (Game/MaquinariaSprites.Luz), que sí saben si el fuego
                    // está encendido; esto solo dice "aquí, en esta pared,
                    // siempre ha habido un fuego delante".
                    float dfx = (celdaX - (SimLevelBuilder.CrisolX + 6)) / 30f;
                    float dfy = (celdaY - (SimLevelBuilder.CuartoY0 + 8)) / 17f;
                    float d2 = dfx * dfx + dfy * dfy;
                    if (d2 < 1f)
                    {
                        float k = (1f - d2) * (1f - d2);
                        c.r *= 1f + 0.40f * k;
                        c.g *= 1f + 0.24f * k;
                        c.b *= 1f + 0.09f * k;
                    }

                    // Viñeta de encuadre: efecto genérico en fracciones fijas
                    // del lienzo, no ligado a ninguna estructura del plano.
                    float nx = tx - 0.5f, ny = ty - 0.52f;
                    float vig = Mathf.Clamp01(1f - (nx * nx * 2.3f + ny * ny * 2.0f));
                    // (4ª pasada) Suelo de la viñeta 0.72 -> 0.80. Era el
                    // último 28% que hundía las esquinas del lienzo, y con la
                    // cámara siguiendo al aprendiz por un mundo de 768x288 las
                    // "esquinas del lienzo" no son las esquinas de la
                    // pantalla: son sitios donde de verdad se juega. Un
                    // encuadre se cierra con un 20%; con un 28% se apaga.
                    c *= 0.80f + 0.20f * vig;

                    // Grano fino: +-1.6% (era +-3%), rompe cualquier banda
                    // plana sin añadir ruido visible.
                    uint hg = HashRoca(x, y, 991u);
                    c *= 1f + ((hg & 31u) / 31f - 0.5f) * 0.032f;

                    px[y * TexW + x] = new Color(
                        Mathf.Clamp01(c.r),
                        Mathf.Clamp01(c.g),
                        Mathf.Clamp01(c.b), 1f);
                }

                rowsSinCeder++;
                if (rowsSinCeder >= RowsPerBatch)
                {
                    rowsSinCeder = 0;
                    yield return null;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);

            var go = new GameObject("WorkshopBackdrop_Sprite");
            var sr = go.AddComponent<SpriteRenderer>();
            float worldW = CellGrid.W * SimRenderer.CellWorldSize;
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, TexW, TexH), Vector2.zero, TexW / worldW, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = -10; // detrás del sprite de la simulación (-5).
            // (RONDA 68, dirección 2.5D: "fondo más oscuro, máquinas
            // claramente separadas del muro") El muro de fondo se HUNDE un
            // ~26% multiplicativo: todo lo que vive en el plano de juego
            // (materia, máquinas, aprendiz) salta hacia adelante gratis.
            sr.color = new Color(0.74f, 0.72f, 0.76f, 1f);
            RegistrarFondoParallax(go); // (ronda 70) escala con margen + base del parallax; sustituye al position=zero de antes.
        }

        // =================================================================
        // (playtest 33) LA SALA CRECIÓ: BÓVEDA, TEXTURAS POR ZONA, VIGAS Y
        // CLARABOYAS
        // =================================================================

        /// <summary>
        /// Fila hasta la que la pared del cuarto tiene que seguir pintándose:
        /// el techo recto (`CuartoY1`) más la flecha máxima de la bóveda (12)
        /// más el pozo de la claraboya (8) más holgura. Derivado del plano
        /// con un margen generoso en vez de exportar dos constantes más desde
        /// SimLevelBuilder: pintar 8 filas de sillería de más es gratis (la
        /// roca profunda las taparía igual en el sim) y equivocarse por
        /// defecto deja un agujero negro visible en la clave.
        /// </summary>
        private const int TechoMaximo = SimLevelBuilder.CuartoY1 + 24;

        /// <summary>La línea de ARRANQUE de la bóveda: el techo recto que talla `ExcavateCuarto`. Por debajo, muro; por encima, casquete.</summary>
        private const int ArranqueBoveda = SimLevelBuilder.CuartoY1;

        /// <summary>
        /// (playtest 34) EL APAREJO ÚNICO DEL TALLER. Cesar, jugando el 33:
        /// *"el fondo tiene 3 diseños: quiero UNO SOLO -- el que está detrás
        /// de la PRENSA es el que más me gusta"*.
        ///
        /// Es ese, literalmente: la hilada BAJA y APRETADA, casi ladrillo
        /// (7x3 celdas), que el 33 reservaba para el muro de contención de la
        /// zona de fuerza. Funciona mejor que los otros dos por una razón que
        /// se ve mejor jugando que razonando: una pieza pequeña repetida
        /// muchas veces da TEXTURA (el ojo la promedia y la lee como
        /// superficie), mientras que un sillar grande da OBJETOS -- y un
        /// fondo lleno de objetos compite con las máquinas, que es justo el
        /// "satura" del veredicto.
        ///
        /// LO QUE SE RETIRA (regla 15 de CLAUDE.md, documentar lo descartado):
        ///  · sillar GRANDE y curtido (11x6, pátina 0.26, sesgo cálido) para
        ///    la zona húmeda + fuego;
        ///  · sillar NOBLE ancho y regular (14x7, pátina 0.10, sesgo frío)
        ///    para la observación y el atrio;
        ///  · y con ellos el sesgo de TEMPERATURA DE COLOR por zona (+-4% en
        ///    R/B), que era la otra mitad de "tres diseños".
        /// La idea no era mala en sí (un taller viejo se remienda por zonas)
        /// pero pedía tres transiciones limpias detrás de tres nervios, y
        /// cualquier estación que se mude -- y en el 34 se mudaron TODAS --
        /// deja las costuras contando una zonificación que ya no existe.
        /// Un fondo no puede depender de dónde estén hoy las máquinas.
        ///
        /// La pátina baja además de 0.14 a **0.07**: es la variación de tono
        /// por pieza, y a 0.14 se veía el despiece desde el otro lado de la
        /// sala ("menos definido, que se sienta FONDO").
        /// </summary>
        private static void AparejoDelTaller(out int piezaAnchoC, out int piezaAltoC, out float patina)
        {
            piezaAnchoC = 7; piezaAltoC = 3; patina = 0.07f;
        }

        /// <summary>
        /// Centro (clave) del tramo de bóveda al que pertenece esta columna --
        /// lo necesita el abanico de dovelas. Los bordes de tramo son las
        /// pilastras del plano más las dos paredes: exactamente la misma
        /// partición que usa `SimLevelBuilder.TallarBoveda`, leída de la misma
        /// tabla pública (regla 39: la medida se lee, no se copia).
        /// </summary>
        private static int CentroDeTramo(int celdaX)
        {
            int izq = SimLevelBuilder.CuartoX0;
            int der = SimLevelBuilder.CuartoX1;
            var cols = SimLevelBuilder.PilastraColumnas;
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] <= celdaX) izq = cols[i];
                else { der = cols[i]; break; }
            }
            return (izq + der) / 2;
        }

        /// <summary>
        /// LAS VIGAS DE PIEDRA del fondo: bandas horizontales con canto claro
        /// y sombra proyectada debajo. Devuelve un multiplicador de brillo.
        ///
        /// (playtest 34) DOS CAMBIOS, los dos por feedback directo:
        ///  · CONTRASTE A LA MITAD LARGA (1.55/0.66/0.58 -> 1.13/0.90/0.86).
        ///    Cesar: *"hay unas LÍNEAS NEGRAS raras en la parte superior del
        ///    fondo: quítalas"*. Eran estas -- la banda de sombra de 4 filas
        ///    al 0.58 sobre una pared que ya es oscura no se lee como "una
        ///    viga proyecta sombra", se lee como una raya negra de punta a
        ///    punta del taller. La viga sigue estando (se necesita: es de
        ///    donde cuelgan las cadenas) pero ahora es FONDO.
        ///  · El parámetro `defin` la desvanece con la profundidad, igual que
        ///    el resto del aparejo: a 110 celdas del taller la viga se
        ///    disuelve en la roca en vez de seguir cruzando el mundo entero.
        /// </summary>
        private static float FactorViga(int celdaY, float defin)
        {
            // (playtest 34, SEGUNDA PASADA -- VISTO JUGANDO) LA VIGA PASA DE
            // SER SOMBRA A SER PIEDRA CLARA. La primera pasada bajó el
            // contraste de 1.55/0.66/0.58 a 1.13/0.90/0.86 para quitar "las
            // líneas negras raras" -- y funcionó demasiado bien: sobre una
            // pared de brillo 0.10 un +-10% es invisible, así que las tres
            // vigas desaparecieron... y con ellas se fue el requisito de
            // Cesar de que las CADENAS CUELGUEN DE ALGO DIBUJADO (obra C): en
            // la captura de la iteración 1 las cadenas colgaban del aire.
            // La corrección no es volver a subir el contraste hacia abajo
            // (eso reintroduce la raya negra) sino hacia ARRIBA: una viga es
            // un dintel de piedra LABRADA, más clara que el paño que
            // atraviesa. Se ve entera de punta a punta del taller y no hay
            // una sola banda oscura nueva en el cuadro.
            // `defin` al cuadrado: la viga se disuelve en la roca más deprisa
            // que la sillería, para no cruzar el mundo entero como tres
            // rayas.
            float k = defin * defin;
            for (int i = 0; i < _vigasY.Length; i++)
            {
                int v = _vigasY[i];
                int d = celdaY - v;
                if (d == 0 || d == 1) return 1f + 0.90f * k;   // el canto alto, iluminado: la línea que se ve desde lejos.
                if (d >= -3 && d <= -1) return 1f + 0.48f * k; // el frente de la viga, en penumbra pero todavía más claro que el muro.
                if (d >= -6 && d <= -4) return 1f - Mathf.Lerp(0.16f, 0.03f, (d + 6) / 3f) * k; // la sombra que proyecta: suave, nunca negra.
            }
            return 1f;
        }

        /// <summary>
        /// Alturas de las TRES vigas del fondo (el 33 tenía dos). Números del
        /// PLANO, no fracciones del lienzo -- misma disciplina que las vigas
        /// del taller clásico.
        ///  · [0] +50: la imposta baja, justo sobre la coronación de las
        ///    estaciones (la más alta, la Columna, remata en `CuartoY0`+47).
        ///  · [1] +88 y [2] `CuartoY1`-8: LAS DOS DEL TERCIO SUPERIOR. La
        ///    sala mide 127 celdas, así que su tercio alto arranca en
        ///    `CuartoY0`+85 -- las dos caen dentro, que es lo que pidió Cesar
        ///    (obra C del playtest 34): las cadenas cuelgan de estas dos y de
        ///    ninguna otra, ver <see cref="_cadenasViga"/>.
        /// La viga [1] es NUEVA: sin ella, entre la imposta baja y el
        /// arranque de la bóveda quedaban 76 celdas de paño liso -- el "muy
        /// apiñado abajo, vacío arriba" que el crecimiento de la sala habría
        /// empeorado en vez de arreglar.
        /// </summary>
        private static readonly int[] _vigasY =
        {
            SimLevelBuilder.CuartoY0 + 50,
            SimLevelBuilder.CuartoY0 + 88,
            SimLevelBuilder.CuartoY1 - 8,
        };

        /// <summary>
        /// EL HAZ FRÍO DE LAS CLARABOYAS -- **RETIRADO** (playtest 34, regla
        /// 15 de CLAUDE.md). Cesar: *"la LUZ de las claraboyas no está lista:
        /// rompo bedrock y queda EN EL AIRE -- QUÍTALA por ahora, luego
        /// volvemos"*. Este método pintaba el cono aditivo AZULADO sobre la
        /// pared del cuarto (el sprite que caía sobre las máquinas lo montaba
        /// `MontarHerrajesDelTaller`, también retirado). Se va por lo mismo
        /// que su gemelo: era luz sin dueño físico -- un degradado en
        /// coordenadas fijas que no sabe nada de la piedra que tiene delante,
        /// así que picar la bóveda dejaba el haz colgando de nada. La
        /// claraboya SIGUE dibujada, como VENTANA CIEGA con su marco (ver
        /// <see cref="ClasificarVano"/>): se retira la luz, no el hueco.
        /// Lo que había, para cuando se retome (necesitará además partículas,
        /// que el propio Cesar identificó como la pieza que falta):
        ///   cono de media anchura `ClaraboyaAncho*0.5+2+caida*0.25`, caída
        ///   0..40 celdas bajo la clave, aporte aditivo
        ///   (0.090, 0.126, 0.185) * lateral * Lerp(0.34, 0.02, prof^0.7).
        /// </summary>
        // private static void AplicarClaraboya(ref Color c, int celdaX, int celdaY) { ... }

        /// <summary>
        /// (playtest 34) LAS VENTANAS DEL FONDO, CON MARCO PROPIO.
        ///
        /// Cesar, jugando el 33: *"el trabado de ladrillos NO debe atravesar
        /// las ventanas"*. Tenía razón y el defecto era estructural: hasta
        /// ahora las hornacinas (`FactorNicho`, retirado y absorbido aquí)
        /// eran un MULTIPLICADOR DE BRILLO aplicado ENCIMA de la sillería ya
        /// dibujada -- o sea que las hiladas, las juntas y el bisel seguían
        /// corriendo por dentro del hueco de lado a lado, como si el muro no
        /// se hubiera interrumpido. Un vano en el que se ve el aparejo del
        /// muro no es un vano: es una mancha.
        ///
        /// La corrección es de ORDEN, no de color: el llamante pregunta AQUÍ
        /// primero y solo dibuja sillería si la respuesta es 0. Un vano tiene
        /// entonces tres capas propias, como en obra real:
        ///   · 1 = MARCO -- jamba, dintel y alféizar: piedra labrada LISA, sin
        ///     una sola junta, un punto más clara que el paño.
        ///   · 2 = VANO -- el plano ciego del fondo, liso, que se hunde con un
        ///     degradado propio (`k`: 0 en el borde, 1 en el centro-alto del
        ///     hueco). Nunca negro: la lección de la tercera pasada del
        ///     playtest 31 es que un hueco se lee por el CONTRASTE DE SU
        ///     CANTO, no por ser un agujero.
        ///
        /// DOS FAMILIAS DE VENTANA, las dos con el mismo tratamiento:
        ///  a) HORNACINAS (<see cref="_centrosNicho"/>): en el paño libre
        ///     entre estaciones, con arco de medio punto arriba. Suben a
        ///     `CuartoY0`+58..+76 -- con la sala 32 celdas más alta y las
        ///     máquinas rematando en +47, la franja del 33 (+49..+67) volvía a
        ///     caer sobre las coronaciones, que es el error que la segunda
        ///     pasada del 31 ya corrigió una vez.
        ///  b) CLARABOYAS CIEGAS (`SimLevelBuilder.ClaraboyaColumnas`): el
        ///     pozo que `TallarBoveda` talla de verdad en la clave. Se dibuja
        ///     como un lucernario alto y estrecho por encima del arranque, y
        ///     es lo ÚNICO que queda de ellas ahora que su luz se retiró.
        /// </summary>
        /// <param name="k">Solo con retorno 2: 0 en el canto del vano, 1 en su fondo.</param>
        private static int ClasificarVano(int celdaX, int celdaY, out float k)
        {
            k = 0f;

            // ---- a) HORNACINAS, EN DOS PISOS -----------------------------
            // (playtest 34, SEGUNDA PASADA -- VISTO JUGANDO) La sala mide
            // ahora 127 celdas y las máquinas rematan en `CuartoY0`+47: entre
            // ellas y el arranque de la bóveda quedaban SETENTA celdas de paño
            // liso. Una sola fila de hornacinas ahí es una cornisa perdida en
            // mitad de un descampado. Van dos, una entre cada par de vigas
            // (`_vigasY` = +50 / +88 / `CuartoY1`-8), que es como se ordena de
            // verdad una fachada: entrepaños entre impostas, no huecos
            // sueltos flotando.
            int piso = ClasificarPisoNicho(celdaX, celdaY, out int ny0, out int ny1, out int centro);
            if (piso != 0)
            {
                int d = celdaX - centro;
                if (d < 0) d = -d;

                // El arco: en las 4 filas de arriba el vano se estrecha,
                // así que la hornacina termina en curva y no en un
                // rectángulo de cartón.
                int desdeArriba = ny1 - celdaY;
                int anchoAqui = desdeArriba >= 4 ? NichoMediaAncho
                              : NichoMediaAncho - (4 - desdeArriba) * 2;

                if (celdaY < ny0 || celdaY > ny1 || anchoAqui < 0 || d > anchoAqui)
                    return 1; // fuera del hueco pero dentro de la orla: es el MARCO.

                // Dentro del hueco: profundidad del plano ciego.
                float lateral = 1f - (d / (float)(anchoAqui + 1));
                float vertical = Mathf.Clamp01((celdaY - ny0) / (float)(ny1 - ny0 + 1));
                k = Mathf.Clamp01(lateral * (0.45f + 0.55f * vertical));
                return 2;
            }

            // ---- b) CLARABOYAS CIEGAS ------------------------------------
            int cy0 = ArranqueBoveda + 3;
            int cy1 = ArranqueBoveda + 21;
            if (celdaY >= cy0 - 2 && celdaY <= cy1 + 2)
            {
                var cols = SimLevelBuilder.ClaraboyaColumnas;
                int media = SimLevelBuilder.ClaraboyaAncho / 2 + 1;
                for (int i = 0; i < cols.Length; i++)
                {
                    int d = celdaX - cols[i];
                    if (d < 0) d = -d;
                    if (d > media + 2) continue;

                    if (celdaY < cy0 || celdaY > cy1 || d > media) return 1; // jamba/dintel/alféizar.

                    float lateral = 1f - (d / (float)(media + 1));
                    // (3ª pasada) La claraboya invierte el degradado respecto
                    // a una hornacina, y por una razón física: una hornacina
                    // es un nicho ciego (cuanto más adentro, más sombra),
                    // pero un lucernario es un POZO que mira hacia fuera --
                    // el fondo está ARRIBA y es de donde vendría la luz. Con
                    // el degradado de hornacina, el pozo se leía como un
                    // agujero negro clavado en la clave (comprobado en la
                    // captura de la iteración 2).
                    float vertical = Mathf.Clamp01((cy1 - celdaY) / (float)(cy1 - cy0 + 1));
                    k = Mathf.Clamp01(lateral * (0.25f + 0.55f * vertical));
                    return 2;
                }
            }

            return 0;
        }

        /// <summary>
        /// (playtest 34) ¿Cae (x,y) en la orla de alguna hornacina, y de cuál?
        /// Devuelve 0 si no, o el número de piso (1 = el bajo, 2 = el alto) y
        /// por `out` la banda vertical y el centro de la hornacina concreta.
        /// Separado de <see cref="ClasificarVano"/> para que ese método no
        /// tenga dos bucles anidados idénticos: la geometría del hueco (arco,
        /// marco, fondo) es la MISMA en los dos pisos, solo cambia dónde.
        /// </summary>
        private static int ClasificarPisoNicho(int celdaX, int celdaY, out int ny0, out int ny1, out int centro)
        {
            for (int piso = 0; piso < 2; piso++)
            {
                ny0 = SimLevelBuilder.CuartoY0 + (piso == 0 ? 58 : 94);
                ny1 = ny0 + 18;
                if (celdaY < ny0 - 2 || celdaY > ny1 + 2) continue;

                int[] centros = piso == 0 ? _centrosNicho : _centrosNichoAlto;
                for (int i = 0; i < centros.Length; i++)
                {
                    int d = celdaX - centros[i];
                    if (d < 0) d = -d;
                    if (d > NichoMediaAncho + 2) continue;
                    centro = centros[i];
                    return piso + 1;
                }
            }
            ny0 = 0; ny1 = 0; centro = 0;
            return 0;
        }

        /// <summary>Media anchura del hueco de una hornacina, en celdas.</summary>
        private const int NichoMediaAncho = 7;

        /// <summary>
        /// Centros de las hornacinas, en los PUNTOS MEDIOS entre las
        /// pilastras del plano (`SimLevelBuilder.PilastraColumnas` =
        /// 133/185/244/284/331 desde el playtest 34): pilastra, hornacina,
        /// pilastra, hornacina = el ritmo de una crujía de verdad -- una
        /// hornacina en la misma columna que una pilastra se estorban, algo
        /// que ya costó una iteración en el 31.
        /// Sitios elegidos: 108 (crujía del crisol), 159 (crisol|prensa), 308
        /// (alcoba, entre columna y chispa) y 355 (atrio del Maestro). El
        /// punto medio 214 se cede a la CLARABOYA de la isla de fuentes y el
        /// 264 a la de la alcoba: dos huecos oscuros compitiendo con un
        /// lucernario en el mismo lienzo es el ruido que la tercera pasada del
        /// 31 vino a quitar.
        /// </summary>
        private static readonly int[] _centrosNicho = { 108, 159, 308, 355 };

        /// <summary>
        /// (playtest 34) EL PISO ALTO de hornacinas, a `CuartoY0`+94..+112 --
        /// entre la viga media y la alta. Va DESPLAZADO respecto al piso bajo
        /// (cae sobre las pilastras, no entre ellas) a propósito: dos filas de
        /// huecos alineados en vertical se leen como una rejilla; alternadas,
        /// como una fachada. Sitios: 133 y 185 (las dos pilastras de la banda
        /// que transforma), 244 (la de la escalinata) y 331 (la del atrio) --
        /// las columnas 284 y las tres claraboyas se dejan libres para no
        /// amontonar huecos en la alcoba de observación.
        /// </summary>
        private static readonly int[] _centrosNichoAlto = { 133, 185, 244, 331 };

        /// <summary>
        /// (playtest 31) Hash entero estable para la veta y el grano de la
        /// roca del fondo. No usa UnityEngine.Random (regla de oro del
        /// proyecto) ni XorShift.FromCell (que está pensado para (tick,x,y)
        /// del hot path de la sim): esto se ejecuta UNA vez, en la corrutina
        /// de arranque, y solo necesita ser determinista y barato.
        /// </summary>
        private static uint HashRoca(int x, int y, uint sal)
        {
            uint h = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ sal;
            h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            return h;
        }

        /// <summary>
        /// (playtest 4..19) Fondo del TALLER CLÁSICO: mampostería con
        /// hiladas/piezas, dos vigas, zócalo y halo de fragua anclado a
        /// CULTIVO. Preservado íntegro por la regla 26 (no encoger sin
        /// justificación) pero SIN llamar desde <see cref="Start"/> en el
        /// pivot (playtest 21) -- ver el bloque "EL CUARTO ÍNTIMO" en el doc
        /// de clase. Si el taller clásico vuelve a ser jugable algún día,
        /// basta con hacer que <see cref="Start"/> llame aquí en vez de a
        /// <see cref="PintarFondoCuartoIntimo"/>.
        /// </summary>
        private IEnumerator PintarFondoTallerClasico()
        {
            var tex = new Texture2D(TexW, TexH, TextureFormat.RGBA32, false)
            {
                name = "TenThousandYearsWorkshopBackdrop",
                // (fix playtest 7) Bilinear sobre un fondo pixel-art contra un sim y
                // una maquinaria en Point: el fondo se veía borroso y el conjunto
                // "descuadrado". Point casa los tres planos del cuadro.
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var px = new Color32[TexW * TexH];

            // Paleta: ciruela oscuro arriba -> pardo casi negro abajo. Todo muy
            // desaturado: la materia de la simulación es lo único saturado.
            // (Paleta validada en playtest 7: NO tocar los valores, solo la
            // resolución/nitidez con la que se pintan.)
            var arriba = new Color(0.150f, 0.115f, 0.190f);
            var abajo = new Color(0.062f, 0.048f, 0.058f);
            var junta = new Color(0.040f, 0.032f, 0.042f);
            var mordido = new Color(0.028f, 0.022f, 0.030f);
            var rescoldo = new Color(0.30f, 0.15f, 0.09f);
            var maderaViga = new Color(0.135f, 0.088f, 0.062f);
            var maderaLuz = new Color(0.195f, 0.132f, 0.090f);

            // Vigas: la baja queda PEGADA por debajo de los estantes (así se lee
            // que los estantes descansan sobre ella y dejan de parecer losas
            // flotando en el vacío); la alta cruza el cielo del taller, que es
            // donde flota el HUD, para que no sea un rectángulo negro.
            // (playtest 15) ChillTrayY0/CellGrid.H ya son las constantes NUEVAS
            // del plano rediseñado (246 y 288 respectivamente, no las de antes
            // de la reingeniería del espacio) -- al leerlas por nombre en vez de
            // copiar el número, la viga baja sigue "pegada" al estante nuevo sin
            // tocar nada aquí.
            // (playtest 19, "EL TALLER SE COMPACTA AÚN MÁS", ver
            // Sim/SimLevelBuilder.cs) ChillTrayY0 bajó de 246 a 236 -- al leerse
            // por nombre, esta viga vuelve a seguir al estante SOLA, sin tocar
            // nada aquí: ahora cae en la celda 231..235 (tocando el estante en
            // 236), justo debajo del nuevo hueco donde flotan la bandeja fría y
            // el estante, encima del pilar de grifos. Efecto lateral BUENO, no
            // buscado a propósito pero coherente: como el pilar de grifos es
            // sólido hasta la celda 232 (TapPillarTopY) en esa misma franja de
            // X, la mitad izquierda de esta viga queda oculta tras la piedra del
            // propio taller (el fondo se dibuja DETRÁS, sortingOrder -10) y solo
            // se ve asomar a la derecha del pilar, exactamente donde el estante
            // sí flota en aire libre -- "la viga sostiene lo que de verdad
            // flota" en vez de dibujarse entera sobre piedra que ya se sostiene
            // sola.
            int vigaBajaY = (SimLevelBuilder.ChillTrayY0 - 5) * Escala;   // celda 231..235, tocando el estante en 236
            int vigaAltaY = (CellGrid.H - 18) * Escala;                  // celda 270
            const int vigaGrosorCeldas = 5;
            int vigaGrosor = vigaGrosorCeldas * Escala;
            const int mensulaPeriodoCeldas = 48;
            const int mensulaAnchoCeldas = 3;
            int mensulaPeriodo = mensulaPeriodoCeldas * Escala;
            int mensulaAncho = mensulaAnchoCeldas * Escala;

            // Zócalo: sillares grandes al nivel del suelo de piedra.
            // (playtest 15) ANTES esto era `(FloorHeight + 6) * Escala`: correcto
            // mientras FloorHeight (10) era el suelo del ÚNICO piso del taller.
            // Ahora FloorHeight sostiene el SÓTANO (bajo tierra, no el taller
            // visible) y el suelo de verdad de CULTIVO/LABORATORIO/ENTREGA es
            // SurfaceFloorY0 (144) -- con el número viejo el zócalo se quedaba
            // pegado al fondo del sótano, 134 celdas por debajo de donde el
            // aprendiz realmente pisa. Efecto lateral BUENO de este fix, dejado
            // a propósito: como `enZocalo` sigue siendo verdad para TODO y<zocaloTop,
            // ahora la mitad inferior entera del lienzo (sótano + bedrock) queda
            // pintada con el sillar GRANDE de zócalo en vez del ladrillo fino de
            // taller -- lee como "piedra de cimentación pesada" bajo tierra,
            // que es exactamente la sensación que le falta a una sala subterránea
            // sin tener que diseñar una paleta de cueva aparte (fuera de alcance
            // de este encargo; documentado aquí por si alguien quiere darle más
            // carácter al sótano en una ronda futura).
            int zocaloTop = (SimLevelBuilder.SurfaceFloorY0 + 6) * Escala;   // celda 150

            int rowsSinCeder = 0;

            for (int y = 0; y < TexH; y++)
            {
                float ty = y / (float)(TexH - 1);
                Color fila = Color.Lerp(abajo, arriba, ty);

                bool enViga = (y >= vigaBajaY && y < vigaBajaY + vigaGrosor)
                           || (y >= vigaAltaY && y < vigaAltaY + vigaGrosor);
                bool enZocalo = y < zocaloTop;

                int altoPieza = enZocalo ? PiezaAlto * 2 : PiezaAlto;
                int anchoPieza = enZocalo ? PiezaAncho * 2 : PiezaAncho;

                int hilada = y / altoPieza;
                // Aparejo a soga: cada hilada desplazada media pieza.
                int desfase = (hilada & 1) == 0 ? 0 : anchoPieza / 2;
                int ly = y % altoPieza; // fila local dentro de la pieza (en téxeles)
                // (fix playtest 7) La junta horizontal ahora es SIEMPRE 1 téxel
                // (ly == 0), sea cual sea Escala, en vez de 1 celda entera.
                bool juntaHorizontal = ly == 0;
                // Fila justo debajo de la junta horizontal: es a la vez "el mortero
                // recibe luz" (punto 4 del brief) y el canto superior de la pieza que
                // sigue, iluminado (punto 3, mismo lenguaje que StaticSolid en
                // Sim/SimRenderer.cs) — son la misma fila física, así que se resuelven
                // con un único bloque.
                bool cantoSuperior = ly == 1;
                bool cantoInferior = ly == altoPieza - 1;

                for (int x = 0; x < TexW; x++)
                {
                    Color c;

                    if (enViga)
                    {
                        // Viga de madera: cara oscura con canto iluminado arriba
                        // y ménsulas verticales cada 48 celdas (ahora en téxeles).
                        int dy = y - (y >= vigaAltaY ? vigaAltaY : vigaBajaY);
                        c = dy >= vigaGrosor - 1 ? maderaLuz : maderaViga;
                        if ((x % mensulaPeriodo) < mensulaAncho) c = Color.Lerp(c, maderaLuz, 0.35f); // ménsula
                        if (dy == 0) c *= 0.55f;                               // sombra proyectada
                    }
                    else
                    {
                        int sx = x + desfase;
                        int pieza = sx / anchoPieza;
                        int lx = sx % anchoPieza;
                        bool juntaVertical = lx == 0;

                        // Variación de tono estable por pieza: ±7%. Suficiente
                        // para que la pared "tenga piezas" sin llamar la atención.
                        uint hPieza = Hash(pieza, hilada);
                        float var01 = ((hPieza & 255) / 255f - 0.5f) * 0.14f;
                        c = fila * (1f + var01);

                        // Bisel de canto (fix playtest 7): mismo lenguaje que la
                        // piedra del sim (StaticSolid) — canto superior aclarado
                        // ~18%, canto inferior oscurecido ~15%. Antes la pieza era
                        // un tono plano sin ningún indicio de volumen.
                        if (cantoSuperior) c *= 1.18f;
                        else if (cantoInferior) c *= 0.85f;

                        // Pieza suelta desconchada (~1 de cada 8, hash estable por
                        // pieza): una esquina mordida un tono más oscuro. Rompe la
                        // monotonía de la sillería sin subir la saturación.
                        bool desconchada = (hPieza & 0x7) == 0;
                        if (desconchada && lx >= 1 && ly >= 1)
                        {
                            int distEsquina = (lx - 1) + (ly - 1); // esquina superior-izquierda de la pieza
                            const int mordiscoTexels = 4;
                            if (distEsquina < mordiscoTexels) c = Color.Lerp(c, mordido, 0.6f);
                        }

                        // Grano de piedra de alta frecuencia (fix playtest 7): hash
                        // POR TÉXEL, no por celda — con Escala=3 hay 9 téxeles por
                        // celda para aprovechar, y eso es lo que separa "piedra" de
                        // "mancha plana". ±5% multiplicativo.
                        uint hTexel = Hash(x, y);
                        float grano01 = ((hTexel & 255) / 255f - 0.5f) * 0.10f; // ±5%
                        c *= 1f + grano01;

                        // Junta más fina y suave (fix playtest 7): 1 téxel, -55%
                        // en vez de -75%. Nada de rejilla dura.
                        if (juntaHorizontal || juntaVertical) c = Color.Lerp(c, junta, 0.55f);
                    }

                    // Viñeta: cierra el encuadre y evita que las esquinas compitan.
                    // (playtest 15) Sigue en fracciones fijas del lienzo entero A
                    // PROPÓSITO: es un efecto de ENCUADRE genérico, no ligado a
                    // ninguna zona concreta del plano, así que no necesita
                    // derivarse de SimLevelBuilder como sí lo necesitan la viga/
                    // el zócalo/la fragua (que sí representan estructuras reales
                    // con coordenadas reales).
                    float tx = x / (float)(TexW - 1);
                    float nx = tx - 0.5f, ny = ty - 0.52f;
                    float vig = Mathf.Clamp01(1f - (nx * nx * 2.3f + ny * ny * 2.0f));
                    c *= 0.62f + 0.38f * vig;

                    // Luz de fragua a la altura de las cubas (donde se juega).
                    // (playtest 15) Centro/radio derivados de CULTIVO -- ver
                    // FraguaCxFrac/CyFrac/RadioX/RadioY en el doc de clase.
                    float gx = (tx - FraguaCxFrac) / FraguaRadioX;
                    float gy = (ty - FraguaCyFrac) / FraguaRadioY;
                    float halo = Mathf.Clamp01(1f - (gx * gx + gy * gy));
                    c += rescoldo * (halo * halo);

                    px[y * TexW + x] = new Color(
                        Mathf.Clamp01(c.r),
                        Mathf.Clamp01(c.g),
                        Mathf.Clamp01(c.b), 1f);
                }

                // (playtest 15, ver "COSTE Y GENERACIÓN POR TROZOS" en el doc de
                // clase) Cede el control a Unity cada RowsPerBatch filas en vez de
                // pintar el lienzo entero en un único frame -- el mundo x6 hace
                // que esa única pasada sea ~2M iteraciones, suficiente para
                // congelar el primer frame de la partida.
                rowsSinCeder++;
                if (rowsSinCeder >= RowsPerBatch)
                {
                    rowsSinCeder = 0;
                    yield return null;
                }
            }

            tex.SetPixels32(px);
            tex.Apply(false, true);

            var go = new GameObject("WorkshopBackdrop_Sprite");
            var sr = go.AddComponent<SpriteRenderer>();
            float worldW = CellGrid.W * SimRenderer.CellWorldSize;
            // (fix build) SpriteRenderer en vez de quad+URP/Unlit: ese shader se eliminaba de la build.
            // (fix playtest 7) TexW ahora es CellGrid.W * Escala, así que pixelsPerUnit
            // (TexW / worldW) sube en la misma proporción: el sprite sigue midiendo
            // EXACTAMENTE CellGrid.W/H * CellWorldSize unidades de mundo (76.8 x 28.8
            // tras el playtest 15), solo cambia la densidad de téxeles por unidad.
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, TexW, TexH), Vector2.zero, TexW / worldW, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = -10; // detrás del sprite de la simulación (-5).
            // (RONDA 68, dirección 2.5D: "fondo más oscuro, máquinas
            // claramente separadas del muro") El muro de fondo se HUNDE un
            // ~26% multiplicativo: todo lo que vive en el plano de juego
            // (materia, máquinas, aprendiz) salta hacia adelante gratis.
            sr.color = new Color(0.74f, 0.72f, 0.76f, 1f);
            RegistrarFondoParallax(go); // (ronda 70) escala con margen + base del parallax; sustituye al position=zero de antes.
        }

        private static uint Hash(int a, int b)
        {
            unchecked
            {
                uint h = (uint)a * 374761393u + (uint)b * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                return h ^ (h >> 16);
            }
        }
    }
}
