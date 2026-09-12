# CRÍTICA · EL RECIBO (leyes-juzgan) · lente INGENIERÍA Y EVIDENCIA

*(Director técnico, segunda pasada, 2026-09-12. Leído entero: `leyes-juzgan.md`, `01_LEYES.md`,
las refutaciones de sello (×2), balanza y cuna, `LabBench.cs` (523 líneas), `SimStepper.Laboratorio.cs`
(libro mayor :49-119, `LabTragar` :1026-1033, `LabPlanta` :801-907, `LabPoroso` caso `Semilla` :684-695,
`LabRespira` :1052-1061, `LabLuz` :1177-1266), `SimStepper.cs` (`Step` :257-330, `ProcessCombustion`
:829-938 y el dado de carbonización :919-930, `GetCombustReserva` :766), `LabParams.cs` (estático;
`Registro` con `Leer/Escribir`), `Cincel.TallarTick` (:383-478), `XorShift.FromCell`, `CellGrid` (once
arrays), benchmarks R133, R135, R136, R148, R150 y la tabla del banco de 2026-09-06.)*

**Veredicto: FINALISTA, con puerta de una semana y dos correcciones obligatorias.** Es la dirección
con menor coste de morir del panel: todo lo que hay que construir para cruzar la puerta es el paquete
J, que cualquier otra dirección necesita («J primero y siempre»). Las dos correcciones: **su prueba
de muerte, tal como está escrita, no distingue una frontera estrecha del ruido del dado del 25 %**
(§2, §4), y **la situación insignia (la carbonera) es, por lo medido, la de física más plana del
laboratorio**; la puerta tiene que incluir una situación de agua, donde el sustrato ya produce curvas.

## 1. Qué necesita del sustrato, y qué dice el código

| pieza | ¿en el catálogo? | ¿acotada y verificable en banco? | dependencia secuencial |
|---|---|---|---|
| diario bajo las cinco puertas (`Paint` 571, `PaintCell` 610, `PaintStable` 741, `PaintRect` 774, `PaintLab` 831) + entrada de parámetro | J·sello (viable con ajuste ×2) | sí: prueba (d) de la refutación (`HashMat` vivo == `CorrerSello`) | ninguna |
| **id de gesto y byte de autor en la entrada** | no está en el catálogo; 10 líneas | sí | diario |
| volcado/carga: 11 arrays de `CellGrid` + `_tick` + libro (~30 `long`) + `_labManantialCeldas` + `LuzCieloX0/X1` + `_zonaInteresChunk` + hash de la tabla de materiales | J·sello | sí: round-trip 4 500 + 4 500 → los siete hashes del alambique | diario |
| `CorrerSello` + condición como dato + «día N sin manos» | J·sello | sí | volcado |
| balanza-lite: bit `entregable`, tragar solo `i+W`, libro por `aux` de la salida, histograma de `carga` | J·balanza (viable ×2); `LabTragar` :1030 es un `if` de arquetipo | sí; hashes de referencia intactos con id 0 | ninguna |
| intervención genérica del banco (hoy cableada a `esAlambique`, LabBench.cs:306) | refutación balanza | sí, horas | ninguna |
| clon en memoria (13 `Array.Copy` ≈ 3 MB) | refutación cuna | sí: alambique bifurcado en 4 500 → mismos hashes | ninguna |
| cielo por geometría (fuente = `Empty` en H−2) | refutación cuna | sí, por hash del nivel de referencia | ninguna |
| validador por veredicto, envejecer, mutaciones, ruinas | J·cuna reducida | sí, pero **en serie** (`LabParams` estático) | CorrerSello |
| firma de leyes | nuevo | sí, en serie (~12-15 min por situación) | CorrerSello |
| gesto IMGUI (SELLAR, contador, recibo, línea de días, diff, carpeta) | — | no (personas) | recibo + clon |

Nada pide física refutada. La única física nueva es «la salida es un agujero para lo granular», una
línea en `LabTragar`. Estado escondido fuera de `CellGrid`: decenas de bytes. Cero `float` en el
partial; el RNG es `XorShift.FromCell(tick,x,y,sal)`, sin estado. Los `double` de `SimStepper.cs`
(:2968-3036) son de las placas heredadas, que el laboratorio no crea.

**Cinco huecos técnicos que la dirección no nombra y que son suyos:**

1. **«Toque» no está definido.** `Cincel.TallarTick` escribe hasta `CarveRatePerTick = 3` celdas
   por tick en un disco de radio 2 (:403-407): un gesto sostenido un segundo son 90 llamadas a
   `Paint`/`PaintLab`. Los «3 toques» del minuto 1 serían 40-90 entradas. Hace falta un id de gesto
   (pulsar → soltar) en la entrada del diario; sin él la columna «toques» mide píxeles.
2. **«Huella = diff de `mat[]` con el nacimiento» es agua.** Con `Caudal` 24 celdas/s el manantial
   mueve `mat[]` sin descanso; y una pila que arde cambia 400 celdas sin que nadie la toque. Definir
   huella como **unión de las celdas escritas por el diario** (barata, exacta, sin física) o como
   diff solo sobre categorías sólidas. Hay que decirlo antes de que alguien la mida.
3. **Determinismo entre máquinas no está probado.** Los 63 hashes prueban una máquina, una build,
   Mono. `Universe.Create` usa `Mathf` (Clamp/Lerp/RoundToInt/Repeat: IEEE sin trascendentes,
   riesgo bajo). La tabla de amigos, el relevo y el histograma dependen de ello. Un día: `LabBench`
   en la build IL2CPP contra los 63 hashes del editor, más el hash de la tabla de materiales en el
   fichero para rechazar al cargar en vez de divergir a los 9 000 ticks. Tiene que estar antes de la
   semana 2 y no está en el calendario.
4. **`LabParams` es estático** (y `LuzCieloX0/X1` también). Validación, mutaciones y firma corren
   en serie en un proceso: 20 situaciones × 20 perturbaciones × 18 000 ticks × 1,6-3 ms ≈ 4-6 h.
   Lote nocturno viable; «cien mutantes por noche» son cien en serie, no mil. Instanciar los 96
   parámetros en un struct que reciba el stepper es un refactor mecánico de 1-2 días verificable por
   los 63 hashes; no bloquea el prototipo, bloquea la escala.
5. **La cláusula «planta viva en (x,y)» es trivialmente satisfacible.** `LabPoroso`, caso `Semilla`
   (:688), no comprueba aire encima: una semilla bajo una celda de agua germina si el sustrato tiene
   ≥ 60 y le llega luz (el agua transmite, −20 por celda). `LabPlanta` solo muere por savia (:885) y
   bajo el agua no transpira (`LabSecarHacia` exige `Empty`, :735): una raíz sumergida de una celda
   es inmortal. Inundar el lecho cumple «planta viva el día 20». Las cláusulas de vida tienen que
   pedir crecimiento (`aux` ≥ n, celdas de planta ≥ n, nacidas − muertas en ventana), no existencia.
   Una hora de banco lo confirma; diez líneas lo arreglan; pero enseña que cada pedido es autoría.

## 2. Evidencia del laboratorio que la sostiene y que la contradice

**La sostiene.**

- El libro mayor ya tiene ~30 contadores `long` y `ArcoMuestra` ya es una fila por día de 1 800
  ticks: el recibo por día existe. `LabBench.Correr` ya hashea los siete campos y ya admite una
  intervención tick-estampada (la caldera): generalizarla es la prueba §8 y cuesta horas.
- El banco corre a 500-660 ticks/s (tabla de 2026-09-06): 9 000 ticks son 14-18 s; 30 días (54 000
  ticks) son 90-110 s. El barrido de §8 (48 corridas) son 12-15 min; el mío (96) 25-30 min.
- **El agua sí produce curvas continuas.** R133: la vida del canal como función de la turbidez
  (carga 255 → ~15 s; 40 → ~100 s; 0 → nunca), la poza decanta de 96 a 45 y la decantación es 6 u
  por visita en reposo. R135 R8: el rocío va entero al bloque más frío (5 926 u contra 0). Es el eje
  donde el sustrato ya discrimina de forma continua, y la dirección lo cita de pasada.
- El cruce «el tapón de fibra que cede solo» (minuto 6) es plausible por código: la fibra no es
  entregable y se apoya; al volverse carbón o ceniza (ambos entregables) la salida la traga y lo de
  encima cae. Un temporizador con una ley. Se verifica en una tarde.

**La contradice, y es lo que la puerta tiene que medir.**

1. **El dado de la carbonera.** `ProcessCombustion` :919-926: cada celda que agota su reserva en
   sordina tira `ChancePercent(RendimientoCarbonPct = 25)` con sal 632 sembrada por `(tick, x, y)`.
   El carbón de una pila es una binomial: 400 celdas → media 100, σ ≈ 8,7 (CV 9 %); 200 celdas
   → media 50, σ ≈ 6,1 (CV 12 %). Como el dado se siembra por posición y tick, **dos máquinas
   equivalentes (la misma corrida una celda a la derecha, o abierta ocho ticks después) resiembran
   todos los dados y difieren un 9-12 % en la columna carbón por construcción.** El criterio de
   muerte de §8 («dispersión < 15 % entre las que cumplen ≥ 40») está a 1-1,7 σ del suelo de ruido
   del propio dado: puede dispararse con ruido puro y puede no dispararse con ruido puro. Tal como
   está escrita, la prueba no distingue «frontera estrecha» de «dado más una ley débil».
2. **La boca no regula la pila maciza.** R136: Tfuel 249,8/249,8 con humo 4 %/40 % («idéntico al
   bit»); 252,1/252,1/252,2 con chimenea 0/1/2; «plana». La llama es inmortal, cuenta como aire en
   `LabRespira` (:1057) y el aire no se gasta. R135 (antes del dado): boca 1/4/8 → 100/81/88 % de
   carbón, **no monótono**; boca 4 y 8 distan 25 celdas de 400, que con el dado de hoy son ~3 σ en
   una pila de 400 pero < 1 σ en una de 100. Predicción desde el código: el eje «boca» separa boca 1
   del resto y poco más. La frontera de la situación insignia sería de dos puntos, no una curva.
3. **Calor mide superficie, no decisión.** `LabRawLlama` domina (40 raw/tick por lengua contra
   2,75/tick del combustible, R136 §2.3): la columna calor es superficie expuesta × tiempo. La
   tensión «carbón contra calor» existe (respirar deja ceniza y calor entero; sordina deja carbón y
   medio calor) pero es de **dos regímenes**, no un continuo.
4. **El nivel del agua es escalón.** R135: el labio una celda más alto anega 24 de 48 columnas. En
   las situaciones de nivel el recibo será bimodal (bañera o rebosadero). El continuo está en
   turbidez y reposo (R133), no en altura.
5. **La mitad de la hora 5 no existe.** Huerto, invernadero y «planta viva el día 20» piden L
   (remedir Q16 con `luz[i+W]`, vidrio transparente) y V (siega, arrastre): 5,5-6,5 semanas que no
   están en el núcleo. R148: luz 0 en la cara del lecho durante 30 min, 2 nacidas; R150: con boca
   de 25 columnas, 9 nacidas y 9 muertas (humedad de la cara 50-99 contra 60). Hoy hay situaciones
   de fuego y de agua; las de vida son promesa.
6. **El salto al día N no es más rápido que mirar.** A ×10 con presupuesto de 20 ms el juego corre
   ~600 ticks/s si el frame aguanta; el banco corre 500-660. Ir hacia delante cuesta lo mismo con o
   sin pantalla; lo que el clon compra es que ir hacia **atrás** sea gratis. El tedio de los tres
   minutos se mitiga con clones y con poder preparar una rama mientras otra corre (dos rejillas de
   3 MB caben), no con velocidad.

**Un cruce a favor que nadie ha medido, y que decide si T es un eje.** Con el bit `entregable`,
abrir la salida en T = 0 no se come fibra: la columna se apoya en la salida y T parecería dominado
(abrir siempre en 0). Pero la celda tragada bajo la pila queda `Empty` un tick y el polvo cae:
bolsas de aire por debajo → `LabRespira` → esas celdas arden enteras → ceniza en vez de carbón, más
calor. Si el efecto es medible, «abrir antes = rápido y menos carbón» es una frontera real escrita
por dos leyes que nadie diseñó. Si es ≤ 1 σ, T es dominado y la situación insignia tiene un eje
menos. La prueba lo decide.

**La única modificación del sustrato que la puerta puede exigir, y es barata:** si el dado domina,
sustituir el `ChancePercent(25)` por una **cuota determinista por posición** (una de cada cuatro
celdas de la pila, por el patrón de `LabPasoSordina`, sin RNG): el conteo deja de tener varianza, la
conservación de energía (R136 C2) se mantiene, cuesta un día y mueve dos o tres hashes (carbonera,
tolva, arco largo). Es el tipo de añadido que la función objetivo prefiere: poco código que
transforma la lectura de todas las situaciones de fuego. No hay byte libre en la fibra para una
memoria por celda (la reserva vive en `aux`, :766-770; `carga` y `reposo` están en uso), así que la
cuota por posición es la única versión de un día.

## 3. Iteración humana oculta

La dirección declara 6-8 días de personas. Lo que esconde:

- **El registro del autor por situación.** El validador exige «el registro del autor cumple»:
  alguien tiene que RESOLVER cada situación en el editor, y comprobar que enseña lo que dice. 12-20
  situaciones × 0,5-1 día = **6-20 días** de Cesar o Fable, incompresibles.
- **La magnitud del pedido es un número de balance.** «60 carbones», «2 000 unidades»: bajo es
  trivial, alto es imposible; el validador (vacío falla, autor cumple, K distintos) no mide
  dificultad, y la firma de leyes mide cuántas leyes importan, no cuánto cuesta. Mitigación
  técnica: pedidos relativos al recibo del vacío («≥ 3× lo que da el mundo sin manos») o situaciones
  sin pedido con histograma; ambos evitan el número por situación.
- **Las cláusulas de vida piden crecimiento, no existencia** (§1.5): cada cláusula-testigo tiene
  un modo de satisfacción trivial que hay que buscar a mano hasta que exista un «solver tonto» en
  el validador (inundar, tapar, no tocar).
- **Los bordes de las bandas** («clara», «al rojo») siguen siendo números de autor; el histograma de
  `carga` quita uno, no todos.
- **La consecuencia del playtest de la semana 6.** Si «es examen», el arreglo no es un banco.

Total realista: **15-25 días-persona** en las primeras diez semanas, no 6-8.

## 4. La prueba más barata capaz de matarla (corregida)

**Semana 1, en banco, sin sello y sin personas.** Balanza-lite (bit `entregable`, tragar solo desde
arriba, contador por material y tick del primer carbón: un día) + intervención genérica del banco
(quitar el tapón en T: horas) + la carbonera del banco sobre salida tapada.

- **Barrido:** boca ∈ {1, 2, 4, 8} × T ∈ {0, 1 500, 3 000, 6 000} × tapón ∈ {esquina, centro} = 32
  configuraciones. **Por cada configuración, tres equivalentes**: el montaje entero corrido +1 y +2
  celdas en x (mismo diseño, otro dado). 96 corridas × 9 000 ticks ≈ 25-30 min.
- **Se mide:** carbón tragado, fibra tragada (debe ser 0), tick del primer carbón,
  `LabRawFuego + LabRawLlama`, ceniza; y por columna la dispersión **intra** (entre equivalentes) e
  **inter** (entre configuraciones que cumplen ≥ 40).
- **La mata:** inter/intra < 1,5 en carbón Y en tick del primer carbón (la frontera es el dado), o
  todas las configuraciones que cumplen dentro de ±1 σ intra (una sola máquina). Si es el dado, se
  aplica la cuota determinista (§2) y se repite en un día; si con cuota sigue < 1,5, la situación
  insignia muere y la dirección se juega en el agua.
- **La confirma:** inter/intra ≥ 3 en al menos dos columnas con puntos no dominados a distinto T o
  boca.
- **Segunda situación, misma semana:** «decantación con salida» (manantial turbio, poza de largo
  L ∈ {4..32} y profundidad P ∈ {1..6}, salida en el fondo o por rebose): histograma de `carga` al
  tragar frente a caudal. R133 predice un continuo; si sale bimodal sin continuo, las situaciones
  de agua no tienen histograma y la dirección muere entera.

Coste: 4 días de Opus, 1 de verificación de Fable, 0 de Cesar. Resultado en una semana. La segunda
evidencia (cinco personas, tres situaciones, ±10 %) queda para la semana 6, con el sello.

## 5. Tiempos corregidos

| hito | Opus | calendario | personas |
|---|---|---|---|
| evidencia para matarla (§4) | 4 días | **1 semana** | 0 |
| determinismo cross-machine (build IL2CPP contra 63 hashes) + hash de tabla de materiales | 1 día | en la semana 2, en paralelo | 0 |
| prototipo feo que permite juzgar el core: diario con id de gesto + volcado + `CorrerSello` + balanza + clon + cielo + gesto mínimo (SELLAR, línea de días con clones, recibo en texto), 3-4 situaciones de fuego y agua | 5,5-6 sem | **5 semanas** (cadena sello → gesto, 3 semanas de Opus en serie ×1,4 de verificación = 4,2; balanza/clon/cielo/cross-machine en paralelo) | 3-4 días (resolver las situaciones) |
| playtest que decide «juego o examen» | — | semana 6 | 2-3 días |
| núcleo completo de §5 (validador, mutaciones, ruinas, firma, bandas, anillo, carpeta) | +3,5-4 sem | +2-3 semanas | 2 días |
| L y V para las situaciones de vida | 5,5-6,5 sem | +4 semanas | 1 día por paquete |

El factor 1,4 sale del laboratorio: seis de las veintiuna rondas fueron verificaciones de Fable
(R136, R138, R141, R144, R146, R150), y es el cuello real. Paralelizable: todo `Sim/` con su prueba
de banco. Secuencial: diario → volcado → `CorrerSello` → validador → firma (una cadena de cinco
semanas). **No construir cuna, mutaciones ni firma antes del playtest de la semana 6**: son tres o
cuatro semanas que solo valen si el core es juego.

## 6. Qué se automatiza en lugar de iterar a mano

El suelo de ruido por columna (equivalentes corridos ±k celdas: debería ser el protocolo estándar
del banco desde ya), la anchura de la frontera, la validez de cada situación y mutante, el
determinismo entre máquinas, la escala temporal de cada fenómeno (el recibo por día), el orden de
campaña (firma, como heurística), el modo trivial de cada cláusula (un solver tonto: registro vacío,
inundar, tapar), y la bifurcación hacia atrás (clones por día). Lo que NO se automatiza y hay que
aceptar: resolver cada situación una vez, elegir su pedido (o hacerlo relativo) y saber si es
placer.

## 7. Órganos a conservar si se descarta

Diario bajo las cinco puertas con id de gesto y byte de autor; volcado/carga (primer fichero de
partida); `CorrerSello` con condición como dato; balanza con `entregable` + tragar desde arriba +
histograma de `carga`; intervención genérica del banco; clon en memoria; cielo por geometría; el
protocolo de equivalentes ±k como medida estándar de ruido; la cuota determinista de carbón si el
dado resulta dominante; el validador por veredicto; el anillo de hashes; la prueba cross-machine.

## 8. Rúbrica v2

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | una física nueva, cero cruces; convierte contadores en juicio |
| ejecutan, revelan y juzgan | 8 | juzgan sin balance; el pedido sigue siendo un número humano; revelan poco hasta M |
| iteración humana (10 = poca) | 6 | 15-25 días-persona reales: registro del autor y pedido por situación |
| verificabilidad automatizable | 9 | la mejor del panel: puerta en una semana, cada pieza con banco |
| la simulación es el juego | 8 | sellar y leer lo que la física escribió |
| onboarding garantizable | 6 | la firma es heurística no validada; «sin texto» es apuesta |
| observabilidad | 5 | bandas y diff; campos en F8, plantas de un píxel |
| tiempo como apuesta | 9 | SELLAR + clones; ir atrás gratis |
| multiplayer emergente | 5 | mesa y relevo por fichero; cross-machine sin probar; nada «divertido juntos» demostrado |
| profundidad por leyes estables | 5 | depende de la frontera; R136 (plana) y el dado del 25 % son evidencia en contra para el fuego; R133 a favor para el agua |
| cuerpo del jugador | 3 | cursor y herramientas heredadas |
| identidad comercial | 6 | frase y GIF; nicho Opus Magnum; SELLAR es verbo de juicio, no de acción |
| dificultad técnica (10 = fácil) | 8 | C# puro y acotado; lo delicado: gesto, huella, cross-machine, `LabParams` estático |

**Riesgo mayor:** que la separación entre soluciones distintas sea menor que la dispersión del dado
por celda (9-12 % en carbón) y que la boca no regule (R136): el histograma sería una mancha de ruido
alrededor de una máquina, y la situación insignia un examen con una respuesta. Se mide en una
semana con equivalentes ±k; si es el dado, se corrige en un día con la cuota; si es la física, la
dirección se juega en el agua.
