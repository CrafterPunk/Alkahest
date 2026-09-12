# CRÍTICA · «A QUE SÍ» (libre) · lente: LA SIMULACIÓN ES EL JUEGO

*(Panel de direcciones, segunda pasada, 2026-09-12, versión 2 de esta lente: la primera dio «segunda
ronda, estrecha»; esta la reevalúa con la función objetivo de Cesar por encima de todo, sin proteger
ni El Pozo ni mi veredicto anterior. Crítico: director creativo purista de la simulación; único tema:
¿la simulación ES el juego o hay un juego tradicional encima?, ¿el core loop suficiente existe sin
acumular features?, ¿las leyes ejecutan, revelan y juzgan solas o hay un diseñador escondido en la
puntuación, la progresión o el contenido?, ¿es divertido minuto a minuto o es un examen?, ¿cómo se
narran el clip y la frase? Leído: `libre.md` entero; `01_LEYES.md` entero con §6; `00_ENCARGO`;
`02_DIRECCIONES`, `03_COMPARATIVA` y `04_POSICION` (para saber dónde quedó la dirección, no para
obedecerlo); las críticas hermanas de ingeniería e iteración humana de esta dirección; `03_MERCADO.md`
§3-4; `leyes-juzgan.md` y su crítica purista (la dirección con la que esta comparte juez). Código:
`LabBench.cs` (`Montaje` :70, `Escenarios` :76-87, `MontarAlambique` :105-109, `MontarCarbonera`
:135-143, la caldera como única intervención del banco :301-311); `MaterialDef.cs` (`Fire` como «solo
la LENGUA VISIBLE» :131-135; `gasLifetime` :122); `SimStepper.Laboratorio.cs` (germinación que lee
`luz[i+W]` :700-712; la planta sin raíz que se vuelve fibra :811); `LabParams.cs` (`GerminaPorMil` 2,
`PlantaLuzMin` 40).)*

**Veredicto: DESCARTAR como dirección; conservar cinco órganos, uno de ellos nuevo.** El core de «A
que sí» es una apuesta sobre la simulación, y la apuesta es el juego tradicional más viejo que
existe. El verbo central (pintar fantasmas) no toca una celda; el marcador mide lo que el jugador
dijo, no lo que su máquina hizo; para que el mapa de cuotas valga, la preparación tiene que ocurrir
con el tiempo parado; y los pesos «que no puso nadie» los puso quien escribió la gramática de los
monos, quien fijó el castigo por fallo y quien reparte el presupuesto por situación. Lo que en la
primera versión de esta crítica mandé a segunda ronda (la banca y sus visores sobre un core que
puntúe la máquina) no es esta dirección: es un órgano para otra. Y la transformación que sí pasa mi
lente (el fantasma pintado ANTES de la obra, como pedido que el jugador escribe y la física cotiza)
cambia la frase y el bucle; también es otra dirección. Por eso el veredicto honesto de esta lente es
descartar la dirección y quedarse con lo que vale.

## 1. Qué es el juego aquí, con la lupa del purista

El bucle tiene cuatro actos: preparar la máquina, pintar fantasmas, SOLTAR, cobrar. El primero y el
tercero son simulación pura y son idénticos en las nueve direcciones. Los dos nuevos, pintar y
cobrar, no tocan una celda: un fantasma es una afirmación que la física no lee, y el cobro es una
lectura de la grilla contra esa afirmación. La consecuencia se enuncia sin adjetivos: **dos máquinas
idénticas puntúan distinto según lo que se pintó encima**. El alambique de R141 vale ×41 para quien
pintó bien y ×3 para quien pintó mal, y vale lo mismo (900 goteos, o 900 perdigones de hielo según
el hecho 4) para la física. En Noita, Dwarf Fortress o Powder Toy el mundo devuelve la consecuencia
de lo que hiciste; aquí devuelve la nota de lo que dijiste. Es el cuaderno falsable de P2 ascendido
a core, y el cuaderno era un órgano de onboarding precisamente porque describir el mundo es una
actividad de examen. La propia frase lo confiesa: «la física te paga según lo difícil que era
acertar». Acertar es el verbo de un test.

El documento tiene una defensa y la concedo entera: el fantasma es el compromiso de SOLTAR hecho
visible, y una apuesta se lee en un fotograma sin voz (condición 1 del clip de `03 §4`). Pero eso es
lo que ya hace el chat de un streamer en P3 y P4: apostar sobre la corrida de otro. Esta dirección
le da al jugador el verbo del espectador. Un espectador con pincel sigue siendo espectador de su
propia máquina.

Lo que sí es virtud real y no la tiene ninguna otra: la **densidad del bucle**. Diez situaciones por
hora con veredicto en un minuto es la única respuesta concreta del panel al hueco del minuto 15-40.
Se conserva, pero como ritmo de F1 (situaciones de 3-6 minutos), no como razón para puntuar
descripciones.

## 2. El diseñador escondido: hay tres, y los tres son números humanos

**(a) La gramática de la ignorancia es el balance.** «Un escalar cuyos pesos no los puso nadie» es
falso. Los puso quien decidió que la ignorancia son rectángulos de 1×1 a 6×6 en sitios sorteados y
tres cortes al azar en el tick 0. La cuota mide «cuán raro es esto bajo construcción aleatoria», no
«cuánto vale saberlo», y las dos cosas se separan justo donde importa: todo producto que exija una
estructura (recinto, sifón cebado, horno) vale ×17 sea trivial o genial, porque los monos nunca
construyen. El documento lo llama degeneración y lo lista como riesgo; no es un riesgo, es el régimen
esperado: el techo se alcanza con cualquier truco de primer orden («Gota II»: frío en el rincón,
agua debajo, ×17, celebrado como ajá). La única cura es enseñar a los monos cada truco, y eso es
balancear un espacio infinito truco a truco: lo que la función objetivo penaliza con más fuerza.

**(b) La regla de puntuación necesita el tuning que dice no tener.** El crítico de iteración humana
hizo la cuenta y la adopto: con cuota `c = 1/p`, acierto `+c`, fallo `−1`, la esperanza de un
fantasma pintado a ciegas con creencia `q = p` es `p ≥ 0`: el ignorante nunca pierde en esperanza,
pintar más siempre es mejor, y la apuesta más rentable de quien no sabe es la de ×1 que el visor sin
manos regala. Que sea saber o casino lo deciden el castigo por fallo, el tope de fantasmas y `K`:
tres mandos globales que afina una persona mirando a otras.

**(c) El presupuesto por situación es el «60 carbones» del Recibo con otro nombre.** {5 frío, 20
terracota, 30 yesca, 40 cortes} decide qué pueden hacer los monos y, por tanto, el mapa entero. Es un
vector de autor por situación; más pequeño que un umbral, pero autoría.

Hay alternativas medibles (banca poblacional con los sellos publicados como monos; mutantes del
registro del jugador; cuota por número de grupos de `LabParams` que mueven el resultado). Las tres
salen del mismo arnés y las tres **cambian la frase** («según lo exacto que tenías que ser», «según
cuántas leyes cruzaste»): no reparan esta dirección, fundan otra.

## 3. El estado cotizado no es el estado jugado: preparar se pausa

Las cuotas se calculan desde el tick 0 con monos que pintan en el tick 0 (es lo que `Correr` con su
`Montaje` permite). El jugador prepara dos o tres minutos con el mundo corriendo (§2: «la caldera
repone, la gota cae, miras») y suelta en el tick 3 600-5 400: el mapa fue computado para un mundo que
no es el que suelta. Las tres salidas tienen precio: pausar durante preparar (el bucle se vuelve
editor estático más corrida, la forma Zachtronics, y el falling-sand deja de ser el juguete que se
toca en caliente); recotizar desde el estado de SOLTAR (17 corridas de 9 000 ticks: 4-6 minutos de
espera por situación, adiós a las diez por hora); o contar los días desde el t0 y que preparar los
consuma. La ingeniería eligió la primera por coherencia; mi lente dice que esa elección es la
confirmación: el core es un puzle con corrida, no un mundo. La tercera salida es la única purista, y
lleva a §6.

## 4. El nivel medio es el borde, y el tutorial está escrito contra una máquina que el código no tiene

Con `K = 16` la resolución de `p` es 1/17: la «gradación» vive entre lo que R0 hace siempre y lo que
los monos hacen entre 1 y 16 veces. Como los monos casi nunca construyen nada que produzca, esa banda
la pueblan las celdas donde **el propio R0 parpadea**: la línea de la superficie de un charco, el
frente de una llama, el borde del humo. El conocimiento pinta interiores (×1 o ×17); la suerte pinta
bordes (×2-×8). La tensión que vende el documento («un ×14 al que le falta una celda de agua y un
día») es literalmente la de acertar el borde.

Y las familias transitorias no existen a un tick: `Fire` es «solo la LENGUA VISIBLE» que aparece con
probabilidad por paso sobre el combustible que arde (MaterialDef.cs:131-135); un fantasma «fuego el
día 1» evaluado como `mat == Fire` en el tick 1 800 es una moneda al aire sobre una pila que arde de
verdad. El humo vive `gasLifetime` y viaja. La gota del serpentín nace a la temperatura del frío y se
hiela al tick siguiente (hecho 4): bajo el frío la familia es hielo en vuelo y humedad en el lecho,
no `Water`. «Gota», la situación del minuto 0, está narrada contra un alambique que el código
contradice. Los arreglos existen (máscara de familias vista en el día, fantasma de región 3×3,
familias por banda) y **cada uno reescribe todas las cuotas**: elegir la semántica del fantasma es
una decisión de diseño con una sesión de personas detrás.

## 5. Minuto a minuto y el clip

El pincel es de tres ejes (celda, familia, día) y el documento admite que no sabe hacerlo sin menú.
Diez situaciones por hora con 5-8 fantasmas son 50-80 entradas de formulario por hora: el ritmo de un
examen, no de un juguete. SOLTAR es mirar a ×10 con la tensión del borde; cobrar es un tañido y un
número.

El clip: 0-2 s, un cuadro translúcido con «×14» sobre una cámara; 2-7 s, la física; 7-10 s, el cuadro
se llena y el número flota. Legible sin voz, con cadena y con consecuencia atribuible, y con un
remate que es una cifra: la palabra que nombra el fenómeno (condición 4) la sustituye un
multiplicador. Salvo en compañía: **«a que sí / a que no»** es el mejor remate verbal del panel,
porque una apuesta entre amigos es drama humano y no tabla. Pero solo existe con dos personas, y la
cápsula (cuadros translúcidos sobre una cueva) no es castores ni balsa. Identidad de un juego de
apuestas sobre un simulador: coherente, y del género más pequeño de `03` (puzle sistémico, 60-150 k).

## 6. La transformación que sí pasa esta lente, y por qué es otra dirección

Invertir el orden: el fantasma se pinta **antes** de la obra. Ya no es una predicción sobre tu
máquina (describir) sino un **pedido que escribe el jugador** y que la banca cotiza desde el t0 de la
situación, con el mundo corriendo y sin pausa: «agua en la repisa el día 3» vale ×9 porque los monos
nunca la suben; ahora tienes tres días (tres minutos a ×1) para construir el sifón antes de que el
horizonte llegue. La obra consume días: tocar tiene precio sin racha ni contador. La máquina es lo
que se juzga (la cláusula sobre la grilla de J, escrita por el jugador y no por un autor); la cuota
es el precio de la ambición; el estado cotizado ES el estado jugado, porque el precio se fijó antes
de que nadie tocara nada. Resuelve de golpe §1 (el verbo vuelve a la grilla), §3 (nada se pausa) y el
diseñador escondido del Recibo (el número del pedido lo teclea el jugador y lo cotiza la física).
No resuelve §2a: un pedido fácil que los monos no cubren sigue pagando ×17, y la prueba de banco de
§7 sigue haciendo falta.

Y un segundo órgano, del co-op: **el que mira, apuesta**. En host y espejo, quien no construye pinta
fantasmas sobre el SOLTAR del otro, ocultos hasta el veredicto. Es información asimétrica temporal
sin rol fijo (cualquiera puede mirar y cualquiera puede construir) y da a la mesa algo que hacer
juntos que no es repartirse la geografía.

Las dos piezas son órganos de F1 («Días sin manos»), no una dirección: cambian la frase de esta
(«di lo que vas a conseguir; la física te dice cuánto vale; constrúyelo antes de que se acabe el
día») y hay que juzgarlas dentro de la que las reciba.

## 7. La prueba más barata que la mata (corregida; hay que correrla aunque se descarte)

La de §8 del documento no puede matar la dirección: el registro del autor ya está en el montaje
(`MontarAlambique` pinta el serpentín; `MontarCarbonera` abre la boca en (101,223)), así que el autor
es R0 y no puede cobrar; el presupuesto es asimétrico (5 de frío contra 31; 3 cortes contra 40); la
gradación se falsifica sola con bordes y familias transitorias; y nadie dice quién pinta los
fantasmas del autor ni cuándo. Versión corregida, consensuada con las otras dos lentes, **2 semanas
sin diario ni J**, sobre `Correr` con `Montaje` compuesto (base + registro) y muestreo por día:

- Montajes sin la solución dentro: cámara alta con caldera y sin serpentín; recinto sellado con hogar
  y sin boca. El registro del autor son las 31 celdas de frío y el corte de la boca, **con el mismo
  presupuesto** que los monos (31 de frío para todos, o serpentín de 5) y una gramática que gaste los
  cortes.
- Semántica honesta: máscara de familias vista en el día; fantasma de región 3×3 con ≥ 5 de 9;
  familias estables (agua en reposo, carbón, ceniza, terracota, sedimento, humedad por banda) aparte
  de las transitorias (fuego, humo, hielo).
- Fantasmas del autor pintados **antes** por regla («los productos para los que se diseñó el
  montaje»), no después de ver la corrida.
- Torneo: R0, rociador (×17 en toda celda posible), repetidor de un truco, mutantes del registro del
  autor, autor. Y la clasificación de cada acierto del autor como **cercado** (el aire de la celda no
  alcanza el borde sin cruzar una celda del registro) o **abierto**.

La mata: (a) < 10 % de las celdas de aire en ×2-×8 sobre familias estables tras excluir el
parpadeo de R0; o (b) el autor, con presupuesto simétrico, no acierta 3 fantasmas de ≥ ×4; o (c)
≥ 80 % de su paga viene de cercados, o el repetidor de un truco alcanza el 50 % del autor; o (d)
moteado > 30 %. Aunque pase las cuatro, mi lente no cambia: demuestra que la banca cotiza
conocimiento, que es lo que F1 necesita de ella como validador y como precio del pedido invertido,
no que describir sea jugar. La prueba de papel (cinco personas, cuatro mapas impresos) pasa a la
sesión de F1 con la pregunta cambiada: no «¿pintan donde el tablón paga?» sino «¿eligen el pedido
caro o el barato?».

## 8. Tiempos corregidos e iteración humana

- **Evidencia para matarla: 2 semanas** (el arnés, 4-5 días de Opus; montajes sin solución,
  gramática simétrica, máscara y región, torneo y cercado/abierto, el resto; ~35 min de banco; un día
  de lectura). No se pierde: es la misma máquina de «K perturbaciones» que la cuna y F1 necesitan.
- **Prototipo feo que permita juzgar el core: 8 semanas de calendario**, no 6: J en su cadena
  secuencial (diario → `CorrerSello` → cláusula; el volcado para el sobre cerrado), 4-4,5 de banca,
  fantasmas, visores y presupuesto, más una capa de cliente que hoy no existe (cursor sin muñeco,
  pincel de tres ejes, render de cuota y de fantasmas: nadie ha jugado nada de esto) y una semana de
  semántica del fantasma en banco que no estaba en la tabla.
- **Iteración humana oculta, nota 5, en la puerta**: la gramática de los monos y el presupuesto por
  situación son el balance con otro nombre y se reabren con cada truco nuevo; la regla de puntuación
  (castigo, tope, K) son mandos que decide alguien mirando; la semántica del fantasma cambia todas
  las cuotas; el pincel de tres ejes; la lámina del visor (G6); y la premisa entera (examen o apuesta)
  que ningún banco decide. Cuatro tardes si la gramática acierta a la primera; un bucle sin fin si no.
  Lo que se automatiza de verdad, y es mucho: las cuotas, la validez de cada situación y mutante, el
  orden de campaña, la regresión de cada ley (recotizar de noche), la detección de copias y de
  divergencia entre máquinas, el buscador de exploits.

## 9. Órganos a conservar

1. **La banca como validador continuo**: la gradación y la explotabilidad como cifras sustituyen el
   «K perturbaciones deben dar veredictos distintos» de la cuna; el corredor de registros al azar
   sobre `Montaje` es el fuzzer de situaciones y la regresión de cada ley nueva.
2. **El pedido invertido**: el fantasma pintado antes de la obra, cotizado por la banca desde el t0,
   con la obra consumiendo días del horizonte; el jugador escribe la cláusula de J y la física la
   cotiza (§6).
3. **El que mira, apuesta**: el fantasma como verbo del espectador en host y espejo, oculto hasta el
   veredicto; asimetría temporal sin rol.
4. **El visor de posibilidad** («qué tiende a hacer la física aquí») y **el visor sin manos** (R0
   por día como fantasmas grises): el tutorial sin texto de cualquier dirección con SOLTAR.
5. **El sobre cerrado y el recibo compartible** (rejilla llena/hueca): asíncrono por fichero y el
   órgano de Wordle a coste cero; con la **escalera por entropía y sensibilidad ±20 % de `LabParam`**
   como orden de campaña sin diseñador.

## 10. Rúbrica v2

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 5 | una ley de juicio que recotiza todo pero no crea una decisión dentro de la simulación: las nuevas son meta (dónde pintar, cuánto ambicionar) y el techo ×17 las aplana |
| las leyes ejecutan, revelan y juzgan | 6 | ejecutan; el visor de posibilidad revela algo nuevo; juzgan contra un prior humano y puntúan la descripción, no la máquina |
| iteración humana (10 = poca) | 5 | gramática y presupuestos = balance por proxy; castigo, tope y K; semántica del fantasma; pincel; lámina; examen o apuesta |
| verificabilidad automatizable | 8 | casi todo en banco; la gradación se falsifica con bordes si no se corrige |
| la simulación es el juego | 3 | el verbo central no toca la grilla; el marcador mide lo dicho; preparar se pausa; una apuesta es un juego tradicional encima |
| onboarding garantizable | 6 | escalera calculada y sin manos como tutorial; «Gota» contradice el hecho 4; el pincel sin resolver |
| observabilidad | 7 | visor de posibilidad y sin manos son lo mejor del documento; con K = 16 el mapa se motea donde el agua decide |
| tiempo como apuesta | 8 | literal; pero lo que se arriesga es un −1, no la máquina, y los días se paran mientras preparas |
| multiplayer emergente | 6 | creencias ocultas y «el que mira apuesta» son buenas ideas; construir juntos no tiene motivo propio; asíncrono válido |
| profundidad por leyes estables | 5 | cada ley recotiza, pero la profundidad del pronóstico se satura en los puntos ciegos del prior: aprender el prior no es aprender física |
| cuerpo del jugador | 2 | cursor, y C «opcional y tardío» |
| identidad comercial | 6 | «a que sí / a que no» es remate humano en compañía; en solitario, una cifra; sin cápsula; género pequeño |
| dificultad técnica (10 = fácil) | 7 | acotado; abiertos: cotizar en la máquina del receptor, pincel, semántica del día |

Puertas: iteración humana 5 (en la puerta), apalancamiento 5 (en la puerta). Ponderado 135.

## 11. Posición

**Descartar como dirección, sin esperar a la prueba.** Lo que la prueba de banco puede demostrar (que
la banca cotiza conocimiento) no toca el defecto de esta lente (que el verbo central es describir y
el marcador mide la descripción); y lo que sí lo toca (el pedido invertido) es otra frase y otro
bucle, que pertenece a F1. Se corre igual la prueba corregida de §7, porque la banca como validador y
como precio del pedido es un órgano que F1 va a usar y hay que saber si mide saber o rareza. Frente a
la primera versión de esta crítica, lo que cambia no es el análisis sino la decisión: una segunda
ronda tendría que probar o bien algo que no puede rescatar el core (el banco) o bien una dirección
distinta (el fantasma antes de la obra). Ninguna de las dos es «A que sí». La frase se le presta a
quien tenga el core, y la apuesta entre amigos, que es lo mejor que trajo, se juega sobre la máquina
de otro.
