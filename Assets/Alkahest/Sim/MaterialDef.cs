using UnityEngine;

namespace Alkahest.Sim
{
    /// <summary>
    /// Arquetipo de comportamiento de un material. Determina qué reglas de
    /// <see cref="SimStepper"/> se aplican a las celdas de ese tipo.
    /// </summary>
    public enum MaterialArchetype : byte
    {
        Empty = 0,
        StaticSolid = 1,
        Powder = 2,
        Liquid = 3,
        Gas = 4,
        Fire = 5,
        Organic = 6,
    }

    /// <summary>
    /// Definición de datos de un material de la simulación. NO es un
    /// ScriptableObject a propósito: todos los materiales de un universo
    /// viven en un único array contiguo (<see cref="Universe.Materials"/>)
    /// para que el hot-path de simulación pueda indexarlos por byte sin
    /// pasar por el sistema de assets de Unity.
    ///
    /// Los sentinels short.MinValue / short.MaxValue en los campos de
    /// transición de fase ("meltsAt", "boilsAt", etc.) significan
    /// "esta transición nunca ocurre".
    /// </summary>
    public sealed class MaterialDef
    {
        public byte id;
        public string devName;
        public MaterialArchetype archetype;

        public Color32 baseColor;
        /// <summary>Rango (0-40) de variación estable de color por celda, usada por SimRenderer.</summary>
        public byte colorJitter;

        /// <summary>Densidad relativa. Usada para decidir quién se hunde/flota entre powders y liquids.</summary>
        public short density;

        /// <summary>Pasos de propagación horizontal (líquidos) o probabilidad de deslizar (powders). Rango sugerido 1..4.</summary>
        public byte fluidity;

        public bool flammable;
        /// <summary>Id del material en el que se convierte al arder (normalmente Fire).</summary>
        public byte burnsInto;
        /// <summary>Temperatura (raw, ver CellGrid.CToRaw) a partir de la cual puede autoignizar por contacto.</summary>
        public short ignitionTemp;

        // Sentinels de "nunca" para las transiciones de fase por temperatura.
        public short meltsAt = short.MaxValue;
        public byte meltsInto;
        public short freezesAt = short.MinValue;
        public byte freezesInto;
        public short boilsAt = short.MaxValue;
        public byte boilsInto;
        public short condensesAt = short.MinValue;
        public byte condensesInto;

        /// <summary>Vida media en ticks para gases/fuego. 0 = eterno (no expira).</summary>
        public byte gasLifetime;

        /// <summary>Si es true, SimRenderer le añade un resplandor/tinte adicional (fuego, brasas, Vivium).</summary>
        public bool emitsGlow;
    }
}
