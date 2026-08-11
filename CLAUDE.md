# ChaosAlchemy — contexto para agentes (LÉEME PRIMERO)

Juego Unity de alquimia emergente: simulación celular estilo Noita + descubrir/nombrar/domesticar
las leyes de un universo distinto por seed. Derivado del template `FriendsLoop-Unity-Steam-Template`
(multiplayer Steam listo pero AÚN NO integrado con la sim). Visión completa: `docs/DECISIONS.md`.
Estado detallado y siguientes pasos: `docs/HANDOFF.md`. Detalles de la sim: `docs/SIM_NOTES.md`.

## Mapa del código (Assets/Alkahest/)
- `Sim/` — autómata celular DETERMINISTA (grid 384x216, 30Hz): `Universe` (materiales + leyes por
  seed + Edictos), `SimStepper` (reglas por arquetipo + ring buffer de eventos), `ReactionEngine`
  (tabla de reacciones), `CellGrid`, `SimRenderer` (textura + quad), `SimLevelBuilder` (taller).
  REGLA DE ORO: nada de UnityEngine.Random ni allocs en el hot path; solo `XorShift` sembrado
  por (tick,x,y). El determinismo es el plan para el futuro netcode.
- `Game/` — capa jugable: `ApprenticeController` (imp volador), `Flask` (aspirar/verter, TODA
  mutación del grid vía `AlkahestSim.Paint`), máquinas (`HeatPlate`/`ChillStone`/`Dispenser`),
  `SubstanceKnowledge` (descubrir/bautizar/observaciones), `OrderSystem`+`DeliveryChute` (pedidos
  por EFECTO, Favor), `DayCycle` (Título→3 jornadas→final, seed vía `AlkahestSim.NextRunSeed`).
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

## Estado (última sesión) y prioridades
HECHO: M1 sim ✅ · M2 interacción ✅ · M3 leyes/reacciones/cultivo ✅ · M4 loop completo
(título/pedidos/diario/jornadas) ✅ compilando y arrancando; fuego con color de llama propio y
shimmer de líquidos recién añadidos. PENDIENTE (orden): 1) probar jornada completa y balancear
(entregas, tiempos, Favor); 2) M5 presentación (fondo taller, sprites, glow — el agua aún "no se
siente agua": considerar shader de suavizado/metaballs); 3) build Windows (menú FriendsLoop ya
trae builder de referencia; crear equivalente Alkahest); 4) multiplayer: sim solo-host + deltas
RLE por chunks despiertos a 10-15Hz — MEDIR antes de decidir (plan en HANDOFF); 5) renombrar repo.
