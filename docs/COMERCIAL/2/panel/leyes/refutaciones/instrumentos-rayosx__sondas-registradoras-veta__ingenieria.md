# Refutación de ingeniería · sondas registradoras (veta)

*Lente instrumentos-rayosx, candidato 4. Leído: `SimStepper.Laboratorio.cs` (134-155, 201-235), `SimStepper.cs` (257-338), `LabBench.cs` (25-75, 265-455, 469-523), `CellGrid.cs` (170-280), `LabPresets.cs` (85-260), `Net/SimSync.cs` (30-51), `Game/Termometro.cs` (83-348).*

## Veredicto: VIABLE CON AJUSTE

## Lo que aguanta

**Encaje y coste.** `LabPasadas()` (SimStepper.Laboratorio.cs:134) es una secuencia de cuatro pasadas cronometradas con `_swFase`; un `LabRegistro()` al final, tras `LabLuz`, cae en la misma estructura sin tocar una línea de física. El registro solo LEE `_grid.temp/humedad/carga/reposo/luz` y escribe en sus propios anillos, así que los siete hashes de `Correr` (LabBench.cs:326-331) no pueden moverse: es la misma garantía «7 iguales, octavo nuevo» que ya usa el candidato de la huella. Determinista por construcción (función del estado y de `_tick`), hasheable con el mismo `Hash(byte[])` FNV-1a. Ocho lecturas cada 8 ticks contra un tick de 1,6-1,9 ms: el «despreciable» es cierto. Presupuesto y determinismo: sin objeción.

**Cadencia.** Anillo A a 8 ticks es la cadencia de visita de `LabCampos` (offset `_tick % 8`, línea 205); anillo B a 256 ticks coincide con el recuento de fuentes (`_tick & 63` no; el de 256 es el de `LabContarFuentes` según su docblock) y con la tolva del banco: 14 000 ticks = 466 s, exactamente los 54 muestras de B que promete la verificación. Los números están bien.

## Lo que no aguanta tal como está escrito

**1. «`MedirLecho` y `ArcoMuestra` se reescriben como sondas» es falso con la sonda definida.** Las dos son agregados de REGIÓN (LabBench.cs:353-373 y 421-455): 36 columnas × 4-7 filas, «columnas con agua encima / 36», «% de sedimento ≥ `PlantaHumedadMin`», media de humedad. Una sonda `(x, y, campo)` es un punto: ocho puntos no reproducen «22/36 y 0/36» de r148/r150, que es la aserción central de la verificación headless. O la sonda es `(rect, campo, reductor)` con reductor ∈ {valor, media, máximo, cuenta ≥ umbral}, o `MedirLecho` se queda como está y la veta solo añade la dimensión temporal. Un rect de 36×7 cada 8 ticks sigue siendo trivial (≈250 lecturas), así que la primera opción no cuesta nada y es la correcta.

**2. «La regresión pasa a decir CUÁNDO divergieron» no lo dan las sondas.** El banco hoy hashea solo el estado final y no compara contra baseline (`LabBenchMenu.cs` escribe el informe; el diff lo hace una persona). Ocho puntos detectan la divergencia solo si pasa por ellos. El instrumento correcto para «cuándo» es un anillo de hashes de rejilla COMPLETA a la cadencia B (FNV sobre `mat+temp+humedad+carga` ≈ 0,9 MB cada 256 ticks ≈ 1 ms, 35 veces en 9 000 ticks): una línea en el bucle de `Correr` y una columna «primera muestra que difiere». Las vetas dicen DÓNDE y QUÉ; el anillo de hashes dice CUÁNDO. Son dos cosas y el candidato las funde.

**3. «Va en el fichero de partida (4 KB)»: ese fichero no existe.** El único IO del laboratorio es PNG + preset JSON + `_libro.json` (`LabPresets.GuardarSnapshot`, 222-260). No hay guardado ni carga de la rejilla. El cruce «asíncrono por fichero: comparar dos grupos» y «retar a otro con mi veta sin abrir el horno» descansan sobre un mundo guardable que no está en esta semana ni en este candidato. Lo honesto: volcar las vetas como texto en `_libro.json` y en `Resultado`, y dejar el reto por fichero para cuando exista el snapshot del mundo.

**4. Multijugador.** `SimSync` envía solo `mat[]` RLE (SimSync.cs:30, 51): el espejo no tiene `temp` ni `humedad` y no puede muestrear. Las vetas viajan del host como 8 bytes por sonda cada 8 ticks: barato, pero es una línea de protocolo que el candidato no cuenta.

**5. «La veta pintada en la pared junto a la sonda».** 256 muestras a un píxel por celda son 256 celdas: un tercio del mundo (768). En el mundo no cabe; va en el HUD junto al pin de `Termometro` (que ya existe: pin, etiqueta, FIFO), con agregado 4:1 por máximo si se quiere en pared.

## Versión mínima viable

`LabRegistro` en la sim: 8 sondas `(rect, campo, reductor)`, dos anillos de 256 bytes, muestreo al final de `LabPasadas`; `Termometro` pasa de 3 sondas locales a plantar en la sim como hace el pincel (entrada de tick, host autoritativo). En `Correr`: sondas declaradas por escenario en el `Montaje`, vetas en `Resultado` como texto de 8 bandas, `HashVetas` como octavo hash, y ANILLO DE HASHES DE REJILLA cada 256 ticks con columna «primera muestra distinta». Volcado en `_libro.json`. Tira en HUD. Sin reto por fichero.

## Benchmark que lo prueba

(a) Siete hashes idénticos al informe actual en los nueve escenarios. (b) Tolva: la veta B de calor sobre la fibra a `(106, 201)` muestra banda ≥ 3 durante ≥ 54 muestras consecutivas y luego cae. (c) Alambique: veta B de humedad-media del rect del lecho `(100-135, 246-249)` reproduce `HumedadMediaLecho` en su última muestra (igualdad exacta). (d) Divergencia sintética: correr el alambique dos veces con un parámetro cambiado en el tick 4 000 desde el `Montaje`; el anillo de hashes debe señalar la primera muestra distinta en 15-16 y nunca antes.

## Valores corregidos

Apalancamiento 6 (instrumento, no ley: no crea decisión física nueva, pero es el canal de SOLTAR y de todo lo que el candidato 3 solo muestra en presente). Tuning 9 (bandas de `LabParams`; paleta y tamaño de tira). Coste 1,5 semanas: sonda con rect y reductor, entrada de sim, anillo de hashes, HUD, protocolo.
