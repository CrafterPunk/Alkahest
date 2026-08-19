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

        // =================================================================
        // (playtest 25, CONTRATO_PERSISTE.md §6.1) EL ARCO DE "LO QUE
        // PERSISTE": cinco tipos nuevos, exclusivos de
        // OrderSystem.GenerateOrdersPersiste() -- los seis de arriba siguen
        // intactos para el modo clásico (regla 26 de CLAUDE.md: nada se
        // borra, se añade al lado).
        // =================================================================

        /// <summary>
        /// N celdas del MISMO Polvo de una base×estado -- CUALQUIER base
        /// vale, pero la PRIMERA celda entregada fija cuál (ver
        /// <see cref="Order.LockedMat"/>): "una sola de sus arenas, pura",
        /// no una mezcla de bases distintas.
        /// </summary>
        Pureza,
        /// <summary>
        /// NO se resuelve en la Tolva (ver OrderSystem.MatchesOrder, que
        /// siempre devuelve false para este tipo): lo cumple
        /// <see cref="EnsayoMaestro"/> sometiendo la muestra al calor del
        /// crisol.
        /// </summary>
        AguantaCalor,
        /// <summary>Ídem AguantaCalor: se resuelve en <see cref="EnsayoMaestro"/>, nunca en la Tolva.</summary>
        Conduce,
        /// <summary>N celdas cuya densidad es menor que la del agua Y que no son solubles (Universe.SolubleEnAgua) -- por tabla, en la Tolva.</summary>
        FlotaInsoluble,
        /// <summary>
        /// Se autocompleta al entregar CUALQUIER celda mientras el jugador
        /// tenga ≥1 patente registrada (<see cref="Hornada.TieneAlMenosUnaPatente"/>):
        /// "el cómo, por escrito", no una sustancia concreta.
        /// </summary>
        Procedimiento,

        // =================================================================
        // (Encargo G, SEMILLA CERO, CONTRATO_SEMILLA.md §2) EL PEDIDO GUIADO.
        // =================================================================
        /// <summary>
        /// La celda es exactamente <see cref="Order.TargetMat"/> (idéntico criterio de
        /// coincidencia que <see cref="NamedMaterial"/>: matId == TargetMat.Value) pero
        /// SIN pasar por <see cref="OrderSystem.RefreshDescripciones"/> -- ese método solo
        /// recalcula Grows/CrystalSolid/NamedMaterial al rebautizar, y los pedidos de
        /// Semilla 0 llevan su texto EXACTO del guion (contrato §1), que
        /// <c>Game/SemillaCero.cs</c> construye y refresca él mismo cuando hace falta (p.
        /// ej. justo al bautizar, en el beat 3) -- dejar que el refresco automático lo
        /// pisara reescribiría la frase del Maestro con la plantilla genérica
        /// "Trae N celdas de lo que llamas...". Solo lo genera
        /// <see cref="OrderSystem.EncolarPedidoGuiado"/>, nunca la generación procedural.
        /// </summary>
        Guiado,
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

        /// <summary>
        /// (playtest 25) SOLO para <see cref="OrderType.Pureza"/>: la PRIMERA
        /// celda de polvo entregada fija qué base×estado exacto cuenta para
        /// el resto del pedido ("una sola de sus arenas, pura" -- no una
        /// mezcla). Null hasta la primera entrega válida; se fija en
        /// OrderSystem.TryDeliverCell, nunca en MatchesOrder (que es
        /// estático y no puede mutar nada). Mutable a propósito, como
        /// Progreso/Completado: esta clase ya es mutada en sitio por
        /// OrderSystem mientras el jugador entrega.
        /// </summary>
        public byte? LockedMat;

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
