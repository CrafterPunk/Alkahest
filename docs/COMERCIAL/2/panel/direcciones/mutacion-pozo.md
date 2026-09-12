# DIRECCIÓN · MUTACIÓN DE EL POZO · «EL POZO SELLADO»

*(Panel de direcciones, segunda pasada. Ángulo obligatorio: conservar de El Pozo lo que la evidencia
sostiene (flujos verticales, la lumbrera como común, el cuerpo, los tres verbos) y eliminar lo que la
función objetivo penaliza (estaciones, roles por estrato, bibliotecas de ruinas de autor). Leído
antes de escribir: `01_LEYES.md` entero, las refutaciones de sello, balanza, cuna y testigo,
`04_VEREDICTO.md`, P1 y P4 de `01_PROPUESTAS.md`, `03_MERCADO.md §4-5`, y `LabParams.cs` /
`SimStepper.Laboratorio.cs` para no inventar nombres. No existe `_huecos_y_combinaciones.md`.)*

## 0. La respuesta a la pregunta obligatoria, antes de nada

**¿Qué reemplaza a las estaciones como fuente de problemas sin autoría ni balance?** Tres cosas que
ya corren en el código y que la primera pasada trató como defectos o como decorado:

1. **Los relojes lentos que las leyes ya escriben.** La grava se colmata; el depósito de finos ciega
   cualquier fondo (`EsFondo` incluye al sumidero: una salida se cierra sola); la tolva se agota a los
   466 s; la planta muere en 10 s sin savia; el humo persiste 255 ticks y come luz. Toda máquina que
   hoy «funciona» tiene una fecha de caducidad escrita por una ley, no por un evento. La estación era
   un parámetro que alguien movía; el reloj lento es la misma tensión sin nadie que la mueva.
2. **El tramo nuevo.** El pozo se cava hacia abajo, tramo a tramo. Lo que el tramo nuevo produce
   sube (humo, vapor, calor) a los tramos sellados de arriba; lo que los sellados entregan cae al
   nuevo. El jugador es su propia estación: cada vez que abre un tramo cambia las condiciones de todo
   lo que ya había resuelto. En co-op, el que abre el tramo nuevo es el vecino.
3. **La promesa.** SOLTAR deja de ser un contador y pasa a ser una **entrega medida** al tramo de
   abajo: «este tramo deja pasar ≥ 120 unidades de agua clara por día». La condición del sello (J) se
   escribe con lo que una **línea de aforo** ya cuenta en el suelo del tramo.

**¿Puede SOLTAR y el juicio por métricas ser el motor del pozo?** Sí, con una condición: la métrica no
puede ser un marcador. Un contador de días sin manos es un examen; una promesa cuya materia el tramo
de abajo (o el amigo que lo cava) **va a recibir en litros** es un juego, porque el juez no es una
tabla: es tu siguiente problema.

## 1. Nombre y frase de diez segundos

**El Pozo Sellado.** «Cava un pozo tramo a tramo. Cada tramo que sellas sigue corriendo sin ti y le
entrega al de abajo lo que prometiste: agua, luz, calor. Las leyes cobran la promesa y cuentan los
días.»

## 2. La experiencia narrada

**El mundo.** Una columna de 288 × 768 celdas (la rejilla actual girada; mismas 221 k celdas), dividida
en ocho **tramos** de 96 filas separados por suelos de roca. La boca del cielo arriba, el manantial en
el tramo 1, roca caliente y el sumidero verdadero (la balanza de J) en el fondo. El ambiente sube con
la profundidad (`CellGrid.ambient` por fila, un dato del constructor: 5 °C arriba, 45 °C abajo): arriba
el frío y la luz sobran y el agua es la que baja; abajo el calor es gratis y todo lo demás llegó de
arriba. No hay tres estratos con dueño: hay un gradiente y una sola pregunta por tramo, qué dejo pasar.

**Las manos.** El muñeco heredado (2×4, frasco, cincel) con los tres verbos: **tallar** (quita sólido,
deja grava), **verter** (el frasco: un fluido o un polvo), **poner** (un bloque de lo que lleva encima).
Un cuarto gesto y solo uno: **prometer**, tocando la **piedra de aforo** que hay en el suelo de cada
tramo. La piedra muestra lo que ayer cruzó el suelo (agua clara y turbia, calor con signo, finos,
carbón, ceniza, semilla, humo que subió, luz en la garganta) y lo que hoy vive en la grilla (planta
viva en tal sitio, terracota, grava colmatada). Eliges una o dos columnas, el número por defecto es el
de hoy, y prometes. Desde ese tick el tramo está **sellado**: no hay barrera física; sellado significa
que el contador de días de ese tramo cuenta, y que cualquier toque tuyo en sus filas lo pone a cero.

**Los primeros diez minutos (campaña, tramo 1: AGUA).** Un manantial brota en la pared izquierda, baja a
saltos, se enturbia al pasar por una veta de sedimento y forma una poza sobre el suelo de roca. Sobre
la poza, un lecho de grava. Nada explica nada; el cuerpo lo hace: el muñeco cae en la poza, sale
tintado de barro y su halo de piel (la vista Piel, siempre encendida: doce celdas de calor y humedad
alrededor del cuerpo) marca frío y mojado. Minuto 2: talla una celda en la orilla y el hilo cambia de
rumbo. Minuto 4: toca la piedra de aforo. Dice «hoy: 0 clara, 0 turbia, nada ha cruzado» y una
promesa de autor: «≥ 100 unidades de agua clara por día». Minuto 5: talla la garganta, tres celdas de
suelo bajo la poza. Toda la poza cae de golpe al tramo 2 (aún vacío, rocoso), y la piedra marca 300
turbias, 0 claras. Minuto 7: vuelve a tapar la garganta con la grava que sacó (poner): el agua se
filtra, sale clara, la piedra marca claras. Minuto 9: la grava se oscurece, la marca de claras baja
día a día (un día son 60 s: `DiaTicks` 1800). Minuto 10: entiende que la grava se cansa y que el agua
quieta se aclara sola; talla una poza de decantación río arriba. La promesa se cumple el día 6.

**La primera hora.** Tramo 2, FUEGO: una bolsa de fibra y una piedra de hogar. Promesa de autor:
«carbón ≥ 30 cruzando por día». Descubre el aire de contacto (tapar la pila la ahoga, humea, deja
carbón) y, de paso, que **su humo sube por la garganta y oscurece el tramo 1**: la primera culpa es
contra sí mismo, y el agua clara de arriba no sufre, pero la piedra del tramo 1 muestra «humo: 40» y él
sabe que si hubiera prometido una planta arriba la habría matado. Tramo 3, FRÍO: núcleo frío, poza
tibia, «goteos ≥ 60 por día»: el alambique, y la sombra del serpentín (R148). Al final de la hora
tiene tres promesas corriendo, tres contadores, y una decisión que ya le duele: el tramo 4 pide luz y
agua, la garganta bajo la lumbrera trae las dos juntas, y el goteo moja columnas.

**Hora 5 (sandbox, pozo sorteado).** Cuatro tramos sellados. Cava el 5 y encuentra una bolsa
envejecida: una pila que carbonizó sola hace 40 000 ticks y una poza que decantó a 6 de carga (la
mitad de la cuna que sobrevivió: montajes de autor de 15 líneas, mutados por sorteo y envejecidos por
simulación antes de que nadie entre). Enciende un horno en el 5. El calor atraviesa el suelo y el
tramo 4 pierde su promesa de planta viva por sequedad del lecho: la piedra del 4 se raja, visible desde
la lumbrera. Decisión real: subir y romper el sello del 4 (perder 38 días) o enfriar el 5 (mover el
horno, abrir la garganta para que el humo se vaya). Mientras decide, SUELTA el pozo entero: ×10, imagen
desaturada, vistas de calor y humedad activas (la vigilia de Sordina), y ve morir la promesa del 2 el
día 48 porque la tolva se agotó: nadie le lleva fibra al fuego. Ahí nace el deseo de las horas
siguientes: que el huerto del 4 alimente el fuego del 5 sin manos (siega y arrastre, paquete V).

**Hora 20.** Ocho tramos. El pozo es una cadena: el agua del 1 llega al 8 clara y tibia; la luz baja
por siete gargantas alineadas y dos cuñas de vidrio (paquete L); la fibra del 4 cae al tolva del 6; el
humo del 6 sale por una chimenea tallada fuera del eje de la luz. La promesa emblema, «planta viva en
el fondo el día 30», se cumple o no. Cesar compara su pozo con el de su hermano (misma semilla): recibo
por tramo, dominancia columna a columna, y el `diff` de los dos registros dice en qué tick divergieron.
O recibe por fichero el pozo del hermano con los tramos 1-4 sellados y cava el 5 con lo que aquel
prometió.

**Qué hace mientras la simulación corre.** Cava el tramo siguiente a ×1 (un día por minuto: los
tramos de arriba envejecen mientras trabaja), o SUELTA a ×10 y mira: la vigilia es lectura, no espera.
Cada día cerrado deja una marca en las piedras; cada volcado diario alimenta el time-lapse.

**Cómo lee el mundo.** El cuerpo primero (Piel, tinte de mojado, tizne y tos al cruzar humo). Una
vista de campo a la vez (calor, humedad, luz, en las bandas de `LabBandas`). La piedra de aforo como
recibo diegético. La piedra rajada como veredicto visible a distancia. El diff entre dos sellos como
«hash hecho visible». Después, la huella (M): se lee lo ocurrido.

**Con 2-3 personas.** Todos cavan el mismo tramo abierto; no hay estratos con dueño. Lo que se hace
juntos es **prometer y soltar**: «¿sellamos el 3 ya?» es la discusión; «no toques el 3» es la regla
que nadie escribió; subir a romper un sello para arreglar el agua de todos es una decisión con coste
visible y culpa con recibo (el registro dice quién tocó qué y en qué tick). Información asimétrica
temporal: cada uno lleva una vista de campo distinta. Primero asíncrono por fichero; host + espejo
después, no ahora.

**Cómo aprende.** Una **campaña de seis tramos de autor** (agua, fuego, frío, huerto, horno, fondo),
cada uno un montaje del banco de 12-15 líneas, envejecido por simulación, con promesa de autor y
validado por veredicto sin personas (registro vacío falla, registro del autor cumple, K mutaciones
miden fragilidad). El orden lo da la profundidad: no hay paisaje que explorar, hay un tramo debajo.
Después, el pozo sorteado: tramos mutados de esas familias y promesas que escribe el jugador.

## 3. Core loop

**En una frase:** cavar un tramo con lo que cae de arriba, prometer lo que dejarás pasar, sellarlo y
cavar el siguiente, mientras lo que sube del nuevo y los relojes lentos ponen a prueba cada promesa.

```
        cae del tramo k-1: agua (clara/turbia, tibia/fría), luz por la garganta, finos, fibra
                                     |
      TRAMO k ABIERTO: tallar · verter · poner      <- el cuerpo lee (Piel), una vista de campo
                                     |
      PROMETER en la piedra de aforo: «>= N clara/día», «planta viva en (x,y)», «carbón >= n/día»
                                     |
      SELLAR (SOLTAR local): días sin manos y días cumpliendo empiezan a contar
                                     |
      CAVAR k+1: abrir la garganta = abrir el grifo de la promesa (todo lo que decantó cae)
                 ^                                  |
   sube del tramo nuevo: humo, vapor, calor         v
   + relojes lentos: colmatación, tolva, savia -> la piedra marca, se raja, o aguanta
                                     |
      SOLTAR global (x10, vigilia) -> recibo diario -> comparar por dominancia / diff de registros
```

## 4. Por qué explota mejor la simulación

**Ejecutan.** Los tramos sellados no son un guardado: corren. El agua que prometiste es agua que
cae; el humo que hiciste es humo que sube. La cadena de suministro vertical es física, no un menú de
comercio: la única bomba es el sifón y la única cinta transportadora el arroyo.

**Revelan.** La línea de aforo convierte cada suelo en un recibo por material, dirección, temperatura
y carga: la culpa vertical de la primera pasada («ese barro es tuyo») pasa de intuición a cifra con
fecha. El registro tick-estampado del sello atribuye sin etiquetas de procedencia: rejugar sin las
entradas de B y mirar el diff es la atribución exacta.

**Juzgan.** La promesa es la condición como dato de J, con una diferencia que quita el único tuning
que el refutador del sello encontró (los umbrales de autor): en el sandbox el umbral lo pone el
jugador, y su valor no lo da una tabla sino el tramo de abajo. En la campaña el umbral es de autor,
pero lo valida el banco. Comparar es dominancia por columna, sin escalar.

**Contenido sin autor.** Los problemas de las horas 5-20 los produce el propio pozo: relojes lentos,
tramo nuevo, vecino. Las situaciones salen de envejecer por simulación montajes que ya existen (el
banco tiene nueve) y mutarlos por sorteo, no de escribir cuarenta ruinas.

**Validación sin personas.** Round-trip del volcado contra los hashes del alambique; conservación del
aforo (Σ lo que cruzó == diferencia de inventario por tramo); validador por veredicto para cada tramo
de campaña y cada mutante; anillo de hashes cada 256 ticks; G3 en un día.

## 5. Qué añade al sustrato

| pieza | qué es | cruza con | coste (Opus) | tuning |
|---|---|---|---|---|
| **J · Juicio entero** | cola bajo las seis puertas, volcado/carga (primer fichero de partida), `CorrerSello`, condición como dato, cláusulas sobre la grilla, `LabBandas`, anillo de hashes, balanza en el fondo | todo lo que mueve un contador | 5-6 sem | 8-9 |
| **Línea de aforo** (nuevo) | por tramo, una fila; toda celda que la cruza en `SwapCells`/`Move`/`LabAguaFluyo` se cuenta por material, dirección, `temp−ambiente` y `carga`; `luz[]` de sus celdas vacías muestreada por día. Contadores, no física: hashes intactos | agua, presión, decantación, gas, luz, polvos | 3 días sobre la cola | 9 |
| **Tramos** (nuevo, builder) | rejilla girada a 288×768; suelos de roca cada 96 filas; `ambient` por fila; piedra de aforo como objeto del nivel; el sumidero verdadero solo en el fondo | térmica (evaporación y helada por profundidad), luz por gargantas alineadas | 3-4 días + G3 | 8 |
| **Bolsas envejecidas** | la mitad de la cuna que sobrevivió: envejecer N ticks headless montajes de autor mutados por sorteo; validador por veredicto; `Clonar` en memoria | erosión, colmatación, cocción, carbonización, germinación | 1,5 sem sobre J | 7-8 |
| **C · Cuerpo** | `CuerpoSim` sensor, Piel, tinte, tizne y tos, brasa viva en el frasco; el muñeco entra en el registro por las puertas | calor, humedad, humo, fuego transportable | 1-1,5 sem | 7 |
| **L · Luz** (segunda ola) | el día que remide Q16; haz y cuña con vidrio; vidrio que transmite y suda; insolación por rayos | la luz como flujo enrutable entre tramos; huertos hondos | 3-3,5 sem | 7-8 |
| **A · Aire, entrega A** (segunda ola) | aire que se gasta: la llama inmortal muere en un tramo sin garganta abierta; B solo si la prueba de dos días mide tiro | fuego, humo, garganta como común de aire | 1 sem (+0,5 B) | 6-7 |
| **V · Vida** (tercera ola) | siega, raíz que busca, arrastre por agua, compost | el huerto de arriba alimenta el fuego de abajo sin manos | 2,5-3 sem | 6-7 |

Primera ola (prototipo feo): J + aforo + tramos + bolsas + C: 8-9 semanas de Opus, 6-7 de calendario
con J ‖ C y tramos ‖ aforo. M y F después, si la dirección los pide (F para el fusible de hielo como
temporizador de promesa; M para leer lo que pasó en un tramo sellado).

## 6. Principal riesgo de diseño

**Que un tramo sellado sea eterno.** Hoy el manantial es perpetuo, el hogar es perpetuo y el frío es
un pin infinito; el único sistema mortal es la planta y es el único que nunca vivió. Un tramo de solo
agua, bien filtrado, cumple su promesa para siempre: el contador cuenta y nada lo amenaza. El motor
depende de que haya relojes (colmatación, tolva, savia) y amenazas desde abajo (humo, calor) en tiempo
de juego. Si la colmatación tarda cuatro horas y el humo no cruza la garganta, el pozo es «nueve
sandboxes apilados con un cronómetro», que es la crítica que la lente vertical ya hizo a P1. El
riesgo segundo es el del examen: que prometer y esperar se sienta como rendir cuentas. La mitigación
no es balance: es que la promesa sea materia que alguien recibe, y que la campaña tenga promesas de
autor mientras el sandbox las deja escribir.

## 7. Cuánto depende de iteración humana

**Lo que va al banco y no a personas:** cada tramo de campaña (veredicto), la conservación del aforo,
el round-trip del sello, el coste vertical, las mutaciones sorteadas, los relojes lentos (cuántos
días tarda en colmatar una grava de 20 celdas con 100 unidades/día: un escenario, un número medido,
ningún ajuste).

**Lo que exige playtest:** (1) el gesto de prometer (¿se entiende la piedra, la columna y el número
sin texto?); (2) si alguien sella voluntariamente antes de que el juego se lo pida; (3) si el día de
60 s se siente como día; (4) si la piedra rajada se lee a distancia; (5) el tamaño del tramo y el ancho
de la garganta. Estimación: cuatro sesiones de 40 minutos con Cesar más una persona sobre el prototipo
feo, y una reescritura de la campaña entre la segunda y la tercera. Ninguna constante física se toca
por sensación: se ajustan gesto y tamaño. Nota: 7/10.

## 8. La prueba más barata capaz de matarla

**Hoy, con el banco actual, un día.** Escenario «dos tramos»: arriba el montaje de R148 (serpentín y
lecho bajo una boca de siete columnas); abajo una carbonera de 20×20 con boca de 1, unidas por una
garganta de tres celdas bajo la boca del cielo; 18 000 ticks (diez días). Medir por día `luz[i+W]`
sobre el lecho de arriba, `temp` del suelo del lecho, `LabPlantasNacidas/Muertas` arriba; contra la
misma pila con la carbonera sin yesca. **Mata:** si con la carbonera ardiendo la luz sobre el lecho no
baja de 40 ningún día y el suelo de arriba no sube 8 raw, el tramo nuevo no amenaza al sellado en
tiempo de juego, el pozo es sandboxes apilados y el motor es un contador. Segunda prueba, un día más,
sobre el mismo escenario: el sumidero provisional del tramo 1 cegado por depósito y la grava colmatada,
medidos en días; si ambos tardan más de 60 días (una hora a ×1), los relojes lentos no son relojes de
juego. Prueba de ingeniería en paralelo: G3 (288×768 con un tramo de agua entero, ≤ 3 ms/tick).

## 9. Tiempos

| tramo de trabajo | qué | semanas |
|---|---|---|
| **evidencia para matarla** | escenario «dos tramos» + relojes lentos + G3, banco actual, sin código nuevo salvo montajes | 0,5 |
| **técnico automatizable y paralelizable** | J (5-6) ‖ C (1-1,5); tramos + `ambient` por fila (0,5) ‖ aforo (0,5, tras la cola); bolsas y validador (1,5, tras `CorrerSello`) | 8-9 de Opus, 6-7 de calendario |
| **dependencias secuenciales** | cola → aforo → condición → validador → campaña de seis tramos; el gesto (piedra, prometer, contador, vigilia ×10) al final | dentro de las 6-7 |
| **prototipo feo que permite juzgar el core** | tres tramos de campaña + un pozo sorteado, F8 apagado, muñeco heredado, piedra como sprite | 6,5 |
| **iteración humana probable** | cuatro sesiones, una reescritura de campaña; segunda ola (L, A) solo tras decidir que el core vale | 3-4 semanas de calendario, no de Opus |

## 10. Lo que deja fuera y por qué

**De El Pozo:** las estaciones (eventos de parámetros: reemplazadas por §0); los tres estratos con
dueño (reemplazados por el tramo abierto y el sello como acto de grupo); la biblioteca de 40-60
ruinas y las 12-15 cámaras (reemplazadas por nueve montajes del banco envejecidos y mutados); el macro
de autor (reemplazado por el gradiente y el sorteo); el cuaderno que dibuja (la piedra es el
cuaderno); los pozos con condición por semilla (todo tramo es uno); la cuerda de fibra y el sonido por
aire abierto (después, si V y audio entran); el bautizo (no cruza nada aquí).

**De las otras familias absorbe:** SOLTAR y el contador (P4), la cámara sellada con veredicto diferido
(P3, por tramo y no por partida), el cuaderno falsable y los turnos de agua (P2: la promesa es la
afirmación falsable y el agua que cruza el aforo es el turno), la vigilia (P5), el censo de vida como
columna (P6), el asíncrono por fichero. **Descarta:** seres lectores y sondas con radio (contenido
disfrazado), la lámpara (destruye el dilema de la garganta bajo la luz), las salidas múltiples de la
balanza (solo el fondo), el testigo como material (sus cláusulas viven en la promesa), la puntuación
escalar, y el multiplayer online ahora.

**Si muriera en §8**, sus órganos valen para otra dirección: el aforo y la promesa escrita por el
jugador sirven a cualquier juego de situaciones de un recinto; las bolsas envejecidas son el generador
de cualquier campaña; la piedra rajada es el veredicto diegético que ninguna familia tenía.

## 11. Autoevaluación (rúbrica v2)

| eje | nota | por qué |
|---|---|---|
| apalancamiento_sistemico | 8 | tres días de aforo y una fila de roca convierten cada ley existente en fuente de problemas y en juez; ninguna física nueva en la primera ola |
| leyes_ejecutan_revelan_juzgan | 9 | corren los tramos sellados, el aforo revela con fecha, la promesa juzga sin umbral de autor en el sandbox |
| iteracion_humana | 7 | gesto, tamaño de tramo y campaña se afinan con cuatro sesiones; la física no |
| verificabilidad_automatizable | 9 | todo lo de la primera ola tiene prueba de banco con hash |
| simulacion_es_el_juego | 8 | la promesa es una capa de datos sobre la simulación; el riesgo del examen existe y está nombrado |
| onboarding_garantizable | 8 | seis tramos validados por veredicto; el orden lo da la profundidad |
| observabilidad | 7 | Piel, una vista, la piedra y el diff; la vista Ojo y la huella llegan en olas posteriores |
| tiempo_como_apuesta | 9 | sellar es prometer; abrir la garganta es abrir el grifo de la apuesta |
| multiplayer_emergente | 7 | sellar juntos y romper sellos ajenos, sin roles; asíncrono casi gratis; host + espejo no ahora |
| profundidad_por_leyes_estables | 7 | depende de que A y V hagan mortales el fuego y productivo el huerto; sin ellos, los tramos de agua son eternos |
| cuerpo_del_jugador | 6 | sensor, tinte, tos y brasa en el frasco; la poza que cae sobre el cuerpo al abrir la garganta es el accidente que el cuerpo compra |
| identidad_comercial | 7 | frase y clip claros (la garganta que se abre; la piedra que se raja); la columna con contador sigue pareciendo un falling-sand con cronómetro hasta que L pinte la luz |
| dificultad_tecnica | 7 | todo acotado; la rotación de la rejilla y el aforo son triviales; J es la pieza larga y ya está refutada dos veces |
