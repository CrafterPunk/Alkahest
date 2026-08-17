# CONTRATO — EL TALLER COMPARTIDO (POC multiplayer, Playtest 28)

Mandato de Cesar: primera prueba de concepto multiplayer; 4 jugadores si es
posible (3 si fuera costoso — NO lo es: el lobby y NGO aceptan 4 sin coste);
cada jugador se distingue por COLOR del personaje; decisiones no
especificadas las toma la dirección. Este contrato ES esas decisiones.

## 0. ARQUITECTURA (cerrada)

- **La sim vive SOLO en el host** (backlog histórico: "sim solo-host +
  deltas"). Los clientes llevan un ESPEJO: mismo `CellGrid` + `SimRenderer`,
  pero `SimStepper` jamás corre en ellos (`AlkahestSim.ModoEspejo`).
- **Sincronización del mundo por CHUNKS**: el host, cada 6 ticks (~5 Hz),
  recopila los chunks tocados desde el último envío (`chunkTouchedTick`) y
  manda su `mat[]` comprimido RLE (byte valor + byte cuenta) por
  CustomMessagingManager (mensaje nombrado, FastBufferWriter, entrega
  confiable). Snapshot COMPLETO al conectar un cliente (todo el grid RLE — es
  casi todo piedra, comprime a nada). `temp[]` y `morph[]` NO se sincronizan
  en el POC (documentar: los clientes ven materiales sin incandescencia ni
  patrón evolutivo — aceptado).
- **Avatares**: prefab construido POR CÓDIGO (cero assets, como todo) al
  vuelo: `NetworkObject` + `OwnerNetworkTransform` (del template) +
  `FriendsLoop.Demo.PlayerIdentity` (nombre Steam flotante, ya existe) +
  `ApprenticeController` + `Flask` + los componentes de interacción locales.
  El controlador solo se habilita para el DUEÑO (patrón
  `enabled = IsOwner` del PlayerController del demo). La cámara sigue al
  avatar LOCAL.
- **COLORES (mandato explícito)**: componente nuevo `AprendizNet` con
  NetworkVariable byte `IndiceColor` (escribe el server al spawnear, por
  orden de llegada): 0 = DORADO (anfitrión), 1 = AZUL CIELO, 2 = VERDE,
  3 = MAGENTA. Tinta TODOS los SpriteRenderers del cuerpo del aprendiz
  (multiplicativo sobre el sprite procedural; expón en ApprenticeController
  un `AplicarTinte(Color)` que recorra sus renderers — ala/cola incluidas).
- **Escritura al mundo desde clientes**: toda mutación pasa por
  `AlkahestSim.Paint/PaintStable/PaintCell/PaintRect`; en ModoEspejo esas
  llamadas NO tocan el espejo: se REENVÍAN al host vía
  `SimSync.SolicitarPintura` ([Rpc(SendTo.Server)] con x,y,radio,mat,modo) y
  el host las aplica a la sim real (el cambio vuelve solo por el sync de
  chunks). Así el FRASCO de un invitado funciona entero sin tocar su código:
  lee del espejo, escribe por red.
- **División de trabajo del POC (aceptada)**: los invitados VUELAN, ASPIRAN
  y VIERTEN. Las máquinas (E) las opera el anfitrión — sus componentes solo
  se instancian en el host. Encargos/pistas/diario: solo anfitrión. Es
  cooperación real ya: los invitados acarrean materia, el anfitrión hornea.

## 1. ARCHIVOS

NUEVOS (carpeta `Assets/Alkahest/Net/`):
- `SimSync.cs` — NetworkBehaviour singleton: broadcaster de chunks (host),
  aplicador (cliente), snapshot on-connect, RPC de pintura. Instancia
  estática `SimSync.Instancia`; `public static bool EnSesion` (¿NGO activo?).
- `AprendizNet.cs` — NetworkBehaviour del avatar: IndiceColor + tinte +
  habilitar/deshabilitar componentes según IsOwner + registro de la cámara
  local + paleta estática `ColoresJugador[4]`.
- `TallerSesionHud.cs` — IMGUI (UiStyles) de sesión: botones ANFITRIÓN (crea
  lobby de 4 + host) / UNIRME (lobby de un amigo Steam o loopback local para
  la prueba de dos instancias), estado de conexión, errores en español,
  lista de jugadores conectados con su color. Habla SOLO con
  SessionCoordinator (regla del template: nadie toca NetworkManager directo).
- `Editor/AlkahestNetSceneBuilder.cs` (en Assets/Alkahest/Editor/) — menú
  **"Alkahest/2. Generar escena Lab MULTI"**: escena aparte con todo lo de la
  escena Lab MÁS NetworkManager + UnityTransport + SteamNetworkingSockets +
  SessionCoordinator + SteamLobbyService + SteamBootstrap + SimSync +
  TallerSesionHud, calcando el cableado de FriendsLoopDemoSceneBuilder
  (léelo entero — el wiring de NetworkManager/transportes/prefab de jugador
  registrado es EXACTAMENTE ese patrón). PlayerPrefab: generado por código y
  registrado como NetworkPrefab (estúdiate cómo el demo registra el suyo).

MODIFICADOS:
- `AlkahestSim.cs` — `public bool ModoEspejo`; si es true: no crear
  SimStepper ni steppear; `Paint*` reenvía a SimSync (si `SimSync.EnSesion`)
  en vez de escribir; expone `AplicarChunkRemoto(...)` para el aplicador.
- `AlkahestGameBootstrap.cs` — camino de red: si la escena tiene SimSync,
  NO spawnea el aprendiz local clásico (el avatar llega por NGO) y espera al
  avatar local para cablear frasco/cámara/foco; máquinas/encargos/pistas
  SOLO si IsServer. El camino mono-jugador de la escena Lab clásica queda
  INTACTO (misma lista plana, bifurcada con un if al principio).
- `DayCycle.cs` — SOLO añadir `public static void ForzarDesbloqueoSesion()`
  (InputLocked=false, HudSilenciado=false) para la escena multi, que no
  tiene ciclo de título. Nada más.
- `Alkahest.Runtime.asmdef` — añadir referencias: Unity.Netcode.Runtime,
  FriendsLoop.Runtime, Unity.Netcode.Components (si existe como asm aparte),
  Unity.Collections. MIRA los asmdef reales del proyecto y de FriendsLoop
  para los nombres exactos.

## 2. REGLAS

- 4 jugadores: `maxPlayers = 4` en la creación del lobby (el HUD lo pasa).
- CLAUDE.md entero aplica (regla 1: `[Rpc(SendTo.Server)]` estilo NGO 2.x,
  Input System nuevo, FindAnyObjectByType). Español latino en todo texto.
- El template NO SE TOCA (Assets/FriendsLoop = solo lectura, "salvo
  integración" — y esta integración no exige tocarlo).
- Cero allocs por frame en el hot path del sync: FastBufferWriter
  reutilizable, buffers precalculados (el RLE de un chunk cabe en 512 bytes;
  reserva y reusa).
- La escena Lab CLÁSICA (menú 1) debe seguir funcionando EXACTAMENTE igual:
  todo lo nuevo se activa solo si hay SimSync en escena / EnSesion.
- No hay Unity disponible ahora para compilar: escribe con DISCIPLINA DE API
  (usa como referencia canónica el código real del template en
  Assets/FriendsLoop — NetworkVariable, RPCs, NetworkManager wiring — y no
  inventes API de NGO que no veas usada ahí o no conozcas con certeza).
  Deja `// DUDA-API:` donde no estés seguro, para la pasada de compilación.

## 3. DEFINICIÓN DE HECHO DEL POC

Dos instancias del build en el mismo PC (una `-transport local` host, otra
cliente loopback) muestran: mismo mundo vivo (el agua vertida por el host
aparece en el cliente), 2-4 aprendices de colores distintos volando con su
nombre encima, un invitado aspira limo del charco y lo vierte en la boca del
crisol del anfitrión, y la hornada del anfitrión la ven todos. Steam
friends-lobby queda cableado para la prueba con amigos.
