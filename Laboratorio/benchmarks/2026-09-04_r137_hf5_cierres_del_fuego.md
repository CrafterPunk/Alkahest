# R137 — HF5: LOS CIERRES DEL FUEGO

Banco headless (`Unity_RunCommand`, sin Play). Defaults de `LabParams` salvo donde se diga.
Los cuatro parches de Fable (R13 del buzón), más un quinto que hizo falta para poder comprobar
el tercero. **El código va marcado `(R136)`** —la ronda de diseño que los especifica, como
pidió Fable—; las medidas de aquí son la R137.

---

## C1 · El hogar es doméstico de verdad

`LabHogar` ya no llama a `InjectHeat` (que suma sin tope) sino a `LabCalentarHasta`, que no
empuja a nadie por encima de `fuego.hogarRaw` = 170. `suelo.terracotaRaw` ya estaba en 150, así
que no hubo que bajarlo.

Cuatro plataformas idénticas —solera de roca, tres celdas de hogar, paredes que retienen el
polvo, carga encima—, 3 000 ticks:

| carga sobre el hogar | resultado | Tmax |
|---|---|---:|
| arena + ceniza | **0 vidrio**, la arena sigue arena | 170 raw (220 °C) |
| agua | hierve entera y el vapor se va | 170 raw |
| fibra | prende: 5 ceniza + 1 brasa | 255 raw |
| carbón | **prende también**: 5 ceniza + 1 brasa | 255 raw |

Los tres primeros son los que Fable pedía. **El cuarto falla**, y la causa está fuera del
laboratorio: `TryIgnite` (`SimStepper.cs:1763`) enciende cualquier vecino inflamable con
`hotEnough || rng.ChancePercent(12)`. El 12 % por tick no mira la temperatura de ignición, así
que en 3 000 ticks el hogar prende por contacto lo que sea, carbón incluido. Es regla base del
juego, no del laboratorio, y no la he tocado. Escalado como **Q10**.

**Y eso obliga a corregir un hallazgo de R135.** Allí escribí que había aparecido sola una
cadena de encendido —hogar → yesca → llama → carbón— porque el hogar (170) no alcanza la
ignición del carbón (200). Esa explicación era falsa: el hogar sí lo enciende, por el 12 %. Lo
que de verdad sostiene la cadena es **geometría**: en el horno la solera de ceniza se interpone
entre el piloto y el carbón, y sin yesca que atraviese esa capa el carbón no prende. Medido
abajo, y es un mecanismo mejor que el que creí tener.

## C2 · Carbonizar ya no crea energía

Rama F2 de `ProcessCombustion`: solo `fuego.rendimientoCarbonPct` = 25 de las celdas ahogadas
deja carbón (sal 632); el resto, ceniza. `Carbon.combustReserva` 160 → **50**.

Carbonera de 400 celdas de fibra en recinto de roca, boca de 1, piloto pegado:

| momento | fibra | carbón | ceniza |
|---|---:|---:|---:|
| t=2 000 | 344 | 12 | 44 |
| t=2 500 | 183 | 53 | 159 |
| **t=3 250 (la fibra se agota)** | **0** | **100 = 25,0 %** | 298 |
| t=5 000 | 0 | 0 | 329 |

**(Corregido en R139.)** A la tabla le faltan las celdas de **brasa**: el carbón agotado no va
a ceniza directamente, va a brasa —bancada ×4 si está tapada— y de ahí a ceniza. En t=5 000
faltan del recuento unas 70 celdas que están en ese estado intermedio.

**El pico de carbón cae exactamente en el 25,0 %**, y `LabCarbonizado` da 102 de 400 (25,5 %).
El criterio «25 % ± 5» se cumple sin margen de duda.

### La identidad, y el quinto parche

La identidad de Fable (`LabCalorFuego + LabEnergiaCarbon ≈ celdas × 560`) daba **+27,6 %**. La
causa medida: **el carbón nace y vuelve a arder**, y su energía se cuenta dos veces — una al
nacer (`LabEnergiaCarbon`) y otra al quemarse (`LabCalorFuego`). Retirar el piloto a los 1 500
ticks no cambia nada: dentro del recinto el frente ya es autosostenido.

Los totales lo confirman sin ambigüedad: `LabCombustibleQuemado` = 21 100 = fibra 16 000
(400 × 40) + carbón 5 100 (102 × 50), **exacto**.

Por eso añadí `LabCalorCarbon` (una línea en la costura ya autorizada): de la brasa, la parte
que puso el propio carbón. Restándola queda el calor del combustible original, y la identidad
se puede escribir sin contar nada dos veces:

`(LabCalorFuego − LabCalorCarbon) + LabEnergiaCarbon ≈ celdas × 560`

| pila (fibra) | carbonizado | identidad |
|---|---:|---:|
| maciza 20×20 (400) | 27,0 % | +5,5 % |
| media 40×10 (400) | 22,5 % | **−0,8 %** |
| fina 100×4 (400) | 19,5 % | **+0,8 %** |
| **maciza 30×30 (900)** | **25,1 %** | **+1,0 %** |

El +5,5 % de la primera baja a **+1,0 %** con 900 celdas. **La identidad cuadra.**

**(Precisado en R139.)** Dos cosas: por celda ahogada el código da **555** raw, no 560
(40×7 + 0,25×50×22), y la identidad es **ESTADÍSTICA, no por construcción** — la carbonización
se decide con la sordina del ÚLTIMO paso y sin memoria por celda, así que el +5,5 % no era solo
ruido del sorteo: eran celdas que respiraron parte de su vida antes de acabar tapadas. Con
n ≥ 900 cuadra a ±1 %. Repetida tras R15 (el hogar ya no enciende por azar), la 20×20 da
**+2,6 %**.

Y el número que explica por qué el 25 y el 50 son los correctos: de los 224 000 raw de la fibra,
la combustión ahogada suelta 112 000 y el carbón que nace guarda **112 200**. Media y media, con
un 0,2 % de diferencia. No es una calibración aproximada: es la mitad justa.

## C3 · El libro cuenta los tres calores

`LabCalorLlama` (una línea junto al `InjectHeat(x, y, 40)` de `ProcessFire`; la lengua no se
toca), `LabCalorHogar` y `LabCalorCarbon`, más `LabCarbonizado` y `LabEnergiaCarbon`. El panel
los muestra con la identidad de la carbonera.

Lo que se ve al contarlos: en el horno de HF2, 130 240 de llama contra 45 400 de brasa. Y en la
carbonera fina, 1 333 200 de llama contra 267 560 de la maciza a igual masa: **cinco veces más**.

**(Corregido en R139, y es el error más interesante de esta ronda.)** De ahí concluí que «la
llama es la fuente dominante». Es falso, y Fable lo corrigió a «¾» con la misma cifra, que
también lo es: **las dos son del libro NOMINAL**. Contando los raw que de verdad se escriben en
la grilla, la llama pone entre el **6 %** (carbonera sellada) y el **29 %** (hoguera al aire), y
la fuente que más escribe es siempre la combustión. La causa: en la hoguera la llama suelta
622 040 nominales y entrega 105 579 —un 4 % de los ≈ 2 488 160 que intenta, ya que cada tick
prueba 40 en cada uno de sus cuatro vecinos más el pin a 255—, porque lo que ya está a 255 no
admite más. Cuanto
más se parece el sitio a un horno, MENOS entrega la llama. La comparación fina/maciza sí se
sostiene: es entre dos cifras nominales del mismo tipo.

## C4 · `fuego.vidaHumo` 400 → 255

Era un byte y 400 se recortaba en silencio. Ayuda reescrita con el tope y con los 510 ticks que
dura bajo techo, donde cuenta doble.

---

## Criterio 3, cerrado como «recinto y contacto»

A **igual masa (400 celdas) y la misma boca (1)**, cambiando solo la forma de la pila:

| geometría | carbonizado | llama (raw) |
|---|---:|---:|
| maciza 20×20 | 27,0 % | 267 560 |
| media 40×10 | 22,5 % | 541 480 |
| fina 100×4 | 19,5 % | **1 333 200** |

Monótono en las dos columnas: cuanto más extendida la pila, menos carbón y más llama. **El
mando de geometría existe y vive en el combustible**, no en la boca del recinto — que es lo que
sostuve en Q9 y Fable confirmó en R12. La boca no regula porque el aire nunca se consume; el
contacto sí, porque la sordina se decide vecino a vecino.

## Aceptaciones

| # | criterio | resultado |
|---|---|---|
| 1 | hogar + arena + ceniza → 0 vidrio a 3 000 ticks | **✔** 0 |
| 1 | agua sobre el hogar hierve | **✔** |
| 1 | fibra pegada prende | **✔** |
| 1 | carbón pegado NO prende | **✘** prende por el 12 % de `TryIgnite` (**Q10**) |
| 2 | HF2 con yesca sigue vidriando ≥ 10 | **✔** **18 de 18** en t=3 500 |
| 2 | el hogar suelto, 0 vidrio | **✔** 0 |
| 3 | B-F3 boca 1 → 25 % ± 5 de carbón | **✔** 25,0 % en el pico |
| 3 | identidad de C2 a ± 5 % | **✔** +1,0 % con n=900 (tras añadir `LabCalorCarbon`) |
| 4 | HF3 (tolva) sin cambios | **✔ mejora**: 466 s por encima de 150 raw (antes 324) |
| 4 | regresión del agua, residuo 0 | **✔** 0 a los 50, 150, 300 y 600 s |
| 5 | criterio 3 como «recinto y contacto» | **✔** tabla de arriba |
| 5 | criterio 5 como «todo raw contado + identidad» | **✔** |

### El horno, con la solera que sí explica la cadena

Recinto de roca 20×14, solera de ceniza, carga de arena entre columnas de ceniza, carbón
alrededor, piloto en la solera:

| | vidrio | carbón a 300 s | Tcarga |
|---|---:|---:|---|
| **con yesca** | **18 de 18** (t=3 500) | 0, todo ceniza | 255 raw = 390 °C |
| sin yesca | **0** | 144 intactas | 170 raw = 220 °C |
| recinto y yesca, sin carbón | 1 | — | 170 raw |

Las tres líneas juntas son la frontera entera: sin combustible la yesca da **una** celda de
vidrio; con combustible pero sin quien lo encienda, **ninguna**; con las dos cosas, la carga
completa. Y el hogar suelto al aire, cero.

### La tolva

360 celdas de fibra sobre un fogón con boca de 3, cero intervenciones: **13 989 ticks (466 s)**
por encima de 150 raw, y seguía caliente al cortar el banco a los 14 000, contra los 324 s de
R135.

**(Corregido en R139.)** Escribí que subía «porque el hogar ya no sobrecalienta la base y arde
más ahogada». El código no lo respalda: el consumo no depende de la temperatura. La razón
calor/reserva cae de 10,3 a 8,3 por el cambio del carbón en C2, no por C1, y además esa razón
mezclaba los 22 del carbón con los 14 de la fibra. Medido bien en R139: **7,3 raw/u de fibra
sola y 3 % de unidades respiradas**. Los 466 s son la medida; la causa se queda sin atribuir
hasta separar C1 de C2.

### Coste

| escena | ms/tick |
|---|---:|
| laboratorio en reposo | 1,85 |
| incendio de 5 000 celdas | 1,80 |

Sin regresión (R135 daba 1,90 y 1,95).

---

## Q8 · El desagüe de grava

Puesto en `SimLevelBuilder.Laboratorio.cs`, 4 líneas de `Bloque`: dos columnas de grava junto a
cada labio (x134-135 y x154-155) y la mitad baja del propio labio también de grava, para que el
agua salga a la boca de la chimenea. **La conservación no se resiente: residuo 0 a los 50, 150,
300 y 600 s.**

Pero el banco **no reproduce el encharcamiento de R135**, así que la aceptación revisada de H4
sigue sin poder evaluarse. Con rocío repartido por el techo a 3 celdas/s, el lecho oeste llega a
una humedad media de 12 en 300 s — muy por debajo de los 60 que pide germinar. Y forzando a 10
celdas/s durante 166 s:

| | anegadas | humedad media |
|---|---:|---:|
| sin desagüe (piso de R135) | 0 / 36 | 0 |
| **con desagüe de grava** | 3 / 36 | **5** |

El desagüe deja **más** agua en el lecho, no menos: la grava (permeabilidad 90) le da al agua
superficial un camino hacia el subsuelo en vez de dejarla correr hasta la boca, y el labio de
grava fija el nivel a la altura del lecho. Es un drenaje **con nivel freático**, que es lo que
quiere un jardín. Pero un chorro puntual se escurre entero por la boca sin mojar nada, y ni
siquiera el caudal alto acerca el sustrato al mínimo de germinación.

**Conclusión honesta: el desagüe está y es correcto, y H4 sigue abierto.** Lo que falta no es
geometría sino el régimen de riego real —el alambique montado y a su caudal—, y eso se mide
jugando. Va a H7.

---

## Lo que hay que corregir de R135

Dos afirmaciones de aquel benchmark quedan desmentidas por estas medidas:

1. **«El hogar no puede encender el carbón (ignición 200 > 170)».** Falso: lo enciende por el
   12 % de `TryIgnite`. La cadena hogar → yesca → carbón es real, pero la sostiene la solera de
   ceniza que separa el piloto del combustible, no el umbral de temperatura.
2. **«La razón calor/reserva mide cuánto ardió sin respirar».** Solo mientras no haya carbón en
   juego: cuando el carbón vuelve a arder, mezcla su calor (22/u) con el del combustible
   original (14/u) y la razón deja de ser interpretable. Por eso el panel muestra ahora
   `LabCalorCarbon` aparte.

## Defaults nuevos de R136 (96 parámetros)

| parámetro | valor | por qué |
|---|---:|---|
| `fuego.rendimientoCarbonPct` | **25** (nuevo) | lo que impide que carbonizar cree energía (C2) |
| `fuego.vidaHumo` | 400 → **255** | era un byte y el exceso se recortaba en silencio (C4) |
| `Carbon.combustReserva` | 160 → **50** | 25 % × 50 × 22 = 275 ≈ media fibra (C2) |


---

*(R139) Este benchmark fue revisado por Fable con cinco lectores independientes: 28 hallazgos
confirmados. Las correcciones marcadas arriba vienen de ahí; el resto de la lista se aplicó en
`2026-09-04_r139_hf5b_honestidad.md`.*
