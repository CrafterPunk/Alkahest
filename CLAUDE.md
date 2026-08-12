# ChaosAlchemy — contexto para agentes (LÉEME PRIMERO)

Juego Unity de alquimia emergente: simulación celular estilo Noita + descubrir/nombrar/domesticar
las leyes de un universo distinto por seed. Derivado del template `FriendsLoop-Unity-Steam-Template`
(multiplayer Steam listo pero AÚN NO integrado con la sim). Visión completa: `docs/DECISIONS.md`.
Estado detallado y siguientes pasos: `docs/HANDOFF.md`. Detalles de la sim: `docs/SIM_NOTES.md`.

## Mapa del código (Assets/Alkahest/)
- `Sim/` — autómata celular DETERMINISTA (grid **256x144**, 30Hz): `Universe` (materiales + leyes
  por seed + Edictos), `SimStepper` (reglas por arquetipo + ring buffer de eventos),
  `ReactionEngine` (tabla de reacciones), `CellGrid`, `SimRenderer` (textura + sprite),
  `SimLevelBuilder` (**EL PLANO del taller: única fuente de verdad de TODAS las coordenadas**).
  REGLA DE ORO: nada de UnityEngine.Random ni allocs en el hot path; solo `XorShift` sembrado
  por (tick,x,y). El determinismo es el plan para el futuro netcode.
  Mundo = 25.6 x 14.4 unidades; cámara ortográfica centrada en (12.8, 7.2) con size 7.2 —
  el `AlkahestSceneBuilder` lo deriva de `CellGrid.W/H`, nunca hardcodeado.
- `Game/` — capa jugable: `ApprenticeController` (imp volador), `Flask` (aspirar/verter, conserva
  la TEMPERATURA de lo aspirado; TODA mutación del grid vía `AlkahestSim.Paint`/`PaintCell`),
  máquinas (`HeatPlate`/`ChillStone`/`Dispenser`/`StorageRack`) con sprites generados en
  `MaquinariaSprites` y foco de interacción arbitrado por `MachineFocus` (solo el aparato más
  cercano responde a E), `SubstanceKnowledge` (descubrir/bautizar/observaciones),
  `OrderSystem`+`DeliveryChute` (pedidos por EFECTO, Favor), `MasterSupplies` (muestras de la
  jornada 2: azoth/vivium/semilla), `HintSystem` (pistas por jornada),
  `DayCycle` (Título→3 jornadas→final, seed vía `AlkahestSim.NextRunSeed`).
- `Audio/` — `SintetizadorSfx` (fábrica estática de `AudioClip` por código, cero assets: ruido,
  filtros, ondas, envolventes) y `DirectorDeAudio` (MonoBehaviour: pool fijo de voces, limitador
  de ritmo por tipo de evento, tecla M para silenciar, interruptor `SistemaActivo`).
- `Dev/DevPalette.cs` — F3: pintar materiales, pausa/step, seed, hover info. Solo editor/dev builds.
- `Editor/AlkahestSceneBuilder.cs` — menú "Alkahest/1. Generar escena Lab" (idempotente).
- `Assets/FriendsLoop/` — infraestructura multiplayer del template. NO TOCAR salvo integración.

## Reglas de trabajo en este entorno (Cowork + PC del usuario)
1. Unity 6.5 EXACTO: Input System nuevo SOLO (`Keyboard.current`); `FindAnyObjectByType` (no
   FirstObjectByType ni FindObjectsByType(FindObjectsSortMode)); ids constantes en GUILayout.Window;
   atributo Rpc: `InvokePermission = RpcInvokePermission.X` (RequireOwnership está deprecado).
2. Editar código EN EL SANDBOX cloud y desplegarlo con SendUserFile → device_commit_files a
   `C:\JuegosUnity\UnityAI_Test\Alkahest\...` (COPIAR uuids EXACTOS del resultado; no teclearlos).
3. El VM montado (device_bash) NO puede borrar ni reescribir refs de git (unlink bloqueado):
   para git usar scripts .cmd de un solo uso en la raíz del proyecto ejecutados con Win+R
   (`C:\...\script.cmd`), o pedírselo al usuario (se ofreció: darle instrucciones y él ejecuta).
4. Computer Use: los permisos de apps SE REINICIAN cada poco — re-solicitar Unity/Explorer con
   resolve+request (pasar entradas verbatim). La ventana de Git GUI pertenece a `wish.exe`.
   Terminales/IDEs son solo-clic: no se puede escribir en ellos; Win+R sí funciona (con .cmd, las
   comillas se pierden: usar scripts, no comandos con rutas con espacios).
5. Probar: abrir Unity (`launch.cmd` si está cerrado), Ctrl+R recompila, menú Alkahest→escena,
   Play; consola abajo. El usuario prueba con gusto — pedirle feedback funciona.
6. Commits frecuentes; mensajes descriptivos en español; push a `CrafterPunk/Alkahest`
   (renombrar repo a `ChaosAlchemy` está pendiente — hacerlo en GitHub Settings y
   `git remote set-url` después).
7. **`StaticSolid` no cae**: `SimStepper` no le aplica gravedad (Cristal, Hielo). Toda mecánica
   que dependa de que la materia baje sola tiene que arrastrarla ella misma (ver
   `DeliveryChute.ArrastreTick`, playtest 8).
8. El ring buffer de eventos de `SimStepper` (`Events`/`EventHead`) lo consumen ya TRES clases,
   cada una de forma **NO destructiva** con su propio índice (`SubstanceKnowledge`,
   `Audio/DirectorDeAudio`, y la lógica de "LEY DESCUBIERTA" dentro de `SubstanceKnowledge`, ver
   playtest 9). Nunca avanzar una cabeza compartida.
9. **Al tocar la difusión de temperatura** (`SimStepper.DiffuseTemperature`), comprobar SIEMPRE que
   la guarda del tirón hacia ambiente cubre el 100% de las celdas (no solo un offset fijo) y que el
   redondeo es simétrico en signo (`diff / 4`, NUNCA `diff >> 2` con posibles negativos) — dos bugs
   de deriva de temperatura sin límite salieron justo de ahí (playtest 9).
10. **`OrderSystem.TryDeliverCell` devuelve `DeliveryOutcome`** (`Progressed`/`OrderAlreadyComplete`/
    `NoMatch`), no `bool`. **El Favor solo se gana completando encargos**; el único gasto es
    `Dispenser.favorCostPerActivation`. No reintroducir Favor por "chatarra" (ver playtest 9).
11. **Al reescribir `DayCycle`**, verificar que sigue vivo el cierre anticipado de jornada
    (`UpdateAllOrdersDoneEarlyClose`, disparado por `OrderSystem.AllOrdersCompleted()`) — ya se
    perdió una vez al introducir los cuatro desenlaces (playtest 8→9).

## Estado (última sesión) y prioridades
HECHO: M1 sim ✅ · M2 interacción ✅ · M3 leyes/reacciones/cultivo ✅ · M4 loop completo ✅ ·
M5 parcial: audio (`Audio/SintetizadorSfx`+`DirectorDeAudio`) y aprendiz rediseñado (imp), SIN
VERIFICAR en editor. Playtest 9 (Opus 5 dirige, Sonnet 5 escribe en 4 encargos paralelos):
enseñanza de "las muestras del Maestro son SEMILLAS que no se gastan, no ingredientes" (momento
LEY DESCUBIERTA + sección LEYES del diario, ver reglas 8/9 arriba... y HANDOFF); dos bugs de
aritmética en la difusión de temperatura que hacían el frío "infinito" (regla 9 arriba); camino
placa→aceite→fuego reparado (no había autoignición por temperatura) y fuego pintado con F3 ya no
se ve gris; Favor SOLO por completar encargos, sin "chatarra" paralela (regla 10 arriba,
`OrderSystem.DeliveryOutcome`); restaurado el cierre anticipado de jornada que se había perdido
(regla 11 arriba); limpieza de audio (popeo por costura de bucle/DC offset/saturación/salto de M,
timbre del agua y del fuego corregidos); ala delantera del aprendiz ya no tapa el cuerpo.
PENDIENTE (orden): 1) verificar en Unity que compila y jugar las 3 jornadas completas; 2)
comprobar que el texto largo del Maestro cabe en el panel de la jornada 2; 3) decidir si el audio
se queda o se apaga (`DirectorDeAudio.SistemaActivo`); 4) build Windows limpia (nunca verificada
desde el rediseño del espacio); 5) renombrar repo GitHub `Alkahest`→`ChaosAlchemy`; 6) replantear
las redomas (`StorageRack`, sugerencia de Cesar); 7) resto de M5 (glow, agua con más cuerpo);
8) multiplayer: sim solo-host + deltas RLE por chunks despiertos a 10-15Hz — MEDIR antes de
decidir (plan en HANDOFF). Detalle completo de la ronda 9: `docs/HANDOFF.md` sección "Playtest 9".
