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
        // CHUNK divide exactamente W (384/16=24), así que el ancho de cualquier
        // chunk es siempre CHUNK; solo la última fila de chunks puede tener menos
        // alto (216 no es múltiplo de 16). Se preasignan ambos tamaños posibles
        // para no asignar memoria en el hot-path de render.
        private Color32[] _chunkScratchFull;   // CHUNK*CHUNK
        private Color32[] _chunkScratchEdge;   // CHUNK*(H % CHUNK)
        private Material _material;
        private Transform _quad;

        private int _frameCounter;

        public Texture2D Texture => _texture;

        public void Init(Universe universe, CellGrid grid)
        {
            _universe = universe;
            _grid = grid;

            _texture = new Texture2D(CellGrid.W, CellGrid.H, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "AlkahestSimTexture",
            };

            _pixels = new Color32[CellGrid.W * CellGrid.H];
            _chunkScratchFull = new Color32[CellGrid.CHUNK * CellGrid.CHUNK];
            int edgeH = CellGrid.H % CellGrid.CHUNK;
            _chunkScratchEdge = edgeH == 0 ? _chunkScratchFull : new Color32[CellGrid.CHUNK * edgeH];

            for (int i = 0; i < _pixels.Length; i++) _pixels[i] = default;
            _texture.SetPixels32(_pixels);
            _texture.Apply(false);

            BuildQuad();
            RenderFrame(0, true);
        }

        private void BuildQuad()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "AlkahestSimQuad";
            // No necesitamos el collider por defecto del primitive.
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            _material = new Material(shader) { name = "AlkahestSimMat" };
            ConfigureTransparentUnlit(_material);
            _material.mainTexture = _texture;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            _quad = go.transform;
            _quad.SetParent(transform, false);

            float worldW = CellGrid.W * CellWorldSize;
            float worldH = CellGrid.H * CellWorldSize;
            _quad.position = new Vector3(worldW * 0.5f, worldH * 0.5f, 0f);
            _quad.localScale = new Vector3(worldW, worldH, 1f);
        }

        /// <summary>
        /// Configura un material del shader Unlit de URP en modo transparente
        /// alfa-blend, usando los nombres de propiedad estándar del shader.
        /// </summary>
        private static void ConfigureTransparentUnlit(Material mat)
        {
            mat.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);   // 0 = Alpha
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        /// <summary>Redibuja los chunks despiertos (o todos, si toca refresco completo) y sube la textura a GPU.</summary>
        public void RenderFrame(uint tick, bool forceFull = false)
        {
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
            var scratch = (h == CellGrid.CHUNK) ? _chunkScratchFull : _chunkScratchEdge;

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

            byte r = baseColor.r, g = baseColor.g, b = baseColor.b;

            if (def.colorJitter > 0)
            {
                int j = (int)(Hash2D(x, y) % (uint)(def.colorJitter * 2 + 1)) - def.colorJitter;
                r = ClampByte(r + j);
                g = ClampByte(g + j);
                b = ClampByte(b + j);
            }

            // Resplandor animado (fuego, materiales emisivos): parpadeo estable por celda+frame.
            if (def.emitsGlow)
            {
                int flicker = (int)(Hash3D(x, y, tick / 3) % 60);
                r = ClampByte(r + flicker);
                g = ClampByte(g + flicker / 2);
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
