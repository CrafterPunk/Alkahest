# MAPA DEL MOTOR — lector `grid_sim_render`

*(Generado por un agente lector el 2026-09-03 sobre el HEAD 371dea4. Referencias archivo:línea. Es contexto, no dogma.)*


## Resumen

Scope: Assets/Alkahest/Sim/CellGrid.cs (312 lines), Assets/Alkahest/AlkahestSim.cs (802), Assets/Alkahest/Sim/SimRenderer.cs (2020), Assets/Alkahest/Sim/XorShift.cs (82), all read in full, plus targeted greps into SimStepper/MaterialDef/DevPalette for hook verification.

DATA MODEL (CellGrid). World is 768x288 (W/H consts, CellGrid.cs:56-57), CHUNK=16 -> 48x18=864 chunks (60-61), PantallaW/H=256x144 is the "one screen" unit (64-65). Per-cell SoA byte arrays: mat, temp, aux, morph, morphScratch, patina, ambient, plus uint[] touchedTick (71-77,116,148-150,170). Per-chunk: byte[] chunkSleepTimer, uint[] chunkTouchedTick (80-82). SleepTicks=30 (68). Temperature is raw byte with C = raw*2-120 (RawToC/CToRaw 192-200); AmbientRaw=70 (=20C) (92). `ambient[]` is a per-cell target temperature, initialized uniform to AmbientRaw (181), consumed ONLY by SimStepper.DiffuseTemperature at SimStepper.cs:2185 (a +-1 pull toward ambient[i] every 4th of the 8-phase diffusion sweeps). It was born for zonal climate (playtest 15), removed in 17, kept explicitly as the vehicle for player-made local climate — a permanent fire or cold room can simply write ambient[] in a region. SetCell writes mat, optionally clears aux, and seeds morph with a position/material hash (210-220); SwapCells swaps mat/temp/aux/morph (228-237). WakeChunk wakes the 3x3 chunk neighborhood and stamps chunkTouchedTick=tick (263-279); WakeChunkIndex wakes one chunk (288-294); TickChunkIdle increments the sleep timer (282-286). patina is renderer-only surface memory (never read by SimStepper).

ORCHESTRATOR (AlkahestSim). FixedDt=1/30, MaxStepsPerFrame=2 (42-43). Update(): if Paused return; accumulator += Time.deltaTime; while (acc>=FixedDt && steps<2) Step(); accumulator clamped to FixedDt*2 (330-362). Render only when steps>0. StepOnce() forces one Step+RenderFrame (795-800). No time-scale hook exists in AlkahestSim; DevPalette instead sets Time.timeScale (0.5/1/2/4 buttons, P toggles, Dev/DevPalette.cs:162-169,304-307). Because Time.deltaTime scales with timeScale but the loop caps at 2 steps/frame, any timeScale >~2 at 60fps silently saturates (accumulator clamped, time is dropped): the cheapest SAFE 1x/5x/10x/50x/100x control is to make MaxStepsPerFrame a mutable field (or add a `public int StepsPerFrameMultiplier`) and run N integer Steps per rendered frame — never change FixedDt (the stepper is tick-based: RNG seeded by (tick,x,y) in XorShift.FromCell, diffusion phase = tick%8, MorphTick throttles by tick bits; dt is meaningless to it). Rendering once after N steps is already what the code does (RenderFrame after the while loop). Public API: SampleMaterial(480), SampleTempRaw(487), Paint(disc, inherits temp)(527), PaintCell(one cell, material+temp)(566), PaintStable(disc, birth temp from StableBirthTempRaw)(697), PaintRect(730), InyectarTemperatura(temp only)(771), WorldToCell/CellToWorld (780-792, CellWorldSize=0.1), Grid/Stepper/Renderer/Universe getters (52-55). All Paint* skip the 1-cell world border and WakeChunk. Mirror mode (ModoEspejo) forwards Paint* to host via SimSync.

RENDERER. Three Texture2D RGBA32 768x288 Point-filtered: main `_texture` (352), `_frontTexture` (labio frontal, DISABLED by static readonly LabioFrontalActivo=false, 319), `_veloTexture` (R129 liquid veil, always on, 392). Sprites: main quad sortingOrder -5 tinted TinteGlobal (726-727), veil at sortingOrder 52 alpha VeloAlfa=115 (755, 341). RenderFrame(tick, forceFull) (820-912): runs ActualizarPatinaFranja (12 full-width rows/frame, whole world), computes visible chunk range (camera rect + ViewMarginChunks=2), and per chunk redraws only if full (every FullRefreshEveryFrames=30 frames) || chunk awake || chunk has continuous positional animation (Vetas/Celdas) || dirty-out-of-camera (chunkTouchedTick != _chunkLastRenderTick or never rendered). RenderChunk (1052-1095) fills a 16x16 Color32 scratch via ComputeCellColor, writes to _pixels and SetPixels32(x0,y0,16,16) on main+veil; Apply(false) is called once per texture only if anyDrawn (906-911) — i.e. at most 2 full-texture GPU uploads per rendered frame (3 if labio enabled). ComputeCellColor(1127-1628) layers: base color + jitter -> morph pattern (ApplyPatron) -> StaticSolid masonry/edge lighting -> powder top highlight -> emitsGlow flicker -> Organic dormant desaturation (aux&0x40) -> liquid shimmer+surface line -> morphological border -> emision -> INCANDESCENCE from temp (raw>150, quadratic-ish, additive ember + capped 0.45 amber lerp, 1574-1598) -> patina. OcultarRoca static (275) makes Stone return alpha 0 (1145); MarcarTodoSucio (278) and RepintarAhora (814) force repaint. The camera lives in this class too (FitMainCamera/UpdateCameraFollow, Tab wide view, wheel zoom, FocoCinematico, Sacudida). No overlay/debug-view hook exists: the only per-cell color path is ComputeCellColor; a debug view (temperature/humidity/flow) must either add a mode switch inside RenderChunk (cheapest: a static `DebugView` enum consulted at 1068 to replace ComputeCellColor, plus MarcarTodoSucio on toggle) or add a fourth texture/sprite following the exact `_veloTexture` pattern (Init 392-398, BuildQuad 751-758, RenderChunk 1094, Apply 910).

XorShift: xorshift32 seeded by FromCell(tick,x,y,salt) hash; NextByte/Chance(pct255)/ChancePercent/Next(max)/NextBool (XorShift.cs:24-80). Any new system must use this, never UnityEngine.Random.


## Hechos clave

- CellGrid W=768,H=288 (CellGrid.cs:56-57); CHUNK=16, ChunksX=48, ChunksY=18 (59-61); PantallaW/H=256/144 (64-65); SleepTicks=30 (68)
- Per-cell arrays: mat, temp, aux, touchedTick(uint), morph, morphScratch, patina, ambient — all W*H (CellGrid.cs:71-77,116,148-150,170,174-182)
- Per-chunk arrays: chunkSleepTimer(byte), chunkTouchedTick(uint, init uint.MaxValue) (CellGrid.cs:80-82,183-185)
- Temperature raw<->C: C=raw*2-120, range -120..390C; AmbientRaw=70=20C (CellGrid.cs:92,192-200); whole temp[] initialised to AmbientRaw (187)
- ambient[] initialised uniform to AmbientRaw (CellGrid.cs:181); its ONLY consumer is SimStepper.DiffuseTemperature at SimStepper.cs:2185 (ambientSweep = ((_tick>>3)&3)==0, +-1 raw pull toward ambient[i]); docblock (94-115) says it is kept precisely as the vehicle for player-created local climate
- DiffuseTemperature processes 1/8 of cells per tick (i = tick%8; i += 8), 4-neighbour average, step=diff/4 with min +-1, skips world border (SimStepper.cs:2153-2192)
- SetCell writes mat, resets aux (default), seeds morph=(idx*37+mat*101+13) (CellGrid.cs:210-220); SwapCells swaps mat/temp/aux/morph together (228-237)
- WakeChunk(x,y,tick) wakes 3x3 chunk neighbourhood: sleepTimer=0, chunkTouchedTick=tick (CellGrid.cs:263-279); WakeChunkIndex wakes exactly one chunk (288-294); IsChunkAwake = sleepTimer<SleepTicks (257-260)
- aux bit 0x40 = OrganicDormantAux (renderer desaturates), bit 0x80 = 'settled' for Organic in SimStepper; aux also = gas/fire life, liquid flow memory, combustion reserve (CellGrid.cs:118-126, MaterialDef.cs:150-154)
- patina[] is written/read ONLY by SimRenderer (ActualizarPatinaFranja 955-1032, ComputeCellColor 1607-1625); 0..90 = wet (currently disabled, playtest 44, lines 1005-1016), 91..220 = soot; never used by SimStepper
- AlkahestSim: FixedDt=1/30, MaxStepsPerFrame=2 (AlkahestSim.cs:42-43); Update loop 330-362: Paused early-return (332), accumulator += Time.deltaTime, while(acc>=FixedDt && steps<2) Step(); accumulator clamped to FixedDt*2 (353-356); RenderFrame(_stepper.Tick) only if steps>0 (358-361)
- StepOnce(): one Step + RenderFrame ignoring accumulator (AlkahestSim.cs:795-800); used by DevPalette 'N' key (Dev/DevPalette.cs:150,299)
- No time-scale hook in AlkahestSim; DevPalette drives Time.timeScale (0.5x/1x/2x/4x, P pause) at Dev/DevPalette.cs:162-169,298-307 — with MaxStepsPerFrame=2, any timeScale above ~2x at 60fps saturates and drops time
- SimStepper.Step order: _tick++, DiffuseTemperature, full-grid row sweep with alternating x direction per tick parity (forward=(tick&1)==0), chunk idle accounting, MorphTick; exposes ActiveCells/ActiveChunks/LastStepMs (SimStepper.cs:257-303)
- Paint(x,y,radius,mat) disc, inherits existing temp (AlkahestSim.cs:527-548); PaintCell(x,y,mat,tempRaw) (566-576); PaintStable(x,y,radius,mat) births at StableBirthTempRaw (697-721; margin StableBirthMarginRaw=10 raw, 617); PaintRect(x0,y0,w,h,mat) (730-749); InyectarTemperatura(x,y,tempRaw) temp only (771-778); all skip border cells (x<=0||x>=W-1||y<=0||y>=H-1) and call WakeChunk
- SampleMaterial(x,y) returns Empty out of bounds (480-484); SampleTempRaw returns AmbientRaw out of bounds (487-491)
- WorldToCell = floor(pos/CellWorldSize); CellToWorld = (cell+0.5)*CellWorldSize; CellWorldSize=0.1 (AlkahestSim.cs:780-792, SimRenderer.cs:21)
- World creation: CrearMundo guards double creation because SimRenderer.Init is NOT idempotent (AlkahestSim.cs:157-169); plan selection: ModoGaleria -> SimLevelBuilder.BuildGaleria (R127), ModoFundacion -> BuildFundacion, else BuildCuartoIntimo (244-246); Universe.Create(seed) then optional AplicarOverridesSemillaCero (199-229)
- Mirror mode (ModoEspejo): no stepper, TickEspejo clock at 30Hz, Paint* forwarded via SimSync.ReenviarPintura; InyectarTemperatura NOT forwarded (temp not synced) (AlkahestSim.cs:88-104,378-416,520-524,763-769)
- SimRenderer textures: _texture (main), _veloTexture (liquid veil, R129, always on), _frontTexture (labio frontal, disabled via LabioFrontalActivo=false static readonly) — all RGBA32 768x288 Point/Clamp (SimRenderer.cs:319-341,352-398)
- Sprites: main quad sortingOrder -5 tinted TinteGlobal(0.930,0.845,0.775) (SimRenderer.cs:52,721-732); veil sprite sortingOrder 52 (between Personaje=50 and ArquitecturaFrente=55), same tint, alpha VeloAlfa=115 (341,751-758)
- RenderFrame(tick, forceFull) (820-912): full refresh every FullRefreshEveryFrames=30 render calls (83,824); patina strip update first (836); visible chunk range from camera + ViewMarginChunks=2 (161,771-801); per-chunk skip unless full||awake||_chunkContinuousAnim||dirty (879-893); Apply(false) on each texture only if anyDrawn (906-911)
- Dirty-out-of-camera mechanism: _chunkLastRenderTick[ci] vs _grid.chunkTouchedTick[ci], _chunkEverRendered[ci] (SimRenderer.cs:254-272,890-891,1089-1090); MarcarTodoSucio resets _chunkEverRendered (278-282); RepintarAhora forces full render with last tick (814-818)
- RenderChunk (1052-1095): per cell ComputeCellColor -> _pixels[idx] and 16x16 scratch; veil scratch = same colour with alpha 115 iff archetype Liquid; SetPixels32(x0,y0,w,h,scratch) per chunk on main+veil
- ComputeCellColor (1127-1628) reads mat, aux (fire life, organic dormant), morph, temp (incandescence), patina, and 4-neighbour mat for edges; returns alpha = def.baseColor.a; Empty returns default (transparent); OcultarRoca && Stone returns transparent (1145)
- Temperature tint: only when temp raw > IncandInicioRaw=150 (~180C); suave = t01*(0.45+0.55*t01); additive ember (72,30,6)*suave then lerp toward (255,214,150) by suave*0.45 (SimRenderer.cs:933-942,1574-1598) — there is NO cold tint (below ambient nothing changes visually)
- Vetas/Celdas patterns are purely positional from tick (no morph) and force chunk redraw even when asleep via _chunkContinuousAnim (SimRenderer.cs:246-253,1210-1214,1083)
- Camera owned by SimRenderer: base ortho = one screen (256x144 cells) * CuartoIntimoZoomFactor=5/9 (~80 cells tall) (SimRenderer.cs:230,473-493); wheel zoom in [0.8, 2.475], Tab = 2.475 wide view (144-158); dead zone 0.30, sharpness 6 (125,135); FocoCinematico/Sacudida statics (676-680)
- XorShift.FromCell(tick,x,y,salt) deterministic per-cell RNG; Chance(pct255), ChancePercent, Next(max), NextByte, NextBool (XorShift.cs:24-80); sim must never use UnityEngine.Random (3-12)
- Existing gallery pattern to copy: Game/GaleriaCurador.cs paints via PaintStable (349,387,422) and SimLevelBuilder.BuildGaleria builds its plan (AlkahestSim.cs:244)
- Direct grid writes outside the API still exist historically: HeatPlate/ChillStone wrote _sim.Grid.temp[] before InyectarTemperatura (HeatPlate.cs:63,197; ChillStone.cs:303); DeliveryChute reads Grid.temp[] directly (DeliveryChute.cs:791)

## APIs / ganchos

- AlkahestSim.Grid : CellGrid (AlkahestSim.cs:53) — direct access to all arrays
- AlkahestSim.Stepper : SimStepper (AlkahestSim.cs:54) — Tick (SimStepper.cs:39), Step() (257), ActiveCells/ActiveChunks/LastStepMs (282,297,302)
- AlkahestSim.Renderer : SimRenderer (AlkahestSim.cs:55); AlkahestSim.Universe (52)
- AlkahestSim.Paused { get; set; } (AlkahestSim.cs:66) — freezes accumulator and rendering
- AlkahestSim.StepOnce() (AlkahestSim.cs:795)
- AlkahestSim.Paint(int x,int y,int radius,byte materialId) (AlkahestSim.cs:527)
- AlkahestSim.PaintStable(int x,int y,int radius,byte materialId) (AlkahestSim.cs:697) — use for matter created from nothing
- AlkahestSim.PaintCell(int x,int y,byte materialId,byte tempRaw) (AlkahestSim.cs:566)
- AlkahestSim.PaintRect(int x0,int y0,int width,int height,byte materialId) (AlkahestSim.cs:730)
- AlkahestSim.InyectarTemperatura(int x,int y,byte tempRaw) (AlkahestSim.cs:771) — temp only, wakes chunk
- AlkahestSim.SampleMaterial(int x,int y) : int (AlkahestSim.cs:480); SampleTempRaw(int x,int y) : byte (487)
- AlkahestSim.WorldToCell(Vector3) : Vector2Int (780); CellToWorld(Vector2Int) : Vector3 (787)
- AlkahestSim.CrearMundoAnfitrion(int seed) (134); CrearMundoEspejo(int seed) (144); PrepararEspejo() (152); static NextRunSeed (40)
- AlkahestSim.AplicarChunkRemoto(int indiceChunk, byte[] rle, int parejas) (429) — mirror only
- CellGrid.Idx(x,y) (203), InBounds (205), GetMat (207-208), SetCell(idx,mat,resetAux=true) (210), SetCell(x,y,mat,resetAux) (222), SwapCells(a,b) (228)
- CellGrid.ChunkIndex(cx,cy) (240), CellToChunk (242), ChunkBounds (249), IsChunkAwake (257), WakeChunk(x,y,tick) (263), TickChunkIdle (282), WakeChunkIndex(cx,cy,tick) (288), LimpiarPatina(x,y) (306)
- CellGrid.RawToC(byte) (192), CToRaw(int) (194)
- SimRenderer.Init(Universe, CellGrid) (347) — NOT idempotent
- SimRenderer.RenderFrame(uint tick, bool forceFull=false) (820); RepintarAhora() (814); MarcarTodoSucio() (278)
- SimRenderer.Texture : Texture2D (345); TinteActual : Color (285)
- static SimRenderer.OcultarRoca : bool (275); static TinteMudanza : float (302); static FocoCinematico : Vector3? (676); static Sacudida : float (678); const SacudidaDuracion=1.6f (679)
- SimRenderer.CellWorldSize = 0.1f (21); BackgroundColor (24); TinteGlobal (52)
- XorShift.FromCell(uint tick,int x,int y,uint salt=0) (XorShift.cs:24); NextByte (50); Chance(byte pct255) (56); ChancePercent(int) (62); Next(int max) (70); NextBool (77)

## Estado por celda / chunk

- mat : byte[W*H] — material id; written by CellGrid.SetCell/SwapCells, AlkahestSim.Paint*, SimStepper; read everywhere (CellGrid.cs:71)
- temp : byte[W*H] — raw temperature 0..255 (C=raw*2-120), init AmbientRaw=70; written by SimStepper.DiffuseTemperature/reactions, PaintCell/PaintStable/InyectarTemperatura, SwapCells; read by SimRenderer incandescence (1574) (CellGrid.cs:73,187)
- aux : byte[W*H] — polysemous: gas/fire remaining life (renderer fire colour uses aux/gasLifetime, SimRenderer.cs:1155-1157), liquid flow memory, organic state (0x40 dormant, 0x80 settled), combustion reserve 7 bits; cleared by SetCell(resetAux=true) (CellGrid.cs:75,118-126)
- touchedTick : uint[W*H] — last tick a cell was processed, prevents double-processing after swaps; SimStepper only (CellGrid.cs:77)
- morph : byte[W*H] — morphological pattern state; seeded by SetCell hash, evolved by SimStepper.MorphTick (reaction-diffusion/dendrites/pulse/sparkle), read by SimRenderer.ApplyPatron; travels with SwapCells (CellGrid.cs:148,219,236)
- morphScratch : byte[W*H] — double buffer for MorphTick reaction-diffusion; SimStepper only (CellGrid.cs:150)
- patina : byte[W*H] — surface memory 0..255, renderer-exclusive: 1..90 wet (accumulation disabled since playtest 44), 91..220 soot; written by SimRenderer.ActualizarPatinaFranja, read by ComputeCellColor; cleared when cell stops being StaticSolid (CellGrid.cs:170; SimRenderer.cs:955-1032,1607-1625)
- ambient : byte[W*H] — per-cell target temperature for the passive pull; init uniform AmbientRaw; read only at SimStepper.cs:2185; intended hook for local climate (CellGrid.cs:116,181)
- chunkSleepTimer : byte[864] — consecutive idle ticks per chunk; awake iff <30; incremented by TickChunkIdle, zeroed by WakeChunk* (CellGrid.cs:80)
- chunkTouchedTick : uint[864] — last tick a chunk was woken (init uint.MaxValue); used by stepper idle accounting and by SimRenderer dirty tracking (CellGrid.cs:82,185)
- SimRenderer per-chunk: _chunkContinuousAnim bool[864] (positional animated patterns), _chunkLastRenderTick uint[864], _chunkEverRendered bool[864] (SimRenderer.cs:253,271-272,399-405)
- SimRenderer per-cell: _pixels Color32[W*H] CPU mirror of main texture (369); scratch buffers _chunkScratch/_veloScratch/_frontScratch Color32[256] (246,338,321)

## Constantes

- CellGrid.W=768, H=288, CHUNK=16, ChunksX=48, ChunksY=18, PantallaW=256, PantallaH=144 (CellGrid.cs:56-65)
- CellGrid.SleepTicks=30 (CellGrid.cs:68)
- CellGrid.AmbientRaw=70 (20C); raw->C = raw*2-120; CToRaw clamps 0..255 (CellGrid.cs:92,192-200)
- CellGrid.OrganicDormantAux=0x40 (CellGrid.cs:126); 0x80 = organic settled bit (SimStepper, per docblock 121-124)
- AlkahestSim.FixedDt=1f/30f, MaxStepsPerFrame=2 (AlkahestSim.cs:42-43); accumulator clamp = FixedDt*MaxStepsPerFrame (353-356)
- AlkahestSim.StableBirthMarginRaw=10 raw = 20C (AlkahestSim.cs:617)
- DiffuseTemperature: 1/8 of cells per tick (tick%8 offset), step=diff/4 min +-1, ambientSweep every 4th sweep (((tick>>3)&3)==0) with +-1 pull (SimStepper.cs:2155-2187)
- MorphTick dormantActiveRound = ((tick>>2)&7)==0 (1/8 throttle for sleeping chunks) (SimStepper.cs:2274)
- SimRenderer.CellWorldSize=0.1f (world units per cell; ppu=10) (SimRenderer.cs:21,723)
- SimRenderer.FullRefreshEveryFrames=30 (SimRenderer.cs:83); ViewMarginChunks=2 (161)
- SimRenderer.VeloAlfa=115 (liquid veil alpha ~45%) (SimRenderer.cs:341); veil sortingOrder 52, main quad -5 (726,755)
- Incandescence: IncandInicioRaw=150 (~180C), IncandTechoMezcla=0.45, IncandBrasaR/G/B=72/30/6; curve suave=t01*(0.45+0.55*t01) (SimRenderer.cs:934-942,1586)
- Patina: PatinaRowsPerFrame=12 (full pass every 24 render frames), MojadoTecho=90, TizneTecho=220, TizneIncremento=14, TizneHumoIncremento=6, MojadoIncremento=10 (disabled), DecayMojado=2, DecayTizneCadaFranjas=6 (SimRenderer.cs:946-953)
- Camera: CuartoIntimoZoomFactor=5/9 (~80 cells tall default), WideViewMultiplier=2.475 (198 cells), ZoomRuedaMinCerca=0.8, ZoomRuedaPaso=0.065, DeadZoneHalfFraction=0.30, CameraFollowSharpness=6, SacudidaDuracion=1.6s, SacudidaAmplitud=0.22 (SimRenderer.cs:125,135,144-145,157,230,679-680)
- TinteGlobal=(0.930,0.845,0.775); TintePlano=(0.630,0.640,0.700); BackgroundColor=#1A1418 (SimRenderer.cs:24,52,303)
- Masonry pattern: BlockW=8, BlockH=4, tone +-6%, joint -22%; edge light top +28%, bottom -20%, left +10%, right -12%; interior -8%, corner -12%; grain +-4 (SimRenderer.cs:1296-1376)
- PatronPeriodoCeldas(escala)=3+(escala-1)/2 (3..6 cells) (SimRenderer.cs:1792)
- DevPalette Time.timeScale presets 0.5/1/2/4 (Dev/DevPalette.cs:304-307)
- MaterialArchetype enum: Empty=0, StaticSolid=1, Powder=2, Liquid=3, Gas=4, Fire=5, Organic=6 (MaterialDef.cs:9-18)

## Riesgos

- Time control via Time.timeScale is NOT safe above ~2x: Update caps at MaxStepsPerFrame=2 and clamps the accumulator (AlkahestSim.cs:344,353-356), so 5x+ silently drops simulated time and also speeds every Unity-side system (camera smoothing, machines, audio). Use extra integer Steps per frame instead; never alter FixedDt (stepper is tick-indexed: RNG, diffusion phase tick%8, ambientSweep, MorphTick throttles).
- Running 50-100 Steps per frame renders once per frame but each Step still does a FULL W*H row sweep with ProcessIfNeeded per cell plus DiffuseTemperature over W*H/8 cells and full chunk accounting (SimStepper.cs:267-297) — cost is proportional to world size even when everything sleeps; 100 steps/frame at 221k cells will not hit 30fps. Benchmarks must read SimStepper.LastStepMs/ActiveCells/ActiveChunks.
- SimRenderer.Init is NOT idempotent and CrearMundo refuses a second call (AlkahestSim.cs:159-163); a laboratory scene needing world rebuild must reload the scene (as RestartRun does) or add a teardown path.
- Any new per-cell array must be added to CellGrid constructor, and to SwapCells if it must travel with matter (only mat/temp/aux/morph travel today, CellGrid.cs:228-237); SetCell does not touch temp (inherits) nor patina.
- aux is heavily overloaded per archetype (gas life, liquid flow memory, organic bits, combustion reserve 7 bits) — a humidity/sediment field cannot reuse aux without colliding; add a new array.
- ambient[] is read every 32 ticks with +-1 pull only; a 'permanent fire' via ambient alone heats very slowly (~1 raw per 32 ticks) — EmisionTermica/InyectarTemperatura (Crisol/HeatPlate pattern) is the active path.
- Determinism: any new rule must use XorShift.FromCell(tick,x,y,salt) with a distinct salt and never depend on iteration order across cells within a tick (morph uses a scratch double buffer for that reason, CellGrid.cs:142-145).
- Mirror/multiplayer: only mat[] is synced (AplicarChunkRemoto, AlkahestSim.cs:423-427); temp/morph/ambient/new arrays are not; InyectarTemperatura is not forwarded (763-769). A lab feature relying on temp must stay host-only or extend SimSync.
- Renderer redraw gating depends on chunk sleep + chunkTouchedTick; a debug overlay (temperature map) that changes appearance without changing grid state will not refresh sleeping chunks unless it forces MarcarTodoSucio/RepintarAhora or uses the full-refresh cadence (every 30 render calls).
- Texture2D.Apply uploads the entire 768x288 RGBA texture (~864KB) per texture per drawn frame (SimRenderer.cs:899-911): adding overlay textures adds a full upload each; consider one overlay texture only.
- There is no cold tint in ComputeCellColor (only >150 raw) — condensation/cooling experiments are visually invisible unless a debug view is added.
- World border ring (x=0,W-1,y=0,H-1) is immutable Stone: all Paint* refuse it and DiffuseTemperature skips it (AlkahestSim.cs:537,542,570,708,713,741,744,774; SimStepper.cs:2168).
- Patina strip update runs over the whole world every render frame (12 rows x 768 with Universe.Get lookups per cell, SimRenderer.cs:961-1031) regardless of camera; it is a fixed per-frame cost to keep in mind for benchmarks.
- OcultarRoca (PielDeRoca) and TinteMudanza are static globals; a lab scene inherits their last values unless reset.
- SimRenderer owns the camera (FitMainCamera/UpdateCameraFollow) and looks for ApprenticeController; a lab without an apprentice falls back to world centre (SimRenderer.cs:602-604) — fine, but any custom camera must fight UpdateCameraFollow every frame or use FocoCinematico.

## Oportunidades

- Time scaling: expose MaxStepsPerFrame as a settable field (or add `StepsPerFrame` multiplier) in AlkahestSim.Update (AlkahestSim.cs:43,344) — 1x/5x/10x/50x/100x = 2/10/20/100/200 max steps with the accumulator scaled; render once after the loop exactly as now. StepOnce (795) already shows the pattern for manual stepping.
- Local climate for the permanent fire / cold room: write CellGrid.ambient[] in a region (CellGrid.cs:116); DiffuseTemperature will pull toward it with zero code changes (SimStepper.cs:2185). Combine with InyectarTemperatura (AlkahestSim.cs:771) for the active source, as Crisol/HeatPlate do.
- Painting/carving: PaintStable for stone/water/steam creation (Crisol.cs:1806 already paints MaterialId.Steam; GaleriaCurador.cs:349 is the gallery brush), Paint(...,MaterialId.Empty) to carve (Cincel pattern), PaintRect for exact-count deposits, PaintCell for matter carrying its own temperature.
- Level plan: AlkahestSim.CrearMundoInterno chooses the plan at AlkahestSim.cs:244-246 (ModoGaleria -> SimLevelBuilder.BuildGaleria, R127); the laboratory should add a sibling flag/branch here and a BuildLaboratorio in SimLevelBuilder.
- Debug views (temperature/humidity/flow): hook inside RenderChunk at SimRenderer.cs:1068 with a static view-mode enum replacing ComputeCellColor, plus MarcarTodoSucio()/RepintarAhora() on toggle (278,814) so sleeping chunks repaint; or add a 4th texture+sprite by cloning the _veloTexture pattern (Init 392-398, BuildQuad 751-758, RenderChunk 1075-1079/1094, Apply 910). Temperature data is already available per cell (_grid.temp[idx]).
- Benchmarks/stats: SimStepper.LastStepMs, ActiveCells, ActiveChunks (SimStepper.cs:282,297,302) and Stopwatch already in Step; CellGrid.IsChunkAwake per chunk for an awake-chunk overlay.
- Sediment/turbidity visual: the liquid veil (R129) already re-paints Liquid cells in front with the same colour — a turbid water material gets the veil for free; liquid shimmer/surface line in ComputeCellColor (1413-1425) applies to any Liquid archetype.
- Vegetation: Organic archetype already has dormant bit (aux 0x40) with renderer desaturation (SimRenderer.cs:1403-1409) and SimStepper.GrowthTick; a simple plant can reuse Organic rules.
- Deterministic RNG: XorShift.FromCell(tick,x,y,salt) with a new salt per rule (XorShift.cs:24).
- Chunk wake control: CellGrid.WakeChunk (3x3) vs WakeChunkIndex (single) lets a new system wake exactly what it touched; TickChunkIdle/IsChunkAwake for sleep-aware passes (CellGrid.cs:257-294).
- Pausing: AlkahestSim.Paused freezes sim+render (AlkahestSim.cs:66,332) independent of Time.timeScale; DevPalette (Dev/DevPalette.cs) is the existing live parameter panel + F3 hover panel + brush (PaintStable at 217) to extend or copy.
- Camera framing for the lab: SimRenderer.FocoCinematico (676) centres any world point with the same smoothing; Tab/wheel zoom already viewport-aware for rendering.

## Preguntas abiertas

- What does SimLevelBuilder.BuildGaleria (R127) and Game/GaleriaCurador.cs already provide (brush, presets, stable flag) — the lab should extend rather than duplicate; not in this reading scope.
- Exact per-Step cost at 768x288 with most chunks asleep (ProcessIfNeeded sweeps all W*H cells per tick): needs a measurement via SimStepper.LastStepMs before committing to 100x via extra steps.
- Does any material for 'turbid water', 'sediment', or 'steam condensation' already exist in Universe/MaterialDef (Crisol paints MaterialId.Steam; DepositoDeAgua uses a 'Lodo' id) — material roster was not read here.
- Whether liquid 'flow memory' in aux (CellGrid.cs:74) is readable enough to build a flow-direction debug map, or whether a dedicated velocity/flow array is needed.
- How the sim would be torn down/rebuilt for parameter presets: CrearMundo refuses a second call and SimRenderer.Init is not idempotent — is scene reload (RestartRun) acceptable for the lab?
- Whether Time.timeScale changes from DevPalette should be disabled in the lab so that sim-time and Unity-time do not double-scale.
- MaterialId constants (Stone, Fire, Brasa, Smoke, Steam, Water, PisoEstructural, Vivium) live in MaterialDef.cs/Universe.cs and were only grepped, not read.
