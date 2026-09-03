# CHECKPOINT DE CONTINUIDAD — EXPERIMENTO MAYOR: "encontrar el juego dentro de la simulación"

*(Documento vivo. LEER PRIMERO si retomas este trabajo sin la conversación original.
Fuente de verdad: este archivo + `docs/LAB/*.md` + `Laboratorio/` (presets, capturas,
benchmarks) + el código bajo `Assets/Alkahest/`. Se actualiza antes y después de cada cambio
grande. Quien retome: Opus 5 según `docs/LAB/HANDOFF_OPUS.md`.)*

Última actualización: 2026-09-03 (Opus 5, fin de H1). HEAD `36805fa` (Ronda 130: las costuras
de Fable ya commiteadas por Cesar). Rama `main`. Los cambios de H1 están SIN commitear: Cesar
corre `ca_playtest131.cmd`. NO hacer `git push` (regla 6b del proyecto).

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

**Fase: 6 — H1, H2 y H3.1 CERRADOS; sigue H3.2-H3.5.** El laboratorio existe, compila sin errores, entra desde
el título, simula (2,08 ms/tick), la presión hidrostática está verificada (tubo en U) y ahora
además es CONSERVATIVA, y **el circuito del agua se cierra**: manantial → arroyo → poza → aguas
abajo → sumidero, con un hilo permanente hacia la cámara profunda por la arenisca y por la
grieta atascada. El libro mayor cuadra al 0,113 % y hay una auditoría de conservación
permanente (`LabBalanceU`). El panel está COMPLETO (presets JSON, snapshots con PNG y libro, pincel de
materia, seis vistas de depuración, ayuda general). El libro mayor cuadra AL BIT (residuo 0 exacto). El ciclo
agua→vapor→condensación FUNCIONA: hervir sobre el hogar llueve en la cámara alta en 15-33 s de
mundo (§6d). Faltan H3.2-H3.5 (sedimentación, colmatación, arcilla, sifón), plantas, cuerpos
cohesionados, banco headless e informe.

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
- **D17 (R132, H3.1) El punto de rocío del vapor visible es un PARÁMETRO y vale 10 °C.**
  `Steam.condensesAt` estaba escrito a mano en 60 °C dentro de `AplicarOverridesLaboratorio`,
  contra el invariante 5 del HANDOFF ("todo número físico vive en LabParams"), y era el número
  que rompía la cadena entera del agua: por encima de los 20 °C de la cueva, cada celda de
  vapor se volvía agua a dos celdas del fuego. Medido: CERO celdas de vapor vivas en toda la
  corrida, con cualquier `vidaVapor`. Ahora es `vapor.condensaC` = 10 °C — por debajo del
  ambiente (el vapor VIAJA) y por encima de la cámara alta a 8 °C (condensa donde hace frío).
  Con él, `vidaVapor` 60 → 180 (la chimenea mide ~65 celdas) y `vapor.ascenso` 6 → 12.
- **D18 (R132) Toda salida anticipada de `LabAgua` sincroniza `hum[i] = vol`.** Son CINCO: las
  cuatro por `vol <= 0` y el DEPÓSITO. `vol` es local y `hum[i]` solo se escribe al final, así
  que salir sin sincronizar hace que el auditor apunte como destruido lo que se acababa de
  transferir. Con las cinco, el residuo de conservación es **0 exacto** en todos los escenarios.
- **D15 (R131, H2) El panel guarda en `Laboratorio/presets/` con JSON escrito a mano.**
  `JsonUtility` no serializa diccionarios y el formato del handoff es un mapa clave→número;
  son 40 líneas de escritor y lector para un formato que se edita en el bloc de notas y se
  versiona en git (un `git diff` contra `_defaults.json` cuenta la historia del experimento).
  El lector es TOLERANTE a propósito: una clave que ya no existe se ignora, una que falta se
  queda como está, y el panel cuenta ambas — así un preset de hoy sigue cargando cuando H4
  añada parámetros de plantas.
- **D16 (R131, H2) Las vistas son una CUARTA TEXTURA, no un modo de `ComputeCellColor`.**
  Mismo patrón que el velo de líquidos (R129): textura propia, sprite propio (orden 54, alfa
  150), rellenada en el mismo barrido por chunks. Descartado teñir el color de la celda: eso
  obligaría a meter el laboratorio dentro del camino caliente del render del juego normal.
  Coste cero cuando no se usa: la textura no se crea hasta que alguien elige una vista, y
  `LabPanel.OnDestroy` devuelve `VistaLab` a Ninguna al salir del laboratorio (es estática y
  sobrevive a la escena: sin eso, el overlay se quedaría encendido en el juego normal).
- **D13 (R131, H1) La fisura es ARENISCA y la grieta está ATASCADA DE GRAVA.** La fisura era
  arena suelta: se derrumbaba y el arroyo entero se colaba a la cámara profunda. Ahora es
  `Arenisca` (id 78), roca porosa estática: el agua la cruza despacio y sale limpia. La grieta
  x336-343 se mantiene, pero rellena de `Grava` sobre una repisa de `Arenisca` en y111: abierta
  era un segundo pozo de 8 celdas en mitad del lecho y el sumidero seguía sin ver una gota.
  Atascada sangra un hilo (≈0,24 celdas/s) y deja pasar el resto. Además la grava se COLMATA
  con los finos del manantial, así que el hilo se cierra solo con el tiempo, y destaparla a
  cincel es un mando real para el jugador. Alternativas descartadas (regla 15): estrechar la
  grieta (cualquier agujero en el lecho se traga TODO el caudal, el ancho da igual) y dejarla
  abierta confiando en que la cámara profunda se llene (240×50 celdas: horas de mundo).
- **D14 (R131) Auditoría de conservación permanente** (`SimStepper.LabBalanceU`): la suma de
  todo lo que el laboratorio crea o destruye de `humedad[]`, contada en los tres únicos sitios
  que escriben sin restar en otro lado (`LabNacerAgua`, `LabTransformar`, el vaciado del poro
  al exudar/gotear). El invariante 3 del HANDOFF pasa a comprobarse con una resta exacta en vez
  de a ojo. Descartado contar solo por celdas: el sumidero traga celdas a medio llenar, así que
  "255 × celdas" sobreestimaba el caudal un 4 % (por eso también existe `LabAguaSumidaU`).
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
(R131, H1) Además: `Sim/Universe.cs` (id `Arenisca`=78, `Count`=79, `Rellenar`) ·
`Sim/Universe.Laboratorio.cs` (def de la Arenisca) · `Sim/LabParams.cs`
(`suelo.permArenisca`, 85 parámetros) · `Sim/LabMateriales.cs` (Arenisca en las 4 tablas) ·
`Sim/SimStepper.Laboratorio.cs` (Arenisca en `LabCampos`/`LabK`/`LabC`; depósito conservativo;
presión conservativa; `LabBalanceU` y `LabAguaSumidaU`) · `Sim/SimRenderer.Laboratorio.cs`
(la arenisca mojada se oscurece) · `Sim/SimLevelBuilder.Laboratorio.cs` (fisura y grieta).

(R131, H2) Nuevo: `Game/LabPresets.cs` (presets, comparación, snapshots). Modificados:
`Game/LabPanel.cs` (pestañas PRESETS/PINCEL/VISTAS, campos de texto con la guarda de la regla
12, anillo del radio, ayuda general) · `Sim/SimRenderer.Laboratorio.cs` (las seis vistas y la
cuarta textura) · `Sim/SimRenderer.cs` (**la excepción del HANDOFF §2**: 5 costuras de 7
líneas — `LabVistaAntesDelFrame` en RenderFrame, la bandera por chunk, `LabVistaCelda` en el
bucle, `LabVistaSetPixels` al cerrar el chunk y `LabVistaApply` en el Apply).

(R132) `Sim/SimStepper.Laboratorio.cs` (las cinco salidas sincronizadas, abono y cocción
conservativos, intercambio de temperatura en la presión, `LabCondensadoGas`) ·
`Sim/LabParams.cs` (`vapor.condensaC` nuevo, 86 parámetros; defaults de vida y ascenso) ·
`Sim/Universe.Laboratorio.cs` (`ReaplicarVapor` aplica también el punto de rocío) ·
`Sim/SimStepper.cs` (**segunda excepción, autorizada por Fable en R2**: dos líneas
`if (LabActivo) LabCondensadoGas++;` en las dos ramas de condensación del vapor visible).

Nuevos: `Sim/LabParams.cs`, `Sim/LabMateriales.cs`, `Sim/SimStepper.Laboratorio.cs`,
`Sim/SimLevelBuilder.Laboratorio.cs`, `Sim/SimRenderer.Laboratorio.cs`, `Sim/Universe.Laboratorio.cs`,
`Game/LabPanel.cs`, `docs/LAB/*`, `Laboratorio/*`, `ca_playtest130.cmd`.

## 4. SISTEMAS AÑADIDOS

Ver `HANDOFF_OPUS.md` §1 (lista verificada) y `DISENO_LABORATORIO.md` §2-§6 (reglas).

## 5. PLAN (hitos de Opus, `HANDOFF_OPUS.md` §4)

H1 plano/arenisca y circuito del agua **(HECHO, §6b)** → H2 panel completo **(HECHO, §6c)**
→ H3.1 ciclo del agua **(HECHO, §6d)** (presets, snapshots, pincel, vistas)
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

## 6b. H1 — EL CIRCUITO DEL AGUA (Opus 5, banco headless + verificación jugando)

Detalle completo y capturas: `Laboratorio/benchmarks/2026-09-03_h1_circuito_del_agua.md`.

| t | nivel poza x290 (labio 132) | x400 | cámara profunda | **sumida** | exudado | descuadre |
|---:|---:|---:|---:|---:|---:|---:|
| 0 | 128 | — | 0 | 0 | 0 | 0 (0,000 %) |
| 3 000 | 136 | 131 | 17 | 759 | 4 | 426 (0,081 %) |
| 9 000 | 136 | 131 | 15 | 4 802 | 72 | 616 (0,113 %) |
| 18 000 | 136 | 131 | 15 | 10 949 | 160 | 632 (0,113 %) |

**Régimen permanente desde t≈3 000**: los niveles no se mueven en 15 000 ticks y el sumidero
traga 20 celdas/s, exactamente lo que entrega el manantial. La arena del mundo sigue siendo 95
celdas en t=18 000 (el montículo de la sala): la fisura ya no se derrumba. 2,08 ms/tick.
Libro a t=18 000: emitida 12 095 · sumida 10 949 · infiltrado 148 939 u · evaporado 63 271 u ·
condensado 0 (H3) · depositado 21 256 · erosionado 20 283 · presión 11 084.

Tres correcciones de conservación encontradas midiendo (el balance salía +10,4 %):
el depósito escribía 255 de humedad fija en vez del volumen real; `LabPresion` aniquilaba el
vapor del hueco al mudar el agua (−4,5 u por mudanza, el 85 % del descuadre; ahora el aire se
muda al hueco que deja el agua); y la auditoría no cubría el vaciado del poro al exudar.

Verificado JUGANDO (regla 52): Play → `ModoLaboratorio` + `RestartRun(777002)` → 10× sostenido
(`LabMultiplicadorReal` = 10,0), 0 errores en consola, capturas
`R131_h1_vivo_panoramica/fisura/grieta.png`.

## 6c. H2 — EL PANEL COMPLETO (Opus 5, verificado jugando)

**Presets** (`Laboratorio/presets/*.json`): guardar con nombre y nota, cargar, listar, «TODO A
DEFAULTS», y una tabla «QUÉ HE TOCADO» que lista los parámetros que no valen su default y, si
hay un preset elegido, también su valor allí. `_defaults.json` se escribe solo al arrancar el
panel. Probado: guardar → cambiar 5 números → defaults (los 5 vuelven a fábrica) → cargar
(los 5 vuelven a 7/111/55/42/21), 85 aplicados, 0 desconocidas, 0 ausentes; comparar lista
exactamente esos 5.

**Snapshot** (un botón, un nombre): deja `<nombre>.json` (preset), `<nombre>.png` (la foto,
misma receta URP que `GaleriaCurador.Capturar`) y `<nombre>_libro.json` (censo por material,
libro mayor completo, inventario de agua por clase, tick, multiplicador y dónde estaba el
muñeco). El primero es `ref_h1_circuito`. **Y sirvió para una comprobación de verdad**: en ese
libro, `inventario total 251 752 − inventario inicial 184 535 = 67 217 = balanceU` **exacto**,
o sea que la auditoría de conservación de H1 también cuadra al bit en el juego en vivo, no
solo en el banco headless.

**Pincel de materia**: 22 entradas en cuatro grupos (SUELO, VIDA, LEYES, FLUIDOS), radio 0-8
con −/+ y anillo en el cursor, izquierdo pinta y derecho borra. Con el pincel armado
`BloqueaHerramientas` es true aunque el ratón esté fuera del panel: no se talla y se pinta a la
vez. El agua turbia va por `PaintLab` (los finos son un campo: `PaintStable` la haría nacer
limpia); la brasa y el fuego por `PaintCell` a 220 raw (regla 22).

**Vistas de depuración**: ninguna · temperatura (diferencia con el ambiente DE CADA CELDA:
gris/rojo/azul) · humedad (negro→cian) · carga (negro→ámbar) · reposo (negro→violeta) · luz
(negro→blanco) · chunks (verde = despierto). Verificadas en vivo: la de temperatura enseña el
penacho del hogar subiendo por la chimenea (la convección funciona) **y el arroyo más FRÍO que
la cueva, porque evaporar enfría** — calor latente visible; la de luz, el cono del hogar y la
columna que baja por la boca del cielo.

**Ayuda general** plegable: qué es una visita, por qué 255 = una celda, cómo se traduce raw a
°C, qué significa el punto ●, y el aviso de que los chunks dormidos no repintan.

Regresión comprobada: con `ModoLaboratorio` en false, `LabActivo`, el tinte, `VistaLab`, el
panel y el sprite de la vista están todos apagados o no existen, y `BloqueaHerramientas` es
false. Capturas: `R131_h2_panel_presets_vista_humedad.png`, `R131_h2_panel_pincel.png`,
`R131_h2_vista_temperatura.png`, `R131_h2_vista_luz.png`.

## 6d. R132 — CONSERVACIÓN EXACTA Y EL CICLO DEL AGUA (H3.1)

Detalle y tablas: `Laboratorio/benchmarks/2026-09-03_r132_conservacion_y_destilacion.md`.

**El residuo de conservación es 0.** El diagnóstico de Fable (el auditor leía el volumen viejo
en `LabAgua`) era correcto y su parche llevó 632 → 144; la medida encontró el resto en la
QUINTA salida, que su parche no listaba: el depósito. Ahora
`Σ humedad(t) − Σ humedad(0) == LabBalanceU` **al bit** a t=3 000, 9 000 y 18 000.

**H3.1 cumple con margen.** La hipótesis de Fable (el vapor moría de vejez a mitad de chimenea)
resultó falsa al medirla: CERO celdas de vapor vivas con cualquier `vidaVapor`, porque
`condensesAt` estaba en 60 °C y la cueva a 20 °C. Con `vapor.condensaC` = 10 °C, `vidaVapor`
180 y `vapor.ascenso` 12: **primera gota de agua líquida en la cámara alta a los 450-1000 ticks
= 15-33 s de mundo** (el criterio pedía < 3 min), 21 celdas de agua arriba y el sedimento seco
del piso de 0 a 17-22 de humedad. Preset `ref_destilacion`.

Regresión: el circuito de H1 idéntico (residuo 0, poza 136, sumidero 4 802 a t=9 000,
1,89 ms/tick).

## 7. PARÁMETROS / DEFAULTS ACTUALES

Los **86** de `Assets/Alkahest/Sim/LabParams.cs` (registro con ayuda). Cambios respecto al
diseño: evapBase 1, turbidezFuente 40, luz.cadaTicks 16, `suelo.permArenisca` = 30 (R131) y,
de la R132, el nuevo `vapor.condensaC` = 10 °C más `vapor.vidaVapor` 60 → 180 y
`vapor.ascenso` 6 → 12. `_defaults.json` lo escribe el panel al arrancar (H2, hecho).

## 8. PROBLEMAS CONOCIDOS

1. ~~**La fisura de arena es polvo y cae**~~ **RESUELTO (R131, H1)**: `Arenisca` porosa
   estática en la fisura y grava sobre repisa en la grieta. Sumidero 10 949 celdas a t=18 000.
2. **LabLuz 5 ms/ejecución** sobre el mundo entero (H5).
3. ~~**Condensación = 0**~~ **RESUELTO (R132, H3.1)**: no era la saturación, era
   `Steam.condensesAt` escrito a mano en 60 °C (por encima del ambiente de la cueva). Ver D17.
4. **Churn erosión↔depósito** en el lecho junto al manantial (H3.2: decidir número o rasgo).
5. **Editar `.cs` en Play** rompe la sesión (recarga de dominio por el RunCommand): receta en
   HANDOFF §3.
6. ~~Sin presets/snapshots/vistas/pincel~~ **RESUELTO (R131, H2)**. Sin plantas (H4); sin
   cuerpos (H6).
7. `RocaSuelta` hoy se comporta como piedra tallable (gancho `LabCuerpos` vacío).
8. La pestaña TIEMPO fija `LabPresupuestoMs` desde `LabParams.PresupuestoMs` cada frame (ok).
9. ~~**(R131) Residuo de conservación**~~ **RESUELTO (R132)**: no estaba en el barrido
   ordinario. Era `LabAgua` saliendo con `vol` sin sincronizar (cinco sitios). Residuo 0.

## 9. SIGUIENTE PASO EXACTO

**Opus 5**: H1 (§6b), H2 (§6c) y H3.1 (§6d) cerrados; presets `ref_h1_circuito` y
`ref_destilacion` guardados. Siguen los cuatro puntos que quedan de H3 (HANDOFF §4):
**H3.2** sedimentación en la poza (decidir el churn erosión↔depósito: ¿sube `sed.depositoReposo`
y baja `sed.erosionPct`, o es un rasgo?), **H3.3** canal sellado por colmatación (medir cuánto
tarda la arena en dejar de infiltrar con agua turbia frente a agua limpia — el pincel ya
distingue las dos), **H3.4** arcilla → secar → compactar → cocer → cuenco de terracota estanco,
**H3.5** sifón y fuente artesiana con el pincel. Cada uno deja su `ref_*.json` con captura y
números. Después H4 (plantas).
**Cesar**: correr `ca_playtest131.cmd` (commit + push de H1) cuando quiera.

**Hallazgo operativo (R131)**: en `Unity_RunCommand` NO hace falta reflexión para los tipos del
proyecto — `using Alkahest.Sim;` y llamar a `Universe.Create(777002)`, `new CellGrid()`,
`SimLevelBuilder.BuildLaboratorioDeLeyes(g)`, `new SimStepper(u, g) { LabActivo = true }` y
`st.Step()` compila y corre **sin entrar en Play**, a ~530 ticks/s. Eso adelanta el banco
headless de H5 y elimina el riesgo de romper la sesión de Play editando `.cs`. La reflexión
solo hace falta para los miembros privados (`DayCycle.RestartRun`), con `BindingFlags`
numéricos porque `using System.Reflection` está prohibido en RunCommand. Las capturas también
salen sin Play: se dibuja la grilla a un `Texture2D` y se escribe el PNG.

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
