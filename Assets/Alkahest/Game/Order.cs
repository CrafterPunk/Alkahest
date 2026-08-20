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

        // =================================================================
        // (ronda 56, LA VIDA ÚTIL DE LO DESCUBIERTO, CONTRATO_RONDA56.md §1a)
        // EL ENCARGO COMPUESTO -- decisión de implementación: TRES ORDERS
        // HERMANAS (una por componente) que comparten <see cref="GrupoId"/>,
        // en vez de una subclase o un arreglo interno. Cada hermana es un
        // Order de <see cref="OrderType.Guiado"/> normal y corriente
        // (mismo criterio de matching que ya usa Semilla Cero: matId exacto
        // == TargetMat) -- es la opción que MENOS pelea con
        // OrderSystem.TryDeliverCell/MatchesOrder existentes, que el
        // contrato pedía explícitamente: no hace falta tocar NINGUNA de las
        // dos, ambas siguen viendo tres Orders sueltas. La única pieza nueva
        // es este grupo de campos, null en TODO Order clásico -- ver
        // OrderSystem.EncolarCompuesto/AvanzarGrupoCompuestoSiToca para
        // quién los llena y quién los consume.
        // =================================================================

        /// <summary>Id estable del compuesto ("vitrales_capilla", "obra_mufla") al que pertenece esta línea, o null si es un encargo normal.</summary>
        public readonly string GrupoId;

        /// <summary>Nombre corto del compuesto ("LOS VITRALES DE LA CAPILLA"), repetido en las 3 hermanas -- cualquiera basta como título del bloque en OrdersHud. Null si <see cref="GrupoId"/> es null.</summary>
        public readonly string GrupoNombreCorto;

        /// <summary>Texto narrativo largo VERBATIM del diseño, repetido en las 3 hermanas (se pinta una sola vez por bloque). Null si <see cref="GrupoId"/> es null.</summary>
        public readonly string GrupoTextoLargo;

        /// <summary>Etiqueta corta del componente para la fila del checklist ("vidrio de botella", "barbotina", "mortero" -- etiqueta de OFICIO fijada por el encargo, no sale de SubstanceKnowledge.NombreDe). Null si <see cref="GrupoId"/> es null.</summary>
        public readonly string GrupoEtiqueta;

        /// <summary>Favor TOTAL del compuesto (60/40, ver CONTRATO_RONDA56.md §0), repetido en las 3 hermanas para que OrdersHud lo muestre en la cabecera del bloque sin ir a preguntarle a OrderSystem. 0 si <see cref="GrupoId"/> es null (Recompensa por línea ya es 0 también -- ver OrderSystem.AddOrderCompuesto).</summary>
        public readonly int GrupoRecompensaTotal;

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
            int? minTempC = null, byte? targetMat = null,
            string grupoId = null, string grupoNombreCorto = null, string grupoTextoLargo = null,
            string grupoEtiqueta = null, int grupoRecompensaTotal = 0)
        {
            Id = id;
            Descripcion = descripcion;
            Tipo = tipo;
            MinCells = minCells;
            Recompensa = recompensa;
            MinTempC = minTempC;
            TargetMat = targetMat;
            GrupoId = grupoId;
            GrupoNombreCorto = grupoNombreCorto;
            GrupoTextoLargo = grupoTextoLargo;
            GrupoEtiqueta = grupoEtiqueta;
            GrupoRecompensaTotal = grupoRecompensaTotal;
        }
    }
}
