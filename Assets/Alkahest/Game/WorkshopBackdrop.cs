using System.Collections;
using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// [ChaosAlchemy] Pared de fondo del taller: un sprite opaco DETRÁS del
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
            yield return PintarFondoCuartoIntimo();
        }

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
                name = "ChaosAlchemyWorkshopBackdrop",
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
            // ROCA PROFUNDA (fuera del cuarto): casi negra. Es lo que se ve
            // por el pasillo de la Tolva y por los bordes del encuadre.
            var techo = new Color(0.030f, 0.027f, 0.037f);
            var suelo = new Color(0.052f, 0.041f, 0.037f);

            // LA PARED DEL CUARTO (dentro de CuartoX0..X1 / Y0..Y1). Mucho
            // más clara que la roca profunda -- no por realismo, sino porque
            // ANTES DE ESTA RONDA el taller entero flotaba sobre un vacío
            // negro (visto jugando, captura de la iteración 1): las máquinas
            // no tenían pared detrás, así que ninguna parecía estar DENTRO de
            // un sitio. Una habitación se lee cuando tiene fondo.
            // (tercera pasada, visto jugando) +15% en las dos: con el tinte
            // global cálido de Sim/SimRenderer.TinteGlobal encima, la pared
            // quedaba por debajo del umbral en que se distingue la sillería a
            // escala de juego -- se veía "oscuro" pero no "de piedra".
            var paredAlta = new Color(0.101f, 0.090f, 0.099f);  // arriba: la bóveda se apaga.
            var paredBaja = new Color(0.173f, 0.136f, 0.110f);  // abajo: pardo cálido, la luz de los fuegos rebota en el zócalo.

            int rowsSinCeder = 0;

            for (int y = 0; y < TexH; y++)
            {
                float ty = y / (float)(TexH - 1);
                // Base vertical: en coordenadas de textura y=0 es ABAJO.
                Color baseFila = Color.Lerp(suelo, techo, Mathf.Pow(ty, 0.85f));
                int bloqueY = y / (Escala * 8); // veta de ~8 celdas de alto.

                int celdaY = y / Escala;
                bool filaCuarto = celdaY >= SimLevelBuilder.CuartoY0 - 2 && celdaY <= SimLevelBuilder.CuartoY1 + 2;
                float tCuarto = filaCuarto
                    ? Mathf.Clamp01((celdaY - SimLevelBuilder.CuartoY0) / (float)(SimLevelBuilder.CuartoY1 - SimLevelBuilder.CuartoY0))
                    : 0f;
                Color paredFila = Color.Lerp(paredBaja, paredAlta, Mathf.Pow(tCuarto, 0.75f));

                for (int x = 0; x < TexW; x++)
                {
                    float tx = x / (float)(TexW - 1);
                    int celdaX = x / Escala;
                    bool enCuarto = filaCuarto
                        && celdaX >= SimLevelBuilder.CuartoX0 - 2 && celdaX <= SimLevelBuilder.CuartoX1 + 2;

                    Color c;
                    if (enCuarto)
                    {
                        c = paredFila;

                        // --- SILLERÍA de la pared: piezas de 9x5 celdas a
                        // soga corrida (hiladas alternas desplazadas media
                        // pieza), con junta fina y BISEL -- el mismo lenguaje
                        // de canto claro arriba / oscuro abajo que usa
                        // SimRenderer.ComputeCellColor para la piedra de la
                        // sim, para que fondo y primer plano rimen en vez de
                        // contradecirse (lección del playtest 7).
                        const int piezaW = 9 * Escala, piezaH = 5 * Escala;
                        int hilada = y / piezaH;
                        int desfase = (hilada & 1) == 1 ? piezaW / 2 : 0;
                        int lx = ((x + desfase) % piezaW + piezaW) % piezaW;
                        int ly = y % piezaH;
                        uint hp = HashRoca((x + desfase) / piezaW, hilada, 5171u);
                        float tono = 1f + ((hp & 63u) / 63f - 0.5f) * 0.18f; // variación de tono por pieza (pátina de la pared).
                        c *= tono;

                        if (lx == 0 || ly == 0) c *= 0.55f;                       // junta de mortero.
                        else if (ly >= piezaH - 2) c *= 1.16f;                    // canto superior: le da la luz.
                        else if (ly <= 2) c *= 0.80f;                             // canto inferior: en sombra.

                        // --- NICHOS: hornacinas de sombra excavadas en la
                        // pared, entre estación y estación (posiciones
                        // derivadas del PLANO real, no fracciones del lienzo
                        // -- misma disciplina que la viga/zócalo del taller
                        // clásico, ver el docblock de la clase).
                        c *= FactorNicho(celdaX, celdaY);

                        // --- ZÓCALO: las 5 primeras celdas sobre el suelo,
                        // más oscuras y sin sillería fina: el arranque del
                        // muro, que es lo que hace que el suelo "nazca de
                        // algo".
                        int sobreSuelo = celdaY - (SimLevelBuilder.CuartoY0 + 3);
                        if (sobreSuelo >= 0 && sobreSuelo < 5) c *= Mathf.Lerp(0.62f, 1f, sobreSuelo / 5f);

                        // --- CORNISA: dos celdas bajo el techo, con luz.
                        if (celdaY >= SimLevelBuilder.CuartoY1 - 2) c *= 1.25f;

                        // --- EL REBOTE DE LA FRAGUA: la pared detrás del
                        // crisol recibe su luz. Es el mismo principio que el
                        // halo de fragua del taller clásico (ver el docblock
                        // de la clase) y, como aquel, va anclado a una
                        // coordenada REAL del plano -- la del horno
                        // (SimLevelBuilder.CrisolX), no a una fracción del
                        // lienzo. Es un rebote ESTÁTICO y flojo: la luz que
                        // late la ponen los halos de la máquina
                        // (Game/MaquinariaSprites.Luz), que sí saben si el
                        // fuego está encendido; esto solo dice "aquí, en esta
                        // pared, siempre ha habido un fuego delante".
                        float dfx = (celdaX - (SimLevelBuilder.CrisolX + 18)) / 42f;
                        float dfy = (celdaY - (SimLevelBuilder.CuartoY0 + 10)) / 30f;
                        float d2 = dfx * dfx + dfy * dfy;
                        if (d2 < 1f)
                        {
                            float k = (1f - d2) * (1f - d2);
                            c.r *= 1f + 0.42f * k;
                            c.g *= 1f + 0.26f * k;
                            c.b *= 1f + 0.10f * k;
                        }
                    }
                    else
                    {
                        c = baseFila;
                        // Veta de roca profunda: bloques irregulares de ~10x8 celdas, ±6%.
                        uint hv = HashRoca(x / (Escala * 10), bloqueY, 7411u);
                        c *= 1f + ((hv & 63u) / 63f - 0.5f) * 0.12f;
                    }

                    // Viñeta de encuadre: mismo lenguaje que la del taller
                    // clásico (fracción fija del lienzo entero, efecto de
                    // encuadre genérico, no ligado a ninguna estructura real
                    // del plano) pero sin el halo de fragua que la acompañaba.
                    float nx = tx - 0.5f, ny = ty - 0.52f;
                    float vig = Mathf.Clamp01(1f - (nx * nx * 2.3f + ny * ny * 2.0f));
                    c *= 0.72f + 0.28f * vig;

                    // Grano fino: ±3%, rompe cualquier banda plana.
                    uint hg = HashRoca(x, y, 991u);
                    c *= 1f + ((hg & 31u) / 31f - 0.5f) * 0.06f;

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
            go.transform.position = Vector3.zero;
        }

        /// <summary>
        /// (playtest 31) LAS HORNACINAS. Cuatro nichos de sombra excavados en
        /// la pared del cuarto, en los HUECOS entre estaciones (derivados de
        /// las anclas reales del plano: entre las fuentes y el crisol, entre
        /// el crisol y la prensa, entre la columna y la chispa, y sobre el
        /// ensayo). No son decoración gratuita: son lo que convierte una
        /// pared corrida -- "todo es lineal", dijo Cesar -- en una pared con
        /// tramos, que es como se lee la profundidad en un decorado 2D.
        /// Devuelve un multiplicador de brillo: 1 fuera del nicho, ~0.45 en
        /// su fondo, con un canto CLARO en el borde superior (la piedra que
        /// sobresale recibe luz) para que se lea hueco y no mancha.
        /// </summary>
        private static float FactorNicho(int celdaX, int celdaY)
        {
            // Centros en X (celdas del plano) y media anchura.
            // 182: hueco fuentes->crisol · 236: crisol->prensa ·
            // 292: columna->chispa · 350: tras el ensayo, junto al pasillo.
            // (segunda pasada, VISTO JUGANDO) Las hornacinas estaban a
            // CuartoY0+12..+30 y NO SE VEÍAN NINGUNA: esa banda es
            // exactamente la altura de las estaciones (20-35 celdas desde el
            // suelo), así que las cuatro quedaban tapadas por el crisol, la
            // prensa, la columna y el altar. La pared LIBRE de este cuarto es
            // la de arriba -- de la coronación de las máquinas al techo --,
            // así que ahí suben. Es también donde funcionan mejor: una
            // hornacina alta se lee como respiradero de la cueva.
            int y0 = SimLevelBuilder.CuartoY0 + 44;
            int y1 = SimLevelBuilder.CuartoY0 + 62;

            if (celdaY < y0 || celdaY > y1) return 1f;

            int mejorDist = int.MaxValue;
            int[] centros = _centrosNicho;
            for (int i = 0; i < centros.Length; i++)
            {
                int d = celdaX - centros[i];
                if (d < 0) d = -d;
                if (d < mejorDist) mejorDist = d;
            }

            const int mediaAncho = 7;
            if (mejorDist > mediaAncho + 1) return 1f;

            // El arco: en las 4 filas de arriba el nicho se estrecha, así que
            // la hornacina termina en curva y no en un rectángulo de cartón.
            int desdeArriba = y1 - celdaY;
            int anchoAqui = desdeArriba >= 4 ? mediaAncho : mediaAncho - (4 - desdeArriba);
            if (anchoAqui < 0) return 1f;

            if (mejorDist > anchoAqui) return 1f;
            // (TERCERA PASADA, VISTO JUGANDO) 0.34 de fondo sobre una pared
            // que ya es oscura daba un RECTÁNGULO NEGRO: no se leía como
            // hueco excavado sino como textura que falta -- y encima caía
            // justo debajo de una pilastra, así que el conjunto parecía una
            // bandera colgada del techo. Un hueco en penumbra se lee por el
            // CONTRASTE DE SU CANTO, no por ser negro: se sube el fondo a
            // 0.72 (se hunde, no desaparece) y se baja el canto a 1.25.
            if (mejorDist == anchoAqui || celdaY == y0) return 1.25f; // canto iluminado del vano.
            return 0.72f; // fondo de la hornacina: se hunde, no es un agujero.
        }

        // (tercera pasada) Desplazados a los PUNTOS MEDIOS entre las
        // pilastras de Sim/SimLevelBuilder.PilastraColumnas (182/236/292/350):
        // hornacina y pilastra en la misma columna se estorbaban. Ahora se
        // alternan -- pilastra, hornacina, pilastra, hornacina -- que es el
        // ritmo de una crujía de verdad.
        private static readonly int[] _centrosNicho = { 209, 264, 321 };

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
                name = "ChaosAlchemyWorkshopBackdrop",
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
            go.transform.position = Vector3.zero;
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
