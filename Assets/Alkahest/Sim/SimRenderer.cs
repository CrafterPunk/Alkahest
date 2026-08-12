using UnityEngine;

namespace Alkahest.Sim
{
    /// <summary>
    /// Traduce el estado de <see cref="CellGrid"/> a una textura y la
    /// muestra sobre un quad en espacio de mundo. Solo redibuja los chunks
    /// despiertos cada frame (más un refresco completo periódico, por si
    /// algo se desincronizase) para mantener el coste de subida a GPU bajo.
    /// </summary>
    public sealed class SimRenderer : MonoBehaviour
    {
        /// <summary>Tamaño en unidades de mundo de una celda de simulación.</summary>
        public const float CellWorldSize = 0.1f;

        /// <summary>Color de fondo de cámara recomendado (charcoal cálido oscuro).</summary>
        public static readonly Color BackgroundColor = new Color32(0x1A, 0x14, 0x18, 0xFF);

        private const int FullRefreshEveryFrames = 30;

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
        private Transform _quad;

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

            // Guardia explícita: todo el render por chunks asume que CHUNK divide
            // los dos ejes. Si alguien vuelve a cambiar CellGrid.W/H a un tamaño
            // que no sea múltiplo de 16, que salte aquí y no en un SetPixels32
            // desalineado imposible de depurar.
#pragma warning disable 0162 // guard intencional sobre constantes (CS0162 si W/H son múltiplos exactos)
            if ((CellGrid.W % CellGrid.CHUNK) != 0 || (CellGrid.H % CellGrid.CHUNK) != 0)
            {
                Debug.LogError($"[ChaosAlchemy] CellGrid {CellGrid.W}x{CellGrid.H} no es múltiplo de CHUNK={CellGrid.CHUNK}: " +
                               "SimRenderer necesita chunks completos (ver el buffer scratch único).");
            }
#pragma warning restore 0162

            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = default;
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);

            BuildQuad();
            FitMainCamera();
            RenderFrame(0, true);
        }

        /// <summary>
        /// (fix playtest 5: "pantalla descuadrada") La cámara guardada en la escena
        /// tenía el encuadre del mundo antiguo (384x216). En vez de depender de
        /// regenerar la escena, la cámara se AUTOAJUSTA al mundo actual en cada
        /// arranque: centro y tamaño ortográfico derivados de CellGrid, siempre.
        /// </summary>
        private static void FitMainCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float worldW = CellGrid.W * CellWorldSize;
            float worldH = CellGrid.H * CellWorldSize;
            cam.orthographic = true;
            // (fix playtest 6) Si el viewport es más ESTRECHO que el aspecto del mundo,
            // encajar solo la altura RECORTA los lados (los grifos quedaban fuera).
            // Se encaja la dimensión limitante: sobra arriba/abajo antes que cortar.
            float aspect = cam.aspect > 0.01f ? cam.aspect : 16f / 9f;
            float sizeForHeight = worldH * 0.5f;
            float sizeForWidth = (worldW * 0.5f) / aspect;
            cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
            cam.transform.position = new Vector3(worldW * 0.5f, worldH * 0.5f, -10f);
            cam.backgroundColor = BackgroundColor;
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

            _quad = go.transform;
            _quad.SetParent(transform, false);
            _quad.position = Vector3.zero; // pivot en (0,0): el sprite cubre 25.6 x 14.4 exacto (256x144 celdas de 0.1).
        }

        /// <summary>Redibuja los chunks despiertos (o todos, si toca refresco completo) y sube la textura a GPU.</summary>
        private float _lastAspect;
        public void RenderFrame(uint tick, bool forceFull = false)
        {
            var camNow = Camera.main;
            if (camNow != null && !Mathf.Approximately(camNow.aspect, _lastAspect))
            {
                _lastAspect = camNow.aspect;
                FitMainCamera();
            }
            _frameCounter++;
            bool full = forceFull || (_frameCounter % FullRefreshEveryFrames) == 0;

            for (int cy = 0; cy < CellGrid.ChunksY; cy++)
            {
                for (int cx = 0; cx < CellGrid.ChunksX; cx++)
                {
                    if (!full && !_grid.IsChunkAwake(cx, cy)) continue;
                    RenderChunk(cx, cy, tick);
                }
            }

            _texture.Apply(false);
        }

        private void RenderChunk(int cx, int cy, uint tick)
        {
            CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);
            int w = x1 - x0;
            int h = y1 - y0;
            var scratch = _chunkScratch; // 256x144: todos los chunks son 16x16 (ver Init).

            int t = (int)tick;
            int scratchI = 0;
            for (int y = y0; y < y1; y++)
            {
                int rowBase = y * CellGrid.W;
                for (int x = x0; x < x1; x++)
                {
                    int idx = rowBase + x;
                    Color32 c = ComputeCellColor(x, y, idx, t);
                    _pixels[idx] = c;
                    scratch[scratchI++] = c;
                }
            }

            _texture.SetPixels32(x0, y0, w, h, scratch);
        }

        private Color32 ComputeCellColor(int x, int y, int idx, int tick)
        {
            byte matId = _grid.mat[idx];
            if (matId == MaterialId.Empty) return default;

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

                // idx+W = celda de ARRIBA (y+1), idx-W = celda de ABAJO (y-1): ver
                // SimStepper (belowIdx = idx - W, aboveIdx = idx + W) y el check de
                // superficie de líquidos más abajo en este mismo archivo.
                bool arribaVacia = y < CellGrid.H - 1 && _grid.mat[idx + CellGrid.W] == MaterialId.Empty;
                bool abajoVacia = y > 0 && _grid.mat[idx - CellGrid.W] == MaterialId.Empty;
                bool izqVacia = x > 0 && _grid.mat[idx - 1] == MaterialId.Empty;
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

                int grano = (int)(Hash3D(x, y, 97) % 9) - 4; // ±4, hash distinto al del bloque y sin componente de tiempo (la piedra no se anima)
                r = ClampByte(r + grano);
                g = ClampByte(g + grano);
                b = ClampByte(b + grano);
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

            // Tinte de temperatura: por encima de raw 150 (~180°C) se calienta hacia
            // naranja/blanco incandescente proporcionalmente a la temperatura.
            byte raw = _grid.temp[idx];
            if (raw > 150)
            {
                float t01 = (raw - 150) / 105f;
                if (t01 > 1f) t01 = 1f;
                r = LerpByte(r, 255, t01);
                g = LerpByte(g, 214, t01);
                b = LerpByte(b, 140, t01);
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
    }
}
