// (playtest 17) `using System;` retirado: lo único que lo necesitaba era el
// `Math.Round` de los degradados de clima, que ya no existen.

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
        public const int ChuteMouthY0 = SurfaceFloorTop + 1 + ChuteBaseHeight + 3; // 189 (altura, sin cambios desde el playtest 4)
        public const int ChuteMouthY1 = ChuteMouthY0 + 49; // 238 (altura, sin cambios desde el playtest 4)

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
        /// el encuadre.
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
        public const int CuartoX0 = 248;
        public const int CuartoX1 = 357; // ancho 110, sin cambios
        public const int CuartoY0 = 168; // (2ª ronda) antes 154
        public const int CuartoY1 = 209; // (2ª ronda) antes 223; alto 42, antes 70

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
        private const int CharcoX0 = 250; // (playtest 22; antes 267, y antes 314)
        private const int CharcoWidth = 14;
        private const int CharcoHeight = 7;
        /// <summary>Filas de agua dentro de la cubeta -- no llena hasta el borde a propósito: se lee como charco, no como aljibe rebosante.</summary>
        private const int CharcoAguaAltura = 2;

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
        // así que montar ahí deja el aparato pegado a la roca del muro (x=247),
        // no flotando.
        //
        // LECTURA DE IZQUIERDA A DERECHA que queda en la sala, que es la que
        // pidió Cesar: **caños + pila -> criatura -> capullo**.
        /// <summary>Columna de montaje de los dos caños: la primera columna de aire, pegada al muro izquierdo de la cámara.</summary>
        public const int CanoMontajeX = CuartoX0;
        /// <summary>Fila del caño de AGUA. Bastante por encima del labio de la cubeta (que llega a CuartoY0+CharcoHeight-1) para que el chorro se vea caer.</summary>
        public const int CanoAguaY = CuartoY0 + 13;
        /// <summary>Fila del caño de NUTRIENTE, encima del de agua. Separación amplia para que las dos chapas de rótulo no se pisen (el problema de "los caños con los títulos apilados" del playtest 15).</summary>
        public const int CanoNutrienteY = CuartoY0 + 21;

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
        /// </summary>
        public const int AprendizX = 290; // (tercera ronda; antes 300)
        public const int AprendizY = CunaTopY + 1; // 180 (tercera ronda; antes 176): justo sobre el remate de la cuna, mirando dentro.

        /// <summary>
        /// Construye el mundo del pivot (playtest 21): todo piedra salvo la
        /// cámara íntima. Ver el docblock de la clase para el reparto
        /// completo de lo que hay dentro y por qué. `BuildTestLevel` NO se
        /// llama desde aquí -- las dos construcciones son alternativas, no
        /// una capa sobre la otra (ver `AlkahestSim.Start`, que elige una de
        /// las dos).
        /// </summary>
        public static void BuildCuartoIntimo(CellGrid grid)
        {
            FillWorldStone(grid);
            ExcavateCuarto(grid);
            BuildCuna(grid);
            BuildRepisa(grid);
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
            PlaceCharco(grid);
            BuildDeliveryNiche(grid); // SIN TOCAR: la Tolva queda sellada porque ya no hay nada excavado a su alrededor.
            PaintClimate(grid);       // mismo ambiente uniforme que el plano viejo (regla 31 de CLAUDE.md: no reintroducir clima por zona).
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

        /// <summary>
        /// Contrafuerte de piedra en ENTREGA con un pozo vertical excavado y
        /// abierto por arriba: la boca de la Tolva. Zócalo ancho abajo +
        /// torre más estrecha arriba, apoyados en la losa de superficie (en
        /// vez del bedrock del mundo, que ahora sostiene el sótano).
        /// </summary>
        public static void BuildDeliveryNiche(CellGrid grid)
        {
            int baseY = SurfaceFloorTop + 1;

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
