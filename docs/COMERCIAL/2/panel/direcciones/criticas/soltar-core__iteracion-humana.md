# CRÍTICA · «Sin Manos» (soltar-core) · lente: ITERACIÓN HUMANA OCULTA

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: productor; único tema: cuánta iteración
humana esconde la dirección (playtest, tuning, balance, contenido de autor, contingencias ante
construcciones arbitrarias) y qué parte se sustituye por banco headless, hashes, generación y
validación automática. Leído: `soltar-core.md` entero, `01_LEYES.md` entero (con §6), las dos
refutaciones del sello, `00_ENCARGO_Y_CRITERIO.md`, `03_MERCADO.md`, las críticas hermanas de esta
dirección (`ingenieria-evidencia`, `simulacion-es-el-juego`) y la de `leyes-juzgan` desde esta misma
lente; código: `LabBench.cs` (`Escenarios`, `Correr` y su única intervención, `ArcoMuestra`),
`SimStepper.Laboratorio.cs` (`LabAgua` :406-535 con decantación, depósito y colmatación; `LabPlanta`
:801-916; `LabSumidero`/`LabTragar` :1021-1033; `LabPoroso` arcilla/terracota), `LabParams.cs`
(`Decantacion` 6, `DepositoUmbral` 200, `DepositoReposo` 24, `PlantaHumedadMin` 60, `PlantaLuzMin` 40,
`GerminaPorMil` 2, `Caudal` 24, `HogarRaw` 170); bancos R148 (arco largo, 30 días) y R150 (Q16).)*

## 0. Veredicto: SEGUNDA RONDA

La dirección declara 8/10 de iteración humana y cuatro o cinco días de personas; esconde entre 20 y 30
días-persona en los tres primeros meses y una capa de control (la mano) que no está costeada ni se
valida en banco. No es el playtest infinito: no hay economía que balancear, ni temporadas, ni
biblioteca de cuencas, ni contingencias ante lo que el jugador construya, porque las leyes lo ejecutan
y el sello lo mide. Ese mérito viene del paquete J y es común a «El Recibo». Lo que Sin Manos añade
sobre J (SOLTAR como medida, racha, apuesta, byte de autor, halo, sellar a ciegas) es barato y casi
no esconde tuning. Lo que esconde está en tres sitios: **los umbrales de las cláusulas** (la propia
dirección, con todos los bancos delante, puso tres de tres fuera del régimen medido), **la mano y la
dote** (diseño de control y economía en miniatura, doce números sin validador) y **la escalera como
espinazo** (doce situaciones resueltas por una persona que caducan con F y V, que la dirección
promete para la segunda entrega). Nota real de iteración: **6, no 8**. Pasa la puerta sin margen.

## 1. Riesgo mayor desde esta lente

**Que «DÍA N SIN MANOS» sea un número afinado a mano por situación y no una medida.** Tres hechos:

- **Los umbrales de ejemplo de §8 están mal los tres.** «Goteos ≥ 500/día»: R148 da 9 588 en 30 días
  = 320/día; R150, 217/día; R141, 180/día. Falla en las seis variantes. «Planta viva el día 30»: cero
  vivas en R148 (2 nacidas / 2 muertas) y en R150 (9 / 9); veinte rondas sin un huerto vivo. Falla en
  las seis. «Carbón ≥ 100»: 122 en R148, 325-400 en R135 con boca 1, 4 y 8. Pasa en las seis. La
  prueba que la dirección propone para matarse se dispararía por calibración antes de que la
  simulación discrimine nada. Si el autor del documento, con diez bancos delante, no acierta un
  umbral, «un número por situación confirmado en minutos» no es una descripción honesta del trabajo.
- **Las cláusulas instantáneas parpadean o se trivializan.** La humedad del lecho oscila 43-125
  (R148) y 50-99 (R150) alrededor del mínimo 60: «humedad ≥ 60» cambia de valor varias veces por día
  y «el último día en que todas se cumplieron» depende de si se evalúa al cierre, por mínimo, media o
  mayoría y con qué histéresis. Y una semilla bajo una celda de agua germina (`LabPoroso` no exige
  aire encima) y no transpira (sin `Empty` vecino): inundar el lecho cumple «planta viva» para
  siempre. Cada decisión de semántica es por tipo de cláusula, cruza todas las situaciones y hoy no
  está escrita.
- **Tres de los seis relojes no son relojes.** El sumidero no se ciega: el depósito exige `reposo ≥ 24`
  visitas con carga ≥ 200 quieta, el sumidero traga cada visita y moverse pone `reposo` a 0. La presa
  de arcilla es bimodal (permeabilidad 2 → 0 u/visita desde agua abierta; 0,4 días por capilaridad):
  interruptor, no horizonte. El fusible de hielo dura ~0,5 días con la térmica de hoy; un fusible «para
  el día 20» exige una reserva 40× el natural, que es un número de balance con nombre de ley. Con los
  tres dones como pins infinitos (hogar 170 raw, manantial 24 celdas/s, núcleo frío), «Llueve donde
  hace frío» da DÍA ∞ y la dirección omite D («el hogar come»), el candidato con más apalancamiento
  por línea del panel. Sin relojes que las leyes produzcan, la mortalidad la pone el autor situación a
  situación, y eso es exactamente el trabajo que esta función objetivo penaliza.

## 2. La iteración humana que esconde, partida a partida

| partida | declarado | corregido | por qué |
|---|---|---|---|
| Semántica de las cláusulas (agregación, ventana, histéresis, existencia frente a crecimiento) | no aparece | 2 días + una vuelta por ley nueva | decisión por tipo de cláusula; depende de la escala de oscilación de cada campo (humedad en minutos, carbón en días) |
| Umbral, horizonte y dote por situación | «un número, en minutos»; «dos tardes» | 6-8 días (×12) | tres de tres ejemplos fuera del régimen medido; el validador acota lo trivial y lo imposible, no lo aburrido; la dote (31 de frío, 200 de arcilla) es economía en miniatura sin validador |
| Registro del autor por situación | implícito en «el del autor debe cumplir» | 6-10 días | alguien resuelve doce situaciones en el editor; es la única prueba de existencia de solución |
| Caducidad con F y V | «hash de versión» | 1-2 días por paquete | el hash detecta, no repara; 21 rondas de física en 3 días en el laboratorio; la escalera validada en la primera entrega se re-resuelve en la segunda |
| La mano (azada de 5, cubo con `counts[]`, PONER con temperatura, PRENDER, SONDAR) | dentro de «gesto visible: una semana» | 2-4 sem de Opus no costeadas + 2-4 días de sensación no validable | no está en el catálogo; el control corporal costó nueve rondas (R110-R121); qué cuenta como un toque fija la escala del presupuesto y de toda comparación entre sellos |
| Render mínimo antes del primer playtest | no aparece | 1-2 sem de Opus + 2 días de láminas | hoy campos en F8 y plantas de un píxel; sin esto las tres sesiones miden F8 |
| «Mirar a ×10 es placer» | tres sesiones | 3 días; bastan para matar, no para arreglar | ×10 no se sostiene en el drama (`LabPresupuestoMs` 20 ms → ×2-3 en incendios); el plan B, sellar a ciegas, convierte el juego en mandar ficheros al banco |
| Longitud del día | «un número» | 1 día, acoplado a todo | a 1 800 ticks la planta muere en un sexto de día, la tolva en 7,8 días y la oscilación de humedad cabe varias veces dentro |
| Gramática de la apuesta y scrub de la repetición | metadatos | 1-2 días de interfaz | qué se puede apostar y cómo se escribe «nada» o «aguanta» es sensación, no dato |
| Comparación por dominancia | metadato | anotado, no penalizado | la dominancia rara vez decide entre dos vectores; la gente pedirá un escalar y ahí vuelve el balance |

**Suma: 20-30 días-persona en los tres primeros meses** (declarados 4-5), más 3-6 semanas de Opus no
costeadas (mano y render). Con los sustitutos de §3: **10-15 días**. Lejos del playtest infinito;
lejos también del 8 declarado.

## 3. Qué se automatiza en su lugar (y la dirección no propone)

1. **Cláusulas relativas e integrales.** «≥ 1,5× lo que da el registro vacío» y «celdas de planta ≥ 3
   durante 3 días» en vez de «≥ 500» y «viva el día 30». Jubila el umbral humano; deja al humano solo
   el horizonte. Una tarde de Opus.
2. **Semántica por estabilidad.** Evaluar cada cláusula con las cuatro agregaciones y jitter de ±100
   ticks en el tick de sello; quedarse con la que no mueve «DÍA N». Un día; se reusa con cada ley nueva.
3. **Umbral por barrido.** Para cada situación, la familia de variantes (boca ±k, hogar movido) y el
   umbral que maximiza la varianza de veredictos y retrasa el día de decisión. El humano elige la
   familia; el banco, el número.
4. **Día de decisión y parpadeo.** Desde el día d, K perturbaciones de una celda: el primer d en que
   todos los veredictos coinciden es el día en que la situación «ya está decidida». Si d ≤ 3 en todas,
   se sabe sin personas. Parpadeo por día gratis con `ArcoMuestra`.
5. **Relojes como tabla del banco con suelo de ruido** (equivalentes ±1, ±2 celdas): el día de fallo de
   cada aparato como función de su única palanca. Es la prueba A de la crítica de ingeniería y vale
   sea cual sea la dirección.
6. **Registro del autor en código + búsqueda en banco.** La caldera de `Correr` ya es un registro de
   1 125 entradas escrito en código. K registros sorteados de 1-5 toques corridos headless (~10 min por
   situación) demuestran que existe solución, miden fragilidad y dan un histograma sintético contra el
   que comparar sin amigos. Cesar cura; no resuelve doce veces.
7. **Regresión nocturna** de los registros contra la física actual tras cada paquete: la lista de lo
   roto es automática; el arreglo, humano.
8. **Kill test de pins.** Toda situación construida sobre hogar + manantial + dote: si «DÍA N» no acota
   en 300 días (540 000 ticks, 15 minutos de banco), la situación no existe y D es obligatorio.
9. **La mano del prototipo = cincel, frasco y pincel del laboratorio bajo las puertas del diario**; un
   toque = una llamada a una puerta. Azada y cubo esperan al playtest, fuera del camino crítico.
10. **Tabla distancia → ticks de fusión del hielo** (una tarde) antes de prometer ningún «día 20».
11. **Determinismo entre máquinas**: `LabBench` en IL2CPP contra los 63 hashes del editor, un día,
    antes de prometer el fichero que circula.

## 4. La prueba más barata que la mata

**Una semana de banco, sin sello, y media jornada de personas en la misma semana.**

- **A · Relojes con suelo de ruido.** Seis aparatos (filtro de grava bajo manantial a caudal 24; tolva;
  carbonera; lecho bajo goteo; presa de arcilla; salida bajo poza turbia) × 3 palancas × 3 equivalentes
  ±k = 54 corridas de 36 000 ticks ≈ 1 h. Se mide el día de primer fallo y la dispersión intra e inter.
- **B · Discriminación con cláusulas relativas.** Las seis variantes del arco y del alambique de §8,
  con «goteos/día ≥ 1,5× el registro vacío», «carbón ≥ 0,5× el máximo aislado de R135» y «celdas de
  planta ≥ 3 en ventana de 3 días»; día de primer fallo como dato, parpadeo por día y día de decisión
  con K = 8 cortes.
- **C · Una tarde.** Semilla sumergida; tabla distancia → fusión del hielo; pins a 300 días
  (goteos/día en el día 1 y en el 300).
- **D · Media jornada humana, sin código.** Cesar y dos personas miran correr la carbonera del banco a
  ×10 en el editor con `LabCarbonizado` subiendo en F8, que existe hoy. Si nadie quiere verla dos
  veces, «mirar es el juego» muere antes de escribir el sello.

**La mata** si menos de cuatro de seis variantes dan día de fallo distinto fuera del suelo ±k, **o**
todos los fallos caen antes del día 3, **o** inter/intra < 1,5 en todos los aparatos (la palanca no
mueve el día más que el dado), **o** todo reloj queda bajo un día o sobre 500, **o** alguna cláusula
parpadea más de una vez por día sin histéresis, **o** los pins no cambian ±10 % en 300 días y D no
entra en la primera entrega. Lo que el banco no puede matar (si tocar con precio es tensión) espera al
prototipo feo. Coste: 4 días de Opus, 1 de Fable, 0,5 de Cesar.

## 5. Tiempos corregidos

- **Evidencia para matarla (banco): 1 semana.** Automatizable, sin dependencias: evaluador desechable
  de cláusulas relativas sobre `ArcoMuestra`, intervención genérica del banco, montajes variantes y
  equivalentes. Evidencia sobre la tesis emocional (juego o examen, mirar como placer): **semana 9**.
- **Prototipo feo: 9 semanas de calendario, no 6-7.** J entero (5-6 sem de Opus en dos hilos, con
  verificación de Fable ~1:1 como en el laboratorio), metadatos y halo (1), el día de L, evaluador con
  agregaciones y jitter (0,5), D (0,5-1, en la primera entrega), render mínimo (1), 6-8 situaciones de
  agua y fuego validadas con registro en código (1 de Opus + 4-6 días de personas), mano = herramientas
  del laboratorio. Con azada y cubo nuevos: 11-12. F y V no antes del playtest de la semana 9.
- **Secuencial:** diario → volcado → `CorrerSello` → gesto → situaciones validadas → escalera (5-6
  semanas en serie); envejecer necesita el volcado; playtest necesita render. **Paralelizable:** balanza,
  `LabBandas`, anillo, D, evaluador, relojes, IL2CPP. **Incomprimible:** lo humano de §2.
- **Iteración humana probable: 20-30 días-persona** hasta juzgar el core; **10-15 con §3**; después,
  1-2 días por paquete de física y una sesión por bloque de situaciones.

## 6. Órganos a conservar si se descarta

- **SOLTAR como medida y no como modo**: «último día en que todas las cláusulas se cumplieron tras el
  último toque»; nada bloquea las herramientas. Vale para cualquier dirección con J.
- **La apuesta de una frase juzgada por la simulación**, degradada a nota de cuaderno hasta que un
  playtest diga que calificarla divierte.
- **Byte de autor en `Intervencion`** y la atribución contrafáctica (rejugar sin las entradas de B).
- **`tickSellado` y «días vistos»**: sellar a ciegas, la racha valiente, el banco como verbo de jugador.
- **El halo de la mano** (vista Piel de C sobre el cursor): «el tacto es gratis, tocar cuesta».
- **Los primeros diez minutos («El hilo»)** como guion de onboarding, reordenados agua → fuego → planta.
- **El fusible de hielo como temporizador tabulado**, no prometido.
- **Cláusulas relativas e integrales, umbral por barrido, día de decisión, parpadeo, equivalentes ±k,
  registro en código y búsqueda en banco, kill test de pins**: la acotación real del validador.
- **La tabla de relojes** como producto del banco, sea cual sea la dirección.

## 7. Puntuaciones (rúbrica v2, 1-10)

| eje | nota | por qué (desde esta lente) |
|---|---|---|
| apalancamiento sistémico | 6 | J no añade física; el fusible es un número de balance con nombre de ley; D, la única palanca barata, queda fuera |
| las leyes ejecutan, revelan y juzgan | 7 | juzgan con umbrales humanos hasta que las cláusulas sean relativas; ejecutan ~11 días y luego pins; revelan en F8 |
| iteración humana (10 = poca) | 6 | 20-30 días frente a 4-5 declarados; mano, dote, registro del autor y re-resolución; sin balance de espacio infinito ni temporadas |
| verificabilidad automatizable | 8 | casi todo en banco; no se verifica si mirar es placer ni la legibilidad |
| la simulación es el juego | 7 | condición, apuesta y racha son capas delgadas; la mano con cubo y la escalera como espinazo son una capa tradicional pequeña |
| onboarding garantizable | 5 | la primera situación narrada es H4 sin resolver; tres de tres umbrales de ejemplo fuera de régimen; el orden y «si enseña» son humanos |
| observabilidad | 5 | halo, bandas y tira por construir; render mínimo no presupuestado; ×10 no sostenido en el drama |
| tiempo como apuesta | 9 | es el verbo entero; −1 porque la racha a cero sin clones puede leerse como castigo |
| multiplayer emergente | 5 | diario multiautor sin roles es real y barato; cross-machine sin medir; nada «divertido juntos» demostrado |
| profundidad por leyes estables | 5 | después del día 11 el mundo es estático; pins; los horizontes largos piden F y V, que re-resuelven la escalera |
| cuerpo del jugador | 2 | solo el halo, por decisión |
| identidad comercial | 7 | frase y contador fuertes; arquetipo con el mejor suelo de `03`; la cápsula hoy es un lecho gris con un píxel verde |
| dificultad técnica (10 = fácil) | 8 | acotado y con prueba; lo abierto son la mano, el hilo del banco y la legibilidad |

Puertas: iteración humana 6 ≥ 5 y apalancamiento 6 ≥ 5: pasa, sin margen.

## 8. Crítica razonada

He visto morir sistémicos indie en el playtest infinito, y esta dirección no es uno de ellos. No tiene
economía que balancear, ni temporadas, ni una biblioteca de cuencas, ni contingencias ante lo que el
jugador construya: las leyes ejecutan lo que construya y el sello lo mide. Ese mérito es real y viene
del paquete J, no de la dirección; es el mismo mérito de «El Recibo». Lo que Sin Manos añade sobre J
(SOLTAR como medida, la racha, la apuesta, el byte de autor, sellar a ciegas) cuesta días y casi no
esconde tuning. El problema está en lo que declara barato y no lo es, y en que la única corrida larga
del laboratorio contradice su premisa.

Primero, los umbrales. La dirección dice que el único número humano por situación se «confirma en
minutos», y su propia prueba de §8 lo desmiente: «goteos ≥ 500/día» está 1,5-3× por encima de todo lo
medido (320/día en R148, 217 en R150), «planta viva el día 30» no se ha cumplido en veinte rondas y
además se cumple trivialmente con una semilla sumergida, y «carbón ≥ 100» pasa en todas las variantes
medidas. Tres de tres, escritos por alguien con todos los bancos delante. La prueba que la dirección
propone para matarse se dispararía por calibración antes de que la simulación tuviera ocasión de
discriminar. Eso no es un descuido: es la demostración de que el umbral por cláusula es tuning, de que
la semántica de la cláusula (instantánea o integral, con o sin histéresis, existencia o crecimiento) es
una decisión de diseño que cruza todas las situaciones, y de que ninguna de las dos está escrita.

Segundo, los relojes. «Varios relojes ya matan algo que no es la planta.» Leídos contra el código, el
sumidero no se ciega (el depósito exige veinticuatro visitas de agua quieta y el sumidero traga cada
visita), la presa de arcilla es un interruptor de geometría (o nunca se moja, o en medio día), y el
fusible de hielo dura medio día con la térmica de hoy, así que «programar un evento para el día 20» es
una reserva cuarenta veces mayor que el natural: un número de balance con nombre de ley. Quedan la
tolva, la carbonera, el filtro y la planta, todos monótonos y todos agotados antes del día 11 en la
única corrida de 30 días que existe. Y los tres dones son pins infinitos: hogar eterno, manantial
perpetuo, frío racionado «como dato». La dirección cuyo verbo entero es SOLTAR omite D, «el hogar
come», medio semana y dos números, y deja F para la segunda entrega. Sin relojes que las leyes
produzcan, la mortalidad la pone el autor situación a situación, y eso es exactamente el trabajo que
la función objetivo quería que las leyes hicieran solas.

Tercero, la capa de control. La mano (azada de cinco celdas, cubo con cuenta por material, PONER con
la temperatura que traía, PRENDER, SONDAR) no está en el catálogo ni en el laboratorio. Es diseño de
control con tuning de sensación, dos a cuatro semanas de Opus que el documento absorbe en «gesto
visible: una semana», y qué cuenta como un toque fija la escala de todo el presupuesto y de cada
comparación entre sellos. Con ella viene la dote por situación, economía en miniatura de doce números
sin validador, y el render mínimo sin el cual las tres sesiones de playtest miden F8 y plantas de un
píxel.

Cuarto, la escalera. Es el espinazo declarado, y cada peldaño es montaje (barato), condición, dote,
umbral y registro del autor resuelto por una persona; con F y V en la segunda entrega, la escalera
validada en la primera se re-resuelve en la segunda. Veinte a treinta días-persona en tres meses
contra los cuatro o cinco declarados. Sigue siendo contenido pequeño y acotado, no balance de un
espacio infinito, y por eso no es «descartar».

Lo que sí sustituye al humano, y la dirección no propone: cláusulas relativas al registro vacío e
integrales por ventana (jubilan el umbral), la semántica elegida por estabilidad bajo jitter, el umbral
por barrido, el día de decisión con K perturbaciones como proxy de aburrimiento, los relojes tabulados
con suelo de ruido, el registro del autor en código y la búsqueda en banco de soluciones (Cesar cura,
no resuelve doce veces), la regresión nocturna, el kill test de pins a 300 días y la mano hecha con
cincel, frasco y pincel bajo las puertas del diario. Con eso la iteración baja a diez o quince días y
la mano sale del camino crítico.

Veredicto: segunda ronda. No descartar, porque sus órganos son el paquete J y sus mejores piezas
(SOLTAR como medida, la apuesta, la atribución por rejugado, los primeros diez minutos) valen para
cualquier dirección, y su prueba corregida cuesta una semana. No finalista, porque la nota real de
iteración es 6 y no 8, porque el prototipo feo son nueve semanas y no seis, porque omite D y promete
relojes que el código no da, y porque comparte con «El Recibo» sello, balanza, validador y fichero y
se diferencia solo en el encuadre. Como estrategia de producción, no hay que elegir entre las dos:
se construye J con las cláusulas relativas y D, y el encuadre (racha y apuesta contra histograma y
clones) lo decide el playtest de la semana nueve, no un juez.
