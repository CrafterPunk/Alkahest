using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Net;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Placa ígnea: el aparato empotrado bajo una cuba que inyecta calor en las
    /// filas de celdas justo encima suyo. Tres intensidades (APAGADA / TEMPLADA /
    /// ARDIENTE) que se ciclan pulsando E cerca de ella.
    ///
    /// ---------------------------------------------------------------------
    /// AVISO DE PÉRDIDA DE TRABAJO (restaurado playtest 7 / fix playtest 14)
    /// ---------------------------------------------------------------------
    /// En el commit e3fed6f (playtest 10) este archivo se SOBRESCRIBIÓ con una
    /// copia obsoleta anterior al playtest 7 durante un despliegue, y se perdió
    /// TODO ese trabajo: los tres anillos de rótulo por cercanía, el halo de
    /// resalte del aparato enfocado y el límite de dos usos del prompt
    /// "E — ...". Reconstruido aquí a partir de
    /// /home/claude/restore/p9/HeatPlate.cs (commit 2ef67e5, último bueno antes
    /// de la pérdida), fusionado a mano con lo que se hizo después (guardas de
    /// atajos, regla 12). La MISMA regresión también se llevó por delante
    /// <see cref="UiStyles.Cercania"/> en UiStyles.cs -- ya RESTAURADA ahí (fix
    /// playtest 14), así que este archivo la usa directamente en vez de una
    /// copia local. SEÑAL DE ALARMA si esto vuelve a perderse: un grep de
    /// MachineFocus.MostrarPromptE en Game/{Dispenser,ChillStone,HeatPlate}.cs
    /// debe dar TRES resultados (uno por archivo) — si vuelve a dar cero, es la
    /// MISMA regresión otra vez.
    ///
    /// ---------------------------------------------------------------------
    /// CAMBIOS DEL PLAYTEST 4
    /// ---------------------------------------------------------------------
    /// 1. IDENTIDAD VISUAL PROPIA. Antes la placa era literalmente una barra de
    ///    0.06 unidades de alto que cambiaba de color: no parecía un aparato,
    ///    parecía una raya. Ahora es un CHASIS de metal remachado (generado por
    ///    código, sin assets) con una ventana por la que se ven sus RESISTENCIAS
    ///    naranjas serpenteando; las resistencias se encienden y laten según la
    ///    intensidad, así que el estado se lee desde el otro lado del taller.
    ///
    /// 2. RÓTULO FIJO Y PEQUEÑO ("el label de las placas tapa la interacción al
    ///    aspirar"). La chapa de identificación va SIEMPRE debajo del chasis,
    ///    sobre la piedra del suelo — nunca dentro de la cuba, que es donde se
    ///    aspira — y es diminuta (UiStyles.PlacaMundo). El prompt "E — regular"
    ///    solo aparece si estás cerca, con las manos libres, y solo las dos
    ///    primeras veces del taller (restaurado playtest 7:
    ///    MachineFocus.MostrarPromptE — a partir de ahí lo sustituye el RESALTE
    ///    dorado del aparato enfocado, ver ActualizarResalte). El chasis se
    ///    apoya en el suelo, al pie de la cuba, justo donde el aprendiz se
    ///    planta para pulsar E: _centroChasis SÍ está donde el jugador trabaja
    ///    (a diferencia de ChillStone, no hizo falta un ancla de labio aparte).
    ///
    /// 3. TEMPLADA = LA BANDA DE CRECIMIENTO DE ESTE UNIVERSO. Antes "Tibia"
    ///    fijaba raw 140 (¡160 °C!) y "Caliente" raw 220 (320 °C): el Vivium
    ///    muere carbonizado por encima de 120 °C y crece entre ~30 y ~60 °C, así
    ///    que NINGUNA posición de la placa permitía cultivar — el arco de
    ///    domesticación entero (decisión §14) era inalcanzable salvo por
    ///    accidente en el gradiente térmico. Ahora TEMPLADA apunta al centro
    ///    exacto de la banda de crecimiento de la seed (Universe.VivGrowMinRaw/
    ///    MaxRaw) y ARDIENTE sigue siendo el fuego de verdad (320 °C, por encima
    ///    de cualquier temperatura de ignición sorteable).
    ///
    /// LIMITACIÓN: escribe _sim.Grid.temp[] directamente en vez de pasar por una
    /// API dedicada del simulador. TODO(ChaosAlchemy): canalizar por
    /// AlkahestSim.InjectHeat de cara al netcode.
    ///
    /// ---------------------------------------------------------------------
    /// MEDICIÓN CONTRA ChillStone (playtest 13, NO TOCADA por este fix salvo el
    /// perfil de caída de abajo): "la placa fría parece irradiar más fuerte,
    /// tardar más en recuperarse y tener más alcance". RowsAffected (3, ahora
    /// sustituido por el perfil de caída, ver más abajo) y TempStepPerTick (5)
    /// YA eran IDÉNTICOS a los de ChillStone -- no había nada que igualar aquí
    /// en alcance/velocidad de empuje. La asimetría real estaba en que
    /// ChillStone SOLO tenía un estado activo (el extremo) mientras esta clase
    /// ya ofrecía TEMPLADA como opción cercana a ambiente; se corrigió
    /// añadiendo FRESCA a ChillStone (ver esa clase), NO tocando nada de esta.
    /// A diferencia de ChillStone, aquí NO hay una versión "rápida" de
    /// ARDIENTE: ARDIENTE es genuinamente necesaria para encender fuego de
    /// verdad (temperatura que TEMPLADA nunca alcanza), así que ya tenía una
    /// razón de ser propia -- el análisis de "¿hace falta un estado extra
    /// rápido?" (fix playtest 14) solo aplicaba a ChillStone.
    ///
    /// ---------------------------------------------------------------------
    /// PERFIL DE CAÍDA DE TEMPERATURA (fix playtest 14, "esperaría que el calor
    /// irradie moderadamente alrededor" / "el frío sigue llegando al grifo")
    /// ---------------------------------------------------------------------
    /// El texto "(alcanza N filas)" del playtest 13 se ha QUITADO (no
    /// comunicaba nada). El empuje de temperatura por tick DECAE con la
    /// distancia a la placa en vez de ser uniforme y cortarse en seco: fila
    /// adyacente al 100% del empuje, cada fila siguiente más débil (ver
    /// <c>FilaEmpujePct</c>, RETIRADO en el playtest 44, para los números
    /// viejos/nuevos y la distancia medida al grifo, MISMO array que
    /// ChillStone.cs, duplicado a propósito -- "aplícalo IGUAL en los dos
    /// aparatos"). SEGUNDA pasada de este mismo playtest 14: la primera
    /// extendió el perfil de 3 a 5 filas; esta lo recorta a 3 filas con
    /// caída más agresiva. SimStepper.DiffuseTemperature (regla 9 de
    /// CLAUDE.md) NO se toca: el perfil vive enteramente en
    /// <see cref="ApplyHeatTick"/>.
    ///
    /// Aparte, y esto NO se toca porque es geometría de nivel (Sim/
    /// SimLevelBuilder.cs, fuera de estos dos archivos): la bandeja fría
    /// (9 filas de alto, 6 útiles) es mucho más DELGADA que una cuba (40
    /// filas de alto, 36 útiles), así que estas filas de calor cubren una
    /// fracción menor de la profundidad útil de la cuba que las mismas filas
    /// de frío en la bandeja. Es la diferencia física esperable entre "bandeja
    /// fina apoyada sobre un enfriador" y "caldero hondo sobre una hornilla
    /// que calienta desde el fondo" — no es un desequilibrio entre estos dos
    /// scripts, es el contenedor que Sim/SimLevelBuilder.cs les da a cada uno.
    ///
    /// Y algo geométricamente IMPOSIBLE que el jugador esperaba sin saberlo:
    /// esta placa calienta la cuba en la que vive (y=14..53); la bandeja fría
    /// está en y=88..96, 35 filas de aire vacío más arriba, y el material
    /// Empty no participa en la difusión de temperatura (docs/SIM_NOTES.md).
    /// Ninguna hornilla puede combatir el frío de la bandeja por difusión —
    /// son cubetas sin contacto térmico entre sí.
    ///
    /// ---------------------------------------------------------------------
    /// ORDEN DE RECUPERACIÓN: "LO ÚLTIMO EN NORMALIZARSE ES LA PLACA" (fix
    /// playtest 14)
    /// ---------------------------------------------------------------------
    /// Mismo reporte y mismo mecanismo que en <see cref="ChillStone"/> (ver
    /// esa clase para la explicación completa): no se puede tocar
    /// SimStepper.DiffuseTemperature (regla 9), así que al apagarse (ARDIENTE
    /// -&gt; APAGADA) la fila adyacente (índice 0 del perfil) sigue sujeta
    /// hacia el último objetivo activo durante <see cref="HoldTicksTrasApagar"/>
    /// ticks más, con un empuje mínimo (<see cref="HoldStepRaw"/>, ver
    /// <see cref="ApplyHoldTick"/>) que solo contrarresta el tirón de vuelta a
    /// ambiente sin seguir calentando nada nuevo (mismo clamp que
    /// <see cref="ApplyHeatTick"/>). Las filas 1 y 2 se sueltan de inmediato,
    /// así que se enfrían ANTES que la fila 0 -- la placa siempre es la
    /// última en volver a temperatura normal. Números idénticos a
    /// ChillStone.cs a propósito.
    ///
    /// ---------------------------------------------------------------------
    /// TAMAÑO DEL APARATO (fix playtest 14, "las placas son demasiado
    /// grandes")
    /// ---------------------------------------------------------------------
    /// AlkahestGameBootstrap.cs (NO EDITABLE en este encargo) pasa a
    /// <see cref="Init"/> el interior ÚTIL COMPLETO de la cuba
    /// (<see cref="SimLevelBuilder.VatInteriorX0"/>/X1, 52 celdas) como
    /// <c>cellX0</c>/<c>cellX1</c> -- antes el aparato ocupaba ese ancho
    /// entero, una losa que cubría todo el fondo de la cuba. Ahora
    /// <see cref="Init"/> lo recorta a una FRACCIÓN centrada
    /// (<see cref="FootprintFraction"/>=0.4, ~21 de 52 celdas) ANTES de que
    /// <see cref="BuildVisual"/> calcule nada, así que el sprite (chasis +
    /// resistencias) Y la zona de efecto (<see cref="ApplyHeatTick"/>, que
    /// recorre <c>_cellX0.._cellX1</c>) quedan automáticamente coherentes
    /// entre sí. Recorte SIMÉTRICO: el centro X no se mueve, así que
    /// <c>PuntoFoco</c> y el ancla de los rótulos siguen exactamente donde
    /// estaban. Mismo criterio y misma fracción que ChillStone.cs (ver esa
    /// clase para la propuesta de cara al taller movible).
    ///
    /// ---------------------------------------------------------------------
    /// TALLER MOVIBLE (playtest 19, Game/Mudanza.cs, tecla V)
    /// ---------------------------------------------------------------------
    /// Implementa <see cref="IMovible"/>: Mudanza puede agarrar esta placa y
    /// recolocarla en cualquier celda dentro del alcance del jugador. El
    /// movimiento de verdad lo hace <see cref="Reposicionar"/>, que NO llama
    /// ni a <see cref="Init"/> ni a <see cref="BuildVisual"/> otra vez --
    /// ver el docblock de ese método para el porqué exacto (en corto:
    /// MaquinariaSprites.CrearCapa siempre crea un GameObject nuevo, así que
    /// una segunda llamada DUPLICARÍA el chasis/las resistencias/el resalte
    /// en vez de reemplazarlos, dejando los viejos huérfanos y visibles en
    /// el sitio antiguo para siempre). El ancho del chasis
    /// (<c>_cellX1-_cellX0+1</c>) es invariante tras <see cref="Init"/> --
    /// Mudanza solo TRASLADA, nunca redimensiona -- así que en la práctica
    /// <see cref="Reposicionar"/> nunca necesita pedir un sprite distinto;
    /// se deja documentado en el propio método qué haría si algún día
    /// tuviera que hacerlo.
    ///
    /// ---------------------------------------------------------------------
    /// LA FÍSICA HONESTA (playtest 44, ronda nocturna, docs/CONTRATO_TERMICA.md
    /// §2b) -- REHECHA LA EMISIÓN ENTERA
    /// ---------------------------------------------------------------------
    /// Mandato de Cesar: *"antes el frío irradiaba mucho y el calor
    /// poquito"* + físicas realistas (calienta de a pocos, con relación real
    /// entre distancia y empuje). El <c>FilaEmpujePct</c> del fix
    /// playtest 14 (perfil fijo {100,45,15}%, paso CONSTANTE hasta clampear
    /// en seco contra el objetivo) SE RETIRA: pasa a vivir en
    /// <see cref="Sim.EmisionTermica"/> (Sim/SimStepper.cs, compartido al
    /// 100% con <see cref="ChillStone"/> -- la simetría entre los dos
    /// aparatos ahora es LITERAL, misma función, mismas constantes, no solo
    /// "el mismo criterio aplicado a mano dos veces"), que combina
    /// DECAIMIENTO POR DISTANCIA continuo (cuadrático, no un array de 3
    /// escalones) con EMPUJE POR DIFERENCIA -- Newton de juguete: el paso se
    /// autofrena según la celda se acerca al objetivo, en vez de empujar a
    /// paso fijo hasta clampear. Ver el docblock de
    /// <see cref="Sim.EmisionTermica"/> para las MEDICIONES que llevaron al
    /// diseño final (collar de contención incluido) -- resumen: un perfil
    /// falloff+Newton "puro" satura el footprint entero al objetivo tarde o
    /// temprano y la difusión compartida (regla 9 de CLAUDE.md, NO tocada)
    /// no tiene radio de corte, así que hace falta un COLLAR que tire
    /// activamente hacia ambiente justo detrás del footprint para cumplir el
    /// ±10°C a 12 celdas del contrato. Las escrituras pasan ahora por
    /// <see cref="AlkahestSim.InyectarTemperatura"/> (creada esta ronda,
    /// cierra la deuda que este mismo docblock anotaba desde el playtest 4:
    /// "escribe _sim.Grid.temp[] directamente en vez de pasar por una API
    /// dedicada").
    ///
    /// ---------------------------------------------------------------------
    /// LA "N ROJA" SE RETIRA (playtest 48, CONTRATO_RONDA48.md §3a: "el
    /// calentador de la N roja es horrible")
    /// ---------------------------------------------------------------------
    /// El chasis metálico remachado con la ventana de resistencias en
    /// zigzag (<c>MaquinariaSprites.ChasisPlaca</c>/<c>ResistenciasPlaca</c>,
    /// IDEA DESCARTADA, regla 15 de CLAUDE.md -- ver el docblock de esas dos
    /// funciones, renombradas <see cref="MaquinariaSprites.LosaPlaca"/> y
    /// <see cref="MaquinariaSprites.LechoBrasasPlaca"/>, para el porqué
    /// completo) se sustituye por una LOSA DE PIEDRA con un LECHO DE BRASAS
    /// incrustado en un nicho recesado -- mismo lenguaje visual que las
    /// brasas reales del hogar/Crisol (playtest 39/41) y misma construcción
    /// por bloques con juntas que <see cref="ChillStone"/> (misma fórmula de
    /// tamaño, mismo <c>Escala</c>): las dos placas de zona se leen ahora
    /// como HERMANAS de una sola familia. Apagada, las brasas quedan en
    /// ceniza gris tenue (nunca del todo negras: sigue habiendo un rescoldo
    /// mínimo, mismo criterio que el hogar del Crisol -- un fuego que se ve
    /// encendido cuando no lo está miente sobre el estado de la máquina);
    /// encendida (Templada o Ardiente), late en naranja-rojo profundo con un
    /// shimmer suave en el filete de contacto superior (donde se apoya la
    /// cuba) -- ver <see cref="MaquinariaSprites.LechoBrasasPlaca"/> para el
    /// porqué el shimmer no cuesta ninguna animación aparte. El campo
    /// <c>_resistencias</c>/los métodos <c>ColorResistencia</c>/
    /// <c>AnimarResistencias</c> se quedan con el mismo nombre (regla de
    /// mínimo diff: son la MISMA capa, solo cambia lo que dibuja el sprite
    /// que sostienen) -- lo que cambió es el sprite, no el mecanismo de
    /// tinte/latido, que ya era correcto.
    ///
    /// EL RÓTULO DE OFICIO (§3b): <see cref="ChapaNombre"/> pasa de "placa
    /// ígnea" a la frase que describe lo que HACE ("PLACA DE CALOR -- entibia
    /// la ZONA de encima"), y la chapa de ESTADO cambia de
    /// "TEMPLADA 64°"/"ARDIENTE 220°" a un VERBO + temperatura
    /// ("calentando · 64°") -- ver <see cref="RebuildChapaEstado"/>.
    ///
    /// ---------------------------------------------------------------------
    /// FUERA EL HALO, TEMPLADA SALE DEL CICLO, Y EL OFICIO SE EXPLICA
    /// (playtest 51, feedback de Cesar en el playtest 50b)
    /// ---------------------------------------------------------------------
    /// Cesar, jugando: (1) *"¿para qué sirve la placa de calor? ni siquiera
    /// consigue la arena tostada aunque llegue a 320°; no sé para qué sirve
    /// el estado tibio"*; (2) *"quítale esa luz horrible -- el rojito
    /// encendido encima de la placa no tiene lugar en nuestro mundito, quiero
    /// ver las partículas reales quemándose o evaporándose"*.
    ///
    /// 1. FUERA EL HALO (IDEA DESCARTADA, regla 15 de CLAUDE.md -- no se borra
    ///    en silencio). El campo <c>_brillo</c> ("BrilloCalor", playtest 49,
    ///    ver <see cref="MaquinariaSprites.Halo"/>) era una capa flotante
    ///    DETRÁS de la losa que latía naranja-rojo con la placa encendida --
    ///    exactamente el "rojito encendido" que Cesar pidió quitar. Retirado
    ///    ENTERO (campo, creación en BuildVisual, ActualizarBrillo() y su
    ///    llamada desde AnimarResistencias): el espectáculo real de "algo se
    ///    está calentando" ya lo dan las animaciones del CA mismo (Game/
    ///    ParticulasFx.cs + el propio material calentándose/hirviendo/
    ///    ardiendo en la sim), no un halo de sprite superpuesto. El latido
    ///    del SERPENTÍN dentro de la textura (<see cref="ColorResistencia"/>/
    ///    <see cref="AnimarResistencias"/>) SE QUEDA: no es un halo flotante,
    ///    es el propio aparato calentándose, visible solo de cerca -- la
    ///    distinción exacta que separa "instrumento honesto" de "luz de
    ///    neón" que pedía el playtest 48/49.
    ///
    /// 2. TEMPLADA SALE DEL CICLO DE E (el código NO se borra). <see cref="CycleState"/>
    ///    ciclaba Off-&gt;Templada-&gt;Ardiente-&gt;Off (mod 3); ahora es un flip de
    ///    DOS estados, Off&lt;-&gt;Ardiente. TEMPLADA era la banda de crecimiento
    ///    del Vivium (playtest 4, ver el punto 3 más arriba) -- con el Vivium
    ///    APARCADO desde el playtest 25 ("LO QUE PERSISTE", spawns comentados)
    ///    ya no tiene ningún consumidor real en Semilla Cero ni en el
    ///    laboratorio clásico: es exactamente el hueco que denuncia la regla
    ///    48 de CLAUDE.md ("cada estado necesita un verbo visible Y un
    ///    consumidor real") y la causa de que Cesar no encontrara para qué
    ///    servía. <c>State.Templada</c>, <c>_templadaRaw</c> y las ramas
    ///    <c>case State.Templada</c> de <see cref="ColorResistencia"/>/
    ///    <see cref="StateLabel"/>/<see cref="RebuildChapaEstado"/> se dejan
    ///    INTACTOS a propósito (regla 15): si el Vivium vuelve a jugarse, el
    ///    paso de vuelta es reinsertar Templada en el ciclo, no reinventar la
    ///    banda térmica. <see cref="IMaquinaUsableRemota.EstadoVivoRed"/> no
    ///    necesitó tocarse: ya reduce cualquier estado activo a un solo bit
    ///    <c>Trabajando</c> (+ <c>FuegoEncendido</c> solo en Ardiente), y
    ///    <c>Net/MaquinaReplica.cs</c> tampoco imprime nunca el nombre crudo
    ///    del estado (verificado: <c>DescribirTrabajo</c> dice "trabajando...",
    ///    nunca "TEMPLADA") -- ningún camino, local o de red, puede mostrar
    ///    ya ese texto.
    ///    DEUDA FUERA DE ESTE ARCHIVO (no editable en este encargo): dos
    ///    textos siguen invitando a "TEMPLADA" que ya no existe en el ciclo --
    ///    <c>Game/HintSystem.cs</c> ("En la CUBA DERECHA, pon la placa en
    ///    TEMPLADA (E)") y <c>Game/JournalHud.cs</c> (dos menciones, la
    ///    receta de cultivo del Vivium). Como el Vivium ya estaba aparcado
    ///    antes de esta ronda, es muy probablemente deuda YA existente (el
    ///    hint/receta de un sistema que ya no se juega), no una regresión
    ///    nueva de este cambio -- pero queda anotada para quien retome esos
    ///    dos archivos.
    ///
    /// 3. EL OFICIO EXPLICADO, NO SOLO NOMBRADO. El rótulo de proximidad de
    ///    cerca gana una segunda línea que se enseña de nuevo EN CADA
    ///    acercamiento (rearmada por transición lejos-&gt;cerca, ver
    ///    <see cref="_oficioVisibleHasta"/>/<see cref="OnGUI"/> -- a
    ///    diferencia de <see cref="ChapaNombre"/>, que se apaga para siempre
    ///    tras aprenderse una vez): "hierve, derrite y seca la ZONA -- transformar
    ///    materia es oficio del CRISOL". Responde de raíz a "¿por qué no
    ///    consigo arena tostada aunque llegue a 320°?": las transiciones del
    ///    RETÍCULO (Polvo-&gt;Calcinado/Fundido/etc., <see cref="EstadoMateria"/>,
    ///    CONTRATO_PERSISTE.md §2) son POR HORNADA -- decisión estructural del
    ///    playtest 27, <c>Game/Crisol.DecidirHornada</c>, que exige una
    ///    CÁMARA cerrada con combustible propio -- y esta placa NUNCA pasa
    ///    por ahí: solo empuja <c>grid.temp[]</c> hacia un objetivo (ver
    ///    <see cref="ApplyHeatTick"/>). Que la arena caliente a 320°C no se
    ///    calcine sola sobre la placa es la FÍSICA CORRECTA del juego, sin
    ///    cartel que lo explicara -- no un bug, el diagnóstico exacto que
    ///    pide la regla 30 de CLAUDE.md antes de tocar código: el "culpable"
    ///    no era esta clase, era la ausencia de un rótulo que dijera "aquí NO
    ///    se transforma nada, solo se cuece lo que ya traigas". Lo que la
    ///    placa SÍ hace de verdad -- hervir agua, fundir hielo/oleaginosos,
    ///    secar hasta el punto de ignición -- lo dice la línea nueva sin
    ///    prometer una capacidad que no tiene.
    /// </summary>
    public sealed class HeatPlate : MonoBehaviour, IMaquinaInteractiva, IMovible, IMaquinaUsableRemota
    {
        private enum State { Off = 0, Templada = 1, Ardiente = 2 }

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        /// <summary>Radio de interacción con E (ESCALA COMPARTIDA con Dispenser/ChillStone, ver ambos archivos).</summary>
        private const float ProximityRange = 3.2f;
        private const byte ArdienteRaw = 220; // ~320 °C, SIN CAMBIOS (el contrato pide conservar el fuego de verdad).

        /// <summary>
        /// (RETIRADO playtest 44, regla 15 de CLAUDE.md -- no se borra en
        /// silencio) Hasta esta ronda, el empuje térmico vivía aquí: paso FIJO
        /// (<c>TempStepPerTick=5</c>) escalado por un perfil de PORCENTAJES
        /// fijo por fila (<c>FilaEmpujePct = {100,45,15}</c>, calibrado a ojo
        /// en el fix playtest 14 contra la distancia al grifo). El paso nunca
        /// se frenaba solo: empujaba a ritmo constante hasta CLAMPEAR contra
        /// el objetivo, no hasta "acercarse" a él. El contrato de esta ronda
        /// (docs/CONTRATO_TERMICA.md §2b) pide un modelo físico de verdad
        /// (decaimiento continuo + empuje proporcional a la diferencia, Newton
        /// de juguete) compartido EXACTAMENTE con <see cref="ChillStone"/> --
        /// ahora vive en <see cref="Sim.EmisionTermica"/> (Sim/SimStepper.cs),
        /// con las mediciones que llevaron a las constantes finales en su
        /// propio docblock. Ver <see cref="ApplyHeatTick"/>.
        /// </summary>
        private const int RetiradoVerDocArriba = 0; // ancla del docblock de arriba a un miembro real (regla 15: no se borra en silencio, pero tampoco se deja un XML doc huérfano sin destino).

        /// <summary>(fix playtest 14, ver doc de clase "TAMAÑO DEL APARATO") Fracción del ancho recibido en Init que ocupa de verdad el aparato, centrada. Mismo valor que ChillStone.cs.</summary>
        private const float FootprintFraction = 0.4f;

        /// <summary>(fix playtest 14, ver doc de clase "ORDEN DE RECUPERACIÓN") Empuje mínimo que sujeta la fila adyacente tras apagarse. Mismo valor que ChillStone.cs.</summary>
        private const int HoldStepRaw = 1;
        /// <summary>(fix playtest 14) Ticks tras apagarse durante los que la fila adyacente sigue sujeta (2 s a 30 Hz). Mismo valor que ChillStone.cs.</summary>
        private const int HoldTicksTrasApagar = 60;

        private AlkahestSim _sim;
        private Transform _player;
        private int _cellX0, _cellX1, _plateRow;
        private State _state = State.Off;
        private float _accumulator;

        /// <summary>Objetivo de TEMPLADA: centro de la banda de crecimiento del Vivium de ESTA seed (ver doc de la clase).</summary>
        private byte _templadaRaw = 82;

        /// <summary>(fix playtest 14) Objetivo activo justo antes de apagarse, hacia el que sigue sujeta la fila adyacente durante el hold -- ver ApplyHoldTick.</summary>
        private byte _lastActiveTarget;
        /// <summary>(fix playtest 14) Cuenta atrás de ticks del hold de apagado -- 0 = suelto del todo. Ver ApplyHoldTick.</summary>
        private int _holdTicksRestantes;

        private SpriteRenderer _resistencias;
        private Vector3 _centroChasis;

        /// <summary>(restaurado playtest 7) Capa de resalte dorado del aparato enfocado, ver ActualizarResalte.</summary>
        private SpriteRenderer _resalte;
        private float _alfaResalte;

        /// <summary>
        /// (playtest 51, ver el docblock de clase "EL OFICIO SE EXPLICA") Reloj de
        /// visibilidad de la segunda línea del rótulo ("hierve, derrite y seca la
        /// ZONA..."): mientras <c>Time.time &lt; _oficioVisibleHasta</c> la línea se
        /// dibuja. Se REARMA cada vez que el aprendiz ENTRA en el anillo de nombre
        /// (transición lejos-&gt;cerca, ver <see cref="_dentroDeRangoNombreAnterior"/>
        /// en <see cref="OnGUI"/>), a diferencia de <see cref="ChapaNombre"/> (que se
        /// apaga para siempre tras <see cref="_yaConocida"/>): esta línea sigue
        /// recordándose en CADA acercamiento, aprendida o no, porque es la que
        /// responde a "¿para qué sirve esto?", una pregunta que Cesar seguía
        /// teniendo bien entrada la partida.
        /// </summary>
        private float _oficioVisibleHasta;
        /// <summary>Estado del frame anterior del anillo de nombre, para detectar la transición lejos-&gt;cerca que rearma <see cref="_oficioVisibleHasta"/>. Ver <see cref="OnGUI"/>.</summary>
        private bool _dentroDeRangoNombreAnterior;

        // ---------------------------------------------------------------
        // ESCALA COMPARTIDA DE CERCANÍA DEL TALLER (restaurado playtest 7,
        // duplicada a propósito en ChillStone.cs; usa UiStyles.Cercania,
        // restaurada en UiStyles.cs en el fix playtest 14).
        //  · RangoEstado: de lejos, SOLO el estado de trabajo (si lo hay).
        //  · RangoNombre: de cerca, además el nombre del aparato — pero solo
        //    hasta que el aprendiz ya lo conoce (ver _yaConocida).
        // ---------------------------------------------------------------
        private const float RangoEstadoPleno = 5.0f;
        private const float RangoEstadoDesvanece = 6.5f;
        private const float RangoNombrePleno = 2.6f;
        private const float RangoNombreDesvanece = 3.6f;

        /// <summary>(playtest 51) Cuánto dura visible la línea de OFICIO tras rearmarse en cada acercamiento. Ver <see cref="_oficioVisibleHasta"/>.</summary>
        private const float OficioDuracionSeg = 6f;

        /// <summary>
        /// Aprendizaje del taller (restaurado playtest 7): el aprendiz ya ha
        /// estado lo bastante cerca como para saber qué es este aparato, así
        /// que su rótulo de NOMBRE no vuelve a dibujarse en lo que dure la
        /// partida. Campo de instancia a propósito — NO estático, NO
        /// PlayerPrefs: cada partida nueva empieza sin nada aprendido.
        /// </summary>
        private bool _yaConocida;

        /// <summary>Chapa del anillo de ESTADO, cacheada: solo se reconstruye al cambiar de estado (nunca dentro de OnGUI, regla de cero asignaciones por frame).</summary>
        private string _chapaEstado;

        /// <summary>(playtest 48, CONTRATO_RONDA48.md §3b: "cada fuego dice su oficio") Ya no es solo un nombre -- dice lo que HACE.</summary>
        private const string ChapaNombre = "PLACA DE CALOR — entibia la ZONA de encima";

        /// <summary>
        /// (playtest 51, feedback de Cesar en el playtest 50b: "¿para qué sirve la
        /// placa de calor? ni siquiera consigue la arena tostada aunque llegue a
        /// 320°") Segunda línea del rótulo -- ver el docblock de clase "EL OFICIO
        /// SE EXPLICA" para por qué esta placa NUNCA transforma materia (las
        /// transiciones del retículo son por HORNADA, decisión del Crisol) y qué
        /// SÍ hace de verdad.
        /// </summary>
        private const string ChapaOficio = "Hierve, derrite y seca la ZONA — transformar materia es oficio del CRISOL.";

        // Foco de interacción: solo el aparato MÁS CERCANO responde a E y
        // muestra su prompt (ver Game/MachineFocus.cs).
        public Vector3 PuntoFoco => _centroChasis;
        public float RangoFoco => ProximityRange;

        // ---------------------------------------------------------------
        // IMovible (playtest 19, ver doc de clase "TALLER MOVIBLE" y
        // Game/Mudanza.cs para el contrato completo).
        // ---------------------------------------------------------------
        public Vector3 CentroMundo => _centroChasis;
        public Vector2 TamanoMundo => new Vector2(
            (_cellX1 - _cellX0 + 1) * SimRenderer.CellWorldSize,
            SimLevelBuilder.WallThickness * SimRenderer.CellWorldSize);
        /// <summary>Celda de anclaje: borde IZQUIERDO del chasis (X0) + fila del SUELO bajo él (_plateRow). El ancho (span) no viaja en la ancla -- es invariante, ver Reposicionar.</summary>
        public Vector2Int AnclaCelda => new Vector2Int(_cellX0, _plateRow);

        /// <summary>¿Cabría el chasis (mismo ancho de siempre x WallThickness de alto) en esa ancla sin tocar el marco protegido del mundo? Puramente informativo -- Mudanza es quien decide si bloquea el drop con esto.</summary>
        public bool CabeEnAncla(Vector2Int anclaCelda)
        {
            int span = _cellX1 - _cellX0 + 1;
            int x0 = anclaCelda.x, x1 = x0 + span - 1;
            int filaInferior = anclaCelda.y - SimLevelBuilder.WallThickness + 1;
            return x0 >= 1 && x1 <= CellGrid.W - 2 && filaInferior >= 1 && anclaCelda.y <= CellGrid.H - 2;
        }

        /// <summary>Inyección de dependencias desde AlkahestGameBootstrap.</summary>
        public void Init(AlkahestSim sim, Transform player, int cellX0, int cellX1, int plateRow)
        {
            _sim = sim;
            _player = player;

            // (fix playtest 14, ver doc de clase "TAMAÑO DEL APARATO") Recorta
            // el ancho recibido (interior COMPLETO de la cuba) a una fracción
            // centrada ANTES de que BuildVisual/ApplyHeatTick lean
            // _cellX0/_cellX1 -- sprite y zona de efecto quedan coherentes
            // entre sí sin duplicar el cálculo del recorte.
            int spanTotal = cellX1 - cellX0 + 1;
            int spanReducido = Mathf.Max(8, Mathf.RoundToInt(spanTotal * FootprintFraction));
            int margen = (spanTotal - spanReducido) / 2;
            _cellX0 = cellX0 + margen;
            _cellX1 = _cellX0 + spanReducido - 1;
            _plateRow = plateRow;

            if (_sim != null && _sim.Universe != null)
            {
                int centro = (_sim.Universe.VivGrowMinRaw + _sim.Universe.VivGrowMaxRaw) / 2;
                _templadaRaw = (byte)Mathf.Clamp(centro, 1, 254);
            }

            BuildVisual();
            UpdateVisualTint();
            RebuildChapaEstado();
            MachineFocus.Registrar(this);
            Mudanza.RegistrarMovible(this); // (playtest 19) ver doc de clase "TALLER MOVIBLE".
        }

        private void OnDestroy()
        {
            MachineFocus.Olvidar(this);
            Mudanza.OlvidarMovible(this);
        }

        /// <summary>
        /// (playtest 19) Recalcula <see cref="_centroChasis"/> y mueve
        /// transform.position a partir de _cellX0/_cellX1/_plateRow. Extraído
        /// de BuildVisual para que <see cref="Reposicionar"/> pueda
        /// reutilizarlo SIN volver a crear ningún GameObject: el ancho
        /// (span) y el alto (WallThickness) del chasis son constantes tras
        /// Init, así que "mover el aparato" es solo recalcular DÓNDE cae ese
        /// rectángulo fijo -- nunca su tamaño.
        /// </summary>
        private void RecalcularCentro()
        {
            float celda = SimRenderer.CellWorldSize;
            int spanCeldas = _cellX1 - _cellX0 + 1;
            // El chasis ocupa las filas de piedra del SUELO de la cuba (las
            // WallThickness filas que terminan en _plateRow): la cuba se apoya
            // encima del aparato, que es exactamente lo que cuenta la fantasía.
            int filaInferior = _plateRow - SimLevelBuilder.WallThickness + 1;
            float centroX = (_cellX0 + spanCeldas * 0.5f) * celda;
            float centroY = (filaInferior + (_plateRow + 1 - filaInferior) * 0.5f) * celda;
            _centroChasis = new Vector3(centroX, centroY, 0f);
            transform.position = _centroChasis;
        }

        /// <summary>
        /// TALLER MOVIBLE (playtest 19, Game/Mudanza.cs): mueve el aparato YA
        /// CONSTRUIDO a una nueva celda de anclaje, SIN volver a llamar a
        /// Init ni a BuildVisual.
        ///
        /// POR QUÉ NO BuildVisual: MaquinariaSprites.CrearCapa siempre hace
        /// `new GameObject` -- una segunda llamada NO reemplaza el chasis/
        /// las resistencias/el resalte, los DUPLICA: los hijos originales se
        /// quedarían huérfanos y visibles en el sitio ANTIGUO para siempre
        /// (nadie los destruye ni los mueve). Aquí no se toca ningún
        /// GameObject: Resalte/Chasis/Resistencias son hijos de `transform`
        /// con localPosition (0,0,0) -- basta con mover
        /// `transform.position` (ver RecalcularCentro) y los tres se
        /// arrastran solos con él.
        ///
        /// POR QUÉ NO Init: Init también recalibra _templadaRaw (inofensivo
        /// repetirlo dentro de la misma partida/seed) pero, sobre todo,
        /// reinicia _state a Off implícitamente si se reconstruyera todo
        /// desde cero -- este método deja _state, _lastActiveTarget y
        /// _holdTicksRestantes completamente intactos a propósito: mover una
        /// placa ENCENDIDA no debe apagarla.
        ///
        /// EL ANCHO NUNCA CAMBIA en esta llamada -- Mudanza solo TRASLADA,
        /// nunca redimensiona -- así que el sprite ya cacheado (mismo ancho
        /// de siempre) sigue siendo válido sin tocarlo. Si algún día algo
        /// pidiera un ancho distinto, el punto correcto sería reasignar
        /// `SpriteRenderer.sprite` desde MaquinariaSprites (que cachea por
        /// ancho, así que no generaría textura nueva) y re-escalar
        /// `transform.localScale` de cada capa -- NUNCA llamar a CrearCapa
        /// otra vez, por la misma razón de arriba.
        /// </summary>
        public void Reposicionar(Vector2Int anclaCelda)
        {
            int span = _cellX1 - _cellX0 + 1; // invariante, ver doc de arriba.
            _cellX0 = anclaCelda.x;
            _cellX1 = _cellX0 + span - 1;
            _plateRow = anclaCelda.y;
            RecalcularCentro();
        }

        // -----------------------------------------------------------------
        // Visual: chasis remachado + resistencias serpenteantes, generados.
        // -----------------------------------------------------------------
        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;
            int spanCeldas = _cellX1 - _cellX0 + 1;

            RecalcularCentro();
            int filaInferior = _plateRow - SimLevelBuilder.WallThickness + 1;
            float anchoMundo = spanCeldas * celda;
            float altoMundo = (_plateRow + 1 - filaInferior) * celda;

            // Resalte de foco (restaurado playtest 7, ver ActualizarResalte):
            // capa DETRÁS de las demás (sortingOrder menor que Chasis=18),
            // copia del sprite principal agrandada ~15%/35% y teñida de oro; al
            // ser mayor asoma por los bordes del chasis como un halo. Se crea
            // UNA vez aquí; en Update solo se le cambia el color (cero
            // allocs/frame).
            _resalte = MaquinariaSprites.CrearCapa(transform, "Resalte", MaquinariaSprites.LosaPlaca(spanCeldas), 16,
                anchoMundo * 1.15f, altoMundo * 1.35f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);

            // (playtest 49, RETIRADO en el playtest 51 -- ver el docblock de
            // clase "FUERA EL HALO, TEMPLADA SALE DEL CICLO, Y EL OFICIO SE
            // EXPLICA") Aquí vivía "BrilloCalor", una capa de halo naranja
            // DEBAJO de la losa (sortingOrder 17). Cesar la reportó
            // literalmente ("el rojito encendido encima de la placa no tiene
            // lugar"): eliminada entera, ninguna capa nueva la sustituye --
            // el latido del serpentín en la textura de abajo ya comunica
            // "encendida" sin sprite flotante.

            MaquinariaSprites.CrearCapa(transform, "Losa", MaquinariaSprites.LosaPlaca(spanCeldas), 18,
                anchoMundo, altoMundo);
            _resistencias = MaquinariaSprites.CrearCapa(transform, "Serpentin",
                MaquinariaSprites.SerpentinPlaca(spanCeldas), 19, anchoMundo, altoMundo);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan la placa.

            // (fix playtest 10) E es un atajo de una sola tecla: no puede robarle letras al
            // campo de bautizar ni competir con el diario a pantalla completa (ver el mismo
            // comentario en Game/ChillStone.cs). El calor de más abajo sigue su curso igual
            // con el libro abierto -- solo se calla el ciclo de intensidad.
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto && EstaEnfocada()) // (integración pt50, regla 12) la ficha modal también bloquea E.
            {
                CycleState();
            }

            if (_state != State.Off)
            {
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
                {
                    ApplyHeatTick();
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;
            }
            else if (_holdTicksRestantes > 0)
            {
                // (fix playtest 14, ver doc de clase "ORDEN DE RECUPERACIÓN")
                // Apagada, pero la fila adyacente todavía sujeta un rato:
                // mismo bucle de acumulador que arriba, sobre una sola fila.
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame && _holdTicksRestantes > 0)
                {
                    ApplyHoldTick();
                    _holdTicksRestantes--;
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;
            }

            AnimarResistencias();
        }

        /// <summary>¿Es ESTE el aparato que el aprendiz tiene delante? (ver Game/MachineFocus.cs)</summary>
        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        private void CycleState()
        {
            bool estabaActiva = _state != State.Off;
            byte objetivoPrevio = TargetRaw(); // objetivo del estado ANTES de cambiarlo.
            // (playtest 51, ver el docblock de clase "TEMPLADA SALE DEL
            // CICLO") Antes ciclaba mod 3 (Off->Templada->Ardiente->Off);
            // TEMPLADA se retira del ciclo de E -- ahora es un flip de DOS
            // estados. State.Templada/_templadaRaw NO se borran (regla 15):
            // si el Vivium vuelve a jugarse, reinsertar el paso intermedio
            // es el único cambio que haría falta aquí.
            _state = _state == State.Off ? State.Ardiente : State.Off;

            if (_state == State.Off && estabaActiva)
            {
                // (fix playtest 14, ver doc de clase "ORDEN DE RECUPERACIÓN")
                // Al apagarse de verdad (siempre se llega a Off desde ARDIENTE
                // en este ciclo), arma el hold de la fila adyacente.
                _lastActiveTarget = objetivoPrevio;
                _holdTicksRestantes = HoldTicksTrasApagar;
            }

            UpdateVisualTint();
            RebuildChapaEstado();
            MachineFocus.RegistrarUsoE(); // (restaurado playtest 7) el estado cambió de verdad: cuenta como un uso aprendido de E.
            Debug.Log($"[ChaosAlchemy] Placa ígnea -> {StateLabel()} ({CellGrid.RawToC(TargetRaw())} °C)");
        }

        private byte TargetRaw() => _state == State.Ardiente ? ArdienteRaw : _templadaRaw;

        /// <summary>
        /// (playtest 44, LA FÍSICA HONESTA) Empuja la temperatura de las filas
        /// por encima de la placa hacia <see cref="TargetRaw"/> usando el
        /// modelo COMPARTIDO de <see cref="Sim.EmisionTermica"/> (mismo código
        /// que <see cref="ChillStone.ApplyColdTick"/>, ver su docblock para
        /// las mediciones): las primeras <see cref="Sim.EmisionTermica.RadioFilas"/>
        /// filas reciben el empuje por diferencia con caída cuadrática por
        /// distancia (<see cref="Sim.EmisionTermica.PasoFootprint"/>); las
        /// filas siguientes, hasta <see cref="Sim.EmisionTermica.CollarFilas"/>
        /// más allá, son el COLLAR de contención que tira activamente hacia
        /// ambiente (<see cref="Sim.EmisionTermica.PasoCollar"/>) para que la
        /// fuga de largo alcance de la difusión compartida (regla 9 de
        /// CLAUDE.md, no tocada) no rebase el ±10°C a 12 celdas del contrato.
        /// Escribe por <see cref="AlkahestSim.InyectarTemperatura"/> (Paint
        /// discipline), nunca directo sobre <c>grid.temp</c>.
        /// </summary>
        private void ApplyHeatTick()
        {
            byte target = TargetRaw();
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            int filasTotal = EmisionTermica.RadioFilas + EmisionTermica.CollarFilas;

            for (int x = _cellX0; x <= _cellX1; x++)
            {
                for (int fila = 0; fila < filasTotal; fila++)
                {
                    int y = _plateRow + fila + 1;
                    if (!CellGrid.InBounds(x, y)) continue;
                    int idx = CellGrid.Idx(x, y);
                    int cur = grid.temp[idx];
                    int delta = fila < EmisionTermica.RadioFilas
                        ? EmisionTermica.PasoFootprint(cur, target, fila, tick, x, y, EmisionTermica.Direccion.SoloSube) // (pt50, D1) LA LÍNEA: una estufa no enfría.
                        : EmisionTermica.PasoCollar(cur);
                    if (delta == 0) continue;
                    int next = Mathf.Clamp(cur + delta, 0, 255);
                    _sim.InyectarTemperatura(x, y, (byte)next);
                }
            }
        }

        /// <summary>
        /// (fix playtest 14, ver doc de clase "ORDEN DE RECUPERACIÓN"; escritura
        /// migrada a <see cref="AlkahestSim.InyectarTemperatura"/> en el
        /// playtest 44) Tras apagarse, sujeta SOLO la fila adyacente hacia
        /// <see cref="_lastActiveTarget"/> con un empuje mínimo
        /// (<see cref="HoldStepRaw"/>) durante <see cref="_holdTicksRestantes"/>
        /// ticks más. Mismo clamp que <see cref="ApplyHeatTick"/>: nunca deja
        /// pasar el objetivo, así que solo MANTIENE la celda donde ya estaba
        /// mientras el resto del footprint (no tocado aquí) ya vuelve a
        /// ambiente libremente.
        /// </summary>
        private void ApplyHoldTick()
        {
            var grid = _sim.Grid;
            int y = _plateRow + 1; // solo la fila adyacente.

            for (int x = _cellX0; x <= _cellX1; x++)
            {
                if (!CellGrid.InBounds(x, y)) continue;
                int idx = CellGrid.Idx(x, y);
                int cur = grid.temp[idx];
                // (pt50, D1) El hold también respeta el signo del aparato: si
                // la celda YA está por encima del último objetivo, la estufa
                // apagada no la enfría — la suelta (el mundo la normaliza).
                if (cur >= _lastActiveTarget) continue;
                int next = Mathf.Min(_lastActiveTarget, cur + HoldStepRaw);
                _sim.InyectarTemperatura(x, y, (byte)next);
            }
        }

        // ---------------------------------------------------------------
        // IMaquinaUsableRemota (playtest 44, docs/CONTRATO_TERMICA.md §1/§2b,
        // API congelada por el contrato de paridad playtest 43): UsarPorRed
        // ejecuta EXACTAMENTE lo mismo que la E local -- literalmente
        // CycleState(), el mismo método privado que Update() llama tras
        // comprobar EstaEnfocada() (esa comprobación de proximidad es del
        // lado del anfitrión y no aplica aquí, ver docblock de la interfaz).
        // EstadoVivoRed: Trabajando para cualquier estado activo (Templada o
        // Ardiente), FuegoEncendido SOLO para Ardiente -- el fuego de verdad,
        // nunca para Templada (que ni siquiera enciende materia inflamable).
        // ---------------------------------------------------------------
        bool IMaquinaUsableRemota.UsarPorRed()
        {
            CycleState();
            return true;
        }

        byte IMaquinaUsableRemota.EstadoVivoRed()
        {
            byte b = 0;
            if (_state != State.Off) b |= EstadoVivoBits.Trabajando;
            if (_state == State.Ardiente) b |= EstadoVivoBits.FuegoEncendido;
            return b;
        }

        private void UpdateVisualTint()
        {
            if (_resistencias == null) return;
            _resistencias.color = ColorResistencia(1f);
        }

        /// <summary>
        /// Las brasas respiran: apagadas son ceniza gris tenue (un rescoldo
        /// mínimo, nunca del todo negras -- playtest 48, ver el docblock de
        /// la clase), templadas laten ámbar, ardientes laten blanco-naranja.
        /// Ya se llama en TODOS los frames (a diferencia de ChillStone, aquí
        /// no vivía dentro de la rama "encendida"), así que basta con
        /// colgarle el resalte de foco al final para que también lata
        /// siempre (restaurado playtest 7).
        /// </summary>
        private void AnimarResistencias()
        {
            if (_resistencias != null && _state != State.Off)
            {
                float pulso = 0.82f + 0.18f * Mathf.Sin(Time.time * (_state == State.Ardiente ? 8f : 3.4f));
                _resistencias.color = ColorResistencia(pulso);
            }

            ActualizarResalte();
        }

        /// <summary>
        /// RESALTE del aparato enfocado (restaurado playtest 7: sustituye al
        /// prompt de texto permanente como señal de "puedes actuar aquí" — ver
        /// MachineFocus.MostrarPromptE). Alfa 0 sin foco; con foco, late entre
        /// 0.40 y 0.80. Se interpola con MoveTowards en vez de asignar el
        /// objetivo directamente para que un objetivo que oscila en cada frame
        /// (el propio latido) y las entradas/salidas de foco no produzcan
        /// parpadeos bruscos. Sin allocs: Color es struct.
        /// </summary>
        private void ActualizarResalte()
        {
            if (_resalte == null) return;
            float objetivo = EstaEnfocada() ? 0.60f + 0.20f * Mathf.Sin(Time.time * 4f) : 0f;
            _alfaResalte = Mathf.MoveTowards(_alfaResalte, objetivo, 6f * Time.deltaTime);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, _alfaResalte);
        }

        private Color ColorResistencia(float pulso)
        {
            switch (_state)
            {
                case State.Ardiente: return new Color(1f, 0.52f * pulso, 0.22f * pulso, 1f);
                case State.Templada: return new Color(1f * pulso, 0.58f * pulso, 0.16f * pulso, 1f);
                // (playtest 48, RECALIBRADO en el 49) Apagada: NICROMO FRÍO.
                // El serpentín se dibuja siempre (SerpentinPlaca, ver su
                // docblock) y este tinte solo decide qué tan vivo se ve. Sube
                // de (0.24,0.21,0.19) a (0.30,0.26,0.23) porque ahora la
                // incandescencia vive DENTRO de la textura: apagado tiene que
                // quedar un alambre gris tibio claramente visible contra la
                // boca casi negra del nicho (0x100A08) -- una placa apagada
                // que se confunde con una losa lisa fue exactamente el fallo
                // que Cesar reportó del playtest 48. Matiz cálido residual,
                // nunca gris puro ni negro.
                default: return new Color(0.30f, 0.26f, 0.23f, 1f);
            }
        }

        private string StateLabel()
        {
            if (_state == State.Ardiente) return "ARDIENTE";
            if (_state == State.Templada) return "TEMPLADA";
            return "APAGADA";
        }

        /// <summary>
        /// Reconstruye la chapa del anillo de ESTADO. Se llama SOLO al cambiar
        /// de estado (restaurado playtest 7, nunca desde OnGUI): el raw objetivo
        /// de cada estado es constante mientras dura ese estado, así que el
        /// texto no cambia frame a frame (regla de cero asignaciones por
        /// frame). Ya NO incluye "(alcanza N filas)" (playtest 13, quitado en
        /// el fix playtest 14: no comunicaba nada -- ver el perfil de caída
        /// real en ApplyHeatTick).
        /// </summary>
        private void RebuildChapaEstado()
        {
            // (playtest 48, CONTRATO_RONDA48.md §3a) VERBO + temperatura en
            // vez del nombre de estado crudo: "calentando · 64°" dice lo que
            // el aparato está HACIENDO, no un identificador interno
            // (TEMPLADA/ARDIENTE sigue vivo en StateLabel() para el log
            // técnico de CycleState() y para el color del rótulo, que ya
            // distingue Templada/Ardiente por sí solo -- ver OnGUI).
            _chapaEstado = _state == State.Off
                ? null // apagada: nada que anunciar de lejos.
                : "calentando · " + CellGrid.RawToC(TargetRaw()) + "°";
        }

        /// <summary>
        /// (restaurado playtest 7) Reescrito para volver a los TRES anillos de
        /// rótulo por cercanía (estado / nombre / prompt), en vez del booleano
        /// "cerca"/"lejos" que trajo la copia obsoleta del playtest 10.
        /// </summary>
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return; // (playtest 21) HudSilenciado, hermano de InputLocked.

            // Salida temprana: si el aprendiz está fuera de los dos anillos, no
            // hay nada que dibujar -- ni siquiera Preparar().
            float cercaniaEstado = UiStyles.Cercania(_centroChasis, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercaniaNombre = UiStyles.Cercania(_centroChasis, _player, RangoNombrePleno, RangoNombreDesvanece);

            // (playtest 51) Rearma el reloj de la línea de OFICIO en cada
            // transición lejos->cerca del anillo de nombre -- ANTES del return
            // temprano de abajo, para que una salida COMPLETA del rango
            // (cercaniaNombre llega a 0) sí baje el flag y el siguiente
            // acercamiento cuente como uno nuevo.
            bool dentroDeRangoNombre = cercaniaNombre > 0f;
            if (dentroDeRangoNombre && !_dentroDeRangoNombreAnterior) _oficioVisibleHasta = Time.time + OficioDuracionSeg;
            _dentroDeRangoNombreAnterior = dentroDeRangoNombre;

            if (cercaniaEstado <= 0f && cercaniaNombre <= 0f) return;

            // Aprendizaje: una vez el aprendiz entra de lleno en el anillo de
            // nombre, la placa queda "conocida" para el resto de la partida y
            // su chapa de nombre deja de dibujarse.
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();

            Color color = _state == State.Ardiente ? UiStyles.Peligro
                        : _state == State.Templada ? UiStyles.Aviso
                        : UiStyles.TextoTenue;

            // 1) Anillo de ESTADO: solo mientras trabaja, y SOLO el estado — nunca
            //    el nombre del aparato aquí (eso es información de reconocimiento,
            //    no de "¿dejé esto encendido?"). Desplazamiento NEGATIVO = hacia
            //    abajo, sobre la piedra del suelo, nunca dentro de la cuba.
            if (_state != State.Off && _chapaEstado != null)
            {
                UiStyles.PlacaMundo(_centroChasis, _chapaEstado,
                    new Color(color.r, color.g, color.b, color.a * cercaniaEstado), -UiStyles.S(17f));
            }

            // 2) Anillo de NOMBRE: solo hasta que el aprendiz ya sabe qué es esto.
            if (!_yaConocida)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroChasis, ChapaNombre,
                    new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaniaNombre), -UiStyles.S(34f));
            }

            // 2b) OFICIO (playtest 51): a diferencia del nombre de arriba, esta
            // línea NO se apaga para siempre -- se rearma en cada acercamiento
            // (ver el bloque justo antes del return temprano), porque responde a
            // "¿para qué sirve esto?", la pregunta que Cesar seguía teniendo bien
            // entrada la partida, no solo la primera vez que vio el aparato.
            if (Time.time < _oficioVisibleHasta)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroChasis, ChapaOficio,
                    new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaniaNombre), -UiStyles.S(50f));
            }

            // 3) PROMPT: además de foco + manos libres, solo las dos primeras
            //    veces del taller (MachineFocus.MostrarPromptE); a partir de ahí
            //    la única señal de "puedes actuar aquí" es el RESALTE dorado
            //    (ver ActualizarResalte), no un texto permanente.
            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centroChasis, "E — regular el fuego",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercaniaNombre), -UiStyles.S(34f));
            }
        }
    }
}
