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

        /// <summary>Array fijo (no crece) de eventos notables; leer entre un "lastSeenHead" propio y <see cref="EventHead"/>, ambos módulo EventBufferSize.</summary>
        public SimNotableEvent[] Events => _events;
        public int EventHead => _eventHead;

        public SimStepper(Universe universe, CellGrid grid)
        {
            _universe = universe;
            _grid = grid;
        }

        private void PushEvent(SimEventType type, byte matId, int x, int y)
        {
            ref var e = ref _events[_eventHead];
            e.type = type;
            e.matId = matId;
            e.x = (short)x;
            e.y = (short)y;
            e.tick = _tick;
            _eventHead = (_eventHead + 1) & (EventBufferSize - 1);
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
                    if (m == MaterialId.Ice) InjectCold(x, y, 2);
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

            if (TryReactNeighbor(x, y, idx, ref matSelf, x - 1, y)) return;
            if (TryReactNeighbor(x, y, idx, ref matSelf, x + 1, y)) return;
            if (TryReactNeighbor(x, y, idx, ref matSelf, x, y - 1)) return;
            TryReactNeighbor(x, y, idx, ref matSelf, x, y + 1);
        }

        /// <summary>Consulta y, si procede, aplica la reacción entre la celda (x,y)/idx y su vecino (nx,ny). Devuelve true si algo reaccionó (para dejar de comprobar más vecinos: matSelf pudo cambiar).</summary>
        private bool TryReactNeighbor(int x, int y, int idx, ref byte matSelf, int nx, int ny)
        {
            if (!CellGrid.InBounds(nx, ny)) return false;
            int nidx = CellGrid.Idx(nx, ny);
            byte matNeighbor = _grid.mat[nidx];
            if (matNeighbor == MaterialId.Empty) return false;

            if (!_universe.Reactions.TryGet(matSelf, matNeighbor, out var reaction)) return false;

            byte t = _grid.temp[idx];
            if (t < reaction.minTempRaw || t > reaction.maxTempRaw) return false;

            var rng = XorShift.FromCell(_tick, x, y, 77);
            if (!rng.ChancePercent(reaction.chancePct)) return false;

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
            if (life > 0) life--;
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
            if (hotEnough || rng.ChancePercent(30))
            {
                PushEvent(SimEventType.Ignite, nm, nx, ny);
                Transform(nidx, MaterialId.Fire);
            }
        }

        // ---------------------------------------------------------------------------------
        // Organic (Vivium)
        // ---------------------------------------------------------------------------------
        private const byte SettledFlag = 0x80;
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
        /// Crecimiento de Vivium (M3, arco de domesticación): una célula asentada
        /// con un vecino ortogonal de Nutrient Y su propia temperatura dentro de
        /// [Universe.VivGrowMinRaw, VivGrowMaxRaw] consume ESE Nutrient (-&gt; Empty)
        /// y, con Universe.VivGrowChancePct de probabilidad, crea una nueva célula
        /// de Vivium en su lugar (si falla, el Nutrient se pierde igualmente: la
        /// célula "gastó" el intento). Como mucho un Nutrient por célula y por tick
        /// (evita un relleno instantáneo; el throttle de abajo lo ralentiza más aún
        /// para que se LEA como un coral creciendo, no como un flood-fill).
        /// Fuera de banda: la célula queda "dormida" (bit CellGrid.OrganicDormantAux,
        /// leído por SimRenderer para una ligera desaturación) -- no crece, pero
        /// tampoco muere (solo se quema por encima de ~120°C vía ApplyPhase/boilsAt,
        /// que reutiliza el mecanismo genérico de transición de fase).
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

            var rng = XorShift.FromCell(_tick, x, y, 88);
            int start = rng.Next(4);

            for (int i = 0; i < 4; i++)
            {
                int dir = (start + i) & 3;
                int nx = x + DirX[dir], ny = y + DirY[dir];
                if (!CellGrid.InBounds(nx, ny)) continue;
                int nidx = CellGrid.Idx(nx, ny);
                if (_grid.mat[nidx] != MaterialId.Nutrient) continue;

                bool grows = rng.ChancePercent(_universe.VivGrowChancePct);
                Transform(nidx, MaterialId.Empty);
                if (grows)
                {
                    Transform(nidx, MaterialId.Vivium);
                    PushEvent(SimEventType.Grow, MaterialId.Vivium, nx, ny);
                }
                return; // un Nutrient por célula y por tick.
            }
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
        /// </summary>
        private void DiffuseTemperature()
        {
            int offset = (int)(_tick % 8u);
            int n = W * H;
            var temp = _grid.temp;

            for (int i = offset; i < n; i += 8)
            {
                int x = i % W;
                int y = i / W;
                if (x == 0 || x == W - 1 || y == 0 || y == H - 1) continue; // borde de Stone, inmutable

                int sum = temp[i - 1] + temp[i + 1] + temp[i - W] + temp[i + W];
                int avg = sum >> 2;
                int cur = temp[i];
                int diff = avg - cur;
                int step = diff >> 2; // suavizado (division entera por 4)
                if (step == 0 && diff != 0) step = diff > 0 ? 1 : -1;
                int next = cur + step;

                // Atracción suave hacia la temperatura ambiente, poco frecuente.
                if (((_tick >> 3) & 3u) == 0u)
                {
                    int ad = CellGrid.AmbientRaw - next;
                    if (ad != 0) next += ad > 0 ? 1 : -1;
                }

                if (next < 0) next = 0; else if (next > 255) next = 255;
                temp[i] = (byte)next;
            }
        }
    }
}
