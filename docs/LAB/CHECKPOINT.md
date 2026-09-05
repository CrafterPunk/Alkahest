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

**Fase: 12 — LABORATORIO CERRADO (Fable, 2026-09-05, R150). `INFORME_FINAL.md` revisado y cerrado; física congelada; H6 congelado; H7 con jugador diferido; nivel de referencia intacto; Q16 cerrada (R26). Lo siguiente es un encargo aparte de Cesar: el diseño de la experiencia comercial.**
el título, simula (2,08 ms/tick), la presión hidrostática está verificada (tubo en U) y ahora
además es CONSERVATIVA, y **el circuito del agua se cierra**: manantial → arroyo → poza → aguas
abajo → sumidero, con un hilo permanente hacia la cámara profunda por la arenisca y por la
grieta atascada. El libro mayor cuadra al 0,113 % y hay una auditoría de conservación
permanente (`LabBalanceU`). El panel está COMPLETO (presets JSON, snapshots con PNG y libro, pincel de
materia, seis vistas de depuración, ayuda general). El libro mayor cuadra AL BIT (residuo 0 exacto). **Las cinco
cadenas de H3 ocurren, medidas, con un solo juego de números** (§6e): destilación, decantación
en la poza, canal que se sella con agua turbia y nunca con agua limpia, arcilla → terracota
estanca, y el alambique con serpentín frío. Faltan plantas (H4), cuerpos cohesionados (H6),
banco headless (H5) e informe (H8).
**H4**: la mecánica de plantas está completa y medida (§6f) — germinan, beben, suben savia,
crecen con luz, ramifican, transpiran, se marchitan en fibra, la fibra arde y la ceniza abona —
pero NO hay régimen estable de vegetación: nacen 36 y mueren 24. Tres causas corregidas y una
abierta (Q7).

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
- **D26 (R135, R8 de Fable) La condensación va al vecino MÁS FRÍO.** El jugador elige dónde
  gotea poniendo un bloque frío: el rocío se CONCENTRA en él (5 926 u) en vez de repartirse por
  el techo (0 u en la roca), y un serpentín en la pared con techo encima ya recibe rocío. El
  número de goteos BAJA (93 → 70 con el mismo plano) precisamente porque el mismo rocío cae
  desde menos celdas: la métrica buena no es cuántas gotas, es dónde caen.
- **D27 (R135, R9 de Fable + un añadido mío) El piso de la cámara alta y el REBOSADERO.** Solera
  de arcilla, lecho de 4 celdas de sedimento y labio de roca a los lados de la boca — pero el
  labio va **a ras del lecho**, no una celda por encima. Con el labio alto, la solera impermeable
  hace una BAÑERA: 24 de las 48 columnas del claro acaban bajo agua y bajo el agua no germina
  nada. A ras, el sobrante se va por la boca. Sustrato del claro a los 150 s: 28 % → **84 %**.
- **D28 (R135, R9 de Fable) Un sedimento con una PLANTA encima no se erosiona.** Una línea en
  `LabErosion`. Es lo que le da a la vegetación su papel sistémico (más plantas → menos lavado →
  más sustrato) y la única forma de que H4 tenga régimen estable sin apagar la erosión.
- **D29 (R135, HF1) EL AIRE DE CONTACTO.** Quien arde sin un vecino de aire, o ahogado en su
  propio humo, arde EN SORDINA (consume a ¼, calienta a ½, sin lengua, humo a ¼) y al agotarse
  deja CARBÓN en vez de brasa. Sin campo de oxígeno: el aire es el vacío que ya existe y el humo
  es el aire gastado. De esa sola regla salen la carbonera, el regulador, el tiro y la brasa
  bancada. Seis costuras `(R135)` en `SimStepper.cs` (excepción autorizada por el diseño).
- **D30 (R135, HF2) El vidrio como marcador de calor industrial**, y `fuego.hogarRaw` 220 → 170.
  El hogar gratuito hierve, seca y cuece la cara de la arcilla, pero NO vidria: para eso hace
  falta un recinto. Y apareció sola una cadena de encendido que nadie escribió: hogar → yesca de
  fibra → llama → carbón. **(Corregido en R136:** la explicación que di —«el hogar no puede
  prender el carbón porque 170 < 200»— era falsa; `TryIgnite` enciende por contacto con un 12 %
  por tick sin mirar la temperatura. La cadena es real, pero la sostiene la SOLERA DE CENIZA que
  separa el piloto del combustible: sin yesca que la atraviese, 144 celdas de carbón siguen
  intactas a los 300 s. Geometría, no umbral. Ver D31 y Q10.**)**
- **D31 (R136, C1) El hogar no calienta a nadie por encima de sí mismo.** `LabHogar` usa
  `LabCalentarHasta` con tope en `fuego.hogarRaw`, no `InjectHeat`, que sumaba sin límite y
  ponía a sus vecinos a 255 raw: arena con ceniza sobre el hogar se volvía VIDRIO en 27 s, sin
  horno. O sea que «el hogar es doméstico» era falso, y con él la frontera entre vivir y
  fabricar. Medido tras el parche: **0 vidrio a 3 000 ticks**, el agua hierve y la fibra prende.
- **D32 (R136, C2) Carbonizar no crea energía: `fuego.rendimientoCarbonPct` = 25 y la reserva
  del carbón 160 → 50.** Solo una de cada cuatro celdas ahogadas deja carbón; el resto, ceniza.
  Las dos constantes no son ajuste de dificultad: de los 224 000 raw de una carbonera de 400
  celdas de fibra, la quema ahogada suelta 112 000 y el carbón que nace guarda **112 200**.
  Mitad y mitad con un 0,2 % de diferencia. Una carbonera cambia cantidad por calidad y pierde
  por el camino, como la de verdad. Pico de carbón medido: **25,0 % exacto**.
- **D33 (R136, C3) El libro cuenta los TRES calores, y el cuarto contador que hizo falta.**
  `LabCalorLlama` + `LabCalorHogar` + `LabCalorFuego`, porque contar solo la brasa mentía: en un
  horno la llama pone 130 240 raw contra 45 400 de la brasa. Y `LabCalorCarbon`, que no estaba
  en la especificación: sin separar lo que suelta el carbón al RE-ARDER, la identidad de C2 se
  desviaba +27,6 % por doble conteo (la energía de una celda carbonizada se cuenta al nacer y
  otra vez al quemarse). Con él, la identidad cierra a **+1,0 %**.
- **D35 (R138, R15) El hogar calienta, no chispea.** Fuera las cuatro llamadas a `TryIgnite` de
  `LabHogar`: era la única línea por la que el hogar encendía con un 12 % de azar en vez de por
  temperatura, y por ella prendía carbón (200) al que no puede calentar por encima de 170. Ahora
  la cadena hogar → yesca de fibra (130) → llama → carbón es verdadera POR DOS NÚMEROS. Chispear
  es privilegio de una llama; una brasa eterna, que por diseño es más fría, enciende solo por
  temperatura. Medido: carbón pegado 6/6 intactas a 3 000 ticks, fibra seca prende en t=48, y el
  horno sigue en 18/18 porque la yesca la enciende `ApplyPhase`.
- **D36 (R138, R17) DOS libros de energía, porque uno solo mentía.** El nominal (`LabCalorFuego`,
  `LabCalorCarbon`, `LabCombustibleCarbon`, `LabUnidadesRespiradas`, `LabCalorNoSoltado`) mide
  calor por unidad de reserva y es el que se CONSERVA: ahí vive la identidad de la carbonera. El
  entregado (`LabInyectar` → `LabRawFuego/Llama/Brasa/Hogar/Frio`) mide los raw que de verdad se
  escriben en `temp[]` tras el recorte, y es el único que admite un TOTAL. Sumar el primero daba
  un total de nada — y peor, daba conclusiones invertidas: en raw entregados la llama pone entre
  el 6 % y el 29 %, no el 90 % ni los ¾ que habíamos escrito, porque suelta 622 040 nominales y
  entrega 105 579. Lo que ya está a 255 no admite más.
- **D34 (R136, Q8) El desagüe de grava es geometría del nivel, no una regla** — y drena al revés
  de lo que se temía: la grava le da al agua un camino al subsuelo en vez de dejarla correr
  hasta la boca, y el labio de grava fija el nivel freático a la altura del lecho. Conservación
  intacta (residuo 0). **H4 sigue abierto**: el banco ya no reproduce el encharcamiento y el
  régimen que falta medir es el del alambique real, jugando.
- **D22 (R134, H4) `suelo.compactVecinos` = 4, y era un número suelto en el stepper.**
  La compactación pedía `LabVecinosSolidos(i) >= 3`, escrito a mano (contra el invariante 5).
  Pero 3 vecinos sólidos los tiene también la CARA de un suelo (abajo y los dos lados), así que
  la superficie de cualquier huerto se volvía ARCILLA —que no es sustrato— y las plantas perdían
  la raíz de golpe (medido: sustrato 254 → 54 mientras arcilla 60 → 302 en 100 s). Con 4 la
  arcilla se forma bajo tierra, como en la realidad. La cadena de H3.4 no se rompe.
- **D23 (R134, H4) `suelo.capilarArriba` 2 → 16.** Con 2, un suelo empapado subía 1 unidad por
  visita a su superficie mientras el secado se llevaba 3: la cara de CUALQUIER suelo tendía a
  cero por mojado que estuviera por dentro, y nada podía germinar nunca. Con 16 se equilibra
  alrededor de 100. Es el número que hacía imposible la vegetación, y no estaba a la vista.
- **D24 (R134, H4) `luz.decayPlanta` 40 → 12.** Con 40, la segunda celda de tallo ya quedaba a
  oscuras (la luz del claro es 48-216) y ninguna planta pasaba de 4 de altura con el tope en 14.
  Con 12, bajo la boca del cielo llegan al tope y en el borde del claro apenas levantan una:
  la vegetación toma la forma de la luz, que es la regla 40 funcionando sola.
- **D25 (R134, H4) La planta TRANSPIRA** (`planta.transpira` = 2 u/visita, nuevo). No estaba en
  la spec, pero sin un sumidero la savia se quedaría dentro para siempre y ninguna planta se
  marchitaría nunca — el criterio "sin agua mueren" sería inalcanzable. Y trae acoplamiento
  gratis: un rincón plantado humedece el aire, así que condensa más.
- **D19 (R133, H3, decisión de arquitectura de Fable en R5.1) El aire no nace seco.**
  `aire.humedadInicialPct` = 60: cada celda de aire arranca al 60 % de SU PROPIA saturación
  (que depende de su ambiente), así que ninguna nace supersaturada y el mundo no llueve solo.
  Con el aire seco, el primer vapor que el jugador produce se gastaba ENTERO en humedecer el
  volumen: en una cámara de 2 548 celdas eso son ~350 celdas de agua tiradas antes de que
  ninguna pared pudiera sudar. **Efecto medido: el mecanismo de condensación por saturación,
  que estaba muerto (`LabCondensado` = 0), revive (755 en 9 000 ticks) y aparecen los PRIMEROS
  GOTEOS del laboratorio.** La evaporación del arroyo cae a la mitad, que es lo correcto.
- **D20 (R133, H3.2) `sed.depositoReposo` 8 → 24; la erosión se queda en 6.** Fable proponía
  subir el reposo Y bajar la erosión; midiendo los dos mandos por separado resultó que el
  reposo hace todo el trabajo (churn −57 % y la poza decanta MEJOR, 61 % contra 53 %) y que
  bajar la erosión empeora la clarificación y deja el lecho estático. El churn era ruido puro:
  las cuatro configuraciones dan el mismo resultado NETO (372-380 celdas) — 6 279 eventos para
  mover 375 celdas era 17 a 1.
- **D21 (R133) El BFS de `LabPresion` se queda dentro del mundo.** Encolaba vecinos sin
  comprobar los bordes: agua en la fila 0 → `c - W` negativo (excepción que mataba el tick), y
  agua en la columna 0 → vecino izquierdo en la fila anterior (cuerpo de agua envuelto por el
  borde). Lo cazó un escenario de banco con la grilla sin roca alrededor; en el plano no salta,
  pero un jugador que talle hasta el borde lo provoca. El tubo en U de Fable sigue dando sus
  números exactos de la R130 (237/199 → 219/217): la regla no cambió.
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

(R133) `Sim/LabParams.cs` (`aire.humedadInicialPct` nuevo → 87 parámetros; `sed.depositoReposo`
24; ayuda de `vapor.condensaC` ampliada con R6) · `Sim/SimLevelBuilder.Laboratorio.cs` (la
humedad inicial del aire, después del clima) · `Sim/SimStepper.Laboratorio.cs` (`LabPresion`
acotada a los bordes del mundo).

(R134) `Sim/SimStepper.Laboratorio.cs` (`LabPlanta` completo, germinación de la semilla y
espontánea, `LabNacerPlanta`, `LabSecarHacia` con tasa, sales 641/643) · `Sim/LabParams.cs`
(`planta.transpira`, `planta.abonoMuerte`, `suelo.compactVecinos` nuevos → 90 parámetros;
`suelo.capilarArriba` 16, `luz.decayPlanta` 12) · `Sim/SimRenderer.Laboratorio.cs` (el brote
más claro que el tallo, por `aux`).

(R135) `Sim/Universe.cs` (`Carbon` = 79, `Count` = 80, `Rellenar`) · `Sim/Universe.Laboratorio.cs`
(def del Carbón; `fuego.vidaHumo` al Smoke) · `Sim/LabParams.cs` (95 parámetros: `fuego.vidaHumo`,
`luz.decayHumo`, `suelo.permCarbon`, `fuego.vidrioRaw`, `fuego.vidrioVisitas`; `fuego.hogarRaw`
170) · `Sim/LabMateriales.cs` (permeabilidad del carbón) · `Sim/SimLevelBuilder.Laboratorio.cs`
(piso de la cámara alta con solera, lecho y rebosadero) · `Sim/SimStepper.Laboratorio.cs`
(`LabRespira`, `LabPasoSordina`, `LabVecinoEs`, la regla del vidrio, el vecino más frío, las
raíces que sujetan el suelo, `LabVidrio`, `LabCombustibleQuemado`/`LabCalorFuego`) ·
`Sim/SimStepper.cs` (**tercera excepción, autorizada por el diseño del fuego**: las costuras del
aire de contacto en `ProcessCombustion`/`ProcessBrasa` y el libro de energía, marcadas `(R135)`).

Nuevos: `Sim/LabParams.cs`, `Sim/LabMateriales.cs`, `Sim/SimStepper.Laboratorio.cs`,
`Sim/SimLevelBuilder.Laboratorio.cs`, `Sim/SimRenderer.Laboratorio.cs`, `Sim/Universe.Laboratorio.cs`,
`Game/LabPanel.cs`, `docs/LAB/*`, `Laboratorio/*`, `ca_playtest130.cmd`.

## 4. SISTEMAS AÑADIDOS

Ver `HANDOFF_OPUS.md` §1 (lista verificada) y `DISENO_LABORATORIO.md` §2-§6 (reglas).

## 5. PLAN (hitos de Opus, `HANDOFF_OPUS.md` §4)

H1 plano/arenisca y circuito del agua **(HECHO, §6b)** → H2 panel completo **(HECHO, §6c)**
→ H3 ciclo del agua completo **(HECHO, §6d y §6e)** → H4 plantas **(mecánica HECHA, §6f)** → cierres R8/R9 y FUEGO HF1-HF4 **(HECHO, §6g)** (presets, snapshots, pincel, vistas)
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

## 6e. R133 — H3 COMPLETO: LAS CINCO CADENAS (Opus 5, con R5-R7 de Fable)

Detalle y tablas: `Laboratorio/benchmarks/2026-09-03_r133_h3_completo.md`.

**H3.1 destilación** (ya en §6d) + la corrección de Fable: con el aire al 60 % de su saturación
llegan los **primeros goteos** del laboratorio (t=2 597 = 87 s; con el aire al 85 %, 64 s).

**H3.2 sedimentación**: la poza es un decantador de verdad — el agua entra a 96 de turbidez y
sale a 45 (limpia un 53-61 %). El churn erosión↔depósito era ruido: 6 279 eventos para un neto
de 375 celdas. Con `sed.depositoReposo` 24, mismo neto con la mitad de eventos y mejor
decantación (D20).

**H3.3 canal sellado**: con agua turbia (carga 255) el lecho de arena deja de infiltrar en
**~15 s**; con la turbidez del manantial (40), en ~100 s; con agua limpia, **nunca** (meseta de
14 850 u por bloque, plana durante 2 700 ticks). Y la costra tiene UNA celda de espesor: debajo
la arena sigue seca. La lección sale sola: *decanta primero, filtra después*.

**H3.4 arcilla**: el sedimento húmedo enterrado compacta en arcilla a los 106 s. Cocer NO ocurre
en un montón estático porque las dos condiciones se excluyen en el sitio (compactar pide humedad
100-230; cocer, ≤ 30) — hay que moldear enterrado y DESENTERRAR: expuesta al aire junto al hogar
cuece en **22-30 s**, y solo las 16 celdas de la CARA (la pieza queda cruda por dentro). Un
cuenco de terracota retiene 164 de 160 celdas en 300 s; el mismo cuenco de arena se queda en 3.

**H3.5 alambique**: el serpentín de `NucleoFrio` en el techo lleva el primer goteo de 84 s a
**7 s** y de 11 goteos a 93. Cerrar la cámara con tabiques NO ayuda (supersatura el aire a 42
sobre 36 pero deja el goteo igual): lo que decide el goteo no es cuánta humedad hay sino cuántas
celdas de aire tocan cada celda de pared, y una cámara pequeña tiene MENOS aire por pared. La
lección no es «frío y poco aire» sino **«una superficie muy fría»** — corrige la predicción de
Fable en R5.2. Presets `ref_destilacion` y `ref_alambique`.

Los cinco fenómenos ocurren con **un solo juego de números**: no hizo falta un preset por
cadena porque las cadenas no compiten entre ellas.

## 6f. R134 — H4: PLANTAS Y FIBRA (mecánica completa; sin régimen estable)

Detalle y tablas: `Laboratorio/benchmarks/2026-09-03_r134_h4_plantas.md`.

**Lo que funciona, medido**: germinan sobre sustrato húmedo e iluminado (36 nacidas sin plantar
ninguna) · **sin luz no** (0 en tres corridas) · **sin agua no** (0) · la raíz bebe, la savia
sube, la punta crece con luz y ramifica · sin savia mueren y dejan **fibra** (24 muertas → 13
fibras) · **la fibra prende y deja ceniza** (fuego → brasa → ceniza 18 → 37) · `LabCampos`
**0,48-0,50 ms** (el criterio pedía < 0,5).

**Lo que NO**: a los 150 s han muerto todas. No hay régimen estable de vegetación.

**Tres causas corregidas** (D22, D23, D24) y una abierta: el piso de la cámara alta es polvo de
2 celdas con un agujero al lado (la boca de la chimenea), así que el goteo lo erosiona y lo
escurre — `sustrato en el claro 74 → 22` en 300 s, y apagando la erosión 74 → 65, o sea que la
erosión explica dos tercios. Escalado a Fable como **Q7**.

**Mapa de luz de la cámara alta** (medido, con la boca en x118-124): x100=72 · x110=152 ·
x120=216 · x130=168 · x140=88 · x145=48 · x150 en adelante = 0. **47 de 91 celdas** superan el
mínimo de 40: el claro es de sobra, `planta.luzMin` se queda en 40.

**Aviso operativo**: un serpentín de núcleo frío pintado en x104-133 **tapa la boca del cielo** y
deja la cámara a oscuras. Al montar el alambique hay que dejar la ventana libre (x104-116 y
x126-145). Costó una tarde de diagnóstico.

## 6g. R135 — CIERRES DE Q6/Q7 Y EL DOMINIO DEL FUEGO (HF1-HF4)

Detalle y tablas: `Laboratorio/benchmarks/2026-09-04_r135_cierres_y_fuego.md`.

**Cierres.** R8 aplicado (D26): su criterio numérico (≥ 93 goteos) NO se cumple —salen 70— pero
la medida explica por qué y demuestra que el efecto sí: el rocío pasa de repartirse por el techo
a concentrarse ENTERO en el bloque frío (5 926 u en el serpentín, **0 en la roca**), y un
serpentín en la pared con techo encima ya recibe rocío. R9 aplicado (D27, D28) más un rebosadero
que no estaba en la spec y que era imprescindible; el sustrato del claro pasa de 28 % a **84 %**
a los 150 s. El criterio revisado de H4 sigue sin cumplirse y el diagnóstico está medido:
**el mismo alambique que trae el agua ahoga el huerto** (Q8).

**HF1 (el aire de contacto).** Carbonera con boca 1: **100 % de carbón, 0 de ceniza, 0 de llama,
0 de humo**; al abrir la boca aparece lo que se consume del todo (19 % de ceniza con boca 4). Una
pila maciza al aire es su propia carbonera (sus 324 celdas interiores no tienen aire). Coste
**+2,6 %** sobre el laboratorio en reposo (1,95 contra 1,90 ms/tick) con 5 000 celdas ardiendo.
El humo persiste con `vidaHumo` 400 (128 celdas estables) a ≤ 2 ms/tick. Regresión del agua
idéntica (residuo 0, sumidero 4 819, 1,90 ms/tick).

**HF2 (el horno).** 386 °C sostenidos dentro del recinto y **29 celdas de vidrio**; el hogar
suelto (170 raw) no vidria nada. La curva boca→temperatura sale **PLANA** (228/232/231 raw): con
el carbón macizo la boca no regula, porque el grueso no respira igualmente. Escalado como Q9.

**HF3 (la tolva).** **324 s de mundo** con el fogón por encima de 150 raw y **cero
intervenciones** (la aceptación pedía 300 s).

**HF4 (el libro de energía).** `LabCombustibleQuemado` y `LabCalorFuego`; su razón mide por sí
sola cuánto de la quema ocurrió sin respirar (10,3 frente a los 14 de la fibra al aire → el 53 %
de la tolva ardió ahogada).

**Veredicto §7 del diseño del fuego: 3,5 de 5.** Cumplen la máquina escondida (horno → vidrio),
la automatización sin jugador (tolva) y el libro de energía; falla el mando monótono tal como
está formulado (Q9) y queda pendiente la cadena cruzada (B-F6, va en H7 jugando).

## 7. PARÁMETROS / DEFAULTS ACTUALES

Los **95** de `Assets/Alkahest/Sim/LabParams.cs` (registro con ayuda). Cambios respecto al
diseño: evapBase 1, turbidezFuente 40, luz.cadaTicks 16, `suelo.permArenisca` = 30 (R131);
`vapor.condensaC` = 10 °C (nuevo), `vapor.vidaVapor` 180 y `vapor.ascenso` 12 (R132); y
`aire.humedadInicialPct` = 60 (nuevo) más `sed.depositoReposo` = 24 (R133); y de la R134
`planta.transpira` = 2, `planta.abonoMuerte` = 40 y `suelo.compactVecinos` = 4 (nuevos), más
`suelo.capilarArriba` 2 → 16 y `luz.decayPlanta` 40 → 12. De la R135: `fuego.vidaHumo` = 400,
`luz.decayHumo` = 24, `suelo.permCarbon` = 20, `fuego.vidrioRaw` = 200 y `fuego.vidrioVisitas`
= 60 (nuevos), más `fuego.hogarRaw` 220 → **170**.
`_defaults.json` lo escribe el panel al arrancar (H2, hecho).

## 8. PROBLEMAS CONOCIDOS

1. ~~**La fisura de arena es polvo y cae**~~ **RESUELTO (R131, H1)**: `Arenisca` porosa
   estática en la fisura y grava sobre repisa en la grieta. Sumidero 10 949 celdas a t=18 000.
2. **LabLuz 5 ms/ejecución** sobre el mundo entero (H5).
3. ~~**Condensación = 0**~~ **RESUELTO (R132, H3.1)**: no era la saturación, era
   `Steam.condensesAt` escrito a mano en 60 °C (por encima del ambiente de la cueva). Ver D17.
4. ~~**Churn erosión↔depósito**~~ **RESUELTO (R133, H3.2)**: era ruido (17 eventos por celda
   de cambio neto). `sed.depositoReposo` 24 lo parte por la mitad y además decanta mejor. Ver D20.
5. **Editar `.cs` en Play** rompe la sesión (recarga de dominio por el RunCommand): receta en
   HANDOFF §3.
6. ~~Sin presets/snapshots/vistas/pincel~~ **RESUELTO (R131, H2)**. ~~Sin plantas~~
   **RESUELTO (R134, H4)**: la mecánica está entera; lo que falta es que la vegetación se
   MANTENGA (§6f, escalado Q7). Sin cuerpos cohesionados (H6).
7. `RocaSuelta` hoy se comporta como piedra tallable (gancho `LabCuerpos` vacío).
8. La pestaña TIEMPO fija `LabPresupuestoMs` desde `LabParams.PresupuestoMs` cada frame (ok).
9. ~~**(R131) Residuo de conservación**~~ **RESUELTO (R132)**: no estaba en el barrido
   ordinario. Era `LabAgua` saliendo con `vol` sin sincronizar (cinco sitios). Residuo 0.

## 9. SIGUIENTE PASO EXACTO

**(Fable, 2026-09-04)** Q6 y Q7 cerradas en `PREGUNTAS_A_FABLE.md` (R8: vecino condensable más
frío; R9: plano de la cámara alta + raíces que sujetan el sedimento; R10: orden). Después de
aplicarlas y pasar la regresión, Opus entra en el DOMINIO DEL FUEGO (`DISENO_FUEGO.md`, hitos
HF1-HF4) antes de H5/H6. Lo de abajo es el estado que dejó Opus en la R134:


**Opus 5**: H1 (§6b), H2 (§6c), H3 entero (§6d, §6e), H4 en su mecánica (§6f) y los cierres
R8/R9 más el DOMINIO DEL FUEGO HF1-HF4 (§6g) cerrados. **Parado aquí por instrucción de Cesar:
no entrar en H5 ni H6.** Escalados abiertos: Q8 (el alambique ahoga el huerto) y Q9 (la boca del
horno no regula con el combustible macizo).

Lo anterior, para cuando se retome:
Presets `ref_h1_circuito`, `ref_destilacion` y `ref_alambique`. Lo siguiente, por orden de
utilidad:

1. **H5 (banco headless y rendimiento)** — es lo más rentable ahora mismo: los siete escenarios
   del HANDOFF ya existen a trozos dentro de los RunCommand de las R131-R134; recogerlos en
   `Sim/LabBench.cs` los hace repetibles y da la tabla que el informe necesita. Además el banco
   ya ha cazado dos bugs (el desbordamiento de `LabPresion` y el conflicto suelo/arcilla) sin
   proponérselo. Incluye acotar `LabLuz` (sigue costando ~5 ms cada ejecución).
2. **H6 (cuerpos cohesionados)**, que es el último sistema sin empezar.
3. **H7 (arco largo jugando)** y **H8 (informe)**.

Pendientes menores: el SIFÓN y la fuente artesiana de H3.5 (la presión ya está verificada con el
tubo en U, así que es una demostración, no una incógnita) — hacerlo en H7, jugando. Y la
respuesta de Fable a Q6 (condensación) y Q7 (régimen estable de vegetación).
**Cesar**: correr `ca_playtest131.cmd` (commit + push de H1) cuando quiera.

**Hallazgo operativo (R131)**: en `Unity_RunCommand` NO hace falta reflexión para los tipos del
proyecto — `using Alkahest.Sim;` y llamar a `Universe.Create(777002)`, `new CellGrid()`,
`SimLevelBuilder.BuildLaboratorioDeLeyes(g)`, `new SimStepper(u, g) { LabActivo = true }` y
`st.Step()` compila y corre **sin entrar en Play**, a ~530 ticks/s. Eso adelanta el banco
headless de H5 y elimina el riesgo de romper la sesión de Play editando `.cs`. La reflexión
solo hace falta para los miembros privados (`DayCycle.RestartRun`), con `BindingFlags`
numéricos porque `using System.Reflection` está prohibido en RunCommand. Las capturas también
salen sin Play: se dibuja la grilla a un `Texture2D` y se escribe el PNG.

**(Opus, 2026-09-05, R149 — leer antes de nada.)** **H8 ESCRITO**:
`docs/LAB/INFORME_FINAL.md`, secciones A-F, borrador para que Fable revise. Con eso quedan hechos
**todos los hitos que no dependen de un jugador**: H1-H5, HF1-HF5e-B, H7s y H8. H6 congelado; H7
con jugador y HF5e-A, deferidos (R25).

**Lo que el informe sostiene**, y donde está el debate:
- La mitad medible de la apuesta **se cumple**: diez cadenas observables, **nueve no programadas**,
  a 1,6 ms/tick (peor caso 3,1).
- La otra mitad —que el conocimiento sustituya el trabajo manual— **está a medias**: el único
  proceso que corre solo durante minutos es la tolva (466 s). Falta una máquina que produzca sin
  volver a tocarla.
- Fuego **4 de 5** medidos; el criterio 4 como «observado en simulación autónoma · con jugador no
  evaluado».
- Estimación para llevarlo al juego: **3-4 meses** de una persona, sin H6 — marcada **[FABLE]**
  junto con la valoración, que son las dos que piden segunda opinión.

Todo lo que dependía de una persona delante va marcado **«no evaluado — ni aprobado ni refutado»**,
con una sección (A.4) dedicada a decirlo.

**Siguiente paso exacto: correr `ca_playtest149.cmd`.** Después lo deciden Cesar y Fable: **Q16**
(la luz del huerto, lo único abierto), la revisión conjunta del informe, y el diseño de la
experiencia — a partir de `HANDOFF_SABADO.md` §3, ya sin observaciones de jugador.

**(Fable, 2026-09-05, R150 — LABORATORIO CERRADO.)** `INFORME_FINAL.md` revisado como arquitecto
y cerrado: marcas **[FABLE]** resueltas (valoración C, estimación E, Q16), conclusiones afinadas
(reglas programadas frente a cadenas no guionizadas; el fuego en 4 de 5 con los medios puntos
explicados; qué falta exactamente para «producir sin volver a tocar»; los hashes prueban
determinismo en una máquina y una build, no cross-machine ni lockstep), y una sección G de cierre
formal. Q16 cerrada en R26: el nivel de referencia no se toca; H4 «no se cumple, causa doble
aislada» (luz: 7 de 73 caras; y con luz, la humedad de la cara bajo goteo —medido en banco
`2026-09-05_r150_fable_q16_huerto_iluminado.md`: 9 plantas contra 2, todas mueren); el huerto que
vive es diseño de nivel de la fase siguiente. Fechas 2026-09-06 corregidas a 2026-09-05 en docs y
nombres de benchmarks. **Física congelada; H6 congelado; H7 con jugador diferido a después de la
primera experiencia.** Lo siguiente no es del laboratorio: el encargo de diseño de la experiencia
comercial lo da Cesar aparte. Cesar: `ca_playtest150.cmd`.

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
