# R139 — HF5b: LA HONESTIDAD DEL FUEGO

Los cinco bloques de la revisión adversaria de Fable (R15-R17, 28 hallazgos confirmados). Banco
headless, sin Play. Nada de esto reabre la física salvo una línea de ignición que Fable decidió.

---

## A · R15 · El hogar calienta, no chispea

Fuera las cuatro llamadas a `TryIgnite` de `LabHogar` — la única línea por la que el hogar
encendía con azar en vez de con temperatura. Y `WakeChunk` en `LabCalentarHasta` cuando la
temperatura cambia. **(Precisado en R145: está bien tenerlo, pero era REDUNDANTE — `LabHogar` ya
despierta su 3×3 —, así que no fue la causa de que nada funcionara. Se queda por robustez, no
porque arreglara un fallo.)**

| caso | criterio | medido |
|---|---|---|
| fibra seca pegada al hogar | prende en ≤ 300 ticks | **t=48** ✔ |
| fibra sobre una celda de arena | prende (Fable midió 605) | **t=651** ✔ |
| fibra mojada pegada | NO prende en 3 000 ticks | **intactas mientras conservan el agua** ✔ |
| carbón pegado | NO prende en 3 000 ticks | **6/6 intactas, Tmax 170** ✔ |
| HF2 con yesca | sigue vidriando | **18 de 18**, igual que antes ✔ |
| tolva | ≥ 300 s | **466 s**, igual que antes ✔ |

**La cadena hogar → yesca → llama → carbón pasa de falsa a verdadera por dos números** (fibra 130
≤ 170 < carbón 200), que era el mecanismo bueno desde el principio. La solera de ceniza del horno
sigue siendo la segunda barrera.

Matiz honesto sobre la fibra mojada: de las 6 celdas, 3 se secan por gravedad (el agua percola al
fondo del banco) y esas sí prenden; las 3 que conservan `humedad` 255 siguen intactas a los 3 000
ticks. O sea que la regla es «lo mojado no prende», no «lo que estuvo mojado no prende» — que es
lo correcto.

## B · Los dos libros, porque uno solo mentía

El «LIBRO DE ENERGÍA» sumaba tres convenciones distintas en un TOTAL que no era total de nada.
Ahora son dos libros separados.

**Libro del combustible (nominal — es lo que se conserva).** Añadidos `LabCombustibleCarbon`,
`LabUnidadesRespiradas` y `LabCalorNoSoltado` (R16). La razón del panel pasa a ser
`(LabCalorFuego − LabCalorCarbon) / (LabCombustibleQuemado − LabCombustibleCarbon)` y su ayuda
dice «solo del combustible original»: antes mezclaba los 22 del carbón con los 14 de la fibra y
el «8,3 raw/u» de la tolva no significaba nada. Medido ahora en la tolva: **7,3 raw/u de fibra
sola** y **3 % de unidades respiradas** — que dice lo mismo que quería decir aquel 8,3, pero de
verdad.

**Libro del calor entregado (raw escritos en la grilla).** Helper `LabInyectar(x, y, cuanto)`:
hace los mismos cuatro `AddTemp` y devuelve la suma de deltas reales, así que la simulación no
cambia. Con él, `LabRawFuego`, `LabRawLlama` (los 40 a los vecinos **y** el delta del pin a 255),
`LabRawBrasa` (**no existía**, y la brasa emite más calor nominal que la combustión que sí se
contaba), `LabRawHogar` y `LabRawFrio` (negativo). Solo este libro tiene TOTAL.

### Y la medida invierte lo que los dos habíamos escrito

| escena | combustión | LLAMA | brasa | llama NOMINAL |
|---|---:|---:|---:|---:|
| hoguera abierta (400 fibra) | 184 351 (51 %) | 105 579 (**29 %**) | 60 834 (17 %) | 622 040 |
| carbonera sellada, boca 1 | 183 801 (67 %) | 17 998 (**6 %**) | 65 068 (23 %) | 271 280 |
| horno de HF2 | 35 901 (48 %) | 5 079 (**7 %**) | 22 051 (29 %) | — |

Yo escribí «la llama es el 90 % del calor»; Fable lo corrigió a «¾ (130 240 / 45 400)». **Las dos
cifras son del libro nominal, y las dos están mal.** En raw entregados la llama pone entre el 6 %
y el 29 %, y la fuente que más escribe es siempre la **combustión**.

La causa está en una línea: en la hoguera la llama suelta **622 040 nominales y entrega 105 579; sobre lo que intenta (≈ 2 488 160) es un 4 %**, porque lo que ya está a 255 no admite más. Cuanto más caliente el sitio —o sea, cuanto
más se parece a un horno—, **menos** entrega la llama. Por eso el sellado da 6 % y el abierto 29 %.

Esto es exactamente lo que el hallazgo B de R17 predecía sin saber la cifra: el libro nominal no
se podía sumar, y sumarlo daba conclusiones invertidas.

## C · El desagüe: los dos bugs corregidos, y el veredicto honesto

Aplicada la geometría de R17: **labio entero de roca** otra vez (x136 y x153, y246-249) y el
conducto **atravesando la solera** (`Bloque(134, 245, 136, 245, Grava)` y simétrico).

| | antes (R137) | ahora |
|---|---|---|
| grava en su sitio a los 300 s | se derrumbaba a la boca | **11/11** ✔ |
| solera bajo el lecho | el conducto ciego la ablandaba | **arcilla 34/34** ✔ |
| conservación del agua | residuo 0 | **residuo 0** ✔ |

Los dos bugs graves están cerrados. **Pero el desagüe no drena nada medible**, y esto es lo que
hay que decir con claridad:

| lecho anegado a 255, a los 300 s | humedad media | exudado |
|---|---:|---:|
| **con** conducto | 30 | 71 |
| **sin** conducto | 30 | 71 |

Idénticos. El lecho se vacía solo —por evaporación y reparto al aire de la cámara— mucho más
deprisa de lo que el conducto puede conducir. Se ve al conducto trabajar (el fondo de (136,245)
sube a 19 y luego a 45 mientras el lecho baja de 255 a 149) pero **nunca llega a 255**, así que
no exuda agua líquida a la boca: la aceptación de R17 no se cumple. Y con un rocío de 10 celdas/s repartido por el techo del lecho oeste (montaje que R145 sustituye por el alambique de r141 §2, que es el que reproduce el régimen real)
durante 300 s el conducto se queda en humedad 12, porque a ese caudal el lecho ni se moja.

**Conclusión: el conducto es correcto, inocuo y decorativo.** Escalado como **Q12**.

## D · Los textos que el código desmentía

Corregidos en el benchmark de R137 y en los docblocks:

1. **La tolva.** «466 s porque el hogar ya no sobrecalienta la base y arde más ahogada» — el
   consumo no depende de la temperatura. Queda el 466 s como medida, sin la causa inventada.
2. **El carbón agotado no va a ceniza, va a brasa** (bancada ×4 si está tapada) y de ahí a ceniza.
   La tabla de C2 tenía las celdas de brasa sin nombrar.
3. **La identidad da 555 raw por celda ahogada, no 560** (40×7 + 0,25×50×22), y es
   **estadística**: la carbonización se decide con la sordina del último paso, sin memoria por
   celda. Cuadra a ±1 % con n ≥ 900 y baila con n pequeña — el +5,5 % de la 20×20 no era solo
   ruido del sorteo, eran celdas que respiraron antes de acabar tapadas.
4. **El comentario de `Grava`** («grueso: no desliza de lado») era una promesa sin línea que la
   cumpliera: `ProcessPowder` prueba la diagonal sin mirar `fluidity`, que solo leen los líquidos.
   Reescrito con lo que hace de verdad y con el aviso para colocarla en un nivel.

### B-F3 repetida tras R15

| | R137 | **R139** |
|---|---:|---:|
| pico de carbón (20×20, boca 1) | 25,0 % | **25,0 %** (t=3 250) |
| carbonizadas | 25,5 % | 25,5 % |
| identidad | +5,5 % | **+2,6 %** |

Mejora porque el hogar ya no enciende por azar. Y el raw con nombre que pedía R16: de 162 500 no
soltados, **112 200 volvieron como carbón y 50 300 se perdieron** como gas sin quemar.

## Persistencia

- `_libro.json` guarda ahora los catorce contadores del fuego más el vidrio: un snapshot podía
  guardarse sin la identidad de la carbonera ni el calor entregado.
- `EscribirDefaultsSiFalta` reescribe cuando el registro tiene más claves que el archivo. Antes
  solo miraba si existía, así que un `_defaults.json` viejo seguía diciendo `vidaHumo` 400 y no
  conocía `fuego.rendimientoCarbonPct`. El archivo que dice cuáles son los valores de fábrica no
  puede ser el único que no se entera de que la fábrica cambió.

## Coste y regresión

| | ms/tick |
|---|---:|
| laboratorio en reposo | 1,90 |
| incendio de 5 000 celdas | 1,91 |

Sin regresión (R135: 1,90/1,95 · R137: 1,85/1,80). Regresión del agua: **residuo 0** a los 50,
150, 300 y 600 s con el nivel nuevo.

## El tamaño real de la costura (para el HANDOFF §2)

Líneas de `SimStepper.cs` que tocan el laboratorio, contadas:

**(Corregido en R145.)** Esta tabla contaba «líneas que MENCIONAN algo del laboratorio», que no
es lo que se preguntaba. Contadas como líneas AÑADIDAS con `git diff 371dea4` (el último commit
sin laboratorio), comentarios incluidos, salen **+49 en `ProcessCombustion`, +13 en `ProcessBrasa`
y +7 en `ProcessFire`**, más 70 fuera de esos tres. La convención vive ahora en el HANDOFF §2.

| método | líneas del método | menciones (la cuenta vieja) |
|---|---|---:|
| `ProcessCombustion` | 829-937 | 18 (16 de código, 2 de comentario) |
| `ProcessBrasa` | 1003-1055 | 4 |
| `ProcessFire` | 1643-1727 | 3 |

Más seis líneas sueltas de R130/R132 (erosión al mover agua, condensado del gas, fibra mojada).
`TryIgnite`, `AddTemp` e `InjectHeat` siguen intactos. Todo bajo `LabActivo`: fuera del
laboratorio el diff es inerte.

## Estado

96 parámetros. Las aceptaciones de R15 se cumplen las seis; la de C (exudar a la boca) no, con la
causa medida y escalada como Q12. Nada más queda abierto de la lista de HF5b.
