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
   (derecha, x≈376-380 y≈60-80) → verifica que el pedido "inflamable" progresa y suma Favor.
   Ese es el circuito crítico sin probar.
4. Balancea lo que chirríe (cantidades de pedido vs capacidad del frasco 900, timer 6 min).

## Backlog priorizado
1. **Probar y balancear la partida completa** (jornadas 2-3: cultivo Vivium con Nutrient+calor
   templado, cristal con Azoth+frío+semilla, pedido de material bautizado; final WIN/LOSE).
2. **M5 presentación**: fondo del taller (sprite generado o gradiente con vigneta), glow aditivo
   para fuego/Vivium, mejorar sensación de agua (el sample bilinear ya suaviza; considerar
   post-shader de metaballs/blur umbralizado sobre la textura de la sim), sprite del aprendiz
   más expresivo, SFX simples. Cesar valora mucho lo visual — es el mayor gap actual.
3. **Build Windows**: crear menú "Alkahest/Build" análogo a `FriendsLoopBuildTools` (escena
   AlkahestLab index 0) y probar el .exe.
4. **Multiplayer (riesgo técnico nº1)**: plan diseñado, NO implementado:
   - Sim corre SOLO en el host. Clientes: render + input remoto (aspirar/verter/E como RPCs).
   - Estado: deltas de chunks despiertos, RLE por filas del byte mat[] (+temp cuantizada cada 4º
     tick), 10-15 Hz, ~5-30 KB/s estimado — MEDIR con `NetDiagnostics` del template antes de
     optimizar. Fallback: lockstep determinista (la sim ya es determinista por diseño: XorShift
     por (tick,x,y), sin flotantes en lógica) — requiere snapshot+replay para joins.
   - Reusar TODO el FriendsLoop: `SessionCoordinator` para lobby/transporte; el gameplay solo
     habla con él. NO rediseñar el template.
5. **Renombrar repo GitHub** `Alkahest`→`ChaosAlchemy` + `git remote set-url` (el productName ya
   es ChaosAlchemy; los namespaces `Alkahest.*` se quedan — decisión registrada).
6. Ideas aparcadas: mercado de ofertas secuenciales, tamiz/filtro, más Edictos, voz (evaluada:
   NO para taller de una pantalla — ver DECISIONS §17).

## Riesgos y trampas conocidas
- El puente Cowork NO puede borrar archivos ni tocar refs de git en el FS montado → scripts .cmd.
- Los permisos de Computer Use caducan solos: presupuesta re-aprobaciones del usuario.
- El fuego se extingue solo si hay agua ENCIMA o 2+ vecinos de agua (fix intencional: el aceite
  ardiendo flota sobre agua). No "arreglar" eso de vuelta.
- SetPixels32 por chunks: los buffers scratch están preasignados (16x16 y 16x8) — no tocar sin
  entender el layout (H=216 no es múltiplo de 16).
- Unity a veces abre ventanas en el 2º monitor (`computer_switch_display`).

## Historial de modelos (para el informe final al usuario)
- **Fable** (orquestador): visión y DECISIONS.md, arquitectura de la sim y del loop, specs de los
  4 agentes, fixes puntuales (regla del fuego, APIs 6.5, color de llama, shimmer), todo el
  Computer Use (pruebas en editor, git, GitHub), template FriendsLoop previo completo.
- **Sonnet** (implementación): ~90% del C# — M1 sim core, M2 interacción, M3 leyes/reacciones
  (parcial, interrumpido), M4 loop completo; investigación del stack Steam (sesión template).
- **Opus**: no participó aún (la revisión visual M5 era su tarea natural — sigue siéndolo).
