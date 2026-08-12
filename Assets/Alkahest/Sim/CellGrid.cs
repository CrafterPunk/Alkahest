using System;

namespace Alkahest.Sim
{
    /// <summary>
    /// Almacén de datos "flat" de la simulación: un mundo de W x H celdas
    /// representado como arrays de bytes contiguos (SoA) para máxima
    /// localidad de caché y cero asignaciones en el hot-path.
    ///
    /// También lleva el "activity tracking" por chunks: un chunk que no ha
    /// tenido cambios en <see cref="SleepTicks"/> ticks se considera dormido
    /// y el stepper lo salta por completo.
    /// </summary>
    public sealed class CellGrid
    {
        // -----------------------------------------------------------------
        // REINGENIERÍA DEL ESPACIO (playtest 4). El mundo era 384x216: con la
        // cámara encuadrando el nivel entero, cada celda ocupaba ~1/384 del
        // ancho de pantalla y las reacciones se veían "pequeñitas"; además
        // obligaba a vuelos largos por un taller medio vacío.
        //
        // 256x144 es el mismo 16:9 EXACTO con un 33% menos de celdas por eje:
        // cada celda se ve un 50% MÁS GRANDE en pantalla (384/256 = 1.5) sin
        // tocar CellWorldSize ni una sola regla de la simulación.
        //
        // Bonus estructural: 144/16 = 9 EXACTO (y 256/16 = 16), así que ya no
        // existe la "fila de chunks recortada" que obligaba a SimRenderer a
        // mantener dos buffers scratch distintos (ver SimRenderer.Init).
        // -----------------------------------------------------------------
        public const int W = 256;
        public const int H = 144; // 256:144 == 16:9 exacto

        public const int CHUNK = 16;
        public const int ChunksX = (W + CHUNK - 1) / CHUNK; // 16 (256/16 exacto)
        public const int ChunksY = (H + CHUNK - 1) / CHUNK; // 9  (144/16 exacto: ya no hay chunk de borde)

        /// <summary>Ticks consecutivos sin actividad antes de dormir un chunk.</summary>
        public const int SleepTicks = 30;

        /// <summary>Id de material por celda.</summary>
        public readonly byte[] mat;
        /// <summary>Temperatura "raw" 0..255 por celda. Ver <see cref="RawToC"/>/<see cref="CToRaw"/>.</summary>
        public readonly byte[] temp;
        /// <summary>Byte auxiliar: vida restante de gas/fuego, memoria de flujo de líquidos, estado orgánico...</summary>
        public readonly byte[] aux;
        /// <summary>Sello de frame/tick en el que se tocó por última vez cada celda (evita procesar dos veces la misma celda en un tick por un swap).</summary>
        public readonly uint[] touchedTick;

        /// <summary>Ticks consecutivos sin actividad, por chunk.</summary>
        public readonly byte[] chunkSleepTimer;
        /// <summary>Último tick en el que un chunk recibió actividad (para no incrementar su sleepTimer más de una vez por tick).</summary>
        public readonly uint[] chunkTouchedTick;

        /// <summary>Temperatura ambiente "raw" (20°C).</summary>
        public const byte AmbientRaw = 70;

        /// <summary>
        /// Bit de <see cref="aux"/> para celdas Organic (Vivium) fuera de su
        /// banda de temperatura de crecimiento: "dormido", sin crecimiento,
        /// leído por SimRenderer para una ligera desaturación visual. No se
        /// usa para ningún otro arquetipo (no colisiona con el bit 0x80 de
        /// "asentado" que usa SimStepper para Organic, ni con el uso de aux
        /// como vida de gas/fuego o memoria de flujo de líquidos).
        /// </summary>
        public const byte OrganicDormantAux = 0x40;

        // =================================================================
        // CAMPO MORFOLÓGICO (playtest 12)
        // =================================================================
        // El "dibujo interno" de cada sustancia NO se guarda: se REGENERA.
        // Cada celda lleva un byte de estado que evoluciona por la regla local
        // de la familia morfológica de su material (ver Sim/MaterialDef.cs,
        // PatronMorfologico) y que SimRenderer traduce a píxeles.
        //
        // Por qué un campo y no una textura por material: porque así el patrón
        // es una PROPIEDAD FÍSICA y no un adorno. Si aspiras materia y la
        // viertes en otro sitio, el campo arranca desde una semilla de posición
        // y la regla lo lleva otra vez hacia el mismo tipo de dibujo — no el
        // mismo dibujo, el mismo TIPO. Esa era exactamente la petición.
        //
        // `morphScratch` es el doble búfer que necesitan las familias de
        // reacción-difusión (Manchas, Laberinto): leer y escribir el mismo
        // array daría un resultado dependiente del orden de recorrido, que es
        // justo lo que rompe el determinismo del que depende el netcode futuro.

        /// <summary>Estado morfológico por celda. Lo evoluciona SimStepper.MorphTick y lo lee SimRenderer.</summary>
        public readonly byte[] morph;
        /// <summary>Doble búfer para las reglas morfológicas que leen vecinos (reacción-difusión). Solo lo usa SimStepper.</summary>
        public readonly byte[] morphScratch;

        public CellGrid()
        {
            mat = new byte[W * H];
            temp = new byte[W * H];
            aux = new byte[W * H];
            morph = new byte[W * H];
            morphScratch = new byte[W * H];
            touchedTick = new uint[W * H];
            chunkSleepTimer = new byte[ChunksX * ChunksY];
            chunkTouchedTick = new uint[ChunksX * ChunksY];
            for (int i = 0; i < chunkTouchedTick.Length; i++) chunkTouchedTick[i] = uint.MaxValue;

            for (int i = 0; i < temp.Length; i++) temp[i] = AmbientRaw;
        }

        // ---- Conversión de temperatura -------------------------------------------------
        // raw en [0,255] <-> Celsius en [-120, 390] aprox. C = raw*2 - 120.
        public static int RawToC(byte raw) => raw * 2 - 120;

        public static byte CToRaw(int celsius)
        {
            int raw = (celsius + 120) / 2;
            if (raw < 0) raw = 0;
            if (raw > 255) raw = 255;
            return (byte)raw;
        }

        // ---- Indexado --------------------------------------------------------------------
        public static int Idx(int x, int y) => y * W + x;

        public static bool InBounds(int x, int y) => x >= 0 && x < W && y >= 0 && y < H;

        public byte GetMat(int x, int y) => mat[Idx(x, y)];
        public byte GetMat(int idx) => mat[idx];

        public void SetCell(int idx, byte materialId, bool resetAux = true)
        {
            mat[idx] = materialId;
            if (resetAux) aux[idx] = 0;
            // (playtest 12) Materia nueva nace con una SEMILLA morfológica, no
            // con un cero: un campo plano tarda mucho en romper la simetría y
            // se vería un instante de materia lisa antes de aparecer el patrón.
            // Hash barato de (posición, material) — sin divisiones, esto está
            // en el hot path. La regla de la familia hará el resto.
            morph[idx] = (byte)(idx * 37 + materialId * 101 + 13);
        }

        public void SetCell(int x, int y, byte materialId, bool resetAux = true)
        {
            SetCell(Idx(x, y), materialId, resetAux);
        }

        /// <summary>Intercambia dos celdas por completo (material, temperatura y aux viajan con la sustancia).</summary>
        public void SwapCells(int idxA, int idxB)
        {
            (mat[idxA], mat[idxB]) = (mat[idxB], mat[idxA]);
            (temp[idxA], temp[idxB]) = (temp[idxB], temp[idxA]);
            (aux[idxA], aux[idxB]) = (aux[idxB], aux[idxA]);
            // El estado morfológico viaja CON la sustancia (playtest 12): un
            // líquido que fluye arrastra su dibujo y lo va reacomodando, en vez
            // de dejar el patrón clavado a las coordenadas del mundo.
            (morph[idxA], morph[idxB]) = (morph[idxB], morph[idxA]);
        }

        // ---- Chunks ------------------------------------------------------------------------
        public static int ChunkIndex(int cx, int cy) => cy * ChunksX + cx;

        public static void CellToChunk(int x, int y, out int cx, out int cy)
        {
            cx = x / CHUNK;
            cy = y / CHUNK;
        }

        /// <summary>Límites [x0,x1) [y0,y1) de un chunk, recortados a la grilla real.</summary>
        public static void ChunkBounds(int cx, int cy, out int x0, out int y0, out int x1, out int y1)
        {
            x0 = cx * CHUNK;
            y0 = cy * CHUNK;
            x1 = Math.Min(x0 + CHUNK, W);
            y1 = Math.Min(y0 + CHUNK, H);
        }

        public bool IsChunkAwake(int cx, int cy)
        {
            return chunkSleepTimer[ChunkIndex(cx, cy)] < SleepTicks;
        }

        /// <summary>Despierta el chunk que contiene (x,y) y sus 8 vecinos (para que las reacciones que cruzan el borde de chunk no se pierdan).</summary>
        public void WakeChunk(int x, int y, uint tick)
        {
            CellToChunk(x, y, out int cx, out int cy);
            for (int dy = -1; dy <= 1; dy++)
            {
                int ny = cy + dy;
                if (ny < 0 || ny >= ChunksY) continue;
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx;
                    if (nx < 0 || nx >= ChunksX) continue;
                    int ci = ChunkIndex(nx, ny);
                    chunkSleepTimer[ci] = 0;
                    chunkTouchedTick[ci] = tick;
                }
            }
        }

        /// <summary>Marca un chunk como "sin cambios este tick" (llamado por el stepper tras procesarlo).</summary>
        public void TickChunkIdle(int cx, int cy)
        {
            int idx = ChunkIndex(cx, cy);
            if (chunkSleepTimer[idx] < 255) chunkSleepTimer[idx]++;
        }

        public void WakeChunkIndex(int cx, int cy, uint tick)
        {
            if (cx < 0 || cx >= ChunksX || cy < 0 || cy >= ChunksY) return;
            int ci = ChunkIndex(cx, cy);
            chunkSleepTimer[ci] = 0;
            chunkTouchedTick[ci] = tick;
        }
    }
}
