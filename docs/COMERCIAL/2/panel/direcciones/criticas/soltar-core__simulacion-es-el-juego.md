# CRÍTICA · «SIN MANOS» (soltar-core) · lente: LA SIMULACIÓN ES EL JUEGO

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: director creativo purista de la
simulación. Leído: `soltar-core.md` entero; `01_LEYES.md` entero (con §6); `metricas-y-soltar.md` y
las dos refutaciones del sello; `03_MERCADO.md`; `01_PROPUESTAS.md` (P4) y `04_VEREDICTO.md`; la
dirección hermana `leyes-juzgan.md` y su crítica con esta misma lente; `LabBench.cs` (`Escenarios`,
`Correr`, `ArcoMuestra`); `SimStepper.Laboratorio.cs` (`LabCampos` :201-212, depósito :452-466,
`LabInfiltrarHacia` :498-520, arcilla :634-638, `LabPlanta` :801-811, `LabHogar` :917-946,
`LabSumidero`/`LabTragar` :1021-1033); `LabMateriales.EsFondo` :98; `SimStepper.cs:742` (moverse pone
`reposo` a 0); `LabParams.cs` (`DepositoReposo` 24, `DepositoUmbral` 200, `AblandaHum` 250, `HogarRaw`
170, `Caudal` 24, `GerminaPorMil` 2, `PlantaHumedadMin` 60, `PlantaLuzMin` 40).)*

**Veredicto: SEGUNDA RONDA**, con condiciones, y solo como la mitad de una fusión con «El Recibo»
(leyes-juzgan): son el mismo producto escrito dos veces. Esta versión aporta lo que a la otra le
falta (SOLTAR como medida, la apuesta juzgada, la atribución por rejugado, los primeros diez
minutos); la otra aporta lo que a esta le falta (que preparar no sea una nota, y que el banco
resuelva y valide en vez de Cesar).

## 1. Lo que hace bien: el gesto ya no prohíbe, y los diez primeros minutos son puro sustrato

La primera pasada mató P4 con dos frases. A la primera («el momento de máxima tensión es el que
prohíbe jugar») esta dirección responde de verdad: SOLTAR deja de ser un modo y pasa a ser **una
medida que la simulación produce** («último día en que todas las cláusulas se cumplieron tras el
último toque»). Nada bloquea las herramientas; el contador es un hecho del libro mayor, no un
candado. Es el mejor movimiento del panel de direcciones sobre SOLTAR y vale por sí solo.

«El hilo» es la escena de onboarding mejor escrita de las ocho direcciones y no tiene un renglón de
guion: cavas UNA celda, el hilo cambia de rumbo, el lecho se oscurece, la semilla germina (humedad
≥ 60 en el sustrato y luz ≥ 40 en el aire de encima, :688-704: verdad de código), la erosión se lleva
el lecho salvo bajo la raíz (D28 de R135), el pilar de sedimento que queda resbala como polvo, la
planta pierde el sustrato y se seca en fibra (:804-811). Cuatro leyes encadenadas, consecuencia
atribuible («yo cavé esa celda») y remate nombrable («DÍA 6 · CAYÓ: PLANTA VIVA»). Es el clip de
`03_MERCADO.md` con tres de cuatro condiciones cumplidas; la que falta (estado legible sin voz) es
render, no diseño.

Tres órganos más son honestos porque los ejecuta la simulación: la **apuesta** la juzga el sello, no
un autor; la **culpa** en co-op es rejugar el diario sin las entradas de B (una ejecución de las
leyes, no una etiqueta de procedencia); y **sellar a ciegas** y correr en banco es el único sitio del
panel donde la velocidad headless (30 días en 1,6 minutos) se convierte en verbo de jugador.

## 2. Lo que hace mal: es Incredible Machine con física de verdad, y lo confiesa

**El género honesto.** Preparar → soltar → correr → leer es el bucle de Zachtronics y de The
Incredible Machine. Ese bucle SÍ puede ser «la simulación es el juego»: en Opus Magnum la simulación
es el puzzle. Pero lo es solo cuando el vocabulario de construcción es ancho y el espacio de máquinas
es combinatorio. Aquí el vocabulario del prototipo es una mano de cuatro verbos, catorce materiales y
una dote; la mortalidad por cruce de leyes es una sola (R135/R148: el alambique que riega, ahoga y
sombrea); y el propio documento dice que «ciclos que las leyes de hoy no cierran solos» y que «el
valle sin horizonte recae en el tedio del P4 original; por eso el espinazo son las situaciones y los
sellos, y el valle es opcional». Traducido a esta lente: **la simulación sola NO es el juego**; el
juego es una escalera de doce situaciones de autor con condición, dote y umbral, más un cronómetro y
un cuestionario. Es un puzzle-campaña delgado, y delgado es mejor que grueso; pero es una capa
encima, y la autoevaluación de 8 en «la simulación es el juego» le corresponde un 5. El test de la
función objetivo («el core loop suficiente existe sin acumular features») lo suspende por confesión:
la profundidad de las horas 5-20 la fía a F (el fusible de hielo) y a V (la fibra que llega sola), es
decir, a la segunda entrega.

**El minuto típico.** Un día son 1 800 ticks: seis segundos a ×10. Una situación de diez días es un
minuto de mirar; una de cien, diez; una de trescientos, media hora. El documento enumera «cuatro
cosas, ninguna es esperar» para ese rato: mirar, apostar, sellar a ciegas y tocar. Pero apostar ocurre
ANTES de soltar, y sellar a ciegas y tocar son las dos maneras de DEJAR de mirar. Queda una: mirar con
un campo a la vez y mover tres sondas. «Decidir dónde mirar es la única decisión que no cuesta racha»
es la frase exacta de un observador, no de un jugador. Y la racha, aunque es una medida y no un
castigo, es la ÚNICA cifra que el jugador ve crecer, así que el incentivo racional durante la corrida
es no tocar: el juguete del falling-sand (tocar y ver AHORA, el placer primario de Noita y Powder
Toy) queda enterrado bajo la única nota disponible. «El tacto es gratis; tocar cuesta» es un lema
elegante y es, literalmente, el anti-Noita.

**Bifurcar está en la hora equivocada.** Lo único que convierte «mirar y esperar la nota» en jugar
es rebobinar al día N, tocar y correr otra rama (el clon en memoria que la refutación de la cuna dejó
casi gratis y que «El Recibo» pone en el centro). Esta dirección lo narra en la hora 20 («bifurcarlo
en el tick 4 000 y ver dónde diverge»). Debería estar en el minuto 5: es el mando de tiempo que los
críticos de P3 exigían y el que hace barato el «¿y si...?».

**La apuesta es un cuestionario.** «¿Qué cae primero y qué día?» es lo que el jugador piensa de
todos modos; escribirlo y que la máquina lo califique («apuesta acertada» en el veredicto) es lo que
separa jugar de rendir examen. Mini Metro y Opus Magnum no son el comparable: allí la puntuación es
lo que la máquina PRODUJO, no lo que el jugador PREDIJO. Las tres mitigaciones de §6 (veredicto
«visto», apuesta «como predicción», racha «como puntuación de jugador») son cosméticas: ninguna cambia
lo que el jugador hace con las manos.

**Toques, rachas y «apuesta acertada» son métricas de interfaz**, no productos de ninguna ley. Solo
«DÍA N SIN MANOS» y las cláusulas sobre libro y grilla los escribe la física. El resto es el diseñador
escondido en la puntuación.

## 3. Los relojes: lo que el código dice de los «sistemas mortales»

La dirección responde a la segunda frase de la primera pasada («el único sistema mortal es la
planta») con seis relojes. Leídos contra el código, uno no existe y dos dones los anulan:

- **«El sumidero es fondo (`EsFondo`, :466): una salida se ciega sola.»** No con este código. Cada
  celda se visita una vez por cada 8 ticks (`LabCampos` :208-210); depositar exige `reposo ≥ 24`
  visitas con `carga ≥ 200` (:466); moverse pone `reposo` a 0 (SimStepper.cs:742); y el sumidero
  traga sus cuatro vecinos líquidos en cada visita (:1021-1033). La celda de agua junto a un sumidero
  vive como mucho una visita y la que ocupa su sitio nace con reposo 0: **nunca deposita**. Peor: los
  finos decantan hacia el agua de ABAJO (:456-463), así que una poza turbia con el sumidero en el fondo
  es un drenaje perfecto de lo concentrado. El cegado solo puede venir de sedimento ya depositado que
  resbale hasta cubrir la boca: geometría, no reloj. La prueba §8.2 lo dirá; mi predicción es que no
  ciega.
- **El hogar es un pin eterno** a 170 raw (`LabHogar` :923 lo reescribe en cada visita), el manantial
  otro (24 celdas/s para siempre) y el núcleo frío otro; la dote son 31 celdas de frío infinito
  «racionado como dato». `01_LEYES.md` §6 lo dice sin rodeos: «con fuentes infinitas SOLTAR no
  arriesga nada por el suministro», y por eso añade **D · el hogar come** (0,5-1 sem, tuning 8) como el
  candidato con más apalancamiento por línea del panel. La dirección cuyo verbo entero es SOLTAR
  **omite D** y deja F para la segunda entrega. Consecuencia concreta: «Llueve donde hace frío» (hogar
  + serpentín de dote + manantial) no tiene reloj: goteos por día constantes para siempre, «DÍA ∞ SIN
  MANOS», situación trivial en cuanto funciona. Igual toda situación de agua clara que no pase por
  grava.

Quedan como relojes reales, con A, D, F y V fuera del prototipo: la tolva (7,8 días), la carbonera
(unos once), la grava que se colmata (`LabInfiltrarHacia` :512), la presa de arcilla que se ablanda
a 250 (:634), el canal que compacta a arcilla y la planta. Todos **monótonos**: cosas que se gastan o
se ciegan. El drama de Dwarf Fortress no viene de que la leña se acabe; viene de que tres cosas se
crucen. Aquí, en cuanto el jugador aprende los cinco relojes (hora 2-3), dimensionar la tolva para
veinte días es aritmética, y la aritmética con cronómetro es un examen.

## 4. El diseñador escondido

- **Doce situaciones de autor** con geometría (10-15 líneas), dote, umbral por cláusula, orden en la
  escalera y **registro del autor** que debe cumplir. Cesar resuelve doce puzzles; y como F y V llegan
  en la segunda entrega, la escalera validada en la primera se re-resuelve en la segunda (21 rondas de
  física en 3 días). «El Recibo» ya propone que el banco busque las soluciones; esta dirección no.
- **La dote por situación** es una economía en miniatura: cuántas celdas de frío y de arcilla hacen
  fácil o imposible cada situación. Doce números más, sin validador que los acote, y exactamente el
  tipo de balance que la función objetivo penaliza.
- **El validador caza lo trivial y lo imposible, no lo aburrido** (§6, confesado). Y el filtro «el
  registro del autor debe cumplir en el mutante» genera el mismo puzzle con otra cosmética.
- **El render que el playtest necesita y nadie presupuesta.** «La materia a simple vista: agua el
  único azul, humo que borra, tinte de turbidez» describe algo que hoy no existe (campos en F8,
  plantas de un píxel). Las tres sesiones de §7 medirían F8 si no hay 1-2 semanas de render antes.
- **La gramática de la apuesta** (qué se puede apostar, cómo se escribe «nada» y «aguanta») y el scrub
  de la repetición son interfaz con tuning de sensación.
- **Cifras honestas:** la longitud del día y «qué es agua clara». Ahí no se esconde nadie.

Iteración humana real: 6-7, no 8.

## 5. Revelar: el mundo sigue en F8, y el clip narrado

Ejecuta (el laboratorio). Juzga (cláusulas, con umbrales humanos). Revela con un halo de doce celdas,
bandas de ley y una tira del recibo: instrumentos correctos sobre un mundo invisible. M y la vista Ojo
quedan «para después».

**El clip de diez segundos, narrado con lo que el prototipo tendría.** 0-2 s: un corte de tierra, un
hilo de agua, un tallo bajo una boca de cielo, una mano-cursor que se levanta; «DÍA 1 SIN MANOS».
2-7 s: a ×10 el hilo se come la orilla grano a grano y el marrón de la turbidez se extiende, salvo bajo
el tallo. 7-10 s: el pilar resbala, el tallo amarillea, cae como fibra; «DÍA 6 · CAYÓ: PLANTA VIVA».
Consecuencia atribuible: la única celda que cavaste. Es un clip de verdad, con cadena de tres eslabones
y remate nombrable, y **exige** plantas de varias celdas, humedad como tinte y erosión visible. Con lo
presupuestado (halo, bandas, el día de L) es un lecho gris con un píxel verde.

**La frase** es la mejor del panel («la física cuenta los días que tu obra aguanta sin ti»; «SIN
MANOS» como remate) y lleva una cola de desarrollador que ningún jugador compra («guarda la partida en
un archivo de 60 KB»); y «que las leyes juzguen quién la hizo mejor» promete un escalar que la
comparación por dominancia, a propósito, no da. La cápsula no existe.

## 6. Dos direcciones, un producto

`leyes-juzgan` y `soltar-core` comparten J entero, la condición como dato, la balanza, el validador
por veredicto, la comparación por dominancia y el fichero. Difieren en el verbo (SELLAR frente a
SOLTAR como medida), en la mano con halo, en la apuesta y en el byte de autor. La otra tiene lo que a
esta le falta: **desmedir la preparación** (toques fuera de la nota), **el clon como mando de tiempo
desde el minuto 0** y **la búsqueda en banco como autor** (el banco resuelve, valida mutantes por «el
autor falla y hay otra solución» y re-resuelve tras cada paquete de física). Esta tiene lo que a la
otra le falta: SOLTAR como medida y no como modo, la apuesta juzgada, la atribución por rejugado y
«El hilo». Que compitan por separado es gastar dos jueces en una idea.

## 7. Condiciones para la segunda ronda

1. **Fusión con «El Recibo»**: un documento, un verbo (SOLTAR como medida), toques fuera de la nota,
   búsqueda de toques en banco como autor y como validador de mutantes.
2. **D entra en la primera entrega** (el hogar come, 0,5-1 sem): sin fuente finita no hay apuesta. Y
   kill test para toda situación construida sobre pins: si «DÍA N» no acota en 300 días, la situación
   no existe.
3. **Bifurcar en el minuto 5, no en la hora 20**: el clon por día es el core, no un extra.
4. **Sandbox con recibo desde el minuto 0**, no en la hora 5: el juguete primero, la condición como
   lente que el jugador enciende; la escalera es opcional, no el espinazo.
5. **Render mínimo antes del primer playtest** (plantas de varias celdas, humedad como tinte, turbidez
   visible): 1-2 semanas presupuestadas, o las sesiones miden F8.
6. **La apuesta se degrada a nota de cuaderno** (se guarda y se contrasta, no se califica en el
   veredicto) hasta que un playtest diga que calificarla divierte.

## 8. La prueba más barata que la mata (corregida)

Mantener las dos de §8 (discriminación y relojes: son buenas y baratas) con dos añadidos de un par de
días cada uno:

- **2b · Inmortalidad de los pins.** Alambique con hogar + serpentín de dote + manantial, 540 000
  ticks (300 días) con `ArcoMuestra`: goteos/día en el día 1 y en el 300. Si no cambian ±10 %, ese tipo
  de situación no tiene reloj y D es obligatorio. Y el sumidero bajo poza turbia a 300 días: si
  «claras/día» no cae a la mitad, el «cegado» de §4 es falso y hay un reloj menos.
- **3 · Coherencia bajo perturbación** (la «fragilidad» del validador, ascendida a criterio de muerte).
  Sobre el montaje del autor de «El hilo» y del arco largo, K = 30 perturbaciones de una celda: 15
  lejos del lecho y del hilo, 15 encima. **La mata**: si las lejanas cambian el veredicto o el día en
  más de un tercio de los casos (caos, R135) la apuesta es lotería y el examen no se puede estudiar; si
  las cercanas NO lo cambian, la simulación no discrimina lo que el jugador toca. Y de paso, todos los
  montajes de un toque sobre «El hilo» (la orilla tiene decenas de celdas, no miles): si la tabla
  completa cabe en una noche y todos los que cumplen son la misma máquina, el puzzle es una clave de
  respuestas.

Bench puro, un día de Opus por añadido, una noche de máquina. Total evidencia para matarla: 2 semanas.

## 9. Tiempos corregidos

| | dirección | esta crítica |
|---|---|---|
| evidencia para matarla | 1,5 sem | 2 sem (evaluador desechable de cláusulas + 12 montajes variantes + pins a 300 días + coherencia K = 30 + tabla de un toque) |
| prototipo feo | 6-7 sem en dos hilos | 8-10 sem: J en `Sim/` 3,5-4 (las dos refutaciones dicen 2,5-4) · mano nueva con cubo y cuatro verbos, halo, lector, SOLTAR, contador, apuesta, tira, veredicto, clon y scrub, bifurcar, diff, cargador de situaciones: 3-4 sem, no «una» · render mínimo 1-2 · D 0,5-1 |
| iteración humana | 8/10 | 6-7: doce situaciones resueltas y re-resueltas tras F y V, doce dotes, orden, día, gramática de la apuesta, dos rondas de legibilidad, tres sesiones |

Secuencial: gesto ← diario; validar situaciones ← `CorrerSello`; envejecer ← volcado; playtest ←
render. Paralelizable: `Sim/` de J con el gesto; D con cualquiera; render con todo.

## 10. Órganos a conservar si se descarta

SOLTAR como **medida** («último día en que todas las cláusulas se cumplieron tras el último toque»),
nunca como modo; la cláusula que cae con nombre y día como remate; la apuesta como nota de cuaderno
juzgada por el sello; byte de autor en `Intervencion` y **atribución por rejugado sin las entradas de
B**; sellar a ciegas y correr en banco (`tickSellado`, «días vistos»); el halo de la mano y el lector
en bandas («el tacto es gratis»); la dote como dato de situación hasta que F la vuelva hielo; situación
= montaje de 10-15 líneas + condición JSON; las pruebas de discriminación y relojes como benchmarks
permanentes; el validador por veredicto; «El hilo» como primera situación de cualquier dirección que
gane.

## 11. Rúbrica v2

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | J no añade física; convierte contadores en juicio; omite D, la ley más barata con más palanca sobre SOLTAR |
| las leyes ejecutan, revelan y juzgan | 6 | ejecutan y juzgan; los umbrales y la dote son humanos y toques/rachas/apuesta son métricas de interfaz; revelan con instrumentos sobre un mundo invisible |
| iteración humana (10 = poca) | 6 | doce situaciones con dote y umbral, re-resueltas por entrega de física; render sin presupuesto; tres sesiones |
| verificabilidad automatizable | 9 | todo en banco: hashes, round-trip, discriminación, relojes, coherencia; solo «si mirar es placer» pide personas |
| la simulación es el juego | 5 | el valle es «tedio» por confesión; el juego es la escalera, el cronómetro y el cuestionario; el juguete de tocar queda bajo la única nota |
| onboarding garantizable | 8 | «El hilo» es el mejor onboarding del panel y es puro sustrato; escalera validada sin personas |
| observabilidad | 5 | halo, bandas y tira; mundo en F8; M y Ojo después; plantas de un píxel |
| tiempo como apuesta | 9 | es el verbo entero y como medida es la versión correcta; un punto menos por los pins que vuelven inmortales situaciones enteras |
| multiplayer emergente | 6 | relevo por fichero sin roles y culpa exacta por rejugado; nada que hacer juntos en tiempo real |
| profundidad por leyes estables | 5 | relojes monótonos y un cruce; 100-300 días fiados a V y F |
| cuerpo del jugador | 2 | ninguno por decisión; halo en el cursor |
| identidad comercial | 7 | la mejor frase y el mejor remate del panel, con cola de desarrollador; cápsula inexistente; arquetipo colonia (mejor suelo) pero producto de puzzle-campaña |
| dificultad técnica (10 = fácil) | 8 | acotado; el determinismo entre máquinas se detecta; clon, scrub y búsqueda son extras |

**Posición:** transformar por fusión, no descartar ni finalista. A la pregunta final: las leyes
EJECUTAN solas; JUZGAN solas lo que una cláusula nombra, pero quien nombra, dota y umbraliza doce
situaciones sigue siendo un autor; REVELAN con un halo sobre un mundo que no se ve. Y el jugador de
esta dirección, en su minuto típico, no juega con la simulación: la mira correr y espera la nota. Eso
se arregla desgravando el toque, poniendo el clon en el minuto 5, encendiendo el recibo en el sandbox
desde el minuto cero, metiendo D y pintando el mundo; con «El Recibo» al lado, la mitad ya está
escrita.
