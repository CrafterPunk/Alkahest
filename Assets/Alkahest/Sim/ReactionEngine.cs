namespace Alkahest.Sim
{
    /// <summary>
    /// Reacción de contacto por pareja de materiales: cuando una celda de
    /// <see cref="a"/> toca ortogonalmente una celda de <see cref="b"/> (o
    /// viceversa, la tabla es simétrica), con <see cref="chancePct"/>/tick de
    /// probabilidad y solo si la temperatura de la celda que dispara la
    /// comprobación cae en [<see cref="minTempRaw"/>, <see cref="maxTempRaw"/>]
    /// (raw 0..255, ver CellGrid.CToRaw), la celda de <see cref="a"/> pasa a
    /// ser <see cref="productA"/> y la de <see cref="b"/> pasa a ser
    /// <see cref="productB"/>. Si un producto es igual al material original,
    /// esa celda no cambia (permite reacciones "de un solo lado", como la
    /// cristalización: Azoth -> Crystal pero el Crystal vecino no cambia).
    /// </summary>
    public struct Reaction
    {
        public byte a, b;
        public byte productA, productB;
        public byte chancePct;
        public short minTempRaw;
        public short maxTempRaw;

        public Reaction(byte a, byte b, byte productA, byte productB, byte chancePct, short minTempRaw = 0, short maxTempRaw = 255)
        {
            this.a = a;
            this.b = b;
            this.productA = productA;
            this.productB = productB;
            this.chancePct = chancePct;
            this.minTempRaw = minTempRaw;
            this.maxTempRaw = maxTempRaw;
        }
    }

    /// <summary>
    /// Tabla de reacciones "horneada" en un lookup plano [256*256] para
    /// consulta O(1) sin asignaciones en el hot-path de <see cref="SimStepper"/>.
    /// Se construye una única vez en <see cref="Universe.Create"/> a partir
    /// de la lista (pequeña, ~10 entradas) de <see cref="Reaction"/> de ese
    /// universo, y es de solo lectura a partir de ahí.
    /// </summary>
    public sealed class ReactionEngine
    {
        private readonly Reaction[] _reactions;
        // Índice (matA*256 + matB) -> índice en _reactions, o -1 si no hay reacción.
        // Simétrico: la misma reacción se registra en (a,b) y (b,a).
        private readonly short[] _lookup;

        public ReactionEngine(Reaction[] reactions)
        {
            _reactions = reactions ?? new Reaction[0];
            _lookup = new short[256 * 256];
            for (int i = 0; i < _lookup.Length; i++) _lookup[i] = -1;

            for (int i = 0; i < _reactions.Length; i++)
            {
                var r = _reactions[i];
                _lookup[r.a * 256 + r.b] = (short)i;
                _lookup[r.b * 256 + r.a] = (short)i;
            }
        }

        /// <summary>Cuántas reacciones de contacto tiene este universo. El índice [0, Count) es ESTABLE toda la partida.</summary>
        public int Count => _reactions.Length;

        /// <summary>La reacción en un índice estable. Para el diario, que necesita listar leyes que el jugador aún no ha visto.</summary>
        public Reaction At(int index) => _reactions[index];

        /// <summary>Busca una reacción registrada entre matA y matB (orden indiferente). No asigna memoria.</summary>
        public bool TryGet(byte matA, byte matB, out Reaction reaction)
        {
            short idx = _lookup[matA * 256 + matB];
            if (idx < 0)
            {
                reaction = default;
                return false;
            }
            reaction = _reactions[idx];
            return true;
        }

        /// <summary>
        /// (playtest 18) Como <see cref="TryGet(byte, byte, out Reaction)"/>,
        /// pero además devuelve el ÍNDICE ESTABLE de la reacción -- es lo que
        /// identifica QUÉ ley acaba de ocurrir para el evento SimEventType.Ley
        /// (ver SimStepper.TryReactNeighbor). La sobrecarga de 3 argumentos
        /// NO se toca: tiene llamantes vivos que no necesitan el índice.
        /// </summary>
        public bool TryGet(byte matA, byte matB, out Reaction reaction, out int index)
        {
            short idx = _lookup[matA * 256 + matB];
            if (idx < 0)
            {
                reaction = default;
                index = -1;
                return false;
            }
            reaction = _reactions[idx];
            index = idx;
            return true;
        }
    }
}
