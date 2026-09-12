# CRÍTICA · INGENIERÍA, EVIDENCIA Y PRUEBA QUE LA MATA · dirección libre «A que sí» (pronóstico)

*(Director técnico, panel de direcciones, segunda pasada, 2026-09-12, versión revisada. Leído entero:
`libre.md`, `01_LEYES.md`, `00_ENCARGO_Y_CRITERIO.md`, las refutaciones de ingeniería de `sello` y
`cuna`, `02_DIRECCIONES.md` §4 y `03_COMPARATIVA_Y_TIEMPOS.md` §1-2 (lo que la síntesis ya dijo de
esta dirección), las otras dos críticas de `libre` (para no repetirlas). Código: `LabBench.cs` entero
(`Escenarios` :76-87, `MontarAlambique` :105-109, `MontarCarbonera` :135-143, `Correr` :265-344 con la
caldera gateada por nombre :295 y :306-311, `MedirLecho`, `ArcoAvanzar/ArcoMuestra` :403-452);
`SimStepper.Laboratorio.cs` (`LabNacerAgua` :264, `LabGotear` :317-327, `LabAire` condensación al
más frío :356-383, germinación :702-712, `LabPlanta` :801-908, `LabHogar` :917-946, `LabRespira`
:1052-1062, `LabLuz` :1177-1258 con la boca de cielo estática :1196-1202); `SimStepper.cs` (`Step`
:257-330, `ApplyPhase` :640-653, `Transform` :707-720, `ProcessCombustion` :829-935 con la lengua
:879-883 y el dado del carbón :919-929); `Universe.Laboratorio.cs` (fibra `combustPasoTicks` 8,
`combustLenguaPct` 40 :88-92; agua hiela a 60 raw :211; hielo funde a 62 :213); `LabParams.cs`
(estático; `RendimientoCarbonPct` 25 :107, `VidaHumo` 255 :103, `GerminaPorMil` 2 :134,
`LuzCieloX0/X1` :131); `SimLevelBuilder.Laboratorio.cs:164`; `CellGrid.SetCell` :239-262; el
benchmark `Laboratorio/benchmarks/2026-09-06_2149_banco.md`; un `grep` de estáticos mutables en
`Sim/`. Mi único tema: qué necesita del sustrato, qué evidencia la sostiene o la contradice, y la
prueba más barata que la mata.)*

## 0. Veredicto en una línea

**SEGUNDA RONDA, y solo para la variante corregida.** La pieza técnica (la banca) es acotada, barata,
sin física nueva y se verifica en banco sin personas: es la automatización de balance más ambiciosa
del panel. Pero **tal como está escrita, la dirección queda refutada por lectura de código**: el
fantasma es una afirmación sobre UNA celda en UN tick, y a esa resolución la física del laboratorio
no es determinista en el sentido que la cuota necesita: el carbón lo decide un dado del 25 % por
celda (`SalLabCarboniza`), la llama es una lengua que aparece con 40 % por paso, el humo es un paseo
al azar de 255 ticks, la gota del serpentín nace helada y la superficie del agua oscila una fila. Para
ocho de las trece familias, el mapa de cuota por celda **cotiza dados, no leyes**, y el «nivel medio»
que la dirección necesita para que haya apuesta lo fabrica el dado gratis. Su propia prueba §8 pasa
en falso por eso. El ajuste (familias por vecindario o fantasma por caja con umbral) es acotado, 2-3
días, pero cambia el pincel, el visor y la frase, y acerca la dirección a «El Recibo con cuota». Se
decide en un día de banco (§6) y, si sobrevive, en semana y media más.

## 1. Qué necesita del sustrato, y qué hay

| pieza | ¿en el catálogo? | ¿acotada y verificable en banco? | dependencia secuencial |
|---|---|---|---|
| diario bajo las cinco puertas + `CorrerSello` + cláusula sobre la grilla | J·sello (viable ×2, 3,2 sem) | sí (prueba (d) de la refutación) | ninguna; es la raíz de todo lo demás |
| volcado y carga de la rejilla | J·sello | sí (round-trip 4 500 + 4 500) | diario; solo lo necesitan sobre cerrado y cueva larga |
| `LabBandas` para «agua clara» | J | sí | ninguna |
| **la banca**: gramática de registros, corredor R0 + K, volcado de `mat` por día, mapa `p` | nuevo | sí: es un `Correr` con `Montaje` compuesto y muestreo por día (patrón `ArcoAvanzar`) | para la prueba, ninguna; para el juego, el diario |
| fantasmas, pincel, evaluación, render | nuevo (cliente) | evaluación sí; pincel y render, personas | cláusula de J (o un stub) |
| visores de cuota y sin manos | nuevo (cliente) | no (lectura en lámina) | banca |
| escalera (entropía, sensibilidad ±20 % por grupo de `LabParams`) | nuevo | sí, EN SERIE (`LabParams` estático) | banca |
| recibo y sobre cerrado | nuevo | sí (hashes) | volcado |

Nada pide física del catálogo refutado. La única modificación del motor que la dirección no nombra y
que sí necesita es la **máscara de familias vista por día** (§3): dos bytes por celda fuera del hash,
`OR` en los tres puntos que escriben `mat[]` (`CellGrid.SetCell` :239, `SimStepper.Transform` :710 que
escribe `mat` a pelo, `SwapCells` :264), reset al cerrar el día. Dos o tres días con un grep de
`mat[` que cierre cualquier cuarto punto.

## 2. Lo que el código ya da, y lo que no da

**Da.** `Correr(nombre, ticks, Montaje)` monta por delegado, restaura `LabParams` a fábrica
(`RestaurarDefaults` :276) y devuelve siete hashes: la prueba no necesita el diario. La única
intervención del banco (la caldera) está gateada por nombre (:295, :306-311): la banca necesita un
delegado por tick o una lista de intervenciones tick-estampadas (horas; es el mismo órgano que la
refutación de la cuna llama «intervención genérica»). `ArcoMuestra` ya es el visor sin manos en tabla.
Coste medido, no estimado: alambique 1,96 ms/tick, carbonera 1,57 (banco del 06-09): cotizar el
alambique con K = 16 son 17 × 9 000 × 1,96 ms ≈ **5,0 min en serie**; la carbonera, 4,0. Cien
situaciones, 8 h en serie. El `grep` no encuentra **ningún estático mutable** en `SimStepper*.cs`,
`CellGrid.cs` ni `Universe.cs` fuera de `LabParams` y los `_arco*` de `LabBench` (que `Correr` no usa):
las K corridas de la MISMA situación pueden ir en hilos dentro del proceso, cada una con su `Universe`
(un día de banco: cuatro hilos, hashes iguales a los de serie). Con cuatro hilos, cien situaciones en
dos horas. Memoria del mapa: 221 184 celdas × 13 familias × 5 días × 1 byte = 14 MB por situación;
acotado a la caja del montaje ± 8 celdas, ~0,5 MB.

**No da, y la dirección lo asume.**

1. **`LabParams` es estático y `Universe.Create` usa `Mathf`/`Color32`.** La escalera (±20 % por
   grupo) va en serie; «paralelo entre procesos» son builds headless de Unity en batchmode, no un
   ejecutable de C# puro. Para el lote nocturno vale; para «cotizar en segundo plano» dentro del juego
   (cueva larga), hilos con los parámetros congelados durante la corrida o un proceso aparte: 3-5 días
   no contados en §5 de la dirección.
2. **La boca del cielo es un estático global** (`LuzCieloX0/X1`, escrito por el constructor :164, leído
   por `LabLuz` :1196-1202). Un corte de un mono (o del jugador) en el techo **no abre luz**: los
   fantasmas de planta y todo lo óptico son ciegos a los cortes hasta «cielo por geometría» (el ajuste
   de la refutación de la cuna: fuente = toda celda `Empty` en H−2; un día, mueve `HashLuz`).
3. **La preparación tiene que ocurrir con el tiempo parado.** Los monos pintan en el tick 0; si el
   jugador prepara tres minutos a ×1 con el mundo corriendo, sus «días» ya no son los del mapa. O el
   montaje es sobre la pizarra parada (SOLTAR = tick 0: no ves caer el agua que viertes hasta soltar) o
   los monos también pintan a lo largo del tiempo (otra gramática). La dirección no lo dice.
4. **Dependencias secuenciales:** diario → `CorrerSello` y cláusula → banca (volcado por día) → visores
   y recibo; volcado/carga → sobre cerrado. Unas cuatro semanas de motor que no se comprimen con más
   Opus; el cliente (fantasmas, pincel) va en paralelo sobre un stub.

## 3. El hallazgo central: el fantasma está por debajo de la resolución determinista útil

La cuota es `1/p` con `p` = fracción de los 17 registros (R0 + 16 monos) que produjeron la familia
**en (x, y) al cerrar el día D**. Ese `p` solo mide conocimiento si, a esa resolución, lo que sale
depende del registro y no de un dado. Leyendo el código familia por familia:

- **Carbón y ceniza.** Al agotarse la reserva en sordina, cada celda tira `XorShift.FromCell(_tick,
  x, y, 632).ChancePercent(25)` (SimStepper.cs:919-929): carbón o ceniza **por celda, al 25 %**, con
  el tick de agotamiento como semilla. Dos regímenes, y los dos matan la cuota por celda:
  - *Registros que no tocan la pila* (un rectángulo lejos: la mayoría de los monos): el tick de
    agotamiento de cada celda es el mismo que en R0, el dado sale igual y el patrón de carbón es
    **bit-idéntico a R0**. El mapa es binario: ×1,2 donde R0 hizo carbón, ×8-×17 donde hizo ceniza, y
    el visor sin manos lo regala.
  - *Registros que tocan la pila* (una paja pegada, una celda de terracota en la boca): el tiempo de
    cada celda se desplaza, el dado se rebaraja y cada celda vuelve a ser carbón con p ≈ 0,25 sea cual
    sea el registro. Con `n ~ Binomial(16, 0,25)` (media 4, σ 1,7; `P(n = 0) = 0,75^16 = 1 %`) el
    **99 % de las celdas de la pila queda en «nivel medio»** por el dado, no por saber nada.
  - *La explotación que el propio visor enseña:* toca la pila con una paja (rebaraja) y pinta «carbón,
    día 1» en todas las celdas donde R0 dejó ceniza (el visor las pinta incandescentes, ×8-×17). Valor
    esperado por fantasma: `0,25 × (8..17) − 0,75 = +1,25..+3,5`, con conocimiento cero. Es el
    tutorial «Brasa» del minuto 5-7: quien la tapa bien ve tres de cada cuatro fantasmas huecos y no
    aprende la carbonera; aprende que el tablón es una tragaperras.
- **Fuego.** `Fire` es «solo la lengua visible»: un `Transform` en la celda vacía de encima con
  `combustLenguaPct` 40 por paso de 8 ticks (:879-883), vida `fireLifetime` ± 4 (`Transform` :712-717).
  `mat == Fire` en el tick 1 800 es una moneda sobre una pila que arde de verdad.
- **Humo.** `VidaHumo` 255 ticks y rumbo por hash de bloque 8×8 (hecho 8 de `01`): la celda exacta a
  un tick es un paseo al azar.
- **Agua bajo el frío (la situación «Gota», minuto 0).** `LabGotear` nace el agua con la temperatura
  de la superficie fría (`LabNacerAgua(j, temp[idx])` :320; `FrioRaw` 30) y `ApplyPhase` la hiela al
  tick siguiente (`freezesAt` = 60 raw, Universe.Laboratorio.cs:211; el hielo funde a 62, :213):
  perdigón que cae y se derrite en el lecho. «Agua el día 1 bajo el frío» es hielo en vuelo y humedad
  en el poroso; la celda con `mat == Water` está donde se acumula, y esa superficie la mueve
  `LabPresion` una fila arriba o abajo cada paso.
- **Planta viva.** La germinación es `Next(1000) < GerminaPorMil` (= 2) por visita y celda (:706-712):
  dado por celda otra vez, además de ×17 o imposible hasta el día de Q16 (R148/R150).
- **Deterministas por celda de verdad:** terracota cocida, vidrio (temperatura sostenida), sedimento
  depositado (casi), aire/vacío. Cuatro de trece.

Consecuencias directas sobre la prueba §8 de la dirección: **(a) pasa en falso** (el dado del carbón
y la banda de la superficie del agua ponen ≥ 10 % de celdas en nivel medio en cualquier montaje) y
**(b) pasa en falso** (tres fantasmas de carbón de cuota ≥ ×4 salen de cualquier pila cerrada, con o
sin conocimiento). Y sobre el diseño: la resolución a la que las leyes son deterministas es el
**agregado** (cuántas celdas de carbón en la pila, cuántos goteos, columnas anegadas: exactamente lo
que el libro mayor y `MedirLecho` ya cuentan). El ajuste acotado es **familias por vecindario** (la
familia en (x, y) = «≥ f de las celdas de la caja 5×5 son M», o el fantasma como caja con umbral),
más la máscara «visto en el día» para lo transitorio (fuego, humo, gota). Precio: el pincel pinta
cajas, el visor se suaviza, la frase deja de ser «aquí» y pasa a ser «por aquí, más o menos tanto», y
el umbral `f` es un número humano (o relativo a R0). Con eso la dirección converge hacia la cláusula
«métrica por día ≥ n» de J cotizada por monos: El Recibo con cuota. No es malo; es otra dirección.

## 4. La cuota paga rareza bajo un prior, no conocimiento (breve: las otras dos críticas lo cubren)

Concuerdo y solo añado lo que el código cuantifica. El presupuesto de §8 da al jugador 40 cortes y a
los monos 3: una zanja de 40 desde la poza al hueco lejano vale ×17 por fantasma con la ciencia de
que el agua baja, y la anulación por celda tocada no la frena (el hueco no está tocado). Con
`Caudal` 24 y `LabPresion`, cualquier rectángulo de un mono en la poza desplaza la superficie
(área/anchura) filas: la **banda de la superficie del agua queda en nivel medio en toda situación con
agua**, gratis. Y la asimetría de la prueba: el serpentín de 31 celdas de R141 contra 5 de frío de
presupuesto; si el serpentín va en la base, el autor es R0 y no puede cobrar; si va en el registro,
gana por presupuesto. La prueba tiene que ser simétrica. Las dos definiciones alternativas que se
miden con el mismo arnés: **mutantes del registro del jugador** (cuota = fragilidad: no paga espacio,
paga precisión; solo existe tras sellar, 1,5-5 min o hilos) y **sensibilidad por grupos de
`LabParams`** (cuota = leyes implicadas: la gota del serpentín depende de evaporación, vapor,
condensación y frío; la zanja de nada). Ninguna es limpia; las tres se miden.

## 5. Evidencia del laboratorio: qué la sostiene y qué la contradice

**La sostiene.** Determinismo con siete hashes en nueve escenarios (la licencia de cotizar, rejugar y
comparar). 1,5-2 ms por tick medidos: cien situaciones caben en una noche en serie y en dos horas con
hilos. `ArcoMuestra` es ya el visor sin manos. Los cruces medidos (R135 el alambique ahoga, R148
sombrea) son el material de los pronósticos de segundo orden de la hora 5.

**La contradice u obliga a corregir.**

- **Hecho 4 (graniza):** «Gota» está escrita contra un alambique que el código contradice (§3). Cada
  situación de mano necesita su corrida R0 antes de escribir su texto.
- **R148/R150:** la planta es ×17 o imposible hasta L; fuera de la primera hora.
- **R135 (una celda de labio anega 24 de 48 columnas):** donde el agua decide, la física es caótica;
  con K = 16 la resolución de `p` es 1/17 y el error binomial en `p = 0,5` es ± 0,125: la misma celda
  cotiza entre ×1,6 y ×2,7 según el sorteo. El mapa será **moteado** donde importa. K = 64
  cuadruplica el coste (20 min por situación en serie).
- **R136 (la boca no regula la pila maciza: Tfuel idéntico con chimenea 0/1/2; boca 1/4/8 → 100/81/88 %
  de carbón, no monótono):** la carbonera tiene dos regímenes, no un gradiente. Su mapa de cuota por
  caja tendrá dos niveles y el dado por celda entre medias.
- **Dones infinitos y «nada produce sin volver a tocarlo» (hecho medido):** bajo R0 el mundo llega a
  régimen en el primer día; los fantasmas del día 5 son ×1 o ×17 hasta V, A, F o D. La cueva larga de
  la hora 20 con K = 4 (cinco niveles de `p`) es la pieza más débil.
- **Hecho 9 (no hay fichero) y determinismo entre máquinas no probado:** sobre cerrado, recibo por
  fichero y cueva larga quedan fuera del prototipo feo; el diseño «los hashes del emisor delatan la
  divergencia» es el correcto, y añade un día de `LabBench` en la build IL2CPP contra los 63 hashes
  del editor antes de prometer nada entre PCs.

## 6. La prueba más barata capaz de matarla (dos escalones)

**Escalón 0 · «17 piedras lejanas, 17 pajas cercanas» · UN DÍA.** Sobre `MontarCarbonera` (recinto
ya cerrado, boca 1, 400 celdas de fibra), 9 000 ticks, sin diario: (i) 17 corridas con una sola celda
de `Stone` en 17 posiciones lejanas (no tocan el recinto); (ii) 17 corridas con una sola celda de
`Fibra` pegada al recinto o en la boca, en 17 posiciones. Por corrida: hash FNV del subrectángulo
(101-120, 201-220) de `mat` y, por celda, si es carbón. Coste: 35 × 9 000 × 1,57 ms ≈ 8 min de banco;
medio día de Opus (perturbación única en el `Montaje`, hash de región, conteo por celda); medio día
de lectura. **La mata** si (i) los 17 hashes de región son iguales (el mapa de carbón es el de R0 y el
visor sin manos lo regala) **y** (ii) en las pajas la frecuencia por celda `n` es compatible con
`Binomial(16, 0,25)` (media 4 ± 1,7; ≥ 60 % de las celdas de la pila con `n` en 1..15 y varianza
entre celdas ≈ 3): la cuota por celda es un dado. **La salva** si en (ii) la varianza entre celdas
supera en más del doble la binomial (estructura: la boca siempre ceniza, el fondo siempre carbón) **y**
un registro de fantasmas puesto con conocimiento (fondo, lejos de la boca) supera en más de 2σ al
mismo número de fantasmas puestos al azar dentro de la pila. El código predice que la mata; se corre
igual, porque es un día y porque decide si el fantasma por celda existe.

**Escalón 1 · el arnés completo · 1,5 semanas, solo para la variante por vecindario.** `Correr` con
`Montaje` compuesto (base + registro) y muestreo al cierre de cada día; máscara «visto en el día»;
familias por vecindario 5×5 con `f` fijo; gramática de registros con **presupuesto simétrico** (autor y
monos con 5 de frío y 40 cortes en segmentos de 1-6); tres generadores (monos, mutantes de R,
`LabParams` ±20 % por grupo); dos montajes (alambique sin serpentín en la base; carbonera con el
recinto sin boca en la base); región = caja ± 8; cinco días; familias agua, hielo, humedad-alta,
carbón, ceniza, fuego y humo por máscara. Cuatro criterios por definición de cuota: (a) gradación
fuera de la banda de superficie de R0 (≥ 10 % de celdas de aire en `n` 1..15 para alguna familia
producto); (b) el autor cobra ≥ 3 fantasmas de ≥ ×4 con el mismo presupuesto; (c) el «tonto
espacial» guionizado (zanja de 40; 30 de fibra lejos del hogar; 5 de frío en el rincón) no llega al
70 % del autor; (d) moteado < 30 % (celdas de nivel medio cuya mediana de 4 vecinos difiere en > 4
niveles). Muere la definición que falle (c) o dos de las otras tres; muere la dirección si mueren las
tres. Coste: 4-5 días de Opus, ~40 min de banco, un día de Fable.

**Escalón 2 · papel, dos días, cinco personas**, solo si algo pasa el escalón 1: la prueba de la
dirección (¿pintan donde el tablón paga o donde ya saben?). Sobre examen o apuesta el banco no dice
nada: solo el prototipo feo a ×10 con fantasmas.

## 7. Tiempos corregidos

| hito | dirección | corregido | por qué |
|---|---|---|---|
| evidencia para matarla | 1,5 sem | **1 día (escalón 0) + 1,5 sem (escalón 1)** | el escalón 0 no tiene dependencias ni código nuevo de motor; el 1 reutiliza `Correr` |
| prototipo feo | 6 sem calendario (8-9 Opus) | **6-7 sem calendario (7-7,5 Opus)** | J-mínimo 2 (diario + `CorrerSello` + cláusula; volcado fuera del feo) + máscara y vecindario 0,5 + banca 1,5 (reutiliza el arnés) + fantasmas y pincel por caja 1-1,5 + visores 0,6 + presupuesto 0,4 + hilos con hashes 0,2 + un día de R0 por situación de mano. Cadena secuencial del motor ≈ 4 sem; cliente en paralelo sobre stub |
| iteración humana para juzgar el core | 3-4 tardes + 2 días | **≈ 6-8 días, más una ronda por cada redefinición de la cuota** | pincel por caja sin menú, lectura del visor en lámina, ×10 con fantasmas, examen/apuesta |
| con L y V | 12-13 sem Opus | 12-14 | sin cambio; L primero (Q16) |

## 8. Iteración humana oculta

«Pesos que no puso nadie» es cierto de la cuota dado el presupuesto y la gramática; y esos dos son de
autor: el vector de presupuesto por situación, el número de fantasmas y el horizonte deciden qué
pueden hacer los monos y, con ello, el mapa entero. Una vez y de gusto: K, techo, suelo, castigo por
fallo, umbral `f` del vecindario y la definición y ventana de cada una de las trece familias (una
tarde de banco cada una). Por situación: la tarde de R0 que rehace el texto. Y lo incomprimible: si
pintar el futuro se siente apuesta o examen, que ningún banco decide y que, si sale «examen», no tiene
arreglo técnico. 5 sobre 10 desde mi lente: pasa la puerta por poco.

## 9. Qué se automatiza en su lugar

El balance entero (las cuotas de todas las situaciones), la validez de cada situación y mutante (R0
falla, el autor cobra, gradación, moteado, explotabilidad), el orden de campaña (entropía y etiqueta
por ley), la regresión de cada ley nueva (recotizar cien situaciones en dos horas con hilos), la
detección de copias y de divergencia entre máquinas, y la existencia de solución. Sigue siendo la
lista de automatización más larga del panel, y es la razón de no descartar la banca aunque muera el
fantasma.

## 10. Órganos que valen para cualquier otra dirección

1. **La banca como validador continuo**: gradación, moteado y explotabilidad como cifras sustituyen
   al «K perturbaciones deben dar veredictos distintos» de la cuna; encaja en F1 y en El Recibo.
2. **El visor sin manos** (R0 por día como fantasmas grises): tutorial sin texto de toda dirección con
   SOLTAR.
3. **La cláusula por familia con máscara «visto en el día» y por vecindario** (no `mat == M` a un
   tick): mejora la condición como dato de J para fuego, humo, gota y carbón.
4. **La intervención genérica tick-estampada de `Correr`** (desgatear la caldera): la necesitan la
   cuna, el sello y cualquier «montaje − solución».
5. **El sobre cerrado entre coautores** (creencias ocultas hasta SOLTAR): multijugador sin roles a
   coste cero de física.
6. **El recibo compartible** (rejilla de aciertos y huecos): el órgano de Wordle para todo veredicto.
7. **La etiqueta por ley** (sensibilidad por grupo de `LabParams`): la firma de leyes de El Recibo.
8. **El día de hilos con hashes**: K corridas en proceso, que abarata todo validador del panel.

## 11. Rúbrica v2 (lente técnica)

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | ley de juicio sin física que recotiza todo; pero a la resolución escrita cotiza dados y rareza espacial, y la palanca real espera a la redefinición |
| leyes ejecutan, revelan y juzgan | 7 | ejecutan; revelan (cuota, sin manos); el juicio es un prior de autor muestreado, y por celda juzga el dado |
| iteración humana (10 = poca) | 5 | presupuesto, fantasmas y horizonte por situación; mandos globales; examen/apuesta; una ronda humana por redefinición |
| verificabilidad automatizable | 9 | todo en banco, incluidos el dado, el moteado y la explotación |
| la simulación es el juego | 5 | dos máquinas idénticas puntúan distinto según lo pintado; el verbo central no toca una celda |
| onboarding garantizable | 7 | escalera calculada y visor sin manos; «Gota» y «Brasa» contradicen el código y se rehacen sobre R0 |
| observabilidad | 7 | dos visores que salen de corridas; moteado con K = 16 donde el agua decide |
| tiempo como apuesta | 9 | apuesta contra el tiempo con cuota |
| multiplayer emergente | 6 | creencias ocultas es real y barato; cuelga del volcado y del determinismo entre máquinas |
| profundidad por leyes estables | 5 | techo ×17, prior que se satura, mundo en régimen desde el día 1 hasta V/A/F/D |
| cuerpo del jugador | 2 | cursor |
| identidad comercial | 6 | recibo y «×17» como clip; puzle 60-150 k; el co-op de creencias puede subirlo |
| dificultad técnica (10 = fácil) | 7 | acotada; vecindario, máscara, hilos y batchmode son coste conocido |

Ponderado: 150 de 240. Puertas: iteración 5 ≥ 5, apalancamiento 6 ≥ 5: pasa por poco.

## 12. Veredicto

**Segunda ronda, condicionada y estrecha.** Como está escrita (fantasma por celda y tick, cuota
contra monos con presupuesto asimétrico) la refuta el código sin correr nada; el escalón 0 de un día
lo confirma o me desmiente. Lo que va a segunda ronda es la variante por vecindario con alguna de las
tres definiciones de cuota, y solo si pasa (a)-(d) en los dos montajes con presupuesto simétrico. Si
solo pasa «mutantes», cambia la frase («según lo exacto que tenías que ser») y es otra dirección. Si
no pasa ninguna, se descarta y se conservan los ocho órganos de §10, que valen por sí solos para F1 y
El Recibo, y en particular la banca como validador: la síntesis de `02` ya la adoptó, y ese es el
producto real de esta dirección aunque el juego no lo sea.
