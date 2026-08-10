namespace Alkahest.Sim
{
    /// <summary>
    /// RNG determinista y minúsculo (xorshift32) para uso exclusivo dentro
    /// de la simulación. La simulación NUNCA debe usar UnityEngine.Random:
    /// eso rompería el determinismo entre partidas con la misma seed y la
    /// misma secuencia de inputs.
    ///
    /// Se construye "por celda y por tick" a partir de una semilla derivada
    /// (tick, x, y) para que el resultado sea reproducible sin necesitar
    /// guardar estado de RNG persistente por celda.
    /// </summary>
    public struct XorShift
    {
        private uint _state;

        public XorShift(uint seed)
        {
            // xorshift no puede arrancar en 0.
            _state = seed == 0 ? 0x9E3779B9u : seed;
        }

        /// <summary>Crea un generador determinista a partir de coordenadas de simulación.</summary>
        public static XorShift FromCell(uint tick, int x, int y, uint salt = 0)
        {
            unchecked
            {
                uint h = tick * 747796405u + 2891336453u;
                h ^= (uint)(x * 374761393);
                h ^= (uint)(y * 668265263);
                h ^= salt * 2246822519u;
                h = (h ^ (h >> 15)) * 2246822519u;
                h = (h ^ (h >> 13)) * 3266489917u;
                h ^= (h >> 16);
                return new XorShift(h);
            }
        }

        private uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Byte pseudoaleatorio 0..255.</summary>
        public byte NextByte()
        {
            return (byte)(NextUInt() & 0xFF);
        }

        /// <summary>True con probabilidad pct/255 (aprox pct%).</summary>
        public bool Chance(byte pct255)
        {
            return NextByte() < pct255;
        }

        /// <summary>True con probabilidad aproximada percent/100.</summary>
        public bool ChancePercent(int percent)
        {
            if (percent <= 0) return false;
            if (percent >= 100) return true;
            return (NextUInt() % 100u) < (uint)percent;
        }

        /// <summary>Entero en [0, max).</summary>
        public int Next(int max)
        {
            if (max <= 1) return 0;
            return (int)(NextUInt() % (uint)max);
        }

        /// <summary>Bool con 50% de probabilidad.</summary>
        public bool NextBool()
        {
            return (NextUInt() & 1u) == 1u;
        }
    }
}
