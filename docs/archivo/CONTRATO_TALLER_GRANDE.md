# CONTRATO — EL TALLER GRANDE (Playtest 27)

Encargo principal: OPUS 5 CON OJOS. Cesar, textual: *"quiero que te apoyes en
Opus 5, permitiendo que ÉL VEA — no que tú veas y le des el prompt — porque su
capacidad de construir cosas bonitas y útiles es mejor que la tuya"*. Este
contrato fija OBJETIVOS y LÍMITES; las decisiones visuales y de composición
son tuyas, Opus, y las validas MIRANDO tus propias capturas de la build real,
iterando hasta que estén bellas y claras. No entregues nada que no hayas
visto con tus ojos.

## 1. EL VEREDICTO DE CESAR SOBRE LAS MÁQUINAS ACTUALES (playtest 26, literal)

- Crisol: "no sé por qué dice 'cargadme combustible'; no entiendo por qué hay
  una N brillando — me hace pensar que ya tiene combustible y que algo está
  prendido; mucho menos sé dónde poner el combustible; peor aún, ahí no cabe
  nada; y más grave: yo al inicio NO TENGO combustible... sin embargo ése es
  el mensaje más persistente."
- Prensa: "se ve horrible; el embudo diminuto y horrible FLOTANDO, que además
  parece la boquilla de entrada en referencia a la primera máquina, pero
  no... cuando descifras que tienes que tirar el material dentro de su
  cajita, sin recibir ningún feedback, su capacidad es tan pequeña que es
  imposible que no desborde la cantidad más pequeña que puedo tener."
- Columna: "si se le puede llamar así a esa escalera sin terminar, es
  inentendible; aún no consigo saber qué hace; no tiene un verbo; sus
  materiales reaccionan con otros" (¡los muros son Crystal, que ES reactivo
  con el Azoth del núcleo de leyes — error real, cámbialos!).
- Banco de chispa: "aún más pequeña, como si hacerlo difícil de entender
  fuera tu objetivo... otro embudo feo que no es boquilla y sin capacidad."
- Ensayo: "jajaja esto ya es lamentable, lo mismo con el embudo feo."
- Y el crisol por dentro: "seca, tuesta y no sé qué más, pero todo lo hace
  RÁPIDO, y cada vez que le tiro limo saco 4 cosas de colores que me
  aturden... si me salen 4 cosas casi de golpe no entendí nada."

## 2. MANDATOS (no negociables)

1. **TODAS las máquinas al menos 6 VECES MÁS GRANDES** (en huella y en
   presencia). El cuarto puede crecer lo que haga falta: `CuartoX0` puede
   bajar hasta 140 y `CuartoY1` subir hasta 240 (el pasillo y la Tolva a la
   derecha no se tocan). Amplía el plano con criterio: aire entre estaciones,
   cada una un EDIFICIO pequeño, no una cajita.
2. **Entrada y salida inconfundibles y CON CAPACIDAD REAL**: dónde se vierte
   (boca generosa, la geometría misma embuda la materia hacia la cámara — un
   embudo TALLADO en piedra con paredes diagonales, no un sprite flotante) y
   dónde reposa el resultado (cámara/bandeja amplia, capaz de contener una
   ración entera de 45 celdas sin desbordar). PROHIBIDO el embudo decorativo:
   si una máquina no recibe por vertido a cámara (la prensa, el banco, el
   ensayo reciben DEPOSITANDO en su lecho/bandeja abierta), no lleva embudo —
   lleva una BANDEJA ABIERTA amplia y enmarcada. El embudo del 26 era mentira
   tres de cuatro veces: eso mató la gramática.
3. **Feedback al recibir y al trabajar**: cuando la materia entra donde debe,
   la máquina lo ACUSA (destello del marco, sonido si existe el canal); el
   pulso de `MaquinariaSprites.AffordanceGlow` queda RESERVADO para "estoy
   trabajando" (su const `ProximidadActiva` ya está en false — puedes
   reconectar la clase a "hornada en curso/prensada/análisis", ése es su
   destino aprobado por Cesar).
4. **EL CRISOL POR HORNADAS** (el cambio de causalidad, diseño cerrado):
   - El brasero arranca FRÍO Y VACÍO, visualmente apagado. Nada de "cargadme
     combustible" como mensaje por defecto: el crisol tiene su fuego bajo
     propio (`CrisolTier0Raw`) y su rótulo en reposo lo dice sin pedir nada
     ("fuego bajo · vierte y prueba" o mejor, tuyo).
   - UNA transformación por hornada, NUNCA cascada: cargas la cubeta, la
     hornada corre a ritmo VISIBLE (~8-12s, con progreso que se ve: brillo
     que sube, burbujeo), produce SU resultado y el crisol REPOSA — el
     resultado queda en la cubeta, intocado, hasta que el jugador lo recoge
     (o lo saca y lo vuelve a meter para el siguiente paso: recoger-y-volver-
     a-pasar es EL gesto del juego, decisión de Cesar).
   - **Extracción del limo por TEMPERATURA, una base por hornada**: cada base
     gana `ExtraccionRaw(baseIdx)` (bandas ascendentes por seed); una hornada
     de limo produce SOLO la base más alta cuya banda quepa en la temperatura
     actual del crisol. Con fuego bajo siempre sale la primera (la del
     combustible garantizado del solver); las demás exigen combustibles
     mejores. La intuición de Cesar ("pensé que estaría en relación al nivel
     de combustible, siendo que algunos llegan a temperaturas más altas") ES
     el diseño: confírmasela. Actualiza el solver de Universe para que la
     escalera hervir→calcinar→combustible→extraer-más-bases→fundir siga
     GARANTIZADA en toda seed, y el log de seed lo siga afirmando.
   - Cantidades LEGIBLES: una ración de limo (45) rinde ~sus 45 celdas de UNA
     base, no confeti de 4 colores a 9 celdas cada uno.
5. **La columna con verbo y muros inertes**: muros de PIEDRA (inmune a leyes
   y al cincel — ya registrada en ObraDelTaller) con VIDRIO VISUAL delante
   (sprite translúcido tuyo, sortingOrder tras el mundo pero delante del
   fondo), grande, con base-tanque donde verter líquidos y boca superior
   donde dejar caer muestras. Su verbo visible: OBSERVAR (que el estar cerca
   muestre una línea sobria tipo "columna de ensayo — deja caer y observa",
   estilo rótulo de foco existente).
6. **Textos en ESPAÑOL LATINO NEUTRO (tuteo)**: nada de "cargadme/dejadlo/
   os/vuestro/tostad". "carga", "deja", "te", "tu". Vale para TODOS los
   textos de tus archivos.
7. **Conserva intactos**: las firmas Init existentes (Bootstrap no cambia
   salvo si necesitas pasar una referencia nueva — permitido, documenta), el
   registro anticincel (`SimLevelBuilder.RegistrarObra` en cada Tallar*,
   ajusta los rects a las huellas nuevas), la RACIÓN de los caños, el caño
   SIN estirar (la separación de los dos chorros de agua/limo la resuelves
   con GEOMETRÍA de la estación de fuentes: bocas a distinta X por ménsulas
   de piedra, pilas generosas — sin deformar el sprite del grifo), la API
   pública de Universe del CONTRATO_PERSISTE §3 (puedes AÑADIR
   `ExtraccionRaw`, no romper lo existente), reglas de CLAUDE.md (léelo:
   sales con cast, cero allocs, IMGUI, español en comentarios, regla 15).
8. **Cero carteles explicativos en el mundo**: los rótulos de foco (E) y las
   chapas de estado siguen; la FORMA explica el resto.

## 3. ARCHIVOS TUYOS (nadie más los toca esta ronda)

`Game/MaquinariaSprites.cs`, `Game/Crisol.cs`, `Game/Prensa.cs`,
`Game/BancoChispa.cs`, `Game/EnsayoMaestro.cs`, `Game/Dispenser.cs` (solo si
la estación de fuentes lo exige), `Game/AlkahestGameBootstrap.cs` (solo
wiring), `Sim/SimLevelBuilder.cs`, `Sim/Universe.cs` (extracción + solver),
`Sim/SimStepper.cs` (solo el caso Limo de ProcessLiquid, que probablemente
QUIERAS RETIRAR: con la hornada, la separación pasa a ser acto del crisol,
no física del mundo — decide y documenta). NO toques: HintSystem, OrderSystem,
JournalHud, DayCycle, Order, Hornada, SubstanceKnowledge (otro agente les
pasa el barrido de español latino EN PARALELO — no habrá conflicto si no los
abres).

## 4. TU FLUJO DE TRABAJO CON OJOS (obligatorio)

1. Lee CLAUDE.md entero, este contrato, docs/HANDOFF.md secciones Playtest
   25-26, y el código actual de tus archivos.
2. Diseña y escribe la primera versión completa en el sandbox
   (/home/claude/alkahest).
3. DESPLIEGA al PC de Cesar: `zip` de TUS archivos → `SendUserFile` (guarda
   el file_uuid EXACTO del resultado) → `mcp__remote-devices__device_commit_files`
   a `C:\JuegosUnity\UnityAI_Test\Alkahest\_deploy_opus.zip` →
   `mcp__remote-devices__device_bash`: `cd "$HOME/mnt/UnityAI_Test/Alkahest" && python3 -c "import zipfile; zipfile.ZipFile('_deploy_opus.zip').extractall('.')"`.
   (Carga las herramientas remote-devices y computer-use por ToolSearch en
   UNA llamada al empezar.)
4. COMPILA Y MIRA: `Unity_ManageMenuItem` "Assets/Refresh" → espera 40s →
   `Unity_GetConsoleLogs` (errores) → menú "Alkahest/1. Generar escena Lab" →
   `Unity_ManageEditor` play. Para VER: computer-use — pide acceso a Unity
   (`computer_resolve_access` → `computer_request_access`; Cesar está delante
   y aprueba). Captura pantalla; clic en "Entrar al taller"; doble clic en la
   pestaña Game para maximizar; WASD vuela (el aprendiz atraviesa piedra: es
   espectral por diseño), E interactúa con lo más cercano, clic izquierdo
   MANTENIDO aspira (usa computer_left_mouse_down/up con 2s), clic derecho
   vierte, F3 abre la paleta dev (pinta cualquier material — úsala para
   probar rápido combustibles y bases sin farmear).
5. MIRA la captura CON OJOS DE CESAR ("público de a pie"): ¿sé dónde va la
   materia sin que nadie me lo diga? ¿la máquina parece un aparato bello o
   una cajita? ¿el resultado se aprecia? Itera: corrige → redespliega → mira
   otra vez. MÍNIMO dos ciclos completos de mirada; termina solo cuando las
   cinco estaciones + fuentes pasen tu propia mirada crítica.
6. Al terminar: `Unity_ManageEditor` stop. Deja el sandbox con tu versión
   final (yo documento e integro). Reporta: qué construiste por estación,
   decisiones de diseño, números finales de constantes, qué viste en cada
   iteración y qué cambiaste por ello.

## 5. PRESUPUESTO DE GUSTO (de director a director)

El idioma visual del proyecto es latón + carboncillo + piedra sobre
oscuridad, sprites procedurales píxel a píxel. No lo abandones: elévalo. Un
crisol panzudo con remaches y un fuego interior que se VE respirar; una
prensa con un tornillo que gira de verdad al prensar; vidrio con brillo
diagonal; un pedestal de examen con dosel. Detalles pequeños, siluetas
GRANDES. Si dudas entre bonito y claro, gana claro — pero casi siempre se
puede tener ambos.
