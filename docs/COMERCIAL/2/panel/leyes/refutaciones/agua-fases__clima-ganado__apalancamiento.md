# Refutación · `clima-ganado` (lente agua-fases) · criterio: apalancamiento

**Veredicto: REFUTADO.** El fenómeno («la roca recuerda») es bueno; el mecanismo (que `ambient[]` persiga a `temp[]`) quita el único sumidero térmico del laboratorio, reabre las dos clases de bug que la regla 31 cerró y cuenta como decisiones cosas que sus propios números desmienten. La versión mínima que compra lo mismo no escribe `ambient[]`.

## 1. Los números se contradicen

«1/8 de la grilla cada 256 ticks» = cada celda visitada cada 2048 ticks. En 18 000 ticks son 8-9 visitas a ±1: una pared llega a 78-79, nunca a los «≥ 90 raw» de la verificación. O el coste es ×8 (sigue barato) o la memoria no existe. Uno de los dos renglones está mal, y es el que fija el carácter de la ley.

## 2. Quita el sumidero: cada umbral calibrado pasa a ser historia

En `LabDifusionTermica` (SimStepper.Laboratorio.cs:1306-1345) el calor del hogar solo sale por el tirón ±1 hacia `_grid.ambient[i]` y por el borde del mundo, que se salta (línea 1317) y está a más de 100 celdas de cualquier sala. Con memoria, justo donde hace falta sumidero `ambient` sube hasta igualar a `temp`, el tirón vale cero y el calor se filtra más lejos, donde vuelve a formarse memoria. `sign(media4 − ambient)` solo estabiliza rampas de ≤ 1 raw/celda: el cono crece hasta el borde; con una hora de hogar el laboratorio es otro mundo. Lo calibrado contra sumidero fijo (`HogarRaw` 170 prende yesca 130 y no carbón 200; `VidrioRaw` 200 sostenido 60 visitas: horno 18/18, hogar 0) depende ahora del tiempo encendido y de la geometría construida. Eso es balance ante construcciones arbitrarias: «vidrio más barato la segunda hornada» es un umbral erosionándose con el uso, no un cruce.

## 3. La mitad fría reabre el bug que la regla 31 mató

SimLevelBuilder.cs:122-128 garantiza que el ambiente no congela agua por sí solo; en el laboratorio `freezesAt` = 60 raw (Universe.Laboratorio.cs:211). Una bodega que «aprende» el frío de un núcleo a 30 raw congela agua sin fuente: «el agua me sigue saliendo congelada», por diseño. Además `media4` truncada con `sign()` sesga hacia abajo (bulto +1 con vecinos 3×71+70 cae; hoyo −1 con 3×69+70 se queda): las memorias frías persisten y las cálidas se erosionan, la asimetría del playtest 13 por redondeo, la razón por la que se quitaron LAS DOS zonas.

## 4. La vista existente lo esconde

SimRenderer.Laboratorio.cs:176 pinta la vista Temperatura como `temp − ambient[idx]`. Una sala curada a 90 raw sin fuego se ve gris, «a su temperatura». El «par Temperatura/Clima» exige redefinir la vista que hay; sin eso, el campo lento e invisible es literalmente «algo se calentó solo».

## 5. Cuenta de decisiones

«Dónde mantener el fuego» no tiene lado de coste: `LabHogar` (línea 917) es un pin sin combustible, nada invita a apagarlo y la memoria solo actúa tras apagar. «Nevera que mejora» necesita los candidatos 1 y 3 y es el bug del §3. «Leer una ruina» la refuta su propia prueba de olvido (±2 de 70 a 30 000 ticks = 17 minutos). Queda una: precalentar la cámara antes de la hornada. Cruces reales hoy: dos (sumidero más lento → fuego/evaporación; el rocío se muda a la pared que no vivió fuego), y ambos son «la temperatura tarda más en volver». Un pomo, no una ley. Apalancamiento 7 → 3.

## 6. Ajuste mínimo (2 días, sin tocar `ambient[]` ni la regla 31)

Inercia térmica de la roca dentro de `LabDifusionTermica`: para `Stone/PisoEstructural/Terracota/Arenisca` el `ambientSweep` corre cada `TiroAmbienteTicks × InerciaRoca` (parámetro, ~8). Tres líneas, un parámetro, acotado (siempre vuelve a 70), simétrico, sin campo, pasada ni vista nuevas, sin dado. Compra lo mismo: la sala sigue tibia minutos tras el fuego, la bodega guarda el frío tras quitar el núcleo, el rocío se muda mientras dura. Banco: aire de sala sellada ≥ 80 raw a 3000 ticks de apagar contra 70 en control; hash «laboratorio base» intacto; rebase de escenarios con fuego. Si Cesar quiere un campo persistente, solo es segura la memoria SOLO cálida, recortada a `[70, 70+ClimaMax]`, con redondeo simétrico, doble búfer (R16) y la vista Temperatura pasada a absoluta antes que la ley.

**Valores corregidos del candidato tal como está:** apalancamiento 3; tuning 4 (`ClimaCadaTicks`, tope, redondeo, materiales, vista, y un playtest para saber si una sala a 60 °C sin fuego se lee como memoria o como fallo); coste 2,5 semanas (pasada, tope y redondeo 3 días; vistas 3; rebase y escenarios 2; una ronda de playtest, la parte incomprimible).
