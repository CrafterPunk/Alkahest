# CRÍTICA · «SELLADO» (híbrido) · lente INGENIERÍA Y EVIDENCIA

*(Director técnico, segunda pasada, 2026-09-12. Leído entero `hibrido.md`, `2/01_LEYES.md`, las seis
refutaciones de sello/balanza/cuna, `_huecos_y_combinaciones.md` §2-3 y el banco de R150 de Q16. Código:
`AlkahestSim.cs` (Update :358-400, puertas :571-857), `SimStepper.Step` (:257-330),
`SimStepper.Laboratorio.cs` (libro :49-120, `LabTragar` :1026, `EsFondo` :466), `LabBench.Correr`
(:265-343, la caldera :296-301), `LabParams.cs` (banderas :110/117/148, todo estático), `CellGrid.cs`
(arrays :71-195), `Cincel.cs` :469-551, `Flask.cs` :614/687, `ApprenticeController.cs`.)*

## 0. Veredicto

**FINALISTA, condicionado a una prueba de UNA semana que se corre ANTES de escribir el sello.** Es la
dirección más acotada y más verificable en banco de las que he leído: no pide una sola ley física nueva,
todo menos el gesto es headless, y el sustrato la sostiene casi entera con código que ya existe. Lo que
no sostiene, y por eso la condición, es la premisa de que el recibo tenga **gradiente** (y no solo
pasa/no pasa) y de que el contador de días cuente algo después del día ocho. Las dos cosas se miden sin
construir nada de la dirección.

## 1. Qué pide del sustrato, contrastado con el código

| pieza | lo que el código dice | corrección |
|---|---|---|
| Diario bajo «seis puertas» | Existen seis: `Paint`, `PaintCell`, `PaintStable`, `PaintRect`, `InyectarTemperatura`, `PaintLab`; cuatro ya llaman `ReenviarSiEspejo` y `SimSync` serializa eso en 8 bytes. Escriben la grilla: cincel (:469 `Paint`, :473 `PaintLab`, :551 `PaintStable`), frasco (:614 `Paint`, :687 `PaintCell`) y panel. **El aprendiz no escribe la grilla** (`ApprenticeController` solo lee `InBounds`): la contradicción (a) del crítico de completitud es vacua hoy y la dirección la mantiene vacua al dejar el cuerpo como sensor. | El documento dice **«cola»**; la refutación de ingeniería demostró que una cola cambia las herramientas (`TallarTick` lee en el mismo frame lo que acaba de escribir). Es **diario**: escribir en el frame y anotar; bit-idéntico porque entre dos `Step()` nadie toca la grilla salvo `patina`. Y el aspirado del frasco debe anotar `(mat, temp)` de lo que quitó, o el búfer del frasco no se reconstruye al bifurcar en un toque anterior al sello. |
| Volcado y carga | 11 arrays de byte + `touchedTick` (uint) + arrays de chunk ≈ 3,3 MB crudos; el estado fuera de la grilla es conocido y corto (`_tick`, ~30 `long` del libro, `_labManantialCeldas`, preset, `LuzCieloX0/X1`, búfer del frasco). | «Un volcado por día» × 100 días = 330 MB crudos. Para el time-lapse basta `mat` (221 KB/día); volcado completo cada 5 días; bifurcar en un día intermedio = cargar el completo anterior y correr ≤ 9 000 ticks headless (≤ 20 s). |
| `CorrerSello` (rejugar) | `Correr` YA tiene una intervención tick-estampada cableada: la caldera (`esAlambique && t % 8 == 0`, `SetCell` + `humedad = 255` + `WakeChunk((uint)t)`). | Generalizarla a lista es lo que la refutación de la balanza pidió; la trampa del tick (`TickActual = t−1` contra `(uint)t` decide `TickChunkIdle`) está identificada y cuesta una línea. Migrar la caldera al diario puede mover el hash del alambique UNA vez. |
| Condición como dato | El libro es acumulativo (`long`); «métrica por día» = diferencia entre muestras diarias. | Trivial. La condición se evalúa sobre el volcado de nacimiento y sobre cada día. |
| Balanza mínima | `LabTragar` solo traga `Liquid`; `EsFondo` incluye al sumidero (:466): **el sedimento ya ciega la salida hoy**. «La que se ciega» está en el código. | «Clara ≤ 16» no tiene base (la decantación TRANSFIERE carga hacia abajo: el fondo se concentra). Histograma de `carga` al tragar, sin umbral; la raya se dibuja en el visor. `PaintLab` debe rechazar sumidero/manantial/hogar/núcleo frío como pincel y como destino (hoy no lo hace). |
| Cuna-lite: «cada bandera de ley apagada (unas veinte)» | **Existen tres**: `TermicaPropia`, `PresionActiva`, `CuerposActivos`. Unas ocho leyes se apagan poniendo a cero parámetros que ya existen (`GerminaPorMil`, `EvapBase`, `Caudal`, `ErosionPct`, `Decantacion`, `ColmatacionPct`, `CompactPct`, `LuzCadaTicks`); fuego, fase y gas viven en `SimStepper.cs` sin gate. | 2-3 días de banderas. Y una ambigüedad de diseño que hay que cerrar: apagar una ley ¿sobre la situación ya envejecida o re-envejeciendo? Re-envejecer da otra geología; no hacerlo deja los productos de la ley en el estado nacido. Decisión: sobre el volcado de nacimiento; la ley se apaga solo durante la partida. |
| Lote de validación | `LabParams` y `LuzCieloX0/X1` son **estáticos**; `Universe.Create` pide el runtime de Unity. 15-20 situaciones × ~40 registros × 54 000 ticks ≈ 30-40 M ticks ≈ 15-20 h en serie. | No bloquea el prototipo (una noche por lote) pero sí iterar la campaña: N procesos batchmode, o instanciar `LabParams` (0,5 sem). Adoptar «cielo por geometría» (toda `Empty` en la fila H−2) de la refutación de la cuna: mata el estático y deja que una boca cavada ilumine, con los hashes intactos. |
| ×10 en vivo | 1,6-1,9 ms/tick × 10 = 16-19 ms contra `LabPresupuestoMs = 20`: ×10 real se sostiene justo; peor caso 3,1 → ×6. `LabCampos` cuesta siempre la grilla entera /8 aunque la cámara sea 96×64. | 30 días = 54 000 ticks = 3-5 min en vivo, ~90 s headless con volcados diarios. Ofrecer las dos: la vigilia a ×10 y «saltar al final» headless. |
| Determinismo entre máquinas | Física entera, sin `float` en el partial, RNG sin estado; `Universe.Create` usa `Mathf`; las builds actuales son Mono (`_Data/Managed`). | Hash de la tabla de materiales en el fichero; un día de banco. Riesgo bajo. |
| Remedir Q16 | **R150 ya midió el huerto con boca ancha** (x100-124): 9 nacidas contra 2, luz de cara 139-210, y **las nueve mueren por humedad** (50-99 contra mínimo 60). | El día de `luz[i+W]` responde si la boca estrecha ya tenía luz; **no reabre el huerto**. «Planta viva el día 30» exige V (raíz que busca) o geometría de riego. §10 («no depende del huerto») es correcto; la esperanza de §2 hora 5 no. |

## 2. Evidencia del laboratorio: a favor y en contra

**A favor.** El alambique con caldera da ~900 goteos/5 min y sin caldera 0: el recibo discrimina el
vacío del autor sin balance. Entre dos registros razonables de la misma familia (R148, serpentín sobre
la boca, contra R150, serpentín fuera y boca ancha) hay gradiente real: 2 contra 9 nacidas, 891 contra
1 624 goteos, columnas anegadas 16→1. La carbonera con salida tiene predicción de código (T=0: fibra
400, carbón 0; T=6 000: carbón ≥ 0,8·`LabCarbonizado`) y es monótona en T. El sedimento ciega el
sumidero sin que nadie lo escribiera. Sesenta y tres hashes en una máquina; 21 rondas en 3 días: el
ritmo del trabajo acotado está demostrado.

**En contra.** (1) **Atractores.** La refutación de la cuna lo dejó escrito: sin autor las leyes dan
anegado, quemado o inerte, y «un corte de una celda rompe, rara vez construye». El espacio entre el
registro vacío y el del autor puede ser **bimodal**: entonces bifurcar no optimiza nada, el recibo es
un check y el juego es un examen por construcción, con o sin franja bonita. (2) **Horizonte.** Con
`DiaTicks = 1 800`: tolva 466 s = 14 000 ticks = 7,8 días; meseta de la carbonera ~6 000 ticks = 3,3
días; `DepositoReposo` 24 visitas = 0,1 día; `CompactReposo` ≈ 1 día; el alambique es estacionario
para siempre (manantial, hogar y núcleo frío son pins infinitos). «30-100 días» pueden ser 25-95 días
de nada. El contador exige un proceso mortal por situación: en fuego lo hay (el combustible); en agua
es la colmatación/cegado, cuya escala de tiempo **no está medida**; en «La piedra fría» (núcleo frío
sobre hogar) no hay ninguno → DÍA ∞, o **D (el hogar come, 0,5 sem) entra al núcleo**, no a las
expansiones. (3) «Planta viva» está muerta hasta V. (4) La «cola» y las «veinte banderas» no existen
tal como se describen.

## 3. Dependencias secuenciales y tiempos corregidos

**Camino crítico (Opus):** diario (0,2) → volcado/carga + round-trip en banco (0,8) → `CorrerSello`
con lista de intervenciones (0,4) → condición como dato + muestreo diario del libro (0,3) → cuna-lite:
envejecer (0,3), validador vacío-falla/autor-cumple (0,5), fragilidad (0,2), banderas (0,5), mutaciones
(0,5) → tres situaciones validadas (0,5) → **prototipo feo ≈ 4,5-5 semanas de Opus**.

**Paralelo (segundo carril):** balanza entregable (1-1,5), `LabBandas` + tres visores (1),
cuerpo-sensor (1), anillo de hashes (0,4), gesto SELLAR + franja + time-lapse + diff (1,5-2, en cuanto
exista el formato del volcado), Mono/IL2CPP (0,2), instanciar `LabParams` (0,5, opcional), cielo por
geometría (0,2), D si la campaña lleva hogares (0,5).

**Total 11-13 semanas de Opus** (el documento dice 10,5-12; la diferencia son banderas, D, cielo por
geometría y el diario del frasco). Dos carriles → 6-7 semanas calendario al núcleo completo.
**Prototipo feo: 6 semanas** (4,5-5 de camino crítico + las rondas de integración en el PC de Cesar
con auto-refresh apagado). **Evidencia para matarla: 1 semana**, no 2 (§5).

## 4. Iteración humana oculta

El documento declara tuning 8. Lo que el banco NO sustituye:

1. **La campaña es autoría.** 15-20 situaciones × (montaje de 10-15 líneas + condición + **registro del
   autor** + una noche de validación + leer el resultado). El validador certifica «no trivial y
   resoluble», no «enseña una ley». Cada situación es media jornada de Fable/Cesar leyendo el banco
   (el ritmo de R148-R150): **2-3 semanas de humano**, solapadas con las semanas 3-7 de Opus.
2. **Qué es un toque.** El frasco aspira 900 celdas/s y el cincel talla por presupuesto de frame: un
   arrastre son cientos de entradas del diario. «Toques» como columna del recibo no significa nada
   hasta que se defina toque = gesto (pulsar→soltar) y se fijen alcance y capacidad (el crítico de
   completitud lo pidió). Decisión de diseño + una sesión de sensación.
3. **`DiaTicks` y el horizonte.** No es una sesión de gusto: sale de la prueba de §5 (el día en que la
   mediana de las situaciones deja de cambiar). Se calcula, no se siente.
4. **Legibilidad del recibo** (franja, vector por dominancia): 1-2 rondas con capturas.
5. **Examen o juego**: 3 sesiones (semanas 7-10), binarias. **Bandas y Piel**: 2 días.

Total ≈ 4-5 semanas calendario de humano, la mitad en paralelo con Opus. **Tuning corregido: 7.**

## 5. La prueba más barata capaz de matarla (una semana, sin sello ni balanza)

«**Recibo con gradiente y horizonte**». Generalizar `Correr` a una lista de intervenciones
`(tick, x0, y0, w, h, mat, temp, humedad, carga)` (la caldera se vuelve 1 125 entradas: es la prueba
(a') de la refutación del sello, que hace falta de todos modos) y muestrear cada 1 800 ticks el libro
más seis métricas leídas de la grilla (celdas de carbón en la región de la salida, carga media de la
grava del labio, histograma de `carga` del agua tragada —tres líneas en `LabTragar`, no la balanza—,
plantas vivas, columnas anegadas, humedad media de la cara). Cuatro montajes (alambique de r141,
carbonera 20×20 con canal a un pozo de una celda, diluvio con labio, arco largo) × 20 registros (vacío,
autor, 12 cortes de una celda, 6 alternativas razonables escritas por Opus: serpentín ±5 columnas, boca
1/2/3, T de apertura 0/3 000/6 000/9 000, labio de 1/2/3 celdas) × 54 000 ticks ≈ 4,3 M ticks ≈ 2-3 h
en un hilo. **3-4 días de Opus + una noche de banco + un día de lectura.**

La matan tres resultados, en tres de los cuatro montajes:

- **K1 (trivialidad, la del documento):** el recibo del autor al día 30 no domina al vacío por ≥ 2×
  en ninguna columna, o el vacío ya cumple la condición → las leyes no juzgan; muere J entera.
- **K2 (gradiente):** los 18 registros no triviales caen, en todas las columnas, a ±10 % del vector
  del vacío o del vector del autor (bimodal) → el recibo es un check, bifurcar no tiene nada que
  optimizar; SELLADO muere como juego con gradiente y el sello sobrevive solo como validador de
  contenido para otra dirección.
- **K3 (horizonte):** ninguna columna del vector diario del autor cambia > 5 % entre el día 8 y el
  día 30 → el contador de días es teatro; «DÍA N» exige un proceso mortal que el sustrato no tiene
  (D deja de ser expansión) y el time-lapse de 3-10 min se queda en uno.

Segunda prueba, en la semana 3, la del sello mismo (refutación (d)): dos minutos en el editor a ×10
con cincel, frasco y panel, sellar, `CorrerSello` → `HashMat` igual al de la grilla viva. Si falla, el
diario no es transparente: reparable, no letal.

## 6. Órganos a conservar si se descarta

Diario bajo las seis puertas + volcado/carga (el primer fichero de partida del proyecto; lo necesita
cualquier dirección); `Correr` con lista de intervenciones y muestreo diario del libro (el banco lo
necesita hoy); anillo de hashes cada 256 ticks; `LabBandas` como única fuente de umbrales; balanza
entregable desde arriba con histograma de carga; envejecer situaciones por simulación y el validador
vacío-falla/autor-cumple; la vista de diferencia; el cuerpo-sensor (Piel); cielo por geometría; el hash
de versión de física en cualquier fichero.

## 7. Rúbrica v2 (1-10), desde esta lente

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | cero cruces físicos; el apalancamiento es del instrumento (toda ley entra al recibo), no de las leyes |
| ejecutan, revelan y juzgan | 8 | ejecutan ya; revelan por materia y diff; juzgan por recibo, pero la condición es de autor y el gradiente está sin medir |
| iteración humana (10 = poca) | 7 | campaña de 15-20 situaciones con registro del autor; toque y alcance; tres sesiones |
| verificabilidad automatizable | 9 | todo menos «examen o juego» y la legibilidad de bandas es banco |
| la simulación es el juego | 8 | sin economía, árbol ni hub; el único añadido no físico es la condición validada |
| onboarding garantizable | 7 | el validador garantiza no trivial y resoluble, no «enseña»; las banderas para ordenar no existen aún |
| observabilidad | 7 | bandas, Piel, recibo, diff, time-lapse; nada de ello está pintado hoy |
| tiempo como apuesta | 8 | SOLTAR con contador y bifurcado con precio; real solo donde hay proceso mortal (pins infinitos) |
| multiplayer emergente | 6 | relevo y liga por fichero son casi gratis por determinismo; «comparar» como diversión está sin probar |
| profundidad por leyes estables | 6 | J no combina nada; crece solo si L/A/V/F llegan |
| cuerpo del jugador | 4 | sensor y portador; sin consecuencias |
| identidad comercial | 6 | fuera de mi lente; DÍA N es un remate nombrable y el time-lapse un clip legible, sin co-op |
| dificultad técnica (10 = fácil) | 8 | acotada; riesgos: lista de estado, memoria de volcados, `LabParams` estático, banderas |

**Posición.** La pregunta final de Cesar («¿pueden las leyes ejecutar, revelar y juzgar de modo que la
profundidad crezca más rápido que el coste?») tiene aquí la mejor respuesta técnica del panel: la
infraestructura cuesta 11-13 semanas de trabajo casi todo verificable en banco y no exige balance. Pero
«juzgar» solo vale si el recibo ordena jugadas intermedias, y eso no lo demuestra ni un hash ni una
refutación: lo demuestra la prueba de §5, en una semana, antes de escribir una línea del sello.
