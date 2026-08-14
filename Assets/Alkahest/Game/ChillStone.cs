using UnityEngine;
using UnityEngine.InputSystem;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// Piedra gélida: el aparato empotrado bajo la bandeja fría del estante
    /// superior. Enfría las filas de celdas justo encima suya. Tres estados
    /// (APAGADA / FRESCA / HELANDO), ciclados con E — mismo patrón que
    /// <see cref="HeatPlate"/> (APAGADA / TEMPLADA / ARDIENTE).
    ///
    /// -----------------------------------------------------------------------
    /// AVISO DE PÉRDIDA DE TRABAJO (restaurado playtest 7 / fix playtest 14)
    /// -----------------------------------------------------------------------
    /// En el commit e3fed6f (playtest 10) este archivo se SOBRESCRIBIÓ con una
    /// copia obsoleta anterior al playtest 7 durante un despliegue, y se perdió
    /// TODO ese trabajo: los tres anillos de rótulo por cercanía, el anclaje al
    /// LABIO de la bandeja (en vez de al bloque empotrado bajo el suelo), el
    /// halo de resalte del aparato enfocado y el límite de dos usos del prompt
    /// "E — ...". El playtest 13 (Fresca) se escribió DESPUÉS y ENCIMA de esa
    /// copia obsoleta, así que su código de estados/calibración por seed sigue
    /// siendo el bueno y NO se toca; lo que se reconstruye aquí es solo la capa
    /// visual perdida, fusionada a mano con Fresca a partir de
    /// /home/claude/restore/p9/ChillStone.cs (commit 2ef67e5, último bueno
    /// antes de la pérdida). La MISMA regresión también se llevó por delante
    /// <see cref="UiStyles.PlacaMundoLateral"/> y <see cref="UiStyles.Cercania"/>
    /// en UiStyles.cs -- ya RESTAURADOS ahí (fix playtest 14), así que este
    /// archivo usa <see cref="UiStyles.Cercania"/> directamente en vez de una
    /// copia local (PlacaMundoLateral no hace falta aquí: la piedra es un
    /// único aparato, no una columna que necesite anclaje lateral, así que
    /// sus tres anillos siguen usando UiStyles.PlacaMundo). SEÑAL DE ALARMA si
    /// esto vuelve a perderse: un grep de <see cref="MachineFocus.MostrarPromptE"/>
    /// en Game/{Dispenser,ChillStone,HeatPlate}.cs debe dar TRES resultados
    /// (uno por archivo) — si vuelve a dar cero, es la MISMA regresión otra vez.
    ///
    /// -----------------------------------------------------------------------
    /// FRESCA (playtest 13, NO TOCADO por este fix salvo lo indicado abajo)
    /// -----------------------------------------------------------------------
    /// Reporte del jugador: "la placa fría parece irradiar más fuerte el frío
    /// que el calor, y tardar más en recuperar su temperatura a 0, además de
    /// tener más alcance". MEDIDO antes de tocar nada:
    ///  · Antes de este fix, ChillStone tenía UN SOLO estado activo: raw 20
    ///    (-80 °C), 50 unidades raw (100 °C) por DEBAJO de ambiente (raw 70,
    ///    20 °C). HeatPlate en cambio SIEMPRE tuvo un estado moderado
    ///    (TEMPLADA, calibrado al centro de la banda de crecimiento del
    ///    Vivium de la seed) además del extremo ARDIENTE. FRESCA cierra esa
    ///    asimetría: se calibra por seed al mínimo entre el punto de
    ///    congelación del agua y el umbral de cristalización de ESTA seed, con
    ///    margen de 10 raw (20 °C) por fiabilidad. HELANDO se conserva intacto
    ///    (raw 20 / -80 °C, ver más abajo el fix playtest 14 sobre para qué
    ///    sirve de verdad).
    ///
    /// -----------------------------------------------------------------------
    /// "PIEDRA HELADA PARECE NO TENER USO" (fix playtest 14)
    /// -----------------------------------------------------------------------
    /// Investigado con números reales antes de decidir nada (Sim/Universe.cs +
    /// Sim/SimStepper.cs, ninguno tocado):
    ///  · El agua se congela con una comparación de UMBRAL, no de intensidad:
    ///    <c>SimStepper.ApplyPhase</c> transforma Agua-&gt;Hielo en cuanto
    ///    <c>t &lt;= def.freezesAt</c>. Cruzar el umbral por 1 raw o por 50
    ///    raw dispara EXACTAMENTE la misma transición.
    ///  · La cristalización (Azoth+CrystalSeed) es un CHEQUEO DE PROBABILIDAD
    ///    fijo (<c>crystallizeChancePct</c>, ~12%/comprobación) que solo exige
    ///    <c>temp &lt;= CrystallizeMaxTempRaw</c>; de nuevo, estar MUY por
    ///    debajo del umbral no sube esa probabilidad ni un punto.
    ///  · FRESCA ya se calibra (ver arriba) para cruzar ambos umbrales con
    ///    margen de sobra en CUALQUIER seed (freezesAt raw ~[50,67],
    ///    CrystallizeMaxTempRaw raw ~[62,70] -&gt; FRESCA ronda raw [40,57],
    ///    siempre por debajo de los dos).
    ///  · El encargo "algo helado" de OrderSystem pide -5 °C o menos: FRESCA
    ///    (hasta -40 °C según seed) lo cumple siempre con margen.
    ///  CONCLUSIÓN MEDIDA: con el reglaje de <c>TempStepPerTick</c> que había
    ///  hasta ahora (igual para los dos estados), HELANDO no desbloqueaba
    ///  NADA que FRESCA no lograra ya -- el jugador tenía razón.
    ///
    ///  DECISIÓN (en vez de quitarlo): darle una razón real de existir en vez
    ///  de borrar un estado entero y su lectura pedagógica ("hay un extremo Y
    ///  un término medio", el mismo patrón que ya tiene HeatPlate). HELANDO
    ///  pasa a empujar la temperatura MÁS RÁPIDO por tick que FRESCA (mismo
    ///  destino final -80 °C que siempre tuvo, pero llega a él antes) --
    ///  "lo necesito YA" en vez de "lo necesito MÁS". Como el umbral de
    ///  congelación/cristalización es el MISMO para ambos estados y el empuje
    ///  por tick es lo único que determina cuántos ticks tarda en cruzarlo,
    ///  HELANDO cruza ese umbral en MENOS ticks que FRESCA -- una ventaja
    ///  real y medible (menos tiempo de pie junto a la bandeja), no cosmética.
    ///  El coste de esa velocidad ("a cambio de pasarse de frío"): como HELANDO
    ///  sigue empujando hacia su propio destino mucho más frío que el umbral
    ///  que hacía falta cruzar, la celda queda MUY por debajo de él -- y por
    ///  el mismo razonamiento del fix de FRESCA de arriba (el tirón de vuelta a
    ///  ambiente de SimStepper.DiffuseTemperature es un paso FIJO, no
    ///  proporcional a la distancia), esa celda tarda MUCHO MÁS en volver a
    ///  descongelarse si se apaga la piedra o se aleja del aparato. HELANDO es
    ///  la opción de urgencia con una factura después; FRESCA es la opción
    ///  cómoda para el uso diario. Ver <see cref="TempStepHelando"/>/
    ///  <see cref="TempStepFresca"/> y el rótulo de estado en OnGUI, que ahora
    ///  insinúa esta diferencia ("· más rápido").
    ///
    /// -----------------------------------------------------------------------
    /// PERFIL DE CAÍDA DE TEMPERATURA (fix playtest 14, "esperaría que el frío
    /// irradie moderadamente alrededor" / "el frío sigue llegando al grifo")
    /// -----------------------------------------------------------------------
    /// El texto "(alcanza N filas)" del playtest 13 explicaba el corte en seco
    /// en vez de arreglarlo, y no comunicaba nada por sí solo -- se ha
    /// QUITADO. En su lugar el empuje de temperatura por tick DECAE con la
    /// distancia a la piedra en vez de ser uniforme y cortarse de golpe:
    /// fila adyacente al 100% del empuje, cada fila siguiente más débil (ver
    /// <see cref="FilaEmpujePct"/> para los números viejos/nuevos y la
    /// distancia medida al grifo de agua -- SEGUNDA pasada de este mismo
    /// playtest 14: la primera dejó 5 filas, demasiado alcance total una vez
    /// se deja la difusión trabajar minutos seguidos; recortado a 3 filas
    /// con caída más agresiva). Suelo de 1 raw/tick para que la fila más
    /// lejana del perfil siga sintiéndose (aunque débil) en vez de quedar
    /// inerte por redondeo a 0. SimStepper.DiffuseTemperature (regla 9 de
    /// CLAUDE.md) NO se toca: este perfil vive enteramente en
    /// <see cref="ApplyColdTick"/>, que ya era el único sitio donde este
    /// aparato escribe temperatura. Coste acotado: siguen siendo bucles
    /// enteros baratos (ancho de bandeja x 3 filas, sin asignaciones), nada
    /// de raíces cuadradas ni floats por celda. Aplicado IGUAL en
    /// HeatPlate.cs (mismo array <c>FilaEmpujePct</c>, duplicado a propósito).
    ///
    /// LO QUE NO SE TOCÓ, a propósito: el ALCANCE geométrico base y la
    /// simetría con HeatPlate (misma familia de constantes, mismo patrón de
    /// ciclo de 3 estados). La sensación de "la placa ígnea no combate el frío
    /// tan rápido como esperaría" con las hornillas cerca de la bandeja fría es,
    /// medida la geometría real (Sim/SimLevelBuilder.cs, NO TOCADO), una
    /// EXPECTATIVA IMPOSIBLE: la bandeja fría vive en y=88..96 y las cubas de
    /// las hornillas terminan en su labio en y=53 -- 35 filas de aire vacío de
    /// por medio, y el material Empty no participa en la difusión de
    /// temperatura (ver docs/SIM_NOTES.md). Ninguna hornilla puede calentar la
    /// bandeja fría por difusión salvo que gas o fuego de verdad vuele
    /// físicamente hasta allí arriba. Es información de diseño, no un bug.
    ///
    /// -----------------------------------------------------------------------
    /// ORDEN DE RECUPERACIÓN: "LO ÚLTIMO EN NORMALIZARSE ES LA PLACA" (fix
    /// playtest 14)
    /// -----------------------------------------------------------------------
    /// Reporte: al apagar la piedra tras tenerla encendida solo un momento,
    /// el agua lejana (la que menos frío había recibido) tardaba en
    /// descongelarse, y para cuando lo hacía la PROPIA piedra ya estaba
    /// "lista" -- al revés de la intuición física (la fuente debería ser lo
    /// último en enfriarse Y lo último en volver a la normalidad). No se
    /// puede tocar SimStepper.DiffuseTemperature (regla 9), así que el
    /// mecanismo vive aquí: al apagarse (transición HELANDO/FRESCA -&gt;
    /// APAGADA), la FILA ADYACENTE (índice 0 del perfil, la que recibe el
    /// 100% del empuje mientras trabaja) sigue "sujeta" hacia el ÚLTIMO
    /// objetivo activo durante <see cref="HoldTicksTrasApagar"/> ticks más,
    /// con un empuje mínimo (<see cref="HoldStepRaw"/>, ver
    /// <see cref="ApplyHoldTick"/>) que solo CONTRARRESTA el tirón de vuelta
    /// a ambiente -- no sigue enfriando nada nuevo, el mismo clamp de
    /// <see cref="ApplyColdTick"/> impide pasarse del objetivo, así que como
    /// mucho la MANTIENE donde estaba. Las filas 1 y 2 (las más alejadas) se
    /// sueltan de inmediato -- dejan de recibir CUALQUIER empuje en el mismo
    /// tick en que se apaga -- así que empiezan a volver a ambiente ANTES
    /// que la fila 0. Resultado: la fila más próxima al aparato (la que el
    /// jugador identifica como "la piedra") es SIEMPRE la última en volver a
    /// temperatura normal. Mecanismo elegido por ser DETERMINISTA (cuenta
    /// atrás de ticks fija, sin RNG) y SIN ASIGNACIONES (mismo patrón de
    /// bucle que ApplyColdTick, solo que sobre una única fila). Duplicado a
    /// propósito en HeatPlate.cs (<c>ApplyHoldTick</c> allí también, mismos
    /// números) -- alternativa descartada: soltar las filas "de fuera hacia
    /// dentro" con temporizadores por fila habría necesitado un array de
    /// contadores (una asignación menos trivial de evitar) para un resultado
    /// perceptualmente casi idéntico; sujetar solo la fila 0 es más simple y
    /// ya basta, porque es la única fila cuyo objetivo real (FRESCA/HELANDO)
    /// se aleja lo bastante de ambiente como para que el orden se note.
    ///
    /// -----------------------------------------------------------------------
    /// POSICIÓN DEL RÓTULO DE FRÍO (fix playtest 14, tercera vez que se toca
    /// esta decisión -- LEER ANTES DE CAMBIAR EL SIGNO DE NADA AQUÍ)
    /// -----------------------------------------------------------------------
    /// Cronología completa para que esto no vuelva a invertirse:
    ///  1) Playtest 7: el rótulo colgaba de <c>_centroBloque</c> (el bloque
    ///     de piedra EMPOTRADO BAJO EL SUELO de la bandeja) con desplazamiento
    ///     HACIA ARRIBA -- se ancló entonces a un punto nuevo, el LABIO
    ///     superior de la bandeja (<c>_anclaRotulo</c>), también con
    ///     desplazamiento hacia arriba (S(17)/S(34)/S(51)), razonando que
    ///     "hacia abajo, aunque pequeño, caería DENTRO de la bandeja".
    ///  2) Playtest 13: el jugador validó EXPLÍCITAMENTE que la posición
    ///     hacia ABAJO (coherente con <see cref="HeatPlate"/>, que SIEMPRE
    ///     ancló su rótulo a su propio <c>_centroChasis</c> con desplazamiento
    ///     NEGATIVO) "quedó muy bien". Esa versión vivía en el archivo que se
    ///     perdió en el commit e3fed6f.
    ///  3) La restauración de este mismo playtest 14 (ver AVISO DE PÉRDIDA DE
    ///     TRABAJO arriba) reconstruyó la capa visual a partir de la copia
    ///     PRE-playtest-7-validado, es decir, volvió a colgar el rótulo del
    ///     LABIO con desplazamiento hacia ARRIBA -- deshaciendo sin querer la
    ///     corrección del punto (2). Efecto visible: "HELANDO -80° · más
    ///     rápido" salía POR ENCIMA de la bandeja.
    ///  4) ESTE FIX: se vuelve a anclar como <see cref="HeatPlate"/> --
    ///     MISMO PUNTO relativo al aparato (su propio centro, <c>_centroBloque</c>,
    ///     que es lo que <c>transform.position</c> ya vale) y MISMO SIGNO de
    ///     desplazamiento (negativo = hacia abajo): <c>-UiStyles.S(17f)</c>
    ///     para el anillo de ESTADO, <c>-UiStyles.S(34f)</c> para NOMBRE y
    ///     PROMPT (idéntico a HeatPlate.OnGUI). <c>_anclaRotulo</c> (el labio)
    ///     se ELIMINA: ya no hace falta un ancla aparte para el rótulo, ni la
    ///     tenía HeatPlate.
    ///  MEDICIÓN de que hay aire libre de sobra por debajo (Sim/
    ///  SimLevelBuilder.cs, NO TOCADO): <c>_centroBloque</c> cae en la fila
    ///  ~89.5 (centro de las <see cref="SimLevelBuilder.WallThickness"/>=3
    ///  filas de piedra del suelo de la bandeja, y=88..90). El offset más
    ///  grande usado (34 px de diseño a 720p) equivale a
    ///  <c>34 * (14.4 mundo / 720 px) = 0.68</c> unidades de mundo = 6.8
    ///  celdas, así que el punto más bajo dibujado cae en fila ~82.7 -- TODAVÍA
    ///  dentro del hueco de aire vacío de 35 filas documentado más abajo
    ///  (y=53..88, sin nada dibujado ni simulado ahí), con ~29.7 filas de
    ///  margen de sobra antes de tocar siquiera el labio de las cubas (y=53).
    ///  Ningún desplazamiento usado aquí se acerca ni de lejos al interior de
    ///  la propia bandeja (y=91..96, POR ENCIMA de <c>_centroBloque</c>, no
    ///  por debajo), así que "hacia abajo" nunca puede caer dentro de ella --
    ///  el razonamiento del punto (1) solo era válido mientras el ancla fuera
    ///  el LABIO (que sí tiene la bandeja justo debajo); con el ancla en el
    ///  CENTRO DEL BLOQUE (como en HeatPlate) deja de aplicar.
    /// -----------------------------------------------------------------------
    /// TAMAÑO DEL APARATO (fix playtest 14, "las placas son demasiado
    /// grandes")
    /// -----------------------------------------------------------------------
    /// AlkahestGameBootstrap.cs (NO EDITABLE en este encargo) pasa a
    /// <see cref="Init"/> el interior ÚTIL COMPLETO de la bandeja
    /// (<see cref="SimLevelBuilder.ChillTrayInteriorX0"/>/X1, 46 celdas) como
    /// <c>cellX0</c>/<c>cellX1</c> -- antes el aparato ocupaba ese ancho
    /// entero, una losa que cubría todo el fondo. Ahora <see cref="Init"/> lo
    /// recorta a una FRACCIÓN centrada (<see cref="FootprintFraction"/>=0.4,
    /// ~18 de 46 celdas) ANTES de que <see cref="BuildVisual"/> calcule nada,
    /// así que el sprite (bloque + cristales) Y la zona de efecto
    /// (<see cref="ApplyColdTick"/>, que recorre <c>_cellX0.._cellX1</c>)
    /// quedan automáticamente coherentes entre sí sin duplicar el cálculo del
    /// recorte -- es un aparato que ocupa un TROZO del fondo, no una losa que
    /// lo cubre entero, tal y como pide el diseño de cara al taller editable.
    /// El recorte es SIMÉTRICO (mismo margen a cada lado), así que el centro
    /// X no se mueve: <c>PuntoFoco</c> y el ancla de los rótulos siguen
    /// exactamente donde estaban, ningún otro número de este archivo depende
    /// de si el aparato es ancho o estrecho. PROPUESTA que había quedado
    /// anotada aquí para "cuando el taller sea de verdad movible por el
    /// jugador": YA CUMPLIDA, parcialmente distinta de como se anotó -- ver
    /// más abajo "TALLER MOVIBLE". AlkahestGameBootstrap.Init sigue pasando
    /// el interior completo de la bandeja tal cual (no se tocó, sigue fuera
    /// de alcance de este encargo); lo que cambia es que ahora, DESPUÉS de
    /// Init, Game/Mudanza.cs puede reposicionar el aparato ya construido a
    /// cualquier punto del mundo sin pasar de nuevo por este recorte.
    ///
    /// ---------------------------------------------------------------------
    /// TALLER MOVIBLE (playtest 19, Game/Mudanza.cs, tecla V)
    /// ---------------------------------------------------------------------
    /// Implementa <see cref="IMovible"/>: Mudanza puede agarrar esta piedra y
    /// recolocarla en cualquier celda dentro del alcance del jugador. El
    /// movimiento de verdad lo hace <see cref="Reposicionar"/>, que NO llama
    /// ni a <see cref="Init"/> ni a <see cref="BuildVisual"/> otra vez --
    /// ver el docblock de ese método para el porqué exacto (en corto:
    /// MaquinariaSprites.CrearCapa siempre crea un GameObject nuevo, así que
    /// una segunda llamada DUPLICARÍA el bloque/los cristales/el resalte en
    /// vez de reemplazarlos, dejando los viejos huérfanos y visibles en el
    /// sitio antiguo para siempre). El ancho del bloque es invariante tras
    /// <see cref="Init"/> -- Mudanza solo TRASLADA, nunca redimensiona.
    ///
    /// Comparte con <see cref="HeatPlate"/> las decisiones del playtest 4/7:
    ///  · IDENTIDAD VISUAL PROPIA — bloque de roca escarchada con AGUJAS DE
    ///    CRISTAL azules que brotan de él y laten cuando trabaja (sprites
    ///    generados en Game/MaquinariaSprites.cs), en lugar de una barra de un
    ///    píxel tintada de azul.
    ///  · RÓTULO FIJO Y PEQUEÑO, anclado al CENTRO DEL APARATO (ver POSICIÓN
    ///    DEL RÓTULO DE FRÍO arriba) y nunca dentro de la bandeja (que es
    ///    donde el jugador aspira).
    ///  · El prompt "E — ..." solo aparece cerca, con las manos libres, y solo
    ///    las dos primeras veces del taller (restaurado playtest 7: a partir de
    ///    ahí lo sustituye el RESALTE dorado del aparato enfocado, ver
    ///    ActualizarResalte).
    ///
    /// Es la máquina clave de dos encargos: "algo helado" (congela agua aquí y
    /// entrégala — el Frasco ahora conserva el frío, ver Game/Flask.cs) y
    /// "cristal" (azoth + semilla de cristal en FRÍO, ver Universe.Create).
    ///
    /// LIMITACIÓN: igual que HeatPlate, escribe _sim.Grid.temp[] directamente.
    /// TODO(ChaosAlchemy): canalizar por una API del sim de cara a netcode.
    /// </summary>
    public sealed class ChillStone : MonoBehaviour, IMaquinaInteractiva, IMovible
    {
        private enum State { Off = 0, Fresca = 1, Helando = 2 }

        private const float TickDt = 1f / 30f;
        private const int MaxStepsPerFrame = 2;
        /// <summary>Radio de interacción con E (ESCALA COMPARTIDA con Dispenser/HeatPlate, ver ambos archivos).</summary>
        private const float ProximityRange = 3.2f;

        private const byte HelandoRaw = 20; // ~-80 °C, extremo garantizado (ver doc de la clase, fix playtest 14).

        /// <summary>Empuje por tick de FRESCA: moderado, el mismo valor que usaba el único estado antes del playtest 13.</summary>
        private const int TempStepFresca = 5;
        /// <summary>
        /// (fix playtest 14) Empuje por tick de HELANDO: más del doble que
        /// FRESCA. Mismo destino final (<see cref="HelandoRaw"/>) que siempre
        /// tuvo, pero al empujar más fuerte por tick cruza el umbral de
        /// congelación/cristalización en MENOS ticks -- es la razón de ser de
        /// HELANDO ahora que FRESCA ya cubre "lo alcanza" (ver doc de clase).
        /// </summary>
        private const int TempStepHelando = 12;

        /// <summary>
        /// (fix playtest 14, SEGUNDA pasada: "el frío sigue llegando al
        /// grifo") Perfil de caída del empuje térmico por fila de distancia
        /// al aparato, en PORCENTAJE del empuje base del estado activo
        /// (<see cref="TempStepFresca"/>/<see cref="TempStepHelando"/>).
        /// Índice 0 = fila adyacente (100%, máximo empuje), cada índice
        /// siguiente una fila más lejos y más débil; la longitud del array ES
        /// el número de filas afectadas.
        ///
        /// NÚMEROS VIEJOS Y NUEVOS (medido contra Sim/SimLevelBuilder.cs, no
        /// tocado): la primera pasada de este mismo playtest 14 sustituyó el
        /// corte plano original (3 filas al 100%) por <c>{100,60,35,20,10}</c>
        /// -- 5 filas, más "moderado alrededor" pero también MÁS LARGO que
        /// las 3 originales, y el jugador reportó que el frío seguía
        /// notándose donde no debería. Distancia real medida entre la fila
        /// más cercana al grifo de AGUA que toca este aparato (x=39..84,
        /// y=91) y la boquilla del grifo (Dispenser: <c>TapMountX(8) +
        /// SpoutOffsetCells(5)=13</c>, <c>TapFirstY(62) - SpoutDropCells(2)
        /// =60</c>): dx=39-13=26, dy=91-60=31, euclídea ≈40.5 celdas, manhattan
        /// 57 -- ya lejísimos incluso con el perfil de 5 filas (que solo
        /// llega a y=95, todavía a 35+ celdas de cualquier punto del grifo).
        /// La influencia NUNCA tocó el grifo por vecindad directa (no hay
        /// ninguna celda no-Empty que conecte la bandeja fría, en x=36..87,
        /// con el pilar de grifos, en x=1..8 -- 27 columnas de aire vacío de
        /// por medio a esa altura) -- pero SimStepper.DiffuseTemperature
        /// (regla 9, NO TOCADO) no tiene un tope de alcance, así que cuanta
        /// más energía térmica se inyecte en total por tick, más lejos se
        /// nota su rastro con el tiempo. Recortado a <c>{100,45,15}</c> --
        /// SOLO 3 filas (antes 5) y caída más agresiva: suma total del
        /// perfil baja de 225 a 160 (-29%), y la fila más lejana pasa de
        /// y+5 (10%) a y+3 (15%), casi la mitad de profundidad. Con esto la
        /// influencia es inequívocamente LOCAL: se queda pegada a la bandeja,
        /// nunca se extiende lo bastante ni dura lo bastante activa como para
        /// que la difusión la arrastre 40 celdas más allá. Mismo criterio
        /// aplicado IGUAL en HeatPlate.cs (mismo array duplicado a
        /// propósito, mismos números viejo/nuevo, misma medición contra el
        /// grifo -- ver esa clase para la distancia medida allí, mayor
        /// todavía por estar la placa ígnea más lejos del banco de grifos).
        /// </summary>
        private static readonly int[] FilaEmpujePct = { 100, 45, 15 };

        /// <summary>
        /// (fix playtest 13) Margen de fiabilidad, en raw, entre FRESCA y el
        /// umbral real (punto de congelación del agua / cristalización) de
        /// esta seed: sin margen, el tirón hacia ambiente de
        /// SimStepper.DiffuseTemperature (±1 raw cada ~32 ticks) podría dejar
        /// una celda oscilando justo encima del umbral en vez de cruzarlo de
        /// forma fiable. 10 raw = 20 °C, mismo orden de magnitud que la
        /// histéresis de +5 °C que ya usa Ice.meltsAt y que el margen de 8 °C
        /// que ARDIENTE deja sobre la ignición máxima sorteable (ver
        /// HeatPlate.cs).
        /// </summary>
        private const int FrescaMarginRaw = 10;

        /// <summary>(fix playtest 14, ver doc de clase "TAMAÑO DEL APARATO") Fracción del ancho recibido en Init que ocupa de verdad el aparato, centrada.</summary>
        private const float FootprintFraction = 0.4f;

        /// <summary>
        /// (fix playtest 14, ver doc de clase "ORDEN DE RECUPERACIÓN") Empuje
        /// mínimo que sujeta la fila adyacente tras apagarse: solo contrarresta
        /// el tirón hacia ambiente, nunca sigue enfriando material nuevo (el
        /// clamp de <see cref="ApplyHoldTick"/> no deja pasar el objetivo).
        /// </summary>
        private const int HoldStepRaw = 1;
        /// <summary>(fix playtest 14) Ticks tras apagarse durante los que la fila adyacente sigue sujeta (2 s a 30 Hz). Mismo valor que HeatPlate.cs.</summary>
        private const int HoldTicksTrasApagar = 60;

        private AlkahestSim _sim;
        private Transform _player;
        private int _cellX0, _cellX1, _plateRow;
        private State _state = State.Off;
        private float _accumulator;

        /// <summary>Objetivo de FRESCA: calibrado por seed en Init() (ver doc de la clase). Valor por defecto plausible si Universe no está listo aún.</summary>
        private byte _frescaRaw = 45;

        /// <summary>(fix playtest 14) Objetivo activo justo antes de apagarse, hacia el que sigue sujeta la fila adyacente durante el hold -- ver ApplyHoldTick.</summary>
        private byte _lastActiveTarget;
        /// <summary>(fix playtest 14) Cuenta atrás de ticks del hold de apagado -- 0 = suelto del todo. Ver ApplyHoldTick.</summary>
        private int _holdTicksRestantes;

        private SpriteRenderer _cristales;
        private Vector3 _centroBloque;

        /// <summary>(restaurado playtest 7) Capa de resalte dorado del aparato enfocado, ver ActualizarResalte.</summary>
        private SpriteRenderer _resalte;
        private float _alfaResalte;

        // ---------------------------------------------------------------
        // ESCALA COMPARTIDA DE CERCANÍA DEL TALLER (restaurado playtest 7,
        // duplicada a propósito en HeatPlate.cs; usa UiStyles.Cercania,
        // restaurada en UiStyles.cs en el fix playtest 14).
        //  · RangoEstado: de lejos, SOLO el estado de trabajo (si lo hay).
        //  · RangoNombre: de cerca, además el nombre del aparato — pero solo
        //    hasta que el aprendiz ya lo conoce (ver _yaConocida).
        // ---------------------------------------------------------------
        private const float RangoEstadoPleno = 5.0f;
        private const float RangoEstadoDesvanece = 6.5f;
        private const float RangoNombrePleno = 2.6f;
        private const float RangoNombreDesvanece = 3.6f;

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

        private const string ChapaNombre = "piedra gélida";

        // Foco de interacción: en _centroBloque, el propio centro del
        // aparato -- desde el fix playtest 14 es TAMBIÉN el punto del que
        // cuelgan los rótulos (ver OnGUI/doc de la clase), así que ya no hace
        // falta razonar sobre la distancia a un ancla aparte (el antiguo
        // _anclaRotulo del labio, eliminado): un único punto para foco y
        // rótulos, igual que HeatPlate.
        public Vector3 PuntoFoco => _centroBloque;
        public float RangoFoco => ProximityRange;

        // ---------------------------------------------------------------
        // IMovible (playtest 19, ver doc de clase "TALLER MOVIBLE" y
        // Game/Mudanza.cs para el contrato completo).
        // ---------------------------------------------------------------
        public Vector3 CentroMundo => _centroBloque;
        public Vector2 TamanoMundo => new Vector2(
            (_cellX1 - _cellX0 + 1) * SimRenderer.CellWorldSize,
            SimLevelBuilder.WallThickness * SimRenderer.CellWorldSize);
        /// <summary>Celda de anclaje: borde IZQUIERDO del bloque (X0) + fila del SUELO bajo él (_plateRow). El ancho (span) no viaja en la ancla -- es invariante, ver Reposicionar.</summary>
        public Vector2Int AnclaCelda => new Vector2Int(_cellX0, _plateRow);

        /// <summary>¿Cabría el bloque (mismo ancho de siempre x WallThickness de alto) en esa ancla sin tocar el marco protegido del mundo? Puramente informativo -- Mudanza es quien decide si bloquea el drop con esto.</summary>
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
            // el ancho recibido (interior COMPLETO de la bandeja) a una
            // fracción centrada ANTES de que BuildVisual/ApplyColdTick lean
            // _cellX0/_cellX1 -- así sprite y zona de efecto quedan
            // automáticamente coherentes entre sí sin duplicar el cálculo.
            int spanTotal = cellX1 - cellX0 + 1;
            int spanReducido = Mathf.Max(8, Mathf.RoundToInt(spanTotal * FootprintFraction));
            int margen = (spanTotal - spanReducido) / 2;
            _cellX0 = cellX0 + margen;
            _cellX1 = _cellX0 + spanReducido - 1;
            _plateRow = plateRow;

            // (fix playtest 13) FRESCA: mínimo entre el punto de congelación
            // del agua y el umbral de cristalización de ESTA seed, menos el
            // margen de fiabilidad -- calibrado por seed igual que
            // HeatPlate._templadaRaw se calibra a VivGrowMinRaw/MaxRaw.
            if (_sim != null && _sim.Universe != null)
            {
                int freezesAt = _sim.Universe.Get(MaterialId.Water).freezesAt;
                int limite = Mathf.Min(freezesAt, _sim.Universe.CrystallizeMaxTempRaw);
                int fresca = limite - FrescaMarginRaw;
                _frescaRaw = (byte)Mathf.Clamp(fresca, HelandoRaw + 1, CellGrid.AmbientRaw - 1);
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
        /// (playtest 19) Recalcula <see cref="_centroBloque"/> y mueve
        /// transform.position a partir de _cellX0/_cellX1/_plateRow.
        /// Extraído de BuildVisual para que <see cref="Reposicionar"/> pueda
        /// reutilizarlo SIN volver a crear ningún GameObject: el ancho
        /// (span) y el alto (WallThickness) del bloque son constantes tras
        /// Init, así que "mover el aparato" es solo recalcular DÓNDE cae ese
        /// rectángulo fijo -- nunca su tamaño.
        /// </summary>
        private void RecalcularCentro()
        {
            float celda = SimRenderer.CellWorldSize;
            int spanCeldas = _cellX1 - _cellX0 + 1;
            int filaInferior = _plateRow - SimLevelBuilder.WallThickness + 1;
            float centroX = (_cellX0 + spanCeldas * 0.5f) * celda;
            float centroY = (filaInferior + (_plateRow + 1 - filaInferior) * 0.5f) * celda;
            _centroBloque = new Vector3(centroX, centroY, 0f);
            transform.position = _centroBloque;
        }

        /// <summary>
        /// TALLER MOVIBLE (playtest 19, Game/Mudanza.cs): mueve el aparato YA
        /// CONSTRUIDO a una nueva celda de anclaje, SIN volver a llamar a
        /// Init ni a BuildVisual.
        ///
        /// POR QUÉ NO BuildVisual: MaquinariaSprites.CrearCapa siempre hace
        /// `new GameObject` -- una segunda llamada NO reemplaza el bloque/
        /// los cristales/el resalte, los DUPLICA: los hijos originales se
        /// quedarían huérfanos y visibles en el sitio ANTIGUO para siempre
        /// (nadie los destruye ni los mueve). Aquí no se toca ningún
        /// GameObject: Resalte/Bloque/Cristales son hijos de `transform` con
        /// localPosition (0,0,0) -- basta con mover `transform.position`
        /// (ver RecalcularCentro) y los tres se arrastran solos con él.
        ///
        /// POR QUÉ NO Init: Init también recalibra _frescaRaw por seed
        /// (inofensivo repetirlo dentro de la misma partida) pero, sobre
        /// todo, este método deja _state, _lastActiveTarget y
        /// _holdTicksRestantes completamente intactos a propósito: mover una
        /// piedra ENCENDIDA no debe apagarla.
        ///
        /// EL ANCHO NUNCA CAMBIA en esta llamada -- Mudanza solo TRASLADA,
        /// nunca redimensiona -- así que el sprite ya cacheado sigue siendo
        /// válido sin tocarlo. Si algún día algo pidiera un ancho distinto,
        /// el punto correcto sería reasignar `SpriteRenderer.sprite` desde
        /// MaquinariaSprites (que cachea por ancho, así que no generaría
        /// textura nueva) y re-escalar `transform.localScale` de cada capa
        /// -- NUNCA llamar a CrearCapa otra vez, por la misma razón de
        /// arriba.
        /// </summary>
        public void Reposicionar(Vector2Int anclaCelda)
        {
            int span = _cellX1 - _cellX0 + 1; // invariante, ver doc de arriba.
            _cellX0 = anclaCelda.x;
            _cellX1 = _cellX0 + span - 1;
            _plateRow = anclaCelda.y;
            RecalcularCentro();
        }

        private void BuildVisual()
        {
            float celda = SimRenderer.CellWorldSize;
            int spanCeldas = _cellX1 - _cellX0 + 1;

            RecalcularCentro();
            int filaInferior = _plateRow - SimLevelBuilder.WallThickness + 1;
            float anchoMundo = spanCeldas * celda;
            float altoMundo = (_plateRow + 1 - filaInferior) * celda;

            // (fix playtest 14) Ya NO hace falta un ancla de rótulo aparte
            // (el antiguo _anclaRotulo, colgado del labio de la bandeja): los
            // rótulos cuelgan de _centroBloque con desplazamiento hacia abajo,
            // igual que HeatPlate cuelga los suyos de _centroChasis -- ver
            // "POSICIÓN DEL RÓTULO DE FRÍO" en el doc de la clase.

            // Resalte de foco (restaurado playtest 7, ver ActualizarResalte):
            // capa DETRÁS de las demás (sortingOrder menor que Bloque=18),
            // copia del sprite principal agrandada ~15%/35% y teñida de oro; al
            // ser mayor asoma por los bordes del bloque como un halo. Se crea
            // UNA vez aquí; en Update solo se le cambia el color (cero
            // allocs/frame).
            _resalte = MaquinariaSprites.CrearCapa(transform, "Resalte", MaquinariaSprites.BloqueGelido(spanCeldas), 16,
                anchoMundo * 1.15f, altoMundo * 1.35f);
            _resalte.color = new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, 0f);

            MaquinariaSprites.CrearCapa(transform, "Bloque", MaquinariaSprites.BloqueGelido(spanCeldas), 18,
                anchoMundo, altoMundo);
            _cristales = MaquinariaSprites.CrearCapa(transform, "Cristales",
                MaquinariaSprites.CristalesGelidos(spanCeldas), 19, anchoMundo, altoMundo);
        }

        private void Update()
        {
            if (_sim == null || _sim.Grid == null) return;
            if (DayCycle.InputLocked) return; // M4: título/intro/fin de día/pantalla final congelan la piedra.

            // (fix playtest 10) E es un atajo de una sola tecla como cualquier otro del
            // proyecto: no puede robarle letras al campo de bautizar (UiStyles.
            // EscribiendoTexto) ni competir con el diario a pantalla completa, que posee
            // el input del MUNDO mientras está abierto (JournalHud.Abierto) -- el tick de
            // frío de más abajo NO se toca (el mundo sigue vivo con el libro abierto),
            // solo se calla el TOGGLE de encendido mientras se escribe o se lee.
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame
                && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && EstaEnfocada())
            {
                CycleState();
            }

            if (_state != State.Off)
            {
                _accumulator += Time.deltaTime;
                int steps = 0;
                while (_accumulator >= TickDt && steps < MaxStepsPerFrame)
                {
                    ApplyColdTick();
                    _accumulator -= TickDt;
                    steps++;
                }
                if (_accumulator > TickDt * MaxStepsPerFrame) _accumulator = TickDt * MaxStepsPerFrame;

                AnimarCristales();
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

            // (restaurado playtest 7) El resalte de foco tiene que latir
            // SIEMPRE, esté la piedra encendida o no -- si no, acercarse a una
            // piedra APAGADA no mostraría ninguna señal de que se puede
            // interactuar con ella. Por eso vive FUERA del "if" de arriba.
            ActualizarResalte();
        }

        /// <summary>¿Es ESTE el aparato que el aprendiz tiene delante? (ver Game/MachineFocus.cs)</summary>
        private bool EstaEnfocada() => MachineFocus.EsFoco(this, _player);

        /// <summary>Ciclo de 3 estados (playtest 13), con aviso de uso aprendido de E (restaurado playtest 7).</summary>
        private void CycleState()
        {
            bool estabaActiva = _state != State.Off;
            byte objetivoPrevio = TargetRaw(); // objetivo del estado ANTES de cambiarlo.
            _state = (State)(((int)_state + 1) % 3);

            if (_state == State.Off && estabaActiva)
            {
                // (fix playtest 14, ver doc de clase "ORDEN DE RECUPERACIÓN")
                // Al apagarse de verdad (siempre se llega a Off desde HELANDO
                // en este ciclo), arma el hold de la fila adyacente.
                _lastActiveTarget = objetivoPrevio;
                _holdTicksRestantes = HoldTicksTrasApagar;
            }

            UpdateVisualTint();
            RebuildChapaEstado();
            MachineFocus.RegistrarUsoE(); // el estado cambió de verdad: cuenta como un uso aprendido de E.
            Debug.Log($"[ChaosAlchemy] Piedra gélida -> {StateLabel()} ({CellGrid.RawToC(TargetRaw())} °C)");
        }

        private byte TargetRaw() => _state == State.Helando ? HelandoRaw : _frescaRaw;

        /// <summary>Empuje BASE por tick del estado activo, antes de aplicar el perfil de caída por fila (fix playtest 14, ver <see cref="FilaEmpujePct"/>).</summary>
        private int TempStepBase() => _state == State.Helando ? TempStepHelando : TempStepFresca;

        /// <summary>
        /// (fix playtest 14) Empuja la temperatura de las filas por encima del
        /// aparato hacia <see cref="TargetRaw"/>, con un empuje por tick que
        /// DECAE con la distancia a la piedra en vez de ser uniforme (ver
        /// <see cref="FilaEmpujePct"/> y el bloque de doc de la clase). El
        /// suelo de <c>Mathf.Max(1, ...)</c> garantiza que ninguna fila del
        /// perfil quede completamente inerte por redondeo entero a 0.
        /// </summary>
        private void ApplyColdTick()
        {
            byte target = TargetRaw();
            int stepBase = TempStepBase();
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;

            for (int x = _cellX0; x <= _cellX1; x++)
            {
                for (int fila = 0; fila < FilaEmpujePct.Length; fila++)
                {
                    int y = _plateRow + fila + 1;
                    if (!CellGrid.InBounds(x, y)) continue;
                    int idx = CellGrid.Idx(x, y);
                    int step = Mathf.Max(1, stepBase * FilaEmpujePct[fila] / 100);
                    int cur = grid.temp[idx];
                    int next = cur > target ? Mathf.Max(target, cur - step) : Mathf.Min(target, cur + step);
                    grid.temp[idx] = (byte)next;
                    grid.WakeChunk(x, y, tick);
                }
            }
        }

        /// <summary>
        /// (fix playtest 14, ver doc de clase "ORDEN DE RECUPERACIÓN") Tras
        /// apagarse, sujeta SOLO la fila adyacente (índice 0 del perfil)
        /// hacia <see cref="_lastActiveTarget"/> con un empuje mínimo
        /// (<see cref="HoldStepRaw"/>) durante <see cref="_holdTicksRestantes"/>
        /// ticks más. El clamp es el mismo patrón que <see cref="ApplyColdTick"/>:
        /// nunca deja pasar el objetivo, así que esto no sigue enfriando la
        /// celda más allá de donde ya estaba, solo la MANTIENE mientras las
        /// filas 1 y 2 (que no se tocan aquí) ya están volviendo a ambiente
        /// libremente por SimStepper.DiffuseTemperature.
        /// </summary>
        private void ApplyHoldTick()
        {
            var grid = _sim.Grid;
            uint tick = _sim.Stepper != null ? _sim.Stepper.Tick : 0u;
            int y = _plateRow + 1; // solo la fila adyacente.

            for (int x = _cellX0; x <= _cellX1; x++)
            {
                if (!CellGrid.InBounds(x, y)) continue;
                int idx = CellGrid.Idx(x, y);
                int cur = grid.temp[idx];
                int next = cur > _lastActiveTarget
                    ? Mathf.Max(_lastActiveTarget, cur - HoldStepRaw)
                    : Mathf.Min(_lastActiveTarget, cur + HoldStepRaw);
                grid.temp[idx] = (byte)next;
                grid.WakeChunk(x, y, tick);
            }
        }

        private void UpdateVisualTint()
        {
            if (_cristales == null) return;
            _cristales.color = ColorCristal(1f);
        }

        /// <summary>Latido lento y frío mientras hiela (opuesto al latido rápido y cálido de la placa ígnea); FRESCA late un poco más rápido, menos urgente que HELANDO.</summary>
        private void AnimarCristales()
        {
            if (_cristales == null || _state == State.Off) return;
            float pulso = 0.80f + 0.20f * Mathf.Sin(Time.time * (_state == State.Helando ? 2.2f : 3.4f));
            _cristales.color = ColorCristal(pulso);
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

        /// <summary>(fix playtest 13) Tres tintes, mismo patrón que HeatPlate.ColorResistencia: apagada mate, FRESCA azul suave, HELANDO azul intenso.</summary>
        private Color ColorCristal(float pulso)
        {
            switch (_state)
            {
                case State.Helando: return new Color(0.62f * pulso + 0.20f, 0.90f * pulso, 1f, 1f);
                case State.Fresca: return new Color(0.50f * pulso + 0.16f, 0.72f * pulso, 0.88f * pulso + 0.08f, 1f);
                default: return new Color(0.42f, 0.46f, 0.52f, 0.75f); // apagada: cristal mate, sin luz propia
            }
        }

        private string StateLabel()
        {
            if (_state == State.Helando) return "HELANDO";
            if (_state == State.Fresca) return "FRESCA";
            return "APAGADA";
        }

        /// <summary>
        /// Reconstruye la chapa del anillo de ESTADO. Se llama SOLO al cambiar
        /// de estado (restaurado playtest 7, nunca desde OnGUI): el raw
        /// objetivo de cada estado es constante mientras dura ese estado, así
        /// que el texto no cambia frame a frame y no hace falta reconstruirlo
        /// cada vez (regla de cero asignaciones por frame). El sufijo
        /// "· más rápido" de HELANDO insinúa la diferencia real con FRESCA (ver
        /// fix playtest 14 en el doc de la clase) sin necesidad de que el
        /// jugador lea el código para descubrirla.
        /// </summary>
        private void RebuildChapaEstado()
        {
            _chapaEstado = _state switch
            {
                State.Helando => $"HELANDO {CellGrid.RawToC(HelandoRaw)}° · más rápido",
                State.Fresca => $"FRESCA {CellGrid.RawToC(_frescaRaw)}°",
                _ => null, // apagada: nada que anunciar de lejos.
            };
        }

        /// <summary>
        /// (restaurado playtest 7, re-anclado fix playtest 14) TRES anillos de
        /// rótulo por cercanía (estado / nombre / prompt), ahora colgados de
        /// <see cref="_centroBloque"/> con desplazamiento NEGATIVO (hacia
        /// abajo) -- exactamente igual que <see cref="HeatPlate.OnGUI"/>. Ver
        /// "POSICIÓN DEL RÓTULO DE FRÍO" en el doc de la clase para la
        /// cronología completa de por qué esto se invirtió una vez y no debe
        /// volver a hacerlo.
        /// </summary>
        private void OnGUI()
        {
            if (_sim == null || DayCycle.InputLocked || DayCycle.HudSilenciado) return; // (playtest 21) HudSilenciado, hermano de InputLocked.

            // Salida temprana: si el aprendiz está fuera de los dos anillos, no
            // hay nada que dibujar -- ni siquiera Preparar().
            float cercaniaEstado = UiStyles.Cercania(_centroBloque, _player, RangoEstadoPleno, RangoEstadoDesvanece);
            float cercaniaNombre = UiStyles.Cercania(_centroBloque, _player, RangoNombrePleno, RangoNombreDesvanece);
            if (cercaniaEstado <= 0f && cercaniaNombre <= 0f) return;

            // Aprendizaje: una vez el aprendiz entra de lleno en el anillo de
            // nombre, la piedra queda "conocida" para el resto de la partida y
            // su chapa de nombre deja de dibujarse.
            if (!_yaConocida && cercaniaNombre >= 0.98f) _yaConocida = true;

            UiStyles.Preparar();
            Color color = _state != State.Off ? UiStyles.Frio : UiStyles.TextoTenue;

            // 1) Anillo de ESTADO: solo mientras hiela, y SOLO el estado — nunca
            //    el nombre del aparato aquí (eso es información de reconocimiento,
            //    no de "¿dejé esto encendido?"). Desplazamiento NEGATIVO = hacia
            //    abajo, sobre la piedra del suelo -- mismo signo que HeatPlate.
            if (_state != State.Off && _chapaEstado != null)
            {
                UiStyles.PlacaMundo(_centroBloque, _chapaEstado,
                    new Color(color.r, color.g, color.b, color.a * cercaniaEstado), -UiStyles.S(17f));
            }

            // 2) Anillo de NOMBRE: solo hasta que el aprendiz ya sabe qué es esto.
            if (!_yaConocida)
            {
                Color tenue = UiStyles.TextoTenue;
                UiStyles.PlacaMundo(_centroBloque, ChapaNombre,
                    new Color(tenue.r, tenue.g, tenue.b, tenue.a * cercaniaNombre), -UiStyles.S(34f));
            }

            // 3) Prompt E: además de foco + manos libres, solo las dos primeras
            //    veces del taller (MachineFocus.MostrarPromptE); a partir de ahí
            //    la única señal de "puedes actuar aquí" es el RESALTE dorado
            //    (ver ActualizarResalte), no un texto permanente.
            if (MachineFocus.MostrarPromptE && EstaEnfocada() && !UiStyles.RatonOcupado)
            {
                UiStyles.PlacaMundo(_centroBloque, "E — encender el frío",
                    new Color(UiStyles.Oro.r, UiStyles.Oro.g, UiStyles.Oro.b, cercaniaNombre), -UiStyles.S(34f));
            }
        }
    }
}
