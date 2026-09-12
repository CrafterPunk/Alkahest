# CRÍTICA · «EL RECIBO» (leyes-juzgan) · lente: LA SIMULACIÓN ES EL JUEGO

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: director creativo purista de la
simulación; único tema: ¿la simulación ES el juego o hay un juego tradicional encima?, ¿el core loop
suficiente existe sin acumular features?, ¿las leyes ejecutan, revelan y juzgan solas o hay un
diseñador escondido en la puntuación, la progresión o el contenido?, ¿es divertido minuto a minuto o
es un examen?, ¿cómo se narran el clip y la frase? Leído: `leyes-juzgan.md` entero; `01_LEYES.md`
entero (con §6); la lente `metricas-y-soltar` y las refutaciones de apalancamiento del sello y la
balanza; `03_MERCADO.md`; las críticas hermanas de ingeniería e iteración humana de esta dirección;
`LabBench.cs` (`Escenarios` :76-87, `MontarCarbonera` :135-143, `Correr` y su única intervención
:301-311, `ArcoMuestra`); `SimStepper.Laboratorio.cs` (libro mayor :49-118, `LabSumidero`/`LabTragar`
:1021-1033, `LabRespira` :1054-1063, depósito sobre fondo :455-470); `SimStepper.cs` (carbonización
:919-926); `Universe.Laboratorio.cs` (Fibra, Carbon, Semilla: todos `Powder`); `LabParams.cs`
(`RendimientoCarbonPct` 25, `GerminaPorMil` 2, `PlantaLuzMin` 40, `DepositoReposo` 24).)*

**Veredicto: SEGUNDA RONDA**, con cinco condiciones que la reescriben sin cambiarle el nombre ni
pedir una línea de física nueva. Es la dirección del panel que menos juego tradicional pone encima
de la simulación y, tal como está escrita, la que más se parece a un examen. Las dos cosas son
ciertas a la vez, y la distancia entre ellas son decisiones de diseño de pocas líneas, no semanas.

## 1. Lo que hace bien: se niega a poner un juego encima

De las ocho direcciones, esta es la que con más honestidad rechaza la capa externa: no hay
puntuación escalar, ni economía, ni estrellas, ni progresión que no salga del banco. El «pedido»
solo nombra una columna; el histograma sustituye a la nota; la campaña la ordena la propia física
(la **firma de leyes**: perturbar `LabParams` por grupo y ver qué mueve el veredicto es la ley
juzgándose a sí misma). Las piezas que la sostienen están medio escritas: el libro mayor tiene
unos cuarenta contadores que nadie juzga (:49-118), `LabBench.Correr` ya contiene una intervención
tick-estampada (la caldera del alambique, :301-311) y `ArcoMuestra` ya cuenta el mundo en tramos de
1 800 ticks. Sellar, rejugar y comparar por dominancia métrica a métrica es exactamente lo que la
función objetivo pide, en C# puro y con prueba de banco por pieza.

Tres órganos son puros porque los ejecuta la simulación y no un autor: **bifurcar desde el clon de
cualquier día** (el «¿y si?» cuesta un segundo: es el mejor dispositivo anti-examen del panel y el
mando de tiempo que P3 pedía); **la atribución por rejugado sin las entradas de B** (la culpa en
co-op es una ejecución de las leyes, no una etiqueta); y **la vista de diferencia** (el hash hecho
visible: la consecuencia atribuible del clip, escrita por un `diff` de `mat[]`).

## 2. Lo que hace mal: el examen de tres toques

**El diario es la máquina, y eso grava el juguete.** En Opus Magnum el minuto a minuto es CONSTRUIR
con un vocabulario enorme y la corrida es el premio; el jugador experimenta gratis en el editor y
solo la máquina final se puntúa. Aquí «cada toque es una entrada tick-estampada» desde el minuto
cero y **toques** es una de las tres columnas de estilo: la dirección cobra por cada experimento de
la fase de preparar, que es justo el placer primario del falling-sand (tocar y ver AHORA). Con el
largo del diario como nota, dos de las tres columnas (toques y huella) premian no hacer nada y
dejar que el motor trabaje. Es la firma de un examen: el jugador aporta elegir qué tres celdas
quitar; el resto lo hace la simulación sola.

**La proporción construir/mirar está invertida respecto a todo comparable que funciona.** Los diez
primeros minutos narrados son: minuto 1, tres toques; minutos 2-4, mirar una pila humear a ×10;
minuto 4, un toque; minuto 6, una idea; minuto 8, SELLAR; minuto 10, la tabla. Un minuto de
construir por tres de mirar. Y lo que se mira es una carbonera a ×10: un borrón negro que humea en
una cámara que, según el propio documento, «se oscurece sola» porque el humo come la luz. Ese
oscurecimiento existe solo en F8: el fuego escribe `luz = 255` y nadie pinta `luz` en pantalla
(hecho 2 de `01_LEYES.md`). La situación insignia se vuelve MENOS legible conforme corre.

**«No hay aprobado» es verdad en la letra y falso en solitario.** El histograma con la barra del
autor (51 carbones, 3 toques, día 4) es la nota mientras no haya amigos con la misma versión de la
física y ficheros cruzados a mano. La dirección apuesta a que «la vara son los amigos»; para el
jugador de la demo la vara es el autor.

**El sandbox llega en la hora 20.** El documento reivindica que «el verbo es construir máquinas de
materia, no resolver, y que el sandbox sin pedido existe desde el día uno» (§10) y narra el sandbox
en la hora 20 (§2). Las dos frases no pueden ser ciertas.

## 3. El espacio se enumera, y la prueba de §8 mide un mando

Si la máquina válida tiene uno a tres toques, el banco recorre el espacio entero. `MontarCarbonera`
tiene unas 85 celdas de muro tallables (perímetro de 22×24 menos 440 de interior, menos hogar y boca).
Los 85 montajes de un toque a 9 000 ticks (~15 s cada uno a 1,7 ms/tick) son 21 minutos; los ~3 570
de dos toques, unas 15 horas de máquina. Si una noche de banco produce la tabla completa de recibos,
la clave de respuestas existe antes de que nadie juegue y el humano es un bucle `for` lento. Opus
Magnum vive porque su espacio es astronómico; el de esta dirección lo acota el vocabulario (cincel,
frasco, catorce materiales, verbos sin fijar según la crítica de iteración humana) y la métrica que
premia tocar poco.

Por eso el barrido de §8 (boca × T × tapón = 48 corridas) mide lo equivocado: varía los parámetros
de UNA estrategia (quitar un tapón en el tick T) y su dispersión mide sensibilidad al tiempo, no
riqueza de estrategias. Puede «confirmar» una frontera que es la curva de un mando. Y tiene un suelo
de ruido que la prueba no resta: cada celda ahogada tira `ChancePercent(25)` con sal 632
(`SimStepper.cs:919-926`); sobre 400 celdas, σ ≈ 8,7 carbones (CV 9 %). Dos máquinas idénticas
corridas una celda más a la derecha difieren un 9 % en la columna que manda. Un recibo que no
publica su propio ruido miente en el histograma.

## 4. El diseñador escondido

- **El pedido lleva número.** «60 carbones», «el día 20»: un umbral de autor por situación, que el
  validador (vacío falla, autor cumple) hace *no trivial* pero no *interesante*. Se automatiza
  cambiando su naturaleza: el pedido es **el recibo de una máquina de referencia** (la del autor o
  la mejor de la búsqueda), comparado por dominancia; nadie teclea un número.
- **El registro del autor** es la única prueba de existencia de solución y caduca con cada cambio
  de física (21 rondas en 3 días; L, V, M y C por delante): una cinta de correr de re-resolver
  12-20 situaciones por paquete. Se automatiza: la búsqueda exhaustiva de §3 vuelve a encontrar la
  frontera tras cada cambio; el «autor» pasa a ser el banco.
- **El validador de mutantes está al revés.** «El registro del autor debe cumplir» filtra los
  mutantes que la MISMA solución resuelve: el mismo puzzle con otra cosmética, y los histogramas
  por mutante colapsan sobre la barra del autor. El mutante que vale es aquel donde el registro del
  autor FALLA y la búsqueda encuentra otra solución.
- **Los dones son pins infinitos** (hogar a 170 raw eterno, manantial a 24 celdas/s eterno). Un
  juez que se dice «las leyes» con una mano infinita del autor en el mundo: toda situación de sostén
  sobre pins («planta viva el día 20» con alambique) tiende a «DÍA ∞ SIN MANOS» en cuanto funciona.
  D («el hogar come», 0,5-1 sem) no está en el núcleo y debería.
- **Cifras honestas:** la longitud del día y la raya de «clara» sobre el histograma de `carga`.
  Ahí no se esconde nadie.

## 5. Revelar: el recibo es una tabla

Las leyes ejecutan (ya) y juzgan (balanza y cláusulas), pero REVELAN por columnas de números. La
dirección hereda campos en F8 y plantas de un píxel y, al hacer del recibo el juego, es la que menos
necesita arreglarlo y la que menos lo arregla (se autoevalúa 6 en observabilidad y lo acepta). Contra
el clip de `03_MERCADO.md`: la condición 1 (estado legible sin voz) se incumple (el borrón); la 2
(cadena de tres eslabones) ocurre en física y no en pantalla; la 3 (consecuencia atribuible) la cumple
la vista de diferencia; la 4 (remate nombrable) la cumple «DÍA 12 SIN MANOS», que es la mejor frase
de la dirección. El «GIF del sello» a ×10 dura tres minutos; un GIF de diez segundos pide render
offline a ×180 (barato con `CorrerSello`) y el contenido sigue siendo el borrón. Sin L (vista Ojo,
pintar `luz`) el clip es una tabla.

## 6. Una alternativa purista a la balanza: el recibo como lugar

La única física nueva de la dirección (bit `entregable`, tragar solo desde arriba) existe porque el
sumidero es un contador. Hay una versión con cero física nueva: la salida es un **pozo** (una
cámara sellada de `Empty` bajo el agujero). Lo que cae, cae de verdad y se queda: el carbón se apila
negro, la ceniza gris encima, el agua se remansa con su tinte de turbidez, la fibra cruda que cayó
sin arder está ahí como fibra. El recibo es «material en región» (una cláusula que la dirección ya
tiene) y se lee sin F8: el pozo que se llena de negro es el estado legible del clip, y la capacidad
finita del pozo es el fin natural de la situación. Añade un cruce que nadie escribió: una brasa que
cae con el carbón puede prender la bodega (ignición del carbón 280 °C), y tu recibo arde. Pierde el
tapón de fibra del minuto 6 (la fibra, `Powder`, cae por el agujero en vez de apoyarse en el
sumidero) y deja el sumidero para los líquidos del laboratorio. Es una tarde de banco decidir cuál
de las dos salidas se usa; la que se ve sin panel es la del purista.

## 7. Condiciones para la segunda ronda

1. **La máquina es el diff, no el diario.** Coste = huella al sellar (celdas de categorías sólidas
   que difieren del nacimiento en el tick del sello: el «area» escrito por las leyes); toques = solo
   los que rompen el sello (ya existe como «días sin manos»). El diario sigue completo para rejugar
   y atribuir, pero no es nota. Así preparar vuelve a ser juguete y las máquinas pueden ser grandes.
2. **Sustituir el barrido de §8 por la búsqueda exhaustiva** de 1-2 toques con gemelos (±1 celda)
   y frontera por clase estructural; la misma búsqueda produce la referencia, valida mutantes por
   «el autor falla y hay otra solución» y re-resuelve tras cada paquete de física. Y para el
   solitario, el histograma sintético de registros sorteados (K = 200-1 000) como vara: «¿lo hiciste
   mejor que el azar?» es una pregunta que las leyes responden.
3. **El pedido como recibo de referencia**, no como número; **verbos fijados antes del barrido**
   («nada crea materia: solo quitar y mover», con la dote de la situación como pila finita).
4. **Sandbox con recibo corriendo desde el minuto 0**, no en la hora 20: el juguete primero, el
   pedido como lente que el jugador enciende.
5. **Pintar lo que ya se calcula antes del primer playtest**: `luz` en pantalla (dos días; el campo
   existe), tinte de turbidez y el pozo si se adopta. Sin eso, la prueba con cinco personas mide una
   tabla y no un mundo. L (remedir Q16, Ojo) detrás.

## 8. La prueba más barata que la mata (corregida)

**Semana 1, banco, sin sello.** Salida mínima (bit `entregable` o pozo: un día) + intervención
genérica del banco generalizando la caldera (:301-311, hoy cableada al nombre del alambique: horas)
+ **enumerador**: todos los montajes de 1 toque (85 corridas, 21 min) y de 2 toques (~3 570, una
noche larga) sobre la carbonera con salida, cada uno con su gemelo (+1 celda en x). Se mide carbón
entregado, fibra cruda entregada, tick del primer carbón, calor, y la dispersión intra (gemelos) e
inter (entre clases). **La mata** si la frontera de Pareto tiene ≤ 2 puntos no dominados de clases
estructurales distintas (pared, suelo, techo, entre hogar y pila, tapón), o si un montaje de 2
toques domina a todos en las tres columnas, o si inter/intra < 1,5 (la frontera es el dado del 25 %).
**La confirma** una frontera con ≥ 4 clases y inter/intra ≥ 3. Cuesta 3-4 días de Opus y una noche
de máquina, y tiene segundo uso: la misma búsqueda es el generador de referencia y de contenido.

**Misma semana, coste cero:** tres personas miran la carbonera del banco correr a ×10 en el editor
con `LabCarbonizado` subiendo (existe hoy). Si nadie quiere verla dos veces, «el sello es el GIF»
muere antes de escribir el sello; y entonces el salto headless al día N (90 s por 30 días) deja de
ser opción y pasa a ser el verbo, con lo que el juego ES la fase de preparar, y la condición 1 deja
de ser condición y pasa a ser todo.

## 9. Tiempos corregidos

| | dirección | esta crítica |
|---|---|---|
| evidencia para matarla | 2-3 días | **1 semana** (3-4 días de Opus + una noche de máquina + media jornada de personas) |
| prototipo feo | 5-6 sem calendario | **6-8 sem**: el gesto (histograma, línea de días, diff, carpeta) en IMGUI son 2,5-3 sem, no 1,5; más 2-3 días de pintar `luz` y turbidez, sin lo cual no se juzga el core |
| iteración humana | 6-8 días en 8 sem | 12-16 días tal como está (autor resuelve y re-resuelve 12-20 situaciones por paquete de física, pedidos con número, verbos sin fijar, dos rondas de legibilidad); 6-8 si la búsqueda y el histograma sintético sustituyen al autor |

Secuencial: diario → volcado → `CorrerSello` → validador → firma (una cadena de cinco semanas).
Paralelizable: salida, clon, cielo, bandas, gesto, pintar `luz`. Humano incomprimible: la tarde del
tedio, dos rondas de legibilidad, resolver una vez lo que la búsqueda no cubra.

## 10. Órganos a conservar si se descarta

Sello (diario bajo las cinco puertas con id de gesto y byte de autor, volcado/carga como primer
fichero de partida, `CorrerSello` con condición como dato, hash de versión); salida con juicio
(bit `entregable` + tragar desde arriba, o el pozo); huella al sellar como «area» escrita por las
leyes; «DÍA N SIN MANOS» como columna; clon en memoria y bifurcar por día; validador por veredicto
corregido al revés; búsqueda exhaustiva de toques con gemelos como generador de referencia, de
contenido y de suelo de ruido; histograma sintético como vara sin personas; firma de leyes; cielo
por geometría; `LabBandas` como única fuente de umbrales; vista de diferencia; atribución por
rejugado sin las entradas de B.

## 11. Rúbrica v2

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | una física nueva (o ninguna, con el pozo) que convierte cuarenta contadores en juicio; la profundidad no la crea el sello, la crean las leyes que vengan y un vocabulario de máquina que hoy es cincel y frasco |
| las leyes ejecutan, revelan y juzgan | 7 | ejecutan y juzgan de verdad; revelan por números; el número del pedido, la barra del autor y los pins infinitos son manos humanas hasta que la búsqueda y D las sustituyan |
| iteración humana (10 = poca) | 6 | sin balance, cierto; escondida: resolver y re-resolver cada situación por paquete de física, pedidos con número, verbos sin fijar, dos rondas de legibilidad |
| verificabilidad automatizable | 9 | todo en banco; con la búsqueda exhaustiva, hasta la referencia, el contenido y el suelo de ruido |
| la simulación es el juego | 6 | el core es sellar y leer lo que la física escribió; pero preparar está gravado, el sandbox llega en la hora 20 y el marco (situación + pedido + barra + histograma) es un puzzle de optimización clásico; 8 con las condiciones 1-4 |
| onboarding garantizable | 7 | firma de leyes y ruinas generadas son automatizables; que se lea sin texto no está probado |
| observabilidad | 5 | recibo por bandas y diff; `luz` calculada y no pintada; el borrón a ×10; el pozo lo subiría a 7 |
| tiempo como apuesta | 9 | SELLAR es el verbo; el clon quita el castigo sin quitar la apuesta |
| multiplayer emergente | 6 | mesa, relevo y tabla por fichero sin roles; culpa por rejugado; nada que hacer JUNTOS en tiempo real |
| profundidad por leyes estables | 5 | escala con cada ley nueva (cada una añade columnas), pero el espacio por situación es enumerable mientras la métrica premie tocar poco; la frontera se mide, no se discute |
| cuerpo del jugador | 3 | cursor y herramientas heredadas; el cuerpo entra como sensor al final |
| identidad comercial | 6 | «SIN MANOS» es remate y «máquinas de arena viva que se juzgan solas» es frase; el clip es una tabla sin L o sin el pozo; el nicho de puzzle tiene la menor mediana |
| dificultad técnica (10 = fácil) | 8 | C# puro y acotado; lo abierto es el gesto |

**Posición:** transformar, no descartar. Las críticas de ingeniería e iteración humana la hacen
finalista con puerta; desde esta lente le falta una reescritura que no cuesta física: que la
máquina sea el diff y no el diario, que la referencia la produzca el banco y no un número, que el
sandbox esté en el minuto cero y que el mundo se vea. Con eso, y fundida con los órganos de SOLTAR
como medida de `soltar-core`, es la dirección que mejor responde a la pregunta final: las leyes
EJECUTAN solas, JUZGAN solas lo que una cláusula nombra, y REVELAN cuando lo que juzgan se ve en el
mundo y no en una tabla. Tal como está escrita, el jugador de su minuto típico no juega con la
simulación: la mira correr y espera la nota.
