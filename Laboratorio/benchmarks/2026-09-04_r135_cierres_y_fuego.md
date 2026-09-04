# R135 — CIERRES DE Q6/Q7 Y EL DOMINIO DEL FUEGO (HF1-HF4)

Banco headless + verificación jugando. Defaults de `LabParams` salvo donde se diga.

---

# PARTE 1 — LOS CIERRES (R8 y R9 de Fable)

## R8 · Condensar sobre el vecino MÁS FRÍO

Aplicado: `LabAire` elige el vecino condensable de menor temperatura (empate → arriba, orden de
comparación fijo, determinista).

**Su criterio numérico no se cumple, y la medida explica por qué.** Fable pedía que
`ref_alambique` diera ≥ los 93 goteos de la R133. Con el MISMO plano (reconstruyendo el piso
viejo para aislar el cambio) salen **70**. Pero el reparto del rocío cambia por completo:

| | goteos | rocío en el SERPENTÍN | rocío en la ROCA |
|---|---:|---:|---:|
| antes (primer vecino) | 93 | — | repartido por el techo |
| ahora (vecino más frío), piso viejo | 70 | **5 926 u** | **0** |

Los goteos bajan porque el rocío **se concentra**: en vez de repartirse por todo el techo (muchas
celdas goteando poco) va entero al bloque frío (pocas celdas goteando mucho). El efecto que
Fable buscaba se cumple exactamente — *el jugador elige dónde gotea poniendo un bloque* —, y se
confirma con el caso que motivaba el cambio: un serpentín en la **pared**, con techo encima,
recibe ahora 1 933 u de rocío donde antes no recibía nada. La métrica correcta no es el número
de goteos sino **dónde** cae el rocío.

## R9 · El plano que aguanta el riego, y las raíces que sujetan el suelo

Aplicado tal cual: solera de arcilla en y245, lecho de 4 celdas de sedimento (y246-249) y labio
de roca a los lados de la boca de la chimenea. Más la regla nueva: **un sedimento con una planta
encima no se erosiona** (`LabErosion`, una línea).

**Y un rebosadero que no estaba en la especificación.** El labio a y250 —una celda por encima
del lecho— convertía la solera impermeable en una **bañera**: medido, a los 150 s **24 de las 48
columnas del claro estaban bajo agua**, y bajo el agua no germina nada. Con el labio a ras del
lecho (y249) el sobrante se va por la boca en cuanto supera la tierra.

| | sustrato del claro a 150 s | a 300 s |
|---|---:|---:|
| piso viejo (R134) | 28 % | 30 % |
| solera + lecho + labio alto | 28 % | 19 % |
| **solera + lecho + rebosadero** | **84 %** | 58 % |

**El criterio revisado de H4 sigue sin cumplirse** (0 plantas vivas a los 5 y a los 10 min), y el
diagnóstico está medido: **el mismo alambique que trae el agua ahoga el huerto.** Hay una ventana
buena al principio —a los 50 s hay **14 columnas aptas** con humedad 141 y luz 125, y germinan—
y a los 150 s el goteo las ha encharcado. Con riego suave (serpentín de 6 celdas, un frasco cada
23 s) siguen 20 columnas bajo agua. Escalado como **Q8**: 2 286 goteos en 10 minutos es caudal
industrial para un jardín, y lo que falta es **regular el riego**, no más agua.

---

# PARTE 2 — EL FUEGO (HF1-HF4)

## HF1 · El aire de contacto

`LabRespira(x, y, idx)`: al menos un vecino ortogonal de aire o llama, y como mucho uno de humo.
Quien no respira arde **en sordina**: consume a ¼ (uno de cada cuatro pasos), calienta a ½, no
saca lengua y humea a ¼. Seis costuras marcadas `(R135)` en `ProcessCombustion`/`ProcessBrasa`
(la excepción autorizada). Más `Carbon` (id 79, `Count` 80), `fuego.vidaHumo` = 400,
`luz.decayHumo` = 24 y `fuego.hogarRaw` 220 → **170** (el hogar es doméstico).

### B-F3 · La carbonera

400 celdas de fibra, recinto de roca, piloto pegado, 6 000 ticks:

| geometría | pico de carbón | ceniza | llama | humo |
|---|---:|---:|---:|---:|
| **boca 1** | **400 (100 %)** | **0** | **0** | **0** |
| boca 4 | 325 (81 %) | 76 (19 %) | sí | sí |
| boca 8 | 350 (88 %) | 50 (13 %) | sí | sí |
| pila maciza al aire | 354 (89 %) | 46 (12 %) | sí | sí |

La lectura correcta no es el carbón sino la **ceniza**: con boca 1 no arde del todo NADA (0
ceniza, 0 llama, 0 humo); al abrir la boca aparece lo que se consume entero. Y un hallazgo que
no estaba previsto: **una pila maciza es su propia carbonera** aunque esté al aire libre,
porque sus 324 celdas interiores no tienen un solo vecino de aire. Es cierto en la realidad y
sale gratis de la regla.

Segundo hallazgo, del propio banco: con el piloto a 4 celdas de distancia, **el recinto cerrado
se enciende solo por acumulación de calor y el abierto no**. El aislamiento es lo que permite
alcanzar la temperatura de ignición, y eso también sale de reglas que ya existían.

### B-F1 · Coste

Incendio de 5 000 celdas de fibra, 4 000 ticks: **1,95 ms/tick** de media (pico 10,8) contra los
1,90 del laboratorio en reposo: **+2,6 %**, muy por debajo del +10 % de aceptación. F1 cuesta dos
conteos de cuatro vecinos por PASO de combustión (cada 8 ticks), no por tick.

### B-F4 · El humo que persiste

2 400 celdas de humo bajo techo, `vidaHumo` 400: la bolsa se estabiliza en 128 celdas y **no se
disipa** (con el valor del juego, 200, caía a 0 en 30 s). Coste 1,66-2,00 ms/tick, por debajo de
los 3 ms de aceptación.

### Regresión del agua

Idéntica: residuo de conservación **0**, poza x290 = 136, sumidero 4 819 a t=9 000, 1,90 ms/tick.
Ninguno de los hitos del agua usa combustión.

## HF2 · El horno y el vidrio

F5 implementado: arena con `temp ≥ fuego.vidrioRaw` (200) y **ceniza al lado** (el fundente)
acumula `reposo`; a `fuego.vidrioVisitas` (60) se vuelve `VidrioVerde`. Si se enfría o pierde el
fundente, la cuenta se reinicia.

**El horno funciona y el hogar solo no.** Recinto de arcilla 20×12, solera de ceniza, 40 de
arena de carga, 180 de carbón, yesca de fibra sobre el piloto:

| t | T de la carga | vidrio | carbón |
|---:|---|---:|---:|
| 100 s | **253 raw = 386 °C** | 10 | 168 |
| 300 s | 200 raw = 280 °C | **29** | 13 |

7 936 ticks (264 s) por encima del umbral del vidrio. El hogar suelto llega a 170 raw: **no hay
vidrio sin recinto**, que es exactamente la frontera que pedía Cesar.

**La cadena de encendido que apareció sola**: el hogar (170 raw) **no puede encender el carbón**
(ignición 200 raw). Hace falta **llama**: hogar → yesca de fibra → llama (255 raw) → carbón. Eso
no está programado en ninguna parte; sale de dos números.

**El criterio de la curva boca→temperatura NO se cumple**: sellado 228 raw, boca 6 → 232, boca 12
→ 231, y vidrio 28/29/29. Con el carbón **macizo** la boca no regula, porque el grueso de la pila
no respira de todos modos y la combustión la lleva la propagación interna, no el aire de la boca.
Escalado como **Q9** con mi lectura: el regulador existe (B-F3 lo demuestra en la carbonera) pero
manda la geometría del combustible, no la del recinto. Medir la boca contra una pila **fina** en
contacto con ella, no contra un bloque macizo.

**Tres veces monté mal el banco antes de que arrancara**, y las tres por lo mismo: los polvos se
reacomodan. Un horno relleno con huecos se vacía hacia el hueco, la yesca se desliza y el piloto
acaba tocando aire. Arrancó cuando puse el fuego **por abajo**, como un horno de verdad.

## HF3 · La estufa de tolva (B-F5)

Silo de 12×30 (360 celdas de fibra) sobre un fogón con boca de 3, piloto en la solera, **cero
intervenciones**:

**9 728 ticks = 324 s de mundo** con el fogón por encima de 150 raw (la aceptación pedía ≥ 9 000
ticks). La fibra cae por gravedad a medida que la base se consume; al final no queda fibra ni
carbón, solo 362 de ceniza. El criterio §7.2 del diseño (proceso de energía sostenido ≥ 5 min con
0 intervenciones) **se cumple**.

## HF4 · El libro de energía

`LabCombustibleQuemado` (unidades de reserva consumidas) y `LabCalorFuego` (raw inyectados por
esa combustión), una línea en `ProcessCombustion`. La razón entre los dos **mide por sí sola
cuánto de la quema ocurrió sin respirar**: con la fibra al aire sería 14 (su `combustCalorRaw`) y
en sordina 7.

En la tolva: **65 920 u quemadas, 678 146 raw, razón 10,3** → el **53 % de la quema ocurrió
ahogada**. Eso es lo que la hace durar cinco minutos en vez de uno, y ahora es un número que se
lee en el panel en vez de una intuición.

## Veredicto §7 del diseño del fuego

| criterio | estado |
|---|---|
| 1. Máquina escondida en las leyes (el horno hace vidrio; la llama suelta no) | **✔** 29 celdas de vidrio a 386 °C; el hogar solo, cero |
| 2. Automatización sin jugador ≥ 5 min | **✔** 324 s con 0 intervenciones (tolva) |
| 3. Mando de geometría con respuesta monótona | **✘** la boca del horno no regula con carbón macizo (Q9); sí regula en la carbonera (0 → 19 % de ceniza) |
| 4. Cadena cruzada no guionizada (agua × luz × fuego × suelo) | **pendiente** (B-F6, iría en H7 jugando) |
| 5. Libro mayor de energía | **✔** implementado y con lectura física (la razón calor/reserva) |

**3,5 de 5.** Lo que no tira es el criterio 3 tal como está formulado, y la medida dice que el
mando existe pero vive en la geometría del **combustible**, no en la del recinto.

## Defaults nuevos de R135 (95 parámetros)

| parámetro | valor | por qué |
|---|---:|---|
| `fuego.hogarRaw` | 220 → **170** | el hogar es doméstico: hierve y seca, no vidria (F4) |
| `fuego.vidaHumo` | **400** | una bolsa de humo tiene que durar para ahogar y oscurecer (F3) |
| `luz.decayHumo` | **24** | el humo tapa la luz del cielo (F3) |
| `suelo.permCarbon` | **20** | el carbón absorbe agua: guardarlo mojado es guardarlo apagado |
| `fuego.vidrioRaw` | **200** | la frontera del calor industrial (F5) |
| `fuego.vidrioVisitas` | **60** | tiempo al rojo seguido; si se enfría, se reinicia (F5) |

Verificado jugando: laboratorio reconstruido con el hogar a 170 y el piso nuevo (arcilla en y245,
sedimento en y247, labio de roca a y249 con el rebosadero libre en y250), y una carbonera de
terracota pintada junto al hogar de la sala convirtiendo fibra en carbón (243 → 109 y subiendo,
solo 7 de ceniza). 0 errores de consola. Captura `R135_hf1_carbonera.png`.
