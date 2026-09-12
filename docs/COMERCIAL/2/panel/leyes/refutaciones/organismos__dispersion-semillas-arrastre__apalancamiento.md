# Refutación · organismos / dispersion-semillas-arrastre · lente apalancamiento

**Veredicto: viable con ajuste.** La idea buena es una y no es la que el candidato vende: usar el circuito manantial → sumidero como transporte de materia. Lo demás descansa en cuatro afirmaciones que el código desmiente y en una fuente (la planta que suelta semillas) que el laboratorio aún no mantiene viva.

## Lo que el código dice y el candidato no

1. **«Lo que flota» no se hunde, pero tampoco sube.** `ProcessPowder` 1088 y 1110 solo deciden si un polvo se HUNDE; `ProcessLiquid` 1194 solo estratifica líquido contra líquido. Ninguna línea sube un polvo ligero a través del agua. Con el arrastre propuesto la semilla viaja mientras el agua justo debajo fluye lateralmente; en el primer escalón esa agua cae en diagonal, la semilla cae al hueco, el agua vecina la cubre por `TryFlow` (1341, solo a `Empty`) y queda enterrada para siempre, o se queda en el suelo del arroyo como piedra que el agua rodea. La «cinta transportadora» no está demostrada; el criterio del banco (≥ 50 % sale del tramo) es honesto porque probablemente falle.

2. **El sumidero no come polvo.** `LabTragar` 1029 devuelve si el arquetipo no es `Liquid`. «Dejar que el sumidero se lleve el sobrante» no existe; lo que existe sin querer es que la primera semilla tape la cara superior de un sumidero empotrado y el circuito inunde. Accidente que nadie eligió, no juicio.

3. **La fibra que flota no se moja.** `LabAgua` infiltra a `down`, `i-1`, `i+1` (443-447), nunca arriba, y `LabCapilar` solo lee porosos. La fibra sobre el agua sigue seca y prende: el cruce con el fuego está al revés.

4. **La costura rompe el contrato de `Move`.** `Move` 738-752 escribe `_cellFinalIdx`, que `LabErosion` y `MaybeReact` leen tras el `switch` de `ProcessIfNeeded` (371-375). Llamar a `Move` para la semilla en el turno del agua desvía el chequeo de reacciones del agua a la celda de la semilla. Hace falta `SwapCells` + `touchedTick` + `WakeChunk` a mano.

5. **La fuente está detrás de un problema abierto.** La semilla nace de una punta con savia ≥ 200. Con `PlantaTranspira` 2 por cara al aire (tres caras en la punta) contra `PlantaBebe` 6 en la raíz, una columna de tres celdas está en déficit salvo con aire saturado: solo hay semillas en cámaras húmedas, y nadie lo midió. El huerto de referencia nunca vivió (R148, R150): esa mitad del candidato produce cero hasta que eso se resuelva. Y `Estado()` no ve vecinos ni `luz` (firma `(m, humedad, carga)`): «la semilla que dice por qué» exige una sobrecarga con índice.

## Decisiones nuevas de verdad

De las cuatro declaradas, «segar sobre el agua» exige el verbo de siega del candidato 1 y «sumidero o rejilla» es falsa. Quedan dos: sembrar aguas arriba y esperar, y usar la poza quieta (`reposo`) como almacén. Cruces reales: agua × polvo ligero, quietud × transporte, sumidero × polvo si se decide. Tres cruces, dos decisiones, una condicionada a que las plantas vivan. Apalancamiento 8 está inflado.

## La versión mínima con más apalancamiento

Todo en el partial, cero costuras en `SimStepper.cs` (HANDOFF §2.2), en `LabPoroso` casos `Semilla` y `Fibra`:

- **Flotación**: si `mat[i+W] == Water`, `SwapCells(i, i+W)`. El espejo de 1194 que faltaba; lo enterrado sale a flote a 3,75 celdas/s.
- **Deriva**: si `mat[i-W] == Water` y `reposo[i-W] < ReposoMovil`, moverse al lateral vacío en la dirección `aux[i-W] & 1` (la memoria de flujo que `TryFlow` ya graba). Dos campos existentes hacen de campo de velocidad gratis, y el remanso es almacén por construcción.
- **`LabTragar` come polvo más ligero que el agua** y lo cuenta: la pérdida es el juicio, no la inundación.
- Semilla desde la punta, vida por `reposo` y abono como se propone, pero gateado a que «huerto de banco» viva ≥ 24 000 ticks.
- Escenario «arroyo sembrado» primero con 60 fibras (material de nivel, funciona hoy); semillas después.

Tuning que esconde: `PlantaSemillaSavia` decide SI hay semillas, no cuándo, por el presupuesto de transpiración; `SemillaVidaVisitas` tope 255 visitas = 68 s de banco de semillas; la política del sumidero. Cinco números, no tres.

**Corregido: apalancamiento 6, tuning 6, coste 1 semana** (la versión mínima son dos tardes de Opus más el escenario; lo caro es el huerto vivo, y eso no es de este candidato).
