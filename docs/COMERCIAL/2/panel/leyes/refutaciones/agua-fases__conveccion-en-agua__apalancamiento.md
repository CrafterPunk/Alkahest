# Refutación · `conveccion-en-agua` (lente agua-fases) · foco: apalancamiento

**Veredicto: viable con ajuste.** Escrita así no dispara donde dice que importa, dos de sus cinco cruces son de otro candidato y su apalancamiento está inflado al doble. El mecanismo (un `Move` ordenado por temperatura en `ProcessLiquid`) sí es cruce y no capa, y se verifica en banco: por eso no la refuto entera.

## 1. Con sus propios números, la ley está muerta

`LabDifusionTermica` (SimStepper.Laboratorio.cs:1336-1337) fuerza un paso mínimo de ±1 raw por visita: dos celdas de agua vecinas que difieran en 1-2 raw se igualan en pocas visitas. La regla exige `flot(ti) > flot(tj) + 2`: al menos 3 raw entre vecinas verticales. Ese gradiente sólo lo dan los inyectores fuertes, y estos no dejan ventana:

- `LabCalentarHasta` suma `HogarCalor` = 40 al agua en contacto: 70 + 40 = 110 = `agua.boilsAt` (Universe.Laboratorio.cs:212). `ApplyPhase` corre en `ProcessIfNeeded` (SimStepper.cs:358) **antes** de `ProcessLiquid`: la celda hierve en la primera visita y nunca se compara con la de arriba. «El fondo sube antes de llegar a 110» es falso con los defaults; en «hervidero» (200 hogares bajo 50 filas de agua, LabBench.cs:170-176) el contacto es directo y la ley no toca la única fila que importa.
- `LabFrio` inyecta −20: 70 → 50 < `freezesAt` 60. El primer hielo nace pegado al núcleo con anomalía y sin ella: la prueba «fila superior contra inferior» es un falso positivo que el banco daría por bueno.
- Un estanque calentado a través de piedra recibe ≤ 1 raw por visita (k = min(2, 8)): gradiente de 1 raw por celda, convección que no dispara jamás.

Con umbral 0 o 1 la ley vive, pero toda diferencia de 1 raw (las que la difusión regenera sin parar) mueve celdas al 50 % por tick: el estanque entero se agita mientras exista cualquier fuente. Ese es justo el comportamiento que compra el cruce con la decantación, y el umbral 2 lo excluye. El mando real es `ConveccionUmbral` contra el paso forzado de la difusión, y no está nombrado.

## 2. La anomalía de 4 °C no cabe en un byte

`°C = raw·2 − 120`: 0-4 °C son 60-62 raw, **dos unidades**. Con umbral 2 nunca dispara; con umbral 1 sólo 60 contra 62, y 60 es hielo ese mismo tick. `AguaDensidadMaxRaw` es un parámetro y una prueba para una regla inobservable.

## 3. Dos cruces son de `fase-con-reserva`

«El vapor nace en la superficie» y «se hiela por arriba» exigen que hervir y congelar tarden visitas; hoy son instantáneos. Sola, la ley compra dos cosas: la superficie de un estanque calentado indirectamente acaba a la temperatura del cuerpo y no a (fondo − profundidad), lo que triplica la evaporación (magnitud, legible sólo por goteos o visor), y un estanque con gradiente no deposita (`reposo` a 0 en cada swap contra `DepositoReposo` 24). De cuatro decisiones sobrevive una: apagar el fuego para cosechar sedimento, y es un candado sobre un camino que ya existe. «Desde abajo o de lado» y «radiador de techo» no producen diferencia observable.

## 4. Viola R55

El `Move` vive en `ProcessLiquid`, que sólo corre en chunks despiertos (SimStepper.cs:349); `LabDifusionTermica` no despierta a nadie. Un estanque calentado por piedra duerme estratificado hasta que cae una gota y entonces se agita de golpe: historia, no ley.

## 5. Tuning escondido

`SwapCells` intercambia `morph`: la agitación se verá como parpadeo del patrón del agua; si lee como «agua inquieta» o como error lo decide Cesar mirando. Y «hervidero» ya transporta calor por materia (las burbujas de vapor condensan al subir): la comparación debe descontarlas.

## Ajuste mínimo con más apalancamiento

1. Subordinarla a `fase-con-reserva`: sin ella, la mitad de sus cruces son imposibles.
2. Quitar la anomalía y `AguaDensidadMaxRaw`: `flot(t) = t`.
3. Umbral 1 estricto sobre raw; `ConveccionPct` como único mando.
4. En `LabAgua` (ya visita cada celda de agua 1/8): `if (|temp[i] − temp[i+W]| ≥ 1) WakeChunk`. La pasada de campos despierta al estanque con gradiente y lo deja dormir isotermo: R55 por una resta.
5. Banco: swaps por tick en «hervidero», ticks hasta dormir tras apagar, `LabDepositado` con y sin fuego.

Apalancamiento corregido: 4 sola (6 con fase-con-reserva). Tuning: 6. Coste: 1 semana, contando el diagnóstico del umbral y la rebase de «hervidero» y «arco largo».
