# MAPA DEL MOTOR — lector `history_rounds`

*(Generado por un agente lector el 2026-09-03 sobre el HEAD 371dea4. Referencias archivo:línea. Es contexto, no dogma.)*


## Resumen

Source: C:/JuegosUnity/UnityAI_Test/Alkahest/docs/archivo/HISTORIAL_RONDAS.md (6763 lines; all line refs below are to this file). It is a chronological log of ~130 rounds. What EXISTS that the laboratory can reuse:

WATER/LIQUIDS. Falling-sand liquids with diagonal corner stepping; the prologue already has a permanent SPRING+CASCADE (manantial in the left wall, two ledges, three free drops into a POZA) fed by the director at a fixed cadence and closed by RezumarPoza (poza seeps 1 cell/frame through its floor once above a cap) — equilibrium 34→48 cells (R73 4520-4522, R64 3815-3823, R74 4580). Water/liquid LEAK lessons are all documented: liquids escape DIAGONALLY over a wall whose top is flush with the interior (fix: walls one row higher, R86 5061-5065), over container lips with vacuum below (fix: container seals its own floor row, R85 4992-4999), through a corridor at water-surface level (poza drained 1972 cells in ~1 min, R129 6750-6755), and a bite in rock beside a ledge turns a stream into a diagonal staircase that floods the floor (R74 4574-4579). REFILL machinery exists (DepositoDeAgua.ActivarRefill): drop born at glass top, quadratic cadence 0.8→~4.8 s (~110-180 s to fill), tope = Capacidad() read from the glass, 4-cell "gota gorda", random-column rain for water (R88/90/91/112/116). Steam is real gas: Water + cima ≥ boilsAt → Steam (R41 2851), convection by low-frequency hash rumbo (salt 551, 8x8 blocks, ~0.5 s), 30% undulation, ceiling-escape order, pockets under roofs (60% lateral pressure) with half-decay (R39 2733-2737). The prologue v1 "gotera" dripped water (PaintStable, 3-cell thread every 1.15 s) directly on brasas at 165 raw and evaporated for real (vapor=4) (R60 3668-3675); it was later moved to the poza precisely because each drop boiled (R64 3815). Condensation exists as a phase branch (condensesAt, ApplyPhase, 1072, 2015) but no round tuned condensation on cold geometry.

TEMPERATURE. Byte temp per cell, raw = 60 + °C/2 (ambient raw 70 = 20 °C; raw 20 = −80 °C; raw 220 = 320 °C). SimStepper.DiffuseTemperature: neighbor diffusion diff/4 and a FIXED ±1-raw pull toward CellGrid.ambient every ~32 ticks; R9 fixed two bugs (ratchet covering only 1/8 of cells; asymmetric >>2 rounding) (1987-2003). CellGrid.ambient is a per-cell byte kept as the vehicle for player-made local climate after zone climate was removed (R17 757-763). Sim.EmisionTermica (R44) gives plates Newton-style falloff plus a 15-row "collar" that returns the edge to ambient (without it diffusion saturated the whole room in 3000 ticks) and AlkahestSim.InyectarTemperatura; measured: boil 3 cells from ARDIENTE in 6-8 ticks, freeze 3 cells from HELANDO (−26 °C) in 22-87 ticks, gradient ≤2 °C at 12 cells (2983-2992). Termometro (G) gives °C readout and 3 probes (2994-2998). Anything that CREATES matter must use PaintStable (rule 22): Paint/SetCell keep the old temp (water born frozen, painted ice melting, R13/R17).

SEDIMENT/MUD. "Lodo" is clay powder (Powder archetype: piles, every exposed face slides, R48 3258-3266); barbotina = clay + water via decreed solubility (R56 3604-3607, R79 4824-4830); limo (slime/"lodo de cantera") is a turbid suspension that precipitates base powders when heated to raw 112 (R25 2229-2231). "El lodo es arcilla en polvo: SE APILA — el contraste con el agua es el aprendizaje" (R73 4527). A permanent mud drip with mound cap + hysteresis exists (R73, R113: refills up to 70/cycle; R114 two cracks; R115 cosmetic randomness via System.Random seed 913). No real infiltration/permeability/erosion/deposition-sealing exists; patina "mojado" was turned off because it promised filtration the sim does not do (R44 2975-2977).

COHESIVE SOLIDS. Gravity-with-cohesion for fabricated solids since R29: a solid holds if supported below or within K cells horizontally through continuous solid; falls 1 cell/tick only into vacuum, liquids support, no lateral slide, only awake chunks (2529-2541). StaticSolid (Stone/Crystal/Ice) has NO gravity by design (1822-1830). Rigid bodies Noita-style were rejected (2697-2698). Anclajes 2x2 IMovible act as tallied stone (R34 2616-2621).

VEGETATION. Vivium GrowthTick exists (tip-only dendritic growth, band 30-60 °C ±15, 75% chance per attempt every 4 ticks, dormant flag 0x40, dies >120 °C, tolerance 2-3 never 1) (1961-1966, 465-491); Criatura/Capullo are parked, not deleted (2250-2253). PielDeRoca draws decorative moss/roots/stalactites (6557).

PERF. 256x144: 0.2-1.0 ms/tick (19). 768x288: headless bench Tools~/BenchSim/Harness.cs (R38) worst case 74,000 active water cells 5.5 ms avg / 11.6 peak of 33.3 budget; in-game 2-5% (2691-2695); 6 scenarios (cascada/diluvio/incendio/arena/mixto/incendio sostenido) table at 2764-2770. Three world-proportional passes (full texture refresh, MorphTick, DiffuseTemperature) were made viewport-aware in R15 (868-871). PielDeRoca: 0.035 ms/chunk, 30 ms full world, 140k verts (6558-6560).

GALLERY. The first gallery exists: SimLevelBuilder.BuildGaleria (9 areas incl. "poza con bolsa de goteo real" and a terrarium of 8 material cubetas), ModoGaleria flag (Fundación pattern), GaleriaCurador (F8; place fire/matter/rock/machines/plates/water tap/balda, stamp by radius, Ctrl+1..9 teleports, F10 capture round, R hot-loads rock texture), LA BODEGA archive room, fogatas revived every 5 s (R127-128 6619-6724). No time scaling or in-game stress benchmark exists; the only pause is AlkahestSim.Paused.


## Hechos clave

- Grid is 768x288 (3x2 screens of 256x144), CHUNK=16, 48x18 chunks — HISTORIAL_RONDAS.md:863-866 (R15)
- Temperature raw mapping raw = 60 + °C/2: ambient raw 70 = 20 °C (1063), raw 20 = −80 °C (1158), raw 220 = 320 °C (1160), raw 150 = 180 °C (2262), raw 112 ≈ 104 °C limo separation (2229-2231)
- Water freezesAt = CToRaw(waterFreezeC), waterFreezeC uniform −15..15 → raw 52..67, always ≥3 raw below ambient: ambient can never freeze water in any seed — 748-753 (R17); Ice.meltsAt = CToRaw(waterFreezeC+5) — 1064-1066
- Water boils up to raw 119 in the worst seed; crisol rescoldo tier0 = raw 120 'hierve TODO lo acuoso en toda seed' — 2229-2231, 2259-2261 (R25)
- DiffuseTemperature: neighbor step diff/4 (was diff>>2, asymmetric), ambient pull ±1 raw every ~32 ticks gated by (_tick>>3)&3 (was broken ratchet hitting only 1/8 of cells) — 1987-2003 (R9)
- Fixed-step ambient pull makes return time linear in distance: ~53 s from −80 °C, ~13 s from Templada, ~160 s from Ardiente — 1161-1164 (R13)
- Empty cells: R13 says Empty does not participate in diffusion (1182-1186); R44 flags that SIM_NOTES claim as FALSE after EmisionTermica — 3013
- R44 EmisionTermica: falloff + Newton push + 15-row COLLAR returning edge to ambient; without collar diffusion saturated the whole room (3000-tick run). Measured: boil at 3 cells from ARDIENTE 6-8 ticks; freeze at 3 cells from HELANDO 22-87 ticks; ≤2 °C at 12 cells; HELANDO recalibrated −80→−26 °C — 2983-2992
- Live verification R44: cold plate sonda −26° on plate, −4° at 2 cells, 20° far away — 3003-3006
- Dispenser.EmitTick used Paint (no temp) so tap water inherited old cell temp and was born frozen; rule: anything that INTRODUCES matter uses PaintStable, movers (Flask, DeliveryChute, MasterSupplies) use Paint/PaintCell/PaintRect — 706-724 (R17)
- AlkahestSim.StableBirthTempRaw(MaterialDef): birth temp = threshold−10 raw (upper) or threshold+10 raw (lower) when ambient would cross meltsAt/boilsAt/freezesAt/condensesAt — 1069-1077 (R13)
- Prologue v1 gotera (R60): GOTERA at x426 from vault onto hogar de brasas x424..428 held at 165 raw via WakeChunk; drop by PaintStable every 1.15 s evaporated for real (vapor=4 first probe) — 3666-3675
- R64: 'LA GOTERA NO ES GOTERA: pozos de agua, agua hirviendo, vapor' — because GoteraX=426 fell ON the brasas each drop boiled; moved to poza x379 (cuenco 372-385 born dry) from a 5-cell stalactite; drop = 3-cell thread every 0.95 s; RezumarPoza seeps water above 34 cells through the floor 1 cell/frame so the drip never stops; first version paused the drip at full and it died at 39 s — 3815-3823, 3874-3875
- Flask restores the average temperature of what it sucked (per material) via PaintCell; carbon arrived hot and self-ignited in the Ensayo; liquids temper during transport — 1795-1797 (R4), 3291-3295 (R49)
- Steam: R41 boiling branch Water+cima≥boilsAt→Steam; convection rumbo by hash salt 551 over (_tick>>4, x>>3, y>>3) (~0.5 s per cell, 8x8 blocks share wind); 30% undulation (salt 549); escape under ceiling: diagonal-rumbo → diagonal-contra → lateral-rumbo → lateral-contra; measured lateral dispersion +52%, escape from overhang +121%, bench worst +3.5% — 2851-2862
- Gas born by SetCell/PaintStable/CerrarHornada with aux==0 died in its FIRST tick (same bug as ProcessFire R9); fixed by seeding lifetime with jitter (salt 553); deuda: grep other consumers of PaintStable/SetCell with gasLifetime>0 — 2856-2859, 2876-2878
- R39 gases with currents: thermal drift (lateral biased to hotter neighbor) + POCKETS under ceiling (60% lateral pressure vs 35% open sky), half decay under direct ceiling; '+3 life per move' version was immortal and motivated rule 55a — 2733-2737
- Brasa (MaterialId 58): lives 8-12 s, modest heat, reignites flammable neighbors 8%/step (salt 523), water → Ash + Steam instantly, decays to Ash — 2724-2731 (R39)
- Persistent combustion: fuel IS the burning cell, 7 MaterialDef params, state in aux (Liquid 7 bits, Powder full byte); oil burns ~32 s/cell from the edge (~90-100 s a pool) — 2712-2722
- Fire born ~240 raw; turba pile at 175 raw MELTED (fusion ≤148 in 777002) — open-pile turba combustion not tameable by bed temp; solution: hogar consumes turba to ash at 0.7 s/cell, bed stays 118 raw, FogonCimaRaw=152 is a constant of the FIRE — 3705-3714 (R61)
- Crisol thermal ladder in Semilla Cero: rescoldo 120 → arena(100); turba 130 → arcilla(124); ceniza 145 → caliza(136); carbón tier1 ~185 → sal(158); combustibles 165..190 raw — 3192-3195, 2482
- R74 corner-step leak: a bite at (339,172-173) beside the high ledge gave the stream a DIAGONAL staircase and the whole flow flooded the floor; left wall not bitten in y152-178 — 4574-4579
- R85 LA FUGA: silo walls stood on poza/crater lips with vacuum below → classic DIAGONAL escape; container now seals its own floor row (PisoEstructural at y0-1); verified 48 lodo + 32 agua with zero leaks — 4992-4999
- R86 end-wall filtration: wall top flush with interior (y152) lets the top liquid cell slip diagonally; walls raised one row above interior; 78/78 with zero escapes — 5061-5065
- R129: poza was draining — corridor pozo↔poza (y68-84) at water level (surface y81), 1972 cells drained through the pozo in ~1 min; floor raised to y82; stable after 45 s — 6750-6755
- Cascade equilibrium: director flow + poza seepage = 34/34 (R73 4522); pozos hondos depth 5 → 34→48 (R74 4580-4581); depth 10 in R113 (6098-6099)
- Refill v1 (R85): ActivarRefill drips every refillSeg=0.8 s through the inlet column to refillTope=60/78 — 5028-5030; R88: drop born at glass TOP and falls, tope 36 (half) — 5185-5191; R90: cadence QUADRATIC 0.8→~4.8 s (~110 s), tope 66 not 72 because drop died mute at 71 — 5263-5268; R91: entire glass 72 with silent tail, factor 7 integral 14→72 ≈ 180 s — 5317-5321; R112: tope = Capacidad() from the real glass, 4 cells per event — 6045-6056; R116: random columns rng 431 — 6178-6180
- Reservoirs x1.6 (R110): footprint 8x13→14x24, capacity 72→276; cavern interior 300-468; moveSpeed 4.0 (later 4.8); collider 6.4x11.2 cells — no single-pass 5-cell chisel tunnel fits — 5992-6016
- Mud drip (gotera del derrumbe): permanent, mound cap + hysteresis, waits if nobody harvests (R73 4525-4526); refills up to 70 per cycle (R113 6094-6095); two cracks ±3 (R114 6123-6126); all cosmetic mud randomness from System.Random seed 913, never the sim; cadences 0.35x-1.85x; each crack silent 25% (R115 6143-6149)
- 'El lodo es arcilla en polvo: SE APILA — el contraste con el agua es el aprendizaje' — 4526-4527 (R73)
- Barbotina = lodo + agua (soluble by decree Override 6b: arcilla sí, chamota no, arena/turba no, caliza/sal sí) — 3604-3607 (R56); still born in prologue but silent, cuenco accepts it as lodo — 4824-4830 (R79)
- Limo primigenio: turbid suspension; heating to raw 112 precipitates base powders per-cell by deterministic draw with seed weights — 2229-2231 (R25); renamed 'lodo de cantera' — 3029-3030 (R45)
- R78 softlock: lodo steals deposit volume (interior 78, meta 48: >30 lodo makes goal impossible) — 4777-4780; volume hierarchy frasco 900 > redoma 300 > depósito 78 — 4783-4784
- Powder behavior: turba veta spilled in cascade at first placement — 'la turba es polvo y toda cara expuesta se desliza sola'; sealed behind intact rock face with a 3-cell asomo — 3258-3266 (R48)
- Cohesion gravity (R29): solid holds if supported below OR within K cells horizontally through CONTINUOUS solid (StaticSolid or Powder; a gap cuts the beam, liquids do not transmit load); K: cerámico 8 > compacto 6 > recocido 5 = cristal 5 > hielo 4 > templado 3; stone/obra never falls; falls 1 cell/tick only into VACÍO; no lateral slide; only awake chunks — 2529-2541
- StaticSolid case in SimStepper.ProcessIfNeeded is EMPTY — Crystal/Ice never call Move(); DeliveryChute.ArrastreTick drags via CellGrid.SwapCells + WakeChunk bottom-up — 1822-1837 (R8)
- Rigid bodies Noita-style: verdict NO (expensive, breaks sync assumptions) — 2697-2698 (R38)
- Vivium growth: settled cell looks for orthogonal Nutrient, own temp in [VivGrowMinRaw,VivGrowMaxRaw] (30-60 °C ±15 per seed), 1 attempt/4 ticks, mother never transforms, dormant OrganicDormantAux outside band, dies >120 °C — 1961-1966 (R9); tip-only dendritic growth, tolerance 2-3 NEVER 1 (100% self-lock with 1), VivGrowChancePct 60→75, 4 habit params per seed incl. vertical bias — 465-491 (R19)
- Criatura/Capullo parked not deleted (spawns commented) — 2250-2253 (R25); creature thermal temperament: NÚCLEO keeps creature inside its band, only ALCANCE AMPLIO carries temperament (a cold creature cooling its own cell self-destructs) — 202-212 (R22)
- Pátina: CellGrid.patina byte, written/read ONLY by SimRenderer (12 rows/frame): wet ≤90 dries, soot up to 220; MOJADO disabled in R44 because it promised filtration the sim does not do; 'goteo-que-se-seca' idea parked until real infiltration exists — 2739-2745, 2975-2977
- Reactions: MaybeReact 1/2 in registered zones (SimStepper.RegistrarZonaInteres, chunk mask) vs 1/8 elsewhere — 2746-2748 (R39)
- Perf 256x144: 0.2-1.0 ms/tick, 60+ fps with sleeping chunks — 19
- Perf 768x288 (R38 bench): half world of water = 74,000 active cells 5.5 ms/tick avg, 11.6 peak vs 33.3 budget; real play uses 2-5% — 2691-2695
- R39 bench (sandbox ~3.5x slower than the R38 machine; baseline 7.7/20.9/9.5/7.3/15.2 ms): cascada 7.97/11.5 · diluvio 21.97/45.6 · incendio 9.69/15.9 · arena 7.64/12.9 · mixto 15.66/21.9 · incendio sostenido 7.28/10.6 (avg/peak ms); round delta +2-8% — 2764-2770
- R44 bench +0.9..3.8% stable — 2992; R41 worst +3.5% (mixto) — 2861-2862; R47 no regression — 3126
- MorphTick estimated <0.5 ms/tick, never measured — 95, 1518; morph field per cell + morphScratch double buffer, 1/4 stride per tick, sleeping chunks at 1/8 rate — 1423-1430
- R15 made the three world-proportional passes (full texture refresh every 30 frames, MorphTick, DiffuseTemperature) viewport-aware — 868-871
- PielDeRoca (marching squares, R124): 0.035 ms/chunk, 30 ms full world, 0.01 ms/frame idle, 140k vertices; F7 levels 0-4 — 6553-6563
- Multi: sim runs only on host; deltas of awake chunks, RLE per row of mat[] + quantized temp every 4th tick, 10-15 Hz, 5-30 KB/s est — 84-87; R43 priority pass ≤60 cells of avatar every 2 ticks (~15 Hz), worst ~155 KB/s per client — 2946-2949; guests do not receive events (no stepper) — 2773-2774; temp not replicated (thermometer shows '—') — 2997
- DifundirChunksSucios starvation fixed: two passes, priority ≤60 cells from any avatar, budget 96 — 2666-2669 (R36)
- Gallery (R127): SimLevelBuilder.BuildGaleria with 9 areas incl. cueva íntima with fogata, patio de fuego, nave 105 cells, wall of 6 fabricated solids, vertical pozo with ledges, POZA CON BOLSA DE GOTEO REAL, vano, pendientes (stairs/ramp/thin ledge/pebble/low tunnel), TERRARIO with 8 material cubetas; ModoGaleria (Fundación pattern, reset in the 6 paths of rule 59); GaleriaCurador; F10 capture round to Galeria/capturas/ — 6622-6637
- R127b/128: machines placed via Init at classic anchor + Reposicionar (rule 36, never re-Init); removed machines go to LA BODEGA (30..300 × 262..284); hot-load Galeria/roca_superficie.png with R; corridors 16 cells wide; fogatas revived every 5 s (real brasa burns to ash in ~1 min); curator on F8; catalog adds placa ígnea, placa gélida, caño de agua, balda; terrario uses only named vocabulary — 6660-6686, 6699-6717
- R129: liquid VEIL (third texture, sorting 52, alpha 115, same ComputeCellColor) and MENISCO in PielDeRoca (_hashOrilla checked only in slow round, ~1 s lag) — 6728-6749
- Time scaling: none documented anywhere; sim fixed 30 Hz; AlkahestSim.Paused freezes single-player sim (3131), editor Play via MCP can leave EditorApplication.isPaused=true — 4103-4106, 6758-6760
- HeatPlate/ChillStone: RowsAffected=3, TempStepPerTick=5 both; ChillStone modes Off→Fresca→Helando; HeatPlate.FootprintFraction covers 40% of the vat floor — 1156-1173, 716
- Dispenser: rombo of 5 cells (radius 1) per tick at 30 Hz → ceiling 150 cells/s, infinite flow, no reserves; taps overflow upward seeking surface up to 8 cells — 1848, 1965-1966, 1760
- Flask capacity 900, reach 60 cells; apprentice 11.2 u/s = 112 cells/s (later 4.8) — 1853, 6031
- Ambient climate by zone (CULTIVO 26 °C, SÓTANO 4 °C, gradients) was built in R15 and REMOVED in R17; CellGrid.ambient byte and PaintClimate (uniform) survive for player-made local climate — 727-763

## APIs / ganchos

- AlkahestSim.PaintStable(x, y, radius, materialId) — birth at stable temp; the rule-22 entry point for any source that creates matter (HISTORIAL_RONDAS.md:1077-1080, 720-724)
- AlkahestSim.StableBirthTempRaw(MaterialDef) — 1069-1076
- AlkahestSim.Paint / PaintCell / PaintRect — move existing matter carrying its own temp (Flask, DeliveryChute, MasterSupplies) — 720-724
- AlkahestSim.InyectarTemperatura — Paint-discipline temperature injection (R44) — 2986
- AlkahestSim.Paused (single-player freeze) — 3131; AlkahestSim.Start runs AdornarCuarto — 2577
- CellGrid.SetCell (never touches temp, aux=0, seeds morph), CellGrid.SwapCells (moves mat+temp+aux+morph), WakeChunk — 1061-1063, 1834-1835, 1407-1411
- CellGrid.AmbientRaw / CellGrid.ambient[] per-cell ambient byte written by SimLevelBuilder.PaintClimate (uniform now) — 727-729, 757-763
- Sim.EmisionTermica (shared plate physics: falloff + Newton push + 15-row collar) — 2983-2986
- SimStepper.DiffuseTemperature (rule 9: do not touch casually) — 1987-2003, 1174-1175
- SimStepper.MorphTick, SimStepper.GrowthTick, SimStepper.ApplyPhase (fusion/boil/condense/autoignition mutually exclusive), TryIgnite, ProcessFire — 1423, 1961, 2015-2019
- SimStepper.RegistrarZonaInteres + per-chunk mask (MaybeReact 1/2 in zones, 1/8 elsewhere); SimLevelBuilder.RegistrarZonasInteres called from Crisol.Init — 2746-2748
- SimStepper.Events (SimNotableEvent[] ring 256) + EventHead; consumers read non-destructively with own index (SubstanceKnowledge, DirectorDeAudio, ParticulasFx.LeerEventosDesde); rate-limited Ley events — 1911-1916, 2757-2760, 648-655
- XorShift.FromCell(tick, x, y, salt) — cast salt to uint when mixing constant+field — 1054-1060
- Universe.Create(seed) / Universe.Get(MaterialId).freezesAt/boilsAt/meltsAt / Universe.CrystallizeMaxTempRaw / Universe.ExtraccionRaw / Universe.TryCruce / Universe.AfinidadDelUniverso / Universe.IdentidadReal — 1169-1171, 2477-2479, 3113-3118, 588-590, 3033
- Universe.SemillaCero = 777002u + AplicarOverridesSemillaCero() — 2794-2799
- Dispenser: SpoutRadius, TickDt, EmitRatePerTick, favorCostPerActivation; EmitTick (chorro + rebose) — 1848, 2040-2042, 719
- Flask: capacity 900, ReachWorld, SuckRatePerTick, Total, Avisar (cursor globe channel), EsAspirable + TickSuck filter, PourMaterial/TickPour — 1853, 3883-3884, 6160-6162
- Cincel (Game/Cincel.cs): C toggles frasco/cincel, TallarTick with ObraDelTaller guard, CeldasTalladas, radius 2, 3 cells/tick — 843-851, 2472, 2159
- SimLevelBuilder: BuildFundacion, BuildGaleria, PaintClimate, ObraDelTaller (EsObraDelTaller), ReservasDelPlano (ColumnaOcupada/RectOcupado), Tallar*, BaseYDeEstacion, FundacionPozoHondo, FundacionVeta*, TapiarSalasSemillaCero/DestaparSala — 3664-3669, 6622-6623, 2470-2472, 4051-4059, 4580, 2801-2803
- SimLevelBuilder.EsRepisaDeCascada() — collision exception for cascade ledges; EsCeldaDeRepisa — 6106-6110, 5107-5109
- DepositoDeAgua (Game/DepositoDeAgua.cs): Init / InitSilo(dueño, huella, carga, piel), DelDueno/Ocupado/Capacidad/CentroMundo, AguaDentro (alias), ActivarRefill, Retirar (Drenando/Hundiendo/Enterrado), RetirarDeGolpe, RetirarRapido, MetaLlenado(rec)=60% Capacidad, InstalarTubo — 4927-4931, 4958-4962, 5028-5030, 5007-5010, 5157, 6091-6093, 5172-5176
- FundacionDirector: RezumarPoza, TickBarrido, TickDesagueFinal (retired R91), MantenerFuegoDelMaestro, ContarLodoEnCrater, LuzEn(mundo), DibujarBandaMaestro, FrascoBloqueado, HudPermitido, _fuentesApagadas, _cascadaMuda, FocoCinematico/Sacudida (SimRenderer) — 3819-3821, 4961, 5075-5077, 5104-5106, 4939, 4534-4537, 5070-5071, 5149, 4523-4524
- GuionDelPrologo (ScriptableObject) holds all prologue texts/quantities/times/caudales; rule 58: serialized fields override code defaults, rename fields to invalidate — 4599-4602, 4759-4763, 5322-5327
- PlanoOverlay (Game/PlanoOverlay.cs): 'GUARDAR FORMA COMO PLANO' captures Stone↔Empty diffs as asset reapplied after BuildFundacion — 4725-4741
- PrologoEscenografia.OnDrawGizmos zone map + DevPalette 'Copiar (x,y)' and 'Captura PNG' with cell in filename — 4742-4750
- GaleriaCurador (F8): place/remove fire, matter, rock, machines (crisol/alambique/prensa/banco/placas/caño/balda), stamp by radius, Ctrl+1..9 teleport, F10 captures, R texture hot-load; LA BODEGA archive — 6626-6633, 6662-6686, 6707-6717
- PielDeRoca: CargarTexturaDeRoca, F7 levels, _hashOrilla menisco; SimRenderer.OcultarRoca / MarcarTodoSucio / TinteActual / RepintarAhora — 6553-6564, 6679-6682, 6742-6747
- HeatPlate / ChillStone: RowsAffected, TempStepPerTick, HelandoRaw, FrescaMarginRaw, FootprintFraction, IMaquinaUsableRemota — 1156-1173, 2988
- Termometro (Game/Termometro.cs): G mode, up to 3 FIFO probes at ~4 Hz — 2994-2998
- ParticulasFx (Game/ParticulasFx.cs): Activas kill-switch (default false), ring 4096, overlay 768x288, observation-based emission 64 births/frame — 2748-2760, 2973-2975
- Tools~/BenchSim/Harness.cs — headless bench running the real SimStepper against build DLLs, 6 scenarios — 2691-2693, 2764-2770
- ApprenticeController: CajaChoca (grid AABB excluding ObraDelTaller), ChaflanCeldas, IntentarAsistencia, Desenterrar, SobreSuelo — 4335-4348, 6498-6506
- Mudanza / IMovible: V grab, R return, Reposicionar, ForzarSalida — 218-220, 6664-6667, 6181-6183
- DirectorDeAudio: GrifoLiquido loop anchored at spring, SistemaActivo switch, FactorMaestro — 4751-4755, 1919-1921

## Estado por celda / chunk

- mat[] (byte MaterialId per cell; RLE'd per row for multiplayer deltas) — HISTORIAL_RONDAS.md:84-86
- temp (byte raw temperature per cell; raw = 60 + °C/2; grid starts entirely at CellGrid.AmbientRaw=70; NOT touched by SetCell/Paint; quantized every 4th tick in net deltas) — 1061-1063, 85
- aux (byte per cell: fire life, combustion state — Liquid 7 bits preserving flow bit, Powder full byte; Vivium bits 0x01/0x02 direction, 0x04 flag, 0x40 OrganicDormantAux, 0x80 settled; gas lifetime; SetCell resets to 0) — 2714-2716, 1493-1496, 2027-2031, 2856-2858
- morph[] + morphScratch[] (byte per cell morphological intensity 0..255, double-buffered, seeded by SetCell hash(idx,material), swapped with matter by SwapCells; must travel in net deltas) — 1404-1411, 1425-1428
- ambient[] (byte per cell ambient temperature target for DiffuseTemperature pull; painted uniform by PaintClimate; kept for player-made local climate) — 727-729, 757-763
- patina[] (byte per cell, renderer-only: wet ≤90 that dries, soot up to 220; MOJADO disabled R44; auto-cleared when cell stops being solid) — 2739-2745, 2975
- touchedTick per cell (10-tick window used by ParticulasFx emission by observation) — 2752-2753
- Per-chunk: sleep/awake state (chunks duermen; sleeping chunks get MorphTick at 1/8 rate; a burning cell keeps itself awake — rule 55b), _chunkContinuousAnim[] bool per chunk for renderer-only wake, per-chunk reaction-interest mask (RegistrarZonaInteres), dirty-chunk diffusion queue (DifundirChunksSucios budget 96), PielDeRoca per-chunk hash + 8 neighbors and second _hashOrilla — 19, 1428-1430, 2722, 1466-1470, 2746-2747, 2666-2669, 6558, 6742-6747
- ObraDelTaller rect registry (protected masonry: anti-chisel, not aspirable, walkable-through for machines except cascade ledges) and ReservasDelPlano — 2470-2472, 4051-4059, 4335-4340, 6106-6110
- PisoEstructural material (structural floor rows sealing containers; never aspirable) — 4994-4996, 6160-6162

## Constantes

- Tick rate 30 Hz, budget 33.3 ms/tick — HISTORIAL_RONDAS.md:2692-2693; sim at 768x288 = 221,184 cells, CHUNK=16, 48x18 chunks — 48-49, 865-866
- Ambient raw 70 = 20 °C; HelandoRaw 20 = −80 °C (pre-R44), −26 °C after R44; Templada ~raw 82; Ardiente raw 220 = 320 °C — 1063, 1158-1160, 2988
- waterFreezeC ∈ [−15, 15] → freezesAt raw 52..67; Ice.meltsAt = freeze+5 °C; water boilsAt ≤ raw 119; crisol rescoldo tier0 raw 120; limo separation raw 112; calcination band Semilla Cero 130..170; TempEnsayo 177 — 748-751, 1064-1066, 2259-2262, 2795-2797, 2823-2826
- DiffuseTemperature: neighbor diff/4; ambient pull ±1 raw every ~32 ticks; per-tick stride i+=8 offset tick%8 — 1990-2003, 1161-1162
- EmisionTermica collar = 15 rows; boil 3 cells from ARDIENTE 6-8 ticks; freeze 3 cells from HELANDO 22-87 ticks; ≤2 °C at 12 cells; hornada front 66 ticks worst — 2983-2991
- HeatPlate/ChillStone RowsAffected=3, TempStepPerTick=5, FrescaMarginRaw=10, FootprintFraction 40% — 1156-1157, 1170, 716
- Fire: life 80 ticks fed, painted fire seeded 16±3 ticks (~0.5 s), decay half-speed under 6; TryIgnite 12%/tick (was 50%); ignition by contact 50%; oil ignitionTemp ~208-312 °C (raw ~164-216); Fire born ~240 raw — 1697-1698, 2020-2031, 2035-2037, 2016-2017, 3709
- Combustion R39: oil ~32 s/cell, pool 90-100 s; Brasa life 8-12 s, reignite 8%/step; turba burns 12.8 s; hogar consumes turba 0.7 s/cell, bed 118 raw, FogonCimaRaw=152 — 2718-2719, 2726-2727, 3192, 3711-3713
- Gas: pockets 60% lateral pressure under ceiling vs 35% open; rumbo hash (_tick>>4, x>>3, y>>3) ≈0.5 s; 30% undulation — 2734-2735, 2853-2855
- Salts: 77 (TryReactNeighbor), 201/209+semillaPatron (morph), 231/233/235 (marea), 503 combustion step, 509 extinction, 521 brasa life, 523 reignite, 547 gas wander/pocket, 549 undulation, 551 rumbo, 553 gas seed, 557/563 thermal front, 569 veta — 1502-1504, 1054-1059, 2159, 2771-2772, 2853-2858, 2991, 3189
- Cohesion K: cerámico 8, compacto 6, recocido 5, cristal 5, hielo 4, templado 3; fall 1 cell/tick — 2536-2538
- Marea fluidity field = number of cells to scan per tick, real scale 1-4 (120 would be a tsunami) — 2164-2166
- Densities: base1 powder 19 < agua 36 (flotation) — 2824
- Vivium: band 30-60 °C ±15 jitter (−20 with Frío Fértil), VivGrowChancePct 60→75, 1 attempt/4 ticks, dies >120 °C, tip tolerance 2-3, ~40 ticks/120 cells — 1961-1966, 486-491
- Dispenser: 5-cell rhombus per tick → 150 cells/s ceiling; overflow seeks surface up to 8 cells — 1848, 1760
- Flask 900, reach 60 cells; StorageRack 300/redoma; deposit interior 78 → 276 after x1.6 (footprint 8x13 → 14x24); LLÉNALO meta 48/24 → 60% of capacity (166) — 1853, 4783-4784, 5992-5994, 6091-6093
- Gotera v1 drop every 1.15 s onto brasas at 165 raw; R64 drop = 3-cell thread every 0.95 s, poza cap 34 cells, seep 1 cell/frame, ración 24 (45 overflowed), stalactite 5 cells — 3672-3673, 3819-3821, 3874-3875
- Cascade equilibrium 34/34 → 48/48 (depth 5) → depth 10 (R113); chorro ≥4 cells/pulse, crack 3 wide, ledges thickness 3; mud drip mound cap 70/cycle, two cracks ±3 — 4522, 4580-4581, 6098-6104, 6094-6095, 6123-6126
- Refill: refillSeg 0.8 s; tope 60/78 → 36 → 66 → 72 → Capacidad(); quadratic cadence 0.8→4.8 s (~110 s), factor 7 integral 14→72 ≈ 180 s; renacer 14 + banco/8 (tope 30); gota gorda ceil(tope/72)=4 cells; water stride 2 (inlet, −2, +2, −4); rng 431 for random columns; mud cosmetic rng seed 913, cadence 0.35x-1.85x, 25% silent — 5028-5030, 5189, 5263-5268, 5317-5321, 6053-6056, 6130-6133, 6178-6180, 6146-6149
- Ring of ORDEN: ~22 cells/s, poza lifted 15 c/s, motas ~110 c/s, tope 300, poza 70 cells, crater 18 — 5160-5163, 5273-5274, 5290, 5308-5310
- Perf: 0.2-1.0 ms/tick @256x144; 5.5 avg/11.6 peak ms/tick for 74k active water @768x288; R39 table cascada 7.97/11.5, diluvio 21.97/45.6, incendio 9.69/15.9, arena 7.64/12.9, mixto 15.66/21.9, incendio sostenido 7.28/10.6 (sandbox 3.5x slower); MorphTick <0.5 ms est.; PielDeRoca 0.035 ms/chunk, 30 ms world, 140k verts; DifundirChunksSucios budget 96; particles ring 4096, 64 births/frame; multi 10-15 Hz 5-30 KB/s est., 155 KB/s worst — 19, 2692-2693, 2764-2770, 95, 6558-6560, 2668, 2748-2753, 85-86, 2948
- Gallery: corridors 16 cells (low tunnel 6), fogatas revived every 5 s, LA BODEGA 30..300 × 262..284, ModoGaleria — 6699-6700, 6702-6704, 6668-6670
- Apprentice: moveSpeed 11.2 → 6.7 (R71) → 4.0 (R110) → 4.8 (R111); collider 6.4x11.2 cells; talla 12 cells; default view 80 cells — 4380-4382, 6009-6012, 6031, 5966-5968
- Audio fire probes 700 (was 220), saturation 5; rate limiter 6/s crystallize+freeze, 4/s rest — 2072-2075, 1907-1910

## Riesgos

- Rule 22 trap recurs in every new matter source: Paint/SetCell do not set temp or aux — water born frozen (R17 706-712), painted ice melting (R13 1061-1068), painted fire dying to smoke (R9 2020-2024), gas born with aux==0 dying first tick (R41 2856-2859). Any lab tap/spring/steam emitter must use PaintStable and seed aux; deuda still open to grep other PaintStable/SetCell consumers with gasLifetime>0 — 2876-2878
- DiffuseTemperature is protected by rule 9 (CLAUDE.md); its fixed ±1-raw pull is the documented cause of slow thermal recovery (1161-1164, 1174-1175). Any local-climate feature must be built on CellGrid.ambient + EmisionTermica rather than editing the pull. Diffusion without a cutoff radius saturated a whole room in 3000 ticks (2983-2985)
- Processes must be MORTAL (rule 55/55a/55b): immortal gas ('+3 life per move'), eternal fire (re-maximizing reserve), permanent drips that pause on a full mound — each produced a bug (2736-2737, 2720-2722, 5023-5024, 3819-3823). A permanent lab stream needs an explicit sink (poza seepage, sealed pozo) or it floods: the poza drained through a corridor at surface level (6750-6755) and the desagüe drank the poza before the ring (5242-5245)
- Liquids escape diagonally: over walls flush with the interior (5061-5065), over lips with vacuum below (4992-4999), down bites next to ledges (4574-4579). Channels/pools must be walled one row above the surface and sealed underneath
- StaticSolid has no gravity by design (1822-1830); solids vs powders behave differently (turba spills from any exposed face 3258-3266; lodo piles 4527). Cohesion K scanning runs only in awake chunks (2539) — moving cohesive bodies is not supported (rigid bodies rejected 2697-2698)
- Determinism: sim RNG is XorShift.FromCell(tick,x,y,salt) with per-feature salts; cast salt to uint when mixing constants and fields (1054-1060); morph needs double buffer because neighbor reads (1407-1411); all cosmetic randomness must use System.Random/Unity Random, never the sim (6146-6147, 6178-6180, 2748-2749); guests do not run the stepper and receive no events (2773-2774); temp is not replicated (2997)
- Chunk sleep hides bugs: sleeping chunks freeze pure-tick patterns (1466-1470); brasas need WakeChunk to keep working (3672); Empty diffusion claim in SIM_NOTES is stale (3013)
- Editor pitfalls: Play via MCP can leave EditorApplication.isPaused=true (4103-4106, 6758-6760); Cesar's editor has auto-refresh OFF — call CompilationPipeline.RequestScriptCompilation and check the type exists (6644-6650); without window focus the player loop suspends — Application.runInBackground=true (3719-3721); Escape is eaten by Play Focused (3262-3268)
- Serialized state overrides code defaults (rule 58 + prefab variant): refillTope=36 survived hot-reload though the asset never had the field (5322-5327), moveSpeed 11.2 in prefab overrode 6.7 (4422-4429). Any lab parameter panel with ScriptableObject/prefab fields must rename fields to invalidate old values or drop [SerializeField]
- Static field initializers calling Unity API crash the type (rule 56, 3146-3156)
- ObraDelTaller/ReservasDelPlano registry: protected rects silently reject the chisel (2470-2472, 4045-4059); machines register footprints in Init and Reposicionar re-tallies rock (6662-6667); PlanoOverlay only captures Stone↔Empty and must exclude runtime-carved zones (4725-4736)
- The apprentice collider (6.4x11.2 cells) cannot pass a single-pass 5-cell chisel tunnel; carving under your feet can bury you (fixed by Desenterrar) — 6013-6016, 6491-6506
- Sandbox resets repeatedly reverted the repo (16+ times); always git fetch and compare HEAD before deploying — 6640-6643, 6721-6723
- Multiplayer: any new per-cell field must be added to the chunk-delta format (morph precedent) — 92-95; MaquinaSync assumes a single instance per machine type — 3597-3599
- Perf: the sandbox bench machine is ~3.5x slower than the play PC; compare relative deltas only (2764-2766). World-proportional passes must stay viewport-aware (868-871)

## Oportunidades

- Reuse the prologue's cascade+poza+seepage as the lab's permanent stream: FundacionDirector caudal + RezumarPoza (1 cell/frame above a cap) already produce a stable equilibrium (34/34, 48/48) — HISTORIAL_RONDAS.md:4520-4522, 3819-3821, 4580-4581; the gallery already has a 'poza con bolsa de goteo real' — 6626
- Turbid water: limo/'lodo de cantera' is an existing turbid suspension that precipitates base powders when heated to raw 112 (2229-2231); barbotina (clay+water solution) already exists via decreed solubility (3604-3607) — a settling/decantation mechanic can build on Solution state + Powder
- Evaporation on a permanent fire is already proven: the R60 gotera dripped water on brasas held at 165 raw via WakeChunk and produced real Steam (3668-3675); MantenerFuegoDelMaestro repaints brasas forever with an obra floor (5104-5107)
- Steam conduction through geometry is implemented: R41 convection/rumbo, ceiling escape order, R39 pockets under roofs (2733-2737, 2851-2861); the alambique's vapor IS the real gas and the column→respiradero→'agua destilada: 7' cycle is verified (2872-2874) — condensation by cooling can use the existing condensesAt branch in ApplyPhase (1072) plus ChillStone/EmisionTermica cold zones (2983-2991)
- Local climate hooks: CellGrid.ambient per-cell byte + PaintClimate kept expressly for player-made climate (757-763); Sim.EmisionTermica + AlkahestSim.InyectarTemperatura for any heat/cold emitter (2983-2986); Termometro probes for validation in °C (2994-2998)
- Reaction hotspots: SimStepper.RegistrarZonaInteres gives 1/2 reaction sampling per chunk zone (2746-2748) — usable for a settling tank
- Cohesive solid rules (R29) and anclajes already let fabricated solids hold as beams; Powder vs StaticSolid archetypes give 'deposition seals a channel' for free if sediment is a Powder (2529-2541, 4527)
- Vegetation: Vivium GrowthTick with dendritic tip growth and per-seed habit params (vertical bias 'trepa hacia la luz') is dormant but intact; Criatura/Capullo parked (465-491, 2250-2253); PielDeRoca already draws decorative moss/roots (6557)
- Bench infrastructure: Tools~/BenchSim/Harness.cs runs the real SimStepper headless with 6 scenarios (cascada, diluvio, incendio, arena, mixto, incendio sostenido) — extend with lab scenarios (2691-2693, 2764-2770); F3 overlay shows ms/tick and active chunks (1614)
- Gallery scaffolding to clone for the lab: ModoGaleria flag pattern (rule 59 reset paths), BuildGaleria, GaleriaCurador catalog (fire/matter/rock/machines/plates/tap/balda, stamp, teleports, F10 captures, texture hot-load), LA BODEGA archive — 6622-6637, 6660-6686, 6707-6717
- Parameter panel precedent: GuionDelPrologo ScriptableObject centralizes texts/quantities/cadences/caudales for the director (4599-4602); DevPalette exposes Copiar (x,y)/Captura PNG and the ParticulasFx toggle (4745-4750, 2973-2975)
- Container tech: DepositoDeAgua parameterized (dueño/huella/carga/piel), refill with Capacidad()-driven tope and quadratic cadence, self-sealing floor and raised walls — reusable as lab basins (4927-4931, 5028-5030, 6045-6056)
- Liquid rendering already has a front veil and a meniscus at rock edges (6728-6749); patina soot near fire/brasa is renderer-only and free (2739-2745)
- Chisel/terrain: Cincel (C, radius 2, 3 cells/tick, PaintStable fill), PlanoOverlay to persist carved terrain as an asset, PrologoEscenografia gizmo zone map — 843-851, 4725-4747

## Preguntas abiertas

- No time-scaling of the sim is documented anywhere (only AlkahestSim.Paused and the fixed 30 Hz tick) — a lab 'time scale' control needs to be designed from scratch; check SimStepper/AlkahestSim for any ticks-per-frame knob
- Condensation: condensesAt exists in ApplyPhase (1072, 2015) but no round ever tuned Steam→Water on cold surfaces; unknown whether Steam condenses on contact with cold cells or only by its own temp falling
- Sediment settling/deposition: nothing models suspended particles in water settling or Powder compaction; the only 'turbid' material is limo separated by heat. How would turbidity be represented — Solution state, a new Powder-in-Liquid, or a per-cell aux channel?
- Does water density 36 vs powder density 19 (2824) already produce sinking/flotation of sediment, and what governs Powder sinking through Liquid?
- Which parts of the R39/R41 gas model (pockets, rumbo hash) are still active after later rounds, and is VaporPorCeldas theater still used by Crisol?
- Whether 'Empty participates in diffusion' is now true after R44 (SIM_NOTES says no, R44 flags that as false — 3013); needs code confirmation for steam conduction through open air
- Refill/drip code lives in FundacionDirector/DepositoDeAgua (prologue-specific); is it reusable outside ModoFundacion without the beat state machine?
- Exact current values of EmisionTermica falloff/collar and HeatPlate/ChillStone targets after R48-R51 (plates reworked, TEMPLADA removed) — the doc gives R13/R44 numbers only
- MorphTick real cost was never measured (95, 1518); with a heavy liquid lab it may matter
- Which rules (55, 58, 59, 36, 38, 39, 47, 52, 53) are cited by the docblocks the next agents will read — CLAUDE.md numbering must be consulted before touching SimStepper/DepositoDeAgua
- Multiplayer: does the lab need to work for guests (no stepper, no temp replication, single machine instance per type)?
