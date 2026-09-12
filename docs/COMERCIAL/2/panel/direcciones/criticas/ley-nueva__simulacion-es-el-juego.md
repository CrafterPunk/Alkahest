# CRÍTICA · «TIRO / La segunda boca» (ley-nueva) · lente: LA SIMULACIÓN ES EL JUEGO

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: director creativo purista de la
simulación. Único tema: ¿la simulación ES el juego o hay un juego encima? ¿Existe el core loop
suficiente sin acumular features? ¿Las leyes ejecutan, revelan y juzgan solas, o hay un diseñador
escondido? ¿Es divertido minuto a minuto o es un examen? ¿Cómo se narran el clip y la frase? Leído
entero: `ley-nueva.md`, `01_LEYES.md`, `03_MERCADO.md`, `00_ENCARGO_Y_CRITERIO.md`, la lente
`aire-viento`, las cuatro refutaciones de `aire-que-fluye` y `aire-atrapado`, la parte de aire de
`_huecos_y_combinaciones.md`, y la crítica hermana de iteración humana (su aritmética del canon la
doy por buena y la cito en vez de repetirla). Código: `SimStepper.cs` :829-960 (`ProcessCombustion`,
la sordina, la lengua, `SpawnSmokeNear`), :1004-1055 (`ProcessBrasa`), :1443-1640 (`ProcessGas`:
rumbo por hash, ondulación, bolsa), :1644-1720 (`ProcessFire`, `life = 30`);
`SimStepper.Laboratorio.cs` :186-240 (`LabCampos`), :328-395 (`LabAire`), :1040-1066
(`LabRespira`, `LabPasoSordina`), :1080-1170 (`LabPresion`); `INFORME_FINAL.md` :123-126 (el tiro
no existe); `CHECKPOINT.md` HF1 (carbonera con boca 1: 0 humo); `CellGrid.cs` (pantalla de 256×144
celdas).)*

## 0. Veredicto en una línea

**Segunda ronda.** Es la dirección más pura del panel en mi tema: no hay juego encima, los cinco
verbos ya existen, la máquina es la forma de los agujeros y la primera máquina es un gesto con hora
que el sello guarda con tick. Y aun así no la pongo en la final, porque de los tres verbos de la
pregunta final (ejecutar, revelar, juzgar) el del medio está roto por construcción: **el estado
más importante de la ley (el fuego que se ahoga) es el que menos materia emite**. La sordina no
saca lengua y humea a un cuarto; una pila de nueve fibras ahogada suelta una celda de humo cada 7-9
segundos. La simulación ejecuta y el sello juzga, pero entre medias no se ve nada, y la propia
experiencia narrada lo confiesa: en los primeros cinco minutos abre tres vistas (Piel, Presión,
Corrientes). Bajo A sola, además, el humo sigue leyendo un hash por bloque de 8×8: la veleta señala
ruido. Lo arregla materia, no panel, y hay dos ajustes de una línea que lo intentan; hasta que se
midan, es una hipótesis de física con el mejor clip del panel.

| campo | valor |
|---|---|
| **veredicto** | segunda ronda (finalista si el humo enseña el aire sin F8 y si B bombea; si B muere, sus órganos van a «las leyes juzgan» como «RESPIRA», no como «TIRO») |
| **riesgo mayor (esta lente)** | REVELAR es una ausencia: la sordina se define por lo que NO hace (sin lengua, humo ÷4, un paso de cada cuatro), así que «el fuego se ahogó» y «el fuego se acabó» se ven igual; la dirección lo compensa con vistas y con un halo, es decir, con panel. Bajo A sola la veleta no existe (el rumbo del humo sigue siendo `XorShift` por bloque) y la vista Corrientes enseñaría ruido como si fuera corriente: la palabra de la reseña sería «fake» |
| **iteración humana oculta** | la del productor (14-22 días en tres meses) la doy por buena; desde esta lente lo incomprimible es el ojo sobre el humo a 7,5 px por celda y la decisión de qué materia hace visible la asfixia: 2-4 sesiones que no se acortan con hashes, más el registro de autor de ocho situaciones |
| **se automatiza en su lugar** | métrica de latencia de señal en banco (ticks entre `aire < umbral` y el primer cambio de materia visible: lengua que muere, humo que aparece); filas de humo bajo techo con la sordina invertida; hash del horno y 18/18 del vidrio como puerta de la inversión; Σvy abierta frente a tapada; Σaire; barrido del canon; regresión nocturna de los registros de autor |
| **prueba que la mata** | **hoy, sin código**: la sordina ya existe por geometría; poner una pila de nueve fibras en un cuarto sellado (o la caja sellada de r136 §3) en pantalla con F8 apagado y preguntar a Cesar si ese fuego respira y por qué se apagó. Si no puede decirlo, A no lo arreglará: A cambia CUÁNDO entra la sordina, no QUÉ se ve |
| **semanas hasta evidencia para matarla** | 2 (la prueba de hoy en la semana 0; A en la 1; chimenea con Σvy y «La vela» sin panel en la 2, sobre el editor actual, sin J) |
| **semanas hasta prototipo feo** | 3 para juzgar el core (A + spike de B + humo a escala + brasa en el frasco + tinte, sin sello: SOLTAR y el ×10 ya existen); 6-7 con J, que es infraestructura de juicio y comparación, no del core |

## 1. Lo que es puro, y por qué compite

Antes de desmontar el eslabón, lo que ninguna otra dirección del panel tiene tan limpio:

- **No hay juego encima.** Ni progresión, ni desbloqueos, ni puntuación escalar, ni recurso
  abstracto. Los verbos son los cinco del aprendiz (cavar, apilar, prender, sellar, abrir) y la
  única ley nueva es una cantidad conservada que todas las leyes existentes leen: el fuego
  (`LabRespira` por umbral en vez de por vecinos), la llama (`ProcessFire` :1673, `life = 30` deja
  de ser inmortal), el gas (`rumboLeft` lee `viento` en vez del hash), el agua (`LabPresion`
  respeta la bolsa), la planta (repone aire donde tiene luz, si se adopta el hueco del crítico de
  completitud). Eso es «cruzar antes de añadir» hecho ley.
- **El agujero es sorpresa y máquina a la vez.** Hoy un agujero decide una cosa (luz); con el aire
  decide cuatro (luz, aire, humo, calor). La primera sorpresa (la llama muere sola) y la primera
  máquina (la segunda boca) son el mismo gesto con el mismo cincel. El hueco que mató a la primera
  pasada (qué hay entre la sorpresa y la máquina) mide aquí un cincelazo.
- **La primera máquina es un gesto con hora.** «Sella la boca baja con arcilla a los veinte
  segundos» no es un objeto: es una intervención tick-estampada. Que el sello guarde la máquina
  como un instante y no como una forma es la idea más pura de toda la pasada: la simulación no
  solo ejecuta la geometría, ejecuta el *cuándo*.
- **El reloj lo pone el fuego.** SOLTAR no tiene horizonte impuesto: la pila se ahoga o se agota
  sola en 10-30 s (una fibra dura 320 ticks: reserva 40 × paso 8). Es la dirección con el loop más
  corto del panel (uno a tres minutos entre toque y veredicto), y por eso la que menos se parece a
  un examen. «Volver al último toque» es deshacer por determinismo, no un botón de juego.
- **El contenido es geometría sobre una cantidad conservada.** La sala no tiene tabla, tiene
  volumen. Ocho montajes de 10-15 líneas, validados por veredicto, envejecidos por cuna. No hay
  biblioteca.

Si el tema fuera solo «¿hay un juego tradicional encima?», sería finalista con 9. El problema está
en el verbo del medio.

## 2. REVELAR es una ausencia

La pregunta final pide que las leyes ejecuten, revelen y juzguen. Aquí ejecutan (conservación
exacta, Σaire) y juzgan (el sello y la balanza, compartidos con toda la pasada). Revelar es lo que
el documento llama su riesgo mayor, y desde esta lente no es un riesgo: es un defecto de
construcción de la ley tal como está escrita hoy.

La sordina, leída en `ProcessCombustion` :855-885, se define por lo que **no** hace: actúa uno de
cada cuatro pasos (`LabPasoSordina`), calienta a la mitad, **no saca lengua** (`if (!sordina &&
...)` :879) y humea a `(combustHumoPct + 3) / 4`: para la fibra, `(16+3)/4 = 4 %`; para el carbón,
`(4+3)/4 = 1 %`. Una celda de fibra ahogada tira ese 4 % una vez cada 32 ticks: **una celda de humo
cada ~800 ticks (27 s)**. Una pila de nueve fibras con tres o cuatro ardiendo a la vez: una celda de
humo cada 7-9 s. Y la lengua, que es lo único que el ojo lee como «fuego», desaparece del todo. Es
decir: el estado que la ley quiere enseñar (el fuego respira mal) se representa como *menos* de
todo. «Se ahogó» y «se acabó» son la misma imagen: un montón de fibra sin llama. La carbonera con
boca 1 de HF1 lo mide sin querer: 100 % de carbón, **0 de humo**. El aparato que mejor funciona es
el que menos se ve.

El documento lo sabe y lo compensa: a 0:40 «el halo se vuelve azul» (vista Piel, siempre
encendida); a 1:30 «abre el rayos X de aire (vista Presión)»; a 3:00 «vista Corrientes: flechas por
bloque de 8×8». Tres vistas en cinco minutos, en la situación que enseña la ley madre. El encargo
dice que los rayos X vuelven con fuerza como parte de aprender a ver, y estoy de acuerdo; pero hay
una diferencia entre un visor que *afina* lo que la materia ya enseña (la vista Piel como tacto) y
un visor que *sustituye* a la materia porque la materia no enseña nada. Aquí es lo segundo, y la
propia dirección escribe la consecuencia en su §6: «el juego pasa a ser leer un panel». No hace
falta esperar a la semana 6 para saberlo: la sordina ya existe hoy por geometría (una pila bajo roca
suelta, el interior de la pila maciza, una caja sellada), y la caja sellada de r136 §3 mide 0,5-1,4
filas de humo. A 256×144 celdas por pantalla, una celda son 7,5 px en 1080p: la «veleta» es una raya
gris de 4-10 px.

Hay una segunda ausencia, más sutil: **el humo de hoy no lleva información del aire.** `ProcessGas`
:1528 decide el rumbo con `XorShift.FromCell(_tick >> 4, x >> 3, y >> 3, SalGasRumbo)`, un hash por
bloque de 8×8 con ventana de 16 ticks, afinado a ojo en los playtests 39-41 («no suba en vertical
perfecta», petición literal de Cesar). Bajo **A sola**, nada de eso cambia: la llama muere, pero el
humo sigue subiendo con la ondulación del hash. «El humo enseña por dónde va el aire» es una promesa
de **B**, no de A. Y la vista Corrientes «por bloque de 8×8, el grano en que `ProcessGas` ya decide»
enseñaría, bajo A, exactamente ese hash pintado como flechas: ruido presentado como corriente. `03
§4` tiene la palabra para eso: «fake». Corrientes no puede existir sin B. Y a B lo predicen muerto los
dos refutadores (la columna térmica sobre el fuego decae ×0,63 por celda en `LabFlujoTermico`: sin
columna caliente no hay Δp·h; solo el humo lleva calor, y en sordina apenas hay humo). El círculo es
incómodo: cuarto cerrado → sordina → poco humo → poca columna caliente → sin tiro aunque se abra
la chimenea. El escenario de dos días está bien diseñado justo para eso; hay que correrlo antes de
escribir una situación.

Conclusión de esta sección: el loop escrito en el §3 de la dirección (ejecutan → revelan → juzgan)
es, tal como está el código, ejecutan → *panel* → juzgan. No es fatal, porque se arregla con materia
y hay dos ajustes de una línea (§7); pero no se puede dar por hecho, y la dirección lo da.

## 3. La frase promete una ley que el documento no entrega

«Cada fuego respira lo mismo que tú.» Es la mejor frase del panel y vende un cruce concreto: el
cuerpo y el fuego compiten por el mismo byte. Luego el §10 lo retira: «sin ahogo ni respiración en
la campana; el cuerpo es instrumento». El jadeo del sprite en aire pobre es un tinte, no una ley: el
cuerpo lee el aire y no lo gasta. Desde la lente de «la simulación es el juego» eso es una
incoherencia de las que se pagan en la reseña: la frase vende un común de tres (fuego, humo, yo) y
el juego entrega un común de dos.

El crítico de completitud ya lo nombró y nadie lo escribió: el cuerpo que respira `aire`
compitiendo con el fuego es una línea (restar en la celda del cuerpo lo que resta una brasa), y
compra «qué se asfixia primero» como decisión, accidente y clip sin barra: el aprendiz que tose, se
ralentiza y **suelta el frasco con la brasa dentro** (que cae y prende la yesca: la ley C mínima ya
lo hace). Eso no es una barra de supervivencia; es una consecuencia física con evidencia forense
(R54). Y convierte el «común invisible» en algo que el jugador *siente* antes de verlo: si a mí me
cuesta respirar, al fuego también. La entrada en la campana con aire finito, que el documento
aparca, cae gratis. O se entrega esa línea, o la frase cambia. Con la línea, la frase es verdad y
la vista Piel deja de ser panel para ser tacto.

## 4. El diseñador escondido

Poco, y hay que decirlo con precisión:

- **El canon.** «Un cuarto sellado de 10×10 ahoga nueve fibras en un día» se presenta como cero
  balance. Es el ancla de escala de la simulación, y toda simulación tiene una (ONI: cuánto respira
  un dupe). Legítimo, pero es un número de diseño y no una identidad física: decide el tamaño de
  sala en que la ley importa, y por tanto la dificultad de cada situación. La crítica hermana
  demuestra que además está sobredeterminado (la pila se agota en 600-900 ticks y el «día» son
  1 800: con el consumo escrito, el fuego se acaba antes de ahogarse, y entonces el canon no se ve).
  Doy esa aritmética por buena; desde mi lente lo que pido es que se llame por su nombre: un número
  humano, elegido una vez, que fija la escala. No «cero».
- **La relajación hacia 128.** Es el único sitio en que un diseñador se mete en la física: aire que
  aparece de la nada para que la cueva no se asfixie con el tiempo. Es el hada del aire, y decide a
  qué tamaño de sala la ley deja de existir. La alternativa del crítico de completitud (la planta
  repone aire donde tiene luz) es una ley con consumidor y cruza huerto y fuego en los dos
  sentidos; la otra alternativa es que la boca del cielo sea la única fuente y que un mundo con
  cien fuegos se asfixie **de verdad**: eso es una consecuencia, no un fallo, y la respuesta del
  jugador es cavar más bocas. Se mide en banco cuál de las dos es jugable; la relajación global no
  debería ser la opción por defecto.
- **Las condiciones.** «Llama viva el día 2», «carbón ≥ 6 el día 3»: números de autor por
  situación. Son cláusulas booleanas, no balance, y ocho es una cifra pequeña. Aceptable.
- **El hash del humo.** Donde `viento` sea cero (la mayor parte del mundo, la mayor parte del
  tiempo) el rumbo sigue siendo el hash afinado a ojo (`GasOndulacionPct` 30, `GasBolsaLateralPct`
  60, ventana de 16 ticks). No es grave, pero es un diseñador escondido en el gas que la dirección
  no menciona, y es el que decide cómo se ve el humo en calma.

## 5. Minuto a minuto: no es un examen

Aquí la dirección gana. Cavar una celda y ver qué hace el humo es táctil y barato; el fuego se
ahoga o se agota en 10-30 s; el «día» a ×10 son seis segundos; el veredicto llega en uno a tres
minutos. Es el ritmo de Noita (tocar, mirar, morir, otra vez), no el de un colony sim. El riesgo de
examen que comparten todas las direcciones de SOLTAR está mitigado por ley, no por interfaz: el
reloj es el combustible.

Con una condición: que el jugador vea *algo* durante esos 10-30 s. Si la asfixia es una ausencia
(§2), el minuto a minuto es «prendo, espero, se apagó, no sé por qué», que es la frase que mata un
sistémico en la reseña. Con la sordina que humea (§7) el minuto a minuto es «prendo, el cuarto se
llena de humo, la llama se agacha, abro, el humo sale, la llama vuelve»: tres estados visibles en
treinta segundos, sin un solo panel.

## 6. El clip y la frase

El clip «TIRA» es el mejor del panel en la condición 3 de `03 §4`: la mano está en el plano, la
consecuencia es mía, y el remate son dos palabras que nombran el fenómeno. Los tres eslabones
(agujero → humo → llama) son físicos. Cumple las cuatro condiciones **si B vive**: la lengua a
sotavento y la columna enderezada son B. Sin B, el clip es «llama muere → agujero → llama vive»:
dos eslabones, sin inclinación, remate «RESPIRA». Sigue siendo un clip, pero no es el título.

La cápsula (un corte de tierra con una boca al cielo y humo en columna) se lee en un fotograma y es
honesta con la sombra de ONI: lo que TIRO tiene y ONI no es la conservación al bit, el co-op en
falling-sand y SOLTAR. Lo que ONI tiene y TIRO no es gas de colores a resolución legible: la batalla
es el render del humo, y el documento le da 0,7 semanas. Justo ahí es donde hay que mirar primero.

## 7. Ajustes desde esta lente

Ninguno cambia la dirección; todos atacan el eslabón del medio con materia en vez de con panel.

1. **La sordina que humea.** Invertir el signo de una línea (:885): `sordina ? (combustHumoPct + 3)
   / 4` → `sordina ? min(100, combustHumoPct × 3)`. Física real: la combustión pobre en oxígeno
   humea *más*, no menos (el carbonero lee el color del humo para saber cuándo tapar). Consecuencias
   en cadena, todas con leyes existentes: el cuarto sellado se llena de humo (visible), el humo come
   luz (la cámara se oscurece sola: R148), `LabRespira` cuenta humo (la sordina se realimenta: la
   carbonera funciona *mejor* y **humea por la boca**, que es exactamente el instrumento real), y
   la pila maciza no cambia (sus celdas interiores no tienen vecino vacío donde soltar humo:
   `SpawnSmokeNear` :941). Riesgo: la realimentación en el horno (36 celdas vacías, carbón
   ardiendo): el hash de «horno» y el 18/18 del vidrio son la puerta. Un día de banco. Si pasa, el
   estado invisible pasa a ser el más visible sin tocar el render, y vale aunque la dirección muera.
   No sustituye a A (el humo como aire gastado ya se midió «idéntico al bit» y una sola boca solo
   da contraflujo): es la legibilidad de A, no su física.
2. **El cuerpo respira.** La línea de §3. Convierte la frase en verdad y la Piel en tacto.
3. **Sin hada del aire.** La planta que repone o la boca del cielo como única fuente, medido;
   `AireRelaxTicks` solo como último recurso, declarado como el número de contingencia que es.
4. **Corrientes solo con B.** La vista no se enseña mientras el rumbo sea un hash.
5. **El prototipo feo sin J.** Para juzgar el core hacen falta A, el spike de B, humo a escala, la
   brasa en el frasco y el tinte: tres semanas sobre el editor actual (SOLTAR y ×10 existen; el
   contador de días es `_tick / 1800`). J es la infraestructura de comparar y validar, no del core.
   Llegar al primer playtest con el sello construido y el humo sin mirar es el patrón que `03 §3`
   lista como causa de muerte de los sistémicos.

## 8. La prueba más barata que la mata

**Hoy, media tarde, cero código.** La sordina ya existe por geometría. Montar en el editor una pila
de nueve fibras en un cuarto de roca de 14×10 sellado (o usar la caja sellada de r136 §3), prender,
F8 apagado, y preguntar a Cesar dos cosas: «¿este fuego respira?» y «¿por qué se apagó?». Si no
puede responder mirando la materia, **A no lo arregla**: A cambia cuándo entra la sordina, no qué se
ve cuando entra. Entonces el orden de construcción se invierte: primero la materia que enseña (la
sordina que humea, el render del humo), después el byte.

Después, en orden: (1) un día de banco con la inversión de la sordina (filas de humo bajo techo,
carbón de la carbonera, hash del horno); (2) A en la semana 1 con el desplazamiento en nacimientos
y Σaire; (3) «chimenea con boca N» con Σvy en la semana 2 (mata B y el título); (4) «La vela» sin
panel sobre el editor actual, sin sello, en la semana 2.

## 9. Tiempos

- **Evidencia para matarla: 2 semanas** (la prueba de hoy en la semana 0; A en la 1; chimenea y La
  vela sin panel en la 2). La dirección dice 1,5 y solo cuenta la ingeniería.
- **Prototipo feo que permita juzgar el core: 3 semanas**, sin J (A 1 + spike B 0,4 + bolsa 0,75 +
  humo a escala y lengua a sotavento 0,5 + brasa y tinte 0,3, en dos hilos). Con J y las ocho
  situaciones: 6-7, en paralelo, pero eso es el prototipo para *comparar*, no para *juzgar*.
- **Iteración humana incomprimible:** la mirada sobre el humo (2-4 sesiones), la elección de qué
  materia hace visible la asfixia (una decisión, una vez), el registro de autor de ocho
  situaciones. El productor la cifra en 14-22 días en tres meses; no tengo razones para bajarla.

## 10. Órganos a conservar si se descarta

**A entera** (el byte con conservación exacta, `LabRespira` y `ProcessFire` por umbral, la llama
mortal); **la bolsa en `LabPresion`**; **la brasa viva en el frasco**; **vidrio transparente + la
quinta pasada + el día de Q16**; **el agujero como válvula de cuatro flujos** (luz, aire, humo,
calor: un gesto, cuatro decisiones); **la máquina como gesto con hora** (la intervención
tick-estampada como forma de máquina: vale para cualquier dirección con sello); **el reloj lo pone
el fuego** (SOLTAR sin horizonte impuesto); **sorpresa y máquina como el mismo objeto** (principio
de onboarding); **la sordina que humea** (si pasa el banco, es una corrección del sustrato); **el
cuerpo que respira** (el cruce cuerpo × aire más barato del panel); **la trampa de agua** como
compuerta con hora; **«chimenea con boca N»** como escenario permanente del banco; **«TIRA» /
«RESPIRA»** como plantilla de remate de dos palabras; y **«la llama que dice por qué murió»** (tick
y aire del apagado en el sello).

## 11. Rúbrica v2 (13 ejes, 1-10)

| eje | nota | por qué (desde esta lente) |
|---|---|---|
| apalancamiento sistémico | 7 | un byte que leen fuego, llama, gas, agua, planta y (si se escribe) cuerpo; 13 decisiones seguras con A + bolsa, 22 si B vive |
| las leyes ejecutan, revelan y juzgan | 6 | ejecutan (9) y juzgan (8, por J); revelan (4): la asfixia es una ausencia y bajo A el humo lee un hash |
| iteración humana (10 = poca) | 6 | sin temporadas ni biblioteca; el ojo sobre el humo y el canon son las dos partidas caras |
| verificabilidad automatizable | 8 | todo el core tiene escenario y gate; la legibilidad no, pero la latencia de señal sí se puede medir |
| la simulación es el juego | 9 | la más pura del panel: cinco verbos, una ley, la máquina es un gesto con hora; −1 por las tres vistas en cinco minutos y la frase que promete una ley no entregada |
| onboarding garantizable | 7 | ocho situaciones ordenadas por leyes; la mitad depende de B |
| observabilidad | 5 | el estado central emite lo mínimo; vistas bien diseñadas pero sustituyen en vez de afinar; Corrientes sería «fake» sin B |
| tiempo como apuesta | 8 | el fuego es su propio reloj; loop de 1-3 minutos; el menos examen del panel |
| multiplayer emergente | 6 | «el mismo aire» es la mejor frase co-op; no se juega hasta host + espejo; el fichero no comparte aire |
| profundidad por leyes estables | 7 | el volumen sustituye a la tabla; la conservación acota; 6 si B muere |
| cuerpo del jugador | 5 | instrumento, no ley; la frase promete más; la línea que lo arregla es barata |
| identidad comercial | 7 | «TIRA» es el mejor clip del panel en atribución; muere con B; sombra de ONI |
| dificultad técnica (10 = fácil) | 6 | acotada en coste, abierta en resultado (B) |

Puertas: iteración 6 ≥ 5, apalancamiento 7 ≥ 5. Pasa las puertas; no pasa a la final sin las
pruebas.

## 12. Veredicto

**Segunda ronda.** Condiciones para volver como finalista, en orden de coste: (1) la prueba de hoy
sin código: Cesar lee la sordina actual sin F8, o el orden pasa a ser materia primero y byte
después; (2) la inversión de la sordina medida en banco con el hash del horno como puerta; (3) el
cuerpo respira o la frase cambia; (4) B con su escenario, y si muere, la dirección se retira como
«RESPIRA» dentro de «las leyes juzgan», sin B'; (5) el prototipo feo sin J en la semana 3, no en la
6. Con eso, es la dirección del panel en que la simulación es más juego y menos capa; sin eso, es la
mejor frase y el mejor clip sobre un eslabón que todavía no se ve.
