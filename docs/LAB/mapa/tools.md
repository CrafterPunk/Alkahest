# MAPA DEL MOTOR — lector `tools`

*(Generado por un agente lector el 2026-09-03 sobre el HEAD 371dea4. Referencias archivo:línea. Es contexto, no dogma.)*


## Resumen

PLAYER-MATTER INTERACTION MAP (all paths relative to Assets/Alkahest). The player avatar is one GameObject built in AlkahestGameBootstrap.SpawnApprentice (Game/AlkahestGameBootstrap.cs:1131-1170) carrying ApprenticeController + Flask + Cincel + Mudanza + Termometro + SubstanceKnowledge + FlaskHud; every tool gets `Init(AlkahestSim)`. All tools mutate the grid ONLY through AlkahestSim.Paint/PaintCell/PaintStable/PaintRect/InyectarTemperatura (AlkahestSim.cs:527-777), which skip the outer frame (x/y 0 and W-1/H-1) and call WakeChunk. Cursor→cell is a private copy of the same camera raycast to plane z=0 in Flask/Cincel/Termometro (Flask.cs:378, Cincel.cs:255, Termometro.cs:209); DevPalette and GaleriaCurador do the same inline.

FLASK (Game/Flask.cs): Capacity=900 cells (l.87); per-material counts + raw-temperature sums (_counts/_tempSum, l.102-103) so poured matter restores its mean temperature via PaintCell (l.516). Aspirate = LMB held, disc SuckRadius=4, SuckRatePerTick=30, ring-ordered nearest-first (l.435-478); pour = RMB, PourRadius=2, PourRatePerTick=20, densest material first (l.486-498), only into Empty cells; dump = Q/MMB, DumpRadius=4, remainder lost (l.535-560). Reach ReachWorld=6u=60 cells from the apprentice (l.97). Not aspirable: Empty, Stone, PisoEstructural, any Fire archetype (EsAspirable l.417-425; also repeated inline l.460-466). Material lock on press (BloquearMaterialBajoElCursor l.372), Shift = indiscriminate. 30 Hz accumulator with MaxStepsPerFrame=2 (l.89-90). Guards in order (l.259-323): DayCycle.InputLocked, DevPalette.IsOpen, GaleriaCurador.Abierto, FundacionDirector.FrascoBloqueado, UiStyles.EscribiendoTexto, JournalHud.Abierto||AlbumReal.Abierto, Cincel.ModoActivo, Mudanza.ModoActivo, StorageRack.RatonSobreRedoma(). NOTE Flask does NOT check Termometro.ModoActivo (documented gap, Termometro.cs:64-75). Flask calls apprentice.AnclaDeTrabajo() every frame while sucking/pouring (l.356).

CHISEL (Game/Cincel.cs): key C toggles ModoActivo (static), X toggles stone/PisoEstructural fill; CicloIncluyePiso=false (l.156) so C only cycles flask↔stone chisel. CarveRadius=FillRadius=2, rates 3 cells/tick (l.142-145), ReachWorldCincel=2.2u=22 cells (l.152). LMB carves (TallarTick l.383: only Stone or PisoEstructural, never frame, refuses SimLevelBuilder.EsObraDelTaller cells with warning), RMB builds (RellenarTick l.460: PaintStable Stone onto Empty/Smoke/Steam; piso may replace non-obra Stone). Carve LOS by thickness: Bresenham count of solid cells before disc, blocks if >=GrosorParedBloquea=3 (l.335-361); build has no LOS. Visual anillo/haz sortingOrder 38/39, mode icon 61.

MOVEMENT (Game/ApprenticeController.cs): moveSpeed=4.8, acceleration=96 (l.192-193, NOT serialized). F6 cycles ModoMovimiento {PiesYVuelo=0, SoloVuelo=1, SoloPies=2} persisted in PlayerPrefs "ModoMovimiento" (l.505, 644-668). Collision is read directly from the grid (no Unity colliders): AABB half-extents 0.32/+0.48/-0.64u, chamfered ChaflanCeldas=3, SubPaso=0.06u, corner-assist 0.05/0.10/0.15u (l.798-806); only Stone and PisoEstructural block, and obra-del-taller Stone is passable except repisas de cascada (CajaChoca l.910-945). Static ColisionConEstructura toggle (l.774). Inside-solid = collision suspended (fly) or Desenterrar (walk, l.694). Keyboard guard: `kb != null && !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto` (l.469, 480).

HEAT/COLD (Game/HeatPlate.cs, Game/ChillStone.cs): both are IMaquinaInteractiva+IMovible+IMaquinaUsableRemota, toggled with E when MachineFocus.EsFoco. They do NOT set temperature: each 30 Hz tick they push grid.temp toward a target with Sim.EmisionTermica (Sim/SimStepper.cs:2847-2921): RadioFilas=5 rows above _plateRow with quadratic falloff and Newton K=0.05·|diff| (fractional part by XorShift per cell, direction-locked SoloSube/SoloBaja), then CollarFilas=15 rows pulled toward AmbientRaw at CollarStepRaw=3; writes via _sim.InyectarTemperatura (HeatPlate.cs:472-495, ChillStone.cs:748-771). Targets: HeatPlate ArdienteRaw=220 (320 °C), Templada=_templadaRaw (Vivium band centre, out of the E cycle since pt51; CycleState is Off<->Ardiente, l.428-451); ChillStone HelandoRaw=47 (-26 °C), Fresca = min(water.freezesAt, CrystallizeMaxTempRaw)-10 (Init l.482-489), 3-state cycle. After switching off, HoldTicksTrasApagar=60 ticks holding only the adjacent row with HoldStepRaw=1 (ApplyHoldTick). Footprint = FootprintFraction 0.4 of the span passed to Init, min 8 cells, centred (HeatPlate.cs:359-364).

THERMOMETER (Game/Termometro.cs): key G, up to 3 probes (FIFO), resampled at 4 Hz, labels from a 256-entry table `CellGrid.RawToC(raw)+"°"` (l.130-136) where RawToC = raw*2-120 (Sim/CellGrid.cs:192; AmbientRaw=70 = 20 °C). Cursor readout drawn with UiStyles.ChipMini/Panel at mouse screen pos (l.443-475). Shows "—" when _sim.Stepper==null (guest mirror). Reach = Flask.ReachWorld.

DISPENSER (Game/Dispenser.cs): E toggles _on; EmitTick each 30 Hz tick paints up to EmitRatePerTick=12 Empty cells in a SpoutRadius=1 disc at (_spoutX,_spoutY) with PaintStable (l.702-722); if none free, searches up to OverflowSearchUp=8 cells upward for one drop (l.737-746). Optional ration (racionCeldas, 45 for lab taps) auto-closes. Init(sim, player, mountX, mountY, matId, orderSystem, favorCost, bloqueado, spoutOffsetCells=5, racionCeldas=0) (l.331).

DEVPALETTE (Dev/DevPalette.cs): dev builds only; F3 toggles (PlayerPrefs "TenThousandYears_DevPalette"), P = Time.timeScale pause, N = _sim.StepOnce() only when open; brush radius 1-10, LMB PaintStable, RMB Paint(Empty), no painting while mouse over window (IsOverWindow l.221). GUILayout.Window id 837465, Rect(12,180,300,480), speed buttons 0.5x/1x/2x/4x set Time.timeScale, FPS/Sim ms/active chunks/tick readout, cell hover with material/°C/aux/"· OBRA". Exposes static IsOpen consumed by Flask/Cincel/Termometro guards.

UI: UiStyles.Preparar() each OnGUI, S(px) scale, Panel/MarcoLaton/Barra/PlacaMundo/PlacaMundoLateral/Cercania, VestirSkin re-skins GUI.skin.window/button/textField/slider once. EscribiendoTexto (UiStyles.cs:898-906) is a static flag with +1-frame tail; RatonOcupado (l.913). Window ids in use: 837465 (DevPalette), 837480 (NamingUi), 837481 (JournalHud), 918273 (GaleriaCurador), 0x414C4B4E (TallerSesionHud).


## Hechos clave

- Flask.Capacity = 900 cells; counts per material in int[256] plus raw-temp sums restored on pour (Game/Flask.cs:87,102-103,516)
- Flask suck: SuckRadius=4, SuckRatePerTick=30, nearest-ring-first, LMB; pour: PourRadius=2, PourRatePerTick=20, RMB, densest first; dump Q/MMB DumpRadius=4, leftover destroyed (Game/Flask.cs:93-99,486-498,535-560)
- Flask reach ReachWorld=6f world units = 60 cells measured from apprentice cell, both cursor and each target cell must be within (Game/Flask.cs:97,453-459)
- Flask cannot aspirate Empty, Stone, PisoEstructural, or Fire-archetype materials (EsAspirable Game/Flask.cs:417-425; TickSuck repeats it 460-466 with feedback text)
- Flask material lock: on LMB press, material under cursor (or nearest aspirable within SuckRadius rings) is locked for the whole press; Shift = indiscriminate (Game/Flask.cs:315-320,372-413)
- Flask.Update guard order: DayCycle.InputLocked, DevPalette.IsOpen, GaleriaCurador.Abierto, FundacionDirector.FrascoBloqueado, UiStyles.EscribiendoTexto, JournalHud.Abierto||AlbumReal.Abierto, Cincel.ModoActivo, Mudanza.ModoActivo, StorageRack.RatonSobreRedoma() (Game/Flask.cs:259-303)
- Flask does NOT yield to Termometro.ModoActivo: with thermometer active LMB both plants a probe and aspirates (documented gap Game/Termometro.cs:64-75)
- Flask pours via _sim.PaintCell(x,y,mat,tempMedia) only into Empty cells; sucks via _sim.Paint(x,y,0,Empty) after reading SampleTempRaw (Game/Flask.cs:468-471,514-516)
- Flask sets apprentice work-anchor each frame (AnclaDeTrabajo) while sucking/pouring: input x0.35, brake 40 u/s^2 (Game/Flask.cs:356; Game/ApprenticeController.cs:768-773)
- Cincel: key C toggles static ModoActivo; CicloIncluyePiso=false so C cycles flask<->stone chisel only; X toggles stone/PisoEstructural fill (Game/Cincel.cs:132,156,196-236)
- Cincel radii CarveRadius=FillRadius=2, rates CarveRatePerTick=FillRatePerTick=3, ReachWorldCincel=2.2u=22 cells (Game/Cincel.cs:142-152)
- Cincel LMB carves (TallarTick) only Stone or PisoEstructural, never frame cells, refuses SimLevelBuilder.EsObraDelTaller cells with 'es obra del taller' warning; RMB builds (RellenarTick) PaintStable Stone onto Empty/Smoke/Steam (piso also replaces non-obra Stone) (Game/Cincel.cs:383-450,460-522)
- Cincel carve line-of-sight counts Stone/PisoEstructural cells along Bresenham apprentice->cursor before the disc; blocks if >= GrosorParedBloquea=3; building has NO LOS (Game/Cincel.cs:335-361,400-405,473-481)
- Cincel Update guards: InputLocked, FrascoBloqueado, DevPalette.IsOpen, GaleriaCurador.Abierto, EscribiendoTexto, JournalHud/AlbumReal.Abierto; entering chisel calls Mudanza.ForzarSalida() (Game/Cincel.cs:181-188,206)
- Movement: moveSpeed=4.8 u/s, acceleration=96 u/s^2, deliberately NOT [SerializeField] (Game/ApprenticeController.cs:192-193)
- F6 cycles ModoMovimiento {PiesYVuelo=0 default, SoloVuelo=1, SoloPies=2}, persisted PlayerPrefs 'ModoMovimiento', lazy-loaded static (Game/ApprenticeController.cs:505-508,644-668)
- Walk physics: VelocidadPaso=1.5, VelocidadCorrer=2.6 (Shift), Gravedad=25, ImpulsoSalto=10.5, GravedadCaida x1.7, coyote/buffer 0.12s, CaidaMax=12, ImpulsoDespegue=2.6 (Game/ApprenticeController.cs:670-687)
- Collision reads the grid directly (no colliders): AABB half-width 0.32u, +0.48u up, -0.64u down, chamfer 3 cells, substep 0.06u, corner assist 0.05/0.10/0.15u; static toggle ColisionConEstructura (Game/ApprenticeController.cs:774,798-806,812-853)
- Only MaterialId.Stone and MaterialId.PisoEstructural block the apprentice; Stone inside SimLevelBuilder.EsObraDelTaller is passable unless EsRepisaDeCascada (Game/ApprenticeController.cs:922-942); liquids/powders/gases are walked through
- If box starts inside solid: flying = collision suspended that frame; walking = Desenterrar searches up/sideways up to 3u then full column (Game/ApprenticeController.cs:694-712,818)
- World bounds derived: WorldMaxX = CellGrid.W*SimRenderer.CellWorldSize (76.8), WorldMaxY 28.8; bottom of world counts as floor (Game/ApprenticeController.cs:207-210,674-678)
- HeatPlate/ChillStone do not set temperature: each 30Hz tick they push temp toward target via EmisionTermica.PasoFootprint over RadioFilas=5 rows above _plateRow (rows _plateRow+1..+5) then PasoCollar over 15 more rows, writing with _sim.InyectarTemperatura (Game/HeatPlate.cs:472-495; Game/ChillStone.cs:748-771)
- EmisionTermica: NewtonK=0.05, Falloff100 quadratic ((5-fila)/5)^2, fractional step decided by XorShift.FromCell(tick,x,y,557), direction lock SoloSube/SoloBaja, CollarStepRaw=3 toward AmbientRaw (Sim/SimStepper.cs:2847-2921)
- HeatPlate targets: ArdienteRaw=220 (=320 C); Templada = centre of Universe.VivGrowMinRaw/MaxRaw (default 82) but REMOVED from E cycle since pt51 (Off<->Ardiente only) (Game/HeatPlate.cs:322,366-370,428-451)
- ChillStone targets: HelandoRaw=47 (-26 C); Fresca = min(Water.freezesAt, Universe.CrystallizeMaxTempRaw) - 10 clamped to [48, 69]; 3-state cycle Off->Fresca->Helando (Game/ChillStone.cs:341,482-489,725-741)
- Both plates hold adjacent row toward last target for HoldTicksTrasApagar=60 ticks with HoldStepRaw=1 after switching off, sign-respecting (Game/HeatPlate.cs:334-337,505-520)
- Plate footprint = max(8, round(span*0.4)) cells centred in the span given to Init; _plateRow is the floor row and effect starts at _plateRow+1 (Game/HeatPlate.cs:359-364; Game/ChillStone.cs:470-476)
- Bootstrap spawns HeatPlate under PilaPlanes[0] (water pila) and ChillStone at SimLevelBuilder.AlcobaFriaX0..X1 with BaseYDeEstacion (Game/AlkahestGameBootstrap.cs:1228-1266)
- Termometro: key G toggles static ModoActivo; LMB plants probe (max 3, FIFO by plant order), RMB removes probe at exact cell; probes persist and resample at 4 Hz when mode is off (Game/Termometro.cs:81-86,255-330)
- Temperature display: CellGrid.RawToC(raw) = raw*2-120; CToRaw inverse; AmbientRaw=70 = 20 C; Termometro caches 256 strings raw+'°' (Sim/CellGrid.cs:92,192-194; Game/Termometro.cs:130-136)
- Termometro colour thresholds: <=0 C Frio, >=90 Peligro, >=40 Aviso, else Texto (Game/Termometro.cs:338-344); shows '—' if _sim.Stepper==null (guest mirror)
- Dispenser EmitTick: budget EmitRatePerTick=12 per 30Hz tick, disc SpoutRadius=1 at (_spoutX,_spoutY), PaintStable only into Empty; if nothing free, one PaintStable drop at first Empty up to OverflowSearchUp=8 rows above; else _rebosando=true (Game/Dispenser.cs:110-111,702-750)
- Dispenser spout = mount + spoutOffsetCells (default 5) in x, mount - SpoutDropCells(2) in y; racionCeldas>0 auto-closes after that many cells (lab taps use 45) (Game/Dispenser.cs:118-137,331-350; Game/AlkahestGameBootstrap.cs:1321-1332)
- Dispenser E toggle costs favorCostPerActivation once via OrderSystem.SpendFavor; Bloqueado seals until Desbloquear() (Game/Dispenser.cs:642-672,685-690)
- DevPalette exists only when Application.isEditor || Debug.isDebugBuild (IsDevBuild); F3 toggles, state in PlayerPrefs 'TenThousandYears_DevPalette' (Dev/DevPalette.cs:41,113-119,138-141,151)
- DevPalette P pauses by Time.timeScale=0 and restores _lastTimeScale; N calls _sim.StepOnce() only when window visible; 0.5x/1x/2x/4x buttons set Time.timeScale (Dev/DevPalette.cs:153-164,282-292)
- DevPalette brush: radius slider 1..10, LMB _sim.PaintStable(cell, radius, mat), RMB _sim.Paint(cell, radius, Empty); no hover/paint when mouse over window rect or when EscribiendoTexto/JournalHud.Abierto (Dev/DevPalette.cs:167-215,221-226)
- DevPalette static IsOpen = _visible && IsDevBuild is the guard consumed by Flask/Cincel/Termometro (Dev/DevPalette.cs:39,148)
- DevPalette hover readout: devName, id, °C via RawToC(grid.temp[idx]), aux, '· OBRA' suffix from SimLevelBuilder.EsObraDelTaller (Dev/DevPalette.cs:295-311)
- AlkahestSim.Paused (bool) freezes stepping+render without touching Time.timeScale; AlkahestSim.Update accumulates Time.deltaTime at FixedDt=1/30 with MaxStepsPerFrame=2, so Time.timeScale>2 cannot exceed 2 sim ticks per frame (AlkahestSim.cs:42-43,58-66,330-355)
- AlkahestSim.Paint/PaintCell/PaintStable/PaintRect/InyectarTemperatura all skip cells with x<=0||x>=W-1||y<=0||y>=H-1 and call WakeChunk; PaintStable uses StableBirthTempRaw(material) (AlkahestSim.cs:527-575,697-777)
- SimLevelBuilder.ObraDelTaller is a static List<RectObra>; RegistrarObra(x0,y0,x1,y1) returns handle; EsObraDelTaller(x,y); EsRepisaDeCascada(x,y); WallThickness=3 (Sim/SimLevelBuilder.cs:395,2199,2237,2261,2967)
- Gallery mode: AlkahestGameBootstrap.ModoGaleria static -> SimLevelBuilder.BuildGaleria(grid) (AlkahestSim.cs:244) and SpawnGaleria (backdrop+apprentice+OrderSystem+DayCycle+GaleriaCurador.Crear) with spawn at GaleriaSpawnX=70,Y=174 (Game/AlkahestGameBootstrap.cs:120,428,1015-1037; Sim/SimLevelBuilder.cs:3051,3060)
- DayCycle.HudSilenciado is forced true in ModoFundacion||ModoGaleria (Game/DayCycle.cs:556); most OnGUI start with `if (InputLocked || HudSilenciado) return`
- Keyboard guard pattern (R12): `bool tecladoLibre = !UiStyles.EscribiendoTexto && !JournalHud.Abierto && !AlbumReal.Abierto && !DayCycle.InputLocked;` (Game/GaleriaCurador.cs:122; ApprenticeController.cs:480; PielDeRoca.cs:174)
- UiStyles.EscribiendoTexto getter is true while flag set OR for 1 frame after it is cleared (Time.frameCount <= _frameFinEscritura+1) (Game/UiStyles.cs:898-909)
- UiStyles.RatonOcupado = any mouse button pressed; used to hide 'E —' prompts (Game/UiStyles.cs:913-921)
- Window ids in use: DevPalette 837465, NamingUi 837480, JournalHud VentanaBautizoPatenteId 837481, GaleriaCurador 918273, TallerSesionHud 0x414C4B4E ('ALKN') (Dev/DevPalette.cs:31; Game/NamingUi.cs:113; Game/JournalHud.cs:249; Game/GaleriaCurador.cs:38; Net/TallerSesionHud.cs:37)
- PlayerPrefs keys in use: 'ModoMovimiento', 'TenThousandYears_DevPalette', 'TenThousandYears_AudioSilenciado', 'TenThousandYears_VolEfectos', 'TenThousandYears_VolGeneral', 'PielDeRoca.Nivel' (ApprenticeController.cs:650; DevPalette.cs:41; Audio/DirectorDeAudio.cs:158-159; Game/DayCycle.cs:278; Game/PielDeRoca.cs:43)
- Keys taken (wasPressedThisFrame grep): E, Esc, R, Q, PgUp/PgDn, Enter/NumpadEnter, N, G, B, X, W, V, T, Space, P, O, Numpad1-4, -, =, M, L, J, H, F3, F6, F7, F8, F9, F10, digits 1-4, C, arrows/WASD (Shift, Ctrl modifiers). Free F-keys: F1, F2, F4, F5, F11, F12
- MachineFocus: IMaquinaInteractiva {PuntoFoco, RangoFoco}; Registrar/Olvidar/Limpiar; EsFoco(m, playerTransform) picks nearest per frame; MostrarPromptE true until RegistrarUsoE called twice (Game/MachineFocus.cs:7-104)
- Mudanza (key V / middle button) provides IMovible {CentroMundo, TamanoMundo, AnclaCelda, CabeEnAncla, Reposicionar}, RegistrarMovible/OlvidarMovible, static ModoActivo, ForzarSalida() (Game/Mudanza.cs:41,275,379-386,1015)
- AtrilDeEmotes.Avisar(string texto, float segundos) is the on-screen notice channel used for F6/F7 mode changes; Flask.Avisar(msg) is the cursor feedback channel used by Cincel/Termometro/StorageRack (Game/AtrilDeEmotes.cs:164; Game/Flask.cs:69)
- Capas.cs sorting-order table: Simulacion -5, FxOverlay -4, MaquinaAtras 14, MaquinaBase 17, MaquinaCuerpo 18, MaquinaDetalle 21, MaquinaFrente 35, Halos 40, Personaje 50, ArquitecturaFrente 55, Foreground 58, CarryEnMano 60 (Game/Capas.cs:43-63)

## APIs / ganchos

- AlkahestSim.Paint(int x,int y,int radius,byte materialId) — AlkahestSim.cs:527 (disc, keeps temp, skips frame, wakes chunk)
- AlkahestSim.PaintCell(int x,int y,byte materialId,byte tempRaw) — AlkahestSim.cs:566
- AlkahestSim.PaintStable(int x,int y,int radius,byte materialId) — AlkahestSim.cs:697 (birth at StableBirthTempRaw)
- AlkahestSim.PaintRect(int x0,int y0,int width,int height,byte materialId) — AlkahestSim.cs:730
- AlkahestSim.InyectarTemperatura(int x,int y,byte tempRaw) — AlkahestSim.cs:771 (temp only, not replicated)
- AlkahestSim.SampleMaterial(int x,int y):int — AlkahestSim.cs:480; AlkahestSim.SampleTempRaw(int x,int y):byte — AlkahestSim.cs:487
- AlkahestSim.WorldToCell(Vector3):Vector2Int — AlkahestSim.cs:780; CellToWorld(Vector2Int):Vector3 — AlkahestSim.cs:787 (cell centre)
- AlkahestSim.StepOnce() — AlkahestSim.cs:795; AlkahestSim.Paused {get;set;} — AlkahestSim.cs:66; AlkahestSim.Universe/Grid/Stepper/Renderer — AlkahestSim.cs:52-55; static int? NextRunSeed — AlkahestSim.cs:40
- SimStepper.Tick:uint, ActiveCells, ActiveChunks, LastStepMs:double, Step() — Sim/SimStepper.cs:39-42,257
- EmisionTermica.PasoFootprint(int cur,int target,int fila,uint tick,int x,int y,Direccion) — Sim/SimStepper.cs:2894; PasoCollar(int cur) — 2916; Falloff100(int fila) — 2863; enum Direccion {SoloSube,SoloBaja} — 2892
- CellGrid.RawToC(byte raw):int = raw*2-120 — Sim/CellGrid.cs:192; CellGrid.CToRaw(int) — 194; CellGrid.InBounds, CellGrid.Idx, grid.temp[], grid.aux[], grid.GetMat(idx)
- Flask: public const int Capacity=900, SuckRadius=4, SuckRadiusWorld, ReachWorld=6f; Total, GetCount(byte), TempMediaDe(byte), MaterialDominante(), Extraer(byte mat,int n,out byte temp), Guardar(byte mat,int n,byte temp), Avisar(string), Feedback/FeedbackUntil, TieneMaterialBloqueado, MaterialBloqueado, EstaAspirando, ModoIndiscriminado, Init(AlkahestSim) — Game/Flask.cs:64-70,87,93-97,118-176,182
- Cincel: static bool ModoActivo {get; private set;}; Init(AlkahestSim) — Game/Cincel.cs:132,176
- Termometro: static bool ModoActivo; Init(AlkahestSim) — Game/Termometro.cs:81,152
- ApprenticeController: static AprendizLocal; ControlDelJugador; CarryAnchor:Vector3; EnSuelo; Volando; Anclado; AnclaDeTrabajo(bool); static Modo; static NombreModo(ModoMovimiento); static bool ColisionConEstructura; PulsoDelFrasco(float); InclinacionDelFrasco(float); AplicarTinte(Color); ReproducirGesto(HojaDeCuadros,float) — Game/ApprenticeController.cs:80,90,383,397,406,639-643,644-663,774,772-773
- HeatPlate.Init(AlkahestSim sim, Transform player, int cellX0, int cellX1, int plateRow) — Game/HeatPlate.cs:352; Reposicionar(Vector2Int); IMaquinaUsableRemota.UsarPorRed()/EstadoVivoRed(); PuntoFoco/RangoFoco; CentroMundo/TamanoMundo/AnclaCelda/CabeEnAncla
- ChillStone.Init(AlkahethSim sim, Transform player, int cellX0, int cellX1, int plateRow) — Game/ChillStone.cs:462; same interfaces as HeatPlate
- Dispenser.Init(AlkahestSim sim, Transform player, int mountCellX, int mountCellY, byte materialId, OrderSystem orderSystem=null, int favorCost=0, bool bloqueado=false, int spoutOffsetCells=5, int racionCeldas=0) — Game/Dispenser.cs:331; Desbloquear(); Bloqueado; Material; Reposicionar(Vector2Int)
- DevPalette: static bool IsOpen — Dev/DevPalette.cs:39 (only consumer-visible API)
- GaleriaCurador: static bool Abierto; static Crear(AlkahestSim, ApprenticeController) — Game/GaleriaCurador.cs:41,108
- UiStyles: Preparar(); S(float px); F(int px); Escala; Panel(Rect)/Panel(Rect,Color fondo,Color borde); PanelRito(Rect); MarcoLaton(Rect[,Color,float]); FileteRombo; Barra(Rect,float,Color); Rellenar(Rect,Color); Globo(Vector2,string,Color); EtiquetaMundo(Vector3,string,Color,float); PlacaMundo(Vector3,string,Color,float desplazarPx); PlacaMundoLateral(Vector3,string,Color,float sepPx,float dyPx,float alfa,bool aLaIzquierda); Cercania(Vector3,Transform,float,float); Ancho(GUIStyle,string); Alto(GUIStyle,string,float); Espaciar(string); static bool EscribiendoTexto {get;set;}; static bool RatonOcupado; styles Titulo/Cuerpo/CuerpoLinea/Chip/ChipMini/Boton/Campo/Slider/SliderThumb/TituloRito etc. — Game/UiStyles.cs:104-133,142,368-402,493-504,534,561-563,601,621,650,697,741,821,870,898,913
- MachineFocus.Registrar/Olvidar/Limpiar/EsFoco/MostrarPromptE/RegistrarUsoE; interface IMaquinaInteractiva {Vector3 PuntoFoco; float RangoFoco;} — Game/MachineFocus.cs:7-104
- Mudanza.RegistrarMovible(IMovible)/OlvidarMovible/ForzarSalida()/ModoActivo; interface IMovible — Game/Mudanza.cs:41,275,379,386,1015
- SimLevelBuilder.RegistrarObra(int x0,int y0,int x1,int y1):int handle; EsObraDelTaller(int,int); EsRepisaDeCascada(int,int); ObraDelTaller list; WallThickness=3; BuildGaleria(CellGrid); BuildFundacion; BuildCuartoIntimo; GaleriaSpawnX/Y — Sim/SimLevelBuilder.cs:395,2199,2237,2261,2446,2967,3051,3060,3153
- AlkahestGameBootstrap static ModoSemillaCero/ModoFundacion/ModoGaleria — Game/AlkahestGameBootstrap.cs:100,112,120; SpawnApprentice — 1120; SpawnGaleria — 1015
- DayCycle.InputLocked / DayCycle.HudSilenciado (static get; private set) — Game/DayCycle.cs:117,143
- AtrilDeEmotes.Avisar(string texto, float segundos) — Game/AtrilDeEmotes.cs:164; AtrilDeEmotes.Crear(ApprenticeController) — 41
- ParticulasFx.Activas static bool toggle — used by DevPalette Dev/DevPalette.cs:275

## Estado por celda / chunk

- CellGrid mat (byte per cell, via grid.GetMat(idx)/SetCell) — read by SampleMaterial (AlkahestSim.cs:480), written by Paint/PaintCell/PaintStable/PaintRect
- CellGrid.temp[] byte per cell raw 0..255 (C = raw*2-120), initialised to AmbientRaw=70 (Sim/CellGrid.cs:187) — read by SampleTempRaw, Flask._tempSum accumulation (Flask.cs:468), Termometro, DevPalette hover; written by PaintCell/PaintStable/InyectarTemperatura and HeatPlate/ChillStone ticks
- CellGrid.ambient[] byte per cell, initialised to AmbientRaw (Sim/CellGrid.cs:181; comment 'today uniform') — not touched by any tool read here
- CellGrid.aux[] byte per cell — shown in DevPalette hover (Dev/DevPalette.cs:302,309); OrganicDormantAux=0x40 const (CellGrid.cs:126)
- Chunk wake state: grid.WakeChunk(x,y,tick) called by every Paint*/InyectarTemperatura; SleepTicks=30, chunk 16x16, 48x18 chunks (Sim/CellGrid.cs:59-68)
- Flask per-material state (not per cell): int[256] _counts, int[256] _tempSum, _total (Game/Flask.cs:102-104)
- Termometro probes: bool[3] _sondaActiva, int[3] _sondaX/_sondaY, byte[3] _sondaRaw, string[3] _sondaLabel, int[3] _sondaOrdenPlantado, float[3] _sondaAcumulador (Game/Termometro.cs:104-112)
- SimLevelBuilder.ObraDelTaller: static List<RectObra> of protected rects consulted per cell by Cincel and ApprenticeController collision (Sim/SimLevelBuilder.cs:2199,2261)
- HeatPlate/ChillStone per-instance: _cellX0,_cellX1,_plateRow,_state,_accumulator,_lastActiveTarget,_holdTicksRestantes (HeatPlate.cs:341-352; ChillStone.cs:394-403)
- Dispenser per-instance: _spoutX,_spoutY,_matId,_on,_accumulator,_racionCeldas,_emitidasEstaApertura,_rebosando (Dispenser.cs:127-131,181-190)

## Constantes

- Sim tick 30 Hz: FixedDt=1/30, MaxStepsPerFrame=2 in AlkahestSim (AlkahestSim.cs:42-43) and every tool/machine (Flask.cs:89-90; Cincel.cs:134-135; HeatPlate.cs:317-318; ChillStone.cs:335-336; Dispenser.cs:78-79)
- SimRenderer.CellWorldSize=0.1 (world unit per cell); CellGrid.W=768, H=288, CHUNK=16, PantallaW=256, PantallaH=144, SleepTicks=30 (Sim/CellGrid.cs:56-68)
- CellGrid.AmbientRaw=70 (=20 C); RawToC = raw*2-120 (Sim/CellGrid.cs:92,192)
- Flask: Capacity=900, SuckRadius=4, SuckRatePerTick=30, PourRadius=2, PourRatePerTick=20, DumpRadius=4, ReachWorld=6f (60 cells), AvisosMax=2 (Game/Flask.cs:80,87,93-97)
- Flask juice: MotasMax=32, CeldasPorMota=8, AnticipacionSeg=0.10, PulsoResorte=170 (dt capped 0.05), sorting 40/41 beam, 42/53 motas (Game/Flask.cs:618,652-705,722-737)
- Cincel: CarveRadius=2, CarveRatePerTick=3, FillRadius=2, FillRatePerTick=3, ReachWorldCincel=2.2f (22 cells), GrosorParedBloquea=3, CicloIncluyePiso=false (Game/Cincel.cs:142-156,335)
- Movement: moveSpeed=4.8, acceleration=96, VelocidadPaso=1.5, VelocidadCorrer=2.6, Gravedad=25, GravedadCaida=1.7, GravedadApice=0.55, ApiceVy=1.2, ImpulsoSalto=10.5, CorteSalto=0.45, CoyoteSeg=0.12, BufferSaltoSeg=0.12, CaidaMax=12, ImpulsoDespegue=2.6, SondaSuelo=0.08, FrenoAncla=40, InputAnclado=0.35 (Game/ApprenticeController.cs:192-193,670-687)
- Collision box: MedioAnchoColision=0.32, MedioAltoArriba=0.48, MedioAltoAbajo=0.64, SubPaso=0.06, ChaflanCeldas=3, AsistenciaEsquina={0.05,0.10,0.15} (Game/ApprenticeController.cs:798-806)
- HeatPlate: ArdienteRaw=220 (320 C), _templadaRaw default 82, FootprintFraction=0.4 (min 8 cells), HoldStepRaw=1, HoldTicksTrasApagar=60, ProximityRange=3.2, RangoEstadoPleno/Desvanece=5.0/6.5, RangoNombrePleno/Desvanece=2.6/3.6, OficioDuracionSeg=6 (Game/HeatPlate.cs:321-338,366,394-401)
- ChillStone: HelandoRaw=47 (-26 C), FrescaMarginRaw=10, _frescaRaw default 45, FootprintFraction=0.4, HoldStepRaw=1, HoldTicksTrasApagar=60, ProximityRange=3.2 (Game/ChillStone.cs:338-391)
- EmisionTermica: RadioFilas=5, NewtonK=0.05, CollarFilas=15, CollarStepRaw=3, XorShift salt 557 (Sim/SimStepper.cs:2850-2858)
- Termometro: MaxSondas=3, SondeoSondaHz=4, PinSortingOrder=41, ModeIconSortingOrder=62, thresholds 0/40/90 C (Game/Termometro.cs:83-85,171-172,338-344)
- Dispenser: EmitRatePerTick=12, SpoutRadius=1, SpoutOffsetCellsDefault=5, SpoutDropCells=2, OverflowSearchUp=8, InsufficientFavorFlashSeconds=1.5, ProximityRange=3.2, lab taps racionCeldas=45 (Game/Dispenser.cs:83,110-111,129,140-144; AlkahestGameBootstrap.cs:1328-1332)
- DevPalette: WindowId=837465, Rect(12,180,300,480), brush radius default 3 range 1..10, speeds 0.5/1/2/4x (Dev/DevPalette.cs:31,47,50,282-292)
- UiStyles: Escala = clamp(Screen.height/720, 1, 2.4); AlfaMinimaVisible=0.12; MargenFueraDeCuadro=S(24); MarcoBorde=5; window padding (14,14,20,14) (Game/UiStyles.cs:148,425,455,301-306)
- Sorting orders (Capas.cs): Simulacion -5, FxOverlay -4, MaquinaCuerpo 18, MaquinaFrente 35, Halos 40, Personaje 50, ArquitecturaFrente 55, Foreground 58, CarryEnMano 60; Cincel beam/ring 38/39, icon 61; Termometro pin 41, icon 62
- Gallery spawn: GaleriaSpawnX=70, GaleriaSpawnY=174; welcome brasa PaintStable(110,170,r2,Brasa)+Fire raw 220 at (109..111,173) (Sim/SimLevelBuilder.cs:3051; AlkahestGameBootstrap.cs:1034-1035)
- MaterialId: Stone=1, Water=3, PisoEstructural=65, Count=66 (Sim/Universe.cs:17,19,107,109)

## Riesgos

- Time scaling via Time.timeScale (DevPalette pattern) is capped by MaxStepsPerFrame=2 in AlkahestSim.Update AND in every machine/tool accumulator: >2x speed silently runs 2 ticks/frame max and heat plates/dispensers desync from the sim rate only if their own caps differ; stress benchmarks that want N ticks must call _sim.StepOnce() in a loop or add a sim-side multiplier (AlkahestSim.cs:42-43,344-355)
- Time.timeScale=0 pause (DevPalette) still lets Flask/Cincel act? Flask.Update returns early only when DevPalette.IsOpen; with scale 0 Time.deltaTime=0 so accumulators do not advance — fine, but AlkahestSim.Paused is the project's own pause used by DayCycle; a LabPanel should prefer Paused to avoid fighting DayCycle (AlkahestSim.cs:58-66)
- Every world-input tool checks DevPalette.IsOpen and GaleriaCurador.Abierto but NOT any new panel flag: a LabPanel that captures clicks must either add its own static Abierto to Flask/Cincel/Termometro/Mudanza guards (editing those files) or reuse an existing guard flag (Flask.cs:264-266; Cincel.cs:184-185; Termometro.cs:174)
- Termometro is not in Flask's guard list: LMB with thermometer active also aspirates (Termometro.cs:64-75)
- DevPalette only exists in editor/dev builds (IsDevBuild); relying on its IsOpen in release builds is a no-op (Dev/DevPalette.cs:151)
- PlacaMundo/OnGUI machine labels early-return when DayCycle.HudSilenciado, which is forced true in ModoGaleria (DayCycle.cs:556): a lab built on ModoGaleria gets no machine labels, no Termometro readout (Termometro.cs:406), no Flask HUD unless HudSilenciado is cleared (movement/click clears it at DayCycle.cs:466)
- Modo flags are mutually exclusive statics on AlkahestGameBootstrap (ModoGaleria/ModoFundacion/ModoSemillaCero) and AlkahestSim chooses the level builder from them at AlkahestSim.cs:244-246; a second gallery needs its own flag+BuildX branch or reuse of ModoGaleria
- Deterministic hazards: EmisionTermica uses XorShift.FromCell(tick,x,y,557) so fractional pushes are tick-deterministic, but the plates run on Unity Time.deltaTime accumulators (frame-dependent tick count) and InyectarTemperatura is NOT replicated in multiplayer mirror (AlkahestSim.cs:760-777)
- Player collision only against Stone/PisoEstructural: cohesive solid bodies made of other materials will not block the apprentice; ObraDelTaller Stone is walk-through unless EsRepisaDeCascada (ApprenticeController.cs:922-942)
- Cincel refuses to carve any cell inside SimLevelBuilder.ObraDelTaller rects: any lab machinery registered there becomes uncarvable; conversely unregistered stone is fully carvable including load-bearing terrain (Cincel.cs:421-440)
- Flask.Extraer/Guardar clamp _tempSum to >=0 and TempMediaDe integer-divides: raw temperature averaging loses precision across mixed hot/cold batches (Flask.cs:141-176)
- Flask pours only into Empty cells within PourRadius=2; a dense turbid-water or sediment material must be Empty-displacing or pours fail silently with haz retraction (Flask.cs:508)
- DumpAll destroys what does not fit: mass is not conserved (Flask.cs:560)
- Dispenser PaintStable births at StableBirthTempRaw, ignoring local temperature — a permanent hot/cold stream must set temp separately (Dispenser.cs:718)
- HeatPlate effect column is fixed to rows _plateRow+1..+20 above the plate x-span; the collar (rows 6-20) actively pulls toward ambient at 3 raw/tick, which will fight any nearby experiment heat source or steam column above a plate (HeatPlate.cs:472-495; SimStepper.cs:2916-2921)
- HeatPlate direction lock SoloSube means it never cools; ChillStone SoloBaja never heats — a 'permanent fire' via HeatPlate reaches at most raw 220 (320 C) with Newton decay, never instantly
- Machine sprites are created once in BuildVisual; calling Init twice duplicates layers (HeatPlate.cs:411-449 docblock) — use Reposicionar to move
- IMGUI window ids must be unique constants, never GetInstanceID (DevPalette.cs:29-31); taken: 837465, 837480, 837481, 918273, 0x414C4B4E
- Keys already bound include G (thermometer AND gesture), C, X, V, E, Q, R, N, P, T, M, H, J, O, L, B, F3, F6-F10, digits 1-4, -/=; F8/F10/R/C also used by GaleriaCurador in gallery mode
- UiStyles.EscribiendoTexto must be set true/false by any panel that owns a text field, else WASD/letters leak into movement and shortcuts (UiStyles.cs:883-909)
- GUI styles must be built inside OnGUI (Preparar checks Event.current); UiStyles.VestirSkin mutates the shared GUI.skin once with HideAndDontSave textures (UiStyles.cs:142-146,220-226)

## Oportunidades

- Sibling LabPanel can copy DevPalette exactly: static IsOpen-like flag, PlayerPrefs-remembered visibility, GUILayout.Window with unique const id, IsOverWindow rect test using Screen.height - mouse.y, GUI.DragWindow(new Rect(0,0,10000,20)), pause via Time.timeScale or better AlkahestSim.Paused, StepOnce for single tick, speed buttons (Dev/DevPalette.cs:113-164,221-226,236-352)
- Live stats already exposed for a benchmark readout: Stepper.LastStepMs, ActiveChunks, ActiveCells, Tick, Universe.Seed, unscaled FPS (Dev/DevPalette.cs:242-244)
- Permanent underground stream: instantiate a Dispenser via Init(sim, player, mountX, mountY, MaterialId.Water, null, 0, false, spoutOffsetCells, racionCeldas:0) and call ToggleRequested through UsarPorRed (IMaquinaUsableRemota) to open it without proximity; or emulate EmitTick with PaintStable at 12 cells/tick (Dispenser.cs:331,702-750)
- Permanent fire/evaporation: HeatPlate.Init(sim, player, x0, x1, floorRow) + cast to IMaquinaUsableRemota.UsarPorRed() to force Ardiente (raw 220) — its Newton push + 5-row footprint boils water within ~6 s at 3 cells (HeatPlate.cs:472-495 docblock); or the gallery's brasa+Fire recipe PaintStable(x,y,2,MaterialId.Brasa) + PaintCell(...,MaterialId.Fire,220) (AlkahestGameBootstrap.cs:1034-1035)
- Condensation by cooling: ChillStone.Init + UsarPorRed twice for Helando (raw 47, -26 C); Fresca is auto-calibrated below water.freezesAt (ChillStone.cs:482-489)
- Reusable thermal model for any new emitter: EmisionTermica.PasoFootprint/PasoCollar + AlkahestSim.InyectarTemperatura (SimStepper.cs:2894-2921; AlkahestSim.cs:771)
- Temperature probing UI for free: Termometro probes (G) persist across modes; the 256-string label table and ColorPorC can be reused for a panel readout (Termometro.cs:130-136,338-344)
- Terrain carving already player-driven: Cincel carve/build with ObraDelTaller reserves; to protect lab fixtures call SimLevelBuilder.RegistrarObra(x0,y0,x1,y1) (Sim/SimLevelBuilder.cs:2237); to let the player carve everything, register nothing
- Turbid water / sediment pickup: Flask filters only by Stone/PisoEstructural/Fire, so any new Powder/Liquid material is aspirable and pourable at its stored temperature automatically (Flask.cs:417-425)
- Bulk scene painting for presets: AlkahestSim.PaintRect(x0,y0,w,h,mat) and PaintStable(x,y,radius,mat) with automatic chunk wake (AlkahestSim.cs:697-756)
- Gallery mode is the closest template for a lab scene: ModoGaleria -> SimLevelBuilder.BuildGaleria(grid) + SpawnGaleria (apprentice with all tools, DayCycle, curator window) (AlkahestGameBootstrap.cs:1015-1037; AlkahestSim.cs:244)
- GaleriaCurador shows an F-key panel pattern with catalogue buttons, radius -/+ keys, Ctrl+1..9 teleports (apprentice.transform.position set directly), F10 screenshot round via ScreenCapture (GaleriaCurador.cs:108-170,480-530)
- Movement modes selectable at runtime via ApprenticeController.Modo/F6 and ColisionConEstructura static for a no-clip lab mode (ApprenticeController.cs:644-668,774)
- UiStyles primitives for a themed panel: Panel, PanelRito, MarcoLaton, Barra (progress), Slider/SliderThumb styles for parameter sliders, Boton, Campo, ChipMini; S() scaling (UiStyles.cs:104-133,493-640)
- Feedback channels: Flask.Avisar(string) near cursor (1.5 s), AtrilDeEmotes.Avisar(text, seconds) for mode notices (Flask.cs:64-70; AtrilDeEmotes.cs:164)
- Wake-on-write already handled: every Paint*/Inyectar call wakes the chunk, so a stress benchmark painting matter never has to touch chunk sleep state
- Machine focus/E interaction and Mudanza relocation come for free by implementing IMaquinaInteractiva/IMovible and registering (MachineFocus.cs:41; Mudanza.cs:379)

## Preguntas abiertas

- StableBirthTempRaw(material) implementation and how temp interacts with a new turbid-water/sediment material (AlkahestSim.cs ~600-690 not read)
- Whether Time.timeScale>2 should be supported by raising MaxStepsPerFrame in AlkahestSim or by a loop of StepOnce for benchmarks; what SimStepper.Step costs at full 768x288 activity (LastStepMs shown but no budget documented here)
- How SimLevelBuilder.BuildGaleria lays out its 9 areas and whether a second gallery should be a new Modo flag or a sub-area of ModoGaleria (Sim/SimLevelBuilder.cs:3060-3150 not read)
- Whether DayCycle.HudSilenciado (forced true in ModoGaleria) should be cleared at spawn for a lab so PlacaMundo/Termometro/FlaskHud draw immediately
- Whether Flask/Cincel/Termometro/Mudanza guard lists may be edited to add a LabPanel.Abierto flag (file ownership rules in CLAUDE.md), or whether the panel must reuse GaleriaCurador.Abierto/DevPalette.IsOpen
- EmisionTermica collar pulls toward CellGrid.AmbientRaw (uniform 70) — is per-cell ambient[] ever non-uniform, and would a lab with a cold zone need a per-zone ambient?
- Does any material already model 'turbid water' or sediment settling (Powder in Liquid) in Universe/SimStepper, or must it be added?
- Multiplayer mirror (ModoEspejo) implications if the lab is ever hosted: InyectarTemperatura is not replicated and Stepper==null on guests
- XorShift.FromCell salt registry: docblock says salts up to 553 were in use before 557; a new stochastic system needs a fresh salt verified by grep
