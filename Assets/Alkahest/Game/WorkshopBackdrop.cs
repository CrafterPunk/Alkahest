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
    ///   · Escala x3 (768x432): 9 téxeles por celda en vez de 1, sitio de sobra
    ///     para bisel + grano + esquinas mordidas dentro de cada pieza.
    ///   · FilterMode.Point: casa con Sim/SimRenderer.cs y con MaquinariaSprites.
    ///   · Pieza más pequeña (10x5 celdas en vez de 16x7) con el MISMO lenguaje
    ///     de iluminación de canto que SimRenderer.ComputeCellColor usa para
    ///     StaticSolid (bisel superior claro / inferior oscuro): fondo y
    ///     primer plano ahora "riman" en vez de contradecirse.
    ///   · Junta de 1 TÉXEL (no 1 celda) y menos oscura (55% en vez de 75%):
    ///     mortero fino, no rejilla dura.
    /// La textura sigue midiendo EXACTAMENTE el tamaño de la grilla en téxeles
    /// (ahora Escala téxeles por celda), así que las hiladas y vigas se pueden
    /// seguir situando en coordenadas del plano (Sim/SimLevelBuilder.cs)
    /// multiplicadas por Escala.
    /// </summary>
    public sealed class WorkshopBackdrop : MonoBehaviour
    {
        // (fix playtest 7) 1 téxel/celda hacía ladrillos de pantalla ENORMES y sin
        // detalle interior. A x3 hay 9 téxeles por celda: suficiente para bisel,
        // grano de alta frecuencia y esquinas mordidas sin que la textura sea cara
        // de generar (768x432 = 331.776 téxeles, una sola vez en Start).
        private const int Escala = 3;
        private const int TexW = CellGrid.W * Escala;  // 768
        private const int TexH = CellGrid.H * Escala;  // 432

        // Mampostería: pieza de 10x5 CELDAS (antes 16x7 celdas a 1 téxel/celda,
        // es decir, ladrillos casi del doble de grandes y sin margen para
        // detalle interior). En téxeles: 30x15.
        private const int PiezaAnchoCeldas = 10;
        private const int PiezaAltoCeldas = 5;
        private const int PiezaAncho = PiezaAnchoCeldas * Escala; // 30
        private const int PiezaAlto = PiezaAltoCeldas * Escala;   // 15

        private void Start()
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
            // (fix playtest 7) Todas estas constantes vivían en CELDAS y se usaban
            // directamente como índice de téxel cuando la textura era 1:1. Ahora
            // hay que escalarlas x Escala o las vigas/zócalo quedan comprimidos en
            // la esquina superior-izquierda de la textura.
            int vigaBajaY = (SimLevelBuilder.ChillTrayY0 - 5) * Escala;   // celda 83..87, tocando el estante en 88
            int vigaAltaY = (CellGrid.H - 18) * Escala;                  // celda 126
            const int vigaGrosorCeldas = 5;
            int vigaGrosor = vigaGrosorCeldas * Escala;
            const int mensulaPeriodoCeldas = 48;
            const int mensulaAnchoCeldas = 3;
            int mensulaPeriodo = mensulaPeriodoCeldas * Escala;
            int mensulaAncho = mensulaAnchoCeldas * Escala;

            // Zócalo: sillares grandes al nivel del suelo de piedra.
            int zocaloTop = (SimLevelBuilder.FloorHeight + 6) * Escala;   // celda 20

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
                    float tx = x / (float)(TexW - 1);
                    float nx = tx - 0.5f, ny = ty - 0.52f;
                    float vig = Mathf.Clamp01(1f - (nx * nx * 2.3f + ny * ny * 2.0f));
                    c *= 0.62f + 0.38f * vig;

                    // Luz de fragua a la altura de las cubas (donde se juega).
                    float gx = (tx - 0.46f) / 0.55f;
                    float gy = (ty - 0.22f) / 0.42f;
                    float halo = Mathf.Clamp01(1f - (gx * gx + gy * gy));
                    c += rescoldo * (halo * halo);

                    px[y * TexW + x] = new Color(
                        Mathf.Clamp01(c.r),
                        Mathf.Clamp01(c.g),
                        Mathf.Clamp01(c.b), 1f);
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
            // EXACTAMENTE 25.6 x 14.4 unidades de mundo, solo cambia la densidad de
            // téxeles por unidad.
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
