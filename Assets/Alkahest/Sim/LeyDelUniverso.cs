namespace Alkahest.Sim
{
    /// <summary>
    /// QUÉ CLASE DE COSA hace una ley, no con qué materiales la hace. Es el eje
    /// que hace que dos semillas se SIENTAN distintas y no solo se vean distintas:
    /// un universo donde lo raro se propaga no se juega como uno donde lo raro
    /// se consume.
    /// </summary>
    public enum FormaDeLey : byte
    {
        /// <summary>A+B -> C+B. B es CATALIZADOR: no se gasta. (Es la forma de la cristalización del núcleo.)</summary>
        Transmutacion = 0,
        /// <summary>A+B -> C+C. Los dos reactivos se vuelven la misma cosa nueva.</summary>
        Fusion = 1,
        /// <summary>A+B -> Empty+C. A se destruye y B se transforma. (Es la forma del ácido del núcleo.)</summary>
        Consumo = 2,
        /// <summary>A+B -> C+gas (Smoke/Steam). Produce algo que SE VE SUBIR: la ley más fácil de presenciar de lejos.</summary>
        Liberacion = 3,
        /// <summary>A+B -> A+A. A se propaga comiéndose a B. La forma peligrosa; como mucho UNA por semilla (ver R5).</summary>
        Contagio = 4,
        /// <summary>No es una reacción de contacto: la ley de crecimiento del Vivium, que vive en SimStepper.GrowthTick.</summary>
        Crecimiento = 5,
    }

    /// <summary>Bajo qué temperatura ocurre una ley. Cualquiera = ocurre sin aparatos; Frio/Calor exigen piedra gélida o placa ígnea.</summary>
    public enum CondicionTermica : byte
    {
        Cualquiera = 0,
        Frio = 1,
        Calor = 2,
    }

    /// <summary>
    /// Una ley del universo tal y como la necesita la capa de juego (diario,
    /// banners de descubrimiento). Es un DESCRIPTOR, no la reacción ejecutable:
    /// la que ejecuta el stepper sigue siendo Reaction/ReactionEngine.
    ///
    /// (playtest 18) Nace junto con el sorteo de química por seed
    /// (<see cref="Universe.Create"/>): el núcleo fijo (7 reacciones que
    /// existen en TODA semilla) se describe con <see cref="esDelNucleo"/> a
    /// true; las leyes sorteadas para esta seed (5-8, gramática en
    /// CONTRATO_FASE3.md sección 6) lo llevan a false; la última entrada de
    /// <see cref="Universe.Leyes"/> (crecimiento del Vivium) también es
    /// núcleo, pero no pasa por <see cref="ReactionEngine"/> porque no es una
    /// reacción de contacto de dos celdas -- ver <see cref="FormaDeLey.Crecimiento"/>.
    /// </summary>
    public struct LeyDelUniverso
    {
        public byte a, b;                     // los dos reactivos, en el MISMO orden que la Reaction
        public byte productoA, productoB;     // productoA sustituye a `a`; productoB sustituye a `b`
        public FormaDeLey forma;
        public CondicionTermica condicion;
        public short minTempRaw, maxTempRaw;
        public byte chancePct;
        /// <summary>true = existe en TODA semilla (el núcleo fijo). false = sorteada para esta semilla.</summary>
        public bool esDelNucleo;
    }
}
