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

        // LA MAREA (CONTRATO_MAREA.md): un proceso propio de SimStepper, NO
        // una reacción sorteada del roster de arriba (regla 33 -- la marea no
        // perturba el sorteo de leyes ni el diario). Marea sube desde el
        // corazón del sótano y convierte lo que toca; Rocío es lo que exuda
        // la criatura al digerirla y es su única cura real. Ver
        // SimStepper.ProcessMarea y Game/Criatura.EscogerProductoDigestion
        // (fuera de este encargo) para el resto de la cadena.
        public const byte Marea = 17;
        public const byte Rocio = 18;

        public const int Count = 19;
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
        /// <summary>
        /// Probabilidad de que consumir un Nutrient en banda cree una nueva
        /// célula de Vivium (si no, solo se consume). Era 60 antes del
        /// playtest 19; sube a 75 para COMPENSAR la tasa de cultivo tras el
        /// gate de "solo las puntas engendran" (ver
        /// <see cref="HabitoTolerarVecinosPunta"/> y
        /// SimStepper.GrowthTick): con el gate, menos células
        /// compiten por Nutrient a la vez, así que a igual probabilidad el
        /// cultivo tardaría más que con la mancha vieja. Medido con un
        /// modelo en Python (informe playtest 19, 150 semillas): la mancha
        /// vieja tardaba ~46 ticks de media en llegar a 120 células a 60%;
        /// la silueta nueva SIN compensar tardaba ~52; con este 75% baja a
        /// ~40 -- igual de rápida o más, sin perder la ramificación (el
        /// gate de puntas no depende de este número).
        /// </summary>
        public readonly byte VivGrowChancePct;

        // -----------------------------------------------------------------
        // HÁBITO DE CRECIMIENTO DEL VIVIUM (playtest 19, forma dendrítica por
        // semilla). Cesar: "lo que no vi por más que intenté es que algo
        // crezca con formas que vengan de algoritmos, fractales qué sé yo...
        // solo vi diferencias de viscosidad y propagación". El campo
        // morfológico (playtest 12) le da TEXTURA al Vivium, pero la FORMA
        // del organismo seguía siendo un borrón redondo -- GrowthTick hacía
        // crecer cualquier célula asentada con Nutrient al lado, así que un
        // núcleo con varios vecinos de Nutrient rellenaba su entorno entero
        // en unos pocos ticks. La regla nueva (ver GrowthTick) hace que
        // SOLO LAS PUNTAS engendren -- una célula con muchos vecinos de
        // Vivium ya es tallo/interior y deja de competir por Nutrient. Estos
        // cuatro números, sorteados por semilla, son el "hábito de
        // crecimiento" concreto de ESTE universo: tupido o ralo, recto o
        // errático, isótropo o con preferencia vertical -- así una silueta
        // se reconoce de otra sin mirar el color.
        //
        // EXPUESTO A PROPÓSITO como campos públicos, igual que
        // AfinidadDelUniverso (playtest 18, ver ese campo): el gancho para
        // una ronda futura es que el diario/el rumor del Edicto lo insinúen
        // sin decirlo ("el Maestro murmura que aquí la vida trepa hacia la
        // luz...", "...que aquí la vida se ramifica muy despacio..."). Esta
        // ronda NO toca ese texto, solo deja los datos listos.
        // -----------------------------------------------------------------
        /// <summary>
        /// Vecinos ORTOGONALES de Vivium que una célula tolera y aun así
        /// seguir contando como PUNTA (ver SimStepper.GrowthTick).
        /// Por encima de este número, la célula es tallo/interior y no
        /// vuelve a intentar engendrar. Rango 2-3, NUNCA 1: un modelo en
        /// Python (informe playtest 19, 60 semillas) mostró que con
        /// tolerancia 1 el 100% de las semillas probadas terminaba en un
        /// anillo cerrado que se autobloquea para siempre (cada célula del
        /// anillo pasa a tener 2 vecinos propios, por encima de la
        /// tolerancia, y ninguna vuelve a crecer) -- un cultivo que deja de
        /// crecer para siempre rompería los encargos, así que se descarta
        /// aunque sea la opción más "fina" visualmente.
        /// </summary>
        public readonly byte HabitoTolerarVecinosPunta;
        /// <summary>
        /// Probabilidad (0-100) de que una punta con dirección conocida la
        /// IGNORE a propósito esta vez y fuerce un candidato distinto -- la
        /// célula sigue teniendo pocos vecinos, así que puede volver a
        /// engendrar otro tick en una tercera dirección, y las dos crías ya
        /// se leen como horquilla. EL parámetro que más cambia la silueta
        /// (más alto = colonia más ramificada y más abierta; más bajo =
        /// filamentos largos con pocas horquillas).
        /// </summary>
        public readonly byte HabitoBifurcarPct;
        /// <summary>
        /// Probabilidad (0-100) de que una punta con dirección conocida SIGA
        /// esa dirección en vez de recalcular el mejor candidato cada vez --
        /// ramas rectas (alto) frente a erráticas/zigzagueantes (bajo).
        /// </summary>
        public readonly byte HabitoPersistenciaPct;
        /// <summary>
        /// Sesgo -100..100: positivo tantea con esa probabilidad crecer
        /// hacia ARRIBA antes que el orden isotrópico normal ("planta que
        /// trepa a la luz"); negativo tantea crecer hacia ABAJO ("moho que
        /// se entierra hacia el nutriente"); 0 = isótropo, crece hacia
        /// donde haya Nutrient sin preferencia. Es un rasgo del UNIVERSO,
        /// no de la textura que le tocó a este Vivium -- se aplica igual en
        /// las tres familias visuales de SimStepper.
        /// </summary>
        public readonly sbyte HabitoSesgoVerticalPct;

        /// <summary>Umbral de temperatura raw por DEBAJO del cual Azoth cristaliza al tocar Crystal/CrystalSeed.</summary>
        public readonly byte CrystallizeMaxTempRaw;
        public readonly byte CrystallizeChancePct;

        public readonly Edicto ActiveEdicto;
        /// <summary>Texto de rumor en español mostrado al jugador (DayCycle, log de arranque). Nunca revela mecánicamente las leyes, solo insinúa.</summary>
        public readonly string EdictoDescripcion;

        // -----------------------------------------------------------------
        // LEYES DEL UNIVERSO (playtest 18, química generada por seed). Ver
        // el bloque grande "SORTEO DE LEYES" en Create() para la gramática
        // (contrato CONTRATO_FASE3.md sección 6) y ConstruirLeyesNucleo/
        // SortearLeyesGeneradas/ConstruirLeyCrecimiento para la construcción.
        // -----------------------------------------------------------------
        /// <summary>
        /// TODAS las leyes de este universo, con ÍNDICE ESTABLE. INVARIANTE QUE
        /// NO SE PUEDE ROMPER: para i &lt; Reactions.Count, Leyes[i] describe
        /// EXACTAMENTE la reacción Reactions.At(i) (mismo par, mismos productos,
        /// misma banda); la última entrada, en el índice
        /// LeyCrecimientoIndice == Reactions.Count, es la del Vivium, que no es
        /// una reacción de contacto. Los eventos de la sim viajan con este índice
        /// (SimEventType.Ley / SimNotableEvent.leyIndice). Se verifica con un
        /// assert de solo-editor al final de Create() -- de esta invariante
        /// depende que un evento pueda identificar QUÉ ley acaba de ocurrir.
        /// </summary>
        public readonly LeyDelUniverso[] Leyes;

        /// <summary>Índice de la ley de crecimiento del Vivium dentro de Leyes. Siempre == Reactions.Count.</summary>
        public int LeyCrecimientoIndice { get; }

        /// <summary>Tope duro de leyes por universo. El diario ya reserva este tamaño (JournalHud.MaxLeyes).</summary>
        public const int MaxLeyes = 24;

        /// <summary>
        /// (playtest 18, AFINIDAD DEL UNIVERSO) 1 o 2 ids de material hacia
        /// los que esta semilla "tira" al elegir el producto de una ley
        /// sorteada -- nunca <see cref="MaterialId.Empty"/> ("todo tiende a
        /// desaparecer" no es una tesis, es una partida rota). Ver el bloque
        /// grande en Create() para el porqué: sin esto, cada ley elegía su
        /// producto en una bolsa uniforme sin relación con las demás leyes
        /// de la MISMA semilla, y el resultado era "5 plantillas con
        /// sustantivos intercambiables" en vez de un universo con carácter.
        ///
        /// EXPUESTO A PROPÓSITO como campo público y legible, no como detalle
        /// interno del generador: el gancho evidente para una ronda futura es
        /// que el rumor del Edicto (<see cref="EdictoDescripcion"/>) la
        /// insinúe sin decirla ("el Maestro murmura que aquí todo quiere
        /// volverse limo..."). Esta ronda NO toca ese texto -- solo deja el
        /// dato listo para que otra ronda lo lea.
        /// </summary>
        public readonly byte[] AfinidadDelUniverso;

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
            byte vivGrowMinRaw, byte vivGrowMaxRaw, byte vivGrowChancePct,
            byte habitoTolerarVecinosPunta, byte habitoBifurcarPct, byte habitoPersistenciaPct, sbyte habitoSesgoVerticalPct,
            byte crystallizeMaxTempRaw, byte crystallizeChancePct,
            Edicto edicto, string edictoDescripcion, string caracterDelUniverso, string[] firmaPorMaterial,
            LeyDelUniverso[] leyes, int leyCrecimientoIndice, byte[] afinidadDelUniverso)
        {
            Seed = seed;
            Materials = materials;
            Reactions = reactions;
            VivGrowMinRaw = vivGrowMinRaw;
            VivGrowMaxRaw = vivGrowMaxRaw;
            VivGrowChancePct = vivGrowChancePct;
            HabitoTolerarVecinosPunta = habitoTolerarVecinosPunta;
            HabitoBifurcarPct = habitoBifurcarPct;
            HabitoPersistenciaPct = habitoPersistenciaPct;
            HabitoSesgoVerticalPct = habitoSesgoVerticalPct;
            CrystallizeMaxTempRaw = crystallizeMaxTempRaw;
            CrystallizeChancePct = crystallizeChancePct;
            ActiveEdicto = edicto;
            EdictoDescripcion = edictoDescripcion;
            CaracterDelUniverso = caracterDelUniverso;
            _firmaPorMaterial = firmaPorMaterial;
            Leyes = leyes;
            LeyCrecimientoIndice = leyCrecimientoIndice;
            AfinidadDelUniverso = afinidadDelUniverso;
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
            // 4b) HÁBITO DE CRECIMIENTO DEL VIVIUM (playtest 19, forma
            //     dendrítica por semilla). Ver el bloque de campos
            //     Universe.HabitoTolerarVecinosPunta y hermanos para el
            //     porqué de cada rango -- en particular por qué la
            //     tolerancia NUNCA sortea 1.
            // -----------------------------------------------------------------
            byte habitoTolerarVecinosPunta = (byte)(rng.Next(3) == 2 ? 3 : 2); // 2 el doble de probable que 3 (2/3 vs 1/3): la mayoría de semillas ramifica fino, algunas más tupido.
            byte habitoBifurcarPct = (byte)rng.Next(4, 21);      // 4-20%: EL parámetro que más cambia la silueta.
            byte habitoPersistenciaPct = (byte)rng.Next(45, 86); // 45-85%.
            sbyte habitoSesgoVerticalPct;
            if (rng.NextDouble() < 0.4)
            {
                habitoSesgoVerticalPct = 0; // isótropo: ni planta ni moho, ~40% de las semillas.
            }
            else
            {
                int magnitudSesgo = rng.Next(30, 71);
                habitoSesgoVerticalPct = (sbyte)(rng.Next(2) == 0 ? magnitudSesgo : -magnitudSesgo);
            }

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
            // LA MAREA y EL ROCÍO (CONTRATO_MAREA.md sección 3.1). Los dos
            // quedan FUERA del sorteo de densidad de arriba (liquidDensity[]
            // solo baraja los 5 líquidos "variables" del roster de leyes) --
            // sus densidades son FIJAS por contrato, no varían por seed: la
            // marea siempre debe hundirse bajo TODO líquido variable (el tope
            // de ese sorteo, con jitter, es ~186) para poder SUBIR desde el
            // fondo del corazón desplazando lo que haya encima, y el Rocío
            // siempre debe flotar sobre ella. Se registran en liquidDensity[]
            // de todas formas (y se leen de ahí, no como literal) para que el
            // patrón "density = liquidDensity[id]" sea uniforme para
            // cualquier líquido del roster, sortee su valor o no.
            liquidDensity[MaterialId.Marea] = 200;
            liquidDensity[MaterialId.Rocio] = 80;

            mats[MaterialId.Marea] = new MaterialDef
            {
                id = MaterialId.Marea,
                devName = "Marea",
                archetype = MaterialArchetype.Liquid,
                // Violeta muy oscuro -- TINTADO ~20% hacia el baseColor del
                // PRIMER material de AfinidadDelUniverso más abajo, JUSTO
                // DESPUÉS de sortearla (busca "TINTE DE LA MAREA" en este
                // mismo método): la marea ES la química de esta semilla
                // hecha carne, así que no puede quedar con un color 100% fijo
                // como el resto del vocabulario del taller. El valor de aquí
                // es el color BASE pre-tinte.
                baseColor = new Color32(46, 22, 58, 255),
                colorJitter = 10,
                density = liquidDensity[MaterialId.Marea], // 200: se hunde bajo TODO líquido variable del roster (tope ~186 con jitter) -- sube desde el fondo, no cae desde arriba.
                // (contrato §3.1, CORREGIDO EN INTEGRACIÓN) El contrato
                // decía "~120" asumiendo una escala 0-255, pero fluidity se
                // consume en TryFlow como Nº DE CELDAS a escanear por tick:
                // la escala real del roster es 1-4. Con 120, una celda de
                // marea sobre piso despejado cruzaría todo el tramo libre EN
                // UN TICK (tsunami, no marea) y pagaría hasta 120 iteraciones
                // de escaneo por celda asentada por tick. fluidity=1 es la
                // lectura correcta de "repta, no salpica": el avance lateral
                // mínimo del motor, un dedo de oscuridad que gana UNA celda
                // cada vez -- la presión de la marea viene de la EMISIÓN y la
                // CONVERSIÓN, nunca de la velocidad de chapoteo.
                fluidity = 1,
                // No arde, no congela, no hierve (contrato): sus campos de
                // transición de fase quedan en los sentinelas short.MaxValue/
                // MinValue por defecto de MaterialDef -- la marea no cambia
                // de fase nunca, su única debilidad es el Rocío/fuego/piedra
                // de SimStepper.ProcessMarea, no la temperatura.
                emitsGlow = false,
                emision = 30,
                // EXCEPCIÓN DOCUMENTADA A LA REGLA 17 (CLAUDE.md): la regla
                // dice que solo lo INNOMINADO sortea firma visual por seed y
                // el vocabulario del taller se ve siempre igual (patron=Liso,
                // borde=Neto). La Marea no es ni una cosa ni la otra -- no es
                // vocabulario del taller (nace del corazón, no de un grifo) y
                // tampoco pasa por SortearFirmasVisuales (no está en
                // UnnamedMaterialIds) -- pero por la MISMA razón que el
                // vocabulario (regla 13/17: "si todo cambia, nada se
                // reconoce"), su patrón/borde deben ser FIJOS entre universos:
                // el jugador tiene que reconocer la marea de un vistazo en
                // CUALQUIER semilla, es la amenaza central del juego, no una
                // sustancia más que descubrir. Por eso patron/borde (y el
                // resto de la firma: escala/fuerza/ritmo/semilla) están
                // escritos a mano aquí, nunca sorteados.
                patron = PatronMorfologico.Pulso,
                borde = BordeMorfologico.Halo,
                patronEscala = 4,   // periodo ~5 celdas (SimRenderer.PatronPeriodoCeldas/MorphPulse): varias bandas visibles incluso en la cámara del corazón (22 celdas de ancho).
                patronFuerza = 90,  // contraste visible sin gritar (rango útil 40..150 documentado en MaterialDef.patronFuerza).
                ritmoAnim = 36,     // late LENTO y grave -- un corazón, no un parpadeo nervioso.
                semillaPatron = 128,
            };

            mats[MaterialId.Rocio] = new MaterialDef
            {
                id = MaterialId.Rocio,
                devName = "Rocio",
                archetype = MaterialArchetype.Liquid,
                baseColor = new Color32(232, 214, 150, 255),
                colorJitter = 8,
                density = liquidDensity[MaterialId.Rocio], // 80: ligero, flota sobre el agua y sobre la propia marea.
                // (contrato §3.1, CORREGIDO EN INTEGRACIÓN) Misma escala
                // real 1-4 que se explica en Marea: "alta" = 4, el tope del
                // roster (como el agua) -- el Rocío corre y se desparrama, es
                // el anti-marea y debe llegar fácil a donde se le vierte.
                fluidity = 4,
                // Sin transiciones térmicas (contrato): sentinelas por defecto.
                emitsGlow = true, // la cura BRILLA -- se ve en la oscuridad del sótano.
                emision = 140,    // mismo rango (70..180) que un innominado sorteado con emitsGlow=true, fijo aquí por la misma excepción documentada arriba en Marea.
                // Misma excepción a la regla 17 que Marea (ver el comentario
                // grande de arriba): es el anti-marea y tiene que reconocerse
                // igual de fijo. patron=Liso no usa patronFuerza/patronEscala/
                // ritmoAnim/semillaPatron (MaterialDef.patron, "Liso no lo
                // usa") así que se dejan en sus valores por defecto a
                // propósito, ni siquiera se escriben aquí.
                patron = PatronMorfologico.Liso,
                borde = BordeMorfologico.Neto,
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
            // NÚCLEO FIJO: estas 7 reacciones existen EN TODA SEMILLA, sin
            // excepción -- son las que sostienen los encargos del Maestro (si
            // desaparece la cristalización hay semillas imposibles de
            // completar). NO REORDENAR este array sin actualizar
            // ConstruirLeyesNucleo() más abajo: describe estas 7 entradas por
            // ÍNDICE, a mano, porque su `forma`/`condicion` no varía nunca.
            // -----------------------------------------------------------------
            const byte acidDissolveChancePct = 40;
            const byte acidNeutralizeChancePct = 20;
            var nucleoReactions = new[]
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

            // -----------------------------------------------------------------
            // SORTEO DE LEYES (playtest 18, CONTRATO_FASE3.md sección 6): 5-8
            // reacciones adicionales, propias de esta seed, siguiendo la
            // gramática ATREVIDA que Cesar eligió ("lo innominado puede
            // reaccionar con el vocabulario del taller"). Bandas térmicas de
            // Frio/Calor derivadas de CellGrid.AmbientRaw (no números mágicos
            // nuevos: ver el comentario de CondicionMarginRaw).
            // -----------------------------------------------------------------
            const int CondicionMarginRaw = 30; // = 15 °C * 2 raw/°C: mismo orden de magnitud que freezeShiftC/boilShiftC/growShiftC (±15 °C) ya usados arriba en esta función para bandear temperaturas por seed.
            byte frioMaxTempRaw = (byte)(CellGrid.AmbientRaw - CondicionMarginRaw);   // raw 40 (-40 °C): estrictamente por DEBAJO de ambiente (raw 70/20 °C).
            byte calorMinTempRaw = (byte)(CellGrid.AmbientRaw + CondicionMarginRaw);  // raw 100 (80 °C): estrictamente por ENCIMA de ambiente.

            // -----------------------------------------------------------------
            // AFINIDAD DEL UNIVERSO (playtest 18, corrección post-crítica de
            // diseño). DIAGNÓSTICO que motiva esto: sin afinidad, cada ley
            // sorteada elegía su producto en una bolsa uniforme de ~12
            // candidatos SIN relación con las demás leyes de la misma
            // semilla -- cada ley es coherente consigo misma pero arbitraria
            // respecto a las otras, así que el jugador no puede generalizar
            // dentro de una partida. Es "5 plantillas con sustantivos
            // intercambiables", no un universo -- y la fantasía del juego es
            // DOMESTICAR LAS LEYES DE UN UNIVERSO, no memorizar una lista de
            // accidentes.
            //
            // Sortea 1-2 materiales "afines" de entre los PRODUCTOS legales
            // (nunca Empty: "todo tiende a desaparecer" no es una tesis, es
            // una partida rota). Al elegir el producto de cada ley sorteada
            // (ver PickProductoDistintoDe/PickProductoDistintoDeAmbos más
            // abajo), la afinidad se prueba PRIMERO con ~55% de probabilidad
            // y SOLO si esa opción sigue siendo legal para ese hueco concreto
            // (ya pasó los filtros de R7/R8) -- si no lo es, o si la tirada
            // de 55% falla, se cae al sorteo uniforme de siempre. La afinidad
            // es una PREFERENCIA dentro del picker, nunca una excepción a una
            // restricción: no puede colar nada que R7/R8 no permitieran ya.
            //
            // Efecto esperado: 3-4 de las 5-8 leyes convergen en la misma
            // sustancia, así que el universo pasa a tener una TESIS ("aquí
            // todo acaba en limo", "este mundo tira a cristal") en vez de una
            // lista de productos sin relación -- generalizable leyendo un
            // puñado de leyes, sorprendente al cambiar de semilla, y sobre
            // todo NOMBRABLE, que es justo lo que Cesar lleva pidiendo desde
            // el playtest 14 ("las texturas solo me inducen a poner nombres
            // como rojo bonito"): un mundo con tendencia se puede bautizar,
            // una lista de accidentes no.
            //
            // EXPUESTA A PROPÓSITO como campo público (Universe.AfinidadDelUniverso,
            // ver el campo): el gancho evidente para una ronda futura es que
            // el rumor del Edicto la insinúe sin decirla. Esta ronda NO toca
            // ese texto -- solo deja el dato listo.
            // -----------------------------------------------------------------
            var afinidadPool = new List<byte>(ProductosPermitidos.Length);
            foreach (var p in ProductosPermitidos) if (p != MaterialId.Empty) afinidadPool.Add(p);

            int nAfinidad = rng.Next(2) == 0 ? 1 : 2; // "uno o dos materiales afines".
            var afinidadDelUniverso = new byte[nAfinidad];
            afinidadDelUniverso[0] = afinidadPool[rng.Next(afinidadPool.Count)];
            if (nAfinidad == 2)
            {
                byte segundo = afinidadDelUniverso[0];
                for (int t = 0; t < 50 && segundo == afinidadDelUniverso[0]; t++)
                    segundo = afinidadPool[rng.Next(afinidadPool.Count)];
                afinidadDelUniverso[1] = segundo; // en el peor caso (rarísimo, 50 intentos fallidos con 11 candidatos) repite el primero -- inofensivo, el picker simplemente tendría dos copias del mismo material entre las que elegir.
            }

            // -----------------------------------------------------------------
            // TINTE DE LA MAREA (CONTRATO_MAREA.md sección 3.1): tiene que
            // ocurrir AQUÍ, justo DESPUÉS de sortear afinidadDelUniverso (y
            // ANTES de que nada más la lea) -- la marea ES la química de esta
            // semilla hecha carne, así que su color se mezcla 80/20 hacia el
            // baseColor del PRIMER material afín de la run (afinidadDelUniverso[0],
            // ya resuelto a un MaterialDef real en mats[] a estas alturas: todo
            // el roster fijo se construyó arriba). Mezcla manual en vez de
            // Color32.Lerp (que no existe en UnityEngine.Color32) -- esto corre
            // UNA vez por Universe.Create, así que el coste es irrelevante.
            {
                Color32 baseMarea = mats[MaterialId.Marea].baseColor;
                Color32 afin = mats[afinidadDelUniverso[0]].baseColor;
                mats[MaterialId.Marea].baseColor = new Color32(
                    (byte)Mathf.RoundToInt(baseMarea.r * 0.8f + afin.r * 0.2f),
                    (byte)Mathf.RoundToInt(baseMarea.g * 0.8f + afin.g * 0.2f),
                    (byte)Mathf.RoundToInt(baseMarea.b * 0.8f + afin.b * 0.2f),
                    baseMarea.a); // alfa NUNCA se mezcla: la marea sigue opaca (regla 23, "lo innominado nace opaco" -- mismo criterio aunque la marea no sea innominada).
            }

            SortearLeyesGeneradas(rng, nucleoReactions, frioMaxTempRaw, calorMinTempRaw, afinidadDelUniverso,
                out Reaction[] leyesGeneradasReactions, out LeyDelUniverso[] leyesGeneradasDescriptores);

            var todasReactions = new Reaction[nucleoReactions.Length + leyesGeneradasReactions.Length];
            Array.Copy(nucleoReactions, todasReactions, nucleoReactions.Length);
            Array.Copy(leyesGeneradasReactions, 0, todasReactions, nucleoReactions.Length, leyesGeneradasReactions.Length);
            var reactionEngine = new ReactionEngine(todasReactions);

            var nucleoLeyes = ConstruirLeyesNucleo(nucleoReactions);
            // (playtest 19) 75, no 60: compensa la tasa de cultivo tras el gate de
            // "solo las puntas engendran" -- ver el docblock de Universe.VivGrowChancePct.
            // Un solo local en vez de una constante duplicada como antes: se usa
            // aquí para el DESCRIPTOR de la ley (lo que lee el diario) y se pasa
            // tal cual al constructor de Universe más abajo, así que ya no puede
            // haber una copia que quede desincronizada.
            const byte vivGrowChancePct = 75;
            var leyCrecimiento = ConstruirLeyCrecimiento(CellGrid.CToRaw(growMinC), CellGrid.CToRaw(growMaxC), vivGrowChancePct);

            int leyCrecimientoIndice = todasReactions.Length; // == reactionEngine.Count, por definición (invariante del contrato).
            var leyes = new LeyDelUniverso[todasReactions.Length + 1];
            Array.Copy(nucleoLeyes, leyes, nucleoLeyes.Length);
            Array.Copy(leyesGeneradasDescriptores, 0, leyes, nucleoLeyes.Length, leyesGeneradasDescriptores.Length);
            leyes[leyCrecimientoIndice] = leyCrecimiento;

#if UNITY_EDITOR
            // INVARIANTE DEL CONTRATO (sección 3): para i < Reactions.Count,
            // Leyes[i] describe EXACTAMENTE Reactions.At(i). Si esto falla,
            // un evento SimEventType.Ley con ese índice apuntaría a la ley
            // equivocada -- el diario anunciaría haber descubierto algo que
            // el jugador no vio, o nunca marcaría lo que sí vio.
            for (int li = 0; li < reactionEngine.Count; li++)
            {
                var r = reactionEngine.At(li);
                var l = leyes[li];
                UnityEngine.Debug.Assert(
                    l.a == r.a && l.b == r.b && l.productoA == r.productA && l.productoB == r.productB
                    && l.chancePct == r.chancePct && l.minTempRaw == r.minTempRaw && l.maxTempRaw == r.maxTempRaw,
                    $"[ChaosAlchemy] INVARIANTE ROTA: Leyes[{li}] no describe Reactions.At({li}) (ver CONTRATO_FASE3.md sección 3).");
            }
            UnityEngine.Debug.Assert(leyCrecimientoIndice == reactionEngine.Count,
                "[ChaosAlchemy] LeyCrecimientoIndice debe ser exactamente Reactions.Count.");
            UnityEngine.Debug.Assert(leyes.Length <= MaxLeyes,
                $"[ChaosAlchemy] Leyes.Length={leyes.Length} supera MaxLeyes={MaxLeyes} (R9 del contrato).");
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            {
                var sb = new System.Text.StringBuilder();
                string afinidadNombres = string.Join(", ", Array.ConvertAll(afinidadDelUniverso, id => mats[id] != null ? mats[id].devName : id.ToString()));
                sb.Append($"[ChaosAlchemy] Química de esta seed ({seed}): {leyesGeneradasDescriptores.Length} leyes sorteadas + 7 núcleo + 1 crecimiento = {leyes.Length} totales. Afinidad: {afinidadNombres}.\n");
                for (int li = 0; li < leyesGeneradasDescriptores.Length; li++)
                {
                    var l = leyesGeneradasDescriptores[li];
                    string nombreA = mats[l.a] != null ? mats[l.a].devName : l.a.ToString();
                    string nombreB = mats[l.b] != null ? mats[l.b].devName : l.b.ToString();
                    string nombrePA = mats[l.productoA] != null ? mats[l.productoA].devName : l.productoA.ToString();
                    string nombrePB = mats[l.productoB] != null ? mats[l.productoB].devName : l.productoB.ToString();
                    sb.Append($"  [{nucleoLeyes.Length + li}] {l.forma} {nombreA}+{nombreB} -> {nombrePA}+{nombrePB} | condicion={l.condicion} chance={l.chancePct}%\n");
                }
                UnityEngine.Debug.Log(sb.ToString());
            }
#endif

            string edictoDescripcion = DescribeEdicto(edicto);

            return new Universe(seed, mats, reactionEngine,
                CellGrid.CToRaw(growMinC), CellGrid.CToRaw(growMaxC), vivGrowChancePct,
                habitoTolerarVecinosPunta, habitoBifurcarPct, habitoPersistenciaPct, habitoSesgoVerticalPct,
                crystallizeMaxTempRaw, crystallizeChancePct,
                edicto, edictoDescripcion, caracterDelUniverso, firmaPorMaterial,
                leyes, leyCrecimientoIndice, afinidadDelUniverso);
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

        // ===================================================================
        // SORTEO DE LEYES POR SEED (playtest 18, CONTRATO_FASE3.md sección 6).
        // Todo este bloque corre UNA vez por Universe.Create (tiempo de
        // horneado): las asignaciones de List<>/HashSet<> de abajo son
        // aceptables por el mismo motivo que en SortearFirmasVisuales arriba
        // -- nunca corre en el hot path de SimStepper.
        //
        // Se apoya en UnnamedMaterialIds (los 6 INNOMINADOS, ya definido más
        // arriba para la firma visual) y añade el resto de los depósitos que
        // pide el contrato.
        // ===================================================================

        /// <summary>VOCABULARIO_REACTIVO del contrato: vocabulario del taller que SÍ puede aparecer como reactivo de una ley sorteada (solo si el otro lado es INNOMINADO, ver R1).</summary>
        private static readonly byte[] VocabularioReactivo =
        {
            MaterialId.Water, MaterialId.Sand, MaterialId.Oil, MaterialId.Nutrient,
            MaterialId.Ice, MaterialId.Ash, MaterialId.Steam, MaterialId.Smoke,
        };

        /// <summary>Depósito completo de reactivos permitidos en una ley sorteada: INNOMINADOS ∪ VOCABULARIO_REACTIVO. Empty/Stone/Fire (R2) nunca aparecen aquí por construcción -- no hace falta filtrarlos en el sorteo, el pool ya los excluye.</summary>
        private static readonly byte[] ReactivosPermitidos = ConcatBytes(UnnamedMaterialIds, VocabularioReactivo);

        /// <summary>
        /// Productos permitidos para una ley sorteada: los 6 INNOMINADOS +
        /// Smoke/Steam/Ash/Water/Sand/Empty (contrato sección 6).
        /// `Fire` NO es producto legal esta ronda -- IDEA DESCARTADA A
        /// PROPÓSITO (CLAUDE.md regla 15), no un olvido: una ley que prende
        /// sola es exactamente el tipo de cosa que hay que ver jugar antes de
        /// soltarla. Si una ronda futura la habilita, basta con añadir
        /// MaterialId.Fire a este array -- el resto del sorteo no necesita
        /// cambiar.
        /// </summary>
        private static readonly byte[] ProductosPermitidos =
        {
            MaterialId.Azoth, MaterialId.CrystalSeed, MaterialId.Crystal, MaterialId.Vivium, MaterialId.Slime, MaterialId.Acid,
            MaterialId.Smoke, MaterialId.Steam, MaterialId.Ash, MaterialId.Water, MaterialId.Sand, MaterialId.Empty,
        };

        /// <summary>
        /// (playtest 18, corrección post-auditoría adversarial — FALLO 1 del
        /// contrato) Materiales que salen de un GRIFO (`Game/Dispenser`,
        /// montados por `AlkahestGameBootstrap.SpawnDispensers`): Water,
        /// Sand, Oil, Nutrient desde el minuto uno, Azoth desde la jornada 2.
        /// `Dispenser.EmitTick` no tiene tope de cantidad -- cualquiera de
        /// estos cinco es una fuente infinita.
        ///
        /// La redacción original del contrato (R5) solo protegía `Water`
        /// ("el agua sale de un grifo infinito"): la RAZÓN era correcta pero
        /// la LISTA estaba incompleta -- Sand/Oil/Nutrient/Azoth salen del
        /// mismo tipo de grifo y un Contagio cuya víctima fuera cualquiera de
        /// ellos colaba exactamente el mismo bucle de materia infinita que la
        /// regla quería impedir. La restricción real nunca fue "proteger el
        /// agua", fue "lo que sale de un grifo no se acaba nunca".
        ///
        /// FUENTE DE VERDAD: `Sim/` no puede referenciar `Game/`
        /// (`AlkahestGameBootstrap` vive en la capa de arriba, Sim es la capa
        /// de abajo) así que esta lista es una COPIA A MANO de
        /// `AlkahestGameBootstrap.SpawnDispensers` y hay que mantenerla
        /// sincronizada TODA VEZ que se añada, quite o cambie un grifo ahí.
        /// SI SE DESINCRONIZA: un grifo nuevo que no se añada aquí vuelve a
        /// abrir el mismo agujero que este array corrige (una ley Contagio
        /// sorteada podría elegir esa materia como víctima infinita sin que
        /// ninguna comprobación lo impida) -- no hay ningún error de
        /// compilación ni de editor que lo señale, así que quien añada un
        /// grifo nuevo tiene que acordarse de este comentario.
        /// </summary>
        private static readonly byte[] MaterialesDeGrifo =
        {
            MaterialId.Water, MaterialId.Sand, MaterialId.Oil, MaterialId.Nutrient, MaterialId.Azoth,
        };

        /// <summary>
        /// (playtest 18, corrección post-auditoría adversarial — sustituye a
        /// la R6 original del contrato) `Vivium` y `CrystalSeed` solo pueden
        /// aparecer como reactivo de una ley SORTEADA en la posición
        /// CATALIZADORA (`b`) de una `Transmutacion` -- la única posición
        /// donde, POR DEFINICIÓN de esa forma, `productoB == b` y el material
        /// no se gasta. En cualquier otra forma, o en la posición `a` de una
        /// Transmutacion (donde si se gasta: `productoA` sustituye a `a`), el
        /// sorteo se descarta.
        ///
        /// LA R6 ORIGINAL NACIÓ INCOMPLETA: solo miraba las formas Consumo y
        /// Contagio ("el vivium es la cadena más lenta del juego, y un
        /// encargo de 'algo vivo' se vuelve imposible si algo se lo come de
        /// forma pasiva"). Pero Fusion (`A+B->C+C`) destruye los DOS
        /// reactivos por igual, Liberacion (`A+B->C+gas`) también cambia los
        /// dos lados, y en Transmutacion el vivium solo se salva la mitad de
        /// las veces (depende de si `rng.Next(2)` lo puso del lado
        /// catalizador) -- las cuatro formas que no eran Consumo/Contagio
        /// podían matarlo igual, la restricción original solo tapaba dos de
        /// las cinco puertas.
        ///
        /// POR QUÉ ESTOS DOS MATERIALES Y NO OTROS DE `UnnamedMaterialIds`:
        ///  - `Vivium`: la cadena más lenta del juego (crece consumiendo
        ///    Nutrient, GrowthTick). Un encargo de "algo vivo" se vuelve
        ///    imposible de completar si una ley sorteada lo destruye al
        ///    contacto con cualquier cosa.
        ///  - `CrystalSeed`: `MasterSupplies` entrega 60 celdas UNA VEZ, en
        ///    la jornada 2 -- no hay grifo, no se repone. La cristalización
        ///    del núcleo YA la trata como catalizador (no se gasta,
        ///    `productB == b` en la Reaction del núcleo): una ley sorteada
        ///    que SÍ la gastara (p.ej. `Fusion(CrystalSeed, Water)`) deja los
        ///    encargos de cristal imposibles en cuanto la semilla toque agua,
        ///    que está por todas partes.
        ///  - `Azoth` NO está en esta lista a propósito: tiene su propio
        ///    grifo desde la jornada 2 (`AlkahestGameBootstrap`, ver
        ///    `MaterialesDeGrifo`), así que es reponible -- perder una
        ///    cantidad no es catastrófico.
        ///  - `Crystal` NO está en esta lista a propósito: es el PRODUCTO de
        ///    la cristalización, no un suministro limitado -- si se gasta, el
        ///    jugador vuelve a fabricarlo con Azoth+CrystalSeed.
        ///  - `Slime`/`Acid` no tienen ninguna cantidad finita que proteger
        ///    (no son ingrediente de ningún encargo con suministro limitado
        ///    conocido en esta ronda), así que quedan libres para el sorteo.
        ///
        /// Es justo el tipo de restricción que alguien va a querer relajar
        /// dentro de seis meses sin saber qué sostenía: si `MasterSupplies`
        /// cambia a reponer CrystalSeed por grifo, o si el vivium deja de ser
        /// la cadena más lenta, esta lista (y el comentario) son el sitio
        /// donde reabrir la decisión con conocimiento de causa.
        /// </summary>
        private static readonly byte[] MaterialesSoloCatalizador =
        {
            MaterialId.Vivium, MaterialId.CrystalSeed,
        };

        /// <summary>Sentinela "no encontrado" para los pickers de producto de abajo. 255 no es un id de material válido (MaterialId.Count == 19, CONTRATO_MAREA.md): nunca puede confundirse con un resultado real.</summary>
        private const byte SinProductoValido = 255;

        private static byte[] ConcatBytes(byte[] a, byte[] b)
        {
            var r = new byte[a.Length + b.Length];
            Array.Copy(a, r, a.Length);
            Array.Copy(b, 0, r, a.Length, b.Length);
            return r;
        }

        /// <summary>
        /// Construye los descriptores de las 7 leyes del NÚCLEO FIJO, en el
        /// MISMO orden que <paramref name="nucleo"/> (invariante Leyes[i] ↔
        /// Reactions.At(i)). Forma/condición se escriben A MANO porque estas
        /// 7 reacciones no varían de forma entre semillas -- solo varían
        /// chancePct/banda térmica, que ya vienen en cada Reaction.
        /// </summary>
        private static LeyDelUniverso[] ConstruirLeyesNucleo(Reaction[] nucleo)
        {
            var leyes = new LeyDelUniverso[nucleo.Length];
            for (int i = 0; i < nucleo.Length; i++)
            {
                var r = nucleo[i];
                leyes[i] = new LeyDelUniverso
                {
                    a = r.a,
                    b = r.b,
                    productoA = r.productA,
                    productoB = r.productB,
                    chancePct = r.chancePct,
                    minTempRaw = r.minTempRaw,
                    maxTempRaw = r.maxTempRaw,
                    esDelNucleo = true,
                };
            }

            // [0][1] Cristalización (Azoth+Crystal, Azoth+CrystalSeed): Transmutacion,
            // el vecino Crystal/CrystalSeed es catalizador (productoB == b en ambas).
            // Solo ocurre en frío -> condicion Frio.
            leyes[0].forma = FormaDeLey.Transmutacion; leyes[0].condicion = CondicionTermica.Frio;
            leyes[1].forma = FormaDeLey.Transmutacion; leyes[1].condicion = CondicionTermica.Frio;
            // [2..5] Ácido disuelve Sand/Ash/Ice/Crystal: Consumo (Acid -> Empty,
            // el objetivo -> Smoke). Sin restricción térmica: Cualquiera.
            for (int i = 2; i <= 5; i++) { leyes[i].forma = FormaDeLey.Consumo; leyes[i].condicion = CondicionTermica.Cualquiera; }
            // [6] Ácido neutralizado por Agua -> ambos Slime: Fusion. Cualquiera.
            leyes[6].forma = FormaDeLey.Fusion; leyes[6].condicion = CondicionTermica.Cualquiera;

            return leyes;
        }

        /// <summary>
        /// La ley de crecimiento del Vivium (playtest 18): NO es una reacción
        /// de contacto (no pasa por ReactionEngine/TryReactNeighbor, vive en
        /// SimStepper.GrowthTick) pero SÍ es núcleo -- existe en TODA semilla,
        /// esDelNucleo=true. a=Vivium consume b=Nutrient; productoA=Vivium (la
        /// propia célula no cambia: sigue viva; si `grows`, aparece OTRA
        /// célula de Vivium en un vecino, ver GrowthTick); productoB=Empty (el
        /// Nutrient se consume siempre, gane o no la tirada de crecimiento).
        /// condicion=Cualquiera porque la banda real no es "frío" ni "calor"
        /// universal, depende de la seed (VivGrowMinRaw/MaxRaw, en min/maxTempRaw).
        /// </summary>
        private static LeyDelUniverso ConstruirLeyCrecimiento(byte vivGrowMinRaw, byte vivGrowMaxRaw, byte vivGrowChancePct)
        {
            return new LeyDelUniverso
            {
                a = MaterialId.Vivium,
                b = MaterialId.Nutrient,
                productoA = MaterialId.Vivium,
                productoB = MaterialId.Empty,
                forma = FormaDeLey.Crecimiento,
                condicion = CondicionTermica.Cualquiera,
                minTempRaw = vivGrowMinRaw,
                maxTempRaw = vivGrowMaxRaw,
                chancePct = vivGrowChancePct,
                esDelNucleo = true,
            };
        }

        /// <summary>
        /// Sortea entre 5 y 8 leyes propias de esta semilla (R9), aplicando
        /// las restricciones R1-R10 del contrato. Cada intento que viola
        /// cualquier restricción se DESCARTA y se reintenta (tope
        /// <see cref="MaxIntentosPorLey"/>); si un hueco agota sus intentos,
        /// esa ley simplemente no se genera (la semilla acaba con menos
        /// leyes de las pedidas) y se deja constancia en el log -- nunca se
        /// relajan las restricciones para "rellenar el hueco como sea".
        /// </summary>
        private const int MaxIntentosPorLey = 200;

        private static void SortearLeyesGeneradas(
            System.Random rng,
            Reaction[] nucleo,
            byte frioMaxTempRaw,
            byte calorMinTempRaw,
            byte[] afinidad,
            out Reaction[] leyesReactions,
            out LeyDelUniverso[] leyesDescriptores)
        {
            int objetivo = 5 + rng.Next(4); // R9: entre 5 y 8 (Next(4) = 0..3).

            // R4: ningún par sorteado puede coincidir con uno ya existente, en
            // NINGUNO de los dos órdenes -- ReactionEngine es un lookup de UNA
            // entrada por par (a*256+b Y b*256+a apuntan a la misma reacción);
            // sin esta comprobación, una segunda entrada para el mismo par
            // SOBREESCRIBIRÍA en silencio la del núcleo (la cristalización, el
            // ácido...) al construir el ReactionEngine, dejando la partida sin
            // esa ley sin ningún error visible -- el bug más peligroso de todo
            // este sorteo porque no falla nunca de forma ruidosa.
            var paresUsados = new HashSet<int>();
            foreach (var r in nucleo)
            {
                paresUsados.Add(r.a * 256 + r.b);
                paresUsados.Add(r.b * 256 + r.a);
            }

            var formasPosibles = new[]
            {
                FormaDeLey.Transmutacion, FormaDeLey.Fusion, FormaDeLey.Consumo,
                FormaDeLey.Liberacion, FormaDeLey.Contagio,
            };

            var reactionsList = new List<Reaction>(objetivo);
            var leyesList = new List<LeyDelUniverso>(objetivo);
            bool contagioUsado = false; // R5: como mucho UNA ley Contagio por semilla.

            for (int slot = 0; slot < objetivo; slot++)
            {
                bool aceptada = false;

                for (int intento = 0; intento < MaxIntentosPorLey && !aceptada; intento++)
                {
                    FormaDeLey forma = formasPosibles[rng.Next(formasPosibles.Length)];
                    if (forma == FormaDeLey.Contagio && contagioUsado) continue; // R5, ya gastada esta semilla.

                    byte a, b;
                    if (forma == FormaDeLey.Contagio)
                    {
                        // R5: `a` (el que se propaga) tiene que ser INNOMINADO.
                        a = UnnamedMaterialIds[rng.Next(UnnamedMaterialIds.Length)];
                        // R5: `b` (la víctima) nunca puede ser un material que
                        // salga de un GRIFO (MaterialesDeGrifo, ver comentario
                        // ahí arriba) -- la fuente es infinita
                        // (`Dispenser.EmitTick` no tiene tope) y un contagio
                        // que se coma lo que sale del grifo se propaga sin
                        // final por la pila. Corrección post-auditoría: la
                        // redacción original solo excluía `Water`; la razón
                        // ("grifo infinito") aplicaba igual a Sand/Oil/
                        // Nutrient/Azoth y se había quedado fuera. CORREA
                        // DELIBERADA Y AFLOJABLE (CLAUDE.md regla 15): si se
                        // ve jugar y una fuente infinita deja de ser un
                        // problema, vaciar `MaterialesDeGrifo` es el único
                        // cambio necesario.
                        byte candidatoB = SinProductoValido;
                        for (int t = 0; t < 50; t++)
                        {
                            byte cand = ReactivosPermitidos[rng.Next(ReactivosPermitidos.Length)];
                            if (cand != a && Array.IndexOf(MaterialesDeGrifo, cand) < 0) { candidatoB = cand; break; }
                        }
                        if (candidatoB == SinProductoValido) continue; // no se encontró víctima válida este intento.
                        b = candidatoB;
                    }
                    else
                    {
                        // R1: al menos uno de los dos reactivos tiene que ser
                        // INNOMINADO -- se garantiza POR CONSTRUCCIÓN: uno de
                        // los dos SIEMPRE sale de UnnamedMaterialIds, nunca de
                        // los dos a la vez del vocabulario. Esto es lo que
                        // impide que agua+arena reaccionen solas: el
                        // vocabulario solo se comporta raro en PRESENCIA de
                        // algo raro.
                        byte innominado = UnnamedMaterialIds[rng.Next(UnnamedMaterialIds.Length)];
                        byte otro = innominado;
                        for (int t = 0; t < 50; t++)
                        {
                            byte cand = ReactivosPermitidos[rng.Next(ReactivosPermitidos.Length)];
                            if (cand != innominado) { otro = cand; break; }
                        }
                        if (otro == innominado) continue; // R3: no se encontró un segundo reactivo distinto.

                        if (rng.Next(2) == 0) { a = innominado; b = otro; }
                        else { a = otro; b = innominado; }
                    }

                    // R2 (Empty/Stone/Fire nunca reactivo): GARANTIZADO por
                    // construcción -- ninguno de los dos aparece en
                    // UnnamedMaterialIds ni en ReactivosPermitidos. Se deja un
                    // assert de solo-editor como red de seguridad por si
                    // alguien amplía esos pools en el futuro sin leer este
                    // comentario.
#if UNITY_EDITOR
                    UnityEngine.Debug.Assert(a != MaterialId.Empty && a != MaterialId.Stone && a != MaterialId.Fire, "[ChaosAlchemy] R2 violada: reactivo prohibido en el pool de sorteo.");
                    UnityEngine.Debug.Assert(b != MaterialId.Empty && b != MaterialId.Stone && b != MaterialId.Fire, "[ChaosAlchemy] R2 violada: reactivo prohibido en el pool de sorteo.");
#endif

                    // R3: a != b.
                    if (a == b) continue;

                    // R4: el par no puede coincidir con ninguno ya presente, en ningún orden.
                    int key1 = a * 256 + b, key2 = b * 256 + a;
                    if (paresUsados.Contains(key1) || paresUsados.Contains(key2)) continue;

                    // R6 (corregida post-auditoría, ver MaterialesSoloCatalizador
                    // arriba para el razonamiento completo): Vivium/CrystalSeed
                    // SOLO pueden ser reactivo en la posición catalizadora (`b`)
                    // de una Transmutacion -- ahí, por definición de la forma,
                    // `productoB == b` y no se gastan. Cualquier otra
                    // combinación (otra forma, o esta forma pero en la posición
                    // `a`, que SÍ cambia) se descarta.
                    bool aProtegido = Array.IndexOf(MaterialesSoloCatalizador, a) >= 0;
                    bool bProtegido = Array.IndexOf(MaterialesSoloCatalizador, b) >= 0;
                    if (aProtegido || bProtegido)
                    {
                        bool esCatalizadorSeguro = forma == FormaDeLey.Transmutacion && bProtegido && !aProtegido;
                        if (!esCatalizadorSeguro) continue;
                    }

                    if (!TryBuildProductos(rng, afinidad, forma, a, b, out byte productoA, out byte productoB)) continue;

                    // R7: prohibido que los dos productos sean Empty (materia
                    // que desaparece sin dejar rastro es una ley que nunca
                    // entrará en el diario).
                    if (productoA == MaterialId.Empty && productoB == MaterialId.Empty) continue;

                    CondicionTermica condicion;
                    byte chancePct;
                    short minTempRaw, maxTempRaw;

                    if (forma == FormaDeLey.Contagio)
                    {
                        // R5: condicion OBLIGATORIAMENTE Frio o Calor, nunca
                        // Cualquiera; chancePct en [2,6] -- un contagio que se
                        // pudiera disparar sin aparato y a tasa alta se comería
                        // el taller antes de que el jugador entendiera qué pasó.
                        condicion = rng.Next(2) == 0 ? CondicionTermica.Frio : CondicionTermica.Calor;
                        chancePct = (byte)(2 + rng.Next(5)); // 2..6
                    }
                    else
                    {
                        condicion = SortearCondicionGeneral(rng);
                        chancePct = SortearChancePctGeneral(rng);
                    }

                    if (condicion == CondicionTermica.Frio) { minTempRaw = 0; maxTempRaw = frioMaxTempRaw; }
                    else if (condicion == CondicionTermica.Calor) { minTempRaw = calorMinTempRaw; maxTempRaw = 255; }
                    else { minTempRaw = 0; maxTempRaw = 255; }

                    var reaction = new Reaction(a, b, productoA, productoB, chancePct, minTempRaw, maxTempRaw);
                    var ley = new LeyDelUniverso
                    {
                        a = a, b = b, productoA = productoA, productoB = productoB,
                        forma = forma, condicion = condicion,
                        minTempRaw = minTempRaw, maxTempRaw = maxTempRaw,
                        chancePct = chancePct, esDelNucleo = false,
                    };

                    reactionsList.Add(reaction);
                    leyesList.Add(ley);
                    paresUsados.Add(key1);
                    paresUsados.Add(key2);
                    if (forma == FormaDeLey.Contagio) contagioUsado = true;
                    aceptada = true;
                }

                if (!aceptada)
                {
                    // El hueco se descarta: se generan menos leyes de las
                    // pedidas para esta semilla, en vez de relajar R1-R8 para
                    // forzar un hueco imposible. Registrado para poder ver,
                    // semilla a semilla, si el tope de intentos se queda corto.
                    UnityEngine.Debug.LogWarning($"[ChaosAlchemy] Sorteo de leyes: hueco {slot + 1}/{objetivo} agotó {MaxIntentosPorLey} intentos sin una combinación válida (R1-R8). Esta semilla tendrá {leyesList.Count} leyes sorteadas en vez de {objetivo}.");
                }
            }

            // R10: al menos DOS de las sorteadas tienen que tener condicion ==
            // Cualquiera. Si el sorteo no lo cumplió por azar, se fuerza
            // convirtiendo hasta 2 leyes YA ACEPTADAS (nunca Contagio, que R5
            // exige Frio/Calor) a Cualquiera -- solo se AMPLÍA su banda
            // térmica a [0,255], nunca se toca su par ni sus productos, así
            // que esto no puede reabrir ninguna de las restricciones R1-R9 ya
            // verificadas para esa ley.
            GarantizarCondicionCualquieraMinima(reactionsList, leyesList);

            leyesReactions = reactionsList.ToArray();
            leyesDescriptores = leyesList.ToArray();
        }

        /// <summary>
        /// Construye productoA/productoB para una ley sorteada según su
        /// forma. Devuelve false si no encontró una combinación válida (pool
        /// de productos agotado tras excluir lo prohibido) -- en la práctica
        /// no debería ocurrir con los 12 productos permitidos, pero el
        /// llamante trata un false como un intento fallido más (se reintenta
        /// desde SortearLeyesGeneradas).
        /// </summary>
        private static bool TryBuildProductos(System.Random rng, byte[] afinidad, FormaDeLey forma, byte a, byte b, out byte productoA, out byte productoB)
        {
            switch (forma)
            {
                case FormaDeLey.Transmutacion:
                {
                    // A+B -> C+B. B es catalizador: productoB == b es justo lo
                    // que define la forma (excepción explícita de R8).
                    // productoA (C) sí tiene que ser distinto de `a` (R8).
                    // (playtest 18) C prueba primero la AFINIDAD de la semilla.
                    byte c = PickProductoDistintoDe(rng, afinidad, a);
                    if (c == SinProductoValido) { productoA = 0; productoB = 0; return false; }
                    productoA = c;
                    productoB = b;
                    return true;
                }
                case FormaDeLey.Fusion:
                {
                    // A+B -> C+C. C distinto de `a` Y de `b` (R8 aplica a los
                    // dos lados: ambos reactivos tienen que cambiar de verdad).
                    // (playtest 18) C prueba primero la AFINIDAD de la semilla.
                    byte c = PickProductoDistintoDeAmbos(rng, afinidad, a, b);
                    if (c == SinProductoValido) { productoA = 0; productoB = 0; return false; }
                    productoA = c;
                    productoB = c;
                    return true;
                }
                case FormaDeLey.Consumo:
                {
                    // A+B -> Empty+C. productoA=Empty siempre (A se destruye;
                    // nunca puede igualar a `a`, porque `a` nunca es Empty por
                    // construcción). productoB (C) distinto de `b` (R8).
                    // (playtest 18) C prueba primero la AFINIDAD de la semilla.
                    byte c = PickProductoDistintoDe(rng, afinidad, b);
                    if (c == SinProductoValido) { productoA = 0; productoB = 0; return false; }
                    productoA = MaterialId.Empty;
                    productoB = c;
                    return true;
                }
                case FormaDeLey.Liberacion:
                {
                    // A+B -> C+gas(Smoke/Steam). productoA (C) distinto de `a`
                    // (R8). El gas tiene que ser distinto de `b` (R8): si el
                    // gas elegido al azar coincide con `b`, se usa el otro --
                    // con solo dos opciones esto siempre resuelve (b es un
                    // único valor, como mucho coincide con una de las dos).
                    // (playtest 18) C prueba primero la AFINIDAD de la semilla;
                    // el GAS no -- Smoke/Steam es un mecanismo fijo de la forma
                    // (contrato sección 6), no un "producto" sujeto a afinidad.
                    byte c = PickProductoDistintoDe(rng, afinidad, a);
                    if (c == SinProductoValido) { productoA = 0; productoB = 0; return false; }
                    byte gas = rng.Next(2) == 0 ? MaterialId.Smoke : MaterialId.Steam;
                    if (gas == b) gas = gas == MaterialId.Smoke ? MaterialId.Steam : MaterialId.Smoke;
                    productoA = c;
                    productoB = gas;
                    return true;
                }
                case FormaDeLey.Contagio:
                {
                    // A+B -> A+A (self-propagación, definición explícita de la
                    // forma: ver LeyDelUniverso.cs). productoA == a es
                    // INHERENTE a Contagio, no un incumplimiento de R8 --R8
                    // nombra explícitamente solo la excepción de Transmutacion
                    // porque es la única forma donde "un lado no cambia" es
                    // una elección de diseño y no la definición misma de la
                    // forma; en Contagio el lado A nunca cambia POR
                    // DEFINICIÓN (es lo que significa "se propaga"), así que
                    // aplicarle la misma exigencia que a Fusion/Liberacion
                    // haría que Contagio no pudiera existir nunca. productoB
                    // (=a) sí es distinto de `b` siempre, porque R3 ya
                    // garantiza a != b -- el lado B SIEMPRE cambia, que es lo
                    // que hace la ley discernible (R8, en espíritu: nunca los
                    // dos lados sin cambio).
                    productoA = a;
                    productoB = a;
                    return true;
                }
                default:
                    productoA = 0;
                    productoB = 0;
                    return false;
            }
        }

        /// <summary>Probabilidad de que un producto pruebe primero la AFINIDAD de la semilla antes del sorteo uniforme. Ver Universe.AfinidadDelUniverso / TryAfinidad.</summary>
        private const int AfinidadChancePct = 55;

        /// <summary>
        /// (playtest 18) Intenta resolver un producto con la AFINIDAD de esta
        /// semilla antes de caer al sorteo uniforme. Devuelve
        /// <see cref="SinProductoValido"/> si no aplica: sin afinidad
        /// configurada, tirada de <see cref="AfinidadChancePct"/> fallida, o
        /// el material afín NO está en <paramref name="candidatosYaFiltrados"/>
        /// -- esa lista ya tiene aplicadas las exclusiones de R7/R8 para este
        /// hueco concreto, así que esto NUNCA puede devolver algo que esas
        /// restricciones habrían rechazado: la afinidad es una preferencia
        /// DENTRO del conjunto ya legal, jamás una excepción a él.
        /// </summary>
        private static byte TryAfinidad(System.Random rng, byte[] afinidad, List<byte> candidatosYaFiltrados)
        {
            if (afinidad == null || afinidad.Length == 0) return SinProductoValido;
            if (rng.Next(100) >= AfinidadChancePct) return SinProductoValido;
            byte af = afinidad[rng.Next(afinidad.Length)];
            return candidatosYaFiltrados.Contains(af) ? af : SinProductoValido;
        }

        private static byte PickProductoDistintoDe(System.Random rng, byte[] afinidad, byte excluido)
        {
            var candidatos = new List<byte>(ProductosPermitidos.Length);
            foreach (var p in ProductosPermitidos)
            {
                if (p != excluido) candidatos.Add(p);
            }
            if (candidatos.Count == 0) return SinProductoValido;

            byte afinElegido = TryAfinidad(rng, afinidad, candidatos);
            if (afinElegido != SinProductoValido) return afinElegido;

            return candidatos[rng.Next(candidatos.Count)];
        }

        private static byte PickProductoDistintoDeAmbos(System.Random rng, byte[] afinidad, byte excluidoA, byte excluidoB)
        {
            var candidatos = new List<byte>(ProductosPermitidos.Length);
            foreach (var p in ProductosPermitidos)
            {
                if (p != excluidoA && p != excluidoB) candidatos.Add(p);
            }
            if (candidatos.Count == 0) return SinProductoValido;

            byte afinElegido = TryAfinidad(rng, afinidad, candidatos);
            if (afinElegido != SinProductoValido) return afinElegido;

            return candidatos[rng.Next(candidatos.Count)];
        }

        /// <summary>
        /// Rango de chancePct para una ley sorteada NO-Contagio: 15..60. No
        /// lo pide el contrato explícitamente (solo fija el rango de
        /// Contagio, R5) -- decisión de diseño de esta ronda, pensada para
        /// que una ley sorteada se sienta del mismo orden que las del núcleo
        /// (ver crystallizeChancePct/acidDissolveChancePct/
        /// acidNeutralizeChancePct arriba en Create(), todas en 12-40): ni
        /// tan alta que se lea como un flash instantáneo (&gt;=80%) ni tan baja
        /// que parezca que "no hace nada" en una sesión corta de playtest.
        /// </summary>
        private static byte SortearChancePctGeneral(System.Random rng)
        {
            return (byte)(15 + rng.Next(46)); // 15..60
        }

        /// <summary>Condicion para una ley sorteada NO-Contagio: tercios iguales entre Cualquiera/Frio/Calor. R10 garantiza el mínimo de Cualquiera aparte (GarantizarCondicionCualquieraMinima), así que este reparto no necesita estar sesgado.</summary>
        private static CondicionTermica SortearCondicionGeneral(System.Random rng)
        {
            int roll = rng.Next(3);
            return roll == 0 ? CondicionTermica.Cualquiera : (roll == 1 ? CondicionTermica.Frio : CondicionTermica.Calor);
        }

        /// <summary>R10: fuerza a Cualquiera hasta 2 leyes ya aceptadas (nunca Contagio) si el sorteo no llegó al mínimo por azar. Ver la nota junto a la llamada en SortearLeyesGeneradas.</summary>
        private static void GarantizarCondicionCualquieraMinima(List<Reaction> reactions, List<LeyDelUniverso> leyes)
        {
            int cualquieraCount = 0;
            for (int i = 0; i < leyes.Count; i++)
                if (leyes[i].condicion == CondicionTermica.Cualquiera) cualquieraCount++;

            int necesarias = 2 - cualquieraCount;
            for (int i = 0; i < leyes.Count && necesarias > 0; i++)
            {
                var ley = leyes[i];
                if (ley.forma == FormaDeLey.Contagio) continue; // R5: Contagio nunca puede ser Cualquiera.
                if (ley.condicion == CondicionTermica.Cualquiera) continue;

                ley.condicion = CondicionTermica.Cualquiera;
                ley.minTempRaw = 0;
                ley.maxTempRaw = 255;
                leyes[i] = ley;

                var reaccion = reactions[i];
                reaccion.minTempRaw = 0;
                reaccion.maxTempRaw = 255;
                reactions[i] = reaccion;

                necesarias--;
            }
            // Si `necesarias` sigue > 0 aquí, es porque el sorteo devolvió muy
            // pocas leyes no-Contagio en total (huecos agotados, ya
            // registrado por LogWarning más arriba): R10 queda en
            // mejor-esfuerzo, no hay ninguna ley más que convertir sin violar
            // R5.
        }
    }
}
