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
    }
}
