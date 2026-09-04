# R136 — FABLE: EL TIRO QUE NO EXISTE Y EL HOGAR QUE NO ERA DOMÉSTICO

Banco headless por `Unity_RunCommand` (sin Play, editor de Cesar, build R135, sin tocar un
`.cs`). Caja de piedra de 20×12 interior (x300-319, y100-111) con suelo y techo de piedra; la
«chimenea» es un hueco de N celdas centrado en el techo. Fuego: dos celdas de `Hogar` en el
centro de la fila baja, yesca de fibra en el resto de esa fila y en la siguiente, carbón encima.
Defaults de `LabParams`. 530-560 ticks/s. Las reglas emuladas se aplican DESPUÉS de cada
`Step()` sobre la grilla, así que aproximan: la versión real vive dentro de la pasada.

---

## 1. La curva boca→temperatura de Q9, reproducida (pila maciza, 60 carbón, 9 000 ticks)

| chimenea | humo del carbón | Tfuel media 2000-9000 | humo dentro (máx) | quemado t=9000 | razón |
|---|---:|---:|---:|---:|---:|
| 0 | 4 % | 249,8 | 0 | 17 200 | 12,69 |
| 0 | **40 %** | 249,8 | 0 | 17 200 | 12,69 |

**Idéntico al bit.** Subir el humo del carbón de 4 a 40 no cambia una sola celda: el humo no
llega a nacer. (Las corridas con chimenea 1-8 se cortaron por el timeout del puente; la §3
las repite con menos ticks.)

## 2. Por qué no nace: la llama es inmortal y tapa la salida (micro-banco, 2 500 ticks)

| caso | humo máx | humo medio | llamas máx | quemado | razón |
|---|---:|---:|---:|---:|---:|
| carbón 4 %, caja sellada | 20 | 1,35 | **20** | 7 513 | 14,88 |
| carbón 40 %, caja sellada | 25 | 5,46 | **20** | 7 513 | 14,88 |
| carbón 40 %, al aire | 12 | 0,76 | 20 | 6 140 | 14,83 |
| fibra, al aire | 6 | 0,75 | 17 | 4 623 | 9,51 |
| fibra, caja sellada | 27 | 5,40 | 20 | 5 778 | 9,26 |

`llamas máx = 20` es exactamente la anchura de la fila superior del combustible. `ProcessFire`
mantiene viva toda llama que toque combustible (`if (life < 30 && HasFlammableOrthogonalNeighbor)
life = 30`), así que la lengua que nace sobre cada celda de la fila superior **no muere nunca**.
De ahí, en cadena:

1. F1 cuenta la llama como aire → la celda de abajo «respira» sin tener una sola celda vacía.
2. `SpawnSmokeNear` solo suelta humo en un vecino **vacío**, y el único vecino libre lo ocupa la
   llama → el humo del combustible casi nunca nace (4 % o 40 %, da igual).
3. La llama inyecta **40 raw por tick** a sus vecinos (`InjectHeat(x, y, 40)`): 15 veces el
   calor del carbón que la sostiene (22 raw cada 8 ticks = 2,75/tick). El calor del horno es
   calor de llama, no de combustible, y no consume reserva.
4. El aire nunca se gasta: una caja sellada tiene el mismo aire a los 9 000 ticks que al
   principio. Solo el humo podría desplazarlo, y el humo no nace.

Con esos cuatro hechos, la boca no tiene qué regular. Opus leyó bien el síntoma («el grueso de
la pila no respira de todos modos»); la causa está una capa más abajo.

## 3. La corrección que probé, emulada (la llama es el sensor)

Regla emulada tras cada tick: una llama sin **ningún** vecino vacío muere en humo, y una llama
con aire encima suelta humo por la punta (5 % por tick).

**Pila maciza** (60 carbón, 6 000 ticks): Tfuel 252,1 / 252,1 / 252,2 con chimenea 0 / 1 / 2;
humo medio 26,6 / 10,9 / 6,2; quemado idéntico (16 002). Plana. La pila maciza arde por dentro
en sordina haga lo que haga la boca, y el termómetro está en el tope del byte.

**Pila FINA** (20 carbón en una sola fila: todo el combustible toca el aire), a t=1500, en plena
fase activa:

| chimenea | Taire (filas 4-8) | humo dentro | llamas | quemado | llamas muertas | humo nacido |
|---:|---:|---:|---:|---:|---:|---:|
| 0 | **245** | 150 | 9 | 3 637 | 353 | 512 |
| 2 | 235 | 41 | 11 | 3 774 | 4 | 1 228 |
| 8 | 228 | 9 | 11 | 3 774 | 2 | 1 233 |

Monótona… **al revés**: la chimenea es una fuga de calor, no una entrada de aire. El ahogo
redujo el consumo un **4 %** (3 637 contra 3 774) y la bolsa de humo (150 celdas de 200 de aire)
no bajó hasta la fila del combustible: la producción se autolimita justo cuando toca las llamas.
Para que el tiro fuera una válvula de verdad harían falta, además, humo inmortal bajo techo.

Con las reglas **actuales**, la misma pila fina da Taire 242 / 242 (chimenea 0 / 8): plana, y
9 / 0 celdas de humo.

## 4. El hogar no es doméstico

| caso | T de la celda sobre el hogar | T media de la caja |
|---|---:|---:|
| hogar solo, caja sellada, t=9 000 | **255** | 156 |
| hogar solo, al aire, t=3 000 | **255** | 149 |

`LabHogar` fija su celda a `fuego.hogarRaw` (170) pero `InjectHeat(fuego.hogarCalor = 40)`
empuja a los cuatro vecinos **sin tope**: se saturan a 255 raw (390 °C). El «hogar a 170» es
verdad solo en su propia celda.

Consecuencia medida (hogar de 4 celdas, arena encima con ceniza a los lados, paredes de piedra
para que nada resbale):

| t | T arena | reposo | material | vidrio |
|---:|---:|---:|---|---:|
| 300 | 255 / 255 | 34 / 34 | arena | 0 |
| 800 | 255 / 255 | — | **VidrioVerde** | **2** |

**Arena sobre el hogar + ceniza = vidrio a los 800 ticks (27 s), sin horno.** La frontera
doméstico/industrial de F4/F5 no existe en la build R135; existe en el texto. (La medida de
Opus «el hogar suelto no vidria» tenía que ser sin ceniza pegada o sin arena tocando el hogar.)

Emulando el tope a 170 después de cada tick la arena vidrió igual, porque la inyección ocurre
dentro de la pasada y la arena cuenta su visita a 210. El tope tiene que vivir **en `LabHogar`**.

## 5. Aritmética que no cuadra (leída de las defs, sin banco)

- **Carbonizar crea energía.** Fibra: 40 u × 14 raw = **560** raw por celda. Carbón: 160 u ×
  22 raw = **3 520** raw por celda. ×6,3 — y B-F3 midió 100 % de rendimiento en celdas con boca
  1. La carbonera es un generador, no un almacén.
- **El libro de energía cuenta la fuente pequeña.** `LabCalorFuego` suma solo el `calorPaso`
  del combustible. No cuenta la llama (40 raw/tick por celda: la fuente dominante, §2.3) ni el
  hogar (40 raw/visita) ni el frío.
- **`fuego.vidaHumo` = 400 es 255.** `gasLifetime` es un byte y `ReaplicarVapor` lo recorta a
  255 (510 bajo techo, por el decaimiento a mitad). El registro lo declara con tope 255 y
  default 400.

## 6. Qué queda en pie de lo medido por Opus

Todo lo que no depende del hogar ni del humo: la tolva (324 s, 0 intervenciones, fibra al
aire), la carbonera como geometría de contacto (boca 1 → 0 ceniza), la pila maciza como su
propia carbonera, el coste (+2,6 %), la regresión del agua. El «horno a 386 °C» hay que
repetirlo con el hogar topado: mis cajas selladas con carbón ardiendo llegan a 240-252 raw sin
ayuda del hogar, así que debería seguir vidriando — pero es una medida, no una deducción.
