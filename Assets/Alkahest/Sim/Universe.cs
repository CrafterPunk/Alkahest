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

        // =================================================================
        // LO QUE PERSISTE (playtest 25, CONTRATO_PERSISTE.md sección 2) — el
        // limo primigenio y el bloque bases×estados. `Count` pasa de 17 a
        // 18 + 5*8 = 58: 41 materiales nuevos (Limo + 40 variantes), byte
        // sigue con holgura de sobra (255).
        // =================================================================
        /// <summary>El material primigenio: líquido turbio del que descienden las 5 bases de la seed (ver DescripciónEnDISENO_LO_QUE_PERSISTE.md §9). Su separación por calor es un proceso ESPECIAL de SimStepper (§4.3 del contrato), no una transición de MaterialDef.</summary>
        public const byte Limo = 17;
        /// <summary>Primera celda del bloque bases×estados. Mapeo fijo: id = BaseEstado0 + base*8 + (byte)estado (ver <see cref="EstadoMateria"/>).</summary>
        public const byte BaseEstado0 = 18;
        /// <summary>Materias base por semilla (sorteadas en <see cref="Universe.Create"/>).</summary>
        public const int BasesCount = 5;

        // -----------------------------------------------------------------
        // LA BRASA (playtest 39, contrato ENCARGO S 1b): la VEJEZ del fuego --
        // ni combustible (no arde por sí sola) ni fuego (no es la lengua
        // visible), un tercer estado con temporizador propio. Id nuevo al
        // FINAL del roster fijo (58), después del bloque bases×estados
        // (18..57): así ningún id existente se desplaza y `MatDe`/`BaseDe`/
        // `EstadoDe` (aritmética posicional sobre BaseEstado0) siguen
        // intactos sin tocar una sola línea.
        // -----------------------------------------------------------------
        public const byte Brasa = 58;

        // -----------------------------------------------------------------
        // LAS RECETAS CRUZADAS (playtest 47, ENCARGO C, CONTRATO_FASE_A.md
        // §1a/§1b): seis materiales NUEVOS, standalone (fuera del bloque
        // bases×estado -- no son (base,estado) de ninguna de las 5 bases,
        // son la MEZCLA de dos de ellas), producidos por
        // Game/Crisol.DecidirHornada cuando la cámara contiene una mezcla
        // relevante (ver Universe.TryCruce). Ids al FINAL del roster fijo,
        // igual que Brasa: así ningún id existente se desplaza y
        // MatDe/BaseDe/EstadoDe (aritmética posicional sobre BaseEstado0)
        // siguen intactos sin tocar una sola línea. Count sube 59 -> 65.
        //
        // DECISIÓN FUERA DE CONTRATO (documentada en el informe de la
        // ronda): el contrato pedía "CalizaCeramico existente" como
        // producto del cruce caliza+arcilla, pero ese id (39,
        // MatDe(2,Ceramico)) YA es el rename "cal sobrecocida" (§1c #3) --
        // usar el MISMO id para dos cosas (la caliza sobrecocida SOLA y el
        // clínker de verdad, con arcilla) habría hecho mentiroso al propio
        // rename ("cal sobrecocida: para clínker de verdad, mezcla" ya no
        // sería cierto si mezclar te devolviera EL MISMO material). Se
        // decidió el camino más simple y honesto que el propio contrato deja
        // como alternativa: <see cref="Clinker"/>, un id propio para el
        // cruce, con su color/reseña dados en el contrato.
        // -----------------------------------------------------------------
        /// <summary>"mortero" -- cal apagada + arena de sílice, cualquier fuego. Ver Universe.TryCruce.</summary>
        public const byte Mortero = 59;
        /// <summary>"vidrio de botella" -- arena de sílice + ceniza, fuego pleno (funde a banda MÁS BAJA que la fusión pura de la arena: la potasa real baja el punto de fusión). Ver Universe.TryCruce.</summary>
        public const byte VidrioVerde = 60;
        /// <summary>"lejía de ceniza" -- ceniza + agua, fuego bajo. Ver Universe.TryCruce.</summary>
        public const byte Lejia = 61;
        /// <summary>"hormigón" -- clínker + arena de sílice, fuego bajo. Ver Universe.TryCruce.</summary>
        public const byte Hormigon = 62;
        /// <summary>"cerámica esmaltada" -- bizcocho + arena de sílice, fuego pleno. Ver Universe.TryCruce.</summary>
        public const byte Esmaltado = 63;
        /// <summary>"clínker" -- caliza molida + arcilla, fuego pleno. El id PROPIO del cruce (ver el bloque de comentarios de arriba); NO confundir con MatDe(2,Ceramico) ("cal sobrecocida", la caliza sobrecocida SOLA). Ver Universe.TryCruce.</summary>
        public const byte Clinker = 64;

        public const int Count = 65; // 18 + 5*8 + 1 (Brasa) + 6 (recetas cruzadas, playtest 47)

        /// <summary>true si `id` cae dentro del bloque bases×estados (18..57).</summary>
        public static bool EsBaseEstado(byte id) => id >= BaseEstado0 && id < BaseEstado0 + BasesCount * 8;

        /// <summary>Índice de base (0..4) de un id del bloque bases×estados. No valida el rango -- llamar solo tras comprobar <see cref="EsBaseEstado"/>.</summary>
        public static int BaseDe(byte id) => (id - BaseEstado0) / 8;

        /// <summary>Estado de un id del bloque bases×estados. No valida el rango -- llamar solo tras comprobar <see cref="EsBaseEstado"/>.</summary>
        public static EstadoMateria EstadoDe(byte id) => (EstadoMateria)((id - BaseEstado0) % 8);

        /// <summary>Construye el id de (base, estado) según el mapeo fijo del contrato.</summary>
        public static byte MatDe(int baseIdx, EstadoMateria estado) => (byte)(BaseEstado0 + baseIdx * 8 + (byte)estado);
    }

    /// <summary>
    /// Los 8 estados canónicos del retículo de "lo que persiste" (playtest 25,
    /// CONTRATO_PERSISTE.md sección 2). Cada (base, estado) es un MaterialId
    /// propio, generado en bucle en <see cref="Universe.Create"/> desde
    /// tablas por seed (cero código por-material) -- el orden/valor de este
    /// enum ES el mapeo fijo que usa <see cref="MaterialId.MatDe"/>, no
    /// reordenar.
    /// </summary>
    public enum EstadoMateria : byte
    {
        Polvo = 0,     // estado natal (Powder)
        Fundido = 1,   // líquido incandescente (Liquid, brilla)
        Templado = 2,  // enfriado RÁPIDO en el mundo: duro (StaticSolid)
        Recocido = 3,  // enfriado LENTO dentro del crisol: dúctil (StaticSolid)
        Compacto = 4,  // prensado (StaticSolid)
        Ceramico = 5,  // compacto cocido: el techo de resistencia (StaticSolid)
        Calcinado = 6, // tostado sin fundir (Powder; a veces combustible)
        Solucion = 7,  // disuelto en agua (Liquid, agua teñida del color de la base)
    }

    /// <summary>Cómo responde un material a la Prensa (playtest 25, Encargo B). Nuevo enum en Alkahest.Sim, archivo Universe.cs por contrato.</summary>
    public enum RespuestaPrensa : byte
    {
        Nada = 0,       // materiales ajenos (piedra, etc.): intocados.
        Compactar = 1,  // pasa al estado Compacto de su base.
        Reventar = 2,   // pasa al estado Polvo (frágil: el templado revienta).
        Escupir = 3,    // se desplaza a la celda libre lateral más cercana (líquidos: no se comprimen).
        Resistir = 4,   // nada, y el rótulo lo dice ("resiste la prensa": dato ganado).
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

        // ===================================================================
        // LO QUE PERSISTE (playtest 25, CONTRATO_PERSISTE.md sección 3) — la
        // API pública de tablas/retículo/garantía. Las tablas están indexadas
        // por MaterialId completo (0..Count-1, ver los métodos de abajo) o por
        // baseIdx (0..BasesCount-1); se sortean en <see cref="Create"/> (ver
        // el bloque "SORTEO DE PERSISTENCIA" y sus métodos privados) y quedan
        // congeladas para toda la partida, igual que Leyes/AfinidadDelUniverso.
        // ===================================================================

        /// <summary>El rescoldo propio del Crisol sin combustible: ~116°C, hierve agua y limo, no funde nada (FusionRaw mínimo de una base es 130). La lee B (Game/Crisol.cs).</summary>
        // (fix integración) 120, no los 118 del contrato original: el agua de la
        // seed hierve a 80..118 °C = raw 100..119 (ver waterBoilC más abajo), así
        // que con 118 el rescoldo NO llegaba a hervir agua/soluciones en el peor
        // sorteo -- y evaporar-para-recristalizar es bucle nuclear del juego, no
        // puede depender de la suerte de la seed. 120 cubre el peor caso (119) y
        // sigue por debajo de toda fusión (raw >= 130): el tier 0 hierve TODO lo
        // acuoso en toda semilla y no funde nada en ninguna. Regla 50: el valor
        // se deriva del código que lo consume, no del nombre.
        public const byte CrisolTier0Raw = 120;

        /// <summary>
        /// (playtest 40, CONTRATO_SEMILLA.md §3) LA SEMILLA DE AUTOR de
        /// "SEMILLA CERO — tu primer taller". Congelada tras verificar con el
        /// arnés headless (<c>Tools~/BenchSim/Harness.cs</c>, ver el informe
        /// de la ronda) que, combinada con <see cref="AplicarOverridesSemillaCero"/>,
        /// cumple 1-4 del contrato. NO es 777001 (el valor sugerido en el
        /// contrato como punto de partida): con esa seed
        /// <c>GanadorGarantizado</c> y <c>BaseCombustibleGarantizada</c>
        /// coinciden en la MISMA base (b0==b1==3) -- la trampa del beat 4
        /// necesita un combustible YA obtenible (el de <c>BaseCombustibleGarantizada</c>)
        /// DISTINTO de la base que se está intentando calcinar, o no hay con
        /// qué "alimentar el brasero" antes de tener la propia calcinada (el
        /// problema del huevo y la gallina). 777002 sí separa ambas
        /// (GanadorGarantizado en base 1, BaseCombustibleGarantizada en base
        /// 3) -- verificado imprimiendo <c>Universe.Create(777002)</c> con el
        /// arnés. Documentado también en <see cref="AplicarOverridesSemillaCero"/>.
        /// </summary>
        public const uint SemillaCero = 777002u;

        private readonly byte[] _umbralPersistenciaRaw;      // [MaterialId.Count]
        private readonly RespuestaPrensa[] _prensaPorMaterial; // [MaterialId.Count]
        private readonly byte[] _conductividadPorMaterial;    // [MaterialId.Count]
        private readonly bool[] _solubleEnAguaPorMaterial;    // [MaterialId.Count]
        private readonly bool[] _esCombustiblePorMaterial;    // [MaterialId.Count]
        private readonly byte[] _tempCombustibleRawPorMaterial; // [MaterialId.Count]

        private readonly byte[] _fusionRaw;      // [BasesCount]
        private readonly byte[] _calcinacionRaw; // [BasesCount]
        private readonly byte[] _ceramizaRaw;    // [BasesCount], 0 = esta base no ceramiza
        private readonly byte[] _solidificaRaw;  // [BasesCount]
        private readonly int[] _pesoEnLimo;      // [BasesCount], suman 100
        private readonly byte[] _extraccionRaw;  // [BasesCount] (playtest 27), bandas ASCENDENTES: ver ExtraccionRaw

        /// <summary>matId del persistente garantizado de esta seed (solver de garantía, ver Create -> ResolverGarantiaPersistencia). Alcanzable en ≤50 reintentos de tabla o clampeado en el último.</summary>
        public readonly byte GanadorGarantizado;
        /// <summary>Temp del pedido CALOR (raw 165..180), SIEMPRE por debajo de UmbralPersistenciaRaw(GanadorGarantizado) - 10.</summary>
        public readonly byte TempEnsayoCalorRaw;
        /// <summary>baseIdx cuyo Calcinado es combustible alcanzable con el rescoldo tier0 (CalcinacionRaw &lt;= CrisolTier0Raw) — la escalera hervir→calcinar→combustible→fundir existe en toda seed.</summary>
        public readonly int BaseCombustibleGarantizada;

        private Universe(int seed, MaterialDef[] materials, ReactionEngine reactions,
            byte vivGrowMinRaw, byte vivGrowMaxRaw, byte vivGrowChancePct,
            byte habitoTolerarVecinosPunta, byte habitoBifurcarPct, byte habitoPersistenciaPct, sbyte habitoSesgoVerticalPct,
            byte crystallizeMaxTempRaw, byte crystallizeChancePct,
            Edicto edicto, string edictoDescripcion, string caracterDelUniverso, string[] firmaPorMaterial,
            LeyDelUniverso[] leyes, int leyCrecimientoIndice, byte[] afinidadDelUniverso,
            byte[] umbralPersistenciaRaw, RespuestaPrensa[] prensaPorMaterial, byte[] conductividadPorMaterial,
            bool[] solubleEnAguaPorMaterial, bool[] esCombustiblePorMaterial, byte[] tempCombustibleRawPorMaterial,
            byte[] fusionRaw, byte[] calcinacionRaw, byte[] ceramizaRaw, byte[] solidificaRaw, int[] pesoEnLimo,
            byte[] extraccionRaw,
            byte ganadorGarantizado, byte tempEnsayoCalorRaw, int baseCombustibleGarantizada)
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
            _umbralPersistenciaRaw = umbralPersistenciaRaw;
            _prensaPorMaterial = prensaPorMaterial;
            _conductividadPorMaterial = conductividadPorMaterial;
            _solubleEnAguaPorMaterial = solubleEnAguaPorMaterial;
            _esCombustiblePorMaterial = esCombustiblePorMaterial;
            _tempCombustibleRawPorMaterial = tempCombustibleRawPorMaterial;
            _fusionRaw = fusionRaw;
            _calcinacionRaw = calcinacionRaw;
            _ceramizaRaw = ceramizaRaw;
            _solidificaRaw = solidificaRaw;
            _pesoEnLimo = pesoEnLimo;
            _extraccionRaw = extraccionRaw;
            GanadorGarantizado = ganadorGarantizado;
            TempEnsayoCalorRaw = tempEnsayoCalorRaw;
            BaseCombustibleGarantizada = baseCombustibleGarantizada;
        }

        public MaterialDef Get(byte id) => Materials[id];

        // ---- API pública de tablas (CONTRATO_PERSISTE.md sección 3) ----
        /// <summary>Temp raw máxima que `id` aguanta sin transformar/arder (dato de tabla, usado por el Ensayo y el solver — no siempre coincide con un campo de MaterialDef, ver comentario en ConstruirUmbralPersistencia).</summary>
        public byte UmbralPersistenciaRaw(byte id) => id < _umbralPersistenciaRaw.Length ? _umbralPersistenciaRaw[id] : (byte)255;
        public RespuestaPrensa Prensa(byte id) => id < _prensaPorMaterial.Length ? _prensaPorMaterial[id] : RespuestaPrensa.Nada;
        public byte Conductividad(byte id) => id < _conductividadPorMaterial.Length ? _conductividadPorMaterial[id] : (byte)0;
        public bool SolubleEnAgua(byte id) => id < _solubleEnAguaPorMaterial.Length && _solubleEnAguaPorMaterial[id];
        public bool EsCombustible(byte id) => id < _esCombustiblePorMaterial.Length && _esCombustiblePorMaterial[id];
        public byte TempCombustibleRaw(byte id) => id < _tempCombustibleRawPorMaterial.Length ? _tempCombustibleRawPorMaterial[id] : (byte)0;

        public byte FusionRaw(int baseIdx) => _fusionRaw[baseIdx];
        public byte CalcinacionRaw(int baseIdx) => _calcinacionRaw[baseIdx];
        public byte CeramizaRaw(int baseIdx) => _ceramizaRaw[baseIdx];
        public byte SolidificaRaw(int baseIdx) => _solidificaRaw[baseIdx];
        public int PesoEnLimo(int baseIdx) => _pesoEnLimo[baseIdx];

        /// <summary>
        /// (playtest 27, CONTRATO_TALLER_GRANDE mandato 4) LA BANDA DE
        /// EXTRACCIÓN de una base: la temperatura raw a partir de la cual el
        /// limo suelta ESA base en el crisol. Las cinco bandas son
        /// ASCENDENTES y disjuntas por seed, y una hornada de limo produce
        /// SOLO la base más alta cuya banda quepa en la temperatura de esa
        /// pasada (ver Game/Crisol.DecidirHornada) -- de ahí "una base por
        /// hornada, ligada al combustible".
        ///
        /// GARANTIZADO POR EL SOLVER en toda seed: la banda más baja está por
        /// debajo de <see cref="CrisolTier0Raw"/> (con el fuego bajo SIEMPRE
        /// sale una base, la primera, sin necesitar combustible ninguno) y
        /// TODAS las bandas quedan por debajo del mejor combustible
        /// alcanzable (ninguna base es contenido muerto). Ver
        /// EvaluarGarantia, garantías 1 y 4.
        ///
        /// POR QUÉ ES ESTO Y NO OTRA COSA: es la intuición que Cesar formuló
        /// solo jugando el 26 -- *"pensé que estaría en relación al nivel de
        /// combustible, siendo que algunos llegan a temperaturas más altas"*.
        /// Cuando el jugador ya ha adivinado tu mecánica, la mecánica correcta
        /// es la que él adivinó.
        /// </summary>
        public byte ExtraccionRaw(int baseIdx) => _extraccionRaw[baseIdx];

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
                burnsInto = MaterialId.Fire, // camino legado: sin efecto mientras combustReserva>0 (ver TryIgnite/ApplyPhase), documentado por si algún día se apaga el sistema nuevo para este material.
                // -----------------------------------------------------------------
                // EL PATRÓN ORO (playtest 39, contrato ENCARGO S 1a): "un charco
                // encendido debe arder DECENAS de segundos consumiéndose
                // visiblemente desde el borde encendido". Con combustPasoTicks=8
                // (potencia de 2, máscara barata) y combustReserva=120 (el tope
                // real es 127: 7 bits libres de aux en un Liquid, ver
                // MaterialDef.combustReserva), el CENTRO de la duración es
                // 120*8=960 ticks = 32s a 30Hz -- "decenas de segundos" con
                // margen a los dos lados. combustResiduo=Empty: el aceite arde
                // SIN dejar nada sólido (todo el rastro es el humo ya emitido
                // durante la quema + la pátina que deja SimRenderer en la piedra
                // de alrededor).
                combustReserva = 120,
                combustPasoTicks = 8,
                combustCalorRaw = 12,
                combustHumoPct = 8,
                combustPropagacionPct = 15, // mismo orden que el 12% del TryIgnite legado: el frente avanza, no explota (fix playtest 9 sigue vigente).
                combustLenguaPct = 35,
                combustResiduo = MaterialId.Empty,
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
                caeSolido = true, cohesionCeldas = 4, // (playtest 29) el hielo cae al perder apoyo -- los puentes helados piden arte, no fe.
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
                caeSolido = true, cohesionCeldas = 5, // (playtest 29) el cristal crecido sin apoyo se desprende: las torres piden base.
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
            // LA BRASA (playtest 39, contrato ENCARGO S 1b). VOCABULARIO DEL
            // TALLER (regla 17 de CLAUDE.md): se ve SIEMPRE igual en toda
            // partida -- es un fenómeno mundano como Fire/Smoke/Ash/Steam, no
            // algo que el jugador bautiza. `patron=Liso` con `emitsGlow=true`
            // (regla 17: Liso es lo único que puede llevar el vocabulario).
            //
            // DECISIÓN (contrato deja abierto "no cae -- o cae como polvo,
            // decide S"): archetype=Powder, cae como la Ceniza en la que se
            // convierte -- una brasa suelta es literalmente un trozo de
            // combustible a medio consumir, del mismo orden físico que la
            // ceniza, no una losa. `aux` en Powder está libre entero (ver
            // SimStepper), así que su cuenta atrás de vida no colisiona con
            // nada.
            //
            // Color rescoldo naranja-rojo APAGADO (nunca blanco, contrato
            // 1b): mucho más oscuro/rojo que Fire (255,140,40) para que se
            // lea como "lo que queda", no como llama nueva. `emitsGlow=true`
            // le da el parpadeo determinista genérico de ComputeCellColor
            // (mismo mecanismo que Fire/Vivium/Crystal, sin código nuevo en
            // el renderer) y patron=Pulso (con patronFuerza/ritmoAnim bajos)
            // añade la respiración lenta y SUTIL que pide el contrato --
            // Pulso no dibuja textura de posición, solo hace latir el brillo
            // (ver PatronMorfologico.Pulso), calibrado suave a propósito
            // (fuerza 45 frente a los 90 típicos de Motas/Manchas) para que
            // sea "sutil", no un faro.
            mats[MaterialId.Brasa] = new MaterialDef
            {
                id = MaterialId.Brasa,
                devName = "Brasa",
                archetype = MaterialArchetype.Powder,
                baseColor = new Color32(150, 58, 24, 255),
                colorJitter = 18,
                density = 110, // ligeramente menos densa que Ash (120): un residuo aún más suelto, a medio consumir.
                fluidity = 1,
                emitsGlow = true,
                patron = PatronMorfologico.Pulso,
                borde = BordeMorfologico.Neto,
                patronFuerza = 45,
                ritmoAnim = 14,
                // gasLifetime reutilizado como SEMILLA de vida (unidades de
                // SimStepper.BrasaLifeUnitTicks ticks cada una, ver
                // SimStepper.ConvertirEnBrasa): 75 unidades * 4 ticks/unidad
                // = 300 ticks = 10s a 30Hz, el CENTRO del rango 8-12s del
                // contrato; ConvertirEnBrasa añade jitter ±15 unidades
                // (60..90 -> 8..12s) con su propia sal, así que este valor es
                // solo el punto medio documentado, no el rango real.
                gasLifetime = 75,
            };

            // ===================================================================
            // LAS RECETAS CRUZADAS (playtest 47, ENCARGO C). Seis materiales
            // NUEVOS con IDENTIDAD REAL propia (no sorteada, no jitter de
            // patrón como lo innominado -- mismo trato que el vocabulario del
            // taller y Brasa, regla 17 de CLAUDE.md: se ven IGUAL en toda
            // partida). Colores/arquetipos/físicas VERBATIM del contrato
            // donde el contrato los da; el resto es DECISIÓN de C, documentada
            // aquí y en el informe de la ronda:
            //  - Mortero/Hormigon/Clinker/Esmaltado: StaticSolid con COHESIÓN
            //    (regla 7 de CLAUDE.md, "los productos sólidos del retículo
            //    SÍ caen al perder apoyo, con cohesión") -- "el hormigón
            //    aguanta MÁS que el mortero" se cumple en dos ejes: cohesión
            //    física (7 vs 5, hormigón voladiza más) Y persistencia térmica
            //    (ver RellenarPersistenciaCruces más abajo). Clinker (8) es el
            //    más duro de los cuatro -- caliza y arcilla cocidas a fuego
            //    pleno, el techo de la familia, igual que el Cerámico real.
            //  - VidrioVerde: StaticSolid cohesión 3, LITERAL del contrato
            //    ("física del Templado").
            //  - Lejia: Liquid, como cualquier disolución.
            //  - RESPUESTA A LA PRENSA: los cinco sólidos usan Resistir, no
            //    Compactar/Reventar -- Game/Prensa.cs (fuera de este encargo)
            //    solo aplica esas dos respuestas a ids EsBaseEstado
            //    (AplicarRespuesta/MaterialSalida gatean por eso), así que
            //    para un id standalone como estos, Reventar/Compactar serían
            //    un rótulo mentiroso sin efecto real. Resistir además ALIMENTA
            //    la resistencia anotada del propio encargo ("resiste la
            //    prensa", ver SubstanceKnowledge.RegistrarResistePrensa) --
            //    coherente con que son productos YA CURADOS, no materia prima.
            //    Lejia (líquido) usa Escupir, mismo criterio que Agua/
            //    Fundido/Solución (los líquidos no se comprimen).
            // ===================================================================
            mats[MaterialId.Mortero] = new MaterialDef
            {
                id = MaterialId.Mortero,
                devName = "Mortero",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(200, 196, 184, 255),
                colorJitter = 10,
                density = 210,
                caeSolido = true,
                cohesionCeldas = 5,
                patron = PatronMorfologico.Liso,
                borde = BordeMorfologico.Neto,
            };
            mats[MaterialId.VidrioVerde] = new MaterialDef
            {
                id = MaterialId.VidrioVerde,
                devName = "VidrioVerde",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(110, 160, 120, 255),
                colorJitter = 10,
                density = 195,
                // "física del Templado" (contrato §1a, literal): cae con
                // cohesión 3 -- el mismo número que el Templado real
                // (MaterialDef del roster base×estado, EstadoMateria.Templado).
                caeSolido = true,
                cohesionCeldas = 3,
                patron = PatronMorfologico.Liso,
                borde = BordeMorfologico.Halo,
            };
            mats[MaterialId.Lejia] = new MaterialDef
            {
                id = MaterialId.Lejia,
                devName = "Lejia",
                archetype = MaterialArchetype.Liquid,
                baseColor = new Color32(210, 205, 180, 255),
                colorJitter = 10,
                density = (short)(liquidDensity[MaterialId.Water] + 15), // agua con potasa disuelta: algo más densa que el agua pura.
                fluidity = 4,
                // Mismo lenguaje visual que las Soluciones reales
                // (ConstruirEstadosDerivados, "disolución visible", playtest
                // 20): Motas + borde Difuso, patronFuerza 90 -- es LITERALMENTE
                // lo mismo (algo disuelto en agua), así que hereda su firma.
                patron = PatronMorfologico.Motas,
                borde = BordeMorfologico.Difuso,
                patronEscala = 2,
                patronFuerza = 90,
                ritmoAnim = 30,
            };
            mats[MaterialId.Hormigon] = new MaterialDef
            {
                id = MaterialId.Hormigon,
                devName = "Hormigon",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(168, 164, 156, 255),
                colorJitter = 10,
                density = 230,
                caeSolido = true,
                cohesionCeldas = 7, // más que el mortero (5): "el hormigón aguanta más" (contrato §1a), verdad real.
                // Vetas sutiles (decisión de C): el árido grueso del hormigón
                // real se lee como jaspeado, a diferencia de la pasta lisa
                // del mortero -- diferenciación visual barata (Vetas es
                // puramente posicional, coste cero en el stepper, regla 16).
                patron = PatronMorfologico.Vetas,
                borde = BordeMorfologico.Neto,
                patronEscala = 3,
                patronFuerza = 55,
            };
            mats[MaterialId.Esmaltado] = new MaterialDef
            {
                id = MaterialId.Esmaltado,
                devName = "Esmaltado",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(196, 120, 88, 255),
                colorJitter = 10,
                density = 220,
                caeSolido = true,
                cohesionCeldas = 6, // LITERAL del contrato §1a.
                // Escarcha: el brillo vítreo del esmalte sobre la superficie.
                patron = PatronMorfologico.Liso,
                borde = BordeMorfologico.Escarcha,
            };
            mats[MaterialId.Clinker] = new MaterialDef
            {
                id = MaterialId.Clinker,
                devName = "Clinker",
                archetype = MaterialArchetype.StaticSolid,
                baseColor = new Color32(150, 145, 138, 255), // LITERAL del contrato (puntos finos, ENCARGO C).
                colorJitter = 12,
                density = 235,
                caeSolido = true,
                cohesionCeldas = 8, // el techo de la familia: caliza y arcilla cocidas a fuego pleno, tan duro como el Cerámico real.
                patron = PatronMorfologico.Liso,
                borde = BordeMorfologico.Neto,
            };

            // ===================================================================
            // LO QUE PERSISTE (playtest 25, CONTRATO_PERSISTE.md sección 4) — el
            // limo primigenio + las 40 variantes base×estado, generadas EN
            // BUCLE desde tablas sorteadas por seed (cero código por-material,
            // ver ResolverPersistencia/ConstruirPolvoBases/
            // ConstruirEstadosDerivados más abajo). Dos pasadas de MaterialDef
            // alrededor de SortearFirmasVisuales: los 5 Polvo ANTES (entran al
            // sorteo de firma visual como innominados nuevos, contrato 4.1) y
            // los 35 estados derivados DESPUÉS (tiñen su color desde el tono
            // final que le tocó a su Polvo -- no pueden sortearse antes de que
            // ese tono exista).
            // -----------------------------------------------------------------
            mats[MaterialId.Limo] = new MaterialDef
            {
                id = MaterialId.Limo,
                devName = "Limo",
                archetype = MaterialArchetype.Liquid,
                // Turbio pardo-grisáceo FIJO en toda seed: el primigenio se
                // reconoce entre universos, la MISMA excepción documentada a
                // la regla 17 de CLAUDE.md que ya vale para el vocabulario del
                // taller (contrato sección 4.1) -- por eso NO entra en
                // UnnamedMaterialIds/SortearFirmasVisuales.
                // (playtest 26, fix con capturas) Antes (94,86,72), un pardo
                // neutro que A ESCALA DE JUEGO se confundía con la piedra: en
                // la verificación visual de esta ronda un lago entero de limo
                // se leía como suelo de roca. Ahora un VERDE OLIVA turbio,
                // inconfundible con la piedra gris y con cualquier líquido del
                // vocabulario (agua azul, aceite negro-pardo) -- sigue siendo
                // "turbio y humilde" (es barro primigenio, no una gema) pero
                // se reconoce de un vistazo, que es su único trabajo.
                baseColor = new Color32(88, 96, 52, 255),
                colorJitter = 16,
                // Densidad entre agua y aceite (contrato 4.1): la media de
                // liquidDensity[Water]/[Oil], ya sorteadas arriba -- así el
                // limo estratifica de forma legible frente a los dos líquidos
                // del vocabulario en TODA seed, sea cual sea su reparto.
                density = (short)((liquidDensity[MaterialId.Water] + liquidDensity[MaterialId.Oil]) / 2),
                fluidity = 2,
                patron = PatronMorfologico.Motas,
                borde = BordeMorfologico.Neto,
                patronEscala = 3,
                patronFuerza = 90,
                ritmoAnim = 20,
                emision = 0,
                semillaPatron = (byte)rng.Next(256),
                // Sin transiciones de MaterialDef (contrato 4.1): su
                // separación por calor es el proceso ESPECIAL de SimStepper
                // (sal SalLimoSeparacion, contrato 4.3), no fusión/ebullición
                // genérica.
            };

            byte waterBoilsAtRaw = CellGrid.CToRaw(waterBoilC);
            ResolverPersistencia(mats, rng, waterBoilsAtRaw,
                out PersistenciaTablas tablasPersistencia,
                out byte[] umbralPersistenciaRaw, out RespuestaPrensa[] prensaPorMaterial,
                out byte[] conductividadPorMaterial, out bool[] solubleEnAguaPorMaterial,
                out bool[] esCombustiblePorMaterial, out byte[] tempCombustibleRawPorMaterial,
                out byte ganadorGarantizado, out byte tempEnsayoCalorRaw, out int baseCombustibleGarantizada);

            // (playtest 47, ENCARGO C) Los 6 ids de las recetas cruzadas
            // (59..64) quedan FUERA de los dos bucles de ResolverPersistencia
            // (0..BaseEstado0-1 = vocabulario, y el bloque bases×estado):
            // exactamente el mismo hueco que ya dejaba Brasa (58, nunca
            // relleno tampoco). Se rellenan aquí, a mano, sobre los arrays ya
            // devueltos -- ver el docblock de RellenarPersistenciaCruces.
            RellenarPersistenciaCruces(umbralPersistenciaRaw, prensaPorMaterial,
                conductividadPorMaterial, solubleEnAguaPorMaterial,
                esCombustiblePorMaterial, tempCombustibleRawPorMaterial);

            ConstruirPolvoBases(mats, rng, tablasPersistencia);

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
            // (playtest 25) UnnamedMaterialIds ahora incluye las 5 bases en
            // Polvo (contrato 4.1) -- la maquinaria de abajo no cambia, todo
            // deriva de UnnamedMaterialIds.Length, así que ampliar ese array
            // (ver su declaración) basta para que las 3 garantías/arrays
            // paralelos de SortearFirmasVisuales se amplíen solos.
            SortearFirmasVisuales(mats, rng, out string caracterDelUniverso, out string[] firmaPorMaterial);

            // (playtest 25) Los 35 estados derivados: ahora sí, con el tono
            // final de cada Polvo ya fijado por SortearFirmasVisuales.
            ConstruirEstadosDerivados(mats, rng, tablasPersistencia, waterBoilsAtRaw,
                mats[MaterialId.Water].baseColor, liquidDensity[MaterialId.Water]);

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
                leyes, leyCrecimientoIndice, afinidadDelUniverso,
                umbralPersistenciaRaw, prensaPorMaterial, conductividadPorMaterial,
                solubleEnAguaPorMaterial, esCombustiblePorMaterial, tempCombustibleRawPorMaterial,
                tablasPersistencia.FusionRaw, tablasPersistencia.CalcinacionRaw, tablasPersistencia.CeramicoUmbral,
                tablasPersistencia.SolidificaRaw, tablasPersistencia.PesoEnLimo,
                tablasPersistencia.ExtraccionRaw,
                ganadorGarantizado, tempEnsayoCalorRaw, baseCombustibleGarantizada);
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
        /// Los innominados de esta run (ver CLAUDE.md regla 13): los 6
        /// originales + (playtest 25, contrato sección 4.1) las 5 bases en
        /// estado Polvo -- "las 5 bases en estado Polvo entran al sorteo de
        /// firma visual como innominados nuevos". TODA la maquinaria de abajo
        /// (SortearFirmasVisuales y sus arrays paralelos: hueSlots,
        /// procOrder, arquetipos, patronPorIdx, huePorIdx...) deriva su
        /// tamaño de <c>UnnamedMaterialIds.Length</c>, así que ampliar este
        /// array basta -- ninguna otra línea de ese método necesitó tocarse.
        /// El vocabulario del taller (Stone/Sand/Water/Oil/Nutrient/Fire/
        /// Smoke/Ash/Steam/Ice) y el Limo (excepción documentada aparte, ver
        /// su MaterialDef en Create) NO pasan por aquí: se quedan con los
        /// valores por defecto de MaterialDef (patron=Liso, patronFuerza=0,
        /// borde=Neto) y el baseColor tal cual se definió arriba, sin tocar
        /// un solo byte — es el suelo firme desde el que el jugador juzga
        /// todo lo demás. (Se valoró darles un patrón fijo MUY tenue idéntico
        /// en toda seed —la excepción que el diseño permite— pero se
        /// descartó: con patronFuerza en 0 el campo `patron` es inerte de
        /// todas formas, así que la "excepción" no aportaba nada que un
        /// `patronFuerza` en 0 no garantizara ya, y cada byte que se toca
        /// aquí es un byte más que vigilar si mañana alguien reintroduce
        /// jitter global por error.)
        /// </summary>
        private static readonly byte[] UnnamedMaterialIds =
        {
            MaterialId.Azoth, MaterialId.CrystalSeed, MaterialId.Crystal,
            MaterialId.Vivium, MaterialId.Slime, MaterialId.Acid,
            MaterialId.MatDe(0, EstadoMateria.Polvo), MaterialId.MatDe(1, EstadoMateria.Polvo),
            MaterialId.MatDe(2, EstadoMateria.Polvo), MaterialId.MatDe(3, EstadoMateria.Polvo),
            MaterialId.MatDe(4, EstadoMateria.Polvo),
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

        /// <summary>Sentinela "no encontrado" para los pickers de producto de abajo. 255 no es un id de material válido (MaterialId.Count == 58 desde CONTRATO_PERSISTE.md, antes 17): nunca puede confundirse con un resultado real.</summary>
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

        // ===================================================================
        // LO QUE PERSISTE (playtest 25, CONTRATO_PERSISTE.md secciones 4.2 y
        // 4.4) — tablas de propiedades por seed + el solver de garantía.
        // Todo este bloque corre UNA vez por Universe.Create (mismo criterio
        // que el sorteo de leyes/firma visual arriba: List<>/Queue<> aquí son
        // aceptables, esto no es hot-path). Puro DATO -- ningún método de
        // aquí abajo toca MaterialDef/mats[]; eso lo hacen por separado
        // ConstruirPolvoBases/ConstruirEstadosDerivados, más abajo del todo.
        // ===================================================================

        /// <summary>Un candidato de tabla de persistencia (contrato 4.2): vector por base + modificadores de estado ya resueltos a valores absolutos. Puro dato, sin depender de MaterialDef -- por eso reintentar el sorteo dentro del solver (ResolverPersistencia) es barato.</summary>
        private sealed class PersistenciaTablas
        {
            public readonly byte[] FusionRaw = new byte[MaterialId.BasesCount];
            public readonly byte[] CalcinacionRaw = new byte[MaterialId.BasesCount];
            public readonly byte[] SolidificaRaw = new byte[MaterialId.BasesCount];
            /// <summary>Umbral de persistencia del Compacto de esta base (FusionRaw + 10..20, contrato 4.2).</summary>
            public readonly byte[] CompactoUmbral = new byte[MaterialId.BasesCount];
            /// <summary>CeramizaRaw del contrato: temp a la que Compacto->Ceramico Y umbral de persistencia del Ceramico resultante (mismo número a propósito: es a la vez el umbral que hay que cruzar y lo que aguanta después). 0 = esta base no ceramiza.</summary>
            public readonly byte[] CeramicoUmbral = new byte[MaterialId.BasesCount];
            /// <summary>Umbral de persistencia del Calcinado de esta base (FusionRaw + 15..30, contrato 4.2) -- DISTINTO de CalcinacionRaw (la temp a la que Polvo->Calcinado).</summary>
            public readonly byte[] CalcinadoUmbral = new byte[MaterialId.BasesCount];
            public readonly short[] DensidadPolvo = new short[MaterialId.BasesCount];
            public readonly byte[] ConductividadBase = new byte[MaterialId.BasesCount];
            public readonly bool[] SolubleBase = new bool[MaterialId.BasesCount];
            public readonly bool[] CombustibleBase = new bool[MaterialId.BasesCount];
            public readonly byte[] TempCombustibleRawBase = new byte[MaterialId.BasesCount];
            public readonly int[] PesoEnLimo = new int[MaterialId.BasesCount];
            /// <summary>(playtest 27) Banda de extracción del limo por base -- ver Universe.ExtraccionRaw. ASCENDENTES y disjuntas; la más baja SIEMPRE por debajo de CrisolTier0Raw.</summary>
            public readonly byte[] ExtraccionRaw = new byte[MaterialId.BasesCount];
        }

        // =================================================================
        // (playtest 27) LAS BANDAS DE EXTRACCIÓN
        // =================================================================
        // Cinco escalones fijos de temperatura, repartidos entre las cinco
        // bases por SORTEO (Fisher-Yates): así "cuál es la arena del fuego
        // bajo" cambia con la seed, pero la ESCALERA siempre existe y siempre
        // se recorre igual, que es lo que el jugador puede formular como una
        // frase (regla 35).
        //
        // NÚMEROS, derivados del código que los consume (regla 50), NO del
        // nombre:
        //  · El escalón 0 (106 ±4 -> 102..110) tiene que estar por debajo de
        //    CrisolTier0Raw=120 SIEMPRE: es la promesa "con el fuego bajo
        //    siempre sale la primera", y es lo que hace jugable el minuto 1
        //    (el jugador NO tiene combustible al empezar -- la queja literal
        //    de Cesar).
        //  · El escalón 4 (158 ±4 -> 154..162) tiene que estar por debajo del
        //    peor TempCombustibleRaw posible, o esa base sería contenido
        //    muerto en esa seed. Por eso el rango de los combustibles sube de
        //    150..175 a 165..190 en esta misma ronda (ver
        //    SortearTablaPersistencia): 165 > 162 con margen en el PEOR caso.
        //  · Los escalones intermedios reparten el hueco a distancias
        //    parecidas para que cada combustible nuevo abra normalmente UNA
        //    base más, no tres de golpe.
        private static readonly byte[] BandasExtraccion = { 106, 124, 136, 148, 158 };
        private const int BandaExtraccionJitter = 4;

        /// <summary>
        /// Sortea UN candidato de tabla (contrato 4.2): vector por base
        /// (fusión 130..170, calcinación 100..125 -- SIEMPRE por debajo de
        /// fusión por construcción de rangos, ni hace falta clampear --,
        /// solidificación, densidades, conductividad, solubilidad) +
        /// modificadores de estado con TENDENCIA FIJA (regla 17 de
        /// CLAUDE.md) y magnitud por seed.
        /// </summary>
        private static PersistenciaTablas SortearTablaPersistencia(System.Random rng)
        {
            var t = new PersistenciaTablas();

            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                t.FusionRaw[b] = (byte)(130 + rng.Next(41));      // 130..170
                t.CalcinacionRaw[b] = (byte)(100 + rng.Next(26)); // 100..125
                t.SolidificaRaw[b] = (byte)(t.FusionRaw[b] - (15 + rng.Next(16))); // 15..30 por debajo de la fusión: histéresis, y siempre muy por encima de CellGrid.AmbientRaw (70) -- TODO enfriamiento en el mundo cruza este umbral y templa, el gesto central del orden-importa (contrato 4.1).
                t.CompactoUmbral[b] = (byte)Mathf.Min(255, t.FusionRaw[b] + 10 + rng.Next(11));  // +10..20 sobre el polvo.
                t.CalcinadoUmbral[b] = (byte)Mathf.Min(255, t.FusionRaw[b] + 15 + rng.Next(16)); // +15..30 sobre el polvo.

                bool ceramiza = rng.Next(100) < 65; // ~65% de las bases ceramiza esta seed; el resto vuelve a Fundido si se recalienta (Game/Crisol.cs, fuera de este archivo).
                t.CeramicoUmbral[b] = ceramiza ? (byte)Mathf.Min(255, t.CompactoUmbral[b] + 25 + rng.Next(16)) : (byte)0; // +25..40 SOBRE COMPACTO.

                t.ConductividadBase[b] = (byte)rng.Next(3); // 0/1/2 uniforme.
            }

            // Densidades bien repartidas (contrato 4.2): anclas elegidas para
            // que SIEMPRE straddleen el rango real de liquidDensity[Water]
            // (34..186, ver densitySlots+jitter arriba en Create) sea cual
            // sea el reparto de esta seed -- garantiza "al menos una base más
            // densa que el agua y una menos" sin depender de qué slot le
            // tocó al agua esta run.
            short[] densAnclas = { 15, 70, 125, 180, 230 };
            int[] densOrden = { 0, 1, 2, 3, 4 };
            ShuffleFisherYates(densOrden, rng);
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                int jitter = rng.Next(-4, 5);
                t.DensidadPolvo[b] = (short)Mathf.Clamp(densAnclas[densOrden[b]] + jitter, 1, 250);
            }

            // Solubilidad: 2-3 de las 5 bases (contrato 4.2).
            int nSolubles = 2 + rng.Next(2);
            int[] solOrden = { 0, 1, 2, 3, 4 };
            ShuffleFisherYates(solOrden, rng);
            for (int i = 0; i < nSolubles; i++) t.SolubleBase[solOrden[i]] = true;

            // Bandas de extracción del limo (playtest 27): los cinco
            // escalones fijos repartidos entre las bases por sorteo. Ver el
            // bloque BandasExtraccion para de dónde salen los números.
            int[] bandaOrden = { 0, 1, 2, 3, 4 };
            ShuffleFisherYates(bandaOrden, rng);
            for (int i = 0; i < MaterialId.BasesCount; i++)
            {
                int jitter = rng.Next(-BandaExtraccionJitter, BandaExtraccionJitter + 1);
                t.ExtraccionRaw[bandaOrden[i]] = (byte)Mathf.Clamp(BandasExtraccion[i] + jitter, 1, 254);
            }

            // Combustible (Calcinado): 1-2 de las 5 bases.
            // (playtest 27) TempCombustibleRaw 150..175 -> **165..190**. El
            // motivo es la escalera nueva: con el techo viejo (150 en el peor
            // caso) la banda de extracción más alta (hasta 162) quedaba
            // inalcanzable y esa base era contenido muerto. De paso arregla
            // algo que ya cojeaba: FusionRaw llega a 170, así que con un
            // combustible de 150 había seeds en las que NADA se podía fundir
            // -- el solver lo tapaba eligiendo otro ganador, pero el eslabón
            // "fundir" de la escalera del contrato no existía de verdad.
            int nCombustibles = 1 + rng.Next(2);
            int[] combOrden = { 0, 1, 2, 3, 4 };
            ShuffleFisherYates(combOrden, rng);
            for (int i = 0; i < nCombustibles; i++)
            {
                int b = combOrden[i];
                t.CombustibleBase[b] = true;
                t.TempCombustibleRawBase[b] = (byte)(165 + rng.Next(26));
            }

            // Pesos de separación del limo: SIEMPRE positivos (las 5 bases
            // tienen que poder salir del limo -- lo exige la garantía 3, ver
            // EvaluarGarantia) y suman 100.
            int[] pesosCrudos = new int[MaterialId.BasesCount];
            int sumaCruda = 0;
            for (int b = 0; b < MaterialId.BasesCount; b++) { pesosCrudos[b] = 1 + rng.Next(30); sumaCruda += pesosCrudos[b]; }
            int sumaRepartida = 0;
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                t.PesoEnLimo[b] = Mathf.Max(1, pesosCrudos[b] * 100 / sumaCruda);
                sumaRepartida += t.PesoEnLimo[b];
            }
            t.PesoEnLimo[MaterialId.BasesCount - 1] += 100 - sumaRepartida; // ajuste de redondeo en la última.
            if (t.PesoEnLimo[MaterialId.BasesCount - 1] < 1)
            {
                // Salvaguarda (no debería ocurrir con 5 bases de mínimo 1 cada
                // una, pero el ajuste de redondeo de arriba podría en teoría
                // llevarse el mínimo si sumaRepartida > 100 por acumulación):
                // reparto llano.
                for (int b = 0; b < MaterialId.BasesCount; b++) t.PesoEnLimo[b] = 20;
            }

            return t;
        }

        /// <summary>Umbral de persistencia (contrato §3) de la variante (b, estado). Compartido por el solver y por ConstruirEstadosDerivados: es LA MISMA tabla, no dos copias.</summary>
        private static byte UmbralPersistenciaEstado(PersistenciaTablas t, int b, EstadoMateria e, byte waterBoilsAtRaw)
        {
            switch (e)
            {
                case EstadoMateria.Polvo: return t.FusionRaw[b];
                case EstadoMateria.Fundido: return 255; // nada lo transforma más arriba en v1 (contrato 4.1: "nada más tiene transiciones").
                case EstadoMateria.Templado: return t.FusionRaw[b]; // el contrato no da modificador térmico propio para este estado (solo Prensa=Reventar) -- mismo umbral que su Polvo.
                case EstadoMateria.Recocido: return t.FusionRaw[b]; // ídem (solo Prensa=Compactar).
                case EstadoMateria.Compacto: return t.CompactoUmbral[b];
                case EstadoMateria.Ceramico: return t.CeramicoUmbral[b] != 0 ? t.CeramicoUmbral[b] : t.CompactoUmbral[b]; // si esta base no ceramiza, el material igual existe (p.ej. F3/DevPalette) con el umbral de su Compacto.
                case EstadoMateria.Calcinado: return t.CalcinadoUmbral[b];
                case EstadoMateria.Solucion: return waterBoilsAtRaw; // coincide con su propio boilsAt: evaporar precipita justo ahí.
                default: return 255;
            }
        }

        /// <summary>Respuesta a la Prensa (contrato §3) por ESTADO -- tendencia fija entre universos (regla 17), no varía por seed. Calcinado/Solucion no los nombra el contrato (4.2 solo da Polvo/Fundido/Templado/Recocido/Compacto/Ceramico): DECISIÓN -- Calcinado es Powder igual que Polvo (misma respuesta, produce el Compacto de su base); Solucion es Liquid igual que Fundido/Limo/agua (Escupir: los líquidos no se comprimen).</summary>
        private static RespuestaPrensa PrensaEstado(EstadoMateria e)
        {
            switch (e)
            {
                case EstadoMateria.Polvo: return RespuestaPrensa.Compactar;
                case EstadoMateria.Fundido: return RespuestaPrensa.Escupir;
                case EstadoMateria.Templado: return RespuestaPrensa.Reventar;
                case EstadoMateria.Recocido: return RespuestaPrensa.Compactar;
                case EstadoMateria.Compacto: return RespuestaPrensa.Resistir;
                case EstadoMateria.Ceramico: return RespuestaPrensa.Resistir;
                case EstadoMateria.Calcinado: return RespuestaPrensa.Compactar; // DECISIÓN, ver docblock del método.
                case EstadoMateria.Solucion: return RespuestaPrensa.Escupir;    // DECISIÓN, ver docblock del método.
                default: return RespuestaPrensa.Nada;
            }
        }

        /// <summary>Conductividad (contrato §3) por (base, estado). Ceramico/Solucion son las DOS excepciones que el contrato nombra explícitamente; el resto (DECISIÓN) hereda la conductividad de su base -- "conductora o iónica" es un rasgo de la sustancia, no del estado.</summary>
        private static byte ConductividadEstado(PersistenciaTablas t, int b, EstadoMateria e)
        {
            switch (e)
            {
                case EstadoMateria.Polvo: return 0; // polvo suelto no conduce.
                case EstadoMateria.Ceramico: return 0; // CONTRATO explícito: "no conduce".
                case EstadoMateria.Solucion: return (byte)(t.ConductividadBase[b] > 0 ? 2 : 0); // CONTRATO explícito: colapsa "conductora o iónica" en un único chequeo (DECISIÓN de simplificación).
                default: return t.ConductividadBase[b];
            }
        }

        /// <summary>SolubleEnAgua (contrato §3): "solo puede ser true para estados Polvo/Calcinado" -- literal.</summary>
        private static bool SolubleEstado(PersistenciaTablas t, int b, EstadoMateria e)
        {
            if (e != EstadoMateria.Polvo && e != EstadoMateria.Calcinado) return false;
            return t.SolubleBase[b];
        }

        /// <summary>EsCombustible (contrato §3): ligado al Calcinado únicamente (contrato 4.2: "combustible en 1-2 de las 5 bases" habla del Calcinado). El `flammable` clásico de MaterialDef es un sistema aparte que v1 no conecta aquí.</summary>
        private static bool CombustibleEstado(PersistenciaTablas t, int b, EstadoMateria e)
        {
            return e == EstadoMateria.Calcinado && t.CombustibleBase[b];
        }

        private static byte TempCombustibleEstado(PersistenciaTablas t, int b, EstadoMateria e)
        {
            return CombustibleEstado(t, b, e) ? t.TempCombustibleRawBase[b] : (byte)0;
        }

        /// <summary>Añade a `outEdges` los ids alcanzables desde `node` en UNA operación del retículo (contrato 4.4: separar/fundir@tier/templar/recocer/prensar/calcinar@tier/ceramizar@tier/disolver/evaporar), respetando `tier` para las 3 operaciones marcadas "@tier". Puramente ABSTRACTO (el solver verifica con grafo, no con la sim, contrato 4.4/DISENO §5): separar/templar/recocer/prensar/disolver/evaporar no llevan "@tier" en el contrato y se modelan aquí SIN gate térmico -- el gate real que SimStepper aplica a separar (temp&gt;=LimoSeparaRaw) es un mecanismo de JUEGO aparte, no una restricción de alcanzabilidad de la tabla.</summary>
        private static void AddEdgesFrom(PersistenciaTablas t, byte tier, byte node, List<byte> outEdges)
        {
            outEdges.Clear();
            if (node == MaterialId.Limo)
            {
                // (playtest 27) SEPARAR PASA A SER UNA OPERACIÓN "@tier". Antes
                // el limo daba las 5 bases de una tacada y sin condición
                // térmica; ahora cada base tiene su banda y solo salen las que
                // el tier alcanza (Game/Crisol saca ADEMÁS solo la más alta por
                // hornada, pero eso es ritmo de juego -- para la ALCANZABILIDAD
                // basta con el gate, igual que fundir/calcinar/ceramizar).
                for (int b = 0; b < MaterialId.BasesCount; b++)
                    if (t.ExtraccionRaw[b] <= tier) outEdges.Add(MaterialId.MatDe(b, EstadoMateria.Polvo));
                return;
            }
            if (!MaterialId.EsBaseEstado(node)) return;

            int baseIdx = MaterialId.BaseDe(node);
            var estado = MaterialId.EstadoDe(node);
            switch (estado)
            {
                case EstadoMateria.Polvo:
                    if (t.FusionRaw[baseIdx] <= tier) outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Fundido)); // fundir@tier
                    outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Compacto)); // prensar (Compactar)
                    if (t.CalcinacionRaw[baseIdx] <= tier) outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Calcinado)); // calcinar@tier
                    if (t.SolubleBase[baseIdx]) outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Solucion)); // disolver
                    break;
                case EstadoMateria.Fundido:
                    outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Templado)); // templar
                    outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Recocido)); // recocer
                    break;
                case EstadoMateria.Templado:
                    outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Polvo)); // prensar (Reventar)
                    break;
                case EstadoMateria.Recocido:
                    outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Compacto)); // prensar (Compactar)
                    break;
                case EstadoMateria.Compacto:
                    if (t.CeramicoUmbral[baseIdx] != 0 && t.CeramicoUmbral[baseIdx] <= tier) outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Ceramico)); // ceramizar@tier
                    break;
                case EstadoMateria.Ceramico:
                    break; // Resistir: sin arista útil.
                case EstadoMateria.Calcinado:
                    outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Compacto)); // prensar (Compactar, ver PrensaEstado).
                    if (t.SolubleBase[baseIdx]) outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Solucion)); // disolver
                    break;
                case EstadoMateria.Solucion:
                    outEdges.Add(MaterialId.MatDe(baseIdx, EstadoMateria.Polvo)); // evaporar
                    break;
            }
        }

        /// <summary>BFS real (con distancia) sobre los ~41 nodos (Limo + 40 variantes) del retículo, a un `tier` térmico dado. `dist[Limo]=0`; nodos no alcanzados quedan en `reached[id]=false` (su `dist` no se usa).</summary>
        private static void BfsPersistencia(PersistenciaTablas t, byte tier, out bool[] reached, out int[] dist)
        {
            reached = new bool[MaterialId.Count];
            dist = new int[MaterialId.Count];
            var queue = new Queue<byte>();
            var edges = new List<byte>(8);

            reached[MaterialId.Limo] = true;
            queue.Enqueue(MaterialId.Limo);

            while (queue.Count > 0)
            {
                byte node = queue.Dequeue();
                AddEdgesFrom(t, tier, node, edges);
                foreach (var next in edges)
                {
                    if (reached[next]) continue;
                    reached[next] = true;
                    dist[next] = dist[node] + 1;
                    queue.Enqueue(next);
                }
            }
        }

        /// <summary>
        /// Evalúa las 3 garantías del contrato (4.4) para UN candidato de
        /// tabla, calculando primero la escalera térmica (tier0 =
        /// CrisolTier0Raw, tier1 = mejor TempCombustibleRaw alcanzable CON LO
        /// YA ALCANZADO a tier0). `tempEnsayo` se sortea aquí (165..180,
        /// consume rng incluso si el intento falla -- da igual, es tabla
        /// pura y el intento se descarta entero de todas formas).
        /// </summary>
        private static void EvaluarGarantia(PersistenciaTablas t, System.Random rng, byte waterBoilsAtRaw,
            out byte tempEnsayo, out byte ganador, out int baseCombustible, out int pasos, out bool ok)
        {
            BfsPersistencia(t, CrisolTier0Raw, out bool[] reached0, out _);

            byte tier1 = CrisolTier0Raw;
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                byte calc = MaterialId.MatDe(b, EstadoMateria.Calcinado);
                if (t.CombustibleBase[b] && reached0[calc] && t.TempCombustibleRawBase[b] > tier1)
                    tier1 = t.TempCombustibleRawBase[b];
            }
            BfsPersistencia(t, tier1, out bool[] reachedFinal, out int[] distFinal);

            // Garantía 1: ≥1 base combustible alcanzable a tier0.
            // (playtest 27) Ahora exige TRES cosas, no dos, porque separar ya
            // es "@tier": la base tiene que (a) ser combustible, (b) poder
            // SALIR DEL LIMO con el fuego bajo -- su banda de extracción por
            // debajo de tier0, antes trivial y hoy no --, y (c) calcinarse con
            // el fuego bajo. Sin (b) el jugador no puede llegar a su primer
            // combustible y el juego no arranca: es exactamente la queja de
            // Cesar ("yo al inicio NO TENGO combustible") elevada a invariante.
            bool g1 = false; int g1Base = -1;
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                if (t.CombustibleBase[b] && t.ExtraccionRaw[b] <= CrisolTier0Raw && t.CalcinacionRaw[b] <= CrisolTier0Raw)
                { g1 = true; g1Base = b; break; }
            }

            // Garantía 4 (playtest 27): TODA base se puede extraer del limo
            // con el mejor combustible alcanzable. Sin esto, una seed podría
            // esconder una o dos bases para siempre -- y con 5 bases x 8
            // estados, perder una base es perder el 20% del retículo sin que
            // nada avise.
            bool g4 = true;
            for (int b = 0; b < MaterialId.BasesCount; b++)
                if (t.ExtraccionRaw[b] > tier1) { g4 = false; break; }

            tempEnsayo = (byte)(165 + rng.Next(16)); // 165..180

            // Garantía 2: ≥1 variante alcanzable con umbral >= tempEnsayo+10.
            // (fix integración, cazado EN EL PRIMER ARRANQUE con el log del
            // solver: "ganador=19" = base 0 FUNDIDO) Fundido y Solucion se
            // EXCLUYEN de la candidatura a ganador: Fundido devuelve umbral
            // 255 ("nada lo transforma más arriba", físicamente cierto) y
            // ganaba SIEMPRE por umbral>mejorUmbral -- pero la garantía es
            // para el pedido AguantaCalor, y eso exige un estado ENTREGABLE:
            // lo fundido se TEMPLA en el viaje al plinto (freezesInto del
            // mundo) y lo que el Ensayo mediría es el Templado, cuyo umbral
            // es otro. Un ganador que no puede llegar al examen siendo él
            // mismo no garantiza nada. Solucion se excluye por la razón
            // simétrica: su umbral ES su evaporación (siempre < tempEnsayo,
            // jamás ganaría, pero se excluye explícito por claridad).
            ganador = 0; bool g2 = false; int mejorUmbral = -1; pasos = 0;
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                for (int e8 = 0; e8 < 8; e8++)
                {
                    var estado = (EstadoMateria)e8;
                    if (estado == EstadoMateria.Fundido || estado == EstadoMateria.Solucion) continue;
                    byte id = MaterialId.MatDe(b, estado);
                    if (!reachedFinal[id]) continue;
                    byte umbral = UmbralPersistenciaEstado(t, b, estado, waterBoilsAtRaw);
                    if (umbral >= tempEnsayo + 10 && umbral > mejorUmbral)
                    {
                        mejorUmbral = umbral; ganador = id; g2 = true; pasos = distFinal[id];
                    }
                }
            }

            // Garantía 3: ≥1 variante conductora alcanzable + ≥1 base soluble + ≥1 base insoluble alcanzables.
            bool g3cond = false, g3sol = false, g3insol = false;
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                for (int e8 = 0; e8 < 8; e8++)
                {
                    byte id = MaterialId.MatDe(b, (EstadoMateria)e8);
                    if (reachedFinal[id] && ConductividadEstado(t, b, (EstadoMateria)e8) >= 1) g3cond = true;
                }
                bool baseAlcanzada = reachedFinal[MaterialId.MatDe(b, EstadoMateria.Polvo)] || reachedFinal[MaterialId.MatDe(b, EstadoMateria.Calcinado)];
                if (baseAlcanzada && t.SolubleBase[b]) g3sol = true;
                if (baseAlcanzada && !t.SolubleBase[b]) g3insol = true;
            }

            baseCombustible = g1Base;
            ok = g1 && g2 && g3cond && g3sol && g3insol && g4;
        }

        /// <summary>
        /// CLAMPEO FINAL (contrato 4.4): si 50 sorteos de tabla no cumplieron
        /// las 3 garantías por azar, se fuerza a mano el mínimo imprescindible
        /// sobre la base 0 (y la 1 para la garantía de insoluble) -- nunca se
        /// relajan las garantías, se ajustan los NÚMEROS. Los caminos usados
        /// (separar, prensar) son operaciones SIN "@tier": Polvo(0) y
        /// Compacto(0) son alcanzables sin depender de ningún combustible, así
        /// que el clampeo no puede fallar por falta de tier.
        /// </summary>
        private static void ClampearGarantia(PersistenciaTablas t, byte tempEnsayo, out byte ganador, out int baseCombustible, out int pasos)
        {
            t.CombustibleBase[0] = true;
            if (t.CalcinacionRaw[0] > CrisolTier0Raw) t.CalcinacionRaw[0] = CrisolTier0Raw;

            // (playtest 27) La escalera de extracción se fuerza a mano en el
            // orden canónico: la base 0 sale con el fuego bajo (banda por
            // debajo de CrisolTier0Raw) y las otras cuatro escalonadas por
            // debajo del combustible que acabamos de garantizar. Sin esto, el
            // clampeo dejaría un mundo donde no se puede sacar NADA del limo.
            for (int b = 0; b < MaterialId.BasesCount; b++) t.ExtraccionRaw[b] = BandasExtraccion[b];
            t.ExtraccionRaw[0] = (byte)(CrisolTier0Raw - 10);
            byte techoBandas = t.ExtraccionRaw[0];
            for (int b = 1; b < MaterialId.BasesCount; b++) if (t.ExtraccionRaw[b] > techoBandas) techoBandas = t.ExtraccionRaw[b];
            if (t.TempCombustibleRawBase[0] < techoBandas + 5) t.TempCombustibleRawBase[0] = (byte)Mathf.Min(255, techoBandas + 5);

            // Ganador: Compacto(0), a 2 pasos (Limo -separar-> Polvo(0) -prensar-> Compacto(0)), con margen de sobra sobre tempEnsayo+10.
            t.CompactoUmbral[0] = (byte)Mathf.Min(255, tempEnsayo + 20);
            ganador = MaterialId.MatDe(0, EstadoMateria.Compacto);
            pasos = 2;
            baseCombustible = 0;

            // Conductividad alcanzable: Compacto(0) hereda la base.
            t.ConductividadBase[0] = 2;

            // Soluble + insoluble alcanzables (ambas bases SIEMPRE
            // alcanzables como Polvo, vía separar, sin tier).
            t.SolubleBase[0] = true;
            if (MaterialId.BasesCount > 1) t.SolubleBase[1] = false;
        }

        /// <summary>
        /// El solver de garantía completo (contrato 4.4): reintenta el sorteo
        /// de tabla hasta 50 veces (tabla pura, microsegundos) y si agota,
        /// clampea la última. Devuelve las tablas finales YA VALIDADAS +
        /// las 6 tablas de propiedades por MaterialId (contrato §3) + los 3
        /// campos de la garantía. `Debug.Assert` + línea de log (formato del
        /// log de leyes) al final.
        /// </summary>
        private static void ResolverPersistencia(
            MaterialDef[] mats, System.Random rng, byte waterBoilsAtRaw,
            out PersistenciaTablas tablas,
            out byte[] umbralPersistenciaRaw, out RespuestaPrensa[] prensaPorMaterial,
            out byte[] conductividadPorMaterial, out bool[] solubleEnAguaPorMaterial,
            out bool[] esCombustiblePorMaterial, out byte[] tempCombustibleRawPorMaterial,
            out byte ganadorGarantizado, out byte tempEnsayoCalorRaw, out int baseCombustibleGarantizada)
        {
            const int MaxIntentosTabla = 50;

            PersistenciaTablas t = null;
            byte tempEnsayo = 0, ganador = 0;
            int baseCombustible = -1, pasos = 0;
            bool ok = false;

            for (int intento = 0; intento < MaxIntentosTabla; intento++)
            {
                t = SortearTablaPersistencia(rng);
                EvaluarGarantia(t, rng, waterBoilsAtRaw, out tempEnsayo, out ganador, out baseCombustible, out pasos, out ok);
                if (ok) break;
            }

            if (!ok)
            {
                ClampearGarantia(t, tempEnsayo, out ganador, out baseCombustible, out pasos);
                UnityEngine.Debug.LogWarning($"[ChaosAlchemy] Persistencia: {MaxIntentosTabla} sorteos de tabla no cumplieron las 3 garantías por azar -- CLAMPEADA la última (ver ClampearGarantia).");
            }

            UnityEngine.Debug.Assert(baseCombustible >= 0 && baseCombustible < MaterialId.BasesCount,
                "[ChaosAlchemy] INVARIANTE ROTA: BaseCombustibleGarantizada fuera de rango tras el solver de persistencia (CONTRATO_PERSISTE.md sección 4.4).");
            UnityEngine.Debug.Assert(MaterialId.EsBaseEstado(ganador),
                "[ChaosAlchemy] INVARIANTE ROTA: GanadorGarantizado no es una variante base×estado (CONTRATO_PERSISTE.md sección 4.4).");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            {
                // (playtest 27) El log de seed AFIRMA también la escalera de
                // extracción, porque la regla 51 nació precisamente de que un
                // solver que no imprime su resultado no protege nada: la
                // garantía nueva ("con el fuego bajo sale la base X; con el
                // combustible garantizado salen las cinco") tiene que poder
                // leerse en el PRIMER arranque, no deducirse jugando.
                var sbEx = new System.Text.StringBuilder();
                sbEx.Append($"[ChaosAlchemy] Persistencia: ganador={ganador} a {pasos} pasos, combustible=base {baseCombustible}, tier1={t.TempCombustibleRawBase[baseCombustible]} (verificado). Extracción del limo por banda:");
                for (int b = 0; b < MaterialId.BasesCount; b++)
                    sbEx.Append($" base{b}={t.ExtraccionRaw[b]}{(t.ExtraccionRaw[b] <= CrisolTier0Raw ? "(fuego bajo)" : string.Empty)}");
                UnityEngine.Debug.Log(sbEx.ToString());
            }
#endif

            // ---- Tablas de propiedades por MaterialId completo (contrato §3), vocabulario+Limo primero con defaults sensatos, luego el bloque bases×estado desde `t`. ----
            umbralPersistenciaRaw = new byte[MaterialId.Count];
            prensaPorMaterial = new RespuestaPrensa[MaterialId.Count];
            conductividadPorMaterial = new byte[MaterialId.Count];
            solubleEnAguaPorMaterial = new bool[MaterialId.Count];
            esCombustiblePorMaterial = new bool[MaterialId.Count];
            tempCombustibleRawPorMaterial = new byte[MaterialId.Count];

            for (byte id = 0; id < MaterialId.BaseEstado0; id++)
            {
                // Vocabulario del taller + Limo: el contrato solo fija Prensa
                // para dos casos ("Limo/agua: Escupir", 4.2) -- el resto de
                // esta franja (DECISIÓN) se deja en Nada/0/false, coherente
                // con que esta mecánica es nueva y estos materiales no
                // participan del retículo. UmbralPersistenciaRaw se deriva de
                // lo que YA transforma a ese material en el mundo (para que
                // el Ensayo tenga un dato no arbitrario incluso si algún día
                // se prueba con vocabulario), salvo Limo (112, el mismo
                // LimoSeparaRaw de SimStepper -- valor exacto duplicado a
                // propósito, contrato 4.3, ninguna dependencia entre archivos).
                umbralPersistenciaRaw[id] = id == MaterialId.Limo ? (byte)112 : UmbralPersistenciaVocabulario(mats[id]); // 112 = LimoSeparaRaw de SimStepper (fix integración: era 150, ver el comentario de esa constante).
                prensaPorMaterial[id] = (id == MaterialId.Water || id == MaterialId.Limo) ? RespuestaPrensa.Escupir : RespuestaPrensa.Nada;
                conductividadPorMaterial[id] = 0;
                solubleEnAguaPorMaterial[id] = false;
                esCombustiblePorMaterial[id] = false;
                tempCombustibleRawPorMaterial[id] = 0;
            }

            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                for (int e8 = 0; e8 < 8; e8++)
                {
                    var estado = (EstadoMateria)e8;
                    byte id = MaterialId.MatDe(b, estado);
                    umbralPersistenciaRaw[id] = UmbralPersistenciaEstado(t, b, estado, waterBoilsAtRaw);
                    prensaPorMaterial[id] = PrensaEstado(estado);
                    conductividadPorMaterial[id] = ConductividadEstado(t, b, estado);
                    solubleEnAguaPorMaterial[id] = SolubleEstado(t, b, estado);
                    esCombustiblePorMaterial[id] = CombustibleEstado(t, b, estado);
                    tempCombustibleRawPorMaterial[id] = TempCombustibleEstado(t, b, estado);
                }
            }

            tablas = t;
            ganadorGarantizado = ganador;
            tempEnsayoCalorRaw = tempEnsayo;
            baseCombustibleGarantizada = baseCombustible;
        }

        /// <summary>UmbralPersistenciaRaw de un material del vocabulario (0..16): el primer umbral que YA lo transforma en el mundo (flammable primero -- arder es la muerte más temprana --, si no fusión, si no ebullición, si no 255 = persiste siempre, como Stone).</summary>
        private static byte UmbralPersistenciaVocabulario(MaterialDef def)
        {
            if (def.flammable && def.ignitionTemp != short.MaxValue) return (byte)Mathf.Clamp(def.ignitionTemp, 0, 255);
            if (def.meltsAt != short.MaxValue) return (byte)Mathf.Clamp(def.meltsAt, 0, 255);
            if (def.boilsAt != short.MaxValue) return (byte)Mathf.Clamp(def.boilsAt, 0, 255);
            return 255;
        }

        /// <summary>
        /// (playtest 47, ENCARGO C) Persistencia/prensa manual para los 6 ids
        /// de las recetas cruzadas (59..64) -- el mismo hueco que
        /// ResolverPersistencia ya dejaba sin rellenar para Brasa (58), ver el
        /// comentario en Create() junto a la llamada. Números DECISIÓN DE C,
        /// documentados en el informe de la ronda:
        ///  - UmbralPersistenciaRaw: "el hormigón aguanta MÁS que el mortero"
        ///    (contrato §1a) se cumple aquí en el eje TÉRMICO (210 vs 150);
        ///    Clinker (230) y Esmaltado (235) son los más resistentes (fuego
        ///    pleno cocidos ya, como el Cerámico real, cuyo umbral es
        ///    CeramicoUmbral[b] -- típicamente 220-255 en la tabla generada);
        ///    VidrioVerde (155) es DELIBERADAMENTE más bajo que un vidrio
        ///    puro -- funde con fundente, y lo que baja el punto de fusión al
        ///    nacer lo vuelve a bajar al recalentar (misma lección, dos
        ///    veces). Lejia (110) es la más floja: es agua con potasa, se
        ///    evapora como cualquier disolución.
        ///  - Prensa/Conductividad/Soluble/Combustible: ver el bloque de
        ///    comentarios junto a los MaterialDef nuevos en Create().
        /// </summary>
        private static void RellenarPersistenciaCruces(
            byte[] umbralPersistenciaRaw, RespuestaPrensa[] prensaPorMaterial,
            byte[] conductividadPorMaterial, bool[] solubleEnAguaPorMaterial,
            bool[] esCombustiblePorMaterial, byte[] tempCombustibleRawPorMaterial)
        {
            void Rellenar(byte id, byte umbral, RespuestaPrensa prensa)
            {
                umbralPersistenciaRaw[id] = umbral;
                prensaPorMaterial[id] = prensa;
                conductividadPorMaterial[id] = 0;
                solubleEnAguaPorMaterial[id] = false;
                esCombustiblePorMaterial[id] = false;
                tempCombustibleRawPorMaterial[id] = 0;
            }

            Rellenar(MaterialId.Mortero, 150, RespuestaPrensa.Resistir);
            Rellenar(MaterialId.VidrioVerde, 155, RespuestaPrensa.Resistir);
            Rellenar(MaterialId.Lejia, 110, RespuestaPrensa.Escupir);
            Rellenar(MaterialId.Hormigon, 210, RespuestaPrensa.Resistir);
            Rellenar(MaterialId.Esmaltado, 235, RespuestaPrensa.Resistir);
            Rellenar(MaterialId.Clinker, 230, RespuestaPrensa.Resistir);
        }

        // ===================================================================
        // (playtest 47, ENCARGO C, CONTRATO_FASE_A.md §1b) LA MEZCLA EN CUBETA:
        // la tabla de cruces. Estática (estos pares/productos dependen SOLO de
        // la identidad real, que a su vez solo existe atada a la seed
        // congelada de Semilla Cero -- ver Universe.SemillaCeroBaseIdx), igual
        // de espíritu que Universe._identidadReal.
        //
        // BANDAS TÉRMICAS: <see cref="Crisol.CrisolTier0Raw"/... perdón,
        // <see cref="CrisolTier0Raw"/> (120) es el rescoldo SIN combustible;
        // el crisol nunca baja de ahí (Game/Crisol.IntentarEncender: `cima =
        // fuelMat!=Empty ? TempCombustibleRaw(fuelMat) : CrisolTier0Raw`).
        // Por eso "cualquiera" y "bajo" son EL MISMO umbral numérico
        // (CrisolTier0Raw): ambos se cumplen siempre que hay hornada. Se
        // mantienen como dos nombres distintos por CLARIDAD DE DISEÑO (el
        // contrato los pide como conceptos separados) y por si una ronda
        // futura introduce un fuego por debajo del rescoldo propio -- hoy no
        // existe, así que numéricamente coinciden, documentado a propósito.
        // <see cref="CruceFuegoPlenoRaw"/> (145) SÍ es un gate real: exige
        // combustible de verdad cargado (el peor combustible sorteable de la
        // tabla es 165..190 raw en TODA seed, ver TempCombustibleRawBase) --
        // muy por debajo de eso, así que cualquier combustible real basta,
        // pero el rescoldo solo (120) nunca lo cruza.
        //
        // LA LECCIÓN DEL VIDRIO DE BOTELLA: CruceFuegoPlenoRaw=145 es
        // MUCHO más bajo que FusionRaw(base0) en Semilla Cero (220, ver el
        // override 2 de AplicarOverridesSemillaCero) -- la potasa de la
        // ceniza (el fundente real) baja el punto de fusión de la arena, y el
        // NÚMERO lo dice: el vidrio de botella funde a 145, el vidrio puro
        // pediría 220 (de hecho, INALCANZABLE con el mejor combustible de
        // esta seed, 165..190 -- la ceniza no es un atajo cualquiera, es LA
        // ÚNICA forma práctica de fundir arena en este universo).
        // ===================================================================
        /// <summary>Tier de fuego que exige un cruce (ver el bloque de comentarios de arriba).</summary>
        public enum TierCruce : byte { Cualquiera = 0, Bajo = 1, Pleno = 2 }

        /// <summary>Gate real de "fuego pleno" para un cruce: exige combustible cargado (ver el bloque de comentarios de arriba).</summary>
        public const byte CruceFuegoPlenoRaw = 145;

        private readonly struct CruceReceta
        {
            public readonly byte A, B, Producto;
            public readonly TierCruce Tier;
            public readonly string Verbo;
            public CruceReceta(byte a, byte b, byte producto, TierCruce tier, string verbo)
            { A = a; B = b; Producto = producto; Tier = tier; Verbo = verbo; }
        }

        // Ids de los ingredientes, VERBATIM de la identidad real de Semilla
        // Cero (§2 de ConstruirIdentidadReal): base0=arena, base1=arcilla,
        // base2=caliza, base3=veta vegetal, base4=sal (SemillaCeroBaseIdx).
        // static readonly, no const: MatDe() no es una expresión constante de
        // compilación (hace aritmética sobre BaseEstado0) -- se calculan UNA
        // vez al cargar la clase, antes de _cruces (orden textual).
        private static readonly byte _cruceArenaPolvo = MaterialId.MatDe(0, EstadoMateria.Polvo);       // "arena de sílice"
        private static readonly byte _cruceArcillaPolvo = MaterialId.MatDe(1, EstadoMateria.Polvo);     // "arcilla"
        private static readonly byte _cruceBizcocho = MaterialId.MatDe(1, EstadoMateria.Recocido);      // "bizcocho"
        private static readonly byte _cruceCalizaPolvo = MaterialId.MatDe(2, EstadoMateria.Polvo);      // "caliza molida"
        private static readonly byte _cruceCalApagada = MaterialId.MatDe(2, EstadoMateria.Recocido);    // "cal apagada"

        private static readonly CruceReceta[] _cruces =
        {
            // cal apagada + arena de sílice -> Mortero, cualquiera (tier0 basta), "amasando".
            new CruceReceta(_cruceCalApagada, _cruceArenaPolvo, MaterialId.Mortero, TierCruce.Cualquiera, "amasando"),
            // caliza molida + arcilla -> Clinker, pleno, "cociendo clínker".
            new CruceReceta(_cruceCalizaPolvo, _cruceArcillaPolvo, MaterialId.Clinker, TierCruce.Pleno, "cociendo clínker"),
            // clínker + arena de sílice -> Hormigon, bajo, "fraguando".
            // (DECISIÓN DE C, documentada) el "agua presente" del contrato se
            // SIMPLIFICA fuera: este sistema detecta solo dominante+secundario
            // (dos materiales), no un tercer ingrediente -- el agua queda
            // implícita en el gesto ("fraguando") y en la propia reseña de
            // Hormigon ("...y agua"), sin comprobación mecánica aparte.
            new CruceReceta(MaterialId.Clinker, _cruceArenaPolvo, MaterialId.Hormigon, TierCruce.Bajo, "fraguando"),
            // arena de sílice + ceniza -> VidrioVerde, pleno (a banda MÁS BAJA
            // que la fusión pura, ver el bloque de comentarios de arriba).
            new CruceReceta(_cruceArenaPolvo, MaterialId.Ash, MaterialId.VidrioVerde, TierCruce.Pleno, "fundiendo con fundente"),
            // ceniza + agua -> Lejia, bajo, "lixiviando".
            new CruceReceta(MaterialId.Ash, MaterialId.Water, MaterialId.Lejia, TierCruce.Bajo, "lixiviando"),
            // bizcocho + arena de sílice -> Esmaltado, pleno, "esmaltando".
            new CruceReceta(_cruceBizcocho, _cruceArenaPolvo, MaterialId.Esmaltado, TierCruce.Pleno, "esmaltando"),
        };

        /// <summary>
        /// (ENCARGO C) ¿Los dos materiales de la cámara forman un cruce
        /// conocido a esta temperatura? Orden-independiente (dominante/
        /// secundario pueden llegar en cualquiera de los dos papeles). LLAMAR
        /// SOLO bajo AlkahestGameBootstrap.ModoSemillaCero (API pura, no se
        /// autoprotege -- mismo contrato que TieneIdentidadReal, ver su
        /// docblock): la Game/Crisol.cs es quien decide el gate, este método
        /// no conoce el modo de juego.
        /// </summary>
        public static bool TryCruce(byte matA, byte matB, byte cimaRaw, out byte producto, out string verbo, out string condicion)
        {
            producto = MaterialId.Empty; verbo = null; condicion = null;
            for (int i = 0; i < _cruces.Length; i++)
            {
                var r = _cruces[i];
                bool coincide = (matA == r.A && matB == r.B) || (matA == r.B && matB == r.A);
                if (!coincide) continue;

                bool fuegoAlcanza = r.Tier == TierCruce.Pleno ? cimaRaw >= CruceFuegoPlenoRaw : cimaRaw >= CrisolTier0Raw;
                if (!fuegoAlcanza) return false; // el par existe, pero este fuego no alcanza -- la escalera de siempre decide.

                producto = r.Producto;
                verbo = r.Verbo;
                condicion = r.Verbo; // (Encargo C) la condición patentable del cruce ES su verbo -- ver Hornada.RegistrarOp, no depende de qué combustible ardía.
                return true;
            }
            return false;
        }

        /// <summary>Interpola linealmente byte a byte entre dos Color32 (mismo espíritu que SimRenderer.LerpByte, copiado aquí para no tocar SimRenderer -- sección 7 del contrato). El alfa siempre sale en 255 (todas las variantes bases×estado son opacas, mismo criterio que lo innominado, regla 23).</summary>
        private static Color32 LerpColor32(Color32 from, Color32 to, float t01)
        {
            byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(from.r, to.r, t01));
            byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(from.g, to.g, t01));
            byte b = (byte)Mathf.RoundToInt(Mathf.Lerp(from.b, to.b, t01));
            return new Color32(r, g, b, 255);
        }

        /// <summary>
        /// Construye los 5 MaterialDef en estado Polvo (contrato 4.1), ANTES
        /// de SortearFirmasVisuales: color/patrón/borde placeholder (los
        /// rellena esa llamada, ya con estos 5 ids dentro de
        /// UnnamedMaterialIds) -- aquí solo lo que SortearFirmasVisuales NO
        /// toca (arquetipo, densidad, fluidez, transición Polvo->Fundido).
        /// </summary>
        private static void ConstruirPolvoBases(MaterialDef[] mats, System.Random rng, PersistenciaTablas t)
        {
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                byte id = MaterialId.MatDe(b, EstadoMateria.Polvo);
                mats[id] = new MaterialDef
                {
                    id = id,
                    devName = $"Base{b}Polvo",
                    archetype = MaterialArchetype.Powder,
                    baseColor = new Color32(160, 160, 160, 255), // placeholder: SortearFirmasVisuales lo sobreescribe a continuación.
                    colorJitter = 16,
                    density = t.DensidadPolvo[b],
                    fluidity = 1,
                    meltsAt = t.FusionRaw[b],
                    meltsInto = MaterialId.MatDe(b, EstadoMateria.Fundido),
                };
            }
        }

        /// <summary>
        /// Construye los 35 MaterialDef de los estados derivados (contrato
        /// 4.1), DESPUÉS de SortearFirmasVisuales: cada uno tiñe su color
        /// desde el tono FINAL que le tocó a su Polvo (dos ejes de
        /// legibilidad -- la base se reconoce por el tono, el estado por el
        /// tratamiento) con reglas FIJAS entre universos. Patrón/borde
        /// también fijos por estado (ninguno de los dos se sortea aquí).
        /// </summary>
        private static void ConstruirEstadosDerivados(MaterialDef[] mats, System.Random rng, PersistenciaTablas t,
            byte waterBoilsAtRaw, Color32 waterColor, short waterDensity)
        {
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                Color32 tono = mats[MaterialId.MatDe(b, EstadoMateria.Polvo)].baseColor;
                Color.RGBToHSV(tono, out float h, out float s, out float v);

                // Fundido: tono saturado + brillo alto + emitsGlow, patron Pulso.
                {
                    byte id = MaterialId.MatDe(b, EstadoMateria.Fundido);
                    Color32 c = Color.HSVToRGB(h, 0.90f, 0.95f, true);
                    c.a = 255;
                    mats[id] = new MaterialDef
                    {
                        id = id, devName = $"Base{b}Fundido", archetype = MaterialArchetype.Liquid,
                        baseColor = c, colorJitter = 10, density = t.DensidadPolvo[b], fluidity = 3,
                        emitsGlow = true,
                        freezesAt = t.SolidificaRaw[b], freezesInto = MaterialId.MatDe(b, EstadoMateria.Templado),
                        patron = PatronMorfologico.Pulso, borde = BordeMorfologico.Halo,
                        patronEscala = 3, patronFuerza = 110, ritmoAnim = 90, emision = 150,
                        semillaPatron = (byte)rng.Next(256),
                    };
                }
                // Templado: tono + blanco 25%, borde Neto, patron Liso (liso vítreo).
                {
                    byte id = MaterialId.MatDe(b, EstadoMateria.Templado);
                    mats[id] = new MaterialDef
                    {
                        id = id, devName = $"Base{b}Templado", archetype = MaterialArchetype.StaticSolid,
                        caeSolido = true, cohesionCeldas = 3, // frágil hasta en esto: ménsulas cortas.
                        baseColor = LerpColor32(tono, new Color32(255, 255, 255, 255), 0.25f), colorJitter = 8,
                        density = short.MaxValue, // StaticSolid: no cae ni compite en densidad de líquidos (regla 7) -- mismo criterio que Stone/Ice/Crystal.
                        patron = PatronMorfologico.Liso, borde = BordeMorfologico.Neto,
                        patronEscala = 3, patronFuerza = 0, ritmoAnim = 0, emision = 0,
                        semillaPatron = (byte)rng.Next(256),
                    };
                }
                // Recocido: tono + gris 20%, patron Vetas suaves.
                {
                    byte id = MaterialId.MatDe(b, EstadoMateria.Recocido);
                    mats[id] = new MaterialDef
                    {
                        id = id, devName = $"Base{b}Recocido", archetype = MaterialArchetype.StaticSolid,
                        caeSolido = true, cohesionCeldas = 5,
                        baseColor = LerpColor32(tono, new Color32(128, 128, 128, 255), 0.20f), colorJitter = 10,
                        density = short.MaxValue,
                        patron = PatronMorfologico.Vetas, borde = BordeMorfologico.Neto,
                        patronEscala = 4, patronFuerza = 40, ritmoAnim = 10, emision = 0,
                        semillaPatron = (byte)rng.Next(256),
                    };
                }
                // Compacto: tono oscurecido 30%, jitter bajo, patron Celdas prieto.
                {
                    byte id = MaterialId.MatDe(b, EstadoMateria.Compacto);
                    mats[id] = new MaterialDef
                    {
                        id = id, devName = $"Base{b}Compacto", archetype = MaterialArchetype.StaticSolid,
                        caeSolido = true, cohesionCeldas = 6,
                        baseColor = LerpColor32(tono, new Color32(0, 0, 0, 255), 0.30f), colorJitter = 6, // "jitter bajo" explícito del contrato.
                        density = short.MaxValue,
                        patron = PatronMorfologico.Celdas, borde = BordeMorfologico.Neto,
                        patronEscala = 2, patronFuerza = 160, ritmoAnim = 10, emision = 0, // "prieto": escala pequeña + fuerza alta.
                        semillaPatron = (byte)rng.Next(256),
                    };
                }
                // Ceramico: tono desaturado + pálido, borde Neto, patron Liso.
                {
                    byte id = MaterialId.MatDe(b, EstadoMateria.Ceramico);
                    Color32 c = Color.HSVToRGB(h, s * 0.35f, Mathf.Min(1f, v + 0.25f), true);
                    c.a = 255;
                    mats[id] = new MaterialDef
                    {
                        id = id, devName = $"Base{b}Ceramico", archetype = MaterialArchetype.StaticSolid,
                        caeSolido = true, cohesionCeldas = 8, // el techo de cohesión: la cerámica voladiza como ninguna.
                        baseColor = c, colorJitter = 8,
                        density = short.MaxValue,
                        patron = PatronMorfologico.Liso, borde = BordeMorfologico.Neto,
                        patronEscala = 3, patronFuerza = 0, ritmoAnim = 0, emision = 0,
                        semillaPatron = (byte)rng.Next(256),
                    };
                }
                // Calcinado: tono oscurecido 50% hacia carbón, patron Motas.
                {
                    byte id = MaterialId.MatDe(b, EstadoMateria.Calcinado);
                    short densCalcinado = (short)Mathf.Clamp(Mathf.RoundToInt(t.DensidadPolvo[b] * 0.7f), 1, short.MaxValue - 1); // -30% (contrato 4.2), a diferencia de los StaticSolid de arriba SÍ importa: Calcinado es Powder y compite de verdad por flotar/hundirse.
                    mats[id] = new MaterialDef
                    {
                        id = id, devName = $"Base{b}Calcinado", archetype = MaterialArchetype.Powder,
                        baseColor = LerpColor32(tono, new Color32(20, 18, 16, 255), 0.50f), colorJitter = 14,
                        density = densCalcinado, fluidity = 1,
                        patron = PatronMorfologico.Motas, borde = BordeMorfologico.Difuso,
                        patronEscala = 2, patronFuerza = 90, ritmoAnim = 30, emision = 0,
                        semillaPatron = (byte)rng.Next(256),
                    };

                    // -----------------------------------------------------------------
                    // COMBUSTIÓN PERSISTENTE del Calcinado combustible (playtest 39,
                    // contrato ENCARGO S 1a: "los calcinados combustibles de las bases
                    // (los que Universe.EsCombustible marca) reciben valores"). Hasta
                    // esta ronda `t.CombustibleBase[b]` solo alimentaba la lógica
                    // ABSTRACTA de hornadas del Crisol (Universe.EsCombustible/
                    // TempCombustibleRaw) -- el MaterialDef en sí no era `flammable`
                    // y jamás ardía de verdad en el mundo. Ahora sí: mismo umbral
                    // exacto (`t.TempCombustibleRawBase[b]`, ya en unidades raw) como
                    // `ignitionTemp`, para que el ensayo del Crisol y la ignición real
                    // de la sim SIEMPRE coincidan en el mismo número.
                    if (t.CombustibleBase[b])
                    {
                        mats[id].flammable = true;
                        mats[id].ignitionTemp = (short)t.TempCombustibleRawBase[b];
                        mats[id].burnsInto = MaterialId.Fire; // camino legado: sin efecto mientras combustReserva>0.
                        // Reserva en unidades LLENAS (Powder: aux libre entero, sin el
                        // recorte a 7 bits de los líquidos) -- 90*8=720 ticks=24s: un
                        // lecho sólido arde más lento que se enciende (el cesto del
                        // Crisol, contrato 1b), pero no eterno.
                        mats[id].combustReserva = 90;
                        mats[id].combustPasoTicks = 8;
                        mats[id].combustCalorRaw = 18;   // un poco más que el aceite: combustible sólido, brasa más caliente.
                        mats[id].combustHumoPct = 15;    // más humo que un líquido: combustión sucia de sólido.
                        mats[id].combustPropagacionPct = 20; // un lecho de combustible propaga más agresivo que un charco.
                        mats[id].combustLenguaPct = 30;
                        mats[id].combustResiduo = MaterialId.Brasa; // sólido combustible -> BRASA (contrato 1a/1b), nunca Empty.
                    }
                }
                // Solucion: color del AGUA teñido 60% hacia el tono base (tech del tinte).
                {
                    byte id = MaterialId.MatDe(b, EstadoMateria.Solucion);
                    mats[id] = new MaterialDef
                    {
                        id = id, devName = $"Base{b}Solucion", archetype = MaterialArchetype.Liquid,
                        baseColor = LerpColor32(waterColor, tono, 0.60f), colorJitter = 10,
                        density = waterDensity, fluidity = 4, // DECISIÓN: densidad de la solución = la del agua (es sobre todo agua) -- el contrato no la fija.
                        boilsAt = waterBoilsAtRaw, boilsInto = MaterialId.MatDe(b, EstadoMateria.Polvo),
                        // DECISIÓN (LA ALQUIMIA VISIBLE, tarea 4 -- "disolución
                        // visible"): patron pasa de Liso/patronFuerza=0 a
                        // Motas/90, LA MISMA firma que Calcinado (calibrada ya
                        // en el playtest 20). `morph` es el campo que dibuja
                        // Motas (CLAUDE.md regla 16, "intensidad de chispa"):
                        // con patronFuerza=0 el campo existía pero
                        // SimRenderer.ComputeCellColor ni lo miraba (regla del
                        // gate `patronFuerza > 0`), así que el chispazo que
                        // Sim/SimStepper.cs va a sembrar en cada celda recién
                        // disuelta (ver ProcessDisolucionAgua) habría sido
                        // invisible sin este cambio. patronEscala baja de 3 a 2
                        // (igual que Calcinado) porque Motas dibuja MANCHAS
                        // sueltas, no vetas -- 3 las habría espaciado de más en
                        // el charco de la pila (interior 10x7, regla 24).
                        patron = PatronMorfologico.Motas, borde = BordeMorfologico.Difuso,
                        patronEscala = 2, patronFuerza = 90, ritmoAnim = 30, emision = 0,
                        semillaPatron = (byte)rng.Next(256),
                    };
                }
            }
        }

        // =====================================================================
        // SEMILLA CERO (playtest 40, CONTRATO_SEMILLA.md §3, ENCARGO M). Pasada
        // de OVERRIDES POST-GENERACIÓN: corre DESPUÉS de que Create() ya
        // terminó (leyes, firmas visuales, tablas de persistencia, TODO ya
        // baked). No reordena nada del sorteo normal -- solo AJUSTA, a mano,
        // los números de UNA base concreta (la "base 0 de Semilla Cero" que
        // este método elige, ver más abajo) para que el arco de beats de
        // DISENO_SEMILLA_CERO.md sea posible SIEMPRE con esta semilla, sin
        // dejarlo al azar del sorteo. Solo se llama cuando
        // Game/AlkahestGameBootstrap.ModoSemillaCero es true (Game/AlkahestSim.cs
        // es quien decide la seed y llama a este método -- ver el docblock de
        // CrearMundoInterno). El modo caótico NUNCA pasa por aquí: cada
        // Universe.Create(seed) normal sale intacto.
        //
        // POR QUÉ LA "BASE GANADORA" DE ESTE MÉTODO NO ES `GanadorGarantizado`:
        // la primera versión de este override usaba `BaseDe(GanadorGarantizado)`
        // como "la base del beat 1" (lectura literal de "la base ganadora del
        // solver" del contrato) -- pero para la seed 777002 GanadorGarantizado
        // ES el Calcinado de esa base (id 32, base 1, estado Calcinado): el
        // MISMO material que el override #2 necesita volver FRÁGIL (banda de
        // calcinación estrecha, se quema a Ash si se pasa de fuego) es el que
        // la garantía de persistencia necesita robusto (sobrevivir a
        // tempEnsayo+10, hasta 190 raw en el peor caso) para el Ensayo del
        // beat 5 ("¿DE VERDAD aguanta?"). Las dos exigencias son
        // matemáticamente incompatibles sobre el MISMO material. Resuelto
        // desacoplando: "la base del beat 1" es una DESIGNACIÓN de M (aquí,
        // <see cref="SemillaCeroBaseIdx"/>), no el resultado del solver de
        // persistencia -- ese solver sigue garantizando SU PROPIA promesa
        // (algo sobrevive al Ensayo) sin que este método la toque.
        /// <summary>La base que Semilla Cero designa como "la arena de sílice" del beat 1 (antes "el sedimento celeste": ver el override 4 de <see cref="AplicarOverridesSemillaCero"/>, ronda "LA QUÍMICA CON NOMBRE REAL") -- ver el docblock de ese método para el porqué de la designación explícita (no es GanadorGarantizado ni BaseCombustibleGarantizada de esta seed).</summary>
        public const int SemillaCeroBaseIdx = 0;

        // ===================================================================
        // (Encargo Q, ronda "LA QUÍMICA CON NOMBRE REAL", docs/DISENO_QUIMICA_REAL.md
        // §2) IDENTIDAD REAL: nombre + mini-reseña de trivia real, copiados VERBATIM
        // de la tabla canónica del diseño (cero parafraseo) para cada material del
        // arco de Semilla Cero -- las 5 bases × 8 estados de la seed congelada
        // 777002 (arena/arcilla/caliza/veta vegetal/sal, mismo mapeo baseIdx 0..4
        // que designa <see cref="SemillaCeroBaseIdx"/>) más los 9 "clásicos" del
        // arco (agua/vapor/hielo/fuego/humo/ceniza/brasa/limo/piedra).
        //
        // Tabla ESTÁTICA (no depende de ninguna instancia de Universe): la seed de
        // Semilla Cero está CONGELADA, así que qué baseIdx es "arena" o "sal" es un
        // dato FIJO del diseño para ESTA seed concreta, no algo que varíe entre
        // Create()s. Fuera de Semilla Cero (universo caótico) estos mismos matId de
        // base×estado NO tienen este significado -- el caótico sortea sus propias
        // propiedades por baseIdx y nunca llama a AplicarOverridesSemillaCero, así
        // que la tabla de abajo describe la seed 777002 y solo ella. Por eso los
        // consumidores (Game/SubstanceKnowledge.cs, Game/AlbumReal.cs del Encargo A)
        // tienen que consultarla SOLO bajo AlkahestGameBootstrap.ModoSemillaCero --
        // esta API no se autoprotege, es responsabilidad del llamante (documentado,
        // no impuesto: TieneIdentidadReal es una tabla pura, no consulta el modo).
        //
        // API CONGELADA para el Encargo A del álbum (§3 del diseño), que compila
        // contra ella EN PARALELO sin ver esta implementación todavía:
        // TieneIdentidadReal/NombreReal/ResenaReal. Strings CONSTANTES construidas
        // UNA vez en el inicializador estático -- nunca por frame, nunca
        // concatenadas (regla dura del proyecto: cero allocs por frame).
        // ===================================================================

        /// <summary>Nombre real + mini-reseña de trivia de un material (docs/DISENO_QUIMICA_REAL.md §2). Struct de solo lectura, nunca mutado tras <see cref="ConstruirIdentidadReal"/>.</summary>
        public readonly struct IdentidadReal
        {
            public readonly string Nombre;
            public readonly string Resena;
            public IdentidadReal(string nombre, string resena) { Nombre = nombre; Resena = resena; }
        }

        private static readonly IdentidadReal[] _identidadReal = ConstruirIdentidadReal();

        /// <summary>Construye la tabla de identidad UNA vez (inicializador estático) -- ver el bloque de comentarios de arriba para el porqué de cada decisión. Copiada VERBATIM de docs/DISENO_QUIMICA_REAL.md §2.</summary>
        private static IdentidadReal[] ConstruirIdentidadReal()
        {
            var tabla = new IdentidadReal[MaterialId.Count];

            // ---- Clásicos del arco (mismo trato) ----
            tabla[MaterialId.Stone] = new IdentidadReal("roca madre", "El hueso del mundo. El cincel la talla; las estaciones la respetan.");
            tabla[MaterialId.Water] = new IdentidadReal("agua", "El disolvente universal. Todo lo que este taller hace, lo hace con ella o contra ella.");
            tabla[MaterialId.Steam] = new IdentidadReal("vapor", "Agua con prisa. Atrápalo en frío y vuelve — eso hace el alambique.");
            tabla[MaterialId.Fire] = new IdentidadReal("fuego", "No es cosa: es proceso. Come combustible y aire.");
            tabla[MaterialId.Smoke] = new IdentidadReal("humo", "Lo que el fuego no alcanzó a comer. Tizna lo que toca.");
            tabla[MaterialId.Ash] = new IdentidadReal("ceniza", "El mineral que la planta juntó en vida. Mal combustible, buen dato.");
            tabla[MaterialId.Ice] = new IdentidadReal("hielo", "Agua ordenada en cristal. Flota sobre sí misma: rareza que permite la vida bajo los lagos.");
            tabla[MaterialId.Limo] = new IdentidadReal("lodo de cantera", "Agua y montaña molida: arena, arcilla, caliza, veta vegetal y sal, en suspensión. Todo lo demás sale de aquí — el calor los separa a cada uno a su temperatura.");
            tabla[MaterialId.Brasa] = new IdentidadReal("brasa", "Fuego en reposo. Sopla o alimenta, y vuelve.");

            // ---- base0 = ARENA (la dócil; extracción 100 — el milagro del beat 1) ----
            tabla[MaterialId.MatDe(0, EstadoMateria.Polvo)] = new IdentidadReal("arena de sílice", "Cuarzo molido por eras. De aquí nace el vidrio: los fenicios lo descubrieron en fogatas sobre playa.");
            tabla[MaterialId.MatDe(0, EstadoMateria.Fundido)] = new IdentidadReal("vidrio fundido", "A ~1700° la arena pierde su forma de cristal y fluye. Naranja de horno, dócil como miel.");
            tabla[MaterialId.MatDe(0, EstadoMateria.Templado)] = new IdentidadReal("vidrio", "Enfriado rápido queda duro y frágil: el vidrio de tus ventanas. La prensa lo revienta — pruébalo.");
            tabla[MaterialId.MatDe(0, EstadoMateria.Recocido)] = new IdentidadReal("vidrio recocido", "Enfriado con calma, pierde tensiones: los talleres reales lo hacen para poder cortarlo.");
            tabla[MaterialId.MatDe(0, EstadoMateria.Compacto)] = new IdentidadReal("arenisca", "Arena prensada: la piedra de las catedrales. El tiempo la hace en milenios; tu prensa, en un gesto.");
            tabla[MaterialId.MatDe(0, EstadoMateria.Ceramico)] = new IdentidadReal("vitrocerámica", "Cocida tras compactar: resiste el fuego que fundiría al vidrio común. Placas de cocina, naves.");
            tabla[MaterialId.MatDe(0, EstadoMateria.Calcinado)] = new IdentidadReal("arena tostada", "El calor la oscurece y seca. Paso previo al fundido en los hornos reales.");
            // Solucion: SIN ENTRADA a propósito (docs/DISENO_QUIMICA_REAL.md §2: "la
            // arena no se disuelve — sin entrada; su lección es la columna: SEDIMENTA").
            // El elemento por defecto del arreglo (Nombre==null) hace que
            // TieneIdentidadReal devuelva false para este matId, tal como pide el diseño.

            // ---- base1 = ARCILLA (flota fina en agua; extracción 122 — la 2ª arena) ----
            tabla[MaterialId.MatDe(1, EstadoMateria.Polvo)] = new IdentidadReal("arcilla", "Roca molida por el agua durante eras. Húmeda es plástica: toda la alfarería humana empieza aquí.");
            tabla[MaterialId.MatDe(1, EstadoMateria.Fundido)] = new IdentidadReal("barro vitrificado", "Pocas veces se funde del todo: los alfareros la vitrifican justo antes.");
            tabla[MaterialId.MatDe(1, EstadoMateria.Templado)] = new IdentidadReal("gres", "Cocción con enfriado brusco: duro, algo frágil. Vajilla de mesa.");
            tabla[MaterialId.MatDe(1, EstadoMateria.Recocido)] = new IdentidadReal("bizcocho", "Primera cocción suave del alfarero: poroso, listo para esmaltar o compactar.");
            tabla[MaterialId.MatDe(1, EstadoMateria.Compacto)] = new IdentidadReal("adobe", "Arcilla prensada y secada: el ladrillo más viejo del mundo. Media humanidad vive entre adobes.");
            tabla[MaterialId.MatDe(1, EstadoMateria.Ceramico)] = new IdentidadReal("cerámica", "Compactada y cocida a fuego pleno: terracota noble. Resiste el fuego, la prensa y los siglos.");
            tabla[MaterialId.MatDe(1, EstadoMateria.Calcinado)] = new IdentidadReal("ladrillo molido", "Arcilla quemada: ya no vuelve a ser plástica jamás. La 'chamota' de los ceramistas.");
            tabla[MaterialId.MatDe(1, EstadoMateria.Solucion)] = new IdentidadReal("barbotina", "Arcilla disuelta en agua: la 'crema' con la que los alfareros pegan piezas.");

            // ---- base2 = CALIZA (extracción 132) ----
            tabla[MaterialId.MatDe(2, EstadoMateria.Polvo)] = new IdentidadReal("caliza molida", "Esqueletos marinos de hace millones de años. La roca de la que sale la cal y el cemento.");
            tabla[MaterialId.MatDe(2, EstadoMateria.Fundido)] = new IdentidadReal("caliza fundida", "Solo un fuego brutal la funde; antes prefiere volverse cal.");
            tabla[MaterialId.MatDe(2, EstadoMateria.Templado)] = new IdentidadReal("caliche", "Costra dura de cal y arena. Suelos enteros del desierto son esto.");
            tabla[MaterialId.MatDe(2, EstadoMateria.Recocido)] = new IdentidadReal("cal apagada", "Cal reposada con calma: la pasta que une muros desde Roma.");
            // (playtest 47, ENCARGO C, rename #2 de la auditoría §2 de INFORME_REALIDAD.md, VERBATIM)
            tabla[MaterialId.MatDe(2, EstadoMateria.Compacto)] = new IdentidadReal("caliza prensada", "El mármol real pide eras de presión y calor. Esto es el primer paso — tu prensa hace la parte rápida.");
            // (playtest 47, ENCARGO C, rename #3: "clínker" pasa a ser el NOMBRE DEL CRUCE
            // caliza+arcilla, ver MaterialId.Clinker/Universe.TryCruce -- esta entrada
            // solo-caliza pasa a "cal sobrecocida", VERBATIM, la pista del cruce dicha por el material.)
            tabla[MaterialId.MatDe(2, EstadoMateria.Ceramico)] = new IdentidadReal("cal sobrecocida", "Caliza cocida de más, sin arcilla que la acompañe. Para clínker de verdad, mezcla.");
            tabla[MaterialId.MatDe(2, EstadoMateria.Calcinado)] = new IdentidadReal("cal viva", "Caliza quemada que SUELTA su aire antiguo. Con agua reacciona caliente: respétala.");
            tabla[MaterialId.MatDe(2, EstadoMateria.Solucion)] = new IdentidadReal("agua de cal", "Cal disuelta: se usaba para encalar casas y curar aguas.");

            // ---- base3 = VETA VEGETAL (el combustible garantizado; extracción 95) ----
            tabla[MaterialId.MatDe(3, EstadoMateria.Polvo)] = new IdentidadReal("turba", "Materia vegetal a medio camino de ser carbón. Arde mal, pero arde — y abona.");
            tabla[MaterialId.MatDe(3, EstadoMateria.Fundido)] = new IdentidadReal("brea", "Alquitrán vegetal fundido: con esto se sellaban los barcos.");
            // (playtest 47, ENCARGO C, rename #4 de la auditoría, VERBATIM)
            tabla[MaterialId.MatDe(3, EstadoMateria.Templado)] = new IdentidadReal("ámbar de brea", "Brea enfriada de golpe, quebradiza y translúcida. El ámbar real es resina con un millón de años de paciencia.");
            tabla[MaterialId.MatDe(3, EstadoMateria.Recocido)] = new IdentidadReal("brea dócil", "Enfriada despacio queda maleable: masilla de calafate.");
            tabla[MaterialId.MatDe(3, EstadoMateria.Compacto)] = new IdentidadReal("briqueta", "Turba prensada: el combustible de las estufas pobres de Europa entera.");
            tabla[MaterialId.MatDe(3, EstadoMateria.Ceramico)] = new IdentidadReal("carbón coquizado", "Cocido sin aire hasta ser casi puro carbono: el coque que funde el acero del mundo.");
            tabla[MaterialId.MatDe(3, EstadoMateria.Calcinado)] = new IdentidadReal("carbón vegetal", "Madera quemada sin aire: EL combustible del alquimista. Arde caliente y parejo.");
            tabla[MaterialId.MatDe(3, EstadoMateria.Solucion)] = new IdentidadReal("licor pardo", "Taninos vegetales disueltos: con esto se curtía el cuero.");

            // ---- base4 = SAL (extracción 154 — la última en soltar) ----
            tabla[MaterialId.MatDe(4, EstadoMateria.Polvo)] = new IdentidadReal("sal de roca", "Mar antiguo evaporado. Valió como moneda: de ahí viene 'salario'.");
            tabla[MaterialId.MatDe(4, EstadoMateria.Fundido)] = new IdentidadReal("sal fundida", "Líquida a ~800°: las plantas solares la usan para guardar calor. Y CONDUCE.");
            // (playtest 47, ENCARGO C, rename #1 de la auditoría, VERBATIM)
            tabla[MaterialId.MatDe(4, EstadoMateria.Templado)] = new IdentidadReal("sal de estampido", "La sal no se vuelve vidrio: REVIENTA al calentarse por el agua atrapada en sus cristales. Los cocineros lo llaman decrepitación.");
            tabla[MaterialId.MatDe(4, EstadoMateria.Recocido)] = new IdentidadReal("sal recristalizada", "Cristales grandes y ordenados, como la flor de sal.");
            tabla[MaterialId.MatDe(4, EstadoMateria.Compacto)] = new IdentidadReal("halita", "Sal prensada en roca: hay minas-catedral excavadas en ella (Wieliczka).");
            tabla[MaterialId.MatDe(4, EstadoMateria.Ceramico)] = new IdentidadReal("bloque salino", "Sal cocida y dura: los bloques que se dan a lamer al ganado.");
            tabla[MaterialId.MatDe(4, EstadoMateria.Calcinado)] = new IdentidadReal("sal tostada", "Seca y quebradiza; los cocineros la tuestan para ahumarla.");
            tabla[MaterialId.MatDe(4, EstadoMateria.Solucion)] = new IdentidadReal("salmuera", "Sal disuelta: el mar en un frasco. CONDUCE la electricidad — la lámpara lo delata.");

            // ---- RECETAS CRUZADAS (playtest 47, ENCARGO C, CONTRATO_FASE_A.md §1a) ----
            // Cinco reseñas VERBATIM del contrato + una (Clinker) COMPUESTA por C
            // (decisión explícita del contrato: "reseña actual del clínker del pt45
            // mejorada mencionando la arcilla" -- ver el bloque de comentarios junto
            // a MaterialId.Clinker para por qué es un id propio y no el rename #3).
            tabla[MaterialId.Mortero] = new IdentidadReal("mortero", "Cal apagada y arena: la pasta que pegó Roma entera. Fragua lento y para siempre.");
            tabla[MaterialId.VidrioVerde] = new IdentidadReal("vidrio de botella", "Arena fundida con ceniza: la potasa baja el punto de fusión. Así se hizo todo el vidrio de bosque medieval — verde por el hierro de la ceniza.");
            tabla[MaterialId.Lejia] = new IdentidadReal("lejía de ceniza", "Agua que pasó por ceniza y le robó la potasa. Limpia, quema, y con grasa haría jabón — la receta más vieja de la química doméstica.");
            tabla[MaterialId.Hormigon] = new IdentidadReal("hormigón", "Clínker molido, arena y agua: piedra líquida que fragua donde la viertas. El material más usado del planeta después del agua.");
            tabla[MaterialId.Esmaltado] = new IdentidadReal("cerámica esmaltada", "Bizcocho cocido con arena encima: la sílice vitrifica en la superficie. Brillo de vajilla noble.");
            tabla[MaterialId.Clinker] = new IdentidadReal("clínker", "Caliza y arcilla cocidas juntas a lo bruto: el corazón del cemento moderno. La arcilla era la pieza que faltaba — sola, la caliza solo se sobrecuece.");

            return tabla;
        }

        /// <summary>True si `matId` tiene identidad real de tabla (docs/DISENO_QUIMICA_REAL.md §2) -- false para la arena disuelta (sin entrada, ver ConstruirIdentidadReal) y para cualquier matId fuera de la tabla. API CONGELADA para el Encargo A del álbum: no consulta el modo de juego, es responsabilidad del llamante restringirse a Semilla Cero (ver el bloque de comentarios de arriba).</summary>
        public static bool TieneIdentidadReal(byte matId) => matId < _identidadReal.Length && _identidadReal[matId].Nombre != null;

        /// <summary>Nombre real VERBATIM de la tabla, o null si `matId` no tiene identidad (ver <see cref="TieneIdentidadReal"/>). API CONGELADA para el Encargo A.</summary>
        public static string NombreReal(byte matId) => TieneIdentidadReal(matId) ? _identidadReal[matId].Nombre : null;

        /// <summary>Mini-reseña de trivia VERBATIM de la tabla, o null si `matId` no tiene identidad (ver <see cref="TieneIdentidadReal"/>). API CONGELADA para el Encargo A.</summary>
        public static string ResenaReal(byte matId) => TieneIdentidadReal(matId) ? _identidadReal[matId].Resena : null;

        // ===================================================================
        // (Encargo Q) COLORES REALES DE LA TABLA -- aplicados por
        // AplicarOverridesSemillaCero (override 4). Alpha 0 = sin entrada
        // (arena.Solucion, la única, mismo motivo que en ConstruirIdentidadReal):
        // NO se sobreescribe, se deja el color que le tocó al sorteo normal de
        // esta seed. Jitter por estado: mismos valores que usaba
        // ConstruirEstadosDerivados para el sorteo normal (16/10/8/10/6/8/14/10
        // para Polvo/Fundido/Templado/Recocido/Compacto/Ceramico/Calcinado/Solucion)
        // -- el diseño no especifica jitter, así que se mantiene la textura ya
        // calibrada, solo cambia el TONO.
        // ===================================================================
        private static readonly Color32[,] _coloresRealesPorBaseEstado = new Color32[MaterialId.BasesCount, 8]
        {
            { // base0 ARENA
                new Color32(194, 178, 128, 255), new Color32(255, 150, 40, 255), new Color32(170, 215, 220, 255),
                new Color32(185, 210, 190, 255), new Color32(168, 140, 105, 255), new Color32(200, 196, 182, 255),
                new Color32(150, 120, 82, 255), default,
            },
            { // base1 ARCILLA
                new Color32(176, 110, 84, 255), new Color32(230, 120, 60, 255), new Color32(146, 120, 110, 255),
                new Color32(205, 160, 120, 255), new Color32(155, 118, 86, 255), new Color32(188, 86, 60, 255),
                new Color32(170, 84, 58, 255), new Color32(150, 105, 88, 255),
            },
            { // base2 CALIZA
                new Color32(205, 200, 188, 255), new Color32(250, 190, 120, 255), new Color32(196, 190, 172, 255),
                new Color32(222, 218, 205, 255), new Color32(214, 210, 200, 255), new Color32(150, 145, 138, 255),
                new Color32(235, 232, 222, 255), new Color32(210, 212, 200, 255),
            },
            { // base3 VETA VEGETAL
                new Color32(92, 72, 50, 255), new Color32(60, 45, 32, 255), new Color32(120, 84, 40, 255),
                new Color32(96, 70, 44, 255), new Color32(70, 58, 44, 255), new Color32(58, 56, 58, 255),
                new Color32(44, 40, 38, 255), new Color32(88, 66, 48, 255),
            },
            { // base4 SAL
                new Color32(238, 236, 230, 255), new Color32(255, 200, 110, 255), new Color32(226, 224, 214, 255),
                new Color32(240, 238, 228, 255), new Color32(222, 218, 206, 255), new Color32(206, 200, 186, 255),
                new Color32(216, 208, 190, 255), new Color32(198, 210, 206, 255),
            },
        };

        private static readonly byte[] _jitterPorEstado = { 16, 10, 8, 10, 6, 8, 14, 10 };

        /// <summary>
        /// Aplica los cuatro overrides del contrato §3 sobre un Universe ya
        /// creado con <see cref="SemillaCero"/> (o una semilla vecina si
        /// algún día hiciera falta recongelar). Documentados 1 a 1:
        ///
        /// 1. EXTRACCIÓN A FUEGO PROPIO, BANDA GENEROSA: <see cref="ExtraccionRaw"/>
        ///    de <see cref="SemillaCeroBaseIdx"/> baja a un valor muy por
        ///    debajo de <see cref="CrisolTier0Raw"/> (100, 20 raw de margen) --
        ///    la primera hornada de Limo a fuego propio SIEMPRE saca esta
        ///    base. Defensivo: cualquier OTRA base cuya banda natural caiga
        ///    en el hueco (100, tier0] se clampea por debajo de 100 -- si no,
        ///    `Game/Crisol.DecidirHornada` (que elige la banda MÁS ALTA
        ///    alcanzable, no la de esta base por nombre) podría sacar la base
        ///    equivocada en el beat 1.
        /// 2. BANDA DE CALCINACIÓN ESTRECHA + SOBRECALENTAMIENTO -&gt; ASH: la
        ///    banda natural del solver es ancha a propósito (CalcinadoUmbral =
        ///    FusionRaw+15..30, contrato 4.2) para que un combustible normal
        ///    NUNCA la cruce sin querer -- Semilla Cero quiere justo lo
        ///    contrario para esta base, así que se sobreescriben los tres
        ///    números implicados (FusionRaw, CalcinacionRaw y el umbral de
        ///    persistencia del Calcinado, vía <see cref="_umbralPersistenciaRaw"/>)
        ///    a mano. El camino "por encima de la banda -&gt; Ash" NO se
        ///    modela como una transición genérica de <c>MaterialDef.boilsAt</c>
        ///    (eso afectaría a TODAS las seeds, rompiendo "el caótico no
        ///    cambia" -- regla dura del contrato): vive como una comparación
        ///    contra la TABLA, detrás de <c>AlkahestGameBootstrap.ModoSemillaCero</c>,
        ///    en <c>Game/Crisol.DecidirHornada</c> -- ver el comentario ahí.
        ///    `Game/Crisol.TempReposoPara` también se ajustó (ver su
        ///    docblock) para que un Calcinado recién salido de la hornada no
        ///    se queme SOLO en reposo: esa función asumía CalcinadoUmbral
        ///    &gt; FusionRaw por construcción del solver, relación que este
        ///    override rompe a propósito.
        /// 3. CALCINADO COMBUSTIBLE GARANTIZADO: NO se toca nada -- el solver
        ///    de persistencia YA lo garantiza (<see cref="BaseCombustibleGarantizada"/>,
        ///    logueado como "combustible=base N"). Para 777002 es la base 3,
        ///    DISTINTA de <see cref="SemillaCeroBaseIdx"/> (0): es el
        ///    combustible que el jugador YA tiene cuando el Maestro pide
        ///    "más de eso, pero TOSTADO" -- el que, usado de más, dispara la
        ///    trampa del override 2. Se deja un Assert de solo-editor por si
        ///    una futura seed recongelada rompiera esta separación en
        ///    silencio.
        /// 4. COLORES REALES DE LA TABLA (Encargo Q, ronda "LA QUÍMICA CON NOMBRE
        ///    REAL", docs/DISENO_QUIMICA_REAL.md §2): las CINCO bases (no solo
        ///    <see cref="SemillaCeroBaseIdx"/>) se tiñen, estado a estado, con
        ///    <see cref="_coloresRealesPorBaseEstado"/> -- el color REAL de cada
        ///    variante ("arena de sílice", "vidrio", "arenisca"...), copiado
        ///    VERBATIM de la tabla del diseño. ANTES (playtest 40) solo la base0
        ///    se retiñía a un celeste inventado (100,190,230) y los 7 estados
        ///    derivados se recalculaban con las fórmulas HSV de
        ///    <see cref="ConstruirEstadosDerivados"/> (el método que las duplicaba,
        ///    <c>RecalcularEstadosDerivadosDeUnaBase</c>, se RETIRA en esta ronda:
        ///    ya no hace falta derivar un color por fórmula cuando el diseño da un
        ///    color EXPLÍCITO por estado). La arena (base0) YA NO es celeste: ese
        ///    retinte era un placeholder de la Semilla Cero original; el pivote de
        ///    "nombre real" lo sustituye por el color auténtico de la arena de
        ///    sílice, (194,178,128). Única excepción: la Solucion de base0 no
        ///    tiene entrada en la tabla ("la arena no se disuelve") y se deja con
        ///    el color que le tocó al sorteo normal de esta seed.
        ///
        /// ADEMÁS (fuera de la numeración 1-4 pero exigido por el contrato,
        /// "Ceniza combustible tier 0.5"): <see cref="MaterialId.Ash"/> se
        /// vuelve combustible SOLO en esta instancia de Universe (nunca en el
        /// caótico, que jamás llama a este método) -- ver el bloque
        /// correspondiente más abajo.
        /// </summary>
        public static void AplicarOverridesSemillaCero(Universe u)
        {
            const int b0 = SemillaCeroBaseIdx;

            // ---- Override 1: extracción a fuego propio, banda generosa ----
            const byte extraccionB0 = 100; // 20 raw por debajo de CrisolTier0Raw=120.
            u._extraccionRaw[b0] = extraccionB0;
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                if (b == b0) continue;
                // Defensivo (ver docblock arriba): ninguna otra base puede
                // quedar "más alcanzable" que b0 a fuego propio, o
                // Game/Crisol.DecidirHornada (Limo -> la banda MÁS ALTA
                // alcanzable) sacaría esa otra base en el beat 1.
                if (u._extraccionRaw[b] > extraccionB0 && u._extraccionRaw[b] <= CrisolTier0Raw)
                    u._extraccionRaw[b] = (byte)(extraccionB0 - 5);
            }

            // ---- Override 2: banda de calcinación estrecha + techo de Ash ----
            // Números elegidos con margen generoso entre sí (verificados a
            // mano contra Game/Crisol.DecidirHornada y TempReposoPara, ver el
            // informe de la ronda para la tabla completa):
            //   CrisolTier0Raw=120 < calcinacionB0=130 < ashTier0_5=145
            //     < techoCalcinadoB0=170 < fusionB0=220 < tierUnoDeB1(natural,~185 en 777002).
            // Espera: tierUnoDeB1 (165..190 en toda seed, ver TempCombustibleRawBase
            // en SortearTablaPersistencia) SIEMPRE cae por encima de
            // techoCalcinadoB0=170 con margen -- salvo el peor caso exacto
            // 165..169, donde la trampa del beat 4 no dispararía. Por eso el
            // Assert de más abajo: si la seed recongelada alguna vez cae ahí,
            // que falle ruidosamente en editor en vez de fallar en silencio
            // en la mesa de Cesar (regla 51 de CLAUDE.md).
            const byte fusionB0 = 220;
            const byte calcinacionB0 = 130;
            const byte techoCalcinadoB0 = 170;
            const byte ashTier0_5 = 145;

            byte polvoB0 = MaterialId.MatDe(b0, EstadoMateria.Polvo);
            byte calcinadoB0 = MaterialId.MatDe(b0, EstadoMateria.Calcinado);

            u._fusionRaw[b0] = fusionB0;
            u.Materials[polvoB0].meltsAt = fusionB0; // mantiene meltsAt (el mundo real) sincronizado con FusionRaw (la tabla que lee Crisol) -- regla 30/49 de CLAUDE.md.
            u._calcinacionRaw[b0] = calcinacionB0;
            u._umbralPersistenciaRaw[calcinadoB0] = techoCalcinadoB0;

            // ---- Override 3: calcinado combustible garantizado (verificación, no cambio) ----
            int baseCombustibleGarantizada = u.BaseCombustibleGarantizada;
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(baseCombustibleGarantizada != b0,
                "[ChaosAlchemy][SemillaCero] BaseCombustibleGarantizada coincide con SemillaCeroBaseIdx: la trampa del beat 4 (\"alimenta el brasero\") no tiene con qué dispararse -- recongelar SemillaCero con otra seed vecina.");
            byte tierUnoDeB1 = u.TempCombustibleRaw(MaterialId.MatDe(baseCombustibleGarantizada, EstadoMateria.Calcinado));
            UnityEngine.Debug.Assert(tierUnoDeB1 >= techoCalcinadoB0,
                "[ChaosAlchemy][SemillaCero] El combustible garantizado de esta seed no supera el techo de calcinación de la base 0: la trampa del beat 4 no se dispara -- recalibrar techoCalcinadoB0 o recongelar la seed.");
#endif

            // ---- Override 4: colores reales de la tabla (las CINCO bases) ----
            // Ver el docblock de arriba y el bloque de comentarios junto a
            // _coloresRealesPorBaseEstado: alpha 0 = sin entrada (solo
            // arena.Solucion), se deja el color natural del sorteo de esta seed.
            for (int b = 0; b < MaterialId.BasesCount; b++)
            {
                for (int e = 0; e < 8; e++)
                {
                    Color32 real = _coloresRealesPorBaseEstado[b, e];
                    if (real.a == 0) continue;
                    byte id = MaterialId.MatDe(b, (EstadoMateria)e);
                    u.Materials[id].baseColor = real;
                    u.Materials[id].colorJitter = _jitterPorEstado[e];
                }
            }

            // ---- CENIZA COMBUSTIBLE TIER 0.5 (contrato §3, último punto) ----
            // Ash se vuelve combustible SOLO en esta instancia -- ver el
            // docblock de la clase de arriba. "Enciende mal pero enciende":
            // combustReserva bajo (24 unidades a 8 ticks/unidad = 192 ticks
            // = 6,4s, MENOS de un tercio de los 24s del Calcinado(b1) de esta
            // misma seed) y calor/propagación modestos (residuo sucio de un
            // fracaso, no un combustible limpio).
            u._esCombustiblePorMaterial[MaterialId.Ash] = true;
            u._tempCombustibleRawPorMaterial[MaterialId.Ash] = ashTier0_5;
            var ashDef = u.Materials[MaterialId.Ash];
            ashDef.flammable = true;
            ashDef.ignitionTemp = ashTier0_5;
            ashDef.burnsInto = MaterialId.Fire; // camino legado, sin efecto real mientras combustReserva>0 (ver MaterialDef.combustReserva).
            ashDef.combustReserva = 24;
            ashDef.combustPasoTicks = 8;
            ashDef.combustCalorRaw = 10;    // menos que el Calcinado combustible de esta seed (18): "sube el fuego apenas por encima del propio".
            ashDef.combustHumoPct = 20;     // ceniza ardiendo mal ensucia más que limpio.
            ashDef.combustPropagacionPct = 10;
            ashDef.combustLenguaPct = 15;
            ashDef.combustResiduo = MaterialId.Empty; // ya es el residuo de un fracaso anterior: al agotarse, no deja nada más.

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log($"[ChaosAlchemy][SemillaCero] Overrides aplicados: base0={b0} (extraccion={u.ExtraccionRaw(b0)}, calcinacion={u.CalcinacionRaw(b0)}, techoCalcinado={u.UmbralPersistenciaRaw(calcinadoB0)}, fusion={u.FusionRaw(b0)}), combustibleGarantizado=base{baseCombustibleGarantizada} (tier1={u.TempCombustibleRaw(MaterialId.MatDe(baseCombustibleGarantizada, EstadoMateria.Calcinado))}), cenizaTier0_5={u.TempCombustibleRaw(MaterialId.Ash)}, ganadorGarantizado(sin tocar)={u.GanadorGarantizado}.");
#endif
        }

        // (Encargo Q) `RecalcularEstadosDerivadosDeUnaBase` -- que duplicaba las
        // fórmulas HSV de ConstruirEstadosDerivados para recolorear una base tras
        // el override de Polvo -- se RETIRA en esta ronda: ver el override 4 de
        // AplicarOverridesSemillaCero arriba. Ya no hace falta derivar un color
        // por fórmula cuando la tabla real (_coloresRealesPorBaseEstado) da un
        // color EXPLÍCITO por estado, copiado VERBATIM del diseño -- inventar uno
        // por fórmula habría contradicho la propia tabla que se supone que se está
        // aplicando. No reimplantar sin releer docs/DISENO_QUIMICA_REAL.md §2.
    }
}
