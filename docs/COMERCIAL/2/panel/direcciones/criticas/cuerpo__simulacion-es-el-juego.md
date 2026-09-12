# CRÍTICA · «PRIMERA PIEDRA» (cuerpo) · lente: LA SIMULACIÓN ES EL JUEGO

*(Panel de direcciones, segunda pasada con la función objetivo de Cesar, 2026-09-12. Reescritura
completa de la crítica de la mañana, sin protegerla. Crítico: director creativo purista de la
simulación. Leído entero: `cuerpo.md`; `01_LEYES.md` (hechos 3, 7, 8 y 9; §6: contradicción (a) y
«los verbos del jugador»); `panel/leyes/cuerpo.md` (C1-C4) y la refutación de apalancamiento de
`humo-respirado`, donde nace el «tapón»; `03_MERCADO.md` §2-§4; `campana-situaciones.md` §1-§4 (la
dirección con la que esta dice competir); las dos críticas hermanas del cuerpo (ingeniería e
iteración humana) para no repetir su trabajo: aquí no se recuenta el coste de la máscara ni las
rondas de control. Código: `SimStepper.cs` (`Step` :257-300, `ProcessIfNeeded` :352-380, la lengua
de fuego que nace en el vacío de encima :880-884, `ProcessGas` :1443-1520); `SimStepper.Laboratorio.cs`
(`LabVecinoVacio` :298, `LabGotear` :317, `LabPresion` :1080-1150: BFS sobre `Water`, superficies por
`Empty` encima, destino `dst + W`); `ApprenticeController.cs` (`ModoMovimiento` :643, velocidades
:672-674, caja :796-798, chaflán :814, `CajaChoca` :905-924); `Flask.cs` (`SuckRatePerTick 30` :102,
`ReachWorld 6f` :107); `LabParams.cs` (`ReposoMovil 3` :63); `SimRenderer.cs` (`CellWorldSize 0.1`
:21). Aritmética usada: celda = 0,1 u; paso 1,5 u/s = 15 celdas/s = 0,5 celda/tick; carrera 26
celdas/s; salto 10,5 u/s en el impulso = 3,5 celdas/tick; caja 6,4 × 11,2 celdas con chaflán de 3;
frasco 30 celdas/tick = 900 celdas/s, a 12 celdas si `ReachWorld` baja a 1,2 u.)*

**Veredicto: DESCARTAR como dirección; ascender el órgano al sustrato.** El cuerpo como sólido
dentro del tick es la mejor idea sobre el avatar que ha producido el panel y la única que hace que el
muñeco viva dentro de la simulación en vez de encima. Pero la dirección construida alrededor tiene un
verbo diseñado para abandonarse (su propio contador puntúa con cero el acto que le da nombre), un
objeto exento de las leyes en el centro de un juego que promete que las leyes mandan, y un
conmutador («a pie») que es un juego de plataformas puesto encima para fabricar escasez de presencia.
El documento lo admite en §10: «su valor real es de capa». Una capa no es una dirección. Esta crítica
le toma la palabra, dice qué órganos suben al sustrato con qué prueba, y nombra la única dirección del
cuerpo que sí merecería refutación aparte.

## 1. Lo que hace bien, y nadie más hace: el avatar dentro del tick

Todas las demás direcciones tratan al muñeco como un cursor con patas que escribe por las puertas de
`AlkahestSim`. Esta lo convierte en una celda gorda: una máscara consultada donde las pasadas
preguntan `Empty`, la posición escrita antes de `Step()` como la caldera del alambique, `X0/Y0` en el
hash. No crea un fenómeno; crea **una piedra que decide**, y con eso cruza gas, líquido, polvo, luz y
fuego sin escribir una ley nueva y con tuning cero, porque bloquear es binario y las curvas ya están
medidas (HF1 para la tapa, `DesnivelMin` para la presa). Eso es literalmente lo que la función objetivo
llama apalancamiento: poco añadido, muchas decisiones. Y cierra por construcción la contradicción (a)
de `01 §6` (el cuerpo como intervención tick-estampada), que cualquier dirección con avatar y sello
tendrá que cerrar de todos modos.

Además resuelve lo que ninguna otra resuelve: la condición 1 del clip de `03 §4`, «estado legible sin
voz», sin F8 y sin arte nuevo. Un muñeco sentado sobre un agujero del que salía humo es legible para
cualquiera. Ese es el activo. Todo lo que sigue es sobre lo que se construyó encima de él.

## 2. El verbo diseñado para abandonarse

El loop es «ser la pieza → sentir → sustituirse → soltar». Pregunta purista: ¿qué produce la fase de
cuerpo que verter la piedra directamente no produzca? Con los números del código, **nada**, y por un
margen absurdo. El frasco aspira y vierte 30 celdas por tick (`Flask.cs:102`): un tapón de tres celdas
para la boca de la carbonera cuesta un décimo de segundo de verter; bajar el alcance a 12 celdas no
cambia el caudal. La información del halo se obtiene caminando hasta el sitio sin sentarse. Y la
dirección no toca `SuckRatePerTick`. Desde la segunda situación, ser la pieza está estrictamente
dominado por poner la piedra, y el documento lo narra él mismo: «hora 5: el cuerpo ya casi no se usa
como pieza; se usa como sonda».

Lo más revelador es que la dirección **codifica la dominación en su juez**: «DÍA N SIN CUERPO» puntúa
con cero cualquier día en que el cuerpo bloquee algo. El único mecanismo que ofrece contra «ser la
pieza es esperar» es que el juego te eche de tu verbo. Un juego cuyo contador castiga hacer lo que le
da nombre no tiene ese verbo como core: lo tiene como tutorial. Un tutorial excelente, de tres a
siete situaciones (tapa, presa, techo, corcho, sombra, esponja, termómetro), tras el cual el jugador
está jugando «Días sin manos» con un muñeco que anda. Que las siete estén contadas por el propio
documento dice cuánto dura el verbo.

## 3. El objeto exento: la excepción del diseñador

Para `ProcessGas`, `ProcessLiquid`, `ProcessPowder`, `LabRespira` y `LabLuzDesde` el cuerpo es roca.
Para `LabFlujoTermico` y `LabAire` es aire: las celdas de la caja conservan material y campos, y el
calor y el vapor difunden a través de ti como si no estuvieras. Opaco a la materia y a la luz,
transparente al calor y a la humedad, inmune al fuego («sentarte sobre una brasa la ahoga», y el
precio es un halo rojo), no lo empuja el agua que represa, no lo entierra el polvo que le cae encima,
no lo mueve nada. Ese objeto no existe en Noita ni en Powder Toy: en Noita el jugador es la cosa del
mundo que más obedece a la simulación, no la única que no. La lente cuerpo había rechazado las cuatro
reglas que hacían que el mundo le hiciera algo al muñeco; esta dirección se queda con la que hace que
el muñeco le haga algo al mundo y declara honestamente que «el riesgo para el cuerpo es delgado». La
honestidad es correcta y la consecuencia también: **un cuerpo al que las leyes no alcanzan no es «la
simulación es el juego»; es un muñeco-pared**. El jugador que se siente sobre la brasa sin pagar nada
dirá la palabra que mata sistémicos en la reseña: *fake*.

La función objetivo dice que «el cuerpo del jugador puede reaccionar físicamente al mundo (sin
barras)». Esta dirección le da a esa frase su lectura más barata: el mundo reacciona al cuerpo; el
cuerpo reacciona con un tinte.

## 4. «A pie» es un juego tradicional encima, con piel de conmutador

La dirección lo confiesa en §2: sin los tres conmutadores (a pie, 12 celdas, polvo en reposo es suelo)
«el cuerpo no necesita estar en ningún sitio». Es decir: el valor del verbo no emerge de las leyes,
se fabrica restringiendo al jugador. Un plataformas (salto de 22 celdas, `ImpulsoSalto` afinado en
nueve rondas R110→R121b, chaflán de 3 celdas para no engancharse) es exactamente la clase de juego
tradicional encima que la función objetivo prohíbe, y el que más iteración humana esconde del panel:
la crítica de iteración humana ya lo contó, aquí solo se nombra la categoría. Y «polvo en reposo es
suelo» no es un predicado: `ReposoMovil = 3` visitas son 24 ticks en los que el montón recién vertido
no sostiene; la «escalera de materia» es una escalera que tarda casi un segundo en existir y que
`Desenterrar` puede deshacer. Nada de eso lo afina un banco.

## 5. Lo que el código dice de la máscara

No mata el órgano; mata la cuenta de «una semana, tuning cero», y sobre todo mata la idea de que la
máscara es una sustitución mecánica. Es una decisión semántica por regla:

- **`LabPresion` no pregunta `Empty` para conectar.** El BFS del cuerpo de agua avanza por `mat ==
  Water` (:1080-1150); las superficies se eligen por `Empty` encima y el destino es `dst + W`. Con
  «las celdas de la caja nunca se reescriben», el agua que había en el canal cuando te plantas sigue
  siendo agua dentro de ti y conecta las dos orillas hasta que drena; la superficie más baja puede
  estar bajo tus pies y el destino dentro de tu caja. La «presa humana» es la prueba correcta
  precisamente porque el diseño tal como está escrito la suspende.
- **La materia también nace en vecinos vacíos**: la lengua de fuego en `aboveIdx` si es `Empty`
  (:882), `LabGotear` por `LabVecinoVacio` (:298-330), `LabNacerAgua`, `SpawnSmokeNear`. Sentarte sobre
  la pila puede llenarte de llamas por dentro si esos puntos no consultan la máscara; y si la
  consultan, el fuego bajo la tapa humana pierde su lengua, que es lo que la dirección quiere pero
  nadie ha medido.
- **La costura frame/tick es real y no es la de sentarse.** A ×10 los diez ticks del frame comparten
  posición: la tapa quieta es perfecta. A ×1, el impulso del salto son 3,5 celdas por tick: cada
  brinco deja celdas de gas dentro de la caja que luego «salen». La prueba de §8 usa un guion
  estático y no lo ejercita.
- **«La sombra humana produce los 7/73 de R148»** cita el número que el hecho 3 de `01` declara
  artefacto (se midió la celda de sedimento, que nadie lee). `LabLuz` rodea obstáculos estrechos: tu
  cuerpo sombrea solo si tapa la boca del cielo misma, y entonces es la tapa de gas con otro nombre.
  «Tapas el sol con la espalda», en la frase, es hoy falso.
- Los «26 puntos de `Empty`» son más de cuarenta según cómo se cuente; da igual el número: cada uno
  es una decisión de qué es el cuerpo para esa regla.

## 6. El diseñador escondido

- La **condición en la inscripción de piedra** («carbón ≥ 30 · planta viva»): una por situación,
  de autor, como en la campaña. Nada nuevo, pero nada automático.
- Los **papeles** son una biblioteca finita de siete; la cuna sortea geología, no papeles. «Todo
  hueco es un puesto» es cierto y vacío: los puestos son siempre los mismos cinco bloqueos.
- **El día.** Aquí «un día en cuarenta segundos a ×10» son 12 000 ticks; en la campaña, 1 800. La
  unidad del juez es un número humano y dos direcciones hermanas discrepan por 6,7×.
- **«Con cuerpo» por `BloqueosCuerpo`** está definido por la física, y eso es un mérito, pero es
  grueso: en una sala con humo cualquier tick te cuenta como pieza aunque no seas pieza de nada, y de
  pie sobre roca en aire quieto no cuentas nunca. La versión geométrica (caja ∩ región de la cláusula)
  que propone la crítica hermana es mejor y sigue sin umbral.

## 7. Minuto a minuto: no es un examen, es una sala de espera

Hay que reconocerlo: no es un examen. El minuto a minuto es físico (andar, sentarse, verter), no una
hoja de recibos, y el veredicto llega diferido. Pero el momento de más tensión es **estar quieto a
×10** mientras el mundo trabaja alrededor de un objeto al que nada le pasa. En Sin Manos la tensión de
no tocar tenía un precio (el contador); aquí no tocar y no moverse es gratis para el cuerpo y solo
cuesta días en un contador que, además, te empuja a levantarte. Lo divertido de la dirección está en
los tres segundos de levantarse (el humo sale a borbotones) y en los diez de sustituirse; lo que hay
entre medias es esperar sentado, y la única razón para no verter ya es que aún no sabes que se puede.

## 8. El clip y la frase

**El clip existe y es el mejor del panel en la condición 1.** 0-2 s: un muñeco camina por una cueva
hacia un agujero del suelo del que sale humo; encima, un lecho oscuro. 2-7 s: se sienta sobre el
agujero; el humo se corta; en corte, abajo, las llamas pierden la lengua y la pila ennegrece; arriba,
el velo (vista Ojo) se levanta del lecho a medida que el humo residual muere (255 ticks: 0,85 s a
×10). 7-10 s: se levanta; el humo le sale por las piernas y se tizna; dos palabras: **TAPA HUMANA**.
Coda de un segundo: vierte sedimento, se va, «DÍA 1 SIN CUERPO». Las cuatro condiciones se cumplen sin
F8 y sin arte nuevo, con una salvedad: «la semilla brota» no se ve con plantas de un píxel; el tercer
eslabón visible tiene que ser el velo o la pila ennegreciendo en corte, nunca la planta.

**La frase no.** Tres oraciones son un párrafo, y una de ellas («tapas el sol con la espalda») es
falsa en el código de hoy. La frase que el clip sostiene es de una línea: «Tú eres la primera piedra;
el mundo cuenta cuántos días aguanta la segunda». Y lo que esa frase vende es una primera hora, no un
juego: el tráiler vendería el tutorial.

## 9. Multijugador

«Sujeta esto mientras…» es el mejor «algo que hacer juntos sin roles» que ha escrito el panel: uno es
tapa mientras el otro talla la chimenea; la información asimétrica es física (cada uno siente su
halo) y la culpa es por ausencia. Dos peros: exige host + espejo (3-4 semanas, fuera del ahora), y el
cuerpo del invitado llega por transform replicado, con interpolación; cada temblor es una fuga. Por
fichero, que es lo que se puede ahora, el co-op se reduce a comparar sellos: deberes, no juego. Y en
cuanto la piedra cuesta un décimo de segundo, «sujeta esto» se dice una vez y luego se vierte.

## 10. Qué se automatiza en su lugar

La máscara vale más como **sonda del banco** que como verbo: con el cuerpo como guion `(tick) → (x, y)`,
aparcar la caja en cada celda transitable de una situación N ticks y medir el delta de veredicto
enumera los *puestos* sin autor. Para cada puesto, correr la sustitución por roca suelta, arcilla,
sedimento y vidrio y anotar cuántos días aguanta cada material es la **matriz de sustituibilidad**.
Esa matriz hace tres cosas sin personas: decide si el verbo está dominado (§11), produce las pistas
de cualquier dirección con avatar («aquí una piedra cambia el recibo») y ordena situaciones por leyes
implicadas, como ya proponen las K perturbaciones. El BFS a pie y el «con cuerpo» geométrico se
quedan tal cual. Todo C# en banco.

## 11. La prueba más barata que mata la dirección (no el órgano)

La de §8 de la dirección (carbón con cuerpo ≥ 90 % del de piedra; `DesnivelMin` sostenido; nueve
hashes intactos; tick ≤ +5 %) es buena y mata **el órgano**; corregida con `LabPresion` y los creadores
de materia dentro de la máscara, es la prueba con la que el órgano sube al sustrato. No mata la
dirección: la dirección muere aunque la máscara funcione perfectamente.

**La que la mata** (2-3 días de banco tras los 4 de la máscara, sin personas): la matriz de
sustituibilidad sobre los nueve montajes del banco más las seis situaciones del prototipo. Si en
≥ 95 % de los puestos algún material vertido desde una celda transitable a ≤ 12 celdas iguala o
supera los días del cuerpo, por construcción nada devuelve al jugador a ser pieza tras la primera
vez: descartar como dirección, conservar la capa. Si aparecen puestos irreemplazables (la arcilla se
cuece y raja, el sedimento se erosiona a 20 celdas/s, la roca suelta cae), hay un verbo que no se
agota y vuelve a segunda ronda como «sujeta esto mientras». Confirmación humana, una sesión con dos
personas tras el prototipo feo: de la cuarta situación en adelante, contar cuántas veces se sientan
antes de verter; menos de una de cada tres es un tutorial.

## 12. Tiempos corregidos

| | dirección | esta crítica |
|---|---|---|
| evidencia para matarla | 1 semana | **1,5 semanas**: 4 días de máscara semántica (con `LabPresion` y creadores de materia) + 2 de la prueba del órgano + 2-3 de la matriz, que es la que decide |
| prototipo feo | 4 sem Opus / 3 de pared | **5 sem Opus / 4 de pared**: máscara por regla, guion en `Correr`, seis situaciones con cláusulas sobre la grilla, BFS, y una sesión de traversabilidad a pie antes de montar nada; sin co-op |
| iteración humana | 3 sesiones en 3 semanas | **6-8 sesiones**: 2-3 de control a pie sobre polvo (precedente R110-R121b), 2 de «sin texto» para las tres primeras situaciones, 1 de láminas del halo, 1 de «¿sentarse es jugar?»; después una por 8-10 situaciones |

## 13. Iteración humana oculta

Menos de lo que parece y más de lo que dice. Cero tablas de balance, cero bibliotecas, cero eventos:
la física no se afina. Lo humano es control (a pie sobre polvo, la única sensación que ningún banco
mide y la que más rondas ha costado en este proyecto), campaña (siete papeles y una inscripción por
situación, «sin texto» verificado a mano), la unidad del día y el N mínimo de días sin cuerpo que
convierte sentarse en no-solución, y todo el co-op, que espera a la ruta A.

## 14. Órganos a conservar

1. **Cuerpo sólido**: máscara por regla (no por punto de `Empty`), posición como entrada del tick,
   `BloqueosCuerpo`, guion en `Correr`, `X0/Y0` en el hash; cierra la contradicción (a) para cualquier
   dirección con avatar y sello. Sube al sustrato con la prueba de dos días corregida.
2. **«Con cuerpo» definido por la física** (geométrico, sin umbral): columna del sello.
3. **Brasa viva en el frasco**: fuego con alcance real, cero parámetros.
4. **Barrido de puestos y matriz de sustituibilidad**: herramienta de banco para cualquier dirección
   con avatar.
5. **Halo Piel** (sensor, paquete C) y **vista Ojo** (ya en L).
6. **Validador a pie** (BFS) y **K perturbaciones del guion del cuerpo**.
7. **Los tres conmutadores** como la «física del toque» que `01 §6` pide fijar en el prototipo feo de
   la dirección ganadora, con el aviso de que «polvo en reposo es suelo» no es un conmutador.
8. **El clip «TAPA HUMANA»** y «la primera máquina eres tú» como las tres a siete primeras
   situaciones de la dirección que gane.

## 15. La dirección del cuerpo que sí habría que refutar aparte

Existe una versión en la que la pieza humana no está dominada por la piedra y el cuerpo no es un
objeto exento: **la pieza que no puede quedarse**. Si el cuerpo pagara por bloquear (el calor que
sube por la boca que tapas, el agua que represa te enfría, el polvo que te cae te entierra) con UN
umbral leído de `LabBandas` (la misma banda que mata a la planta), y el banco midiera «aguantas N
segundos» en vez de afinarlo nadie, entonces ser la pieza sería un fusible humano: sujetar lo que
arde mientras el otro talla, y soltar antes de que te cueste. Ahí «sujeta esto mientras» es un verbo
de verdad, el cuerpo obedece a las leyes, y la tensión del minuto a minuto es física y no de
contador. Es lo que la función objetivo pide literalmente («el cuerpo reacciona físicamente al
mundo, sin barras») y lo que esta dirección descarta en §5 como «tuning de sensación». Las
refutaciones de C1-C4 midieron un cuerpo que no bloquea; un cuerpo que bloquea se sienta en la bolsa
que él mismo crea, y eso no está medido: es un día de banco (`temp[]` en la caja sobre la boca de la
carbonera a 0, 2 y 6 celdas del hogar). No es esta dirección y no debe evaluarse como si lo fuera;
es la única heredera que valdría un documento propio.

## 16. Rúbrica v2

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | la máscara es el mejor apalancamiento por línea del catálogo, pero compra onboarding y clip, no profundidad: no crea fenómeno y los papeles se agotan en siete |
| las leyes ejecutan, revelan y juzgan | 6 | ejecutan (la pieza está en el tick) y revelan (halo, bloqueos); juzgan solo con J; condición, papeles y día son de autor |
| iteración humana (10 = poca) | 6 | sin balance, cierto; escondida: a pie sobre polvo, «sin texto», N mínimo, el día; todo el co-op |
| verificabilidad automatizable | 8 | guion, hashes, equivalencia con piedra, BFS, matriz de sustituibilidad; lo que no se verifica es «a pie se siente» |
| la simulación es el juego | 5 | ningún juego tradicional declarado encima, pero el cuerpo es el único objeto exento de las leyes, «a pie» es un plataformas que fabrica el valor del verbo, y el core se consume solo |
| onboarding garantizable | 8 | su mejor eje: la primera máquina eres tú; las K perturbaciones dicen dónde importa el cuerpo; «sombra» está mal citada |
| observabilidad | 7 | el cuerpo es el instrumento más legible del panel y el halo es tacto; lejos sigue sin verse y la planta sigue siendo un píxel |
| tiempo como apuesta | 5 | sustituirse y soltar es SOLTAR, heredado de J; ser la pieza es lo contrario de soltar y el contador te echa |
| multiplayer emergente | 6 | «sujeta esto mientras» es lo mejor del panel para hacer juntos sin roles; exige online, el cuerpo remoto tiembla, y por fichero es comparar deberes |
| profundidad por leyes estables | 4 | lo admite el documento: prototipo → piedra por ley y luego se agota; la profundidad es del sustrato |
| cuerpo del jugador | 6 | instrumento pleno; como cuerpo, una roca con piernas: la cláusula de Cesar en su lectura más barata |
| identidad comercial | 7 | «TAPA HUMANA» y «LA PRESA ERAS TÚ» son los mejores clips del panel; la frase de tres oraciones sobra y una es falsa; el tráiler vende una primera hora |
| dificultad técnica (10 = fácil) | 7 | acotada; lo sutil es la máscara como decisión por regla (`LabPresion` sobre `Water`, creadores de materia, `SwapCells`), el salto a 3,5 celdas/tick y el cuerpo remoto |

**Posición.** Descartar como dirección y ascender el órgano. A la pregunta final («¿pueden las leyes
ejecutar, revelar y juzgar solas, de modo que la profundidad crezca más rápido que el coste?») esta
dirección responde mejor que ninguna para UNA cosa, el cuerpo como pieza dentro del tick, y peor que
ninguna para la profundidad: por diseño se agota a la hora 5 y su contador lo certifica. Es la mejor
primera hora que el sustrato puede tener, y así hay que construirla: dentro de la dirección que gane,
con la máscara en el sustrato y la matriz en el banco. Si alguien quiere una dirección del cuerpo, es
el fusible humano, y hay que refutarlo aparte.
