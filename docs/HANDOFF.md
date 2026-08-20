# HANDOFF — ChaosAlchemy (para Opus u otro modelo que continúe)

*Escrito por Fable (orquestador) al quedar ~20% de créditos. Léelo junto a `CLAUDE.md` (raíz),
`docs/DECISIONS.md` (visión/20 decisiones) y `docs/SIM_NOTES.md` (detalle técnico de la sim).*

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
