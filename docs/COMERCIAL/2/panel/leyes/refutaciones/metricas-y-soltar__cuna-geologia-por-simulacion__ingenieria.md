# Refutación de ingeniería · metricas-y-soltar / cuna-geologia-por-simulacion

**Veredicto: viable con ajuste.** Nacer una situación corriendo las leyes sobre el banco es barato, determinista y reutiliza la física validada sin tocarla. El código refuta tres piezas del validador y dos cifras: «soluciones distintas» por `HashMat`, la escalera por cortes de una celda, las capas por edad con `reposo`, la boca de cielo «por gramática» y el «cien semillas en una noche».

## Lo que el código confirma

- El banco ya admite un nacimiento: `LabBench.Correr` (LabBench.cs 265) recibe un `Montaje(CellGrid)` y crea el stepper después de montar (293). La cuna es un montaje que talla con `FromCell(0,x,y,(uint)seed ^ sal)` (R21) y corre un stepper privado N ticks: su libro es el de nacimiento y el de la partida nace limpio.
- La geología prometida existe en `LabCampos`: erosión (Laboratorio.cs 173-199), depósito, compactación (621-632), cocción a ≥ 150 raw (634-655; el hogar pinta 170, cuece solo lo que la difusión alcanza), carbón en sordina (SimStepper.cs 919-926), germinación (702-712).
- El manantial rodeado espera (1015): una cueva que no drena se ahoga y para. Lago, no bug.
- Determinismo intacto: la cuna lo consume (`afecta_determinismo` debería ser false). Arruga: un stepper nuevo sobre grilla nacida con otro reinicia `_tick` y `ProcessIfNeeded` (SimStepper.cs 352) salta celdas cuando `touchedTick` coincide por azar; limpiarlo al entregar.

## Lo que el código refuta

**1. `HashMat` no cuenta soluciones.** Con `Caudal` 24 (LabParams.cs 101) `mat[]` no descansa nunca; dos registros que cumplen y difieren en una celda divergen en toda el agua: «hashes distintos entre los que cumplen» = «cuántos cumplen».

**2. Un corte de una celda no es un solver.** K cortes solo distinguen trivial (el vacío cumple) de casi trivial (un corte cumple); el resto es «sin solución conocida». Dos peldaños. R150 lo muestra: el arco largo exige ensanchar la boca 18 columnas y ni así (humedad 50-99).

**3. `reposo` no es edad.** Byte que sube por visita y satura a 255 (494, 552, 714): a los ~2 040 ticks toda celda quieta lee 255. A N = 20 000 no hay capas; queda el time-lapse.

**4. La boca de cielo es un estático global.** `LabLuz` lee `LabParams.LuzCieloX0/X1` (1196): un rango contiguo, escrito por el constructor (SimLevelBuilder.Laboratorio.cs 164) y restaurado por `Correr` (277-338). Ni dos bocas ni dos situaciones en paralelo: todo `LabParams` es estático.

**5. Las cifras.** (1+K)·H = 234 000 ticks a 1,6-3,1 ms (un mundo recién tallado y lleno de agua corre como el «diluvio turbio», 3,06) = 6-12 min por semilla; sin bifurcar el estado se paga N en cada registro: 14-25 min. Cien semillas = 11-42 h en un hilo. El «35-100 s» de nacer sí es correcto.

**6. Salidas y leña.** `LabTragar` (1026) traga un vecino por visita: una salida de una celda drena ≤ 15 celdas/s, menos que el manantial; y lo que el hogar toca al nacer arde 11 minutos antes de que nadie entre. Casi toda semilla tonta es un lago con la leña gastada. No refuta: el libro lo cuenta y la gramática lo aprende.

## Ajuste mínimo

- `CellGrid.Clonar` + `SimStepper.Clonar` en memoria (13 `Array.Copy`, `_tick`, `_labPase`, libro, ring, `_ultimoTickPorLey`; `_labVisita` se limpia): bifurcar en N sin esperar al volcado del candidato 2. Prueba: alambique bifurcado en 4 500 + 4 500 → los siete hashes de r141 en ambos.
- Cielo por geometría: fuente = toda celda `Empty` en la fila H−2. El nivel de referencia da la misma `luz[]` (la boca es ese aire en x118-124), los hashes no se mueven, muere el estático y una boca cavada por el jugador ilumina.
- Soluciones distintas = recibos cuantizados (balanza del candidato 1, 8 tramos por métrica).
- Perturbaciones = 5-6 macroverbos paramétricos (canal L ≤ 24 de agua a salida o lecho, tapón 1-3, boca ±n, puente de arcilla), listas de `Intervencion` en el tick 0.
- Condición C = familia fija relativa al recibo («m ≥ f × nacimiento»), nunca por situación.
- Precheck de conectividad manantial→salida y aborto si `LabAguaEmitida` se estanca; lote paralelo con un `Universe` por hilo.

## Benchmark

«Cuna, semilla 1»: misma semilla dos veces → siete hashes iguales a N y N+H; clon = original. Calibración: alambique con «goteos ≥ 500» (vacío falla, caldera cumple); arco largo con «planta viva día 30» (vacío falla, boca x100-124 falla: reproduce R150). Lote de 100 semillas con la gramática más tonta: % lagos, % que drenan, terracota, carbón, plantas vivas, tres cajones del validador. La mata: < 5 % de semillas no triviales con algún macroverbo que cumpla.

## Valores corregidos

Apalancamiento 7: sustituye la biblioteca de autor y el recibo es lectura real, pero el «juez» es un filtro de trivialidad hasta que exista la gramática de perturbaciones. Tuning 6: dos gramáticas con gusto, medidas en lotes headless, no en playtest. Coste 4,5 semanas: las 3 declaradas más clon, cielo por geometría, macroverbos, condiciones relativas y lote paralelo; asume la balanza del 1 y no necesita el volcado del 2.
