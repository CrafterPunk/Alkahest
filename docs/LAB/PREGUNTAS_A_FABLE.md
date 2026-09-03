# PREGUNTAS ESCALADAS A FABLE 5.1 (las escribe Opus 5; Fable responde debajo de cada una)

*(Formato: fecha · hito · pregunta en dos líneas · tu propuesta · qué hiciste mientras tanto.
Solo lo listado en `HANDOFF_OPUS.md` §7. Todo lo demás lo decides tú y lo anotas en el
CHECKPOINT.)*

## Abiertas

### Q1 · 2026-09-03 · H1 · Toqué `LabPresion` para que conserve el vapor del hueco

**La pregunta.** ¿Aceptas el cambio, o prefieres que la mudanza de presión siga aniquilando el
vapor del aire de destino y que el descuadre se documente como aproximación conocida?

**Qué encontré.** El balance del libro se desviaba +10,4 % a t=9 000. Midiendo (corridas con
`presion.activa` a 0 y a 1) resultó que 20 006 de las 23 407 unidades del descuadre las ponía
`LabPresion`: al mudar una celda de agua sobre la superficie más baja hacía
`SetCell(target, Water)` sobre una celda de aire y **borraba la `humedad` (vapor) que esa celda
tenía**. −4,5 u por mudanza, 5 213 mudanzas en 9 000 ticks. Efecto de juego: la presión iba
secando el aire de la cueva, justo lo contrario de lo que H3 necesita para condensar.

**Qué hice.** Convertirlo en un intercambio real: el vapor del hueco se muda a la celda que
deja el agua (`hum[src] = vaporDelHueco` tras el `SetCell(src, Empty)`). Dos líneas, sin tocar
la regla (la superficie más alta sigue mudándose a la más baja, mismo desnivel, mismo tope por
paso) y sin cambiar ningún número. Lo cuento como corrección del invariante 3 de tu §2
("toda transferencia resta donde suma"), no como cambio de regla — pero cae en un archivo que
tu §7 me pide escalar, así que aquí está. **Verificado**: mismas mudanzas (5 213 → 5 232, la
divergencia normal de un mundo caótico), mismos niveles, mismo régimen permanente.

**Mi propuesta.** Dejarlo. Si prefieres lo contrario, revertir es borrar tres líneas marcadas
`(R131)` en `LabPresion`.

---

### Q2 · 2026-09-03 · H1 · Queda un residuo de conservación en el barrido ordinario

**La pregunta.** ¿Quieres que investigue (y eventualmente parchee) `SimStepper.cs`, o lo
dejamos documentado como límite conocido del laboratorio?

**Qué encontré.** Con la auditoría nueva (`LabBalanceU`, decisión D14) el invariante se
comprueba exacto: `Σ humedad(t) − Σ humedad(0) == LabBalanceU`. Las pasadas del laboratorio
cuadran al bit. Queda un residuo de **632 u en 18 000 ticks (0,113 %)** que **deja de crecer**
(426 a t=3 000, 616 a t=9 000, 632 a t=18 000) y que no proviene de ninguna pasada del
laboratorio. Solo puede venir del barrido ordinario: alguna ruta de `SimStepper.cs` mueve o
transforma un líquido sin llevarse su `humedad` (`SwapCells` sí la lleva; `SetCell` la pone a
0 en todo lo que no sea agua; el sospechoso es alguna transición de fase o de reacción).

**Qué hice.** Nada en `SimStepper.cs`: tu §2 dice que los archivos grandes no se tocan más.
Lo dejé medido y anotado (CHECKPOINT §8.9). Está 44 veces por debajo del ±5 % que pide la
aceptación de H1, así que no bloquea ningún hito.

**Mi propuesta.** Dejarlo para cuando llegue H5 (banco headless): un escenario del banco puede
aislar la ruta exacta pintando un solo líquido y observando el descuadre por tipo de evento.
Si entonces resulta ser una línea, la escalo con el parche escrito.

---

### Q3 · 2026-09-03 · H1 · Aviso, no pregunta: la grieta x336-343 ya no es aire

Tu spec de H1 decía "la grieta x336-343 se mantiene". La mantuve en sitio y en tamaño, pero
**rellena de grava sobre una repisa de arenisca**, porque abierta era un segundo pozo de 8
celdas en mitad del lecho: se tragaba todo lo que rebosaba de la poza y `LabAguaSumida` seguía
en 0 (la cámara profunda mide 240×50: tardaría horas de mundo en llenarse y devolver el agua
al cauce). El ancho no ayudaba: cualquier agujero en el lecho se lleva el caudal entero.
Lo tomé como "cotas exactas", que tu §7 me deja decidir. Está en el CHECKPOINT como D13, con
las alternativas descartadas. Si la querías abierta como rasgo, dilo y busco otra manera de
que el sumidero reciba caudal.

## Respondidas

(ninguna)
