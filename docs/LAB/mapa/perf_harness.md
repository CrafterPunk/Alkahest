# MAPA DEL MOTOR — lector `perf_harness`

*(Generado por un agente lector el 2026-09-03 sobre el HEAD 371dea4. Referencias archivo:línea. Es contexto, no dogma.)*


## Resumen

TIMING THAT EXISTS. The only per-tick timer in the codebase is inside the stepper: `SimStepper._sw` (System.Diagnostics.Stopwatch, C:/JuegosUnity/UnityAI_Test/Alkahest/Assets/Alkahest/Sim/SimStepper.cs:35) is restarted at the top of `Step()` (line 259) and stopped at the end (line 301), exposing `LastStepMs` (double, line 42/302). It wraps the whole tick: DiffuseTemperature, the row sweep, chunk sleep bookkeeping, and MorphTick. There is no Unity Profiler/ProfilerMarker/BeginSample anywhere, no Time.realtimeSinceStartup, no timing in SimRenderer, SimSync or Editor tools. Counters exposed by SimStepper: `Tick` (uint), `ActiveCells` (cells actually processed this tick, line 40/282), `ActiveChunks` (awake chunks after sleep update, line 41/297), `EventHead`/`Events` ring (256 entries) plus the monotonic `LeerEventosDesde(ref ulong cursor, buf)` reader (line 190). These are shown live only in the F3 dev palette (`Assets/Alkahest/Dev/DevPalette.cs:243-245`: FPS, "Sim: X ms", "Chunks activos a/864", "Celdas activas", Seed, Tick). DevPalette also has P (pause via Time.timeScale), N (single tick via `AlkahestSim.StepOnce()`, line 150/299) and 0.5x/1x/2x/4x buttons that set `Time.timeScale` (lines 304-307). Because `AlkahestSim.Update` accumulates `Time.deltaTime` with `FixedDt=1/30` and `MaxStepsPerFrame=2` (`Assets/Alkahest/AlkahestSim.cs:42-43, 342-356`), timeScale >2 at 60 fps is silently capped at 2 ticks/frame (so real time-scaling in a lab needs its own loop calling `_stepper.Step()` N times, or raising MaxStepsPerFrame).

THE HEADLESS BENCHMARK ALREADY EXISTS. `C:/JuegosUnity/UnityAI_Test/Alkahest/Tools~/BenchSim/Harness.cs` (96 lines, static class `Harness.Main`) defines 6 stress scenarios (CASCADA, DILUVIO TOTAL, INCENDIO, ARENA MASIVA, MUNDO MIXTO, INCENDIO SOSTENIDO) as `Action<CellGrid, Universe>` lambdas that fill rectangles with `grid.SetCell + grid.WakeChunk(x,y,0)` (helper `Bloque`, line 65). `Escenario` (line 72) creates `Universe.Create(12345)`, a fresh `CellGrid`, a 12-row stone floor, runs 30 warm-up ticks, then 300 timed ticks summing `stepper.LastStepMs`, reporting mean, peak (with `ActiveCells` at the peak) and headroom vs 33.3 ms. It bypasses AlkahestSim/SimLevelBuilder/SimRenderer entirely (pure Sim/ classes). It is NOT in an asmdef and is compiled outside Unity (sandbox script `compile_fiel.sh` against the 155 DLLs of `Builds/TenThousandYearsMulti/..._Data/Managed`, CLAUDE.md item 7) because Universe.cs/MaterialDef.cs still reference UnityEngine (Color32, Debug.Assert at Universe.cs:1285-1292, Debug.Log at 4072). Measured numbers live in `docs/archivo/INFORME_MOTOR.md:12-18` (worst case DILUVIO 5.48 ms mean / 11.65 ms peak at 74,000 active cells; in-game via F3 0.6-1.7 ms) and every subsequent motor contract required "the harness must keep compiling" and a budget of +2 ms worst case (`docs/archivo/CONTRATO_MOTOR.md:176-182`). A second headless use: thermal calibration (EmisionTermica constants, SimStepper.cs:2803-2839) and Semilla Cero seed selection (Universe.cs:364-379) were verified by running the harness with custom setups; `HISTORIAL_RONDAS.md:2989-2992` records "Banco: +0.9..3.8% estable".

OTHER INSTRUMENTATION. `Assets/Alkahest/Game/TelemetriaMovimiento.cs` is gameplay-only telemetry (static counters of time on foot vs flying, pour/aspirate on ground/air, takeoffs, landings, jumps, gestures; Debug.Log every 120 s and on mode change/close, tag `[Telemetría movimiento]`); it never touches the sim. `Game/Termometro.cs` is a G-key thermometer with up to 3 probes sampled at ~4 Hz (dev validation of temperature fields, no timing). GaleriaCurador F10 saves 9 PNG captures per area (image regression, not perf).

PERSISTENT CAMPFIRE. `Brasa` is MaterialId 58 (`Sim/Universe.cs:59`), a Powder (density 110, fluidity 1, emitsGlow, Pulso pattern; `gasLifetime=75` reused as life seed, Universe.cs:894-916). In the stepper, `ProcessPowder` (SimStepper.cs:972) routes it to `ProcessBrasa` (line 917): water above or 2+ orthogonal water -> Ash + steam puff; every `BrasaLifeUnitTicks=4` ticks it wakes its own chunk, injects `BrasaCalorRaw=10` heat to itself and 4 neighbours, tries to reignite flammable neighbours with 8 % chance, and decrements `aux` life (seeded 60..90 units by `ConvertirEnBrasa`, i.e. 8-12 s) -> Ash when 0. So a Brasa bed painted via PaintStable dies to Ash in about a minute at most (PaintStable leaves aux=0, which `ProcessBrasa` decrements from 0 -> stays 0 -> Ash on the first sampled step; the fire above (Fire cells, gasLifetime ~16-80) also expires). The gallery works around this in `Game/GaleriaCurador.cs:366-391`: `RefrescarFogatas` runs every 5 s over a remembered list of fire anchors (`_fogatas`, max 64); it counts Brasa/Fire vs Ash in a 5x5 window; if nothing remains it forgets the fire (player removed it); if fewer than 6 embers remain it repaints `PaintStable(f.x, f.y, 2, Brasa)` and 3 Fire cells at y+3 with `PaintCell(...,Fire,220)`. Placement (`Aplicar`, line 347-364) paints Brasa radius r plus 3 Fire cells at cy+r+1. The initial gallery bonfire is hardcoded in `AlkahestGameBootstrap.SpawnGaleria` (line 1035-1036: `PaintStable(110,170,2,Brasa)` + 3 Fire at y 173) and is NOT registered in `_fogatas`, so it dies. The Fundacion prologue keeps the Master's fire alive differently (FundacionDirector.cs:3250-3290: rewrites `grid.aux=BrasaVida(90)` and `temp>=165` on the ember rect every tick and paints Fire tongues). Fire's own heat: `ProcessFire` sets its cell temp to 255 and InjectHeat 40 (SimStepper.cs:1591-1593).

ORGANISM SYSTEM. `Criatura` (Rescoldo creature) and `Capullo` (cocoon) still exist and compile but are parked (ESTADO.md:84-91: nobody spawns them; `BuildCuna/BuildRepisa/PlaceNutrienteMound` have no callers). Materials `Nutrient` (id in fixed roster, Powder, flammable, Universe.cs:773-785) and `Vivium` (Organic archetype, Universe.cs:787+) DO still exist and `SimStepper.GrowthTick` (line 1821) is live: settled Vivium consumes an orthogonal Nutrient and grows with `VivGrowChancePct` when temperature is in `[VivGrowMinRaw,VivGrowMaxRaw]`, throttled 1/4 ticks, only tips (`CountOrganicNeighbors <= HabitoTolerarVecinosPunta`) grow, with persistence/bifurcation/vertical bias per seed. The gallery catalogue already exposes "nutriente" (GaleriaCurador.cs:88).


## Hechos clave

- SimStepper.Step() is the only timed unit: Stopwatch restart at C:/JuegosUnity/UnityAI_Test/Alkahest/Assets/Alkahest/Sim/SimStepper.cs:259, stop at :301, LastStepMs at :302; it includes DiffuseTemperature (:262), the bottom-up row sweep (:267-280), chunk sleep update (:284-297) and MorphTick (:299).
- ActiveCells counts cells for which ProcessIfNeeded returned 1 (SimStepper.cs:282, :311-372): only cells in awake chunks, not already touched this tick, and non-Empty.
- ActiveChunks counts chunks with chunkSleepTimer < CellGrid.SleepTicks(30) after TickChunkIdle (SimStepper.cs:284-297; CellGrid.cs:68, :257-286).
- No Unity Profiler, ProfilerMarker, BeginSample/EndSample, or Time.realtimeSinceStartup exists anywhere under Assets/Alkahest (grep empty).
- The F3 dev overlay (Assets/Alkahest/Dev/DevPalette.cs:243-245) is the only in-game display of LastStepMs/ActiveChunks/ActiveCells/Tick/Seed; it is active when Application.isEditor || Debug.isDebugBuild (:158).
- AlkahestSim.Update pacing: FixedDt=1/30 and MaxStepsPerFrame=2 (Assets/Alkahest/AlkahestSim.cs:42-43); accumulator clamped to 2*FixedDt (:353-356); StepOnce() (:795-800) does one Step + RenderFrame ignoring the accumulator.
- The DevPalette 4x speed button sets Time.timeScale=4 (DevPalette.cs:307) but the accumulator cap means at most 2 ticks per rendered frame, so effective speed is bounded by frame rate.
- Headless harness: C:/JuegosUnity/UnityAI_Test/Alkahest/Tools~/BenchSim/Harness.cs — 6 scenarios (lines 10-62), Bloque helper (:65-70) uses grid.SetCell + WakeChunk(x,y,0), Escenario (:72-95) = Universe.Create(12345) + new CellGrid + 12-row stone floor + 30 warm-up ticks + 300 timed ticks, prints mean/peak/peak-active-cells/headroom.
- The harness runs SimStepper directly (no AlkahestSim, no SimLevelBuilder, no SimRenderer) and is compiled outside Unity against the build's Managed DLLs (CLAUDE.md:50-55 compile_fiel.sh); Universe.cs and MaterialDef.cs still depend on UnityEngine (Color32; Debug.Assert at Universe.cs:1285-1292; Debug.Log at Universe.cs:4072) so it is not Unity-free.
- Measured baselines in docs/archivo/INFORME_MOTOR.md:12-22: Cascada 2.29 ms mean / 5.86 peak (8,000 cells); Diluvio 5.48 / 11.65 (74,000); Incendio 2.49 / 4.15 (17,940); Arena 1.99 / 2.97 (15,000); Mixto 3.94 / 5.80 (41,656); in-game via F3 0.6-1.7 ms; earliest note 0.2-1.0 ms/tick at 60+ fps (HISTORIAL_RONDAS.md:19).
- Contract rule for engine rounds: <= ~2 ms added in the worst harness scenario and the harness must keep compiling (docs/archivo/CONTRATO_MOTOR.md:176-182).
- Harness was also used for thermal calibration (SimStepper.cs:2803-2839 EmisionTermica docblock: 3000-tick runs, seeds 12345/10/51) and for Semilla Cero seed selection (Universe.cs:364-379).
- TelemetriaMovimiento (Assets/Alkahest/Game/TelemetriaMovimiento.cs:14-59) is static, gameplay-only, logs every 120 s (CadaSeg) and on CerrarBloque/Cerrar; it never touches the sim.
- Brasa = MaterialId 58, Powder, density 110, fluidity 1, emitsGlow, Pulso pattern, gasLifetime 75 reused as life seed (Universe.cs:59, :894-916).
- ProcessBrasa (SimStepper.cs:917-958): extinguished by water above or 2+ water neighbours (-> Ash + SpawnSteamPuff); every 4 ticks (BrasaLifeUnitTicks, :894) wakes own chunk, temp += BrasaCalorRaw(10) and InjectHeat(10) to 4 neighbours, 8 % reignite (BrasaReencenderPct :896), aux life-- -> Ash at 0.
- ConvertirEnBrasa (SimStepper.cs:900-906) seeds aux=60..90 via SalBrasaVida; PaintStable(Brasa) leaves aux=0 so a hand-painted ember decays immediately once sampled.
- GaleriaCurador persistent campfire: catalog entry 'fogata' Mat=Brasa Estable=true Fuego=true (GaleriaCurador.cs:80); Aplicar paints Brasa radius r + 3 Fire (PaintCell temp 220) at cy+r+1 and records anchor (:347-364); RefrescarFogatas every 5 s repaints when Brasa+Fire < 6 in a 5x5 window, forgets when 0 Brasa/Fire/Ash (:366-391).
- Initial gallery bonfire hardcoded at (110,170) r=2 + Fire at y=173 in AlkahestGameBootstrap.SpawnGaleria (Assets/Alkahest/Game/AlkahestGameBootstrap.cs:1035-1036), not registered in _fogatas.
- Prologue keeps Master's fire alive by rewriting grid.aux=BrasaVida(90) and temp>=BrasaRaw(165) each tick over the ember rect and painting Fire tongues (Game/FundacionDirector.cs:182-183, :3250-3290).
- ProcessFire sets own temp to 255 and InjectHeat(40) to 4 neighbours each tick (SimStepper.cs:1591-1593); free Fire with aux==0 gets life 16±3 (:1530-1531,:1560-1563); fuel-adjacent fire clamps life to >=30 (:1568).
- Criatura/Capullo are parked but intact (docs/ESTADO.md:84-91); Criatura seeds real Vivium with PaintStable (Criatura.cs:749) and relies on SimStepper.GrowthTick; Capullo progresses by real cell temperature >= Universe.VivGrowMinRaw.
- Nutrient (Powder, flammable, ignition 180C) and Vivium (Organic, ignition 150C, boilsAt ~120C -> Ash) are still in the roster (Universe.cs:773-800); GrowthTick is live (SimStepper.cs:1821-1952).
- Gallery mode entry: DayCycle title button sets AlkahestGameBootstrap.ModoGaleria=true then RestartRun(SemillaCero) (DayCycle.cs:1053-1058); AlkahestSim.CrearMundoInterno picks SimLevelBuilder.BuildGaleria (AlkahestSim.cs:244); Bootstrap.SpawnGaleria (:1015-1040) spawns backdrop+apprentice+DayCycle+GaleriaCurador.
- BuildGaleria (Sim/SimLevelBuilder.cs:3060+) = ObraDelTaller.Clear, ReservasDelPlano.Clear, FillWorldStone, then DrawSolidRect rooms; anchors GaleriaAnclaX/Y/Nombre (:3054-3058).
- Dispenser emits EmitRatePerTick=12 cells/tick via PaintStable in SpoutRadius=1 (Dispenser.cs:120-121, :692-722) — an existing 'permanent stream' source usable in the lab (GaleriaCurador can place a water 'cano' at the cursor, :272-277).
- SimRenderer cost is viewport-bounded (ComputeVisibleChunkRange, SimRenderer.cs:838) and Texture2D.Apply only when something was drawn (:906-911); full refresh every 30 frames (:83).

## APIs / ganchos

- public void SimStepper.Step() — C:/JuegosUnity/UnityAI_Test/Alkahest/Assets/Alkahest/Sim/SimStepper.cs:257
- public uint SimStepper.Tick / public int ActiveCells / public int ActiveChunks / public double LastStepMs — SimStepper.cs:39-42
- public SimStepper(Universe universe, CellGrid grid) — SimStepper.cs:147
- public int SimStepper.LeerEventosDesde(ref ulong cursor, SimNotableEvent[] destino) — SimStepper.cs:190 (non-destructive multi-cursor event reader; use for per-scenario event counters e.g. Boil/Freeze/Ignite/Ember)
- public SimNotableEvent[] SimStepper.Events / public int EventHead / const int EventBufferSize=256 — SimStepper.cs:51,144-145
- public void SimStepper.RegistrarZonaInteres(int x0,int y0,int x1,int y1) — SimStepper.cs:224 (reaction sampling 1/2 instead of 1/8 in those chunks)
- public bool SimStepper.EncenderCombustionPersistente(int x,int y) / EstaCombustionActiva(int x,int y) — SimStepper.cs:755,768
- public void AlkahestSim.StepOnce() — Assets/Alkahest/AlkahestSim.cs:795 (one Step + RenderFrame, ignores accumulator)
- public bool AlkahestSim.Paused {get;set;} — AlkahestSim.cs:66
- public void AlkahestSim.PaintStable(int x,int y,int radius,byte materialId) — AlkahestSim.cs:697 (creates matter at stable temp; WakeChunk)
- public void AlkahestSim.Paint(int x,int y,int radius,byte materialId) — AlkahestSim.cs:527 (moves/erases; keeps temp)
- public void AlkahestSim.PaintCell(int x,int y,byte materialId,byte tempRaw) — AlkahestSim.cs:566
- public void AlkahestSim.PaintRect(int x0,int y0,int w,int h,byte materialId) — AlkahestSim.cs:730 (no temp write)
- public void AlkahestSim.InyectarTemperatura(int x,int y,byte tempRaw) — AlkahestSim.cs:771
- public int AlkahestSim.SampleMaterial(int x,int y) / public byte SampleTempRaw(int x,int y) — AlkahestSim.cs:480,487
- public Universe AlkahestSim.Universe / CellGrid Grid / SimStepper Stepper / SimRenderer Renderer — AlkahestSim.cs:52-55
- public static Universe Universe.Create(int seed) — Assets/Alkahest/Sim/Universe.cs:505; public const uint SemillaCero=777002u (:381); public MaterialDef Get(byte id) (:452)
- public static void Universe.AplicarOverridesSemillaCero(Universe u) — Universe.cs:3719
- public void CellGrid.SetCell(int idx|x,y, byte mat, bool resetAux=true) / SwapCells / WakeChunk(x,y,tick) / WakeChunkIndex / TickChunkIdle / IsChunkAwake — Assets/Alkahest/Sim/CellGrid.cs:210-294
- public static int CellGrid.RawToC(byte) / byte CToRaw(int) / Idx(x,y) / InBounds / ChunkBounds — CellGrid.cs:192-255
- public static void SimLevelBuilder.BuildGaleria(CellGrid grid) — Assets/Alkahest/Sim/SimLevelBuilder.cs:3060; DrawSolidRect(grid,x0,y0,w,h,mat) — :4594; FillWorldStone(grid) private — :3346
- public static bool SimLevelBuilder.EsObraDelTaller(int x,int y) — SimLevelBuilder.cs:2261 (chisel respects these rects)
- public static GaleriaCurador GaleriaCurador.Crear(AlkahestSim sim, ApprenticeController aprendiz) — Assets/Alkahest/Game/GaleriaCurador.cs:108; public static bool Abierto (:41)
- private void GaleriaCurador.RefrescarFogatas() — GaleriaCurador.cs:367 (5 s refresh of persistent campfires)
- public static bool AlkahestGameBootstrap.ModoGaleria — Assets/Alkahest/Game/AlkahestGameBootstrap.cs:120; private void SpawnGaleria() — :1015
- public static class TelemetriaMovimiento { Tick(bool aPie,float dt); Verter(bool); Aspirar(bool); Despegue(); Aterrizaje(); Gesto(); Salto(); CerrarBloque(string); Informe(); Cerrar() } — Assets/Alkahest/Game/TelemetriaMovimiento.cs:14-59
- public static bool DevPalette.IsOpen — Assets/Alkahest/Dev/DevPalette.cs:40 (guards used by Flask/Cincel)
- Harness.Main() / static void Bloque(CellGrid,int x0,int y0,int w,int h,byte mat) / static void Escenario(string, Action<CellGrid,Universe>) — C:/JuegosUnity/UnityAI_Test/Alkahest/Tools~/BenchSim/Harness.cs:7,65,72
- public static int EmisionTermica.PasoFootprint(int cur,int target,int fila,uint tick,int x,int y,Direccion) / PasoCollar(int cur) — SimStepper.cs:2795,2917 (heat/cold plate physics reusable for a lab 'fire' or 'cooler')

## Estado por celda / chunk

- CellGrid.mat : byte[W*H] material id — written by SetCell/SwapCells/Transform/Paint*, read everywhere (CellGrid.cs:71)
- CellGrid.temp : byte[W*H] raw temperature 0..255 (C = raw*2-120), ambient 70 — written by DiffuseTemperature, InjectHeat/AddTemp, PaintStable/PaintCell/InyectarTemperatura, plates; read by ApplyPhase/GrowthTick/gas drift (CellGrid.cs:73)
- CellGrid.aux : byte[W*H] multiplexed: gas/fire remaining life; Brasa life units; Liquid bit0 = flow direction, bits1-7 = combust reserve; Powder full byte = combust reserve; Organic 0x80 settled, 0x40 dormant (OrganicDormantAux), 0x04 came-from-known, 0x03 came-from dir (CellGrid.cs:75,126; SimStepper.cs:722-743,1694-1702)
- CellGrid.touchedTick : uint[W*H] last tick processed — guard against double processing after swap (CellGrid.cs:77; SimStepper.cs:317-318,699)
- CellGrid.morph : byte[W*H] morphological pattern state, seeded by hash in SetCell, travels in SwapCells; evolved by MorphTick (CellGrid.cs:148,219,236)
- CellGrid.morphScratch : byte[W*H] double buffer for MorphTick only (CellGrid.cs:150; SimStepper.cs:2297-2406)
- CellGrid.patina : byte[W*H] surface memory (wet <~90 / soot up to ~220), written/read ONLY by SimRenderer, never by stepper (CellGrid.cs:170)
- CellGrid.ambient : byte[W*H] per-cell ambient target for DiffuseTemperature pull; currently uniform 70 (CellGrid.cs:116; SimStepper.cs:2183-2188)
- CellGrid.chunkSleepTimer : byte[48*18] ticks idle per chunk; awake if < SleepTicks(30) (CellGrid.cs:80,257-286)
- CellGrid.chunkTouchedTick : uint[48*18] last tick a chunk was woken (init uint.MaxValue) (CellGrid.cs:82,185)
- SimStepper._zonaInteresChunk : bool[864] reaction-sampling mask 1/2 vs 1/8 (SimStepper.cs:71)
- SimStepper._morphChunkRelevant : bool[864] scratch per tick for MorphTick (SimStepper.cs:78)
- SimStepper._ultimoTickPorLey : uint[Leyes.Length] per-law event cooldown (SimStepper.cs:101)
- SimStepper._events : SimNotableEvent[256] ring + _eventHead + _eventWriteIndex (ulong monotonic) (SimStepper.cs:52-61)
- SimRenderer._chunkContinuousAnim : bool[864], _chunkEverRendered, _chunkLastRenderTick per chunk (SimRenderer.cs:253,399,890-891)

## Constantes

- CellGrid.W=768, H=288, CHUNK=16, ChunksX=48, ChunksY=18 (864 chunks), PantallaW/H=256/144 — Assets/Alkahest/Sim/CellGrid.cs:56-65
- CellGrid.SleepTicks=30 — CellGrid.cs:68
- CellGrid.AmbientRaw=70 (20 C); RawToC = raw*2-120 — CellGrid.cs:92,192
- AlkahestSim.FixedDt=1/30f, MaxStepsPerFrame=2 — Assets/Alkahest/AlkahestSim.cs:42-43
- AlkahestSim.StableBirthMarginRaw=10 (20 C) — AlkahestSim.cs:617
- SimStepper.EventBufferSize=256; LeyEventCooldownTicks=30 — SimStepper.cs:51,99
- DiffuseTemperature: 1/8 of cells per tick (offset=tick%8), diff/4 step, ambient pull ±1 when ((tick>>3)&3)==0 (every ~32 ticks) — SimStepper.cs:2155-2191
- MaybeReact sampling mask 7 (1/8) or 1 (1/2) in zonaInteres — SimStepper.cs:388
- MorphTick: 1/4 cells per tick, dormant chunks every 32 ticks (dormantActiveRound) — SimStepper.cs:2245,2275
- BrasaLifeUnitTicks=4, BrasaCalorRaw=10, BrasaReencenderPct=8; Brasa life 60..90 units (8-12 s); Brasa gasLifetime=75 — SimStepper.cs:894-896,904; Universe.cs:915
- Fire: FreeFireSeedLife=16 ±3, FadeTailLife=6, own temp 255, InjectHeat 40, TryIgnite 12 %/tick, fuel-adjacent life clamp 30 — SimStepper.cs:1530-1534,1568,1591-1593,1673
- Oil: combustReserva=120, combustPasoTicks=8 (~32 s burn), combustCalorRaw=12, humo 8 %, propagacion 15 %, lengua 35 %, residuo Empty — Universe.cs:687-693
- MaterialDef defaults: combustPasoTicks=8, combustCalorRaw=15, combustHumoPct=12, combustPropagacionPct=15, combustLenguaPct=35 — Assets/Alkahest/Sim/MaterialDef.cs:160-175
- Water: fluidity 4, freezesAt CToRaw(waterFreezeC in [-20,15]), boilsAt CToRaw(waterBoilC in ~[100,119] raw) — Universe.cs:648-661; Steam: density -50, gasLifetime 60, condensesAt CToRaw(waterBoilC-40) — :710-721; Smoke gasLifetime 200 — :731
- Sand density 180 fluidity 1; Ash density 120; Nutrient density 140 flammable ignition CToRaw(180); Vivium density 170 fluidity 0 ignition CToRaw(150); Limo (turbid primordial liquid) density = avg(water,oil), fluidity 2 — Universe.cs:637-800,1075-1112
- Gas: GasBolsaLateralPct=60, GasDeambularPct=35, GasOndulacionPct=30; rumbo hash (_tick>>4, x>>3, y>>3) — SimStepper.cs:1319-1336,1419
- Powder sink into lighter liquid 60 %; lateral slide 15 % when fluidity>2 — SimStepper.cs:993,1021
- ProcessDisolucionAgua: 1/8 sampling then 20 % — SimStepper.cs:1187,1200 (water + soluble powder -> Solucion)
- EmisionTermica: RadioFilas=5, NewtonK=0.05, CollarFilas=15, CollarStepRaw=3, SalEmpujeTermico=557 — SimStepper.cs:2851-2860
- XorShift salts in use (must stay unique): 1,2,5,9,13,17,42,77,88,91,205,237,239,241,401,0x7A11,503,509,521,523,547,549,551,553,557,563; morph offsets 201/209/213/221+semillaPatron — SimStepper.cs:116-131, 2858
- Harness: seed 12345, 12-row stone floor, 30 warm-up ticks, 300 timed ticks, budget 33.3 ms — Tools~/BenchSim/Harness.cs:74-94
- Dispenser EmitRatePerTick=12, SpoutRadius=1, OverflowSearchUp=8 — Assets/Alkahest/Game/Dispenser.cs:120-121,151
- GaleriaCurador: fogata refresh every 5 s, threshold <6 embers in 5x5, max 64 fires, brush radius 0..6, paint throttle 0.06 s — GaleriaCurador.cs:154-155,186,362,370,385
- TelemetriaMovimiento.CadaSeg=120 s — TelemetriaMovimiento.cs:16
- SimRenderer.FullRefreshEveryFrames=30 — Assets/Alkahest/Sim/SimRenderer.cs:83
- MaterialId.Count=66; Brasa=58; Limo=17; BaseEstado0=18 (5 bases x 8 states); PisoEstructural=65 — Universe.cs:44-109

## Riesgos

- LastStepMs is wall-clock and includes MorphTick + chunk bookkeeping; it is not split per phase. Any per-phase profiling requires adding Stopwatch sections inside Step() (hot path; keep zero-alloc) or wrapping in ProfilerMarker — neither exists today.
- Determinism depends on XorShift.FromCell(tick,x,y,salt) with unique salts (SimStepper.cs:116-131 lists used ones; 557/563 also taken). A new lab system must grep and pick fresh salts; reusing one silently correlates behaviour.
- PaintStable/Paint call WakeChunk with TickActual, and the harness Bloque uses WakeChunk(x,y,0). Scenario setup order and which tick you wake at affect the first 30 ticks of sleep behaviour; keep setup identical between runs for bit-identical replays.
- Universe.Create(seed) chemistry varies per seed (water freeze/boil, oil ignition, base materials); a benchmark must pin the seed (harness uses 12345; game uses SemillaCero 777002 plus AplicarOverridesSemillaCero when ModoSemillaCero/ModoFundacion). ModoGaleria does NOT apply the overrides (AlkahestSim.cs:228-229) although it restarts with seed 777002.
- Time.timeScale-based speed (DevPalette) is capped by MaxStepsPerFrame=2 and the accumulator clamp (AlkahestSim.cs:344-356): 4x is unreachable at 60 fps. A lab time-scale must drive extra Step() calls itself (and RenderFrame once), not rely on timeScale; also every machine (Crisol, ChillStone, Cincel, Alambique, Criatura) runs its own 1/30 accumulator with its own MaxStepsPerFrame (2 or 3), so sim-only time scaling desyncs machines from the grid.
- PaintStable(Brasa) leaves aux=0, so painted embers die on first sampled ProcessBrasa step; only fire tongues and heat remain briefly. A 'permanent fire' needs either periodic repaint (GaleriaCurador pattern) or aux/temp rewriting each tick (FundacionDirector pattern) or a new stepper-side eternal ember flag.
- Brasa/Fire are extinguished by water above or 2 orthogonal water cells and emit Steam (SpawnSteamPuff) — a stream routed near the fire will kill it; Steam condenses to Water at condensesAt when life hits 0 (ProcessGas :1395-1398), i.e. condensation only on expiry, not continuously.
- DiffuseTemperature has no cutoff radius: sustained heat/cold sources leak across the whole grid over thousands of ticks (documented SimStepper.cs:2815-2826); EmisionTermica's collar exists precisely to contain it. A permanent fire in a sealed lab will slowly heat everything unless a collar or ambient sink is added.
- Gas life extension under a roof is half-decay only (SimStepper.cs:1383-1392); Steam gasLifetime=60 (+jitter) means steam cannot travel far through long ducts before expiring/condensing — conducting steam through geometry may need higher gasLifetime for a lab preset (tuning MaterialDef, which is per-Universe instance and mutable, e.g. via an overrides pass like AplicarOverridesSemillaCero).
- Sediment/turbid water does not exist as a mechanism: Limo is a plain liquid; ProcessDisolucionAgua turns Water+soluble powder into Solucion (a different liquid id), not a suspension; powders sink into lighter liquids with 60 % per tick. Settling/deposition must be new stepper code (and new aux bit usage must respect the existing bit map).
- Cohesive solid movement: caeSolido StaticSolids fall straight down 1 cell/tick only into Empty, with cantilever check up to cohesionCeldas<=8 (SimStepper.cs:1274-1308); no lateral push, no rotation; Stone/PisoEstructural never fall (R7).
- Universe.cs/MaterialDef.cs reference UnityEngine (Color32, Debug.Assert/Log), so the 'headless' harness needs UnityEngine.CoreModule.dll from a build; a Unity Test Runner (PlayMode/EditMode) harness inside the project would avoid the external compile script, but there is no Tests asmdef today.
- Event ring is 256 entries and three consumers read it non-destructively (R8); acid/boil can push dozens of events per tick, so counting events in a benchmark must use LeerEventosDesde every tick or it loses data.
- SimLevelBuilder.BuildGaleria clears ObraDelTaller/ReservasDelPlano static lists; any lab builder must do the same or chisel/protection rules from a previous mode leak in (ModoGaleria flag resets per R59 in DayCycle/SimSync).
- GaleriaCurador._fogatas repaints with radius 2 regardless of the radius used when placing; the initial bootstrap fire is not tracked. RefrescarFogatas uses Time.deltaTime (frame time), not sim ticks: under pause/timeScale it drifts relative to the sim.
- SimSync mirrors only mat[] (temp/morph/aux not synced): a lab that relies on temperature or aux state cannot be observed correctly by a multiplayer guest (AlkahestSim.cs:423-427).

## Oportunidades

- Reuse Tools~/BenchSim/Harness.cs verbatim as the benchmark skeleton: add lab scenarios (turbid stream through a carved channel, sealed pool + permanent ember, steam duct + cold wall, Vivium+Nutrient bed, caeSolido body) as new Escenario lambdas; it already reports mean/peak/active cells.
- SimStepper.LastStepMs + ActiveCells + ActiveChunks are already public per tick: an in-game benchmark panel just needs to sum them over N StepOnce()/Step() calls (DevPalette.cs:243-245 shows the exact fields).
- AlkahestSim.StepOnce() and Paused give a ready-made deterministic 'N ticks then stop' driver from inside the game; a lab time-scale can call _stepper.Step() k times then RenderFrame once (mirroring StepOnce).
- PaintStable(x,y,r,mat) is the sanctioned way to spawn matter at stable temperature (R22/R29); Dispenser.EmitTick shows a 12 cells/tick permanent source pattern; GaleriaCurador already places a water 'cano' (Dispenser Init at cursor) and Brasa fires at the cursor.
- GaleriaCurador.RefrescarFogatas (5 s repaint when <6 embers) is the existing 'permanent campfire' mechanism; FundacionDirector's per-tick aux/temp rewrite (FundacionDirector.cs:3250-3290) is the stronger 'eternal ember' pattern for a lab fire.
- SimStepper.LeerEventosDesde(ref cursor, buf) gives zero-alloc per-tick counters of Boil/Freeze/Ignite/Ember/Grow/Dissolve/Ley events for scenario metrics (e.g. evaporation rate = Boil events/tick, condensation = count Steam->Water via grid diff).
- EmisionTermica (SimStepper.cs:2848-2922) is a pure static heat/cold pusher with a containment collar: reusable for a lab 'permanent fire' or 'cold wall' without new physics (HeatPlate/ChillStone already consume it with their own 1/30 accumulators).
- Universe.AplicarOverridesSemillaCero shows the pattern for mutating MaterialDef fields of a live Universe (e.g. raise Steam gasLifetime, tune Water fluidity) for lab presets without touching Create().
- SimStepper.RegistrarZonaInteres(x0,y0,x1,y1) raises reaction sampling to 1/2 in the lab area (visibility of chemistry).
- CellGrid.ambient is uniform but per-cell and read every DiffuseTemperature pass: a cold wall/condenser can be expressed as lower ambient in a region at zero hot-path cost (R31 keeps the array for player-made climate).
- GrowthTick + Nutrient/Vivium already implement simple vegetation with seed-dependent habit (persistence, bifurcation, vertical bias) and temperature band VivGrowMinRaw..MaxRaw; Criatura.cs shows how to seed and probe it (PaintStable Vivium, count cells).
- caeSolido + cohesionCeldas (ProcessSolidoCohesion) is a minimal cohesive solid body mechanism (Ice, crystal, reticulum products) to build on.
- SimRenderer patina byte (wet/soot memory) is renderer-only and free for the stepper: sediment/turbidity visuals could ride it without touching determinism.
- TelemetriaMovimiento's static-counter + periodic Debug.Log + CerrarBloque pattern is the house style for lightweight telemetry; a 'TelemetriaLaboratorio' can mirror it (no sim coupling, log tag in brackets).
- GaleriaCurador F10 RondaDeCapturas (PNG per area via SubmitRenderRequest) provides image-regression snapshots for lab presets.

## Preguntas abiertas

- How is Tools~/BenchSim/Harness.cs actually built today? The compile script (compile_fiel.sh, 155 DLLs) lives in the volatile sandbox, not in the repo; no csproj/README exists under Tools~. Should the lab benchmark instead become an EditMode/PlayMode test or an in-game panel to avoid the external toolchain?
- Should per-phase timing (DiffuseTemperature / sweep / MorphTick) be added inside SimStepper.Step()? It is hot-path code guarded by contracts; a ProfilerMarker or three Stopwatch reads per tick is cheap but touches Sim/ (needs CLAUDE.md-style documentation and harness compatibility).
- What is the intended semantics of 'turbid water' and 'sediment settling'? Nothing exists: options are (a) a new suspension liquid material with a settle probability (like Limo but converting to a powder on rest), (b) aux-bit turbidity on Water (bit budget: Water uses bit0 flow, bits1-7 combust reserve are unused for Water since combustReserva==0 — 7 free bits), (c) mixed Water+Sand cells with modified sink rules.
- Steam conduction: is Steam gasLifetime=60 (+half-decay under roof) enough for duct lengths in the lab, or should the lab Universe override it? Condensation currently occurs only when life expires below condensesAt (ProcessGas :1395-1398) — does the lab need continuous condensation on contact with cold cells?
- Should ModoGaleria (and a future ModoLaboratorio) apply AplicarOverridesSemillaCero? Today gallery uses seed 777002 without overrides, so its chemistry differs from the game's Semilla Cero.
- Which time-scaling approach is acceptable: extra Step() calls per frame (sim-only, desyncs machine accumulators) vs raising MaxStepsPerFrame globally vs Time.timeScale (already capped)?
- Is the in-game benchmark expected to run with rendering on (SimRenderer.RenderFrame cost is outside LastStepMs) — should render ms be measured separately?
- Does the lab need Criatura/Capullo (parked) or only the raw Vivium/Nutrient growth in the stepper? ESTADO.md says they return as 'organismos-solución' by Cesar's decision — spawning them in the lab may pre-empt that design.
