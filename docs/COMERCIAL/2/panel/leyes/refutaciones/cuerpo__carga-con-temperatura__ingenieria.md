# Refutación de ingeniería · `cuerpo` / `carga-con-temperatura`

**Veredicto: VIABLE CON AJUSTE.** Apalancamiento 7 · tuning 7 · 1 semana sobre C1, que aún no existe (`grep CuerpoSim|LabCuerpoJugador|TFrasco` en `Assets/`: cero líneas).

## Lo que el código confirma

- El frasco ya lleva temperatura por material (`Flask.cs:112-114`, `TempMediaDe` en `:145`) y la restituye al verter (`:686-687`). `AjustarTemp(delta)` es `_tempSum[m] += delta·_counts[m]`: honesto con la estructura existente.
- La brasa entra al frasco: es `Powder` (`Universe.cs:936`) y `EsAspirable` solo veta piedra, piso, sólidos del mundo y `Fire` (`Flask.cs:522-531`). El hielo también (`:589-593`; no está en `EsSolidoDelMundo`).
- Los umbrales existen y el laboratorio los decreta: agua congela a 60 raw, hierve a 110, hielo funde a `CToRaw(5)` (`Universe.Laboratorio.cs:211-213`). La sordina es real: `LabPasoSordina(x, y, 4)` descuenta una unidad de cada cuatro pasos (`SimStepper.cs:1048`): 16 ticks por unidad, 32-48 s de brasa en el frasco. El 10 raw por visita coincide con `(BrasaCalorRaw+1)/2` cada 4 ticks (`:1029`).
- Determinismo: entradas escritas antes de `Step()` como la caldera (`LabBench.cs:307-311`), enteros. Coste por tick: una suma y tres comparaciones cada 8 ticks.

## Lo que refuto del texto tal cual

1. **La inercia térmica no existe con la fórmula que cita.** `LabDifusionTermica` hace `step = flujo/(64·c)` y luego `if (step == 0 && flujo != 0) step = ±1` (`SimStepper.Laboratorio.cs:1333-1334`). Con `k = 2` y `c = CAgua·30 = 120`: `flujo = (78−110)·2 = −64`, `step = 0 → −1`. Con 3 celdas (`c = 12`): también `−1`. Las dos cargas se enfrían 1 raw por visita. Y en las 50 visitas del guion de 400 ticks el agua baja de 110 a 60 raw = `freezesAt`: la regla (1) la vuelve hielo. **El agua hirviendo llega a la cámara alta congelada.** Hace falta un resto fraccional propio (byte `RestoFrasco`, o el dado de `LabLatente`, `:281-296`) y quitar el suelo de ±1 a esa celda virtual.
2. **La mano no se abre por la brasa.** El frasco es un término más en la suma de C1 sobre 66 celdas: aporta como mucho `(255−78)·2 = 354`, mientras el aire a 70 raw con `k = min(8, KAire 4)` tira `66·(70−78)·4 = −2112` por visita. `Calor` oscila en 71-72 y jamás cruza `calorSuelta` (100). El clip central no ocurre. Ajuste honesto: la mano lee el TARRO, no el núcleo: `TFrasco ≥ piel.quemaMano` la abre; el intercambio con `Calor` queda como abrigo/refresco.
3. **La brasa vertida hoy muere al nacer.** `PaintCell` pasa por `SetCell`, que resetea `aux` (`AlkahestSim.cs:617`, `CellGrid.cs:242`); `ProcessBrasa` con `life == 0` la vuelve ceniza en su primer paso, tras un solo dado del 8 % (`:1038-1053`). «La yesca prende con la brasa caída» exige `VidaBrasa` en el frasco y `aux` al verter. No son doce líneas: `CuerpoSim` necesita `CeldasAgua/Hielo/Brasa`, `VidaBrasa` y una struct de órdenes de vuelta (fundir/congelar/hervir/apagar) que `Flask` aplica con recorte, porque aspira y vierte a frame rate entre ticks. Unas 80 líneas y `Flask.cs` se reabre en cuatro sitios.
4. **El riesgo multi está al revés.** El anfitrión YA descarta el `tempRaw` del invitado y vierte a temperatura estable (`SimSync.cs:1111-1125`). Con `Cuerpos[4]` en el anfitrión, el vertido del invitado usa `Cuerpos[i].TFrasco` local: la ida y vuelta que teme el candidato desaparece. Lo que falta es subir los conteos invitado→anfitrión (un RPC), porque `Flask` corre solo en el dueño (`AprendizNet.cs:165`).
5. **Hervir vacía el frasco.** Una celda de vapor por visita son 3,75 celdas/s: 30 celdas a 110 raw se van en 8 s si no templan antes. Consecuencia legítima, pero la mide el informe, no el playtest.

## Versión mínima

Sobre C1: `RestoFrasco` sin suelo ±1; `piel.kFrasco` y `piel.quemaMano` en el grupo PIEL; conteos por material y `VidaBrasa` en `CuerpoSim`; órdenes de fase a una celda por visita (las tres, sin fundir todo el hielo de golpe); `PaintCell` con `aux` para brasa; vapor por la boca con `SetCell` + `gasLifetime`; `TFrasco` en `HashCuerpo`. Vaho y escarcha, después.

## Benchmark que lo prueba

1. «brasa en mano», cuerpo quieto: tick en que `TFrasco ≥ quemaMano` y tick en que `VidaBrasa` llega a 0 (debe caer en 960-1440).
2. «agua hirviendo a la cámara alta», 30 y 3 celdas por el mismo guion: dos temperaturas de llegada distintas, monótonas en `kFrasco` (1/2/4), ninguna congela.
3. «hielo al taller»: 10 celdas a 30 raw, 300 celdas por aire a 70: tick de la primera fusión.
4. Brasa vertida sobre fibra seca: `Ignite > 0` en las 9 seeds del banco.
5. Los nueve escenarios sin cuerpo: hashes idénticos al bit, ms/tick dentro de +1 %.
