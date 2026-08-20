# ChaosAlchemy — contexto para agentes (LÉEME PRIMERO)

Juego Unity de alquimia emergente: simulación celular estilo Noita + descubrir/nombrar/domesticar
las leyes de un universo distinto por seed. Derivado del template `FriendsLoop-Unity-Steam-Template`
(multiplayer Steam listo pero AÚN NO integrado con la sim). Visión completa: `docs/DECISIONS.md`.
Estado detallado y siguientes pasos: `docs/HANDOFF.md`. Detalles de la sim: `docs/SIM_NOTES.md`.

## Mapa del código (Assets/Alkahest/)
- `Sim/` — autómata celular DETERMINISTA (grid **768x288** desde el playtest 15 = 3x2 pantallas;
  `CellGrid.PantallaW/H` siguen midiendo 1 pantalla = 256x144 para pensar el plano en esa unidad; 30Hz): `Universe` (materiales + leyes
  por seed + Edictos + sorteo de FIRMA VISUAL, ver regla 16 + **QUÍMICA GENERADA POR SEMILLA**,
  ver reglas 33-35), `SimStepper` (reglas por arquetipo +
  `MorphTick` que evoluciona el campo morfológico + ring buffer de eventos), `ReactionEngine`
  (tabla de reacciones; expone `Count`/`At(i)`/`TryGet(...,out int index)` — el índice ES la
  identidad de una ley), `LeyDelUniverso` (playtest 18: `FormaDeLey` Transmutacion/Fusion/Consumo/
  Liberacion/Contagio/Crecimiento + `CondicionTermica`, el descriptor que consume el diario),
  `CellGrid` (incluye `byte[] morph`/`morphScratch`, ver regla 16),
  `SimRenderer` (textura + sprite; también dibuja el patrón/borde de la firma), `SimLevelBuilder`
  (**EL PLANO del taller: única fuente de verdad de TODAS las coordenadas**).
  REGLA DE ORO: nada de UnityEngine.Random ni allocs en el hot path; solo `XorShift` sembrado
  por (tick,x,y). El determinismo es el plan para el futuro netcode.
  El `AlkahestSceneBuilder` deriva las medidas del mundo de `CellGrid.W/H`, nunca hardcodeadas; la
  cámara SIGUE AL APRENDIZ desde el playtest 15 (con el mundo a 3x2 pantallas dejó de ser opcional).
  `CellGrid.ambient` (temperatura ambiente por celda) existe y hoy es UNIFORME en todo el mundo —
  el clima por zona se retiró en el playtest 17, ver regla 31 antes de reimplantarlo.
  `MaterialDef` lleva la FIRMA VISUAL de cada sustancia (playtest 12): `patron`
  (`PatronMorfologico`: Liso/Vetas/Manchas/Laberinto/Celdas/Dendritas/Pulso/Motas), `borde`
  (`BordeMorfologico`: Neto/Halo/Escarcha/Difuso), `patronEscala`, `patronFuerza`, `ritmoAnim`,
  `emision`, `semillaPatron`.
- `Game/` — capa jugable: `ApprenticeController` (imp volador), `Flask` (aspirar/verter, conserva
  la TEMPERATURA de lo aspirado; TODA mutación del grid vía `AlkahestSim.Paint`/`PaintCell`;
  BLOQUEO DE MATERIAL al pulsar aspirar, más el haz de mundo — el anillo de alcance que lo
  acompañaba se retiró en el playtest 11, ver regla 15), máquinas (`HeatPlate`/`ChillStone`/`Dispenser`/`StorageRack`) con sprites generados en
  `MaquinariaSprites` y foco de interacción arbitrado por `MachineFocus` (solo el aparato más
  cercano responde a E). `Cincel` (playtest 16): tecla **C** alterna frasco/cincel — es un MODO,
  no otro botón (el frasco se desactiva mientras está activo); clic izq. talla piedra a vacío, der.
  rellena vacío con piedra vía `PaintStable`. Primera pieza de la fase "taller movible".
  `Dispenser` emite con `PaintStable`, no con `Paint` (regla 29). `Mudanza` (playtest 19): tecla
  **V** para agarrar y recolocar grifos/placas/piedra gélida (contrato `IMovible`), **R** con las
  manos vacías devuelve todo a su sitio de fábrica (reglas 36-38). `HeatPlate`/`ChillStone` (playtest 14): `FootprintFraction`=0.4 recorta el
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
7. **`StaticSolid` no cae... SALVO los marcados `caeSolido` (matizada en playtest 29)**: la
   PIEDRA y la obra del taller JAMÁS caen (son la arquitectura del mundo); los productos sólidos
   del retículo, el hielo y el cristal SÍ caen al perder apoyo, con PRINCIPIO DE COHESIÓN
   (`MaterialDef.cohesionCeldas`: ménsula de K celdas por materia continua -- cerámico 8,
   compacto 6, recocido 5, cristal 5, hielo 4, templado 3; ver
   `SimStepper.SolidoTieneApoyo/ProcessSolidoCohesion`). Caída recta, solo a hueco vacío (los
   líquidos sostienen: el hielo sigue flotando). El arrastre manual de la Tolva/columna
   (`ArrastreTick`) sigue vigente para lo que NO cae solo.
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
22. **`AlkahestSim.PaintStable(x, y, radius, materialId)` es el camino para CREAR materia de la
    nada (playtest 13; ampliada en el 17 — ya NO es "solo `DevPalette`", ver regla 29)**: la celda nace a `StableBirthTempRaw(MaterialDef)`,
    una temperatura en la que el material es ESTABLE (evita el bug de "pintar hielo produce
    agua": `Paint`/`SetCell` nunca tocan `temp`, y la celda heredaba ambiente=70raw, que cruza
    siempre `Ice.meltsAt` en cualquier seed). `Paint`/`PaintCell`/`PaintRect` NO cambiaron de
    firma ni de comportamiento — siguen siendo los correctos para lo que MUEVE materia que ya
    existía y lleva su propia temperatura consigo (Flask al verter, MasterSupplies, DeliveryChute).
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
29. **SI ALGO *INTRODUCE* MATERIA EN EL MUNDO, USA `PaintStable`; `Paint` ES PARA LO QUE LA
    *MUEVE* (playtest 17, generaliza la regla 22)**: `Paint`/`PaintCell`/`PaintRect` NO TOCAN
    `temp`, así que una celda creada con ellos hereda la temperatura que ese hueco tuviera antes.
    Eso convirtió el grifo de agua en un fabricante de hielo durante dos rondas: si la boquilla o
    la pila se habían enfriado alguna vez, `Dispenser.EmitTick` emitía agua que nacía ya congelada
    (en cualquier seed, sin que el clima interviniera). Es el MISMO fallo que "pintar hielo produce
    agua" (regla 22), que se corrigió en `DevPalette` y nadie fue a buscar al resto de creadores de
    materia. Consumidores actuales de `PaintStable`: `DevPalette`, `Dispenser` (chorro y rebose),
    `Cincel` (rellenar piedra). Antes de añadir cualquier fuente nueva de materia, preguntarse si
    CREA o si MUEVE.
30. **DESCARTAR UN SOSPECHOSO NO ES IDENTIFICAR AL CULPABLE (playtest 17, la regla de método más
    cara de la sesión)**: la investigación del playtest 16 sobre "el agua del grifo sale congelada"
    midió BIEN (demostró con números que el degradado frío del sótano no llegaba a la boquilla: 41
    filas de base garantizada de por medio como mínimo) y luego firmó una sentencia contra el
    siguiente sospechoso disponible — "es varianza de semilla, es personalidad del universo, no
    bug" — sin someterlo a la misma exigencia. El culpable real estaba en la línea que CREA el
    agua. Cuando el síntoma sea "materia recién creada aparece en un estado imposible", el primer
    sitio que hay que mirar es SIEMPRE quién la crea y a qué temperatura la deja, no el entorno
    donde aparece. Corolario: una investigación que termina en "no es un bug, es diseño" tiene que
    justificar por qué NO revisó el camino de creación, o no ha terminado.
31. **EL CLIMA POR ZONA EXISTIÓ Y SE RETIRÓ — NO REIMPLANTARLO SIN LEER POR QUÉ (playtest 17)**:
    `SimLevelBuilder` pintaba un CULTIVO templado (26°C) y un SÓTANO frío (4°C) con degradados;
    hoy `PaintClimate` pinta `CellGrid.AmbientRaw` uniforme en todo el mundo. Dos razones: el
    taller va a ser MOVIBLE (un clima atado a coordenadas fijas contradice la fase siguiente:
    convierte una decisión del jugador en algo que el plano ya decidió), y dejar solo el cálido
    reintroduciría la asimetría calor/frío del playtest 13 por la puerta de atrás (la ventaja la
    da el APARATO, no la casilla). **El array `CellGrid.ambient` SE QUEDA a propósito**: cuesta lo
    mismo que una constante y es el vehículo del clima que sí vuelve — el que CREA EL JUGADOR (una
    fragua que entibia su alrededor), local por naturaleza. Efecto colateral valioso: con ambiente
    uniforme a 20°C, `Water.freezesAt` (raw 52..67 en el rango real de seeds) NUNCA lo alcanza —
    en ninguna seed puede el ambiente congelar agua solo, en ningún punto del mundo.
32. **DOCUMENTAR LA RONDA NO ES OPCIONAL NI ES EL ÚLTIMO PASO SI SE ACABA EL TIEMPO (playtest
    17)**: los playtests 15 y 16 se commitearon SIN sección en `docs/HANDOFF.md`, pese a que Cesar
    tiene una instrucción permanente de documentarlo todo. Se reconstruyeron a posteriori leyendo
    los diffs — se pudo porque los docblocks del código sí eran extensos, que es la red de
    seguridad de la regla 15. Es el mismo mecanismo que produjo la regresión de las reglas 26-27:
    trabajo que ocurre y no queda escrito deja de existir para quien venga después.
33. **LA INVARIANTE `Leyes[i]` ↔ `Reactions.At(i)` ES SAGRADA (playtest 18)**: para
    `i < Reactions.Count`, `Universe.Leyes[i]` describe EXACTAMENTE `ReactionEngine.At(i)`; la ley
    de crecimiento del Vivium (que no es una reacción de contacto) va la última, en
    `LeyCrecimientoIndice == Reactions.Count`. Los eventos `SimEventType.Ley` viajan con ese
    índice y es lo ÚNICO que identifica qué ley acaba de ocurrir: si los dos arrays se desalinean,
    el jugador descubre la ley equivocada y el diario le miente, sin ningún error visible. Las dos
    listas solo pueden crecer JUNTAS, en el mismo `Add`. Hay un assert de solo-editor; no quitarlo.
34. **LAS RESTRICCIONES DEL SORTEO DE QUÍMICA PROTEGEN LA PARTIDA, NO EL GUSTO (playtest 18)**:
    R1-R10 en `Universe.SortearLeyesGeneradas`. Las que no se pueden tocar sin romper algo:
    **R1** (al menos un reactivo INNOMINADO — garantiza que dos materiales del vocabulario nunca
    reaccionan entre sí, o sea que el agua y la arena de la pila jamás hacen algo raro solas);
    **R4** (el par no puede colisionar con ninguno ya presente, comprobado en LOS DOS órdENES:
    `ReactionEngine` es un lookup de una entrada por par y una colisión sobreescribiría EN SILENCIO
    una ley del núcleo, dejando semillas sin cristalización); **`MaterialesDeGrifo`** (la víctima
    de un Contagio no puede salir de un grifo — son infinitos y `Dispenser` no tiene tope: sería un
    bucle de materia sin fin. La lista es una copia a mano de `AlkahestGameBootstrap`: si alguien
    añade un grifo allí y no la actualiza, el agujero se reabre sin que nada avise);
    **`MaterialesSoloCatalizador`** (`Vivium` y `CrystalSeed` solo pueden ser reactivo en la
    posición de catalizador de una `Transmutacion` — el vivium es la cadena más lenta del juego y
    la semilla de cristal se entrega UNA vez, 60 celdas: si una ley los destruye pasivamente, hay
    encargos imposibles. Coste consciente: esos dos tienen una sola frase posible).
    **Cualquier restricción que se relaje exige volver a correr el modelo** de aceptación/descarte:
    endurecer bajó la tasa por intento al 48.8%, y con 200 intentos por hueco sale cero escasez en
    20.000 semillas — pero eso es un margen medido, no una intuición.
35. **UN MUNDO SE DOMESTICA, UNA LISTA DE ACCIDENTES NO (playtest 18, la lección de diseño de la
    ronda)**: la primera versión de la química generada pasó TODAS las auditorías técnicas y aun
    así estaba mal, porque el producto de cada ley salía de una bolsa uniforme y dentro de una
    misma semilla no había ningún patrón que el jugador pudiera aprender — *"5 plantillas con
    sustantivos intercambiables"*. Lo arregla `Universe.AfinidadDelUniverso`: 1-2 materiales afines
    por semilla que los pickers prefieren un ~55% (siempre como PREFERENCIA entre candidatos ya
    filtrados, NUNCA como excepción a una restricción). Resultado medido: el 54.4% de las leyes de
    una semilla convergen en su afín, y el mundo pasa a tener una tesis legible y NOMBRABLE ("aquí
    todo acaba en limo"). Criterio general para lo que venga: **generar variedad no basta; hay que
    generar variedad que el jugador pueda formular como una frase.** Si no se puede decir en voz
    alta, no se puede bautizar, y si no se puede bautizar no alimenta la fantasía del juego.
36. **`BuildVisual()` NO ES IDEMPOTENTE, Y `Init()` NO ES UN "MOVER" (playtest 19)**:
    `MaquinariaSprites.CrearCapa` SIEMPRE hace `new GameObject`, así que llamar dos veces a
    `BuildVisual` duplica todos los hijos y deja los viejos huérfanos, visibles, en la posición
    antigua, para siempre. Y volver a llamar a `Init()` es peor: en `Dispenser` resetea
    `favorCostPerActivation` y `Bloqueado` a sus valores por defecto — **vuelve a sellar el grifo
    de Azoth** — y no resetea `_on`, así que un grifo abierto seguiría emitiendo en la boquilla
    nueva. Para mover un aparato existe `Reposicionar(Vector2Int)` (contrato `IMovible` en
    `Game/Mudanza.cs`), que reutiliza los hijos ya creados y no pasa por ninguno de los dos.
37. **TRES MODOS EXCLUYENTES: FRASCO / CINCEL / MUDANZA (playtest 19)**: cada modo nuevo tiene que
    ceder ante los otros Y hacer que los otros cedan ante él — la exclusión es SIMÉTRICA o no
    sirve. El cincel se añadió en el playtest 16 sin poder tocar `Flask.cs` y dejó el hueco medio
    año; la mudanza se añadió sin poder tocar `Cincel.cs` y dejó el otro medio. Los dos huecos son
    el precio de trabajar con propiedad de archivos disjunta: **al integrar una ronda en paralelo
    hay que ir a buscar las guardas recíprocas que ningún encargo podía cerrar**, porque no
    aparecen solas ni rompen la compilación. `Mudanza.ForzarSalida()` es la puerta que debe usar
    un cuarto modo si algún día lo hay.
38. **SI EL JUGADOR PUEDE ROMPER ALGO EN SILENCIO, DALE EL DESHACER — NO LE QUITES LA HERRAMIENTA
    (playtest 19)**: mover la piedra gélida fuera de la bandeja no rompe nada visible pero vuelve
    imposibles encargos enteros sin decir por qué (el frío sigue funcionando, solo que ya no donde
    el Maestro sembró la semilla). Prohibirlo era la respuesta fácil y la equivocada: Cesar pidió
    explícitamente poder mover las cosas a su antojo, y una herramienta que te impide equivocarte
    tampoco te deja descubrir. La respuesta correcta fue **R con las manos vacías = todo vuelve a
    su sitio de fábrica**. Criterio general: cuando una libertad nueva pueda dejar la partida en
    un estado malo Y MUDO, la palanca es abaratar la vuelta atrás, no estrechar la libertad.
39. **CALIBRAR SIEMPRE CONTRA MEDIDAS LEÍDAS, NUNCA CONTRA PROSA (playtest 19)**: los comentarios
    del proyecto decían "cuba interior 52x37" y "bandeja fría interior 46x6"; las medidas reales
    eran 58x37 y 44x7 desde el playtest 15, y la regla 24 mandaba calibrar contra ellas. Todo lo
    que dependa de una medida de recipiente tiene que LEERLA de `SimLevelBuilder` en tiempo de
    ejecución (las redomas de `StorageRack` ya lo hacen, y por eso se recalculan solas cuando el
    estante se mueve). `SimRenderer.Init` tiene además un assert que revienta con `LogError` si el
    periodo de patrón deja de caber tres veces en el recipiente más estrecho: que se entere quien
    lo rompa al arrancar, no tres rondas después.
40. **UN MECANISMO DE CRECIMIENTO PUEDE AUTOBLOQUEARSE — MODÉLALO ANTES DE ESCRIBIRLO (playtest
    19)**: el crecimiento dendrítico (solo engendran las PUNTAS, las células con pocos vecinos
    vivos) tiene un fallo mortal: si la colonia se cierra sobre sí misma, nadie puede crecer y el
    cultivo muere para siempre. Con tolerancia de 1 vecino, **el 100% de 60 semillas modeladas
    terminaba en un anillo autobloqueado**; con vecindad de Moore, 27 de 60. El rango vive en 2-3,
    **nunca 1**, con el dato escrito en el propio campo de `Universe`. Cualquier cambio ahí exige
    volver a correr el modelo, no razonar a ojo — y ojo con `OrderSystem`, cuyo balance ya no puede
    razonarse como "crecimiento exponencial": ahora escala con el PERÍMETRO ÚTIL, no con la masa.
41. **UNA MECÁNICA PUEDE VIVIR EN DOS ARCHIVOS: REPARTIR MAL LA PROPIEDAD ES UN FALLO DEL
    DIRECTOR, NO DE LOS AGENTES (playtest 20)**: se encargó "bajar la escala de los patrones" con
    `SimRenderer.cs` en un encargo y `SimStepper.cs` en otro — pero la escala vive en LOS DOS
    (`Vetas`/`Celdas` son posicionales y las calcula el renderer; `Manchas`/`Laberinto`/
    `Dendritas`/`Pulso`/`Motas` salen de `MorphTick`). Cambiaron 2 de 8 familias, la firma visual
    se sortea, y Cesar probó la build y no vio NADA. Ningún agente se equivocó: la partición no
    admitía hacer el trabajo completo, y ninguno podía verlo desde su lado. **Antes de repartir
    archivos, preguntarse dónde vive REALMENTE la mecánica, no dónde vive el nombre del archivo**;
    y si una mecánica cruza la frontera, o va entera a un encargo o se escribe un contrato entre
    los dos (como en el playtest 18).
42. **`morph` ES UN SOLO CAMPO, Y ESO LIMITA LO QUE PUEDE DIBUJAR (playtest 20)**: una
    reacción-difusión BIESTABLE DE UN SOLO CAMPO **no produce patrones de Turing** — se homogeneiza
    siempre (engrosamiento tipo Allen-Cahn), así que en un charco acotado colapsa a un tinte plano
    hiciera lo que hiciera `patronEscala`. Durante ocho rondas el código AFIRMABA que
    `MorphReactionDiffusion` producía "puntos vs. bandas" y era falso: `Manchas` y `Laberinto`
    nunca se diferenciaron por forma, solo hoy por brillo medio. El playtest 20 arregló el colapso
    (anclaje de ruido ESTÁTICO por bloque, con `XorShift.FromCell(0u, ...)` — **tick constante 0,
    nunca `_tick`**, o el mapa cambia cada frame y el patrón parpadea), pero la distinción de FORMA
    exige un Gray-Scott real de DOS campos (U y V), que es un cambio de `CellGrid`. Está en el
    backlog como cambio estructural, no como afinado.
43. **UN CAMBIO QUE EL JUGADOR NO PUEDE DISTINGUIR DE "NO PASÓ NADA" ES, PARA ÉL, UN CAMBIO QUE NO
    OCURRIÓ (playtest 20)**: los encargos de la jornada 1 bajaron de 60/80 celdas a 32-40/42-54 y
    Cesar reportó "no encontré cambios en los niveles". El cambio estaba y funcionaba; lo que
    faltó fue **decirle los números exactos que tenía que ver en pantalla** en vez de "pedirá un
    40% menos". Al entregar una ronda, por cada cambio hay que dar el gesto concreto y el valor
    observable que lo comprueba — y si algo solo se ve en la jornada 2 (el vivium, por ejemplo),
    decirlo, o el playtest se gasta buscando lo inalcanzable.
44. **UN RECURSO QUE EL JUGADOR PUEDE PERDER PARA SIEMPRE ES UNA TRAMPA (playtest 22)**: Cesar,
    sobre el charco del cuarto íntimo — *"si por lo que sea lo pierdo ya no hay mucho más que
    hacer, y así evitamos dejar cositas en el suelo que se pueden perder"*. En un juego cuyo verbo
    es EXPERIMENTAR, una fuente infinita no es una comodidad: **es lo que permite equivocarse**.
    Los dos caños básicos (agua y nutriente, coste 0) volvieron al cuarto por eso. Criterio
    general: todo lo que el diseño espera que el jugador GASTE probando cosas tiene que ser
    reponible; lo escaso se reserva para lo que el diseño espera que ATESORE.
45. **EL RASGO DE UN INDIVIDUO NO PUEDE VIVIR EN SU MATERIAL (playtest 22)**: Cesar preguntó *"nació
    lo mismo que tenía vivo, ¿esto es probabilidad o así es?"* — ninguna de las dos: era un hueco.
    Las dos criaturas son literalmente `MaterialId.Vivium`, así que color, patrón y hábito de
    crecimiento salen de la SEMILLA DE LA PARTIDA (regla 17) y toda cría nacía clon de su padre.
    Para que dos seres del mismo material se distingan, **el rasgo tiene que ser un campo de la
    instancia** (`Criatura._temperamento`), y continuo, no una etiqueta — sin continuo no hay
    herencia con desviación. Vale para cualquier cosa que se quiera individualizar después.
46. **UNA CRIATURA QUE SE ENFRÍA A SÍ MISMA SE MATA (playtest 22)**: el temperamento térmico tiene
    un fallo mortal simétrico al de la regla 40. Si una criatura fría enfría su propia celda, sale
    de su banda de crecimiento, se duerme y no crece nunca más. Por eso `ApplyCalorTick` separa dos
    radios: **el NÚCLEO mantiene SIEMPRE a la criatura dentro de su banda**, pase lo que pase, y
    solo **el ALCANCE AMPLIO lleva el temperamento hacia fuera**. Ese anillo exterior es lo que la
    convierte en instrumento; el núcleo es lo que la mantiene viva. No fusionarlos.
47. **NO REUTILIZAR UNA CONSTANTE DE POSICIÓN SOLO PORQUE EL NOMBRE ENCAJA (playtest 22, corolario
    de la 39)**: los caños del cuarto íntimo NO pasan por `SpawnOneDispenser`, porque ese método
    deriva su sitio de `TapMountX`/`TapFirstY`/`TapStepY` — las coordenadas del banco de grifos del
    taller CLÁSICO, hoy enterrado a 30 celdas de la cámara. Reutilizarlo habría plantado los dos
    caños dentro de la roca, invisibles, sin ningún error. Cuando el plano tiene dos zonas vivas a
    la vez, el nombre de una constante ya no basta para saber si es la correcta.

48. **CADA ESTADO Y CADA TEMPERAMENTO NECESITA UN VERBO VISIBLE Y UN CONSUMIDOR REAL (playtest
    23, la regla de diseño del slice)**: el calor tenía consumidor (el capullo) y el frío no tenía
    ninguno — por eso una criatura fría era un callejón sin salida y Cesar reportó "me tocó fría y
    no puedo evolucionar". La generación 1 nace ahora con los dos polos garantizados (Rescoldo
    original SIEMPRE cálido 0.72..0.90; primera cría SIEMPRE fría 0.08..0.25) y el frío tiene dos
    consumidores: el HIELO (universal — raw 30 < freezesAt 52..67 en toda semilla) y las leyes con
    `condicion=Frio` de la semilla. La herencia fina ±0.16 queda para las generaciones 2+.
    Criterio general: antes de añadir un eje de variación, nombrar (a) el verbo con el que el
    jugador lo LEE y (b) el consumidor con el que lo APROVECHA; sin ambos, es ruido.

49. **LA PROMESA DE UN DOCBLOCK ES UN CONTRATO EJECUTABLE (playtest 24)**: el docblock de
    `SimStepper.MareaActiva` prometía "mientras sea false sus celdas SOLO fluyen: no convierten,
    no amortiguan" — pero `ProcessMarea` no comprobaba el gate: la marea dormida habría digerido
    el sótano desde el tick 0, con la documentación jurando lo contrario. Al auditar código de un
    encargo, leer cada promesa de comportamiento en los comentarios y buscar LA LÍNEA que la
    cumple; una promesa sin línea es un bug ya escrito.

50. **ANTES DE FIJAR UN NÚMERO DE CONFIG, LEER CÓMO LO CONSUME EL MOTOR (playtest 24)**: el
    contrato pedía `fluidity ~120` asumiendo una escala 0-255, pero `TryFlow` lo consume como Nº
    DE CELDAS a escanear por tick (escala real del roster: 1-4). 120 habría hecho que la marea
    cruzara todo un piso despejado EN UN TICK (tsunami, no marea) pagando hasta 120 iteraciones
    por celda asentada por tick. El nombre del campo no dice sus unidades; el código que lo lee,
    sí. Corregido a 1 (marea: repta) y 4 (Rocío: corre). Pariente de la regla 47 (no confiar en
    el NOMBRE de una constante): tampoco confiar en la ESCALA aparente de un campo.

51. **UNA GARANTÍA PROCEDURAL CUANTIFICA SOBRE LO ENTREGABLE, NO SOBRE LO FÍSICO (playtest
    25)**: el solver de persistencia eligió como "ganador garantizado" al estado FUNDIDO —
    umbral térmico 255 porque nada lo transforma hacia arriba, físicamente cierto y jugablemente
    absurdo: lo fundido se TEMPLA en el viaje al plinto del Ensayo, así que el material que
    llegaba al examen era OTRO. Una invariante de diseño ("toda semilla tiene solución") debe
    formularse en términos de estados que el JUGADOR puede presentar, no de estados que existen.
    Corolario que lo cazó: todo solver imprime su resultado en el log de seed — el bug se vio en
    el PRIMER arranque leyendo "ganador=19" ("un assert que no se puede leer no protege nada").

52. **LO QUE SOLO SE VE JUGANDO (playtest 26)**: la ronda de legibilidad compiló limpia y toda
    su lógica era correcta -- y aun así traía DOS fallos que ningún grep podía ver: el limo se
    confundía con la piedra A ESCALA DE JUEGO (el color de un material se juzga contra sus
    vecinos en pantalla, no en el hex del código) y un grifo infinito sobre suelo abierto
    inundaba el laboratorio en 20 segundos (la geometría alrededor de una fuente cambia lo que
    la fuente ES). Toda ronda que cambie forma, plano o color se verifica JUGANDO con capturas
    antes de entregar; de ahí salieron la RACIÓN de los caños y el oliva del limo.

53. **TEXTOS DE JUEGO EN ESPAÑOL LATINO NEUTRO (playtest 27, pedido directo de Cesar)**: tuteo
    singular, jamás vosotros/os/vuestro ni imperativos en -ad/-ed/-id ("carga", no "cargad");
    cuidado con léxico peninsular ("coger" es malsonante en LATAM: "tomar/agarrar"). Aplica a
    TODO string visible al jugador; los comentarios del código quedan como estén.

53. **COMPILAR EN EL SANDBOX ANTES DE DESPLEGAR (playtest 37 — el fin del "despliega y reza")**:
    el sandbox tiene un compilador Unity-FIEL: las DLLs de la build real del jugador
    (`Builds/ChaosAlchemyMulti/..._Data/Managed/*.dll`, staged a `/home/claude/unityrefs/`) +
    `dotnet csc` (`/usr/lib/dotnet/sdk/*/Roslyn/bincore/csc.dll`) con
    `-nostdlib+ -noconfig -t:library -langversion:9.0 -define:UNITY_64,UNITY_2023_1_OR_NEWER,NETCODEGAMEOBJECTS,STEAMWORKSNET`,
    todas las fuentes no-Editor de Assets/Alkahest+FriendsLoop, y todos los refs menos
    `Alkahest.Runtime.dll`. Detectó al primer intento el ÚNICO error real del playtest 36
    (CS0030 en SaberSync) que tres auditorías de símbolos a mano no vieron. OBLIGATORIO
    correrlo antes de todo despliegue; si el sandbox se reinicia, re-stagear las DLLs (5 min).
    Ojo: no sustituye el arranque en Unity (ILPP de NGO, escenas, runtime) — sustituye el ciclo
    ciego de "a ver si compila en el PC de Cesar".

54. **LOS FRACASOS DEJAN EVIDENCIA FORENSE (playtest 38, ley de diseño global)**: nada
    desaparece sin dejar rastro — un fracaso produce ceniza, residuo, gas, tizne, y una
    anotación automática en el diario ("a esta temperatura se destruye"). Fracasar es un
    experimento que salió con datos; la ceniza es combustible malo. Toda mecánica destructiva
    nueva se diseña respondiendo: ¿qué evidencia queda y qué enseña?

55. **TODO PROCESO VIVO DEBE DEMOSTRARSE MORTAL Y DESPIERTO (playtest 39, la ronda de motor)**:
    dos caras de la misma auditoría, obligatoria para cualquier mecánica con recursos por tick.
    (a) MORTAL: si una celda GANA recurso por hacer algo frecuente (moverse, tocar, reaccionar),
    calcula el balance esperado ganancia−decaimiento ANTES de integrar — el primer diseño de
    "bolsas de gas" daba +1,8/tick de vida contra −1 de descuento: humo matemáticamente INMORTAL
    bajo la bóveda sellada del taller (y el banco de 300 ticks era demasiado corto para verlo).
    La vida extra se expresa como decaimiento más lento acotado, jamás como ganancia por evento.
    (b) DESPIERTO: un proceso que consume recurso en el tiempo (combustión, brasa) pero que en la
    mayoría de sus pasos no mueve ni transforma nada debe llamar a `WakeChunk` sobre sí mismo —
    si depende de que OTROS despierten su chunk, una racha del RNG lo congela en silencio a
    mitad de proceso (el charco a medio arder como estatua, la brasa eterna).

56. **JAMÁS API DE UNITY EN INICIALIZADORES ESTÁTICOS (playtest 47, visto en vivo)**: un
    inicializador de campo estático (o .cctor) que llama a PlayerPrefs/AudioListener/
    Application/etc. lanza UnityException, el TIPO queda envenenado (TypeInitializationException)
    y TODO lo que lo toque explota en cascada — el juego entero "sale roto" sin un solo error de
    compilación, porque es una restricción de RUNTIME que el compilador fiel (regla 53) no puede
    cazar. El patrón correcto: centinela (-1/null) + carga perezosa en el primer acceso desde
    Awake/Start/Update/OnGUI. Ante un reporte de "todo roto de golpe", buscar
    TypeInitializationException en la consola ANTES que nada.

## Estado (última sesión) y prioridades
HECHO: M1 sim ✅ · M2 interacción ✅ · M3 leyes/reacciones/cultivo ✅ · M4 loop completo ✅ ·
M5 parcial (audio + aprendiz imp). Playtest 12: campo morfológico. Playtest 13: afinado de esa
base. Playtest 14: ronda de recuperación de la regresión de tres rondas (reglas 26-28) + la
conversación de dirección que abrió la fase actual.

**Playtest 15** (Opus dirige, Sonnet escribe): EL MUNDO x6 — `CellGrid` 256x144 → **768x288**
(3x2 pantallas, requisito de co-op de Cesar), cámara que sigue al aprendiz, `SimLevelBuilder`
reescrito con las CUATRO ZONAS (CULTIVO x16..250 / LABORATORIO x262..505 / ENTREGA x517..767 en la
mitad de arriba; SÓTANO x220..530 y10..143 debajo, sólo accesible volando por el POZO), y las tres
pasadas que escalaban con el mundo entero (refresco de textura, `MorphTick`, `DiffuseTemperature`)
pasadas a CONSCIENTES DEL VIEWPORT. Introdujo el clima por zona, retirado dos rondas después.
**Playtest 16**: costura del bucle ambiental (el chasquido cada 4.5s), rótulos de mundo que ya no
siguen a la cámara (`DentroDePantalla`, un bug que la cámara fija escondía), `OrdersHud` plegable
con **O** (los encargos "estorbaban espacio construible"), lo básico redistribuido al 47% del
ancho sin encoger el mundo ni mover geometría validada, y el **CINCEL** (`Game/Cincel.cs`, tecla
**C**): primera pieza real del taller movible.
**Playtest 17** (Opus dirige y escribe, revisión por agente independiente) — VALIDADO por Cesar:
la causa real del "agua del grifo congelada" (`Dispenser` emitía con `Paint`, que no toca `temp`:
el agua heredaba el frío del hueco y nacía helada — regla 29), el clima por zona RETIRADO entero
(regla 31, el array `ambient` se queda para el clima que cree el JUGADOR), barrido de siete
comentarios que quedaron mintiendo, y las secciones 15/16 del HANDOFF escritas a posteriori
(regla 32). Pusheado junto con el 15 y el 16 en el commit `9033b31`.
**Playtest 18** (Opus dirige: contrato congelado + gramática + 4 auditorías; Sonnet escribe en 2
encargos disjuntos) — PENDIENTE DE VALIDAR EN EL EDITOR: **LA FASE 3, química generada por
semilla**. Sobre el núcleo fijo de 7 reacciones se sortean 5-8 leyes más con una GRAMÁTICA de
formas (`FormaDeLey`), lo innominado puede reaccionar con el vocabulario del taller (decisión
explícita de Cesar; medido: el 76.4% de las leyes lo tocan) pero dos materiales del vocabulario
nunca entre sí; `Universe.AfinidadDelUniverso` da a cada semilla una TESIS nombrable (regla 35);
las reacciones por fin emiten un evento que identifica QUÉ ley ocurrió (`SimEventType.Ley` +
`leyIndice`, con limitador de ritmo); y el diario pasa a mostrar solo lo PRESENCIADO, con los
huecos a la vista y un contador "N de M". Detalle completo: `docs/HANDOFF.md` sección Playtest 18.

**Playtest 19** (RONDA NOCTURNA EN AUTÓNOMO — Cesar dejó el encargo y se fue a dormir: *"sorpréndeme,
muéstrame tu mejor esfuerzo, confío en ti, mañana lo pruebo"*. Opus dirige e integra; Sonnet escribe
en 4 encargos paralelos con propiedad disjunta; 3 auditorías) — PENDIENTE DE VALIDAR EN EL EDITOR.
El hilo común de su reporte era que EXPERIMENTAR CANSA, así que la ronda entera baja el coste de
probar cosas: **taller compacto** (la bandeja fría y el estante bajan encima del banco de grifos, la
Tolva se acerca 137 celdas; de 107/154/225 celdas de distancia a 49/50/88, sin encoger el mundo ni
mover geometría validada); **MODO MUDANZA** (tecla V, el paso 5 de la fase: agarrar y recolocar
grifos, placas y piedra gélida — con R para devolver todo a su sitio, regla 38); **crecimiento
DENDRÍTICO** del vivium (solo engendran las puntas; 4 parámetros de hábito por semilla, incluido si
la vida trepa hacia la luz o se entierra hacia el nutriente — reglas 40); **patrones legibles con
poca materia** (redomas +80% de área derivada del ancho real del estante, swatch del frasco +31%,
periodo de patrón de 5-12 a 3-6 celdas); y **el mago pide un 40% menos**, con temblor por semilla,
porque la jornada 1 era literalmente idéntica cada partida.

**Playtest 20** (Opus dirige y audita; Sonnet escribe) — PENDIENTE DE VALIDAR: Cesar probó la build
del 19 y dijo *"no encontré cambios en los niveles ni en la morfología de las formas"*. Tenía razón
en lo segundo por un error de reparto de archivos MÍO (regla 41): la escala de los patrones vive en
`SimRenderer` Y en `SimStepper`, se repartieron a encargos distintos y solo cambiaron 2 de las 8
familias. Al ir a arreglarlo apareció algo peor: **cinco familias nunca funcionaron como el código
decía** — `Manchas`/`Laberinto` colapsaban siempre a tinte plano (regla 42), `Dendritas` acababa
cubriendo el charco entero, `Pulso` **ignoraba `patronEscala` por completo** y `Motas` era invisible
el 90% del tiempo. Arreglado con anclaje de ruido estático, mapa de orígenes elegibles y
recalibración; queda pendiente que Manchas y Laberinto se distingan por FORMA (backlog).
También: los encargos SÍ habían bajado (32-40 y 42-54 en la jornada 1) pero no se le dieron los
números para comprobarlo (regla 43), y los scripts de commit pasan a llevar el número del playtest.

**Playtest 22** (ronda accidentada: tres reinicios del sandbox y un límite de cuota que cortó a un
agente; el código llegó entero pero **SIN VERIFICAR EN EL EDITOR**, el MCP de Unity cayó antes de
poder compilarlo) — **HERRAMIENTAS VIVAS**: la tesis que el propio Cesar puso en su referencia de
arte, *las máquinas no son máquinas, son criaturas con temperamento que colocas donde las
necesitas*. Temperamento térmico CONTINUO por individuo (regla 45) que sustituye a la placa ígnea y
a la piedra gélida, con el núcleo protegido para que una criatura fría no se suicide (regla 46);
la cría HEREDA del progenitor que la incubó con desviación, así que criar deja de ser esperar y
pasa a ser orientar; criatura y capullo implementan `IMovible` (tecla V, R devuelve); y vuelven los
dos caños básicos —agua y nutriente, coste 0, en el muro izquierdo— con el charco movido debajo
para recogerlos (reglas 44 y 47). PENDIENTE de esta ronda: **el halo no se convirtió en luz real**,
que era una buena intuición de Cesar y sigue siendo el siguiente paso barato.
La sección "LA TENSIÓN DE FONDO DEL PROYECTO" del HANDOFF resume, para quien tenga que opinar desde
fuera, el problema central: cómo hacer legible un simulador profundo sin simplificarlo — y la
hipótesis actual, que es DARLE UN CUERPO.

**Playtest 23** (FABLE de vuelta en dirección; código escrito por Fable sin agentes; compilado y
arrancado SIN ERRORES en el Unity real vía MCP) — **LA CADENA COMPLETA del encargo de Cesar**:
descubrir → transformar → capacidad nueva → preguntas nuevas. Diagnóstico con números de sus dos
"trabados" (temperamento inicial uniforme = mitad de partidas con capullo incubable-nunca; herencia
±0.16 = crías clon); regla 48 como respuesta; primera criatura siempre cálida, primera cría siempre
fría → HIELO como capacidad nueva universal; rótulos en verbos con acción; fuera el loot del suelo;
encargos del pivot que piden exactamente lo que el jugador acaba de aprender a hacer (hielo + lo
bautizado POR SU NOMBRE); fix de la O (panel vacío indistinguible de "no se abrió", regla 43).
Guion esperado de la partida en `docs/HANDOFF.md` sección Playtest 23.

**Playtest 24** (Fable dirige e integra; dos encargos Sonnet en paralelo sobre
`docs/CONTRATO_MAREA.md`; compilado y arrancado sin errores en el Unity real vía MCP) — **LA
MAREA, la super-modificación pedida por Cesar** ("quiero probar tu visión... todo de golpe"): la
afinidad de la semilla se vuelve EL ANTAGONISTA. Corazón en el zócalo del sótano
(`CorazonMarea*`), `MaterialId.Marea=17` (convierte 6%/1%, piedra INMUNE, amortigua temp hacia
-20°C, tintada 20% al color del afín, firma visual FIJA Pulso/Halo — excepción documentada a la
regla 17) y `MaterialId.Rocio=18` (la cura: brilla, mata marea 1:1 SIN azar). La criatura digiere
Marea→Rocío SIEMPRE (caso previo a los 3 escalones), le teme Y la digiere a la vez, muere a 9 s de
núcleo cubierto (cuerpo → Marea). `MareaDirector`: despertar (12 celdas talladas o 300 s), 3
pistas de arco (canal prioritario del HintSystem), victoria ≥24 Rocío en el corazón ("EL MUNDO SE
AQUIETA") / derrota sin criaturas ("LA MAREA OS TRAGÓ") — `DayCycle.TerminarPartida`, desenlace
clásico intacto. Reglas 49-50 nacieron de la integración. Visión completa, guion esperado y
preguntas abiertas en `docs/HANDOFF.md` sección Playtest 24.
**DESCARTADO POR CESAR tras probarlo** ("atrevida e interesante, pero la descarté") y RETIRADO
DEL CÓDIGO entero en el playtest 25 (revert quirúrgico a playtest 23 + borrado de MareaDirector;
los docs se quedan como archivo de la decisión). Las reglas 49-50 sobreviven: son del proyecto.

**Playtest 25 — LA DIRECCIÓN VIGENTE: "LO QUE PERSISTE"** (dirección de Cesar, diseño de Fable en
`docs/DISENO_LO_QUE_PERSISTE.md`, TRES encargos Sonnet en paralelo sobre
`docs/CONTRATO_PERSISTE.md`; compilado 0 errores/0 warnings a la primera y verificado en el Unity
real vía MCP): el eje del juego cambia de "aprender a fabricar" a **descubrir qué persiste ante
condiciones**. RETÍCULO DE ESTADOS: 5 bases por seed × 8 estados (`EstadoMateria`, ids 18..57,
`Count=58`), el historial vive en el ESTADO (markoviano, grafo no conmutativo: fundir→prensar
escupe; prensar→hornear da cerámico; templar≠recocer). LIMO primigenio (id 17, caño ex-nutriente)
que se separa por calor en las 5 bases. Máquinas: `Crisol` (rescoldo tier0 raw 120 + temp máxima
decidida por el COMBUSTIBLE; calcina/ceramiza/recuece), `Prensa` (Compactar/Reventar/Escupir/
Resistir), `BancoChispa` (conductividad 0/1/2 — LA propiedad invisible), Columna de ensayo en el
nivel, `EnsayoMaestro` (el pedido de calor se ensaya A LA VISTA, estrellas por margen real).
Pedidos = ARCO FIJO de 5 (`GenerateOrdersPersiste`), el 5º compra el PROCEDIMIENTO (paga doble).
`Hornada` (ring 8 ops) + PATENTES v0 en sección PROCEDIMIENTOS del diario. SOLVER DE GARANTÍA en
`Universe.Create` (BFS, escalera tier0→tier1, 3 garantías, log por seed) — los pedidos imposibles
son estructuralmente imposibles. Criatura/Capullo APARCADOS (spawns comentados, archivos
intactos): volverán como organismos-solución (el "veneno para ratas" de Cesar). Regla 51 nació
del primer arranque. Guion esperado y preguntas abiertas en HANDOFF sección Playtest 25.
BACKLOG NUEVO de esta dirección (v2): biblioteca transversal entre mundos (firmas funcionales +
persistencia JSON), alambique/condensación, fragilidad-frío, configuración fantasma visual sobre
máquinas, royalties por patente, hornada por-lote (v0 es global), teatro físico para
FlotaInsoluble, automatización como premio de lategame.

**Playtest 26 — EL TALLER QUE SE EXPLICA SOLO** (feedback de Cesar sobre el 25: máquinas
ilegibles, "sin carteles", consejos que aturden; contrato `docs/CONTRATO_LEGIBILIDAD.md`, dos
encargos Sonnet + fixes e integración de Fable VERIFICANDO EN VIVO con capturas en el PC de
Cesar): GRAMÁTICA VISUAL (embudo=entrada, brasero=combustible, cubeta enmarcada=resultado, el
verbo en el cuerpo: chimenea/husillo/arco/pedestal) + **AFFORDANCE GLOW** (la boca late si lo
del frasco le sirve -- "¿meto limo en todas?" lo contesta el taller señalando) + LA LÍNEA DEL
TALLER (cuarto 232..357, fuentes→crisol→prensa→columna→chispa→ensayo→tolva; mampostería
centralizada en SimLevelBuilder vía TallarEnPlano estáticos; caño de limo con voladizo 12 propio)
+ consejos a 12s con N=siguiente, H=ocultar, contador 3/10 y sección CONSEJOS releíble en el
diario (hook del playtest 10 por fin consumido) + **LA RACIÓN** de los caños del laboratorio
(45 celdas por apertura, "· servido — E para más"; nació de ver la inundación EN VIVO) + limo
recoloreado a oliva (se camuflaba con la piedra). Regla 52 nació de esta verificación. Detalle y
preguntas abiertas en HANDOFF sección Playtest 26.

**Playtest 27 — EL TALLER GRANDE**: veredicto duro de Cesar sobre el 26 (máquinas cajita,
embudos falsos, crisol que escupe 4 colores). Fixes Fable (glow de proximidad APAGADO —
conservado para "máquina trabajando"; ObraDelTaller anticincel; caño sin estirar) + OPUS 5 CON
OJOS (3 ciclos desplegando/jugando/capturando en el PC real): cuarto 218x73, estaciones 6-20x,
CRISOL POR HORNADAS (una transformación por pasada, resultado que REPOSA, extracción del limo
por temperatura: UNA base por hornada ligada al combustible, solver con garantías G1-G4,
Universe.ExtraccionRaw), columna con muros de piedra (Crystal era REACTIVO: bug) + vidrio visual
+ Game/ColumnaEnsayo.cs nuevo. Barrido español latino (regla 53). Pistas reescritas al modelo de
hornadas.
**Playtest 28 — EL TALLER COMPARTIDO (POC multiplayer)**: 4 jugadores, colores
dorado/cielo/verde/magenta, sim solo-host + espejo por chunks RLE 5Hz (Net/SimSync.cs), frasco
de invitados con predicción + reenvío, escena MULTI aparte (menús 2 y 4), template FriendsLoop
intacto. PENDIENTE COMPILAR (asmdef cambiados; DUDA-API concentradas en CustomMessagingManager).
Detalle completo de ambos en HANDOFF.

FASE ACORDADA (orden): 1) ✅ cámara que sigue al aprendiz; 2) ✅ taller a 2-3 pantallas;
3) ✅ química generada por semilla (playtest 18); 4) **comportamiento variable por semilla, no
solo aspecto** (de ahí nacen los nombres que Cesar busca) ← SIGUIENTE: la gramática de leyes ya da
variedad de QUÉ reacciona con qué; falta que varíe CÓMO se mueve/se comporta la materia misma; 5) ✅ taller movible (playtest 19: cincel + mudanza; falta el estante, y anclar de verdad a bedrock);
6) **mundo persistente con semilla y progreso** ← SIGUIENTE, junto con el desbloqueo de áreas por
niveles que Cesar viene pidiendo ("un lugar pequeño que luego se me amplíe").
Backlog heredado aún vigente: **Gray-Scott de DOS campos** para que `Manchas` y `Laberinto` se
distingan por FORMA y no solo por brillo (regla 42 — es un cambio de `CellGrid`, no un afinado);
separar en `Universe.cs` los rangos solapados de `waterFreezeC` y
`crystallizeThresholdC` (hoy en algunas seeds cristalizar exige un frío que ya fabrica hielo);
consolidar `FirmaVisualFabrica` con `JournalHud` (regla 25); enganchar `HintSystem.PistasMostradas`
en PROCEDIMIENTOS del diario; descripción completa de encargos en el diario; decidir si el audio se
queda; renombrar repo GitHub `Alkahest`→`ChaosAlchemy` + `productName`; resto de M5 (glow, agua con
más cuerpo); build de Windows con la checklist (`docs/HANDOFF.md` sección "Playtest 11"); CURVA DE
PROGRESIÓN (jornadas cortas, una mecánica cada una); desbloqueo de áreas por nivel; multiplayer
(sim solo-host + deltas RLE, el formato debe contemplar `morph`, regla 16); medir el coste real de
`MorphTick` con el mundo 6x.
