# Refutación · El sello (registro, volcado, veredicto) — lente apalancamiento

**Veredicto: viable con ajuste.** Es la pieza más barata y verificable del panel, pero no es una ley: es infraestructura de juicio con cero cruces físicos, y la mitad que juzga (la `Condicion` con umbrales) esconde autoría que el candidato declara como cero.

## 1. La premisa de código es falsa, aunque reparable

«`PaintLab` (831) es el único camino de las ediciones del jugador.» No: el cincel vacía con `_sim.Paint` (Cincel.cs:469) y rellena con `PaintStable` (:551); el frasco vierte con `Paint`/`PaintCell` (Flask.cs:614, 687); el pincel del panel borra con `Paint` (LabPanel.cs:224) y pinta con `PaintStable`/`PaintCell` (:247); las placas usan `InyectarTemperatura`. La cola va bajo las seis puertas de `AlkahestSim`, con byte de tipo y radio. Un día.

La prueba (a) tiene una trampa de un tick: la caldera despierta el chunk con `(uint)t` (LabBench.cs:311) y `PaintLab` con `TickActual` = t−1; esa diferencia en `chunkTouchedTick` decide si `TickChunkIdle` corre (SimStepper.cs:311-314). La cola solo es transparente si se aplica tras `_tick++`.

## 2. «Al tick, no al frame» no compra nada

`Update` corre N `Step()` enteros y las escrituras del juego caen entre ticks (AlkahestSim.cs:376-384): ya son tick-estampadas de hecho. Más barato de lo declarado, no más correcto.

## 3. Intervenciones que el struct no codifica

Los sliders de F8 escriben parámetros en vivo (`p.Escribir(nv)`, LabPanel.cs:631) y cargan presets (:484). Un slider en el tick 3000 no cabe en el struct y el rejugado diverge en silencio. Ajuste: el preset es parte del montaje y queda congelado tras el sello. Igual la boca de cielo: `LuzCieloX0/X1` no están en `Registro` (LabParams.cs:131; las fija SimLevelBuilder.Laboratorio.cs:164) y un volcado sin ellas pierde la luz.

## 4. Estado escondido, y uno que es físico

Fuera de `CellGrid` hay `_tick`, `_labPase`, `_labManantialCeldas`, 19 contadores `long` del libro y, no listado, `_zonaInteresChunk`, que sube el muestreo de reacciones de 1/8 a 1/2 (SimStepper.cs:428). Lo escribe el builder: `Cargar` sobre grilla desnuda da otro hash. La prueba (b) lo caza; el ajuste es que el sello sea (semilla, montaje por builder, preset, registro) y el volcado solo acelere.

## 5. Determinismo: el riesgo está mal ubicado

El stepper no tiene un solo `float` ni RNG con estado; `Universe.Create` usa Clamp/Lerp/RoundToInt/Repeat, aritmética IEEE sin trascendentes; `density` es `short`. Riesgo bajo, medible en un día: `LabBench` en la build IL2CPP contra los 63 hashes del editor.

El riesgo grande es el cuerpo. Hoy el aprendiz es fantasma (cero menciones en Sim/) y el registro es completo. La función objetivo quiere un cuerpo que reaccione al calor, al humo y a las corrientes; en cuanto TOQUE la grilla, su Transform flotante tiene que volverse intervención tick-estampada o el sello muere. El candidato no lo dice.

## 6. Dónde se esconde el tuning

Cola, volcado, rejugado, hashes y diff: tuning cero, cierto. La `Condicion` no: «(métrica, comparador, umbral, por día)» es un número de autor por situación, lo mismo que `MedirLecho` en código, y R148/R150 enseñan lo frágiles que son (humedad 50-99 contra mínimo 60). Pasarlo a JSON no lo hace gratis: sin validador, cada umbral es una tarde de Cesar. Y todo registro caduca con cada cambio de física (21 rondas en 3 días): el fichero necesita hash de versión para rechazar en vez de divergir.

## 7. Apalancamiento

Cruces de leyes físicas: cero, por construcción. Decisiones nuevas de verdad: cuándo soltar (solo si algo premia los días sin manos, es decir, con condición) y bifurcar el registro ajeno. «Qué dejar corriendo» y «cuántos toques» ya existían; ahora se miden. El valor es de infraestructura: balanza, testigo y cuna no se juzgan en banco sin esto. Es la capa que la función objetivo pide, no una ley: 7.

## Versión mínima con más apalancamiento

Cola bajo las seis puertas + registro; volcado con la lista completa de estado y el round-trip como prueba; `CorrerSello` que devuelve el recibo por día (vector del libro), hashes y «ticks desde el último toque» como hecho, no juicio; comparación entre sellos por dominancia métrica a métrica; hash de versión de física en el fichero. Los umbrales entran con el validador de la cuna. Cuatro semanas: tres de Sim/ y una del gesto mínimo visible (soltar, contador, diff).
