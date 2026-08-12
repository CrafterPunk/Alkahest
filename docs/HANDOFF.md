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

## Backlog priorizado
0. **Playtest 7 (abajo)**: los 8 puntos reportados por Cesar están HECHOS en código, PENDIENTES
   de verificación en Unity por Cesar (chapas laterales de grifos, resalte dorado como sustituto
   del prompt E, rótulos de la piedra gélida, embudo de la tolva invertido + contenido que
   desaparecía, avisos repetitivos del frasco, navegación manual de pistas, retextura del fondo,
   causa raíz del etiquetado de redomas).
1. **BALANCE de la partida completa** — máxima prioridad ahora que la interacción y la
   legibilidad están arregladas. En la captura del playtest 7 Cesar iba con 149★ sobre una meta
   de 120 y la jornada SEGUÍA activa, con encargos de 150/100/220 celdas y 1:35 en el reloj: los
   tamaños de encargo son inalcanzables en ese tiempo y la condición de victoria por Favor no
   cierra la partida sola. Hay que jugar las 3 jornadas enteras y balancear tamaños de encargo,
   tiempos, ritmo de Favor y la propia meta (¿120 sigue siendo el número correcto si ya se llega
   a 149 sin que el juego termine?).
2. **Replantear las redomas** — DESPUÉS del balance. Cesar sugirió que quizás deberían estar
   abajo (más a mano) y que eso podría usarse para levantar el gameplay de reacciones; de momento
   se dejaron donde están ("las botellas tienen un lugar protagónico para su poco uso... tampoco
   está mal para esta parte del desarrollo, solo hagamos que funcione" — petición explícita de
   Cesar de NO rediseñar su ubicación todavía, solo arreglar el bug del playtest 7).
3. **Resto de M5**: glow aditivo para fuego/Vivium, agua con más cuerpo (metaballs/post-blur),
   sprite del aprendiz más expresivo, SFX simples.
4. **Verificar una build limpia de Windows** (menú "Alkahest/2. Build demo Windows" ya existe).
5. **Renombrar repo GitHub** `Alkahest`→`ChaosAlchemy` en GitHub Settings + `git remote set-url`
   (el productName ya es ChaosAlchemy; los namespaces `Alkahest.*` se quedan — decisión registrada).
6. **Integración multiplayer** (riesgo técnico nº1), plan diseñado, NO implementado:
   - Sim corre SOLO en el host. Clientes: render + input remoto (aspirar/verter/E como RPCs).
   - Estado: deltas de chunks despiertos, RLE por filas del byte mat[] (+temp cuantizada cada 4º
     tick), 10-15 Hz, ~5-30 KB/s estimado — MEDIR con `NetDiagnostics` del template antes de
     optimizar. Fallback: lockstep determinista (la sim ya es determinista por diseño: XorShift
     por (tick,x,y), sin flotantes en lógica) — requiere snapshot+replay para joins.
   - Reusar TODO el FriendsLoop: `SessionCoordinator` para lobby/transporte; el gameplay solo
     habla con él. NO rediseñar el template.
7. Ideas aparcadas: mercado de ofertas secuenciales, tamiz/filtro, más Edictos, voz (evaluada:
   NO para taller de una pantalla — ver DECISIONS §17).

## Riesgos y trampas conocidas
- El puente Cowork NO puede borrar archivos ni tocar refs de git en el FS montado → scripts .cmd.
- Los permisos de Computer Use caducan solos: presupuesta re-aprobaciones del usuario.
- El fuego se extingue solo si hay agua ENCIMA o 2+ vecinos de agua (fix intencional: el aceite
  ardiendo flota sobre agua). No "arreglar" eso de vuelta.
- SetPixels32 por chunks: hay UN buffer scratch preasignado de 16x16 y el render asume que CHUNK
  divide W y H (256x144 lo cumple; hay una guardia con LogError en SimRenderer.Init si se rompe).
- Unity a veces abre ventanas en el 2º monitor (`computer_switch_display`).

## Historial de modelos (para el informe final al usuario)
- **Fable** (orquestador): visión y DECISIONS.md, arquitectura de la sim y del loop, specs de los
  4 agentes, fixes puntuales (regla del fuego, APIs 6.5, color de llama, shimmer), todo el
  Computer Use (pruebas en editor, git, GitHub), template FriendsLoop previo completo.
- **Sonnet** (implementación): ~90% del C# — M1 sim core, M2 interacción, M3 leyes/reacciones
  (parcial, interrumpido), M4 loop completo; investigación del stack Steam (sesión template).
- **Opus**: no participó aún (la revisión visual M5 era su tarea natural — sigue siéndolo).
- **Playtest 6**: Opus 5 dirigió (diagnóstico de los seis reportes de Cesar, decisiones de diseño
  y de arte, especificación de cada fix y revisión); Sonnet 5 escribió TODO el código, en cuatro
  encargos paralelos con propiedad de archivos disjunta, más dos pases de revisión de compilación
  también con Sonnet 5.
- **Playtest 7**: Opus 5 dirigió (diagnóstico de los ocho reportes de Cesar, decisiones de diseño
  y de arte, especificación de cada encargo y revisión); Sonnet 5 escribió TODO el código, en seis
  encargos paralelos con propiedad de archivos disjunta, más dos pases de revisión de compilación
  también con Sonnet 5.

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

## Playtest 6 → ronda de arreglos dirigida por Opus 5, código de Sonnet 5 — PENDIENTE DE VERIFICACIÓN EN UNITY POR CESAR
Seis hallazgos de Cesar, uno por uno con diagnóstico y fix:

1. **Warning CS0162 en `SimRenderer.cs`**. La guardia "CHUNK divide W y H" es una comparación
   entre constantes, así que el compilador la evalúa en tiempo de compilación y marca el bloque
   como inalcanzable. No se borró (sigue protegiendo a quien cambie `CellGrid.W/H` en el futuro):
   se envolvió en `#pragma warning disable 0162` / `#pragma warning restore 0162`.

2. **"Al acabar las metas no termino el nivel"**. `DayCycle` ahora detecta con
   `OrderSystem.AllOrdersCompleted()` que todos los encargos de la jornada están entregados,
   muestra el aviso "TODOS LOS ENCARGOS ENTREGADOS · pulsa ENTER para cerrar la jornada (Ns)" y
   cierra la jornada por `EnterDayEnd()` al pulsar ENTER o al agotarse una cuenta atrás de 12 s —
   es el MISMO camino que el cierre por temporizador, no se duplicó la transición. La cuenta
   atrás se acota cada frame a `_timeRemaining`, así que nunca alarga la jornada más allá de lo
   que ya tocaba.

3. **Cámara recortando la izquierda (los grifos quedaban fuera de cuadro)**. `FitMainCamera()`
   solo encajaba la altura del mundo; ahora encaja la dimensión LIMITANTE con
   `Mathf.Max(sizeForHeight, (worldW*0.5f)/aspect)`: si el viewport es más estrecho que el
   aspecto del mundo, sobra espacio arriba/abajo en vez de recortar los lados. Además
   `RenderFrame` guarda `_lastAspect` y vuelve a llamar a `FitMainCamera()` cuando el aspecto
   cambia (pantalla completa, redimensionado de ventana).

4. **Rótulos de mundo que se encendían de lejos y no se apagaban nunca**. Sistema de TRES
   ANILLOS compartido por `ChillStone`, `HeatPlate` y `Dispenser`, con las mismas constantes en
   los tres: `RangoEstadoPleno=5.0f` / `RangoEstadoDesvanece=6.5f` (solo el ESTADO del aparato —
   grados, "abierto" — y solo si está trabajando), `RangoNombrePleno=2.6f` /
   `RangoNombreDesvanece=3.6f` (el nombre del aparato), `ProximityRange`/`RangoFoco=2.8f` (el
   prompt `E — ...`, condicionado además a `MachineFocus.EsFoco` y `!UiStyles.RatonOcupado`).
   Infraestructura nueva en `UiStyles`: sobrecarga `PlacaMundo(pos, texto, color, desplazarPx,
   float alfa)` y `Cercania(puntoMundo, jugador, rangoPleno, rangoDesvanece)` con SmoothStep.
   APRENDIZAJE: cada aparato tiene un campo de instancia `_yaConocida`; en cuanto la cercanía al
   anillo de NOMBRE llega a >=0.98 se marca, y el nombre no se vuelve a dibujar en esa partida (el
   estado y el prompt siguen apareciendo con normalidad). Sin PlayerPrefs ni estáticos: se
   resetea cada partida, deliberadamente. Se ELIMINÓ `Dispenser.RangoVisible = 7f` (existía desde
   el playtest 5, "no pude acceder a los grifos") porque ya no hace falta: el estado se ve desde
   5 u con el grifo abierto y los grifos están en columna, así que acercarse a uno revela los
   vecinos. Los textos de los grifos ya no dicen "grifo": ahora `AGUA`, `ACEITE`, `AZOTH`,
   `AGUA · abierto`, `AGUA · rebosa`, `sellado por el Maestro`. Los `Debug.Log` internos sí
   conservan "grifo".

5. **La tolva no parecía una tolva**. Se sustituyeron jambas+labio+flecha por un EMBUDO DE
   LATÓN generado por código en `DeliveryChute.SpriteEmbudoMetal` (trapecio invertido con ala de
   recogida, labio iluminado, dos filas de remaches, banda de refuerzo, cara interior degradada a
   negro, sombra proyectada), a 8 téxeles por celda y `FilterMode.Point`. Paleta latón
   RGB(168,126,58)/(214,176,96)/(86,62,28) — regla nueva: el oro de `UiStyles` es color de UI, no
   de mundo. El rótulo "TOLVA DEL MAESTRO" solo aparece cerca (3.0/4.2) y solo hasta la primera
   visita (`_yaConocida`); "vierte AQUÍ" desapareció como texto permanente, y la flecha solo sale
   si el jugador está cerca Y `Flask.Total > 0`. ANIMACIÓN DE VOLCADO de 0.55 s: sacudida
   ease-out de 0.06 u (una sola por entrega, reiniciable), garganta que se enciende de negro hacia
   `UiStyles.Exito` (verde, encajó) o `UiStyles.Aviso` (ámbar, chatarra), y un anillo de onda que
   se expande perdiendo opacidad. Fuera de la animación el embudo está QUIETO — se eliminaron los
   pulsos permanentes de alfa, que era justo la queja de Cesar. Los mensajes de resultado subieron
   de 0.5 s a 1.1 s de duración. `DeliveryChute.AsegurarJugador()` localiza al aprendiz con
   `FindAnyObjectByType<ApprenticeController>()` y cachea su `Flask`, porque
   `AlkahestGameBootstrap.Init(sim, orderSystem)` no le pasa el jugador.

6. **"Los materiales, el bedrock y los bordes de la placa parecen en baja resolución"**.
   Diagnóstico: la textura de la sim es 1 téxel por celda (256x144) estirada a pantalla completa
   (~7.5 px de pantalla por celda a 1080p), y la PIEDRA era casi un color plano, así que grandes
   áreas se leían como mancha borrosa; encima se muestreaba con `FilterMode.Bilinear`. Arreglos en
   `SimRenderer.ComputeCellColor`: para `StaticSolid`, (a) aparejo de sillería de 8x4 celdas con
   hiladas impares desplazadas medio bloque, tono ±6% estable por `Hash2D(blockX, hilada)` y
   juntas -22%; (b) iluminación de canto (la que más rinde): celda de arriba vacía +28%, celda de
   abajo vacía -20%, izquierda vacía +10% — hace que labios de cubas, bedrock y el contrafuerte de
   la tolva se lean como arquitectura tallada; (c) grano fino ±4 con `Hash3D(x,y,97)`. Para
   `Powder`, aclarado de canto superior +15%. NO se tocó la ruta de color del FUEGO ni el
   shimmer/superficie de líquidos (ambos ya validados por el jugador en playtests anteriores).
   `filterMode` cambiado a `FilterMode.Point` — decisión de arte: coherencia con los sprites de
   maquinaria, que ya eran Point; queda comentado en el código cómo revertirlo. En
   `MaquinariaSprites` se añadió `Escala=3` + helper `S(v)`: las texturas pasan de ~2-4 téxeles/
   celda a ~8-14, sin cambiar ningún tamaño de mundo (`CrearCapa` calcula `localScale` a partir de
   anchoMundo/altoMundo, así que es autocorrectivo) ni ninguna firma pública. Excepción
   documentada: `ListonEstante(int anchoPx)` recibe su ancho ya en téxeles desde `StorageRack.cs`,
   así que sigue a ~2 téxeles/celda de ancho; subirlo exige tocar `StorageRack`.

**Preguntas abiertas para el próximo playtest**: ¿el `FilterMode.Point` gusta o se vuelve a pedir
`Bilinear`? ¿el anillo de nombre de 2.6 u se queda corto o largo?

**Reparto de modelos de esta ronda**: Opus 5 dirigió (diagnóstico de los seis reportes, decisiones
de diseño y de arte, especificación de cada fix y revisión); Sonnet 5 escribió TODO el código, en
cuatro encargos paralelos con propiedad de archivos disjunta, más dos pases de revisión de
compilación también con Sonnet 5.

## Playtest 7 → ronda de arreglos dirigida por Opus 5, código de Sonnet 5 — PENDIENTE DE VERIFICACIÓN EN UNITY POR CESAR
Ocho hallazgos de Cesar, uno por uno con diagnóstico y fix:

1. **"Las etiquetas E-abrir + el coste no están en el lugar correcto: 'cerrar' del agua está
   escrito sobre el grifo de arena"**. Los cinco grifos están en columna a 10 celdas (1 unidad de
   mundo) unos de otros, así que cualquier rótulo con desplazamiento VERTICAL desde el ancla de un
   grifo cae sobre el vecino de arriba o abajo. Infraestructura nueva en `UiStyles`:
   `PlacaMundoLateral(pos, texto, color, separacionPx, desplazarYPx, alfa, bool aLaIzquierda)`, que
   ancla la chapa a un LADO del punto en vez de encima. Cada grifo tiene ahora una chapa
   PERMANENTE en su propia fila, a la DERECHA del caño (`desplazarYPx = 0`), así que dos chapas de
   grifos vecinos son geométricamente incapaces de solaparse. Queda documentado por qué a la
   derecha y no a la izquierda como sugería Cesar: el pilar de piedra al que se atornillan los
   grifos ocupa las columnas x=1..8 del mundo (`SimLevelBuilder.TapPillarX0/X1`) y está pegado al
   borde izquierdo de la pantalla — no cabe una chapa ahí; a la derecha hay pared vacía de sobra.
   Contenido de la chapa, por prioridad: `SELLADO` / nombre en MAYÚSCULAS + coste de encender
   (`ACEITE  3★`) / `AGUA · abierto` / `AGUA · rebosa`, con alfa `0.45 + 0.55*Cercania(2.6, 5.5)`
   en reposo salvo abierto/rebosando/avisos puntuales, que van siempre a alfa 1 (información
   urgente a cualquier distancia).

2. **"Indicarle al jugador todo el tiempo que necesita presionar la E es cansado y estorba;
   quizás solo la primera vez, y luego una señal, un contorno de resalte"**. El contador de usos
   vive ahora en `MachineFocus` (`MostrarPromptE`, `RegistrarUsoE()`, `UsosParaAprender = 2`,
   reiniciado en `Limpiar()` con cada partida nueva) y NO duplicado en cada MonoBehaviour: "pulsa
   E junto a un aparato" es UNA regla del juego, no una propiedad de cada máquina — en cuanto la
   usas dos veces la sabes para siempre, da igual en qué aparato fue. En su lugar, RESALTE del
   aparato enfocado en los tres aparatos con E (`Dispenser`, `ChillStone`, `HeatPlate`): una capa
   extra creada UNA vez en `BuildVisual`, DETRÁS del sprite principal (sortingOrder menor: caño 15
   < cuerpo 19/gota 20; chasis/bloque de ChillStone y HeatPlate 16 < 18/19), copia del sprite
   principal escalada (1.22x el caño; 1.15x ancho / 1.35x alto en placa y piedra) y teñida de
   `UiStyles.Oro`. Alfa 0 sin foco; con foco, late entre 0.40 y 0.85 (`0.60/0.65 + 0.20 *
   Mathf.Sin(...)` según el aparato), interpolado con `Mathf.MoveTowards` a ~6/s para que la
   entrada y salida del foco sea un fundido y no un parpadeo. La capa se anima SIEMPRE, esté la
   máquina encendida o apagada — en `ChillStone` hubo que sacar la llamada de animación de la
   rama `if (_state != State.Off)` (si no, acercarse a una piedra apagada no mostraría ninguna
   señal de que se puede interactuar con ella); en `HeatPlate` ya se llamaba en todos los frames.

3. **"'Encender el frío' quedó muy lejos de la plataforma de frío"**. `ChillStone` anclaba sus
   rótulos a `_centroBloque`, el bloque de piedra gélida EMPOTRADO BAJO EL SUELO de la bandeja, no
   a la bandeja en sí. Ahora cuelgan de `_anclaRotulo`, calculado UNA vez en `BuildVisual` a partir
   de las constantes de `SimLevelBuilder` (única fuente de verdad del plano del taller): X medio
   de `ChillTrayInteriorX0..X1`, Y en `ChillTrayY0 + ChillTrayHeight` (el labio superior de la
   bandeja). Los desplazamientos de los rótulos de estado/nombre/prompt son ahora HACIA ARRIBA
   (antes hacia abajo, con signo negativo) — hacia abajo caían DENTRO de la bandeja, que solo tiene
   0.6 unidades de mundo de profundidad interior y es justo donde el jugador aspira/vierte.
   `PuntoFoco` (el que usa `MachineFocus` para decidir si la piedra responde a E) se deja a
   propósito en `_centroBloque`: está a menos de 1 unidad del labio, muy por debajo de
   `RangoFoco=2.8`, así que acercarse a trabajar la bandeja activa la máquina igual — el bug era
   puramente visual, del rótulo, no del área de interacción. En `HeatPlate` se verificó
   explícitamente que `_centroChasis` ya estaba bien situado (el chasis se apoya en el suelo, al
   pie de la cuba, justo donde el aprendiz se planta para pulsar E) y no se tocó. La lectura de
   grados en las chapas de estado (p.ej. `HELANDO -80°`) NO se tocó: Cesar la validó explícitamente
   ("lo de las temperaturas está muy bien").

4. **"La tolva es confusa: no sé si está de cabeza, y debajo hay un hueco que no se llena,
   simplemente el contenido de mi frasco desaparece"**. DOS bugs distintos en `DeliveryChute`.
   (a) El embudo (`SpriteEmbudoMetal`) estaba literalmente invertido: en un `Texture2D` de Unity
   la fila `y=0` es la de ABAJO del sprite renderizado, pero el bucle de generación usaba
   `t = y/(h-1)` y pintaba el ancho de las ALAS en `y=0`; con el pivote del sprite en la base
   anclado al labio del pozo, el resultado era boca ancha apoyada en el pozo y garganta estrecha
   flotando por encima — un embudo al revés. Fix: `t = 1f - y/(h-1)`, y el labio iluminado +
   la fila de remaches se movieron al borde ancho (la fila alta de la textura, que ahora es
   arriba de verdad). (b) `FactorGarganta` pasó de 0.58 a 1.00 para que la garganta dibujada mida
   exactamente el ancho real del pozo excavado en `SimLevelBuilder` (22 celdas,
   `ChuteMouthX0..X1` = 216..237), sin tocar ninguna coordenada del plano; el ala de recogida
   (`FactorAlas=1.32`) sigue sobresaliendo un 32% a los lados. (c) "El contenido desaparece":
   `ConsumeTick` barría las 29 filas del pozo entero (`ChuteMouthY0..ChuteMouthY1`) y consumía la
   celda en el MISMO tick en que entraba, así que lo vertido se evaporaba pegado al labio sin
   caer nunca visiblemente. Ahora solo consume en el FONDO,
   `ZoneY0..ZoneFloorY1` donde `ZoneFloorY1 = ChuteMouthY0 + SimLevelBuilder.ChuteSillRows - 1`
   (constante nueva `ChuteSillRows = 3` en `SimLevelBuilder`, así que se traga en las filas 44..46
   dentro del pozo 44..72): lo que se vierte ahora CAE por gravedad a través del aire del resto
   del pozo antes de tragarse, y se ve caer. (d) Se añadió `SpriteConductoInterior` (degradado
   vertical gris oscuro→negro + dos costillas metálicas tenues, `FilterMode.Point`,
   `sortingOrder = -7`, entre el sprite de la sim en -5 y el fondo del taller en -10) para que el
   pozo se lea como el interior de un conducto y no como un boquete transparente sobre el fondo.

5. **"El mensaje de que no tengo algo en el frasco también estorba; a veces solo clickeas y te
   sale ese mensaje por todo el mapa"**. `Flask` lleva ahora un `Dictionary<string,int>
   _vecesMostrado` (campo de instancia, se reinicia solo con cada partida nueva): cada TEXTO de
   aviso concreto ("frasco vacío...", "demasiado lejos...", etc.) se muestra sus
   `VecesAntesDeCallar = 3` primeras veces con normalidad, y a partir de ahí se sustituye por un
   destello silencioso de `DestelloDuracion = 0.15` s (propiedad pública `DestelloIntensidad`,
   0..1, decayendo linealmente) pintado por `FlaskHud` tiñendo el borde del panel del frasco hacia
   `UiStyles.Aviso` con `Color.Lerp`. Queda razonada la decisión de diseño: una acción fallida
   DEBE tener alguna respuesta o el juego se siente roto ("hice clic y no pasó nada" dejaría de
   ser falso), pero esa respuesta no tiene por qué ser texto una vez que el jugador ya conoce el
   motivo. `SetFeedback`/el nuevo método público `Avisar` ganaron el parámetro `bool repetitivo =
   true`: por defecto los regaños de acción fallida cuentan para el silencio; `repetitivo = false`
   queda reservado para información real que sí debe mostrarse siempre (no usado aún, pero deja
   la distinción lista). El valor por defecto también es lo que mantiene compilando sin cambios
   las llamadas existentes de `StorageRack.cs` a `_frasco.Avisar(...)`.

6. **"Las pistas están bien, pero debería salir una flechita de leer siguiente para poder leer
   todas antes de ocultarlas"**. `HintSystem` navega ahora con las flechas IZQUIERDA/DERECHA
   (Input System nuevo, `Keyboard.current.leftArrowKey`/`rightArrowKey`), moviendo una pista por
   pulsación con tope en los extremos — `Mathf.Clamp`, NO cíclico: llegar al final debe sentirse
   como el final. Al primer toque de una flecha se activa `_modoManual`: la rotación automática
   por tiempo se detiene y ADEMÁS la tanda deja de caducar por tiempo (antes una tanda entera se
   ocultaba sola pasados `DuracionJornada1`/`DuracionOtrasJornadas` segundos), así que las pistas
   se quedan en pantalla hasta que el jugador las oculte a mano con H. `ReiniciarParaJornada`
   (llamado por `DayCycle` al empezar cada día) vuelve a poner el modo automático para la nueva
   tanda. Pie del panel con indicador de posición y flechas activas
   (`"◀ ▶  3/8   ·   H — ocultar consejos"`, omitiendo la flecha que no se puede usar en cada
   extremo), cacheado y reconstruido solo cuando cambia el índice mostrado o el tamaño de la
   tanda, nunca por frame.

7. **"El fondo de ladrillos púrpura es buena idea y la iluminación también, pero la textura es
   horrible y está como descuadrada"**. Diagnóstico en `WorkshopBackdrop`: la textura vivía a 1
   téxel por celda (256x144, ~7.5 px de pantalla por celda a 1080p) estirada a pantalla completa,
   con piezas de mampostería de 16x7 celdas ENORMES y planas (sin ningún detalle interior), una
   junta de 1 celda que en pantalla se veía como una banda gorda, y `FilterMode.Bilinear` mientras
   el sim y la maquinaria ya habían pasado a `Point` en rondas anteriores — fondo borroso contra
   primer plano nítido, de ahí la sensación de "sucio"/"descuadrado". Fix: `Escala = 3` (textura
   768x432, 9 téxeles por celda), `FilterMode.Point` en toda la textura, pieza de mampostería
   bajada a 10x5 celdas (30x15 téxeles) con BISEL de canto (+18% en el canto superior de cada
   pieza / -15% en el inferior) usando el MISMO lenguaje de iluminación que
   `SimRenderer.ComputeCellColor` ya aplica a `StaticSolid` en la simulación — esa rima entre
   fondo y primer plano es la clave del arreglo, no solo la resolución. Además: grano ±5%
   calculado POR TÉXEL (antes por celda, con `Hash(x,y)`), 1 de cada 8 piezas con una esquina
   desconchada (hash estable por pieza), y junta reducida a 1 TÉXEL al -55% de opacidad (antes 1
   celda entera al -75%, "rejilla dura"). Se conservan sin tocar la paleta ciruela, el degradado
   vertical, la luz de fragua, la viñeta, las vigas con ménsulas y el zócalo — todo eso ya lo
   había validado Cesar. Queda documentada la lista de constantes que había que multiplicar por
   `Escala` para que no quedaran comprimidas en la esquina superior-izquierda de la textura:
   `PiezaAncho`/`PiezaAlto`, `vigaBajaY`, `vigaAltaY`, `vigaGrosor`, `mensulaPeriodo`,
   `mensulaAncho`, `zocaloTop`. `pixelsPerUnit` del sprite pasó de 10 a 30 (proporcional a
   `TexW/worldW`) para que el sprite siga midiendo exactamente 25.6 x 14.4 unidades de mundo, solo
   cambia la densidad de téxeles por unidad.

8. **"El etiquetado parece útil pero lo etiquetado no se muestra; al colocar otra etiqueta parece
   que se sobrescribió la anterior; nombré un compuesto y apareció en el índice del frasco pero no
   sobre la botella"**. CAUSA RAÍZ ÚNICA para los tres síntomas, en `NamingUi.ResolveTarget()`.
   Las redomas del estante (`StorageRack.Redoma`) NO ocupan celdas de la simulación — son un
   mueble puramente visual que guarda `Mat`/`Cantidad` por su cuenta — pero debajo de ellas hay
   una losa de `MaterialId.Stone` que sí está simulada (`SimLevelBuilder.RackX0..RackX1`).
   `ResolveTarget` muestreaba el grid de la simulación bajo el cursor, obtenía `Stone`, lo
   descartaba por ser piedra, y se replegaba a "el material dominante del frasco" — casi siempre
   una sustancia distinta a la que de verdad guarda la redoma señalada. De ahí los tres síntomas
   exactos del reporte: la redoma se quedaba en `???` porque su material nunca se bautizaba; el
   nombre sí aparecía en el panel del frasco (que consulta el mismo diccionario `NombreDe` por
   `materialId`, pero para el material del frasco, no de la redoma); y "se sobrescribió la
   anterior" era en realidad rebautizar sin querer un material del frasco que ya tenía nombre.
   Queda documentado explícitamente que dos hipótesis alternativas se comprobaron y eran FALSAS:
   ni hay caché al guardar (`SubstanceKnowledge` es un diccionario por `materialId` consultado en
   vivo) ni el nombre se guarda "por mezcla" en vez de por material (el catálogo de materiales es
   fijo, 17 en `MaterialId`), así que el modelo de datos era correcto y no se tocó — el bug era de
   una sola línea de flujo. Fix: `StorageRack` expone `public static byte MaterialBajoCursor()` y
   `ResolveTarget` la consulta PRIMERO, antes del muestreo del grid. De paso, legibilidad: la
   cantidad de cada redoma llena se muestra SIEMPRE (indicador barato de "hay algo aquí y
   cuánto"), y el NOMBRE completo solo se dibuja en UNA redoma a la vez — la que señala el ratón,
   o si no la más cercana al aprendiz vía `UiStyles.Cercania` — truncado con "…" midiendo con
   `UiStyles.Ancho` contra el hueco real de pantalla hasta la redoma vecina (`HuecoDisponiblePx`,
   proyectado con la cámara, que en este taller es fija): hay 5 redomas repartidas en ~65 celdas
   de losa, ~13 celdas cada una, y los nombres bautizados largos no caben todos a la vez. Queda
   constancia de que Cesar pidió explícitamente NO rediseñar la ubicación de las botellas todavía
   ("las botellas tienen un lugar protagónico para su poco uso... tampoco está mal para esta parte
   del desarrollo, solo hagamos que funcione") — ver punto 2 del Backlog.

**Preguntas abiertas para el próximo playtest**: ¿funciona el resalte dorado como sustituto del
prompt "E — ..." (punto 2), o hace falta algo más explícito? ¿3 apariciones es el número correcto
antes de silenciar un aviso repetido del frasco (punto 5), o debería ser más/menos? ¿el
`FilterMode.Point` convence ahora que el fondo también lo usa (punto 7), tras la pregunta abierta
que dejó el playtest 6 sin responder?

**Reparto de modelos de esta ronda**: Opus 5 dirigió (diagnóstico de los ocho reportes de Cesar,
decisiones de diseño y de arte, especificación de cada encargo y revisión); Sonnet 5 escribió TODO
el código, en seis encargos paralelos con propiedad de archivos disjunta, más dos pases de
revisión de compilación también con Sonnet 5.
