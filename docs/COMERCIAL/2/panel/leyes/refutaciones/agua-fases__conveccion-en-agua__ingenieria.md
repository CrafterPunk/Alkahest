# Refutación de ingeniería · `conveccion-en-agua` (lente agua-fases)

**Veredicto: viable con ajuste.** La rama encaja donde dice y es determinista; fallan la anomalía de 4 °C (no cabe a 2 °C por raw), el escenario de banco (sobre un hogar el agua hierve antes de subir) y el sueño de chunks (un estanque dormido no convecta).

## Lo que el código confirma

- El enchufe es exacto: `ProcessLiquid` (`SimStepper.cs` l. 1138-1244) falla gravedad (l. 1184-1197) y diagonales (l. 1202-1216) antes de `TryFlow` (l. 1220); el gate `def.id == Water` ya existe en l. 1169.
- `Move` (l. 738-752) hace `SwapCells` (`CellGrid.cs` l. 264-279: viajan `temp`, `humedad`, `carga`, `reposo`), sella `touchedTick[idx2]` y despierta 3×3. El barrido sube en `y`, así que la celda que sube no se reprocesa (guarda l. 384-386): una celda por tick, sin doble movimiento.
- «El más ligero sube» es un orden estricto: cada intercambio reduce inversiones, no hay ping-pong vertical ni con `ConveccionPct` 100. Σ`temp` no difiere «< 0,5 %»: es una permutación, difiere 0.
- Sal: la mayor del laboratorio es 643 (l. 129); 647 está libre (grep en ambos archivos).
- Coste: el precio es el chunk despierto (`SleepTicks` 30); «hervidero» (10 000 celdas, `LabBench.cs` l. 171) y «mundo entero despierto» lo acotan; peor caso medido hoy, 3,08 ms.

## Lo que el candidato no vio

1. **La anomalía es código muerto a esta resolución.** 1 raw = 2 °C; 4 °C = 62 raw y `freezesAt` = 60 (`Universe.Laboratorio.cs` l. 211). `ApplyPhase` corre al principio de `ProcessIfNeeded` (l. 396; cuerpo l. 640-655): el agua a 60 es hielo antes de que `ProcessLiquid` la vea. Queda un solo valor líquido bajo el máximo: 61. `flot(61) = 1`, `flot(62) = 0`; con umbral 2 (o 1) nunca dispara; con umbral 0 dispara cada diferencia de 1 raw, que es el paso mínimo de `LabDifusionTermica` (l. 1335-1336) y del tirón a ambiente: churn por ruido. Con `fase-con-reserva` sigue pidiendo umbral ≤ 1. El test «primer hielo arriba con anomalía, abajo sin ella» lo decide dónde encuentra `ApplyPhase` un ≤ 60, o sea junto al frío: con el núcleo arriba las dos configuraciones hielan por arriba.
2. **Sobre un hogar el agua hierve antes de subir.** `LabHogar` (l. 917-946) usa `LabCalentarHasta` con `HogarCalor` = 40 por visita (l. 950-965): la celda en contacto pasa de 70 a 110 en UNA visita y `boilsAt` = 110 la vuelve vapor en el mismo `ProcessIfNeeded`. «El fondo sube antes de 110» solo vale calentando a través de roca (`k = min(8,2) = 2`, l. 1347-1354: +1 raw por visita). «Hervidero» seguirá siendo caldera; el escenario nuevo necesita un suelo de piedra entre hogar y tanque o mide ebullición, no convección.
3. **Un estanque dormido no convecta.** La regla vive en `ProcessLiquid`, que solo corre en chunks despiertos (l. 381-382); `LabDifusionTermica` (l. 1306) recorre toda la grilla. Un estanque dormido que gana gradiente por conducción a través de una pared no se entera: nadie procesa sus celdas. Es el hueco que ya tiene `ApplyPhase`, y la razón de que `LabCalentarHasta` despierte a su destino (R138, A2). El contacto directo se propaga solo; el calor indirecto, no.
4. **`reposo` en las dos celdas: no en `Move`.** `Move` solo anula `reposo[idx2]` (l. 742) y el swap deja en `idx` el de la desplazada. Tocar `Move` rebasa nueve hashes por nada.
5. **La decantación depende de la geometría.** `reposo` sube por visita de `LabAgua` (l. 495, cada 8 ticks); `ReposoMovil` 3 y `DepositoReposo` 24 son 24 y 192 ticks. Con hogar ancho el dado lo reinicia cada ~2 ticks; con fuente estrecha solo la pluma, y el resto deposita a su lado (más rico, no fallo). El test «carga > 80 %» debe fijar el ancho.
6. `LabPresion` (l. 1080-1170) copia superficies y no lee `temp`; convección es interior. Sin conflicto. El bit 0 de `aux` (memoria de `TryFlow`) viaja en el swap: inocuo.

## Versión mínima

Sin anomalía: `flot(t) = t`, dos parámetros (`ConveccionPct` 50, `ConveccionUmbral` 2), sal 647, `reposo[idx] = 0` local tras `Move`. Una línea en `LabAgua`: `if (mat[up] == Water && temp[i] > temp[up] + umbral) WakeChunk`: la pasada de campos despierta lo que la térmica desequilibró. Escenario: tanque 100×30 sobre piso de piedra de una celda, hogar bajo el tercio izquierdo; criterios: a t = 3000 la superficie del extremo frío supera al fondo de ese extremo contra `ConveccionPct = 0`; Σ`temp` idéntica entre corridas salvo lo que `LabRawHogar` y `LabEvaporado` expliquen; `LabDepositado` con hogar ancho < 20 % del de la corrida fría; ms/tick de «hervidero» y «mundo entero despierto» dentro de +15 %. La anomalía vuelve con `fase-con-reserva` y un comparador ensanchado (`(62 − t)·AnomaliaGanancia`, ≥ 4), con escenario propio.

**Coste:** 1 semana. **Apalancamiento:** 6 (transporte de calor por materia, claridad como termómetro, evaporación de toda la superficie, radiador; el hielo por arriba lo da la geometría con o sin esto). **Tuning:** 8.
