using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Net;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// EL CRISOL — reconstruido entero en el PLAYTEST 27
    /// (docs/CONTRATO_TALLER_GRANDE.md, mandatos 1-4).
    ///
    /// =====================================================================
    /// POR QUÉ SE REHIZO: EL VEREDICTO DE CESAR SOBRE EL PLAYTEST 26
    /// =====================================================================
    /// Textual: *"no sé por qué dice 'cargadme combustible'; no entiendo por
    /// qué hay una N brillando -- me hace pensar que ya tiene combustible y
    /// que algo está prendido; mucho menos sé dónde poner el combustible;
    /// peor aún, AHÍ NO CABE NADA; y más grave: yo al inicio NO TENGO
    /// combustible... sin embargo ése es el mensaje más persistente"*. Y
    /// sobre el interior: *"seca, tuesta y no sé qué más, pero todo lo hace
    /// RÁPIDO, y cada vez que le tiro limo saco 4 cosas de colores que me
    /// aturden"*.
    ///
    /// Cinco fallos distintos, cinco respuestas:
    ///  1. AHÍ NO CABE NADA -> la cámara pasa de 7x5=35 celdas a
    ///     <see cref="CamaraAncho"/>x<see cref="CamaraAlto"/> = 13x9 = **117
    ///     celdas**: una ración entera de caño (45) cabe con holgura.
    ///  2. NO SÉ DÓNDE PONER EL COMBUSTIBLE -> el brasero deja de ser una
    ///     cubetita pegada al crisol y pasa a ser un CESTO DE HIERRO APARTE,
    ///     6 celdas a la derecha, chato y ancho donde el crisol es alto y
    ///     panzudo (<see cref="MaquinariaSprites.CestoBrasero"/>): dos bocas
    ///     que no se parecen en nada.
    ///  3. "CARGADME COMBUSTIBLE" DE ENTRADA -> el rótulo en reposo ya no
    ///     pide nada: dice lo que el aparato ES ("fuego bajo · vierte y
    ///     prueba"). El crisol arranca con su rescoldo propio
    ///     (<see cref="Universe.CrisolTier0Raw"/>) y el brasero arranca
    ///     FRÍO Y VACÍO, visualmente apagado.
    ///  4. LA N BRILLANDO -> era el pulso de proximidad del playtest 26. Se
    ///     apagó (`AffordanceGlow.ProximidadActiva=false`) y el mismo
    ///     mecanismo pasa a significar lo que todo el mundo lee en un pulso:
    ///     ESTOY TRABAJANDO (<see cref="MaquinariaSprites.AffordanceGlow.AlfaTrabajo"/>,
    ///     encendido solo mientras corre una hornada).
    ///  5. TODO RÁPIDO Y CUATRO COSAS DE GOLPE -> ver el bloque siguiente.
    ///
    /// =====================================================================
    /// EL CAMBIO DE CAUSALIDAD: **HORNADAS** (mandato 4, diseño cerrado)
    /// =====================================================================
    /// El crisol del 25/26 era un CAMPO: mantenía la cubeta caliente todo el
    /// rato y sondeaba transformaciones cada 0.8s. Consecuencia inevitable:
    /// cascada (el limo se separaba, el polvo resultante se calcinaba, el
    /// calcinado seguía...) y ninguna de las tres cosas se veía ocurrir.
    ///
    /// Desde el playtest 27 el crisol es un HORNO POR HORNADAS:
    ///  · En REPOSO **no empuja temperatura ninguna**. Eso es lo que hace
    ///    estructuralmente imposible la cascada: sin batch no hay calor, y
    ///    sin calor no hay una segunda transformación. (Es también la razón
    ///    de que el rótulo en reposo no pida nada: no está esperando leña,
    ///    está esperando una orden.)
    ///  · **E enciende UNA hornada.** Se decide EN EL MOMENTO DEL ENCENDIDO
    ///    qué transformación va a ocurrir (material dominante de la cámara x
    ///    temperatura disponible, ver <see cref="DecidirHornada"/>) y ya no
    ///    cambia: una pasada, una transformación, siempre.
    ///  · La hornada corre <see cref="HornadaSegundos"/> segundos a ritmo
    ///    VISIBLE: el rescoldo sube, las burbujas suben, el cesto ruge y la
    ///    silueta entera late. Nada ocurre "de golpe".
    ///  · Al acabar, el crisol **REPOSA CON EL RESULTADO DENTRO**, y lo
    ///    MANTIENE a una temperatura en la que ese resultado es estable
    ///    (<see cref="TempReposoPara"/>) hasta que el jugador lo recoge. Ese
    ///    "recoger y volver a pasar" es EL gesto del juego (decisión de
    ///    Cesar), y por eso el resultado tiene que seguir ahí, intacto,
    ///    cuando vuelvas.
    ///
    /// LA CARRERA CONTRA `SimStepper.ApplyPhase`, RESUELTA SIN CARRERA. El
    /// crisol del 26 tenía un `RecocidoScan` que corría en CADA tick de
    /// física para ganarle por 4 raw al templado del mundo -- una carrera
    /// invisible que el jugador no podía ni ver ni entender (y justo el tipo
    /// de mecanismo que la regla 49 obliga a mirar con lupa). Ya no existe:
    ///  · Durante el 88% de la hornada el objetivo térmico se CLAMPEA por
    ///    debajo del umbral del mundo (<see cref="TechoSeguroPara"/>), así
    ///    que el mundo no puede transformar nada antes de tiempo.
    ///  · RECOCER es ahora una hornada explícita (metes Fundido, pulsas E):
    ///    el crisol lo sostiene JUSTO por encima de su punto de
    ///    solidificación durante toda la pasada y lo convierte él al final.
    ///  · TEMPLAR sigue siendo del mundo, y sigue siendo el contraste del
    ///    diseño: sacas el Fundido con el frasco y lo viertes FUERA, donde se
    ///    enfría de golpe. Enfriar dentro = Recocido; enfriar fuera =
    ///    Templado. Dos gestos distintos, los dos visibles.
    ///
    /// **UNA BASE POR HORNADA, ELEGIDA POR LA TEMPERATURA** (mandato 4). El
    /// limo ya no se separa en el mundo (Sim/SimStepper.cs retiró
    /// `ProcessLimoSeparacion` esta ronda). Lo separa el crisol, y saca UNA
    /// sola base: la MÁS ALTA cuya banda <see cref="Universe.ExtraccionRaw"/>
    /// quepa en la temperatura de esta hornada. Con el fuego bajo sale
    /// siempre la primera (su banda está por debajo de `CrisolTier0Raw` en
    /// toda seed, garantizado por el solver); las demás exigen combustibles
    /// mejores. Es LITERALMENTE la intuición que Cesar formuló solo -- *"pensé
    /// que estaría en relación al nivel de combustible, siendo que algunos
    /// llegan a temperaturas más altas"* -- convertida en la mecánica.
    ///
    /// EL COMBUSTIBLE SE CONSUME POR HORNADA, no por reloj. Una celda de
    /// combustible = una pasada. El playtest 26 quemaba una cada 6s aunque no
    /// estuvieras haciendo nada, lo que hacía imposible planificar.
    ///
    /// =====================================================================
    /// GEOMETRÍA (mandato 1 y 2)
    /// =====================================================================
    /// La mampostería la talla Sim/SimLevelBuilder.cs vía
    /// <see cref="TallarEnPlano"/> (regla 47, sin cambios desde el 26); las
    /// medidas viven aquí y son una sola fuente de verdad.
    ///
    ///        ╱‾‾‾‾‾‾‾‾‾ boca embudada, 11 filas ‾‾‾‾‾‾‾‾‾╲   <- y 190
    ///       ╱   (las paredes DE PIEDRA se abren 6 celdas   ╲
    ///      ╱     por lado: la geometría embuda de verdad)   ╲ <- y 180
    ///     │███  cámara 13x9 = 117 celdas  ███│      ╭─────╮
    ///     │███████████████████████████████████│     │cesto│  <- brasero
    ///     └───────────────────────────────────┘     ╰─────╯      y 171..176
    ///
    /// NADA DE EMBUDOS FLOTANTES (mandato 2, el error que "mató la
    /// gramática" en el 26): el embudo del crisol es MAMPOSTERÍA TALLADA con
    /// paredes diagonales -- lo que vierte de verdad -- y el sprite solo pone
    /// el LABIO de latón que lo remata
    /// (<see cref="MaquinariaSprites.LabioBoca"/>) más las guías de latón que
    /// forran la rampa. Las estaciones que reciben DEPOSITANDO (prensa,
    /// chispa, ensayo) no llevan embudo ninguno.
    ///
    /// =====================================================================
    /// CONVERSIÓN POR FRENTES (playtest 44, LA FÍSICA HONESTA,
    /// docs/CONTRATO_TERMICA.md §2a)
    /// =====================================================================
    /// Hasta esta ronda, la cámara entera convertía de golpe en
    /// <see cref="CerrarHornada"/> al agotarse <see cref="HornadaSegundos"/>:
    /// diez segundos de rescoldo/burbujas subiendo y ni una sola celda
    /// cambiando de material hasta el chasquido final. Ahora
    /// <see cref="ProcessConversionFrente"/> convierte celda a celda, TICK A
    /// TICK, con un umbral que depende de la fila de distancia al hogar --
    /// el "tostado" se ve subir desde el fondo del puchero hacia la boca a lo
    /// largo de toda la pasada (ver el docblock de ese método para el
    /// mecanismo completo). <see cref="CerrarHornada"/> se queda como
    /// GARANTÍA final (stragglers), no como el único punto de conversión, y
    /// el testigo forense (<see cref="Hornada.RegistrarOp"/>) sigue
    /// disparando EXACTAMENTE UNA vez, al cierre, con el total acumulado del
    /// frente entero -- el contrato de qué ve <see cref="Hornada"/> no
    /// cambia, solo cuándo (y cómo de gradual) llega la materia a ese total.
    /// </summary>
    public sealed class Crisol : MonoBehaviour, IMaquinaInteractiva, IMovibleAnclaEsquina, IMaquinaUsableRemota
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 3;
        /// <summary>Radio de foco. 3.2 -&gt; 4.0 (playtest 27): el aparato mide 41 celdas de ancho, así que tiene que responder también desde su brasero (a 22 celdas de la cámara) -- MachineFocus se queda con el MÁS CERCANO, y desde el brasero el Crisol sigue ganando a la Prensa por 2 celdas.</summary>
        private const float ProximityRange = 4.0f;

        // -----------------------------------------------------------------
        // GEOMETRÍA (playtest 27, mandato 1). Todas PÚBLICAS: las lee
        // Sim/SimLevelBuilder.cs para tallar el plano y para documentar las
        // huelgas entre estaciones.
        // -----------------------------------------------------------------
        /// <summary>Ancho del hueco interior de la cámara. 7 -&gt; 13.</summary>
        public const int CamaraAncho = 13;
        /// <summary>Alto del hueco interior de la cámara. 5 -&gt; 9. 13x9 = 117 celdas (la ración de un caño son 45).</summary>
        public const int CamaraAlto = 9;
        /// <summary>Grosor del muro de piedra. 1 -&gt; 2: un muro de una celda en un aparato de 40 de ancho se lee como una raya, no como obra.</summary>
        public const int MuroGrosor = 2;
        /// <summary>Filas de la BOCA EMBUDADA sobre la cámara.</summary>
        public const int BocaFilas = 11;
        /// <summary>Cuánto se abre cada pared de la boca, en celdas, de abajo a arriba. La boca acaba midiendo 13+2*6 = 25 celdas de luz.</summary>
        public const int BocaVuelo = 6;
        /// <summary>Ancho del hueco interior del brasero.</summary>
        public const int BraseroAncho = 5;
        /// <summary>Alto del hueco interior del brasero: CHATO a propósito (la cámara mide 9) -- las dos bocas no se parecen ni en silueta ni en altura.</summary>
        public const int BraseroAlto = 6;
        /// <summary>
        /// (segunda pasada) Celdas que el SPRITE del cuerpo sobresale de la
        /// mampostería por cada lado. El muro de piedra mide 2 celdas: a la
        /// escala de juego eso son ~16 px de chapa, y la panza vacía se leía
        /// como un ALAMBRE alrededor de un agujero. Con el vuelo, la pared
        /// visible del crisol pasa a 2+3 = 5 celdas (~40 px) y la panza puede
        /// ABOMBARSE fuera de la piedra, que es lo que la hace parecer un
        /// caldero. El sprite recorta su cámara con las medidas EXACTAS del
        /// hueco real, así que sigue sin tapar nada de lo que hay dentro.
        /// </summary>
        public const int VueloCuerpo = 3;
        /// <summary>Lo mismo para el cesto del brasero (menos vuelo: es una pieza más pequeña y no debe competir con la panza).</summary>
        public const int VueloCesto = 2;
        /// <summary>Filas del HOGAR: el nicho de fuego tallado BAJO la cámara y bajo el cesto. Sellado por construcción (piedra arriba, piedra a los lados, roca debajo) -- no le puede caer nada dentro; es teatro puro, y es lo que hace que "fuego bajo" sea una descripción y no una promesa.</summary>
        public const int HogarFilas = 2;
        /// <summary>
        /// Celdas de aire entre el muro derecho de la cámara y el muro
        /// izquierdo del brasero. 10 -&gt; **6** (segunda pasada, visto
        /// jugando): con 10, el cesto quedaba a 4 celdas de la jamba de la
        /// PRENSA y a 12 de la cámara del Crisol -- o sea que se leía como
        /// parte de la prensa. La duda original de Cesar ("mucho menos sé
        /// dónde poner el combustible") habría sobrevivido intacta. Con 6, el
        /// cesto se mete debajo del flanco derecho de la boca embudada: los
        /// dos recintos se leen como UN horno con DOS bocas, y quedan 8
        /// celdas de aire limpio hasta la Prensa.
        /// </summary>
        public const int BraseroSeparacion = 6;

        // -----------------------------------------------------------------
        // SUELO SOBERANO (playtest 32, fix "aparece un poco enterrada" /
        // "rastro de bedrock" -- ver AplanarPlataforma/RestaurarSueloBase
        // más abajo para el porqué completo).
        // -----------------------------------------------------------------
        /// <summary>Holgura a cada lado del rect exterior que la plataforma propia aplana. Coincide con el margen que Sim/SimLevelBuilder.AdornarCuarto respeta alrededor de cada obra registrada -- una terraza nunca puede empezar dentro de lo que esta estación ya se garantiza a sí misma llano.</summary>
        private const int PlataformaMargen = 2;
        /// <summary>Filas de piedra maciza garantizadas DEBAJO del suelo propio (baseY-1 hacia abajo). De sobra para tapar el escalón más alto que talla una terraza (2-4 filas, ver SimLevelBuilder.TallarTerraza) si la máquina se muda encima de una.</summary>
        private const int PlataformaProfundidad = 6;

        // ---- Compatibilidad de nombres (regla 15: se documenta lo que se
        // retira, no se borra en silencio). Sim/SimLevelBuilder.cs del
        // playtest 26 documentaba las huelgas citando CubetaAncho/TolvaAncho/
        // HuecoEntreCubetaYTolva. Se conservan como alias EXACTOS de las
        // medidas nuevas para que ningún comentario ni llamante quede
        // colgando, y para que nadie reintroduzca los valores viejos.
        public const int CubetaAncho = CamaraAncho;
        public const int CubetaAlto = CamaraAlto;
        public const int TolvaAncho = BraseroAncho;
        public const int TolvaAlto = BraseroAlto;
        public const int HuecoEntreCubetaYTolva = BraseroSeparacion;

        // -----------------------------------------------------------------
        // HORNADA (mandato 4)
        // -----------------------------------------------------------------
        /// <summary>Duración de una hornada. El contrato pide 8-12s "con progreso que se ve": 10 es el centro, y es el tiempo en el que da tiempo a MIRAR sin aburrirse.</summary>
        private const float HornadaSegundos = 10f;
        /// <summary>Fracción de la hornada durante la que el objetivo térmico se mantiene POR DEBAJO del umbral del mundo (ver <see cref="TechoSeguroPara"/>): así ninguna transformación ocurre antes de tiempo y no hay ninguna carrera invisible.</summary>
        private const float FraccionConTecho = 0.88f;
        /// <summary>Cuánto sube/baja la temperatura de la cámara por tick de física mientras corre una hornada.</summary>
        private const int TempStepPerTick = 5;
        /// <summary>Margen por encima del punto de solidificación al que el crisol sostiene un Fundido durante la hornada de RECOCIDO -- lo justo para que el mundo no lo temple por su cuenta antes de que acabe la pasada.</summary>
        private const int MargenRecocido = 3;
        /// <summary>Margen por debajo del umbral del mundo al que se clampea la rampa durante <see cref="FraccionConTecho"/>.</summary>
        private const int MargenTecho = 2;

        // -----------------------------------------------------------------
        // CONVERSIÓN POR FRENTES (playtest 44, LA FÍSICA HONESTA,
        // docs/CONTRATO_TERMICA.md §2a): la hornada dejaba de VERSE mientras
        // corría -- toda la cámara convertía de golpe en CerrarHornada, sin
        // que ninguna celda cambiara de material durante los 10s previos.
        // Ver el docblock de <see cref="ProcessConversionFrente"/> para el
        // mecanismo completo (por qué es un umbral por FILA y no un empuje
        // por fila -- se modeló primero el empuje y no separaba nada, la
        // rampa compartida es demasiado lenta para que un sesgo de empuje
        // produzca distancia visible entre filas en 300 ticks).
        // -----------------------------------------------------------------
        /// <summary>
        /// FRACCIÓN de <c>(cima-ambiente)</c> que la fila más próxima al
        /// hogar (fila 0, y=_camY0) se ahorra frente a la CIMA real:
        /// convierte en cuanto la rampa compartida cruza
        /// <c>cima - MargenFrenteFraccion*(cima-ambiente)</c>, mientras que
        /// la fila más lejana (junto a la boca) exige la cima exacta -- que
        /// solo se libera en el último <see cref="FraccionConTecho"/>..1 de
        /// la hornada.
        ///
        /// FRACCIÓN, NO UN RAW FIJO -- ESTO YA SE PROBÓ MAL UNA VEZ (modelado
        /// numérico de la rampa real, no a ojo): un margen fijo de 25 raw daba
        /// ~69 ticks de separación con <c>cima</c>=120 (el rescoldo propio,
        /// <see cref="Universe.CrisolTier0Raw"/>) pero se hundía a ~56 con
        /// <c>cima</c>=190 (el mejor combustible sorteable, ver
        /// <c>Universe.TempCombustibleRawBase</c>, 165..190) -- la rampa
        /// cubre el MISMO número de ticks (0..264) sea cual sea el rango de
        /// temperatura, así que a más rango, más raw por tick, y un margen
        /// fijo se cruza antes en términos de TICKS cuanto más caliente sea
        /// la hornada -- justo lo contrario de "el mismo espectáculo en toda
        /// hornada". Con el margen como FRACCIÓN del rango real, la
        /// separación medida se queda estable entre ~58 y ~89 ticks en todo
        /// el rango practicable de <c>cima</c> del juego (120 sin
        /// combustible; 165..190 con el mejor; ~100..155 en Recocido, ver
        /// <c>Universe.SolidificaRaw</c>) -- SIEMPRE por encima del piso de
        /// 60 que exige el contrato, con margen para el jitter del
        /// muestreo/probabilidad de abajo. NO válido si <c>cima</c> cayera a
        /// menos de ~15-20 raw por encima de ambiente (degenera a spread≈0,
        /// ver el modelo): no ocurre hoy -- el objetivo mínimo real es
        /// Recocido en su peor seed, <c>SolidificaRaw+MargenRecocido</c> ≈
        /// 103, 33 raw por encima de <see cref="CellGrid.AmbientRaw"/> --
        /// pero queda anotado por si una ronda futura introduce una hornada
        /// de objetivo casi-ambiente.
        /// </summary>
        private const float MargenFrenteFraccion = 0.25f;
        /// <summary>
        /// Máscara de muestreo barato: solo se comprueba 1 de cada 4 celdas
        /// por tick (<c>(x+y+tick)&MuestreoMascara == 0</c>), el mismo patrón
        /// de "muestreo por tablero" que <see cref="Sim.SimStepper"/> usa
        /// para difusión/morfología -- una hornada llena (117 celdas) no
        /// necesita mirar las 117 cada uno de los 300 ticks para que el
        /// frente se vea avanzar con fluidez.
        /// </summary>
        private const int MuestreoMascara = 3;
        /// <summary>
        /// Probabilidad de convertir una celda muestreada que YA cruzó su
        /// umbral de fila, no 100%: da un chisporroteo irregular en vez de un
        /// barrido geométrico perfecto (más "se está cocinando" que "un
        /// escáner"). <see cref="CerrarHornada"/> sigue de garantía: ninguna
        /// celda se queda sin convertir por mala suerte de este dado.
        /// </summary>
        private const int ProbabilidadConversionPct = 55;
        /// <summary>
        /// Salt propia de <see cref="XorShift.FromCell"/> para el frente de
        /// conversión -- 563 para no colisionar con ninguna de las
        /// CONSTANTES fijas que ya usan Sim/SimStepper.cs (1, 2, 5, 9, 13,
        /// 17, 42, 77, 88, 91, 205, 237, 239, 503, 509, 521, 523, 547, 549,
        /// 551, 553, 557) ni con <see cref="SalVaporCrisol"/> (241) de este
        /// mismo archivo -- verificado con grep antes de fijar el número.
        /// </summary>
        private const uint SalFrenteHornada = 563;
        /// <summary>
        /// Total de celdas convertidas por <see cref="ProcessConversionFrente"/>
        /// DURANTE la hornada en curso, acumulado tick a tick. Se resetea en
        /// <see cref="IntentarEncender"/> (una hornada nueva, un contador
        /// nuevo) y <see cref="CerrarHornada"/> le SUMA las stragglers de su
        /// propia pasada de garantía antes de usarlo para el witness forense
        /// (<see cref="Hornada.RegistrarOp"/>) y el vapor -- el jugador ve
        /// UN solo número al final, el mismo total que vería si el frente no
        /// existiera, solo que ahora ha llegado ahí VIÉNDOSE avanzar.
        /// </summary>
        private int _hornadaConvertidasAcumulado;

        // -----------------------------------------------------------------
        // LA ALQUIMIA VISIBLE (playtest 30, encargo de Cesar: "el mejor
        // efecto que teníamos es el de fuego y ahora no tiene lugar" +
        // "evaporar cosas... que se vea el agua dejar la olla").
        // -----------------------------------------------------------------
        /// <summary>Cada cuánto se refresca la llama pintada del brasero -- sondeo con acumulador (nunca por tick, CLAUDE.md regla "probes con acumulador"), mismo orden de magnitud que <see cref="MaquinariaSprites.AffordanceGlow.ProbeIntervalSeconds"/>.</summary>
        private const float FuegoBraseroRefrescoSeg = 0.2f;
        /// <summary>Celdas de hornada CONVERTIDAS (ver <see cref="CerrarHornada"/>) por cada celda de Steam que se empuja sobre la cámara -- "que se vea el agua dejar la olla" sin inundar el aire de vapor. Una hornada llena de la cámara (117 celdas) deja como mucho 19 celdas de vapor visibles, un puñado de bocanadas, no una tormenta.</summary>
        private const int VaporPorCeldas = 6;
        /// <summary>Salt propia para <see cref="XorShift.FromCell"/> al elegir la columna del vapor -- primera salt que usa este archivo; 241 para no colisionar con ninguna de las CONSTANTES fijas que ya usa Sim/SimStepper.cs (1, 2, 5, 9, 13, 17, 42, 77, 88, 91, 205, 237, 239 -- verificado con grep antes de fijar el número).</summary>
        private const uint SalVaporCrisol = 241;

        private enum Fase { Reposo, Corriendo, Lista }

        private AlkahestSim _sim;
        private Transform _player;
        private SubstanceKnowledge _conocimiento;

        private int _anchorX;
        private int _baseY;

        // Cámara y brasero (interiores, sin muros).
        private int _camX0, _camX1, _camY0, _camY1;
        private int _braX0, _braX1, _braY0, _braY1;
        private int _bocaY0, _bocaY1;
        private int _outX0, _outX1, _outY0, _outY1;

        /// <summary>
        /// (playtest 29) Handle del rect anticincel de ESTA instancia en
        /// <see cref="SimLevelBuilder.ObraDelTaller"/> -- lo devuelve
        /// <see cref="SimLevelBuilder.RegistrarObra"/> al registrarse en
        /// <see cref="Init"/> (no en <see cref="TallarEnPlano"/>, que es
        /// estático y corre ANTES de que exista esta instancia: ver el
        /// bloque "OBRA MOVIBLE" en Sim/SimLevelBuilder.cs para el porqué del
        /// diseño). <see cref="Reposicionar"/> lo usa para actualizar el rect
        /// en vez de dejar el viejo protegido para siempre.
        /// </summary>
        private int _handleObra = -1;

        private Vector3 _centro, _centroCamara, _centroBrasero, _centroBoca;
        private float _accumulator;

        private Fase _fase = Fase.Reposo;
        private float _hornadaT;
        private byte _hornadaEntrada, _hornadaSalida;
        private byte _hornadaCima;      // temperatura que alcanza esta hornada.
        private byte _hornadaTecho;     // clampeo durante FraccionConTecho.
        private string _hornadaCondicion;
        private string _hornadaVerbo;
        private byte _targetRaw;        // objetivo térmico ACTUAL (0 = no empujar nada: reposo).
        private byte _reposoRaw;        // temperatura de mantenimiento del resultado (fase Lista).

        private byte _fuelMat;          // combustible presente en el cesto ahora mismo (Empty = ninguno).
        private bool _cestoArdiendo;    // ¿arde AHORA? Solo durante una hornada alimentada.

        private bool _camaraTieneAlgo;
        private byte _dominanteCamara;

        /// <summary>Acumulador del sondeo de la llama del brasero (LA ALQUIMIA VISIBLE, tarea 1) -- ver <see cref="RefrescarLlamasBrasero"/>.</summary>
        private float _llamaAcc;

        // ---- Visual ----
        private SpriteRenderer _resalte;
        private SpriteRenderer _latidoTrabajo;
        private SpriteRenderer _brasasHogar, _brasasCesto;
        private SpriteRenderer _destelloCamara, _destelloCesto;

        // (playtest 31, ILUMINACIÓN DE ÁNIMO) EL CRISOL ES LA FUENTE DE LUZ
        // PRINCIPAL DEL TALLER. Dos halos, no uno: el HOGAR (bajo la panza,
        // siempre encendido aunque sea un rescoldo -- un horno de alquimista
        // nunca está del todo frío) y el BRASERO (solo cuando arde
        // combustible de verdad, y entonces es la luz más viva de la escena).
        // Que respiren a ritmos y desfases distintos es lo que hace que
        // parezcan fuego y no una animación: ver MaquinariaSprites.Luz.Latir.
        private MaquinariaSprites.Luz _luzHogar, _luzBrasero, _luzCamara;
        /// <summary>(playtest 33) Las dos luces RECTANGULARES recortadas a la propia mampostería -- ver el bloque "LA LUZ DEJA DE SER UN STICKER" en BuildVisual.</summary>
        private MaquinariaSprites.Luz _luzMuro, _luzMuroCesto;
        private float _alfaResalte;
        private const int Burbujas = 6;
        private readonly SpriteRenderer[] _burbujas = new SpriteRenderer[Burbujas];
        // -----------------------------------------------------------------
        // BOCANADAS DE LA CHIMENEA (retocadas en el playtest 41,
        // CONTRATO_VAPOR.md §2 -- "la animación de vapor es muy mala", Cesar)
        //
        // QUÉ SE MIRÓ ANTES DE TOCAR NADA (regla 52): con el gas de la sim ya
        // convectando de verdad (encargo S), el penacho REAL que sale del
        // brasero y de la boca del crisol es ancho, irregular y serpentea. Al
        // lado de eso, estas cuatro bocanadas se leían como una cinta
        // transportadora, y por tres motivos concretos que estaban en la
        // fórmula:
        //   (a) nacían a alfa 0.70 de golpe en la boca del tubo -- un POP. El
        //       humo real no aparece opaco: se hace visible.
        //   (b) las cuatro compartían periodo, altura, velocidad y amplitud
        //       exactos, desfasadas 1/4 de ciclo: un carrusel perfectamente
        //       periódico, que es justo lo que el ojo lee como "animación".
        //   (c) el rizo lateral era un seno de amplitud fija, así que las
        //       cuatro trazaban LA MISMA ese.
        //
        // DECISIÓN (sprite vs gas): NO se retiran. La chimenea es el verbo del
        // cuerpo del crisol (gramática visual del playtest 26: "chimenea =
        // está trabajando") y ahí NO nace gas de la sim -- la combustión
        // ocurre dentro del cesto, no en el tubo. Retirarlas dejaría al
        // aparato sin señal de trabajo. Lo que sí cambian es de RANGO: pasan
        // de protagonista a ACENTO (alfa pico 0.70 -> 0.34) y dejan de mentir
        // como animación:
        //   - nacen transparentes y se hacen visibles en el primer 22% de su
        //     vida (mata el pop) y se apagan al cuadrado (una voluta se
        //     desvanece, no se corta);
        //   - cada bocanada lleva su propio periodo, altura, amplitud y
        //     deriva, sacados de una tabla FIJA por índice (ver
        //     HumoVariacion*): rompe el carrusel sin un solo Random ni una
        //     alloc por frame;
        //   - el rizo suma una DERIVA que crece con la altura (el humo se
        //     escora al alejarse del tubo) en vez de un seno puro.
        // -----------------------------------------------------------------
        private const int HumoPuffs = 4;
        private const float HumoCicloSeg = 2.4f;
        /// <summary>Alfa máxima de una bocanada. Bajada de 0.70 a 0.34 en el playtest 41: el gas real de la sim es el protagonista y esto es un acento del aparato, no humo que compita con él.</summary>
        private const float HumoAlfaPico = 0.34f;
        /// <summary>Fracción de vida durante la que la bocanada SE HACE VISIBLE al salir del tubo. Sin esto nace opaca de golpe en la boca -- el "pop" que delataba la animación.</summary>
        private const float HumoFraccionEntrada = 0.22f;
        /// <summary>Multiplicador del periodo de cada bocanada (índice 0..3). Números primos entre sí a ojo para que las cuatro no vuelvan a coincidir en fase: sin esto son un carrusel.</summary>
        private static readonly float[] HumoVariacionPeriodo = { 1.00f, 1.37f, 0.79f, 1.18f };
        /// <summary>Altura que alcanza cada bocanada, en celdas. Distintas entre sí: una columna de humo no tiene un techo común.</summary>
        private static readonly float[] HumoVariacionAltura = { 12.0f, 9.5f, 14.5f, 11.0f };
        /// <summary>Amplitud del rizo lateral de cada bocanada, en celdas.</summary>
        private static readonly float[] HumoVariacionRizo = { 1.20f, 0.70f, 1.60f, 0.95f };
        /// <summary>Deriva lateral acumulada al final del ascenso, en celdas: el humo SE ESCORA según sube en vez de volver siempre al eje del tubo.</summary>
        private static readonly float[] HumoVariacionDeriva = { 0.9f, -1.4f, 1.8f, -0.6f };
        private readonly SpriteRenderer[] _humo = new SpriteRenderer[HumoPuffs];
        private Vector3 _humoOrigen;

        // Acuse de recibo (mandato 3): destello del marco al entrar materia.
        private readonly MaquinariaSprites.Destello _acuseCamara = new MaquinariaSprites.Destello();
        private readonly MaquinariaSprites.Destello _acuseCesto = new MaquinariaSprites.Destello();
        private int _celdasCamaraPrev, _celdasCestoPrev;

        // El pulso de la clase AffordanceGlow, en su destino aprobado: TRABAJANDO.
        private readonly MaquinariaSprites.AffordanceGlow _pulsoTrabajo = new MaquinariaSprites.AffordanceGlow();

        private const float RangoEstadoPleno = 7.0f;
        private const float RangoEstadoDesvanece = 9.0f;
        private const float RangoNombrePleno = 3.4f;
        private const float RangoNombreDesvanece = 4.6f;
        private bool _yaConocida;
        private const string ChapaNombre = "el crisol";

        public Vector3 PuntoFoco => _centroCamara;
        public float RangoFoco => ProximityRange;

        // ---- IMovible ----
        public Vector3 CentroMundo => _centro;
        public Vector2 TamanoMundo => new Vector2(
            (_outX1 - _outX0 + 1) * SimRenderer.CellWorldSize,
            (_outY1 - _outY0 + 1) * SimRenderer.CellWorldSize);
        /// <summary>Ancla: esquina inferior izquierda del rect EXTERIOR del horno (boca incluida). Todo lo demás viaja relativo a esto.</summary>
        public Vector2Int AnclaCelda => new Vector2Int(_outX0, _baseY);

        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = _outX1 - _outX0 + 1;
            int alto = _outY1 - _outY0 + 1;
            return anclaCelda.x >= 1 && anclaCelda.x + span - 1 <= CellGrid.W - 2
                && anclaCelda.y >= 1 && anclaCelda.y + alto - 1 <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap. FIRMA SIN CAMBIOS. `anchorX` = SimLevelBuilder.CrisolX.</summary>
        public void Init(AlkahestSim sim, Transform player, SubstanceKnowledge conocimiento, int anchorX)
        {
            _sim = sim;
            _player = player;
            _conocimiento = conocimiento;

            _anchorX = anchorX;
            _baseY = SimLevelBuilder.BaseYDeEstacion(SimLevelBuilder.CrisolX); // (playtest 33) cota por zona -- ver BaseYDeEstacion.

            RecalcularRegiones();
            BuildVisual();
            _targetRaw = 0; // REPOSO: el crisol no empuja nada hasta que enciendas una hornada.

            // (playtest 39, contrato ENCARGO S 1e) REACCIONES DIRIGIDAS: se
            // registran aquí, no en SimLevelBuilder (que solo talla CellGrid
            // y no tiene SimStepper), porque el Crisol es el único archivo de
            // este encargo con acceso a AMBOS a la vez en tiempo de
            // ejecución. Ver el docblock de SimLevelBuilder.RegistrarZonasInteres
            // para el porqué completo y la deuda de integración anotada
            // (el sitio "natural" es el bootstrap del nivel, que no es
            // archivo de este encargo). Solo hace falta UNA vez -- el Crisol
            // es una instancia única del taller.
            if (_sim.Stepper != null) SimLevelBuilder.RegistrarZonasInteres(_sim.Stepper);

            MachineFocus.Registrar(this);
            // (playtest 29) El registro anticincel lo hace la INSTANCIA, no
            // TallarEnPlano -- ver el docblock de _handleObra y el bloque
            // "OBRA MOVIBLE" en Sim/SimLevelBuilder.cs. (playtest 32, FIX)
            // `TallarEnPlano` (estático, corre en BuildCuartoIntimo) YA
            // registró este MISMO rect para que AdornarCuarto pudiera verlo
            // al tallar terrazas (ver SimLevelBuilder.HallarObraExacta) --
            // aquí se RECLAMA ese handle en vez de crear uno nuevo huérfano.
            int hOut0 = _outX0, hOut1 = _outX1, hOut0y = _outY0 - HogarFilas, hOut1y = _outY1;
            int handleExistente = SimLevelBuilder.HallarObraExacta(hOut0, hOut0y, hOut1, hOut1y);
            _handleObra = handleExistente >= 0 ? handleExistente : SimLevelBuilder.RegistrarObra(hOut0, hOut0y, hOut1, hOut1y);
            Mudanza.RegistrarMovible(this);
        }

        // =================================================================
        // HUELLA — una sola aritmética, compartida por la instancia y por el
        // tallado del plano (para que el dibujo y la piedra jamás difieran).
        // =================================================================
        private struct Huella
        {
            public int CamX0, CamX1, CamY0, CamY1;
            public int BraX0, BraX1, BraY0, BraY1;
            public int BocaY0, BocaY1;
            public int OutX0, OutX1, OutY0, OutY1;
        }

        private static Huella Calcular(int anchorX, int baseY)
        {
            Huella h;
            h.CamX0 = anchorX - CamaraAncho / 2;
            h.CamX1 = h.CamX0 + CamaraAncho - 1;
            h.CamY0 = baseY + 1;
            h.CamY1 = h.CamY0 + CamaraAlto - 1;

            h.BocaY0 = h.CamY1 + 1;
            h.BocaY1 = h.BocaY0 + BocaFilas - 1;

            h.BraX0 = h.CamX1 + MuroGrosor + BraseroSeparacion + MuroGrosor;
            h.BraX1 = h.BraX0 + BraseroAncho - 1;
            h.BraY0 = baseY + 1;
            h.BraY1 = h.BraY0 + BraseroAlto - 1;

            h.OutX0 = h.CamX0 - MuroGrosor - BocaVuelo;
            h.OutX1 = h.BraX1 + MuroGrosor;
            h.OutY0 = baseY;
            h.OutY1 = h.BocaY1 + 1;
            return h;
        }

        /// <summary>Cuánto se ha abierto la boca en la fila `i` (0 = la primera sobre la cámara). Progresión entera para que la rampa se vea escalonada y tallada, no interpolada.</summary>
        private static int VueloEnFila(int i) => (BocaVuelo * (i + 1) + BocaFilas / 2) / BocaFilas;

        private void RecalcularRegiones()
        {
            var h = Calcular(_anchorX, _baseY);
            _camX0 = h.CamX0; _camX1 = h.CamX1; _camY0 = h.CamY0; _camY1 = h.CamY1;
            _braX0 = h.BraX0; _braX1 = h.BraX1; _braY0 = h.BraY0; _braY1 = h.BraY1;
            _bocaY0 = h.BocaY0; _bocaY1 = h.BocaY1;
            _outX0 = h.OutX0; _outX1 = h.OutX1; _outY0 = h.OutY0; _outY1 = h.OutY1;

            float c = SimRenderer.CellWorldSize;
            _centro = new Vector3((_outX0 + (_outX1 - _outX0 + 1) * 0.5f) * c,
                                  (_outY0 + (_outY1 - _outY0 + 1) * 0.5f) * c, 0f);
            transform.position = _centro;
            _centroCamara = new Vector3((_camX0 + CamaraAncho * 0.5f) * c, (_camY0 + CamaraAlto * 0.5f) * c, 0f);
            _centroBrasero = new Vector3((_braX0 + BraseroAncho * 0.5f) * c, (_braY0 + BraseroAlto * 0.5f) * c, 0f);
            _centroBoca = new Vector3((_camX0 + CamaraAncho * 0.5f) * c, (_bocaY1 + 1f) * c, 0f);
            _humoOrigen = new Vector3((_camX1 + BocaVuelo - 0.5f) * c, (_bocaY1 + 11f) * c, 0f);
        }

        /// <summary>
        /// (playtest 32, encargo A: "SUELO SOBERANO BAJO CADA ESTACIÓN")
        /// Aplana un colchón propio ANTES de tallar nada más: piedra maciza
        /// de <see cref="PlataformaProfundidad"/> filas justo debajo del
        /// suelo, INCLUYENDO la fila `baseY`, y vacío desde `baseY+1` hasta
        /// la cornisa de la huella (outY1), con <see cref="PlataformaMargen"/>
        /// celdas de holgura a cada lado del rect exterior. Así la estación
        /// nace SIEMPRE sobre una losa propia y plana -- esté el terreno real
        /// debajo donde esté (una terraza tallada por
        /// SimLevelBuilder.AdornarCuarto, un desnivel futuro, lo que sea) --
        /// nunca "un poco enterrada" ni con el canto de una terraza asomando
        /// por un lado.
        ///
        /// `baseY` se queda SIEMPRE en el colchón de piedra, nunca en el
        /// vaciado -- no es un capricho: el suelo del recinto (`TallarRecinto`,
        /// fila `y0-1` = `baseY`) solo repinta piedra bajo su PROPIO ancho
        /// (cámara o brasero), no bajo el margen ni bajo el hueco de
        /// `BraseroSeparacion` entre ambos -- si esta plataforma vaciara
        /// `baseY` ahí, ese hueco se quedaría con un agujero en el suelo que
        /// nada vuelve a tapar. Construcción de nivel: `SetCell`, no
        /// `PaintStable` (regla 29 es para runtime; ver
        /// <see cref="AplanarPlataformaCaliente"/> para el equivalente que usa
        /// Reposicionar).
        /// </summary>
        private static void AplanarPlataforma(CellGrid grid, int outX0, int outX1, int baseY, int outY1)
        {
            int x0 = outX0 - PlataformaMargen;
            int x1 = outX1 + PlataformaMargen;
            for (int x = x0; x <= x1; x++)
            {
                for (int y = baseY - PlataformaProfundidad; y <= baseY; y++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Stone);
                for (int y = baseY + 1; y <= outY1; y++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
            }
        }

        /// <summary>Talla el horno completo (cámara + boca embudada + cesto del brasero) sobre el CellGrid del plano. Construcción de nivel: `SetCell`, no `PaintStable` (regla 29 es para runtime).</summary>
        public static void TallarEnPlano(CellGrid grid, int anchorX, int baseY)
        {
            var h = Calcular(anchorX, baseY);

            AplanarPlataforma(grid, h.OutX0, h.OutX1, baseY, h.OutY1); // (playtest 32) SIEMPRE lo primero -- ver el docblock del método.

            // Cámara: suelo + dos muros + interior vaciado.
            TallarRecinto(grid, h.CamX0, h.CamX1, h.CamY0, h.CamY1);

            // LA BOCA EMBUDADA: en cada fila las paredes se separan un poco
            // más, así que la materia que caiga dentro de la boca RESBALA
            // hacia la cámara -- el embudo es la piedra, no un sprite.
            for (int i = 0; i < BocaFilas; i++)
            {
                int y = h.BocaY0 + i;
                int vuelo = VueloEnFila(i);
                int izq = h.CamX0 - vuelo;
                int der = h.CamX1 + vuelo;
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    if (CellGrid.InBounds(izq - t, y)) grid.SetCell(izq - t, y, MaterialId.Stone);
                    if (CellGrid.InBounds(der + t, y)) grid.SetCell(der + t, y, MaterialId.Stone);
                }
                for (int x = izq; x <= der; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
            }

            // El cesto del brasero, aparte.
            TallarRecinto(grid, h.BraX0, h.BraX1, h.BraY0, h.BraY1);

            // (segunda pasada) LOS DOS HOGARES: sendos nichos de fuego bajo la
            // cámara y bajo el cesto, tallados en la losa del cuarto (filas
            // baseY-2..baseY-1). Quedan SELLADOS por construcción -- piedra
            // encima (la fila baseY, el suelo del recinto), piedra a los lados
            // (el resto de la losa) y roca maciza debajo (la losa del cuarto
            // solo llega hasta CuartoY0) -- así que nada puede caer dentro ni
            // salir: son puro teatro. Antes, las brasas se dibujaban sueltas
            // SOBRE el suelo y se leían como grava roja derramada.
            TallarHogar(grid, h.CamX0, h.CamX1, baseY);
            TallarHogar(grid, h.BraX0, h.BraX1, baseY);

            // (playtest 29) Este método es estático y corre UNA vez desde
            // SimLevelBuilder.BuildCuartoIntimo, ANTES de que exista ninguna
            // instancia de Crisol. (playtest 32, FIX) Por eso mismo SÍ hace
            // falta registrar aquí: SimLevelBuilder.AdornarCuarto también
            // corre dentro de BuildCuartoIntimo, DESPUÉS de este tallado pero
            // ANTES de que exista ninguna instancia -- si el registro
            // esperara a `Init` (que corre en otro frame, desde
            // Game/AlkahestGameBootstrap.cs), AdornarCuarto tallaría terrazas
            // como si el Crisol no existiera. `Init` RECLAMA este mismo
            // handle en vez de duplicarlo (ver SimLevelBuilder.HallarObraExacta).
            SimLevelBuilder.RegistrarObra(h.OutX0, h.OutY0 - HogarFilas, h.OutX1, h.OutY1);
        }

        /// <summary>Vacía el nicho de fuego bajo un recinto (ver el comentario de <see cref="TallarEnPlano"/> para por qué queda sellado y no es una trampa para la materia).</summary>
        private static void TallarHogar(CellGrid grid, int x0, int x1, int baseY)
        {
            for (int y = baseY - HogarFilas; y <= baseY - 1; y++)
                for (int x = x0; x <= x1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
        }

        private static void TallarRecinto(CellGrid grid, int x0, int x1, int y0, int y1)
        {
            for (int x = x0 - MuroGrosor; x <= x1 + MuroGrosor; x++)
                if (CellGrid.InBounds(x, y0 - 1)) grid.SetCell(x, y0 - 1, MaterialId.Stone);
            for (int y = y0 - 1; y <= y1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    if (CellGrid.InBounds(x0 - t, y)) grid.SetCell(x0 - t, y, MaterialId.Stone);
                    if (CellGrid.InBounds(x1 + t, y)) grid.SetCell(x1 + t, y, MaterialId.Stone);
                }
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);
        }

        /// <summary>Equivalente EN CALIENTE de <see cref="AplanarPlataforma"/> -- las celdas de Stone que CREA usan PaintStable (regla 29), la franja que vacía usa PaintRect (Empty no tiene temperatura de estabilidad que respetar). Misma convención de límites (`_baseY` en el colchón, vaciado desde `_baseY+1`) -- ver el docblock de la versión fría.</summary>
        private void AplanarPlataformaCaliente()
        {
            int x0 = _outX0 - PlataformaMargen;
            int x1 = _outX1 + PlataformaMargen;
            int ancho = x1 - x0 + 1;
            for (int y = _baseY - PlataformaProfundidad; y <= _baseY; y++)
                for (int x = x0; x <= x1; x++)
                    _sim.PaintStable(x, y, 0, MaterialId.Stone);
            _sim.PaintRect(x0, _baseY + 1, ancho, _outY1 - (_baseY + 1) + 1, MaterialId.Empty);
        }

        /// <summary>Misma talla que <see cref="TallarEnPlano"/> pero EN CALIENTE (regla 29: PaintStable). Solo la usa <see cref="Reposicionar"/> (Mudanza).</summary>
        private void TallarEnCaliente()
        {
            AplanarPlataformaCaliente(); // (playtest 32) SIEMPRE lo primero -- ver AplanarPlataforma.
            TallarRecintoCaliente(_camX0, _camX1, _camY0, _camY1);
            for (int i = 0; i < BocaFilas; i++)
            {
                int y = _bocaY0 + i;
                int vuelo = VueloEnFila(i);
                int izq = _camX0 - vuelo, der = _camX1 + vuelo;
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.PaintStable(izq - t, y, 0, MaterialId.Stone);
                    _sim.PaintStable(der + t, y, 0, MaterialId.Stone);
                }
                _sim.PaintRect(izq, y, der - izq + 1, 1, MaterialId.Empty);
            }
            TallarRecintoCaliente(_braX0, _braX1, _braY0, _braY1);
        }

        private void TallarRecintoCaliente(int x0, int x1, int y0, int y1)
        {
            for (int x = x0 - MuroGrosor; x <= x1 + MuroGrosor; x++) _sim.PaintStable(x, y0 - 1, 0, MaterialId.Stone);
            for (int y = y0 - 1; y <= y1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.PaintStable(x0 - t, y, 0, MaterialId.Stone);
                    _sim.PaintStable(x1 + t, y, 0, MaterialId.Stone);
                }
            _sim.PaintRect(x0, y0, x1 - x0 + 1, y1 - y0 + 1, MaterialId.Empty);
        }

        /// <summary>
        /// (playtest 29, encargo B) Borra la mampostería VIEJA de la huella
        /// `h` -- <see cref="Reposicionar"/> la llama con la huella de ANTES
        /// de mover el ancla, justo antes de tallar la nueva. Espejo de
        /// <see cref="TallarRecinto"/>/<see cref="TallarEnCaliente"/> con dos
        /// diferencias a propósito:
        ///  1. Escribe <see cref="MaterialId.Empty"/> vía <c>_sim.Paint</c>
        ///     en vez de Stone vía PaintStable -- esto no CREA materia, la
        ///     QUITA (regla 29 de CLAUDE.md), el mismo camino que usa
        ///     Game/Cincel.cs al tallar piedra a vacío.
        ///  2. NUNCA toca la fila `y0-1` de cada recinto (la losa COMPARTIDA
        ///     de todo el cuarto, <c>SimLevelBuilder.BuildCuartoFloor</c> --
        ///     "jamás piedra del mundo", encargo B) ni el interior de cámara/
        ///     brasero (puede tener materia dentro: "el contenido... queda
        ///     donde está", mismo encargo -- cae solo por gravedad en cuanto
        ///     el muro que lo contenía desaparece). Solo desaparecen los
        ///     MUROS propios que esta máquina inventó sobre esa losa.
        /// </summary>
        private void BorrarEnCaliente(Huella h)
        {
            BorrarRecintoCaliente(h.CamX0, h.CamX1, h.CamY0, h.CamY1);
            for (int i = 0; i < BocaFilas; i++)
            {
                int y = h.BocaY0 + i;
                int vuelo = VueloEnFila(i);
                int izq = h.CamX0 - vuelo, der = h.CamX1 + vuelo;
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.Paint(izq - t, y, 0, MaterialId.Empty);
                    _sim.Paint(der + t, y, 0, MaterialId.Empty);
                }
                // La fila interior de la boca (izq..der) ya es Empty por
                // diseño -- el embudo talla aire, nunca piedra -- así que no
                // hace falta (ni conviene: podría tener materia cayendo)
                // tocarla aquí.
            }
            BorrarRecintoCaliente(h.BraX0, h.BraX1, h.BraY0, h.BraY1);
        }

        /// <summary>Muros de un recinto, EXCLUYENDO la fila `y0-1` (la losa compartida del cuarto) y sin tocar el interior -- ver <see cref="BorrarEnCaliente"/>.</summary>
        private void BorrarRecintoCaliente(int x0, int x1, int y0, int y1)
        {
            for (int y = y0; y <= y1; y++)
                for (int t = 1; t <= MuroGrosor; t++)
                {
                    _sim.Paint(x0 - t, y, 0, MaterialId.Empty);
                    _sim.Paint(x1 + t, y, 0, MaterialId.Empty);
                }
        }

        /// <summary>
        /// (playtest 32, encargo A: fix "rastro de bedrock") Tras borrar la
        /// mampostería propia de la huella VIEJA, restaura el suelo de esa
        /// huella (+margen) al NIVEL BASE del cuarto -- la losa general de
        /// SimLevelBuilder.BuildCuartoFloor, constante, sin importar dónde
        /// estuviera <see cref="_baseY"/>. Sin esto, una máquina que se había
        /// mudado a un sitio NO estándar (baseY distinto del nivel base --
        /// p. ej. encima de una terraza) dejaba, al volver a mudarse, un
        /// escalón de piedra huérfano ahí: <see cref="BorrarEnCaliente"/>
        /// solo quita los MUROS propios y a propósito nunca toca la fila
        /// `h.OutY0-1`/`h.OutY0` (podría ser la losa compartida real, ver su
        /// docblock), así que la plataforma elevada que
        /// <see cref="AplanarPlataformaCaliente"/> había levantado ahí
        /// sobrevivía a la mudanza. Si `h.OutY0` (=baseY viejo) ya estaba al
        /// nivel base -- el caso normal, nunca se ha movido a otra altura --
        /// el bucle no itera nada: gratis.
        /// </summary>
        private void RestaurarSueloBase(Huella h)
        {
            int sueloBase = SimLevelBuilder.CuartoY0 + SimLevelBuilder.WallThickness - 1;
            int x0 = h.OutX0 - PlataformaMargen;
            int x1 = h.OutX1 + PlataformaMargen;
            for (int y = sueloBase + 1; y <= h.OutY0; y++)
                for (int x = x0; x <= x1; x++)
                    _sim.Paint(x, y, 0, MaterialId.Empty);
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        public void Reposicionar(Vector2Int anclaCelda)
        {
            // (playtest 29, encargo B) 1) BORRAR la mampostería vieja, con la
            // huella de ANTES de tocar el ancla -- si se calculara después de
            // mover _anchorX/_baseY, `Calcular` devolvería la huella NUEVA y
            // borraríamos el sitio equivocado.
            var huellaVieja = Calcular(_anchorX, _baseY);
            BorrarEnCaliente(huellaVieja);
            RestaurarSueloBase(huellaVieja); // (playtest 32) ver el docblock -- limpia cualquier pedestal elevado que esta instancia hubiera dejado ahí.

            int dx = anclaCelda.x - _outX0;
            int dy = anclaCelda.y - _baseY;
            _anchorX += dx;
            _baseY += dy;
            RecalcularRegiones();
            TallarEnCaliente(); // 2) TALLAR la nueva. regla 36: NUNCA volver a llamar a Init/BuildVisual para mover.

            // 3) ACTUALIZAR el registro anticincel -- mismo rect que Init
            // registró, con la geometría YA recalculada.
            SimLevelBuilder.ActualizarObra(_handleObra, _outX0, _outY0 - HogarFilas, _outX1, _outY1);
        }

        // =================================================================
        // BUCLE
        // =================================================================
        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return;

            SondearCamara();

            // LA ALQUIMIA VISIBLE (tarea 1): mientras el brasero arde de
            // verdad, refrescar sus llamas pintadas -- sondeo con
            // acumulador, NUNCA por frame/tick (regla de probes).
            _llamaAcc += Time.deltaTime;
            if (_llamaAcc >= FuegoBraseroRefrescoSeg)
            {
                _llamaAcc -= FuegoBraseroRefrescoSeg;
                RefrescarLlamasBrasero();
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                IntentarEncender();
                MachineFocus.RegistrarUsoE();
            }

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                if (_fase == Fase.Corriendo)
                {
                    _hornadaT += TickDt;
                    ActualizarObjetivoHornada();
                    // (playtest 44, §2a) El frente corre TICK A TICK, no solo
                    // al cierre -- necesita ver el _targetRaw YA actualizado
                    // de esta pasada (ActualizarObjetivoHornada de arriba) y
                    // EmpujarTemperatura de abajo aplicado sobre la MISMA
                    // temperatura que acaba de leer, así que va entre las dos.
                    ProcessConversionFrente();
                    if (_hornadaT >= HornadaSegundos) CerrarHornada();
                }
                EmpujarTemperatura();
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            _acuseCamara.Avanzar(Time.deltaTime);
            _acuseCesto.Avanzar(Time.deltaTime);
            _pulsoTrabajo.Trabajando = _fase == Fase.Corriendo;
            ActualizarVisual();
        }

        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        /// <summary>
        /// Una pasada barata por cámara y cesto: material dominante, si hay
        /// algo, y el ACUSE DE RECIBO (mandato 3) cuando el número de celdas
        /// ocupadas SUBE -- que es exactamente "acaba de entrar materia por
        /// donde debía". No hace falta comparar celda a celda: subir de
        /// ocupación solo puede venir de que algo ha entrado.
        /// </summary>
        private void SondearCamara()
        {
            var grid = _sim.Grid;
            int nCam = 0, nCesto = 0;
            byte dominante = MaterialId.Empty;
            int mejor = 0;

            for (int y = _camY0; y <= _camY1; y++)
            {
                for (int x = _camX0; x <= _camX1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty) continue;
                    nCam++;
                    int cuenta = 0;
                    for (int y2 = _camY0; y2 <= _camY1; y2++)
                        for (int x2 = _camX0; x2 <= _camX1; x2++)
                            if (grid.GetMat(x2, y2) == m) cuenta++;
                    if (cuenta > mejor) { mejor = cuenta; dominante = m; }
                }
            }

            byte fuel = MaterialId.Empty;
            var universe = _sim.Universe;
            for (int y = _braY0; y <= _braY1; y++)
            {
                for (int x = _braX0; x <= _braX1; x++)
                {
                    byte m = grid.GetMat(x, y);
                    if (m == MaterialId.Empty) continue;
                    // (LA ALQUIMIA VISIBLE, tarea 1) Las llamas que este mismo
                    // archivo pinta sobre el combustible (ver
                    // RefrescarLlamasBrasero) NO cuentan como "materia que
                    // acaba de entrar" -- si no se excluyen, cada llamita
                    // nueva/parpadeante dispararía el acuse de recibo del cesto
                    // sin que el jugador haya vertido nada.
                    if (m == MaterialId.Fire) continue;
                    nCesto++;
                    if (fuel == MaterialId.Empty && universe != null && universe.EsCombustible(m)) fuel = m;
                }
            }

            if (nCam > _celdasCamaraPrev) _acuseCamara.Disparar();
            if (nCesto > _celdasCestoPrev) _acuseCesto.Disparar();
            _celdasCamaraPrev = nCam;
            _celdasCestoPrev = nCesto;

            _camaraTieneAlgo = nCam > 0;
            _dominanteCamara = dominante;
            _fuelMat = fuel;

            // El resultado ya no está solo en la cámara: o el jugador lo
            // recogió (cámara vacía) o le echó materia nueva encima. En los
            // dos casos el crisol vuelve a REPOSO, para que el rótulo diga la
            // verdad ("cargado · E para encender") en vez de seguir anunciando
            // una hornada que ya no describe lo que hay dentro.
            if (_fase == Fase.Lista && (!_camaraTieneAlgo || _dominanteCamara != _hornadaSalida)) VolverAReposo();
        }

        private void VolverAReposo()
        {
            _fase = Fase.Reposo;
            _targetRaw = 0;
            _cestoArdiendo = false;
            _hornadaT = 0f;
        }

        /// <summary>
        /// LA ALQUIMIA VISIBLE (tarea 1, encargo de Cesar: "necesitamos VER
        /// cosas transformándose: el mejor efecto que teníamos es el de
        /// fuego y ahora no tiene lugar"). Mientras el brasero arde de
        /// verdad (<see cref="_cestoArdiendo"/> -- solo cierto durante una
        /// hornada que se encendió CON combustible, ver
        /// <see cref="IntentarEncender"/>), pinta <see cref="MaterialId.Fire"/>
        /// justo encima de la celda de combustible más alta de cada columna
        /// del cesto, DENTRO del recinto ya tallado (<see cref="_braX0"/>..
        /// <see cref="_braX1"/>, <see cref="_braY0"/>..<see cref="_braY1"/> --
        /// mampostería de piedra por los cuatro costados, ver
        /// <see cref="TallarEnPlano"/>: la piedra es inmune al fuego, así que
        /// nunca "quema el taller"). Usa <see cref="AlkahestSim.PaintStable"/>
        /// y no <see cref="AlkahestSim.Paint"/> a propósito (regla 29 de
        /// CLAUDE.md): esto CREA fuego de la nada, no mueve fuego que ya
        /// existiera, así que tiene que nacer a la temperatura estable del
        /// propio Fuego (<see cref="MaterialDef.StableBirthTempRaw"/>), no
        /// heredar la del hueco de aire que tuviera antes.
        ///
        /// EN CUANTO <see cref="_cestoArdiendo"/> se apaga (hornada cerrada
        /// o combustible agotado), este método deja de pintar -- "ni una
        /// llama más" -- y las que ya existen se comportan como cualquier
        /// Fuego del mundo (Sim/SimStepper.cs las apaga/consume solas por su
        /// propio arquetipo): no hace falta borrarlas a mano.
        /// </summary>
        private void RefrescarLlamasBrasero()
        {
            if (!_cestoArdiendo) return;
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            for (int x = _braX0; x <= _braX1; x++)
            {
                int topFuelY = -1;
                for (int y = _braY0; y <= _braY1; y++)
                    if (grid.GetMat(x, y) == _fuelMat) topFuelY = y; // el bucle sube: el último asignado es el más alto.

                if (topFuelY < 0) continue; // esta columna ya no tiene combustible: nada que encender encima.

                int flameY = topFuelY + 1;
                if (flameY > _braY1) continue; // el combustible llega hasta el borde del cesto: no hay hueco encima para la llama.

                byte encima = grid.GetMat(x, flameY);
                if (encima != MaterialId.Empty && encima != MaterialId.Fire) continue; // no desplazar nada que no sea aire o fuego previo.

                _sim.PaintStable(x, flameY, 0, MaterialId.Fire);
                grid.WakeChunk(x, flameY, tick);
            }
        }

        // =================================================================
        // ENCENDER UNA HORNADA
        // =================================================================
        /// <summary>
        /// (ENCARGO N, playtest 43) EL HANDLER COMPARTIDO DE E: este método
        /// YA era el cuerpo del E local -- `Update()` solo comprueba
        /// `EstaEnfocada()` (proximidad DEL ANFITRIÓN) ANTES de llamarlo, así
        /// que no había nada que extraer para reutilizarlo tal cual desde
        /// <see cref="UsarPorRed"/> (ver el docblock de
        /// <see cref="Alkahest.Net.IMaquinaUsableRemota"/>: esa proximidad no
        /// aplica en la vía remota). Único cambio de este encargo: la firma
        /// pasa de `void` a `bool` para poder devolver "no procedía" sin
        /// tocar ninguna de las ramas existentes.
        /// </summary>
        private bool IntentarEncender()
        {
            if (_fase == Fase.Corriendo) return false; // ya está trabajando: E no hace nada (y el rótulo lo dice).
            var universe = _sim.Universe;
            if (universe == null) return false;

            if (!_camaraTieneAlgo || _dominanteCamara == MaterialId.Empty)
            {
                Rotular("la cámara está vacía · vierte algo dentro", UiStyles.TextoTenue);
                return false;
            }

            // Temperatura DISPONIBLE en esta pasada: el rescoldo propio si el
            // cesto está vacío, o la del combustible cargado si lo hay. El
            // crisol nunca está muerto (regla 44 al revés), pero tampoco
            // trabaja solo.
            byte cima = _fuelMat != MaterialId.Empty
                ? universe.TempCombustibleRaw(_fuelMat)
                : Universe.CrisolTier0Raw;

            if (!DecidirHornada(universe, _dominanteCamara, cima,
                    out byte salida, out string condicion, out string verbo, out byte objetivo))
            {
                Rotular("este fuego no le hace nada · prueba otro combustible", UiStyles.Aviso);
                return false;
            }

            _hornadaEntrada = _dominanteCamara;
            _hornadaSalida = salida;
            _hornadaCondicion = condicion;
            _hornadaVerbo = verbo;
            _hornadaCima = objetivo;
            _hornadaTecho = TechoSeguroPara(universe, _hornadaEntrada, objetivo);
            _hornadaT = 0f;
            _hornadaConvertidasAcumulado = 0; // (playtest 44) una hornada nueva, un contador de frente nuevo.
            _fase = Fase.Corriendo;

            // El combustible se gasta AL ENCENDER: una celda, una pasada.
            if (_fuelMat != MaterialId.Empty)
            {
                ConsumirUnaCeldaDeCombustible();
                _cestoArdiendo = true;
            }
            Rotular(null, UiStyles.Aviso);
            return true;
        }

        // =================================================================
        // (ENCARGO N, playtest 43, CONTRATO_PARIDAD.md §2a/§2b) EL GANCHO REMOTO
        // =================================================================

        /// <summary>UsarPorRed ejecuta EXACTAMENTE la acción del E local -- ver el docblock de <see cref="IntentarEncender"/>.</summary>
        bool IMaquinaUsableRemota.UsarPorRed() => IntentarEncender();

        /// <summary>
        /// Empaqueta el estado que el Crisol YA tiene (nada nuevo que
        /// calcular, contrato §2b: "el estado ya existe internamente, solo
        /// empaquétalo"). Combina hornada activa, brasero con llama/brasas y
        /// resultado reposando -- los tres bits pueden estar activos a la
        /// vez (una hornada alimentada con el brasero ardiendo) o ninguno
        /// (reposo frío).
        /// </summary>
        byte IMaquinaUsableRemota.EstadoVivoRed()
        {
            byte b = 0;
            if (_fase == Fase.Corriendo) b |= EstadoVivoBits.Trabajando;
            if (_cestoArdiendo) b |= EstadoVivoBits.FuegoEncendido;
            if (_fase == Fase.Lista) b |= EstadoVivoBits.ResultadoListo;
            return b;
        }

        /// <summary>
        /// (playtest 39, contrato ENCARGO S 1b) LA TEATRO SINCRONIZADO: hasta
        /// esta ronda, "gastar" una celda de combustible era borrarla al
        /// instante (SetCell a Empty) -- el cesto nunca se veía arder de
        /// verdad, solo la llama pintada aparte por RefrescarLlamasBrasero.
        /// Ahora se ENCIENDE de verdad (Sim/SimStepper.EncenderCombustionPersistente):
        /// consume su propia reserva a su propio ritmo y deja BRASA real
        /// cuando se agota, en vez de desaparecer sin rastro. LA AUTORIDAD
        /// del resultado químico SIGUE SIENDO DecidirHornada -- esto no
        /// cambia qué transforma la hornada ni cuándo cierra (ese reloj es
        /// completamente independiente de cuánto tarde la celda en
        /// consumirse de verdad), solo lo que se VE en el cesto mientras
        /// tanto. Si el material del cesto no tiene parámetros de combustión
        /// persistente (Universe.EsCombustible no lo marcó esta seed, o es
        /// un combustible legado), cae al borrado instantáneo de siempre.
        /// </summary>
        private void ConsumirUnaCeldaDeCombustible()
        {
            var grid = _sim.Grid;
            var stepper = _sim.Stepper;
            uint tick = stepper != null ? stepper.Tick : 0u;
            for (int y = _braY0; y <= _braY1; y++)
            {
                for (int x = _braX0; x <= _braX1; x++)
                {
                    if (grid.GetMat(x, y) != _fuelMat) continue;
                    // Ya ardiendo de una hornada anterior (la duración de
                    // combustión real puede superar los HornadaSegundos de la
                    // abstracción): busca otra celda en vez de re-encenderla
                    // o borrarla a medio consumir.
                    if (stepper != null && stepper.EstaCombustionActiva(x, y)) continue;
                    if (stepper != null && stepper.EncenderCombustionPersistente(x, y)) return;

                    grid.SetCell(x, y, MaterialId.Empty);
                    grid.WakeChunk(x, y, tick);
                    return;
                }
            }
        }

        /// <summary>
        /// LA REGLA DE UNA SOLA TRANSFORMACIÓN. Dado el material dominante y
        /// la temperatura que esta pasada puede alcanzar, decide QUÉ va a
        /// pasar -- una cosa, decidida antes de empezar y ya inmutable. Si
        /// devuelve false, no hay hornada posible y el rótulo lo dice.
        /// </summary>
        private bool DecidirHornada(Universe universe, byte entrada, byte cima,
            out byte salida, out string condicion, out string verbo, out byte objetivo)
        {
            salida = MaterialId.Empty; condicion = null; verbo = null; objetivo = cima;

            // --- LIMO: extracción por temperatura, UNA base por hornada ---
            if (entrada == MaterialId.Limo)
            {
                int elegida = -1;
                for (int b = 0; b < MaterialId.BasesCount; b++)
                    if (universe.ExtraccionRaw(b) <= cima && (elegida < 0 || universe.ExtraccionRaw(b) > universe.ExtraccionRaw(elegida)))
                        elegida = b;
                if (elegida < 0) return false; // no debería pasar: el solver garantiza una banda por debajo de tier0.

                salida = MaterialId.MatDe(elegida, EstadoMateria.Polvo);
                condicion = CondicionCalor();
                verbo = "extrayendo";
                return true;
            }

            // --- AGUA: hierve directo a vapor (playtest 41, CONTRATO_VAPOR.md
            // 1a). MaterialId.Water es VOCABULARIO fijo (id=3), no un id del
            // bloque bases×estado -- EsBaseEstado(Water) es false, así que sin
            // esta rama el corte de justo abajo la descartaba SIEMPRE y
            // "encender" con agua en la cámara no hacía nada (la causa raíz
            // exacta del diagnóstico §0.1 del contrato: "no entendí por qué no
            // pude hervir el agua"). Con CrisolTier0Raw=120 por encima de todo
            // boilsAt posible del agua en cualquier seed (raw 100..119, ver
            // Universe.waterBoilC/CrisolTier0Raw), el fuego más flojo del
            // crisol YA hierve agua siempre; el chequeo de umbral de abajo es
            // por ROBUSTEZ (combustible más frío que tier0, o un futuro
            // override que baje `cima`), no porque vaya a fallar hoy.
            if (entrada == MaterialId.Water)
            {
                if (cima < universe.Get(MaterialId.Water).boilsAt) return false;
                salida = MaterialId.Steam;
                condicion = CondicionCalor();
                verbo = "hirviendo";
                return true;
            }

            if (!MaterialId.EsBaseEstado(entrada)) return false;

            int baseIdx = MaterialId.BaseDe(entrada);
            switch (MaterialId.EstadoDe(entrada))
            {
                case EstadoMateria.Polvo:
                    if (cima >= universe.FusionRaw(baseIdx))
                    {
                        salida = MaterialId.MatDe(baseIdx, EstadoMateria.Fundido);
                        condicion = CondicionCalor(); verbo = "fundiendo";
                        return true;
                    }
                    if (cima >= universe.CalcinacionRaw(baseIdx))
                    {
                        // -----------------------------------------------------
                        // (playtest 40, SEMILLA CERO, CONTRATO_SEMILLA.md §3
                        // override 2 / DISENO_SEMILLA_CERO.md enmienda 2) LA
                        // TRAMPA DEL BEAT 4: si el fuego SUPERA el techo de
                        // persistencia real del Calcinado de esta base
                        // (Universe.UmbralPersistenciaRaw, LA MISMA tabla que
                        // ya usan el Ensayo y el solver -- no un número nuevo),
                        // el resultado no es "calcinado a medio camino": es
                        // CENIZA. Leído de la tabla, no de un `if` que compare
                        // contra un ID de base concreto -- por eso esto NO
                        // cambia nada en modo caótico (el gate es el flag, no
                        // el material): en cualquier seed normal el hueco
                        // entre CalcinacionRaw y ese umbral es ancho a
                        // propósito (CalcinadoUmbral = FusionRaw+15..30, ver
                        // Universe.SortearTablaPersistencia) y ningún
                        // combustible de la tabla (165..190 raw) lo cruza
                        // jamás; solo Semilla Cero estrecha esa banda a mano
                        // para UNA base (Universe.AplicarOverridesSemillaCero).
                        // -----------------------------------------------------
                        if (AlkahestGameBootstrap.ModoSemillaCero
                            && cima >= universe.UmbralPersistenciaRaw(MaterialId.MatDe(baseIdx, EstadoMateria.Calcinado)))
                        {
                            salida = MaterialId.Ash;
                            condicion = CondicionCalor(); verbo = "calcinando"; // el jugador solo lo sabe al ver ceniza en la cubeta -- regla 54 de CLAUDE.md, "el fracaso es un experimento que salió con datos".
                            objetivo = cima;
                            return true;
                        }

                        salida = MaterialId.MatDe(baseIdx, EstadoMateria.Calcinado);
                        condicion = CondicionCalor(); verbo = "calcinando";
                        // Se calcina POR DEBAJO de la fusión: si el fuego da de
                        // sobra, el objetivo se queda a medio camino de la banda
                        // en vez de pasarse (y fundirlo, que sería otra cosa).
                        objetivo = (byte)Mathf.Min(cima, Mathf.Max(universe.CalcinacionRaw(baseIdx), universe.FusionRaw(baseIdx) - 4));
                        return true;
                    }
                    return false;

                case EstadoMateria.Compacto:
                    byte ceramiza = universe.CeramizaRaw(baseIdx);
                    if (ceramiza == 0 || cima < ceramiza) return false;
                    salida = MaterialId.MatDe(baseIdx, EstadoMateria.Ceramico);
                    condicion = CondicionCalor(); verbo = "ceramizando";
                    return true;

                case EstadoMateria.Fundido:
                    // RECOCER: la hornada de enfriado lento. No necesita
                    // combustible -- de hecho el fuego sobra: lo que hace el
                    // crisol es SOSTENER la pieza justo por encima de su punto
                    // de solidificación mientras se ordena por dentro.
                    salida = MaterialId.MatDe(baseIdx, EstadoMateria.Recocido);
                    condicion = "recocido lento"; verbo = "recociendo";
                    objetivo = (byte)Mathf.Min(255, universe.SolidificaRaw(baseIdx) + MargenRecocido);
                    return true;

                case EstadoMateria.Solucion:
                    byte evapora = universe.UmbralPersistenciaRaw(entrada); // == el punto de ebullición de su disolvente.
                    if (cima < evapora) return false;
                    salida = MaterialId.MatDe(baseIdx, EstadoMateria.Polvo);
                    condicion = CondicionCalor(); verbo = "evaporando";
                    return true;

                default:
                    return false; // Templado/Recocido/Calcinado/Cerámico: el crisol ya no puede hacerles nada. Eso es información.
            }
        }

        /// <summary>
        /// El techo térmico que impide que <c>SimStepper.ApplyPhase</c>
        /// transforme la carga ANTES de que acabe la pasada -- lo que mataba
        /// el "ritmo visible". Es el umbral que el MUNDO usaría sobre el
        /// material de entrada, menos un margen; si el mundo no tiene nada
        /// que decir sobre ese material, no hay techo (255).
        /// </summary>
        private byte TechoSeguroPara(Universe universe, byte entrada, byte cima)
        {
            if (!MaterialId.EsBaseEstado(entrada)) return 255;
            int baseIdx = MaterialId.BaseDe(entrada);
            if (MaterialId.EstadoDe(entrada) == EstadoMateria.Polvo)
            {
                int techo = universe.FusionRaw(baseIdx) - MargenTecho;
                return (byte)Mathf.Clamp(Mathf.Min(cima, techo), 0, 255);
            }
            return 255;
        }

        /// <summary>
        /// La temperatura a la que el crisol MANTIENE el resultado mientras
        /// reposa (mandato 4: "el resultado queda en la cubeta, INTOCADO,
        /// hasta que el jugador lo recoge"). Se elige para que el mundo no
        /// pueda transformar lo que acaba de salir: por debajo de su fusión
        /// si es un sólido, por encima de su solidificación si es un fundido.
        /// </summary>
        private byte TempReposoPara(Universe universe, byte salida)
        {
            if (!MaterialId.EsBaseEstado(salida)) return CellGrid.AmbientRaw;
            int baseIdx = MaterialId.BaseDe(salida);
            if (MaterialId.EstadoDe(salida) == EstadoMateria.Fundido)
                return (byte)Mathf.Min(255, universe.SolidificaRaw(baseIdx) + MargenRecocido * 2);
            byte reposo = (byte)Mathf.Max(CellGrid.AmbientRaw, universe.FusionRaw(baseIdx) - 12);
            // (playtest 40, SEMILLA CERO) En toda seed NATURAL, CalcinadoUmbral
            // = FusionRaw+15..30 por construcción del solver (contrato 4.2), así
            // que `FusionRaw-12` queda SIEMPRE muy por debajo del techo de
            // persistencia real del Calcinado y este segundo tope de abajo
            // nunca muerde nada (Min no-op). Semilla Cero rompe esa relación a
            // propósito para UNA base (banda de calcinación ESTRECHA, la
            // trampa del beat 4, ver Universe.AplicarOverridesSemillaCero):
            // sin este freno, un Calcinado recién salido de la hornada podría
            // quedar en reposo POR ENCIMA de su propio techo y quemarse solo a
            // Ash sin que el jugador tocara nada.
            if (MaterialId.EstadoDe(salida) == EstadoMateria.Calcinado)
            {
                int techo = universe.UmbralPersistenciaRaw(salida) - 15;
                if (techo < reposo) reposo = (byte)Mathf.Max(CellGrid.AmbientRaw, techo);
            }
            return reposo;
        }

        private void ActualizarObjetivoHornada()
        {
            float t = Mathf.Clamp01(_hornadaT / HornadaSegundos);
            byte cima = _hornadaCima;
            byte techo = _hornadaTecho;
            // Rampa visible: la temperatura SUBE durante toda la pasada (el
            // rescoldo se ve subir con ella) pero clampeada bajo el techo
            // seguro hasta el último tramo, cuando por fin se suelta.
            byte objetivoLibre = (byte)Mathf.RoundToInt(Mathf.Lerp(CellGrid.AmbientRaw, cima, Mathf.Min(1f, t / FraccionConTecho)));
            _targetRaw = t < FraccionConTecho ? (byte)Mathf.Min(objetivoLibre, techo) : cima;
        }

        /// <summary>
        /// (playtest 44, §2a del contrato) CONVERSIÓN POR FRENTES: mientras
        /// corre la hornada, las celdas de la cámara convierten
        /// INDIVIDUALMENTE en cuanto su fila cruza su propio umbral, no todas
        /// de golpe al cierre. Fila 0 = adyacente al hogar (<see cref="
        /// _camY0"/>, donde vive el calor de verdad); fila
        /// <c>CamaraAlto-1</c> = junto a la boca, la más lejos. El umbral de
        /// cada fila es un punto por debajo de <see cref="_hornadaCima"/>
        /// (ver <see cref="MargenFrenteFraccion"/>): la fila 0 lo cruza pronto (la
        /// rampa compartida de <see cref="ActualizarObjetivoHornada"/> llega
        /// ahí sin pelear con el techo de seguridad), la última fila exige la
        /// CIMA exacta, que solo se libera en el tramo final de la hornada
        /// (más allá de <see cref="FraccionConTecho"/>) -- el resultado es
        /// que el "tostado" se ve subir desde el fondo del puchero hacia
        /// arriba a lo largo de toda la pasada, no un chasquido al final.
        ///
        /// POR QUÉ UN UMBRAL POR FILA Y NO UN EMPUJE POR FILA: se modeló
        /// primero dar a cada fila su propio ritmo de EMPUJE (como hacen
        /// HeatPlate/ChillStone con <see cref="Sim.EmisionTermica"/>) y no
        /// separaba nada -- la rampa de la hornada sube ~0.36 raw/tick,
        /// demasiado lento para que cualquier diferencial de empuje
        /// razonable produzca más que ruido de redondeo entre filas en 300
        /// ticks; o las filas convergían todas a la vez, o alguna nunca
        /// llegaba. <see cref="EmpujarTemperatura"/> se queda SIN sesgo por
        /// fila a propósito (todas las celdas persiguen el mismo
        /// <see cref="_targetRaw"/>): es el UMBRAL de conversión, no la
        /// velocidad de calentar, el que decide en qué tick cruza cada fila.
        ///
        /// Muestreo barato y determinista (regla de oro de CLAUDE.md): solo
        /// 1 de cada 4 celdas se comprueba por tick (<see cref="
        /// MuestreoMascara"/>, patrón <c>(x+y+tick)&mascara</c>, el mismo
        /// truco de "tablero" que ya usa <see cref="Sim.SimStepper"/> para
        /// difusión), y de las muestreadas que ya cruzaron su umbral solo
        /// convierte <see cref="ProbabilidadConversionPct"/>% (salt propia
        /// <see cref="SalFrenteHornada"/>) -- sin esto el frente avanzaría en
        /// un barrido geométrico perfecto en vez de un chisporroteo. Cero
        /// asignaciones: <c>XorShift</c> es struct, el bucle es el mismo
        /// doble-for barato que <see cref="EmpujarTemperatura"/>.
        ///
        /// <see cref="CerrarHornada"/> sigue siendo la GARANTÍA: cualquier
        /// celda que no haya cruzado a tiempo (mala suerte del dado, o
        /// simplemente estar en el filo del umbral cuando se acaban los 300
        /// ticks) se convierte ahí, de golpe -- el jugador NUNCA ve una
        /// hornada "atascada" con materia sin convertir en la cubeta.
        /// </summary>
        private void ProcessConversionFrente()
        {
            if (CamaraAlto <= 1) return; // guarda: con una sola fila no hay frente que trazar (division por cero más abajo).
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            // Margen como FRACCIÓN del rango real (cima-ambiente), no un raw
            // fijo -- ver el docblock de MargenFrenteFraccion para el porqué
            // (un raw fijo se hunde por debajo del piso de 60 ticks con
            // hornadas calientes). Mathf.Max(0,...) es defensivo: si algún
            // día una hornada tuviera objetivo por debajo de ambiente (no
            // ocurre hoy), el margen se queda en 0 en vez de negativo.
            int rango = Mathf.Max(0, _hornadaCima - CellGrid.AmbientRaw);

            for (int y = _camY0; y <= _camY1; y++)
            {
                int fila = y - _camY0; // 0 = adyacente al hogar.
                float fracFila = (float)fila / (CamaraAlto - 1); // 0 en el hogar, 1 en la boca.
                int umbral = _hornadaCima - Mathf.RoundToInt(MargenFrenteFraccion * rango * (1f - fracFila));

                for (int x = _camX0; x <= _camX1; x++)
                {
                    if (((x + y + (int)tick) & MuestreoMascara) != 0) continue; // muestreo barato: 1 de cada 4 celdas por tick.

                    int idx = CellGrid.Idx(x, y);
                    if (grid.GetMat(idx) != _hornadaEntrada) continue;
                    if (grid.temp[idx] < umbral) continue; // esta fila aún no llegó a SU umbral.

                    var rng = XorShift.FromCell(tick, x, y, SalFrenteHornada);
                    if (!rng.ChancePercent(ProbabilidadConversionPct)) continue; // chisporroteo, no barrido perfecto.

                    grid.SetCell(idx, _hornadaSalida, resetAux: false);
                    grid.WakeChunk(x, y, tick);
                    _hornadaConvertidasAcumulado++;
                }
            }
        }

        private void CerrarHornada()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            int stragglers = 0;

            for (int y = _camY0; y <= _camY1; y++)
            {
                for (int x = _camX0; x <= _camX1; x++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.GetMat(idx) != _hornadaEntrada) continue;
                    grid.SetCell(idx, _hornadaSalida, resetAux: false);
                    grid.WakeChunk(x, y, tick);
                    stragglers++;
                }
            }

            // (playtest 44, §2a) CerrarHornada ya NO es la única fuente de
            // conversiones: ProcessConversionFrente lleva convirtiendo desde
            // el principio de la pasada. Esta barrida final es la GARANTÍA
            // (cualquier celda que quedó por debajo de su umbral de fila, o
            // que el dado de ProbabilidadConversionPct no tocó a tiempo) --
            // `stragglers` cuenta SOLO lo que esta pasada convierte de más;
            // el total real de la hornada es el acumulado del frente MÁS
            // estas rezagadas, y es ESE total el que ve el resto del método
            // (testigo forense, vapor, destrucción por hornada) -- exactamente
            // el mismo número que veía el jugador antes de esta ronda, solo
            // que ahora una parte ya se vio convertir en vivo.
            _hornadaConvertidasAcumulado += stragglers;
            int convertidas = _hornadaConvertidasAcumulado;

            if (convertidas > 0) Hornada.RegistrarOp("crisol", _hornadaEntrada, _hornadaSalida, _hornadaCondicion);

            // (integración pt40, SEMILLA CERO) Si la hornada DESTRUYÓ la
            // entrada a ceniza (la trampa del beat 4 -- y cualquier
            // sobrecalcinado futuro), el testigo forense lo anota: esta
            // transformación es de HORNADA, no de la CA, así que el canal
            // Boil de SubstanceKnowledge.ApplyWitness jamás la ve. La cima
            // real de la hornada es la temperatura que mató la muestra.
            if (convertidas > 0 && _hornadaSalida == MaterialId.Ash && _conocimiento != null)
                _conocimiento.RegistrarDestruccionPorHornada(_hornadaEntrada, _hornadaCima);

            // LA ALQUIMIA VISIBLE (tarea 2, encargo de Cesar: "evaporar
            // cosas; ver algo diluirse en agua"). "extrayendo" (Limo ->
            // arena, hierve para separar) y "evaporando" (Solución -> Polvo,
            // el agua deja la mezcla) son las DOS hornadas de esta pasada en
            // las que agua de verdad abandona la cámara -- justo lo que pide
            // el mandato. Las demás (fundiendo/calcinando/ceramizando/
            // recociendo) no llevan agua, así que no emiten nada.
            //
            // "hirviendo" (playtest 41, CONTRATO_VAPOR.md 1a) se deja FUERA a
            // propósito, decisión verificada y documentada aquí: en
            // extrayendo/evaporando el agua es solo una FRACCIÓN de lo que
            // había en la cámara (el resto se queda como arena/polvo), así
            // que el empujón de EmitirVaporCubeta es lo único que hace visible
            // que algo de agua se fue. En "hirviendo" el bucle de arriba YA
            // convirtió el 100% de la cámara a Steam real -- empujar más
            // Steam encima solo duplicaría vapor sobre vapor (y, peor, sobre
            // celdas que a menudo ya NO están vacías porque son la propia
            // cámara ahora llena de Steam), sin añadir nada que el jugador no
            // vea ya con el agua entera dejando la olla.
            if (convertidas > 0 && (_hornadaVerbo == "extrayendo" || _hornadaVerbo == "evaporando"))
                EmitirVaporCubeta(convertidas);

            _fase = Fase.Lista;
            _cestoArdiendo = false;
            _reposoRaw = TempReposoPara(_sim.Universe, _hornadaSalida);
            _targetRaw = _reposoRaw;
            Rotular(null, UiStyles.Exito);
        }

        /// <summary>
        /// LA ALQUIMIA VISIBLE (tarea 2). Empuja <see cref="VaporPorCeldas"/>-avo
        /// de las celdas convertidas como <see cref="MaterialId.Steam"/> justo
        /// sobre la cámara (dentro de la boca embudada, aire ya tallado --
        /// ver <see cref="TallarEnPlano"/>), para que el ojo del jugador
        /// atrape "el agua se está yendo" en el mismo instante en que la
        /// hornada cierra, en vez de tener que esperar a que el Steam que ya
        /// nace solo (Sim/SimStepper.cs, arquetipo Gas) se abra paso él solo
        /// por la cámara. <see cref="AlkahestSim.PaintStable"/>, no
        /// <see cref="AlkahestSim.Paint"/> (regla 29): esto CREA vapor de la
        /// nada, no mueve uno que ya existiera.
        /// </summary>
        private void EmitirVaporCubeta(int convertidas)
        {
            int n = Mathf.Min(convertidas / VaporPorCeldas, CamaraAncho);
            if (n <= 0) return;

            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            int y = _bocaY0; // primera fila de aire justo sobre la cámara -- ya tallada a Empty por la boca embudada.

            for (int i = 0; i < n; i++)
            {
                var rng = XorShift.FromCell(tick, _camX0, _camY1, SalVaporCrisol + (uint)i);
                int x = _camX0 + rng.Next(CamaraAncho);
                if (grid.GetMat(x, y) != MaterialId.Empty) continue; // no desplazar nada -- si ya hay algo ahí, esta bocanada se pierde (determinista: la próxima hornada lo intentará de nuevo).
                _sim.PaintStable(x, y, 0, MaterialId.Steam);
                grid.WakeChunk(x, y, tick);
            }
        }

        /// <summary>Empuja la temperatura de la cámara hacia <see cref="_targetRaw"/>. Con `_targetRaw` a 0 (REPOSO) no toca NADA: sin ese silencio no habría "una transformación por hornada".</summary>
        /// <summary>
        /// (playtest 44, escritura migrada a <see cref="AlkahestSim.
        /// InyectarTemperatura"/>, Paint discipline) Empuja la temperatura de
        /// la cámara hacia <see cref="_targetRaw"/> con paso FIJO
        /// (<see cref="TempStepPerTick"/>), SIN sesgo por fila -- a
        /// diferencia de HeatPlate/ChillStone, aquí NO hace falta el modelo
        /// de <see cref="Sim.EmisionTermica"/>: el crisol es una cámara
        /// SELLADA por los cuatro costados (mampostería, ver
        /// <see cref="TallarEnPlano"/>), no un footprint irradiando a campo
        /// abierto, así que no hay fuga de largo alcance que contener con un
        /// collar. El paso fijo (en vez de Newton) es DELIBERADO: es lo que
        /// hace que todas las celdas persigan el MISMO <c>_targetRaw</c> casi
        /// en fase, para que sea el UMBRAL por fila de
        /// <see cref="ProcessConversionFrente"/> -- no la velocidad de
        /// calentar -- quien decida el orden del frente (ver el docblock de
        /// ese método).
        /// </summary>
        private void EmpujarTemperatura()
        {
            if (_targetRaw == 0) return;
            var grid = _sim.Grid;
            int target = _targetRaw;

            for (int y = _camY0; y <= _camY1; y++)
            {
                for (int x = _camX0; x <= _camX1; x++)
                {
                    if (!CellGrid.InBounds(x, y)) continue;
                    int idx = CellGrid.Idx(x, y);
                    if (grid.GetMat(idx) == MaterialId.Empty) continue; // el aire no se calienta: lo que arde es la carga.
                    int cur = grid.temp[idx];
                    int next = cur < target ? Mathf.Min(target, cur + TempStepPerTick) : Mathf.Max(target, cur - TempStepPerTick);
                    _sim.InyectarTemperatura(x, y, (byte)next);
                }
            }
        }

        private string CondicionCalor()
        {
            if (_fuelMat == MaterialId.Empty) return "fuego bajo";
            string nombre = _conocimiento != null ? _conocimiento.NombreParaHud(_fuelMat) : "???";
            return "combustible:" + nombre;
        }

        // =================================================================
        // VISUAL
        // =================================================================
        private void BuildVisual()
        {
            float c = SimRenderer.CellWorldSize;

            // ---- El cuerpo: panza de hierro sobre los muros REALES de la
            // cámara, con el hueco recortado a transparente para que se vea
            // dentro (ver la nota de MaquinariaSprites: un sprite de máquina
            // no puede tapar su propia cámara).
            // (segunda pasada) El sprite ABOMBA VueloCuerpo celdas por fuera
            // de la piedra y recorta su cámara con `muroDibujado`, así que la
            // transparencia sigue cayendo EXACTAMENTE sobre el hueco real.
            int muroDibujado = MuroGrosor + VueloCuerpo;             // 5
            int spanCuerpo = CamaraAncho + 2 * muroDibujado;         // 23
            int altoCuerpo = CamaraAlto + 2;                         // 11
            float anchoCuerpo = spanCuerpo * c, altoCuerpoW = altoCuerpo * c;
            Vector3 posCuerpo = new Vector3((_camX0 + CamaraAncho * 0.5f) * c, (_baseY + altoCuerpo * 0.5f) * c, 0f);

            var cuerpoGo = new GameObject("CrisolCuerpo");
            cuerpoGo.transform.SetParent(transform, false);
            cuerpoGo.transform.position = posCuerpo;

            var panza = MaquinariaSprites.PanzaCrisol(spanCuerpo, altoCuerpo, muroDibujado, 1);

            _resalte = MaquinariaSprites.CrearCapa(cuerpoGo.transform, "Resalte", panza, 14, anchoCuerpo * 1.10f, altoCuerpoW * 1.14f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);
            _latidoTrabajo = MaquinariaSprites.CrearCapa(cuerpoGo.transform, "LatidoTrabajo", panza, 15, anchoCuerpo * 1.06f, altoCuerpoW * 1.08f);
            _latidoTrabajo.color = new Color(1f, 0.55f, 0.18f, 0f);
            MaquinariaSprites.CrearCapa(cuerpoGo.transform, "Panza", panza, 18, anchoCuerpo, altoCuerpoW);
            _destelloCamara = MaquinariaSprites.CrearCapa(cuerpoGo.transform, "AcuseCamara",
                MaquinariaSprites.MarcoBandeja(spanCuerpo, altoCuerpo), 22, anchoCuerpo, altoCuerpoW);
            _destelloCamara.color = new Color(1f, 1f, 0.9f, 0f);

            // ---- EL FUEGO, DEBAJO DEL PUCHERO. Es la imagen que todo el
            // mundo entiende sin que nadie se la explique, y es lo que hace
            // que "fuego bajo" en el rótulo sea una descripción y no una
            // promesa.
            var hogarGo = new GameObject("CrisolHogar");
            hogarGo.transform.SetParent(transform, false);
            hogarGo.transform.position = new Vector3(posCuerpo.x, (_baseY - HogarFilas * 0.5f) * c, 0f);
            _brasasHogar = MaquinariaSprites.CrearCapa(hogarGo.transform, "Brasas",
                MaquinariaSprites.LechoBrasas(CamaraAncho, HogarFilas), 17, CamaraAncho * c, HogarFilas * c);

            // ---- LA BOCA: guías de latón forrando la rampa de piedra (una
            // por fila y lado: 22 tejas pequeñas, creadas una vez) + el labio
            // que la corona. Sin ellas la boca embudada es piedra sobre
            // piedra y se pierde contra la roca del fondo.
            var bocaGo = new GameObject("CrisolBoca");
            bocaGo.transform.SetParent(transform, false);
            bocaGo.transform.position = Vector3.zero;
            var teja = MaquinariaSprites.Solido();
            for (int i = 0; i < BocaFilas; i++)
            {
                int y = _bocaY0 + i;
                int vuelo = VueloEnFila(i);
                Color tono = (i % 2 == 0) ? new Color(0.76f, 0.59f, 0.29f, 1f) : new Color(0.55f, 0.42f, 0.20f, 1f);
                var izq = MaquinariaSprites.CrearCapa(bocaGo.transform, "GuiaIzq" + i, teja, 19, 1f * c, 1f * c);
                izq.transform.position = new Vector3((_camX0 - vuelo - 0.5f) * c, (y + 0.5f) * c, 0f);
                izq.color = tono;
                var der = MaquinariaSprites.CrearCapa(bocaGo.transform, "GuiaDer" + i, teja, 19, 1f * c, 1f * c);
                der.transform.position = new Vector3((_camX1 + vuelo + 1.5f) * c, (y + 0.5f) * c, 0f);
                der.color = tono * 0.8f;
            }
            int spanLabio = CamaraAncho + 2 * BocaVuelo + 2 * MuroGrosor; // 29
            var labioGo = new GameObject("CrisolLabio");
            labioGo.transform.SetParent(transform, false);
            labioGo.transform.position = new Vector3(_centroCamara.x, (_bocaY1 + 1f) * c, 0f);
            MaquinariaSprites.CrearCapa(labioGo.transform, "Sprite", MaquinariaSprites.LabioBoca(spanLabio, 3), 20, spanLabio * c, 3f * c);

            // ---- Chimenea + bocanadas (solo mientras arde combustible).
            var chimeneaGo = new GameObject("CrisolChimenea");
            chimeneaGo.transform.SetParent(transform, false);
            chimeneaGo.transform.position = new Vector3((_camX1 + BocaVuelo - 0.5f) * c, (_bocaY1 + 6f) * c, 0f);
            MaquinariaSprites.CrearCapa(chimeneaGo.transform, "Sprite", MaquinariaSprites.Chimenea(3), 19, 3f * c, 10f * c);
            for (int i = 0; i < HumoPuffs; i++)
            {
                var humoGo = new GameObject("Humo" + i);
                humoGo.transform.SetParent(transform, false);
                var sr = MaquinariaSprites.CrearCapa(humoGo.transform, "Sprite", MaquinariaSprites.Humo(), 23, 3f * c, 3f * c);
                sr.color = new Color(0.82f, 0.80f, 0.78f, 0f);
                _humo[i] = sr;
            }

            // ---- Burbujas dentro de la cámara mientras corre la hornada.
            for (int i = 0; i < Burbujas; i++)
            {
                var bgo = new GameObject("Burbuja" + i);
                bgo.transform.SetParent(transform, false);
                var sr = MaquinariaSprites.CrearCapa(bgo.transform, "Sprite", MaquinariaSprites.Burbuja(), 21, 1.2f * c, 1.2f * c);
                sr.color = new Color(1f, 0.9f, 0.7f, 0f);
                _burbujas[i] = sr;
            }

            // ---- EL CESTO DEL BRASERO, aparte y con otra silueta.
            int muroCesto = MuroGrosor + VueloCesto;       // 4
            int spanCesto = BraseroAncho + 2 * muroCesto;  // 13
            int altoCesto = BraseroAlto + 2;               // 8
            float anchoCestoW = spanCesto * c, altoCestoW = altoCesto * c;
            var cestoGo = new GameObject("CrisolBrasero");
            cestoGo.transform.SetParent(transform, false);
            cestoGo.transform.position = new Vector3((_braX0 + BraseroAncho * 0.5f) * c, (_baseY + altoCesto * 0.5f) * c, 0f);
            MaquinariaSprites.CrearCapa(cestoGo.transform, "Cesto",
                MaquinariaSprites.CestoBrasero(spanCesto, altoCesto, muroCesto, 1), 18, anchoCestoW, altoCestoW);
            _destelloCesto = MaquinariaSprites.CrearCapa(cestoGo.transform, "AcuseCesto",
                MaquinariaSprites.MarcoBandeja(spanCesto, altoCesto), 22, anchoCestoW, altoCestoW);
            _destelloCesto.color = new Color(1f, 0.85f, 0.6f, 0f);

            var cestoHogarGo = new GameObject("CrisolBraseroHogar");
            cestoHogarGo.transform.SetParent(transform, false);
            cestoHogarGo.transform.position = new Vector3((_braX0 + BraseroAncho * 0.5f) * c, (_baseY - HogarFilas * 0.5f) * c, 0f);
            _brasasCesto = MaquinariaSprites.CrearCapa(cestoHogarGo.transform, "Brasas",
                MaquinariaSprites.LechoBrasas(BraseroAncho, HogarFilas), 17, BraseroAncho * c, HogarFilas * c);
            _brasasCesto.color = new Color(0.16f, 0.14f, 0.13f, 1f); // ARRANCA APAGADO (mandato 4): frío y vacío.

            // ---- (playtest 31) SOMBRA PROPIA: la panza y el cesto se APOYAN
            // en la piedra. Sin esto los sprites flotan sobre el suelo, que es
            // la mitad del "programmer art" que Cesar señaló.
            MaquinariaSprites.Sombra(transform,
                new Vector3(posCuerpo.x, (_baseY - HogarFilas - 0.4f) * c, 0f),
                anchoCuerpo * 1.25f, 4.5f * c, 0.42f);
            MaquinariaSprites.Sombra(transform,
                new Vector3((_braX0 + BraseroAncho * 0.5f) * c, (_baseY - HogarFilas - 0.4f) * c, 0f),
                anchoCestoW * 1.2f, 3.5f * c, 0.38f);

            // =============================================================
            // (playtest 33) LA LUZ DEJA DE SER UN STICKER
            // =============================================================
            // Cesar, sobre la luz del 31/32: *"la LUZ sobre el horno no es
            // mala pero se ve OMNIPRESENTE, parece PEGADA EN LA PANTALLA, no
            // se siente parte del horno: ajusta qué puede alumbrar y qué no
            // con criterio -- quizás el contenedor de paredes brillando sin
            // incluir el techo porque no tiene -- que no parezca un STICKER
            // pegado"*.
            //
            // EL DIAGNÓSTICO EXACTO (medido sobre los valores viejos, no a
            // ojo): `_luzHogar` era un disco de **46 celdas de diámetro**
            // centrado a media altura del hogar. La huella COMPLETA del
            // Crisol mide 37x24. O sea que el halo desbordaba la máquina por
            // los cuatro lados -- 11 celdas de naranja a cada lado sobre la
            // piedra desnuda del suelo, y 11 por encima de la boca, sobre
            // AIRE. Con la caída 2.2 de aquel `Halo()` eso todavía llevaba un
            // 6% de alfa a 17 celdas del centro. Un disco así, quieto, que
            // cubre más que su propio objeto, es exactamente lo que el ojo
            // clasifica como "capa pegada al cuadro".
            //
            // EL CRITERIO NUEVO, en tres piezas y una regla:
            //   REGLA: nada se ilumina si no hay una superficie física ahí
            //   que pudiera recibir esa luz.
            //   1) LUZ DE MURO (`_luzMuro`, la pieza nueva): un rectángulo
            //      que cubre EXACTAMENTE la mampostería del horno
            //      (`_outX0.._outX1` x `hogar..bocaY1`) y se apaga a cero en
            //      su borde de arriba -- ver MaquinariaSprites.LuzDeMuro.
            //      Es lo que Cesar describió con sus palabras ("el contenedor
            //      de paredes brillando sin incluir el techo"): el cuerpo del
            //      horno se pone al rojo desde dentro y el aire de encima
            //      queda oscuro.
            //   2) BOCA DE FUEGO (`_luzHogar`): 46 -> **15** celdas. Deja de
            //      ser "la luz de la sala" y pasa a ser el rescoldo que se ve
            //      por la boca del hogar: no llega ni al borde de la panza.
            //   3) CÁMARA (`_luzCamara`): de (CamaraAncho+8) x (CamaraAlto+6)
            //      = 21x15 a **(CamaraAncho-2) x (CamaraAlto-1)** = 11x8, o
            //      sea DENTRO de la cámara. Lo que está al rojo es la carga,
            //      y la carga está ahí dentro; que se derramara luz por
            //      encima del labio era el mismo error en pequeño.
            //   4) BRASERO (`_luzBrasero`): 30 -> **11**, más su propia luz
            //      de muro sobre el cesto (`_luzMuroCesto`).
            // Y, transversal a todo, la caída del sprite radial compartido
            // pasó de 2.2 a 3.6 (MaquinariaSprites.Halo, ver su docblock):
            // eso arregla de paso la lámpara del Banco de Chispa y los
            // destellos de las redomas, que Cesar citó por su nombre.
            // =============================================================
            int muroSpan = _outX1 - _outX0 + 1;                 // 37 celdas: la huella real, no un número a ojo.
            int muroY0 = _baseY - HogarFilas;                   // la primera fila del hogar, bajo la panza.
            int muroY1 = _bocaY1;                               // LA CORNISA: por encima de aquí no hay horno que alumbrar.
            int muroAlto = muroY1 - muroY0 + 1;
            _luzMuro = MaquinariaSprites.Luz.CrearMuro(transform, "LuzMuroCrisol",
                new Vector3(posCuerpo.x, (muroY0 + muroAlto * 0.5f) * c, 0f),
                muroSpan, muroAlto, muroSpan * c, muroAlto * c,
                new Color(1f, 0.50f, 0.19f), sesgoAbajo: 0.70f);

            _luzHogar = MaquinariaSprites.Luz.Crear(transform, "LuzHogar",
                new Vector3(posCuerpo.x, (_baseY - HogarFilas * 0.5f) * c, 0f),
                15f * c, new Color(1f, 0.56f, 0.22f));
            _luzCamara = MaquinariaSprites.Luz.CrearOvalada(transform, "LuzCamara",
                new Vector3(posCuerpo.x, (_baseY + CamaraAlto * 0.45f) * c, 0f),
                (CamaraAncho - 2) * c, (CamaraAlto - 1) * c, new Color(1f, 0.72f, 0.36f));
            _luzBrasero = MaquinariaSprites.Luz.Crear(transform, "LuzBrasero",
                new Vector3((_braX0 + BraseroAncho * 0.5f) * c, (_baseY + BraseroAlto * 0.35f) * c, 0f),
                11f * c, new Color(1f, 0.48f, 0.16f));

            int cestoSpan = BraseroAncho + 2 * muroCesto;       // 13, el mismo que ya usa el sprite del cesto arriba.
            int cestoY0 = _baseY - HogarFilas;
            int cestoY1 = _baseY + BraseroAlto + 1;             // el labio del cesto: su cornisa.
            int cestoAlto = cestoY1 - cestoY0 + 1;
            _luzMuroCesto = MaquinariaSprites.Luz.CrearMuro(transform, "LuzMuroBrasero",
                new Vector3((_braX0 + BraseroAncho * 0.5f) * c, (cestoY0 + cestoAlto * 0.5f) * c, 0f),
                cestoSpan, cestoAlto, cestoSpan * c, cestoAlto * c,
                new Color(1f, 0.44f, 0.14f), sesgoAbajo: 0.78f);
        }

        private void ActualizarVisual()
        {
            float c = SimRenderer.CellWorldSize;
            bool corriendo = _fase == Fase.Corriendo;
            float t = corriendo ? Mathf.Clamp01(_hornadaT / HornadaSegundos) : 0f;

            // El hogar: rescoldo tenue en reposo, sube con la hornada.
            if (_brasasHogar != null)
            {
                float pulso = 0.82f + 0.18f * Mathf.Sin(Time.time * (corriendo ? 6.5f : 2.0f));
                // (segunda pasada) EN FRÍO, CASI NEGRO. Con 0.20, el hogar
                // apagado seguía siendo un puñado de puntos rojos, y un fuego
                // que se ve encendido cuando NO lo está miente sobre el estado
                // de la máquina -- el mismo pecado que el "cargadme
                // combustible" del 26. 0.06 deja un rescoldo que solo se
                // adivina de cerca.
                float intensidad = corriendo ? Mathf.Lerp(0.30f, 1f, t) : (_fase == Fase.Lista ? 0.18f : 0.06f);
                // TERCERA PASADA (visto jugando otra vez): el color se
                // interpola DESDE EL CARBÓN APAGADO, no desde una base
                // naranja. La fórmula anterior partía de r=0.55 aunque la
                // intensidad fuese 0.06, así que el hogar frío seguía siendo
                // un puñado de ascuas rojas -- o sea que la máquina seguía
                // diciendo "estoy encendida" cuando no lo estaba, que es
                // exactamente el pecado que esta ronda vino a corregir.
                Color carbon = new Color(0.17f, 0.13f, 0.11f);
                Color fuego = new Color(1f, 0.55f, 0.18f);
                Color mezcla = Color.Lerp(carbon, fuego, intensidad);
                _brasasHogar.color = new Color(mezcla.r * pulso, mezcla.g * pulso, mezcla.b * pulso, 1f);
            }

            // El cesto: negro mientras no arda; blanco-naranja mientras arde.
            if (_brasasCesto != null)
            {
                if (_cestoArdiendo)
                {
                    float p = 0.8f + 0.2f * Mathf.Sin(Time.time * 9f);
                    _brasasCesto.color = new Color(1f, 0.62f * p, 0.24f * p, 1f);
                }
                else _brasasCesto.color = new Color(0.16f, 0.14f, 0.13f, 1f);
            }

            // (playtest 31) LAS LUCES SIGUEN AL FUEGO, no al revés: la misma
            // `intensidad` que decide el color de las brasas decide cuánta
            // luz sale de ellas, así que es IMPOSIBLE que el halo diga
            // "encendido" mientras el hogar está negro (el error que la
            // tercera pasada del playtest 27 corrigió en las brasas: no
            // reintroducirlo por la puerta de la luz).
            {
                float intensidadHogar = corriendo ? Mathf.Lerp(0.30f, 1f, t) : (_fase == Fase.Lista ? 0.18f : 0.06f);
                // (segunda pasada, visto jugando) el rescoldo en reposo daba
                // 0.068 de alfa: invisible. Un horno apagado del todo tampoco
                // sería honesto -- SÍ tiene rescoldo (las brasas se dibujan a
                // 0.06 de intensidad, no a 0) -- así que la luz arranca en
                // 0.13 y sube a ~0.50 con la hornada al rojo.
                _luzHogar?.Latir(0.13f + 0.46f * intensidadHogar, 0.03f + 0.07f * intensidadHogar, 0.85f);
                // La cámara sólo brilla mientras cocina: es la carga la que
                // está al rojo, y en reposo no hay nada al rojo dentro.
                _luzCamara?.Intensidad(corriendo ? Mathf.Lerp(0.12f, 0.40f, t) : (_fase == Fase.Lista ? 0.16f : 0f));
                // El brasero: la luz más viva del taller, y con otro ritmo
                // (1.7 Hz frente a 0.85) para que las dos llamas nunca
                // respiren a la vez.
                if (_cestoArdiendo) _luzBrasero?.Latir(0.46f, 0.12f, 1.7f, 0.37f);
                else _luzBrasero?.Intensidad(0f);

                // (playtest 33) LA MAMPOSTERÍA DEL PROPIO HORNO. Es la luz que
                // sustituye al disco gigante, y la que de verdad cuenta el
                // estado: se alimenta de la MISMA `intensidadHogar` que las
                // brasas (imposible que la piedra diga "al rojo" con el hogar
                // negro -- la lección del playtest 27 que el 31 ya respetaba,
                // aquí extendida al muro), y suma un plus cuando el brasero
                // arde de verdad, porque entonces hay DOS fuegos calentando la
                // misma piedra.
                //   · rescoldo tenue (fase Vacio, intensidad 0.06) -> 0.10
                //   · resultado listo reposando (0.18)             -> 0.15
                //   · hornada plena (1.0)                          -> 0.44
                //   · + brasero ardiendo                           -> +0.16
                // Latido MUY lento (0.55 Hz) y de poca amplitud: la piedra
                // tiene inercia térmica, no parpadea como una llama.
                float muro = 0.06f + 0.38f * intensidadHogar + (_cestoArdiendo ? 0.16f : 0f);
                _luzMuro?.Latir(muro, 0.018f + 0.03f * intensidadHogar, 0.55f, 0.13f);
                if (_cestoArdiendo) _luzMuroCesto?.Latir(0.34f, 0.06f, 1.25f, 0.61f);
                else _luzMuroCesto?.Intensidad(0.05f); // el cesto frío guarda un rescoldo mínimo: apagarlo del todo lo desprende visualmente del horno.
            }

            if (_latidoTrabajo != null)
                _latidoTrabajo.color = new Color(1f, 0.55f, 0.18f, _pulsoTrabajo.AlfaTrabajo * 0.55f);
            if (_destelloCamara != null)
                _destelloCamara.color = new Color(1f, 1f, 0.9f, _acuseCamara.Alfa);
            if (_destelloCesto != null)
                _destelloCesto.color = new Color(1f, 0.85f, 0.6f, _acuseCesto.Alfa);

            // Burbujas: solo mientras corre, subiendo por la cámara.
            for (int i = 0; i < Burbujas; i++)
            {
                var sr = _burbujas[i];
                if (sr == null) continue;
                if (!corriendo) { sr.color = new Color(1f, 0.9f, 0.7f, 0f); continue; }
                float fase = Mathf.Repeat(Time.time * 0.55f + i / (float)Burbujas, 1f);
                float px = _camX0 + 1.5f + (CamaraAncho - 3f) * ((i * 2.7f) % 1f);
                float py = _camY0 + fase * (CamaraAlto - 1f);
                sr.transform.position = new Vector3(px * c, py * c, 0f);
                sr.color = new Color(1f, 0.92f, 0.74f, (1f - fase) * 0.85f * Mathf.Lerp(0.4f, 1f, t));
            }

            // Humo: solo mientras el cesto arde de verdad (el verbo en el cuerpo).
            // Ver el bloque de constantes HumoVariacion* arriba para el porqué
            // de cada término (playtest 41: de carrusel a acento).
            for (int i = 0; i < HumoPuffs; i++)
            {
                var sr = _humo[i];
                if (sr == null) continue;
                if (!_cestoArdiendo) { sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0f); continue; }

                // Periodo propio por bocanada: las cuatro dejan de coincidir.
                float fase = Mathf.Repeat(Time.time / (HumoCicloSeg * HumoVariacionPeriodo[i]) + i / (float)HumoPuffs, 1f);

                // Rizo (seno de amplitud propia) + DERIVA que crece con la
                // altura: la voluta se escora al alejarse del tubo en vez de
                // volver siempre al eje.
                float dx = Mathf.Sin(fase * Mathf.PI * 2f + i * 1.7f) * HumoVariacionRizo[i]
                         + fase * HumoVariacionDeriva[i];
                sr.transform.position = _humoOrigen + new Vector3(dx * c, fase * c * HumoVariacionAltura[i], 0f);
                sr.transform.localScale = Vector3.one * (0.5f + fase * 1.7f) * (c * 3f) / sr.sprite.rect.width;

                // Alfa: ENTRA (se hace visible al salir del tubo, mata el pop)
                // y SALE al cuadrado (se desvanece, no se corta).
                float restante = 1f - fase;
                float alfa = fase < HumoFraccionEntrada
                    ? (fase / HumoFraccionEntrada) * HumoAlfaPico
                    : restante * restante * HumoAlfaPico / ((1f - HumoFraccionEntrada) * (1f - HumoFraccionEntrada));
                sr.color = new Color(0.82f, 0.80f, 0.78f, alfa);
            }

            if (_resalte != null)
            {
                float objetivo = EstaEnfocada() ? 0.55f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
                _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
                _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
            }
        }

        // =================================================================
        // RÓTULOS (español latino, tuteo — mandato 6)
        // =================================================================
        private string _aviso;
        private Color _avisoColor = UiStyles.Aviso;
        private float _avisoHasta;

        private void Rotular(string texto, Color color)
        {
            _aviso = texto;
            _avisoColor = color;
            _avisoHasta = texto != null ? Time.time + 3.5f : 0f;
        }

        /// <summary>
        /// El rótulo de la CÁMARA. Prioridades, en orden:
        ///  1) un aviso reciente (te acabo de decir por qué no ha pasado nada);
        ///  2) la hornada en curso, con su verbo y su cuenta atrás;
        ///  3) hornada lista -- el resultado te espera;
        ///  4) hay carga y no está encendido -- E;
        ///  5) reposo vacío: NO PIDE NADA (el fallo del 26), describe.
        /// </summary>
        private string EtiquetaCamara()
        {
            if (_aviso != null && Time.time < _avisoHasta) return _aviso;
            if (_fase == Fase.Corriendo)
            {
                int quedan = Mathf.CeilToInt(Mathf.Max(0f, HornadaSegundos - _hornadaT));
                return _hornadaVerbo + "… " + quedan + "s";
            }
            if (_fase == Fase.Lista) return "hornada lista · recógela con el frasco";
            if (_camaraTieneAlgo) return "cargado · E para encender la hornada";
            return "fuego bajo · vierte y prueba";
        }

        private string EtiquetaCesto()
        {
            if (_cestoArdiendo) return "ardiendo";
            if (_fuelMat == MaterialId.Empty) return "brasero · vacío";
            string nombre = _conocimiento != null ? _conocimiento.NombreParaHud(_fuelMat) : "???";
            return nombre + " · listo para arder";
        }

        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return;

            float cercEstado = UiStyles.Cercania(_centroCamara, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercNombre = UiStyles.Cercania(_centroCamara, _player, RangoNombrePleno, RangoNombreDesvanece);
            float cercCesto = UiStyles.Cercania(_centroBrasero, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            if (cercEstado <= 0f && cercNombre <= 0f && cercCesto <= 0f) return;
            if (!_yaConocida && cercNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();

            if (cercEstado > 0f)
            {
                Color color = _fase == Fase.Corriendo ? UiStyles.Peligro
                            : (_fase == Fase.Lista ? UiStyles.Exito
                            : (_aviso != null && Time.time < _avisoHasta ? _avisoColor : UiStyles.Aviso));
                UiStyles.PlacaMundo(_centroBoca, EtiquetaCamara(),
                    new Color(color.r, color.g, color.b, color.a * cercEstado), -UiStyles.S(6f));
            }

            // La chapa del brasero cuelga DE SU PROPIA BOCA, nunca de la del
            // crisol: el playtest 26 puso los dos mensajes en el mismo sitio y
            // por eso "cargadme combustible" parecía hablar de la cubeta.
            if (cercCesto > 0f)
            {
                Color colorCesto = _cestoArdiendo ? UiStyles.Peligro : UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroBrasero, EtiquetaCesto(),
                    new Color(colorCesto.r, colorCesto.g, colorCesto.b, colorCesto.a * cercCesto), -UiStyles.S(24f));
            }

            if (!_yaConocida && cercNombre > 0f)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroBoca, ChapaNombre, new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercNombre), -UiStyles.S(23f));
            }

            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado && _fase != Fase.Corriendo)
            {
                UiStyles.PlacaMundo(_centroBoca, "E — encender la hornada",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercNombre), -UiStyles.S(23f));
            }
        }
    }
}
