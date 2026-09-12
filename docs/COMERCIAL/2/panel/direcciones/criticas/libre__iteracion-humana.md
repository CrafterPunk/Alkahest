# CRÍTICA · «A QUE SÍ» (libre, pronóstico) · lente: ITERACIÓN HUMANA OCULTA

*(Panel de direcciones, segunda pasada, 2026-09-12, segunda lectura. Crítico: productor que ha visto
morir sistémicos indie en el playtest infinito. Único tema: cuánto playtest, tuning, balance, contenido
de autor y contingencia ante construcciones arbitrarias esconde la dirección, y qué parte de eso se
sustituye por banco headless, hashes, generación y validación automática. Leído entero: `libre.md`,
`01_LEYES.md`, `00_ENCARGO_Y_CRITERIO.md`, `03_MERCADO.md`, las otras dos críticas de esta dirección
(ingeniería y purista), la crítica de iteración humana de `leyes-juzgan` (la dirección hermana), y lo
que `02_DIRECCIONES.md` y `03_COMPARATIVA_Y_TIEMPOS.md` ya sintetizaron de la primera lectura. Código:
`LabBench.cs` entero (`Escenarios` :76-87, `MontarAlambique` :105-109, `MontarCarbonera` :135-142,
`Correr` :265-345 con `RestaurarDefaults` y la caldera cableada al nombre, `ArcoAvanzar`/`ArcoMuestra`);
`SimStepper.cs` (la lengua de `Fire` con `combustLenguaPct` :876-884; el dado del carbón
`RendimientoCarbonPct` 25 con `XorShift.FromCell` :905-935); `SimStepper.Laboratorio.cs` (`LabGotear`
:317-327 nace el agua a la temperatura de la superficie; germinación que lee `luz[i+W]` :702-707);
`Universe.Laboratorio.cs` (`freezesAt` = 60 raw :211; la fibra flota, densidad 60 frente a 110 :80,
:210); `LabParams.cs` (95 parámetros estáticos, `Registro`); las cinco puertas de `AlkahestSim.cs`
(`Paint` :571, `PaintCell` :610, `PaintStable` :741, `PaintRect` :774, `PaintLab` :831); `LabPanel.cs`
(pincel de radio 0-8 sobre el catálogo entero :195-247); `Cincel.cs` (radio 2, 3 celdas por tick,
alcance 2,2; el clic derecho construye piedra); `Flask.cs` (900 celdas, aspira 30 por tick, alcance 6);
`Laboratorio/benchmarks/2026-09-06_2149_banco.md` (alambique 1,96 ms/tick, carbonera 1,57).)*

## 0. Veredicto en una línea

**Descartar como dirección; conservar sus órganos, y correr de todos modos su prueba de banco, porque
es el validador que F1 necesita.** No la descarto por días de playtest (aunque son 35-55 de personas en
tres meses, contra las «tres o cuatro tardes» declaradas): la descarto por la *naturaleza* de lo que
esconde. En El Recibo lo oculto era contenido pequeño y acotado (doce a veinte números de autor). Aquí
lo oculto es un **prior de autor sobre el espacio infinito de construcciones del jugador**: la gramática
de los monos decide todas las cuotas, cada ×17 fiable que encuentre un jugador exige enseñarle ese truco
a los monos, y cada verbo nuevo del jugador exige una gramática nueva. Eso es balancear un espacio
casi infinito truco a truco, que es lo que la función objetivo penaliza «con fuerza» y lo que ha matado
a más sistémicos que ninguna otra cosa. Las tres definiciones de cuota que quitan al humano del bucle
existen y se miden en el mismo arnés (§7), pero las dos que sobreviven al análisis cambian la frase
del juego y se disuelven en órganos que F1 ya tiene (la firma por ablación como precio; la apuesta de
una frase). Lo que sobrevive es mejor que la dirección: la banca como cifra de gradación, el visor sin
manos, el visor de posibilidad, el torneo de estrategias, la cuota justa y el sobre cerrado.

## 1. Lo que declara y lo que esconde

| partida | declarado | corregido | por qué (con el código delante) |
|---|---|---|---|
| balance | «pesos que no los puso nadie; los produjo la física contra la ignorancia» | pesos que puso **la gramática del prior**, más siete mandos globales (K, tamaños, colocación, suelo de Laplace, techo, castigo por fallo, tope de fantasmas) y **tres números por situación** (presupuesto por familia, tope de fantasmas, horizonte en días) | la cuota `1/p` mide cuántas veces un constructor al azar produjo esa familia AHÍ, no cuánto costaba saberlo. Todo lo que la gramática no hace nunca vale ×17: es el prior, no la física, quien fija el techo. §2 |
| contingencias ante construcciones arbitrarias | «una celda tocada por tu propio registro anula el fantasma»; presupuesto y tope de fantasmas «acotan» la degeneración | una **política sobre el espacio infinito** que se parchea truco a truco: zanja de 40 al hueco lejano, verter 900 celdas de agua que la gramática no vierte, horno → vidrio, frío en el rincón | cada verbo que los monos no tienen es una mina de ×17; cada verbo que entra después (la brasa en el frasco de C, el hielo transportable de F) es una gramática nueva. §2 |
| contenido de autor | 10-15 montajes de 10-15 líneas + 4 situaciones de mano | 14-19 × (montaje + R0 + **presupuesto** + horizonte + **registro del autor dentro del presupuesto** + juzgar si el mapa enseña) + caducidad con L, V, A, F | la refutación de la cuna ya lo dijo: lo caro no es escribir la cámara, es juzgar si enseña; «entropía × familias en cuota media» es una cifra, no una pedagogía. §5 |
| semántica del fantasma | «material en (x,y) == M el día D» (la cláusula de J) | una decisión de diseño (celda o región; tick de cierre o máscara vista; familias estables o transitorias) que **cambia todas las cuotas** | `Fire` es una lengua con `combustLenguaPct` 35 por paso; el carbón es un dado al 25 % por celda; la gota del serpentín nace a la temperatura de la superficie y se hiela a 60 raw. A un tick, tres de las familias del tutorial son monedas. §4 |
| playtest | «tres o cuatro tardes, no meses» + 2 días de papel | **35-55 días de personas en tres meses** tal como está escrita; 17-26 con las sustituciones de §7 | §9; y una tarde por cada definición de cuota que se pruebe, porque cada una cambia la sensación |
| prueba que la mata | mapa binario o autor que no cobra; 3-4 días | pasa en falso por tres razones: el registro del autor ya está en el montaje (es R0), el presupuesto es asimétrico (5 de frío contra 31; 3 cortes contra 40), y la banda media aparece sola por dados y bordes | los dos críticos hermanos lo vieron; lo firmo y añado el torneo de estrategias y la regla de puntuación como variables. §8 |
| hora 5-20 | cueva larga con pronóstico semanal, retos, cinco cuevas hermanas | cuelga de una **no estacionariedad que el sustrato no tiene** («nada produce sin volver a tocarlo»; tres dones infinitos) y de una población | sin D, A, V o F el mundo de R0 llega a régimen y todo fantasma vale ×1 o ×17; con K = 4 hay cinco niveles de p. §6 |
| cómputo | «paralelo entre procesos» | builds headless de Unity (`Universe.Create` usa `Mathf` y `Color32`), o hilos en proceso con un día de banco que pruebe hashes iguales; cotizar «en segundo plano» dentro del cliente son 3-5 días no contados | `LabParams` es estático; el `grep` del director técnico no encuentra otro estático mutable en `SimStepper*.cs` |

## 2. El prior es el balance: la cinta de correr de los monos

La banca cotiza contra dieciséis registros «de quien no sabe nada»: rectángulos de 1×1 a 6×6 de cada
material del presupuesto en sitios sorteados y tres cortes al azar, todo en el tick 0. La cuota de una
celda es `1/p` contra ESE prior. Un novato no hace eso (pone yesca junto al hogar y agua junto a la
planta), pero el problema no es el realismo del prior: es que **el prior es un objeto de autor con
mantenimiento**, y su mantenimiento es exactamente el trabajo que la función objetivo prohíbe.

**Los verbos del jugador no caben en la gramática.** Las cinco puertas de `AlkahestSim` (`Paint`,
`PaintCell`, `PaintStable`, `PaintRect`, `PaintLab`) admiten cualquier byte en cualquier celda; el
pincel de `LabPanel` pinta cualquiera de los materiales del catálogo con radio hasta 8 (unas 200 celdas
por clic); el cincel construye piedra con el clic derecho; el frasco vierte 900 celdas de agua. El
presupuesto de ejemplo {5 frío, 20 terracota, 30 yesca, 40 cortes} no contiene agua. Si el jugador
puede verter y los monos no vierten, todo lo que ocurra aguas abajo del vertido vale ×17. La regla
general es esta: **cada verbo que los monos no tienen es una mina de ×17, y cada verbo que se añada
después (la brasa viva en el frasco de C, el hielo transportable de F, la siega de V) es una decisión
nueva de gramática**. No es una lista que se cierre: crece con el sustrato, que es justo lo que la
dirección promete que crezca.

**Los trucos son estructurales y el techo los iguala.** El director técnico describió la zanja de
cuarenta cortes desde la poza al hueco lejano (las celdas de la zanja quedan anuladas; el hueco no está
tocado; agua en el hueco = ×17 × N fantasmas, con el conocimiento de que el agua baja). Añado dos que
el documento presenta como ajás: llevar el frío al rincón y pintar agua debajo (minuto 2-4: «×17,
primera vez que una decisión tuya cambió el precio») y el horno → vidrio que el propio §6 llama
degeneración. Los tres pagan el máximo. El tope ×17 no distingue el truco de la hazaña, y las
mitigaciones que ofrece (presupuesto, tope de fantasmas, «nivel medio») no separan al que lee del que
repite: los dos llenan sus ocho fantasmas de primer orden. Cuando un jugador encuentre un ×17 fiable,
la única cura dentro de esta definición es **enseñar a los monos ese truco** (retocar la gramática),
recotizar todo (automático, una noche) y volver a sentir las cuotas (humano, una tarde). Un parche por
sesión en los tres primeros meses es la estimación honesta, y no converge, porque el espacio es
infinito y el prior es finito.

**Por qué esto es peor que lo que esconde El Recibo.** Allí lo oculto son doce a veinte umbrales de
autor, uno por situación, con un sustituto sin número (tick del primer X). Aquí lo oculto es una
*política* sobre todas las situaciones a la vez: la misma gramática cotiza la carbonera y el alambique,
y cada retoque mueve todos los mapas. Es el cuadrante «técnicamente sencillo, humanamente caro» en su
forma más pura: escribir la gramática cuesta dos días; saber si el mapa resultante «tiene sentido» solo
lo sabe una persona mirando, y lo vuelve a tener que saber tras cada parche.

**Las salidas que quitan al humano del bucle, y lo que cuestan.** (i) **La banca poblacional** (los
nueve montajes del banco y después cada sello publicado como monos): un truco que ya está en la banca
deja de pagar ×17 solo; pero depende de una población (comportamiento humano, penalizado) y solo
existe tras lanzar. (ii) **Mutantes del registro del jugador** (cuota = fragilidad del resultado bajo
±1-2 celdas): sin prior, la zanja con un corte menos no llega (×2), el frío movido dos celdas sigue
condensando (×1): paga precisión, no novedad, y la cuota solo existe tras SOLTAR, así que el tablón deja
de dirigir la construcción. Otra frase, otra dirección. (iii) **Cuota por leyes** (1 + grupos de
`LabParams` cuya perturbación ±20 % cambia el resultado en (x, y, día), sobre el registro del jugador:
diez grupos × 9 000 ticks ≈ 3 minutos en serie, menos de uno con hilos): sin prior, sin explotación
espacial (la zanja no depende de ninguna ley: ×1; la gota del serpentín depende de evaporación, vapor,
condensación y frío: ×5), y escala con «cruzar leyes». Es la versión que pasa esta lente sin
mantenimiento humano; y es, letra por letra, la **firma por ablación** de F1 aplicada por fantasma. La
mejor «A que sí» posible es un órgano de F1.

## 3. La regla de puntuación esconde dos mandos por situación

Cuota `c = 1/p`, acierto paga `c`, fallo resta 1. Con creencia `q` del jugador, `EV = q·c − (1 − q)`.
Para quien sabe exactamente lo que saben los monos (`q = p`): `EV = p ≥ 0`. Consecuencias:

1. **La ignorancia nunca pierde en esperanza.** Rociar fantasmas a la tasa base es EV positivo en
   todas partes; copiar el visor sin manos (fantasmas de ×1) paga 1 seguro por fantasma. Pintar más
   siempre es mejor, así que el **tope de fantasmas por situación** no es un detalle de interfaz: es el
   único freno, y es un número de autor por situación.
2. **La corazonada paga casi como el saber.** Un ×17 con `q = 0,5` vale `EV = 8` contra 17 de la
   certeza; ocho fantasmas así tienen una desviación típica de unos 25 puntos. Si la sensación es
   «casino» y no «yo sabía», es por esta aritmética, y nadie la ha mirado.

La corrección es de una línea y quita dos mandos: **cuota justa**, el acierto paga `c − 1` y el fallo
resta 1. Entonces `EV = q·c − 1`, cero para `q = p`: el ignorante empata en esperanza, el copión de R0
cobra cero, el rociador cobra cero con varianza, y la puntuación mide solo la ventaja sobre el prior. El
tope de fantasmas pasa a ser una cuestión de pantalla, no de balance, y el castigo deja de ser un
parámetro. No resuelve el problema del prior (§2: la zanja sigue pagando 16 por fantasma), pero
convierte «¿es justa la regla?» en algo que un torneo de estrategias decide en una noche (§7.1) en vez
de dos sesiones con personas.

## 4. La gradación que fabrican el dado y el borde

El «nivel medio del mapa, donde se distingue a quien lee de quien repite» es la pieza que sostiene la
apuesta. Con el código delante, el nivel medio lo fabrican tres cosas que no son conocimiento:

- **El dado del carbón.** `RendimientoCarbonPct` 25 con `XorShift.FromCell(_tick, x, y)`
  (SimStepper.cs:921-922): cada celda ahogada es una moneda al 25 %, con un tick de agotamiento que
  cambia de un registro a otro. En `MontarCarbonera` la pila y la boca ya están en el montaje, así que
  R0 y los dieciséis monos sellan la misma pila y `n ~ Binomial(16, 0,25) ≈ 4`: `p ≈ 0,29`, cuota
  **×3-×4 uniforme en todo el interior**. «Carbón, día 1» en cualquier celda de la pila paga ×3,4 al
  que sabe que la pila carboniza y también al que no; la ceniza, ×1,3. El nivel medio de la situación
  insignia del fuego es una lotería por construcción.
- **Las familias transitorias.** `Fire` es solo la lengua visible, escupida con `combustLenguaPct` 35
  por paso de combustión muestreado en la celda vacía de encima (SimStepper.cs:879-883): «fuego el día
  1» evaluado a un tick es una moneda sobre una pila que arde de verdad. El humo vive 255 ticks y
  viaja. La gota del serpentín nace con la temperatura de la superficie fría (`LabGotear` →
  `LabNacerAgua(j, temp[idx])`) y se hiela al tick siguiente (`freezesAt` 60 raw; hecho 4 de `01`):
  bajo el frío, la familia es hielo en vuelo y humedad en el lecho, no `Water`. «Gota», el minuto 0-1
  del tutorial, está escrita contra un alambique que el código contradice.
- **El muestreo.** Con K = 16 la resolución de p es 1/17 y el error binomial en p = 0,5 es ±0,125: la
  misma celda cotiza entre ×1,6 y ×2,7 según la semilla de los monos. Donde el agua decide (R135: una
  celda de labio anega 24 de 48 columnas) el mapa sale moteado, no graduado, y el tablón no se lee como
  tablón. La prueba de lámina al estilo G6 fallaría por una razón de muestreo, no de diseño. K = 64
  cuadruplica el coste (22 minutos por situación en serie).

De ahí que la **semántica del fantasma** (celda contra región 3×3 con umbral; tick de cierre contra
máscara de familias vista durante el día; familias estables contra transitorias) no sea un detalle de
implementación: cambia todas las cuotas y la sensación entera. Tal como está escrita, esa decisión la
descubre el playtest. Se puede llevar al banco (A/B de las cuatro semánticas con gradación estable,
brecha y moteado como métricas) y dejar a la persona elegir entre las que sobreviven: una sesión en vez
de tres.

## 5. Contenido: lo que cuesta cada situación y cuándo caduca

El documento cuenta «diez o quince montajes de 10-15 líneas y las cuatro situaciones de mano». Lo que
cuesta cada situación, partida a partida:

| pieza | quién | coste |
|---|---|---|
| montaje de 10-15 líneas | Opus | minutos |
| corrida R0 y mapa | banco | 4-6 minutos en serie |
| presupuesto por familia | **autor** | decide qué pueden hacer los monos y, por tanto, el mapa entero; bajo, no cabe la máquina; alto, financia la zanja |
| horizonte en días | autor (o derivado: día en que R0 llega a régimen + 2) | un número |
| tope de fantasmas | autor (o cero con la cuota justa) | un número |
| registro del autor **dentro del presupuesto** | **persona jugando** o Opus en código y el banco buscando | el director técnico ya vio que la prueba §8 usa 31 celdas de frío con un presupuesto de 5 |
| ¿el mapa enseña algo? | **persona mirando** | lo caro, según la refutación de la cuna; «entropía» no es pedagogía |
| texto de la situación (las de mano) | autor | tras R0, no antes (hecho 4) |

A 0,5 días por situación, 14-19 situaciones son 7-10 días la primera vez. Y **caducan**: L cambia
«planta viva» de ×17-imposible a cotizable; A cambia «llama viva el día 3 en cuarto cerrado» de ×1 a
×17; F añade hielo; V añade fibra donde la corriente la deja. La recotización es automática (una noche
con alerta de etiqueta), pero el juicio de si la primera hora sigue enseñando, el texto de las de mano
y los registros de autor que dejan de caber en su presupuesto son humanos: 1-2 días por paquete, cuatro
paquetes prometidos. La regla de producción que lo deja en cero es la misma que en El Recibo: **física
primero, contenido último**. Mientras L, A, V y F se muevan, las únicas situaciones son los nueve
montajes del banco, que se recotizan solos.

## 6. Comportamiento humano específico y lo que cuelga de él

- **La cueva larga (hora 20)** promete pronósticos semanales sobre un mundo persistente. Con tres dones
  infinitos (hogar eterno, manantial eterno, núcleo frío) y «nada produce sin volver a tocarlo», el
  mundo de R0 llega a régimen y todo fantasma vale ×1 o ×17 hasta que entren D, A, V o F; y con K = 4
  hay cinco niveles de p. No es una partida de personas: es una dependencia de cuatro paquetes de
  física que el documento no cuenta como prerrequisito.
- **Retos, cinco cuevas hermanas, banca poblacional**: población. Válido como capa opcional después de
  lanzar; no como fuente de profundidad.
- **El sobre cerrado** es el órgano asíncrono más barato del panel, y cuelga de dos cosas sin medir: el
  volcado de J (hecho 9) y el determinismo entre máquinas (hoy probado en un editor; IL2CPP contra los
  hashes del banco es un día; si falla muere el órgano, no la dirección).
- **Preparar con el tiempo parado o corriendo**: la banca cotiza desde el tick 0 con monos que pintan
  en el tick 0; si el jugador prepara tres minutos con el mundo corriendo, sus días no son los del mapa.
  Pausar convierte el bucle en editor más corrida; recotizar al soltar son 4-6 minutos o hilos. Es una
  decisión de diseño con una sesión de sensación detrás que no está en el presupuesto.
- **La cotización en segundo plano dentro del cliente**: `LabParams` es estático; o hilos en proceso
  (un día de banco: cuatro hilos, hashes idénticos a los de serie) o una build headless aparte. 3-5 días
  de Opus no contados.

## 7. Qué se automatiza en su lugar

1. **Torneo de estrategias como validador de la regla y de la definición.** Por situación y por
   definición de cuota (monos, mutantes, leyes) y regla (escrita, justa): R0, el copión de R0, el
   rociador (un fantasma en cada celda físicamente posible), el tonto espacial guionizado (zanja al
   hueco, pila lejos del hogar, frío en el rincón, con fantasmas por regla), K mutantes del registro del
   autor y el autor con presupuesto simétrico. Exigir `autor > mutantes > tonto > rociador ≥ copión ≥
   R0` y tonto < 70 % del autor. Una noche sustituye dos o tres sesiones de «¿se siente justo?».
2. **Cuota justa** (paga `c − 1`): quita el castigo y el tope de fantasmas del balance.
3. **Gradación medida solo sobre familias estables** (agua en reposo, carbón, ceniza, terracota,
   sedimento, planta viva), con exclusión de las celdas donde R0 parpadea dentro del día, y una
   **métrica de moteado** (fracción de celdas de nivel medio cuya mediana de vecinos difiere en más de
   cuatro niveles); A/B de las cuatro semánticas en banco.
4. **Buscador de exploits y solver del autor**: hill-climb de doscientos registros al presupuesto
   maximizando la puntuación; si el óptimo usa menos del 20 % del presupuesto, la situación tiene un
   exploit y se etiqueta antes de que lo encuentre un jugador; el mismo óptimo es el registro de
   referencia, así que Cesar juzga mapas en vez de resolver situaciones.
5. **Presupuesto y horizonte derivados**: presupuesto = 1,5 × el registro más barato que el solver
   encuentra para la familia objetivo; horizonte = día en que el hash de `mat` de R0 deja de cambiar,
   más dos.
6. **Recotización nocturna con alerta de etiqueta** por paquete de física: la lista de situaciones cuya
   ley etiquetadora o entropía cambió más de un umbral.
7. **Cuota por leyes** (ablación por grupo de `LabParams` sobre el registro del jugador) como la única
   definición sin prior; con hilos en proceso verificados por hash (un día).
8. **IL2CPP contra editor** (un día) antes de prometer el sobre cerrado.

Lo que no se automatiza y hay que presupuestar como humano: si pintar el futuro es apostar o rendir
examen; si el pincel de tres ejes (celda, familia, día) se maneja sin menú; si el visor de cuota se lee
en lámina; y si alguien mira el tablón.

## 8. La prueba más barata que la mata (corregida)

Sin diario ni J. Arnés sobre `Correr` con `Montaje` compuesto (base + registro) y muestreo al cierre de
cada día (patrón `ArcoAvanzar`), sobre **«montaje − solución»**: la cámara alta con caldera y **sin**
serpentín; el recinto con hogar y **sin** boca (hoy los dos montajes llevan la solución dentro, así que
«el registro del autor» sería R0). Presupuesto **simétrico** (31 de frío para todos, o 5 para todos, y
40 cortes que los monos gasten en segmentos de 1-6). Máscara de familias vista por día (2 bytes fuera
del hash) y clasificador por banda para agua en poroso. Cinco días de 1 800 ticks. Los fantasmas del
autor se pintan **antes** de correr, por regla («los productos para los que se diseñó el montaje»).
Tres definiciones de cuota (monos, mutantes, leyes) × dos reglas (escrita, justa) × las estrategias del
torneo de §7.1.

| criterio | la mata si |
|---|---|
| (a) gradación estable | < 10 % de las celdas de aire de la región en ×2-×8 para todas las familias estables, excluidas las celdas donde R0 parpadea |
| (b) el autor cobra | < 3 fantasmas de ≥ ×4 con presupuesto simétrico |
| (c) explotabilidad | tonto espacial ≥ 70 % de la puntuación del autor con el mismo número de fantasmas |
| (d) moteado | > 30 % de las celdas de nivel medio |
| (e) origen de la paga | ≥ 50 % de la paga del autor viene de celdas cercadas por su propio registro o de familias transitorias |

**Qué la mata como dirección:** la definición de monos falla (c) o (e) en cualquiera de los dos
montajes (el prior es el juego, y su mantenimiento es el playtest infinito), y ninguna otra definición
pasa las cinco. **Qué la resucita:** una definición pasa las cinco en los dos montajes; entonces se
escribe una dirección nueva con la frase de esa definición («según lo exacto que tenías que ser» o
«según cuántas leyes cruzaba tu acierto») y se juzga de nuevo. Coste: **6-8 días de Opus** (arnés,
gramáticas, máscara, clasificador, cinco métricas, torneo, tonto guionizado, cercado/abierto) + unos 40
minutos de banco por definición + un día de Fable y Cesar leyendo mapas: **dos semanas**, sin
dependencias, y el arnés es el validador por gradación que F1 quiere de todos modos.

Segunda prueba, humana, dos días, solo si la primera resucita algo: las cuatro situaciones de mano en
papel con el mapa impreso, cinco personas: ¿pintan donde el tablón paga o donde ya saben? Sobre
examen o apuesta ninguna de las dos dice nada; eso cuesta el prototipo feo.

## 9. Tiempos corregidos

**Automatizable y paralelizable (Opus, con prueba de banco por pieza):**

| pieza | semanas nominales | hilo |
|---|---|---|
| arnés de §8 (montaje − solución, gramáticas simétricas, máscara por día, clasificador, cinco métricas, torneo) | 1-1,2 | 1 |
| hilos en proceso con hashes iguales + IL2CPP contra editor | 0,4 | 2 |
| fantasmas y pincel de tres ejes sobre un stub | 1-1,5 | 2 |
| visores de cuota y sin manos; recibo compartible | 0,9 | 2 |
| A/B de semánticas y definiciones en banco | 1 | 1 (tras el arnés) |
| escalera (entropía, sensibilidad) | 0,7 | 2 |

**Secuencial (cada pieza necesita la anterior):**

| pieza | semanas nominales |
|---|---|
| J mínimo: diario bajo las cinco puertas + `CorrerSello` + cláusula sobre la grilla (volcado y sobre cerrado fuera del feo) | 2-2,5 |
| banca sobre el arnés: volcado por día, mapa p, presupuesto contado por el diario | 1-1,5 |
| integración: fantasmas evaluados por `CorrerSello`, visores sobre el mapa real | 0,7 |

Total nominal ≈ 9,5-11 semanas de Opus (la dirección dice 8-9 sobre un J de 3,2). Con la calibración de
`03` (lo acotado cabe en un tercio o la mitad del calendario; lo secuencial y lo humano no), **7 semanas
de calendario hasta el prototipo feo** (camino crítico: arnés → J mínimo → banca → integración ≈ 5
semanas; cliente en paralelo; sesiones desde la semana 5). **Evidencia para matarla: 2 semanas** (§8).

**Humano e incomprimible (tres primeros meses, días de personas):**

| partida | tal como está escrita | con las sustituciones de §7 |
|---|---|---|
| elegir definición de cuota y regla, y sentirlas | 5-8 | 2-3 |
| parches al prior por cada exploit encontrado (uno por sesión) | 4-8 | 0-1 |
| semántica del fantasma (celda/región, tick/máscara) | 3-4 | 1 |
| presupuesto, tope y horizonte de 14-19 situaciones | 4-6 | 1-2 |
| registro del autor dentro del presupuesto + juzgar si el mapa enseña | 5-8 | 2-4 |
| re-juzgar tras L, V, A, F | 3-6 | 0-1 |
| texto de las cuatro de mano sobre R0 real (hecho 4) | 3-4 | 2-3 |
| pincel de tres ejes sin menú | 2 | 2 |
| lámina del visor de cuota (G6) | 2 | 2 |
| preparar con tiempo parado o corriendo | 1 | 1 |
| ×10 con fantasmas: tensión o espera | 1-2 | 1 |
| examen o apuesta (la que decide) | 3-5 | 3-5 |
| sobre cerrado con 2-3 personas | 1-2 | 0 (pospuesto) |
| **total** | **37-58** (declarado: 3-4 tardes + 2 días) | **17-26** |

De los 17-26 con sustituciones, unos 12-15 (examen o apuesta, láminas, ×10, preparar) son sesiones
que F1 necesita igual. El coste humano *marginal* del fantasma sobre F1 es de 5-10 días, y lo que
compra a cambio es un marco de puntuación sobre una simulación que no toca. Eso, y no el total, es lo
que decide el veredicto.

## 10. Órganos a conservar si se descarta

1. **La banca como validador continuo**: `p(x, y, familia, día)` como cifra de gradación, explotabilidad
   y moteado; sustituye al «K perturbaciones deben dar veredictos distintos» de la cuna y entra en F1
   sin tocar nada.
2. **El visor sin manos** (R0 por día como fantasmas grises): tutorial sin texto para cualquier
   dirección con SOLTAR; cuesta un volcado por día.
3. **El visor de posibilidad** («qué tiende a hacer la física aquí»): el mejor rayos X del panel,
   útil sin puntuación.
4. **El torneo de estrategias** (R0, copión, rociador, tonto guionizado, mutantes, autor) como validador
   de cualquier regla de puntuación; hoy no existe en ninguna dirección.
5. **La cuota justa** (paga `c − 1`): regla de cualquier apuesta con prior; quita dos mandos.
6. **La cuota por leyes** (ablación por grupo de `LabParams` sobre el registro del jugador): el precio
   de la «apuesta de una frase» de F1, sin prior y sin explotación espacial.
7. **El fantasma como apuesta privada opcional antes de SOLTAR, oculta hasta el veredicto**: información
   asimétrica temporal sin roles, portable a F1 a coste cero de física.
8. **El recibo compartible** (rejilla de aciertos y huecos) y **el sobre cerrado** (fichero del sello
   más fantasmas cifrados con los hashes del emisor): el órgano de Wordle y el asíncrono más barato.
9. **La máscara de familias vista por día** (2 bytes fuera del hash): la semántica honesta de las
   cláusulas transitorias (fuego, humo, gota) para la condición como dato de J.
10. **La escalera por entropía y sensibilidad**: complementa la firma por ablación con «cuánto hay que
    saber».
11. **La densidad de bucle** (diez situaciones por hora, cada una con veredicto): la única respuesta
    concreta del panel al hueco del minuto 15-40, como objetivo de diseño para la campaña de F1.

## 11. Rúbrica v2 (13 ejes, 1-10)

| eje | nota | por qué (desde esta lente) |
|---|---|---|
| apalancamiento sistémico | 6 | una ley de juicio sin física nueva que recotiza todo; pero el prior es un objeto de autor con mantenimiento y el techo ×17 aplana el espacio |
| las leyes ejecutan, revelan y juzgan | 7 | ejecutan y revelan (sin manos, posibilidad); cotizan contra un prior humano, no contra la física sola |
| iteración humana (10 = poca) | 5 | 37-58 días frente a 3-4 tardes; lo oculto es balance de un espacio infinito por proxy; 6-7 con las sustituciones, pero entonces es otra dirección |
| verificabilidad automatizable | 8 | casi todo va a banco, incluidos sus fallos (dado, borde, moteado, exploit); la sensación no |
| la simulación es el juego | 5 | el verbo central no toca una celda; el marcador mide la descripción; preparar debe pausarse |
| onboarding garantizable | 7 | escalera calculada y visor sin manos; «Gota» contradice el hecho 4; las cuatro de mano son reescrituras |
| observabilidad | 8 | dos visores que salen de corridas, los mejores del panel; moteado con K = 16 |
| tiempo como apuesta | 9 | literal; lo que se arriesga es un −1, no la máquina |
| multiplayer emergente | 6 | creencias ocultas sin roles es buena y barata; cueva larga y retos dependen de población; nada construido |
| profundidad por leyes estables | 5 | cada ley recotiza; la hora 5-20 cuelga de D, A, V y F, y el prior se satura al aprenderlo |
| cuerpo del jugador | 2 | cursor; C opcional y tardío |
| identidad comercial | 6 | «×17» y el recibo son clipeables; nicho de puzle 60-150 k; «injusto» es la palabra de reseña que espera al ×17 arbitrario |
| dificultad técnica (10 = fácil) | 7 | acotado; abiertos: semántica, K, cotización en cliente |

Ponderado: 150 de 240. Puertas: iteración humana 5 (en la puerta), apalancamiento 6. El veredicto no
descansa en la puerta sino en §2.

## 12. Veredicto y condiciones

**Descartar como dirección.** Tres razones en orden de peso: (1) lo que esconde es un prior de autor
sobre el espacio infinito de construcciones, con mantenimiento truco a truco y verbo a verbo, que es
la forma exacta del playtest infinito que la función objetivo penaliza con más fuerza; (2) la única
definición de cuota que no necesita prior ni mantenimiento (por leyes) es la firma por ablación de F1
aplicada por fantasma, así que la mejor versión de la dirección ya es un órgano de la finalista; (3) el
coste humano marginal del fantasma sobre F1 (5-10 días) compra un marco de puntuación, no una decisión
dentro de la simulación.

**Condiciones que impongo aunque se descarte:**

1. **Correr la prueba de §8 de todos modos**, dos semanas sin dependencias, porque su arnés es el
   validador por gradación y el torneo de estrategias que F1 necesita, y porque es la única forma de
   saber si «cuota por leyes» o «mutantes» merecen una dirección nueva con su propia frase.
2. **Los órganos 1-9 de §10 entran en la capa J de F1** con su prueba de banco; el fantasma, como
   apuesta privada opcional antes de SOLTAR, oculta hasta el veredicto, sin tablón que dirija la
   construcción.
3. **Ninguna gramática de monos se retoca a mano por situación.** Si un mapa no gradúa, se cambia la
   definición de cuota o la semántica, medidas en banco; nunca se enseña un truco a los monos. Es la
   frase que separa este órgano del playtest infinito.
4. **Física primero, contenido último**, como en El Recibo: nada de mano se escribe antes de L y A.

Si la prueba resucita una definición, la dirección que nazca de ella se juzga de nuevo con esta misma
lente, y la primera pregunta será la de siempre: ¿qué hay que mantener a mano cuando un jugador
encuentra lo que el banco no vio?
