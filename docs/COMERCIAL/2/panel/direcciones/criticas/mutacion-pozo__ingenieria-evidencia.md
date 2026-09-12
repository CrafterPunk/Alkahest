# CRÍTICA · «El Pozo Sellado» (mutacion-pozo) · lente: INGENIERÍA Y EVIDENCIA

*(Director técnico, segunda pasada, 2026-09-12. Único tema: qué pide del sustrato, qué evidencia del
laboratorio la sostiene y cuál la contradice, y la prueba más barata capaz de matarla. Leído entero:
`mutacion-pozo.md`, `01_LEYES.md`, las refutaciones de ingeniería y apalancamiento de sello, balanza y
cuna; `SimStepper.Laboratorio.cs` (`LabAgua` :406-495, `LabInfiltrarHacia` :497-520, `LabPoroso`
:535-720, `LabPlanta` :801-908, `LabHogar`/`LabCalentarHasta` :917-991, `LabManantial` :1004,
`LabSumidero`/`LabTragar` :1021-1033, `LabPresion` :1080-1175, `LabLuz` :1177-1288,
`LabDifusionTermica` :1306-1355); `SimStepper.cs` (`Move` :738, `ProcessGas` :1443-1620);
`LabMateriales.cs` (`EsFondo` :98); `LabParams.cs` (todos los defaults); `Universe.Laboratorio.cs`
(:211-213, hielo); `CellGrid.cs` (W/H `const` :56-57, `ambient` :116); `LabBench.cs` entero; el banco
`2026-09-06_2149_banco.md`; CHECKPOINT §6f-6g y R148/R150; INFORME_FINAL §B y §G. Lo que afirmo del
código lleva línea; lo que es aritmética sobre el código lo marco como tal: va al banco antes de
creerse.)*

## 0. Veredicto en una línea

**SEGUNDA RONDA.** No pide nada refutado y casi todo tiene banco: J entero más una capa propia fina
(aforo, tramos, promesa, bolsas). Pero el motor que la distingue de «J con cronómetro por tramo» es
el acoplamiento vertical, y el código medido dice lo contrario de su narración: el calor no cruza un
suelo de roca, el humo cruza solo mientras un fuego abierto tiene combustible, la planta viva es
inmune a la oscuridad, la luz del cielo muere en la fila 255, y los relojes que sí existen (el filtro
se ciega en un minuto, la columna se inunda en catorce) no son los que cuenta. Su propia prueba de
§8 mataría por montaje. La prueba honesta cuesta lo mismo (media semana de banco, solo montajes) y no
se ha corrido: hasta que se corra no es finalista.

## 1. Qué pide del sustrato, pieza a pieza

| pieza | ¿catálogo? | ¿acotada y verificable en banco? | dependencia secuencial | coste declarado → corregido |
|---|---|---|---|---|
| **J entero** (diario bajo las puertas, volcado/carga, `CorrerSello`, condición como dato, cláusulas de grilla, `LabBandas`, anillo, balanza solo al fondo) | sí, J (viable ×2) | sí: pruebas a'-d de la refutación de ingeniería | raíz de todo lo demás | 5-6 sem → 5-6; el núcleo que el pozo necesita (diario, volcado, `CorrerSello`, condición) son 3-3,5; balanza, bandas y anillo pueden ir detrás del prototipo |
| **Línea de aforo** | nuevo; «contadores puros, hashes intactos» es cierto | sí, si lleva su validador (Σ cruces == Δ inventario por tramo) | tras el diario (comparte libro y volcado) | 3 días → **1 semana** (§4.1: no basta con `SwapCells`/`Move`) |
| **Tramos** (rejilla 288×768, suelos cada 96 filas, `ambient` por fila, piedra, sumidero solo al fondo) | nuevo; `ambient[]` ya existe y `LabDifusionTermica` :1341 ya tira hacia él | sí, pero mata los nueve hashes del banco (§4.2) | **antes** del formato del volcado (el fichero lleva W y H) | 3-4 días + G3 → **1 semana** (rotación 1 día, rebase del banco 2-3 días, G3 medio día) |
| **Bolsas envejecidas** (mitad de la cuna) | J·cuna reducida (viable con ajuste) | sí | tras `CorrerSello` y el validador | 1,5 sem → 1,5 (§4.3: la columna entera se envejece en una corrida de ~60 s, no tramo a tramo) |
| **C** (sensor, Piel, tinte, tizne, brasa) | sí, C | sí (un día de `temp[]` en la caja) | ninguna; paralelo | 1-1,5 → 1-1,5 |
| L segunda ola, A entrega A, V tercera ola | sí | sí | fuera del prototipo feo | como el catálogo |

Dos dependencias que la dirección no nombra: **la forma de la rejilla va antes del volcado** (o el
primer fichero de partida nace con W/H equivocados y con nueve hashes de referencia que ya no montan)
y **el aforo va después del diario**. Y una que va antes de todo: el diario del sello exige que el
muñeco escriba la grilla por las puertas tick-estampadas (contradicción (a) de `01_LEYES §6`); la
dirección lo dice en una frase y es la condición de que el sello exista.

## 2. El acoplamiento vertical, leído del código

Es lo único que separa «pozo» de «recibo con partición». Cinco hechos, tres de aritmética y dos ya
medidos.

**2.1 El calor no cruza un suelo de roca (aritmética).** `LabDifusionTermica` :1327: paso =
`Σ d·k / (64·c)`; roca `k=2, c=3` (divisor 192); paso mínimo forzado a ±1 si hay cualquier flujo
(:1328); tirón hacia `ambient[i]` de 1 raw cada 32 ticks (:1334-1338), es decir 0,25 raw por visita.
Por una cadena de roca pasa como mucho ~1 raw por visita y eslabón (para pasar 2 hace falta Σd ≥ 192)
y cada eslabón pierde 0,25: **un suelo de cuatro celdas es adiabático en régimen; uno de una o dos
transmite**. El horno de la hora 5 («el calor atraviesa el suelo y el 4 pierde su promesa») solo
existe pegado a un suelo de 1-2 celdas. Por la garganta (aire, `k=4, c=1`, convección ×2 hacia arriba
:1350) sí sube calor, pero se reparte y el tirón lo come en 10-20 celdas: solo el lecho que esté al
lado de la boca de la garganta lo nota. **El grosor del suelo y la distancia lecho-garganta son el
mando de acoplamiento térmico y la dirección no los fija**: geometría por balancear, lo que la función
objetivo penaliza. Se confirma en una tarde (horno bajo suelos de 1, 2, 4 y 8).

**2.2 El humo cruza, mientras haya fuego abierto (medido).** `ProcessGas`: sube una celda por tick en
aire libre (:1549), vida `VidaHumo` 255 con la mitad de descuento bajo techo (:1483-1490): cruza 96
filas en ~100 ticks y llega hasta la boca del cielo, donde `LabLuz` la apaga en su fuente
(`LuzDecayHumo` 24 por celda). Ya está medido: R148 puso la chimenea de 80 celdas de fibra sobre el
huerto y dio 15 celdas de humo en el minuto 5 y **0 después**, porque el fuego se consumió
(INFORME_FINAL §B). La carbonera de boca 1 —la que la dirección elige para su prueba— arde en sordina
(humo/4, SimStepper.cs:885) y dio 0 humo fuera del recinto (`01_LEYES §3`). Lo único que humea días
sin manos es la tolva: 466 s = 7,8 días de 1 800 ticks; nadie la repone hasta V. El humo como
amenaza es un temporizador de combustible, no un reloj.

**2.3 La planta viva es inmune a la oscuridad (código).** `LabPlanta` :801-908 mata solo por perder el
sustrato (:809) o por savia cero durante `PlantaMarchitaVisitas` (:886-903); la luz solo gobierna
germinar (:704, `luz[i+W]`) y crecer (:870-873). Una planta con raíz húmeda vive a `luz = 0` para
siempre. **La cláusula «planta viva en (x,y)» no la rompe el humo**; lo que rompe es «nacidas ≥ n/día».
La primera culpa de la primera hora es falsa en el código.

**2.4 La luz del cielo muere en la fila 255 (código).** `LuzDecayCielo` 1 por fila de aire (:1226,
rango 0-64 en LabParams :291): desde la boca, el tramo 2 recibe ~159, el tramo 3 ~63 (solo sus 23
primeras filas por encima de `PlantaLuzMin` 40), el tramo 4 nada. «Siete gargantas y dos cuñas» es L,
o poner `decayCielo` a 0, que quita el dilema. Sí están en el código dos cruces que la dirección no
usa: un tapón de grava en la garganta **apaga** la luz de abajo (`LabLuzDesde` devuelve 0 en sólidos,
:1269) y cinco filas de poza cuestan 100 (`LuzDecayAgua` 20). **Filtrar cuesta luz**: una promesa
contra otra sin regla nueva.

**2.5 Lo que sí sube del tramo nuevo es el agua, y la dirección no lo nombra (aritmética).** El
manantial emite `Caudal` 24 celdas/s (:1011) hasta quedar rodeado, y entonces espera (:1016). Un
sumidero traga 4 vecinos líquidos por visita (:1023) = 15 celdas/s por celda de sumidero. Con el
sumidero solo en el fondo, todo tramo cuya garganta no drene se llena: un tramo de 288×96 con ~20 k
celdas libres se inunda en ~14 minutos = 14 días de 60 s; la columna entera con un sumidero de una
celda sube a 9 celdas/s netas y termina inundada; con dos celdas (30 > 24) drena para siempre. **La
hidrología del pozo la deciden `Caudal` y el número de celdas del sumidero**, dos números del
constructor con un umbral duro (`Caudal ≤ 15 × nSumidero`), y es el acoplamiento vertical más fuerte
que hay en el código: lo que sube del tramo nuevo es el nivel del agua, y una promesa de caudal muere
cuando el tramo de abajo se inunda hasta su aforo. El riesgo de §6 («el tramo de agua es eterno»)
está invertido: el tramo de agua se ahoga.

## 3. Los relojes lentos, con números

| reloj | mecanismo | tiempo (medido o aritmética) | ¿vive entre el día 1 y el 60 (60 s/día)? |
|---|---|---|---|
| colmatación de grava | `LabInfiltrarHacia` :505-512: `rate ∝ perm·libre²`, `finos = (int)(rate·carga/255)` sin restar de la carga del agua | agua de manantial (carga 40): 11 u/visita al inicio; **el entero trunca a 0 en cuanto rate < 7, o sea a carga ≈ 55**: se para al 22 % y deja pasar el 55 % para siempre. Agua concentrada (255): cierra al ~70 % en ~60-80 visitas ≈ 20 s | no: o nunca o en un tercio de día |
| cegado por depósito | `LabAgua` :466: carga ≥ 200 y reposo ≥ 24 visitas sobre cualquier `EsFondo` (grava incluida) | agua turbia quieta sobre el tapón: el tapón se cubre de sedimento (perm 12 frente a 90) en **~200-400 ticks**; la poza se ciega a razón de ≤ 4,8 celdas/s con carga 40 | segundos por celda; la poza entera, proporcional a su volumen (300 celdas: 1 día; 5 000: 17 días) |
| **cegado del sumidero** | la dirección lo cita como reloj (§0.1) | **no existe**: el agua sobre el sumidero vive ≤ 8 ticks y nunca llega a reposo 24 (refutación de la balanza, que la dirección dice haber leído) | no |
| tolva | HF3 | 466 s = 7,8 días | sí, uno; muere sin V |
| inundación de un tramo | §2.5 | ~14 días por tramo sin drenaje | **sí, y es el más fuerte** |
| planta sin savia | 40 visitas | ~10 s | no |
| manantial, hogar, núcleo frío | pins | eternos | no |

Los días 6, 30, 38 y 48 de la narración no los produce ninguna ley: los produce `DiaTicks` (que no
existe en `LabParams`) y una colmatación que en el código no tiene fase lenta. Los relojes que sí
viven en esa ventana son la inundación, el cegado de una poza grande y la tolva, y ninguno aparece
en el core loop escrito.

## 4. Huecos técnicos propios

**4.1 El aforo no vive en `SwapCells`.** Con la garganta abierta lo que cae pasa por `Move` :738 y
`SwapCells`: ahí sí. Pero la jugada canónica (minuto 7: tapar la garganta con grava para que salga
clara) hace cruzar el agua **como unidades de `humedad`**: `LabInfiltrarHacia` (agua → grava),
percolación en `LabPoroso` :558-575 (grava → grava), exudación :543-551 (`LabNacerAgua` bajo el
tapón), capilaridad :577-579. El vapor cruza como humedad de celdas de aire (`LabIntercambioVapor`,
`VaporAscenso`) y vuelve como goteo (`LabGotear`). Y `LabPresion` :1140-1150 muda una celda de la
superficie más alta a la más baja con dos `SetCell`: si una garganta llena conecta dos pozas, el agua
cruza la fila sin pasar por ella. Son ocho o nueve ganchos (Move, mudanza de presión, infiltración,
percolación, exudación, capilaridad, vapor, goteo, tragado) más el validador de conservación por tramo
que caza los que falten. El «calor con signo» es exacto solo para lo que cruza como celda; la
conducción a través de la fila es un flujo de campo y no se atribuye sin aproximar. Una semana, no
tres días; tuning 9 se sostiene porque no hay ningún número, solo contabilidad.

**4.2 Girar la rejilla cuesta el banco.** `CellGrid.W/H` son `const` (:56-57), 231 usos por nombre y
solo dos literales: girar es un día. Pero los nueve montajes de `LabBench` escriben coordenadas
absolutas hasta x = 740 (`Bloque` recorta por `InBounds` sin avisar) y los nueve hashes son la
licencia de optimizar y la prueba de determinismo: en 288×768 ninguno monta. O W/H pasan a
`static readonly` (dos formas en el mismo ejecutable; `i % W` e `i / W` viven en todos los bucles
calientes y la división por no-constante cuesta: medir, puede ser 5-15 %) o el banco se rebasa para la
forma nueva con montajes relativos a un origen y se pierde la continuidad con R130-R150. Decisión
previa a J, no «3-4 días en paralelo». G3 tal como está formulado (≤ 3 ms/tick) ya está contestado:
el diluvio (110 k celdas de agua turbia) corre a 3,01 ms y un tramo de agua son 27 k. El G3 útil es
«el banco con W/H no constantes queda dentro del 10 % del banco con `const`».

**4.3 Bolsas envejecidas: una corrida, no ocho.** `LabCampos` y `LabDifusionTermica` recorren la
grilla entera aunque duerma: ~1,5-2 ms/tick de suelo; 40 000 ticks son 60-80 s. Como los suelos de
roca aíslan (§2.1) y las gargantas nacen cerradas, envejecer los ocho tramos juntos en una corrida es
lo mismo que envejecerlos por separado: una barra de carga de un minuto o un hilo de fondo. Lo caro
es el validador (K mutantes × N ticks por tramo = 40 corridas de un minuto por semilla): offline para
la campaña, nunca en el sorteo en vivo.

**4.4 El gradiente de ambiente choca con el hielo.** `Universe.Laboratorio.cs` :211-213: el agua
hiela a `CToRaw(0)` = 60 raw y el hielo funde a `CToRaw(5)`; el «5 °C arriba» del constructor es
**exactamente el punto de fusión del hielo del laboratorio** y está 2 raw sobre el de congelación.
Cualquier gota que el núcleo frío o el alambique hiele arriba (hecho 4 de `01_LEYES`: hoy graniza) no
vuelve a fundir nunca con el tirón al ambiente, y el hielo es opaco y sólido: tapona gargantas y apaga
luz. Es un número del constructor sin medir, y hay que subirlo o medirlo. En el otro extremo, 45 °C
(82 raw) multiplica por 13 la evaporación (`EvapBase` 1 + `EvapPorGrado` × 12; `LabAgua` :417 lee la
constante `AmbientRaw` 70, no `ambient[i]`): el fondo evapora, el vapor sube por las gargantas abiertas
y condensa en la roca fría de arriba. Un alambique de columna gratis; también sin medir.

**4.5 Determinismo entre máquinas: cero medidas.** INFORME_FINAL §G lo dice: los hashes prueban una
máquina y una build. Todo el «asíncrono por fichero» y «comparar pozos de la misma semilla» depende de
que dos PCs den los 63 hashes iguales. Es integer de punta a punta salvo `Universe.Create` (Mathf sin
trascendentes); riesgo bajo, pero es medio día con dos PCs y va antes de prometerlo.

**4.6 La vigilia ×10 con ocho tramos despiertos.** Mundo despierto: 3,04 ms/tick con 864 chunks. Ocho
hogares o fuegos mantienen la columna despierta; ×10 son ~30 ms de simulación por frame de 33: ×10 es
el techo, no el suelo.

**4.7 Lo que sí es gratis.** `ambient` por fila (un array que el constructor pinta); sellado sin
barrera (contadores por `y / 96` sobre el diario); `CorrerSello` por pozo entero; el fichero en cuanto
exista el volcado; Piel sobre `temp[]`/`humedad[]`; «el número por defecto es el de hoy» en la
promesa, que se autocalibra y evita el umbral de autor en sandbox.

## 5. La prueba más barata capaz de matarla

Media semana de banco, sin código nuevo salvo montajes y una fila más en `ArcoMuestra`, en la rejilla
actual (sin girar). Cuatro escenarios; los dos primeros deciden.

**A · «Dos tramos, honesto» (un día).** Arriba, la cámara alta con la geometría de R150 (boca de 25
columnas, la que dio luz 139-210 en la cara y 9 nacidas). Abajo, bajo una garganta de 3 celdas
alineada con la boca, **la tolva del banco** (360 celdas de fibra, boca 3: la única fuente de humo
que dura días), y aparte un horno de 255 raw bajo suelos de roca de 1, 2 y 4 celdas con un lecho de
sedimento encima de cada uno. 18 000 ticks. Por cada 1 800: fracción de caras del lecho con
`luz[i+W] < 40`, luz media del aire sobre la cara, `temp` del sedimento sobre cada suelo de prueba,
`LabPlantasNacidas/Muertas` y vivas. **Mata si** con la tolva ardiendo la fracción de caras bajo 40
no supera el 50 % durante un día entero **y** el sedimento sobre el suelo de 1 celda no sube 8 raw:
nada de lo que hace un tramo llega al sellado en tiempo de juego. **Y aunque pase**: si el
oscurecimiento termina con la tolva (≤ 8 días) y nada lo repone, la amenaza es combustible y la
dirección queda detrás de V antes de tener adversario.

**B · «Columna hidráulica» (medio día).** En la rejilla actual, tres cámaras apiladas de 96 filas
unidas por gargantas abiertas de 3, manantial arriba, sumidero de 1 y de 2 celdas abajo, `ambient`
por fila de 62 a 82. Por día: nivel del agua en cada cámara, `LabAguaEmitida`, `LabAguaSumidaU`,
`Freeze`, evaporado y condensado. **Mata en el otro sentido si** con dos celdas de sumidero la columna
inunda la cámara alta en < 20 días o si con una celda drena: entonces la hidrología no tiene ventana
jugable y toda promesa muere o vive por un número del constructor. Y dice si el 5 °C hiela.

**C · «Tapón de grava» (media tarde).** Poza de manantial (carga 40) y poza concentrada (255) sobre
un tapón de 3×4 en una garganta; por día, unidades exudadas debajo y carga media del tapón. Predicción
por lectura: con 40 se para en carga ≈ 55 y no ciega; con 255 ciega en ~80 visitas; en ambos, el
depósito lo cubre de sedimento en < 1 día. **Mata si** se cumple: «la grava se cansa día a día» no
existe sin tocar física (mueve `HashCarga`) o sin agua turbia de autor.

**D · Hashes en dos PCs (medio día, dos personas).** El banco en la build de Cesar y en otro PC.
Mata el asíncrono, no la dirección.

Si A y C matan, se descarta y sus órganos (§7) pasan a J. Si A sobrevive solo con la tolva, es
finalista **detrás de V**, con V dentro del prototipo.

## 6. Tiempos corregidos

| tramo de trabajo | qué | semanas |
|---|---|---|
| evidencia para matarla | A-D de §5, banco actual, montajes solamente | **0,5** |
| técnico paralelizable | J núcleo (3-3,5) ‖ C (1-1,5) ‖ decisión de rejilla + rotación + rebase del banco (1) ‖ hashes en dos PCs (0,1) | 5-6 de Opus, 3-3,5 de calendario |
| secuencial | rejilla → formato del volcado; diario → aforo con nueve ganchos y validador (1) → promesa, piedra y contador por tramo (0,5) → `CorrerSello` → validador por veredicto con cláusula de eternidad y bolsas (1,5) → tres tramos de campaña validados (1) | 4 de calendario, encadenadas |
| prototipo feo que permite juzgar el core | tres tramos de campaña + un pozo sorteado, muñeco heredado, piedra como sprite, F8 apagado | **7,5** de calendario (10-11 de Opus); la dirección decía 6,5 |
| iteración humana probable | 4 sesiones se vuelven 6: la primera enseñará la inundación o el tramo eterno y obligará a fijar `DiaTicks`, grosor de suelo, ancho de garganta y `Caudal`/sumidero, todos medibles en banco antes de la sesión; una reescritura de campaña | 4-5 de calendario, no de Opus |
| si A solo sobrevive con la tolva | V antes del prototipo | +2,5-3 de Opus |

## 7. Órganos a conservar si muere

1. **La línea de aforo a nivel de unidad** (nueve ganchos + Σ cruces == Δ inventario): la balanza de
   cualquier recinto con frontera, no solo del pozo.
2. **La promesa con receptor y «el número por defecto es el de hoy»**: el umbral lo pone lo que hay
   aguas abajo, autocalibrado; vale para cualquier dirección con J.
3. **Sellado sin barrera física**: SOLTAR local como contadores por región sobre el diario.
4. **La piedra rajada**: veredicto diegético legible a distancia, sin panel.
5. **Bolsas envejecidas en una corrida + validador por veredicto + cláusula de eternidad** («el
   montaje del autor sin tocar debe fallar antes del día D»): generador y filtro de cualquier campaña.
6. **El reloj «drenar o ahogarse»** (`Caudal` frente a celdas de sumidero, 14 días por tramo): el
   adversario vertical más fuerte del código, sin nombre en ninguna dirección.
7. **Tres hechos del sustrato** que hay que arreglar en el laboratorio de todos modos: la roca de
   ≥ 4 celdas es adiabática bajo el tirón al ambiente (el grosor como mando discreto); el
   truncamiento entero de `LabInfiltrarHacia` que detiene la colmatación al 22 % con agua de
   manantial; y el filo de 2 raw entre hielo y ambiente a 5 °C.
8. **El cruce «filtrar cuesta luz»**: un tapón poroso en una garganta es filtro y persiana a la vez.

## 8. Puntuaciones (rúbrica v2)

| eje | nota | por qué |
|---|---|---|
| apalancamiento_sistemico | 6 | la capa propia (aforo, tramos, promesa) es fina; el valor es de J, que comparten cinco direcciones; el acoplamiento que la distingue es hidráulico y no está diseñado |
| leyes_ejecutan_revelan_juzgan | 7 | J ejecuta y juzga; la promesa con receptor es buena forma del juicio; los relojes que harían fallar sin manos no son los que cuenta |
| iteracion_humana | 6 | `DiaTicks`, grosor de suelo, ancho de garganta, `Caudal`/sumidero, gradiente de ambiente frente al hielo, seis promesas de autor; todos medibles en banco, ninguno medido |
| verificabilidad_automatizable | 8 | todo tiene banco; el rebase de los nueve hashes y el validador del aforo son coste, no riesgo |
| simulacion_es_el_juego | 7 | la promesa es datos sobre la simulación; el examen está nombrado y no resuelto |
| onboarding_garantizable | 7 | tramos validados por veredicto es el mejor onboarding del panel si lleva cláusula de eternidad; sin ella entran tramos que no fallan |
| observabilidad | 6 | Piel, una vista, la piedra; la luz no se pinta hasta L; la piedra rajada es sprite |
| tiempo_como_apuesta | 8 | sellar = prometer y abrir la garganta = abrir el grifo son físicos y buenos |
| multiplayer_emergente | 6 | asíncrono en cuanto exista el volcado, con cero medidas entre máquinas; sellar juntos es plausible y sin evidencia |
| profundidad_por_leyes_estables | 5 | con el sustrato actual los tramos se ahogan o son eternos; depende de A y V, como admite |
| cuerpo_del_jugador | 5 | C como instrumento; lo propio del pozo (la poza que cae al abrir) es un accidente, no un sistema |
| identidad_comercial | 6 | frase y clip de la garganta que se abre; hasta L es un falling-sand en columna con cronómetro |
| dificultad_tecnica | 7 | todo acotado; lo oculto es el aforo a nivel de unidad, el rebase del banco y la decisión de rejilla |

## 9. Crítica razonada

La dirección tiene la mejor idea de juicio del panel —la promesa cuya materia recibe el tramo de
abajo, de modo que el juez es tu siguiente problema y no una tabla— y la monta sobre J, lo más barato
y verificable que dejó el catálogo. Su capa propia es pequeña y honesta: una fila que cuenta, suelos
cada 96 filas, un gesto. Desde ingeniería no pide nada refutado y casi todo tiene banco. El problema
no es lo que añade sino lo que da por existente.

Lo que hace pozo a un pozo es que lo de abajo amenace a lo de arriba y que el tiempo cobre. He leído
las dos cosas en el código. El calor no cruza roca: con `k=2, c=3`, el paso mínimo de ±1 y el tirón de
un raw cada 32 ticks, una cadena de roca transporta un raw por visita y pierde un cuarto por eslabón;
un suelo de cuatro celdas es adiabático y uno de una o dos transmite. La dirección no fija el grosor,
y ese grosor es el mando. El humo sí cruza, una celda por tick hasta la boca del cielo, pero R148 ya lo
midió: quince celdas sobre el lecho en el minuto cinco y cero después, porque el fuego se consumió; la
carbonera de boca 1 que elige para su prueba dio cero humo fuera. Lo único que humea días es la
tolva, 466 segundos, y nadie la repone hasta V. La planta viva no muere de oscuridad: `LabPlanta`
solo mata por savia. Y la luz del cielo muere en la fila 255: dos tramos y medio, no siete.

Los relojes que cuenta tampoco están. La colmatación tiene un truncamiento entero que la detiene al
22 % con agua de manantial; con agua concentrada ciega en veinte segundos; y el depósito cubre el
tapón de sedimento en menos de un día. El «sumidero que se ciega» no existe: el agua sobre él vive
ocho ticks, como dice la refutación de la balanza que la dirección cita. Los días 6, 30 y 48 los pone
`DiaTicks`. Pero hay un reloj que sí vive en esa ventana y no aparece en su core loop: el manantial
emite 24 celdas por segundo y un sumidero de una celda traga 15. Todo tramo que no drene se inunda en
catorce días de sesenta segundos, y lo que sube del tramo nuevo es el nivel del agua. Su riesgo de §6
está invertido: el tramo de agua no es eterno, se ahoga. Y quién gana, si el manantial o el sumidero,
lo deciden dos números del constructor con un umbral duro.

Hay huecos técnicos propios. El aforo no vive en `SwapCells`: tapar la garganta con grava hace cruzar
el agua como unidades de humedad por infiltración, percolación y exudación, y la presión muda celdas
de superficie a superficie sin pasar por la fila: nueve ganchos y un validador de conservación, una
semana. Girar la rejilla es un día, pero los nueve montajes del banco escriben coordenadas absolutas y
sus hashes son la licencia de optimizar: girar cuesta el banco o exige W/H no constantes con división
en todos los bucles calientes, y esa decisión va antes del volcado. El «5 °C arriba» es exactamente
el punto de fusión del hielo del laboratorio: lo que hiele arriba no funde nunca. Y nadie ha comparado
un hash entre dos máquinas, que es lo que el asíncrono por fichero promete.

La prueba de §8 mataría por montaje. La honesta cuesta lo mismo: arriba la cámara de R150, abajo la
tolva bajo una garganta alineada y un horno bajo suelos de 1, 2 y 4; diez días midiendo caras bajo 40,
temperatura del lecho y nacidas. Al lado, la columna hidráulica con uno y dos sumideros, y el tapón de
grava. Media semana. Segunda ronda, por eso: no descarto una idea de juicio que vale, pero no la hago
finalista con una narración cuyos números no salen de las leyes, con su adversario real sin nombrar
y con una prueba que se mataría sola. Si vuelve con A y B corridas y el reloj de la inundación en el
core loop, es finalista detrás de J; si A solo sobrevive con la tolva, detrás de V.
