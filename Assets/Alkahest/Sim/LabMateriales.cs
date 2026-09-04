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
                case MaterialId.Arenisca:
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
                case MaterialId.Arenisca:
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
                case MaterialId.Arenisca: return MaterialId.Sand; // (R131) arena cementada: el cincel la devuelve a arena suelta.
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
                case MaterialId.Arenisca: return LabParams.PermArenisca;
                case MaterialId.Carbon: return LabParams.PermCarbon;
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

        /// <summary>
        /// (R140) Nombre en castellano de CUALQUIER material, para que en el laboratorio no quede
        /// nada sin nombrar: lo que se ve tiene que poder decirse.
        ///
        /// Primero pregunta al juego (`Universe.NombreReal`), así que lo que ya tiene nombre real
        /// lo conserva y no se inventa un segundo — el vidrio del horno se sigue llamando «vidrio
        /// de botella» aquí y en la campaña. La tabla de abajo solo cubre el hueco, que son dos
        /// grupos: los quince materiales del laboratorio (ids 65-79), que no están en la tabla del
        /// juego y salían con su `devName` en inglés; y los INNOMINADOS (aceite, limo, azoth…),
        /// que en la campaña salen como «???» **por diseño** —regla 13/23: se bautizan jugando— y
        /// ese silencio hay que respetarlo allí. Pero el laboratorio es un banco de trabajo, no
        /// una partida: aquí quien mira es el investigador, no el aprendiz, y necesita poder leer
        /// la grilla. Por eso esta tabla nombra por lo que la cosa ES, sin tocar el bautizo.
        /// </summary>
        public static string Nombre(byte m)
        {
            if (m < MaterialId.Count && Universe.TieneIdentidadReal(m)) return Universe.NombreReal(m);
            switch (m)
            {
                case MaterialId.Empty:           return "aire";
                case MaterialId.Oil:             return "aceite";
                case MaterialId.Slime:           return "limo";
                case MaterialId.Nutrient:        return "nutriente";
                case MaterialId.Vivium:          return "vivium";
                case MaterialId.Azoth:           return "azoth";
                case MaterialId.CrystalSeed:     return "semilla de cristal";
                case MaterialId.Crystal:         return "cristal";
                case MaterialId.Acid:            return "ácido";
                // El retículo base x estado (ids 18-57) va por aritmética y no tiene constante
                // propia por celda; de los cuarenta, este es el único sin identidad real.
                case (byte)(MaterialId.BaseEstado0 + 7): return "base en solución";
                // --- los quince del laboratorio (65-79) ---
                case MaterialId.PisoEstructural: return "piso estructural";
                case MaterialId.Sedimento:       return "sedimento";
                case MaterialId.Arcilla:         return "arcilla";
                case MaterialId.Terracota:       return "terracota";
                case MaterialId.Grava:           return "grava";
                case MaterialId.Planta:          return "planta";
                case MaterialId.Fibra:           return "fibra";
                case MaterialId.Hogar:           return "hogar";
                case MaterialId.NucleoFrio:      return "núcleo frío";
                case MaterialId.Manantial:       return "manantial";
                case MaterialId.Sumidero:        return "sumidero";
                case MaterialId.RocaSuelta:      return "roca suelta";
                case MaterialId.Semilla:         return "semilla";
                case MaterialId.Arenisca:        return "arenisca de laboratorio";
                case MaterialId.Carbon:          return "carbón";
                default:                         return "material " + m;
            }
        }

        /// <summary>
        /// (R140) El ESTADO de una celda dicho en palabras, no en números: lo que la distingue de
        /// otra del mismo material. Un sedimento empapado y uno seco son la misma `mat` y dos
        /// cosas distintas para el mundo, y hasta ahora la diferencia solo se veía cambiando a la
        /// vista de humedad. Devuelve null si no hay nada que destacar.
        /// </summary>
        public static string Estado(byte m, byte humedad, byte carga)
        {
            if (m == MaterialId.Water) return carga >= 128 ? "muy turbia" : carga >= 40 ? "turbia" : "limpia";
            if (m == MaterialId.Empty || EsGasId(m))
                return humedad >= 200 ? "saturado de vapor" : humedad >= 120 ? "húmedo"
                     : humedad >= 40 ? "algo húmedo" : "seco";
            if (EsPoroso(m))
            {
                string h = humedad >= 230 ? "encharcado" : humedad >= 150 ? "empapado"
                         : humedad >= 60 ? "húmedo" : humedad >= 20 ? "apenas húmedo" : "seco";
                if (carga >= 128) return h + ", muy fértil";
                if (carga >= 40) return h + ", fértil";
                return h;
            }
            if (m == MaterialId.Planta) return humedad >= 120 ? "con savia" : humedad >= 40 ? "poca savia" : "marchitándose";
            if (humedad >= 150) return "con rocío";
            return null;
        }
    }
}
