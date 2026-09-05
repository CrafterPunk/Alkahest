# 04 · VEREDICTO

*(Fable 5.1, 2026-09-05. El juicio que Cesar pidió: qué descarto, qué pasa a segunda ronda, qué
construiría primero, con qué evidencia y con qué incógnitas. No es arquitectura técnica ni plan de
implementación: es la elección del juego y la puerta que hay que cruzar antes de construirlo.)*

## 1. La decisión, en una tabla

| propuesta | veredicto | en qué queda |
|---|---|---|
| **P1 El Pozo** | **construir primero**, tras la puerta de §5 | el juego: pozo vertical persistente, tres verbos, tres estratos, estaciones; con los injertos de §3 |
| P2 La Ladera | **segunda ronda: la geometría de reserva** | si la vertical falla en banco (§5, G1-G3), El Pozo se tumba y se convierte en La Ladera sin cambiar nada más; sus órganos (cuaderno falsable, turnos de agua, asíncrono por fichero, Obras) se injertan desde ya |
| P6 Claraboya | **segunda ronda: la dirección de arte y la cápsula alternativa** | el censo de vida, los bichos-sensor, las semillas raras y «la humedad como color» entran en El Pozo; la cápsula cozy es una opción de marketing que se decide con el arte hecho |
| P3 Un Año Después | **degradado a formato**: no es el producto, es la demo y un modo | «una cámara, dos horas, un año» es la demo de Next Fest de El Pozo (una cámara del pozo) y el modo «sellar el pozo» de las estaciones; su frase es la mejor de las seis y se presta |
| P4 Sin Manos | **descartado como producto** | se queda SOLTAR: el contador «días sin manos» como medida opcional y como reto de comunidad; y los instrumentos que las leyes ya producen |
| P5 Sordina | **descartado como producto** | se queda como **especificación de observabilidad**: sondas de materia, gramática de vibración, «todo avisa antes de cambiar», la vigilia (ESPERAR), los seres lectores como contenido tardío |

No cierro con «todas podrían funcionar». Dos no deben construirse como juego (P4, P5), una no debe
ser el producto (P3), y entre las tres que quedan elijo una y digo bajo qué condición medible cambiaría
a la segunda.

## 2. Por qué El Pozo y no La Ladera (la decisión real)

Son la misma familia (sandbox sistémico persistente con cuerpo, tres jugadores por geografía, la
intuición de Cesar) y la rúbrica las separa por 6 puntos de 270, que es ruido. La diferencia es el
eje del mundo, y la evidencia del laboratorio favorece la vertical:

- **Los flujos mejor medidos son verticales.** Agua 5/5 (conservación, presión por cuerpos
  conectados, sifones) baja; humo y vapor suben; la luz del cielo «baja casi sin perder» por la
  vertical de la boca (R148). En un pozo cada par de jugadores queda conectado por física sin diseñar
  nada. En la ladera el crítico tiene razón: **Manantial es una válvula** y Hondo tiene los juguetes;
  la asimetría no la cura el diseño, la cura la gravedad.
- **La luz como común solo es drama si es escasa.** En superficie con cielo abierto la luz sobra
  (Q16: 139-210 con una boca ancha) y el huerto de la solana deja de ser un conflicto. En el pozo la
  luz es **una columna** que quien pone techos roba: el mejor conflicto del laboratorio (R148: el
  aparato que riega es el que hace sombra) se convierte en el conflicto entre personas.
- **La identidad en diez segundos es más nítida**: tres casitas a tres alturas, agua que cae, humo
  que sube, lluvia en medio. La ladera es un valle en sección, que ya existe en varias tiendas.
- **La expresión tiene dirección**: florecer hacia arriba, el huerto del Fondo, jardines colgantes en
  la lumbrera. Es la meta emblema que ninguna otra propuesta tiene y la que Cesar pone al final de su
  progresión (manual → conocimiento → control → automatización → expresión).
- **El coste de rejilla es el medido**: 256×864 son las mismas 221 k celdas. Lo que no está medido
  (luz por 600 celdas, humo entre estratos, un cuerpo de agua alto) se mide en banco en días, no en
  meses (§5).

Lo que La Ladera tiene mejor y me llevo: el cuaderno quemado con afirmaciones falsables (después de
la primera hora), los turnos de agua con compuertas-piedra, las Obras como condición, el cuerpo del
avatar como instrumento y, sobre todo, **el modo asíncrono por fichero**: tres amigos, un pozo, el
archivo circula, sin red. Es casi gratis con el determinismo ya medido y es la prueba de tres más
barata que existe.

**Condición de cambio, medible.** Si en banco (G1-G3 de §5) una lumbrera recta de 300-600 celdas
no entrega luz ≥40 abajo, o el humo persistente no cruza dos estratos en tiempo útil, o un cuerpo de
agua de 800 celdas de alto rompe el presupuesto por tick, **La Ladera gana ese mismo día** con todo lo
demás igual. No hay una tercera opción entre las dos.

## 2b. Lo que el mercado y las críticas tardías cambian, y lo que no

Los tres investigadores de mercado y los ocho críticos de las lentes vertical, jardín, asimétrico y
percepción llegaron después de escrito el veredicto. Cuatro cosas cambian; la decisión no.

- **El mercado recomienda otra cosa y hay que decirlo.** Por valor esperado, el investigador de
  ventas elegiría **colonia como producto y expediciones como demo**: el mejor suelo y la mejor
  mediana (base 120-300 k frente a 100-250 k del sandbox co-op), y la distancia más corta al motor.
  Mantengo El Pozo por tres razones, y las tres son falsables: (1) el bucle de colonia (automatizar,
  quitar las manos, contar días) está **dentro** de El Pozo como progresión de las 20 horas y como
  SOLTAR, no fuera; (2) el arquetipo co-op es el único donde el sustrato tiene un diferenciador que
  nadie ha capturado (ninguna sim de fluidos ni ningún falling-sand comercial ofrece co-op oficial, y
  la demanda existe en mods); (3) el techo del co-op es el más alto (500 k-1 M) y el precio es el
  riesgo técnico, que la puerta mide en semanas. Y adopto entera la mitad de su consejo: **la demo es
  P3**.
- **El co-op no salva un bucle flojo** (Nightingale) y **un mundo persistente que exige a varias
  personas muere de servidores vacíos hacia el día 15** (Eco). Consecuencia para El Pozo, ahora
  explícita: se juega bien en solitario desde el día uno (las ruinas amables son los vecinos ausentes),
  el modo asíncrono por fichero no necesita a nadie conectado, y el host-jugador no bloquea el pozo de
  los demás. Sin eso, la reseña dice «servidor muerto».
- **La condensación es local.** Los dos críticos de Sima señalan que la regla medida (el vapor
  condensa en el vecino condensable más frío) hace improbable que el vapor del Fondo llueva en la
  Boca; «un pozo que respira» puede ser tres sandboxes apilados. Lo acepto: el alambique repartido deja
  de ser un pilar y pasa a hipótesis de G2, las cámaras distan 70-100 filas y no mil, y **la culpa
  entre estratos se apoya en lo medido**: el agua que baja (conservación y presión), la luz de la
  lumbrera (el techo de uno deja ciego al de abajo) y el humo que sube y persiste (medido en cámara;
  su alcance es lo que G2 mide).
- **«Oscuro» significa dos cosas** en un mundo enterrado: húmedo y sin luz, las dos variables que
  deciden la germinación. G6 incluye desde ahora una lámina que las separe («¿está mojado o está a
  oscuras?»), y la semilla que dice **por qué** falló (se hincha y se pudre si está mojada y a oscuras;
  palidece si está seca; idea de Punto de rocío) entra como injerto.
- **El único «construir» del panel** lo dio un crítico de producción a una percepción con avatar y
  doce estancias apiladas en un pozo: estructuralmente es El Pozo con otra frase, y su condición es la
  misma puerta. Lo leo como convergencia, no como competidor.
- Las cuatro condiciones del clip de diez segundos (`03 §4`) pasan a ser criterio de G6 y G7: estado
  legible sin voz, cadena de tres eslabones, consecuencia atribuible, remate nombrable.

## 3. Los injertos (lo que El Pozo toma de las otras cinco)

| de | qué | por qué |
|---|---|---|
| P3 | **la demo**: una cámara del pozo, dos horas, «sellar» y ver un año a ×10; la frase «todo lo que hagas aquí seguirá pasando cuando te vayas» | es el formato de Next Fest perfecto (contenido acotado, gancho en un tuit) y no compromete el producto |
| P3 | el modo **estación sellada**: cuando el grupo no está, el pozo puede correr una estación a ×10 y enseñarla como time-lapse al volver | convierte la persistencia en veredicto sin exigir un corredor de años para el juego entero |
| P4 | **SOLTAR** y «DÍA N SIN MANOS» como medida opcional del pozo y reto de comunidad | es la puntuación que se lee en diez segundos y la única que mide «produce sin volver a tocar» |
| P4, P5 | **instrumentos que las leyes ya producen** (terracota = termómetro de máxima, grava colmatada = registro de turbidez, rocío = higrómetro) y **sondas de materia** (fibra ¿≥130?, carbón ¿≥200?, bolita de arcilla, semilla, frasco frío) | observabilidad sin panel con cosas del mundo; barata; y las sondas enseñan escala sin números |
| P5 | «**todo avisa antes de cambiar**» como promesa de diseño (ninguna transición sin señal previa) y la gramática de vibración + tono | es la regla que convierte al espectador en observador; se prueba en láminas antes de cualquier shader |
| P5 | ESPERAR como **vigilia** (×10 con la imagen desaturada: mejor lectura a cambio de no actuar) | resuelve «qué hace el jugador mientras la tolva arde 466 s» sin panel |
| P6 | el **censo de vida** como medida del huerto; bichos-sensor; semillas raras como condiciones; «la humedad como color» | da al eje verde una puntuación legible y una dirección de arte que un público cozy reconoce |
| P2 | cuaderno falsable, turnos de agua, Obras, cuerpo como instrumento, asíncrono por fichero | dicho en §2 |
| Sima (P1) | **huellas** de 8 bits por celda (mancha de goteo, marca de marea, hollín) y el legado pre-simulado | una causalidad se lee después de ocurrida; el pozo abandonado es la ruina amable del siguiente (R60) |

Nada de esto se implementa ahora. Es la lista de lo que el juego elegido **es**, para que la puerta de
§5 pruebe lo correcto.

## 4. Evidencia del laboratorio que sostiene la elección (y la que la amenaza)

**Sostiene.**

- Los cuatro comunes (luz, agua, calor, aire) se reparten por geografía y no caben en un inventario
  (`00 §3`); un pozo los apila.
- R135 y R148: dos veces, con mecanismos distintos, el aparato que resuelve un problema crea el
  siguiente sin que nadie lo escribiera. Es el motor de historias de El Pozo y ocurre entre cámaras.
- Fuego 4/5 con dos mandos legibles (recinto y contacto): tapar, destapar, encerrar; sin fuelles que
  no existen.
- Tolva 466 s, carbonera por contacto, horno 18/18 frente a 0: aparatos que son geometría y que el
  jugador puede descubrir por error.
- ×10 sostenido y chunks dormidos: estaciones, vigilia, legado y demo caben en el presupuesto.
- Determinismo en una máquina: asíncrono por fichero, repeticiones, GIF.
- Turbidez replicada gratis y cuantización a 3-4 bits que coincide con las bandas perceptivas: la
  ruta A basta.

**Amenaza.**

- El huerto nunca vivió en el nivel de referencia (causa doble aislada en Q16: luz y reparto del
  riego, las dos de nivel). Todo el eje verde es hipótesis hasta G4.
- Nada produce sin volver a tocarlo: faltan alimentación no manual, recogida y ciclo cerrado; y el
  ciclo necesita la fibra recogible, que es física nueva (G5).
- Nadie ha jugado. Diversión minuto a minuto: 4 en todas las críticas.
- Humo y vapor a larga distancia sin medir; la luz por lumbrera larga sin medir; el coste de agua alta
  sin medir (G1-G3).
- La capa perceptiva no existe y **no tiene fallback** si no se lee (G6 y su regla).

## 5. La puerta: lo que hay que hacer antes de construir nada

Cuatro a seis semanas, en banco y en papel, sin arte, sin shaders, sin red, sin arquitectura. Cada
experimento tiene una regla de decisión. El orden importa: los tres primeros deciden P1 frente a P2; los
dos siguientes deciden si existe el eje verde; los dos últimos deciden si se lee y si se juega.

| # | experimento | cómo | dura | regla de decisión |
|---|---|---|---|---|
| G1 | **luz por lumbrera** | banco headless: lumbrera recta de 300 y 600 celdas sobre un lecho; medir luz en la cara | una tarde | luz ≥40 abajo → P1 sigue; si no → P2 |
| G2 | **humo y vapor entre estratos** | banco: fuego en el estrato bajo, huerto dos estratos arriba, medir luz del lecho y llegada del vapor a una vena fría alta | dos días | el humo llega y oscurece en ≤10 min → P1; si se queda local → P2 (y la ladera tampoco tendrá humo entre terrazas: aceptarlo) |
| G3 | **coste vertical** | banco: rejilla 256×864 con un cuerpo de agua de 800 celdas de alto y tres zonas activas; ms/tick | un día | ≤3 ms/tick sostenido con tres zonas → P1; si no, estratos más bajos o P2 |
| G4 | **huerto vivo** | banco: lecho de sedimento sobre grava, riego lateral (acequia o goteo desviado), luz de lumbrera; 30 min sin tocar | una semana | humedad 60-99 y luz ≥40 sostenidas, plantas vivas a los 30 min → sigue; si no vive en ninguna geometría (vertical ni superficie) → **el eje verde no es un sistema** y el producto pasa a ser P3 con cámaras de agua y fuego |
| G5 | **ciclo cerrado con fibra recogible** | **la única descongelación de física**: la planta muerta o cosechada deja fibra; correr huerto → fibra → tolva → alambique → huerto en banco con cesta como stub | dos semanas (Opus) | cierra ≥1 vuelta sin manos → «produce sin volver a tocar» existe; si no cierra → SOLTAR se cae y el juego es de mantenimiento, no de automatización (se decide si eso basta) |
| G6 | **lectura sin panel** | láminas fijas y tres GIF hechos con una LUT sobre capturas (sin shader): «señala lo más caliente», «¿dónde va a llover?», «¿qué agua está sucia?»; cinco desconocidos | dos días | ≥4 de 5 aciertan en las tres → la piel basta; si no → **antes de seguir se diseña un fallback diegético** (una lente o un ser que lee, nunca un HUD) |
| G7 | **los diez primeros minutos** | la escena inicial de El Pozo montada en el motor actual, una cámara, F8 apagado; el hermano de Cesar y una persona más; grabar | una sesión | descubren «tapar la brasa la ahoga» y «a la semilla le falta luz» sin ayuda → la escena vale; si no, se reescribe la escena, no el concepto |
| G8 | **prueba de tres** (opcional, en paralelo) | asíncrono por fichero: tres personas, un pozo mock, tres turnos cada una, notas en piedra | dos semanas | aparecen culpa y ayuda con nombre («ese barro es tuyo») → el multiplayer es el que promete; si solo aparece espera → revisar la dosis (más flujos por hora) |

Dos notas. G5 es física nueva y la física está congelada desde R141: pido a Cesar que **autorice
explícitamente esa única descongelación**, con benchmark de regresión sobre los siete hashes, y
ninguna otra. Y G6 es el experimento que ningún concepto puede saltarse: si la piel no se lee, ninguna
de las seis existe.

## 6. Ventas y nicho de la elección

Del `03 §3`: arquetipo «sandbox sistémico persistente co-op», 12 meses, Steam, 19,99 USD, si se
ejecuta bien: pesimista 15-40 k, **base 60-250 k** (la mitad inferior con 8-15 k wishlists; la
superior con ≥50 k), optimista 500 k-1 M copias. El factor que más mueve el resultado es el
multiplayer real y el clip de culpa co-op; después, que la materia se lea a escala de cámara. La demo
de Next Fest en formato P3 es la prueba de mercado más barata (una cámara, dos horas, un año) y es lo
que todos los equipos pequeños que llegaron a un millón hicieron antes de lanzar.

## 7. Lo que no hago todavía, y lo que necesito de Cesar

No convierto El Pozo en arquitectura. No escribo shaders, ni red, ni generador. No toco física salvo
que Cesar autorice G5.

De Cesar necesito tres cosas: (1) elegir (o corregir la elección; la condición de cambio a La Ladera
está escrita y es medible); (2) autorizar la descongelación única de G5; (3) decidir quién corre la
puerta: G1-G5 son de banco y las puede correr Opus con el protocolo del laboratorio; G6-G8 son con
personas y las tiene que organizar Cesar. Cuando la puerta esté cruzada, la fase siguiente es diseño
de nivel y de percepción sobre el motor actual, y solo después arquitectura.
