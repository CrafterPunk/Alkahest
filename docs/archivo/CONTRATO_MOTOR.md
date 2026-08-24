# CONTRATO CONGELADO — LA RONDA DE MOTOR (Playtest 39)

Dos encargos paralelos: **S = sim** (combustión persistente + brasas + gases
con corrientes + pátina + reacciones dirigidas) y **F = fx** (capa de
partículas desprendidas). Aprobado por Cesar sobre INFORME_MOTOR.md §3
(paquete 1+3+5 + sub-muestreo dirigido) más la extensión de combustión
persistente: *"que aceite u otros combustibles puedan alimentar y mantener
una llama mientras exista combustible, con consumo progresivo, calor, humo y
residuo... tiene que seguir siendo determinista y barato"* + **brasas**
(el fuego que muere deja brasa: menos luz, todavía calor, reencendible).
Sin cuerpos rígidos (decidido: no valen su precio).

## 0. LA TESIS DE LA RONDA

El fuego de hoy es un actor sin memoria: nace, parpadea y muere en ~2s
aunque esté parado sobre un lago de aceite. Después de esta ronda el fuego
es un PROCESO: el combustible ES la celda que arde (con su reserva, su ritmo
y su residuo), la llama visible es su lengua, la brasa es su vejez, el humo
busca el techo, y el taller RECUERDA cada incendio en su pátina. Y todo eso
sin gastar más de ~2 ms del presupuesto medido en INFORME_MOTOR.md.

## 1. ENCARGO S — el fuego como proceso (y las tres capas del informe)

Archivos de S: `Sim/Universe.cs`, `Sim/SimStepper.cs`, `Sim/CellGrid.cs`,
`Sim/MaterialDef.cs`, `Sim/SimRenderer.cs`, `Sim/SimEvents.cs`,
`Game/Crisol.cs`, `Tools~/BenchSim/Harness.cs`, y toques SOLO ADITIVOS en
`Sim/SimLevelBuilder.cs` (registro de zonas de interés).

### 1a. Combustión persistente parametrizada

- `MaterialDef` gana parámetros de combustión por material (nombres en
  español, decide S los finos): **unidades de combustible** (cuánto arde),
  **ritmo de combustión** (celdas de vida quemadas por paso), **calor
  producido**, **producción de humo** (probabilidad), **residuo** (ya existe
  `burnsInto` — S decide si lo generaliza o lo reutiliza), **propagación**
  (agresividad al encender vecinos). El aceite y los calcinados combustibles
  de las bases (los que `Universe.EsCombustible` marca) reciben valores; el
  aceite clásico es el patrón oro: un charco encendido debe arder DECENAS de
  segundos consumiéndose visiblemente desde el borde encendido.
- **El estado "ardiendo" vive en `aux` de la celda de combustible** (byte:
  0 = frío, >0 = reserva restante mientras arde). VERIFICAR antes qué usos
  tiene `aux` hoy por arquetipo (Fire lo usa como vida; gases como lifetime)
  y documentar en comentario que no hay colisión para líquidos/polvos
  combustibles. Si algún combustible ya usa `aux`, S resuelve y documenta.
- La celda ardiendo: consume reserva a su ritmo (muestreado, no cada tick),
  SUBE su `temp` (calor producido) contagiando a vecinos por la difusión
  existente, escupe **lenguas de Fire** en celdas Empty ortogonales
  superiores (el Fire actual pasa a ser la LENGUA VISIBLE, no el consumidor
  del material), suelta **Smoke** con su probabilidad, y ENCIENDE vecinos
  inflamables según propagación + `ignitionTemp`. Reserva agotada →
  **residuo**: líquidos combustibles → humo/nada según material; sólidos
  combustibles → **BRASA**.
- El camino de ignición existente (Fire toca inflamable / temp ≥
  ignitionTemp) ahora PRENDE la celda (le pone reserva en aux) en vez de
  convertirla en Fire de golpe. El agua sigue mandando: celda ardiendo
  mojada (las reglas de extinción actuales) se apaga (aux → 0) con
  chorro de Steam.

### 1b. La brasa (nuevo material)

- `MaterialId.Brasa = 58`, `Count = 59`. Sólido estático (no cae — o cae
  como polvo, decide S y documenta), color rescoldo (naranja-rojo APAGADO
  con parpadeo sutil vía el campo morfológico o jitter determinista de
  color — NUNCA blanco).
- Vive ~8-12 s (cuenta atrás en `aux`), decae a **Ash**. Mientras vive:
  emite calor (menos que el fuego), y REENCIENDE inflamables adyacentes con
  probabilidad baja (salt nuevo). Echarle combustible fresco encima a una
  brasa = el fuego renace. Agua la mata a Ash de inmediato (con Steam).
- El brasero del Crisol se vuelve honesto: `Crisol.cs` ya quema combustible
  abstracto por tiers — S sincroniza el TEATRO: mientras la hornada quema
  combustible, las celdas de combustible del cesto arden DE VERDAD (aux
  encendido) y al agotarse el tier dejan brasas reales en el cesto. La
  lógica de tiers sigue siendo la autoridad del resultado químico; lo que
  cambia es que ahora el cesto se VE arder y abrasarse.

### 1c. Gases con corrientes (informe §3.5)

- Steam y Smoke (y gases de las bases si aplica): deriva térmica — al
  decidir su paso lateral, sesgo determinista hacia el vecino MÁS CALIENTE
  (leyendo `temp`), y al toparse con techo forman BOLSAS: se esparcen
  lateralmente bajo la bóveda en vez de morir en el sitio (pueden ganar
  vida extra embolsados, decide S con presupuesto). Salts nuevos, cero
  allocs. El alambique se vuelve más lógico gratis.

### 1d. Pátina — la memoria superficial (informe §3.3)

- `CellGrid` gana `public readonly byte[] patina` (0 = limpia). **La escribe
  y la lee SOLO el lado visual (`SimRenderer`), JAMÁS el stepper**: el
  renderer observa transiciones (celda Fire/ardiendo/Brasa junto a sólido
  estático → tizne que se acumula; líquido junto a sólido → mojado que se
  seca solo; Smoke bajo techo → tiznado de bóveda) y oscurece/tiñe el color
  final por celda. Al no tocar la sim: determinismo intacto, coste cero en
  el tick, y en multi CADA CLIENTE genera su pátina de lo que ve replicado
  — funciona para invitados sin un byte de tráfico. Presupuesto: el sondeo
  del renderer va por acumulador (una franja de filas por frame, no todo el
  mundo), documentado.
- El cincel y la mudanza LIMPIAN pátina donde tallan/restauran (que no
  queden manchas flotando en aire nuevo) — si eso exige un toque fuera de
  los archivos de S, S expone un helper público en CellGrid
  (`LimpiarPatina(x,y)`) y lo documenta como deuda de integración para
  Fable en vez de tocar archivos ajenos.

### 1e. Reacciones dirigidas (informe §4)

- API nueva en `SimStepper`: `RegistrarZonaInteres(x0, y0, x1, y1)` →
  máscara por chunk. `MaybeReact` muestrea **1/2 dentro de zonas de
  interés** y 1/8 fuera (el patrón actual `((x+y+tick)&7)` pasa a `&1` en
  interés). Las CUBETAS de las máquinas son las zonas: S registra desde
  `SimLevelBuilder` (donde viven todas las constantes de cubetas — solo
  llamadas aditivas) y/o desde `Crisol.cs`. Presupuesto: el informe lo
  midió como "casi gratis".

### 1f. Eventos para la capa de partículas

- El ring de eventos actual se consume destructivamente
  (`ConsumeEvents`, cliente único: SubstanceKnowledge). S lo convierte en
  ring persistente con índice de escritura monotónico y añade un lector NO
  destructivo multi-cursor (`LeerEventosDesde(ref cursor, ...)` o
  equivalente) SIN cambiar la firma ni la semántica de `ConsumeEvents`.
  Tipos de evento nuevos si hacen falta (p.ej. `Ember`): añadir al final
  del enum, jamás renumerar (SubstanceKnowledge ignora tipos que no
  conoce — verificar que su switch tiene default silencioso).
- El escenario nuevo del banco: S añade **INCENDIO SOSTENIDO** a
  `Tools~/BenchSim/Harness.cs` (piscina grande de aceite encendida + lecho
  de sólido combustible + techo para bolsas de humo, 300 ticks) para medir
  la combustión persistente contra los números del informe.

## 2. ENCARGO F — la capa de partículas desprendidas (informe §3.1)

Archivos de F: `Game/ParticulasFx.cs` (**NUEVO**, todo vive aquí) + las
líneas mínimas de spawn en `Game/AlkahestGameBootstrap.cs` (en `TrySpawn` Y
en `TrySpawnRed` — un jugador y multi; nada más en ese archivo).

- **Naturaleza**: partículas DECORATIVAS no-sim. No tocan la grilla, no
  tocan el determinismo, pueden usar Random de Unity (visual-only). En
  multi son client-local: cada cliente las genera de lo que VE en su
  grilla replicada — cero tráfico. Mueren en 0.3–1.5 s.
- **Arquitectura**: un `MonoBehaviour` con ring buffer preasignado
  (~4096 `struct Particula {x, y, vx, vy, vida, vidaMax, color, tipo}`),
  CERO allocs por frame. Render: una `Texture2D` overlay transparente de
  768×288 (misma escala mundo→píxel que la sim) en un `SpriteRenderer`
  ordenado SOBRE la textura del mundo y bajo los sprites de máquinas/HUD;
  se limpia y repinta solo las celdas tocadas (lista de píxeles sucios,
  `SetPixels32` parcial o full con buffer reutilizado + `Apply` una vez
  por frame). Gravedad simple + fricción por tipo en el update.
- **Emisión por OBSERVACIÓN de la grilla** (no depende del encargo S; el
  enganche al ring de eventos lo hace Fable en integración): cada frame,
  sondeo barato de una ventana alrededor de la cámara usando
  `grid.touchedTick` (celdas recién cambiadas) + `grid.mat`:
  - **Salpicaduras**: líquido que acaba de aterrizar (celda líquida recién
    tocada con sólido debajo y empty arriba) → 1-3 gotitas del color del
    líquido saltando con vx aleatorio.
  - **Chispas y ascuas**: celdas Fire → chispas naranjas ascendentes
    ocasionales que titilan y caen apagándose.
  - **Motas del crisol**: aire caliente (temp alta en celdas Empty sobre
    material caliente) → motas tenues ascendiendo.
  - **Polvo**: polvo que acaba de aterrizar → nubecita del color del
    material, se disipa.
  - **Vaho**: Steam recién nacido → volutas blancas semitransparentes.
  - Presupuesto de emisión por frame (p.ej. máx 64 nacimientos) para que
    un diluvio no funda el overlay; la ventana de sondeo va por
    acumulador de franjas, no el mundo entero por frame.
- **Sin dependencias nuevas**: F NO llama a ninguna API que no exista hoy
  en main. Si S termina antes y aparece `LeerEventosDesde`, NO usarla —
  eso es integración de Fable.
- Colores SIEMPRE derivados de `MaterialDef.color` del material que emite
  (regla del taller: nada de blanco puro; CarbonEmergencia como tinte de
  emergencia).

## 3. HECHOS COMPARTIDOS (los dos encargos)

- CLAUDE.md manda (leerlo entero antes de tocar nada). En particular:
  regla 7 (determinismo: `XorShift.FromCell(tick,x,y,SALT)` con salt ÚNICO
  nuevo — grep de los usados antes de elegir), regla de cero allocs en hot
  path, comentarios en español CON EL PORQUÉ, regla 15 (comentar, no
  borrar), regla 48 (verbo + consumidor), langversion 9.0.
- La sim compilable sin Unity: NADA en `Sim/` (salvo SimRenderer y
  SimLevelBuilder, ya excluidos del arnés) puede referenciar UnityEngine ni
  `Game/`. El arnés (`Tools~/BenchSim/Harness.cs`) debe seguir compilando.
- Presupuesto TOTAL de la ronda: ≤ ~2 ms añadidos en el peor escenario del
  banco (el diluvio puede llegar a ~7.5 ms). Si algo amenaza el
  presupuesto, recortar ambición y documentarlo, no pasarse.
- El OTRO encargo corre EN PARALELO sobre el mismo árbol: errores de
  compilación en archivos que no son tuyos pueden ser transitorios —
  ignóralos y reporta solo los tuyos.
- Verificación de compilación (regla 53, el rig ya está montado):
  ```
  cd /home/claude/alkahest
  CSC=$(find /usr/lib/dotnet /usr/share/dotnet -name csc.dll 2>/dev/null | head -1)
  SRC=$(find Assets -name '*.cs' ! -path '*/Editor/*')
  REFS=$(for f in /home/claude/unityrefs/*.dll; do case "$f" in *Alkahest.Runtime.dll) ;; *) printf ' -r:%s' "$f";; esac; done)
  dotnet "$CSC" -nologo -nostdlib+ -noconfig -t:library -langversion:9.0 \
    -define:UNITY_64 -define:UNITY_2023_1_OR_NEWER -define:NETCODEGAMEOBJECTS -define:STEAMWORKSNET \
    -out:/tmp/check_$$.dll $REFS $SRC
  ```

## 4. DEFINICIÓN DE HECHO

- **S**: compila (rig 53) sin errores en sus archivos; un charco de aceite
  encendido arde sostenido y muere en brasas→ceniza; el brasero del crisol
  arde y abrasa de verdad; el humo hace bolsas bajo la bóveda; la pátina
  ennegrece piedra junto al fuego y se moja/seca junto al agua; el arnés
  corre INCENDIO SOSTENIDO y S reporta la tabla nueva completa de los 6
  escenarios; reacciones 1/2 en cubetas verificable por comentario+código.
- **F**: compila; con solo mirar la pantalla, verter agua salpica, el fuego
  chispea, el crisol respira motas, la arena levanta polvo y el vapor
  humea; jamás un alloc por frame (buffers preasignados); presupuesto de
  emisión visible en el código.
- Ambos: resumen final con archivos tocados, decisiones fuera de contrato
  marcadas EXPLÍCITAMENTE, salts nuevos usados, y deudas para integración.
