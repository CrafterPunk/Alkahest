# DIRECCIÓN LIBRE · «A QUE SÍ» (pronóstico): pinta el futuro, la física fija la cuota

*(Panel de direcciones, segunda pasada. Ángulo: dirección libre. Leído: `01_LEYES.md` entero, la
lente de métricas y SOLTAR con sus refutaciones, `00-04` de la primera pasada, `LabBench.cs`
(`Escenarios`, `Correr` con `Montaje` delegado, `ArcoMuestra`), `LabParams.cs`. El crítico de huecos no
existía en disco.)*

## 1. Nombre y frase

**Nombre de trabajo:** «A que sí» (mecanismo: el *pronóstico*; en el banco, `LaBanca`).

**Frase de diez segundos:** «Arma tu máquina de agua y fuego, pinta en la pared lo que va a pasar y
suelta el tiempo: la física te paga según lo difícil que era acertar.»

**Por qué no la cubren los otros siete ángulos.** Todos los que juzgan (situaciones, SOLTAR, «las
leyes juzgan») comparten una forma: alguien escribe la condición y la simulación dice si el jugador la
cumplió. Un examen con examinador automático. Aquí se invierte la autoría: **el jugador escribe la
condición** (pinta lo que cree que va a ocurrir, dónde y qué día) y **la simulación fija el precio**
de esa afirmación corriendo K partidas ignorantes con el mismo presupuesto. La cláusula sobre la grilla
del sello se usa al revés: no para aprobar, sino para cotizar. Ningún otro ángulo tiene a la simulación
calculando *cuánto vale saber algo*, y esa es la pieza que responde de frente a la pregunta final: el
balance (lo que cuesta meses de tuning humano) se calcula en banco, y cada ley nueva reescribe las
cuotas de todas las situaciones sin que nadie las toque. No es un contenedor (cabe en un pozo, una
ladera o una cámara sellada); es una ley de juicio.

## 2. La experiencia narrada

**Las manos.** Cursor, no muñeco. Tres gestos. **Pintar materia** (cavar, terracota, yesca, arcilla,
núcleo frío, verter agua) hasta un presupuesto por situación (20 celdas de terracota, 5 de frío, 30 de
yesca, 40 cortes: el diario del sello ya cuenta las celdas tocadas). **Pintar fantasmas**: un pincel
que no pone materia sino una afirmación translúcida: «aquí, agua, el día 2», «aquí, planta viva, el
día 5», «aquí, carbón, el día 1», «aquí, nada, el día 3» (vaciar una poza también es un pronóstico).
**SOLTAR**: se cierra el diario, corre el horizonte a ×10 y los fantasmas se llenan o se quedan huecos.
Un fantasma solo reclama lo que una ley produce: una celda tocada por tu propio registro anula el
fantasma que pongas en ella, así que nadie cobra por pintar terracota y pronosticar terracota.

**La cuota.** Antes de que entres, el banco corrió la situación con el registro vacío y con K = 16
registros al azar del mismo presupuesto («la banca»: rectángulos de 1×1 a 6×6 de cada material en
sitios sorteados, tres cortes al azar: lo que haría quien no sabe nada) y guardó, por celda, familia de
material y día, cuántas veces salió eso. La cuota de un fantasma es `1/p` con suelo de Laplace y techo
`K+1 = 17`. Agua bajo el manantial: ×1. Agua en una repisa a la que solo llega por un sifón cebado: ×9.
Vidrio en cualquier parte: ×17 (la banca nunca construye un horno). Un fantasma acertado paga su cuota;
uno fallado resta uno. La puntuación es la suma: un escalar cuyos **pesos no los puso nadie**; los
produjo la física contra la ignorancia.

**El visor de cuota.** Un rayos X nuevo y el más didáctico del juego: eliges «agua, día 2» y el mundo se
tiñe por cuota: oscuro donde el agua llega sola, cálido donde a veces, blanco incandescente donde la
banca nunca la vio. Un tablón de recompensas escrito por las leyes: dice qué sería impresionante aquí
sin que ningún diseñador lo haya pensado. Con él va el **visor sin manos**: lo que pasa si no haces nada
(el registro vacío por día, como fantasmas grises). Los rayos X de bandas (calor, humedad, luz, carga,
con `LabBandas` como única fuente de umbrales) se leen como en el laboratorio; con M, la huella dice
después por qué un fantasma se quedó hueco.

**Los primeros diez minutos.**
- *0-1.* «Gota»: un cuenco de agua caliente, un techo, una celda de núcleo frío en el techo. No
  construyes. Tres fantasmas. El visor de cuota está en «agua, día 1»: bajo el frío, ×6; el resto,
  ×1 o ×17. Pintas tres fantasmas de agua donde crees que caerán las gotas. SOLTAR: un día son 1 800
  ticks, seis segundos a ×10. Dos se llenan con un tañido y un «×6» que flota; el tercero queda hueco.
  Aprendiste que condensa en el vecino más frío, y que acertar donde nadie acierta paga.
- *2-4.* «Gota II»: el frío lo pones tú (cinco celdas, donde quieras del techo). El visor muestra que la
  banca a veces acierta bajo el techo (×3) y nunca en el rincón alejado (×17). Llevas el frío al rincón,
  pintas agua debajo, SOLTAR: ×17. Primera vez que una decisión tuya cambió el precio de lo que sabías.
- *5-7.* «Brasa»: una pila de fibra junto a un hogar y diez celdas de terracota. Fantasmas de carbón y
  ceniza. Sin aire arde en sordina y deja carbón: la tapas, pintas carbón dentro y ceniza en la boca.
  Aprendes la carbonera por la cuota, no por un texto.
- *8-10.* «Yesca mojada»: la gota cae sobre la yesca que el hogar debería prender. «Fuego el día 1»
  junto a la yesca paga ×11 porque la banca casi nunca la mantiene seca: desvías la gota o mueves la
  yesca. Es R135 en miniatura, la primera situación que cruza dos leyes.

**Hora 1.** Situaciones de 3-5 días, presupuestos de 40-80 celdas, 5-8 fantasmas, 3-6 minutos cada una
(montaje dos o tres minutos, SOLTAR 20-40 segundos a ×10, con bajada a ×1 cuando un fantasma está a
punto). Empiezan las combinaciones: alambique más lecho; la primera planta pronosticada (si el día de L
remide Q16 y el huerto vivía por luz, «planta viva, día 5» es la recompensa mayor de la primera hora;
si no, cuando llegue V). El hueco del minuto 15-40 que mató a las seis propuestas se ataca por
**densidad del bucle**: diez situaciones por hora, cada una con veredicto.

**Hora 5.** Cuevas envejecidas por simulación (los nueve montajes del banco y sus mutaciones: boca ±k,
hogar movido, veta más gruesa), horizontes de 7 días, pronósticos de segundo orden: «el humo del horno
oscurece la cámara de arriba → la planta de (118,250) muere → fibra ahí el día 6». El **sobre
cerrado**: mandas a una amiga el fichero del sello (mundo, diario, hashes, tus fantasmas cifrados) y
ella pinta los suyos antes de ver el resultado; el veredicto se rejuega en su máquina y los hashes del
emisor delatan divergencias entre PCs. El **recibo compartible**: una rejilla de cuadritos llenos y
huecos con la suma («A QUE SÍ #217 · 6/8 · ×41»), el órgano de Wordle a coste cero.

**Hora 20.** La **cueva larga**: un mundo de 768×288 persistente donde cada semana (7 días de
simulación) publicas un pronóstico sobre tu propio mundo; la banca corre el registro vacío y cuatro
monos desde tu último sello en segundo plano (cinco corridas de 12 600 ticks, dos minutos) y cotiza.
Jardinería de tus propias predicciones: la tolva que se acaba a los 466 s, la grava que se colmata.
Retos: misma situación para varias personas, comparación por suma. Órgano tardío: «las cinco cuevas
hermanas» (tu registro rejugado en cinco mutantes, como los casos ocultos de Zachtronics).

**Mientras corre.** Miras. Los fantasmas se llenan con su tañido y su cuota flotante; cambias de visor
(calor, humedad, luz, cuota, sin manos) pero no tocas; te asomas por día. La tensión es la de un
fantasma de ×14 al que le falta una celda de agua y un día.

**Con 2-3 personas, sin roles.** Host y espejo (ruta A): construyen juntos la misma máquina y cada
quien pinta sus fantasmas en su color, **ocultos hasta SOLTAR**. Cooperan en la obra y compiten en la
lectura; al soltar, las creencias se revelan («¿pintaste agua AHÍ?»). Es la información asimétrica
temporal del encargo sin geografía: la asimetría es lo que cada uno cree, y dura hasta el veredicto.
Asíncrono por fichero desde el primer día, porque el sobre cerrado es el fichero del sello.

**Cómo se aprende.** Una escalera que **no ordena ninguna persona**: el banco calcula por situación qué
familias alcanzan la cuota media (lo enseñable), la entropía del mapa (cuánto hay que saber) y su
sensibilidad (recorre el registro vacío con un `LabParam` movido ±20 %: qué leyes mueven el mapa
etiqueta la situación por ley). La campaña va ley por ley con montajes de autor de 10-15 líneas y sus
mutantes; el sandbox es la cueva larga con las mismas cuotas como retos.

## 3. Core loop

Preparar con un presupuesto → pintar lo que crees que va a pasar, a la cuota que fija la física →
SOLTAR → los fantasmas se llenan o quedan huecos → leer con los visores por qué → siguiente.

```
   [situación cotizada por la banca: R0 + 16 monos]
                │
       preparar (materia, presupuesto)  ←──────────┐
                │                                  │
       pintar fantasmas (celda, familia, día)      │  leer: bandas, huella,
                │      visor de cuota = tablón     │  visor sin manos, diff
             SOLTAR (diario cerrado, ×10, días)    │
                │                                  │
       veredicto: Σ cuota(aciertos) − fallos ──────┘
                │
       recibo compartible · sobre cerrado · reto
```

## 4. Por qué explota mejor la simulación

**Ejecutan.** Nada nuevo: el laboratorio con diario.

**Revelan.** El visor de cuota es un rayos X de *lo que la física tiende a hacer aquí*; el visor sin
manos es el mundo sin ti; ambos salen de corridas, no de autoría. Un fantasma hueco es una pregunta con
coordenadas y día; las bandas y (con M) la huella son la respuesta.

**Juzgan y cotizan.** La cláusula «material en (x,y) == M el día D» ya está en J. Lo nuevo es que la
simulación decide cuánto vale cada cláusula corriéndola contra la ignorancia. Por eso la profundidad
crece más rápido que el coste de diseñarla: cuando entre A, «llama viva el día 3 en un cuarto cerrado»
pasará de ×1 a ×17 en todas las situaciones de fuego **sin tocar ninguna**; con F aparecerán fantasmas
de hielo con precio; con L, fantasmas de planta bajo tierra. Cada ley reescribe el tablón.

**Contenido sin autor.** Cuotas, escalera, etiqueta por ley y validez de cada situación y mutante son
salidas del banco. La refutación de la cuna dijo que lo caro es juzgar si una cámara enseña algo: aquí
«enseña algo» es una cifra (familias en cuota media × entropía).

**Validado en banco sin personas.** Por situación: determinismo (misma semilla, mismos hashes), mapa con
gradación (≥ tres niveles, ≥ 10 % de las celdas de aire en el nivel medio para alguna familia) y **el
registro del autor cobra** (≥ 3 fantasmas de cuota ≥ ×4): si la máquina de referencia no gana a los
monos, la situación no entra.

## 5. Qué añade al sustrato

Ninguna ley física. Sobre el paquete **J** (prerrequisito: diario en las cinco puertas, volcado y carga,
`CorrerSello`, cláusula sobre la grilla, `LabBandas`; 3,2 semanas refutadas):

| pieza | qué es | cruza | Opus |
|---|---|---|---|
| **La banca** | gramática de registros al azar por presupuesto; corredor de R0 + K; volcado de `mat` por día; mapa `p(x,y,familia,día)` con Laplace y techo; familias = lo que producen las leyes (agua, agua clara con `carga ≤ AguaClaraCargaMax` de la balanza, hielo, humo, fuego, carbón, ceniza, planta viva, fibra, terracota cocida, vidrio, sedimento, aire) | todas las leyes | 1-1,5 sem |
| **Fantasmas** | capa de reclamos en cliente; pincel (celda, familia, día); anulación por celda tocada; evaluación con la cláusula de J; render translúcido, llenado, hueco; suma | cláusula del sello | 1 sem |
| **Visor de cuota y sin manos** | dos vistas sobre el mapa y sobre R0 | `VistaLaboratorio`, bandas | 3 días |
| **Presupuesto** | tope de celdas por familia, contado por el diario | diario | 2 días |
| **Escalera** | entropía, familias en cuota media, sensibilidad por `LabParam`; orden y etiqueta | `LabParams`, banco | 3-4 días |
| **Recibo y sobre cerrado** | rejilla compartible; fichero del sello más fantasmas cifrados; rejugado local | volcado, hashes | 3 días |

Total sobre J: 4-4,5 semanas de Opus. Después: **L** (el día de Q16 primero: decide si la planta se
pronostica en la hora 1; el vidrio que transmite da al horno un producto con cuota), **V** (fantasmas
de fibra donde la corriente la deja), **A** (la llama mortal reescribe el tablón de fuego), **F**
(hielo con cuota; fusible como temporizador), **M** (la huella como respuesta al fantasma hueco), **C**
opcional como sonda. Cómputo: cotizar = (1 + K) × H ticks; el alambique (9 000 ticks, ~15 s) con
K = 16, cuatro minutos en serie (`LabParams` es estático: se paraleliza entre procesos, no dentro).
Cien situaciones con mutantes, una noche.

## 6. Principal riesgo de diseño

Que pintar el futuro se sienta como rellenar un examen y no como apostar. El panel dejó esa duda abierta
y esta dirección la enfrenta con la forma más probada de convertir conocimiento en tensión (elegir
cuánto ambicionar a una cuota), pero eso no lo decide una ley. Segundo, degeneración: que lo óptimo sea
ignorar el tablón y repetir la hazaña estructural que ya sabes (horno → vidrio ×17 en todas partes). Lo
acotan el presupuesto (sin terracota no hay recinto), el número de fantasmas por situación y el nivel
medio del mapa, donde se distingue a quien lee de quien repite. Tercero: que el nivel medio no exista
(§8).

## 7. Cuánto depende de iteración humana

**Exige playtest**: (1) la ergonomía del pincel de fantasmas (familia y día sin menú: dos o tres
sesiones con cinco personas); (2) si el visor de cuota se lee en lámina, como G6; (3) si mirar a ×10
con fantasmas es tensión o espera (incógnita común a todas); (4) la sensación examen/apuesta, en papel
antes de código de render. Tres o cuatro tardes, no meses.

**Se sustituye por banco**: el balance entero (las cuotas), dificultad y orden de la campaña, validez de
cada situación y mutante, regresión de cada ley nueva (recotizar), detección de copias y de divergencia
entre máquinas. Autoría restante: diez o quince montajes de 10-15 líneas y las cuatro situaciones de
mano.

## 8. La prueba más barata capaz de matarla

No necesita el diario: `LabBench.Correr` acepta un `Montaje` delegado y los monos pintan en el tick 0.
Sobre `MontarAlambique` (serpentín de 31 celdas de R141) y `MontarCarbonera`, con presupuesto {5 frío,
20 terracota, 30 yesca, 40 cortes}: correr R0 y 16 registros al azar, volcar `mat` al final de cada día
durante cinco días, calcular el mapa de cuota para agua, carbón, fuego y ceniza. Tres o cuatro días.

**La mata**: (a) el mapa es binario (todo ×1 o todo ×17, menos del 10 % de las celdas de aire en cuota
media para todas las familias): sin gradación no hay apuesta y la dirección se reduce a «las leyes
juzgan»; o (b) el registro del autor (el serpentín; la carbonera con boca 1) no acierta ni tres
fantasmas de cuota ≥ ×4: el conocimiento no gana a los monos, no hay nada que cobrar. Segunda prueba,
humana y de dos días, solo si pasa la primera: las cuatro situaciones de mano en papel con el mapa
impreso, cinco personas, una pregunta: ¿pintan donde el tablón paga o donde ya saben? Si nadie mira el
tablón, el visor no dirige y el minuto 15-40 sigue vacío.

## 9. Tiempos

- **Evidencia para matarla: 1,5 semanas** (banco, un Opus, sin dependencias).
- **Prototipo feo que permita juzgar el core: 6 semanas de calendario** (8-9 de Opus en dos hilos:
  J-motor en uno; fantasmas, visores y presupuesto sobre un stub en el otro; banca y escalera cuando el
  diario cierre). Dependencias secuenciales: la banca necesita el volcado por día; el sobre cerrado,
  volcado y carga. Iteración humana: tres o cuatro tardes desde la semana 5, más dos de papel.
- **Iteración humana probable**: 7 en la rúbrica: sin balance, sin biblioteca, sin temporadas; queda
  sensación de pincel, lectura del visor y ritmo del ×10.
- Con L y V para que planta y fibra tengan cuota: 12-13 semanas de Opus, en paralelo de dos en dos.

## 10. Lo que deja fuera y por qué

Estaciones y eventos (la tensión la ponen las cuotas, estables mientras las leyes lo sean). Bibliotecas
de contenido. Roles por geografía. El cuerpo como leyes (el cursor es la mano; C después si un playtest
lo pide). Aire, frío y memoria en el prototipo feo: reescriben el tablón, pero el core se juzga sin
ellos. Lámpara, sondas con radio, polilla, veta en juego. El año de P3 (el veredicto llega en un
minuto). La economía de la mano de P4 (el presupuesto del diario la sustituye). «La mano solo actúa con
luz» de P5. El censo de P6 (una planta pronosticada es su censo).

**Conserva**: SOLTAR y el contador de días (el horizonte); la cámara sellada con veredicto diferido (la
situación); el cuaderno falsable de P2, que deja de ser cuaderno y es el core (fantasmas = afirmaciones
falsables con precio); asíncrono por fichero (sobre cerrado); la huella (respuesta al fantasma hueco).
**Descarta**: la semilla que dice por qué falló (lo dicen los visores); los seres lectores; «días sin
manos» como puntuación (el pronóstico lo subsume: «esto sigue vivo el día 300» es un fantasma).

**Si no gana**, dos órganos valen para cualquier otra: la banca como validador continuo (sustituye el
«K perturbaciones deben dar veredictos distintos» de la cuna por una cifra de gradación) y el visor sin
manos como tutorial sin texto.

## 11. Autoevaluación (rúbrica v2)

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 8 | una ley de juicio, ninguna física; reescribe el valor de cada situación y de cada ley futura |
| leyes ejecutan, revelan y juzgan | 9 | ejecutan, revelan (cuota, sin manos, bandas), juzgan y **cotizan** sin un número humano |
| iteración humana (10 = poca) | 7 | pincel, visor, ritmo; cero balance, cero biblioteca |
| verificabilidad automatizable | 9 | banca, escalera, validez y regresión son trabajos de banco con hashes |
| la simulación es el juego | 7 | el fantasma es el compromiso hecho visible; pero es un marco de puntuación y lo admito |
| onboarding garantizable | 8 | escalera calculada; cuatro situaciones de mano; visor sin manos |
| observabilidad | 8 | dos visores nuevos que salen de corridas; bandas; huella después |
| tiempo como apuesta | 10 | literalmente una apuesta contra el tiempo con cuota |
| multiplayer emergente | 7 | construir juntos y creer distinto, oculto hasta SOLTAR; sobre cerrado; sin roles; online no resuelto |
| profundidad por leyes estables | 8 | cada ley nueva recotiza todo; sin eventos |
| cuerpo del jugador | 3 | cursor; C opcional y tardío |
| identidad comercial | 7 | frase, clip (el fantasma que se llena, «×17»), recibo compartible; arquetipo puzle sistémico (60-150 k base según `03`); el co-op de creencias puede subirlo |
| dificultad técnica (10 = fácil) | 8 | todo acotado y en banco; el único coste abierto es cotizar la cueva larga en segundo plano |

Puertas: iteración humana 7 ≥ 5, apalancamiento 8 ≥ 5. Compite.
