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
    /// CUATRO ZONAS, pedidas explícitamente por Cesar:
    ///
    ///                                     y=287 (muro/techo del mundo)
    ///   ┌──────────── SUPERFICIE (mitad de arriba, y=144..287) ─────────────────────┐
    ///   │      CULTIVO        │      LABORATORIO       │        ENTREGA             │
    ///   │      x16..250       │      x262..505         │  x517..767 (Tolva llega    │
    ///   │      (20 °C)        │        (20 °C)          │  hasta el muro del mundo)  │
    ///   │ cubaA        cubaB  │ banco+grifos+pila,      │  x517: Tolva PEGADA a      │
    ///   │ x30          x187   │ bandeja fría, estante,  │  LABORATORIO (boca en      │
    ///   │ LEJOS ·····► CERCA  │ ..........POZO..........│  x529..550) + contrafuerte │
    ///   │ (placas ígneas en   │                          │  que SIGUE hasta x767:     │
    ///   │  las dos cubas)     │                          │  cerca de usar, lejos de   │
    ///   │                     │                          │  ver acabar (LEJOS·►)      │
    ///   └─────────────────────┴──────────┬───────────────┴────────────────────────────┘
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
    /// recogida, bandeja fría, estante de redomas, y el POZO por el que se
    /// baja al sótano. ENTREGA = la Tolva
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
    /// encargo): cuba A (`VatAX0`=30, la segunda cuba de cultivo), el SÓTANO
    /// entero (solo se llega volando por el POZO, sin cambios — ES la razón
    /// de que se sienta como una sala aparte) y, dentro de LABORATORIO
    /// mismo, la bandeja fría y el estante de redomas (`ChillTrayX0`=390,
    /// `RackX0`=447): NO se movieron — siguen donde estaban, que ya es
    /// "cerca" en términos de distancia, pero no forman parte del bucle
    /// básico (grifo/pila/cuba/Tolva) así que no hacía falta acercarlos más
    /// ni alejarlos; se dejan quietos porque tocarlos sin necesidad es el
    /// tipo de riesgo que el encargo pide evitar.
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
        public const int LabX1 = 505;
        public const int EntregaX0 = 517;
        /// <summary>
        /// 751 -> 607 (playtest 16, "CERCA DEL INICIO" en el docblock de la
        /// clase). Solo alimenta <see cref="ChuteWallX0"/> (`= EntregaX1 -
        /// 90`, fórmula SIN TOCAR): al bajar este valor, el zócalo de la
        /// Tolva se acerca 144 celdas a LABORATORIO SIN dejar de dibujarse
        /// hasta <c>CellGrid.W</c> (esa parte de <see cref="BuildDeliveryNiche"/>
        /// usa el ancho del mundo, no `EntregaX1`) — la boca queda cerca, el
        /// bloque de piedra que la sostiene se sigue viendo llegar hasta el
        /// muro. `EntregaX0` no se tocó: sigue marcando donde EMPIEZA la
        /// zona (documentación/ASCII, no alimenta ningún cálculo — ver el
        /// grep del docblock) y por pura coincidencia de la nueva aritmética
        /// queda pegado al nuevo `ChuteWallX0` (los dos caen en 517).
        /// </summary>
        public const int EntregaX1 = 607;

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
        // de recogida + columna de grifos), bandeja fría y estante de
        // redomas, más el POZO que baja al SÓTANO. Todo cabe con margen en
        // los 244 celdas de ancho de la zona (x262..505).
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
        public const int ChillTrayX0 = 390;
        public const int ChillTrayWidth = 50; // antes 52: mismo orden de magnitud, ver medición de recipientes en el resumen.
        public const int ChillTrayY0 = 246;
        public const int ChillTrayHeight = 10;
        public const int ChillTrayInteriorX0 = ChillTrayX0 + WallThickness;                       // 393
        public const int ChillTrayInteriorX1 = ChillTrayX0 + ChillTrayWidth - 1 - WallThickness;  // 436
        public const int ChillTrayInteriorY0 = ChillTrayY0 + WallThickness;                       // 249
        /// <summary>Fila donde vive la piedra fría (última de su suelo).</summary>
        public const int ChillPlateRow = ChillTrayY0 + WallThickness - 1; // 248

        // ---- Estante de redomas -------------------------------------------------------------
        public const int RackX0 = 447;
        public const int RackX1 = 501;
        public const int RackY0 = 246; // mismo nivel que la bandeja fría — un único "estante superior" leído de un vistazo.
        public const int RackHeight = 3;
        public const int RackTopY = RackY0 + RackHeight; // 249

        // ---- El POZO: la única conexión entre LABORATORIO y SÓTANO -------------------------
        // El aprendiz vuela, así que no hace falta una escalera — solo un
        // hueco vertical real que atraviese el techo del sótano Y la losa de
        // superficie a la vez (ver BuildSotano). Centrado entre el banco y
        // la bandeja fría/estante para no chocar con ninguno de los dos.
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
        // encargo). Más ancho que el propio Laboratorio (x220..530 vs.
        // x262..505) para que se sienta como una sala propia, no un simple
        // hueco bajo la huella exacta de arriba.
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
        // =================================================================

        public const int ChuteWallX0 = EntregaX1 - 90; // 517 (antes 661)
        public const int ChuteMouthX0 = ChuteWallX0 + 12; // 529 (antes 673)
        public const int ChuteMouthX1 = ChuteMouthX0 + 21; // 550 (antes 694; 22 celdas de boca, igual que el playtest 4)
        public const int ChuteMouthY0 = SurfaceFloorTop + 1 + ChuteBaseHeight + 3; // 189 (altura, sin cambios)
        public const int ChuteMouthY1 = ChuteMouthY0 + 49; // 238 (altura, sin cambios)

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
