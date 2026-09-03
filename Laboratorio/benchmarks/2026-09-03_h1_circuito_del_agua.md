# H1 — EL PLANO CIERRA EL CIRCUITO DEL AGUA (R131, Opus 5)

Banco **headless** (sin Play, sin cámara): `Universe.Create(777002)` +
`AplicarOverridesLaboratorio` + `CellGrid` + `BuildLaboratorioDeLeyes` + `SimStepper` con
`LabActivo`, todos los chunks despiertos en t=0. Defaults de `LabParams` salvo donde se diga.
PC de Cesar, editor 6000.5.7f1. ~530 ticks/s (1 tick = 1/30 s de mundo).

## El problema que traía la R130

La fisura x192-202 y111-134 era **arena** (Powder): se derrumbaba a la cámara profunda y
dejaba un agujero de 11 celdas en mitad del lecho. El arroyo entero se colaba por ahí y el
sumidero **no recibía una gota** (`LabAguaSumida = 0` a t=3565). La grieta x336-343, abierta,
era un segundo pozo de 8 celdas justo después del labio de la poza: aunque se tapara la
fisura, todo lo que rebosaba caía por ella y el sumidero seguía seco.

## Qué se cambió

| | antes | ahora |
|---|---|---|
| fisura x192-202 y111-134 | `Sand` (Powder, se cae) | **`Arenisca`** (id 78, StaticSolid, `caeSolido=false`, perm 30) |
| grieta x336-343 y111-132 | aire (pozo abierto) | **repisa de `Arenisca` en y111 + `Grava` y112-132** (grieta atascada de escombro) |

`Arenisca` = roca **porosa**: no cae, el agua la atraviesa despacio y sale **limpia** por el
otro lado (los finos se quedan dentro, colmatándola). El cincel la desprende como `Sand`.

## Medidas

Serie con defaults (una corrida, determinista):

| t | agua (celdas) | nivel poza x290 (labio 132) | nivel x400 | cámara profunda | **sumida** (celdas) | **exudado** | descuadre |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 0 | 395 | 128 | — | 0 | 0 | 0 | 0 (0,000 %) |
| 3 000 | 1 299 | 136 | 131 | 17 | **759** | 4 | 426 (0,081 %) |
| 9 000 | 924 | 136 | 131 | 15 | **4 802** | 72 | 616 (0,113 %) |
| 18 000 | — | 136 | 131 | 15 | **10 949** | 160 | 632 (0,113 %) |

Libro mayor a t=18 000: emitida 12 095 · sumida 10 949 · evaporado 63 271 u · condensado **0**
(H3) · goteos 0 · infiltrado 148 939 u · exudado 160 · depositado 21 256 · erosionado 20 283 ·
compactado 19 · presión 11 084 mudanzas. **2,08 ms/tick** de media, 480 ticks/s.

**Régimen permanente desde t≈3 000**: los niveles no se mueven entre t=3 000 y t=18 000, y el
sumidero traga ~0,68 celdas/tick = **20 celdas/s**, que es exactamente el caudal que emite el
manantial (24 pedidas, 20,4 entregadas por las celdas con cara libre). El arroyo entrega
prácticamente todo su caudal al sumidero; la cámara profunda se queda con un charco **estable
de 15 celdas** alimentado por el hilo que se filtra.

**La arena no se mueve**: 95 celdas de `Sand` en t=0 y en t=18 000 (el montículo de la sala).
Antes la fisura entera se derrumbaba.

## Aceptación H1

- `LabAguaSumida > 0` → **10 949 celdas** ✔
- poza llena hasta su labio → nivel **136**, labio 132: rebosa y corre aguas abajo ✔
- la cámara profunda recibe goteo por la grieta y exudación por la arenisca (`LabExudado > 0`)
  → **160 exudaciones**, charco estable de 15 celdas ✔
- balance del libro ±5 % → **0,113 %**, y deja de crecer entre t=9 000 y t=18 000 ✔

## Tres correcciones de conservación encontradas midiendo

El balance de partida se desviaba **+10,4 %** a t=9 000. No era una sola causa:

1. **El depósito creaba agua.** `LabAgua` escribía `LabTransformar(i, Sedimento, 255, 0)` con
   255 fijo aunque la celda de agua tuviera menos volumen — el propio comentario ya decía que
   "el agua que había queda como humedad del sedimento". Ahora pasa `vol`.
2. **La presión secaba el aire.** `LabPresion` mudaba una celda de agua sobre la superficie
   más baja y **aniquilaba el vapor** que ocupaba ese hueco: −4,5 u por mudanza, −20 006 u en
   9 000 ticks (el 85 % del descuadre). Ahora el aire del destino se muda al hueco que deja
   el agua: es un intercambio, como el de cualquier celda que se mueve.
3. **La auditoría estaba incompleta** (esto no era un bug de la sim): la exudación y el goteo
   vacían el poro sin pasar por `LabTransformar`, así que el balance perdía 255 u por evento.

Para no volver a discutir esto de oído, el stepper lleva ahora **`LabBalanceU`**: la suma de
todo lo que crea o destruye de `humedad[]`, contada en los tres únicos sitios que escriben sin
restar en otro lado. El invariante 3 del HANDOFF se comprueba con una resta:

    Σ humedad(t) − Σ humedad(0)  ==  LabBalanceU

El residuo (632 u en 18 000 ticks, 0,113 %) viene del barrido ordinario de `SimStepper.cs`,
que no toco (HANDOFF §2). Está anotado en `PREGUNTAS_A_FABLE.md`.

También se añadió `LabAguaSumidaU` (unidades, no celdas): el sumidero traga celdas a medio
llenar y contar "255 × celdas" sobreestimaba el caudal real un 4 %.

## Capturas (`Laboratorio/capturas/`)

Retrato directo de la grilla (color base del material + turbidez del agua + mojado de los
porosos), sin cámara ni Play, así que el banco headless las produce igual:

- `R131_h1_plano_t0.png`, `_t3000.png`, `_t9000.png` — el plano entero (x28-435, y45-287).
- `R131_h1_fisura_t3000.png`, `_t9000.png` — el frente mojado bajando por la arenisca.
- `R131_h1_grieta_t9000.png` — la grieta atascada de grava sobre su repisa.
- `R131_h1_sumidero_t9000.png` — el pozo del sumidero tragando.
