using System;
using System.Diagnostics;

namespace Alkahest.Sim
{
    /// <summary>
    /// Núcleo de la simulación: avanza la grilla un tick determinista.
    /// Todo el código de este archivo es hot-path: nada de LINQ, nada de
    /// colecciones que asignen memoria, nada de UnityEngine.Random.
    ///
    /// Orden de un tick:
    ///   1) Difusión de temperatura (1/8 de las celdas, interleaved por tick%8).
    ///   2) Barrido de celdas activas fila a fila de abajo hacia arriba
    ///      (evita que una celda caiga varias filas en el mismo tick),
    ///      alternando la dirección de recorrido en X según la paridad
    ///      del tick (evita sesgo direccional en las diagonales).
    ///   3) Por celda: transición de fase genérica (fusión/ebullición/...)
    ///      seguida de la regla específica de su arquetipo.
    ///   4) Actualización de temporizadores de sueño por chunk.
    ///   5) MorphTick: evolución del campo morfológico (playtest 12) para
    ///      las 5 familias que lo necesitan (Manchas, Laberinto, Dendritas,
    ///      Pulso, Motas) -- ver la cabecera de <see cref="MorphTick"/>.
    ///
    /// Determinismo: toda aleatoriedad usa <see cref="XorShift"/> sembrado
    /// a partir de (tick, x, y, sal), nunca UnityEngine.Random ni estado
    /// mutable compartido entre celdas dentro del mismo tick.
    /// </summary>
    public sealed class SimStepper
    {
        private const int W = CellGrid.W;
        private const int H = CellGrid.H;

        private readonly Universe _universe;
        private readonly CellGrid _grid;
        private readonly Stopwatch _sw = new Stopwatch();

        private uint _tick;

        public uint Tick => _tick;
        public int ActiveCells { get; private set; }
        public int ActiveChunks { get; private set; }
        public double LastStepMs { get; private set; }

        // ---------------------------------------------------------------------------------
        // Ring buffer de eventos notables (M4: SubstanceKnowledge lee esto cada frame para
        // los "auto-observaciones" del diario -- vistoArder, vistoCristalizar, etc).
        // Array fijo, sin asignaciones por evento; al llenarse, sobrescribe los más
        // antiguos (si un lector externo se queda más de 256 eventos por detrás pierde
        // los más viejos, aceptable para este uso: solo alimenta notas de diario).
        // ---------------------------------------------------------------------------------
        public const int EventBufferSize = 256;
        private readonly SimNotableEvent[] _events = new SimNotableEvent[EventBufferSize];
        private int _eventHead;

        // ---------------------------------------------------------------------------------
        // (playtest 15 -- mundo a 768x288, 6x) Búfer reutilizado tick a tick por MorphTick
        // para marcar qué chunks son "relevantes" este tick (ver MorphTick para el porqué).
        // Preasignado UNA vez aquí -- CERO asignaciones en el hot path, igual que _events.
        // ---------------------------------------------------------------------------------
        private readonly bool[] _morphChunkRelevant = new bool[CellGrid.ChunksX * CellGrid.ChunksY];

        // ---------------------------------------------------------------------------------
        // (playtest 18) LIMITADOR DE RITMO POR LEY -- CONTRATO_FASE3.md sección 7, NO
        // OPCIONAL. El anillo de arriba tiene 256 entradas y lo leen tres clases sin
        // consumirlo (CLAUDE.md regla 8). Un ácido disolviendo ya empuja decenas de eventos
        // Dissolve por tick; si CADA reacción empujara ADEMÁS un evento Ley sin límite, el
        // anillo daría la vuelta antes de que el consumidor lo leyera y una ley podría no
        // descubrirse NUNCA -- un bug intermitente y dependiente de la carga, de los que se
        // tardan varias rondas en ver, precisamente porque solo aparece bajo carga alta.
        //
        // _ultimoTickPorLey[i] guarda el último tick en el que se empujó un evento Ley para
        // Universe.Leyes[i]; PushLeyEvent solo empuja si es la PRIMERA vez para esa ley o si
        // pasaron >= LeyEventCooldownTicks (30 ticks = 1s a 30Hz, ver Step) desde el anterior
        // empujón de ESA MISMA ley -- otras leyes no comparten cooldown entre sí. Array
        // preasignado al tamaño de Universe.Leyes en el constructor: cero allocs en el hot
        // path, y determinista (depende solo del historial de ticks de la sim, nunca de
        // temporizadores de reloj real). Se "reinicia" porque AlkahestSim.Start() crea un
        // SimStepper NUEVO cada vez que la sim se reinicia (ver ese archivo) -- no hace falta
        // un método Reset aparte, el array nace limpio con cada instancia.
        // ---------------------------------------------------------------------------------
        private const uint LeyEventCooldownTicks = 30;
        private const uint LeyEventNuncaEmpujado = uint.MaxValue; // sentinela: "aún no se ha empujado ningún evento para esta ley".
        private readonly uint[] _ultimoTickPorLey;

        // ---------------------------------------------------------------------------------
        // LO QUE PERSISTE (playtest 25, CONTRATO_PERSISTE.md sección 4.3) -- sales propias
        // de los dos procesos nuevos (regla del proyecto: sal propia por uso nuevo). Ambas
        // como `const uint` a propósito (no `int`): así `(uint)seed ^ SalLimoSeparacion` no
        // necesita conversión implícita de constante -- ya son del mismo tipo que pide
        // XorShift.FromCell (regla 21 de CLAUDE.md, cast explícito toda sal que mezcle
        // constante+campo).
        // ---------------------------------------------------------------------------------
        private const uint SalDisolucion = 237;
        private const uint SalLimoSeparacion = 239;
        /// <summary>Temp raw a partir de la cual el Limo se separa (contrato 4.3, valor EXACTO del contrato). Nota de honestidad para quien audite esto: el propio contrato describe este umbral como "alcanzable con el rescoldo tier0" (~116°C), pero 150 &gt; Universe.CrisolTier0Raw (118) -- con solo el rescoldo NUNCA se alcanza; hace falta combustible (tier1). Implementado tal cual el número congelado; discrepancia anotada como pregunta al director, no corregida aquí.</summary>
        // (fix integración) 112 (= 104 °C), no los 150 del contrato original: el
        // contrato decía "150 (60°C)" -- aritmética rota, raw 150 son 180 °C, por
        // encima del rescoldo tier 0 del crisol (Universe.CrisolTier0Raw = 120) que
        // el propio contrato promete como suficiente para separar el limo. 112 está
        // SIEMPRE al alcance del tier 0 y por encima del ambiente (70): hervir limo
        // es el primer gesto del juego y no puede exigir combustible que aún no
        // existe. La separación es del LIMO, no del agua: no depende del
        // waterBoilC sorteado. (Regla 50.)
        private const byte LimoSeparaRaw = 112;

        /// <summary>Array fijo (no crece) de eventos notables; leer entre un "lastSeenHead" propio y <see cref="EventHead"/>, ambos módulo EventBufferSize.</summary>
        public SimNotableEvent[] Events => _events;
        public int EventHead => _eventHead;

        public SimStepper(Universe universe, CellGrid grid)
        {
            _universe = universe;
            _grid = grid;

            _ultimoTickPorLey = new uint[universe.Leyes.Length];
            for (int i = 0; i < _ultimoTickPorLey.Length; i++) _ultimoTickPorLey[i] = LeyEventNuncaEmpujado;
        }

        /// <summary>
        /// (playtest 18) `leyIndice` por defecto -1: TODOS los caminos viejos
        /// (Ignite/Boil/Freeze/Crystallize/Grow/Dissolve) siguen empujando
        /// exactamente igual que antes y jamás identifican una ley. Solo
        /// <see cref="PushLeyEvent"/> pasa un índice real.
        /// </summary>
        private void PushEvent(SimEventType type, byte matId, int x, int y, short leyIndice = -1)
        {
            ref var e = ref _events[_eventHead];
            e.type = type;
            e.matId = matId;
            e.x = (short)x;
            e.y = (short)y;
            e.tick = _tick;
            e.leyIndice = leyIndice;
            _eventHead = (_eventHead + 1) & (EventBufferSize - 1);
        }

        /// <summary>
        /// Empuja un evento SimEventType.Ley para <paramref name="leyIndice"/>,
        /// sujeto al limitador de ritmo por ley (ver <see cref="_ultimoTickPorLey"/>
        /// arriba). Llamado desde TryReactNeighbor (leyes de contacto) y desde
        /// GrowthTick (la ley de crecimiento del Vivium, con leyIndice ==
        /// Universe.LeyCrecimientoIndice) -- mismo limitador para las dos,
        /// ninguna ley puede saturar el anillo por sí sola.
        /// </summary>
        private void PushLeyEvent(int leyIndice, byte matId, int x, int y)
        {
            if ((uint)leyIndice >= (uint)_ultimoTickPorLey.Length) return; // defensivo: no debería ocurrir con un Universe.Leyes bien construido.
            uint ultimo = _ultimoTickPorLey[leyIndice];
            if (ultimo != LeyEventNuncaEmpujado && _tick - ultimo < LeyEventCooldownTicks) return;
            _ultimoTickPorLey[leyIndice] = _tick;
            PushEvent(SimEventType.Ley, matId, x, y, (short)leyIndice);
        }

        public void Step()
        {
            _sw.Restart();
            _tick++;

            DiffuseTemperature();

            int activeCells = 0;
            bool forward = (_tick & 1u) == 0u;

            for (int y = 0; y < H; y++)
            {
                int cy = y / CellGrid.CHUNK;
                if (forward)
                {
                    for (int x = 0; x < W; x++)
                        activeCells += ProcessIfNeeded(x, y, cy);
                }
                else
                {
                    for (int x = W - 1; x >= 0; x--)
                        activeCells += ProcessIfNeeded(x, y, cy);
                }
            }

            ActiveCells = activeCells;

            int awakeChunks = 0;
            for (int cy = 0; cy < CellGrid.ChunksY; cy++)
            {
                for (int cx = 0; cx < CellGrid.ChunksX; cx++)
                {
                    int ci = CellGrid.ChunkIndex(cx, cy);
                    if (_grid.chunkTouchedTick[ci] != _tick)
                    {
                        _grid.TickChunkIdle(cx, cy);
                    }
                    if (_grid.IsChunkAwake(cx, cy)) awakeChunks++;
                }
            }
            ActiveChunks = awakeChunks;

            MorphTick();

            _sw.Stop();
            LastStepMs = _sw.Elapsed.TotalMilliseconds;
        }

        // Posición final de la celda tras Process*/Move este tick, actualizada por Move()
        // y consultada tras el switch para saber dónde chequear reacciones de contacto
        // (M3: ReactionEngine). Reiniciada al comienzo de cada ProcessIfNeeded.
        private bool _cellMoved;
        private int _cellFinalX, _cellFinalY, _cellFinalIdx;

        private int ProcessIfNeeded(int x, int y, int cy)
        {
            int cx = x / CellGrid.CHUNK;
            if (!_grid.IsChunkAwake(cx, cy)) return 0;

            int idx = CellGrid.Idx(x, y);
            if (_grid.touchedTick[idx] == _tick) return 0;
            _grid.touchedTick[idx] = _tick;

            byte m = _grid.mat[idx];
            if (m == MaterialId.Empty) return 0;

            ApplyPhase(idx);
            m = _grid.mat[idx];
            var def = _universe.Get(m);

            _cellMoved = false;
            _cellFinalX = x;
            _cellFinalY = y;
            _cellFinalIdx = idx;

            switch (def.archetype)
            {
                case MaterialArchetype.Powder:
                    ProcessPowder(x, y, idx, def);
                    MaybeReact(_cellFinalX, _cellFinalY, _cellFinalIdx, _cellMoved);
                    break;
                case MaterialArchetype.Liquid:
                    ProcessLiquid(x, y, idx, def);
                    MaybeReact(_cellFinalX, _cellFinalY, _cellFinalIdx, _cellMoved);
                    break;
                case MaterialArchetype.Gas:
                    ProcessGas(x, y, idx, def);
                    break;
                case MaterialArchetype.Fire:
                    ProcessFire(x, y, idx, def);
                    break;
                case MaterialArchetype.Organic:
                    ProcessOrganic(x, y, idx, def);
                    MaybeReact(_cellFinalX, _cellFinalY, _cellFinalIdx, _cellMoved);
                    break;
                case MaterialArchetype.StaticSolid:
                    // (fix playtest) El hielo YA NO inyecta frío a vecinos: creaba una zona fría
                    // autosostenida que seguía congelando tras apagar la piedra fría.
                    //
                    // (playtest 29, GRAVEDAD CON COHESIÓN -- decisión de Cesar)
                    // Los sólidos marcados con def.caeSolido (estados del
                    // retículo, hielo, cristal -- JAMÁS la piedra, que es la
                    // arquitectura del mundo) caen recto al perder apoyo,
                    // con principio de MÉNSULA: se sostienen si a
                    // ≤ cohesionCeldas en horizontal, por materia sólida
                    // continua, alguien tiene apoyo debajo. Vigas y voladizos
                    // sensatos sí; alfombras flotantes no. Coste: solo corre
                    // en chunks despiertos (un sólido asentado duerme igual
                    // que siempre) y el escaneo lateral está acotado por
                    // cohesionCeldas (≤8).
                    if (def.caeSolido) ProcessSolidoCohesion(x, y, idx, def);
                    break;
            }

            return 1;
        }

        // ---------------------------------------------------------------------------------
        // Reacciones de contacto (M3: ReactionEngine, tabla horneada por Universe.Create).
        // Coste acotado: solo se comprueba si la celda se movió este tick, o (para celdas
        // asentadas) con un muestreo de 1/8 por tick -- igual que la difusión de calor.
        // ---------------------------------------------------------------------------------
        private void MaybeReact(int x, int y, int idx, bool moved)
        {
            if (!moved && ((x + y + (int)_tick) & 7) != 0) return;
            ProcessReactions(x, y, idx);
        }

        private void ProcessReactions(int x, int y, int idx)
        {
            byte matSelf = _grid.mat[idx];
            if (matSelf == MaterialId.Empty) return;

            // ---------------------------------------------------------------
            // MORFOLOGÍA DE CRECIMIENTO (playtest 12) -- cristalización.
            // Solo aplica cuando SELF es CrystalSeed: Crystal en sí es
            // StaticSolid y el switch de ProcessIfNeeded NUNCA llama a
            // MaybeReact/ProcessReactions para StaticSolid (ver ese switch),
            // así que Crystal jamás inicia su propio barrido de reacciones.
            // La reacción Azoth+Crystal (bulk) solo puede dispararse desde
            // el lado de AZOTH (self=Azoth), y ahí el producto que cambia es
            // SIEMPRE self (el propio Azoth se autoconvierte, ver la tabla
            // en Universe.Create: productA==productB==Crystal para esa
            // entrada) -- no hay ningún vecino que "elegir" en ese caso, la
            // forma no se puede sesgar por ese camino. La ÚNICA reacción de
            // cristalización que de verdad convierte a un VECINO (no a sí
            // misma) es Azoth+CrystalSeed vista desde CrystalSeed
            // (productNeighbor==Crystal): ahí sí hay una elección real de
            // vecino que hacer, y es la que se sesga aquí.
            if (matSelf == MaterialId.CrystalSeed && TryCrystalGrowth(x, y, idx, ref matSelf)) return;

            if (TryReactNeighbor(x, y, idx, ref matSelf, x - 1, y)) return;
            if (TryReactNeighbor(x, y, idx, ref matSelf, x + 1, y)) return;
            if (TryReactNeighbor(x, y, idx, ref matSelf, x, y - 1)) return;
            TryReactNeighbor(x, y, idx, ref matSelf, x, y + 1);
        }

        /// <summary>
        /// (playtest 12) Elige a qué Azoth vecino le toca cristalizar según
        /// la firma del Cristal (<see cref="MaterialDef.patron"/> de
        /// MaterialId.Crystal), en vez del orden fijo Izq/Der/Abajo/Arriba
        /// de <see cref="ProcessReactions"/>. GARANTÍA DE TASA: dentro de
        /// <see cref="TryReactNeighbor"/> la tirada de probabilidad usa
        /// <c>XorShift.FromCell(_tick, x, y, 77)</c> -- las MISMAS
        /// coordenadas (x,y) para los 4 posibles vecinos de esta celda, así
        /// que el primer número que produce el generador es idéntico sin
        /// importar cuál de los vecinos se compruebe primero. Como además
        /// el umbral de temperatura se evalúa contra <c>_grid.temp[idx]</c>
        /// (la temperatura de SELF, no la del vecino), el resultado de la
        /// tirada para "¿cristaliza este tick?" es el MISMO para cualquier
        /// candidato Azoth de esta celda -- reordenar los vecinos cambia
        /// SOLO cuál se convierte cuando la tirada sale bien, nunca si sale
        /// bien. Si el candidato elegido falla, <see cref="ProcessReactions"/>
        /// cae al barrido de siempre (reintenta el mismo u otro vecino con
        /// el mismo resultado determinista, y sigue cubriendo Ácido u otras
        /// reacciones no relacionadas con el cristal).
        /// </summary>
        private bool TryCrystalGrowth(int x, int y, int idx, ref byte matSelf)
        {
            var crystalDef = _universe.Get(MaterialId.Crystal);
            int bestDir = -1;

            if (crystalDef.patron == PatronMorfologico.Celdas)
            {
                // COMPACTO: puntúa cada candidato Azoth por cuántos vecinos
                // suyos YA son Crystal/CrystalSeed y elige el más rodeado --
                // así el cristal rellena huecos en vez de alargar un frente.
                int bestScore = -1;
                for (int d = 0; d < 4; d++)
                {
                    if (!IsAzothDir(x, y, d)) continue;
                    int nx = x + DirX[d], ny = y + DirY[d];
                    int score = CountCrystalNeighbors(nx, ny);
                    if (score > bestScore) { bestScore = score; bestDir = d; }
                }
            }
            else
            {
                // DENDRÍTICO (Dendritas) o LAMINAR (cualquier otro plausible
                // para StaticSolid: Vetas/Liso, ver FamiliasPlausibles) --
                // orden de preferencia fijo por la firma del Cristal.
                GetCrystalDirOrder(crystalDef, out int d0, out int d1, out int d2, out int d3);
                if (IsAzothDir(x, y, d0)) bestDir = d0;
                else if (IsAzothDir(x, y, d1)) bestDir = d1;
                else if (IsAzothDir(x, y, d2)) bestDir = d2;
                else if (IsAzothDir(x, y, d3)) bestDir = d3;
            }

            if (bestDir < 0) return false;
            int tx = x + DirX[bestDir], ty = y + DirY[bestDir];
            return TryReactNeighbor(x, y, idx, ref matSelf, tx, ty);
        }

        /// <summary>
        /// Orden de comprobación de los 4 vecinos ortogonales para el modo
        /// DENDRÍTICO/LAMINAR de <see cref="TryCrystalGrowth"/>. Determinista
        /// por <see cref="MaterialDef.semillaPatron"/> del Cristal (NO usa
        /// XorShift de tick: la FORMA de crecimiento debe ser estable en el
        /// tiempo, no un ruido distinto cada tick).
        ///   Dendritas  -> sesgo FUERTE a un único eje preferido (crece en
        ///                 punta, casi nunca a los lados): mismo criterio de
        ///                 semillaPatron que <see cref="MorphDendrites"/>,
        ///                 para que forma y textura cuenten la misma
        ///                 historia direccional.
        ///   Vetas/Liso -> LAMINAR: sesgo a UN EJE completo (las dos
        ///                 direcciones opuestas de ese eje primero), no a
        ///                 una única punta -- una lámina crece hacia los dos
        ///                 lados de su plano, no en una sola dirección.
        /// </summary>
        private void GetCrystalDirOrder(MaterialDef crystalDef, out int d0, out int d1, out int d2, out int d3)
        {
            if (crystalDef.patron == PatronMorfologico.Dendritas)
            {
                int pref = crystalDef.semillaPatron & 3;
                d0 = pref;
                d1 = (pref + 1) & 3;
                d2 = (pref + 2) & 3;
                d3 = (pref + 3) & 3;
                return;
            }

            // Laminar: DirX/DirY = {Izq,Der,Abajo,Arriba} (índices 0,1,2,3).
            // semillaPatron decide si el eje preferido es horizontal o
            // vertical, fijo para toda la run (una sola tirada, en el bit
            // menos significativo).
            bool horizontal = (crystalDef.semillaPatron & 1) == 0;
            if (horizontal) { d0 = 0; d1 = 1; d2 = 2; d3 = 3; }
            else { d0 = 2; d1 = 3; d2 = 0; d3 = 1; }
        }

        private bool IsAzothDir(int x, int y, int dir)
        {
            int nx = x + DirX[dir], ny = y + DirY[dir];
            return CellGrid.InBounds(nx, ny) && _grid.mat[CellGrid.Idx(nx, ny)] == MaterialId.Azoth;
        }

        private int CountCrystalNeighbors(int x, int y)
        {
            int c = 0;
            if (CellGrid.InBounds(x - 1, y)) { byte m = _grid.mat[CellGrid.Idx(x - 1, y)]; if (m == MaterialId.Crystal || m == MaterialId.CrystalSeed) c++; }
            if (CellGrid.InBounds(x + 1, y)) { byte m = _grid.mat[CellGrid.Idx(x + 1, y)]; if (m == MaterialId.Crystal || m == MaterialId.CrystalSeed) c++; }
            if (CellGrid.InBounds(x, y - 1)) { byte m = _grid.mat[CellGrid.Idx(x, y - 1)]; if (m == MaterialId.Crystal || m == MaterialId.CrystalSeed) c++; }
            if (CellGrid.InBounds(x, y + 1)) { byte m = _grid.mat[CellGrid.Idx(x, y + 1)]; if (m == MaterialId.Crystal || m == MaterialId.CrystalSeed) c++; }
            return c;
        }

        /// <summary>Consulta y, si procede, aplica la reacción entre la celda (x,y)/idx y su vecino (nx,ny). Devuelve true si algo reaccionó (para dejar de comprobar más vecinos: matSelf pudo cambiar).</summary>
        private bool TryReactNeighbor(int x, int y, int idx, ref byte matSelf, int nx, int ny)
        {
            if (!CellGrid.InBounds(nx, ny)) return false;
            int nidx = CellGrid.Idx(nx, ny);
            byte matNeighbor = _grid.mat[nidx];
            if (matNeighbor == MaterialId.Empty) return false;

            if (!_universe.Reactions.TryGet(matSelf, matNeighbor, out var reaction, out int reactionIndex)) return false;

            byte t = _grid.temp[idx];
            if (t < reaction.minTempRaw || t > reaction.maxTempRaw) return false;

            var rng = XorShift.FromCell(_tick, x, y, 77);
            if (!rng.ChancePercent(reaction.chancePct)) return false;

            // (playtest 18) La ley acaba de ocurrir de verdad (pasó banda térmica
            // y tirada de probabilidad): empuja su evento Ley, con el ÍNDICE
            // ESTABLE que TryGet acaba de devolver -- Universe.Leyes[reactionIndex]
            // describe exactamente esta Reaction (invariante del contrato). Sujeto
            // al limitador de ritmo por ley, igual que GrowthTick.
            PushLeyEvent(reactionIndex, matSelf, x, y);

            byte productSelf, productNeighbor;
            if (reaction.a == matSelf)
            {
                productSelf = reaction.productA;
                productNeighbor = reaction.productB;
            }
            else
            {
                productSelf = reaction.productB;
                productNeighbor = reaction.productA;
            }

            byte origSelf = matSelf;
            byte origNeighbor = matNeighbor;

            if (productNeighbor != origNeighbor)
            {
                NotifyReactionEvent(origNeighbor, productNeighbor, nx, ny);
                Transform(nidx, productNeighbor);
            }
            if (productSelf != origSelf)
            {
                NotifyReactionEvent(origSelf, productSelf, x, y);
                Transform(idx, productSelf);
                matSelf = productSelf;
            }

            return true;
        }

        /// <summary>Traduce un cambio de material producido por una reacción en un evento notable (Crystallize/Dissolve) si corresponde. No todos los cambios de reacción son "notables" (p.ej. Acid+Water->Slime,Slime no se registra hoy).</summary>
        private void NotifyReactionEvent(byte from, byte to, int x, int y)
        {
            if (to == MaterialId.Smoke)
            {
                PushEvent(SimEventType.Dissolve, from, x, y);
            }
            else if (from == MaterialId.Azoth && to == MaterialId.Crystal)
            {
                PushEvent(SimEventType.Crystallize, MaterialId.Azoth, x, y);
            }
        }

        // ---------------------------------------------------------------------------------
        // Transiciones de fase genéricas (temperatura -> cambio de material).
        // ---------------------------------------------------------------------------------
        private void ApplyPhase(int idx)
        {
            byte m = _grid.mat[idx];
            var def = _universe.Get(m);
            byte t = _grid.temp[idx];

            if (def.meltsAt != short.MaxValue && t >= def.meltsAt)
            {
                Transform(idx, def.meltsInto);
            }
            else if (def.freezesAt != short.MinValue && t <= def.freezesAt)
            {
                PushEvent(SimEventType.Freeze, m, idx % W, idx / W);
                Transform(idx, def.freezesInto);
            }
            else if (def.boilsAt != short.MaxValue && t >= def.boilsAt)
            {
                PushEvent(SimEventType.Boil, m, idx % W, idx / W);
                Transform(idx, def.boilsInto);
            }
            else if (def.condensesAt != short.MinValue && t <= def.condensesAt)
            {
                Transform(idx, def.condensesInto);
            }
            // (fix playtest 9) AUTOIGNICIÓN POR TEMPERATURA. Antes un material inflamable
            // SOLO ardía si un vecino YA era Fuego -- TryIgnite (más abajo) solo se llama
            // desde ProcessFire, es decir, desde una celda que YA está ardiendo. Pero la
            // única forma "legal" de CONSEGUIR fuego en el juego es la Placa ígnea en
            // ARDIENTE bajo aceite (no hay grifo de fuego, a propósito): sin esta rama, el
            // aceite podía superar de sobra su ignitionTemp y NUNCA prendía, porque no
            // había ninguna llama alrededor que disparase TryIgnite. Genérica igual que
            // melt/freeze/boil/condense de arriba: cualquier MaterialDef con `flammable` +
            // `ignitionTemp` finito autoenciende al cruzar el umbral, sin necesitar contacto.
            // Placa Ardiente = raw 220 (320 °C); ignitionTemp del aceite varía por seed en
            // ~208..312 °C (raw ~164..216) -- 220 la supera SIEMPRE, así que el camino
            // placa->aceite->fuego ahora sí funciona para cualquier seed.
            else if (def.flammable && def.ignitionTemp != short.MaxValue && t > def.ignitionTemp)
            {
                PushEvent(SimEventType.Ignite, m, idx % W, idx / W);
                Transform(idx, def.burnsInto);
            }
        }

        /// <summary>Convierte la celda en otro material, inicializando aux según el arquetipo destino (vida de gas/fuego).</summary>
        private void Transform(int idx, byte newId)
        {
            var def = _universe.Get(newId);
            _grid.mat[idx] = newId;

            if (def.archetype == MaterialArchetype.Gas || def.archetype == MaterialArchetype.Fire)
            {
                if (def.gasLifetime > 0)
                {
                    int x = idx % W, y = idx / W;
                    var rng = XorShift.FromCell(_tick, x, y, 42);
                    int jitter = rng.Next(9) - 4; // -4..+4, evita que celdas creadas juntas expiren en el mismo tick exacto
                    int v = def.gasLifetime + jitter;
                    if (v < 1) v = 1;
                    if (v > 255) v = 255;
                    _grid.aux[idx] = (byte)v;
                }
                else
                {
                    _grid.aux[idx] = 0;
                }
            }
            else
            {
                _grid.aux[idx] = 0;
            }

            int cx = idx % W, cy = idx / W;
            _grid.WakeChunk(cx, cy, _tick);
        }

        private void Move(int x1, int y1, int idx1, int x2, int y2, int idx2)
        {
            _grid.SwapCells(idx1, idx2);
            _grid.touchedTick[idx2] = _tick;
            _grid.WakeChunk(x1, y1, _tick);
            _grid.WakeChunk(x2, y2, _tick);

            // La celda que estaba en (x1,y1) ahora vive en (x2,y2): lo recordamos para
            // que el chequeo de reacciones (tras el switch de ProcessIfNeeded) mire al
            // vecindario correcto, no al de la posición ya abandonada.
            _cellMoved = true;
            _cellFinalX = x2;
            _cellFinalY = y2;
            _cellFinalIdx = idx2;
        }

        // ---------------------------------------------------------------------------------
        // Powder
        // ---------------------------------------------------------------------------------
        private void ProcessPowder(int x, int y, int idx, MaterialDef def)
        {
            if (y == 0) return; // fila 0 es siempre borde de Stone

            int belowIdx = idx - W;
            var belowDef = _universe.Get(_grid.mat[belowIdx]);

            if (belowDef.archetype == MaterialArchetype.Empty || belowDef.archetype == MaterialArchetype.Gas)
            {
                Move(x, y, idx, x, y - 1, belowIdx);
                return;
            }

            var rng = XorShift.FromCell(_tick, x, y, 1);

            if (belowDef.archetype == MaterialArchetype.Liquid && belowDef.density < def.density)
            {
                if (rng.ChancePercent(60))
                {
                    Move(x, y, idx, x, y - 1, belowIdx);
                    return;
                }
            }

            bool leftFirst = rng.NextBool();
            for (int i = 0; i < 2; i++)
            {
                int dx = (i == 0) == leftFirst ? -1 : 1;
                int nx = x + dx, ny = y - 1;
                if (!CellGrid.InBounds(nx, ny)) continue;
                int nidx = CellGrid.Idx(nx, ny);
                var ndef = _universe.Get(_grid.mat[nidx]);
                if (ndef.archetype == MaterialArchetype.Empty || ndef.archetype == MaterialArchetype.Gas)
                {
                    Move(x, y, idx, nx, ny, nidx);
                    return;
                }
                if (ndef.archetype == MaterialArchetype.Liquid && ndef.density < def.density && rng.ChancePercent(60))
                {
                    Move(x, y, idx, nx, ny, nidx);
                    return;
                }
            }

            // Deslizamiento lateral ocasional sobre un líquido si la fluidez es alta.
            if (def.fluidity > 2 && belowDef.archetype == MaterialArchetype.Liquid && rng.ChancePercent(15))
            {
                bool goRight = rng.NextBool();
                int dx = goRight ? 1 : -1;
                int nx = x + dx;
                if (CellGrid.InBounds(nx, y))
                {
                    int nidx = CellGrid.Idx(nx, y);
                    var ndef = _universe.Get(_grid.mat[nidx]);
                    if (ndef.archetype == MaterialArchetype.Empty)
                    {
                        Move(x, y, idx, nx, y, nidx);
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Liquid
        // ---------------------------------------------------------------------------------
        private void ProcessLiquid(int x, int y, int idx, MaterialDef def)
        {
            if (y == 0) return;

            // ---------------------------------------------------------------
            // LO QUE PERSISTE (playtest 25, CONTRATO_PERSISTE.md sección 4.3)
            // -- los DOS únicos procesos nuevos de SimStepper, los dos
            // muestreados 1/8 (mismo patrón que MaybeReact) y los dos ANTES
            // del flujo/gravedad de abajo: si cualquiera transforma la celda,
            // vuelve inmediatamente (la celda ya no es la Liquid que era, y
            // el resto de este método asume `def` todavía válido).
            // ---------------------------------------------------------------
            // (playtest 27, CONTRATO_TALLER_GRANDE mandato 4) LA SEPARACIÓN
            // DEL LIMO YA NO ES FÍSICA DEL MUNDO -- ES UN ACTO DEL CRISOL.
            // Regla 15: se comenta el porqué, no se borra el mecanismo
            // (ProcessLimoSeparacion/PickBaseDelLimo siguen abajo, intactos y
            // sin llamantes).
            //
            // POR QUÉ SE RETIRA. Cesar, jugando el 26: *"cada vez que le tiro
            // limo saco 4 cosas de colores que me aturden... si me salen 4
            // cosas casi de golpe no entendí nada"*. Y tenía razón por partida
            // doble: (a) cada celda sorteaba SU base por separado, así que un
            // charco de limo caliente escupía confeti de cinco colores a la
            // vez, y (b) al ser física del mundo ocurría en CUALQUIER sitio
            // caliente, no en el aparato que el jugador estaba mirando.
            // Desde el playtest 27, una hornada de limo en el crisol produce
            // UNA sola base -- la más alta cuya banda `Universe.ExtraccionRaw`
            // quepa en la temperatura de esa pasada (Game/Crisol.cs) -- y el
            // limo derramado por el suelo, por muy caliente que esté, no se
            // separa solo. La separación pasa a ser algo que TÚ haces.
            //   if (def.id == MaterialId.Limo) { if (ProcessLimoSeparacion(x, y, idx)) return; }
            if (def.id == MaterialId.Water)
            {
                if (ProcessDisolucionAgua(x, y, idx)) return;
            }

            int belowIdx = idx - W;
            var belowDef = _universe.Get(_grid.mat[belowIdx]);

            if (belowDef.archetype == MaterialArchetype.Empty || belowDef.archetype == MaterialArchetype.Gas)
            {
                Move(x, y, idx, x, y - 1, belowIdx);
                return;
            }

            if (belowDef.archetype == MaterialArchetype.Liquid && belowDef.density < def.density)
            {
                // Estratificación por densidad: el líquido más denso siempre se hunde
                // bajo el más ligero (esto es lo que hace que el aceite flote en agua).
                Move(x, y, idx, x, y - 1, belowIdx);
                return;
            }

            var rng = XorShift.FromCell(_tick, x, y, 2);
            bool leftFirst = rng.NextBool();
            for (int i = 0; i < 2; i++)
            {
                int dx = (i == 0) == leftFirst ? -1 : 1;
                int nx = x + dx, ny = y - 1;
                if (!CellGrid.InBounds(nx, ny)) continue;
                int nidx = CellGrid.Idx(nx, ny);
                var ndef = _universe.Get(_grid.mat[nidx]);
                if (ndef.archetype == MaterialArchetype.Empty || ndef.archetype == MaterialArchetype.Gas
                    || (ndef.archetype == MaterialArchetype.Liquid && ndef.density < def.density))
                {
                    Move(x, y, idx, nx, ny, nidx);
                    return;
                }
            }

            // Flujo horizontal: usa 1 bit de memoria en aux para preferir la misma
            // dirección varios ticks seguidos y evitar el "jitter" visual.
            if (def.fluidity > 0)
            {
                bool prefRight = (_grid.aux[idx] & 0x1) != 0;
                int primaryDir = prefRight ? 1 : -1;

                if (TryFlow(x, y, idx, primaryDir, def.fluidity)) return;

                // La dirección preferida está bloqueada: prueba la contraria.
                // TryFlow ya deja grabada la nueva dirección en el aux de la celda movida.
                TryFlow(x, y, idx, -primaryDir, def.fluidity);
            }
        }

        // ---------------------------------------------------------------------------------
        // LO QUE PERSISTE (playtest 25, CONTRATO_PERSISTE.md sección 4.3) -- los dos
        // procesos nuevos. Cero allocs (ninguno asigna memoria; las sales son `const uint`,
        // los XorShift son structs por valor).
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Separación del limo por calor (contrato 4.3): con `temp >= LimoSeparaRaw`,
        /// muestreada 1/8, la celda de Limo se convierte en el POLVO de la base sorteada
        /// por <see cref="XorShift.FromCell"/> con tick FIJO 0 (determinista POR CELDA:
        /// hervir dos veces el mismo sitio da lo mismo, no depende de CUÁNDO se hierve) y
        /// los pesos <see cref="Universe.PesoEnLimo"/>. Devuelve true si transformó la
        /// celda (el llamante debe `return` sin seguir procesándola como Liquid este tick).
        /// </summary>
        /// <remarks>(playtest 27) SIN LLAMANTES a propósito -- ver el bloque comentado en ProcessLiquid: la separación del limo pasó a ser un acto del Crisol (una base por hornada, elegida por temperatura). Se conserva por la regla 15 y porque el gate `LimoSeparaRaw` sigue siendo el umbral que documenta `umbralPersistenciaRaw[Limo]` en Universe.</remarks>
        private bool ProcessLimoSeparacion(int x, int y, int idx)
        {
            if (((x + y + (int)_tick) & 7) != 0) return false; // muestreo 1/8, patrón de MaybeReact.
            if (_grid.temp[idx] < LimoSeparaRaw) return false;

            var rng = XorShift.FromCell(0, x, y, (uint)_universe.Seed ^ SalLimoSeparacion);
            byte destino = PickBaseDelLimo(rng);
            Transform(idx, destino);
            return true;
        }

        /// <summary>Elige la base (Polvo de) según los pesos <see cref="Universe.PesoEnLimo"/> (suman 100, contrato 4.2/4.4).</summary>
        private byte PickBaseDelLimo(XorShift rng)
        {
            int total = 0;
            for (int b = 0; b < MaterialId.BasesCount; b++) total += _universe.PesoEnLimo(b);
            if (total <= 0) return MaterialId.MatDe(0, EstadoMateria.Polvo); // salvaguarda defensiva: el solver de Universe.Create garantiza pesos positivos que suman 100, esto no debería disparar nunca.

            int roll = rng.Next(total);
            int acumulado = 0;
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                acumulado += _universe.PesoEnLimo(b);
                if (roll < acumulado) return MaterialId.MatDe(b, EstadoMateria.Polvo);
            }
            return MaterialId.MatDe(MaterialId.BasesCount - 1, EstadoMateria.Polvo); // defensivo por redondeo, no debería alcanzarse.
        }

        /// <summary>
        /// Disolución (contrato 4.3, solo Water): con un vecino ortogonal Polvo/Calcinado
        /// soluble, muestreada 1/8, 20% por muestreo: el agua -> Solucion de esa base, el
        /// polvo -> Empty (1+1=1, masa simple y legible). Devuelve true si disolvió (el
        /// llamante debe `return` sin seguir procesando esta celda como Liquid este tick).
        /// </summary>
        private bool ProcessDisolucionAgua(int x, int y, int idx)
        {
            if (((x + y + (int)_tick) & 7) != 0) return false; // muestreo 1/8, patrón de MaybeReact.

            // Orden fijo Izq/Der/Abajo/Arriba, coste acotado: el primer vecino soluble que
            // aparece gana (no hace falta elegir "el mejor", solo uno cualquiera).
            if (!TryFindVecinoSoluble(x - 1, y, out int nIdx, out byte baseIdx)
                && !TryFindVecinoSoluble(x + 1, y, out nIdx, out baseIdx)
                && !TryFindVecinoSoluble(x, y - 1, out nIdx, out baseIdx)
                && !TryFindVecinoSoluble(x, y + 1, out nIdx, out baseIdx))
            {
                return false;
            }

            var rng = XorShift.FromCell(_tick, x, y, SalDisolucion);
            if (!rng.ChancePercent(20)) return false;

            Transform(idx, MaterialId.MatDe(baseIdx, EstadoMateria.Solucion));
            Transform(nIdx, MaterialId.Empty);
            return true;
        }

        /// <summary>Vecino en (nx,ny) es un Polvo/Calcinado soluble (Universe.SolubleEnAgua ya restringe a esos dos estados, contrato §3) -- si sí, devuelve su índice y la base.</summary>
        private bool TryFindVecinoSoluble(int nx, int ny, out int nIdx, out byte baseIdx)
        {
            nIdx = -1;
            baseIdx = 0;
            if (!CellGrid.InBounds(nx, ny)) return false;
            int idx = CellGrid.Idx(nx, ny);
            byte m = _grid.mat[idx];
            if (m == MaterialId.Empty) return false;
            if (!_universe.SolubleEnAgua(m)) return false;
            nIdx = idx;
            baseIdx = (byte)MaterialId.BaseDe(m);
            return true;
        }

        /// <summary>Busca la primera celda vacía en línea recta (hasta `steps` celdas) y se mueve ahí.</summary>
        private bool TryFlow(int x, int y, int idx, int dir, int steps)
        {
            for (int s = 1; s <= steps; s++)
            {
                int nx = x + dir * s;
                if (!CellGrid.InBounds(nx, y)) return false;
                int nidx = CellGrid.Idx(nx, y);
                byte nm = _grid.mat[nidx];
                if (nm == MaterialId.Empty)
                {
                    // Deja la preferencia de dirección grabada antes de moverse (viaja con el swap).
                    _grid.aux[idx] = (byte)((_grid.aux[idx] & 0xFE) | (dir > 0 ? 1 : 0));
                    Move(x, y, idx, nx, y, nidx);
                    return true;
                }
                var ndef = _universe.Get(nm);
                if (ndef.archetype != MaterialArchetype.Gas)
                {
                    // Bloqueado por algo sólido/líquido/otro material: no se puede seguir en esta dirección.
                    return false;
                }
                // Es gas: lo consideramos "atravesable" para el barrido y seguimos buscando un hueco vacío más allá.
            }
            return false;
        }

        // ---------------------------------------------------------------------------------
        // Sólidos con gravedad (playtest 29) -- ver el case StaticSolid de ProcessIfNeeded.
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// ¿La celda (x,y) tiene APOYO? Directo: lo de abajo no es vacío.
        /// Por cohesión: recorriendo la fila hacia un lado, a través de
        /// celdas SÓLIDAS continuas (StaticSolid o Powder asentado cuentan
        /// como materia portante; un hueco corta la viga), hay a
        /// ≤ cohesionCeldas una celda con apoyo directo. La piedra es apoyo
        /// y también portante: una viga empotrada en el muro se sostiene.
        /// </summary>
        private bool SolidoTieneApoyo(int x, int y, byte cohesion)
        {
            if (y <= 0) return true; // el fondo del mundo sostiene.
            if (_grid.mat[CellGrid.Idx(x, y - 1)] != MaterialId.Empty) return true;
            if (cohesion == 0) return false;

            for (int dir = -1; dir <= 1; dir += 2)
            {
                for (int d = 1; d <= cohesion; d++)
                {
                    int nx = x + dir * d;
                    if (!CellGrid.InBounds(nx, y)) break;
                    byte m = _grid.mat[CellGrid.Idx(nx, y)];
                    if (m == MaterialId.Empty) break; // la viga se corta: no hay materia que transmita el apoyo.
                    var a = _universe.Get(m).archetype;
                    if (a != MaterialArchetype.StaticSolid && a != MaterialArchetype.Powder) break; // líquido/gas no transmiten carga.
                    if (_grid.mat[CellGrid.Idx(nx, y - 1)] != MaterialId.Empty) return true; // ese vecino sí está apoyado: ménsula válida.
                }
            }
            return false;
        }

        /// <summary>Caída recta de un sólido sin apoyo: una celda por tick, solo a hueco VACÍO (los líquidos sostienen -- el hielo sigue flotando en el agua, como siempre). Sin deslizamiento lateral: un sólido no es un polvo.</summary>
        private void ProcessSolidoCohesion(int x, int y, int idx, MaterialDef def)
        {
            if (SolidoTieneApoyo(x, y, def.cohesionCeldas)) return;

            int belowIdx = CellGrid.Idx(x, y - 1);
            _grid.SwapCells(idx, belowIdx);
            _grid.WakeChunk(x, y, _tick);
            _grid.WakeChunk(x, y - 1, _tick);
            _cellMoved = true;
            _cellFinalX = x;
            _cellFinalY = y - 1;
            _cellFinalIdx = belowIdx;
        }

        // ---------------------------------------------------------------------------------
        // Gas
        // ---------------------------------------------------------------------------------
        private void ProcessGas(int x, int y, int idx, MaterialDef def)
        {
            if (def.gasLifetime > 0)
            {
                byte life = _grid.aux[idx];
                if (life > 0) life--;
                _grid.aux[idx] = life;
                if (life == 0)
                {
                    byte t = _grid.temp[idx];
                    if (def.condensesAt != short.MinValue && t <= def.condensesAt)
                        Transform(idx, def.condensesInto);
                    else
                        Transform(idx, MaterialId.Empty);
                    return;
                }
            }

            int aboveIdx = -1;
            if (y < H - 1) aboveIdx = idx + W;

            if (aboveIdx >= 0)
            {
                byte am = _grid.mat[aboveIdx];
                if (am == MaterialId.Empty)
                {
                    Move(x, y, idx, x, y + 1, aboveIdx);
                    return;
                }
                var adef = _universe.Get(am);
                if (adef.archetype == MaterialArchetype.Gas && adef.density > def.density)
                {
                    Move(x, y, idx, x, y + 1, aboveIdx);
                    return;
                }
            }

            var rng = XorShift.FromCell(_tick, x, y, 5);
            bool leftFirst = rng.NextBool();
            for (int i = 0; i < 2; i++)
            {
                int dx = (i == 0) == leftFirst ? -1 : 1;
                int nx = x + dx, ny = y + 1;
                if (!CellGrid.InBounds(nx, ny)) continue;
                int nidx = CellGrid.Idx(nx, ny);
                byte nm = _grid.mat[nidx];
                if (nm == MaterialId.Empty)
                {
                    Move(x, y, idx, nx, ny, nidx);
                    return;
                }
            }

            // Deambular lateral aleatorio para dar sensación de corriente/turbulencia.
            if (rng.ChancePercent(35))
            {
                int dx = rng.NextBool() ? 1 : -1;
                int nx = x + dx;
                if (CellGrid.InBounds(nx, y))
                {
                    int nidx = CellGrid.Idx(nx, y);
                    if (_grid.mat[nidx] == MaterialId.Empty)
                    {
                        Move(x, y, idx, nx, y, nidx);
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Fire
        // ---------------------------------------------------------------------------------
        // (fix playtest 9) Vida corta pero VISIBLE para fuego recién creado sin combustible
        // (pintado con F3, o nacido de una reacción que no sembró aux): ~16 ticks (0.53 s)
        // ±3, deliberadamente mucho más corta que la vida de un fuego alimentado
        // (Universe.fireLifetime ~80 ticks, sembrada por Transform() al encender por
        // contacto/autoignición) -- fuego sin combustible SIGUE debiendo apagarse pronto,
        // esa regla ya era correcta, solo que "pronto" no puede significar "instantáneo".
        private const byte FreeFireSeedLife = 16;
        private const int FreeFireJitter = 3;
        // Por debajo de esta vida, decae a MEDIA velocidad (desvanecido, ver ProcessFire).
        private const byte FadeTailLife = 6;

        private void ProcessFire(int x, int y, int idx, MaterialDef def)
        {
            // El fuego solo se extingue si está realmente sumergido: agua ENCIMA,
            // o rodeado por 2+ celdas de agua ortogonales. Una sola celda de agua
            // al lado/debajo NO apaga la llama — el aceite ardiendo flota sobre el
            // agua y debe poder arder (ese es exactamente el fenómeno interesante).
            int waterNeighbors = CountOrthogonalNeighbors(x, y, MaterialId.Water);
            bool waterAbove = y < H - 1 && _grid.mat[idx + W] == MaterialId.Water;
            if (waterAbove || waterNeighbors >= 2)
            {
                Transform(idx, MaterialId.Steam);
                return;
            }

            byte life = _grid.aux[idx];

            // (fix playtest 9) Una celda de Fuego con aux==0 en la ENTRADA de este método
            // es SIEMPRE recién creada: el camino de expiración de más abajo transforma la
            // celda el MISMO tick en que life llega a 0 (return inmediato), así que un
            // Fuego con aux==0 jamás sobrevive de un tick al siguiente por el camino normal.
            // Antes esto se interpretaba como "vida agotada" y la celda se convertía a
            // Humo/Ceniza en el primerísimo tick procesado -- el jugador pintaba fuego en
            // aire (F3) y lo veía gris al instante ("el fuego en el aire sigue siendo
            // gris"): en realidad nunca llegaba a leerse como llama.
            if (life == 0)
            {
                var seedRng = XorShift.FromCell(_tick, x, y, 91);
                life = (byte)(FreeFireSeedLife + seedRng.Next(2 * FreeFireJitter + 1) - FreeFireJitter);
            }

            // La llama con combustible al lado se mantiene viva (fix playtest anterior):
            // sin esto el fuego moría a humo en ~1.5 s y "no parecía fuego".
            if (life < 30 && HasFlammableOrthogonalNeighbor(x, y)) life = 30;

            // (fix playtest 9) DESVANECIDO: en vez de restar 1 de vida cada tick hasta el
            // último instante (llama roja a tope -> humo gris de golpe: un salto visual
            // duro), por debajo de FadeTailLife decae a MEDIA velocidad -- un tick sí, un
            // tick no, determinista por paridad de (x+y+tick), sin RNG -- así se dobla el
            // tiempo pasado en el rojo mortecino final antes de convertirse en Humo/Ceniza:
            // se LEE como un apagado gradual, no como un corte. No cambia la paleta de la
            // llama (SimRenderer.ComputeCellColor sigue igual): solo alarga el tramo bajo.
            bool halfRateFade = life <= FadeTailLife && (((x + y + (int)_tick) & 1) != 0);
            if (life > 0 && !halfRateFade) life--;
            _grid.aux[idx] = life;
            if (life == 0)
            {
                var expRng = XorShift.FromCell(_tick, x, y, 9);
                bool belowSolid = y > 0 && IsSupportive(_grid.mat[idx - W]);
                if (belowSolid && expRng.ChancePercent(25))
                    Transform(idx, MaterialId.Ash);
                else
                    Transform(idx, MaterialId.Smoke);
                return;
            }

            // El fuego calienta fuertemente su propia celda (fuente de calor para la difusión).
            _grid.temp[idx] = 255;
            InjectHeat(x, y, 40);

            // Intenta encender vecinos inflamables (cada vecino tira su propio dado,
            // sembrado por sus propias coordenadas, para no repetir el mismo resultado 4 veces).
            TryIgnite(x - 1, y);
            TryIgnite(x + 1, y);
            TryIgnite(x, y - 1);
            TryIgnite(x, y + 1);

            // El fuego tiende a subir levemente, pero SOLO si no está tocando
            // combustible: una llama sobre aceite se queda pegada a su combustible
            // (arde en el sitio) en lugar de elevarse y morir en el aire.
            if (y < H - 1 && !HasFlammableOrthogonalNeighbor(x, y))
            {
                int aboveIdx = idx + W;
                var riseRng = XorShift.FromCell(_tick, x, y, 13);
                if (_grid.mat[aboveIdx] == MaterialId.Empty && riseRng.ChancePercent(35))
                {
                    Move(x, y, idx, x, y + 1, aboveIdx);
                }
            }
        }

        private int CountOrthogonalNeighbors(int x, int y, byte materialId)
        {
            int n = 0;
            if (CellGrid.InBounds(x - 1, y) && _grid.mat[CellGrid.Idx(x - 1, y)] == materialId) n++;
            if (CellGrid.InBounds(x + 1, y) && _grid.mat[CellGrid.Idx(x + 1, y)] == materialId) n++;
            if (CellGrid.InBounds(x, y - 1) && _grid.mat[CellGrid.Idx(x, y - 1)] == materialId) n++;
            if (CellGrid.InBounds(x, y + 1) && _grid.mat[CellGrid.Idx(x, y + 1)] == materialId) n++;
            return n;
        }

        private bool HasFlammableOrthogonalNeighbor(int x, int y)
        {
            if (CellGrid.InBounds(x - 1, y) && _universe.Get(_grid.mat[CellGrid.Idx(x - 1, y)]).flammable) return true;
            if (CellGrid.InBounds(x + 1, y) && _universe.Get(_grid.mat[CellGrid.Idx(x + 1, y)]).flammable) return true;
            if (CellGrid.InBounds(x, y - 1) && _universe.Get(_grid.mat[CellGrid.Idx(x, y - 1)]).flammable) return true;
            if (CellGrid.InBounds(x, y + 1) && _universe.Get(_grid.mat[CellGrid.Idx(x, y + 1)]).flammable) return true;
            return false;
        }

        private static bool IsSupportive(byte matId)
        {
            return matId != MaterialId.Empty;
        }

        private bool HasOrthogonalNeighbor(int x, int y, byte materialId)
        {
            if (CellGrid.InBounds(x - 1, y) && _grid.mat[CellGrid.Idx(x - 1, y)] == materialId) return true;
            if (CellGrid.InBounds(x + 1, y) && _grid.mat[CellGrid.Idx(x + 1, y)] == materialId) return true;
            if (CellGrid.InBounds(x, y - 1) && _grid.mat[CellGrid.Idx(x, y - 1)] == materialId) return true;
            if (CellGrid.InBounds(x, y + 1) && _grid.mat[CellGrid.Idx(x, y + 1)] == materialId) return true;
            return false;
        }

        private void TryIgnite(int nx, int ny)
        {
            if (!CellGrid.InBounds(nx, ny)) return;
            int nidx = CellGrid.Idx(nx, ny);
            byte nm = _grid.mat[nidx];
            if (nm == MaterialId.Fire || nm == MaterialId.Empty) return;
            var ndef = _universe.Get(nm);
            if (!ndef.flammable) return;

            bool hotEnough = ndef.ignitionTemp != short.MaxValue && _grid.temp[nidx] > ndef.ignitionTemp;
            var rng = XorShift.FromCell(_tick, nx, ny, 17);
            // (fix playtest 9) Antes 50%/tick: un charco de aceite conectado se prendía
            // ENTERO en 1-2 ticks (~0.05 s), un fogonazo instantáneo e imposible de observar
            // o de usar como proceso ("la reacción es muy rápida"). Con 12%/tick el frente de
            // llama avanza a razón de ~1 celda cada 8 ticks (~0.27 s): quemar aceite pasa a
            // ser un proceso que se ve avanzar, no un flash. (hotEnough sigue siendo
            // instantáneo a propósito: un vecino YA por encima de su ignitionTemp -- p.ej.
            // aceite recién autoencendido por la Placa ígnea, ver ApplyPhase -- enciende de
            // verdad al contacto, eso es autoignición real, no azar).
            if (hotEnough || rng.ChancePercent(12))
            {
                PushEvent(SimEventType.Ignite, nm, nx, ny);
                Transform(nidx, MaterialId.Fire);
            }
        }

        // ---------------------------------------------------------------------------------
        // Organic (Vivium)
        // ---------------------------------------------------------------------------------
        private const byte SettledFlag = 0x80;
        // (playtest 12, ampliado en el 19 -- persistencia/bifurcación de
        // GrowthTick, ya no solo el modo Enredadera) Bits de aux libres para
        // Organic: 0x80=SettledFlag, 0x40=OrganicDormantAux (CellGrid), así
        // que 0x01/0x02/0x04 quedan libres sin colisionar. Quedan 0x08/0x10/
        // 0x20 sin usar todavía (bits libres para lo que venga después).
        private const byte CameFromKnownFlag = 0x04; // "esta célula recuerda de qué dirección vino" (sembradas por el jugador no lo tienen: aux nace en 0).
        private const byte CameFromDirMask = 0x03;   // índice (0..3) en DirX/DirY de la dirección de la que vino.
        private static readonly int[] DirX = { -1, 1, 0, 0 };
        private static readonly int[] DirY = { 0, 0, -1, 1 };

        private void ProcessOrganic(int x, int y, int idx, MaterialDef def)
        {
            bool settled = (_grid.aux[idx] & SettledFlag) != 0;

            if (!settled)
            {
                if (y > 0)
                {
                    int belowIdx = idx - W;
                    var belowDef = _universe.Get(_grid.mat[belowIdx]);
                    if (belowDef.archetype == MaterialArchetype.Empty || belowDef.archetype == MaterialArchetype.Gas)
                    {
                        // Cae recto (sin difundirse en diagonal: es "pegajoso", no se abanica como la arena).
                        Move(x, y, idx, x, y - 1, belowIdx);
                        return;
                    }
                }

                _grid.aux[idx] |= SettledFlag;
                _grid.WakeChunk(x, y, _tick);
            }

            // Nota: a diferencia de otros arquetipos, una célula asentada de Vivium
            // sigue necesitando ser revisada TODOS los ticks (mientras su chunk esté
            // despierto) para poder crecer -- por eso GrowthTick se llama tanto la
            // primera vez que se asienta como en cada tick posterior.
            GrowthTick(x, y, idx);
        }

        /// <summary>
        /// Crecimiento de Vivium (M3, arco de domesticación; REESCRITO en el
        /// playtest 19 para darle FORMA al crecimiento -- Cesar: "lo que no
        /// vi por más que intenté es que algo crezca con formas que vengan
        /// de algoritmos, fractales qué sé yo... solo vi diferencias de
        /// viscosidad y propagación"). Una célula asentada con un vecino
        /// ortogonal de Nutrient Y su propia temperatura dentro de
        /// [Universe.VivGrowMinRaw, VivGrowMaxRaw] consume ESE Nutrient (-&gt;
        /// Empty) y, con Universe.VivGrowChancePct de probabilidad, crea una
        /// nueva célula de Vivium en su lugar (si falla, el Nutrient se
        /// pierde igualmente: la célula "gastó" el intento).
        ///
        /// EL CAMBIO QUE ROMPE LA MANCHA: antes CUALQUIER célula asentada con
        /// Nutrient al lado podía engendrar, así que un núcleo con varios
        /// vecinos de Nutrient rellenaba su entorno entero en unos pocos
        /// ticks -- un borrón redondo, sin silueta (el campo morfológico,
        /// playtest 12, le daba TEXTURA a ese borrón, nunca forma). Ahora
        /// SOLO LAS PUNTAS engendran: CountOrganicNeighbors cuenta los
        /// vecinos ORTOGONALES de Vivium de esta MISMA célula (la candidata
        /// a engendrar, no el hueco vecino); por encima de
        /// Universe.HabitoTolerarVecinosPunta la célula es TALLO/interior y
        /// ya cumplió su función estructural -- no vuelve a intentar crecer
        /// (no se marca de ninguna forma ni muere: simplemente deja de
        /// competir por el Nutrient que le quede alrededor, que solo un
        /// vecino que SÍ siga siendo punta podrá alcanzar). Con esto una
        /// colonia crece por sus EXTREMOS y se ramifica, en vez de engordar
        /// entera a la vez -- ver el modelo Python del informe de la ronda
        /// para las siluetas resultantes en varias semillas.
        ///
        /// Encima del gate, tres afinamientos leídos de Universe (el
        /// "hábito de crecimiento" de esta semilla, junto a
        /// AfinidadDelUniverso):
        ///   - PERSISTENCIA (HabitoPersistenciaPct): una punta con dirección
        ///     conocida (aux, CameFromDirMask/CameFromKnownFlag) tiende a
        ///     seguir en línea recta en vez de recalcular candidato cada vez
        ///     -- ramas rectas en vez de zigzagueantes.
        ///   - BIFURCACIÓN (HabitoBifurcarPct): de vez en cuando, una punta
        ///     con dirección conocida IGNORA esa dirección a propósito y
        ///     fuerza un candidato distinto -- como la célula sigue teniendo
        ///     pocos vecinos, puede volver a engendrar en una TERCERA
        ///     dirección otro tick, y las dos crías ya se leen como
        ///     horquilla. Es EL parámetro que más cambia la silueta.
        ///   - SESGO VERTICAL (HabitoSesgoVerticalPct): antes del orden
        ///     isotrópico de siempre, tantea la vertical preferida de este
        ///     universo (arriba = "planta que trepa a la luz", abajo = "moho
        ///     que se entierra hacia el nutriente"; 0 = isótropo). Es un
        ///     rasgo del UNIVERSO, no de la textura que le tocó a este
        ///     Vivium -- por eso se aplica igual en las tres familias
        ///     visuales de abajo.
        ///
        /// El modo heredado del playtest 12 (Enredadera/Mata/Disperso, según
        /// la familia visual de Vivium) se conserva como matiz SECUNDARIO:
        /// Enredadera REFUERZA la persistencia del universo, Mata la
        /// DEBILITA, y Disperso sigue usando su propia heurística de
        /// candidato (el vecino MENOS rodeado) en vez de persistencia. Ya no
        /// decide él solo si hay dirección continuada o no, como antes de
        /// esta ronda -- el gate de puntas y el hábito por semilla son
        /// universales para las tres familias.
        ///
        /// COMO MUCHO un Nutrient por célula y por tick (evita un relleno
        /// instantáneo; el throttle de abajo lo ralentiza más aún para que
        /// se LEA como un coral creciendo, no como un flood-fill). Fuera de
        /// banda: la célula queda "dormida" (bit CellGrid.OrganicDormantAux,
        /// leído por SimRenderer para una ligera desaturación) -- no crece,
        /// pero tampoco muere (solo se quema por encima de ~120°C vía
        /// ApplyPhase/boilsAt, que reutiliza el mecanismo genérico de
        /// transición de fase).
        ///
        /// COSTE: el gate añade una llamada a CountOrganicNeighbors (4
        /// lecturas de byte) por célula asentada elegible -- antes solo la
        /// pagaba el modo Disperso, y solo por candidato válido (hasta 4
        /// veces). En el peor caso teórico (las 221.184 celdas del mundo
        /// llenas de Vivium y despiertas a la vez) son ~55.000 células
        /// elegibles por tick (1/4 por el throttle) con una lectura extra de
        /// 4 vecinos cada una: ~220.000 lecturas de byte adicionales por
        /// tick, frente al presupuesto de 33 ms a 30 Hz -- no medible con un
        /// profiler real, y en la práctica un cultivo ocupa una fracción
        /// pequeña del mundo, con los chunks dormidos filtrando el resto
        /// antes de llegar aquí.
        ///
        /// TASA DE CULTIVO: el gate reduce cuántas células compiten por
        /// Nutrient a la vez (las de tallo dejan de intentarlo), así que a
        /// igualdad de VivGrowChancePct cultivar 120 células tardaría más
        /// que con la mancha vieja -- por eso Universe.VivGrowChancePct sube
        /// de 60 a 75 esta ronda (ver ese campo para las cifras medidas).
        /// </summary>
        private void GrowthTick(int x, int y, int idx)
        {
            byte t = _grid.temp[idx];
            bool inBand = t >= _universe.VivGrowMinRaw && t <= _universe.VivGrowMaxRaw;

            if (!inBand)
            {
                _grid.aux[idx] |= CellGrid.OrganicDormantAux;
                return;
            }
            if ((_grid.aux[idx] & CellGrid.OrganicDormantAux) != 0)
            {
                _grid.aux[idx] &= unchecked((byte)~CellGrid.OrganicDormantAux);
            }

            // Throttle de ritmo visual: cada célula solo intenta crecer 1 de cada 4 ticks.
            if (((x * 13 + y * 7 + (int)_tick) & 3) != 0) return;

            // (playtest 19) SOLO LAS PUNTAS ENGENDRAN -- ver el docblock de
            // arriba. Vecinos ORTOGONALES de Vivium de esta MISMA célula (no
            // del hueco al que aspira a crecer): por encima de la tolerancia
            // de este universo, es tallo/interior y no compite más por
            // Nutrient este tick (ni ningún otro, salvo que un vecino suyo
            // muera y le baje el conteo -- Vivium asentado no muere solo,
            // así que en la práctica es definitivo).
            if (CountOrganicNeighbors(x, y) > _universe.HabitoTolerarVecinosPunta) return;

            var rng = XorShift.FromCell(_tick, x, y, 88);
            int start = rng.Next(4); // SIEMPRE se consume, aunque el camino elegido no lo use -- mismo criterio que siempre: no desalinear las tiradas siguientes entre células con y sin dirección conocida.

            bool tieneDirConocida = (_grid.aux[idx] & CameFromKnownFlag) != 0;
            int dirConocida = _grid.aux[idx] & CameFromDirMask;

            // BIFURCACIÓN (playtest 19): una punta con dirección conocida,
            // de vez en cuando, descarta esa dirección A PROPÓSITO -- ver
            // docblock. Tirada propia, independiente de `start`.
            bool bifurca = tieneDirConocida && rng.ChancePercent(_universe.HabitoBifurcarPct);
            int dirExcluida = bifurca ? dirConocida : -1;

            var vivDef = _universe.Get(MaterialId.Vivium);
            var mode = VivGrowthModeFor(vivDef.patron);
            // La familia visual de este Vivium AFINA la persistencia base
            // del universo, no la sustituye (ver docblock): Enredadera
            // (Dendritas/Laberinto) la refuerza, Mata (Celdas/Pulso) la
            // debilita; Disperso no la usa -- prioriza dejar huecos (scoring
            // más abajo).
            int persistenciaPct = _universe.HabitoPersistenciaPct;
            if (mode == VivGrowthMode.Enredadera) persistenciaPct = System.Math.Min(100, persistenciaPct + 15);
            else if (mode == VivGrowthMode.Mata) persistenciaPct = System.Math.Max(0, persistenciaPct - 15);

            int dir = -1;

            if (mode != VivGrowthMode.Disperso && tieneDirConocida && !bifurca && IsNutrientDir(x, y, dirConocida)
                && rng.ChancePercent(persistenciaPct))
            {
                dir = dirConocida; // sigue recto: avanza, no retrocede hacia el padre.
            }

            if (dir < 0 && mode == VivGrowthMode.Disperso)
            {
                // Prefiere el candidato con MENOS vecinos de Vivium ya
                // alrededor (lo opuesto al "compacto" del Cristal, ver
                // TryCrystalGrowth): así el crecimiento deja huecos en vez
                // de rellenar el hueco más rodeado.
                int bestScore = int.MaxValue;
                for (int d = 0; d < 4; d++)
                {
                    if (d == dirExcluida || !IsNutrientDir(x, y, d)) continue;
                    int nx0 = x + DirX[d], ny0 = y + DirY[d];
                    int score = CountOrganicNeighbors(nx0, ny0);
                    if (score < bestScore) { bestScore = score; dir = d; }
                }
            }

            if (dir < 0 && _universe.HabitoSesgoVerticalPct != 0)
            {
                // SESGO VERTICAL: tantea la vertical preferida de este
                // universo antes que el orden isotrópico de abajo. 3=arriba,
                // 2=abajo (ver DirX/DirY arriba; misma convención de ejes
                // que la gravedad de Powder/Liquid: idx-W, y-1, es "abajo").
                int preferida = _universe.HabitoSesgoVerticalPct > 0 ? 3 : 2;
                if (preferida != dirExcluida && IsNutrientDir(x, y, preferida)
                    && rng.ChancePercent(System.Math.Abs((int)_universe.HabitoSesgoVerticalPct)))
                {
                    dir = preferida;
                }
            }

            if (dir < 0)
            {
                // Isotrópico -- comportamiento original, orden aleatorio
                // sembrado por celda, saltando la dirección que la
                // bifurcación acaba de descartar.
                for (int i = 0; i < 4; i++)
                {
                    int d = (start + i) & 3;
                    if (d == dirExcluida) continue;
                    if (IsNutrientDir(x, y, d)) { dir = d; break; }
                }
            }

            if (dir < 0 && bifurca && IsNutrientDir(x, y, dirConocida))
            {
                // La bifurcación descartó el ÚNICO candidato disponible este
                // tick: mejor engendrar en línea recta que no engendrar nada
                // (improvisado a propósito, ver el informe de la ronda).
                dir = dirConocida;
            }

            if (dir < 0) return; // ningún Nutrient disponible este tick para esta punta.

            int nx = x + DirX[dir], ny = y + DirY[dir];
            int nidx = CellGrid.Idx(nx, ny);

            bool grows = rng.ChancePercent(_universe.VivGrowChancePct);
            Transform(nidx, MaterialId.Empty);
            if (grows)
            {
                Transform(nidx, MaterialId.Vivium);
                // Graba de qué dirección vino (persistencia/bifurcación de SU
                // cría, si la tiene). Transform ya puso aux[nidx]=0 arriba,
                // así que esto no pisa nada.
                _grid.aux[nidx] = (byte)((dir & CameFromDirMask) | CameFromKnownFlag);
                PushEvent(SimEventType.Grow, MaterialId.Vivium, nx, ny);
                // (playtest 18) La ley de crecimiento del Vivium también es una
                // LEY: empuja su propio evento Ley con LeyCrecimientoIndice,
                // sujeto al MISMO limitador de ritmo que las leyes de contacto
                // (CONTRATO_FASE3.md sección 7) -- un cultivo de Vivium creciendo
                // rápido no debe poder saturar el anillo más que un ácido disolviendo.
                PushLeyEvent(_universe.LeyCrecimientoIndice, MaterialId.Vivium, nx, ny);
            }
            // un Nutrient por célula y por tick (igual que antes).
        }

        /// <summary>
        /// Modo derivado de la familia visual de Vivium (playtest 12,
        /// morfología de crecimiento). Desde el playtest 19 es un matiz
        /// SECUNDARIO sobre el hábito de crecimiento de la semilla
        /// (Universe.HabitoTolerarVecinosPunta y hermanos, que se aplican
        /// igual en los tres modos): Enredadera solo refuerza la
        /// persistencia base, Mata la debilita, y Disperso sustituye la
        /// persistencia por su propia heurística de candidato. Ver
        /// GrowthTick para el porqué completo.
        /// </summary>
        private enum VivGrowthMode { Mata, Enredadera, Disperso }

        /// <summary>
        /// Dendritas/Laberinto (rasgo lineal o ramificado) -> Enredadera
        /// (refuerza la persistencia). Celdas/Pulso (rasgo compacto y
        /// coherente, panal o respiración de bloque) -> Mata (la debilita).
        /// Manchas/Motas (rasgo disperso por definición) -> Disperso (deja
        /// huecos, elige el vecino menos rodeado). Cubre las 6 familias
        /// plausibles para Organic (Universe.FamiliasPlausibles), 2 cada una.
        /// </summary>
        private static VivGrowthMode VivGrowthModeFor(PatronMorfologico patron)
        {
            switch (patron)
            {
                case PatronMorfologico.Dendritas:
                case PatronMorfologico.Laberinto:
                    return VivGrowthMode.Enredadera;
                case PatronMorfologico.Celdas:
                case PatronMorfologico.Pulso:
                    return VivGrowthMode.Mata;
                default: // Manchas, Motas (y cualquier caso no plausible que llegara aquí de todos modos).
                    return VivGrowthMode.Disperso;
            }
        }

        private bool IsNutrientDir(int x, int y, int dir)
        {
            int nx = x + DirX[dir], ny = y + DirY[dir];
            return CellGrid.InBounds(nx, ny) && _grid.mat[CellGrid.Idx(nx, ny)] == MaterialId.Nutrient;
        }

        private int CountOrganicNeighbors(int x, int y)
        {
            int c = 0;
            if (CellGrid.InBounds(x - 1, y) && _grid.mat[CellGrid.Idx(x - 1, y)] == MaterialId.Vivium) c++;
            if (CellGrid.InBounds(x + 1, y) && _grid.mat[CellGrid.Idx(x + 1, y)] == MaterialId.Vivium) c++;
            if (CellGrid.InBounds(x, y - 1) && _grid.mat[CellGrid.Idx(x, y - 1)] == MaterialId.Vivium) c++;
            if (CellGrid.InBounds(x, y + 1) && _grid.mat[CellGrid.Idx(x, y + 1)] == MaterialId.Vivium) c++;
            return c;
        }

        // ---------------------------------------------------------------------------------
        // Temperatura
        // ---------------------------------------------------------------------------------
        private void InjectHeat(int x, int y, int amount)
        {
            AddTemp(x - 1, y, amount);
            AddTemp(x + 1, y, amount);
            AddTemp(x, y - 1, amount);
            AddTemp(x, y + 1, amount);
        }

        private void InjectCold(int x, int y, int amount)
        {
            AddTemp(x - 1, y, -amount);
            AddTemp(x + 1, y, -amount);
            AddTemp(x, y - 1, -amount);
            AddTemp(x, y + 1, -amount);
        }

        private void AddTemp(int x, int y, int amount)
        {
            if (!CellGrid.InBounds(x, y)) return;
            int idx = CellGrid.Idx(x, y);
            int v = _grid.temp[idx] + amount;
            if (v < 0) v = 0; else if (v > 255) v = 255;
            _grid.temp[idx] = (byte)v;
        }

        /// <summary>
        /// Difusión de temperatura barata: cada tick solo se procesa 1/8 de las
        /// celdas (offset = tick % 8), promediando con los 4 vecinos ortogonales
        /// en aritmética entera. En 8 ticks (≈0.27s a 30Hz) toda la grilla se
        /// habrá actualizado una vez.
        ///
        /// (fix playtest 9 -- "el frío se propaga sin fin"). Dos bugs de aritmética
        /// raw independientes hacían que el campo de temperatura derivara sin límite
        /// en vez de estabilizarse en un charco local alrededor de la fuente:
        ///
        ///  1) TIRÓN HACIA AMBIENTE COLAPSADO A 1/8 DE LA GRILLA, PARA SIEMPRE. La
        ///     condición vieja era `(_tick &amp; 7u) == 0u`, comprobada UNA VEZ por
        ///     iteración sobre las celdas con `i % 8 == offset` (offset = tick % 8).
        ///     Pero offset == tick % 8 SIEMPRE, así que "tick % 8 == 0" solo puede
        ///     ser cierto en la misma iteración en la que offset == 0, es decir,
        ///     SOLO para las celdas con i % 8 == 0. Las otras 7/8 partes de la
        ///     grilla no recibían el tirón NUNCA (ni una sola vez en toda la
        ///     partida): un trinquete de un solo sentido -- exactamente el mismo
        ///     patrón que el bug del hielo del playtest 1, pero por aritmética en
        ///     vez de por chunks dormidos. El calor/frío inyectado por Placa/Piedra/
        ///     Fuego se difundía entre vecinos (que sí corre en TODA la grilla, sin
        ///     depender de qué chunk está despierto) pero nunca se relajaba hacia
        ///     ambiente fuera de ese 1/8 fijo, así que con suficientes ticks
        ///     ("con el tiempo") terminaba filtrándose a cualquier rincón del taller
        ///     sin que nada lo devolviera a los 20°C. Arreglo: la condición ahora se
        ///     basa en cuántas veces se ha difundido CADA celda (_tick >> 3, que para
        ///     una celda con offset fijo o avanza exactamente 1 por cada una de sus
        ///     difusiones), no en el offset mismo -- así el tirón llega a TODA la
        ///     grilla, una vez cada ~32 ticks (4 difusiones), como decía el comentario
        ///     original y como se diseñó tras el playtest 1.
        ///  2) REDONDEO SESGADO HACIA EL FRÍO. `diff >> 2` es desplazamiento
        ///     aritmético: para enteros NEGATIVOS en C# equivale a floor(diff/4), no
        ///     a truncar hacia cero. Ejemplo real: diff=+5 -> +5&gt;&gt;2=+1, pero
        ///     diff=-5 -> -5&gt;&gt;2=-2 (¡el doble de magnitud!). El fallback
        ///     "step==0 -> ±1" igualaba el caso |diff|&lt;4, pero para |diff|>=5 no
        ///     múltiplo de 4 quedaba un sesgo real y permanente: una celda más
        ///     caliente que sus vecinos se enfriaba más rápido de lo que una celda
        ///     más fría que sus vecinos se calentaba. Aplicado 30 veces por segundo,
        ///     eso es una deriva neta hacia el frío garantizada en TODA la grilla,
        ///     con o sin piedra gélida encendida. Arreglo: `diff / 4` (división
        ///     entera con truncamiento hacia cero, simétrica para signo positivo y
        ///     negativo) en vez de `diff >> 2`.
        ///
        /// (playtest 15 -- EL CLIMA POR CELDA, mundo a 768x288. AVISO AL LECTOR:
        /// los tres puntos (a)/(b)/(c) de abajo se escribieron cuando el clima
        /// era POR ZONA, con degradados; en el playtest 17 pasó a ser uniforme
        /// -- ver el addendum al final de este bloque. Se conservan enteros
        /// porque el análisis de estabilidad que hacen sigue siendo el que hay
        /// que rehacer el día que el clima vuelva a variar por celda, y hoy es
        /// simplemente un caso particular más fácil, no un análisis distinto.)
        /// `CellGrid.ambient`
        /// sustituye a la constante `AmbientRaw` como objetivo del tirón: cada celda
        /// tira hacia SU PROPIO clima (`_grid.ambient[i]`, pintado una única vez por
        /// `SimLevelBuilder.PaintClimate` al construir el nivel, nunca durante el
        /// tick a tick), no hacia un 20°C fijo para todo el taller. Releída la
        /// verificación de la regla 9 con este cambio, punto por punto:
        ///  (a) COBERTURA 100%. `ambientSweep` sigue siendo exactamente
        ///      `((_tick&gt;&gt;3)&amp;3u)==0u`, SIN TOCAR: decide CUÁNDO se aplica el
        ///      tirón (una ronda de cada 4, uniforme para toda celda de offset fijo,
        ///      razonamiento del punto 1 de arriba) y es enteramente independiente de
        ///      CUÁL es el valor objetivo. Cambiar `CellGrid.AmbientRaw` por
        ///      `_grid.ambient[i]` es una lectura de array en el sitio donde antes
        ///      había una constante -- cero ramas nuevas, cero cambio de qué celdas
        ///      entran en la guarda: el 100% de cobertura de antes sigue siendo el
        ///      mismo 100%, celda por celda.
        ///  (b) REDONDEO SIMÉTRICO. El tirón hacia ambiente nunca usó `diff/4`: es un
        ///      paso de ±1 directo (`ad &gt; 0 ? 1 : -1`), simétrico por construcción
        ///      para cualquier signo de `ad` -- y `ad` ahora se mide contra
        ///      `_grid.ambient[i]` en vez de una constante, pero la operación de
        ///      redondeo en sí (el `? 1 : -1`) no cambió ni una línea. La división
        ///      entera `diff/4` de la difusión (paso 1 de esta función) tampoco se
        ///      tocó.
        ///  (c) FRONTERAS DE CLIMA -- ¿dos celdas vecinas con ambiente distinto se
        ///      bombean energía entre sí? NO, por construcción: el tirón de la celda
        ///      i SOLO lee `_grid.ambient[i]` (su propio objetivo fijo). En NINGÚN
        ///      sitio de esta función el tirón de una celda lee el `ambient` de un
        ///      VECINO -- no existe, por tanto, un lazo "A tira de B hacia el clima de
        ///      A": cada celda converge de forma independiente hacia SU objetivo, sin
        ///      que ese objetivo dependa de sus vecinos. La ÚNICA interacción ENTRE
        ///      vecinos sigue siendo la difusión del paso 1 (`avg` de las 4
        ///      TEMPERATURAS ACTUALES de los vecinos, nunca de sus ambientes), y esa
        ///      difusión ya estaba probada estable antes de este cambio. Además, los
        ///      dos términos que se suman a `cur` son contracciones que NUNCA
        ///      sobrepasan su objetivo: el paso de difusión cumple `|step| &lt;=
        ///      |diff|` con el mismo signo que `diff` (división entera truncada hacia
        ///      cero, nunca mayor que la distancia a recorrer), así que `next` nunca
        ///      cruza `avg`; el paso de ambiente es literalmente ±1 hacia el
        ///      objetivo, así que tampoco lo cruza (un objetivo entero a distancia
        ///      &gt;=1 nunca se sobrepasa moviéndose exactamente 1 unidad). Dos
        ///      operaciones que nunca sobrepasan su objetivo, compuestas y recortadas
        ///      a [0,255], no pueden producir una oscilación creciente ni una fuga de
        ///      energía sin límite -- como mucho convergen más rápido o más despacio
        ///      según cuánto difieran los climas de dos zonas. Por último,
        ///      `SimLevelBuilder.PaintClimate` pinta el clima en DEGRADADOS de varias
        ///      decenas de celdas (`ClimaGradienteX`/`ClimaGradienteY`), nunca un
        ///      escalón: entre dos celdas ortogonalmente vecinas `ambient` difiere
        ///      como mucho en 1 raw unit en la inmensa mayoría de las fronteras (el
        ///      salto más brusco posible, sótano-base o cultivo-base, siempre queda
        ///      repartido en ese degradado) -- la distancia entre los objetivos de dos
        ///      vecinos es siempre pequeña frente al peso de la difusión, que además
        ///      actúa 4x más a menudo que el tirón (cada 8 ticks contra cada 32): la
        ///      difusión gana la partida y el campo real converge a una versión
        ///      suavizada del degradado de clima, sin escalón visible y sin oscilar.
        ///
        /// (playtest 17 -- EL CLIMA POR ZONA SE RETIRÓ, esta función NO CAMBIA)
        /// `SimLevelBuilder.PaintClimate` ya no pinta zonas: pinta `CellGrid.
        /// AmbientRaw` uniforme en todo el mundo (el porqué, en el docblock de
        /// esa clase -- resumen: el taller va a ser movible, así que un clima
        /// atado a coordenadas fijas contradice la fase siguiente). El código de
        /// aquí abajo se queda EXACTAMENTE igual, leyendo `_grid.ambient[i]` por
        /// celda, por dos razones: cuesta lo mismo que una constante (una lectura
        /// de array), y es el vehículo del clima que sí volverá -- el que crea el
        /// JUGADOR (una fragua que entibia su alrededor), que por naturaleza es
        /// local y no puede expresarse con una constante global.
        /// El análisis (a)/(b)/(c) de arriba sigue siendo válido y hoy es además
        /// trivialmente cierto: con todos los `ambient` iguales, el punto (c)
        /// (fronteras de clima) no tiene ni siquiera una frontera que examinar.
        /// NO SIMPLIFICAR esto de vuelta a la constante `CellGrid.AmbientRaw`:
        /// se ganaría nada y se perdería el gancho.
        /// </summary>
        private void DiffuseTemperature()
        {
            int offset = (int)(_tick % 8u);
            int n = W * H;
            var temp = _grid.temp;

            // Cuántas veces lleva difundiéndose CADA celda (ver punto 1 arriba):
            // para una celda de offset fijo o = i%8, esto avanza en +1 exactamente
            // cada vez que le toca su turno (cada 8 ticks), independientemente de o.
            bool ambientSweep = ((_tick >> 3) & 3u) == 0u;

            for (int i = offset; i < n; i += 8)
            {
                int x = i % W;
                int y = i / W;
                if (x == 0 || x == W - 1 || y == 0 || y == H - 1) continue; // borde de Stone, inmutable

                int sum = temp[i - 1] + temp[i + 1] + temp[i - W] + temp[i + W];
                int avg = sum >> 2; // media de 4 vecinos: división exacta, sin signo negativo posible (temps son bytes >=0).
                int cur = temp[i];
                int diff = avg - cur;
                int step = diff / 4; // truncamiento hacia cero: simétrico en signo (fix playtest 9, ver doc de arriba).
                if (step == 0 && diff != 0) step = diff > 0 ? 1 : -1;
                int next = cur + step;

                // Atracción suave hacia la temperatura ambiente, poco frecuente, para
                // TODA la grilla (fix playtest 9: antes solo 1/8 de las celdas, ver doc).
                // (playtest 15) Objetivo por CELDA, no la constante global: ver punto (c)
                // de la verificación de arriba para el porqué esto no crea un lazo entre
                // vecinos con clima distinto.
                if (ambientSweep)
                {
                    int ad = _grid.ambient[i] - next;
                    if (ad != 0) next += ad > 0 ? 1 : -1;
                }

                if (next < 0) next = 0; else if (next > 255) next = 255;
                temp[i] = (byte)next;
            }
        }

        // ---------------------------------------------------------------------------------
        // CAMPO MORFOLÓGICO (playtest 12) -- ver CellGrid.morph/morphScratch y
        // MaterialDef.PatronMorfologico para el contrato completo. Solo evoluciona
        // para las 5 familias que lo necesitan: Manchas y Laberinto (reacción-difusión,
        // leen vecinos), Dendritas (rama que se propaga a un vecino), Pulso (fase que
        // avanza) y Motas (chispa que se enciende y decae). Liso no se usa; Vetas y
        // Celdas son puramente posicionales y las calcula SimRenderer con hashes --
        // esas tres familias cuestan CERO aquí, ni una sola operación aparte del
        // chequeo del enum `patron` que ya se hace para descartarlas.
        //
        // (playtest 15 -- mundo a 768x288, 6x) A 256x144 el estriado 1/4 barría 9.216
        // celdas/tick y los dos Array.Copy movían 2*36.864=73.728 bytes/tick: barato de
        // sobra. A 768x288 esas mismas cifras suben a 55.296 celdas/tick y 2*221.184=
        // 442.368 bytes/tick de copia PURA -- eso sí es un coste real y, a diferencia del
        // estriado (aritmética entera barata, sigue siendo aceptable tal cual), crece con
        // el tamaño del MUNDO en vez de con lo que hay que dibujar. Arreglo: en vez de
        // copiar y recorrer el array entero cada tick, MorphTick ahora trabaja por CHUNK
        // y se salta por completo cualquier chunk que no pueda haber cambiado este tick
        // (ver `_morphChunkRelevant`/`ChunkOrNeighborsAwake` más abajo) -- en un taller
        // típico (la mayor parte del mundo dormida) esto reduce el coste real a una
        // fracción pequeña de esas cifras; en el peor caso (todo el mundo despierto a la
        // vez) el coste iguala al de antes, nunca lo empeora en cifra de celdas (sí añade
        // el overhead, pequeño y acotado, de trocear la copia en bloques de 16 elementos
        // en vez de un único memcpy -- ver el comentario de la Fase 1).
        // ---------------------------------------------------------------------------------
        private void MorphTick()
        {
            // ---- Estriado 1/4 -- VERIFICACIÓN ARITMÉTICA de cobertura uniforme ----
            // offset = tick % 4 recorre {0,1,2,3,0,1,2,3,...} en secuencia estricta.
            // Cada celda i tiene un offset FIJO o=i%4 para siempre (no depende del tick).
            // "Le toca turno a la celda i" se evalúa como (tick%4)==(i%4): para cualquier
            // o fijo en {0,1,2,3}, esta condición se cumple exactamente una vez cada 4
            // ticks consecutivos (es congruencia módulo 4, no un subconjunto arbitrario).
            // Por tanto las CUATRO clases o=0,1,2,3 -- es decir, el 100% de las celdas --
            // se visitan con la MISMA frecuencia: una vez cada 4 ticks (7.5 Hz a 30 Hz de
            // sim). A diferencia de los dos bugs de deriva de temperatura del playtest 9,
            // aquí NO hay una guarda temporal separada combinada con el offset: el offset
            // ES la única guarda, así que no existe la combinación "offset fijo AND guarda
            // basada en el mismo tick" que dejaba 7/8 de la grilla sin visitar nunca en
            // DiffuseTemperature (bug 1 de ese fix). El único filtro adicional de abajo
            // (chunks dormidos) usa un contador de RONDA (`tick>>2`) que es el MISMO
            // número para TODAS las celdas visitadas en el mismo tick sin importar su
            // offset -- ver el comentario de `dormantActiveRound` -- así que tampoco
            // introduce ese patrón de bug. (playtest 15) Reorganizar el recorrido por
            // CHUNK en vez de en una única pasada plana no cambia este razonamiento: dentro
            // de un chunk, x0 = cx*CHUNK y CHUNK=16 son ambos múltiplo de 4, y W=768
            // también lo es, así que para cualquier fila `(y*W + x) % 4 == x % 4` --
            // recorrer `x` desde `x0+offset` en pasos de 4 dentro del chunk visita
            // EXACTAMENTE el mismo subconjunto de índices que el `i % 4 == offset` de
            // siempre, célula por célula, solo que agrupado por chunk.
            int offset = (int)(_tick % 4u);

            // ---- Chunks dormidos: SÍ se respetan, pero a 1/8 de frecuencia ----
            // Decisión: un chunk dormido (charco quieto, cristal ya formado...) sigue
            // evolucionando su patrón morfológico, solo que mucho más despacio, en vez de
            // congelarse a medias o de ignorar el ahorro de los chunks dormidos por
            // completo. Las dos alternativas descartadas tienen un problema real cada una:
            //   - Respetarlos a rajatabla (saltar del todo mientras duermen): un charco
            //     que se queda quieto justo cuando su Manchas/Laberinto está a medio
            //     converger se congela con ese dibujo incompleto PARA SIEMPRE (mientras
            //     no se vuelva a tocar) -- contradice literalmente la premisa del playtest
            //     12 ("vuelve a TENDER a formar el patrón", no "se congela a mitad de
            //     camino"). Además Dendritas/Pulso/Motas ni siquiera tienen un estado
            //     "converged": Motas dejaría de titilar del todo, Pulso dejaría de
            //     respirar -- se leería como un bug, no como sueño.
            //   - Ignorar el sueño (evolucionar dormidos a la misma frecuencia que
            //     despiertos): pierde el ahorro por el que existen los chunks dormidos.
            //     El taller de una partida típica tiene mucha más agua/aceite/arena
            //     (Liso, coste cero aquí de todas formas) en reposo que innominado
            //     RD/Dendritas/Pulso/Motas, así que el ahorro real de saltarse dormidos
            //     por completo sería pequeño comparado con el coste de "se ve mal".
            // g = tick>>2 es el número de ronda de 4 ticks en curso. Para CUALQUIER celda
            // a la que le toque turno en este tick (por construcción del estriado de
            // arriba, tick == offsetDeEsaCelda + 4*g), g es el MISMO valor para todas las
            // celdas procesadas este tick sin importar su offset -- por eso "g%8==0" no
            // favorece a ningún subconjunto de celdas dormidas sobre otro: en cada ronda
            // de 4 ticks, o mutan TODAS las celdas dormidas a las que les toca turno, o
            // NINGUNA. Resultado: un chunk dormido evoluciona su morph 1 vez cada 32
            // ticks (~1.07s a 30Hz) en vez de 1 vez cada 4 (~0.13s) -- 8x más barato -- y
            // el patrón converge igual, solo más despacio, tal como pide la premisa.
            bool dormantActiveRound = ((_tick >> 2) & 7u) == 0u;

            int chunksX = CellGrid.ChunksX;
            int chunksY = CellGrid.ChunksY;

            // ---- Fase 0: marca los chunks "relevantes" este tick -----------------------
            // Un chunk es relevante si ÉL MISMO o alguno de sus 8 vecinos está despierto
            // (o si `dormantActiveRound` hace que TODOS lo sean este tick, ver más abajo).
            // Ver `ChunkOrNeighborsAwake` para la prueba de que el radio 3x3 es exactamente
            // suficiente -- ni de más ni de menos -- para capturar todo lo que puede
            // modificar `morph` este tick.
            for (int cy = 0; cy < chunksY; cy++)
            {
                for (int cx = 0; cx < chunksX; cx++)
                {
                    _morphChunkRelevant[CellGrid.ChunkIndex(cx, cy)] =
                        dormantActiveRound || ChunkOrNeighborsAwake(cx, cy);
                }
            }

            var morph = _grid.morph;
            var scratch = _grid.morphScratch;

            // ---- Fase 1: sincroniza morphScratch = morph, SOLO en chunks relevantes ----
            // Obligatorio (doble búfer, regla 16): `morph` puede haber cambiado este mismo
            // tick por SwapCells (movimiento, en la pasada de ProcessIfNeeded/Move que ya
            // corrió antes de llegar aquí) o por SetCell (pintado del jugador, entre ticks)
            // -- ninguno de los dos toca `morphScratch`, así que hace falta re-sincronizar
            // ANTES de que la Fase 2 empiece a escribir, para que las familias de
            // reacción-difusión lean vecinos actualizados y no un morph de hace 4 ticks.
            // Trocear la copia en chunks de 16x16 en vez de un único Array.Copy del array
            // entero significa 16 llamadas (una por fila, ya que las filas de un chunk NO
            // son contiguas en memoria: stride W entre ellas) por chunk relevante en vez de
            // 1 llamada gigante -- más llamadas, pero cada una solo copia lo que puede
            // haber cambiado; en el peor caso (todo relevante) esto es más lento en
            // constante que el memcpy único de antes pero sigue siendo un puñado de
            // microsegundos frente al presupuesto de 33ms/tick a 30Hz, y en el caso típico
            // (mundo mayormente dormido) es la ganancia real de este cambio.
            for (int cy = 0; cy < chunksY; cy++)
            {
                for (int cx = 0; cx < chunksX; cx++)
                {
                    if (!_morphChunkRelevant[CellGrid.ChunkIndex(cx, cy)]) continue;
                    CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);
                    int len = x1 - x0;
                    for (int y = y0; y < y1; y++)
                    {
                        int rowStart = y * W + x0;
                        Array.Copy(morph, rowStart, scratch, rowStart, len);
                    }
                }
            }

            // ---- Fase 2: evoluciona -- SOLO escribe en morphScratch, NUNCA en morph ----
            // Recorre TODOS los chunks relevantes que además estén despiertos (o que sea
            // ronda dormida activa) y, dentro de cada uno, exactamente el 1/4 de celdas
            // que le toca este tick (ver la verificación de estriado de arriba). Familias
            // que solo escriben su propia celda (Manchas/Laberinto/Pulso/Motas) nunca
            // salen del chunk que se está recorriendo; Dendritas puede escribir en UN
            // vecino ortogonal que caiga en un chunk distinto -- por eso la Fase 1 ya
            // sincronizó también los vecinos de cualquier chunk despierto (radio 3x3), y
            // por eso esta fase entera corre COMPLETA antes de que la Fase 3 copie nada de
            // vuelta: ningún chunk relevante puede recibir una escritura de Dendritas antes
            // de tener su scratch sincronizado, sin importar en qué orden se recorran los
            // chunks aquí (conmutativo, ver MorphDendrites).
            for (int cy = 0; cy < chunksY; cy++)
            {
                for (int cx = 0; cx < chunksX; cx++)
                {
                    int ci = CellGrid.ChunkIndex(cx, cy);
                    if (!_morphChunkRelevant[ci]) continue;
                    if (!_grid.IsChunkAwake(cx, cy) && !dormantActiveRound) continue;

                    CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);
                    for (int y = y0; y < y1; y++)
                    {
                        int rowBase = y * W;
                        for (int x = x0 + offset; x < x1; x += 4)
                        {
                            int i = rowBase + x;
                            byte m = _grid.mat[i];
                            if (m == MaterialId.Empty) continue;

                            var def = _universe.Get(m);
                            var patron = def.patron;
                            bool needsEvolve = patron == PatronMorfologico.Manchas || patron == PatronMorfologico.Laberinto
                                || patron == PatronMorfologico.Dendritas || patron == PatronMorfologico.Pulso
                                || patron == PatronMorfologico.Motas;
                            if (!needsEvolve) continue; // Liso/Vetas/Celdas: coste cero, ver cabecera de MorphTick.

                            switch (patron)
                            {
                                case PatronMorfologico.Manchas:
                                    MorphReactionDiffusion(x, y, i, def, laberinto: false);
                                    break;
                                case PatronMorfologico.Laberinto:
                                    MorphReactionDiffusion(x, y, i, def, laberinto: true);
                                    break;
                                case PatronMorfologico.Dendritas:
                                    MorphDendrites(x, y, i, def);
                                    break;
                                case PatronMorfologico.Pulso:
                                    MorphPulse(x, y, i, def);
                                    break;
                                case PatronMorfologico.Motas:
                                    MorphSparkle(x, y, i, def);
                                    break;
                            }
                        }
                    }
                }
            }

            // ---- Fase 3: copia de vuelta morphScratch -> morph, SOLO chunks relevantes -
            // Simétrica a la Fase 1. Cualquier chunk NO relevante quedó, por construcción,
            // sin tocar en morph NI en morphScratch este tick (nada pudo escribir en él:
            // ni él ni sus 8 vecinos estaban despiertos, y no es ronda dormida activa) --
            // así que morph y morphScratch YA eran iguales ahí antes de este tick y lo
            // siguen siendo después, sin necesidad de copiar nada (ver `ChunkOrNeighborsAwake`).
            for (int cy = 0; cy < chunksY; cy++)
            {
                for (int cx = 0; cx < chunksX; cx++)
                {
                    if (!_morphChunkRelevant[CellGrid.ChunkIndex(cx, cy)]) continue;
                    CellGrid.ChunkBounds(cx, cy, out int x0, out int y0, out int x1, out int y1);
                    int len = x1 - x0;
                    for (int y = y0; y < y1; y++)
                    {
                        int rowStart = y * W + x0;
                        Array.Copy(scratch, rowStart, morph, rowStart, len);
                    }
                }
            }
        }

        /// <summary>
        /// (playtest 15) ¿Este chunk o alguno de sus 8 vecinos está despierto? Mismo
        /// radio 3x3 que <see cref="CellGrid.WakeChunk"/> (documentado ahí: "para que las
        /// reacciones que cruzan el borde de un chunk no se pierdan") -- aquí por el mismo
        /// motivo pero para escrituras de `morph`: dentro de un mismo tick, una celda
        /// puede moverse UNA celda de distancia (Move/SwapCells, dx,dy en {-1,0,1}) o
        /// Dendritas puede propagar su rama a UN vecino ortogonal -- en ambos casos el
        /// desplazamiento es de, como mucho, 1 celda, así que el chunk DESTINO de
        /// cualquier escritura de `morph` originada en un chunk despierto es, por fuerza,
        /// ese mismo chunk o uno de sus 8 vecinos directos (el tamaño de chunk, 16, es
        /// mucho mayor que el alcance de 1 celda de cualquiera de las dos operaciones, así
        /// que nunca se salta un chunk entero). Por eso el radio 3x3 es EXACTAMENTE
        /// suficiente: ni hace falta más (nada llega más lejos de 1 celda) ni menos
        /// (ambas operaciones sí cruzan la frontera de un chunk cuando la celda de origen
        /// está en su borde).
        /// </summary>
        private bool ChunkOrNeighborsAwake(int cx, int cy)
        {
            int cy0 = cy > 0 ? cy - 1 : 0;
            int cy1 = cy < CellGrid.ChunksY - 1 ? cy + 1 : CellGrid.ChunksY - 1;
            int cx0 = cx > 0 ? cx - 1 : 0;
            int cx1 = cx < CellGrid.ChunksX - 1 ? cx + 1 : CellGrid.ChunksX - 1;
            for (int ny = cy0; ny <= cy1; ny++)
            {
                for (int nx = cx0; nx <= cx1; nx++)
                {
                    if (_grid.IsChunkAwake(nx, ny)) return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Manchas y Laberinto: reacción-difusión de un único campo (morph = "v", el
        /// reactivo) al estilo Gray-Scott, simplificado a UN byte por celda en vez de
        /// dos especies (u,v) porque CellGrid solo lleva un morph por celda por diseño
        /// (contrato del renderer). El sustrato "u" se aproxima como el complementario
        /// local u=255-v (allí donde v es alto, se asume que el sustrato disponible es
        /// bajo) -- no es Gray-Scott textbook (ahí u difunde por su cuenta), pero
        /// reproduce el mismo comportamiento cualitativo: producción autocatalítica
        /// uv² que se satura cuando v→255 (u→0) y decae con (feed+kill)*v.
        ///
        /// PARÁMETROS (derivados de patronEscala 1..8 y patronFuerza 0..255): "feed" usa
        /// la MISMA fórmula en los dos regímenes (feed=8..23, solo de patronFuerza) y lo
        /// que separa Manchas de Laberinto es el DELTA kill-feed, deliberadamente en
        /// bandas que NUNCA se solapan aunque feed varíe:
        ///   Laberinto (bandas):  kill-feed = 2..6   (delta pequeño: kill≈feed)
        ///   Manchas (puntos):    kill-feed = 16..23 (delta grande: kill≫feed)
        /// kill≫feed (Manchas) es el régimen real de Gray-Scott para puntos: el reactivo
        /// no se sostiene en un frente amplio y colapsa en manchas aisladas que compiten
        /// por el sustrato. kill≈feed (Laberinto) es el régimen de bandas/laberinto real:
        /// el reactivo se sostiene en frentes alargados que serpentean en vez de
        /// colapsar. Se usa el DELTA (no el ratio kill/feed) para que las dos bandas
        /// NUNCA se toquen sea cual sea el valor de feed que toque por seed:
        /// delta_manchas_min(16) > delta_laberinto_max(6) siempre.
        ///
        /// EL ANCLAJE DE RUIDO (playtest 20, la parte nueva): un sistema biestable de un
        /// solo campo con difusión puramente local, sin más, SIEMPRE termina
        /// homogeneizándose en un dominio acotado por mucho que se ajuste "diffDiv" --
        /// es "coarsening" tipo Allen-Cahn, no un fallo de calibración. Confirmado con
        /// una réplica en Python del propio Gray-Scott simplificado (ver el informe de
        /// la ronda): en un charco AISLADO de 20-40 celdas, con la fórmula vieja
        /// (diffDiv=20-escala*2, sin más) el campo converge a un tinte casi plano en
        /// TODAS las escalas -- exactamente lo que Cesar reportó y el playtest 19 solo
        /// pudo sospechar sin confirmar. Forzar diffDiv al límite de estabilidad
        /// numérica del laplaciano de 4 vecinos (diffDiv=4) sí produce estructura, pero
        /// OSCILA de verdad cada tick (confirmado con el mismo modelo: hasta 130/255 de
        /// cambio por tick, un parpadeo real, no percibido) -- viola el punto 4 del
        /// encargo ("que no se convierta en hervido nervioso"). La solución que sí
        /// converge y se queda quieta: una heterogeneidad FIJA por BLOQUE de "block"
        /// celdas (variable local más abajo, 1 o 2 según patronEscala; mapa estático,
        /// calculado con XorShift.FromCell usando un tick CONSTANTE=0 en vez de _tick --
        /// por diseño, no cambia turno a turno). Bloques "fríos" decaen a 0 sin remedio;
        /// bloques "calientes" sostienen su punto fijo alto; la propia difusión (a un
        /// diffDiv ya seguro, ≥16, lejos del límite de estabilidad) redondea la frontera
        /// entre ambos. Resultado: un patrón ESTABLE (no parpadea, converge y se queda)
        /// y no plano, con manchas/bandas de un tamaño ligado a "block" en vez de a la
        /// longitud de difusión (que en un solo campo no tiene "longitud de onda"
        /// propia que seleccionar, a diferencia de un Gray-Scott de dos especies).
        ///
        /// LA MONEDA, NO UN JITTER DE AMPLITUD FIJA (auditoría independiente de esta
        /// misma ronda): la primera versión de este anclaje sumaba a "kill" un ruido
        /// ±100 CONSTANTE, sin relación con la magnitud real de kill (10..29 en
        /// Laberinto, 24..46 en Manchas) -- entre 3x y 10x el propio valor que
        /// perturbaba, así que en Laberinto (kill bajo) cerca de la mitad de los
        /// bloques quedaban pegados al clamp inferior killLocal=1, un régimen que no es
        /// ni Laberinto ni Manchas y que borraba la distinción entre las dos familias
        /// que el propio DELTA de arriba jura proteger. La cifra "100" no tenía además
        /// ninguna derivación: a diferencia de diffDiv/decayStep/seedChanceInv/chanceInv
        /// (cada uno con un número salido de la réplica en Python), era una constante
        /// suelta. Corregido con una cifra real: resolviendo el punto fijo del reactivo
        /// (255-v)v=256·S, esa ecuación solo tiene raíces reales -- el punto fijo alto
        /// que sostiene la sustancia -- cuando el discriminante 255²-4·256·S no es
        /// negativo, es decir S=feed+kill ≤ 255²/1024 ≈ 63,5; por encima de eso el único
        /// punto fijo es v=0 y el reactivo muere sin remedio. Con eso, la moneda de cada
        /// bloque es binaria y estática (tick fijo=0): "cara" deja kill EXACTAMENTE como
        /// lo definió la familia -- el delta manchas/laberinto llega intacto a esa mitad
        /// de los bloques, nada de ruido encima --; "cruz" empuja S a un objetivo fijo
        /// de 75 (bien pasado 63,5, con margen para no depender de dónde caía kill antes
        /// de moverlo), así que ese bloque decae a 0 con garantías. El boost SIEMPRE se
        /// SUMA (nunca resta), así que en el camino normal killLocal nunca necesita el
        /// clamp -- el clamp que queda es defensivo, no la salida esperada del cálculo.
        /// Contrapartida medida y aceptada (no escondida): con esto Manchas y Laberinto
        /// siguen sin diferenciarse por FORMA (puntos vs bandas) tanto como el diseño
        /// original aspiraba -- ese ratio de forma nunca fue mecánicamente real en esta
        /// aproximación de un solo campo, ver el informe -- pero sí se diferencian por
        /// COBERTURA/BRILLO medio (Manchas, con su S más alto, deja sistemáticamente
        /// menos superficie "caliente" y un tinte más oscuro que Laberinto a igualdad de
        /// escala y fuerza; medido con el mismo modelo, ~20-30 puntos de media sobre
        /// 255), y ninguna de las dos vuelve a colapsar a plano ni depende de un clamp
        /// saturado para sostenerse.
        /// </summary>
        private void MorphReactionDiffusion(int x, int y, int idx, MaterialDef def, bool laberinto)
        {
            if (x == 0 || x == W - 1 || y == 0 || y == H - 1) return; // defensivo: el borde es siempre Stone/Liso y no debería llegar aquí de todas formas.

            int v = _grid.morph[idx];
            int left = _grid.morph[idx - 1];
            int right = _grid.morph[idx + 1];
            int down = _grid.morph[idx - W];
            int up = _grid.morph[idx + W];

            int feed = 8 + (def.patronFuerza >> 4); // 8..23, misma fórmula en ambos regímenes.
            int kill;
            if (laberinto)
            {
                kill = feed + 2 + (def.patronEscala >> 1); // delta 2..6.
            }
            else
            {
                kill = feed + 15 + def.patronEscala; // delta 16..23.
            }

            // Anclaje de ruido ESTÁTICO por bloque (ver cabecera): block(escala) es el
            // tamaño de rasgo en celdas -- 1 en escala baja (grano fino, varias manchas
            // diminutas caben en un charco de 20-40 celdas), 2 en escala alta (parches
            // más anchos, pero SIN llegar a que uno solo ocupe el charco entero, que es
            // justo el colapso que se está arreglando). División entera por block: como
            // x,y nunca son negativos aquí (defensivo de arriba ya descarta el borde),
            // "/" trunca igual que la posición del bloque espera.
            int block = def.patronEscala <= 4 ? 1 : 2;
            int bx = x / block;
            int by = y / block;

            // MONEDA 50/50 por bloque, NO ruido de amplitud fija (playtest 20, auditoría
            // independiente): la primera versión sumaba un jitter ±100 fijo a "kill" --
            // un número sin ninguna cifra detrás, y ±100 es de 3x a 10x el propio kill
            // real (10..29 en Laberinto, 24..46 en Manchas, feed 8..23), así que para
            // Laberinto con kill bajo la mitad de los bloques quedaba pegada al clamp
            // inferior killLocal=1 -- un régimen que no es NI Laberinto NI Manchas,
            // borrando la distinción entre las dos que la cabecera jura que protege.
            // Arreglo con una cifra real detrás: (255-v)v=256·S tiene raíces reales
            // (el punto fijo alto que sostiene la sustancia) solo si el discriminante
            // 255²-4·256·S no es negativo, es decir S=feed+kill <= 255²/1024 ≈ 63,5 --
            // Manchas/Laberinto viven SIEMPRE por debajo de eso con su kill de diseño
            // (S real 24..40 en Laberinto, 38..57 en Manchas para feed 11..17), así que
            // ambos son bloques "calientes" de sobra por defecto. Cada bloque tira una
            // moneda ESTÁTICA (tick fijo=0, no _tick -- si cambiara turno a turno el
            // propio anclaje parpadearía, igual que antes): "cara" deja kill EXACTAMENTE
            // como lo definió la familia (nada de ruido encima -- el delta manchas/
            // laberinto llega intacto a la mitad de los bloques, sin diluirse);
            // "cruz" empuja S a ColdTargetS=75, bien por encima de 63,5 con margen, así
            // que ese bloque decae a 0 sin remedio, "frío" de verdad. No hace falta
            // clampear en el camino normal (ColdBoost se SUMA, nunca resta, así que
            // killLocal no baja de "kill"); el clamp de abajo es solo defensivo por si
            // patronFuerza/patronEscala se salieran algún día del rango nominal.
            const int ColdTargetS = 75;
            var coinRng = XorShift.FromCell(0u, bx, by, (uint)(221 + def.semillaPatron));
            bool cold = coinRng.Next(2) == 0;
            int killLocal = kill;
            if (cold)
            {
                int coldBoost = ColdTargetS - (feed + kill);
                if (coldBoost < 20) coldBoost = 20; // piso defensivo: siempre un empujón real, aunque S ya estuviera muy cerca de 63,5.
                killLocal = kill + coldBoost;
            }
            if (killLocal < 1) killLocal = 1; else if (killLocal > 200) killLocal = 200; // defensivo, ver arriba -- no debería dispararse con patronEscala/patronFuerza en su rango nominal.

            // Difusión: SIEMPRE por encima del límite de estabilidad del laplaciano de
            // 4 vecinos (diffDiv>=4, ver cabecera) -- 24-escala cae en 16..23, muy lejos
            // del filo, así que nunca oscila. Escala alta -> diffDiv más bajo -> más
            // peso de difusión -> bordes más suaves/redondeados sobre los parches (más
            // anchos por el bloque) más grandes de esa escala; escala baja -> bordes más
            // netos sobre el grano fino. truncamiento hacia cero (operador "/", no ">>":
            // mismo criterio de simetría de signo que el fix de temperatura del
            // playtest 9 -- lap puede ser negativo).
            int diffDiv = 24 - def.patronEscala; // escala 1->23, escala 8->16.
            if (diffDiv < 4) diffDiv = 4; // defensivo: nunca por debajo del límite de estabilidad numérica (ver cabecera), aunque patronEscala se saliera algún día del 1..8 nominal.

            int lap = left + right + up + down - 4 * v;
            int diffuseStep = lap / diffDiv;

            int u = 255 - v;
            int reactTerm = (u * v * v) >> 16;             // aprox. u*v² escalado a ~0..254
            int decay = ((feed + killLocal) * v) >> 8;       // aprox. (feed+killLocal)*v

            int next = v + diffuseStep + reactTerm - decay;
            if (next < 0) next = 0; else if (next > 255) next = 255;
            _grid.morphScratch[idx] = (byte)next;
        }

        /// <summary>
        /// Dendritas: morph = fuerza de rama (0 = sin rama). Semillas dispersas y raras
        /// (patronEscala controla la densidad); desde una semilla, la rama se propaga a
        /// UN vecino por tick elegido con sesgo direccional fijo por
        /// <see cref="MaterialDef.semillaPatron"/> (mismo eje preferido que usa
        /// <see cref="GetCrystalDirOrder"/> para el modo dendrítico de cristalización:
        /// forma y textura cuentan la misma historia), decayendo con la distancia
        /// recorrida -- así se lee como aguja que se afina hacia la punta, no como una
        /// mancha redonda que crece uniforme.
        ///
        /// ORÍGENES ELEGIBLES (playtest 20, la parte nueva): con la fórmula vieja
        /// (seedChanceInv 600..2700, decayStep 11..18) una réplica en Python de esta
        /// misma regla mostró que en un charco AISLADO de 20-40 celdas, dado tiempo
        /// suficiente (unos 300 ticks, 10s a 30Hz -- nada raro en una partida), CUALQUIER
        /// celda a v=0 puede volver a sembrar, así que el conjunto de celdas alguna vez
        /// tocadas por una rama solo CRECE con el tiempo (nunca se vacía) hasta cubrir
        /// el 100% del charco -- el patrón se lee entonces como un gradiente borroso
        /// uniforme, no como agujas aisladas: el mismo colapso "a tinte plano" que
        /// Manchas, por un mecanismo distinto (percolación, no coarsening). La cura:
        /// solo un SUBCONJUNTO FIJO y disperso de celdas puede arrancar una semilla
        /// (mapa "elegible" estático por celda, calculado con XorShift.FromCell y un
        /// tick CONSTANTE=0 en vez de _tick, igual que el anclaje de
        /// <see cref="MorphReactionDiffusion"/>); las celdas NO elegibles solo reciben
        /// valor por propagación desde una rama viva y, al decaer a 0, se quedan
        /// apagadas para siempre salvo que otra rama vuelva a pasar por ahí. Con eso,
        /// más un decayStep MUCHO mayor (una rama solo alcanza unas pocas celdas antes
        /// de apagarse, en vez de encadenar resiembras que la hacen crecer sin límite),
        /// el charco converge a una cobertura PARCIAL y ESTABLE (confirmado con el mismo
        /// modelo: 13-90% según escala, sin parpadeo turno a turno) en vez de 0% o 100%.
        /// </summary>
        private void MorphDendrites(int x, int y, int idx, MaterialDef def)
        {
            int v = _grid.morph[idx];

            if (v == 0)
            {
                // Origen elegible (ver cabecera): mapa ESTÁTICO (tick fijo=0), 1 cada
                // ~(10-escala/2) celdas -- 10 en escala baja (pocos orígenes, agujas
                // finas y aisladas), 6 en escala alta (más orígenes, pero las agujas
                // siguen siendo cortas por el decayStep grande de más abajo, así que no
                // llegan a fundirse en una alfombra).
                int eligibleK = 10 - def.patronEscala / 2; // 10..6
                if (eligibleK < 1) eligibleK = 1;
                var eligibleRng = XorShift.FromCell(0u, x, y, (uint)(213 + def.semillaPatron));
                if (eligibleRng.Next(eligibleK) != 0) return; // esta celda nunca arranca una semilla.

                // Semilla RARA además de dispersa: sin esto, la celda elegible sembraría
                // en cuanto quedara libre y el "orígenes fijos" de arriba no bastaría por
                // sí solo para acotar la cobertura.
                int seedChanceInv = 100 + (8 - def.patronEscala) * 40; // 1 entre 100..380 por turno de esta celda.
                var seedRng = XorShift.FromCell(_tick, x, y, (uint)(201 + def.semillaPatron));
                if (seedRng.Next(seedChanceInv) == 0)
                {
                    _grid.morphScratch[idx] = (byte)(200 + seedRng.Next(56)); // 200..255: arranca fuerte.
                }
                return;
            }

            if (x == 0 || x == W - 1 || y == 0 || y == H - 1) return; // no propaga fuera de rango (defensivo).

            // Decaimiento por paso: MUCHO más agresivo que antes (145..75 en vez de
            // 11..18) -- ver cabecera, es lo que evita que una sola rama, sostenida por
            // resiembras sucesivas en su origen, acabe recorriendo el charco entero.
            // Escala alta -> decayStep más bajo -> agujas algo más largas (coherente con
            // "escala alta = rasgo más grueso" del resto de familias) pero SIN acercarse
            // al régimen de la fórmula vieja que inundaba el charco.
            int decayStep = 155 - 10 * def.patronEscala; // escala 1->145, escala 8->75.
            int next = v - decayStep;
            if (next <= 0) return; // la rama muere aquí; no se propaga más.

            var rng = XorShift.FromCell(_tick, x, y, 205);
            int preferredDir = def.semillaPatron & 3;
            // 70% continúa por el eje preferido de la sustancia (fuerte sesgo
            // direccional: agujas, no manchas); el resto reparte entre las otras 3.
            int dir = rng.ChancePercent(70) ? preferredDir : (preferredDir + 1 + rng.Next(3)) & 3;

            int nx = x + DirX[dir], ny = y + DirY[dir];
            if (!CellGrid.InBounds(nx, ny)) return;
            int nidx = CellGrid.Idx(nx, ny);
            if (_grid.mat[nidx] != _grid.mat[idx]) return; // la rama no cruza a otra sustancia.

            // max() en vez de sobrescribir: conmutativo, así que si dos ramas compiten
            // por el mismo vecino en el mismo tick, el resultado no depende de cuál de
            // las dos se procesó primero en el recorrido (determinismo, ver cabecera).
            if (_grid.morphScratch[nidx] < next) _grid.morphScratch[nidx] = (byte)next;
        }

        /// <summary>
        /// Pulso: morph = fase 0..255. Se recalcula como función DIRECTA de
        /// (tick, posición) en vez de acumular sobre el valor anterior: fase =
        /// (tick * velocidad + desfaseEspacial) mod 256. Esto evita cualquier deriva
        /// por acumulación de redondeo y es autocorrectivo si la celda cambia de
        /// posición (un líquido con Pulso que fluye arrastra un morph "viejo" con el
        /// SwapCells de CellGrid, pero en su próximo turno de estriado se recalcula de
        /// cero para su posición ACTUAL, sin arrastrar error). El desfase espacial usa
        /// distancia Manhattan a un ancla fija por sustancia (semillaPatron) para que la
        /// "ola" de fase recorra la masa en vez de que todas las celdas respiren a la
        /// vez.
        ///
        /// EL MULTIPLICADOR "5" ERA UNA CONSTANTE, NO USABA patronEscala (playtest 20,
        /// bug encontrado esta ronda): con spatialOffset=(dist*5)&amp;0xFF el periodo
        /// espacial de la onda es 256/5≈51 celdas -- más de una PANTALLA de recipiente
        /// (regla 24/39), así que en cualquier charco de 20-40 celdas el jugador nunca
        /// llega a ver un ciclo completo: la "onda" se lee como un degradado liso que
        /// crece hacia una esquina, no como un pulso con bandas que se repiten. Y como
        /// ninguna otra familia deriva a su vez el tamaño de Pulso de patronEscala (a
        /// diferencia de Manchas/Laberinto/Dendritas, todos tocados esta misma ronda),
        /// Pulso era la única de las 8 familias cuya escala NO HACÍA NADA en absoluto.
        /// Arreglo: el mismo periodo en celdas que ya usa <c>SimRenderer.PatronPeriodoCeldas</c>
        /// para Vetas/Celdas (3+(escala-1)/2, 3..6 celdas -- validado contra los
        /// recipientes reales del taller por la regla 39), reutilizado aquí como
        /// multiplicador espacial: periodo pequeño -> multiplicador grande -> bandas
        /// finas y frecuentes; periodo grande -> multiplicador pequeño -> bandas anchas.
        /// A 3-6 celdas de periodo, un charco de 20-40 celdas ya muestra varias bandas
        /// completas (el mismo criterio de repetición de la regla 24).
        /// </summary>
        private void MorphPulse(int x, int y, int idx, MaterialDef def)
        {
            // ritmoAnim (0..255, mismo campo que ya usa el renderer) fija la velocidad:
            // avanza 1..16 unidades de fase por CADA turno de estriado de esta celda
            // (que le toca una vez cada 4 ticks), así que a ritmoAnim alto (~160, típico
            // de Organic) completa una vuelta de fase cada ~64 turnos (~256 ticks,
            // ~8.5s) -- un respirar lento y perceptible, no un parpadeo.
            uint speed = (uint)(1 + (def.ritmoAnim >> 4)); // 1..16
            uint globalPhase = (_tick * speed) & 0xFFu;

            int anchorX = def.semillaPatron % W;
            int anchorY = (def.semillaPatron * 41) % H; // segundo hash barato, para no alinear anchorY con anchorX.
            int dist = System.Math.Abs(x - anchorX) + System.Math.Abs(y - anchorY);

            // periodoCeldas: MISMA fórmula que SimRenderer.PatronPeriodoCeldas (Vetas/
            // Celdas), para que "escala" signifique lo mismo (tamaño de rasgo en
            // celdas) en toda la firma visual de la sustancia, no solo en dos de las
            // ocho familias. spatialMult=256/periodo (entero, trunca): periodo 3->85,
            // periodo 6->42.
            int periodoCeldas = 3 + (def.patronEscala - 1) / 2; // 3..6.
            int spatialMult = 256 / periodoCeldas;
            int spatialOffset = (dist * spatialMult) & 0xFF; // bandas de "onda expansiva" concéntrica (Manhattan) desde el ancla.

            _grid.morphScratch[idx] = (byte)((globalPhase + (uint)spatialOffset) & 0xFFu);
        }

        /// <summary>
        /// Motas: morph = intensidad de una chispa (0 = apagada). Rara vez se enciende
        /// una celda a un valor alto (patronEscala controla la rareza) y decae rápido
        /// con el TIEMPO (a diferencia de Dendritas, que decae con la distancia
        /// recorrida): unos pocos turnos de vida, un parpadeo, no una mancha que se
        /// queda pintada.
        ///
        /// CHANCEINV RECALIBRADO (playtest 20): con la vida de una chispa en 3-6 turnos
        /// (~12-24 ticks) y chanceInv=900..2300, una réplica en Python de esta regla
        /// mostró que un charco de ~30 celdas tenía alguna mota encendida en menos del
        /// 10% de los ticks muestreados -- prácticamente invisible en la práctica, y
        /// exactamente lo que Cesar reportó ("no encontré cambios"). Con chanceInv
        /// bajado a 80..360 el mismo charco muestra alguna mota encendida 24% del
        /// tiempo en escala baja (sigue siendo un material "inquieto pero esquivo": el
        /// diseño pide rareza) y 78% del tiempo en escala alta, con hasta 4-5 motas
        /// simultáneas -- lo bastante frecuente para que el jugador lo note sin que dos
        /// materiales Motas de escalas distintas se lean igual.
        /// </summary>
        private void MorphSparkle(int x, int y, int idx, MaterialDef def)
        {
            int v = _grid.morph[idx];
            if (v > 0)
            {
                int decay = 40 + (def.patronFuerza >> 2); // 40..~103 por turno -> muere en 3-6 turnos (12-24 ticks, ~0.4-0.8s).
                int next = v - decay;
                _grid.morphScratch[idx] = (byte)(next > 0 ? next : 0);
                return;
            }

            int chanceInv = 400 - def.patronEscala * 40; // 1 entre ~80..360 por turno de esta celda.
            if (chanceInv < 50) chanceInv = 50;
            var rng = XorShift.FromCell(_tick, x, y, (uint)(209 + def.semillaPatron));
            if (rng.Next(chanceInv) == 0)
            {
                _grid.morphScratch[idx] = (byte)(220 + rng.Next(36)); // 220..255: chispazo brillante.
            }
        }
    }
}
