# CRÍTICA · «SELLADO» (hibrido) · lente: LA SIMULACIÓN ES EL JUEGO

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: director creativo purista de la
simulación (Dwarf Fortress, Noita, Powder Toy). Único tema: ¿la simulación es el juego o hay un juego
tradicional encima? ¿El core loop suficiente existe sin acumular features? ¿Las leyes ejecutan,
revelan y juzgan solas, o hay un diseñador escondido en la puntuación, la progresión o el contenido?
¿Es divertido minuto a minuto o es un examen? ¿Cómo se narra el clip y la frase? Leído: `hibrido.md`
entero; `01_LEYES.md` entero con §6; las seis refutaciones del paquete J (sello ×2, balanza ×2, cuna
×2); `03_MERCADO.md`; las críticas hermanas de esta dirección (ingeniería, iteración humana) para
citarlas y no repetirlas. Código: `LabBench.cs` (`Escenarios` :76-88, `Correr` :265-335, la caldera
:306-311); `SimStepper.Laboratorio.cs` (libro mayor :49-120; decantación y depósito :452-480;
`LabInfiltrarHacia` :498-521; `LabPlanta` :801, fibra :811; `LabManantial` :1004; `LabSumidero` /
`LabTragar` :1021-1033; mudanza por presión :1157); `SimStepper.cs:742` (`Move` pone `reposo` a 0);
`LabMateriales.EsFondo` :98, `Tallable` :43; `LabParams.cs` (`TurbidezFuente` 40, `Decantacion` 6,
`DepositoUmbral` 200, `DepositoReposo` 24, `ColmatacionPct` 100, `Caudal` 24, `HogarRaw` 170,
`PlantaLuzMin` 40, `PlantaHumedadMin` 60). Aritmética: día = 1 800 ticks = 60 s a 30 Hz = 6 s a ×10;
30 días = 54 000 ticks = 3 min de time-lapse o ≈ 90 s de banco a 1,7 ms/tick, bastante menos en una
cámara de 96×64 con chunks dormidos.)*

**Veredicto: SEGUNDA RONDA (transformar).** Es la mejor síntesis de la familia J y la que más cerca
está de finalista: trae hecha la fusión que las críticas hermanas de esta lente pidieron a «El
Recibo» y a «Sin Manos», y el kit de observación más completo del panel. No pasa por tres razones
que son de diseño, no de física: su respuesta a «examen o juego» (bifurcar el registro gratis dentro
del intento) deshace la apuesta que el sello crea; de los dos «procesos mortales» con los que
sustituye al huerto, uno está narrado contra el código y el otro no tiene escala medida; y el recibo
sigue calificando el toque. Con la cámara persistente, el toque desgravado, D y un reloj medido es la
finalista de la familia J; tal como está escrita es Opus Magnum con arena, y su propia prueba de
«examen o juego» no sabría distinguirlo.

## 1. Lo que hace bien: la fusión ya está hecha y el marco es delgado

Las críticas de esta lente a «El Recibo» y a «Sin Manos» terminaban igual: son el mismo producto
escrito dos veces, fúndanse. Esta dirección lo hace y añade lo que faltaba a las dos: SOLTAR como
medida y no como modo («último día en que todas las cláusulas se cumplieron tras el último toque»),
la Piel siempre encendida (el rayos X como tacto: la única forma de instrumento que no es un panel),
el time-lapse por volcados diarios con la vista de diferencia (el hash hecho visible), y la cuna-lite
con el validador por veredicto ya corregido por los refutadores. No hay puntuación escalar, ni
economía, ni árbol, ni hub: la condición es una lectura de la grilla y del libro, el orden de la
campaña lo calcula el banco, y cualquier ley nueva entra en el recibo sin tocar el juez. Desde esta
lente es el marco más delgado que se puede poner sobre el sustrato sin dejarlo desnudo, y casi todo lo
que lo sostiene es C# en banco. «No depende del huerto» es la frase más honesta del panel.

También acierta en lo que deja fuera: la vertical como espina, las estaciones, los roles por
estrato, la lámpara, la prohibición de tocar. Y su §8 es una prueba real, aunque —se verá en §7— no
la que puede matarla.

## 2. El hallazgo principal: el bifurcado deshace la apuesta que el sello crea

La dirección nombra su riesgo mayor (que sea un examen) y lo defiende con tres piezas «del núcleo»:
el registro editable, el recibo como vector y el horizonte corto. La primera es la que importa, y
está al revés.

**La frase sobre la puerta promete lo que la mecánica niega.** La única frase del juego es «Todo lo
que hagas aquí seguirá pasando cuando te vayas». El núcleo de la dirección es que nada de lo que
hagas tiene por qué seguir pasando: arrastras la franja al día 11, tocas, y los días 12-14 no
ocurrieron. En Dwarf Fortress, Noita y Powder Toy lo que pasó, pasó; ése es el activo entero del
género y lo que separa «culpa mía» de «deshacer». Aquí la consecuencia irreversible y atribuible
—condición 3 del clip de `03 §4`— se retira por diseño en el minuto 9 de la primera situación.

**Los días no cuestan nada.** «Tocar solo cuesta días» sería un precio si los días fueran de alguien.
En un rejugado determinista un día son 6 s de time-lapse o una fracción de segundo de `CorrerSello`;
la propia dirección presume de que un registro se rejuega en banco en segundos. El precio real de
bifurcar es cero, y la dirección lo sabe: «volver atrás es gratis pero se nota en el recibo».

**Y «se nota en el recibo» es exactamente la firma del examen.** El recibo es un vector «(clara,
finos, carbón, calor, goteos, plantas, toques, días)» comparado por dominancia. Toques y días son
columnas: bifurcar no cuesta mundo, cuesta nota. Es la anotación en el margen que dice «usó la clave
de respuestas». La crítica hermana a «El Recibo» pidió desmedir la preparación (toques fuera de la
nota; el diario completo solo para rejugar y atribuir); esta dirección la funde y no toma esa
corrección: el toque sigue gravado, y ahora el rebobinado también. Y «toque» ni siquiera es una
unidad honesta: el cincel escribe decenas de celdas por pulsación (lo señala la crítica de
ingeniería), así que la columna mide píxeles, no decisiones.

**Lo que queda es un puzzle con deshacer.** Preparar con tres verbos sobre 6 144 celdas, sellar,
mirar tres minutos, arrastrar la franja, corregir, sellar. Es Opus Magnum con un falling-sand como
evaluador, y la dirección lo confiesa en §10 al pedir prestada «la estructura de comparación de Opus
Magnum». Es un género legítimo (200-500 k según `03 §2`), pero no es «la simulación es el juego» con
un 8 ni «tiempo como apuesta» con un 9: en Opus Magnum el minuto a minuto es CONSTRUIR con un
vocabulario enorme y la corrida es el premio; aquí el vocabulario son cincel, frasco y tres
materiales que poner, y la corrida es el juego. Con un espacio pequeño, determinista y con rebobinado
gratis, el jugador es un buscador de colina sobre una función que el banco recorre mejor que él: 1-2
toques sobre una cámara de 96×64 se enumeran en una noche (`02 §3 R6` ya hizo la cuenta: 92 montajes
de un toque, ~4 200 de dos). El banco es el mejor jugador de este juego, y eso es lo contrario de lo
que la función objetivo busca.

**Por eso la segunda prueba de §8 no puede distinguir.** «Si la mediana de bifurcados voluntarios es
cero, es un examen»: cierto. Pero si la mediana es alta, no es «juego»: es «deshacer es agradable»,
que lo es en cualquier puzzle con rebobinado. La prueba mide el placer del undo, no si la simulación
sostiene la partida. La medida que sí separa las tres cosas está en §9.

## 3. La intro está narrada contra el código

«La que se ciega» necesita dos relojes: el sumidero cegado por sedimento (la geología de la cámara
envejecida) y la grava del labio que se colmata el día 11. Ninguno está medido, y el primero, tal
como se cuenta, no lo produce este código por el camino que la dirección describe.

- **El agua sobre un sumidero nunca deposita.** El depósito exige `reposo ≥ 24` visitas con `carga ≥
  200` sobre un fondo (:466; `EsFondo` incluye al sumidero, :98). Pero `LabSumidero` traga a sus
  cuatro vecinos líquidos en cada visita (:1021-1033, cada 8 ticks), la celda que llega en su lugar
  nace con `reposo` 0 por `Move` (SimStepper.cs:742), y la mudanza por presión también lo pone a 0
  (:1157). Ninguna celda de agua sobrevive 24 visitas encima de un sumidero: el drenaje es un lugar de
  `reposo` cero por construcción. Peor para la escena: los finos decantan hacia el agua de ABAJO
  (:455-463), así que la poza con el sumidero en el fondo es un drenaje perfecto de lo concentrado, no
  un sitio que se ciega. Los dos refutadores de la balanza se contradicen justo aquí (el de
  apalancamiento dice que «hoy una salida se colmata sola»; el de ingeniería, que «el agua sobre él
  vive ≤ 8 ticks y no alcanza `DepositoReposo`»): el código da la razón al segundo. Queda un camino
  que la crítica de ingeniería encuentra y que es plausible: depósito en el fondo vecino, sedimento
  como polvo que resbala al punto más bajo. Plausible no es medido: nadie ha corrido una cámara
  manantial-canal-poza-sumidero 30 000 ticks. La geología que «cuenta lo que pasó» hay que medirla;
  hoy es narración, como la primera pasada avisó de P3.
- **La grava se colmata por una curva autolimitada sin escala.** `LabInfiltrarHacia` atrapa finos a
  razón de `rate × carga/255` y `rate ∝ libre²` (:509-513): cuanto más colmatada, más despacio se
  colmata. Con `TurbidezFuente` 40, el labio tarda en dejar de pasar agua un tiempo que nadie ha
  medido y que puede no estar a escala de días (R148 midió 72 000 ticks del arco largo y no anotó
  colmatación; y el labio narrado es de roca suelta, no de grava). «Día 11, la barra vuelve a parda»
  es el número del que cuelga toda la escena de diez minutos, y es un deseo.
- **Los dones siguen siendo pins.** Manantial eterno (:1004), hogar de 170 raw eterno, núcleo frío
  eterno. `01_LEYES.md §6` lo dice: «con fuentes infinitas SOLTAR no arriesga nada por el
  suministro», y propone D (el hogar come, 0,5-1 semana, tuning 8) como el candidato con más
  apalancamiento por línea. La dirección cuyo verbo es sellar y contar días deja D fuera, igual que
  «Sin Manos». Sin D y con el sumidero que no se ciega por sí mismo, toda cámara de agua clara que no
  pase por grava tiene DÍA ∞, y la de fuego doméstico también.

De los «procesos mortales» que la dirección ofrece a cambio del huerto («la salida que se ciega y la
tolva que se acaba»), el primero es probablemente falso como se narra y el segundo es un reloj
monótono de 7,8 días (466 s). La crítica de ingeniería cuenta cuatro relojes en el código; la de
iteración humana lee R148 y encuentra que cuatro de las cinco situaciones de la hora 1 se deciden
antes del día 1. Con eso, la tensión de la campaña de la hora 1 es aritmética con cronómetro; la
única mortalidad por cruce sigue siendo la de R135/R148 (el alambique que ahoga y sombrea), y ésa la
hizo un autor.

## 4. Minuto a minuto: cuatro toques y tres minutos de poza

El juguete existe mientras se prepara a ×1, y la dirección lo dice bien. Pero la preparación narrada
son cuatro toques en cinco minutos, y el recibo los cuenta. El placer primario del falling-sand
(tocar y ver ahora, cien veces por minuto) queda en la fase que la nota castiga; el placer de mirar
queda en una poza a ×10 durante tres minutos que nadie ha mirado todavía. Entre las dos, arrastrar
una franja. No es tedio garantizado: es que todo lo que el documento ofrece para ese rato (Piel, tres
visores, franja creciendo) es instrumentación, y decidir dónde mirar es la única decisión gratis. La
dirección no describe un solo momento en que la simulación le plantee al jugador un problema que no
escribió un autor; la refutación de la cuna sigue en pie (sin autor, tres atractores: anegado,
quemado, inerte). La sim aquí evalúa problemas; no los produce.

Hay una idea de otra dirección que arreglaría la mitad de esto y que la función objetivo prefiere
por ser ley y no botón: la velocidad como estado (×1 mientras la mano actúa, ×10 al retirarla, de
«Días sin manos»). En Sellado el ×10 es un botón que se pulsa después de SELLAR; convertido en
estado, «soltar» deja de ser un gesto de interfaz y pasa a ser una propiedad física del mundo que se
está mirando.

## 5. Revelar: el mejor kit de la familia J sobre un mundo que sigue sin verse

Piel siempre encendida, bandas de ley en vez de rampas, time-lapse con scrub, diff de `mat[]`,
terracota y carbón como registro: es el mejor conjunto de instrumentos del panel y casi todo es
banco. Pero el prototipo feo es «F8 como visores, recibo como texto, plantas de un píxel», y las tres
sesiones de «examen o juego» se harán sobre eso: medirán F8. La dirección presupuesta la legibilidad
de bandas y Piel en dos días con láminas y GIF; lo que no presupuesta es el render mínimo (agua como
único azul, turbidez como tinte, plantas de varias celdas) sin el cual el clip de `03` incumple la
condición 1 y el playtest no juzga el core sino la interfaz. Son 1-2 semanas que no están en las
10,5-12. Y el diff de `mat[]` con `Caudal` 24 lo escribe el agua: sirve solo sobre sólidos.

## 6. El diseñador escondido

- **15-20 situaciones de autor**, cada una con montaje, condición con umbrales y registro del autor
  que debe cumplir; y la cinta de correr: L, A, V, F, M «entran como situaciones reordenadas por el
  banco sin tocar el juez», pero cada paquete cambia la física y caduca los registros del autor (21
  rondas en 3 días): alguien re-resuelve la campaña por paquete. El validador comprueba; no resuelve.
- **El horizonte y `DiaTicks`**: un número por situación que es el mando de dificultad, humano.
- **Los relojes de la intro** (§3): si no salen a escala de días, se afinan `ColmatacionPct`,
  `Caudal`, `TurbidezFuente` hasta que salgan; eso es balance de un espacio pequeño, y es humano.
- **La economía de «poner»**: sin inventario, si poner es crear materia gratis, cualquier condición se
  cumple pintando (un labio más, un núcleo frío más) y la única fricción es una columna que la
  dominancia no obliga a mirar. La dirección no lo fija; decide qué juego es.
- **Legibilidad**: bandas, Piel, franja, time-lapse y diff son cinco vistas nuevas; dos días es
  optimista; tres o cuatro tardes con desconocidos es lo normal.
- **El ritmo**: 6 s por día, 3-10 min de time-lapse: nadie lo ha mirado.
- **Cifras honestas**: qué es «clara» (16 entre 40 y 6: un número, pequeño) y el largo del día.

Tuning humano real: **6**, no 8. Coincide con las otras dos lentes.

## 7. La prueba de §8 no puede fallar

«El autor domina al vacío por ≥ 2 en alguna columna en 3 de 4 montajes» no puede fallar: los
registros «del autor» listados (la caldera, el serpentín fuera de la boca, el labio) son el aparato
del montaje; sin caldera no hay alambique, y el vacío pierde por construcción (900 goteos contra 0).
Mide «hacer algo produce más que no hacer nada», que es una tautología de las intervenciones que SON
el proceso. Vale como regresión, no como juicio. La prueba que mata está en §10.

## 8. Multijugador, clip y frase

**Multijugador.** Relevo y liga por fichero, sin roles: honesto y casi gratis por determinismo. Pero
es comparar deberes; nada se hace junto. La «información asimétrica temporal» (ves qué hizo, no por
qué) es real y buena. Con la cámara persistente de §9 el relevo gana algo que hoy no tiene: B hereda
un mundo vivido, no un registro que puede reescribir desde el toque 1. Y cuelga de un día que nadie
ha hecho: IL2CPP contra los 63 hashes.

**Clip.** La frase de la dirección tiene tres oraciones y no cabe en un tuit; el cartel «DÍA N SIN
MANOS» es heredado y sigue siendo lo más vendible del lote. El clip que la dirección describe
(time-lapse con franja y dos cifras) es una tabla animada. El clip bueno existe y es el fracaso, no el
aprobado: 0-2 s, agua parda cayendo al sumidero, el halo de la Piel oscuro en los pies; 2-7 s, tres
piedras de labio, la poza se aquieta, la barra se vuelve azul, «DÍA 3… DÍA 9» mientras la grava
ennegrece grano a grano; 7-10 s, «DÍA 11», la barra parda; remate: «la grava se cansa». Cumple las
cuatro condiciones de `03 §4` solo si la grava se colmata a escala de días y se ve a un píxel. Y el
clip que la dirección añade —el rebobinado al día 11— es un clip de Braid, no de Dwarf Fortress:
nombra «deshacer», no «culpa mía».

**Frase de un tuit:** «Arregla la cámara, sella la puerta y cuenta los días que aguanta sin ti; si te
la pasan, sigue donde el otro la dejó.»

## 9. Condiciones para la segunda ronda

1. **La cámara viva es persistente; bifurcar crea una cámara hermana.** El registro se puede
   bifurcar (es la mejor idea del relevo), pero el resultado es OTRA cámara, un fichero nuevo; la que
   se está viviendo no se reescribe. DÍA N SIN MANOS se cuenta sobre la vida de la cámara; tocar en el
   día actual es trabajo que se nota (limpiar la grava colmatada, no des-colmatarla). Así la frase
   sobre la puerta es verdad y el precio del toque es mundo, no nota. En un determinista con volcado
   la irreversibilidad es una convención; la dirección elige la contraria y debe elegir ésta.
2. **Toques fuera del recibo.** El diario completo sigue (rejugar, atribuir, detectar copias); el
   recibo pesa lo que sale por el sumidero y los días, no cuántas veces se tocó.
3. **Validador de reloj y D en la primera entrega.** Una situación entra en la campaña solo si el
   registro del autor, rejugado a 3×H, se cae algún día (algo se degrada) y el vacío se cae después
   del día 1; el hogar come (0,5-1 sem) para que el fuego doméstico tenga reloj. Lo que no tiene
   reloj va a la cámara libre como lámina, no a la campaña.
4. **Sustituir la segunda prueba de §8** por dos medidas: con personas, cuántos **continúan** (tocan
   en el día actual y siguen), cuántos **rebobinan** y cuántos **abandonan**; en banco, la «ganancia
   por bifurcar» de §11. Mata si nadie continúa nunca (mundo sin peso: puzzle con deshacer, y
   entonces hay que venderlo como tal) o si nadie toca a mitad de corrida (examen).
5. **Render mínimo presupuestado** (1-2 sem) antes del primer playtest, o aceptar por escrito que
   las tres sesiones miden F8.
6. **Velocidad como estado** en vez de botón SELLAR + ×10, y la economía de «poner» fijada antes de
   cualquier barrido (solo quitar y mover, o presupuesto de toques como conservación del banco).

## 10. La prueba más barata que la mata

**En banco, 3-4 días de Opus y una noche de máquina.** Intervención genérica tick-estampada en
`Correr` (la de la caldera, desacoplada de `esAlambique` :295-311; la refutación de la balanza ya la
pide) más una sonda por día de la condición de cada montaje. Rejugar los nueve montajes con su
registro de referencia a 30 días (54 000 ticks) y, aparte, «La que se ciega» tal como la narra §2
(manantial 24 c/s, canal, poza, sumidero en el fondo, labio de grava) a 30 y a 300 días, anotando
claras/día y el día en que la condición se cae. **Mata**: si siete o más de los nueve nunca se caen, o
si en «La que se ciega» claras/día no cae a la mitad en 300 días (ni el sumidero se ciega ni la grava
se colmata a escala de días), el sustrato no tiene relojes de días para cámaras de agua, la intro es
falsa, y «contar los días» no cobra nada: la dirección muere como está escrita y solo sobrevive con D
y un reloj de grava afinado, que es tuning. Dos semanas hasta ese dato, en paralelo con la balanza
mínima y la discriminación de la propia dirección. Coincide en coste y en semana con las puertas que
proponen las otras dos lentes (finitud y vector; tabla de relojes y día de decisión): son la misma
noche de banco con tres columnas más.

## 11. Qué se automatiza en su lugar

- **Validador de reloj** (banco): el registro del autor a 3×H se cae algún día; el vacío se cae
  después del día 1. **Horizonte automático**: H = f × día de ruptura del autor; f por familia.
- **Ganancia por bifurcar** (banco, una hora por situación con muestreo): mejor recibo por dominancia
  con todos los toques antes del sello (k = 0) frente al mejor con un toque a mitad de corrida (k = 1,
  muestreando 6 días × 50 celdas × 3 verbos). Si k = 1 nunca domina a k = 0, tocar a mitad de corrida
  es una decisión muerta y el rebobinado es puro deshacer: la situación se etiqueta como puzzle, no
  como apuesta. Es la versión sin personas de la segunda prueba de §8.
- **Búsqueda exhaustiva de 1-2 toques** como autor (encuentra el registro de referencia tras cada
  paquete de física: fin de la cinta de correr) y como validador de mutantes al revés («el autor
  falla y hay otra solución», no «el autor cumple»).
- **Pins a 300 días**: toda situación construida sobre manantial/hogar/núcleo se corre a 540 000
  ticks; si DÍA N no acota, no entra en la campaña.
- **Edad honesta** = diff entre volcados consecutivos (no `touchedTick`, que es guarda de reentrada).
- Regresión de todo lo anterior por hash de versión de física; detección de copias por registro.

## 12. Tiempos corregidos

| | dirección | esta crítica |
|---|---|---|
| evidencia para matarla | 2 sem (discriminación) | 2 sem, midiendo relojes (§10) en paralelo con la discriminación |
| prototipo feo que permita juzgar el core | 7 sem | 8-9 sem: sello 3-4 → cuna-lite 2 → tres situaciones 0,5 (6,5 en serie, igual); el gesto (SELLAR, contador, franja, time-lapse por 30 volcados, scrub, bifurcar como cámara hermana, diff) son 2,5-3 sem, no 1,5, y está en el camino crítico porque sin él la sesión no puede matar; render mínimo 1-2 sem si el playtest ha de medir el core; D 0,5-1. La cámara persistente no cuesta nada: es el mismo diario, más largo |
| iteración humana | 3-4 sesiones (sem 8-10) | 6-8 sesiones: examen/juego con la medida corregida, ritmo y horizonte, legibilidad de cinco vistas (3-4 tardes), los relojes de la intro si hay que afinarlos, y la cámara libre de la hora 20, que es P4 con su tramo manual sin medir |

Secuencial: gesto ← diario; validar ← `CorrerSello`; envejecer ← volcado; playtest ← render.
Paralelizable: balanza, bandas, Piel, anillo, D, relojes en banco, búsqueda de toques, IL2CPP.

## 13. Órganos a conservar si se descarta

El sello entero (diario bajo las seis puertas, volcado/carga con la lista completa de estado,
`CorrerSello`, condición como dato, hash de versión) como primer fichero de partida del proyecto; la
balanza con bit `entregable` y tragar solo desde arriba; `LabBandas` como única fuente de umbrales y
los tres visores por bandas; la Piel siempre encendida, el tinte y la brasa viva en el frasco; la
cuna-lite (envejecer, validador por veredicto, fragilidad, mutaciones) más el validador de reloj de
esta crítica; el time-lapse por volcados diarios con scrub y la vista de diferencia (solo sólidos);
**bifurcar como semántica de fichero** (el otro bifurca tu registro y nace otra cámara), nunca como
reescritura de la cámara viva; «DÍA N SIN MANOS» como propiedad de un mundo persistente; el anillo de
hashes; «La que se ciega» como intro si y solo si su reloj se mide; el orden de la campaña por leyes
implicadas como informe (no como orden automático: con 12-15 compuertas, apagar «agua» mata todo y
apagar «luz» nada); la frase de un tuit.

## 14. Rúbrica v2

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | J no añade física; el bifurcado crea una decisión (dónde volver) y mata otra (aguantar); la palanca real es que toda ley entra en el recibo |
| las leyes ejecutan, revelan y juzgan | 7 | ejecutan; juzgan lo que una cláusula nombra, con umbrales y horizonte humanos; revelan mejor que sus hermanas (Piel, bandas, diff), después de ocurrido |
| iteración humana (10 = poca) | 6 | 15-20 situaciones resueltas y re-resueltas por paquete; horizonte; relojes de la intro; economía de poner; cinco vistas; ritmo; render sin presupuesto |
| verificabilidad automatizable | 9 | todo en banco, y con §11 también «examen o juego» en su mitad objetiva |
| la simulación es el juego | 5 | el marco es delgado, pero es un puzzle con deshacer sobre problemas de autor; la sim evalúa, no plantea; el banco juega mejor; la frase de la puerta la contradice la mecánica |
| onboarding garantizable | 7 | envejecido, validado, ordenado por banco; la intro cuelga de dos relojes sin medir y el validador no exige que nada se degrade |
| observabilidad | 7 | el mejor kit de la familia J; el mundo sigue en F8 y las plantas en un píxel para el prototipo que se va a probar |
| tiempo como apuesta | 4 | rebobinar gratis no apuesta nada; los días cuestan segundos; los pins hacen DÍA ∞; 8 con cámara persistente, D y relojes medidos |
| multiplayer emergente | 5 | relevo y liga por fichero, sin roles; comparar deberes, nada juntos; IL2CPP sin probar |
| profundidad por leyes estables | 6 | crece por ley que entra en el recibo; hoy relojes monótonos y un cruce, sobre 6 144 celdas y tres verbos que se enumeran |
| cuerpo del jugador | 5 | sensor y portador de brasa; sin consecuencias, y lo dice |
| identidad comercial | 6 | cartel heredado bueno; frase larga; el clip propio es un rebobinado; arquetipo Opus Magnum (200-500 k) con un puzzle de menor mediana |
| dificultad técnica (10 = fácil) | 8 | acotada; la lista de estado del volcado, la memoria de 30 clones (~100 MB), el banco en serie, IL2CPP y el gesto son lo abierto |

Suma simple: 81 / 130. Puertas: pasa (iteración 6, apalancamiento 6), sin margen en apalancamiento.

**Posición:** transformar, no descartar ni finalista. A la pregunta final: las leyes EJECUTAN solas
y JUZGAN solas lo que una cláusula de autor nombra; REVELAN con el mejor kit del panel sobre un mundo
que todavía no se ve; y el jugador de esta dirección, en su minuto típico, prepara cuatro toques,
mira una poza tres minutos y rebobina. Con la cámara persistente, el toque desgravado, D y un reloj
medido, es la finalista de la familia J (y entonces es, casi celda por celda, F1 de `02`); tal como
está, es Opus Magnum con arena, y su propia prueba de «examen o juego» no sabría distinguirlo.
