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
    /// FAMILIA MORFOLÓGICA: cómo se organiza la materia POR DENTRO (playtest 12).
    ///
    /// Idea de Cesar, y es la buena: *"la morfología puede ser una propiedad del
    /// material, no una forma rígida"* y *"no necesitas que al aspirarlo conserve
    /// exactamente el dibujo píxel por píxel; necesitas que cuando vuelva a
    /// existir, vuelva a TENDER a formar ese tipo de patrón"*.
    ///
    /// Por eso el patrón NO se guarda: se REGENERA. Cada celda lleva un byte de
    /// estado morfológico (<see cref="CellGrid.morph"/>) que evoluciona por una
    /// regla local propia de la familia. Da igual dónde acabe la materia: la
    /// regla la vuelve a llevar hacia su atractor. Aspiras manchas y viertes
    /// manchas, aunque no sean las mismas manchas.
    /// </summary>
    public enum PatronMorfologico : byte
    {
        /// <summary>Sin dibujo interno. El vocabulario del taller (agua, arena, aceite) vive aquí: son las constantes del mundo y deben verse iguales en toda partida.</summary>
        Liso = 0,
        /// <summary>Vetas de mármol: ruido deformado, estable por posición. Quieto y mineral.</summary>
        Vetas = 1,
        /// <summary>Reacción-difusión en régimen de puntos: manchas que se reparten y compiten por el espacio.</summary>
        Manchas = 2,
        /// <summary>Reacción-difusión en régimen de bandas: serpentinas laberínticas.</summary>
        Laberinto = 3,
        /// <summary>Teselas tipo Voronoi con borde marcado: espuma, tejido celular.</summary>
        Celdas = 4,
        /// <summary>Crecimiento ramificado tipo DLA: agujas y dendritas que parten de semillas.</summary>
        Dendritas = 5,
        /// <summary>Sin dibujo, pero LATE: el brillo respira por zonas con desfase espacial.</summary>
        Pulso = 6,
        /// <summary>Motas dispersas que se encienden y se apagan. Materia inquieta.</summary>
        Motas = 7,
    }

    /// <summary>Cómo termina la materia contra el vacío. Es lo primero que el ojo compara entre dos sustancias (playtest 12).</summary>
    public enum BordeMorfologico : byte
    {
        /// <summary>Corte limpio.</summary>
        Neto = 0,
        /// <summary>Aureola tenue del propio color, un píxel hacia fuera.</summary>
        Halo = 1,
        /// <summary>Cristalitos claros en el contorno.</summary>
        Escarcha = 2,
        /// <summary>Se deshilacha: el borde pierde opacidad de forma irregular.</summary>
        Difuso = 3,
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

        // -----------------------------------------------------------------
        // GRAVEDAD CON COHESIÓN (playtest 29, decisión de Cesar: "haz lo de
        // la gravedad... ¿todo pixel necesita base o habrá un principio de
        // cohesión con apoyos sensatos?" -- se eligió COHESIÓN).
        // -----------------------------------------------------------------
        /// <summary>¿Este StaticSolid CAE cuando pierde apoyo? La PIEDRA jamás (es la arquitectura del mundo entero) y la obra del taller tampoco; los productos sólidos del retículo, el hielo y el cristal sí. Solo aplica a archetype StaticSolid.</summary>
        public bool caeSolido;
        /// <summary>Alcance de MÉNSULA en celdas: un sólido sin apoyo directo se sostiene si a ≤ este número de celdas en horizontal, a través de materia sólida CONTINUA, hay una celda con apoyo debajo. Fijo por estado (vocabulario, regla 17): lo cerámico voladiza más que lo templado. 0 = sin cohesión (cae si no tiene apoyo debajo).</summary>
        public byte cohesionCeldas;
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

        /// <summary>Vida media en ticks para gases/fuego. 0 = eterno (no expira). Para MaterialId.Brasa (Powder) se reutiliza como semilla base de vida en UNIDADES de <c>SimStepper.BrasaLifeUnitTicks</c> ticks cada una -- ver SimStepper.ConvertirEnBrasa.</summary>
        public byte gasLifetime;

        /// <summary>Si es true, SimRenderer le añade un resplandor/tinte adicional (fuego, brasas, Vivium).</summary>
        public bool emitsGlow;

        // =================================================================
        // COMBUSTIÓN PERSISTENTE (playtest 39, contrato ENCARGO S 1a)
        // =================================================================
        // El fuego de siempre (MaterialArchetype.Fire) nacía y moría solo
        // (aux = vida en ticks) sin importarle si tenía combustible debajo:
        // era un actor sin memoria. Estos campos convierten al COMBUSTIBLE
        // mismo (el aceite, un polvo Calcinado inflamable...) en la celda que
        // arde de verdad -- Fire pasa a ser solo la LENGUA VISIBLE que
        // escupe. Ver SimStepper.ProcessCombustion para el consumidor.
        //
        // combustReserva == 0 es EL GATE: significa "este material no
        // participa del sistema nuevo" y conserva el camino LEGADO de
        // siempre (ignitionTemp -> Transform directo a burnsInto, igual que
        // en toda ronda anterior). Por diseño de esta ronda (contrato 1a:
        // "el aceite y los calcinados combustibles de las bases... reciben
        // valores") SOLO Oil y el/los Calcinado(s) que Universe.EsCombustible
        // marca reciben combustReserva>0 -- Slime/Azoth-inflamables-por-seed
        // y Nutrient/Vivium (también `flammable`) se QUEDAN a propósito en
        // el camino legado esta ronda: decisión de alcance explícita, no un
        // olvido (ver el informe de la ronda). Extenderlos es tan barato
        // como rellenar estos mismos campos en una ronda futura.
        /// <summary>
        /// Unidades de reserva de combustible. 0 = material fuera del
        /// sistema de combustión persistente (legado). Para arquetipo
        /// Liquid, CellGrid.aux solo tiene 7 bits libres para esto (el bit 0
        /// ya lo usa la memoria de flujo horizontal, ver
        /// SimStepper.ProcessLiquid) -- CLAMP a 127 para cualquier líquido
        /// combustible, documentado donde se lee/escribe
        /// (SimStepper.GetCombustReserva/SetCombustReserva). Para Powder,
        /// aux está libre entero: 0..255.
        /// </summary>
        public byte combustReserva = 0;

        /// <summary>Ticks entre cada unidad de reserva consumida -- SIEMPRE potencia de 2 (el muestreo usa una máscara de bits barata, no módulo: ver SimStepper.ProcessCombustion). Cuanto más alto, más lento arde para la misma reserva.</summary>
        public byte combustPasoTicks = 8;

        /// <summary>Calor (raw) inyectado a la propia celda y a los 4 vecinos ortogonales (vía SimStepper.InjectHeat) en cada paso de combustión muestreado -- más suave que la lengua de Fire, que fija su propia celda a 255.</summary>
        public byte combustCalorRaw = 15;

        /// <summary>Probabilidad % por paso de combustión de soltar un Smoke en un vecino vacío.</summary>
        public byte combustHumoPct = 12;

        /// <summary>Probabilidad % por paso de combustión de intentar encender cada vecino inflamable (agresividad de propagación; aparte del contacto ya-caliente, que sigue siendo instantáneo).</summary>
        public byte combustPropagacionPct = 15;

        /// <summary>Probabilidad % por paso de combustión de escupir una lengua de Fire visible en la celda vacía justo encima.</summary>
        public byte combustLenguaPct = 35;

        /// <summary>Material que queda cuando la reserva se agota. Líquidos: normalmente Empty ("arde sin dejar nada", el humo ya emitido durante la quema es todo el rastro). Sólidos/polvos: MaterialId.Brasa.</summary>
        public byte combustResiduo = MaterialId.Empty;

        // =================================================================
        // FIRMA VISUAL (playtest 12) — la identidad de una sustancia
        // =================================================================
        // Cesar, tras terminar las tres jornadas y empezar otro universo:
        // *"al final, al escoger otro universo, solo tuve más de lo mismo"*.
        // Tenía razón: hasta ahora la variación por seed era SOLO NUMÉRICA
        // (probabilidades, bandas de temperatura, Edictos). Dos partidas se
        // veían idénticas porque la materia se veía idéntica.
        //
        // Una sustancia se reconoce por la COMBINACIÓN de estos campos, no por
        // uno solo. Con 8 familias × 4 bordes × escala × fuerza × ritmo ×
        // emisión × semilla, el espacio de firmas es enorme, y —lo importante—
        // se PERCIBE distinto, que es lo que pedía el reporte.
        //
        // REGLA DE DISEÑO: esto solo se sortea por seed para LO INNOMINADO
        // (Azoth, Semilla de Cristal, Cristal, Vivium, Limo, Ácido). El
        // vocabulario del taller (agua, arena, aceite, nutriente, fuego, humo,
        // ceniza, vapor, hielo) se ve SIEMPRE igual: son el suelo firme desde
        // el que el jugador juzga lo demás. Si todo cambia, nada se reconoce.
        // Ver CLAUDE.md regla 13 (dos clases de material).

        /// <summary>Familia morfológica: cómo se organiza la materia por dentro.</summary>
        public PatronMorfologico patron = PatronMorfologico.Liso;

        /// <summary>Cómo termina la materia contra el vacío.</summary>
        public BordeMorfologico borde = BordeMorfologico.Neto;

        /// <summary>Tamaño del rasgo del patrón, en celdas (1..8). Pequeño = grano fino; grande = manchas anchas.</summary>
        public byte patronEscala = 3;

        /// <summary>Contraste del patrón sobre el color base (0 = invisible, 255 = brutal). Valores útiles ~40..150: la materia debe seguir leyéndose por su color.</summary>
        public byte patronFuerza = 0;

        /// <summary>Velocidad de la animación del patrón. 0 = QUIETO (importante: no todo debe moverse, o la pantalla se vuelve ruido).</summary>
        public byte ritmoAnim = 0;

        /// <summary>Luz propia (0..255). Distinta de <see cref="emitsGlow"/>, que es el parpadeo heredado del fuego.</summary>
        public byte emision = 0;

        /// <summary>
        /// Desplaza todos los hashes del patrón. Dos materiales de la MISMA
        /// familia con la misma escala se calcarían píxel a píxel sin esto.
        /// </summary>
        public byte semillaPatron = 0;
    }
}
