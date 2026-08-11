using System;
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

        private Universe(int seed, MaterialDef[] materials, ReactionEngine reactions,
            byte vivGrowMinRaw, byte vivGrowMaxRaw, byte crystallizeMaxTempRaw, byte crystallizeChancePct,
            Edicto edicto, string edictoDescripcion)
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
        }

        public MaterialDef Get(byte id) => Materials[id];

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
                ? (byte)Mathf.Clamp(Mathf.RoundToInt(45f * 1.5f), 1, 255)
                : (byte)45;

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
                baseColor = new Color32(110, 108, 112, 255),
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
                baseColor = new Color32(72, 66, 68, 255),
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
                baseColor = new Color32(170, 220, 235, 255),
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

            // Pequeño jitter de color por seed: cada universo "se siente"
            // ligeramente distinto aunque las reglas de juego sean iguales.
            // Nota: esto NO afecta al hot-path por tick, solo se ejecuta
            // una vez en la creación del universo.
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (m.archetype == MaterialArchetype.Empty) continue;
                m.baseColor = JitterHue(m.baseColor, rng);
            }

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
                edicto, edictoDescripcion);
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

        private static Color32 JitterHue(Color32 c, System.Random rng)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            // ±8 grados de hue aprox (8/360).
            float delta = ((float)rng.NextDouble() * 2f - 1f) * (8f / 360f);
            h = Mathf.Repeat(h + delta, 1f);
            Color rgb = Color.HSVToRGB(h, s, v, true);
            byte a = c.a;
            Color32 result = rgb;
            result.a = a;
            return result;
        }
    }
}
