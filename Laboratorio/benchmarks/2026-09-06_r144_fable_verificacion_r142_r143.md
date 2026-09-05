# R144 — FABLE: VERIFICACIÓN DE R142 (HF5c + H5) Y R143 (SONIDO Y DIARIO)

Banco headless por `Unity_RunCommand`, sin Play, sobre el assembly de la R143.

## 1. El banco H5 reproduce sus hashes (determinismo entre sesiones)

`LabBench.Correr` sobre dos escenarios, comparado con la tabla de Opus en
`2026-09-04_r142_hf5c_y_h5.md`:

| escenario | ticks | ms/tick | hash mat (R142) | hash mat (R144) |
|---|---:|---:|---|---|
| laboratorio base | 3 000 | 1,75 | `4d24ee8a` | `4d24ee8a` **igual** |
| carbonera 20×20 boca 1 | 9 000 | 1,60 | `69e65c00` | `69e65c00` **igual** |

R143 no tocó `Sim/`, y los hashes lo confirman. **Pero** la revisión adversaria encontró que
`LabBench.Correr` no aplica `Universe.AplicarOverridesLaboratorio`: los dos coincidimos porque los
dos medimos el universo de la campaña (vapor que condensa a ≈ 60 °C, humo de 200, agua con densidad
sorteada). Los ocho hashes quedan invalidados como licencia para optimizar hasta que el banco
aplique los overrides y se regenere la tabla (R23-1).

## 2. `LabLuz` acotada: correcta por lectura

Cada paso horizontal descuenta al menos `dMin` = min(decayAire, decayAgua, decayPlanta, decayHumo),
todos con mínimo 1 en el registro; el decaimiento del cielo (que puede ser 0) solo actúa en vertical,
dentro de las mismas columnas; el reset a 0 recorre la grilla entera antes de propagar, así que
cuando la ventana se encoge no queda luz vieja fuera; los sólidos descuentan `dAire ≥ dMin`. Una
celda a k columnas de cualquier fuente tiene luz ≤ 255 − k·dMin: fuera de la ventana es 0 también
sin acotar. La comparación celda a celda de Opus debe repetirse con los overrides (§1), pero el
argumento no depende de ellos.

## 3. Revisión adversaria (27 hallazgos, 0 refutados)

Detalle y correcciones en `PREGUNTAS_A_FABLE.md` R23. Las dos altas: el banco sin overrides (§1) y
el vidrio verde en `EsSolidoDelMundo`, que rompe la campaña (el frasco deja de aspirarlo; los
encargos que lo piden se vuelven imposibles) — instrucción mía de R19-7, revertida en R23-2.
