# Refutación de ingeniería · `rayosx-bandas-de-ley` (lente instrumentos-rayosx)

**Veredicto: VIABLE CON AJUSTE.** No toca la simulación ni el presupuesto por tick, y sus pruebas caben en el banco. Hay que corregir cuatro afirmaciones que el código desmiente y evitar una tentación de diseño que quitaría un instrumento al equipo.

## Lo que aguanta

- **Determinismo y tick: intactos por construcción.** `LabVistaColor` (`SimRenderer.Laboratorio.cs`) solo se evalúa con `LabVistaRellenando` dentro de `RenderChunk`; cambiar rampas por bandas es sustituir tres multiplicaciones por dos comparaciones. La máscara por sonda (Chebyshev, radio 24) son tres comparaciones por celda con vista activa. Nada escribe en `CellGrid`: los siete hashes de `LabBench.Resultado` no pueden moverse.
- **`LabBandas` como C# puro** encaja: `LabMateriales.Estado` ya es eso (lee `PlantaHumedadMin` y `TurbidezFuente` en vivo) y `DibujarLectura` (`LabPanel.cs:647`) ya reconstruye el rótulo cuando esos parámetros cambian.
- **Sondas con campo**: `Termometro.cs` tiene los tres slots FIFO, el muestreo a 4 Hz sin allocs y la tabla de strings. Un byte `campo` por slot y una tabla de palabras por banda es el mismo patrón.

## Lo que el código desmiente

1. **«Todas leen `LabParams` en vivo».** Falso para las bandas de temperatura que más importan: las igniciones viven en `MaterialDef.ignitionTemp` (`Universe.Laboratorio.cs:70/83/159`: fibra 140 raw, yesca 130, carbón 200), y el abono es un literal `h >= 128` en `SimStepper.Laboratorio.cs:659`, contra la regla de cabecera de `LabParams`. `LabBandas` debe recibir el `Universe` (el banco ya lo crea) o promover esos números a parámetros; si no, la banda «prende» miente en cuanto cambie la química.
2. **La asimetría multijugador no existe.** `SimSync.cs:51-56` replica solo `mat`; en el espejo `Stepper == null` y `Termometro` ya escribe «—». Un invitado con vista de humedad vería el ambiente de arranque. Salida que el propio candidato regala: una banda son 3 bits, un parche de bandas en un radio pesa ~900 B por sonda y cabe en el formato de `SimSync`. Pero es otro encargo, fuera de la semana declarada, y solo en ruta A.
3. **«Palabra, color y física coinciden siempre»**: no. El comentario R142 de `LabMateriales` dice que `EncharcadoU`, `AireSaturadoU`… son juicios de lenguaje, no umbrales de simulación. Y los umbrales reales de humedad dependen del material (arcilla 30/250, sedimento 100-230, fibra 100, ceniza 128, sustrato 60): una tabla por material haría que el mismo color signifique cosas distintas a un píxel de distancia, justo el riesgo que el candidato nombra. Ajuste: cortes globales y monótonos por campo, ocho colores fijos, y que el lector diga cuál corte le importa a ese material.
4. **El agregado 4×4 con zoom lejano** no tiene consumidor: `Flask.cs:32-35` documenta que el zoom se rechazó. Fuera.
5. Detalle: la condensación es `h > sat` estricta (`SimStepper.Laboratorio.cs:357`); la banda «≥1 condensa» está corrida en uno.

## La tentación a evitar

`VistaLaboratorio` global es el instrumento con el que Cesar y los agentes cerraron H1-H4 (capturas, panel F8). Sustituirla por la vista enmascarada quita una herramienta de trabajo para dar una de juego. Ambas comparten `LabBandas` y `LabVistaColor`; separarlas es un bool (`MascaraPorSondas`). Plantar o quitar una sonda debe pasar por `_vistaLabCambiada → MarcarTodoSucio()`: los chunks dormidos solo repintan cada 30 frames (`SimRenderer.cs:880-895`) aunque `LabCampos` visite el mundo entero.

## Versión mínima y benchmark

`LabBandas` (Universe + LabParams, cortes globales por campo), `LabVistaColor` por bandas, bool de máscara, `Termometro` con campo por sonda y palabra de banda en la placa, lector con la banda. Sin 4×4, sin réplica al invitado (que muestre «—» como hoy).

Prueba en el banco: `LabBandas.Autocomprobar()` añadido a `LabBench.Informe`: para cada parámetro-umbral registrado y cada material, `banda(u) != banda(u-1)`, repetido con cada slider en su `Min` y su `Max`; resultado «0 fallos». Nueve escenarios con los siete hashes idénticos antes y después. Lo que no se prueba sin persona es si ocho colores se leen sobre celdas de tres píxeles: la pátina «mojada» que Cesar apagó en el playtest 44 es el precedente, y por eso el tuning no es 9.

**Apalancamiento 6** (solo la pata REVELAR; sin asimetría multijugador ni zoom, lo nuevo es una decisión: qué medir con tres sondas). **Tuning 8.** **Coste 1 semana** en la versión mínima; +0,5 si se quiere la réplica de bandas al espejo.
