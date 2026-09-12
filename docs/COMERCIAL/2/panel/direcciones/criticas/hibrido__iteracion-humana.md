# CRÍTICA · «SELLADO» (hibrido) · lente: ITERACIÓN HUMANA OCULTA

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: productor; único tema: cuánta iteración
humana esconde la dirección (playtest, tuning, balance, contenido de autor, contingencias ante lo que
construya el jugador) y qué parte se sustituye por banco headless, hashes, generación y validación
automática. Leído entero: `hibrido.md`, `01_LEYES.md` (con §6), las refutaciones de sello (×2), balanza
y cuna (×2), `_huecos_y_combinaciones.md` §2 (los verbos), `03_MERCADO.md` §1-3, `02_DIRECCIONES.md`
§1-4 y `03_COMPARATIVA_Y_TIEMPOS.md` §1-2, las críticas hermanas de ingeniería y de simulación sobre
esta misma dirección (para contradecirlas solo con causa), los bancos `2026-09-05_r148_h7s_arco_largo.md`
y `2026-09-04_r137_hf5_cierres_del_fuego.md` (la tolva). Código: `SimStepper.Laboratorio.cs` (libro
:49-118; `LabCampos` :201-239 con la cadencia de 8 ticks en :205 y la roca suelta en `LabRoca` :229-232;
decantación :455-464 y depósito :466; `LabInfiltrarHacia` :498-521; `LabManantial` :1004;
`LabSumidero`/`LabTragar` :1021-1032), `LabMateriales.cs` (`Tallable` :43, `EsFondo` :98),
`LabParams.cs` (las tres banderas :110/:117/:148; `PermGrava` :73 y su rótulo :210; `ColmatacionPct`
:84; `DepositoReposo` :65), `LabBench.cs` (`Escenarios` :76-88, `Correr` :265-335, la caldera
:299-311), `AlkahestSim.cs` (las seis puertas :571-831), `Cincel.cs` (radio 2, alcance 22),
`CLAUDE.md` (operativa de despliegue con auto-refresh apagado).)*

## 0. Veredicto: FINALISTA, con la cifra corregida (6, no 8) y la puerta cambiada

Desde esta lente es, con «El Recibo» y «Días sin manos», la dirección con menos playtest infinito del
panel: cero temporadas, cero eventos, cero biblioteca (15-20 situaciones y sus mutantes), cero balance
de economía (hay materia y geometría), cero contingencias ante lo que construya el jugador (las leyes
lo ejecutan, el sello lo mide, el validador lo acota) y una regresión automática por construcción. Y
tiene la respuesta más barata del panel a la frustración, que es la fuente de playtest más cara de
«Sin Manos»: como tocar nunca se prohíbe, no hay castigo que afinar.

Lo que esconde está en tres sitios, todos acotados. (1) Los **relojes**: contar días exige procesos
que cambien de estado a escala de días, y hoy hay dos medidos (tolva 7,8 días, carbonera ~5, los dos
monótonos y de un solo uso), dos narrados sin medir (labio que se colmata, sumidero que se ciega) y
tres pins eternos; si los relojes no existen, o solo existen en una banda estrecha de parámetros,
«DÍA N» sale de afinar `ColmatacionPct`, `TurbidezFuente` e `Infiltracion` por situación, que es balance
con otro nombre. (2) La **campaña**: cada situación lleva un registro del autor que hoy solo puede
producir una persona y que caduca con cada paquete de física. (3) La **presentación** (franja, scrub,
bifurcar, diff, bandas, Piel): incomprimible y en el camino crítico, porque la única prueba de «examen
o juego» exige tenerla en pantalla. Nada de eso es infinito; casi todo se convierte en banco con las
nueve medidas de §3. Por eso finalista y no segunda ronda: la prueba que decide si los relojes existen
cuesta 2,5 semanas, no necesita el sello y puede matarla de verdad.

## 1. Riesgo mayor desde esta lente

**Que los «días» los fabrique un humano.** Contar días solo cobra algo si hay procesos mortales a esa
escala. Contando en el código:

- **Dos relojes medidos, los dos monótonos.** La tolva: 13 989 ticks por encima de 150 raw (r137) =
  7,8 días de 1 800 ticks. La carbonera del arco largo: 30 008 unidades y 122 carbones «en los primeros
  cinco minutos» (R148) = 5 días, y después «sin actividad». Son relojes de combustible: cuentan hacia
  abajo una vez y no vuelven; el DÍA N que producen es «cuánto combustible pusiste», una columna, no
  una apuesta.
- **Tres pins eternos.** `LabHogar` pinta 170 raw sin combustible, `LabManantial` emite 24 celdas/s
  para siempre, el núcleo frío no se agota. Toda situación sostenida por ellos («la piedra fría», el
  horno, el alambique con caldera) tiene DÍA ∞ o DÍA 0: el alambique de r141, con la caldera como
  registro (1 125 toques, el último en el último tick), rinde DÍA 0 por su propia métrica. El remedio
  más barato del catálogo, D («el hogar come», 0,5-1 semana, tuning 8), no está en §5 de la dirección.
- **Los dos relojes de la intro están narrados, no medidos, y el código los matiza.** (a) El sumidero
  «cegado a medias» por sedimento: el depósito exige `carga ≥ 200` y `reposo ≥ 24` visitas sobre un
  fondo (:466; `EsFondo` incluye al sumidero, :98), pero `LabTragar` vacía los cuatro vecinos líquidos
  del sumidero en cada visita, cada 8 ticks (:205, :1023), así que ninguna celda de agua encima del
  sumidero llega a 24 visitas: **el agua sobre un sumidero nunca deposita**. El cegado solo puede
  llegar por sedimento depositado al lado que, como polvo, resbale al hueco. Plausible, y sin una sola
  medida. (b) El labio: la narración pone «tres celdas de roca suelta» (toques 2-4) y once días después
  «la grava del labio se colmata»; la roca suelta va a `LabRoca` (:229-232), no es porosa y no se
  colmata nunca; solo la grava, que el jugador no puede «poner» (sale de tallar). Y la grava lleva un
  rótulo («el agua pasa rápido y apenas se colmata», :210) que el código contradice: `LabInfiltrarHacia`
  atrapa `rate × carga × ColmatacionPct` finos por visita con `rate ∝ perm × libre²`, así que a igual
  `libre` la grava (perm 90) atrapa 7,5 veces más finos por visita que el sedimento (perm 12); una
  promesa sin línea (R49). Por esa aritmética, un labio con agua de carga 40 encima pierde la mitad de
  su paso en ~140 visitas (menos de un día) y el 90 % en ~1 300 (unos 6 días); con agua decantada
  encima, más despacio. Es decir: **el reloj del labio existe plausiblemente a escala de días, pero es
  lineal en `TurbidezFuente`, `ColmatacionPct` e `Infiltracion`**: mover uno un 25 % mueve el día un
  25 %. Un número que se mueve linealmente con tres parámetros es un mando, y el mando lo girará alguien
  por situación hasta que «DÍA 14» quede bonito.

Consecuencia: si la tabla de relojes de §3.1 sale con menos de dos relojes que vuelvan o se repitan
entre 3 y 100 días, la tensión de SELLAR no la da una ley; la da el tuning por situación, que es
exactamente lo que la función objetivo penaliza aunque venga envuelto en banco.

## 2. Iteración humana oculta, pieza a pieza

| pieza | declarado | real | por qué |
|---|---|---|---|
| Campaña de 15-20 situaciones | «montajes de 10-15 líneas», validadas sin personas | 10-18 días de Cesar en el primer lote; 1-2 días por paquete de física después | El montaje es barato (`MontarHorno` son 15 líneas). Lo caro es el **registro del autor** (hoy solo lo produce una persona jugando con el diario o escribiéndolo como la caldera), la condición (umbral y horizonte: dos números por situación) y jugarla una vez para saber si **enseña**: el validador dice no-trivial y cumplible, no legible (la refutación de la cuna: «lo caro es juzgar si una cámara enseña algo»). Y el registro caduca con cada cambio de física (21 rondas en 3 días): el hash de versión lista los rotos, no los arregla |
| Semántica de las cláusulas | no aparece | 2-3 días + una vuelta por ley nueva | ¿Instantánea al cierre del día, mínimo, media, mayoría? ¿Con histéresis? R148 mide la humedad del lecho oscilando 43-125 y R150 50-99 alrededor del mínimo 60: «campo ≥ v» y «planta viva» parpadean varias veces por día y con ellas «el último día en que todas se cumplieron». Decisión por tipo de cláusula, transversal; ninguna ley la toma |
| `DiaTicks` y horizonte | «un número, una o dos sesiones» | un número acoplado a todo + 15-20 horizontes | Todo umbral «por día» y todo reloj cambian de significado con él. Derivable del banco (§3.4); si no se deriva, es gusto por situación |
| Economía de «poner» | «poner (roca suelta, arcilla, núcleo frío, brasa)», sin inventario | 1 día de diseño + 1 sesión, ANTES del barrido de §4 | Si poner es crear materia gratis, cualquier condición se cumple pintando (un núcleo frío más, un labio más) y la única fricción es la columna «toques», que la dominancia no obliga a mirar. Si poner es solo lo que el frasco o el cincel sacaron (R60), el juego es geometría y transporte. El crítico de huecos lo señaló; la dirección no lo fija, y el barrido de §4 mide un juego distinto según la respuesta |
| Relojes | «todo lo físico ya está» | 2 medidos (monótonos), 2 sin medir, 3 pins; D fuera | §1 |
| Presentación (franja, scrub por volcados, bifurcar, diff, bandas, Piel) | 1,5 sem de Opus, «la única parte que no es banco» | 2,5-3 sem de Opus + 3-4 tardes con desconocidos | Cinco vistas nuevas; «qué muestra el diff» (con `Caudal` 24 el agua lo escribe entero: diff solo sobre sólidos) y «cómo se ve que bifurqué en el toque 5 del día 11» son decisiones de UI, el punto donde murieron Clockwork Empires y Maia (`03 §3`). Más el render mínimo (1-2 sem) o las sesiones miden F8 |
| Examen o juego | 3 sesiones binarias | 3 sesiones + 1-2 de seguimiento, **solo con scrub y bifurcado en pantalla** | La defensa de la dirección es el bifurcado; su prueba exige tenerlo construido: semana 8-9, no 7. La variante del crítico purista (bifurcar crea una cámara hermana; la viva no se reescribe) cuesta lo mismo y hace la sesión más medible (continúan / rebobinan / abandonan) |
| Orden de la campaña por banderas apagadas | 0,5 sem dentro de la cuna-lite; «unas veinte» banderas | 1-1,5 sem de Opus + 10-20 h de banco por pasada | `LabParams` tiene TRES banderas (`TermicaPropia` :110, `PresionActiva` :117, `CuerposActivos` :148); evaporación, infiltración, decantación, cocción y combustión viven dentro de `LabAgua`/`LabPoroso`/`ProcessFire`. Y es un informe que Cesar lee para ordenar, no un ordenador (apagar «agua» mata todo; apagar «luz» nada sin huerto) |
| Banco «una noche» | 3 M ticks, dos horas | ~1 h por situación en serie; 20 situaciones ≈ 20 h; con mutantes, el doble | Vacío + autor + 12 cortes + 15 banderas + ~10 mutantes ≈ 39 corridas × 54 000 ticks × 1,7 ms. `LabParams` es estático: en serie salvo player headless con `-bench` (el mismo día que prueba IL2CPP) |
| Relevo y liga por fichero | «casi gratis por determinismo» | comportamiento humano específico: dos personas con la misma versión y una carpeta; IL2CPP 1 día sin hacer | «Ana bifurcó tu registro» es la primera frase de la sesión y no ocurrirá sin dos personas. En solitario la única vara es el autor y la dominancia por columnas no evita el examen |
| El bucle de despliegue | no se cuenta | 1 acción de Cesar por ronda (auto-refresh apagado, `ca_playtestNN.cmd`): 40-60 rondas en 12 semanas ≈ 20-30 h | No es el cuello de botella, pero «técnico y barato» no significa «sin Cesar» |
| Q16 | 0,2 sem | correcto | decide si entran situaciones con planta, no si vive la dirección |

**Lo que no esconde, y hay que decirlo:** temporadas cero, eventos cero, biblioteca grande cero,
balance de economía cero, geometría arbitraria a balancear cero (el recibo es relativo al nacimiento y
el validador poda), contingencias cero, castigo por tocar cero. La regresión tras cada cambio de física
es automática por construcción.

**Suma:** 22-32 días de personas en los tres primeros meses frente a los ~6 declarados; con §3, 12-18.
**Iteración humana real: 6, no 8.** Es la cifra de todo el panel según `03 §2`; aquí lo que la baja es
contenido pequeño y acotado más una premisa sin medir, no balance de un espacio infinito.

## 3. Qué se automatiza en su lugar (y la dirección no propone)

1. **Tabla de relojes, primera noche de banco.** Seis montajes a 100 días (180 000 ticks): labio de
   grava a caudal del manantial con turbidez 40; sumidero bajo poza turbia (96×64,
   manantial-canal-poza-sumidero, la intro tal cual); presa de arcilla; tolva; carbonera con salida;
   alambique alimentado por manantial. Salida: el día en que cada uno cambia de estado y si vuelve. Es
   la existencia de la tensión, medida y no sentida.
2. **Sensibilidad de cada reloj a sus parámetros.** El mismo montaje a ±25 % de `ColmatacionPct`,
   `TurbidezFuente`, `Infiltracion` y `Caudal`. Si el día se mueve más del 50 % con un parámetro, ese
   reloj es un mando y hay que decidir ahora si se congela con las constantes del laboratorio (y se
   acepta el día que salga) o no entra en la campaña. Es la medida que separa «ley» de «balance» antes
   de que nadie afine nada.
3. **El registro del autor como guion, no como partida.** Intervenciones tick-estampadas escritas en el
   montaje (la caldera ya lo es: `Correr` :299-311, desacoplada de `esAlambique`), con 5-6 macroverbos
   paramétricos (canal, tapón, labio, boca ±n, serpentín a altura h, pila de n). Así un paquete de
   física **re-corre** los 20 registros en una noche y solo hay que mirar los que cambian de veredicto;
   hoy habría que re-jugarlos. Convierte 1-2 días de persona por paquete en 1-2 horas.
4. **Horizonte y `DiaTicks` derivados.** H = 1,5 × el día en que el registro del autor deja de cumplir
   sin mantenimiento; si no falla en 60 días, la situación no tiene apuesta, H = 3 y se etiqueta «de
   aprendizaje» (no se espera bifurcado). `DiaTicks` = el día tal que la mediana de los relojes de la
   campaña cae entre 10 y 30 días. Elimina 15-20 números humanos y una sesión.
5. **Agregación de cláusulas por estabilidad.** Para cada cláusula, las cuatro agregaciones (cierre,
   mínimo, media, mayoría) con jitter de ±100 ticks en el sello; se queda la que no mueve «DÍA N»; el
   umbral, el que maximiza la varianza de veredictos entre los mutantes. Parpadeo por día contado
   gratis con `ArcoMuestra`.
6. **Día de decisión.** Desde el día d, 8 cortes de una celda; el primer d en que todos los veredictos
   coinciden. Proxy de «hay algo que bifurcar», sin personas.
7. **Ganancia por bifurcar, en banco.** Un escalador de colina con presupuesto de k bifurcados de un
   toque sobre el registro del autor. Si con k = 3 iguala o supera al autor en todas las columnas en
   la mitad de las situaciones, el espacio es enumerable y las sesiones dirán «puzzle con deshacer»
   antes de hacerse. Es la parte de «examen o juego» que sí se mide sin ojos.
8. **Economía de verbos como regla de conservación del banco.** Poner solo lo que el frasco o el cincel
   sacaron (R60) y presupuesto de toques = 1,5 × los del autor; el validador corre con esa mano. Una
   decisión de un día tomada antes del barrido, y luego cero tuning.
9. **D en el núcleo** (media semana): el reloj más barato; sin él, la mitad de las situaciones son
   planas. **IL2CPP contra los 63 hashes y player headless con `-bench`**: un día, antes de escribir
   «pásale el fichero a un amigo» en la frase.

## 4. La prueba más barata que la mata

La suya de §8 (cuatro montajes × vacío, autor, doce cortes × 30 días) es necesaria y, escrita así,
**solo puede aprobarla**: los registros «del autor» son el aparato del montaje (sin caldera no hay
alambique), y que el aparato domine a su ausencia no prueba que las leyes juzguen, prueba que una
intervención cambia una salida. Coincido con las dos críticas hermanas en eso; desde esta lente la
puerta es otra y cabe en el mismo banco:

1. **Tabla de relojes** (§3.1): muere si menos de dos relojes caen entre 3 y 100 días **y vuelven o se
   repiten** (la tolva sola no cuenta: es un cronómetro).
2. **Sensibilidad** (§3.2): muere si todos los relojes que caen en 3-100 días se mueven más del 50 % con
   un solo parámetro a ±25 %. Entonces los días son un mando y la campaña es balance por situación.
3. **Día de decisión** (§3.6): muere si es ≤ 3 en tres de los cuatro montajes de la dirección.
4. **Parpadeo por cláusula** sin histéresis: muere si alguna cláusula cambia más de una vez por día (el
   «DÍA N» del cartel sería un número afinado a mano).

Más su criterio original (dominancia ≥ 2 en alguna columna, o el vacío ya cumple). Coste: balanza
mínima (1 semana, en paralelo) + intervención genérica en `Correr` (1 día) + montajes variantes (2
días) + evaluador desechable de cláusulas sobre `ArcoMuestra` (2-3 días) + dos noches de banco (~15 M
ticks en serie) + 2 días de leer tablas: **2,5 semanas, sin sello.** La segunda prueba, con personas,
es la suya (mediana de bifurcados voluntarios cero = examen) con las tres cuentas del purista
(continúan / rebobinan / abandonan), y solo vale con scrub y bifurcado en pantalla: semana 8-9.

## 5. Tiempos corregidos

- **Hasta evidencia para matarla: 2,5 semanas** de banco (§4), técnico y sin personas. La tesis
  emocional (bifurcar convierte el examen en juego) no se mata antes de la **semana 9**.
- **Hasta prototipo feo que permita juzgar el core: 8-9 semanas de calendario, no 7.** Opus: 12-14
  semanas en total (las 10,5-12 declaradas más D, las banderas a 1-1,5, el evaluador de cláusulas, la
  regla de verbos y el gesto a 2,5-3), paralelizables de dos en dos. Camino crítico: diario bajo las
  seis puertas y volcado (3-4) → volcados diarios, scrub y bifurcar (1,5-2; está en el camino crítico
  porque sin él la sesión no puede matar) → cuna-lite (1,5-2) → tres situaciones envejecidas con guion
  de autor (0,5). Paralelo: balanza, bandas, Piel, anillo, relojes, sensibilidad, D, IL2CPP, player
  headless.
- **Iteración humana: 22-32 días de personas** hasta juzgar el core; con §3, **12-18**. Después, 1-2 h
  por paquete de física con el registro como guion (1-2 días sin él) y una sesión por ley nueva.
- Técnico automatizable y paralelizable: todo J, balanza, bandas, Piel, anillo, evaluador, relojes,
  sensibilidad, mutantes, D, IL2CPP. Secuencial: diario → volcado → `CorrerSello` → validador →
  situaciones → sesiones. Incomprimible: láminas, tres o cuatro sesiones, leer tablas, y una decisión de
  diseño (los verbos) que hay que tomar antes de medir.

## 6. Órganos a conservar si se descarta

- **El registro editable como respuesta a «examen o juego»** (tocar nunca se prohíbe; solo mueve el
  último toque). Con o sin la variante de cámara hermana, es el único órgano del panel que quita el
  castigo del playtest.
- **La condición como dato + validador por veredicto**, con relojes, sensibilidad, día de decisión,
  parpadeo y ganancia por bifurcar como su acotación real.
- **El registro del autor como guion de macroverbos**, para que la campaña se re-corra y no se re-juegue.
- **La balanza mínima con bit entregable** (carbón, ceniza, semilla; tragar sólido solo desde arriba).
- **Envejecer por simulación como intro sin texto**, con la tabla de relojes como su ficha.
- **El recibo como vector por dominancia** y `HashMat` final como detector de copias; **la vista de
  diferencia** solo sobre sólidos.
- **La Piel** y la brasa viva en el frasco; **el anillo de hashes** y el hash de versión de física en el
  fichero; **la cámara libre** como situación sin condición.

## 7. Puntuaciones (rúbrica v2, 1-10)

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | J no añade física; el bifurcado es metadato; el apalancamiento real depende de relojes que no existen y deja fuera D |
| las leyes ejecutan, revelan y juzgan | 8 | juzgan sí; revelan con bandas, franja y diff por construir; ejecutan dos relojes de cinco, monótonos |
| iteración humana (10 = poca) | 6 | 22-32 días frente a 6; campaña, cláusulas, verbos, láminas, relojes que pueden ser mandos; nada infinito |
| verificabilidad automatizable | 8 | todo en banco menos «examen o juego» y la legibilidad; la prueba de §8 hay que endurecerla para que pueda matar |
| la simulación es el juego | 7 | la condición es la única capa y está validada; «poner» sin precio y los toques en el recibo son dos grietas |
| onboarding garantizable | 6 | el validador caza trivial e imposible; «enseña» lo ve una persona; tres banderas, no veinte |
| observabilidad | 6 | hoy F8 y plantas de un píxel; cinco vistas por construir y validar con láminas |
| tiempo como apuesta | 7 | el bifurcado es la mejor forma de la apuesta del panel; pero apuesta sin reloj no es apuesta, y hoy hay dos cronómetros y tres pins |
| multiplayer emergente | 5 | fichero a mano entre amigos con la misma versión; nada divertido que hacer juntos probado |
| profundidad por leyes estables | 6 | crece por leyes que entran en el recibo sin tocar el juez, cierto; hoy son agua y fuego con dos relojes |
| cuerpo del jugador | 5 | sensor y portador; consecuencias pospuestas, con razón |
| identidad comercial | 7 | la frase de Un Año Después y el cartel de Sin Manos; sin clip co-op; arquetipo 60-150 k base |
| dificultad técnica (10 = fácil) | 8 | acotada, con prueba; lo abierto es la lista de estado del volcado, la memoria del time-lapse, el banco en serie e IL2CPP |

Puertas: iteración humana 6 ≥ 5 y apalancamiento 6 ≥ 5: pasa, sin margen en apalancamiento.

## 8. Crítica razonada

He visto morir sistémicos indie en el playtest infinito, y SELLADO no tiene la forma de esos muertos.
No hay economía que balancear, ni temporadas, ni eventos, ni biblioteca de cuevas, ni contingencias
ante lo que el jugador construya, porque las leyes ejecutan lo que construya, el sello lo mide y un
validador decide sin personas si una situación es trivial o imposible. La regresión tras cada cambio de
física es automática por construcción. Y trae algo que desde esta lente vale más que cualquier ley:
como tocar nunca se prohíbe, no hay castigo que afinar. La frustración es la fuente de playtest más
cara de «Sin Manos», y aquí no existe.

Lo que critico es lo que declara barato y no lo es, y una premisa que la única medida larga del
laboratorio no sostiene. Empiezo por la premisa. Contar días solo cobra algo si hay procesos que
cambien de estado a escala de días. En el código hay dos relojes medidos, la tolva (7,8 días) y la
carbonera (5), y los dos son cronómetros de combustible: cuentan hacia abajo una vez y no vuelven; el
DÍA N que producen es «cuánto combustible pusiste». Hay tres pins eternos (hogar, manantial, núcleo
frío) que dan DÍA ∞ o DÍA 0 a todo lo que sostienen, y la dirección deja fuera el remedio más barato del
catálogo, «el hogar come». Y los dos relojes de la intro están narrados: el agua sobre un sumidero nunca
deposita, porque `LabTragar` la vacía cada ocho ticks y el depósito pide veinticuatro visitas de
quietud, así que el cegado solo puede llegar por polvo que resbala desde al lado, plausible y sin una
medida; y el labio es de «roca suelta», que no es porosa y no se colmata, mientras que la grava que sí
se colmata no se puede «poner». Por la aritmética de `LabInfiltrarHacia`, un labio de grava con agua
turbia encima pierde el 90 % de su paso en unos seis días: el reloj existe plausiblemente, pero es
lineal en tres parámetros. Un día que se mueve un 25 % cuando un parámetro se mueve un 25 % es un mando,
y el mando lo girará alguien por situación hasta que «DÍA 14» quede bonito. Eso es balance con otro
nombre, y es lo que esta lente tiene que penalizar aunque venga envuelto en banco.

Después, lo que esconde. La campaña no son «montajes de 10-15 líneas»: cada situación es montaje,
envejecimiento, umbral y horizonte, un registro del autor que hoy solo produce una persona, jugarla una
vez para saber si enseña (el validador dice no-trivial y cumplible, no legible) y volver a jugarla cuando
un paquete de física la rompa; el hash de versión lista los rotos, no los arregla. La semántica de las
cláusulas no aparece: con la humedad del lecho oscilando 43-125 alrededor del mínimo 60, «campo ≥ v»
parpadea varias veces por día y con ella el «último día en que todas se cumplieron». La economía de
«poner» no está definida: si poner es crear materia gratis, cualquier condición se cumple pintando y la
única fricción es una columna que la dominancia no obliga a mirar. El orden «sin diseñador» por
banderas apagadas cuenta con veinte banderas y `LabParams` tiene tres. Y la prueba de «examen o juego»
solo puede matar con el scrub y el bifurcado en pantalla, que la dirección pone en paralelo y en
realidad están en el camino crítico: semana 8-9, no 7. Son 22-32 días de personas frente a los seis
declarados.

Casi todo eso se convierte en banco, y es lo que la dirección no propone: la tabla de relojes como
primera noche; la sensibilidad de cada reloj a sus parámetros, que es la medida que separa ley de
balance antes de que nadie afine nada; el registro del autor como guion de macroverbos, para que un
paquete de física re-corra la campaña en una noche en vez de re-jugarla; el horizonte y `DiaTicks`
derivados; la agregación de cláusulas por estabilidad bajo jitter; el día de decisión con ocho cortes
como proxy de «hay algo que bifurcar»; un escalador de colina con tres bifurcados como proxy de «puzzle
con deshacer»; la regla de verbos como conservación del banco; D por media semana; IL2CPP en un día.
Con eso la iteración humana baja a 12-18 días y lo que queda es incomprimible pero corto: láminas, tres
o cuatro sesiones, leer tablas, y una decisión de diseño que hay que tomar antes de medir.

Veredicto: finalista, con la cifra corregida a 6 y la puerta cambiada. Su prueba de §8, tal como está,
solo puede aprobarla: que el aparato domine a su ausencia prueba que una intervención cambia una salida,
no que las leyes juzguen. Con relojes, sensibilidad, día de decisión y parpadeo en el mismo banco cuesta
dos semanas y media sin sello y puede matarla de verdad. Si pasa, es la forma del paquete J con menos
contenido de autor y la única con una defensa contra el examen que no hay que balancear; si los relojes
no existen o son mandos, lo que muere es «SELLAR como apuesta», y sus órganos se los queda cualquier
dirección que sobreviva. Mi veredicto no cambia con la variante del purista (bifurcar crea una cámara
hermana): cuesta lo mismo y hace la sesión más medible.
