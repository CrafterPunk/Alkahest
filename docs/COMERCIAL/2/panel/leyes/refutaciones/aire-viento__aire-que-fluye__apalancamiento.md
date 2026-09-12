# Refutación · aire-viento / aire-que-fluye · lente APALANCAMIENTO

**Veredicto: VIABLE CON AJUSTE.** El candidato pega dos leyes de valor muy distinto: «el aire se gasta» (masa conservada, consumo, difusión, fuente) y «el aire fluye» (flotación, `viento`, gas que lo lee). La primera es un cruce real, barato y falsable que cierra el negativo medido de la llama inmortal. La segunda no funciona como está escrita, y dos de sus cinco cruces son del candidato 2.

## 1. Cruces: cinco declarados, dos reales

- **Fuego**: real. `LabRespira` (Laboratorio.cs 1052-1062) cuenta vecinos por material; leer `aire[j] >= 32` convierte la sordina en caudal, y `ProcessFire` (SimStepper.cs 1673, `life = 30` con combustible al lado) deja de ser inmortal si su celda se vacía. Único cruce que toca una decisión medida (INFORME_FINAL 123-126).
- **Humo**: parcial. `ProcessGas` (1531-1560) sube siempre que hay `Empty` encima; `rumboLeft` solo decide la ondulación y el lado de la bolsa. Nadie lee `vy`: con tiro o sin él el humo sube igual.
- **Secado y plantas**: falsos aquí. `LabSecarHacia` (734-751) lee `humedad[j]`; el byte `aire` no mueve `humedad`. Es la advección del candidato 2, cobrada por adelantado.
- **Térmica y luz**: sin cambio, por declaración.

## 2. La columna no bombea: el tiro no emerge del 1 solo

La flotación usa `exceso = temp[i] − ambient[i]`. En una chimenea de aire entre roca, `LabFlujoTermico` (1347) toma `k = min(KAire 4, KRoca 2)`, dobla desde abajo y halva hacia arriba, con `CAire = 1`: el balance por visita es `8e(n−1) + 2e(n+1) − 14e(n)`, cuyo modo estacionario decae ×0,63 por celda. Diez celdas sobre el fuego el exceso es cero; solo el humo lleva calor (`Move` → `SwapCells`, 264-279), un 16 % de los pasos de fibra y un 4 % del carbón. Queda una burbuja de tres celdas calientes cuya masa sale por difusión: por un conducto de 40 celdas, Δ/(8·40) unidades por visita; la altura PERJUDICA al tiro. El «Δp ∝ Δρ·g·h» exige columna caliente: el candidato 2 dentro del 1 desde el primer día. El plan B (subir `AireFlota`) convierte toda sala templada por un hogar de 170 raw en bomba de techo.

## 3. «SetCell no los toca» rompe la conservación que promete

`LabNacerAgua` (264) y `LabTransformar` (254) pasan por `SetCell` (CellGrid 240), que no tocaría `aire`: el agua que nace en un `Empty` esconde 128 unidades; `LabTragar` (1032) las suelta junto al sumidero. A `Caudal = 24` celdas/s son ~3 000 u/s teletransportadas, contra ≤16 u por visita y celda de boca (`diff/8`). Igual con `LabGotear`, la ceniza de `ProcessFire` (1697) y la planta que crece. `Σaire` cuadra y la física es absurda. Falta desplazamiento explícito en cada nacimiento y muerte de celda; no está presupuestado.

## 4. El banco no verifica lo prometido, y la escala esconde el tuning

`LabBench.Correr` (277-279) pone `LuzCieloX0/X1 = −1` en los nueve escenarios: la fuente no existe en el banco. Y «N=0 → ≤20 % quemado» es inalcanzable con `AireConsumo = 2`: sala más chimenea guardan ~38 000 u; bajar a 32 exige gastar ~29 000; una fibra entera consume 40 pasos × ~3 u = 120 u, así que hacen falta 1 200 fibras para lograrlo con el 20 %, en una sala de 216 celdas interiores. Subir el consumo ×10-20 hace parpadear en sordina al fogón abierto, porque la difusión repone como mucho 12 u por visita y vecino. Un byte con tres escalas acopladas (reserva de sala, reposición local, fuente); la que decide «qué sala ahoga qué fuego» la elige un humano contra las salas de los jugadores. Además `MontarHorno` (116-131) es una caja sin boca: ~36 celdas vacías, 4 600 u, que la fila de carbón expuesta gasta en ~1 000 ticks. El 18/18 medido pasa a depender de `AireConsumo`. Eso esconde el 7/10.

## 5. Ajuste mínimo, en dos entregas

**A (0,7 semanas, donde está el apalancamiento):** `aire` + difusión + fuente + consumo + `LabRespira`/`ProcessFire` leyendo el byte + desplazamiento en `LabTransformar`/`LabNacerAgua`/`Transform` + `HashAire` + vista Presión + boca de cielo activa en el banco. Sin flotación, sin `viento`, sin tocar `ProcessGas`. Compra: apagar cerrando, bancar brasas, cuarto que se asfixia, carbonera por caudal, llama mortal. Tres de las seis decisiones.

**B (1 semana, falsable en dos días):** flotación CON advección de calor y vapor, `viento`, `ProcessGas` y el escenario de la chimenea con `Σvy` como criterio de muerte. Si no bombea, se tira B y queda A entera.

**Valores corregidos:** apalancamiento **6** (A sola 5; A+B si el tiro vive, 8); tuning **5**; coste **2,2 semanas** (A 0,7 + B 1,0 + re-balance de horno, carbonera y tolva 0,5).
