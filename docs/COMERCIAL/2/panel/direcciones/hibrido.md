# DIRECCIÓN · HÍBRIDO CON NÚCLEO CLARO · «SELLADO»

*(Panel de direcciones, segunda pasada, 2026-09-12. Ángulo: híbrido cuyo núcleo cabe en una frase y
cuyo core loop existe sin acumular features. Leído: `2/01_LEYES.md` entero, las seis familias de
`01_PROPUESTAS.md`, `02`, `03`, `04`, `fable_hipotesis.md`, las lentes de métricas, luz y organismos, y
las refutaciones de sello, balanza, cuna, testigo, vidrio, haz, huella, cuerpo, arrastre y planta con
órganos. Código: `SimStepper.Laboratorio.cs` (libro mayor :49-120; `LabTragar` :1026 solo traga
líquidos; la planta muerta ya deja fibra :811), `LabBench.cs` (nueve montajes, siete hashes). El crítico
de completitud no existe aún; escribo sin él.)*

## 1. Nombre y frase

**SELLADO.** *Da forma a una cámara que las leyes envejecieron, sella la puerta y deja que las leyes
cuenten los días y pesen lo que sale por el sumidero; si el recibo no te gusta, vuelve al toque que
quieras y cámbialo, o pásale el fichero a un amigo.*

El núcleo en una frase de diseño: **cada toque queda escrito; al soltar, las leyes corren, enseñan y
pesan; el registro se bifurca y se comparte.** Funde tres órganos que en la primera pasada vivían en
familias distintas y que el panel de leyes convirtió en objetos del motor: la cámara sellada con
veredicto diferido (Un Año Después), SOLTAR con «DÍA N SIN MANOS» (Sin Manos) y el asíncrono por fichero
(La Ladera). Lo que los une no es una capa: es el **sello** (registro tick-estampado + volcado +
`CorrerSello` + condición como dato) y la **balanza** (el sumidero pesa lo que traga). Sin esas dos
piezas los tres órganos eran promesas; con ellas son el mismo objeto visto desde tres lados.

## 2. La experiencia narrada

**Con las manos.** El aprendiz-muñeco heredado (2×4) con tres verbos: tallar (una celda de sólido,
deja grava), verter (el frasco, un búfer fuera de la grilla que el sello vuelca) y poner (roca suelta,
arcilla, núcleo frío, la brasa viva en el frasco). Cada verbo pasa por una puerta de `AlkahestSim` y
queda en el registro como `(tick, x, y, qué)`. Sin inventario, menú, receta ni termómetro. El cuerpo es
sensor: un halo de 12 celdas (la **Piel**) pinta las bandas de calor y humedad de lo que toca; el sprite
se tiñe al mojarse y se tizna al cruzar humo. El tacto es el rayos X que se lleva puesto.

**Cómo empieza una sesión.** En la última cámara con su recibo esperando, o con un fichero recibido:
«Ana bifurcó tu registro en el toque 7 y sacó DÍA 34». Seguir, bifurcar la de Ana, o la siguiente
situación de la campaña. Ninguna pantalla.

**Los diez primeros minutos («La que se ciega»).** Una cámara de 96×64 que el banco envejeció 30 000
ticks antes de que nadie entrara: manantial alto, canal, poza, sumidero abajo. La geología cuenta lo
que pasó: el agua corrió turbia, el sedimento se depositó sobre el sumidero y lo cegó a medias, la poza
rebosó, el lecho es barro. Sobre la puerta, la única frase del juego: «Todo lo que hagas aquí seguirá
pasando cuando te vayas.» Minuto 1: el muñeco entra; la Piel muestra el suelo empapado. Minuto 2: talla
la celda que ciega el sumidero (toque 1); el agua corre y se la traga, parda. Minutos 3-5: pone tres
celdas de roca suelta como labio aguas arriba (toques 2-4); la poza remansa; a ×1 se ve el lodo
asentarse. Minuto 6: pulsa SELLAR. La puerta se cierra; el mundo corre a ×10 (un «día» = 1 800 ticks =
6 s); en la franja inferior crece el recibo por día: barras de agua clara (azul) y turbia (parda) por
la salida. Minutos 7-8: día 3, clara; día 11, la grava del labio se colmata y la barra vuelve a parda;
día 14, la condición («≥ 120 u de agua clara por día») deja de cumplirse: **DÍA 14 SIN MANOS**. Minuto
9: arrastra la franja al día 11 (hay un volcado por día), ve la grava negra y toca: la puerta se
reabre, el registro se **bifurca** en ese tick, ensancha la poza (toque 5, día 11), sella: DÍA 20 y
sigue clara. Minuto 10: recibo final, la cifra que Ana verá. Sin una palabra ha aprendido: turbidez que
decanta en reposo, grava que se colmata, que el sumidero pesa, que un toque tardío cuesta días y que
volver atrás es gratis pero se nota en el recibo.

**Dónde aparece el momento interesante.** En el primer bifurcado: cuando entiende que la apuesta no es
«no puedo tocar» sino «cada toque mueve mi último toque». Tocar siempre está permitido; solo cuesta
días. El momento de máxima tensión ya no prohíbe jugar (la objeción a Sin Manos): tienta.

**Hora 1.** Cuatro situaciones más, una ley y un instrumento cada una: «La brasa tapada» (aire de
contacto: la pila envejecida ya es media carbonera; la salida bajo la pila tiene el bit entregable y el
carbón cae solo; el toque es el tamaño de la boca), «La piedra fría» (el núcleo frío llueve sobre el
hogar; la terracota del envejecimiento dice dónde estuvo el calor; moverlo es un toque), «El alambique
de r141» (la caldera como registro de 1 125 entradas que el banco ya tiene), «El horno sin boca» (el
recibo cuenta `LabVidrio`). Al final de la hora, la primera comparación: misma cámara, dos registros, y
la vista de **diferencia** (el `diff` de `mat[]` entre los dos estados finales): en celdas, por qué Ana
sacó más días.

**Hora 5.** Situaciones compuestas (alambique que riega un lecho; carbonera con salida y horno
encadenados; el hervidero) y la primera vez que toma el registro de otro y lo bifurca donde quiere:
«tu solución hasta el toque 6, luego la mía». Descubre que el recibo es un vector (clara, finos,
carbón, calor, goteos, plantas, toques, días) y que comparar es dominar en alguna columna, no sumar
puntos. Si la remedición de Q16 reabre el huerto, entran situaciones con planta viva como cláusula
(«planta viva en (118,250) el día 30»: el testigo que las leyes ya producen).

**Hora 20.** La **cámara libre**: el laboratorio entero (768×288) envejecido como una situación sin
horizonte ni condición; el recibo por día es la única cuenta y el registro es el diario. Cierra el
ciclo que el laboratorio nunca corrió y manda el fichero. Cada ley nueva (haz por vidrio, aire que se
gasta, hielo con reserva) entra como situaciones reordenadas por el banco sin tocar el juez: el juego
crece por leyes, no por cámaras.

**Mientras la simulación corre.** A ×1, mientras prepara, la simulación es el juguete: el agua fluye
ahora, el fuego prende ahora; los procesos de una cámara pequeña duran minutos. Tras sellar, mira el
time-lapse (3-10 min a ×10 para 30-100 días) con la franja creciendo, arrastra a cualquier día, y
decide si aguanta o toca. Mirar es la apuesta; tocar la cierra. Es la vigilia de Sordina con un precio
que cobran las leyes.

**Cómo lee el mundo.** Tres resoluciones. (1) La materia que las leyes ya escriben: terracota
(termómetro de máxima), carbón (ardió sin aire), grava negra (turbidez), vidrio (200 sostenidos),
planta viva (mojado y con luz, sostenido). (2) La Piel, siempre encendida. (3) Tres **visores** por
tecla, uno a la vez, con la imagen desaturada debajo: CALOR, AGUA, LUZ. Sus bandas son los umbrales de
las leyes (`LabBandas` como única fuente: 130 yesca, 170 hogar, 200 vidrio; 60 germina, 100 fibra
mojada, 16 agua clara; 40 luz mínima): la simulación resuelve a 8 bits y el ojo lee a 3, y aprender a
leer el visor es aprender la ley. Después de ocurrido: la franja del recibo y la vista de diferencia.

**Con 2-3 personas, sin roles.** **Relevo**: el fichero circula (el registro pesa KB); quien lo recibe
rejuega, bifurca donde quiere y sigue; información asimétrica temporal (ve qué hiciste, no por qué; la
respuesta es rejugar). **Liga**: misma situación, cada uno su registro, recibos comparados por
dominancia; el mismo `HashMat` final es la misma solución (copias detectadas gratis). **Mesa** (más
tarde, ruta A): dos o tres en la misma cámara escribiendo el mismo registro; el sello no sabe quién
tocó. El cuaderno falsable de La Ladera queda absorbido: toda afirmación es una repetición.

**Cómo aprende.** Campaña de situaciones pequeñas de autor **envejecidas y validadas sin personas**,
ordenadas por leyes implicadas, y después la cámara libre. El orden no lo decide un diseñador: el
banco corre el registro del autor con cada bandera de ley apagada (unas veinte) y anota cuáles cambian
el veredicto; las situaciones se ordenan por número de leyes implicadas y, a igualdad, por fragilidad
(fracción de cortes de una celda bajo los cuales el registro del autor sigue cumpliendo). Las primeras
implican una ley; las de la hora 5, tres o cuatro. Las ruinas amables emergen del envejecimiento (la
pila junto al hogar es una carbonera enterrada; el canal erosionado dejó lecho); la «rotura pequeña»
son dos líneas del montaje.

## 3. Core loop

**En una frase:** leer la cámara → tocar (cada toque queda escrito) → sellar → las leyes corren,
enseñan y pesan → bifurcar en un toque o pasar el fichero.

```
   ┌──────── LEER (materia · Piel · un visor por bandas) ◄──────────────────┐
   │                                                                        │
   ▼                                                                        │
 TOCAR ──► registro (tick,x,y,qué) ──► SELLAR ──► CORREN (×10, contador de días)
   ▲        cada toque mueve                              │
   │        «último toque»                                ▼
   │                                       ENSEÑAN (time-lapse por volcado diario ·
   │                                                terracota/carbón/grava · diff)
   │                                                      │
   │                                                      ▼
   │                                       PESAN (recibo por salida y por día ·
   │                                              DÍA N SIN MANOS · toques)
   │                                                      │
   └──── BIFURCAR en el toque k ◄──── o ──── PASAR el fichero ◄─┘
```

**Por qué cada órgano es núcleo y no capa.** El **sello** es la forma de una partida: sin él no hay
«soltar» como objeto, ni bifurcar, ni comparar, ni validar. La **balanza** es el único juez que no
necesita más números humanos que «clara» y «día»; sin ella «qué produje» exige recogida manual, que no
existe. **SOLTAR con contador** convierte el registro en apuesta y su cifra la producen las leyes. La
**cuna-lite** (envejecer + validar) no es contenido: es cómo las leyes fabrican el tablero y deciden si
vale. El **cuerpo-sensor** es el primer instrumento; el precio real del toque no es un número de
economía: es que cuenta y mueve el último toque. El **bifurcado** es la respuesta a «examen o juego»:
el registro editable es la diferencia entre rendir cuentas y jugar. Todo lo demás es capa y va en §10.

## 4. Por qué explota mejor la simulación

**Ejecutan.** Todo lo físico ya está: agua 5/5, fuego 4/5, condensación, colmatación, cocción,
carbonización, presión. La dirección no pide ninguna ley nueva para existir; el núcleo vive con agua y
fuego, y si el huerto nunca vive, el juego sigue entero: la salida que se ciega y la tolva que se acaba
son los procesos mortales que «días sin manos» necesita (Sin Manos solo tenía la planta).

**Revelan.** La materia como registro; el time-lapse como volcado diario del sello; la vista de
diferencia como el hash hecho visible; la Piel como el campo leído por el cuerpo; el envejecimiento
como intro sin texto (la geología es historia).

**Juzgan.** La balanza convierte cada contador del libro mayor en una columna del recibo sin un número
de balance; la condición como dato hace que cualquier ley nueva entre en el recibo sin tocar nada; «DÍA
N» sale de registro + condición; la comparación entre personas es por dominancia, no por fórmula; el
validador decide si una situación vale sin que nadie la juegue (el registro vacío falla, el del autor
cumple, fragilidad por cortes); el orden de la campaña sale de qué leyes cambian el veredicto.

**Contenido sin autor.** Situaciones envejecidas (10-15 líneas de montaje + N ticks); mutaciones
validadas de cada montaje (boca ±k, veta más gruesa, hogar movido) corridas con el registro del autor:
familias verificadas, no cuevas libres; la etiqueta de dificultad (leyes implicadas × fragilidad); la
cámara libre; y cada registro compartido, que es una situación nueva desde el toque donde se bifurca.

**Validado en banco sin personas.** Discriminación del recibo, no trivialidad, cumplimiento del autor,
fragilidad, orden de la campaña, determinismo (round-trip del volcado a mitad del alambique; el anillo
de hashes dice cuándo divergieron dos corridas), hash de versión de física en el fichero (un registro
viejo se rechaza o se rejuega con aviso, nunca diverge en silencio).

## 5. Qué añade al sustrato

| pieza | semanas Opus | cruza con | por qué es núcleo |
|---|---|---|---|
| Sello: cola bajo las seis puertas de `AlkahestSim`, registro, volcado/carga con la lista completa de estado (`_tick`, `_labPase`, libro, preset congelado, `LuzCieloX0/X1`, `_zonaInteresChunk`, búfer del frasco), `CorrerSello`, condición como dato, hash de versión | 3-4 | nada físico; cambia cuándo entra la mano | la forma de la partida |
| Balanza mínima: bit `entregable` (carbón, ceniza, semilla), tragar sólido solo desde arriba, libro por salida (`aux`) y calidad | 1 | turbidez × decantación (la salida por rebose recibe clara, la del fondo turbia); el sedimento sigue cegando la salida | el juez sin números |
| `LabBandas` única fuente de umbrales + tres visores por bandas + Piel (CuerpoSim sensor, tinte, brasa viva) | 0,5 + 0,5 + 1 | lee `temp`, `humedad`, `luz` | aprender a ver |
| Cuna-lite: envejecer montajes, validador por veredicto, fragilidad, mutaciones, orden por leyes implicadas | 1,5 + 0,5 | todas, corriéndolas | el tablero lo fabrican las leyes |
| Anillo de hashes del banco | 0,4 | — | acorta la iteración |
| Gesto SELLAR, contador, franja de recibo, time-lapse por volcados diarios, vista de diferencia | 1,5 | consume el volcado | la única parte que no es banco |
| Remedir Q16 con `luz[i+W]` y quinta pasada descendente | 0,2 | luz × planta | decide si el huerto entra en la campaña 1 |

Total **10,5-12 semanas de Opus**; camino crítico sello (4) → cuna-lite (2) → campaña validada (0,5)
≈ 6,5. En paralelo con el sello: balanza, bandas, cuerpo, anillo. Nada de L, A, F, V, M salvo la
remedición de Q16: son las expansiones que prueban la tesis («una ley nueva entra en el recibo sin
tocar el juez»), en el orden de `01_LEYES.md` §4: L, A, luego V o F.

## 6. Principal riesgo de diseño

**Que sea un examen.** Que quien prepara, sella y lee un recibo sienta que rinde cuentas. Es la
pregunta que el panel de leyes dejó sin resolver y ninguna ley la resuelve. Las tres defensas están en
el núcleo: el registro es editable (el jugador elige cuánto apostar eligiendo cuándo deja de tocar); el
recibo es un vector comparado por dominancia (no hay una nota, hay columnas para elegir); la cámara es
pequeña y el horizonte corto (3-10 min de time-lapse). Riesgo segundo: la condición de la campaña es
autoría por situación (el refutador del sello tiene razón); el validador la hace verificable, no
gratuita, y por eso la campaña tiene 15-20 situaciones y no 40. Riesgo tercero: determinismo entre
máquinas (IL2CPP contra los 63 hashes del editor, un día); el fichero lleva hashes, la divergencia se
detecta y el recibo de cada uno se calcula en su máquina.

## 7. Cuánto depende de iteración humana

**Exige playtest.** (1) Examen o juego: tres sesiones de 40 min con tres personas cada una (hermano de
Cesar y desconocidos), F8 apagado; se cuentan bifurcados voluntarios por persona y si alguien pregunta
«¿cuántos días te dio?». Pregunta binaria, no balance. (2) El largo del horizonte y `DiaTicks`: un
número, una o dos sesiones. (3) Legibilidad de bandas y Piel: láminas y tres GIF con cinco desconocidos,
dos días (la G6 de la primera pasada, con bandas de ley en vez de rampas). (4) `AguaClaraCargaMax` (16):
sale del propio motor (emite a 40, decanta a 6 por visita), no del gusto.

**Lo que el banco sustituye.** Toda la validación de contenido (no trivial, cumple el autor,
fragilidad, discriminación); el orden y la dificultad de la campaña; el determinismo; la regresión de
cada ley nueva sobre los recibos de todas las situaciones (una noche); la detección de copias; la
caducidad de registros por versión. No hay balance de economía (hay materia y geometría), ni de tiempo
(las constantes son las del laboratorio y el contador las vuelve cifra sin afinarlas), ni de estaciones,
ni de roles. Tuning humano estimado: **8**.

## 8. La prueba más barata capaz de matarla

**¿El recibo discrimina?** Con la balanza mínima (1 semana) y sin sello: cuatro montajes del banco
(alambique, carbonera con salida, diluvio, arco largo), cada uno con tres registros: vacío, «autor» (la
caldera; el serpentín fuera de la boca; el labio de la poza) y doce cortes de una celda, corridos 30
días (54 000 ticks); 4 × 14 × 54 000 ≈ 3 M ticks, dos horas de banco. **La mata**: si en tres de los
cuatro montajes el recibo del día 30 del registro del autor no domina al del registro vacío en ninguna
columna por un factor ≥ 2, o el vacío ya cumple la condición, las leyes no juzgan y con eso muere esta
dirección y cualquier otra construida sobre J. Dos semanas hasta ese dato. La segunda prueba, con
personas y tras el prototipo feo: si en tres sesiones la mediana de bifurcados voluntarios es cero, es
un examen.

## 9. Tiempos

- **Hasta evidencia para matarla: 2 semanas** (balanza mínima + variantes de montaje + una noche de
  banco). Técnico, acotado, sin dependencia secuencial.
- **Hasta prototipo feo que permita juzgar el core: 7 semanas.** Secuencial: sello (3-4) → cuna-lite
  (1,5-2) → tres situaciones envejecidas y validadas (0,5). Paralelo con el sello: balanza, `LabBandas`,
  visores, Piel, anillo; paralelo con la cuna: gesto SELLAR, franja, time-lapse, diff. El prototipo feo
  es F8 como visores, recibo como texto, tres situaciones, el muñeco heredado, sin arte.
- **Iteración humana: 3-4 sesiones en las semanas 8-10** (examen o juego; horizonte; bandas) más el
  día de IL2CPP. Incomprimible pero corta: decide sí o no, no afina.
- **Después**, si vive: L (3-3,5 semanas, empezando por el día de Q16) como primera expansión y prueba
  de la tesis; A; V o F. Cada una entra como situaciones nuevas reordenadas por el banco y ninguna toca
  el juez. Ruta A (mesa) solo cuando el relevo por fichero haya demostrado que la gente compara.

## 10. Lo que deja fuera y por qué

De **El Pozo**: la vertical como columna vertebral (la geometría es de la situación, no del producto),
las estaciones (evento), los roles por estrato, el avatar con cuerda y trepa, las 40-60 ruinas de autor
(las fabrica el envejecimiento), el cuaderno que dibuja (el registro es el cuaderno). De **La Ladera**:
temporadas, viajero y encargos (el recibo es el deseo), objetos multicelda (6-8 semanas por una cesta
que la salida hace innecesaria). De **Un Año Después**: las 12-15 cámaras de autor a 2-4 semanas (aquí
son montajes de 10-15 líneas validados), el año de 360 000 ticks (30-100 días bastan si §8 pasa) y **el
Patio**: la cámara libre hace su trabajo sin hub. De **Sin Manos**: la mano sin cuerpo y la prohibición
de tocar (aquí tocar bifurca). De **Sordina**: los seres lectores, la gramática de vibración y el audio
como mitad de la percepción (tuning humano alto, sin diseñador de sonido; las bandas primero). De
**Claraboya**: bichos-sensor, semillas raras, día y noche. Del catálogo: la lámpara, la polilla, los
polvos al viento, el testigo como material (la cláusula del sello lo sustituye), la huella como campo
(si vuelve, es `patina` bajo `LabActivo`, después del prototipo), el cuerpo como conjunto de leyes
(solo sensor), lockstep y ruta A ahora.

**Lo que pierde a cambio.** El techo del sandbox co-op (500 k-1 M) por el arquetipo de expediciones
con condición fuerte (base 60-150 k, el más barato de alcanzar y el más apto para demo y stream, `03
§3`) con la estructura de comparación de Opus Magnum (200-500 k). La frase es la de Un Año Después y el
cartel el de Sin Manos («DÍA N»). La cápsula no tiene el clip de culpa co-op; tiene el time-lapse con
la franja del recibo y dos cifras una debajo de otra; si el relevo demuestra que la gente compara, la
mesa recupera el clip co-op sobre el mismo núcleo. Y es honesta con la incógnita mayor de la primera
pasada: **no depende del huerto**; si Q16 reabre el eje verde, la campaña gana situaciones; si no, el
producto es «cámaras de agua y fuego», lo que `04 §5 G4` prescribía.

## 11. Autoevaluación (rúbrica v2, 1-10)

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 7 | J tiene cero cruces físicos (el refutador tiene razón); su apalancamiento es que toda ley entra en el recibo sin tocar nada |
| las leyes ejecutan, revelan y juzgan | 9 | recibo, días, validador, orden de campaña |
| iteración humana (10 = poca) | 8 | tres sesiones binarias y dos números; el contenido se valida solo |
| verificabilidad automatizable | 9 | todo menos «examen o juego» y la legibilidad de las bandas |
| la simulación es el juego | 8 | el único añadido no físico es la condición de la campaña, validada; sin economía, árbol ni hub |
| onboarding garantizable | 8 | situaciones envejecidas, validadas y ordenadas por leyes implicadas |
| observabilidad | 7 | bandas de ley, Piel, recibo, diff; sin piel de shader todavía |
| tiempo como apuesta | 9 | SELLAR con contador y bifurcado con precio |
| multiplayer emergente | 6 | relevo y liga por fichero, sin roles; mesa después |
| profundidad por leyes estables | 7 | crece por leyes que entran sin tocar el juez; depende de que L/A/V/F lleguen |
| cuerpo del jugador | 5 | sensor y portador; consecuencias pospuestas |
| identidad comercial | 7 | la mejor frase y el mejor cartel de la primera pasada; sin clip co-op ni cápsula cozy |
| dificultad técnica (10 = fácil) | 8 | acotada; el riesgo es la lista de estado del volcado e IL2CPP |

**Posición.** Transformar el veredicto anterior: El Pozo deja de ser columna vertebral y sobrevive
como la cámara libre y, si la mesa llega, como el modo co-op; la columna vertebral es la cámara sellada
con registro, recibo y bifurcado. Respuesta a la pregunta final: sí, las leyes pueden ejecutar, revelar
y juzgar, y la profundidad crece más rápido que el coste de diseñarla **si y solo si** el recibo
discrimina en un horizonte corto (§8, dos semanas) y bifurcar convierte el examen en juego (§7, tres
sesiones). Las dos pruebas son las más baratas del panel.
