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
  la TEMPERATURA de lo aspirado; TODA mutación del grid vía `AlkahestSim.Paint`/`PaintCell`;
  BLOQUEO DE MATERIAL al pulsar aspirar, más el haz de mundo — el anillo de alcance que lo
  acompañaba se retiró en el playtest 11, ver regla 15), máquinas (`HeatPlate`/`ChillStone`/`Dispenser`/`StorageRack`) con sprites generados en
  `MaquinariaSprites` y foco de interacción arbitrado por `MachineFocus` (solo el aparato más
  cercano responde a E), `SubstanceKnowledge` (descubrir/bautizar/observaciones; dos clases de
  material, ver regla 12), `OrderSystem`+`DeliveryChute` (pedidos por EFECTO, Favor),
  `MasterSupplies` (muestras de la jornada 2: azoth/vivium/semilla), `HintSystem` (pistas por
  jornada, una línea ejecutable cada una), `JournalHud` (el diario: libro a pantalla completa,
  `GUI.depth = -1000`, propiedad `Abierto`), `UiStyles` (estilo IMGUI compartido; propiedad
  `EscribiendoTexto`, ver regla 12), `DayCycle` (Título→3 jornadas→final, seed vía
  `AlkahestSim.NextRunSeed`).
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
6b. **EL REPO DE GITHUB ES LA FUENTE DE VERDAD; EL SANDBOX ES VOLÁTIL (playtest 11)**: el sandbox
    de trabajo en la nube se ha reiniciado a mitad de sesión y ha perdido la copia de trabajo
    entera, revirtiéndola a un snapshot de rondas atrás. Ante cualquier duda sobre el estado del
    código, comparar contra un clon fresco de GitHub antes de editar, y no acumular varias rondas
    de trabajo sin commit — un commit reciente es la única red de seguridad real.
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
12. **REGLA DE ATAJOS (playtest 10)**: un campo de texto IMGUI se come TODAS las letras. Todo
    atajo de una sola tecla debe comprobar `UiStyles.EscribiendoTexto` (propiedad estática, la
    sube/baja `NamingUi`; sigue en `true` un frame extra tras cerrarse para que el atajo tampoco
    dispare en el frame en que se confirma con Enter). Los atajos del MUNDO (E, WASD/flechas, Q,
    clics de aspirar/verter/redomas, F3/P/N) deben comprobar ADEMÁS `JournalHud.Abierto`. Las dos
    únicas excepciones deliberadas: **M** (silenciar, `Audio/DirectorDeAudio`, solo
    `EscribiendoTexto` — es una preferencia del jugador, no una acción de juego) y **ESC** (cierra
    lo que esté abierto, es universal). Tabla completa de atajos y archivos: `docs/HANDOFF.md`
    sección "Playtest 10". `ApprenticeController` es la ÚNICA excepción NO deliberada: no
    comprueba `DayCycle.InputLocked` (hueco preexistente, anotado, sin corregir).
13. **DOS CLASES DE MATERIAL (playtest 10)**: `SubstanceKnowledge.NombreComun` es la única fuente
    de verdad. VOCABULARIO DEL TALLER (Stone/Sand/Water/Oil/Nutrient + fenómenos mundanos
    Steam/Smoke/Fire/Ash/Ice) tiene nombre desde el día 1 — nadie lo bautiza. LO INNOMINADO
    (Azoth/CrystalSeed/Crystal/Vivium/Slime/Acid) enseña "???" hasta que el jugador lo bautiza.
    **Los encargos (`OrderSystem`) describen por EFECTO/ORIGEN mientras el material siga
    innominado y pasan a usar el nombre del jugador al bautizar**: el recálculo lo dispara
    `SubstanceKnowledge.NamingVersion` en `Update()`; como `Order.Descripcion` es readonly, se
    sustituye la instancia `Order` entera, nunca se construyen strings en `OnGUI`. Mismo criterio
    en pistas, banners de "LEY DESCUBIERTA" y texto del Maestro: describir por origen/lugar, nunca
    revelar la identidad interna de algo que el HUD todavía enseña como "???" (la misma
    circularidad que ya se corrigió una vez, no reintroducirla).
14. **REGLA DE BUILD (playtest 11)**: `AlkahestBuildTools.BuildDemoWindows()` REGENERA la escena
    (`AlkahestSceneBuilder.GenerateLabScene()`) antes de compilar — nunca confiar en que el `.unity`
    guardado en el repo esté al día (una escena vieja se coló sin avisar durante cinco rondas,
    salvada solo por `SimRenderer.FitMainCamera()`). Build actual usa
    `BuildOptions.Development | ShowBuiltPlayer` (Player.log + F3 activa para verificar); **quitar
    `BuildOptions.Development` antes de la build de reparto** o F3 llega al jugador. La checklist
    para validar el `.exe` vive en `docs/HANDOFF.md` sección "Playtest 11" — reutilizarla en cada
    build futura, no reinventarla.
15. **DOCUMENTAR EN EL CÓDIGO LAS IDEAS DESCARTADAS, no solo las que se quedan** (playtest 11): al
    quitar el anillo de alcance de `Flask.cs` se dejó un párrafo en la cabecera de la clase
    explicando qué era y por qué se retiró, para que nadie lo reimplemente pensando que es una idea
    nueva. Práctica del proyecto de aquí en adelante.

## Estado (última sesión) y prioridades
HECHO: M1 sim ✅ · M2 interacción ✅ · M3 leyes/reacciones/cultivo ✅ · M4 loop completo ✅ ·
M5 parcial: audio (`Audio/SintetizadorSfx`+`DirectorDeAudio`) y aprendiz rediseñado (imp), SIN
VERIFICAR en editor. Playtest 11 (Opus 5 dirige, Sonnet 5 escribe en 2 encargos): VALIDADO por
Cesar el punto de luz del haz, el bloqueo de material y el rótulo del frío (cierra el pendiente del
playtest 10); anillo de alcance del frasco RETIRADO por petición del jugador (el haz y el bloqueo
se quedan, regla 15 arriba); pre-vuelo de la build de Windows (regla 14 arriba): el builder ahora
regenera la escena antes de compilar — se encontró que la escena guardada llevaba la cámara vieja
del rediseño del playtest 4, salvada sin que nadie lo supiera por `SimRenderer.FitMainCamera()`;
build aún sin EJECUTAR ni verificar el `.exe`. Reconocida la falta de curva de dificultad como
deuda de diseño (no un ajuste de números): el balance del día 3 está calibrado para la velocidad de
prueba de Cesar, no para un jugador nuevo — falta una ronda de progresión con jornadas cortas.
PENDIENTE (orden): 1) verificar en Unity que compila y jugar las 3 jornadas completas; 2) ejecutar
la build de Windows y validarla con la checklist (`docs/HANDOFF.md` sección "Playtest 11"); 3)
enganchar `HintSystem.PistasMostradas` en la sección PROCEDIMIENTOS del diario (API ya existe, sin
consumidor); 4) decidir si el audio se queda o se apaga (`DirectorDeAudio.SistemaActivo`); 5)
CURVA DE PROGRESIÓN — jornadas cortas de una mecánica cada una; 6) renombrar repo GitHub
`Alkahest`→`ChaosAlchemy` + `productName`; 7) replantear las redomas (`StorageRack`, sugerencia de
Cesar); 8) resto de M5 (glow, agua con más cuerpo); 9) multiplayer: sim solo-host + deltas RLE por
chunks despiertos a 10-15Hz — MEDIR antes de decidir (plan en HANDOFF). Detalle completo de la
ronda 11: `docs/HANDOFF.md` sección "Playtest 11".
