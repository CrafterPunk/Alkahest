# TEN THOUSAND YEARS

> *Reconstruye el conocimiento humano a partir del barro, el fuego y la observación.*

Un mundo post-apocalíptico de **ruinas amables**: la civilización anterior dejó
sus máquinas rotas — desenterrarlas y REPARARLAS es el juego. No empiezas de
cero: la materia quedó; el conocimiento se perdió.

Juego de simulación de materia (falling-sand estilo Noita) + taller de producción + co-op.
Un imp volador al servicio de un Maestro gigantesco funda un taller desde el barro: aspira y
vierte materia con su frasco, talla la roca, alimenta el crisol y descubre —forzando la materia
con fuego, prensa y agua— **qué persiste**. Todo el universo material desciende del limo
primordial; el conocimiento es la progresión.

**Motor propio**: autómata celular determinista de 768x288 celdas a 30 Hz, escrito a mano en C#
(sin f&iacute;sica de Unity en la simulación). Todo el arte actual se genera por código (greybox).

- **Visión completa**: `docs/GDD_TEN_THOUSAND_YEARS.md` (normativo).
- **Estado actual y backlog**: `docs/ESTADO.md`.
- **Detalles técnicos de la sim**: `docs/SIM_NOTES.md`.
- **Historia del desarrollo** (70+ rondas de playtest): `docs/archivo/` — consultar solo si
  hace falta el porqué de una decisión vieja.

---

## Cómo abrir y correr

1. **Unity 6.5 exacto: `6000.5.7f1`** (congelada hasta después del Steam Fest). URP, Input System nuevo.
2. Abrir el proyecto (raíz de este repo). Si Unity entra en Safe Mode y luego sale: regenerar la escena (paso 3).
3. Menú **`Ten Thousand Years → 1. Generar escena Lab (un jugador)`** — idempotente, reconstruye
   `Assets/Alkahest/Scenes/AlkahestLab.unity` desde código.
4. **Play**. Del título salen los tres modos:
   - **PRÓLOGO — la fundación**: el tutorial/experiencia inicial (en diseño activo). Repetible siempre.
   - **MODO NORMAL — SEMILLA CERO**: el juego principal, semilla de autor con arco guiado.
   - **MODO CAÓTICO — semilla libre**: procedural; universo distinto por seed.
   - `ESC` pausa y permite volver al título desde cualquier modo.

### Builds
- **`Ten Thousand Years → 3. Build demo Windows`** (un jugador) → `Builds/TenThousandYearsDemo/`.
- **`Ten Thousand Years → 4. Build MULTI Windows`** (co-op Steam, hasta 4) → `Builds/TenThousandYearsMulti/`.
- Las builds de prueba abren en **ventana 1600x900 redimensionable** (`Alt+Enter` = pantalla completa)
  y son *Development Build* (F3 = paleta dev activa). Steam muestra "SpaceWar" porque
  `steam_appid.txt` usa el appid 480 de pruebas de Valve — desaparecerá al comprar appid propio.

### Controles (resumen)
`WASD/flechas` volar · `clic izq.` aspirar · `clic der.` verter · `Q` vaciar · `Shift` aspirar todo ·
`C` cincel (izq. talla / der. construye, `X` alterna piedra/piso) · `V` mudanza de aparatos ·
`R` (manos vacías) todo a su sitio · `E` usar aparato · `J` diario · `O` encargos · `M` silencio · `F3` dev.

---

## Mapa del código (`Assets/Alkahest/`)

| Carpeta | Qué vive ahí |
|---|---|
| `Sim/` | El autómata celular determinista: `Universe` (materiales + química por semilla), `SimStepper` (reglas físicas), `CellGrid` (mat/temp/morph), `SimRenderer` (grid→textura), `SimLevelBuilder` (**el plano: única fuente de verdad de TODAS las coordenadas**), `ReactionEngine`, `LeyDelUniverso`. |
| `Game/` | La capa jugable: `ApprenticeController` (el imp), `Flask` (aspirar/verter + su juice), `Cincel`, `Mudanza`, las estaciones (`Crisol`, `Prensa`, `BancoChispa`, `ColumnaEnsayo`, `EnsayoMaestro`, `Alambique`), `FundacionDirector` (los beats del prólogo), `DayCycle` (título/pausa/jornadas), `OrderSystem` (encargos), `SubstanceKnowledge` (descubrir/bautizar), HUDs IMGUI, `MaquinariaSprites` (todo sprite se genera por código), `Capas` (tabla de sortingOrder). |
| `Audio/` | `SintetizadorSfx` (clips sintetizados por código, cero assets) + `DirectorDeAudio` (mezcla, limitadores). |
| `Net/` | Co-op: `SimSync` (sim solo-host + espejo RLE por chunks), `AprendizNet`, `MaquinaSync`/`MaquinaReplica`, lobby. Sobre el template `Assets/FriendsLoop/` (Steam/NGO — no tocar salvo integración). |
| `Dev/` | `DevPalette` (F3): pintar materiales, pausa, seed, inspección. Solo editor/dev builds. |
| `Editor/` | Generadores de escena (todo se instancia por código al arrancar) y build tools. |

## Las reglas de oro (las que rompen cosas si se ignoran)

1. **Determinismo**: en el hot path de la sim, jamás `UnityEngine.Random` ni allocs — solo
   `XorShift` sembrado por `(tick,x,y)`. Es el plan para el netcode.
2. **Crear materia vs moverla**: lo que *introduce* materia usa `AlkahestSim.PaintStable`
   (nace a temperatura estable); `Paint`/`PaintCell` solo *mueven* materia que ya lleva su
   temperatura. Confundirlos produce bugs tipo "el grifo fabrica hielo".
3. **Coordenadas**: toda posición del mundo se lee de `SimLevelBuilder`, nunca se hardcodea
   ni se copia a mano de otro plano.
4. **`morph` va a doble búfer** (`morphScratch`) en las familias que leen vecinos, o el
   resultado depende del orden de recorrido y muere el determinismo.
5. **Atajos IMGUI**: todo atajo de una tecla comprueba `UiStyles.EscribiendoTexto` (y los del
   mundo, además `JournalHud.Abierto`), o un campo de texto se los come.
6. **Nada de API de Unity en inicializadores estáticos** (envenena el tipo entero en runtime
   sin error de compilación). Centinela + carga perezosa.
7. **`BuildVisual()`/`Init()` no son idempotentes**: para mover un aparato existe
   `Reposicionar` (contrato `IMovible`). Llamarlos dos veces duplica hijos o resetea estado.
8. **Los `[SerializeField]` guardados en prefab/escena PISAN el default del código.** Los
   números de juego se afinan en código; ante "cambié el valor y no pasó nada", grep al
   `.prefab`/`.unity`.
9. **Textos de juego en español latino neutro** (tuteo, nunca "os/vosotros").
10. **Capas visuales**: todo `sortingOrder` nuevo se lee de `Game/Capas.cs`.

*(El catálogo completo de lecciones y trampas conocidas vive en `CLAUDE.md` — orientado al
agente de IA que codea el proyecto — y la crónica entera en `docs/archivo/HISTORIAL_RONDAS.md`.)*

## Flujo de trabajo actual

El código se escribe con Claude (agente IA) en un sandbox remoto, se compila ahí con un rig
fiel a Unity, se despliega a este disco y **Cesar hace el push** con un script `ca_playtestNN.cmd`
de un solo uso (doble clic: commit + push). GitHub es la fuente de verdad. Cada ronda queda
documentada en `docs/archivo/HISTORIAL_RONDAS.md`.

**Próximo gran paso** (ver `docs/ESTADO.md`): renombre estructural (namespace/escena/repo aún
se llaman `Alkahest` por legado) y **escenificación** — mover piezas generadas por código a la
escena para poder intervenir desde el editor.
