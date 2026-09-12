# Choque térmico con memoria de cocción — refutación de ingeniería

**Veredicto: viable con ajuste.** Apalancamiento 7 · tuning 7 · 0,75 semanas.

## Lo que el código confirma

- **Encaje.** `LabCampos` (`SimStepper.Laboratorio.cs:201-238`) agrupa `Terracota` con `Stone` en `LabRoca`; hay que separar el caso (`LabRoca` + `LabCocido`) y añadir `case VidrioVerde`, que hoy no tiene entrada. `reposo` está libre en ambos: `LabTransformar` (l. 254-262) lo pone a 0 al cocer o vidriar y nadie lo toca después; `SwapCells` lo transporta y `ProcessSolidoCohesion` (`SimStepper.cs:1407`) cae con `SwapCells`, no con `Move`, así que una pared que se derrumba conserva su memoria. Sales 601-643 ocupadas; 647 libre.
- **Conservación.** `SetCell` (`CellGrid.cs:239`) no escribe `temp`: la temperatura se conserva gratis. El agua NO: `LabTransformar(i, Grava, 0, 0)` haría que `LabBalanceU` contara como destruido el rocío de la terracota; hay que pasar `humedad[i]` (la grava es porosa y lo admite).
- **Determinismo y coste.** Escritura solo en la propia celda; RNG `XorShift.FromCell(_tick, x, y, sal)` como `SalLabCompacta`; cuatro lecturas por visita en las pocas celdas cocidas del mundo. Nada que medir.

## Donde el candidato se equivoca de física (a su favor)

El riesgo mayor declarado, «la olla que se raja al hervir», parte de una premisa que no está en el código: **el agua no tiene punto de ebullición**. `LabAgua` (l. 406-432) solo evapora la celda con aire encima, y `LabLatente` resta 4 raw POR CELDA ENTERA (~1 raw por visita); el resto del agua se calienta por `LabDifusionTermica` hasta la temperatura de la pared. Y esa difusión (l. 1333: `if (step == 0 && flujo != 0) step = ±1`) mueve pared y agua a 1 raw por visita cada una: el agua que está desde el principio sigue a la pared con un desfase de 2-3 raw. Δ ≥ 60 solo ocurre con agua que LLEGA fría. La semántica «orden temporal» no la da la regla nueva: ya la da la térmica existente. El montaje (a) pasa con margen enorme.

## Donde se equivoca de números (en contra)

El hogar no calienta a nadie por encima de 170 (`LabHogar`, l. 925, R136-C1), así que una caja sobre el hogar está a 165-170 tras 3000 ticks. Montaje (c): `reposo` 220 → umbral 60 + 35 = 95; agua a 70 → Δ ≈ 95-100 → **se raja**. El aserto «0» está en el filo o falla. Y (b)/(c) con la caja pintada por el banco arrancan con `reposo` 0: la primera visita lo fija a 70 (< `TerracotaRaw`) y el término `(reposo − 150)·50/100` sale negativo: umbral 20. Los «cuatro parámetros fijados por cálculo» los fija el banco, no el cálculo.

## Donde se equivoca de juego

«Cocer en horno o en hogar según el uso que tendrá la pieza» supone mover la pieza. No hay verbo: `Mudanza` mueve `IMovible` (aparatos), no celdas; el cincel la vuelve grava; el frasco lleva fluidos. Lo cocido se usa donde se coció. La decisión real es dónde levantas la arcilla respecto a la fuente: la cara interior de un horno de arcilla queda a 200+ (umbral ≥ 85) y la exterior a 130-150 (umbral 60): el punto débil es la piel externa, y eso sí es legible. `LabCuerpos` (l. 1290) es un gancho vacío; el cruce con `RocaSuelta` es futuro.

## Ajuste mínimo

1. `exceso = max(0, reposo − TerracotaRaw)`; `Estado` dice «cocida a X °C» solo si `reposo ≥ TerracotaRaw`.
2. `LabTransformar(i, Grava, humedad[i], 0)` para terracota; `(i, Sand, 0, 0)` para vidrio.
3. Banco `olla`: (a) como está; (b) y (c) llenan a **90 raw** (Δ 80: raja a umbral 60, aguanta a 95, 15 raw de margen a cada lado); (c) siembra 220 DESPUÉS de la primera visita. (d) «horno con yesca» + goteo de 70 raw en el tick 6000 sobre el vidrio → `LabChoques ≥ 1` y `LabVidrio` vuelve a subir. Regresión honesta: `HashMat`/`HashTemp` intactos en los 9; `HashReposo` de «horno con yesca» cambia (la memoria escribe en el vidrio) y estrena línea base.
4. `LabChoques` en `Informe`; los cuatro parámetros en `LabParams` vía `R(...)`.

## Valores corregidos

Apalancamiento 8 → 7: el accidente agua-fuego es sólido y barato, y la térmica existente le da la semántica correcta sin ayuda; el dial de cocción vale menos sin verbo de traslado. Tuning 8 → 7: la tasa `ChoquePct` es gusto (cuánto tarda una tubería caliente en ceder a un goteo). Coste 1 → 0,75 semanas: cuatro montajes de banco, cuatro parámetros y un rótulo; la física ya hace el resto.
