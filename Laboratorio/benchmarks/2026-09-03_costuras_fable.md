# Medidas de las costuras (Fable, 2026-09-03, editor de Cesar, Play mode, Unity 6000.5.7f1)

Escenario: laboratorio recién construido (BuildLaboratorioDeLeyes), manantial a 24 celdas/s,
poza prellenada, StepOnce en bucle desde RunCommand (incluye RenderFrame fuera de LastStepMs).

| Medida | Valor |
|---|---|
| Media del tick (300 ticks, 87 chunks despiertos, ~13k celdas activas) | 2,34 ms (pico 9,0 ms) |
| difusión térmica del laboratorio (LabDifusionTermica, 1/8 grilla) | 0,67 ms |
| barrido de chunks despiertos (con salto de filas dormidas) | 0,55 ms |
| morph | 0,19 ms |
| **LabCampos** (1/8 de TODA la grilla) | **0,15 ms** |
| **LabPresion** (media por tick; corre cada 2 ticks) | **0,09 ms** |
| **LabLuz** (media por tick; corría cada 8 ticks) | **0,68 ms → ~5,4 ms por ejecución = el pico** |
| 3000 ticks headless desde RunCommand (con render por tick) | 7,4 s reales = 406 ticks/s ≈ 13,5× tiempo real |
| Tubo en U (2 columnas de 8 de ancho, 1904 celdas de agua) | niveles 237/199 → 219/217 en 240 ticks; agua conservada exacta; 150 celdas movidas por presión |
| Caudal medido del manantial (cara libre de 7 celdas) | 20,4 celdas/s (pedido 24) |

Libro mayor tras 3565 ticks (119 s de mundo): emitida 2398 · sumida 0 (el arroyo entero se
cuela por la fisura de arena, ver abajo) · evaporado 26556 u (104 celdas) · condensado 0
(la zona fría aún no recibe vapor) · infiltrado 34473 u (135 celdas) · depositado 1274 ·
erosionado 969 · presión movió 2169 celdas · compactado 0.

Hallazgos:
1. LabLuz es el cuello de botella: 4 barridos sobre 221k celdas. Acotar al área del
   laboratorio (x ≤ 440) y/o solo filas con aire, o incremental. Default subido a cada 16 ticks.
2. La fisura de arena (x192-202, y111-134) es POLVO: sin roca debajo cae a la cámara profunda
   (delta + laguna). Para el filtro previsto hace falta un poroso ESTÁTICO (propuesta: material
   `Arenisca`, StaticSolid permeable ~30, tallable → Sand). Mientras tanto el fenómeno emergente
   ("el arroyo se pierde por la fisura y aparece abajo") es válido y observable.
3. Churn erosión↔depósito en el lecho cerca del manantial (969 vs 1274): neto ~305 celdas de
   sedimento nuevo de 2398 emitidas × 40/255 ≈ 376 esperadas. Coherente con la conservación.
4. El multiplicador 100× no es alcanzable con LabLuz a 5 ms; sin luz, ~2 ms/tick → ~15-20×
   dentro de 33 ms de frame. Medir con LabBench antes de decidir el techo.
