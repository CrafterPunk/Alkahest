using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>Transformaciones "notables" que el aprendiz puede haber presenciado, una por SimEventType.</summary>
    [Flags]
    public enum WitnessFlags : byte
    {
        None = 0,
        Arder = 1 << 0,       // SimEventType.Ignite
        Cristalizar = 1 << 1, // SimEventType.Crystallize
        Crecer = 1 << 2,      // SimEventType.Grow
        Disolverse = 1 << 3,  // SimEventType.Dissolve
        Hervir = 1 << 4,      // SimEventType.Boil
        Congelarse = 1 << 5,  // SimEventType.Freeze
    }

    /// <summary>
    /// "Qué sabe el aprendiz" sobre cada material del universo, por
    /// materialId: si lo ha descubierto, qué nombre le ha puesto (null =
    /// "???" todavía) y qué transformaciones le ha visto sufrir.
    ///
    /// Descubrimiento: cualquier material que entre en el Frasco (sondeo de
    /// los conteos de <see cref="Flask"/> cada <see cref="FlaskPollInterval"/>
    /// segundos) O que el jugador mantenga bajo el cursor
    /// <see cref="HoverDiscoverSeconds"/> segundos seguidos.
    ///
    /// Presenciar: se consume el ring buffer de eventos notables de
    /// SimStepper cada frame (con un "lastSeenHead" propio, igual que
    /// recomienda su doc-comment), traduciendo cada SimEventType al
    /// material FUENTE del evento. Nota: "dentro de la vista de cámara" es
    /// trivialmente cierto siempre en esta build (una única cámara fija
    /// ortográfica que encuadra todo el nivel), así que no se comprueba.
    ///
    /// =====================================================================
    /// (fix playtest 10) "¿POR QUÉ BAUTIZARÍA VIVIUM SI EL JUEGO YA LE LLAMA
    /// ASÍ?" -- el reporte tenía razón: la fantasía (docs/DECISIONS.md §2/§11/
    /// §12, "no haces pociones: aprendes física alienígena... y le pones
    /// nombre") se rompía porque casi todo llegaba YA bautizado por el
    /// juego. Aquí se reparte el roster en dos clases y <see cref="NombreComun"/>
    /// es la ÚNICA fuente de verdad de a cuál pertenece cada material (null
    /// = lo innominado, hay que descubrirlo y bautizarlo).
    ///
    ///   VOCABULARIO DEL TALLER (nombre común desde la celda 1, el Maestro ya
    ///   os lo enseñó -- nadie bautiza el agua):
    ///     Stone     "piedra"     -- arquitectura del taller (muros/cubas), no es una sustancia que se descubra.
    ///     Sand      "arena"      -- grifo del banco.
    ///     Water     "agua"       -- grifo del banco.
    ///     Oil       "aceite"     -- grifo del banco.
    ///     Nutrient  "nutriente"  -- grifo del banco.
    ///     Steam     "vapor"      -- fase mundana del agua al hervir, cualquiera la reconoce.
    ///     Smoke     "humo"       -- fase mundana de la combustión, cualquiera la reconoce.
    ///     Fire      "fuego"      -- fenómeno mundano, cualquiera lo reconoce (aunque en este taller se FABRIQUE, no se compre).
    ///     Ash       "ceniza"     -- residuo mundano de arder, cualquiera lo reconoce.
    ///     Ice       "hielo"      -- fase mundana del agua al congelarse, cualquiera lo reconoce.
    ///
    ///   LO INNOMINADO (NombreComun devuelve null -&gt; "???" hasta bautizar;
    ///   nadie, ni el Maestro, tiene un nombre para esto):
    ///     Azoth        -- líquido de la reserva del Maestro: SÍ sale de un grifo (el quinto, sellado
    ///                     hasta la jornada 2), pero es justo lo que "él tampoco sabe qué es" -- por
    ///                     eso lo guarda aparte y lo suelta como muestra, no como básico de taller.
    ///     CrystalSeed  -- semilla rara que el Maestro os entrega SIN nombre (ver Game/MasterSupplies.cs):
    ///                     es la definición misma de "lo innominado" del diseño.
    ///     Crystal      -- producto de una reacción exótica de este universo (Azoth+semilla en frío,
    ///                     ver Sim/Universe.cs), nadie lo ha clasificado todavía.
    ///     Vivium       -- semilla orgánica que el Maestro entrega sin nombre; el mínimo exigido por
    ///                     el diseño y el ejemplo textual del reporte que originó este cambio.
    ///     Slime        -- sin grifo propio; solo aparece al neutralizar Ácido con Agua (reacción de
    ///                     Sim/Universe.cs) -- un subproducto exótico de una reacción, de libro.
    ///     Acid         -- sin grifo propio; no hay fuente normal de partida en esta build (roster
    ///                     reservado para extensiones futuras/paleta dev F3) -- por eso mismo nunca
    ///                     puede ser "vocabulario de taller": nadie del taller lo usa todavía.
    ///
    ///   NOTA DE ALCANCE: Game/MasterSupplies.cs y Game/Dispenser.cs son de SOLO LECTURA en esta
    ///   ronda y siguen nombrando "Azoth" en su diálogo/chapa de grifo -- es una inconsistencia
    ///   residual conocida (ver informe de esta ronda), no un descuido: arreglarla exige tocar esos
    ///   dos archivos, fuera del alcance permitido aquí.
    /// =====================================================================
    /// </summary>
    public sealed class SubstanceKnowledge : MonoBehaviour
    {
        private const float FlaskPollInterval = 0.5f;
        private const float HoverDiscoverSeconds = 1f;

        // ---------------------------------------------------------------------------------
        // (fix playtest 9) "LEY DESCUBIERTA": el jugador reportó que llevaba horas sin
        // entender que las muestras del Maestro son SEMILLAS (catalizadores que no se
        // gastan), no ingredientes. El malentendido es invisible porque las leyes que lo
        // desmentirían ocurren en celdas diminutas, lejos del cursor, sin ningún aviso: la
        // primera vez que pasan de verdad, nadie las ve pasar. Aquí se anuncian UNA vez
        // cada una, con la frase ejecutable de la ley, no una descripción poética.
        //
        // (playtest 18, CONTRATO_FASE3.md) REESCRITO: con química sorteada por semilla hay
        // entre 13 y 16 leyes por universo (7 núcleo + 5-8 sorteadas + 1 crecimiento), no
        // 2 fijas -- los dos `bool` cableados a mano (`_leyCristalDescubierta`,
        // `_leyVivumDescubierta`) y los dos constructores de texto a mano
        // (ConstruirTextoLeyCristal/Vivium) ya no valen: ninguna ley se conoce en tiempo de
        // compilación. El registro pasa a ser POR ÍNDICE (`_leyDescubierta`, dimensionado
        // con `Universe.Leyes.Length` en Init) y el disparador es el evento ADICIONAL
        // `SimEventType.Ley` (no sustituye a Crystallize/Grow/etc., que se siguen
        // consumiendo igual para las witness flags -- ver ApplyWitness/ApplyLey). El texto
        // ya no se escribe a mano por ley: se GENERA desde el descriptor `LeyDelUniverso`
        // con una plantilla por `FormaDeLey` (ver ConstruirTextoLey).
        //
        // Cola de LeyBannerCapacidad (subida de 2 a 8, ver el comentario junto a la
        // constante): un vertido puede disparar varias reacciones casi a la vez.
        // ---------------------------------------------------------------------------------
        private const float LeyBannerDuracionSeg = 7f;

        // (playtest 18) ERA 2 ("solo 2 leyes de este tipo existen"): con química por seed
        // eso es falso -- puede haber 13-16 leyes y varias descubrirse en el mismo puñado
        // de ticks (un vertido grande dispara reacciones en cadena). 8 da margen amplio sin
        // reservar memoria real (son solo dos arrays paralelos de referencias/bytes). Si la
        // cola se llena de todas formas, EncolarLeyBanner descarta el banner en silencio --
        // pero el registro en `_leyDescubierta` ya se hizo ANTES de encolar (ver ApplyLey),
        // así que nunca se pierde que la ley fue presenciada, solo el aviso visual de esa vez.
        private const int LeyBannerCapacidad = 8;

        private AlkahestSim _sim;
        private Flask _flask;

        private readonly bool[] _discovered = new bool[MaterialId.Count];
        private readonly string[] _playerName = new string[MaterialId.Count];
        private readonly WitnessFlags[] _witness = new WitnessFlags[MaterialId.Count];

        // -----------------------------------------------------------------
        // (playtest 25, CONTRATO_PERSISTE.md §2/§6.4) BASES INNOMINADAS: las
        // <see cref="MaterialId.BasesCount"/> materias base viven en HASTA 8
        // MaterialId cada una (Polvo/Fundido/Templado/.../Solucion, ver
        // Alkahest.Sim.EstadoMateria) pero "una base = un nombre, no ocho" --
        // bautizar CUALQUIER estado nombra la BASE entera; el nombre
        // guardado aquí NUNCA lleva el sufijo de estado (eso es cosmético,
        // ver SufijoEstado/NombreParaHud) para que reabrir NamingUi sobre
        // otro estado de la misma base y volver a pulsar "Bautizar" sin
        // tocar el campo no vaya ACUMULANDO sufijos ("arena (fundido)
        // (fundido)..."). _playerName sigue siendo la fuente de verdad para
        // TODO lo demás innominado (Azoth/CrystalSeed/Crystal/Vivium/Slime/
        // Acid, el roster viejo): esas no tienen "base" que agrupar.
        // -----------------------------------------------------------------
        private readonly string[] _baseName = new string[MaterialId.BasesCount];

        // -----------------------------------------------------------------
        // (playtest 25, CONTRATO_PERSISTE.md §5.3/§6.4) OBSERVACIONES DE
        // PROPIEDAD: líneas de texto libre presenciadas por el jugador
        // (BancoChispa: "encendió la lámpara"/"la lámpara ni parpadeó";
        // EnsayoMaestro: cómo murió o sobrevivió una muestra) -- DISTINTAS
        // de <see cref="WitnessFlags"/> (un enum cerrado de 6 transformaciones
        // de SimStepper, chips fijos). Aquí es texto libre acumulado por
        // material, mismo criterio "una vez por línea distinta" que
        // BuildChips en JournalHud.cs usa para las WitnessFlags: sin
        // duplicar la misma observación si el jugador repite el mismo
        // ensayo/análisis dos veces sobre el mismo material.
        // -----------------------------------------------------------------
        private readonly string[] _observaciones = new string[MaterialId.Count];

        private float _flaskPollTimer;
        private int _lastEventHead;

        private byte _hoverMatId;
        private float _hoverTimer;

        // Cuántas veces se ha bautizado/renombrado algo (nunca decrece): JournalHud lo usa
        // como parte de su firma de "¿hay que reconstruir el texto cacheado?" -- CountNamed()
        // por sí solo no detecta un RE-bautizo (mismo material, nombre nuevo), porque el
        // conteo de nombrados no cambia.
        public int NamingVersion { get; private set; }

        // Estado de la ley "descubierta" (ver doc arriba). (playtest 18) Registro POR
        // ÍNDICE de Universe.Leyes -- dimensionado en Init, cuando Universe ya existe
        // (AlkahestGameBootstrap.TrySpawn no llama a Init hasta que _sim.Universe != null,
        // ver AlkahestGameBootstrap.cs). null hasta entonces; todos los accesos públicos
        // (LeyDescubierta/CountLeyesDescubiertas) y ApplyLey se defienden de ese caso.
        private bool[] _leyDescubierta;

        /// <summary>Sube cada vez que se descubre una ley (nunca decrece). Ver LeyesVersion del contrato: JournalHud lo mete en su firma de caché para saber cuándo reconstruir el texto de la sección LEYES.</summary>
        public int LeyesVersion { get; private set; }

        // Cola FIFO fija de textos de banner ya construidos (se construyen una única vez,
        // al disparar el evento -- nunca en OnGUI/Update). _leyBannerColaMat es paralelo a
        // _leyBannerCola: guarda el matId que MaterialParaInvitarBautizo eligió para esa
        // ley, para poder encadenar el aviso de bautizo justo cuando el banner de esa ley
        // termina (fix playtest 10, ver DispararAvisoBautizoTrasLey).
        private readonly string[] _leyBannerCola = new string[LeyBannerCapacidad];
        private readonly byte[] _leyBannerColaMat = new byte[LeyBannerCapacidad];
        // (playtest 25) Título por entrada de la cola: "LEY DESCUBIERTA" para
        // ApplyLey, "¡NUEVO PROCEDIMIENTO!" para ActualizarPatentes -- MISMA
        // cola/animación/panel (ver DrawLeyBanner), solo cambia el rótulo, en
        // vez de duplicar toda la maquinaria de banner para un segundo tipo
        // de aviso "estilo LEY DESCUBIERTA" (contrato §6.4, literal).
        private readonly string[] _leyBannerColaTitulo = new string[LeyBannerCapacidad];
        private int _leyBannerColaCount;
        private int _leyBannerColaLeidos;
        private string _leyBannerActual;
        private string _leyBannerActualTitulo;
        private byte _leyBannerActualMat;
        private float _leyBannerHasta;

        /// <summary>(playtest 25) Último Hornada.PatenteCount visto -- sube solo al CONGELAR una patente nueva (a diferencia de PatentesVersion, que también sube al bautizar), así que es la señal correcta de "hay un descubrimiento nuevo que anunciar", nunca un re-bautizo.</summary>
        private int _ultimoPatenteCountVisto;

        // ---------------------------------------------------------------------------------
        // (fix playtest 10) INVITACIÓN A BAUTIZAR: "esto no tiene nombre — T para
        // bautizarlo". Mismo criterio de "avisar y luego callar" que ya usa
        // Game/DeliveryChute.cs con su chatarra (_scrapWarned: un bool por material, el
        // aviso completo sale UNA vez por material y no vuelve a repetirse para ESE
        // material) -- aquí con el mismo patrón: un aviso por cada sustancia innominada
        // distinta que el jugador llegue a apuntar/cargar, nunca más de una vez cada
        // una. Con 6 sustancias innominadas en el roster (ver tabla de arriba) esto da,
        // de forma natural, "las primeras veces y calla" sin inventar un contador nuevo.
        // Se dispara desde dos sitios: ActualizarAvisoBautizo (apuntar/cargar) y
        // DispararAvisoBautizoTrasLey (encadenado al final del banner "LEY
        // DESCUBIERTA", para que descubrir la ley y la invitación a nombrar su
        // producto se sientan un mismo momento, no dos).
        // ---------------------------------------------------------------------------------
        private const float AvisoBautizoDuracionSeg = 3.5f;
        private readonly bool[] _avisoBautizoMostrado = new bool[MaterialId.Count];
        private float _avisoBautizoHasta;

        // ---------------------------------------------------------------------------------
        // (playtest 32, encargo C) "ALGO NUEVO": Cesar, jugando el 32 --
        // "cuando descubro algo, debe salirme en pantalla la opción de bautizarlo, porque
        // si no no me entero". El globo de arriba (DrawAvisoBautizo) es discreto A
        // PROPÓSITO y solo aparece si el jugador vuelve a APUNTAR/CARGAR la sustancia --
        // si el descubrimiento ocurrió por sondeo pasivo (PollFlask/PollHover) y el
        // jugador sigue a lo suyo, puede no verlo nunca. Este es el aviso GRANDE, en el
        // MOMENTO EXACTO del descubrimiento: reutiliza la MISMA cola/panel que "LEY
        // DESCUBIERTA"/"¡NUEVO PROCEDIMIENTO!" (ver EncolarLeyBanner/_leyBannerCola*),
        // así que "cola si se descubren varios" sale gratis -- es la misma FIFO. El
        // gate "un solo aviso por material" es el MISMO `_avisoBautizoMostrado` que ya
        // usaba el globo -- las dos vías comparten el flag a propósito: si este banner
        // ya invitó a bautizar un material, el globo no vuelve a insistir (y viceversa).
        // ---------------------------------------------------------------------------------
        private const string TituloDescubrimiento = "ALGO NUEVO";

        /// <summary>
        /// ÚNICO punto de entrada para poner `_discovered[matId]` a true desde fuera de
        /// <see cref="Bautizar"/> (que ya lo hace como efecto colateral de nombrar, sin
        /// invitación posible: si lo acabas de bautizar no hace falta invitarte a
        /// bautizarlo). Detecta la TRANSICIÓN false-&gt;true -- sin ella no hay "momento
        /// de descubrir" que anunciar -- y si el material es innominado y todavía no se
        /// le ha ofrecido bautizo por ninguna vía, encola "ALGO NUEVO".
        /// </summary>
        private void MarcarDescubierto(byte matId)
        {
            if (matId == MaterialId.Empty || matId >= MaterialId.Count) return;
            if (_discovered[matId]) return; // ya lo sabíamos -- sin transición no hay nada que anunciar.
            _discovered[matId] = true;
            if (!NecesitaBautizo(matId)) return; // vocabulario de taller o ya tiene nombre (regla 13/17): nada que anunciar.
            if (_avisoBautizoMostrado[matId]) return; // ya se le ofreció bautizo por otra vía -- un solo aviso por material.
            _avisoBautizoMostrado[matId] = true;
            EncolarLeyBanner(ConstruirTextoDescubrimiento(matId), matId, TituloDescubrimiento);
        }

        /// <summary>Texto del banner "ALGO NUEVO": describe por ORIGEN/EFECTO (RespaldoLey, regla 13/17 -- nunca la identidad interna de algo que el HUD sigue enseñando como "???"). Construido UNA vez al descubrir, nunca en OnGUI.</summary>
        private static string ConstruirTextoDescubrimiento(byte matId)
        {
            return "Has descubierto " + RespaldoLey(matId) + ". Pulsa T para bautizarlo.";
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Flask flask)
        {
            _sim = sim;
            _flask = flask;

            // (playtest 18) Dimensionado aquí y no como inicializador de campo porque el
            // tamaño depende de la seed (Universe.Leyes.Length varía entre 13 y 16, ver
            // CONTRATO_FASE3.md secciones 1 y 6) -- AlkahestGameBootstrap.TrySpawn garantiza
            // que _sim.Universe ya existe en este punto, pero se defiende igual por si
            // algún día Init se llama desde otro sitio.
            int count = (_sim != null && _sim.Universe != null) ? _sim.Universe.Leyes.Length : 0;
            _leyDescubierta = new bool[count];
        }

        public bool EsDescubierto(byte matId) => matId < MaterialId.Count && _discovered[matId];

        /// <summary>¿El jugador ha PRESENCIADO la ley `indiceLey` (índice de Universe.Leyes)? Contrato sección 5. Un índice fuera de rango devuelve false sin romper nada.</summary>
        public bool LeyDescubierta(int indiceLey)
        {
            return _leyDescubierta != null && indiceLey >= 0 && indiceLey < _leyDescubierta.Length && _leyDescubierta[indiceLey];
        }

        /// <summary>Cuántas leyes ha presenciado (la N del "N de M" del diario). Contrato sección 5.</summary>
        public int CountLeyesDescubiertas()
        {
            if (_leyDescubierta == null) return 0;
            int n = 0;
            for (int i = 0; i < _leyDescubierta.Length; i++) if (_leyDescubierta[i]) n++;
            return n;
        }

        /// <summary>
        /// Nombre puesto por el jugador, o "???" si todavía no se ha
        /// bautizado (o el id es inválido). (playtest 25) Para una
        /// base×estado (<see cref="MaterialId.EsBaseEstado"/>) devuelve el
        /// nombre de la BASE, SIN sufijo de estado -- este es el valor que
        /// NamingUi.cs (fuera del alcance de este encargo) precarga en su
        /// campo de texto al reabrir la ventana para renombrar: si llevara
        /// el sufijo ("arena (fundido)"), un re-bautizo sin editar el campo
        /// grabaría el sufijo COMO SI fuera el nombre de la base. El sufijo
        /// es puramente cosmético y vive en <see cref="NombreParaHud"/>.
        /// </summary>
        public string NombreDe(byte matId)
        {
            if (matId >= MaterialId.Count) return "???";
            if (MaterialId.EsBaseEstado(matId))
            {
                string baseName = _baseName[MaterialId.BaseDe(matId)];
                return string.IsNullOrEmpty(baseName) ? "???" : baseName;
            }
            return _playerName[matId] ?? "???";
        }

        /// <summary>
        /// Nombre "de taller" en español de los materiales MUNDANOS -- los que
        /// salen de los grifos básicos o son fenómenos que cualquiera reconoce
        /// (ver la tabla de clasificación completa en el doc-comment de la
        /// clase). El Maestro ya los tiene catalogados, así que mostrarlos no
        /// rompe la fantasía de descubrir.
        ///
        /// Devuelve null para TODO lo innominado (Slime, Azoth, CrystalSeed,
        /// Crystal, Vivium, Acid): esas hay que descubrirlas y bautizarlas --
        /// es el contrato que usa el resto del HUD (frasco, redomas, diario,
        /// tolva, encargos) para decidir cuándo enseñar "???".
        ///
        /// (fix playtest 10) Azoth y CrystalSeed SALÍAN de aquí antes ("el
        /// Maestro os las entrega en mano, así que ya las conocéis"): el
        /// reporte que motiva este cambio señaló justo el problema de fondo
        /// -- si todo llega bautizado, bautizar es un trámite vacío. El
        /// Maestro las entrega precisamente PORQUE tampoco sabe qué son (ver
        /// MasterSupplies.TextoEntrega); aquí vuelven a ser innominadas.
        ///
        /// Existe para que la UI NUNCA enseñe los devName internos en inglés
        /// ("Water", "Nutrient"), que era lo que hacían el HUD del frasco y los
        /// rótulos de los grifos.
        /// </summary>
        public static string NombreComun(byte matId)
        {
            switch (matId)
            {
                case MaterialId.Stone: return "piedra";
                case MaterialId.Sand: return "arena";
                case MaterialId.Water: return "agua";
                case MaterialId.Oil: return "aceite";
                case MaterialId.Nutrient: return "nutriente";
                case MaterialId.Steam: return "vapor";
                case MaterialId.Smoke: return "humo";
                case MaterialId.Fire: return "fuego";
                case MaterialId.Ash: return "ceniza";
                case MaterialId.Ice: return "hielo";
                // (playtest 25, CONTRATO_PERSISTE.md §6.4) EL LIMO ES
                // VOCABULARIO: "el Maestro lo conoce" -- misma excepción
                // documentada que la firma visual fija del limo en
                // Sim/Universe.cs (regla 17 de CLAUDE.md, "solo varía lo
                // innominado"). Las 40 variantes base×estado (18..57) NO
                // pasan por aquí: caen al default (null) a propósito, son
                // "lo innominado" nuevo -- ver NombreDe/NombreParaHud/
                // Bautizar para cómo se nombran (por BASE, no por matId).
                case MaterialId.Limo: return "limo primordial"; // (playtest 28) "Limo Primordial" a pedido de Cesar, para la prueba con sus amigos de Steam.
                default: return null; // lo innominado: Slime, Azoth, CrystalSeed, Vivium, Crystal, Acid, y las 40 variantes base×estado.
            }
        }

        /// <summary>
        /// Sufijo fijo de estado (contrato §6.4: "(fundido)", "(cerámico)"...)
        /// -- SOLO cosmético, nunca se guarda en <see cref="_baseName"/> ni en
        /// el campo que precarga NamingUi (ver NombreDe). Polvo es el estado
        /// NATAL de la base: sin sufijo, es literalmente "la arena", no "la
        /// arena (en polvo)".
        /// </summary>
        private static string SufijoEstado(EstadoMateria e)
        {
            switch (e)
            {
                case EstadoMateria.Fundido: return " (fundido)";
                case EstadoMateria.Templado: return " (templado)";
                case EstadoMateria.Recocido: return " (recocido)";
                case EstadoMateria.Compacto: return " (compacto)";
                case EstadoMateria.Ceramico: return " (cerámico)";
                case EstadoMateria.Calcinado: return " (calcinado)";
                case EstadoMateria.Solucion: return " (disuelto)";
                default: return ""; // Polvo: estado natal, sin sufijo.
            }
        }

        /// <summary>
        /// Nombre para los HUD: el que le puso el jugador &gt; el común de
        /// taller &gt; "???". (playtest 25) Para una base×estado, el nombre
        /// de la BASE + el sufijo fijo de su estado (ver SufijoEstado) --
        /// "una base = un nombre, no ocho" (contrato §6.4), pero el sufijo sí
        /// distingue "arena" de "arena (fundido)" en cualquier rótulo del
        /// juego (Tolva, encargos, diario, Ensayo).
        /// </summary>
        public string NombreParaHud(byte matId)
        {
            if (matId >= MaterialId.Count) return "???";
            if (MaterialId.EsBaseEstado(matId))
            {
                string baseName = _baseName[MaterialId.BaseDe(matId)];
                if (string.IsNullOrEmpty(baseName)) return "???";
                return baseName + SufijoEstado(MaterialId.EstadoDe(matId));
            }
            string propio = _playerName[matId];
            if (!string.IsNullOrEmpty(propio)) return propio;
            return NombreComun(matId) ?? "???";
        }

        /// <summary>
        /// True si `matId` es de "lo innominado" (ver tabla de clasificación
        /// en el doc-comment de la clase) Y todavía nadie le ha puesto
        /// nombre -- es decir, si de verdad hace falta invitar a bautizarlo
        /// (fix playtest 10, invitación "T para bautizarlo" de
        /// <see cref="ActualizarAvisoBautizo"/>). Falso para el vocabulario
        /// del taller (nunca hace falta bautizar el agua) y falso en cuanto
        /// ya tiene nombre propio.
        /// </summary>
        private bool NecesitaBautizo(byte matId)
        {
            if (matId == MaterialId.Empty || matId >= MaterialId.Count) return false;
            if (NombreComun(matId) != null) return false;
            // (playtest 25) Una base×estado necesita bautizo si su BASE
            // sigue sin nombre -- consultar _playerName[matId] aquí (como
            // antes de esta ronda) siempre daría "vacío" para estos ids,
            // porque nunca se escribe en _playerName para ellos (ver
            // Bautizar): el aviso "T para bautizarlo" se habría quedado
            // pegado para siempre incluso tras nombrar la base.
            if (MaterialId.EsBaseEstado(matId)) return string.IsNullOrEmpty(_baseName[MaterialId.BaseDe(matId)]);
            return string.IsNullOrEmpty(_playerName[matId]);
        }

        /// <summary>
        /// Pone/quita el nombre de un material. Nombre vacío o solo espacios
        /// equivale a "olvidarlo" (vuelve a mostrar "???"). (playtest 25)
        /// Para una base×estado, bautiza la BASE entera (contrato §6.4: "una
        /// base = un nombre, no ocho") -- da igual en qué estado concreto
        /// apuntaba el jugador al pulsar T, el nombre se aplica a los 8.
        /// </summary>
        public void Bautizar(byte matId, string nombre)
        {
            if (matId >= MaterialId.Count) return;
            string limpio = string.IsNullOrWhiteSpace(nombre) ? null : nombre.Trim();

            if (MaterialId.EsBaseEstado(matId))
            {
                _baseName[MaterialId.BaseDe(matId)] = limpio;
            }
            else
            {
                _playerName[matId] = limpio;
            }
            _discovered[matId] = true; // bautizar implica conocerlo.
            NamingVersion++; // ver doc de la clase: JournalHud detecta re-bautizos con esto.
        }

        /// <summary>
        /// (playtest 25, CONTRATO_PERSISTE.md §5.3/§6.4) Anota en la ficha
        /// del material una línea de observación PRESENCIADA -- lo llama el
        /// BancoChispa (B, encendió/no la lámpara) y EnsayoMaestro (C, cómo
        /// sobrevivió o murió una muestra). "Una vez por línea distinta":
        /// repetir el mismo ensayo/análisis sobre el mismo material no
        /// duplica la observación en la ficha, mismo espíritu que
        /// JournalHud.BuildChips con las WitnessFlags.
        /// </summary>
        public void RegistrarObservacionPropiedad(byte matId, string observacion)
        {
            if (matId >= MaterialId.Count || string.IsNullOrEmpty(observacion)) return;

            string previas = _observaciones[matId];
            if (!string.IsNullOrEmpty(previas))
            {
                // (barato) split manual sin LINQ/regex: el hot path de esta
                // llamada es "una vez por análisis", no por frame, pero el
                // proyecto evita asignaciones innecesarias por costumbre.
                if (previas == observacion) return;
                int idx = previas.IndexOf(observacion, System.StringComparison.Ordinal);
                if (idx >= 0)
                {
                    bool inicio = idx == 0 || previas[idx - 1] == ' ';
                    int fin = idx + observacion.Length;
                    bool final = fin == previas.Length || (fin + 2 < previas.Length && previas[fin] == ' ');
                    if (inicio && final) return; // ya estaba, como línea completa (no como subcadena de otra).
                }
                _observaciones[matId] = previas + " · " + observacion;
            }
            else
            {
                _observaciones[matId] = observacion;
            }

            MarcarDescubierto(matId); // (playtest 32) presenciar una propiedad implica conocer el material -- y, si es la primera vez, anuncia "ALGO NUEVO".
            // (playtest 25, FIX) Sube SOLO cuando el texto cambió de verdad
            // (los `return` de arriba ya cortaron los no-op) -- ver
            // ObservacionesVersion. Sin este contador, registrar una
            // observación sobre un material YA descubierto (el caso normal:
            // ensayar algo que ya conoces) no tocaba CountDiscovered ni
            // NamingVersion ni LeyesVersion, así que la firma de caché de
            // JournalHud no cambiaba y la observación podía quedarse
            // invisible en el diario hasta que otro evento cualquiera
            // invalidara la caché por casualidad -- el mismo hueco que la
            // regla 48 de CLAUDE.md pide cerrar (propiedad nueva sin
            // consumidor de verdad).
            ObservacionesVersion++;
        }

        /// <summary>Observaciones de propiedad acumuladas (RegistrarObservacionPropiedad) para la ficha de SUSTANCIAS del diario. Cadena vacía si no hay ninguna.</summary>
        public string ObservacionesDe(byte matId) => matId < MaterialId.Count ? (_observaciones[matId] ?? "") : "";

        /// <summary>Sube cada vez que RegistrarObservacionPropiedad cambia de verdad el texto de un material -- mismo patrón que NamingVersion/LeyesVersion/Hornada.PatentesVersion, para que JournalHud sepa cuándo reconstruir la ficha de SUSTANCIAS.</summary>
        public int ObservacionesVersion { get; private set; }

        /// <summary>
        /// Nombre "de ley": como NombreParaHud, pero con una descripción de
        /// respaldo (nunca "???") para materiales aún sin bautizar -- las
        /// leyes del diario/el aviso de descubrimiento tienen que leerse
        /// aunque el jugador no haya puesto nombre todavía.
        ///
        /// (fix playtest 10) `respaldo` YA NO puede ser el nombre genérico
        /// interno ("azoth", "semilla de cristal"...): con la reclasificación
        /// de arriba esos materiales son innominados de verdad, así que
        /// escribir su identidad aquí sería la MISMA circularidad que se
        /// acaba de arreglar en las pistas (un texto dice "Vivium" y el HUD
        /// enseña "???" al lado). El llamante pasa una descripción de
        /// ORIGEN/efecto en su lugar (ver ConstruirTextoLey*): esta función
        /// solo decide EN QUÉ ORDEN de prioridad se usa (nombre propio &gt;
        /// nombre común de taller -- ninguno de los dos es circular -- &gt;
        /// descripción de respaldo).
        /// </summary>
        private string NombreLey(byte matId, string respaldo)
        {
            if (matId >= MaterialId.Count) return respaldo;
            string propio = _playerName[matId];
            if (!string.IsNullOrEmpty(propio)) return propio;
            return NombreComun(matId) ?? respaldo;
        }

        /// <summary>
        /// (playtest 18) Sobrecarga que resuelve el respaldo por tabla fija (ver
        /// RespaldoLey) en vez de que cada llamante lo escriba a mano. Antes solo dos
        /// llamantes (ConstruirTextoLeyCristal/Vivium) necesitaban respaldo, uno cada
        /// uno; con leyes sorteadas por seed CUALQUIERA de las 6 sustancias innominadas
        /// puede aparecer en cualquier ley (a, b, productoA o productoB), así que
        /// ConstruirTextoLey necesita poder pedir el nombre de cualquiera sin saber de
        /// antemano cuál va a tocarle.
        /// </summary>
        private string NombreLey(byte matId) => NombreLey(matId, RespaldoLey(matId));

        /// <summary>
        /// Descripción de respaldo por ORIGEN/EFECTO para cada sustancia innominada,
        /// nunca su identidad interna (regla 13/17 de CLAUDE.md) -- usada por
        /// NombreLey(byte) mientras el jugador no haya bautizado ni el material sea
        /// vocabulario de taller. Azoth/CrystalSeed/Vivium reutilizan LITERALMENTE los
        /// mismos textos que ya usan MasterSupplies.TextoEntrega y Game/HintSystem.cs
        /// ("el líquido reservado del Maestro", "su semilla sin nombre", "el retoño de
        /// la cuba") para que el vocabulario del jugador sea el mismo en cualquier
        /// pantalla que hable de esa sustancia. Crystal/Slime/Acid no tenían respaldo
        /// hasta ahora (solo dos leyes tenían banner); se añaden aquí siguiendo el mismo
        /// criterio de origen/efecto: Crystal por CÓMO nace (del frío), Slime por lo que
        /// lo produce (neutralizar ácido), Acid por lo que se le ha visto hacer (disolver).
        /// </summary>
        private static string RespaldoLey(byte matId)
        {
            switch (matId)
            {
                case MaterialId.Empty: return "nada";
                case MaterialId.Azoth: return "el líquido reservado del Maestro";
                case MaterialId.CrystalSeed: return "su semilla sin nombre";
                case MaterialId.Vivium: return "el retoño de la cuba";
                case MaterialId.Crystal: return "la piedra que nace del frío";
                case MaterialId.Slime: return "el poso que deja el ácido al apagarse";
                case MaterialId.Acid: return "lo que disuelve lo que toca";
                default: return NombreComun(matId) ?? "algo sin nombre todavía"; // vocabulario de taller (ya tiene nombre común) o id fuera de rango.
            }
        }

        public WitnessFlags WitnessOf(byte matId) => matId < MaterialId.Count ? _witness[matId] : WitnessFlags.None;

        public bool Vio(byte matId, WitnessFlags flag) => (WitnessOf(matId) & flag) != 0;

        public int CountDiscovered()
        {
            int n = 0;
            for (int m = 1; m < MaterialId.Count; m++) if (_discovered[m]) n++;
            return n;
        }

        public int CountNamed()
        {
            int n = 0;
            for (int m = 1; m < MaterialId.Count; m++) if (_playerName[m] != null) n++;
            return n;
        }

        private void Update()
        {
            if (_sim == null || _sim.Stepper == null) return;

            PollFlask();
            PollHover();
            ConsumeEvents();
            ActualizarPatentes();
            ActualizarBannerLey();
            if (!DayCycle.InputLocked) ActualizarAvisoBautizo();
        }

        /// <summary>
        /// (playtest 25, CONTRATO_PERSISTE.md §6.4) "Se ofrece PATENTAR
        /// (aviso en pantalla estilo 'LEY DESCUBIERTA')": encola un banner
        /// por cada patente CONGELADA nueva desde el último frame (barato:
        /// una comparación de enteros casi siempre). Solo el ANUNCIO vive
        /// aquí -- el bautizo de verdad (elegir nombre) pasa por el libro,
        /// sección PROCEDIMIENTOS (ver JournalHud.cs): NamingUi.cs no se
        /// toca en este encargo y su API está atada a un MaterialId real de
        /// la sim, no a un índice de patente, así que no puede reutilizarse
        /// tal cual para esto (DECISIÓN documentada en el resumen del
        /// encargo).
        /// </summary>
        private void ActualizarPatentes()
        {
            int actual = Hornada.PatenteCount;
            if (actual <= _ultimoPatenteCountVisto) return;

            for (int i = _ultimoPatenteCountVisto; i < actual; i++)
            {
                // Nunca se nombra la sustancia (regla 13/17 de CLAUDE.md: no
                // reventar la circularidad de "???" con un texto que la
                // describe igual): el aviso apunta al LIBRO, no al material.
                EncolarLeyBanner(
                    "Has producido algo que nunca habías fijado así. Paténtalo en tu libro (J), sección PROCEDIMIENTOS.",
                    MaterialId.Empty,
                    "¡NUEVO PROCEDIMIENTO!");
            }
            _ultimoPatenteCountVisto = actual;
        }

        /// <summary>
        /// (fix playtest 10) Si lo que el jugador tiene apuntado o cargado en el frasco
        /// ahora mismo (mismo criterio que <see cref="NamingUi.ResolveTarget"/>: cursor
        /// primero, frasco de respaldo -- así el aviso siempre coincide con lo que T
        /// abriría) es innominado y sin bautizar, y es la PRIMERA vez que esta sustancia
        /// concreta dispara el aviso, lo enciende. Nunca construye el texto aquí (es
        /// literal, ver DrawAvisoBautizo): solo decide CUÁNDO mostrarlo.
        /// </summary>
        private void ActualizarAvisoBautizo()
        {
            byte objetivo = NamingUi.ResolveTarget(_sim, _flask);
            if (objetivo == MaterialId.Empty) return;
            if (!NecesitaBautizo(objetivo)) return;
            if (_avisoBautizoMostrado[objetivo]) return;

            _avisoBautizoMostrado[objetivo] = true;
            _avisoBautizoHasta = Time.time + AvisoBautizoDuracionSeg;
        }

        /// <summary>
        /// (fix playtest 10) "Descubrir la ley y nombrar a su producto deberían
        /// encadenarse de forma natural": se llama justo cuando el banner "LEY
        /// DESCUBIERTA" de <paramref name="matId"/> termina de mostrarse. Si ese
        /// material sigue innominado, encadena la invitación a bautizar (respetando el
        /// mismo "una vez por material" que el resto del sistema -- si el jugador ya lo
        /// vio por otra vía, p.ej. lo apuntó mientras esperaba el banner, no se repite).
        /// </summary>
        private void DispararAvisoBautizoTrasLey(byte matId)
        {
            if (!NecesitaBautizo(matId)) return;
            if (_avisoBautizoMostrado[matId]) return;

            _avisoBautizoMostrado[matId] = true;
            _avisoBautizoHasta = Time.time + AvisoBautizoDuracionSeg;
        }

        private void PollFlask()
        {
            if (_flask == null) return;

            _flaskPollTimer += Time.deltaTime;
            if (_flaskPollTimer < FlaskPollInterval) return;
            _flaskPollTimer = 0f;

            for (int m = 1; m < MaterialId.Count; m++)
            {
                if (_discovered[m]) continue;
                if (_flask.GetCount((byte)m) > 0) MarcarDescubierto((byte)m);
            }
        }

        private void PollHover()
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null || _sim == null)
            {
                _hoverTimer = 0f;
                return;
            }

            Vector2 screenPos = mouse.position.ReadValue();
            var ray = cam.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            var groundPlane = new Plane(Vector3.forward, Vector3.zero);
            if (!groundPlane.Raycast(ray, out float enter))
            {
                _hoverTimer = 0f;
                return;
            }

            Vector3 world = ray.GetPoint(enter);
            Vector2Int cell = _sim.WorldToCell(world);
            if (!CellGrid.InBounds(cell.x, cell.y))
            {
                _hoverTimer = 0f;
                return;
            }

            byte matId = (byte)_sim.SampleMaterial(cell.x, cell.y);
            if (matId == MaterialId.Empty || matId == MaterialId.Stone)
            {
                _hoverTimer = 0f;
                return;
            }

            if (matId != _hoverMatId)
            {
                _hoverMatId = matId;
                _hoverTimer = 0f;
            }

            _hoverTimer += Time.deltaTime;
            if (_hoverTimer >= HoverDiscoverSeconds)
            {
                MarcarDescubierto(matId); // (playtest 32) antes ponía el flag directo cada frame de hover -- MarcarDescubierto ya filtra la transición, así que esto deja de reevaluar NecesitaBautizo en cada frame que sigas mirando.
            }
        }

        private void ConsumeEvents()
        {
            var stepper = _sim.Stepper;
            var events = stepper.Events;
            int head = stepper.EventHead;

            int i = _lastEventHead;
            int steps = 0;
            while (i != head && steps < SimStepper.EventBufferSize)
            {
                var e = events[i];
                // (playtest 18) SimEventType.Ley es un evento ADICIONAL (contrato sección
                // 4): los eventos de siempre (Ignite/Boil/.../Dissolve) se siguen
                // consumiendo exactamente igual en ApplyWitness, Ley se despacha aparte
                // porque lleva su propio dato (leyIndice) y no una WitnessFlag.
                if (e.type == SimEventType.Ley) ApplyLey(e.leyIndice);
                else ApplyWitness(e.type, e.matId);
                i = (i + 1) & (SimStepper.EventBufferSize - 1);
                steps++;
            }
            _lastEventHead = head;
        }

        private void ApplyWitness(SimEventType type, byte matId)
        {
            if (matId >= MaterialId.Count) return;

            WitnessFlags flag;
            switch (type)
            {
                case SimEventType.Ignite: flag = WitnessFlags.Arder; break;
                case SimEventType.Boil: flag = WitnessFlags.Hervir; break;
                case SimEventType.Freeze: flag = WitnessFlags.Congelarse; break;
                case SimEventType.Crystallize: flag = WitnessFlags.Cristalizar; break;
                case SimEventType.Grow: flag = WitnessFlags.Crecer; break;
                case SimEventType.Dissolve: flag = WitnessFlags.Disolverse; break;
                default: return;
            }

            _witness[matId] |= flag;
        }

        /// <summary>
        /// (playtest 18) Disparo de "LEY DESCUBIERTA": solo la PRIMERA vez que el índice
        /// de esta ley concreta llega por el evento SimEventType.Ley (empujado desde
        /// Sim/SimStepper.cs, ya limitado de ritmo allí -- ver CONTRATO_FASE3.md sección
        /// 7, esta clase no necesita su propio limitador). `leyIndice` llega como short
        /// desde SimNotableEvent; se defiende el rango ANTES de indexar -- un evento con
        /// un índice fuera de rango (que no debería poder ocurrir con un Universe.Leyes
        /// bien construido, pero el contrato lo exige por si acaso) se ignora sin más,
        /// nunca tira la partida.
        /// </summary>
        private void ApplyLey(int leyIndice)
        {
            if (_leyDescubierta == null) return;
            if (leyIndice < 0 || leyIndice >= _leyDescubierta.Length) return; // índice inválido: se ignora, ver doc de arriba.
            if (_leyDescubierta[leyIndice]) return; // ya presenciada -- solo cuenta la primera vez.

            _leyDescubierta[leyIndice] = true;
            LeyesVersion++;

            var ley = _sim.Universe.Leyes[leyIndice];
            EncolarLeyBanner(ConstruirTextoLey(ley), MaterialParaInvitarBautizo(ley), "LEY DESCUBIERTA");
        }

        /// <summary>(playtest 25) `titulo` nuevo, con valor por defecto para no tocar la llamada de ApplyLey: ver doc de <see cref="_leyBannerColaTitulo"/> (misma cola sirve a los dos avisos "estilo LEY DESCUBIERTA" del juego).</summary>
        private void EncolarLeyBanner(string texto, byte matId, string titulo = "LEY DESCUBIERTA")
        {
            // (playtest 18) Ya NO es defensivo-imposible: con 13-16 leyes por universo un
            // vertido puede disparar varias reacciones casi a la vez y llenar la cola de
            // verdad. Si pasa, el banner de esa ley se pierde (nadie ve el aviso), pero el
            // registro de "presenciada" YA se hizo en ApplyLey antes de llamar aquí -- son
            // dos cosas distintas a propósito: perder un aviso visual es aceptable, perder
            // el registro de descubrimiento no lo sería (el diario mentiría para siempre
            // sobre si el jugador vio esa ley).
            if (_leyBannerColaCount >= LeyBannerCapacidad) return;
            _leyBannerCola[_leyBannerColaCount] = texto;
            _leyBannerColaMat[_leyBannerColaCount] = matId;
            _leyBannerColaTitulo[_leyBannerColaCount] = titulo;
            _leyBannerColaCount++;
        }

        /// <summary>
        /// (playtest 18) Qué material invitar a bautizar cuando termine el banner de esta
        /// ley (ver DispararAvisoBautizoTrasLey): el producto que CAMBIA respecto a su
        /// reactivo es "lo nuevo que acabas de ver aparecer", el candidato natural a
        /// nombrar -- antes esto era MaterialId.Crystal/MaterialId.Vivium a mano porque
        /// solo había dos leyes con banner; ahora cualquier ley puede dispararlo. Caso
        /// especial: Crecimiento (única ley con esa forma, la del Vivium) no tiene un
        /// "producto nuevo" en el sentido de la fórmula -- productoA==a (la célula sigue
        /// siendo la misma) y productoB es Nutrient consumiéndose a Empty, que no es nada
        /// que invitar a nombrar -- así que se invita sobre el propio organismo (a).
        /// </summary>
        private static byte MaterialParaInvitarBautizo(LeyDelUniverso ley)
        {
            if (ley.forma == FormaDeLey.Crecimiento) return ley.a;
            if (ley.productoA != ley.a) return ley.productoA;
            if (ley.productoB != ley.b) return ley.productoB;
            return ley.a; // caso degenerado (no debería darse con el sorteo del contrato: R8 exige que al menos un lado cambie).
        }

        /// <summary>
        /// (playtest 18) Construido UNA vez, al disparar el evento (ver ApplyLey) --
        /// nunca en Update/OnGUI. Sustituye a los ConstruirTextoLeyCristal/Vivium de
        /// antes: con leyes sorteadas por seed no hay dos textos que escribir a mano, hay
        /// que GENERAR el texto desde el descriptor `LeyDelUniverso`, una plantilla por
        /// `FormaDeLey` (CONTRATO_FASE3.md sección 1) para que la FORMA de la ley se note
        /// -- un catalizador que no se gasta se lee muy distinto de una fusión o de un
        /// contagio que se propaga, y esa diferencia es la que hace que una semilla se
        /// SIENTA distinta de otra, no solo se vea distinta. Los nombres pasan siempre
        /// por NombreLey(byte) (bautizado &gt; vocabulario de taller &gt; respaldo de
        /// origen/efecto): mientras un material siga innominado nunca se revela su
        /// identidad interna (regla 13/17 de CLAUDE.md).
        /// </summary>
        private string ConstruirTextoLey(LeyDelUniverso ley)
        {
            string a = NombreLey(ley.a);
            string A = MayusculaInicial(a);
            string b = NombreLey(ley.b);
            string B = MayusculaInicial(b);
            string pa = NombreLey(ley.productoA);
            string pb = NombreLey(ley.productoB);

            // La condición térmica tiene que quedar dicha para que el jugador sepa
            // REPRODUCIR lo que acaba de ver, no solo lo que acaba de pasar (contrato
            // sección 5, punto 3: "al frío"/"con calor" cuando no sea Cualquiera).
            string cond = ley.condicion == CondicionTermica.Frio ? " en frío"
                : ley.condicion == CondicionTermica.Calor ? " con calor"
                : "";

            string cuerpo;
            switch (ley.forma)
            {
                case FormaDeLey.Transmutacion:
                    // A+B -> C+B: B es catalizador, no se gasta -- la propiedad que más
                    // cambia cómo se juega una semilla (siembra una vez y ya).
                    cuerpo = $"{A}, al tocar {b}{cond}, se vuelve {pa}. {B} no se gasta: sirve de semilla una y otra vez.";
                    break;

                case FormaDeLey.Fusion:
                    // A+B -> C+C: los dos reactivos se funden en la misma cosa nueva.
                    cuerpo = $"{A} y {b}, al tocarse{cond}, se funden los dos en {pa}.";
                    break;

                case FormaDeLey.Consumo:
                    // A+B -> Empty+C: A desaparece del todo, B se transforma.
                    cuerpo = $"{A}, al tocar {b}{cond}, desaparece por completo — y {b} se transforma en {pb}.";
                    break;

                case FormaDeLey.Liberacion:
                    // A+B -> C+gas: la ley más fácil de presenciar de lejos, algo sube.
                    cuerpo = $"{A}, al tocar {b}{cond}, se vuelve {pa} y suelta {pb}, que sube por el aire.";
                    break;

                case FormaDeLey.Contagio:
                    // A+B -> A+A: A se propaga comiéndose a B. Como mucho una por semilla
                    // (R5 del contrato) y SIEMPRE con condición térmica -- nunca Cualquiera.
                    cuerpo = $"{A} se propaga{cond}: en cuanto toca {b}, {b} se convierte también en {a}. Basta un punto de contacto.";
                    break;

                case FormaDeLey.Crecimiento:
                    // Caso especial (vive en Sim/SimStepper.GrowthTick, no es reacción de
                    // contacto): la única ley con esta forma es la del Vivium, garantizada
                    // en TODA semilla (esDelNucleo). Se mantiene el texto de siempre
                    // ("templado", no "frío"/"calor" genérico): la banda real es propia de
                    // la seed y no encaja en la etiqueta binaria de CondicionTermica.
                    cuerpo = $"{A}, asentado y con {b} al lado, a temperatura templada, crea más de sí mismo. No se consume: cada célula que nace es otra semilla.";
                    break;

                default:
                    // Defensivo: FormaDeLey es un enum cerrado con 6 valores, todos
                    // cubiertos arriba -- esta rama no debería alcanzarse nunca, pero un
                    // texto genérico es mejor que una excepción si el enum creciera en el
                    // futuro sin actualizar este switch.
                    cuerpo = $"{A} y {b} reaccionan{cond}.";
                    break;
            }

            return "LEY DESCUBIERTA — " + cuerpo;
        }

        private static string MayusculaInicial(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>Avanza la cola FIFO de leyes pendientes cuando la actual caduca o no hay ninguna mostrándose todavía.</summary>
        private void ActualizarBannerLey()
        {
            if (_leyBannerActual != null)
            {
                // (playtest 32, encargo C) "ALGO NUEVO" desaparece EN CUANTO
                // el jugador bautiza esa sustancia, aunque no hayan pasado
                // los ~6s de LeyBannerDuracionSeg -- ya cumplió su propósito
                // ("hasta que bautice" del encargo). Solo aplica a este
                // título: "LEY DESCUBIERTA"/"¡NUEVO PROCEDIMIENTO!" no
                // dependen de que nada se nombre, se quedan su duración
                // completa como siempre.
                bool yaBautizado = _leyBannerActualTitulo == TituloDescubrimiento
                    && _leyBannerActualMat != MaterialId.Empty && !NecesitaBautizo(_leyBannerActualMat);
                if (!yaBautizado && Time.time < _leyBannerHasta) return;
                // (fix playtest 10) El banner que acaba de terminar YA enseñó la ley:
                // este es el momento justo para encadenar "¿cómo lo llamáis?" -- ver
                // doc de DispararAvisoBautizoTrasLey.
                DispararAvisoBautizoTrasLey(_leyBannerActualMat);
                _leyBannerActual = null;
            }
            if (_leyBannerColaLeidos >= _leyBannerColaCount) return;

            _leyBannerActual = _leyBannerCola[_leyBannerColaLeidos];
            _leyBannerActualMat = _leyBannerColaMat[_leyBannerColaLeidos];
            _leyBannerActualTitulo = _leyBannerColaTitulo[_leyBannerColaLeidos];
            _leyBannerColaLeidos++;
            _leyBannerHasta = Time.time + LeyBannerDuracionSeg;
        }

        private void OnGUI()
        {
            if (DayCycle.InputLocked || DayCycle.HudSilenciado) return; // (playtest 21) HudSilenciado, hermano de InputLocked.

            // (playtest 32, encargo C) "ALGO NUEVO" respeta EscribiendoTexto
            // (el resto de banners de esta cola no lo hacía, y no hay motivo
            // para cambiarles el comportamiento establecido): invita a pulsar
            // T, así que mostrarlo justo cuando un campo de texto YA se comió
            // esa tecla es lo único que no tiene sentido. No se pierde -- solo
            // no se DIBUJA mientras se escribe; reaparece en cuanto el campo
            // se cierra, hasta que caduque o el jugador bautice.
            bool ocultarPorTexto = _leyBannerActualTitulo == TituloDescubrimiento && UiStyles.EscribiendoTexto;
            bool hayLey = _leyBannerActual != null && !ocultarPorTexto;
            // El aviso de bautizo nunca compite por atención con el banner de ley (que
            // ya cubre el mismo centro-superior de pantalla) ni con el campo de texto
            // abierto -- reaparecerá solo (DispararAvisoBautizoTrasLey) en cuanto el
            // banner de ley termine, si sigue haciendo falta.
            bool hayAviso = !hayLey && !UiStyles.EscribiendoTexto && Time.time < _avisoBautizoHasta;
            if (!hayLey && !hayAviso) return;

            UiStyles.Preparar();
            if (hayLey) DrawLeyBanner();
            if (hayAviso) DrawAvisoBautizo();
        }

        /// <summary>
        /// Aviso cálido y destacado (estilo UiStyles, sin HUD permanente): título en el
        /// color de aviso del taller + cuerpo centrado con la ley en frase ejecutable.
        /// El texto ya viene cacheado (ConstruirTextoLey): aquí solo se mide/dibuja, igual
        /// que hace Game/HintSystem.cs con su pista activa.
        /// </summary>
        // (playtest 32, encargo C) "Cinzel para el titular si UiStyles expone las
        // fuentes": SÍ las expone (UiStyles.FuenteTitulos, público desde el playtest
        // 31) pero solo para "ALGO NUEVO" -- "LEY DESCUBIERTA"/"¡NUEVO PROCEDIMIENTO!"
        // ya llevan su Alerta de siempre (Alegreya) y no hay motivo para tocarles el
        // aspecto establecido. Copia de UiStyles.Alerta con la fuente cambiada,
        // reconstruida SOLO cuando cambia Screen.height -- mismo criterio de caché que
        // UiStyles.Preparar (nunca un GUIStyle nuevo por frame).
        private static GUIStyle _tituloDescubrimientoEstilo;
        private static int _tituloDescubrimientoAltura = -1;

        private static GUIStyle EstiloTituloDescubrimiento()
        {
            if (_tituloDescubrimientoEstilo != null && _tituloDescubrimientoAltura == Screen.height)
                return _tituloDescubrimientoEstilo;
            _tituloDescubrimientoAltura = Screen.height;
            _tituloDescubrimientoEstilo = UiStyles.Alerta != null ? new GUIStyle(UiStyles.Alerta) : new GUIStyle();
            if (UiStyles.FuenteTitulos != null) _tituloDescubrimientoEstilo.font = UiStyles.FuenteTitulos;
            return _tituloDescubrimientoEstilo;
        }

        private void DrawLeyBanner()
        {
            // (playtest 25) Antes literal "LEY DESCUBIERTA": ahora la misma
            // cola/panel sirve también al aviso de patente (ver
            // ActualizarPatentes), así que el título viaja con cada entrada
            // de la cola en vez de estar fijo.
            string titulo = _leyBannerActualTitulo ?? "LEY DESCUBIERTA";
            string texto = _leyBannerActual;
            GUIStyle estiloTitulo = titulo == TituloDescubrimiento ? EstiloTituloDescubrimiento() : UiStyles.Alerta;

            float pad = UiStyles.S(14f);
            float acento = UiStyles.S(4f);
            float ancho = Mathf.Clamp(Screen.width - UiStyles.S(160f), UiStyles.S(360f), UiStyles.S(640f));
            float interior = ancho - pad * 2f - acento;

            float altoTitulo = UiStyles.Alto(estiloTitulo, titulo, interior);
            float altoCuerpo = UiStyles.Alto(UiStyles.CuerpoCentrado, texto, interior);
            float alto = pad + altoTitulo + UiStyles.S(4f) + altoCuerpo + pad;

            // Zona propia del centro-superior de pantalla: por debajo del reloj/HintSystem
            // (que vive pegado a S(54f)) y por encima del área de juego habitual, para no
            // pisar ni el panel de pistas ni los encargos/frasco de los laterales.
            var panel = new Rect((Screen.width - ancho) * 0.5f, Screen.height * 0.30f, ancho, alto);
            UiStyles.Panel(panel, UiStyles.TintaFuerte, UiStyles.Oro);
            UiStyles.Rellenar(new Rect(panel.x, panel.y, acento, panel.height), UiStyles.Oro);

            GUI.Label(new Rect(panel.x + acento + pad, panel.y + pad, interior, altoTitulo), titulo, estiloTitulo);
            GUI.Label(new Rect(panel.x + acento + pad, panel.yMax - pad - altoCuerpo, interior, altoCuerpo), texto, UiStyles.CuerpoCentrado);
        }

        /// <summary>Texto de la invitación: literal, no se construye nunca por frame (regla del proyecto: nada de strings en OnGUI).</summary>
        private const string TextoAvisoBautizo = "esto no tiene nombre — T para bautizarlo";

        /// <summary>
        /// (fix playtest 10) Globo breve y discreto junto al cursor, mismo widget que ya
        /// usa FlaskHud para su feedback ("frasco vacío", "demasiado lejos") -- ningún
        /// HUD nuevo, solo otro uso del mismo <see cref="UiStyles.Globo"/>.
        /// </summary>
        private void DrawAvisoBautizo()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            var gui = new Vector2(screenPos.x, Screen.height - screenPos.y - UiStyles.S(34f));
            UiStyles.Globo(gui, TextoAvisoBautizo, UiStyles.Oro);
        }

        /// <summary>Chip corto en español para una única flag de presenciado (usado por JournalHud).</summary>
        public static string ChipLabel(WitnessFlags flag)
        {
            switch (flag)
            {
                case WitnessFlags.Arder: return "arde";
                case WitnessFlags.Cristalizar: return "cristaliza";
                case WitnessFlags.Crecer: return "crece";
                case WitnessFlags.Disolverse: return "se disuelve";
                case WitnessFlags.Hervir: return "hierve";
                case WitnessFlags.Congelarse: return "se congela";
                default: return "";
            }
        }
    }
}
