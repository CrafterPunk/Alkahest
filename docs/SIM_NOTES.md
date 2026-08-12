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

## M4 — Bucle de partida ("CHAOS ALCHEMY")

M4 añade una capa de juego (`Assets/Alkahest/Game/`) por encima de Sim/ sin
tocar el hot-path de simulación (aparte de dos cambios mínimos en
`AlkahestSim.cs`: `Paused` -- ya existía desde M3/M4 temprano -- y el nuevo
`static int? NextRunSeed`). Nombre visible del juego: **CHAOS ALCHEMY**
(los namespaces de código siguen siendo `Alkahest.*`; no se ha tocado
`ProjectSettings`).

### Archivos nuevos (Game/)

```
SubstanceKnowledge.cs -> qué sabe el aprendiz de cada material: descubierto,
                          nombre puesto por el jugador, transformaciones
                          presenciadas (lee el ring buffer de eventos de
                          SimStepper.Events/EventHead)
NamingUi.cs            -> ventana IMGUI (T) para "bautizar" un material
JournalHud.cs          -> ventana IMGUI (J) con la lista de descubiertos
Order.cs / OrderSystem.cs -> Favor + generación/seguimiento de encargos
DeliveryChute.cs        -> "Tolva del Maestro": consume material y lo
                            evalúa contra los encargos activos
OrdersHud.cs             -> HUD de Favor + encargos activos (arriba-dcha.)
DayCycle.cs               -> máquina de estados de la partida + overlays
```

### Máquina de estados de partida (DayCycle.cs)

```
Título -> DayIntro(día) -> Playing (cuenta atrás 6:00) -> DayEnd
            ^                                                |
            +----------- (si día < 3 y sin derrota) ---------+
                                     |
                              (día 3 o derrota)
                                     v
                                EndScreen
```

- Mientras la fase activa NO es `Playing`, `DayCycle` fuerza
  `AlkahestSim.Paused = true` y expone `DayCycle.InputLocked` (estático),
  consultado por Flask/Dispenser/HeatPlate/ChillStone/NamingUi para
  ignorar por completo el input del jugador durante los overlays.
- **Victoria**: `Favor >= OrderSystem.WinFavorTarget` (120) tras completar
  la jornada 3, sin derrota anticipada.
- **Derrota anticipada**: dos jornadas SEGUIDAS sin un solo encargo
  completado.
- **Cambiar de seed**: Título (campo de seed) y los botones de la pantalla
  final ("Reintentar mismo universo" / "Nuevo universo") fijan
  `AlkahestSim.NextRunSeed` y recargan la escena
  (`SceneManager.LoadScene(sceneActual)`); `AlkahestSim.Start()` consume esa
  seed una única vez. Un flag estático interno de `DayCycle`
  (`_skipTitleOnLoad`) hace que, tras ese reload, la partida entre
  directamente en `DayIntro(1)` en vez de volver a mostrar el Título.

### Encargos (Order/OrderSystem)

Favor inicial: 20. Tipos de encargo (`OrderType`), evaluados por
`OrderSystem.MatchesOrder` contra cada celda que cae en la Tolva:

| Tipo | Condición |
|---|---|
| `Flammable` | `MaterialDef.flammable` de la celda |
| `Grows` | `archetype == MaterialArchetype.Organic` (familia Vivium) |
| `CrystalSolid` | `matId == MaterialId.Crystal` |
| `Hot` | `RawToC(temp) >= MinTempC` |
| `Cold` | `RawToC(temp) <= MinTempC` (mismo campo que Hot, reinterpretado como techo) |
| `NamedMaterial` | `matId == TargetMat` (solo se genera si hay algo bautizado y descubierto) |

Encargos por jornada (deterministas: tipo/umbral/recompensa fijos; solo la
frase de sabor y, en el día 3, QUÉ material bautizado concreto pide
`NamedMaterial` dependen de `System.Random(seed*31+día)`, uso de capa de
juego, nunca en Sim/):

- **Día 1**: Flammable 60 (+25), Hot 80°C 80 celdas (+25).
- **Día 2**: Grows 120 (+35), Cold -5°C 60 celdas (+30), CrystalSolid 80 (+40).
- **Día 3**: CrystalSolid 200 (+50), NamedMaterial 100 (+45, o Flammable 150
  +45 si nadie ha bautizado nada todavía), Grows 250 (+55).

### Tolva del Maestro (DeliveryChute.cs)

Bolsillo de piedra fijo en celdas x∈[W-8,W-4], y∈[60,80] (pintado una única
vez con `AlkahestSim.Paint`, techo abierto como abertura de vertido).
Cada tick a 30Hz consume (`Paint Empty`) toda celda no-Empty/no-StaticSolid
del interior y la evalúa contra `OrderSystem.ActiveOrders` EN ORDEN (primer
encargo incompleto que matchea gana progreso); lo que no matchea ningún
encargo se acumula como "chatarra" y da +1 Favor cada 10 celdas
desperdiciadas.

### Coste de Favor en los grifos (Dispenser.cs)

`favorCostPerActivation` (fijado desde `AlkahestGameBootstrap`): Water/Sand
0, Oil 2, Nutrient 5. Se cobra una única vez al pasar de OFF a ON (no por
tick); si no hay Favor suficiente el grifo no se enciende y el label
muestra "(sin Favor)" un momento.

## M12 — Campo morfológico (playtest 12): la materia TIENDE a un patrón

Referencia técnica para quien vaya a tocar la simulación. Origen: hasta esta
ronda la variación por seed era solo numérica (probabilidades, bandas de
temperatura, Edictos) — dos partidas se veían idénticas porque la materia se
veía idéntica. Tesis del sistema (palabras de Cesar, el diseñador): *"la
morfología puede ser una propiedad del material, no una forma rígida"* y *"no
necesitas que al aspirarlo conserve exactamente el dibujo píxel por píxel;
necesitas que cuando vuelva a existir, vuelva a TENDER a formar ese tipo de
patrón"*. Por eso el patrón no se guarda ni se serializa: se REGENERA por una
regla local que cada celda vuelve a aplicar dondequiera que acabe.

### El contrato

`CellGrid.morph` (`byte[]`, 0..255) es la intensidad morfológica por celda,
paralela a `mat`/`temp`/`aux`. `SimRenderer` la convierte en desplazamiento de
brillo sobre el color base, escalado por `MaterialDef.patronFuerza`. Qué hace
cada familia (`PatronMorfologico`) con ese byte:

| Familia | Quién la evoluciona | Significado de `morph` |
|---|---|---|
| `Liso` | nadie | no se usa |
| `Vetas` | `SimRenderer` (hash puro de x,y,tick) | **no se usa** — puramente posicional |
| `Celdas` | `SimRenderer` (Voronoi puro de x,y,tick) | **no se usa** — puramente posicional |
| `Manchas` / `Laberinto` | `SimStepper.MorphTick` | concentración de reacción-difusión |
| `Dendritas` | `SimStepper.MorphTick` | fuerza de rama (0 = sin rama) |
| `Pulso` | `SimStepper.MorphTick` | fase 0..255 |
| `Motas` | `SimStepper.MorphTick` | intensidad de chispa (0 = apagada) |

`Vetas` y `Celdas` cuestan CERO en el stepper — `MorphTick` las descarta en el
primer `if` sin tocar memoria. El ahorro es real: en un taller típico, la
mayoría de la materia en pantalla es vocabulario del taller (`Liso`).

### Doble búfer — obligatorio, no una optimización

`CellGrid.morphScratch` es un segundo array del mismo tamaño. `MorphTick`
copia `morph → morphScratch` al empezar, TODAS las familias escriben
exclusivamente en `morphScratch` durante el recorrido, y al final se copia
`morphScratch → morph`. Es obligatorio porque `Manchas`/`Laberinto` leen los 4
vecinos ortogonales para su reacción-difusión: si leyeran y escribieran el
mismo array, el resultado dependería del orden en que el bucle visita las
celdas, y eso rompe el determinismo del que depende el netcode futuro (mismo
principio que ya obligó al fix de `DiffuseTemperature` en el playtest 9,
aplicado aquí desde el diseño en vez de como parche posterior). `Dendritas`
escribe en un VECINO (no en sí misma) usando `max()` en vez de sobrescribir,
que es conmutativo: si dos ramas compiten por el mismo vecino en el mismo
tick, el resultado no depende de cuál se procesó primero.

`CellGrid.SetCell` siembra `morph` con un hash barato de `(idx, material)` en
vez de ponerlo a cero — un campo plano tarda mucho en romper la simetría y se
vería un instante de materia lisa antes de que aparezca el patrón.
`CellGrid.SwapCells` intercambia `morph` junto con `mat`/`temp`/`aux`: un
líquido que fluye arrastra su dibujo consigo en vez de dejarlo clavado a las
coordenadas del mundo.

### El estriado 1/4 y su verificación aritmética

`MorphTick` solo procesa 1/4 de las celdas por tick: `offset = tick % 4`,
bucle `for (i = offset; i < n; i += 4)`. Verificación: cada celda `i` tiene un
offset fijo `o = i % 4` para siempre. "Le toca turno" se evalúa como
`(tick % 4) == o`, que es congruencia módulo 4 — se cumple exactamente una vez
cada 4 ticks consecutivos, para cualquier `o` en {0,1,2,3}. Las cuatro clases
cubren el 100% de las celdas con la MISMA frecuencia (una vez cada 4 ticks,
7.5 Hz a 30 Hz de sim). A diferencia de los dos bugs de deriva de temperatura
del playtest 9, aquí el offset es la ÚNICA guarda — no hay una segunda
condición temporal combinada con él que pudiera colapsar la cobertura a un
subconjunto (ese fue exactamente el bug 1 de `DiffuseTemperature`).

### Chunks dormidos: se respetan, pero a 1/8 de frecuencia

Un chunk dormido (`!CellGrid.IsChunkAwake`) SÍ sigue evolucionando su morph,
solo que 8x más despacio: `dormantActiveRound = ((tick >> 2) & 7) == 0`, así
que un chunk dormido muta 1 vez cada 32 ticks (~1.07 s a 30 Hz) en vez de 1
vez cada 4 (~0.13 s). Decisión deliberada frente a las dos alternativas
obvias:
- Congelar del todo mientras duerme: un charco que se queda quieto justo
  cuando su Manchas/Laberinto está a medio converger se congelaría con ese
  dibujo incompleto PARA SIEMPRE — contradice la premisa del sistema
  ("vuelve a TENDER a formar el patrón", no "se congela a mitad de camino").
  Además Dendritas/Pulso/Motas ni siquiera tienen un estado "converged":
  Motas dejaría de titilar del todo, Pulso dejaría de respirar — se leería
  como un bug, no como sueño.
- Ignorar el sueño (evolucionar dormidos a la misma frecuencia): anula el
  ahorro por el que existen los chunks dormidos.

### Parámetros reales por familia

**Manchas / Laberinto** (`MorphReactionDiffusion`): reacción-difusión
estilo Gray-Scott simplificada a un único byte `v` (el sustrato `u` se
aproxima como `255-v`). `feed = 8 + (patronFuerza>>4)` (8..23, misma fórmula
en ambos regímenes); lo que separa las dos familias es el DELTA `kill-feed`,
en bandas que nunca se solapan sea cual sea `feed`: Laberinto delta 2..6
(`kill = feed + 2 + (patronEscala>>1)`, bandas que serpentean, kill≈feed);
Manchas delta 16..23 (`kill = feed + 15 + patronEscala`, puntos que colapsan
y compiten por sustrato, kill≫feed). Difusión con `lap / diffDiv` donde
`diffDiv = max(4, 20 - patronEscala*2)` (división con truncamiento hacia
cero, no `>>`, mismo criterio de simetría de signo que el fix de temperatura
del playtest 9 — `lap` puede ser negativo).

**Dendritas** (`MorphDendrites`): semillas dispersas y raras — probabilidad
`1 / (600 + (8-patronEscala)*300)` por turno de estriado de la celda. Al
germinar arranca en 200..255. Se propaga a UN vecino por tick con 70% de
sesgo al eje `semillaPatron & 3` (el resto reparte entre los otros 3),
decayendo `10 + patronEscala` por paso hasta morir en 0 — se lee como aguja
que se afina hacia la punta, no como mancha redonda.

**Pulso** (`MorphPulse`): NO acumula sobre el valor anterior (evita deriva
por redondeo y es autocorrectivo si la celda se mueve). Se recalcula como
función pura: `fase = (tick*velocidad + distanciaManhattan(ancla)*5) mod 256`,
con `velocidad = 1 + (ritmoAnim>>4)` (1..16) y el ancla fija por
`semillaPatron`. A `ritmoAnim` alto (~160) completa una vuelta cada ~256
ticks (~8.5 s): un respirar lento, no un parpadeo.

**Motas** (`MorphSparkle`): probabilidad de chispazo `1 / (2500 -
patronEscala*200)` por turno; al encenderse arranca en 220..255 y decae con
el TIEMPO (a diferencia de Dendritas, que decae con la distancia recorrida) a
`40 + (patronFuerza>>2)` por turno — un parpadeo de pocos turnos de vida, no
una mancha que se queda pintada.

### Morfología de CRECIMIENTO: cristalización y Vivium

La firma visual no solo pinta: también sesga qué VECINO elige una reacción de
crecimiento, sin tocar nunca CUÁNTOS se convierten (la tasa/probabilidad es
idéntica a antes de esta ronda).

**Cristalización** (`TryCrystalGrowth`, `SimStepper.cs`): solo hay una
reacción de cristalización que de verdad elige vecino —
`CrystalSeed`(Powder)+`Azoth` vista desde `CrystalSeed`, porque `Crystal` es
`StaticSolid` y nunca ejecuta sus propias reacciones (Azoth+Crystal a granel
es autoconversión del propio Azoth, sin vecino que elegir). Tres modos según
`patron` de `Crystal`:
- **Compacto** (`Celdas`): puntúa cada Azoth candidato por cuántos vecinos
  suyos ya son Crystal/CrystalSeed y elige el más rodeado (`CountCrystalNeighbors`)
  — el cristal rellena huecos en vez de alargar un frente.
- **Dendrítico** (`Dendritas`): orden de comprobación de los 4 vecinos con
  sesgo fuerte a un único eje (`semillaPatron & 3`), mismo criterio que
  `MorphDendrites` — forma y textura cuentan la misma historia.
- **Laminar** (cualquier otro plausible para StaticSolid — Vetas/Liso): sesgo
  a UN EJE completo (las dos direcciones opuestas primero, no una sola
  punta), horizontal o vertical fijo por el bit menos significativo de
  `semillaPatron`.
Garantía de tasa: `TryReactNeighbor` tira el dado con
`XorShift.FromCell(tick,x,y,77)` sobre las coordenadas de SELF (no del
vecino), así que el resultado de "¿cristaliza este tick?" es el mismo sin
importar qué vecino se compruebe primero — reordenar los vecinos cambia solo
CUÁL se convierte, nunca SI se convierte.

**Vivium** (`GrowthTick`, `SimStepper.cs`): tres modos según `patron` de
`Vivium` (`VivGrowthModeFor`):
- **Enredadera** (Dendritas/Laberinto): sigue la dirección de la que vino la
  célula madre, guardada en 3 bits libres de `aux` (`CameFromDirMask = 0x03`
  para la dirección, `CameFromKnownFlag = 0x04`; no colisiona con `0x80`
  "asentado" de Organic ni `0x40` `OrganicDormantAux`).
- **Disperso** (Manchas/Motas, y fallback si Enredadera/Mata no encuentran
  candidato): prefiere el candidato con MENOS vecinos de Vivium alrededor
  (`CountOrganicNeighbors`) — lo opuesto al modo Compacto del cristal; deja
  huecos en vez de rellenarlos.
- **Mata** (Celdas/Pulso): comportamiento original, isótropo, orden aleatorio
  sembrado por celda.
Garantía de tasa: la elegibilidad sigue siendo exactamente "¿hay un Nutrient
ortogonal?" en los tres modos, y `rng.ChancePercent(VivGrowChancePct)` (60%)
se llama UNA sola vez por célula elegible por tick, igual que antes de esta
ronda — solo cambia CUÁL Nutrient candidato se usa cuando hay más de uno.

### Render (SimRenderer.cs)

Vetas: bandas senoidales (tabla de seno de 256 entradas, construida una vez)
deformadas por `LatticeNoise` (rejilla de hash con interpolación bilineal
entera); `patronEscala` (1..8) se remapea a periodos de 14..35 celdas —nunca
literal, o a ~7.5 px/celda sería ruido. Celdas: Voronoi barato de 9 puntos
jitterados por rejilla (`VoronoiEdge`), `patronEscala` remapeado a teselas de
18..46 celdas. Manchas/Laberinto/Pulso/Dendritas/Motas leen `CellGrid.morph`
directamente. Dendritas SOLO ilumina (nunca oscurece), para leerse como aguja
y no como sombra; Motas es aditivo puro hacia blanco.
Bordes (detectando vecino `Empty` ortogonal): `Neto` no hace nada; `Halo`
suma brillo fijo +34 (independiente de `patronFuerza` — el borde es silueta,
no patrón); `Escarcha` enciende ~1/3 de las celdas de contorno con un hash
estable por posición (no por tick, para no titilar); `Difuso` oscurece hacia
`BackgroundColor` en la mitad de las celdas de contorno — deliberadamente NO
baja el alfa (ver advertencia abajo).
**Chunks dormidos + patrones puramente posicionales**: Vetas y Celdas se
recalculan puras de `(x,y,tick)` sin leer `morph`, así que si el chunk no se
redibuja por estar dormido, SE CONGELAN aunque `ritmoAnim>0`. Las demás
familias ya avanzan al ritmo throttleado de `MorphTick`. Solución:
`_chunkContinuousAnim[]` (`bool[]` por chunk), marcado por `RenderChunk` si
alguna celda del chunk es Vetas/Celdas con `ritmoAnim>0`; `RenderFrame` exime
a esos chunks del sueño SOLO para el redibujado (la física de la sim sigue
dormida igual). Deliberadamente no se subió `FullRefreshEveryFrames`, que
habría encarecido toda la grilla por un puñado de sustancias.
**Advertencia para quien toque el renderer**: el borde `Difuso` NO debe bajar
el alfa. El sim es 1 téxel/celda en `FilterMode.Point`, y detrás vive
`WorkshopBackdrop` — otra textura Point a triple resolución. Un téxel
semitransparente ahí no se funde con nada: dos texturas Point de resoluciones
distintas componiendo alfa producen un mosaico duro del fondo asomando en
bloques de ~7.5 px, que se lee como "recorte roto", no como deshilachado. Es
una idea que ya se probó y se descartó — no reimplementarla (ver regla 15 de
`CLAUDE.md`).

### El sorteo de firma (Universe.Create → SortearFirmasVisuales)

Solo aplica a lo innominado (Azoth, CrystalSeed, Crystal, Vivium, Slime,
Acid) — el vocabulario del taller se queda siempre en `Liso`/`Neto`. Tres
garantías con verificación numérica:
1. **Separación de tono**: ancla de tono por seed + reparto a intervalos de
   `360/6 = 60°` con jitter ±12° → separación angular mínima garantizada de
   36° entre cualquier par (60 - 2×12).
2. **Diversidad de familias**: orden barajado + cada material prefiere una
   familia aún no usada dentro de su lista de plausibles por arquetipo
   (`FamiliasPlausibles`), con refuerzo de que al menos una quede
   `Liso`/`Vetas` para que la pantalla no se vuelva ruido puro.
3. **Legibilidad**: luminancia perceptual `L = 0.2126R + 0.7152G + 0.0722B`,
   mínimo `L >= 0.40` (pared del taller L≈0.127, piedra L≈0.345). `EnsureMinLuma`
   sube primero `V`; si `V` ya está a tope y sigue sin llegar al mínimo (caso
   real: azules/violetas saturados con H≈240°, donde R y G son casi cero
   incluso con V=1), baja `S` hasta un suelo de 0.15 (sigue leyéndose como
   color, no gris puro).

API: `Universe.CaracterDelUniverso` (frase corta cacheada del "clima visual"
de la run, sin nombrar sustancias) y `Universe.DescribirFirma(byte matId)`
(cacheada en array privado en la creación, nunca reconstruida por frame).
