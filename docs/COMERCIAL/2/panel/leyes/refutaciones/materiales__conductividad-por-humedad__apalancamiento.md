# Refutación de apalancamiento — «El poroso mojado conduce, el seco aísla» (materiales, candidato 4)

**Veredicto: refutado** como candidato con entrada propia. No es una capa: es una interpolación honesta dentro de una ley que ya existe. Pero el apalancamiento se mide en decisiones nuevas, y cruzando las leyes vigentes con el código en la mano no compra ninguna.

## 1. Las tres «decisiones nuevas», una por una

1. **«Curar los materiales antes de construir.»** El cono de arena nace por `SetCell` (`SimLevelBuilder.Laboratorio.cs:78-79`), que limpia `humedad`: nace SECO. Solo la veta de arcilla nace húmeda (`Campo(..., 160, 0)`), y es material de talla, no de obra. No hay materia que curar hasta que otra ley (el goteo) moje algo. Cero.
2. **«Aislar el horno con roca o polvo seco.»** Hoy `KRoca = 2 < KPolvo = 3` (`LabParams.cs:111`): la roca YA aísla mejor. Y `LabFlujoTermico` (`SimStepper.Laboratorio.cs:1349-1350`) toma `min(ki, kj)`: polvo mojado contra roca, hogar, terracota o arcilla conduce 2, igual que seco. La decisión existe y el candidato no la ensancha donde importa.
3. **«Agua lejos del horno, o para enfriarlo.»** Ya existe dos veces: el combustible mojado no prende (`FibraMojadaMin`, `:164`) y el agua apaga. Esto añade una tercera causa del MISMO resultado, más lenta y menos legible.

Cuento **cero decisiones nuevas** y **dos cruces reales acotados**: el núcleo de arena/ceniza del horno mojado (arena contra ceniza, ambas porosas: k 3 → 8, c 2 → 4) y el lecho de sedimento mojado que reparte calor lateralmente. Ninguno cambia lo que el jugador hace; cambian cuánto tarda.

## 2. Por qué el efecto es pequeño incluso donde existe

- **Aritmética entera.** `k = 3 + 5·h/255` vale 4 a h=60, 5 a h=127, 6 a h=200; `c = 2 + 2·h/255` NO cambia por debajo de h=128. En el rango que importa a las plantas (60-100) el candidato aporta +1 de k y 0 de c. Solo cerca de la saturación se parece al agua, y a 255 con vacío al lado el poroso EXUDA una celda de agua real (`LabPoroso`, paso 1), que ya tiene `KAgua`/`CAgua`.
- **Mojado y caliente dura segundos.** `LabSecarHacia` (`:735-749`): `rate = 3·(16 + t)/16`, `t = temp − 70`; a 200 raw son 27 u por visita, una cada 8 ticks (`LabCampos`): una celda saturada se seca en ~80 ticks, 2,7 s. Sin remojo continuo el «horno que suda» es un transitorio; con remojo, manda el agua líquida que cae.
- **El latente ya está y no llega.** `Latente = 4` raw por 255 u (`LabParams.cs:57`): secar una celda entera enfría 8 °C una vez. La «tercera vía agua×fuego» es solo el término c, y c se dobla únicamente en el poroso saturado.

## 3. Sin señal, no hay aprendizaje

«Un muro mojado humea» es falso: en `SimStepper.Laboratorio.cs` no se crea `MaterialId.Steam` en ningún sitio (aparece solo en `LabK`/`LabC`); el secado escribe `humedad[j]` del aire, invisible. Sin F8 la única señal es negativa («no vidria»), indistinguible de «falta ceniza» o «se enfrió a ratos» (`reposo` se reinicia). Una ley que ejecuta pero no revela ni juzga no enseña.

## 4. El tuning que esconde

«Cero parámetros» es cierto y engañoso. La visibilidad depende de los cocientes `KAgua/KPolvo` (8/3) y `CAgua/CPolvo` (4/2), los mismos que gobiernan agua, aire y alambique (90-900 goteos). Ensancharlos retoca la línea base térmica entera: nueve hashes y la calibración del goteo. No se afina aislado.

## Versión mínima con algo de apalancamiento

Solo como **kill test de un día** sobre el banco del horno que ya existe, nunca como ley con nombre:
1. Interpolar SOLO `c` (lo único que sobrevive a `min(k)`), desde el `c0` del propio material, y solo en polvos (Sand, Ash, Carbon, Sedimento, Grava, Fibra); arcilla y arenisca fuera.
2. Flag `termica.humedadConduce`, default 0.
3. `MontarHorno` con el núcleo sembrado a `humedad = 255` y repuesto cada 8 ticks desde arriba: `LabVidrio` 18 seco vs < 18 mojado a 9 000 ticks. Si da 18/18, muere.
4. Aunque separe, no se enciende sin señal visible. Esa señal (vapor nacido del secado caliente en `LabSecarHacia`) cruzaría humo, luz y condensación: ahí vive el apalancamiento que este candidato reclama sin tenerlo. Es otro candidato.

**Valores corregidos.** Apalancamiento 6 → **2**. Tuning 9 → **8** (sin parámetros propios, pero acoplado a la línea base térmica). Coste 0,5 → **0,25** semanas: el kill test; enviarlo costaría 0,75 con línea base nueva, y no hay razón para pagarla.
