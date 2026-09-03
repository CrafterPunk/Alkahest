# CHECKPOINT DE CONTINUIDAD — EXPERIMENTO MAYOR: "encontrar el juego dentro de la simulación"

*(Documento vivo. LEER PRIMERO si retomas este trabajo sin la conversación original.
Fuente de verdad: este archivo + `docs/LAB/*.md` + `Laboratorio/` (presets, capturas,
benchmarks) + el código bajo `Assets/Alkahest/`. Se actualiza antes y después de cada cambio
grande. Quien retome: Opus 5 según `docs/LAB/HANDOFF_OPUS.md`.)*

Última actualización: 2026-09-03 07:30 (Fable, fin de la fase de costuras y handoff).
HEAD del repo al empezar: `371dea4` (Ronda 129). Rama `main`. Cambios SIN commitear (Cesar corre
`ca_playtest130.cmd`). NO hacer `git push` (regla del proyecto).

---

## 0. OBJETIVO DEL EXPERIMENTO (en palabras de Cesar, condensado)

Hipótesis: **el conocimiento sustituye progresivamente al trabajo manual.** El jugador empieza
transportando materia a mano; al entender propiedades y procesos, construye configuraciones
espaciales (canales, pozas, cámaras, chimeneas) que hacen trabajo continuo o semiautomático.
Sensación buscada: *"descubrir una máquina que estaba escondida en las leyes del mundo"*.

Pregunta rectora: ¿puede este motor convertirse en un sandbox donde aprender cómo funciona el
mundo permite construir procesos cada vez más poderosos, hasta que conocimiento y geometría
sustituyen al trabajo manual?

Entregables (del encargo): 1) segunda galería jugable (conservar la actual) · 2) simulación
expandida donde tenga sentido, justificando emergente vs ruido · 3) fluidos: aproximación
celular barata (presión/nivel/flujo/infiltración) · 4) procesos LENTOS observables · 5)
vegetación mínima como hipótesis · 6) sólidos cohesionados como hipótesis · 7) panel de
parámetros en vivo + tiempo 1×..100× + presets/snapshots/ayuda · 8) stress tests medidos · 9)
análisis multiplayer · 10) informe final A-F (+ infografía si ayuda).
NO hacer: prólogo, onboarding, campaña, finales, narrativa, economía, guardado, máquinas
heredadas como centro, versión comercial. Este laboratorio es instrumentación descartable.

## 0b. REPARTO (mensaje de Cesar, 2026-09-03 ~06:10)

Por presupuesto: **Fable 5.1 = arquitecto/director técnico; Opus 5 = implementador principal.**
Fable hizo las COSTURAS con el motor, el algoritmo novedoso (presión), la pasada de campos, el
plano, el panel mínimo y la verificación. Opus continúa por hitos (`HANDOFF_OPUS.md` §4) y
escala a Fable solo lo listado en `HANDOFF_OPUS.md` §7 (vía `docs/LAB/PREGUNTAS_A_FABLE.md`).

## 1. ESTADO ACTUAL

**Fase: 3 — HANDOFF A OPUS.** El laboratorio existe, compila sin errores, entra desde el título,
simula (2,3 ms/tick con 87 chunks despiertos), la presión hidrostática está verificada (tubo en
U), el ciclo agua→vapor→humedad→condensación está implementado pero NO afinado (condensado = 0 a
los 2 min), la sedimentación y la erosión ocurren, la infiltración ocurre. Plantas, cuerpos
cohesionados, presets/snapshots/vistas, banco headless e informe: pendientes (hitos H1-H8).

Documentos: `docs/LAB/DISENO_LABORATORIO.md` (diseño), `docs/LAB/HANDOFF_OPUS.md` (hitos e
interfaces), `docs/LAB/MULTIPLAYER.md` (análisis de red), `docs/LAB/mapa/*.md` (9 mapas del
motor con archivo:línea), `Laboratorio/benchmarks/2026-09-03_costuras_fable.md` (medidas),
`Laboratorio/capturas/costuras_0[1-4]_*.png` (evidencia visual).

## 2. DECISIONES TOMADAS (y por qué)

- **D1 Ubicación**: `docs/LAB/` (versionado) para docs; `Laboratorio/` (raíz, hermana de
  `Galeria/`) para presets, capturas y benchmarks generados en juego.
- **D2 Modo**: tercer flag `ModoLaboratorio` (patrón ModoGaleria, regla 59 en 6 caminos), misma
  escena y sim, plano propio, spawn propio, panel propio (F8). La Galería de estilo, intacta.
- **D3 Costuras mínimas en los archivos grandes; todo lo nuevo en partials** (`*.Laboratorio.cs`)
  y `Game/Lab*.cs`, gateado por `SimStepper.LabActivo`. Fuera del laboratorio el juego no cambia.
- **D4 Cuatro campos por celda** (humedad, carga, reposo, luz) en vez de un sistema por fenómeno;
  cada campo sirve a 3-6 consecuencias (tabla en DISENO §2). Descartados como ruido: exposición
  térmica histórica, flujo acumulado como campo, presión como campo, cohesión como campo.
- **D5 Una sola regla no local**: presión por cuerpos de agua conectados (BFS, mudanza de la
  superficie más alta a la más baja, ≤4 celdas por paso, cada 2 ticks). Da vasos comunicantes,
  sifón y fuente artesiana por O(agua). Verificado y conservativo.
- **D6 Procesos lentos en una pasada sobre TODA la grilla a 1/8 por tick** (como la temperatura),
  independiente del sueño de chunks: una poza dormida evapora. Solo despiertan chunks al cambiar
  materia. Coste medido 0,15 ms.
- **D7 Térmica propia del laboratorio** (k y c por clase, convección, tirón a ambiente) en vez
  de tocar `DiffuseTemperature` (regla 9). Conmutable con `termica.propia`.
- **D8 Universo**: seed 777002 sin overrides de Semilla Cero + `AplicarOverridesLaboratorio`
  (agua/vapor por decreto). El laboratorio no depende del sorteo.
- **D9 Clima por celda** en el laboratorio (cámara alta 8 °C, cámara profunda 12 °C) usando
  `CellGrid.ambient`: sin una zona fría el vapor no condensa hasta saturar la cueva entera
  (físicamente correcto, jugablemente invisible). La regla 31 lo retiró DEL JUEGO; aquí es un
  experimento y está documentado en el plano.
- **D10 El cincel desprende la arcilla como sedimento húmedo** (tallar barro te da barro); la roca
  madre sigue sin rendir nada (R60). Terracota y roca suelta se rompen en grava.
- **D11 Tiempo** = N ticks enteros por frame con presupuesto de ms (jamás `FixedDt`); las
  máquinas con acumulador propio NO escalan (el mundo corre más deprisa que las manos; deliberado).
- **D12 Defaults cambiados tras medir**: `agua.evapBase` 2→1 (pozas duraban 2,5 min al aire),
  `sed.turbidezFuente` 90→40 (la poza se cegaba en 2 min), `luz.cadaTicks` 8→16 (5 ms por
  ejecución). Manantial: solo cuentan las celdas con cara libre (el caudal pedido se reparte
  entre ellas; medido 20,4 de 24).

## 3. ARCHIVOS MODIFICADOS / AÑADIDOS (todo sin commitear)

Modificados (costuras, comentadas con «(R130)» en el código):
`Assets/Alkahest/AlkahestSim.cs` (overrides, plano, LabActivo, multiplicador, PaintLab, tinte) ·
`Sim/CellGrid.cs` (4 arrays, SetCell, SwapCells) · `Sim/MaterialDef.cs` (arquetipo Planta) ·
`Sim/Universe.cs` (ids 66-77, Count 78, partial, CrearMateriales, Rellenar) ·
`Sim/SimStepper.cs` (partial, tiempos por fase, salto de filas dormidas, LabPasadas, caso
Planta, combustible mojado ×2, reposo en Move, erosión ×3) · `Sim/SimRenderer.cs` (partial,
LabTinte al final de ComputeCellColor) · `Sim/SimLevelBuilder.cs` (partial) ·
`Game/AlkahestGameBootstrap.cs` (flag, reset, SpawnLaboratorio) · `Game/DayCycle.cs` (botón,
HUD silenciado, resets) · `Net/SimSync.cs` (3 resets) · `Game/Mudanza.cs` · `Game/WorkshopBackdrop.cs`
· `Game/Cincel.cs` (tallable, producto, LOS) · `Game/Flask.cs` (no aspirar sólidos del lab,
guarda del panel) · `Game/ApprenticeController.cs` (colisión, sombra) · `Game/Termometro.cs` (guarda).
Nuevos: `Sim/LabParams.cs`, `Sim/LabMateriales.cs`, `Sim/SimStepper.Laboratorio.cs`,
`Sim/SimLevelBuilder.Laboratorio.cs`, `Sim/SimRenderer.Laboratorio.cs`, `Sim/Universe.Laboratorio.cs`,
`Game/LabPanel.cs`, `docs/LAB/*`, `Laboratorio/*`, `ca_playtest130.cmd`.

## 4. SISTEMAS AÑADIDOS

Ver `HANDOFF_OPUS.md` §1 (lista verificada) y `DISENO_LABORATORIO.md` §2-§6 (reglas).

## 5. PLAN (hitos de Opus, `HANDOFF_OPUS.md` §4)

H1 plano/arenisca y circuito del agua → H2 panel completo (presets, snapshots, pincel, vistas)
→ H3 ciclo del agua afinado jugando (5 presets de referencia) → H4 plantas y fibra → H5
rendimiento y banco headless → H6 cuerpos cohesionados → H7 arco largo con capturas → H8 informe.

## 6. PRUEBAS REALIZADAS Y RESULTADOS (Fable, 2026-09-03, editor de Cesar)

- Compilación: 0 errores tras las costuras y tras los ajustes (auto-refresh apagado: receta en
  HANDOFF §3). 84 parámetros registrados.
- Entrada al modo por reflexión (`ModoLaboratorio=true` + `DayCycle.RestartRun(777002)`):
  «Mundo construido: plano=LABORATORIO seed=777002», panel y spawn correctos.
- Tubo en U (2 columnas de 8 de ancho, 1904 celdas de agua, 240 ticks): niveles 237/199 →
  219/217, agua conservada exacta, 150 mudanzas de presión. **La regla no local funciona.**
- Coste (300 ticks, 87 chunks despiertos, 13k celdas activas): 2,34 ms media, pico 9,0
  (LabLuz 5,4 ms cada ejecución). Campos 0,15 · presión 0,09 · difusión propia 0,67 · barrido 0,55.
- 3000 ticks headless en 7,4 s (406 ticks/s). Libro mayor a t=3565: emitida 2398, sumida 0,
  evaporado 104 celdas, condensado 0, infiltrado 135 celdas, depositado 1274, erosionado 969,
  presión 2169 mudanzas.
- Capturas: `costuras_01_sala.png` (sala del hogar, arena, hogar, piel de roca, arroyo debajo),
  `costuras_02/03_poza_t0/t3000.png` (poza turbia con sedimento y grava),
  `costuras_04_panoramica_t3565.png` (todo el plano: la fisura de arena cayó a la cámara
  profunda y el arroyo se cuela por ahí → laguna abajo).

## 7. PARÁMETROS / DEFAULTS ACTUALES

Los 84 de `Assets/Alkahest/Sim/LabParams.cs` (registro con ayuda). Cambios respecto al diseño:
evapBase 1, turbidezFuente 40, luz.cadaTicks 16. `_defaults.json` lo escribirá el panel (H2).

## 8. PROBLEMAS CONOCIDOS

1. **La fisura de arena es polvo y cae** (plano): el arroyo drena entero a la cámara profunda y
   el sumidero no recibe nada. Solución en H1 (`Arenisca` porosa estática).
2. **LabLuz 5 ms/ejecución** sobre el mundo entero (H5).
3. **Condensación = 0** en 2 min: el vapor no llega a la zona fría o la saturación está mal
   escalada (H3.1). La térmica propia sí enfría la cámara alta hacia 8 °C (ambiente por celda).
4. **Churn erosión↔depósito** en el lecho junto al manantial (H3.2: decidir número o rasgo).
5. **Editar `.cs` en Play** rompe la sesión (recarga de dominio por el RunCommand): receta en
   HANDOFF §3.
6. Sin presets/snapshots/vistas/pincel todavía (H2); sin plantas (H4); sin cuerpos (H6).
7. `RocaSuelta` hoy se comporta como piedra tallable (gancho `LabCuerpos` vacío).
8. La pestaña TIEMPO fija `LabPresupuestoMs` desde `LabParams.PresupuestoMs` cada frame (ok).

## 9. SIGUIENTE PASO EXACTO

**Opus 5**: leer `HANDOFF_OPUS.md` §0 y §3; ejecutar H1 (Arenisca + fisura + verificar el
circuito del agua con sondas y capturas) y anotar aquí los números; seguir con H2.
**Cesar**: correr `ca_playtest130.cmd` (commit + push de las costuras) cuando quiera.

## 10. CÓMO RETOMAR SIN ESTA CONVERSACIÓN

1. Leer este archivo, luego `docs/LAB/HANDOFF_OPUS.md`, luego `DISENO_LABORATORIO.md`.
2. `git --no-optional-locks status`.
3. Unity abierto (6000.5.7f1). Compilar con la receta (HANDOFF §3). Play → título → botón
   «laboratorio de leyes» → F8. Por MCP: RunCommand con
   `AlkahestGameBootstrap.ModoLaboratorio=true` (y los otros tres a false) y
   `DayCycle.RestartRun((int?)777002)` por reflexión (`BindingFlags` numéricos `(4|8|16|32)`).
4. Sondas: `FindObjectsByType(AlkahestSim)[0]` → `Stepper` → `StepOnce()` ×N, contadores `Lab*`,
   `Ms*`; captura con `Camera.main` → RenderTexture → PNG en `Laboratorio/capturas/`.
5. Seguir por §9.
