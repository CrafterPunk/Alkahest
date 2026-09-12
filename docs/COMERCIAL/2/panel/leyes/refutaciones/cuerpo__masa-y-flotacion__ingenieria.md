# Refutación de ingeniería · cuerpo / `masa-y-flotacion`

**Veredicto: viable con ajuste.** La masa sí, renormalizada y con suelo; la flotación no, todavía.

## 1. La fórmula no cuadra con el frasco real

`Flask.Capacity = 900` celdas y `SuckRatePerTick = 30`: el frasco se llena en un segundo, así que la
carga típica son cientos de celdas. La caja del muñeco cubre 6,4 × 11,2 ≈ 72 celdas
(`MedioAnchoColision 0.32`, `MedioAltoAbajo 0.64`, `MedioAltoArriba 0.48`). Con
`m = 60 + Σ counts·density/110`:

- 100 celdas de agua → m = 160 → `FactorSalto` 0,375: el salto de 2,2 u (`ImpulsoSalto 10.5`) cae a
  0,31 u, por debajo del brinco mínimo de 0,5 u que el paquete R121b garantiza con `CorteSalto`.
- Frasco lleno de grava (`density 200`, `Universe.Laboratorio.cs`) → m ≈ 1 700 → factor 0,035: el muñeco
  ni salta ni camina.
- El empuje máximo son 72 celdas sumergidas: cualquier frasco con más de ~12 celdas de agua hunde el
  cuerpo. «Lastre» y «flotador» se reducen a «toda carga hunde».

Tal como está escrita, la regla inutiliza el verbo principal a pie. Arreglarla exige normalizar por
`Capacity` y ponerle un suelo al factor: justo el número que solo se afina jugando.

## 2. La flotación choca con la física validada y con un modo sin decidir

`HandleMovement` tiene dos ramas («DOS MODOS, UNA FÍSICA», R118b): a pie (gravedad, coyote, buffer,
ápice, corte) y volando (`MoveTowards(input·moveSpeed)`, sin gravedad). La flotación es un tercer medio:

- En A/B el hover ignora cualquier empuje: «la poza deja de ser un sitio al que se vuela» solo vale si
  el empuje pisa al vuelo, un estado nuevo dentro del bloque que costó nueve rondas (R110-R121).
- En C el agua no es `EsSolidoDelMundo` (`LabMateriales.cs:14`): el muñeco YA se hunde y camina por el
  fondo. «Bucear con grava» existe hoy gratis; lo nuevo es una restricción, y la decisión que compra es
  recuperar lo que se le quitó.
- Flotando, `SobreSuelo` es falso (sonda de sólidos bajo la caja) y `puedeSaltar` nunca se cumple: a pie
  no se sale del agua a una cornisa sin un «salto de agua» propio, con `AsentarEnSuelo`, `Desenterrar`
  y el squash de aterrizaje reaccionando al estado nuevo. Es water-platforming, afinado a mano siempre.
- `CajaChoca` hace `continue` sobre lo no sólido y devuelve al primer sólido: no cuenta agua; hace falta
  un bucle nuevo. Y `Water` es celda parcial (`humedad` 1..255): leer solo `mat[]` trata una lámina
  casi vacía como celda llena.

DISENO_MOVIMIENTO §3, riesgo (1): el mundo está diseñado para un volador y el modo base sigue en
instrumentación (F6, PlayerPrefs). Construir el medio acuático sobre un modelo sin elegir es construir
dos veces.

## 3. Lo que el banco puede juzgar

Determinismo y tick: intactos (todo en `Update`, lectura de `mat[]` vía `SampleMaterial`, frasco local
también en el invitado). Pero el banco no tiene avatar: solo una función pura es verificable. Probar el
empuje headless exigiría extraer la física de `HandleMovement` a un paso puro con dt fijo, es decir,
refactorizar el código afinado a mano con riesgo de regresión de la sensación validada.

## 4. Coste real

Lo declarado (0,25 semanas) cubre la función de masa. El medio acuático son ~1,5 semanas de Opus más
3-5 sesiones de Cesar, la moneda cara. Es la propuesta con más iteración humana y menos juicio de la
simulación, como reconoce su propio autor.

## 5. Ajuste mínimo (lo que sí aprobaría)

Solo masa. Carga relativa `L = Σ counts[m]·density[m] / (Capacity·110)` (la lectura
`Universe.Get(m).density` ya está en `Flask.cs:224`), calculada en `Flask` al cambiar los conteos, no
por frame. `FactorCarga = max(piel.sueloCarga, 1/(1 + L·piel.pesoFrasco))`: dos registros en
`LabParams` (grupo PIEL), suelo ≥ 0,6 para que el brinco mínimo siga existiendo. Se aplica a `velPaso`
e `ImpulsoSalto` (`ApprenticeController.cs:559-569`) y a `moveSpeed` en vuelo (`:588`). Con C1,
`Mojado/255` suma a L. Sin flotación ni arrastre hasta que el modo de movimiento esté decidido y la
lente de aire traiga un campo que le dé al agua algo más que sostener.

**Benchmark:** prueba unitaria de `MasaRelativa`/`FactorCarga` con contenidos sintéticos (vacío → 1,0;
900 agua y 900 grava → suelo; 900 fibra intermedio; monótona; nunca bajo el suelo) y un número medido
en juego: `TelemetriaMovimiento.Salto()` registra la altura alcanzada por carga.

**Valores corregidos (versión ajustada):** apalancamiento 4 (la ley de densidad que ya estratifica
líquidos en `ProcessLiquid:1194` y hunde polvos en `ProcessPowder:1088` pasa a regir al cuerpo, pero
compra una sola decisión: qué llevar), tuning 7 (dos sliders, uno por sensación), coste 0,5 semanas
(dos días de Opus y una sesión de Cesar).
