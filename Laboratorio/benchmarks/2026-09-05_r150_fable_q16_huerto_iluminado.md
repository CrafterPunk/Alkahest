# R150 — FABLE: Q16 EN BANCO, EL HUERTO CON LUZ (sin tocar el nivel de referencia)

Banco headless por `Unity_RunCommand`, sin Play. Nivel de referencia **montado y luego alterado
solo en memoria**: boca de cielo ensanchada de x118-124 a **x100-124** (aire en y273-286 y
`LuzCielo` contiguo), serpentín de 18 celdas de núcleo frío en y272 **x118-135** (sobre el lecho
oeste, fuera de la vertical de la boca nueva), caldera de siete celdas repuesta cada 8 ticks
sobre el hogar (la de r141/r148). 13 500 ticks (7,5 min de mundo). «Cara» = la celda de sedimento
más alta de cada columna con aire o planta encima.

| min | plantas vivas (celdas) | nacidas / muertas | anegadas | caras con luz ≥ 40 | luz media de la cara | humedad media de la cara | goteos |
|---:|---:|---|---:|---:|---:|---:|---:|
| 0,8 | 0 | 0 / 0 | 16 / 36 | 1 / 1 | 192 | 194 | 32 |
| 2,5 | 0 | 0 / 0 | 1 / 36 | **18 / 24** | 158 | 51 | 246 |
| 5,0 | 1 | **8 / 7** | 1 / 36 | 6 / 9 | 139 | 99 | 891 |
| 7,5 | 0 | **9 / 9** | 1 / 36 | 5 / 5 | 210 | 50 | 1 624 |

Contra el arco largo de R148 (boca original, serpentín tapándola): luz de la cara **0** durante 30
minutos, **2** plantas nacidas.

## Lectura

1. **La luz deja de ser el límite con geometría, no con física.** Con una boca de 25 columnas la
   cara del lecho recibe 139-210 de luz (el mínimo para germinar es 40) y germinan **9 plantas
   contra 2**. Es la confirmación de la causa que aisló R148 y de que la solución es de nivel.
2. **Y aparece el segundo límite: la humedad de la cara.** Las nueve plantas mueren igual. La
   humedad media de la cara oscila entre 50 y 99 con el mínimo de las plantas en 60: el goteo de un
   serpentín moja las columnas que tiene debajo, se infiltra y se seca por la cara; no moja un
   lecho. Con la luz resuelta, lo que decide el huerto es **cómo se reparte el riego**, y eso también
   es geometría (dónde cae el rocío, cuántos serpentines, cuánta caldera), no una regla.
3. El recuento de «caras» baja con el tiempo (24 → 5): parte de la cara del lecho deja de ser
   sedimento (se compacta, se cubre o se moja). No lo persigo: es el mismo régimen de nivel que
   decide el punto 2, y pertenece a la fase siguiente.

## Decisión (R26)

H4 **no se cumple en el nivel de referencia**, y la causa está aislada y es doble, en este orden:
luz (una sola boca de 7 columnas sobre 73 de lecho) y, resuelta la luz, reparto del riego. Las dos
son geometría del nivel. El nivel de referencia **no se toca**: es el que llevan diez rondas de
medidas y sus hashes; el diseño del huerto que vive (bocas, serpentines, caldera) pasa a la fase
comercial con estas dos cifras como punto de partida. Lo que el laboratorio sí deja demostrado es
la **mecánica**: la germinación responde a la luz (×4,5 con el mismo riego) y la planta vive o
muere por la humedad de su raíz, como estaba diseñado en H4.
