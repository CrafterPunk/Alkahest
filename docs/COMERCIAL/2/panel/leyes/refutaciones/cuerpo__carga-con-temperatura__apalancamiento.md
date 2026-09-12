# Refutación · cuerpo / `carga-con-temperatura` (lente: apalancamiento)

**Veredicto: refutado tal como está escrito.** Sobrevive una pieza más pequeña (§6).

## 1. La inercia que promete no existe con la fórmula que cita

La regla dice «misma fórmula que `LabFlujoTermico`» con `c = CAgua · CeldasFrasco`. Esa fórmula
(`SimStepper.Laboratorio.cs`, `LabDifusionTermica`) hace `step = flujo/(64·c)` y, si sale cero,
**fuerza ±1**. Con `k = min(2, 8) = 2` y `d ≤ 255`, `flujo ≤ 510`; el paso natural solo llega a 1 si
`64·4·N ≤ 510`, es decir `N ≤ 1`. Con dos celdas o con las 900 de `Flask.Capacity` el paso es siempre
el suelo: **1 raw por visita (7,5 °C/s) para cualquier carga**. «30 celdas se enfrían despacio, 3 en un
suspiro» es falso: las dos igualan al cuerpo en 32 visitas = 8,5 s. Sin el suelo (acumulador
fraccional: mecanismo nuevo), el frasco lleno tarda ≥ 450 visitas por raw y vuelve a ser el termo de
hoy. Como `SuckRatePerTick = 30` llena 900 celdas en un segundo, «cuánto llevar» no es decisión: más
es gratis y siempre mejor. La decisión 1 muere en las dos ramas.

## 2. El cuerpo es un termostato, no una ruta

C1 tira de `Calor` hacia 78 cada `TiroAmbienteTicks`, y el frasco se iguala al cuerpo en 8,5 s: la
temperatura de llegada es la del cuerpo en los últimos ocho segundos, no la del camino. «Arroyo o
galería del hogar» se reduce a «dónde estabas al final». El «abrigo»: el frasco entra en la suma de
flujo del cuerpo como UN término (`2·32 = 64`) frente a 66 celdas de aire frío a `k = 4` (≈ −150 cada
una): menos del 1 %. No se notaría ni con el panel F8.

## 3. La brasa es un temporizador fijo, y la caída no prende

`TFrasco += BrasaCalorRaw` por visita sin contar cuántas brasas; el cuerpo recibe el suelo de +1 menos
el tirón metabólico: la mano se abre a los ~8 s SIEMPRE, con una brasa o con cien, y mucho antes de
que la brasa muera (60-90 unidades × 16 ticks = 32-48 s). Mecha constante, no cruce. Peor: la regla 3
no toca el vertido, y `PaintCell → SetCell` resetea `aux` (`CellGrid.cs:242`): la brasa vertida nace
con vida 0 y `ProcessBrasa` la vuelve ceniza en su primer paso (≤ 4 ticks) tras UNA tirada de
`BrasaReencenderPct = 8 %`. «La yesca es lo primero que prende» ocurre el 8 % de las veces, hoy y con C2.

## 4. Coste escondido y multi

`CuerpoSim`, `LabCuerpoJugador`, `Calor`, `calorSuelta`, `HashCuerpo`: cero apariciones en el
código. C2 no tiene sentido sin C1 (1 semana). En multi no hay «retraso»: `SimSync.cs:1111` (R76)
descarta HOY la temperatura del frasco del invitado y pinta con `PaintStable`; para el invitado C2
exige antes `temp[]` en el RLE, prerrequisito anotado y no hecho.

## 5. Cuenta de cruces y decisiones

De 5 decisiones: la 1 muerta; la 2 casi muerta; la 3 es de C1/C4; la 4 es una mecha fija; la 5 (hielo
arriba) es real, pero es una restricción: el hielo no aguanta 8,5 s en mano con el cuerpo a ≥ 62 raw
(`Ice.meltsAt`), así que «algo helado −5 °C» (`OrderSystem.cs:468`) pasa a exigir el bloque frío a ~7 s
de la boca de entrega: geometría de nivel que hay que balancear, justo lo que la función objetivo
penaliza. La fase por media, además, convierte 900 celdas de hielo en agua en una sola visita
(acantilado, sin `LabLatente`). Cruces reales: hielo ↔ cuerpo frío (necesita C1) y los encargos
heredados. Uno y medio de cinco. Apalancamiento 4, no 8.

## 6. Ajuste: la versión mínima que sí compra juego

**«La brasa sigue viva en el frasco.»** `Flask` guarda `_vidaBrasa` como guarda `_tempSum`, la
descuenta a la cadencia propia de la brasa (`BrasaLifeUnitTicks`, sin sordina: la boca está abierta),
pasa `counts[Brasa] → counts[Ash]` al agotarse, y `PourMaterial` pinta con
`SetCell(idx, mat, resetAux: false)` + `aux = vida restante` (sobrecarga de `PaintCell`). Unas 20
líneas, cero parámetros, sin C1, sin tocar la sim, prueba unitaria determinista. Compra: llevar fuego
tiene alcance medido por la vida de la propia brasa (8-12 s → 120-180 celdas a pie), la brasa caída
prende de verdad (8 % × ~20 pasos ≈ 80 %) y la cadena hogar → yesca → carbón queda acotada en el mapa.
Tuning escondido: ninguno nuevo. El «frasco como celda del mundo» (una celda guardada por visita,
round-robin contra la celda del `CarryAnchor`, fase por celda, sin cuerpo) solo tiene sentido cuando
cargar más cueste algo (C4); hasta entonces es un termo.

**Coste corregido:** 1,5 semanas tal como está (C1 + C2, solo anfitrión); 0,25 el ajuste.
