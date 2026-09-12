# Refutación de ingeniería · `insolacion` (Lo que la luz pierde, lo gana en calor)

**Veredicto: VIABLE CON AJUSTE.** La regla es acotada, sin RNG, cabe en el presupuesto y se mide
en el banco. Lo que no se sostiene es la ESCALERA: con los números del documento, en el nivel
real dos soles no prenden la yesca y cuatro no vidrian. El arreglo es un parámetro que ya existe.

## Lo que el código confirma

- Umbrales exactos: fibra `CToRaw(140)` = 130 raw, planta 140, carbón 200
  (`Universe.Laboratorio.cs:70,83,159`); agua hierve a 110 (`:212`); arcilla 150 y vidrio
  200/60 visitas (`LabParams.cs:91,105-106`). Cámara alta a 64 raw (`SimLevelBuilder.Laboratorio.cs:177`).
- `LabCalentarHasta` (`SimStepper.Laboratorio.cs:950-966`) es el escritor correcto: tope, clamp y
  `WakeChunk` (R138 A2). Basta sacar `LabRawHogar += nuevo - t` (`:960`) a un `ref long` para
  tener `LabRawSol`; la identidad «= Σ raw escritos» sale por construcción, como en R142.
- Determinismo intacto: orden fijo por columna y por índice, ningún `XorShift`. Coste declarado
  correcto. Los hashes se mueven solo en los escenarios con boca; el banco compara entre corridas,
  no contra oro (`LabBench.cs:17,326-331`): «se apunta» es lo normal.

## Lo que se refuta

1. **La escalera usa rayos de 255 y ningún rayo llega con 255.** Con el haz del candidato 1
   (aire −1/celda, giro −8), el rayo directo baja de y=286 a la cara del lecho (y=249): 36 celdas,
   P = 219. El segundo, girado a la misma fila, llega con ~210. Suma 429 → tope = 64 + 36·429/255
   = **124 raw: la fibra (130) NO prende con dos soles**. Cuatro rayos (el de abajo paga dos giros
   y una celda de vidrio) suman ~830 → **181: no hay vidrio**. Con `LuzSolRaw` = 43 el suelo
   cuadra, pero una plataforma a 5 celdas de la boca da 147 con dos soles: la planta (140) arde y
   el peldaño «yesca sí, planta no» (ventana de 10 raw) se mueve ±10 con la altura de la obra.
   Geometría que hay que balancear: lo que la función objetivo penaliza.
2. **`_labCola` no es reutilizable.** Se asigna perezosamente dentro de `LabPresion` (`:1085`):
   con `presion.activa = 0` (`LabParams.cs:117`) el primer `LabLuz` lee `null`; y `LabCuerpos`
   (H6, `:1295`) ya tiene reservado ese búfer. Búfer propio.
3. **La versión de 3 días (solo vertical) es contraproducente.** El rayo directo cae en las 7
   columnas de sedimento x118-124, y249: las ÚNICAS 7 de 73 caras con luz ≥ 40. Sin espejos
   convierte el único punto germinable en secadero (`LabSecarHacia:734`, t ≈ 24 → ×2,5). El
   valor de la ley depende entero del candidato 1.

## La versión mínima viable

- **Haz sin pérdida en aire**: `LuzDecayCielo = 0` (el slider ya admite 0, `LabParams.cs:291`)
  como decaimiento del rayo; paga solo giros (8) y medios (agua 20, vidrio 4, hielo 6). Con
  `LuzSolRaw = 36` la escalera es exacta e independiente de la profundidad: 100 / 135 / 170 / 203.
  La cota de bucles pasa de «255 pasos» a «31 giros»: tope duro de 2 048 pasos por rayo.
- **Base fija, no `ambient[i]`**: `tope = LuzSolBase (64) + LuzSolRaw·sol/255`. Con ambiente,
  el peldaño 2 da 141 en la sala del hogar (70 raw) y quema la planta.
- **Calor en `LabCampos`, no en `LabLuz`**: `LabLuz` deja `sol[i]` (int[] propio, cada 16 ticks);
  `LabCampos` (`:201`), antes del `switch`, hace `if (sol[i] > 0) LabCalentarHasta(...)`. Misma
  cadencia que el hogar, y la arena llega a su comprobación de vidrio (`:597`) con la temperatura
  recién escrita (`reposo` se reinicia si cae de 200; cada 16 ticks el peldaño 4 era marginal).
- Al cargar snapshot o espejo, forzar `LabLuz` antes del primer tick: `sol` es posicional, como `luz`.

## Benchmark

`lupa de espejos` como en el documento, pero **a dos alturas** (lecho y=249 y plataforma a 5
celdas de la boca): los mismos cuatro resultados en ambas prueban que la escalera es acotada. En
`alambique de r141`, además de `Goteos`/`HumedadMediaLecho`/`SustratoPct`, temperatura media del
aire de la cámara alta antes y después: el tirón a ambiente de 1 raw/32 ticks (`:1339`) acota la
deriva a decenas de celdas, menos que el hogar del nivel, pero se mide.

## Corrección de valores

Apalancamiento 7: un hogar sin humo que se apunta con geometría. Tuning 7 con el ajuste; sin él,
balance por geometría. Coste 1,5 semanas sobre el candidato 1; 3 si el 1 no entra.
