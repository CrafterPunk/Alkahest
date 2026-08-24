# CONTRATO CONGELADO — LA PARIDAD VIVA (Playtest 43)

Reporte de Cesar tras la primera prueba real con un amigo: *"al absorber y
soltar elementos lo percibimos lageado y descoordinado; yo siendo el host
podía hacer todo pero mi amigo no podía abrir los grifos, activar las
máquinas, escuchar el sonido ni ver las animaciones de las máquinas...
buscamos una experiencia fluida."*

Dos encargos PARALELOS con archivos disjuntos: **N = nervios** (interacción
remota + estado vivo replicado) y **A = sentidos** (audio del invitado +
fluidez del frasco).

## 0. DIAGNÓSTICO (hecho — no re-diagnosticar)

1. **El invitado no puede usar nada**: `Net/MaquinaReplica.cs` es visual
   puro por diseño ("sin IMaquinaInteractiva, no responde a E"). Las
   máquinas reales viven SOLO en el anfitrión. No existe ningún camino
   E-del-invitado → acción.
2. **Cero animaciones de máquinas en el invitado**: `EntradaMaquina` del
   registro replicado lleva tipo/índice/ancla/centro/tamaño — NINGÚN estado
   vivo (trabajando/fuego/listo). Las réplicas son estatuas.
3. **Cero audio en el invitado**: `SpawnDirectorDeAudio` solo se llama en
   las ramas de anfitrión/un jugador; la rama invitado de `TrySpawnRed`
   termina sin él. Y aunque se spawneara: `ConsumirEventosSim` corta con
   `Stepper == null` (el invitado no corre sim → no hay ring de eventos) y
   las voces de grifo piden `Dispenser[]` reales (solo-anfitrión).
4. **El lag del frasco**: la difusión de chunks corre cada 6 ticks (~5 Hz,
   `IntervaloDifusionTicks`) con presupuesto 96 — hasta ~200 ms de retardo
   visual sobre el RTT en TODO lo que el invitado vierte/aspira. La pasada
   de prioridad por cercanía de avatar (pt36) existe pero corre a la misma
   cadencia.

## 1. API CONGELADA ENTRE ENCARGOS (N la implementa, A la consume)

- `EntradaMaquina.estadoVivo` (byte, bits): **bit0** Trabajando
  (hornada/prensada/análisis en curso), **bit1** FuegoEncendido (brasero
  del crisol con llama/brasas), **bit2** ResultadoListo (algo REPOSA en la
  cubeta esperando recogida), **bit3** Sirviendo (grifo abierto), **bit4**
  LuzPlena (lámpara del banco dictaminando), bits 5-7 reserva.
- `MaquinaSync.TryGetEstado(byte tipo, byte indice, out byte estado)` —
  válido en los dos lados.
- Evento estático `MaquinaSync.AlCambiarEstadoMaquina`
  (`System.Action<byte tipo, byte indice, byte antes, byte ahora>`) —
  disparado EN AMBOS LADOS al cambiar el estado replicado de una entrada.
- Errores de compilación contra esta API mientras N no termina son
  transitorios (protocolo pt40): repórtalos solo si persisten en tu
  compilación FINAL.

## 2. ENCARGO N — los nervios (interacción remota + estado vivo)

Archivos de N: `Net/MaquinaSync.cs`, `Net/MaquinaReplica.cs`, y en
`Game/{Crisol, Prensa, BancoChispa, ColumnaEnsayo, EnsayoMaestro, Alambique,
Dispenser}.cs` SOLO los métodos nuevos del gancho (nada más se toca ahí).

### 2a. E remoto

- Interfaz nueva (vive en MaquinaSync.cs o archivo propio pequeño en Net/):
  `IMaquinaUsableRemota { bool UsarPorRed(); byte EstadoVivoRed(); }`.
  Cada una de las 7 máquinas la implementa: `UsarPorRed()` ejecuta
  EXACTAMENTE lo que hace su E local (extraer el cuerpo del handler a un
  método compartido; el chequeo de PROXIMIDAD DEL ANFITRIÓN no aplica — la
  proximidad la validó el invitado sobre su réplica), devolviendo false si
  la acción no procede (sin carga, ocupada...). `EstadoVivoRed()` empaqueta
  los bits del §1 desde el estado real que la máquina ya tiene.
- `MaquinaSync`: `SolicitarUsoServerRpc(byte tipo, byte indice)` (idioma
  NGO del proyecto: `[Rpc(SendTo.Server, InvokePermission =
  RpcInvokePermission.Everyone)]`) → `BuscarFuente(tipo, indice)` → cast a
  `IMaquinaUsableRemota` → `UsarPorRed()`. Validación server-side de
  cordura: la posición del avatar del solicitante a ≤ un radio generoso del
  centro de la máquina (anti-teleuso, no precisión de píxel).
- `MaquinaReplica`: si el avatar local está dentro del rango de foco
  (cercanía que ya calcula para la chapa), muestra **"E — usar"** en la
  chapa y responde a E llamando al Rpc. ARBITRAJE local: si varias réplicas
  están en rango, solo la MÁS CERCANA responde (registro estático simple en
  MaquinaReplica, mismo espíritu que Game/MachineFocus, sin tocarlo).
  Respetar `UiStyles.EscribiendoTexto`/`JournalHud.Abierto` como todo
  input del proyecto.

### 2b. Estado vivo replicado

- El anfitrión SONDEA cada máquina fuente (`EstadoVivoRed()`) con
  acumulador (~4 Hz, jamás por frame) y actualiza `estadoVivo` en el
  registro SOLO al cambiar (NetworkList Value event — el patrón ya usado).
- `MaquinaReplica` ANIMA según bits: Trabajando = latido/glow del cuerpo
  (mismo lenguaje que las máquinas reales: AffordanceGlow/latido existente
  en MaquinariaSprites — reutilizar, no inventar); FuegoEncendido = tinte
  cálido pulsante en la zona del brasero; ResultadoListo = destello suave
  periódico en la cubeta ("ven a recoger"); Sirviendo = nada visual extra
  (el chorro real ya se replica por chunks); LuzPlena = halo frío en la
  lámpara del banco. Presupuesto: SpriteRenderer.color con seno, cero
  allocs, mismo costo que las chapas.
- La CHAPA de la réplica gana una segunda línea de estado por bits
  (textos fijos por tipo: "trabajando...", "¡listo — recoge!", "sirviendo",
  apagada si 0) — sin strings replicados, el texto vive en el cliente.
- Y dispara `AlCambiarEstadoMaquina` en ambos lados (§1).

## 3. ENCARGO A — los sentidos (audio del invitado + frasco fluido)

Archivos de A: `Audio/DirectorDeAudio.cs`, `Game/AlkahestGameBootstrap.cs`
(SOLO añadir el spawn de audio en la rama invitado), `Net/SimSync.cs`
(cadencia), `Game/Flask.cs` SOLO si la medición del lote lo pide.

### 3a. El invitado OYE

- Spawnear `DirectorDeAudio` en la rama invitado de `TrySpawnRed` con
  dependencias tolerantes a null (sin OrderSystem local → sin stingers de
  encargos, documentado; flask y player reales del invitado).
- **MODO ESPEJO** del director (nuevo, gateado por `Stepper == null`): sin
  ring de eventos, el audio ambiental sale de OBSERVAR la grilla replicada
  alrededor del oyente con acumulador (~4 Hz, franjas — mismo espíritu que
  ParticulasFx): densidad de Fire cercano → crepitar; líquido activo
  (touchedTick reciente) → chapoteo; Steam denso → siseo. Reutilizar los
  MISMOS loops/clips del director, solo cambia la fuente de intensidad.
- **Voces de grifo sin Dispenser**: anclar las voces a las entradas del
  registro de MaquinaSync (tipo grifo, centro replicado) y encenderlas con
  el bit Sirviendo (§1). En anfitrión NADA cambia (sigue con sus
  Dispenser reales).
- **One-shots de máquina**: suscribirse a `AlCambiarEstadoMaquina` y
  disparar los one-shots existentes en las transiciones (empieza a
  trabajar, resultado listo, lámpara dictamina) EN EL INVITADO. En el
  anfitrión no duplicar: sus máquinas reales ya suenan — gatear por
  `!EsServidor`.

### 3b. El frasco fluido

- `SimSync`: la PASADA DE PRIORIDAD (chunks sucios a ≤60 celdas de
  cualquier avatar) pasa a difundirse cada **2 ticks (~15 Hz)**; el resto
  del mundo conserva los 6 ticks. Presupuesto: los chunks prioritarios son
  pocos por construcción; mantener `MaxBytesCarga` y documentar el peor
  caso (4 avatares juntos vertiendo = ~un puñado de chunks × 15 Hz).
- Medir el camino del verter del invitado (lote
  `SolicitarPinturaServerRpc`): si el lote espera a llenarse antes de
  enviarse, bajar el intervalo de envío a ≤2 ticks también. Si ya envía
  por tick, no tocar Flask.cs y documentarlo.
- El resultado que busca Cesar, literal: verter y ver el chorro caer AHÍ,
  con retardo de red puro (~RTT + 66 ms), no de cadencia.

## 4. HECHOS COMPARTIDOS

CLAUDE.md entero (reglas 7, 15, 48, 53, 55). El idioma RPC del proyecto:
`[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]`.
El modo un-jugador y el anfitrión NO cambian de comportamiento perceptible
(el anfitrión ya lo tiene todo — regla dura). Determinismo de la sim
intacto (nada de esto toca el tick). Cero allocs por frame. El arnés debe
seguir compilando. Compilación regla 53 (rig ya montado, comando en
CONTRATO_SEMILLA.md §4). El OTRO encargo corre EN PARALELO — errores en
sus archivos o contra la API §1 son transitorios.

## 5. DEFINICIÓN DE HECHO

- **N**: el invitado, junto a una réplica, ve "E — usar" y su E abre el
  grifo / arranca la hornada / prensa / analiza / lee la columna / ensaya
  en el anfitrión; las réplicas laten al trabajar, brillan con el brasero,
  destellan al tener resultado; la validación server-side rechaza usos a
  distancia absurda; compila limpio.
- **A**: el invitado oye el grifo al servir (voz anclada a la réplica), el
  crepitar del fuego cercano, el chapoteo de su propio verter, y los
  one-shots de las máquinas al cambiar de estado; verter/aspirar se ve a
  ~15 Hz en la zona del jugador; compila limpio; presupuesto de red
  documentado.
- Ambos: informe con archivos tocados, decisiones fuera de contrato
  EXPLÍCITAS, y deudas para Fable. La prueba real de dos jugadores es de
  Cesar (el exe no es controlable por computer-use) — dejadle el camino
  regado: qué mirar, en qué orden.
