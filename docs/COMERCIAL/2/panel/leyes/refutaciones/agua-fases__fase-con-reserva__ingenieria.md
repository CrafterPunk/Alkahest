# Refutación de ingeniería · `agua-fases` / `fase-con-reserva`

**Veredicto: VIABLE CON AJUSTE.** Apalancamiento 8 · tuning 8 · 1,5 semanas (1 la versión mínima, 0,5 el hervor en fase B).

## Lo que el código confirma

- Los ganchos existen. `ApplyPhase` condiciona cada transición a un sentinel (`SimStepper.cs:646-657`) y `AplicarOverridesLaboratorio` ya muta `freezesAt/boilsAt/meltsAt` en sitio (`Universe.Laboratorio.cs:211-213`): anularlos son tres líneas sin tocar el juego heredado.
- La reserva cabe. Bits 1..7 de `aux` en líquidos es la convención de `GetCombustReserva/SetCombustReserva` (`SimStepper.cs:766-785`); `TryFlow` escribe solo el bit 0 (`:1352`); el agua no es `flammable`. En hielo (StaticSolid) `aux` está libre. `SwapCells` lleva `aux` (`CellGrid.cs:268`): la reserva viaja al caer, fluir y flotar.
- `LabCampos` y `LabDifusionTermica` comparten el estriado `offset = _tick % 8` (`SimStepper.Laboratorio.cs:205` y `:1308`): cada celda difunde y se visita el mismo tick, así que el pin a 60 no deja huecos. Y `LabCampos` recorre TODA la grilla, chunks dormidos incluidos: mejor sitio que `ApplyPhase` para fundir un tapón lejos del hogar.
- Flotación sin ping-pong: `SolidoTieneApoyo` (`SimStepper.cs:1382`) toma por apoyo cualquier vecino inferior no vacío, luego el hielo sobre agua no cae; el swap junto a la rama de gravedad de `ProcessLiquid` (`:1185`) sube una celda por tick y `Move` marca `touchedTick`. `LabPresion` no copia `aux` (`:1150`): una línea, como dice el candidato.
- Sin dado nuevo, enteros; una comparación por celda de agua despierta y un `case` en un switch 1/8; todo lo propuesto son contadores y hashes (`LabBench` ya hashea `aux`).

## Lo que refuto del texto tal cual

1. **Falta el aguanieve.** `LabGotear` nace el agua con la temperatura de la superficie (`:321`): en el serpentín, 30 raw. Con el pin a 60 escrito como está, la gota recibe +30 raw gratis y su acumulador queda en 30 mientras el aire la calienta sin cobrárselo. Con 900 goteos son ~27 000 raw inventados en la cámara y `LabRawCongela == LabRawFusion` no cierra. Regla mínima: si `temp > 60 && acum > 0`, `q = min(temp − 60, acum); acum −= q; temp −= q`. Tres líneas, y la única forma de que el frío finito lo sea.
2. **No existe caja adiabática.** `LabDifusionTermica` acota el tirón a `TiroAmbienteTicks < 8 ? 8` (`:1311`) y el panel lo tope en 256: «al máximo» sigue empujando ±1 raw hacia 70 cada 256 ticks y funde el hielo del test de identidad. Hace falta el sentinel `0` = sin tirón (una línea) o formular el banco sobre `Σtemp + Σreserva` con el tirón contabilizado.
3. **La mitad del hervor apenas compra.** Junto al hogar (+40 raw por visita) el acumulador sobre 110 se llena en una visita: hierve como hoy. El «≤ 110 detrás de la olla» ya es emergente: ninguna celda de agua sobrevive sobre 110 en `ApplyPhase`. Peor: con el clavado a 1/8, la celda que el hogar inyecta en SU tick se queda hasta 7 ticks a 150 y los vecinos difunden contra ese valor: el benchmark saldrá ~115-120 y se leerá como fallo. Fuera de la versión mínima; `Latente = 4` se queda.
4. **Detalles que rompen el banco.** Con el sentinel, `ApplyPhase` deja de emitir `Freeze`: la rama nueva debe hacer `PushEvent`. `LabTransformar` pasa por `SetCell`, que resetea `aux` (`CellGrid.cs:242`): la reserva se escribe después, con `hum` y `carga` explícitos para que `LabBalanceU` no vea creación. Y «laboratorio base» y «mundo entero despierto» llevan núcleo frío (`LabBench.cs:108, 225`): cinco hashes rebasados, no tres.

## Versión mínima

Solo fusión: sentinels de `freezesAt/meltsAt`; `LatenteFusion` (48 raw ≈ 96 °C-equivalente; el valor físico son ~80 K: se lee, no se afina); pin a 60 con aguanieve en `LabAgua` paso 0; `case Ice: LabHielo` en `LabCampos`; `HieloFlota` en `ProcessLiquid`; `aux` en `LabPresion`; `LabRawCongela/LabRawFusion`; `TiroAmbienteTicks = 0`; `Freeze` emitido. Hervor con reserva en fase B, solo si un escenario demuestra que el instantáneo no basta.

## Benchmark que lo prueba

1. Caja adiabática (`TiroAmbienteTicks = 0`): tanque a 55 raw congela; hogar 3000 ticks funde; `LabRawCongela + LabRawFusion == 0` y `Σtemp + Σaux` constante al raw.
2. Gota nacida a 30 raw en aire a 70: `acum` vuelve a 0 antes de que `temp` supere 60.
3. Tapón de 1 celda a 5 del hogar: tick de fusión reproducible y monótono en `LatenteFusion` (24/48/96).
4. Estanque con tapa: `LabEvaporado = 0` y `LabPresionMovidas = 0` en 3000 ticks; al fundir, ambos > 0.
5. «alambique»: `Freeze` > 0 y humedad media del lecho contra la de hoy (dice si el granizo riega o hiela).
6. ms/tick en «mundo entero despierto» dentro de +2 %; cinco hashes rebasados y explicados.
