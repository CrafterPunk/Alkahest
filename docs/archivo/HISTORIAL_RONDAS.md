# HISTORIAL DE RONDAS — TEN THOUSAND YEARS (archivo histórico)

*Crónica cronológica de 70+ rondas de desarrollo/playtest. Nació como el HANDOFF del proyecto
cuando aún se llamaba ChaosAlchemy; los nombres viejos que aparecen abajo (ChaosAlchemy,
menús "Alkahest/…") son HISTORIA y se conservan tal cual — el estado actual vive en
`README.md`, `docs/ESTADO.md` y `CLAUDE.md`. Consultar este archivo solo para el porqué
de una decisión vieja.*

## Qué es esto
Prototipo vertical de "alquimia emergente": el jugador cae en un universo con leyes de la materia
variables por seed, experimenta en cubas, descubre sustancias, las BAUTIZA con nombres propios, y
domestica procesos (cultivar Vivium, cristalizar Azoth) para cumplir pedidos por EFECTO del
Maestro en 3 jornadas. Single-player primero; el multiplayer Steam del template FriendsLoop está
en el proyecto pero no integrado con la sim.

## Estado real verificado
| Pieza | Estado |
|---|---|
| Sim celular (M1) | ✅ Verificada jugando: arena apila, agua estratifica bajo aceite, fuego arde el aceite, chunks duermen (0.2-1.0 ms/tick, 60+ fps) |
| Interacción (M2) | ✅ Verificada por el USUARIO jugando (frasco aspirar/verter, grifos, placas) |
| Leyes/reacciones/cultivo (M3) | ✅ Compila y arranca; Edictos sortean y se muestran; reacciones/cultivo SIN prueba de juego profunda aún |
| Loop de juego (M4) | ✅ Título → Jornada 1 con pedidos generados VISTO en pantalla; el resto del flujo (entregas→Favor→jornadas 2-3→final) SIN probar |
| Color de fuego + shimmer líquidos | ✅ código desplegado, PENDIENTE verificación visual (feedback del usuario: "el fuego no tenía color fuego") |
| Firma morfológica por seed (M12) | ✅ verificada en editor (playtest 14): Cesar confirma que la piel varía por seed, pero DIAGNOSTICA que eso no basta — ver "Playtest 14" §4, "falta morfología de comportamiento" |
| Firma visual GENERADA en redomas/frasco + `PaintStable` + Fresca de `ChillStone` (playtest 13) | ✅ verificado en editor (playtest 14), sin reportes nuevos de bug sobre esta parte |
| Regresión playtest 7→10 en máquinas y `UiStyles` (chapa lateral, halo de foco, límite de usos de E, `PlacaMundoLateral`/`Cercania`) | ✅ RECUPERADA por fusión a tres bandas y validada por Cesar en el editor (playtest 14, ver sección completa — es el hallazgo mayor de la ronda) |
| Recuadros negros al arrancar / rótulo del frío invertido / Helando redundante / placas de tamaño completo (regresiones de la propia fusión + ajustes pedidos) | ✅ corregidos y validados por Cesar en el editor (playtest 14) |
| Commits | M1+M2 en `001a9a1`; M3+M4+rebranding PENDIENTE de commit (script `ca_commit.cmd` listo en la raíz del proyecto) |

## Cómo continuar (receta operativa)
1. Lee CLAUDE.md (reglas del entorno: despliegue de archivos, git por scripts, permisos volátiles).
2. Ejecuta/pide al usuario ejecutar `ca_commit.cmd` si aún no se hizo (checkpoint M3+M4).
3. Abre Unity (proyecto `C:\JuegosUnity\UnityAI_Test\Alkahest`), Play, y JUEGA una jornada
   completa con el DevPalette (F3) como atajo: pinta Oil en una cuba → entrégalo en la Tolva
   (boca del muro derecho, celdas x 216-237 y 44-72) → verifica que el pedido "inflamable"
   progresa y suma Favor.
   Ese es el circuito crítico sin probar.
4. Balancea lo que chirríe (cantidades de pedido vs capacidad del frasco 900, timer 6 min).

## Backlog priorizado (actualizado tras playtest 14)
**FASE NUEVA — plan de dirección acordado con Cesar tras el diagnóstico "falta morfología"
(ver "Playtest 14" §4-5), en este orden y por esta razón:**
1. **Cámara que sigue al aprendiz**: barata, requisito de todo lo demás, y desbloquea probar el
   taller grande por partes en vez de todo de golpe.
2. **El taller a 2-3 pantallas**: rediseñar `SimLevelBuilder` con las zonas que bocetó Cesar
   (cultivo — laboratorio principal — entrega, más un sótano) y hacer conscientes del viewport las
   tres pasadas que hoy cuestan proporcional al MUNDO entero y no a lo VISIBLE (el refresco
   completo cada 30 frames, `MorphTick`, `DiffuseTemperature`) — a 768x288 (~221.184 celdas, 6x
   las 36.864 actuales) dejan de ser gratis.
3. **La química generada por semilla**: un núcleo fijo que garantice que los encargos siguen
   siendo posibles + reacciones sorteadas encima, y el diario mostrando "leyes descubiertas: 3 de
   11" para que se sepa que hay más por descubrir.
4. **El comportamiento varía por semilla, no solo el aspecto**: que una sustancia trepe en un
   universo y se hunda en otro; que lo que crece con calor aquí crezca con frío allá. Ahí es donde
   nacen los nombres que Cesar busca — hoy el color es la única variable, y por eso los nombres
   salen "amarillo brillante".
5. **El taller movible**: mover grifos, estantes, placas y botellas con el botón central del
   ratón, anclados a bedrock, con el grifo orientándose según el lado por el que se ancla.
6. **El mundo persistente con semilla y progreso guardado**, estilo Minecraft — *"donde sienta que
   el conocimiento de un universo me suma"*.

**Backlog heredado, aún vigente:**
7. **Consolidar `FirmaVisualFabrica`** (`Game/StorageRack.cs`) **con el generador de
   `JournalHud`**: duplican las siete funciones de patrón y los hashes (deuda técnica anotada en
   el playtest 13 §4) — mover a un único `Game/FirmaVisualFabrica.cs` compartido.
8. **Enganchar `HintSystem.PistasMostradas` en la sección PROCEDIMIENTOS del diario**
   (`JournalHud`): la API ya existe, escrita en paralelo en el playtest 10, pero nadie la consume
   todavía (arrastrado sin tocar desde el playtest 10).
9. **Decidir el destino del audio M5**: ¿se queda o se apaga con `SistemaActivo = false` en
   `DirectorDeAudio`? Depende de feedback de Cesar.
10. **Renombrar repo GitHub** `Alkahest`→`ChaosAlchemy` + `git remote set-url` + `productName` en
    ProjectSettings (los namespaces `Alkahest.*` se quedan — decisión registrada).
11. **Resto de M5**: glow aditivo fuego/Vivium, agua con más cuerpo (metaballs/post-blur).
12. **Ejecutar la build de Windows y validarla con la checklist del playtest 11**: el builder ya
    regenera la escena antes de compilar y deja el resumen en consola + diálogo — falta EJECUTARLO
    y pasar el `.exe` por la checklist (`docs/HANDOFF.md` sección "Playtest 11").
13. **CURVA DE PROGRESIÓN — jornadas cortas de una mecánica cada una** (playtest 11 §4): no hay
    onboarding; probablemente se resuelve junto con el rediseño del taller de la fase 2.
14. **Multiplayer (riesgo técnico nº1)**: plan diseñado, NO implementado. Se decidió hacer antes la
    morfología (playtest 12) por dos razones: de diseño, el co-op multiplica una experiencia (y si
    la experiencia se agota en una partida, el co-op multiplica el agotamiento); y técnica — el
    plan de netcode ya añadía un campo nuevo por celda (`CellGrid.morph`), y si se hubiera hecho
    después del multiplayer habría obligado a rehacer el formato de deltas.
    - Sim corre SOLO en el host. Clientes: render + input remoto (aspirar/verter/E como RPCs).
    - Estado: deltas de chunks despiertos, RLE por filas del byte mat[] (+temp cuantizada cada 4º
      tick), 10-15 Hz, ~5-30 KB/s estimado — MEDIR con `NetDiagnostics` del template antes de
      optimizar. Fallback: lockstep determinista (la sim ya es determinista por diseño: XorShift
      por (tick,x,y), sin flotantes en lógica) — requiere snapshot+replay para joins.
    - **NOTA (playtest 12): el formato de deltas debe contemplar el campo `morph`** — es un byte
      más por celda que viaja con la materia (`CellGrid.SwapCells`), así que cualquier RLE de
      `mat[]` que no lo lleve dejará la textura interna desincronizada entre host y clientes.
    - Reusar TODO el FriendsLoop: `SessionCoordinator` para lobby/transporte; el gameplay solo
      habla con él. NO rediseñar el template.
15. **Medir el coste real de `SimStepper.MorphTick`** con el overlay de dev (F3): estimado
    <0,5 ms/tick sobre un presupuesto de 33 ms, SIN VERIFICAR en Unity (arrastrado desde el
    playtest 12) — más urgente ahora que la fase 2 multiplica el tamaño del mundo 6x.
16. Ideas aparcadas: mercado de ofertas secuenciales, tamiz/filtro, más Edictos, voz (evaluada:
    NO para taller de una pantalla — ver DECISIONS §17); replantear las redomas del estante (queda
    absorbido por el taller movible de la fase 5).

## Riesgos y trampas conocidas
- El puente Cowork NO puede borrar archivos ni tocar refs de git en el FS montado → scripts .cmd.
- Los permisos de Computer Use caducan solos: presupuesta re-aprobaciones del usuario.
- El fuego se extingue solo si hay agua ENCIMA o 2+ vecinos de agua (fix intencional: el aceite
  ardiendo flota sobre agua). No "arreglar" eso de vuelta.
- SetPixels32 por chunks: hay UN buffer scratch preasignado de 16x16 y el render asume que CHUNK
  divide W y H (256x144 lo cumple; hay una guardia con LogError en SimRenderer.Init si se rompe).
- Unity a veces abre ventanas en el 2º monitor (`computer_switch_display`).

## Playtest 23 → LA CADENA COMPLETA: descubrir → transformar → capacidad nueva → preguntas
## nuevas — Fable de vuelta en dirección; compilado y arrancado sin errores vía MCP
Ronda dirigida y ESCRITA por Fable 5 (sin agentes: cambios quirúrgicos, y la cuota de agentes
había demostrado ser frágil). Verificado en el Unity real: compila limpio y arranca sin errores.

**El encargo de Cesar**, literal: *"¿Podemos construir una progresión donde, experimentando,
consiga crear nuevas herramientas vivas, nuevas fuentes y nuevas sustancias útiles, hasta sentir
que estoy domesticando el sistema?... La prioridad absoluta es una versión inicial sencilla,
legible, agradable, con pocas cosas, pero que ya permita sentir una pequeña cadena real de
descubrimiento → transformación → nueva capacidad → nueva experimentación."*

### EL DIAGNÓSTICO, con números — sus dos "trabados" eran el mismo bug
1. **El temperamento inicial se sorteaba uniforme 0..1** → en ~la mitad de las partidas la
   criatura nacía FRÍA. Una fría contenta empuja su anillo hacia raw 30 (−60°C) y el capullo solo
   avanza por encima de `VivGrowMinRaw` (~25-40°C): **capullo muerto, partida trabada**. Le pasó a
   él, y no era mala suerte: era estructural.
2. **La herencia desviaba ±0.16** sobre un eje 0..1 → la cría era casi el padre. Su *"nació lo
   mismo pero más pequeña"*.
3. El dato que lo resuelve todo: **el frío YA podía congelar agua en toda semilla** (raw 30 <
   `freezesAt` 52..67). La capacidad existía; faltaba el camino hasta ella y el cartel que la
   nombrara.

### LA REGLA DE LA RONDA
**Cada estado y cada temperamento necesita un VERBO visible y un CONSUMIDOR real.** El calor tenía
consumidor (el capullo); el frío no tenía ninguno — por eso era un callejón. Ahora el frío tiene
dos: el HIELO (universal, determinista, vocabulario) y las leyes con `condicion=Frio` de cada
semilla (variable, descubrible). En la primera semilla probada tras el cambio, 4 de las 6 leyes
sorteadas exigían frío: la cría fría es la llave de la mayor parte de la química de ese universo.

### LO HECHO (6 archivos, cambios quirúrgicos)
1. **`Criatura.SortearOHeredarTemperamento`**: el Rescoldo original nace SIEMPRE cálido
   (0.72..0.90 por semilla). La sala inicial tiene un solo consumidor térmico y pide calor; la
   generación 1 nace del lado que la sala puede consumir. No extremo (1.0 evaporaría la pila).
2. **`Capullo.Eclosionar`**: la primera cría de la run nace SIEMPRE fría (0.08..0.25). La
   generación 1 enseña el EJE entero (naciste con el polo cálido, criaste el polo frío); la
   herencia fina ±0.16 se conserva intacta para las generaciones 2+ (regla 15).
3. **Rótulos en VERBOS con consecuencia**: *"congela lo que la rodea"* / *"irradia calor"* /
   *"hambrienta — viértele nutriente"* / *"asustada — aleja el peligro"*. Y el capullo, que no
   tenía rótulo, ahora dice **por qué** no avanza: *"incubando — avanza con el calor"* /
   *"detenido — hace demasiado frío aquí"*.
4. **Fuera el loot del suelo** (`PlaceNutrienteMound` bifurcado, no borrado): *"hace pensar en
   exploración/recolección tipo Minecraft"*. El caño de nutriente lo hacía redundante, y quitarlo
   convierte el PRIMER acto del jugador en alimentarla ÉL — más íntimo, más causal, y enseña el
   frasco de paso.
5. **Encargos del pivot (`GenerateOrdersPivot`)**: los de la jornada clásica (inflamable + 80°C)
   eran IMPOSIBLES en el cuarto íntimo — el premio de cavar 23 celdas era un muro. Ahora: **"algo
   helado a −5°C"** (= la cría fría, la validación externa de la capacidad nueva) y, si ya
   bautizó algo, **el Maestro se lo pide POR SU NOMBRE** — bautizar gana valor mecánico.
6. **Fix de la O**: no era la tecla — con cero encargos el panel medía solo cabecera y se abría a
   un panel casi vacío indistinguible de "no se abrió" (regla 43). Ahora lo dice: *"nadie os ha
   oído todavía — la Tolva sigue sellada tras la roca, hacia la derecha"*.

### LA PARTIDA QUE DEBERÍA SALIR (guion esperado del playtest)
Despierta hambrienta → el rótulo te dice qué hacer → la alimentas → se enciende e irradia →
viertes agua encima → exuda algo nuevo → lo bautizas → el capullo junto a ella se agrieta →
nace la cría FRÍA, azul → acercas agua → **HIELO** → cavas hasta la Tolva → te pide exactamente
hielo, y lo que tú bautizaste, por su nombre. Cada eslabón enseña el siguiente.

### QUÉ NO ENTRÓ (anotado, decidido, no olvidado)
Identidad perceptual profunda de materiales (coral/venas — bloqueada por el Gray-Scott de dos
campos del backlog); pedidos narrativos ("veneno para ratas") — exigen un sistema de PROPIEDADES
de material, que es la fase siguiente natural si este slice funciona; breeding dirigido
multi-generación (la herencia fina ya está lista esperándolo); análisis por olor/sensación.

---

## Playtest 22 → HERRAMIENTAS VIVAS: las máquinas son criaturas — pendiente de validar
Ronda accidentada (tres reinicios del sandbox y un límite de cuota que cortó a un agente a mitad),
pero el código llegó entero. **Sin verificar en el editor: el MCP de Unity se cayó antes de poder
compilarlo yo.**

**Reporte de Cesar tras jugar el playtest 21**, que es lo que ordenó esta ronda:
*"no puedo reacomodar el hijito que nació"* · *"nació lo mismo que tenía vivo, ¿esto es
probabilidad o así es?"* · *"se ilumina bastante cuando come pero no sé si es fuente de luz, quizás
pueda serlo"* · *"busqué los caños por el mapa, quizás los rompí con el empty del F3 o simplemente
no hay"* · *"tampoco encontré la máquina de calor o de hielo, imagino que esa es la función del ser
vivo pero no lo puedo mover, tampoco veo la temperatura que tiene"* · *"quizás necesite los caños
más básicos al inicio; hay un charquito de agua pero si por lo que sea lo pierdo ya no hay mucho
más que hacer, y así evitamos dejar cositas en el suelo que se pueden perder"*.

### LA TESIS, y la escribió él
Su referencia de arte lleva un panel rotulado **"HERRAMIENTAS VIVAS"**. Eso es el juego:

> **Las máquinas no son máquinas: son criaturas.** No instalas una placa de calor y una piedra
> fría — crías seres con TEMPERAMENTO y los colocas donde los necesitas. Montar el laboratorio es
> ordenar tus instrumentos vivos. El cincel excava el espacio; las criaturas lo amueblan.

Con eso encaja todo lo ya construido: alimentar importa porque un ser bien alimentado trabaja
mejor; el capullo importa porque criar es cómo consigues el temperamento que te falta; digerir es
la alquimia hecha por un ser; y **el taller enterrado pasa a ser la herencia de quien construyó
máquinas en vez de criar seres**.

### Qué se hizo
1. **TEMPERAMENTO TÉRMICO POR INDIVIDUO** (`Criatura.cs`). Valor CONTINUO en la instancia (no en el
   material — ese era el bug de fondo: los dos seres son el mismo `MaterialId.Vivium`, así que
   color, patrón y hábito salían de la SEMILLA y por eso la cría era un clon). Las etiquetas
   calor/frío/templado son solo la presentación.
   **LA TRAMPA, documentada porque es fácil caer**: si una criatura fría enfría su propia celda, se
   sale de su banda de crecimiento, se duerme y no crece nunca — se autodestruye. Por eso los dos
   radios que ya existían se separan: **el NÚCLEO mantiene SIEMPRE a la criatura dentro de su
   banda**, y solo **el ALCANCE AMPLIO lleva el temperamento**. Ese anillo exterior es lo que la
   convierte en instrumento.
2. **LA CRÍA HEREDA CON DESVIACIÓN** (`Capullo.HeredarTemperamentoConDesviacion`, decisión de Cesar
   frente a "tirada nueva"). Se resuelve en el capullo porque solo él conoce a la vez al progenitor
   y el instante del nacimiento; se pasa a `Criatura` por
   `Criatura.TemperamentoHeredadoPendiente` porque la firma de `Init` está congelada por contrato.
   Hereda de `Criatura.MasCercanaA` — de quien lo incubó, no de un "padre" global.
3. **CRIATURA Y CAPULLO SON MOVIBLES**: implementan `IMovible` y se registran en `Mudanza`. Tecla
   **V** los agarra, **R** los devuelve a su sitio (regla 38).
4. **LOS DOS CAÑOS BÁSICOS VUELVEN** (`SimLevelBuilder.CanoMontajeX/CanoAguaY/CanoNutrienteY` +
   `AlkahestGameBootstrap.SpawnCanoBasico`). Solo **agua y nutriente**, coste 0, montados en el muro
   izquierdo en columna como en la referencia de arte. **El charco se movió de x=267 a x=250 para
   quedar justo debajo de las boquillas**: deja de ser decorado y pasa a ser pila de recogida.
   Arena, aceite y azoth siguen enterrados — aparecen al excavar, y esa es su recompensa.
   *NO se reutilizó `SpawnOneDispenser`*: aquel deriva su posición de `TapMountX/TapFirstY/TapStepY`,
   que son las coordenadas del banco clásico enterrado a 30 celdas de allí. Habría plantado los
   caños dentro de la roca — exactamente el fallo que advierte la regla 39.
   La lectura de la sala queda **caños+pila → criatura → capullo**, de izquierda a derecha.

### Lo que quedó SIN HACER de esta ronda
- **El halo no se convirtió en luz real.** Cesar lo intuyó (*"quizás pueda serlo"*) y es buena idea:
  la cámara está oscura y las criaturas deberían ser lo que la alumbra, así que colocarlas sería
  también decidir dónde ves. Sin shaders (prohibidos en runtime) y sin tocar el alfa por celda
  (regla 19): capas de sprite.
- **Nada de esto se ha visto correr.** Compila a nivel de lectura (llaves, símbolos cruzados,
  firmas) pero no pasó por un compilador de verdad.

### NOTA DE PROCESO (importante para quien retome)
El sandbox se reinició **tres veces** en esta sesión y revirtió el repo cinco rondas sin avisar
(regla 6b). Una de esas veces un encargo trabajó una hora entera contra código de cinco rondas
atrás sin que nadie lo supiera, y concluyó —con razón para lo que veía— que `Universe.Leyes` y
`Universe.AfinidadDelUniverso` no existían. **Comprobar `git log --oneline -1` contra GitHub ANTES
de encargar nada**, y desplegar pronto en vez de acumular.

---

## LA TENSIÓN DE FONDO DEL PROYECTO (para quien tenga que opinar desde fuera)
*Cesar lo nombró así: "por si volvemos a Fable para recoger su opinión si nos quedamos atascados en
este intento de introducir lo complejo del mundo en algo simple para el jugador".*

Ese ES el problema central de ChaosAlchemy, y conviene dejarlo escrito sin adornos:

**Debajo hay un simulador honesto y profundo**: autómata celular determinista de 768x288 a 30Hz,
química generada por semilla con una gramática de formas de ley, campo morfológico, crecimiento
dendrítico con hábito por semilla, temperatura por celda. Todo eso funciona y está medido.

**Arriba hay un jugador que necesita entender qué está pasando en diez segundos.** Y el historial
de esta sesión es, casi entero, la crónica de esa fricción:
- Los patrones existían pero hacía falta demasiada materia para verlos (playtests 19-20) — y cinco
  de las ocho familias nunca funcionaron como el código afirmaba.
- La química por semilla generaba variedad real pero *"5 plantillas con sustantivos
  intercambiables"* hasta que se le dio una TESIS por semilla (regla 35).
- Los encargos bajaron un 40% y Cesar no lo notó, porque no se le dieron los números que tenía que
  ver (regla 43).
- El taller creció a 3x2 pantallas por un requisito de co-op y en la ronda siguiente hubo que
  compactarlo porque experimentar cansaba.
- Y el pivot entero nace de que un laboratorio técnico lleno de instrumentos **no comunicaba
  ninguna fantasía**, mientras que una sola criatura viva sí.

**La hipótesis de trabajo actual, que es lo que habría que someter a juicio externo:** que la vía
para hacer legible un sistema complejo no es simplificarlo ni explicarlo, sino **darle un cuerpo**.
Una criatura que tiene hambre enseña temperatura, química y crecimiento sin una sola línea de
tutorial, porque el jugador ya sabe leer a un ser vivo. Las "herramientas vivas" son esa apuesta
llevada al final: en vez de aprender qué hace cada máquina, convives con seres que tienen carácter.

**El riesgo de esa apuesta, dicho honestamente:** que la criatura sea una capa de simpatía sobre la
misma complejidad, y que el jugador acabe igual de perdido pero además sintiéndose culpable por no
cuidar bien a algo. La señal de que va bien será que Cesar pueda **decir en voz alta** qué clase de
mundo le tocó ("aquí todo acaba en limo", "esta cría salió friolera") sin abrir el diario. La señal
de alarma será que siga necesitando que se lo expliquen.

---

## Playtest 20 → LAS CINCO FAMILIAS DE PATRÓN QUE NUNCA FUNCIONARON, y por qué Cesar no vio
## los cambios que le prometí — pendiente de validar en el editor

**Reporte de Cesar tras probar la build del playtest 19:** *"pude mover los grifos, y las placas de
frío y calor, quedó bien"* · *"ahora empezaba con todo más cerca, está muy bien"* · **"No encontré
cambios en los niveles ni en la morfología de las formas. Tenía ganas de probarlo."** · *"me queda
la duda de si existe un commit 19 que no se generó"*.

Tres dudas distintas, y las tres tienen respuestas distintas. Dos son culpa mía.

### 1. SÍ HABÍA COMMIT 19 — la confusión la causa mi propia numeración
El commit existe (`d1560f6`) y está en GitHub, junto con el del playtest 18 (`a4b7b93`). El push
funcionó. **La confusión es que mis scripts van desfasados un número**: `ca_commit15.cmd` commiteó
el playtest 14, `ca_commit16.cmd` los playtests 15-17, `ca_commit17.cmd` el 18 y `ca_commit18.cmd`
el 19. Es un nombre heredado de cuando la cuenta coincidía y no lo corregí nunca. **A partir de
aquí los scripts llevan el número del PLAYTEST que commitean**, no un contador propio: este es
`ca_playtest20.cmd`.

### 2. LOS ENCARGOS SÍ CAMBIARON — pero el cambio es invisible si no lo buscas
La jornada 1 ya no pide 60 y 80 celdas: pide **32-40 y 42-54**, y varía con la semilla. Está en el
código desplegado y verificado. Lo que no hice fue **decírselo de forma comprobable**: le conté que
"pediría un 40% menos" en vez de darle los números exactos que tenía que ver en pantalla. Un cambio
que el jugador no puede distinguir de "no pasó nada" es, para él, un cambio que no ocurrió.

### 3. LA MORFOLOGÍA: TENÍA RAZÓN, Y EL FALLO ES UN ERROR DE PLANIFICACIÓN MÍO
En el playtest 19 encargué "bajar la escala de los patrones para que se lean con poca materia" y
repartí los archivos así: `Sim/SimRenderer.cs` a un encargo, `Sim/SimStepper.cs` a otro. **Pero la
escala de los patrones vive en LOS DOS SITIOS**: `Vetas` y `Celdas` son puramente posicionales y
las calcula `SimRenderer` (regla 16), mientras que `Manchas`, `Laberinto`, `Dendritas`, `Pulso` y
`Motas` salen de `SimStepper.MorphTick`. El agente que tenía `SimRenderer` bajó las dos que podía
y dejó anotado que las otras no eran suyas; el que tenía `SimStepper` estaba haciendo el
crecimiento dendrítico y no tenía ese encargo.
**Resultado: cambiaron 2 de 8 familias.** Como la firma visual se sortea por semilla, lo más
probable es que los materiales de Cesar cayeran en las otras seis — y no vio absolutamente nada.
Ningún agente se equivocó: **la partición de archivos que hice yo no admitía hacer el trabajo
completo**, y ninguno podía verlo desde su lado.

### LO QUE APARECIÓ AL IR A ARREGLARLO: cinco familias que nunca funcionaron
Antes de tocar nada se montó una réplica en Python del hash y de las cinco funciones `Morph*`, y
se verificó el diagnóstico con los parámetros VIEJOS. El problema era **mucho más grave que una
mala calibración**:

- **`Manchas` y `Laberinto` no podían producir patrón, jamás.** El campo `morph` es de UN SOLO
  valor por celda, y una reacción-difusión biestable de un solo campo **no produce patrones de
  Turing**: se homogeneiza siempre (engrosamiento tipo Allen-Cahn). En un charco acotado colapsaba
  a un tinte casi plano hiciera lo que hiciera `patronEscala`. Ni puntos, ni bandas, ni diferencia
  entre las dos: un degradado. **El comentario de `SimRenderer` que afirmaba lo contrario llevaba
  mintiendo desde el playtest 12** — corregido.
- **`Dendritas`**: una sola semilla, con tiempo suficiente, acababa cubriendo el charco entero
  (percolación). Se veía como un borrón, no como agujas.
- **`Pulso`**: **su fórmula nunca usaba `patronEscala`**. Multiplicador espacial fijo `5`, periodo
  ~51 celdas — más grande que cualquier charco pequeño. Era la única de las ocho cuya escala no
  hacía nada en absoluto.
- **`Motas`**: disparaba tan poco que era invisible más del 90% del tiempo.

O sea que la queja de Cesar del playtest 19 ("necesito mucho material para ver las formas") no era
un problema de calibración: **cinco de las ocho familias nunca funcionaron como decían funcionar**,
desde que se introdujeron.

### LO ARREGLADO
- **Manchas/Laberinto**: `diffDiv` sale de la zona inestable (`24-escala`, 16..23), y —el cambio
  real— un **anclaje de ruido ESTÁTICO por bloque**, calculado con `XorShift.FromCell(0u, bx, by,
  sal)` con **tick constante 0, no `_tick`** (si usara el tick, el mapa cambiaría cada frame y el
  patrón parpadearía). Los bloques "fríos" decaen, los "calientes" sostienen su punto fijo, y la
  difusión redondea la frontera. Verificado: 0 celdas cambian entre el turno 1500 y el 3000 — hay
  estructura y es estable, no hervido.
- **Dendritas**: `seedChanceInv` de 600..2700 a 100..380, más un **mapa estático de orígenes
  elegibles** (1 de cada 6..10 celdas) para que no acabe cubriéndolo todo, y `decayStep` mucho más
  agresivo — con la dirección del efecto de escala corregida (el comentario decía una cosa y la
  fórmula hacía la contraria).
- **Pulso**: pasa a reutilizar el mismo periodo ya calibrado que usan Vetas/Celdas (3..6 celdas).
- **Motas**: `chanceInv` de 900..2300 a 80..360.

### LO QUE NO SE ARREGLÓ, Y HAY QUE DECIRLO
**`Manchas` y `Laberinto` siguen siendo gemelas.** Ya no colapsan —ahora las dos muestran
estructura real— pero se diferencian por **brillo medio** (Manchas ~15-35/255 más oscura), no por
forma. La distinción puntos-vs-bandas que el diseño prometía **nunca fue mecánicamente posible con
un solo campo**. El arreglo de verdad es un Gray-Scott real de DOS campos (U y V), que exige tocar
`CellGrid` — va al backlog como tal, no como afinado.

Y una expectativa que conviene aterrizar: **un charco de 30 celdas son ~30 píxeles.** Ningún
algoritmo dibuja una forma reconocible en 30 píxeles. Lo que sí cambia es que ahora hay estructura
visible en vez de un tinte plano, y que a 150 celdas las familias se distinguen de verdad
(Dendritas ramifica, Pulso hace bandas diagonales, Motas son chispas aisladas).

### Sobre el crecimiento dendrítico del vivium (playtest 19), que tampoco pudo ver
No es un fallo: **el vivium solo aparece en la JORNADA 2**, cuando el Maestro deja el retoño. Si se
está atascado en la 1, es inalcanzable. Atajo para probarlo sin jugar la progresión: **F3** abre la
paleta de desarrollo (la build es `Development`), pintar vivium + nutriente en una cuba y encender
la placa dentro de la banda.

### Verificación
Réplica en Python de las cinco funciones con los hashes reales, PNGs comparativos viejo/nuevo por
familia y por tamaño de charco, y una auditoría independiente sobre `SimStepper.cs`: compilación,
determinismo (cero `UnityEngine.Random`, doble búfer intacto, mapas estáticos con tick 0), cero
allocs en el hot path, rangos de todos los divisores para `patronEscala` 1..8, y confirmación de
que `GrowthTick` es byte a byte idéntico. La auditoría encontró además que el jitter inicial
(±100 sobre un `kill` de 10..46) dejaba el 25-42% de los bloques pegados al clamp; se rediseñó a
un esquema binario derivado del punto crítico analítico (`S ≤ 255²/1024 ≈ 63.5`), con 0% de
saturación medida. **Sin compilador de C# en el sandbox: la compilación real sigue pendiente.**

---

## Playtest 19 → EL TALLER SE ENCOGE Y SE MUEVE, LA VIDA CRECE CON FORMA, Y EL MAGO PIDE MENOS
## — ronda nocturna en autónomo, pendiente de validar en el editor
Ronda dirigida por Opus 5 (survey de viabilidad, 4 encargos en paralelo con propiedad disjunta,
3 auditorías, e integración a mano de lo que ningún encargo podía cerrar); Sonnet 5 escribió el
código. **Cesar dejó el encargo y se fue a dormir**: *"sorpréndeme, muéstrame tu mejor esfuerzo,
confío en ti, mañana lo pruebo"*. Todo lo que sigue se decidió sin poder preguntarle.

**EL HILO COMÚN DE SU REPORTE, que es lo que ordenó las prioridades:** experimentar CANSA. Todo
está lejos, hace falta demasiada materia para ver un patrón, y el mago pide cantidades que se
comen la jornada. Cuatro quejas distintas, un solo problema de fondo — así que la ronda entera va
de **bajar el coste de probar cosas**.

### 1. EL TALLER COMPACTO (`SimLevelBuilder.cs`, `WorkshopBackdrop.cs`)
Cesar: *"debería transmitir la sensación de estar yo en un lugar pequeño que luego quizás con los
niveles se me amplíe el espacio, pero ahora eso no es necesario"*.
El mundo NO se encoge (768x288 es el tamaño final y el desbloqueo por niveles lo va a usar): lo
que cambia es la distribución. La bandeja fría y el estante de redomas bajan a vivir **encima del
propio banco de grifos** (x262..374, y=236..245) y la Tolva se acerca 137 celdas
(`EntregaX1` 607→470). Nada del banco, la pila, el pilar, el pozo ni el sótano se movió: geometría
ya validada no se toca dos veces.

| Aparato | Antes | Ahora |
|---|---|---|
| grifos | 25 | 25 |
| placa de cuba B | 64 | 64 |
| **piedra gélida** | 107 | **49** |
| **estante de redomas** | 154 | **50** |
| **boca de la Tolva** | 225 | **88** |
| placa de cuba A (lejos a propósito) | 216 | 216 |

**La verificación importa tanto como el cambio.** En el playtest 16 un pilar invadió la pila y no
se detectó. Esta vez se comprobó con una **simulación celda a celda sobre un grid real de 768x288,
parseando las constantes directamente del `.cs` final** (no copiadas a mano, para eliminar el error
de transcripción): 210 pares de rectángulos comprobados, **cero solapes nuevos** (los 4 existentes
son piezas anidadas dentro del sótano y la pared compartida pila/pilar, ya intencionales); todos
los interiores libres; paredes completas; el punto de aparición del aprendiz sigue cayendo en aire.
Bandeja y estante dejan 8 celdas de hueco entre sí y 3 celdas de aire sobre el pilar de grifos.

### 2. EL MODO MUDANZA — mover los aparatos a su antojo (`Mudanza.cs` nuevo, + 6 archivos)
Cesar: *"ya tengo ganas de mover las cosas a mi antojo"*. Es el paso 5 de la fase acordada.
**Tecla V.** Clic izq. agarra el aparato más cercano al cursor, clic izq. otra vez lo suelta.
Silueta verde/roja siguiendo el cursor. Grifos, placas ígneas y piedra gélida son movibles; la
Tolva y el estante no (todavía).

**La decisión técnica que lo hizo barato:** los cuatro aparatos ya estaban 100% parametrizados por
celda desde el playtest 15, así que mover uno es recalcular su ancla — no reconstruirlo. Pero
`BuildVisual()` **no es idempotente**: `MaquinariaSprites.CrearCapa` siempre hace `new GameObject`,
así que llamarlo dos veces duplicaría todos los hijos y dejaría los viejos huérfanos y visibles en
el sitio antiguo, para siempre. Y volver a llamar a `Init()` es peor todavía: resetearía
`favorCostPerActivation` y `Bloqueado` a sus valores por defecto, **volviendo a sellar el grifo de
Azoth**. Por eso cada aparato tiene un `Reposicionar(Vector2Int)` que reutiliza los hijos ya
creados y **nunca** pasa por `BuildVisual` ni por `Init`. Escrito en el docblock de los tres, porque
es exactamente la trampa que alguien repetirá.

**Mientras llevas un aparato, el aparato real no se toca**: solo se mueve una silueta genérica, y
`Reposicionar` se llama UNA vez, al soltar bien. Por eso cancelar es literalmente gratis.

**Y SE ARREGLÓ EL SOLAPAMIENTO DE MODOS QUE EL CINCEL DEJÓ A MEDIAS.** Su propio docblock lo
documentaba como pendiente: `Flask` seguía leyendo clics mientras el cincel estaba activo. Ahora
**frasco, cincel y mudanza son tres modos excluyentes de verdad**, con la exclusión simétrica en
los tres sentidos. El último lado (Cincel → Mudanza) lo cerró el director al integrar: ningún
encargo era dueño de los dos archivos a la vez, y ese hueco es el precio recurrente de trabajar en
paralelo — hay que ir a buscarlo, no aparece solo.

**LA RED DE SEGURIDAD, que salió de la auditoría de jugabilidad y es lo mejor de esta parte.**
Mover un aparato fuera de su recipiente no rompe nada visible, pero **vuelve imposibles encargos
enteros en silencio**: la piedra gélida fuera de la bandeja sigue enfriando, solo que ya no donde
el Maestro sembró la semilla, así que "algo helado" deja de ser cumplible y el juego no dice por
qué. La respuesta NO podía ser prohibirlo (Cesar pidió justo lo contrario, y una herramienta que
te impide equivocarte tampoco te deja descubrir): la respuesta es que **deshacer sea trivial**.
**R con algo agarrado cancela ese arrastre; R con las manos vacías devuelve TODOS los aparatos a
su sitio de fábrica.** Se experimenta sin miedo porque el camino de vuelta es una tecla.
(Detalle que casi cuesta caro: las dos listas paralelas — aparatos y anclas de fábrica — tenían un
punto de limpieza que quitaba solo de una. Habría hecho que R devolviera cada aparato al sitio de
otro, que es peor que no tener R.)

También se arregló que el **audio de un grifo siga a su boquilla** al moverlo: `DirectorDeAudio`
cacheaba las coordenadas UNA vez al crear las voces.

### 3. QUE LA VIDA CREZCA CON FORMA (`SimStepper.cs`, `Universe.cs`)
Cesar: *"lo que no vi por más que intenté es que algo crezca con formas que vengan de algoritmos,
fractales qué sé yo, ya habíamos hablado de eso; solo vi diferencias de viscosidad y propagación"*.
Tenía razón: el campo morfológico del playtest 12 da TEXTURA, que es piel — la silueta del
organismo seguía siendo un borrón redondo, porque **cualquier célula del cuerpo podía engendrar**.

**El cambio es una sola idea:** una mancha crece porque engendra todo el cuerpo; una rama crece
porque **solo engendran las PUNTAS**. Ahora una célula solo compite por el nutriente si tiene pocos
vecinos vivos; en cuanto queda rodeada pasa a ser tallo y no vuelve a intentarlo. Con eso solo, las
manchas se vuelven dendritas. Encima: persistencia de dirección, bifurcación, y **4 parámetros de
"hábito" sorteados por semilla** (cuántos vecinos tolera una punta, probabilidad de bifurcar,
cuánta persistencia, y un sesgo vertical que puede ser positivo — trepa hacia la luz — negativo —
se entierra hacia el nutriente — o nulo). La vida de un universo se reconoce de la de otro.
Y juega a favor de su otra queja: **una silueta ramificada se distingue con 30 celdas; una mancha
necesita 300**.

**El fallo mortal de este mecanismo es que la colonia se autobloquee** (todas las células con
demasiados vecinos, nadie puede crecer, cultivo muerto para siempre). Se investigó con un modelo
en Python antes de escribir nada: con tolerancia 1 vecino, **el 100% de 60 semillas terminaba en
un anillo cerrado autobloqueado**; con vecindad de Moore (filamentos más finos, más bonitos),
27 de 60. Se eligió la variante segura y el rango de tolerancia se fijó en 2-3, **nunca 1**, con el
dato escrito en el propio campo. La auditoría independiente lo reverificó después con 1.000
tiradas sobre la cuba real y el retoño real del Maestro: **cero atascos**.
`VivGrowChancePct` sube de 60 a 75 para compensar el freno — no es un rebalanceo de dificultad,
es que cultivar cueste lo mismo que antes: ~46 ticks/120 celdas con la regla vieja, ~52 sin
compensar, ~40 compensada.

### 4. QUE EL PATRÓN SE LEA SIN ESFUERZO (`StorageRack.cs`, `FlaskHud.cs`, `SimRenderer.cs`)
Cesar: *"los patrones son visibles y son distintos en cada generación, eso está bien, pero aún
siento que necesito mucho material para ver las formas, y me cuesta un esfuerzo visual cuando los
meto en los frascos; quizás los frascos pueden ser más gordos"*.
- **Redomas +53% de ancho, +80% de área visible**, y —esto es lo que las hace útiles— sus medidas
  ahora se **derivan del ancho real del estante en tiempo de ejecución**, no de constantes fijas:
  si el estante se mueve o cambia, se recalculan solas.
- **El swatch del frasco** pasa de 18 a 28 téxeles de lienzo y +31% en pantalla.
- **El periodo de los patrones baja de 5-12 celdas a 3-6.** Agrandar el recipiente sin achicar el
  periodo solo habría enseñado un trozo gigante de una sola repetición (regla 24). Repeticiones en
  la bandeja fría: antes 3.7-8.8, ahora 7.3-14.7. En un charco de ~30 celdas, la escala más gruesa
  pasa de un borrón sin repetición a mostrar 1-2 repeticiones reales.
- Se añadió un **assert de arranque** que revienta con `LogError` si el periodo máximo deja de
  caber tres veces en el recipiente más estrecho — para que el próximo que suba el techo de
  `patronEscala` se entere al arrancar, no tres rondas después. Y la cifra "bandeja fría 46x6" de
  la regla 24 estaba obsoleta: la medida real es 44x7, y ahora se lee de las constantes.

### 5. EL MAGO PIDE MENOS, Y NO PIDE LO MISMO CADA PARTIDA (`OrderSystem.cs`)
Cesar: *"aún estoy atorado con los niveles de cosas que me pide el mago, en especial el nivel 1,
que creo que siempre es el mismo"*. Dos quejas en una frase, las dos ciertas.
Todos los umbrales estaban calibrados contra el TIEMPO de jornada (¿cabe en el 60-70% de los
360s?) y esa cuenta era correcta — **pero medía la pregunta equivocada**. El juego no va de
producir en cantidad, va de experimentar; un umbral que "cabe en el tiempo" te obliga igualmente a
pasarte ese tiempo acarreando frasco. Cumplir tiene que ser el peaje corto que te deja seguir
jugando, no la jornada entera. Ahora todas las cantidades pasan por un `Volumen()` que **recorta
al 60% y añade un temblor de ±12% por semilla** — y la jornada 1, que estaba escrita con
constantes y era literalmente idéntica cada partida, por fin usa el `rng` sembrado con
(semilla, día) que este archivo ya construía y solo usaba la jornada 3.
**Las RECOMPENSAS no se tocan**: toda la aritmética de desenlaces (120/180/260 de Favor, máximo
teórico 305) depende del Favor, no de las celdas, así que sigue cuadrando exactamente — verificado
sumando las recompensas del código en la auditoría.

### 6. Verificación
Un survey de viabilidad previo, 4 encargos con propiedad de archivos disjunta, y **tres auditorías
independientes**: compilación cruzada (los 4 agentes escribieron sin verse: interfaz `IMovible`
implementada por tres clases, símbolos cruzados, firmas de `Init`, meta y guid del archivo nuevo),
**jugabilidad** (¿se puede terminar la partida con la geometría nueva? ¿puede el jugador romperse
la partida con la mudanza? ¿se atasca el cultivo? ¿cuadra la economía?) y una verificación final
tras las ediciones a mano del director. De la auditoría de jugabilidad salió la red de seguridad
de la tecla R, que es el mejor cambio de la ronda y no estaba en el plan.
Se arreglaron de paso dos avisos XML preexistentes (`CS1570`/`CS1574`) en `Cincel.cs` y
`OrderSystem.cs`. Ningún archivo encogió.
**Sigue sin haber compilador de C# en el sandbox: la compilación real está pendiente del editor.**

---

## Playtest 18 → LA QUÍMICA YA NO ES LA MISMA EN TODA SEMILLA: leyes generadas, un universo
## con TESIS, y el diario como motor de curiosidad — pendiente de validar en el editor
Ronda dirigida por Opus 5 (contrato de API congelado, gramática, auditorías); Sonnet 5 escribió el
código en 2 encargos de propiedad disjunta. **Es la fase 3 del plan acordado en el playtest 14.**

**EL DIAGNÓSTICO DE PARTIDA, de Cesar:** *"una semilla nueva solo cambia la piel porque la piel es
lo único que se genera"* y *"solo soy capaz de descubrir dos o tres reacciones"*. Los números le
daban la razón sin discusión: la tabla tenía **7 reacciones IDÉNTICAS en toda semilla** (solo
variaban `chancePct` y las bandas), y el juego solo sabía anunciar **DOS** leyes, cableadas a mano
con dos `bool` (`_leyCristalDescubierta`, `_leyVivumDescubierta`) atados a dos tipos de evento.
Trece rondas de riqueza de presentación sobre una capa de sistemas de siete entradas.

**LAS DOS DECISIONES QUE TOMÓ CESAR ANTES DE EMPEZAR** (se le preguntaron a propósito porque
cambian la identidad del juego, no solo el código):
1. *Alcance de la química sorteada*: **la variante ATREVIDA** — lo innominado puede reaccionar CON
   el vocabulario del taller ("en este universo el aceite y el líquido del Maestro hacen algo").
   Lo que el vocabulario hace POR SÍ SOLO no cambia jamás (el agua sigue apagando el fuego,
   congelándose e hirviendo igual en toda semilla: eso vive en `ApplyPhase`, intocado).
2. *Criterio del diario*: **solo lo PRESENCIADO, con hueco visible**.

---

### 1. LA GRAMÁTICA DE LEYES (`Sim/LeyDelUniverso.cs`, nuevo, + `Universe.cs`)
Sobre el núcleo fijo de 7 reacciones (las que sostienen los encargos: si desaparece la
cristalización hay semillas imposibles de completar) se sortean **5-8 leyes más** por semilla. Lo
que las hace sentirse distintas no es el par de materiales sino la **FORMA**, que es un eje nuevo:

- **Transmutación** `A+B -> C+B` — B es CATALIZADOR, no se gasta. (La forma de la cristalización.)
- **Fusión** `A+B -> C+C` — los dos se vuelven la misma cosa nueva.
- **Consumo** `A+B -> Empty+C` — A se destruye y B se transforma. (La forma del ácido.)
- **Liberación** `A+B -> C+gas` — suelta algo que SE VE SUBIR: la ley más fácil de presenciar de lejos.
- **Contagio** `A+B -> A+A` — A se propaga comiéndose a B. La forma peligrosa.
- **Crecimiento** — la del Vivium, que no es una reacción de contacto y vive en `GrowthTick`.

Más una **CondicionTermica** (`Cualquiera`/`Frio`/`Calor`) cuyas bandas quedan a propósito por
debajo y por encima del ambiente, para que una ley con condición NO pueda dispararse sola en el
taller: si se dispara a 20°C, la condición no significa nada.

### 2. LA AFINIDAD DEL UNIVERSO — el añadido que salvó la ronda
La primera versión pasó todas las auditorías y aun así **estaba mal a nivel de diseño**, y lo dijo
el propio agente revisor sin que nadie se lo preguntase: *"el producto no tiene lógica causal
perceptible... cada ley es arbitraria en aislado. Si el jugador intenta generalizar entre leyes
sorteadas de la MISMA semilla, no va a encontrar un patrón, porque no existe"*. Su veredicto:
*"la profundidad real de la gramática es la de 5 plantillas con sustantivos intercambiables"*.

Eso choca de frente con la fantasía del juego — **no se domestica una lista de accidentes**. La
corrección: en tiempo de horneado se sortean 1-2 **materiales afines**, y los pickers de producto
los prefieren un ~55% de las veces (solo entre candidatos que las restricciones ya habían
aceptado: la afinidad es una PREFERENCIA dentro del picker, nunca una excepción a una regla).

El efecto medido: **el 54.4% de las leyes de una semilla convergen en su material afín**, con el
grueso de las semillas (70%) entre 2 y 5 leyes convergentes sobre 5-8. Casi ninguna semilla se
queda sin tesis (0.9%) y casi ninguna se vuelve monótona (0.2% converge al 100%). En lenguaje
llano: la semilla de ejemplo 31337 se lee de un tirón como **"aquí todo acaba en limo"**, y la
1000 como un mundo de fusión fría que tira a semilla de cristal. Eso ya es una ley POR ENCIMA de
las leyes de contacto: generalizable, sorprendente al cambiar de semilla, y sobre todo
**NOMBRABLE** — que es exactamente lo que Cesar lleva pidiendo desde el playtest 14 (*"las
texturas solo me inducen a poner nombres como rojo bonito"*). Un mundo con tendencia se puede
bautizar; una lista de accidentes no.
`Universe.AfinidadDelUniverso` queda expuesto público: el gancho evidente de una ronda futura es
que el RUMOR del Edicto la insinúe sin decirla (no se tocó `EdictoDescripcion` esta ronda).

### 3. LAS RESTRICCIONES, Y LOS DOS AGUJEROS QUE TENÍA EL CONTRATO
Diez restricciones duras (R1-R10) protegen la partida. Las dos que de verdad importan:
- **R1 — al menos un reactivo tiene que ser INNOMINADO.** Implementa la decisión de Cesar y a la
  vez protege el taller: garantiza que dos materiales del vocabulario NUNCA reaccionan entre sí,
  así que el agua y la arena de la pila jamás hacen algo raro solas. El vocabulario solo se
  comporta de forma extraña **en presencia de algo extraño**. Medido: el **76.4%** de las leyes
  sorteadas tocan al menos un material del vocabulario — la decisión de Cesar se nota en los
  números, no se diluye.
- **R4 — el par no puede colisionar con ninguno ya presente**, comprobado en los DOS órdenes.
  `ReactionEngine` es un lookup de UNA entrada por par: una colisión **sobreescribiría en silencio
  una ley del núcleo** y podría dejar una partida sin cristalización.

**Y DOS AGUJEROS QUE ERAN MÍOS, NO DEL CÓDIGO** (los encontró la auditoría adversarial; el
implementador había seguido el contrato al pie de la letra):
- **R5 protegía el agua y se olvidaba de los otros grifos.** Escribí *"la víctima de un Contagio
  nunca puede ser `Water`, porque sale de un grifo infinito"*. Razón correcta, lista incompleta:
  hay CINCO grifos (agua, arena, aceite, nutriente abiertos desde el minuto uno; azoth en la
  jornada 2) y `Dispenser.EmitTick` no tiene tope de cantidad. Un contagio con víctima `Sand`,
  `Oil` o `Nutrient` producía **exactamente el bucle de materia infinita que la regla pretendía
  impedir**, pasando todas las comprobaciones. Corregido con `MaterialesDeGrifo`.
- **R6 solo miraba `Consumo` y `Contagio`.** La razón que di era que el vivium es la cadena más
  lenta del juego y un encargo de "algo vivo" se vuelve imposible si algo se lo come pasivamente —
  pero en `Fusion` los dos reactivos se vuelven un tercero y en `Liberacion` los dos cambian: el
  vivium moría igual. Y el mismo agujero tenía un segundo material que no vi: **`CrystalSeed`**,
  que `MasterSupplies` entrega 60 celdas UNA vez y que la cristalización trata como catalizador —
  una ley `Fusion(CrystalSeed, Water)` dejaba los encargos de cristal imposibles en cuanto la
  semilla tocara agua, que está por todas partes. Sustituida por: **`Vivium` y `CrystalSeed` solo
  pueden aparecer como reactivo en la posición de CATALIZADOR de una `Transmutacion`**.
  **COSTE ACEPTADO Y CONSCIENTE**: esos dos materiales pasan a tener una sola frase posible ("X se
  convierte en Y al tocar el vivium, que sigue igual"). Se pierde sabor en dos de los seis
  innominados, justo dos de los más importantes. Proteger que ningún encargo sea imposible vale
  más que la variedad, pero queda escrito que fue un intercambio, no un descuido.

Validación del endurecimiento: se modeló la lógica de aceptación/descarte en Python y se simularon
**20.000 semillas** — tasa de aceptación por intento 48.8%, y **cero** semillas quedándose cortas
de leyes sobre 130.007 huecos pedidos (con 200 intentos por hueco).

### 4. QUE UNA LEY SE PUEDA PRESENCIAR (`SimEvents.cs`, `SimStepper.cs`, `ReactionEngine.cs`)
Antes, **una reacción al dispararse no emitía nada identificable**: solo dos casos cableados
empujaban evento, y el evento llevaba UN material, ni el segundo reactivo ni cuál de las
reacciones había sido. Con química sorteada eso es inservible.
- `SimEventType.Ley = 6` + `SimNotableEvent.leyIndice`. Los seis eventos viejos se siguen
  empujando EXACTAMENTE igual, con `leyIndice = -1`: los lee el audio y el sistema de testigos, y
  cambiar uno rompe cosas lejos (regla 8).
- `ReactionEngine` expone `Count`/`At(i)`/`TryGet(...,out int index)`.
- **INVARIANTE, de la que depende todo**: `Leyes[i]` describe exactamente `Reactions.At(i)` para
  `i < Reactions.Count`, y la ley de crecimiento va la última, en `LeyCrecimientoIndice ==
  Reactions.Count`. Si se desalineara, el jugador descubriría la ley equivocada. Verificada con
  assert de solo-editor y trazada a mano en la auditoría, incluido el caso de un sorteo descartado.
- **LIMITADOR DE RITMO, no opcional**: el anillo tiene 256 entradas y un ácido disolviendo ya
  genera decenas de eventos por tick. Si cada reacción empujara además un evento `Ley`, el anillo
  daría la vuelta antes de que el consumidor leyese y **una ley podría no descubrirse nunca** —
  bug intermitente y dependiente de la carga, de los que tardan tres rondas en verse. Se empuja
  como mucho un evento por ley por segundo, con centinela `uint.MaxValue` para el caso "nunca
  empujado" (usar 0 habría hecho que la primerísima ley del tick 0 no se empujara jamás: el fallo
  de arranque clásico, comprobado a propósito en la auditoría).

### 5. EL DIARIO COMO MOTOR DE CURIOSIDAD (`SubstanceKnowledge.cs`, `JournalHud.cs`)
- Los dos `bool` cableados pasan a ser un registro real **por índice de ley**, con validación de
  rango: un evento con índice fuera de rango se ignora, no tira la partida.
- **El texto del banner se GENERA desde el descriptor**, con una plantilla por FORMA — porque la
  forma es lo que hace memorable una ley y lo que distingue una semilla de otra. Ejemplo real:
  *"El retoño de la cuba se propaga con calor: en cuanto toca la arena, la arena se convierte
  también en el retoño de la cuba. Basta un punto de contacto."* Respeta la regla 13/17 al pie de
  la letra: mientras algo siga innominado se describe por ORIGEN, nunca por su identidad interna.
  Y menciona la condición térmica cuando no es `Cualquiera`, porque sin eso el jugador no sabe
  reproducir lo que acaba de ver.
- **CAMBIO DE CRITERIO**: antes el diario revelaba una ley con solo conocer sus dos ingredientes,
  aunque nunca la hubieras visto ocurrir — un criterio derivado, razonable en su momento. Ahora
  una ley entra **si y solo si la has presenciado**. Las que faltan **ocupan sitio** como renglones
  idénticos entre sí (no filtran ni materiales, ni forma, ni condición) y un contador **"N de M"**
  en la cabecera de sección dice cuánto queda. Es lo que convierte el diario en una pregunta en
  vez de en un manual.
- La cola de banners pasa de 2 a 8 (el comentario decía *"solo 2 leyes de este tipo existen"*, ya
  falso). Si se llena se pierde el AVISO pero **nunca** el registro: son dos cosas distintas y
  confundirlas habría sido un bug silencioso.
- **La trampa de la ronda**: la firma de caché del diario. Si no incluye `LeyesVersion`, descubrir
  una ley no repinta nada y el sistema "funciona" en todo menos en lo nuevo, sin error visible.
  Incluida — y de paso reescrita a desplazamientos por rangos, porque la versión con primos tenía
  una colisión real (991 rebautizos compensaban un material descubierto de más).

**BUG LATENTE ENCONTRADO DE PASO**: `JournalHud` cableaba `productB = Vivium` para la ley de
crecimiento como truco de presentación; el dato real es `Empty` (el nutriente se consume). Al
pasar a leer el descriptor habría salido *"Vivium + Nutriente, templado -> ??? nuevo"*.

### 6. Verificación
Cuatro pases con agentes independientes: auditoría adversarial de la capa Sim (¿puede una semilla
generar una partida rota?), revisión de la costura y la capa Game, y una verificación final de
`Universe.cs` tras las dos tandas de correcciones. Ningún archivo encogió (regla 26): todos
crecieron. **Sigue sin haber compilador de C# en el sandbox: la compilación real está pendiente
del editor de Cesar.**

---

## Playtest 17 → FUERA EL CLIMA POR ZONA, Y LA CAUSA REAL DEL "AGUA DEL GRIFO CONGELADA"
## (que NO era el clima) — pendiente de validar en el editor
Ronda dirigida y escrita por Opus 5. Dos archivos de código real tocados, cinco de documentación.

**Lo que pidió Cesar, literal:** *"yo creo que vamos a dejar lo del sótano frío fuera por ahora,
quizás no sea necesario porque si van a construir su mapita como quieran pues puede que no quieran
que esté condicionada la temperatura o caso contrario que roleen semilla hasta que les toque frío.
Pero principalmente porque da problemas: actualmente el agua me sigue saliendo congelada y probando
en la hornilla de la izquierda a media temperatura se volvía agua pero los bordes se hacían hielo.
Solo corrige eso y hago un commit, el resto se ve bien."*

**1. LA CAUSA REAL DEL AGUA CONGELADA — y por qué la investigación del playtest 16 se equivocó.**
`Dispenser.EmitTick` llamaba a `AlkahestSim.Paint`, que **NO TOCA `temp`**. Una celda recién
emitida heredaba la temperatura que ese hueco tuviera de antes: si la boquilla o la pila se habían
enfriado alguna vez (un charco frío previo, hielo que estuvo ahí, la piedra gélida cerca), el agua
del grifo nacía YA CONGELADA — en cualquier seed, con clima o sin él. Es literalmente el mismo
fallo que "pintar hielo produce agua" (regla 22), que ya se corrigió una vez en `DevPalette` y
nadie fue a buscar al resto de sitios que crean materia de la nada.
Y explica el SEGUNDO síntoma de Cesar, el que descartaba definitivamente la teoría del clima:
*"en la hornilla a media temperatura se volvía agua pero los bordes se hacían hielo"* — no es que
los bordes se congelaran, es que **llegaba ya helada** y solo se derretía sobre el 40% del fondo
que cubre la placa (`HeatPlate.FootprintFraction`, playtest 14). Un ambiente frío habría congelado
el centro también; una placa caliente derritiendo una capa que llega helada deja exactamente ese
patrón. El síntoma que parecía confirmar el clima era la prueba de que no era el clima.
Corregido con `PaintStable` en los DOS puntos de emisión de `Dispenser` (el chorro y el rebose).
`AlkahestSim.PaintStable` deja de ser "solo para la paleta de dev" y pasa a tener una regla
general escrita en su docblock: **si algo INTRODUCE materia en el mundo en vez de moverla, usa
`PaintStable`; `Paint`/`PaintCell`/`PaintRect` son para lo que MUEVE materia que ya existía y
lleva su propia temperatura consigo** (Flask al verter, DeliveryChute, MasterSupplies).

**LA LECCIÓN, que vale más que el bug** (escrita también en el docblock de `SimLevelBuilder`):
la investigación del playtest 16 fue rigurosa y llegó a la conclusión equivocada. Midió bien —
demostró con números que el degradado frío del sótano NO llegaba a la boquilla (41 filas de base
garantizada de por medio como mínimo, 80 contra el frío puro) — y luego, en vez de parar ahí,
firmó una sentencia contra el siguiente sospechoso disponible ("es varianza de semilla, un 38% de
seeds congelan solas en el sótano, no es un bug sino personalidad del universo") sin someterlo a
la misma exigencia. **Descartar un sospechoso no es identificar al culpable.** Cuando el síntoma
es "materia recién creada aparece en un estado imposible", el primer sitio que hay que mirar es
siempre QUIÉN LA CREA y a qué temperatura la deja — no el entorno donde aparece.

**2. EL CLIMA POR ZONA, RETIRADO ENTERO (los dos, no solo el frío).**
Se borran `CultivoAmbientRaw`(raw 73/26°C), `SotanoAmbientRaw`(raw 62/4°C), `ClimaGradienteX/Y`
(45/40) y las funciones `AmbientForSurfaceX`/`AmbientForSotanoY`. `PaintClimate` sobrevive como
único punto de entrada pero pinta `CellGrid.AmbientRaw` uniforme en todo el mundo.
Cesar nombró solo el sótano; se quitan los dos por tres razones:
- Su razón (1) es SIMÉTRICA: el clima por zona supone que las zonas son fijas, y la fase de
  "taller movible" dice justo lo contrario. Un CULTIVO cálido deja de tener sentido en cuanto el
  jugador puede poner la cuba donde quiera — y peor, convierte una decisión suya en algo que el
  plano ya decidió por él.
- Dejar solo el cálido reintroduciría por la puerta de atrás la **asimetría calor/frío** que Cesar
  ya reportó en el playtest 13 ("la placa fría parece irradiar más fuerte que el calor"): las
  placas ígneas empujarían desde 26°C regalados mientras la piedra gélida pelea desde los 20°C
  base de LABORATORIO. La ventaja la tiene que dar el APARATO, no la casilla.
- El coste de quitar el cálido está MEDIDO y es casi nulo: la banda de crecimiento del Vivium es
  30..60°C ±shift (`Universe.growMinC/growMaxC`), así que los 26°C de CULTIVO nunca metían nada
  DENTRO de la banda por sí solos — solo acortaban el salto en 6°C. Quien cría el Vivium sigue
  siendo la placa.

**LO QUE GARANTIZA AHORA UN AMBIENTE UNIFORME A 20°C:** `Water.freezesAt` = `CToRaw(waterFreezeC)`
con `waterFreezeC` uniforme en los enteros -15..15, o sea raw 52..67 — el PEOR caso (raw 67) sigue
3 unidades raw por debajo de la base (raw 70). **En NINGUNA seed puede el ambiente congelar agua
por sí solo, en NINGÚN punto del mundo.** Antes esa garantía solo valía para LABORATORIO/ENTREGA;
ahora vale para el mundo entero, y con ella desaparece de raíz la clase entera de "algo se congeló
solo" — el clima deja de poder ser sospechoso nunca más.

**LA INFRAESTRUCTURA SE QUEDA, y no por inercia.** `CellGrid.ambient` (un byte por celda) y el
tirón por celda de `SimStepper.DiffuseTemperature` NO se tocan: hoy cuestan lo mismo que una
constante (una lectura de array) y son el vehículo del clima que SÍ vuelve — el que **crea el
jugador** (una fragua que entibia lo que tiene alrededor, una sala que se enfría porque él la
selló). Eso es local por naturaleza y no cabe en una constante global. Clima ganado, no clima
heredado del plano. Documentado en los tres sitios (regla 15: las ideas descartadas se escriben en
el código, no solo las que se quedan) para que nadie lo reimplemente por zonas fijas creyendo que
es una idea nueva.

**3. BARRIDO DE DOCUMENTACIÓN OBSOLETA.** Quitar el clima dejaba SIETE comentarios mintiendo por
el código (el diagrama ASCII de zonas con sus "26 °C"/"4 °C", el docblock de `CellGrid.ambient`
que era la explicación canónica del campo, la justificación de las placas ígneas en
`AlkahestGameBootstrap`, la de la piedra gélida —cuyo argumento geométrico sigue en pie y de hecho
se refuerza—, la revisión de zona de `MasterSupplies`, el análisis de estabilidad de
`DiffuseTemperature` y la propia línea de llamada a `PaintClimate`). Todos corregidos, cada uno
diciendo QUÉ decía antes y por qué ya no vale, en vez de borrarlo sin dejar rastro. De paso se
corrigió un `VatBX0 (118)` que llevaba obsoleto desde el playtest 16 (hoy 187) y se retiró el
`using System;` de `SimLevelBuilder` (lo único que lo necesitaba era el `Math.Round` de los
degradados).

**Verificación:** dos pases de revisión con agente independiente (Sonnet 5) sobre el parche
completo — (a) referencias colgantes a los símbolos borrados, firma real de `PaintStable` contra
los dos puntos de llamada, accesibilidad de `CellGrid.ambient`, y **enumeración de TODO método y
constante presente en HEAD y ausente en la copia de trabajo** (regla 26: `SimLevelBuilder.cs`
encoge 5.701 bytes y hay que demostrar que la merma es exactamente la prosa reescrita más los seis
símbolos nombrados, nada más — demostrado); (b) caza de errores de sintaxis, bloques `<summary>`
rotos, XML mal formado en comentarios de documentación (CS1570) y balance de llaves. No hay
compilador de C# en el sandbox: la verificación es por lectura, así que **la compilación real
sigue pendiente del editor de Cesar**.

---

## Playtest 16 → EL CHASQUIDO DEL BUCLE, RÓTULOS QUE SE VAN DE PANTALLA, EL HUD QUE SE PLIEGA,
## LO BÁSICO CERCA DEL INICIO Y EL CINCEL — escrito a posteriori (ver nota al final)
Ronda dirigida por Opus 5; Sonnet 5 escribió el código. **Validada por Cesar** salvo el agua del
grifo, que siguió fallando y se resolvió en el playtest 17.

Los seis puntos de Cesar y qué se hizo con cada uno:

**1. *"El sonido popea una vez cada 5 seg más o menos sin que haga nada yo."*** El lecho ambiental
es un bucle de exactamente 4.5s y el chasquido era su costura. El `SuavizarBucle` del playtest 9
mezclaba la cola hacia `buffer[i]` en vez de hacia `buffer[0]` — solo mejoraba el salto de VALOR
2.7x y empeoraba el de PENDIENTE, que es justo lo que el oído detecta como clic. Reescrito en dos
etapas (primero valor, después pendiente) en `SintetizadorSfx.SuavizarBucle`.

**2. *"El título 'tolva del maestro' acompaña la pantalla"*** — el rótulo seguía a la cámara en
vez de quedarse en su sitio. Causa: `UiStyles.Globo` recortaba la posición contra los bordes de
pantalla, algo inofensivo con una cámara fija y un bug en cuanto la cámara sigue al jugador
(playtest 15). Se añadieron `DentroDePantalla`/`MargenFueraDeCuadro` a `UiStyles`: un rótulo de
mundo que se sale del cuadro simplemente NO SE DIBUJA, en vez de deslizarse al borde.

**3. *"Los encargos del Maestro ahora se sienten como que estorban un espacio construible, ¿qué se
te ocurre que puede ser lo mejor?"*** — pregunta abierta, no orden. Respuesta: **no ocultarlos**
(son el objetivo del jugador) pero tampoco dejarlos ocupando una esquina entera de un taller que
ahora se construye. `OrdersHud` reescrito con dos estados: COLAPSADO por defecto (una línea por
encargo, solo el progreso "49/60") y EXPANDIDO con la tecla **O** (descripción completa + barra).
Se expande solo cuando pasa algo que merece la atención: encargo nuevo del día o cambio de estado
grueso (sin tocar → en progreso → completo). El pulso se rastrea por `Order.Id`, que sobrevive a un
re-bautizo, así que renombrar una sustancia nunca dispara un falso positivo.

**4. *"El espacio en general se siente bien, también se siente súper grande... tendría que estar
lo básico en el primer cuarto."*** Sin encoger el mundo (el tamaño 3x2 es el pedido en el playtest
15) se redistribuyó: `VatBX0` 118→187 y `EntregaX1` 751→607. Lo esencial (grifos, pila, una cuba,
la Tolva) pasa de repartirse por el 75% del ancho a caber en el 47%. Medido desde el punto de
aparición del aprendiz (x≈303): la cuba B pasa de 122 celdas a 53, la boca de la Tolva de 370 a
226. **Sin mover ni una celda** del banco de grifos, la pila, el pilar, el pozo o el sótano — la
geometría ya validada no se arriesga otra vez. Se quedan LEJOS a propósito la cuba A, el sótano
entero y la bandeja fría/estante. Efecto secundario gratis: el contrafuerte de la Tolva ahora es
MÁS ancho (251 celdas de piedra visible en vez de 107), así que se ve seguir lejos con más piedra,
no menos, sin dibujar nada nuevo.

**5. *"El grifo de agua solamente en la build de Unity aparece congelada el agua."*** Investigado
a fondo, **con la conclusión equivocada** — ver el playtest 17, donde aparece la causa real. Lo
que sí quedó bien medido y sigue siendo válido: el degradado frío del sótano nunca llegaba a la
boquilla.

**6. *"No olvides la parte de tener un cincel o algo así que permita editar el bedrock."***
`Game/Cincel.cs`, nuevo (558 líneas). Tecla **C** alterna entre frasco y cincel: es un MODO, no
otro botón — con el frasco desactivado mientras el cincel está activo, porque compartir los
mismos clics entre dos herramientas es la receta del error accidental. Botón izquierdo talla
piedra a vacío, el derecho rellena vacío con piedra (vía `PaintStable`, regla 22). Radio 2, 3
celdas por tick, alcance el del frasco (`Flask.ReachWorld`) y doble protección del borde del
mundo. Es la primera pieza real de la fase "taller movible".

> **NOTA DE PROCESO (importante):** esta sección y la del playtest 15 se escribieron *a
> posteriori*, en el playtest 17, al descubrir que ambas rondas se habían commiteado **sin pasar
> por el HANDOFF**. Cesar tiene una instrucción permanente — *"asegúrate de que se documenta
> todo"* — y se incumplió dos rondas seguidas. Es exactamente el mismo mecanismo que produjo la
> regresión del playtest 10-14: trabajo que ocurre y no queda escrito deja de existir para quien
> venga después. Reconstruido leyendo los diffs de `af94acb` y `bb0923b` y los docblocks que sí se
> escribieron en el código, que por suerte son extensos.

---

## Playtest 15 → EL TALLER DEJA DE SER UNA PANTALLA: 768x288, CÁMARA QUE SIGUE AL APRENDIZ,
## CLIMA POR ZONA (retirado después, ver playtest 17) — escrito a posteriori
Ronda dirigida por Opus 5; Sonnet 5 escribió el código. La ronda más grande en superficie tocada
desde la morfología: 1.346 líneas nuevas en 9 archivos, y toca el núcleo de la sim.

Es la ejecución de los pasos 1 y 2 del plan de fase acordado en el playtest 14, motivada por Cesar
con un requisito de CO-OP, no de estética: *"un laboratorio de 2-3 pantallas de ancho y 1,5-2 de
alto... suficiente para que dos personas puedan estar trabajando en cosas distintas sin verse
constantemente"*.

**1. EL MUNDO x6.** `CellGrid` pasa de 256x144 a **768x288** (3 pantallas de ancho x 2 de alto).
`PantallaW/PantallaH` se quedan valiendo 256x144 para poder seguir pensando el plano en unidades
de pantalla. Chunks: CHUNK=16, 48x18 chunks.

**2. LAS TRES PASADAS QUE ERAN PROPORCIONALES AL MUNDO ENTERO.** Multiplicar el grid por 6 no es
gratis: había tres recorridos cuyo coste escalaba con el tamaño total y no con lo que se ve —
refresco completo de textura, `MorphTick` y `DiffuseTemperature`. Las tres pasaron a ser
CONSCIENTES DEL VIEWPORT. Es el trabajo invisible de la ronda y el que evita que el mundo grande
cueste 6x en fotogramas.

**3. CUATRO ZONAS**, pedidas por Cesar: CULTIVO (x16..250, las cubas de Vivium) — LABORATORIO
(x262..505, banco de grifos + pila + bandeja fría + estante + el POZO) — ENTREGA (x517..767, la
Tolva) en la mitad de arriba (y=144..287); y el SÓTANO (x220..530, y=10..143) debajo, al que solo
se llega VOLANDO por el pozo, única conexión entre las dos mitades. `SimLevelBuilder` se reescribe
como plano completo y sigue siendo la única fuente de verdad de todas las coordenadas.

**4. CÁMARA QUE SIGUE AL APRENDIZ** (paso 1 del plan). Con el mundo a 3x2 pantallas deja de ser
opcional.

**5. CLIMA POR ZONA — RETIRADO EN EL PLAYTEST 17.** Se añadió `CellGrid.ambient` (un byte de
temperatura ambiente por celda) y `SimLevelBuilder.PaintClimate` pintaba un CULTIVO templado
(26°C) y un SÓTANO frío (4°C) con degradados de decenas de celdas. La idea: *"el espacio deja de
ser distancia y pasa a ser recurso"* — cristalizar en el sótano cuesta menos frío activo, cultivar
en cultivo menos calor. **Duró dos rondas.** Cesar lo mandó fuera en el playtest 17 porque
contradice la fase siguiente (si el taller es movible, el clima no puede estar atado a
coordenadas). El ARRAY se queda; lo que se fue es que el PLANO decida el clima. Razonamiento
completo en el docblock de `SimLevelBuilder` y en la sección del playtest 17 de arriba.

**Feedback de Cesar sobre esta ronda** (playtest 15, en su momento): *"Se recuperó la mayoría, los
caños están bien... los dos carteles negros al iniciar... el cartel de HELANDO se volvió a colocar
en la posición incorrecta... se volvió a congelar el agua del caño... yo esperaría que lo último en
llegar a temperatura normal sea la placa. También pienso que si vamos a hacer el taller editable
por el jugador convendría que las placas fueran mucho más pequeñas."* Los rótulos y las placas se
atendieron en el 14/16; el agua del grifo tardó hasta el 17.

---

## Playtest 14 → LA REGRESIÓN GRANDE Y SU RECUPERACIÓN A TRES BANDAS, LOS RECUADROS NEGROS
## EXPLICADOS, HELANDO VS FRESCA, PLACAS MÁS PEQUEÑAS, Y EL DIAGNÓSTICO DE FASE "FALTA
## MORFOLOGÍA" — TODO VALIDADO POR CESAR EN EL EDITOR
Ronda dirigida por Opus 5 (rastreo en git de la regresión perdida en tres rondas, especificación
de 2 encargos con propiedad de archivos disjunta y revisión); Sonnet 5 escribió el código en esos
2 encargos.

**1. LA REGRESIÓN GRANDE: se perdió el trabajo del playtest 7 y nadie lo notó en tres rondas.**
Cesar: *"perdimos un update en alguna build, los caños con los títulos apilados nuevamente
mostrando palabra 'grifo' en todos los títulos y sin brillar al acercarme."*
Rastreado en git con los tamaños exactos: en el commit **`e3fed6f` (playtest 10)** `Dispenser.cs`
pasó de **26.866 a 18.186 bytes**, y con él se perdió TODO el trabajo del playtest 7 sobre
máquinas: la chapa lateral por grifo, el halo de resalte del aparato enfocado, y el límite de 2
usos del prompt "E". Lo mismo en `ChillStone.cs` (16.950→9.676 bytes) y `HeatPlate.cs`
(16.858→11.211 bytes). **Y también `UiStyles.PlacaMundoLateral` y `UiStyles.Cercania`**
(`UiStyles.cs` cayó de 18.876 a 15.970 bytes), **y la sección "Playtest 7" entera de este mismo
HANDOFF** — no existe hoy ninguna sección con ese nombre; se perdió con lo demás.
CAUSA: el sandbox de trabajo en la nube se reinició a mitad de la ronda 10; los agentes editaron
una copia obsoleta y se desplegó encima de lo bueno.
POR QUÉ NADIE LO VIO EN TRES RONDAS (10, 11, 12): **el juego seguía compilando**, porque se
perdieron a la vez la API y todos sus consumidores. La única huella era
`MachineFocus.MostrarPromptE`, declarado y sin un solo llamante durante tres rondas.
RECUPERACIÓN: fusión a tres bandas — base = versión del commit `2ef67e5` (playtest 9, la última
buena), encima las decisiones posteriores que sí eran buenas (`Dispenser.ResolverNombre()` y los
nombres innominados de la regla 13/17, las guardas de atajos de la regla 12, y el estado Fresca de
`ChillStone` del playtest 13). NO fue un copy-paste del pasado. `UiStyles.PlacaMundoLateral`/
`Cercania` restauradas desde el historial con sus firmas originales, y los duplicados locales que
se habían creado como parche se eliminaron después.
**REGLA DE PROCESO NUEVA, la lección más importante de la ronda**: antes de desplegar hay que
hacer `git diff` contra el remoto y **desconfiar de TODO archivo que ENCOJA de tamaño**. Un
archivo que pierde miles de bytes sin que el cambio lo justifique es una regresión hasta que se
demuestre lo contrario. Que el proyecto compile NO es señal de que no se ha perdido nada.

**2. Las regresiones que produjo la propia fusión (y su arreglo).** Recuperar trabajo de tres
rondas atrás reintroduce decisiones antiguas que ya se habían corregido después. Salieron tres,
todas reportadas por Cesar y todas ya arregladas:
- **Recuadros negros vacíos flotando en el taller al arrancar**, y *"al alejarme del calor se
  muestra la etiqueta en negro antes de desaparecer"*. CAUSA REAL: `UiStyles.PlacaMundo` **nunca
  aplicaba el desvanecimiento al panel** — pintaba el fondo a la opacidad fija de `TintaFuerte`
  ignorando el alfa del llamante, y solo desvanecía el texto. Caso reproducido con las constantes
  reales: al arrancar, el aprendiz aparece a ~4,75 u de `HeatPlate_0` (dentro de
  `RangoEstadoDesvanece`=6.5 pero fuera de `RangoNombreDesvanece`=3.6), así que el anillo de
  NOMBRE pedía dibujar con alfa exactamente 0: texto invisible, panel opaco. Esos eran los dos
  recuadros. FIX general en `UiStyles` (afecta a `PlacaMundo`, `PlacaMundoLateral` y `Globo`): el
  panel se desvanece **al cubo** mientras el texto va lineal, así la caja siempre muere antes que
  la letra; y el umbral de no-dibujar subió de 0,02 a **0,12** (`AlfaMinimaVisible`), que es donde
  el texto deja de leerse.
- **El rótulo del frío volvió a la posición incorrecta.** La versión del playtest 7 anclaba los
  rótulos de `ChillStone` al labio de la bandeja con desplazamiento HACIA ARRIBA; en el playtest
  13 Cesar validó explícitamente la posición HACIA ABAJO, coherente con `HeatPlate`. La fusión lo
  invirtió. FIX: `ChillStone` usa ahora el mismo punto (`_centroBloque`, análogo a
  `HeatPlate._centroChasis`) y el mismo signo negativo (`-S(17f)`/`-S(34f)`). Medición: el offset
  mayor lleva el rótulo a la fila ~82,7, dentro del hueco de 35 filas de aire (y=53..88), con
  ~29,7 filas de margen. **La cronología completa quedó escrita en el header del archivo**
  (playtest 7 → validado en el 13 → invertido por la restauración → corregido), para que no gire
  una tercera vez.
- Deja escrito el patrón general: **al recuperar trabajo antiguo, revisar qué decisiones de ese
  trabajo fueron corregidas después** — una fusión a tres bandas puede deshacer correcciones
  validadas.

**3. Ajustes pedidos y aplicados.**
- **Helando no servía para nada que Fresca no hiciera** (medido: Fresca ya cruza el umbral de
  congelación del agua y el de cristalización con margen en cualquier seed). Ahora Helando
  conserva su destino de −80 °C pero empuja **12 raw/tick contra los 5 de Fresca**: es el modo "lo
  necesito YA", a cambio de sobreenfriar y tardar más en volver. El rótulo lo insinúa:
  `HELANDO -80° · más rápido`.
- **Se quitó el texto `(alcanza N filas)`**, que no comunicaba. En su lugar la influencia térmica
  DECAE con la distancia en vez de cortarse en seco. Perfil final `FilaEmpujePct = {100, 45, 15}`
  (3 filas), tras un primer intento de `{100,60,35,20,10}` (5 filas) que seguía llegando demasiado
  lejos: −29% de energía inyectada. Idéntico en los dos aparatos, la simetría entre ellos es un
  principio del proyecto. Distancias medidas al grifo de agua: ChillStone ≈40,5 celdas euclídeas,
  HeatPlate ≈73,2.
- **Orden de recuperación térmica** (petición de Cesar: *"lo último en llegar a temperatura normal
  debería ser la placa"*): al apagarse, la fila ADYACENTE al aparato sigue empujada débilmente
  (`HoldStepRaw=1`) hacia el último objetivo durante `HoldTicksTrasApagar=60` ticks (2 s), mientras
  las filas exteriores se sueltan de inmediato. La fuente es ahora lo último en normalizarse.
  Determinista, sin asignaciones, y sin tocar `SimStepper.DiffuseTemperature` (regla 9).
- **Placas mucho más pequeñas**, de cara al taller movible: ambos aparatos recortan el ancho que
  reciben del bootstrap a una fracción centrada del 40% (`FootprintFraction`), aplicada ANTES de
  `BuildVisual` para que sprite y zona de efecto queden coherentes. HeatPlate 52→21 celdas,
  ChillStone 46→18. El centro no se mueve, así que foco y rótulos no requieren ajuste.
  **Propuesta anotada en el código**: que `AlkahestGameBootstrap` pase en el futuro posición +
  ancho pequeño en vez del interior completo, para poder colocar el aparato en cualquier punto del
  fondo.

**4. LA CONVERSACIÓN DE DIRECCIÓN — esto es lo más valioso de la ronda.** Cesar reportó que el
juego **se siente atascado**: cuatro grifos y uno bloqueado, hay que tirar cosas en otros sitios
para seguir, no ve cómo progresar a combinaciones complejas, solo descubre dos o tres reacciones,
y *"al cambiar de semilla solo cambio la textura de una de ellas pero no su comportamiento ni su
aplicación"*. Y lo más agudo: *"me hace más falta morfología, porque si no las texturas solo me
inducen a poner nombres como 'rojo bonito' o 'amarillo brillante'; quizás si cambia la forma
entonces sí podría tener otras palabras en mente"*.
DIAGNÓSTICO ACORDADO, tesis de la fase siguiente: **se ha construido una capa de presentación muy
rica sobre una capa de sistemas muy delgada.** Trece rondas mejorando cómo el juego SE LEE
(patrones, audio, diario, rótulos, firmas) mientras el número de cosas que se pueden HACER apenas
ha crecido: los mismos cuatro grifos, las mismas dos o tres reacciones alcanzables, el mismo
taller clavado, los mismos tres días. Una semilla nueva solo cambia la piel porque **la piel es lo
único que se genera**: la química es fija y escrita a mano. Y por eso los nombres salen "amarillo
brillante": el color es lo único que distingue una sustancia de otra, así que es lo único que se
puede decir de ella. **El nombre sale del comportamiento, no de la paleta.**
PROPUESTAS DE CESAR, aceptadas: (a) **taller editable por el jugador** — mover grifos, estantes,
placas y botellas con el botón central del ratón, anclados a bedrock, con el grifo orientándose
según el lado por el que se ancla; (b) **mundo persistente con semilla y progreso guardado**,
estilo Minecraft, *"donde sienta que el conocimiento de un universo me suma"*; (c) **un taller de
2-3 pantallas de ancho y 1,5-2 de alto**, con su boceto de zonas: cultivo — laboratorio principal —
entrega, y un sótano. Su razón, que es la correcta y hay que dejarla escrita: *"suficiente para que
dos personas puedan estar trabajando en cosas distintas sin verse constantemente"* — **eso no es
estética, es el requisito del co-op**; un taller de una pantalla donde los dos ven lo mismo no es
cooperativo.

**5. RESPUESTA TÉCNICA: ¿está la simulación desacoplada del viewport?** Pregunta literal de
Cesar. Respuesta documentada, porque es la base del plan de fase.
**Sí, y la pieza difícil ya está hecha**: el sistema de chunks con sueño (M1) ya procesa solo lo
activo, no la grilla entera — es exactamente el mecanismo "de crecer sin simular un tablero
monstruoso" que él describía.
Lo que SÍ está acoplado, tres cosas acotadas y ninguna arquitectónica:
1. `SimRenderer.FitMainCamera()` enmarca el mundo entero a propósito. Una cámara que siga al
   jugador es un cambio pequeño ahí.
2. Tres pasadas cuestan proporcional al MUNDO y no a lo VISIBLE: el refresco completo cada 30
   frames, `MorphTick` (un cuarto de todas las celdas por tick) y `DiffuseTemperature` (toda la
   grilla). Con 768x288 —3 pantallas x 2, unas **221.184 celdas, 6x las 36.864 actuales**— dejan de
   ser gratis. Solución: hacerlas conscientes del viewport, igual que ya lo son de los chunks. NO
   es refactorización.
3. `SimLevelBuilder` tiene las coordenadas escritas para una pantalla; eso no es refactorizar, es
   rediseñar el taller, y vive en un solo archivo.
Lo que NO es problema: una textura de 768x288 son 221k téxeles (trivial para GPU) y los arrays por
campo pasan de 36k a 221k bytes.

**PENDIENTE tras esta ronda** (ver Backlog arriba para el detalle completo): 1) cámara que sigue
al aprendiz; 2) el taller a 2-3 pantallas (rediseño de `SimLevelBuilder` + zonas + tres pasadas
conscientes del viewport); 3) química generada por semilla con núcleo fijo + reacciones sorteadas
+ "leyes descubiertas: N de M" en el diario; 4) comportamiento (no solo aspecto) variable por
semilla; 5) taller movible; 6) mundo persistente con semilla y progreso; más el backlog heredado
(consolidar `FirmaVisualFabrica`, enganchar `HintSystem.PistasMostradas`, decidir el audio,
renombrar el repo, medir `MorphTick`, resto de M5, build de Windows, multiplayer).
Preguntas abiertas para el próximo playtest: ¿la cámara siguiendo al aprendiz se siente bien sin
romper la lectura del taller actual? ¿el núcleo fijo de química es suficiente para que los
encargos generados por semilla sigan siendo siempre resolubles? ¿"leyes descubiertas: N de M"
comunica que hay más por encontrar sin abrumar?


## Playtest 13 → SAFE MODE, EL HIELO QUE SE FUNDÍA SOLO, HUMO-COMO-BORRADOR ES CORRECTO,
## PATRONES SIN SOBRECARGAR MATERIA, FIRMA VISUAL GENERADA EN REDOMAS Y FRASCO, Y LA
## ASIMETRÍA DEL FRÍO — SIN VERIFICAR EN EDITOR
Ronda dirigida por Opus 5 (diagnóstico de los seis hallazgos de Cesar, especificación de 4
encargos con propiedad de archivos disjunta, revisión de compilación); Sonnet 5 escribió el
código en esos 4 encargos, más 1 pase de revisión.

**0. Nota de arranque: tras Safe Mode, regenerar la escena.** Al salir de Safe Mode el juego
arrancaba sin mostrar nada. Causa: los componentes de la escena quedan sin script asignado al
recompilar en Safe Mode. Se arregló con **Alkahest → 1. Generar escena Lab** (idempotente,
`AlkahestSceneBuilder`), que reengancha todo. **Este es ahora el primer reflejo documentado tras
cualquier Safe Mode.** También se corrigieron dos errores de compilación reales: `CS1503`
(`int`→`uint` en el argumento 4 de `XorShift.FromCell`, líneas 1360 y 1443 de `SimStepper.cs`).
De las 14 llamadas a `FromCell` en ese archivo, 12 pasan una constante literal (`77`, `205`...),
que C# convierte implícitamente a `uint`; esas dos pasan `201 + def.semillaPatron` y
`209 + def.semillaPatron` — `semillaPatron` es un `byte` que al sumarse a una constante se
promueve a `int`, y de `int` a `uint` no hay conversión implícita. Fix: cast explícito
`(uint)(...)`. **Regla general dejada por escrito: al pasar `salt` a `XorShift.FromCell`, si la
expresión mezcla una constante con un campo hay que castear a `uint`.**

**1. BUG: pintar hielo con la paleta producía agua.** `CellGrid.SetCell` (la que usa
`AlkahestSim.Paint`) nunca toca `temp`, así que la celda pintada hereda la temperatura que
hubiera antes ahí — en la práctica siempre `CellGrid.AmbientRaw = 70` (20 °C), porque el grid
arranca entero a ambiente. Y `Universe.Create` sortea `Ice.meltsAt = CToRaw(waterFreezeC + 5)`
con `waterFreezeC` acotado a `[-20, 15]` por seed, así que `meltsAt` cae SIEMPRE en raw
`[52, 70]`. Como la ambiente es exactamente el extremo superior de ese rango, la condición de
fusión de `SimStepper.ApplyPhase` (`t >= meltsAt`) era cierta en cualquier seed, siempre: el
hielo pintado se fundía a agua en el primerísimo tick, sin excepción.
FIX GENERAL, no un parche del hielo: `AlkahestSim.StableBirthTempRaw(MaterialDef)` calcula una
temperatura de nacimiento en la que el material sea ESTABLE, con las mismas comparaciones que
`ApplyPhase`: si ambiente cruzaría una cota superior activa (`meltsAt`/`boilsAt`), nace en
`umbral − 10 raw`; si cruzaría una inferior (`freezesAt`/`condensesAt`), nace en
`umbral + 10 raw`; si ambiente ya cae dentro de la banda (el caso normal — Agua, Cristal...), se
deja ambiente sin tocar. Expuesto en un método NUEVO, `AlkahestSim.PaintStable(x, y, radius,
materialId)`, que solo usa `DevPalette`. `Paint`/`PaintCell`/`PaintRect` NO cambiaron de firma
ni de comportamiento (comprobado con grep que sus únicos llamantes son Flask, MasterSupplies,
DeliveryChute, Dispenser y DevPalette), y `Flask` sigue restituyendo la temperatura media de lo
aspirado, validado desde el playtest 4.

**2. El humo era un borrador — y es correcto que lo sea.** Cesar: *"el tirar humo es como si
fuera un borrador... quizás porque no es un material a manipular sino una consecuencia"*. Tenía
razón: Humo/Vapor/Fuego/Ceniza son SUBPRODUCTOS (nacen de una reacción y se disipan solos), y el
Humo en concreto es un gas de vida corta — no es un bug de la sim, el fallo era que la paleta no
distinguía insumo de subproducto. `DevPalette` agrupa ahora en dos bloques con cabecera
("Insumos (los manipulas tú)" / "Subproductos (nacen de una reacción y se disipan solos)"),
tiñe los botones de subproducto, y al seleccionar uno muestra una línea fija explicando qué lo
produce y cómo se disipa. **No se quitó ningún material: pintar humo sigue siendo útil para
depurar.** `DevPalette.SubproductoIds` es una lista de DISEÑO (fija a mano), no derivada de
`MaterialArchetype`, porque el arquetipo no distingue "consecuencia" de "insumo".

**3. Los patrones necesitaban demasiada materia — sobrecorrección del playtest 12 + un bug
real.** Cesar: *"se necesita mucho material para poder apreciar los patrones... voy a terminar
preparando mucho más de lo que necesito solo para apreciar bien el patrón y poder
documentarlo"*.
- **Sobrecorrección del playtest 12**: por miedo a que a 7,5 px/celda una frecuencia alta se
  leyera como ruido, se había remapeado `patronEscala` a periodos de 14..35 celdas (Vetas) y
  teselas de 18..46 (Celdas). Medición de los recipientes reales (`SimLevelBuilder`): cuba
  interior 52x37 (`VatWidth=58`/`VatHeight=40` menos `WallThickness=3` por lado), bandeja fría
  interior 46x6 (`ChillTrayInteriorX0..X1` = 39..84, `ChillTrayWidth=52`). Con rasgos de 35
  celdas se ve UNA sola repetición en el recipiente más estrecho. **Principio de diseño dejado
  por escrito: un patrón se reconoce por su REPETICIÓN, no por su tamaño.** Nuevo remapeo:
  ambas familias a `4 + patronEscala` = **5..12 celdas** (`SimRenderer.cs`, `veinScale`/
  `cellSize`), que da entre 3,8 y 9,2 repeticiones en el recipiente más estrecho.
- Manchas/Laberinto/Dendritas viven en `SimStepper` (no se tocaron): su tamaño de rasgo emerge
  de `feed`/`diffDiv` (4..18 celdas) y de la longitud de rama de Dendritas (~14..23), ya del
  orden adecuado.
- Suelo de `patronFuerza` de Powder subido de 40 a **55** en `Universe.Create` (es el arquetipo
  con `patronEscala` más pequeño: rasgo diminuto + contraste mínimo era la combinación con más
  riesgo de caer bajo el umbral de percepción — a 40 el swing máximo era ±20/255, ~8%, apenas
  por encima de ruido de compresión de pantalla; a 55 sube a ±27/255).
- **BUG REAL confirmado**: Cesar reportó *"el rosa es transparente porque se logran divisar un
  poquito los patrones de los ladrillos atrás, el verde no"*. `SimRenderer` estaba limpio
  (devuelve `baseColor.a` sin tocar salvo Fuego, y el borde `Difuso` solo modula RGB, cumple la
  regla 19). El bug estaba en `Universe.SortearFirmasVisuales`: preservaba
  `alphaOriginal = m.baseColor.a` del roster, y los tres líquidos innominados nacen con alfa
  215-235 (Azoth 215, Acid 220, Slime 235), así que **toda la masa** era semitransparente, no
  solo el contorno — el mismo mosaico duro contra `WorkshopBackdrop` que la regla 19 advierte
  para el borde, pero aplicado a la sustancia entera. El verde no lo era por ser sólido (alfa
  255). FIX: lo innominado fuerza alfa 255 siempre. **Regla dejada por escrito: lo innominado
  nace OPACO; el alfa <255 del roster es para el vocabulario del taller, no para la firma
  sorteada.**

**4. Documentar sin producir de más — la firma generada.** Cesar: *"al llenar las botellas
estos patrones no se notan ni se animan sus contenidos, lo que lo hace más dependiente del
nombre. Una nueva dificultad a tener en cuenta."* Principio: no hace falta acumular materia
para ver una firma, porque una muestra se puede GENERAR — no es una foto del mundo, es la firma
del material dibujada a la escala que convenga; con eso, cinco celdas bastan para documentar.
- Nueva clase `internal static FirmaVisualFabrica` (declarada al final de `StorageRack.cs`,
  namespace `Alkahest.Game`), con `GenerarPixeles(w, h, def, frameIdx, maskAlpha, esBordeMask,
  sobreMundo)`. Replica la técnica de `JournalHud.CrearMiniatura` adaptada para aceptar una
  máscara de alfa y una máscara de borde, así sirve tanto a la silueta de una redoma como a un
  cuadradito de HUD.
- `StorageRack`: el contenido de cada redoma muestra el patrón y el borde del material, con la
  silueta de la botella tomada de `MaquinariaSprites.ContenidoRedoma()` (verificado que su
  textura se crea con `Apply(false, false)` y por tanto es LEGIBLE desde código — con
  `makeNoLongerReadable: true` habría petado en runtime). **Y se anima**: hasta
  `FirmaVisualFabrica.AnimFrames` fotogramas pregenerados cuyo desfase imita el `drift` de
  `SimRenderer`; en `Update` solo se intercambia el sprite cacheado cuando cambia el índice
  (`Time.time * FirmaVisualFabrica.AnimFps`). `ritmoAnim == 0` → un solo fotograma → quieto de
  verdad.
- `FlaskHud`: cada fila del panel y el chip de material bloqueado junto al cursor muestran la
  firma en el mismo rectángulo que antes era color plano — **sin tocar la maqueta**, que ya tuvo
  quejas de apiñamiento.
- Regla 19 respetada con un flag `sobreMundo`: en el mundo (redomas) el borde Difuso oscurece
  hacia `SimRenderer.BackgroundColor`; en un panel opaco de UI sí puede bajar alfa.
- **DEUDA TÉCNICA ANOTADA**: `FirmaVisualFabrica` duplica las siete funciones de patrón y los
  hashes de `JournalHud` (privados y en un archivo que no era editable en esta ronda). Debe
  consolidarse en un único `Game/FirmaVisualFabrica.cs` compartido en una ronda futura. Añadido
  al backlog.

**5. El frío: la asimetría era real, pero no donde parecía.** Cesar: *"la placa fría parece
irradiar más fuerte el frío que el calor, y tardar más en recuperar su temperatura, además de
tener más alcance"*.
Tabla medida (`ChillStone.cs`/`HeatPlate.cs`, comentarios de cabecera): `RowsAffected = 3` en
AMBOS; `TempStepPerTick = 5` en AMBOS; área — bandeja interior 46 celdas de ancho vs cuba 52 —
**la placa cubre más área absoluta**. O sea, no había asimetría de alcance ni de velocidad.
La asimetría real: **la piedra gélida solo tenía UN modo y era el más extremo** (raw 20 = −80 °C,
50 unidades / 100 °C por debajo de ambiente), mientras `HeatPlate` siempre tuvo un modo moderado
(Templada, calibrada al centro de la banda de crecimiento del Vivium, típicamente raw ~82, solo
12 unidades / 24 °C por encima) además del extremo Ardiente (raw 220, +320 °C, 150 unidades). Y
como el tirón hacia ambiente de `SimStepper.DiffuseTemperature` es un paso FIJO (±1 raw cada
~32 ticks, ~1,07 s), no proporcional a la distancia, el tiempo de retorno es lineal en la
distancia empujada: **~53 s desde −80 °C contra ~13 s desde Templada** (y ~160 s desde Ardiente,
150 unidades). Para dimensionarlo: el propio juego define "frío" como ≤−5 °C en el encargo Cold
del día 2 — −80 °C es 16× más frío que lo que el juego pide, y el punto de congelación del agua
de esta seed nunca baja de −20 °C.
FIX: `ChillStone` gana un estado intermedio **Fresca** (ciclo Off→Fresca→Helando→Off con E,
igual patrón que `HeatPlate`), calibrado por seed en `Init()` al mínimo entre
`Universe.Get(MaterialId.Water).freezesAt` y `Universe.CrystallizeMaxTempRaw`, menos
`FrescaMarginRaw = 10` raw de margen frente al tirón de `DiffuseTemperature` (típico ~raw 50,
−20 °C). Sigue congelando y cristalizando sin dejar la zona helada un minuto. Renombrado
`ColdRaw` → `HelandoRaw` (se conserva intacto, raw 20, para cuando el jugador SÍ quiere el
resultado instantáneo garantizado).
NO se tocó `SimStepper.DiffuseTemperature` (regla 9 de `CLAUDE.md`), aunque su tirón fijo es la
causa técnica: queda REPORTADO como observación, no corregido.
**Y un hallazgo de diseño importante**: las hornillas NO pueden calentar la bandeja fría,
geométricamente. La bandeja vive en y=88..96 y las cubas terminan en su labio en y=53
(`VatInteriorY1`): hay **35 filas de aire vacío** entre medias, y `Empty` no participa en la
difusión de temperatura (`docs/SIM_NOTES.md`, "Límites conocidos"). Ninguna placa puede influir
allí salvo que fuego o gas vuelen físicamente hasta la bandeja. No es un bug; es información de
diseño que conviene tener presente al balancear.
Añadido menor en ambos aparatos: cuando están encendidos y el aprendiz cerca, el rótulo añade
`(alcanza N filas)` — mismo número (3) en los dos, para que el jugador pueda comprobar por sí
mismo que el alcance es igual. No es un elemento permanente (regla 15).

**PENDIENTE tras esta ronda** (ver Backlog arriba para el detalle completo): (a) verificar en
Unity y juzgar si los patrones se leen ya con poca materia; (b) comprobar si la piedra en modo
Fresca se siente manejable; (c) consolidar `FirmaVisualFabrica` con el generador de `JournalHud`
(deuda técnica del punto 4); (d) enganchar `HintSystem.PistasMostradas` en la sección
PROCEDIMIENTOS del diario; (e) decidir si el audio se queda (`DirectorDeAudio.SistemaActivo`);
(f) CURVA DE PROGRESIÓN — jornadas cortas de una mecánica cada una; (g) renombrar `Alkahest` →
`ChaosAlchemy` (GitHub + `productName`); (h) replantear las redomas; (i) resto de M5; (j) nueva
build de Windows con la checklist del playtest 11; (k) multiplayer, con el formato de deltas
contemplando `morph`.
Preguntas abiertas para el próximo playtest: ¿los patrones se leen ya en un charco de trabajo
normal, sin preparar de más? ¿la firma en redomas y frasco quita la necesidad de producir de más
para documentar? ¿el modo Fresca es el que se usará a diario, o sigue haciendo falta Helando
como opción rápida?


## Playtest 10 → LA FANTASÍA DE BAUTIZAR RECUPERADA, EL DIARIO ES UN LIBRO,
## BLOQUEO DE MATERIAL + HAZ + ANILLO DEL FRASCO, y barrido de atajos — SIN VERIFICAR EN EDITOR
Ronda dirigida por Opus 5 (diagnóstico de los hallazgos de Cesar, especificación de 4 encargos
con propiedad de archivos disjunta, revisión de compilación); Sonnet 5 escribió TODO el código.

**1. LA FANTASÍA DE BAUTIZAR, RECUPERADA (el hallazgo de diseño de la ronda).** Cesar: *"No
entendí por qué etiquetar de vivium algo que el juego ya le llama así. ¿Por qué le pondría otro
nombre? Se perdió algo ahí de la idea inicial."* Tenía razón: el juego presentaba todo ya
clasificado, así que bautizar era un trámite vacío. `SubstanceKnowledge` reparte ahora el roster
en dos clases (tabla escrita en el comentario de la clase):
- **VOCABULARIO DEL TALLER** (nombre desde el día 1, `NombreComun` no devuelve null): Stone,
  Sand, Water, Oil, Nutrient (grifos del banco) y los fenómenos mundanos Steam, Smoke, Fire, Ash,
  Ice. Criterio: nadie bautiza el agua.
- **LO INNOMINADO** (`NombreComun` devuelve null -> "???" hasta bautizar): Azoth (sale de un
  grifo, pero es la reserva que "tampoco sabe qué es"), CrystalSeed, Crystal (Azoth+semilla en
  frío), Vivium, Slime (solo nace de Ácido+Agua) y Acid (sin grifo en esta build).
Los encargos (`OrderSystem`) describen ahora por EFECTO/ORIGEN mientras el material objetivo siga
sin nombre, y pasan a usar el nombre del jugador en cuanto se bautiza: `DescribirGrows` ("...de lo
que crece solo, sin que lo alimenten a mano" → "...de lo que llamáis \"musgo hambriento\"") y
`DescribirCrystalSolid` ("la piedra que crece en la bandeja fría", deliberadamente sin mencionar
Azoth, también innominado, para no repetir la circularidad). El recálculo lo dispara
`SubstanceKnowledge.NamingVersion` en `Update()`: como `Order.Descripcion` es readonly (Game/
Order.cs), `RefreshDescripciones()` sustituye la instancia `Order` entera cuando cambia la
versión, nunca construye strings en `OnGUI`.
`Dispenser` y `MasterSupplies` se corrigieron en el mismo barrido para no contradecir la
reclasificación: `Dispenser.ResolverNombre()` resuelve el nombre vía `SubstanceKnowledge`
(`FindAnyObjectByType` en `Init` — no se pudo cambiar la firma de `Init` sin tocar
`AlkahestGameBootstrap`, fuera de alcance) en vez de caer en el `devName` interno en inglés; antes
el grifo de Azoth decía literalmente "Azoth" para siempre. `MasterSupplies.TextoEntrega` (jornada
2, 352 caracteres, antes 330) describe las tres semillas por ORIGEN ("el líquido del grifo alto",
"el retoño de la cuba", "la semilla de la bandeja fría"), abriendo con *"El Maestro os deja tres
semillas SIN NOMBRE: ni él sabe qué son, y espera que vosotros les pongáis uno."* El panel de
`DayCycle.DrawDayIntro` (vía `AbrirPanel`) se subió de 490 a 510 px de alto para la jornada 2 como
margen de seguridad (usa `GUILayout.FlexibleSpace()`, así que 490 ya sobraba, pero sin confirmar
jugando).

**2. EL DIARIO ES UN LIBRO.** Cesar: *"la presentación gráfica es muy pobre, además de incómodo
de leer. No siento que sea tan necesario que vea todo el resto de la pantalla mientras quiero leer
mi libro"* y, sobre las pistas, *"las indicaciones son súper largas... no sentí que tenía que
descubrirlo como algo divertido sino que tengo que descubrirlo porque no tomé una captura de
pantalla"*. `JournalHud` reescrito (866 líneas): velo a pantalla completa (alfa 0.86) + libro de
dos páginas de pergamino apagado (marrón 0.30/0.24/0.18, NO blanco puro) con lomo, márgenes por
`UiStyles.S()`, tres niveles tipográficos y aire generoso entre entradas. `GUI.depth = -1000` lo
pone delante de todo (en IMGUI gana el depth MÁS BAJO). Tres secciones con pestañas clicables:
**LEYES** (con `★ SE PROPAGA` destacado), **SUSTANCIAS**, y la nueva **PROCEDIMIENTOS** (recetas
paso a paso sintetizadas desde `Universe.Reactions` + la ley de crecimiento del Vivium, con el
paso que siempre se olvida explícito: "★ SE PROPAGA — el X no se gasta, repite el paso 2").
Paginación real con "página N de M" y botones "anterior"/"siguiente" (se evitan los glifos
◀/▶ y ⟳, sin uso previo comprobado en la fuente IMGUI real del proyecto — solo ·/—/★). Abre y
cierra con **J**, y también cierra con **ESC**. Expone `public static bool Abierto`.
Caché: una sola firma (`CountDiscovered()*1000003 + NamingVersion`) reconstruye las tres listas de
`Entrada`; la paginación (`ComputePages`/`FillColumn`) se mide cada `OnGUI` sobre los strings ya
cacheados, con `UiStyles.Alto` (word-wrap real, sin asignaciones).
**Pistas de `HintSystem` recortadas a una línea ejecutable cada una** (jornada 1: 12 líneas;
jornada 2: 9, con los procedimientos de cristal y vivium partidos en pasos sueltos; jornada 3: 6).
La duración de la tanda sale ahora de `pistas × segundos-por-pista` (antes al revés: una duración
fija repartida entre las pistas que hubiera). El detalle largo vive en el diario, no en el aviso
flotante.
**PENDIENTE ANOTADO EN EL CÓDIGO**: `HintSystem` expone ya `public static IReadOnlyList<string>
PistasMostradas`, pensada para que `JournalHud` archivase literalmente las pistas ya vistas, pero
**`JournalHud` todavía NO la consume** — ambos archivos se escribieron en paralelo en la misma
ronda y `JournalHud` no pudo depender de una API que `HintSystem` aún no tenía. `JournalHud`
sintetiza en su lugar la sección PROCEDIMIENTOS desde `Universe.Reactions` (misma fuente que
LEYES). Enganchar `PistasMostradas` en el diario queda para la próxima ronda.

**3. EL FRASCO: BLOQUEO DE MATERIAL, HAZ Y ANILLO DE ALCANCE.** Dos reportes que eran el mismo
problema. *"El tener el cursor y el personaje como dos opciones para controlar el movimiento se
siente antinatural"* y *"al absorber materiales a veces te llevas unas pocas unidades de otro...
¿cómo lo manejarías tú?"*.
- **Bloqueo de material** (`Flask.BloquearMaterialBajoElCursor`): al PULSAR aspirar se muestrea el
  material bajo el cursor y queda fijado para toda la pulsación; solo entran al frasco celdas de
  ESE material (`TickSuck`). Si bajo el cursor no hay nada aspirable, busca en anillos crecientes
  (mismo recorrido que `TickSuck`) el aspirable más cercano dentro del alcance; si no hay ninguno,
  no bloquea ni aspira. Filtro centralizado en `Flask.EsAspirable` (no Empty, no Stone, no
  arquetipo Fire) para que nunca diverja del que ya usaba `TickSuck`. **Shift**
  (`leftShiftKey.isPressed`, comprobado cada frame, no solo al pulsar) aspira todo
  indiscriminadamente: el comportamiento viejo sigue disponible para limpiar destrozos.
  Motivo escrito en el código de por qué NO se hizo el zoom que pedía el jugador: pelearía con
  `SimRenderer.FitMainCamera` (recuadrar la cámara fue un bug recurrente en los playtests 5 y 6),
  obligaría a navegar además de a apuntar, y no resuelve el problema de fondo — que la herramienta
  no discrimina.
- **El haz** (`UpdateWorldVisuals`): línea (`SpriteRenderer` 1x1 estirado y rotado, cero assets)
  del `CarryAnchor` al cursor mientras se aspira o vierte, coloreada por el material bloqueado o el
  dominante del frasco (`ColorDelHaz`), con un punto de luz (`_beamPulseSr`) recorriéndola en la
  dirección del flujo. Se CORTA en el borde de `ReachWorld` y cambia a `BeamColorAviso` (tono
  rojizo) si el cursor está fuera de alcance.
- **El anillo de alcance** (`BuildRingVisual`/`UpdateWorldVisuals`): aro tenue alrededor del
  aprendiz con el radio real de `ReachWorld`, alfa 0.05 en reposo (`RingRestAlpha`) y encendiéndose
  (hasta `RingMaxAlpha=0.60`) por proximidad del cursor al borde.
- `sortingOrder`: anillo 20, haz 40/41 — entre el sim (-5) y el aprendiz (cola -3, ala trasera -2,
  ala delantera -1, cuerpo 0, base 50; ver playtest 9).
- `FlaskHud` muestra un chip discreto junto al cursor con el material bloqueado, solo mientras se
  aspira, y "todo (Mayús.)" si Shift lo anula.

**4. BARRIDO DE ATAJOS: `UiStyles.EscribiendoTexto` y `JournalHud.Abierto`.** Cesar: *"Al escribir
una etiqueta, si usas la tecla M interfiere con el sonido (muteándolo). No deberían pisarse."* Un
campo de texto IMGUI se come TODAS las letras, así que el bug existía para cada atajo de una
tecla: la "h" ocultaba las pistas y la "t" cerraba el propio diálogo de bautizar a media palabra.
Infraestructura nueva: `UiStyles.EscribiendoTexto` (propiedad estática que además sigue
devolviendo true durante UN FRAME EXTRA tras cerrarse, para que el atajo tampoco se dispare en el
mismo frame en que se confirma con Enter; la levanta y la baja `NamingUi`, incluido el cierre
forzado por `DayCycle.InputLocked`) y `JournalHud.Abierto`.
**REGLA DEL PROYECTO (ver también CLAUDE.md): todo atajo de una sola tecla debe comprobar
`UiStyles.EscribiendoTexto`, sin excepción. Los atajos del MUNDO deben comprobar además
`JournalHud.Abierto`; los del propio libro y el silenciar del audio (preferencia del jugador) no.**
Tabla completa de atajos (material de referencia):
| Tecla | Sistema | Archivo | Guardas |
|---|---|---|---|
| M silenciar | Audio | `Audio/DirectorDeAudio` | solo `EscribiendoTexto` (a propósito: preferencia del jugador) |
| F3 paleta / P pausa / N step / clics de pintar | Dev | `Dev/DevPalette` | ambas |
| E interactuar | Máquinas | `Game/HeatPlate`, `Game/ChillStone`, `Game/Dispenser` | `InputLocked` + ambas |
| WASD y flechas | Movimiento | `Game/ApprenticeController` | ambas (la velocidad decae con naturalidad, no se congela en seco) |
| ENTER cierre anticipado de jornada | Jornada | `Game/DayCycle` | `EscribiendoTexto` |
| Clics en redomas | Estantería | `Game/StorageRack` | `InputLocked` + `DevPalette.IsOpen` + ambas |
| T bautizar, ESC | Bautizar | `Game/NamingUi` | ESC deliberadamente SIN la guarda (es universal) |
| J / ESC / Re Pág / Av Pág | Diario | `Game/JournalHud` | — (es el propio libro) |
| H pistas | Pistas | `Game/HintSystem` | `EscribiendoTexto` |
| Q vaciar, clics de aspirar/verter, Shift | Frasco | `Game/Flask` | todas las guardas |
El pase de revisión cazó DOS huecos más del mismo tipo, buen ejemplo de por qué hace falta el
pase: (a) la T abría el diálogo de bautizar DETRÁS del libro (que fuerza `GUI.depth=-1000`) y le
robaba el foco de teclado — el jugador podía escribir en un campo invisible; (b) `StorageRack`
comprobaba `JournalHud.Abierto` pero no `EscribiendoTexto`, así que se podía seguir haciendo clic
en las redomas mientras se escribía un nombre (el mismo conflicto, colado por un clic de ratón en
vez de por una letra). Ambos corregidos en esta ronda.
`ApprenticeController` **NO comprueba `DayCycle.InputLocked`** — hueco preexistente, anotado en el
código, NO corregido en esta ronda (fuera del alcance de los 4 encargos).

**5. Menor: el rótulo del frío.** Cesar: *"La etiqueta HELANDO y los grados aparece por encima de
la plataforma y no por debajo, como en las placas de fuego."* Al investigarlo, `ChillStone` YA
usaba el mismo signo (negativo = abajo) y la misma fórmula de anclaje (`_centroBloque`) que
`HeatPlate` (`_centroChasis`). Medición dejada en comentario en `ChillStone.cs`: el bloque gélido
ocupa las filas 88-90 (`ChillTrayY0=88` + `WallThickness`), la bandeja está ENCIMA (interior filas
91-96), y bajo el bloque hay 48 celdas libres bajo la meseta del banco (filas 40-87, en x=62,
`BenchX0..BenchX1=1..64`, techo `BenchTopY=39`) y 34 en el punto más estrecho bajo el muro de la
Cuba A (filas 54-87, `VatAX0=72`, `VatInteriorY1=53`), contra un desplazamiento de 3.4-6.8 celdas
(`S(17f)`/`S(34f)`). NO se cambiaron números — el análisis dice que el anclaje ya era correcto.
**QUEDA PENDIENTE DE CONFIRMAR con una captura nueva**: si el rótulo sigue leyéndose mal, el punto
de anclaje del bloque no es el que se supone y hay que revisarlo de nuevo con otros ojos.

**6. Validado por el jugador (no tocar).** *"El momento de la ley descubierta sí se sintió muy
bien."* — el momento LEY DESCUBIERTA del playtest 9 funciona; también validó la lectura de grados,
la navegación de pistas con flechas y el ocultar con H.

**PENDIENTE tras esta ronda** (ver Backlog arriba para el detalle completo: verificar en Unity que
compila y jugar las 3 jornadas; confirmar con captura el rótulo del frío; enganchar
`HintSystem.PistasMostradas` en PROCEDIMIENTOS del diario; decidir el destino del audio; build
Windows limpia; renombrar repo; replantear redomas; resto de M5; multiplayer).
Preguntas abiertas para el próximo playtest: ¿el bloqueo de material resuelve la precisión sin
necesidad de zoom? ¿el haz y el anillo hacen que cursor y personaje se sientan una sola
herramienta, o hay que probar que el imp se mueva hacia el cursor? ¿el diario se lee ya cómodo?
¿bautizar se siente ahora como un descubrimiento?


## Playtest 12 → LA FIRMA MORFOLÓGICA: cada sustancia tiende a un patrón, no solo a un número —
## LA RONDA MÁS ARQUITECTÓNICA HASTA AHORA, TOCA EL NÚCLEO DETERMINISTA — SIN VERIFICAR EN EDITOR
Ronda dirigida por Opus 5 (arquitectura del campo morfológico, el contrato compartido entre los
tres archivos de `Sim/`, las tres garantías del sorteo y las reglas de diseño); Sonnet 5 escribió
el código en 5 encargos paralelos con propiedad de archivos disjunta, más 1 pase de revisión.

**0. Contexto y decisión de orden.** Cesar terminó las tres jornadas por primera vez, empezó otro
universo y dijo: *"al final, al escoger otro universo, solo tuve más de lo mismo"*. Hasta ahora la
variación por seed era SOLO NUMÉRICA (probabilidades, bandas de temperatura, Edictos): dos partidas
se veían idénticas porque la materia se veía idéntica. Él mismo propuso la solución, y era la
correcta — es la TESIS del sistema: *"la morfología puede ser una propiedad del material, no una
forma rígida"* y *"no necesitas que al aspirarlo conserve exactamente el dibujo píxel por píxel;
necesitas que cuando vuelva a existir, vuelva a TENDER a formar ese tipo de patrón"*.
Preguntó si hacerlo antes o después del multiplayer. Se decidió ANTES, por dos razones: (a) de
diseño, el co-op multiplica una experiencia, y si la experiencia se agota en una partida el co-op
multiplica el agotamiento; (b) TÉCNICA Y DECISIVA — el plan de netcode es sim solo-host + deltas
RLE por chunks despiertos, y esta ronda añade un campo nuevo POR CELDA (`CellGrid.morph`).
Añadirlo después del multiplayer habría obligado a rehacer el formato de deltas.

**1. LA ARQUITECTURA: el patrón no se guarda, se REGENERA.**
- `Sim/MaterialDef.cs`: dos enums nuevos — `PatronMorfologico` (`Liso, Vetas, Manchas, Laberinto,
  Celdas, Dendritas, Pulso, Motas`) y `BordeMorfologico` (`Neto, Halo, Escarcha, Difuso`) — y la
  FIRMA VISUAL como campos: `patron`, `borde`, `patronEscala` (1..8, tamaño del rasgo),
  `patronFuerza` (contraste, útil ~40..150), `ritmoAnim` (0 = quieto), `emision` (luz propia,
  distinta de `emitsGlow`), `semillaPatron` (desplaza los hashes para que dos materiales de la
  misma familia no se calquen).
- `Sim/CellGrid.cs`: campos nuevos `byte[] morph` (estado morfológico por celda) y `byte[]
  morphScratch` (doble búfer, imprescindible para las familias que leen vecinos: leer y escribir el
  mismo array daría un resultado dependiente del orden de recorrido y rompería el determinismo del
  que depende el netcode). `SetCell` SIEMBRA `morph` con un hash barato de (idx, material) — no lo
  pone a cero, porque un campo plano tarda mucho en romper la simetría y se vería un instante de
  materia lisa. `SwapCells` intercambia `morph` con la materia, para que un líquido que fluye
  arrastre su dibujo en vez de dejarlo clavado a las coordenadas del mundo.
- **EL CONTRATO** que respetan los tres archivos: `morph` es una intensidad 0..255 que el renderer
  convierte en desplazamiento de brillo escalado por `patronFuerza`. `Liso` no lo usa. **`Vetas` y
  `Celdas` son PURAMENTE POSICIONALES: el stepper NO las toca, las calcula el renderer con hashes**
  (ahorro grande — coste cero en `MorphTick`). `Manchas`/`Laberinto` = concentración de
  reacción-difusión. `Dendritas` = fuerza de rama. `Pulso` = fase. `Motas` = intensidad de chispa.

**2. `Universe.Create(seed)` — el sorteo con garantías.** Tabla de familias plausibles por
arquetipo: StaticSolid (Crystal) → Liso/Vetas/Celdas/Dendritas (nunca late); Powder (CrystalSeed) →
Liso/Vetas/Manchas/Motas (sin laberintos ni teselas, eso pide medio continuo); Liquid
(Azoth/Slime/Acid) → todo menos Dendritas; Organic (Vivium) → Dendritas/Celdas/Manchas/
Laberinto/Pulso/Motas (nunca Liso/Vetas, que es lo mineral e inerte). Hay tabla equivalente de
bordes plausibles (Escarcha solo mineral/granular, Difuso nunca en sólido).
TRES GARANTÍAS implementadas y verificadas, con sus números:
- **Separación de tono**: tono ancla por seed + reparto a intervalos de 360/6 = 60° con jitter ±12°
  → separación angular mínima garantizada de **36°** entre cualquier par de las 6 sustancias.
- **Diversidad de familias**: orden barajado + cada material prefiere una familia aún no usada
  dentro de su lista plausible + refuerzo explícito de que al menos una quede `Liso` o `Vetas`.
  Razón de diseño: **si todo late y burbujea la pantalla se vuelve ruido y nada destaca; la
  quietud es contraste.**
- **Legibilidad**: luminancia perceptual `L = 0.2126R + 0.7152G + 0.0722B`, mínimo `L >= 0.40` (la
  pared del taller está en L≈0.127 y la piedra en L≈0.345). BUG REAL ENCONTRADO Y CORREGIDO durante
  el desarrollo: subir solo V no basta para azules/violetas saturados (en H≈240° R y G son casi
  cero incluso con V=1, y se quedaba en L≈0.31, visto con seed 42/Crystal); el fallback es
  desaturar hasta S=0.15.
- **REGLA DE DISEÑO CENTRAL**: solo varían LO INNOMINADO (Azoth, CrystalSeed, Crystal, Vivium,
  Slime, Acid). El VOCABULARIO DEL TALLER (Water, Sand, Oil, Nutrient, Stone, Fire, Smoke, Ash,
  Steam, Ice) se ve SIEMPRE igual — es el suelo firme desde el que el jugador juzga lo demás; si
  todo cambia, nada se reconoce. Se retiró además el jitter de tono global de ±8° que antes se
  aplicaba a todos (`docs/SIM_NOTES.md` de M1 lo mencionaba; ya no aplica, ver ahí).
- API nueva: `Universe.CaracterDelUniverso` (frase corta cacheada, sin nombrar sustancias — respeta
  la regla 13 de circularidad) y `Universe.DescribirFirma(byte matId)` (cacheada en array privado,
  nunca construye por frame).

**3. `SimStepper.MorphTick` — la evolución.**
- Estriado 1/4 por tick con `offset = tick%4`. **VERIFICACIÓN ARITMÉTICA** (ver regla 9 de
  CLAUDE.md): la congruencia módulo 4 se cumple exactamente una vez cada 4 ticks para cualquier
  celda; a diferencia de los dos bugs de `DiffuseTemperature` del playtest 9, aquí el offset es la
  ÚNICA guarda, no hay una segunda condición temporal combinada con él.
- Doble búfer: `Array.Copy(morph→morphScratch)` al inicio, todas las familias escriben SOLO en
  `morphScratch`, `Array.Copy` de vuelta al final. Dendritas escribe en un vecino con `max()`, que
  es conmutativo y por tanto independiente del orden.
- Chunks dormidos: SÍ se respetan pero a 1/8 de frecuencia (ronda de 32 ticks ≈ 1,07 s). Razón:
  congelarlos del todo deja un charco a medio patrón para siempre; no respetarlos anula el ahorro.
- Parámetros reales: Manchas/Laberinto comparten `feed = 8 + (fuerza>>4)` (8..23) y se separan por
  el delta kill−feed en bandas que nunca se solapan (Laberinto delta 2..6 → bandas; Manchas delta
  16..23 → puntos que colapsan). Dendritas: semilla 1/(600+(8−escala)·300), 70% de sesgo al eje
  `semillaPatron&3`, decae 10+escala por paso. Pulso: función PURA de `(tick·velocidad +
  distanciaManhattan·5) mod 256`, autocorrectiva si la celda se mueve. Motas: chispa
  1/(2500−escala·200) a 220-255, decae con el TIEMPO a 40+(fuerza>>2) por turno.

**4. Morfología de CRECIMIENTO (lo que más se nota: es silueta, no textura).**
- Cristalización, tres modos derivados de `patron` del Crystal: **dendrítico** (sesgo fuerte a un
  eje), **compacto** (elige el Azoth candidato más rodeado de cristal), **laminar** (sesgo a un eje
  completo). Hallazgo: solo hay UNA reacción de cristalización que elige vecino de verdad —
  `CrystalSeed` (Powder) alcanzando un Azoth adyacente; `Crystal` es `StaticSolid` y nunca ejecuta
  sus propias reacciones, así que vista desde el Azoth es autoconversión sin vecino que elegir.
- Vivium, tres modos: **enredadera** (sigue la dirección de la que vino, guardada en bits libres de
  `aux`: `0x01/0x02` dirección y `0x04` flag, sin colisionar con `0x80` asentado ni `0x40`
  OrganicDormantAux), **mata** (isótropo, lo de antes), **disperso** (prefiere el candidato con
  menos vecinos de Vivium, deja huecos).
- **LAS TASAS NO CAMBIAN**: solo cambia QUÉ VECINO se elige, nunca cuántos. Verificado:
  `TryReactNeighbor` tira el dado con `XorShift.FromCell(tick,x,y,77)` sobre las coordenadas
  propias, así que reordenar los vecinos cambia cuál se convierte, jamás si se convierte. Igual con
  el 60% del Vivium y su throttle `&3`.

**5. `SimRenderer` — hacerlo visible.** Técnica por familia: Vetas = bandas senoidales (tabla de
seno de 256 entradas construida una vez) deformadas por `LatticeNoise` (rejilla de hash con
interpolación bilineal entera), con `patronEscala` remapeada a periodos de 14..35 celdas (nunca
literal, o a 7,5 px/celda sería ruido). Celdas = Voronoi de 9 puntos jitterados, remapeado a
teselas de 18..46 celdas. Manchas/Laberinto/Pulso/Dendritas/Motas leen `morph`. Dendritas solo
ILUMINA (nunca oscurece) para leerse como aguja y no como sombra. Motas es aditivo puro hacia
blanco.
Bordes: Neto (no-op), Halo (+34 fijo, independiente de `patronFuerza`: el borde es silueta, no
patrón), Escarcha (1/3 de las celdas de contorno +70, hash estable sin tiempo). **Difuso: se
DESCARTÓ bajar el alfa** — el sprite del sim es 1 téxel/celda en Point y detrás hay otra textura
Point a triple resolución, así que un téxel semitransparente produce un mosaico duro del fondo
asomando en bloques de ~7,5 px, que se lee como bug de recorte. Se resolvió oscureciendo hacia
`BackgroundColor` en la mitad de las celdas de contorno. **Es una trampa que reaparecerá — no
volver a intentar bajar el alfa del borde.**
PROBLEMA DE CHUNKS DORMIDOS Y PATRONES ANIMADOS, con su solución: solo Vetas y Celdas son un
problema real (no usan `morph`, se recalculan puras de `tick`, así que si el chunk no se redibuja
se CONGELAN). Las demás ya avanzan al ritmo throttleado del stepper. Solución: `_chunkContinuousAnim[]`,
un `bool[]` por chunk que `RenderChunk` marca si alguna celda es Vetas/Celdas con `ritmoAnim>0`, y
`RenderFrame` exime a esos chunks del sueño SOLO para el redibujado (la física sigue dormida). No se
subió `FullRefreshEveryFrames` porque habría encarecido toda la grilla por un puñado de sustancias.
Todo lo validado sigue intacto: ruta de color del FUEGO, shimmer y línea de superficie de los
líquidos, sillería + iluminación de canto de StaticSolid, canto superior de los polvos, tinte de
temperatura, guard de CHUNK con el pragma, `FitMainCamera` con `Mathf.Max`, `_lastAspect`,
`FilterMode.Point`.

**6. El diario como catálogo del universo.** `JournalHud`: la sección SUSTANCIAS pasa a fichas de
catálogo con una MINIATURA REAL generada por código (30x30, `FilterMode.Point`, cacheada por
material en `_miniaturas`, construida una sola vez en el primer dibujado, `Apply(false, true)`),
que reproduce el patrón y el borde de la sustancia. Más el nombre (o `???`), la firma escrita vía
`Universe.DescribirFirma`, y las observaciones. El carácter del universo y la seed van en la
cabecera del libro. `OnDestroy` destruye las miniaturas: `DayCycle.RestartRun` recarga la escena
entera, así que sin eso se acumularían texturas huérfanas partida tras partida.
`SubstanceKnowledge` NO se modificó (la comprobación de "sin bautizar" se compone con la API
existente).

**7. BUG: la tecla T de bautizar.** Reporte: *"me parece que la T estuvo bloqueada hasta que quité
las pistas con la H... igual luego no pude activarla en otro frasco"*. TRES hallazgos:
- **La H no tenía nada que ver** — H y T no comparten estado mutable; H solo LEE
  `UiStyles.EscribiendoTexto`. Fue percepción. Teoría plausible del porqué: el panel de pistas vive
  arriba en el centro, justo donde suele estar el cursor mientras se lee, y esa zona cae sobre aire
  en coordenadas de mundo — o sea, estaba apuntando a nada. **Era percepción; nadie debe "arreglar"
  algo que no existe.**
- **CAUSA RAÍZ**: `NamingUi.Open()` hacía `return` EN SILENCIO cuando `ResolveTarget()` devolvía
  `Empty`. Indistinguible de "la tecla está bloqueada".
- **"otro frasco" = las redomas del estante**: solo hay un `Flask` (el del aprendiz); las redomas
  no viven en la grilla de la sim (son atrezzo + un conteo privado), así que el muestreo bajo el
  cursor siempre las ve `Empty` y T no podía resolver objetivo ahí.
- **Bug adicional**: apuntar a vocabulario del taller SÍ abría la ventana — se podía bautizar el
  agua, contra la regla 13.
FIX: `Open()` → `TryOpen()`, que SIEMPRE responde vía `Flask.Avisar` (mismo canal que
`StorageRack`, globo junto al cursor), distinguiendo los tres casos que para el jugador son
distintos: no apuntas a nada / eso está en la estantería / eso ya se llama X y el vocabulario del
taller no se bautiza. **Nota para futuros llamantes: `NamingUi.Open()` ya no existe, es
`TryOpen()`.**

**8. Otros.** `DayCycle` muestra `CaracterDelUniverso` + seed en la intro de la jornada 1 (NO en la
pantalla de Título, porque ahí el Universe cargado puede ser de usar y tirar si el campo de seed
queda vacío: prometería un mundo que luego no se juega), y en la pantalla final una línea aclarando
que "Nuevo universo" sortea otro carácter. Verificado que `UpdateAllOrdersDoneEarlyClose` sigue
vivo (regla 11).
Confirmado también que el commit 12 se subió UNA sola vez pese a la duda de Cesar (una segunda
ejecución del script habría dado "nothing to commit" y "everything up to date", sin daño).

**PENDIENTE tras esta ronda** (ver Backlog arriba para el detalle completo): (a) verificar en Unity
y jugar DOS universos seguidos para juzgar si la variación se percibe; (b) revisar que ningún
patrón quede ilegible o molesto a 7,5 px/celda; (c) medir el coste real de `MorphTick` (estimado
<0,5 ms/tick sobre un presupuesto de 33 ms, sin verificar en Unity); (d) enganchar
`HintSystem.PistasMostradas` en la sección PROCEDIMIENTOS del diario; (e) decidir si el audio se
queda (`DirectorDeAudio.SistemaActivo`); (f) CURVA DE PROGRESIÓN; (g) renombrar `Alkahest` →
`ChaosAlchemy` (GitHub + `productName`); (h) replantear las redomas; (i) resto de M5; (j)
multiplayer — con una nota nueva: el formato de deltas debe contemplar el campo `morph`.
Preguntas abiertas para el próximo playtest: ¿se percibe de verdad que el segundo universo es otro
sitio? ¿alguna familia queda ilegible a la escala de celda? ¿el sesgo dendrítico del cristal se lee
como agujas o como ruido?


## Playtest 11 → FUERA EL ANILLO, PRE-VUELO DE LA BUILD DE WINDOWS, y la dificultad como deuda
## de diseño reconocida — SIN VERIFICAR EN EDITOR NI CON EL .exe
Ronda dirigida por Opus 5 (diagnóstico del feedback de Cesar, especificación de 2 encargos con
propiedad de archivos disjunta, revisión); Sonnet 5 escribió TODO el código en esos 2 encargos.

**1. Validado por el jugador (no tocar).** Cesar, tras probar la ronda 10: *"El punto de luz del
color de material está increíble, gracias."* · *"Lo del bloqueo del material me encantó, quedó
increíble."* · *"El rótulo de frío quedó muy bien."* Este último **cierra el pendiente** que quedaba
abierto del playtest 10 §5 (confirmar por captura el anclaje del rótulo de `ChillStone`) —
RESUELTO, no hacía falta tocar el código. El momento LEY DESCUBIERTA sigue validado desde el
playtest 9.

**2. FUERA EL ANILLO DE ALCANCE.** Cesar: *"El anillo de alcance está feo, quítalo. Lo dejamos con
este único cambio para continuar pruebas."* El haz y el bloqueo de material (mismo trío del
playtest 10 §3) se quedan intactos — solo se pidió quitar el anillo. Extirpación quirúrgica en
`Game/Flask.cs`: eliminados `BuildRingVisual()`, `CrearSpriteAnillo()`, los campos `_ringSr`/
`_ringAlpha`, las constantes `Ring*` (radio, alfas de reposo/máximo) y los campos de color
`BrassLight`/`BrassShadow` (solo los usaba el degradado del anillo; `BrassBase` se conserva porque
el haz sí lo usa como tono neutro cuando no hay material concreto que teñirlo). Se conservan
intactos el haz, el punto de luz que lo recorre (`_beamPulseSr`), el bloqueo de material y
`ReachWorld`. El límite de alcance se comunica ahora SOLO con el corte del haz en el borde (y el
aviso "demasiado lejos" que ya existía). Queda un párrafo en la cabecera de `Flask` explicando que
el anillo existió y por qué se retiró, para que nadie lo reimplemente pensando que es una idea
nueva. **Práctica de proyecto que se deja registrada aquí y en `CLAUDE.md`: documentar en el propio
código las ideas que se probaron y se descartaron, no solo las que se quedaron** — el coste de un
párrafo es mucho menor que el de que alguien, rondas después, vuelva a implementar algo que ya se
probó y no gustó.

**3. PRE-VUELO DE LA BUILD DE WINDOWS.** Nunca se había verificado una build desde el rediseño del
taller (playtest 4); se arrastraban cinco rondas de cambios sin comprobar el `.exe`. Precedente que
motiva la cautela: el `Shader.Find` del playtest 2, que costó un playtest entero de confusión
("todo un poco roto") porque solo se manifestaba fuera del editor.
**HALLAZGO PRINCIPAL**: `AlkahestBuildTools.BuildDemoWindows()` solo comprobaba que
`AlkahestLab.unity` **existiera** en disco, nunca que estuviera al día con el código. Y la escena
guardada en el repo todavía tenía **la cámara de antes del rediseño del espacio**: posición
(19.2, 10.8) y tamaño ortográfico 10.8 — valores hardcodeados de la grilla vieja 384x216, cuando la
actual 256x144 exige centro (12.8, 7.2) y tamaño 7.2. Esto no llegó a manifestarse en ningún
playtest porque `SimRenderer.FitMainCamera()` reajusta la cámara en cada `Start()` (fix del
playtest 5): es decir, **llevábamos rondas enteras salvados por un parche defensivo**, sin saber
que la escena guardada estaba desfasada. Lección que queda escrita: un parche defensivo en runtime
puede ocultar durante rondas que el estado horneado (la escena) lleva tiempo desactualizado —
verificar el fuente, no solo el síntoma.
**FIX** en `Editor/AlkahestBuildTools.cs::BuildDemoWindows()`: guarda los cambios pendientes de
forma segura (`EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`, cancela la build si el
usuario dice que no), **REGENERA la escena** llamando a `AlkahestSceneBuilder.GenerateLabScene()`
antes de compilar (que ya deriva la cámara de `CellGrid.W/H` — el mismo mecanismo del playtest 4,
ver `Editor/AlkahestSceneBuilder.cs::BuildMainCamera()`), aborta con `Debug.LogError` claro si la
regeneración falla o si el `.unity` sigue sin existir después, envuelve
`BuildPipeline.BuildPlayer` en try/catch (una excepción de configuración ya no se cuela como traza
cruda), e imprime un resumen (resultado, errores, avisos, tamaño, tiempo, ruta) tanto en consola
como en un `EditorUtility.DisplayDialog`. Opciones de build:
`BuildOptions.Development | BuildOptions.ShowBuiltPlayer` **para esta primera verificación** —
deja `Player.log` escrito y mantiene F3 (`DevPalette`) activa para inspeccionar la sim en vivo sin
recompilar; **quitar `BuildOptions.Development` para la build de reparto** (cambio de una línea en
`AlkahestBuildTools.cs`), o F3 llega al jugador final.
**VERIFICADO Y CORRECTO por lectura de código (sin cambios) — se documenta la lista aquí porque es
la checklist de build reutilizable para cualquier ronda futura:**
- Cero `Shader.Find`/`Resources.Load`/`new Material`/fuentes dinámicas en runtime — todo el render
  es `SpriteRenderer` + `Sprite.Create` sobre texturas generadas por código (regla del playtest 2,
  ver `CLAUDE.md`).
- Cero `UnityEditor` fuera de `Editor/` y cero `#if UNITY_EDITOR` en el resto del proyecto.
- `Alkahest.Runtime.asmdef` no referencia `Alkahest.Editor.asmdef`; `Audio/` no tiene asmdef propio
  y queda cubierta por el Runtime (solo hay 2 asmdefs en `Assets/Alkahest/`).
- `AlkahestLab.unity` ya estaba en el índice 0 de `EditorBuildSettings.scenes`
  (`AlkahestSceneBuilder.UpdateBuildSettings`).
- Todos los sistemas de juego se generan por código en `AlkahestGameBootstrap.TrySpawn()`, así que
  lo único horneado necesario en la escena es Cámara + un GameObject con `SimRenderer`+
  `AlkahestSim`+`DevPalette`+`AlkahestGameBootstrap` (`AlkahestSceneBuilder.BuildAlkahestObject`).
- `AudioListener` presente en la Main Camera (`AlkahestSceneBuilder.BuildMainCamera`) y
  `Audio/DirectorDeAudio.EnsureListener()` lo añade defensivamente si faltara.
- `Dev/DevPalette` gatea con `Application.isEditor || Debug.isDebugBuild` en `Awake`/`Update`/
  `OnGUI` (`IsDevBuild()`): inerte en release, activa en Development.
- `apiCompatibilityLevel: 6`, sin override de `managedStrippingLevel`, sin `link.xml` y sin
  necesidad de él — cero reflexión en todo `Assets/Alkahest/`.
**RIESGOS ANOTADOS, no corregidos en esta ronda:**
(a) `productName` sigue siendo `Alkahest`, así que el `Player.log` de la build vive en
`%USERPROFILE%\AppData\LocalLow\FriendsLoop\Alkahest\Player.log` — va junto con el renombrado del
repo pendiente en el backlog.
(b) `FriendsLoop` (Steamworks/Netcode) se compila en el player aunque su escena no se cargue, y no
hay `steam_api64.dll` en el repo. No se encontró ningún `RuntimeInitializeOnLoadMethod` que pudiera
auto-arrancar Steamworks al iniciar, pero no se pudo descartar al 100% sin compilar y ejecutar la
build de verdad.
**CHECKLIST PARA VALIDAR EL `.exe` (reutilizable en cada build futura, es el paso 2 del backlog):**
que la materia se vea al abrir un grifo (el bug del `Shader.Find` del playtest 2); que el fondo del
taller aparezca; que las máquinas se vean y respondan a E; que suene el audio y M lo silencie; que
el diario se abra con J y cierre con J/ESC; que la simulación corra a velocidad normal (F3 da
ms/tick y chunks activos); y una partida corta entregando algo en la Tolva para ver progresar un
encargo.

**4. DIFICULTAD: no hay curva, y es una deuda de diseño reconocida.** Cesar: *"La progresión de
dificultad es muy alta pero imagino que es intencional porque es testeo y luego podremos hacer
niveles más pequeños para que quede clara la mecánica. Para mí está 'bien' porque permite que no
sean tan lentas mis pruebas, ¿esa es la intención?"*
Respuesta honesta que queda escrita aquí: **en parte sí y en parte no.** Los umbrales del día 3
(playtest 8) se derivaron midiendo tasas reales (cristalizar ~2,2 s/celda, ver la tabla de balance
de esa sección) para ocupar el 60-70% de la jornada **de alguien que ya conoce el bucle** — está
calibrado para la velocidad de prueba del propio Cesar, y en ese sentido sí es intencional. Pero
**no existe una curva de dificultad**: tres jornadas de seis minutos con todo el sistema desplegado
desde el día 1 es un vertical slice pensado para PROBAR el juego, no un onboarding pensado para
APRENDERLO. Un jugador nuevo se come el taller entero de golpe: grifos, frasco, calor, frío,
semillas que se propagan, Edictos, todo el día 1. Hace falta una ronda propia de **PROGRESIÓN**:
jornadas cortas que introduzcan una mecánica cada una (grifos y frasco → calor → frío → las
semillas que se propagan). Esto es diseño de nivel, no un ajuste de números de balance — se añade
al backlog como punto propio y bien visible (ver arriba, punto 6).

**5. Nota de infraestructura (para el que retome el proyecto).** El sandbox de trabajo en la nube
se reinició a mitad de sesión y **perdió la copia de trabajo entera**, revirtiéndola a un snapshot
anterior al playtest 8 (`Audio/DirectorDeAudio.cs` llegó a quedar en 0 bytes). Se recuperó
**clonando el repo desde GitHub**, porque Cesar había subido el commit del playtest 10 (commit 11
en `CrafterPunk/Alkahest`). Lección operativa, registrada también en `CLAUDE.md`: **el repo de
GitHub es la única fuente de verdad fiable; la copia de trabajo del sandbox es volátil.** Ante
cualquier duda sobre el estado del código, comparar contra un clon fresco antes de editar, y no
acumular varias rondas sin commit.

**PENDIENTE tras esta ronda** (ver Backlog arriba para el detalle completo: ejecutar la build de
Windows y validarla con la checklist; enganchar `HintSystem.PistasMostradas` en PROCEDIMIENTOS del
diario; decidir el destino del audio; curva de progresión con jornadas cortas; renombrar repo;
replantear redomas; resto de M5; multiplayer).
Preguntas abiertas para el próximo playtest: ¿el `.exe` pasa la checklist completa? ¿se nota la
falta del anillo o el haz solo ya comunica bien el alcance? ¿por dónde empezar la curva de
progresión — separar el día 1 en varias jornadas más cortas, o introducir mecánicas de forma
gradual dentro del mismo día 1?


## Historial de modelos (para el informe final al usuario)
- **Fable** (orquestador): visión y DECISIONS.md, arquitectura de la sim y del loop, specs de los
  4 agentes, fixes puntuales (regla del fuego, APIs 6.5, color de llama, shimmer), todo el
  Computer Use (pruebas en editor, git, GitHub), template FriendsLoop previo completo.
- **Sonnet** (implementación): ~90% del C# — M1 sim core, M2 interacción, M3 leyes/reacciones
  (parcial, interrumpido), M4 loop completo; investigación del stack Steam (sesión template).
- **Opus**: no participó aún (la revisión visual M5 era su tarea natural — sigue siéndolo).
- **Playtest 8**: **Opus 5 dirigió** (diagnóstico de la regresión de los sólidos, decisiones de
  balance y de dirección de sonido y de arte, especificación de los 4 encargos y revisión);
  **Sonnet 5 escribió todo el código** en 4 encargos paralelos con propiedad de archivos disjunta,
  más 1 pase de revisión de compilación.
- **Playtest 9**: mismo reparto — **Opus 5 dirigió** (diagnóstico de los siete hallazgos de Cesar,
  especificación de 4 encargos paralelos con propiedad de archivos disjunta y revisión);
  **Sonnet 5 escribió todo el código** en esos 4 encargos, más 1 pase de revisión de compilación.
- **Playtest 10**: mismo reparto — **Opus 5 dirigió** (diagnóstico de los hallazgos de Cesar,
  especificación de 4 encargos paralelos con propiedad de archivos disjunta y revisión);
  **Sonnet 5 escribió todo el código** en esos 4 encargos, más 1 pase de revisión de compilación
  que encontró 2 defectos reales de integración (T detrás del libro, `StorageRack` sin la guarda
  `EscribiendoTexto`).
- **Playtest 11**: mismo reparto — **Opus 5 dirigió** (diagnóstico del feedback de Cesar,
  especificación de 2 encargos con propiedad de archivos disjunta y revisión); **Sonnet 5 escribió
  el código** en esos 2 encargos (extirpación del anillo de alcance en `Flask.cs`, y pre-vuelo de
  la build de Windows en `AlkahestBuildTools.cs`).
- **Playtest 12**: mismo reparto, ronda más arquitectónica hasta ahora (toca el núcleo
  determinista) — **Opus 5 dirigió** (arquitectura del campo morfológico, el contrato compartido
  entre los tres archivos de `Sim/`, las tres garantías del sorteo de firma visual y las reglas de
  diseño); **Sonnet 5 escribió el código** en 5 encargos paralelos con propiedad de archivos
  disjunta, más 1 pase de revisión de compilación.
- **Playtest 13**: mismo reparto — **Opus 5 dirigió** (diagnóstico de los seis hallazgos de
  Cesar, incluida la nota de arranque tras Safe Mode y los dos `CS1503` de compilación,
  especificación de 4 encargos paralelos con propiedad de archivos disjunta y revisión); **Sonnet
  5 escribió el código** en esos 4 encargos, más 1 pase de revisión de compilación.
- **Playtest 14**: mismo reparto, ronda de recuperación — **Opus 5 dirigió** (rastreo en git de la
  regresión del playtest 7 perdida durante tres rondas, diagnóstico de las regresiones producidas
  por la propia fusión de recuperación, especificación de 2 encargos con propiedad de archivos
  disjunta y revisión, y la conversación de dirección con Cesar sobre la fase siguiente); **Sonnet
  5 escribió el código** en esos 2 encargos. Todo VALIDADO por Cesar en el editor.

## Playtest 1 del usuario (post-M4) y fixes aplicados
Hallazgos de Cesar jugando: (1) el fuego "no parecía fuego": moría a humo gris en ~1.5 s y no
prendía nada (solo Oil era inflamable); (2) la piedra fría seguía enfriando tras apagarse y con
radio enorme (causa real: el HIELO inyectaba frío a sus vecinos → zona fría autosostenida);
(3) onboarding confuso — no logró completar pedidos en su primera ronda (esperado: no hay
tutorial; pendiente M5/M6).
Fixes en commit siguiente: hielo ya no inyecta frío; retorno a temperatura ambiente 4x más
rápido; ignición por contacto 30%→50%; la llama se mantiene viva mientras toque combustible;
vida del fuego 45→80 ticks; Nutrient y Vivium ahora son inflamables (arden bonito, y el cultivo
puede incendiarse — riesgo/recompensa). VALIDADO por el usuario: "el fuego funciona bien, también se arregló lo del frío" (commit del playtest pusheado).
Siguiente en el backlog tras validar: onboarding suave (pistas contextuales en la Jornada 1,
p.ej. "prueba a verter aceite en la tolva" cuando el pedido inflamable lleva 2 min sin progreso).


## Stint M5-parcial (onboarding + fondo + build) — último trabajo de Fable
Añadido en este commit: `Game/HintSystem.cs` (7 pistas rotatorias abajo-centro durante los
primeros ~2.5 min de juego real — responde al "no entendí cómo jugar"), `Game/WorkshopBackdrop.cs`
(fondo del taller: gradiente ciruela + viñeta + grano, quad opaco detrás de la sim, cero assets),
`Editor/AlkahestBuildTools.cs` (menú "Alkahest/2. Build demo Windows" → Builds/ChaosAlchemyDemo/
ChaosAlchemy.exe, y "3. Abrir carpeta de builds"). Wiring en AlkahestGameBootstrap.
SIN VERIFICAR en editor (escrito a ciegas por economía de créditos): riesgo bajo, pero el primer
Play puede revelar detalles (z-orden del fondo vs sim, estilo de las pistas). Si el fondo tapara
la sim: bajar su z de 0.5 a 2 o comprobar que el quad de la sim es transparente-sobre-opaco.
M5 restante para Opus: glow aditivo fuego/Vivium, agua con metaballs/post-blur, sprite del
aprendiz expresivo, SFX, y la revisión visual de conjunto (su especialidad). Después: balance de
la partida completa y el plan multiplayer (sección Backlog).


## Playtest 2 (build .exe) — causa raíz y fixes
Hallazgos de Cesar probando la BUILD: materia/fondo invisibles ("todo un poco roto, los caños no
funcionaban"), aunque en el editor sí se veía; la paleta F3 (que arrancaba ABIERTA) mezclaba su
pincel con el frasco ("aspirar borra todo" = pincel Empty; "tiro arena al inicio"); verter "no
hacía nada" (frasco vacío/lejos, sin feedback); la Tolva no se entiende.
CAUSA RAÍZ del build roto: los quads de sim y fondo usaban Shader.Find("URP/Unlit") en runtime —
Unity ELIMINA de la build los shaders no referenciados por assets. En editor existe, en el .exe
no. FIX: ambos son ahora SpriteRenderer (el shader de sprites sí se incluye — REGLA para el
futuro: nada de Shader.Find en runtime salvo shaders garantizados; usar sprites o materiales-asset).
Más fixes: DevPalette arranca CERRADA con estado persistente (PlayerPrefs) y expone IsOpen — el
Frasco ignora clics con la paleta abierta; feedback en HUD del frasco ("frasco vacío", "demasiado
lejos"); Tolva dorada brillante con etiqueta "vierte AQUÍ tus entregas".
Diseño confirmado al usuario: el loop es grifo→charco→ASPIRAR→VERTER en cubas (para experimentar)
o en la Tolva (para entregar). Los grifos vierten al suelo/cuba que tengan debajo, no a "tarritos".
PENDIENTE (Opus, M5): claridad visual de la Tolva (animación/flujo de succión al entregar),
glow, agua con cuerpo, sprite del aprendiz, revisión de conjunto. Verificar build de nuevo tras
estos fixes (los sprites deberían verse ya en el .exe).


## Playtest 3 → pase visual/UX de Opus (M5) — SIN VERIFICAR EN EDITOR
Nuevo `Game/UiStyles.cs`: paleta + GUIStyles cacheados + primitivas (Panel/Barra/Globo/
EtiquetaMundo) que usan TODOS los HUD; el HUD escala con la resolución (`UiStyles.S()`, base 720p).
Cambios: DevPalette ya no pinta con la ventana cerrada (era el "tira arena como loco" y el
"aspirar rompe el mundo"); FlaskHud pasa a barra compacta ARRIBA-IZQUIERDA (la zona baja queda
libre) con retícula y avisos junto al cursor; OrdersHud mide el texto (se acabó el truncado a 34
caracteres); HintSystem sube a arriba-centro bajo el reloj (H lo oculta); la Tolva es ahora un
nicho excavado en un contrafuerte del muro derecho — geometría en `SimLevelBuilder.Chute*`
(boca en celdas x 369-379, y 60-91; sustituye al bolsillo x 376-380 y 60-80) — con marco dorado
pulsante, flecha y destello al tragar; los grifos rebosan hacia arriba en vez de atascarse.
DOS BUGS DE DISEÑO ARREGLADOS DE PASO: el frasco no aspiraba sólidos estáticos y la Tolva los
ignoraba, así que los encargos de Cristal/"algo helado" eran IMPOSIBLES (ahora solo se excluye la
piedra; el fuego tampoco se aspira, con aviso). Paleta: Stone más oscuro, Ash pardo (no se
confunde con humo), Crystal violeta-cian (no se confunde con hielo).

## Pase visual/UX de Opus (playtest 3)
Opus 5 hizo el pase completo (18 archivos): UiStyles.cs (estilo unificado, escala por resolución
base 720p), fix definitivo del pincel fantasma del F3 (pintaba con la paleta CERRADA — esa era
la causa de "tira arena" y "aspirar rompe el mundo"), frasco como barra compacta arriba-izquierda
(la esquina de juego queda libre), feedback como globo junto al cursor + retícula por colores,
encargos sin truncar con barra de meta (Favor 120), pistas arriba-centro medidas (H las oculta),
Tolva = nicho excavado en el muro derecho con jambas doradas, flecha y "¡ENTREGA ACEPTADA!",
grifos con rebose (buscan superficie hasta 8 celdas), rótulos en español, paleta de colores
retocada (Stone oscuro, Ash pardo, Crystal violeta-cian). BUGS DE DISEÑO detectados por Opus:
frasco y Tolva rechazaban sólidos estáticos → los encargos de Cristal y de frío eran IMPOSIBLES;
ahora solo la piedra es inaspirable y el fuego avisa "te quemaría el frasco". Queda para otro
pase: agua con cuerpo (metaballs), sprite del aprendiz, SFX. Verificar en editor: lista completa
en el mensaje de Opus (tolva, encargo de cristal, resoluciones, glifos ✓★, rebose, F3).

## Playtest 4 → REINGENIERÍA DEL ESPACIO (Opus) — SIN VERIFICAR EN EDITOR
Pase grande. **La grilla pasa de 384x216 a 256x144** (mismo 16:9): cada celda se ve un 50% más
grande —las reacciones dejan de ser "pequeñitas"— y 144/16=9 exacto elimina el chunk de borde
(SimRenderer usa ya un solo buffer scratch, con guardia si alguien rompe la divisibilidad).
Cámara y límites del mundo se DERIVAN de `CellGrid.W/H` (nada de 19.2/10.8 a mano).

**El taller se rediseña como espacio de trabajo** y `Sim/SimLevelBuilder.cs` pasa a ser EL PLANO:
todas las coordenadas viven ahí como constantes públicas y `AlkahestGameBootstrap` las lee (antes
las duplicaba). Suelo a y<14; DOS cubas grandes centrales (x 72..129 y 138..195, interior 52x37)
con placa ígnea cada una; banco de trabajo a la izquierda con los 5 grifos en COLUMNA VERTICAL
sobre un pilar, vertiendo todos en una pila de recogida (x 6..59) — se acabó regar el borde
inferior; estante superior con bandeja fría (piedra gélida, x 36..87) y estantería de redomas
(x 104..168); Tolva reubicada en el contrafuerte derecho (boca x 216..237, y 44..72).

**Máquinas con identidad**: sprites generados nuevos en `Game/MaquinariaSprites.cs` (chasis
remachado con resistencias naranjas, bloque escarchado con agujas de cristal, caño de latón con
boquilla y volante, redomas de vidrio con tapón). Los rótulos son ahora CHAPAS pequeñas y fijas
(`UiStyles.PlacaMundo`, sin recorte vertical, típicamente por DEBAJO del aparato) y el prompt
"E — ..." solo sale si estás cerca y sin botones del ratón pulsados. Nuevo
`Game/MachineFocus.cs`: de todas las máquinas en rango solo responde a E la MÁS CERCANA —
imprescindible con cinco grifos apilados.

**Nueva máquina `Game/StorageRack.cs`**: 5 redomas, una sustancia cada una, cap 300, clic derecho
llena / izquierdo recupera, mini-etiqueta con el nombre bautizado y la cantidad. Captura el ratón
para que el Frasco no pinte sobre el mueble.

**TRES BUGS DE PROGRESIÓN ARREGLADOS** (los tres hacían encargos imposibles en juego real):
1. *El frasco no llevaba temperatura*: al verter, la celda nacía a AMBIENTE, así que "algo helado"
   y "algo que queme al tacto" NO se podían cumplir (nada calienta ni enfría dentro de la Tolva).
   Ahora `Flask` guarda la temperatura media por material y la restituye vía `AlkahestSim.PaintCell`.
2. *No había fuente de Azoth/CrystalSeed/Vivium*: sin ellas, cristal y "algo vivo" eran imposibles
   fuera del F3. Nuevo `Game/MasterSupplies.cs`: al empezar la jornada 2 el Maestro abre el grifo
   de AZOTH (4 Favor/uso, sellado hasta entonces), deja ~80 celdas de VIVIUM en la cuba derecha y
   60 de SEMILLA DE CRISTAL en la bandeja fría, anunciado en la intro de jornada.
3. *La placa ígnea no tenía posición de cultivo*: "Tibia" eran 160 °C y el Vivium muere a 120 °C y
   crece a 30-60 °C — el arco de domesticación entero era inalcanzable. Ahora los estados son
   APAGADA / TEMPLADA (centro exacto de la banda de crecimiento de la seed) / ARDIENTE (320 °C).
   Placa y piedra afectan 3 filas en vez de 2.

Además: aprendiz +40% de velocidad, bobbing a la mitad, sprite 15% más pequeño; `HintSystem` con
guion POR JORNADA (60 s al empezar cada día, incluida la pista de que el fuego se fabrica y no se
compra); `WorkshopBackdrop` reescrito con mampostería, vigas, zócalo y luz de fragua (adiós
"pantallón negro"); encargos de la jornada 3 rebajados a 150 cristal / 220 vivium.

PENDIENTE: verificarlo TODO en el editor (checklist en el mensaje de Opus), y después SFX, agua
con cuerpo (metaballs) y el plan de multiplayer.


## Playtest 8 → REGRESIÓN DE LOS SÓLIDOS, balance medido desde código, y M5 audio + aprendiz — SIN VERIFICAR EN EDITOR
Ronda dirigida por Opus 5 (diagnóstico, decisiones de balance/dirección de sonido/dirección de
arte, especificación de 4 encargos con propiedad de archivos disjunta, revisión de compilación);
Sonnet 5 escribió TODO el código.

**BUG CRÍTICO — los sólidos no se entregaban en la Tolva ("los que resultan de combinaciones
raras", Cesar dixit) — REGRESIÓN NUESTRA del playtest 7.** Al restringir el consumo de la Tolva a
las 3 filas del fondo del pozo (`ChuteSillRows = 3` en `DeliveryChute.cs`) para que lo vertido se
viera CAER, se rompió el caso `StaticSolid`: en `SimStepper.ProcessIfNeeded`, `case
MaterialArchetype.StaticSolid:` está VACÍO — nunca llama a `Move()`, así que Cristal y Hielo no
caen por gravedad (por diseño: son estáticos). Y `Flask.PourMaterial`/`TickPour` pintan la celda
en la posición EXACTA del cursor, a cualquier altura del pozo. Resultado: un sólido vertido a
media altura se quedaba flotando ahí para siempre y el encargo nunca avanzaba. Los "sólidos raros"
de Cesar son justo los productos de reacción — Cristal (cristalización de Azoth) y Hielo
(congelación de Agua) — que es donde más se nota porque nadie los vierte a ras de sillar a propósito.
LECCIÓN GENERAL: cualquier mecánica que asuma que la materia "cae sola" debe comprobar antes el
arquetipo — `StaticSolid` NO tiene gravedad, por diseño de la sim, y va a seguir sin tenerla.
FIX: `ArrastreTick()` nuevo en `DeliveryChute`, a 30 Hz junto al consumo. Recorre
`ChuteMouthX0..ChuteMouthX1` y las filas de `ZoneFloorY1+1` hasta `ZoneY1` **de abajo hacia
arriba** (para que una misma celda no caiga varias filas en un tick — se vería como un salto), y
si la celda de debajo está vacía usa `CellGrid.SwapCells` + `WakeChunk` en origen y destino —
los mismos helpers que `SimStepper.Move`, así no se pierde temperatura ni aux y los chunks se
despiertan. Sin distinguir arquetipo, sin asignaciones, determinista (puramente posicional); la
zona son 22x26 celdas, barato. Justificación de diseño: la Tolva es un aparato del taller y
"engullir" es su verbo — que arrastre hacia su garganta lo que le eches, incluida la piedra, es
coherente con la ficción y conserva la caída visible que motivó el playtest 7. Añadido de paso: un
aviso educativo, una sola vez por material (`_scrapWarned[]`), cuando lo entregado no cuenta para
ningún encargo, con el nombre legible vía el método público nuevo
`OrderSystem.NombreParaMensaje(byte matId)` — antes se tragaba el material en silencio y parecía
roto.

**BALANCE, medido desde el código (no a ojo) — tabla de referencia para rebalancear sin volver a medir:**
| Sistema | Tasa medida | Fuente |
|---|---|---|
| Grifo | rombo de 5 celdas (radio 1) por tick a 30 Hz → techo 150 cel/s; los encargos piden 0,17-0,7 cel/s de media — el grifo nunca es cuello de botella | `Dispenser.cs`: `SpoutRadius`, `TickDt`, `EmitRatePerTick` |
| Cristalización | 12%/comprobación (27% bajo Edicto de Frío Fértil); se comprueba cada tick si la celda se movió, si no 1 de cada 8 ticks (~0,267 s) → ~2,2 s/celda (~0,99 s con el Edicto) | `Universe.Create` + `SimStepper.MaybeReact` |
| Bandeja fría | 46x6 = 276 celdas útiles | `SimLevelBuilder.ChillTray*` |
| Vivium | 60% de éxito, un intento cada 4 ticks por celda asentada en banda con Nutriente; el cuello de botella real es el ARRANQUE (retoño r=5, 81 celdas, nace dormido), no la tasa — una vez alimentado es exponencial | `SimStepper.GrowthTick`, `Universe.VivGrowChancePct`, `MasterSupplies.cs` |
| Cuba B | interior ~52x37 = 1924 celdas, nunca limita | `SimLevelBuilder.cs` |
| Frasco + aprendiz | capacidad 900, alcance 60 celdas sin moverse; aprendiz 11,2 u/s = 112 celdas/s → cualquier viaje cuesta 1-2 s, la logística nunca es cuello de botella | `Flask.cs`, `ApprenticeController.cs` |
| Jornada | 360 s, sin cambios | `DayCycle.DayDurationSeconds` |

Cambios de encargo (**días 1 y 2 SIN TOCAR** — Cesar los completó, son buena referencia): el día 3
pedía 150 Crystal + 100 Named/Flammable + 220 Grows (470 celdas en 6 min, inalcanzable con cristal
a 2,2 s/celda). Rebalanceado a **90 CrystalSolid (+45), 70 NamedMaterial (+35, con fallback
Flammable 100) y 130 Grows (+50)** — unas 290 celdas, el 60-70% de la jornada, dejando margen para
experimentar (experimentar es el corazón del juego, no un extra).
Segundo fallo, aritmético: 20 de inicio + 50 (día 1) + 105 (día 2) = **175★ SIN tocar el día 3**,
contra `WinFavorTarget = 120` — la meta no significaba nada, se superaba sola. Diseño nuevo: la
partida SIEMPRE dura las tres jornadas (el arco de tres días es la forma del juego) y el final se
GRADÚA en cuatro desenlaces (`OrderSystem.Desenlace`): **despedido** <120, **aprendiz** ≥120,
**oficial** ≥180 (justo por encima del máximo pre-día-3 de 175: exige entregar algo del día 3),
**maestro** ≥260 (máximo teórico ≈305 menos el colchón de lo que se gasta en grifos de Favor).
`OrdersHud` muestra el escalón vigente y reescala la barra a ese escalón en vez de quedarse
clavada al 100%; la pantalla final nombra el desenlace y dice cuánto faltaba para el siguiente.
`DayCycle` ya no corta la partida por meta alcanzada ni por "dos jornadas sin entregar" (ese aviso
queda como texto de sabor, `_avisoDesatencion`).
Favor como recurso: `favorCostPerActivation` NO estaba a 0 (Agua/Arena 0, Aceite 2, Nutriente 5,
Azoth 4) — era irrelevante porque 120 se superaba solo. Con los cuatro escalones, cada Favor
gastado compite con llegar a oficial/maestro. RECOMENDACIÓN NO IMPLEMENTADA: el coste solo se
cobra al pasar OFF→ON, así que se puede dejar un grifo abierto indefinidamente gratis; considerar
coste por volumen o goteo por tick si se quiere más fricción.

**M5 AUDIO — el taller suena (antes el juego era completamente mudo).** Dos archivos nuevos en
`Assets/Alkahest/Audio/`: `SintetizadorSfx.cs` (fábrica estática que sintetiza y cachea 13
`AudioClip` por código: primitivas de ruido, paso-bajo de un polo, paso-bajo barrido, seno/
triángulo, desafinado, trémolo, granos, envolventes AD/campana y crossfade de bucle) y
`DirectorDeAudio.cs` (MonoBehaviour con pool fijo de voces: one-shot + bucles + una voz por
grifo; cero asignaciones por frame). CERO ASSETS, como todo el proyecto.
Los 10 sonidos y su timbre: lecho ambiental (ruido marrón doble-filtrado + zumbido de fragua a
42/84 Hz), grifo por arquetipo (líquido = medio con burbujeo por trémolo, polvo = agudo granular,
gas = siseo tenue), fuego (ruido con granos de amplitud, volumen y `AudioLowPassFilter` modulados
por cuánto fuego hay), aspirar/verter (barrido de paso-bajo ascendente/descendente), ignición
(seno grave descendente + soplo), cristalizar/congelar (campanilla de 4 parciales inarmónicos
desafinados con caída independiente), tolva (chirrido grave + granos), bautizar (dos notas de
triángulo G4→C5), encargo completado (acorde do-mi-sol de latón), fin de jornada (campana de 5
parciales + gong, ~1,4 s).
Reglas de dirección de sonido a respetar en el futuro: nada de senos pelados ni ruido blanco crudo
(todo pasa por paso-bajo — el taller es piedra, latón, fuego y líquido), todo clip empieza y acaba
en silencio (un clic al inicio se oye como defecto), volumen maestro 0,5 y mezcla por debajo del
umbral de molestia (es un juego de observar), variación de pitch ±6% / volumen ±10% en cada
one-shot.
LIMITADOR DE RITMO (lo crítico): la sim dispara cientos de eventos por tick. Máximo 6/s para
cristalizar+congelar (comparten limitador) y 4/s para el resto; los eventos suprimidos SUBEN el
volumen del siguiente disparo (hasta +100%) en vez de sonar más veces, así una avalancha de
cristalización suena a avalancha y no a metralleta.
Cómo se entera de lo que pasa: `SimStepper.Events` (`SimNotableEvent[]` público) + `EventHead`;
`SubstanceKnowledge` ya lo leía de forma NO destructiva con su propio índice, y `DirectorDeAudio`
hace lo mismo con `_ultimoEventoLeido` y un `while (i != head && pasos < EventBufferSize)`, así
que ambos conviven sin robarse eventos y no hizo falta tocar `Sim/`. Lo que no tiene evento
dedicado se observa por estado: delta de `Flask.Total`, muestreo de la boquilla del grifo,
muestreo del sillar de la Tolva, `SubstanceKnowledge.CountNamed()` y `OrderSystem.CompletedCount()`
al subir, y flanco de subida de `DayCycle.InputLocked` para el fin de jornada.
Control: tecla **M** silencia (comprobado que no pisa F3/H/T/E/Q/flechas/WASD), persiste en
`PlayerPrefs`, funciona incluso con `DayCycle.InputLocked` porque es una preferencia y no una
acción de juego. **Interruptor general: `private const bool SistemaActivo = true;` en
`DirectorDeAudio` — a `false` desactiva el componente entero en `Awake`. Es el plan B si el audio
no convence.**

**M5 APRENDIZ — de cuadrado morado a imp.** `ApprenticeController.cs` reescrito, pase puramente
VISUAL (física, velocidad y alcance sin tocar). Silueta: cabeza grande / cuerpo pequeño (elipses
solapadas), dos cuernecillos con punta de latón, dos alas de polilla (barrido Bézier con vena de
latón), cola fina con cuenta en la punta, collar y gema en la frente, ojos grandes con pupila y
punto de luz. Paleta: cuerpo morado claro desaturado (0xA8,0x96,0xC4), luz (0xD2,0xC4,0xEC),
sombra (0x6E,0x5E,0x8E) — más luminoso que el ciruela del fondo — y contorno casi negro
(0x16,0x10,0x1E) de 1 téxel en toda la silueta vía una pasada genérica `AplicarContorno`, para que
no se pierda ni contra el ladrillo ni contra materia saturada. Latón según la regla del repo:
(168,126,58)/(214,176,96)/(86,62,28), nunca `UiStyles.Oro`.
Resolución: de 24x28 téxeles a 33 ppu, ahora 72x84 a 99 ppu — mismo tamaño de mundo exacto
(72/99 = 24/33), x3 de detalle, coherente con `MaquinariaSprites`.
Animación sin Animator ni clips: bobbing en reposo (valores del playtest 4 intactos); aleteo con
dos capas rotando sobre su gozne, frecuencia 1,1 Hz en reposo → 5,4 Hz a velocidad máxima con
suavizado exponencial y el ala trasera desfasada 0,5 rad para dar profundidad; inclinación del
cuerpo hasta 16° proporcional a la velocidad horizontal (lo que más "peso" da a un personaje
volador); parpadeo alternando dos sprites de cuerpo pre-generados a intervalos irregulares de
2,2-6,5 s; y el frasco persiguiendo con `SmoothDamp` el mismo punto que usa `Flask.cs`, para que se
retrase un pelín al arrancar y frenar.
`CarryAnchor` conserva su fórmula (único consumidor externo: `Flask.cs`). `sortingOrder` base 50,
capas derivadas -2/-1/+1/+2, todas por debajo del indicador de `Flask.cs` (60).

**PENDIENTE tras esta ronda** (orden sugerido):
(a) verificar en Unity que TODO compila y jugar la partida entera de 3 jornadas con el balance
nuevo; (b) decidir si el audio se queda o se apaga con `SistemaActivo = false`; (c) **build limpia
de Windows** (nunca se ha verificado una desde el rediseño del espacio, playtest 4); (d) renombrar
el repo GitHub `Alkahest`→`ChaosAlchemy` + `git remote set-url`; (e) replantear las redomas (Cesar
sugirió que quizás deberían ir abajo y levantar el gameplay de reacciones); (f) resto de M5 (glow,
agua con más cuerpo); (g) integración multiplayer (sim solo-host + deltas RLE por chunks despiertos
a 10-15 Hz, MEDIR antes de decidir — sección Backlog).
Preguntas abiertas para el próximo playtest: ¿el audio funciona o hay que apagarlo? ¿el día 3 se
siente jugable con 290 celdas? ¿los cuatro desenlaces dan razón para seguir jugando pasada la
meta? ¿el imp se lee bien en movimiento sobre materia saturada?


## Playtest 9 → LA LEY DE LAS SEMILLAS, dos bugs de difusión de temperatura, el camino del fuego,
## Favor solo por encargos, regresión del cierre de jornada, y limpieza de audio — SIN VERIFICAR EN EDITOR
Ronda dirigida por Opus 5 (diagnóstico de los siete hallazgos de Cesar, especificación de 4
encargos con propiedad de archivos disjunta, revisión de compilación); Sonnet 5 escribió TODO el
código.

**1. LA LEY DE LAS SEMILLAS — el problema más importante de la ronda.** Cesar, tras horas de
juego: *"Aún no descifro cómo hacer crecer cristal o vivium; por más que hago combinaciones no he
conseguido multiplicar la cantidad del producto. Está raro que me den ingredientes que también son
la meta."* Lo entendía bien: era un fallo de ENSEÑANZA, no de balance ni de simulación. Diagnóstico
reutilizable: **el jugador creía que las muestras del Maestro eran INGREDIENTES que se gastan; son
SEMILLAS, catalizadores que no se consumen.**
Leyes verificadas en el código (referencia futura): Cristal — `Universe.Create` hornea
`Reaction(Azoth,Crystal,Crystal,Crystal)` y `Reaction(Azoth,CrystalSeed,Crystal,CrystalSeed)`; como
`ReactionEngine`/`SimStepper.TryReactNeighbor` solo transforma la celda cuyo producto difiere del
original, **la semilla no se consume**. 12%/comprobación (27% con el Edicto de Frío Fértil), con el
AZOTH a ≤5 °C (≤20 °C con el Edicto) — se comprueba la temperatura de la celda de Azoth porque el
Cristal es `StaticSolid` y nunca dispara `MaybeReact`. Vivium (`SimStepper.GrowthTick`): una célula
asentada busca Nutrient ortogonal; si SU PROPIA temperatura cae en `[VivGrowMinRaw,VivGrowMaxRaw]`
(30-60 °C ±15 de jitter por seed; -20 con Frío Fértil), consume ese Nutrient y con
`VivGrowChancePct=60%` fijo crea Vivium nuevo — la madre NUNCA se transforma. 1 intento cada 4
ticks por célula, máx. 1 Nutrient por célula y tick. Fuera de banda no muere, se marca dormida
(`OrganicDormantAux`); solo muere por encima de 120 °C (`boilsAt→Ash`). Grifos: caudal infinito,
sin reservas; jornada 1 Agua/Arena gratis, Aceite 2 Favor, Nutriente 5; Azoth (4) nace sellado y lo
abre `MasterSupplies` al empezar la jornada 2.
Qué se hizo: (a) `SubstanceKnowledge` añade el momento **LEY DESCUBIERTA**: engancha el ring buffer
de `SimStepper` con su propio índice (mismo patrón no destructivo que `DirectorDeAudio`), dispara
una sola vez por partida en el primer `Crystallize` y el primer `Grow`, texto construido una vez
(nunca en OnGUI) con los nombres bautizados si existen, panel cálido de 7 s con cola FIFO de 2. (b)
`JournalHud` añade una sección **LEYES**: recorre `Universe.Reactions.TryGet` para todos los pares
(más la ley de crecimiento del Vivium a mano, que no vive en esa tabla) — sigue siendo correcta si
las leyes cambian por seed; marca `★ SE PROPAGA` con la regla genérica `(productA==a) != (productB
==b)`; cacheada por `CountDiscovered()` + la propiedad nueva `SubstanceKnowledge.NamingVersion`
(necesaria porque `CountNamed()` no detecta un re-bautizo). (c) Pistas de jornadas 2 y 3
reescritas como procedimientos con ubicaciones reales de `SimLevelBuilder`. (d)
`MasterSupplies.TextoEntrega` reescrito: "tres SEMILLAS, no ingredientes: no se gastan, se
ALIMENTAN...". RIESGO ANOTADO en el backlog: el texto pasó de ~155 a ~330 caracteres dentro de un
panel de altura fija (490 px, `DayCycle.AbrirPanel`); usa `GUILayout.FlexibleSpace()` antes del
botón así que debería absorberlo, pero falta verificarlo jugando la jornada 2.

**2. DOS BUGS DE ARITMÉTICA EN LA DIFUSIÓN DE TEMPERATURA (el frío desbocado).** Reporte: *"la
congelación con el tiempo parece que irradia infinito; después de un rato ya no puedo ni abrir el
caño de agua porque sale una tirita de hielo y se tapa."* No era `ChillStone` (verificado: apaga
bien, acotada a la bandeja, piso `ColdRaw=20`). Eran dos fallos en `SimStepper.DiffuseTemperature`:
- **Trinquete colapsado a 1/8 de la grilla**: el tirón hacia ambiente estaba guardado por
  `if ((_tick & 7u) == 0u)` DENTRO del bucle `for (i=offset; i<n; i+=8)` con `offset=tick%8`. Como
  `offset` ES `tick%8`, la condición solo puede cumplirse en la pasada donde `offset==0`: solo las
  celdas con `i%8==0` recibían el tirón; el resto de la grilla (7/8) NUNCA lo recibía. La difusión
  entre vecinos sí corría en toda la grilla y paseaba el frío por el taller, sin nada que lo
  devolviera a 20 °C. Mismo patrón de trinquete que el bug del hielo del playtest 1, reencarnado
  en la aritmética. Fix: condición basada en `(_tick>>3)&3u` (cuenta las difusiones de cada celda,
  no el offset), así el tirón alcanza toda la grilla cada ~32 ticks.
- **Redondeo sesgado hacia el frío**: `diff>>2` es desplazamiento aritmético y en C# equivale a
  `floor(diff/4)` para negativos: `+5>>2=+1` pero `-5>>2=-2`. Enfriarse iba al doble de velocidad
  que calentarse, 30 veces por segundo, en toda la grilla, con o sin piedra encendida. Fix:
  `diff/4`, que trunca hacia cero simétricamente.
LECCIÓN GENERAL: en un campo que se actualiza a 30 Hz, cualquier asimetría de redondeo o guarda que
no cubra todas las celdas se convierte en deriva garantizada. Al tocar la difusión, comprobar
SIEMPRE que la guarda cubre el 100% de las celdas y que el redondeo es simétrico en signo.

**3. EL CAMINO PARA CONSEGUIR FUEGO NO EXISTÍA.** Reportes: *"no sé si la idea es que luego me den
un caño de fuego"* + *"el fuego en el aire sigue siendo gris"* + *"la reacción [del aceite] es muy
rápida"*.
- **Autoignición ausente**: `TryIgnite` (con su comprobación de `ignitionTemp`) solo se llamaba
  desde `ProcessFire`, es decir, hacía falta una celda de Fuego vecina — no había autoignición
  espontánea por temperatura, así que aceite sobrecalentado sin llama cerca NUNCA prendía: el único
  camino legal para obtener fuego (placa ARDIENTE bajo aceite) estaba roto. Los números sí daban:
  placa Ardiente = raw 220 (320 °C), `ignitionTemp` del aceite ~208-312 °C según seed (raw ~164-
  216). Fix: rama genérica de autoignición en `ApplyPhase` (junto a fusión/ebullición/condensación,
  mutuamente excluyentes) para cualquier material `flammable` con `ignitionTemp` finito.
- **Fuego pintado con F3 = gris**: `AlkahestSim.Paint`→`CellGrid.SetCell` pone `aux=0` sin pasar
  por `Transform`, así que la celda llegaba a `ProcessFire` con `life=0` y sin combustible cerca se
  convertía en Humo/Ceniza en el primer tick — el jugador nunca veía la llama, veía humo. Fix:
  `life==0` a la entrada se interpreta ahora como "recién creada" (nunca puede serlo por expiración,
  que siempre transforma la celda el mismo tick) y se siembra con ~16±3 ticks (~0,5 s), corta frente
  a los ~80 del fuego alimentado; más un tramo de decaimiento a media velocidad por debajo de 6 de
  vida para que se apague desvaneciéndose. La PALETA de la llama en `SimRenderer` no se tocó
  (validada por el jugador).
- **Combustión del aceite**: probabilidad por tick en `TryIgnite` de **50% → 12%** (con 50% un
  charco conectado prendía entero en 1-2 ticks; con 12% el frente avanza ~1 celda cada 8 ticks,
  ~0,27 s: observable y utilizable).

**4. FAVOR: FUERA LA PUNTUACIÓN PARALELA.** Reporte: *"me suben los puntos aunque le agregue
cualquier cosa a la tolva y esta me diga que no lo necesita... o solo nos quedamos con el de las
misiones si te parece bien."* Se eligió su segunda opción. Eliminados `ScrapPerFavor`, el contador
`_scrap` y la llamada `AddFavor(1)` de `DeliveryChute` — era la única fuente de Favor que no fuera
completar un encargo. Razones: incoherente con la ficción (el Maestro paga lo que encargó), rompía
los cuatro desenlaces (si cualquier basura suma, los umbrales no miden nada), y el juego se
contradecía a sí mismo (decía "esto no lo necesito" y acto seguido pagaba). La materia que no
cuenta se sigue consumiendo (engullir sigue siendo el verbo de la Tolva) pero sin pagar.
`OrderSystem.TryDeliverCell` pasa de `bool` a `enum DeliveryOutcome { Progressed,
OrderAlreadyComplete, NoMatch }` para distinguir "material equivocado" de "ese encargo ya está
completo" — el segundo caso importa porque significa que el jugador está desperdiciando trabajo.
Aviso una-vez-por-material reutilizando `_scrapWarned`.
Consecuencia de balance: antes el máximo de Favor era ILIMITADO (la chatarra sumaba sin tope);
ahora **305 ★ es el techo real y exacto** de una partida perfecta (20 inicial + 50 día 1 + 105 día
2 + 130 día 3, menos lo gastado en grifos). Los umbrales 120/180/260 no cambian. Única fuente de
Favor: completar encargos. Único gasto: `Dispenser.favorCostPerActivation` (Agua/Arena 0, Aceite 2,
Nutriente 5, Azoth 4).

**5. REGRESIÓN: el cierre anticipado de jornada se había perdido.** El aviso "todos los encargos
entregados · pulsa ENTER" del playtest 6 desapareció en la reescritura de `DayCycle` del playtest 8
(sistema de cuatro desenlaces). Restaurado como `UpdateAllOrdersDoneEarlyClose()`: se dispara con
`AllOrdersCompleted()`, 12 s de cuenta atrás (`DayEndAutoCloseSeconds`) acotada cada frame a
`_timeRemaining`, ENTER la corta, y usa el MISMO `EnterDayEnd()` que el fin por temporizador. Cierra
la JORNADA, no la partida: la partida sigue durando siempre tres jornadas. LECCIÓN: al reescribir
`DayCycle` hay que comprobar que este cierre sigue vivo — ya se perdió una vez.

**6. AUDIO: cuatro causas del popeo, y dos timbres mal.** Reportes: *"el sonido ambiental popea,
sobre todo cuando cruza con otra cosa que tenga sonido"*, *"el agua parece más arena que agua"*,
*"tampoco suena como fuego"*.
- **Costura del bucle**: `SuavizarBucle` mezclaba cabeza Y cola al mismo valor, lo que DESPLAZABA
  el salto al borde de la ventana de crossfade en vez de eliminarlo. Reescrito para tocar solo la
  cola, arrastrándola hacia la cabeza con peso smoothstep (derivada cero en los bordes). El bucle
  ambiental se construye ahora en muestras exactas (525×189=99225) para que el zumbido de 42/84 Hz
  cierre en fase.
- **DC offset**: `PasoAltoDC` (~20 Hz) aplicado a los 5 bucles largos antes de normalizar y coser.
- **Saturación al sumar** (la causa que describía el jugador): Unity suma las voces y recorta a
  1.0. Presupuesto de mezcla documentado en `DirectorDeAudio`: ambiente de pico 0.5→0.28 y volumen
  0.55→0.30, fuego y grifos recortados, más ducking sencillo (cada one-shot agacha los bucles a
  0.55× durante ~300 ms). Suma de picos ≈0.57 típico y ≈0.78 en avalancha, bajo el tope de 0.8.
- **Cambio de volumen en seco**: la tecla M saltaba 0↔0.5 de golpe sobre fuentes que seguían
  sonando. Ahora `_factorMaestroSuavizado` rampa en ~60-70 ms.
- **El agua sonaba a arena porque era la misma receta**: ruido con TRÉMOLO DE AMPLITUD, la firma
  acústica de un árido. Ahora: base grave (corte ~170-350 Hz) con el CORTE del filtro modulado en
  vez de la amplitud, más `AnadirBurbujas` — senos de 20-40 ms con barrido ascendente de 350 a
  1000 Hz, ~4-5/s. Ese barrido ascendente es lo que el oído reconoce como agua. Regla de sonido a
  respetar: modular el corte = líquido; modular la amplitud = árido.
- **El fuego no sonaba**: 220 sondas sobre 36.864 celdas, con el fuego sin combustible apagándose
  en 1 tick, exigían ~3.300 celdas ardiendo a la vez para saturar. Fix: ataque instantáneo con piso
  audible (0.42) en cuanto se detecta una celda, liberación lenta (2 s), 700 sondas, saturación de
  20→5. Y el timbre: los chasquidos se sumaban al rugido ANTES de un segundo paso-bajo que se los
  comía, dejando viento. Separados en dos capas: rugido grave continuo (380 Hz) y chasquidos
  brillantes (4-9 ms, corte a 7500 Hz solo anti-aliasing).

**7. Menor: el ala del aprendiz tapaba el cuerpo.** `AlaDelantera` estaba en `sortingOrder+1`.
Nuevo orden de atrás a delante: `Cola(-3) < AlaTrasera(-2) < AlaDelantera(-1) < Cuerpo(0)`, base
50 — ambas alas detrás del cuerpo, coherente con una criatura de perfil (las dos nacen del lomo).
La profundidad entre ellas se conserva por el tono atenuado del ala trasera y el desfase de
aleteo. `CarryAnchor`, física y tamaño de mundo intactos.

**PENDIENTE tras esta ronda** (ver Backlog arriba para el detalle completo).
Preguntas abiertas para el próximo playtest: ¿el momento "LEY DESCUBIERTA" desbloquea de verdad la
comprensión del motor? ¿quedó limpio el audio? ¿el fuego se lee ya como fuego y el camino
placa→aceite se encuentra solo? ¿el frío se comporta como un charco local estable?

---

## Playtest 24 → LA MAREA — **DESCARTADO POR CESAR** (retirado del código en el playtest 25)

> Cesar, tras leer la visión y probar la build: "fue una idea atrevida e interesante, pero la
> descarté". El código se retiró ENTERO en la ronda siguiente (revert quirúrgico a playtest 23 +
> borrado de MareaDirector); esta sección queda como archivo de la decisión (las decisiones se
> documentan, no se borran). Las reglas 49-50 de CLAUDE.md, nacidas de su integración, SÍ
> sobreviven — son del proyecto, no de la marea. Su reemplazo: la dirección de Cesar
> "descubrir qué persiste", ver playtest 25 y docs/DISENO_LO_QUE_PERSISTE.md.

**El mandato de Cesar, literal**: "necesito un juego y lo que tengo ahora hay experimentación...
quiero que ensayes tú una nueva dirección creativa, disruptiva si cabe el término... quiero probar
tu visión después de una super modificación no una pequeña, todo de golpe, confío en ti."

**LA VISIÓN EN UNA FRASE**: el mundo se está digiriendo a sí mismo; tú, tus criaturas y las leyes
que descubras sois lo único que mastica en dirección contraria.

### Qué es LA MAREA

La afinidad de la semilla (playtest 18, `Universe.AfinidadDelUniverso`) deja de ser un dato de
sabor y se convierte en EL ANTAGONISTA. Desde un CORAZÓN enterrado en el zócalo del sótano
(`SimLevelBuilder.CorazonMarea*`, x352..373 y14..19, cámara carvada + fila de Marea dormida) mana
una marea violeta-oscura TINTADA 20% hacia el color del material afín de la run — la marea ES la
química de esta semilla hecha carne. Convierte lo que toca en sí misma (6% líquidos/polvos/
orgánicos por muestreo, 1% Vivium/hielo/cristal — engullir un cuerpo SE VE VENIR), amortigua la
temperatura hacia -20°C a su alrededor (apaga la estrategia térmica cerca del frente), y la PIEDRA
ES INMUNE — el cincel pasa de herramienta a FORTIFICACIÓN.

Sus dos debilidades: el FUEGO la quema con pérdida (10% → Smoke, no se recupera lo quemado), y el
ROCÍO — oro pálido, brilla en la oscuridad — la mata 1:1 SIN AZAR (Marea+Rocío → Sand+Empty,
determinista: el jugador puede CONTAR su cura). El Rocío solo sale de un sitio: la criatura
digiriendo Marea (caso previo a los tres escalones de `EscogerProductoDigestion`, igual en TODO
universo). La criatura le TEME a la marea y la digiere A LA VEZ — sufre para fabricar la cura — y
muere si la marea cubre su núcleo 9 s (su cuerpo Vivium se convierte en Marea: engullida de
verdad, no borrada).

### El arco

- **Despertar** (`MareaDirector`, sondeo 2 s): 12 celdas REALMENTE talladas con el cincel
  (`Cincel.CeldasTalladas` — abrir camino a la Tolva son ~23, así que el primer viaje al mundo
  YA la despierta: "el mundo también se abre hacia ti") o 300 s jugables. Pista: "Algo se ha
  despertado abajo."
- **Primer Rocío**: flag `Criatura.RocioExudado` marcado en `CompletarDigestion` (fix de
  integración: la pista didáctica dispara EN el momento de exudar, no cuando el Rocío llega al
  corazón — ahí ya no enseña nada). Pista: "Eso que exuda tu criatura HIERE a la marea."
- **La marea sube** (por encima de `SotanoY1-20`): "La piedra la frena; el cincel ya no es solo
  una herramienta."
- **VICTORIA**: ≥24 celdas de Rocío dentro del rect del corazón (≈2 frascadas — exige el VIAJE por
  el pozo, no una gota simbólica) → "EL MUNDO SE AQUIETA". **DERROTA**: marea despierta y
  `Criatura.NumVivas == 0` → "LA MAREA OS TRAGÓ". Ambas en `DayCycle.TerminarPartida` +
  `DrawEndScreenMarea` (el desenlace clásico por Favor queda íntegro para el modo cronometrado).

### Por qué esto convierte la experimentación en JUEGO

Todo lo que ya existía gana propósito sin cambiar: el cincel fortifica (piedra inmune), la mudanza
es retirada, el taller clásico enterrado es un ARSENAL que reclamar (el grifo de aceite = arma de
fuego), los túneles que caves son riesgo (la marea sube por ellos), el capullo son vidas extra, y
las leyes con condición térmica de cada semilla siguen siendo el laboratorio — pero ahora hay un
RELOJ DE PRESIÓN lento que da significado a saber más que el mundo.

### Implementación (dos encargos Sonnet en paralelo sobre docs/CONTRATO_MAREA.md, congelado)

- **Encargo A (Sim)**: `MaterialId.Marea=17/Rocio=18/Count=19`; defs con firma visual FIJA entre
  universos (excepción documentada a la regla 17: Pulso/Halo para la marea, la amenaza central se
  reconoce en cualquier seed); `SimStepper.ProcessMarea` (proceso PROPIO, jamás ReactionEngine —
  regla 33 intacta) con muestreo 1/8 y sales nuevas 231/233/235; emisión 1 celda/20 ticks;
  `SimStepper.MareaActiva` (gate, default false); cámara del corazón en `BuildCuartoIntimo`.
- **Encargo B (Game)**: `MareaDirector` (nuevo), `Cincel.CeldasTalladas`, miedo+digestión+muerte
  en `Criatura`, `DayCycle.TerminarPartida`, `HintSystem.EncolarPistaDeMarea` (canal de prioridad
  que interrumpe la rotación, misma placa).
- **Fixes de integración (Fable)**: (1) fluidity 120/200 del contrato corregida a 1/4 — el campo
  se consume como Nº de celdas a escanear por tick (escala real 1-4), 120 habría sido un tsunami
  de un tick y hasta 120 iteraciones/celda/tick; (2) el gate `MareaActiva` faltaba DENTRO de
  `ProcessMarea` (el docblock prometía "dormida no convierte" pero nada lo cumplía: habría digerido
  el sótano desde el tick 0); (3) pista de primer Rocío movida del corazón al momento de exudación.

**Verificado**: compila sin errores ni warnings en el Unity real (vía MCP), escena regenerada,
30 s de play sin excepción, MareaDirector spawneado. `Unity_RunCommand` no devolvió logs (fallo
del lado del paquete de Unity AI), así que el despertar forzado no se pudo probar en vivo: el
primer despertar real lo verá Cesar.

### Guion esperado de la partida

Despiertas con tu Rescoldo → lo alimentas, nace la cría fría, aprendes hielo (cadena del playtest
23 intacta) → cavas hacia la Tolva → AL ABRIR EL CAMINO, la marea despierta (pista) → sigues
jugando, la marea sube por el sótano → la ves por el pozo, o sube hasta tocar tus túneles → pruebas
cosas: el agua se corrompe, la piedra aguanta, el fuego muerde con humo → aspiras marea con el
frasco y se la viertes a tu criatura (se asusta... y la digiere) → EXUDA ORO QUE BRILLA → pista:
eso la HIERE → decides: ¿fortificar y criar, o bajar YA con dos frascadas? → el viaje por el pozo
con el frasco lleno de Rocío, la marea subiendo por el mismo hueco → viertes sobre el corazón,
24 celdas → EL MUNDO SE AQUIETA.

**Preguntas abiertas para el playtest**: ¿el ritmo de emisión (1 celda/20 ticks) da un juego de
~20-40 min o hay que acelerarlo tras el despertar? ¿la marea llega a AMENAZAR el cuarto íntimo o
se queda en espectáculo del sótano (el pozo está lejos del cuarto)? ¿9 s de núcleo cubierto se
leen como agonía evitable o como muerte súbita injusta? ¿24 celdas de Rocío son el número
correcto? ¿hace falta que la marea EMITA también desde celdas convertidas lejanas (frentes
secundarios) para que fortificar importe de verdad?

---

## Playtest 25 → LO QUE PERSISTE: el cambio de eje (dirección de Cesar; Fable dirige; TRES encargos Sonnet en paralelo)

**El mandato de Cesar**: la idea central cambia de "aprender cómo fabricar cosas" a "DESCUBRIR
QUÉ PERSISTE ante determinadas condiciones". Toda propiedad relevante debe ser observable y
manipulable; el orden de las operaciones debe importar; toda semilla debe garantizar al menos una
solución persistente; procesos patentables; pedidos por propiedad, no por objeto. Diseño completo
respondiendo sus 8 puntos: `docs/DISENO_LO_QUE_PERSISTE.md`. Contrato de implementación:
`docs/CONTRATO_PERSISTE.md`.

**La visión en una frase**: cada semilla es una pregunta — ¿qué puede durar aquí? — y el
laboratorio es cómo el jugador le arranca la respuesta al mundo.

### Lo construido

**El retículo de estados (el corazón).** 5 materias base por semilla × 8 estados
(Polvo/Fundido/Templado/Recocido/Compacto/Cerámico/Calcinado/Solución), cada estado un MaterialId
propio (18..57, `Count` 17→58), generados por tabla en `Universe.Create`. EL HISTORIAL VIVE EN EL
ESTADO (materiales markovianos): el orden importa porque el grafo es no conmutativo — fundir y
prensar escupe líquido (nada); prensar y hornear da cerámico (el techo). Enfriar RÁPIDO en el
mundo = Templado (duro, la prensa lo revienta); enfriar LENTO dentro del crisol = Recocido
(dúctil, la prensa lo compacta). Solo 3 transiciones viven en MaterialDef (fundir/templar/
evaporar-precipita); calcinar, ceramizar y recocer son del Crisol. Dos ejes de legibilidad
visual: la BASE se reconoce por el tono (sorteado como innominado), el ESTADO por el tratamiento
fijo entre universos (fundido brilla, compacto prieto, calcinado carbonizado...).

**El Limo primigenio.** El caño de nutriente ahora gotea LIMO (la criatura está aparcada, ver
abajo): una suspensión turbia de la que desciende TODA la materia base del universo. Calentarlo
(raw 112, al alcance del rescoldo) lo separa: cada celda precipita el polvo de una base por
sorteo determinista por-celda con pesos por seed. El primer gesto del juego ya es el juego.

**Las máquinas.** CRISOL (la central): rescoldo propio tier0 (raw 120 — hierve TODO lo acuoso en
toda seed, no funde nada) y temperatura máxima decidida por el COMBUSTIBLE que le cargues
(la progresión térmica es descubrimiento, no un dial); calcina en banda sostenida, ceramiza el
compacto, y recuece lo fundido que muere dentro (gana la carrera al templado del mundo con +4 raw
de margen). PRENSA: E y la mandíbula cae — compacta lo dócil, revienta lo frágil, ESCUPE los
líquidos (desplazamiento físico, no números). BANCO DE CHISPA: el instrumento de análisis puro —
dos bornes y una lámpara que delata la conductividad (0/1/2), LA propiedad invisible. Columna de
ensayo de cristal en el nivel (estratificación/disolución/flotación se observan solas).

**Pedidos por propiedad + el Ensayo del Maestro.** Arco fijo de 5, de uno en uno (el arco ES el
tutorial): separar limo → algo que aguante el rojo → algo que encienda la lámpara → algo que
flote sin disolverse → el PROCEDIMIENTO por escrito (paga doble: el conocimiento vale más que la
sustancia). El pedido de calor se resuelve en el ENSAYO junto a la Tolva: la muestra se calienta
A LA VISTA 5s y se cuentan supervivientes; estrellas ★/★★/★★★ por margen real de umbral (Favor
x1/x1.5/x2) — el espectro de soluciones "mejores o peores" instaurado desde el pedido 2. El fallo
no consume el pedido y dice CÓMO murió la muestra.

**Hornada + patentes v0.** Las máquinas registran cada op (entrada→salida+condición) en un ring
de 8; el primer (base,estado) jamás producido congela una PATENTE (hasta 4 pasos), que entra como
página numerada en la sección PROCEDIMIENTOS del diario y se bautiza con el flujo de siempre.

**El solver de garantía.** En `Create`, BFS sobre los ~41 nodos con la escalera térmica
tier0→tier1: garantiza en TODA semilla (reintenta el sorteo hasta 50 veces, clampea si agota) que
(1) existe la escalera hervir→calcinar→combustible→fundir, (2) existe un ganador ENTREGABLE con
umbral ≥ ensayo+10, (3) hay conductor alcanzable, base soluble y base insoluble. La temp del
pedido de calor sale del solver — los pedidos imposibles son estructuralmente imposibles ahora.
Log por seed: "Persistencia: ganador=<id> a N pasos, combustible=base K (verificado)".

**La criatura APARCADA, no borrada** (regla 15): Criatura/Capullo intactos en el repo, sus spawns
comentados. Volverán como el escalón vivo del sistema (organismos que satisfacen predicados — el
sueño del "veneno para ratas" de Cesar).

### Fixes de integración (Fable, sobre los encargos)

1. **Regla 50 otra vez, dos números míos del contrato**: tier0 118→120 (el agua de la seed
   hierve hasta raw 119: con 118 el rescoldo no evaporaba en el peor sorteo) y separación del
   limo 150→112 (mi contrato decía "150 (60°C)" — aritmética rota, raw 150 son 180°C,
   inalcanzable sin combustible). El agente A SEÑALÓ la inconsistencia en vez de obedecerla a
   ciegas — exactamente lo que el contrato le pide.
2. **El ganador fundido (regla 51 nueva)**: el solver elegía como "ganador garantizado" al estado
   FUNDIDO (umbral 255: nada lo transforma hacia arriba — físicamente cierto, jugablemente
   absurdo: se templa en el viaje al plinto). Cazado EN EL PRIMER ARRANQUE gracias al log del
   solver ("ganador=19"). Fundido y Solución excluidos de la candidatura: la garantía cuantifica
   sobre estados ENTREGABLES.

**Verificado**: compilado en el Unity real vía MCP con CERO errores y CERO warnings a la primera
(tres agentes en paralelo, 15 archivos, ~1.500 líneas nuevas — el contrato congelado funciona);
escena regenerada; 40s de play sin excepción; solver verificado en dos seeds reales (ganador
entregable a 2 pasos en ambas).

### Guion esperado de la partida

Apareces en el laboratorio con dos caños (agua y limo turbio) y el primer pedido ya visible:
"separadme el limo". Viertes limo en el crisol → hierve solo (rescoldo) → precipitan arenas de
colores → entregas una, pura → pedido 2: "algo que aguante el rojo". Pruebas tu polvo en el
plinto: FUNDE a mitad del ensayo (el fallo te lo dice) → tuestas polvos en el crisol → uno se
vuelve negro carbón → ¡NUEVO PROCEDIMIENTO! (patente) → lo cargas de combustible: el crisol RUGE
→ ahora sí: pruebas el calcinado en el plinto y AGUANTA (★★) → pedido 3: la lámpara → pasas todo
tu catálogo por el banco de chispa... → pedido 5: el Maestro te compra el LIBRO. Cada eslabón
enseña el siguiente y cada respuesta abre dos preguntas.

**Preguntas abiertas para el playtest**: ¿la separación del limo se LEE (celdas precipitando en
colores) o hace falta más teatro? ¿el crisol comunica sus 3 modos (rescoldo/combustible/
enfriando)? ¿20s de calcinación sostenida son eternos o justos? ¿el arco de 5 pedidos dura una
sesión buena (~30-45 min)? ¿la patente se siente como MI descubrimiento o como un popup?

---

## Playtest 31 → LA IDENTIDAD (ronda nocturna de Opus 5 con ojos, 5 ciclos desplegar-jugar-mirar-corregir)

**El encargo literal de Cesar** (mañana enseña el juego a sus amigos): *"el menú de bautizar tiene
que dejar de parecer un menú de Windows XP y ascender a integrar un universo sencillo pero con
alma, un lugar donde el jugador quiera pasar horas; iluminación; segunda capa de mejora para las
máquinas más bonitas; ahora todo es lineal; sorpréndeme"*. Cinco batallas, en orden de impacto.

### 1. TIPOGRAFÍA = ALMA (Game/UiStyles.cs)
Las dos fuentes que ya vivían en `Assets/Alkahest/Resources/Fuentes/` entran en juego:
**Cinzel** (lapidaria) para TÍTULOS y **Alegreya** (humanista) para TODO el cuerpo.
`UiStyles.CargarFuentes` las carga UNA vez con `Resources.Load<Font>` y **cae a la fuente por
defecto si devuelven null** (una build sin la carpeta Resources no se rompe: `GUIStyle.font=null`
significa "la del skin", que es el comportamiento de siempre).
**El truco que las aplica a TODA la UI sin tocar los demás archivos**: `UiStyles.VestirSkin()`
escribe `GUI.skin.font = Alegreya`, y un GUIStyle con `font==null` resuelve contra el skin AL
DIBUJAR -- así heredan JournalHud (que construye sus estilos copiando `GUI.skin.label`), OrdersHud,
HintSystem, FlaskHud, DevPalette y cualquier HUD futuro. Los cuerpos suben un punto (Alegreya
tiene la x más pequeña). `UiStyles.Espaciar()` da tracking a las capitales de Cinzel (cacheado por
cadena: cero allocs por frame).
`VestirSkin` además reestila `textField/textArea/button/box/window` con texturas 9-slice generadas
(carboncillo + filo de latón + sombra interior, `HideFlags.HideAndDontSave` para que sobrevivan a
la recarga de escena) y pone el **caret de oro** (`GUI.skin.settings.cursorColor`).

### 2. EL BAUTIZO CON ALMA (Game/NamingUi.cs, Game/JournalHud.cs)
La ventana pasa de `GUILayout.Window` con skin del sistema a **RITO**: `UiStyles.PanelRito`
(vitela ahumada en 10 bandas de degradado + `MarcoLaton` de doble filete con cantoneras + sombra
proyectada), título **"B A U T I Z O"** en Cinzel con filete de rombo, **la muestra del material a
92 px** generada con su FIRMA VISUAL REAL (`FirmaVisualFabrica`, la misma que pinta redomas y
frasco -- con su patrón y su animación), la firma descrita en palabras ("violeta, laberinto vivo,
borde con halo"), el campo con foco automático, la línea ceremonial *"El nombre que le des lo verá
todo el taller"* y **Enter bautiza**. VISTO Y PROBADO EN VIVO: bautizado "flor de niebla" (azoth) y
"arena solar" (una base de la hornada).
El diario habla el mismo idioma: tapa con `MarcoLaton`, portada y pestañas en Cinzel espaciado,
papel de vitela oscura (0.30 -> 0.196: era la superficie más clara del juego y deslumbraba en una
partida en penumbra).
Título y desenlaces de `DayCycle` también en Cinzel espaciado, con filete de rombo.

### 3. ILUMINACIÓN DE ÁNIMO (Sim/SimRenderer.cs, Game/WorkshopBackdrop.cs, máquinas)
- **Tinte global**: `SimRenderer.TinteGlobal` multiplica el sprite de la sim en la GPU (coste cero
  por celda, `ComputeCellColor` intacto). Segunda pasada: de neutro-frío a **sesgado en
  temperatura** (0.930/0.845/0.775) -- con un tinte neutro el taller seguía leyéndose gris lavanda.
- **Halos**: `MaquinariaSprites.Halo()` (radial, caída ^2.2) + la clase `Luz` (crear/intensidad/
  latir con desfase; cero allocs por frame) + `SombraSuave()`/`Sombra()`. Son GameObjects HIJOS de
  cada máquina, así que la mudanza los arrastra sola (regla 36). Fuentes reales: rescoldo y cámara
  del **crisol** (siguen la MISMA intensidad que dibuja las brasas: imposible que el halo diga
  "encendido" con el hogar negro), **brasero** cuando arde de verdad, **lámpara del banco de
  chispa** (la única luz FRÍA del taller, apagada si no conduce: la ausencia sigue siendo el dato),
  **vidrio de la columna** (verdosa, constante y floja), **hogar del ensayo**, **destellos de las
  redomas** del estante. Sombras propias bajo crisol, brasero, prensa, chispa, ensayo y columna:
  las estaciones se APOYAN en el suelo en vez de flotar.
- **La pared**: el fondo era un color plano casi negro (herencia del "cuarto íntimo" del playtest
  21) y con el taller grande eso se leía como TELÓN -- las máquinas flotaban en un vacío. Ahora,
  DENTRO del rect real del cuarto (`CuartoX0..X1/Y0..Y1`), hay sillería a soga corrida con junta,
  bisel y pátina por pieza, zócalo, cornisa, **hornacinas** y el **rebote de la fragua** anclado a
  `SimLevelBuilder.CrisolX`; fuera, roca profunda con veta. Todo con el mismo troceo por corrutina
  de siempre.

### 4. SEGUNDA CAPA DE LAS MÁQUINAS
Sombras propias en todas las estaciones + los halos de arriba (que es lo que le da volumen al
latón ya existente). NO se rehízo geometría de mampostería (la lógica de tallado no se toca).

### 5. ROMPER LA LÍNEA (Sim/SimLevelBuilder.cs, `AdornarCuarto`)
Sin mover NINGUNA ancla de estación (hay réplicas de red y registros que dependen de ellas) y sin
tocar el suelo BAJO las estaciones (sus `TallarEnPlano` asumen `baseY`):
- **Terrazas**: el método descubre los huecos libres solo, preguntando a `ObraDelTaller` qué
  columnas están ocupadas, y talla peldaños de 2-4 filas en cada costura entre estaciones.
  *(Medido jugando: con el taller grande del playtest 27 los huecos son de 4-10 celdas, no de 20 --
  por eso el margen bajó de 3 a 1 y el hueco mínimo de 12 a 4; con los valores iniciales no salió
  ni una terraza.)*
- **Pilastras colgantes** con ménsula desde el techo en las cuatro costuras de zona
  (`PilastraColumnas` = 182/236/292/350), dejando >40 celdas de vano libre para volar.
- **Arco de medio punto** sobre la boca del pasillo a la Tolva (perfil tabulado: este archivo NO
  tiene `using UnityEngine` a propósito y no lo gana por ocho raíces cuadradas constantes).
- Las hornacinas del fondo van en los PUNTOS MEDIOS entre pilastras (209/264/321): pilastra,
  hornacina, pilastra, hornacina = una crujía.

### Lo que SOLO se vio jugando (regla 52, tres correcciones de esta ronda)
1. **Las hornacinas no se veían**: estaban a la altura de las estaciones (que miden 20-35 celdas),
   o sea tapadas por las máquinas. Subieron a la pared libre de arriba.
2. **Y entonces se veían DEMASIADO**: a 0.34 de brillo eran rectángulos negros que parecían
   textura que falta, y encima caían justo bajo una pilastra ("una bandera colgada del techo").
   Un hueco se lee por el CONTRASTE DE SU CANTO, no por ser negro: 0.72 de fondo, 1.25 de canto.
3. **El Enter del bautizo no llegaba**: la comprobación vivía solo en `OnGUI`, antes de
   `GUI.Window`; con el campo enfocado IMGUI entrega el KeyDown DENTRO del ámbito de la ventana.
   Ahora se comprueba en los dos sitios.

### Estado de verificación
Compilado en el Unity real (0 errores / 0 warnings, consola limpia tras `Clear`+Ctrl+R), escena
`AlkahestLab` regenerada y JUGADA: título, taller, hornada completa del crisol con su luz, bautizo
por Enter y por botón, y diario abierto con las 4 pestañas. 5 ciclos de desplegar-mirar-corregir.

**Lo que sigue debiendo** (honesto, para la ronda siguiente): las máquinas siguen siendo siluetas
oscuras con filos de latón -- les falta pátina y remaches DENTRO del sprite (esta ronda se quedó en
sombra + luz); el suelo del cuarto es una losa lisa de 218 celdas y las terrazas solo pueden actuar
en las costuras (romper la línea de verdad pide mover anclas, que estaba prohibido); y no hay
partículas de ninguna clase (chispas del banco, motas subiendo del crisol) -- es el siguiente
escalón barato de "vida" en la escena.

---

## Playtest 26 → EL TALLER QUE SE EXPLICA SOLO (dos encargos Sonnet + verificación con capturas por Fable en el PC de Cesar)

**El feedback de Cesar (playtest 25)**: "no queda claro dónde van las cosas, dónde se reciben,
si debería meter limo en todas... hay que trabajar mucho en las máquinas para que se entienda su
funcionamiento... el inicio tiene que mejorar SIN CARTELES... para el público de a pie, si se
ocupa más espacio no pasa nada... los consejos están pasando muy rápido y aturde, tampoco sé si
los puedo relanzar y desapareció lo de poder saltar a otro." Contrato:
`docs/CONTRATO_LEGIBILIDAD.md`.

### La gramática visual (toda máquina, presente y futura)

EMBUDO de latón = entrada de materia (una familia de sprites, se aprende una vez). BRASERO de
hierro con rescoldo = entrada de combustible (la ÚNICA otra boca; forma/altura/color distintos).
CUBETA ENMARCADA = aquí queda el resultado. EL VERBO EN EL CUERPO: chimenea con bocanadas SOLO al
quemar combustible; mandíbulas+husillo; electrodos+arco+lámpara; vidrio con marcas; pedestal
ceremonial del Ensayo. Y el **AFFORDANCE GLOW**: a ≤10 celdas con el frasco cargado de M, cada
boca PULSA solo si M le sirve (crisol: líquidos/polvos; brasero: combustibles; prensa: lo que
compacta o revienta; chispa: variantes base; ensayo: pedido activo). LA DUDA DE CESAR ("¿meto
limo en todas?") LA CONTESTA EL TALLER SEÑALANDO, sin un cartel. Helper único compartido
(sondeo 0.25s + seno sobre SpriteRenderer.color, cero allocs).

### La línea del taller (el plano nuevo)

El cuarto crece a la izquierda (CuartoX0 248→232, ancho 126). De izquierda a derecha = el proceso
entero: FUENTES (caños agua+limo, cada uno con su PILA de recogida) → CRISOL (brasero|panza+
embudo+chimenea) → PRENSA → COLUMNA de vidrio → BANCO DE CHISPA → ENSAYO → pasillo → TOLVA.
Crudo → transformar → forzar → observar → revelar → examinar → entregar. Holguras 10/10/9/13
(≥8). TODA la mampostería la talla ahora SimLevelBuilder vía los `TallarEnPlano` estáticos de
cada máquina (regla 47: una sola fuente de verdad del plano; el auto-tallado en Init del playtest
25 se retiró). El caño de limo ganó VOLADIZO PROPIO (Dispenser con alcance por instancia, 12 vs
5): sin él, ambos chorros caían por la misma columna y el limo desembocaba en la pila del agua.

### Consejos que no aturden

12s por consejo (antes 8-9); **N** = siguiente (sin marcar como leído lo saltado); **H** =
ocultar/mostrar; contador "consejo 3/10" en la placa; sección **CONSEJOS** nueva en el diario
que lista los ya mostrados para releer (el hook `PistasMostradas` del playtest 10 POR FIN con su
consumidor); la placa se calla con el diario abierto. Conflicto cazado por el encargo H: la N de
DevPalette (paso de tick) ahora exige la paleta abierta.

### Lo que SOLO se vio jugando (verificación con capturas — Cesar prestó el PC)

Fable jugó la build real por computer-use (capturas + WASD + frasco) y cazó DOS problemas
invisibles en el código:
1. **La inundación**: 20 segundos de grifo abierto sobre el suelo corrido de la línea = medio
   laboratorio bajo limo (las cubas hondas del taller clásico lo contenían; el suelo abierto no).
   Fix: **LA RACIÓN** — los caños del laboratorio sirven ~45 celdas por apertura y se cierran
   solos, chapa "· servido — E para más" (regla 43: un autocierre sin rótulo parece un grifo
   roto). Los grifos clásicos siguen infinitos (racionCeldas=0 default).
2. **El limo camuflado**: su pardo (94,86,72) se confundía con la piedra A ESCALA DE JUEGO — un
   lago entero se leía como suelo. Fix: verde oliva turbio (88,96,52) + jitter 16.
Verificado en vivo tras los fixes: ración contenida en su pila, oliva inconfundible, "hirviendo"
en el crisol al recibir limo, frasco aspirando (35/900 · "limo"), y el AFFORDANCE GLOW del
embudo del crisol LATIENDO con limo en el frasco mientras prensa/chispa/ensayo permanecían
apagados. Diario: 4 pestañas (LEYES · SUSTANCIAS · PROCEDIMIENTOS · CONSEJOS) confirmadas en
pantalla. Compilado 0 errores / 0 warnings.

**Preguntas abiertas**: ¿45 celdas es LA ración correcta? ¿el glow se entiende como "esto le
sirve" o hace falta que el pulso sea más obvio? ¿la columna de vidrio se lee como vidrio (hoy:
dos muros de Crystal verde)? ¿el Ensayo se distingue lo bastante del resto como EL examen?

---

## Playtest 27 → EL TALLER GRANDE (OPUS 5 con ojos propios) + español latino + fixes de Cesar

**El veredicto de Cesar sobre el 26** (resumen; literal en docs/CONTRATO_TALLER_GRANDE.md §1):
máquinas cajita, embudos falsos flotantes, "cargadme combustible" sin sentido al inicio, la
columna una "escalera sin terminar" SIN verbo y con muros REACTIVOS (Crystal + Azoth: bug real),
el crisol haciéndolo todo rápido y escupiendo 4 colores por pasada ("si me salen 4 cosas casi de
golpe no entendí nada"). Y: "quiero que te apoyes en Opus 5, permitiendo que ÉL VEA... su
capacidad de construir cosas bonitas y útiles es mejor que la tuya".

**Fixes directos de Fable (los "evidentes")**: (1) el pulso de affordance por PROXIMIDAD se
APAGA (`MaquinariaSprites.AffordanceGlow.ProximidadActiva=false`, clase conservada: su destino
aprobado es latir cuando la máquina TRABAJA); (2) LA OBRA DEL TALLER NO CEDE AL CINCEL
(`SimLevelBuilder.ObraDelTaller` + registro en cada Tallar* + guarda en Cincel.TallarTick —
Cesar se llevó mampostería de una estación creyendo tallar roca); (3) caño de limo SIN estirar
(la separación de chorros pasó a geometría de la estación de fuentes).

**OPUS 5, tres ciclos de mirada real en el PC de Cesar** (desplegó, compiló, jugó, capturó,
corrigió — reporte completo en el propio código): cuarto 218x73 (CuartoX0 140, CuartoY1 240);
huellas 6-20x más grandes (crisol 15x6→37x24 con 117 celdas de cubeta; prensa 31x23; columna
23x42 con VIDRIO visual y muros de PIEDRA inerte + archivo nuevo Game/ColumnaEnsayo.cs con su
verbo "deja caer y observa"; chispa con ampolla de filamento; ensayo hecho ALTAR con dosel);
fuentes con machón de piedra que separa los dos chorros y pilas de 70 celdas; embudo TALLADO en
piedra solo donde se vierte de verdad, bandejas abiertas donde se deposita. **EL CRISOL POR
HORNADAS**: en reposo no empuja temperatura (cascada estructuralmente imposible); E enciende UNA
hornada de ~10s con progreso visible; el resultado REPOSA hasta recogerlo;
recoger-y-volver-a-pasar es EL gesto. **Extracción por temperatura**: `Universe.ExtraccionRaw`
(5 bandas por seed), una hornada de limo saca SOLO la base más alta que quepa en la temperatura
actual — con fuego bajo siempre la primera (garantía del solver, G1-G4 nuevas), mejores
combustibles = otras arenas (la intuición de Cesar confirmada como diseño). Combustibles
165..190 raw. `ProcessLimoSeparacion` retirado de SimStepper (regla 15): el limo ya no se separa
solo en el mundo. Lo que Opus VIO y corrigió: hierro invisible sobre negro, brasas como grava,
brasero leyéndose como de la prensa, mandíbula "martillo aparcado en el techo", y sus propios
nudillos de columna reinventando la "escalera" que Cesar odió — tres ciclos hasta pasar su
propia mirada.

**Español latino (barrido Sonnet)**: 21 archivos revisados, 16 fixes en 5 (vosotros/os/vuestro/
imperativos -ad → tuteo latino: "Sepárame el limo: tráeme una sola de sus arenas", "El Maestro
te asciende..."); comentarios de código intactos (documentación interna). Guion de pistas
REESCRITO por Fable para el modelo de hornadas (12 pasos ejecutables).

**Estado de verificación**: Opus compiló y jugó su parte (0 err/0 warn, 3 ciclos); el delta
posterior (guarda anticincel, textos, pistas) quedó desplegado con sintaxis verificada pero SIN
compilar — el puente MCP de Unity cayó con Cesar fuera. PRIMER PASO al volver: foco a Unity,
Ctrl+R, consola.

## Playtest 28 → EL TALLER COMPARTIDO (POC multiplayer, 4 jugadores)

Mandato de Cesar (saliendo de compras): "prepara una prueba multiplayer; el juego se dijo para
3, si es posible habilítalo para 4; cada jugador debe distinguirse con un cambio de color; lo no
especificado decídelo tú". Contrato: docs/CONTRATO_MULTI_POC.md. Construido por Opus (sin ojos:
sin Unity disponible, disciplina de API calcando el template FriendsLoop).

**Arquitectura**: sim SOLO en el host; clientes en ESPEJO (`AlkahestSim.ModoEspejo`, sin
stepper) sincronizado por chunks RLE (~5Hz, mensaje "AlkChunks", snapshot con LA SEED en
cabecera al conectar — el espejo crea el mismo universo y luego recibe deltas; chunk de piedra
= 6 bytes, snapshot completo 6-15KB); avatares por prefab generado en editor
(OwnerNetworkTransform + PlayerIdentity del template + `Net/AprendizNet.cs` con color replicado
— PALETA: dorado anfitrión / azul cielo / verde / magenta); frasco de invitados con PREDICCIÓN
local + reenvío de pintura al host por lotes (desviación razonada del contrato: sin predicción,
el frasco duplicaba materia); máquinas/encargos/cincel/mudanza SOLO anfitrión (división de
trabajo del POC: invitados acarrean, el anfitrión hornea); escena MULTI aparte (menú
"Alkahest/2. Generar escena Lab MULTI" + "4. Build MULTI Windows") — la escena clásica intacta.
4 jugadores (lobby maxPlayers=4).

**PENDIENTE DE COMPILAR** (los asmdef cambiaron: reimport probable). Los `// DUDA-API:` están
concentrados en la capa CustomMessagingManager de SimSync.cs (lo único sin calco del template);
si algo no compila, empezar ahí. **La prueba**: build MULTI, abrir el .exe DOS VECES con
`-transport local`, ANFITRIÓN en una y UNIRME local en la otra: dos imps de colores con nombre,
el agua del host apareciendo en el cliente, el invitado acarreando limo a la boca del crisol.
Con amigos: sin `-transport`, lobby de Steam (invitación por overlay).

---

## Playtest 29 (parcial, mismo commit que los fixes del multi) → GRAVEDAD CON COHESIÓN

GO de Cesar tras la evaluación de pros/contras; su pregunta exacta — "¿todo pixel va a necesitar
una base o habrá un principio de cohesión que permita construir con apoyos sensatos?" — se
respondió eligiendo COHESIÓN: un sólido se sostiene si tiene apoyo debajo O si a ≤K celdas en
horizontal, a través de materia sólida CONTINUA (StaticSolid o Powder; un hueco corta la viga,
los líquidos no transmiten carga), alguien tiene apoyo directo. K fijo por material (vocabulario,
regla 17): cerámico 8 > compacto 6 > recocido 5 = cristal 5 > hielo 4 > templado 3 (frágil hasta
en esto). La PIEDRA y la obra del taller jamás caen. Caída recta 1 celda/tick, solo a VACÍO (los
líquidos sostienen: el hielo sigue flotando en el agua); sin deslizamiento lateral (un sólido no
es un polvo). Coste: solo chunks despiertos, escaneo acotado por K; un sólido asentado duerme
como siempre. Regla 7 de CLAUDE.md matizada. Consecuencia de juego deseada: construir pasa a
tener INGENIERÍA — vigas y voladizos sensatos sí, alfombras flotantes no; lo fundido vertido en
el aire ya no queda como calcomanía. También en este lote: fix del arranque multi (registro
ÚNICO del prefab de avatar — el doble registro editor+runtime hacía que NGO invalidara todo y
ANFITRIÓN "no hiciera nada") y ANFITRIÓN que cae solo a taller LOCAL con aviso si Steam no está
abierto. Máquinas como objetos de red (mudanza para invitados): pospuesto a la siguiente ronda
por decisión de Cesar ("con que el host pueda ahora me basta").

**Playtest 29, segundo lote (bugs del playtest de Cesar)**: mudanza sin huellas (borrar mampostería
vieja + `ActualizarObra` con handle — antes los muros viejos quedaban REGISTRADOS como obra y el
cincel se negaba a quitarlos: piedra fantasma indestructible); Columna y Ensayo movibles (las 5
estaciones); sombra de agarre alineada a la huella real; zoom con rueda (techo compartido con
Tab, vista actual = máxima cercanía); `steam_appid.txt` junto al exe de la build MULTI (sin él
SteamAPI fallaba con Steam abierto — el "no estoy conectado" del reporte); aviso de caída a local
legible, con causas reales, caducidad y botón. Builds: la buena es `ChaosAlchemyMulti`
(`ChaosAlchemyDemo` es del template, borrable); en el editor manda la ESCENA ABIERTA (menú 2
abre la MULTI). PENDIENTE al cierre de la ronda: compilación del lote 2 en Unity (desplegado
completo; Cesar no estaba frente al PC — recompila al enfocar Unity).

**LA VÍSPERA (playtests 31-32, ronda nocturna autónoma)**: resumen operativo — tramo 1 (Sonnet x2
paralelos): máquinas en red con réplicas+mudanza de invitados, fuego real en brasero, vapor,
ALAMBIQUE fabricable con cerámico (primer instrumento fruto del progreso), disolución visible,
estante de redomas; tramo 2 (Opus con ojos, 6 iteraciones): tipografía Cinzel/Alegreya, bautizo
como rito, luz de fragua, pared con sillería, terrazas. Verificado jugando; builds:
ChaosAlchemyMulti (final) + Respaldo_TramoA_Multi. Fix definitivo: NetworkConfig null en editor.
Deuda elegida a consciencia (3am, estabilidad > features): PARTÍCULAS (chispas del banco, motas
del crisol, polvillo de la prensa) — primera tarea de la próxima ronda. Los commits 31/32
comparten archivos: compilable garantizado solo tras subir ambos en orden.

---

## Playtest 33 → CIMIENTOS Y ARQUITECTURA (los "ajustes finales" de Cesar tras la demo)

**Los bugs de cimientos (Sonnet, causa raíz confirmada)**: las terrazas del 32 se tallaban ANTES
de que las estaciones se registraran en ObraDelTaller (AdornarCuarto corre en AlkahestSim.Start;
las máquinas se registraban recién en su Init, del bootstrap) → el sistema de protección miraba
un registro VACÍO: terrazas coladas bajo las huellas (máquinas "enterradas", suelo con "formita
rara"). Fix: registro desde TallarEnPlano/génesis + Init RECLAMA el handle (HallarObraExacta).
El "rastro de bedrock EN EL SUELO" al mudar: sin snapping vertical, al soltar en otra cota la
fila de suelo propio vieja quedaba huérfana → PLATAFORMA SOBERANA (cada estación aplana
huella+2 antes de tallar; al mudarse RestaurarSueloBase devuelve el sitio al nivel de la losa).
El foco flotante del banco: +12f mágico sin relación con el travesaño (+4f) — regla 47 otra vez;
ahora una sola fuente de verdad geométrica + cordón visual. Y el AVISO DE BAUTIZO: descubrir un
innominado dispara el banner "ALGO NUEVO — pulsa T para bautizarlo" por la misma cola FIFO de
"LEY DESCUBIERTA" (un aviso por material, Cinzel, respeta EscribiendoTexto).

**La arquitectura (Opus con ojos, 3 ciclos jugando)**: el cuarto crece (140..378 × 168..262,
bóveda hasta 274): SEIS casquetes parabólicos con nervios sobre pilastras (arcos fajones), TRES
claraboyas ciegas con haz frío (la única temperatura opuesta al fuego), tres aparejos de
sillería por zona, dovelas, vigas cruzadas, cadenas. NUEVE BALDAS físicas de piedra con ménsulas
inclinadas de latón (los polvos/sólidos se APOYAN de verdad — cohesión regla 7): la "línea sobre
el horno" que amaba Cesar ahora es una galería continua de 33 celdas, más repisas en las alturas
para EXHIBIR hallazgos. ZONIFICACIÓN del interiorista: húmeda (140..171) → fuego (crisol 194 +
alambique) → fuerza (prensa 246) → ESCALINATA-umbral → alcoba de observación (columna 284 +
chispa 324, juntos bajo la bóveda alta y sus claraboyas) → atrio → ENSAYO (362) casado con la
Tolva en el vestíbulo de entrega — el recorrido crudo→transformar→observar→examinar→entregar SE
SIENTE. LA LUZ DEJÓ DE SER STICKER: diagnóstico medido (disco de 46 celdas sobre máquina de
37×24); ahora LuzDeMuro recortada a la mampostería real con corte duro arriba ("el contenedor
brillando sin incluir el techo", literal de Cesar), halos 46→15/11, caída 2.2→3.6 (arregla de
paso lámpara y redomas), latido atado a la intensidad real del hogar. El vestíbulo de la Tolva
ganó sillería (28% más oscura) al entrar en cuadro por primera vez.

**La cota por zona (Fable, cierre de la deuda del arquitecto)**: `BaseYDeEstacion(anclaX)` — la
alcoba de observación vive a +6 (sobre la escalinata), el atrio del Ensayo a +3; resuelta POR
ANCLA, no por tipo (regla 47); las plataformas soberanas hacen el resto, y al mudar, la estación
aplana a la cota donde cae.

**Deuda declarada y viva**: PARTÍCULAS (motas del crisol, chispas del banco, polvo en los haces
de claraboya — "el haz de luz PIDE partículas"), segundo registro de objetos pequeños en la
pared izquierda (herramientas colgadas, hornacinas con cosas), ménsulas de las baldas cortas
aún un punto "a caballete".

---

## Playtest 34 → LIMO PRIMORDIAL: el rebautizo, los anclajes y el taller que respira

**El juego se llama LIMO PRIMORDIAL** (decisión de Cesar; título + "Todo lo que existe
desciende del limo."). **EL SISTEMA DE ANCLAJES** (la joya — Cesar diseñó sin querer la
CONSTRUCCIÓN del juego): cuadraditos de latón 2x2 IMovibles que SON piedra tallada (los sólidos
con gravedad se apoyan gratis), sustituyen bedrock al colocarse (esquinas perfectas) y no dejan
hueco al quitarse de piedra original; las baldas adelgazan a 1 fila (≤12 celdas, objetos Balda
IMovibles con el cuadrito dibujado en los extremos), y hay un DEPÓSITO de 6 anclajes de sobra
junto al estante. Multi: CERROJO de mudanza (quien agarra bloquea; otros ven "lo está moviendo
otro alquimista" y la posición final — decisión de costo de Cesar). Redomas y Alambique ahora
móviles (el alambique solo se registraba en Mudanza al completar la construcción — nunca en obra
pendiente; + un handle huérfano de RegistrarObra en vez de ActualizarObra). **MUERTE DEL
AUTO-PATENTE DE 1 PASO**: cadenas de 1 paso jamás ofrecen patente; el BAUTIZO sale al frente si
el resultado es innominado; patentar exige ≥2 pasos Y todos los ingredientes bautizados
("bautiza sus ingredientes para poder patentarlo"); el aviso de procedimiento abre el diario EN
la pestaña PROCEDIMIENTOS. **EL ESPACIO (Opus, 4 ciclos)**: cuarto 80..378 × 136..262 (+25% ancho,
+34% alto — crece hacia ABAJO para ganar aire de cabeza), bóveda FUERA del encuadre inicial (se
descubre volando, verificado); plano central de Cesar: transformar-izquierda (crisol 102, prensa
158) / FUENTES AL CENTRO (isla con doble machón, agua 205 + limo 222) / observar-derecha
(columna 260, chispa 302) / ensayo 362 con la Tolva; FONDO ÚNICO (el aparejo de la prensa,
contraste a la mitad) cubriendo TODO el mundo con penumbra por profundidad (romper bedrock ya no
muestra negro), ventanas con marco que el trabado no cruza, cadenas colgando de vigas DEL FONDO,
capiteles que flotaban 25 celdas (bug del 33) aterrizados, HazClaraboya retirado (regla 15, no
estaba listo — palabra de Cesar). Deuda: atrio desnudo, hornacinas del piso alto bajo pilastras,
réplicas de red de Balda/Anclaje dicen "aparato" (MaquinaReplica fuera de alcance del encargo),
depósito de anclajes sin verificar contra mampostería en el editor. RUMBO STEAM NEXT FEST
OCTUBRE 2026 (registro cierra fin de agosto): avance estimado 60% — falta Steamworks propio
(appid real, página, cápsulas — trámite de Cesar), pase de audio, y 30 min jugables sin tutor.

**Playtest 35 (los ajustes del 34)**: los dos "no aparecen" eran espejos exactos — StorageRack/
Alambique solo se spawneaban en TrySpawn (un jugador) y los soportes con un gate `EsServidor`
que en la escena clásica (sin SimSync) es false: cada cosa vivía solo donde el otro modo la
echaba de menos; gate correcto `EnEscena && !EsServidor`. Grifos y PILAS ahora se mudan POR
SEPARADO (Game/Pila.cs nuevo, patrón soberano; la guía contiene el voladizo real del caño — de
paso un bug dormido en TamanoMundo) y ambos entran al registro de red con cerrojo. El estante
iba a aparecer FLOTANDO SOBRE LAS PILAS al moverse las fuentes al centro (EstanteX0=PilaAguaX0,
regla 47 de manual): ahora a la izquierda en alto sobre el crisol (88..124, sobre el domo del
alambique, derivado no adivinado) como sugirió la captura de Cesar. F9 cierra DEL TODO el panel
de sesión (quedaba una ventanita sobre el FRASCO; recordatorio de 3s y silencio). Réplicas con
nombres reales ("balda", "el alambique"...) — deuda: sprites de réplica de Rack/Alambique/Pila
caen al genérico (MaquinariaSprites quedó fuera del alcance del encargo). Verificado en un
jugador: 17 baldas + 6 anclajes + 2 pilas + estante + alambique en jerarquía, 0 errores.

**Playtest 36 — PARIDAD MULTI PROFUNDA** (reporte de Cesar probando con un invitado real; captura
del lado invitado): cuatro causas raíz. (1) Réplicas blancas: ConstruirVisualEstatico no tenía
casos para Rack/Alambique/Pila → default sin tintar; ahora piezas reales tintadas. (2) El
empapelado de chapas: MaquinaReplica.OnGUI dibujaba TODAS las chapas incondicionalmente a
opacidad plena (23 réplicas); ahora por cercanía, y Balda/Anclaje sin chapa jamás (mobiliario:
la forma es el rótulo — documentado para que nadie lo "arregle" de vuelta). (3) El invitado sin
menús: TrySpawnRed rama invitado terminaba en un return mudo tras el avatar; Y
SubstanceKnowledge.Update se apagaba ENTERO con Stepper null (el gate era correcto solo para
ConsumeEvents y tapaba todo el método). Nuevo Net/SaberSync.cs (autoadjunto en SimSync.Awake —
sin regenerar escena): descubiertos, nombres (FixedString128, upsert), leyes presenciadas,
encargos activos + Favor replicados; late-join recibe todo (NetworkList sincroniza estado
completo al spawn); bautizo de invitado por ServerRpc con eco de autoridad; OrdersHud con rama
read-only replicada; HintSystem/NamingUi/JournalHud/OrdersHud spawneados para invitados. (4) El
"a veces no se ve el chorro": la ruta RPC→Paint→dirty estaba SANA (auditada y descartada); el
bug real era inanición en DifundirChunksSucios (barrido circular ciego con presupuesto 96) —
ahora dos pasadas: prioridad a chunks a ≤60 celdas de CUALQUIER avatar conectado, resto con el
cursor de siempre. PENDIENTE DE VERIFICAR por Cesar: prueba real de dos ventanas (la build no es
una app registrable para el control remoto — el editor solo puede ser un lado).

**Playtest 37 (hotfix del 36)**: el 36 no compilaba en el PC de Cesar. Los menús de editor SÍ
corrían (Unity mantiene los ensamblados del último compile bueno), lo que despistaba. Se montó
EL COMPILADOR UNITY-FIEL EN EL SANDBOX (regla 53 nueva): DLLs de la build real + dotnet csc —
encontró al primer intento el único error real: CS0030 en SaberSync:340, un cast inválido
`(string)FixedString128Bytes` en la comparación de cambios de encargos. Fix: comparación por
`Equals` sin alloc + `RecortarDescripcion` como único punto de verdad del recorte a 120 chars
(si el volcado y la comparación recortaran distinto, un encargo largo se re-difundiría cada
sondeo para siempre). Verificado: 0 errores contra las DLLs reales.

---

## Playtest 38 → EL INFORME DEL MOTOR + SEMILLA CERO v2 (ronda de diagnóstico, sin código de juego)

Cesar pidió diagnóstico frío del motor ("¿en qué % del máximo estamos? ¿qué tan espectacular
puede ser y a qué costo?") antes de congelar Semilla 0. Se construyó EL BANCO HEADLESS
(`Tools~/BenchSim/Harness.cs`, corre el SimStepper real compilado contra las DLLs de la build —
regla 53): peor caso medido = medio mundo de agua (74.000 celdas activas) a 5,5 ms/tick de media
y 11,6 de pico contra 33,3 de presupuesto. En juego real usamos el 2-5%. Conclusión: el cuello
de botella del espectáculo no es el algoritmo — es que no le hemos pedido espectáculo. Informe
completo con menú de mejoras y costes en docs/INFORME_MOTOR.md; paquete recomendado: partículas
desprendidas + pátina/manchas + gases con corrientes (+~2 ms peor caso) ANTES de Semilla 0;
cuerpos rígidos estilo Noita: veredicto NO (caro, rompe supuestos del sync, no es nuestro juego).
SEMILLA CERO v2 en docs/DISENO_SEMILLA_CERO.md: las cinco sugerencias externas aceptadas y
curadas (bautizo ganado con el Maestro exigiéndolo, fracaso forense ASCENDIDO A LEY regla 54,
desbloqueos como preguntas literales, currículo de 4+1 ideas, final abierto con el anzuelo del
vasito del alambique + contador de autonomía como métrica reina). Orden acordado a validar por
Cesar: motor → semilla → playtest.

---

## Playtest 39 → LA RONDA DE MOTOR (el fuego como proceso + la capa de partículas)

El GO de Cesar al paquete del informe (partículas + pátina + gases + reacciones dirigidas) MÁS
la extensión acordada: combustión persistente parametrizada y brasas. Sin cuerpos rígidos
(decidido). Contrato congelado en docs/CONTRATO_MOTOR.md, dos encargos Sonnet en paralelo con
archivos disjuntos, integrados y auditados por Fable.

**COMBUSTIÓN PERSISTENTE (encargo S)**: el combustible ES la celda que arde. MaterialDef gana 7
parámetros (reserva/ritmo/calor/humo/propagación/lengua/residuo); el estado "ardiendo" vive en
`aux` (Liquid: 7 bits, preserva el bit de flujo; Powder: byte entero). El Fire de siempre pasa a
ser LA LENGUA VISIBLE que escupe la celda ardiendo, no el consumidor. Ignición (temp o contacto)
ahora PRENDE la celda en vez de transformarla; el agua sigue mandando (mismos criterios de
extinción, con chorro de Steam). Aceite = patrón oro: un charco arde ~32s/celda consumiéndose
desde el borde (~90-100s la piscina entera, verificado con diagnóstico confinado tick a tick).
Calcinados combustibles de la seed conectados por primera vez a la ignición REAL (antes solo el
Crisol los conocía como abstracción). Guardas críticas: ApplyPhase/TryIgnite no re-maximizan la
reserva de una celda ya ardiendo (fuego eterno), y regla 55b: la celda ardiendo se mantiene
despierta ella misma.

**BRASA (MaterialId 58, Count 59)**: la vejez del fuego. Sólido-polvo rescoldo (ámbar apagado
con pulso, jamás blanco), vive 8-12s (jitter sal 521), emite calor modesto, REENCIENDE vecinos
inflamables al 8% por paso (sal 523), agua → Ash + Steam al instante, decae a Ash. Residuo de
los combustibles sólidos; el aceite muere en nada (líquido). El brasero del Crisol ahora es
HONESTO: sus celdas de combustible arden de verdad y dejan brasas reales en el cesto (la lógica
de tiers sigue siendo la autoridad química). Evento nuevo `Ember` (al final del enum, sin
renumerar).

**GASES CON CORRIENTES**: deriva térmica determinista (el lateral se sesga al vecino más
caliente) + BOLSAS bajo techo (60% de presión lateral vs 35% a cielo abierto, prueba el otro
lado si la bolsa cierra). La "vida extra embolsado" del contrato se integró como MEDIO
DECAIMIENTO bajo techo directo — la versión original (+3 por movimiento) era matemáticamente
inmortal y motivó la regla 55a.

**PÁTINA**: `CellGrid.patina` (byte, un canal con doble lectura: mojado ≤90 que se seca, tizne
hasta 220 casi permanente), escrita y leída SOLO por SimRenderer (barrido de 12 filas/frame):
tizne junto a Fire/Brasa, tiznado lento bajo humo pegado a bóveda, mojado junto a líquidos.
Cero coste en el tick, determinismo intacto, y en multi cada cliente la genera de lo que VE —
invitados incluidos, sin un byte de tráfico. La pátina de una celda que DEJA de ser sólida se
limpia sola en el barrido (cincel/mudanza no necesitan saber que existe).

**REACCIONES DIRIGIDAS**: `SimStepper.RegistrarZonaInteres` + máscara por chunk; MaybeReact 1/2
en las cubetas de todas las estaciones (registradas por SimLevelBuilder.RegistrarZonasInteres,
invocado desde Crisol.Init), 1/8 en el resto del mundo.

**CAPA DE PARTÍCULAS (encargo F, Game/ParticulasFx.cs nuevo)**: decorativas no-sim (Random de
Unity, client-local en multi, cero tráfico). Ring de 4096 structs preasignados, overlay
Texture2D 768x288 alineado con la textura del mundo (sorting -4), doble lista de téxeles sucios
con swap por referencia, Apply ≤1/frame y solo si hubo cambios. Emisión por OBSERVACIÓN
(touchedTick ventana de 10 ticks + mat/temp, franja de 6 filas/frame alrededor de la cámara,
presupuesto 64 nacimientos/frame): salpicaduras de líquidos al aterrizar, chispas del Fire (su
misma fórmula de color joven/viejo), motas del aire caliente del crisol, nubecitas de polvo,
vaho del Steam. Integración Fable: enganche adicional al ring de eventos NO destructivo
(`LeerEventosDesde`, cursor propio, solo anfitrión/un jugador) para ráfagas episódicas — Ignite
(2 chispas amarillas), Boil (voluta), Ember (3 ascuas, "el último suspiro" que marca rescoldo
reencendible) — y ascuas tenues de las celdas Brasa vivas (4%).

**EL BANCO (con el 6º escenario INCENDIO SOSTENIDO nuevo)**: el sandbox hoy corre ~3,5x más
lento que la máquina que midió el informe (verificado con git stash: el baseline SIN cambios da
7,7/20,9/9,5/7,3/15,2 ms en esta misma sesión). La comparación honesta es relativa: el delta de
TODA la ronda es +2-8% por escenario. Tabla final integrada (media/pico ms): cascada 7,97/11,5 ·
diluvio 21,97/45,6 · incendio 9,69/15,9 · arena 7,64/12,9 · mixto 15,66/21,9 · incendio
sostenido 7,28/10,6 — headroom 1,5-4,8x hasta en este sandbox lento. En el PC de juego el margen
real es mucho mayor.

**Sales nuevas**: 503 (paso de combustión), 509 (extinción), 521 (vida brasa), 523 (reencender),
547 (gas deambular/bolsa). **Deudas**: `Crisol.RefrescarLlamasBrasero` parcialmente redundante
con las lenguas reales (recortable); Freeze/Crystallize/Grow/Dissolve aún sin partícula propia;
el invitado no recibe eventos (sin stepper) — sus ráfagas episódicas salen solo de la
observación continua.

---

## Playtest 40 → SEMILLA CERO (la primera sesión como experiencia de autor)

El GO tras subir el 39: construir DISENO_SEMILLA_CERO.md (las cinco enmiendas) sobre las capas
del motor. Contrato congelado en docs/CONTRATO_SEMILLA.md — que ahora es LA FUENTE ÚNICA del
arco beat a beat (el detalle v1 vivía en la conversación pre-compactación). Dos encargos Sonnet
paralelos (G=guion, M=mundo), integrados y auditados por Fable.

**EL ARCO (Game/SemillaCero.cs, director de beats, solo escena un jugador)**: milagro (primera
hornada a fuego propio, banner con nombre provisional) → "Tráeme 25 de ese... 'sedimento
celeste' tuyo" → "No pienso seguir diciendo 'sedimento celeste'. Ponle nombre." (NamingUi
forzado por el personaje) → el tostado con TRAMPA (banda de calcinación estrecha, el brasero
tier1 se pasa → ceniza + nota forense + "la ceniza también arde, mal, pero arde" → el reintento
se alimenta CON esa ceniza, tier 0.5 = 145 raw) → cuatro preguntas que destapan salas ("¿Puedes
hacerlo MÁS DURO?"/prensa, "¿Por qué esto queda ENCIMA?"/columna, "¿Esto CONDUCE?"/chispa,
"¿DE VERDAD aguanta?"/ensayo) → final abierto ("No necesito nada más por hoy. ...Pero queda
limo.") con el vasito del alambique lleno (gotea desde el minuto 0, nadie lo menciona) y el
CONTADOR DE AUTONOMÍA (log por acción + resumen por minuto + línea en F3): la métrica reina.

**EL MUNDO (encargo M)**: `Universe.SemillaCero = 777002u` (777001 descartada: ganador y
combustible colisionaban en la misma base) + `AplicarOverridesSemillaCero()` post-generación:
extracción base0 a 100 (tier0=120 siempre alcanza, competidores clampeados), banda de
calcinación estrecha 130..170 con sobrecalentamiento→Ash (vía DecidirHornada por tabla, gateado
al modo — el caótico jamás lo ve), color celeste en cascada a los 7 estados derivados, ceniza
combustible tier 0.5 (145 raw, reserva corta). Tapiados de obra sobre las 4 salas (huella
dinámica desde ObraDelTaller, jambas intactas = puerta condenada) con API congelada
`TapiarSalasSemillaCero`/`DestaparSala(sim, 0..3)`; las máquinas tapiadas NO spawnean hasta el
destape (PollDestapesSemillaCero en Bootstrap — cero chapas/glow a través del muro por
construcción). Pantalla de entrada: "SEMILLA CERO — tu primer taller" / seed + "MODO CAÓTICO".
Alambique auto-construido en Semilla 0. NOMBRE PROVISIONAL global (estado+color: "sedimento
celeste", tabla de 8 estados × 12 colores por distancia RGB) y NOTA FORENSE global ("cerca de
~N° se destruye"): las dos excepciones deliberadas que mejoran también el caótico.

**LO QUE LA AUDITORÍA DE INTEGRACIÓN CAZÓ (la costura entre encargos)**: (1) INTERBLOQUEO DURO
en "¿Esto CONDUCE?": el pedido OrderType.Conduce solo lo completaba EnsayoMaestro — cuya sala
sigue tapiada hasta el beat siguiente. Arreglo: testigo MaxConductividadObservada
(BancoChispa→SubstanceKnowledge) y el director completa el pedido cuando la lámpara dicta
sentencia EN EL BANCO. (2) EL TESTIGO FORENSE NUNCA DISPARABA: G lo escuchaba por eventos Boil
de la CA, pero la trampa real es una HORNADA (Crisol.DecidirHornada) que jamás emite Boil —
puente nuevo RegistrarDestruccionPorHornada llamado desde CerrarHornada con la cima real, y el
director vigila el POLVO (la entrada destruida), no el calcinado. (3) El salto escondido del
beat 5.2 (nada de base0 flota; la respuesta es EXTRAER base1, banda 122) cubierto con un consejo
nuevo ("El limo guarda MÁS de una arena..."). (4) "Nuevo universo" del EndScreen apaga
ModoSemillaCero (reintentar mismo universo lo conserva: el arco se puede rejugar).

**LA ARITMÉTICA DEL ARCO, VERIFICADA CON DIAGNÓSTICOS HEADLESS (seed 777002)**: b0 calcinado
CONDUCE pleno (nivel 2 — el jugador ya carga la respuesta cuando llega la pregunta de la
chispa); b1 polvo FLOTA insoluble (dens 19 < agua 36, banda 122 — la pregunta de la columna
obliga a descubrir la segunda arena); TempEnsayo=177: b0 calcinado (umbral 170) MUERE en el
ensayo — el círculo forense se cierra — y b1 calcinado (umbral 188) pasa... solo con fuego
medido de ceniza (145), porque el tier1 (185) FUNDE b1 (fusión 160): la lección del beat 4
reaparece sola en el beat 5.4. Nada de esto es casualidad: es la seed congelada tras medirla.

**Deudas**: el contador de manipulaciones aproxima por ráfagas del frasco (sin tocar
Flask/Crisol); Freeze/Crystallize sin partícula propia (heredada del 39); el playtest con la
gente de la primera prueba (medir minuto 1 + autonomía) es el paso 3 del orden acordado.

---

## Playtest 41 → EL VAPOR VIVO (hervir de verdad, gas con rumbo, color que no miente)

Feedback de Cesar tras el 40: no pudo hervir agua; el polvo celeste "se pone amarillo" al
calentarse; el vapor subía "en vertical perfecta", se estancaba bajo muros, y la animación de
vapor era "muy mala". Contrato en docs/CONTRATO_VAPOR.md con el diagnóstico de causas raíz
hecho ANTES de encargar (regla: no se re-diagnostica en el encargo). Dos encargos SECUENCIALES:
S (Sonnet, sim) y V (Opus CON OJOS en el PC real — Cesar pidió explícitamente que la visión
fuera de Opus).

**LAS TRES CAUSAS RAÍZ**: (1) `DecidirHornada` cortaba con `EsBaseEstado` — el agua JAMÁS tuvo
rama de hornada (la promesa del manual se perdió en la reescritura del pt27). (2) El tinte
térmico del renderer fundía el color al 100% hacia ámbar por encima de raw 150 — a fuego de
brasero cualquier material perdía su identidad. (3) La lateralidad del gas re-sorteaba
dirección CADA tick — temblaba en el sitio en vez de derivar.

**ENCARGO S**: rama "hirviendo" (Water+cima≥boilsAt → Steam; el teatro `VaporPorCeldas` queda
excluido para esta hornada: la cámara entera YA es vapor real). CONVECCIÓN: rumbo/viento
coherente por hash de baja frecuencia (sal 551, `_tick>>4, x>>3, y>>3` — misma celda mantiene
rumbo ~0,5s, bloques de 8x8 comparten corriente: viento, no ruido), ondulación 30% en ascenso
libre (sal 549 — nada de vertical perfecta), deriva térmica degradada a desempate, escape bajo
techo: diagonal-rumbo → diagonal-contraria → lateral-rumbo → lateral-contraria. HALLAZGO de
verificación: el gas nacido por `SetCell` directo (PaintStable/CerrarHornada) llegaba con
aux==0 y moría EN SU PRIMER TICK — mismo bug que ProcessFire pt9; siembra con jitter (sal 553).
Sin ese fix, "hirviendo" habría vaciado la cámara en vapor invisible. MEDIDO: dispersión
lateral de una columna libre +52% (stddev X 1,59→2,41); escape bajo saliente: pico de celdas
fuera de la sombra 19→42 (+121%); banco 6 escenarios: peor caso +3,5% (mixto), sin-gas planos.

**ENCARGO V (Opus con ojos, 2 pasadas desplegadas y capturadas con la MISMA seed 187415343)**:
INCANDESCENCIA en dos capas — brasa ADITIVA (+72R/+30G/+6B escalada; sumar no borra la
diferencia entre materiales) + mezcla ACOTADA a techo 0,45 (a raw 255 sobrevive el 55% de cada
canal), curva ~t^1.5 sin Pow; a 320°C (el caso de Cesar) la mezcla es 0,245: "el azul, al
blanco", nunca "material amarillo". BOCANADAS de chimenea degradadas a acento: alfa 0,70→0,34
con fade de entrada, y tablas por índice de periodo/altura/rizo/deriva (antes las 4 bocanadas
idénticas desfasadas 1/4 = carrusel); se quedan porque la chimenea es el verbo del cuerpo
(pt26) y ahí no nace gas real. VAHO reubicado a la SUPERFICIE del penacho (dentro de la masa
era invisible y gastaba presupuesto): nace solo con aire arriba/al lado, más lento y longevo.
`Alambique.cs` NO tocado: no tenía teatro de vapor — su vapor ES el gas real (verificado el
ciclo completo: columna del crisol → respiradero → "agua destilada: 7"). Falso positivo
cazado: la banda "oliva" sobre el azul caliente era CrystalSeed de una ley de la seed, no
tinte.

**Deudas**: rótulo del alambique dice "llevas 0" cuando llevas cerámico en el frasco (solo
cuenta lo YA vertido — merece distinguir "llevas" de "has vertido"); grep pendiente de otros
consumidores de PaintStable/SetCell con gasLifetime>0 y aux==0; cabina para próximas rondas:
clics sintéticos down+up en el mismo frame SE PIERDEN (usar left_mouse_down→wait→up), verter
= apuntar + Q (no hay right_mouse_down), el panel F3 se desplaza si el material tiene
descripción. Regla 53 recordada: al re-stagear DLLs, renombrar la de espacios
(SteamTransportNGO.dll) — los 4 errores CS2001 de esta ronda fueron eso.

---

## Playtest 42 → HOTFIX MULTI: el "StartHost devolvió false" mudo ahora se explica solo

Captura de Cesar probando multi: ANFITRIÓN → "Steam no respondió... Abrí tu taller en modo
LOCAL: puedes jugar" y JUSTO DEBAJO "Algo falló: No se pudo iniciar el host local (StartHost
devolvió false)" — dos mensajes contradictorios y ninguna causa accionable.

**Causa más probable** (dos ventanas en el mismo PC): la otra ventana ya era ANFITRIONA del
puerto 7777 — o un proceso viejo del juego lo retenía — y el bind UDP de UTP falla con un false
mudo de NGO. **El fix no adivina: diagnostica.** En SessionCoordinator.StartHost (modo local),
DOS guardas antes de intentar nada: (1) NGO todavía escuchando aunque el coordinador se crea
Offline (Shutdown asíncrono a medio terminar) → se cierra y se pide UN reintento; (2) sonda
UDP de usar-y-tirar sobre el 7777 (PuertoUdpLibre — UTP corre sobre UDP, misma firma): si está
ocupado, el error dice EXACTAMENTE qué hacer ("si esta es tu SEGUNDA ventana, pulsa UNIRME —
solo una puede ser ANFITRIÓN; si no hay otra, un proceso viejo retiene el puerto: Administrador
de tareas o reinicio"). Cualquier otra excepción de la sonda se trata como libre (mejor que UTP
lo intente de verdad que bloquear por paranoia).

**Y el aviso contradictorio**: TallerSesionHud ahora redacta el aviso del fallback DESPUÉS de
intentar el arranque (el modo local es síncrono): si abrió, la promesa de siempre; si no,
"Y el modo LOCAL tampoco pudo abrir — mira el motivo aquí abajo", donde LastError trae el
diagnóstico fino. Nunca más "puedes jugar" encima de "algo falló".

Recordatorio de prueba de dos ventanas: UNA ventana ANFITRIÓN en local, la SEGUNDA siempre
UNIRME en local. Para el exe: rehacer la build (menú Alkahest → 4) para que lleve este fix.

---

## Playtest 43 → LA PARIDAD VIVA (el invitado usa, ve, oye — y el frasco fluye)

Primera prueba real con un amigo: "al absorber y soltar lo percibimos lageado; mi amigo no
podía abrir los grifos, activar las máquinas, escuchar el sonido ni ver las animaciones."
Contrato docs/CONTRATO_PARIDAD.md con el diagnóstico hecho antes de encargar: las réplicas
eran visuales por diseño (nada respondía a E), el registro replicado no llevaba estado vivo
(estatuas), el DirectorDeAudio ni se spawneaba en el invitado (y su consumo de eventos muere
sin Stepper), y la difusión de chunks corría toda a ~5Hz. Dos encargos paralelos con API
congelada entre ellos (protocolo pt40 — esta vez N terminó antes y A compiló sin transitorios).

**ENCARGO N (nervios)**: interfaz nueva Net/MaquinaUsableRemota.cs (UsarPorRed/EstadoVivoRed +
bits congelados: Trabajando/FuegoEncendido/ResultadoListo/Sirviendo/LuzPlena); las 7 máquinas
la implementan extrayendo el cuerpo de su E local a un método compartido (Dispenser movió sus
MachineFocus.RegistrarUsoE al call-site para preservar el criterio "solo cuenta si abrió de
verdad"). MaquinaSync: estadoVivo en EntradaMaquina (preservado en los 3 call-sites de
mudanza), sondeo anfitrión 4Hz solo-si-cambió, TryGetEstado + evento AlCambiarEstadoMaquina en
ambos lados, SolicitarUsoServerRpc con validación server-side (avatar a ≤14 celdas del
CentroMundo real — medido contra la geometría: el foco nunca queda a >8). MaquinaReplica:
"E — usar" con arbitraje local del más cercano, animación por bits componiendo sobre su único
SpriteRenderer (Trabajando latido → FuegoEncendido cálido → LuzPlena frío → ResultadoListo
destello, aplicado último por urgencia) y segunda línea de chapa con textos fijos del cliente.
Decisiones marcadas: ColumnaEnsayo siempre estado 0 (acción instantánea), Alambique reutiliza
ResultadoListo para "hay destilado en el matraz".

**ENCARGO A (sentidos)**: DirectorDeAudio se spawnea en la rama invitado (OrderSystem null →
sin stingers, documentado) y gana MODO ESPEJO (gate Stepper==null): el ambiente sale de
OBSERVAR la grilla replicada en la ventana de cámara (mismo patrón que ParticulasFx) a 4Hz —
Fire→crepitar, líquido activo→chapoteo, Steam→siseo (clip GrifoGas reutilizado); el sondeo
global del anfitrión se corta en espejo (si no, un incendio lejano sonaría cerca). Voces de
grifo ancladas a las réplicas por nombre (deuda: acoplamiento por string, degrada a mudo) y
encendidas por el bit Sirviendo; one-shots por transición de estado gateados !EsServidor.
FRASCO: la pasada de prioridad (≤60 celdas de avatar) pasa de 6 a 2 ticks (~15Hz), el resto
queda a 5Hz; peor caso realista ~155 KB/s por cliente (techo teórico igual al preexistente).
Flask.cs NO tocado: medido, su lote ya vacía en cada LateUpdate — el cuello era solo la vuelta.

**BONUS (reporte nuevo de Cesar a mitad de ronda: "restos de bedrock que no se pueden quitar,
muy esporádicamente", el amigo jugando solo)**: hipótesis principal = celdas de ObraDelTaller
que se leen como piedra normal (jambas/marcos/banda del hogar) y el cincel las rechazaba EN
SILENCIO. Instrumentado en vez de parchear a ciegas: (1) el cincel ahora AVISA ("es obra del
taller — no cede al cincel; las estaciones se mueven con V") por el canal Avisar de siempre;
(2) el hover del F3 añade el sufijo "· OBRA" cuando la celda está en un rect protegido. Si el
reporte se repite SIN el aviso y SIN el sufijo, la causa es otra y sabremos exactamente dónde
mirar. Auditados de paso los ciclos handle/reclamo de obra (génesis→Init→mudanza): coherentes.

**Deudas**: réplica de una sola pieza (la animación se compone en un tinte — capas por zona en
ronda futura con MaquinariaSprites); rangoFoco replicado para paridad exacta de alcance;
cerrojo de mudanza no consultado por el uso remoto (ventana de carrera mínima); calibrar en
vivo las constantes del audio espejo (elegidas sin oído); el acoplamiento por nombre de las
voces de grifo. LA PRUEBA REAL es de Cesar con su amigo — checklist en los informes de ambos
encargos, resumida en el mensaje de entrega.

---

## Playtest 44 → LA FÍSICA HONESTA (ronda nocturna autónoma 1/2)

Mandato de Cesar antes de dormir: partículas baratas FUERA, mojado FUERA, física realista
"digna de mirar" (calentamiento que se propaga de a pocos), las placas de calor/frío DE VUELTA
y más realistas, termómetros en °C para validar. Contrato docs/CONTRATO_TERMICA.md; dos
encargos paralelos (T=térmica, I=instrumentos/arco) + kill-switches de Fable.

**KILL-SWITCHES**: ParticulasFx.Activas=false por defecto (código íntegro, regla 15; toggle en
el panel F3 para compararla con ojos); pátina MOJADO apagada (prometía una filtración que la
sim no hace — la idea del goteo-que-se-seca de Cesar queda anotada aquí para cuando haya
filtración real); el tizne se queda.

**ENCARGO T**: `Sim.EmisionTermica` (física compartida de placas: falloff + empuje por
diferencia estilo Newton + COLLAR de 15 filas que devuelve el borde a ambiente — sin él la
difusión sin radio de corte saturaba el cuarto entero, medido en corrida de 3000 ticks);
`AlkahestSim.InyectarTemperatura` (disciplina Paint, la deuda del docblock saldada); HELANDO
recalibrada -80°→-26°C (bajo el modelo Newton la ventaja de HELANDO sobre FRESCA es emergente,
ya no necesita un target brutal); ambas placas con IMaquinaUsableRemota. CONVERSIÓN POR
FRENTES en el crisol: las celdas convierten individualmente al alcanzar su banda con umbral
por fila (probé sesgo de empuje por fila y NO separa — la rampa es lenta; el umbral sí),
margen como FRACCIÓN de (cima-ambiente) (un margen fijo daba <60 ticks con cima alta — cazado
barriendo TODO el rango de cimas 120-190); CerrarHornada queda como garantía de rezagados,
RegistrarOp una vez al cierre. Salts 557/563. MEDIDO headless: hervir a 3 celdas de ARDIENTE
6-8 ticks; congelar a 3 de HELANDO 22-87 ticks (peor seed); gradiente a 12 celdas ≤2°C;
frente de hornada 66 ticks el peor caso (margen fino ~10%, docblock avisa qué lo invalida).
Banco: +0.9..3.8% estable (los dos picos >5% fueron jitter del sandbox — re-run bajo umbral).

**ENCARGO I**: Game/Termometro.cs — tecla G = modo termómetro (readout vivo en °C junto al
cursor; clic pincha hasta 3 SONDAS FIFO con etiqueta viva a ~4Hz y acento por temperatura;
clic der. quita; las sondas persisten fuera del modo; frasco/cincel exclusos en modo; invitado
multi marca "—", la temp no se replica). Placas en el mundo: CALOR en la zona húmeda junto a
las pilas, FRÍA en la alcoba de la columna; en los tres modos, con réplicas (tipos 11/12) y E
remoto. SEMILLA CERO: la placa de calor desde el beat 1; beat nuevo del FRÍO entre chispa y
ensayo — "¿Y si lo ENFRÍAS?" → quinta sala destapable (arrays a 5, verificado) + pedido
"Tráeme HIELO — apúrate, que el frío no espera a nadie" (8 de Ice) con línea de fracaso
edge-trigger si se derrite en el camino.

**VERIFICADO CON OJOS (editor de Cesar, madrugada)**: compila (1 warning CS0162 en Crisol —
micro-deuda); termómetro vivo ("20°" ambiente en cursor), sondas pinchadas con etiquetas;
placa fría: chapa "HELANDO -26° · más rápido", sonda EN placa -26°, a 2 celdas -4°, lejos 20°
— EL GRADIENTE EXISTE y es empinado (el fix del "frío que inundaba"); material vertido encima
se asienta/congela en segundos. Escena MULTI genera y el panel pt42 se ve sano.

**Deudas**: CS0162 Crisol(1387); hervir/frente verificados solo headless (números arriba) —
verificación in-game de la hornada por frentes pendiente de la mañana; SIM_NOTES.md repite
"Empty no difunde" que es falso (flag de T); el sandbox se reseteó DOS veces esta ronda —
recuperación regla 6b desde el disco de Cesar funcionó (staging inverso de los 17 archivos).

---

## Playtest 45 → LA QUÍMICA CON NOMBRE REAL (ronda nocturna 2/2: el álbum de figuritas)

Mandato de Cesar (mensaje de madrugada): materiales con su MEJOR REFERENTE REAL, árbol de
figuritas coleccionable, indicador pulsante al descubrir + el menú bonito con nombre real y
mini reseña de trivia — "muchos saben intrínsecamente cómo hacer vidrio o cerámica: que usen
ese conocimiento; con esto restamos dificultad, que es el camino". Documento rector
docs/DISENO_QUIMICA_REAL.md con LA TABLA CANÓNICA (48 identidades: 5 bases × estados + 9
clásicos, cada una con nombre/color RGB/reseña de 2 líneas — verbatim al código).

**LA TESIS**: el retículo YA fabricaba cosas reales — solo las llamábamos Base2Calcinado. El
pivote es un CONTRATO DE IDENTIDAD sobre la seed 777002: arena de sílice→vidrio,
arcilla→cerámica, caliza→cal viva, VETA VEGETAL→carbón vegetal (el combustible garantizado ES
carbón ahora — mejor que dárselo hecho), sal→salmuera-que-conduce. El limo se queda con nombre
honesto: LODO DE CANTERA (lodo mineral real del que todo se separa por temperatura). El modo
CAÓTICO conserva íntegro el sistema anónimo/provisional/bautizo.

**ENCARGO Q (identidad)**: Universe con tabla estática IdentidadReal (TieneIdentidadReal/
NombreReal/ResenaReal) + colores reales aplicados en cascada a las CINCO bases en los
overrides de Semilla Cero (la arena ya no es celeste: 194,178,128); SubstanceKnowledge devuelve
nombres reales en Semilla Cero (gana a provisional y bautizo; NecesitaBautizo=false ahí; ficha
del diario abre con la reseña; extendido a NombreLey para no mezclar "sedimento celeste" con
"arena de sílice"); evento estático AlDescubrir en MarcarDescubierto (API del álbum). BEAT 3
REESCRITO: el Maestro ya no exige inventar nombre — enseña el real ("Eso es ARENA DE SÍLICE,
aprendiz. Apúntalo.") y el arco entero hereda los nombres reales gratis ("Tráeme 25 de ese...
'arena de sílice' tuyo").

**ENCARGO A (el álbum)**: Game/AlbumReal.cs — tecla B y quinta pestaña del diario: árbol de
figuritas (columnas por familia, filas por estado, derivado del grafo REAL de Universe pero
dibujando solo las aristas-verbo de cabecera: el verbo es la pista, jamás la receta), siluetas
grises "?" → swatch real + nombre al descubrir, progreso N/M (40 fijo; arena-Solución queda
"?" para siempre — verdad legítima del universo), fila de clásicos siempre revelada. EL
MOMENTO: AlDescubrir → cola FIFO + MEDALLÓN dorado latiendo junto a encargos → B abre la
FICHA-VITRINA (lenguaje visual de NamingUi: marco, latón, Cinzel dorado) con swatch, NOMBRE
REAL, reseña y "Anotado en tu álbum" (encadena la cola). Hover sobre figurita revelada relee
la reseña. En caótico el álbum existe con nombres provisionales y "aún por estudiar".

**Deudas**: NombreComun(Stone) directo en StorageRack/Dispenser/FlaskHud/OrderSystem sigue
diciendo "piedra" (fuera de alcance de Q); el rótulo del grifo dice "LIMO PRIMORDIAL" (marca
del juego — la ficha dice "lodo de cantera"; decisión estética, revisable); hit-box del
medallón copia constantes de OrdersHud; el menú de inicio/volumen (bloque 3 prometido) NO se
construyó — la noche se invirtió en física+química, que era la prioridad del mandato.

---

## Playtest 46 → EL ÁLBUM DIGNO DEL BAUTIZO + EL INFORME DE REALIDAD

Cesar con su café: pidió adueñarse de las físicas (informe grande) y tres arreglos de UI del
álbum estrenado anoche. Además jugó MODO CAÓTICO creyendo que era Semilla Cero (su captura de
"TINTE GRIS" + seed aleatoria en consola) — el arco entero vive tras el botón SEMILLA CERO.

**EL INFORME (docs/INFORME_REALIDAD.md — el documento de decisión)**: auditoría de verdad de
las 48 identidades (69% ★★★, 23% ★★, 8% ★ con renames propuestos: sal vítrea→"sal de
estampido", mármol joven→"caliza prensada", clínker→exigir caliza+arcilla, resina dura/brea
dócil a revisar); el modelo generativo (máquinas = operaciones unitarias reales; las 5
familias = los 5 pilares neolítico→Roma, un oficio real por familia); RECETAS CRUZADAS
dormidas (mortero, cemento honesto, hormigón, vidrio verde de ceniza, lejía, esmalte: +8
materiales estrella SIN bases ni máquinas nuevas); el principio TODO CAMINO DA ALGO
(transformación | mezcla con nombre | LEY NEGATIVA anotada — matriz material×operación ~350
celdas como encargo futuro); ranking de expansión (1º recetas cruzadas, 2º MENA/metalurgia,
3º electrólisis del banco, 4º fermentación+tonel); ley editorial: "se puede simplificar la
realidad; jamás contradecirla" — la confesión elegante como blindaje ante críticas.

**RONDA VISUAL OPUS (con ojos en el PC real, 2 despliegues verificados jugando)**: el
solapamiento reportado NO era ninguna de las dos hipótesis del encargo — eran DOS causas
vistas en pantalla: (a) álbum y ficha no excluyentes (velo 0.90 dejaba leer el árbol debajo);
(b) los verbos del árbol centrados en el mismo punto con Overflow ("fundirprensacalcinardisolver").
Arreglos: exclusión mutua + velo 0.94 + verbos anclados al hijo con Clip. EL LIBRITO (46x54,
sprite por código: cuero, lomo de latón con nervios, canto de vitela, rombo dorado que late —
late la LUZ, no el tamaño; numerito de latón si hay cola) reemplaza al medallón "horrible".
LA FICHA calcada de la anatomía de NamingUi (pad, filete rombo, Cinzel dorado, FirmaVisual
real del material de muestra, alto CALCULADO) con contador de ráfaga "1 de N" desde 2. EL
ÁLBUM CON PÁGINAS: 6 dobles páginas (una familia por página: vitrinas de latón a la izquierda,
árbol de verbos de ESA familia a la derecha; clásicos con página propia), navegación del libro
real + ←→, progreso por familia y total. Deuda de método anotada: en el PC de Cesar el Input
System no ve clics sintéticos del ratón — los descubrimientos de prueba se dispararon por el
MCP de Unity (AplicarDescubrimientoRemoto), que además permitió ráfagas de 2/3/5.

**INTEGRACIÓN FABLE**: las dos deudas de una línea de Opus saldadas — ApprenticeController no
mueve al aprendiz con el álbum abierto (mismo trato que JournalHud) y el banner "ALGO NUEVO"
de SubstanceKnowledge cede el paso cuando AlbumReal.Abierto (cubre árbol y ficha). Compilado
regla 53: 0 errores.

**PRÓXIMA RONDA (a decisión de Cesar tras el informe)**: Fase A propuesta — renames de la
auditoría + matriz anti-"nada" + recetas cruzadas + (pendiente del mandato nocturno) menú de
inicio con volumen.

---

## Playtest 47 → FASE A: SER DUEÑOS DE LO QUE HAY (recetas cruzadas + renames + resistencias + menú)

GO de Cesar al plan del INFORME_REALIDAD §7. Contrato docs/CONTRATO_FASE_A.md. Dos encargos
paralelos (C=cruces, M=menú) + integración Fable. Cesar prueba TODO al final de esta fase.

**ENCARGO C — LA MEZCLA EN CUBETA**: 6 materiales nuevos (Mortero=59, VidrioVerde=60, Lejia=61,
Hormigon=62, Esmaltado=63, Clinker=64 → Count=65) con identidad real verbatim (nombre/color/
reseña). Tabla de cruces en Universe.TryCruce (dominante+secundario ≥20% de la cámara, el cruce
ES la única transformación de la hornada): cal apagada+arena→mortero "amasando" · caliza+
arcilla→clínker (fuego pleno) "cociendo clínker" · clínker+arena→hormigón "fraguando" · arena+
ceniza→vidrio de botella "fundiendo con fundente" (la mezcla funde a banda MÁS BAJA que la
arena pura: la lección real del fundente) · ceniza+agua→lejía "lixiviando" · bizcocho+arena→
esmaltado. AUDITORÍA DE INTEGRACIÓN: el informe del agente resumía MAL tres ingredientes
(caliza cruda por cal apagada, mortero por clínker, turba por ceniza) — el CÓDIGO estaba
correcto; lección: el informe no es la verdad, el código sí. Los 4 RENAMES de la auditoría
aplicados verbatim (sal de estampido/caliza prensada/cal sobrecocida/ámbar de brea).
RESISTENCIAS ANOTADAS (rebanada del anti-"nada"): "resiste este fuego" desde la hornada
imposible y "resiste la prensa" desde el RESISTE — integración Fable en Prensa vía
ConectarConocimiento (la firma congelada de Init se respeta; 3 llamadores cableados). Página 7
del álbum "MEZCLAS DEL OFICIO" con las recetas como preguntas. Cruces gateados a Semilla Cero
(deuda: generalizar por roles al caótico). Fraguado de mortero/hormigón como hornada extendida
(22s/28s). Banco reconstruido y corrido: sin regresión (sandbox 2 núcleos, orden relativo
intacto).

**ENCARGO M — EL MENÚ**: AJUSTES en el título (volumen general = AudioListener.volume; efectos
del taller = multiplicador en DirectorDeAudio.FactorMaestro, el punto único que ya alimentaba
loops y one-shots; PlayerPrefs ChaosAlchemy_VolGeneral/VolEfectos; slider con thumb de latón
nuevo en UiStyles). PAUSA con Escape: escalera de guardas documentada (EscribiendoTexto →
nada; diario/álbum abiertos → ellos consumen su ESC; ajustes abiertos → los cierra; solo con
mundo limpio en Playing alterna pausa). Un jugador congela la sim (AlkahestSim.Paused); multi
NO congela por garantía ESTRUCTURAL (el DayCycle de pausa multi se auto-instancia vía
ForzarDesbloqueoSesion SIN referencia a _sim — no puede congelar aunque quiera). VOLVER AL
TÍTULO: un jugador recarga escena sin skipTitle; multi = SessionCoordinator.Disconnect + carga
de AlkahestLab.

**Deudas**: pausa multi sin probar en vivo (dos ventanas = prueba de Cesar); cruces solo en
Semilla Cero; la matriz completa material×operación (~350 celdas) sigue pendiente como encargo
propio; el slider comparte textura de riel con los campos de texto.

---

## Playtest 47b → HOTFIX: el tipo envenenado (regla 56 nueva)

Cesar probó la Fase A y "salió roto": sin título, sin HUD, mundo a medias. Consola (leída por
el MCP de Unity): `PlayerPrefs.GetFloat is not allowed to be called from a MonoBehaviour
constructor... DayCycle.cctor → TypeInitializationException` en cascada sobre CADA OnGUI que
consulta DayCycle.InputLocked. El encargo M cargó los volúmenes en INICIALIZADORES DE CAMPO
ESTÁTICO (DayCycle._volGeneral y DirectorDeAudio._volumenEfectos) — Unity lo prohíbe en
runtime y el compilador fiel no puede cazarlo (regla 53 no cubre restricciones de runtime).
FIX: centinela -1 + carga perezosa en el primer acceso (Awake/OnGUI son contextos permitidos),
ambos archivos, con docblocks espejo. Barrido del resto del proyecto: ningún otro inicializador
estático llama API de Unity. REGLA 56 nueva en CLAUDE.md. Verificado en vivo tras redeploy.

## Playtest 48 — EL DESATASCO (la veta a la vista, el multi que falla limpio, los dos fuegos que se explican)

Feedback de Cesar sobre el 47b: Semilla Cero TRABADA ("conseguí sílice, con agua
conseguí tinte, y de ahí no evolucioné; no pude cumplir lo del tostado"), el multi
en solitario roto ("StartHost devolvió false" + panel de INVITADO + mundo de ruido
gris), los menús nuevos invisibles, y los dos fuegos del inicio sin explicación
(la "N roja horrible" del calentador, el brasero chiquitito que no comunica).

### Los tres diagnósticos (detalle completo en docs/CONTRATO_RONDA48.md §0)
- **D1 EL ECLIPSE DE LA VETA (regla 57 nueva)**: la extracción del limo elige la
  banda MÁS ALTA ≤ cima (Crisol:1300) y la cima depende SOLO del combustible
  (Crisol:1131). El override 1 de Semilla Cero clampaba la base combustible
  garantizada (banda natural 106±4) POR DEBAJO de la arena (100): eclipse
  permanente → sin turba → sin carbón → calcinación de arena (banda 130)
  inalcanzable a rescoldo (120) → beat 4 imposible → arco muerto. NADIE pudo
  haber completado jamás el arco: el "trabado" de Cesar era estructural.
- **D2 EL "TINTE"**: la arena salió SOLUBLE del sorteo de 777002; el diseño dice
  "la arena no se disuelve" y solo omitió su entrada de identidad — la física
  seguía disolviendo y el nombre caía al provisional "tinte …"
  (SubstanceKnowledge:579). Doble violación (ley editorial + "sin nombres pobres").
- **D3 EL MULTI QUE SE UNÍA A SÍ MISMO**: StartHost false NO limpiaba (lobby Steam
  huérfano, m_IsHosting sucio) y HandleLobbyJoined no tenía NI UNA guarda → el
  LobbyEnter del PROPIO lobby entraba por la rama de invitado → StartClient contra
  uno mismo → "Estás en el taller de otro". El transporte Steam vendorizado SIEMPRE
  devuelve true en StartServer: un StartHost false es una EXCEPCIÓN dentro del
  arranque de NGO (tragada por su try/catch), jamás "el socket de Steam falló".
  El ruido gris = el mundo SÍ se creó y la cámara sin aprendiz cae al centro
  (384,144) = piedra maciza. El .unity del 47b además intercambió el orden de
  spawn SimSync/MaquinaSync (regenerado por el director, sin querer).

### Lo que entrega la ronda (tres encargos Sonnet paralelos + integración Fable)
- **V — LA VETA A LA VISTA**: veta de turba orgánica (salt 569, ~359 celdas,
  x80..83 y139..258, muro izquierdo del cuarto íntimo ensanchado +1 pilar solo en
  Semilla Cero) tallable con C desde el arranque; turba combustible crudo tier 130
  (arde 12,8s, humeante, residuo ceniza — regla 55 respetada); ESCALERA POR
  DECRETO: rescoldo 120→arena(100) · turba 130→ARCILLA(124) · ceniza 145→
  CALIZA(136) · carbón tier1(~185)→SAL(158) — bandas fijadas en el override
  (integración: en una seed de autor los peldaños no se heredan del sorteo), log
  de arranque "ESCALERA fuel->base" con verificación ARGMAX real + Asserts de
  eclipse; arena insoluble (D2) y turba flotante-insoluble garantizada (beat 5.2);
  beat 1 fijado a base0 (la veta ya no puede secuestrar el milagro); línea nueva
  del Maestro en el beat 4: "Esa veta parda del muro es TURBA: tállala (C), el
  brasero la come."
- **R — EL MULTI QUE FALLA LIMPIO**: StartHost false ahora cierra TODO (Shutdown +
  LeaveLobby + banderas + SessionLeft + Offline) con try/catch propio que captura
  la excepción REAL de NGO en LastError ("Steam creó la sala pero la partida no
  pudo arrancar: …"); HandleLobbyJoined con tres guardas (estado ≠ Offline / mi
  propio SteamID / host pendiente); LastError caduca a 14s con "entendido" y se
  limpia al conectar; botón "JUGAR SOLO EN ESTE PC" cuando hay error (LocalLoopback,
  el camino que ya existía enterrado); sonda IsSupported+IsSteamReady antes del
  intento Steam; MaquinaSync blindado con try/catch (spawn y Update — una
  excepción ahí tumba StartHost entero y mudo); la pausa/AJUSTES existen desde el
  PRIMER FRAME del lobby multi (ForzarDesbloqueoSesion al detectar escena, no tras
  el avatar) + botón AJUSTES en el panel del lobby (reflexión sobre
  _ajustesAbiertos, costura documentada). COSTURA PENDIENTE: SimSync:330
  CrearMundoAnfitrion sin try/catch (candidato #1 del fallo original, archivo
  fuera de alcance esta ronda).
- **L — LOS DOS FUEGOS SE EXPLICAN**: fuera la resistencia zigzag roja (la "N
  roja", documentada como idea descartada regla 15) → losa de piedra con LECHO DE
  BRASAS que laten (paleta de las brasas reales pt39, apagada = ceniza gris);
  ChillStone armonizada (dientes de escarcha más finos); rótulos de oficio:
  "PLACA DE CALOR — entibia la ZONA de encima" / "PIEDRA GÉLIDA — enfría la ZONA
  de encima" / consejo único "el crisol TRANSFORMA por hornadas; la placa solo
  CALIENTA la zona"; brasero del crisol +63% de área (5x6→7x7 interior, huella
  37→39, colchón a la Prensa 6 celdas verificado) con pila de combustible visible;
  RÓTULO DE CARGA del crisol ("cámara: arena de sílice · 62%" / "cesto: turba ×4 →
  fuego hasta ~130°"), cacheado por firma, cero allocs.

### El guion de prueba esperado (Semilla Cero, ahora completable)
limo al crisol + E → arena (beat 1-3) → beat 4 pide TOSTADO → tallar la veta
parda (C) → turba al brasero → calcina en banda 130..170 → si en cambio usas
carbón (turba calcinada, tier1 ~185) te pasas → CENIZA + lección forense + la
ceniza es combustible medido (145) → preguntas 5.1-5.4 → escalera completa:
turba→arcilla, ceniza→caliza, carbón→sal → los 6 cruces de la Fase A por fin
alcanzables (mortero, clínker, hormigón, vidrio de botella, lejía, esmaltado).

### Deudas de la ronda
SimSync.CrearMundoAnfitrion sin try/catch (D3, candidato #1); AbrirAjustes()
público en DayCycle (retirar la reflexión de TallerSesionHud); regenerar la
escena MULTI en el PC de Cesar (orden de spawn del .unity del 47b — el código ya
es tolerante, pero el orden correcto no estorba); SimLevelBuilder comenta
"CRISOL 37 celdas" y ya son 39 (comentario, no código); verificación visual en
vivo de placas/brasero/veta (regla 52) — la hace el director con capturas antes
de entregar; la trampa del beat 4 ya no es OBLIGATORIA (turba 130 calcina bien a
la primera; el sobrecalentamiento queda para quien use carbón) — alineado con el
mandato de Cesar "restamos dificultad".

### Verificación EN VIVO de la ronda (regla 52, cabina del director) y 3 fixes que nacieron de ella
1. **CS0246 en el editor que el rig no vio**: `using Netcode.Transports;` en
   TallerSesionHud — ese namespace vive en el asmdef del transporte Steam, que
   Alkahest.Runtime NO referencia. El compilador fiel une todo en UN ensamblado
   (punto ciego de la regla 53 con fronteras de asmdef). Fix: la sonda pasó a
   `SessionCoordinator.TransporteSteamSoportado` (el lado correcto de la
   frontera; además es el papel declarado de la fachada).
2. **La veta se derramó en cascada** (primera colocación en x80..83 = AIRE del
   cuarto: CuartoX0 es la primera columna de aire, la cara del muro es 79;
   medido el grid en vivo: roca maciza real en x68..79). Reubicada y luego
   SELLADA (x73..78 tras la cara intacta x79) porque la turba es polvo y toda
   cara expuesta se desliza sola — enterró la boca del crisol en la segunda
   prueba. Descubrimiento: ASOMO de 3 celdas a ras de piso (a lo sumo una
   migaja se desliza como señuelo) + la línea del Maestro del beat 4. Conteo
   final EN EDITOR: 445 celdas (banda 250-500) — la réplica Python del hash
   había prometido 359 sobre el sitio equivocado: el assert leíble salvó la
   ronda dos veces.
3. **El editor SE COME Escape** (modo Play Focused de la vista Game): la 'a'
   de moverse llega al Input System, Escape no — por eso Cesar JAMÁS vio la
   pausa del pt47 ("no vi los menús"): prueba en el editor. Fix: botón
   **MENÚ · Esc** en la esquina inferior derecha del HUD (Playing, también en
   multi) que abre la MISMA pausa con el mouse; Escape sigue para las builds.

Verificado en vivo con capturas: escalera fuel→base impresa sin eclipses
(100/124/136/158 contra 120/130/145/185), veta legible y contenida, título con
AJUSTES, MENÚ→PAUSA→AJUSTES completo con mouse, brasero con lecho de brasas,
placa nueva sin zigzag, lobby multi con AJUSTES/MENÚ desde el primer frame, y
**ANFITRIÓN por Steam levanta a la primera** (lobby id, mundo, 36 máquinas,
avatar dorado) tras regenerar las DOS escenas. Deudas nuevas: SimSync:330
CrearMundoAnfitrion sin try/catch (candidato #1 del fallo original);
AbrirAjustes() público en DayCycle (retirar la reflexión del HUD multi).

## Playtest 49 — EL RITMO Y EL ENSAYO QUE ENSEÑA (+ las placas de la foto, por Opus con ojos)

Feedback de Cesar sobre el 48: (1) trabado en "¿DE VERDAD aguanta?" — "no sé qué
aguanta el rojo, no lo hace ni el ladrillo ni la brea ni el carbón"; (2) las
placas de calor y frío quedaron casi iguales ("parece que perdiste registro del
que te pedí en la foto" — su captura vieja tenía DOS aparatos distintos);
(3) abrumado: 3 descubrimientos en el primer minuto sin tiempo de leerlos;
(4) la turba se ve finita (respuesta: 445 celdas + ceniza reutilizable; la
reposición real llega con la Fase B).

### El diagnóstico del "nada aguanta" (verificado EN VIVO)
El Ensayo FUNCIONA: 45/45 celdas de ladrillo molido (umbral 188) sobrevivieron
al rojo (177) en una prueba forense en el editor real. Lo que mataba a los
candidatos de Cesar eran DOS TRAMPAS MUDAS: (a) el frasco CONSERVA la
temperatura — el carbón recién horneado llegaba caliente y se encendía solo en
la bandeja; (b) los líquidos se TEMPLAN en el viaje — la brea llegaba como
ámbar, que sí muere (regla 51). Y una INVERSIÓN DE REALISMO: la cerámica (180)
aguantaba menos que su propio polvo calcinado (188).

### Lo entregado
- **El altar templa la muestra**: EnsayoMaestro normaliza la bandeja a ambiente
  antes de la rampa — el ensayo juzga el material, no el viaje.
- **El ensayo que enseña**: DescribirMuerte nombra EN QUÉ se convirtió la
  muestra ("se encendió: era combustible (ahora es …)"); contador público
  FallosAguantaCalor; a los 2 fallos el Maestro suelta la pista
  (diario/"resiste este fuego"/lo cocido/en frío); el pedido 5.4 gana la
  coletilla "Trae al Ensayo algo que resista el rojo."
- **La cerámica es el techo** (override 2b, solo Semilla Cero): Ceramico sube a
  max(natural,205) y Compacto a max(natural,ensayo+3) SOLO si quedaban por
  debajo del Calcinado de su base; log de umbrales de arcilla en el arranque.
- **La cola con respiro**: el TEATRO de los descubrimientos (banner ALGO NUEVO,
  pulso del librito) sale de a UNO con ≥10s de intervalo y nunca detrás de un
  panel abierto (reloj compartido SubstanceKnowledge.PuedeAnunciarTeatro/
  RegistrarAnuncioTeatro; AlbumReal pide turno con _libritoAnunciado). El
  REGISTRO sigue siendo inmediato: nada se pierde, solo se ordena.
- **LAS DOS PLACAS DE LA FOTO (Opus con ojos, 4 ciclos desplegando/capturando
  en el PC real)**: son DOS aparatos inconfundibles — PLACA DE CALOR = losa de
  fundición oscura con SERPENTÍN incandescente pulido (sinusoide submuestreada,
  incandescencia horneada en 3 bandas, bornes de latón, halo cálido) — la "N
  roja" vuelve como estufa digna, cronología del serpentín en el header;
  PLACA FRÍA = regleta metálica CLARA con ranurado de disipador y 6
  dientes-prisma de escarcha (sin violeta: tinte medido en vivo 0.85,0.96,1.00).
  Dato medido que explicaba el "son iguales": ambas placas miden 8×3 celdas =
  73×27 px en pantalla — la diferencia tiene que vivir en silueta+valor+grano a
  la vez. Réplicas multi heredan el look (deuda: verificar en sesión multi).

Detalle de la ronda anterior y sus deudas: sección Playtest 48.

## Playtest 50 — EL REWORK DE SEMILLA CERO (guion compacto, camino enseñado, fichas que tú cierras, placas mono-función, y la tercera sección)

Mandato de Cesar tras el 49: "un rework profundo para elevar la calidad" — el
arranque traicionaba la literatura de la seed 0 (milagro en minuto 0-2, pocas
opciones): dos fuegos indistinguibles desde el minuto 0, una placa-termostato
que enfriaba y calentaba (irreal), descubrimientos que se desvanecían sin
leerse, cantidades de recadero (25+15+15) y un trayecto mudo hasta la Tolva.
Contrato: docs/CONTRATO_RONDA50.md. Cuatro encargos paralelos + integración.

- **P — FÍSICA MONO-FUNCIÓN**: EmisionTermica gana dirección (enum
  Direccion SoloSube/SoloBaja como flag del registro — NO derivado de
  comparar con ambiente, ver el porqué en su docblock): la estufa jamás baja
  un grado, la nevera jamás sube uno; el HOLD de apagado respeta el signo.
  Falloff y collar intactos; cero cambios visuales.
- **F — LAS FICHAS QUE TÚ CIERRAS**: al descubrir, la ficha-vitrina del álbum
  se abre SOLA (nombre real + reseña + firma) y espera cierre manual
  ("Anotado en tu álbum" / Escape / clic); la cola cede el turno al CIERRE
  (+2s), no a un temporizador; el banner ALGO NUEVO queda de precursor solo
  si hay un panel ocupado; los 6 innominados clásicos conservan su banner de
  bautizo (es la única vía que enseña la T). Nuevo _bannerTeatroVisible
  autocurativo para que banner y ficha no se pisen.
- **G — GUION COMPACTO Y CAMINO ENSEÑADO**: cantidades beat2 25→10, beat3
  15→8, beat4 15→8, prensa/columna 10→6, frío 8→6. LA TOLVA CERCANA en seed 0
  (x129..138, boca 131..136, baseY 138 — el hueco crisol→prensa; la tolva
  clásica lejana NO se talla: una sola boca); late con pedido activo (patrón
  LuzHogar), rótulo "TOLVA — deja aquí lo pedido (vierte con clic derecho)",
  consejo del gesto completo en el primer pedido, y FLECHA de dirección en
  OrdersHud (glifo ←/→/↓, solo tipos que se entregan en Tolva, solo seed 0).
  El 5.4 y la pista del Maestro dejan de presumir "resista": "algo que
  sobreviva al rojo sin arder ni fundirse — lo bien cocido aguanta."
- **M — EL PAR TÉRMICO Y LA TERCERA SECCIÓN**: en seed 0 la placa de calor ya
  NO nace al arranque — nace JUNTO a la piedra gélida al destaparse la
  SalaFria (un solo fuego en el minuto 0: el crisol; la placa conserva el
  sitio de la pila — la alcoba mide exactamente 8 celdas y no cabe el par
  adyacente, deuda anotada para ensanchar AlcobaFriaAncho). Y **SEMILLA CERO
  COMPARTIDA**: botón nuevo del anfitrión en el lobby multi — mundo 777002 +
  overrides + veta + TODAS las salas destapadas + identidades reales + cruces,
  SIN director de beats (laboratorio para pruebas en simultáneo, pedido
  textual de Cesar). El invitado detecta la seed en la cabecera de chunks
  (SimSync) y aplica overrides + ModoSemillaCero en su espejo; el flag se
  resetea en OnNetworkDespawn y en los 3 botones de host (anti-fuga);
  TapiarSalas gana el gate !SimSync.EnEscena. COSTURA CERRADA del pt48:
  CrearMundoAnfitrion ahora en try/catch con el error real logueado.
- **Integración (regla 12)**: con la ficha abriéndose sola, DIEZ archivos de
  input de mundo ganan la guarda AlbumReal.Abierto (Flask/Cincel/Mudanza
  ocultan visuales; Crisol/HeatPlate/ChillStone/Prensa/BancoChispa/Alambique/
  EnsayoMaestro bloquean E) — el hueco preexistente que F reportó.

Deudas nuevas: ensanchar AlcobaFriaAncho para el par térmico adyacente;
réplicas multi de las placas nuevas sin verificar en sesión; TallarArcoPasillo
sin gate en seed 0 (efecto inerte, documentado).

### Playtest 50b — LA PISTOLA HUMEANTE DEL MULTI (el hash cero)

Cesar probó multi con un amigo y ANFITRIÓN volvió a fallar — pero esta vez el
fallo fue LIMPIO (mensaje veraz, guardas del pt48 impidiendo el auto-join) y la
consola entregó por fin la excepción que NGO se tragaba:
`AlkahestMaquinaSync tried to register with ScenePlacedObjects which already
contains the same GlobalObjectIdHash value 0 for AlkahestSimSync`.

CAUSA RAÍZ DE TODA LA SAGA "StartHost devolvió false" (pt42→47b→50): el
generador de la escena MULTI crea los NetworkObject por código y guardaba el
.unity en la misma pasada — un objeto AÚN NO PERSISTIDO no tiene
GlobalObjectId válido, así que su GlobalObjectIdHash se serializaba en 0. Dos
objetos a 0 → PopulateScenePlacedObjects lanza DENTRO de HostServerInitialize
→ NGO traga la excepción → StartHost false mudo. INTERMITENTE porque recién
generada la escena (objetos vivos, OnValidate corrido) FUNCIONA — mis
verificaciones en vivo del pt48/50 pasaban — y al REABRIR Unity los ceros
vuelven del archivo y revienta: por eso a Cesar le fallaba y a mí no.

FIX (`AlkahestNetSceneBuilder.SellarGlobalObjectIdHashes`): guardar la escena
PRIMERO (los objetos ya persisten), regenerar el hash de cada NetworkObject
(generador interno de NGO por reflexión; fallback FNV-1a determinista del
nombre si quedara 0/duplicado), verificar unicidad, guardar OTRA VEZ, e
imprimir la tabla (regla 51). Verificado: el .unity en disco quedó con
hashes únicos no-cero (3765942628 / 969867961, los ceros restantes son el
campo InScenePlacedSource..., normal) y StartHost(Steam) por comando arrancó
a la primera: "Anfitrión listo: sim, máquinas, encargos y avatar propio".
La escena sellada YA está en el disco de Cesar; el fix del builder viaja en
ca_playtest50b.cmd.

## Playtest 51 — EL RECETARIO DEL LABORATORIO Y LA PLACA HONESTA

Feedback de Cesar sobre el 50b (jugando la SEMILLA CERO COMPARTIDA): el arco
clásico "LO QUE PERSISTE" corría en el laboratorio multi y su segundo pedido
("algo que aguante el rojo sin ceder") es de final de partida — confuso e
inalcanzable temprano; la placa de calor no explica su oficio (no puede
calcinar arena NI DEBE: el retículo transforma por hornada, decisión pt27),
el estado TIBIO era un fósil de la era del vivium, y el halo rojo de Opus
("el rojito encima de la placa") sobraba: "quiero ver las partículas reales".

- **GenerateOrdersSemillaCompartida()**: arco fijo de 5 pedidos uno-a-uno que
  enseña la cadena temprana real con nombres reales — 10 arena de sílice →
  8 turba (veta, C) → 6 carbón vegetal → 6 arena tostada → 6 barbotina
  (Solución entregable verificada en DeliveryChute: "engulle... da igual").
  140 Favor total. Solo en ModoSemillaCero multi; el caótico no cambia.
- **Texto del pedido de calor clásico** (ArcoPersisteTextos[1]): "Trae al
  ENSAYO del Maestro algo que sobreviva al rojo sin arder ni fundirse — lo
  bien cocido aguanta (cerámica, ladrillo)."
- **HeatPlate**: halo _brillo RETIRADO entero (regla 15); TEMPLADA fuera del
  ciclo de E (Off↔Ardiente; el código queda documentado por si el vivium
  vuelve); rótulo nuevo "Hierve, derrite y seca la ZONA — transformar materia
  es oficio del CRISOL" (el "no calcina a 320°" es física correcta sin
  cartel, no un bug). ChillStone NO tocada (sin halo; FRESCA sí tiene
  consumidor real). Deuda: HintSystem/JournalHud aún mencionan TEMPLADA en
  recetas del vivium aparcado.

## Playtest 52 — SEMILLA CERO CO-OP GUIADA (el arco entero, con amigo)

Mandato de Cesar: "que la [Semilla] 0 en multiplayer escale igual que la de un
jugador — probar con mi amigo, escuchar sus descubrimientos, que le aparezcan
en pantalla aunque no sea host". La compartida deja de ser laboratorio
destapado y corre EL ARCO GUIADO:

- El director SemillaCero corre SOLO en el ANFITRIÓN del multi (gate de Init
  estrechado: invitado jamás lo instancia; enmienda documentada del contrato
  pt40 §2). El recetario del pt51 queda RETIRADO de este camino (reserva).
- Salas TAPIADAS también en multi (revertido el gate del pt50 en AlkahestSim;
  el espejo del invitado no tapia localmente: la piedra le llega por chunks).
- LA VOZ DEL MAESTRO viaja: SemillaCero publica texto+duración (par estático),
  SaberSync lo replica (NetworkVariable FixedString con recorte UTF-8 y
  elipsis) y OrdersHud lo pinta en el invitado con el MISMO panel
  (DibujarPanelMaestro estático compartido).
- FICHAS EN EL INVITADO: auditado el camino _descubiertos →
  AplicarDescubrimientoRemoto → AlDescubrir → ficha modal — YA disparaba el
  teatro correcto (transición false→true), sin código nuevo; el catch-up
  tardío entra por la cola con respiro del pt50.
- BUG REAL cazado de paso: las descripciones de pedido >128 bytes se
  truncaban a mitad de frase para el invitado (el beat 5.4 lo hacía) —
  recorte por bytes con elipsis en SaberSync.
- DECISIÓN (costura #1, documentada): MaquinaSync publica su registro UNA vez
  y no admite altas tardías → las 6 estaciones nacen TODAS al arrancar la
  sesión co-op (las salas de piedra siguen selladas hasta su beat: la guía la
  hace el muro, no el spawn). Costo cosmético: una chapa "E — usar" pegándose
  mucho a un muro sellado. Deuda: escaneo incremental de MaquinaSync.
- Deudas: flecha a la Tolva omitida en el invitado (DeliveryChute no es
  NetworkObject); verificar host→salir→host SIN cerrar el juego (¿AlkahestSim
  re-crea el mundo?); HintSystem/JournalHud aún mencionan TEMPLADA.

## Playtest 53 — EL REGISTRO INCREMENTAL (las máquinas ya no flotan sobre sus muros)

Captura de Cesar en el co-op: las salas selladas EN LA GRILLA, pero los
SPRITES de las 6 estaciones (nacidas todas al inicio por la limitación del
registro, pt52 costura #1) dibujados encima de la piedra — la Prensa flotando
con su "E — prensar" sobre el muro sellado.

- **MaquinaSync acepta ALTAS TARDÍAS**: el primer lote publica solo las
  familias que siempre nacen juntas (crisol/grifos/balda/anclaje/rack/
  alambique/pilas) + las tapiables SI existen (caótico); las que falten las
  añade `SondearAltasTardias` (mismo acumulador de 0.5s, se auto-apaga con
  `_altasTardiasCompletas` al completar las 6). El registro solo CRECE
  (NetworkList.Add — los índices publicados son identidad). Lado invitado:
  CERO código nuevo (AlCambiarRegistro ya manejaba Add en vivo).
- **El co-op spawnea POR BEAT otra vez**: revertido el bloque del pt52 en
  TrySpawnRed — mismo patrón condicional `SalaDestapada` que un jugador;
  PollDestapesSemillaCero hace el resto. Ventana muro-cae→réplica-aparece:
  ≤0.5s + RTT. El caótico multi, sin cambios.
- Invitado tardío: el catch-up del NetworkList trae el registro crecido
  completo (verificado por diseño). Un jugador: cero cambios.
- **FUGA CONFIRMADA, NO CERRADA (deuda prioritaria)**: host que sale de la
  sesión y re-hostea SIN cerrar el juego — `_spawned` (bootstrap:103) y el
  estado de MaquinaSync (:418) nunca se resetean; `SessionEvents.RaiseSessionLeft`
  no tiene suscriptores en Assets/Alkahest. El fix real toca SimSync/
  AprendizNet/OrderSystem/MachineFocus/DirectorDeAudio — pieza propia.
  WORKAROUND mientras tanto: tras salir de una sesión, VOLVER AL TÍTULO y
  reentrar a la escena multi antes de re-hostear.

## Playtest 54 — SOBRIEDAD Y PARIDAD (el buzón del Maestro)

Feedback de Cesar (capturas solo vs multi de la seed 0): las dos versiones no
eran iguales; las redomas del estante eran ruido; y la Tolva cercana partía el
camino con jambas doradas, flecha flotante y letrerote — "la única cosa que
necesito investigar al inicio es el crisol".

- **LA FUGA DE PARIDAD (causa raíz)**: Balda/Anclaje/Pila guardaban una
  bandera estática "ya creadas" SIN reset — quien entraba SEGUNDO a un modo
  en el mismo proceso se quedaba sin esos muebles (por eso el multi salía
  decorado y el solo pelado, o al revés según el orden). Fix de integración:
  ResetGuardaEstatica() en los tres, llamado junto a MachineFocus.Limpiar()
  en ambos arranques de mundo del bootstrap.
- **La mitad de soportes**: galerías 10→5, BaldaPlan 17→8 (-53%), alternando
  por índice para repartir; anclajes y pilas intactos.
- **Redomas fuera de la seed 0**: StorageRack.Init gana `visible` —
  componente vivo (MaquinaSync exige estantes>=1 para publicar el registro
  multi) pero sin sprites ni redomas en Semilla Cero. Vuelven en el caótico.
- **EL BUZÓN DEL MAESTRO** (ex Tolva cercana): reubicado ELEVADO y lateral
  (x97..106, base y198, la cota del ex-estante, cerca del alambique), fuera
  del paso; jambas doradas, triángulo flotante y letrero permanente
  RETIRADOS; ahora es un buzón de piedra con marco de latón fino, bandejita
  y relieve de pergamino/sello (comunica "entregas" sin letras); rótulo solo
  de proximidad ("BUZÓN DEL MAESTRO — vierte aquí lo pedido"); halo de
  pedido activo a mitad de intensidad; la flecha del panel de encargos lee
  la posición real y apunta sola al sitio nuevo. Consejos del HintSystem
  renombrados a Buzón (integración). El caótico conserva su Tolva clásica.
- Deuda menor: TryFlechaTolva nunca emite "↑" (dominan ←/→ salvo justo
  debajo del buzón).

## Playtest 55 — LOS SIETE BUGS DEL CO-OP (la noche, parte 1)

Reportes de Cesar tras probar con su amigo (build + editor). Causas REALES:

1. **VOLVER AL TÍTULO reventaba la build multi**: la build empaqueta SOLO la
   escena multi y DayCycle cargaba "AlkahestLab" por nombre → error de build
   profile. Fix: en multi, VOLVER AL TÍTULO recarga la ESCENA ACTIVA (el
   lobby es el título de esa build). El "Can't disconnect client" era una
   segunda llamada a Disconnect durante el frame de Shutdown de NGO — guarda
   idempotente (Offline → no-op).
2. **Al invitado no le saltaban las fichas — hipótesis (a) CONFIRMADA**: el
   catch-up de SaberSync corre en cuanto existe el avatar local, pero el
   AlbumReal del invitado nace DESPUÉS del primer snapshot del mundo
   (Cableado ⇐ Universe ⇐ primer chunk): el evento AlDescubrir se disparaba
   a cero oyentes y la idempotencia lo silenciaba PARA SIEMPRE. Fix: TEATRO
   PENDIENTE persistente en SubstanceKnowledge (_pendientesVitrina, FIFO 40)
   — se llena SIEMPRE en MarcarDescubierto; AlbumReal lo drena en su primer
   Update (barrido de backlog) y en cada evento vivo; la cola con respiro
   del pt50 conserva el ritmo. Nada pierde teatro en silencio.
3. **Omisiones del espejo**: (a) chapa "el grifo" 4 celdas lejos — la red
   solo transmite CentroMundo (ancla de agarre, +2.5c) y el rótulo real vive
   en +6.5c: compensado en MaquinaReplica (delta X solo grifos); (b) "marco
   dorado con grietas" = la réplica de la Pila estiraba una textura 2:1 a
   una huella 14x9 — la textura ahora sigue la proporción real; (c) el agua
   de la pila: pipeline de chunks estructuralmente sano — pendiente captura
   en vivo (posible "agua con más cuerpo" del backlog, no bug de red).
4. **Panel cortado en el borde del invitado — causa INESPERADA**: el botón
   VOLVER AL TÍTULO lanzaba (bug 1) DENTRO del BeginArea de la Pausa y el
   GUIClip global quedaba corrupto TODO el frame, recortando los HUD ajenos
   contra el rect de la pausa. Fix triple: try/finally en DrawPausa (raíz),
   clamp a pantalla de DibujarPanelMaestro (garantía), y OnGuiReplicado
   usaba TextoSinEncargos clásico en vez de TextoVacio (contenido obsoleto).
5. **Ámbar de brea al quemar turba — NO ES BUG, ES QUÍMICA REAL**: el calor
   de la combustión funde turba vecina (brea = alquitrán vegetal, la
   destilación destructiva existe: así se hacía la brea de los barcos) y al
   enfriar templa a ámbar. Poquito, coherente, documentado — es el tipo de
   sorpresa que la ley editorial protege.
6. **Álbum**: reseñas/nombres largos se desbordaban (estilos compartidos sin
   word-wrap, medidos ahora con UiStyles.Alto) + TOOLTIPS por hover en las 8
   cabeceras de estado y en "cómo se llega" (textos precalculados, tono
   ocre).
7. **El haz del frasco**: el código estaba INTACTO desde el playtest 11 — lo
   mataba AlbumReal.Abierto atascada en true tras morir su instancia (fin de
   sesión/recarga) vía la guarda de la regla 12. Fix doble: Abierto=false en
   AlbumReal.OnDestroy, y SALIR de la sesión ahora RECARGA la escena activa
   (cierra de paso toda la fuga de re-host del pt53: bootstrap._spawned,
   registro de MaquinaSync, HUDs huérfanos — el fix "por piezas" queda
   como deuda de lujo).

NOTA DE TALLER: 6º reinicio del sandbox a mitad de ronda — el repo renació
del pt54 pusheado (regla 6b pagó de nuevo) y el rig regla 53 se reconstruyó;
los dos encargos verificaron a mano y Roslyn confirmó después: EXIT=0.

## Playtest 56 — LA VIDA ÚTIL DE LO DESCUBIERTO (la noche, parte 2: la vertical slice)

Mandato nocturno de Cesar: cambiar el loop de "descubrir→registrar→descubrir"
a "descubrir→producir→almacenar→utilizar→necesitar→descubrir". Diseño
completo: docs/DISENO_VIDA_UTIL.md; contrato: docs/CONTRATO_RONDA56.md.

- **EL ENCARGO COMPUESTO** (Order gana grupo: 3 hermanas Guiado con GrupoId
  compartido — TryDeliverCell/MatchesOrder intactos): al entrar el arco en
  FinalAbierto, EL MUNDO empieza a pedir — "LOS VITRALES DE LA CAPILLA"
  (40 vidrio de botella / 12 barbotina para el engobe / 20 mortero, 60★)
  con checklist de 3 líneas en OrdersHud (▫/✓, n/total, flecha al Buzón);
  al completarse: cierre narrativo + se encadena "UNA MUFLA DE VIDRIERO"
  (24 cerámica / 16 vidrio / 12 mortero, 40★).
- **LA ALACENA** (StorageRack renace con papel): se revela con
  SemillaCero.FaseVidaUtil en x109..140 y198 (el hueco tras el Buzón — el
  sitio literal del ex-estante COLISIONABA con la jamba del Buzón, medido),
  6 casillas (caótico sigue con sus 5 intactas), cap real 300/redoma,
  nombre+cantidad y nivel visible ya existían — se añadió Revelar()
  idempotente y el rótulo "LA ALACENA — guarda aquí lo que produzcas".
- **LA MUFLA** (expansión pagada con materiales): sitio reservado x55
  (CuartoX0 baja 80→30, mismo patrón pt34; la veta se muda sola);
  al completarse "obra_mufla" el bootstrap talla Crisol.TallarEnPlano y
  spawnea la SEGUNDA instancia de Crisol. Deuda: en co-op el invitado la VE
  (chunks) pero no la usa (MaquinaSync asume instancia única por tipo).
- **Costura contratada**: OrderSystem.AlCompletarCompuesto(id) +
  SemillaCero.MaestroAnuncia(linea, seg).
- **REGLA 57 CAZADA EN VIVO ANTES DE ZARPAR**: el pigmento era licor pardo
  (veta Solucion) — pero la veta es INSOLUBLE por decreto del beat 5.2, y
  la verificación en el editor mostró además que el sorteo dejaba la
  ARCILLA insoluble: la barbotina del recetario pt51 TAMBIÉN era
  inalcanzable. Doble fix: Override 6b (solubilidad POR DECRETO según la
  tabla de identidades: arcilla sí, chamota no, arena/turba no, caliza/sal
  sí) y el pigmento del encargo pasó a barbotina/engobe (técnica real).
  El licor pardo queda sin camino en 777002 a sabiendas (el costo de que
  la turba flote), como la arena disuelta.
- Deudas: replicación del compuesto al invitado (SaberSync solo lleva las
  3 líneas sueltas sin título ni narrativa — FixedString128); barra de
  progreso individual en el checklist; réplica multi de la mufla; más
  encargos del mundo (este es EL piloto).

### El bug del compuesto (cazado y matado la misma noche, verificado 2x en vivo)
SÍNTOMA (reproducido 3 veces por TryDeliverCell en el editor): completar SOLO
la línea del mortero (celda 20 exacta) pagaba el compuesto ENTERO (+60★,
obra encolada) con vidrio 0/40 y barbotina 0/12 intactas. AUTOPSIA: regla 49
de manual — el docblock de `EncolarPedidoGuiado` prometía "_arcoPersisteIndex
se queda como esté, normalmente -1, nunca activado en este modo" y era FALSO
desde el playtest 25: `DayCycle.EnterCuartoIntimoSilencioso` (el arranque de
TODA partida solo, línea 374) llama `GenerateOrdersPersiste()` TAMBIÉN en
Semilla Cero, dejando el arco de LO QUE PERSISTE ARMADO (índice 0) debajo del
guion entero. Cada pedido guiado completado lo incrementaba en silencio; al
rebasar ArcoPersisteCount, `AvanzarArcoPersisteSiToca` seguía haciendo
`ActiveOrders.Clear()` SIN reponer nada — así que al completarse UNA línea
del compuesto, borraba a sus hermanas un renglón ANTES de que
`AvanzarGrupoCompuestoSiToca` las buscara: cero hermanas incompletas = grupo
"hecho". TRIPLE FIX: (1) DayCycle no genera LO QUE PERSISTE si
ModoSemillaCero (causa raíz); (2) `EncolarPedidoGuiado` y `EncolarCompuesto`
DESARMAN ambos arcos (`_arcoPersisteIndex=_arcoRecetarioIndex=-1`) — quien
toma el escenario desarma a los francotiradores; (3) al completarse la obra,
`ActiveOrders.Clear()` real (la promesa "vacío tras la obra" tampoco tenía su
línea: quedaba el checklist ✓✓✓ eterno y `AllOrdersCompleted()==true` de por
vida). VERIFICADO end-to-end con el código final: mortero 20/20 NO cierra el
grupo; vitrales completos → +60★ → obra encolada; obra completa → +40★ →
ActiveOrders vacío + "La mufla se construyó en 55,138" + Crisol_Mufla
spawneado. Lección para la 49: cuando un método "no-op si no está activo" se
llama desde un camino compartido, el comentario que jura quién lo activa se
verifica leyendo TODOS los llamantes del activador, no el docblock.

## Rondas 57-60 — EL GIRO FINAL: TEN THOUSAND YEARS (nombre nuevo, GDD, limpieza L1 y el greybox del inicio oscuro)

**57**: fix del beat de la columna ("¿Por qué esto queda ENCIMA?" ahora enseña
experimento + criterio + lugar de entrega, con la turba como camino nombrado);
INFORME_VISION_FINAL.md (pronóstico bifurcado por arte, nombres).
**57b**: INFORME_OPINION_GIRO.md — feedback profundo del giro de Cesar (mundo
vacío, trueque, libro mayor, asimetría "lo que pides tarda / lo que te piden
espera", volátiles con 3 guardas, fuera el Favor, las EDADES).
**58-59**: GDD_TEN_THOUSAND_YEARS.md v0.1→v0.3 (NORMATIVO — leerlo antes que
este HANDOFF para todo lo nuevo). Nombre sellado: TEN THOUSAND YEARS +
"Rebuild human knowledge from mud, fire, and observation". Decisiones: inicio
oscuro minimalista, 3 slots de guardado, dos buzones simétricos, stock por
tiempo, un solo precio por objeto (alquilar/poseer aparcado), semilla de
autor, caótico como promesa bloqueada, patentes apagadas, repo→TenThousandYears.

**60 (nocturna en autónomo, Cesar descansando)**:
- LIMPIEZA L1: el Favor FUERA de todo HUD (OrdersHud solo+replicado: línea
  "N ★", barra dorada, "+N ★" por fila y en cabecera de compuesto — la
  mecánica interna sigue viva SOLO para el caótico legado, ver bloque regla-15
  donde vivía ActualizarFavorTexto); PATENTES apagadas
  (Hornada.PatentesActivas=false, ring sigue grabando). Las pantallas de
  jornada del caótico (DayCycle 1193/1345/1440) conservan sus Favor a
  propósito: mueren con el rediseño del caótico, no antes.
- EL GREYBOX DEL INICIO OSCURO (GDD §5, F1 parcial): ModoFundacion (bootstrap,
  excluyente con ModoSemillaCero, botón "EL INICIO — fundación (greybox)" en
  el Título), SimLevelBuilder.BuildFundacion (mundo de piedra maciza + UNA
  caverna x340..460 y140..200: mesa del Maestro, hogar de brasas x424..428,
  GOTERA en x426 desde la bóveda, montón de arcilla x352..364, cuenco de agua
  x372..385), y Game/FundacionDirector.cs: beats Mirar→Saludo→Gotera→Barro→Fin,
  viñeta de oscuridad IMGUI (depth 50, radio por beat, amanece al final),
  brasas mantenidas a 165raw con WakeChunk (regla 55b), gota por PaintStable
  cada 1.15s QUE SE EVAPORA DE VERDAD contra las brasas (verificado: vapor=4
  en el primer sondeo — la primera reacción es sim pura, cero teatro), pedidos
  como checklist con Progreso espejando el frasco (sin Buzón aún — el atajo se
  retira cuando la fundación gane su Buzón de Entrada), y LA GOTERA DOMADA:
  al entregarla nace Grifo_Fundacion sobre el cuenco ("los fenómenos no se
  eliminan: se DOMESTICAN" — la tesis del juego en un gesto).
- VERIFICADO EN VIVO (Unity MCP, arco entero): mundo construido (agua=69,
  arcilla=52, brasa=227raw), HUD dormido en beat 0 (gate
  FundacionDirector.HudPermitido en DetectarPrimeraAccion), saludo por
  proximidad despierta el HUD, gotera 15/15 → grifo en (37.45,15.25) + drip
  parado, barro 12/12 → fin con pedidos limpios y amanecer (radio 2910).
- Compilador fiel reconstruido 2 VECES esta sesión (el sandbox perdió /home
  dos veces — regla 6b más viva que nunca; el zip de refs vive en
  _to_delete/_unityrefs.zip del disco de Cesar, re-stagear de ahí).
- PENDIENTE F1: fogón propio (turba prestada + asomo de veta), vidrio+estante,
  frasco propio de cerámica REAL (hoy el saludo/frasco es teatro), Buzón de
  Entrada de la fundación, y el tag `ultimo-clasico` que crea
  ca_playtest60.cmd ANTES de commitear esta ronda.

## Ronda 61 (autónoma) — LA FUNDACIÓN COMPLETA: fuego propio, vidrio y estante (beats 4-6)

El greybox del inicio oscuro cubre ahora TODO el §5 del GDD. Plano: orilla de
arena (x388..399, MatDe(0,Polvo) -- la arena de sílice del cruce, regla 47),
HOGAR VACÍO (x404..410, lecho y140 = la fila donde el polvo reposa), bolsón de
turba SELLADO tras la cara del muro izquierdo con asomo de 3 celdas que gotean
acotado (lección pt48 aplicada), sitio del estante (x448..458). Director:
beats Fogon (cargar 12 turba en el hogar, progreso espeja EL MUNDO, no el
frasco) → Vidrio (arena+ceniza en el hogar encendido → 8 al Maestro) →
Estante (StorageRack de 3 redomas junto a la mesa) → Fin.

LA SAGA TÉRMICA (3 iteraciones en vivo, regla 50 encadenada): a 175 raw la
turba del hogar SE FUNDÍA (retículo: 9 fundido+4 templado, cero ceniza); a
152, también (su fusión en 777002 anda <=148); y a 132 con celda de Fire de
ignición, TAMBIÉN (Fire nace ~240 y la cascada térmica funde la pila antes de
que arda). VEREDICTO: la combustión abierta de turba en pila no es domable
por temperatura de lecho. SOLUCIÓN (filosofía del brasero del Crisol real):
el hogar encendido CONSUME su turba a ceniza por cadencia (0.7s/celda, regla
54: el residuo queda), el lecho queda TIBIO (118), y la cima que ve
Universe.TryCruce es FogonCimaRaw=152 -- constante del FUEGO, no de la celda,
exactamente como el crisol aplica su cima a la hornada sin fundir la cámara.
El cruce es LA TABLA REAL (cero física inventada).

VERIFICADO EN VIVO (arco entero): saludo → gotera → grifo → barro → hogar
cargado → encendido → censo final [ceniza]x9 [VIDRIO]x5 nacidos en el hogar →
8 vidrio entregados → Estante_Fundacion nacido → cierre con pedidos limpios y
amanecer. HALLAZGO OPERATIVO: sin foco de ventana el editor SUSPENDE el
player loop (parecía un bug del director y era la PC sola) --
Application.runInBackground=true en toda verificación remota desde ahora.

PENDIENTE F1: cocción real del frasco (hoy teatro), Buzón de Entrada de la
fundación, ficha a la vista. SIGUIENTE (en curso): F2 LA ECONOMÍA.

## Ronda 62 (autónoma) — F2: LA ECONOMÍA (el tablón, el trueque y el buzón de salida)

Game/Trueque.cs nuevo: el Maestro de doble entrada del GDD §6, dormido hasta
que el arco cierra (FundacionDirector beat Fin -> Trueque.Activar, con la
línea "MI TABLÓN queda abierto... lo que pidas tarda"). Las cinco reglas
selladas tienen su línea (regla 49): catálogo CONST para 777002 (turba 15 por
6 vidrio/75s, arena 15 por 4/50s, arcilla 12 por 4/50s, y CALIZA como página
CERRADA visible-no-comprable), stock por ciclo de 180s, se paga DEL FRASCO
(pagar vacía lo producido: la tesis en un gesto), pedidos con tiempo de
entrega que aterrizan como MATERIA FÍSICA (PaintStable) en el BUZÓN DE
SALIDA -- el nicho natural entre la mejilla del hogar y la mesa
(FundacionSalidaX0..X1 = 430..433, nombrado en el plano, regla 39). Panel
IMGUI por proximidad+E (guardas regla 12), con segunda pestaña LIBRO MAYOR
v0: historial del arco, horizonte (la caliza con requisito legible, el
matraz), y "...32 páginas más que aún no puedes leer" (el patrón anti-menú-
gris del informe de opinión). Máx 4 pedidos en camino; alejarse cierra el
panel.

VERIFICADO EN VIVO: activación, pedido de arena pagando 4 vidrio del frasco
(10->6), stock 2->1, entrega madura -> 15 de arena físicas en el nicho +
aviso del Maestro, cola a cero; guardas de página cerrada y de pago
insuficiente respondiendo con su frase. PENDIENTE F2: el contador real de
"20 vidrio cambiados" que abre la página de la caliza; réplica multi del
tablón; calibración de precios (hoy el bootstrap arena->vidrio->arena es
circular a propósito de greybox, anotado).

## Ronda 62b (autónoma) — EL PASE DE OPUS-CON-OJOS (dirección de arte UI/UX sobre capturas reales)

Con permiso de pantalla de Cesar, un agente OPUS capturó los 4 estados del
greybox en el editor real y entregó 12 directivas numéricas (acta en el
resultado del agente; prioridades 3-1-2). Implementado y RE-VERIFICADO con
capturas:
- BUG BLOQUEANTE (dir. 3): la rama SpawnFundacion NO spawneaba DayCycle ->
  nadie corría ApplyPause -> InputLocked se quedaba en su `true` inicial ->
  NINGÚN HUD del juego se dibujaba jamás en la fundación. Fix: SpawnDayCycle
  (supplies/hints null). Verificado: FRASCO + ENCARGOS + MENÚ·Esc despiertan
  con el saludo, y el encuadre cero sigue limpio (solo HudSilenciado).
- Viñeta (dir. 1/12): parda cálida (#0d0906 exterior, #1a1109 medio), dos
  paradas 0.42/0.78, ÓVALO (squash y=0.82), foco más pegado a las brasas en
  el beat 0 (lerp 0.25), rellenos con 2px de solape (sin costuras). En la
  62a ya había caído el clásico Texture2D.blackTexture-tiene-alfa-cero.
- GLOW (dir. 11): la luz tiene causa -- halo radial #ff8a2b al 18% sobre las
  brasas del Maestro (S56) y sobre el fogón propio al encenderse (S64),
  dibujado bajo la oscuridad.
- LA BANDA DEL MAESTRO (dir. 4): FundacionDirector.DibujarBandaMaestro
  estática (la consume también Trueque): banda 46% arriba-centro, filetes
  solo arriba/abajo #8a6a30, rótulo dorado, cuerpo crema. Reemplaza a
  SemillaCero.DibujarPanelMaestro EN LA FUNDACIÓN (seed 0 intacta).
- CHAPAS (dir. 5): ancladas sobre su objeto y atenuadas por la luz local
  (FundacionDirector.LuzEn(mundo) público, clamp 0.25..1).
- EL TABLÓN (dir. 2/7/8/9/10): reescrito a layout por Rect -- scrim #0d0906
  al 55%, panel S420 anclado arriba (top S96, nunca sobre el sujeto),
  pergamino #100c09 con DOBLE filete dorado, título EL TABLÓN + pestañas
  TRUEQUE/LIBRO MAYOR con estado activo/inactivo, filas con PEDIR (S64x20)
  centrado en SU renglón, EN CAMINO degradado a etiqueta con filete, CERRAR
  (E) chapa abajo-derecha, alto por contenido.
PENDIENTE del acta de Opus (post-test): paleta del mundo (piedra parda
#4a3c33, agua #2b4f7a -- toca tablas de Universe, con cuidado regla 17),
banda del maestro con fade, dodge dinámico del panel respecto al sujeto.
LECCIÓN OPERATIVA: los permisos de computer-use SIGUEN muriendo solos
(regla 4) -- re-pedir sin drama; y el "escalón negro" que Opus vio era la
costura de redondeo del óvalo, no un chunk.

## Ronda 63b (autónoma) — EL TABLÓN ELEVADO A LA LÍNEA DEL BAUTIZO

Mandato de Cesar ("el menú de interacción con el mago era horrible... que lo
tome Opus y lo eleve a la calidad del menú bautizar, para que se mantenga en
la línea gráfica"). La línea del bautizo YA está codificada como API
(UiStyles del skin vestido pt31): PanelRito (vitela ahumada + marco de latón
con cantoneras), TituloRito + Espaciar() (capital lapidaria), FileteRombo,
MarcoLaton, paleta Oro/Laton/Pergamino, estilo Ceremonial. El tablón de
Trueque se REESCRIBIÓ entero sobre ese vocabulario (cero hex propios): título
lapidario con rombo, pestañas del skin con la activa en ORO y subrayado,
línea de tienda en Ceremonial, FILAS CON LA MUESTRA DEL MATERIAL ENMARCADA EN
LATÓN (la muestra del rito: ves la materia que pides -- turba parda, arena
dorada, arcilla), botones Pedir del skin alineados a su renglón, EN CAMINO
con hilo de latón, Cerrar (E) centrado. La banda del Maestro también:
Pergamino + filetes Laton + "EL MAESTRO" espaciado en Oro. VERIFICADO CON
CAPTURAS en el editor real (estado de exhibición por reflexión). De paso:
OrdersHud.TextoVacio ahora da "(nada por ahora)" también en ModoFundacion
(el texto clásico hablaba de la Tolva sellada y usaba "os" -- regla 53; el
caótico legado lo conserva hasta su rediseño). NOTA para el testeo: el panel
del tablón se AUTOCIERRA al alejarse del nicho (>10 celdas) -- es guarda, no
bug.

## Ronda 64 — LA PRIMERA EXPERIENCIA, A PRUEBA DE BURROS (feedback pt64 + pase Opus con runtime)

Cesar probó el greybox y lo destrozó con razón: "empezó 1 segundo bien y
luego se fue todo al carajo". Sus reclamos, uno a uno, y lo que se hizo:

1. "LA GOTERA NO ES GOTERA: pozos de agua, agua hirviendo, vapor". Causa:
   GoteraX=426 caía SOBRE las brasas (165 raw): cada gota hervía. Ahora cae
   en LA POZA (x379, cuenco 372-385 que nace SECO) desde una ESTALACTITA de
   piedra en la bóveda; el objetivo del beat 2 es "recoger lo que se está
   inundando". Gota = hilo de 3 celdas cada 0.95s. La poza REZUMA
   (RezumarPoza: el agua sobre 34 celdas se filtra por el fondo, 1
   celda/frame) así que EL HILO NUNCA SE APAGA y jamás desborda — la primera
   versión la PAUSABA al llenarse y Opus la cazó con runtime: la gotera
   moría a los 39s (el reclamo original, reintroducido por la guarda).
   VERIFICADO con datos: a t=45s+ dripTimer corriendo y agua clavada en 34.
2. "TEXTOS LARGOS, DESAPARECEN RÁPIDO, MALA UBICACIÓN... como novelas
   visuales, que presione siguiente". La banda temporizada MURIÓ para el
   guion: DIÁLOGO POR PÁGINAS abajo-centro en PanelRito (línea del bautizo),
   UNA frase corta por página, avanza con CLIC/E/ESPACIO/ENTER, pie
   "clic izquierdo — continuar/cerrar · n/m". Mientras está abierto,
   UiStyles.EscribiendoTexto bloquea TODO el mundo (regla 12): el clic de
   avanzar jamás aspira. [NonSerialized] en _paginas/_pagina/_alCerrar: el
   hot-reload del editor resucitaba el array null como VACÍO y OnGUI
   indexaba -1 cada frame (visto en vivo: 50 IndexOutOfRange/s tras Ctrl+R).
   DibujarBandaMaestro SIGUE VIVA solo para los avisos del tendero (Trueque).
3. "GARANTIZAR la primera experiencia, que me avise que presione clic
   izquierdo". BANDA DE OBJETIVO persistente abajo-centro, patrón único
   (directiva Opus #6): `GESTO — objeto · n/m`, gesto SIEMPRE primero en
   #FFD159 negrita (richText). Orden pedagógico del barro corregido: enseña
   CLIC DERECHO (verter) antes que aspirar una crema que aún no existe.
4. "MATERIAL TIRADO POR AHÍ, NO PUEDE SER... que me lo dé el Maestro y caiga
   en ese momento". BuildFundacion ya no coloca arcilla/charco/orilla de
   arena; el asomo derramable de la veta también murió (3 celdas resbalaban
   al piso) — la señala un cartel de mundo "VETA — talla con C" desde el
   beat del fogón. El Maestro VIERTE: TickVertidoDelMaestro deja caer
   arcilla (30, x366) y arena (20, x400) POR PROXIMIDAD (≤25 celdas,
   1 celda/0.15s) — la primera versión vertía al cerrar el diálogo y Opus
   confirmó con runtime que caía en zona oscura sin nadie mirando.
5. "ESA ANIMACIÓN HORRIBLE DE LUZ... tenemos física reeaaal... que esté
   ardiendo de verdad y sea la única fuente de luz, titilante". FUERA el
   glow radial pintado (textura _glow, RIP). El hogar del Maestro es BRASA
   REAL (MaterialId.Brasa, 2 filas autocurativas con vida repuesta — el
   motor inyecta el calor) con ~3 lenguas de Fire REALES mantenidas
   (mueren solas a ~16 ticks: el parpadeo ES la sim) y TIRO corto (una
   llama sobre +6 filas se apaga: a esa altura el Fire agónico parecía
   "arena subiendo"). El titileo de la viñeta se DERIVA del número de
   celdas de Fire vivas (_luzFuego). La luz tiene causa física o no existe.
6. HALLAZGO EN VIVO: el aprendiz NUNCA tuvo colisión con piedra (solo clamp
   a bordes del mundo) — a oscuras, 2s de vuelo contra el muro = tester
   perdido DENTRO de la roca (me pasó). CorralDelGreybox lo contiene a la
   caverna (+9 celdas de gracia hacia la veta). Colisión real = deuda
   post-Fest.
7. SILUETA DEL MAESTRO (Opus #4: "no hay Maestro, el rótulo señala ladrillo
   vacío"): túnica encapuchada 6x8 celdas generada por código, sentada a la
   mesa, dos brasas por ojos; la chapa EL MAESTRO sube a MesaTopY+11 (encima
   de la capucha) y SE OCULTA a <14 celdas (le caía en la cara al jugador).
8. Más directivas Opus aplicadas: tipografía RELATIVA a Screen.height
   (PrepararEstilosPropios; cuerpo diálogo 2.7% del alto, objetivo 2.1%),
   cuerpo del diálogo a la IZQUIERDA, pie #D8C89A, guiones — reales,
   UiStyles.Exito verde neón→MUSGO (0.55,0.70,0.42), viñeta exterior
   #0d0906→#150f0b (el jugador era invisible fuera del cono) y foco de la
   luz 0.65→0.85 hacia el jugador (tu atención es tu lámpara), spawn a
   (400,150) como SILUETA en la penumbra junto al fuego (radio inicial
   S(240)→S(280)), mejillas del fogón 2x3 ("dos palitos no leen como
   fogón"), grifo montado a +8 (levitaba a +12), ración 24 (45 desbordaba
   la poza), estalactita 5 celdas.
VETOS RAZONADOS a Opus (anotados, no perdidos): SuckRatePerTick/ReachWorld
   del Flask NO se tocaron (globales de TODO el juego, y el reclamo de Cesar
   era que aspirar costaba DEMASIADO — decisión suya post-test); BrasaVida
   90→52 rechazado (perilla equivocada: la vida de la brasa no gobierna la
   lengua). PENDIENTES del acta: /900 del HUD del frasco, "?" del encargo
   colapsado, subtítulo del frasco con fade a los 12s, alpha del MENÚ·Esc,
   botón "omitir intro" para partidas repetidas (pedido de Cesar).
OPERATIVA: sandbox se REINICIÓ en plena ronda (2ª vez en dos días) y revirtió
   a 63b; se recuperó TODO desde el disco de Cesar (device_stage_files de los
   3 archivos recién desplegados) — regla 6b salvó la ronda por minutos.
   Snapshot local a6259ca. Verificado JUGANDO: frame cero (fuego + silueta +
   banner), diálogo 5 páginas con clics, aspirado 42 de agua en 5s, entrega
   15/15, grifo + costal de arcilla, gotera inmortal a t=45s.
ARCHIVOS: FundacionDirector.cs (reescrito ~900 líneas), SimLevelBuilder.cs
   (plano fundación), UiStyles.cs (Exito). ca_playtest65.cmd barre la ronda.

## Ronda 65 — EL MAESTRO DA ÓRDENES (feedback pt65: "insufrible, mucho texto")

Cesar tras la mini-prueba del 65: "no quiero history telling, no he definido
un lore; concéntrate en las ACCIONES y comunícalas breve pero claramente".
Y la dirección del personaje: el Maestro será alguien GIGANTESCO (quizás
solo se aprecie su mano) — imponente, concreto, da órdenes, es directo y NO
respeta mucho al jugador (asistente de una raza pequeña y ultrafiel; el
respeto se gana jugando). NO construir lore.

LO QUE CAMBIÓ (solo FundacionDirector.cs, textos):
- Diálogos de 21 páginas TOTALES a 12, todos en voz de ORDEN, cero
  exposición. Muestras: saludo (5→2): "Llegaste. Toma mi frasco. Te lo
  PRESTO — no lo pierdas." / "Esa gotera está inundando la poza. Tráeme 15
  sorbos de agua. Ya." · grifo (4→2): "La gotera ya es grifo. Sobre la
  poza. Se abre con E." · fogón encendido (4→2): "Arde. La turba deja
  CENIZA — no la tires." · vidrio (3→2): "Vidrio. Aceptable." · fin (3→2):
  "Agua, barro, fuego, vidrio. Empiezas a servir."
- MUEREN las líneas de lore: "el mundo se apagó", "los fenómenos se
  DOMESTICAN", "nada se pierde del todo", "esto ya es un taller". Si algún
  día vuelven, será decisión de lore explícita de Cesar, no mía.
- Panel de ENCARGOS en telegrama (el gesto ya vive en la banda de
  objetivo): "Agua de la poza — tráele 15." / "Crema de arcilla y agua —
  tráele 12." / "Turba al hogar vacío — 12." / "Vidrio del hogar encendido
  — tráele 8."
- Banner Mirar: "WASD / FLECHAS — ve con el Maestro, junto al fuego".
VERIFICADO en vivo con capturas: saludo completo en DOS clics, tipografía
grande, pie "continuar/cerrar · n/m" correcto, chapa oculta de cerca.
NOTA DE ARTE (para el futuro, NO ahora): la silueta greybox actual es
placeholder; el diseño final apunta a escala gigante (una MANO en cuadro
puede bastar). Anotado, sin construir.
ca_playtest66.cmd barre la ronda.

## Ronda 66 — LA FALSA PROFUNDIDAD 2.5D, v1 (dirección de Cesar + imagen aspiracional)

Mandato: capa de profundidad visual por planos SIN tocar la sim 2D. Los 6
niveles quedaron NOMBRADOS en Game/Capas.cs (tabla única, regla 39; los
valores históricos NO se renumeraron). Lo implementado y VERIFICADO en vivo:

1. ROCA MADRE CON CUERPO (SimRenderer.ComputeCellColor, StaticSolid):
   - canto DERECHO por fin sombreado (-12%; existía solo el izquierdo),
   - INTERIOR de masa -8% (celda sin caras al aire): el borde queda como aro
     iluminado y varias celdas chicas se leen como UNA roca ("lógica
     pequeña, apariencia más grande" -- el autotiling correcto aquí no son
     tiles: la lógica YA es 1 celda = 1 téxel),
   - ESQUINAS (2+ caras al aire) -12%: redondeo percibido, marching-squares
     gratis.
2. EL LABIO FRONTAL (Nivel 4): segunda textura del mismo grid
   (_frontTexture, sprite orden 55 = Capas.ArquitecturaFrente), rellenada en
   el MISMO barrido por chunks. Pinta SOLO el anillo de AIRE pegado a
   roca/piso (pardo #18130f, alfa 150): el aprendiz (orden 50) pasa POR
   DETRÁS al rozar muros = oclusión = profundidad. SOLO sobre vacío a
   propósito: si materia entra a la celda, el labio desaparece ahí (las
   reacciones jamás se tapan). La colisión NUNCA viene de aquí.
3. PISO ESTRUCTURAL (MaterialId.PisoEstructural=65, Count 65->66):
   StaticSolid que JAMÁS cae (arquitectura, regla 7), casi inmune al calor
   (245 raw), vocabulario ("piso estructural", no se bautiza). Se dibuja
   FABRIL: viguetas horizontales de 3 filas + remache cada 6 columnas, sin
   grano -- recto contra lo orgánico. Se coloca con el CINCEL: X alterna
   piedra/piso, y el piso REEMPLAZA roca madre al colocarse (verificado:
   losa flotante en el aire que no cae + columna de piso incrustada en el
   muro). Nunca sobre materia viva ni obra del taller. Tallable como la
   piedra.
4. COLISIÓN DEL APRENDIZ (ApprenticeController.ColisionConEstructura,
   global): caja 2x3 celdas por eje con SUBPASOS de 0.06u (a 11.2 u/s un
   frame son ~2 celdas: sin subpasos tunelaba pisos de 1 celda). Solo
   bloquean roca/piso; polvos/líquidos/gases se atraviesan. Si el frame
   arranca ya-dentro-de-sólido, la colisión se suspende ese frame (jamás
   quedas clavado). VERIFICADO: 2.2s empujando contra el muro = clavado en
   la celda 341 (antes: celda 170, dentro de la roca). El corral del
   greybox de la fundación SE RETIRÓ (prohibía la fantasía de tallar
   túneles; la colisión lo reemplaza).
5. CINCEL: alcance PROPIO de 22 celdas (ReachWorldCincel=2.2f; heredaba las
   60 del frasco) + LÍNEA DE VISIÓN Bresenham (PrimerSolidoEnRayo): solo se
   edita la cara que VES; editar detrás de pared avisa "hay pared de por
   medio — abre camino primero". La velocidad de tallado NO cambió.
   VERIFICADO: muesca en disco de 3-4 celdas tallada en la cara del muro.
   Sobre los "restos que no se come" (pt64): el disco por anillos SÍ cubre
   completo (round(dist)); las causas reales conocidas son la obra del
   taller (ya avisa desde pt43) y el borde del alcance -- con alcance corto
   + LOS esa clase de síntoma se encoge; si Cesar lo vuelve a ver, pedir
   captura con F3.
OPINIÓN DEL BOSQUEJO (registrada en chat): 8/10 como meta, alcanza para
   demo digna con 3 podas (toda máquina enseña su interior simulado o no
   entra; máx 2 tokens de clutter por pantalla; paleta fija de ~16 colores
   para consistencia entre sprites generados). La paleta pardo-latón-ámbar
   coincide con la UI del bautizo ya hecha.
DIFERIDO (siguiente pasada de esta dirección): vidrio frontal
   MachineBack->Sim->MachineFront por máquina (Capas.MaquinaFrente=35 ya
   reservado); chequeo de superficie válida al soltar máquinas con V
   (Mudanza); sprites de Niveles 0-2 (repisas/carteles/lámparas de pared);
   oscurecer el backdrop; migrar literales viejos a Capas.
RIESGO UX ABIERTO (reportar Cesar tras probar): sensación de la colisión
   (¿estorba volar?) y del alcance corto del cincel (¿22 celdas basta?).
OPERATIVA: verificado en vivo vía MCP con datos de grilla (mapas de celdas
   # / P / o) + capturas con amanecer forzado. Snapshot local 111e241.
   ca_playtest67.cmd barre la ronda.

## Ronda 68 — EL PULIDO DEL 2.5D (feedback pt67 con captura) + TODO GLOBAL

Cesar tras el pt67: construir se sentía TOSCO ("me marca constantemente
'abre camino primero' al completar detalles"), el personaje SE HUNDÍA en
roca/piso (captura), y "los bordes pintan tiles oscuros que entran en el
personaje". Y el mandato extra de mitad de turno: hacer lo pendiente y que
todo funcione en TODAS las versiones (caótico incluido).

1. LÍNEA DE VISIÓN v2 (Cincel): la v1 bloqueaba con el PRIMER sólido del
   rayo -- cada saliente fino y cada celda de la PROPIA OBRA recién
   colocada era un muro imaginario (extender una plataforma se
   auto-bloqueaba). Ahora: (a) CONSTRUIR NO TIENE LOS (añadir materia donde
   alcanzas no roba nada; regla 15 documenta la LOS de construcción
   retirada); (b) TALLAR mide GROSOR (SolidosAntesDelDisco): salientes de
   1-2 celdas se pulen sin drama, paredes de >=3 celdas en el rayo siguen
   bloqueando ("primero abre camino" vive donde importa). Aviso nuevo:
   "hay pared GRUESA de por medio".
2. CAJA DE COLISIÓN al tamaño visual: 2x3 -> 3x4 celdas (half 0.15/0.20) +
   8 puntos de muestreo (esquinas + medios de los 4 lados; sin los medios
   verticales un diente de 1 celda se colaba por el centro). Sigue cabiendo
   por un túnel de disco del cincel.
3. LABIO FRONTAL APAGADO (LabioFrontalActivo=false, static readonly
   anti-CS0162; regla 15): los "tiles oscuros" de la captura. A 1
   téxel/celda el anillo es blocky sin remedio; el volumen queda a cargo
   del sombreado de masa (que sí gustó). Infra conservada tras bandera; NO
   reactivar sin arte de borde real.
4. ACABADO POR CONTEXTO del piso (idea "preseteado" de Cesar): cada cara
   del piso se termina según lo que toca -- al AIRE canto neto de pieza
   fabricada (arriba +30, abajo -26, lados -16), contra ROCA junta de
   asiento (-8..-12: se lee encastrado). Los cantos/interior/esquinas/grano
   genéricos quedaron SOLO para la roca (antes se aplicaban doble).
5. APOYO ESTRUCTURAL AL SOLTAR (Mudanza, pendiente de la 66): las
   ESTACIONES (IMovibleAnclaEsquina: ancla=esquina de huella, garantía
   pt29) exigen >=70% de roca/piso/obra en la fila bajo su huella; aviso
   "necesita apoyo firme — construye piso o roca debajo". Grifos (ancla=
   boquilla, de pared) y seres quedan fuera a propósito.
6. FONDO MÁS OSCURO (pendiente de la 66): WorkshopBackdrop x0.74 en los DOS
   sitios de creación -- máquinas y latón saltan del muro.
GLOBAL: colisión/cincel/piso/fondo son de TODOS los modos por construcción
   (viven en ApprenticeController/Cincel/SimRenderer/WorkshopBackdrop, no
   en la fundación). VERIFICADO en partida CAÓTICA real: colisión activa,
   18 celdas de piso colocadas junto al taller clásico, fondo hundido con
   las estaciones destacando (captura).
SIGUE DIFERIDO (necesita pase propio con ojos de Opus, máquina por
   máquina): vidrio frontal MachineBack->Sim->MachineFront (Capas.
   MaquinaFrente=35 reservado); sprites decorativos de Niveles 0-2.
Snapshot local 6ee1c39. ca_playtest68.cmd barre la ronda.

## Ronda 69 — LOS DOS FANTASMAS DEL PT68 + EL SÁNDWICH DEL RECIPIENTE (piloto Crisol)

Feedback pt68 de Cesar: "se siente mucho mejor, suficientemente bueno por
ahora" (bordes verticales quedan para el futuro), y DOS detalles: (1) en
modo caos, "es obra del taller — no cede al cincel" sobre una pared que
debería poder borrarse (captura de un bloque rocoso normal); (2) el colider
mejoró pero AÚN insuficiente — "se sobrepone a la pared... podría quedar un
milímetro por detrás, al menos no debería atravesarla". Y el mandato:
"visto estos dos detalles, vamos con lo siguiente, lets go".

1. EL "ES OBRA DEL TALLER" FANTASMA — tres culpables, todos de la misma
   familia (piedra de aspecto normal protegida en silencio):
   a) ReservarSitioMufla registraba en ObraDelTaller un rect de ~39x15
      celdas de suelo/roca SIN NADA visible encima (la mufla "en el génesis
      es solo piedra normal", ronda 56) — y corría en TODOS los modos,
      cuando obra_mufla solo existe en el arco de Semilla Cero: en caótico
      esa roca quedaba anticincel PARA SIEMPRE. Es casi seguro el bloque de
      la captura (x41..79, la franja izquierda del cuarto).
   b) TallarTerraza registraba su montículo (Stone de perfil irregular,
      indistinguible de roca madre en pantalla).
   c) TallarPilastra ídem (se lee como "pared" colgando del techo).
   FIX: lista nueva SimLevelBuilder.ReservasDelPlano — la ven
   ColumnaOcupada/RectOcupado (el urbanismo del génesis: nada se talla
   encima del sitio de la mufla) pero NUNCA EsObraDelTaller (el cincel).
   La protección real de la mufla la registra Crisol.TallarEnPlano cuando
   la obra se construye DE VERDAD (y de paso muere el rect duplicado que
   dejaba el diseño viejo). Terrazas y pilastras ya NO se registran (regla
   15 documenta el porqué en cada método; verificado que ningún pase
   posterior dependía de esos rects — el escaneo de huecos es monótono).
   CRITERIO NUEVO: la protección anticincel es para MAMPOSTERÍA DE MÁQUINA
   visible; la piedra decorativa cede al cincel como cualquier roca.
   VERIFICADO en runtime (caos): 25 rects de obra, todos de estación/
   balda/fuente; RESERVAS=1 (la mufla); EsObra(55,141)=False.
2. COLISIÓN, SEGUNDA PASADA — la del pt67 subió la caja "al tamaño visual"
   A OJO sin medir el sprite. Medido contra GenerateBodyTexture: el cuerpo
   dibujado son 44x~76 px a 99ppu = 4.4x7.7 CELDAS (+bob ±0.04u); la caja
   3x4 dejaba ~0.7 celdas de cabeza dentro de pared por lado y ~1.6 arriba/
   abajo. Ahora: half 0.21x0.30 (4.2x6.0 celdas, cubre el macizo; solo
   rozan las puntas blandas: cuernos y barbilla) y CajaChoca es AABB EXACTA
   contra la grid (recorre las ~5x7 celdas tocadas; el muestreo de 8 puntos
   dejaba huecos de 3 celdas por arista y un diente de 1-2 se colaba).
   VERIFICADO en runtime: frontera exacta en 0.21 contra el muro (lejos
   3.23=False / cerca 3.19=True). COSTE ASUMIDO reportado a Cesar: un túnel
   HORIZONTAL de UNA pasada del cincel (5 celdas de luz) ya no da la talla
   (6.0) — hay que ensancharlo con otra pasada; los pozos verticales sí
   caben (4.2<5). Cesar eligió explícitamente este lado del trato.
3. "LO SIGUIENTE" — EL SÁNDWICH DEL RECIPIENTE, piloto en el CRISOL
   (MachineBack->Sim->MachineFront, diferido de la 66):
   · Capas.MaquinaFondoInterior = -8 (entre backdrop -10 y sim -5).
   · MaquinariaSprites.FondoInterior: panel opaco del interior (refractario
     oscuro 0x17/13/11, oclusión perimetral, luz tenue hacia la boca,
     hiladas insinuadas) — se ve por las celdas VACÍAS de la cámara: el
     hueco deja de enseñar el ladrillo del cuarto.
   · MaquinariaSprites.RebordeRecipiente: la pared CERCANA (orden
     Capas.MaquinaFrente=35), banda de 2 filas con canto iluminado y
     remaches que SOLAPA la base de la carga — la materia queda DENTRO sin
     taparla (las reacciones siguen protagonistas).
   · Montado en cámara Y cesto del brasero del Crisol (hijos del transform:
     se mudan solos con Reposicionar, regla 36).
   VERIFICADO JUGANDO con capturas (regla 52): cavidad oscura, arena
   pintada dentro, reborde comiéndole ~2 filas — captura_r69_carga2.png
   (las capturas quedaron en la raíz del proyecto, borrables).
   PENDIENTE (si el piloto convence a Cesar): extender a Prensa/Columna/
   Ensayo/Alambique/Pila máquina por máquina, cada una con su pase visual;
   sprites decorativos de Niveles 0-2.
TRUCO OPERATIVO NUEVO: captura SIN permisos de escritorio — RunCommand
   renderiza Camera.main a RenderTexture y escribe PNG en la raíz del
   proyecto; device_stage_files lo trae. OJO: el Play desde MCP puede
   quedar EN PAUSA (EditorApplication.isPaused=true → la textura de la sim
   no refresca y las capturas salen clavadas): comprobar isPaused y
   despausar antes de diagnosticar "no se pintó nada".
ca_playtest69.cmd barre la ronda.

### Ronda 69b — colider vertical cerrado (feedback pt69 con capturas del bob)

Cesar capturó los puntos MÁS NORTE y MÁS SUR del idle: paredes verticales
ya decentes ("un milímetro no fastidia"), suelo y techo "aún jodidos". La
causa: el BOB visual (±0.04u alrededor de transform.position) no estaba en
la caja. Caja ahora ASIMÉTRICA: MedioAltoArriba=0.40 (coronilla 0.364+bob),
MedioAltoAbajo=0.38 (barbilla 0.343+bob); solo las antenas (1px, puntas
blandas) pueden asomar en el pico del bob. Verificado en runtime: frontera
exacta en 0.38 contra el suelo. El trato del túnel no cambia (la pasada
horizontal única ya estaba bloqueada desde la caja 6.0).

PENDIENTE DE DECISIÓN (pedido grande de Cesar en el pt69): darle una vuelta
al game-feel de ASPIRAR/VERTER ("la actividad más recurrente, tiene que
sentirse una delicia"). Propuesta con opciones enviada — motas en tránsito
(Poltergust/Astroneer), frasco vivo (squash&stretch+pop), sonido que cuenta
el llenado (pitch por fracción), mundo que responde (remolino/salpicadura),
haz vivo. Esperando qué combinación elige antes de implementar.

### Ronda 69c — EL VIAJE DE LA MATERIA (juice de aspirar/verter, paquete 1+2+3)

Cesar en el pt69: "dale una vuelta a lo que ocurre en pantalla al absorber
y devolver — es la actividad más recurrente y tiene que sentirse una
delicia; busca referencias y propón". Propuesta de 4 opciones (motas en
tránsito / frasco que respira / sonido que cuenta el llenado / mundo que
responde); ELIGIÓ el paquete 1+2+3. Referencias: Poltergust de Luigi's
Mansion, pistola de terreno de Astroneer, catecismo Vlambeer. Diagnóstico:
la materia se TELETRANSPORTABA; la delicia está en hacerla VIAJAR.

1. MOTAS EN TRÁNSITO (Flask.cs, bloque "EL VIAJE DE LA MATERIA"): pool
   FIJO de 24 sprites (punto suave del haz reutilizado, orden 42 — sobre
   el haz 40/41, bajo las alas 47+). 1 mota por cada ~10 celdas movidas
   (CeldasPorMota), nacida EN la celda comida, color = baseColor del
   material. Bezier con comba lateral aleatoria; easeIn hacia el frasco
   (la succión tira, destino VIVO = CarryAnchor cada frame) y easeOut al
   verter (el chorro cae, crece al salir). Q (vaciar) = ráfaga (el pool
   la acota) + encogida x3. Capa 100% cosmética: Paint/PaintCell intactos,
   System.Random local de presentación (criterio DirectorDeAudio), cero
   allocs por frame. Si el pool está lleno, la emisión se pierde en
   silencio. Con modales (diario/álbum) las motas TERMINAN su vuelo vía
   ActualizarJuice en OcultarVisualesDeMundo (no quedan clavadas).
2. EL FRASCO RESPIRA: muelle amortiguado _pulso (k=170, c=11) — cada mota
   que LLEGA da +impulso (pop ~+17% de escala), cada tick de vertido da
   -impulso (el bote se aprieta). Se aplica al swatch (UpdateCarryVisual)
   y al TARRO en mano vía ApprenticeController.PulsoDelFrasco (campo
   _pulsoFrasco aplicado a _carriedFlaskTr.localScale en HandleVisual).
   GUARDA DE ESTABILIDAD (cazada EN la verificación, no teórica): Euler
   explícito con k=170 DIVERGE si dt>=~0.15s (un frame con hitch) — dt
   se capa a 0.05 en ActualizarJuice; mi prueba manual con dt=0.15 lo
   demostró clavando el clamp en -0.25.
3. EL SONIDO CUENTA EL LLENADO (DirectorDeAudio.cs): ReproducirOneShot y
   DispararLimitado aceptan pitchBase (default 1f — ningún otro llamador
   cambia); ActualizarPollerFrasco pasa 0.86+0.42·frac: aspirar sube de
   tono al llenarse (física de botella real), verter arranca agudo con el
   frasco lleno y cae — la variación aleatoria ±6% se multiplica encima.
VERIFICADO en vivo (captura_r69c_motas.png): 5 motas en vuelo tras 42
celdas aspiradas (la cuenta exacta: 3+2+0 por tick), reguero curvo color
arena hacia el frasco. MotaTamano subido 0.062→0.078 tras verla a zoom.
GESTOS PARA CESAR (regla 43): aspira un charco — reguero de motas curvas
del color del material entrando al frasco y el tarro dando pops; vierte —
gotas que salen frenando + el bote se encoge; el TONO del sorbo sube
conforme llenas y baja conforme vacías; Q = ráfaga.
PENDIENTE (opción 4, si tras probar quiere más): remolino en la boca de
succión, salpicadura al impactar el vertido, haz ondulante.

### Ronda 69d — ANTICIPACIÓN Y CIERRE (feedback de Cesar sobre el juice, "califica y aplica según tu criterio")

Cesar probó el 69c: "me gustó pero aún no se siente lo suficientemente
bueno". Tres ideas suyas, LAS TRES APLICADAS (calificación: las tres son de
manual — anticipación->acción->resolución es el patrón clásico de
animación, y el truco de sorting es profundidad gratis):

1. EL CARTELITO SE CANSA: "frasco vacío — aspira algo" salía CADA VEZ al
   verter en vacío ("es cansadísimo"). Ahora AvisarLimitado: cada aviso
   (vacío Y lleno, patrón idéntico) vive por EPISODIOS de ~2.5s (mantener
   el botón refresca sin gastar cupo) y tras 2 episodios se CALLA para
   siempre en la sesión — el jugador nuevo lo ve un par de veces (a prueba
   de burros), el veterano deja de sufrirlo. AvisosMax=0 lo apaga del todo
   si Cesar lo pide.
2. ANTICIPACIÓN -> ACCIÓN -> RESOLUCIÓN (flancos de botón en Update):
   · empezar a aspirar: INHALE (impulso negativo del muelle: el frasco se
     encoge un instante) y las motas esperan ~100ms (AnticipacionSeg) —
     SOLO visual, la celda se aspira desde el primer tick ("no metería
     delays reales que hagan torpe el control", textual).
   · soltar: POP de cierre (traga y asienta, SettleAspirar).
   · empezar a verter: el TARRO SE LADEA ±14° hacia el lado del cursor
     (MoveTowards 220°/s: llega en ~65ms, ANTES de la primera mota — la
     inclinación ES la anticipación del chorro) vía
     ApprenticeController.InclinacionDelFrasco; motas esperan la ventana.
   · cortar el chorro: asentamiento suave y el tarro vuelve a vertical
     (también al abrir un modal: _vertiendoVisual=false en
     OcultarVisualesDeMundo).
3. PROFUNDIDAD POR SORTING (idea textual de Cesar: "en los últimos píxeles
   pasan por delante del personaje... solo sorting, engañar elegantemente
   al cerebro"): el último 22% del viaje hacia el frasco (y el primero al
   salir) se dibuja en MotasOrdenFrente=53 — delante del cuerpo (50) y del
   tarro (52), debajo del swatch (60): la mota cruza por delante y muere
   EN la boca.
VERIFICADO en runtime (sondas): inclinación -14.0 en 2 frames / 0.0 tras
cortar; EmitirMota muda en ventana de anticipación, emite después; 0
errores de consola. Los flancos usan el input real (no sondeable por MCP):
la sensación final la valida Cesar jugando.
GESTOS (regla 43): verter en vacío avisa 2 veces y luego calla; al pulsar
aspirar el tarro se encoge un suspiro ANTES del reguero; al soltar, pop;
al verter el tarro se ladea hacia el cursor antes de la primera gota; las
motas cruzan POR DELANTE del imp justo antes de entrar al frasco.
ca_playtest69.cmd (git add -A) barre también esta pasada.

### Ronda 69e — EL HAZ QUE SE RETRAE + "+25% DE NOTORIEDAD"

Dos pedidos de Cesar tras sentir el 69d:
1. LA RETRACCIÓN DEL HAZ ("cuando termine de aspirar todo de un elemento
   la línea guía se debe encoger hacia el frasco, para no seguir
   presionándole"): _celdasJuiceTick se resetea AL PRINCIPIO de
   TickSuck/TickPour (antes de toda guarda) y el Update contabiliza
   _ticksSinMover tras cada tick; con >=3 ticks secos (~0.1s -- evita
   parpadeo con flujos intermitentes) la extensión del haz (_hazExtension
   0..1, multiplica `largo`) cae a 0 en ~220ms (HazEncogerVelPorSeg=4.5) y
   al refluir o re-pulsar crece desde el frasco en ~80ms
   (HazCrecerVelPorSeg=12 -- regalo colateral: cada arranque tiene gesto
   de DESPLIEGUE). Cubre todos los "no fluye": material bloqueado agotado,
   frasco lleno al aspirar, vacío al verter, sin hueco donde verter, fuera
   de alcance. Es el aviso DIEGÉTICO que sustituye al cartelito limitado
   de la 69d. En la rama !hazActivo la extensión se resetea a 0.
2. "+25% DE NOTORIEDAD" en las partes 1-2-3: motas 0.078->0.098 (~1 celda),
   CeldasPorMota 10->8 (~4/tick), pool 24->32, duración 0.16-0.26 ->
   0.20-0.32, comba 0.10-0.32 -> 0.13-0.40; muelle: llegada 2.2->2.8,
   inhale -1.6->-2.1, settle 1.3->1.7 / 0.8->1.0, vertido -1.1->-1.4;
   audio: pitch 0.86..1.28 -> 0.80..1.36 y volumen 0.18->0.22.
VERIFICADO en runtime (sondas): extensión crece con flujo vivo (0->0.06 en
2 invocaciones intra-frame) y se retrae en seco (1->0.98 ídem; direcciones
correctas, velocidades reales 80ms/220ms por frame de juego); 0 errores.
GESTO (regla 43): mantén aspirar sobre un charquito hasta agotarlo -- al
secarse, la línea guía se ENCOGE sola hacia el frasco (~0.2s): esa es la
señal de soltar. Apunta a material de nuevo sin soltar: vuelve a
desplegarse.
ca_playtest69.cmd (git add -A) barre también esta pasada.

### Ronda 69f — VERIFICACIÓN MULTI del juice (pregunta de Cesar)

Cesar: "asegúrate de que se vea también en el multiplayer, esa parte no la
contrasté antes". VERIFICADO POR LECTURA DE CÓDIGO (el camino completo):
en la escena MULTI el avatar LOCAL se cablea en Net/AprendizNet.Cablear,
que llama al MISMO Flask.Init(sim) de un jugador — y ahí dentro viven
BuildCarryVisual/BuildBeamVisual/BuildMotasVisual, así que motas, muelle,
inclinación, retracción del haz y avisos limitados funcionan para el
jugador local en multi POR CONSTRUCCIÓN, anfitrión E invitado (el frasco
del invitado predice contra el espejo y su Universe da los colores). El
audio también: SpawnDirectorDeAudio corre en AMBAS ramas del bootstrap de
red (la del invitado desde el playtest 43) con el flask local — la rampa
de pitch viaja igual. Los avatares REMOTOS tienen su Flask DESACTIVADO
(AprendizNet.OnNetworkSpawn) → no muestran su juice, exactamente igual
que su haz, que tampoco se veía: no es regresión. BACKLOG: replicar el
juice/haz de los otros jugadores (verse aspirar entre sí) es una feature
de red nueva, anotada.
LOS PLAYTESTS PENDIENTES: GitHub está en 7a49008 (rondas 66+68 completas,
verificado contenido por contenido en su momento). TODO lo de la 69 (a-e)
vive solo en el disco de Cesar. Como cada .cmd hace `git add -A`, correr
SOLO ca_playtest69.cmd reúne todo en un commit — no hay orden que respetar.

### Ronda 69g — EL MULTI ROTO DEL INVITADO: la fuga gemela de ModoFundacion

Cesar, con captura del lado del INVITADO: "se rompió el multi... está todo
desincronizado y rotísimo, revisa profundamente". Investigación por lectura
completa del camino de red (SimSync/AprendizNet/MaquinaReplica/AlkahestSim):

LO QUE NO ERA: el protocolo RLE está sano (encoder/decoder simétricos,
cap 255 con split correcto), el snapshot manda el grid ENTERO (864 chunks)
con NetworkDelivery.ReliableFragmentedSequenced (sin drops posibles), el
Start del sim espera a la sesión (SimSync.EnEscena), y las réplicas de
máquinas NO tallan ni borran piedra (MaquinaReplica.Reposicionar solo
pide). Parte del look "pobre" del invitado es DISEÑO del POC pt30: las
máquinas remotas son SILUETAS simplificadas (un sprite escalado), no el
cuerpo completo con mampostería -- deuda conocida, anotada abajo.

LA FUGA CONFIRMADA: `AlkahestGameBootstrap.ModoFundacion` es un estático
que enciende el botón "EL INICIO — fundación" del título (EL PRIMER botón
que pulsa cualquier jugador nuevo, ronda 60) y que NADIE apagaba en el
camino multi -- mientras que ModoSemillaCero tiene TRES resets (lobby,
snapshot del invitado, OnNetworkDespawn). Un invitado que pasó por la
fundación y luego se une a una sesión:
  · CrearMundoInterno le aplicaba Universe.AplicarOverridesSemillaCero
    SOBRE la seed del anfitrión -> paleta, identidades del retículo y
    umbrales de OTRO universo: el espejo pinta el mundo del anfitrión con
    los colores/materiales equivocados EN TODA LA SESIÓN.
  · Y construía BuildFundacion (mundo casi vacío) como base del espejo --
    el snapshot corrige mat[] entero, pero el Universe ya quedó torcido.
Un ANFITRIÓN con el flag pegado era aún peor: hosteaba el plano de la
fundación como mundo compartido.
FIX (simétrico al de ModoSemillaCero del pt50): ModoFundacion=false en los
TRES sitios -- rama anfitrión de OnNetworkSpawn (antes de
CrearMundoAnfitrion), rama invitado de AlRecibirChunks (antes de
CrearMundoEspejo) y OnNetworkDespawn. La fundación es de UN jugador; si
algún día hay fundación co-op, su botón de lobby pondrá el flag.

+ LA LÍNEA DE LA VERDAD (AlkahestSim.CrearMundoInterno): un Debug.Log al
construir cualquier mundo con plano/seed/espejo/flags/overrides -- comparar
esa línea entre las dos consolas responde en segundos si ambos lados
construyeron el mismo universo. Para el próximo reporte de desync: pedir
captura de consola (o Player.log) de AMBOS lados.

SIN VERIFICAR EN VIVO (requiere dos instancias + Steam): compilado limpio
(fiel + editor); la verificación real es el próximo test de Cesar con su
amigo -- AMBOS con el build nuevo (los dos lados deben compartir formato).
BACKLOG SUBRAYADO POR ESTA RONDA: réplicas de máquina del invitado con el
visual completo (hoy siluetas del POC pt30) -- es probablemente la mitad
del "se ve rotísimo" que quedará tras el fix.

## Ronda 70 — DOMINGO DE AJUSTES: máquinas atravesables, esquinas suaves, parallax, título

ANTES DE EMPEZAR, CUARTO REINICIO DEL SANDBOX: el repo local revirtió a la
63b y el rig perdió las DLLs. Recuperación: git reset a origin (Cesar SÍ
corrió ca_playtest69.cmd: GitHub tiene la 69a-e completa en 6770d86), PERO
el fix 69g del multi NO alcanzó ese push -- recuperado del disco de Cesar
vía device_stage_files (verificado por contenido) y va en el commit de esta
ronda. Rig reconstruido (155 DLLs); OJO: la DLL "SteamNetworkingSockets
Transport for Netcode for GameObjects.dll" lleva ESPACIOS y rompe el
compile_fiel.sh (REFS sin comillas) -- en /home/claude/unityrefs se renombra
a SteamTransportNGO.dll (la identidad del ensamblado vive en el metadato,
no en el nombre del archivo).

Los cuatro ajustes de Cesar:
1. EL PERSONAJE NO COLISIONA CON LAS MÁQUINAS ("que la gestión del
   laboratorio no se convierta en una trampa"). Decisión: nuestra colisión
   es la GRILLA leída directo (cero colliders de Unity, nada de capas) --
   la mampostería de máquina es Stone pero está registrada en
   ObraDelTaller, así que CajaChoca la EXCLUYE: solo bloquean roca madre y
   piso estructural. Sonda en runtime: caja centrada sobre el muro de una
   estación = False; sobre roca madre = True.
2. ESQUINAS SUAVES ("muy pixel perfect: si toca un poquito ya no pasa el
   mono"). Su idea de Composite/Tilemap Collider no aplica (no hay
   colliders que fusionar); la tosquedad eran las ESQUINAS AFILADAS de la
   AABB. Dos suavizados en ApprenticeController:
   · CHAFLÁN (ChaflanCeldas=2): la caja pasa a OCTÁGONO -- las celdas del
     rincón no cuentan, rozar un diente de 1-2 celdas ya no frena.
   · DESLIZAMIENTO ASISTIDO (IntentarAsistencia, sondas 0.5/1.0/1.5
     celdas): si un paso choca pero desplazándose en el eje perpendicular
     hay hueco, el imp RESBALA alrededor del borde sin perder velocidad --
     la corner correction clásica. El avance bloqueado de verdad sigue
     frenando el eje como siempre.
3. PARALLAX LEVE (WorkshopBackdrop): el muro de fondo (-10) se desplaza un
   3% del recorrido de la cámara (su rango 2-5%), escalado 1.03 y
   recentrado para no descubrir bordes en los extremos del mundo. SOLO el
   muro: los herrajes están anclados a geometría real de la grilla.
   Sonda en runtime: posición real == esperada al 3%, escala 1.03.
4. TÍTULO: "LIMO PRIMORDIAL" -> "TEN THOUSAND YEARS" (el nombre vigente
   del proyecto, GDD ronda 60) + descripción nueva "Diez mil años de
   servicio empiezan con un frasco prestado." (liga con el beat 1 sin
   inventar lore, regla del pt65) + EL TELÓN: DrawTitle ya no usa el velo
   0.72 (que dejaba ver el mapa "feo" detrás) sino un fondo OPACO de tinta
   parda (0.085/0.072/0.062); los overlays de jornada conservan su velo
   translúcido a propósito (ahí ver el taller es parte del gesto).
   VERIFICADO con captura de escritorio: título dorado + telón sin mundo.
ca_playtest70.cmd barre la ronda (incluye el 69g rescatado).

## Ronda 71 — TRES PUERTAS, ESLOGAN DEL GDD, UNIFICACIÓN DE NOMBRES, -40% DE VELOCIDAD

Feedback de Cesar sobre la 70 + mandatos nuevos. Diagnósticos primero:
· "NO VI EL PARALLAX": dos causas reales. (a) EL PRÓLOGO NO TENÍA FONDO --
  SpawnFundacion retornaba antes de la línea que crea WorkshopBackdrop, así
  que detrás de la piedra estaba el color plano de cámara y el parallax no
  tenía nada que mover. FIX: la fundación ahora spawnea el backdrop (visual
  puro; la viñeta del director lo oscurece donde toca). (b) LA BUILD ESTABA
  VIEJA: ver abajo.
· "LAS MEJORAS SOLO SE VEN EN UNITY, NO EN LA BUILD": la build que probó es
  ANTERIOR a las rondas del juice -- el .exe no se reconstruye solo. Tras
  bajar esta ronda: Ctrl+R y menú "Ten Thousand Years/3. Build demo
  Windows" (o 4 para MULTI) -- la primera build nueva saldrá ya a
  Builds/TenThousandYearsDemo|Multi con TODO lo acumulado.
· "ME DA LA IMPRESIÓN QUE ACELERA": SUSTENTO CONFIRMADO -- MoveTowards con
  acceleration=44 tardaba 0.25s en llegar a tope. moveSpeed 11.2 -> 6.7
  (-40% pedido) y acceleration 44 -> 96 (rampa 0.07s: conserva un pelo de
  suavizado anti-robótico pero ya no se LEE como acelerar).

LAS TRES PUERTAS (DayCycle.DrawTitle -- SOLO etiquetas y orden visual, cada
botón conserva flags/seed/punto de inicio EXACTOS, mandato textual de no
fusionar Normal y Caótico todavía):
  · "PRÓLOGO — la fundación"        (ex "EL INICIO — fundación (greybox)")
  · "MODO NORMAL — SEMILLA CERO"    (ex "SEMILLA CERO — tu primer taller")
  · "MODO CAÓTICO — semilla libre"  (ex "...entrar con esta semilla"), con
    su campo de seed intacto.
ESC->PAUSA->volver al título ya funcionaba en los tres modos (la fundación
spawnea DayCycle desde el fix 62b); verificado InputLocked=False en prólogo.
ESLOGAN OFICIAL bajo el título (GDD §1, versión española): "Reconstruye el
conocimiento humano con barro, fuego y observación."

UNIFICACIÓN DE NOMBRES (mandato: "no puede quedar vestigio que genere
confusión a un dev futuro"):
  · Barrido global "ChaosAlchemy" -> "TenThousandYears" en 34 .cs (tags de
    log [TenThousandYears], nombres de clips/texturas, PrefKeys -- OJO: el
    volumen guardado y la pref del DevPalette se resetean UNA vez).
  · Menús de Unity: "Alkahest/1..5" -> "Ten Thousand Years/1..5".
  · Builds: Builds/TenThousandYearsDemo/TenThousandYears.exe y
    Builds/TenThousandYearsMulti/TenThousandYearsMulti.exe. (El rig
    compile_fiel se re-stagea desde la carpeta NUEVA tras la primera build
    multi; la vieja ChaosAlchemyMulti puede borrarse entonces.)
  · PlayerSettings.productName = "TEN THOUSAND YEARS" (título de ventana).
  · "SpaceWar" EN STEAM: NO es vestigio nuestro -- steam_appid.txt=480, el
    app de PRUEBAS de Valve (Spacewar). Saldrá así hasta comprar el appid
    propio en Steamworks ($100, fase de lanzamiento); no hay nada que
    limpiar en el código.
  · QUEDA COMO FASE DE LIMPIEZA (GDD §13, NO tocado hoy a propósito --
    romper refs de escena/asmdef merece ronda propia): namespace Alkahest.*,
    escena AlkahestLab, asmdef Alkahest.Runtime, repo GitHub `Alkahest`.
VERIFICADO con captura: menú "Ten Thousand Years" en la barra de Unity,
título con eslogan y tres puertas, telón opaco, log [TenThousandYears];
sondas: backdrop vivo en prólogo, moveSpeed=6.7, accel=96.
CUARTO REINICIO DE SANDBOX documentado en la ronda 70; el rig renombra
"SteamNetworkingSockets Transport...dll" -> SteamTransportNGO.dll (espacios
rompen el script). ca_playtest71.cmd barre la ronda.

### Ronda 71b — la velocidad que NO se aplicaba en multi, parallax perceptible, builds en ventana

· VELOCIDAD, INTUICIÓN DE CESAR CONFIRMADA CON DATO: el prefab
  Net/AlkahestAprendizRed.prefab serializaba moveSpeed: 11.2 /
  acceleration: 44 -- un [SerializeField] guardado en prefab PISA el
  default del código, así que la escena MULTI (editor Y build) volaba a la
  velocidad vieja por mucho que el código dijera 6.7. FIX DE RAÍZ: se
  retira [SerializeField] de ambos campos -- el dato huérfano del prefab
  queda ignorado por la semántica de serialización y el CÓDIGO pasa a ser
  la única fuente de verdad en todos los modos y builds. (Trampa nueva
  para la lista: pariente de la regla 36 -- un valor afinado en código
  puede estar siendo pisado por un prefab/escena serializado años atrás;
  ante "cambié el número y no cambió nada", grep al .prefab/.unity.)
· PARALLAX "no vi nada que se moviera atrás en ningún caso": causa hallada
  en la segunda mirada -- el 3% es IMPERCEPTIBLE contra ladrillo uniforme
  (tras cruzar una pantalla entera la divergencia acumulada es 3% del
  ancho). FactorParallax 0.03 -> 0.08 (los fondos cercanos clásicos usan
  10-20%; 8% sigue leve), MargenParallax 1.03 -> 1.06.
· BUILDS EN VENTANA (pedido): los DOS build scripts fijan antes de
  compilar PlayerSettings.fullScreenMode=Windowed, 1600x900,
  resizableWindow=true, allowFullscreenSwitch=true -- las builds nuevas
  arrancan en ventana redimensionable (para dividir pantalla con Unity) y
  Alt+Enter alterna a pantalla completa. Versionado en el script, no a
  mano en Project Settings.
Recordatorio vigente: REBUILD tras bajar esto (Ctrl+R + "Ten Thousand
Years/3" o "/4") -- las builds no se reconstruyen solas.
ca_playtest71.cmd (git add -A) barre también esta pasada; si ya se corrió,
usar ca_playtest71b.cmd.

### Ronda 72 — la gran depuración para trabajar con un externo (el hermano de Cesar, dev Unity)

Pedido de Cesar: revisar el proyecto completo y "bajar de peso" — quitar de los MD la
información de "de dónde venimos" que no suma, cazar nombres viejos que quedaran, reducir
archivos, mejorar el orden y la trazabilidad; todo como paso previo a la escenificación
(mover piezas generadas por código a la escena para que su hermano intervenga desde el editor).

· DOCS REESTRUCTURADOS EN TRES CAPAS:
  - `README.md` (raíz, NUEVO): la puerta de entrada para un dev externo — qué es el juego,
    cómo abrir/correr/build, controles, mapa del código por carpeta, 10 reglas de oro,
    flujo de trabajo y próximo gran paso. Reemplaza el README del template FriendsLoop.
  - `docs/ESTADO.md` (NUEVO, documento vivo): dónde estamos por sistema, backlog priorizado,
    tabla del código APARCADO con evidencia de referencias, y los DOS PLANES escritos:
    ronda estructural (renombre namespace/escena/asmdef/repo + poda, 6 pasos) y
    escenificación (4 fases: visual puro → prefabs de estación → nunca-a-escena la sim →
    generadores pasan de crear a validar).
  - `CLAUDE.md` REESCRITO COMPACTO: solo operativa del agente + catálogo de reglas con
    NUMERACIÓN ESTABLE R7–R59 (el código las cita por número). La versión narrativa
    completa quedó en `docs/archivo/CLAUDE_v1_completo.md`. Reglas nuevas: R58
    (SerializeField en prefab pisa el código) y R59 (flags estáticos de modo se resetean
    en todos los caminos multi + línea de la verdad).
· ARCHIVO: 27 documentos históricos movidos a `docs/archivo/` (CONTRATO_*, INFORME_*,
  DISENO_*, DECISIONS, INCIDENTES, PIVOT, MANUAL_MAQUINAS desactualizado, y
  HANDOFF.md → HISTORIAL_RONDAS.md, este archivo). docs/ vivo queda en 4:
  GDD, GUION_TESTERS_GREYBOX, SIM_NOTES, ESTADO.
· LIMPIEZA DE DISCO (mv a `_to_delete/`, device_bash no puede borrar; Cesar la vacía
  cuando quiera): cmds ya pusheados (ca_playtest70/71/71b — verificado en git log),
  zips viejos, capturas sueltas, y las builds muertas ChaosAlchemyDemo/,
  ChaosAlchemyMulti/ y FriendsLoopDemo/ (los respaldos "Historica Chaos" y
  "Respaldo_TramoA_Multi" quedaron INTACTOS).
· NOMBRES: barrido final — el código quedó en cero menciones a ChaosAlchemy y cero
  menús "Alkahest/" (último vestigio: un docblock en AlkahestBuildTools.cs, corregido).
  Los docs vivos también en cero; el archivo conserva los nombres viejos a propósito
  (es historia). Legado consciente y documentado en CLAUDE.md: namespace `Alkahest.*`,
  escena AlkahestLab, asmdef Alkahest.Runtime, repo GitHub — su renombre ES la ronda
  estructural planificada en ESTADO.md.
· CÓDIGO APARCADO AUDITADO, NO BORRADO (con evidencia): Criatura/Capullo/SerSprites
  referenciados por 7-9 archivos (volverán como organismos-solución, decisión de Cesar);
  taller clásico enterrado bajo la piedra del génesis (rama viva en AlkahestSim);
  BuildCuna/BuildRepisa/PlaceNutrienteMound/BuildTolvaCercana sin llamantes pero con
  su porqué en docblocks. Todos son candidatos de la PODA de la ronda estructural,
  que se hará con Unity abierto y partida completa de verificación — no hoy a ciegas.
· Sin cambios de gameplay: el único .cs tocado fue un comentario. compile_fiel EXIT=0.
ca_playtest72.cmd barre la ronda.

### Ronda 73 — EL PRÓLOGO REHECHO: presentar el verbo (con los ojos de Opus)

Espec completa de Cesar: rework breve del prólogo, reutilizando lo que funcione, con UNA
prioridad — que el verbo del juego (absorber y verter materia) quede clarísimo. Inicio
oscuro con VEN. y WASD contextual; TOMA. y el frasco; el agua FLUYENDO físicamente por el
escenario; entrega en receptor placeholder; un derrumbe cinematográfico que abre la gotera
de lodo (el Maestro NO entrega materiales); recompensa: el depósito de agua de su imagen de
referencia, emergiendo cinematográficamente, usable, con autofill insinuado. Dos canales de
UI separados (voz narrativa vs tutorial funcional), tutoriales que validan RESULTADOS y no
teclas, cero checklist ("Sí. Eso era.", no "objetivo 1/7"), y todo como esqueleto editable.

· FundacionDirector REESCRITO (v2): beats Despertar→Ven→Toma→Agua→EntregaAgua→Derrumbe→
  Lodo→EntregaLodo→Recompensa→Fin, con el bloque EL GUION (palabras de la voz, cantidades,
  tiempos, triggers, radios de luz, caudales) junto y editable. Se conservan de la v1: la
  viñeta de luz con causa física, la silueta del Maestro, su fuego de Brasa real, LuzEn y
  DibujarBandaMaestro (Trueque). Retirados (regla 15, viven en git/este archivo): el arco
  barro→fogón→vidrio→estante→tablón (volverá tras el verbo), el diálogo por páginas, la
  banda de objetivo, la gotera de estalactita, el vertido del Maestro y los pedidos guiados.
· LA VOZ DEL MAESTRO: palabras sueltas enormes (VEN. TOMA. AGUA. TRÁELA. BIEN. LODO.
  TRÁELO. OBSERVA.), centro-alto, sombra, deriva ascendente, sin panel y SIN bloquear input;
  clip subgrave propio (SintetizadorSfx.VozDelMaestro) con pitch determinista por palabra.
  Cola de una palabra: dos voces jamás se pisan.
· TUTORIAL CONTEXTUAL (Game/TutorialContextual.cs, nuevo y reutilizable): fichas de tecla
  blancas opacas bajo el jugador; se iluminan al pulsar, se CONFIRMAN al validar el
  resultado (desplazamiento real por dirección con tecla — el bob idle no cuenta; materia
  que de verdad entró/salió del frasco), destello + blip + 0.5 s + fade. Sin contadores.
· LA CASCADA: manantial que brota del muro izquierdo y baja por dos repisas hasta la poza
  (tres caídas francas), caudal del director + el rezumado de la poza como cierre del ciclo
  (equilibrio verificado en vivo: 34/34). El agua se presenta EN MOVIMIENTO.
· EL DERRUMBE: plano cinematográfico (FocoCinematico + Sacudida nuevos en SimRenderer, con
  la viñeta siguiendo el foco), rugido sintetizado, grieta en cono en la bóveda, tromba de
  26 terrones, cráter excavado por el impacto, y gotera de lodo PERMANENTE con tope de
  montículo e histéresis (fuente no agotable que espera si nadie la cosecha). El lodo es
  arcilla en polvo: SE APILA — el contraste con el agua es el aprendizaje.
· EL CUENCO DEL MAESTRO (receptor placeholder): entrega = VERTER dentro (el verbo, no un
  menú); placa de mundo con n/m; al completar, el Maestro lo TOMA drenándolo a la vista.
  El lodo mojado (barbotina por agua residual) cuenta y se drena también.
· EL DEPÓSITO DE AGUA (Game/DepositoDeAgua.cs + sprites TanqueMarco/Fondo/Tubo según la
  referencia): emerge del suelo tras el segundo BIEN. (oculto tras la roca mientras sube,
  terrones a los flancos, temblor), muros de piedra a la sim al asentarse (esperando a que
  el jugador no esté en medio — jamás emparedar), primer llenado a la vista con agua REAL
  aspirable a través del vidrio-sprite, y autofill lento con histéresis. Amanece y el
  tablón despierta (Trueque.Activar). Referencias de Cesar archivadas en docs/ref/.
· FRASCO Y CINCEL BLOQUEADOS hasta el TOMA. (FundacionDirector.FrascoBloqueado, guardas en
  Flask/Cincel/ApprenticeController): en el inicio oscuro no llevas herramientas.
· REVISIÓN "OPUS CON OJOS" pre-playtest (pedido de Cesar: "con los ojos de Opus"): 21
  hallazgos, 3 bloqueaban el arco — (1) sin guarda de pausa un ESC se comía el derrumbe
  entero contra la sim congelada; (2) el lodo disuelto en barbotina rompía el contador de
  la entrega; (3) los muros del tanque podían emparedar al imp sin salida. Todos aplicados,
  más: voces que no se pisan, respiros medidos desde el evento real, cono del derrumbe sin
  celdas huérfanas, rezumado a cadencia fija (no por frame), repisas/cuenco como obra
  anticincel, fichas sin allocs por frame, PaintStable en los muros (regla 22).
· VERIFICADO EN VIVO (MCP, jugando por sondas + 3 capturas): cascada fluyendo y poza en
  equilibrio, arco COMPLETO Despertar→Fin recorrido (frasco entregado, HUD despierto,
  entregas drenadas, derrumbe con grieta+cráter+montículo 74, depósito emergido y LLENO en
  régimen de autofill), repetibilidad limpia (reinicio → Despertar, techo restaurado, tanque
  inexistente, flags a cero), 0 errores y 0 warnings de consola en toda la sesión.
Queda para el playtest de Cesar: la SENSACIÓN (voz, fichas, luz, tiempos — todo en EL
GUION), y la validación por INPUT real (las sondas confirman la máquina de estados; las
teclas físicas las prueba él). ca_playtest73.cmd barre la ronda.

### Ronda 74 — el feedback del primer playtest del prólogo ("me encantó, solo unas modificaciones")

· GIT DESATASCADO: el ca_playtest73.cmd falló con "index.lock: File exists" — el lock lo dejó
  huérfano un `git status` corrido por el puente de Claude, que NO puede borrar archivos (el
  unlink del lock falla en el montaje). Movido a _to_delete/ y LECCIÓN OPERATIVA: todo git de
  solo lectura por device_bash va con `--no-optional-locks`. El cmd de esta ronda barre las
  DOS rondas (73+74).
· WARNING CS0162 (Crisol.cs:1638, reporte de Cesar): la guarda `if (CamaraAlto <= 1) return`
  con CamaraAlto const=9 se plegaba a código inalcanzable. Retirada con nota: la división es
  segura por construcción mientras sea const > 1.
· DEPÓSITO SIN AUTOFILL (corrección de diseño de Cesar): el tanque ya NO se rellena solo ni
  insinúa tubo — eso llega con la tubería de conexión infinita (ref 2). Emerge con un CULO
  de agua (14) y la TAREA FINAL nueva es LLÉNALO. — el verbo por última vez, al revés:
  verter dentro de algo tuyo (beat LlenarDeposito, placa "LLÉNALO — n/m", meta 48). Al
  completar: BIEN., amanecer y Trueque.
· FONDO DE RUINA: el prólogo deja el ladrillo del taller — WorkshopBackdrop.PintarFondoRuina:
  vacío casi negro con paños de mampostería superviviente de bordes mordidos, tres vigas
  caídas en silueta y escombro al pie. "Estamos reconstruyendo porque algo pasó." Sin
  herrajes (baldas/cadenas son el taller vestido).
· BORDES MORDIDOS: erosión determinista por tramos en bóveda y muros + suelo roto en el ala
  izquierda (huecos y escombros de 1). CAZADO EN VIVO EN LA PRIMERA VERIFICACIÓN: una
  mordida en (339,172-173) pegada a la repisa alta le daba al arroyo una escalera DIAGONAL
  (el paso esquinado de los líquidos falling-sand) y el caudal entero encharcaba el suelo —
  el muro izquierdo no se muerde en y152-178 (sello de la veta + TODA la banda de la
  cascada): la destrucción muerde, la fontanería no.
· POZOS HONDOS: poza y cuenco a 5 de profundidad (FundacionPozoHondo) — la materia se
  acomoda a la vista; equilibrio de poza 34→48.
· EL RINCÓN A LA DERECHA: brasas 424-428→428-432, mesa 434-446→436-448, nicho del trueque y
  cuenco corridos con ellos: 6 celdas + murete entre el depósito y el fuego — verificado:
  CERO vapor junto al tanque (antes hervía).
· NRE del parpadeo (ApprenticeController.ScheduleNextBlink) en hot-reload: _rng
  (System.Random) resucita null en un domain reload — mismo patrón del diálogo 64b; `??=`.
· VERIFICADO EN VIVO: arco completo hasta el nuevo Fin (tanque en 59/48, amanecer, 0
  errores/warnings), fuga sellada (0 celdas fuera del camino, poza 48/48), fondo de ruina
  montado y el del taller ausente, plano nuevo sondado celda a celda, capturas.
ca_playtest74.cmd barre las rondas 73 y 74 juntas.

### Ronda 75 — LA ESCENIFICACIÓN del prólogo (arquitectura híbrida Scene/Prefab + código)

Mandato de Cesar: migración QUIRÚRGICA para poder retocar el juego desde Unity sin pedir
código por cada ajuste — empezando por el prólogo para trabajar con su hermano — con criterio
de autoridad por elemento, sin big-bang, por familias validadas, y reversible.

· FAMILIA 1 — EL GUION COMO ASSET: Game/GuionDelPrologo.cs (ScriptableObject) con TODOS los
  textos/cantidades/tiempos/triggers/radios/caudales/layout de UI del prólogo. El director y
  el depósito leen `_g` (solo lectura); los defaults viven en los inicializadores del SO y
  entran solos si el asset falta. VERIFICADO: vozVen editada en el asset → el director la dice.
· FAMILIA 2 — LA ESCENOGRAFÍA EN ESCENA: Game/PrologoEscenografia.cs, raíz
  "Prologo_Escenografia" con marcadores movibles (gizmos etiquetados): MAESTRO (posición y
  escala de la silueta Y de los triggers de proximidad — mover el marcador mueve al Maestro
  entero) y DEPÓSITO (base-centro, ajustado a celdas). El director monta y dispara SOBRE los
  marcadores; jamás escribe en ellos. VERIFICADO JUGANDO: marcador movido 1u + escala 1.3 →
  silueta y trigger AHÍ (VEN→TOMA→Agua completados en la posición nueva); depósito x0=418
  siguiendo su marcador.
· FAMILIA 3 — ARTE HORNEADO + PREFABS: Editor/PrologoBakeTools.cs (menú "6. Hornear arte del
  prólogo"): baja a PNG los sprites procedurales (Maestro, TanqueMarco/Fondo, y el telón de
  la ruina 2304x864 — con el pintor por filas EXTRAÍDO a estático compartido: horneado y
  procedural son la misma verdad de píxeles), crea DepositoVisual.prefab (hijos Fondo/Marco;
  solo si falta: sus ediciones son sagradas) y GuionDelPrologo.asset (ídem), y ata todo a los
  campos VACÍOS de la escena. El Maestro queda VISIBLE en el editor (hijo Silueta del
  marcador). El runtime prefiere prefab/asset y cae al procedural si faltan.
· LOS GENERADORES YA NO ARRASAN: menú 1 → "Validar-completar" (abre la escena real, añade
  solo lo que falte, JAMÁS pisa lo existente); el pre-vuelo de builds pasa por ahí (la regla
  14 evoluciona: la build valida, no destruye). El botón rojo quedó aparte: "1b. REGENERAR
  DESDE CERO" con diálogo de confirmación (recuperación R20). Bootstrap: find-or-create del
  WorkshopBackdrop (la escena puede traer el suyo con el sprite horneado; la escena manda
  sobre su transform — el parallax usa la variante "tal cual", sin reescalar).
· VERIFICACIÓN QUIRÚRGICA EN VIVO: retoque manual simulado (marcadores movidos/escalados +
  palabra del guion) → guardado → MENÚ 1 → TODO SOBREVIVIÓ → Play → el código lo respetó
  (sondas + captura del Maestro al 130% fuera de su mesa). Regresión del MODO CLÁSICO:
  Semilla Cero arranca con 1 solo backdrop (el de escena), fondo del taller correcto, sim y
  Crisol vivos, 0 errores. MULTI: sin código de red tocado; su escena no trae backdrop → el
  find-or-create crea el histórico (comportamiento idéntico); prefab de red intacto. Los
  retoques de prueba se RESTAURARON: la escena queda en el estado histórico limpio.
· LO QUE SE QUEDÓ EN CÓDIGO A PROPÓSITO (explicado en ESTADO.md): la UI IMGUI (migrar a
  uGUI = reescribir toda la capa de UI; el guion ya da los números), la geometría tallada de
  cascada/poza/cuenco/cráter (la sim es la verdad), y el fuego del Maestro (Brasa real).
· La matriz de autoridad completa y las familias PENDIENTES (máquinas del taller a prefabs,
  decoración, escena multi) quedaron en ESTADO.md como receta para repetir el criterio.
ca_playtest75.cmd barre la ronda (incluye la escena guardada, los PNG, el prefab y el guion).

### Ronda 76 — estabilidad del multiplayer (los ojos de Opus sobre Net/) + el prólogo es solo-single

(QUINTO REINICIO DEL SANDBOX al arrancar la ronda: el repo local revirtió a la 63b. Recuperado
COMPLETO desde el disco de Cesar con un tar vía device_bash — GitHub estaba atrás porque los
cmds 73-75 aún no corren. Lección nueva en CLAUDE.md: git de lectura por el puente SIEMPRE con
--no-optional-locks.)

· EL PRÓLOGO ES SOLO-SINGLE (pedido de Cesar): invariante explícita con dos guardas — el
  bootstrap apaga ModoFundacion (y HudPermitido/FrascoBloqueado) con acuse al entrar a la
  escena MULTI, y FundacionDirector se autodestruye con error si algún día naciera en una
  escena de red. La arquitectura ya lo impedía (bifurcación SimSync.EnEscena + R59); ahora
  además se DICE.
· REVIEW DE ESTABILIDAD MULTI (agente Opus, Net/ entero + costuras): veredicto — el
  protocolo (RLE, permisos RPC, cerrojos, altas tardías) está sano; lo que rompía el multi
  era EL CICLO DE VIDA: el juego no sabía terminar una sesión que él no decidió terminar, y
  una decena de estáticas de proceso sobrevivían al fin de partida. 12 hallazgos aplicados:
  1. (CRÍTICO) Fin de sesión INVOLUNTARIO (host cierra / cae transporte): el invitado quedaba
     en la escena sucia — _spawned=true, espejo con seed vieja rechazando chunks EN BUCLE,
     réplicas huérfanas, catch-up del saber "ya hecho". Fix: TallerSesionHud escucha
     OnStateChanged y cualquier caída a Offline desde sesión viva dispara LA MISMA recarga
     de escena del botón SALIR (con aviso estático que sobrevive a la recarga). La deuda
     pt53/pt55 completa, cerrada.
  2. (BUG) El invitado FABRICABA temperatura: su espejo no replica temp[], así que vertía a
     20°C y PISABA la temperatura real del mundo del host (lavandería térmica). Fix: lo que
     un cliente vierte nace a temperatura estable (R22) hasta que temp[] viaje en el RLE.
  3-5. (CRÍTICOS) Estáticas clavadas que mataban sesiones futuras: JournalHud.Abierto (el
     gemelo exacto del fix pt55 de AlbumReal — mataba haz/cincel/mudanza para siempre),
     UiStyles.EscribiendoTexto (rito de bautizo muerto abierto = TODAS las teclas muertas),
     y Cincel/Mudanza/Termometro.ModoActivo (cincel pegado = frasco del invitado
     IRRECUPERABLE). Fix: OnDestroy baja cada una — con guarda `enabled` para que el Destroy
     del avatar de un amigo que se va no te apague TU modo.
  6. (BUG) Cerrojo de mudanza eterno: invitado agarra máquina con V y se desconecta → nadie
     podía moverla el resto de la partida. Fix: el server libera los cerrojos del cliente
     caído (OnClientDisconnectCallback).
  7. (BUG) R12 incompleta en la E remota del invitado: con la ficha-vitrina del
     descubrimiento en pantalla (se abre sola), la E abría grifos a ciegas. Fix: guardas
     AlbumReal + DevPalette en MaquinaReplica.
  8. (FRÁGIL) El snapshot se pedía UNA vez: si el host aún no tenía mundo, el invitado
     quedaba gris para siempre. Fix: reintento cada 2 s con acuse mientras el espejo no
     tenga grilla.
  9-10. (FRÁGILES) Hornada nunca se limpiaba en multi (ids de la seed anterior); la
     contabilidad de difusión de SimSync sobrevivía a la sesión. Fix: Hornada.Limpiar() en
     el arranque multi; reset de _ultimoTickEnviado y compañía en OnNetworkDespawn.
  11. (BUG menor) El campo del id de lobby era el ÚNICO TextField sin EscribiendoTexto:
     pegar el id con Ctrl+V togleaba la mudanza. Fix: foco = flag.
  12. Limpieza del prefab de red: las claves muertas moveSpeed/acceleration (la trampa R58
     durmiente) fuera del YAML.
  DESCARTADOS con evidencia (verificados, no re-investigar): R58 en prefabs, doble
  suscripción de callbacks, permisos RPC/NetworkVariable, RLE/reordenación de snapshot,
  determinismo (UnityEngine.Random solo en partículas client-local), FrascoBloqueado en
  multi, el find-or-create del backdrop R75, doble DayCycle, pausa del invitado.
· Pendiente de VERIFICACIÓN EN VIVO con dos ventanas (no se pudo esta ronda: Unity ocupado
  descargándose la versión para el hermano): el flujo host-cierra→invitado-recarga y el
  cerrojo liberado. Compila fiel EXIT=0 y el editor de Cesar compila con 0 errores/avisos.
ca_playtest76.cmd barre la ronda.

## Ronda 76b — LA VERIFICACIÓN EN VIVO DEL CICLO DE SESIÓN (Unity libre, mandato de Cesar)

· Cesar liberó el editor ("unity libre para que termines pruebas") y se corrió EN VIVO el
  ciclo completo que la R76 dejó pendiente, en AlkahestLabMulti por sondas MCP:
  1. Host local (loopback) → Hosting, mundo construido, avatar vivo. De paso se verificó la
     guarda nueva: ModoFundacion encendido a propósito antes de hostear → el bootstrap lo
     apagó con acuse en consola (el prólogo NO puede colarse en multi).
  2. Disconnect() directo (fin INVOLUNTARIO, sin botón SALIR) → el aviso estático se pobló
     y la escena se recargó sola: Offline, espejo desarmado, avatar viejo muerto, aviso en
     pantalla. El hallazgo nº1 de la R76, confirmado jugando.
  3. RE-HOST tras la recarga → primera pasada FALLÓ: IsListening=False, el host nuevo moría
     solo. Censo: "AlkahestRed" (NetworkManager+SessionCoordinator) es DontDestroyOnLoad y
     único (singleton-guard OK); la causa era otra — el shutdown de NGO es ASÍNCRONO
     (ShutdownInProgress) y si la recarga corre antes de que termine, ProcessServerShutdown
     remata al host RECIÉN nacido. Fix: la recarga pendiente de TallerSesionHud espera
     `!nm.IsListening && !nm.ShutdownInProgress` antes del LoadScene (un frame más tarde no
     duele; un host asesinado sí).
  4. Con el fix desplegado, ciclo completo limpio: host → caída → recarga (esperando el
     cierre real) → re-host → mundo NUEVO vivo (grid OK, TickActual 1077→1438 avanzando),
     aviso limpio.
· DOS TRAMPAS DE VERIFICACIÓN reconfirmadas para el futuro: (a) los errores "[FriendsLoop]
  El cliente de Steam no está en ejecución" son RUIDO ESPERADO en loopback local, pero cada
  tanda dispara el Error Pause del editor y congela TODO — ante "no pasa nada" en pruebas
  multi, mirar EditorApplication.isPaused ANTES de diagnosticar (pasó TRES veces en esta
  sola verificación); (b) SimSync.Grid es el ESPEJO del invitado — en el host siempre es
  null y el mundo real se sonda por AlkahestSim.Grid/TickActual.
· El editor quedó en la escena clásica AlkahestLab, fuera de Play. El fix viaja en el cmd
  de la R77 (git add -A barre todo lo pendiente: cmds 73-76 sin correr aún).

## Ronda 77 — LAS HERRAMIENTAS DE CESAR: el overlay del cincel, el mapa de zonas, la cascada sonora

· Mandato: "tomo todas tus recomendaciones" (las herramientas de editor propuestas en la R76
  para que Cesar, sin ser dev, perfile el mundo y se haga entender). Cuatro entregas:
· EL OVERLAY DEL CINCEL (Game/PlanoOverlay.cs, la recomendada): en Play, Cesar talla con el
  cincel y pinta Stone con la paleta dev; el botón "GUARDAR FORMA COMO PLANO" (F3, solo
  prólogo, solo editor) regenera el plano virgen en una grilla de borrador (con SNAPSHOT/
  RESTORE del registro de obra: el registro vivo tiene más que el plano — el depósito
  registra su rect en runtime), toma la DIFERENCIA solo en pares Stone↔Empty (la silueta de
  la roca; líquidos y polvos son la sim viviendo, no el escenario) y la guarda como asset.
  AlkahestSim la reaplica justo tras BuildFundacion — ahí y no en el bootstrap porque el
  mundo se RECONSTRUYE (RestartRun) y el overlay pertenece a cada construcción. Respeta con
  ACUSE: la obra del taller (regla 38) y la ZONA DEL DERRUMBE (grieta+cráter: esa roca la
  talla el director cada partida; capturarla congelaría la cinemática como agujero de
  fábrica). Reversible borrando el asset. El menú 1 lo cablea a la escena (así llega a las
  builds); en editor hay fallback por ruta para el ciclo tallar→guardar→reiniciar sin tocar
  la escena. VERIFICADO EN VIVO, ciclo reina completo: esculpido por sonda (8 talladas + 6
  añadidas + 1 dentro del cráter) → captura ("8 talladas + 6 añadidas... omitidas 1 de la
  zona del derrumbe") → RestartRun → muescas y bultos REAPLICADOS, celda del cráter VIRGEN
  → menú 1 cableó la referencia. El asset de prueba (garabato de sonda) se retiró después:
  el primer overlay de verdad lo guardará Cesar.
· EL MAPA DE ZONAS (PrologoEscenografia.OnDrawGizmos): TODOS los rectángulos funcionales
  del plano del prólogo dibujados en la vista de escena con etiqueta — caverna, manantial,
  repisas (obra), poza, grieta y cráter (marcados "runtime": ahí NO capturar), cuenco, mesa,
  brasas, depósito, fogón del jugador, veta, estante, nicho de salida, spawn. Leídos de las
  MISMAS constantes que consume SimLevelBuilder (regla 24). Toggle `mostrarZonasDelPlano`.
· COORDENADAS A MANO (DevPalette): botón "Copiar (x,y)" (GUIUtility.systemCopyBuffer) y
  botón "Captura PNG" con LA CELDA EN EL NOMBRE del archivo — "mira esta captura, la celda
  del nombre es donde lo quiero" pasa a ser una frase completa. Usan la última celda vista
  (al pulsar, el ratón está sobre la ventana y el hover del mundo es inválido por diseño).
· LA CASCADA SUENA (FundacionDirector): bucle GrifoLiquido anclado al manantial, caída
  cuadrática por distancia (el perfil de los grifos) × cascadaVolumen del guion ×
  VolumenEfectos; se calla con el menú de pausa. Verificado en vivo: 0.0000 en el spawn →
  0.3306 al pie del manantial. Es la pista sonora que INVITA a acercarse antes de que la
  voz nombre el agua.
· EL JUEGO LIBRE CIERRA POR CONDUCTA (no por reloj): tras juegoLibreMinSeg (5 s), ALEJARSE
  de la poza (juegoLibreAlejarseCeldas=34) es la frase "terminé de jugar" — ahí llega
  TRÁELA. El reloj queda como tope de seguridad (juegoLibreTopeSeg=45): RENOMBRADO de
  juegoLibreSeg a propósito, porque el asset ya guardado serializa el nombre viejo con 14 y
  un campo serializado PISA el default de código (regla 58) — con el nombre nuevo el asset
  viejo lo ignora. Verificado en vivo forzando el estado: Agua/Libre + 6 s + jugador lejos
  → beat=EntregaAgua al tick siguiente.
· Nota de sonda nueva: AssetDatabase.DeleteAsset está VETADO vía MCP ("user interactions
  are not supported") — para retirar un asset desde el puente: mv a _to_delete/ con
  device_bash + Refresh.
· compile_fiel EXIT=0; editor de Cesar 0 errores. ca_playtest77.cmd barre TODO lo pendiente
  (cmds 73-76 sin correr: el más nuevo manda).

## Ronda 78 — CONSULTA DE DISEÑO: sólidos, inventario, viscosidad y la línea jugable

· Cesar pidió consejo extendido en documento: cómo guardar SÓLIDOS (el barro en el depósito
  "no completó la misión"), qué papel tienen matraces/tubos si el personaje es enano, el
  inventario en pantalla ("líquidos check y el resto?"), mejorar el game feel de líquidos y
  polvos, y cómo debe evolucionar la línea jugable tras el prólogo.
· Entregado como HOJA VIVA en `_notas/DISENO_R78_SOLIDOS_INVENTARIO_Y_LINEA.md` (fuera del repo por decisión de Cesar: se itera con Claude, no se versiona). Hallazgos clave verificados en
  código antes de opinar:
  1. (BUG DE DISEÑO CONFIRMADO) El lodo en el depósito es un SOFTLOCK SILENCIOSO: AguaDentro()
     SÍ verifica agua, pero el lodo roba volumen del vidrio (interior 78, meta 48: con >30 de
     lodo la meta es imposible) y la placa no lo dice — viola las reglas 38/43. Fix propuesto:
     contador honesto + llave de purga (pendiente de aprobación, tabla §8 fila 1).
  2. El estante de redomas (StorageRack: 5×300, CUALQUIER material incl. polvos, etiquetas +
     firmas) existe y el prólogo NO lo spawnea; el plano ya reserva su sitio ("lo levanta el
     Maestro en el beat 6"). La jerarquía de volúmenes está invertida: frasco 900 > redoma
     300 > depósito 78.
  3. Recetario propuesto: SILO para granel de polvos (gemelo del depósito, compuerta+canaleta),
     redomas para lo curado, LINGOTES por Prensa (caeSolido/cohesión, regla 7) como capítulo
     de progresión (lodo→ladrillo→REPARAR LA RUINA que el fondo ya narra), saco/item-ización
     descartada con razones. Game feel: brillo de superficie líquida, vertido en grumos,
     nubecita de impacto, GrifoPolvo al verter polvo, viscosidad por fluidity. Línea jugable:
     escalera descubrir→nombrar→producir→almacenar→automatizar (refill como rito de
     canonización)→construir; fases F1-F3.
· Ronda de solo-documento: sin código tocado. SÉPTIMO... no: SEXTO reinicio del sandbox a
  mitad de ronda — árbol recuperado del disco de Cesar con tar (la receta de CLAUDE.md).
ca_playtest78.cmd barre esta ronda y TODO lo pendiente (73-77 sin correr).

## Ronda 79 — EL PULIDO DEL PRÓLOGO (primer feedback de juego real de Cesar) + LA HOJA VIVA

· Cesar jugó el prólogo ("está hermoso") y trajo detalles. Los CLAROS se aplicaron; los dos
  de tutorial quedaron como PROPUESTAS a discutir (ver abajo). Además: la consulta R78 salió
  del repo — es una HOJA VIVA para iterar con Claude, no un documento versionado. Vive en
  `_notas/` (gitignoreada junto con `_to_delete/`).
· APLICADO Y VERIFICADO EN VIVO:
  1. LA TURBA "INCRUSTADA EN LA PARED": era el bolsón sellado de la R61-64. El sellado del
     pt48 evitaba el DERRAME, pero en 2D la roca no TAPA: el parche se veía dentro del muro
     como residuo. RETIRADO ENTERO del plano (regla 15; las constantes FundacionVeta* se
     conservan para el beat del fogón futuro). Sonda: zona x333-339/y152-160 = 100% piedra;
     captura del ala izquierda limpia. Con esto se cierra el ÚLTIMO vector de descubrimiento
     accidental del greybox.
  2. "LA LUZ PIERDE MUY RÁPIDO EL TRACK DEL PERSONAJE": el culpable era el bias 0.62 del
     VEN. (la luz prestaba 38% de su centro al fuego del Maestro: al moverse con el WASD
     recién aprendido, el jugador quedaba al borde de su propio óvalo). Ahora
     `luzBiasVen=0.92` (guion) — la luz es del jugador — y el RUMBO lo señala lo nuevo:
  3. LA LUCECITA DEL MAESTRO (pedido literal: "una luz diminuta desde esa área, como que
     algo está ocurriendo ahí"): resplandor ámbar diminuto sobre el hogar de brasas, SOLO
     durante el VEN., que nace en fade con la palabra y parpadea con la llama real
     (_luzFuego). Compuesto ENCIMA de la capa oscura de la viñeta (un segundo agujero no se
     puede recortar por alfa; un glow cálido encima lee igual: ascua lejana en la penumbra).
     Radio y alfa en el guion (lucecitaRadioPx/lucecitaAlfa).
  4. LA CHAPA "EL MAESTRO" YA NO SE ADELANTA: se dibujaba desde el primer frame; ahora se
     oculta durante todo el Despertar — el nombre se lee por primera vez cuando su voz ya
     sonó (entra junto con la lucecita, en el VEN.).
  5. EN EL PRÓLOGO NO SE DESCUBRE NI SE BAUTIZA (hardcode pedido: "que simplemente no
     puedan ocurrir"): SubstanceKnowledge.OnGUI retorna en ModoFundacion — ni banner "ALGO
     NUEVO"/"LEY DESCUBIERTA" ni "T para bautizarlo" (que además invitaba a una tecla sin
     UI: NamingUi no se spawnea en el prólogo). El detonante real era la BARBOTINA
     (lodo+agua): SIGUE naciendo y el cuenco SIGUE aceptándola como lodo (mojar el lodo no
     castiga), pero en silencio; el saber se registra igual y el catálogo la mostrará
     cuando el juego completo despierte. DECISIÓN (pregunta de Cesar "¿debe aparecer como
     material descubierto?"): NO durante el prólogo — es más información de la que el arco
     quiere cargar; nada se pierde, solo se calla.
· PENDIENTE DE DECISIÓN (ideas entregadas en chat, no aplicadas — pedido expreso):
  a) el conteo del VERTER que "obliga a vaciar todo" (re-aspirar tras el corte sube la
     base fantasma); b) el jugador que aspira rápido y luego no sabe repetir el gesto.
· SÉPTIMO reinicio del sandbox al abrir la ronda — recuperado del disco con tar, receta de
  siempre. compile_fiel EXIT=0; editor 0 errores.
ca_playtest79.cmd barre esta ronda y todo lo pendiente.

## Ronda 80 — EL MANTÉN REAL Y LA FICHA-RECUERDO (las dos ideas aprobadas del aspirar)

· Cesar subió el playtest 79 y aprobó las dos ideas del tutorial de aspirar; aplicadas:
  1. EL MANTÉN REAL (FundacionDirector, caso Aspirar): la ficha ya no confirma con la meta
     de celdas a secas — exige además una racha de succión CONTINUA (botón sostenido Y agua
     entrando, con 0.3 s de gracia entre celdas porque el frasco no traga cada frame) de al
     menos `aspirarHoldSeg` (0.6, guion). La racha se LATCHEA: lograda una vez, vale aunque
     el resto se tapone a clics. El gesto que la ficha celebra es el gesto que hay que
     aprender.
  2. LA FICHA-RECUERDO (caso Libre): si tras `recordatorioAspirarSeg` (8) sin que entre agua
     al frasco el jugador sigue en la zona del agua, la ficha "CLIC IZQ — mantén" reaparece
     UNA sola vez. Si aspira, confirma con su blip; si no, se DESVANECE sola a los
     `recordatorioDuraSeg` (6) — método nuevo `TutorialContextual.Desvanecer()`: fade-out
     sin blip ni destello (Ocultar corta en seco; esto respira). Al cerrarse el juego libre
     por conducta, la ficha activa se desvanece también (nada queda huérfano).
· VERIFICADO EN VIVO por sondas: (1) 15 aguas inyectadas al frasco SIN gesto → la ficha NO
  confirma (sub=Aspirar, mantén=false); (2) mantén latcheado → confirma al tick (sub=Verter);
  (3) en Libre con el ocio vencido, el recuerdo apareció y CADUCÓ solo por el camino del
  Desvanecer (timer a -0.003, sin excepciones, ficha fuera).
· compile_fiel EXIT=0; editor 0 errores. Pendiente de decisión aparte: cómo enseñar "la
  acción vive en el CURSOR" (opciones entregadas en chat) y el conteo del verter.
ca_playtest80.cmd barre la ronda.

## Ronda 81 — ENSEÑAR EL CURSOR (opciones 2+3 aprobadas) + LA MIRA GATEADA, con revisión Opus

· Mandato de Cesar: opción 2 (haz de presentación) + opción 3 (aro de la boca) + "la mira no
  debería aparecer hasta que recibo el frasco" + "revisa la ejecución visual con Opus para
  garantizar el objetivo no solo de ejecución sino de ORIENTACIÓN del player".
· LO CONSTRUIDO:
  1. LA MIRA GATEADA: FlaskHud.OnGUI retorna con FrascoBloqueado — sin frasco no hay
     retícula ni panel (la mira es la boca de la herramienta; antes del TOMA. promete un
     verbo que no tienes).
  2. EL HAZ DE PRESENTACIÓN: al aterrizar el frasco (+0.35 s de respiro), su haz se estira
     UNA vez desde la MANO real (CarryAnchor) hasta el cursor vivo, recortado a ReachWorld,
     sostiene y se recoge (hazPresentacionSeg=2.0). La línea que los testers vetaron como
     permanente, como gesto de nacimiento.
  3. EL ARO DE LA BOCA: anillo tenue en el cursor durante el paso de ASPIRAR, radio =
     anillo de succión REAL (Flask.SuckRadiusWorld, ahora público) medido con la cámara.
· LA REVISIÓN OPUS (ojos frescos, con números a 1080p) tumbó la primera ejecución — 17
  hallazgos, 3 BLOQUEA-OBJETIVO, TODOS aplicados:
  #1 el haz como sprites de mundo nacía ENTERRADO en la viñeta (negro opaco desde ~3.07u,
     el haz mide 6u): REHECHO EN IMGUI, dibujado tras DibujarVineta — sobre la oscuridad.
  #2 el aro MENTÍA en el verter (dibujaba succión=4 cuando el vertido usa PourRadius=2):
     aro SOLO en aspirar; al verter, el tarro ladeándose ya señala el cursor.
  #3 el tramo TOMA.→AGUA. quedaba sin guía y la cascada estaba a VOLUMEN 0 hasta con los
     pies en la poza (medido: ancla en el manantial + radio 55 = 0.0016): ancla a la MITAD
     de la caída + radio 95 (spawn≈0.12 ✓ verificado en vivo) + LUCECITA DE LA POZA durante
     Agua/Ir (el indicador R79, aplicado al destino siguiente).
  #4 Q a oscuras quemaba el contador de la línea de ayuda del frasco (guardas asimétricas
     en ActualizarUsosAyuda): simetría completa con Flask.Update.
  #5-#7 el aro calla ante cincel/mudanza/álbum/paleta/bautizo; se enfría en fuera-de-alcance
     (a juego con la retícula roja); radio corregido al anillo euclídeo real (+0.5 celdas,
     banda de la textura compensada).
  #8 el haz duraba 1.15s compitiendo con el HUD naciendo: 2.0s + retraso 0.35 + renombre
     R58-seguro trasTomaSeg→trasTomaRespiroSeg (2.6) para que el beat no corte el gesto.
  #13 el latido del aro ahora DECAE (4s) — un anillo pulsando minutos era la línea
     permanente vetada, por la puerta de atrás.
  #14 la ficha-recuerdo revive el aro (volvía media lección sin el anillo).
  #15 hot-reload reseteaba FrascoBloqueado (estática) y la mira aparecía sola: verdad de
     instancia _frascoEntregado + re-afirmación por frame.
  Nota final de la revisión: renombre radioAgua→radioAguaLuz (615) — con 440 el último
  tercio del alcance (y el corte rojo del haz real) se jugaba a oscuras.
· Verificado en vivo: volumen de cascada en spawn 0.122 (antes 0.000), FrascoBloqueado=True
  en el Despertar, guion nuevo cargado (2.6/615/95/2.0 — los renombres esquivaron los
  valores viejos serializados del asset), ciclo completo del haz corrido y recogido sin
  errores. La constatación VISUAL final es el playtest de Cesar (el haz/aro son IMGUI: no
  salen en capturas de RenderTexture).
· compile_fiel EXIT=0; editor 0 errores. ca_playtest81.cmd barre la ronda.

## Ronda 83 — EL CAPÍTULO 2 DEL PRÓLOGO: el plan, los ojos de Opus y la FASE A (el silo)

· Mandato grande de Cesar ("sorpréndeme"): extender el prólogo — segundo depósito a llenar
  de BARRO; cinemática de REORDEN (fondo al castillo, estantería central piso-techo,
  tanques reubicados con tubería que TOCA EL SUELO, refill lento); y el CINCEL rediseñado
  (la gente lo activa y olvida — igual que la mudanza) con tutorial de construcción
  (piso/techo/completar/destruir) justificado en el arco.
· EL PLAN: docs/PLAN_PROLOGO_CAP2.md — arco, beats, cinemática, cincel SOSTENIDO
  (quasimode: mantener C = cincel; soltar = frasco; precedentes Terraria smart-cursor-hold
  / cuasimodos clásicos / herramienta-visible-en-mano), fases A/B1/B2/C.
· LOS OJOS DE OPUS SOBRE EL PLAN (antes de una línea de código): 19 hallazgos, 7
  BLOQUEA-PLAN — la geometría no cabía (el hueco poza|cráter mide 6 columnas, medido), la
  estantería pisaba el fogón, el tubo "hasta el suelo" medía CERO con el tanque en el
  suelo (→ bahías APILADAS: el mueble es la premisa del tubo), el tanque de lodo
  estrangulaba su propia fuente (montículo), sellar la grieta no apagaba la gotera (pinta
  en y200, el sello va en y201+), el capítulo duplicaba el softlock R78 §1 en simétrico, y
  el barrido "borraba" el desastre del jugador (→ el barrido RECOGE: cada celda suelta
  reaparece EN su tanque — ese es el mínimo del refill). Todo integrado al plan; dos bugs
  vivos cazados de paso (el anillo del cincel mentía el alcance 60 vs 22; docblock caduco
  de Cincel — regla 49).
· FASE A CONSTRUIDA Y VERIFICADA EN VIVO:
  - DepositoDeAgua PARAMETRIZADO (dueño/huella/carga/piel): Init clásico intacto +
    InitSilo (lodo+barbotina, 6x9 en x386-391 — FundacionSilo* medidas, interior 36,
    nace VACÍO). DelDueno/Ocupado/Capacidad/CentroMundo generalizados; AguaDentro queda
    de alias histórico.
  - Beats Recompensa2 (OBSERVA. + foco + sacudida + tope emergerTopeSeg + aviso
    "apártate — algo sube" si el jugador empareda) y LlenarDeposito2 (meta
    llenarDeposito2Meta=24; al lograrlo: BIEN. + amanecer + Trueque — puente temporal
    hasta que la Fase B inserte el REORDEN).
  - PLACA HONESTA MÍNIMA (Opus A5, compartida tanque/silo): "· sobra lo que NO es X —
    aspíralo" SOLO cuando la meta es matemáticamente imposible por estorbo; gateada por
    guion (placaAvisaEstorbo) — respeta el derecho de veto de Cesar sobre el contador.
  - ContarLodoEnCrater EXCLUYE el rect del silo (el lodo atesorado no pausa la gotera).
  - Marcador Deposito2 en la escenografía + gizmo + validador del menú 1.
  - VERIFICADO: rect (386,140)-(391,148) cap 36 ✓; cadena LlenarDeposito→Recompensa2→
    LlenarDeposito2 al llenar el agua ✓; muros 18/18 en la sim ✓; estorbo de 20 aguas →
    metaBloqueada=True (la línea honesta se enciende) ✓; 24 de lodo → beat=Fin ✓;
    montículo=3 con 24 de lodo dentro del silo (exclusión ✓); CAPTURA: el silo bajo con
    su lodo visible junto al tanque alto del agua — la familia de recipientes se lee sola.
· Siguiente: FASE B1 (hundir + barrido-que-recoge + fondo), B2 (estantería + bahías
  apiladas + tubos + refill), C (cincel sostenido + la Obra). ca_playtest83.cmd barre.

## Ronda 84 — EL SILO GEMELO + FASE B1: EL REORDEN (la cinemática del capítulo 2)

· Nota de git: otra vez el cmd anterior barrió la ronda siguiente (82 contuvo 83; el 83
  solo llevó borrados de playtests viejos). GitHub completo; los mensajes van corridos.
· EL SILO, AL TAMAÑO DEL TANQUE (decisión de Cesar: "que uno sea más chiquito se me hace
  raro"): huella 8x13 con muros en x385/x392 — sobre los LABIOS de poza y cráter (piedra
  estática, regla 7: jamás cae; el contenido reposa en x386-391/y139 macizo). Interior 78,
  gemelo exacto. La distinción vendrá por carteles/decoración (en observación).
· FASE B1 DEL REORDEN, construida y VERIFICADA EN VIVO de punta a punta:
  1. LA RETIRADA (DepositoDeAgua.Retirar → Drenando/Hundiendo/Enterrado): drena celda a
     celda CONTANDO al banco (el mundo lo toma, no lo derrama — Opus C2), borra muros,
     DEGENERA su rect de obra (el handle ahora se guarda — sin él quedaba obra fantasma
     anticincel, Opus A8/fantasma R69), se hunde en reversa y el director destruye/recrea.
  2. EL BARRIDO QUE RECOGE (TickBarrido): un frente de izquierda a derecha que junta SOLO
     agua y lodo/barbotina sueltos al banco — jamás piedra/piso (la obra del jugador),
     jamás los rects protegidos (poza/cuenco/hogar/fogón/cráter), jamás los EXPERIMENTOS
     (otras materias se respetan). Manantial y rezumado EN PAUSA mientras barre (Opus A10).
  3. EL FONDO DEL CASTILLO (WorkshopBackdrop.TransicionAFondoTaller): el fondo clásico del
     taller se pinta encima de la ruina y entra en fundido (pintor bombeado A MANO para
     bajarle el alfa el mismo frame en que nace: cero flash). "Estamos reconstruyendo
     porque algo pasó" queda pagado: el espacio SE ORDENA a la vista. Sin herrajes aún.
  4. EL RENACER CARGADO: ambos recipientes reaparecen (por ahora en sus sitios; la
     estantería central con tubos es la B2) con carga inicial = lo drenado + lo barrido,
     rellenándose despacio a la vista — "lo que sale primero es lo que tú derramaste".
     Amanecer + Trueque cierran como siempre.
· VERIFICADO: cadena completa hasta beat=Fin paso=4; bancos agua=98/lodo=28; el desastre
  sembrado (6 agua + 3 lodo) recogido a 0; el EXPERIMENTO sembrado (arena) NO fue tocado
  por el barrido (desapareció por SU QUÍMICA: el agua sembrada al lado lo alcanzó — el
  barrido no lo tocó, ids ajenos ni se leen); poza protegida intacta (agua viva);
  fondoTaller vivo; tanque renació 76/76. CAPTURA: el interior ya viste ladrillo del
  castillo, los dos recipientes cargados, fogata viva, suelo limpio.
· ANOMALÍA EN CAZA (regla 30: no cerrada): el silo renació UNA vez con 14/28 y fase
  Listo. No reproducida (vertido re-armado: 27-28 ✓ estable dos veces). Descartados con
  evidencia: barrido en curso, drenado activo, reacciones (censo homogéneo id26), 
  rezumaderos (rects ajenos). El TOPE del paso 4 salvó el arco (diseñado para eso).
  EVIDENCIA FORENSE instalada (regla 54): si el tope vuelve a cerrar con carga
  incompleta, la consola imprime censo de ambos recipientes + bancos.
ca_playtest84.cmd barre la ronda.

## Ronda 85 — FUGAS SELLADAS + BARRIDO VISIBLE + FASE B2: LA ESTANTERÍA CON TUBOS Y REFILL

· Dos cazas del playtest de Cesar ("el lodo y el agua se filtran cuando hay hueco abajo…
  llegué hasta ORDEN y no vi cambio en el desorden… tampoco vi los tubos de refill"):
  1. LA FUGA: los muros del silo pisan los LABIOS de poza/cráter con vacío debajo — el
     escape DIAGONAL clásico de los líquidos (el mismo mecanismo de la fuga de cascada
     R74). FIX: el recipiente SELLA SU PROPIO SUELO al asentarse (fila de PisoEstructural
     bajo TODA la huella, y0-1) y la obra lo incluye. VERIFICADO: lecho 65x8 bajo ambas
     huellas; 48 lodo + 32 agua pintados de golpe → 48/48 y 32/32 dentro, CERO fugas.
     Y MEDIDO EN VIVO un daño colateral del primer intento: desmontar dejaba el suelo en
     Empty = ZANJA en el lecho de roca → ahora _sueloPrevio recuerda qué había y la
     retirada lo RESTAURA exacto (verificado: 0,1,1,1,1,1,1,1 → sellado → restaurado igual).
  2. EL BARRIDO INVISIBLE: los rects protegidos protegían ENTEROS — y el desastre FLUYE
     justo ahí (el lodo escapado acababa en la poza; la poza protegió su mugre). FIX
     doble: (a) protección DEL DUEÑO, no del rect — la poza conserva su agua y entrega su
     lodo; el cráter conserva su lodo y entrega su agua (cuenco/hogar/fogón siguen
     intocables); (b) EL FRENTE VISIBLE: columna de luz IMGUI (lucecita estirada + filo
     pulsante) que cruza la caverna con el frente real — el ORDEN se VE pasar aunque no
     haya nada que recoger (regla 43). + Debug.Log en CADA transición de paso del REORDEN.
· TOPE DEL PASO 1 LIMPIO: RetirarDeGolpe() — si el tope dispara a medio drenar, cuenta lo
  restante al banco, limpia el interior, desmonta y restaura ANTES del Destroy (sin muros
  huérfanos ni obra fantasma). Verificado en vivo por partida doble (probe + un ciclo que
  cruzó el tope de verdad).
· FASE B2 — LA ESTANTERÍA CENTRAL (constantes FundacionEstanteria*/FundacionBahia*):
  x386-401 entre la poza (labio x385) y el fogón (obra x402). Montantes 2x en ambos
  flancos de y140 a y188 — NO llegan a la bóveda A PROPÓSITO: el imp es un VOLADOR y la
  caverna jamás se sella (paso de vuelo y189-199, 11 celdas; caja del imp 7.8). Tablas
  x388-399 en y145/y166. Obra POR PIEZA (4 rects), jamás bounding box. La huella se
  LIMPIA antes de construir (Opus A12) banqueando lo suelto y respetando piedra/piso del
  jugador. Si alguien está parado ahí, sale nadando (doctrina ApprenticeController).
· EL RENACER REUBICADO (paso 3 del REORDEN): InitFinal/InitSiloFinal → bahía BAJA y146 =
  AGUA, bahía ALTA y167 = LODO (y167 medido: el domo del tanque bajo llega a y165).
  Marcadores depositoFinal/deposito2Final (escenografía + validador + gizmos).
  Emergencia CORTA (8 celdas) para no atravesar la bahía de abajo. La gotera del
  derrumbe (x395-397) cae DERECHA a la boca del silo alto: la herida alimenta el almacén
  hasta que la Fase C selle el techo — y ContarLodoEnCrater sube su techo de conteo tras
  el reorden (el montículo en la tabla también pausa la gotera: regla 55, proceso mortal).
· LOS TUBOS AL SUELO (pedido literal: "que toque el suelo, que se reabastece desde el
  suelo"): TanqueTuboLateral (codo al vidrio + caña con juntas + BRIDA que pisa y139),
  hijo del GO raíz (no emerge: el tanque "se conecta" al asomar), se enciende al asentar.
  REFILL: ActivarRefill → goteo refillSeg=0.8 por la columna del inlet (lado del tubo,
  brota desde abajo) hasta refillTope=60/78; llega EXCLUSIVAMENTE con el renacer (antes,
  llenar es TU tarea — R74 intacta). En Fase C el arranque del refill del silo pasará al
  sellado del techo (el relevo de la fuente).
· OJOS DE OPUS SOBRE LA PUESTA EN ESCENA (mandato de Cesar para todo el cap2) — 8
  hallazgos; APLICADOS EN CALIENTE los dos accionables:
  - #5 INVERSIÓN SEMÁNTICA (medida en captura): el tubo del LODO moría junto a la POZA de
    agua → flancos INTERCAMBIADOS: el agua bebe de la poza (izquierda), el lodo bebe de la
    herida/cráter (derecha). Capturado y verificado.
  - #3/#4 un solo material para cuatro funciones: el tubo se fundía con montantes/tablas →
    COBRE PROPIO de tubería (rojizo oscuro, sin verdín, juntas contadas) — la caña
    siluetea continua. Capturado y verificado.
  - DIFERIDOS (backlog, decisión de Cesar): cascada punteada lee como coleccionables
    (Cesar ya aprobó su look "hasta una revisión final"); color del lodo = identidad de
    seed (cambiarlo es decisión de arte global); anti-escalera del mueble (escuadras,
    capiteles) y marcas de nivel/gota subiendo = decoración del greybox que Cesar aplazó
    ("carteles y cosas decorativas vemos luego"); HUD con tinte por tanque.
· VERIFICADO EN VIVO (dos ciclos completos hasta beat=Fin): logs de TODOS los pasos;
  bancos coherentes (drenado+intrusos+barridos); intrusos recogidos de poza y cráter;
  montantes 98/98, tablas 24/24, paso de vuelo 0 sólidos; tubos activos tocando suelo;
  refill llenó ambos hasta 60/60 (tope) desde cargas menores; lecho restaurado exacto
  tras retirada. CAPTURAS: estantería con ambos tanques cargados + tubos legibles.
· ca_playtest85.cmd barre la ronda.

## Ronda 86 — EL ORDEN ABSORBE TODO: adiós estantería, tanques con tubo integrado

· Feedback de Cesar sobre la R85 (tres frentes): (1) "el agua se sigue filtrando por el
  extremo derecho de su tanque"; (2) "no quiero uno apilado del otro… esas estructuras de
  tubos y armazón hay que retirarlas… el sprite completo con un tubo GRUESO, del mismo
  material"; (3) el guion cinematográfico: "con la palabra ORDEN se absorbe todo lo del
  mapa, desaparece la cascada, la caída de lodo, y ahora tienes este nuevo punto de
  evolución" — el refill infinito REEMPLAZA a las fuentes naturales.
· LA FILTRACIÓN DEL EXTREMO, cazada: muro e interior rasaban a la MISMA altura (y152) —
  la celda líquida del tope pegada al muro se escurría en DIAGONAL por encima. FIX: los
  muros suben UNA fila sobre el interior (y0..y1+1); la boca sigue abierta para verter.
  VERIFICADO A LO BRUTO: ambos recipientes llenados a tope (78/78) → CERO escapes.
· LA ESTANTERÍA SE RETIRÓ ENTERA (regla 15 en cuatro archivos): ConstruirEstanteria,
  consts FundacionEstanteria*/FundacionBahia*, marcadores depositoFinal/deposito2Final
  (el validador BARRE los GOs huérfanos de la escena — verificado), InitFinal/InitSiloFinal
  y el caño fino TanqueTuboLateral ("no con ese cosito"). La lógica era sana (el lodo
  arriba bebiendo de la gotera) pero el guion nuevo mata la premisa: sin gotera, el mueble
  no paga su silueta.
· EL GUION NUEVO DEL REORDEN (paso 1): _fuentesApagadas PARA SIEMPRE — manantial,
  rezumadero y gotera mueren con el ORDEN; el rumor de la cascada se desvanece y el
  AudioSource se DETIENE; el barrido pierde la protección de poza y cráter (se absorben
  ENTEROS al banco — "todo eso entró en ese refill infinito"); cuenco/hogar/fogón siguen
  intocables. Y EL TRAGO FINAL (TickDesagueFinal, medido en vivo): el agua EN VUELO por
  las repisas llegaba DETRÁS del frente y dejaba un charco huérfano de 13 celdas — ahora
  el fondo de la poza se bebe su residuo hasta CERO (y el cráter su cola de lodo), una
  celda por pulso, a la vista.
· EL RENACER (paso 3): cada reservorio EN SU SITIO (marcadores de siempre), pero el
  armazón procedural es TanqueMarcoConTubo — el marco clásico + una COLUMNA DE COBRE
  GRUESA (3 celdas) al flanco derecho, mismo cobre y misma pátina ("no necesariamente se
  tiene que ver como tubería, sino del mismo material"), tapón ROSCADO arriba, dos
  abrazaderas remachadas, y el pie EN EL SUELO del sprite (la única queja de Cesar con su
  referencia). El sello del suelo se EXTIENDE bajo la columna (x1+1..x1+3): el tubo pisa
  piso firme aunque el flanco sea el labio del cráter. El prefab de piel clásico se salta
  cuando hay tubo (volverá cuando Cesar hornee su sprite v2). Refill en ambos.
· VERIFICADO EN VIVO (ciclo completo con gotera activa): muros y153 en ambos ✓; 78/78 sin
  escapes ✓; beat=Fin con tanques en x414/x385 cargas 60/60 (tope del refill) ✓; poza=0,
  cráter=0, cascada en vuelo=0 SOSTENIDO tras el trago final ✓; audio de cascada
  playing=False ✓; marcadores huérfanos barridos de la escena ✓. CAPTURAS: los dos
  reservorios con su columna de cobre y tapón, la caverna ordenada SIN cascada.
· Pendiente que viaja a la FASE C: el arranque del refill ya no espera al techo (las
  fuentes mueren en el ORDEN — decisión de guion de Cesar que simplifica C: sellar la
  grieta queda como acto constructivo puro).
· ca_playtest86.cmd barre la ronda.

## Ronda 87 — FONDOS DE PIEDRA, PISO ETERNO DEL FUEGO, Y MÁS CINE PARA EL ORDEN

· Cinco frentes del playtest de Cesar, todos atendidos:
  1. "ROMPÍ EL SUELO BAJO EL DEPÓSITO y se cayó todo… no necesitan piso de bedrock, debería
     ser igual del material del recipiente, ni sobresalir: son cerrados por abajo" → el
     FONDO del recipiente es ahora PIEDRA (el mismo material de los muros): invisible
     contra el lecho, sin sobresalir, y bajo la MISMA obra que protege los muros
     (verificado en vivo: fondo 1x8 de piedra + EsObraDelTaller=True en fondo y muros de
     ambos). El mecanismo exacto del rompimiento que vio no se reprodujo (regla 30: caza
     ABIERTA) — pero con el fondo en obra, el cincel ahora AVISA "es obra del taller" en
     vez de morder; si vuelve a romperse, la causa es otra y el aviso la delatará.
  2. "EL FUEGO NO TIENE PISO — al hacerle hueco empezó a caer incontrolablemente" → el
     lecho del hogar de brasas (y139) es OBRA: MantenerFuegoDelMaestro repinta brasas por
     siempre, y un socavón ahí era lluvia de fuego infinita al subsuelo. El fuego eterno
     exige piso eterno (verificado: obra en el lecho ✓).
  3. "Al desaparecer después del ORDEN deberían desaparecer las plataformas donde caía el
     agua" → LAS REPISAS DE LA CASCADA SE VAN CON ELLA: el frente del barrido las deshace
     columna a columna (EsCeldaDeRepisa contra las constantes del plano, regla 24) y su
     obra se degenera al terminar (verificado: 84 celdas → 0; obra False).
  4. "Hace falta notar el ensamble… que se note que me lo dio el Maestro" + "las
     partículas deberían ingresar en los depósitos… me falta más cine" → DOS GESTOS:
     (a) LAS MOTAS DEL BARRIDO: cada celda recogida suelta un destello (azul el agua,
     pardo el lodo) que VUELA en arco hasta el sitio donde su reservorio renacerá y se
     hunde ahí — la materia SE VE viajar a su nuevo dueño (IMGUI sobre la viñeta, tope 64
     vivas, duraciones escalonadas deterministas; verificado spawn/tick en vivo — el
     draw es IMGUI: se ve jugando, no en capturas RenderTexture);
     (b) LA FIRMA DEL REGALO: al renacer, la cámara viaja al punto medio de los dos
     reservorios y el Maestro dice TOMA. — la misma palabra con la que entregó el frasco:
     sus regalos hablan igual.
  5. "La luz debería dejar de ser focal… después de que cae el lodo, para que se ponga a
     jugar" → al cerrar el derrumbe (la cámara vuelve al jugador), la viñeta SE ABRE a
     radioLodoJuego (guion, 1500) y no vuelve a encogerse — la penumbra dramática quedó
     atrás; el amanecer PLENO (2400) sigue siendo del final.
· Sobre "las piecitas que no se pueden quitar entre los recipientes": son las MEJILLAS DEL
  FOGÓN DEL JUGADOR (x402-403 y x411-412, obra desde la ronda 61) — el hogar vacío para el
  fuego propio del beat futuro. Decisión pendiente de Cesar: se quedan (esperando su beat)
  o se les da cartel/decoración cuando toque.
· VERIFICADO EN VIVO (ciclo completo): fondo piedra+obra ✓; lecho del hogar obra ✓;
  repisas 84→0 con obra degenerada ✓; poza 0 ✓; cargas 60/60 ✓; motas spawn 5→0 por tick ✓;
  log "TOMA. — recipientes renaciendo EN SUS SITIOS" ✓.
· ca_playtest87.cmd barre la ronda.

## Ronda 88 — EL ANILLO DEL ORDEN (la dirección de escena de Opus) + los últimos pisos

· Encargo de Cesar tras el playtest 87: "la animación de ORDEN se ve como algo primarioso
  hecho con PowerPoint… necesitamos a Opus para afinar lo espectacular — o discreto pero
  como DECLARACIÓN DE OMNIPOTENCIA. No vi nada que me indicara que el Maestro colocó los
  refill: más teatralidad omnipotente." + cuatro frentes mecánicos.
· LA SESIÓN DE DIRECCIÓN CON OPUS (leyó el código real): el hallazgo que tumbó todo — el
  barrido corría de IZQUIERDA A DERECHA (un wipe horizontal ES una transición de
  PowerPoint) y el poder viajaba HACIA el Maestro (x442), no desde él. Guion nuevo entero,
  segundo a segundo, TODO implementado y verificado en vivo:
  - EL VACÍO (1.6 s): el sonido del mundo muere ANTES de la palabra — la cascada cae MUDA
    (_cascadaMuda). La cámara se planta al centro y NO se mueve durante la declaración.
  - LA LUZ SE CIERRA sobre el Maestro (0.7 s): la sala se apaga hacia él; el único punto
    iluminado es su silueta (_focoDeLuz desacopla la LUZ de la CÁMARA; _radioForzado
    puentea el suavizado: el guion manda sobre el Lerp).
  - ORDEN. a pitch 0.82 — la nota más baja del prólogo (DecirConTono: el pitch por
    módulo aritmético dejaba a ORDEN más neutro que TOMA), hold 3.6 (el texto muere
    DESPUÉS que el mundo). CERO SACUDIDA: el mundo no se asusta, acata.
  - EL PULSO: chasquido de luz 320→400 en 0.08, flash blanco 0.22→0, SUB de 38 Hz
    (SintetizadorSfx.SubGrave, seno con decaimiento exponencial). El tanque del Maestro
    —lo más cercano y lo más suyo— se ANULA ahí mismo (RetirarRapido: 0.5 s, no 2.7; "no
    es un vaciado, es una anulación"). El fondo ruina→castillo arranca CON la palabra.
  - EL ANILLO (4.2 s): la luz se abre del Maestro hacia afuera (~22 c/s, curva
    DistanciaDelAnillo) y EL BORDE DE LA VIÑETA ES EL FRENTE — lo que la luz toca,
    obedece (dos cabezas de limpieza columnar invisibles bajo el óvalo: lectura circular,
    coste cero). El silo se anula a d=50; LA POZA se levanta a 15 c/s (MotasTope 64→160:
    el tope se comía el plano principal — Opus lo midió); las repisas caen; EL FRENTE SE
    DETIENE 0.4 s con la cascada cayendo sola en el borde de la luz — lo último natural
    del mundo — y la boca del manantial SE SELLA con piedra (verificado: (339,175)=Stone).
  - EL IRIS: la viñeta usada como LENTE (S(520) sobre los dos sitios — el primer plano
    del pixel art). Dos RESPLANDORES respiran en el suelo donde las motas aterrizaron
    (contadores _poolAgua/_poolLodo): ya no adorno — la carga ESPERANDO.
  - EL RENACER: ambos suben JUNTOS y YA LLENOS (cargaInstantanea — la espera de
    CargaLista era un progress bar; fuera). Sacudida 0.35 AL ASENTAR: ganada.
  - LOS TUBOS (el encargo textual): quietud 0.7 → polvo ANTES que metal → las columnas
    de cobre EMPUJAN desde el subsuelo (InstalarTubo, easeOutBack con overshoot,
    TanqueTuboGrueso como PIEZA PROPIA — el marco vuelve al clásico) → UN SOLO CLANK
    para los dos (SintetizadorSfx.Clank, parciales inarmónicos; es un acto, no dos
    piezas) + sacudida 0.5 → 0.45 s de nada (el cobre puesto, inerte) → TOMA. a pitch
    0.90 (el verbo del frasco: un regalo) → LA PRIMERA GOTA, sola, cayendo desde el
    tope con física real → el ritmo de 0.8 s: la promesa de infinito.
  - RETIRADO (regla 15): el frente pulsante a 6 Hz ("un frente que jadea es una máquina;
    el poder no se esfuerza"), la sacudida en la palabra, los 5 pasos-diapositiva.
· LOS CUATRO FRENTES MECÁNICOS DE CESAR:
  1. "No necesitan piso ni de madera ni de tierra NI DE NADA — son de metal y ese es su
     piso" → EL FONDO VIVE DENTRO: la fila y0 es el fondo del propio recipiente (piedra,
     tapada por el zócalo de cobre del sprite), interior útil y0+1..y1 (72), y EL LECHO
     DEL MUNDO NO SE TOCA JAMÁS (adiós piso postizo y adiós _sueloPrevio). Verificado:
     lecho y139 virgen (0,1,1,1,1,1,1,1), fila y140 = estructura 8/8.
  2. "El autofill del barro es muy rápido" + 3. "el del agua hace algo raro: llena la
     base y luego gotea" → LA GOTA NACE EN EL TOPE del vidrio (junto al tubo) y CAE a la
     vista, misma cadencia 0.8 s ambos; refillTope = 36 = LA MITAD ("los barriles llegan
     hasta la mitad de llenos y se ve que se refillean") y el renacer entra por DEBAJO de
     la mitad para que la gota complete a la vista. Verificado: ambos descansan en 36/72.
  4. "En la esquina inferior izquierda del silo a veces parece que se filtra algo" → eran
     los TERRONES decorativos de la emergencia cayendo al pozo de la poza: si bajo el
     flanco no hay suelo, el terrón no se suelta.
· DECISIÓN DE DISEÑO REGISTRADA (hoja viva): en SEMILLA CERO no hay bautizo — todo viene
  bautizado con nombres de referente real (lágrima gris, tinte pardo… SON finales); la
  mecánica de bautizar queda relegada al MODO CAÓTICO. La doctrina R13/17/23 se parte por
  modo.
· VERIFICADO EN VIVO: secuencia completa con timestamps al ritmo del guion (ORDEN :44 →
  PULSO :45 → ANILLO completo :49 → RENACER :51 → TUBOS :54 → AMANECER :57); bancos
  agua=40/lodo=17; renacer 28/17 (bajo la mitad) → 36/36 por goteo (tope mitad ✓); tubos
  instalados ✓; manantial sellado ✓; 0 errores. CAPTURAS: los reservorios a media carga
  con sus columnas encajadas. Los sonidos nuevos y el ritmo completo los estrena Cesar
  jugando (IMGUI/audio no salen en RenderTexture).
· ca_playtest88.cmd barre la ronda.

## Ronda 89 — EL MUNDO CRECE AL OESTE + LA COSECHA A PLENA LUZ + LOS NOMBRES REALES

· Tres encargos de Cesar ("vamos 2 min de gameplay pero vamos bien"):
  1. EL ESTUDIO DE NOMBRES (con Opus leyendo el código): hallazgo mayúsculo — LA TABLA DE
     NOMBRES REALES YA EXISTÍA (Universe.ConstruirIdentidadReal, 48 entradas con reseña de
     trivia) y el prólogo NO LA LEÍA (los nombres pedían ModoSemillaCero; el prólogo corre
     con ModoFundacion — química y colores sí usaban la condición doble; los nombres eran
     el único sistema desalineado). "Sedimento cobrizo" YA ERA "arcilla" (92% de
     fidelidad), "tinte pardo" YA ERA "barbotina" (88%, término real de alfarería).
     APLICADO: (a) tabla enchufada al prólogo; (b) cal viva + agua → cal apagada — la
     reseña prometía la reacción y NO EXISTÍA (la peor mentira: la que el conocimiento
     real invita a comprobar); (c) solubles por decreto completados (caliza→agua de cal,
     sal→salmuera: el comentario lo prometía desde la ronda 56 y el código solo escribía
     la arcilla); (d) Sand = "arena de río" (colisionaba en pantalla con "arena de
     sílice"). Estudio completo con %, riesgos y backlog: docs/DISENO_NOMBRES_SEMILLA_CERO.md
     (el trabajo grande destapado: las 8 leyes sorteadas contradicen los nombres reales —
     "caliza+turba→agua" es química falsa con nombres verdaderos; pendiente de decisión).
  2. EL ENSANCHE (+20 AL OESTE, caverna 320-460 = 141): reparto nuevo medido — veta/
     manantial 320 · repisas · POZA 352-365 · [3 de suelo] · SILO 369-376 + tubo 377-379
     (¡por fin sobre suelo MACIZO: ni volando sobre la poza ni pisando el cráter!) ·
     [6 de suelo] · CRÁTER 386-394 (grieta 390) · fogón 396-406 · spawn 395 · TANQUE
     412-419 + tubo 420-422 · brasas/mesa igual. Los claros del tanque se DUPLICARON
     (fogón→tanque 6, tubo→hogar 4; antes 2 y 2 — el pedido del "doble respecto a la
     fogata"). Marcadores de escena movidos al plano nuevo y escena guardada (autoridad de
     escena: si se quedaban, pisaban a las constantes). OJO: el PlanoOverlay guardado de
     Cesar (si tiene retoques) usa coordenadas absolutas — sus formas del flanco oeste
     quedarán 20 celdas corridas respecto a las nuevas posiciones; re-guardar si molesta.
  3. EL ORDEN "10 de 10, disfrutarlo un poquito más… no veo cómo llegan las partículas":
     (a) EL ANILLO POR CONSTANTES (regla 24: el ensanche habría dejado corta la curva
     horneada) — velocidades Opus sobre el plano real, ~5 s de anillo en la caverna de
     141; (b) LA COSECHA: el anillo ABSORBE en su borde y RECUERDA cada punto de origen;
     con la caverna ENTERA iluminada (el después), todo lo recogido SE LEVANTA de donde
     estaba — en el orden en que se recogió — y vuela en arcos a los dos sitios, ~2.2 s de
     corriente doble a plena vista; recién entonces el iris. El ORDEN creció ~3 s donde
     Cesar quería mirarlo.
· DOS BUGS CAZADOS CON NÚMEROS en la primera pasada en vivo: (a) banco de agua = 9 con la
  poza llena — EL DESAGÜE FINAL se bebía la poza ANTES de que el anillo llegara (desde el
  ensanche tarda ~3 s): gateado fuera del Beat.Reorden — el trago es para el residuo de
  después, jamás un competidor del banco; (b) tanque renacido con carga 0 — el spawn
  nuevo (409) caía DENTRO de la guarda antiemparedamiento del tanque (408-423) y un
  jugador quieto congelaba el asentado para siempre (el tope del director cerraba el arco
  con un tanque SIN muros): spawn a 395 (la posición relativa de siempre, sobre el labio
  del cráter) + LA ESPERA CON TOPE (6 s y los muros entran igual: sales nadando —
  doctrina ApprenticeController, regla 38).
· VERIFICADO EN VIVO (ciclo definitivo): pre-ORDEN sano (tanque 14 listo, poza 48); banco
  agua=63/lodo=22 (nada robado); COSECHA de 82 destellos; renacer 28/22 bajo la mitad;
  tubos sin topes (3 s, no 12); FINAL 36/72 y 36/72 — LA MITAD EXACTA en ambos; nombres
  en vivo: mat26="arcilla", mat33="barbotina" ✓; muro oeste macizo (24 de roca hasta el
  cuarto íntimo); suelo bajo silo+tubo 11/11 macizo ✓. CAPTURAS del mundo ancho.
· ca_playtest89.cmd barre la ronda.

## Ronda 90 — TODO AL MAESTRO: el perfilado de la geología, el flashazo, y el refill que completa

· Dos frentes de Cesar ("apóyate con Opus, ya casi queda"), con revisión Opus que CONTÓ
  las mordidas ejecutando el XorShift (132 celdas: "eso no es espectáculo, es una gotera")
  y trajo seis vetos con números:
  1. EL REFILL v3: renacer a ~1/5 (14 + banco/8, tope 30 — el eco del banco conserva la
     promesa R84 "lo que derramaste vuelve"); la gota cae POR EL CENTRO del vidrio;
     COMPLETA el tanque (tope 66, no 72: con el vidrio raso la fila de arriba se ocupaba
     y la gota moría muda en 71 — medido por Opus); cadencia CUADRÁTICA 0.8→~4.8 s
     (~110 s el llenado: se VE al inicio, "todo toma tiempo" después — la lineal mataba
     la fase visible enseguida).
  2. LA COSECHA DIFERIDA SE RETIRÓ (regla 15): abrir la luz 3 s para verla DESINFLABA el
     clímax ("solo necesitaba un flashazo épico que sí me lo daba la oscuridad"). Las
     motas vuelven a volar EN VIVO — pero ahora TODAS AL MAESTRO ("debería absorberlas
     todas él… y le caen cosas de todos lados"): desde el borde oscuro hacia el centro
     iluminado, siempre visibles, a ~110 c/s (8× el frente: la luz sale, la materia
     entra); las del remate llegan apretadas (dur fija) para que el trago caiga con la
     última.
  3. EL PERFILADO DE LA GEOLOGÍA: al paso del anillo, las MORDIDAS de la destrucción
     (R74: bóveda, muros, ala izquierda) SE ENDEREZAN — la roca vuelve a su línea — y
     cada celda corregida suelta esquirlas GRISES al Maestro (3 por celda; 5 en los
     muros: las que "caen de los lados"): ~400 destellos de pura geología obedeciendo,
     espectacular AUNQUE el jugador no haya hecho desorden. La grieta del DERRUMBE queda
     intacta a propósito: esa herida es de la Fase C. Cabezas extendidas a X0-2/X1+2
     (Opus BLOQUEA #1: la profundidad 2 de los muros quedaba sin visitar).
  4. EL HALO Y EL FLASHAZO: el Maestro SE HINCHA tragando (+0.012 de halo por mota que
     llega) y el cierre es ese halo REVENTANDO — la luz espera a la última mota (tope
     1.3 s), COLAPSA A NEGRO 0.12 s (la oscuridad que Cesar pidió de vuelta, solo el
     sub a pitch 0.85), blanco 0.55 sostenido 0.06 + decaimiento 0.30 (el doble del
     pulso o leería como repetición), apertura 0.25, y la caverna limpia se muestra
     0.8 s — no 3 — antes del iris. Flash paramétrico (pulso chico / flashazo grande).
  5. Motas: tope 300 (con 160 la roca se perdía muda detrás del agua en mapas
     inundados); LA ROCA NUNCA SE DESCARTA (es el espectáculo garantizado); agua/lodo
     saltean 1 de cada 2 sobre 240; jitter determinista por índice (esquirlas, no un
     píxel clonado); tres colores (azul/pardo/gris roca).
  6. BUG DEL ENSANCHE CAZADO: el terreno roto del ala izquierda seguía en el rango viejo
     (342-369) y MORDÍA BAJO LA POZA nueva (352-365) — corregido a 322-349.
· VERIFICADO EN VIVO: perfilado 112/114 mordidas enderezadas (bóveda 2 restantes en zona
  a oscuras — anotadas, regla 30; muros 0/40 ✓); grieta del manantial sellada ✓; grieta
  del derrumbe intacta ✓; ciclo completo a Fin con cargas 1/5+eco subiendo hacia 66 cada
  vez más lento ✓; halo y motas en 0 al cierre ✓. CAPTURA: la caverna PERFILADA — bóveda
  y muros en línea recta donde antes había dientes. El negro→flashazo, el halo hinchándose
  y la lluvia de esquirlas grises son IMGUI: se estrenan en el playtest de Cesar.
· ca_playtest90.cmd barre la ronda.

## Ronda 91 — LOS POZOS SELLADOS, EL TOPE ENTERO, Y NI UNA MANCHA

· Tres frentes de Cesar:
  1. "El pozo que traga el agua y el que traga el lodo deberían quedar SELLADOS después
     del ORDEN" + "la animación debe incluir el APLANAMIENTO del hueco de agua y no
     generar un hueco traga-lodo" → EL APLANAMIENTO ES PARTE DEL ANILLO: al paso del
     frente, la poza (70 celdas) y el cráter (18) se rellenan de piedra COLUMNA A
     COLUMNA, cada celda con su esquirla gris al Maestro — los pozos dejan de tragar
     para siempre, dentro de la coreografía. De regalo mata el REFLUJO (la piedra que
     sube impide que el agua vecina vuelva al hueco recién vaciado — la "cola" que antes
     bebía el desagüe). Los EXPERIMENTOS hundidos en la poza NO se entierran (regla 38:
     dejan su muesca honesta). `TickDesagueFinal` RETIRADO (regla 15): sin pozos no hay
     residuo que beber.
  2. "Deberían seguir llenando HASTA EL TOPE aunque no se visualice la última sección…
     ¿que el total tarde 3 minutos?" → refill al vidrio ENTERO (72): gota visible por el
     centro mientras hay caída; cuando el tope se ocupa, COLA SILENCIOSA rincón a rincón.
     Cadencia cuadrática factor 7: la integral de 14 a 72 da ~180 s — los 3 minutos,
     calculados, no estimados (y sí me parece bien: es una fuente infinita de fondo, no
     una espera activa). Y LA CAZA DE LA RONDA: el reporte de Cesar "cuando llegan a la
     mitad dejan de llenarse" no era el tope 66 — era la REGLA 58 EN SU VARIANTE
     HOT-RELOAD: el GuionDelPrologo VIVO del editor serializó refillTope=36 (el default
     R88) a través de las recompilaciones AUNQUE el asset del disco jamás tuvo el campo
     (verificado: guion efectivo decía 36 con el código diciendo 72). Cura doctrinal:
     RENOMBRE → refillTopeCeldas (la memoria vieja queda ignorada). Catálogo R58
     ampliado en CLAUDE.md con la tercera vía.
  3. "Asegura que absorba TODAS las partículas — siempre quedan restos entre los
     contenedores… incluidas las de humo" → TRES capas: (a) el anillo ahora absorbe
     también HUMO y VAPOR (esquirla gris, sin banco: el Maestro se los traga y ya);
     (b) el RENACER ES LIMPIO — los tanques del mundo ordenado ya no escupen terrones
     (eran exactamente los "restos entre los contenedores"); (c) EL REPASO DEL AMANECER:
     pasada final silenciosa por la caverna entera que borra cualquier resto suelto
     (agua/lodo/humo/vapor) caído detrás del anillo, respetando protegidos, experimentos
     y el interior de los recipientes — con su línea forense de cuántos borró.
· VERIFICADO EN VIVO: poza 0/70 y cráter 0/18 no-piedra (SELLADOS) ✓; restos sueltos en
  la caverna entera = 0 ✓; tubos ✓; guion efectivo refillTopeCeldas=72 ✓; la cola
  silenciosa cerró el caso 71→72 en <7 s (el caso que antes moría mudo) ✓.
· ca_playtest91.cmd barre la ronda.

## Ronda 92 — LA RETAGUARDIA DEL ANILLO: la absorción no delega en el hardcodeo

· El reporte de Cesar tras el playtest 91: "después de la absorción TARDA en pasar la
  limpieza del amanecer, cosa que se siente como una limpieza más por HARDCODEO que
  absorción de partículas; aun así las partículas A RAS DEL SUELO y hasta algunas líneas
  por encima NO SE LIMPIAN; idealmente tendrían que limpiarse con la absorción del
  Maestro, lo mismo para su pocito donde antes pedía las cosas." El diagnóstico: el
  frente del anillo pasa UNA sola vez por columna, pero los charcos FLUYEN de vuelta al
  terreno ya barrido detrás de él — y quedaban ahí ~10 s hasta el RepasoDelAmanecer, que
  al borrarlos de golpe se leía exactamente como lo que era: un wipe. (Su playtest lo
  midió: 259 restos borrados por el repaso.)
· Tres piezas: (1) LA RETAGUARDIA DEL ANILLO — cada frame de la ceremonia (pasos 2-5)
  el intervalo ya conquistado se RE-BARRE a ALTURA COMPLETA: agua/lodo al banco,
  humo/vapor al trago, mota al Maestro por celda; el humo del fogón protegido vuela en
  CHORRO CONTINUO hasta el amanecer; respeta protegidos y el interior de los recipientes.
  (2) EL CUENCO FUERA DE LA PROTECCIÓN: el Maestro se traga su propio receptor (su
  contenido al banco; post-ORDEN las entregas viven en el tablón). (3) EL CUENCO
  APLANADO en PerfilarColumna con el trato de la poza (regla 38: experimentos dejan
  muesca). El RepasoDelAmanecer queda como RED DE SEGURIDAD y su línea forense de
  termómetro: si un día reporta algo, la retaguardia tiene un agujero.
· VERIFICADO EN VIVO (desorden sembrado): banda del suelo bajó el repaso 259→30 (humo
  alto) → altura completa pasos 2-5: REPASO EN SILENCIO = 0 ✓; agua=0 lodo=0 ✓; cuenco
  0/35 no-piedra ✓; el humo post-Fin es del fogón VIVO (el mundo respira, no es mancha).
· ca_playtest92.cmd barrió la ronda.

## Ronda 93 — EL FINAL DEL PRÓLOGO: la Obra, el Acomodo y el Adiós (mandato nocturno)

· Mandato de Cesar: terminar el prólogo DESDE su lenguaje (no reutilizar caótico/semilla
  cero); sin misiones de esquina con la O; tolva descartada/reinventada; el ORDEN
  repetible para agrandar el espacio; frasco entregado = el que llevas; plataforma con
  su techo en silueta blanca; V como ESTADO clarísimo; mudanza de los dos reservorios
  al centro; Maestro ETÉREO cuya despedida es "dejar de prestar atención", canal abierto
  en el tablón sin figura permanente; fuego primitivo; "sorpréndeme".
· TRES BEATS NUEVOS tras el Reorden: OBRA (ALZA., cincel volado a la mano + siluetas
  blancas de plataforma y grieta), ACOMODO (ACOMODA., reservorios IMovible con imán de
  slots ±2 — regla 36 derogada solo para el mundo ordenado; Reposicionar muda muros,
  fondo, contenido, obra, visual y tubo sin derramar), ADIÓS (ORDEN. repetido, vano en
  el muro oeste, DIEZ MIL AÑOS., figura etérea desvaneciéndose en motas hacia el tablón,
  Trueque activado ahí; el fuego queda). + Maestro etéreo (flota y respira), TarroDeMano
  en el TOMA, CincelHerramienta, OrdersHud fuera de la Fundación (regla 15), placa de
  estado de la MUDANZA, gases no bloquean mampostería (el humo del fogón bloqueaba el
  techo — cazado en vivo), tubo espejado al flanco libre, anti-solape de recipientes.
· VERIFICADO con dos ciclos Reorden→Fin en vivo (imán, cargas intactas, vano 64,
  presencia 0, Trueque activo, consola limpia).
· VEREDICTO DEL PLAYTEST DE CESAR (la mañana siguiente): "le falta mucho" — los
  contornos de la mudanza no calzan con los sprites; el "manchón de techo" no se
  entiende; ALZA es "una línea mal pintada que cubre el requisito, más horrible que
  antes"; C→X incómodo al dedo; y "lo peor: el remate del título totalmente
  anticlimático — ¿DIEZ MIL AÑOS qué? y pum, rompes 20 píxeles a la izquierda". LA
  LECCIÓN (pariente de la regla 43): completar el esqueleto de una escena no es
  dirigirla — el listón del REORDEN (10/10) lo puso una dirección de escena con
  números; el final del prólogo la recibe en la R94.
· NOTA DE INTENDENCIA: el sandbox se reseteó tras esta ronda y los commits locales
  murieron con él — el CÓDIGO sobrevivió entero porque Cesar corrió el barredor
  (GitHub la verdad, regla 6), pero el HISTORIAL de la 92-93 y el doc de familias
  NO habían viajado a su disco y hubo que reconstruirlos de memoria. REGLA NUEVA DE
  FACTO: los docs de la ronda viajan a su disco EN EL MISMO DEPLOY que el código,
  siempre.

## Ronda 94 — LA CALIDAD DEL FINAL: dirección de escena Opus tras el veredicto

· El reset del sandbox se llevó los commits locales de la 92-93 (el CÓDIGO sobrevivió
  entero vía GitHub, regla 6; el HISTORIAL y el doc de familias se reconstruyeron de
  memoria y AHORA VIAJAN al disco de Cesar con cada deploy). Compilador fiel re-stageado
  desde Builds/TenThousandYearsDemo (regla 7).
· LOS OJOS DE OPUS midieron por qué falló el final (con números): el vano quedaba a 3
  celdas del borde del encuadre (FocoCinematico en el plinto → x306..467: literalmente
  no se veía) y daba a ROCA MACIZA (un nicho, no un umbral); el título competía con una
  imagen gastada 2.6 s antes, sin silencio, con la tipografía de las órdenes; la
  migración al tablón eran 7.5 celdas en plano general; la grieta del techo estaba a 58
  celdas SOBRE el encuadre (el jugador jamás la vio antes de que se la pidieran); ALZA
  eran 2 filas de Stone crudo sobre piedra recién perfilada (un rasguño), y
  PisoEstructural — que el renderer YA dibuja fabril — no se usaba nunca.
· LO CONSTRUIDO (la dirección entera, implementada):
  1. EL PLINTO ESCALONADO (3 hiladas, 54 exactas: 377-396/378-395/379-394) — un
     descuido ya no cubre el requisito. SlotY0=143 (los reservorios se apoyan sobre la
     cara del plinto, 8+8 = los 16 justos de la hilada alta).
  2. EL PERFILADO DE TU OBRA (2.0 s): 0.30 de silencio → BIEN. + la luz se arrodilla
     (S(200)) → barrido 20 col a 25 col/s (Stone→PisoEstructural SOLO en las 54
     pedidas, 3 motas/col, clank cada 4 con pitch subiendo 1.05→1.33: la escalera del
     cantero) → el sello (SubGrave 0.90 + sacudida 0.18) → la luz se libera. LO PINTADO
     DE MÁS QUEDA BRUTO a propósito: "lo bruto lo pones tú, el mundo perfila lo que
     pidió" — verificado (2 celdas extra siguieron Stone).
  3. EL TECHO, MOSTRADO Y MOTIVADO: plano de establecimiento de 1.6 s al decir ALZA.
     (la cámara sube a la grieta) + LA HERIDA GOTEA polvo cada 0.75 s con su sonido
     agudo (pitch 1.40 — lo que hace levantar la vista; aterriza sobre tu plinto; se
     apaga PARA SIEMPRE al sellar: no es fuente) + premio propio (luz S(140), celdas a
     PisoEstructural, clanks, el gemido de la montaña asentándose).
  4. EL UMBRAL NUEVO (766 celdas, ya no "20 píxeles"): el muro RETROCEDE (x319→312,
     altura completa, 10 col/s, la roca vuela hacia el Maestro y SALE del encuadre) →
     el ARCO con clave (x311→302, y140-160, 14 col/s) → el RECODO que sube (x302-305,
     y164-180) y SE PIERDE — desde la caverna no se ve el final del pasaje: una
     promesa, no una mentira. El suelo y139 intacto: se entra caminando. Y LA LUZ DEL
     OTRO LADO: dos resplandores FRÍOS (0.80,0.87,1.00) — la primera luz del juego que
     no es fuego ni Maestro — PERMANENTES (respiran a 0.3 Hz, sobreviven a Beat.Fin).
  5. EL ADIÓS EN 11 PASOS (~18 s): silencio → ORDEN. (mismo 0.82: es el mismo verbo; la
     oscuridad VUELVE sobre lo que construiste) → el pulso y EL VIAJE (la luz SE VA de
     ti hacia el oeste: él ya no es el centro; la cámara rezagada da el traveling
     gratis) → el muro retrocede → el vano → la luz del otro lado (1.6 s de pura
     contemplación, EN PRIMER PLANO — aquí se entiende) → EL NEGRO, su firma (sub a
     0.60, solo su fuego y el mañana al 45%) → EL TÍTULO SOBRE EL NEGRO (0.90 s de la
     palabra SOLA en pantalla) y el amanecer definitivo naciendo DEBAJO de ella → la
     imagen completa (1.7 s de nada: tu plinto, sus reservorios, el fuego, el tablón,
     el destello frío) → LA DESPEDIDA en plano cerrado (brasas + tablón + figura en el
     MISMO encuadre; ~22 motas DORADAS — tipo 3 nuevo: su atención, no materia — caen
     de él al tablón, que se enciende ámbar PARA SIEMPRE; un clank agudo: la última
     posándose; la figura se apaga y nunca más parpadea; su fuego sigue) → EL MUNDO ES
     TUYO (Trueque + placa "EL TABLÓN — E", la única vez que el juego lo nombra).
  6. EL TÍTULO CON TRATAMIENTO PROPIO (la única no-orden del Maestro): DecirTitulo —
     doble capa sonora (voz a 0.62, la nota más baja del prólogo, + sub a 0.50);
     tipografía 1.55×, centrado vertical EXACTO, SIN deriva (las órdenes flotan; el
     título está TALLADO), tracking doble, más blanco (0.98,0.96,0.90), sombra doble,
     fades 0.70/1.20, y DOS FILETES a ±S(34): ninguna palabra suya lleva reglas; esta
     sí, porque es el nombre de lo que empieza.
  7. LOS CONTORNOS DE LA MUDANZA CALZAN: el contrato IMovible del recipiente se mide en
     el rect VISUAL (spanVisual×altoVisual, ancla x0-1) — la silueta de arrastre y los
     slots cubren EXACTAMENTE el sprite. 8. LA C CICLA (frasco→piedra→piso→frasco): la
     X queda de alias — "presionar C y luego X es súper incómodo al dedo". 9. CARTELES
     AGUA/ARCILLA sobre los reservorios de cerca (desde el Acomodo): el pacto de las
     familias, visible — la distinción REAL lodo-sucio/arcilla-limpia (material
     variante) queda declarada como ronda propia en el doc de familias.
· VERIFICADO EN VIVO (ciclo completo Reorden→Fin): plinto 53 objetivos (la mejilla del
  fogón ya era piedra) → vestido 54/54 de PisoEstructural con lo extra bruto ✓; techo
  goteando y premiado ✓; imán con Y desviada (380,143)/(388,144) → (379/387,143) ✓;
  umbral EXACTO 488/210/68 con suelo 18/18 intacto ✓; vano y tablón vivos ✓; Trueque ✓;
  consola 0 errores 0 warnings ✓. Los 11 pasos y el título son IMGUI: se estrenan en el
  playtest de Cesar.
· ca_playtest94.cmd barre la ronda.

## Ronda 95 — EL NOMBRE EN INGLÉS, EL BOTÓN "2" Y EL CENSO DE ALCANCE

· (Otro reset del sandbox — 2º del día; GitHub tenía la R94 porque Cesar corrió el
  barredor: recuperación limpia con reset --hard. Las DLLs sobrevivieron.)
· 1) REGLA DE MARCA: el nombre es TEN THOUSAND YEARS SIEMPRE, en todo idioma; solo el
  eslogan se localiza. El título del Adiós ahora se dice en inglés (DecirTitulo) y el
  doc de familias lo declara.
· 2) EL BOTÓN "2" (provisional): a la derecha del botón PRÓLOGO, misma fila, cero
  cambios de estructura — arranca DESDE el final del primer ORDEN. FundacionDirector.
  ArranqueEnObra (estático, se consume en Init) → TickSaltoInicial: la CIRUGÍA del
  checkpoint reusa las rutinas del anillo (LimpiarColumna + PerfilarColumna en todas
  las columnas, sello del manantial, obras de repisas degeneradas, motas descartadas),
  frasco/HUD como si el TOMA. hubiera pasado, fuentes muertas, reservorios renacidos
  (20/16) con tubo + refill, luz de amanecer, y beat=Obra directo. VERIFICADO EN VIVO:
  beat=Obra obraFase=2, tanques Listos con tubo y refill (41/39 subiendo), poza 0
  no-piedra. Muere cuando el prólogo quede sellado.
· 3) EL CENSO DE ALCANCE (doc de familias §7, la pregunta "¿a cuántos llego?"):
  HALLAZGO #1 — el kit de hoy (agua+arcilla+brasas) alcanza SEIS materiales y CERO
  transformaciones térmicas (brasas 100 < umbrales 180/188/205): la promesa del fuego
  está mecánicamente incumplida; la veta de turba se retiró en R79 y nunca volvió.
  HALLAZGO #2 — el Trueque es INCOMPRABLE (todo precia en vidrio de botella, que
  exige lo que solo el propio Trueque vende: deadlock circular). Curva contada:
  6 → ~15 (+turba) → ~19 (+arena) → ~26 (+caliza) → ~30 (+sal) de 55 nombres; oro/
  plata/jerga ≈ 18/10/25 — los encargos se formulan en ORO, la jerga es profundidad.
  Propuesta de entrada en 3 canales (geología: vetas expuestas por el vano; economía:
  Trueque repreciado por dominio; evento: el mundo entrega). Decisiones de Cesar
  pendientes: vetas en el vano, repreciado, y el techo de la cerámica (205 > carbón
  185: ¿coque, horno concentrador, o bajar el techo?).
· ca_playtest95.cmd barre la ronda.

## Ronda 95b — LA AUDITORÍA HISTÓRICA Y LA DECISIÓN DE LAS MÁQUINAS

· Decisión de Cesar: EL PRÓLOGO SE SELLA como está — la entrada de materiales es de
  SEMILLA CERO en funcionamiento. Su pregunta: de los descubrimientos REALES de la era
  (hasta el cerámico), ¿cuántos cubre el entregable, cuáles importantes quedan fuera
  (con calificación) y qué solución Pareto nos acerca a la realidad?
· Respuesta mecánica primero: los nuevos materiales son POLVOS y la máquina de refill
  YA EXISTE (DepositoDeAgua paramétrica) — entran como SILOS COMPRADOS en el tablón,
  uno por vez (nada suelto el día 1; el orden de compra es la progresión). "Una misma
  máquina que resuelva": exactamente.
· La auditoría (doc de familias §8): canon de ~20 descubrimientos con importancia;
  cubrimos 9 plenos + 3 proxy + cobre en plan = ~60% del peso histórico. EL PATRÓN:
  casi todo el arco MINERAL/PIROTÉCNICO (77/80 puntos), casi nada del ORGÁNICO
  (fibra/madera/cuero/fermento/cera). El entregable es honesto como "LA ERA DEL FUEGO
  Y EL MINERAL"; la brecha #1 es LA CUERDA (string revolution, imp. 10).
· EL PARETO: (1) PIGMENTOS = GRATIS (negro=carbón, ocre=arcilla, blanco=cal; solo
  piden el verbo pintar — y encienden el pilar OBSERVACIÓN al pie de la letra);
  (2) LA FIBRA como material de VOCABULARIO (no base 6: las bases están horneadas en
  la seed) — una adición cubre cuerda+cestería+tejido y estrena el eje TENSIÓN;
  (3) fauna y fermento NO son omisión sino CRONOLOGÍA: eras II-III del roadmap
  esbozado (§8.4). Decisiones pendientes de Cesar: pigmentos, fibra, repreciado del
  Trueque a silos.
· ca_playtest95b.cmd barre los docs (95 la llevaba el código; si ya corrió, este
  recoge la auditoría).

## Ronda 96 — EL ROADMAP DE LAS CINCO ERAS (documento para externos)

· Pedido de Cesar: un documento llamado ROADMAP, gráfico, "para un externo y para mí":
  cuántos materiales tendría el juego que cumple el 95% de la promesa, oro/plata por
  era, orden de desarrollo, qué agrega cada sección, qué silo de autorefill necesita
  cada una, duración por cantidad de personas, y valor de venta si se ejecuta bien.
· ENTREGADO: docs/ROADMAP.md (la verdad, en el repo) + página visual compartible (los
  estratos de las cinco eras). El mapa: I EL FUEGO Y EL MINERAL (~30 jugables, cierre
  ~1 mes, 3-5 h) → II EL HILO Y EL METAL (fibra + cobre, ~42, 2-3 meses) → III EL
  GRANO Y LA BESTIA (bio-tiempo, ~57, 3-4 meses) → IV EL BRONCE Y LA RUEDA (~67,
  2-3 meses; el fuelle tumba el techo térmico) → V LA QUÍMICA PACIENTE (~79, 3-4
  meses; jabón/pólvora/porcelana). Totales: ~79 materiales jugables (35 de oro),
  12-15 meses, 25-34 h co-op (1-4, óptimo 2-3), promesa al ~95% del canon
  preindustrial al cerrar la V. Silos por era: (I) turba/arena/caliza/sal comprables,
  (II) fibra + mena por veta, (III) grano, (IV) estaño, (V) azufre y salitre.
  Precio propuesto: demo gratis → EA $14.99 (I-II) → $19.99 (IV) → $19.99-24.99 (1.0),
  contra comparables Noita/Potion Craft/Core Keeper; el diferenciador: "todo lo que
  aprendes es verdad". Fuera del arco, con nombre: era industrial, seda/púrpura/
  óptica/imprenta, remodelación (multiplicador, no contenido), lo alquímico sorteado.
· ca_playtest96.cmd barre la ronda.

## Ronda 97 — LA ENTREGA EN LA MANO (+ las respuestas de la sección 2)

· Cesar, del playtest de la sección 2: "debe iniciar llamándome como cuando me dio mi
  frasco; si no, parece que me avienta un cuchillo a distancia". HECHO: la Obra abre
  con el rito del TOMA. — la voz dice VEN. (entera, lección del beat original), la
  LUCECITA del Maestro marca el rumbo (DibujarLucecitaMaestro también en la espera de
  la Obra), y el vuelo del cincel solo nace con DistAlMaestro < distCharla.
· Los otros 4 puntos, respondidos sin tocar código (pedido explícito):
  2 (cincel: ¿limitar a construir y desbloquear después?) — de acuerdo con matiz: el
  prólogo ENSEÑA solo construir (ya es así); TALLAR se desbloquea en Semilla Cero con
  la primera veta ("el cincel también quita" cuando hay algo que quitar que importe);
  PISO con el primer encargo de construcción. Los CANDADOS reales se instalan al final
  (regla vigente de Cesar: necesita las herramientas completas para editar el mapa).
  3 (el hueco y las dos luces) — explicado: es EL UMBRAL del mandato "el ORDEN
  agranda el espacio" + la promesa del mañana (luz fría = la primera no-fuego).
  4 (mudanza: pjota a cámara con plano + puntero) — propuestas anotadas (frontal con
  plano, puntero mano abierta/cerrada, contornos punteados en todo IMovible, saturación
  bajada tipo plano de obra); Cesar elige.
  5 (institucionalizar retoques a mano) — paso a paso del flujo real documentado en el
  resumen: pintar EN PLAY (paleta F3 o cincel) → NO salir de Play → avisar la zona →
  el agente censa por sondas y lo escribe en SimLevelBuilder (regla 24) → deploy →
  permanente. El menú 6 hornea ARTE (PNG+prefab+guion), no terreno.
· ca_playtest97.cmd barre la ronda.

## Ronda 98 — LA MUDANZA CON CARIÑO DE OPUS (el punto 4, completo)

· Cesar aprobó el punto 4 entero: "me encantó. no le pongas mucho cariño al personaje
  porque es un placeholder... sí ponle mucho cariño de opus al resto". Dirección de
  escena Opus (leyó el estado VIVO del disco) implementada al número:
· EL RELOJ DEL MODO (Mudanza.EstadoT, público): entrada 0.22s / salida 0.14s con
  MoveTowards (llega a 0/1 EXACTOS, sin epsilon); _focoT (0.12/0.15) para el agarre.
  Todo IMGUI/tintes escala por EstadoT: el modo entra y sale RESPIRANDO, nunca de golpe.
· LA VISTA DE PLANO (SimRenderer.TinteMudanza + TintePlano 0.630/0.640/0.700):
  el quad del mundo (solo el quad: máquinas a color, viñeta intacta) se enfría a
  pizarra con smoothstep del EstadoT — el taller se vuelve el plano de sí mismo.
· EL PUNTERO QUE PROMETE (IMGUI, 8 texturas 13x13 dibujadas a mano en código, zoom
  entero, hotspot fijo (6,6)): mano ABIERTA sobre candidato (latón al alcance, rojo
  fuera), mano CERRADA llevando (verde/rojo según si el sitio vale), CRUCETA azul en
  el vacío. Contorno tinta (0x16,0x10,0x1E) y puño latón SIEMPRE; el clic se anuncia
  antes de hacerse.
· LOS CONTORNOS PUNTEADOS: cada IMovible se enmarca en su rect VISUAL + aire S(2),
  guion S(5)/hueco S(4), azul del icono, 0.55 al alcance / 0.28 lejos, entrada en
  cascada (stagger 35ms por aparato), el agarrado se apaga con _focoT y el resto
  baja a 0.40. ESTÁTICOS (sin latido, veto R81 #13). Esquinas siempre con tinta.
· EL CAPATAZ (placeholder a conciencia): _bodySpriteFrontal (ojos al centro, pupilas
  sin sesgo) + PlanoEnMano (rect crema 226/214/182 a -24°, bajo el brazo). El imp te
  MIRA mientras planificas; alas y cola siguen batiendo.
· La retícula del FlaskHud CEDE con fundido cruzado (1-EstadoT) salvo sobre la redoma;
  el Globo de Avisar queda FUERA del guard (canal vivo).
· TERCER RESET DEL SANDBOX en la ventana: el repo volvió a R85 pero los 4 archivos
  R98 sobrevivieron como working tree. Recuperación: reset a origin/main (R95) +
  FundacionDirector R97 e HISTORIAL rescatados DEL DISCO DE CESAR (la regla nueva de
  deployar docs con el código pagó su primera deuda). compile_fiel EXIT=0.
· El eslogan pendiente de la tarea nocturna (el reset se la llevó): README y GDD
  ahora dicen "a partir del barro, el fuego y la observación".
· VERIFICADO EN VIVO (botón 2 → Obra → ModoActivo por reflexión): EstadoT 0→1→0
  limpio, TinteMudanza=1 con quad EXACTO en TintePlano y retorno EXACTO a TinteGlobal,
  8/8 texturas del puntero sin excepción, imp frontal con plano (captura), retícula
  cedida, 0 errores de consola en todo el ciclo.
· ca_playtest98.cmd barre la ronda.

## Ronda 99 — LA SILUETA REAL Y LAS HORMIGAS (el contorno con cariño)

· Cesar, del playtest R98: "el contorno de los contenedores no es exacto, no lo
  cubre con su tubo de refill... un contorno de rallitas, algo de movimiento, se
  siente tosco y sin gracia. Mira ejemplos de traslados y tráenos buenas
  prácticas". Investigado: el canon es Photoshop/RimWorld — hormigas marchantes
  (guiones que DESFILAN a paso constante, jamás pulsando el alfa) para "esto
  está en proceso", y la silueta de la HUELLA REAL con accesorios incluidos
  (Factorio enseña la caja completa). Adaptado entero:
· IMovibleSilueta (interfaz nueva): el aparato no-rectangular dicta su propio
  perímetro (polígono rectilíneo horario, Y arriba). El depósito entrega su L:
  cuerpo + saliente del tubo (2 celdas del flanco, tapón en _y1+4), del lado
  donde el tubo VIVE HOY (Reposicionar lo espeja; el signo de localPosition.x
  es la verdad). Agarre/silueta/CabeEnAncla siguen midiendo el cuerpo.
· LA MARCHA: una sola corrida de guiones (S(5)/S(4)) recorre el perímetro
  entero con fase global Time.time — un periodo por segundo, sentido horario,
  el paso del delineante paciente. Los guiones CRUZAN las esquinas
  partiéndose. TRASLACIÓN pura: el veto R81 #13 (alfa que respira) sigue en pie.
· EL CANDIDATO EN LATÓN: el aparato bajo el cursor al alcance marcha en el
  latón del puño del puntero (mismo vocabulario: latón = puedes) y sube a 0.85.
· InflarPoligono: el aire S(2) ahora infla el polígono entero (arista fuera por
  su normal exterior, vértices recruzados: la vertical fija X, la horizontal Y).
· DOS BUGS CAZADOS EN LA VERIFICACIÓN EN VIVO (captura de escritorio real,
  permiso de Cesar — primera vez que el agente VE la IMGUI):
  (1) EL CENSO MUDO (latente de R98): el stagger tapaba el retraso a 0.28 s con
  presupuesto total EstadoT*0.22 ≤ 0.22 — todo índice ≥ 7 quedaba en alfa CERO
  PARA SIEMPRE. Con la estantería registrada (8 baldas + 6 anclajes + 2 pilas
  ANTES que los tanques) el censo entero desaparecía — la causa raíz del "No vi
  esto" de Cesar. Techo 0.13 y el índice cuenta VIVOS, no huecos.
  (2) InflarPoligono mutaba vértices EN SITIO mientras el test de verticalidad
  leía los ya inflados: polígono corrupto, contorno invisible. Todo se mide
  sobre el original antes de tocar un vértice.
· VERIFICADO EN VIVO con capturas de escritorio: ambos tanques con su L exacta
  (sonda: 6 puntos, (411..423)×(140..159) con hombro en 156), guiones en fase
  distinta entre capturas (la marcha marcha), manita del puntero visible,
  0 errores de consola. Los 16 muebles de la estantería también censados.
· ca_playtest99.cmd barre la ronda.

## Ronda 100 — LA MUDANZA COMPLETA: forma, suelo honesto, espejo y los marcos

· Veredicto del playtest 99 (Cesar): (1) "el contorno está hermoso... el cuadrado
  verde no tiene la forma exacta del contorno, sigue siendo básico"; (2) "puedo
  colocar las cosas volando con un poco de paciencia" (colocar→agarrar→una línea
  arriba→repetir); (3) "presioné la L para espejar y no vi nada"; (4) "los
  cuadros blancos que indican dónde posicionar están bien básicos y se sobreponen".
· LA SOMBRA CON FORMA (IMovibleSilueta.SiluetaRelativa): al agarrar se hornea UNA
  textura de 1 px por celda con la silueta exacta (la L con su tubo, del lado
  pedido) y el preview la usa en vez del 1x1 estirado. Cero costo por frame: solo
  se rehornea al agarrar o al pulsar L. El rect genérico queda de fallback.
· EL SUELO HONESTO (ApoyoFirme, extraído del bloque R68): EL APARATO NO SE APOYA
  EN SÍ MISMO — como el aparato real queda plantado mientras lo llevas, su propio
  muro era Stone+obra un renglón arriba: la torre voladora. Toda celda de su rect
  visual actual cuenta como evaluada pero jamás como apoyo. Y LA SILUETA YA NO
  MIENTE: el apoyo entra a la validez visual (verde = las TRES verdades: mundo +
  alcance + suelo), antes se pintaba verde sobre el aire y el soltar la desmentía.
· LA L ESPEJA (IMovibleEspejable, práctica estándar de todo modo de colocación —
  RimWorld/Factorio/Sims rotan con tecla sobre el fantasma): llevando, el deseo
  queda PENDIENTE y la silueta voltea al instante; sobre un candidato sin agarrar,
  espeja EN SITIO. El aire manda (flanco tapado → el otro; ninguno → se queda) y
  cada resultado tiene su aviso. AplicarFlancoTubo (extraído de R93) honra la
  preferencia; soltar en el mismo sitio también espeja (el early-return lo aplica).
· LOS MARCOS DE LOS SLOTS: los rects visuales de los tanques se pisan 2 columnas
  (anclas a 8, vuelo +1 por lado) — por eso "se sobreponen". Ahora cada slot es un
  marco punteado QUIETO sobre la huella real (8 celdas, adyacentes muro a muro,
  cero solape) vía Mudanza.MarcarRectMundo (el trazo del delineante extraído y
  prestado: ProyectarPoliAPantalla + MarcharPoliEnPantalla; fase 0 = el destino
  espera, no desfila — dos lenguajes, cero confusión).
· EL ESPECTROFOTÓMETRO: el velo blanco de los slots salía como LOSA GRIS aunque
  bajáramos el alfa — medido en el PNG: rgb 64 con alfa 0.05. El proyecto rinde en
  espacio LINEAL: alfa chico sobre negro ≈ (alfa)^(1/2.2) percibido, 0.05→~25%.
  No hay número chico que lo salve: el velo MURIÓ (ApagarSilueta — los objetos
  quedan como puro estado del beat) y el marco punteado es el marcador entero.
· CUARTO RESET del sandbox a mitad de ronda: recuperado de GitHub (Cesar subió
  la R99) + los 3 archivos R100 rescatados DEL DISCO (la regla del deploy
  inmediato volvió a pagar). Y cazado en vivo: Unity NO recompila en Play — el
  Refresh con el editor corriendo difiere el reload y las sondas mienten
  (verificar con un cambio de firma: LatirSilueta params=3).
· VERIFICADO EN VIVO (sondas + capturas de escritorio): apoyo(en sitio)=true /
  apoyo(1 arriba)=FALSE (la torre muere); forma 12x19 celdas, offset -2 al
  espejar; espejo en sitio: tubo derecha→izquierda logrado; marcos punteados
  limpios con divisor doble al centro y CERO losa; 0 errores de consola.
· ca_playtest100.cmd barre la ronda.

## Ronda 101 — LOS MARCOS VERDADEROS, EL CHECK Y LA LIMPIEZA DEL ORDEN

· Cesar mandó captura con cuatro detalles del Acomodo: (1) "las siluetas no
  tienen la misma dimensión ni forma de los contornos de los contenedores";
  (1b) "un ligero relleno blanco latiendo... y un sonido gratificante de check
  al colocar... del mismo tono de la indicación WASD"; (2) "el contenedor de
  arcilla debe caer más a la izquierda, que no se cruce con los contornos";
  (3) "quedan cositos que sobresalen del suelo que no puede quitar el cincel
  entre los dos contenedores... resto de algo que ya no usamos".
· (1) LOS MARCOS SON LA L VERDADERA: cada slot libre dibuja la MISMA silueta
  del contenedor en su POSE FINAL — rect visual completo (10 de vuelo a vuelo,
  domo incluido) + saliente del tubo ESPEJADO como quedará al encajar (A tubo
  izquierda, B derecha: dos sujetalibros). Mudanza presta MarcarPoligonoMundo
  (el trazo quieto ahora acepta el polígono entero, mismo dialecto rectilíneo
  de IMovibleSilueta). Los cuerpos se cruzan 2 columnas al centro — como se
  cruzarán los sprites reales: verdad, no error.
· (1b) EL RELLENO LATIENTE va en IMGUI — el espacio GAMMA, donde un alfa chico
  ES chico (la lección de la losa lineal R100): tono FichaFondo del tutorial
  WASD (0.93/0.93/0.90), late 0.050±0.028 a 2.4 rad/s, y se PARTE en el eje
  387 para que el cruce de cuerpos no sume doble brillo. Y EL CHECK: BlipSlot
  ahora suena a TutorialConfirma (EL MISMO timbre de las fichas WASD — el
  orden de menú al que pertenece el marcador) + clank corto a pitch 1.15:
  el check y el peso, juntos.
· (2) EL SILO RENACE CORRIDO: FundacionSiloRenacerX0=364 (−5) SOLO para el
  ORDEN (InitSilo con xRenacer, que además hace ceder la autoridad del
  marcador de escenografía); el silo del acto 1 sigue clavado al plano de la
  poza. Con x0=364 el tubo remata en 375 y el marco A (que baja hasta 376)
  queda a una celda de aire. Suelo verificado con sonda antes de elegir el
  número (y139 sólido, aire limpio hasta 358).
· (3) EL ORDEN BARRE EL HOGAR VACÍO: los cositos eran LAS MEJILLAS DEL FOGÓN
  del jugador (x396-397 y x405-406, piedra 2×3 con obra anticincel) — el
  fuego propio nunca llegó al guion final del prólogo (la turba se retiró en
  R79). RetirarFogon() borra las mejillas (el suelo y139 no se toca) y
  degenera FundacionFogonObra (handle nuevo, guardado al registrar); corre en
  el renacer del ORDEN real Y en el checkpoint. El acto 1 conserva su fogón
  (regla 15: la tarea del fuego primitivo queda retirada del prólogo, no del
  juego).
· QUINTO RESET del sandbox al abrir la ronda — recuperación limpia de GitHub
  (Cesar ya había pushado la R100); esta vez CERO pérdida.
· VERIFICADO EN VIVO (sondas + captura de escritorio): mejillas restantes 0 y
  obra del fogón muerta, SiloDeLodo.x0=364 con el depósito intacto en 412,
  marcos en L con sus escalones de tubo en los flancos y relleno suave sin
  losa, 0 errores de consola. El check sonoro se estrena con Cesar (audio no
  capturable).
· ca_playtest101.cmd barre la ronda.

## Ronda 102 — LA LIMPIEZA DEL OESTE, LOS SLOTS QUE COLINDAN Y EL REPARA

· Cesar con dos capturas: (1) "elimina esos residuos que se ven de la roca
  madre a la izquierda, y las luces perennes esas al extremo izquierdo,
  quítalas sí o sí — haremos otra cosa ahí"; (2) "ambos contornos se invaden,
  cuando deberían a lo mucho colindar, idealmente equidistantes a los dos
  reservorios y no pegaditos al de arcilla"; (3) "cambiar el verbo ALZA que no
  se entiende por REPARA o algo así".
· LOS RESIDUOS ERAN LA ESTANTERÍA DE SEMILLA CERO: Mudanza.Init spawneaba
  baldas/anclajes/pilas también en la fundación — su geometría (BaldaPlanes y
  compañía) mide el taller del OTRO mundo, y en el prólogo los herrajes de
  latón caían flotando sobre la roca madre del oeste. Compuerta en
  SpawnBaldasYAnclajesSiCorresponde: ModoFundacion → return (el mueble entra
  al juego cuando entra su mundo). Bonus: el censo de la mudanza del prólogo
  queda en 2 movibles reales, sin 16 fantasmas.
· LA LUZ FRÍA DEL VANO SE RETIRÓ (regla 15): era la "promesa del mañana" de
  la R94 (fondo + derrame respirando, al 45% durante el negro del título).
  El paso 6 del Adiós queda como respiro sobre el vano en sombra — el hueco
  recortado contra la roca sigue contando la salida. Cesar hará otra cosa ahí;
  DibujarLucesPersistentes es el sitio cuando lo decida.
· LOS SLOTS SE SEPARAN A 10 (SlotAX0/BX0 = 378/388): las huellas dejan 2
  celdas de aire entre tanques, los marcos (ahora EL CUERPO del contenedor:
  rect visual 10x19 con domo, SIN saliente de tubo) COLINDAN exacto en 387 —
  cero invasión — y el par centra el plinto (386.5), con aire hacia el silo
  corrido de la R101. Cada huella cuelga 1 celda sobre el escalón medio del
  zigurat (7/8 de apoyo: pasa el 70%). EL IMÁN DECRETA LA POSE al encajar:
  slot A espejado a la izquierda, B a la derecha — los sujetalibros; el
  jugador puede re-espejar con L, el aire seguirá mandando.
· ALZA → REPARA (voz y placa de la Obra): el derrumbe se repara — plataforma
  y techo bajo el mismo verbo, ahora comprensible.
· SEXTO RESET del sandbox al abrir la ronda; recuperación GitHub (R100) +
  disco de Cesar (R101 completa, aún sin push). Y un corte del puente en
  plena verificación — el deploy inmediato ya había pagado: cero pérdida.
· VERIFICADO EN VIVO: baldas=0, anclajes=0, pilas=0 en fundación; encaje
  simulado por sondas — dep.x0=378 tubo IZQUIERDA, silo.x0=388 tubo DERECHA
  (captura RT del par sujetalibros con sus tubos a los flancos y 2 de aire
  entre huellas); REPARA en voz y placa; 0 errores de consola.
· ca_playtest102.cmd barre la ronda.

## Ronda 103 — EL DECRETO DEL ACOMODO: sitio exacto, forma exacta, la L enseñada

· Cesar, cuarto veredicto sobre los marcos: "no tienen la forma de los
  recipientes, se volvieron a achicar... asegúrate de que la huella de pista
  sea igual a la de los contenedores, NO los vuelvas a entrelazar por amor a
  dios... siguen bien pegadito al de arcilla y no equidistante a ambos...
  lo ideal es que las sombras solo admitan colocarse en lugar exacto y con
  la forma exacta — así enseñamos la L, con la indicación WASD y el sonido:
  es un gran momento para enseñarlo".
· EL PLINTO SE CENTRA (+6: PlintoX0 {383,384,385}, X1 {402,401,400}; slots
  384/394): el par de marcos con sus tubos (381-405) respira SEIS celdas de
  aire por cada lado — silo (tubo hasta 375) y depósito (visual desde 411) —
  equidistante de verdad. Los focos cinemáticos que citaban el plinto por
  literal (perfilado 377, cierre del adiós 387) ahora leen PlintoX0/X1: la
  cámara sigue a la piedra, no a un número muerto (regla 47).
· LOS MARCOS SON LA L ENTERA, SIN ENTRELAZADO: cuerpo 10x19 con domo +
  saliente del tubo hacia AFUERA (A izquierda, B derecha — los sujetalibros).
  Con los slots a 10, los cuerpos colindan en UNA arista (la costura 393) y
  las siluetas no comparten ni una celda. Relleno latiente por pieza (cuerpo
  + tubo), fills adyacentes sin doble brillo.
· EL DECRETO DE SITIO (Mudanza, API nueva): mientras el director lo dicte,
  soltar solo vale en las anclas decretadas (±2/±1, el pulso del imán) y CON
  el espejo del marco. Tres veredictos: vale / lejos ("aquí no — su marco lo
  espera en la plataforma") / flanco equivocado ("el tubo va al otro flanco —
  L lo espeja": LA LECCIÓN en el momento exacto). La sombra se pinta roja
  fuera del decreto — la mano cerrada del puntero dice lo mismo gratis. El
  director lo refresca por tick del Acomodo (solo slots libres) y una RED DE
  SEGURIDAD lo levanta en cualquier otro beat (pariente de la regla 59: el
  decreto es estático y sobrevive al RestartRun). El imán ya no decreta pose:
  lo que llega ya viene bien volteado por el jugador.
· LA FICHA DE LA L, estilo WASD con su sonido (TutorialContextual): la cadena
  del Acomodo ahora es V ("la mudanza recoloca el taller") → CLIC IZQ
  ("llévalos a la plataforma", confirmada al primer agarre) → L ("el tubo
  elige flanco", confirmada al presionarla con su TutorialConfirma). Como el
  depósito y el silo renacen con el tubo a la derecha y el slot A exige
  izquierda, al menos UNA colocación pide la L: la enseñanza es inevitable.
· Mudanza.LlevandoAparato (estático) para que el tutorial sepa CUÁNDO enseñar.
· Cesar subió el playtest 101 y arrastró la R102 (un solo commit, GitHub
  manda): sandbox sincronizado por reset a origin, sin pérdida.
· VERIFICADO EN VIVO con sondas: decreto = [(383,143)izq, (393,143)der];
  veredictos A/tubo-derecha=2 (la lección), B/tubo-derecha=0, lejos=1,
  A/tubo-izquierda=0 (también con tolerancia +2/+1); red de seguridad: al
  salir del Acomodo, HayDecreto=False al frame siguiente; 0 errores de
  consola. Los marcos nuevos se estrenan con Cesar (misma ruta de dibujo ya
  verificada en R101).
· ca_playtest103.cmd barre la ronda.

## Ronda 104 — EL TÍTULO CALLA (previa a la primera prueba con usuarios)

· Cesar, antes de su PRIMERA PRUEBA CON USUARIOS: "elimina el título de TEN
  THOUSAND YEARS al final de la parte 2 del prólogo — no hemos construido ese
  momento aún, y encima estaba súper grande. Lets go".
· El paso 7 del Adiós queda como EL AMANECER: el ritmo intacto (negro →
  subgrave → el mundo ensamblándose alrededor de su fuego), solo calla la
  palabra. DecirTitulo/_vozEsTitulo/PrepararEstiloTitulo quedan vivos (regla
  15) esperando el momento del título que sí se construya.
· Compilado EXIT=0, Refresh con editor detenido, 0 errores. Cambio quirúrgico:
  un solo archivo, cero ritmos tocados — listo para la prueba.
· ca_playtest104.cmd barre la ronda.

## Ronda 105 — LA CADENA DEL FUEGO (el roadmap se vuelve infografía)

· Cesar: "extiende el archivo, la web de las cinco eras: quiero la cadena del
  fuego — qué temperaturas me permiten hacer qué materiales en qué etapa; cuando
  la fogata no alcance, los nombres y las imágenes chiquitas de las estructuras
  más representativas de la humanidad; el imaginario colectivo y el porcentaje
  de fidelidad. Nosotros ya tenemos algo definido".
· LO DEFINIDO, leído del código (regla 24): °C = raw×2 − 120 (CellGrid);
  bandas de extracción {106,124,136,148,158}; combustibles 165–190; umbrales de
  arcilla 180/188/205. La correspondencia juego↔historia es POR PELDAÑO, no por
  número: la magia comprime tiempo Y temperatura.
· LA CADENA (7 peldaños): fogata (400–700°C, adobe/pigmentos) → hoyo de cocción
  (600–900, la primera cerámica ~26.000 a.C.) → horno de tiro + calera
  (900–1.100, cal/esmaltes/vidrio: EL TECHO DE LA ERA I) → crisol y fuelles
  (1.100–1.200, el cobre funde a 1.085: Era II) → horno de lupia (1.200–1.300,
  hierro SIN fundir: Era IV) → horno dragón (1.250–1.400, porcelana y la
  redención del gres: Era V) → alto horno (1.400–1.600, acero que asoma: la
  frontera donde el arco termina).
· EL ARTIFACT "Las Cinco Eras" reconstruido y extendido AL MISMO URL (Cesar
  aprobó sobrescribir): tipografías DEL JUEGO (Cinzel+Alegreya), doble escala
  humanidad↔raw con rampa de incandescencia real, 7 estructuras dibujadas en
  pixel-SVG propio (la CSP prohíbe imágenes externas: se dibujaron a mano),
  chips de era con paleta validada por daltonismo en claro Y oscuro (5 pasadas
  del validador), pacto de fidelidad como leyenda, y todo el contenido v1
  (mapa, detalle por era, ritmo, fronteras, precio, riesgos) conservado.
  Render verificado en ambos temas con Chromium headless.
· docs/ROADMAP.md gana el §2.5 con la tabla textual completa + notas de
  honestidad (qué es código vivo, qué es promesa, qué es consenso arqueológico).
· ca_playtest105.cmd barre la ronda (solo docs).

## Ronda 106 — LOS MATERIALES DE LA DEMO (el artifact hermano + las misiones)

· Cesar: "uno nuevo, distinto, en el mismo estilo artístico: todos los
  materiales de la demo con sus nombres, las minihistorias, cómo se ve el
  cuadrito con puntitos, cómo evoluciono de uno a otro; y debajo la propuesta
  de misiones — entretenidas, identificables, contextualizadas, ojalá nombres
  comunes, buscados con astucia. El playtest 106 debe contener este avance y
  el de las cinco eras".
· ARTIFACT NUEVO «Los Materiales de la Demo»
  (https://claude.ai/code/artifact/9b1ecbca-52de-4373-80f1-c28dc2772a9b),
  mismo lenguaje visual que Las Cinco Eras (Cinzel+Alegreya, ceniza/brasa/
  pátina): ~30 fichas en 5 familias (kit del despertar → +turba → +arena →
  +caliza → +sal) con la MINIHISTORIA VERBATIM de Universe.cs, el swatch de
  16×16 generado en canvas con EL COLOR Y EL JITTER REALES del código
  (Encargo Q: tabla _coloresRealesPorBaseEstado + _jitterPorEstado — "lo que
  ves aquí es lo que verás ahí"), clase ORO/PLATA/JERGA, y la cadena de
  evolución de cada familia como tira de nodos→verbos.
· LAS 10 MISIONES propuestas, todas en ORO con ancla en el imaginario: EL
  CÁNTARO (el horno ES la misión — la decisión abierta del techo 205), LA
  PARED QUE FALTA (adobe+mortero al boquete del derrumbe), CARBÓN PARA EL
  INVIERNO, VIDRIO PARA LA REDOMA (la misión ES el desatasco del deadlock del
  Trueque), ENCALA LA CASA (la exotérmica que se siente), LA MANO EN LA PARED
  (Altamira, pilar observación), QUE NO GOTEE (templado vs recocido sin
  decirlo), PIEDRA LÍQUIDA (hormigón: puente a la remodelación), EL SALARIO
  (la sal como economía), LA COLADA (la lejía que siembra el jabón de la V).
· docs/DEMO_MATERIALES_Y_MISIONES.md: el gemelo textual (curva, tabla de
  misiones, estado honesto de qué está cableado y qué es propuesta).
· Render verificado con Chromium headless (fichas, swatches con grano real,
  cadenas y misiones); paleta heredada de la R105 (ya validada en ambos temas).
· ca_playtest106.cmd barre la ronda — y con ella viaja también la R105 (la
  cadena del fuego), que seguía sin subir: el pedido explícito de Cesar.

## Ronda 107 — EL DECRETO DEL MUNDO (los textos se ponen firmes)

· Sábado de consolidación, mandato de Cesar: "asegurarnos que estas nuevas
  ideas de diseño están firmes en todas partes, y que las abandonadas salgan
  del registro para eliminar peso". Sin código: solo la verdad por escrito.
· GDD gana el §0 EL DECRETO DEL MUNDO (SELLADO): ruinas amables, máquinas
  heredadas que se REPARAN (resuelve la incoherencia del vidrio), regla de
  hierro R60 ("lo heredado se repara, jamás se desguaza" — ahora regla
  numerada en CLAUDE.md), "no empezamos de cero: la materia quedó, el
  conocimiento se perdió", el muñeco de remiendos como avatar canónico
  (levita; intro LEVÁNTATE.; variantes por máscara), nodos habilitadores por
  seed + caravanas del tablón por el vano, dirección visual 2D artesanal con
  acabado ABIERTO.
· docs/DIRECCION_DE_ARTE.md NUEVO: el documento madre de la capa visual —
  identidad, personaje, pipeline 3D-como-herramienta (cabeza rígida, máscara
  de acento, 6 animaciones, 3 perillas de convivencia, riesgo del tratamiento
  nombrado) y el reparto Cesar/agente.
· ESTADO.md: bloque DECISIONES CERRADAS ("el diseño mayor está sellado") +
  RETIRADO DEL TRACK (tolva, estantería en fundación, luz del vano, hogar
  vacío, veta de turba del plano, título del Adiós, clima por zona, el imp
  como definitivo, el pixel como supuesto). README y ROADMAP §0.5 alineados;
  DEMO_MATERIALES gana el §4 con las DOS máquinas (hoyo + horno) y la
  cerámica sin licencia. CLAUDE.md: foco nuevo (la capa visual).
· ca_playtest107.cmd barre la ronda (solo docs).

## Ronda 108 — EL MUÑECO ENTRA AL TALLER (talla y cámara selladas)

· Cesar entregó el PNG del muñeco de remiendos (cubo de tablones con parches
  de metal, musgo, brote, ojos-lámpara, túnica de arpillera) y pidió medir
  talla y distancia de cámara con referencias de mercado, no a gusto propio.
· Investigación previa: la vara es el % de altura de pantalla. Noita 5-6%,
  Terraria 4% (pero la comunidad juega a 150-200% de zoom), Dome Keeper 5-6%,
  Celeste/Animal Well ~9%, Hollow Knight ~10%, Eastward/Moonlighter 10-13%.
  Regla emergente: bajo ~6% el cariño es silueta+movimiento; la cara existe
  desde ~8%; los ojos expresivos viven de 10% para arriba.
· CORRECCIÓN medida en vivo: el juego ya muestra 90 celdas por defecto desde
  el playtest 21 (CuartoIntimoZoomFactor 0.625), no 144 — el instinto de
  cámara íntima ya estaba tomado hace meses. El aprendiz viejo: 9 celdas=10%.
· BANCO DE TALLAS (alkahest_arte/componer_tallas.py en el sandbox): el
  muñeco recortado con rembg (isnet), compuesto a 10/12/14 celdas sobre
  capturas reales del RT a 80/96/120/144 celdas visibles. Veredicto: T=14
  compite con las ruinas (le llega al hombro al depósito — prohibido), T=10
  no cambia el peso, T=12 a 80-96 celdas es el punto (12.5-15%, liga
  Eastward): ojos y brote legibles, el maestro más alto, los tanques doblan.
· SELLADO (Cesar: "acepto todas tus recomendaciones"): talla 12 celdas,
  defecto se queda en 90, la rueda gana ZOOM IN hasta 80 celdas
  (ZoomRuedaMinCerca=8/9). Colisión INTACTA: solo crece el cuerpo visual.
· Implementación: el PNG vive en Resources/Personaje/MunhecoRemiendos.png
  (1200 px de alto a 1000 px/unidad = 1.2u = 12 celdas exactas; mips +
  bilinear para encoger sin chisporroteo). ApprenticeController asciende la
  ruta customSprite a ESTAMPA de primera clase: Resources.Load si el
  inspector no manda otra cosa, bob + bandeo (tilt) + espejo por dirección +
  frasco cargado + plano de mudanza (CrearFrascoYPlano compartido con el rig
  procedimental, que queda como retén honesto). Sin parpadeo: los ojos del
  muñeco son lámparas. En mudanza no se espeja (el arte ya es 3/4 frontal).
· El imp procedimental NO se borra: es el fallback si falta el asset, y sus
  texturas siguen siendo la referencia de "cero assets" del M5.
· Pendiente que el pipeline 3D reemplace la estampa con animaciones reales
  (DIRECCION_DE_ARTE.md); las 4 capturas del banco viajan en el chat de la
  ronda y las composiciones son reconstruibles (script + PNG + checkpoint).

## Ronda 109 — EL DEFECTO ES LA INTIMIDAD (y la lección de la escala)

· Cesar probó al muñeco y jugó TODO el rato pegado al tope de la rueda (80
  celdas): "ahí apenas alcancé a sentir que yo era el personaje". Ley vieja
  del oficio: lo que el jugador elige siempre debe ser el defecto.
· CuartoIntimoZoomFactor 5/8→5/9 (defecto 90→80 celdas). La rueda guarda
  reserva de intimidad hasta 72 (piso 0.9). WideViewMultiplier 2.2→2.475:
  Tab y el tope de alejar dan las MISMAS 198 celdas de siempre — el gesto
  del plano no cambia ni un pelo.
· Su segundo hallazgo es el importante: a esta cercanía el MUNDO se siente
  de juguete ("yo soy muy grande en comparación con el resto"). Respuesta
  técnica dejada por escrito en DIRECCION_DE_ARTE: el zoom agranda todo por
  igual y NO puede cambiar la proporción personaje/mueble — encoger al
  personaje + zoom más agresivo es EXACTAMENTE la misma imagen con
  partículas más gordas. La proporción solo se corrige en celdas: crecen
  los muebles. Las partículas ya se sienten bien (dixit Cesar), así que no
  se toca ni zoom ni talla: crecen los reservorios.
· Maquetas entregadas sobre la captura real del muñeco vivo: reservorios a
  x1.35 (19→~26 celdas) y x1.60 (19→~30). Pendiente el veredicto de Cesar
  para la cirugía de verdad (huella en celdas: InitSilo anchoHuella/
  altoHuella, slots del plinto, L de mudanza, marcos, decreto — todo
  paramétrico, pero es cirugía, no sprite).
· Dato honesto para la idea de "espacio real" de Cesar: el depósito HOY
  guarda 72 celdas y su vidrio muestra ~78 — ya es casi 1:1. El que
  comprime espacio es el FRASCO (900 en un tarro). Con reservorios x1.35
  el vidrio mostraría ~140: espacio real con margen de sobra.

## Ronda 110 — LAS RUINAS TORREAN (x1.6, y el muñeco pisa y pesa)

· Veredicto de Cesar sobre la maqueta: "tengo que hacer la prueba 1.60 y aún
  le puede faltar más". Además: -40% de velocidad ("lo siento muy rápido") y
  el colider honesto ("invade el terreno como un 15% de su dimensión").
· LOS RESERVORIOS x1.6: huella 8x13 → 14x24 (visual 10x19 → 16x30; capacidad
  72 → 276 celdas). Todo el aparato de la mudanza (L, decreto, marcos,
  ApoyoFirme) era paramétrico desde R99-R103 y escaló solo; los literales
  del plano no, y ahí fue la cirugía:
  - LA CAVERNA SE ENSANCHA (-20 izquierda / +8 derecha: interior 300-468).
    Cadena del agua corrida -20 (manantial, repisas, poza, veta); brasas +4
    y mesa/cuenco +6 para darle aire al tubo del depósito crecido.
  - Silo asiento 349-362, renacer del ORDEN en 353; depósito 412-425; ambos
    y140-163. MARCADORES de escenografía movidos y escena guardada (base-
    centro: depósito 41.9, silo 35.6).
  - Plinto {374,375,376}/{406,405,404}; slots 375/391 (huellas 375-388 /
    391-404, costura 390); marcos L con cuerpo 16, tapón +27, domo +30 —
    espejo exacto de SiluetaRelativa. Derrumbe (390) quedó EXACTO en el
    centro del plinto nuevo sin moverlo: carambola del ensanche asimétrico.
  - Cargas del checkpoint 20/16 → 75/60 (misma fracción visible en un vidrio
    x3.8). La piel horneada del acto 1 se ESTIRA (1.60x1.58) hasta la v2.
· VELOCIDAD: moveSpeed 6.7 → 4.0 (-40% clavado, pedido textual).
· COLIDER (regla 39: medido, no prosa — alfa>32 sobre el PNG): cubo 0.677u
  de ancho, tapa +0.437, pies -0.598. Caja 0.64 ancho, +0.48/-0.64 con el
  bob: los pies ya tocan suelo (antes se hundían 2.2 celdas — el 15% que
  Cesar midió a ojo, que era 18% real). Solo el BROTE asoma (punta blanda,
  heredero de las antenas). Chaflán 2→3, proporcional a la caja.
· COSTE ASUMIDO (para reportar a Cesar): con caja de 6.4 x 11.2 celdas, NINGÚN
  túnel de una pasada de cincel (5 de luz) deja pasar al muñeco — antes los
  pozos verticales sí daban. Hace falta doble pasada en ambos sentidos. Es
  coherente (un cuerpo de 12 celdas no cabe por un tubo de 5) pero cambia
  el excavado; si duele en juego, se revisa A PROPÓSITO.

## Ronda 111 — LA RUEDA APRENDE A VOLVER A CASA (y el mundo pide crecer)

· Veredicto de Cesar sobre la escala x1.6: "está al límite, hasta quisiera
  que fuera más grande... es lo más chico que puede ser y sentirse bien".
  Acordado: se prueba ASÍ y la sensación de mundo grande vendrá de CONTENIDO
  (densidad, máquinas, ruinas), no de más inflación — el ensanche ya comió
  la holgura de la caverna y x2 se comería el mundo de 768.
· LA RUEDA (Cesar: "muy sensible... incapaz de volver a la distancia
  default"): el diagnóstico era brutal — ZoomRuedaPaso 0.28 x el
  multiplicador 2.475 = 0.69 de factor POR MUESCA, en un rango total de
  1.7. Dos muescas cruzaban todo. Paso 0.28 → 0.065 (~9 muescas el alejar,
  1-2 el acercar) + EL RETÉN DEL DEFECTO: si una muesca cruza el 1.0, la
  rueda frena EXACTO en la vista por defecto; el siguiente clic continúa.
  Volver a casa ya no es puntería.
· Un tick más de cercanía: ZoomRuedaMinCerca 0.9 → 0.8 (64 celdas en el
  tope de acercar; el defecto sigue en 80).
· VELOCIDAD: 4.0 "se sintió muy lento" → 4.8 (+20% exacto pedido).
  Historial del número: 6.7 (origen) → 4.0 (R110) → 4.8 (R111).
· Reset #8 del sandbox mid-ronda: repo volvió a R85; recuperación estándar
  (reset a origin/R109 + rescate de la R110 completa desde el disco de
  Cesar vía stage). El deploy inmediato volvió a pagar: CERO pérdida.

## Ronda 112 — LA GOTA GORDA (el refill alcanza el vidrio nuevo)

· Cesar: "asegúrate que los contenedores sean capaces de llenarse a tope
  nuevamente... no me molesta que se tarde... quizás solo haga falta
  mantener el tiempo máximo de refill para terminar de llenarlo a tope".
· El bug latente: refillTopeCeldas=72 en el guion (asset serializado, R58)
  contra un vidrio que desde la R110 admite 276 — el goteo se plantaba al
  cuarto de tanque. Fix ESTRUCTURAL: el tope ya no se configura, ES
  Capacidad() leída del vidrio real (no puede volver a quedarse fósil);
  el campo del guion queda retirado con nota R15.
· EL TIEMPO SE MANTIENE (el pedido exacto): la curva cuadrática de la R91
  no se toca (mismos eventos, misma integral de ~3 minutos); lo que crece
  es LA GOTA — cada evento deposita ceil(tope/72)=4 celdas en una fila
  corta en el tope del vidrio, centrada en el inlet: cae con física a la
  vista, más ancha — un chorro de a 4 le queda bien a un tubo GRUESO. La
  cola silenciosa de la R91 completa lo que la fila ya no admite.
· Verificado en vivo con sondas: el conteo cruza el tope viejo (72) y
  sigue hasta el vidrio entero.

### R112b — LA REVISIÓN JUGADA (el agente recorre el prólogo de verdad) + BUILD

· Mandato de Cesar mid-ronda: "revisa todo desde la primera sección del
  prólogo y realiza ajustes a tu criterio, prepara al final una build".
· PRIMERA VEZ que el agente JUEGA el prólogo con inyección de input real
  (Input System QueueStateEvent: teclado WASD sostenido y ratón con puntero
  en coordenadas de mundo proyectadas): tutorial WASD completado, VEN/TOMA
  con el vuelo del frasco, succión REAL de la poza (105 celdas de agua),
  vertido al cuenco, DERRUMBE disparado, entrega de lodo, emergencia de
  ambos reservorios x1.6, llenados (48 agua / 24 lodo) y REORDEN completo
  hasta la Obra. Diez beats seguidos SIN una sola rotura de geometría.
· Verificaciones de la ronda que cayeron solas en el camino: la cadena del
  agua a -20 con su guía punteada ✔, el reventón del derrumbe clavado en el
  eje 390 ✔, el cuenco en 459 ✔, los marcadores movidos anclan los tanques
  en 412/349 ✔, y LA GOTA GORDA EN VIVO: tras el renacer, agua=182 y
  lodo=185 — ambos MUY por encima del tope fósil (72) y subiendo hacia las
  276, con el vidrio del silo visiblemente a dos tercios. La R112 quedó
  probada jugando, no solo con sondas.
· AJUSTES A CRITERIO: ninguno hizo falta — la parametrización de R99-R110
  aguantó la revisión entera. (Observación menor anotada: durante el
  derrumbe el foco cinemático es breve y la cámara vuelve pronto al
  aprendiz; no es regresión — mismo comportamiento de antes del ensanche.)
· BUILD DEMO WINDOWS generada por el menú 3 vía agente (la validación de
  escena previa PRESERVÓ los marcadores movidos): Succeeded, 0 errores,
  213.3 MB, 31.5 s — Builds/TenThousandYearsDemo/TenThousandYears.exe con
  TODO lo de las rondas 108-112 dentro (muñeco, cámara, escala, rueda,
  velocidad, colider, refill).

## Ronda 113 — LA CASCADA A ESCALA (y el muñeco toca su mundo)

· Cuatro veredictos de Cesar tras jugar la build + su pregunta de integración.
· METAS ESTRUCTURALES (el remedio de la gota gorda, aplicado a la exigencia):
  el LLÉNALO. pedía 48/24 celdas — el 67%/33% del vidrio VIEJO; con 276 de
  capacidad "con echarle un poquito ya se salta". Ahora MetaLlenado(rec) =
  60% de Capacidad() leída (166 hoy) y los campos del guion quedan retirados
  (R15). Sin riesgo de atasco: la gotera del montículo repone hasta 70 por
  ciclo — son viajes, no un muro.
· EL IRIS DEL REORDEN ABRAZA A AMBOS (Cesar: "solo tengo luz sobre uno... el
  otro me lo encuentro por sorpresa"): con el ensanche los tanques renacen a
  76 celdas de punta a punta y el círculo de S(520) dejaba uno a oscuras.
  S(700): la instalación de los DOS tubos se ve entera.
· LA CASCADA A ESCALA (Cesar: "se siente como algo muy diminuto"):
  - Pozo y cuenco al DOBLE de hondo (FundacionPozoHondo 5→10, factura
    compartida) y el cuenco de entregas de 7→11 de boca (item 4).
  - Chorro ≥4 celdas por pulso con la grieta crecida a 3 de boca.
  - Repisas con GROSOR 3 (creciendo hacia abajo: la línea de flujo no se
    mueve) y DOS TONOS (cuerpo de roca + ceja de piso estructural): cornisa
    labrada, no relleno. El labio crece con ellas.
  - Y CON COLISIÓN: el "lo atravieso" venía de la ronda 70 (obra = paso
    franco, pensado para las máquinas, que arrastró a las repisas).
    EsRepisaDeCascada() es la excepción de la excepción: las cornisas
    sostienen, las máquinas siguen francas. Tras el ORDEN el director las
    barre y la pregunta muere sola.
· INTEGRACIÓN QUE YA VALE LA PENA (la pregunta de Cesar, respondida con dos
  técnicas baratas): BAÑO TONAL (multiplicador 0.94/0.90/0.86 como color de
  fábrica — la ilustración de estudio se sienta en la cueva; el tinte de
  jugador multiplica encima) y SOMBRA DE CONTACTO (elipse suave en el primer
  suelo bajo los pies, sondeo de 8 celdas, encoge y desvanece con la altura:
  el truco de los levitadores — ata al muñeco al terreno sin física nueva).
  Lo caro (rim light, oclusión, normales) espera al pipeline 3D.

## Ronda 114 — TRES GOTAS FINAS (los últimos retoques del prólogo)

· Cambios mínimos de Cesar tras el playtest 113, los tres del oficio fino:
· DOS CHORRITOS DE ARCILLA (era "un chorrito del techo... tardo mucho en
  llenar el nuevo reservorio"): la gotera del derrumbe gotea ahora por DOS
  grietas (±3 del eje, bamboleo en espejo) — doble caudal hacia el
  montículo; tope e histéresis intactos, solo se espera la mitad.
· EL TOMA. DEL BEAT 3 SE RETIRÓ (R15): "en realidad no agarro nada, y opaca
  el VEN — pasa muy cerca". El vuelo del frasco cuenta la entrega solo. La
  palabra sobrevive donde SÍ es regalo: la primera gota del reorden.
· GOTAS DE AGUA SEPARADAS ("el de arcilla está perfecto; el de agua,
  intercalado o más interespaciado"): el refill del agua gotea a zancada 2
  (inlet, -2, +2, -4) — cuatro gotas sueltas como lluvia de tubo, no un
  bloque. La arcilla conserva su fila compacta por veredicto expreso.

## Ronda 115 — MUERE EL METRÓNOMO (y la herida cambia de lugar)

· Cuatro veredictos del playtest 114:
· EL TOMA. DESAPARECE DEL TODO: el de la R114 era el del beat 3; quedaba el
  del reorden (la primera gota) y era el que Cesar seguía viendo. Fuera
  también (R15) — el clank, la gota y el amanecer hablan solos.
· LA SOMBRA CON PRESENCIA: 0.55 de alfa sobre piedra oscura se comía la
  gamma. Ahora 0.85→0.25 y escala 1.35→0.75 con la altura.
· MUERE EL METRÓNOMO DEL LODO (Cesar: "secuencia perfecta formando una
  figura geométrica... tiene que ser disparejo"): el reventón del derrumbe
  usaba %3 (un peine) y la gotera R114 bamboleaba en espejo (simetría).
  Ahora TODO el lodo cosmético sale de un System.Random (semilla 913, ??=
  por hot-reload R74, jamás la sim): columnas al azar, cadencias que
  respiran (0.35x-1.85x), y cada grieta calla un 25% de las veces. Caudal
  promedio conservado: el llenado no empeora.
· LA HERIDA CAMBIA DE LUGAR (sección 2): el boquete del REPARA era la
  grieta del derrumbe reutilizada (388-392) — goteaba barro JUSTO sobre el
  plinto donde pones el piso. Ahora los albañiles del ORDEN sellan esa
  grieta y la bóveda cede sobre LA VIEJA CASCADA (326-335, el DOBLE de
  boca: 5→10): el barro cae donde vivía el agua — cada material muestra su
  conducta — y la reparación se ve importante. El goteo del techo también
  perdió el metrónomo.

## Ronda 116 — OCHO AFINACIONES (el prólogo a punto de caramelo)

· Ocho veredictos del playtest 115, todos quirúrgicos:
· 1) EL FRASCO YA NO ASPIRA ARQUITECTURA: el PisoEstructural (cejas de las
  repisas, pisos, techos) entra a la lista de "jamás al frasco" junto a la
  piedra — en EsAspirable Y en el filtro del TickSuck (misma verdad doble).
· 2) LA CARGA INICIAL SE VE: con el vidrio de 12 de ancho, la carga vieja
  del guion ni pintaba una fila. Mínimo estructural: DOS filas completas
  del interior (el silo sigue naciendo vacío — pasa 0 explícito).
· 3) LEYENDAS LEGIBLES EN CUALQUIER PARTE: un tercio más grandes, en
  negrita, y viajando con el muñeco sobre su propia placa casi negra (la
  opción limpia: sin regalarle a la UI una zona fija de pantalla).
· 4) EL CINCEL SE INVIERTE (testers de Cesar: "es más natural así"): clic
  IZQ construye, clic DER talla. Avisos de texto actualizados.
· 5) EL MATRAZ A ESCALA: CarryAnchor de la estampa = LA MANO REAL (0.42,
  -0.16) — el mismo punto que persigue el tarro visual (adiós al "círculo
  creciendo fuera de una botella chiquita") — y el tarro crece x1.9. El
  mapita (plano) crece a juego (0.11x0.16).
· 6) SOMBRA +20%: alfa 1.0 pegado al suelo, 1.45 de escala. "Con eso ya
  queda."
· 7) LA LLUVIA DEL REFILL: el agua elige columnas AL AZAR por todo el ancho
  del vidrio (rng 431, ??= R74) — cada pulso distinto, goteo de lluvia. La
  arcilla conserva su fila compacta (veredicto R114).
· 8) LA MUDANZA SE CIERRA SOLA: colocar el segundo reservorio ES el final —
  el director llama a Mudanza.ForzarSalida() (la puerta oficial, R37) y el
  Adiós se ve a color, sin el filtro morado que nadie sabía apagar.

## Ronda 117 — INVESTIGACIÓN: ANIMAR AL MUÑECO BARATO (2025–2026)

· Pedido de Cesar antes del flujo previsto: investigación profunda de
  herramientas/papers/modelos 2025–26 que abaraten la animación 2D del
  muñeco, con 14 dimensiones por candidato, licencias de código/pesos/
  outputs SEPARADAS, ranking de 5 pipelines completos contra la referencia
  (Mixamo + rig proxy + retarget + prerender), una prueba ejecutable antes
  del modelo 3D, y opinión sobre emotes sociales por acordes.
· Entregado: docs/INVESTIGACION_ANIMACION_2026.md (17 fichas, 5 pipelines,
  la prueba de una tarde, apuestas, emotes) + infografía hermana de las
  Cinco Eras (artifact "Cinco Rutas para Mover al Muñeco") + §2.6 del
  ROADMAP enlazando ambos.
· Hallazgos que cambian el plan: Wan-Animate-2 (Apache-2.0, 7 ago. 2026,
  tres semanas de vida, Lite en tiempo real) permite VALIDAR la animación
  con el PNG actual en una tarde; UniRig (MIT) reemplaza el rig proxy
  manual; HY-Motion 1.0 aporta gestos por texto bajo licencia Tencent
  (excluye UE/UK/Corea, exige declarar IA); Puppeteer (Apache) es la
  apuesta para gestos actuados sin Mixamo. Trampas de licencia anotadas:
  Animate-X (pesos sin licencia), MatAnyone (no comercial), Cascadeur
  gratis (no comercial, sin FBX).
· Veredicto de director: NO sustituir la referencia sino envolverla —
  ruta 1 esta semana para decidir, ruta 2 para producir, ruta 3 como red.
  Emotes: cosmética pura, jamás estado de la sim; "invocar al maestro" solo
  si el maestro APARECE (no si DA); primer corte 12 + 3 duetos + 1 ritual,
  después de las dos máquinas.
· Reset #10 del sandbox a mitad de la ronda (el commit cayó sobre el árbol
  viejo); rehecho sobre GitHub sin pérdida.

### 117b — EL ARNÉS QUEDA ARMADO (fuera del repo)

· Carpeta `C:\JuegosUnity\UnityAI_Test\Arnes_Animacion\` (LEEME.md dentro):
  `arnes.cmd`/`arnes.py` lanzan Wan Animate 2 por la API HTTP del ComfyUI
  local (sube imagen+video, cambia textos/semilla/largo, espera, baja el mp4
  a `salidas\<tag>\` con `informe.txt` y el prompt usado);
  `wan_animate_2_api.json` es la plantilla oficial exportada en formato API
  (51 nodos, subgrafos aplanados) con el muñeco ya descrito en el positivo.
· Entorno: ComfyUI 0.34.2 portable en D:, torch 2.13+cu130, cinco pesos del
  template (int8 convrot 15.9 GB, umt5 fp8, clip_vision_h, VAE bf16, LoRA
  lightx2v 4 pasos). MCP `comfyui` (artokun/comfyui-mcp vía npx) en
  `claude_desktop_config.json` junto a unity-mcp; conector oficial de Blender
  5.2 operativo (fue la mano que editó el config, midió RAM y reinició
  ComfyUI). Node 20.19 basta (avisos EBADENGINE; pide 22).
· Dos tropiezos con lección: (1) el nodo Load Video traía el nombre por
  defecto del template (`street_dance_drive.mp4`) → "entrada inválida";
  siempre elegir el archivo subido en el desplegable. (2) A mitad del
  muestreo, aimdo (el cargador dinámico de VRAM) murió con `error=1450`
  (recursos del sistema insuficientes) al streamear los 15.9 GB con ~7 GB de
  RAM libre → `run_arnes.bat` = arranque normal + `--disable-pinned-memory`.
· ComfyUI devuelve 403 a navegaciones iniciadas por extensiones
  (`Sec-Fetch-Site: cross-site`, su anti-CSRF): Claude en Chrome solo puede
  trabajar en una pestaña de ComfyUI si la URL la teclea Cesar una vez.
· Primera tanda: `voltereta01` (casifinal.png + voltereta.mp4, seed 777002,
  81 cuadros 482x854). El veredicto visual (¿aguanta la cabeza-caja, las
  costuras, la escala de juguete?) es la entrada de la ruta 1 del informe.

## Ronda 118 — LA ESTAMPA CAMINA (video → sprites → juego, en una noche)

· Veredicto de la ruta 1 (informe R117): el muñeco de remiendos ES ANIMABLE
  con el PNG actual. `voltereta01/02` (Wan Animate 2, modelo completo int8):
  la cabeza-caja gira en 3D coherente (tres caras, placas, musgo), costuras y
  parches se mantienen, la escala de juguete no se pierde, y el modelo
  INVENTA una espalda plausible (caja trasera, parche en la túnica) en la
  voltereta completa. Tiempos: 24 min con 3.6 GB de RAM libre, 14 min con
  10 GB (Unity cerrado). Cesar: "el video está hermoso".
· Tres defectos del primer intento y sus curas, ya en el arnés: (1) duraba
  1.3 s porque el template toma los primeros 81 cuadros TAL CUAL y el video
  iba a 62.5 fps — Wan trabaja a 16 fps → `preparar.py` re-muestrea a 16 fps
  (81 cuadros = 5 s de gesto) ANTES de subir; (2) el "aura" venía del halo del
  PNG `casifinal.png` → `muneco_plano.png`: el recorte transparente del repo
  sobre gris plano con aire arriba/abajo; (3) el personaje se salía por abajo
  al agacharse porque la referencia iba pegada a los bordes → mismo fix.
· Pedido de Cesar: SIN PARPADEO (los ojos son lámparas). En la primera tanda
  el modelo "cerró los ojos" al bajar la cabeza. Al positivo: "two round holes
  with fixed glowing amber lights inside, like lanterns: no eyelids, never
  blinking, always lit"; al negativo: blinking, eyelids, closing eyes, human
  eyes, eyeballs, glowing aura, halo.
· A3 EN MARCHA (el actor es Mixamo, no Cesar): 5 FBX de Mixamo (Walking in
  place, Picking Up, Standing Idle 03, Happy Idle, Standing Up) → Blender 5.2
  por su conector oficial: maniquí Y Bot gris claro sobre fondo gris 0.30,
  cámara 50 mm a 4 m, personaje girado 35° (3/4 mirando a la derecha, como la
  estampa), 480x848 a 30 fps, EEVEE 16 muestras, modificador CYCLES para los
  ciclos — 156 cuadros en 15 s. Cinco `pose_*.mp4` en `entradas\videos\`.
  Lección de Blender 5.x: `action.fcurves` ya no existe (layers → strips →
  channelbags → fcurves) y el video se activa con
  `image_settings.media_type = 'VIDEO'` antes de `file_format = 'FFMPEG'`.
· `postproceso.py`: mp4 → alfa (gris del borde + SOLO la región exterior
  conectada al borde, así las placas grises del cubo no se agujerean; borde
  suave en una franja de 2 px) → búsqueda del ciclo que cierra (periodo
  esperado 32/30·16 ≈ 17 cuadros) → caja común → hoja PNG + manifiesto JSON
  (cuadros, columnas, tamaño, fps, pivote = centro del personaje DE PIE en el
  cuadro 0, alturaPersonajePx). Probado sobre la voltereta: bordes limpios,
  brote y placas intactos.
· EN EL JUEGO: `HojaDeCuadros.cs` corta los sprites en runtime con
  Sprite.Create desde Resources/Personaje/Anim/<nombre>.png +
  <nombre>_manifiesto.json (sin Sprite Editor, sin Animator, sin .anim); la
  hoja se escala SOLA a la talla de la estampa (ppu = alturaPersonajePx /
  1.2 u), con compensación si Unity redujo la textura. ApprenticeController:
  con velocidad horizontal > 15% de moveSpeed y fuera del modo capataz, la
  estampa reproduce `caminar` al ritmo del paso (0.6–1.15×, 16 fps) y el bob
  sintético baja a un cuarto; quieto o subiendo vuelve la estampa base. Sin
  asset, nada cambia (retén honesto). El .meta de la hoja es el de la estampa
  con maxTextureSize 8192.
· Decisión de Cesar: NO probar el GGUF todavía — primero ver qué da el modelo
  completo con algo útil y dentro del juego; después comparar el cuantizado
  contra esa vara ("normalmente degradan mucho"). Idea suya para el reposo:
  Standing Idle 03 (juega con la botella entre las manos) — pero el frasco
  del juego es mágico y flota cerca, no va en la mano.

### 118b–e — EL PRIMER PLAYTEST DE LA CAMINATA (cinco vueltas en una noche)

· Cesar jugó y devolvió: "se mueve súper rápido para lo que se ve en sus
  pasos", "un borde a su alrededor", "parece que levita", "no sé si queremos
  que camine... que tenga física y camine lento hasta que con espaciador
  empiece a volar, sin perder lo que ya hay".
· R118b — DOS MODOS, UNA FÍSICA: a pie (gravedad 22 u/s², A/D a
  VelocidadPaso 1.1 u/s = la zancada real del ciclo; W o ESPACIO despega)
  y volando (todo lo de siempre: WASD a 4.8, hover; se aterriza bajando con
  S hasta tocar suelo, nunca por rozar). Arranca a pie cayendo al primer
  suelo. `SobreSuelo` sondea 0.08u bajo la caja (los subpasos de 0.06u
  dejan hueco); el avatar remoto deduce el modo. A pie: sin bob ni bandeo,
  la hoja corre al ritmo de la zancada (0.5–1.6×). Borde: descontaminación
  del contorno (color − (1−α)·fondo)/α en la franja + recorte del velo.
· R118c — "camina un poco por el aire" → `AsentarEnSuelo` (baja en pasos de
  0.005 hasta tocar) + el visual baja 0.04u a pie (el margen de bob de la
  caja). "La cabeza se achica al empezar a caminar" → el video TRAE el
  arranque: Wan empieza siempre en la pose de la referencia y en 1–5 gira
  al 3/4 de marcha; la hoja lleva `intro` y `TickCaminar` lo toca al
  empezar y al revés al frenar. Hoja sin compresión (DXT5 ensuciaba el
  alfa) y erosión de 1 px.
· R118d — "cuando termina de caminar la cabeza popea... se ve mejor
  caminando que de frente" → la pose quieta ES el cuadro 0 de la hoja (la
  estampa re-dibujada por el modelo): quieto↔caminar continuo por
  construcción; el PNG original queda de retén. Arranque a 7 cuadros al 70%.
· R118e — "entre las piernas se ve fondo blanco" → los huecos CERRADOS con
  color de fondo (dist. mediana < 10, área ≥ 20) también se quitan; erosión
  2 px. "Al parar sigue unos frames" → frenado corto: solo los últimos 4
  cuadros del arranque a ritmo pleno (~0.25 s). Veredicto: "se ve mejor,
  micro detalles... está wow".
· Pendientes con nombre: (1) el cuadro 0 del video tiene un pie a medio
  paso → REFERENCIA CANÓNICA: sacar del reposo generado el cuadro limpio
  (`preparar.cmd cuadro`) y regenerar caminar/recoger desde esa pose;
  (2) RECORDAR: si quedan micro-filos, generar sobre fondo más oscuro (gris
  0.30 como los videos de pose de Blender) deja el filo del color de la
  cueva; (3) los pies "poco firmes" son el mundo plano bajo un muñeco con
  volumen: se resuelve con la capa visual de las rocas, no en el personaje;
  (4) 16 fps sin interpolar ES la cadencia a pasos de la dirección de arte;
  bajar a 12 es un número.
· Lección de herramienta: la inyección de teclado por MCP solo llega al
  juego si el Editor tiene el foco de Windows (Input System:
  PointersAndKeyboardsRespectGameViewFocus + ResetAndDisableNonBackground);
  cuando Cesar escribe en Claude, el foco se va. Para verificar con él
  presente: que juegue él y la telemetría grabe.

### 118f — REFERENCIA CANÓNICA, REPOSO Y RECOGER (la noche cierra con tres hojas)

· Tiempos reales por tanda con Unity cerrado y 17 GB de RAM libres: 3–5 min
  (el modelo cabe en RAM y ya no toca disco). Con Blender renderizando o con
  su escena cargada en la GPU: 10–30 min. `blender_pose.py` queda en la
  carpeta del arnés; tras renderizar, Blender vacía la escena.
· `reposo01` (Standing Idle 03, desde el arte): reposo GESTICULANTE (manos
  jugando), pies plantados, pero el cuadro 0 sale con la mano alzada porque
  Wan MEZCLA la referencia con la primera pose del video. Lección: el cuadro
  0 = referencia × pose inicial. Para una pose canónica hace falta un video
  cuyo primer cuadro sea neutro.
· `reposo02` (Happy Idle, desde el arte): reposo CALMADO (brazos abajo,
  respira, la cabeza gira suave), fidelidad de primera generación. Su cuadro
  0 es la REFERENCIA CANÓNICA v2 (`muneco_canon.png`): pies plantados, brazos
  abajo, 3/4 del arte. Ciclo 17..63 (47 cuadros, distancia 212: cierra).
· `caminar02` se generó desde una canónica de SEGUNDA generación (cuadro 22
  de reposo01) y se notó la deriva: parche cambiado de lado, menos placas,
  ojos más grandes. `caminar03` desde la canónica v2: fiel y continuo (cuadro
  0 = canon; ciclo 63..79, distancia 107, la mejor hasta ahora).
· `recoger01` (Picking Up de frente, azimut 35): al inclinarse la CAJA viene
  hacia cámara y tapa el cuerpo — ilegible. `recoger02` (azimut 75, casi de
  perfil, ×1.85 de velocidad para meter los 9.6 s del FBX en 5 s): la caja se
  inclina al suelo y el cuerpo se agacha; al levantarse queda con las manos
  juntas (sosteniendo algo). Regla de dirección: gestos que se inclinan hacia
  cámara van de perfil; los de pie, a 35°.
· Juego: `HojaDeCuadros` gana `Base` (cuadro canónico), `PingPong`,
  `CuadroDelCiclo(t)`; el aprendiz carga `reposo` (ciclo a pie, cuadro base
  volando), `caminar` (arranque 5 + ciclo 17) y `recoger` (una pasada; tecla
  G a pie y quieto, para verlo; API `ReproducirGesto`). Arnés: `--velocidad`,
  `--base`, `--pingpong`, `--tolerancia`, `preparar cuadro`.
· Pendiente de veredicto de Cesar (pidió parar aquí y mirar): calidad de las
  tres hojas en el juego, si el reposo calmado es EL reposo o si el
  gesticulante (reposo01) entra como variación, y el gesto de recoger.

## Ronda 119 — EL BALANCE Y LAS DOS PREGUNTAS (documentos de decisión)

· Veredicto de Cesar sobre la R118: "las animaciones son hermosas... a la altura
  de un juego AAA... parece hecho a posta, algo muy real dentro de un mundo de
  fantasía; el contraste se ve intencional". Y lo negativo con la misma
  claridad: ES INCONSISTENTE (popeos, dedos que aparecen, pies que cambian,
  bordes que quizá pidan mano) y cada tirada es un dibujante nuevo — "lo que
  iba a ser automatizado se convierte en trabajo manual de visión fina".
  Satisfecho para demo interna; para demo gratuita pide ~40% menos
  inconsistencia; duda de que se pueda.
· Respuesta de dirección en `docs/PLAN_ANIMACION_R119.md`: la reformulación
  (fábrica de candidatos para una biblioteca FINITA: la inconsistencia es costo
  de curaduría, no bug de runtime), expectativa honesta por camino (ruta 1:
  +20-25 de esos 40 con QC automático, cirugía de cuadros, N semillas, fondo
  oscuro y manoplas al negativo; ruta 4: nicho de micro-loops, no biblioteca;
  ruta 2/3D: consistencia garantizada + proyección del arte original como
  textura + TRELLIS.2 como puerta barata; no pagar modelos), y el plan del
  ATRIL de emotes en dos capas (T = abanico de prueba de toda hoja en
  Resources/Personaje/Anim, hoy; emotes sociales de verdad tras las dos
  máquinas). Los clips ganadores de la ruta 1 quedan como ESPECIFICACIÓN de
  estilo de movimiento (extraíbles a esqueleto 2D para retarget futuro).
· `docs/DISENO_MOVIMIENTO.md` (decisión ABIERTA): solo-volar vs
  caminar+volar vs solo-caminar, con referencias (Noita como hermana
  falling-sand: levitación que se recarga en suelo; Terraria: vuelo con
  descenso suave; Starbound, Ori, HK, ONI), riesgos por opción, la fricción
  observada por Cesar (los que vuelan trabajan desde un poco más lejos de lo
  que necesitan) y la mitigación transversal propuesta: EL ANCLA DE TRABAJO
  (verter/aspirar amortigua y clava al personaje). Recomendación: instrumentar
  la B (que contiene a las otras dos) y que la telemetría del playtest elija.
· Reset #12 del sandbox a mitad de la ronda; recuperado de origin/main +
  tar del disco de Cesar (protocolo de la regla 6), cero pérdida.
· Cesar reinició Claude: el MCP de ComfyUI quedó VIVO (comfyui__* en la lista
  de herramientas; sin usar aún — "primero vamos a mirar bien el panorama").

## Ronda 120 — RETOQUES, EL ATRIL POR ACORDES Y EL ANCLA (Cesar salió hora y media)

· Decisión de dirección tomada por Cesar tras la R119: el pipeline generativo
  queda como prototipador de acting y referencia de estilo; la producción
  irá a un personaje 3D canónico (image-to-3D + limpieza mínima + rig
  determinista + render con normales para luz dinámica de la cueva). Mi
  evaluación: TRELLIS.2 (MIT) primero, Tripo Pro (quads + auto-rig, 19.90/mes)
  para comparar, Hunyuan3D descartado como base por licencia; la CABEZA NO SE
  GENERA (es un cubo con el arte proyectado), solo el cuerpo chibi; Mixamo
  auto-rigger como camino de rig; el títere 2D queda como plan C con
  disparadores. Mientras tanto, esta ronda cierra lo que ya vive en el juego.
· "Se traba un poquito a los pocos frames de iniciar": el arranque (0..4)
  saltaba al ciclo elegido por mejor cierre (63..79) — distancia 1587 contra
  331 entre cuadros consecutivos. `postproceso.py` ahora exige ciclo CONTIGUO
  al arranque (busca el mejor cierre empezando en [intro, intro+6] y estira
  el arranque hasta ahí: caminar = arranque 0..10 + ciclo 11..27, cierre 293)
  y `TickCaminar` ya no suma un paso doble al cruzar al ciclo. Frenado: los
  primeros 6 cuadros del arranque al revés (el giro de vuelta a la base).
· ATRIL DE EMOTES POR ACORDES (`AtrilDeEmotes.cs`), como pidió Cesar: nada
  radial; dígito 1-4 abre un grupo (lista discreta arriba a la izquierda con
  una barrita que se agota en 2.6 s), segundo dígito dispara; hasta 16 gestos
  con dos teclas. Los grupos se arman SOLOS con los manifiestos de
  Resources/Personaje/Anim (campo `etiqueta` opcional); los ciclos se tocan
  ~2 vueltas como emote; solo a pie, respeta las guardas de la regla 12.
· EL ANCLA DE TRABAJO (DISENO_MOVIMIENTO §0): Flask avisa cada frame si se
  vierte/aspira; el aprendiz frena a 40 u/s² y el input pesa 0.35 -- clavado
  en el aire o en el suelo mientras trabaja; cayendo no aplica.
· TELEMETRÍA DE MOVIMIENTO (`TelemetriaMovimiento`): % a pie vs volando,
  verter/aspirar desde suelo o aire, despegues, aterrizajes, gestos; línea
  "[Telemetría movimiento]" cada 2 min y al cerrar. Son los tres números que
  decidirán la mecánica (opción B instrumentada, la que Cesar aprobó).
· Nuevos FBX de Cesar → videos de pose (Falling 35°, Jumping 35°, Walking Turn
  180 35°, Zombie Idle 35°, Getting Up 75°) → cinco tandas desde la canónica
  v2: flotar (Falling ×0.75), saltar, girar (×0.5, se toca a 32 fps), zombi,
  despertar (Getting Up: el prólogo EMPIEZA despertando). `postproceso.py`
  recorta la cola estática de los gestos de una pasada y acepta `--etiqueta`.
· Sandbox: reset #13 (repo a R85 otra vez); origin/main ya tenía la R119, así
  que recuperar fue un reset --hard. El scratch del arnés sobrevivió.
· Bug del arnés cazado en la hornada: cinco tandas en cola subían TODAS su
  video de pose como `pose_16fps.mp4` a ComfyUI/input y la última pisaba a las
  anteriores — `saltar01` salió con el video de despertar (flotar se salvó por
  ejecutarse antes de las otras subidas). `arnes.py` ahora sube
  `pose_<tag>_16fps.mp4`; las cuatro tandas mezcladas fueron a `_to_delete` y
  se relanzaron. Con tres instancias de Unity abiertas y 0.9 GB de RAM libre,
  cada tanda tarda ~16 min (contra 3–5 con la RAM libre): la regla "Unity
  cerrado durante las tandas" queda confirmada con número.
· Las cuatro capturas `cap118c_*.png` que se colaron en la raíz del repo en el
  push de la R118 se retiran (a `_to_delete/`).

## Ronda 121 — LAS TRES OPCIONES DE MOVIMIENTO, JUGABLES (F6)

· Cesar dio por buena la demo interna de animación ("basta para una demo
  interna, suficiente para ir ahora por el fondo y los tiles") y pidió lo que
  la R120 dejó a medias: que las tres opciones del DISENO_MOVIMIENTO §4 se
  puedan PROBAR, no solo leer.
· `ApprenticeController.Modo` (PlayerPrefs, carga perezosa por la regla 56):
  A solo vuelo (siempre `_volando`, nunca aterriza), B pies y vuelo (R118b
  intacto), C solo pies (gravedad; Espacio/W = SALTO con impulso 8.2 ≈ 1.5u y
  gravedad 2.2x al soltar = brinco corto; Shift = correr a 2.1 u/s, la hoja
  acelera al ritmo hasta 1.6x; la V no existe). F6 rota los tres; el atril
  avisa el modo al entrar y al cambiar. Una sola física podada, no tres.
· `TelemetriaMovimiento` incluye el modo y los saltos, y al cambiar de modo
  cierra un BLOQUE (imprime y arranca de cero): los tres números salen
  separados por opción. Protocolo sugerido: 10 min por modo por tester, orden
  distinto, comparar bloques y sensaciones.
· Reset #14 del sandbox al empezar; origin/main tenía la R120 (Cesar la
  subió), recuperación limpia.

### 121b — EL PAQUETE DE SENSACIÓN DE PLATAFORMAS (para que la comparación sea justa)

· Cesar: "el último [modo C] se siente muy tosco; referencias tipo Super Mario
  de SNES o mejores". Investigado (GMTK Platformer Toolkit, el catálogo de
  davetech de 18 plataformeros, Celeste) y aplicado en `HandleMovement`, a pie
  en B y C: aceleración 30 u/s² y frenado 45 (tope en ~0.07 s, el pie se
  planta), control aéreo 75%; paso 1.5 u/s (era 1.1) y Shift corre a 2.6 (la
  hoja acelera hasta 1.9x); salto de 2.2u (22 celdas, 1.8 alturas del muñeco)
  con 0.42 s al ápice (Gravedad 25, impulso 10.5), caída 1.7x (los que caen
  más rápido de lo que suben son el 40% del catálogo, SMW/Celeste entre
  ellos), corte al soltar al 45% (Celeste; brinco mínimo ~0.5u), ápice suave
  (gravedad 55% con |vy| < 1.2), coyote 0.12 s, buffer de salto 0.12 s,
  CaidaMax 12, y squash de aterrizaje proporcional a la caída (se aplasta
  hasta 18% y recupera en ~0.12 s, pies fijos). Sin tocar el vuelo (4.8).

## Ronda 122 — EL QUE TALLA BAJO SUS PIES (desenterrar a pie)

· Playtest de Cesar en modo C: talló con el cincel el piso donde irían los
  contenedores, cayó "del mapa" y no pudo subir. Sonda con Unity abierto:
  pos y=0.00 (el clamp de WorldMinY), vy=-12 permanente, caja DENTRO de sólido,
  EnSuelo=false. Mecanismo: el hueco tallado era más chico que el cuerpo; con
  la caja dentro de sólido la colisión se suspende (diseño de la R66 para
  "salir nadando" volando) y a pie la gravedad lo hundió hasta el fondo del
  mundo, donde la sonda de suelo no encuentra nada debajo y el salto nunca se
  habilita.
· Dos fixes en `ApprenticeController`: (1) `Desenterrar`: a pie, si la caja
  arranca dentro de sólido, se busca la primera posición libre hacia arriba
  (media celda por paso hasta 3u, también a los lados) y después toda la
  columna hasta WorldMaxY — se planta ahí con vy=0; el vuelo sigue nadando
  como siempre. (2) `SobreSuelo`: el fondo del mundo cuenta como suelo.
  Verificado en vivo: enterrado 0.5u → en superficie al frame siguiente;
  hundido a y=0.2 bajo 14u de roca → en superficie al frame siguiente,
  EnSuelo=true. Regla de diseño que queda: a pie, tallar bajo tus pies te deja
  sobre el borde que sobrevive si el hueco no te cabe; si te cabe, caes y
  subes saltando (2.2u) o tallando escalones. Regla 38 intacta: hay salida.

## Ronda 123 — ¿2D A FONDO O 2.5D? (evaluación, sin código)

· Pedido de Cesar: antes de ir por el fondo y los tiles, evaluar en serio si
  seguir profundizando el 2D o abrir la puerta al 2.5D (A mantener 2D · B falso
  2.5D agresivo · C 2.5D real), con 14 criterios, tres preguntas clave y una
  crítica a la idea del fondo como civilización evolutiva por hitos. Sin
  implementar nada.
· Entregado `docs/EVALUACION_2D_VS_2_5D.md`: recomendación (A + la mitad
  barata de B: 2D Renderer + Light2D con causa + normales + tres planos; C
  archivada), tabla A/B/C (116 / 114 / 65 sobre 140), los síntomas que se
  sentían son herramientas 2D estándar sin usar (el renderer activo es el
  Universal 3D — no hay Light2D —, ni SpriteMask ni SortingGroup, un solo plano
  de parallax al 8 %, sin normales, post en IMGUI), el mayor riesgo (el pivote
  silencioso B→C), la mayor oportunidad (la luz con CAUSA: Fire real de la
  sim), experimento de una tarde «la ventana iluminada» con criterios de
  muerte, lo que NO se decide aún (C, acabado pixel, contenido del fondo,
  render de 1 téxel/celda) y la información que falta (prueba de Marching
  Squares de Cesar, capturas, mín-spec, telemetría de movimiento, encargos de
  ORO, réplicas co-op).
· Fondo evolutivo: se vuelve DISEÑO si los hitos son los materiales de ORO
  entregados por el vano (GDD §0 nodos + caravanas) y con la regla del espejo
  retrasado (nunca por delante de tu hito, para no matar R60 ni el Trueque);
  homínidos coexistiendo solo como siluetas, jamás en fichas. Notas: narrativa
  8 · visual 7 · progreso 6 · diferenciación 7 · premios 6 · coste/riesgo 7.
· Infografía interactiva «La Ventana Iluminada» (capas, cámara, terreno con
  Marching Squares calculado en vivo, sándwich, ventana con parallax e hitos
  0–4 día/noche): https://claude.ai/code/artifact/1cd1c2ec-5cb4-453b-a2b7-577b67172dbe · fuente en `docs/ref/infografia_2d_vs_2_5d.html`.
  Enlace en `ROADMAP.md` §2.7.
· Coherencia: no toca GDD §0 ni DIRECCION_DE_ARTE; anota que «LEVITA, no
  camina» sigue sellado y que la investigación F6 A/B/C podría pedir una
  enmienda explícita, que este documento no hace.

## Ronda 124 — LA PIEL DE ROCA (prueba visual de terreno con marching squares)

· Pedido de Cesar: probar una piel/contorno orgánico tipo Marching Squares
  SOLO para el sólido estático/destructible, sin tocar la sim ni la materia
  granular, para ver si el contraste roca lisa / partículas cuadradas funciona
  o canta. Referencia conceptual: Sebastian Lague (autómata + marching
  squares + malla de contorno), adaptada a un terreno vivo.
· `Game/PielDeRoca.cs` (nuevo): mallas por chunk debajo de la sim (−6);
  campo = solidez media de 4 celdas por esquina, umbral 0.49 (muros de una
  celda enteros, chaflán/filete de media celda, silueta a ≤½ celda de la
  colisión), temblor determinista en aristas, guijarros para celdas aisladas.
  Cuatro capas ordenadas por Z: sombra (canto desplazado + halo proyectado al
  telón), relleno texturizado con MASA INTERNA por distancia al aire, bandas
  por orientación (SUELO filo claro / PARED oclusión / TECHO sombra) + línea
  de tinta (juntas finas contra otros sólidos), decoración procedural
  (estalactitas con gota, raíces, musgo PÁTINA, grietas). Textura de roca
  procedural 256² (grano, manchas, estratos suaves, grietas Worley
  enmascaradas); cero assets. Actualización por hash de chunk + 8 vecinos;
  medido: 0.035 ms/chunk, 30 ms el mundo, 0.01 ms/frame en reposo, 140 k
  vértices (celdas macizas fundidas por fila).
· `SimRenderer`: `OcultarRoca` (Stone a alfa 0 mientras la piel está activa),
  `MarcarTodoSucio()`, `TinteActual`. `ApprenticeController` crea la piel con
  el jugador local. F7 rota 5 niveles (0 grilla · 1 contorno · 2 bandas · 3
  profundidad · 4 decorada; PlayerPrefs, el atril avisa). Ctrl+F7 talla LA
  CUEVA DE MUESTRA alrededor del jugador (solo anfitrión).
· Iteración con ojo (capturas en Temp/cap124): v1 estratos fuertes → leía
  como madera/lodo; v2 placas Worley continuas → adoquín; v3 sin grietas →
  lodo plano; v4/v5 grietas esporádicas + roca más clara + filo de suelo +
  canto visible + halo de sombra → se queda. Comparación antes/después en
  `docs/ref/piel_de_roca_R124.jpg`. Nota completa (qué hice, decisión visual,
  arte final, limitaciones, riesgos, siguiente paso): `docs/PRUEBA_PIEL_DE_ROCA.md`.
· Hallazgo lateral: la sim de la escena de laboratorio arranca en pausa
  (`Paused=true`) hasta que el juego la suelta; la textura de la sim no
  repinta hasta el primer tick (la piel no depende del tick).

## Ronda 125 — DIRECCIÓN V2: TERRENO, LUZ Y ESPACIO (solo docs)

· Cierre de jornada pedido por Cesar: documentar las decisiones tomadas tras
  la piel de roca sin implementar nada. Nuevo `docs/DIRECCION_V2_TERRENO_LUZ_ESPACIO.md`
  con tres etiquetas estrictas: [DECIDIDO] 2D bien explotado como
  arquitectura, sim/precisión/cámara lateral intocables, presentación final
  con iluminación 2D y profundidad pintada, 2.5D real archivado, piel de roca
  como dirección del terreno natural, gramática visual única (7 principios);
  [HIPÓTESIS] excavar como verbo de espacio (descubrimiento, estabilización,
  crecimiento del laboratorio, con guardas: jamás rinde material, no compite
  con verter), prólogo contenido con derrumbe de apertura que revela el vano
  y el fondo evolutivo, fondo evolutivo en el Nivel 0; [APLAZADO] locomoción
  (con la lista de lo que debe existir antes de decidir; «LEVITA» del GDD §0
  intacto), acabado pixel, nivel base de la piel, contenido del fondo,
  cambio al 2D Renderer, prólogo contenido. §3 fija herramientas,
  parámetros de partida, límites y criterios de muerte de la iluminación
  (luz global 0.55–0.70, ≤8 luces con causa, materia con ≤15 % de luz,
  sombras cortas solo de muñeco y máquinas, bloom umbral alto, ≤1 ms).
· `EVALUACION_2D_VS_2_5D.md` (R123) queda como archivo histórico sin tocar.
  Enlaces añadidos en `ROADMAP.md` §2.7 y una nota "ver también" al inicio de
  `DIRECCION_DE_ARTE.md` (que sigue siendo el normativo de personaje, paleta,
  talla y cámara).
· Retoque de la R124 incluido en esta subida: rótulo fijo del nivel de la
  piel abajo a la derecha, cueva de muestra movida a Ctrl+Shift+F7 (Ctrl+F7
  encerró a Cesar en un cuadro), repintado inmediato de la sim al alternar
  (`SimRenderer.RepintarAhora`).
· Reset #15 del sandbox durante esta ronda (volvió a la R85); recuperado con
  `origin/main` (Cesar ya había subido la R124) + los dos .cs del retoque
  desde el disco de Cesar.

## Ronda 126 — EL PLAN DE LA GALERÍA DE ESTILO (planificación, sin código)

· Idea de Cesar: mientras él produce el arte, crear un sandbox de imagen
  fuera del juego («el prólogo no es el lugar óptimo para tomar decisiones de
  estilo») con acceso dev para colocar fogatas, máquinas y elementos.
· `docs/PLAN_GALERIA_DE_ESTILO.md` + esquema `docs/ref/esquema_galeria.png`
  (9 áreas conectadas por corredores). Ubicación técnica: flag `ModoGaleria`
  (patrón Fundación, regla 59), misma escena/sim/piel/muñeco, plano propio,
  catálogo IMGUI hermano de DevPalette. Metas: 1 EL LIENZO · 2 EL PROBADOR
  (hot-load de texturas) · 3 EL BANCO DE LUZ. Regla: nada de la Galería migra
  al juego sin su propia ronda.

## Ronda 127 — LA GALERÍA DE ESTILO, META 1: EL LIENZO (verificada en vivo)

· Cesar aprobó el esquema de la R126 y pidió además agregar/quitar/duplicar
  objetos sin ensuciar pantalla. Implementado TODO el lienzo:
  `SimLevelBuilder.BuildGaleria` (9 áreas conectadas: cueva íntima con
  fogata, patio de fuego con hoyo, nave de 105 celdas con pilares y siluetas
  de escala, pared de juntas con 6 sólidos fabricados contra la roca, pozo
  vertical con repisas, poza con bolsa de goteo real, vano, pendientes con
  escalera de 1 celda/rampa/repisa fina/guijarro/túnel bajo, terrario con 8
  cubetas de materia), `ModoGaleria` (patrón Fundación, reseteado en los 6
  caminos de la regla 59), botón tenue «galería de estilo» en el título,
  `GaleriaCurador` (G abre/cierra — cerrado NO se ve nada; clic coloca del
  catálogo fuego/materia/roca, clic der. quita, C+clic copia una ESTAMPA
  12x12 de materia y el clic la duplica, -/+ radio, Ctrl+1..9 teletransporta
  con la cámara plantada, F10 = RONDA DE CAPTURAS: 9 PNG con fecha en
  Galeria/capturas/), telón en modo ruina sin herrajes (WorkshopBackdrop),
  asmdef ahora referencia URP runtime (lo pedía el curador y lo pedirá la
  luz de la Meta 3). DayCycle: HUD silenciado en galería y sin arco persiste.
  Verificado en vivo: mundo correcto, 2 rondas de capturas de las 9 áreas.
· INCIDENTE Y LECCIÓN (reglas 26/32): el sandbox amaneció en una copia vieja
  (R85) sin avisar; el primer despliegue de esta ronda PISÓ en el disco de
  Cesar versiones más nuevas de 6 archivos, y el HISTORIAL de la R126 se
  subió truncado (R86–125 perdidas en ese commit). Recuperado todo de git
  (HEAD 9ad29e8 + HISTORIAL de la R125) y reaplicada la galería encima; el
  HISTORIAL completo viaja en esta ronda. Guardia nueva: ANTES de cualquier
  despliegue, `git fetch` y comparar el HEAD del sandbox con el del disco.
· Segunda lección operativa: el editor de Cesar tiene el auto-refresh
  APAGADO — un `AssetDatabase.Refresh()` no recompila; hay que llamar
  además `CompilationPipeline.RequestScriptCompilation()` y verificar que el
  tipo nuevo EXISTE antes de entrar a Play (dos rondas de capturas salieron
  del assembly viejo por esto, y la cámara suavizada además viajaba en los
  teletransportes: ahora se planta de golpe).
· Pendiente de la Galería: Meta 1b (máquinas reales en el catálogo), Meta 2
  (hot-load de texturas de Cesar), Meta 3 (banco de luz). Los marcos dorados
  que flotan en la nave son cuadros del telón de ruina: se revisarán con la
  Meta 2.

### 127b — COMPONER, NO EXPLORAR (dirección de Cesar, aplicada en la misma ronda)

· Cesar: «mínimas cosas en la galería; más que un lugar que explorar es un
  lugar para componer; fortalece el inicio humilde (las fogatas, que se
  puedan quitar y poner) y un menú para ir agregando el resto de las cosas
  que hicimos antes».
· Decluttered: fuera las siluetas de escala de la nave y las brasas
  pre-puestas del patio; queda UNA fogata de bienvenida en la cueva íntima
  (es materia: el clic derecho del curador la quita, como todo).
· MÁQUINAS EN EL CATÁLOGO (Meta 1b): crisol, alambique, prensa y banco de
  chispa se colocan con un clic. El truco honra la regla 36 (jamás re-Init):
  Init en su ancla clásica + `Reposicionar` al cursor (las estaciones ya son
  IMovible), con FOTO previa de la huella clásica para restaurar el hueco
  fantasma que el primer Reposicionar talla en la roca. «Quitar máquina» la
  muda a LA BODEGA (sala 30..300 × 262..284, fuera de las áreas): no hay
  camino de baja en los registros del taller, así que no se destruye — se
  archiva, y se puede recuperar. Verificado en vivo: crisol y alambique
  colocados en la nave con su brasero y su tallado correctos.
· HOT-LOAD DE TEXTURAS (primer peldaño de la Meta 2): con el curador
  abierto, R lee `Galeria/roca_superficie.png` (teselable, cualquier tamaño)
  y `PielDeRoca.CargarTexturaDeRoca` recambia la piel ENTERA al instante,
  sin recompilar ni reconstruir mallas (solo cambia el MaterialPropertyBlock
  del relleno). Verificado en vivo con una textura de prueba ocre: el mundo
  entero cambió de piel con una tecla; el estado se lee en la ventana del
  curador. Una textura de prueba quedó en Galeria/ (sustitúyela por la
  tuya: mismo nombre, R, y listo).
· HITO DE PARADA para las pruebas de Cesar: título → «galería de estilo» →
  G (curador), clic coloca / clic der. quita / C+clic estampa / -+ radio ·
  máquinas del grupo MÁQUINAS · Ctrl+1..9 áreas · F7 piel · F10 capturas ·
  R textura.

## Ronda 128 — LA GALERÍA AFINADA CON EL PLAYTEST DE CESAR (verificada en vivo)

· Siete observaciones de Cesar tras una sesión larga en la Galería, todas
  aplicadas o respondidas:
  1) Corredores ANCHOS: todo pasaje a 16 celdas de luz (el muñeco mide 12);
     el túnel bajo de pendientes a 6.
  2) El PISO sale del ciclo de la C (Cesar: "el segundo cincel incomoda"):
     C alterna solo frasco ↔ cincel piedra; la X sigue viva como camino
     explícito (regla 15: aparcado con `CicloIncluyePiso=false`).
  3) BOTONES DEL CINCEL AL ESTÁNDAR del género (Minecraft/Terraria):
     clic IZQUIERDO talla, derecho construye; avisos actualizados.
  4) Fogatas PERSISTENTES: el curador reaviva sus fogatas cada 5 s (la brasa
     real se consumía a ceniza en un minuto — por eso Cesar solo encontró
     cenizas); si la quitan a mano (área sin brasa NI ceniza) se olvida.
  5) Curador: tecla F8 (la G era del termómetro y del gesto — doble
     activación reportada); con el curador abierto FRASCO Y CINCEL CEDEN los
     clics (guarda estilo DevPalette en Flask/Cincel — era el choque del
     C+clic); la ESTAMPA ahora es por radio (-/+) y SOLO materia suelta (ni
     roca ni fabricados: copiaba "mucho de todo"); ANILLO de radio visible
     en el cursor (no había evidencia del -/+); la R del hot-load funciona
     con el curador cerrado y AVISA su resultado 4 s en pantalla.
  6) Sin muebles fantasma: baldas/anclajes del plano clásico ya no nacen en
     la Galería (venían de Mudanza.Init con posiciones del cuarto íntimo);
     al catálogo entran PLACA ÍGNEA, PLACA GÉLIDA, CAÑO DE AGUA (la
     naciente que faltaba) y BALDA, todos colocados directo al cursor.
  7) Terrario solo con VOCABULARIO nombrado (fuera el innominado "???";
     entra la brasa).
· Verificado en vivo: placa/caño/balda colocados por catálogo; 1 sola balda
  en escena (la del curador). Diseño pendiente de decisión (propuestas con
  costo dadas a Cesar): el personaje nítido DELANTE del agua (propuesta:
  segunda textura solo-líquidos semitransparente delante del muñeco, patrón
  _frontTexture, ~1 día) y el serrucho de las pendientes contra líquidos
  (propuesta: banda de orilla/menisco sobre el contorno mojado, ~1 tarde;
  el escalón interno es la materia siendo materia y se queda).
· Reset #16 del sandbox al empezar (volvió a R85); recuperado de origin
  (Cesar ya había subido la R127) sin pérdidas — la guardia nueva del fetch
  funcionó.


## Ronda 129 — EL VELO DE LÍQUIDOS Y EL MENISCO (las dos propuestas aprobadas)

· Los dos pendientes de diseño de la R128, aprobados por Cesar ("los dos
  cambios que propones aprobados. Vamos a probarlo"):
  1) VELO DE LÍQUIDOS (SimRenderer): tercera textura, siempre activa, que
     repinta SOLO los líquidos por delante del aprendiz (orden 52: sobre
     Personaje=50, bajo ArquitecturaFrente=55 y CarryEnMano=60) con el
     mismo color que ya calculó ComputeCellColor y alfa 115 (~45%): el
     muñeco metido en la poza queda TEÑIDO por el agua en vez de flotar
     nítido delante de ella. A diferencia del labio frontal (tumbado en
     R67 por blocky) aquí no se inventa ningún borde: es la misma agua de
     la sim dos veces, el pixel delantero es idéntico al trasero. Se llena
     en el mismo barrido por chunks y sube a GPU al mismo ritmo; acompaña
     el tinte del quad (vista de plano de la mudanza incluida).
  2) EL MENISCO (PielDeRoca): donde el contorno de la piel tiene líquido
     media celda hacia afuera, una hebra clara del color del líquido
     (Lerp hacia blanco 0.55, alfa 0.9) a caballo de la línea de tinta
     (0.14 C fuera / 0.08 C dentro) — el agua "moja" la roca y el
     serrucho de las pendientes contra líquidos se lee como orilla.
     Ensucia por un SEGUNDO hash por chunk (_hashOrilla: solo líquido
     PEGADO a roca, el material entra al hash porque da el color) que se
     revisa SOLO en la ronda lenta — una poza con corriente no
     reconstruye malla cada tick; el menisco puede llegar con ~1 s de
     retraso y ese es exactamente el precio pactado.
· BONUS descubierto al verificar en vivo: LA POZA SE VACIABA — el corredor
  pozo↔poza (y68-84) estaba a nivel del agua (superficie y81) y en ~1
  minuto de sim las 1972 celdas drenaban por el pozo hasta el piso del
  terrario (por eso nadie lo vio: los playtests anteriores miraban la poza
  recién construida). Piso del corredor a y82 (una celda sobre la
  superficie, 16 de luz intactas): verificado estanca tras 45 s de sim.
· Verificado en vivo con capturas: muñeco pecho adentro VELADO por el agua
  (cabeza cálida fuera, cuerpo azulado dentro) y menisco claro en la
  orilla. De paso, el editor volvió a quedar EN PAUSA tras el Play por MCP
  (la guardia de CLAUDE.md §5 lo cazó: chunks con ever=False delataron el
  render congelado).
· Aparte (fuera del repo, en Notas\): la ruta de comunidad reescrita v2
  con la capa personal de Cesar (anonimato total, dos embudos, la
  compulsión educadora productizada, contra-anclas del ruido de precio).

## Ronda 130 — EL LABORATORIO DE LEYES: costuras y handoff (Fable, sesión nocturna 2026-09-03)

· Encargo de Cesar: «encontrar el juego dentro de la simulación» — una SEGUNDA galería
  (sandbox de investigación, sin narrativa) para probar la hipótesis de que el conocimiento
  sustituye al trabajo manual (canales, pozas, sedimentación, evaporación, vapor conducido,
  condensación, filtración, automatización emergente), con panel de parámetros en vivo,
  presets, tiempo acelerado, stress tests, análisis multiplayer e informe final. A mitad de
  la noche Cesar cambió la estrategia por presupuesto: Fable = arquitecto, Opus 5 =
  implementador. Todo lo de esta ronda vive en `docs/LAB/` (CHECKPOINT, DISENO_LABORATORIO,
  HANDOFF_OPUS, MULTIPLAYER, mapa/ con 9 mapas del motor) y en `Laboratorio/` (capturas,
  benchmarks, presets).
· Reconocimiento completo del motor con 9 lectores paralelos (stepper, universo, grid/render,
  galería, herramientas, piel/red, docs históricos, historial, harness) → `docs/LAB/mapa/`.
· Costuras (todas con «(R130)» en el código): `ModoLaboratorio` (regla 59 en 6 caminos),
  botón en el título, `SpawnLaboratorio`, plano `BuildLaboratorioDeLeyes` (partial), 4 campos
  por celda en CellGrid (humedad/carga/reposo/luz, viajan en SwapCells), 12 materiales nuevos
  (ids 66-77: Sedimento, Arcilla, Terracota, Grava, Planta —arquetipo nuevo—, Fibra, Hogar,
  NucleoFrio, Manantial, Sumidero, RocaSuelta, Semilla), `AplicarOverridesLaboratorio`,
  ganchos en SimStepper (tiempos por fase, salto de filas de chunks dormidas, LabPasadas,
  erosión, combustible mojado, reposo), tiempo acelerado con presupuesto en AlkahestSim,
  `PaintLab`, cincel/frasco/colisión con los sólidos del laboratorio, tinte del renderer.
· `SimStepper.Laboratorio.cs`: pasada de campos sobre TODA la grilla a 1/8 (aire: vapor,
  condensación, rocío; agua: evaporación, infiltración con colmatación, decantación, depósito;
  porosos: exudación, percolación, capilaridad, secado, compactación→arcilla, ablandamiento,
  cocción→terracota, abono; hogar/frío/manantial/sumidero), PRESIÓN hidrostática por cuerpos
  de agua conectados (verificada con un tubo en U: 237/199 → 219/217, agua conservada), luz por
  máximo con decaimiento, térmica propia (k/c por clase, convección). `LabParams` con 84
  parámetros registrados (nombre, unidad, default, rango, ayuda). `LabPanel` (F8) mínimo:
  tiempo 1×-100×, coste por fase, libro mayor, sliders por grupo con «D» y «?».
· Medido en el editor de Cesar: 2,34 ms/tick con 87 chunks despiertos (campos 0,15, presión
  0,09, luz 5,4 ms cada 8 ticks = el cuello), 406 ticks/s headless. Hallazgo del plano: la
  fisura de arena es polvo y cae a la cámara profunda (el arroyo se cuela por el agujero):
  Opus lo corrige en H1 con una roca porosa estática.
· Handoff a Opus 5: 8 hitos con interfaces, criterios de aceptación, benchmarks, riesgos y
  qué escalar (`docs/LAB/HANDOFF_OPUS.md`). Sin push (cmd 130 para Cesar).

## Ronda 131 — EL LABORATORIO, H1: EL CIRCUITO DEL AGUA SE CIERRA (Opus 5, verificada en vivo)

· Primer hito del handoff de Fable (`docs/LAB/HANDOFF_OPUS.md` §4, H1). El plano de la R130
  tenía una fuga de diseño medida: la fisura x192-202 era ARENA (Powder), se derrumbaba a la
  cámara profunda y el arroyo entero se colaba por el agujero — el sumidero no recibía una
  gota (`LabAguaSumida = 0`). Y detrás quedaba una segunda trampa: la grieta x336-343, abierta,
  era otro pozo de 8 celdas justo después del labio de la poza.
· **`Arenisca`** (id 78, `MaterialId.Count` = 79): roca POROSA estática (`caeSolido=false`,
  `suelo.permArenisca` = 30, color 196/172/128). No cae, el agua la atraviesa despacio y sale
  LIMPIA por el otro lado — los finos se quedan dentro y la van colmatando. El cincel la
  desprende como `Sand`. Entra en las cuatro tablas de `LabMateriales`, en el `switch` de
  porosos de `LabCampos`, en `LabK`/`LabC` como roca, en `Rellenar(250, Resistir)` y en el
  tinte del renderer (la arenisca mojada se oscurece: el frente de agua bajando SE VE).
· La fisura pasa a `Arenisca`. La grieta se mantiene en sitio y tamaño pero **atascada de
  escombro**: `Grava` y112-132 sobre una repisa de `Arenisca` en y111. Sangra un hilo
  (≈0,24 celdas/s) hacia la cámara profunda y deja pasar el resto aguas abajo; además la grava
  se colmata con los finos del manantial, así que el hilo se cierra solo, y destaparla a
  cincel es un mando real. Alternativas descartadas documentadas en el código (regla 15).
· **Resultado medido** (banco headless, 18 000 ticks, determinista): régimen permanente desde
  t≈3 000 — los niveles no se mueven en 15 000 ticks y el sumidero traga 20 celdas/s, que es
  exactamente lo que entrega el manantial. Sumidero 10 949 celdas (antes 0), 160 exudaciones,
  charco estable de 15 celdas en la cámara profunda, arena del mundo constante en 95 celdas
  (la fisura ya no se derrumba). 2,08 ms/tick, 480 ticks/s.
· **Tres correcciones de conservación encontradas midiendo** (el balance salía +10,4 %):
  (1) el depósito escribía 255 de humedad fija en el sedimento en vez del volumen real de la
  celda de agua — el propio comentario ya decía qué debía hacer; (2) `LabPresion` aniquilaba
  el vapor del hueco al mudar el agua (−4,5 u por mudanza, el 85 % del descuadre: la presión
  iba SECANDO el aire de la cueva, justo lo contrario de lo que H3 necesita) — ahora el aire
  se muda al hueco que deja el agua, que es un intercambio como cualquier otro; (3) la
  auditoría no cubría el vaciado del poro al exudar/gotear.
· **`LabBalanceU`**: auditoría de conservación permanente. Suma todo lo que el laboratorio crea
  o destruye de `humedad[]` en los tres únicos sitios que escriben sin restar en otro lado, así
  que el invariante 3 del handoff se comprueba con una resta exacta:
  `Σ humedad(t) − Σ humedad(0) == LabBalanceU`. Descuadre final **0,113 %** (pedido: ±5 %) y
  deja de crecer. El residuo no está en el laboratorio sino en el barrido ordinario de
  `SimStepper.cs`, que el handoff prohíbe tocar: escalado a Fable con la medida.
  También `LabAguaSumidaU` (unidades, no celdas: contar "255 × celdas" sobreestimaba un 4 %).
· **Hallazgo operativo**: `Unity_RunCommand` puede instanciar los tipos del proyecto
  directamente (`using Alkahest.Sim;` + `new CellGrid()` + `new SimStepper(u, g)` + `Step()`)
  y correr la simulación **sin entrar en Play**, a ~530 ticks/s, y dibujar la grilla a un PNG.
  Eso adelanta el banco headless de H5 y quita el riesgo de matar la sesión de Play editando
  un `.cs`. La reflexión solo hace falta para miembros privados (`DayCycle.RestartRun`).
· Verificado JUGANDO (regla 52): Play → `ModoLaboratorio` + `RestartRun(777002)` → 10×
  sostenido (`LabMultiplicadorReal` = 10,0), 0 errores de consola, capturas en vivo de la
  panorámica, la fisura y la grieta. Evidencia: `Laboratorio/capturas/R131_h1_*.png`,
  medidas en `Laboratorio/benchmarks/2026-09-03_h1_circuito_del_agua.md`, decisiones D13/D14 y
  §6b en `docs/LAB/CHECKPOINT.md`, escalados Q1-Q3 en `docs/LAB/PREGUNTAS_A_FABLE.md`.
  Sin push (cmd 131 para Cesar).

## Ronda 131b — EL LABORATORIO, H2: EL PANEL COMPLETO (Opus 5, verificada en vivo)

· Segundo hito del handoff. El panel de la R130 sabía enseñar y tocar números; ahora sabe
  GUARDARLOS, PINTAR MATERIA y ENSEÑAR LOS CAMPOS QUE NO SE VEN.
· **`Game/LabPresets.cs`**: presets JSON en `Laboratorio/presets/` (nombre, fecha, nota y los
  85 números), cargar, listar, comparar y `_defaults.json` escrito solo al arrancar. JSON a
  mano y a propósito: `JsonUtility` no serializa diccionarios y el formato es un mapa
  clave→número que se edita en el bloc de notas y se versiona en git. El lector es TOLERANTE
  (clave que ya no existe → se ignora; clave que falta → se queda como está) y lo cuenta, para
  que un preset de hoy siga cargando cuando H4 añada los parámetros de las plantas.
· **Snapshot** = preset + PNG (misma receta URP que el curador) + `_libro.json` con censo por
  material, libro mayor completo, inventario de agua por clase, tick y posición del muñeco. El
  primero, `ref_h1_circuito`, sirvió además de comprobación real: en su libro,
  `251 752 − 184 535 = 67 217 = balanceU` EXACTO — la auditoría de conservación de H1 cuadra al
  bit también en el juego en vivo, no solo en el banco headless.
· **Pincel de materia**: 22 entradas en cuatro grupos (SUELO, VIDA, LEYES, FLUIDOS), radio 0-8
  con −/+ y anillo en el cursor, izquierdo pinta y derecho borra. Con el pincel armado
  `BloqueaHerramientas` es true aunque el ratón esté fuera del panel: no se talla y se pinta a
  la vez. El agua turbia entra por `PaintLab` (los finos son un campo: `PaintStable` la haría
  nacer limpia) y la brasa/fuego por `PaintCell` a 220 raw (regla 22). Colocar un MANANTIAL o
  un SUMIDERO con el pincel es el gesto más potente del sandbox.
· **Vistas de depuración**: cuarta textura siguiendo el patrón exacto del velo de líquidos
  (R129) — textura propia, sprite propio (orden 54, alfa 150), rellenada en el mismo barrido
  por chunks. Seis campos: temperatura (diferencia con el ambiente DE CADA CELDA), humedad,
  carga, reposo, luz y chunks despiertos. Coste cero cuando no se usa: la textura no se crea
  hasta que alguien elige una vista, y `LabPanel.OnDestroy` devuelve `VistaLab` a Ninguna al
  salir del laboratorio (es estática y sobrevive a la escena). Es la excepción documentada del
  HANDOFF §2 a «no tocar los archivos grandes»: 5 costuras de 7 líneas en `SimRenderer.cs`.
· **Lo que las vistas enseñaron a la primera**: la de temperatura muestra el penacho del hogar
  subiendo por la chimenea (la convección funciona) y el arroyo MÁS FRÍO que la cueva, porque
  evaporar enfría — calor latente visible sin abrir un solo número. La de luz, el cono del
  hogar y la columna que baja por la boca del cielo.
· Ayuda general plegable (qué es una visita, por qué 255 = una celda, raw→°C, el punto ●, y el
  aviso de que los chunks dormidos pueden ir 30 frames por detrás en una vista).
· Verificado JUGANDO (regla 52): guardar → cambiar 5 números → «TODO A DEFAULTS» → cargar
  restaura exactamente esos 5 (85 aplicados, 0 desconocidas, 0 ausentes); snapshot deja los
  tres archivos; capturas de las pestañas y de las vistas. Regresión: con `ModoLaboratorio` en
  false, `LabActivo`, el tinte, `VistaLab`, el panel y el sprite de la vista están apagados o
  no existen, y `BloqueaHerramientas` es false. 0 errores de consola. Sin push (cmd 131).

## Ronda 132 — EL LABORATORIO: CONSERVACIÓN AL BIT Y EL CICLO DEL AGUA (Opus 5, con Fable respondiendo)

· Fable contestó los tres escalados de la R131 (`docs/LAB/PREGUNTAS_A_FABLE.md`): aceptó el
  intercambio de vapor en `LabPresion` (y pidió añadir también el de temperatura, hecho),
  aceptó la grieta atascada de grava, y DIAGNOSTICÓ el residuo de conservación leyendo el
  código en vez de suponerlo: no estaba en `SimStepper.cs` sino en `LabAgua`, donde `vol` es
  una variable local y `hum[i]` solo se escribe al final, así que el auditor apuntaba como
  DESTRUIDO lo que se acababa de TRANSFERIR. Correcto.
· Su parche (las cuatro salidas por `vol <= 0`) llevó el residuo de **632 a 144**, no a cero.
  La medida encontró el resto: **Δresiduo == Δdepósito en cada tick, 1 u por depósito**. El
  DEPÓSITO es la QUINTA salida del método y ocurre después de evaporar e infiltrar, así que
  arrastra el mismo desfase. Con la quinta sincronización: **residuo 0 EXACTO** a t=3 000,
  9 000 y 18 000, y en un canal de prueba aislado. `Σ humedad(t) − Σ humedad(0) == LabBalanceU`
  al bit; el invariante 3 del handoff pasa de aspiración a aserción comprobable.
  Queda escrita en el código la regla: toda salida anticipada de `LabAgua` sincroniza.
· También del mismo diagnóstico: el abono de ceniza solo cede lo que CABE en el sustrato, y la
  cocción manda al aire el agua que le queda a la arcilla antes de volverse terracota (un horno
  de cerámica humedece el cuarto) en vez de destruirla en silencio.
· **H3.1 — el ciclo del agua, y una hipótesis de Fable refutada midiendo.** Él sospechaba que
  el vapor moría de vejez a mitad de chimenea (`vidaVapor` 60 ticks vs ~65 celdas). Medido con
  el gesto de H3.1 (verter agua sobre el hogar): **CERO celdas de vapor vivas, nunca, con
  cualquier `vidaVapor`** — subirlo a 200 no cambió nada. No moría de vejez: moría en el mismo
  tick. `Steam.condensesAt` estaba escrito A MANO en 60 °C dentro de
  `AplicarOverridesLaboratorio`, por encima de los 20 °C de la cueva, así que cada celda de
  vapor se volvía agua a dos celdas de la brasa. Ese número no era un `LabParam`: ni el panel
  lo enseñaba ni un preset lo capturaba, contra el invariante 5 del propio handoff.
· Promovido a **`vapor.condensaC`** (86 parámetros) y puesto en **10 °C**: por debajo del
  ambiente, para que el vapor VIAJE; por encima de los 8 °C de la cámara alta, para que
  condense donde de verdad hace frío. Con él, `vapor.vidaVapor` 60 → 180 (ahí Fable sí tenía
  razón: la chimenea mide 65 celdas) y `vapor.ascenso` 6 → 12.
· **Resultado**: hervir agua sobre la brasa eterna manda una columna de vapor por la chimenea y
  **llueve en la cámara alta a los 450-1000 ticks = 15-33 s de mundo** (el criterio pedía menos
  de 3 minutos). 21 celdas de agua líquida arriba y el sedimento seco del piso pasando de 0 a
  17-22 de humedad. Destilación en columna salida de la geometría del plano, no de un guion.
  Preset y snapshot `ref_destilacion`; capturas de la columna, del reflujo cayendo por la
  chimenea y de la cámara fría mojándose.
· Autorización de Fable usada: contador `LabCondensadoGas` (la condensación del vapor VISIBLE
  no la contaba nadie, así que «condensado = 0» no significaba lo que parecía) con sus dos
  líneas en `SimStepper.cs`, marcadas `(R132)`.
· Regresión: el circuito de agua de H1 idéntico con los defaults nuevos (residuo 0, poza 136,
  sumidero 4 802 celdas a t=9 000, 1,89 ms/tick, 528 ticks/s), 0 errores de consola.
· Escalados nuevos a Fable (Q4, Q5): hay DOS mecanismos de condensación y solo dispara el del
  gas — `LabGoteos` sigue en 0 porque el aire nunca llega a su saturación en una cámara de
  2 548 celdas; y el aviso de que `condensaC` era un número del decreto físico. Sin push (cmd 132).

## Ronda 133 — EL LABORATORIO, H3 COMPLETO: LAS CINCO CADENAS DEL AGUA (Opus 5, con Fable)

· Fable respondió Q4/Q5 y añadió una corrección de condición inicial (R5.1) que resultó ser la
  pieza que faltaba: **el aire de la cueva no nace seco**. `aire.humedadInicialPct` = 60 — cada
  celda de aire arranca al 60 % de SU PROPIA saturación, que depende de su ambiente, así que
  ninguna nace supersaturada. Con aire seco, el primer vapor que el jugador produce se gastaba
  entero en humedecer el volumen (en una cámara de 2 548 celdas, ~350 celdas de agua tiradas).
  Efecto medido y mayor de lo que él predijo: su mecanismo de condensación por saturación, que
  estaba MUERTO (`LabCondensado` = 0 en toda la R132), revive con 755 en 9 000 ticks y aparecen
  **los primeros goteos de la historia del laboratorio** (t=2 597 = 87 s; con el aire al 85 %,
  64 s; con aire seco, nunca). Mi Q4 no era «elegir entre los dos mecanismos»: el suyo estaba
  apagado por la condición inicial. 0 supersaturadas, residuo 0, circuito de H1 idéntico.
· **H3.2 SEDIMENTACIÓN**: la poza es un decantador de verdad — el agua entra a 96 de turbidez y
  sale a 45. El churn erosión↔depósito era **ruido puro**: las cuatro configuraciones probadas
  dan el MISMO resultado neto (372-380 celdas), o sea 6 279 eventos para mover 375 celdas, 17 a
  1. Fable proponía subir `sed.depositoReposo` Y bajar `sed.erosionPct`; midiendo los dos
  mandos por separado, el reposo hace todo el trabajo (churn −57 % y la poza decanta MEJOR,
  61 % contra 53 %) y bajar la erosión empeora la clarificación (49 %) y deja el lecho estático.
  Adoptada media propuesta: reposo 8 → 24, erosión se queda en 6.
· **H3.3 CANAL SELLADO**: con agua turbia (carga 255) el lecho de arena deja de infiltrar en
  ~15 s; con la turbidez del manantial (40), en ~100 s; con agua limpia, **nunca** (meseta de
  14 850 u por bloque, plana durante 2 700 ticks). Y un detalle no previsto: **la costra tiene
  UNA celda de espesor** — debajo la arena sigue seca (col=0, hum=20), mientras que con agua
  limpia el frente mojado atraviesa el lecho entero. Colmatación superficial como en un filtro
  real, y la lección sale sola: *decanta primero, filtra después*.
· **H3.4 ARCILLA**: el sedimento húmedo enterrado compacta en arcilla a los 106 s. Cocer NO
  ocurre en un montón estático y el perfil dice por qué: las dos condiciones **se excluyen en el
  sitio** (compactar pide humedad 100-230, cocer pide ≤ 30), así que la arcilla se forma justo
  donde está mojada y, una vez formada, su permeabilidad 2 le impide secarse por dentro. No es
  un bug: es la receta del alfarero — moldear enterrado y DESENTERRAR. Expuesta al aire junto al
  hogar cuece en **22-30 s**, y solo las 16 celdas de la CARA: la pieza queda cruda por dentro,
  como una cerámica mal cocida de verdad. El cuenco de terracota retiene 164 de 160 celdas en
  300 s; el mismo cuenco de arena se queda en 3.
· **H3.5 ALAMBIQUE**: el serpentín de `NucleoFrio` pintado en el techo de la cámara alta lleva
  el primer goteo de 84 s a **7 s** y de 11 goteos a 93, con 2 614 u de rocío colgando. Fable
  acertó de lleno con el serpentín. Su predicción sobre el tamaño, en cambio, NO se cumple:
  cerrar la cámara con tabiques supersatura el aire (42,8 sobre 36) pero deja el goteo
  exactamente igual (11 goteos, 85 s). La razón está en la aritmética de `LabAire`: cada celda
  de aire condensa como mucho 24 u sobre UNA superficie vecina por visita, así que lo que decide
  el goteo no es cuánta humedad hay sino cuántas celdas de aire tocan cada celda de pared — y
  una cámara pequeña tiene MENOS aire por pared. La regla que el laboratorio enseña no es «frío
  y poco aire» sino **«una superficie muy fría»**. Escalado como Q6 con la propuesta de dejarlo.
· **Un bug de bordes en `LabPresion`, cazado por el banco.** El escenario del cuenco (grilla
  sintética sin roca alrededor) reventó con `IndexOutOfRangeException`: el BFS de los cuerpos de
  agua encolaba vecinos sin comprobar los límites del mundo — agua en la fila 0 hacía `c - W`
  negativo, y agua en la columna 0 tomaba como vecino izquierdo la última celda de la fila
  anterior, o sea un cuerpo de agua ENVUELTO por el borde del mundo. En el plano no salta porque
  hay roca alrededor, pero un jugador que talle hasta el borde lo provoca. Acotado con una
  división por celda desencolada; **el tubo en U de Fable sigue dando sus números exactos de la
  R130 (237/199 → 219/217)** y la presión cuesta lo mismo (0,094 ms/tick).
· Los CINCO fenómenos de H3 ocurren con **un solo juego de números**: no hizo falta un preset
  por cadena porque las cadenas no compiten entre ellas. Defaults nuevos:
  `aire.humedadInicialPct` 60, `sed.depositoReposo` 24 (87 parámetros en total).
· Presets y snapshots `ref_destilacion` y `ref_alambique`; captura `R133_h35_alambique.png`;
  medidas en `Laboratorio/benchmarks/2026-09-03_r133_h3_completo.md`; decisiones D19-D21 y §6e
  en `docs/LAB/CHECKPOINT.md`; respuestas R5b-R7c y escalado Q6 en `PREGUNTAS_A_FABLE.md`.
  Verificado jugando, 0 errores de consola. Sin push (cmd 133).

## Ronda 134 — EL LABORATORIO, H4: PLANTAS Y FIBRA (Opus 5)

· Una planta es una COLUMNA de celdas que se pasan savia hacia arriba, y los campos que ya
  existían bastan: `humedad` es la savia, `aux` la altura sobre la raíz (la planta no se mueve,
  así que su aux no lo usa nadie) y `reposo` el contador de sequía. Cinco cosas por visita: sin
  raíz ni tallo debajo se seca en fibra; la RAÍZ bebe del sustrato sin bajarlo del punto de
  marchitez (y más si el suelo es fértil: el bonus se aplica a lo que bebe, no a un contador
  aparte); la SAVIA sube media diferencia con tope; TRANSPIRA al aire no saturado; y la PUNTA
  crece si tiene savia, sitio y LUZ en el destino, con su tirada de rama en diagonal.
· **Germinación**: una `Semilla` asentada sobre sustrato húmedo e iluminado se abre y su propia
  agua es la primera savia; y un sustrato húmedo, iluminado y con aire encima brota solo con
  `planta.germinaPorMil` por visita. **Marchitez**: sin savia N visitas → fibra, y deja abono al
  sustrato que la sostenía.
· **Conservación**: beber, subir y transpirar son transferencias; lo único que destruye agua es
  la savia que se vuelve tallo, y se audita a mano. **Residuo 0** en todos los escenarios.
· **Cumplen y están medidos**: germinan solas (36 nacidas sin plantar ninguna) · sin luz NO
  (0 en tres corridas) · sin agua NO (0) · sin savia mueren y dejan fibra (24 muertas → 13
  fibras) · la fibra seca PRENDE y deja ceniza (fuego → brasa → ceniza 18 → 37) · `LabCampos`
  0,48-0,50 ms (el criterio pedía < 0,5).
· **NO cumple**: a los 150 s han muerto todas. No hay régimen estable de vegetación. Tres causas
  encontradas y corregidas, la cuarta escalada:
· **(1) El suelo se convertía en cerámica.** La compactación pedía `LabVecinosSolidos(i) >= 3`,
  un número suelto en el stepper (contra el invariante 5). Pero 3 vecinos sólidos los tiene
  también la CARA de un suelo (abajo y los dos lados), así que la superficie de cualquier huerto
  se volvía ARCILLA —que no es sustrato— y las plantas perdían la raíz de golpe: sustrato 254 →
  54 mientras arcilla 60 → 302 en 100 s. Promovido a `suelo.compactVecinos` = 4 («enterrado de
  verdad»); la cadena de H3.4 sigue dando 141 celdas de arcilla en un montón enterrado.
· **(2) Ningún suelo podía tener la cara húmeda.** `suelo.capilarArriba` valía 2/256: un suelo
  empapado subía UNA unidad por visita a su superficie mientras el secado se llevaba TRES. La
  cara de cualquier suelo tendía a cero por muy mojado que estuviera por dentro, así que nada
  podía germinar nunca. Subido a 16: se equilibra alrededor de 100. Era el número que hacía
  imposible la vegetación y no estaba a la vista de nadie.
· **(3) Las plantas se apagaban a sí mismas.** `luz.decayPlanta` valía 40 y el claro de la
  cámara alta da 48-216, así que la segunda celda de tallo ya quedaba a oscuras: ninguna pasaba
  de 4 de altura con el tope en 14. Bajado a 12; ahora bajo la boca del cielo llegan al tope y
  en el borde del claro apenas levantan una celda — la vegetación toma la forma de la luz, que
  es la regla 40 funcionando sola.
· **(4, escalado Q7)** El piso de la cámara alta es polvo de 2 celdas con un agujero al lado
  (la boca de la chimenea): el goteo lo erosiona y lo escurre. Sustrato en el claro 74 → 22 en
  300 s; apagando la erosión, 74 → 65, o sea que la erosión explica dos tercios. Propuesta:
  tocar el PLANO (lecho de 4-5 celdas sobre solera de arcilla y un labio alrededor de la boca),
  no la regla de erosión — que la lluvia lave un suelo desnudo es correcto y es una lección.
· **Mapa de luz de la cámara alta** (medido): 47 de 91 celdas del piso superan el mínimo de 40;
  el claro es de sobra y `planta.luzMin` se queda en 40. Aviso operativo que costó una tarde: un
  serpentín de núcleo frío pintado en x104-133 TAPA la boca del cielo y deja la cámara a
  oscuras; hay que dejar la ventana libre.
· 90 parámetros. Regresión: residuo 0 con plantas activas, H1 idéntico (poza 136, sumidero 4 819,
  1,92 ms/tick), H3.4 intacto, 0 errores. Verificado jugando (huerto pintado en el claro: las
  plantas amarillean al quedarse sin savia y el brote sale más claro que el tallo).
  Benchmark, CHECKPOINT D22-D25 y §6f, captura `R134_h4_huerto.png`. Sin push (cmd 134).

## Ronda 135 — EL LABORATORIO: CIERRES DE Q6/Q7 Y EL DOMINIO DEL FUEGO (Opus 5, diseño de Fable)

· **Cierre de Q6 (R8).** `LabAire` condensa ahora sobre el vecino condensable MÁS FRÍO, no sobre
  el primero de la lista. Su criterio de regresión (≥ 93 goteos en `ref_alambique`) NO se cumple
  —salen 70 con el mismo plano— pero la medida explica por qué y demuestra que el efecto sí: el
  rocío en la ROCA pasa de repartido por el techo a **CERO**, y el del serpentín a 5 926 u. Los
  goteos bajan porque el mismo rocío cae desde menos celdas, más concentradas. Y el caso que
  motivaba el cambio funciona: un serpentín en la PARED, con techo encima, recibe 1 933 u donde
  antes no recibía nada. Propuesto cambiar la métrica de «cuántas gotas» a «dónde caen».
· **Cierre de Q7 (R9).** Piso nuevo de la cámara alta: solera de arcilla, lecho de 4 celdas de
  sedimento y labio de roca a los lados de la boca de la chimenea. Más la regla que Fable añadió:
  un sedimento con una PLANTA encima no se erosiona (una línea en `LabErosion`), que es lo que da
  a la vegetación su papel sistémico. **Y un rebosadero que no estaba en la spec y era
  imprescindible**: el labio una celda por encima del lecho convierte la solera impermeable en
  una BAÑERA (24 de 48 columnas del claro bajo agua a los 150 s, y bajo el agua no germina nada).
  A ras del lecho, el sustrato del claro pasa de 28 % a **84 %**. El criterio revisado de H4
  sigue sin cumplirse, pero por ENCHARCAMIENTO y no por sequía: a los 50 s hay 14 columnas aptas
  y germinan; a los 150 s las ha tapado el agua. Escalado como Q8: 2 286 goteos en 10 minutos es
  caudal industrial para un jardín de 48 columnas.
· **HF1 — EL AIRE DE CONTACTO.** La única regla nueva del fuego: quien arde sin un vecino de aire,
  o ahogado en su propio humo, arde EN SORDINA (consume a ¼, calienta a ½, sin lengua, humo a ¼),
  y al agotarse deja CARBÓN (id 79) en vez de brasa. Sin campo de oxígeno: el aire es el vacío que
  ya existe y el humo es el aire gastado. Seis costuras `(R135)` en `ProcessCombustion`/
  `ProcessBrasa` (excepción autorizada). Más `fuego.vidaHumo` 400, `luz.decayHumo` 24 y
  `fuego.hogarRaw` 220 → **170**: el hogar gratuito pasa a ser DOMÉSTICO.
· **B-F3 la carbonera**: con boca de 1 celda, **100 % de carbón, 0 de ceniza, 0 de llama y 0 de
  humo**; al abrir la boca aparece lo que se consume del todo (19 % de ceniza con boca 4). Dos
  hallazgos que no estaban previstos: **una pila maciza es su propia carbonera** aunque esté al
  aire libre (sus 324 celdas interiores no tienen un vecino de aire), y **un recinto cerrado se
  enciende solo por acumulación de calor mientras que uno abierto no** — el aislamiento es lo que
  permite alcanzar la ignición. Coste: **+2,6 %** (1,95 contra 1,90 ms/tick) con 5 000 celdas
  ardiendo, muy por debajo del +10 % de aceptación. Regresión del agua idéntica (residuo 0).
· **HF2 — EL HORNO Y EL VIDRIO.** La arena con ceniza al lado (el fundente) acumula su «tiempo al
  rojo» por encima de `fuego.vidrioRaw` y se vuelve `VidrioVerde`; si se enfría, la cuenta se
  reinicia. Medido: dentro del recinto de arcilla, **386 °C sostenidos y 29 celdas de vidrio**;
  el hogar suelto (170 raw) no vidria nada. **Apareció sola una cadena de encendido que nadie
  escribió**: el hogar no puede prender el carbón (ignición 200 > 170), así que hace falta LLAMA
  — hogar → yesca de fibra → llama 255 raw → carbón. La curva boca→temperatura sale **PLANA**
  (228/232/231 raw): con el carbón macizo la boca no regula porque el grueso no respira de todos
  modos. Escalado como Q9. Y una nota de método: monté mal el banco tres veces, siempre por lo
  mismo — los polvos se reacomodan y un horno relleno con huecos se vacía hacia el hueco; arrancó
  cuando puse el fuego POR ABAJO, como un horno de verdad.
· **HF3 — LA ESTUFA DE TOLVA.** Silo de 360 celdas de fibra sobre un fogón con boca de 3:
  **324 s de mundo con el fogón por encima de 150 raw y CERO intervenciones** (la aceptación
  pedía 300 s). La fibra cae por gravedad a medida que la base se consume.
· **HF4 — EL LIBRO DE ENERGÍA.** `LabCombustibleQuemado` y `LabCalorFuego`. Su razón mide por sí
  sola cuánto de la quema ocurrió sin respirar: 10,3 frente a los 14 de la fibra al aire → el
  **53 % de la tolva ardió ahogada**, que es exactamente lo que la hace durar cinco minutos en
  vez de uno. Deja de ser intuición y pasa a ser un número del panel.
· **Veredicto §7 del diseño del fuego: 3,5 de 5.** Cumplen la máquina escondida en las leyes
  (horno → vidrio), la automatización sin jugador (tolva) y el libro de energía; falla el mando
  monótono tal como está formulado (Q9), y la cadena cruzada queda para H7 jugando.
· 95 parámetros. Verificado jugando: laboratorio con el hogar a 170 y el piso nuevo (arcilla,
  sedimento y labio con su rebosadero), y una carbonera de terracota pintada junto al hogar
  convirtiendo fibra en carbón (243 → 109, solo 7 de ceniza). 0 errores de consola.
  Benchmark, CHECKPOINT D26-D30 y §6g, buzón R8b-R10b y Q8/Q9, captura `R135_hf1_carbonera.png`.
  Parado en HF4 por instrucción de Cesar: sin entrar en H5 ni H6. Sin push (cmd 135).


## Ronda 136 — EL LABORATORIO: EL TIRO QUE NO EXISTE Y EL HOGAR QUE NO ERA DOMÉSTICO (Fable 5.1, solo medidas y decisiones)

Cesar pidió decidir si los 1,5 puntos que le faltaban al fuego (veredicto 3,5/5 de Opus en la
R135) se cerraban con correcciones conceptuales pequeñas, antes de tocar H5/H6. Fable no tocó
código: midió la build R135 en banco headless por RunCommand (caja de piedra 20×12, hogar +
yesca + carbón, chimenea de 0-8 celdas) y respondió Q8 y Q9.

**Lo medido.** (1) Subir el humo del carbón de 4 % a 40 % da una simulación **idéntica al bit**:
el humo no nace porque la llama sobre combustible es inmortal (`life = 30` mientras toque
combustible), F1 la cuenta como aire y ocupa la única celda vacía por donde saldría el humo.
(2) La llama inyecta 40 raw/tick sin gastar reserva: 15 veces el carbón que la sostiene; el
calor del horno es de llama. (3) El aire no se gasta: no hay tiro que regular. Emulando «la
llama es el sensor», la pila fina da 245/235/228 raw con chimenea 0/2/8 —monótona al revés, la
chimenea es una fuga— y el ahogo baja el consumo un 4 %. (4) **El hogar no es doméstico**:
`InjectHeat(40)` sin tope deja a 255 raw la celda de encima; arena + ceniza sobre el hogar →
VidrioVerde a los 800 ticks sin horno. (5) Carbonizar multiplica la energía ×6,3 (fibra 560
raw/celda → carbón 3 520). (6) `vidaHumo` 400 es 255 (byte).

**Decisiones (R11-R14).** Q8: sin regla; la válvula del riego es la caldera, no el serpentín
(tras R8 el serpentín recibe todo el rocío); desagüe de grava en el nivel, geometría. Q9:
criterio 3 reformulado como dos mandos que sí existen (recinto y contacto), medio punto; el
tiro se declara inexistente y su paquete de tres reglas queda archivado, no autorizado. HF5:
cuatro parches de honestidad —C1 tope del hogar (`LabCalentarHasta`), C2 rendimiento del
carbón 25 % con reserva 50 (identidad energética), C3 contadores de llama/hogar/carbón, C4
vidaHumo 255— con aceptaciones. Veredicto: 3,5 → 2,5 medido → 4 tras HF5 (4,5 con la cadena
en H7). Recomendación: **congelar física nueva** tras HF5; H7 jugando; H6 espera.

Archivos: `Laboratorio/benchmarks/2026-09-04_r136_fable_tiro_y_hogar.md`,
`docs/LAB/PREGUNTAS_A_FABLE.md` (R11-R14), `docs/LAB/DISENO_FUEGO.md` §10,
`docs/LAB/HANDOFF_OPUS.md` HF5, `docs/LAB/CHECKPOINT.md` §9. Sin cambios de código.

## Ronda 137 — EL LABORATORIO: HF5, LOS CIERRES DEL FUEGO (Opus 5, parches de Fable)

· **El punto de partida es que Fable midió la build R135 en su propio banco y desmintió tres
  cosas de su propio diseño**: el hogar calentaba a sus vecinos a 255 raw (arena con ceniza
  encima vidriaba en 27 s, sin horno), carbonizar multiplicaba la energía ×6,3, y el libro no
  contaba la llama. Más `vidaHumo` 400, que era un byte y se recortaba en silencio. HF5 son sus
  cuatro parches, y salió un quinto.
· **C1 — EL HOGAR ES DOMÉSTICO DE VERDAD.** `LabHogar` usa `LabCalentarHasta`, con tope en
  `fuego.hogarRaw`, en vez de `InjectHeat`, que sumaba sin límite. Medido a 3 000 ticks: hogar +
  arena + ceniza → **0 vidrio**; el agua encima hierve; la fibra pegada prende. La frontera
  entre vivir y fabricar vuelve a estar donde tenía que estar.
· **PERO el carbón pegado también prende, y eso corrige un hallazgo de R135.** La causa está
  fuera del laboratorio: `TryIgnite` enciende cualquier vecino inflamable con un **12 % por
  tick** que NO mira la temperatura de ignición. Así que la «cadena de encendido emergente» que
  celebré en R135 —hogar no prende carbón porque 170 < 200, luego hace falta yesca— era falsa.
  Lo bueno: **medida otra vez, la cadena sigue en pie por un motivo mejor**. En el horno la
  SOLERA DE CENIZA se interpone entre el piloto y el combustible, y sin yesca que la atraviese
  el carbón sigue intacto a los 300 s (144 celdas, 170 raw). Con yesca, 18 de 18 de vidrio. Es
  geometría, no un umbral. Escalado como Q10 sin tocar la regla base.
· **C2 — CARBONIZAR YA NO CREA ENERGÍA.** `fuego.rendimientoCarbonPct` = 25 (sal 632) y la
  reserva del carbón 160 → 50. Solo una de cada cuatro celdas ahogadas deja carbón; el resto,
  ceniza. **Pico de carbón medido: 25,0 % EXACTO**, justo cuando la fibra se agota. Y el número
  que justifica las dos constantes: de los 224 000 raw de una carbonera de 400 celdas de fibra,
  la quema ahogada suelta 112 000 y el carbón que nace guarda **112 200** — mitad y mitad con un
  0,2 % de diferencia. Cambiar cantidad por calidad perdiendo por el camino, como una carbonera
  de verdad.
· **EL QUINTO PARCHE, que no estaba en la spec.** La identidad de Fable daba **+27,6 %** y la
  causa no era ninguno de sus tres diagnósticos: **el carbón nace y vuelve a arder**, así que su
  energía se cuenta al nacer y otra vez al quemarse. La prueba dura fue el propio libro:
  `LabCombustibleQuemado` = 21 100 = fibra 16 000 + carbón 5 100, exacto. Añadido
  `LabCalorCarbon` (una línea en la costura ya autorizada), la identidad se escribe entera y
  cierra a **+1,0 % con 900 celdas** (−0,8 % y +0,8 % en dos configuraciones de 400; el +5,5 %
  de una cuarta es ruido del sorteo con n pequeña).
· **C3 — LOS TRES CALORES.** `LabCalorLlama`, `LabCalorHogar`, `LabCalorCarbon`,
  `LabCarbonizado` y `LabEnergiaCarbon`, con la identidad en el panel. Lo que se ve al contarlos
  y antes no: **la llama es la fuente dominante**. En el horno, 130 240 raw de llama contra
  45 400 de brasa — la brasa que se ve por la boca es la cuarta parte del calor.
· **C4** — `fuego.vidaHumo` 400 → **255**, con la ayuda reescrita (tope de byte; bajo techo
  cuenta doble, 510 ticks).
· **CRITERIO 3 CERRADO COMO «RECINTO Y CONTACTO».** A igual masa (400 celdas) y la MISMA boca,
  cambiando solo la forma de la pila: maciza 20×20 → 27 % de carbón y 267 560 raw de llama;
  media 40×10 → 22,5 % y 541 480; **fina 100×4 → 19,5 % y 1 333 200**. Monótono en las dos
  columnas y **×5 de llama** entre extremos. El mando de geometría existe y vive en el
  COMBUSTIBLE, no en la boca del recinto: el aire nunca se consume, pero el contacto se decide
  vecino a vecino. Era exactamente lo que quedó abierto en Q9.
· **Aceptaciones**: horno con yesca **18 de 18** celdas de carga vidriadas (pedía ≥10), sin
  yesca **0**, con recinto y yesca pero sin carbón **1** — las tres líneas juntas son la
  frontera entera y ninguna estaba programada. Tolva **466 s** por encima de 150 raw (antes
  324), porque el hogar ya no sobrecalienta la base: la razón calor/reserva baja de 10,3 a
  **8,3 raw/u**, arde más ahogada y por eso dura más. Regresión del agua con **residuo 0** a los
  50, 150, 300 y 600 s. Coste **1,85 ms/tick** en reposo y 1,80 con 5 000 celdas ardiendo, sin
  regresión.
· **Q8, el desagüe de grava** (4 líneas en `SimLevelBuilder.Laboratorio.cs`): dos columnas de
  grava junto a cada labio y la mitad baja del labio también de grava. Conservación intacta. Y
  **drena al revés de lo que se temía**: la grava le da al agua un camino al subsuelo en vez de
  dejarla correr hasta la boca, y el labio fija el nivel freático a la altura del lecho — un
  drenaje CON nivel, que es lo que quiere un jardín. **H4 sigue abierto**: el banco ya no
  reproduce el encharcamiento de R135 (con rocío repartido el sustrato llega a humedad 12 en
  300 s y germinar pide 60), así que lo que falta medir es el caudal del alambique real, y eso
  se ve jugando. Va a H7, como propuso Fable.
· **Q11 escalada**: arder ahogado DESTRUYE la mitad de la energía (la sordina consume reserva a
  ¼ y da ½ del calor por unidad). Para la fibra está compensado por diseño; para el carbón, que
  al agotarse deja ceniza y no carbón, se pierde de verdad. Mi lectura es que está bien y además
  empuja a apagar la carbonera y sacar el producto, pero el criterio 5 dice «todo raw contado» y
  esa pérdida no la nombra nadie.
· 96 parámetros. Benchmark `2026-09-04_r136_hf5_cierres_del_fuego.md`, CHECKPOINT D31-D34 (y D30
  corregida), buzón con R11b-R14b y Q10/Q11. Verificado en banco headless, sin Play. Tras HF5,
  física nueva CONGELADA (recomendación de Fable; decide Cesar): sigue H7 jugando. Sin push
  (cmd 137; el 134 y el 135 siguen pendientes).


## Ronda 138 — EL LABORATORIO: REVISIÓN DE HF5 Y ORDEN DE CIERRE (Fable 5.1, sin código)

Opus entregó HF5 (R137, commit d711454): los cuatro parches de honestidad del fuego (tope del
hogar, rendimiento de la carbonera 25 % con reserva 50, contadores de llama/hogar/carbón, vidaHumo
255), el desagüe de grava de Q8, y dos preguntas nuevas: Q10 (el hogar sí prende carbón por el 12 %
de contacto de `TryIgnite`) y Q11 (arder ahogado pierde la mitad de la energía).

**Verificación propia (banco headless, geometrías distintas de las de Opus).** Hogar solo: 0
vidrio. Cuatro celdas de fibra sobre el hogar: **2 celdas de vidrio** (la llama, no el hogar; el
horno hace 18 de 18 → el criterio 1 se reescribe en cantidad). Carbonera 20×20 con boca 1: dos
corridas con **hash idéntico** (determinismo de la sal 632), identidad energética dentro del ±5 %.
Nivel real: **residuo 0** de conservación del agua a 3 000 ticks; y la grava del desagüe se mueve
(5 de 8 por conducto en su sitio, dos celdas en la boca de la chimenea).

**Revisión adversaria del diff** (cinco lectores + dos refutadores por hallazgo): 28 hallazgos
confirmados, 1 refutado, ninguno de física. Los cuatro altos: el hogar chispea desde el archivo
del laboratorio (línea 905), no desde el juego base; la brasa (material) no existe para el libro
de energía; el labio de grava del desagüe se derrumba en la boca en el primer tick (todo polvo
desliza en diagonal a un hueco, `fluidity` no lo impide); y la «salida por la mitad baja del labio»
no puede drenar (un poroso solo suelta agua a un vacío al saturar a 255). Además: los tres calores
del panel están en unidades distintas y se suman como TOTAL; el «12 % por tick» es por visita; la
causa de los 466 s de la tolva no la respalda el código; el carbón agotado va a brasa, no a ceniza;
la identidad es 555 por celda y estadística; el snapshot no guarda los contadores del fuego;
`_defaults.json` está viejo.

**Decisiones (R15-R17).** R15: el hogar calienta, no chispea (fuera `TryIgnite` de `LabHogar`;
enciende solo por temperatura: fibra sí, carbón no, mojada no) más `WakeChunk` en
`LabCalentarHasta`. R16: la pérdida en sordina se nombra (`LabCalorNoSoltado`) y el panel deja de
sumar peras con manzanas. R17: dos libros (nominal que se conserva; entregado con brasa y frío),
labio de roca y conducto a través de la solera, textos del benchmark corregidos. Todo junto es
**HF5b**, la última ronda antes de congelar. Orden acordado con Cesar: HF5b → **física nueva
congelada** → H5 banco → H7 jugando → H8 informe; **H6 (sólidos) documentado y congelado** para su
propia etapa. Veredicto del fuego: 4 de 5 medido.

Archivos: `Laboratorio/benchmarks/2026-09-04_r138_fable_verificacion_hf5.md`,
`docs/LAB/PREGUNTAS_A_FABLE.md` (R15-R17), `docs/LAB/HANDOFF_OPUS.md` (HF5b, H5/H6, §8),
`docs/LAB/CHECKPOINT.md` (fase y §9), `docs/LAB/DISENO_FUEGO.md` §10 ampliado. Sin código.

## Ronda 139 — EL LABORATORIO: HF5b, LA HONESTIDAD DEL FUEGO (Opus 5, revisión de Fable)

· **El punto de partida es una revisión adversaria de Fable sobre el commit de HF5**, con cinco
  lectores independientes y dos refutadores por hallazgo: **28 confirmados, 1 refutado**. Ninguno
  era de física; eran de contabilidad, de geometría del nivel y de texto. HF5b los cierra.
· **EL HOGAR CALIENTA, NO CHISPEA (R15).** Fuera las cuatro llamadas a `TryIgnite` de `LabHogar`:
  era la única línea por la que el hogar encendía con un 12 % de azar en vez de por temperatura, y
  por ella prendía carbón (200) al que no puede calentar por encima de 170. Ahora la cadena
  hogar → yesca de fibra (130) → llama → carbón es verdadera POR DOS NÚMEROS, que era el mecanismo
  bueno desde el principio. Medido: **carbón pegado 6/6 intactas a los 3 000 ticks con Tmax 170**,
  fibra seca pegada prende en t=48, fibra sobre una celda de arena en t=651, y la fibra que
  conserva su humedad no prende. El horno sigue en **18/18** porque la yesca la enciende
  `ApplyPhase`, y la tolva en **466 s**. Más `WakeChunk` en `LabCalentarHasta`: `AddTemp` no
  despierta a nadie, y sin eso una celda en el borde de un chunk dormido se quedaba a 170 sin que
  nadie la procesara.
· **DOS LIBROS DE ENERGÍA, PORQUE UNO SOLO MENTÍA (R17-B).** El nominal mide calor por unidad de
  reserva y es el que se CONSERVA: ahí vive la identidad de la carbonera, y ahí van los tres
  contadores nuevos (`LabCombustibleCarbon`, `LabUnidadesRespiradas` y `LabCalorNoSoltado`). El
  entregado —helper `LabInyectar`, que hace los mismos `AddTemp` y devuelve la suma de deltas
  reales— mide los raw que de verdad se escriben en `temp[]` tras el recorte, con
  `LabRawFuego/Llama/Brasa/Hogar/Frio`. La brasa **no existía para el libro** y emite más calor
  nominal que la combustión que sí se contaba: era el agujero más grande.
· **Y LA MEDIDA NOS DESMIENTE A LOS DOS.** Yo escribí «la llama es el 90 % del calor»; Fable lo
  corrigió a «lo medido es ¾». **Las dos cifras son del libro nominal y las dos están mal.** En
  raw entregados, la llama pone el **29 %** en una hoguera al aire, el **7 %** en el horno y el
  **6 %** en una carbonera sellada, y la fuente que más escribe es siempre la COMBUSTIÓN
  (48-67 %). La causa cabe en una frase: en la hoguera la llama suelta **622 040 nominales y
  entrega 105 579**, el 17 %, porque lo que ya está a 255 no admite más. Cuanto más se parece el
  sitio a un horno, MENOS entrega la llama. El hallazgo B de Fable predijo exactamente esto sin
  saber la cifra: sumar el libro nominal daba conclusiones invertidas.
· **TODO RAW TIENE NOMBRE (R16).** `LabCalorNoSoltado` cuenta la mitad que la sordina no suelta.
  En la carbonera 20×20: **no soltado 162 500, de eso volvió como carbón 112 200 y se perdió
  50 300** como gas sin quemar, que es lo que hace una combustión incompleta de verdad.
· **EL DESAGÜE: LOS DOS BUGS CERRADOS Y UN VEREDICTO INCÓMODO.** La «salida por la mitad baja del
  labio» de R137 no podía drenar (un poroso solo suelta agua a un vacío al saturar, y al labio
  solo le llega capilaridad lateral) y además se derrumbaba: la grava es polvo y `ProcessPowder`
  la deslizaba en diagonal al aire de la boca. Corregido con labio entero de roca y el conducto
  ATRAVESANDO la solera hasta asomar a la boca: **grava 11/11 en su sitio a los 300 s** y
  **solera 34/34 arcilla** (antes el conducto ciego la ablandaba). Pero medido con y sin
  conducto, un lecho anegado a 255 baja a 30 en 300 s **exactamente igual** y el exudado es el
  mismo: el lecho se vacía por el aire mucho más deprisa de lo que la grava conduce. El conducto
  es correcto, inocuo y **decorativo**. Escalado como Q12 con mi propuesta de dejarlo, sin afinar
  `permGrava` contra un banco que no reproduce el caudal del alambique.
· **Textos que el código desmentía**, corregidos en el benchmark de R137: la causa inventada de
  los 466 s de la tolva (el consumo no depende de la temperatura), el carbón agotado que va a
  BRASA y no a ceniza, la identidad que da **555** raw por celda ahogada y no 560 y que es
  ESTADÍSTICA y no por construcción, y el comentario de `Grava` («no desliza de lado») que era
  una promesa sin línea que la cumpliera — `ProcessPowder` prueba la diagonal sin mirar
  `fluidity`, que solo leen los líquidos.
· **Persistencia**: `_libro.json` guarda ahora los quince contadores del fuego, y
  `EscribirDefaultsSiFalta` reescribe cuando el registro tiene más claves que el archivo (antes
  un `_defaults.json` viejo seguía diciendo `vidaHumo` 400 para siempre — el archivo que dice
  cuáles son los valores de fábrica era el único que no se enteraba de que la fábrica cambió).
· **B-F3 repetida**: pico de carbón **25,0 %** y la identidad de **+5,5 % a +2,6 %**, porque el
  hogar ya no enciende por azar. Agua con **residuo 0** a los 50, 150, 300 y 600 s. Coste
  **1,90 / 1,91 ms/tick**, sin regresión. 96 parámetros.
· **El tamaño real de la costura, contado y puesto en el HANDOFF §2**: 18 líneas en
  `ProcessCombustion`, 4 en `ProcessBrasa`, 3 en `ProcessFire` (el «6 líneas» original se quedó
  corto tres rondas seguidas). `TryIgnite`, `AddTemp` e `InjectHeat` intactos.
· Benchmark `2026-09-04_r139_hf5b_honestidad.md`, CHECKPOINT D35-D36 y §9, buzón R15b-R17b y Q12,
  HANDOFF HF5b y §2. Verificado en banco headless, sin Play. **Después de esto, física nueva
  CONGELADA**: sigue H5 (banco) → H7 (jugando) → H8 (informe); H6 documentado y congelado para su
  propia etapa. Sin push (cmd 139).
