# HANDOFF — de Fable 5.1 (arquitecto) a Opus 5 (implementador principal)

*(2026-09-03. Este documento es autosuficiente: con el repositorio y `docs/LAB/` un agente
nuevo retoma el trabajo sin la conversación original. Léelo entero antes de tocar código.)*

## 0. ORDEN DE LECTURA (30 minutos)

1. `docs/LAB/CHECKPOINT.md` — estado, decisiones, archivos, pruebas, problemas, siguiente paso.
2. `docs/LAB/DISENO_LABORATORIO.md` — el diseño (por qué cada regla existe).
3. Este documento — arquitectura entregada, hitos, interfaces, criterios, riesgos, escalado.
4. `docs/LAB/mapa/*.md` — nueve mapas del motor con referencias archivo:línea (contexto, no dogma).
5. `CLAUDE.md` (raíz) — reglas del proyecto numeradas; el código las cita. Respétalas.
6. El código nuevo, en este orden: `Sim/LabParams.cs` → `Sim/LabMateriales.cs` →
   `Sim/SimStepper.Laboratorio.cs` → `Sim/SimLevelBuilder.Laboratorio.cs` →
   `Sim/Universe.Laboratorio.cs` → `Sim/SimRenderer.Laboratorio.cs` → `Game/LabPanel.cs`.

## 1. QUÉ ESTÁ ENTREGADO Y VERIFICADO (compila, corre, medido en el editor de Cesar)

- **Modo** `AlkahestGameBootstrap.ModoLaboratorio` (patrón ModoGaleria, regla 59 en los 6
  caminos: título ×3, bootstrap multi, SimSync ×3, "nuevo universo"). Botón en el título:
  «laboratorio de leyes (sandbox de investigación, dev)». `SpawnLaboratorio()` en el bootstrap.
- **Plano** `SimLevelBuilder.BuildLaboratorioDeLeyes` (x 30-430, toda la altura): sala del
  hogar con Hogar eterno, montículo de arena, veta de arcilla húmeda, bolsillos ocultos; pozo;
  galería del arroyo con piso escalonado, Manantial (agua turbia, 20 celdas/s), poza prellenada
  con sedimento, grava, grieta a la cámara profunda, pozo del Sumidero; cámara profunda con
  cubeta; chimenea → cámara alta fría (ambiente 8 °C) con sedimento seco → boca del cielo (luz).
  Clima por celda (`CellGrid.ambient`) frío arriba y fresco abajo. Teletransportes Ctrl+1..6.
- **Estado por celda** (`CellGrid`): `humedad`, `carga`, `reposo` (viajan en `SwapCells`,
  `SetCell` los limpia y el agua nace con volumen 255), `luz` (posicional).
- **12 materiales** (ids 66-77, `MaterialId.Count=78`): Sedimento, Arcilla, Terracota, Grava,
  Planta (arquetipo nuevo `Planta=7`), Fibra, Hogar, NucleoFrio, Manantial, Sumidero,
  RocaSuelta, Semilla. Defs en `Universe.Laboratorio.cs`; persistencia rellenada.
  `Universe.AplicarOverridesLaboratorio` fija agua (densidad 110, hierve 100 °C, congela 0 °C),
  vapor (condensa 60 °C, vida = `LabParams.VaporVida`), aceite flota.
- **Pasadas** (`SimStepper.Laboratorio.cs`, solo con `LabActivo`):
  `LabCampos` (aire: difusión conservativa del vapor, ascenso, condensación sobre superficie,
  rocío que gotea · agua: evaporación, infiltración con colmatación, decantación, DEPÓSITO,
  mezcla · porosos: exudación, percolación, capilaridad, secado, compactación → Arcilla,
  ablandamiento, cocción → Terracota, abono de ceniza · roca: rocío · hogar/frío/manantial/
  sumidero), `LabPresion` (cuerpos de agua conectados igualan superficies — VERIFICADO con un
  tubo en U: 237/199 → 219/217, agua conservada), `LabLuz` (máximo con decaimiento, 4 barridos),
  `LabDifusionTermica` (k/c por clase, convección, tirón a ambiente, contracción garantizada),
  `LabErosion` (agua que se mueve arranca sedimento → agua turbia), `LabCombustibleMojado`.
  Ganchos ya cableados en `SimStepper.cs` (Step, ProcessIfNeeded, ApplyPhase, TryIgnite, Move,
  ProcessLiquid). Tiempos por fase (`MsDifusion…MsCuerpos`) y libro mayor (`Lab*` long).
- **Tiempo**: `AlkahestSim.LabMultiplicador` (N ticks enteros por frame con presupuesto
  `LabPresupuestoMs`; `LabMultiplicadorReal` informa). `AlkahestSim.PaintLab(x,y,mat,temp,hum,carga)`.
- **Herramientas**: el cincel talla Stone/Piso/Arcilla/Terracota/RocaSuelta y la arcilla se
  desprende como sedimento húmedo (`LabMateriales.ProductoDeTalla`); el frasco no aspira los
  sólidos del laboratorio; el muñeco colisiona con ellos; con el ratón sobre el panel las
  herramientas ceden (`LabPanel.BloqueaHerramientas`).
- **Render**: `SimRenderer.LabTinte` (agua turbia parda, celda a medio evaporar más
  transparente, porosos mojados oscuros y fríos, sedimento fértil, planta sin savia amarillea,
  roca con rocío). Activo solo en el laboratorio (`SimRenderer.LabTinteActivo`).
- **Panel** `LabPanel` (F8): pestañas TIEMPO (1×/5×/10×/50×/100×, pausa, un tick, presupuesto,
  coste por fase, chunks/celdas/FPS), LIBRO (censo + libro mayor + balance), y una pestaña por
  grupo del registro con slider, valor+unidad, «D» (default) y «?» (ayuda plegable).
- **Medidas** en `Laboratorio/benchmarks/2026-09-03_costuras_fable.md`; capturas en
  `Laboratorio/capturas/costuras_0[1-4]_*.png`.

Lo que NO está (tus hitos): presets/snapshots/comparación, pincel de materia, vistas de
depuración, plantas (crecimiento), cuerpos cohesionados, LabBench headless, la roca porosa
estática para el filtro, el ajuste fino de los números jugando, el informe final.

## 2. ARQUITECTURA (invariantes que no se negocian)

1. **Nada del laboratorio corre fuera de `LabActivo`.** Fuera del modo, el juego es bit a bit
   el de antes (salvo el salto de filas de chunks dormidas en `Step`, semánticamente idéntico,
   y `reposo[idx2]=0` en `Move`, un byte). Si tocas `SimStepper.cs`, deja el gate.
2. **Los archivos grandes NO se editan más**: `SimStepper.cs`, `Universe.cs`, `SimRenderer.cs`,
   `SimLevelBuilder.cs`, `AlkahestSim.cs`, `AlkahestGameBootstrap.cs`, `DayCycle.cs`,
   `Cincel.cs`, `Flask.cs`, `ApprenticeController.cs` ya tienen sus costuras. Todo lo nuevo va
   en los `*.Laboratorio.cs` (partials) y en `Game/Lab*.cs`. Excepción documentada: la cuarta
   textura de vistas exige tres líneas en `SimRenderer.cs` (ver H2); y un material nuevo exige
   `MaterialId` + `Count` en `Universe.cs` + `Rellenar(...)` en `RellenarPersistenciaCruces`.
3. **Conservación en unidades**: 255 = una celda. Toda transferencia resta donde suma. Fuentes:
   Manantial (+), goteo/exudación/erosión (convierten, no crean). Sumideros: Sumidero,
   depósito (agua → sedimento), evaporación (→ vapor del aire). El libro mayor debe cuadrar.
4. **Determinismo**: solo `XorShift.FromCell(_tick,x,y,sal)`; sales nuevas > 631 y grep
   previo; jamás `UnityEngine.Random` en Sim/; escrituras a vecinos en orden fijo.
5. **Todo número físico vive en `LabParams`** con registro (nombre, grupo, unidad, default,
   rango, ayuda). Un número suelto en el stepper es un bug.
6. **R55**: todo proceso lento que no cambia materia y necesita el barrido despierta su chunk
   (el Hogar lo hace). Lo que vive en `LabCampos` no lo necesita (cubre toda la grilla).
7. **Semántica de los campos** (tabla en `DISENO_LABORATORIO.md` §2 y docblock de CellGrid).
8. **Solo anfitrión**: los campos nuevos no viajan por la red. No intentes jugar el laboratorio
   en la escena MULTI (`SimSync` resetea el flag).

Flujo de un tick con `LabActivo`: `LabDifusionTermica` (o `DiffuseTemperature` si
`termica.propia=0`) → barrido de chunks despiertos (con erosión y combustible mojado) →
contabilidad de chunks → `MorphTick` → `LabCampos` → `LabPresion` (cada N) → `LabLuz` (cada N)
→ `LabCuerpos` (gancho vacío).

**(R141) FÍSICA CONGELADA — decisión de Cesar, 2026-09-04.** Desde HF5c no entra ninguna regla
nueva de simulación ni cambia ningún número de física sin escalarlo a Fable y a Cesar. Lo que
queda —HF5c, H5, H7, H8— es contabilidad, herramienta, juego e informe. H6 (sólidos) espera su
propia etapa.

## 3. OPERATIVA CON EL EDITOR DE CESAR (aprendida a golpes esta noche)

- Editar código → `AssetDatabase.Refresh(ForceSynchronousImport)` +
  `global::UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation()` (así, con
  `global::`, dentro de RunCommand) → `GetState` hasta `IsCompiling=false` → `ReadConsole`
  errores → verificar por reflexión que el símbolo nuevo existe → Play.
- **JAMÁS edites un `.cs` con el editor en Play**: el siguiente RunCommand dispara un refresco,
  recompila, recarga el dominio, los estáticos se resetean y `AlkahestSim` queda sin grid. Si
  pasa: RunCommand con `ModoLaboratorio=true` + `DayCycle.RestartRun((int?)777002)` por
  reflexión (receta en CHECKPOINT §10) sin salir del Play.
- Entrar al laboratorio por MCP: Play → RunCommand que pone los flags y llama a `RestartRun`.
- Sondas: `Object.FindObjectsByType(tSim, FindObjectsSortMode.None)[0]`, leer `Grid`/`Stepper`
  por reflexión, `StepOnce()` en bucle para simular N ticks (≈400 ticks/s). `Stopwatch` no
  compila en RunCommand (usa `System.DateTime.Now`). `result.Log` no acepta `{0:F2}`: usa
  `string.Format` antes.
- Capturas: render de `Camera.main` a RenderTexture → PNG en `Laboratorio/capturas/` (receta
  en los RunCommand del checkpoint). Panorámica: `orthographicSize 14.6`, posición (23, 14.4).
- Git: NUNCA push. Genera `ca_playtestNNN.cmd` (add -A + commit + push) para que Cesar lo corra.
  Documenta cada ronda en `docs/archivo/HISTORIAL_RONDAS.md` (R131 en adelante).

## 4. HITOS PARA OPUS 5 (en este orden; cada uno compila, se verifica en vivo y se anota)

### H1 · El plano cierra el circuito del agua (½ día)
**Problema medido**: la fisura de arena (x192-202, y111-134) es polvo y cae a la cámara profunda;
el arroyo entero se cuela por el agujero y nunca llega al sumidero.
**Trabajo**: (a) nuevo material `Arenisca` (id 78, `Count=79`): StaticSolid, `caeSolido=false`,
permeabilidad `suelo.permArenisca` (default 30, registrar), color (196,172,128), tallable →
Sand (añadir a `LabMateriales.EsSolidoDelMundo/Tallable/ProductoDeTalla/Permeabilidad`, al
`switch` de porosos en `LabCampos`, a `LabK/LabC` como roca, y `Rellenar(Arenisca, 250,
Resistir)`). (b) La fisura pasa a Arenisca; la grieta x336-343 se mantiene. (c) Validar con
sondas: a los 3000 ticks el sumidero ha tragado > 0 y la poza está llena hasta su labio
(y=132). (d) Capturas panorámicas t0/t3000/t9000 y libro mayor en el checkpoint.
**Aceptación**: `LabAguaSumida > 0`; poza llena; la cámara profunda recibe goteo por la grieta
y exudación por la arenisca (LabExudado > 0); balance del libro cuadra ±5 %.
**Decides tú**: cotas exactas, si la grieta es 8 o 6 de ancho. **Escala**: cambiar el tamaño del mapa.

### H2 · Panel completo (1 día)
- **Presets JSON** en `Laboratorio/presets/<nombre>.json`: `{ "nombre", "fecha", "nota",
  "params": { clave: valor } }`. Guardar (campo de texto con `UiStyles.EscribiendoTexto`
  mientras se escribe, regla 12), cargar (lista de archivos), «defaults» (todo), «D» por
  parámetro (ya existe), **comparar**: tabla de claves ≠ default y ≠ preset elegido.
  Al arrancar escribir `_defaults.json` si no existe.
- **Snapshot** = preset + PNG (misma receta que GaleriaCurador.Capturar) + `_libro.json`
  (censo, libro mayor, tick, posición del muñeco) con un solo botón y un nombre.
- **Pincel MATERIA**: pestaña con los materiales del laboratorio + agua/brasa/fuego/hielo/aceite,
  radio -/+, clic pinta (`PaintStable`; agua turbia vía `PaintLab` con `TurbidezFuente`;
  brasa+fuego como el curador), clic derecho borra. `LabPanel.BloqueaHerramientas` debe ser
  true mientras el pincel esté seleccionado (añade `PincelActivo`).
- **Vistas de depuración** (`SimRenderer.Laboratorio.cs`): `VistaLab` estática
  {Ninguna, Temperatura, Humedad, Carga, Reposo, Luz, Chunks}; cuarta textura siguiendo el
  patrón exacto de `_veloTexture` (Init 392-398, BuildQuad 751-758, RenderChunk 1075-1094,
  Apply 910 de `SimRenderer.cs`; sortingOrder 54, alfa 150; rampas: temperatura azul→rojo con
  el ambiente en gris, humedad negro→cian, carga negro→ámbar, reposo, luz blanco, chunks
  despiertos verde); al cambiar de vista `MarcarTodoSucio()`. Los chunks dormidos solo
  refrescan cada 30 frames: aceptable, documéntalo en la ayuda.
- Ayuda ya existe por parámetro; añade un botón «AYUDA GENERAL» con el texto de
  `LabParams` (unidades, visitas, raw) plegable.
**Aceptación**: guardar → cambiar 5 números → cargar restaura exactamente; comparar lista
esas 5; snapshot deja PNG+JSON+libro; vistas visibles en capturas.
**Decides tú**: disposición, colores, atajos (no F3/F6/F7/G/C/V/E/Q/R). **Escala**: nada.

### H3 · El ciclo del agua, afinado jugando (1-2 días)
Objetivo: que las cinco cadenas del diseño §7 ocurran solas o con poca intervención, a
velocidad OBSERVABLE (ni instantáneas ni de horas). Protocolo: para cada fenómeno, un preset
de referencia + captura antes/después + números del libro mayor + el tiempo de mundo que tardó.
1. Evaporación → vapor → cámara alta fría → condensación → goteo sobre el sedimento seco.
   Hoy `cond=0` a los 2 min: el vapor no llega o la saturación (`vapor.satBase` 60,
   `satPorGrado` 4) está mal escalada. Sonda: humedad media del aire por zona y temperatura
   de la cámara alta (debe bajar a ~8 °C por el ambiente). Ajusta `VaporAscenso`,
   `VaporDifusion`, `SatPorGrado` hasta ver goteo en < 3 min de mundo con agua hirviendo en
   el hogar (verter agua sobre el Hogar con el frasco).
2. Sedimentación en la poza: llenado de la poza en minutos, agua más clara aguas abajo
   (medir carga media antes/después). Hoy hay churn erosión↔depósito en el lecho junto al
   manantial (969 vs 1274 en 2 min): decide si sube `sed.depositoReposo` (8 → 24) y baja
   `sed.erosionPct` (6 → 3), o si es un rasgo (el lecho vivo).
3. Canal sellado por colmatación: verter agua turbia sobre arena (pincel) y medir cuánto
   tarda en dejar de infiltrar (`LabInfiltrado` se aplana). Con `ColmatacionPct` 100 y
   `Infiltracion` 32 la arena de 40 de permeabilidad debería sellarse en 1-3 min de agua
   turbia y no sellarse nunca con agua limpia.
4. Arcilla: tallar la veta → sedimento húmedo → secar junto al hogar → ¿se compacta en
   Arcilla? (necesita reposo 200 visitas ≈ 53 s + 3 vecinos sólidos + humedad 100-230) →
   cocer → Terracota. Comprobar que un cuenco de terracota tallado retiene agua (perm 0).
5. Fuente artesiana/sifón: con el pincel, un tubo lleno desde el nivel del manantial hasta
   la sala: el agua brota en la sala. Captura.
**Aceptación**: cinco presets `Laboratorio/presets/ref_*.json` con su captura y una línea de
números cada uno en el checkpoint. **Decides tú**: todos los defaults de LabParams (anota los
cambios). **Escala**: cambiar una REGLA (no un número) de `LabCampos`/`LabPresion`.

### H4 · Plantas y fibra (1 día)
Spec (todo en `LabPlanta`/`LabPoroso` case Semilla, sales 619+):
- Germinación: Semilla asentada (Powder) sobre `EsSustrato` con `humedad ≥ PlantaHumedadMin`
  y `luz ≥ PlantaLuzMin` → Planta (aux = 0 = altura). Espontánea: sustrato con aire encima,
  húmedo e iluminado, `GerminaPorMil` por visita → Planta encima.
- Raíz (Planta con sustrato debajo): bebe `PlantaBebe` del sustrato → `humedad` (savia).
- Cada celda pasa `PlantaPasaSavia` a la Planta de encima si tiene menos.
- Punta (sin Planta encima): si savia ≥ `PlantaCrecerSavia` y `luz[encima] ≥ PlantaLuzMin` y
  altura < `PlantaAltoMax` y encima Empty → nueva Planta (aux = altura+1), savia −Crecer;
  con `PlantaRamaPct` crece en diagonal (arriba-izq/der) si Empty. Velocidad ×(1 +
  `PlantaFertilidadBonusPct` × carga[sustrato]/255/100).
- Marchitez: savia 0 durante `PlantaMarchitaVisitas` (usa `reposo` como contador) → Fibra
  (cae) y +fertilidad 40 al sustrato. Ya existe: sin raíz ni tallo debajo → Fibra.
- Arde (ya: flammable 160 °C, mojada no prende). Fibra = combustible persistente → Brasa → Ash;
  Ash mojada = abono (ya).
- Render: `LabTinte` ya amarillea sin savia; añade punta más clara (aux alto) si quieres.
**Aceptación**: en la cámara alta con goteo + luz del cielo brotan plantas solas en < 5 min;
sin luz no; sin agua mueren en ~1 min y dejan fibra; la fibra seca prende en el hogar y
deja ceniza que abona. Coste de `LabCampos` sigue < 0,5 ms con 2000 plantas.
**Decides tú**: forma (ramas), colores. **Escala**: si quieres que las plantas alteren
permeabilidad/cohesión del suelo (nuevo acoplamiento).

### HF5c · LOS FLECOS — **HECHO (R142)**, junto con H5. Ocho flecos aplicados, desagüe retirado (0/36 anegadas, residuo 0), `LabLuz` 2,86 → 0,50 ms idéntica, `LabBench.cs` con ocho escenarios y sus hashes, ninguno por encima de 2,96 ms/tick. Detalle abajo.
Índice; el detalle exacto está en `PREGUNTAS_A_FABLE.md` R18 y R19. (1) Fuera el desagüe (cuatro
líneas): medido con el riego real encharcaba MÁS (26/36 contra 7/36 a los 150 s), y la causa es
estructural (capilaridad 4/256 con tope 192; exudar pide 255). (2) Los pines propios del hogar y
del frío al libro entregado; el panel dice qué incluye el TOTAL. (3) `LabReservaApagada` para la
extinción por agua. (4) `EscribirDefaultsSiFalta` compara claves, no cardinalidad. (5) `Estado()`
lee `LabParams` donde exista el parámetro. (6) Lector sin allocs por frame (o texto corregido).
(7) «vidrio» a `EsSolidoDelMundo`. (8) Textos: el 17 % de la llama es ≈ 4 %, catorce contadores
más el vidrio, el rocío sin montaje, el `WakeChunk` redundante, y la tabla de la costura del §2
con convención explícita (+49/+13/+7 contra 371dea4).
**Aceptación:** nivel sin conducto ≤ 8 columnas anegadas a los 300 s con el alambique de r141 §2,
sustrato ≥ 60 %, residuo 0; TOTAL entregado = cinco fuentes con pines; coste sin regresión;
`ca_playtest142.cmd`. **Desde aquí la física está CONGELADA** (Cesar, 2026-09-04).

### H5 · Rendimiento y banco (1 día) — **ARRANCA EN LA MISMA RONDA QUE HF5c** (orden de Cesar, 2026-09-04): herramienta y rendimiento, CERO física nueva
Los escenarios ya existen a trozos en los RunCommand de las R131-R138 (los de Fable están en
`Laboratorio/benchmarks/2026-09-04_r136_*.md` y `_r138_*.md`: caja 20×12 con hogar + yesca + carbón,
plataforma hogar + arena + ceniza + combustible, carbonera 20×20 con doble corrida y hash). Recógelos
todos en `Sim/LabBench.cs` con el hash de `mat/temp/aux` como prueba de determinismo por escenario.
- `LabLuz`: acotar a `LabParams.LuzX0..X1` (30..440) y a filas con aire (mantén un `bool[]` de
  filas con aire por chunk-fila), o incremental por chunks despiertos + vecinos. Meta: ≤ 1 ms
  por ejecución.
- `Sim/LabBench.cs` (puro C#, sin Unity API): escenarios sobre `CellGrid` nuevo + `Universe.Create
  (777002)` + `AplicarOverridesLaboratorio` + stepper con `LabActivo`: (1) laboratorio base
  3000 ticks; (2) diluvio turbio (medio mundo de agua carga 255); (3) arroyo 30 000 ticks;
  (4) hervidero (200 celdas de Hogar bajo 10 000 de agua, chimenea, cámara fría); (5) 5000
  plantas; (6) 20 cuerpos de roca suelta cayendo; (7) mundo entero despierto (pintar 1 celda por
  chunk). Salida: `Laboratorio/benchmarks/<fecha>_<escenario>.md` con media/pico por fase,
  chunks/celdas activos, `GC.GetTotalMemory` antes/después, ticks/s. Lanzable desde un
  MenuItem del editor («Ten Thousand Years/8. Banco del laboratorio») y desde el panel (BENCH).
- Medir el multiplicador real alcanzable (1/5/10/50/100) en el laboratorio base y anotarlo.
**Aceptación**: tabla completa; ningún escenario > 12 ms/tick de media en el PC de Cesar salvo
(2) y (7), y esos con su número. **Decides tú**: técnica de acotación. **Escala**: cambiar la
frecuencia base de `LabCampos` (1/8) o el tamaño de chunk.

### H6 · Cuerpos cohesionados — hipótesis · **CONGELADO (decisión de Cesar, 2026-09-04)**
Queda DOCUMENTADO (aquí y en `DISENO_LABORATORIO.md` §8, con `RocaSuelta`, `LabCuerpos` vacío y
`Grava`) y **no se implementa en esta etapa**: los sólidos merecen su propia etapa y no entran a
contaminar un sistema de partículas, líquidos, gases, calor y vida que acaba de quedar coherente.
Lo de abajo es la hipótesis tal como se dejó, para cuando llegue su momento.
`LabCuerpos()` cada tick (o cada 2): etiquetar componentes de `RocaSuelta` (reusa
`_labVisita/_labCola`, solo en chunks despiertos o vecinos); un cuerpo está apoyado si alguna
celda tiene debajo algo que no es Empty/gas/agua y no es del cuerpo, o y==1. Sin apoyo: mover
todo el cuerpo 1 celda abajo en orden ascendente de y (Swap con la celda de destino: aire o
líquido, que sube); `aux` = ticks de caída (todas las celdas). Al apoyarse con aux ≥
`FracturaCaida`: `FracturaPct` de las celdas → Grava, empezando por la fila inferior y una
grieta por hash (`SalLabCuerpo`); contar `LabCuerposCaidos/LabFracturas`. Cincel sobre
RocaSuelta: en `Cincel.TallarTick` ya llega a `ProductoDeTalla`; para «más de un golpe» añade
en `LabMateriales` un `GolpesNecesarios(m)` y usa `aux` como daño (necesita un gancho de 3
líneas en Cincel; documenta). Catálogo: pincel MATERIA coloca bloques de RocaSuelta.
**Aceptación**: un bloque de 20×10 tallado por debajo cae entero, desplaza el agua de una
poza, y se fractura si cae ≥ 6 celdas; coste < 0,3 ms con 20 cuerpos.
**Decides tú**: forma de la grieta. **Escala**: empuje lateral o rotación (no).

### HF · EL DOMINIO DEL FUEGO (2026-09-04, va ANTES de H5 y H6)
Diseño completo, reglas mínimas, cadenas esperadas, benchmarks y criterio en
`docs/LAB/DISENO_FUEGO.md` (§8 tiene los hitos HF1-HF4 con sus aceptaciones). Una regla nueva
(el aire de contacto), un material (`Carbon`), tres parámetros, el vidrio como marcador de
calor industrial y el hogar como fuente doméstica. Excepción autorizada en `SimStepper.cs`,
toda gateada por `LabActivo` y marcada `(R135)`/`(R136)`/`(R138)`. **Tamaño real medido en R139**
(el «6 líneas» original se quedó corto tres rondas seguidas):

| método | líneas del método | tocadas |
|---|---|---:|
| `ProcessCombustion` | 829-937 | 18 (16 de código, 2 de comentario) |
| `ProcessBrasa` | 1003-1055 | 4 |
| `ProcessFire` | 1643-1727 | 3 |

Más seis líneas sueltas de R130/R132 (erosión al mover agua, condensado del gas, fibra mojada).
`TryIgnite`, `AddTemp` e `InjectHeat` siguen **intactos**: fuera del laboratorio el diff es
inerte. Antes de ampliarla otra vez, actualizar esta tabla.

### HF5b · LA HONESTIDAD DEL FUEGO — **HECHO (R139)**

Los cinco bloques de la revisión adversaria de Fable (R15-R17, 28 hallazgos):
`Laboratorio/benchmarks/2026-09-04_r139_hf5b_honestidad.md`. El hogar ya no chispea (fuera
`TryIgnite` de `LabHogar`, más `WakeChunk` en `LabCalentarHasta`), DOS libros separados —nominal
y entregado, con `LabInyectar` y los cinco `LabRaw*`—, `LabCalorNoSoltado`, el desagüe con labio
de roca y conducto a través de la solera, la persistencia y los textos. Las seis aceptaciones de
R15 se cumplen (carbón pegado **6/6 intactas**, HF2 **18/18**, tolva **466 s**), identidad de C2
a **+2,6 %**, agua con **residuo 0**, coste **1,90 / 1,91** ms/tick.

Y una corrección que nos alcanza a los dos: «la llama es el 90 %» (Opus) y «lo medido es ¾»
(Fable) salen ambas del libro NOMINAL y son falsas. En raw **entregados** la llama pone entre el
**6 %** y el **29 %**, y la fuente que más escribe es la combustión — la llama suelta 622 040
nominales y entrega 105 579, porque lo que ya está a 255 no admite más.

Abierta **Q12**: el desagüe está bien construido pero no drena nada medible. Siguiente: correr
`ca_playtest139.cmd`, **congelar la física** y seguir con **H5 → H7 → H8**.

### HF5 · LOS CIERRES DEL FUEGO — **HECHO (R137)**

**Ejecutado y medido**: `Laboratorio/benchmarks/2026-09-04_r137_hf5_cierres_del_fuego.md`.
C1-C4 puestos, más un quinto parche (`LabCalorCarbon`) sin el que la identidad de C2 no era
comprobable. Hogar doméstico (**0 vidrio** a 3 000 ticks), pico de carbón **25,0 % exacto**,
identidad **+1,0 %**, horno con yesca **18 de 18** y sin yesca **0**, tolva **466 s** (antes
324), agua con **residuo 0**, coste 1,85 ms/tick. Criterio 3 cerrado como «recinto y contacto»:
a igual masa y misma boca, la pila fina da **×5 de llama** que la maciza. 96 parámetros.
Dos aceptaciones no se cumplen y están escaladas sin bloquear: **Q10** (el hogar sí enciende el
carbón, por el 12 % de `TryIgnite` — corrige lo que R135 dijo de la cadena de encendido) y
**Q11** (arder ahogado destruye la mitad de la energía). Q8 puesto (desagüe de grava, 4 líneas);
**H4 sigue abierto** y pasa a H7, que es donde se mide el riego real. Siguiente: correr
`ca_playtest137.cmd` y **H7 con Cesar**.

<details><summary>La especificación original (R136 de Fable)</summary>

Fable midió la build R135 en banco headless (`Laboratorio/benchmarks/2026-09-04_r136_fable_tiro_y_hogar.md`)
y encontró tres cosas falsas de su propio diseño: el hogar calienta a 255 raw a sus vecinos
(arena + ceniza sobre el hogar vidria sin horno), carbonizar multiplica la energía ×6,3, y el
libro de energía no cuenta la llama (40 raw/tick por celda, la fuente dominante). Los parches
exactos, con código, están en `PREGUNTAS_A_FABLE.md` R13; el porqué del tiro que no existe, en
R12; el veredicto y el orden, en R14. Resumen operativo:

- **C1** `LabHogar` con tope (`LabCalentarHasta`): el hogar no empuja a nadie por encima de
  `fuego.hogarRaw`. Archivo del laboratorio, sin excepción. Si `suelo.terracotaRaw` > 170,
  bajarlo a ≤ 170.
- **C2** Rama F2 de `ProcessCombustion` (+3 líneas en la costura ya autorizada, marca `(R136)`):
  `fuego.rendimientoCarbonPct` = 25 (nuevo), sal `SalLabCarboniza` = 632, resto → `Ash`;
  `Carbon.combustReserva` 160 → 50. Identidad: `rend × reservaC × calorC ≈ ½ × reservaP × calorP`.
- **C3** Contadores `LabCalorLlama` (una línea gateada por `LabActivo` junto al
  `InjectHeat(x, y, 40)` de `ProcessFire` — **excepción autorizada (R136), una línea, la lengua
  no se toca**), `LabCalorHogar`, `LabEnergiaCarbon`, `LabCarbonizado`. Panel: los tres calores.
- **C4** `fuego.vidaHumo` default 255 y ayuda «tope 255 (byte); bajo techo cuenta doble».
- **Q8** Desagüe de grava en el nivel de referencia si cabe en ≤ 30 líneas (R11); si no, H7.

**Aceptación (todo en banco, sin Play, con tabla en `Laboratorio/benchmarks/`):**
1. Hogar + arena + ceniza → 0 vidrio a 3 000 ticks; agua sobre el hogar hierve; fibra pegada
   prende; carbón pegado NO prende.
2. HF2 repetido con yesca: el horno sigue vidriando (≥ 10 celdas); el hogar suelto, 0.
3. B-F3 con boca 1: 25 % ± 5 de carbón, el resto ceniza; `LabCalorFuego` + `LabEnergiaCarbon`
   ≈ celdas de fibra × 560 (± 5 %).
4. HF3 (tolva) sin cambios; regresión del agua idéntica (residuo 0).
5. Criterio 3 cerrado como «recinto y contacto» con B-F3 más pila fina contra maciza a igual
   masa; criterio 5 como «todo raw contado + identidad C2».
6. Opcional si sobra tiempo: banco humo × luz (fuego de fibra bajo el techo de la cámara alta →
   `luz` en el lecho por debajo de `planta.luzMin`), que deja el criterio 4 a medio camino.

**Después de HF5: física nueva CONGELADA** (recomendación de Fable, decide Cesar). Sigue H7
(jugar con Cesar), luego H5 (herramienta) y H8. H6 espera. Escala si C1 deja al horno sin
vidrio con yesca o si la identidad de C2 no cuadra.

</details>

### HF5b · HONESTIDAD DEL LIBRO Y DEL DESAGÜE (R138 de Fable; UNA ronda; la última antes de congelar)
Sale de la revisión adversaria de d711454 (28 hallazgos, ninguno de física) y de mi banco
(`Laboratorio/benchmarks/2026-09-04_r138_fable_verificacion_hf5.md`). Todo está en
`PREGUNTAS_A_FABLE.md` R15-R17; aquí solo el índice:

- **Ignición (R15 + R17 A).** `LabHogar` sin `TryIgnite` (enciende solo por temperatura) y
  `WakeChunk` en `LabCalentarHasta`. Carbón pegado NO prende; fibra sí; fibra mojada no.
- **Dos libros (R16 + R17 B).** Nominal (combustible, se conserva: + `LabCombustibleCarbon`,
  `LabUnidadesRespiradas`, `LabCalorNoSoltado`; razón depurada) y entregado (`LabInyectar` con
  deltas reales: fuego, llama, **brasa**, hogar, **frío**; el único TOTAL). Textos del panel y
  docblocks; contadores del fuego en el snapshot `_libro.json`; `_defaults.json` regenerable.
- **Desagüe (R17 C).** Labio de roca entero; conducto que atraviesa la solera hasta (136,245) y
  (153,245) con salida al aire de la boca; comentario de `Grava` corregido. Medido con rocío a
  10 celdas/s durante 300 s.
- **Textos del benchmark R137 (R17 D).** Tolva sin causa inventada; brasa en la tabla de C2 y en
  Q11; identidad 555 por celda y estadística.

**Aceptación:** las de R15; TOTAL entregado coherente (signo del frío incluido); B-F3 20×20 con
su +5,5 % y la identidad reescrita; desagüe medido; regresión del agua residuo 0; coste sin
regresión; `ca_playtest139.cmd`. **Después de esto la física queda congelada** y sigue H5.

### HF5d · ANTES DE LA BUILD DE H7 (R144 de Fable; media ronda; sin física)
Sale de la revisión adversaria de R142 y R143 (27 hallazgos, ninguno de física; detalle y
aceptación en `PREGUNTAS_A_FABLE.md` R23). Dos altas: (1) `LabBench.Correr` sin
`Universe.AplicarOverridesLaboratorio` — el banco mide el universo de la campaña; una línea y se
regenera la tabla con siete hashes por escenario; (2) el vidrio verde en `EsSolidoDelMundo` rompe la
campaña (instrucción de Fable en R19-7, revertida). Para H7: hitos «PRIMER X» por delta desde F9,
`LabInit` solo en el laboratorio, goteos limitados por segundo, ruta y colisiones del diario en la
build, snapshot en cada marca, distancia sin teletransportes. Y lo que HF5c dio por hecho sin
estar: los textos de R19-8, `LabReservaApagada` en el snapshot, «muy turbia» a 128, la tupla del
lector, la ayuda del carbón. Banco: defaults y `LuzCielo` al arrancar, hash de los siete campos,
caldera incondicional, hervidero con cámara, informe con unidades y hora.
**Aceptación:** la de R23. `ca_playtest145.cmd`. Después: build → el amigo (R21) → Cesar →
`OBSERVACIONES_H7.md` → Fable.

### H7 · El arco largo (½ día de juego, capturas)
Juega 30-40 minutos de mundo (usa 10×) siguiendo lo que el mundo sugiera; anota cada
«¿por qué pasó eso?» y cada intento de reproducirlo. Guarda 6-10 snapshots. Este material es
la sección A del informe. No fuerces ninguna cadena.

### H8 · Informe final (1 día)
`docs/LAB/INFORME_FINAL.md` con las secciones A-F del encargo de Cesar, usando:
`Laboratorio/benchmarks/*`, los presets `ref_*`, las capturas, `docs/LAB/MULTIPLAYER.md`
(análisis de Fable, sección D) y la valoración C: escribe tu borrador y marca las preguntas que
quieras escalar a Fable (C y la estimación E en meses son las que más se benefician de
una segunda opinión). Más: entrada R131+ en `HISTORIAL_RONDAS.md`, `ca_playtestNNN.cmd`,
y una infografía (Artifact) del sistema resultante si ayuda.

## 5. INTERFACES (lo que puedes llamar sin leer los archivos grandes)

- `LabParams.*` (estáticos) · `LabParams.Registro` (`List<LabParam>`) · `Buscar(clave)` ·
  `RestaurarDefaults()` · `Saturacion(tempRaw)` · `VaporVidaCambiado` → `Universe.ReaplicarVapor(u)`.
- `LabMateriales.EsSolidoDelMundo / Tallable / ProductoDeTalla / Permeabilidad / EsPoroso /
  EsFino / EsRocaImpermeable / EsFondo / EsErosionable / EsSustrato / EsGasId / EmiteLuz`.
- `SimStepper`: `LabActivo`, `Ms*` (8 fases), `Lab*` contadores (long), `LabPasadas()` privado.
  Métodos privados reutilizables desde el partial: `LabTransformar(idx,mat,hum,carga)`,
  `LabNacerAgua(idx,temp,carga)`, `LabVecinoVacio(idx,abajoPrimero,permitirArriba)`,
  `LabGotear(idx)`, `LabLatente(idx,u,signo)`, `InjectHeat/InjectCold(x,y,n)`, `TryIgnite(x,y)`,
  `_labVisita/_labCola/_labSup` (scratch de presión), `DirX/DirY`.
- `AlkahestSim`: `LabMultiplicador`, `LabPresupuestoMs`, `LabMultiplicadorReal`,
  `PaintLab(x,y,mat,temp,hum,carga)`, `PaintStable/Paint/PaintCell/PaintRect`, `StepOnce()`,
  `Paused`, `Grid/Stepper/Universe/Renderer`.
- `CellGrid`: `humedad/carga/reposo/luz` (byte[]), `ambient`, `SetCell/SwapCells/WakeChunk`.
- `SimLevelBuilder`: `LabSpawnX/Y`, `LabAnclaX/Y/Nombre`, `BuildLaboratorioDeLeyes`,
  `DrawSolidRect`, (privados accesibles desde el partial) `FillWorldStone`, `ObraDelTaller`.
- `SimRenderer`: `LabTinteActivo` (static), `LabTinte` (partial), `MarcarTodoSucio()`,
  `RepintarAhora()`, `_veloTexture` como patrón.
- `LabPanel`: `Abierto`, `RatonSobrePanel`, `BloqueaHerramientas`, `Crear(sim, aprendiz)`.
- `AlkahestGameBootstrap.ModoLaboratorio`; `DayCycle.RestartRun(int?)` (privado, por reflexión).

## 6. RIESGOS Y TRAMPAS CONOCIDAS

- Editar `.cs` en Play mata la sesión (ver §3). Compilar con auto-refresh apagado exige la
  receta completa.
- `PaintStable(Water)` nace con volumen 255 y carga 0: el agua turbia solo nace del Manantial,
  de la erosión o de `PaintLab`.
- `Transform()` (barrido) NO toca humedad/carga: agua→vapor→aire conserva el volumen en
  `humedad` por accidente feliz; si añades transiciones nuevas, piensa qué llevan.
- `SetCell` pone humedad=0 en todo lo que no es agua: al transformar un poroso con
  `LabTransformar` pasa la humedad que quieras conservar (lo hace).
- El frasco cuenta celdas, no volumen: una celda a medio evaporar cuenta 1. Aceptado.
- La presión mueve agua de la superficie más alta a la más baja del MISMO cuerpo, hasta 4
  celdas por paso: una cascada conectada a una poza se «acelera». Si molesta, sube
  `presion.desnivelMin` o exige `reposo ≥ 1` en el destino.
- `LabLuz` cuesta ~5 ms sobre el mundo entero (H5).
- `RocaSuelta` es StaticSolid con `caeSolido=false`: sin H6 se comporta como piedra tallable.
- La piel de roca (`PielDeRoca`) solo dibuja `Stone`; arcilla/terracota/arenisca se ven en
  grilla con junta (P2). Es lo esperado; no toques `EsRoca` (8 sitios).
- Las máquinas del taller (crisol, placas) no están en el laboratorio a propósito.
- Los chunks dormidos no repintan: un overlay que cambie sin cambiar `mat` tarda ≤ 30 frames.

## 7. QUÉ DECIDES TÚ Y QUÉ SE ESCALA A FABLE

**Decides tú** (anótalo en el checkpoint): todos los defaults de `LabParams`; cotas del plano
salvo su extensión; disposición y atajos del panel; formato interno del preset; técnica de
acotación de la luz; forma de fractura; colores; textos de ayuda; qué escenarios de banco
añadir; orden de H4/H6.

**Escala a Fable** (escribe la pregunta en `docs/LAB/PREGUNTAS_A_FABLE.md` con tu propuesta y
sigue con otro hito mientras tanto): cambiar una REGLA de `LabCampos`/`LabPresion`/
`LabDifusionTermica` (no un número); tocar `SimStepper.cs`/`Universe.cs`/`SimRenderer.cs`
fuera de lo previsto en §2; tamaño de chunk, de mundo o frecuencia base 1/8; nuevo array por
celda; cualquier cosa de red; la valoración C y la estimación E del informe.

## 8. SIGUIENTE PASO EXACTO

**(Actualizado por Fable, 2026-09-06, R144.)**

1. **HF5d** (media ronda, Opus): R23. `ca_playtest145.cmd`.
2. **Build** de un jugador y comprobación de la mezcla por Cesar (R22, cuatro criterios).
3. **H7**: el amigo primero (R21, entrevista de tres preguntas), después Cesar (dos sesiones);
   protocolo en `HANDOFF_SABADO.md` §2 y `GUIA_H7.md`; las tres sesiones con sonido (R20).
4. `docs/LAB/OBSERVACIONES_H7.md` (Opus) → Fable puntúa el criterio 4 del fuego y revisa.
5. **H8** informe; luego el diseño de la experiencia comercial (Fable, desde las observaciones).
6. **Física CONGELADA** (§2); **H6** documentado y congelado.
