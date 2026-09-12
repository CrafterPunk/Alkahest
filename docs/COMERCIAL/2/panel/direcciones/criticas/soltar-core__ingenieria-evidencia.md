# CRÍTICA · SIN MANOS (soltar-core) · lente INGENIERÍA Y EVIDENCIA

*(Director técnico, segunda pasada, 2026-09-12, versión final. Leído entero: `soltar-core.md`,
`01_LEYES.md`, las refutaciones del sello (×2), `LabBench.cs` (montajes, `Correr`, `MedirLecho`,
`ArcoIniciar/Avanzar/Muestra`), `SimStepper.Laboratorio.cs` (`LabCampos`, `LabErosion`, `LabAgua`,
`LabInfiltrarHacia`, `LabPoroso` con los casos `Arcilla` y `Semilla`, `LabPlanta`, `LabSumidero`,
`LabTragar`, `LabSecarHacia`), `SimStepper.cs` (conteo de `float`), `LabMateriales.cs`,
`LabParams.cs`, `Universe.Laboratorio.cs`, `AlkahestSim.cs:358-400` y `:560-575`, `SimSync.cs:150-165`,
benchmarks R133, R135, R139, R148, R150 y el banco del 2026-09-06, `02_COMPARATIVA.md`,
`04_VEREDICTO.md`, la memoria `runcommand-headless` y la crítica hermana
`leyes-juzgan__ingenieria-evidencia.md`.)*

**Veredicto: FINALISTA, con puerta de una semana y tres correcciones obligatorias.** Desde el
sustrato, Sin Manos y El Recibo (leyes-juzgan) son **la misma construcción** durante las primeras
cinco semanas: el paquete J entero. Lo que Sin Manos añade encima (SOLTAR como medida, apuesta,
racha, byte de autor, halo de la mano, fusible de hielo) cuesta 3-5 días y no toca la física. No
son dos finalistas compitiendo por un hueco de construcción: es un build con dos encuadres, y el
encuadre lo decide el playtest de la semana 8, no el banco. Las tres correcciones: **su prueba de
muerte, tal como está escrita, la mata por calibración y no por física** (§2, §4); **su escalera
de onboarding arranca por el único sistema que el laboratorio no consiguió mantener vivo en veinte
rondas** (la planta; R148, R150); y **omite el paquete D («el hogar come»)**, sin el cual SOLTAR
nunca arriesga el fuego doméstico y el mundo es estático desde el día ~5 (§2.9).

## 0. Unidades que hay que tener delante

- **Visita** = 8 ticks (`LabCampos`: `offset = _tick % 8`). Todo «visitas» de `LabParams` se
  multiplica por 8. `DepositoReposo = 24` visitas = 192 ticks = 6,4 s; `CompactReposo = 200` =
  1 600 ticks = 53 s; `PlantaMarchitaVisitas = 40` = 320 ticks = 10,7 s.
- **Día** = 1 800 ticks = 60 s a ×1, 6 s a ×10 (dato humano, no ley).
- **Banco**: 500-660 ticks/s en `RunCommand` sin Play (banco del 2026-09-06: 1,5-2,0 ms/tick en
  los nueve escenarios, 3,0 en diluvio y mundo despierto); una sonda por MCP se corta a los dos
  minutos, así que 72 000 ticks son **dos sondas** con los estáticos de `ArcoIniciar/Avanzar`.
  Desde el menú del editor corre entero (lo lanza Cesar).

## 1. Qué necesita del sustrato, y qué dice el código

| pieza | ¿en el catálogo? | ¿acotada y verificable en banco? | dependencia secuencial |
|---|---|---|---|
| diario bajo las cinco puertas (`Paint` 571, `PaintCell` 610, `PaintStable` 741, `PaintRect` 774, `PaintLab` 831; las cuatro primeras ya llaman a `ReenviarSiEspejo`, y `SimSync` ya serializa x,y,radio,mat,modo,temp en 8 bytes) + entrada de parámetro + **id de gesto** + byte de autor | J·sello (viable ×2); id de gesto y byte de autor no están en el catálogo (10 líneas) | sí: prueba (d) de la refutación, `HashMat` vivo == `CorrerSello` | ninguna |
| volcado/carga (11 arrays + `_tick` + libro + `_labManantialCeldas` + `LuzCieloX0/X1` + `_zonaInteresChunk`) | J·sello | sí: round-trip 4 500 + 4 500 → siete hashes de r141 | diario |
| `CorrerSello` + condición como dato + «día N sin manos» + apuesta juzgada | J·sello | sí | volcado |
| balanza-lite (bit `entregable`, tragar solo `i+W`, libro por `aux`, histograma de `carga`) | J·balanza (viable ×2) | sí; `LabTragar` es un `if` de arquetipo; hashes intactos con id 0 | ninguna |
| `LabBandas` única fuente de umbrales; anillo de hashes cada 256 ticks | J | sí | ninguna |
| halo de la mano (vista Piel de C sobre el cursor) + lector | C reducido | no es física; solo lee `temp/humedad/luz` | ninguna |
| la MANO (azada de 5 celdas, CAVAR a cubo con `counts[]`, PONER, PRENDER, SONDAR) | no está en el catálogo: capa de control nueva que sustituye a cincel/frasco/aprendiz | no (sensación); sí en lo que escribe: todo pasa por las cinco puertas | diario (para que cada verbo sea entrada) |
| día de L: quinta pasada en `LabLuz` + remedir Q16 con `luz[i+W]` | L | sí, un día; **la remedición sola son diez minutos** (`ArcoMuestra` suma `g.luz[i]` de la celda de sedimento: basta leer `i+W`) y no toca física; la quinta pasada sí mueve `HashLuz` en nueve escenarios | ninguna |
| F núcleo: fusión con reserva, fusible de hielo | F (viable ×2) | sí: caja adiabática | ninguna, pero ver §2.6 |
| **D: el hogar come** (no está en la dirección) | D (crítico de completitud; 0,5-1 sem, tuning 8) | sí: «hogar sin combustible no decae en 3 000 ticks» | ninguna |
| sellar a ciegas **mientras se sigue jugando** | no listado | exige hilo de trabajo para un segundo `SimStepper` (C# puro, viable) **y cielo por geometría** (`LabParams.LuzCieloX0/X1` es estático: dos grillas con bocas distintas no coexisten en un proceso) | clon en memoria (refutación de la cuna) |
| V (arrastre, siega) para horizontes de 100 días | V | sí, con su prueba «huerto de banco» | L (Q16) |

Nada pide física refutada. Estado escondido fuera de `CellGrid`: decenas de bytes.
**Determinismo entre máquinas, medido en el código**: `SimStepper.cs` tiene 4 líneas con
`float/double` (`LastStepMs`, y `NewtonK`/`EmisionTermica` de las placas heredadas que
`SpawnLaboratorio` no crea), el partial tiene 1 (`MsDifusion…`, timing) y `Universe.Create` tiene
57 (`Mathf.Clamp/Lerp/RoundToInt`: la tabla de materiales). El riesgo está **confinado a la tabla**,
y el arreglo definitivo es barato: serializar los `MaterialDef` del universo dentro del fichero
(no solo su hash) y cargarlos en vez de recrearlos. Con eso, el «hash de versión detecta la
divergencia» pasa de aviso a garantía. Aun así, un día: `LabBench` en la build IL2CPP contra los 63
hashes del editor, antes de la semana 2, porque el co-op por fichero sin roles ES el multiplayer de
la dirección.

## 2. Evidencia que la sostiene y evidencia que la contradice

**La sostiene.** El libro mayor ya tiene ~35 contadores y `ArcoMuestra` ya es una fila por día: el
recibo por día existe. El banco corre a 500-660 ticks/s: 30 días en 90 s, así que «sellar a ciegas»
es barato (300 días son 15-18 min, no «cinco»). Y el agua **discrimina de verdad y de forma
continua**: R133 mide la vida del filtro como función de la turbidez (carga 255 → ~15 s; 40 →
~100 s; 0 → nunca), la decantación es un continuo (6 u por visita en reposo) y R135-R8 mide que el
rocío va entero al bloque más frío. La geometría del agua es el eje donde el sustrato ya produce
curvas, no escalones. La dirección lo cita de pasada y debería construir sobre ello.

**La contradice, punto por punto:**

1. **«Goteos ≥ 500/día» falla en todo lo medido.** R148: 9 588 goteos en 30 días = **320/día**
   (360/día entre los días 5 y 10); R150: 1 624 en 7,5 = **217/día**; R141: 900 en 5 min =
   180/día. La cláusula está 1,5-3× por encima del régimen que el laboratorio produjo con la
   caldera de código. Las seis variantes fallan el día 1; la cláusula no discrimina. Y con
   manantial y núcleo frío como pins eternos, «goteos ≥ n» **no cae nunca** en banco: no es un
   reloj, es una constante de la geometría.
2. **«Planta viva el día 30» falla en todo lo medido** (R148: 0 vivas, 2/2; R150: 0 vivas, 9/9,
   humedad de la cara 50-99 contra 60). Y, según el código, es **trivialmente satisfacible por el
   camino contrario**: el caso `Semilla` de `LabPoroso` pide sustrato húmedo debajo y `luz[i] ≥
   40`, **no aire encima**; el agua transmite luz; `LabSecarHacia` solo transpira hacia `Empty`,
   así que una planta sumergida no pierde savia y la raíz bebe de un sustrato que el agua de encima
   mantiene ≥ 60. Una raíz sumergida de una celda es inmortal. Inundar el lecho cumple «planta
   viva». La cláusula honesta pide crecimiento (celdas ≥ n, nacidas − muertas en ventana, altura
   ≥ 3), no existencia. Una hora de banco lo confirma.
3. **«Carbón ≥ 100» pasa en todo lo medido.** R148 (arco largo, boca 1): 122; R135 aislada: boca 1
   → 400, boca 4 → 325, boca 8 → 350 (no monótono; boca 4 y 8 a menos de un σ del dado del 25 %).
   Las seis variantes pasan.
   **Resultado de la prueba de §8 tal como está escrita: planta 6× fallo, goteos 6× fallo, carbón
   6× pase, todo decidido antes del día 3.** Sus dos criterios de muerte se disparan por
   calibración de umbrales, sin que la simulación haya tenido ocasión de discriminar.
4. **«El sumidero es fondo: una salida se ciega sola» no se sigue del código.** `EsFondo` incluye
   al sumidero (`LabMateriales.cs:98`), pero el depósito exige agua con carga ≥ 200 **quieta 24
   visitas** y `LabTragar` vacía las cuatro celdas vecinas **en cada visita del sumidero**: el
   agua que lo toca nunca acumula reposo. La salida solo se ciega si un **polvo** (sedimento,
   densidad 150 > 110 del agua; grava 200) resbala sobre su cara, porque `LabTragar` solo traga
   `Liquid`. Reloj real, dependiente de geometría (una poza cuyo talud de depósito descargue sobre
   la salida) y no medido.
5. **La «presa de arcilla empapada» es bimodal.** `PermArcilla = 2`: infiltración desde agua =
   32·2·255³/255⁴ = 0,25 → **0 por visita**. Una presa seca contra agua abierta no se moja nunca.
   Solo se empapa por percolación desde un poroso mojado encima (1 u/visita) o capilaridad lateral
   desde sedimento saturado (4/256 de la diferencia, ≤ 3 u/visita): 250 u en 1-3 días. **Contra
   agua limpia aguanta para siempre; contra barro cede en días.** Eso es una ley legible y un
   reloj; no es lo que la dirección describe.
6. **El fusible de hielo «para el día 20» no existe con la térmica de hoy.** `TiroAmbienteTicks
   = 32`: toda celda sin pin va hacia ambiente 1 raw cada 32 ticks; el hielo se derrite por encima
   de 60 raw. Un bloque a 30-40 raw llega a 61 en 700-1 000 ticks: **0,4-0,55 días, esté donde
   esté el fuego**. Con F, la meseta latente se dimensiona con `LatenteFusion`, y un fusible de 20
   días exige una reserva ~40× el medio día natural: es un número de balance con nombre de ley. Se
   tabula en una tarde (distancia al hogar → ticks de fusión).
7. **«El hilo roza el lecho» no lo moja.** Infiltración lateral desde agua a sedimento:
   32·12·255²·128/255⁴ = 0,76 → **0 por visita**; vertical: 1 u/visita. Un arroyo al lado del
   lecho no le da agua y sí lo **erosiona** (carga 40 ≤ `ErosionCargaMax` 64, 6 % por evento;
   R134: 74 → 22 celdas en 300 s), salvo la celda justo bajo cada celda de planta. Los «quince
   segundos» del minuto 1-2 solo salen con agua ENCIMA del lecho, que es el régimen de
   bañera/rebosadero de R135. La situación 1 es el problema H4 sin resolver, no diez líneas.
8. **×10 no es ×10 cuando hay algo que mirar.** `AlkahestSim.Update` corta por `LabPresupuestoMs`
   (20 ms) y descarta el tiempo que no cupo; con picos de 8-14 ms/tick (banco: diluvio 14,0,
   mundo despierto 10,5) el mundo corre a ×2-3 en los minutos de drama y `LabMultiplicadorReal`
   lo delata. No rompe el sello (todo va por tick); rompe la promesa de la frase.
9. **Después del día ~5 el arco es estático, y no es un eslogan: está en r148.** «30 008 unidades
   quemadas y 122 celdas carbonizadas en los primeros cinco minutos; el resto del arco, sin
   actividad»; humo 0 desde el día 10; anegadas 0/36 desde el día 15; lo único que sigue moviéndose
   hasta el día 30 es la humedad del lecho (33-100 %) y el contador lineal de goteos. «La carbonera
   de 400 celdas arde once días» no está medida y r148 la contradice. Tolva 7,8 días en su propio
   escenario; filtro 0,7-1,7 días con agua del manantial. Los horizontes de 100-300 días de la hora
   20 dependen de V (arrastre y siega, «después y según Q16») y de D (el hogar come, ausente); con
   hogar, manantial y núcleo frío como pins, **SOLTAR no arriesga ningún suministro**.

**Un cruce a favor que la dirección no explota:** la vida del filtro como función de la
decantación de la poza aguas arriba es una curva continua y monótona ya medida (R133). «Decanta
primero, filtra después» es la primera situación honesta del juego, no «El hilo».

## 3. Iteración humana oculta

Declara 8/10 (doce umbrales, un día, tres sesiones). Lo que esconde:

- **El registro del autor por situación**: el validador exige que el autor cumpla; alguien
  resuelve cada situación. 12 × 0,5-1 día = **6-12 días** de Cesar o Fable (parcialmente
  automatizable buscando entre diarios mutados, pero la primera solución es humana).
- **La mano**: cuatro verbos nuevos con cubo, alcance y ritmo son diseño de control con tuning de
  sensación (el control corporal costó nueve rondas, R110-R121; una mano es más barata pero no es
  cero): 2-4 días, no validables en banco.
- **Los umbrales**: el validador caza lo trivial y lo imposible; §2.1-2.3 muestran que los tres
  umbrales de ejemplo estaban fuera del régimen medido. Mitigación técnica: cláusulas **relativas
  al recibo del registro vacío** («≥ 2× lo que da el mundo sin manos») e **integrales** (por
  ventana, no instantáneas). Quita el número por situación; no quita la elección del horizonte.
- **La reserva del fusible** (§2.6) y los dos números de D: tres números.
- **Toda situación caduca con cada cambio de física** (F, V, D, la quinta pasada de luz mueven
  hashes): el validador se relanza solo (12 situaciones × ~12 corridas × 54 000 ticks ≈ 4 h de
  banco nocturno), pero si el registro del autor deja de cumplir, alguien resuelve otra vez.
  Consecuencia de orden: **congelar la física del prototipo antes de validar la escalera**.
- **El playtest «juego o examen»**: 3 días; si es examen, el arreglo no es un banco.

Total realista: **15-25 días-persona** en las primeras diez semanas. Nota corregida: **6/10**.

## 4. La prueba más barata capaz de matarla (rediseñada)

**Una semana, en banco, sin sello, sin personas.** Un evaluador desechable sobre el libro por día
(vector de contadores cada 1 800 ticks: `ArcoMuestra` generalizado a cualquier montaje) + la
intervención genérica del banco (quitar tapón en T; la caldera ya es una) + el **protocolo de
equivalentes ±k**: cada montaje corrido +1 y +2 celdas en x (mismo diseño, otro dado) para medir
el suelo de ruido. Se corre desde el menú del editor (una tirada de ~1 h) o por MCP en sondas de
36 000 ticks con los estáticos de `ArcoIniciar` (o un hilo C# que sobreviva a la sonda).

**Prueba A · Relojes con suelo de ruido (la que decide).** Seis aparatos con su cláusula natural y
su única palanca: filtro de grava bajo manantial (claras/día < 50 % del día 1; palanca: largo de la
poza de decantación aguas arriba, 0/8/16/32), tolva (boca < 200 raw; palanca: altura 15/30),
carbonera (carbón deja de crecer; palanca: boca 1/3), lecho bajo goteo (sustrato < 50 %; palanca:
labio ±1), presa de arcilla (primera celda ablandada; palanca: sedimento saturado a un lado sí/no),
salida bajo poza turbia (tragado/día < 50 %; palanca: profundidad 2/6). 6 aparatos × 3 palancas ×
3 equivalentes = 54 corridas × 36 000 ticks ≈ 1 h de banco. **Se mide**: día de primer fallo por
corrida; dispersión intra (equivalentes) e inter (palancas). **La mata**: todo reloj < 1 día o >
500 días, **o** inter/intra < 1,5 en todos los aparatos (la palanca no mueve el día de fallo más
que el dado). **La confirma**: al menos tres aparatos con inter/intra ≥ 3 y relojes entre 2 y 100
días.

**Prueba B · Discriminación por veredicto (la de §8, corregida).** Las seis variantes del arco y
del alambique, pero con cláusulas **relativas** (goteos/día ≥ 1,5× el registro vacío; carbón ≥
0,5× el máximo aislado de R135; planta: celdas de planta ≥ 3 en ventana de 3 días) y con día de
primer fallo como dato, no pase/fallo. **La mata**: menos de 4 de 6 con día de fallo distinto fuera
del suelo ±k, o todos los fallos antes del día 3.

**Prueba C · Una tarde**: semilla sumergida (§2.2), tabla distancia → ticks de fusión del hielo
(§2.6), y la lectura de `luz[i+W]` en el arco de r148 (§1) sin tocar `LabLuz`. Las tres deciden qué
cláusulas son honestas antes de escribir el JSON de ninguna situación.

Coste: 3-4 días de Opus, 1 de verificación de Fable, 0 de Cesar (más el día de la build IL2CPP,
que sí es suyo).

## 5. Tiempos corregidos

| hito | Opus | calendario | personas |
|---|---|---|---|
| evidencia para matarla (§4 A+B+C) | 4 días | **1 semana** | 0 |
| día de L (remedir Q16 con `luz[i+W]`, después la quinta pasada) + determinismo IL2CPP + tabla de materiales en el fichero | 2-3 días | semana 1-2, en paralelo | 1 (build) |
| prototipo feo: diario (id de gesto, byte de autor) + volcado/round-trip + `CorrerSello` + condición (relativa e integral) + balanza-lite + `LabBandas` + anillo + clon + cielo por geometría + **D** + mano + gesto (SOLTAR, contador, tira, apuesta, halo) + 6-8 situaciones de agua y fuego validadas | 7-8 sem | **7-8 semanas** en dos hilos (cadena diario → volcado → `CorrerSello` → gesto → situaciones, 5-6 en serie; D, balanza, bandas y halo en paralelo; verificación de Fable ~1:1 como en el laboratorio) | 8-12 días |
| playtest «juego o examen» | — | semana 8-9 | 3 días |
| F núcleo (fusible con reserva dimensionada) | 1-1,5 sem | +1,5, antes de re-validar la escalera | 1-2 días |
| V (arrastre, siega) para horizontes de 100 días | 1,5-2 sem | +2, tras el playtest | 1 día |

La dirección dice 6-7 semanas a prototipo feo y 1,5 a evidencia; corrijo a 7-8 y 1. La mano, D y
la verificación son la diferencia. No construir F ni V antes del playtest de la semana 8, y
validar la escalera solo con la física congelada.

## 6. Qué se automatiza en lugar de iterar a mano

El suelo de ruido por aparato (equivalentes ±k), el día de fallo de cada reloj como función de su
palanca, la discriminación por variante, la validez de cada situación y mutante (vacío falla, autor
cumple, K distintos), la re-validación nocturna tras cada cambio de física, el envejecimiento, el
determinismo entre máquinas por hash y por tabla en el fichero, el salto headless al día N sobre
clones (en vez de mirar ×10), las cláusulas relativas al recibo del vacío (jubilan el umbral
humano), y la atribución por rejugado sin las entradas de B, que es un **contrafáctico**, no una
auditoría (en un falling-sand caótico, quitar a B cambia el mundo entero; la misma medida de
fragilidad del validador dice cuánto ruido lleva la culpa). Lo que no se automatiza: resolver cada
situación una vez, elegir su horizonte, la sensación de la mano, y si mirar es placer.

## 7. Órganos a conservar si se descarta

SOLTAR como **medida** («último día en que todas las cláusulas se cumplieron tras el último
toque»), no como modo; la apuesta de una frase juzgada por la simulación; el byte de autor y el
rejugado contrafáctico; `tickSellado` y días vistos (la racha a ciegas); el halo de la mano; el
recibo por día como tira de bandas; el evaluador por día sobre cualquier montaje y el protocolo de
equivalentes ±k; las cláusulas relativas e integrales; la tabla de relojes de la prueba A (vale
para cualquier dirección); el fusible de hielo como temporizador tabulado; la mano con cubo por
material (PONER con temperatura); la tabla de materiales serializada en el fichero; todo el paquete
J, que es común a El Recibo.

## 8. Rúbrica v2

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | J no añade física; F y D añaden una cada uno; convierte contadores en juicio y apuesta |
| ejecutan, revelan y juzgan | 8 | juzgan sin balance; ejecutan ~5-15 días y luego el mundo se para (r148); revelan poco hasta M |
| iteración humana (10 = poca) | 6 | 15-25 días-persona; mano, registro del autor, horizontes, reserva del fusible, re-resolver tras cambios de física |
| verificabilidad automatizable | 9 | la mejor del panel junto con El Recibo; la puerta es una semana |
| la simulación es el juego | 8 | la condición es una línea; la apuesta, una nota |
| onboarding garantizable | 5 | el validador caza trivial e imposible; la primera situación narrada es H4 sin resolver; reordenar agua → fuego → planta |
| observabilidad | 6 | halo, bandas y tira en el prototipo; campos en F8, plantas de un píxel; ×10 no sostenido en el drama |
| tiempo como apuesta | 9 | es el verbo; sellar a ciegas necesita hilo y cielo por geometría |
| multiplayer emergente | 5 | diario multiautor sin roles es real y barato; cross-machine sin probar; nada «divertido juntos» demostrado |
| profundidad por leyes estables | 5 | el agua discrimina en continuo; el fuego en dos regímenes más dado; los horizontes largos dependen de D y V |
| cuerpo del jugador | 2 | sin avatar por decisión; solo el halo |
| identidad comercial | 7 | frase, contador y fichero que circula; arquetipo con el mejor suelo de `03`; el clip exige estado legible que hoy no hay |
| dificultad técnica (10 = fácil) | 8 | C# puro y acotado; lo delicado: id de gesto, hilo del banco, cielo por geometría, cross-machine |

**Riesgo mayor (ingeniería):** que los relojes fuera de la planta sean escalones de geometría
(presa seca/mojada, bañera/rebosadero, boca 1/resto) más el dado del 25 %, y no curvas: entonces
«DÍA N SIN MANOS» sería una constante por aparato y la racha no premiaría entendimiento sino haber
elegido el lado bueno del escalón. El agua (R133, R135-R8) dice que hay curvas; el fuego (R135,
R136) dice que hay escalones; r148 dice que después del día 5 solo quedan los pins. La prueba A lo
mide en una semana. Y una advertencia de proceso: la prueba de §8 tal como está escrita mataría
la dirección por sus propios umbrales; correrla sin corregir sería medir mal y creerse el
resultado.
