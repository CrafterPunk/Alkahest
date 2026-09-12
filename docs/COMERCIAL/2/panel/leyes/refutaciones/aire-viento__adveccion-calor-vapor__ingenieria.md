# Refutación de ingeniería · `adveccion-calor-vapor` (lente aire-viento)

**Veredicto: viable con ajuste.** Condicionado por entero al candidato 1: `aire` y `viento` no existen en `CellGrid.cs`, y `LabAire` (`SimStepper.Laboratorio.cs` l. 328-389) solo mueve vapor.

## Lo que el código confirma

- El enchufe existe: `LabIntercambioVapor` (l. 391-404) es una transferencia entera, en sitio, con recorte a 255 y sin dado; copiarla para calor es determinista sin sal nueva. `HashTemp` y `HashHumedad` ya están en `LabBench.Resultado` (l. 38-40).
- `Conveccion` (`LabParams.cs` l. 113) solo sesga `k` en `LabFlujoTermico` (l. 1347-1355); ponerlo a 0 no rompe nada estructural.
- Coste: `MsCampos` ya se mide por fase (l. 134-138). Una multiplicación, un recorte y dos escrituras por transferencia; en reposo `diff <= 1` sale antes. Estimo +10-20 % de `LabAire`, menos de 0,1 ms: cabe en los 1,6-1,9 ms.

## Lo que el candidato no vio

1. **El humo ya lleva su calor.** `SwapCells` (`CellGrid.cs` l. 264-279) intercambia `temp` y `humedad`, y `Move()` (`SimStepper.cs` l. 738) lo usa para todo gas. El humo caliente que sube por la chimenea advecta hoy; lo que falta es el `Empty`. El apalancamiento declarado cuenta como nuevo algo que ya existe a medias.
2. **`LabAire` solo visita `Empty`** (`LabCampos`, l. 215): los gases reciben vapor pero nunca lo dan. El tiro del «hervidero» va lleno de vapor visible y una chimenea con humo no tendría celdas que ejecuten la regla. Deuda del 1 que el 2 hereda.
3. **La fórmula no conserva raw.** `temp[j] += (temp[i] − temp[j])·t/128` calienta `j` sin enfriar `i`. Con masa sería correcto (propiedad intensiva), pero aquí `temp` es raw por celda sin masa y el «libro del calor entregado» (l. 84-88, `LabRawFuego`…) presume que todo raw escrito tiene nombre. Con el hogar clavado a 170 y la llama a 255, cada transferencia es una fuente sin contador. No rompe un invariante exacto (C2 es estadística), pero ensucia el único libro que «sí admite un TOTAL».
4. **`hum[i]·t/aire_i` divide por cero** cuando el consumo del 1 deja `aire = 0` junto a un fuego. Y la magnitud es corta: con t = 1-8, t/128 es un 1-6 % de la diferencia por visita frente al 6,25 % por vecino de la conducción isótropa (`KAire = 4`, `step = 4d/64`, l. 1332). La lengua que sigue al viento queda enterrada salvo que el coeficiente sea un parámetro.
5. **El benchmark no discrimina.** «≥ ambient + 8 con conducto, = ambient sin él» lo pasa también la conducción por un conducto de `Empty` sin flujo alguno.

## Versión mínima viable

- Helper `LabAdvectar(i, j, t)` desde cada transferencia de aire del 1: `d = (temp[i] − temp[j])·t·AdvecCalor >> 7`, recortado a `|temp[i] − temp[j]| / 2`; `temp[j] += d; temp[i] −= d`. Par: Σtemp invariante por construcción, contracción sin sobrepaso, truncamiento hacia cero (regla 9). Vapor: `v = hum[i]·t·AdvecVapor >> 7`, recorte a lo que cabe, resta y suma emparejadas: `LabBalanceU` intacto. Sin división por `aire_i`, sin sal, sin dado.
- Dos parámetros en `LabParams` (`aire.advecCalor`, `aire.advecVapor`, default 16) y un contador `LabRawAdvectado` (0 neto: es la auditoría).
- `Conveccion` a 0 solo si «laboratorio base» enseña doble subida; guardas de no regresión: «horno con yesca» `LabVidrio` 18/18 y el hogar que prende yesca (130) y no carbón (200).

## Benchmark que lo prueba

Escenario «dos cámaras»: la chimenea con boca 3 del 1 más un conducto de `Empty` que cruza dos cámaras iguales de roca, una AGUAS ARRIBA del hogar (entre boca y fuego) y otra AGUAS ABAJO. Métrica: `temp` media final de abajo menos arriba. Con `AdvecCalor = 0` debe ser ≈ 0 (la conducción es isótropa); con 16, ≥ +8 raw y monótona en {4, 8, 16, 32}. Auditoría: Σtemp del aire solo cambia por `LabRawFuego + LabRawHogar + LabRawFrio + latente + tiro` (con `TiroAmbienteTicks = 256` para aislar). «hervidero» con `Goteos` habilitado (hoy solo alambique y arco largo, `esAlambique` l. 295): ≥ +30 % frente al 1 solo.

## Valores corregidos

Apalancamiento 7: multiplicador real del 1, cero sin él, y parte de lo prometido ya existe por `SwapCells`. Tuning 7: dos coeficientes con curva de banco, más la decisión `Conveccion`/`KAire` si la conducción tapa la lengua. Coste 0,5 semanas tras el 1: helper, parámetros, escenario «dos cámaras», `Goteos` en hervidero y rebase.
