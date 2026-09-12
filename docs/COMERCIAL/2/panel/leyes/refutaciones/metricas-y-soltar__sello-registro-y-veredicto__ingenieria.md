# REFUTACIÓN DE INGENIERÍA · El sello: registro, volcado y veredicto rejugable

**Veredicto: viable con ajuste.** Apalancamiento 8 · tuning 8 · coste 2,5 semanas.

Leído: `SimStepper.Step()` (SimStepper.cs:257-330), `SimStepper.Laboratorio.cs` en sus tramos de
estado, `CellGrid.cs`, `LabBench.Correr`, `AlkahestSim` (puertas de pintura 560-840 y bucle de pasos
358-400), `Cincel.TallarTick`, `Flask` (614, 687), `LabPanel` (224-247, 631), `SimSync` (155-164),
`SpawnLaboratorio`, `Universe.Laboratorio.cs`.

## Por qué no se refuta

La física del laboratorio es **entera de punta a punta**: cero `float` en el partial;
`LabDifusionTermica` sustituye a `DiffuseTemperature`; los dobles de `EmisionTermica` (NewtonK,
SimStepper.cs:2968) sirven a las placas heredadas, que `SpawnLaboratorio` no crea. El azar es
`XorShift.FromCell(tick,x,y,sal)`, sin estado. La semilla es fija e idéntica en juego y banco
(`RestartRun(Universe.SemillaCero)` = 777002 = `LabBench.SeedLaboratorio`). Sin Crisol en el
laboratorio, `_zonaInteresChunk` (que altera el muestreo de `MaybeReact`) es todo falso en las dos
vías. `LabCuerpos` es un gancho vacío. Fuera de `CellGrid` el stepper solo guarda `_tick`, el libro y
**`_labManantialCeldas`** (recontado cada 64 ticks, línea 203): el único estado escondido real, y
son 4 bytes. `touchedTick`, `_labVisita/_labCola/_labSup`, `_labPase`, `morphScratch`, `_events` y
`_ultimoTickPorLey` son transitorios o no físicos: no hace falta volcarlos. Coste por tick: cero.
La prueba (b) del round-trip caza todo lo que Opus olvide. Nada choca con una regla validada.

## Los cuatro errores del candidato (reparables)

1. **«`PaintLab` es el único camino» es falso.** En el laboratorio escriben la grilla cuatro
   puertas: `Paint` (cincel al borrar, Cincel.cs:470; frasco al aspirar, Flask.cs:614; panel 224),
   `PaintCell` (frasco al verter, Flask.cs:687; panel 245), `PaintStable` (panel 247) y `PaintLab`
   (cincel 473, panel 236). Un registro que solo capture `PaintLab` rejuega un cincel que pone
   sedimento sin quitar la roca. El gancho ya existe: `Paint/PaintCell/PaintStable/PaintRect`
   empiezan llamando a `ReenviarSiEspejo(x,y,radio,mat,modo,tempRaw)`, y `SimSync` ya serializa eso
   en 8 bytes con `ModoPaint..ModoPaintRect`. La entrada del sello es ese registro más `tick`,
   `humedad` y `carga`: 14 bytes, `ModoPaintLab = 4`.
2. **Diario, no cola.** Encolar y aplicar «al principio del Step» cambia las herramientas:
   `TallarTick` lee `Grid.temp/humedad` y `SampleMaterial` en el mismo frame tras sus propias
   escrituras (bucle de `budget`); el frasco lee lo que acaba de vaciar. Con una cola leen estado
   viejo. Ajuste: escribir en el frame como hoy **y** anotar. Es bit-idéntico porque entre dos
   `Step()` nadie más toca la grilla (`RenderFrame` solo escribe `patina`).
3. **El sello es «el último tick completado».** `WakeChunk` escribe `chunkTouchedTick`, y al
   final del paso `!= _tick` decide `TickChunkIdle` (SimStepper.cs:311-314): un sello desplazado un
   tick cambia cuándo duerme el chunk, y el sueño es física. El rejugado aplica las entradas con
   `tick == _tick` **antes** del `_tick++`, despertando con ese valor. La caldera del banco sella con
   `(uint)t` (el tick *siguiente*) y no escribe `temp` (LabBench.cs:296-301): la prueba (a) con
   entradas tipo `PaintLab` **no** reproduce los hashes. Migrarla al diario (1 125 entradas
   `ModoPaintRect` 7×1; `SetCell` ya pone `humedad=255` al agua) puede mover el hash una vez.
4. **Los sliders son intervenciones.** `LabPanel` escribe parámetros en caliente (`p.Escribir(nv)`,
   631) y `VaporVidaCambiado` remuta el `Universe`. Ajuste: segunda entrada (tick, índice de
   `Registro`, valor), diez líneas; o «tocar un slider rompe el sello». Prefiero la primera.

## Versión mínima y banco que la prueba

Diario en las cinco puertas de `AlkahestSim` (una línea junto a `ReenviarSiEspejo`) + entrada de
parámetro; `CellGrid.Volcar/Cargar` de los once arrays más `_tick`, libro, `_labManantialCeldas`,
preset y `LuzCieloX0/X1`, preservando el `uint.MaxValue` inicial de `chunkTouchedTick`;
`CorrerSello` con la `Condicion` como datos. Banco: (a') alambique con la caldera como diario →
hashes de referencia; (b) volcar en 4 500, cargar, correr 4 500 → los siete hashes de r141; (c) arco
largo con registro vacío → tabla de R148; y **(d), la única que prueba el diario**: dos minutos en
el editor a ×10 con cincel, frasco y panel, tecla «sellar», `CorrerSello` en banco → `HashMat`
igual al de la grilla viva al sellar. (a)-(c) jamás ejercitan las herramientas. Añadir al fichero un
hash de la tabla de materiales del `Universe`: la divergencia de `Mathf` en `Universe.Create` se
detecta al cargar, no tras 9 000 ticks.

## Valores corregidos

**Apalancamiento 8**: no añade una relación física; compra SOLTAR, apuesta, comparación,
multiplayer asíncrono y la prueba cross-machine con muy poco código, pero por sí solo no crea
ningún cruce emergente. **Tuning 8**: la `Condicion` por situación es autoría hasta que la cuna la
automatice. **Coste 2,5 semanas**: 1,5 de motor y banco (barato, verificable) + 1 de gesto,
contador y vista de diferencia, donde empieza la iteración humana.
