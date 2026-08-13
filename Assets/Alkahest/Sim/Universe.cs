using System;
using System.Collections.Generic;
using UnityEngine;

namespace Alkahest.Sim
{
    /// <summary>
    /// Ids estables de materiales del roster base. Usar estas constantes en
    /// vez de números mágicos en cualquier sitio que necesite referenciar un
    /// material concreto (SimStepper, DevPalette, SimLevelBuilder, etc.).
    /// El orden/valor NO debe cambiar entre versiones para no romper saves
    /// o niveles ya serializados.
    /// </summary>
    public static class MaterialId
    {
        public const byte Empty = 0;
        public const byte Stone = 1;
        public const byte Sand = 2;
        public const byte Water = 3;
        public const byte Oil = 4;
        public const byte Slime = 5;
        public const byte Steam = 6;
        public const byte Smoke = 7;
        public const byte Fire = 8;
        public const byte Ash = 9;
        public const byte Ice = 10;
        public const byte Nutrient = 11;
        public const byte Vivium = 12;

        // Roster M3 ("leyes del universo"): el líquido extraño de la run,
        // su pareja de cristalización, y el disolvente ácido.
        public const byte Azoth = 13;
        public const byte CrystalSeed = 14;
        public const byte Crystal = 15;
        public const byte Acid = 16;

        public const int Count = 17;
    }

    /// <summary>
    /// Torsión global nombrable de la run ("Edicto"), elegida deterministamente
    /// a partir de la seed en <see cref="Universe.Create"/>. Ver
    /// <see cref="Universe.EdictoDescripcion"/> para el texto de rumor mostrado
    /// al jugador.
    /// </summary>
    public enum Edicto
    {
        FrioFertil = 0,
        MateriaIrascible = 1,
        DensidadInvertida = 2,
    }

    /// <summary>
    /// Un "universo" es el conjunto de leyes/materiales de una partida
    /// concreta, derivado de una seed. Cada `Universe.Create(seed)`:
    ///  (a) baraja, DENTRO de rangos acotados por arquetipo, las propiedades
    ///      de los materiales "variables" (densidad de líquidos, qué material
    ///      es inflamable este run, temperaturas de ignición/fusión/ebullición,
    ///      banda de crecimiento de Vivium) usando un System.Random(seed)
    ///      LOCAL, usado solo en la creación (nunca durante el tick a tick,
    ///      nunca UnityEngine.Random);
    ///  (b) elige 1 de 3 "Edictos" (torsiones globales adicionales) y hornea
    ///      su efecto en los mismos valores;
    ///  (c) hornea la tabla de reacciones de contacto en un <see cref="ReactionEngine"/>.
    /// El resto del código (Sim, Game, Dev, Editor) SIEMPRE lee materiales y
    /// leyes a través de esta clase, nunca hardcodea densidades/temperaturas
    /// en otro sitio.
    /// </summary>
    public sealed class Universe
    {
        public readonly int Seed;
        public readonly MaterialDef[] Materials;
        public readonly ReactionEngine Reactions;

        // ---- Leyes de crecimiento/cristalización de esta run (bakeadas, ver Create) ----
        /// <summary>Banda de temperatura raw en la que Vivium crece consumiendo Nutrient.</summary>
        public readonly byte VivGrowMinRaw;
        public readonly byte VivGrowMaxRaw;
        /// <summary>Probabilidad de que consumir un Nutrient en banda cree una nueva célula de Vivium (si no, solo se consume).</summary>
        public readonly byte VivGrowChancePct = 60;

        /// <summary>Umbral de temperatura raw por DEBAJO del cual Azoth cristaliza al tocar Crystal/CrystalSeed.</summary>
        public readonly byte CrystallizeMaxTempRaw;
        public readonly byte CrystallizeChancePct;

        public readonly Edicto ActiveEdicto;
        /// <summary>Texto de rumor en español mostrado al jugador (DayCycle, log de arranque). Nunca revela mecánicamente las leyes, solo insinúa.</summary>
        public readonly string EdictoDescripcion;

        // -----------------------------------------------------------------
        // FIRMA VISUAL DEL UNIVERSO (playtest 12, reporte "más de lo mismo").
        // Ver el bloque grande en Create() para el sorteo; aquí solo se
        // cachea el resultado (nunca se construyen estas cadenas por frame).
        // -----------------------------------------------------------------
        /// <summary>
        /// Frase corta (idioma del Maestro) que resume el carácter visual de
        /// esta run: tono dominante + rasgo morfológico más repetido entre lo
        /// innominado. NUNCA nombra sustancias (siguen siendo "???" hasta que
        /// el jugador las bautiza, ver CLAUDE.md regla 13) — describe el
        /// "clima" del universo, no su contenido.
        /// </summary>
        public readonly string CaracterDelUniverso;

        /// <summary>Descripción corta cacheada de la firma visual por material ("granate, manchas lentas, borde escarchado"), indexada por id. Consumida por el diario.</summary>
        private readonly string[] _firmaPorMaterial;

        private Universe(int seed, MaterialDef[] materials, ReactionEngine reactions,
            byte vivGrowMinRaw, byte vivGrowMaxRaw, byte crystallizeMaxTempRaw, byte crystallizeChancePct,
            Edicto edicto, string edictoDescripcion, string caracterDelUniverso, string[] firmaPorMaterial)
        {
            Seed = seed;
            Materials = materials;
            Reactions = reactions;
            VivGrowMinRaw = vivGrowMinRaw;
            VivGrowMaxRaw = vivGrowMaxRaw;
            CrystallizeMaxTempRaw = crystallizeMaxTempRaw;
            CrystallizeChancePct = crystallizeChancePct;
            ActiveEdicto = edicto;
            EdictoDescripcion = edictoDescripcion;
            CaracterDelUniverso = caracterDelUniverso;
            _firmaPorMaterial = firmaPorMaterial;
        }

        public MaterialDef Get(byte id) => Materials[id];

        /// <summary>Descripción corta y cacheada de la firma visual de un material ("granate, manchas lentas, borde escarchado"). Usada por el diario; nunca se construye por frame.</summary>
        public string DescribirFirma(byte matId)
        {
            return matId < _firmaPorMaterial.Length ? _firmaPorMaterial[matId] : string.Empty;
        }

        /// <summary>
        /// Crea un universo determinista a partir de una seed. Un
        /// System.Random local (NUNCA UnityEngine.Random, y nunca
        /// almacenado para uso posterior) decide tanto el jitter de color
        /// como TODA la variación de leyes de este run.
        /// </summary>
        public static Universe Create(int seed)
        {
            var rng = new System.Random(seed);
            var mats = new MaterialDef[MaterialId.Count];

            // -----------------------------------------------------------------
            // 1) Elegir el Edicto de este run primero: sus efectos se aplican
            //    como ajustes adicionales sobre la variación "base" calculada
            //    más abajo.
            // -----------------------------------------------------------------
            var edicto = (Edicto)rng.Next(3);

            // -----------------------------------------------------------------
            // 2) Densidad de líquidos: 5 anclas de densidad evenly-spaced,
            //    repartidas en orden aleatorio entre los 5 líquidos del roster
            //    (+jitter pequeño por líquido) para que la estratificación
            //    (quién flota sobre quién) varíe de verdad entre runs y no
            //    sea siempre "Oil < Water < Slime". DensidadInvertida invierte
            //    el reparto resultante.
            // -----------------------------------------------------------------
            byte[] densitySlots = { 40, 75, 110, 145, 180 };
            byte[] liquidIds = { MaterialId.Oil, MaterialId.Acid, MaterialId.Water, MaterialId.Azoth, MaterialId.Slime };
            int[] order = { 0, 1, 2, 3, 4 };
            ShuffleFisherYates(order, rng);
            if (edicto == Edicto.DensidadInvertida) Array.Reverse(order);

            var liquidDensity = new short[MaterialId.Count];
            for (int i = 0; i < liquidIds.Length; i++)
            {
                int jitter = rng.Next(-6, 7);
                liquidDensity[liquidIds[i]] = (short)Mathf.Clamp(densitySlots[order[i]] + jitter, 1, 240);
            }

            // -----------------------------------------------------------------
            // 3) Inflamabilidad: 1 de cada 3 runs, ni Slime ni Azoth arden;
            //    si no, exactamente uno de los dos es inflamable este run.
            // -----------------------------------------------------------------
            int flamPick = rng.Next(3); // 0 = ninguno, 1 = Slime, 2 = Azoth
            bool slimeFlammable = flamPick == 1;
            bool azothFlammable = flamPick == 2;

            // -----------------------------------------------------------------
            // 4) Temperaturas de ignición ±20% (adicional -30% bajo Materia
            //    Irascible), fusión/ebullición del agua ±15 grados, banda de
            //    crecimiento de Vivium ±15 grados (adicional -20 bajo Frío
            //    Fértil), vida del fuego +50% bajo Materia Irascible.
            // -----------------------------------------------------------------
            float irascibleIgnitionBonus = edicto == Edicto.MateriaIrascible ? -0.30f : 0f;

            float oilIgnitionFrac = RandRange(rng, -0.20f, 0.20f) + irascibleIgnitionBonus;
            int oilIgnitionC = Mathf.RoundToInt(260f * (1f + oilIgnitionFrac));

            float altIgnitionFrac = RandRange(rng, -0.20f, 0.20f) + irascibleIgnitionBonus;
            int altIgnitionC = Mathf.RoundToInt(240f * (1f + altIgnitionFrac)); // Slime/Azoth arden algo más fácil que el aceite si les toca ser inflamables.

            byte fireLifetime = edicto == Edicto.MateriaIrascible
                ? (byte)Mathf.Clamp(Mathf.RoundToInt(80f * 1.5f), 1, 255)
                : (byte)80;

            float freezeShiftC = RandRange(rng, -15f, 15f);
            float boilShiftC = RandRange(rng, -15f, 15f);
            int waterFreezeC = Mathf.Clamp(Mathf.RoundToInt(0f + freezeShiftC), -20, 15);
            int waterBoilC = Mathf.Clamp(Mathf.RoundToInt(100f + boilShiftC), 80, 118);

            float growShiftC = RandRange(rng, -15f, 15f);
            if (edicto == Edicto.FrioFertil) growShiftC -= 20f;
            int growMinC = Mathf.RoundToInt(30f + growShiftC);
            int growMaxC = Mathf.RoundToInt(60f + growShiftC);
            if (growMaxC <= growMinC) growMaxC = growMinC + 10;

            // -----------------------------------------------------------------
            // 5) Cristalización: umbral frío por defecto ~5°C, 12% de
            //    probabilidad/comprobación; Frío Fértil la hace más laxa/rápida.
            // -----------------------------------------------------------------
            int crystallizeThresholdC = 5;
            int crystallizeChance = 12;
            if (edicto == Edicto.FrioFertil)
            {
                crystallizeThresholdC += 15;
                crystallizeChance += 15;
            }
            byte crystallizeMaxTempRaw = CellGrid.CToRaw(crystallizeThresholdC);
            byte crystallizeChancePct = (byte)Mathf.Clamp(crystallizeChance, 1, 100);

            // -----------------------------------------------------------------
            // Roster de materiales.
            // -----------------------------------------------------------------
            mats[MaterialId.Empty] = new MaterialDef
            {
                id = MaterialId.Empty,
                devName = "Empty",
                archetype = MaterialArchetype.Empty,
                baseColor = new Color32(0, 0, 0, 0),
                colorJitter = 0,
                density = 0,
            };

            mats[MaterialId.Stone] = new MaterialDef
            {
                id = MaterialId.Stone,
                devName = "Stone",
                archetype = MaterialArchetype.StaticSolid,
                // (pase visual M5) Antes gris neutro 110: competía con la arena y
                // con la ceniza, y el taller entero se veía "lavado". Ahora es un
                // gris más oscuro y emparentado con el ciruela del fondo: la
                // arquitectura RETROCEDE y los materiales del jugador destacan.
                baseColor = new Color32(92, 86, 98, 255),
                colorJitter = 14,
                density = short.MaxValue,
            };

            mats[MaterialId.Sand] = new MaterialDef
            {
                id = MaterialId.Sand,
                devName = "Sand",
                archetype = MaterialArchetype.Powder,
                baseColor = new Color32(214, 186, 121, 255),
                colorJitter = 18,
                density = 180,
                fluidity = 1,
            };

            mats[MaterialId.Water] = new MaterialDef
            {
                id = MaterialId.Water,
                devName = "Water",
                archetype = MaterialArchetype.Liquid,
                baseColor = new Color32(64, 118, 214, 200),
                colorJitter = 10,
                density = liquidDensity[MaterialId.Water],
                fluidity = 4,
                freezesAt = CellGrid.CToRaw(waterFreezeC),
                freezesInto = MaterialId.Ice,
                boilsAt = CellGrid.CToRaw(waterBoilC),
                boilsInto = MaterialId.Steam,
            };

            mats[MaterialId.Oil] = new MaterialDef
            {
                id = MaterialId.Oil,
                devName = "Oil",
                archetype = MaterialArchetype.Liquid,
                baseColor = new Color32(84, 58, 20, 220),
                colorJitter = 12,
                density = liquidDensity[MaterialId.Oil],
                fluidity = 3,
                flammable = true,
                ignitionTemp = CellGrid.CToRaw(oilIgnitionC),
                burnsInto = MaterialId.Fire,
            };

            mats[MaterialId.Slime] = new MaterialDef
            {
                id = MaterialId.Slime,
                devName = "Slime",
                archetype = MaterialArchetype.Liquid,
                baseColor = new Color32(84, 196, 96, 235),
                colorJitter = 16,
                density = liquidDensity[MaterialId.Slime],
                fluidity = 1,
                flammable = slimeFlammable,
                ignitionTemp = slimeFlammable ? CellGrid.CToRaw(altIgnitionC) : short.MaxValue,
                burnsInto = MaterialId.Fire,
            };

            mats[MaterialId.Steam] = new MaterialDef
            {
                id = MaterialId.Steam,
                devName = "Steam",
                archetype = MaterialArchetype.Gas,
                baseColor = new Color32(224, 228, 232, 130),
                colorJitter = 10,
                density = -50,
                gasLifetime = 60, // ~300 ticks / 5 -> ver nota abajo
                condensesAt = CellGrid.CToRaw(waterBoilC - 40),
                condensesInto = MaterialId.Water,
            };

            mats[MaterialId.Smoke] = new MaterialDef
            {
                id = MaterialId.Smoke,
                devName = "Smoke",
                archetype = MaterialArchetype.Gas,
                baseColor = new Color32(58, 54, 58, 180),
                colorJitter = 14,
                density = -60,
                gasLifetime = 200,
            };

            mats[MaterialId.Fire] = new MaterialDef
            {
                id = MaterialId.Fire,
                devName = "Fire",
                archetype = MaterialArchetype.Fire,
                baseColor = new Color32(255, 140, 40, 255),
                colorJitter = 30,
                density = -80,
                gasLifetime = fireLifetime,
                emitsGlow = true,
            };

            mats[MaterialId.Ash] = new MaterialDef
            {
                id = MaterialId.Ash,
                devName = "Ash",
                archetype = MaterialArchetype.Powder,
                // (pase visual M5) Era (72,66,68), casi idéntico al humo
                // (58,54,58): un montón de ceniza parecía humo posado. Ahora tira
                // a pardo cálido de brasa apagada, distinguible de humo y piedra.
                baseColor = new Color32(88, 74, 64, 255),
                colorJitter = 16,
                density = 120,
                fluidity = 1,
            };

            mats[MaterialId.Ice] = new MaterialDef
            {
                id = MaterialId.Ice,
                devName = "Ice",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(196, 226, 236, 235),
                colorJitter = 10,
                density = short.MaxValue,
                meltsAt = CellGrid.CToRaw(waterFreezeC + 5), // pequeña histéresis respecto al punto de congelación del agua.
                meltsInto = MaterialId.Water,
            };

            mats[MaterialId.Nutrient] = new MaterialDef
            {
                id = MaterialId.Nutrient,
                devName = "Nutrient",
                archetype = MaterialArchetype.Powder,
                baseColor = new Color32(120, 78, 96, 255),
                colorJitter = 20,
                density = 140,
                fluidity = 1,
                flammable = true, // (fix playtest) materia orgánica: arde de forma satisfactoria
                ignitionTemp = CellGrid.CToRaw(180),
                burnsInto = MaterialId.Fire,
            };

            mats[MaterialId.Vivium] = new MaterialDef
            {
                id = MaterialId.Vivium,
                devName = "Vivium",
                archetype = MaterialArchetype.Organic,
                baseColor = new Color32(60, 220, 196, 255),
                colorJitter = 12,
                density = 170,
                fluidity = 0, // No se mueve una vez asentado.
                flammable = true, // (fix playtest) el coral vivo arde — cuida tu cultivo
                ignitionTemp = CellGrid.CToRaw(150),
                burnsInto = MaterialId.Fire,
                emitsGlow = true,
                // Reutiliza el mecanismo genérico de fase (boilsAt) como "muere quemado":
                // por encima de ~120°C se convierte en Ash. Umbral fijo (no varía por seed).
                boilsAt = CellGrid.CToRaw(120),
                boilsInto = MaterialId.Ash,
            };

            mats[MaterialId.Azoth] = new MaterialDef
            {
                id = MaterialId.Azoth,
                devName = "Azoth",
                archetype = MaterialArchetype.Liquid,
                baseColor = new Color32(150, 90, 210, 215),
                colorJitter = 22,
                density = liquidDensity[MaterialId.Azoth],
                fluidity = 3,
                flammable = azothFlammable,
                ignitionTemp = azothFlammable ? CellGrid.CToRaw(altIgnitionC) : short.MaxValue,
                burnsInto = MaterialId.Fire,
                emitsGlow = true,
            };

            mats[MaterialId.CrystalSeed] = new MaterialDef
            {
                id = MaterialId.CrystalSeed,
                devName = "CrystalSeed",
                archetype = MaterialArchetype.Powder,
                baseColor = new Color32(206, 196, 226, 255),
                colorJitter = 18,
                density = 210,
                fluidity = 1,
            };

            mats[MaterialId.Crystal] = new MaterialDef
            {
                id = MaterialId.Crystal,
                devName = "Crystal",
                archetype = MaterialArchetype.StaticSolid,
                // (pase visual M5) Era (170,220,235), casi el mismo pálido que el
                // hielo (196,226,236): imposible saber si habías fabricado cristal
                // o simplemente congelado agua — y el cristal es lo que piden los
                // encargos de las jornadas 2 y 3. Placeholder violeta-cian y brillo;
                // el color REAL de cada run lo decide SortearFirmasVisuales más
                // abajo (playtest 12): ya NO hereda el tono de Azoth a propósito
                // — la separación de tono entre las 6 sustancias innominadas exige
                // repartirlas por el círculo cromático entero, y el jugador de
                // todas formas aprende la relación Azoth→Cristal viendo la reacción
                // de cristalización, no por parecido de color.
                baseColor = new Color32(152, 172, 255, 255),
                colorJitter = 10,
                density = short.MaxValue,
                emitsGlow = true,
                // "Se hace añicos" bajo calor de fuego real (reutiliza meltsAt/meltsInto genérico).
                meltsAt = CellGrid.CToRaw(300),
                meltsInto = MaterialId.CrystalSeed,
            };

            mats[MaterialId.Acid] = new MaterialDef
            {
                id = MaterialId.Acid,
                devName = "Acid",
                archetype = MaterialArchetype.Liquid,
                baseColor = new Color32(182, 204, 46, 220),
                colorJitter = 14,
                density = liquidDensity[MaterialId.Acid],
                fluidity = 4,
            };

            // -----------------------------------------------------------------
            // FIRMA VISUAL (playtest 12): sortea patrón/borde/color de LO
            // INNOMINADO y deja el vocabulario del taller con su color EXACTO
            // e intacto (sin jitter siquiera) — ver SortearFirmasVisuales.
            // Antes había aquí un jitter de hue de ±8° aplicado a TODOS los
            // materiales, vocabulario incluido: eso contradice el reporte del
            // playtest 12 ("solo tuve más de lo mismo") por el lado contrario
            // — hacía que el suelo firme del jugador (agua, arena...) también
            // se moviera un poco entre partidas. Se retira a propósito: el
            // vocabulario del taller debe leerse IDÉNTICO en toda seed.
            SortearFirmasVisuales(mats, rng, out string caracterDelUniverso, out string[] firmaPorMaterial);

            // -----------------------------------------------------------------
            // Tabla de reacciones de contacto (ver ReactionEngine/SimStepper).
            // -----------------------------------------------------------------
            const byte acidDissolveChancePct = 40;
            const byte acidNeutralizeChancePct = 20;
            var reactions = new[]
            {
                // Cristalización: Azoth + Crystal/CrystalSeed, en frío -> Azoth se vuelve Crystal.
                // El vecino (b) no cambia: productB == b.
                new Reaction(MaterialId.Azoth, MaterialId.Crystal, MaterialId.Crystal, MaterialId.Crystal,
                    crystallizeChancePct, 0, crystallizeMaxTempRaw),
                new Reaction(MaterialId.Azoth, MaterialId.CrystalSeed, MaterialId.Crystal, MaterialId.CrystalSeed,
                    crystallizeChancePct, 0, crystallizeMaxTempRaw),

                // Ácido disuelve Sand/Ash/Ice/Crystal (NO Stone, por seguridad de nivel):
                // el ácido se consume (-> Empty) y el objetivo se convierte en Smoke.
                new Reaction(MaterialId.Acid, MaterialId.Sand, MaterialId.Empty, MaterialId.Smoke, acidDissolveChancePct),
                new Reaction(MaterialId.Acid, MaterialId.Ash, MaterialId.Empty, MaterialId.Smoke, acidDissolveChancePct),
                new Reaction(MaterialId.Acid, MaterialId.Ice, MaterialId.Empty, MaterialId.Smoke, acidDissolveChancePct),
                new Reaction(MaterialId.Acid, MaterialId.Crystal, MaterialId.Empty, MaterialId.Smoke, acidDissolveChancePct),

                // Ácido neutralizado por Agua: ambos -> Slime.
                new Reaction(MaterialId.Acid, MaterialId.Water, MaterialId.Slime, MaterialId.Slime, acidNeutralizeChancePct),
            };
            var reactionEngine = new ReactionEngine(reactions);

            string edictoDescripcion = DescribeEdicto(edicto);

            return new Universe(seed, mats, reactionEngine,
                CellGrid.CToRaw(growMinC), CellGrid.CToRaw(growMaxC),
                crystallizeMaxTempRaw, crystallizeChancePct,
                edicto, edictoDescripcion, caracterDelUniverso, firmaPorMaterial);
        }

        private static string DescribeEdicto(Edicto edicto)
        {
            switch (edicto)
            {
                case Edicto.FrioFertil:
                    return "El Maestro murmura: \"en este taller, el frío es fértil...\"";
                case Edicto.MateriaIrascible:
                    return "El Maestro murmura: \"cuidado — la materia de aquí es irascible...\"";
                case Edicto.DensidadInvertida:
                    return "El Maestro murmura: \"algo va al revés en el peso de las cosas...\"";
                default:
                    return "El Maestro murmura algo ininteligible.";
            }
        }

        private static float RandRange(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private static void ShuffleFisherYates(int[] arr, System.Random rng)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }

        // ===================================================================
        // FIRMA VISUAL POR SEED (playtest 12) — Ver Sim/MaterialDef.cs para
        // los campos que se rellenan aquí. Todo este bloque corre UNA vez por
        // Universe.Create, así que las asignaciones/List/HashSet de abajo son
        // aceptables (regla de oro del hot-path es sobre SimStepper, no esto).
        // ===================================================================

        /// <summary>
        /// Los 6 materiales innominados de esta run (ver CLAUDE.md regla 13).
        /// El vocabulario del taller (Stone/Sand/Water/Oil/Nutrient/Fire/
        /// Smoke/Ash/Steam/Ice) NO pasa por aquí: se queda con los valores
        /// por defecto de MaterialDef (patron=Liso, patronFuerza=0, borde=
        /// Neto) y el baseColor tal cual se definió arriba, sin tocar un solo
        /// byte — es el suelo firme desde el que el jugador juzga todo lo
        /// demás. (Se valoró darles un patrón fijo MUY tenue idéntico en toda
        /// seed —la excepción que el diseño permite— pero se descartó: con
        /// patronFuerza en 0 el campo `patron` es inerte de todas formas, así
        /// que la "excepción" no aportaba nada que un `patronFuerza` en 0 no
        /// garantizara ya, y cada byte que se toca aquí es un byte más que
        /// vigilar si mañana alguien reintroduce jitter global por error.)
        /// </summary>
        private static readonly byte[] UnnamedMaterialIds =
        {
            MaterialId.Azoth, MaterialId.CrystalSeed, MaterialId.Crystal,
            MaterialId.Vivium, MaterialId.Slime, MaterialId.Acid,
        };

        /// <summary>
        /// TABLA DE ARQUETIPO → FAMILIAS PLAUSIBLES (playtest 12). La firma no
        /// debe contradecir la física: un StaticSolid (Crystal) no anima
        /// frenético, un Organic (Vivium) no debería salir "muerto" (Liso).
        ///   StaticSolid (Crystal)    → Liso, Vetas, Celdas, Dendritas
        ///                              (facetas minerales o agujas de cristal;
        ///                              nunca algo que "lata" o "burbujee").
        ///   Powder (CrystalSeed)     → Liso, Vetas, Manchas, Motas
        ///                              (grano suelto: nada de laberintos ni
        ///                              teselas continuas, eso pide un medio
        ///                              continuo que un polvo no tiene).
        ///   Liquid (Azoth/Slime/Acid)→ Liso, Vetas, Manchas, Laberinto,
        ///                              Celdas, Pulso, Motas (casi todo vale
        ///                              menos Dendritas: una rama rígida no
        ///                              cuadra con algo que fluye y se vierte).
        ///   Organic (Vivium)         → Dendritas, Celdas, Manchas, Laberinto,
        ///                              Pulso, Motas (crece, así que nada de
        ///                              Liso/Vetas: eso es lo mineral e
        ///                              inerte, lo opuesto a "vivo").
        /// </summary>
        private static PatronMorfologico[] FamiliasPlausibles(MaterialArchetype arquetipo)
        {
            switch (arquetipo)
            {
                case MaterialArchetype.StaticSolid:
                    return new[] { PatronMorfologico.Liso, PatronMorfologico.Vetas, PatronMorfologico.Celdas, PatronMorfologico.Dendritas };
                case MaterialArchetype.Powder:
                    return new[] { PatronMorfologico.Liso, PatronMorfologico.Vetas, PatronMorfologico.Manchas, PatronMorfologico.Motas };
                case MaterialArchetype.Organic:
                    return new[] { PatronMorfologico.Dendritas, PatronMorfologico.Celdas, PatronMorfologico.Manchas, PatronMorfologico.Laberinto, PatronMorfologico.Pulso, PatronMorfologico.Motas };
                case MaterialArchetype.Liquid:
                default:
                    return new[] { PatronMorfologico.Liso, PatronMorfologico.Vetas, PatronMorfologico.Manchas, PatronMorfologico.Laberinto, PatronMorfologico.Celdas, PatronMorfologico.Pulso, PatronMorfologico.Motas };
            }
        }

        /// <summary>Bordes plausibles por arquetipo: Escarcha solo en lo mineral/granular (frío que cristaliza), Difuso no en lo sólido (un sólido no se deshilacha).</summary>
        private static BordeMorfologico[] BordesPlausibles(MaterialArchetype arquetipo)
        {
            switch (arquetipo)
            {
                case MaterialArchetype.StaticSolid:
                    return new[] { BordeMorfologico.Neto, BordeMorfologico.Halo, BordeMorfologico.Escarcha };
                case MaterialArchetype.Powder:
                    return new[] { BordeMorfologico.Neto, BordeMorfologico.Difuso, BordeMorfologico.Escarcha };
                case MaterialArchetype.Organic:
                    return new[] { BordeMorfologico.Neto, BordeMorfologico.Halo, BordeMorfologico.Difuso };
                case MaterialArchetype.Liquid:
                default:
                    return new[] { BordeMorfologico.Neto, BordeMorfologico.Halo, BordeMorfologico.Difuso };
            }
        }

        /// <summary>
        /// Sortea la firma visual completa de lo innominado y escribe el
        /// resultado directamente en <paramref name="mats"/>. Ver el cuerpo
        /// para las tres garantías (separación de tono, diversidad de
        /// familias, legibilidad) y su verificación aritmética.
        /// </summary>
        private static void SortearFirmasVisuales(MaterialDef[] mats, System.Random rng, out string caracterDelUniverso, out string[] firmaPorMaterial)
        {
            int n = UnnamedMaterialIds.Length; // 6

            // -----------------------------------------------------------------
            // GARANTÍA 1: separación de tono. Un tono de anclaje por seed +
            // reparto a intervalos regulares de 360/n grados con jitter
            // acotado a ±12°: la separación angular entre dos vecinos del
            // círculo NUNCA baja de (360/n) - 2*12. Con n=6 eso es 60-24=36°,
            // muy por encima del umbral en el que dos tonos empiezan a
            // confundirse a simple vista (~20-25° en una pantalla típica) —
            // el jugador tiene margen de sobra para distinguirlas en una cuba
            // llena. El orden material→hueco se baraja para que no sea
            // siempre "Azoth se lleva el primer hueco".
            // -----------------------------------------------------------------
            float anchorHueDeg = (float)rng.NextDouble() * 360f;
            float hueStepDeg = 360f / n;
            int[] hueSlots = new int[n];
            for (int i = 0; i < n; i++) hueSlots[i] = i;
            ShuffleFisherYates(hueSlots, rng);

            // -----------------------------------------------------------------
            // GARANTÍA 2: diversidad de familias. Se procesa en un orden
            // barajado (no siempre el mismo material "gana" la familia que
            // más le conviene) y cada material prefiere una familia AÚN NO
            // usada esta run dentro de las plausibles para su arquetipo; solo
            // repite si su arquetipo ya agotó las no usadas. Con 4-7 familias
            // plausibles por arquetipo y solo 6 materiales, las repeticiones
            // son raras y nunca totales.
            // -----------------------------------------------------------------
            int[] procOrder = new int[n];
            for (int i = 0; i < n; i++) procOrder[i] = i;
            ShuffleFisherYates(procOrder, rng);

            var arquetipos = new MaterialArchetype[n];
            for (int i = 0; i < n; i++) arquetipos[i] = mats[UnnamedMaterialIds[i]].archetype;

            var patronPorIdx = new PatronMorfologico[n];
            var familiasUsadas = new HashSet<PatronMorfologico>();
            for (int k = 0; k < n; k++)
            {
                int idx = procOrder[k];
                var plausibles = FamiliasPlausibles(arquetipos[idx]);
                var frescas = new List<PatronMorfologico>();
                foreach (var f in plausibles) if (!familiasUsadas.Contains(f)) frescas.Add(f);
                var pool = frescas.Count > 0 ? frescas : new List<PatronMorfologico>(plausibles);
                var elegida = pool[rng.Next(pool.Count)];
                familiasUsadas.Add(elegida);
                patronPorIdx[idx] = elegida;
            }

            // Refuerzo de la garantía 2: "al menos una debe quedar Liso o
            // Vetas" (si todo late y burbujea, la pantalla se vuelve ruido).
            // Vivium (Organic) nunca puede ser la elegida: su tabla de
            // familias plausibles no incluye ni Liso ni Vetas a propósito
            // (regla 3 del encargo: coherencia con lo que la sustancia hace).
            bool hayCalma = false;
            for (int i = 0; i < n; i++)
                if (patronPorIdx[i] == PatronMorfologico.Liso || patronPorIdx[i] == PatronMorfologico.Vetas) { hayCalma = true; break; }
            if (!hayCalma)
            {
                var candidatos = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    var pl = FamiliasPlausibles(arquetipos[i]);
                    if (Array.IndexOf(pl, PatronMorfologico.Liso) >= 0 || Array.IndexOf(pl, PatronMorfologico.Vetas) >= 0)
                        candidatos.Add(i);
                }
                int pick = candidatos[rng.Next(candidatos.Count)];
                patronPorIdx[pick] = rng.Next(2) == 0 ? PatronMorfologico.Liso : PatronMorfologico.Vetas;
            }

            // -----------------------------------------------------------------
            // GARANTÍA 3: legibilidad sobre el fondo. La pared del taller
            // (Game/WorkshopBackdrop.cs) va de ciruela oscuro (0.150,0.115,
            // 0.190) arriba a casi negro (0.062,0.048,0.058) abajo, y la
            // piedra (Stone) es (92,86,98). Con luminancia perceptual
            // L = 0.2126R + 0.7152G + 0.0722B (0..1):
            //   pared arriba  -> L ≈ 0.127
            //   piedra        -> L ≈ 0.345  (el elemento más claro del fondo)
            // Se exige L >= 0.40 para toda sustancia innominada: por encima
            // de la piedra con margen y muy por encima de la pared, así que
            // nunca se hunde visualmente contra ninguno de los dos.
            // -----------------------------------------------------------------
            const float minLuma = 0.40f;

            var huePorIdx = new float[n];
            for (int i = 0; i < n; i++)
            {
                byte matId = UnnamedMaterialIds[i];
                var m = mats[matId];
                var arch = arquetipos[i];
                var patron = patronPorIdx[i];

                // ---- Borde ----
                var bordes = BordesPlausibles(arch);
                m.borde = bordes[rng.Next(bordes.Length)];

                // ---- Escala/fuerza/ritmo/emisión, por arquetipo (regla 3:
                // coherencia con lo que la sustancia HACE) ----
                byte escala, fuerza, ritmo, emision;
                switch (arch)
                {
                    case MaterialArchetype.StaticSolid:
                        // Mineral, facetas grandes, y NUNCA anima: StaticSolid
                        // ni siquiera cae solo (CLAUDE.md regla 7) — que tampoco lata.
                        escala = (byte)(3 + rng.Next(6)); // 3..8
                        fuerza = (byte)(50 + rng.Next(101)); // 50..150
                        ritmo = 0;
                        break;
                    case MaterialArchetype.Powder:
                        escala = (byte)(1 + rng.Next(3)); // 1..3, grano fino
                        // (fix playtest 13) Suelo subido de 40 a 55: Powder es el
                        // ÚNICO arquetipo cuyo suelo de fuerza bajaba de 50 (los
                        // otros tres van 50/50/60), y es precisamente el que
                        // combina con el patronEscala MÁS PEQUEÑO del roster
                        // (1..3, "grano fino"): rasgo diminuto + contraste mínimo
                        // es la combinación que con más probabilidad cruza el
                        // umbral de percepción hacia "invisible". amt en
                        // ModulatePattern es wave*fuerza/255 con wave~±127: a
                        // fuerza=40 el swing máximo era ±20/255 (~8%), apenas
                        // por encima de ruido de compresión de pantalla; a 55
                        // sube a ±27/255, ya un empujón de brillo claramente
                        // legible sin desbordar el techo de 110 (sigue siendo el
                        // arquetipo menos contrastado, coherente con "grano
                        // suelto" frente a mineral/orgánico).
                        fuerza = (byte)(55 + rng.Next(56)); // 55..110
                        ritmo = (byte)rng.Next(41); // 0..40, casi estático
                        break;
                    case MaterialArchetype.Organic:
                        // Vivo: nunca del todo quieto (mínimo 40) para que se
                        // note que crece incluso antes de que el jugador lo sepa.
                        escala = (byte)(2 + rng.Next(5)); // 2..6
                        fuerza = (byte)(60 + rng.Next(91)); // 60..150
                        ritmo = (byte)(40 + rng.Next(121)); // 40..160
                        break;
                    case MaterialArchetype.Liquid:
                    default:
                        escala = (byte)(2 + rng.Next(4)); // 2..5
                        fuerza = (byte)(50 + rng.Next(81)); // 50..130
                        ritmo = (byte)rng.Next(121); // 0..120
                        break;
                }
                // Vetas es "quieto y mineral" por definición (ver doc del enum
                // en MaterialDef.cs): se frena aunque su arquetipo sea Liquid.
                if (patron == PatronMorfologico.Vetas) ritmo = (byte)rng.Next(21); // 0..20
                // Liso no tiene dibujo que animar; forzar 0 evita "ritmo
                // fantasma" en un patrón invisible.
                if (patron == PatronMorfologico.Liso) { ritmo = 0; fuerza = 0; }

                emision = m.emitsGlow ? (byte)(70 + rng.Next(111)) : (byte)rng.Next(41); // 70..180 si ya brilla, si no 0..40

                m.patron = patron;
                m.patronEscala = escala;
                m.patronFuerza = fuerza;
                m.ritmoAnim = ritmo;
                m.emision = emision;
                m.semillaPatron = (byte)rng.Next(256);

                // ---- Color: tono repartido + legibilidad garantizada ----
                float jitterDeg = ((float)rng.NextDouble() * 2f - 1f) * 12f; // ±12°
                float hueDeg = Mathf.Repeat(anchorHueDeg + hueSlots[i] * hueStepDeg + jitterDeg, 360f);
                float sat = 0.55f + (float)rng.NextDouble() * 0.30f; // 0.55..0.85
                float val = 0.55f + (float)rng.NextDouble() * 0.30f; // 0.55..0.85 (punto de partida; EnsureMinLuma lo empuja si hace falta)
                // (fix playtest 13, "el rosa es transparente / el verde no") ANTES
                // esta línea era `byte alphaOriginal = m.baseColor.a;`, que
                // conservaba el alfa base del roster (255 para StaticSolid/Powder/
                // Organic, pero 215/220/235 para Azoth/Acid/Slime -- los tres
                // ÚNICOS Liquid de lo innominado). SimRenderer.ComputeCellColor
                // devuelve `baseColor.a` sin tocarlo en NINGÚN camino salvo Fuego
                // (que ya fuerza 255 aparte) -- es el mismo alfa para TODA la
                // celda, contorno E INTERIOR, cada frame. Eso es exactamente la
                // trampa que la regla 19 de CLAUDE.md advierte para el borde
                // Difuso (mosaico duro del ladrillo de fondo en bloques de
                // ~7.5px, dos texturas Point de resoluciones distintas
                // componiendo alfa) pero aplicada a la SUSTANCIA ENTERA, no solo
                // al contorno -- el jugador lo describió literal ("se logran
                // divisar un poquito los patrones de los ladrillos atrás") para
                // una sustancia rosa que por su descripción es Liquid (Azoth con
                // alfa heredado 215, o Acid/Slime con 220/235), mientras que la
                // verde ("no tiene transparencia") encaja con StaticSolid/Powder/
                // Organic, que ya nacían en 255. La transparencia de líquido es
                // una decisión de arte VÁLIDA para el vocabulario del taller
                // (Water/Oil, ver regla 17: nunca pasan por aquí, se quedan con
                // su alfa de diseño intacto) pero lo innominado necesita máxima
                // legibilidad de patrón -- por eso aquí, y SOLO aquí, se fuerza
                // opacidad total en vez de heredar el alfa del roster. El borde
                // Difuso sigue intacto y sigue sin tocar el canal alfa (oscurece
                // hacia BackgroundColor, ver SimRenderer.ComputeCellColor): esto
                // no lo sustituye, corrige un bug DISTINTO que se sumaba encima.
                const byte OpacidadTotalInnominado = 255;
                Color32 candidato = Color.HSVToRGB(hueDeg / 360f, sat, val, true);
                candidato.a = OpacidadTotalInnominado;
                candidato = EnsureMinLuma(candidato, minLuma);
                m.baseColor = candidato;
                huePorIdx[i] = hueDeg;
            }

            // -----------------------------------------------------------------
            // Carácter del universo: tono del material con la familia más
            // repetida (excluyendo Liso, que por definición no es un "rasgo").
            // No nombra sustancias: describe el clima visual de la run.
            // -----------------------------------------------------------------
            var conteoFamilia = new Dictionary<PatronMorfologico, int>();
            foreach (var p in patronPorIdx)
            {
                if (p == PatronMorfologico.Liso) continue;
                conteoFamilia.TryGetValue(p, out int c);
                conteoFamilia[p] = c + 1;
            }
            PatronMorfologico dominante = PatronMorfologico.Liso;
            int mejorConteo = -1;
            int idxDominante = 0;
            for (int i = 0; i < n; i++)
            {
                if (patronPorIdx[i] == PatronMorfologico.Liso) continue;
                int c = conteoFamilia[patronPorIdx[i]];
                if (c > mejorConteo) { mejorConteo = c; dominante = patronPorIdx[i]; idxDominante = i; }
            }
            string colorPlural = HueNombrePlural(huePorIdx[idxDominante]);
            caracterDelUniverso = $"Un mundo de {colorPlural} que {VerboParaFamilia(dominante)}.";

            // -----------------------------------------------------------------
            // Cachear DescribirFirma para TODOS los materiales (no solo lo
            // innominado): el vocabulario del taller también tiene firma
            // (siempre Liso/Neto), así el diario puede llamarlo sin distinguir
            // casos. Nunca se reconstruye por frame: se hornea aquí, una vez.
            // -----------------------------------------------------------------
            firmaPorMaterial = new string[mats.Length];
            for (int id = 0; id < mats.Length; id++)
            {
                var m = mats[id];
                if (m == null || m.archetype == MaterialArchetype.Empty) { firmaPorMaterial[id] = string.Empty; continue; }
                Color.RGBToHSV(m.baseColor, out float h01, out _, out _);
                string colorNombre = HueNombreSingular(h01 * 360f);
                string fraseFamilia = FraseFamiliaIndividual(m.patron, m.ritmoAnim);
                string bordeEtiqueta = BordeEtiqueta(m.borde);
                firmaPorMaterial[id] = $"{colorNombre}, {fraseFamilia}, {bordeEtiqueta}";
            }
        }

        /// <summary>Luminancia perceptual 0..1 (coeficientes Rec.709, aproximación suficiente para un umbral de legibilidad, no para color science).</summary>
        private static float Luma(Color32 c)
        {
            return (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
        }

        /// <summary>
        /// Empuja el color hacia arriba de luminancia en pasos deterministas
        /// acotados, hasta cumplir el mínimo o agotar el rango.
        /// PRIMERO sube V (brillo); pero subir V solo no basta para tonos
        /// azul/violeta puros a saturación alta — con H≈240° y S alta, HSV
        /// da R≈G≈0 incluso con V=1 (L=0.2126R+0.7152G+0.0722B se apoya casi
        /// todo en el canal B, que pesa solo 0.0722), así que un azul
        /// saturado puede quedarse en L≈0.07 aunque V esté a tope. Bug real
        /// encontrado simulando el sorteo (seed 42, Crystal caía a L≈0.31,
        /// por debajo del mínimo de 0.40) antes de fijarlo aquí: una vez V
        /// toca techo, el bucle SIGUE bajando S (hacia blanco) hasta cumplir
        /// el mínimo o tocar un suelo de saturación (0.15, sigue leyéndose
        /// como color, no gris puro).
        /// </summary>
        private static Color32 EnsureMinLuma(Color32 c, float minLuma)
        {
            byte a = c.a;
            Color.RGBToHSV(c, out float h, out float s, out float v);
            for (int i = 0; i < 24 && Luma(Color.HSVToRGB(h, s, v, true)) < minLuma; i++)
            {
                if (v < 1f) v = Mathf.Min(1f, v + 0.08f);
                else if (s > 0.15f) s = Mathf.Max(0.15f, s - 0.08f);
                else break; // rango agotado (no debería ocurrir con minLuma=0.40, ver comentario arriba)
            }
            Color32 result = Color.HSVToRGB(h, s, v, true);
            result.a = a;
            return result;
        }

        // 12 tonos a intervalos de 30° cubriendo el círculo cromático completo,
        // en singular (descripción de un material) y plural (frase del universo).
        private static readonly string[] HueNombresSingular =
        {
            "rojo", "naranja", "ámbar", "dorado", "verde", "esmeralda",
            "turquesa", "azul", "añil", "violeta", "magenta", "carmín",
        };
        private static readonly string[] HueNombresPlural =
        {
            "rojos", "naranjas", "ámbares", "dorados", "verdes", "esmeraldas",
            "turquesas", "azules", "añiles", "violetas", "magentas", "carmines",
        };

        private static int HueBucket(float hueDeg)
        {
            int b = Mathf.FloorToInt(Mathf.Repeat(hueDeg, 360f) / 30f);
            return Mathf.Clamp(b, 0, 11);
        }

        private static string HueNombreSingular(float hueDeg) => HueNombresSingular[HueBucket(hueDeg)];
        private static string HueNombrePlural(float hueDeg) => HueNombresPlural[HueBucket(hueDeg)];

        /// <summary>Verbo (plural, concuerda con el nombre de color plural) para la frase de carácter del universo.</summary>
        private static string VerboParaFamilia(PatronMorfologico p)
        {
            switch (p)
            {
                case PatronMorfologico.Vetas: return "se agrietan en vetas quietas";
                case PatronMorfologico.Manchas: return "se reparten en manchas inquietas";
                case PatronMorfologico.Laberinto: return "se enredan en serpentinas laberínticas";
                case PatronMorfologico.Celdas: return "se organizan en celdas como panal";
                case PatronMorfologico.Dendritas: return "crecen en agujas y ramas";
                case PatronMorfologico.Pulso: return "respiran";
                case PatronMorfologico.Motas: return "titilan en motas dispersas";
                default: return "guardan una calma tersa"; // Liso, no debería llegar aquí (se excluye antes)
            }
        }

        /// <summary>Frase corta de familia+ritmo para DescribirFirma (un material concreto).</summary>
        private static string FraseFamiliaIndividual(PatronMorfologico p, byte ritmoAnim)
        {
            if (p == PatronMorfologico.Liso) return "superficie lisa";
            string velocidad = VelocidadAdjetivo(p, ritmoAnim);
            string sustantivo;
            switch (p)
            {
                case PatronMorfologico.Vetas: sustantivo = "vetas"; break;
                case PatronMorfologico.Manchas: sustantivo = "manchas"; break;
                case PatronMorfologico.Laberinto: sustantivo = "laberinto"; break;
                case PatronMorfologico.Celdas: sustantivo = "celdas"; break;
                case PatronMorfologico.Dendritas: sustantivo = "dendritas"; break;
                case PatronMorfologico.Pulso: sustantivo = "pulso"; break;
                case PatronMorfologico.Motas: sustantivo = "motas"; break;
                default: sustantivo = "patrón"; break;
            }
            return $"{sustantivo} {velocidad}";
        }

        /// <summary>Adjetivo de velocidad concordado en género/número con el sustantivo de familia (Laberinto/Pulso son masculino singular; el resto, femenino plural).</summary>
        private static string VelocidadAdjetivo(PatronMorfologico p, byte ritmoAnim)
        {
            bool masculinoSingular = p == PatronMorfologico.Laberinto || p == PatronMorfologico.Pulso;
            int tier = ritmoAnim == 0 ? 0 : (ritmoAnim < 90 ? 1 : 2);
            if (masculinoSingular)
                return tier == 0 ? "quieto" : tier == 1 ? "lento" : "vivo";
            return tier == 0 ? "quietas" : tier == 1 ? "lentas" : "vivas";
        }

        private static string BordeEtiqueta(BordeMorfologico b)
        {
            switch (b)
            {
                case BordeMorfologico.Halo: return "borde con halo";
                case BordeMorfologico.Escarcha: return "borde escarchado";
                case BordeMorfologico.Difuso: return "borde difuso";
                default: return "borde neto";
            }
        }
    }
}
