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

        public const int Count = 13;
    }

    /// <summary>
    /// Un "universo" es el conjunto de leyes/materiales de una partida
    /// concreta, derivado de una seed. Hoy los valores de juego (densidad,
    /// fluidez, temperaturas...) son fijos para todas las seeds -- solo la
    /// paleta de color recibe una pequeña variación -- pero TODO el resto
    /// del código (Sim, Dev, Editor) lee los materiales a través de esta
    /// clase para que el día que queramos variación de leyes por partida,
    /// el único archivo a tocar sea este.
    /// </summary>
    public sealed class Universe
    {
        public readonly int Seed;
        public readonly MaterialDef[] Materials;

        private Universe(int seed, MaterialDef[] materials)
        {
            Seed = seed;
            Materials = materials;
        }

        public MaterialDef Get(byte id) => Materials[id];

        /// <summary>
        /// Crea un universo determinista a partir de una seed. Un
        /// System.Random local (NUNCA UnityEngine.Random, y nunca
        /// almacenado para uso posterior) se usa solo para el jitter de
        /// color inicial de cada material.
        /// </summary>
        public static Universe Create(int seed)
        {
            var rng = new System.Random(seed);
            var mats = new MaterialDef[MaterialId.Count];

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
                density = 100,
                fluidity = 4,
                freezesAt = CellGrid.CToRaw(0),
                freezesInto = MaterialId.Ice,
                boilsAt = CellGrid.CToRaw(100),
                boilsInto = MaterialId.Steam,
            };

            mats[MaterialId.Oil] = new MaterialDef
            {
                id = MaterialId.Oil,
                devName = "Oil",
                archetype = MaterialArchetype.Liquid,
                baseColor = new Color32(84, 58, 20, 220),
                colorJitter = 12,
                density = 60,
                fluidity = 3,
                flammable = true,
                ignitionTemp = CellGrid.CToRaw(260),
                burnsInto = MaterialId.Fire,
            };

            mats[MaterialId.Slime] = new MaterialDef
            {
                id = MaterialId.Slime,
                devName = "Slime",
                archetype = MaterialArchetype.Liquid,
                baseColor = new Color32(84, 196, 96, 235),
                colorJitter = 16,
                density = 150,
                fluidity = 1,
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
                condensesAt = CellGrid.CToRaw(60),
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
                gasLifetime = 45,
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
                meltsAt = CellGrid.CToRaw(5),
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
                fluidity = 0, // No se mueve una vez asentado (ver SimStepper.GrowthTick TODO).
                emitsGlow = true,
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

            return new Universe(seed, mats);
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
