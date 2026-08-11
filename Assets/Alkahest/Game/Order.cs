namespace Alkahest.Game
{
    /// <summary>
    /// Criterio que debe cumplir una celda entregada en <see cref="DeliveryChute"/>
    /// para hacer avanzar un <see cref="Order"/>. Ver OrderSystem.MatchesOrder
    /// para el detalle exacto de cada criterio.
    /// </summary>
    public enum OrderType
    {
        /// <summary>El material es inflamable en este universo (def.flammable).</summary>
        Flammable,
        /// <summary>El material pertenece a la familia Vivium (def.archetype == Organic).</summary>
        Grows,
        /// <summary>La celda es Crystal (matId == MaterialId.Crystal).</summary>
        CrystalSolid,
        /// <summary>La celda está a MinTempC o más (°C).</summary>
        Hot,
        /// <summary>La celda está a MinTempC o menos (°C) -- mismo campo, reinterpretado como techo.</summary>
        Cold,
        /// <summary>La celda es exactamente TargetMat (solo se genera si hay algo bautizado y descubierto).</summary>
        NamedMaterial,
    }

    /// <summary>
    /// Un encargo del Maestro: entregar cierta cantidad de celdas que
    /// cumplan un criterio en <see cref="DeliveryChute"/>. Clase (no struct):
    /// <see cref="OrderSystem"/> guarda una lista de instancias y las muta en
    /// sitio (Progreso/Completado) conforme el jugador entrega material.
    /// </summary>
    public sealed class Order
    {
        public readonly int Id;
        public readonly string Descripcion;
        public readonly OrderType Tipo;
        public readonly int MinCells;
        public readonly int Recompensa;

        /// <summary>
        /// Umbral de temperatura en °C. Para <see cref="OrderType.Hot"/> es un
        /// mínimo (temp &gt;= MinTempC); para <see cref="OrderType.Cold"/> es,
        /// pese al nombre del campo, un MÁXIMO (temp &lt;= MinTempC). Null para
        /// el resto de tipos.
        /// </summary>
        public readonly int? MinTempC;

        /// <summary>Material objetivo exacto, solo relevante para <see cref="OrderType.NamedMaterial"/>.</summary>
        public readonly byte? TargetMat;

        public int Progreso;
        public bool Completado;

        public Order(int id, string descripcion, OrderType tipo, int minCells, int recompensa,
            int? minTempC = null, byte? targetMat = null)
        {
            Id = id;
            Descripcion = descripcion;
            Tipo = tipo;
            MinCells = minCells;
            Recompensa = recompensa;
            MinTempC = minTempC;
            TargetMat = targetMat;
        }
    }
}
