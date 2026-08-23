using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// La Tolva del Maestro: el pozo excavado en el contrafuerte de piedra del
    /// muro derecho (su geometría vive en Sim/SimLevelBuilder.BuildDeliveryNiche,
    /// que es la única fuente de verdad de dónde está la boca). Solo se CONSUME
    /// en las filas del fondo del pozo (el "sillar", <see cref="ChuteSillRows"/>
    /// filas junto al suelo de piedra) y se evalúa contra los encargos activos
    /// de <see cref="OrderSystem"/>.
    ///
    /// DOS BOCAS POSIBLES, UNA SOLA POR PARTIDA (CONTRATO_RONDA50.md §3b,
    /// ENCARGO G, playtest 50, diagnóstico D4 "EL TRAYECTO MUDO"; visual
    /// rediseñada en el playtest 54, ver "REDISEÑO DEL BUZÓN" más abajo):
    /// en modo CAÓTICO/MULTI esta clase sigue tallando/usando la boca
    /// CLÁSICA de siempre (lejos, tras el pasillo -- contrato §3e, "el
    /// caótico NO cambia"). En Semilla Cero usa EL BUZÓN DEL MAESTRO,
    /// tallado DENTRO del cuarto íntimo, ELEVADO Y LATERAL sobre la zona
    /// del Crisol/Alambique (<see cref="SimLevelBuilder.BuildBuzonMaestro"/>,
    /// ver el docblock de las constantes `Buzon*` para el sitio exacto y
    /// por qué -- sustituye a la TOLVA CERCANA del playtest 50,
    /// <see cref="SimLevelBuilder.BuildTolvaCercana"/>, RETIRADA de este
    /// camino pero conservada intacta, regla 15 de CLAUDE.md). La elección
    /// la hace <see cref="Init"/>, leyendo
    /// <see cref="AlkahestGameBootstrap.ModoSemillaCero"/> UNA sola vez --
    /// ver <see cref="_zoneX0"/>/<see cref="_zoneFloorY1"/>, que dejaron de
    /// ser `const` por eso. El arrastre y el consumo (más abajo) son
    /// IDÉNTICOS para las dos bocas: solo cambia DÓNDE viven. El marco
    /// visual y el rótulo YA NO son idénticos desde el playtest 54 -- ver
    /// "REDISEÑO DEL BUZÓN" para las tres diferencias, todas SOLO en
    /// Semilla Cero.
    ///
    /// FAVOR SOLO POR ENCARGOS (fix playtest 9): antes, lo que no encajaba con
    /// ningún encargo incompleto se contaba como "chatarra" y daba 1 Favor
    /// cada <c>ScrapPerFavor</c> celdas -- "para que experimentar nunca fuera
    /// del todo inútil". Eliminado por completo (constante, contador y
    /// llamada a AddFavor incluidos): el reporte del playtest 9 fue literal
    /// -- "me suben los puntos aunque le agregue cualquier cosa a la tolva y
    /// esta me diga que no lo necesita". Un mensaje ("esto no lo necesito") y
    /// una recompensa (+Favor) contradictorios en el mismo gesto enseñan a
    /// ignorar los dos. Decisión de dirección: SOLO los encargos dan Favor
    /// (ver OrderSystem.TryDeliverCell). La materia vertida que no cuenta
    /// SIGUE consumiéndose igual -- "engullir" sigue siendo el verbo de la
    /// Tolva, ver el bloque LA GARGANTA ARRASTRA más abajo -- pero ya no paga
    /// por ello; en su lugar se avisa CLARO y BREVE de por qué no contó,
    /// distinguiendo los dos motivos posibles (ver ConsumeTick):
    ///  · "material equivocado" -- ningún encargo activo pide esto.
    ///  · "encargo ya completo" -- el material SÍ era el correcto, pero ese
    ///    encargo concreto ya se cumplió; importa que el jugador se entere,
    ///    porque significa que está desperdiciando trabajo (verter más de lo
    ///    que ya se pidió no sirve de nada, a diferencia de antes).
    /// Reutiliza el mismo sistema de aviso "una vez por material" que ya
    /// existía (<see cref="_scrapWarned"/>) -- no se ha añadido HUD nuevo.
    ///
    /// LA GARGANTA ARRASTRA (fix playtest 8: "aún hay algún problema para
    /// entregar sólidos en la tolva, los que resultan de combinaciones raras").
    /// CAUSA RAÍZ: consumir solo en el fondo (en vez de en las 29 filas del
    /// pozo, como antes de la ronda pasada) fue un acierto para que lo vertido
    /// se VEA CAER -- pero los materiales de arquetipo StaticSolid (Cristal,
    /// Hielo: precisamente lo que sale de "combinaciones raras" como la
    /// cristalización de Azoth o la congelación) NO tienen regla de caída en
    /// SimStepper.ProcessIfNeeded (su case está vacío a propósito, ver el fix
    /// de hielo-inyecta-frío ahí mismo): si el jugador los vierte a media
    /// altura del pozo -- y Flask.TickPour/PourMaterial pinta en el punto
    /// exacto donde apunta el cursor, a CUALQUIER altura dentro del alcance,
    /// no solo en el labio -- se quedan flotando ahí para siempre y nunca
    /// llegan al sillar. Revertir a "consumir en todo el pozo" perdería la
    /// caída visible que el jugador ya valoró en el playtest anterior.
    ///
    /// SOLUCIÓN DE DISEÑO, no parche: la Tolva es un APARATO del taller y
    /// "engullir" es su verbo propio. Que arrastre hacia su garganta todo lo
    /// que le eches -- sólido, líquido, polvo, da igual -- es coherente con la
    /// ficción (una tolva no deja las cosas flotando a mitad de tubo) y hace
    /// VISIBLE lo que pasa con cada entrega. <see cref="ArrastreTick"/> tira
    /// de toda celda no vacía del pozo un paso hacia su fila inferior si esa
    /// fila está libre, arquetipo aparte; al llegar al sillar, el consumo de
    /// siempre hace su trabajo. Es puramente posicional y determinista (nada
    /// de aleatoriedad: el orden de barrido decide todo), y vive aquí -- en
    /// Game/, que ya muta el grid vía AlkahestSim.Paint -- y NUNCA en Sim/,
    /// que debe permanecer agnóstico de la Tolva.
    ///
    /// REDISEÑO VISUAL (playtest 3: "la tolva quedó fatal, no entiendo dónde
    /// dejar las cosas"). Cuatro señales redundantes, para que se entienda de un
    /// vistazo y desde el otro extremo del taller:
    ///  1. Un HUECO REAL en la pared (no un bolsillo de 3 celdas invisible).
    ///  2. Un MARCO DORADO grueso (jambas + labio) alrededor de la boca.
    ///  3. Una FLECHA que flota sobre la boca, cabeceando hacia ella.
    ///  4. Un PULSO de alfa en el labio + rótulo con fondo oscuro; al tragar
    ///     algo, destello verde ("entrega aceptada") o ámbar (no contó).
    ///
    /// EL RÓTULO YA NO PERSIGUE AL JUGADOR, Y CALLA (fix playtest 16, reporte
    /// con captura: "el título me acompaña al desplazarme a la izquierda, en
    /// vez de quedarse en su lugar; debería desaparecer tras unas pocas veces
    /// y ser más discreto"). CAUSA RAÍZ: el señalamiento 4 de arriba se apoyaba
    /// en <see cref="UiStyles.EtiquetaMundo"/>/<see cref="UiStyles.Globo"/>,
    /// que hasta esta ronda ACOTABAN la posición del rótulo a los bordes de la
    /// pantalla con Mathf.Clamp -- una salvaguarda inocua mientras la cámara
    /// enmarcaba el mundo entero (todo punto de mundo estaba siempre en
    /// pantalla, el clamp nunca se disparaba de verdad) que con la cámara
    /// SIGUIENDO al aprendiz (pantalla de tres) se convirtió en el bug: la
    /// boca de la Tolva, pegada al muro derecho, queda fuera de cuadro en
    /// cuanto el jugador se aleja hacia la izquierda, y el clamp "traía de
    /// vuelta" su rótulo al borde derecho de la pantalla -- pegado ahí,
    /// siguiendo al jugador como si el aparato lo persiguiera. Arreglado EN
    /// GENERAL en UiStyles.cs (ver <c>DentroDePantalla</c>/
    /// <c>MargenFueraDeCuadro</c>): un rótulo de mundo cuyo ancla cae fuera del
    /// rectángulo visible (con un margen para no parpadear en el borde) ya no
    /// se dibuja en absoluto, así que esta clase no necesitó ningún cambio
    /// para ese primer síntoma.
    ///
    /// LO QUE SÍ SE TOCÓ AQUÍ es la otra mitad del reporte -- "que aparezca
    /// menos veces y de forma más discreta". El texto fijo "TOLVA DEL MAESTRO
    /// — vierte AQUÍ" (rama de reposo de <see cref="OnGUI"/>, cuando no hay
    /// destello ni aviso de chatarra) es, en esencia, el MISMO tipo de rótulo
    /// que el prompt "E — regular el fuego"/"E — encender el frío"/"E — abrir"
    /// de placa ígnea/piedra gélida/grifos: una instrucción de "así se usa
    /// este aparato" que solo hace falta enseñar las primeras veces del
    /// taller. Por eso NO se inventa un contador nuevo -- se reutiliza
    /// <see cref="MachineFocus.MostrarPromptE"/> tal cual, el mismo flag
    /// GLOBAL y compartido que ya usan esos tres aparatos (ver
    /// <c>UsosParaAprender</c>=2 en Game/MachineFocus.cs): en cuanto el
    /// jugador ha aprendido a pulsar E en CUALQUIER máquina del taller (dos
    /// veces), el texto de la Tolva deja de aparecer para siempre en esa
    /// partida, exactamente igual que los prompts de las demás máquinas. La
    /// Tolva no usa E -- se vierte, no se pulsa -- así que nunca LLAMA a
    /// <c>MachineFocus.RegistrarUsoE()</c>, solo LEE el flag: es una
    /// consumidora más de la misma lección aprendida, no una fuente. El
    /// candidato descartado fue duplicar aquí el patrón de <c>_yaConocida</c>
    /// de HeatPlate/ChillStone (un booleano de instancia que se fija la
    /// primera vez que el jugador entra de lleno en un anillo de cercanía):
    /// encajaría igual de bien, pero el jugador pidió literalmente "unas
    /// pocas veces", y <c>MostrarPromptE</c> ya cuenta usos (dos) en vez de
    /// fijarse con una sola visita, así que es el criterio existente que
    /// mejor encaja con lo pedido -- no había que inventar ninguno.
    ///
    /// LA SEÑAL QUE QUEDA es puramente visual: <see cref="AnimarMarco"/> ahora
    /// también ilumina jambas/labio/flecha según la cercanía del aprendiz
    /// (<see cref="UiStyles.Cercania"/>, el mismo criterio compartido que usa
    /// el halo de foco de las demás máquinas) -- de lejos el marco se ve
    /// apagado pero encontrable, y se enciende a su brillo pleno según el
    /// jugador entra en el radio de verter (ver <c>BrilloRangoPleno</c>,
    /// calibrado contra <see cref="Flask.ReachWorld"/>). Ni la animación de
    /// volcado (destello de aceptado/rechazado, el bamboleo de la flecha) ni
    /// <see cref="ArrastreTick"/> se tocaron -- ambos siguen validados tal
    /// cual del playtest 3/8.
    ///
    /// LIMITACIÓN: lee _sim.Grid.temp[] directamente para evaluar los encargos
    /// Hot/Cold (mismo patrón que HeatPlate/ChillStone).
    /// TODO(TenThousandYears): canalizar por una API de lectura del sim.
    ///
    /// =====================================================================
    /// VERIFICADO (playtest 21, EL PIVOT): "LA RECOMPENSA DE CAVAR" NO SE CALLA
    /// =====================================================================
    /// <see cref="OnGUI"/> añadió la guarda <c>|| DayCycle.HudSilenciado</c>
    /// (hermana de <c>DayCycle.InputLocked</c>, ver su docblock en
    /// Game/DayCycle.cs). Se comprobó explícitamente que esto NO calla el
    /// rótulo de la Tolva justo cuando el jugador por fin cava hasta ella:
    ///  · <c>HudSilenciado</c> se apaga (para siempre, en toda la partida) en
    ///    el primer <c>DayCycle.DetectarPrimeraAccion</c> que vea movimiento
    ///    (WASD/flechas) O clic de ratón.
    ///  · Cavar (Game/Cincel.cs, líneas ~220-221) usa exactamente
    ///    <c>mouse.leftButton.isPressed</c> (tallar) / <c>rightButton.isPressed</c>
    ///    (rellenar) -- el MISMO gesto que ya cuenta como "clic" para
    ///    <c>DetectarPrimeraAccion</c>. La Tolva está sellada tras 23-35
    ///    celdas de roca (ver Sim/SimLevelBuilder.cs, "EL CUARTO ÍNTIMO"): es
    ///    IMPOSIBLE llegar a ella sin haber tallado antes, así que
    ///    <c>HudSilenciado</c> ya lleva rato en <c>false</c> (desde el primer
    ///    golpe de cincel) mucho antes de que el jugador asome a la boca del
    ///    pozo. Su rótulo llega intacto a ese momento.
    ///  · Es un pestillo de un solo sentido (true -> false, nunca al revés
    ///    salvo <c>DayCycle.Init()</c> en una partida nueva): nada se queda
    ///    mudo para siempre -- todos los HUD guardados por HudSilenciado
    ///    (los otros 9 archivos listados en su docblock) se reactivan solos
    ///    con la misma pulsación, sin ningún paso manual adicional.
    ///  · Hallazgo lateral (no un bug, documentado por si sorprende en el
    ///    futuro): <see cref="MachineFocus.MostrarPromptE"/> solo se apaga
    ///    tras <c>UsosParaAprender</c>=2 llamadas a
    ///    <c>MachineFocus.RegistrarUsoE()</c>, y esas llamadas viven SOLO en
    ///    HeatPlate/ChillStone/Dispenser (grep confirmado) -- los tres
    ///    aparatos que el pivot deja de instanciar en
    ///    AlkahestGameBootstrap.TrySpawn(). Con ellos ausentes, nada más en
    ///    el cuarto íntimo llama a RegistrarUsoE (Criatura/Capullo tampoco lo
    ///    hacen), así que <c>MostrarPromptE</c> se queda en <c>true</c> el
    ///    resto de la partida: el texto "TOLVA DEL MAESTRO — vierte AQUÍ" NO
    ///    se retira tras dos usos como en el taller clásico, sino que sigue
    ///    disponible siempre que no haya destello/aviso activo -- si acaso
    ///    RESPALDA la "recompensa de cavar" en vez de arriesgarla.
    ///
    /// =====================================================================
    /// REDISEÑO DEL BUZÓN (PLAYTEST 54) -- SOLO SEMILLA CERO
    /// =====================================================================
    /// Feedback de Cesar sobre el rediseño del pt50 (la "Tolva Cercana"):
    /// "El coso arriba de la tolva que aún no funciona" (el triángulo/
    /// embudo amarillo -- <see cref="SpriteFlecha"/> cabeceando sobre la
    /// boca) es "ruido"; y sobre el sitio en sí: "atravesarla con esa
    /// flecha amarilla y letrero grande -- dos barras amarillas que no
    /// tienen nada de tolva -- en medio del camino, alejándome de la única
    /// cosa que necesito investigar al inicio [el Crisol], es muy mala
    /// decisión... algo que GRÁFICAMENTE comunique que tengo que dejar
    /// pedidos, no una flecha amarilla y un letrerote".
    ///
    /// TRES CAMBIOS, SOLO EN LA RAMA <see cref="_esBuzonSemillaCero"/> (el
    /// caótico/multi sigue siendo LA TOLVA CLÁSICA de siempre, ni un píxel
    /// distinto -- <see cref="BuildVisualClasica"/>/las ramas `else` de
    /// <see cref="AnimarMarco"/>/<see cref="OnGUI"/>):
    ///  1. SITIO: <see cref="SimLevelBuilder.BuzonMouthX0"/>..<c>Y1</c>, un
    ///     hueco ELEVADO Y LATERAL sobre la zona del Crisol/Alambique (ver
    ///     el docblock grande de las constantes `Buzon*` en
    ///     Sim/SimLevelBuilder.cs) -- ya NO parte en dos el paso caminable
    ///     Crisol->Prensa (ese hueco de suelo, x129..138, vuelve a ser
    ///     suelo limpio).
    ///  2. VISUAL: <see cref="BuildVisualBuzon"/> sustituye jambas
    ///     doradas+labio+flecha por un marco de LATÓN discreto (más fino,
    ///     `UiStyles.Laton` en vez de `UiStyles.Oro`), una bandejita fija
    ///     que NO pulsa, y un relieve de pergamino/sello ESTÁTICO -- ningún
    ///     elemento nuevo se anima salvo el color/alfa por cercanía y
    ///     destello (mismo criterio que el resto del taller). El halo de
    ///     "pedido activo" (<see cref="_luzPedido"/>) SE QUEDA -- Cesar:
    ///     "es la señal buena" -- pero a la MITAD de intensidad
    ///     (<see cref="AnimarMarco"/>: centro/amplitud 0.30/0.22 ->
    ///     0.15/0.11).
    ///  3. RÓTULO: <see cref="OnGUI"/> deja de gatear el texto permanente
    ///     por <c>MachineFocus.MostrarPromptE</c> (el criterio de "tutorial
    ///     aprendido tras 2 usos" del taller clásico) -- pasa a ser SOLO DE
    ///     PROXIMIDAD, vía <see cref="UiStyles.PlacaMundo"/> (el mismo
    ///     patrón de desvanecimiento por cercanía que ya usa el resto del
    ///     taller, regla 28 de CLAUDE.md), con el texto acortado "BUZÓN DEL
    ///     MAESTRO — vierte aquí lo pedido" (ya no repite el gesto completo
    ///     en el cuerpo del rótulo permanente -- eso lo enseña el consejo
    ///     de primer pedido de Game/SemillaCero.cs, sin cambios).
    ///
    /// LA FLECHA DEL ORDERSHUD (glifo ←/→/↓ en el panel de encargos,
    /// playtest 50, `Game/OrdersHud.cs::TryFlechaTolva`, archivo AJENO a
    /// este encargo) SE QUEDA SIN TOCAR -- verificado que sigue apuntando
    /// al sitio correcto: lee <c>_tolva.transform.position</c> de ESTE
    /// componente, y <see cref="Init"/> sigue fijando ese transform al
    /// centro del labio de la boca activa (ver el bloque de arriba, "El
    /// transform se ancla al CENTRO DEL LABIO") -- solo que ahora esa boca
    /// es el Buzón. HALLAZGO, NO CORREGIBLE DESDE AQUÍ (archivo ajeno):
    /// `TryFlechaTolva` solo emite "↓" cuando el objetivo está por DEBAJO
    /// del aprendiz, NUNCA "arriba" (contrato pt50, textual: "solo
    /// izquierda/derecha/abajo") -- con el Buzón ahora POR ENCIMA del
    /// spawn, ese caso "arriba" cae al `else` de X (`→`/`←`). Verificado
    /// que esto NO produce una flecha engañosa en la práctica: desde
    /// <see cref="SimLevelBuilder.AprendizX"/>=186 hasta
    /// <see cref="SimLevelBuilder.BuzonX0"/>≈97 el desnivel en X (~85-89
    /// celdas) domina siempre al desnivel en Y (~46 celdas), así que
    /// <c>ax &gt; ay</c> y la flecha muestra "←" -- correcto. Solo se
    /// degrada (residual "←"/"→" en vez de "arriba") si el jugador llega a
    /// pararse casi exactamente bajo el Buzón, un caso raro y de bajo
    /// coste que no justifica tocar un archivo ajeno.
    /// </summary>
    public sealed class DeliveryChute : MonoBehaviour
    {
        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;

        private const float FlashSeconds = 0.5f;

        /// <summary>
        /// Cuánto tiempo se muestra el aviso "esto no cuenta" la PRIMERA vez
        /// que se entrega un material en cada uno de los dos motivos posibles
        /// (fix playtest 8, ampliado en playtest 9 para distinguir "material
        /// equivocado" de "encargo ya completo" -- ver docblock de la clase).
        /// Más largo que FlashSeconds a propósito: es la única vez que se lee
        /// el nombre del material y el motivo completo, así que necesita
        /// tiempo de lectura, no solo de pulso.
        /// </summary>
        private const float ScrapEducationSeconds = 2.5f;

        // Geometría de la boca, tomada del constructor de nivel (nunca
        // duplicada aquí).
        //
        // (CONTRATO_RONDA50.md §3b, ENCARGO G, playtest 50) YA NO SON
        // `const`: en modo CAÓTICO/MULTI la boca sigue siendo la Tolva
        // CLÁSICA de siempre (SimLevelBuilder.ChuteMouthX0..Y1, lejos, tras
        // el pasillo -- contrato §3e, "el caótico NO cambia"), pero en
        // Semilla Cero es la TOLVA CERCANA que este mismo encargo talló
        // DENTRO del cuarto (SimLevelBuilder.TolvaCercanaMouthX0..Y1, ver el
        // docblock de esas constantes para el porqué exacto del sitio,
        // "a segundos de vuelo del Crisol" -- contrato, textual). La
        // decisión se toma UNA vez, en Init(), leyendo
        // AlkahestGameBootstrap.ModoSemillaCero -- mismo patrón exacto que ya
        // usan Sim/SimLevelBuilder.cs::BuildVetaTurba y Game/HintSystem.cs
        // para el mismo flag. Ya NO hace falta tocar
        // Game/AlkahestGameBootstrap.cs (que sigue llamando a
        // `SpawnDeliveryChute` sin pasar ningún sitio): esta clase decide
        // sola qué boca es la suya, exactamente como ya hacía antes de esta
        // ronda -- la única diferencia es que ahora hay DOS bocas posibles en
        // el plano en vez de una, y el gate en SimLevelBuilder.BuildCuartoIntimo
        // se asegura de que solo UNA de las dos se talle de verdad por
        // partida (contrato §3b: "una sola boca").
        private int _zoneX0, _zoneX1, _zoneY0, _zoneY1;

        /// <summary>
        /// Filas del "sillar" (junto al suelo de piedra del pozo, _zoneY0 hacia
        /// arriba) donde de verdad se CONSUME. SimLevelBuilder es de solo
        /// lectura para esta tarea y no expone esta constante, así que vive
        /// aquí -- es una decisión de Game/, no de la geometría del taller.
        /// 3 filas: bastan para que el jugador vea el material posarse un
        /// instante antes de desaparecer (feedback de "esto SÍ ha llegado"),
        /// sin alargar la espera de un encargo grande. En la boca clásica
        /// deja ChuteMouthY1 - ChuteMouthY0 + 1 - 3 = 26 filas de pozo real
        /// donde arrastrar (22 columnas x 26 filas); en el Buzón del Maestro
        /// (playtest 54, BuzonBocaAlto=8, BuzonBocaAncho=6) deja 5 filas de
        /// pozo -- de sobra igual, ningún encargo de Semilla 0 vierte más de
        /// un puñado de celdas por chorro, y el Buzón es una hornacina de
        /// pared compacta, no un pozo de suelo a techo (contraste con la
        /// Tolva Cercana retirada, TolvaCercanaAlto=20, que dejaba 17).
        /// </summary>
        private const int ChuteSillRows = 3;
        private int _zoneFloorY1;

        private AlkahestSim _sim;
        private OrderSystem _orderSystem;
        private float _accumulator;

        /// <summary>
        /// (fix playtest 16) Transform del aprendiz, para <see cref="AnimarMarco"/>
        /// -- la señal visual de "vierte aquí" que sustituye al rótulo
        /// permanente necesita saber la distancia del jugador, igual que
        /// <see cref="UiStyles.Cercania"/> en el resto de las máquinas.
        /// AlkahestGameBootstrap.cs (fuera del alcance de este fix, no es
        /// editable aquí) llama a <see cref="Init"/> solo con `sim` y
        /// `orderSystem` -- a diferencia de HeatPlate/ChillStone/Dispenser, que
        /// SÍ reciben el Transform del jugador por inyección directa -- así que
        /// se busca aquí con <c>FindAnyObjectByType</c> (regla 1 de CLAUDE.md;
        /// mismo patrón defensivo que ya usa Dev/DevPalette.cs para su propio
        /// Sim). El aprendiz ya existe en la escena cuando se llama a Init
        /// (TrySpawn lo crea antes de SpawnDeliveryChute), pero por si ese
        /// orden cambiara algún día, Update() reintenta hasta encontrarlo.
        /// </summary>
        private Transform _player;

        // Aviso educativo "una vez por material" (fix playtest 8, ver
        // "Además"; ampliado en playtest 9 para llevar también el MOTIVO):
        // un array plano indexado por MaterialId, sin listas ni asignaciones
        // en el hot path de ConsumeTick. Ya no cuenta celdas para dar Favor
        // (eso desapareció, ver docblock de la clase) -- solo decide cuándo
        // mostrar el mensaje largo una única vez por material.
        private readonly bool[] _scrapWarned = new bool[MaterialId.Count];
        private string _scrapMsg;
        private float _scrapMsgHasta;

        // Motivo del último desajuste (fix playtest 9): alimenta tanto el
        // mensaje educativo largo como el destello ámbar corto/recurrente de
        // OnGUI, para que incluso las entregas 2ª, 3ª... de un material ya
        // avisado sigan mostrando el motivo correcto en el rótulo breve.
        private bool _lastMismatchWasCompletedOrder;

        // --- Marco CLÁSICO (caótico/multi -- SIN CAMBIOS, regla "el caótico
        // no cambia" del contrato pt50, ver el docblock de la clase). ---
        private SpriteRenderer _jambaIzq;
        private SpriteRenderer _jambaDer;
        private SpriteRenderer _labio;
        private SpriteRenderer _flecha;
        private Transform _flechaTr;
        private float _flechaY;

        // --- EL BUZÓN DEL MAESTRO (playtest 54, SOLO Semilla Cero) -- ver el
        // docblock grande al final de esta clase, "REDISEÑO DEL BUZÓN
        // (PLAYTEST 54)", y SimLevelBuilder.BuildBuzonMaestro para el sitio. ---
        private bool _esBuzonSemillaCero;
        private SpriteRenderer _boMarcoIzq;
        private SpriteRenderer _boMarcoDer;
        private SpriteRenderer _boMarcoSup;
        private SpriteRenderer _boBandeja;
        private float _boRotuloY;

        private float _flashHasta;
        private bool _flashAceptado;

        /// <summary>
        /// (CONTRATO_RONDA50.md §3c, ENCARGO G, playtest 50) "LA TOLVA LATE
        /// CON PEDIDO ACTIVO... reutilizando el patrón de luz/latido
        /// existente" (contrato, textual) -- <see cref="MaquinariaSprites.Luz"/>
        /// es exactamente ese patrón (ya usado por Game/Crisol.cs::_luzHogar y
        /// media docena de máquinas más, ver <c>Latir</c>). SOLO Semilla Cero
        /// (contrato §3e): en caótico se crea igual (barato, un halo apagado
        /// no cuesta nada) pero nunca se enciende -- ver AnimarMarco.
        /// </summary>
        private MaquinariaSprites.Luz _luzPedido;

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, OrderSystem orderSystem)
        {
            _sim = sim;
            _orderSystem = orderSystem;
            _player = FindAnyObjectByType<ApprenticeController>()?.transform; // (fix playtest 16, ver doc del campo _player)

            // (playtest 54) LA BOCA QUE TOCA: el BUZÓN DEL MAESTRO en Semilla
            // Cero (tallado por SimLevelBuilder.BuildBuzonMaestro -- ver el
            // docblock de las constantes Buzon* para el porqué del sitio y
            // el rediseño; sustituye a la Tolva Cercana del playtest 50,
            // RETIRADA de este camino pero conservada intacta, regla 15),
            // clásica en caótico/multi -- decidido UNA vez aquí, nunca por
            // frame.
            if (AlkahestGameBootstrap.ModoSemillaCero)
            {
                _zoneX0 = SimLevelBuilder.BuzonMouthX0;
                _zoneX1 = SimLevelBuilder.BuzonMouthX1;
                _zoneY0 = SimLevelBuilder.BuzonMouthY0;
                _zoneY1 = SimLevelBuilder.BuzonMouthY1;
            }
            else
            {
                _zoneX0 = SimLevelBuilder.ChuteMouthX0;
                _zoneX1 = SimLevelBuilder.ChuteMouthX1;
                _zoneY0 = SimLevelBuilder.ChuteMouthY0;
                _zoneY1 = SimLevelBuilder.ChuteMouthY1;
            }
            _zoneFloorY1 = _zoneY0 + ChuteSillRows - 1;

            // (playtest 54) Decidido UNA vez aquí, nunca por frame -- mismo
            // criterio que la elección de boca de arriba. Distingue las DOS
            // vistas completamente distintas de BuildVisual/AnimarMarco/OnGUI
            // de más abajo.
            _esBuzonSemillaCero = AlkahestGameBootstrap.ModoSemillaCero;

            // El transform se ancla al CENTRO DEL LABIO de la boca: es el punto
            // al que apuntan flecha y rótulo.
            transform.position = new Vector3(
                (_zoneX0 + _zoneX1 + 1) * 0.5f * SimRenderer.CellWorldSize,
                (_zoneY1 + 1) * SimRenderer.CellWorldSize,
                0f);

            if (_esBuzonSemillaCero) BuildVisualBuzon();
            else BuildVisualClasica();
        }

        // -----------------------------------------------------------------
        // Visual CLÁSICA (caótico/multi): marco dorado + flecha, SIN
        // CAMBIOS respecto al playtest 3/16 -- "el caótico no cambia"
        // (contrato pt50 §3e), ver el docblock grande al final de la clase.
        // -----------------------------------------------------------------
        private void BuildVisualClasica()
        {
            float celda = SimRenderer.CellWorldSize;
            float bocaIzq = _zoneX0 * celda;
            float bocaDer = (_zoneX1 + 1) * celda;
            float bocaAlto = (_zoneY1 + 1 - _zoneY0) * celda;
            float centroY = (_zoneY0 * celda + (_zoneY1 + 1) * celda) * 0.5f;
            float grosor = 0.26f;

            var solido = SpriteSolido();

            // Jambas: dos pilastras doradas pegadas a los cantos de piedra de la boca.
            _jambaIzq = CrearSprite("JambaIzq", solido, 19,
                new Vector3(bocaIzq - grosor * 0.5f, centroY, 0f),
                new Vector3(grosor, bocaAlto + grosor, 1f));
            _jambaDer = CrearSprite("JambaDer", solido, 19,
                new Vector3(bocaDer + grosor * 0.5f, centroY, 0f),
                new Vector3(grosor, bocaAlto + grosor, 1f));

            // Labio: la línea que cruza la boca. Es el elemento que PULSA — marca
            // el plano exacto donde hay que soltar el material.
            _labio = CrearSprite("Labio", solido, 20,
                new Vector3((bocaIzq + bocaDer) * 0.5f, (_zoneY1 + 1) * celda, 0f),
                new Vector3(bocaDer - bocaIzq + grosor * 2f, 0.10f, 1f));

            // Flecha cabeceando sobre la boca.
            var flechaGO = new GameObject("Flecha");
            flechaGO.transform.SetParent(transform, false);
            _flechaTr = flechaGO.transform;
            _flechaY = (_zoneY1 + 1) * celda + 0.75f;
            _flechaTr.position = new Vector3((bocaIzq + bocaDer) * 0.5f, _flechaY, 0f);
            _flecha = flechaGO.AddComponent<SpriteRenderer>();
            _flecha.sprite = SpriteFlecha(0.95f);
            _flecha.sortingOrder = 21;
            _flecha.color = UiStyles.Oro;

            // (CONTRATO_RONDA50.md §3c) EL HALO DE "HAY PEDIDO ACTIVO": un
            // óvalo suave, más ancho que la boca, DETRÁS de jambas/labio
            // (orden 18, uno menos que las jambas) -- late (Latir, ver
            // AnimarMarco) en vez de brillar fijo, así se distingue del
            // brillo por CERCANÍA que ya tienen jambas/labio/flecha. Nace
            // apagado (Luz.Crear siempre nace así): que encienda es decisión
            // de AnimarMarco, nunca de aquí.
            _luzPedido = MaquinariaSprites.Luz.CrearOvalada(transform, "LuzPedidoTolva",
                new Vector3((bocaIzq + bocaDer) * 0.5f, centroY, 0f),
                (bocaDer - bocaIzq) * 1.6f, bocaAlto * 1.3f, UiStyles.Oro);
        }

        // -----------------------------------------------------------------
        // Visual del BUZÓN DEL MAESTRO (playtest 54, SOLO Semilla Cero):
        // ver "REDISEÑO DEL BUZÓN (PLAYTEST 54)" al final de la clase para
        // el porqué completo. CUATRO piezas, ninguna una flecha ni un
        // letrero:
        //  1. Un marco de LATÓN discreto (UiStyles.Laton, no UiStyles.Oro --
        //     "más apagado que Oro", ver el docblock de esa constante en
        //     UiStyles.cs) alrededor de la ranura -- mucho más fino
        //     (grosor 0.10 contra 0.26 de las jambas clásicas) que "las dos
        //     barras amarillas" que criticó Cesar.
        //  2. Una bandejita: una repisa fina que sobresale bajo la ranura
        //     (UiStyles.LatonOscuro, estática, sin animar).
        //  3. Un relieve de pergamino (UiStyles.Pergamino) con un pequeño
        //     sello de latón encima (UiStyles.LatonOscuro), DENTRO del
        //     margen de piedra que SimLevelBuilder.BuildBuzonMaestro deja
        //     sobre la ranura (BuzonBlockTopY - BuzonMouthY1 = 3 filas) --
        //     comunica "aquí se entregan encargos" sin una sola letra
        //     (contrato, textual: "algo que GRÁFICAMENTE comunique").
        //  4. El mismo halo ovalado de "hay pedido activo" de siempre
        //     (Luz.CrearOvalada, API sin cambios), con intensidad a la
        //     MITAD en AnimarMarco (contrato: "baja su intensidad a ~la
        //     mitad; es la señal buena").
        // -----------------------------------------------------------------
        private void BuildVisualBuzon()
        {
            float celda = SimRenderer.CellWorldSize;
            float bocaIzq = _zoneX0 * celda;
            float bocaDer = (_zoneX1 + 1) * celda;
            float bocaAlto = (_zoneY1 + 1 - _zoneY0) * celda;
            float centroY = (_zoneY0 * celda + (_zoneY1 + 1) * celda) * 0.5f;
            float grosorMarco = 0.10f; // discreto -- contraste deliberado con el `grosor=0.26f` de las jambas clásicas.

            var solido = SpriteSolido();

            // El marco: tres tramos (dos jambas + lintel superior). La base
            // de la ranura la hace `_boBandeja` más abajo, que además
            // sobresale (lee como repisa, no como cuarta jamba).
            _boMarcoIzq = CrearSprite("BuzonMarcoIzq", solido, 19,
                new Vector3(bocaIzq - grosorMarco * 0.5f, centroY, 0f),
                new Vector3(grosorMarco, bocaAlto + grosorMarco, 1f));
            _boMarcoDer = CrearSprite("BuzonMarcoDer", solido, 19,
                new Vector3(bocaDer + grosorMarco * 0.5f, centroY, 0f),
                new Vector3(grosorMarco, bocaAlto + grosorMarco, 1f));
            _boMarcoSup = CrearSprite("BuzonMarcoSup", solido, 19,
                new Vector3((bocaIzq + bocaDer) * 0.5f, (_zoneY1 + 1) * celda, 0f),
                new Vector3(bocaDer - bocaIzq + grosorMarco * 2f, grosorMarco, 1f));

            // La bandejita: repisa de latón viejo, fija, que sobresale un
            // poco hacia el jugador -- NO pulsa como el labio clásico (esa
            // era la "barra amarilla" que se leía como carril de aterrizaje;
            // aquí el pulso lo lleva el halo del pedido, más discreto).
            _boBandeja = CrearSprite("BuzonBandeja", solido, 19,
                new Vector3((bocaIzq + bocaDer) * 0.5f, _zoneY0 * celda - 0.05f, 0f),
                new Vector3(bocaDer - bocaIzq + grosorMarco * 3f, grosorMarco * 1.6f, 1f));
            _boBandeja.color = UiStyles.LatonOscuro;

            // El relieve de pergamino/sello: DENTRO del margen de piedra que
            // el plano ya deja libre sobre la ranura -- puramente
            // decorativo y ESTÁTICO (no lo toca AnimarMarco): la señal que
            // SÍ cambia con el tiempo es el halo del pedido, no esta placa.
            _boRotuloY = (_zoneY1 + 1) * celda + 0.55f;
            var pergaminoGo = new GameObject("BuzonPergamino");
            pergaminoGo.transform.SetParent(transform, false);
            pergaminoGo.transform.position = new Vector3((bocaIzq + bocaDer) * 0.5f, _boRotuloY, 0f);
            pergaminoGo.transform.localScale = new Vector3((bocaDer - bocaIzq) * 0.62f, 0.42f, 1f);
            var pergaminoSr = pergaminoGo.AddComponent<SpriteRenderer>();
            pergaminoSr.sprite = solido;
            pergaminoSr.sortingOrder = 17;
            pergaminoSr.color = UiStyles.Pergamino;

            var selloGo = new GameObject("BuzonSello");
            selloGo.transform.SetParent(transform, false);
            selloGo.transform.position = new Vector3((bocaIzq + bocaDer) * 0.5f, _boRotuloY, 0f);
            selloGo.transform.localScale = new Vector3(0.20f, 0.20f, 1f);
            var selloSr = selloGo.AddComponent<SpriteRenderer>();
            selloSr.sprite = solido;
            selloSr.sortingOrder = 18;
            selloSr.color = UiStyles.LatonOscuro;

            // Mismo halo ovalado de siempre (API sin cambios) -- ver
            // AnimarMarco para la intensidad a la mitad de esta ronda.
            _luzPedido = MaquinariaSprites.Luz.CrearOvalada(transform, "LuzPedidoBuzon",
                new Vector3((bocaIzq + bocaDer) * 0.5f, centroY, 0f),
                (bocaDer - bocaIzq) * 1.6f, bocaAlto * 1.3f, UiStyles.Oro);
        }

        private SpriteRenderer CrearSprite(string nombre, Sprite sprite, int orden, Vector3 posicion, Vector3 escala)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform, false);
            go.transform.position = posicion;
            go.transform.localScale = escala;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = orden;
            sr.color = UiStyles.Oro;
            return sr;
        }

        private static Sprite SpriteSolido()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "TenThousandYearsChuteTex" };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        /// <summary>Triángulo apuntando hacia ABAJO, dibujado a mano (sin assets).</summary>
        private static Sprite SpriteFlecha(float anchoMundo)
        {
            const int w = 24, h = 18;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "TenThousandYearsChuteArrowTex",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                // y = 0 es la punta (abajo), y = h-1 la base (arriba).
                float mitad = (y / (float)(h - 1)) * (w * 0.5f);
                for (int x = 0; x < w; x++)
                {
                    bool dentro = Mathf.Abs(x + 0.5f - w * 0.5f) <= mitad;
                    px[y * w + x] = dentro ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);

            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), w / anchoMundo);
        }

        // -----------------------------------------------------------------
        // Lógica
        // -----------------------------------------------------------------
        private void Update()
        {
            if (_sim == null || _sim.Grid == null || _orderSystem == null) return;
            if (DayCycle.InputLocked) return;

            _accumulator += Time.deltaTime;
            int steps = 0;
            while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
            {
                // Misma cadencia que el consumo (30Hz, igual que SimStepper):
                // la zona de arrastre es como mucho 22x26 = 572 celdas, así
                // que barrerla entera cada tick es barato, y hacerlo a la
                // cadencia del propio sim es lo que hace que un sólido caiga
                // "una fila por tick" igual de fluido que un polvo -- si
                // arrastrase cada 2 ticks se vería a mitad de velocidad que
                // todo lo demás en el pozo, un cojeo perceptible sin motivo
                // de rendimiento que lo justifique. Arrastrar ANTES de
                // consumir: lo que este mismo tick llega al sillar ya se
                // evalúa, en vez de esperar un tick extra sin motivo.
                ArrastreTick();
                ConsumeTick();
                _accumulator -= TickDt;
                steps++;
            }
            if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

            // (fix playtest 16) Reintento perezoso si Init() se llamó antes de
            // que el aprendiz existiera -- no debería pasar con el orden actual
            // de AlkahestGameBootstrap.TrySpawn(), pero es gratis y evita que
            // un reordenamiento futuro deje el marco sin señal de cercanía para
            // siempre. Una vez encontrado, no se vuelve a buscar.
            if (_player == null) _player = FindAnyObjectByType<ApprenticeController>()?.transform;

            AnimarMarco();
        }

        /// <summary>
        /// (fix playtest 16, ver BrilloRangoPleno/Desvanece más abajo)
        /// Distancia a partir de la cual el marco/flecha llegan a su brillo
        /// PLENO -- deliberadamente dentro de <see cref="Flask.ReachWorld"/>
        /// (el alcance real de verter), para que "se enciende del todo"
        /// coincida con "ya puedo verter aquí", no antes.
        /// </summary>
        private const float BrilloRangoPleno = 4.0f;

        /// <summary>Distancia a la que el brillo ya ha caído del todo a <see cref="BrilloLejos"/>.</summary>
        private const float BrilloRangoDesvanece = 8.0f;

        /// <summary>
        /// Brillo mínimo (alfa relativo) cuando el aprendiz está lejos: NO cero
        /// -- la Tolva sigue siendo un HUECO REAL en la pared con su marco
        /// dorado (rediseño playtest 3), así que debe seguir siendo
        /// encontrable de un vistazo aunque el jugador esté al otro lado del
        /// taller. Lo que cambia con la cercanía no es "visible sí/no" sino
        /// "apagado y quieto" -> "vivo y latiendo con fuerza".
        /// </summary>
        private const float BrilloLejos = 0.22f;

        private void AnimarMarco()
        {
            float t = Time.time;
            bool destello = t < _flashHasta;

            // Pulso lento y constante: "esto está vivo, esto espera algo".
            float pulso = 0.55f + 0.45f * Mathf.Sin(t * 3.2f);

            // (fix playtest 16: "ilumina la flecha/el embudo al acercarse, en
            // vez de un texto") Sustituye al rótulo permanente como señal
            // discreta de "vierte aquí": el marco y la flecha se atenúan de
            // lejos y se encienden a su brillo pleno según el aprendiz entra en
            // el radio de verter -- mismo criterio de cercanía que usa el resto
            // del taller para su halo de foco (UiStyles.Cercania). Durante un
            // destello (aceptado/rechazado) el brillo NO se aplica -- ese
            // feedback tiene que leerse a plena intensidad pase lo que pase,
            // es la animación de volcado validada y no se toca.
            float cercania = UiStyles.Cercania(transform.position, _player, BrilloRangoPleno, BrilloRangoDesvanece);
            float brillo = Mathf.Lerp(BrilloLejos, 1f, cercania);

            // (playtest 54) EL TONO BASE distingue las dos vistas: Laton
            // (marco discreto) para el Buzón, Oro (marco clásico) para la
            // Tolva caótica -- SIN CAMBIOS de comportamiento para esta
            // última. El destello de aceptado/rechazado (Exito/Aviso) es
            // IDÉNTICO en los dos: esa señal tiene que leerse a plena
            // intensidad pase lo que pase, es la animación de volcado
            // validada y no se toca.
            Color tono = _esBuzonSemillaCero ? UiStyles.Laton : UiStyles.Oro;
            Color acento = destello ? (_flashAceptado ? UiStyles.Exito : UiStyles.Aviso) : tono;

            if (_esBuzonSemillaCero)
            {
                // EL BUZÓN (playtest 54): marco fino, ESTÁTICO en posición
                // (nada bobea, no hay flecha) -- solo el color/alfa
                // responden a cercanía y destello, igual que el resto del
                // taller (halo de foco de las cinco estaciones).
                Color marco = destello ? acento : new Color(tono.r, tono.g, tono.b, 0.85f * brillo);
                if (_boMarcoIzq != null) _boMarcoIzq.color = marco;
                if (_boMarcoDer != null) _boMarcoDer.color = marco;
                if (_boMarcoSup != null) _boMarcoSup.color = marco;
                // La bandejita NO pulsa (ver su docblock en BuildVisualBuzon)
                // -- sí participa del destello aceptado/rechazado, que debe
                // leerse desde cualquier pieza del marco.
                if (_boBandeja != null) _boBandeja.color = destello ? acento : UiStyles.LatonOscuro;
            }
            else
            {
                // LA TOLVA CLÁSICA (caótico/multi) -- SIN CAMBIOS.
                if (_jambaIzq != null) _jambaIzq.color = destello ? acento : new Color(tono.r, tono.g, tono.b, 0.85f * brillo);
                if (_jambaDer != null) _jambaDer.color = _jambaIzq != null ? _jambaIzq.color : tono;
                if (_labio != null) _labio.color = new Color(acento.r, acento.g, acento.b, destello ? 1f : (0.35f + 0.55f * pulso) * brillo);

                if (_flechaTr != null)
                {
                    Vector3 p = _flechaTr.position;
                    p.y = _flechaY + Mathf.Sin(t * 2.6f) * 0.16f;
                    _flechaTr.position = p;
                }
                if (_flecha != null) _flecha.color = new Color(acento.r, acento.g, acento.b, (0.55f + 0.45f * pulso) * (destello ? 1f : brillo));
            }

            // (CONTRATO_RONDA50.md §3c) LA TOLVA LATE CON PEDIDO ACTIVO --
            // SOLO Semilla Cero (contrato §3e), y solo mientras exista un
            // pedido sin completar (PedidoActivoCompletado ya limpia la
            // lista entera al llegar al final abierto, ver SemillaCero.cs, y
            // durante los huecos entre beats -- p. ej. mientras el Maestro
            // exige el nombre -- también puede quedar en 0 un instante:
            // apagado ahí es correcto, no hay nada que "llevar" todavía).
            // Latido INDEPENDIENTE de la cercanía (a diferencia de jambas/
            // labio/flecha de arriba): es la señal que se lee DESDE LEJOS,
            // "hay algo pendiente en esa boca", no la de "ya estás cerca".
            //
            // (playtest 54) INTENSIDAD A LA MITAD (centro/amplitud
            // 0.30/0.22 -> 0.15/0.11, rango resultante 0.04..0.26 contra el
            // 0.08..0.52 de antes; misma cadencia, hz=0.55 sin tocar) --
            // Cesar: "el latido de luz con pedido activo SE QUEDA pero
            // sutil... es la señal buena". Aplica IGUAL en las dos vistas
            // (la luz siempre fue "SOLO Semilla Cero" -- en caótico
            // `pedidoActivo` es `false` y esta línea no hace nada, como
            // siempre).
            bool pedidoActivo = AlkahestGameBootstrap.ModoSemillaCero && _orderSystem != null
                && _orderSystem.ActiveOrders.Count > 0 && !_orderSystem.ActiveOrders[0].Completado;
            if (pedidoActivo) _luzPedido?.Latir(0.15f, 0.11f, 0.55f);
            else _luzPedido?.Intensidad(0f);
        }

        /// <summary>
        /// Consume SOLO en el sillar (_zoneY0.._zoneFloorY1, ver constante):
        /// aquí, y no en todo el pozo, es donde vive la caída visible que
        /// pidió el playtest anterior. Lo que hay más arriba lo trae
        /// <see cref="ArrastreTick"/> hasta aquí, arquetipo aparte -- por eso
        /// esta función ya no necesita distinguir sólidos de líquidos.
        /// </summary>
        private void ConsumeTick()
        {
            for (int x = _zoneX0; x <= _zoneX1; x++)
            {
                for (int y = _zoneY0; y <= _zoneFloorY1; y++)
                {
                    byte matId = (byte)_sim.SampleMaterial(x, y);
                    if (matId == MaterialId.Empty) continue;

                    // Solo la PIEDRA se ignora (es el propio nicho). Antes se
                    // ignoraba todo sólido estático, lo que hacía IMPOSIBLE
                    // entregar Cristal o Hielo — justo lo que piden los encargos
                    // de cristal y de "algo helado" de las jornadas 2 y 3.
                    if (matId == MaterialId.Stone) continue;

                    byte tempRaw = _sim.Grid.temp[CellGrid.Idx(x, y)];
                    var outcome = _orderSystem.TryDeliverCell(_sim.Universe, matId, tempRaw);
                    bool matched = outcome == OrderSystem.DeliveryOutcome.Progressed;
                    if (!matched)
                    {
                        // (fix playtest 9) Ya NO se acumula "chatarra" para
                        // pagar Favor -- ver docblock de la clase. Lo único
                        // que queda es avisar de POR QUÉ esta celda no contó,
                        // distinguiendo los dos motivos: la Tolva ya no dice
                        // una cosa y hace la contraria.
                        _lastMismatchWasCompletedOrder = outcome == OrderSystem.DeliveryOutcome.OrderAlreadyComplete;

                        // (fix playtest 8, "Además"; motivo añadido en
                        // playtest 9) Primera vez que ESTE material concreto
                        // sale como no-contado: se lo decimos por su nombre Y
                        // por el motivo, una sola vez -- reutilizando el
                        // mismo rótulo de mundo que ya usa el resto de la
                        // Tolva, no un sistema de mensajes nuevo. Las
                        // siguientes veces vuelve al aviso corto genérico de
                        // OnGUI (que también respeta el motivo, ver
                        // _lastMismatchWasCompletedOrder): ya lo sabe, no
                        // hace falta repetirle la frase larga cada entrega.
                        if (!_scrapWarned[matId])
                        {
                            _scrapWarned[matId] = true;
                            string nombre = _orderSystem.NombreParaMensaje(matId);
                            _scrapMsg = _lastMismatchWasCompletedOrder
                                ? $"\"{nombre}\" ya no hace falta -- ese encargo está completo. Se traga igual, pero no suma Favor."
                                : $"\"{nombre}\" no lo pide ningún encargo activo. Se traga igual, pero no suma Favor.";
                            _scrapMsgHasta = Time.time + ScrapEducationSeconds;
                        }
                    }

                    // Prioridad al verde: si en el mismo chorro entra algo que SÍ
                    // encaja, el jugador ve "aceptado" y no el aviso de descarte.
                    if (matched) _flashAceptado = true;
                    else if (Time.time >= _flashHasta) _flashAceptado = false;
                    _flashHasta = Time.time + FlashSeconds;

                    _sim.Paint(x, y, 0, MaterialId.Empty);
                }
            }
        }

        /// <summary>
        /// LA GARGANTA ARRASTRA (fix playtest 8): dentro del pozo, tira de
        /// TODA celda no vacía una fila hacia su suelo si esa fila está
        /// libre -- sin mirar arquetipo. Es lo que hace que un sólido
        /// estático (Cristal, Hielo) vertido a media altura no se quede
        /// flotando ahí para siempre esperando una regla de caída que
        /// SimStepper nunca le va a dar (StaticSolid no tiene Move()).
        ///
        /// Recorre filas de ABAJO HACIA ARRIBA (de _zoneFloorY1+1, la primera
        /// fuera del sillar, hasta _zoneY1, el labio): así, cuando una celda
        /// baja a la fila que se acaba de procesar, esa fila ya no se vuelve
        /// a visitar este tick para ESA celda original -- ninguna celda
        /// concreta cae más de 1 fila en esta llamada. Una columna entera
        /// apilada SÍ se ve descender 1 fila de golpe (cada celda, la suya
        /// propia), que es sedimentación normal; lo que se evita es que una
        /// única celda "teletransporte" varias filas en un solo tick, que es
        /// justo lo que pasaría recorriendo de arriba hacia abajo (la celda
        /// de arriba encontraría, en la misma pasada, el hueco que acaba de
        /// dejar libre la de abajo, y seguiría cayendo sin parar).
        ///
        /// Usa CellGrid.SwapCells (mismo helper que SimStepper.Move) para
        /// mover mat+temp+aux juntos de una sola vez, y WakeChunk en origen y
        /// destino para que el chunk se despierte igual que si lo hubiera
        /// movido el propio sim. Cero asignaciones: todo son índices e ints.
        /// </summary>
        private void ArrastreTick()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper.Tick;

            for (int x = _zoneX0; x <= _zoneX1; x++)
            {
                for (int y = _zoneFloorY1 + 1; y <= _zoneY1; y++)
                {
                    int idx = CellGrid.Idx(x, y);
                    if (grid.mat[idx] == MaterialId.Empty) continue;

                    int belowIdx = CellGrid.Idx(x, y - 1);
                    if (grid.mat[belowIdx] != MaterialId.Empty) continue;

                    grid.SwapCells(idx, belowIdx);
                    grid.WakeChunk(x, y, tick);
                    grid.WakeChunk(x, y - 1, tick);
                }
            }
        }

        // -----------------------------------------------------------------
        // Rótulo
        // -----------------------------------------------------------------
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return; // (playtest 21) HudSilenciado, hermano de InputLocked.

            UiStyles.Preparar();

            string texto;
            Color color;
            if (Time.time < _scrapMsgHasta)
            {
                // (fix playtest 8) El aviso educativo "una vez por material"
                // pisa al destello normal mientras dura: es más largo a
                // propósito (ver ScrapEducationSeconds) y solo se dispara una
                // vez por material, así que merece prioridad sobre el pulso
                // genérico de aceptado/descarte.
                texto = _scrapMsg;
                color = UiStyles.Aviso;
            }
            else if (Time.time < _flashHasta)
            {
                // (fix playtest 9) El destello corto y recurrente (toda
                // entrega, no solo la primera por material) también respeta
                // el motivo -- literales fijos, sin concatenar cada frame.
                texto = _flashAceptado ? "¡ENTREGA ACEPTADA!"
                    : _lastMismatchWasCompletedOrder ? "ese encargo ya está completo"
                    : "material equivocado (ningún encargo lo pide)";
                color = _flashAceptado ? UiStyles.Exito : UiStyles.Aviso;
            }
            else if (_esBuzonSemillaCero)
            {
                // (playtest 54) EL BUZÓN: rótulo SOLO DE PROXIMIDAD (contrato
                // textual: "que aparezca... con desvanecimiento, patrón
                // existente"), ya no gateado por MachineFocus.MostrarPromptE
                // -- ese era el criterio de "tutorial aprendido tras 2 usos"
                // del taller clásico (regla 12 nunca lo exigió aquí, era una
                // elección del playtest 16); el Buzón es la ÚNICA vía de
                // entrega del arco guiado, así que se señala siempre que el
                // jugador esté cerca, toda la partida, no solo las primeras
                // veces. UiStyles.PlacaMundo YA se desvanece sola por
                // AlfaMinimaVisible (regla 28 de CLAUDE.md) y por estar fuera
                // de cuadro (mismo criterio que EtiquetaMundo, fix playtest
                // 16) -- no hace falta ningún `return` manual por distancia.
                // Texto acortado (contrato, textual): ya NO explica el gesto
                // completo ("vierte con clic derecho") en el CUERPO del
                // rótulo permanente -- esa instrucción vive ahora en el
                // consejo de primer pedido de Game/SemillaCero.cs (pt50,
                // archivo ajeno, sin cambios).
                float cercaniaTexto = UiStyles.Cercania(transform.position, _player, BrilloRangoPleno, BrilloRangoDesvanece);
                Color oro = UiStyles.Oro;
                UiStyles.PlacaMundo(new Vector3(transform.position.x, _boRotuloY, 0f),
                    "BUZÓN DEL MAESTRO — vierte aquí lo pedido",
                    new Color(oro.r, oro.g, oro.b, cercaniaTexto), UiStyles.S(30f));
                return; // PlacaMundo ya decidió si dibujar o no -- no caer al EtiquetaMundo clásico de abajo.
            }
            else if (MachineFocus.MostrarPromptE)
            {
                // (fix playtest 16: "debería desaparecer tras unas pocas
                // veces") Reutiliza TAL CUAL el mismo flag global que ya usa
                // el resto del taller para callar su prompt de texto ("E —
                // regular el fuego", "E — abrir"...) tras UsosParaAprender=2
                // usos -- ver Game/MachineFocus.cs y el docblock de esta
                // clase. La Tolva nunca LLAMA a RegistrarUsoE (no se
                // interactúa con E), solo LEE si el jugador ya se graduó del
                // tutorial en CUALQUIER aparato del taller; en cuanto lo hace,
                // esta rama deja de alcanzarse para el resto de la partida.
                //
                // (playtest 54) SOLO CAÓTICO/MULTI llega aquí ahora -- Semilla
                // Cero se resuelve en la rama de arriba (_esBuzonSemillaCero).
                // El texto/color de la Tolva clásica quedan SIN CAMBIOS
                // (contrato "el caótico no cambia").
                texto = "TOLVA DEL MAESTRO — vierte AQUÍ";
                color = UiStyles.Oro;
            }
            else
            {
                // (fix playtest 16) Enseñado: nada que escribir. La única
                // señal que queda es visual -- ver AnimarMarco, que ilumina
                // jambas/labio/flecha según la cercanía del aprendiz, igual
                // que el halo dorado de foco sustituye al prompt de texto en
                // el resto de las máquinas.
                return;
            }

            // Solo llegan aquí abajo el aviso educativo y el destello corto
            // (ambos SIN CAMBIOS en las dos vistas, ver arriba) -- la rama
            // Buzón ya retornó desde su propio bloque. Ancla sobre la flecha
            // en la vista clásica, sobre el relieve de pergamino en el
            // Buzón (playtest 54: `_flechaY` nunca se fija en
            // BuildVisualBuzon, así que hay que leer `_boRotuloY` ahí en vez
            // de dejarlo en su valor por defecto 0).
            //
            // UiStyles YA NO acota el rótulo al borde de la pantalla (fix
            // playtest 16, ver UiStyles.cs): con la cámara siguiendo al
            // aprendiz, la boca -pegadísima al muro derecho en el caótico-
            // pasa buena parte del tiempo fuera de cuadro, y ahora su rótulo
            // simplemente no se dibuja en ese caso, en vez de perseguir al
            // jugador clavado en el borde de la pantalla.
            float anclaY = _esBuzonSemillaCero ? _boRotuloY : _flechaY;
            UiStyles.EtiquetaMundo(new Vector3(transform.position.x, anclaY, 0f), texto, color, UiStyles.S(26f));
        }
    }
}
