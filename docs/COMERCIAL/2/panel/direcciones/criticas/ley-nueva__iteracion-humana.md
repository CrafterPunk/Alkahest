# CRÍTICA · «TIRO / La segunda boca» (ley-nueva) · lente: ITERACIÓN HUMANA OCULTA

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: productor que ha visto morir sistémicos
en el playtest infinito. Único tema: cuánto playtest, tuning, balance, contenido de autor y
contingencia ante construcciones arbitrarias esconde la dirección, y qué parte se sustituye por banco
headless, hashes, generación y validación automática. Leído entero: `ley-nueva.md`, `01_LEYES.md`,
`03_MERCADO.md`, `00_ENCARGO_Y_CRITERIO.md` §3 (rúbrica y puertas), las cuatro refutaciones de
`aire-que-fluye` y `aire-atrapado`, la de apalancamiento de `adveccion-calor-vapor`, las dos de
`humo-respirado` (las medidas del humo), `Laboratorio/benchmarks/2026-09-04_r136_fable_tiro_y_hogar.md`
(la caja sellada), `docs/LAB/CHECKPOINT.md` §6g (HF1). Código: `SimStepper.cs` :829-930 (sordina a un
cuarto, lengua, `SpawnSmokeNear`, carbón), :1443-1600 (`ProcessGas`: rumbo por hash de bloque 8×8,
ondulación 30 %), `SimStepper.Laboratorio.cs` :1052-1066 (`LabRespira`: aire ≥ 1 y humo ≤ 1 por
material), :1080-1170 (`LabPresion`: la mudanza por `SetCell`), `Universe.Laboratorio.cs` :87-92 y
:169-174 (fibra 40 × 8 ticks, humo 16 %; carbón 50 × 8, humo 4 %), `LabParams.cs` (95 parámetros,
`VidaHumo` 255), `LabBench.cs` :76-87 y :265-280 (`Correr` apaga la boca del cielo: `LuzCieloX0/X1 =
−1`), `CellGrid.cs` :64-65 (pantalla 256×144 celdas: 7,5 px por celda en 1080p), `Net/SimSync.cs`
:30-57 (el espejo replica solo `mat[]`).)*

## 0. Veredicto en una línea

**Segunda ronda.** Como estrategia de producción es la dirección más limpia del panel: una ley, cinco
verbos que ya existen, cero temporadas, cero biblioteca, cero comportamiento humano específico, y el
espacio del jugador acotado por una cantidad conservada en vez de por una tabla. Pasa las dos puertas
de la rúbrica con margen. No la pongo en la final porque **el título cuelga de un experimento que
dos refutadores predicen negativo** (B, el tiro), y porque las dos partidas de iteración humana que
esconde son del tipo que no se comprime con hashes: **un canon físico que la aritmética del
combustible vuelve sobredeterminado** (acabará siendo balance por sala) y **la legibilidad de un
campo invisible cuya única materia visible, el humo, casi no nace en el código actual**. La cifra
honesta es **14-22 días de personas en los tres primeros meses** frente a los «4-6 horas y una
sesión por hito» que declara (unas 4×). Sigue siendo poco para un juego; es más de lo que dice, y
está en el camino crítico. Si en la semana 3 el banco mide `Σvy > 0` y Cesar lee el humo sin F8, es
finalista con estos números; si no, sus órganos valen más que ella y se van a «las leyes juzgan»,
como la propia dirección prevé.

| campo | valor |
|---|---|
| **veredicto** | segunda ronda (finalista condicionada a las tres pruebas de la semana 1-3) |
| **riesgo mayor (esta lente)** | el «único número humano» no existe como tal: el canon «un cuarto de 10×10 ahoga nueve fibras en un día» no se puede observar (la pila entera arde en 500-900 ticks, el día son 1 800) y sus identidades tiran del mismo parámetro en sentidos opuestos (`AireConsumo` alto para ahogar el cuarto, bajo para que el fogón abierto no parpadee), sobre tres resultados ya calibrados que el byte reabre (vidrio 18/18 en horno sin boca, carbonera 25 %, tolva 466 s). Cuando no cierren a la vez, alguien elige cuál cede y lo repite por situación: «la sala no tiene tabla, tiene volumen» se convierte en «el volumen es la tabla» |
| **iteración humana oculta** | 14-22 días en tres meses (declarado ≈ 3-4): compromiso del canon y tamaño de sala por situación (1-3), lenguaje visual del humo por el ojo (3-4, la partida más cara; 2 si se separa del tuning físico con render de persistencia y métrica de banco), ocho situaciones con condición numerada, registro de autor y segunda vuelta (4-6), re-balance de horno/carbonera/tolva como decisión (1-2), re-resolver registros tras B, bolsa y vapor (2-4), mesa de 2-3 sin aire compartido real (1) |
| **se automatiza en su lugar** | barrido nocturno del canon sobre `AireConsumo × AireDifusion × AireMinRespira × caudal` contra seis identidades (imprime si existe solución y **lee** el tamaño de sala, no lo elige); `Σaire` con desplazamiento en los ocho sitios que crean o destruyen `Empty` como assert; «chimenea con boca N» con `Σvy` y fuente de aire desacoplada de `LuzCielo`; campana; horno con boca 1..4 contra 18/18; render de persistencia del humo fuera del sim (sin hash, sin tocar `combustHumoPct`); métrica de legibilidad en banco como puerta antes de que mire una persona; umbrales de la escalera por validador; gemelos por situación; regresión nocturna de los ocho registros; IL2CPP contra editor |
| **prueba que la mata** | (0) hoy, media tarde, sin código: la caja sellada de r136 en el editor con F8 apagado; si una pluma de 1-9 celdas a 7,5 px no se lee, el render va antes que el byte; (1) una noche de banco tras A: si ningún punto cumple «el cuarto sellado ahoga la llama antes de media pila» y «el fogón abierto no entra en sordina» y «la chimenea de boca 3 sostiene», el canon es balance; (2) las dos pruebas de la dirección (`Σvy`; «La vela» sin panel) adelantadas a la semana 2-3 sobre el editor actual, sin sello |
| **semanas hasta evidencia para matarla** | 2,5 (la dirección dice 1,5 y cuenta solo la prueba de ingeniería) |
| **semanas hasta prototipo feo** | 7 (6 con J recortado a registro + rejugar + condición + contador; 8-9 con validador, cuna y las ocho situaciones) |

## 1. Lo que no esconde, y por eso compite

Desde la silla del productor hay que decirlo primero. No pide temporadas ni eventos. No pide
biblioteca: ocho montajes de 10-15 líneas. No depende de que tres personas se comporten de una
manera (el co-op es «el mismo aire», no roles). No pide balancear geometrías: la conservación acota
el espacio, y una sala mal hecha se ahoga sola en vez de romper una tabla. Sus dos pruebas de muerte
están nombradas, son baratas y tienen criterio numérico, y la dirección dice qué muere si B muere.
La sorpresa y la máquina son el mismo objeto (un agujero), lo que cierra el hueco que mató a la
primera pasada, y el reloj lo pone el fuego, no un horizonte: el «examen» de SOLTAR se mitiga por
ley. Ninguna otra dirección del panel es tan clara sobre su propia falsación. Lo que sigue es lo que
**no** cuenta.

## 2. Las siete partidas que el §7 no cuenta

| partida | declarado | corregido | por qué |
|---|---|---|---|
| **el canon** | «UN número; tres incógnitas, tres identidades, cero balance» | un número + **el compromiso** cuando las identidades no cierren + **el tamaño de sala de cada situación** | §3: la aritmética del combustible y las tres calibraciones existentes lo vuelven sobredeterminado. El banco dice si hay solución; si no la hay, la elección es humana y se repite por sala |
| **el humo como veleta** | «render a escala legible, 0,7 sem; dos o tres miradas» | **3-4 días** de láminas por el ojo, y con riesgo de tocar física | §4: en la caja sellada de r136 el humo medio es 1,35-5,4 celdas y la razón está en `SpawnSmokeNear`; subir `combustHumoPct` cambia `LabRespira`, la sordina y los hashes: ajustar la legibilidad es ajustar la carbonera |
| **ocho situaciones** | «10-15 líneas + condición como dato; ningún número lo elige una persona» | 8 × (montaje + condición **con número** + **registro del autor jugado por una persona** + juicio de «enseña» + segunda vuelta) | el validador exige que el registro del autor cumpla: alguien lo juega. «Llama viva el día 2», «carbón ≥ 6 el día 3», nueve fibras, 14×10: cada uno es un número de autor. Y 3-4 peldaños (3, 6, 7, 8) **no existen si B muere** |
| **re-balance de horno, carbonera y tolva** | 0,5 sem de Opus | + **1-2 días de decisión** | son los tres únicos resultados calibrados del laboratorio; el byte reabre los tres (el horno de `MontarHorno` es una caja sin boca de ~36 celdas de aire que la fila de carbón agota en ~1 000 ticks: el 18/18 pasa a depender de `AireConsumo`) y alguien fija el nuevo punto |
| **caducidad** | no aparece | cada entrega de la propia dirección (A → B → bolsa → vapor → vidrio) cambia el campo: los registros de autor hechos bajo A caducan con B | regresión nocturna automatizable (0,3 sem); re-resolver, humano: 1-2 días por entrega, y después por cada paquete L, V, F |
| **relajación hacia 128** | «asfixia local, no global» | un parámetro global que decide **a partir de qué tamaño de sala la ley deja de existir**: la contingencia ante construcciones arbitrarias, una y central | §6; mejor que un slider es la ley del crítico de huecos (la planta repone aire donde tiene luz); si no, `AireRelaxTicks` se reelige tras cada sesión de sandbox |
| **co-op «el mismo aire»** | «uno sella su carbonera y la fragua del otro entra en sordina» | no se puede probar sin host + espejo (3-4 sem, fuera del presupuesto), y `SimSync` replica solo `mat[]`: el invitado no vería ni Presión ni Piel | §7; el asíncrono por fichero no comparte aire; la mejor frase co-op del panel es promesa hasta la ruta A |

## 3. El canon está sobredeterminado, y se puede leer en vez de elegir

Datos del código. Una fibra: `combustReserva 40 × combustPasoTicks 8 = 320 ticks` (11 s) por celda
respirando; en sordina, uno de cada cuatro pasos. Nueve fibras con propagación 18 % arden en cadena:
la pila entera dura **500-900 ticks**; el «día» son 1 800. Primera consecuencia: **el canon tal como
está escrito no se puede observar**: la pila se acaba antes del día, con aire o sin él. Hay que
reescribirlo en fracción de combustible («ahoga la llama antes de media pila»), no en días.

Segunda: un cuarto de 10×10 con la pila son ~91 celdas de aire × 128 = **11 648 unidades**; para que
la media baje del umbral (`AireMinRespira 32` en la refutación) hay que retirar ~8 700. La pila son
360 pasos de combustión: **24 unidades por paso**, y **~48** si la llama debe morir a media pila.
La refutación de apalancamiento ya midió el otro extremo: la difusión repone como mucho 12 u por
visita y vecino, y «subir el consumo ×10-20 hace parpadear en sordina al fogón abierto». Estamos
exactamente ahí: la identidad «el cuarto sellado muere» pide consumo alto; la identidad «la chimenea
de boca 3 sostiene la misma pila» y la calibración del fogón abierto de «laboratorio base» piden
consumo bajo; el único grado de libertad entre ambas es `AireDifusion`, que a su vez decide si el
régimen es **local** (todo se ahoga, abierto o cerrado, porque el aire no llega) o **de reserva** (la
sala grande nunca se ahoga). Hay una banda donde ambas cosas ocurren o no la hay; eso lo dice el
banco en una noche. Encima están el horno (18/18 con caja sin boca), la carbonera (25 % por
geometría, que pasa a ser por caudal) y la tolva (466 s): seis identidades, cinco parámetros, y la
sexta («cuatro pilas no asfixian el mundo») depende de un séptimo (`AireRelaxTicks`), que es el que
la ley quería evitar.

**La salida de producción que la dirección no toma**: no *elegir* el canon, *leerlo*. Fijar
`AireConsumo` por la restricción dura que ya existe (el fogón abierto de «laboratorio base» y la tolva
no cambian su cuenta de sordina), y que el banco **imprima** el tamaño de sala que ahoga nueve fibras
a media pila. Ese número es el de «La vela», y no lo elige nadie. Con la aritmética de arriba, a
consumo ≈ 12 u/paso la sala sale de unas 22-45 celdas de aire (5×5 a 7×7): un armario para un muñeco
de 2×4. Si el banco confirma ese orden, la vela no existe a escala de fibra y la situación 1 se
monta con carbón (50 × 8 = 400 ticks por celda, pero humo 4 %: aún menos veleta) o con más pila. Es
un criterio de muerte numérico que la dirección no tiene y que cuesta una noche.

## 4. La veleta no tiene materia, y el ojo la itera

El riesgo mayor de la dirección es que el humo no se lea como corriente. Desde esta lente el problema
empieza antes: **casi no hay humo**. En la caja sellada de r136 (20×12 interior, 240 celdas de aire)
la fibra da humo máximo 27 y medio **5,4** celdas; el carbón, máximo 20 y medio **1,35**; y subir el
humo del carbón de 4 % a 40 % es *idéntico al bit*, porque `SpawnSmokeNear` solo suelta humo en un
vecino vacío y el único vecino libre lo ocupa la llama. Con la regla de tiro emulada, la bolsa fue de
9 celdas sin chimenea y 0 con chimenea de 8; la carbonera de boca 1 (HF1) da **0 humo**. La veleta
tiene entre 0 y 9 celdas, a 7,5 px por celda en 1080p: una raya de 4-10 px. Es el problema de las
«plantas de un píxel», que 150 rondas no resolvieron, trasladado al gas.

Y aquí está la trampa de iteración. Lo natural cuando «se lee a medias» (que es lo normal, no «no se
lee») es subir `combustHumoPct`, `VidaHumo`, la densidad por paso, la ondulación. Todos esos números
**son física**: `LabRespira` cuenta humo (humo ≤ 1 respira), así que más humo es más sordina, otra
carbonera y otros hashes. Ajustar la legibilidad del humo es ajustar la carbonera. Ese acoplamiento es
lo que convierte tres miradas en cinco sesiones y en un mes.

Dos cosas lo abaratan, y las pido como condición. **(1) Separar por construcción legibilidad y
física:** un búfer de **persistencia** en el render (un byte por celda o por bloque que sube cuando
pasa humo y decae solo, fuera del stepper, sin hash), de modo que una celda de humo que sube deje
una estela y la pluma se lea como pluma sin cambiar una sola tirada de la simulación. Con B, la
vista Corrientes ya pinta `viento`; con A sola, la estela es lo único que hace visible el camino.
**(2) Una métrica de legibilidad en banco** antes de que mire nadie: filas de humo coherentes bajo el
techo del cuarto sellado, y fracción de ticks en que la columna de la chimenea tiene ≥ N celdas con
`|vx| ≥ 2` del mismo signo. Si la métrica no supera un mínimo, Cesar no mira todavía. Lo que queda
humano es una mirada sobre láminas, no una campaña.

## 5. Ocho situaciones son pocas, pero son un treadmill con dos motores

El validador por veredicto (vacío falla, K perturbaciones difieren, el autor cumple) hace bien la
mitad del trabajo. La otra mitad: el número de cada condición lo escribe una persona; el registro del
autor lo juega una persona (es la única prueba de que hay solución); que la situación *enseñe* lo
juzga una persona; y la escalera caduca dos veces, con cada entrega de la propia dirección (B cambia
el aire de todo; la bolsa cambia las situaciones 4-5; el vapor las 6-7) y con cada paquete posterior.
Media jornada por situación y vuelta, dos vueltas: 4-6 días. Automatizable: la regresión nocturna que
rejuega los ocho registros contra la física del día e imprime cuáles dejaron de cumplir; los
**gemelos** (la misma situación con la boca una celda al lado o el toque un tick después) para que «K
perturbaciones» mida ruido y no solo discriminación. Humano: re-resolver los rotos.

La contingencia que sí existe: si B muere, los peldaños 3, 6, 7 y 8 desaparecen y «La vela» pierde
su segunda mitad (la boca baja que endereza el humo). No se reescriben cuatro montajes: se reescribe
la escalera. La dirección lo sabe y no lo presupuesta.

## 6. Contingencias ante construcciones arbitrarias

Es la partida donde esta dirección está mejor que casi todas: no hay geometría que balancear, porque
el volumen es la variable y la conservación la acota. Pero tiene una contingencia central y dos
impuestos.

La central es `AireRelaxTicks`. La cueva es cerrada y la boca del cielo de 7 columnas inyecta ≤ 14
u/tick: un jugador que sella la boca ahoga con el tiempo todos los fuegos del mundo; uno que abre
cincuenta bocas vuelve el aire trivial. La relajación hacia 128 decide a qué tamaño de sala la ley
deja de importar, y ese número se elige contra las salas de los jugadores, es decir, después de cada
sesión de sandbox. La ley del crítico de huecos (la planta repone aire donde tiene luz) lo sustituye
por un consumidor real; hay que adoptarla o declarar el slider como lo que es.

Los impuestos son de ingeniería, no de personas, pero son secuenciales: (a) todo lo que crea o
destruye `Empty` desplaza aire (`LabTransformar`, `LabNacerAgua`, `Transform`, `LabGotear`, la ceniza
de `ProcessFire`, la planta que crece, la mudanza de `LabPresion`, `LabTragar`, la expiración de gas:
ocho o nueve sitios), y **cada ley futura paga lo mismo**; el assert `Σaire` lo caza, pero cada
paquete lleva medio día más. (b) El banco no tiene fuente: `Correr` pone `LuzCieloX0/X1 = −1` para que
el hash signifique «esta física»; la fuente de aire ligada a la boca del cielo no existe en los nueve
escenarios ni en «chimenea con boca N» hasta que se desacople de la luz. Una línea, pero sin ella el
spike de B mide un sistema cerrado.

## 7. Lo que no se puede probar en el presupuesto

«El mismo aire» es la mejor frase multijugador del panel y no se puede jugar: `SimSync` replica solo
`mat[]` (su docblock deja fuera `temp[]` y `morph[]` a propósito); un byte `aire` nuevo no llega al
invitado, que no vería ni Presión ni Piel. La asimetría «quien está lejos solo ve el humo» es verdad
por accidente. Host + espejo son 3-4 semanas fuera de este presupuesto; el asíncrono por fichero
intercambia registros, no aire. Los «1-2 días de mesa» solo pueden probar el relevo por fichero.

## 8. Qué se automatiza en su lugar

- **Barrido del canon** (§3): existencia de solución para las seis identidades y lectura del tamaño
  de sala; una noche; imprime el punto o la contradicción. No elige.
- **`Σaire` exacto** con desplazamiento en los ocho sitios: assert en los nueve escenarios y «campana».
- **«Chimenea con boca N»** con `Σvy` abierta frente a tapada y fuente desacoplada de `LuzCielo`.
- **Horno con boca 1..4** contra el 18/18: la recalibración se mide; la decisión no.
- **Render de persistencia del humo** fuera del stepper (§4) y **métrica de legibilidad** como puerta.
- **Umbrales de la escalera** por validador; **gemelos**; **regresión nocturna** de los ocho registros.
- **IL2CPP contra editor** (un día): sin esto «comparar registros por fichero» es promesa.
- **La planta que repone aire** en vez de `AireRelaxTicks`.

Lo que no se automatiza y se presupuesta: cuál identidad cede si el barrido no cierra; la mirada
sobre las láminas del humo; el número de cada condición; si el fuego que se apaga solo es tensión o
espera; re-resolver registros; la mesa por fichero.

## 9. La prueba más barata que la mata, ordenada por coste

0. **Hoy, media tarde, sin código.** La caja sellada de r136 en el editor con F8 apagado. Cesar dice
   por dónde va el humo y si el fuego «respira». Si una pluma de 1-9 celdas no se lee a 7,5 px, el
   render (persistencia) va **antes** que el byte, no después.
1. **Semana 1, una noche de banco tras A.** El barrido de §3. **Mata** si ningún punto cumple a la vez
   «el cuarto sellado ahoga la llama antes de media pila», «el fogón abierto no entra en sordina» y
   «la chimenea de boca 3 sostiene la pila»: el canon es balance y «cero balance» es falso. **No mata
   pero cambia la nota** si existe un punto y rompe el 18/18 o el 25 %: la dirección hereda tres
   recalibraciones humanas. Y si el tamaño de sala leído es menor que 6×6, la vela no existe a escala
   de fibra.
2. **Semana 2, dos días.** «Chimenea con boca N» tal como la escribe la dirección. Mata B y con B el
   título; A y la bolsa pasan a «las leyes juzgan».
3. **Semana 2-3, un día, una persona.** «La vela» sin panel **sobre el editor actual**, sin sello: A +
   persistencia + brasa en el frasco bastan para preguntar por qué murió la llama y por dónde entró
   el aire. La dirección la pone en la semana 6 con J construido; adelantarla es la corrección de
   producción más importante de esta crítica. Llegar al playtest con la infraestructura hecha y el
   core sin probar es el patrón exacto de `03 §3`.

## 10. Tiempos corregidos

**Automatizable y paralelizable (Opus, con prueba de banco por pieza):** A con desplazamiento y
auditoría (1); fuente en banco (0,05); B como spike (0,4); bolsa (0,75); vapor (0,2); vidrio + quinta
pasada + Q16 (0,4); render de persistencia y vistas (0,7); brasa y sensores (0,3). ≈ 3,8 semanas en
dos hilos. J en paralelo: 4,5-5 semanas para registro + volcado + `CorrerSello` + condición
(no 5-6 con validador y cuna), sin tocar la física.

**Secuencial:** A → barrido → chimenea → (B o no B) → escalera; y J → validador → cuna → las ocho
situaciones aceptadas. Camino crítico: J recortado (4,5-5) + escritura y validación de las ocho (1) +
reescritura de la escalera si B muere (0,5).

**Humano e incomprimible (tres primeros meses):**

| partida | días |
|---|---|
| caja sellada hoy + «La vela» sin panel + tensión o espera (3 sesiones) | 2 |
| compromiso del canon y tamaño de sala por situación (1 si se lee; 3 si se elige) | 1-3 |
| lenguaje visual del humo (2 con persistencia y métrica; 4 sin ellas) | 2-4 |
| ocho situaciones: condición + registro del autor + juicio + segunda vuelta | 4-6 |
| re-balance de horno, carbonera y tolva como decisión | 1-2 |
| re-resolver registros tras B, bolsa y vapor (1-2 por entrega) | 2-4 |
| mesa y relevo por fichero | 1 |
| **total** | **14-22** (declarado ≈ 3-4) |

**Hasta evidencia para matarla: 2,5 semanas** (prueba 0 hoy; A en la semana 1; barrido, chimenea y
La vela sin panel en la 2-3). **Hasta prototipo feo: 7 semanas** (6 con J recortado; 8-9 con
validador, cuna y escalera completa).

## 11. Órganos a conservar si se descarta

**A entera** (byte `aire` con conservación exacta y desplazamiento en los ocho sitios; `LabRespira` y
`ProcessFire` por umbral: la llama inmortal muere en cuarto cerrado; apagar cerrando; bancar brasas;
qué cuarto se ahoga primero); la **bolsa en `LabPresion`** (campana, cámara que solo se inunda hasta
el túnel, bomba de dos tiempos con el núcleo frío que ya existe); **vidrio transparente + quinta
pasada + el día de Q16**; **la brasa viva en el frasco**; la **trampa de agua como compuerta con
hora**; **«chimenea con boca N»** como escenario permanente aunque B muera (mide que no hay tiro, que
también es un hecho); el **método del canon leído en banco** (identidades, no sliders) como patrón
para D («el hogar come») y F; la **planta que repone aire**; la **escalera ordenada por leyes
implicadas**; las vistas **Presión y Corrientes por bloque 8×8** como halo de la Piel; el **render de
persistencia del humo** y la **métrica de legibilidad**; el clip «TIRA» como plantilla de remate de
dos palabras; y «la llama que dice por qué murió» (tick y aire del apagado) como cláusula del sello.

## 12. Rúbrica v2 (13 ejes, 1-10)

| eje | nota | por qué (desde esta lente) |
|---|---|---|
| apalancamiento sistémico | 6 | 22 decisiones sobre un byte si B vive (7-8), 13 si muere (5); con dos refutadores en contra, la esperanza es 6 |
| las leyes ejecutan, revelan y juzgan | 7 | ejecutan (conservación) y juzgan (J) de verdad; «revelan» es la apuesta sin medir, y es la mitad del loop |
| iteración humana (10 = poca) | 6 | 14-22 días frente a 3-4; sin temporadas ni biblioteca, pero dos partidas del tipo caro (canon sobredeterminado, legibilidad por el ojo) en el camino crítico |
| verificabilidad automatizable | 8 | toda la física tiene escenario, métrica y gate; la legibilidad y el compromiso no, y la métrica de banco solo acota |
| la simulación es el juego | 8 | cinco verbos existentes y una ley; el sello es árbitro, no capa; el examen se mitiga por ley |
| onboarding garantizable | 6 | ocho situaciones validadas, pero con registro de autor humano y media escalera condicionada a B |
| observabilidad | 5 | el campo es invisible por naturaleza; el humo medido es 1-9 celdas y 0 en la carbonera; tres resoluciones bien pensadas, ninguna medida |
| tiempo como apuesta | 7 | el fuego como reloj es real pero corto (11 s por celda): la apuesta de días es de la tolva y la carbonera, no de la vela |
| multiplayer emergente | 5 | «el mismo aire» no se puede jugar hasta host + espejo, y `SimSync` no lleva el byte; el fichero no comparte aire |
| profundidad por leyes estables | 6 | si B vive, 7; el volumen de la sala es el número de dificultad y cada entrega de física reinicia los registros |
| cuerpo del jugador | 5 | instrumento (jadeo, tos, tizne, brasa), no ley; honesto |
| identidad comercial | 6 | «TIRA» cumple las cuatro condiciones del clip; sombra de ONI; el clip no existe si B muere |
| dificultad técnica (10 = fácil) | 6 | acotada en coste, abierta en resultado: B es un experimento y la conservación en nacimientos toca ocho sitios y grava cada ley futura |

Puertas: iteración 6 ≥ 5, apalancamiento 6 ≥ 5. Pasa; no entra en la final sin las pruebas.

## 13. Veredicto y condiciones

**Segunda ronda.** Vuelve como finalista si: (1) la mirada sobre la caja sellada y «La vela» sin
panel se hacen en la semana 2-3 sobre el editor actual, no en la 6 con J construido; (2) el barrido
del canon corre antes de escribir una sola situación y su resultado (existe / no existe / existe y
rompe qué / qué sala sale) se escribe en la dirección, y el canon se reformula en fracción de
combustible; (3) B se mide con su escenario y una fuente en el banco y, si muere, la dirección se
retira dejando los órganos de §11 en «las leyes juzgan», **sin intentar un B'** (el segundo intento
de bombear es donde empieza el playtest infinito); (4) la legibilidad del humo se separa de la
física por construcción (persistencia en el render, métrica en banco) para que ninguna mirada toque
`combustHumoPct`; (5) la cifra de iteración se corrige a 14-22 días y se presupuestan gemelos y
regresión nocturna; (6) `AireRelaxTicks` se sustituye por la planta que repone aire o se declara
como el número de contingencia que es. Con las pruebas superadas es la dirección del panel cuya
profundidad crece más rápido que su coste de diseño, porque su contenido es geometría de agujeros
sobre una cantidad conservada; sin ellas es una hipótesis de física con un clip muy bueno.
