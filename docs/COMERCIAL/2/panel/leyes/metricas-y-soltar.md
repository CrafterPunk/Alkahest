# LENTE · LAS LEYES COMO JUEZ Y EL TIEMPO COMO APUESTA

*(Panel de leyes, segunda pasada comercial. Leído antes de proponer: `SimStepper.Laboratorio.cs`
(libro mayor, `LabCampos`, `LabSumidero`/`LabTragar`, `LabPresion`, `LabLuz`), `SimStepper.Step()`,
`LabBench.cs` (`Correr`, `MedirLecho`, `ArcoMuestra`), `CellGrid.cs`, `LabParams.cs`,
`SimLevelBuilder.Laboratorio.cs`, `LabPresets.EscribirLibro`, `AlkahestSim.PaintLab`, `SimSync`.)*

## 0. La tesis en una frase

El motor ya produce el veredicto; lo que falta es que el veredicto sea **un objeto del motor**. El
libro mayor del stepper lleva unos cuarenta contadores (`LabAguaSumidaU`, `LabGoteos`,
`LabCarbonizado`, `LabRawFuego`, `LabPlantasNacidas/Muertas`, `LabBalanceU`...), el banco produce
siete hashes FNV-1a por escenario, `LabBench.Correr` ya contiene una **intervención tick-estampada**
(la caldera repuesta cada 8 ticks, aplicada antes de `st.Step()`) y `ArcoMuestra` ya cuenta el
mundo en tramos de 1 800 ticks. Puntuación, determinismo, repetición y «día» existen como piezas de
laboratorio; los cuatro candidatos las convierten en leyes del juego. Ninguno añade un número de
balance: añaden definiciones (qué es «agua clara», cuánto dura un «día») y medidas.

---

## 1. LA BALANZA · el sumidero pesa lo que traga

**Resumen.** Hoy `LabTragar(j)` solo traga líquidos y solo cuenta celdas y unidades de agua; tira la
turbidez y la temperatura. La balanza hace del sumidero el punto de entrega universal: traga también
lo que cae sobre él (arquetipo `Powder`: carbón, ceniza, semilla, sedimento, grava, fibra) y lleva
un libro **por material, por salida y por calidad**.

**Regla precisa.** En `LabTragar`: tragar si `archetype == Liquid || archetype == Powder`.
Contadores nuevos en `SimStepper.Laboratorio.cs`: `long[] LabSumidoPorMat` (índice `MaterialId`),
`LabSumidoAguaClaraU` (unidades con `carga[j] <= AguaClaraCargaMax`, `LabParam` nuevo con default
16: el manantial emite a 40 y decanta a 6 por visita, así que «clara» significa «pasó por reposo»),
`LabSumidoFinosU` (`carga·humedad/255`) y `LabSumidoCalorRaw` (`temp[j] - AmbientRaw`, con signo).
**Salidas con nombre**: el sumidero es estático (nunca pasa por `SwapCells`), así que su `aux` está
libre; el constructor de nivel escribe `g.aux[idx] = idSalida` y los contadores se indexan
`[salida, material]`. Todo va a `LabPresets.EscribirLibro` y al `Informe()` del banco. Cero RNG,
cero escrituras nuevas en arrays: los siete hashes no se mueven.

**Cruces con leyes existentes.**
- *Turbidez y decantación*: clara/turbia deja de ser un color y pasa a ser la cifra; una poza antes
  del sumidero es decisión medible y la grava colmatada se lee como caída de unidades claras por día.
- *Fuego y carbón*: «carbón producido» deja de necesitar recogida manual: una carbonera con el suelo
  inclinado hacia una salida entrega su 25 % por gravedad. Cierra el eslabón «recogida» sin H6.
- *Calor*: el alambique y el hervidero pasan a tener producto medible (agua caliente entregada); el
  frío, un descuento.
- *Presión y plantas*: un sumidero en el fondo de un tubo en U vacía el cuerpo entero, y la balanza
  dice cuán turbio; semillas y fibra tragadas son censo.

**Decisiones nuevas.** A qué salida llevar cada cosa; decantar o entregar turbio y rápido; quemar
el carbón (energía) o dejarlo caer (carbón entregado); enfriar o no el agua del alambique antes de
la salida; sacrificar caudal por claridad con un banco de grava. Cada máquina es una elección sobre
*qué* entrega.

**Observabilidad.** Sin panel: el agua ya lleva su tinte de turbidez y la salida come visiblemente.
Con visor: un «recibo» por salida (barras clara/turbia/finos/calor/carbón por día), impreso como
veredicto al cerrar un sello.

**Coste.** 1 semana (contadores, `aux` de salida en `SimLevelBuilder`, libro y banco). Determinismo:
no lo toca. Tick: despreciable (una comparación de arquetipo más por vecino de sumidero, cada 8
ticks). **Banco**: «laboratorio base» (3 000 ticks) debe dar `Σ LabSumidoPorMat[Water]·u ==
LabAguaSumidaU` y los siete hashes iguales; escenario nuevo «carbonera con salida» (la de 20×20 con
el suelo de sumidero): `LabSumidoPorMat[Carbon] > 0` tras la quema. **Tuning humano**: 9 (solo la
definición de «clara»). **Apalancamiento**: 9. **Riesgo mayor**: que la entrega sea demasiado fácil
(todo cae al sumidero). Mitigación de diseño, no de balance: el sumidero es un don del nivel, como
el manantial, y nunca lo coloca el jugador; la geometría entre la máquina y la salida es el juego.

---

## 2. EL SELLO · registro de intervenciones, volcado de estado y veredicto rejugable

**Resumen.** SOLTAR como objeto del motor: una partida sellada es (semilla, montaje, registro de
intervenciones tick-estampadas, horizonte, condición); un resultado se **rejuega** desde el fichero,
se **compara** entre jugadores y se **valida** en banco sin personas.

**Regla precisa.** Tres piezas en `Sim/` (C# puro, como `LabBench`):
1. `Intervencion` (struct de 12 bytes: `tick, x, y, mat, temp, humedad, carga`) y una cola en
   `SimStepper`. `AlkahestSim.PaintLab` (línea 831, el único camino de las ediciones del jugador en
   el laboratorio) deja de escribir en el frame y **encola**; `Step()` (SimStepper.cs:257) aplica la
   cola al principio, con el patrón de la caldera de `LabBench.Correr`. Latencia: un tick. Una hora
   con 5 000 toques pesa 60 KB.
2. `CellGrid.Volcar/Cargar`: RLE por array (el de `SimSync` se reutiliza) de `mat, temp, aux, morph,
   humedad, carga, reposo, luz, ambient, chunkSleepTimer, chunkTouchedTick` más `_tick`, `_labPase`,
   el libro mayor y el preset. `patina` no (renderer, por contrato); `touchedTick` no hace falta
   porque compara contra `_tick`. Un mundo cabe en 100-300 KB.
3. `LabBench.CorrerSello(sello, registro)`: monta, aplica el registro tick a tick, corre el horizonte
   y devuelve libro + siete hashes + **veredicto**. Una `Condicion` es una lista de (métrica del
   libro, comparador, umbral, por día); «día» = `DiaTicks` (LabParam, default 1 800, el tramo de
   `ArcoMuestra`). «DÍA N SIN MANOS» = último día en que todas las cláusulas se cumplieron tras el
   último toque. La condición es dato, no código; `MedirLecho` queda como calibración.

**Cruces con leyes existentes.** Ninguna regla física cambia; cambia *cuándo* entra la mano (al
tick, no al frame) y cualquier regla pasa a poder juzgar. Cambios concretos: `PaintLab` → cola; la
caldera del banco pasa a ser un registro; `LabPresets.GuardarSnapshot` gana el volcado (hoy guarda
preset + png + libro, pero no la grilla: un snapshot no se reabre); `LabDiario` gana marcas con tick
exacto gratis.

**Decisiones nuevas.** *Cuándo* soltar (antes = más días posibles, más riesgo); *qué* dejar
corriendo (la tolva de 466 s, el alambique que ahoga); cuántos toques gastar (el largo del registro
es «celdas cavadas» sin código nuevo); **rejugar el registro de otro** en tu copia, bifurcarlo en el
tick 4 000 y ver dónde diverge; apostar contra el propio registro («¿aguanta 300 días si no toco?»).
Multiplayer asíncrono por fichero: se comparte el registro (KB), no el mundo; el mismo `HashMat`
final es la misma solución (detección de copias gratis) y comparar dos estados es un `diff` de
`mat[]`.

**Observabilidad.** Sin panel: el gesto de SOLTAR, el contador de días y el time-lapse (un volcado
por día). Con visor: la **vista de diferencia** entre dos estados (tu solución contra la de otro; el
tick 0 contra el horizonte): el hash hecho visible.

**Coste.** 3 semanas (1 cola + registro, 1 volcado/carga, 1 `CorrerSello` + condición + informe).
Determinismo: lo *usa*, y el banco lo amplía con el test más barato del proyecto: «volcar en el tick
4 500 del alambique, cargar, correr 4 500 más → los siete hashes de `alambique de r141`». Todo
estado que Opus olvide volcar aparece como hash distinto. Tick: O(toques por tick), despreciable.
**Banco**: (a) el alambique corrido por la vía del registro (la caldera como 1 125 entradas)
reproduce sus hashes → la cola es transparente; (b) el round-trip de volcado; (c) `CorrerSello` con
registro vacío sobre «arco largo» reproduce la tabla de R148. **Tuning humano**: 9. **Apalancamiento**:
9. **Riesgo mayor**: determinismo entre máquinas, probado solo en un editor (`Universe.Create` usa
`Mathf`): un registro rejugado en otro PC puede divergir. Mitigación: el fichero lleva veredicto *y*
hashes, la divergencia se detecta en vez de aceptarse en silencio, y el banco se vuelve la prueba
cross-machine que la ruta B necesitaba de todos modos.

---

## 3. LA CUNA · geología por simulación y validador de discriminación

**Resumen.** La primera pasada pedía 12-15 cámaras de autor; la función objetivo lo penaliza. La
cuna genera situaciones **corriendo las leyes**: cueva tosca sorteada, dones perpetuos por gramática,
y el mundo corre solo N ticks a velocidad de banco antes de que nadie entre. Después el banco decide
si la situación vale, con el sello del candidato 2.

**Regla precisa.** `SimLevelBuilder.GenerarPorSimulacion(seed)`: (1) talla con `XorShift.FromCell`
(sales ≥ 700; las del stepper llegan a 643) una cueva tosca con vetas de arcilla, arena, grava,
fibra y semillas; (2) coloca los dones por gramática (manantial alto, una o dos salidas bajas, hogar,
frío, boca); (3) corre 20 000-60 000 ticks headless (35-100 s de banco): el agua erosiona y deposita
lechos, la grava se colmata, el sedimento compacta, lo que el hogar tocó cuece a terracota, las
pilas junto al hogar dejan carbón y brota donde luz y agua coincidieron; (4) el estado final es la
situación y su **libro de nacimiento** (cuánto tragó cada salida, cuán turbio, cuántas plantas
vivieron) es su etiqueta. **Validador** (`LabBench.ValidarSituacion`): con condición C y horizonte
H corre (a) el registro vacío → debe FALLAR (no trivial); (b) K = 8-16 registros de perturbación
(cortes de una celda junto a agua, fuego o luz, sorteados con la semilla) → fracción que cumple y
número de `HashMat` finales distintos entre los que cumplen = **soluciones distintas**; (c) el
registro del autor, si existe → debe cumplir. Discrimina si el veredicto varía entre (a), (b) y (c).

**Cruces con leyes existentes.** Erosión/depósito (lechos reales, no pintados), colmatación,
compactación y cocción (terracota donde hubo fuego: el termómetro de máxima de la primera pasada,
gratis), carbonización (carboneras enterradas como ruinas amables), presión (lagos nivelados),
germinación (huertos que murieron y dejaron abono). Los negativos medidos se vuelven activos: «el
huerto que nunca vivió» de R148/R150 es una situación que discrimina (el registro vacío falla; la
boca y el reparto del riego cambian el veredicto).

**Decisiones nuevas.** Elegir la situación por su etiqueta («la que entrega 80 % turbio», «la que
arde a sordina desde el tick 0»); la escalera de dificultad la da el validador; leer la geología
como historia (la terracota dice dónde hubo fuego, el lecho por dónde corrió el agua).

**Observabilidad.** Sin panel: el nacimiento es un time-lapse que sirve de intro. Con visor: capas
por edad (`reposo`) y el recibo de nacimiento como carta.

**Coste.** 3 semanas (2 gramática + generación, 1 validador). Determinismo: lo usa, con sales
propias. Tick: fuera de línea; validar una situación = (1 + K) × H ticks ≈ 4-8 min de banco con
H = 18 000 y K = 12: cien semillas en una noche. **Banco**: misma semilla dos veces → mismos siete
hashes; el validador se calibra primero sobre el alambique (registro vacío contra caldera:
veredictos distintos) y el arco largo. **Tuning humano**: 6 (la gramática necesita gusto; la
*aceptación* es automática). **Apalancamiento**: 8. **Riesgo mayor**: la tasa de situaciones buenas
por semilla. Si es 5 %, un lote nocturno da cinco; si es 0 %, la gramática necesita a una persona.
Es la prueba que la mata y se mide en una semana con la gramática más tonta.

---

## 4. EL TESTIGO · la condición vive en la grilla

**Resumen.** Un material estático que **recuerda** lo que vio a su alrededor: cuatro bytes de
extremos y duraciones. La condición de una situación se escribe colocando testigos, no código, y
el jugador puede colocar los suyos como instrumentos.

**Regla precisa.** `MaterialId.Testigo` (estático, densidad máxima, tratado como `Stone` por luz y
térmica). Caso nuevo en el `switch` de `LabCampos`: por visita lee sus cuatro vecinos y escribe
`temp[i] = max(temp[i], max temp vecinos)` (termómetro de máxima), `humedad[i] = max(...)`
(higrómetro de máxima), `carga[i]++` saturado si tiene agua encima (visitas anegado) y `reposo[i]++`
saturado si `luz[vecino] >= PlantaLuzMin` (visitas con luz). No es poroso, condensable ni sustrato:
ninguna regla le suma ni le resta, conservación intacta. Una `Condicion` del candidato 2 se refiere
a testigos por posición o por id (`aux`), igual que a salidas.

**Cruces con leyes existentes.** Luz (¿el claro alcanzó el mínimo?, la pregunta de R148), humedad
(el 50-99 de la cara de R150), calor (¿el horno sostuvo 200 raw?), agua (anegado). No cambia
ninguna regla: solo lee.

**Decisiones nuevas.** Dónde poner un testigo para *saber* (uno en el lecho dice si la humedad de
la raíz aguantó); construir contra un testigo visible del autor («este punto debe seguir mojado e
iluminado el día 30»); leer un testigo ajeno tras rejugar su registro.

**Observabilidad.** Sin panel: cambia de color por lo que registró (tornasol: pálido seco, oscuro
mojado, rojizo si vio 200 raw). Con visor: rayos X que leen sus cuatro bytes.

**Coste.** 1 semana. Determinismo: sin RNG. Tick: cuatro lecturas y cuatro `max` por testigo cada 8
ticks. **Banco**: un testigo en el lecho oeste del «arco largo» debe coincidir con «luz media» y
«humedad media de la cara» de `ArcoMuestra` (autoconsistencia); los escenarios sin testigo, hashes
intactos. **Tuning humano**: 9. **Apalancamiento**: 7. **Riesgo mayor**: que lea como «material
mágico» y rompa «la simulación es el juego». Mitigación: la terracota ya es un termómetro de máxima
y la grava colmatada un registro de turbidez; el testigo generaliza un instrumento que las leyes ya
producen, y los del autor son indestructibles como el hogar para que la condición no se cincele.

---

## 5. Cómo juzgan las leyes sin balance humano (el sistema completo)

Preparar (la cuna nace una situación con libro y testigos) → comprometerse (cada toque es una
entrada tick-estampada) → SOLTAR (la cola se cierra, el mundo corre a ×10, el contador cuenta días)
→ resultado (la balanza y los testigos escriben el recibo; el sello lo hace rejugable). Comparar es
rejugar el registro ajeno y mirar el `diff`; validar contenido es correr (1 + K) registros por
situación. Los únicos números humanos son «clara» y «día»; cada ley nueva de las otras lentes
(viento, óptica, hielo) entra en el recibo sin tocar nada de esto: si mueve un contador, ya juzga.
Orden sugerido: 1 y 4 en paralelo (dos semanas), 2 (tres), 3 (tres): ocho semanas de Opus en banco
headless, con una prueba que mata cada pieza en su primera semana.

## 6. Lo que descarté y por qué

- **Una puntuación escalar** (fórmula que pese agua clara, carbón y días). Exige balance por
  definición; el recibo es un vector y se compara por métrica o por dominancia.
- **Tablas online y lockstep.** Determinismo cross-machine no probado; el asíncrono por fichero con
  rejugado local da lo mismo y detecta la divergencia en vez de sufrirla.
- **Etiquetas de procedencia** en agua y humo (la primera pasada las presupuestaba). Un byte por
  celda y una vía más en `SwapCells`; rejugar sin las entradas de B y mirar el `diff` es la
  atribución exacta y gratis. Lo mismo vale para una máscara «tocado por el jugador»: el registro
  es más barato y rejugable.
- **Condiciones en código por situación** (como `MedirLecho`). Es autoría; queda como calibración.
- **Un agente que juegue para probar resolubilidad.** Para discriminar bastan perturbaciones
  sorteadas; una situación sin solución conocida se etiqueta así y se publica igual.
- **Días como clima o estaciones.** El día es un contador; nada cambia por ser día 300. La tensión
  ya la ponen las leyes: la tolva se acaba a los 466 s por su cuenta.
- **El time-lapse.** Presentación que consume el volcado del sello; no es sustrato.
