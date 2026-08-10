# Alkahest — Notas del núcleo de simulación (M1)

Autómata celular de "arena cayendo" (falling-sand) determinista, orientado a
datos y pensado para variación de leyes por seed en el futuro.

## Arquitectura

```
Assets/Alkahest/
  Sim/
    MaterialDef.cs      -> datos puros de un material (clase, no ScriptableObject)
    Universe.cs          -> fábrica estática: roster de materiales + ids estables (MaterialId)
    CellGrid.cs           -> almacenamiento flat (SoA) + activity tracking por chunks
    XorShift.cs           -> RNG determinista de la simulación
    SimStepper.cs         -> reglas por arquetipo, difusión de calor, transiciones de fase
    SimRenderer.cs        -> CellGrid -> Texture2D -> quad en espacio de mundo
    SimLevelBuilder.cs    -> nivel de pruebas M1 (borde, suelo, 3 cubetas, 2 estantes)
  AlkahestSim.cs           -> orquestador MonoBehaviour (acumulador 30Hz, API de pintar/samplear)
  Dev/DevPalette.cs        -> overlay IMGUI de desarrollo
  Editor/AlkahestSceneBuilder.cs -> genera la escena AlkahestLab
```

Todo el resto del juego debería leer materiales a través de `Universe`
(`sim.Universe.Get(id)` o `sim.Universe.Materials`), nunca hardcodear
colores/densidades en otro sitio. El día que queramos variación de leyes
por partida (ej. "en este run el agua es más viscosa"), el único archivo
que debería tocarse es `Universe.cs`.

## Materiales (roster fijo, ids estables)

`MaterialId` define los ids: Empty(0), Stone(1), Sand(2), Water(3), Oil(4),
Slime(5), Steam(6), Smoke(7), Fire(8), Ash(9), Ice(10), Nutrient(11),
Vivium(12). Estos valores NO deben cambiar de orden entre versiones.

Cada `Universe.Create(seed)` aplica un pequeño jitter de tono (±8°) a los
colores base usando un `System.Random(seed)` **local, usado una única vez**
en la creación — nunca durante el tick a tick. Los valores de juego
(densidad, fluidez, temperaturas de transición...) son hoy iguales para
todas las seeds.

## Reglas por arquetipo (SimStepper)

- **Powder** (Sand, Ash, Nutrient): cae recto si abajo hay Empty/Gas; se
  hunde en un líquido menos denso con 60% de probabilidad por tick (para
  que el asentamiento se vea natural en vez de instantáneo); si no puede
  caer recto prueba diagonal abajo-izq/abajo-der en orden aleatorio
  (semilla determinista `(tick,x,y)`); con `fluidity > 2` puede deslizar
  lateralmente sobre un líquido con 15% de probabilidad.
- **Liquid** (Water, Oil, Slime): igual que powder para caída/diagonales,
  pero el intercambio de densidad con un líquido más ligero debajo es
  **incondicional** (así el aceite siempre acaba flotando sobre el agua,
  no es un proceso probabilístico). Si no puede caer, fluye
  horizontalmente hasta `fluidity` celdas en la dirección preferida,
  memorizada en 1 bit de `aux` para no "tiritar" cambiando de lado cada
  tick; si la dirección preferida está bloqueada, prueba la contraria y
  actualiza la memoria.
- **Gas** (Steam, Smoke): gravedad invertida (sube), puede desplazar a un
  gas más denso por encima, diagonales hacia arriba, y un 35% de
  probabilidad de deambular lateralmente (da sensación de corriente). Vida
  media en `aux` (con jitter ±4 al nacer para no sincronizar expiraciones);
  al expirar se convierte en Empty, salvo que su temperatura ya esté por
  debajo de `condensesAt`, en cuyo caso condensa (Steam -> Water).
- **Fire**: si toca Water en ortogonal, se apaga al instante y se
  convierte en Steam en su propia celda. Si no, consume vida (`aux`) igual
  que un gas; al expirar se convierte en Smoke, o en Ash con 25% de
  probabilidad si hay algo sólido debajo. Cada tick fija su propia
  temperatura al máximo representable e inyecta +40 de calor a sus 4
  vecinos ortogonales; intenta encender vecinos inflamables (por contacto
  directo con 30% de probabilidad, o automáticamente si su temperatura ya
  supera `ignitionTemp`); tiene 50% de probabilidad de subir una celda si
  hay Empty encima.
- **Organic (Vivium)**: cae recto (sin diagonales: se comporta "pegajoso",
  no se abre en abanico como la arena) mientras no tenga soporte debajo.
  En cuanto encuentra algo debajo que no sea Empty/Gas, se marca con el
  bit `0x80` de `aux` como **asentado** y ya nunca vuelve a moverse (ni
  aunque el soporte desaparezca después). El método `GrowthTick(...)` es
  un hook vacío marcado con `TODO` para la lógica de crecimiento futura
  (expansión sobre Nutrient, generación de recursos, etc.).
- **StaticSolid** (Stone, Ice): nunca se mueve. Ice inyecta -2 de frío a
  sus 4 vecinos cada tick en el que está despierta.

### Transiciones de fase

Genéricas para cualquier arquetipo, evaluadas **antes** de la regla de
movimiento de cada celda, comparando su temperatura actual contra los
umbrales del `MaterialDef` (`meltsAt`, `freezesAt`, `boilsAt`,
`condensesAt`). Los sentinels `short.MinValue`/`short.MaxValue` significan
"esta transición nunca ocurre" — están tan lejos del rango real de
temperatura (0..255 raw) que la comparación nunca se cumple.

Ejemplo con los valores actuales: el agua hierve a 100°C -> Steam, y
Steam se condensa de vuelta a Water en cuanto su temperatura baja de 60°C
(lo cual ocurre solo por difusión hacia el ambiente de 20°C si no toca
nada más, o mucho más rápido si toca hielo, que enfría activamente sus
vecinos).

## Modelo de temperatura

Se guarda como `byte` "raw" 0..255 por celda; conversión con
`CellGrid.RawToC`/`CToRaw`: `C = raw*2 - 120` (rango representable
aproximado -120°C a 390°C). Ambiente = raw 70 (20°C).

Difusión barata: cada tick solo se procesa 1/8 de las celdas
(`offset = tick % 8`, paso 8), promediando con los 4 vecinos ortogonales
en aritmética entera (todo `>>`/enteros, sin floats) y atrayendo
suavemente hacia el ambiente cada ~32 ticks. En 8 ticks (~0.27s a 30Hz)
toda la grilla se ha refrescado una vez. El fuego y el hielo inyectan
calor/frío directamente a sus vecinos **cada tick** (no sujeto al
muestreo de 1/8), para que enciendan/enfríen con la reactividad esperada.

## Determinismo

- La simulación nunca usa `UnityEngine.Random`; toda la aleatoriedad pasa
  por `XorShift`, sembrado determinísticamente a partir de
  `(tick, x, y, sal)` — nunca se guarda estado de RNG persistente por
  celda.
- El único uso de `System.Random` en todo el sistema es en
  `Universe.Create`, una sola vez, para el jitter de color inicial; no
  afecta al comportamiento de juego ni se usa tick a tick.
- Orden de barrido: fila por fila de abajo (`y=0`) hacia arriba (evita que
  una celda caiga varias filas en el mismo tick — por eso "de abajo a
  arriba" resuelve la gravedad de forma natural), alternando la dirección
  de recorrido en X según la paridad del tick para no sesgar las
  diagonales hacia un lado.
- Guard `touchedTick`: cada celda procesada se marca con el tick actual
  antes de aplicarle su regla; si un swap la mueve a una posición que el
  barrido visitará más tarde en el mismo tick, esa marca evita que se
  procese dos veces (cae/sube como máximo una celda por tick).
- **Aviso**: la coordinación entre chunks despiertos (qué chunk exactamente
  entra en el barrido según su `sleepTimer`) depende del historial exacto
  de inputs de pintado del jugador (vía `AlkahestSim.Paint`), así que dos
  partidas con la misma seed solo serán bit-a-bit idénticas si además
  reciben exactamente la misma secuencia de inputs de pintado en el mismo
  tick — lo cual es el comportamiento esperado (replay determinista de
  seed+inputs, no solo de seed).

## Estrategia de rendimiento

- Todo el estado es SoA en arrays de `byte`/`uint` planos, indexados
  `idx = y*W + x`. Cero colecciones, cero LINQ, cero allocations en el
  hot-path de `SimStepper`/`SimRenderer` (los únicos `new` de por-tick son
  structs `XorShift`, que no reservan en el heap).
- Activity tracking por chunks de 16x16: un chunk sin cambios durante 30
  ticks se considera dormido y el barrido lo salta por completo
  (`CellGrid.IsChunkAwake`). Cualquier mutación despierta el chunk
  afectado **y sus 8 vecinos** (`CellGrid.WakeChunk`), para que las
  reacciones que cruzan el borde de un chunk no se pierdan.
- `SimRenderer` solo vuelve a escribir en la `Texture2D` los chunks
  despiertos cada frame (con `SetPixels32` por rectángulo), más un
  refresco completo cada 30 frames como red de seguridad.
- `SimStepper.LastStepMs` usa un `Stopwatch` para medir el coste real del
  último tick; se expone en el overlay de dev (F3).

## Cómo extender

- **Nuevo material**: añadir su id a `MaterialId`, su entrada en
  `Universe.Create`, y (si necesita comportamiento nuevo, no solo
  parámetros) una rama en el `switch` de `SimStepper.ProcessIfNeeded` o
  ajustar la regla de arquetipo existente que más se le parezca.
- **Nueva reacción de fase**: normalmente basta con rellenar los campos
  `meltsAt/meltsInto`, etc. del `MaterialDef` — `ApplyPhase` ya los evalúa
  genéricamente para cualquier material.
- **Nueva ley por seed**: variar valores dentro de `Universe.Create` en
  función de `seed`/`rng` en vez de escribirlos fijos (hoy son fijos a
  propósito para mantener M1 predecible mientras se itera el resto).

## Límites conocidos (M1)

- `Vivium` no tiene todavía lógica de crecimiento (`GrowthTick` es un
  hook vacío).
- El flujo horizontal de líquidos (`TryFlow`) se detiene en el primer
  vecino no vacío que no sea gas; no reptan por encima de obstáculos ni
  se derraman por los lados de una celda ocupada en diagonal.
- No hay persistencia/serialización de la grilla todavía (fuera del
  alcance de M1).
- El material `Empty` no participa en difusión de temperatura por
  simplicidad (su temperatura no se re-consulta hasta que se ocupa con
  otro material, momento en el que ya arrancó en el ambiente por defecto
  del array `temp`, que se inicializa entero a 20°C).
