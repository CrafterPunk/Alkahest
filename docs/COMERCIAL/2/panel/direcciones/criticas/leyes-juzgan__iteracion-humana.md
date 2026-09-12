# CRÍTICA · «EL RECIBO» (leyes-juzgan) · lente: ITERACIÓN HUMANA OCULTA

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: productor que ha visto morir sistémicos en
el playtest infinito. Único tema: cuánto playtest, tuning, balance, contenido de autor y contingencia
esconde la dirección, y qué parte de eso se sustituye por banco headless, hashes, generación y
validación automática. Leído entero: `leyes-juzgan.md`, `01_LEYES.md`, `00_ENCARGO_Y_CRITERIO.md`,
`03_MERCADO.md`, la lente `metricas-y-soltar` y sus ocho refutaciones (sello, balanza, cuna, testigo),
`_huecos_y_combinaciones.md` (los verbos), las otras dos críticas de esta dirección. Código:
`LabBench.cs` (`Escenarios`, `MontarCarbonera`, `Correr` y su única intervención cableada al alambique,
`ArcoMuestra`), `SimStepper.Laboratorio.cs` (libro mayor :49-118, `LabSumidero`/`LabTragar`
:1021-1033, `LabRespira`), `SimStepper.cs` :905-935 (el dado del carbón: `RendimientoCarbonPct` 25 por
celda con `XorShift.FromCell`), `LabParams.cs` (90 parámetros en diez grupos), `Cincel.cs` (radio 2,
3 celdas por tick, alcance 2,2), `Flask.cs` (900 celdas, 30 por tick al aspirar), `LabPanel.cs` (pincel
de radio 0-8 sobre el catálogo entero por `PaintStable`/`PaintCell`/`PaintLab`), las cinco puertas de
`AlkahestSim.cs` :571-831.)*

## 0. Veredicto en una línea

**Finalista, con la cifra de iteración corregida al alza y cinco sustituciones que la devuelven a la
baja.** Es la dirección del panel que menos balance esconde, porque no tiene números de balance de un
espacio infinito, ni temporadas, ni eventos, ni biblioteca grande: el recibo mide y la física decide.
Pero «6-8 días de personas en ocho semanas» cuenta solo cuatro playtests y omite cuatro partidas
humanas que el propio documento nombra sin costear: el **registro del autor** que el validador exige por
situación y que **caduca con cada paquete de física** (L, V, M y C están prometidos después del núcleo),
el **número del pedido** (uno por situación: es el único número de balance de la dirección y hay 12-20),
la **economía de verbos** (sin definir; decide si la frontera existe o si el recibo mide cuánto pintaste)
y la **legibilidad del gesto** (bandas, histograma, diff: donde murieron Maia y Clockwork Empires). Tal
como está escrita: **28-40 días de personas en los tres primeros meses**, cuatro a cinco veces lo
declarado. Con las sustituciones de §6: **14-22**, y más de la mitad de lo que queda es incomprimible
por naturaleza (mirar si se lee, mirar si es juego). Sigue siendo la más baja del catálogo, y por eso
sobrevive: lo que esconde es contenido pequeño y acotado, no balance.

## 1. Lo que declara y lo que esconde

| partida | declarado | corregido | por qué (con el código delante) |
|---|---|---|---|
| números de balance | 2 (día = 1 800 ticks; la raya de «clara» sobre el histograma de `carga`) | 2 + **un umbral por situación** + un horizonte por familia | «60 carbones», «2 000 unidades claras», «planta viva el día 20»: la columna «ticks hasta cumplir» exige un umbral, y el umbral es de autor. El validador (vacío falla, autor cumple) garantiza *no trivial*, no *interesante*: 40 y 90 carbones son situaciones distintas y solo una persona jugándola sabe cuál enseña. La refutación de apalancamiento del sello lo dijo con otras palabras: «sin validador, cada umbral es una tarde de Cesar». |
| contenido de autor | 12-20 montajes de 10-15 líneas | 12-20 × (montaje + pedido + **registro del autor jugado por una persona** + juzgar si enseña + curar sus mutantes) | Escribir `MontarHorno` son 15 líneas; lo caro, como dijo la refutación de la cuna, es juzgar si una cámara enseña algo. Y el validador de la dirección **exige** «el registro del autor debe cumplir»: alguien resuelve cada situación en el editor, y esa resolución es un artefacto humano. |
| caducidad | «hash de versión de física en el fichero» | hash **+ re-resolver a mano lo que cada paquete rompa** | El hash detecta la divergencia; no la repara. El laboratorio hizo 21 rondas en 3 días; L, V, M y C mueven hashes en varios escenarios cada uno. Cada paquete obliga a rejugar (automático) y re-resolver (humano) la fracción de registros que dejan de cumplir. |
| verbos | «cincel, frasco y pincel» | **sin definir**: si el pincel de `LabPanel` entra, el jugador crea materia | El pincel pinta cualquiera de los 14 materiales del laboratorio con radio hasta 8 (unas 200 celdas por clic, `LabPanel.cs:233-247`). «60 carbones» = pintar 240 celdas de fibra junto al hogar en un trazo. La refutación de ingeniería de la balanza ya avisó: «`PaintLab` escribe cualquier byte en cualquier celda; "nunca lo coloca el jugador" es una frase, no una guarda». |
| huella | «celdas cuyo material difiere del de nacimiento» | huella **del jugador**, no del mundo | En la carbonera 400 celdas de fibra se vuelven carbón, ceniza o aire hagas lo que hagas; con `Caudal` 24 el agua reescribe `mat[]` cada tick. Como columna de coste no discrimina; hay que redefinirla (celdas del registro, o diff solo sobre sólidos). Una línea, pero hay que decirla o el playtest la descubrirá por ti. |
| la vara | «los amigos, no el autor» | los amigos **con la misma versión y ficheros intercambiados a mano**; en solitario, la barra del autor | Nada online. Para Cesar y su hermano vale; para el jugador de la demo la única barra es la del autor y eso es el examen que el documento dice no ser. |
| gesto | 1,5 semanas de IMGUI | 2-2,5 semanas de Opus **+ dos rondas de láminas** | Recibo por bandas, histograma por métrica, línea de días con clones, vista de diferencia, carpeta: cinco vistas nuevas. El código es barato; que se lean sin cifras es exactamente lo que ningún hash prueba. |
| playtests | 6-8 días | 6-8 de playtest **+ 20-30 de autoría, curación y re-resolución** | §5. |

## 2. Las cuatro partidas humanas que el documento no cuenta

### 2.1 El registro del autor es obligatorio y caduca: la cinta de correr

El validador por veredicto tiene tres cláusulas y la tercera («el registro del autor debe cumplir») es
la única prueba de existencia de solución y la única demostración rejugable del nivel uno. Es humana
por definición. Con 12-20 situaciones a 0,5-0,75 días cada una (diseñar el montaje, elegir el pedido,
resolverla, ver si el «ajá» ocurre; el documento confiesa en el minuto 6 que «si no ocurre así, el
primer ajá se busca en otra de las ocho leyes»), son 6-15 días la primera vez. Y la dirección planea
cuatro paquetes de física después del núcleo. Cada uno mueve hashes; cada uno invalida una fracción de
los registros; la lista de rotos es automática (una regresión nocturna, 0,3 semanas), el arreglo no
(1-2 días por paquete). Con L, V, M y C: 4-8 días más de re-resolver, y una tabla de amigos que deja de
comparar entre versiones.

**El error es de orden de producción, no de diseño.** La solución más barata no es congelar la física:
es **escribir el contenido al final**. Mientras la física se mueve, las únicas situaciones son los
nueve montajes del banco (que ya se revalidan solos con los hashes). La campaña de 12-20 se escribe
cuando L y V estén dentro, porque la hora 5 del documento (huerto, invernadero, planta viva el día 20)
las necesita de todos modos. Eso reordena el calendario (física primero, contenido último) y deja la
cinta de correr en cero para la primera campaña. Lo que quede después (M, C) revela más que juzga y
mueve pocos hashes.

### 2.2 El número del pedido: doce a veinte números de balance

El documento dice «no hay aprobado; el pedido solo dice qué columna manda», y en la misma página pone
«ticks hasta cumplir el pedido» como primera columna del recibo. Las dos cosas no caben juntas: para
medir ticks hasta cumplir hace falta un umbral, y el umbral lo elige el autor. Es el único número de
balance de la dirección, pero son 12-20, y bajo es trivial y alto es imposible sin que el validador lo
note (mide trivialidad, no dificultad).

Tres sustitutos, todos sin número por situación:

1. **«Tick del primer X»** como columna de tiempo: el umbral es una unidad, físico, sin tuning. El
   banco de la refutación de la balanza ya lo mide (tick del primer carbón).
2. **Pedido relativo al vacío** con un factor fijo por familia (`≥ 3× lo que da el mundo sin manos`):
   4-5 números para todo el juego, no uno por situación; la refutación de ingeniería de la cuna lo
   propuso.
3. **Sin pedido**: solo la columna y el histograma. Es lo que el documento dice querer y lo que su
   primera columna contradice.

### 2.3 La economía de verbos decide si hay juego, y no está escrita

Hoy el laboratorio tiene tres manos: el cincel (disco de radio 2, 3 celdas por tick: un disco de 13
celdas tarda cinco ticks; alcance 2,2), el frasco (900 celdas, aspira 30 por tick: una poza en un
segundo) y el pincel del panel (crea cualquier material, radio 8). Con el pincel, la frontera es
trivial y el recibo mide cuánto pintaste; para arreglarlo habría que poner un presupuesto de materia
por situación, y eso es balance de autor. Sin el pincel (solo quitar y mover, como manda el R60 del
mundo heredado), el presupuesto **es la geometría del montaje**: lo que hay en pantalla es lo que hay,
y ninguna contingencia nueva hace falta. Pero entonces el espacio por situación es pequeño (la crítica
purista lo cuantifica: 92 montajes de un toque en 25 minutos de banco, unos 4 200 de dos toques en una
noche), y un espacio pequeño se enumera antes de que nadie juegue.

Para esta lente la consecuencia es concreta: la decisión de verbos es **un día de diseño y una ronda
de sensación** (2-3 días de personas) que no está en el presupuesto, y hay que tomarla **antes** del
barrido de §8 o el barrido mide un juego que no será. Y la ley que valga tiene que ser una guarda en
las puertas (`PaintLab` rechaza dones y materiales creables), no una frase.

### 2.4 La legibilidad del gesto es la mitad de la iteración humana real

El recibo «por bandas, no cifras», el histograma por métrica, la línea de días con clones, la vista de
diferencia y la carpeta de amigos son cinco vistas nuevas. `LabBandas` como única fuente de umbrales
quita los números de los campos (60, 100, 150, 170, 200 ya están en `LabParams`), pero las bandas de
los productos (¿qué es «mucho carbón»?) no tienen origen físico: solo tienen sentido relativas a una
población. Y que cuatro bandas se lean sin cifras es lo que el G6 de la primera pasada ya pedía probar
con láminas y desconocidos. Dos rondas, 3-4 días, incomprimibles. El documento cuenta una.

## 3. Comportamiento humano específico: la vara, la mesa y el relevo

La función objetivo penaliza depender de comportamiento humano específico. Aquí hay tres dependencias,
de peso distinto:

- **La vara son los amigos.** Es la pieza que convierte examen en juego y depende de amigos con la
  misma versión que se manden ficheros a mano. Sustituto honesto y automático: una **población
  sintética** (K = 200-1 000 registros sorteados de 1-3 cortes o mudanzas, corridos en banco: 15 s cada
  uno a 1,7 ms/tick, una a cuatro horas por situación) y la **frontera de Pareto de la búsqueda
  exhaustiva** de 1-2 toques, presentadas como «lo que el mundo puede dar», no como el autor. Da los
  percentiles para las bandas de producto, detecta la solución dominante antes de que Cesar juegue y
  caza exploits. Media semana sobre `CorrerSello`.
- **La mesa de tres** y **el relevo por fichero**: cero código, pero saber si es divertido es un
  playtest. Se pospone hasta después del veredicto «juego o examen»; si no se hace, no cuesta.
- **El determinismo entre máquinas**: hoy probado en un editor. Un día de banco (IL2CPP contra los 63
  hashes). Si falla, muere el relevo, no la dirección.

## 4. Contingencias ante construcciones arbitrarias: pocas, y todas son guardas

Es la partida donde la dirección está mejor. En una situación de una pantalla, sin crear materia, el
jugador solo puede quitar roca, mover lo que cabe en el frasco y esperar. Las contingencias reales son
tres guardas de una línea: los dones son indestructibles (`Tallable` false y `PaintLab` los rechaza), el
sumidero no se pinta ni se tapa con `SetCell` (que resetea `aux` y le quita el nombre a la salida), y
nada crea materia. El sandbox «sin pedido» con la rejilla entera admite cualquier construcción y no
necesita balance porque no hay meta. Los exploits que queden (tapón de fibra que se vuelve carbón
entregable, como el del minuto 6, que es un ajá y no un exploit) los encuentra la búsqueda exhaustiva
antes que cualquier persona. Temporadas cero, eventos cero. En esta columna la dirección merece la
nota que se pone.

## 5. Riesgo mayor desde esta lente: la frontera fabricada a mano

La dirección dice que su riesgo es que la frontera no exista, y que se mide. Cierto. Lo que esta lente
añade es **qué pasa el día después de medirla, si no existe**. Con lo que el código dice hoy, es lo
probable para la situación insignia: el carbón sale de un dado por celda (`RendimientoCarbonPct` 25
con `XorShift.FromCell`): una pila de 400 da 100 carbones con σ ≈ 8,7, dos máquinas idénticas corridas
una celda aparte difieren un 9-12 % por construcción; y R136 midió que la boca no regula la pila maciza
(249,8 raw con chimenea 0, 1 y 2). Si la semana 1 devuelve una espiga o ruido, la tentación será
**buscar los parámetros que hagan aparecer la frontera**: un `RendimientoCarbonPct` por situación, una
boca «que regule», un umbral «que separe». Eso es balance por la puerta de atrás, situación a situación,
y es exactamente el playtest infinito con otro nombre. Hay que prohibirlo por escrito antes de la
prueba: **`LabParams` es global y no se toca por situación; si la frontera no aparece con la física que
hay, se cambia de situación o se añade una ley, nunca un número.** Con esa regla la dirección es
honesta; sin ella es un sistémico más.

## 6. Qué se automatiza en su lugar

1. **Validez de cada situación y mutante**: registro vacío falla, registro de referencia cumple,
   fragilidad bajo K cortes (ya en la dirección). Con el validador **al revés** para los mutantes
   publicables (el registro del autor falla en el mutante y la búsqueda encuentra otra solución): así
   el mutante es contenido nuevo y no cosmética, y la curación humana baja a la mitad.
2. **El par y la existencia de solución**: búsqueda exhaustiva de 1-2 toques por situación
   (25 minutos y una noche de banco). Sustituye al registro del autor como prueba de existencia y como
   demostración de nivel uno; Cesar pasa de resolver a **juzgar** (0,25 días por situación en vez de
   0,5-0,75). Y tras cada paquete de física vuelve a encontrar la frontera sola.
3. **El número del pedido**: tick del primer X, o pedido relativo al vacío con un factor por familia.
4. **La anchura de la frontera y el ruido**: barrido más **gemelos** (cada configuración corrida ±1 y
   ±2 celdas): inter/intra por columna como medida estándar del banco.
5. **La población contra la que comparar**: histograma sintético y frontera de Pareto, con los
   percentiles como bandas de producto (quita las bandas de autor).
6. **Regresión nocturna de registros** contra la física actual, con la lista de rotos en el informe.
7. **Determinismo entre máquinas**: IL2CPP contra los 63 hashes, un día.
8. **La escala temporal y el «tedio»**: el salto headless al día N y el scrub sobre clones son una
   decisión de diseño, no un playtest. Con honestidad: el banco corre a 525-625 ticks/s y el ×10 a 300,
   así que el salto **halva** la espera, no la borra (30 días son 90 s headless). Lo que sí borra la
   espera es correr los días en segundo plano mientras el jugador prepara la rama siguiente; y el
   horizonte por familia (uno por familia, no por situación) se deriva del banco: 1,5× el día en que el
   registro de referencia deja de cumplir sin mantenimiento.
9. **El orden de campaña**: firma de leyes (diez grupos de `LabParams` × 18 000 ticks ≈ 6 minutos por
   situación). Es heurística: mide cuántas leyes importan, no cuánto cuesta ni si se entiende.

Lo que **no** se automatiza y hay que presupuestar como humano: juzgar si una situación enseña, elegir
la regla de verbos, si el recibo por bandas se lee sin cifras, si el nivel uno enseña sin texto, y la
única pregunta que importa: si preparar tres toques y sellar es juego o examen.

## 7. La prueba más barata que la mata (corregida)

**Semana 1, banco, sin sello, cero personas** (0,6 semanas de Opus: bit `entregable`, tragar sólido solo
desde arriba, la intervención genérica «quitar el tapón en T» generalizando la caldera de `Correr`, que
hoy está cableada al nombre del alambique; contadores por material y tick del primer carbón):

1. **El barrido de la dirección**: boca ∈ {1, 2, 4, 8} × T ∈ {0, 1 500, 3 000, 6 000} × tapón ∈
   {esquina, centro} = 32 configuraciones.
2. **Sus gemelos**: cada configuración con el montaje entero corrido +1 y +2 celdas en x (misma máquina,
   otro dado). 96 corridas × 9 000 ticks ≈ 25 minutos.
3. **La búsqueda exhaustiva de un toque**: los ~92 montajes de una celda de pared quitada, 25 minutos.

Se mide carbón tragado, fibra tragada (debe ser 0), tick del primer carbón, calor total, y por columna
la dispersión intra (gemelos) e inter (configuraciones que cumplen ≥ 40 carbones).

**La mata** si inter/intra < 1,5 en carbón y en tick del primer carbón (la frontera es el dado), o si
todas las configuraciones que cumplen caen dentro de ±1 σ intra (una sola máquina), o si un montaje de
un toque domina a todos los de la búsqueda en las tres columnas (el par se encuentra en cinco minutos).
**La confirma parcialmente** si inter/intra ≥ 3 en dos columnas con puntos no dominados a distinto T o
boca: eso prueba una curva legible de dos mandos, no un espacio de máquinas; la profundidad la tendrán
que traer las leyes siguientes.

**Misma semana, media jornada de Cesar, sin código nuevo**: las nueve situaciones del banco con cincel y
frasco en el editor, a ×10, con `LabCarbonizado` y `LabGoteos` en F8. Si en seis de nueve su primer
intento cae a ≤ 1 σ del mejor punto del banco, el humano no hace nada que el bucle no haga: es examen,
y se sabe antes de escribir el sello.

**En paralelo, un día**: IL2CPP contra editor.

**Semana 6, con el sello y la población sintética**: cinco personas, tres situaciones. La mata si sus
recibos válidos caen dentro del ±10 % en las tres columnas, **o** si todos caen dentro del percentil
central del histograma sintético.

## 8. Tiempos corregidos

**Automatizable y paralelizable (Opus, con prueba de banco por pieza):**

| pieza | semanas | hilo |
|---|---|---|
| bit `entregable` + tragar desde arriba + intervención genérica + barrido + gemelos + búsqueda de 1 toque | 0,7 | 1 |
| IL2CPP contra editor | 0,2 | 2 |
| balanza: libro por salida y calidad, histograma de `carga` | 1,2 | 2 |
| clon en memoria, cielo por geometría, `LabBandas`, anillo de hashes | 1,3 | 2 |
| gesto IMGUI (sellar, salto al día N, recibo por bandas, línea de días, diferencia, carpeta, histograma) | 2-2,5 | 2 |

**Secuencial (un hilo, cada pieza necesita la anterior):**

| pieza | semanas |
|---|---|
| diario bajo las cinco puertas con id de gesto y byte de autor + entrada de parámetro + preset congelado tras sellar + estado escondido (`_labManantialCeldas`, `LuzCieloX0/X1`, `_zonaInteresChunk`, libro) + volcado/carga + hash de versión | 3,5-4 |
| `CorrerSello` + condición como dato + cláusulas-testigo + «tick del primer X» | 1 |
| validador por veredicto (al revés para mutantes) + envejecer + mutaciones + ruinas | 1,5 |
| búsqueda exhaustiva de 2 toques + población sintética + regresión nocturna | 1 |
| firma de leyes | 0,4 |

Total Opus ≈ 11-12 semanas (la dirección dice 8,5-9,5; la diferencia son el gesto real, los gemelos, la
búsqueda, la población y la regresión). En dos hilos, **6 semanas de calendario hasta el prototipo
feo** con las nueve situaciones del banco, SELLAR, recibo, clones y fichero; **8-9** con la campaña
de 12-20 ordenada por firma, que por §2.1 no debería escribirse antes de L y V.

**Humano e incomprimible (tres primeros meses):**

| partida | tal como está escrita | con las sustituciones de §6 |
|---|---|---|
| economía de verbos: decisión + una ronda de sensación | 2-3 | 2 |
| nueve situaciones del banco: pedido + registro + juzgar si enseñan | 3-4 | 1-2 |
| 12-20 situaciones de campaña: ídem + segunda vuelta | 8-14 | 4-6 |
| curación de mutaciones y ruinas publicables | 2-3 | 1-2 |
| re-resolver tras cada paquete de física (L, V, M, C) | 4-8 | 0-2 |
| tedio o espectáculo | 0,5 | 0 |
| recibo por bandas e histograma legibles (láminas, dos rondas) | 3-4 | 3-4 |
| nivel uno sin texto (dos rondas) | 4 | 3-4 |
| mesa y relevo por fichero | 1-2 | 0 (pospuesto) |
| playtest «juego o examen» (semana 6) | 2-3 | 2-3 |
| **total** | **28-40** (declarado 6-8) | **14-22** |

**Hasta evidencia para matarla: 1 semana** (barrido + gemelos + búsqueda de un toque + media jornada
de Cesar + IL2CPP). Evidencia decisiva y humana: semana 6. **Hasta prototipo feo: 6 semanas.**

## 9. Órganos a conservar si se descarta

El sello entero (diario bajo las cinco puertas con id de gesto y byte de autor, entrada de parámetro,
volcado y carga como primer fichero de partida, `CorrerSello` con la condición como dato, hash de
versión); la balanza con bit `entregable`, tragar solo desde arriba e histograma de `carga`; la
intervención genérica del banco; el clon en memoria; el cielo por geometría; el validador por veredicto
(al revés para mutantes); la firma de leyes como ordenador heurístico; `LabBandas` como única fuente de
umbrales y el anillo de hashes; el protocolo de gemelos (inter/intra) como medida estándar de ruido del
banco; la búsqueda exhaustiva de 1-2 toques como par, generador de contenido y cazador de exploits; la
población sintética como vara sin personas; la regresión nocturna de registros contra la física; «tick
del primer X» como columna de tiempo sin umbral; el salto headless al día N con scrub sobre clones;
«rejugar sin las entradas de B» como atribución exacta; y la regla de producción **física primero,
contenido último**. Todo esto es «J primero y siempre» y no se pierde una semana si la dirección muere
como producto.

## 10. Rúbrica v2 (13 ejes, 1-10)

| eje | nota | por qué (desde esta lente) |
|---|---|---|
| apalancamiento sistémico | 6 | una física nueva (la salida como agujero para lo granular); convierte cuarenta contadores en juicio; pero el apalancamiento real depende de una economía de verbos sin escribir y de leyes que vendrán después |
| las leyes ejecutan, revelan y juzgan | 8 | juzgan de verdad (balanza, cláusulas, diff); el número del pedido y el par siguen siendo humanos hasta que la búsqueda los sustituya; revelan por números hasta L y M |
| iteración humana (10 = poca) | 7 | 28-40 días reales frente a 6-8 declarados, 14-22 con sustituciones; cero balance de espacio infinito, cero temporadas, biblioteca pequeña; la cinta de correr de registros es la partida que más crece y la que el orden de producción elimina |
| verificabilidad automatizable | 9 | la mejor del panel: cada pieza con banco; frontera, ruido, población, par y regresión sin personas |
| la simulación es el juego | 7 | el core es sellar y leer lo que la física escribió; el marco (pedido, par, histograma) es un puzzle de optimización y el riesgo de examen es real |
| onboarding garantizable | 6 | la firma ordena sin diseñador pero es heurística; la ruina de nivel uno es de autor y «sin texto» son dos rondas de personas |
| observabilidad | 5 | bandas y diferencia; la máquina en sí sigue en F8 y con plantas de un píxel hasta L y M |
| tiempo como apuesta | 9 | SELLAR es el verbo; el clon quita el castigo sin quitar la apuesta; el salto al día N quita el peaje |
| multiplayer emergente | 5 | mesa y relevo sin roles, pero nada probado como divertido y el determinismo entre máquinas sin medir |
| profundidad por leyes estables | 6 | escala con cada ley nueva (columnas y situaciones), pero un espacio de dos mandos es una curva, no una cola, y cada paquete de física reinicia las tablas si el contenido se escribe antes |
| cuerpo del jugador | 3 | cursor y herramientas heredadas; el cuerpo entra como sensor al final |
| identidad comercial | 6 | frase y «DÍA N SIN MANOS» nombrables; nicho de Opus Magnum (200-500 k, la mediana más baja); el GIF hoy es una pila de un píxel que humea |
| dificultad técnica (10 = fácil) | 8 | C# puro, acotado, en banco; lo abierto es el gesto, la huella sin fluidos y la regla de verbos |

Puertas: iteración 7 ≥ 5, apalancamiento 6 ≥ 5. Pasa.

## 11. Veredicto y condiciones

**Finalista.** Condiciones que impongo desde esta lente, todas anteriores a la primera semana de banco:

1. **La regla de verbos por escrito y como guarda**: nada crea materia; los dones y el sumidero no se
   pintan ni se tallan. Sin esto, el barrido de la semana 1 mide otro juego.
2. **`LabParams` es global y no se toca por situación.** Si la frontera no aparece con la física que
   hay, se cambia de situación o se añade una ley; nunca un número. Es la única frase que separa esta
   dirección del playtest infinito.
3. **El barrido lleva gemelos y la búsqueda de un toque**, o no mide lo que la dirección dice medir.
4. **La columna de tiempo es «tick del primer X»**, no «ticks hasta cumplir un umbral»; el pedido
   nombra una columna, como el documento promete y su recibo contradice.
5. **Física primero, contenido último**: la campaña de 12-20 se escribe después de L y V; hasta
   entonces las situaciones son las nueve del banco, que se revalidan solas.
6. **La cifra de iteración humana se corrige** a 28-40 días tal como está, 14-22 con la búsqueda
   exhaustiva, la población sintética, la regresión nocturna y el salto al día N presupuestados como
   sustitutos.

Con eso, es la única dirección del catálogo en la que lo que esconde es contenido pequeño y acotado, y
no balance; y la única cuyo coste de morir es cero, porque todo lo construido para cruzar la puerta es
la capa J que cualquier otra necesita.
