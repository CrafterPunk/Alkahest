# CRÍTICA · «El Pozo Sellado» (mutacion-pozo) · lente: ITERACIÓN HUMANA OCULTA

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: productor. Único tema: cuánta
iteración humana esconde la dirección (playtest, tuning, balance, contenido de autor, contingencias
ante construcciones arbitrarias del jugador) y qué parte se sustituye por banco headless, hashes,
generación y validación automática. Leído entero: `mutacion-pozo.md`, `01_LEYES.md`, las cuatro
refutaciones del paquete J (sello ×2, balanza, cuna), `03_MERCADO.md` §2-4, CHECKPOINT §6b y §9
(R148, R150). Código leído, no citado de memoria: `LabParams.cs` (registro entero), `LabBench.cs`
(escenarios, `MontarTolva`, `MontarCarbonera`), `SimStepper.Laboratorio.cs` (`LabAgua` :440-495,
`LabInfiltrarHacia` :497-520, `LabPlanta` :789-906, `LabManantial`/`LabTragar` :1004-1035,
`LabLuz` :1177-1270, `LabDifusionTermica` :1303-1345), `SimStepper.cs` (`ProcessGas`). Toda
aritmética sobre el código va marcada como tal: es para el banco, no para creerla.)*

## 0. Veredicto: SEGUNDA RONDA

| campo | valor |
|---|---|
| **veredicto** | segunda ronda |
| **riesgo mayor (esta lente)** | que los «relojes lentos que las leyes ya escriben» no existan a escala de juego y haya que FABRICARLOS con constantes: `DiaTicks` (no está en el código), altura del tramo, ancho de garganta, gradiente de `ambient`, `LuzDecayCielo`, cantidad de fibra por tramo. Eso es balance de un espacio acoplado disfrazado de física, exactamente lo que la función objetivo penaliza, y es lo que decide si la nota de iteración es 6 o 4 |
| **iteración humana oculta** | 16-25 días de personas hasta poder juzgar el core (frente a «cuatro sesiones de 40 min y una reescritura»): calibración de la escala temporal (2-3 vueltas), 6 tramos de campaña con registro del autor resueltos y re-resueltos tras cada ola de física, 2-3 vueltas de la UI de la piedra, curación de bolsas envejecidas, la definición de «clara», y la sesión del examen que ningún hash sustituye. Nota real 6, no 7; y 4 si el banco dice que los relojes hay que fabricarlos |
| **se automatiza en su lugar** | tabla de relojes en días por escenario; cláusula de ETERNIDAD en el validador; barrido de escala temporal (`DiaTicks` × tramo × garganta) impreso como tabla; umbral de campaña derivado del percentil del recibo del autor; registro del autor como SCRIPT de intervenciones tick-estampadas (revalidación gratis tras cada ola); gramática de promesa restringida a integrales diarias del aforo (sin parpadeo); predicados de interés sobre las bolsas; conservación del aforo como assert |
| **prueba que la mata** | «tramo eterno» + «dos tramos» con tres emisores (carbonera de boca 1, pila abierta, hogar bajo suelo de 2/4/8 celdas) + tabla de relojes; un montaje, cero física nueva |
| **semanas hasta evidencia para matarla** | 1 |
| **semanas hasta prototipo feo** | 4,5 con la rodaja barata (7 si se sigue «J entero primero») |

La dirección es, de todo el panel, la que menos balance humano necesita POR DISEÑO: en el sandbox
el umbral lo escribe el jugador y lo cobra su propio tramo siguiente; no hay temporadas, ni eventos,
ni biblioteca de ruinas, ni comportamiento humano específico; ante cualquier construcción arbitraria
del jugador no hay contingencia que prever, porque la línea de aforo cuenta lo que sea y el tramo de
abajo lo recibe. Por eso no la descarto. No la hago finalista porque su motor («los relojes lentos y
lo que sube del tramo nuevo ponen a prueba cada promesa») no está en el código a escala de juego, su
propia prueba de §8 está montada con el emisor que no emite, y su plan gasta 5-6 semanas de J antes
de la primera sesión que podría decir «examen». Una semana de banco decide entre la nota 6 y la 4;
hasta entonces la nota 7 declarada no está ganada.

## 1. Lo que el código dice de los relojes que la dirección da por hechos

El §0 de la dirección sustituye las estaciones por «los relojes lentos que las leyes ya escriben»
(colmatación, depósito, tolva, savia, humo). Leí cada uno. Aritmética sobre el código, para el banco:

- **Colmatación de la grava** (`LabInfiltrarHacia` :505-512): `rate = 32·perm·libre²·fraccion/255⁴`,
  `finos = rate·carga·100/25500`. Con grava (perm 90) y el agua del manantial (`TurbidezFuente` 40):
  `rate` 11 al principio, `finos` 1 por visita; en cuanto `rate` cae a 6 (carga de la grava ≈ 54),
  `finos` trunca a 0 y la colmatación SE PARA. Son ~54 visitas de 8 ticks: **catorce segundos** de
  fase rápida y después un punto fijo eterno con la mitad del caudal inicial. No hay meseta lenta
  ni día 40: hay un transitorio de un cuarto de minuto y luego nada, salvo agua muy turbia.
- **Depósito** (:466-489): agua con carga ≥ 200 y reposo ≥ 24 sobre cualquier fondo se vuelve
  sedimento. El propio comentario de `sed.turbidezFuente` (LabParams :188) dice «la poza se ciega en
  unos minutos»; R148 midió 21 256 depósitos en 18 000 ticks. Es rápido, no lento, y solo actúa con
  agua turbia: un tramo bien decantado entrega clara y no ciega nada.
- **Tolva y carbonera** (`LabBench` :146-155, :231-237): 360 y 400 celdas de fibra. La tolva dura
  466 s; R148: «el fuego se consume en los primeros cinco minutos». El reloj del tramo de fuego es
  la CANTIDAD DE FIBRA que el autor puso: un número de contenido por tramo, y la promesa «carbón ≥ 30
  por día» es un pico el día 1 y cero el resto, salvo que alguien lleve fibra a mano, que rompe el
  sello. Sin V (siega y arrastre; el huerto que la alimentaría nunca vivió) el tramo de fuego no
  tiene promesa que se pueda mantener.
- **Savia** (:889-906): 40 visitas = 11 s. Es un fusible, no un reloj.
- **Humo**: `VidaHumo` 255 ticks (510 bajo techo); el panel midió carbonera de boca 1 = 0 humo fuera.
- **Luz por gargantas** (`LabLuz` :1226-1235): la pasada descendente pierde `LuzDecayCielo` (1) por
  celda de aire: 255 al cielo, ~159 al suelo del tramo 2, ~63 al del 3, cero en el 4. «Luz por
  siete gargantas alineadas» y «planta viva en el fondo el día 30» son imposibles con la ley actual;
  posibles con `LuzDecayCielo = 0` (un slider, una decisión de balance) o con el haz de L.
- **Calor por el suelo**: `ambient[]` tira ±1 raw cada 32 ticks (:1337-1341): un pin de un raw por
  segundo contra el que la conducción (`KRoca` 2) tiene que ganar a través de un suelo cuyo grosor
  la dirección no fija. Se mide, no se discute; pero el §8 no lo especifica.
- **Agua**: manantial perpetuo a 24 celdas/s (1 440 por día de 1 800 ticks), único sumidero en el
  fondo, conservación exacta. En régimen permanente (R148: desde t≈3 000 los niveles no se mueven en
  15 000 ticks) todo tramo deja pasar exactamente lo que entra. Toda promesa de agua es eterna a
  partir del minuto dos.

Conclusión de la lente: entre el minuto 2 y la eternidad no hay ningún reloj de ley. Si el pozo
necesita promesas que mueran entre el día 5 y el 40, alguien tendrá que ELEGIR `DiaTicks`, la
altura del tramo, la garganta, el gradiente, `LuzDecayCielo`, `ColmatacionPct` y la fibra por
tramo para que ocurra. Seis o siete constantes acopladas que se reeligen tras cada sesión son un
bucle de balance, pequeño pero exactamente del tipo que la función objetivo penaliza. La salida
honesta no es tuning: son leyes que hagan mortal el fuego (A «el aire se gasta», D «el hogar come»)
y productivo el huerto (V), y la dirección las manda a la segunda ola.

## 2. La iteración humana que el §7 no cuenta, pieza a pieza

| pieza | declarado | real (días de personas) | por qué |
|---|---|---|---|
| Escala temporal (`DiaTicks`, tramo, garganta, gradiente, `LuzDecayCielo`, fibra por tramo) | «ninguna constante física se toca por sensación» | 3-4, en 2-3 vueltas | no son constantes físicas, son arquitectura; pero fijan si las promesas mueren en tiempo de juego, y se reeligen tras cada sesión salvo que el banco imprima la tabla (§3.1-3.3) |
| Campaña: 6 tramos con registro del autor | «una reescritura» | 4-8 | el validador exige que el registro del autor cumpla: alguien RESUELVE cada tramo; con dos reescrituras, 12-18 resoluciones. Y cada ola de física (L mueve `HashLuz` en nueve escenarios; A cambia la combustión) caduca todos los registros: si son partidas de Cesar, se rejuegan; si son scripts, cuestan cero (§3.4) |
| Tres de seis tramos sobre física sin verificar | no aparece | secuencial, antes de cualquier autoría | huerto: Q16 sin remedir (7/73 caras; con luz, 9 plantas mueren, R150); frío: el alambique probablemente graniza (nadie contó `Freeze`); fuego: llama inmortal y tolva de 466 s. Son un día, una tarde y una semana de Opus, pero están DELANTE de la campaña y el §9 los pone detrás |
| Gesto de prometer (piedra: columnas, número por defecto, una o dos cláusulas) | listado en §7 | 3, en 2-3 vueltas | es la UI de la que depende todo; «el número por defecto es el de hoy» tomado en régimen permanente promete algo eterno; tomado antes, algo inalcanzable: cuándo se muestrea el defecto es semántica que solo una sesión decide |
| Definición de «clara» | no aparece | 1 | un umbral de carga (la refutación de la balanza: `AguaClaraCargaMax` 16, cruzado con `TurbidezFuente` 40, `Decantacion` 6, `DepositoUmbral` 200); decide si «decantar antes» existe como decisión |
| Bolsas envejecidas | «validador por veredicto» | 1-2 | el validador garantiza que discriminan, no que interesan; alguien mira muestras de seis familias |
| El examen (¿prometer y esperar es jugar?) | nombrado | 2, incomprimibles | 2-4 sesiones con Cesar más una persona; ningún hash lo adelanta |
| Legibilidad sin F8 (piedra rajada a distancia, tinte, vigilia ×10) | implícito | 2 | láminas con desconocidos (el G6 del veredicto anterior); hoy todo se ve con F8 y las plantas son un píxel |
| Toque accidental en tramo sellado (co-op) | no aparece | 1 | un cincelazo en el tramo 4 borra 38 días: confirmar, o que el tramo sellado rechace toques; diseño de control con sensación |
| La mano (alcance, presupuesto de toques) | pospuesta a después del playtest | 0 ahora | correcto; el sello binario «tocó / no tocó» no necesita la economía de toques de «Sin Manos» |

**Suma: 16-25 días de personas**, en 4-5 semanas de calendario interleavadas, frente a ~5 días
declarados. Sigue lejos del playtest infinito (no hay economía, ni temporadas, ni biblioteca), y
casi todo es gesto, campaña y escala, no constantes físicas. **Nota real: 6.** Si la prueba de §4
dice que los relojes hay que fabricarlos con constantes: **4**.

## 3. Qué se automatiza en su lugar (y la dirección no propone)

1. **Tabla de relojes en días.** Por escenario del banco, `Informe` imprime «días de N ticks hasta:
   colmatación 50 % / punto fijo, cegado del fondo, tolva vacía, planta muerta, humo extinguido».
   Medio día de Opus. Elegir `DiaTicks` con esa tabla delante es una decisión, no una iteración.
2. **Barrido de escala temporal.** Los mismos montajes corridos con `DiaTicks` ∈ {900, 1800, 3600},
   tramo ∈ {64, 96, 128}, garganta ∈ {1, 3, 5}, imprimiendo el día en que muere cada promesa de
   campaña. Un día de banco en serie; convierte 2-3 vueltas humanas en una lectura.
3. **Cláusula de ETERNIDAD en el validador** (dos líneas sobre el validador por veredicto de la
   cuna): el montaje del autor SIN TOCAR debe FALLAR antes del día D, y con el registro del autor
   debe CUMPLIR. Ningún tramo eterno entra en la campaña; «sandbox apilado con cronómetro» se
   detecta sin jugarlo. Añadir el «día de decisión» (desde el día d, K perturbaciones de una celda;
   el primer d en que todos los veredictos coinciden) como proxy de aburrimiento.
4. **Registro del autor como script**, no como partida: una lista de intervenciones tick-estampadas
   en código, como la caldera del alambique en `LabBench`. Es lo único que hace que la campaña
   sobreviva a las olas L, A y V sin que Cesar rejuegue seis tramos por ola; y es la mayor ahorro
   de personas de toda la dirección.
5. **Umbral de campaña derivado**: percentil 70-80 del recibo diario del registro del autor, nunca
   tecleado. Un número humano menos por tramo.
6. **Gramática de promesa restringida a integrales diarias del aforo** («Σ clara cruzada/día ≥ N»,
   «Σ carga cruzada/día ≤ X» en vez de «clara»): la integral no parpadea; las cláusulas de grilla
   («planta viva en (x,y)») oscilan con la humedad del lecho (43-125 en R148) y exigirían histéresis
   por tipo, que es tuning. Que «clara» sea un número del jugador, no del autor.
7. **Predicados de interés sobre las bolsas envejecidas** (agua presente, luz en el suelo ≥ x, al
   menos un reloj armado) para reducir la curación a muestreo.
8. **Piedra rajada binaria**: rajada = la promesa falló hoy; sin «N días fallando» que afinar.
9. Lo que la dirección ya trae y es correcto: conservación del aforo como assert (Σ cruzó == Δ
   inventario por tramo), round-trip del volcado contra los hashes, anillo de hashes, G3, un día de
   `LabBench` en IL2CPP antes de prometer el fichero que circula.

## 4. La prueba más barata que la mata (corregida)

Un montaje, cero física nueva, una semana de Opus contando lecturas:

- **(a) Tramo eterno** (medio día): un tramo de agua (manantial, poza, lecho de grava, garganta de
  3) con un contador de agua y carga que cruza una fila (veinte líneas, hashes intactos), 54 000
  ticks. **Mata** si el recibo diario tiene variación < 5 % desde el día 3 y ningún reloj lo mueve
  después del día 5: la única promesa de la primera ola es eterna y la sesión del examen se jugaría
  sobre un contador sin amenaza.
- **(b) Dos tramos con tres emisores** (un día): el montaje de R148 arriba; abajo, por turnos, la
  carbonera de boca 1 (la de §8), la pila abierta de la sala (8×10) y un hogar de 170 bajo suelos de
  2, 4 y 8 celdas; 18 000 ticks; por día, `luz[i+W]` sobre el lecho, `temp` del suelo del lecho,
  nacidas/muertas. **Mata** si ninguno de los tres baja la luz de 40 ni sube el suelo 8 raw ningún
  día: el tramo nuevo no amenaza al sellado y el pozo es J con un cronómetro por tramo. La versión
  de §8, solo con la carbonera, mataría por montaje (0 humo fuera del recinto, 01_LEYES §3).
- **(c) Tabla de relojes** de §3.1 sobre los nueve escenarios (medio día) y G3 en paralelo.

Si (a) y (b) matan a la vez, la dirección no es una dirección: es un envoltorio de J que necesita
A, D y V antes de poder juzgarse, y sus órganos pasan a «El Recibo». Si (b) acopla y (c) muestra al
menos un reloj entre el día 2 y el 40, vuelve como finalista con la superficie de balance más
pequeña del panel.

## 5. Tiempos corregidos y el orden que cambiaría

| tramo de trabajo | dirección | corregido |
|---|---|---|
| evidencia para matarla | 0,5 sem | **1 sem** (tres montajes, contador de fila, tabla de relojes, G3) |
| prototipo feo que permite juzgar el core | 6,5 sem tras J entero | **4,5 sem de Opus, ~4 de calendario** con la rodaja barata |
| iteración humana hasta juzgar el core | 4 sesiones + 1 reescritura | **16-25 días de personas**, 4-5 sem de calendario |
| técnico total si la dirección vale | 8-9 primera ola + 7-8 segunda | igual, pero A y D entran en la primera |

**Rodaja barata (4,5 semanas de Opus):** tramos + `ambient` por fila (0,5) ‖ aforo como contadores
(0,5); promesa MÍNIMA (una cláusula sobre el aforo, contador de días, el toque pone a cero; sin
diario ni volcado: 0,5); A «el aire se gasta» (1) + D «el hogar come» (0,5): UN sistema mortal que el
jugador alimenta; C sensor y Piel en paralelo (1); piedra como sprite (0,5); tres tramos escritos
como script con umbral derivado. Después, dos sesiones de 40 minutos deciden «jugar o examen». Solo
entonces J entero (diario, volcado, `CorrerSello`, validador con cláusula de eternidad), bolsas,
campaña de seis, L y V. J entero antes de esa sesión es construir seis semanas de tribunal antes del
primer juicio; lo que se pierde por posponerlo (comparar por fichero, validar sin personas, culpa
con recibo en co-op) no hace falta para responder la única pregunta que ningún banco responde.

## 6. Órganos a conservar si muriera

- La **línea de aforo** (contadores puros sobre `SwapCells`/`Move`/`LabAguaFluyo`, hashes intactos):
  sirve a cualquier juego de recinto con entrada y salida.
- La **promesa escrita por el jugador** con umbral derivado del recibo, nunca tecleado: quita el
  balance de cualquier dirección de situaciones.
- La **cláusula de eternidad** y el **día de decisión** del validador: detectan sandboxes con
  cronómetro en cualquier campaña sin jugarla.
- El **registro del autor como script** en código: campañas que sobreviven a los cambios de física.
- El **barrido de escala temporal** como herramienta del banco.
- La **piedra rajada** como veredicto diegético binario legible a distancia.
- El **tramo nuevo como estación** («lo que abres cambia lo que ya resolviste»): vale para cualquier
  mundo de cámaras encadenadas.
- Las **bolsas envejecidas** con predicados de interés: generador de cualquier campaña.

## 7. Puntuaciones (rúbrica v2)

| eje | nota | por qué |
|---|---|---|
| apalancamiento_sistemico | 7 | tres días de aforo y una fila de roca convierten cada ley en fuente de problemas y en juez; pero la primera ola no añade física y el motor necesita A, D y V |
| leyes_ejecutan_revelan_juzgan | 7 | ejecutan; revelan con fecha; juzgan sin umbral de autor en el sandbox; hoy juzgan un régimen permanente, así que el juicio es un contador hasta que algo sea mortal |
| iteracion_humana | 6 | 16-25 días de personas: escala temporal, campaña con registros, piedra, «clara», bolsas, examen; poco para un juego, más de lo declarado; 4 si los relojes hay que fabricarlos |
| verificabilidad_automatizable | 8 | toda la primera ola tiene prueba de banco; faltan la cláusula de eternidad y el barrido de escala; el examen no se verifica |
| simulacion_es_el_juego | 7 | la promesa es una capa de datos sobre la simulación; cuatro constantes de arquitectura que no están en el código son el diseñador escondido |
| onboarding_garantizable | 6 | el validador garantiza discriminación, no aprendizaje; tres de seis tramos esperan física sin verificar; la luz no llega al tramo 4 |
| observabilidad | 6 | nadie pinta luz; plantas de un píxel; Piel y una vista; la piedra rajada es una promesa sin línea |
| tiempo_como_apuesta | 9 | sellar es prometer y abrir la garganta es abrir el grifo: el mejor SOLTAR del panel |
| multiplayer_emergente | 6 | «¿sellamos el 3?» es la conversación buena; lo demás no se prueba hasta host + espejo; asíncrono casi gratis |
| profundidad_por_leyes_estables | 5 | agua eterna, fuego como cuenta atrás de fibra de autor, planta que nunca vivió; la profundidad llega con A, D, V y L, 8-10 semanas más |
| cuerpo_del_jugador | 6 | sensor, tinte, tos, brasa; la poza que cae sobre el cuerpo al abrir la garganta es el accidente que el cuerpo compra |
| identidad_comercial | 7 | frase y remate nombrables («la piedra que se raja»); el clip necesita luz pintada |
| dificultad_tecnica | 8 | todo acotado y verificable; rotación y aforo triviales; J refutado dos veces; G3 es un día |

## 8. Crítica razonada

**Desde la silla del productor, lo que hace bien.** Es la dirección del panel que menos balance
humano necesita por diseño. El umbral, que es donde el tuning suele esconderse, lo escribe el jugador
y lo cobra su propio tramo siguiente; no hay temporadas, ni eventos, ni biblioteca de ruinas, ni
comportamiento humano específico que guionizar; ante cualquier cosa que el jugador construya no hay
contingencia que prever, porque la línea de aforo cuenta lo que sea y el tramo de abajo lo recibe.
Comparar es dominancia por columna, sin escalar. Las situaciones salen de envejecer nueve montajes
que el banco ya tiene. Eso es lo que la función objetivo pide y por eso no la descarto.

**Lo que esconde, y por qué no la hago finalista todavía.** El motor que sustituye a las estaciones
son «los relojes lentos que las leyes ya escriben». Los leí uno a uno y ninguno vive entre el minuto
dos y la eternidad. La colmatación de la grava, con la turbidez del manantial, se para por
truncamiento entero en unos catorce segundos (carga ≈ 54) y queda en un punto fijo; el depósito
ciega «en unos minutos» según el comentario del propio parámetro y solo con agua turbia; la tolva y
la carbonera son 360-400 celdas de fibra puestas por el autor y se consumen en cinco u ocho minutos,
así que el reloj del tramo de fuego es una cantidad de contenido por tramo y «carbón ≥ 30 por día»
es un pico el día uno y cero después, salvo que alguien lleve fibra a mano y rompa el sello; la
savia mata en once segundos; la luz del cielo pierde uno por celda de aire y muere en el suelo del
tramo tres, así que «siete gargantas alineadas» y «planta viva en el fondo el día 30» son imposibles
sin poner `LuzDecayCielo` a cero o sin el haz de L; el agua, con manantial perpetuo, sumidero único y
conservación exacta, es eterna desde el minuto dos (R148: 15 000 ticks sin mover un nivel). Si el
pozo necesita promesas que mueran entre el día 5 y el 40, alguien tendrá que
elegir `DiaTicks` (que no existe en el código), la altura del tramo, la garganta, el gradiente de
ambiente, el decaimiento de la luz y la fibra por tramo para que ocurra, y reelegirlos tras cada
sesión. Es un bucle de balance pequeño, pero es balance de un espacio acoplado disfrazado de física,
y decide si mi nota es 6 o 4. La salida honesta no es tuning: son las leyes que hacen mortal el fuego
(A, D) y productivo el huerto (V), que la dirección manda a la segunda ola.

**La iteración que el §7 no cuenta.** Suma 16-25 días de personas frente a unos cinco declarados:
la escala temporal en dos o tres vueltas; seis tramos de campaña que exigen un registro del autor,
es decir, que alguien los resuelva, y que caducan con cada ola de física (L mueve nueve hashes); tres
de esos seis tramos sobre física sin verificar (Q16, `Freeze`, la llama inmortal), que son baratas
pero están delante de toda autoría y el plan las pone detrás; la UI de la piedra y la semántica del
número por defecto (tomado en régimen permanente promete algo eterno); la definición de «clara»; la
curación de bolsas; el toque accidental en co-op; y la sesión del examen, que ningún hash adelanta.
Sigue siendo poco para un juego. Nota real: 6.

**Lo que se automatiza en su lugar.** Una tabla de relojes en días por escenario y un barrido de
escala temporal (`DiaTicks` × tramo × garganta) que convierten vueltas humanas en una lectura; la
cláusula de eternidad en el validador (el montaje sin tocar debe fallar antes del día D) y el día de
decisión por K perturbaciones, que detectan el sandbox con cronómetro sin jugarlo; el registro del
autor como script de intervenciones tick-estampadas, como la caldera del banco, que es el mayor
ahorro de personas de toda la dirección porque revalida la campaña gratis tras cada ola; el umbral
de campaña como percentil del recibo del autor, nunca tecleado; la gramática de promesa restringida a
integrales diarias del aforo, que no parpadean, dejando «clara» como número del jugador; predicados
de interés sobre las bolsas; la piedra rajada binaria.

**La prueba y el orden.** La prueba de §8 mataría por montaje: la carbonera de boca 1 emite cero
humo fuera del recinto. La versión honesta cuesta lo mismo: tramo eterno (un tramo de agua con
contador de fila, 54 000 ticks), dos tramos con tres emisores (carbonera, pila abierta, hogar bajo
suelos de 2, 4 y 8 celdas) y la tabla de relojes; una semana. Si mata dos veces, la dirección es un
envoltorio de J y sus órganos pasan a «El Recibo». Si pasa, el orden debe cambiar: J entero antes de
saber si prometer es jugar son seis semanas de tribunal antes del primer juicio. La rodaja barata
(tramos, aforo, promesa mínima sin diario, A más D como único sistema mortal, C en paralelo, tres
tramos como script) da un prototipo feo en cuatro semanas y media, y dos sesiones deciden «jugar o
examen» antes de pagar J, bolsas, campaña, L y V. Con eso, y con los relojes medidos, vuelve como el
finalista con la superficie de balance más pequeña del panel.
