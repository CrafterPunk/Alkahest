# R138 — FABLE: VERIFICACIÓN INDEPENDIENTE DE HF5 (build R137, commit d711454)

Banco headless por `Unity_RunCommand`, sin Play, sobre el assembly compilado por Opus
(`fuego.rendimientoCarbonPct` = 25, `fuego.vidaHumo` = 255, `Carbon.combustReserva` = 50
leídos en vivo). Geometrías propias, no las de Opus: si los números coinciden, coinciden por
la física y no por el montaje.

## 1. La frontera doméstica, con combustible encima del hogar

Plataforma: hogar de 4 celdas, paredes de piedra a los lados (retienen el polvo), encima
`[ceniza][arena][arena][ceniza]`, y sobre eso 0, 1 o 3 filas de combustible. 3 000 ticks.

| carga | primer fuego (tick) | T máx de la arena | vidrio | libro (llama / hogar / brasa) |
|---|---:|---:|---:|---|
| nada | — | 170 | **0** | 0 / 5 948 / 0 |
| 1 fila de fibra (4 celdas) | 605 | 255 | **2** | 53 000 / 5 348 / 2 240 |
| 3 filas de fibra (12) | 713 | 255 | **2** | 52 520 / 4 579 / 5 998 |
| 3 filas de carbón (12), sin tocar el hogar | — | 170 | 0 | 0 / 6 073 / 0 |

Lecturas: (1) C1 funciona: el hogar solo no pasa de 170 y no vidria. (2) **Cuatro celdas de
fibra sobre el hogar bastan para vidriar las dos de arena**: la llama (255, inmortal mientras
toque combustible) más la brasa que deja suman más de las 60 visitas. No es el hogar, es la
llama; y sale a 1-2 celdas pegadas a la llama, no a una carga. (3) La fibra prendió **sin tocar
el hogar** (había arena y ceniza en medio) por temperatura, a los 605 ticks: la autoignición de
`ApplyPhase` funciona a través de una celda a 170. (4) El carbón que no toca el hogar no prende
en 3 000 ticks: 170 < 200. El que sí lo toca prende por el 12 % de `TryIgnite` (Q10 de Opus).

## 2. Carbonera de 400 celdas, boca 1: determinismo e identidad

Recinto de piedra 20×20 de fibra, piloto (hogar) en la esquina interior, boca de una celda en el
techo. Dos corridas idénticas de 3 500 ticks, hash de `mat`/`temp`/`aux` de toda la caja:

| | corrida A | corrida B |
|---|---|---|
| hash | 8663622888110843897 | **idéntico** |
| pico de carbón | 85 celdas (21,3 %) a t=3 456 | idéntico |
| carbonizadas / quemado | 91 / 17 957 u | idéntico |
| identidad `(calorFuego − calorCarbon) + energíaCarbon` | 211 197 vs 223 440 ideal | −5,5 % |

La corrida no había terminado (31 celdas de fibra sin arder = 17 360 raw todavía en la pila):
descontándolas el ideal es 206 080 y la identidad da **+2,5 %**. Dentro del ±5 % pedido, y
determinista al bit con la sal 632.

## 3. Regresión del agua en el nivel real

`BuildLaboratorioDeLeyes`, 3 000 ticks: Σhumedad − Σhumedad(0) − `LabBalanceU` = **0** a
t=1 500 y t=3 000 (agua 1 322 celdas, emitida 2 018, sumida 748). Sin regresión.

## 4. El desagüe de grava (Q8/R11) se mueve

A t=3 000, de las 8 celdas de grava de cada conducto quedan **5 en su sitio** (oeste y este); el
labio de grava (x136/x153, y246-247) sigue entero; y hay grava en la **boca** de la chimenea:
`mat(137,246)` = Grava y `mat(137,245)` = Grava (la solera de arcilla en x136,y245 sigue). Es
decir, la grava del conducto resbala hacia la boca. Cuánto drena después de eso, y a dónde va
el resto, es de Opus (R17). No afecta a la conservación (residuo 0).

## 5. Lo que NO he podido reproducir aquí y doy por bueno de Opus

HF2 con yesca (18/18), la tolva (466 s) y el coste (1,80-1,85 ms/tick): geometrías largas. La
identidad y el 25 % los he visto salir con otra geometría, así que sus números son creíbles.
