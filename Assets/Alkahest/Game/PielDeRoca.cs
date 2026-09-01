using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 124, PRUEBA VISUAL — pedido de Cesar) LA PIEL DE ROCA: un contorno
    /// orgánico tipo MARCHING SQUARES dibujado ENCIMA de la grilla, solo para la
    /// ROCA MADRE (MaterialId.Stone). La sim NO cambia: cada celda sigue siendo
    /// una celda para el stepper, la colisión, el cincel y la red; lo único que
    /// hace esta clase es (1) pedirle a SimRenderer que no pinte Stone
    /// (SimRenderer.OcultarRoca) y (2) dibujar mallas por chunk POR DEBAJO de la
    /// sim (sortingOrder -6) para que arena, agua y brasas sigan pasando por
    /// delante, cuadradas, como manda el pilar de la materia real.
    ///
    /// Referencia conceptual: Sebastian Lague, "Procedural Cave Generation"
    /// (autómata + marching squares + malla de contorno) — adaptado aquí a un
    /// terreno VIVO que cambia cada tick: el campo escalar es la solidez media
    /// de las 4 celdas que rodean cada esquina (0, ¼, ½, ¾, 1), el umbral 0.49
    /// deja los muros de una celda enteros y las esquinas se achaflanan media
    /// celda como mucho (la silueta nunca se aleja más de eso de la colisión).
    ///
    /// CUATRO NIVELES acumulativos (F7 los rota, el atril avisa, PlayerPrefs):
    ///   1 · Contorno   — relleno texturizado + línea de tinta (regla 19 en malla).
    ///   2 · Bandas     — cada tramo sabe si es SUELO (luz), PARED (oclusión) o
    ///                    TECHO (sombra) por su normal; la masa interna queda
    ///                    más oscura que el borde expuesto.
    ///   3 · Profundidad— "masa profunda": la misma silueta desplazada abajo-
    ///                    izquierda y oscura, detrás de todo: la roca es una
    ///                    placa con canto, no una pintura.
    ///   4 · Decorada   — estalactitas en techos, musgo en suelos, grietas en
    ///                    paredes, todo procedural y determinista por posición.
    ///
    /// Regla de oro de la R123: toda profundidad es pintura; la colisión NUNCA
    /// viene de aquí.
    /// </summary>
    public sealed class PielDeRoca : MonoBehaviour
    {
        public enum Nivel { Apagada = 0, Contorno = 1, Bandas = 2, Profundidad = 3, Decorada = 4 }

        public const string PrefModo = "PielDeRoca.Nivel";
        public static PielDeRoca Instancia { get; private set; }
        public Nivel Modo { get; private set; } = Nivel.Decorada;

        // ---- geometría en unidades de mundo (1 celda = 0.1 u) ----
        private const float C = SimRenderer.CellWorldSize;
        private const float Umbral = 0.49f;
        private const float AnchoTinta = 0.18f * C;     // línea de contorno (≈2 px a 80 celdas)
        private const float TintaFuera = 0.05f * C;
        private const float BandaSuelo = 0.50f * C;
        private const float BandaPared = 0.80f * C;
        private const float BandaTecho = 1.10f * C;
        private const float HaloSombra = 1.30f * C;      // sombra proyectada sobre el telón, hacia fuera del contorno
        private static readonly Vector2 DesplazoSombra = new Vector2(-0.32f * C, -0.48f * C);
        private const float EscalaUV = 1f / 2.56f;        // la textura de roca tesela cada 25.6 celdas
        private const int OrdenPiel = -6;                 // debajo de la sim (-5), encima del vidrio interior de máquinas (-8)

        // ---- colores (tinta parda sobre ceniza cálida; la paleta madre) ----
        private static readonly Color Tinta = new Color(0.16f, 0.12f, 0.10f, 1f);
        private static readonly Color SombraProfunda = new Color(0.29f, 0.24f, 0.22f, 1f); // el CANTO de la placa: más oscuro que la roca, más claro que el telón
        private static readonly Color LuzSuelo = new Color(1.00f, 0.96f, 0.88f, 0.85f);
        private static readonly Color OclusionPared = new Color(0.10f, 0.07f, 0.07f, 0.45f);
        private static readonly Color SombraTecho = new Color(0.08f, 0.06f, 0.09f, 0.70f);
        private const int RadioMasa = 6;                   // celdas: a esta distancia del aire la masa interna llega a su tono más oscuro
        private const float LumMasaInterna = 0.74f;
        private static readonly Color Musgo = new Color(0.12f, 0.56f, 0.43f, 0.85f);  // PÁTINA #1F8F6E
        private static readonly Color Transparente = new Color(0f, 0f, 0f, 0f);

        private AlkahestSim _sim;
        private CellGrid _grid;
        private Universe _universo;
        private AtrilDeEmotes _atril;
        private Material _material;
        private Texture2D _texRoca;
        private MaterialPropertyBlock _mpbRoca, _mpbPlano;
        private string _propTinte;
        private Color _tinteAplicado = new Color(-1, -1, -1, -1);
        private Transform _raiz;

        private sealed class ChunkVisual
        {
            public GameObject raiz;
            public MeshFilter sombra, relleno, bandas, deco;
            public MeshRenderer rSombra, rRelleno, rBandas, rDeco;
        }
        private ChunkVisual[] _chunks;
        private uint[] _tickVisto;
        private ulong[] _hash;
        private bool[] _sucio;
        private int _rondaHash;
        private const int ChunksPorFrameHash = 24;   // pasada completa cada ~36 frames
        private const int ChunksPorFrameBuild = 32;

        // buffers reutilizados
        private static readonly List<Vector3> V = new List<Vector3>(4096);
        private static readonly List<Color> K = new List<Color>(4096);
        private static readonly List<Vector2> U = new List<Vector2>(4096);
        private static readonly List<int> T = new List<int>(8192);
        private readonly float[] _campo = new float[(CellGrid.CHUNK + 1) * (CellGrid.CHUNK + 1)];
        private const int VentanaMasa = CellGrid.CHUNK + 2 * RadioMasa;
        private readonly byte[] _dist = new byte[VentanaMasa * VentanaMasa];
        private int _distX0, _distY0;

        // =====================================================================
        public static PielDeRoca Asegurar(AlkahestSim sim, AtrilDeEmotes atril)
        {
            if (Instancia != null) { if (atril != null) Instancia._atril = atril; return Instancia; }
            if (sim == null || sim.Grid == null || sim.Universe == null || sim.Renderer == null) return null;
            var go = new GameObject("PielDeRoca");
            var p = go.AddComponent<PielDeRoca>();
            p._sim = sim; p._grid = sim.Grid; p._universo = sim.Universe; p._atril = atril;
            p.Iniciar();
            return p;
        }

        private void Awake() { Instancia = this; }
        private void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
            SimRenderer.OcultarRoca = false;
            if (_texRoca != null) Destroy(_texRoca);
        }

        private void Iniciar()
        {
            _raiz = transform;
            Modo = (Nivel)Mathf.Clamp(PlayerPrefs.GetInt(PrefModo, (int)Nivel.Decorada), 0, 4);
            // El material de sprite por defecto del proyecto (el que ya usan todos
            // los SpriteRenderer): cero Shader.Find, cero assets (regla del playtest 2).
            var tmp = new GameObject("PielDeRoca_tmpMat").AddComponent<SpriteRenderer>();
            _material = tmp.sharedMaterial;
            Destroy(tmp.gameObject);
            _propTinte = _material != null && _material.HasProperty("_Color") ? "_Color"
                       : (_material != null && _material.HasProperty("_RendererColor") ? "_RendererColor" : null);
            _texRoca = GenerarTexturaRoca(256);
            _mpbRoca = new MaterialPropertyBlock();
            _mpbRoca.SetTexture("_MainTex", _texRoca);
            _mpbPlano = new MaterialPropertyBlock();

            int n = CellGrid.ChunksX * CellGrid.ChunksY;
            _chunks = new ChunkVisual[n];
            _tickVisto = new uint[n];
            _hash = new ulong[n];
            _sucio = new bool[n];
            for (int i = 0; i < n; i++) { _hash[i] = HashChunk(i); _sucio[i] = true; }
            AplicarModo(false);
        }

        // =====================================================================
        private void Update()
        {
            if (_grid == null) return;

            var kb = Keyboard.current;
            bool tecladoLibre = kb != null && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto;
            if (tecladoLibre && kb.f7Key.wasPressedThisFrame)
            {
                if (kb.leftCtrlKey.isPressed || kb.rightCtrlKey.isPressed) CavarCuevaDeMuestra();
                else
                {
                    Modo = (Nivel)(((int)Modo + 1) % 5);
                    PlayerPrefs.SetInt(PrefModo, (int)Modo);
                    AplicarModo(true);
                }
            }
            if (Modo == Nivel.Apagada) return;

            // 1) Tinte de la vista (TinteGlobal → TintePlano en la mudanza).
            Color tinte = _sim.Renderer != null ? _sim.Renderer.TinteActual : SimRenderer.TinteGlobal;
            if (tinte != _tinteAplicado) { _tinteAplicado = tinte; AplicarTinte(); }

            // 2) Chunks tocados por la sim desde el último vistazo → rehash.
            int total = _chunks.Length;
            for (int ci = 0; ci < total; ci++)
            {
                uint t = _grid.chunkTouchedTick[ci];
                if (t == _tickVisto[ci]) continue;
                _tickVisto[ci] = t;
                Rehash(ci);
            }
            // 3) Ronda lenta sobre todos (el espejo de un invitado puede escribir mat[] sin tocar los ticks).
            for (int k = 0; k < ChunksPorFrameHash; k++)
            {
                _rondaHash = (_rondaHash + 1) % total;
                Rehash(_rondaHash);
            }
            // 4) Reconstruir hasta N sucios por frame.
            int construidos = 0;
            for (int ci = 0; ci < total && construidos < ChunksPorFrameBuild; ci++)
            {
                if (!_sucio[ci]) continue;
                Construir(ci);
                _sucio[ci] = false;
                construidos++;
            }
        }

        // =====================================================================
        // LA CUEVA DE MUESTRA (Ctrl+F7): un escenario para juzgar la piel con
        // todos sus casos a la vista — cámara ovalada con techo irregular,
        // suelo con escalera de una celda, dos pilares, una repisa de una celda
        // de grosor, un guijarro aislado y una poza. Se talla alrededor del
        // jugador con el mismo Paint del cincel (la sim sigue mandando). Solo en
        // el anfitrión: el espejo no talla (Stepper == null).
        // =====================================================================
        private void CavarCuevaDeMuestra()
        {
            if (_sim == null || _sim.Stepper == null) { if (_atril != null) _atril.Avisar("la cueva de muestra solo se talla en el anfitrión", 3f); return; }
            var ap = FindAnyObjectByType<ApprenticeController>();
            Vector3 pos = ap != null ? ap.transform.position : Vector3.zero;
            int px = Mathf.Clamp(Mathf.FloorToInt(pos.x / C), 40, CellGrid.W - 41);
            int py = Mathf.Clamp(Mathf.FloorToInt(pos.y / C), 22, CellGrid.H - 30);
            const int A = 30, B = 15;               // semiejes de la cámara (celdas)
            int cxc = px + 8, cyc = py + 6;         // el jugador queda en el tercio izquierdo, cerca del suelo
            // 1) Un bloque de roca que contenga todo.
            _sim.PaintRect(cxc - A - 6, cyc - B - 8, 2 * A + 12, 2 * B + 16, MaterialId.Stone);
            // 2) La cámara: óvalo con radio perturbado (bóveda más alta a la derecha).
            for (int y = cyc - B - 2; y <= cyc + B + 4; y++)
                for (int x = cxc - A - 2; x <= cxc + A + 2; x++)
                {
                    float dx = (x - cxc) / (float)A, dy = (y - cyc) / (float)B;
                    float ang = Mathf.Atan2(dy, dx);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ondul = 1f + 0.10f * Mathf.Sin(ang * 5f + 1.3f) + 0.07f * Mathf.Sin(ang * 11f) + (dy > 0 && dx > 0 ? 0.18f * dx : 0f);
                    if (r < ondul) _sim.Paint(x, y, 0, MaterialId.Empty);
                }
            // 3) Suelo plano de la cámara (para que el jugador tenga piso) + escalera de una celda.
            _sim.PaintRect(cxc - A - 2, cyc - B - 8, 2 * A + 6, 6, MaterialId.Stone);
            for (int i = 0; i < 9; i++) _sim.PaintRect(cxc - A + 10 + i, cyc - B - 2, 1, i + 1, MaterialId.Stone);
            // 4) Dos pilares irregulares del suelo al techo.
            for (int y = cyc - B - 3; y <= cyc + B + 2; y++)
            {
                int w1 = 2 + ((y / 3) % 2), w2 = 3 - ((y / 4) % 2);
                _sim.PaintRect(cxc + 6, y, w1, 1, MaterialId.Stone);
                _sim.PaintRect(cxc + 20 + ((y / 5) % 2), y, w2, 1, MaterialId.Stone);
            }
            // 5) Repisa de una celda de grosor, un guijarro aislado y una poza en el suelo.
            _sim.PaintRect(cxc - 12, cyc + 1, 14, 1, MaterialId.Stone);
            _sim.Paint(cxc - 18, cyc + 6, 0, MaterialId.Stone);
            _sim.PaintRect(cxc - 4, cyc - B - 4, 8, 3, MaterialId.Empty);
            _sim.PaintRect(cxc - 4, cyc - B - 4, 8, 2, MaterialId.Water);
            // 6) Hueco lateral bajo (túnel) que sale por la derecha, con techo bajo.
            _sim.PaintRect(cxc + A - 4, cyc - B - 2, 12, 5, MaterialId.Empty);
            if (_atril != null) _atril.Avisar("cueva de muestra tallada alrededor tuyo  ·  F7 rota la piel", 4f);
        }

        private void Rehash(int ci)
        {
            ulong h = HashChunk(ci);
            if (h == _hash[ci]) return;
            _hash[ci] = h;
            int cx = ci % CellGrid.ChunksX, cy = ci / CellGrid.ChunksX;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= CellGrid.ChunksX || ny >= CellGrid.ChunksY) continue;
                    _sucio[CellGrid.ChunkIndex(nx, ny)] = true;
                }
        }

        private ulong HashChunk(int ci)
        {
            int cx = ci % CellGrid.ChunksX, cy = ci / CellGrid.ChunksX;
            CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);
            ulong h = 1469598103934665603UL;
            var mat = _grid.mat;
            for (int y = y0; y < y1; y++)
            {
                int fila = y * CellGrid.W;
                for (int x = x0; x < x1; x++)
                {
                    byte m = mat[fila + x];
                    // Solo importan Stone y "otro sólido estático" (junta) — el resto es aire para la piel.
                    ulong v = m == MaterialId.Stone ? 2UL : (m != MaterialId.Empty && _universo.Get(m).archetype == MaterialArchetype.StaticSolid ? 1UL : 0UL);
                    h = (h ^ v) * 1099511628211UL;
                }
            }
            return h;
        }

        private void AplicarModo(bool avisar)
        {
            bool activa = Modo != Nivel.Apagada;
            if (SimRenderer.OcultarRoca != activa)
            {
                SimRenderer.OcultarRoca = activa;
                if (_sim.Renderer != null) _sim.Renderer.MarcarTodoSucio();
            }
            _raiz.gameObject.SetActive(true);
            for (int i = 0; i < _chunks.Length; i++)
            {
                var c = _chunks[i];
                if (c == null) continue;
                c.raiz.SetActive(activa);
                _sucio[i] = true; // el nivel cambia qué capas se emiten
            }
            if (avisar && _atril != null) _atril.Avisar("piel de roca: " + NombreNivel(Modo) + "  ·  F7 cambia", 3.5f);
        }

        public static string NombreNivel(Nivel n)
        {
            switch (n)
            {
                case Nivel.Apagada: return "APAGADA (grilla de siempre)";
                case Nivel.Contorno: return "1 · CONTORNO";
                case Nivel.Bandas: return "2 · CONTORNO + BANDAS suelo/pared/techo";
                case Nivel.Profundidad: return "3 · + PROFUNDIDAD (masa profunda)";
                default: return "4 · + DECORADA (estalactitas, musgo, grietas)";
            }
        }

        private void AplicarTinte()
        {
            if (_propTinte != null)
            {
                _mpbRoca.SetColor(_propTinte, _tinteAplicado);
                _mpbPlano.SetColor(_propTinte, _tinteAplicado);
            }
            for (int i = 0; i < _chunks.Length; i++)
            {
                var c = _chunks[i];
                if (c == null) continue;
                c.rRelleno.SetPropertyBlock(_mpbRoca);
                c.rSombra.SetPropertyBlock(_mpbPlano);
                c.rBandas.SetPropertyBlock(_mpbPlano);
                c.rDeco.SetPropertyBlock(_mpbPlano);
            }
        }

        // =====================================================================
        // CONSTRUCCIÓN DE UN CHUNK
        // =====================================================================
        private bool EsRoca(int x, int y)
        {
            if (x < 0 || y < 0 || x >= CellGrid.W || y >= CellGrid.H) return true; // el borde del mundo cierra la silueta
            return _grid.mat[y * CellGrid.W + x] == MaterialId.Stone;
        }

        /// <summary>Esquina (i,j) = punto entre las celdas (i-1,j-1),(i,j-1),(i-1,j),(i,j): solidez media.</summary>
        private float Esquina(int i, int j)
        {
            int n = 0;
            if (EsRoca(i - 1, j - 1)) n++;
            if (EsRoca(i, j - 1)) n++;
            if (EsRoca(i - 1, j)) n++;
            if (EsRoca(i, j)) n++;
            return n * 0.25f;
        }

        private static uint Hash(int x, int y, int sal)
        {
            uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ (uint)(sal * 2246822519);
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
        private static float Hash01(int x, int y, int sal) => (Hash(x, y, sal) & 0xFFFF) / 65535f;

        private void Construir(int ci)
        {
            int cx = ci % CellGrid.ChunksX, cy = ci / CellGrid.ChunksX;
            CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);

            // ¿Hay roca en el chunk o en su halo? Si no, sin malla.
            bool hay = false;
            for (int y = y0 - 1; y <= y1 && !hay; y++)
                for (int x = x0 - 1; x <= x1; x++)
                    if (x >= 0 && y >= 0 && x < CellGrid.W && y < CellGrid.H && _grid.mat[y * CellGrid.W + x] == MaterialId.Stone) { hay = true; break; }
            var cv = _chunks[ci];
            if (!hay)
            {
                if (cv != null) { cv.sombra.sharedMesh.Clear(); cv.relleno.sharedMesh.Clear(); cv.bandas.sharedMesh.Clear(); cv.deco.sharedMesh.Clear(); }
                return;
            }
            if (cv == null) cv = _chunks[ci] = CrearVisual(ci);

            // Campo escalar en las esquinas (17x17).
            const int N = CellGrid.CHUNK + 1;
            for (int j = 0; j < N; j++)
                for (int i = 0; i < N; i++)
                    _campo[j * N + i] = Esquina(x0 + i, y0 + j);

            bool bandas = Modo >= Nivel.Bandas, profundidad = Modo >= Nivel.Profundidad, deco = Modo >= Nivel.Decorada;
            CalcularDistanciaAlAire(x0, y0);

            // ---------- RELLENO + SOMBRA ----------
            V.Clear(); K.Clear(); U.Clear(); T.Clear();
            var poly = new Vector2[8]; var esBorde = new bool[8];
            var segmentos = new List<(Vector2 a, Vector2 b, Vector2 n, bool junta)>(512);

            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    int i = x - x0, j = y - y0;
                    float va = _campo[j * N + i], vb = _campo[j * N + i + 1], vc = _campo[(j + 1) * N + i + 1], vd = _campo[(j + 1) * N + i];
                    int caso = (va > Umbral ? 1 : 0) | (vb > Umbral ? 2 : 0) | (vc > Umbral ? 4 : 0) | (vd > Umbral ? 8 : 0);
                    bool celdaRoca = _grid.mat[y * CellGrid.W + x] == MaterialId.Stone;
                    if (caso == 0)
                    {
                        // Celda de roca aislada (todas sus esquinas por debajo): un guijarro para que no desaparezca la colisión.
                        if (celdaRoca)
                        {
                            float cxw = (x + 0.5f) * C, cyw = (y + 0.5f) * C, r = 0.42f * C;
                            poly[0] = new Vector2(cxw - r, cyw - r * 0.6f); poly[1] = new Vector2(cxw - r * 0.5f, cyw - r); poly[2] = new Vector2(cxw + r * 0.5f, cyw - r); poly[3] = new Vector2(cxw + r, cyw - r * 0.6f);
                            poly[4] = new Vector2(cxw + r, cyw + r * 0.6f); poly[5] = new Vector2(cxw + r * 0.5f, cyw + r); poly[6] = new Vector2(cxw - r * 0.5f, cyw + r); poly[7] = new Vector2(cxw - r, cyw + r * 0.6f);
                            EmitirPoligono(poly, 8, x, y, true);
                            for (int s = 0; s < 8; s++) { var a = poly[s]; var b = poly[(s + 1) % 8]; segmentos.Add((a, b, NormalFuera(a, b), false)); }
                        }
                        continue;
                    }

                    if (caso == 15)
                    {
                        // Celdas macizas: se funden en un solo quad por fila (8x menos vértices en la masa).
                        int xr = x + 1;
                        while (xr < x1)
                        {
                            int ir = xr - x0;
                            if (_campo[j * N + ir] > Umbral && _campo[j * N + ir + 1] > Umbral && _campo[(j + 1) * N + ir + 1] > Umbral && _campo[(j + 1) * N + ir] > Umbral) xr++; else break;
                        }
                        poly[0] = new Vector2(x * C, y * C); poly[1] = new Vector2(xr * C, y * C); poly[2] = new Vector2(xr * C, (y + 1) * C); poly[3] = new Vector2(x * C, (y + 1) * C);
                        EmitirPoligono(poly, 4, x, y, false);
                        x = xr - 1;
                        continue;
                    }
                    Vector2 pa = new Vector2(x * C, y * C), pb = new Vector2((x + 1) * C, y * C), pc = new Vector2((x + 1) * C, (y + 1) * C), pd = new Vector2(x * C, (y + 1) * C);
                    Vector2 eab = Interp(pa, pb, va, vb, x, y, 0), ebc = Interp(pb, pc, vb, vc, x, y, 1), ecd = Interp(pc, pd, vc, vd, x, y, 2), eda = Interp(pd, pa, vd, va, x, y, 3);

                    // Casos ambiguos (5 y 10): el centro decide si es puente o dos puntas.
                    if (caso == 5 || caso == 10)
                    {
                        float centro = (va + vb + vc + vd) * 0.25f;
                        if (centro <= Umbral)
                        {
                            if (caso == 5)
                            {
                                Tri(pa, eab, eda, x, y); Tri(pc, ecd, ebc, x, y);
                                segmentos.Add((eab, eda, NormalFuera(eab, eda), false)); segmentos.Add((ecd, ebc, NormalFuera(ecd, ebc), false));
                            }
                            else
                            {
                                Tri(pb, ebc, eab, x, y); Tri(pd, eda, ecd, x, y);
                                segmentos.Add((ebc, eab, NormalFuera(ebc, eab), false)); segmentos.Add((eda, ecd, NormalFuera(eda, ecd), false));
                            }
                            continue;
                        }
                    }

                    // Recorrido CCW a→b→c→d: esquina dentro → vértice; cambio dentro/fuera → punto de arista.
                    int n = 0;
                    bool ia = va > Umbral, ib = vb > Umbral, ic = vc > Umbral, id = vd > Umbral;
                    if (ia) { poly[n] = pa; esBorde[n++] = false; }
                    if (ia != ib) { poly[n] = eab; esBorde[n++] = true; }
                    if (ib) { poly[n] = pb; esBorde[n++] = false; }
                    if (ib != ic) { poly[n] = ebc; esBorde[n++] = true; }
                    if (ic) { poly[n] = pc; esBorde[n++] = false; }
                    if (ic != id) { poly[n] = ecd; esBorde[n++] = true; }
                    if (id) { poly[n] = pd; esBorde[n++] = false; }
                    if (id != ia) { poly[n] = eda; esBorde[n++] = true; }
                    EmitirPoligono(poly, n, x, y, false);
                    // Tramos de contorno: dos puntos de arista consecutivos en el recorrido.
                    for (int s = 0; s < n; s++)
                    {
                        int s2 = (s + 1) % n;
                        if (esBorde[s] && esBorde[s2])
                        {
                            Vector2 a = poly[s], b = poly[s2];
                            Vector2 nf = NormalFuera(a, b);
                            Vector2 m = (a + b) * 0.5f + nf * (0.5f * C);
                            int mx = Mathf.FloorToInt(m.x / C), my = Mathf.FloorToInt(m.y / C);
                            bool junta = false;
                            if (mx >= 0 && my >= 0 && mx < CellGrid.W && my < CellGrid.H)
                            {
                                byte mm = _grid.mat[my * CellGrid.W + mx];
                                junta = mm != MaterialId.Empty && mm != MaterialId.Stone && _universo.Get(mm).archetype == MaterialArchetype.StaticSolid;
                            }
                            segmentos.Add((a, b, nf, junta));
                        }
                    }
                }
            }
            Volcar(cv.relleno.sharedMesh);

            if (profundidad)
            {
                // La misma silueta, desplazada y oscura (la placa tiene canto).
                for (int k = 0; k < V.Count; k++) { var v = V[k]; V[k] = new Vector3(v.x + DesplazoSombra.x, v.y + DesplazoSombra.y, 0f); K[k] = SombraProfunda; }
                // Y la sombra que la roca proyecta sobre el telón: un halo oscuro hacia FUERA del contorno.
                Color s0 = new Color(0f, 0f, 0f, 0.50f);
                foreach (var sg in segmentos)
                {
                    Vector2 dir = (sg.b - sg.a); float len = dir.magnitude; if (len < 1e-5f) continue; dir /= len;
                    Vector2 a = sg.a - dir * (0.6f * C), b = sg.b + dir * (0.6f * C);
                    Quad(a, b, b + sg.n * HaloSombra, a + sg.n * HaloSombra, s0, s0, Transparente, Transparente);
                }
                Volcar(cv.sombra.sharedMesh);
            }
            else cv.sombra.sharedMesh.Clear();

            // ---------- BANDAS + TINTA ----------
            V.Clear(); K.Clear(); U.Clear(); T.Clear();
            foreach (var sg in segmentos)
            {
                Vector2 dir = (sg.b - sg.a); float len = dir.magnitude; if (len < 1e-5f) continue; dir /= len;
                Vector2 a = sg.a - dir * (0.10f * C), b = sg.b + dir * (0.10f * C);
                if (bandas && !sg.junta)
                {
                    Color col; float ancho;
                    if (sg.n.y > 0.45f) { col = LuzSuelo; ancho = BandaSuelo; }
                    else if (sg.n.y < -0.45f) { col = SombraTecho; ancho = BandaTecho; }
                    else { col = OclusionPared; ancho = BandaPared; }
                    // Las bandas empiezan donde termina la tinta (si no, la línea las tapa).
                    Vector2 ai = a - sg.n * AnchoTinta, bi = b - sg.n * AnchoTinta;
                    Quad(ai, bi, bi - sg.n * ancho, ai - sg.n * ancho, col, col, Transparente, Transparente);
                    if (sg.n.y > 0.45f)
                    {
                        // El filo del suelo: una hebra clara pegada al canto (lo que en la grilla era el +28% de la cara al aire).
                        Color filo = new Color(1f, 0.97f, 0.90f, 0.95f);
                        Quad(ai, bi, bi - sg.n * (0.16f * C), ai - sg.n * (0.16f * C), filo, filo, filo, filo);
                    }
                }
                // Línea de tinta (más fina en juntas con otros sólidos).
                float fuera = sg.junta ? 0f : TintaFuera, dentro = sg.junta ? AnchoTinta * 0.5f : AnchoTinta;
                Quad(a + sg.n * fuera, b + sg.n * fuera, b - sg.n * dentro, a - sg.n * dentro, Tinta, Tinta, Tinta, Tinta);
            }
            Volcar(cv.bandas.sharedMesh);

            // ---------- DECORACIÓN ----------
            V.Clear(); K.Clear(); U.Clear(); T.Clear();
            if (deco)
            {
                foreach (var sg in segmentos)
                {
                    if (sg.junta) continue;
                    Vector2 m = (sg.a + sg.b) * 0.5f;
                    int hx = Mathf.FloorToInt(m.x / C), hy = Mathf.FloorToInt(m.y / C);
                    float len = (sg.b - sg.a).magnitude; if (len < 0.5f * C) continue;
                    Vector2 dir = (sg.b - sg.a) / len;
                    if (sg.n.y < -0.7f && Hash(hx, hy, 11) % 6 == 0)
                    {
                        // Estalactita: cuelga del techo hacia el aire (n apunta hacia abajo).
                        float largo = (0.8f + 1.6f * Hash01(hx, hy, 12)) * C, ancho = (0.35f + 0.3f * Hash01(hx, hy, 13)) * C;
                        float sesgo = (Hash01(hx, hy, 14) - 0.5f) * 0.4f * C;
                        Vector2 baseA = m - dir * ancho - sg.n * (0.15f * C), baseB = m + dir * ancho - sg.n * (0.15f * C);
                        Vector2 punta = m + sg.n * largo + dir * sesgo;
                        Color cr = new Color(0.30f, 0.26f, 0.25f, 1f);
                        TriColor(baseA, baseB, punta, cr, cr, Tinta);
                        // gota en la punta (agua que cuelga): azul mudanza apagado
                        if (Hash(hx, hy, 15) % 3 == 0)
                        {
                            Vector2 g = punta + sg.n * (0.08f * C); float rg = 0.12f * C;
                            Color ag = new Color(0.45f, 0.62f, 0.80f, 0.9f);
                            Quad(g + new Vector2(-rg, 0), g + new Vector2(0, -rg), g + new Vector2(rg, 0), g + new Vector2(0, rg), ag, ag, ag, ag);
                        }
                    }
                    else if (sg.n.y < -0.5f && Hash(hx, hy, 61) % 10 == 0)
                    {
                        // Raíz: una hebra que cuelga del techo y se curva, tono pátina oscuro.
                        Vector2 p = m - sg.n * (0.1f * C); float largo = (1.4f + 1.8f * Hash01(hx, hy, 62)) * C; int tramos = 4;
                        float curva = (Hash01(hx, hy, 63) - 0.5f) * 0.9f;
                        Color rz = new Color(Musgo.r * 0.55f, Musgo.g * 0.55f, Musgo.b * 0.5f, 0.95f);
                        for (int s = 0; s < tramos; s++)
                        {
                            float k = (s + 1) / (float)tramos;
                            Vector2 q = m + sg.n * (largo * k) + dir * (curva * largo * k * k);
                            Vector2 nn = new Vector2(-(q - p).y, (q - p).x).normalized * (0.09f * C * (1f - 0.6f * k));
                            Quad(p + nn, q + nn, q - nn, p - nn, rz, rz, rz, rz);
                            p = q;
                        }
                    }
                    else if (sg.n.y > 0.7f && Hash(hx, hy, 21) % 6 == 0)
                    {
                        // Musgo: una loma baja sobre el suelo, tono PÁTINA mezclado con la roca.
                        float w = (0.6f + 0.7f * Hash01(hx, hy, 22)) * C, h = (0.22f + 0.2f * Hash01(hx, hy, 23)) * C;
                        Vector2 p0 = m - dir * w, p1 = m - dir * w * 0.5f + sg.n * h * 0.8f, p2 = m + sg.n * h, p3 = m + dir * w * 0.5f + sg.n * h * 0.75f, p4 = m + dir * w;
                        Color mus = Musgo; mus.a = 0.8f + 0.15f * Hash01(hx, hy, 24);
                        Color musOsc = new Color(Musgo.r * 0.6f, Musgo.g * 0.6f, Musgo.b * 0.6f, mus.a);
                        poly[0] = p0 - sg.n * (0.12f * C); poly[1] = p0; poly[2] = p1; poly[3] = p2; poly[4] = p3; poly[5] = p4; poly[6] = p4 - sg.n * (0.12f * C);
                        Abanico(poly, 7, musOsc, mus);
                    }
                    else if (Mathf.Abs(sg.n.y) <= 0.7f && Hash(hx, hy, 31) % 9 == 0)
                    {
                        // Grieta: zigzag de tinta hacia dentro de la pared.
                        Vector2 p = m; Vector2 haciaDentro = -sg.n;
                        Color gr = new Color(Tinta.r, Tinta.g, Tinta.b, 0.55f);
                        for (int s = 0; s < 3; s++)
                        {
                            float paso = (0.5f + 0.5f * Hash01(hx + s, hy, 32)) * C;
                            Vector2 lado = dir * ((Hash01(hx, hy + s, 33) - 0.5f) * 0.8f * C);
                            Vector2 q = p + haciaDentro * paso + lado;
                            Vector2 nn = new Vector2(-(q - p).y, (q - p).x).normalized * (0.07f * C);
                            Quad(p + nn, q + nn, q - nn, p - nn, gr, gr, gr, gr);
                            p = q;
                        }
                    }
                }
            }
            Volcar(cv.deco.sharedMesh);
        }

        /// <summary>Transformada de distancia (Manhattan, dos pasadas) al aire para el chunk + halo de RadioMasa celdas.</summary>
        private void CalcularDistanciaAlAire(int x0, int y0)
        {
            _distX0 = x0 - RadioMasa; _distY0 = y0 - RadioMasa;
            const int Nv = VentanaMasa;
            for (int j = 0; j < Nv; j++)
                for (int i = 0; i < Nv; i++)
                    _dist[j * Nv + i] = EsRoca(_distX0 + i, _distY0 + j) ? (byte)99 : (byte)0;
            for (int j = 0; j < Nv; j++)
                for (int i = 0; i < Nv; i++)
                {
                    int k = j * Nv + i; int v = _dist[k];
                    if (i > 0 && _dist[k - 1] + 1 < v) v = _dist[k - 1] + 1;
                    if (j > 0 && _dist[k - Nv] + 1 < v) v = _dist[k - Nv] + 1;
                    _dist[k] = (byte)v;
                }
            for (int j = Nv - 1; j >= 0; j--)
                for (int i = Nv - 1; i >= 0; i--)
                {
                    int k = j * Nv + i; int v = _dist[k];
                    if (i < Nv - 1 && _dist[k + 1] + 1 < v) v = _dist[k + 1] + 1;
                    if (j < Nv - 1 && _dist[k + Nv] + 1 < v) v = _dist[k + Nv] + 1;
                    _dist[k] = (byte)v;
                }
        }

        private float DistanciaAlAire(Vector2 p)
        {
            // Muestreo bilineal entre centros de celda para que el degradado no escalone.
            float fx = p.x / C - 0.5f - _distX0, fy = p.y / C - 0.5f - _distY0;
            int i0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, VentanaMasa - 2), j0 = Mathf.Clamp(Mathf.FloorToInt(fy), 0, VentanaMasa - 2);
            float tx = Mathf.Clamp01(fx - i0), ty = Mathf.Clamp01(fy - j0);
            float a = _dist[j0 * VentanaMasa + i0], b = _dist[j0 * VentanaMasa + i0 + 1], c = _dist[(j0 + 1) * VentanaMasa + i0], d = _dist[(j0 + 1) * VentanaMasa + i0 + 1];
            return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
        }

        private Vector2 Interp(Vector2 p, Vector2 q, float vp, float vq, int x, int y, int arista)
        {
            float t = Mathf.Abs(vq - vp) < 1e-4f ? 0.5f : (Umbral - vp) / (vq - vp);
            // Temblor orgánico determinista sobre la arista (nunca cambia la topología, solo desliza el punto).
            t += (Hash01(x, y, 40 + arista) - 0.5f) * 0.22f;
            t = Mathf.Clamp(t, 0.08f, 0.92f);
            return p + (q - p) * t;
        }

        private static Vector2 NormalFuera(Vector2 a, Vector2 b)
        {
            Vector2 d = b - a; // polígono CCW: el interior queda a la IZQUIERDA; fuera = derecha.
            return new Vector2(d.y, -d.x).normalized;
        }

        // ---- emisores de geometría ----
        private void EmitirPoligono(Vector2[] p, int n, int x, int y, bool guijarro)
        {
            if (n < 3) return;
            // Variación lenta de luminancia por vértice para romper la tesela de la textura.
            int b = V.Count;
            for (int k = 0; k < n; k++)
            {
                float lum = 0.94f + 0.12f * Hash01(Mathf.FloorToInt(p[k].x / (8f * C)), Mathf.FloorToInt(p[k].y / (8f * C)), 50);
                // MASA INTERNA vs BORDE EXPUESTO: cuanto más lejos del aire, más oscura la roca.
                float d = DistanciaAlAire(p[k]);
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d - 0.5f) / (RadioMasa - 0.5f)));
                lum *= Mathf.Lerp(1f, LumMasaInterna, t);
                if (guijarro) lum *= 0.9f;
                V.Add(new Vector3(p[k].x, p[k].y, 0f));
                K.Add(new Color(lum, lum, lum, 1f));
                U.Add(new Vector2(p[k].x * EscalaUV, p[k].y * EscalaUV));
            }
            for (int k = 1; k < n - 1; k++) { T.Add(b); T.Add(b + k + 1); T.Add(b + k); }
        }
        private void Tri(Vector2 a, Vector2 b, Vector2 c, int x, int y)
        {
            var arr = new[] { a, b, c };
            EmitirPoligono(arr, 3, x, y, false);
        }
        private void TriColor(Vector2 a, Vector2 b, Vector2 c, Color ka, Color kb, Color kc)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c); K.Add(ka); K.Add(kb); K.Add(kc); U.Add(Vector2.zero); U.Add(Vector2.zero); U.Add(Vector2.zero);
            // orientación indiferente: material sin culling (sprites)
            T.Add(i); T.Add(i + 2); T.Add(i + 1);
        }
        private void Quad(Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color ka, Color kb, Color kc, Color kd)
        {
            int i = V.Count;
            V.Add(a); V.Add(b); V.Add(c); V.Add(d); K.Add(ka); K.Add(kb); K.Add(kc); K.Add(kd);
            U.Add(Vector2.zero); U.Add(Vector2.zero); U.Add(Vector2.zero); U.Add(Vector2.zero);
            T.Add(i); T.Add(i + 2); T.Add(i + 1); T.Add(i); T.Add(i + 3); T.Add(i + 2);
        }
        private void Abanico(Vector2[] p, int n, Color borde, Color centro)
        {
            int b = V.Count;
            Vector2 c = Vector2.zero; for (int k = 0; k < n; k++) c += p[k]; c /= n;
            V.Add(c); K.Add(centro); U.Add(Vector2.zero);
            for (int k = 0; k < n; k++) { V.Add(p[k]); K.Add(borde); U.Add(Vector2.zero); }
            for (int k = 0; k < n; k++) { T.Add(b); T.Add(b + 1 + (k + 1) % n); T.Add(b + 1 + k); }
        }

        private static void Volcar(Mesh m)
        {
            m.Clear();
            if (V.Count == 0) return;
            m.indexFormat = V.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            m.SetVertices(V); m.SetColors(K); m.SetUVs(0, U); m.SetTriangles(T, 0, false);
            m.RecalculateBounds();
        }

        private ChunkVisual CrearVisual(int ci)
        {
            var cv = new ChunkVisual();
            cv.raiz = new GameObject("PielChunk_" + ci);
            cv.raiz.transform.SetParent(_raiz, false);
            cv.sombra = Capa(cv.raiz.transform, "sombra", 0.03f, out cv.rSombra, _mpbPlano);
            cv.relleno = Capa(cv.raiz.transform, "relleno", 0.02f, out cv.rRelleno, _mpbRoca);
            cv.bandas = Capa(cv.raiz.transform, "bandas", 0.01f, out cv.rBandas, _mpbPlano);
            cv.deco = Capa(cv.raiz.transform, "deco", 0.0f, out cv.rDeco, _mpbPlano);
            cv.raiz.SetActive(Modo != Nivel.Apagada);
            return cv;
        }

        private MeshFilter Capa(Transform padre, string nombre, float z, out MeshRenderer mr, MaterialPropertyBlock mpb)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(padre, false);
            // Mismo sortingOrder para las cuatro capas; la Z decide el orden entre ellas
            // (cámara orto: menor z = más cerca = se dibuja después). Así el relleno de
            // un chunk vecino nunca tapa las bandas de este: primero TODOS los rellenos,
            // luego TODAS las bandas, luego la decoración.
            go.transform.localPosition = new Vector3(0f, 0f, z);
            var mf = go.AddComponent<MeshFilter>();
            var m = new Mesh { name = "piel_" + nombre };
            m.MarkDynamic();
            mf.sharedMesh = m;
            mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _material;
            mr.sortingOrder = OrdenPiel;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            mr.SetPropertyBlock(mpb);
            return mf;
        }

        // =====================================================================
        // TEXTURA DE ROCA (procedural, determinista, cero assets)
        // =====================================================================
        private Texture2D GenerarTexturaRoca(int n)
        {
            var def = _universo.Get(MaterialId.Stone);
            Color32 b32 = def.baseColor;
            // Se tira ligeramente hacia la tinta parda de la paleta madre.
            Color baseCol = Color.Lerp(new Color(b32.r / 255f, b32.g / 255f, b32.b / 255f), new Color(0.41f, 0.355f, 0.315f), 0.6f); baseCol.a = 1f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear, name = "PielDeRoca_tex" };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float f = Fbm(x, y, n, 8, 3, 90);                 // grano fino
                    float mancha = Fbm(x, y, n, 2, 2, 91);            // manchas grandes (~12 celdas)
                    float est = Mathf.Sin((y + Fbm(x, y, n, 4, 2, 92) * 50f) * (Mathf.PI * 2f / 52f));
                    est = Mathf.Pow(Mathf.Max(0f, est), 3f);          // solo la cresta: una veta clara fina cada ~5 celdas
                    float placas = Mathf.Min(Worley(x, y, n, 4, 120), Worley(x, y, n, 6, 130) + 0.02f); // dos tamaños de placa
                    float mascara = Ruido(x * 3f / n + 0.5f, y * 3f / n + 0.5f, 3, 93);              // ~la mitad de las juntas se ve
                    float v = 1f + (f - 0.5f) * 0.20f + (mancha - 0.5f) * 0.18f + est * 0.05f;
                    if (mascara > 0.5f) v *= 0.80f + 0.20f * Mathf.Clamp01(placas / 0.06f);       // grieta fina y esporádica
                    if ((Hash(x, y, 77) % 100) < 2) v *= 0.85f;      // motas oscuras
                    if ((Hash(x, y, 78) % 300) < 1) v *= 1.14f;      // destellos de mica
                    Color c = baseCol * v; c.a = 1f;
                    px[y * n + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        private static float Ruido(float x, float y, int periodo, int sal = 90)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            int xa = ((x0 % periodo) + periodo) % periodo, xb = (xa + 1) % periodo, ya = ((y0 % periodo) + periodo) % periodo, yb = (ya + 1) % periodo;
            float a = Hash01(xa, ya, sal), b = Hash01(xb, ya, sal), c = Hash01(xa, yb, sal), d = Hash01(xb, yb, sal);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }
        /// <summary>F2−F1 de un Worley teselable (celdas de ~5 celdas de sim): pequeño = junta entre placas.</summary>
        private static float Worley(int x, int y, int n, int per, int sal)
        {
            float fx = x * per / (float)n, fy = y * per / (float)n;
            int cx = Mathf.FloorToInt(fx), cy = Mathf.FloorToInt(fy);
            float f1 = 9f, f2 = 9f;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int gx = cx + dx, gy = cy + dy;
                    int wx = ((gx % per) + per) % per, wy = ((gy % per) + per) % per;
                    float sx = gx + Hash01(wx, wy, sal), sy = gy + Hash01(wx, wy, sal + 1);
                    float d = (sx - fx) * (sx - fx) + (sy - fy) * (sy - fy);
                    if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) f2 = d;
                }
            return Mathf.Sqrt(f2) - Mathf.Sqrt(f1);
        }

        private static float Fbm(int x, int y, int n, int per, int octavas, int sal)
        {
            // Octavas teselables (periodos que dividen a n).
            float v = 0f, amp = 0.55f, suma = 0f;
            for (int o = 0; o < octavas; o++)
            {
                v += Ruido(x * per / (float)n, y * per / (float)n, per, sal) * amp;
                suma += amp; amp *= 0.5f; per *= 2;
            }
            return v / suma;
        }
    }
}
