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
  cercano responde a E; `MachineFocus.MostrarPromptE`/`RegistrarUsoE()` llevan el contador GLOBAL
  del tutorial de la tecla E — el prompt de texto se apaga tras 2 usos en cualquier aparato, y a
  partir de ahí el aparato enfocado se anuncia con un resalte dorado, no con texto),
  `SubstanceKnowledge` (descubrir/bautizar/observaciones), `OrderSystem`+`DeliveryChute` (pedidos
  por EFECTO, Favor), `MasterSupplies` (muestras de la jornada 2: azoth/vivium/semilla),
  `HintSystem` (pistas por jornada, navegables con flechas), `UiStyles` (paleta y estilos IMGUI
  compartidos por todo el HUD; primitivas de rótulo de mundo — `PlacaMundo`/`PlacaMundoLateral`
  para anclaje lateral y `Cercania` para opacidad por distancia, ver regla más abajo),
  `DayCycle` (Título→3 jornadas→final, seed vía `AlkahestSim.NextRunSeed`).
- `Dev/DevPalette.cs` — F3: pintar materiales, pausa/step, seed, hover info. Solo editor/dev builds.
- `Editor/AlkahestSceneBuilder.cs` — menú "Alkahest/1. Generar escena Lab" (idempotente).
- `Assets/FriendsLoop/` — infraestructura multiplayer del template. NO TOCAR salvo integración.

## Reglas de trabajo en este entorno (Cowork + PC del usuario)
1. Unity 6.5 EXACTO: Input System nuevo SOLO (`Keyboard.current`); `FindAnyObjectByType` (no
   FirstObjectByType ni FindObjectsByType(FindObjectsSortMode)); ids constantes en GUILayout.Window;
   atributo Rpc: `InvokePermission = RpcInvokePermission.X` (RequireOwnership está deprecado).
2. Editar código EN EL SANDBOX cloud y desplegarlo con SendUserFile → device_commit_files a
   `C:\JuegosUnity\UnityAI_Test\Alkahest\...` (COPIAR uuids EXACTOS del resultado; no teclearlos).
   El puente de archivos del dispositivo (`device_commit_files`/`device_bash`/`device_stage_files`)
   puede DESAPARECER a mitad de sesión aunque `get_device_info` siga respondiendo y muestre
   `connectedFolders` — no confiar en que "si conecta, funciona". PLAN B validado (playtest 6):
   empaquetar los archivos modificados en un ZIP con las rutas relativas del proyecto
   (`Assets/Alkahest/...`), mandarlo con `SendUserFile` junto a un `.cmd` que Cesar guarda en
   Descargas y ejecuta con doble clic. El `.cmd` hace `tar -xf "%ZIP%" -C
   "C:\JuegosUnity\UnityAI_Test\Alkahest"` (tar viene de serie en Windows 10+ y descomprime zip),
   borra `.git\index.lock` si existe, y hace `git add -A` + `commit` + `push`. IMPORTANTE: los
   mensajes de commit dentro de un `.cmd` deben ir en ASCII PURO (sin tildes ni ñ) por la página
   de códigos de la consola de Windows.
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
7. **`UiStyles.Oro` es color de UI, no de mundo.** Los objetos físicos del taller (grifos, tolva,
   maquinaria) son de LATÓN, no de oro puro: paleta RGB(168,126,58) medio / (214,176,96) luz /
   (86,62,28) sombra. `UiStyles.Oro` se reserva para brillos, destellos y resaltes de foco —
   señales de INTERFAZ, no materiales del mundo (regla que ya rompió el diseño original de la
   Tolva en el playtest 6 y se dejó fijada entonces).
8. **Los rótulos de aparatos en COLUMNA usan `UiStyles.PlacaMundoLateral`** (anclaje a un LADO del
   punto), nunca `PlacaMundo` con desplazamiento vertical. A 1 unidad de mundo de separación entre
   aparatos (los cinco grifos, p.ej.) cualquier offset vertical cae sistemáticamente sobre el
   vecino — es justo el bug que motivó `PlacaMundoLateral` en el playtest 7.

## Estado (última sesión) y prioridades
HECHO: M1 sim ✅ · M2 interacción ✅ · M3 leyes/reacciones/cultivo ✅ · M4 loop completo
(título/pedidos/diario/jornadas) ✅ · playtest 6 y 7 (Opus 5 dirigiendo, código de Sonnet 5),
detalle completo de ambos en `docs/HANDOFF.md` — PENDIENTES de verificación en Unity por Cesar.
Playtest 7 (8 puntos): chapas laterales permanentes por grifo (`PlacaMundoLateral`, ya no pisan al
vecino de columna), tutorial de la tecla E centralizado en `MachineFocus` (2 usos y pasa a un
resalte dorado del aparato enfocado), rótulo de la piedra gélida colgado del labio de la bandeja
en vez del bloque empotrado, tolva con el embudo antes invertido corregido + contenido que ya no
desaparece sin caer, avisos del frasco que se silencian tras 3 repeticiones (con destello mudo en
vez de texto), pistas navegables con flechas (modo manual, sin caducar), retextura del fondo
(Escala x3, Point, bisel de canto) y la causa raíz del etiquetado de redomas (`StorageRack` no
ocupa celdas de la sim; `NamingUi` consultaba el grid en vez de la redoma bajo el cursor).
PENDIENTE (orden): 1) **BALANCE de la partida completa** — máxima prioridad: en el playtest 7
Cesar iba con 149★ sobre meta 120 y la jornada seguía activa, con encargos de 150/100/220 celdas
en 1:35 de reloj (tamaños inalcanzables, la meta de Favor no cierra la partida); 2) replantear la
ubicación de las redomas cuando el balance esté hecho (Cesar sugirió bajarlas y usarlas para subir
el gameplay de reacciones — de momento solo se arregló el bug, sin rediseñar, a petición suya);
3) resto de M5 (glow, agua con más cuerpo, sprite del aprendiz, SFX); 4) verificar build limpia de
Windows; 5) renombrar repo `Alkahest`→`ChaosAlchemy`; 6) multiplayer: sim solo-host + deltas RLE
por chunks despiertos a 10-15Hz — MEDIR antes de decidir (plan en HANDOFF).
