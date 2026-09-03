# TEN THOUSAND YEARS — contexto para agentes (LÉEME PRIMERO)

Falling-sand determinista (768x288 @30Hz) + taller + co-op. **Qué es el juego y cómo correrlo:
`README.md` (raíz). Estado y backlog: `docs/ESTADO.md`. Visión: `docs/GDD_TEN_THOUSAND_YEARS.md`.
Sim a fondo: `docs/SIM_NOTES.md`. Crónica de 70+ rondas: `docs/archivo/HISTORIAL_RONDAS.md`.**
Este archivo = solo lo operativo del agente: cómo trabajar aquí + el catálogo de reglas ganadas
a base de bugs (numeración ESTABLE: el código las cita por número; la versión narrativa completa
quedó en `docs/archivo/CLAUDE_v1_completo.md`).

Legado de nombres: repo GitHub `Alkahest`, namespace `Alkahest.*`, escena `AlkahestLab`, asmdef
`Alkahest.Runtime` — su renombre es la "ronda estructural" planificada en ESTADO.md. Todo lo
visible ya dice Ten Thousand Years (menús "Ten Thousand Years/1..5", builds
`Builds/TenThousandYears*`, productName, logs `[TenThousandYears]`).

## Operativa (Cowork + PC de Cesar)

1. Unity 6.5 EXACTO (6000.5.7f1): Input System nuevo solo (`Keyboard.current`);
   `FindAnyObjectByType`; ids constantes en GUILayout.Window; Rpc con `InvokePermission`.
2. Editar EN EL SANDBOX → `SendUserFile` → `device_commit_files` a
   `C:\JuegosUnity\UnityAI_Test\Alkahest\...` (uuids COPIADOS del resultado, jamás tecleados).
3. `device_bash` NO puede borrar (unlink bloqueado): para "borrar", `mv` a `_to_delete/`.
   `AssetDatabase.DeleteAsset` también está VETADO vía MCP RunCommand ("user interactions
   are not supported", R77): retirar assets = mv a `_to_delete/` + `AssetDatabase.Refresh()`.
   Git en el PC: scripts `.cmd` de un solo uso que corre Cesar (Win+R o doble clic).
   TODO git de lectura vía device_bash lleva `--no-optional-locks` (un `git status` normal
   deja un index.lock que el puente no puede borrar y el push de Cesar revienta — pasó en
   la R74; el lock huérfano se quita con `mv` a _to_delete/).
4. Permisos de computer-use caducan solos: re-pedir con `computer_resolve_access` +
   `computer_request_access` (entradas verbatim). Cesar los concede al vuelo.
5. Probar EN VIVO vía Unity MCP: Refresh → esperar → 0 errores → Play. OJO (R127): el editor
   de Cesar tiene AUTO-REFRESH APAGADO — tras cada deploy, `AssetDatabase.Refresh()` +
   `CompilationPipeline.RequestScriptCompilation()` y VERIFICAR por reflexión que el tipo/símbolo
   nuevo existe ANTES de Play (si no, se prueba el assembly viejo sin un solo error). → sondas por RunCommand
   (`internal class CommandScript : IRunCommand`, método `Execute(ExecutionResult)`; tipos por
   reflexión `Type.GetType("Alkahest.Game.X, Alkahest.Runtime")`; BindingFlags numéricos
   `(System.Reflection.BindingFlags)(4|8|16|32)` — PROHIBIDO `using System.Reflection`).
   `Application.runInBackground = true` siempre, y OJO: el Play por MCP puede quedar EN PAUSA
   (`EditorApplication.isPaused`) — comprobarlo antes de diagnosticar "no pasa nada".
   Capturas sin permisos de escritorio: RunCommand renderiza Camera.main a RenderTexture → PNG
   en la raíz del proyecto → `device_stage_files` (la IMGUI NO sale ahí: menús = captura de
   escritorio con permiso). Toda ronda visual se verifica JUGANDO con capturas (regla 52).
6. Commits frecuentes, mensajes en español. **6b: EL AGENTE NUNCA HACE PUSH** — Cesar corre
   `ca_playtestNN.cmd` (git add -A + commit + push). GitHub es LA VERDAD; el sandbox es
   VOLÁTIL (15+ reinicios ya, y puede amanecer en una COPIA VIEJA sin avisar — R127: un deploy
   desde un sandbox en R85 pisó 6 archivos más nuevos y truncó el HISTORIAL de un commit;
   recuperar del disco de Cesar con tar vía device_bash si GitHub está atrás): ANTES de
   cualquier deploy, `git fetch` y comparar el HEAD del sandbox con el del remoto/disco; el
   fix 69g casi se pierde por no llegar a un push. Documentar cada ronda en `docs/archivo/HISTORIAL_RONDAS.md` + generar
   el cmd nuevo SIEMPRE, sin excepción.
7. Compilador fiel del sandbox (`/home/claude/compile_fiel.sh` + 155 DLLs en
   `/home/claude/unityrefs`, staged desde `Builds/TenThousandYearsMulti/..._Data/Managed`):
   OBLIGATORIO EXIT=0 antes de todo deploy. Si el sandbox resetea: re-stagear las DLLs y
   renombrar la que tiene espacios ("SteamNetworkingSockets Transport…") a
   `SteamTransportNGO.dll` o el script revienta. No sustituye el arranque real en Unity
   (ILPP/escenas/runtime).

## Reglas del proyecto (números estables — el código las cita; anécdotas en el archivo)

- **R7** StaticSolid no cae, SALVO los `caeSolido` (productos/hielo/cristal) con cohesión por
  `cohesionCeldas`; piedra y obra del taller JAMÁS caen.
- **R8** El ring de eventos de `SimStepper` tiene TRES consumidores no destructivos con índice
  propio; nunca avanzar una cabeza compartida.
- **R9** En `DiffuseTemperature`: la guarda de ambiente cubre el 100% de las celdas y el
  redondeo es simétrico (`diff/4`, jamás `>>2` con negativos).
- **R10** `TryDeliverCell` devuelve `DeliveryOutcome`; el Favor solo se gana completando
  encargos.
- **R11** Al reescribir `DayCycle`, verificar vivo el cierre anticipado de jornada.
- **R12** ATAJOS: toda tecla comprueba `UiStyles.EscribiendoTexto`; las del mundo, además
  `JournalHud.Abierto`/`AlbumReal.Abierto`. Excepciones deliberadas: M y ESC.
- **R13/17/23** Dos clases de material: VOCABULARIO (nombre fijo, visual fijo `Liso/Neto`,
  opaco, igual en toda seed) y LO INNOMINADO (se bautiza; firma sorteada por seed, alfa 255).
  Encargos/pistas/Maestro describen por EFECTO/ORIGEN hasta el bautizo — jamás revelar
  identidades que el HUD muestra como "???".
- **R14** Las builds REGENERAN la escena antes de compilar; quitar `BuildOptions.Development`
  en la build de reparto (o F3 llega al jugador).
- **R15** DOCUMENTAR EN EL CÓDIGO las ideas retiradas (qué eran y por qué se fueron).
- **R16/42** `morph`: byte por celda; Vetas/Celdas posicionales (renderer, coste 0);
  Manchas/Laberinto/Dendritas/Pulso/Motas en `MorphTick` con DOBLE BÚFER OBLIGATORIO; viaja en
  `SwapCells`; nace con hash, nunca 0. Un campo solo NO hace Turing (backlog Gray-Scott de 2);
  ruido anclado con tick constante 0, nunca `_tick`.
- **R19** Borde `Difuso`: jamás bajar alfa (mosaico duro contra el backdrop); oscurecer hacia
  `BackgroundColor` en el contorno.
- **R20** Tras Safe Mode: regenerar la escena ("Ten Thousand Years/1") ANTES de investigar.
- **R21** `XorShift.FromCell(salt)` toma `uint`: expresión mixta = cast explícito.
- **R22/29** Lo que CREA materia usa `PaintStable` (nace a temperatura estable); `Paint` solo
  MUEVE materia con su temperatura. Ante "materia recién creada en estado imposible", mirar
  SIEMPRE primero a quién la crea.
- **R24/39** Calibrar contra medidas LEÍDAS de `SimLevelBuilder` en runtime, jamás contra
  prosa; patrones: ≥3 repeticiones en el recipiente más estrecho.
- **R26** Antes de desplegar: diff contra el remoto y DESCONFIAR de archivos que encogen —
  compilar no prueba que no se perdió nada (API y consumidores pueden morir juntos).
- **R27** Al recuperar trabajo viejo por fusión, revisar qué correcciones posteriores desharía.
- **R28** Panel de rótulo se desvanece al CUBO, texto lineal, umbral `AlfaMinimaVisible`=0.12.
- **R30** Descartar un sospechoso NO es identificar al culpable: la investigación termina en
  el mecanismo confirmado, o no ha terminado.
- **R31** El clima por zona se RETIRÓ (dos razones documentadas); `CellGrid.ambient` queda
  para el clima que cree el JUGADOR.
- **R32** Documentar la ronda no es opcional ni "lo último si alcanza el tiempo".
- **R33** INVARIANTE SAGRADA `Leyes[i]` ↔ `Reactions.At(i)`; solo crecen JUNTAS; assert de
  editor intocable.
- **R34** Las restricciones del sorteo de química (R1, R4 bidireccional, MaterialesDeGrifo,
  MaterialesSoloCatalizador) protegen la partida: relajar una exige re-correr el modelo
  (20.000 seeds, 0 escasez medida).
- **R35** Generar variedad no basta: tiene que poder FORMULARSE EN UNA FRASE (afinidad del
  universo) o no se puede bautizar.
- **R36** `BuildVisual()`/`Init()` NO son idempotentes; mover = `Reposicionar` (IMovible),
  jamás re-Init.
- **R37** Frasco/cincel/mudanza EXCLUYENTES con guardas SIMÉTRICAS; un cuarto modo entra por
  `Mudanza.ForzarSalida()`.
- **R38** Si el jugador puede romper algo en silencio: abaratar el deshacer (R = a fábrica),
  no quitar la herramienta.
- **R40/46** Modelar el AUTOBLOQUEO de todo mecanismo de crecimiento antes de escribirlo
  (tolerancia dendrítica 2-3, jamás 1; núcleo vital separado del alcance térmico).
- **R41** Antes de repartir encargos paralelos: ¿dónde vive LA MECÁNICA? (no el nombre del
  archivo). Si cruza frontera: entera a un encargo, o contrato entre dos.
- **R43** Cada cambio entregado lleva su gesto concreto y el número observable en pantalla —
  lo indistinguible de "no pasó nada" no ocurrió.
- **R44** Lo que el diseño espera que se GASTE experimentando es reponible; lo escaso, para
  atesorar.
- **R45** El rasgo de un individuo vive en la INSTANCIA (y continuo), no en su material.
- **R47/50** No reutilizar constantes de posición por el nombre; leer cómo CONSUME el motor un
  número antes de fijarlo (las unidades las define el consumidor).
- **R48** Todo eje de variación nuevo: verbo VISIBLE + consumidor REAL, o es ruido.
- **R49** Toda promesa de docblock tiene SU línea que la cumple; promesa sin línea = bug.
- **R51/57** Garantías procedurales sobre lo ENTREGABLE por el jugador; toda escalera con
  picker argmax se audita simulando el argmax; en seed de autor los peldaños van POR DECRETO;
  todo solver imprime su resultado en el log.
- **R52** Lo que cambia forma/plano/color se verifica JUGANDO con capturas antes de entregar.
- **R53** Textos de juego en ESPAÑOL LATINO neutro (tuteo; jamás os/vosotros/-ad; "tomar", no
  "coger").
- **R54** Los fracasos dejan evidencia forense (residuo + anotación); toda mecánica
  destructiva responde: ¿qué queda y qué enseña?
- **R55** Todo proceso con recursos por tick se demuestra MORTAL (nunca ganancia neta por
  evento frecuente) y DESPIERTO (`WakeChunk` sobre sí mismo).
- **R56** JAMÁS API de Unity en inicializadores estáticos (TypeInitializationException en
  cascada, invisible al compilador): centinela + carga perezosa.
- **R58** *(ronda 71b; variante 91)* Un `[SerializeField]` guardado en prefab/escena PISA el
  default del código: los números se afinan en código y SIN serializar; ante "cambié el valor
  y no cambió nada", grep al `.prefab`/`.unity`. VARIANTE HOT-RELOAD (R91): el ScriptableObject
  VIVO del editor serializa sus campos a través de las recompilaciones AUNQUE el asset del
  disco no los tenga — cambiar el default de un campo ya cargado NO llega al juego hasta
  reimportar o RENOMBRAR el campo (refillTope se clavó en 36 así). El renombre es la cura
  universal de las tres vías.
- **R60** *(R107, la regla de hierro del mundo)* LO HEREDADO SE REPARA, JAMÁS SE DESGUAZA:
  las ruinas dan recipientes y herramientas, nunca materiales a granel — si un escombro
  puede rasparse para obtener vidrio/metal, la economía y la tesis mueren juntas. Toda
  mecánica nueva de ruinas se audita contra esta línea ANTES de escribirse.
- **R59** *(ronda 69g)* Los flags estáticos de modo (`ModoFundacion`/`ModoSemillaCero`) se
  resetean en TODOS los caminos multi (host, snapshot del invitado, despawn): un flag pegado
  construye el universo equivocado sin un solo error. La consola imprime la "línea de la
  verdad" del mundo construido — compararla entre lados ante cualquier desync.

## El laboratorio de leyes (R130, en curso — Opus 5 implementa)

Segunda galería = sandbox de investigación de la hipótesis "el conocimiento sustituye al
trabajo manual". TODO vive en `docs/LAB/` (leer `CHECKPOINT.md` primero, luego
`HANDOFF_OPUS.md`) y en `Laboratorio/` (capturas, presets, benchmarks). Código: partials
`*.Laboratorio.cs` + `Game/LabPanel.cs` + `Sim/LabParams.cs`, gateado por
`SimStepper.LabActivo`. Regla operativa nueva: **jamás editar un `.cs` con el editor en Play**
(el RunCommand recompila y recarga el dominio).

## Estado en una línea

EL FOCO: la CAPA VISUAL del mundo de ruinas amables (GDD §0: decreto R107) — personaje
muñeco de remiendos vía pipeline 3D→sprites (docs/DIRECCION_DE_ARTE.md), cámara más
cerca, y cerrar la demo Era I (2 máquinas: hoyo y horno). El diseño mayor está SELLADO;
retiradas en ESTADO.md. Después: renombre/poda y la escenificación para el hermano de
Cesar. Backlog completo: `docs/ESTADO.md`.
