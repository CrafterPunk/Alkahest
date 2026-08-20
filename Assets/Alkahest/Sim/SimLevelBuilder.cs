// (playtest 17) `using System;` retirado: lo único que lo necesitaba era el
// `Math.Round` de los degradados de clima, que ya no existen.
//
// (playtest 26, ampliado en el 29) `using Alkahest.Game;`: mismo patrón ya
// establecido por Sim/SimRenderer.cs (que ya depende de Game/UiStyles y
// Game/JournalHud) -- SimLevelBuilder llama a los `TallarEnPlano` estáticos
// de Crisol/Prensa/BancoChispa/EnsayoMaestro/ColumnaEnsayo (regla 47 de
// CLAUDE.md: SimLevelBuilder talla TODA la mampostería del plano, ver el
// bloque "PLAYTEST 26" más abajo; ColumnaEnsayo se sumó en el playtest 29
// para que la Columna pudiera implementar IMovible -- ver el bloque "OBRA
// MOVIBLE" junto a ObraDelTaller).
using Alkahest.Game;
// (playtest 40, SEMILLA CERO) `using Alkahest;`: `TapiarSalasSemillaCero`/
// `DestaparSala` reciben un `AlkahestSim` (API congelada, CONTRATO_SEMILLA.md
// §3) -- vive en el namespace RAÍZ `Alkahest` (Assets/Alkahest/AlkahestSim.cs),
// no en `Alkahest.Game` como el resto de lo que este archivo ya importaba.
using Alkahest;

namespace Alkahest.Sim
{
    /// <summary>
    /// EL PLANO DEL TALLER. Construye el nivel entero por código (cero assets)
    /// y es la ÚNICA FUENTE DE VERDAD de dónde está cada cosa: Game/ lee estas
    /// constantes para colocar placas, piedra fría, grifos, estantería y Tolva.
    ///
    /// =====================================================================
    /// EL TALLER DEJA DE SER UNA PANTALLA (playtest 15)
    /// =====================================================================
    /// Cesar, motivando el tamaño (no es estética, es un REQUISITO DEL CO-OP):
    /// *"un laboratorio de 2-3 pantallas de ancho y 1,5-2 de alto... suficiente
    /// para que dos personas puedan estar trabajando en cosas distintas sin
    /// verse constantemente"*. `CellGrid` pasó de 256x144 (1 pantalla) a
    /// 768x288 (3 pantallas de ancho x 2 de alto, `CellGrid.PantallaW/H` siguen
    /// midiendo 1 pantalla = 256x144 para pensar el plano en esa unidad).
    ///
    /// CUATRO ZONAS, pedidas explícitamente por Cesar. (playtest 19: el
    /// diagrama de abajo es el PLANO ACTUAL, tras compactar LABORATORIO/
    /// ENTREGA otra vez — ver la sección "EL TALLER SE COMPACTA AÚN MÁS"
    /// más abajo para el porqué y las medidas completas; el reparto en
    /// CUATRO zonas de Cesar sigue intacto, solo cambió la disposición
    /// DENTRO de LABORATORIO y el ancho de ENTREGA.)
    ///
    ///                                     y=287 (muro/techo del mundo)
    ///   ┌──────────── SUPERFICIE (mitad de arriba, y=144..287) ─────────────────────┐
    ///   │      CULTIVO        │  LABORATORIO (compacto) │        ENTREGA            │
    ///   │      x16..250       │      x262..384          │  x380..767 (Tolva llega   │
    ///   │      (20 °C)        │        (20 °C)           │  hasta el muro del mundo) │
    ///   │ cubaA        cubaB  │ bandeja fría + estante   │  x380: contrafuerte       │
    ///   │ x30          x187   │ x262..374, y236..245:    │  PEGADO a LABORATORIO     │
    ///   │ LEJOS ·····► CERCA  │ FLOTAN sobre el banco     │  (boca en x392..413) +    │
    ///   │ (placas ígneas en   │ (playtest 19). Debajo:    │  contrafuerte que SIGUE   │
    ///   │  las dos cubas)     │ banco+grifos+pila,        │  hasta x767: cerca de     │
    ///   │                     │ .....POZO (x343..382)..... │  usar, lejos de ver      │
    ///   │                     │                            │  acabar (LEJOS·►)        │
    ///   └─────────────────────┴──────────┬─────────────────┴───────────────────────────┘
    ///                     y=155/144 losa de superficie (con el pozo abierto)
    ///                            │
    ///   ┌──────────── SÓTANO (mitad de abajo, y=10..143, x220..530) ────────┐
    ///   │        (20 °C) — bajo LABORATORIO, se llega VOLANDO               │
    ///   │      repisa        PLATAFORMA DE CRISTALIZACIÓN       repisa      │
    ///   │                        x333..392                                  │
    ///   └─────────────────────────────────────────────────────────────────┘
    ///                     y=0..9 bedrock bajo TODO el mundo
    ///
    /// Reparto (detalles internos libres, pero el reparto de zonas es el que
    /// pidió Cesar): CULTIVO = las cubas grandes donde se cría el Vivium (las
    /// placas ígneas hacen TODO el trabajo de calor: hasta el playtest 16 el
    /// clima de la zona ayudaba un poco, ya no — ver la sección del CLIMA).
    /// LABORATORIO = el centro de operaciones: banco de grifos + pila de
    /// recogida, bandeja fría, estante de redomas (playtest 19: los dos
    /// últimos ahora FLOTAN sobre el propio banco en vez de vivir aparte, ver
    /// más abajo), y el POZO por el que se baja al sótano. ENTREGA = la Tolva
    /// del Maestro con margen de sobra alrededor. SÓTANO = una sala bajo el
    /// laboratorio a la que solo se llega volando (el aprendiz VUELA: no hace
    /// falta ninguna escalera, solo que los muros/plataformas den FORMA al
    /// espacio — ver <see cref="BuildSotano"/>). (playtest 16: dentro de
    /// CULTIVO y ENTREGA ahora hay una sub-distinción CERCA/LEJOS del punto de
    /// partida — ver la sección siguiente, es un reparto DENTRO de la zona,
    /// las cuatro zonas de Cesar no cambiaron.)
    ///
    /// =====================================================================
    /// EL CLIMA POR ZONA — RETIRADO EN EL PLAYTEST 17 (leer antes de
    /// reimplementarlo: existió, funcionaba, y se quitó a propósito)
    /// =====================================================================
    /// Entre el playtest 15 y el 16 este archivo pintaba `CellGrid.ambient`
    /// por zona: un CULTIVO templado (`CultivoAmbientRaw`, raw 73 = 26°C,
    /// "criar Vivium ahí cuesta menos calor activo") y un SÓTANO frío
    /// (`SotanoAmbientRaw`, raw 62 = 4°C, "cristalizar ahí cuesta menos frío
    /// activo — es la razón de bajar"), con degradados de varias decenas de
    /// celdas (`ClimaGradienteX`/`ClimaGradienteY`) para que ninguna frontera
    /// invisible se leyera como un escalón. Todo eso está BORRADO; hoy el
    /// mundo entero nace en `CellGrid.AmbientRaw` (raw 70 = 20°C), que es el
    /// valor con el que el array ya nace — ver <see cref="PaintClimate"/>,
    /// que se conserva como único punto de entrada pero pinta uniforme.
    ///
    /// POR QUÉ SE RETIRÓ. Cesar, playtest 17, con dos razones distintas:
    ///  (1) DISEÑO — *"si van a construir su mapita como quieran, puede que
    ///      no quieran que esté condicionada la temperatura, o al revés que
    ///      roleen semilla hasta que les toque frío"*. El clima por zona
    ///      supone que las zonas son fijas; la fase de "taller movible"
    ///      (backlog, CLAUDE.md) dice justo lo contrario. Un CULTIVO cálido
    ///      deja de tener sentido en cuanto el jugador puede poner la cuba
    ///      donde le dé la gana, y peor: convierte una decisión suya en algo
    ///      que el plano ya decidió por él.
    ///  (2) SÍNTOMA — *"el agua me sigue saliendo congelada"*, dos rondas
    ///      seguidas. Ojo, esto NO lo causaba el clima (ver abajo), pero el
    ///      clima era el sospechoso obvio y su presencia hacía imposible
    ///      descartar a ojo dónde estaba el fallo de verdad.
    ///
    /// SE QUITAN LOS DOS, NO SOLO EL FRÍO. Cesar nombró el sótano, pero la
    /// razón (1) es simétrica y hay una razón técnica añadida para no dejar
    /// solo el cálido: si CULTIVO regalara 6°C a las placas ígneas mientras
    /// la piedra gélida pelea desde la base en LABORATORIO, sería exactamente
    /// la asimetría calor/frío que Cesar ya reportó en el playtest 13 ("la
    /// placa fría parece irradiar más fuerte que el calor"), pero escondida
    /// en el plano en vez de en el aparato. Coste real de quitar el cálido:
    /// CASI NINGUNO — la banda de crecimiento del Vivium es 30..60°C ±shift
    /// (`Universe.growMinC/growMaxC`), así que los 26°C de CULTIVO nunca
    /// metían nada DENTRO de la banda por sí solos; solo acortaban el salto
    /// en 6°C, y quien hace el trabajo de verdad sigue siendo la placa.
    ///
    /// LO QUE SÍ GARANTIZA UN AMBIENTE UNIFORME A 20°C: `Water.freezesAt` =
    /// `CToRaw(waterFreezeC)` con `waterFreezeC` uniforme en los enteros
    /// -15..15, o sea raw 52..67 — el PEOR caso (raw 67) sigue 3 unidades raw
    /// por debajo de la base (raw 70). **En NINGUNA seed puede el ambiente
    /// congelar agua por sí solo, en NINGÚN punto del mundo.** Antes esto solo
    /// valía para LABORATORIO/ENTREGA; ahora vale para el mundo entero, y con
    /// ello desaparece de raíz la clase entera de "algo se congeló solo".
    ///
    /// LA INFRAESTRUCTURA SE QUEDA. `CellGrid.ambient` (un byte por celda) y
    /// el tirón por celda de `SimStepper.DiffuseTemperature` NO se tocan: hoy
    /// cuestan lo mismo que una constante (una lectura de array) y son el
    /// vehículo correcto para el clima que viene después — el que CREA EL
    /// JUGADOR (una fragua que entibia lo que tiene alrededor, un sótano que
    /// se enfría porque él lo selló), que es clima ganado, no clima heredado
    /// del plano. Si alguien lo reimplementa por zonas fijas, que sea sabiendo
    /// que ya se hizo y por qué se deshizo.
    ///
    /// =====================================================================
    /// "EL GRIFO SALE CONGELADO": LA INVESTIGACIÓN DEL PLAYTEST 16 SE
    /// EQUIVOCÓ DE CULPABLE — la causa real apareció en el 17
    /// =====================================================================
    /// Se deja escrito porque es el ejemplo más caro de la sesión de una
    /// investigación rigurosa que llega a la conclusión equivocada.
    ///
    /// LA PARTE QUE ERA CORRECTA (y sigue siéndolo, medida, no supuesta): el
    /// degradado frío NUNCA llegaba a la boquilla. `PaintClimate` separaba el
    /// mundo en dos funciones que jamás se mezclaban — para y &gt;=
    /// `SurfaceFloorY0`(144) el ambiente dependía SOLO de x, para y&lt;144
    /// SOLO de y. La boquilla vive en (279, 184): y=184 &gt;= 144, así que
    /// caía siempre en la función que ignora la altura, y x=279 &gt;=
    /// `LabX0`(262) devolvía base sin excepción. Aun mirando la fila más alta
    /// del sótano (y=143) el degradado YA valía raw 70. Entre el frío sin
    /// degradar y el grifo había un mínimo de 41 filas clavadas en base (12 de
    /// losa maciza + 29 de aire), 80 filas contra el frío puro. El pozo
    /// (`WellX0..X1`=343..382) tampoco ayudaba: su tramo por encima de y=144
    /// heredaba el clima de superficie y está a 64 celdas horizontales del
    /// grifo, con `Empty` que difunde pero no convecta.
    ///
    /// LA CONCLUSIÓN QUE ERA FALSA: de ahí se dedujo "entonces es VARIANZA DE
    /// SEMILLA, un 38% de seeds tienen `waterFreezeC` &gt;= 4°C y el sótano
    /// congela solo; no es un bug, es personalidad del universo". El
    /// razonamiento numérico era bueno y la conclusión no se sostenía, porque
    /// se detuvo en cuanto encontró UNA explicación compatible con los
    /// síntomas en vez de comprobar el camino por el que el agua nace.
    ///
    /// LA CAUSA REAL (playtest 17, `Game/Dispenser.cs`): `EmitTick` llamaba a
    /// `AlkahestSim.Paint`, que NO TOCA `temp`. La celda recién emitida
    /// heredaba la temperatura que ese hueco tuviera de antes — si la boquilla
    /// o la pila se habían enfriado alguna vez (un charco frío previo, hielo
    /// que estuvo ahí, la piedra gélida cerca), el agua nacía YA CONGELADA,
    /// en cualquier seed y sin que el clima interviniera para nada. Es
    /// literalmente el mismo fallo que "pintar hielo produce agua" (regla 22),
    /// y explica también el segundo síntoma de Cesar ("en la hornilla se
    /// volvía agua pero los bordes se hacían hielo"): llegaba helada y solo
    /// se derretía sobre el 40% del fondo que cubre la placa
    /// (`HeatPlate.FootprintFraction`). Arreglado con `PaintStable` en los dos
    /// puntos de emisión.
    ///
    /// LA LECCIÓN, que vale más que el bug: descartar un sospechoso NO es
    /// identificar al culpable. La investigación probó de sobra que el clima
    /// era inocente y aun así firmó una sentencia contra el siguiente
    /// sospechoso disponible sin comprobarlo con la misma exigencia. Cuando
    /// un síntoma es "materia recién creada aparece en un estado imposible",
    /// el primer sitio que hay que mirar es SIEMPRE quién la crea y a qué
    /// temperatura la deja — no el entorno donde aparece.
    ///
    /// RECOMENDACIÓN QUE SIGUE VIVA (`Universe.cs`, no tocado): `waterFreezeC`
    /// y `crystallizeThresholdC` se sortean en rangos solapados, así que en
    /// algunas seeds el umbral de cristalizar cae por debajo del de congelar
    /// y cristalizar exige un frío que ya está fabricando hielo. Con el clima
    /// fuera esto ya no produce congelaciones espontáneas, pero sigue siendo
    /// un solape por azar, no por diseño.
    ///
    /// =====================================================================
    /// CERCA DEL INICIO (playtest 16, "lo básico en el primer cuarto")
    /// =====================================================================
    /// Cesar: *"Tendría que estar lo básico en el primer cuarto... para no
    /// obligar a jugar por todas partes al inicio"*, y antes: *"se siente
    /// súper grande, pero imagino que podría ser desbloqueo de áreas por
    /// niveles"*. El mundo de 3x2 pantallas NO se encoge (`CellGrid.W/H`
    /// intactos, es el TAMAÑO FINAL, pedido así en el playtest 15) — lo que
    /// cambia es la DISTRIBUCIÓN dentro de él: antes, "lo esencial" (grifos,
    /// pila, una cuba, la Tolva) estaba repartido a lo ancho de CASI TODO el
    /// mundo (cuba B en x118, boca de la Tolva hasta x694, 576 celdas de por
    /// medio, 75% del ancho); ahora esos cuatro elementos caben en 363
    /// celdas (x187 a x550, 47% del ancho) — SIN mover ni una celda del banco de grifos, la
    /// pila, el pilar, el pozo o el sótano, que ya estaban validados y son
    /// justo la geometría que el encargo pide no arriesgar de nuevo (ver la
    /// nota "en la ronda pasada el pilar invadió la pila" del propio
    /// encargo). Dos cambios, nada más:
    ///  · `VatBX0`: 118 -> 187. La cuba B (la que `MasterSupplies` siembra
    ///    con el retoño de Vivium) pasa a pegarse al borde derecho de
    ///    CULTIVO (su labio cae justo en `CultivoX1`=250, a 11 celdas de
    ///    `LabX0` — el MISMO hueco de "zona contigua" que ya usaba el
    ///    archivo entre CULTIVO y LABORATORIO, no un número nuevo). Cuba A
    ///    se queda en `VatAX0`=30, sin tocar: es la que se queda LEJOS a
    ///    propósito.
    ///  · `EntregaX1`: 751 -> 607. `ChuteWallX0` (`= EntregaX1 - 90`, fórmula
    ///    SIN TOCAR) pasa de 661 a 517 — 11 celdas después de `LabX1`(505),
    ///    otra vez el mismo hueco de zona contigua. Como el zócalo y la
    ///    torre del contrafuerte siguen dibujándose hasta `CellGrid.W` (no
    ///    hasta `EntregaX1`, esa parte de `BuildDeliveryNiche` no cambió), la
    ///    Tolva queda con la BOCA pegada a LABORATORIO (x529..550, ver
    ///    `ChuteMouthX0/X1`) pero el BLOQUE DE PIEDRA que la sostiene sigue
    ///    llegando hasta x=767 igual que antes — de hecho ahora es MÁS ANCHO
    ///    (517..767, 251 celdas, antes 661..767, 107) porque su borde
    ///    izquierdo se acercó y el derecho no se movió: cerca para usar, y
    ///    se ve seguir lejos con MÁS piedra visible, no menos, gratis, sin
    ///    dibujar nada nuevo.
    ///
    /// DISTANCIAS MEDIDAS (el aprendiz nace sobre el centro de `BasinInterior`,
    /// x≈303, ver `Game/AlkahestGameBootstrap.SpawnApprentice`, NO TOCADO):
    ///  · Antes: cuba B a 122 celdas del spawn, boca de la Tolva a 370.
    ///  · Ahora: cuba B a 53 celdas del spawn, boca de la Tolva a 226.
    /// El bucle completo (grifo -> transformar en la cuba/banco -> Tolva) ya
    /// no exige cruzar el mundo de punta a punta — el vuelo de punta a punta
    /// (768 celdas, ~6.9s a `moveSpeed`, ver el comentario de medición en
    /// `Game/ApprenticeController.cs`, NO TOCADO) sigue existiendo, pero ya
    /// no hace falta para jugar la primera ronda del bucle básico.
    ///
    /// QUÉ SE QUEDA LEJOS A PROPÓSITO (permitido explícitamente por el
    /// encargo, y SIGUE SIENDO CIERTO tras el playtest 19 de abajo): cuba A
    /// (`VatAX0`=30, la segunda cuba de cultivo) y el SÓTANO entero (solo se
    /// llega volando por el POZO, sin cambios — ES la razón de que se sienta
    /// como una sala aparte). (playtest 19: este párrafo decía además que la
    /// bandeja fría y el estante de redomas "no se movieron" — ESO YA NO ES
    /// CIERTO, se movieron esa ronda porque SÍ acabaron formando parte del
    /// bucle básico. Ver la sección "EL TALLER SE COMPACTA AÚN MÁS" más
    /// abajo para el porqué completo; se deja esta nota aquí, en vez de
    /// borrar el párrafo entero, para que quede constancia de que el
    /// criterio de esta zona CAMBIÓ de una ronda a la siguiente y por qué.)
    ///
    /// INSINUACIÓN "SE VE HACIA LOS LADOS" (pedida literalmente por Cesar):
    ///  · Izquierda: cuba A, visible/alcanzable nada más salir de LABORATORIO
    ///    por ese lado — el jugador ve que CULTIVO sigue más allá de cuba B.
    ///  · Derecha: el contrafuerte de la Tolva, que YA llegaba hasta el muro
    ///    del mundo y sigue llegando (ver arriba) — la boca está cerca pero
    ///    la piedra que la sostiene se ve continuar mucho más allá.
    ///  · Abajo: el POZO (hueco real en el suelo de LABORATORIO, siempre
    ///    visible desde el banco de grifos, sin cambios) insinúa que el
    ///    taller también baja — exactamente el "una parte se puede ver hacia
    ///    los lados" que pedía Cesar, en las tres direcciones que tenía
    ///    sentido dar (los muros norte/sur del mundo son el borde real).
    ///
    /// =====================================================================
    /// PLAYTEST 19: EL TALLER SE COMPACTA AÚN MÁS ("todo a mano")
    /// =====================================================================
    /// Cesar, la noche antes de este playtest, sobre por qué la corrección
    /// del playtest 16 (arriba) no bastaba todavía: *"hacer las pruebas es
    /// cansado donde todo está separado... debería transmitir la sensación
    /// de estar yo en un lugar pequeño que luego quizás con los niveles se
    /// me amplíe el espacio, pero ahora eso no es necesario"*. El playtest
    /// 16 ya había acercado cuba B y la Tolva, pero dejó la bandeja fría y
    /// el estante DONDE ESTABAN a propósito ("no forman parte del bucle
    /// básico" era el argumento de aquella ronda) — y esos dos, más la
    /// Tolva (que seguía siendo, con diferencia, lo más lejano), obligaban
    /// a seguir cruzando buena parte de LABORATORIO para la primera hora de
    /// juego. Esta ronda termina ese trabajo.
    ///
    /// EL MUNDO NO SE ENCOGE (repetido a propósito: es la restricción más
    /// fácil de romper sin querer en una ronda de compactación).
    /// `CellGrid.W/H` siguen en 768x288 — es el tamaño FINAL que el
    /// desbloqueo de áreas por niveles va a usar. Lo que cambia es DÓNDE,
    /// dentro de ese mundo, viven la bandeja fría, el estante y la boca de
    /// la Tolva. Cuba A y el SÓTANO se quedan exactamente donde estaban:
    /// lejos a propósito, sin ningún cambio (ver "QUÉ SE QUEDA LEJOS" arriba).
    ///
    /// TRES CAMBIOS, los tres dentro de este archivo:
    ///
    ///  1. BANDEJA FRÍA + ESTANTE FLOTAN AHORA SOBRE EL BANCO, EN VEZ DE
    ///     VIVIR APARTE AL NIVEL DEL TECHO. Antes: `ChillTrayX0`=390,
    ///     `RackX0`=447, ambos en `Y0`=246 (al nivel del techo de
    ///     LABORATORIO) — 96 y 150 celdas del punto de aparición
    ///     respectivamente. Ahora: `ChillTrayX0`=262 (la MISMA X que
    ///     `TapPillarX0`/`BenchX0`, ambos = `LabX0`), `RackX0`=320,
    ///     los dos en `Y0`=236 — FLOTANDO justo encima del pilar de grifos
    ///     y la meseta del banco. No es una idea nueva: es la que ya
    ///     predecía `Game/WorkshopBackdrop.cs` desde el playtest 4 ("vigas
    ///     horizontales... explican los estantes flotantes"), aquí por fin
    ///     aprovechada de verdad en vez de solo en el fondo pintado. Y=236
    ///     deja 3 celdas de aire sobre `TapPillarTopY`=232 (el mismo margen
    ///     de `WallThickness` que usa el resto del archivo para "cerca pero
    ///     sin tocar") y 41 celdas de aire libre hasta el techo del mundo
    ///     (y=286) — de sobra, no se pega al muro. Anchuras SIN TOCAR (regla
    ///     del encargo: la calibración de patrones de otro encargo depende
    ///     de la medida exacta del interior de la bandeja fría, regla 24).
    ///
    ///  2. LA TOLVA SE ACERCA OTRA VEZ. `EntregaX1`: 607 -&gt; 470 (y
    ///     `EntregaX0`, que sigue sin alimentar ningún cálculo — solo
    ///     documentación/ASCII —, se ajusta a 380 para seguir marcando de
    ///     verdad dónde empieza el contrafuerte). `ChuteWallX0` (fórmula SIN
    ///     TOCAR otra vez, `= EntregaX1 - 90`) baja de 517 a 380; la boca
    ///     (`ChuteMouthX0/X1`) pasa de 529..550 a 392..413. Igual que en el
    ///     playtest 16, el contrafuerte SIGUE dibujándose hasta
    ///     `CellGrid.W` (`BuildDeliveryNiche` no cambió) — la piedra sigue
    ///     viéndose llegar hasta el muro del mundo, 137 celdas más ancha
    ///     todavía que en el playtest 16 (380..767 = 388 celdas, antes
    ///     517..767 = 251).
    ///
    ///  3. `LabX1` (505 -&gt; 384) es SOLO documentación/ASCII, igual que
    ///     `EntregaX0` (ninguno de los dos alimenta ningún cálculo — ver el
    ///     grep del docblock): se actualiza para dejar de mentir sobre
    ///     dónde termina de verdad el contenido de LABORATORIO ahora que
    ///     nada suyo llega ya hasta la x=505 original (la pieza más a la
    ///     derecha es el estante, que termina en 374).
    ///
    /// LO QUE **NO** SE TOCÓ, a propósito, para arriesgar lo mínimo posible
    /// (el encargo pide comprobación exhaustiva de solapes precisamente
    /// porque "en la ronda pasada un pilar invadió la pila y no se
    /// detectó" — cuantas menos piezas validadas se muevan, menos
    /// superficie de fallo): CULTIVO entero (`CultivoX0/X1`, `VatAX0/
    /// VatBX0` — cuba B YA estaba a 64 celdas del punto de aparición con el
    /// método de medida de esta ronda, sin tocar nada); el banco/pila/pilar
    /// de grifos (`BenchX0/X1`, `BasinX0/Width/Height`, `TapPillarX1/
    /// TopY` — la geometría que el playtest 16 ya marcó como "no tocar sin
    /// necesidad" sigue sin necesidad); el POZO (`WellX0/X1`: su hueco solo
    /// existe entre y=141..155 — muy por debajo de la nueva altura y=236 de
    /// la bandeja/estante, así que no hacía falta reubicarlo para dejarles
    /// sitio, comprobado por script, ver el informe de la ronda); y el
    /// SÓTANO entero.
    ///
    /// DISTANCIAS, MEDIDAS POR SCRIPT (no a ojo — el informe de la ronda
    /// trae el script completo; aquí solo el resultado, con un criterio de
    /// medida fijo: distancia euclídea desde el punto de aparición hasta el
    /// punto MÁS CERCANO del área realmente interactiva de cada aparato —
    /// el segmento de la fila de la placa en cubas/bandeja, el rectángulo
    /// del estante/boca, la boquilla más cercana en los grifos — nunca el
    /// centro geométrico de la pieza, que sobreestima cuánto hay que
    /// acercarse):
    ///
    ///   aparato                          antes (16)   ahora (19)
    ///   grifos (boquilla más cercana)        25.0         25.0  (sin tocar)
    ///   placa de cuba B                      64.4         64.4  (sin tocar)
    ///   piedra gélida (bandeja fría)         107.2         49.0
    ///   estante de redomas                   154.4         49.8
    ///   boca de la Tolva                     225.5         88.5
    ///   placa de cuba A (lejos, aposta)      215.7        215.7 (sin tocar)
    ///
    /// Los seis aparatos del bucle básico y su vecindad inmediata (grifos,
    /// pila -- el propio punto de aparición --, cuba B, bandeja fría,
    /// estante, Tolva) caen ahora dentro de los ~90 celdas que pide el
    /// encargo; cuba A se queda fuera de ese radio a propósito, tal y como
    /// permite el encargo.
    ///
    /// RECIPIENTES (regla 24, medido de nuevo tras el reparto — NINGUNA
    /// medida cambió, solo su posición en el mundo):
    ///  · Cuba (A o B): interior 58x37 (`VatInteriorX0/X1`, `VatInteriorY0/
    ///    Y1`) = 2146 celdas, de sobra para encargos de 90-130 y para 3-4
    ///    repeticiones de un rasgo de 5-12 celdas en cualquiera de sus dos
    ///    dimensiones.
    ///  · Pila de recogida: interior 58x17 (`BasinInteriorX0/X1/Y0/Y1`).
    ///  · Bandeja fría: interior 44x7 (`ChillTrayInteriorX0/X1`, `ChillPlate
    ///    Row`..labio) — el más estrecho de los tres, igual sobra para 3-4
    ///    repeticiones de un rasgo de 5-12 celdas (44/12≈3.7, 44/5≈8.8).
    /// </summary>
    public static class SimLevelBuilder
    {
        // =================================================================
        // NIVEL BASE
        // =================================================================

        /// <summary>
        /// Filas 0..FloorTop de bedrock MACIZO bajo TODO el mundo (antes era
        /// el suelo del taller entero; ahora es la base sobre la que se apoya
        /// el SÓTANO — la superficie tiene su propio suelo, ver
        /// <see cref="SurfaceFloorY0"/>). Valor bajo (10, antes 14): el
        /// sótano necesita casi toda la mitad inferior del mundo como aire
        /// interior, así que el bedrock que lo sostiene se mantiene fino.
        /// </summary>
        public const int FloorHeight = 10;
        public const int FloorTop = FloorHeight - 1; // 9

        /// <summary>Grosor de pared estándar de cubas/bandejas/salas.</summary>
        public const int WallThickness = 3;

        // =================================================================
        // SUPERFICIE / SÓTANO — la división vertical del mundo en dos
        // mitades (playtest 15). `CellGrid.H` = 288 = 2 pantallas exactas,
        // así que la mitad es un número redondo sin necesidad de ajuste.
        // =================================================================

        /// <summary>Primera fila de la mitad de ARRIBA del mundo (superficie: Cultivo/Laboratorio/Entrega).</summary>
        public const int SurfaceFloorY0 = CellGrid.H / 2; // 144
        /// <summary>Grosor de la losa de suelo de la superficie (más gruesa que el bedrock: aquí se apoya TODO el taller de arriba).</summary>
        public const int SurfaceFloorHeight = 12;
        /// <summary>Última fila maciza de la losa de superficie. El interior jugable de la superficie empieza en SurfaceFloorTop+1.</summary>
        public const int SurfaceFloorTop = SurfaceFloorY0 + SurfaceFloorHeight - 1; // 155

        // =================================================================
        // ZONAS HORIZONTALES DE LA SUPERFICIE (x). Valores del encargo de
        // Cesar, con márgenes simétricos de 16 celdas a los muros del mundo
        // (x=0/767) y huecos de 11 celdas entre zonas contiguas — suficientes
        // para que el degradado de clima (ver PaintClimate) tenga sitio para
        // respirar sin tocar el borde de ninguna estructura.
        // =================================================================

        public const int CultivoX0 = 16;
        public const int CultivoX1 = 250;
        public const int LabX0 = 262;
        /// <summary>
        /// 505 -> 384 (playtest 19, "EL TALLER SE COMPACTA AÚN MÁS" en el
        /// docblock de la clase). Documentación/ASCII únicamente — igual que
        /// <see cref="EntregaX0"/>, no alimenta ningún cálculo (ver el grep
        /// del docblock) — se baja para dejar de mentir: desde este playtest
        /// nada de LABORATORIO llega ya hasta la x=505 original (la pieza
        /// más a la derecha es el estante, que termina en 374).
        /// </summary>
        public const int LabX1 = 384;
        /// <summary>
        /// 517 -> 380 (playtest 19). Documentación/ASCII únicamente, igual
        /// que <see cref="LabX1"/> — se ajusta para seguir marcando de
        /// verdad dónde empieza el contrafuerte de la Tolva ahora que
        /// <see cref="ChuteWallX0"/> también bajó a 380.
        /// </summary>
        public const int EntregaX0 = 380;
        /// <summary>
        /// 751 -> 607 (playtest 16) -> 470 (playtest 19, "EL TALLER SE
        /// COMPACTA AÚN MÁS" en el docblock de la clase). Solo alimenta
        /// <see cref="ChuteWallX0"/> (`= EntregaX1 - 90`, fórmula SIN TOCAR
        /// otra vez esta ronda): al bajar este valor, el zócalo de la Tolva
        /// se acerca 137 celdas más a LABORATORIO SIN dejar de dibujarse
        /// hasta <c>CellGrid.W</c> (esa parte de <see cref="BuildDeliveryNiche"/>
        /// usa el ancho del mundo, no `EntregaX1`) — la boca queda cerca, el
        /// bloque de piedra que la sostiene se sigue viendo llegar hasta el
        /// muro, exactamente el mismo efecto que ya buscaba el playtest 16,
        /// llevado más lejos porque la boca (225.5 celdas del spawn) seguía
        /// siendo, con diferencia, el aparato del bucle básico más alejado.
        /// </summary>
        public const int EntregaX1 = 470;

        // =================================================================
        // CULTIVO: dos cubas grandes (donde se cría el Vivium) + una placa
        // ígnea por cuba (las coloca AlkahestGameBootstrap.SpawnHeatPlates,
        // que ya itera VatAX0/VatBX0 — no hace falta tocar nada ahí). El
        // "grifo de nutriente" que pide el encargo para esta zona vive en la
        // columna de grifos de LABORATORIO (ver más abajo): Bootstrap solo
        // admite UNA columna de grifos (un único TapMountX/TapFirstY/TapStepY
        // para los 5), así que no hay forma de duplicar un grifo físico aquí
        // sin tocar AlkahestGameBootstrap (no editable en este encargo). El
        // aprendiz VUELA, así que cargar Nutriente desde el banco hasta las
        // cubas de Cultivo es un trayecto corto y trivial, no una fricción de
        // diseño real.
        //
        // (playtest 16) LAS DOS CUBAS YA NO SON SIMÉTRICAS A PROPÓSITO: cuba
        // A se queda LEJOS (VatAX0 sin tocar) y cuba B se acerca a LABORATORIO
        // (VatBX0 nuevo) — ver "CERCA DEL INICIO" en el docblock de la clase
        // para los números completos (distancias, huecos, exposición al
        // degradado de clima).
        // =================================================================

        public const int VatWidth = 64;  // antes 58: "cubas grandes", algo más anchas.
        public const int VatHeight = 40; // sin cambios: interior 37 filas útiles ya validado (regla 24).
        /// <summary>La cuba LEJOS a propósito (playtest 16) — sin tocar desde el playtest 15.</summary>
        public const int VatAX0 = 30;
        /// <summary>
        /// 118 -> 187 (playtest 16): pegada al labio derecho de CULTIVO
        /// (<see cref="CultivoX1"/>=250, 11 celdas de hueco hasta `LabX0`,
        /// el mismo hueco de "zona contigua" que ya usaba el archivo). Es la
        /// cuba CERCA del inicio. `MasterSupplies` sigue sembrando el retoño
        /// de Vivium aquí (cuba B) — sigue leyendo esta constante por nombre,
        /// no le importa dónde cae.
        /// </summary>
        public const int VatBX0 = 187;

        /// <summary>Suelo de las cubas: a ras de la losa de superficie, sin meseta extra (a diferencia del banco de Laboratorio, ver BenchMesetaHeight).</summary>
        public const int VatBaseY0 = SurfaceFloorTop + 1; // 156

        /// <summary>Fila donde vive la placa calefactora de una cuba (la última de su suelo de piedra).</summary>
        public const int VatPlateRow = VatBaseY0 + WallThickness - 1; // 158

        public static int VatInteriorX0(int vatX0) => vatX0 + WallThickness;
        public static int VatInteriorX1(int vatX0) => vatX0 + VatWidth - 1 - WallThickness;
        /// <summary>Primera fila útil del interior de una cuba (justo encima de su suelo).</summary>
        public const int VatInteriorY0 = VatBaseY0 + WallThickness;      // 159
        public const int VatInteriorY1 = VatBaseY0 + VatHeight - 1;      // 195 (labio)

        // =================================================================
        // LABORATORIO: "el centro de operaciones" — banco de trabajo (pila
        // de recogida + columna de grifos), y desde el playtest 19 la
        // bandeja fría y el estante de redomas FLOTAN encima de ese mismo
        // banco (antes vivían aparte, a la derecha, al nivel del techo —
        // ver "EL TALLER SE COMPACTA AÚN MÁS" en el docblock de la clase),
        // más el POZO que baja al SÓTANO. Todo cabe con margen en los 123
        // celdas de ancho de la zona (x262..384, `LabX1`, solo documentación
        // — el elemento más a la derecha es el estante, que termina en 374).
        // =================================================================

        /// <summary>Meseta maciza bajo la pila de recogida y el pilar de grifos — a diferencia de las cubas de Cultivo, el banco SÍ se eleva un poco sobre la losa (mismo lenguaje que el playtest 4 original).</summary>
        public const int BenchX0 = LabX0;       // 262
        public const int BenchX1 = 345;
        public const int BenchMesetaHeight = 4;
        public const int BenchTopY = SurfaceFloorTop + BenchMesetaHeight; // 159

        /// <summary>Pila de recogida: cubeta ancha y poco profunda donde caen TODOS los grifos. Se apoya sobre la meseta (BenchTopY+1).</summary>
        public const int BasinY0 = BenchTopY + 1; // 160
        public const int BasinX0 = 272;
        public const int BasinWidth = 64;
        public const int BasinHeight = 20; // labio en y=179
        public const int BasinInteriorX0 = BasinX0 + WallThickness;                   // 275
        public const int BasinInteriorX1 = BasinX0 + BasinWidth - 1 - WallThickness;  // 332
        public const int BasinInteriorY0 = BasinY0 + WallThickness;                   // 163
        public const int BasinInteriorY1 = BasinY0 + BasinHeight - 1;                 // 179

        /// <summary>Pilar de piedra al que se atornillan los grifos, en columna vertical compacta, pegado al borde izquierdo de LABORATORIO (mismo criterio que el playtest 4: flush con el muro/borde de zona más cercano).</summary>
        public const int TapPillarX0 = LabX0; // 262
        public const int TapPillarX1 = 274; // = BasinInteriorX0-1 (275-1): flush con la pila, sin invadir su interior (antes 278 solapaba 4 columnas de la pila con piedra sólida).
        public const int TapPillarTopY = 232;

        /// <summary>Celda de anclaje de los grifos. El caño sale en voladizo hacia la pila de recogida (ver Dispenser.SpoutOffsetCells).</summary>
        public const int TapMountX = TapPillarX1; // 274
        /// <summary>Altura del grifo más bajo: 5 celdas sobre el labio de la pila (BasinInteriorY1), mismo margen que el playtest 4 original — se ve caer desde justo encima de la boca de la pila.</summary>
        public const int TapFirstY = BasinInteriorY1 + 5; // 184
        /// <summary>Separación vertical entre grifos: 10 celdas (1 unidad de mundo), igual que siempre — bastan para que MachineFocus no ambigüe el grifo enfocado.</summary>
        public const int TapStepY = 10;

        // ---- Bandeja fría (estante) -------------------------------------------------------
        /// <summary>
        /// X0: 390 -> 262 (playtest 19, "EL TALLER SE COMPACTA AÚN MÁS" en
        /// el docblock de la clase). Y0: 246 -> 236 (ver más abajo). Antes
        /// vivía a la derecha del banco, al nivel del techo (96 celdas del
        /// spawn); ahora FLOTA justo encima del pilar de grifos y la meseta
        /// (misma X que <see cref="TapPillarX0"/>/<see cref="BenchX0"/>,
        /// ambos = LabX0), bajada del techo al primer hueco libre sobre el
        /// pilar — mismo idioma visual que ya predecía
        /// Game/WorkshopBackdrop.cs ("vigas... explican los estantes
        /// flotantes"), aquí por fin aprovechado. Anchura SIN TOCAR (regla
        /// del encargo: la calibración de patrones de otro encargo depende
        /// de este número, ver <see cref="ChillTrayWidth"/>).
        /// </summary>
        public const int ChillTrayX0 = 262;
        public const int ChillTrayWidth = 50; // antes 52: mismo orden de magnitud, ver medición de recipientes en el resumen. SIN TOCAR esta ronda (regla 24: otro encargo calibra patrones contra esta medida).
        /// <summary>
        /// 246 -> 236 (playtest 19). Dejamos 3 celdas de aire libre sobre
        /// <see cref="TapPillarTopY"/>=232 (el mismo margen de
        /// <see cref="WallThickness"/> que usa el resto del archivo para
        /// "cerca pero sin tocar") y 41 celdas de aire libre hasta el techo
        /// del mundo (y=286, la última fila que no es el borde macizo) — de
        /// sobra, no se pega al muro.
        /// </summary>
        public const int ChillTrayY0 = 236;
        public const int ChillTrayHeight = 10;
        public const int ChillTrayInteriorX0 = ChillTrayX0 + WallThickness;                       // 265
        public const int ChillTrayInteriorX1 = ChillTrayX0 + ChillTrayWidth - 1 - WallThickness;  // 308
        public const int ChillTrayInteriorY0 = ChillTrayY0 + WallThickness;                       // 239
        /// <summary>Fila donde vive la piedra fría (última de su suelo).</summary>
        public const int ChillPlateRow = ChillTrayY0 + WallThickness - 1; // 238

        // ---- Estante de redomas -------------------------------------------------------------
        /// <summary>
        /// 447 -> 320 (playtest 19). Pegado a <see cref="ChillTrayX0"/> con
        /// el mismo hueco de 8 celdas que ya los separaba antes de moverse
        /// (<c>320 - (262+50) = 8</c>) — el estante sigue siendo lo
        /// inmediatamente contiguo a la bandeja fría, solo que ahora los dos
        /// están a un vistazo vertical de la pila, no a un vuelo horizontal.
        /// </summary>
        public const int RackX0 = 320;
        /// <summary>374 (antes 501): ancho SIN TOCAR (55 celdas, <c>RackX1-RackX0+1</c>), solo reubicado.</summary>
        public const int RackX1 = 374;
        public const int RackY0 = 236; // 246 -> 236 (playtest 19): mismo nivel que la bandeja fría — sigue siendo un único "estante superior" leído de un vistazo, solo que ahora sobre el banco en vez de sobre el techo.
        public const int RackHeight = 3;
        public const int RackTopY = RackY0 + RackHeight; // 239

        // ---- El POZO: la única conexión entre LABORATORIO y SÓTANO -------------------------
        // El aprendiz vuela, así que no hace falta una escalera — solo un
        // hueco vertical real que atraviese el techo del sótano Y la losa de
        // superficie a la vez (ver BuildSotano). SIN TOCAR en el playtest 19
        // pese a que la bandeja fría/el estante se mudaron encima del banco:
        // el hueco del pozo solo existe entre y=141..155 (ver WellCarveY0/
        // Y1 abajo), muy por debajo de la nueva altura y=236 de la bandeja/
        // estante — no hacía falta reubicarlo para dejarles sitio (ver la
        // tabla de solapes del informe de la ronda).
        public const int WellX0 = 343;
        public const int WellX1 = 382;
        /// <summary>Primera fila que se talla vacía: el techo del sótano (las WallThickness filas superiores de su caja).</summary>
        public const int WellCarveY0 = SotanoY1 - WallThickness + 1; // 141
        /// <summary>Última fila que se talla vacía: la losa de superficie entera, hasta tocar el interior ya abierto del Laboratorio.</summary>
        public const int WellCarveY1 = SurfaceFloorTop; // 155

        // =================================================================
        // SÓTANO: sala bajo LABORATORIO, mitad inferior del mundo. Se llega
        // VOLANDO por el pozo — por eso no hay escaleras, solo muros y
        // plataformas que le dan FORMA al espacio (petición explícita del
        // encargo). Más ancho que el propio Laboratorio (x220..530; LabX1
        // bajó a 384 en el playtest 19, así que ahora es AÚN más ancho en
        // proporción) para que se sienta como una sala propia, no un simple
        // hueco bajo la huella exacta de arriba. SIN TOCAR esta ronda: el
        // sótano se queda lejos a propósito (ver "QUÉ SE QUEDA LEJOS" en el
        // docblock de la clase).
        // =================================================================

        public const int SotanoX0 = 220;
        public const int SotanoX1 = 530;
        /// <summary>Suelo del sótano: se apoya directo en el bedrock del mundo.</summary>
        public const int SotanoY0 = FloorHeight; // 10
        /// <summary>Techo del sótano: justo bajo la losa de superficie (contiguo, sin hueco de aire entre los dos).</summary>
        public const int SotanoY1 = SurfaceFloorY0 - 1; // 143

        public const int SotanoInteriorX0 = SotanoX0 + WallThickness; // 223
        public const int SotanoInteriorX1 = SotanoX1 - WallThickness; // 527
        public const int SotanoInteriorY0 = SotanoY0 + WallThickness; // 13
        public const int SotanoInteriorY1 = SotanoY1 - WallThickness; // 140

        /// <summary>Plataforma de cristalización: justo bajo el pozo, para que "aterrizar" y "estar donde se trabaja" sean el mismo gesto.</summary>
        public const int SotanoPlinthX0 = WellX0 - 10; // 333
        public const int SotanoPlinthX1 = WellX1 + 10; // 392
        public const int SotanoPlinthHeight = 8;

        /// <summary>Dos repisas SOLO para dar forma a la caja vacía (no son caminos: el aprendiz vuela).</summary>
        public const int SotanoLedgeAX0 = 250;
        public const int SotanoLedgeAX1 = 310;
        public const int SotanoLedgeAY0 = 55;
        public const int SotanoLedgeBX0 = 440;
        public const int SotanoLedgeBX1 = 500;
        public const int SotanoLedgeBY0 = 90;
        public const int SotanoLedgeHeight = 6;

        // =================================================================
        // ENTREGA: la Tolva del Maestro + espacio de maniobra alrededor (ver
        // Game/DeliveryChute.cs). Mismas proporciones internas que el diseño
        // original (jambas a +12/-33 del zócalo), solo reubicadas y con la
        // base en la losa de superficie en vez del bedrock del mundo.
        //
        // (playtest 16) LA BOCA SE ACERCÓ A LABORATORIO, EL CONTRAFUERTE NO
        // SE ENCOGIÓ: `ChuteWallX0` bajó de 661 a 517 (11 celdas de hueco
        // tras `LabX1`, "CERCA DEL INICIO" en el docblock de la clase), pero
        // el zócalo y la torre se siguen dibujando hasta `CellGrid.W` más
        // abajo (`BuildDeliveryNiche`, sin tocar esa parte) — el bloque de
        // piedra es ahora MÁS ANCHO que antes (de 517 a 767, 251 celdas, en
        // vez de 661 a 767, 107), no más corto. Es justo el efecto que pide
        // el encargo ("insinuar que se extiende"): la boca queda cerca de
        // usar, la piedra se ve seguir mucho más allá.
        //
        // (playtest 19) SE REPITE LA MISMA JUGADA, MÁS LEJOS: `ChuteWallX0`
        // baja de 517 a 380 (mismo criterio: 11 celdas tras el nuevo `LabX1`
        // =384, "EL TALLER SE COMPACTA AÚN MÁS" en el docblock de la clase)
        // porque, medida la distancia real al punto de aparición, la boca
        // seguía siendo con diferencia el aparato más lejano del bucle
        // básico (225.5 celdas) pese a la corrección del playtest 16. El
        // contrafuerte sigue sin encogerse (de 380 a 767, 388 celdas: MÁS
        // ancho todavía que los 517-767=251 del playtest 16).
        // =================================================================

        public const int ChuteWallX0 = EntregaX1 - 90; // 380 (playtest 16: 517; antes: 661)
        public const int ChuteMouthX0 = ChuteWallX0 + 12; // 392 (playtest 16: 529; antes: 673)
        public const int ChuteMouthX1 = ChuteMouthX0 + 21; // 413 (playtest 16: 550; antes: 694; 22 celdas de boca, igual que el playtest 4)
        /// <summary>
        /// (playtest 34) LA TOLVA VIAJA CON EL CUARTO. Era
        /// `SurfaceFloorTop + 1 + ChuteBaseHeight + 3` = 189, una cota
        /// heredada del taller CLÁSICO (la losa de superficie), que en la
        /// construcción del cuarto íntimo ni siquiera existe -- funcionaba
        /// por coincidencia numérica con `CuartoY0`=168. Con el cuarto
        /// bajando 32 filas (ver el bloque PLAYTEST 34) esa coincidencia se
        /// rompía y la boca habría quedado 32 celdas por encima del atrio del
        /// Ensayo, con el pasillo entrando por su tercio bajo en vez de por
        /// arriba. Pasa a derivarse de `CuartoY0` con EXACTAMENTE el mismo
        /// desfase de siempre (189-168 = 21): el pozo no se rediseña ni se
        /// redimensiona -- 50 filas, las mismas del playtest 4 -- solo se
        /// traslada con la sala. Todos sus consumidores
        /// (Game/DeliveryChute.cs, Game/DayCycle.cs, Audio/DirectorDeAudio.cs,
        /// Game/WorkshopBackdrop.cs) lo leen POR NOMBRE.
        /// </summary>
        public const int ChuteMouthY0 = CuartoY0 + 21; // 157 (era 189 con CuartoY0=168: mismo desfase)
        public const int ChuteMouthY1 = ChuteMouthY0 + 49; // 206 (altura, 50 filas, sin cambios desde el playtest 4)

        /// <summary>
        /// Cuántas filas del FONDO del pozo son la "boca que traga" (ver
        /// Game/DeliveryChute.cs, que mantiene su PROPIA copia de este valor
        /// a propósito — SimLevelBuilder es de solo lectura para esa tarea).
        /// </summary>
        public const int ChuteSillRows = 3;

        private const int ChuteBaseHeight = 30; // alto del zócalo ancho del contrafuerte (antes 28).

        // =================================================================
        // EL CLIMA (playtest 17: RETIRADO POR ZONAS, ver el docblock de la
        // clase). Aquí vivían CultivoAmbientRaw (raw 73), SotanoAmbientRaw
        // (raw 62) y los anchos de degradado ClimaGradienteX/Y (45/40). Ya no
        // existen: el mundo entero usa CellGrid.AmbientRaw. No se dejan como
        // constantes muertas a propósito -- una constante sin uso invita a
        // volver a enchufarla sin leer POR QUÉ se desenchufó.
        // =================================================================

        // =====================================================================
        // EL CUARTO ÍNTIMO (playtest 21, EL PIVOT) — "el taller grande no
        // desaparece, está ENTERRADO". `BuildTestLevel` (arriba, SIN TOCAR
        // ni una constante) sigue existiendo íntegro porque el plano clásico
        // vuelve a construirse en cuanto el jugador excave lo bastante lejos
        // con el cincel: nada de lo de arriba era trabajo perdido. Lo que
        // arranca la partida AHORA es `BuildCuartoIntimo`: TODO el mundo
        // (768x288, sin excepción) nace de `MaterialId.Stone`, y se excava a
        // mano UNA sola cámara pequeña alrededor de donde el aprendiz
        // aparecía en el plano viejo (x≈303, y≈189 -- ver el comentario de
        // `SpawnApprentice` en `Game/AlkahestGameBootstrap.cs`, que sigue
        // siendo la referencia histórica aunque ese código ya no se llame en
        // este modo): así la cámara (`SimRenderer.FitMainCamera`, que sigue
        // sin tocar ninguna coordenada) no tiene que aprender nada nuevo, el
        // encuadre de siempre cae justo encima de la cámara íntima.
        //
        // QUÉ HAY DENTRO, Y NADA MÁS (pedido explícito: "la sala tiene que
        // sentirse grande y casi vacía"):
        //  · la CUNA (`DrawUShape`, la misma primitiva de las cubas de
        //    Cultivo del plano viejo) donde se asienta el Rescoldo
        //    (`Game/Criatura.cs`, propiedad del otro encargo -- este archivo
        //    solo expone el ANCLA, `CunaCriaturaX/Y`, "celda de SUELO sobre
        //    la que se asienta" según el contrato).
        //  · la REPISA (una plataforma flotante, mismo lenguaje visual que
        //    `RackX0`/`ChillTrayX0` del plano viejo: "las vigas horizontales
        //    de WorkshopBackdrop ya explican los estantes flotantes") donde
        //    se apoya el Capullo (`CapulloX/Y`). REUBICADA (ronda de
        //    integración tras la primera entrega): el otro encargo le dio al
        //    Rescoldo calor PROPIO -- una criatura contenta empuja
        //    temperatura a su alrededor, y "cuidar produce vida" depende de
        //    que ese calor LLEGUE al capullo. Midieron con una réplica de
        //    `SimStepper.DiffuseTemperature` que el alcance útil son 10-14
        //    celdas (más allá, el tirón hacia ambiente levanta un muro que
        //    no se cruza). TERCERA RONDA (ver la sección de abajo): la
        //    repisa YA NO vive encima de la cuna, vive AL LADO, en el MISMO
        //    suelo (`RepisaY0`==`CuartoY0`) -- la separación de 12 celdas es
        //    ahora horizontal, medida con la métrica REAL que usa
        //    `Game/Criatura.ApplyCalorTick` (Chebyshev, no euclídea: ver
        //    `CapulloDistanciaX` más abajo para la cuenta completa).
        //  · un MONTÓN de `MaterialId.Nutrient` TOCANDO el lado IZQUIERDO de
        //    la cuna (ver `NutrienteMoundX0`, más abajo): CRÍTICO, según el
        //    contrato -- `Sim/SimStepper.GrowthTick` (NO TOCADO por este
        //    encargo) solo hace crecer una célula de Vivium si tiene
        //    Nutrient en una celda ORTOGONALMENTE ADYACENTE, nunca "cerca"
        //    en radio. El sembrado inicial de `Criatura.SembrarCuerpoInicial`
        //    (disco de radio 2, ver ese archivo) llega hasta la columna
        //    `CunaCriaturaX-2`; el montón termina en `CunaCriaturaX-3`, así
        //    que su columna derecha queda literalmente pegada al borde del
        //    disco desde el tick 0 -- el primer estirón no depende de que el
        //    jugador haga nada primero. LADO IZQUIERDO A PROPÓSITO: la
        //    repisa vive arriba-derecha de la cuna, así que sembrar el
        //    nutriente al lado CONTRARIO maximiza la distancia real entre
        //    "donde crece la masa de Vivium" y "donde se apoya el capullo"
        //    (ver la comprobación de separación en el informe de la ronda:
        //    varios cientos de celdas de margen antes de que el crecimiento
        //    pudiera alcanzar la repisa): el Rescoldo no puede crecer hasta
        //    tragarse el capullo sin que antes se le acabe el nutriente
        //    local, que está del lado de donde nunca va.
        //  · un CHARCO pequeño de `MaterialId.Water` -- CERCA del grupo
        //    cuna/repisa (segunda ronda de integración, "AJUSTE
        //    COMPOSICIÓN": antes vivía en la esquina opuesta de la sala,
        //    exiliado a 57 celdas; ahora a un puñado de celdas de la pared
        //    de la cuna, "algo con lo que experimentar" que de verdad
        //    invita porque se ve junto a lo demás en el primer plano) -- en
        //    una cubeta en miniatura (misma primitiva `DrawUShape` que la
        //    cuna, sobre el mismo suelo `CuartoY0`) para que se lea como un
        //    charco y no como una mancha que se desparrama por todo el
        //    suelo llano (Water es Liquid de verdad, fluye). TERCERA RONDA:
        //    pasa del lado DERECHO de la cuna (compartido con la repisa) al
        //    IZQUIERDO -- la lectura de izquierda a derecha queda charco ->
        //    cuna/criatura -> repisa/capullo, ver la sección de abajo.
        //  · nada de grifos, placas, piedra gélida ni estante: ese
        //    equipamiento sigue viviendo en `BuildTestLevel`/
        //    `AlkahestGameBootstrap` tal cual, a la espera de la fase en la
        //    que el jugador excave hasta él.
        //
        // SEGUNDA RONDA DE INTEGRACIÓN -- "AJUSTE COMPOSICIÓN" (Cesar vio el
        // PNG de la cámara ampliada y encontró la geometría correcta pero la
        // COMPOSICIÓN mal: todo apelotonado en la esquina inferior izquierda
        // de una caja de 70 celdas de alto, con dos tercios del encuadre
        // vacíos arriba y el aprendiz flotando solo en esa nada). Dos
        // cambios, los dos de constantes, geometría interna intacta salvo
        // traducción de posición:
        //  1. LA SALA BAJA DE ALTO: `CuartoY1-CuartoY0+1` pasa de 70 a 42
        //     celdas (dentro del 40-45 pedido). El ANCHO (110) no se toca,
        //     ni tampoco `CuartoX1`/`ChuteWallX0`: la distancia a la Tolva
        //     (23/35 celdas, ver más abajo) queda exactamente igual, tal
        //     como se pidió explícitamente.
        //  2. TODO SE RECOMPONE ALREDEDOR DE LA CRIATURA en vez de en la
        //     esquina: la cuna pasa del borde izquierdo (antes a 15 celdas
        //     del muro) al centro-izquierda de la sala; la repisa/capullo
        //     sigue arriba-derecha DENTRO del mismo alcance térmico 10-14
        //     (la traducción no cambia ninguna distancia relativa, solo el
        //     origen); el charco se acerca de 57 a 10 celdas del muro de la
        //     cuna (antes exiliado al otro extremo); y el aprendiz nace
        //     DENTRO de la boca abierta de la cuna, a ~6.4 celdas del centro
        //     del disco sembrado del Rescoldo, en vez de sobrevolando el
        //     centro geométrico del vacío. Cálculo del encuadre de cámara
        //     real desde ese punto de aparición: ver el docblock de
        //     `AprendizX/Y` más abajo.
        //
        // =====================================================================
        // TERCERA RONDA: CESAR LO VIO CORRER EN SU UNITY -- tres correcciones
        // concretas tras jugarlo de verdad (el zoom vive en SimRenderer, las
        // otras dos aquí)
        // =====================================================================
        // (1) EL ZOOM ERA INSUFICIENTE. `SimRenderer.CuartoIntimoZoomFactor`
        //     (0.7) mostraba ~100 celdas de alto para una sala de 42 -- "dos
        //     tercios de la pantalla son roca". Baja a 5/16=0.3125 (45 celdas
        //     de alto exactas, dentro del 45-55 pedido) -- ver el docblock de
        //     esa constante para la derivación completa y por qué NO llega a
        //     cero roca visible (la cámara de seguimiento con zona muerta
        //     ancla al aprendiz al 65% de la altura del encuadre, así que con
        //     el aprendiz cerca del suelo de la sala una parte de roca de
        //     verdad SIGUE entrando por abajo -- se documenta la cifra real,
        //     no se finge que desaparece).
        // (2) LA COMPOSICIÓN SE ALINEA EN UNA LÍNEA. Antes la repisa flotaba
        //     13 celdas por ENCIMA de la cuna (mismo plano solo de nombre);
        //     ahora `RepisaY0` = `CuartoY0`, el MISMO suelo que usan la cuna y
        //     el charco -- las tres estructuras comparten fila de suelo
        //     exacta (`CapulloY` == `CunaCriaturaY`, diferencia CERO), leídas
        //     de izquierda a derecha: charco -> cuna/criatura -> repisa/
        //     capullo, con el aprendiz naciendo a la izquierda de la
        //     criatura sobre el mismo grupo. El alcance térmico 10-14 (real:
        //     distancia CHEBYSHEV desde `Game/Criatura.ApplyCalorTick`, NO
        //     euclídea como decía el comentario de la ronda anterior -- ver
        //     `CapulloX` más abajo) se conserva empujando la separación a
        //     HORIZONTAL casi pura (dx=12, dy=2). El montón de Nutrient y su
        //     adyacencia ortogonal al disco sembrado NO se tocan (siguen
        //     midiendo desde `CunaCriaturaX/Y`, que no se movió).
        // (3) LA REPISA SOLAPA EL SUELO DE LA CUNA A PROPÓSITO. Con la
        //     repisa en el suelo y el capullo a solo 12 celdas en X del
        //     centro de la cuna (`CunaCriaturaX`=295, cuna de 20 de ancho =
        //     9 celdas de radio a su pared derecha), una repisa de 16 celdas
        //     de ancho NO CABE sin tocar la huella de la cuna sin romper el
        //     alcance térmico -- la aritmética no da (9 + hueco + 8 > 14 para
        //     cualquier hueco positivo). Se acepta el solape (`RepisaX0`=299
        //     pisa las columnas 299-304 de la cuna): es INOFENSIVO porque esas
        //     columnas, en esas filas (168-170), YA eran el mismo suelo
        //     macizo de piedra que dibuja `BuildCuna` -- repisa y cuna
        //     comparten literalmente la misma losa, que es justo la lectura
        //     de "mismo plano" que pedía Cesar, no un error de solapamiento.
        //
        // LA TOLVA, SELLADA (el gancho del final del primer compás, SIN
        // TOCAR en esta ronda): `BuildDeliveryNiche` (sin tocar una sola
        // línea) se llama DESPUÉS de excavar la cámara, así que su hueco
        // (boca en `ChuteMouthX0..Y1`) queda tallado dentro de piedra que ya
        // era maciza -- ENTERRADO, exactamente como el resto del mundo.
        // Distancia real, medida por script: del borde derecho de la cámara
        // (`CuartoX1`=357, SIN CAMBIOS en esta ronda) al primer muro sólido
        // de la Tolva (`ChuteWallX0`=380) hay 23 celdas de piedra lisa;
        // hasta el hueco real de la boca (`ChuteMouthX0`=392, ya dentro del
        // propio contrafuerte, que también hay que atravesar) hay 35. Las
        // dos cifras caen dentro del "trecho corto pero real" que pedía el
        // encargo (20-40 celdas), y el rango vertical de la boca
        // (`ChuteMouthY0..Y1` = 189..238) SIGUE solapando con la cámara, ya
        // más baja (`CuartoY0..CuartoY1` = 168..209): la franja de solape
        // pasa de 35 a 21 filas (189..209) -- más corta que antes, pero
        // sigue siendo una franja real desde la que cavar en línea recta
        // hacia la derecha lleva a la Tolva, no a piedra eterna.
        // =====================================================================

        /// <summary>
        /// Límites de la cámara íntima (interior EXCAVADO, sin contar los
        /// muros de piedra que la rodean por fuera -- fuera de este
        /// rectángulo, todo el mundo es Stone macizo). Centro geométrico
        /// ((CuartoX0+CuartoX1)/2, (CuartoY0+CuartoY1)/2) = (302, 188), a una
        /// celda del punto histórico x≈303/y≈189 donde nacía el aprendiz en
        /// el plano viejo -- a propósito, para no pelearse con la cámara ni
        /// el encuadre. (playtest 26: `CuartoX0` bajó a 232, así que el
        /// centro geométrico REAL hoy es (294.5, 188), no (302, 188) -- esta
        /// cifra queda como registro histórico de por qué se eligió
        /// `AprendizX`/`AprendizY` en su momento, no como valor vigente; ver
        /// las constantes mismas, sin cambios, para la posición real.)
        ///
        /// ALTURA REDUCIDA (2ª ronda de integración, "AJUSTE COMPOSICIÓN"):
        /// 70 celdas de alto con todo el contenido viviendo en las ~20
        /// celdas de abajo se leía como una caja vacía, no como una sala
        /// íntima -- ver el docblock de la clase. 42 celdas (dentro del
        /// 40-45 pedido) deja aire de sobra sobre la criatura (la sala
        /// SIGUE pudiendo sentirse "grande pero mayormente vacía": con el
        /// grupo cuna/repisa/aprendiz terminando en Y=183 y el techo en
        /// Y=209, quedan 26 filas -- el 62% de la altura -- de aire libre
        /// arriba) sin que la mitad del encuadre sea negro. El ANCHO (110)
        /// no se toca -- Cesar lo confirmó explícitamente correcto.
        /// </summary>
        /// <summary>
        /// PLAYTEST 26 (CONTRATO_LEGIBILIDAD.md §2, "LA LÍNEA DEL TALLER"):
        /// 248 -&gt; 232. Cesar, playtest 25: *"si se ocupa más espacio no pasa
        /// nada"* -- el cuarto CRECE hacia la IZQUIERDA (ancho 110 -&gt; 126)
        /// para caber la línea completa de cinco estaciones + las dos pilas
        /// de recogida sin apretar. <see cref="CuartoX1"/> NO se toca (357):
        /// 357-232+1=126, la aritmética cuadra sola, así que todo lo anclado
        /// al lado DERECHO del cuarto (Columna/Ensayo/pasillo/Tolva, ninguno
        /// tocado por este playtest) queda exactamente donde estaba. Lo único
        /// que se desplaza es <see cref="CanoMontajeX"/> (`= CuartoX0`, los
        /// caños se mueven con la pared -- CORRECTO Y DESEADO, contrato:
        /// "verifica que el spawn del aprendiz y el pasillo a la Tolva siguen
        /// sanos"): comprobado, <see cref="AprendizX"/>=290 y
        /// <see cref="CarvePasilloTolva"/> (que arranca en `CuartoX1+1`, no en
        /// `CuartoX0`) no dependen de este valor, así que ninguno de los dos
        /// se mueve ni un píxel.
        /// </summary>
        /// <summary>
        /// PLAYTEST 27 (CONTRATO_TALLER_GRANDE, mandato 1): 232 -&gt; 140. Las
        /// seis estaciones dejan de ser cajitas y pasan a ser EDIFICIOS: el
        /// cuarto crece otra vez hacia la IZQUIERDA (ancho 126 -&gt; 218) y
        /// hacia ARRIBA (<see cref="CuartoY1"/> 209 -&gt; 240, alto 42 -&gt; 73).
        /// Los dos límites son EXACTAMENTE los que el contrato autoriza
        /// ("`CuartoX0` puede bajar hasta 140 y `CuartoY1` subir hasta 240");
        /// se usan enteros porque la línea de seis estaciones-edificio no cabe
        /// en menos (ver el reparto medido en "LA LÍNEA DEL TALLER GRANDE"
        /// más abajo: 218 celdas repartidas al detalle, sobran 15).
        ///
        /// <see cref="CuartoX1"/> sigue SIN TOCAR (357) por la misma razón que
        /// en el 26: todo lo anclado al lado derecho (pasillo, boca de la
        /// Tolva, contrafuerte) queda donde estaba y no hay que revalidarlo.
        /// El crecimiento vertical SÍ es nuevo y sí toca revisarlo: el techo
        /// del cuarto pasa a y=240, y la boca de la Tolva vive en
        /// <see cref="ChuteMouthY0"/>..<see cref="ChuteMouthY1"/> = 189..238,
        /// o sea que la franja de solape (por la que va el pasillo) CRECE de
        /// 21 a 50 filas -- el pasillo (`PasilloTolvaY0`=195) sigue dentro sin
        /// tocarlo. El contrafuerte empieza en x=380, 23 celdas a la derecha
        /// de `CuartoX1`: sin solape.
        /// </summary>
        // =================================================================
        // PLAYTEST 33 -- EL TECHO RESPIRA / LA ZONIFICACIÓN DEL INTERIORISTA
        // =================================================================
        // Encargo literal de Cesar sobre el taller del 32: *"estaría increíble
        // que el techo sea MÁS ALTO para que respire mejor el laboratorio...
        // hay que generar DISTANCIAS VISUALES entre máquinas que TRANSFORMAN
        // y las OBSERVACIONALES; asociar más la última (el Ensayo) con la
        // ENTREGA del Maestro... el laboratorio inicial crecería pero TENEMOS
        // ESPACIO"*.
        //
        // LO QUE CAMBIA AQUÍ (y por qué se puede, ahora sí):
        //  · `CuartoY1` 240 -> **262** (+22 filas). El cuarto pasa de 73 a 95
        //    celdas de alto. El mundo mide 288 de alto y la bóveda de
        //    `TallarBoveda` sube hasta 12 filas MÁS (y=274 en el tramo más
        //    alto), así que quedan 13 filas de roca maciza por encima: el
        //    techo nuevo NO toca el borde del mundo. Nada más del plano vive
        //    por encima de y=240 en esta construcción (`BuildDeliveryNiche`
        //    remata en `ChuteMouthY1`=238 y vive en x>=380, fuera del cuarto;
        //    el POZO/Tolva y `AlambiqueBaseY`=210 quedan muy por debajo).
        //  · `CuartoX1` 357 -> **378** (+21 columnas, ancho 218 -> 239). Se
        //    para EXACTAMENTE 1 celda antes de `ChuteWallX0`=380 (el zócalo
        //    del contrafuerte de la Tolva): el cuarto crece hasta besar la
        //    entrega sin invadirla, y el pasillo (`CarvePasilloTolva`, que
        //    arranca en `CuartoX1+1`) se acorta solo. Esas 21 columnas son el
        //    ATRIO del Ensayo del Maestro, que se muda ahí (ver
        //    `EnsayoPlintoX`).
        //  · Las ANCLAS de estación se MUEVEN por primera vez (Cesar lo
        //    autorizó explícitamente esta ronda; hasta el 31 estaba prohibido,
        //    ver el bloque "LO QUE NO SE PUEDE TOCAR" de `AdornarCuarto`).
        //    Solo cambia el VALOR de las constantes ya existentes: los
        //    consumidores (Game/AlkahestGameBootstrap.cs, Net/MaquinaSync.cs,
        //    las réplicas) las leen POR NOMBRE, así que no hay ni una firma
        //    que tocar. El `baseY` de todas SIGUE siendo `CuartoY0+2`=170 --
        //    eso sí está clavado en cada Game/*.cs y no es de esta ronda.
        //
        // EL RECORRIDO (crudo -> transformar -> observar -> examinar ->
        // entregar), que es lo que el reparto de abajo intenta que se SIENTA:
        //   ZONA HÚMEDA 140..171   fuentes+pilas+estante   (de aquí sale todo)
        //   umbral      172..179   donde nace el aprendiz
        //   ZONA DE FUEGO 180..216 crisol + brasero + alambique encima
        //   paso        217..230   terraza tallada
        //   ZONA DE FUERZA 231..261 prensa, con aire propio a los dos lados
        //   EL UMBRAL   262..281   la escalinata larga: aquí se deja de
        //                          fabricar y se empieza a mirar
        //   ALCOBA DE OBSERVACIÓN 282..337  columna + banco de chispa JUNTOS
        //                          (6 celdas entre ellos: son un solo
        //                          instrumento doble), bajo la bóveda más alta
        //                          y sus dos claraboyas frías
        //   atrio       338..347   terraza ceremonial
        //   ENSAYO DEL MAESTRO 348..376  pegado al pasillo de entrega
        //   pared       377..378 -> pasillo 379..392 -> boca de la Tolva
        // Suma: 32+8+37+14+31+20+23+6+27+10+29+2 = 239 = el ancho nuevo.
        //
        // =================================================================
        // PLAYTEST 34 -- "EL MAPA NOS QUEDA CHICO" + EL PLANO CENTRAL
        // =================================================================
        // Cesar, jugando el 33, punto por punto:
        //  (2) *"todo muy apiñado; el tamaño de los objetos es correcto, el
        //      mapa nos queda chico: +1/3 de ALTURA para que respire, y que
        //      el TECHO LO DESCUBRAN AL VOLAR HACIA ARRIBA"*.
        //  (7) *"a lo largo también: al menos +1/4"*.
        //  (9) *"los grifos de agua y limo a una POSICIÓN CENTRAL para que el
        //      tránsito fluya, sugiriendo organización primigenia: máquinas
        //      que TRANSFORMAN a la IZQUIERDA, las de OBSERVACIÓN a la
        //      DERECHA"*.
        //
        // ALTO: 95 -> **127** filas (95 x 4/3 = 126.7). EL CRECIMIENTO VA
        // HACIA ABAJO, NO HACIA ARRIBA, y no por gusto: la bóveda de
        // `TallarBoveda` sube hasta 13 filas sobre `CuartoY1` y el pozo de
        // claraboya otras 6, así que con `CuartoY1`=262 el techo ya remataba
        // en y=281 sobre un mundo de 288 -- por arriba quedaban 4 celdas de
        // margen, no 32. `CuartoY0` baja de 168 a **136** y TODO lo que se
        // deriva de él (el suelo, el `baseY` de las cinco estaciones vía
        // `BaseYDeEstacion`, los caños, el estante, el depósito de anclajes)
        // baja con él SOLO: la maquinaria conserva su cota relativa y lo que
        // gana son 32 celdas de AIRE sobre la cabeza (de ~55 a ~87 celdas
        // libres entre la coronación de las máquinas y el arranque de la
        // bóveda). Eso es literalmente "que respire".
        //   · EL TECHO FUERA DEL ENCUADRE INICIAL (comprobación aritmética,
        //     misma fórmula que `SimRenderer.FitMainCamera`): el encuadre
        //     mide 45 celdas de alto y la cámara ancla al aprendiz con la
        //     zona muerta del 30%, así que desde `AprendizY`=154 se ve hasta
        //     y≈176. El arranque de la bóveda está en 262 y la clave en 275:
        //     **86 celdas por encima del borde superior del encuadre**. Hay
        //     que VOLAR para descubrirlo, que es lo que pidió Cesar.
        //   · La Tolva viaja CON el cuarto (`ChuteMouthY0/Y1` y
        //     `PasilloTolvaY0` bajan las mismas 32 filas): el pasillo y el
        //     pozo quedan INTACTOS en forma y en su relación con el atrio del
        //     Ensayo -- solo se trasladan, no se rediseñan. Todos sus
        //     consumidores (Game/DeliveryChute.cs, Game/DayCycle.cs,
        //     Audio/DirectorDeAudio.cs, Game/WorkshopBackdrop.cs) los leen POR
        //     NOMBRE, así que no hay ni una firma que tocar.
        //
        // ANCHO: 239 -> **299** columnas (239 x 5/4 = 298.75). EL CRECIMIENTO
        // VA HACIA LA IZQUIERDA (`CuartoX0` 140 -> **80**) por la razón
        // simétrica: `CuartoX1`=378 se para 1 celda antes del contrafuerte de
        // la Tolva (`ChuteWallX0`=380) y crecer por ahí se comería la entrega.
        //
        // EL PLANO CENTRAL (punto 9 de Cesar). El recorrido deja de ser una
        // cinta transportadora de izquierda a derecha y pasa a ser una PLANTA
        // con centro: las fuentes -- lo único de lo que sale TODO -- van al
        // medio, y desde ahí se sale a mano izquierda a transformar y a mano
        // derecha a observar. "Organización primigenia": el pozo del pueblo
        // en la plaza y los oficios alrededor.
        //   aire        80..87   ( 8)  muro izquierdo (playtest 34) -- ronda 56:
        //                              PASILLO hacia la MUFLA, ver más abajo.
        //   CRISOL      88..124  (37)  + ALAMBIQUE encima (transformar)
        //   paso       125..142  (18)  terraza tallada
        //   PRENSA     143..173  (31)  (transformar)
        //   UMBRAL     174..197  (24)  <- aquí nace el aprendiz (AprendizX=186)
        //   FUENTES    198..231  (34)  machón+pila del agua, machón+pila del
        //                              limo, el ESTANTE de redomas encima y el
        //                              DEPÓSITO DE ANCLAJES al lado: EL CENTRO
        //   ESCALINATA 232..257  (26)  el corte transformar | observar
        //   COLUMNA    258..280  (23)  (observar, cota +6)
        //   aire       281..288  ( 8)
        //   CHISPA     289..315  (27)  (observar, cota +6)
        //   ATRIO      316..347  (32)  terraza ceremonial
        //   ENSAYO     348..376  (29)  casado con la Tolva (cota +3)
        //   pared      377..378 ( 2) -> pasillo 379..392 -> boca de la Tolva
        // Suma: 8+37+18+31+24+34+26+23+8+27+32+29+2 = 299 = el ancho del 34.
        //
        // (ronda 56, LA VIDA ÚTIL DE LO DESCUBIERTO) EL CUARTO CRECE OTRAS 50
        // CELDAS A LA IZQUIERDA -- mismo movimiento, misma dirección, mismo
        // motivo que el playtest 34 (ganar aire sin reordenar NADA de lo de
        // arriba: `CuartoX1`=378 sigue sin tocarse, la Tolva no se entera).
        // La franja nueva (30..79, 50 celdas) aloja LA MUFLA (ver el bloque
        // de constantes `MuflaX`/`MuflaBaseY` junto a `EnsayoPlintoX` más
        // arriba para las 39 celdas exactas de su huella y el porqué no cabía
        // en ningún hueco de la lista de arriba) más 11+8 celdas de aire a
        // cada lado (muro nuevo|mufla|Crisol). `VetaTurbaX0/X1/CaraX`
        // (derivadas de `CuartoX0`, regla 47 de CLAUDE.md) se mudan CON el
        // muro sin tocar una sola fórmula -- exactamente la garantía que ya
        // dio el playtest 34 al mover este mismo borde.
        //   aire nuevo   30..40  (11)  muro izquierdo NUEVO
        //   MUFLA        41..79  (39)  segunda estación de Crisol (ronda 56,
        //                              tallada solo al completar "obra_mufla")
        //   aire         80..87  ( 8)  el mismo colchón de siempre hasta el Crisol
        // =================================================================
        public const int CuartoX0 = 30;  // (playtest 34) 140 -> 80; (ronda 56) 80 -> 30: +50 celdas para LA MUFLA. Ver el bloque de arriba.
        public const int CuartoX1 = 378; // (playtest 33) 357 -> 378. NO SE MUEVE en el 34: besa el contrafuerte de la Tolva.
        public const int CuartoY0 = 136; // (playtest 34) 168 -> 136: alto 95 -> 127 (+1/3), creciendo HACIA ABAJO. Ver el bloque de arriba.
        public const int CuartoY1 = 262; // (playtest 33) 240 -> 262. NO SE MUEVE en el 34: por arriba ya no cabe la bóveda.

        /// <summary>
        /// (playtest 33) LA COTA POR ZONA: la planta del taller dejó de ser
        /// una sola línea de suelo. Se resuelve POR ANCLA X, no por tipo de
        /// máquina (regla 47: si algún día una estación se muda de zona por
        /// diseño, su cota la decide DÓNDE está el plano, no su nombre).
        /// Zona de fuego y fuerza: planta baja (CuartoY0+2, la de siempre).
        /// Alcoba de observación (x >= 250, tras la escalinata de 6): +6.
        /// Atrio del Ensayo (x >= 340): +3, un peldaño ceremonial.
        /// Mudanza: al soltar en cualquier sitio, la plataforma soberana de
        /// cada estación aplana a la cota del ancla donde cae -- esta función
        /// solo fija las cotas DE FÁBRICA.
        ///
        /// (playtest 34) LAS FRANJAS SE MUDAN A LAS X NUEVAS del plano
        /// central: con las fuentes en el medio (198..231) y la escalinata en
        /// 232..257, la alcoba de observación arranca en la Columna (258) --
        /// el umbral se comprueba a 250, dentro de la escalinata, para que
        /// una estación soltada a media escalera ya se considere "arriba".
        /// El atrio baja de 348 a 340 por lo mismo: la terraza ceremonial
        /// empieza en 316 y el Ensayo en 348; 340 cae dentro del atrio, no en
        /// el aire de la alcoba. Las X son las ÚNICAS que cambian aquí --
        /// las cotas (+3/+6) las validó Cesar en el 33.
        /// </summary>
        public static int BaseYDeEstacion(int anclaX)
        {
            if (anclaX >= 340) return CuartoY0 + 5; // atrio del Ensayo (+3 sobre la planta baja).
            if (anclaX >= 250) return CuartoY0 + 8; // alcoba de observación (+6, sobre la escalinata).
            return CuartoY0 + 2;                    // planta baja de oficios.
        }

        // ---- La cuna --------------------------------------------------------
        // (2ª ronda, "AJUSTE COMPOSICIÓN") Antes a 15 celdas del muro
        // izquierdo (esquina, no centro). Ahora centro-izquierda de la sala
        // (centro geométrico en X = 302.5; CunaCriaturaX = 295 queda a solo
        // 7.5 celdas del centro, claramente "en medio", con sitio de sobra a
        // su derecha para la repisa y el charco sin tocar el muro derecho).
        private const int CunaX0 = 285; // (2ª ronda) antes 263
        private const int CunaWidth = 20;
        private const int CunaHeight = 12;

        /// <summary>Ancla de la CUNA (contrato): celda del SUELO sobre el que se asienta la criatura, centro en X. `CuartoY0 + WallThickness - 1` = la última fila maciza del propio suelo de la U (mismo criterio que `VatPlateRow`/`ChillPlateRow` del plano viejo: "la fila donde vive la placa/piedra es la última de su suelo").</summary>
        public const int CunaCriaturaX = CunaX0 + CunaWidth / 2; // 295 (2ª ronda; antes 273)
        public const int CunaCriaturaY = CuartoY0 + WallThickness - 1; // 170 (2ª ronda; antes 156)

        // ---- El montón de Nutrient, TOCANDO la cuna por dentro ---------------
        // Ver el docblock de la clase para por qué "tocando" no es un capricho:
        // GrowthTick solo mira los 4 vecinos ORTOGONALES de una célula de
        // Vivium, nunca un radio. Vive DENTRO del propio interior de la cuna.
        // LADO IZQUIERDO: pegado a la pared izquierda con 1 celda de margen,
        // no toca el muro, en la esquina opuesta a la repisa (arriba-derecha).
        // Misma fórmula relativa a CunaCriaturaX/Y que la ronda anterior --
        // solo se traduce con la cuna, la geometría interna no cambia.
        private const int NutrienteMoundWidth = 4;
        private const int NutrienteMoundX0 = CunaCriaturaX - 2 - NutrienteMoundWidth; // 289 (2ª ronda; antes 267): termina en 292, TOCANDO (adyacencia ortogonal, sin hueco) el borde izquierdo del disco sembrado (radio 2 -> desde CunaCriaturaX-2 = columna 293).
        private const int NutrienteMoundY0 = CunaCriaturaY + 1; // 171 (2ª ronda; antes 157): justo sobre el suelo de la cuna, mismo nivel que el disco sembrado.
        private const int NutrienteMoundHeight = 4; // 171..174 (2ª ronda; antes 157..160)

        // ---- La repisa del capullo, EN EL MISMO SUELO QUE LA CUNA (tercera
        // ronda: "recomponer como una línea", ver el docblock de la clase) -
        // Antes flotaba 13 celdas por ENCIMA del remate de la cuna
        // (RepisaY0=CunaTopY+2); eso se lee como "otro piso", no "la misma
        // línea". Ahora `RepisaY0 = CuartoY0`, EXACTAMENTE el mismo suelo que
        // usan `BuildCuna` y `PlaceCharco` -- las tres estructuras comparten
        // fila (`CapulloY` == `CunaCriaturaY`, diferencia cero, dentro del
        // "1-2 celdas" que pedía el encargo con margen de sobra). Con la
        // diferencia vertical anulada, TODA la separación del alcance
        // térmico tiene que venir de X: ver `CapulloDistanciaX` para la
        // cuenta con la métrica REAL (Chebyshev, no euclídea).
        // `CunaTopY` se conserva (ya no la usa esta sección, pero sigue
        // viva: ancla `AprendizY` más abajo, "justo sobre el remate de la
        // cuna, mirando dentro").
        private const int CunaTopY = CuartoY0 + CunaHeight - 1; // 179 (2ª ronda; antes 165): última fila de la U (su remate abierto).
        private const int RepisaWidth = 16;
        /// <summary>Mismo suelo que la cuna y el charco (tercera ronda) -- antes CunaTopY+2 (181), flotando 13 celdas por encima.</summary>
        private const int RepisaY0 = CuartoY0; // 168 (tercera ronda; antes 181)
        private const int RepisaHeight = 3;
        /// <summary>
        /// Distancia en X, centro a centro, entre la cuna y el capullo
        /// (tercera ronda: reemplaza `RepisaOffsetX`, que medía un
        /// desplazamiento decorativo sobre una repisa que flotaba en otra
        /// altura). Con `RepisaY0`==suelo de la cuna, la distancia CHEBYSHEV
        /// real que cuenta `Game/Criatura.ApplyCalorTick` (`dist =
        /// Mathf.Max(Abs(dx), Abs(dy))`, NUNCA euclídea pese a lo que decía
        /// el comentario de la ronda anterior) queda dominada casi del todo
        /// por este valor: centro térmico en (`CunaCriaturaX`,
        /// `CunaCriaturaY`+2) = (295,172) -- ver `AlturaCuerpoCeldas` en
        /// Criatura.cs --, capullo en (307,170) -&gt; Chebyshev =
        /// max(|307-295|, |170-172|) = max(12,2) = 12. DENTRO de 10-14, y es
        /// literalmente una de las tres distancias (10/12/14) que
        /// `PerfilCalorPct` verifica explícitamente en Criatura.cs.
        /// </summary>
        private const int CapulloDistanciaX = 12;
        private const int RepisaX0 = CunaCriaturaX + CapulloDistanciaX - RepisaWidth / 2; // 299 (tercera ronda; antes 292): PISA a propósito las columnas 299-304 de la cuna -- ver el docblock de la clase, "LA REPISA SOLAPA EL SUELO DE LA CUNA A PROPÓSITO".

        /// <summary>
        /// Ancla de la REPISA (contrato): celda del suelo/repisa sobre la que
        /// se apoya el capullo. Distancia CHEBYSHEV real (tercera ronda: ver
        /// `CapulloDistanciaX` arriba para la cuenta completa y por qué
        /// Chebyshev y no euclídea) al centro térmico del Rescoldo
        /// (`CunaCriaturaX`, `CunaCriaturaY`+2) = 12 -- dentro de los 10-14
        /// de alcance verificados por el otro encargo, y ahora EN EL MISMO
        /// PLANO que la cuna (`CapulloY` == `CunaCriaturaY`, diferencia
        /// cero) en vez de flotando 13 celdas por encima: la separación es
        /// horizontal, no vertical, que es justo la lectura "en línea" que
        /// pedía Cesar.
        /// </summary>
        public const int CapulloX = RepisaX0 + RepisaWidth / 2; // 307 (tercera ronda; antes 300)
        public const int CapulloY = RepisaY0 + RepisaHeight - 1; // 170 (tercera ronda; antes 183) -- == CunaCriaturaY.

        // ---- El charco de Water, a la IZQUIERDA de la cuna (tercera ronda:
        // "recomponer como una línea", ver el docblock de la clase --
        // cuenco -> criatura -> capullo, leído de izquierda a derecha). Antes
        // vivía a la DERECHA (X0=314, el mismo lado que ahora ocupa la
        // repisa/capullo) -- con las dos cosas a la derecha, la sala se leía
        // con todo amontonado en un lado y nada en el otro, lo contrario de
        // un escaparate. Misma cubeta en miniatura (DrawUShape sobre
        // `CuartoY0`, YA el mismo suelo que la cuna y la repisa -- sin
        // cambios en esa parte) solo trasladada al otro lado, con el mismo
        // criterio de hueco pequeño (5 celdas hasta la pared izquierda de la
        // cuna) que ya usaba la ronda anterior para "cerca, mismo primer
        // plano".
        // (playtest 22) EL CHARCO SE MUEVE OTRA VEZ, Y AHORA POR UNA RAZÓN
        // MECÁNICA, NO DE COMPOSICIÓN: pasa de 267 a 250 para quedar JUSTO
        // DEBAJO DE LAS BOQUILLAS de los dos caños del muro izquierdo (ver
        // `CanoMontajeX`). La cubeta deja de ser un charco decorativo y pasa a
        // ser la PILA DE RECOGIDA: abres el grifo y el agua cae dentro, en vez
        // de derramarse por el suelo de la sala. Comprobado: la boquilla cae en
        // x = CanoMontajeX + Dispenser.SpoutOffsetCells = 253, y el interior de
        // la cubeta es 253..260 (X0+WallThickness .. X0+Width-WallThickness-1),
        // así que el chorro entra por la primera columna útil.
        //
        // (playtest 26, RETIRADO -- ver PILAS DE RECOGIDA más abajo) El
        // contrato §2 pide DOS pilas nombradas, una por caño ("cada uno
        // vierte SOBRE SU PILA"), en vez de una única cubeta compartida.
        // `PlaceCharco` (más abajo en el archivo) se CONSERVA sin llamantes
        // (regla 15 de CLAUDE.md: comentar el porqué, no borrar) -- las tres
        // constantes de aquí abajo siguen vivas porque ese método las sigue
        // usando, aunque `BuildCuartoIntimo` ya no lo invoque.
        private const int CharcoX0 = 250; // (playtest 22; antes 267, y antes 314)
        private const int CharcoWidth = 14;
        private const int CharcoHeight = 7;
        /// <summary>Filas de agua dentro de la cubeta -- no llena hasta el borde a propósito: se lee como charco, no como aljibe rebosante.</summary>
        private const int CharcoAguaAltura = 2;

        // =================================================================
        // PLAYTEST 26 (CONTRATO_LEGIBILIDAD.md §2) — LAS DOS PILAS DE
        // RECOGIDA, una por caño ("el chorro deja de perderse por el suelo")
        // =================================================================
        // Contrato: "pilas de recogida talladas bajo cada caño, 6 de ancho, 3
        // de hondo, marco de piedra". Interpretadas como OUTER (mismo
        // criterio que `BasinWidth`/`VatWidth` en este archivo: el X0/ancho
        // que se nombra es siempre el footprint EXTERIOR, con
        // `PilaMuroGrosor`=1 -- NO el `WallThickness`=3 de las cubas grandes,
        // que no cabría en 6 celdas (6-2*3=0, imposible)). Mismo idioma que
        // las cubetas de Crisol/Prensa/BancoChispa/Ensayo: "caldero
        // compacto", no cuba de taller.
        //
        // BASEY = `CuartoY0+2` (170), MISMA convención que Crisol/Prensa/
        // BancoChispa/Ensayo (`baseY`): la fila `CuartoY0+2` YA es la última
        // fila maciza del suelo general (ver BuildCuartoFloor), así que el
        // interior de la pila (`baseY+1` en adelante) NUNCA colisiona con la
        // losa -- si el suelo de la pila se pusiera en CuartoY0 a secas, las
        // dos filas de "interior" caerían DENTRO de la losa ya sólida y la
        // pila nacería tapiada por dentro sin ningún error visible.
        //
        // DECISIÓN (fuera del contrato, documentada): AMBAS PILAS COMPARTEN
        // COLUMNA DE CAÍDA. `Dispenser.SpoutOffsetCells`=5 es una constante
        // ÚNICA (no por-instancia, archivo fuera de mi alcance) y los dos
        // caños montan en la MISMA `CanoMontajeX` (solo cambian de fila Y,
        // "se separan en vertical como hoy" -- contrato §2, textual): el
        // chorro de los DOS caños cae siempre por la columna
        // x=CanoMontajeX+5=237, dentro de PilaAgua, nunca de PilaLimo. No hay
        // forma de resolverlo sin tocar Dispenser.cs/AlkahestGameBootstrap.cs
        // (fuera de mi alcance, "NADA MÁS" del contrato). Se tallan las DOS
        // pilas FRAMED de todos modos -- (a) cumple la lectura visual pedida
        // ("aquí cada material tiene su cubeta", la duda original de Cesar
        // era justo esta), y (b) un líquido que rebosa PilaAgua fluye por el
        // suelo hacia PilaLimo en vez de perderse (SimStepper, Liquid de
        // verdad) -- el objetivo LITERAL de Cesar ("el chorro deja de
        // perderse por el suelo") se cumple igual.
        // =================================================================
        // PLAYTEST 27 — LA ESTACIÓN DE FUENTES (mandato 1, 2 y 7 del
        // CONTRATO_TALLER_GRANDE). Las pilas de 6x3 del playtest 26 tenían
        // interior 4x2 = 8 CELDAS: la ración de un caño son 45, así que
        // desbordaban SIEMPRE, desde el primer chorro. Ahora cada pila es una
        // pila de taller de verdad: 14x9 exterior, muro de 2, interior 10x7 =
        // **70 celdas** -- una ración entera (45) cabe con holgura y aún se ve
        // el nivel subir dentro.
        //
        // LOS DOS CHORROS SE SEPARAN CON GEOMETRÍA, NO ESTIRANDO EL CAÑO
        // (mandato 7, textual de Cesar: "no estires el tamaño del caño de
        // Limo, vuélvelo a su dimensión normal"). El playtest 26 los separó
        // dándole al caño de limo un voladizo de 12 celdas -- el sprite
        // procedural se estiraba y quedaba deforme. La solución correcta es
        // MÉNSULA DE PIEDRA: el caño de agua monta en la pared izquierda del
        // cuarto y el de limo monta, más alto, en un machón de piedra propio
        // (<see cref="MensulaLimoX0"/>) plantado entre las dos pilas. Los dos
        // caños conservan el voladizo estándar de 5 celdas (`Dispenser.
        // SpoutOffsetCellsDefault`, sin tocar) y cada chorro cae dentro de SU
        // pila porque las BOCAS están a distinta X, que es como se resuelve
        // esto en un taller de verdad.
        //
        // Cuentas (comprobadas contra Game/Dispenser.cs, no a ojo):
        //  · agua: monta en x=CanoAguaX=140 -> boquilla en 145; interior de
        //    PilaAgua = 143..152. Cae en la 3ª columna útil. ✔
        //  · limo: monta en x=CanoLimoX=157 (cara derecha de la ménsula) ->
        //    boquilla en 162; interior de PilaLimo = 160..169. Cae en la 3ª
        //    columna útil, simétrico al de agua. ✔
        //
        // =================================================================
        // (playtest 34) LA ESTACIÓN DE FUENTES SE VA AL CENTRO -- y por eso
        // gana un SEGUNDO machón, el del agua
        // =================================================================
        // Cesar: *"los grifos de agua y limo a una POSICIÓN CENTRAL para que
        // el tránsito fluya, sugiriendo organización primigenia"*.
        //
        // EL PROBLEMA QUE ESO CREA, Y SU SOLUCIÓN: hasta el 33 el caño de
        // agua montaba en `CanoAguaX = CuartoX0` -- o sea, atornillado a la
        // PARED IZQUIERDA de la sala. Una fuente en el centro no tiene pared
        // donde atornillarse: con la constante vieja, el caño del agua se
        // habría quedado a 118 celdas de su pila, escupiendo al suelo desde
        // el muro (exactamente el fallo que la regla 47 de CLAUDE.md describe
        // -- reutilizar una constante de posición porque el nombre encaja).
        // La respuesta es la MISMA que el playtest 27 dio para el limo, y por
        // simetría: un MACHÓN DE PIEDRA propio. La estación pasa a leerse
        // como una isla de cuatro piezas, machón-pila-machón-pila, que es lo
        // que la vuelve un objeto exento en medio de la sala y no un mueble
        // arrimado a un muro:
        //   machón agua 198..200 | PILA AGUA 201..214 | machón limo 215..217 |
        //   PILA LIMO 218..231
        // Cuentas comprobadas contra Game/Dispenser.cs (voladizo estándar de
        // 5 celdas, `SpoutOffsetCellsDefault`, sin tocar):
        //  · agua: monta en x=`CanoAguaX`=200 (cara derecha de su machón) ->
        //    boquilla en 205; interior de PilaAgua = 203..212. ✔
        //  · limo: monta en x=`CanoLimoX`=217 -> boquilla en 222; interior de
        //    PilaLimo = 220..229. ✔
        // El ESTANTE de redomas y el DEPÓSITO DE ANCLAJES viajan solos: los
        // dos derivan de `PilaAguaX0`/`PilaLimoX0` (ver `EstanteX0`/`EstanteX1`
        // y Game/Anclaje.cs::SpawnDeposito), así que el centro del taller
        // queda además siendo el sitio donde se guardan las cosas -- que es
        // justo la "organización primigenia" que pedía el encargo.
        // =================================================================
        private const int PilaMuroGrosor = 2;  // 1 -> 2 (playtest 27): un muro de 1 celda en una pila de 14 de ancho se lee como una raya, no como obra.
        public const int PilaAnchoOuter = 14;  // 6 -> 14.
        public const int PilaHondoOuter = 9;   // 3 -> 9. Interior resultante: 10 x 7 = 70 celdas (la ración son 45).
        /// <summary>Pila del AGUA. (playtest 34) 141 -&gt; 201: interior 203..212 -- la columna de caída real (205) entra por la tercera columna útil.</summary>
        public const int PilaAguaX0 = 201;
        /// <summary>Pila del LIMO. (playtest 34) 158 -&gt; 218: interior 220..229 -- la boquilla del limo cae en 222.</summary>
        public const int PilaLimoX0 = 218;

        /// <summary>Fila de suelo EXTERIOR compartida por las dos pilas -- mismo baseY que Crisol/Prensa/BancoChispa/Ensayo (una fila encima del suelo general, ver BuildCuartoFloor). Antes vivía como variable local dentro de BuildPilasFuentes; ahora es pública porque <see cref="PilaPlanes"/> (y cualquiera que lea el plano) la necesita.</summary>
        public const int PilaBaseY = CuartoY0 + 2;

        /// <summary>
        /// (fix Cesar playtest 34, "GRIFOS Y PILAS SE MUDAN POR SEPARADO")
        /// Una pila ya troceada: la "U" de piedra completa (paredes+suelo),
        /// footprint EXTERIOR -- ver Game/Pila.cs, el reemplazo del tallado
        /// directo que hacía <see cref="BuildPilasFuentes"/> hasta esta
        /// ronda (mismo relevo que <see cref="BaldaPlanes"/> le hizo a las
        /// viejas <c>Repisas</c> en el playtest 33: la mampostería estática
        /// pasa a ser un objeto <c>IMovible</c>).
        /// </summary>
        public struct PilaPlan { public int X0, Y0, Ancho, Hondo, Muro; }

        /// <summary>Las dos pilas de la estación de fuentes -- MISMAS medidas que siempre (PilaAnchoOuter/PilaHondoOuter/PilaMuroGrosor), ahora expuestas como catálogo para que Game/Pila.cs las talle en el génesis (<see cref="Pila.TallarEnPlano"/>, llamado desde <see cref="BuildPilasFuentes"/>) y las vuelva a instanciar en caliente (<see cref="Pila.SpawnTodas"/>, llamado desde Game/Mudanza.cs).</summary>
        public static readonly PilaPlan[] PilaPlanes =
        {
            new PilaPlan { X0 = PilaAguaX0, Y0 = PilaBaseY, Ancho = PilaAnchoOuter, Hondo = PilaHondoOuter, Muro = PilaMuroGrosor },
            new PilaPlan { X0 = PilaLimoX0, Y0 = PilaBaseY, Ancho = PilaAnchoOuter, Hondo = PilaHondoOuter, Muro = PilaMuroGrosor },
        };

        /// <summary>Machón de piedra ENTRE las dos pilas: es lo que sostiene el caño de limo más alto y a otra X (ver el bloque de arriba). Tres celdas de ancho, del suelo al arranque del caño.</summary>
        public const int MensulaLimoX0 = 215;
        public const int MensulaLimoX1 = 217;

        /// <summary>(playtest 34) Machón GEMELO del agua, a la izquierda de su pila -- lo que sustituye a la pared del cuarto ahora que la estación de fuentes vive exenta en el centro. Ver el bloque de arriba.</summary>
        public const int MensulaAguaX0 = 198;
        public const int MensulaAguaX1 = 200;
        /// <summary>Remate del machón del agua: dos celdas sobre la fila de su caño, mismo criterio que <see cref="MensulaLimoTopY"/>.</summary>
        public const int MensulaAguaTopY = CanoAguaY + 2;
        /// <summary>Remate del machón: dos celdas por encima de la fila del caño de limo, para que el aparato se vea atornillado a piedra y no flotando.</summary>
        public const int MensulaLimoTopY = CanoLimoY + 2;

        // =================================================================
        // LOS DOS CAÑOS BÁSICOS (playtest 22)
        // =================================================================
        // Cesar, tras jugar el pivot: *"quizás necesite los caños más básicos
        // al inicio; hay un charquito de agua pero si por lo que sea lo pierdo
        // ya no hay mucho más que hacer, y así evitamos dejar cositas en el
        // suelo que se pueden perder"*.
        //
        // Su razón es la correcta y vale como criterio general del proyecto:
        // **en un juego que te pide experimentar, un recurso que puedes perder
        // para siempre es una trampa.** Un charco de 16 celdas de agua se
        // evapora, se ensucia o se lo bebe la criatura, y a partir de ahí la
        // sala es un cuarto bonito donde no se puede hacer nada. Una fuente
        // INFINITA no es una comodidad: es lo que permite equivocarse, que es
        // de lo que va el juego entero.
        //
        // SOLO DOS, y a coste 0 de Favor: AGUA (el disolvente de todo) y
        // NUTRIENTE (la comida de la criatura, o sea lo que la mantiene viva y
        // caliente). Arena, aceite y azoth se quedan enterrados con el taller
        // clásico -- aparecen al excavar, y esa es su recompensa. Dos caños es
        // "lo básico" literal; cinco sería volver al banco de grifos que este
        // pivot se llevó por delante.
        //
        // MONTADOS EN EL MURO IZQUIERDO, en columna, como en la referencia de
        // arte que mandó Cesar (allí hay una columna de grifos etiquetados a la
        // izquierda del cuadro). `CuartoX0` es la PRIMERA COLUMNA DE AIRE de la
        // sala (BuildCuartoIntimo excava CuartoX0..X1 con DrawSolidRect/Empty),
        // así que montar ahí deja el aparato pegado a la roca del muro
        // (x=`CuartoX0`-1=231, playtest 26 -- antes 247 con `CuartoX0`=248),
        // no flotando.
        //
        // LECTURA DE IZQUIERDA A DERECHA que queda en la sala (playtest 26,
        // reemplaza "criatura -> capullo", aparcados desde el playtest 25):
        // **caños + pilas -> CRISOL -> PRENSA -> COLUMNA -> CHISPA -> ENSAYO
        // -> pasillo -> Tolva** (contrato §2, "la línea del taller").
        /// <summary>
        /// (playtest 27) LOS DOS CAÑOS DEJAN DE COMPARTIR COLUMNA DE MONTAJE
        /// -- ver el bloque de LA ESTACIÓN DE FUENTES más arriba. Esta
        /// constante se conserva con su nombre histórico y pasa a ser el
        /// ALIAS del caño de agua (el que sí monta en la pared del cuarto),
        /// para no romper a nadie que la lea por su nombre viejo.
        /// </summary>
        public const int CanoMontajeX = CanoAguaX;
        /// <summary>(playtest 34) Columna de montaje del caño de AGUA: la cara DERECHA de su machón (antes `CuartoX0`, la pared del cuarto -- ver el bloque "LA ESTACIÓN DE FUENTES SE VA AL CENTRO").</summary>
        public const int CanoAguaX = MensulaAguaX1; // 200
        /// <summary>Columna de montaje del caño de LIMO: la cara DERECHA del machón de piedra, para que su chorro caiga en la pila del limo y no en la del agua.</summary>
        public const int CanoLimoX = MensulaLimoX1; // 157
        /// <summary>Fila del caño de AGUA (190). La boquilla queda 2 celdas más abajo (Dispenser.SpoutDropCells) y el labio de su pila está en 178: **10 celdas de caída visible**. Que el chorro se vea caer entero es media explicación de qué hace el aparato.</summary>
        public const int CanoAguaY = CuartoY0 + 22; // 190
        /// <summary>Fila del caño de NUTRIENTE/LIMO (196): 6 celdas MÁS ALTO que el de agua y 17 a su derecha (sobre su ménsula), con 16 celdas de caída visible hasta su pila. Las dos chapas de rótulo no se pisan y los dos chorros se leen como dos fuentes distintas, no como un caño con dos bocas.</summary>
        public const int CanoNutrienteY = CuartoY0 + 28; // 196
        /// <summary>
        /// LO QUE PERSISTE (playtest 25, contrato §4.5): "el caño de NUTRIENTE
        /// pasa a ser el caño de LIMO: misma boca, otro material". Alias
        /// documentado en vez de renombrar `CanoNutrienteY` -- el nombre viejo
        /// sigue describiendo la POSICIÓN física (encima del caño de agua,
        /// misma columna de montaje), y el alias nuevo describe qué sale por
        /// ahí ahora (`AlkahestGameBootstrap.SpawnCanoBasico` pasa
        /// `MaterialId.Limo`, no `MaterialId.Nutrient` -- ver ese archivo). Es
        /// EXACTAMENTE la misma celda: no hay dos caños, hay un caño con dos
        /// nombres para dos épocas del proyecto.
        /// </summary>
        public const int CanoLimoY = CanoNutrienteY;

        // ---- Estante de redomas (playtest 30, REUBICADO playtest 34) -----
        // "LA ALQUIMIA VISIBLE" (encargo de Cesar) reactiva Game/StorageRack.cs
        // en el cuarto íntimo. DECISIÓN ORIGINAL (playtest 30, YA NO VIGENTE,
        // conservada para que se entienda la cronología): la MISMA huella en
        // X que las dos pilas de las fuentes (`EstanteX0 = PilaAguaX0`),
        // "cerca de las fuentes". Válido cuando la estación de fuentes vivía
        // en el muro izquierdo -- (fix Cesar playtest 34) LA ESTACIÓN DE
        // FUENTES SE MUDÓ AL CENTRO (ver el bloque grande junto a
        // `PilaAguaX0`) y esta constante SE MUDÓ CON ELLA sin que nadie lo
        // decidiera, por la MISMA trampa que describe la regla 47 de
        // CLAUDE.md ("no reutilizar una constante de posición solo porque el
        // nombre encaja"): `EstanteX0..X1` pasaron a coincidir EXACTAMENTE
        // con `PilaAguaX0..PilaLimoX0+13` (201..231), o sea las redomas
        // habrían aparecido flotando ENCIMA de las dos pilas de agua/limo
        // -- ni "cerca de las fuentes" ni visible, la causa real del bug
        // "en MULTI/un jugador no aparecen las redomas" que reportó Cesar
        // para esta ronda (además del gap de spawn de la hipótesis A).
        //
        // SITIO NUEVO (captura de Cesar): zona IZQUIERDA, sobre la zona del
        // CRISOL (`CrisolX`=102, huella real 88..124 -- ver el docblock junto
        // a esa constante), a la ALTURA DE LA GALERÍA DE BALDAS de esa misma
        // zona (`_galeriasOriginales[0]/[1]`, Y=`CuartoY0+42`=178, las "alas"
        // que flanquean el plinto del Alambique) -- no a ras de suelo. El
        // ANCHO completo (88..124, 37 celdas) SÍ cruza por encima del propio
        // Alambique (huella real X 96..108, ver `Alambique.Calcular`), así
        // que la altura no puede ser la de esas baldas (Y=178, la MISMA fila
        // que el techo del plinto): tiene que quedar POR ENCIMA del domo de
        // vidrio del Alambique entero. `EstanteBaseY` se DERIVA de la
        // geometría real del Alambique (regla 39/47: leer medidas, no
        // adivinarlas) -- `AlambiqueBaseY` + `MatrazAlto` + `DomoAlto` + 1
        // (techo) es EXACTAMENTE `Alambique.Calcular(...).OutY1`, el borde
        // superior real de su domo (verificado a mano: 178+5+9+1=193) -- más
        // <see cref="EstanteMargenSobreAlambique"/> de aire para que el
        // listón de las redomas no toque el cristal. Game/StorageRack.cs
        // sigue siendo pura vista (no talla mampostería, deriva sus medidas
        // del ancho real -- regla 39), así que no hace falta registrar obra
        // ni tallar plinto aquí -- puede flotar sobre el Alambique sin que
        // el cincel ni la física tengan nada que decir al respecto.
        //
        // EL DEPÓSITO DE ANCLAJES NO QUEDA HUÉRFANO: `Game/Anclaje.cs::
        // SpawnDeposito` ya deriva su sitio de `EstanteX0`/`EstanteBaseY`
        // (nunca de un literal propio, ver ese archivo), así que se muda
        // solo con el estante -- sigue naciendo `DepositoDesnivel`=14 celdas
        // por encima de las redomas, en el mismo aire libre de la zona del
        // crisol, sin tocar ninguna mampostería nueva.
        private const int EstanteMargenSobreAlambique = 5; // celdas de aire entre el techo del domo del Alambique y el listón del estante -- puramente estético, para que no se toquen visualmente.
        public const int EstanteX0 = CrisolX - 14; // 88 -- borde izquierdo de la huella real del Crisol (ver el docblock junto a CrisolX).
        public const int EstanteX1 = CrisolX + 22; // 124 -- borde derecho de esa misma huella.
        public const int EstanteBaseY = AlambiqueBaseY + Alambique.MatrazAlto + Alambique.DomoAlto + 1 + EstanteMargenSobreAlambique; // 193 (techo real del domo) + 5 = 198.

        // =================================================================
        // (ronda 56, LA VIDA ÚTIL DE LO DESCUBIERTO, CONTRATO_RONDA56.md §2a)
        // LA ALACENA -- el mismo mueble (Game/StorageRack.cs), revelado en
        // Semilla Cero cuando `SemillaCero.FaseVidaUtil` sube a `true`.
        //
        // POR QUÉ NO SON `EstanteX0/EstanteX1/EstanteBaseY` TAL CUAL (el
        // contrato pedía literalmente "leer EstanteX0/BaseY reales del
        // plano" -- esto es la verificación de esa lectura, regla 39/47 de
        // CLAUDE.md: medir contra el plano REAL, no confiar en que el nombre
        // de la constante siga describiendo un hueco vacío): en Semilla
        // Cero, `EstanteBaseY`=198 YA NO es aire libre en todo su ancho --
        // el BUZÓN DEL MAESTRO (`BuzonX0..BuzonX1`=97..106, tallado SOLO en
        // Semilla Cero, ver `BuildBuzonMaestro`) usa EXPLÍCITAMENTE
        // `BuzonBaseY = EstanteBaseY` (misma cota) y ocupa justo el TERCIO
        // CENTRAL de `EstanteX0..EstanteX1` (88..124) con piedra maciza
        // hasta `BuzonBlockTopY`=209 -- por encima de donde llegarían 2 de
        // las 6 redomas (con `NumRedomas`=6 sobre el ancho 88..124, las
        // redomas #2 y #3 caen en 94.6..99.9 y 100.75..106.1, DENTRO de la
        // piedra del Buzón). El CAÓTICO nunca talla el Buzón (gate
        // `ModoSemillaCero` en `BuildBuzonMaestro`), así que
        // `EstanteX0/X1/BaseY` SIGUEN SIENDO CORRECTOS para el estante
        // clásico -- sin cambios, contrato §2a "en el CAÓTICO no cambia
        // nada" -- el problema es SOLO para el mueble que se revela EN
        // Semilla Cero.
        //
        // EL SITIO REAL: a la derecha del Buzón, MISMA `EstanteBaseY` (ya
        // validada libre de la maquinaria de abajo -- el domo del Alambique
        // termina en 193, el depósito de anclajes empieza en 212, ver el
        // bloque de arriba) pero desplazado en X para no compartir columna
        // con la piedra del Buzón. `AlacenaMargenBuzon`/`AlacenaMargenPrensa`
        // son AIRE de sobra (ninguno de los dos toca nada, ver los números
        // exactos en los comentarios de cada constante) -- no hace falta el
        // mínimo posible, solo que no colisione.
        // =================================================================
        private const int AlacenaMargenBuzon = 3;  // aire tras la jamba derecha del Buzón (BuzonX1=106) antes de la primera redoma.
        private const int AlacenaMargenPrensa = 2; // aire antes de la jamba izquierda del pórtico de la Prensa.
        public const int AlacenaX0 = BuzonX1 + AlacenaMargenBuzon; // 109.
        /// <summary>
        /// 140 -- derivado de la huella REAL de la Prensa (`Prensa.LechoAncho`/
        /// `MuroGrosor`/`LechoAJamba`/`JambaAncho`, constantes públicas de
        /// Game/Prensa.cs, regla 39: nunca copiar el número "143..173" de un
        /// comentario, leerlo de las medidas reales) menos 1 celda de la
        /// jamba izquierda de la Prensa (`OutX0`=143) menos
        /// <see cref="AlacenaMargenPrensa"/>. Ancho resultante 109..140 = 32
        /// celdas (vs. 37 del estante clásico) -- de sobra para 6 redomas
        /// más pequeñas (ver Game/StorageRack.cs, que deriva su tamaño del
        /// ancho real que reciba, sin ningún cambio de código).
        /// </summary>
        public const int AlacenaX1 = PrensaX - Prensa.LechoAncho / 2 - Prensa.MuroGrosor - Prensa.LechoAJamba - Prensa.JambaAncho - 1 - AlacenaMargenPrensa; // 140.
        /// <summary>Misma cota que el estante clásico (<see cref="EstanteBaseY"/>) -- ya validada libre de todo lo de abajo (domo del Alambique, depósito de anclajes), ver el bloque de arriba.</summary>
        public const int AlacenaBaseY = EstanteBaseY;

        // =================================================================
        // (CONTRATO_RONDA54.md, playtest 54) EL BUZÓN DEL MAESTRO -- SOLO
        // SEMILLA CERO. Reemplaza a la TOLVA CERCANA del playtest 50
        // (<see cref="BuildTolvaCercana"/>, constantes `TolvaCercana*` más
        // abajo, RETIRADA de la llamada de <see cref="BuildCuartoIntimo"/>
        // pero conservada intacta -- regla 15 de CLAUDE.md): Cesar, sobre
        // esa versión, "atravesarla con esa flecha amarilla y letrero
        // grande... en medio del camino, alejándome de la única cosa que
        // necesito investigar al inicio [el Crisol]... algo que
        // GRÁFICAMENTE comunique que tengo que dejar pedidos, no una flecha
        // amarilla y un letrerote" -- y sugirió el sitio: "de repente en
        // algún lugar de arriba y mucho menos llamativo... una alcoba
        // pequeña en el muro sobre la zona del crisol, o en la repisa
        // superior izquierda cerca del alambique".
        //
        // EL SITIO, LEÍDO DEL PLANO (regla 39/47 de CLAUDE.md): la Tolva
        // vieja vivía en el HUECO DE SUELO x129..138 (el paso caminable
        // Crisol->Prensa) -- exactamente lo que Cesar señaló como el
        // problema. El sitio nuevo es <see cref="EstanteBaseY"/>=198 (la
        // cota YA VALIDADA "5 celdas sobre el domo real del Alambique",
        // <see cref="EstanteMargenSobreAlambique"/>, ver el párrafo de
        // arriba) -- que en Semilla Cero queda VACANTE porque el estante de
        // redomas no se hace visible ahí (ver
        // `AlkahestGameBootstrap.SpawnStorageRack`, gate nuevo de esta
        // ronda: el componente sigue vivo por Net/MaquinaSync.cs, pero no
        // construye ni un solo sprite). El Buzón ocupa ese mismo aire en
        // vez de dejarlo huérfano: ELEVADO (60 celdas sobre el suelo del
        // Crisol, 138), LATERAL (fuera del paso Crisol<->Prensa, que vuelve
        // a ser suelo limpio) y "a segundos de vuelo" (mismo eje X que el
        // Crisol/Alambique, <see cref="BuzonX0"/> derivado de
        // <see cref="CrisolX"/>, no de un literal).
        //
        // A diferencia de <see cref="BuildTolvaCercana"/> (que reutilizaba
        // el suelo YA sólido del cuarto, `BuildCuartoFloor`), aquí NO hay
        // piedra por defecto -- a esta altura el cuarto está hueco
        // (`ExcavateCuarto`) -- así que <see cref="BuildBuzonMaestro"/>
        // maciza el bloque entero (mismo criterio que
        // <see cref="BuildAlcobaFria"/>: sólido primero, ranura vacía
        // después) antes de vaciar la ranura.
        //
        // GEOMETRÍA: MISMO ancho funcional que la Tolva que sustituye
        // (<see cref="BuzonBocaAncho"/>=6, igual que la vieja
        // `TolvaCercanaBocaAncho`) pero MUCHO más baja
        // (<see cref="BuzonBocaAlto"/>=8 contra 20): ya no es un pozo de
        // suelo a techo, es una hornacina de pared -- "ranura horizontal
        // oscura" (contrato, textual), no un tiro vertical. Tres filas
        // libres por encima de la ranura para el relieve de pergamino/sello
        // (puramente decorativo, un sprite sobre la piedra -- Game/
        // DeliveryChute.cs lo pinta, este archivo no talla nada extra ahí).
        //
        // Registrada como Obra (anticincel) y DELIBERADAMENTE fuera de
        // `RegistrarObraSemillaCero` -- el Buzón NUNCA se tapia, mismo
        // criterio que la Tolva vieja: tiene que estar descubrible desde el
        // minuto 0 (late con el primer pedido, ver Game/DeliveryChute.cs).
        //
        // SIN COLISIÓN VERIFICADA (regla 39, medido contra constantes
        // reales, no a ojo): el depósito de 6 anclajes de
        // Game/Anclaje.cs::SpawnDeposito nace en `EstanteBaseY +
        // DepositoDesnivel` = 198+14 = 212 -- <see cref="BuzonBlockTopY"/>
        // (209) deja 2 filas de aire libres (210-211) antes de tocarlo. Por
        // el oeste, la galería de baldas "ala izquierda" (`_galeriasOriginales`
        // tras la reducción de esta ronda, X0=84..93, Y=178) queda muy por
        // debajo (178<198) y 4 columnas a la izquierda de `BuzonX0`=97: sin
        // solape en ninguno de los dos ejes.
        // =================================================================
        /// <summary>Ancho de cada jamba de piedra que flanquea la ranura del Buzón.</summary>
        public const int BuzonMuro = 2;
        /// <summary>Ancho de la ranura interior -- MISMO ancho funcional que la Tolva Cercana que sustituye (TolvaCercanaBocaAncho=6).</summary>
        public const int BuzonBocaAncho = 6;
        /// <summary>Alto de la ranura interior, en celdas -- una hornacina de pared, no un pozo de suelo a techo (contraste con TolvaCercanaAlto=20). 3 filas de sillar (Game/DeliveryChute.cs::ChuteSillRows) + margen de caída visible de sobra para lo que vierte un pedido de Semilla Cero.</summary>
        public const int BuzonBocaAlto = 8;
        /// <summary>Jamba izquierda, borde exterior del bloque -- centrado sobre el Crisol/Alambique (misma X, CrisolX=102): 102-5=97.</summary>
        public const int BuzonX0 = CrisolX - 5;
        /// <summary>Jamba derecha, borde exterior. 97 + 2*2 + 6 - 1 = 106.</summary>
        public const int BuzonX1 = BuzonX0 + BuzonMuro * 2 + BuzonBocaAncho - 1;
        /// <summary>Ranura, borde izquierdo (dentro de la jamba). 99.</summary>
        public const int BuzonMouthX0 = BuzonX0 + BuzonMuro;
        /// <summary>Ranura, borde derecho (dentro de la jamba). 104.</summary>
        public const int BuzonMouthX1 = BuzonX1 - BuzonMuro;
        /// <summary>Última fila maciza de la base del bloque -- reutiliza EXACTAMENTE EstanteBaseY (198, "5 celdas sobre el domo del Alambique"), vacante en Semilla Cero porque el estante no se hace visible ahí.</summary>
        public const int BuzonBaseY = EstanteBaseY;
        /// <summary>Ranura, primera fila de aire (justo sobre la base sólida). 199.</summary>
        public const int BuzonMouthY0 = BuzonBaseY + 1;
        /// <summary>Ranura, última fila de aire (el labio). 206.</summary>
        public const int BuzonMouthY1 = BuzonMouthY0 + BuzonBocaAlto - 1;
        /// <summary>Techo del bloque de piedra -- 3 filas por encima de la ranura para el relieve de pergamino/sello (decorativo, Game/DeliveryChute.cs lo pinta encima de esta piedra).</summary>
        public const int BuzonBlockTopY = BuzonMouthY1 + 3;

        /// <summary>
        /// Talla el Buzón (ver el bloque de constantes de arriba para el
        /// porqué exacto del sitio): maciza el bloque entero (a diferencia
        /// de <see cref="BuildTolvaCercana"/>, aquí NO hay suelo previo del
        /// que partir) y vacía la ranura interior. SOLO se llama desde
        /// <see cref="BuildCuartoIntimo"/> bajo el gate de
        /// <see cref="AlkahestGameBootstrap.ModoSemillaCero"/>.
        /// </summary>
        private static void BuildBuzonMaestro(CellGrid grid)
        {
            DrawSolidRect(grid, BuzonX0, BuzonBaseY, BuzonX1 - BuzonX0 + 1, BuzonBlockTopY - BuzonBaseY + 1, MaterialId.Stone);

            DrawSolidRect(grid, BuzonMouthX0, BuzonMouthY0, BuzonMouthX1 - BuzonMouthX0 + 1, BuzonMouthY1 - BuzonMouthY0 + 1, MaterialId.Empty);

            RegistrarObra(BuzonX0, BuzonBaseY, BuzonX1, BuzonBlockTopY);
        }

        // =================================================================
        // LO QUE PERSISTE (playtest 25, CONTRATO_PERSISTE.md sección 4.5) --
        // el REAMUEBLADO del cuarto íntimo. PÁRRAFO HISTÓRICO (playtest 26 lo
        // CORRIGE, ver el bloque siguiente): en el playtest 25, Crisol/
        // Prensa/BancoChispa/Ensayo tallaban su PROPIA mampostería en Init()
        // vía PaintStable, y SimLevelBuilder solo daba el ancla de una celda.
        // ESO YA NO ES CIERTO -- se deja el párrafo para que quede constancia
        // del reparto anterior, no como instrucción vigente.
        //
        // =================================================================
        // PLAYTEST 26 (CONTRATO_LEGIBILIDAD.md §2, regla 47 de CLAUDE.md) —
        // SIMLEVELBUILDER PASA A TALLAR TODA LA MAMPOSTERÍA DEL PLANO
        // =================================================================
        // Las máquinas DEJAN de tallar su propia mampostería en Init()
        // (Game/Crisol.cs::TallarMamposteria, Game/Prensa.cs::TallarLecho,
        // Game/BancoChispa.cs::TallarRanura, Game/EnsayoMaestro.cs::
        // TallarCubeta quedan vivas SOLO para Mudanza en caliente -- ver el
        // docblock de cada clase). Cada una expone un método ESTÁTICO
        // `TallarEnPlano(CellGrid, ...)` que opera directo sobre la grilla
        // (SetCell, no PaintStable -- es construcción de nivel, no runtime,
        // ver regla 29 de CLAUDE.md) con la MISMA geometría que su propia
        // instancia, así que las medidas (anchos/altos/grosores de muro) NO
        // se duplican a mano aquí: BuildCuartoIntimo solo pasa el ancla
        // (constantes de más abajo) y `CuartoY0+2` como `baseY`.
        //
        // SITIOS ELEGIDOS (suelo y=CuartoY0+2=170, huelga de piso último
        // sólido -- mismo criterio que `CunaCriaturaY`), leídos de izquierda
        // a derecha -- ORDEN NUEVO del contrato §2 ("la línea del taller"):
        // caños->CRISOL->PRENSA->COLUMNA->CHISPA->ENSAYO->pasillo (a la
        // Tolva). Columna y Chispa INTERCAMBIAN orden respecto al playtest 25
        // (antes Chispa->Columna): ahora es hervir->prensar->OBSERVAR en
        // vidrio->REVELAR lo invisible->examinar, la progresión que pide el
        // contrato ("crudo->transformar->forzar->observar->revelar->
        // examinar->entregar").
        //   pilas (234..246, ver PilaAguaX0/PilaLimoX0 más arriba) -> [7
        //   huelga] -> CRISOL (huella real 254..268, 15 celdas: ver
        //   Crisol.CubetaAncho/TolvaAncho/HuecoEntreCubetaYTolva) -> [10
        //   huelga, ≥8 pedido por el contrato] -> PRENSA (279..285, 7
        //   celdas) -> [10] -> COLUMNA (296..300, 5 celdas) -> [9] -> BANCO
        //   DE CHISPA (310..314, 5 celdas) -> [13] -> ENSAYO DEL MAESTRO
        //   (328..332, 5 celdas) -> [24 hasta la pared, sala abierta de
        //   sobra] -> pared derecha del cuarto (357) -> pasillo pre-carvado
        //   -> Tolva.
        // Las CUATRO huelgas entre estaciones (10/10/9/13) superan el
        // mínimo de 8 celdas que pide el contrato -- medidas EXACTAS a partir
        // de la aritmética real de cada máquina (Crisol.CubetaAncho/
        // TolvaAncho/etc., Prensa.LechoAncho, BancoChispa.RanuraAncho,
        // EnsayoMaestro.PlintoAncho, todas PÚBLICAS desde este playtest para
        // que este comentario no mienta por duplicar un número a mano: se
        // recalcularon leyendo el código de cada archivo, no a ojo). Las
        // huellas de Crisol/Prensa/BancoChispa/Ensayo se derivan de sus
        // propias constantes de ancho (copiadas aquí en el comentario para
        // huelga, NO duplicadas como código: si alguno de esos archivos
        // cambia su ancho, este comentario puede quedar desactualizado -- la
        // huelga real la decide siempre el archivo dueño de la geometría,
        // esto es documentación de intención).
        // =================================================================

        // =================================================================
        // PLAYTEST 27 — LA LÍNEA DEL TALLER **GRANDE** (mandato 1: "TODAS las
        // máquinas al menos 6 VECES MÁS GRANDES... cada una un EDIFICIO
        // pequeño, no una cajita")
        // =================================================================
        // REPARTO EXACTO de las 218 celdas del cuarto (140..357), medido
        // sumando huellas reales, no a ojo. Cada tramo es [x0..x1] (ancho):
        //
        //   FUENTES   140..171 (32)   dos pilas 14x9 + machón de piedra
        //   aire      172..179 ( 8)   <- aquí nace el aprendiz (AprendizX=175)
        //   CRISOL    180..216 (37)   cámara + boca embudada + brasero aparte
        //   aire      217..224 ( 8)
        //   PRENSA    225..255 (31)   jambas + dintel + lecho abierto 15x5
        //   aire      256..259 ( 4)
        //   COLUMNA   260..282 (23)   fuste 13 de hueco x 34 de alto + boca
        //   aire      283..285 ( 3)
        //   CHISPA    286..312 (27)   bandeja 13x5 + dos electrodos + lámpara
        //   aire      313..316 ( 4)
        //   ENSAYO    317..345 (29)   dais elevado + bandeja 15x5 + dosel
        //   aire      346..357 (12)   -> pasillo a la Tolva (358..392)
        //
        // Suma exacta: 32+8+37+8+31+4+23+3+27+4+29+12 = 218 = el ancho del
        // cuarto (140..357). Cada huella se DERIVA de las constantes públicas
        // de su propia clase (Crisol.CamaraAncho/BocaVuelo/BraseroSeparacion,
        // Prensa.LechoAncho/JambaAncho, BancoChispa.BandejaAncho/PlintoAncho,
        // EnsayoMaestro.PlintoAncho/DaisVuelo/ColumnaAncho) y de las de aquí
        // para la Columna: si alguna cambia, ESTE COMENTARIO puede quedar
        // desactualizado, pero la geometría real no se descuadra -- cada
        // TallarEnPlano hace su propia aritmética. Es documentación de
        // intención, igual que en el playtest 26.
        //
        // COMPARATIVA DE HUELLA (playtest 26 -> 27), que es el mandato
        // literal ("al menos 6 veces más grandes"):
        //   Crisol   15x6=90    -> 37x24 = 888    (x9.9, hogar incluido)
        //   Prensa    7x4=28    -> 31x30 = 930    (x33)
        //   Columna   5x22=110  -> 23x42 = 966    (x8.8)
        //   Chispa    5x3=15    -> 27x38 = 1026   (x68, lámpara incluida)
        //   Ensayo    5x4=20    -> 29x44 = 1276   (x64, dosel incluido)
        //   Pila (una) 6x3=18   -> 14x9  = 126    (x7)
        // Y lo que de verdad importa, la CAPACIDAD del recinto que recibe
        // materia (una ración de caño son 45 celdas; el 26 desbordaba con
        // todas):
        //   cámara del Crisol   35 -> 117 celdas
        //   lecho de la Prensa  15 ->  75
        //   bandeja del Banco    6 ->  65
        //   bandeja del Ensayo  12 ->  75
        //   pila de recogida     8 ->  70
        // =================================================================

        /// <summary>
        /// Ancla (centro de la CÁMARA) del Crisol -- Game/Crisol.cs la lee tal
        /// cual. 258 -&gt; 194 (playtest 27). **NO se mueve en el playtest 33**:
        /// es el único ancla que la zonificación nueva deja donde estaba, y a
        /// propósito -- la ZONA DE FUEGO ya estaba donde tiene que estar
        /// (izquierda-centro, pegada a las fuentes, con el Alambique justo
        /// encima) y moverla habría arrastrado también `AlambiqueX` y el
        /// rebote de fragua de Game/WorkshopBackdrop.cs sin ganar nada.
        /// Huella real: x 180..216, y 168..191 (hogar incluido).
        /// </summary>
        public const int CrisolX = 102; // (playtest 34) 194 -> 102: la ZONA DE FUEGO se va a la IZQUIERDA ("las que TRANSFORMAN a la izquierda"). Huella real 88..124.
        /// <summary>
        /// Ancla (centro del LECHO) de la Prensa. 282 -&gt; 240 (playtest 27)
        /// -&gt; **246** (playtest 33). Huella 231..261. La ZONA DE FUERZA gana
        /// aire propio a los dos lados: 14 celdas libres hasta el Crisol
        /// (antes 8) y 20 hasta la alcoba de observación (antes 4). Es la
        /// máquina más ruidosa del taller (jambas de 20 celdas + dintel) y era
        /// la que menos respiraba.
        /// </summary>
        public const int PrensaX = 158; // (playtest 34) 246 -> 158: segunda estación de la banda de TRANSFORMAR, a la izquierda de las fuentes. Huella 143..173.
        /// <summary>
        /// Ancla (centro de la BANDEJA) del Banco de Chispa. 312 -&gt; 299
        /// (playtest 27) -&gt; **324** (playtest 33). Huella 311..337. Se va
        /// con la Columna a la ALCOBA DE OBSERVACIÓN: los dos instrumentos
        /// quedan a 6 celdas uno de otro (columna 282..304, chispa 311..337),
        /// o sea que se leen como UNA estación doble bajo la misma bóveda
        /// alta, y no como dos eslabones más de la cinta transportadora.
        /// Cesar: *"asociar... las OBSERVACIONALES... romper la línea"*.
        /// </summary>
        public const int BancoChispaX = 302; // (playtest 34) 324 -> 302: sigue casado con la Columna en la ALCOBA DE OBSERVACIÓN, ahora a la derecha de las fuentes. Huella 289..315.
        /// <summary>
        /// Ancla (centro de la BANDEJA) del Ensayo del Maestro. 330 -&gt; 331
        /// (playtest 27) -&gt; **362** (playtest 33). Huella 348..376.
        /// EL CAMBIO DE ZONA MÁS GRANDE de la ronda, y el que Cesar pidió por
        /// su nombre: *"el Ensayo se va al vestíbulo de la Tolva porque su uso
        /// está más cercano a la ENTREGA"*. Con `CuartoX1`=378 el altar queda
        /// a 2 celdas de la pared del cuarto y a 3 de la boca del pasillo
        /// (`CarvePasilloTolva`, 379..392): ensayar la muestra y entregarla
        /// pasan a ser el mismo gesto en el mismo sitio, con el ATRIO
        /// (terraza de 338..347 + arco del pasillo) haciendo de antesala
        /// ceremonial compartida.
        /// </summary>
        public const int EnsayoPlintoX = 362; // (playtest 34) NO SE MUEVE: sigue casado con la Tolva en el extremo derecho, tal y como pidió Cesar.

        // =================================================================
        // (ronda 56, LA VIDA ÚTIL DE LO DESCUBIERTO, CONTRATO_RONDA56.md §2b)
        // EL SITIO RESERVADO DE LA MUFLA -- segunda instancia de Crisol,
        // spawneada por AlkahestGameBootstrap al completarse "obra_mufla"
        // (evento de OrderSystem, §3 del contrato).
        //
        // POR QUÉ NO CABE EN NINGÚN HUECO EXISTENTE (medido, no a ojo --
        // regla 39 de CLAUDE.md): la huella TOTAL de un Crisol (con el
        // brasero de playtest 48, BraseroAncho=7) mide
        // <c>CamaraAncho/2 + MuroGrosor + BocaVuelo</c> a la izquierda del
        // ancla más <c>CamaraAncho/2 + 2*MuroGrosor + BraseroSeparacion +
        // BraseroAncho + MuroGrosor</c> a la derecha -- **39 celdas en
        // total** (verificado con el propio Crisol.CrisolX: 88..126, DOS
        // celdas más ancho que el "88..124" que documentan los comentarios
        // de zonificación de más arriba, que datan de ANTES del playtest 48
        // -- ver la nota de esa ronda en Game/Crisol.cs sobre el brasero
        // agrandado). Ninguno de los huecos del PLANO CENTRAL llega a 39:
        // "paso" 125..142 (18, además YA tallado como terraza por
        // AdornarCuarto), "umbral" 174..197 (24), "escalinata" 232..257
        // (26), "atrio" 316..347 (32, YA terraza ceremonial) -- el más ancho
        // se queda 7 celdas corto INCLUSO vacío, y ninguno está de verdad
        // vacío: todos ya son terrazas decorativas talladas por
        // AdornarCuarto en el génesis (pisarlas sería "pisar algo
        // validado", justo lo que prohíbe el contrato).
        //
        // LA SOLUCIÓN: EL CUARTO CRECE 50 CELDAS MÁS A LA IZQUIERDA (mismo
        // movimiento, mismo criterio y misma dirección que ya usó el
        // playtest 34 para ganar espacio -- ver el bloque grande junto a
        // `CuartoX0` más abajo). El aire que YA existía entre el muro
        // izquierdo y el Crisol (80..87, 8 celdas, "muro izquierdo" en el
        // mapa de zonas) se convierte en la boca de un pasillo hacia una
        // SEGUNDA sala de fuego, en vez de intentar exprimir una estación de
        // 39 celdas en un hueco de 8. `VetaTurbaX0/X1/CaraX` (derivadas de
        // `CuartoX0`, regla 47) se mudan CON el muro sin ningún cambio de
        // fórmula.
        //
        // SITIO: `MuflaX`=55, huella real 41..79 (39 celdas) -- 11 celdas de
        // aire hasta el nuevo muro izquierdo (`CuartoX0`=30) y 8 celdas de
        // aire hasta la huella real del Crisol (88), ninguna de las dos
        // toca nada. `MuflaBaseY` reutiliza EXPLÍCITAMENTE
        // <see cref="BaseYDeEstacion"/>(<see cref="CrisolX"/>) en vez de
        // <c>BaseYDeEstacion(MuflaX)</c> -- DEUDA DE Game/Crisol.cs, no de
        // este archivo (ver el docblock de <see cref="ReservarSitioMufla"/>
        // para el porqué exacto): `Crisol.Init()` hardcodea
        // `BaseYDeEstacion(CrisolX)` para CUALQUIER ancla que reciba (un
        // supuesto de instancia única que esta ronda es la primera en
        // romper), así que la MASONRY (`Crisol.TallarEnPlano`, que sí recibe
        // `baseY` como parámetro explícito) tiene que usar el MISMO valor
        // para que la piedra tallada y la instancia viva coincidan celda a
        // celda -- `MuflaX`=55 cae en el mismo bracket que `CrisolX`=102
        // (los dos &lt;250) así que, por una vez, el atajo de Crisol.cs da
        // la respuesta correcta por casualidad de rango, no por corrección
        // general: si algún día `MuflaX` cruzara 250, seguiría habiendo que
        // pasar `MuflaBaseY` a mano (como ya se hace aquí) para no
        // heredar el bug.
        // =================================================================
        public const int MuflaX = 55;
        public static readonly int MuflaBaseY = BaseYDeEstacion(CrisolX);

        // =================================================================
        // (CONTRATO_TERMICA.md §3b, ENCARGO I, playtest 44) LA ALCOBA FRÍA:
        // el sitio de la piedra gélida (Game/ChillStone.cs, encargo T EN
        // PARALELO -- aquí SOLO se talla el suelo+contención; la máquina la
        // instancia Game/AlkahestGameBootstrap.cs con las coordenadas de
        // abajo, invocando ChillStone.Init tal cual, sin tocar ese archivo).
        // =================================================================
        // SITIO ELEGIDO (mandato de Cesar vía contrato: "la FRÍA en la
        // alcoba de la columna, la zona más 'de instrumentos' del taller"):
        // el hueco "columna|chispa" de LA ARQUITECTURA DEL CUARTO (ver el
        // bloque "AHORA TODO ES LINEAL" más abajo) -- 281..288 (8 celdas),
        // el ÚNICO tramo de suelo libre entre las dos estaciones de
        // OBSERVAR. `ColumnaX0`=260 + `ColumnaAncho`=19 + `ColumnaBocaVuelo`=2
        // da un huella exterior real de la Columna 258..280 (comprobado
        // contra su propia `Huella.OutX0/OutX1`, no contra el comentario
        // histórico "(23)" que quedó desalineado del ancho real -- regla 39
        // de CLAUDE.md); `BancoChispaX`=302 con huella documentada 289..315.
        // La pilastra que decora ese hueco (X=284, ver `PilastraColumnas`)
        // CUELGA del techo y no toca el suelo (todas terminan en y&gt;=240,
        // muy por encima de esta bandeja) -- cero colisión.
        //
        // POR QUÉ AQUÍ Y NO PEGADO AL FUSTE: la Columna necesita su interior
        // libre para el tanque de densidad (`ColumnaTanqueAlto`) y ya está
        // registrada como Obra -- superponer aquí encima habría significado
        // dos aparatos dibujándose en el mismo hueco. Un paso a la derecha,
        // A LA MISMA COTA (`BaseYDeEstacion(AlcobaFriaX0)` = 144, igual que
        // Columna/Chispa) es literalmente "la alcoba de la columna": el
        // sitio donde el jugador YA está mirando de cerca al observar.
        //
        // DECISIÓN FUERA DE CONTRATO, DOCUMENTADA -- SOLO DOS MURETES, NO UN
        // DrawUShape COMPLETO: el hueco disponible son 8 celdas exactas. Una
        // "U" con el mismo `PilaMuroGrosor`=2 de las fuentes dejaría solo 4
        // de interior, por debajo del mínimo de 8 que
        // HeatPlate/ChillStone.Init exige tras su propio recorte
        // `FootprintFraction`=0.4 (`Mathf.Max(8, ...)`, ver esos archivos).
        // Con SOLO dos muretes de 1 celda (uno a cada lado, sin muro de PIE
        // separado del colchón de suelo) el interior efectivo es el hueco
        // ENTERO -- encaje EXACTO con ese mínimo (margen=0), sin desbordar
        // ni hacia la Columna ni hacia la Chispa.
        public const int AlcobaFriaX0 = 281; // primera columna libre tras la Columna (huella real hasta 280).
        public const int AlcobaFriaAncho = 8; // hasta 288, un margen antes de que empiece BancoChispa (huella real desde 289).
        public const int AlcobaFriaX1 = AlcobaFriaX0 + AlcobaFriaAncho - 1; // 288
        /// <summary>Alto de los dos muretes de contención por encima del suelo (celdas) -- bandeja pequeña, suficiente para que lo vertido no se desparrame de inmediato hacia los vecinos.</summary>
        public const int AlcobaFriaMuroAlto = 8;
        /// <summary>Mismo criterio visual que `PlataformaProfundidad` de las 5 estaciones (Crisol/Prensa/BancoChispa/ColumnaEnsayo/EnsayoMaestro, cada una en su propio Game/*.cs): colchón de piedra bajo el suelo para que no quede un escalón raro contra los vecinos.</summary>
        private const int AlcobaFriaProfundidad = 6;
        /// <summary>Mismo criterio que `PlataformaMargen` de esas 5 estaciones.</summary>
        private const int AlcobaFriaMargen = 2;

        // =================================================================
        // (CONTRATO_RONDA48.md §1a, ENCARGO V, "EL DESATASCO") LA VETA DE
        // TURBA -- SOLO Semilla Cero (ver el gate en BuildVetaTurba). D1
        // diagnosticó que la única vía anterior para obtener la base
        // combustible garantizada de la seed (Universe.SemillaCeroBaseTurbaIdx)
        // era el Crisol, y que Universe.AplicarOverridesSemillaCero la
        // eclipsaba ahí para siempre; Cesar pt45 ya había pedido el arreglo
        // real: *"si hay que partir de carbón/madera esos serán los puntos
        // de partida"*. Esta veta ES ese punto de partida: turba tallada,
        // visible, alcanzable con el cincel desde el arranque, embutida en
        // piedra YA maciza -- nunca sale del limo (Universe la aparta de la
        // escalera de extracción a propósito, ver su override 1).
        //
        // SITIO ELEGIDO: el muro IZQUIERDO del cuarto (`CuartoX0`=80..82, ya
        // piedra maciza de fábrica por `DrawUShape`/`WallThickness`=3 en
        // `ExcavateCuarto`), el más cercano a la ZONA DE FUEGO (`CrisolX`=102,
        // huella real 88..124 -- ver su docblock): 6-8 celdas de aire lo
        // separan del crisol, la distancia más corta a cualquier estación
        // desde ese muro. Los dos caños del cuarto (`CanoAguaX`/
        // `CanoMontajeX`) YA NO montan ahí (playtest 34, "LA ESTACIÓN DE
        // FUENTES SE VA AL CENTRO": `CanoAguaX`=`MensulaAguaX1`≈200), así que
        // el muro entero queda libre de fábrica -- cero colisión con nada ya
        // construido en ese rango. Ancho 4 (`VetaTurbaX0`..`VetaTurbaX1` =
        // 80..83): una celda MÁS que el propio `WallThickness`=3 -- ver
        // `BuildVetaTurba` para el porqué del pilar extra (evitar la repisa
        // "ala izquierda de la línea del horno", X0=84, Y=`CuartoY0`+42=178,
        // primera entrada de `_galeriasOriginales` -- con X1=83 nunca se
        // solapan, sea cual sea la Y). Alto: `VetaTurbaY0`=`CuartoY0`+`WallThickness`=139
        // (una fila por encima de la losa del suelo, que ocupa
        // `CuartoY0`..`CuartoY0`+`WallThickness`-1=136..138 -- SIN redundar
        // con ella; con los caños ya lejos de este muro no hace falta ningún
        // margen adicional, así que la veta arranca prácticamente pegada al
        // suelo de la ZONA DE FUEGO, lo más "cerca del crisol" que da el
        // plano) a `VetaTurbaY1`=`CuartoY1`-4=258 (4 filas limpias antes del
        // remate del muro en 262, donde empieza la geometría de la bóveda).
        // (integración pt48, VISTO EN VIVO) CORRECCIÓN DE SITIO: la primera
        // versión ponía la veta en CuartoX0..CuartoX0+3 (80..83) creyendo que
        // ahí vivía el muro -- pero `CuartoX0` es LA PRIMERA COLUMNA DE AIRE
        // de la sala (ver el bloque "LA ESTACIÓN DE FUENTES" más arriba: la
        // cara del muro es CuartoX0-1 = 79), así que la veta caía en el aire,
        // solo tallaba 76 celdas donde pillaba piedra suelta, y el "pilar
        // extra" plantaba una columna de piedra FLOTANDO dentro del cuarto.
        // Medido en vivo con el grid corriendo: x68..79 es roca maciza 120/120
        // en todo el rango Y de la veta. La veta vive ahora EMBUTIDA en esa
        // roca, con la cara en x79 asomando al cuarto -- tallar (C) es el
        // verbo, como pide el contrato. El pilar extra se RETIRÓ entero
        // (regla 15: idea descartada -- ensanchar un muro que no era el muro).
        // (integración pt48, SEGUNDA LECCIÓN EN VIVO) LA CARA SE SELLA: la
        // primera reubicación dejaba la veta asomando en x79 (la cara del
        // muro) -- y la turba es POLVO: toda celda de la cara con aire en
        // diagonal-abajo se deslizaba sola, la veta entera se derramó en
        // cascada dentro del cuarto y enterró la boca del crisol (visto con
        // capturas, regla 52). La veta vive ahora SELLADA tras una columna
        // de piedra intacta (x79), y el jugador la descubre por el ASOMO de
        // la base (ver BuildVetaTurba: 3 celdas a ras de piso, de las que a
        // lo sumo una se desliza como migaja-señuelo) más la línea del
        // Maestro en el beat 4. Tallar (C) sigue siendo el verbo.
        public const int VetaTurbaX0 = CuartoX0 - 7;                  // 73
        public const int VetaTurbaX1 = CuartoX0 - 2;                  // 78 -- sellada: x79 (la cara) queda piedra.
        /// <summary>La columna de la CARA del muro (piedra intacta que sella la veta) -- solo el asomo de la base la perfora.</summary>
        public const int VetaTurbaCaraX = CuartoX0 - 1;               // 79
        public const int VetaTurbaY0 = CuartoY0 + WallThickness;      // 139
        public const int VetaTurbaY1 = CuartoY1 - 4;                  // 258
        /// <summary>Cada fila que cae en un "cuello" (contrato §1a, "bolsones dentro de la piedra": la turba es Polvo y CAE, así que un corredor sin estrechamientos se derramaría entero de golpe al tallar el primer hueco) tiene probabilidad `VetaTurbaCuelloPct`/100 -- ver <see cref="BuildVetaTurba"/>.</summary>
        private const int VetaTurbaCuelloPct = 8;
        /// <summary>Probabilidad de saltarse una celda NO-espina dentro del radio local -- el borde deshilachado que hace la silueta orgánica (contrato §1a: "no un rectángulo").</summary>
        private const int VetaTurbaBordeSaltoPct = 15;
        /// <summary>
        /// (regla del proyecto sobre salts de <see cref="XorShift"/>: elegir uno
        /// libre y documentarlo) Grep de `FromCell(` en toda `Assets/Alkahest`
        /// verificado antes de escribir esto: la sal declarada más alta del
        /// proyecto es `Game/Crisol.cs::SalFrenteHornada`=563. 569 es la
        /// primera libre a partir de ahí (y `SalVetaTurba+1` -- 570 -- para el
        /// segundo generador de la misma fila, el del borde deshilachado --
        /// también libre). No reutilizar 569/570 en otro archivo sin volver a
        /// grepear.
        /// </summary>
        private const uint SalVetaTurba = 569u;

        /// <summary>
        /// EL ALAMBIQUE (playtest 30, "LA ALQUIMIA VISIBLE" -- encargo de
        /// Cesar: "que el vapor se pueda ATRAPAR"). DECISIÓN: misma X que el
        /// Crisol, directamente encima de su chimenea/boca -- el vapor que
        /// Game/Crisol.cs emite sobre la cubeta (ver Crisol.EmitirVaporCubeta)
        /// sube por ese mismo hueco de aire YA EXCAVADO por
        /// <see cref="BuildCuartoIntimo"/> (ProcessGas sube 1 celda/tick si el
        /// hueco de encima está vacío -- determinista, no depende de que el
        /// gas deambule en horizontal). Ver Game/Alambique.cs para la
        /// geometría completa (domo+matraz).
        /// </summary>
        public const int AlambiqueX = CrisolX;
        /// <summary>
        /// 210: la boca del Crisol (su "abocinado") remata en
        /// <c>BocaY1+1</c> = 191 (baseY 170 + CamaraAlto 9 + BocaFilas 11 +
        /// 1, ver Game/Crisol.cs::Calcular) y su bocanada de humo nace visual
        /// en y≈201 (Crisol._humoOrigen) -- 210 deja 19 celdas de aire libre
        /// de margen sobre la boca real y por encima del alcance visual del
        /// humo, para que el domo frío del alambique nunca se confunda con la
        /// chimenea del crisol. El matraz+domo+techo resultante llega hasta
        /// y=225 (ver Game/Alambique.cs::Calcular), 15 celdas por debajo del
        /// techo de piedra del cuarto (<see cref="CuartoY1"/>=240).
        /// </summary>
        public const int AlambiqueBaseY = CuartoY0 + 42; // 178. (playtest 34) ERA UN LITERAL (210) atado al `CuartoY0` viejo: con el suelo 32 filas más abajo habría dejado el alambique flotando a 74 celdas de su crisol. Pasa a DERIVARSE (regla 39/47), conservando exactamente las 42 celdas sobre la losa que validó el playtest 30.

        /// <summary>
        /// Pared IZQUIERDA del fuste de la Columna de Ensayo. 296 -&gt; 262
        /// (playtest 27). Cesar sobre la del 26: *"si se le puede llamar así a
        /// esa escalera sin terminar, es inentendible"* -- tenía razón dos
        /// veces: 5 celdas de ancho con 3 de hueco no son una columna, y sus
        /// muros eran de <see cref="MaterialId.Crystal"/>, que **ES REACTIVO**
        /// con el Azoth del núcleo de leyes (la columna podía disolverse
        /// sola). Desde esta ronda los muros son <see cref="MaterialId.Stone"/>
        /// (inerte a leyes -- R2 del sorteo de química lo excluye del pool de
        /// reactivos, ver Universe.SortearLeyesGeneradas -- e inerte al cincel
        /// por <see cref="ObraDelTaller"/>) y el VIDRIO es un sprite
        /// translúcido delante (Game/ColumnaEnsayo.cs, mandato 5).
        /// </summary>
        /// (playtest 33) 262 -&gt; **284**: la Columna encabeza la ALCOBA DE
        /// OBSERVACIÓN (ver el bloque PLAYTEST 33 junto a `CuartoX1`). Huella
        /// exterior 282..304.
        public const int ColumnaX0 = 260; // (playtest 34) 284 -> 260: encabeza la alcoba de observación tras la escalinata nueva (232..257). Huella exterior 258..280.
        /// <summary>19 = 3 de muro + 13 de hueco + 3 de muro (antes 5 = 1+3+1).</summary>
        public const int ColumnaAncho = 19;
        /// <summary>Grosor del muro de piedra del fuste.</summary>
        public const int ColumnaMuro = 3;
        /// <summary>Alto del fuste sobre el suelo: 22 -&gt; 34 celdas (interior y=171..204).</summary>
        public const int ColumnaAlto = 34;
        /// <summary>Filas de ABOCINADO sobre el fuste (y=205..209): la boca se abre 2 celdas por lado, así que desde lejos se ve DÓNDE se deja caer la muestra.</summary>
        public const int ColumnaBocaFilas = 5;
        public const int ColumnaBocaVuelo = 2;
        /// <summary>Altura del TANQUE de la base (las N primeras filas del interior): ahí se vierten los líquidos y ahí se lee la estratificación. Solo lo usa el marco visual de Game/ColumnaEnsayo.cs -- físicamente es el mismo hueco.</summary>
        public const int ColumnaTanqueAlto = 10;
        /// <summary>
        /// Cada cuántas filas lleva el fuste una marca de nivel (nudillo de
        /// piedra que sobresale a los lados, NUNCA dentro del hueco de
        /// observación). 5 -&gt; **11** (segunda pasada del playtest 27, visto
        /// jugando): con paso 5 salían siete nudillos por lado y el fuste se
        /// leía como una ESCALERA -- el mismo veredicto que Cesar dio del
        /// playtest 26, reinventado sin querer. Con 11 quedan dos por lado, que
        /// es lo justo para que la piedra tenga textura; la graduación de
        /// verdad la dan ahora los tres zunchos de latón horizontales de
        /// Game/ColumnaEnsayo.cs.
        /// </summary>
        // (playtest 29) private -> public: Game/ColumnaEnsayo.cs necesita
        // leerla ahora que el tallado (TallarEnPlano/TallarEnCaliente) vive
        // en esa clase en vez de aquí mismo.
        public const int ColumnaMarcaPaso = 11;

        /// <summary>Primera fila de hueco interior de cualquier estación (justo sobre la losa del cuarto). Todas las estaciones se apoyan aquí -- una sola constante en vez de `CuartoY0+3` repetido.</summary>
        public const int EstacionSueloY = CuartoY0 + WallThickness; // 171

        // ---- El pasillo PRE-CARVADO a la Tolva (contrato §4.5) --------------
        /// <summary>
        /// 6 de alto (contrato del playtest 25, valor EXACTO) -&gt; **8**
        /// (playtest 33). El Ensayo del Maestro se muda al vestíbulo y este
        /// pasillo deja de ser un desagüe al fondo de la sala para ser LA
        /// PUERTA del atrio: 6 celdas obligaban a colarse rozando el suelo
        /// con el frasco lleno. Sigue holgadamente dentro de la franja de
        /// solape con la boca de la Tolva (`ChuteMouthY0..Y1` = 189..238):
        /// 195..202.
        /// </summary>
        private const int PasilloTolvaAlto = 8;
        /// <summary>
        /// Banda vertical del túnel: dentro de la franja de solape real entre
        /// el cuarto (`CuartoY0..CuartoY1`=168..209) y la boca de la Tolva
        /// (`ChuteMouthY0..Y1`=189..238) documentada en el docblock de la
        /// clase ("LA TOLVA, SELLADA") -- 189..209, 21 filas. Centrada con
        /// margen a ambos lados (6 filas por debajo hasta 189, 9 por encima
        /// hasta 209): ni pegada al suelo del cuarto ni al techo.
        /// </summary>
        private const int PasilloTolvaY0 = CuartoY0 + 27; // 163. (playtest 34) ERA UN LITERAL (195). Baja las MISMAS 32 filas que el cuarto: el pasillo no se rediseña, se traslada, conservando su relación exacta con el atrio del Ensayo y con la boca de la Tolva.

        /// <summary>
        /// Dónde nace el aprendiz (contrato). TERCERA RONDA: el aprendiz
        /// pasa del INTERIOR de la cuna (antes X=300, a la DERECHA del
        /// centro -- casi pegado al lado del capullo) a flotar justo por
        /// ENCIMA del remate abierto (`CunaTopY`+1), a la IZQUIERDA del
        /// centro de la cuna -- "el aprendiz nace a la izquierda de la
        /// criatura, a pocas celdas, sobre esa misma línea" (encargo). Sigue
        /// siendo el mismo grupo cuna/criatura, sin sobrevolar el centro
        /// geométrico de nada:
        /// dx=`CunaCriaturaX`-`AprendizX`=295-290=5,
        /// dy=(`CunaCriaturaY`+2)-`AprendizY`=172-180=-8,
        /// distancia euclídea al centro del disco sembrado del Rescoldo =
        /// √(5²+8²) = √89 ≈ 9.4 celdas -- "unas pocas celdas", del mismo
        /// orden que la ronda anterior (6.4).
        ///
        /// ENCUADRE DE CÁMARA DESDE ESTE PUNTO (calculado, no a ojo, réplica
        /// exacta de `FitMainCamera`/`UpdateCameraFollow` de
        /// `Sim/SimRenderer.cs`, aspect 16:9, zoom
        /// `CuartoIntimoZoomFactor`=5/16 -- ver el docblock de esa constante
        /// para el porqué del valor): la cámara arranca en el centro del
        /// mundo (38.4, 14.4 world units, `Editor/AlkahestSceneBuilder.
        /// BuildMainCamera`) y el primer frame (snap=true) la mueve, con la
        /// zona muerta del 30%, a (30.25, 17.375) world units --
        /// rectángulo visible resultante en CELDAS: X≈[262.5, 342.5] (80
        /// celdas), Y≈[151.25, 196.25] (45 celdas EXACTAS, dentro del 45-55
        /// pedido). El charco (X 267-280), la cuna (X 285-304, Y 168-179) y
        /// la repisa/capullo (X 299-314, Y 168-170) caen ENTEROS dentro de
        /// ese rectángulo desde el segundo cero. De las 45 celdas de alto,
        /// 16.75 quedan por DEBAJO del suelo real de la sala (`CuartoY0`,
        /// piedra de verdad, sin excavar -- la cámara de seguimiento con
        /// zona muerta ancla al aprendiz al 65% de la altura del encuadre,
        /// así que con el aprendiz a solo 12 celdas del suelo una franja de
        /// roca real sigue entrando por abajo, documentada aquí en vez de
        /// fingida a cero) y 16.25 son sala excavada VACÍA por encima del
        /// grupo cuna/repisa (aire libre, no roca) -- el resto (~12 celdas)
        /// es el propio grupo cuna/charco/repisa/criatura. Verificado
        /// numéricamente en el informe de la ronda (réplica en Python de la
        /// misma aritmética de cámara).
        ///
        /// CUARTA RONDA (contrato §4.5, encargo A): `BuildCuna`/`BuildRepisa`
        /// dejan de llamarse desde `BuildCuartoIntimo` (el cuarto ya no
        /// siembra criatura/capullo, ver el comentario en ese método), así
        /// que el remate físico de la U que describe este párrafo YA NO SE
        /// PINTA -- el aprendiz nace flotando sobre aire excavado, no sobre
        /// un borde de piedra real. `CunaTopY` (y por tanto la fórmula de
        /// `AprendizY` de abajo) SIGUE existiendo y sigue valiendo 179: es
        /// una constante derivada de `CunaX0/CunaWidth/CunaHeight`, que no
        /// toqué, así que el NÚMERO no cambia. Deliberadamente NO recoloqué
        /// el punto de aparición solo porque su vecindario dejó de
        /// construirse (regla 47 de CLAUDE.md, en corolario: mover un sitio
        /// ya validado sin una razón que lo exija es el mismo error que
        /// reutilizar uno por el nombre) -- cae ahora sobre el hueco abierto
        /// entre `BuildCuartoFloor` y la nueva maquinaria de la izquierda del
        /// Crisol.
        ///
        /// VERIFICADO TRAS PLAYTEST 26 (`CrisolX` 274-&gt;258, `PrensaX`
        /// 294-&gt;282, reordenación de la línea del taller, contrato §2): 290
        /// sigue cayendo en el hueco ABIERTO entre la huella de la Prensa
        /// (outer 279..285, ver `Game/Prensa.cs`) y `ColumnaX0`=296 -- no
        /// colisiona con ninguna mampostería nueva. `AprendizY`=180 además
        /// queda muy por encima de la altura de cualquier estación (la más
        /// alta, el Crisol, no pasa de `CuartoY0+2+CubetaAlto`=175), así que
        /// tampoco hay colisión vertical. NO se recoloca (mismo corolario de
        /// la regla 47 citado arriba): sigue siendo un sitio validado sin
        /// razón para moverlo.
        /// </summary>
        /// <summary>
        /// (playtest 27) 290 -&gt; 175. Esta vez SÍ había razón para moverlo (el
        /// corolario de la regla 47 que citaba el párrafo de arriba pide una
        /// razón, no inmovilidad): con la línea nueva, x=290 cae DENTRO de la
        /// bandeja del Banco de Chispa. El sitio nuevo es el hueco de aire
        /// entre las FUENTES y el CRISOL (173..177), que además es el sitio
        /// narrativamente correcto: el jugador abre los ojos al principio de
        /// la línea, con los dos caños a su izquierda y la boca del crisol a
        /// su derecha, que es el primer gesto del juego.
        ///
        /// ENCUADRE (misma aritmética que `SimRenderer.FitMainCamera`, aspect
        /// 16:9, zoom `CuartoIntimoZoomFactor`=5/8): el rectángulo visible
        /// mide ~160x90 celdas, así que desde aquí entran las dos pilas
        /// (141..171), el machón, el CRISOL entero con su boca y su brasero
        /// (180..220) y todavía asoma la Prensa por la derecha -- tres
        /// estaciones de golpe, y el cuarto entero cabe de alto (73 celdas de
        /// sala en 90 de encuadre). El taller grande se lee de un vistazo; a
        /// esta distancia una estación de 41 celdas ocupa el 26% del ancho de
        /// pantalla, que es la presencia que pedía el mandato 1.
        /// </summary>
        public const int AprendizX = 186; // (playtest 34) 175 -> 186: EL UMBRAL, justo a la izquierda de la isla de fuentes -- el jugador nace mirando el centro del taller, con transformar a su izquierda y observar a su derecha.
        public const int AprendizY = CuartoY0 + 18; // 154. (playtest 34) ERA UN LITERAL (186) y con el suelo 32 filas más abajo habría hecho nacer al aprendiz a 50 celdas del piso. Derivado: misma altura relativa de siempre (a la altura del caño de agua).

        /// <summary>
        /// Construye el mundo del pivot (playtest 21): todo piedra salvo la
        /// cámara íntima. Ver el docblock de la clase para el reparto
        /// completo de lo que hay dentro y por qué. `BuildTestLevel` NO se
        /// llama desde aquí -- las dos construcciones son alternativas, no
        /// una capa sobre la otra (ver `AlkahestSim.Start`, que elige una de
        /// las dos).
        /// </summary>
        // =================================================================
        // OBRA DEL TALLER (playtest 27) -- registro de la mampostería de las
        // estaciones, para que el CINCEL no pueda llevársela por delante.
        // Cesar, probando el 26: "me dio la impresión que con la herramienta
        // para eliminar bedrock me llevé parte de la construcción de los
        // equipos". Cada Tallar* registra aquí su rect EXTERIOR (muros
        // incluidos); Game/Cincel.cs consulta EsObraDelTaller antes de tallar
        // piedra. Estático y reconstruido en cada BuildCuartoIntimo (misma
        // vida que el plano); List fija, cero allocs en consulta.
        //
        // OBRA MOVIBLE (playtest 29, doble bug reportado por Cesar: "la
        // tercera y la última estructura no las puedo mover" + "van dejando
        // una huella de bedrock... de donde estuvieron antes"). La causa
        // exacta del segundo bug era que el registro era SOLO DE ALTA: una
        // estación que se reposicionaba (Game/Mudanza.cs) tallaba su
        // mampostería nueva pero (a) nunca borraba la vieja y (b) nunca podía
        // actualizar SU rect aquí, así que el cincel seguía protegiendo el
        // sitio viejo -- piedra fantasma Y encima indestructible.
        //
        // DISEÑO ELEGIDO (el más limpio de los dos que planteaba el encargo):
        // `RegistrarObra` devuelve un HANDLE (el índice en la lista) y
        // `ActualizarObra(handle, ...)` sustituye ese rect in-place. El
        // HANDLE LO GUARDA LA INSTANCIA, NO EL TALLADO ESTÁTICO: los
        // `TallarEnPlano` estáticos de Crisol/Prensa/BancoChispa/
        // EnsayoMaestro/ColumnaEnsayo (llamados una vez desde
        // BuildCuartoIntimo, ANTES de que exista ningún GameObject) ya NO
        // llaman a RegistrarObra -- solo tallan piedra. Es la INSTANCIA, en
        // su propio `Init()` (que corre después, cuando AlkahestGameBootstrap
        // la crea, y que ya volvió a calcular su propia geometría con el
        // mismo método `Calcular`/`RecalcularRegion*` que usó el tallado
        // estático), la que llama a `RegistrarObra` y se queda con el
        // handle. Alternativa descartada: que `TallarEnPlano` devolviera el
        // handle y BuildCuartoIntimo lo guardara en un campo estático por
        // estación para que Init lo leyera -- funciona, pero exige un canal
        // de paso de estado entre dos llamadas separadas en archivos
        // distintos (SimLevelBuilder y AlkahestGameBootstrap) solo para
        // sortear algo que la propia instancia ya puede calcular sola. Con
        // el diseño elegido, cada estación es dueña de su handle desde que
        // nace hasta que se destruye (mismo ciclo de vida que ya usa con
        // MachineFocus/Mudanza.RegistrarMovible), y no hay ninguna entrada
        // huérfana: cada rect de la lista tiene SIEMPRE 0 o 1 dueños vivos.
        // =================================================================
        /// <summary>Rect inclusivo propio (este archivo no usa UnityEngine a propósito: es sim pura; RectInt habría traído el using solo para esto).</summary>
        public struct RectObra { public int X0, Y0, X1, Y1; }

        public static readonly System.Collections.Generic.List<RectObra> ObraDelTaller = new System.Collections.Generic.List<RectObra>(16);

        /// <summary>Registra un rect exterior de mampostería protegida (x0..x1, y0..y1 inclusivos). Devuelve el HANDLE (índice en la lista) para poder actualizarlo luego con <see cref="ActualizarObra"/> cuando la estación se mueva -- ver el bloque "OBRA MOVIBLE" de arriba.</summary>
        public static int RegistrarObra(int x0, int y0, int x1, int y1)
        {
            ObraDelTaller.Add(new RectObra { X0 = x0, Y0 = y0, X1 = x1, Y1 = y1 });
            return ObraDelTaller.Count - 1;
        }

        /// <summary>
        /// (playtest 29) Sustituye el rect protegido en el HANDLE indicado --
        /// lo llama `Reposicionar` de cada estación movible, DESPUÉS de
        /// borrar su mampostería vieja y tallar la nueva, para que el
        /// registro anticincel SIGA a la piedra en vez de proteger para
        /// siempre un sitio que ya no tiene nada tallado. Un handle fuera de
        /// rango (-1, o una lista que se vació y se reconstruyó sin que la
        /// instancia se enterara) es un error de programación, no algo que
        /// deba tirar una excepción a mitad de una mudanza -- se ignora en
        /// silencio, igual que `EsObraDelTaller` no valida sus rects.
        /// </summary>
        public static void ActualizarObra(int handle, int x0, int y0, int x1, int y1)
        {
            if (handle < 0 || handle >= ObraDelTaller.Count) return;
            ObraDelTaller[handle] = new RectObra { X0 = x0, Y0 = y0, X1 = x1, Y1 = y1 };
        }

        /// <summary>¿(x,y) pertenece a la obra protegida del taller? Lo consulta el cincel celda a celda -- bucle plano sobre ~10 rects, sin allocs.</summary>
        public static bool EsObraDelTaller(int x, int y)
        {
            for (int i = 0; i < ObraDelTaller.Count; i++)
            {
                var r = ObraDelTaller[i];
                if (x >= r.X0 && x <= r.X1 && y >= r.Y0 && y <= r.Y1) return true;
            }
            return false;
        }

        /// <summary>
        /// (playtest 32, FIX de raíz de "las máquinas aparecen enterradas" /
        /// "el suelo bajo la columna tiene forma rara") Busca un rect YA
        /// registrado con estas coordenadas EXACTAS y devuelve su handle, o
        /// -1 si no existe.
        ///
        /// POR QUÉ HACE FALTA: <see cref="AdornarCuarto"/> corre DENTRO de
        /// <see cref="BuildCuartoIntimo"/> -- es decir, ANTES de que exista
        /// ninguna instancia de Crisol/Prensa/ColumnaEnsayo/BancoChispa/
        /// EnsayoMaestro (esas se crean después, cuando
        /// Game/AlkahestGameBootstrap.cs llama a su `Init`). Pero el registro
        /// anticincel de esas cinco estaciones lo hace la INSTANCIA en su
        /// propio `Init` (ver el bloque "OBRA MOVIBLE" más arriba), NUNCA su
        /// `TallarEnPlano` estático. Resultado: cuando `AdornarCuarto` leía
        /// `ObraDelTaller` para decidir dónde tallar terrazas, la lista SOLO
        /// tenía las pilas de fuentes y el plinto del Alambique -- las cinco
        /// estaciones de la línea del taller eran invisibles para él, así que
        /// una terraza podía tallarse literalmente ENCIMA de su propia
        /// mampostería (el escalón asomando dentro/al borde de la huella =
        /// "enterrada" / "suelo con forma rara"). Confirmado leyendo el
        /// código, no solo sospechado (regla 30 de CLAUDE.md).
        ///
        /// LA SOLUCIÓN: cada `TallarEnPlano` de esas cinco estaciones AHORA
        /// también llama a <see cref="RegistrarObra"/> con el rect exacto
        /// que su propia instancia calculará después (mismo `anchorX`/
        /// `baseY`, mismo método `Calcular` -- letra por letra). Cuando la
        /// instancia nace y corre `Init`, en vez de registrar un rect NUEVO
        /// (que dejaría el de género huérfano para siempre, protegiendo un
        /// sitio sin dueño y rompiendo la garantía "0 o 1 dueños" del bloque
        /// de arriba), busca ese rect con este método y SE QUEDA su handle.
        /// Cero canal de paso de estado entre BuildCuartoIntimo y Bootstrap
        /// (la alternativa que el bloque "OBRA MOVIBLE" ya descartaba): solo
        /// una búsqueda barata (~10 rects) sobre coordenadas que ya coinciden
        /// por construcción.
        /// </summary>
        public static int HallarObraExacta(int x0, int y0, int x1, int y1)
        {
            for (int i = 0; i < ObraDelTaller.Count; i++)
            {
                var r = ObraDelTaller[i];
                if (r.X0 == x0 && r.Y0 == y0 && r.X1 == x1 && r.Y1 == y1) return i;
            }
            return -1;
        }

        // =====================================================================
        // SEMILLA CERO -- TAPIADOS POR PREGUNTA (playtest 40,
        // CONTRATO_SEMILLA.md §3, API CONGELADA -- el otro encargo de la ronda
        // codea contra `TapiarSalasSemillaCero`/`DestaparSala` sin conocer el
        // resto de este bloque).
        // =====================================================================
        // LA IDEA: en vez de duplicar a mano la aritmética de `Calcular()` de
        // Prensa/ColumnaEnsayo/BancoChispa/EnsayoMaestro (cuatro archivos
        // ajenos que podrían cambiar de forma sin avisar), se captura el rect
        // EXACTO que cada uno ya registra en `ObraDelTaller` (bloque "OBRA
        // MOVIBLE" arriba) justo al tallarse, en `BuildCuartoIntimo` (ver
        // `RegistrarObraSemillaCero`, llamado ahí). Tapiar = rellenar de
        // piedra TODO lo que dentro de ese rect siga siendo aire en el
        // génesis (el hueco de lecho/bandeja/plinto/tanque -- lo que sea que
        // cada TallarEnPlano dejara vacío), lo que convierte el propio marco
        // de jambas+dintel/muros de la estación -- YA con forma de puerta,
        // por diseño de la línea del taller, playtest 26 -- en una puerta
        // condenada: se lee distinto de un tramo de corredor liso porque
        // conserva la silueta de portal, sin inventar un material nuevo ni
        // depender de sprites que aún no existen (decisión de M, documentada:
        // ver el bloque de abajo sobre por qué las máquinas NO se spawnean).
        // Las celdas que se tapiaron se recuerdan (no se recalculan) para que
        // `DestaparSala` las borre UNA A UNA sin tocar el marco de piedra que
        // ya estaba ahí desde el génesis.
        //
        // LAS MÁQUINAS TAPIADAS NO SPAWMEAN SUS SPRITES/FOCOS (decisión de M,
        // ver el docblock de contrato §3): `Game/AlkahestGameBootstrap.cs` NO
        // llama a `SpawnPrensa/SpawnColumnaEnsayo/SpawnBancoChispa/
        // SpawnEnsayoMaestro` mientras la sala siga tapiada -- lee
        // `SalaDestapada(n)` (más abajo) por polling barato en su propio
        // Update(), el mismo patrón que ya usa para reintentar el spawn
        // inicial. Así nunca hay una `MachineFocus.Registrar` ni una chapa
        // "E — usar" para una máquina detrás del muro (lo que el contrato
        // prohíbe explícitamente).
        // (CONTRATO_TERMICA.md §3c, ENCARGO I, playtest 44) SalaFria=4: QUINTA
        // sala tapiable, EXTENSIÓN de la API congelada de CONTRATO_SEMILLA.md
        // -- ninguna firma existente cambia (TapiarSalasSemillaCero/
        // DestaparSala/SalaDestapada siguen recibiendo lo mismo de siempre),
        // solo se suma un índice válido más al mismo mecanismo. El beat del
        // FRÍO (Game/SemillaCero.cs) la destapa exactamente como las otras
        // cuatro destapan la suya.
        public const int SalaPrensa = 0, SalaColumna = 1, SalaChispa = 2, SalaEnsayo = 3, SalaFria = 4;
        private const int SalasSemillaCeroCount = 5;

        private static readonly RectObra[] _obraSalaSemillaCero = new RectObra[SalasSemillaCeroCount];
        private static readonly System.Collections.Generic.List<int>[] _celdasTapiadasSemillaCero =
        {
            new System.Collections.Generic.List<int>(96),
            new System.Collections.Generic.List<int>(96),
            new System.Collections.Generic.List<int>(96),
            new System.Collections.Generic.List<int>(96),
            new System.Collections.Generic.List<int>(24), // (SalaFria) la alcoba fría es mucho más pequeña que las otras cuatro -- capacidad inicial menor, el List crece solo si hiciera falta.
        };
        private static readonly bool[] _salaDestapadaSemillaCero = new bool[SalasSemillaCeroCount];

        /// <summary>Llamado justo tras cada `TallarEnPlano` de las cuatro estaciones tapiables en `BuildCuartoIntimo`: si esa llamada registró un rect nuevo en `ObraDelTaller` (siempre debería), lo recuerda para el bloque de Semilla Cero.</summary>
        private static void RegistrarObraSemillaCero(int sala, int obraCountAntes)
        {
            _obraSalaSemillaCero[sala] = ObraDelTaller.Count > obraCountAntes
                ? ObraDelTaller[obraCountAntes]
                : new RectObra { X0 = 0, Y0 = 0, X1 = -1, Y1 = -1 }; // rect vacío/inválido -- defensivo, no debería pasar nunca (regla 51: que falle a la vista si pasa).
        }

        /// <summary>¿Sigue tapiada la sala `sala` (0=prensa,1=columna,2=chispa,3=ensayo,4=fría)? Solo tiene sentido en modo Semilla Cero -- fuera de él, `TapiarSalasSemillaCero` nunca se llamó y esto siempre da `false`.</summary>
        public static bool SalaDestapada(int sala) => sala >= 0 && sala < SalasSemillaCeroCount && _salaDestapadaSemillaCero[sala];

        /// <summary>
        /// API CONGELADA (CONTRATO_SEMILLA.md §3). Tapia con mampostería de
        /// obra (indestructible al cincel: reutiliza EL MISMO rect que
        /// `ObraDelTaller` ya protege, ver `EsObraDelTaller`) las cinco
        /// salas de Semilla Cero (CONTRATO_TERMICA.md §3c sumó la quinta,
        /// SalaFria). Llamada UNA vez, en el génesis del mundo
        /// (`Game/AlkahestSim.cs::CrearMundoInterno`, justo después de
        /// `SimLevelBuilder.BuildCuartoIntimo`), antes de que exista ninguna
        /// instancia de máquina.
        /// </summary>
        public static void TapiarSalasSemillaCero(AlkahestSim sim)
        {
            var grid = sim.Grid;
            for (int sala = 0; sala < SalasSemillaCeroCount; sala++)
            {
                _salaDestapadaSemillaCero[sala] = false;
                _celdasTapiadasSemillaCero[sala].Clear();
                var r = _obraSalaSemillaCero[sala];
                if (r.X1 < r.X0 || r.Y1 < r.Y0) continue; // rect no poblado -- ver RegistrarObraSemillaCero.
                for (int y = r.Y0; y <= r.Y1; y++)
                {
                    for (int x = r.X0; x <= r.X1; x++)
                    {
                        if (!CellGrid.InBounds(x, y)) continue;
                        // Solo lo que el TallarEnPlano de la estación dejó
                        // vacío (el lecho/bandeja/plinto/tanque): el marco de
                        // jambas+dintel/muros YA es piedra y no hace falta
                        // tocarlo -- así DestaparSala puede devolver
                        // exactamente estas celdas a aire sin arriesgarse a
                        // borrar el marco que la propia estación talló.
                        if (grid.GetMat(x, y) != MaterialId.Empty) continue;
                        _celdasTapiadasSemillaCero[sala].Add(CellGrid.Idx(x, y));
                        grid.SetCell(x, y, MaterialId.Stone);
                    }
                }
            }
        }

        /// <summary>
        /// API CONGELADA (CONTRATO_SEMILLA.md §3). Destapa la sala `sala`
        /// (0=prensa,1=columna,2=chispa,3=ensayo,4=fría): borra el tapiado (solo las
        /// celdas exactas que `TapiarSalasSemillaCero` rellenó, no el marco)
        /// y despierta sus chunks -- el polvo de derrumbe lo regala
        /// `Game/ParticulasFx.cs` solo, observando el material cambiar bajo
        /// el visor (mismo mecanismo que cualquier otro cambio de bloque, sin
        /// que este método tenga que saber que existe). Idempotente: una
        /// segunda llamada sobre una sala ya destapada no hace nada.
        /// </summary>
        public static void DestaparSala(AlkahestSim sim, int sala)
        {
            if (sala < 0 || sala >= SalasSemillaCeroCount || _salaDestapadaSemillaCero[sala]) return;
            var grid = sim.Grid;
            var stepper = sim.Stepper;
            uint tick = stepper != null ? stepper.Tick : 0u;
            var celdas = _celdasTapiadasSemillaCero[sala];
            for (int i = 0; i < celdas.Count; i++)
            {
                int idx = celdas[i];
                grid.SetCell(idx, MaterialId.Empty);
                grid.WakeChunk(idx % CellGrid.W, idx / CellGrid.W, tick);
            }
            _salaDestapadaSemillaCero[sala] = true;
        }

        public static void BuildCuartoIntimo(CellGrid grid)
        {
            ObraDelTaller.Clear(); // (playtest 27) plano nuevo, registro nuevo -- ver el bloque OBRA DEL TALLER.
            FillWorldStone(grid);
            ExcavateCuarto(grid);
            // (playtest 33) EL TECHO DEJA DE SER PLANO. Va aquí, pegado a la
            // excavación, porque solo QUITA piedra por encima de CuartoY1: ni
            // una estación, ni una terraza, ni una balda vive tan arriba, así
            // que el orden respecto a todo lo demás es indiferente y este es
            // el sitio donde se lee (excavar la sala, y acto seguido abovedarla).
            TallarBoveda(grid);
            BuildCuartoFloor(grid); // (contrato §4.5) suelo UNIFORME de la sala entera -- ver el docblock, es lo que hace cierta la frase de EnsayoMaestro.TallarCubeta ("suelo ya es piedra maciza del cuarto").
            // (contrato §4.5, encargo A) BuildCuna/BuildRepisa YA NO SE
            // LLAMAN -- el cuarto íntimo deja de sembrar criatura/capullo
            // esta ronda (el encargo del taller de materiales, B, ya dejó
            // CunaCriaturaX/Y y CapulloX/Y sin usar salvo en líneas
            // COMENTADAS de AlkahestGameBootstrap.cs, confirmando que la
            // criatura se aparca por contrato, no por descuido mío). Los dos
            // métodos se CONSERVAN intactos sin llamantes (regla 15: bifurcar
            // -- documentar por qué se deja de llamar algo -- no borrar), por
            // si una ronda futura reintroduce la cría en otra zona. El hueco
            // que dejan (CunaX0=285..CunaX0+CunaWidth-1 y RepisaX0..+RepisaWidth-1,
            // ambas sobre CuartoY0) es justo el que ocupa la nueva maquinaria
            // de abajo -- ver el bloque "SITIOS ELEGIDOS" más arriba.
            //   BuildCuna(grid);
            //   BuildRepisa(grid);
            // (playtest 23) EL MONTÓN DE NUTRIENTE YA NO SE COLOCA -- el
            // método PlaceNutrienteMound se conserva intacto sin llamantes
            // (regla 15: bifurcar, no borrar). Cesar, jugando el 22:
            // "algunos materiales/semillas aparecen tirados por el mapa, y
            // eso no me gusta porque hace pensar en exploración/recolección
            // tipo Minecraft. Quiero que la fantasía sea fabricar, criar,
            // transformar y descubrir desde el laboratorio". Además el caño
            // de nutriente (CanoNutrienteY, infinito, coste 0) dejó el montón
            // redundante -- y quitarle a la criatura la comida pre-servida
            // convierte el PRIMER acto del jugador en ALIMENTARLA ÉL (el
            // rótulo de hambre le dice cómo): más íntimo, más causal, y
            // enseña el frasco de paso. El "primer estirón en 15s" pasa a ser
            // "responde a los segundos de que TÚ le des de comer".
            //   PlaceNutrienteMound(grid);
            // (playtest 26) PlaceCharco(grid) YA NO SE LLAMA -- lo reemplazan
            // las DOS pilas nombradas del contrato §2 (ver BuildPilasFuentes).
            // NOTA DE INTEGRACIÓN: la DECISIÓN original del encargo ("ambos
            // chorros caen por la misma columna") quedó OBSOLETA en la misma
            // ronda -- Dispenser ganó voladizo por instancia y el caño de limo
            // se spawnea con alcance 12 (ver AlkahestGameBootstrap), así que
            // cada chorro aterriza HOY sobre su propia pila: agua en x237
            // (PilaAgua 234..239), limo en x244 (PilaLimo 241..246). El método
            // viejo se conserva intacto sin llamantes (regla 15 de CLAUDE.md).
            //   PlaceCharco(grid);
            BuildPilasFuentes(grid); // (playtest 27) la ESTACIÓN DE FUENTES: dos pilas grandes + el machón del caño de limo.

            // (playtest 26, regla 47 de CLAUDE.md) LA LÍNEA DEL TALLER: cada
            // máquina talla su propia geometría vía su TallarEnPlano estático
            // (mismas medidas que su instancia -- ver el bloque "PLAYTEST 27"
            // más arriba, junto a CrisolX/PrensaX/etc.), en el orden del
            // contrato: Crisol -> Prensa -> Columna -> Chispa -> Ensayo.
            // (playtest 33, LA COTA POR ZONA -- cierra la deuda que el
            // arquitecto dejó anotada: "la observación no está de verdad más
            // arriba") Cada estación pregunta su cota a BaseYDeEstacion en
            // vez del literal único: la alcoba de observación vive SOBRE la
            // escalinata (+6) y el atrio del Ensayo un peldaño arriba (+3).
            // Sus plataformas soberanas (aplanado huella+2) hacen el resto.
            Crisol.TallarEnPlano(grid, CrisolX, BaseYDeEstacion(CrisolX));
            // (playtest 40, SEMILLA CERO) Capturamos el handle que cada
            // TallarEnPlano acaba de registrar en ObraDelTaller (ver el
            // bloque "OBRA MOVIBLE" más arriba: SIEMPRE registran su rect
            // exacto ahí) comparando el tamaño de la lista antes/después --
            // así SimLevelBuilder.TapiarSalasSemillaCero/DestaparSala pueden
            // leer el rect EXACTO de cada estación sin duplicar a mano la
            // aritmética de `Calcular()` de cuatro archivos ajenos (Prensa/
            // ColumnaEnsayo/BancoChispa/EnsayoMaestro), que además podría
            // desalinearse silenciosamente si esos archivos cambian de forma.
            int obraAntesPrensa = ObraDelTaller.Count;
            Prensa.TallarEnPlano(grid, PrensaX, BaseYDeEstacion(PrensaX));
            RegistrarObraSemillaCero(SalaPrensa, obraAntesPrensa);
            // (playtest 29, encargo C) EL TALLADO SE MUDÓ a Game/ColumnaEnsayo.cs
            // (`TallarEnPlano`, mismo patrón que Crisol/Prensa/BancoChispa/
            // EnsayoMaestro, regla 47): la Columna necesitaba un método
            // invocable POR INSTANCIA para poder implementar IMovible (borrar
            // mampostería vieja + tallar la nueva al Reposicionar). Las
            // constantes de geometría (ColumnaX0/Ancho/Muro/Alto/BocaFilas/
            // BocaVuelo/MarcaPaso/TanqueAlto) SIGUEN viviendo aquí -- son el
            // plano, no el tallado -- exactamente igual que Crisol lee
            // `SimLevelBuilder.CrisolX` pero es dueño de `CamaraAncho`.
            int obraAntesColumna = ObraDelTaller.Count;
            ColumnaEnsayo.TallarEnPlano(grid, ColumnaX0, BaseYDeEstacion(ColumnaX0)); // muros de PIEDRA (ya no Crystal) + abocinado + marcas de nivel.
            RegistrarObraSemillaCero(SalaColumna, obraAntesColumna);
            int obraAntesChispa = ObraDelTaller.Count;
            BancoChispa.TallarEnPlano(grid, BancoChispaX, BaseYDeEstacion(BancoChispaX));
            RegistrarObraSemillaCero(SalaChispa, obraAntesChispa);
            // (CONTRATO_TERMICA.md §3b, ENCARGO I) LA ALCOBA FRÍA: mismo
            // patrón que las cuatro de arriba (talla + registra su handle de
            // Semilla Cero) para que Game/ChillStone.cs (encargo T) tenga
            // dónde vivir sin que este archivo conozca esa clase.
            int obraAntesFria = ObraDelTaller.Count;
            BuildAlcobaFria(grid);
            RegistrarObraSemillaCero(SalaFria, obraAntesFria);
            int obraAntesEnsayo = ObraDelTaller.Count;
            EnsayoMaestro.TallarEnPlano(grid, EnsayoPlintoX, BaseYDeEstacion(EnsayoPlintoX));
            RegistrarObraSemillaCero(SalaEnsayo, obraAntesEnsayo);

            // (playtest 30, "LA ALQUIMIA VISIBLE") EL ALAMBIQUE: a diferencia
            // de las cinco de arriba, NACE COMO OBRA PENDIENTE (ver el
            // docblock de Game/Alambique.cs) -- aquí solo se talla el PLINTO
            // (una losa de piedra, ya en el génesis del mundo) y se registra
            // como obra anticincel; la mampostería real (matraz+domo) la
            // talla la propia instancia EN CALIENTE (PaintStable, regla 29)
            // cuando el jugador paga el cerámico y pulsa E.
            Alambique.TallarEnPlano(grid, AlambiqueX, AlambiqueBaseY);
            Alambique.PlintoRect(AlambiqueX, AlambiqueBaseY, out int alaX0, out int alaY0, out int alaX1, out int alaY1);
            RegistrarObra(alaX0, alaY0, alaX1, alaY1);

            // (CONTRATO_RONDA50.md §3b, ENCARGO G, playtest 50) LA TOLVA: "una
            // sola boca" (contrato, textual) -- en Semilla Cero la Tolva vive
            // DENTRO del cuarto, a segundos del Crisol (ver el bloque de
            // constantes junto a BuildTolvaCercana para el porqué exacto del
            // sitio); la Tolva CLÁSICA lejana (BuildDeliveryNiche, tras el
            // pasillo) NO se talla en absoluto -- ni pozo, ni pasillo, ni arco
            // (el contrafuerte al este de CuartoX1 queda piedra maciza sin
            // tocar, ver FillWorldStone). El modo CAÓTICO/MULTI no cambia
            // (contrato §3e): sigue tallando la de siempre, sin la cercana.
            if (AlkahestGameBootstrap.ModoSemillaCero)
            {
                // (playtest 54) BuildTolvaCercana YA NO SE LLAMA AQUÍ -- ver
                // el bloque de constantes `Buzon*` junto a EstanteBaseY para
                // el porqué completo. El método y sus constantes se
                // CONSERVAN intactos, sin llamantes (regla 15 de CLAUDE.md).
                BuildBuzonMaestro(grid); // registra su propia Obra ANTES de AdornarCuarto -- mismo criterio que la Tolva vieja.
            }
            else
            {
                BuildDeliveryNiche(grid); // SIN TOCAR: la Tolva queda sellada porque ya no hay nada excavado a su alrededor.
                CarvePasilloTolva(grid);  // (contrato §4.5) DESPUÉS de BuildDeliveryNiche a propósito -- ver el docblock del método.
            }
            // (ronda 56, LA VIDA ÚTIL DE LO DESCUBIERTO, CONTRATO_RONDA56.md
            // §2b) EL SITIO RESERVADO DE LA MUFLA: en el génesis es SOLO
            // suelo/piedra normal (la excavación de más arriba ya lo deja
            // hueco, la losa de WallThickness ya lo deja sólido -- nada que
            // tallar aquí todavía) -- SOLO se registra como Obra para que
            // AdornarCuarto (justo debajo) no plante una terraza/pilastra
            // encima. Tiene que ir ANTES de AdornarCuarto, como cualquier
            // otra estación de esta lista. Ver el docblock de
            // ReservarSitioMufla para las coordenadas exactas.
            ReservarSitioMufla();
            // (playtest 31) LA ARQUITECTURA: peldaños, pilastras y el arco del
            // pasillo. VA AL FINAL, después de TODAS las estaciones, porque
            // decide dónde puede tallar leyendo `ObraDelTaller` -- que solo
            // está completo cuando cada TallarEnPlano ya ha registrado su
            // huella. Ver el docblock de AdornarCuarto.
            AdornarCuarto(grid);
            // (playtest 33) LAS BALDAS, LO ÚLTIMO -- ver el docblock de
            // TallarRepisas para por qué no pueden registrarse antes que las
            // terrazas (ColumnaOcupada solo mira la X).
            TallarRepisas(grid);
            PaintClimate(grid);       // mismo ambiente uniforme que el plano viejo (regla 31 de CLAUDE.md: no reintroducir clima por zona).
            // (CONTRATO_RONDA48.md §1a, ENCARGO V) LA VETA DE TURBA -- AL FINAL
            // a propósito: solo sustituye celdas que YA son Stone, así que
            // corre después de TODA la arquitectura decorativa (AdornarCuarto/
            // TallarRepisas, que podrían tallar o vaciar algo en su rango) y
            // tiene la última palabra sobre su propio muro sin arriesgarse a
            // que un pase posterior la borre. Internamente comprueba
            // AlkahestGameBootstrap.ModoSemillaCero y no hace nada en
            // caótico/multi.
            BuildVetaTurba(grid);
        }

        /// <summary>
        /// (playtest 39, contrato ENCARGO S 1e, REACCIONES DIRIGIDAS) Registra
        /// como "zona de interés" (MaybeReact muestrea 1/2 en vez de 1/8, ver
        /// SimStepper.RegistrarZonaInteres) las cubetas de TODAS las máquinas
        /// del cuarto -- donde el jugador mira mientras algo reacciona. Toque
        /// PURAMENTE ADITIVO (contrato: "solo llamadas aditivas de registro
        /// de zonas") -- no cambia ni una celda del plano, solo llama a la
        /// API nueva de SimStepper con las mismas constantes de geometría que
        /// ya usa BuildCuartoIntimo para tallar cada estación.
        ///
        /// Los rectángulos son GENEROSOS a propósito (huella de la máquina +
        /// margen hacia arriba para el vapor/humo que sube de su cubeta): una
        /// zona de interés que se pasa un poco no rompe nada (sigue siendo
        /// "más reacciones", nunca menos), así que no hace falta la
        /// aritmética exacta de cada TallarEnPlano (Prensa/ColumnaEnsayo/
        /// BancoChispa/EnsayoMaestro/Alambique NO son archivos de este
        /// encargo -- propiedad disjunta, CLAUDE.md regla 37/41).
        ///
        /// NO SE LLAMA DESDE AQUÍ MISMO: SimLevelBuilder no tiene una
        /// referencia a SimStepper (solo talla CellGrid). El único archivo de
        /// este encargo con acceso simultáneo a ambos en tiempo de ejecución
        /// es Game/Crisol.cs (recibe AlkahestSim en Init) -- ver la llamada
        /// allí. Deuda de integración anotada en el informe de la ronda: el
        /// sitio "natural" para esta llamada es el bootstrap del nivel
        /// (Game/AlkahestGameBootstrap.cs), que no es archivo de este
        /// encargo.
        /// </summary>
        public static void RegistrarZonasInteres(SimStepper stepper)
        {
            if (stepper == null) return;

            // Estaciones de la LÍNEA DEL TALLER (huella exterior documentada
            // junto a cada constante de ancla, + margen vertical hacia la
            // boca/vapor de cada una).
            stepper.RegistrarZonaInteres(88, BaseYDeEstacion(CrisolX) - 4, 125, BaseYDeEstacion(CrisolX) + 45); // Crisol: cámara+boca+brasero (huella 88..124) + hogar/vapor.
            stepper.RegistrarZonaInteres(143, BaseYDeEstacion(PrensaX) - 2, 174, BaseYDeEstacion(PrensaX) + 25); // Prensa (huella 143..173).
            stepper.RegistrarZonaInteres(258, BaseYDeEstacion(ColumnaX0) - 2, 281, BaseYDeEstacion(ColumnaX0) + 40); // Columna de Ensayo (huella exterior 258..280) + tanque.
            stepper.RegistrarZonaInteres(289, BaseYDeEstacion(BancoChispaX) - 2, 316, BaseYDeEstacion(BancoChispaX) + 25); // Banco de Chispa (huella 289..315).
            stepper.RegistrarZonaInteres(EnsayoPlintoX - 20, BaseYDeEstacion(EnsayoPlintoX) - 2, EnsayoPlintoX + 20, BaseYDeEstacion(EnsayoPlintoX) + 30); // Ensayo Maestro (sin huella exacta documentada aquí: margen generoso alrededor del ancla).
            stepper.RegistrarZonaInteres(AlambiqueX - 20, AlambiqueBaseY - 4, AlambiqueX + 20, AlambiqueBaseY + 40); // Alambique (comparte X con el Crisol, cota propia).

            // Las cubetas/pilas de fuentes -- rectángulos EXACTOS ya conocidos
            // por este archivo (interior real, no una estimación).
            stepper.RegistrarZonaInteres(BasinInteriorX0, BasinInteriorY0, BasinInteriorX1 + 1, BasinInteriorY1 + 1); // pila caliente.
            stepper.RegistrarZonaInteres(ChillTrayInteriorX0, ChillTrayInteriorY0, ChillTrayInteriorX1 + 1, ChillTrayY0 + ChillTrayHeight); // bandeja fría.
            stepper.RegistrarZonaInteres(PilaAguaX0, PilaBaseY, PilaLimoX0 + 6, PilaBaseY + 12); // las dos pilas del cuarto íntimo (agua+limo, adyacentes).
        }

        /// <summary>
        /// (playtest 27) LA ESTACIÓN DE FUENTES entera: las dos pilas grandes
        /// (14x9 exterior, interior 10x7 = 70 celdas cada una) y el MACHÓN DE
        /// PIEDRA entre ellas que sostiene el caño de limo más alto y a otra
        /// X. Ver el bloque de doc junto a <see cref="PilaAguaX0"/> para las
        /// cuentas de en qué columna cae cada chorro.
        /// </summary>
        private static void BuildPilasFuentes(CellGrid grid)
        {
            // (fix Cesar playtest 34) Las dos "U" de piedra ya NO se tallan
            // aquí a mano -- Game/Pila.cs::TallarEnPlano hace exactamente el
            // mismo DrawUShape + RegistrarObra de siempre (mismas medidas,
            // <see cref="PilaPlanes"/>), pero como método ESTÁTICO de la
            // clase dueña de la geometría, igual que Balda/Crisol/Prensa/etc.
            // -- así Game/Pila.cs puede reclamar el MISMO handle de obra al
            // instanciarse (ver Pila.Init/HallarObraExacta) en vez de que
            // este archivo registre uno y la instancia otro.
            for (int i = 0; i < PilaPlanes.Length; i++)
            {
                var p = PilaPlanes[i];
                Pila.TallarEnPlano(grid, p.X0, p.Y0, p.Ancho, p.Hondo, p.Muro);
            }

            // Los machones: de la losa al remate, macizos. El del limo sube
            // por ENTRE las dos pilas (215..217, con la del agua acabando en
            // 214 y la del limo empezando en 218) y el del agua (playtest 34)
            // cierra la isla por la izquierda (198..200): las CUATRO piezas se
            // tocan y se leen como UNA estación exenta en el centro de la
            // sala, no como cuatro objetos sueltos en el suelo.
            DrawSolidRect(grid, MensulaLimoX0, CuartoY0, MensulaLimoX1 - MensulaLimoX0 + 1,
                MensulaLimoTopY - CuartoY0 + 1, MaterialId.Stone);
            DrawSolidRect(grid, MensulaAguaX0, CuartoY0, MensulaAguaX1 - MensulaAguaX0 + 1,
                MensulaAguaTopY - CuartoY0 + 1, MaterialId.Stone);

            // (playtest 27) Registro anticincel de los machones -- ver
            // ObraDelTaller. Las dos pilas YA quedaron registradas dentro de
            // Pila.TallarEnPlano (fix Cesar playtest 34, ver el bucle de
            // arriba): duplicar su RegistrarObra aquí crearía un SEGUNDO
            // handle fantasma que Game/Pila.cs nunca reclamaría (el mismo
            // bug de handle huérfano que ya describe Game/Alambique.cs).
            RegistrarObra(MensulaLimoX0, CuartoY0, MensulaLimoX1, MensulaLimoTopY);
            RegistrarObra(MensulaAguaX0, CuartoY0, MensulaAguaX1, MensulaAguaTopY);
        }

        /// <summary>
        /// (CONTRATO_TERMICA.md §3b, ENCARGO I) LA ALCOBA FRÍA: suelo +
        /// muretes de contención donde vive la piedra gélida
        /// (Game/ChillStone.cs, archivo del encargo T -- aquí SOLO se talla
        /// el sitio, ver el bloque de constantes junto a
        /// <see cref="AlcobaFriaX0"/> para el porqué exacto del sitio y de
        /// "solo dos muretes, no una U completa"). Registrado como Obra
        /// (anticincel) igual que cualquier estación -- sin clase propia con
        /// su propio <c>TallarEnPlano</c> porque no hay ninguna: ChillStone
        /// no talla su propia mampostería (a diferencia de Crisol/Prensa/
        /// BancoChispa/ColumnaEnsayo/EnsayoMaestro/Pila), así que este
        /// archivo hace el trabajo entero, como ya hace con
        /// <see cref="BuildCuartoFloor"/>/<see cref="CarvePasilloTolva"/>.
        /// </summary>
        private static void BuildAlcobaFria(CellGrid grid)
        {
            int baseY = BaseYDeEstacion(AlcobaFriaX0); // 144 -- misma cota que Columna/Chispa, ver doc de las constantes.

            // Colchón bajo el suelo, con margen a los lados -- mismo
            // criterio visual que las 5 estaciones (AplanarPlataforma en
            // cada Game/*.cs), reescrito aquí en pequeño porque esta
            // "estación" no tiene clase propia: vive entera en el plano.
            int xC0 = AlcobaFriaX0 - AlcobaFriaMargen;
            int xC1 = AlcobaFriaX1 + AlcobaFriaMargen;
            for (int x = xC0; x <= xC1; x++)
                for (int y = baseY - AlcobaFriaProfundidad; y <= baseY; y++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Stone);

            // Muretes de contención de 1 celda a cada lado (no una "U"
            // completa -- ver decisión documentada junto a las constantes):
            // el interior entre ambos (AlcobaFriaX0+1..AlcobaFriaX1-1) queda
            // vacío, listo para recibir agua/hielo.
            for (int y = baseY + 1; y <= baseY + AlcobaFriaMuroAlto; y++)
            {
                if (CellGrid.InBounds(AlcobaFriaX0, y)) grid.SetCell(AlcobaFriaX0, y, MaterialId.Stone);
                if (CellGrid.InBounds(AlcobaFriaX1, y)) grid.SetCell(AlcobaFriaX1, y, MaterialId.Stone);
            }

            RegistrarObra(AlcobaFriaX0, baseY - AlcobaFriaProfundidad, AlcobaFriaX1, baseY + AlcobaFriaMuroAlto);
        }

        /// <summary>
        /// (CONTRATO_RONDA48.md §1a, D1) LA VETA DE TURBA -- el desatasco de
        /// Semilla Cero. Embute <c>MaterialId.MatDe(Universe.SemillaCeroBaseTurbaIdx,
        /// EstadoMateria.Polvo)</c> (turba, color real pardo de la tabla, ver
        /// <c>Universe.ConstruirIdentidadReal</c>) en el muro izquierdo del
        /// cuarto (ver el bloque de constantes junto a
        /// <see cref="VetaTurbaX0"/> para el porqué exacto del sitio).
        ///
        /// PRIMER PASO -- EL PILAR EXTRA: <c>DrawUShape</c> solo talla
        /// <see cref="WallThickness"/>=3 celdas de muro (80..82);
        /// <see cref="VetaTurbaAncho"/>=4 pide una columna más (83) para dar
        /// grosor 3-6 sin invadir la repisa "ala izquierda" (X0=84). Se
        /// maciza esa columna en TODA la altura del muro (mismo
        /// `CuartoY0..CuartoY1` que las otras dos) para que se lea como parte
        /// de la misma piedra, no como un parche pegado.
        ///
        /// SEGUNDO PASO -- LA FORMA: un paseo aleatorio determinista
        /// (<see cref="XorShift.FromCell"/>, tick constante 0 -- mismo patrón
        /// de ruido ESTÁTICO de nivel que la regla 42 de CLAUDE.md, salt
        /// propio <see cref="SalVetaTurba"/>) serpentea la ESPINA en X dentro
        /// del corredor mientras Y avanza; un radio local 1-3 alrededor de la
        /// espina más un filtro de borde (<see cref="VetaTurbaBordeSaltoPct"/>)
        /// da la silueta orgánica ("no un rectángulo", contrato §1a). Cada
        /// fila tiene además <see cref="VetaTurbaCuelloPct"/>% de probabilidad
        /// de ser un CUELLO (radio 0, solo la espina) -- LOS BOLSONES del
        /// contrato: la turba es Polvo y CAE, así que un corredor sin
        /// estrechamientos se derramaría entero de golpe en cuanto el
        /// jugador abriera un hueco encima; los cuellos trocean la veta en
        /// bolsones que caen por separado.
        ///
        /// Solo sustituye celdas que YA son <see cref="MaterialId.Stone"/>
        /// (nunca pinta sobre aire, nunca sobre la repisa/otra obra) y cuenta
        /// las celdas realmente talladas. (Integración pt48: la verificación
        /// "fuera de Unity reimplementando el hash" de la primera versión
        /// DIVERGIÓ del XorShift real -- prometía 359 celdas y el editor
        /// midió 76, porque además el corredor original caía en el aire. La
        /// fuente de verdad es el Assert de abajo más el log de arranque,
        /// leídos EN el editor -- regla 51: un assert que no se puede leer no
        /// protege nada, y una réplica del hash no es el hash.) El mínimo 250
        /// responde a la regla 44 (mejor sobrar que agotarse a mitad de
        /// partida); el máximo 500 evita convertir el muro en un silo.
        ///
        /// SOLO SEMILLA CERO: en caótico/multi el muro se queda macizo, sin
        /// veta -- una sustancia con identidad real fija
        /// (<see cref="Universe.TieneIdentidadReal"/>) no tiene sentido fuera
        /// de la seed congelada 777002.
        /// </summary>
        private static void BuildVetaTurba(CellGrid grid)
        {
            if (!AlkahestGameBootstrap.ModoSemillaCero) return;

            // (integración pt48) El "pilar extra" que aquí ensanchaba el muro
            // se RETIRÓ: ensanchaba una columna de AIRE (CuartoX0 es interior,
            // no muro) y dejaba piedra flotante dentro del cuarto. La veta
            // vive ahora en la roca maciza real (ver VetaTurbaX0/X1).

            byte turba = MaterialId.MatDe(Universe.SemillaCeroBaseTurbaIdx, EstadoMateria.Polvo);
            int spineX = (VetaTurbaX0 + VetaTurbaX1) / 2;
            int celdas = 0;

            for (int y = VetaTurbaY0; y <= VetaTurbaY1; y++)
            {
                var rngFila = XorShift.FromCell(0u, VetaTurbaX0, y, SalVetaTurba);

                int paso = rngFila.Next(3) - 1; // -1/0/+1: paseo aleatorio de la espina.
                spineX += paso;
                if (spineX < VetaTurbaX0 + 1) spineX = VetaTurbaX0 + 1;
                if (spineX > VetaTurbaX1 - 1) spineX = VetaTurbaX1 - 1;

                bool esCuello = rngFila.ChancePercent(VetaTurbaCuelloPct);
                int radio = esCuello ? 0 : 1 + rngFila.Next(3); // 1-3, salvo cuello (0).

                for (int x = VetaTurbaX0; x <= VetaTurbaX1; x++)
                {
                    int dx = x - spineX;
                    if (dx < 0) dx = -dx;
                    if (dx > radio) continue;

                    if (x != spineX)
                    {
                        // Borde deshilachado (nunca en la propia espina, que
                        // siempre queda sólida): generador propio por celda,
                        // salt+1 para no repetir la tirada de la fila.
                        var rngCelda = XorShift.FromCell(0u, x, y, SalVetaTurba + 1u);
                        if (rngCelda.ChancePercent(VetaTurbaBordeSaltoPct)) continue;
                    }

                    if (!CellGrid.InBounds(x, y) || grid.GetMat(x, y) != MaterialId.Stone) continue;
                    grid.SetCell(x, y, turba);
                    celdas++;
                }
            }

            // EL ASOMO (integración pt48): 3 celdas de turba perforan la cara
            // (VetaTurbaCaraX) a ras del piso del cuarto. Física medida: la
            // celda de la base tiene piedra debajo Y en diagonal-abajo (el
            // suelo del cuarto), así que NO se desliza; la de encima puede
            // deslizarse UNA celda al piso como migaja-señuelo. Es el cartel
            // natural de la veta ("aquí hay algo pardo") sin regalarla ni
            // enterrar máquinas -- ver el docblock de VetaTurbaX0/X1.
            for (int y = VetaTurbaY0; y <= VetaTurbaY0 + 2 && y <= VetaTurbaY1; y++)
            {
                if (CellGrid.InBounds(VetaTurbaCaraX, y) && grid.GetMat(VetaTurbaCaraX, y) == MaterialId.Stone)
                {
                    grid.SetCell(VetaTurbaCaraX, y, turba);
                    celdas++;
                }
            }

            // (a propósito, NUNCA RegistrarObra aquí) el rect de la veta NO se
            // registra como Obra del Taller: ese registro es lo que
            // Game/Cincel.cs consulta para BLOQUEAR el tallado sobre
            // mampostería de máquina (EsObraDelTaller). La veta es
            // exactamente lo contrario -- piedra normal, tallable desde el
            // arranque -- así que debe quedar FUERA de esa lista o el cincel
            // rebotaría contra ella (el mismo bug que ya reportó Cesar en el
            // playtest 26 para las estaciones, pero al revés: aquí el bug
            // sería no poder tallar nunca).

#if UNITY_EDITOR
            UnityEngine.Debug.Assert(celdas >= 250 && celdas <= 500,
                $"[ChaosAlchemy][SemillaCero] La veta de turba talló {celdas} celdas (banda esperada 250-500, contrato §1a) -- si el número real se sale de la banda, ajustar VetaTurbaAncho/Y0/Y1 o los porcentajes de VetaTurbaCuelloPct/VetaTurbaBordeSaltoPct.");
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log($"[ChaosAlchemy][SemillaCero] Veta de turba tallada: {celdas} celdas en x[{VetaTurbaX0}..{VetaTurbaX1}] y[{VetaTurbaY0}..{VetaTurbaY1}], muro izquierdo del cuarto (junto a CrisolX={CrisolX}).");
#endif
        }

        /// <summary>TODO el mundo, borde incluido: no hace falta un FillBorder aparte (como en BuildTestLevel) porque la cámara íntima (CuartoX0..X1/Y0..Y1, muy dentro de 0..767/0..287) nunca toca el borde real del mundo -- se queda macizo por construcción, sin una pasada extra.</summary>
        private static void FillWorldStone(CellGrid grid)
        {
            for (int y = 0; y < CellGrid.H; y++)
            {
                for (int x = 0; x < CellGrid.W; x++)
                {
                    grid.SetCell(x, y, MaterialId.Stone);
                }
            }
        }

        private static void ExcavateCuarto(CellGrid grid)
        {
            DrawSolidRect(grid, CuartoX0, CuartoY0, CuartoX1 - CuartoX0 + 1, CuartoY1 - CuartoY0 + 1, MaterialId.Empty);
        }

        private static void BuildCuna(CellGrid grid)
        {
            DrawUShape(grid, CunaX0, CuartoY0, CunaWidth, CunaHeight, WallThickness);
        }

        private static void BuildRepisa(CellGrid grid)
        {
            DrawSolidRect(grid, RepisaX0, RepisaY0, RepisaWidth, RepisaHeight, MaterialId.Stone);
        }

        /// <summary>
        /// (contrato §4.5) Suelo UNIFORME de la sala entera: las
        /// <see cref="WallThickness"/> filas de abajo del cuarto
        /// (`CuartoY0..CuartoY0+WallThickness-1`), a todo su ancho
        /// (`CuartoX0..CuartoX1`), macizas de Stone. Antes de esta ronda el
        /// suelo solo existía DEBAJO de las estructuras en U que lo tallaban
        /// ellas mismas (`BuildCuna`/`PlaceCharco`, vía `DrawUShape`); ahora
        /// que la sala aloja maquinaria que se auto-talla (Crisol/Prensa/
        /// BancoChispa, ver `Game/*.cs`) el suelo tiene que existir ANTES de
        /// que ellas lo pisen. La única pieza que lo EXIGE explícitamente es
        /// `Game/EnsayoMaestro.cs::TallarCubeta`, cuyo propio comentario dice
        /// "suelo ya es piedra maciza del cuarto, no hace falta tallar
        /// debajo" -- sin este método esa frase sería falsa y el plinto del
        /// Ensayo flotaría sobre aire. Se llama justo después de
        /// `ExcavateCuarto` (que vació TODA la sala a Empty, suelo incluido)
        /// y antes de cualquier estructura.
        /// </summary>
        private static void BuildCuartoFloor(CellGrid grid)
        {
            DrawSolidRect(grid, CuartoX0, CuartoY0, CuartoX1 - CuartoX0 + 1, WallThickness, MaterialId.Stone);
        }

        // (playtest 29) `BuildColumnaEnsayo` SE MUDÓ ENTERO a
        // `Game/ColumnaEnsayo.TallarEnPlano` -- ver el comentario junto a su
        // llamada en `BuildCuartoIntimo`. No es una idea descartada (regla 15
        // de CLAUDE.md no aplica: es el MISMO tallado, con la MISMA
        // aritmética, solo que ahora vive donde puede volver a invocarse por
        // instancia al Reposicionar) -- por eso no se deja un cadáver
        // comentado aquí, solo esta nota de dónde buscarlo.

        /// <summary>
        /// (contrato §4.5) El pasillo PRE-CARVADO del cuarto a la boca de la
        /// Tolva, 6 de alto (<see cref="PasilloTolvaAlto"/>), desde la pared
        /// derecha del cuarto (`CuartoX1+1`=358) hasta el borde de la boca ya
        /// tallada por `BuildDeliveryNiche` (`ChuteMouthX0`=392) -- un tramo
        /// de 35 celdas que hoy es piedra lisa sin tallar (ver el docblock de
        /// la clase, "23 celdas de piedra lisa" entre `CuartoX1` y
        /// `ChuteWallX0`=380).
        ///
        /// SE LLAMA DESPUÉS de `BuildDeliveryNiche`, a propósito: esa función
        /// pinta la "torre" del contrafuerte (`torreX0`=`ChuteWallX0`+4=384,
        /// hasta `ChuteMouthY1`=238) de Stone SOBRE lo que hubiera antes -- si
        /// este método corriera primero, la torre volvería a sellar el tramo
        /// x=384..391 del pasillo. Llamarlo después dibuja el hueco ENCIMA de
        /// la piedra ya puesta, sin que nada lo tape después (nada más pinta
        /// esa franja en `BuildCuartoIntimo`).
        /// </summary>
        private static void CarvePasilloTolva(CellGrid grid)
        {
            int x0 = CuartoX1 + 1;
            int x1 = ChuteMouthX0;
            DrawSolidRect(grid, x0, PasilloTolvaY0, x1 - x0 + 1, PasilloTolvaAlto, MaterialId.Empty);
        }

        // ===================================================================
        // (playtest 31, "AHORA TODO ES LINEAL") LA ARQUITECTURA DEL CUARTO
        // ===================================================================
        // Veredicto de Cesar sobre el taller del playtest 27-30: "ahora todo
        // es lineal". Tenía razón y el motivo es geométrico, no de gusto: el
        // suelo del cuarto es UNA losa perfectamente recta de 218 celdas
        // (BuildCuartoFloor) y las seis estaciones se posan encima en fila,
        // todas a la misma altura. Un plano así solo puede leerse como una
        // cinta transportadora.
        //
        // LO QUE NO SE PUEDE TOCAR (y por qué):
        //  · Las ANCLAS de las estaciones (CrisolX, PrensaX, ColumnaX0,
        //    BancoChispaX, EnsayoPlintoX, AlambiqueX): hay réplicas de red
        //    (Net/MaquinaSync.cs) y registros anticincel que dependen de
        //    ellas.
        //  · El SUELO BAJO CADA ESTACIÓN: todos los TallarEnPlano asumen
        //    `baseY = CuartoY0+2`. Un peldaño debajo de una estación le
        //    dejaría la cubeta enterrada o flotando.
        //
        // LO QUE SÍ: los TRAMOS ENTRE estaciones. Este método los descubre
        // solo -- recorre el ancho del cuarto preguntando a `ObraDelTaller`
        // (el registro que cada estación rellena al tallarse) qué columnas
        // están libres, y en cada hueco suficientemente ancho levanta:
        //   1) una TERRAZA de 1-3 filas con sus peldaños de entrada y salida
        //      (el suelo deja de ser una recta y pasa a tener cota);
        //   2) una PILASTRA colgante desde el techo con su ménsula, dejando
        //      libre la franja central por la que vuela el aprendiz -- lo que
        //      a la vista es un ARCO entre zona y zona;
        //   3) en el hueco más ancho, además, un PLINTO decorativo tallado.
        // Y aparte, el ARCO DE MEDIO PUNTO sobre la boca del pasillo a la
        // Tolva: la salida del taller merece un vano, no un agujero
        // rectangular.
        //
        // SEGURIDAD DEL FLUJO (regla 52, "lo que solo se ve jugando): las
        // dos pilas de las fuentes (PilaAguaX0..PilaLimoX0+13, x 141..171)
        // están REGISTRADAS como obra, así que este método nunca toca ni su
        // suelo ni sus muros, y el chorro del caño sigue cayendo en la misma
        // columna a la misma pila. Las terrazas empiezan a partir de
        // `AdornoMargen` celdas después del último muro de cualquier
        // estación, así que tampoco pueden tapar una boca ni una cubeta.
        //
        // (playtest 32, FIX real de "máquinas enterradas"/"suelo con forma
        // rara") Ese último párrafo era una promesa incumplida (regla 49 de
        // CLAUDE.md): `ColumnaOcupada`/`RectOcupado` leen `ObraDelTaller`, pero
        // las CINCO estaciones de la línea del taller (Crisol/Prensa/
        // ColumnaEnsayo/BancoChispa/EnsayoMaestro) no se registraban ahí hasta
        // que su instancia corría `Init` -- que pasa DESPUÉS de que este
        // método ya haya tallado las terrazas (ver
        // Game/AlkahestGameBootstrap.cs). Es decir: `AdornoMargen`(1) + el
        // margen implícito de `ColumnaOcupada`(1) SÍ sumaban los 2 celdas de
        // respeto que se querían, pero sobre un registro que en la práctica
        // estaba VACÍO para esas cinco estaciones -- el margen no protegía
        // nada. Arreglado en la RAÍZ (ver `HallarObraExacta` más arriba):
        // cada `TallarEnPlano` de las cinco ahora registra su rect exacto AQUÍ
        // MISMO, en frío, y la instancia lo RECLAMA en vez de duplicarlo
        // cuando nace. `AdornoMargen` se queda en 1 sin tocar -- el número
        // ya era correcto, lo que faltaba era que el registro existiera a
        // tiempo.
        // ===================================================================

        /// <summary>
        /// Celdas de respeto a cada lado de la huella de una estación antes
        /// de tallar adorno. (SEGUNDA PASADA, VISTO JUGANDO: era 3 y no salió
        /// NI UNA terraza. Motivo medido sobre el plano real: con el taller
        /// grande del playtest 27 las seis estaciones se comen prácticamente
        /// los 218 celdas de ancho de la sala -- los huecos entre ellas son de
        /// 4-10 celdas, no de 20. Con 3 de respeto a cada lado no quedaba
        /// nada que tallar. A 1, cada costura entre estaciones recibe su
        /// escalón, que es justo donde el ojo necesita el corte.)
        /// </summary>
        /// (playtest 33) 1 -&gt; **2**. Ahora que cada estación APLANA SU PROPIA
        /// PLATAFORMA (`AplanarPlataforma` en las cinco Game/*.cs, con
        /// `PlataformaMargen`=2 de holgura), el respeto de la terraza tiene
        /// que quedar ESTRICTAMENTE FUERA de esa holgura o las dos escrituras
        /// se pisan: 2 aquí + 1 implícito de `ColumnaOcupada` = 3 celdas
        /// desde el último muro, una más que la plataforma soberana. Y el
        /// número se puede permitir ahora porque la zonificación del 33
        /// abrió huecos de 8-20 celdas donde el 31 tenía 4-10.
        private const int AdornoMargen = 2;
        /// <summary>Ancho mínimo de hueco LIBRE que justifica un escalón (segunda pasada: 10 -&gt; 4, ver AdornoMargen).</summary>
        private const int AdornoHuecoMinimo = 4;
        /// <summary>(playtest 33) A partir de este ancho, el hueco deja de recibir un escalón decorativo y recibe una ESCALINATA -- ver <see cref="TallarTerraza"/>.</summary>
        private const int AdornoHuecoEscalinata = 12;
        /// <summary>(playtest 34) La columna del UMBRAL entre la banda que transforma y la alcoba que observa: el hueco que la contiene es el único que se gana la escalinata (ver <see cref="TallarTerraza"/>), y es la misma frontera que usa <see cref="BaseYDeEstacion"/> para dar la cota +6.</summary>
        private const int UmbralAlcobaX = 250;

        /// <summary>
        /// Columnas donde CUELGA una pilastra del techo, una por costura
        /// entre zonas del proceso. Son también las NERVADURAS (arcos
        /// fajones) que dividen la bóveda en tramos -- ver
        /// <see cref="TallarBoveda"/>: pilastra colgante y nervio de bóveda
        /// son la MISMA crujía vista abajo y arriba, no dos adornos sueltos.
        /// Van a mano y no derivadas de las anclas porque las anclas se
        /// mueven con la mudanza y esto es la SALA, que no se muda.
        ///
        /// (playtest 33) De 4 a **5**, recolocadas sobre las costuras nuevas:
        /// 176 fuentes|fuego · 224 fuego|fuerza · 272 fuerza|OBSERVACIÓN (la
        /// grande) · 308 dentro de la alcoba, entre columna y chispa (las
        /// abraza en vez de separarlas: el vano de 6 celdas queda arqueado) ·
        /// 343 observación|atrio del Maestro.
        /// </summary>
        /// (playtest 33) PÚBLICA: Game/WorkshopBackdrop.cs la lee para saber
        /// dónde caen los nervios de la bóveda (y por tanto la clave de cada
        /// tramo, que es lo que orienta el abanico de dovelas del fondo) y
        /// dónde cambiar el aparejo de la sillería. Antes que duplicar cinco
        /// números en otro archivo -- regla 39 y regla 47 de CLAUDE.md.
        public static readonly int[] PilastraColumnas = { 133, 185, 244, 284, 331 }; // (playtest 34) recolocadas a las costuras del PLANO CENTRAL: crisol|prensa (133), prensa|fuentes (185), fuentes|escalinata (244), columna|chispa (284), chispa|atrio (331). Todas caen en aire libre, ninguna sobre una huella.

        /// <summary>
        /// (playtest 33, "algunos pilares están bien pero puede haber MÁS
        /// COSAS") Caída de cada pilastra, en el MISMO orden que
        /// <see cref="PilastraColumnas"/>. Antes era una constante única (14
        /// para las cuatro) y por eso el techo se leía como una cornisa con
        /// cuatro dientes iguales: un ritmo regular es exactamente lo que el
        /// ojo deja de mirar. Con caídas distintas (10..22) el techo tiene
        /// CADENCIA -- y la más larga cae sobre el umbral de la observación,
        /// que es la costura que más queremos que se note.
        /// TODAS terminan en y&gt;=240 (`CuartoY1`-22), o sea muy por encima de
        /// la repisa más alta del taller (y=242 en <see cref="Repisas"/>): las
        /// dos familias no pueden tocarse, comprobado a mano rect a rect.
        /// </summary>
        /// (playtest 33, 2ª pasada) PÚBLICA por lo mismo que PilastraColumnas:
        /// Game/WorkshopBackdrop.cs viste cada pilastra con su sillar y necesita
        /// saber cuánto mide.
        public static readonly int[] PilastraCaidas = { 24, 17, 28, 19, 26 }; // (playtest 34) alargadas ~35%: con el cuarto 32 celdas más alto, las caídas del 33 se quedaban en muñones pegados al techo.

        // ===================================================================
        // (playtest 33) LAS REPISAS -- "un taller donde EXHIBIR lo que
        // encuentras"
        // ===================================================================
        // El encargo que más ilusión le hacía a Cesar, literal: *"Arriba del
        // horno hay una LÍNEA que me gustó mucho porque puedo APOYAR COSAS:
        // idealmente créale como BRACITOS INCLINADOS para que pueda sostener
        // más cosas, y en el techo puedo tener MÁS de estos lugares para ir
        // acomodando lo que encuentro"*.
        //
        // QUÉ ERA "LA LÍNEA" (identificado leyendo el plano, no adivinado):
        // el PLINTO del Alambique -- `Alambique.TallarEnPlano` pinta una losa
        // de piedra de 13 celdas (x 188..200) a `AlambiqueBaseY`=210, justo
        // encima de la boca del Crisol. Es lo único horizontal y flotante que
        // hay ahí arriba, y es lo que él usa de estante.
        //
        // QUÉ SE HACE: esa línea se PROLONGA con dos alas propias (178..187 y
        // 201..210, misma fila y=210), y el taller gana otras SIETE baldas
        // repartidas por las alturas nuevas. Todas son PIEDRA REAL de 2 filas
        // (no decoración): un polvo o un sólido con `caeSolido` vertido encima
        // se APOYA de verdad, porque tiene apoyo directo debajo -- ni siquiera
        // hace falta invocar la cohesión de la regla 7, que es la red de
        // seguridad para los voladizos, no para esto. La piedra JAMÁS cae
        // (regla 7), así que una balda no se puede desplomar con carga.
        //
        // POR QUÉ 2 FILAS Y NO 1: con una sola fila la balda se lee como una
        // raya (misma lección que `PilaMuroGrosor` 1->2 en el playtest 27) y,
        // peor, el canto no admite el bisel claro/oscuro con el que
        // SimRenderer dibuja la piedra -- o sea que ni siquiera parecería
        // tallada.
        //
        // LOS "BRACITOS INCLINADOS" son sprites, no piedra: los monta
        // Game/WorkshopBackdrop.cs leyendo esta misma tabla
        // (`MaquinariaSprites.MensulaInclinada` + filo de latón + sombra
        // proyectada). Deliberadamente NO se tallan en la grilla: una ménsula
        // diagonal de piedra real sería una trampa para los polvos que caen
        // por su lado, y el jugador tiene cincel -- vería una escalera rara
        // que puede picar. La regla del proyecto es que la piedra dice qué
        // SOSTIENE y el sprite dice qué PARECE.
        //
        // ALCANZABLES VOLANDO: la más alta está a 72 celdas del suelo del
        // taller y el aprendiz vuela sin límite de altura; el encuadre de
        // cámara (90 celdas de alto, ver SimRenderer.CuartoIntimoZoomFactor)
        // cubre de suelo a bóveda sin necesidad de alejar el zoom.
        // ===================================================================
        /// <summary>Una balda: piedra maciza en las filas `Y-1` y `Y`, de `X0` a `X1` inclusive.</summary>
        public struct RepisaPlan { public int X0, X1, Y; }

        /// <summary>
        /// (fix Cesar playtest 33: SISTEMA DE BALDAS COMO CONSTRUCCIÓN) RETIRADO
        /// a propósito -- se queda declarado y VACÍO (regla 15 de CLAUDE.md,
        /// documentar en el código lo que se retira) porque
        /// Game/WorkshopBackdrop.cs (prohibido en este encargo) itera este
        /// array para vestir cada balda con ménsulas triangulares y filo de
        /// latón: un array vacío convierte ese bucle en un no-op seguro sin
        /// tocar ese archivo. El catálogo REAL de posiciones/alturas vive
        /// ahora en <see cref="_galeriasOriginales"/> (privado, la misma
        /// tabla de coordenadas de siempre) y se trocea en segmentos más
        /// cortos y de UNA sola fila -- ver <see cref="BaldaPlanes"/> y
        /// Game/Balda.cs, el reemplazo completo de este sistema: Cesar,
        /// literal, *"las baldas deberían ser más angostas... las ménsulas
        /// triangulares deberían ser unos cuadraditos pequeños... que sea el
        /// nuevo sistema de construcción"*.
        /// </summary>
        public static readonly RepisaPlan[] Repisas = new RepisaPlan[0];

        /// <summary>
        /// (fix Cesar playtest 33) El mismo catálogo de galerías de siempre
        /// (posiciones/alturas sin cambiar, "SEGUNDA PASADA, VISTO JUGANDO"
        /// de la ronda anterior: menos piezas, más largas, alineadas con la
        /// crujía) -- ahora PRIVADO, es la materia prima que
        /// <see cref="ComputeBaldaPlanes"/> trocea en baldas de UNA fila y de
        /// hasta <see cref="BaldaLargoMax"/> celdas. Ya NO se talla
        /// directamente: cada entrada es la "media crujía" original que se
        /// reparte entre 1-2 baldas más cortas con un hueco entre medias.
        ///
        /// (playtest 54, PARIDAD SOLO/MULTI + "REDUCIR LOS SOPORTES A LA
        /// MITAD") Cesar, comparando capturas multi vs. un jugador de
        /// Semilla Cero 0: "reducir los soportes movibles como a la mitad
        /// para que no esté tan cargado, y que ambas versiones estén
        /// IGUALES". INVESTIGACIÓN DE LA DISPARIDAD (regla 43 de CLAUDE.md,
        /// conteo exacto): esta tabla y `BuildCuartoIntimo` (que la talla
        /// vía `TallarRepisas`) NO tienen ninguna rama por `SimSync.EnEscena`
        /// -- `AlkahestSim.CrearMundoInterno` llama a
        /// `SimLevelBuilder.BuildCuartoIntimo` exactamente una vez, IGUAL en
        /// el proceso de un jugador y en el del anfitrión del multi (grep
        /// confirmado: ningún `if (SimSync...)` en este archivo más allá de
        /// los gates de `ModoSemillaCero`, que valen igual en los dos
        /// modos). Los DOS caminos de spawn de las instancias reales
        /// (`AlkahestGameBootstrap.TrySpawn` para un jugador,
        /// `.TrySpawnRed` para el anfitrión) llaman al MISMO
        /// `Mudanza.Init(_sim)` (línea 878 y ~vía `Net/AprendizNet.cs::Cablear`
        /// respectivamente), que llama al MISMO
        /// `SpawnBaldasYAnclajesSiCorresponde()` (`Game/Mudanza.cs`), que
        /// llama a los MISMOS `Balda.SpawnTodas`/`Anclaje.SpawnDeposito`/
        /// `Pila.SpawnTodas` -- el plano y el código de spawn YA ESTÁN
        /// UNIFICADOS, sin ninguna decisión de diseño que los separe.
        ///
        /// CAUSA REAL DE LA DISPARIDAD OBSERVADA (archivos AJENOS a este
        /// encargo, fuera de mi lista de archivos exclusivos -- documentado
        /// aquí para el director, NO corregido en esta ronda):
        /// `Game/Balda.cs:144` (`private static bool _todasCreadas`),
        /// `Game/Anclaje.cs:195` (`private static bool _yaCreado`) y
        /// `Game/Pila.cs:190` (`private static bool _todasCreadas`) son
        /// banderas ESTÁTICAS DE PROCESO sin ningún reset (ni en
        /// `DayCycle.RestartRun`, ni al cambiar de escena, ni en ningún
        /// `OnNetworkDespawn`). En una build de reparto (o en una sesión de
        /// Editor que navegue Título->Lab->Título->Multi SIN detener el
        /// Play, ver regla 6b de CLAUDE.md sobre por qué el sandbox insiste
        /// en volver a un clon fresco) el dominio NO se recarga entre
        /// escenas: quien entra SEGUNDO a un modo dentro del MISMO proceso
        /// encuentra la bandera ya en `true` y `SpawnTodas`/`SpawnDeposito`
        /// retornan sin tallar nada -- ninguna balda, ningún anclaje,
        /// ninguna pila para ESE modo, en ESA sesión. Encaja EXACTAMENTE
        /// con el reporte ("1 = multi... 2 = un jugador", el multi
        /// decorado, el solo pelado): jugar multi primero deja las tres
        /// banderas en `true`, y la sesión de un jugador que sigue en el
        /// MISMO proceso las encuentra ya gastadas. Esto NO es una
        /// diferencia de DISEÑO entre modos (que es lo que este archivo
        /// controla) -- es una fuga de estado de proceso en tres archivos
        /// que no están en mi lista de archivos exclusivos de esta ronda
        /// (`Sim/SimLevelBuilder.cs`, `Game/DeliveryChute.cs`,
        /// `Game/AlkahestGameBootstrap.cs`, `Game/MaquinariaSprites.cs`,
        /// `Game/StorageRack.cs`). DEUDA PRIORITARIA para quien posea
        /// `Game/Balda.cs`/`Game/Anclaje.cs`/`Game/Pila.cs`: resetear las
        /// tres banderas en el mismo sitio que ya limpia
        /// `MachineFocus.Limpiar()` (la primera línea de
        /// `AlkahestGameBootstrap.TrySpawn`/`TrySpawnRed`, ver ese archivo),
        /// que es justo donde el proyecto ya reconoce este problema para
        /// OTRO registro estático.
        ///
        /// LO QUE SÍ HACE ESTA RONDA, dentro de mis archivos: LA MITAD de
        /// las galerías (5 de 10, alternas por índice -- ni un tramo
        /// contiguo del cuarto se queda sin ninguna, criterio de
        /// composición "reparte, no amontones") se retiran de la tabla.
        /// CONTEO EXACTO (regla 43): 10 galerías -> 17 `BaldaPlan`
        /// troceados (<see cref="ComputeBaldaPlanes"/>, `BaldaLargoMax`=12,
        /// `BaldaHueco`=3) ANTES; 5 galerías -> 8 troceados DESPUÉS (-53%).
        /// Se retiraron (X0..X1, filas trozadas): 111..120 (1), 144..170
        /// (2), 200..228 (2, la galería sobre el estante de redomas --
        /// coherente con que el estante deja de hacerse visible en Semilla
        /// Cero, ver AlkahestGameBootstrap.SpawnStorageRack), 258..280 (2),
        /// 318..344 (2) = 9 troceados retirados. Se conservan: 84..93 (1),
        /// 128..139 (1), 176..192 (2), 234..254 (2), 290..314 (2) = 8.
        /// Como el plano es la ÚNICA fuente de esta geometría (ver el
        /// párrafo de arriba), el recorte se aplica IGUAL en un jugador y en
        /// el anfitrión del multi sin ninguna rama nueva -- una vez
        /// corregida la fuga de arriba, "ambas versiones estarán IGUALES"
        /// de verdad, no solo en el plano sino en lo que el jugador ve.
        /// </summary>
        private static readonly RepisaPlan[] _galeriasOriginales =
        {
            // (playtest 34) REMAPEADAS AL PLANO CENTRAL. Dos cosas cambian a
            // la vez y las dos son obligatorias: (a) las X, porque las zonas
            // se reordenaron (transformar a la izquierda, fuentes al centro,
            // observar a la derecha) y una galería sobre el sitio viejo caería
            // hoy sobre otra máquina; (b) las Y, que ERAN LITERALES atados al
            // `CuartoY0` viejo (168) y ahora se escriben como `CuartoY0 + N`
            // con EXACTAMENTE el mismo desfase que tenían -- 228 era
            // CuartoY0+60, 210 era +42, etc. Así las baldas conservan su
            // altura relativa sobre la maquinaria (que es lo que las hace
            // alcanzables y útiles) en vez de quedarse clavadas a una cota
            // absoluta que el cuarto ya no usa (regla 47 de CLAUDE.md).
            //
            // (playtest 54) MITAD RETIRADA -- ver el docblock de arriba para
            // el conteo exacto y el criterio (alternas por índice). Las
            // cinco líneas retiradas NO se borran del historial: quedan
            // comentadas, en su sitio original, para que quien las busque
            // por playtest 34 las encuentre.
            new RepisaPlan { X0 =  84, X1 =  93, Y = CuartoY0 + 42 }, // ALA IZQUIERDA de "la línea" del horno (flanquea el plinto del Alambique).
            //  RETIRADA (playtest 54): new RepisaPlan { X0 = 111, X1 = 120, Y = CuartoY0 + 42 }, // ALA DERECHA de "la línea" del horno.
            new RepisaPlan { X0 = 128, X1 = 139, Y = CuartoY0 + 54 }, // paso crisol -> prensa (corta: el hueco solo da para eso).
            //  RETIRADA (playtest 54): new RepisaPlan { X0 = 144, X1 = 170, Y = CuartoY0 + 72 }, // galería alta sobre la Prensa.
            new RepisaPlan { X0 = 176, X1 = 192, Y = CuartoY0 + 46 }, // el umbral donde nace el aprendiz: una balda BAJA, a mano.
            //  RETIRADA (playtest 54): new RepisaPlan { X0 = 200, X1 = 228, Y = CuartoY0 + 60 }, // galería del CENTRO, sobre el estante de redomas (que deja de verse en Semilla Cero) y el depósito de anclajes.
            new RepisaPlan { X0 = 234, X1 = 254, Y = CuartoY0 + 52 }, // sobre la escalinata: el descansillo entre transformar y observar.
            //  RETIRADA (playtest 54): new RepisaPlan { X0 = 258, X1 = 280, Y = CuartoY0 + 62 }, // galería de la alcoba, sobre la Columna.
            new RepisaPlan { X0 = 290, X1 = 314, Y = CuartoY0 + 76 }, // galería alta de la alcoba, sobre el Banco de Chispa.
            //  RETIRADA (playtest 54): new RepisaPlan { X0 = 318, X1 = 344, Y = CuartoY0 + 60 }, // galería del atrio del Maestro.
        };

        /// <summary>Una balda ya troceada: UNA fila maciza (Y), de X0 a X1 inclusive -- ver <see cref="BaldaPlanes"/>.</summary>
        public struct BaldaPlan { public int X0, X1, Y; }

        /// <summary>Longitud máxima de una balda, en celdas (pedido literal del encargo: "más angostas").</summary>
        public const int BaldaLargoMax = 12;
        /// <summary>Hueco entre dos baldas que salen de la misma galería original.</summary>
        public const int BaldaHueco = 3;

        /// <summary>
        /// (fix Cesar playtest 33) El catálogo REAL que Game/Balda.cs talla:
        /// cada entrada de <see cref="_galeriasOriginales"/> se divide en 1-2
        /// segmentos de como mucho <see cref="BaldaLargoMax"/> celdas, con
        /// <see cref="BaldaHueco"/> celdas de aire entre segmentos de la
        /// misma galería -- calculado UNA vez, aritmética entera pura (este
        /// archivo no usa UnityEngine, ver la cabecera: nada de Mathf).
        /// PÚBLICO: Sim/SimLevelBuilder.BuildCuartoIntimo llama a
        /// Game/Balda.cs::TallarEnPlano por cada entrada (ver
        /// <see cref="BuildCuartoIntimo"/>), y el mismo array alimenta el
        /// spawn de las instancias reales (ver Game/Mudanza.cs).
        /// </summary>
        public static readonly BaldaPlan[] BaldaPlanes = ComputeBaldaPlanes();

        private static BaldaPlan[] ComputeBaldaPlanes()
        {
            int total = 0;
            for (int i = 0; i < _galeriasOriginales.Length; i++)
                total += SegmentosParaAncho(_galeriasOriginales[i].X1 - _galeriasOriginales[i].X0 + 1);

            var result = new BaldaPlan[total];
            int idx = 0;
            for (int i = 0; i < _galeriasOriginales.Length; i++)
            {
                var g = _galeriasOriginales[i];
                int ancho = g.X1 - g.X0 + 1;
                int n = SegmentosParaAncho(ancho);

                // Espacio útil para las N baldas una vez descontados los
                // (n-1) huecos entre ellas; el resto (si `ancho` no es
                // múltiplo exacto) se reparte UNA celda extra a los primeros
                // segmentos, para que ninguna balda quede más de una celda
                // más corta que otra de la misma galería.
                int anchoUtil = ancho - (n - 1) * BaldaHueco;
                if (anchoUtil < n) anchoUtil = n; // salvaguarda: nunca menos de 1 celda por segmento.
                int largoBase = anchoUtil / n;
                int resto = anchoUtil % n;

                int cursor = g.X0;
                for (int s = 0; s < n; s++)
                {
                    int largo = largoBase + (s < resto ? 1 : 0);
                    int x0 = cursor;
                    int x1 = x0 + largo - 1;
                    result[idx++] = new BaldaPlan { X0 = x0, X1 = x1, Y = g.Y };
                    cursor = x1 + 1 + BaldaHueco;
                }
            }
            return result;
        }

        /// <summary>Cuántos segmentos de como mucho <see cref="BaldaLargoMax"/> celdas hacen falta para trocear una galería de `ancho` celdas (contando ya el hueco entre segmentos) -- división entera hacia arriba, sin Mathf.</summary>
        private static int SegmentosParaAncho(int ancho)
        {
            int paso = BaldaLargoMax + BaldaHueco;
            int n = (ancho + paso - 1) / paso;
            return n < 1 ? 1 : n;
        }

        // ===================================================================
        // (playtest 33) LA BÓVEDA POR TRAMOS
        // ===================================================================
        // Cesar: *"el techo deja de ser plano... aquí puedes ensayar más
        // texturas en vez de solo techo plano"*. `ExcavateCuarto` deja el
        // techo como una recta perfecta en `CuartoY1`; este método le talla
        // ENCIMA un casquete por cada crujía (los tramos que definen
        // <see cref="PilastraColumnas"/> más las dos paredes), dejando intacto
        // el nervio de piedra sobre cada pilastra -- que es exactamente lo que
        // es un arco fajón: la piedra que NO se vació entre dos bóvedas.
        //
        // El perfil es una PARÁBOLA entera (`4*i*(n-1-i)/(n-1)²`), no un
        // círculo: este archivo no tiene `using UnityEngine` a propósito (ver
        // la cabecera) y una raíz cuadrada por columna solo para redondear
        // medio téxel no lo justifica -- a esta escala (35-48 celdas de vano,
        // 6-12 de flecha) una parábola y un arco rebajado son
        // indistinguibles en pantalla.
        //
        // ALTURA POR TRAMO (la "flecha"), en el mismo orden que los tramos de
        // izquierda a derecha: la crujía de la ALCOBA DE OBSERVACIÓN se lleva
        // las dos más altas (12), porque el objetivo de diseño de la ronda es
        // que los instrumentos tengan su propio volumen -- se entra ahí y el
        // techo se abre. La del atrio del Maestro va a 9 (solemne pero más
        // baja: es un vestíbulo, no la nave). Máximo absoluto: y=262+12=274,
        // con 13 filas de roca hasta el borde del mundo (287).
        // ===================================================================
        private static readonly int[] BovedaFlechas = { 7, 9, 10, 13, 13, 10 }; // (playtest 34) +1 en todas: la sala es más ancha y más alta, y una bóveda que se descubre volando merece un punto más de flecha. Máximo absoluto: 262+13=275, más el pozo de claraboya (6) = 281, con 6 filas de roca hasta el borde del mundo (287).
        /// <summary>Media anchura del NERVIO (arco fajón) que se deja sin vaciar sobre cada pilastra.</summary>
        private const int BovedaNervio = 3; // 5 columnas de nervio (cx-2..cx+2): tan ancho como la pilastra con su ménsula, para que el arco fajón y la pilastra sean la MISMA pieza vista arriba y abajo.

        /// <summary>
        /// (playtest 33) Columnas donde una CLARABOYA FALSA perfora la clave
        /// de la bóveda: un pozo ciego de <see cref="ClaraboyaAncho"/> celdas
        /// que sube <see cref="ClaraboyaAlto"/> más y NO llega a la
        /// superficie (el mundo sigue siendo roca maciza por encima). Es
        /// falsa a propósito: el taller está excavado en la montaña y una
        /// salida real al cielo cambiaría el sitio, no solo su luz. Lo que la
        /// hace leerse como tragaluz es la LUZ FRÍA que Game/WorkshopBackdrop
        /// deja caer por ella -- por eso esta tabla es pública.
        /// Sitios: sobre las pilas de las fuentes (el agua devuelve el
        /// reflejo), y las dos de la alcoba de observación (luz fría sobre
        /// los instrumentos: la observación tiene su propia temperatura de
        /// color, opuesta a la fragua).
        /// </summary>
        public static readonly int[] ClaraboyaColumnas = { 214, 268, 302 }; // (playtest 34) una sobre la ISLA DE FUENTES (el agua devuelve el reflejo) y dos sobre la alcoba de observación. Comprobadas contra PilastraColumnas: ninguna cae sobre un nervio.
        public const int ClaraboyaAncho = 5;
        private const int ClaraboyaAlto = 6; // (playtest 34) 8 -> 6: con las flechas de bóveda subidas a 13, el pozo tenía que ceder dos filas para no besar el borde del mundo.

        /// <summary>
        /// (ronda 56, LA VIDA ÚTIL DE LO DESCUBIERTO, CONTRATO_RONDA56.md
        /// §2b) Registra en <see cref="ObraDelTaller"/> el rect EXACTO que
        /// <see cref="Crisol.TallarEnPlano"/> tallará más tarde en
        /// <see cref="MuflaX"/>/<see cref="MuflaBaseY"/> cuando el jugador
        /// complete el pedido de obra ("obra_mufla", evento contratado en
        /// §3) -- SIN tallar nada todavía (regla del contrato: "en el
        /// génesis es solo suelo/piedra normal"). Corre en el génesis, ANTES
        /// de <see cref="AdornarCuarto"/>, para que esa pasada de
        /// decoración vea el sitio como "ocupado" y no plante una terraza o
        /// una pilastra encima -- MISMO criterio que usa el propio Crisol
        /// para su instancia real (su `TallarEnPlano` registra su huella
        /// ANTES de que exista la instancia, ver el docblock de
        /// <c>Crisol._handleObra</c>).
        ///
        /// LA ARITMÉTICA ES UNA COPIA DELIBERADA de
        /// <c>Crisol.Calcular</c> (privado, no reutilizable desde aquí) --
        /// usa SOLO las constantes PÚBLICAS de Game/Crisol.cs
        /// (CamaraAncho/MuroGrosor/BocaVuelo/BraseroAncho/
        /// BraseroSeparacion/BocaFilas/HogarFilas), nunca un número mágico
        /// propio, así que si esas medidas cambian algún día (un reajuste de
        /// Crisol.cs, archivo ajeno a este encargo) este rect se entera
        /// solo. Cuando <see cref="Crisol.Init"/> corra de verdad sobre esta
        /// instancia (al completarse la obra), su propio
        /// <see cref="HallarObraExacta"/> encontrará y RECLAMARÁ este mismo
        /// handle en vez de crear uno huérfano -- el mismo mecanismo,
        /// documentado, que ya usa el Crisol original entre su
        /// `TallarEnPlano` (génesis) y su `Init` (spawn tardío, aquí:
        /// mid-partida).
        /// </summary>
        private static void ReservarSitioMufla()
        {
            int camX0 = MuflaX - Crisol.CamaraAncho / 2;
            int camX1 = camX0 + Crisol.CamaraAncho - 1;
            int camY0 = MuflaBaseY + 1;
            int camY1 = camY0 + Crisol.CamaraAlto - 1;

            int bocaY1 = camY1 + Crisol.BocaFilas; // = camY1 + 1 (bocaY0) + BocaFilas - 1.

            int braX0 = camX1 + Crisol.MuroGrosor + Crisol.BraseroSeparacion + Crisol.MuroGrosor;
            int braX1 = braX0 + Crisol.BraseroAncho - 1;

            int outX0 = camX0 - Crisol.MuroGrosor - Crisol.BocaVuelo;
            int outX1 = braX1 + Crisol.MuroGrosor;
            int outY0 = MuflaBaseY - Crisol.HogarFilas; // mismo -HogarFilas que Crisol.TallarEnPlano pasa a RegistrarObra.
            int outY1 = bocaY1 + 1;

            RegistrarObra(outX0, outY0, outX1, outY1);
        }

        private static void AdornarCuarto(CellGrid grid)
        {
            int suelo = CuartoY0 + WallThickness - 1; // 170: última fila maciza de la losa = la cota "cero" del taller.

            int x = CuartoX0 + 1;
            while (x <= CuartoX1)
            {
                if (ColumnaOcupada(x)) { x++; continue; }

                int inicio = x;
                while (x <= CuartoX1 && !ColumnaOcupada(x)) x++;
                int fin = x - 1;

                int h0 = inicio + AdornoMargen;
                int h1 = fin - AdornoMargen;
                if (h1 - h0 + 1 < AdornoHuecoMinimo) continue;

                TallarTerraza(grid, h0, h1, suelo);
            }

            for (int i = 0; i < PilastraColumnas.Length; i++)
            {
                int cx = PilastraColumnas[i];
                int caida = i < PilastraCaidas.Length ? PilastraCaidas[i] : PilastraCaida;
                if (cx <= CuartoX0 + 2 || cx >= CuartoX1 - 2) continue;
                if (RectOcupado(cx - PilastraAncho / 2 - 1, CuartoY1 - caida, cx + PilastraAncho / 2 + 1, CuartoY1)) continue;
                TallarPilastra(grid, cx, caida);
            }

            TallarArcoPasillo(grid);
        }

        /// <summary>
        /// (playtest 33) Vacía el casquete de cada crujía POR ENCIMA de
        /// <see cref="CuartoY1"/> y perfora las claraboyas ciegas. Se llama
        /// justo después de <see cref="ExcavateCuarto"/> y ANTES de cualquier
        /// estación: solo QUITA piedra, y solo por encima del techo recto, así
        /// que no puede tocar ni una plataforma soberana (la más alta, la del
        /// Ensayo, remata en y=199) ni el registro de <see cref="ObraDelTaller"/>.
        /// </summary>
        private static void TallarBoveda(CellGrid grid)
        {
            // Los bordes de tramo: pared izquierda, las pilastras, pared derecha.
            int tramos = PilastraColumnas.Length + 1;
            for (int t = 0; t < tramos; t++)
            {
                int bordeIzq = t == 0 ? CuartoX0 : PilastraColumnas[t - 1] + BovedaNervio;
                int bordeDer = t == tramos - 1 ? CuartoX1 : PilastraColumnas[t] - BovedaNervio;
                int n = bordeDer - bordeIzq + 1;
                if (n < 6) continue;

                int flecha = t < BovedaFlechas.Length ? BovedaFlechas[t] : 8;
                int den = (n - 1) * (n - 1);
                for (int i = 0; i < n; i++)
                {
                    int alto = flecha * (4 * i * (n - 1 - i)) / den;
                    for (int k = 1; k <= alto; k++)
                    {
                        int y = CuartoY1 + k;
                        if (CellGrid.InBounds(bordeIzq + i, y)) grid.SetCell(bordeIzq + i, y, MaterialId.Empty);
                    }
                }
            }

            // Las claraboyas: un pozo ciego desde la clave del tramo hacia
            // arriba. Se talla DESPUÉS del casquete para poder arrancar desde
            // la piedra que el casquete acaba de dejar (buscando hacia arriba
            // la primera fila sólida: así el pozo nace pegado a la clave y no
            // flotando dentro del hueco).
            for (int i = 0; i < ClaraboyaColumnas.Length; i++)
            {
                int cx = ClaraboyaColumnas[i];
                int yArranque = CuartoY1;
                while (yArranque < CellGrid.H - 2 && grid.GetMat(cx, yArranque + 1) == MaterialId.Empty) yArranque++;

                int x0 = cx - ClaraboyaAncho / 2;
                int x1 = x0 + ClaraboyaAncho - 1;
                for (int y = yArranque + 1; y <= yArranque + ClaraboyaAlto; y++)
                {
                    if (y >= CellGrid.H - 3) break; // nunca se asoma al borde del mundo: es una claraboya FALSA.
                    for (int px = x0; px <= x1; px++)
                        if (CellGrid.InBounds(px, y)) grid.SetCell(px, y, MaterialId.Empty);
                }
            }
        }

        /// <summary>
        /// (playtest 33) Talla las baldas de <see cref="Repisas"/> y las
        /// registra como obra del taller (el cincel no se las lleva).
        ///
        /// SE LLAMA AL FINAL, DESPUÉS de <see cref="AdornarCuarto"/>, y eso es
        /// deliberado: <see cref="ColumnaOcupada"/> mira solo la X de cada
        /// rect registrado (no su Y), así que una balda a 60 celdas de altura
        /// registrada ANTES prohibiría tallar terraza en toda esa columna del
        /// suelo -- las repisas se habrían comido la mitad de los escalones
        /// sin que nada avisara. Registrarlas después mantiene las dos cosas:
        /// terrazas donde tocan y baldas protegidas del cincel.
        /// </summary>
        /// <summary>
        /// (fix Cesar playtest 33) Ya NO talla directamente: cada
        /// <see cref="BaldaPlanes"/> pasa por Game/Balda.cs::TallarEnPlano
        /// (misma disciplina que ColumnaEnsayo/BancoChispa/EnsayoMaestro,
        /// regla 47 -- el tallado vive en la clase que también sabe
        /// reponerlo al mover), que ya registra su propia obra anticincel.
        /// El nombre del método se conserva (llamado desde
        /// <see cref="BuildCuartoIntimo"/>) para no tocar ese call site.
        /// </summary>
        private static void TallarRepisas(CellGrid grid)
        {
            for (int i = 0; i < BaldaPlanes.Length; i++)
            {
                var b = BaldaPlanes[i];
                Balda.TallarEnPlano(grid, b.X0, b.X1, b.Y);
            }
        }

        /// <summary>¿Pasa alguna huella de estación por la columna `x`? (Bucle plano sobre ~10 rects: se ejecuta 218 veces UNA vez, en la construcción del nivel.)</summary>
        private static bool ColumnaOcupada(int x)
        {
            for (int i = 0; i < ObraDelTaller.Count; i++)
            {
                var r = ObraDelTaller[i];
                if (x >= r.X0 - 1 && x <= r.X1 + 1) return true;
            }
            return false;
        }

        /// <summary>¿Solapa el rect dado con alguna obra ya registrada? Para las pilastras, que viven ARRIBA: la comprobación por columna sola las prohibiría en sitios donde a 14 celdas del techo no hay absolutamente nada.</summary>
        private static bool RectOcupado(int x0, int y0, int x1, int y1)
        {
            for (int i = 0; i < ObraDelTaller.Count; i++)
            {
                var r = ObraDelTaller[i];
                if (x1 >= r.X0 && x0 <= r.X1 && y1 >= r.Y0 && y0 <= r.Y1) return true;
            }
            return false;
        }

        /// <summary>
        /// Terraza tallada entre dos estaciones: dos peldaños de subida, una
        /// meseta 2-3 filas por encima de la cota general, y dos de bajada.
        /// La altura se sortea con un hash del propio hueco (determinista: el
        /// mismo plano en todas las partidas y en todas las réplicas de red;
        /// aquí NO puede entrar UnityEngine.Random, regla de oro del
        /// proyecto).
        /// </summary>
        private static void TallarTerraza(CellGrid grid, int x0, int x1, int sueloY)
        {
            int ancho = x1 - x0 + 1;
            uint h = (uint)(x0 * 73856093) ^ 0x9E3779B9u;
            h ^= h >> 13; h *= 0x5bd1e995u; h ^= h >> 15;
            int alto = 2 + (int)(h % 3u); // 2, 3 o 4 filas de desnivel: dos costuras seguidas nunca miden lo mismo.
            // (playtest 33) LA ESCALINATA: en un hueco ancho -- y con la
            // zonificación nueva el único que llega es el UMBRAL de 20 celdas
            // entre la Prensa y la alcoba de observación -- el desnivel sube a
            // 6 filas. No es decoración: es el corte que Cesar pidió entre las
            // máquinas que TRANSFORMAN y las que OBSERVAN. Un escalón de 3 se
            // cruza sin enterarse; uno de 6, con la cota de la alcoba a media
            // altura de la Prensa, obliga a SUBIR para ir a mirar.
            //
            // (playtest 34) LA ESCALINATA DEJA DE SER "CUALQUIER HUECO ANCHO"
            // Y PASA A SER UN SITIO. Con el plano central, el cuarto tiene
            // CUATRO huecos de 18-32 celdas (crisol|prensa, el umbral donde
            // nace el aprendiz, transformar|observar y el atrio) -- con la
            // condición vieja los cuatro se habrían levantado 6 filas y el
            // suelo del taller habría quedado como una sierra, que es
            // exactamente lo contrario de "que respire". Solo se gana la
            // escalinata el hueco que CONTIENE <see cref="UmbralAlcobaX"/>: el
            // corte entre las máquinas que transforman y las que observan,
            // que es el único desnivel que la cota por zona
            // (<see cref="BaseYDeEstacion"/>) necesita de verdad. Los otros
            // tres se quedan con su terraza de 2-4 filas de siempre.
            bool escalinata = ancho >= AdornoHuecoEscalinata && x0 <= UmbralAlcobaX && x1 >= UmbralAlcobaX;

            int peldano = ancho / 6; // ancho de cada peldaño...
            if (peldano < 1) peldano = 1;
            else if (peldano > 4) peldano = 4;

            if (escalinata)
            {
                // El perfil de abajo sube UN nivel por cada `peldano`
                // columnas desde CADA borde, así que la cota máxima que
                // alcanza de verdad es `(ancho/2)/peldano`, NO `alto`: con
                // ancho 14 y peldaño 2 se quedaba en 3 por mucho que aquí
                // pidiéramos 6. (Medido sobre el hueco real de 14 celdas que
                // deja la zonificación del 33 entre Prensa y Columna, no
                // supuesto -- regla 39.) Para que la escalinata SÍ llegue a
                // su cota, el peldaño baja a 1 y la cota se acota a lo que
                // cabe: escalones de una celda, empinados, que es justo lo
                // que hace que subir a la alcoba de observación se NOTE.
                peldano = 1;
                int cabe = (ancho - 2) / 2;
                alto = cabe < 6 ? cabe : 6;
                if (alto < 2) alto = 2;
            }

            for (int i = 0; i < ancho; i++)
            {
                int px = x0 + i;
                // Perfil: sube en peldaños, meseta, baja en peldaños.
                int desdeIzq = i / peldano;
                int desdeDer = (ancho - 1 - i) / peldano;
                // (playtest 34) LA ESCALINATA NO BAJA. El perfil simétrico de
                // las terrazas (sube por los dos lados y vuelve a bajar) es lo
                // correcto para una costura decorativa entre dos máquinas que
                // están A LA MISMA COTA -- pero la escalinata del umbral
                // separa dos plantas distintas: la de oficios (`CuartoY0+2`) y
                // la alcoba de observación (`CuartoY0+8`). Con el perfil
                // simétrico, el jugador subía seis peldaños y volvía a bajar
                // seis justo antes de llegar a la Columna, con un pozo entre
                // el último escalón y la plataforma soberana de la estación:
                // una loma, no una escalera. Monótona creciente, la meseta
                // ENTREGA la cota a la alcoba y el suelo queda continuo.
                int nivel = escalinata ? desdeIzq : (desdeIzq < desdeDer ? desdeIzq : desdeDer);
                if (nivel > alto) nivel = alto;
                if (nivel <= 0) continue;

                for (int k = 1; k <= nivel; k++)
                {
                    int y = sueloY + k;
                    if (CellGrid.InBounds(px, y)) grid.SetCell(px, y, MaterialId.Stone);
                }
            }

            RegistrarObra(x0, sueloY + 1, x1, sueloY + alto); // es obra del taller: el cincel no se la lleva por error (playtest 27).
        }

        /// <summary>
        /// Pilastra colgante con ménsula: baja del techo del cuarto
        /// <see cref="PilastraCaida"/> celdas, así que el vano que deja
        /// debajo (más de 40 celdas) es de sobra para volar. Es la pieza que
        /// convierte "un pasillo largo" en "una sala con tramos".
        /// </summary>
        /// <summary>(playtest 33) Valor por defecto; la caída REAL de cada pilastra la da <see cref="PilastraCaidas"/> -- ver ese docblock para por qué dejaron de ser todas iguales.</summary>
        private const int PilastraCaida = 14;
        /// <summary>(playtest 33) 3 -&gt; 4: con el techo 22 celdas más alto, una pilastra de 3 de ancho colgando 20 celdas se leía como un cable, no como piedra.</summary>
        private const int PilastraAncho = 4;

        private static void TallarPilastra(CellGrid grid, int cx, int caida)
        {
            int x0 = cx - PilastraAncho / 2;
            int x1 = x0 + PilastraAncho - 1;
            int yTop = CuartoY1;
            int yBot = CuartoY1 - caida;

            for (int y = yBot; y <= yTop; y++)
            {
                for (int px = x0; px <= x1; px++)
                {
                    if (CellGrid.InBounds(px, y)) grid.SetCell(px, y, MaterialId.Stone);
                }
            }

            // La MÉNSULA: la última hilada vuela una celda a cada lado (un
            // capitel invertido). Sin ella la pilastra es un poste; con ella
            // es piedra tallada.
            for (int px = x0 - 1; px <= x1 + 1; px++)
            {
                if (CellGrid.InBounds(px, yBot)) grid.SetCell(px, yBot, MaterialId.Stone);
                if (CellGrid.InBounds(px, yBot + 1)) grid.SetCell(px, yBot + 1, MaterialId.Stone);
            }

            RegistrarObra(x0 - 1, yBot, x1 + 1, yTop);
        }

        /// <summary>
        /// ARCO DE MEDIO PUNTO sobre la boca del pasillo a la Tolva: talla
        /// (a Empty) el perfil curvo por encima del vano recto que dejó
        /// <see cref="CarvePasilloTolva"/>. Solo QUITA piedra sobre el hueco
        /// que ya existía, así que no puede sellar el paso ni abrir uno nuevo
        /// hacia ningún sitio: el arco vive dentro de las 6 celdas de ancho
        /// que el pasillo ya ocupaba.
        ///
        /// (CONTRATO_RONDA50.md §3b, ENCARGO G, playtest 50) SIGUE SIN GATE
        /// PROPIO a propósito, decisión documentada: en Semilla Cero
        /// <see cref="CarvePasilloTolva"/> ya NO se llama (ver el bloque
        /// Semilla Cero de <see cref="BuildCuartoIntimo"/>), así que "el
        /// hueco que ya existía" no existe -- esta llamada sigue corriendo
        /// (<see cref="AdornarCuarto"/> no distingue de modo) y talla un
        /// puñado de celdas Empty AISLADAS dentro de piedra maciza, un par de
        /// columnas al este de <see cref="CuartoX1"/> (fuera del cuarto,
        /// nunca expuestas a su interior: <see cref="TallarArcoPasillo"/>
        /// arranca en `CuartoX1+1`, no en `CuartoX1`). Sin cincel que llegue
        /// ahí (el muro que las rodea sigue macizo) son inertes: no se ven,
        /// no se alcanzan, no cambian el conteo de "una sola boca" del
        /// contrato. Gatearlo también habría sido gratis, pero tocar
        /// <see cref="AdornarCuarto"/> para un efecto sin jugador que lo vea
        /// no lo justificaba -- anotado por si una ronda futura reintroduce
        /// algo que SÍ pueda tropezar con ese bolsón sellado.
        /// </summary>
        private static readonly int[] PerfilArco = { 0, 3, 3, 4, 4, 4, 3, 3 };

        private static void TallarArcoPasillo(CellGrid grid)
        {
            int yTecho = PasilloTolvaY0 + PasilloTolvaAlto - 1;
            int x0 = CuartoX1 + 1;

            for (int i = 0; i < PerfilArco.Length; i++)
            {
                int px = x0 + i;
                // Perfil de medio punto TABULADO (radio 4): la tabla es
                // round(sqrt(r² - (r-i)²)) calculada a mano -- este archivo NO
                // tiene `using UnityEngine` a propósito (es plano puro, sin
                // dependencia del motor: ver la cabecera) y no va a ganar uno
                // por ocho raíces cuadradas que son constantes.
                int subida = PerfilArco[i];
                for (int k = 1; k <= subida; k++)
                {
                    int y = yTecho + k;
                    if (CellGrid.InBounds(px, y)) grid.SetCell(px, y, MaterialId.Empty);
                }
            }
        }

        private static void PlaceNutrienteMound(CellGrid grid)
        {
            DrawSolidRect(grid, NutrienteMoundX0, NutrienteMoundY0, NutrienteMoundWidth, NutrienteMoundHeight, MaterialId.Nutrient);
        }

        private static void PlaceCharco(CellGrid grid)
        {
            DrawUShape(grid, CharcoX0, CuartoY0, CharcoWidth, CharcoHeight, WallThickness);
            int interiorX0 = CharcoX0 + WallThickness;
            int interiorWidth = CharcoWidth - 2 * WallThickness;
            int floorTopY = CuartoY0 + WallThickness - 1; // última fila maciza del suelo de la cubeta.
            DrawSolidRect(grid, interiorX0, floorTopY + 1, interiorWidth, CharcoAguaAltura, MaterialId.Water);
        }

        // =================================================================
        // CONSTRUCCIÓN
        // =================================================================

        public static void BuildTestLevel(CellGrid grid)
        {
            FillBorder(grid);
            FillFloor(grid, FloorHeight);   // bedrock bajo todo el mundo (sostiene el sótano).
            FillSurfaceFloor(grid);         // losa de la mitad de arriba (el pozo la talla después).
            BuildSotano(grid);              // sala + plataforma + repisas + el POZO (tala techo+losa).
            BuildCultivo(grid);
            BuildLaboratorio(grid);
            BuildDeliveryNiche(grid);
            PaintClimate(grid);             // ambiente uniforme (el clima POR ZONA se retiró en el playtest 17, ver la funcion).
        }

        /// <summary>Bedrock del mundo entero: sostiene el sótano y cierra el mundo por abajo.</summary>
        private static void FillFloor(CellGrid grid, int floorHeight)
        {
            for (int y = 0; y < floorHeight; y++)
            {
                for (int x = 1; x < CellGrid.W - 1; x++)
                {
                    grid.SetCell(x, y, MaterialId.Stone);
                }
            }
        }

        /// <summary>Losa de suelo de la mitad de arriba, a todo lo ancho del mundo — BuildSotano talla después el hueco del pozo dentro de ella.</summary>
        private static void FillSurfaceFloor(CellGrid grid)
        {
            for (int y = SurfaceFloorY0; y <= SurfaceFloorTop; y++)
            {
                for (int x = 1; x < CellGrid.W - 1; x++)
                {
                    grid.SetCell(x, y, MaterialId.Stone);
                }
            }
        }

        /// <summary>
        /// El SÓTANO: una sala CERRADA (con techo, a diferencia de las
        /// cubetas en U de arriba, que están abiertas) + una plataforma de
        /// cristalización bajo el pozo + dos repisas que solo dan forma al
        /// volumen vacío (el aprendiz vuela: no hacen falta caminos) + el
        /// POZO, que talla un hueco vertical a través del techo de esta sala
        /// Y de la losa de superficie a la vez — es la ÚNICA conexión entre
        /// las dos mitades del mundo.
        /// </summary>
        private static void BuildSotano(CellGrid grid)
        {
            DrawSolidRect(grid, SotanoX0, SotanoY0, SotanoX1 - SotanoX0 + 1, SotanoY1 - SotanoY0 + 1, MaterialId.Stone);
            DrawSolidRect(grid, SotanoInteriorX0, SotanoInteriorY0,
                SotanoInteriorX1 - SotanoInteriorX0 + 1, SotanoInteriorY1 - SotanoInteriorY0 + 1, MaterialId.Empty);

            DrawSolidRect(grid, SotanoPlinthX0, SotanoInteriorY0,
                SotanoPlinthX1 - SotanoPlinthX0 + 1, SotanoPlinthHeight, MaterialId.Stone);

            DrawSolidRect(grid, SotanoLedgeAX0, SotanoLedgeAY0, SotanoLedgeAX1 - SotanoLedgeAX0 + 1, SotanoLedgeHeight, MaterialId.Stone);
            DrawSolidRect(grid, SotanoLedgeBX0, SotanoLedgeBY0, SotanoLedgeBX1 - SotanoLedgeBX0 + 1, SotanoLedgeHeight, MaterialId.Stone);

            DrawSolidRect(grid, WellX0, WellCarveY0, WellX1 - WellX0 + 1, WellCarveY1 - WellCarveY0 + 1, MaterialId.Empty);
        }

        /// <summary>Las DOS cubas grandes de cultivo. Las placas ígneas las coloca AlkahestGameBootstrap (una por cuba, leyendo VatAX0/VatBX0).</summary>
        private static void BuildCultivo(CellGrid grid)
        {
            DrawUShape(grid, VatAX0, VatBaseY0, VatWidth, VatHeight, WallThickness);
            DrawUShape(grid, VatBX0, VatBaseY0, VatWidth, VatHeight, WallThickness);
        }

        /// <summary>Banco de trabajo (meseta + pila de recogida + pilar de grifos), bandeja fría y estante de redomas — todo dentro de la huella de LABORATORIO.</summary>
        private static void BuildLaboratorio(CellGrid grid)
        {
            DrawSolidRect(grid, BenchX0, SurfaceFloorTop + 1, BenchX1 - BenchX0 + 1, BenchMesetaHeight, MaterialId.Stone);
            DrawUShape(grid, BasinX0, BasinY0, BasinWidth, BasinHeight, WallThickness);
            DrawSolidRect(grid, TapPillarX0, BasinY0, TapPillarX1 - TapPillarX0 + 1, TapPillarTopY - BasinY0 + 1, MaterialId.Stone);

            DrawUShape(grid, ChillTrayX0, ChillTrayY0, ChillTrayWidth, ChillTrayHeight, WallThickness);
            DrawSolidRect(grid, RackX0, RackY0, RackX1 - RackX0 + 1, RackHeight, MaterialId.Stone);
        }

        // =================================================================
        // (playtest 54, REGLA 15 DE CLAUDE.md) RETIRADA DE LA LLAMADA, NO
        // BORRADA. `BuildCuartoIntimo` ya NO invoca `BuildTolvaCercana` --
        // el bloque de constantes `Buzon*` (junto a `EstanteBaseY`, más
        // arriba en este archivo) y `BuildBuzonMaestro` la sustituyen
        // ENTERA en Semilla Cero. Motivo: Cesar, sobre esta boca, "de
        // repente atravesarla con esa flecha amarilla y letrero grande --
        // dos barras amarillas que no tienen nada de tolva -- en medio del
        // camino, alejándome de la única cosa que necesito investigar al
        // inicio [el Crisol], es muy mala decisión" -- el hueco de suelo
        // x129..138 que tallaba esta función ES LITERALMENTE el paso
        // caminable Crisol->Prensa (ver el bloque "EL SITIO" más abajo,
        // todavía correcto como registro histórico de por qué se eligió
        // ese hueco en su día). Todo el bloque de abajo (constantes +
        // método) se conserva intacto y compilable por si un diseño futuro
        // quisiera una boca a ras de suelo en ese mismo hueco -- no se ha
        // detectado ningún otro llamante.
        // =================================================================
        // (CONTRATO_RONDA50.md §3b, ENCARGO G, playtest 50) LA TOLVA CERCANA
        // -- SOLO SEMILLA CERO (diagnóstico D4, "EL TRAYECTO MUDO"). Cesar,
        // veredicto del pt49, textual: "un trayecto muy grande para dejarlas
        // en la tolva SIN NADA QUE ME LO ENSEÑE". La Tolva CLÁSICA
        // (BuildDeliveryNiche, más abajo, ChuteMouthX0..Y1, tras el pasillo
        // fuera del cuarto) sigue existiendo tal cual para el modo CAÓTICO
        // (contrato §3e: "el caótico NO cambia") -- en Semilla Cero esa Tolva
        // lejana NO SE TALLA EN ABSOLUTO (ver el gate en BuildCuartoIntimo, y
        // el docblock de TallarArcoPasillo para la única esquirla inerte que
        // deja sin gate propio): "una sola boca" (contrato, textual), y es
        // ESTA.
        //
        // EL SITIO, LEÍDO DEL PLANO (regla 39/47 de CLAUDE.md, nunca de
        // prosa): el hueco "paso crisol -> prensa" (x125..142, ver el bloque
        // "EL PLANO CENTRAL" junto a CuartoX0/CrisolX/PrensaX más arriba en
        // este archivo) es la única terraza abierta DENTRO del cuarto que
        // (a) queda pegada a la huella del Crisol (huella real 88..124, ver
        // el docblock de EstanteX0/X1) sin pisarla -- ColumnaOcupada la
        // protege hasta x=125, esta boca arranca en TolvaCercanaX0=129, 4
        // columnas libres de margen --, (b) deja margen de sobra antes de la
        // huella de la Prensa (real 143..173, protegida desde x=142 --
        // TolvaCercanaX1=138 aquí abajo, 4 columnas libres), y (c) está a
        // 129-79=50 columnas de la cara tallable de la veta de turba
        // (VetaTurbaCaraX=79, y139..258): ni la toca ni se confunde con ella
        // (contrato §3b, "sin pisar estaciones ni la veta"). No es una
        // elección arbitraria -- el propio plano ya reservaba este hueco como
        // "corto, solo da para eso" (ver `_galeriasOriginales[2]`, la balda
        // en X0=128..X1=139, Y=CuartoY0+54=190, MISMO hueco): esa balda
        // cuelga 32 celdas por encima del labio de esta boca (y=158) sin
        // tocarla -- las dos estructuras SIEMPRE convivieron aquí, la balda
        // arriba, la Tolva ahora abajo.
        //
        // "A SEGUNDOS DE VUELO DEL CRISOL" (contrato, textual): el borde
        // oeste de la boca interior (TolvaCercanaMouthX0=131) queda a
        // 131-124=7 columnas del borde este de la huella del Crisol (124) --
        // de un vistazo, sin cruzar ninguna otra estación ni la Prensa.
        //
        // GEOMETRÍA: dos jambas de piedra (TolvaCercanaMuro=2 celdas cada
        // una) flanqueando una boca interior de TolvaCercanaBocaAncho=6
        // celdas, abierta por arriba TolvaCercanaAlto=20 celdas -- bien por
        // debajo de la pilastra colgante que ya ocupa esa misma columna
        // (X=133 cae dentro de esta boca; PilastraColumnas[0]/
        // PilastraCaidas[0]=24 cuelga desde el techo hasta y=CuartoY1-24=238,
        // **80 celdas por encima** del labio de esta boca en y=158: cero
        // solapamiento, verificado contra las constantes reales, no a ojo).
        // El SUELO de la boca es el propio suelo uniforme del cuarto
        // (BuildCuartoFloor ya deja sólida la fila BaseYDeEstacion(x)=
        // CuartoY0+2=138 en TODO el ancho de la sala, incluido este hueco):
        // a diferencia de BuildAlcobaFria (que vive en la cota +6 de la
        // alcoba de observación, otra franja de suelo), esta boca no
        // necesita colchón propio.
        //
        // Registrada como Obra del taller (RegistrarObra, protección
        // anticincel de siempre, y ANTES de AdornarCuarto para que
        // ColumnaOcupada la respete al tallar terrazas/pilastras) pero
        // DELIBERADAMENTE FUERA de RegistrarObraSemillaCero: la Tolva NUNCA
        // se tapia -- tiene que estar visible y latiendo desde el minuto 0
        // (D4), no destaparse como las cinco salas de pregunta.
        //
        // COSTURA PARA EL DIRECTOR (INTEGRACIÓN, encargo M/bootstrap): estas
        // constantes son PÚBLICAS y Game/DeliveryChute.cs (mismo encargo, ver
        // ese archivo) ya las lee DIRECTAMENTE por sí solo -- ese archivo
        // decide en su propio Init() si usa esta boca o la clásica según
        // AlkahestGameBootstrap.ModoSemillaCero, exactamente el mismo patrón
        // que ya usan SimLevelBuilder.BuildVetaTurba/Game/HintSystem.cs. NO
        // hace falta tocar Game/AlkahestGameBootstrap.cs: SpawnDeliveryChute
        // ya llama a `DeliveryChute.Init(sim, orderSystem)` sin pasarle
        // ningún sitio (DeliveryChute deriva su geometría sola de
        // SimLevelBuilder, ver su propio docblock) -- se documenta aquí de
        // todos modos porque el contrato pide expresamente exponer la
        // constante/API del sitio seed-0 como costura reportable, aunque en
        // este caso ya viene cableada sin que el bootstrap tenga que moverse.
        // =================================================================
        /// <summary>Ancho de cada jamba de piedra que flanquea la boca cercana.</summary>
        public const int TolvaCercanaMuro = 2;
        /// <summary>Ancho de la boca interior (por donde cae la materia), sin contar jambas.</summary>
        public const int TolvaCercanaBocaAncho = 6;
        /// <summary>Jamba izquierda, borde exterior.</summary>
        public const int TolvaCercanaX0 = 129;
        /// <summary>Jamba derecha, borde exterior. 129 + 2*2 + 6 - 1 = 138.</summary>
        public const int TolvaCercanaX1 = TolvaCercanaX0 + TolvaCercanaMuro * 2 + TolvaCercanaBocaAncho - 1;
        /// <summary>Boca interior, borde izquierdo (dentro de la jamba). 131.</summary>
        public const int TolvaCercanaMouthX0 = TolvaCercanaX0 + TolvaCercanaMuro;
        /// <summary>Boca interior, borde derecho (dentro de la jamba). 136.</summary>
        public const int TolvaCercanaMouthX1 = TolvaCercanaX1 - TolvaCercanaMuro;
        /// <summary>
        /// Última fila maciza del suelo bajo esta boca -- MISMA fórmula que
        /// <see cref="BaseYDeEstacion"/> para x&lt;250 (planta baja), escrita
        /// a mano porque <c>BaseYDeEstacion</c> no es const-foldable
        /// (CLAUDE.md regla 39: el valor SÍ está verificado contra el método
        /// real -- 129&lt;250, así que <c>BaseYDeEstacion(129)</c> devuelve
        /// exactamente esto, <c>CuartoY0+2</c>).
        /// </summary>
        public const int TolvaCercanaBaseY = CuartoY0 + 2; // 138.
        /// <summary>Alto de la boca abierta, en celdas.</summary>
        public const int TolvaCercanaAlto = 20;
        /// <summary>Boca interior, primera fila de aire (justo sobre el suelo). 139.</summary>
        public const int TolvaCercanaMouthY0 = TolvaCercanaBaseY + 1;
        /// <summary>Boca interior, última fila de aire (el labio, abierto por arriba). 158.</summary>
        public const int TolvaCercanaMouthY1 = TolvaCercanaMouthY0 + TolvaCercanaAlto - 1;

        /// <summary>
        /// Talla la Tolva cercana (ver el bloque de constantes de arriba para
        /// el porqué exacto del sitio): dos jambas de piedra + boca vacía,
        /// plantada en el suelo ya sólido del cuarto (sin colchón propio,
        /// como sí necesita <see cref="BuildAlcobaFria"/>). SOLO se llama
        /// desde <see cref="BuildCuartoIntimo"/> bajo el gate de
        /// <see cref="AlkahestGameBootstrap.ModoSemillaCero"/>.
        /// </summary>
        private static void BuildTolvaCercana(CellGrid grid)
        {
            for (int y = TolvaCercanaMouthY0; y <= TolvaCercanaMouthY1; y++)
            {
                for (int t = 0; t < TolvaCercanaMuro; t++)
                {
                    if (CellGrid.InBounds(TolvaCercanaX0 + t, y)) grid.SetCell(TolvaCercanaX0 + t, y, MaterialId.Stone);
                    if (CellGrid.InBounds(TolvaCercanaX1 - t, y)) grid.SetCell(TolvaCercanaX1 - t, y, MaterialId.Stone);
                }
            }

            // La boca: vacía entre las dos jambas -- el suelo general
            // (BuildCuartoFloor) ya deja TolvaCercanaBaseY sólido debajo de
            // aquí, así que no hace falta repintarlo.
            DrawSolidRect(grid, TolvaCercanaMouthX0, TolvaCercanaMouthY0,
                TolvaCercanaMouthX1 - TolvaCercanaMouthX0 + 1, TolvaCercanaMouthY1 - TolvaCercanaMouthY0 + 1, MaterialId.Empty);

            RegistrarObra(TolvaCercanaX0, TolvaCercanaMouthY0, TolvaCercanaX1, TolvaCercanaMouthY1);
        }

        /// <summary>
        /// Contrafuerte de piedra en ENTREGA con un pozo vertical excavado y
        /// abierto por arriba: la boca de la Tolva. Zócalo ancho abajo +
        /// torre más estrecha arriba, apoyados en la losa de superficie (en
        /// vez del bedrock del mundo, que ahora sostiene el sótano).
        /// </summary>
        public static void BuildDeliveryNiche(CellGrid grid)
        {
            // (playtest 34) Era `SurfaceFloorTop + 1` (156), la losa del
            // taller CLÁSICO. Ahora se deriva de la propia boca -- la misma
            // aritmética invertida (189-3-30 = 156 daba el valor viejo), así
            // que el contrafuerte sigue naciendo EXACTAMENTE 33 filas por
            // debajo del labio del pozo y viaja con él cuando el cuarto se
            // mueve, en vez de quedarse anclado a una losa que esta
            // construcción no talla.
            int baseY = ChuteMouthY0 - 3 - ChuteBaseHeight;

            // Zócalo: de la losa de superficie hasta la altura de las cubas/banco.
            DrawSolidRect(grid, ChuteWallX0, baseY, CellGrid.W - ChuteWallX0, ChuteBaseHeight, MaterialId.Stone);

            // Torre: algo más estrecha, hasta el labio de la boca.
            int torreX0 = ChuteWallX0 + 4;
            int torreY0 = baseY + ChuteBaseHeight;
            DrawSolidRect(grid, torreX0, torreY0, CellGrid.W - torreX0, ChuteMouthY1 + 1 - torreY0, MaterialId.Stone);

            // Pozo excavado (queda abierto por arriba: es por donde se vierte).
            DrawSolidRect(grid, ChuteMouthX0, ChuteMouthY0,
                ChuteMouthX1 - ChuteMouthX0 + 1, ChuteMouthY1 - ChuteMouthY0 + 1, MaterialId.Empty);
        }

        /// <summary>
        /// EL CLIMA (playtest 17): un ÚNICO ambiente para todo el mundo,
        /// `CellGrid.AmbientRaw` (raw 70 = 20°C). Ver el docblock de la clase
        /// para las dos razones por las que el clima POR ZONA se retiró.
        ///
        /// Esta función parece redundante — `CellGrid.ambient` ya nace con ese
        /// mismo valor en el constructor — y se conserva a propósito por dos
        /// motivos: (a) deja UN solo sitio nombrado donde el clima se decide,
        /// así que quien lo reintroduzca (el clima del FUTURO es el que crea
        /// el jugador: una fragua que entibia su alrededor, no un rótulo del
        /// plano) tiene el gancho evidente y el porqué escrito al lado;
        /// (b) hace que `BuildTestLevel` sea COMPLETO por sí mismo — hoy
        /// `AlkahestSim.Start` siempre construye sobre un `CellGrid` recién
        /// creado (cuyo constructor ya deja `ambient` en base, así que esta
        /// pasada es hoy una reescritura de valores ya correctos), pero si
        /// alguna vez se reconstruye el nivel sobre una grilla REUTILIZADA, un
        /// `ambient` sucio de antes no sobrevivirá al rediseño. Una pasada por
        /// la grilla entera una vez por partida (221.184 escrituras de byte):
        /// irrelevante frente a los 30Hz.
        /// </summary>
        private static void PaintClimate(CellGrid grid)
        {
            for (int i = 0; i < grid.ambient.Length; i++)
            {
                grid.ambient[i] = CellGrid.AmbientRaw;
            }
        }

        // ---------------------------------------------------------------------------------
        private static void FillBorder(CellGrid grid)
        {
            for (int x = 0; x < CellGrid.W; x++)
            {
                grid.SetCell(x, 0, MaterialId.Stone);
                grid.SetCell(x, CellGrid.H - 1, MaterialId.Stone);
            }
            for (int y = 0; y < CellGrid.H; y++)
            {
                grid.SetCell(0, y, MaterialId.Stone);
                grid.SetCell(CellGrid.W - 1, y, MaterialId.Stone);
            }
        }

        // ---------------------------------------------------------------------------------
        // Primitivas de dibujo, públicas para reuso por niveles futuros.
        // ---------------------------------------------------------------------------------

        /// <summary>Dibuja una cubeta en forma de U: paredes laterales de `wallThickness` de ancho y suelo también de `wallThickness`, abierta por arriba.</summary>
        public static void DrawUShape(CellGrid grid, int x0, int y0, int width, int height, int wallThickness)
        {
            int x1 = x0 + width - 1;
            int yTop = y0 + height - 1;

            // Suelo de la cubeta.
            for (int y = y0; y < y0 + wallThickness; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Stone);
                }
            }

            // Paredes laterales.
            for (int y = y0; y <= yTop; y++)
            {
                for (int t = 0; t < wallThickness; t++)
                {
                    if (CellGrid.InBounds(x0 + t, y)) grid.SetCell(x0 + t, y, MaterialId.Stone);
                    if (CellGrid.InBounds(x1 - t, y)) grid.SetCell(x1 - t, y, MaterialId.Stone);
                }
            }
        }

        /// <summary>Rectángulo sólido relleno del material indicado.</summary>
        public static void DrawSolidRect(CellGrid grid, int x0, int y0, int width, int height, byte materialId)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                for (int x = x0; x < x0 + width; x++)
                {
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, materialId);
                }
            }
        }
    }
}
