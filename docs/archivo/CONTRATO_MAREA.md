# CONTRATO CONGELADO — LA MAREA (Playtest 24)

Este documento es la ÚNICA fuente de verdad compartida entre los dos encargos
paralelos (A = Sim, B = Game). Nada de lo que aquí se define se cambia sin
volver al director. Si tu encargo necesita algo que no está aquí, NO lo
inventes en el espacio del otro encargo: resuélvelo dentro de tus archivos o
déjalo anotado como pregunta al final de tu respuesta.

## 0. LA VISIÓN EN UNA FRASE

El mundo se está digiriendo a sí mismo: desde un CORAZÓN en el sótano sube una
MAREA oscura (afín a la química de la semilla) que convierte lo que toca en sí
misma. La piedra la bloquea (el cincel pasa a ser fortificación), el fuego la
quema con pérdida, y la única cura real es el ROCÍO: lo que exuda la criatura
al digerir materia de la marea. Ganas llevando Rocío hasta el corazón por el
pozo. Pierdes si la marea engulle a tu última criatura.

## 1. HECHOS COMPARTIDOS (no tocar, solo usar)

- Grid: `CellGrid.W=768, H=288`; `CellGrid.Idx(x,y)`, `InBounds`, `GetMat`,
  `SetCell(idx, mat, resetAux)`, arrays públicos `mat[]`, `temp[]` (raw byte).
- Temperatura: `raw = (°C + 120) / 2`; `CellGrid.RawToC/CToRaw`. Ambiente raw 70
  (=20 °C). Agua congela raw 52..67 según seed. Banda de cultivo Vivium:
  `Universe.VivGrowMinRaw..VivGrowMaxRaw`.
- Determinismo: SOLO `XorShift.FromCell(uint tick, int x, int y, uint salt)`.
  Regla 21: toda sal que mezcle constante + campo se castea `(uint)(...)`.
  Cada uso nuevo lleva SAL PROPIA (constantes nuevas, no reutilizar las de otros).
- Cero allocs en el hot path de SimStepper (nada de new/LINQ/boxing por tick).
- IMGUI únicamente para UI; sprites por código (SpriteRenderer); jamás
  `Shader.Find` en runtime.
- Comentarios en español, con la voz de bitácora del proyecto (por qué, no qué).
- Regla 33: `Universe.Leyes[i] ↔ Reactions.At(i)` NO SE ROMPE. La Marea NO se
  implementa como reacción del ReactionEngine ni como ley: es un proceso
  propio de SimStepper (como el fuego o el crecimiento). Así no perturba el
  sorteo de leyes existente ni el diario.
- Regla 48: todo estado nuevo necesita VERBO visible y CONSUMIDOR real.

## 2. IDENTIDADES NUEVAS (compartidas, exactas)

En `MaterialId` (Universe.cs):

```csharp
public const byte Marea = 17;
public const byte Rocio = 18;
public const int Count = 19;   // antes 17
```

Los ids 0..16 existentes NO cambian. Cualquier array dimensionado con
`MaterialId.Count` crece solo (p. ej. `_conteoDigestion` de Criatura).

## 3. ENCARGO A — SIM (archivos: `Sim/Universe.cs`, `Sim/SimStepper.cs`, `Sim/SimLevelBuilder.cs`)

### 3.1 MaterialDefs (en `Universe.Create`, junto a los demás)

**Marea** — `archetype = Liquid`, `density = 200` (se hunde bajo el agua y la
desplaza: la marea SUBE desde abajo), `fluidity` media-baja (~120: repta, no
salpica). Color base violeta muy oscuro `(46, 22, 58)` TINTADO ~20 % hacia el
`baseColor` del PRIMER material de `Universe.AfinidadDelUniverso` (mezcla en
`Create`, tras sortear la afinidad — la marea ES la química de esta semilla
hecha carne). `colorJitter = 10`. **Excepción documentada a la regla 17**
(morfología sorteada por seed): `patron = PatronMorfologico.Pulso`,
`borde = BordeMorfologico.Halo`, fijos — la marea debe reconocerse ENTRE
universos; escribe el comentario de la excepción. `emitsGlow = false`,
`emision` baja fija (~30). No arde, no congela, no hierve
(sus transiciones térmicas quedan en los `MaxValue/MinValue` por defecto:
la marea no cambia de fase; su debilidad es otra, ver 3.2).

**Rocío** — `archetype = Liquid`, `density` ligera (~80, flota sobre agua),
`fluidity` alta (~200). Color oro pálido `(232, 214, 150)`, `colorJitter = 8`,
`emitsGlow = true` (la cura BRILLA — se ve en la oscuridad del sótano),
`patron = Liso`, `borde = Neto` (también fijo, misma excepción: es el
anti-marea y se reconoce igual entre universos). Sin transiciones térmicas.

Registra ambos en `liquidDensity[]` como ya hacen los demás líquidos.

REVISIÓN OBLIGADA: barre `Sim/SimRenderer.cs` y cualquier `Dev/DevPalette.cs`
buscando tablas dimensionadas a mano a 17 (o listas explícitas de materiales)
y hazlas crecer con los dos nuevos; casi todo se dimensiona con
`MaterialId.Count` y crecerá solo, pero compruébalo con grep antes de dar por
hecho.

### 3.2 `SimStepper.ProcessMarea` (proceso propio, NO ReactionEngine)

Gate global: `public bool MareaActiva` en SimStepper (campo de instancia,
default `false`). Mientras sea `false`, las celdas Marea existen pero NO
convierten ni amortiguan (solo fluyen como líquido) — el corazón duerme.

En `ProcessIfNeeded`, tras el `case Liquid` (movimiento) y ANTES de
`MaybeReact`, si `m == MaterialId.Marea`: llamar
`ProcessMarea(_cellFinalX, _cellFinalY, _cellFinalIdx)`. La Marea NO llama a
`MaybeReact` (no participa en las leyes de la seed: regla 33).

Dentro de `ProcessMarea` (todo muestreado, mismo patrón 1/8 que MaybeReact:
`if (((x + y + (int)_tick) & 7) != 0) return;` — coste acotado):

1. **Curación (prioridad máxima, determinista):** si algún vecino ortogonal es
   `Rocio` → esta celda Marea se vuelve `Sand` (materia muerta, inerte) y el
   vecino Rocio se vuelve `Empty`. 1:1, SIEMPRE (sin azar): el jugador debe
   poder CONTAR su cura.
2. **Fuego (arma con pérdida):** si algún vecino ortogonal es `Fire` → con
   probabilidad ~10 % (XorShift, sal propia `SalMareaFuego`) esta celda →
   `Smoke`. El fuego muerde la marea, pero lo quemado no se recupera.
3. **Conversión (el hambre del mundo):** elegir UN vecino ortogonal al azar
   (XorShift, sal propia `SalMareaConversion`). Según el material del vecino:
   - `Stone` → INMUNE, nunca. (La piedra es la muralla: da sentido al cincel.)
   - `Empty`, `Marea`, `Rocio`, `Fire`, `Smoke`, `Steam` → nada.
   - Líquidos, polvos, gases restantes, `Nutrient`, `Slime`, `Ash` → prob. ~6 %
     de volverse `Marea`.
   - `Vivium` y sólidos estáticos NO-piedra (`Ice`, `Crystal`, `CrystalSeed`)
     → prob. ~1 % (lenta: engullir un cuerpo o una muralla de hielo se VE
     venir; el hielo de la cría fría es muralla temporal, peor que piedra).
4. **Amortiguación térmica:** empujar `temp[]` de la PROPIA celda y del vecino
   elegido 1 raw hacia 50 (=−20 °C) por muestreo. La marea apaga la
   estrategia térmica cerca de sí: ni placas ni criatura calientan "a través"
   de ella. (1 raw/muestreo; sin bucles extra.)

Al convertir una celda usar `_grid.SetCell` + despertar chunk como ya hacen
los procesos vecinos (mirar cómo lo hace ProcessFire y calcar el patrón).

### 3.3 Emisión (el corazón mana)

En `Step()` (fuera del barrido, tras MorphTick), si `MareaActiva`: cada
`MareaEmisionCadaTicks = 20` ticks (~0,67 s), pintar 1 celda de Marea en una
posición al azar (XorShift, sal `SalMareaEmision`, tick como semilla) dentro
del RECTÁNGULO DEL CORAZÓN (constantes de 3.4), solo si esa celda está
`Empty` o es líquido no-Marea. Ritmo lento: presión de fondo, no tsunami.

### 3.4 `SimLevelBuilder` — la cámara del corazón

Constantes públicas nuevas (zona del zócalo del sótano, x333..392 ya piedra):

```csharp
public const int CorazonMareaX0 = 352;
public const int CorazonMareaX1 = 373;
public const int CorazonMareaY0 = SotanoInteriorY0 + 1; // 14
public const int CorazonMareaY1 = SotanoInteriorY0 + 6; // 19
```

En `BuildCuartoIntimo` (que hoy llena el mundo de piedra): CARVAR la cámara
(rect a Empty) dentro del zócalo, dejando ≥2 celdas de piedra por cada lado, y
sembrar el fondo de la cámara (fila Y0) con Marea (dormida hasta MareaActiva).
El pozo (WellX0..X1 = 343..382) queda ENCIMA: quien baje por él y excave el
último tramo llega a la cámara. NO abrir el camino: la piedra entre pozo y
cámara es del jugador y su cincel.

## 4. ENCARGO B — GAME (archivos: `Game/MareaDirector.cs` (nuevo), `Game/Criatura.cs`, `Game/DayCycle.cs`, `Game/HintSystem.cs`, `Game/Cincel.cs`, `Game/AlkahestGameBootstrap.cs`)

### 4.1 `Cincel.CeldasTalladas`

En Cincel.cs: `public static int CeldasTalladas { get; private set; }` —
incrementa con cada celda de piedra REALMENTE tallada (no rellenada). Reset a
0 en `Init`. Es la señal de despertar del director.

### 4.2 `MareaDirector` (MonoBehaviour nuevo)

`Init(AlkahestSim sim, DayCycle dayCycle, HintSystem hints)`. Spawn en
`AlkahestGameBootstrap.TrySpawn()` tras DayCycle, con las referencias reales.

**Despertar:** la marea despierta (pone `sim.Stepper.MareaActiva = true`, una
sola vez) cuando `Cincel.CeldasTalladas >= 12` (has empezado a abrir el mundo:
el mundo también se abre hacia ti) O cuando lleves `>= 300 s` de partida
jugable (`!DayCycle.InputLocked`). Al despertar: anunciar por el sistema de
pistas (ver 4.5) y un latido grave — nada de pantallazos.

**Sondeo (cada 2 s, nunca por frame):**
- VICTORIA: contar celdas `Rocio` dentro del rect del corazón
  (`SimLevelBuilder.CorazonMarea*`). Si `>= 24` → `dayCycle.TerminarPartida(victoria: true)`.
  24 celdas ≈ dos frascadas: exige viaje, no gota simbólica.
- DERROTA: si la marea está despierta y `Criatura.NumVivas == 0` →
  `dayCycle.TerminarPartida(victoria: false)`.

### 4.3 `Criatura` (tres cambios quirúrgicos)

1. **Registro público:** `public static int NumVivas => _activas.Count;`
2. **Miedo:** en `SondearDigestionYAmenaza`, la condición de amenaza suma
   `mat == MaterialId.Marea` (junto a Fire/Acid; la marea NUNCA puede ser
   `_ultimoProductoDigestion` así que no necesita esa exclusión — ver 3).
   NOTA DE DISEÑO (dejarla en comentario): miedo y digestión COEXISTEN a
   propósito — la marea la asusta Y la digiere a la vez; la criatura sufre
   para fabricar la cura. Asustada no bloquea la digestión hoy (verificado)
   y así debe seguir.
3. **Digestión de Marea (el eslabón central del juego):** en
   `EscogerProductoDigestion`, ANTES del escalón 1: si
   `matEntrada == MaterialId.Marea` → producto `MaterialId.Rocio`, SIEMPRE,
   en todo universo. Comentario: la criatura es lo único del mundo que mastica
   en dirección contraria. (La exclusión `_ultimoProductoDigestion` funciona
   sola: tras exudar Rocío, para repetir hay que traerle OTRA marea — el
   jugador hace de porteador entre el frente y su criatura.)
4. **Muerte por marea:** si la celda del NÚCLEO de la criatura (la misma que
   usa `ApplyCalorTick` como núcleo) es `MaterialId.Marea` durante más de
   `9 s` acumulados (contador que se vacía a mitad de ritmo cuando está
   libre), la criatura muere: libera su cuerpo (celdas Vivium → Marea — la
   imagen más dura del juego, engullida de verdad), `Destroy(gameObject)`
   (OnDestroy ya la saca de `_activas`). El sondeo existente de 0,4 s basta;
   NO añadir escaneos por frame.

### 4.4 `DayCycle.TerminarPartida`

```csharp
public void TerminarPartida(bool victoria)
```

Guarda `_desenlaceMarea = victoria ? DesenlaceMarea.Victoria : DesenlaceMarea.Derrota`
y salta `_phase = Phase.EndScreen`. En `DrawEndScreen`, si hay desenlace de
marea, se dibuja EN VEZ del desenlace por Favor (que se mantiene para el modo
clásico):
- Victoria: título `"EL MUNDO SE AQUIETA"` (color `UiStyles.Oro`), subtítulo:
  `"El Rocío alcanzó el corazón. La marea se retira a dormir, y por primera vez el taller respira. Vosotros, y lo que criasteis, sois la razón."`
- Derrota: título `"LA MAREA OS TRAGÓ"` (color `UiStyles.Peligro`), subtítulo:
  `"La última criatura se apagó bajo la marea. El mundo terminó de digerirse a sí mismo, y nadie quedó para masticar en dirección contraria."`
Debajo, las mismas stats (seed, descubiertos, bautizados) y los mismos botones
de reinicio que ya tiene la pantalla.

### 4.5 `HintSystem` — el arco contado en pistas

Método nuevo `public void EncolarPistaDeMarea(string pista)` que INTERRUMPE la
cola normal y muestra esa línea (misma placa, mismo estilo, prioridad). El
director la usa en tres momentos, una vez cada uno:
- Al despertar: `"Algo se ha despertado abajo. El agua del fondo ya no es agua."`
- Primer Rocío exudado (detectable: sondeo del director encuentra >0 celdas
  Rocio en el mundo por primera vez): `"Eso que exuda tu criatura HIERE a la marea. Recuérdalo."`
- Marea por primera vez por encima de y = `SimLevelBuilder.SotanoY1 - 20`
  (sube hacia la superficie): `"La marea sube. La piedra la frena; el cincel ya no es solo una herramienta."`

### 4.6 `AlkahestGameBootstrap`

Añadir el spawn del MareaDirector con sus referencias. NADA MÁS: no tocar
las firmas congeladas existentes.

## 5. LO QUE NINGÚN ENCARGO TOCA

`ReactionEngine.cs`, `LeyDelUniverso.cs`, `SimEvents.cs`, `Capullo.cs`,
`OrderSystem.cs`, `OrdersHud.cs`, `JournalHud.cs`, sorteo de leyes/afinidad
en `Universe.Create` (solo se LEE la afinidad para el tinte), y toda la
maquinaria clásica dormida (HeatPlate, ChillStone, MasterSupplies...).

## 6. DEFINICIÓN DE HECHO

Compila sin warnings nuevos; cero allocs nuevos por tick; comentarios en
español explicando POR QUÉ; cada constante nueva con su nombre exacto de este
contrato (otros archivos las referenciarán tal cual). Al terminar, lista los
archivos tocados y cualquier decisión que hayas tenido que tomar fuera del
contrato.
