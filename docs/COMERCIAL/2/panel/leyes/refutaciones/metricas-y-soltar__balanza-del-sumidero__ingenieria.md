# REFUTACIÓN DE INGENIERÍA · «La balanza: el sumidero pesa lo que traga»

*Candidato `balanza-del-sumidero`. Leído: `SimStepper.Laboratorio.cs` (`LabAgua` :430-495,
`LabTragar` :1026-1033), `SimStepper.cs` (`ProcessPowder` :1085-1130), `CellGrid.cs`,
`LabBench.cs` (`Correr` :265-335), `Universe.Laboratorio.cs`, `SimLevelBuilder.Laboratorio.cs`,
`AlkahestSim.PaintLab`.*

## Veredicto: VIABLE CON AJUSTE

## Lo que el código confirma

La ley es tan barata como dice. `LabTragar(j)` ya lee `_universe.Get(m).archetype`; añadir
`|| Powder` no cuesta una lectura más, no usa RNG y `LabTransformar` ya despierta el chunk. El
sumidero es `StaticSolid` con `caeSolido = false`: el barrido no lo mueve, `LabPresion` solo mueve
agua y `LabCuerpos` solo `RocaSuelta`; nunca pasa por `SwapCells` y su `aux` queda libre. Los
contadores `long[salida, MaterialId.Count]` pesan 10 KB. El polvo desliza en diagonal a vacío sin
depender de `fluidity` (SimStepper.cs:1096-1110): un canal hacia una salida vacía una pila por
gravedad, la «tolva» ya existe.

## Lo que el código refuta

1. **El benchmark insignia se come su materia prima.** La fibra es `Powder`
   (Universe.Laboratorio.cs:77) y el sumidero traga `i + W` (el vecino de ARRIBA) cada visita, sin
   tope. En «la 20×20 con suelo de sumidero» la fila inferior (20 celdas) desaparece en la primera
   visita, la de encima cae y desaparece en la siguiente: 400 celdas en ~160 ticks (5 s), cuando una
   celda de fibra tarda 11 s en arder. Resultado: `Sumido[Fibra] ≈ 400, Carbon = 0`. La «carbonera
   con el suelo inclinado hacia la salida» entrega el 100 % de la fibra sin arder. No es un fallo de
   la ley sino su consecuencia real: un sumidero universal es un AGUJERO, y no se construye encima.
2. **«Los siete hashes no se mueven» solo con id 0.** `Correr` hashea `g.aux` (LabBench.cs:326):
   escribir `idSalida ≠ 0` en el sumidero de referencia mueve `HashAux` de tres escenarios.
3. **`Σ Sumido[Water] == LabAguaSumidaU` es tautológico**: misma línea de código. No detecta nada.
4. **«Clara ≤ 16» no tiene base.** La decantación (:459-465) TRANSFIERE carga al agua de abajo, no
   la destruye: el fondo, que es lo que el sumidero come, se concentra. Nadie ha medido qué carga
   llega hoy al sumidero; 16 es un número de balance disfrazado de definición.
5. **La mitigación del riesgo mayor no está en el código.** `PaintLab` escribe cualquier byte en
   cualquier celda: el jugador puede pintar sumideros y pintar sobre ellos (`SetCell` resetea `aux`
   y la salida pierde su nombre). «Nunca lo coloca el jugador» es una frase, no una guarda.

Nivel de referencia: el sumidero (416-429, 96-99) está encerrado en roca, la grava más cercana en
x 336-372; el agua sobre él vive ≤ 8 ticks y no alcanza `DepositoReposo = 24` visitas. Con id 0,
los siete hashes de los tres escenarios deben quedar intactos en 72 000 ticks de arco largo: ese es
el test de regresión real.

## Versión mínima viable

- `LabTragar`: `Liquid || Powder`. Contadores `long[salida, MaterialId.Count]` indexados por el
  `aux[i]` del sumidero (0 = sin nombre; el nivel de referencia no escribe `aux`).
- En vez de `AguaClaraCargaMax`, un histograma `long[salida, 16]` de `carga` al tragar: cero
  parámetros; la raya «clara» se dibuja en el visor. `LabSumidoCalorRaw += temp[j] −
  CellGrid.AmbientRaw` (const 70).
- `PaintLab` rechaza `Sumidero/Manantial/Hogar/NucleoFrio` como pincel y como destino.
- Volcado por salida en `EscribirLibro` e `Informe()`.

**Banco.** (a) Tres escenarios del nivel de referencia: siete hashes idénticos. (b) Partición:
`Σ histograma == LabAguaSumida` y `Σ bins·u == LabAguaSumidaU`. (c) «Carbonera con salida»:
`MontarCarbonera` con un tapón de roca de una celda en la esquina del suelo sobre un canal hacia un
bloque de sumidero; `Correr` gana una intervención tick-estampada genérica (la de la caldera, hoy
cableada a `esAlambique`) que quita el tapón en el tick T. T = 0 → `Sumido[Fibra] ≈ 400, Carbon =
0`; T = 6 000 (tras el plateau de `LabCarbonizado`) → `Sumido[Carbon] ≥ 0,8·LabCarbonizado,
Fibra = 0`. Dos corridas que discriminan «cuándo abrir el suelo»: la primera apuesta de SOLTAR con
juez.

## Valores corregidos

Coste 1,5 semanas (ley y libro, un día; salidas múltiples, guarda de `PaintLab`, intervención
genérica del banco y recibo en F8, el resto). Tuning 9: sin el umbral, el único número humano es qué
salida va dónde, y eso lo decide la cuna. Apalancamiento 7: la medida es la moneda del sello
(candidato 2) y sola vale poco; la física nueva real es «la salida es un agujero para lo granular»,
que compra tolvas, canales y el momento de abrir el suelo, pero presión, calor y turbidez ya hacían
lo que hacen: aquí solo se pesan.
