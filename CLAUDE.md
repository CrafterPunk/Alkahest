# ChaosAlchemy — contexto para agentes (LÉEME PRIMERO)

Juego Unity de alquimia emergente: simulación celular estilo Noita + descubrir/nombrar/domesticar
las leyes de un universo distinto por seed. Derivado del template `FriendsLoop-Unity-Steam-Template`
(multiplayer Steam listo pero AÚN NO integrado con la sim). Visión completa: `docs/DECISIONS.md`.
Estado detallado y siguientes pasos: `docs/HANDOFF.md`. Detalles de la sim: `docs/SIM_NOTES.md`.

## Mapa del código (Assets/Alkahest/)
- `Sim/` — autómata celular DETERMINISTA (grid **256x144**, 30Hz): `Universe` (materiales + leyes
  por seed + Edictos + sorteo de FIRMA VISUAL, ver regla 16), `SimStepper` (reglas por arquetipo +
  `MorphTick` que evoluciona el campo morfológico + ring buffer de eventos), `ReactionEngine`
  (tabla de reacciones), `CellGrid` (incluye `byte[] morph`/`morphScratch`, ver regla 16),
  `SimRenderer` (textura + sprite; también dibuja el patrón/borde de la firma), `SimLevelBuilder`
  (**EL PLANO del taller: única fuente de verdad de TODAS las coordenadas**).
  REGLA DE ORO: nada de UnityEngine.Random ni allocs en el hot path; solo `XorShift` sembrado
  por (tick,x,y). El determinismo es el plan para el futuro netcode.
  Mundo = 25.6 x 14.4 unidades; cámara ortográfica centrada en (12.8, 7.2) con size 7.2 —
  el `AlkahestSceneBuilder` lo deriva de `CellGrid.W/H`, nunca hardcodeado.
  `MaterialDef` lleva la FIRMA VISUAL de cada sustancia (playtest 12): `patron`
  (`PatronMorfologico`: Liso/Vetas/Manchas/Laberinto/Celdas/Dendritas/Pulso/Motas), `borde`
  (`BordeMorfologico`: Neto/Halo/Escarcha/Difuso), `patronEscala`, `patronFuerza`, `ritmoAnim`,
  `emision`, `semillaPatron`.
- `Game/` — capa jugable: `ApprenticeController` (imp volador), `Flask` (aspirar/verter, conserva
  la TEMPERATURA de lo aspirado; TODA mutación del grid vía `AlkahestSim.Paint`/`PaintCell`;
  BLOQUEO DE MATERIAL al pulsar aspirar, más el haz de mundo — el anillo de alcance que lo
  acompañaba se retiró en el playtest 11, ver regla 15), máquinas (`HeatPlate`/`ChillStone`/`Dispenser`/`StorageRack`) con sprites generados en
  `MaquinariaSprites` y foco de interacción arbitrado por `MachineFocus` (solo el aparato más
  cercano responde a E). `HeatPlate`/`ChillStone` (playtest 14): `FootprintFraction`=0.4 recorta el
  ancho recibido del bootstrap a una fracción centrada ANTES de `BuildVisual` (placas más pequeñas,
  el centro no se mueve); al apagarse, la fila adyacente sigue empujada débilmente
  (`HoldStepRaw`=1) hacia el último objetivo durante `HoldTicksTrasApagar`=60 ticks mientras las
  filas exteriores se sueltan de inmediato, para que el aparato sea lo último en normalizarse
  (`ApplyHoldTick`, en ambos archivos por simetría). `SubstanceKnowledge` (descubrir/bautizar/
  observaciones; dos clases de material, ver regla 12), `OrderSystem`+`DeliveryChute` (pedidos por
  EFECTO, Favor), `MasterSupplies` (muestras de la jornada 2: azoth/vivium/semilla), `HintSystem`
  (pistas por jornada, una línea ejecutable cada una), `JournalHud` (el diario: libro a pantalla
  completa, `GUI.depth = -1000`, propiedad `Abierto`), `UiStyles` (estilo IMGUI compartido;
  propiedad `EscribiendoTexto`, ver regla 12; el panel de un rótulo se desvanece al CUBO y el texto
  lineal, umbral `AlfaMinimaVisible`=0.12, ver regla 26), `DayCycle` (Título→3 jornadas→final, seed
  vía `AlkahestSim.NextRunSeed`).
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
16. **EL CONTRATO DE `morph` (playtest 12, campo morfológico)**: `CellGrid.morph`/`morphScratch`
    son un byte de intensidad 0..255 por celda que `SimRenderer` traduce a desplazamiento de brillo
    escalado por `patronFuerza`. `Liso` no lo usa. **`Vetas` y `Celdas` son PURAMENTE
    POSICIONALES: `SimStepper.MorphTick` NO las toca, las calcula `SimRenderer` con hashes de
    `(x,y,tick)`** — coste cero en el stepper. `Manchas`/`Laberinto` = concentración de
    reacción-difusión (leen vecinos); `Dendritas` = fuerza de rama; `Pulso` = fase; `Motas` =
    intensidad de chispa. **El doble búfer `morphScratch` es OBLIGATORIO** para las familias que
    leen vecinos: leer y escribir el mismo array daría un resultado dependiente del orden de
    recorrido, rompiendo el determinismo del que depende el netcode futuro (`Array.Copy` de ida al
    empezar `MorphTick`, de vuelta al final; todas las familias escriben solo en `morphScratch`).
    `morph` viaja CON la sustancia en `CellGrid.SwapCells` (un líquido que fluye arrastra su
    dibujo) y nace sembrado con un hash de `(idx, material)` en `SetCell`, nunca a cero. Detalle
    técnico completo (parámetros por familia, estriado, chunks dormidos, modos de crecimiento):
    `docs/SIM_NOTES.md`.
17. **SOLO VARÍA LO INNOMINADO (playtest 12)**: la firma visual (`Universe.Create` →
    `SortearFirmasVisuales`) solo se sortea para Azoth/CrystalSeed/Crystal/Vivium/Slime/Acid. El
    VOCABULARIO DEL TALLER (Water/Sand/Oil/Nutrient/Stone/Fire/Smoke/Ash/Steam/Ice) se ve SIEMPRE
    igual (`patron=Liso`, `borde=Neto`) en toda partida — mismo criterio de circularidad que la
    regla 13. Si todo cambia por seed, nada se reconoce.
18. **`NamingUi.Open()` YA NO EXISTE (playtest 12): es `TryOpen()`**, y SIEMPRE responde al
    jugador vía `Flask.Avisar` (nunca `return` mudo) distinguiendo apuntar a nada / apuntar a una
    redoma de `StorageRack` (no vive en la grilla de la sim, siempre resuelve `Empty`) / apuntar a
    vocabulario del taller (no se bautiza, regla 13/17).
19. **TRAMPA DEL BORDE `Difuso` (playtest 12, para quien toque `SimRenderer`)**: NO bajar el alfa
    para simular un borde deshilachado. El sim es 1 téxel/celda en `FilterMode.Point` sobre otra
    textura Point a triple resolución detrás (`WorkshopBackdrop`); un téxel semitransparente
    produce un mosaico duro del fondo en bloques de ~7,5 px, que se lee como bug de recorte, no
    como deshilachado. La solución correcta es oscurecer hacia `BackgroundColor` en una fracción de
    las celdas de contorno (ver `SimRenderer.ComputeCellColor`, caso `BordeMorfologico.Difuso`).
20. **TRAS SALIR DE SAFE MODE, REGENERAR LA ESCENA (playtest 13)**: los componentes de
    `AlkahestLab.unity` quedan sin script asignado y el juego arranca sin mostrar nada. Primer
    reflejo: **Alkahest → 1. Generar escena Lab** (idempotente) antes de investigar nada más.
21. **`XorShift.FromCell` toma `uint` como `salt` (playtest 13)**: una llamada con una constante
    literal (`77`, `205`...) convierte implícitamente; una expresión que MEZCLE una constante con
    un campo (p. ej. `201 + def.semillaPatron`, donde el campo es `byte` y se promueve a `int`)
    necesita cast explícito `(uint)(...)` — de `int` a `uint` no hay conversión implícita
    (`CS1503`, visto en `SimStepper.cs`).
22. **`AlkahestSim.PaintStable(x, y, radius, materialId)` es el camino para pintar materia de la
    nada (playtest 13, solo `DevPalette`)**: la celda nace a `StableBirthTempRaw(MaterialDef)`,
    una temperatura en la que el material es ESTABLE (evita el bug de "pintar hielo produce
    agua": `Paint`/`SetCell` nunca tocan `temp`, y la celda heredaba ambiente=70raw, que cruza
    siempre `Ice.meltsAt` en cualquier seed). `Paint`/`PaintCell`/`PaintRect` NO cambiaron de
    firma ni de comportamiento — siguen siendo los que usa el juego real (Flask, MasterSupplies,
    DeliveryChute, Dispenser).
23. **LO INNOMINADO NACE OPACO (playtest 13, amplía la regla 19)**: `alfa = 255` siempre en
    `Universe.SortearFirmasVisuales`, sin excepción. El alfa <255 del roster (215-235 en Azoth/
    Acid/Slime) es vocabulario del taller y NO debe propagarse a la firma sorteada — si se
    propaga, toda la masa del líquido queda semitransparente contra `WorkshopBackdrop` (mismo
    mosaico duro que advierte la regla 19, pero en la sustancia entera, no solo el contorno).
24. **UN PATRÓN SE RECONOCE POR SU REPETICIÓN, NO POR SU TAMAÑO (playtest 13)**: al calibrar
    `patronEscala` (Vetas/Celdas en `SimRenderer`), medir contra el tamaño real de los
    recipientes de `SimLevelBuilder` (cuba interior 52x37, bandeja fría interior 46x6) y apuntar
    a 3-4 repeticiones mínimo en el recipiente más estrecho — no a evitar "ruido" a ciegas.
25. **DEUDA TÉCNICA: `Game/StorageRack.cs::FirmaVisualFabrica` duplica el generador de patrones
    de `JournalHud`** (playtest 13, ambos archivos de propiedad disjunta en la ronda que la creó).
    Consolidar en un `Game/FirmaVisualFabrica.cs` compartido en una ronda futura.
26. **ANTES DE DESPLEGAR, `git diff` CONTRA EL REMOTO Y DESCONFIAR DE TODO ARCHIVO QUE ENCOJA
    (playtest 14, la regla más importante de la ronda)**: el sandbox de trabajo en la nube se
    reinició a mitad del playtest 10 y los agentes desplegaron una copia obsoleta encima de lo
    bueno — `Dispenser.cs` pasó de 26.866 a 18.186 bytes (y `ChillStone.cs`/`HeatPlate.cs`/
    `UiStyles.cs` igual) sin que nadie lo notara durante TRES rondas, porque **el proyecto seguía
    compilando**: se perdieron a la vez la API y todos sus consumidores. Un archivo que pierde
    miles de bytes sin que el cambio lo justifique es una regresión hasta que se demuestre lo
    contrario. Que compile NO es señal de que no se ha perdido nada.
27. **AL RECUPERAR TRABAJO ANTIGUO CON UNA FUSIÓN, REVISAR QUÉ DECISIONES DE ESE TRABAJO SE
    CORRIGIERON DESPUÉS (playtest 14)**: una fusión a tres bandas puede deshacer correcciones ya
    validadas. Caso real: el rótulo del frío se ancló ARRIBA en el playtest 7, se corrigió a ABAJO
    y Cesar lo validó en el playtest 13, y la restauración de la regresión (regla 26) lo volvió a
    invertir a ARRIBA sin querer — se corrigió una segunda vez, con la cronología completa dejada
    en el header de `ChillStone.cs` para que no gire una tercera.
28. **EN `UiStyles`, EL PANEL DE UN RÓTULO SE DESVANECE AL CUBO Y EL TEXTO LINEAL (playtest 14)**:
    umbral de no-dibujar `AlfaMinimaVisible`=0.12 (`PlacaMundo`/`PlacaMundoLateral`/`Globo`). Si
    ambos se desvanecen igual (lineal), un panel casi negro sobrevive perceptualmente mucho más
    que un texto claro y queda una caja negra vacía sin texto — el bug real detrás de "recuadros
    negros flotando en el taller" y "la etiqueta en negro antes de desaparecer".

## Estado (última sesión) y prioridades
HECHO: M1 sim ✅ · M2 interacción ✅ · M3 leyes/reacciones/cultivo ✅ · M4 loop completo ✅ ·
M5 parcial: audio (`Audio/SintetizadorSfx`+`DirectorDeAudio`) y aprendiz rediseñado (imp), SIN
VERIFICAR en editor. Playtest 12: campo morfológico completo. Playtest 13: afinó esa base (Safe
Mode, `PaintStable`, insumos/subproductos, remapeo de `patronEscala`, firma GENERADA, estado
Fresca). **Playtest 14 (Opus 5 dirige, Sonnet 5 escribe en 2 encargos), ronda de recuperación,
todo VALIDADO por Cesar en el editor**: se rastreó y recuperó (fusión a tres bandas) la regresión
del playtest 7 perdida sin detectar durante tres rondas — máquinas + `UiStyles.PlacaMundoLateral`/
`Cercania` (reglas 26-27); se corrigieron tres regresiones producidas por la propia fusión
(recuadros negros por el desvanecimiento de panel sin cubo, regla 28; rótulo del frío invertido
otra vez); Helando ahora se diferencia de Fresca (12 vs 5 raw/tick); el alcance térmico decae con
la distancia en vez de cortarse en seco (`FilaEmpujePct`); orden de recuperación con `HoldTicks
TrasApagar`; placas más pequeñas (`FootprintFraction`). Y una CONVERSACIÓN DE DIRECCIÓN clave:
Cesar diagnosticó "falta morfología de comportamiento, no solo de aspecto" — trece rondas
enriqueciendo cómo el juego SE LEE sobre una capa de sistemas delgada. Detalle técnico completo:
`docs/HANDOFF.md` sección "Playtest 14" y `docs/SIM_NOTES.md`.
FASE NUEVA acordada (orden): 1) cámara que sigue al aprendiz; 2) taller a 2-3 pantallas
(rediseño de `SimLevelBuilder` con las zonas de Cesar + tres pasadas conscientes del viewport,
hoy proporcionales al mundo entero: refresco completo, `MorphTick`, `DiffuseTemperature`);
3) química generada por semilla (núcleo fijo + reacciones sorteadas + "leyes descubiertas: N de
M" en el diario); 4) comportamiento variable por semilla, no solo aspecto (de ahí nacen los
nombres que Cesar busca); 5) taller movible (grifos/estantes/placas anclados a bedrock);
6) mundo persistente con semilla y progreso guardado.
Backlog heredado aún vigente: consolidar `FirmaVisualFabrica` con `JournalHud` (regla 25);
enganchar `HintSystem.PistasMostradas` en PROCEDIMIENTOS del diario; decidir si el audio se queda;
renombrar repo GitHub `Alkahest`→`ChaosAlchemy` + `productName`; resto de M5 (glow, agua con más
cuerpo); ejecutar la build de Windows con la checklist (`docs/HANDOFF.md` sección "Playtest 11");
CURVA DE PROGRESIÓN; multiplayer (sim solo-host + deltas RLE, el formato debe contemplar `morph`,
regla 16); medir el coste real de `MorphTick` (más urgente con la fase 2, que multiplica el
tamaño del mundo 6x).
