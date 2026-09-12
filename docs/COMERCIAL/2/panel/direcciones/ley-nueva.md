# DIRECCIÓN · LA LEY NUEVA · «TIRO»

*(Panel de direcciones, segunda pasada. Ángulo: la dirección que nace de una ley nueva. Ley madre: el
paquete A del catálogo refutado (aire que se gasta, aire que fluye condicionado, bolsa en
`LabPresion`, vapor advectado), más un día de L (vidrio transparente) porque es lo único que separa
el aire de la luz. Leído: `01_LEYES.md`, las lentes de aire, luz, agua y métricas, las refutaciones
de `aire-que-fluye`, `aire-atrapado` y `adveccion-calor-vapor`, `03_MERCADO.md`, y `SimStepper.cs`
:853-919: la sordina consume a un cuarto, calienta a la mitad, no saca lengua, echa un cuarto del
humo, y de ella sale el carbón.)*

## 1. Nombre y frase

**Nombre de trabajo:** TIRO (alternativa: *La segunda boca*).

**Frase de diez segundos:** «Un taller cavado bajo una sola boca al cielo, donde el aire se acaba:
cada fuego respira lo mismo que tú, el humo enseña por dónde va el aire y cada agujero que abres
cambia todo lo demás. Cava, prende, suelta, y mira si tira.»

**El clip:** una pila de fibra arde en un cuarto cerrado; el humo se aplasta contra el techo; la llama
pierde la lengua, se encoge, muere (segundos 0-3). Una mano cincela una celda en el techo: el humo
escapa, la llama vuelve a medias (3-6). Cincela otra a ras de suelo en la pared de enfrente: el humo
se endereza en columna, la llama se inclina y ruge (6-9). Dos palabras en pantalla: «TIRA» (10).
Estado legible sin voz, tres eslabones físicos, consecuencia atribuible, remate nombrable.

## 2. La experiencia narrada

**Qué hace con las manos.** El jugador es el aprendiz-muñeco de 2×4 con frasco y cincel. Cinco verbos
existentes: cavar, apilar, prender (la brasa viva en el frasco, paquete C), sellar (arcilla que
compacta, roca suelta, agua) y abrir. Lo que cambia es que **cada agujero pasa a ser una válvula de
cuatro flujos**: luz que entra, aire que entra o sale, humo que sale, calor que se pierde. Hoy un
agujero al cielo decide una cosa; con el aire decide cuatro.

**Los primeros diez minutos (situación 1, «La vela»).** Un cuarto de roca de 14×10, nueve fibras
apiladas, la boca del cielo a tres celdas de roca por encima. El sello dice «DÍA 0 · 0 toques ·
condición: llama viva el día 2».

- *0:00-0:40.* Suelta la brasa sobre la pila. Lengua, humo que sube y se aplasta contra el techo. La
  vista Piel (halo de 12 celdas alrededor del cuerpo, siempre encendida) pinta el aire vecino en gris.
- *0:40-1:30.* La llama pierde la lengua: es la sordina, y se ve sin panel porque la sordina no saca
  lengua y echa un cuarto del humo. El halo se vuelve azul. La llama muere. Ocho segundos después el
  humo se ha ido; quedan dos celdas de carbón, cinco fibras intactas y una mancha. «DÍA 0 · falla».
  Primera sorpresa: el fuego se apagó solo, y el cuarto dice por qué.
- *1:30-3:00.* Abre el rayos X de aire (vista Presión: gris nominal, azul pobre, rojo denso): el
  cuarto entero es azul. Cincela una celda en el techo. El humo sale y por el mismo agujero baja una
  lengua gris, lenta: contraflujo. Vuelve a prender con la brasa del frasco. La llama vive a
  trompicones: lengua, sin lengua, lengua. Lectura: «un agujero no basta».
- *3:00-5:00.* Cincela una celda a ras de suelo en la pared opuesta. El humo se endereza en columna,
  la llama se inclina hacia la boca baja (la lengua nace en la diagonal a sotavento con `|vx| ≥ 2`:
  una línea). Vista Corrientes: flechas por bloque de 8×8, el grano en que `ProcessGas` ya decide.
- *5:00-6:00.* SOLTAR. El mundo corre a ×10; DÍA 1, DÍA 2. «DÍA 2 SIN MANOS · cumple».
- *6:00-10:00.* Situación 2, «La carbonera»: dieciséis fibras, condición «carbón ≥ 6 celdas en el
  suelo el día 3». Repite las dos bocas: ceniza, cero carbón. Vuelve al último toque (el sello rejuega
  el registro hasta el toque anterior: deshacer por determinismo). Prende abierto, espera a que la
  pila entera tenga llama y **sella la boca baja con arcilla a los veinte segundos**: sordina, el
  carbón se acumula. SOLTAR: DÍA 3, carbón 7, cumple. La primera máquina es un gesto con hora, y el
  sello lo guarda con tick.

El hueco que todos los críticos señalaron en la primera pasada (entre la primera sorpresa y la
primera máquina) mide aquí ocho minutos, porque sorpresa y máquina son el mismo objeto: un agujero.

**Hora 1.** Situaciones 3-6: «El humo sabe» (un huerto bajo la boca, un fuego debajo; el humo sube por
el pozo de luz y mata el huerto: la boca que alumbra y la que ventila compiten), «El respiradero» (un
manantial inunda la galería; la bodega sellada no se inunda porque la bolsa resiste, pero su fuego
muere), «La bomba» (subir agua con un hogar bajo una bolsa: el aire caliente expulsa, el núcleo frío
rellena), «El secadero» (la fibra mojada no arde; una llama y dos bocas la secan a sotavento).
Después, el sandbox: el mundo de 768×288 con una boca de siete columnas, envejecido por la cuna.

**Hora 5.** Máquinas compuestas. El horno deja de ser una caja sin boca (`MontarHorno`: 36 celdas
vacías que una fila de carbón agota en ~1 000 ticks): el vidrio pasa a ser «un recinto que respira sin
perder los 200 raw durante 60 visitas», y el ancho de su boca es la decisión. La trampa de agua: un
codo lleno de agua corta el aire; cuando se evapora o se drena, la chimenea arranca sola:
temporizador de SOLTAR hecho con agua. El invernadero: techo de vidrio (luz entra, aire no sale, el
vidrio suda y riega) frente a techo abierto (ventila, seca).

**Hora 20.** La red: chimeneas, trampas, bolsas, dos fuegos, un huerto, un alambique a sotavento. El
desafío que el jugador se pone solo: «DÍA 30 SIN MANOS con la fragua encendida y el huerto vivo».
Compara su registro con el de otro por fichero; el `diff` de `mat[]` dice dónde divergen.

**Mientras corre.** Mira el humo: es la veleta; la llama inclinada, el anemómetro; la lengua que
desaparece, la sordina; el nivel que se detiene a media bolsa, la presión. A ×10 camina por la red con
la vista Piel: el halo le dice si el cuarto donde está respira.

**Cómo lee el mundo.** Tres resoluciones: la simulación por celda; la materia (humo, llama, rocío,
nivel) por lo que se ve; las vistas Presión y Corrientes por bloque de 8×8, el grano en que el gas ya
lee la corriente. El cuerpo: tos y tizne al cruzar humo, jadeo del sprite en aire pobre, sin barras.

**Con 2-3 personas.** El mismo aire. Uno sella su carbonera y la fragua del otro entra en sordina: la
negociación nace de la física, no de roles. Algo que hacer juntos: uno cincela la chimenea mientras
el otro sostiene el fuego y canta lo que hace el humo. Asimetría temporal: quien está en el cuarto ve
el halo; quien está lejos, solo el humo. Asíncrono por fichero: se intercambian registros, no mundos.

**Cómo aprende.** Una escalera de ocho situaciones ordenada por leyes implicadas: A sola (1-2), A con
humo y luz (3), bolsa (4-5), B con vapor (6-7), todas (8: «la sala que se ahoga primero»). Cada una
es un montaje de 10-15 líneas más una condición como dato; la cuna la envejece por simulación (el
humo asentado, la terracota donde hubo fuego: la ruina amable sin biblioteca) y el validador por
veredicto la acepta o la tira sin que nadie la juegue. El sandbox es la situación grande sin horizonte.

## 3. Core loop

**En una frase:** abrir o tapar bocas y colocar fuego y agua → SOLTAR → el aire ejecuta (se gasta,
tira, empuja) → el humo revela → el sello juzga.

```
 cavar / tapar bocas · apilar · prender · sellar · trampa de agua
                 │  cada toque = entrada tick-estampada del sello
                 ▼
             SOLTAR (×10)
                 │
   ┌─────────────┴─────────────┐
   │ el aire se gasta  (A)     │
   │ el aire tira      (B)     │  EJECUTAN
   │ la bolsa resiste / empuja │
   └─────────────┬─────────────┘
                 ▼
   humo = veleta · llama inclinada o muerta · nivel de la bolsa · rocío       REVELAN
                 ▼
   DÍA N SIN MANOS · carbón entregado · goteos · agua subida · Σaire cuadra  JUZGAN
                 ▼
   volver al último toque (rejugar el registro) → preparar otra vez
```

## 4. Por qué explota mejor la simulación

**Decisiones nuevas, contadas.** Hoy un fuego decide dos cosas (dónde, con qué) y un agujero una
(luz). Con el aire como masa que se gasta y fluye:

| cruce | decisiones nuevas |
|---|---|
| Fuego × aire (7) | sellar para carbonizar o abrir para calentar; dónde va la segunda boca (baja tira, alta contrafluye); alto y sección de la chimenea; apagar cerrando en vez de mojar; bancar brasas cerrando a medias; de qué lado entra el aire (el humo va al contrario); qué cuarto se ahoga primero cuando arden dos |
| Agua × aire (5) | respiradero o bolsa; sellar la bodega contra la inundación y perder su fuego; subir agua con fuego o con el frasco; la trampa de agua como compuerta con hora; el serpentín a sotavento del vapor advectado |
| Luz × aire (3) | por qué boca sale el humo (la que alumbra el huerto o la otra); chimenea recta (tiro fuerte, humo lejos de la luz) o serpentina (calor aprovechado, humo que sombrea); techo de vidrio o abierto |
| Plantas y vapor × aire (4) | secadero con una llama y dos bocas; el huerto a barlovento o a sotavento del alambique; invernadero sellado o ventilado; el ancho de la boca del horno |
| Cuerpo × aire (2) | entrar en la sala llena de humo o esperar a que el tiro la vacíe; dónde estar cuando arde el taller |
| Entre personas (1) | quién sella qué, porque el aire es de todos |

Veintidós decisiones sobre un byte. La refutación fija A sola en 5 y A+B en 8 si el tiro vive; la
cuenta es la de A+B. Sin B caen las de vapor, luz y secadero y quedan trece: todavía más que
cualquier otra ley del catálogo a igual coste.

**Qué ejecutan.** `Σaire = Σ0 + inyectado − consumido` exacto en los nueve escenarios; consumo en
`ProcessCombustion` y `ProcessBrasa`; `LabRespira` por umbral en vez de por geometría; `ProcessFire`
con vida solo si hay aire (la llama inmortal muere en cuarto cerrado sin regla de chimenea); tiro por
presión `p = aire·(temp+120)` con sesgo hidrostático; bolsa por BFS de la región conexa (campana al
100 %, rama cerrada que no iguala, cámara que solo se inunda hasta el túnel, bomba de dos tiempos).

**Qué revelan sin panel.** El humo lee `viento` en vez del hash por bloque: el ruido que hoy «se lee
como viento y no tiene causa» pasa a tener causa. La lengua que desaparece, la llama inclinada, el
rocío a sotavento, el nivel que se detiene, la terracota que marca hasta dónde llegó el calor.

**Qué juzgan.** El sello: «DÍA N SIN MANOS» con cláusulas sobre la grilla («Fire en la región»,
«Water ausente en la bodega», «humedad ≤ 100 en la fibra») y sobre el libro («carbón entregado ≥ 6»,
«goteos por día ≥ 40»). La balanza: carbón que cae al sumidero. El cociente
`LabUnidadesRespiradas / LabCombustibleQuemado` como rendimiento de la carbonera. Todo contador que
el aire mueva entra en el recibo sin tocar nada.

**Contenido sin autor.** Cada geometría de agujeros es una máquina distinta, y el espacio lo acota la
conservación, no el balance: la sala no tiene tabla, tiene volumen. La cuna envejece los montajes; el
validador (registro vacío falla, K perturbaciones dan veredictos distintos, el del autor cumple)
decide si una situación vale.

**Qué se valida en banco.** «Chimenea con boca N» (`Σvy` abierta frente a tapada; aire medio de la
sala); «campana» (aire interior estable 3 000-9 000 ticks; con hogar ≥ 15 celdas expulsadas; con
respiradero, lleno); «hervidero» y «alambique» con `LabGoteos`; «laboratorio base» y «diluvio turbio»
intactos; horno con boca 1..4; ms por tick (+0,07 ms en el nivel real, medido por el refutador).

## 5. Qué añade al sustrato

| pieza | sem. Opus | cruza con |
|---|---|---|
| **A** byte `aire`, difusión con doble búfer, fuente en la boca, consumo, `LabRespira`/`ProcessFire` por byte, desplazamiento en `LabTransformar`/`LabNacerAgua`/`Transform` (la conservación que la refutación exigió), relajación lenta, auditoría, vista Presión | 0,7-1 | fuego, humo |
| **B** tiro por presión, `viento` posicional en dos bytes leído por `ProcessGas`; vapor advectado (seis líneas) | 0,5-1, condicionada | humo, vapor, secado, plantas, luz |
| **Bolsa** BFS de regiones en `LabPresion`; sellada y fría succiona, caliente expulsa | 0,75 | agua, fuego, frío |
| Re-balance de horno, carbonera y tolva contra el byte | 0,5 | fuego, vidrio |
| Render de humo y llama a escala, lengua a sotavento, vista Corrientes; jadeo, tos, tizne; brasa viva en el frasco (C mínimo) | 0,7 | cuerpo, observabilidad |
| Vidrio que transmite y suda + quinta pasada de `LabLuz` y remedir Q16 (L) | 0,4 | luz × aire × plantas |

Total propio: **3,5-4,5 semanas**. Infraestructura compartida con toda la pasada y usada entera: sello
(registro, volcado, `CorrerSello`, condición como dato, volver al último toque), balanza, validador y
cuna (J, 5-6 semanas, en paralelo: no toca la física).

**El único número humano.** La refutación señaló tres escalas acopladas en el byte (reserva de sala,
reposición local, fuente) y que «qué sala ahoga qué fuego» lo elegiría un humano contra las salas de
los jugadores. Respuesta: UN canon, como «clara» y «día» en J: *un cuarto sellado de 10×10 ahoga una
pila de nueve fibras en un día (1 800 ticks)*. De ahí el banco deriva `AireConsumo`, `AireRelaxTicks`
y el caudal de la boca con tres identidades (el cuarto sellado muere el día 1; la chimenea de boca 3
sostiene la misma pila hasta agotarla; cuatro pilas no asfixian el mundo de 768×288). Tres
incógnitas, tres identidades, cero balance.

## 6. Principal riesgo de diseño

**El común invisible.** El aire es un byte que nadie ve. La apuesta es que humo, llama y nivel de
agua bastan para leerlo; si a la escala de la cámara el humo de una celda no se lee como corriente,
el jugador percibe «el fuego se apagó solo» como fallo, la vista Presión se vuelve obligatoria y el
juego pasa a ser leer un panel: incumple la condición 1 del clip y la tesis de que la simulación es
el juego. Segundo, compartido con toda dirección de SOLTAR: que preparar, soltar y leer un recibo sea
rendir un examen; aquí lo mitiga que el reloj lo pone el fuego (se ahoga o se agota solo), no un
horizonte impuesto. Tercero, de ingeniería: que B no bombee; lo acota la prueba de dos días.

## 7. Cuánto depende de iteración humana

**Exige playtest** (Cesar, tres sesiones de una a dos horas en el prototipo feo): (a) si humo y llama
bastan para leer la corriente sin F8, jugando «La vela» sin panel; (b) si SOLTAR con un fuego que se
apaga solo es tensión o espera; (c) elegir el canon del cuarto de 10×10 una sola vez y la escala del
render del humo (dos o tres miradas). Después, una sesión corta por hito.

**Lo sustituye el banco:** la curva boca → respiración, `Σvy` abierta frente a tapada, Σaire, el
nivel de la campana, los goteos con vapor advectado, la boca del horno, los umbrales de las ocho
situaciones (los fija el validador), su discriminación y su envejecimiento. Ningún número de la
escalera lo elige una persona.

## 8. La prueba más barata capaz de matarla

1. **«Chimenea con boca N»** (dos días de banco tras la semana de A): sala 20×14, pila sobre hogar,
   chimenea 2×40, boca baja de 0/1/3/8; `Σvy` en la sección con la chimenea abierta frente a tapada a
   igual boca, y aire medio de la sala. **Mata:** `Σvy ≈ 0` o curvas indistinguibles. Sin tiro no hay
   segunda boca ni sotavento: la dirección tal como se titula muere, y A + bolsa (trece decisiones)
   pasan como sustrato a la dirección de «las leyes juzgan».
2. **«La vela» sin panel** (un día, una persona): Cesar juega la situación 1 con F8 apagado y se le
   pregunta por qué murió la llama y por dónde entró el aire tras la segunda boca. **Mata:** si no
   puede decirlo mirando humo y llama, el común es invisible y la dirección se retira dejando sus órganos.

## 9. Tiempos

- **Evidencia para matarla:** 1,5 semanas (A con el desplazamiento de conservación, B como spike de
  dos días con el escenario de la chimenea). Trabajo técnico puro, un solo hilo.
- **Prototipo feo que permita juzgar el core:** 6 semanas de calendario. En paralelo: A + B + bolsa +
  vapor + re-balance + render (3,5-4,5) ‖ sello + balanza + validador (4-5, sin tocar la física).
  Dependencia secuencial real: las ocho situaciones necesitan A y B para montarse y el validador para
  aceptarse; el camino crítico es J (5) más escritura y validación de situaciones (1). Los montajes
  los escribe Fable mientras Opus construye.
- **Iteración humana probable:** 4-6 horas de Cesar en el prototipo (§7) y una sesión por hito
  después. Lo incomprimible es la mirada sobre el humo a escala; el resto son identidades de banco.

## 10. Lo que deja fuera y por qué

- **Haz y cuña, insolación** (L): un segundo flujo enrutable antes de medir el primero dobla la carga
  geométrica del jugador. Si B vive, el cruce que los justifica es la **chimenea solar** (una celda
  calentada por el sol bajo la boca hace tirar sin fuego): siguiente paso, no este.
- **Hielo con reserva** (F): la bomba ya rellena con el núcleo frío existente. **Siega y arrastre** (V):
  el combustible de las situaciones es presupuesto; el sandbox lo pedirá. **Huella y hollín** (M):
  revelan, no crean. **Polvos al viento:** solo si B mide tiro («la chispa que sube»).
- **El cuerpo como leyes:** sin ahogo ni respiración en la campana; el cuerpo es instrumento. Lámpara,
  escarcha, clima que recuerda, testigo como material: fuera por las razones de `01`.
- **De la primera pasada:** estaciones, bibliotecas de ruinas y cámaras, roles por geografía, el
  cuaderno falsable (lo sustituye la condición como dato), el censo de vida y los bichos-sensor.
  **El Pozo** no se protege: sobrevive su forma (un mundo con una boca al cielo es la forma natural de
  un juego sobre aire), no sus estratos ni sus estaciones. Conservo sus órganos: SOLTAR y el contador
  de días (el reloj lo pone el fuego), la cámara sellada con veredicto diferido (el sello es el aire),
  los instrumentos que las leyes producen (el humo como veleta), el asíncrono por fichero, y «la
  semilla que dice por qué falló» convertida en «la llama que dice por qué murió» (el sello guarda el
  tick y el aire en que se apagó).

**Identidad comercial, con honestidad:** los gases visibles son territorio de *Oxygen Not Included*,
cuyo título es la mecánica. Lo que TIRO tiene y ONI no: física de píxeles con conservación exacta,
co-op en falling-sand (ningún comparable lo ofrece) y SOLTAR como verbo. El clip «TIRA» cumple las
cuatro condiciones de `03 §4`; la cápsula (un corte de tierra con una boca al cielo y humo en
columna) se lee en un fotograma.

## 11. Autoevaluación (rúbrica v2)

| eje | nota | por qué |
|---|---|---|
| apalancamiento_sistemico | 8 | 22 decisiones sobre un byte; 13 si B muere |
| leyes_ejecutan_revelan_juzgan | 8 | conservación y tiro ejecutan; el humo revela; sello y balanza juzgan con un solo canon humano |
| iteracion_humana (10 = poca) | 7 | tres sesiones y un canon; el resto, identidades de banco |
| verificabilidad_automatizable | 9 | todo el core tiene escenario, métrica y gate de muerte |
| simulacion_es_el_juego | 9 | cinco verbos existentes; el juego es la geometría de los agujeros |
| onboarding_garantizable | 8 | ocho situaciones por leyes, validadas por veredicto, envejecidas por cuna |
| observabilidad | 7 | el humo como visor es apuesta y riesgo; tres resoluciones diseñadas |
| tiempo_como_apuesta | 8 | el fuego es su propio reloj |
| multiplayer_emergente | 7 | el aire es de todos; sin roles; registros por fichero |
| profundidad_por_leyes_estables | 8 | el volumen sustituye a la tabla: nada que balancear por sala |
| cuerpo_del_jugador | 5 | instrumento (jadeo, tos, tizne, brasa), no ley |
| identidad_comercial | 7 | clip y cápsula claros; sombra de ONI |
| dificultad_tecnica (10 = fácil) | 6 | acotada, pero B puede no bombear y la conservación en nacimientos exige cuidado |
