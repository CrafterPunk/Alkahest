# CRÍTICA · «Días sin manos» (campaña de situaciones + sandbox) · lente INGENIERÍA Y EVIDENCIA

*(Director técnico, segunda pasada, 2026-09-12. Leídos enteros: `campana-situaciones.md`,
`01_LEYES.md`, las refutaciones de sello (×2), balanza y cuna (×2), `_huecos_y_combinaciones.md`,
`00_ENCARGO_Y_CRITERIO.md` §3, `03_MERCADO.md` §1-2. Código leído: `LabBench.cs` completo (los nueve
montajes, `Correr` :265-344, `MedirLecho`, `ArcoMuestra`), `SimStepper.Laboratorio.cs` completo
(`LabPasadas` :134-153, `LabCampos` :201-239, `LabPoroso` y la germinación :698-712, `LabPlanta`
:801-907, `LabHogar` :917-947, `LabTragar` :1026-1033, `LabLuz` :1177-1266, `LabDifusionTermica`
:1306-1345, `LabCuerpos` :1290), `SimStepper.cs` (`Step` :257-338, `ProcessIfNeeded` :346-353,
autoignición :685-698, `ProcessFire` :1644-1728), `LabParams.cs` completo, `AlkahestSim.cs` (bucle
:358-406, puertas :564-841), `LabPanel.cs` (multiplicador :381-387, presets :439-500),
`LabPresets.Guardar` :85-106, `SimSync` :161-164 y :1122-1128, `Universe.Laboratorio.cs` (Fibra,
Hogar, Carbon), `CellGrid.cs` (arrays y `chunkTouchedTick`), `LabMateriales.cs`; bancos
`2026-09-06_2149_banco.md`, R141, R148, R150. Mi único tema: qué pide del sustrato, qué evidencia la
sostiene o la contradice, y la prueba más barata que la mata.)*

## 0. Veredicto en una línea

**Finalista con puerta.** La puerta es una semana de banco, sin sello y sin personas (§4), que
decide si la máquina de la dirección (mutar → validar → firmar → ordenar) produce una escalera o
copias, y si el sustrato tiene relojes a escala de día. Todo lo que pide está en el catálogo con
veredicto «sí» salvo una ley («el hogar come»), y esa ley, leída contra el código, hay que
reescribirla (§3.f). Es la dirección del panel que menos pide del sustrato y más del banco, y la de
menor arrepentimiento: si muere, el 80 % de lo construido es la capa J que cualquier otra dirección
necesita. Lo que esta lente no puede juzgar (si soltar y leer un recibo es jugar o rendir un examen)
sigue siendo el riesgo mayor, y su prueba humana llega con ruido a las 3 semanas y limpia a las 7.

## 1. Qué pide del sustrato, pieza por pieza

| pieza | catálogo | verificable en banco | dependencia secuencial | hecho del código que la afecta |
|---|---|---|---|---|
| **J entero**: diario bajo las cinco puertas + sliders, volcado/carga, `CorrerSello`, condición como dato, días sin manos, balanza `entregable`, `LabBandas`, anillo de hashes, hash de versión | sí (sello 7,5/8; balanza 6/8, corregidos) | sí, entera: round-trip de r141 (volcar en 4 500, cargar, correr 4 500 → siete hashes), prueba (d) del refutador con cincel y frasco en vivo | **columna vertebral**: diario → volcado → `CorrerSello` → condición → validador → firma → escalera | Las cinco puertas ya llaman a `ReenviarSiEspejo` (:574, :613, :744, :780) salvo `PaintLab` (:831) e `InyectarTemperatura` (:815): el diario es una línea por puerta. PERO el aplicador de `SimSync` (:1122-1128) es **deliberadamente infiel**: rejuega `PaintCell` como `PaintStable` y `PaintRect` a `AmbientRaw`. `CorrerSello` necesita aplicador propio con los parámetros exactos (tempRaw, humedad, carga) y con el tick colocado antes del `_tick++` (refutación del sello, error 3). Además las puertas son métodos de un MonoBehaviour; el banco monta con `CellGrid` + `SimStepper`: hace falta un Pintor común en `Sim/` |
| Cuna mínima: envejecer, `Clonar`, validador honesto, mutación, cielo por geometría | sí, versión ajustada (5,5/5,5) | sí | detrás de `CorrerSello` (envejecer y clonar no; validador sí) | Cielo por geometría relinea `HashLuz` en 6 de 9 montajes (la fila 286 está abierta en horno, carbonera, tolva, hervidero, diluvio, mundo despierto); inocuo para `mat`: `luz[]` solo la leen germinación (:704) y crecimiento (:870-873), y `EsSustrato` es sedimento, arena y ceniza (`LabMateriales` :104), que en esos montajes está seco o encerrado |
| Firma y escalera (`LeyesActivas`, ablación, greedy, recibo cuantizado) | nuevo; es banco | sí | detrás del validador | **La medida no necesita el bitmask**: ~15 ejes de `LabParams` a cero ablan erosión, decantación, infiltración, percolación, capilaridad, secado, evaporación, condensación, compactación, colmatación, germinación, carbón, caudal, hogar y frío sin una línea. Lo que NO se puede ablar por parámetro: la combustión (`ProcessCombustion`), el barrido (caer, fluir) y la luz como tal. De los cinco interruptores que cita la dirección: `CuerposActivos` gatea un gancho VACÍO (:1290-1296) = cero información; `TermicaPropia = 0` SUSTITUYE por `DiffuseTemperature` (:269), no quita la térmica; `LuzCadaTicks` tiene tope 64 en el registro (:289) y con `_tick % cada == 0` un valor «enorme» deja `luz[] = 0` para siempre, o sea abla luz Y plantas a la vez. Quedan tres bits reales (presión, germinación, luz∪plantas): ocho firmas posibles, colapso por construcción |
| Solver de macroverbos | nuevo; es banco | sí | detrás de la mutación | `LabParams` es estático y `LuzCieloX0/X1` también: un mundo por proceso. K = 32-64 registros × H ticks a 1,5-3 ms/tick = 15-90 min por mutante. Paralelismo = N procesos de una build batch-mode (el banco «corre en una build», docblock :23), no hilos |
| Vistas: bandas, Ojo, Piel, Edad, scrub, diff | sí (Ojo y bandas como vistas) | presentación | scrub y diff cuelgan del volcado | El fuego ya escribe `luz = 255` (`EmiteLuz` :110: fuego, brasa, hogar) y nadie lo pinta. **`touchedTick` no es edad**: es la guarda de reentrada por tick (`SimStepper.cs` :352-353), se escribe en toda celda procesada de un chunk despierto. Muestra chunks dormidos; un arroyo en régimen lee «recién tocado» para siempre. La edad honesta es el diff entre volcados |
| Luz mínima: día de Q16 con `luz[i+W]`, vidrio que transmite y suda | sí (8,5) | sí | ninguna | R150 ya midió con boca ancha: nacen ×4,5 y mueren las 9 por humedad de raíz (50-99 contra 60). «Planta viva día 30» **no tiene hoy solución de referencia**: es geometría de riego, autoría |
| **«El hogar come»** | **sin refutar** (nace del crítico de huecos; paquete D en `01` §4 con kill propio) | sí, pero exige escenario propio | ninguna | Ver §3.f: ocho de nueve montajes llevan `Hogar`, y la fibra prende a 130 raw < 170 del hogar |
| Velocidad como estado | presentación | — | ninguna | `LabMultiplicador` existe (`AlkahestSim` :372-400) con presupuesto de ms; a 3 ms/tick el presupuesto de 20 ms da ×6-7 real, ×10 en los montajes de 1,5-2 ms. «×1 mientras la mano actúa» es una línea en `LabPanel` |

Nada de lo que pide está en el cajón «refutado». Lo único que toca física es el hogar que come.

## 2. Lo que el laboratorio sostiene y lo que contradice

**Sostiene.** (1) `Correr` ya es un `CorrerSello` con una sola intervención tick-estampada, cableada
por nombre (`esAlambique`, :295 y :306-311): generalizarla es un día. (2) Determinismo: nueve
escenarios con siete hashes cada uno, cero `float` en el partial, RNG sin estado; el sello es la
pieza más barata y verificable del panel y los dos refutadores coinciden. (3) Cuatro contrastes con
juez ya medidos: recinto contra hogar en el vidrio (18/18 contra 0, R135), carbonera con boca 1
contra abierta (B-F3), hervidero con barra fría cruzando el tiro contra sin ella (R145/R148),
alambique con caldera (902 goteos, banco 2026-09-06). (4) El banco corre a 500-660 ticks/s: 30 días
(54 000 ticks) son 85-110 s headless; la película por día se produce, no se espera. (5) `ArcoMuestra`
ya es una fila del recibo por día. (6) El libro tiene ~25 contadores `long` y la condición como dato
los lee sin tocar la física.

**Contradice.** (1) **El «vacío» del alambique no está medido**: el banco solo tiene el alambique
CON caldera (902); `Goteos` vale −1 en los demás y el nivel sin caldera pero con serpentín no está
en ninguna tabla. La primera «apuesta con juez» es plausible, no medida. (2) El huerto nunca vivió:
R148 (2 nacidas, 0 vivas a 30 min) y R150 (9 nacidas, 9 muertas): el peldaño de vida no tiene madre
con solución. (3) El fuego no tiene nada que perder: hogar eterno, fibra que se consume en 5 min de
mundo (R148: «el fuego de la sala se consume en los primeros cinco minutos»), tolva 466 s = 7,8 días
de juego. (4) Los campos son invisibles fuera de F8 y las plantas miden un píxel: un FALLA es hoy un
FALLA sin causa, y eso contamina la prueba humana. (5) **Difusión y campos recorren la grilla
entera** (`LabDifusionTermica` :1316 y `LabCampos` :208: `for (i = offset; i < n; i += 8)`), no los
chunks despiertos: el banco lo confirma, 0,61-0,71 ms de difusión en los nueve escenarios sea cual
sea su tamaño. Piso ~0,8 ms/tick con el mundo dormido. Una situación de 96×64 cuesta lo mismo que el
nivel: «los chunks dormidos hacen que el resto no cueste» vale para el barrido y no para la mitad
del tick. No cambia el juego; cambia la aritmética del lote. (6) Los presets del `LabPanel` son
`LabParams` en JSON (`Guardar` :92-101 escribe el `Registro`), sin montaje: la Etapa 2 «cinco
situaciones como presets» necesita un selector que monte `LabBench.MontarX` en la grilla viva, un
día que no está contado. (7) Relojes a escala de día sin medir: la inundación (una sala de 128×72 a
~20 celdas/s netas se llena en unos 8 días) y el combustible tienen escala; la colmatación de la
grava y el relleno de la poza no la tienen en ninguna tabla. El «hacia el día 8» del minuto 7-10 es
narración.

## 3. Correcciones técnicas, una por una

**a. La Etapa 1 no puede fallar.** «Registro vacío contra intervención de referencia sobre los nueve
montajes» presupone que los montajes son madres. No lo son: llevan la solución dentro. El horno ES
el recinto (18/18 sin nadie), la carbonera ES la boca de 1, el hervidero ES la barra fría, la tolva
arde sola, el diluvio decanta solo; el laboratorio base y el arco largo fallan «planta viva» con o
sin intervención (R148, R150); el mundo despierto no es situación. Con el `Correr` actual discrimina
como mucho uno (alambique), y su vacío no está medido. En cuanto se parte cada montaje en madre y
registro, quien escribe las dos mitades garantiza que discriminen: «5 de 9» pasa siempre. Es una
decisión de autoría disfrazada de umbral. La ablación con los cinco interruptores citados tiene
tres bits reales (§1): el colapso de firmas está garantizado por construcción y no dice nada de la
física. Hay que reescribirla (§4).

**b. La firma se mide hoy, por parámetros.** Quince ejes a cero ya ablan la mitad de las leyes sin
código. El bitmask limpio es una optimización y una limpieza (y es invasivo: evaporación,
condensación, turbidez, infiltración, capilaridad, compactación y cocción viven dentro de
`LabAgua`/`LabPoroso`, y la combustión en `SimStepper.cs`), no un prerrequisito de la medida.

**c. Hay que restar la base.** Toda situación con manantial firma agua, caudal, infiltración: la
firma-lección es firma(situación) menos la intersección del peldaño 0, o la escalera tiene dos
peldaños. Con doce familias y la base restada quedan 7-11 bits de lección: la escalera son 7-11
peldaños, no «cien situaciones en una noche».

**d. El coste del lote es de reloj y de procesos.** |L| × H por situación va en serie dentro de un
proceso (`LabParams` estático). Con L = 12, H = 18 000 y 1,5-2 ms/tick: 5-7 min por firma, 25 min
con horizonte de 30 días; cien situaciones = 10-40 h en un hilo. Hace falta la build batch-mode y un
runner de N procesos (medio día) antes de decir «una noche».

**e. La vista Edad no es edad.** `touchedTick` es la guarda de reentrada. Renombrarla (mapa de
chunks dormidos) o sustituirla por el diff de volcados, que la dirección ya tiene.

**f. «El hogar come» contradice dos números medidos y ocho montajes.** (i) Ocho de los nueve
montajes del banco llevan `Hogar` (todos salvo el diluvio): un hogar que se apaga sin combustible
mata el hervidero (200 hogares bajo agua, sin fibra) y el «mundo entero despierto» (864 hogares
eternos: el peor caso del planificador, que existe porque el hogar se despierta a sí mismo, R55).
Debe ser un **material nuevo** o un flag con cero por defecto, con escenario y kill propios; así
relinea un hash, no ocho. (ii) `Fibra.ignitionTemp = CToRaw(140)` = 130 raw y `Carbon` = 200 raw;
`LabHogar` calienta a sus vecinos hasta 170 (`LabCalentarHasta` :942-945) y la autoignición de
`ApplyPhase` (:685-698) prende cualquier inflamable seco por encima de su umbral. **La fibra que toca
el hogar es una hoguera antes de que nadie la pueda «comer»**, y la llama sobre combustible es
inmortal (no hay tiro). El carbón, a 200, no prende a 170: es el único combustible que un hogar
puede consumir sin convertirlo en llama. La ley honesta es «el hogar come carbón» (consume el carbón
que lo toca, guarda reserva en `aux`, decae hacia el ambiente sin ella), y cruza carbonera → carbón
→ hogar, que es un cruce mejor que el de la tolva. Con eso, 0,5 semanas siguen valiendo, pero la
situación «mantén el hogar vivo 30 días» pide carbón, no fibra, y la tolva de 466 s no es su
máquina: es una hoguera de 7,8 días.

**g. «Toque» no está definido.** `Cincel.TallarTick` escribe decenas de celdas por gesto en un bucle
de `budget`; el frasco vierte por celda. Si el diario cuenta entradas, «1 toque» del minuto 2 son
40. Id de gesto (pulsar → soltar) en la entrada: diez líneas, pero sin ellas la columna «toques»
mide píxeles y el histograma de Zachtronics no existe.

**h. Determinismo entre máquinas: no probado.** Los 63 hashes prueban una máquina y una build.
Relevo, bifurcación e histograma dependen de que el fichero de A rejugado en B dé los mismos hashes.
`Universe.Create` usa `Mathf` (IEEE sin trascendentes: riesgo bajo). Un día: `LabBench` en la build
IL2CPP contra los 63 hashes del editor. Si divergen, «asíncrono por fichero» es película, no
rejugado, y hay que saberlo antes del sello.

## 4. La prueba más barata que la mata

**Semana 1, banco, sin motor nuevo, sin personas (5-7 días de Opus).**

(a) `Intervencion[]` genérica en `Correr` (tick, rect o disco, material, tempRaw, humedad, carga); la
caldera pasa a ser la primera entrada; hashes de los nueve intactos (un día).
(b) Cuatro madres partidas de los montajes medidos, como «montaje − solución» con rotura de una
línea: alambique sin caldera (solución: caldera), horno sin recinto (solución: recinto), carbonera
abierta (solución: tapar a boca 1), hervidero sin barra (solución: barra). Condición sobre libro o
grilla: goteos ≥ 500, vidrio ≥ 18, carbón ≥ 0,8 × plateau, condensado ≥ f × vacío (un día).
(c) **Sonda por día de la condición** a 54 000 ticks para vacío y para autor: el día en que cada uno
se cae (medio día).
(d) Ocho mutantes por madre con tres operadores (boca ±k, fuente u hogar movidos ±x, grosor de pared
±1); solución del autor rejugada sobre cada mutante; recibo cuantizado a 8 tramos por métrica (un
día).
(e) Ablación por **parámetros a cero** sobre 12 ejes, madres y mutantes válidos, H = 9 000-18 000,
firma = ejes cuya anulación cambia el veredicto, **menos la intersección** de las cuatro madres (un
día de código; 3-4 h de banco en 2-4 procesos batch-mode).
(f) Medio día aparte: `LabBench` en IL2CPP contra los 63 hashes.

**La mata** (umbral escrito antes de correr): menos de 3 firmas-lección distintas entre las cuatro
madres (la escalera no existe); o tres o más de las cuatro madres son binarias en el día 1 (el
autor nunca se cae en 30 días y el vacío se cae antes del día 2: el sustrato no tiene relojes a
escala de día y «días sin manos» no mide nada); o más del 80 % de los mutantes válidos repiten el
recibo cuantizado de su madre (la máquina fabrica copias); o menos del 30 % de los mutantes
sobreviven a la solución del autor (la máquina no fabrica nada y la campaña vuelve a ser
biblioteca). Si vive: ≥ 3 firmas, ≥ 1 madre con reloj entre el día 3 y el 25, 30-80 % de mutantes
válidos, ≥ 2 familias de recibo por madre.

**Semana 3, personas (Etapa 2 corregida, 3-4 días de preparación).** Selector de montaje del banco
en la grilla viva (un día), contador de días en pantalla y una línea del libro por día con el día
del FALLA (un día), ×1/×10 por estado de la mano (una línea). Tres personas, cinco madres, una con
reloj. Se mide: reintento espontáneo tras FALLA; soltar antes del horizonte por decisión propia;
cuántas veces alguien toca a mitad de corrida frente a cuántas reinicia; si alguien dice por qué
falló leyendo solo la línea del día. Mata: menos de dos de tres reintentan, nadie suelta, cero toques
a mitad, o nadie explica el FALLA con la línea. Salvedad honesta: sin bandas ni película un FALLA es
un FALLA sin causa; la versión limpia es la del prototipo feo (semana 7).

## 5. Tiempos corregidos

**Paralelizable, técnico, en banco (Opus, carril 2):** semana de banco de §4 (1) · balanza
`entregable` con guarda de `PaintLab` (1-1,5) · `LabBandas` como fuente única (0,5) · bandas en
pantalla + Ojo + Piel + línea por día (1-1,5) · día de Q16 + vidrio que transmite y suda (0,7) · hogar
que come carbón como material nuevo con escenario y kill (0,5) · build batch-mode + runner de N
procesos (0,5) · selector de montaje + velocidad como estado (0,5) · IL2CPP contra 63 hashes (0,1).
Unas 6-7 semanas de trabajo, en paralelo con la columna.

**Secuencial (camino crítico):** Pintor común en `Sim/` (0,3) → diario bajo las cinco puertas + id de
gesto + entrada de parámetro (0,5) → volcado/carga con la lista completa de estado (`_tick`, libro,
`_labManantialCeldas`, `_labPase`, preset, `LuzCieloX0/X1`, `chunkTouchedTick` con su
`uint.MaxValue` inicial, `_zonaInteresChunk`) + round-trip de r141 (1) → `CorrerSello` con aplicador
propio + condición como dato + días sin manos (1) → validador + envejecer + `Clonar` (1) → mutación
(0,5) → bitmask limpio + firma (1-1,5; la medida ya existe por parámetros) → escalera + recibo
cuantizado (0,5) → solver (1). **7,5-8,5 semanas en serie.** Scrub y diff (1) cuelgan del volcado y
entran a partir de la semana 3. La dirección dice 11-13 en dos carriles: coincide en esfuerzo; el
camino crítico real son 8, y cada cambio de física posterior re-corre el lote entero (automático).

**Iteración humana:** lectura del banco de la semana 1 (media jornada de Cesar y Fable); sesión
ruidosa (semana 3); sesión limpia con bandas y película (semana 7); dos más en el mes siguiente;
después una por lote de 20-30 situaciones. Cuatro o cinco sesiones antes de saber si hay «otra vez».

- **Hasta evidencia para matarla: 3 semanas** (1 de banco que mata la máquina y el reloj; 2 más hasta
  la sesión ruidosa que mata el examen con ruido). Limpia: 7.
- **Hasta prototipo feo que permita juzgar el core: 7 semanas** (sello mínimo con condición y
  contador, 4 en serie; bandas, línea por día y película, 1,5 solapadas; ocho madres partidas de los
  montajes con firma medida por parámetros, 1; holgura del ciclo compilar-desplegar de Cesar).

## 6. Iteración humana oculta (lo que el banco no decide)

(1) **Qué enseña cada madre.** El validador garantiza que discrimina y que el solver la resuelve, no
que una persona vea la ley; la firma mide de qué ley depende el veredicto, no qué ley hay que
entender. Cesar juega cada madre una vez: 1-2 h por madre, 2-4 días por lote. (2) El peldaño de vida
no existe hasta que alguien diseñe el riego que R150 dejó como geometría. (3) Horizonte por madre y
métrica de la condición: 12-20 números humanos si no se derivan del reloj del banco (§7). (4) La
granularidad del bitmask (¿`LabPoroso` entero o infiltración, capilaridad, compactación y cocción por
separado?) decide cuántos peldaños tiene la escalera: se mide en lote, se elige a mano. (5) La tasa
del hogar que come: un número que decide si 30 días son una carbonera o un silo imposible. (6) La
presentación (condición sin texto, recibo por día, película, diff): 3-5 sesiones de láminas,
incomprimibles. Nota: 7, y vale solo si se acepta una campaña acotada por el solver o se paga
autoría por familia.

## 7. Qué se automatiza en su lugar

Validez (vacío falla / autor cumple), resolubilidad acotada por el solver, fragilidad (cortes de una
celda), **reloj** (día de caída del autor a 3×H; horizonte = f × ese día; sin caída, la madre va al
sandbox como lámina), firma por ablación (hoy por parámetros, mañana por bitmask) con base restada,
orden greedy por inclusión, hermana de contraste como el mutante cuya firma difiere en un bit,
regresión por hash de versión de física, comparación por recibo por día, detección de copias por
recibo cuantizado, envejecimiento y mutación, «el día en que se torció» como primer día en que baja
el vector de la condición, y el lote nocturno en N procesos batch-mode. Todo headless, todo con
umbral escrito antes de correr.

## 8. Órganos a conservar si se descarta

`Intervencion[]` genérica en `Correr` y «montaje − solución» como forma canónica de todo escenario
del banco · sonda por día de una condición como dato · sello con condición y días sin manos como
hecho · Pintor común en `Sim/` con aplicador fiel · id de gesto en el diario · validador honesto ·
validador de reloj · firma por ablación con parámetros a cero y base restada · runner de N procesos
batch-mode · hash de versión de física en el fichero · velocidad como estado · el hogar que come
carbón como material nuevo con su banco · la prueba IL2CPP contra los 63 hashes.

## 9. Puntuaciones (rúbrica v2, 1-10)

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | infraestructura de juicio; no crea cruces, multiplica los que hay; la única física nueva es el hogar que come, y reescrita a carbón |
| leyes ejecutan, revelan y juzgan | 8 | juzgan de verdad (condición sobre libro y grilla, balanza, días sin manos); revelan después (película, diff); la lección sigue siendo humana |
| iteración humana (10 = poca) | 7 | cuatro o cinco sesiones y muestreo; el pasa-bajos del validador devuelve autoría por familia |
| verificabilidad automatizable | 9 | todo en banco con umbral escrito antes; su mejor eje |
| la simulación es el juego | 7 | el marco es delgado; la sim evalúa problemas que escribe un autor; el riesgo es que condición y recibo pesen más que la física |
| onboarding garantizable | 8 | la escalera por firma es un mecanismo real (7-11 peldaños); el peldaño de vida no tiene madre viva |
| observabilidad | 6 | bandas, Ojo, Piel, película: todo del carril paralelo; hoy los campos son invisibles y Edad no es edad |
| tiempo como apuesta | 6 | tocar pone el contador a cero: apuesta con reloj; pero los relojes de día no están medidos, reiniciar gratis domina a tocar y el fuego solo apuesta con el hogar que come carbón |
| multiplayer emergente | 5 | relevo y bifurcación por fichero, tras el sello y tras IL2CPP; nada simultáneo |
| profundidad por leyes estables | 6 | 7-11 peldaños y 30-80 situaciones reales con doce familias; crece solo cuando entra una ley (A, V, F), que la dirección pospone |
| cuerpo del jugador | 4 | sensor y registro; en una cámara de 96×64 con alcance de 60 celdas no hay geografía que sentir |
| identidad comercial | 6 | «DÍA N SIN MANOS» es nombrable y el clip es el fracaso con la grava ennegreciendo; la cápsula es tierra y agua |
| dificultad técnica (10 = fácil) | 8 | C# puro, banco, fuerza bruta en procesos; lo delicado es el estado escondido del volcado, el aplicador fiel y el bitmask invasivo |

Puertas: iteración 7 y apalancamiento 6, las dos por encima de 5. Pasa.

## 10. Crítica razonada

Esta dirección es, de las ocho, la que menos pide del sustrato y la que más pide del banco, y eso
es exactamente lo que la función objetivo premia. Todo lo que necesita (el sello con condición como
dato, la balanza, la cuna en su mitad barata, las bandas, las vistas) está en el catálogo con
veredicto «sí» y con los números corregidos por los dos refutadores. Solo toca la física en una
ley, «el hogar come», que nació del crítico de huecos y nunca pasó por refutación, y leída contra el
código no puede entrar como está: ocho de los nueve montajes del banco llevan `Hogar`, y un hogar
que se apaga sin combustible mata el hervidero y el «mundo entero despierto», el peor caso del
planificador. Peor: la fibra prende a 130 raw y el hogar calienta a 170, así que la fibra que toca un
hogar es una hoguera antes de que nadie la pueda comer, y la llama sobre combustible es inmortal. El
carbón, a 200, es el único combustible que un hogar puede consumir sin volverlo llama. La ley
honesta es «el hogar come carbón», material nuevo con su escenario y su kill; cruza carbonera →
carbón → hogar, y la tolva de 466 s deja de ser su máquina.

El laboratorio sostiene la mitad de la tesis con medidas hechas: el recinto contra el hogar (18
contra 0 vidrios), la carbonera con boca de 1 contra abierta, el hervidero con y sin barra fría, el
alambique con caldera. Son contrastes con juez que existen sin saberlo, y `Correr` ya es un
`CorrerSello` con una intervención cableada. Lo que contradice es igual de concreto: el vacío del
alambique no está en ninguna tabla; el huerto nunca vivió (R148, R150), así que el peldaño de vida
no tiene madre con solución; el fuego no tiene nada que perder; los campos son invisibles fuera de
F8; y los relojes a escala de día (colmatación, poza que se llena) no están medidos, con lo que el
«hacia el día 8» de la narración es narración.

Cinco hechos del código corrigen el plan. Los presets del panel son parámetros, no montajes: la
Etapa 2 necesita un selector que monte en la grilla viva. La ablación que la dirección pospone al
bitmask existe hoy: quince ejes de `LabParams` a cero anulan la mitad de las leyes sin código,
mientras que dos de los cinco interruptores que cita son inservibles (`CuerposActivos` gatea un
gancho vacío; `TermicaPropia = 0` sustituye la térmica) y el tercero confunde luz con plantas.
Difusión y campos recorren la grilla entera con el mundo dormido (0,61-0,71 ms en los nueve
escenarios): una situación pequeña no es más barata que el nivel, y como `LabParams` es estático el
lote paraleliza por procesos, no por hilos. `touchedTick` es la guarda de reentrada por tick, no una
edad. Y el aplicador de `SimSync` es deliberadamente infiel (`PaintCell` rejugado como
`PaintStable`): el sello necesita aplicador propio o el rejugado no es bit-idéntico.

La Etapa 1 tal como está escrita no mide: se predice leyendo. Los nueve montajes llevan la solución
dentro, así que el vacío cumple por construcción o hay que partirlos en madre y registro, y entonces
quien escribe las dos mitades garantiza que discriminen. Propongo sustituirla por una semana de
banco que ataque lo único que distingue esta dirección de Un Año Después: la máquina y el reloj.
Intervención genérica en `Correr`, cuatro madres partidas de los montajes medidos, sonda por día de
la condición a 30 días para vacío y autor, ocho mutantes por madre con tres operadores y la solución
del autor rejugada, recibos cuantizados y firma por parámetros a cero con la base restada. La mata
que haya menos de tres firmas-lección, que tres de las cuatro madres sean binarias en el día 1, que
más del 80 % de los mutantes válidos repitan el recibo de su madre o que menos del 30 % sobrevivan a
la solución del autor. Cinco a siete días de Opus, cero de Cesar, y decide antes de escribir el
sello si la escalera existe, si la mutación produce puzles o copias y si hay algo que contar en
días.

El riesgo estructural que la dirección no nombra es que su validador es un filtro pasa-bajos: un
mutante entra en la campaña si la solución del autor sigue valiendo (mismo puzle) o si un solver de
seis verbos lo resuelve (puzle fácil); los que exigen una idea nueva van al sandbox. La escalera
queda acotada por la imaginación del solver y la salida es más autoría por familia. Las «100-300
situaciones» son 7-11 peldaños por sus variantes de práctica, 30-80 situaciones reales. No es fatal,
pero el número que hay que vender es el de peldaños, y la profundidad crece solo cuando entra una
ley, que la dirección pospone.

Tiempos: el camino crítico real son ocho semanas en serie (Pintor común, diario con id de gesto,
volcado con round-trip, `CorrerSello`, validador, mutación, bitmask, escalera, solver); el carril
paralelo son seis o siete. Evidencia para matarla: una semana de banco para la máquina y el reloj,
tres hasta la sesión ruidosa que mata el examen con ruido, siete limpia. Prototipo feo: siete.
Iteración humana: cuatro o cinco sesiones, más la decisión de qué enseña cada madre, que ningún
banco toma.

Finalista con puerta, por una razón de producción que esta lente sí puede defender: aunque muera en
la sesión de la semana tres, el sello, el validador, la firma, el Pintor común, el runner y el
validador de reloj valen para cualquier otra dirección, y su prueba de máquina cuesta una semana sin
personas. Es la dirección de menor arrepentimiento del panel. Lo que no decide ninguna ley, si
soltar y leer un recibo es jugar, lo decide la sesión siete, con bandas y película delante.
