# MAPA DEL MOTOR — lector `gallery`

*(Generado por un agente lector el 2026-09-03 sobre el HEAD 371dea4. Referencias archivo:línea. Es contexto, no dogma.)*


## Resumen

END-TO-END WIRING OF THE EXISTING STYLE GALLERY (ModoGaleria), the template for a ModoLaboratorio.

FLAG. `AlkahestGameBootstrap.ModoGaleria` is a public static bool (Assets/Alkahest/Game/AlkahestGameBootstrap.cs:120), sibling of `ModoSemillaCero` (:84) and `ModoFundacion` (:105). Rule R59: the flag must be reset on every path. Resets today: DayCycle title buttons PRÓLOGO (:1028) and SEMILLA CERO (:1045), "Nuevo universo" (:1567), Bootstrap when entering the MULTI scene (:388), SimSync host/guest/despawn (Net/SimSync.cs:355, :448, :992). NOT reset by the "MODO CAÓTICO" button (DayCycle.cs:1071-1076), the provisional "2" button (:1033-1038), "Reintentar mismo universo" (:1555) nor by `VolverAlTitulo` (:766-776, plain scene reload) — a laboratory flag added the same way must be added to all those sites.

TITLE ENTRY. DayCycle.DrawTitle draws a small dim button "galería de estilo (banco de imagen, dev)" (DayCycle.cs:1053-1059): sets ModoGaleria=true, the other two false, then `RestartRun((int)Universe.SemillaCero)` (:859: sets `AlkahestSim.NextRunSeed`, `_skipTitleOnLoad=true`, reloads the active scene by buildIndex). No Editor menu item exists for the gallery: the Editor menus are "Ten Thousand Years/1, 1b, 2, 3, 4, 5, 6" (AlkahestSceneBuilder.cs:30,67; AlkahestNetSceneBuilder.cs:55,72; AlkahestBuildTools.cs:18,127; PrologoBakeTools.cs:36). The plan doc proposed "7. GALERÍA DE ESTILO (Play)" (docs/PLAN_GALERIA_DE_ESTILO.md §2) but it was never written; number 7 is free.

WORLD BUILD. `AlkahestSim.CrearMundo` picks the plane in one place (Assets/Alkahest/AlkahestSim.cs:244-246): `if (ModoGaleria) SimLevelBuilder.BuildGaleria(_grid); else if (ModoFundacion) BuildFundacion; else BuildCuartoIntimo`. Note the "línea de la verdad" log at :258-264 still prints plano=FUNDACION/CUARTO only (gallery shows as CUARTO). Universe overrides (`AplicarOverridesSemillaCero`) apply only for SemillaCero/Fundacion (:229-230), not gallery; gallery uses seed 777002 but stock chemistry.

BuildGaleria (Sim/SimLevelBuilder.cs:3060-3151): clears `ObraDelTaller`/`ReservasDelPlano`, `FillWorldStone` (every cell Stone, :3346), then a local `Sala(x0,y0,x1,y1)` lambda = `DrawSolidRect(..., Empty)` with inclusive corners. It does NOT call `PaintClimate` (ambient already AmbientRaw from CellGrid ctor, CellGrid.cs:181) nor `FillBorder` (world stone already covers edges). Coordinates are cell units, x right, y UP with y=0 the bottom bedrock row (SimStepper.cs:965 "fila 0 es siempre borde de Stone"; class docblock "y=287 techo del mundo"); world pos = (cell+0.5)*SimRenderer.CellWorldSize (0.1f, SimRenderer.cs:21).

SPAWN. Bootstrap.TrySpawn: after `MachineFocus.Limpiar()` and the three static-guard resets (:425), `if (ModoGaleria) { SpawnGaleria(); return; }` (:428) BEFORE the Fundación branch. SpawnGaleria (:1015-1040): `ObtenerOCrearBackdrop()`, `SpawnApprentice()` (frasco+cincel+mudanza+termómetro+knowledge+FlaskHud, ~:1102) re-positioned to GaleriaSpawnX/Y, `SpawnOrderSystem` (needed by DayCycle), `SpawnDayCycle(orderSystem, knowledge, null, null)` (so InputLocked wakes), `GaleriaCurador.Crear(_sim, apprentice)`, then one welcome campfire `PaintStable(110,170,2,Brasa)` + 3 Fire cells at y173 painted with `PaintCell(...,220)`. No director, no Trueque, no hints, no audio director, no HUDs.

HUD SILENCING. DayCycle.EnterCuartoIntimoSilencioso sets `HudSilenciado = ModoFundacion || ModoGaleria` (DayCycle.cs:556) and skips `GenerateOrdersPersiste` for gallery (:566). Other gallery gates: Mudanza.Init skips classic baldas/anclajes/pilas (Game/Mudanza.cs:486); WorkshopBackdrop paints the ruin backdrop without classic ironwork (Game/WorkshopBackdrop.cs:272); Flask (Game/Flask.cs:259) and Cincel (Game/Cincel.cs:209) yield clicks while `GaleriaCurador.Abierto`.

CURATOR (Game/GaleriaCurador.cs, 567 lines). Static `Abierto` (:41). Keys guarded by `UiStyles.EscribiendoTexto/JournalHud.Abierto/AlbumReal.Abierto/DayCycle.InputLocked` (:122). F8 toggles (:127); Ctrl+1..9 → `TeleportarA(i)` (:130-142, :333-345: moves apprentice AND snaps Camera.main); F10 → `RondaDeCapturas` coroutine (:144, :430-448) teleports through the 9 anchors, waits 4 frames, renders Camera.main to a 1920x1080 RenderTexture via URP `SingleCameraRequest` (:450-469) and writes `Galeria/capturas/<yyyy-MM-dd_HHmm>/area{i+1}_{name}.png`; R → hot-load `Galeria/roca_superficie.png` into `PielDeRoca.CargarTexturaDeRoca` (:205-216). `RefrescarFogatas` (:367-391) runs every 5 s: for each remembered campfire, counts Brasa/Fire vs Ash in a 5x5; if none of either it forgets it (removed by hand), if <6 embers it repaints `PaintStable(f,2,Brasa)` + 3 Fire cells at y+3. Max 64 campfires (:362). Catalog (:78-101): FUEGO fogata(Brasa stable + fire, remembered), lecho de brasas, llama suelta (Fire at raw 220 via PaintCell); MATERIA agua/arena/aceite/lodo(Limo)/ceniza/nutriente/hielo (PaintStable); ROCA piedra, piso estructural; MÁQUINAS crisol/alambique/prensa/banco de chispa (Init at classic anchor + `MoverMaquinaConFoto` :292-312 which snapshots the classic-anchor area, `Reposicionar`s to cursor, restores the snapshot with `PaintCell(..., AmbientRaw)`), placa ígnea (`HeatPlate.Init(sim,player,cx-3,cx+3,cy)`), placa gélida (`ChillStone.Init` same), "caño de agua (naciente)" (`Dispenser.Init(sim,player,cx,cy,Water)` — defaults: no order system, favor 0, unlocked, racionCeldas=0 = infinite, Dispenser.cs:314-317), balda (`Balda.Init(sim,cx-4,cx+4,cy)`), "quitar máquina (a la bodega)" (:314-331: nearest IMovible within 2.5 world units → `Reposicionar(new Vector2Int(36 + (slot%5)*52, 263))`). Left click applies every 0.06 s (:184-189); right click = `_sim.Paint(cx,cy,radius,Empty)` (:193). ESTAMPA: C+click `CopiarEstampa` (:393-410) copies a (2r+1)² patch of loose matter only (Stone and any `MaterialArchetype.StaticSolid` become Empty, :404), sets `_sel=-1`; subsequent clicks `EstamparEn` (:414-424) PaintStable each non-Empty cell centered on cursor, never erasing. Window id constant 918273 (:38); OnGUI draws ring cursor + footer only when open (:474-510).

PAINT API (AlkahestSim.cs): Paint(x,y,r,mat) :527 moves matter keeping temp; PaintCell(x,y,mat,tempRaw) :566; PaintStable(x,y,r,mat) :697 (R22/29: creation must use this; temp = StableBirthTempRaw); PaintRect :730; all refuse the 1-cell frame (x<=0||x>=W-1||y<=0||y>=H-1) and WakeChunk. Also `StepOnce()` :795 and `Paused` :66; tick loop FixedDt=1/30 with MaxStepsPerFrame=2 (:42-43, :342-355) — the only time-scaling hook today.

LEVEL PRIMITIVES (SimLevelBuilder.cs): `DrawSolidRect(grid,x0,y0,w,h,mat)` :4594 (public, InBounds-guarded); `DrawUShape(grid,x0,y0,w,h,wall)` ~:4572 (public); `FillWorldStone` :3346 (private); `FillBorder` :4549; `FillFloor` :4278; `PaintClimate` :4540; `RectObra` struct :2197; `ObraDelTaller` list :2199 (chisel protection, queried by `EsObraDelTaller` :2261); `ReservasDelPlano` :2228 / `ReservarPlano` :2231 (urbanism only); `RegistrarObra` :2237 returns handle; `ActualizarObra` ~:2255. For a laboratory plane, copy the BuildGaleria pattern: public consts + `BuildLaboratorio(CellGrid)` (name clash: a private `BuildLaboratorio(CellGrid)` already exists at :4333 for the classic workshop — pick another name, e.g. BuildLaboratorioExperimental).

GALLERY AREA TABLE (inclusive cells) and anchors (GaleriaAnclaX/Y :3054-3055): 1 cueva íntima x30-148 y168-190 (anchor 70,176; spawn 70,174); 2 patio de fuego x170-300 y165-235 + murete x226-227 y165-169 + hoyo x250-265 y162-164 (235,172); 3 la nave x170-470 y55-160 with pillars at x300(+3-4 wide) and x380/381 (320,75); 4 pared de juntas x470-477, 6 strips of 11 rows from y72 step 14: PisoEstructural, Mortero, Hormigon, VidrioVerde, Esmaltado, Clinker (462,85); 5 el pozo x530-585 y45-265, ledges 24x2 every 40 rows from y85 alternating x530/x561 (557,120); 6 poza x610-750 y68-110, water x615-745 y70-81, drip pocket x676-683 y114-117 with 3-cell leak at x680 y111-113 (680,90); 7 el vano x610-750 y150-265 (680,180); 8 pendientes x28-330 y8-50 with stair, ramp, 1-cell ledge x140-169 y30, pebble (200,20), tunnel massif x230-279 y12-37 with 6-high tunnel y8-13 (150,22); 9 terrario x360-750 y8-50, 8 cuvettes 16x12 of Water,Sand,Oil,Limo,Ash,Nutrient,Ice,Brasa from x367 step 17 (skip x500-596 for the well mouth) (450,22); BODEGA x30-300 y262-284 (slots at x36+52k, y263). Corridors (16 clear): x148-170 y168-184; x246-262 y152-170; x84-100 y50-170; x330-360 y16-34; x470-530 y55-71; x585-610 y82-98 (floor raised to y82 in R129 to stop the pool draining); x585-610 y186-202; x654-670 y110-150.

FREE SOLID-STONE REGIONS of the 768x288 gallery map (usable for a laboratory if it reuses this plane; a separate plane starts from all-Stone anyway): largest x301-529 y161-286 (229x126, right of patio/bodega, above nave, left of well); x101-169 y51-167 (69x117) and x30-83 y51-167; x30-169 y191-261 (above cueva, minus nothing); x170-300 y236-261; x610-766 y111-149 minus corridor x654-670; x586-609 y99-185; x751-766 full height; y0-7 bottom band; y266-286 above the vano; x1-27 left strip. Border cells x0/x767/y0/y287 are immutable (Paint API refuses them; IMovible.CabeEnAncla honors the same frame).


## Hechos clave

- ModoGaleria flag: Assets/Alkahest/Game/AlkahestGameBootstrap.cs:120; branch in TrySpawn :428 (`if (ModoGaleria) { SpawnGaleria(); return; }`, checked before ModoFundacion); SpawnGaleria :1015-1040
- Plane selection is a single site: Assets/Alkahest/AlkahestSim.cs:244-246 (`if (ModoGaleria) BuildGaleria; else if (ModoFundacion) BuildFundacion; else BuildCuartoIntimo`); the truth-line log :258-264 does not know about gallery (prints CUARTO)
- Title button 'galería de estilo (banco de imagen, dev)': Assets/Alkahest/Game/DayCycle.cs:1053-1059 → sets flags and RestartRun((int)Universe.SemillaCero); RestartRun :859-864 sets AlkahestSim.NextRunSeed, _skipTitleOnLoad, reloads scene by buildIndex
- HUD silenced in gallery: DayCycle.cs:556 (`HudSilenciado = ModoFundacion || ModoGaleria`); no orders generated :566
- Gallery constants: SimLevelBuilder.cs:3051 GaleriaSpawnX=70,GaleriaSpawnY=174; :3054-3058 GaleriaAnclaX/Y/Nombre arrays (9 entries); BuildGaleria :3060-3151
- BuildGaleria does not call PaintClimate or FillBorder; FillWorldStone (:3346) fills all 768x288 with Stone and rooms are carved with DrawSolidRect(...Empty) via a local Sala lambda (:3066)
- Curator: Assets/Alkahest/Game/GaleriaCurador.cs — Crear :108, Update :118-195, F8 toggle :127, Ctrl+1..9 :130-142, F10 :144, R hot-load :148/:205-216, TeleportarA :333-345 (snaps Camera.main)
- F10 capture ronda: GaleriaCurador.cs:430-448 writes Galeria/capturas/<yyyy-MM-dd_HHmm>/area{i+1}_{nombre}.png at 1920x1080 via URP SingleCameraRequest (:450-469)
- Fogatas persistentes: GaleriaCurador.cs:62-67 list (max 64, :362), RefrescarFogatas :367-391 every 5 s; forgets a campfire when 5x5 has neither Brasa/Fire nor Ash; repaints when <6 embers
- Catalog struct Colocable + 21 entries: GaleriaCurador.cs:69-101 (fogata, lecho de brasas, llama suelta, agua, arena, aceite, lodo=Limo, ceniza, nutriente, hielo, piedra, piso estructural, crisol, alambique, prensa, banco de chispa, placa ígnea, placa gélida, caño de agua (naciente), balda, quitar máquina)
- Machine placement: ColocarMaquina :227-289; classic stations Init at classic anchor then MoverMaquinaConFoto :292-312 (snapshot + Reposicionar + restore with PaintCell(...,AmbientRaw)); HeatPlate/ChillStone/Dispenser/Balda Init directly at cursor (:260-283)
- Bodega: Sala(30,262,300,284) at SimLevelBuilder.cs:3123; QuitarMaquinaCercana (GaleriaCurador.cs:314-331) parks at (36 + (slot%5)*52, 263)
- Estampa: CopiarEstampa :393-410 copies (2r+1)^2 loose matter only (Stone and StaticSolid archetypes dropped, :404), EstamparEn :414-424 PaintStable per cell, never erases
- Right click = _sim.Paint(cx,cy,radio,Empty) (GaleriaCurador.cs:193); left click rate-limited 0.06 s (:184-192); Aplicar :347-364 uses PaintStable for Estable entries, PaintCell(...,220) for fire
- Gates elsewhere: Mudanza.cs:486 (no classic baldas/anclajes/pilas in gallery), WorkshopBackdrop.cs:272 (ruin backdrop), Flask.cs:259 and Cincel.cs:209 (yield clicks while GaleriaCurador.Abierto)
- R59 resets of ModoGaleria: DayCycle.cs:1028, :1045, :1567; Bootstrap :388; SimSync.cs:355, :448, :992. Missing: DayCycle 'MODO CAÓTICO' :1071-1076, provisional '2' :1033-1038, 'Reintentar mismo universo' :1555, VolverAlTitulo :766-776
- Editor menu items present: 1 (AlkahestSceneBuilder.cs:30), 1b (:67), 2 (AlkahestNetSceneBuilder.cs:55), 3 (AlkahestBuildTools.cs:18), 4 (AlkahestNetSceneBuilder.cs:72), 5 (AlkahestBuildTools.cs:127), 6 (PrologoBakeTools.cs:36). No '7.' gallery menu exists; plan doc §2 proposed it
- Coordinate convention: cell (x,y), y=0 bottom row (SimStepper.cs:965 'fila 0 es siempre borde de Stone'; powders fall to y-1), y increases upward; world = (cell+0.5)*0.1 (SimRenderer.cs:21 CellWorldSize=0.1f; Mudanza.cs:116 'eje Y hacia arriba')
- Grid: CellGrid.W=768,H=288 (CellGrid.cs:56-57), CHUNK=16 → 48x18 chunks, PantallaW/H=256/144 (:64-65), SleepTicks=30, Idx=y*W+x (:203), InBounds (:205), temp raw: C = raw*2-120 (:193-201)
- Sim clock: AlkahestSim.cs:42 FixedDt=1/30, :43 MaxStepsPerFrame=2, accumulator loop :342-355, Paused property :66, StepOnce :795 — no time-scale multiplier exists yet
- Materials (Sim/Universe.cs MaterialId): Empty0 Stone1 Sand2 Water3 Oil4 Slime5 Steam6 Smoke7 Fire8 Ash9 Ice10 Nutrient11 Vivium12 Azoth13 CrystalSeed14 Crystal15 Acid16 Limo17 Brasa58 Mortero59 VidrioVerde60 Lejia61 Hormigon62 Esmaltado63 Clinker64 PisoEstructural65 (lines 16-107)
- Apprentice spawn helper SpawnApprentice (~Bootstrap.cs:1102) always nests at AprendizX/Y=186,154 (SimLevelBuilder.cs:2143-2144) and callers re-position; in the gallery plane that cell is nave air so no harm
- DevPalette (F3) is available in editor/dev builds regardless of mode (Dev/DevPalette.cs:138, :158 IsDevBuild) and already paints materials with PaintStable (:217) and captures PNG via ScreenCapture (:351-355)
- History: docs/archivo/HISTORIAL_RONDAS.md rounds 126-129 (lines 6607-6760) document the gallery; R129 raised the pozo↔poza corridor floor to y82 because the pool drained through it in ~1 min (SimLevelBuilder.cs:3134-3141)

## APIs / ganchos

- public static bool AlkahestGameBootstrap.ModoGaleria — Assets/Alkahest/Game/AlkahestGameBootstrap.cs:120 (add a sibling ModoLaboratorio here)
- private void AlkahestGameBootstrap.SpawnGaleria() — AlkahestGameBootstrap.cs:1015 (template for SpawnLaboratorio); branch site TrySpawn :428
- private ApprenticeController SpawnApprentice() — ~AlkahestGameBootstrap.cs:1102; private OrderSystem SpawnOrderSystem(SubstanceKnowledge) and SpawnDayCycle(OrderSystem, SubstanceKnowledge, MasterSupplies, HintSystem) near file end (~:1590-1610)
- private static WorkshopBackdrop ObtenerOCrearBackdrop() — AlkahestGameBootstrap.cs:1001
- public static void SimLevelBuilder.BuildGaleria(CellGrid grid) — Sim/SimLevelBuilder.cs:3060; public const int GaleriaSpawnX/GaleriaSpawnY :3051; public static readonly int[] GaleriaAnclaX/GaleriaAnclaY, string[] GaleriaAnclaNombre :3054-3058
- public static void SimLevelBuilder.DrawSolidRect(CellGrid grid, int x0, int y0, int width, int height, byte materialId) — :4594
- public static void SimLevelBuilder.DrawUShape(CellGrid grid, int x0, int y0, int width, int height, int wallThickness) — ~:4572
- private static void SimLevelBuilder.FillWorldStone(CellGrid) :3346; FillBorder :4549; FillFloor(CellGrid,int) :4278; PaintClimate :4540 (all private — a new Build* inside the same partial class can call them)
- public static int SimLevelBuilder.RegistrarObra(int x0,int y0,int x1,int y1) :2237; ActualizarObra(int handle,...) ~:2255; bool EsObraDelTaller(int x,int y) :2261; ReservarPlano :2231; lists ObraDelTaller :2199 / ReservasDelPlano :2228; struct RectObra :2197
- AlkahestSim (Assets/Alkahest/AlkahestSim.cs): public void Paint(int x,int y,int radius,byte mat) :527; PaintCell(int x,int y,byte mat,byte tempRaw) :566; PaintStable(int x,int y,int radius,byte mat) :697; PaintRect(int x0,int y0,int w,int h,byte mat) :730; public bool Paused :66; public void StepOnce() :795; public static int? NextRunSeed :40; Universe/Grid/Stepper/Renderer getters :52-55; Vector3 CellToWorld(Vector2Int) (used by Dispenser.cs:359)
- AlkahestSim.CrearMundo plane switch — AlkahestSim.cs:244-246 (add `else if (ModoLaboratorio) SimLevelBuilder.BuildLaboratorioX(_grid)` before Fundación or first)
- GaleriaCurador.Crear(AlkahestSim sim, ApprenticeController aprendiz) — Game/GaleriaCurador.cs:108; static bool Abierto :41 (consumed by Flask.cs:259, Cincel.cs:209)
- DayCycle: private void RestartRun(int? seed) :859; static bool HudSilenciado :143 (set at :556); DrawTitle button block :1020-1076; static bool InputLocked (used by curator :122)
- Dispenser.Init(AlkahestSim sim, Transform player, int mountCellX, int mountCellY, byte materialId, OrderSystem orderSystem=null, int favorCost=0, bool bloqueado=false, int spoutOffsetCells=SpoutOffsetCellsDefault, int racionCeldas=0) — Game/Dispenser.cs:314-317; Reposicionar(Vector2Int) :356
- HeatPlate.Init(AlkahestSim sim, Transform player, int cellX0, int cellX1, int plateRow) — Game/HeatPlate.cs:453; Reposicionar :543; ChillStone.Init same signature — Game/ChillStone.cs:518; Reposicionar :609
- Balda.Init(AlkahestSim sim, int x0, int x1, int y) — Game/Balda.cs:82; Reposicionar :115
- interface IMovible { Vector3 CentroMundo; Vector2 TamanoMundo; Vector2Int AnclaCelda; bool CabeEnAncla(Vector2Int); void Reposicionar(Vector2Int); } — Game/Mudanza.cs:41-70 (+ IMovibleAnclaEsquina :109, IMovibleSilueta :124, IMovibleEspejable :149)
- PielDeRoca.Instancia / CargarTexturaDeRoca(Texture2D) — Game/PielDeRoca.cs:44, :415 (F7 toggles the skin :174)
- DevPalette.IsOpen (Dev/DevPalette.cs:40), F3 toggle :138, IsDevBuild :158
- CellGrid: static int Idx(int x,int y) :203; static bool InBounds :205; byte GetMat(int x,int y); void SetCell(int idx, byte mat, bool resetAux=true) :~207 and SetCell(x,y,mat); const byte AmbientRaw=70 :92; static int RawToC / byte CToRaw :193-201
- Editor menu pattern: [MenuItem("Ten Thousand Years/N. ...", priority = N)] static void — e.g. Assets/Alkahest/Editor/AlkahestSceneBuilder.cs:30 (opens scene via EditorSceneManager.OpenScene LabScenePath)

## Estado por celda / chunk

- CellGrid.mat (byte[W*H]) — material id per cell; written by SetCell/Paint*, level builders (grid.SetCell), stepper; read everywhere (CellGrid.cs:~70)
- CellGrid.temp (byte[W*H]) — raw temperature 0..255 (C = raw*2-120), initialized to AmbientRaw=70 in ctor (CellGrid.cs:187); written by PaintStable/PaintCell/SetTemp API and SimStepper.DiffuseTemperature
- CellGrid.aux (byte[W*H]) — auxiliary byte: gas/fire remaining life, liquid flow memory, organic state (CellGrid.cs:~74); reset by SetCell unless resetAux=false
- CellGrid.touchedTick (uint[W*H]) — tick stamp of last processing, prevents double-processing after a swap (CellGrid.cs:~76)
- CellGrid.morph / morphScratch (byte[W*H]) — R16/42 per-cell morph field with mandatory double buffer, born from hash never 0, travels in SwapCells (CellGrid.cs:~170-176)
- CellGrid.patina (byte[W*H]) — per-cell patina/wear byte (CellGrid.cs:~170)
- CellGrid.ambient (byte[W*H]) — per-cell ambient target temperature for DiffuseTemperature; uniform AmbientRaw today (R31), kept for player-made climate; written by SimLevelBuilder.PaintClimate (:4540) and the ctor (:181)
- CellGrid.chunkSleepTimer (byte[ChunksX*ChunksY]) and chunkTouchedTick (uint[...]) — per-16x16-chunk sleep bookkeeping (SleepTicks=30); WakeChunk called by every Paint* (CellGrid.cs:~78-80, :182-184)
- GaleriaCurador._fogatas (List<Vector2Int>, max 64) — curator-side memory of campfire centers, not per-cell but the only persistent 'fire source' registry (GaleriaCurador.cs:66)
- GaleriaCurador._estampa (byte[,]) — copied material patch for stamping (GaleriaCurador.cs:58)
- SimLevelBuilder.ObraDelTaller / ReservasDelPlano (List<RectObra>) — static rect registries (chisel protection / urbanism reservation), cleared at the start of every Build* (SimLevelBuilder.cs:2199, :2228, :3062-3063)

## Constantes

- CellGrid.W=768, H=288, CHUNK=16, ChunksX=48, ChunksY=18, PantallaW=256, PantallaH=144, SleepTicks=30, AmbientRaw=70 (=20 °C) — Sim/CellGrid.cs:56-92
- AlkahestSim.FixedDt=1f/30f, MaxStepsPerFrame=2 — AlkahestSim.cs:42-43
- SimRenderer.CellWorldSize=0.1f — Sim/SimRenderer.cs:21
- GaleriaSpawnX=70, GaleriaSpawnY=174 — SimLevelBuilder.cs:3051
- GaleriaAnclaX={70,235,320,462,557,680,680,150,450}, GaleriaAnclaY={176,172,75,85,120,90,180,22,22} — SimLevelBuilder.cs:3054-3055
- Curator: WindowId=918273 (:38), default radius 2, range 0..6 (:154-155), click repeat 0.06 s (:186), fire birth temp raw 220 (=320 °C) (:356,:361), campfire refresh 5 s (:370), ember threshold <6 in 5x5 (:385), max 64 campfires (:362), remove-machine radius 2.5 world units = 25 cells (:317), bodega slot formula x=36+(slot%5)*52, y=263 (:328), capture size 1920x1080 (:454), 4 frames wait per area (:441)
- Welcome campfire: PaintStable(110,170,r2,Brasa) + Fire at (109..111,173) raw 220 — AlkahestGameBootstrap.cs:1035-1036
- Corridor clearance 16 cells (puppet is 12 tall), low tunnel 6 — SimLevelBuilder.cs:3125-3128, :3110
- Pozo↔poza corridor floor y82 (water surface y81) — SimLevelBuilder.cs:3134-3141
- Machine placement footprints: HeatPlate/ChillStone cx-3..cx+3 (7 wide, min 8 enforced internally), Balda cx-4..cx+4 — GaleriaCurador.cs:263,:269,:281; Dispenser racionCeldas=0 → infinite (Dispenser.cs:316)
- Classic plane anchors reused by the curator: CrisolX, AlambiqueX, PrensaX, BancoChispaX (SimLevelBuilder) — GaleriaCurador.cs:237-255
- Fundación seed shared: Universe.SemillaCero (777002) used by RestartRun for the gallery — DayCycle.cs:1058
- Classic plane refs: FloorHeight=10 (:391), SurfaceFloorY0=144 (:404), CuartoX0=30,X1=378,Y0=136,Y1=262 (:1068-1071), AprendizX=186,Y=154 (:2143-2144), FundacionX0=300,X1=468,Y0=140,Y1=200 (:2902-2903), FundacionAprendiz 395,150 (:3025)
- Temperature mapping: raw 0..255 ↔ -120..390 °C, C=raw*2-120 — CellGrid.cs:193-201

## Riesgos

- R59 gaps: ModoGaleria is NOT reset by DayCycle 'MODO CAÓTICO' (:1071-1076), the '2' button (:1033-1038), 'Reintentar mismo universo' (:1555) or VolverAlTitulo (:766-776). Because Bootstrap :428 and AlkahestSim :244 check ModoGaleria FIRST, a sticky flag silently builds the gallery instead of the chosen mode. A ModoLaboratorio must be reset in all 6 documented sites plus these, and the mode-priority order must be decided explicitly.
- AlkahestSim.cs:258-264 'línea de la verdad' log only distinguishes FUNDACION/CUARTO; gallery/laboratory worlds log as CUARTO — extend it or desyncs are harder to diagnose.
- BuildGaleria clears static registries ObraDelTaller/ReservasDelPlano (:3062-3063) — any second builder must do the same or stale rects protect random stone from the chisel.
- Level builders must use grid.SetCell/DrawSolidRect (construction), not PaintStable (runtime, R29); but anything that CREATES matter at runtime must use PaintStable (R22/29) or it inherits stale temp (the 'grifo sale congelado' bug, SimLevelBuilder docblock ~:120-180).
- Brasa decays to Ash by itself (SimStepper ProcessBrasa :970+); a 'permanent fire' needs a re-lighting loop like RefrescarFogatas or a HeatPlate; Fire cells have a life in aux and die.
- Water drains through any corridor at or below its surface (R129 bonus, SimLevelBuilder.cs:3134-3141): pools need corridor floors >= surface+1 or a lip; a 'permanent stream' must have a sink or it floods (1972 cells drained to the terrario in ~1 min).
- Dispenser with racionCeldas=0 is infinite (Dispenser.cs:316) — an unbounded water source without an evaporator/sink will fill the map; the classic room caps caños with racionCeldas=45 (Bootstrap SpawnCanoBasico ~:1207).
- Static per-process guards: Balda/Anclaje/Pila ResetGuardaEstatica are called in TrySpawn (:425); MachineFocus.Limpiar too. Mudanza.Init skips classic furniture only via `!ModoGaleria` (Mudanza.cs:486) — a laboratory flag needs the same gate or baldas/anclajes/pilas of the classic room spawn floating in rock.
- WorkshopBackdrop.cs:272 and DayCycle.cs:556/:566 gate on ModoFundacion||ModoGaleria — the new flag must be added or the classic ironwork backdrop, HUD and 'LO QUE PERSISTE' orders appear.
- Curator's classic-station placement relies on Init at classic anchors (CrisolX etc.) + snapshot/restore (GaleriaCurador.cs:227-312); in another plane those anchors may hit carved air or water and the snapshot region (ancla-4..+w+8, -8..+h+12) is restored with PaintCell(AmbientRaw), which would overwrite moving liquid there.
- Camera: SimRenderer follows the apprentice with smoothing; TeleportarA snaps Camera.main (GaleriaCurador.cs:344) — captures depend on that; Tab widens the view (SimRenderer.cs:551).
- No time-scale API exists: AlkahestSim runs fixed 30 Hz with at most 2 steps/frame (:43); 'time scaling' for a lab must be added in AlkahestSim.Update's accumulator loop (:342-355) — determinism is per-tick so extra ticks per frame are safe, but MaxStepsPerFrame clamps catch-up.
- Determinism: XorShift.FromCell(salt) takes uint (R21); positional noise must use a constant tick, not _tick (R16/42); any new per-tick process must be MORTAL and WakeChunk itself (R55).
- Editor auto-refresh is OFF on Cesar's machine (CLAUDE.md §5): after adding a script, call AssetDatabase.Refresh + CompilationPipeline.RequestScriptCompilation and verify the type exists before Play.
- Name clash: SimLevelBuilder already has a private BuildLaboratorio(CellGrid) at :4333 (classic workshop zone) — choose a distinct method name for the laboratory plane.
- Paint API refuses the 1-cell world frame (x<=0||x>=W-1||y<=0||y>=H-1) but DrawSolidRect/SetCell do not — level builders can overwrite border cells; keep rooms >= 1 cell inside.

## Oportunidades

- Copy-paste pattern for the new mode: flag (Bootstrap :120) → title button (DayCycle :1053-1059) → plane switch (AlkahestSim :244) → Build* (SimLevelBuilder :3060) → Spawn* (Bootstrap :1015) → curator (GaleriaCurador.Crear) → gates (Mudanza :486, WorkshopBackdrop :272, DayCycle :556/:566). Adding an Editor menu '7.' that sets the flag and enters Play is the only never-built piece (plan §2).
- GaleriaCurador can be reused almost verbatim: the anchor arrays are the only plane-specific data (TeleportarA reads SimLevelBuilder.GaleriaAnclaX/Y; RondaDeCapturas uses GaleriaAnclaNombre). Parameterizing Crear(...) with anchor arrays + capture subfolder gives a LaboratorioCurador for free (F10 → e.g. Laboratorio/capturas).
- RefrescarFogatas (GaleriaCurador.cs:367-391) is a ready-made 'permanent fire' mechanism (5 s re-light with PaintStable Brasa + Fire cells); a lab evaporator can reuse it or a HeatPlate (HeatPlate.Init at cursor already in catalog) for a steady heat source without consuming fuel.
- Dispenser.Init(sim, player, x, y, Water) with racionCeldas=0 is an infinite water source already in the catalog ('caño de agua (naciente)'); Limo (turbid water/mud) is a catalog material and a stock caño material (Bootstrap :498) — a 'permanent underground stream of turbid water' = a Dispenser emitting Limo or alternating Water/Limo.
- Terrario cuvette pattern (SimLevelBuilder.cs:3100-3112) and DrawUShape are ready for settling/sediment basins; Sand/Limo/Ash/Nutrient are all placeable and sink/settle per SimStepper powder rules.
- Steam already exists (MaterialId.Steam=6; Crisol paints it with PaintStable, Game/Crisol.cs:1806; Alambique vents vapor, Game/Alambique.cs:343) — evaporation/condensation experiments can start from Water + HeatPlate and Ice/ChillStone (both placeable) without new materials.
- AlkahestSim.Paused and StepOnce (:66, :795) plus DevPalette's existing pause/step UI (Dev/DevPalette.cs) give a base for a lab time panel; a multiplier only needs a loop-count change at AlkahestSim.cs:342-355.
- DevPalette (F3) already provides a material painter with PaintStable, hover-cell readout, copy-coords and PNG capture (:340-360) — usable inside any mode in editor/dev builds.
- PaintStable/PaintCell(temp) let a lab place hot/cold matter deterministically; CellGrid.ambient is explicitly reserved for player/lab-made climate (R31) — a lab 'ambient temperature' slider can write grid.ambient per region.
- ObraDelTaller/RegistrarObra protects rects from the chisel (SimLevelBuilder.cs:2237/:2261) — useful to make lab fixtures (stream source, evaporator floor) un-carvable, exactly as BuildFundacion protects the eternal hearth floor (:3175).
- Free stone block x301-529 y161-286 (and x610-766 y111-149) is large enough for a whole lab inside the existing gallery plane if a shared map is preferred; otherwise a fresh FillWorldStone plane gives 766x286 usable cells.
- Bodega concept (park machines out of frame, GaleriaCurador.cs:327-330) and IMovible.Reposicionar give free 'move cohesive bodies' for machines; for loose cohesive solids, R7 mentions caeSolido + cohesionCeldas in Universe (products/ice/crystal fall as bodies).
- PielDeRoca hot-load (R key → Galeria/roca_superficie.png) and F7 skin toggle work in any mode where the curator exists.
- Capture pipeline (GaleriaCurador.Capturar :450-469) is URP-safe and headless-friendly (RenderTexture, no ScreenCapture) — reuse for stress benchmarks' before/after images.

## Preguntas abiertas

- Should ModoLaboratorio reuse the gallery plane (free block x301-529 y161-286) or a fresh plane? A fresh plane needs its own anchor arrays and a curator parameterization.
- Mode priority when several static flags are true: Bootstrap and AlkahestSim check ModoGaleria first; where should ModoLaboratorio sit, and should the code reset the others on set (the title buttons do it manually)?
- Universe overrides: gallery uses seed 777002 without AplicarOverridesSemillaCero (AlkahestSim.cs:229-230). Does the lab want the author-decreed chemistry (e.g. clay solubility) or stock seed chemistry?
- Exact signature/semantics of SimStepper's Steam/condensation and Brasa→Ash timings were not read (SimStepper.cs ProcessGas/ProcessBrasa) — a follow-up reader should map them for the evaporate/condense goals.
- Does the multi scene (SimSync) need to know about the new flag beyond the three R59 reset lines (SimSync.cs:355,:448,:992)? Gallery is single-player only.
- Where should the laboratory parameter panel live: extend GaleriaCurador's IMGUI window (constant id 918273) or a sibling with its own id? Rule: constant window ids, keys with R12 guards.
- Editor menu '7.' pattern (set flag → EditorApplication.EnterPlaymode) was planned but never implemented; needs a static flag that survives domain reload into Play (statics reset on Play unless Enter Play Mode options disable domain reload) — verify how the title button avoids this (it sets the flag at runtime, not from the editor).
