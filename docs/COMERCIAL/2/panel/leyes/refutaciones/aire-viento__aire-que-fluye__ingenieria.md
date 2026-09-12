# REFUTACIÓN DE INGENIERÍA · `aire-que-fluye` (lente aire-viento, candidato 1)

**Veredicto: VIABLE CON AJUSTE.** Apalancamiento 9 → **7**. Tuning 7. Coste **1,5 semanas** (se sostiene, repartido de otra forma).

## 1. Lo que aguanta la lectura del código

- **Acotado y determinista.** Enteros, barrido de orden fijo, escrituras en sitio con el mismo criterio que `LabIntercambioVapor` (`SimStepper.Laboratorio.cs` l. 375-388). Sin flotantes: apto para lockstep. 442 KB.
- **Coste medido, no estimado.** `LabCampos` despacha `LabAire` sobre TODAS las celdas `Empty`, dormidas o no (l. 190-196). Carbonera (~213 k celdas de aire): fase `campos` 0,48 ms; nivel de referencia (~31 k, sumando los `Aire(...)` de `SimLevelBuilder.Laboratorio.cs`): 0,15 ms. Doblar `LabAire` es **+0,07 ms en el nivel real y +0,45 en los sintéticos**; el aire quieto sale por `diff ≤ 1` y `temp == ambient`. Cabe sin plan B.
- **Encaja donde dice.** `ProcessCombustion` l. 855, `LabRespira` l. 1052, `ProcessGas` l. 1528 (`rumboLeft`), `LabVistaColor` es un `case` más, `R()` de `LabParams` admite cinco parámetros.

## 2. Lo que se refuta: el tiro, tal como está escrito, no bombea

La flotación compara **masa**, no presión: `sube mientras aire[up] − aire[i] < exceso·AireEquilibrio/16`. Cuando la sala se agota (justo cuando la chimenea debe tirar), el fondo está pobre y la boca del cielo fija la cima a 128: el gradiente supera el tope y **la bomba se apaga sola**. En estacionario el flujo neto por columna queda acotado por `tope/(2·AireDifusion)`: 1-2 u cada 8 ticks. Una pila de 25 celdas consume `AireConsumo·vecinos` cada 8 ticks ≈ **9 u/tick**. Predicción: `Σvy ≈ 0` o negativo (el aire BAJA por la chimenea a alimentar el fuego). El «riesgo mayor» no es un riesgo: es el resultado esperado del modelo.

Y el banco propuesto **no lo detectaría**: la curva boca→respiración es monótona por pura difusión (boca 1 ≈ 1,5 u/tick; boca 8 ≈ 12), sin una unidad de flotación; y el humo ya sube solo (`ProcessGas` l. 1560), luego «humo fuera > dentro» tampoco discrimina. Solo `Σvy` y *chimenea abierta frente a tapada a igual boca* prueban el tiro.

**Cruces mal atribuidos.** «Seca a sotavento» y «transpira mejor al viento» exigen que el vapor VIAJE con el aire: eso es el candidato 2. El 1 no escribe `humedad[]`; `LabSecarHacia` (l. 734) ve el mismo déficit con o sin corriente. Dos de cinco cruces no son suyos: de ahí el 7.

## 3. Sitios y detalles que faltan

1. **`LabPresion` l. 1154-1160** muda a mano (`SetCell` + copia de campos): `aire` no viajaría con el hueco y la auditoría Σaire fallaría en «diluvio» y «alambique». Sitio no listado.
2. **`viento` debe ser posicional** (como `luz`), no viajar en `SwapCells`; y `LabCampos` no despacha `Smoke/Steam/Fire`: bajo un humo estancado el viento no se recalcula nunca. Añadir el caso de gas.
3. **Dos nibbles con media `(3v+n)/4`** truncan a cero los flujos de 1-3 u, que es el rango real de `diff/8`. Dos bytes por eje o sin media.
4. **Asfixia global.** La cueva es cerrada; la boca del cielo (7 celdas, x118-124) inyecta ≤ 14 u/tick y no llega a 700 celdas. Una carbonera gasta ~48 k u (1 % del mundo); a ×10 durante horas, cien fuegos ahogan la cueva y nada la repone. Hace falta **relajación lenta hacia 128** (`AireRelaxTicks`, calculada para que una sala de 200 celdas reponga menos de lo que consume una sola pila): asfixia local sí, global no. Mismo recurso que `TiroAmbienteTicks`; se fija por cálculo, no por playtest.

## 4. Versión mínima y su banco

**Entrega A (1 semana):** `aire` consumible + `LabRespira` por umbral + llama mortal (`ProcessFire` l. 1683: `life = 30` solo con `aire ≥ AireMinRespira`) + rumbo del gas por `viento` + relax global + auditoría + visor. **Sin flotación.** Ya compra el regulador cuantitativo, cerrar-para-carbonizar, abrir-para-calentar, el cuarto que se asfixia y el humo como veleta.

**Entrega B (0,5 semanas, condicionada):** el tiro con **presión, no masa**: difundir `p = aire·(temp+120)` más un sesgo hidrostático de 1 u/celda vertical. Mismo coste (un producto por intercambio); es la Δp en la base que el tiro real necesita.

**Gates:** (i) «laboratorio base» y «diluvio turbio» con `mat/temp/aux/humedad` **intactos** (nada arde, no hay gas: el campo debe ser inerte) más `HashAire/HashViento`; (ii) «chimenea con boca N»: `Σvy` en la sección y aire medio de la sala *abierta vs tapada* a igual N. Si B no separa las dos curvas en dos días, B muere y A se queda.
