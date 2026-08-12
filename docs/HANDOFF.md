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

## Backlog priorizado (actualizado tras playtest 11)
1. **Verificar en Unity que compila y jugar la partida entera de 3 jornadas** con todos los fixes
   del playtest 10 (ver esa sección) — sigue sin probarse en editor.
2. **Ejecutar la build de Windows y validarla con la checklist del playtest 11**: el builder ya
   regenera la escena antes de compilar y deja el resumen en consola + diálogo — falta EJECUTARLO
   y pasar el `.exe` por la checklist (`docs/HANDOFF.md` sección "Playtest 11").
3. ~~Confirmar con una captura nueva que el rótulo del frío quedó bien~~ **RESUELTO (playtest 11)**:
   Cesar lo confirmó jugando — "el rótulo de frío quedó muy bien".
4. **Enganchar `HintSystem.PistasMostradas` en la sección PROCEDIMIENTOS del diario**
   (`JournalHud`): la API ya existe, escrita en paralelo en el playtest 10, pero nadie la consume
   todavía.
5. **Decidir el destino del audio M5**: ¿se queda o se apaga con `SistemaActivo = false` en
   `DirectorDeAudio`? Depende de feedback de Cesar.
6. **CURVA DE PROGRESIÓN — jornadas cortas de una mecánica cada una** (playtest 11 §4): no hay
   onboarding, es diseño y no un ajuste de números. Ver detalle en la sección "Playtest 11".
7. **Renombrar repo GitHub** `Alkahest`→`ChaosAlchemy` + `git remote set-url` + `productName` en
   ProjectSettings (los namespaces `Alkahest.*` se quedan — decisión registrada).
8. **Replantear las redomas** (`StorageRack`): Cesar sugirió que quizás deberían ir abajo, más
   accesibles, para levantar el gameplay de reacciones/experimentación.
9. **Resto de M5**: glow aditivo fuego/Vivium, agua con más cuerpo (metaballs/post-blur).
10. **Multiplayer (riesgo técnico nº1)**: plan diseñado, NO implementado:
   - Sim corre SOLO en el host. Clientes: render + input remoto (aspirar/verter/E como RPCs).
   - Estado: deltas de chunks despiertos, RLE por filas del byte mat[] (+temp cuantizada cada 4º
     tick), 10-15 Hz, ~5-30 KB/s estimado — MEDIR con `NetDiagnostics` del template antes de
     optimizar. Fallback: lockstep determinista (la sim ya es determinista por diseño: XorShift
     por (tick,x,y), sin flotantes en lógica) — requiere snapshot+replay para joins.
   - Reusar TODO el FriendsLoop: `SessionCoordinator` para lobby/transporte; el gameplay solo
     habla con él. NO rediseñar el template.
11. Ideas aparcadas: mercado de ofertas secuenciales, tamiz/filtro, más Edictos, voz (evaluada:
    NO para taller de una pantalla — ver DECISIONS §17).

## Riesgos y trampas conocidas
- El puente Cowork NO puede borrar archivos ni tocar refs de git en el FS montado → scripts .cmd.
- Los permisos de Computer Use caducan solos: presupuesta re-aprobaciones del usuario.
- El fuego se extingue solo si hay agua ENCIMA o 2+ vecinos de agua (fix intencional: el aceite
  ardiendo flota sobre agua). No "arreglar" eso de vuelta.
- SetPixels32 por chunks: hay UN buffer scratch preasignado de 16x16 y el render asume que CHUNK
  divide W y H (256x144 lo cumple; hay una guardia con LogError en SimRenderer.Init si se rompe).
- Unity a veces abre ventanas en el 2º monitor (`computer_switch_display`).

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
