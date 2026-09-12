# CRÍTICA · «Primera piedra» (cuerpo) · lente: ITERACIÓN HUMANA OCULTA

*(Panel de direcciones, segunda pasada, 2026-09-12; versión de la reanudación del workflow. Crítico:
productor que ha visto morir sistémicos indie en el playtest infinito. Único tema: cuánta iteración
humana esconde esta dirección (playtest, tuning, balance, contenido de autor, contingencias ante
construcciones arbitrarias del jugador) y qué parte se sustituye por banco headless, hashes,
generación y validación automática. Leído entero: `cuerpo.md`, `01_LEYES.md` (con §6),
`panel/leyes/cuerpo.md` (C1-C4), las refutaciones de `humo-respirado` (origen del «tapón»),
`03_MERCADO.md`, `00_ENCARGO_Y_CRITERIO.md`, las dos críticas hermanas de esta dirección
(`ingenieria-evidencia`, `simulacion-es-el-juego`) y la de `campana-situaciones` desde esta misma lente
para calibrar la escala. Código: `Game/Flask.cs` (`EsAspirable` :520-531, `ReachWorld` :107),
`Game/ApprenticeController.cs` (`CajaChoca` :905-945, caja :796-798, modos :643-667, paquete de
plataformas :672-689), `AlkahestSim.cs` (acumulador a ×10 :372-399), `Sim/SimStepper.cs` (26 lecturas de
`MaterialId.Empty` y 6 de `MaterialArchetype.Empty` :1080-1211/:1831; `reposo = 0` al moverse :742),
`Sim/SimStepper.Laboratorio.cs` (visita cada 8 ticks :37/:205; `reposo++` :494/:552/:714;
`LabVecinoVacio` :303-312; `LabRespira` :1056-1059; `LabPresion` :1119-1168), `Sim/LabParams.cs`
(`ReposoMovil 3`, `CompactReposo 200`, `CompactHumMin 100`, `ErosionPct 6`, `Caudal 24`),
`Sim/LabMateriales.cs` (`EsSolidoDelMundo`, rendimientos del cincel :59-66), `Sim/LabBench.cs`
(`MontarCarbonera` :135-143; la caldera como única intervención :301-311);
`docs/archivo/HISTORIAL_RONDAS.md` R110, R113, R121, R121b, R122.)*

## 0. Veredicto: SEGUNDA RONDA (como capa que se injerta; no como dirección)

| campo | valor |
|---|---|
| **veredicto** | segunda ronda |
| **riesgo mayor (esta lente)** | El tercer paso del loop, **SUSTITUIRSE**, no está especificado para las herramientas que el jugador tiene. `Flask.EsAspirable` niega arcilla, terracota, roca suelta y todo `EsSolidoDelMundo`: el frasco solo lleva polvos, líquidos, fibra, grava, ceniza, semilla y carbón. Ninguno de los cuatro sustitutos que la dirección nombra (roca suelta, arcilla, sedimento, vidrio) se puede verter tal cual salvo el sedimento, que cae por una boca de techo y se erosiona en un arroyo. «Qué material, de dónde, cuánto y desde qué puesto» es una contingencia de autor por situación que ningún validador comprueba hoy; y detrás, el controlador de a pie sobre polvo, la única sensación que ningún banco afina |
| **iteración humana oculta** | 10-12 sesiones y 12-16 días-persona en las primeras 8 semanas, no «3 sesiones y una por cada 8-10 situaciones»: 1 de a pie, 2-3 de control sobre montón vertido (secuenciales, con Cesar), 1 de láminas del halo, 2 de «sin texto» para las tres primeras situaciones, 1 de «¿esperar sentado a ×10 es jugar?», 3-4 para llegar a 20-25 situaciones a hora 5, más la decisión humana del N mínimo de días y de la dote de materia por situación. Cero tablas de balance, cero temporadas, cero bibliotecas de puestos: lo humano es control, contingencia por situación y campaña, no física |
| **se automatiza en su lugar** | escenario «sustitución vertida a pie» en el banco (la solución del autor como guion cuerpo → verter, no como sesión); cláusula de validador «existe sustituto vertible alcanzable»; «con cuerpo» geométrico (caja ∩ región de las cláusulas, sin umbral); barrido de puestos (veredicto con el cuerpo en cada celda alcanzable); BFS a pie sobre el nivel ANTES de sentar a nadie; andador headless (la integración de `ApprenticeController` como función pura por tick); escenario «caminata sobre montón» que cuenta ticks de suelo perdido; hashes, equivalencia tapa/piedra, alcance de la brasa, coste por tick |
| **prueba que la mata** | Semana 1, banco, sin personas: máscara + guion `(tick)→(x,y)` y, ADEMÁS de «tapa humana» y «presa humana», «tapa vertida» y «presa vertida»: el guion del cuerpo seguido de lo único que el jugador puede hacer a 12 celdas y a pie (`PaintCell` de sedimento desde encima de la boca; sedimento en el canal de 4 celdas a `Caudal 24`). Muere si en dos de las tres situaciones del tutorial ningún material vertible reproduce ≥ 90 % del efecto del cuerpo sin enterrar la pila o sin que el arroyo se lo lleve antes de `CompactReposo`. Día 0, sin código: el hermano de Cesar en `SoloPies` con `ReachWorld` 1.2f en una cueva que el BFS apruebe. Semana 2, una sesión: sentado sobre la boca a ×10 cuarenta segundos; si lo que se recuerda es la espera, la mitigación estructural no existe |
| **semanas hasta evidencia para matarla** | 2 |
| **semanas hasta prototipo feo** | 5 (pared; 4,5-5 de Opus; independiente de J si las cláusulas se evalúan sobre la grilla en juego) |

## 1. Lo que compra, desde la silla del productor

Es, con diferencia, la dirección del panel con menos tuning de simulación, y lo es de verdad: la máscara
es binaria (bloquea o no), los dos números de la piel (`cCarne`, `caras`) los fija el banco, la brasa no
tiene parámetros, la vista Ojo es un velo, y las curvas que el cuerpo explota ya están medidas (HF1 para
la tapa, `DesnivelMin` para la presa, los 7/73 de R148 para la sombra). No hay temporadas ni eventos ni
economía: SOLTAR y el contador son la única capa encima de la física. Y tiene un órgano que ninguna
otra dirección de situaciones tiene: **la solución del autor puede ser un guion** `(tick) → (x, y)`
que escribe Fable y ejecuta el banco, no una sesión de Cesar. El validador por veredicto de `01` exige
«el registro del autor debe cumplir»; en las campañas ese registro lo juega una persona doce o veinte
veces; aquí lo escribe código y se rejuega gratis. Es exactamente el tipo de iteración que más mata
sistémicos indie, quitada de la mesa.

Contingencias ante construcciones arbitrarias del jugador: la máscara las absorbe. Construya lo que
construya, el cuerpo es una roca más para cinco leyes, y el veredicto lo pone la física, no una tabla.
Eso también vale.

## 2. La iteración humana que el §7 no cuenta, partida a partida

| partida | declarado | corregido | por qué |
|---|---|---|---|
| **Sustituirse** (el tercer paso del loop) | «te sustituyes por roca suelta, arcilla, sedimento, vidrio»; ninguna sesión | 1 decisión de autor por situación (material, origen, cantidad, puesto de vertido) hasta que exista la cláusula de validador de §4; 0,5-1 día por lote de 6-8 situaciones | `Flask.EsAspirable` (:520-531) devuelve falso para `Stone`, `PisoEstructural`, todo `EsSolidoDelMundo` (arcilla, terracota, hogar, roca suelta) y fuego. El jugador solo vierte polvos, líquidos, fibra, grava, ceniza, semilla, carbón. La arcilla nace del sedimento quieto y mojado 200 visitas (`CompactReposo`, 1 600 ticks, `CompactHumMin 100`, `CompactPct 2 %`); el vidrio, del horno a 200 raw en recinto (hora 5). Sobre una boca de techo el polvo cae (hecho medido: «el polvo solo cae»); tapar la carbonera vertiendo sedimento es enterrar la pila, la máquina de R135, no la tapa. En el canal de 4 celdas a 24 celdas/s el sedimento se erosiona (`ErosionPct 6`) y `reposo` vuelve a 0 con cada movimiento (:742): que la presa vertida llegue viva a `CompactReposo` es una pregunta de banco que la narración da por respondida. La prueba de la dirección compara el cuerpo con **roca suelta montada** (:135-143), que el jugador no puede poner |
| **A pie sobre polvo** («polvo en reposo es suelo») | «tres conmutadores; no toca ni uno de los números de R110-R121b» | 2-3 sesiones con Cesar, secuenciales; el banco mide, no juzga | `CajaChoca` solo bloquea `EsSolidoDelMundo` (:927). Con `reposo ≥ ReposoMovil` (3 visitas × 8 ticks = 24 ticks) el montón recién vertido no sostiene durante casi un segundo a ×1; la celda que se mueve un paso vuelve a ser aire bajo los pies; el agua que toca el montón lo licúa; y `Desenterrar` (R122) te expulsa hacia arriba de lo que te cubra, incluido lo que viertes. Los números del paquete de plataformas (R121b: aceleración 30, frenado 45, salto 2,2 u, coyote 0,12 s) se afinaron sobre ROCA en seis rondas con Cesar dentro (R110 colisión y −40 % de velocidad; R113 tocar; R118b; R121 modos; R121b «se siente muy tosco»; R122 desenterrar). Esto es el controlador sobre arena, la escalera de materia es el único modo de subir a pie, y cada clase de suelo ha costado rondas |
| **«DÍA N SIN CUERPO»** | «lo define la física: un tick con bloqueos es un tick con cuerpo» | dos decisiones humanas: ámbito y N mínimo; 1 sesión para ver si se juega el contador | Definido por `BloqueosCuerpo`, el contador se pone a cero cada vez que un penacho de vapor o una gota te rozan al cruzar la cueva por el otro extremo: en un mundo con gas y goteo el cuerpo bloquea algo casi siempre. Hace falta un ámbito (región de las cláusulas) o un umbral, y el umbral es tuning. Y si sentarse cumple la condición, sentarse ES la solución: para que no lo sea el veredicto exige N días sin cuerpo, y N es el `DiaTicks` de esta dirección, global, elegido a mano y reelegido tras cada sesión. La mitigación del §6 («el contador lo delata») solo existe si N existe |
| **Las tres primeras situaciones sin texto** | 1 sesión con 2 personas | 2 sesiones; cada fallo = reescritura secuencial | Son todo el tutorial. La primera («tapa») exige que alguien descubra sentarse sobre un agujero del que sale humo sin una palabra; la segunda, plantarse en un canal; la tercera, correr con una brasa. Una situación que no se descubre se reescribe (barato en código, secuencial en calendario) y se vuelve a probar |
| **Situaciones de hora 1 a hora 5** | «~1 sesión por cada 8-10, solo para el sin texto; la validez la da el validador» | 1 sesión por cada 4-6 (3-4 sesiones hasta 20-25 situaciones) | El validador con K perturbaciones garantiza validez, alcanzabilidad y que el cuerpo importa; no garantiza que enseñe ni en qué orden. Las situaciones de una ley y un papel (tapa, presa, sombra, techo, corcho, esponja, termómetro) son siete papeles: la dirección misma dice que a hora 5 el cuerpo «ya casi no se usa como pieza». Las de dos leyes con geología sorteada heredan la cuna, el candidato con la nota de tuning más baja del catálogo (5,5), no el 7 que la dirección declara |
| **La espera a ×10** | «segundos a un minuto; la fase de cuerpo es el prototipo» | 1 sesión, incomprimible, binaria | Es el riesgo que la dirección nombra. Si mata, no hay slider: la mitigación es estructural (penalizar volver) o no existe. Es la sesión más barata y la más importante |
| **Láminas del halo** (G6) | 1 sesión con 5 desconocidos | 1 sesión | Correcto: `LabVistaColor` (SimRenderer.Laboratorio.cs:167) recortada a 12 celdas es render existente; lo humano es si se lee |
| **Co-op «sujeta esto mientras…»** | nota 6; «nada probado» | 0 sesiones antes de la ruta A (3-4 semanas); 2+ sesiones de 2-3 personas después | El invitado no tiene stepper ni `temp[]` (`01` hecho 8/9): sin host + espejo no hay halo del invitado ni pieza del invitado. No es iteración oculta: es promesa sin evidencia posible en el prototipo feo |
| **Costura con el legado** | no aparece | 1 hora, pero hay que saberlo | `Flask.ReachWorld 6f` (:107) es constante compartida con `DeliveryChute` («deliberadamente dentro de ReachWorld», :664) y `FlaskHud`; 6f → 1.2f rompe el juego heredado del mismo ejecutable salvo que el alcance dependa del modo |

Total honesto: **10-12 sesiones, 12-16 días-persona en 8 semanas** frente a «3 sesiones y una por cada
8-10 situaciones». Sigue siendo poco para un juego; casi todo es control, contingencia por situación y
campaña, no constantes físicas. Nota real de iteración: **6**, no 7.

## 3. Lo que dice el código (lo que «1 semana, tuning 0» esconde de trabajo técnico)

- **Dos helpers, no uno, y entre 30 y 45 sitios, no 26.** `SimStepper.cs` tiene 26 lecturas de
  `MaterialId.Empty` y 6 por arquetipo (`archetype == Empty || Gas`: polvo :1080/:1105/:1127, líquido
  :1187/:1211, :1831). `SimStepper.Laboratorio.cs` añade los que deciden si el cuerpo es AIRE:
  `LabRespira` (:1056-1059, que la dirección reclama como cruce sin contarlo), la superficie de
  `LabPresion` (:1119/:1136/:1168, que además escribe agua con `SetCell` directo en :1158: la presa
  humana puede recibir agua dentro de la caja), `LabVecinoVacio` (:303-312, donde nacen el goteo y el
  vapor: el «techo humano» del alambique recibe la gota como celda interior), `LabSecarHacia` (:736),
  `LabAire` (:215) y la germinación (:703). Cada uno es una decisión binaria verificable; pero hay que
  ESCRIBIRLA, no descubrirla en el hash. Y `DiffuseTemperature` suma otra: sin identidad térmica la tapa
  humana tapa el gas y deja pasar el calor como aire. Trabajo técnico acotado; no es tuning; sí son
  tres días más de los que la dirección cuenta.
- **La caja de colisión es un octógono; la máscara tiene que ser el rectángulo.** `ChaflanCeldas 3`
  sobre 6,4 × 11,2 celdas deja una celda en la fila de los pies. Si la máscara reutiliza `CajaChoca`,
  el gas escapa por las esquinas. Con el rectángulo, la posición subcelda cubre 7 u 8 columnas: una
  boca de 3 se tapa con ±2 celdas de tolerancia (no es caza de píxel; el barrido lo confirma).
- **A ×10 el frame no es el tick.** `AlkahestSim.Update` corre hasta `2 × mult` = 20 ticks por frame con
  presupuesto en milisegundos y descarta el tiempo que no cupo (:391-393; `LabMultiplicadorReal` puede
  ser < 10). La posición escrita por frame vale para 10-20 ticks; el sello debe estampar «posición
  válida del tick a al b», no frames. Es la contradicción (a) de `01` §6 y se cierra en los tres días de
  arquitectura. Consecuencia buena: a ×10 el cuerpo es diez veces más lento que el mundo, así que
  «sentarse y acelerar» es físicamente lo mismo que «ser piedra».
- **El banco solo conoce una intervención** (la caldera, :301-311). El guion del cuerpo es la segunda y
  es la misma pieza que el sello necesita: «posición como entrada del tick» es trabajo de J
  adelantado, no coste propio de esta dirección.
- **`SoloPies` existe y se juega hoy** (R121, F6). La sesión de a pie no necesita una línea.
- **El cincel sí produce lo que el frasco no lleva**: tallar arcilla da sedimento, terracota y roca
  suelta dan grava (`LabMateriales` :59-66), con alcance propio de 22 celdas. Pero nada de eso vuelve a
  ser sólido al verterlo: la única vía de «piedra» que el jugador pone es sedimento que compacta.

## 4. Lo que se automatiza en lugar de la mano

1. **«Sustitución vertida a pie»** como escenario de banco. `MontarCarbonera` + guion del cuerpo a
   horcajadas de la boca 3 000 ticks + `PaintCell` de N celdas de sedimento desde una posición a pie a
   12 celdas + 6 000 ticks sin cuerpo; se mide carbón y celdas de sedimento dentro de la cámara. Lo
   mismo con la presa (sedimento en el canal a `Caudal 24`; se mide desnivel sostenido y el tick en
   que compacta o se lo lleva el agua). Un día. Convierte la contingencia de autor en un número.
2. **Cláusula de validador «existe sustituto vertible alcanzable».** Para cada situación, el guion del
   autor incluye el vertido; el validador exige que el veredicto cumpla con el guion completo (cuerpo
   → verter → soltar), que el material esté dentro del alcance BFS a pie y que el registro vacío
   falle. La solución del autor sigue siendo código, no sesión.
3. **«Con cuerpo» geométrico.** Último tick en que la caja intersecó la región de alguna cláusula de la
   condición. Sin umbral. `BloqueosCuerpo` queda como métrica del informe, no como juez.
4. **BFS sobre el nivel del laboratorio ANTES de sentar a nadie.** El laboratorio (768×288, cámara alta
   del alambique, techo de la carbonera en y = 223) se construyó para un aprendiz que vuela; probar
   «a pie» ahí en tres minutos es probar el nivel, no el modo.
5. **Barrido de puestos.** Correr cada situación con el cuerpo quieto en cada celda alcanzable (3 000
   ticks, ~5 s por corrida a 1,6-1,9 ms/tick) y registrar el veredicto: el mapa de «dónde ser pieza» y
   su anchura. Sustituye a la sesión «¿encuentran el sitio?» por una medida de tolerancia.
6. **Andador headless.** La integración de `ApprenticeController` (caja, chaflán, salto, sonda de suelo,
   desenterrar, polvo-suelo) como función pura por tick. El guion del autor pasa a ser TECLAS; la
   alcanzabilidad en tiempo la ejecuta el propio andador; el BFS deja de ser una aproximación que hay
   que mantener fiel a mano; el diario del sello guarda entradas en vez de posiciones. Dos días.
7. **«Caminata sobre montón».** Guion de pasos sobre sedimento vertido con agua que lo toca a mitad;
   se cuentan ticks de suelo perdido, hundimientos y desentierros. Acota `ReposoMovil` y la regla
   antes de la primera sesión de control; no la sustituye.
8. **Lo que la dirección ya pone en banco y es correcto**: nueve hashes intactos sin cuerpo,
   equivalencia tapa/piedra, presa/muro, esponja, alcance de la brasa en nueve seeds, +≤5 % por tick.

## 5. La prueba que la mata, y el orden

| cuándo | qué | quién |
|---|---|---|
| día 0 | BFS sobre el laboratorio (o cueva de 96×64 montada a mano) · el hermano de Cesar a pie (`SoloPies`, `ReachWorld` 1.2f). Si no llega a la boca en 3 minutos, «a pie» muere y la pieza sobrevive en vuelo | Fable, una persona |
| semana 1 | posición como entrada del tick + guion (3 días, es J adelantado) · máscara rectangular con dos helpers e identidad térmica decidida · tapa humana / presa humana / **tapa vertida / presa vertida** / barrido de x sobre la carbonera / nueve hashes / +≤5 % | Opus, banco |
| semana 2 | una sesión: sentado sobre la boca a ×10 cuarenta segundos con la máscara viva; verter después. Si lo que se recuerda es la espera, muere como dirección y queda como capa | Cesar + 1 |
| semanas 2-5 | piel + brasa + Ojo ∥ polvo-suelo + andador + caminata sobre montón · seis situaciones `Montar*` con cláusulas sobre la grilla, contador geométrico y sustitución en el guion | Opus, en paralelo de dos |
| semanas 4-6 | 2-3 sesiones de control sobre montón; 2 de «sin texto»; 1 de láminas | Cesar + testers |
| después | injerto en la dirección que gane (situaciones + sello); co-op solo tras la ruta A | — |

**Muere** si en dos de las tres situaciones del tutorial ningún material vertible reproduce ≥ 90 % del
efecto del cuerpo (sin enterrar la pila; sin que el arroyo se lleve la presa antes de compactar), o si
la sesión de la semana 2 dice «esperé». **No la matan** las pruebas de la propia dirección: una máscara
en los puntos de entrada ES una piedra para la entrada; el 90 % del carbón y `DesnivelMin` pasan por
construcción (lo dice la crítica de ingeniería y lo confirma el código). Valen como guardia de regresión.

## 6. Tiempos corregidos

- **Hasta evidencia para matarla: 2 semanas** (1 técnica sin personas con la sustitución vertida
  incluida; 1 sesión de espera a ×10 con la máscara viva). La dirección dice 1 porque su prueba no
  incluye el paso que puede fallar.
- **Hasta prototipo feo: 5 semanas de pared** (4,5-5 de Opus en paralelo de dos; la dependencia
  secuencial única es la posición como entrada del tick, 3 días; independiente de J si las cláusulas se
  evalúan sobre la grilla en juego y el contador vive en el stepper).
- **Iteración humana: 10-12 sesiones, 12-16 días-persona en 8 semanas.** Automatizable: todo lo de §4.
  Incomprimible: control sobre polvo, «sin texto», espera a ×10, láminas. Fuera del prototipo: co-op.

## 7. Órganos a conservar si muriera

- **El cuerpo sólido como entrada tick-estampada con guion en banco** (`Libre`/`Ocupa`, dos helpers,
  rectángulo, identidad térmica explícita): la segunda intervención del banco y la base del sello; vale
  para cualquier avatar u objeto móvil con guion.
- **La solución del autor como guion, no como sesión**, con la sustitución dentro del guion: quita las
  resoluciones humanas de cualquier campaña de situaciones.
- **La brasa viva en el frasco** (0,25 semanas, 0 parámetros) y **la vista Ojo**.
- **«Con cuerpo» geométrico** → «DÍA N SIN X» para cualquier herramienta (frasco, cincel, cuerpo).
- **El barrido de puestos** como medida automática de tolerancia de cualquier situación.
- **El andador headless y el BFS a pie**: validación de alcanzabilidad sin personas para cualquier
  dirección con avatar.
- **El escenario «sustitución vertida»**: la primera prueba de banco del proyecto que juzga lo que el
  jugador puede hacer con sus herramientas reales, no lo que el montaje pone.
- **Las K perturbaciones del guion del cuerpo** en el validador: dicen en qué ley importa el cuerpo.

## 8. Puntuaciones (rúbrica v2)

| eje | nota | por qué |
|---|---|---|
| apalancamiento_sistemico | 6 | una máscara cruza cinco leyes con curvas medidas, pero la sustitución que cierra el loop solo dispone de sedimento que compacta: la «piedra» del jugador es una y tarda 1 600 ticks |
| leyes_ejecutan_revelan_juzgan | 6 | ejecutan (pieza), revelan (halo, luz); juzgan solo con J, y «con cuerpo» y el N mínimo son decisiones de diseño |
| iteracion_humana | 6 | cero balance, cero bibliotecas, cero eventos; pero control sobre polvo, contingencia de sustitución por situación y campaña: 10-12 sesiones |
| verificabilidad_automatizable | 8 | guion, hashes, equivalencia, sustitución vertida, barrido, BFS, andador; lo no verificable es sensación y descubrimiento |
| simulacion_es_el_juego | 7 | el cuerpo es una celda gorda y el contador la única capa; el N mínimo y el orden de la campaña son reglas encima |
| onboarding_garantizable | 7 | una ley y un papel por situación, validador con perturbaciones y BFS; el «sin texto» es humano y las tres primeras son todo |
| observabilidad | 7 | halo, Ojo, replay con rayos X; nadie pinta luz hoy y las plantas son un píxel |
| tiempo_como_apuesta | 6 | sustituirse y soltar es real; la espera sentada es el riesgo y N es humano |
| multiplayer_emergente | 4 | verbo plausible, cero evidencia posible antes de la ruta A; el invitado no siente ni bloquea |
| profundidad_por_leyes_estables | 5 | prestada del sustrato; siete papeles y luego es la campaña más una ley, como la dirección admite |
| cuerpo_del_jugador | 8 | instrumento y pieza; riesgo hacia fuera; honesta al no prometer un cuerpo que sufre ni afinarlo a mano |
| identidad_comercial | 7 | «TAPA HUMANA» y «LA PRESA ERAS TÚ» cumplen las cuatro condiciones de `03`; la cápsula depende del muñeco |
| dificultad_tecnica | 7 | acotada: dos helpers sobre 30-45 sitios, identidad térmica, rectángulo, costura frame/tick, chunks dormidos, halo del invitado |

## 9. Crítica razonada

**Lo que compra.** Desde la silla del productor, «Primera piedra» es la dirección del panel que menos
tuning de simulación esconde, y no es una pose: la máscara del cuerpo es binaria, los dos números de la
piel los fija el banco, la brasa no tiene parámetros y las curvas que el cuerpo explota ya están
medidas. No hay temporadas, ni eventos, ni economía que balancear, ni biblioteca de puestos; ante
cualquier construcción del jugador el cuerpo es una roca más para cinco leyes y el veredicto lo pone la
física. Y tiene el órgano que más iteración quita de la mesa: la solución del autor de cada situación
puede ser un guion `(tick) → (x, y)` que escribe Fable y rejuega el banco, no una sesión de Cesar.

**La iteración que el §7 no cuenta.** Está en cuatro sitios, ninguno en la física. Primero, el tercer
paso del loop. La dirección dice «te sustituyes por roca suelta, arcilla, sedimento, vidrio»; el código
dice que `Flask.EsAspirable` niega todo `EsSolidoDelMundo`: el frasco no lleva arcilla, terracota ni
roca suelta. El jugador solo vierte polvos, y el polvo cae por una boca de techo: tapar la carbonera
vertiendo sedimento es enterrar la pila, la máquina de R135, no la tapa. La arcilla de la presa nace del
sedimento quieto y mojado 200 visitas, en un canal a 24 celdas/s que lo erosiona y donde cada
movimiento pone `reposo` a cero. Que la sustitución funcione en cada situación es hoy una contingencia
de autor (qué material, de dónde, cuánto, desde qué puesto) que ningún validador comprueba, y la prueba
de la dirección compara el cuerpo con roca suelta montada, que el jugador no puede poner. Segundo,
«polvo en reposo es suelo» no es un conmutador: es el controlador de a pie sobre arena, con 24 ticks de
histéresis y `Desenterrar` expulsándote de lo que viertes; los números de R121b se afinaron sobre roca
en seis rondas con Cesar dentro, y esto reabre esa clase de sesión, la única que ningún banco compra.
Tercero, «DÍA N SIN CUERPO» por bloqueos se pone a cero con cualquier penacho que te roce, y para que
sentarse no sea la solución hace falta un N mínimo global: dos números humanos que la dirección no
nombra. Cuarto, la campaña: las tres primeras situaciones sin texto son todo el tutorial y cada fallo
se reescribe en serie; de hora 1 a hora 5 hereda la cuna, el candidato con menos nota de tuning del
catálogo. Total honesto: 10-12 sesiones y 12-16 días-persona en ocho semanas, frente a tres sesiones.
Sigue siendo poco; pero es control, contingencia por situación y campaña, y la nota real es 6, no 7.

**Lo que se automatiza en su lugar.** Un escenario «sustitución vertida a pie» que mida carbón y
desnivel tras el vertido real convierte la contingencia en un número; una cláusula de validador
«existe sustituto vertible alcanzable» mantiene la solución del autor como código; «con cuerpo»
geométrico (caja ∩ región de las cláusulas) elimina el umbral; el barrido de puestos sustituye a
«¿encuentran el sitio?» por una anchura medida; el BFS corre sobre el nivel antes de sentar a nadie; el
andador headless convierte el guion en teclas y la alcanzabilidad en ejecución; y «caminata sobre
montón» acota la regla del polvo antes de la primera sesión de control, aunque no la sustituya.

**La prueba y los tiempos.** Las pruebas de la propia dirección no pueden matarla: una máscara en los
puntos de entrada es una piedra para la entrada, y el 90 % del carbón pasa por construcción. La que
mata es la sustitución vertida en dos de tres situaciones del tutorial, más una sesión sentado sobre
la boca a ×10 con la máscara viva. Dos semanas hasta esa evidencia, no una. Cinco de pared hasta el
prototipo feo. Nada de esto espera a J: la arquitectura de posición como entrada del tick es la
segunda intervención del banco y es J adelantado.

**Veredicto.** Segunda ronda, como capa, no como dirección, por la razón que el propio documento
escribe: los papeles del cuerpo se agotan en siete y a hora 5 es la campaña más una ley. Pero es la
capa de onboarding y de clip más barata que el sustrato puede tener, su evidencia cuesta dos semanas
casi sin personas, y si muere se quedan la máscara con guion, la sustitución como escenario, la brasa,
el Ojo, el andador y el contador geométrico. Lo que no debe hacerse en ningún caso: prometer «te
sustituyes por una piedra» sin haber medido que la piedra que el jugador puede verter funciona.
