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
        // -----------------------------------------------------------------
        // EL TALLER DEJA DE SER UNA PANTALLA (playtest 15)
        //
        // Hasta ahora el mundo medía exactamente un 16:9 y la cámara lo
        // encuadraba entero. Cesar: *"el juego se siente atascado... hay que
        // tirar cosas en otros lugares... esto es un problema serio de diseño y
        // ubicación de las cosas en el espacio"*, y propuso un laboratorio de
        // 2-3 pantallas de ancho y 1,5-2 de alto, con su razón exacta:
        // *"suficiente para que dos personas puedan estar trabajando en cosas
        // distintas sin verse constantemente"*. Eso no es estética: es el
        // REQUISITO DEL CO-OP. Un taller de una pantalla donde los dos ven lo
        // mismo todo el rato no es cooperativo.
        //
        // 768x288 = 3 pantallas de ancho x 2 de alto (256x144 cada una).
        // Son 221.184 celdas, SEIS VECES las 36.864 anteriores.
        //
        // Por qué esto NO exigió refactorizar la simulación: los chunks con
        // sueño (M1) ya procesaban solo lo activo. Lo que sí hubo que hacer es
        // que las tres pasadas que costaban proporcional al MUNDO y no a lo
        // VISIBLE (refresco completo del render, MorphTick, DiffuseTemperature)
        // pasaran a ser conscientes del viewport y del sueño de chunks.
        //
        // 768/16 = 48 y 288/16 = 18, ambos EXACTOS: se conserva la propiedad
        // que SimRenderer necesita (todos los chunks miden 16x16, un único
        // buffer scratch). Ver la guarda de SimRenderer.Init.
        // -----------------------------------------------------------------
        public const int W = 768;
        public const int H = 288; // 3x2 pantallas de 256x144

        public const int CHUNK = 16;
        public const int ChunksX = (W + CHUNK - 1) / CHUNK; // 48 (768/16 exacto)
        public const int ChunksY = (H + CHUNK - 1) / CHUNK; // 18 (288/16 exacto)

        /// <summary>Ancho y alto de UNA pantalla en celdas: el tamaño del mundo antiguo, y la unidad en la que se piensa el plano del taller.</summary>
        public const int PantallaW = 256;
        public const int PantallaH = 144;

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

        /// <summary>
        /// Temperatura ambiente "raw" de referencia (20 °C). Sigue siendo la
        /// BASE del taller y el valor por defecto de todo lo que no tiene una
        /// temperatura propia (lo que lleva el frasco, una redoma vacía...).
        /// Desde el playtest 17 es TAMBIÉN el ambiente real de todas y cada
        /// una de las celdas: el clima por zonas se retiró (ver
        /// <see cref="ambient"/>).
        /// </summary>
        public const byte AmbientRaw = 70;

        /// <summary>
        /// EL CLIMA DEL TALLER (playtest 15, REPLANTEADO EN EL 17).
        /// Temperatura ambiente por celda: el valor al que
        /// `SimStepper.DiffuseTemperature` tira cada celda cuando nada la
        /// calienta ni la enfría. Lo pinta `SimLevelBuilder.PaintClimate` una
        /// vez, al construir el nivel; el hot-path de la difusión solo hace
        /// una lectura de array — ni una rama más que con una constante.
        ///
        /// HOY ESTÁ UNIFORME: todas las celdas valen <see cref="AmbientRaw"/>.
        /// En el playtest 15 este array nació para dar CLIMA POR ZONA (un
        /// SÓTANO frío donde cristalizar costara menos frío activo, un CULTIVO
        /// templado donde criar costara menos calor: "el espacio deja de ser
        /// distancia y pasa a ser recurso"). Se retiró en el playtest 17 —
        /// el razonamiento completo, con las dos razones de Cesar y el coste
        /// medido de cada mitad, está en el docblock de `SimLevelBuilder`.
        ///
        /// EL ARRAY SE QUEDA, y no por inercia: el clima que vuelve es el que
        /// CREA EL JUGADOR (una fragua que entibia lo que tiene alrededor, una
        /// sala que se enfría porque él la selló). Eso es local por naturaleza
        /// y no cabe en una constante global — este array es exactamente su
        /// vehículo, y hoy no cuesta nada tenerlo listo.
        /// </summary>
        public readonly byte[] ambient;

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

        // =================================================================
        // PÁTINA — LA MEMORIA SUPERFICIAL (playtest 39, contrato ENCARGO S 1d)
        // =================================================================
        // Un único byte 0..255 por celda, escrito y leído EXCLUSIVAMENTE por
        // Sim/SimRenderer.cs (JAMÁS por SimStepper: el determinismo de la sim
        // no depende de este array en absoluto, y en multi cada cliente lo
        // reconstruye solo de lo que VE en su grilla replicada -- cero bytes
        // de tráfico de red). 0 = superficie limpia.
        //
        // UN SOLO CANAL PARA DOS LECTURAS (decisión de S, el contrato deja
        // los finos abiertos): valores bajos (hasta ~90) se leen como
        // HUMEDAD (un sólido que estuvo junto a líquido, oscurecido y con un
        // tinte frío) y decaen rápido -- "se seca solo"; valores altos
        // (hasta ~220) se leen como TIZNE (un sólido que estuvo junto a
        // Fuego/Brasa ardiendo, o bajo una bóveda con Humo pasando) y decaen
        // muchísimo más despacio -- el taller RECUERDA sus incendios. Ver
        // SimRenderer.ActualizarPatinaFranja/ComputeCellColor para el
        // acumulador y la traducción a color.
        public readonly byte[] patina;

        // =================================================================
        // LABORATORIO DE LEYES (R130, docs/LAB/DISENO_LABORATORIO.md §2)
        // =================================================================
        // Cuatro campos persistentes por celda para los procesos lentos del
        // laboratorio. Viven SIEMPRE (221 KB cada uno) pero solo los escribe
        // SimStepper cuando `SimStepper.LabActivo` (ModoLaboratorio); en el
        // juego normal quedan a 0 y ningún sistema los lee. NO viajan por la
        // red (SimSync solo replica mat[], ver docs/LAB/HANDOFF_OPUS.md §D).
        //
        // Semántica por material (una sola tabla, dos o tres lecturas cada uno):
        //   humedad: aire = vapor en el aire · Water = VOLUMEN (255 = celda
        //            llena) · porosos = agua contenida · roca = rocío · Planta = savia.
        //   carga:   Water = finos en suspensión (turbidez) · porosos = finos
        //            atrapados (colmatación) · Sedimento = fertilidad.
        //   reposo:  visitas de la pasada de campos sin moverse (quietud/edad).
        //   luz:     luz recibida (posicional: NO viaja en SwapCells).
        /// <summary>(R130 lab) Vapor en aire / volumen en agua / agua contenida en porosos / rocío en roca / savia en planta.</summary>
        public readonly byte[] humedad;
        /// <summary>(R130 lab) Finos en suspensión (agua) / colmatación (porosos) / fertilidad (Sedimento).</summary>
        public readonly byte[] carga;
        /// <summary>(R130 lab) Visitas de LabCampos sin moverse: quietud del agua, edad de compactación, temporizador de planta.</summary>
        public readonly byte[] reposo;
        /// <summary>(R130 lab) Luz recibida 0..255, recalculada por LabLuz cada 8 ticks. Posicional.</summary>
        public readonly byte[] luz;

        public CellGrid()
        {
            mat = new byte[W * H];
            temp = new byte[W * H];
            aux = new byte[W * H];
            morph = new byte[W * H];
            morphScratch = new byte[W * H];
            patina = new byte[W * H];
            humedad = new byte[W * H];
            carga = new byte[W * H];
            reposo = new byte[W * H];
            luz = new byte[W * H];
            ambient = new byte[W * H];
            for (int i = 0; i < ambient.Length; i++) ambient[i] = AmbientRaw;
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
            // (R130 lab) Materia nueva nace con sus campos limpios; el AGUA nace
            // LLENA (humedad = volumen = 255). Es lo que hace que un frasco
            // vertido, un manantial o un Transform() produzcan celdas de agua
            // completas sin que cada llamante tenga que saberlo.
            humedad[idx] = materialId == MaterialId.Water ? (byte)255 : (byte)0;
            carga[idx] = 0;
            reposo[idx] = 0;
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
            // (R130 lab) Los campos del laboratorio viajan con la sustancia
            // (el agua turbia se lleva su carga; la arena mojada su agua).
            // `luz` NO: es posicional y la recalcula LabLuz.
            (humedad[idxA], humedad[idxB]) = (humedad[idxB], humedad[idxA]);
            (carga[idxA], carga[idxB]) = (carga[idxB], carga[idxA]);
            (reposo[idxA], reposo[idxB]) = (reposo[idxB], reposo[idxA]);
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

        /// <summary>
        /// (playtest 39, contrato ENCARGO S 1d) Limpia la pátina de una celda
        /// (a 0). DEUDA DE INTEGRACIÓN PARA FABLE: el contrato pide que "el
        /// cincel y la mudanza LIMPIEN pátina donde tallan/restauran" para
        /// que no queden manchas flotando en aire nuevo -- Game/Cincel.cs y
        /// Game/Mudanza.cs NO son archivos de este encargo (propiedad
        /// disjunta, ver CLAUDE.md regla 37), así que esta ronda solo expone
        /// el helper; falta la llamada desde esos dos archivos en la
        /// integración.
        /// </summary>
        public void LimpiarPatina(int x, int y)
        {
            if (!InBounds(x, y)) return;
            patina[Idx(x, y)] = 0;
        }
    }
}
