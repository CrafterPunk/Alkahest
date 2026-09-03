namespace Alkahest.Sim
{
    /// <summary>
    /// (R130, LABORATORIO DE LEYES) Las clasificaciones de material que el
    /// laboratorio necesita y que los sistemas de juego consultan (colisión
    /// del muñeco, cincel, frasco) sin tener que conocer los ids nuevos uno
    /// por uno. Puro C#, sin API de Unity (compila en el banco headless).
    /// Los ids se enumeran en MaterialId (Universe.cs); la física en
    /// SimStepper.Laboratorio.cs; los parámetros en LabParams.
    /// </summary>
    public static class LabMateriales
    {
        /// <summary>Sólidos que el MUNDO ofrece como pared/suelo: bloquean al muñeco y cortan la línea de visión del cincel. Piedra y piso siempre; los del laboratorio solo existen si alguien los pintó.</summary>
        public static bool EsSolidoDelMundo(byte m)
        {
            switch (m)
            {
                case MaterialId.Stone:
                case MaterialId.PisoEstructural:
                case MaterialId.Arcilla:
                case MaterialId.Terracota:
                case MaterialId.Hogar:
                case MaterialId.NucleoFrio:
                case MaterialId.Manantial:
                case MaterialId.Sumidero:
                case MaterialId.RocaSuelta:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Lo que el cincel puede DESPRENDER. Hogar/Manantial/Sumidero/NúcleoFrío son las leyes del lugar: no ceden.</summary>
        public static bool Tallable(byte m)
        {
            switch (m)
            {
                case MaterialId.Stone:
                case MaterialId.PisoEstructural:
                case MaterialId.Arcilla:
                case MaterialId.Terracota:
                case MaterialId.RocaSuelta:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Qué deja el cincel al tallar. La roca madre no rinde nada (regla de hierro R60: excavar no da materiales a granel); la ARCILLA sí se desprende como Sedimento (tallar barro te da barro: es lo que el propio sedimento formó), y lo cocido/suelto se rompe en Grava.</summary>
        public static byte ProductoDeTalla(byte m)
        {
            switch (m)
            {
                case MaterialId.Arcilla: return MaterialId.Sedimento;
                case MaterialId.Terracota: return MaterialId.Grava;
                case MaterialId.RocaSuelta: return MaterialId.Grava;
                default: return MaterialId.Empty;
            }
        }

        /// <summary>Permeabilidad 0..255 (0 = no poroso). Lee LabParams para que el panel la afine en vivo.</summary>
        public static int Permeabilidad(byte m)
        {
            switch (m)
            {
                case MaterialId.Sand: return LabParams.PermArena;
                case MaterialId.Grava: return LabParams.PermGrava;
                case MaterialId.Sedimento: return LabParams.PermSedimento;
                case MaterialId.Ash: return LabParams.PermCeniza;
                case MaterialId.Fibra: return LabParams.PermFibra;
                case MaterialId.Arcilla: return LabParams.PermArcilla;
                default: return 0;
            }
        }

        public static bool EsPoroso(byte m) => Permeabilidad(m) > 0;

        /// <summary>Finos (capilaridad hacia arriba): sedimento, arcilla, ceniza.</summary>
        public static bool EsFino(byte m) => m == MaterialId.Sedimento || m == MaterialId.Arcilla || m == MaterialId.Ash;

        /// <summary>Roca impermeable donde el vapor condensa como ROCÍO que gotea.</summary>
        public static bool EsRocaImpermeable(byte m) => m == MaterialId.Stone || m == MaterialId.Terracota || m == MaterialId.PisoEstructural || m == MaterialId.RocaSuelta;

        /// <summary>Fondo sobre el que el agua turbia puede DEPOSITAR sedimento (cualquier cosa que no sea aire, gas ni líquido).</summary>
        public static bool EsFondo(byte m) => m != MaterialId.Empty && m != MaterialId.Water && !EsGasId(m);

        /// <summary>Lo que el agua en movimiento puede EROSIONAR. La arcilla, con probabilidad aparte.</summary>
        public static bool EsErosionable(byte m) => m == MaterialId.Sedimento || m == MaterialId.Arcilla;

        /// <summary>Sustrato donde una planta puede arraigar.</summary>
        public static bool EsSustrato(byte m) => m == MaterialId.Sedimento || m == MaterialId.Sand || m == MaterialId.Ash;

        /// <summary>Ids de gas del roster fijo (Steam/Smoke) sin pasar por Universe: para el hot path de las pasadas.</summary>
        public static bool EsGasId(byte m) => m == MaterialId.Steam || m == MaterialId.Smoke;

        /// <summary>Fuente de luz propia (para LabLuz): fuego vivo, brasa y hogar.</summary>
        public static bool EmiteLuz(byte m) => m == MaterialId.Fire || m == MaterialId.Brasa || m == MaterialId.Hogar;
    }
}
