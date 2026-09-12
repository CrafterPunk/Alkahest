# CRÍTICA · PRIMERA PIEDRA (cuerpo) · lente INGENIERÍA Y EVIDENCIA

*(Director técnico, segunda pasada, 2026-09-12; sustituye a la corrida de las 13:22 e integra lo que
aquella verificó. Leído entero: `cuerpo.md`, `01_LEYES.md`, `panel/leyes/cuerpo.md` (C1-C4) y sus ocho
refutaciones, `_huecos_y_combinaciones.md`, CHECKPOINT §6g (HF1). Código a R151: `SimStepper.cs`
(`Step` :257-338, `ProcessPowder` :1061-1135, `ProcessLiquid` :1138-1240, `ProcessGas` :1443-1615,
`TryGasLateral` :1607-1616, `ProcessBrasa` :1004-1057, `Move` :738), `SimStepper.Laboratorio.cs`
(`LabCampos` :201-215, `LabVecinoVacio` :298-312, `LabSecarHacia` :734, `LabRespira` :1052-1061,
`LabPresion` :1080-1175, `LabLuz`/`LabLuzDesde`/`LabLuzDecay` :1177-1285), `CellGrid.cs` (`SleepTicks`
30 :68, `WakeChunk` 3×3 :305), `AlkahestSim.cs` (bucle ×10 :372-383, `PaintCell` :610), `Flask.cs`
(`ReachWorld 6f` :107, `Capacity 900` :91, `TickPour`/`PaintCell` :633-690), `ApprenticeController.cs`
(`SoloPies` :643, caja 6,4×11,2 :796-798, `CajaChoca` :905-924, velocidades :672-674, a pie sin bob
:1041), `LabParams.cs` (`DesnivelMin 3`, `ReposoMovil 3`, `PlantaLuzMin 40`, `FibraMojadaMin 100`),
`SimLevelBuilder.Laboratorio.cs` (plano :13-28, boca del cielo x118-124 y273-286, `LuzCielo` :164),
`LabBench.cs` (`MontarCarbonera` boca 1 :135-143, `Correr` :265-335).)*

## 0. Veredicto

**SEGUNDA RONDA, como capa injertable; con el orden de pruebas invertido.** Es la dirección más
barata del panel de construir y de matar, y por eso mismo no admite que su evidencia siga sin
producirse: **las dos pruebas que propone no pueden matarla** (una tiene línea base nula, la otra
apunta a un objetivo trivial), y lo que sí puede matarla cuesta un día sin código y dos días de banco
sin arquitectura. No es finalista porque, por su propia confesión (§10), es «campaña + una ley»; porque
la pieza se agota (un prototipo por ley); y porque el valor de «ser la pieza» frente a «poner la
piedra» no está medido. No se descarta porque sus órganos (posición como entrada del tick, la
física del toque, brasa viva, Ojo, BFS a pie, «con cuerpo» por la física) son los más reutilizables del
panel y `_huecos` los señaló como lo que falta en TODAS las direcciones.

## 1. Qué pide del sustrato y qué dice el código

| pieza | catálogo | acotada y verificable en banco | dependencia secuencial |
|---|---|---|---|
| máscara «el cuerpo ocupa» donde las pasadas preguntan si una celda está libre | el «tapón» que la refutación del humo dejó fuera: no refutado, no evaluado | sí, con corrección de la prueba (§3) | posición como entrada del tick (2-3 días) antes de todo |
| `CuerpoSim {Calor, Mojado}`, halo 12, tinte, tizne | C (viable con ajuste ×2) | sí: «cuerpo en el alambique» de la refutación C1, más el **día de sonda** de `01` §3 que nadie ha corrido | ninguna |
| brasa viva en el frasco | C2 | sí: `Ignite > 0` en 9 seeds | ninguna |
| a pie · 12 celdas · polvo en reposo es suelo | control, fuera del sustrato a propósito (`01` §6.2) | NO: es sensación | ninguna; existe hoy (`SoloPies` :643; `ReachWorld` es una constante) |
| vista Ojo | L | sí | ninguna |
| BFS a pie + K perturbaciones del guion | extensión de J·cuna | sí | `CorrerSello` (J) o, sin J, `Correr(montaje, guion)` |
| «DÍA N SIN CUERPO» | J·sello | sí | J; sin J, un contador en el stepper y en `Informe` |

### 1.1 «26 puntos de `Empty`» son cuatro familias de predicados, y la caja sigue siendo aire

Las celdas de la caja **nunca se reescriben**: siguen siendo `Empty` en `mat[]`. Toda pasada que
pregunte «¿esto es aire?» sin consultar la máscara trata al cuerpo como aire. Y no preguntan de una
sola forma:

1. Identidad `mat[n] == Empty`: gas (:1533, :1547, :1588, `TryGasLateral` :1614), chispa que sube
   (:1723), vapor que nace (:951-964), fuego que salta arriba (:882), `LabRespira` :1056-1059,
   `LabLuzDesde` :1272, `LabVecinoVacio` :303-312, `LabPlanta` :703/:870-873, `LabSecarHacia` :736.
2. Arquetipo `archetype == Empty || Gas` (y `Liquid` menos denso): polvo :1080/:1105/:1127; líquido
   :1187/:1211 y `TryFlow`; :1831. **Consecuencia:** si en la caja hay humo (gas), el polvo y el
   líquido pueden entrar desplazándolo aunque la identidad diga «no libre».
3. `LabPresion`: la superficie es «agua con `Empty` encima» (:1119, :1136, :1168) y la mudanza es un
   `SetCell(target, Water)` (:1155) que **teletransporta agua al interior de la caja** si el cuerpo
   está sobre una superficie. Además el BFS (:1112-1121) recorre celdas de agua sin mirar máscara.
4. `LabCampos` (:215) ejecuta `LabAire` en cada `Empty`: la caja evapora, condensa y seca como aire.

Son 35-40 sitios con cuatro semánticas, no 26 con una. Dos implementaciones posibles:

- **Por sitio** (`Libre(idx)`, `LibreDef(idx)`): 35-40 ediciones en un núcleo congelado desde R141;
  conserva el «prueba la siguiente opción» de cada pasada. 1 semana más caza de fugas.
- **Cuello de botella en `Move()` (:738) + ocho sitios fuera de `Move`** (`LabPresion` ×4,
  `LabLuzDesde`, `LabRespira`, `LabVecinoVacio`, salto de `LabAire` en `LabCampos`, nacimientos
  :882/:951): 3 días. Precio: la celda bloqueada pierde su tick sin probar la siguiente dirección
  (el humo bajo el vientre no prueba el lateral). Para una tapa es lo que se quiere; para una presa
  el agua que choca de lado se queda quieta y se apila igual. Vale para la prueba de muerte; el
  refinamiento por sitio solo si el banco enseña artefactos.

Estructura correcta en ambos casos: `byte[] ocupa` W×H (221 KB), reescrito por la pasada del cuerpo
(borrar caja vieja, pintar la nueva: ~150 escrituras por movimiento), una carga más por
comprobación, sin división ni AABB en el bucle caliente. `ocupa` es entrada, no estado: se hashea
`(X0, Y0)`, no el array; con `X0 < 0` los 63 hashes son idénticos por construcción. **Es un invariante
transversal como R33:** cada ley futura que pregunte «¿es aire?» (el byte `aire` de A, el arrastre de
V, el haz de L) añade un sitio y una fuga posible. La equivalencia cuerpo≡piedra no se prueba una
vez: entra al anillo del banco como los hashes.

Sueño de chunks: el humo sellado bajo el cuerpo descuenta `aux` sin `WakeChunk`; a los 30 ticks
(:68) el chunk duerme y el bolsillo se congela. Hay que despertar la caja **al entrar y al salir**
(`WakeChunkIndex` solo sobre los chunks de la caja, no `WakeChunk` 3×3 cada tick: cuatro cuerpos ×
hasta 4 chunks × 9 serían 36 chunks despiertos, +0,1-0,3 ms sobre 1,6-1,9). El «≤ 5 %» es justo, y
se mide sobre «mundo entero despierto», no sobre el laboratorio.

### 1.2 La prueba «tapa humana» tiene línea base nula

`MontarCarbonera` es **boca 1** (:142). HF1 (CHECKPOINT :482): boca 1 → 100 % carbón, 0 ceniza, 0
llama, **0 humo**. Cuerpo sobre la boca 1, roca suelta en la boca 1 o **nada en la boca 1** dan lo
mismo. El criterio «carbón con cuerpo ≥ 90 % del carbón con piedra» no puede fallar y no distingue
el cuerpo de la ausencia de cuerpo. Mide cobertura de sitios, no si la pieza importa. La corrección:
**boca 4** (19 % ceniza abierta, la única curva con pendiente medida), y como el cuerpo mide 6,4
celdas y cubre 4 con 1,2 de margen por lado, la ondulación diagonal del humo desde (104,223) hacia
(105,224) solo queda cubierta si el centro está a ±0,5 celdas: la aceptación real es un **barrido de
desfase en x (−2..+2)** con el carbón por desfase. «Tapa robusta a ±1 celda» o el jugador a pie no
podrá hacer lo que hace el guion; si hace falta margen en la máscara, ese margen es el único número
de la pieza.

La condición de ejemplo del minuto 1-3 («carbón ≥ 30 · planta viva») tampoco discrimina: con 400
celdas de fibra y boca 3 abierta salen cientos de carbón sin nadie. El validador por veredicto de J
la rechazaría («el registro vacío debe fallar»): la autoría de situaciones no es gratis y J es
dependencia dura para que la campaña sea válida, no solo para el contador.

### 1.3 La pieza bloquea la ENTRADA, no la PRESENCIA

Como la caja no se reescribe, lo que había dentro se queda y solo puede salir a celdas libres. En un
canal con caudal, la caja se vacía aguas abajo en 10-20 ticks y el cuerpo es presa: **la presa
funciona, medida tras el transitorio**. En un tubo en U **lleno** no hay celda libre: el agua de la
caja no sale nunca, el BFS de `LabPresion` la atraviesa y **el cuerpo no es corcho**. «Corcho» exige
que el tubo tenga aire del lado del cuerpo; es cámara de autor.

### 1.4 Geometría de «sombra» y de «a pie» en el nivel real

La boca del cielo es un pozo de 7×14 sobre el techo de la cámara alta (x118-124, y273-286). Tapar
el sol con la espalda exige estar DENTRO del pozo, sin suelo: inalcanzable a pie salvo vertiendo
24+ celdas de polvo desde el lecho (246-249; salto 22). La sombra es real por aritmética (7 columnas
justas, `LabLuz` rodea obstáculos más estrechos que la boca) y se mide en banco con un bloque
de piedra en el pozo leyendo `luz[i+W]` sobre el lecho, la métrica de `01` §3; pero como situación es
de vuelo o de geología de autor.

La prueba humana de la dirección («llegar a la boca de la carbonera en tres minutos») apunta al
objetivo trivial: la carbonera del arco largo (x100-121, y176-199) está a 20 celdas del spawn
(118,182) en suelo llano. El objetivo que discrimina es la **cámara alta** (y245-272) por la
chimenea: ~31 celdas de vertical con salto de 22; solo se llega vertiendo escaleras. Esa es la prueba,
y cuesta cero: `SoloPies` existe y `ReachWorld` es una constante.

### 1.5 Lo que sí confirma el código

- **Costura frame/tick, cerrada.** `AlkahestSim` :372-383 da `mult` pasos por frame con una posición
  por frame (`DefaultExecutionOrder(−10)`): idéntica para los diez ticks. A pie no hay bob en el
  transform (:1041): caja estable quieto. El sello guarda `(tick, X0, Y0)` al cambiar. La
  contradicción (a) de `_huecos` se cierra para el cuerpo (no para el `Paint` del frasco, que es de J).
- **Brasa viva.** `PaintCell` → `SetCell` resetea `aux` (AlkahestSim :617): hoy la brasa vertida nace
  muerta; el fijo es `VidaBrasa` en `Flask` + `PaintCell` con `aux`: ~80 líneas y `Flask` reabierto en
  cuatro sitios (refutación C2): **0,5 semanas, no 0,25**. Número escondido: R135 dice que la brasa
  tapada descuenta uno de cada cuatro (`ProcessBrasa` :1017, 32-48 s); la dirección quiere 8-12 s. A
  1,5 u/s son 120-180 celdas; corriendo (2,6 u/s) 208-312: elegir la vida del frasco es diseño.
- **Piel.** Adopta las correcciones de las refutaciones (`cCarne` 8-16, `caras 4`,
  `LabNacerAguaParcial`, sobrecarga de `LabSecarHacia`). Falta correr el **día de sonda** de `01`
  (`temp[]` en la caja a 0/2/6 celdas del hogar): si no distingue, el halo es un termómetro redondo.
- **Multi.** El espejo no tiene stepper ni `temp[]` (AlkahestSim :338; `SimSync`): halo del invitado
  = ventana 31×35×2 B a 4-5 Hz (~6 KB/s) y su caja llega al anfitrión con 2-5 ticks de retraso: fuga
  al moverse, ninguna quieto. ~0,5 semanas que no están en las 3.
- **«Con cuerpo» por bloqueos** tiene el agujero de la sombra (nadie «intenta entrar» en `LabLuz`) y
  el de pasar cerca (decenas de bloqueos al cruzar un penacho): contar también los `LabLuzDesde`
  enmascarados y definir región y umbral. Es la única decisión con número de la dirección.
- **Polvo en reposo es suelo** pide `SampleReposo` y diez líneas en `CajaChoca`, y con él llegan
  situaciones que nadie ha jugado: enterrarse bajo lo que viertes, `Desenterrar` (R121c) saliendo
  por un montón, un montón que el agua vuelve móvil bajo tus pies. Es la clase de iteración
  (R110→R121b) que la dirección dice no tocar; la toca por el suelo, no por el salto.

## 2. Evidencia del laboratorio

**La sostiene:** HF1 (boca 1 → 100 %, boca 4 → 19 % ceniza: tapar tiene curva); `LabPresion` 5/5;
el humo come luz (`LuzDecayHumo` 24); salto 22 celdas y montones con deslizamiento propio: la
escalera de materia es plausible; `LabVistaColor`/`LabTinte` existen: el halo es un recorte.

**La contradice:** ninguna curva del laboratorio cambia porque el tapón sea de carne; con 12 celdas
y frasco reversible, sustituirse por piedra es un gesto y quitarla otro: la pieza humana es una
piedra reversible cuyo único añadido es el halo (paquete C sin máscara). La máscara compra
**onboarding** (aprender la máquina siéndola), no una decisión de régimen. El «humo en las piernas»
del minuto 1-3 exige boca 3 abierta con la pila ardiendo (cámara envejecida por la cuna): cierto y de
autor.

## 3. La prueba más barata que la mata (reordenada)

- **Puerta 0 · día 1 · cero código.** `SoloPies` + `ReachWorld 1.2f`. El hermano de Cesar, a pie:
  llegar al lecho de la cámara alta vertiendo arena, y a la boca del horno. **Mata «a pie»** si no
  llega en cinco minutos o vuela «sin querer»; entonces cae la premisa («sin esas tres el cuerpo no
  necesita estar en ningún sitio») y queda C + máscara en vuelo: capa, no dirección.
- **Puerta ½ · días 2-3 · banco sin arquitectura: la piedra sustituta.** Los seis papeles con un
  bloque de piedra 7×11 en el sitio del cuerpo, como variantes `Montar*`: tapa (carbonera **boca 4**:
  abierta / piedra en la boca / bloque encima, ×5 desfases), presa (canal de 4 de manantial a
  sumidero: abierto / bloque), sombra (bloque en el pozo del cielo: `luz[i+W]` sobre el lecho y
  `LabPlantasNacidas`), techo (bloque bajo el serpentín: goteos por columna), corcho (tubo con y sin
  aire). Con un gancho por tick que quite el bloque en el tick T (el mismo guion que luego usa la
  máscara), **bisección de la presencia mínima** por situación (idea de la corrida anterior: 13
  corridas × 9 000 ticks ≈ 3 min por situación). **Mata la pieza** si ningún papel cambia el veredicto
  de su situación más que el ruido entre seeds (bloque ≡ nada): no se escribe la máscara. **Mata
  «esperar»** si la presencia mínima supera 1 800 ticks (60 s a ×10) en la mitad de las situaciones.
- **Puerta 1 · semanas 1-2,5 · secuencial.** `ocupa` + `Move` + ocho sitios, guion en `Correr`, seis
  equivalencias cuerpo/bloque por clase de sitio (gas, líquido, polvo sobre la cabeza, brasa bajo el
  cuerpo, goteo, presión) con barrido de desfase, guardia de 63 hashes, ms/tick en «mundo entero
  despierto» con cuatro cuerpos, verificación 1:1. **Mata la máscara** si la fuga supera 10 % en
  alguna clase tras el transitorio, no es robusta a ±1 celda, mueve un hash sin cuerpo o cuesta > 5 %.

## 4. Tiempos corregidos

| hito | Opus | calendario | personas |
|---|---|---|---|
| puerta 0 | 1 h | día 1 | 1 sesión |
| puerta ½ (piedra sustituta + presencia mínima) | 2 días | días 2-3 | 0 |
| puerta 1 (máscara + equivalencias + desfase) | 1,5 sem | semanas 1-2,5 | 0 |
| a pie · 12 · polvo=suelo | 0,25 sem | carril paralelo | 2-3 sesiones |
| piel sensor + día de sonda | 0,75 sem | paralelo | 1 sesión (G6) |
| brasa viva | 0,5 sem | paralelo | 0 |
| Ojo | 0,2 sem | paralelo | 0 |
| 6-8 situaciones `Montar*` con cláusulas sobre la grilla y contador en `LabPanel` | 1 sem | tras la máscara | 1-2 sesiones (sin texto) |
| BFS a pie | 0,3 sem | paralelo | 0 |
| K perturbaciones del guion | 0,3 sem | **tras J** (validador por veredicto) | 0 |
| halo e intervención del invitado | 0,5 sem | semana 5 | 0 |
| veredicto completo, fichero, «día N sin cuerpo» | J: 5-6 sem | J va primero de todos modos | — |

**Evidencia para matarla: 0,5 semanas** (puertas 0 y ½; la ingeniería de la máscara a las 2,5).
**Prototipo feo sin J: 4,5 semanas de Opus, 4 de pared** en dos carriles (la dirección dice 4/3).
**Iteración humana antes de juzgar el core: 5-7 sesiones** (la dirección dice 3), más 3-6 días de
autoría y resolución de 10-14 situaciones.

## 5. Iteración humana oculta

«¿Ser la pieza es esperar?» solo lo responde una persona; la mitigación (contador) es un diagnóstico,
no un arreglo; la presencia mínima del banco es su proxy aritmético y hay que correrlo antes. A pie
con montones: colisión nueva, 2-4 sesiones. Semántica del contador («¿por qué se me puso a cero?»):
1-2. Descubrir sin texto: una sesión por 4-6 situaciones, no por 8-10. Autoría y resolución de las
situaciones: 0,25-0,5 día cada una. Precisión de colocación de la tapa: la decide el barrido. Total
realista: 8-14 días-persona en ocho semanas.

## 6. Qué se automatiza en su lugar

Equivalencia cuerpo/piedra por clase de sitio y por desfase; fuga (humo fuera del recinto con cuerpo
menos con piedra); desnivel tras el transitorio; `luz[i+W]` bajo la sombra; goteos bajo el techo;
presencia mínima por bisección; alcance de la brasa bajo cada regla; `Ignite > 0` en 9 seeds;
`cCarne`/`caras`; ms/tick con cuatro cuerpos; guardia de hashes; BFS a pie; en qué ley importa el
cuerpo (K perturbaciones, con J); validez de cada situación. No se automatiza: sensación a pie con
montones, la vida de la brasa en el frasco, qué bloqueos cuentan, y si un minuto quieto a ×10 es jugar.

## 7. Órganos a conservar si se descarta

Posición como entrada del tick con guion `(tick)→(x,y)` en `Correr` (sirve a C, al sello y a
cualquier sólido móvil futuro); `ocupa` como máscara de entrada; el protocolo de equivalencia por
clase de sitio y el barrido de desfase; la piedra sustituta y la bisección de presencia mínima como
medidas estándar del banco para CUALQUIER pieza; brasa con `aux` en el frasco; vista Ojo; halo como
recorte de `LabVistaColor`; «con cuerpo» medido por la física (con la corrección de la luz); BFS a pie
como filtro de la cuna; `SoloPies` + `ReachWorld 1.2` + polvo en reposo como suelo: la física del toque
que `_huecos` §2 pide para el prototipo feo de toda dirección con avatar.

## 8. Rúbrica v2

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | una máscara cruza cinco leyes con curvas medidas; no cambia ningún régimen: compra onboarding, no decisiones nuevas |
| ejecutan, revelan y juzgan | 6 | ejecutan sí; revelan (halo) sí; juzgan solo con J; «con cuerpo» tiene el agujero de la sombra |
| iteración humana (10 = poca) | 6 | 8-14 días-persona; a pie con montones reabre el historial más caro del proyecto por el suelo |
| verificabilidad automatizable | 7 | guion, equivalencias, hashes, BFS, presencia mínima; pero sus dos pruebas propias no pueden fallar y hay que reescribirlas |
| la simulación es el juego | 8 | el cuerpo es una celda gorda de entrada; la única capa es un contador |
| onboarding garantizable | 7 | una ley y un papel por situación; validador con perturbaciones; sombra y corcho son de autor |
| observabilidad | 6 | el halo es el visor más barato y real del panel; la fuga por la costura es invisible para el jugador |
| tiempo como apuesta | 5 | sustituirse y soltar existe en toda dirección con sello; ser la pieza no arriesga nada y volver cuesta cero |
| multiplayer emergente | 5 | «sujeta esto mientras…» sin probar; halo del invitado sin replicar |
| profundidad por leyes estables | 4 | prototipo→piedra por ley y se agota; la profundidad es del sustrato |
| cuerpo del jugador | 8 | instrumento pleno y pieza física sin barras; el riesgo va hacia fuera |
| identidad comercial | 6 | «TAPA HUMANA» es clip; el cuerpo que no sufre y el avatar de 7×11 limitan la cápsula |
| dificultad técnica (10 = fácil) | 7 | C# acotado; lo delicado: cuatro familias de predicados en un núcleo congelado, un invariante transversal permanente, sueño al salir, halo del invitado |

**Riesgo mayor (técnico):** la máscara es un invariante transversal sobre cuatro familias de
predicados «¿es aire?» en un núcleo congelado desde R141, que cada ley futura (aire A, arrastre V, haz
L) vuelve a abrir: cuerpo≡piedra es una prueba permanente del anillo, no una demostración; y la
dirección llega sin haber medido si la pieza importa (línea base nula). **Riesgo mayor (diseño):** que
«a pie» muera o cueste las nueve rondas de siempre; se decide el día 1 sin escribir física, y hay que
decidirlo antes de la máscara.
