namespace Alkahest.Sim
{
    /// <summary>Tipo de transformación notable registrada en el ring buffer de eventos de SimStepper.</summary>
    public enum SimEventType : byte
    {
        Ignite = 0,      // material -> Fire (visto arder)
        Boil = 1,        // transición genérica por boilsAt (incluye el "quemado" de Vivium -> Ash)
        Freeze = 2,       // transición genérica por freezesAt
        Crystallize = 3, // Azoth -> Crystal por contacto
        Grow = 4,        // nueva célula de Vivium creada por crecimiento
        Dissolve = 5,     // material disuelto por Acid
        /// <summary>(playtest 18) Una LEY acaba de ocurrir. `leyIndice` dice cuál. Es el único evento que identifica una ley.</summary>
        Ley = 6,
    }

    /// <summary>
    /// Evento "notable" (transformación interesante) de una celda, para que
    /// capas de más alto nivel (Game/SubstanceKnowledge) puedan reaccionar
    /// sin tener que hacer polling celda a celda. Struct plano, sin
    /// referencias, apto para un array fijo sin asignaciones.
    /// </summary>
    public struct SimNotableEvent
    {
        public byte matId;
        public SimEventType type;
        public short x;
        public short y;
        public uint tick;
        /// <summary>(playtest 18) Índice en Universe.Leyes cuando type == Ley. VALE -1 en todos los demás tipos.</summary>
        public short leyIndice;
    }
}
