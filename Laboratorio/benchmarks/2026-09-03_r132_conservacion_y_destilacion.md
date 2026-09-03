# R132 — CONSERVACIÓN EXACTA Y EL CICLO DEL AGUA (H3.1)

Banco headless (`Unity_RunCommand`, sin Play, ~530 ticks/s) + verificación jugando.
Defaults de `LabParams` salvo donde se diga. PC de Cesar, 6000.5.7f1.

## 1. El residuo de conservación: de 632 a CERO

Fable diagnosticó (R2 del buzón) que el residuo de la R131 no estaba en `SimStepper.cs` sino
en `LabAgua`: `vol` es una variable **local** y `hum[i]` solo se escribe al final del método,
así que el auditor de `LabTransformar` leía el volumen **viejo** y apuntaba como DESTRUIDO lo
que en realidad se había TRANSFERIDO al aire o al poroso en esa misma visita. Correcto.

Su parche cubría las cuatro salidas por `vol <= 0`. Aplicado, el residuo bajó de **632 a 144**
— no a cero. La medida dijo dónde estaba el resto:

| escenario | residuo | correlación |
|---|---:|---|
| laboratorio, t=18 000 | 144 | ni exudación ni presión ni erosión |
| canal de prueba, 2 000 ticks | 87 | `ero`=87, `dep`=87 (1:1 con ambos) |
| canal, erosión sí / depósito no | **0** | descarta la erosión |
| canal, tick a tick | — | `Δresiduo == Δdepósito` en cada tick, siempre |

Es la **quinta salida** de `LabAgua`, que el parche no listaba: el DEPÓSITO
(`LabTransformar(i, Sedimento, vol, 0)`) también sale del método con `vol` sin sincronizar, y
ocurre *después* de evaporar e infiltrar. El residuo por depósito es exactamente
(evaporado + infiltrado) de esa visita: 1 u con los valores de fábrica.

`hum[i] = (byte)vol;` antes de transformar. Resultado:

| t | residuo (laboratorio) | residuo (canal) |
|---:|---:|---:|
| 3 000 | **0** | — |
| 9 000 | **0** | — |
| 18 000 | **0** | **0** (2 000 ticks) |

**Σ humedad(t) − Σ humedad(0) == `LabBalanceU`, al bit.** El invariante 3 del HANDOFF deja de
ser una aspiración y pasa a ser una aserción comprobable en cualquier escenario.

Regla que queda escrita en el código: *toda salida anticipada de `LabAgua` sincroniza
`hum[i] = vol` antes de transformar.* Son cinco.

Otros dos puntos del mismo diagnóstico, también aplicados: el **abono de ceniza** solo cede lo
que CABE en el sustrato (antes apuntaba la transferencia entera aunque el destino la recortara
a 255), y la **cocción** manda al aire el agua que le queda a la arcilla antes de volverse
terracota (un horno de cerámica humedece el cuarto) en vez de destruirla en silencio.

## 2. H3.1 — el ciclo del agua: hervir, subir, llover

### La hipótesis de Fable, medida y descartada

Fable propuso (R4.2) que el vapor moría de vejez a mitad de chimenea (`vidaVapor` 60 ticks vs
~65 celdas de chimenea) y que subiéndolo a 150-200 llegaría arriba. Medido:

| configuración | vapor vivo | maxY alcanzada | condensaciones de gas |
|---|---:|---:|---:|
| defaults (vida 60) | **0** | **−1** | 229 |
| vidaVapor 200 | **0** | **−1** | 275 |
| vida 200 + ascenso 20 + difusión 2 | **0** | **−1** | 248 |

**Ni una sola celda de vapor viva, nunca, en ninguna configuración.** No moría de vejez: moría
en el mismo tick. `Steam.condensesAt` estaba fijado a **60 °C** dentro de
`AplicarOverridesLaboratorio` — por encima de los 20 °C de la cueva —, así que cada celda de
vapor se volvía agua en cuanto salía de la zona caliente del hogar, a dos celdas de la brasa.
Subir la vida no podía arreglarlo: el vapor no llegaba a viejo.

Ese número estaba **escrito a mano**, contra el invariante 5 del propio HANDOFF ("todo número
físico vive en `LabParams`"). Promovido a `vapor.condensaC` y aplicado por `ReaplicarVapor`,
que ya era el camino de `vidaVapor`.

### El barrido

| configuración | vapor vivo (máx) | maxY | agua arriba | sedimento seco de la cámara alta |
|---|---:|---:|---:|---:|
| rocío 60 °C (el de antes) | 0 | −1 | 0 | 0,0 |
| rocío 0 °C, vida 200 | 94 | **286** | 0 | 0,0 |
| rocío 10 °C, vida 180 | — | — | 1ª gota en t≈450 | 1,3 |
| rocío 14 °C, vida 180 | — | — | 1ª gota en t≈420 | 4,1 |
| **rocío 10 °C, vida 180, ascenso 12** | 106 | 286 | **21 celdas** | **17-22** |

Con el rocío por debajo del ambiente de la cueva (20 °C) pero por encima del de la cámara alta
(8 °C), el vapor **puede viajar** los 65 celdas de chimenea y **condensa exactamente donde hace
frío**. Eso es la destilación en columna que el diseño pedía, y sale de la geometría del plano.

### Defaults nuevos (decisión de Opus, §7)

| parámetro | antes | ahora | por qué |
|---|---:|---:|---|
| `vapor.condensaC` | 60 (a mano, no era parámetro) | **10** | por debajo del ambiente, por encima de la cámara fría |
| `vapor.vidaVapor` | 60 | **180** | la chimenea mide ~65 celdas |
| `vapor.ascenso` | 6 | **12** | con 6 la humedad se queda en la galería |

### Aceptación H3.1

> «goteo en la cámara alta en < 3 min de mundo con agua hirviendo en el hogar»

**Primera gota de agua líquida en la cámara alta a t≈450-1000 ticks = 15-33 s de mundo.** El
sedimento seco del piso pasa de humedad 0 a 17-22. Verificado headless y jugando (tiempo
controlado con `Paused` + `StepOnce` para que el fenómeno no se escape entre capturas).
Preset y snapshot: `Laboratorio/presets/ref_destilacion.{json,png,_libro.json}`.

Capturas: `R132_h3_columna_vapor.png` (la columna saliendo del agua sobre la brasa),
`R132_h3_lluvia_camara_alta.png` (gotas condensadas cayendo por la chimenea: el reflujo),
`R132_h3_camara_alta_mojada.png` (el vapor entrando en la cámara fría y el piso mojándose).

### Regresión del circuito de H1 con los defaults nuevos

Residuo 0 · poza x290 = 136 (labio 132) · sumidero 4 802 celdas a t=9 000 · **1,89 ms/tick** ·
528 ticks/s. Idéntico a la R131: el ciclo del agua nuevo no toca el circuito mientras el
jugador no encienda nada.
