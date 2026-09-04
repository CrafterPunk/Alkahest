# R141 — FABLE: VERIFICACIÓN DE HF5b Y R140, Y Q12 CON EL RIEGO REAL

Banco headless por `Unity_RunCommand`, sin Play, sobre el assembly de la R140 (commits 44c38ba y
2139765). Geometrías propias.

## 1. Nada sin nombre (R140), auditado por id

`LabMateriales.Nombre(m)` para los 80 ids: **0 genéricos («material N»), 0 iguales al `devName`**,
y `Estado()` no lanza con (200,100), (0,0) ni (255,255) en ningún id. Los quince del laboratorio
(65-79) salen en castellano; los innominados de la campaña (aceite, limo, azoth, semilla de
cristal, cristal, ácido) salen con nombre **solo aquí**, como decidió Opus (regla 13/23 intacta
fuera del laboratorio). Conforme.

## 2. Q12 con el riego REAL, no con lechos anegados a mano

Opus midió el conducto contra un lecho saturado artificialmente (humedad 30 y exudado 71 con y sin
conducto: «correcto, inocuo y decorativo»). Esa medida no reproduce el régimen que motivó el
desagüe (el alambique de R135: 2 286 goteos en 10 min). Lo monté: nivel real, serpentín de 31
celdas de `NucleoFrio` en el techo de la cámara alta sobre el lecho oeste (x105-135, y272) y
caldera repuesta sobre el hogar (siete celdas de agua en y180 cada 8 ticks). 9 000 ticks (300 s).
«Anegada» = columna del lecho oeste (x100-135) con al menos una celda de agua líquida encima
(y250-256).

| t | | CON conducto (R139) | SIN conducto (piso de R135) |
|---:|---|---:|---:|
| 1 500 | columnas anegadas | 8 / 36 | 10 / 36 |
| 4 500 | columnas anegadas | **26 / 36** | **7 / 36** |
| 9 000 | columnas anegadas | **17 / 36** | **5 / 36** |
| 9 000 | humedad media del lecho | 157 | 102 |
| 9 000 | fondo del conducto (136,245) / (135,245) | 65 / 244 | — |
| 9 000 | goteos / condensado / exudado | 842 / 534 311 / 94 | 902 / 575 500 / 86 |

El riego es el mismo (goteos y condensado equivalentes; el manantial de la caldera idéntico:
7 875 reposiciones). **Con el conducto el lecho se encharca más y retiene más agua**: la grava
(permeabilidad 90) se llena hasta 244 y no suelta nada apreciable por la boca (exudado 94 contra
86 sin conducto), así que actúa de esponja sin salida dentro del lecho, y lo que no cabe se queda
en superficie. El desagüe no es inocuo: empeora lo que pretendía arreglar.

Y el dato bueno: **sin conducto, con este caudal, 5 de 36 columnas anegadas a los 300 s**, dentro
del «≤ 8 de 48» de la aceptación de H4 (R11). El régimen de riego que decide el huerto es el de la
caldera, como decía R11; con el hogar topado a 170 (C1) hierve menos que en R135 y el lecho ya no
se ahoga. La medida final es de H7, jugando.

## 3. Lo que doy por bueno de R139 sin repetirlo

Las seis aceptaciones de R15 (las medí en R138 con otra geometría y coinciden: fibra sobre arena
605 contra 651), la B-F3 al 25 % con identidad +2,6 %, el residuo 0 del agua (mi corrida de Q12 lo
confirma de paso: conservación intacta con el alambique encendido) y el coste 1,90 ms/tick.
