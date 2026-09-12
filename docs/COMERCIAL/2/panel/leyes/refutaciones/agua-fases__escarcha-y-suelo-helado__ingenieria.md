# Refutación de ingeniería · `escarcha-y-suelo-helado` (lente agua-fases)

*Director técnico. Leído: `SimStepper.cs` (ApplyPhase 640, Transform 707, SolidoTieneApoyo), `SimStepper.Laboratorio.cs` (LabAire, LabGotear, LabPoroso, LabPlanta, LabFrio, LabLuz, LabDifusionTermica), `CellGrid`, `LabMateriales`, `LabBench.cs`, `Cincel.cs:468`.*

## Veredicto: VIABLE CON AJUSTE

Determinismo intacto (sin dado nuevo), coste por tick despreciable, verificable en el banco. Pero tres de sus cinco piezas no hacen lo que dicen, por código que el autor no leyó.

### 1. La escarcha del serpentín no se sostiene: cae como el granizo de hoy

`LabNacerHielo` nace en `LabVecinoVacio(abajoPrimero)`: debajo del serpentín. Ese hielo es StaticSolid con `caeSolido` y `cohesionCeldas 4`, y `SolidoTieneApoyo` solo lo sostiene con algo debajo o con una cadena horizontal de ≤4 celdas hasta alguien apoyado. Bajo 31 celdas de serpentín con aire a los lados, la primera escarcha cae al tick siguiente. Sin adhesión, (a) es el granizo de hoy con otro contador: ni carámbanos, ni sábana, ni nada que picar. Es el error de fondo.

### 2. El hielo no puede recibir condensación: `humedad` ya es su volumen

`Transform()` no toca `humedad`: el agua que se congela deja el hielo con 255 y al fundirse recupera su celda entera (así se conserva hoy). En `LabAire`, `cabe = 255 − hum[tgt]` = 0. Añadir Ice a `LabEsSuperficieCondensable` es código muerto para todo hielo nacido de agua; y si la escarcha nace con humedad 0 para «poder recibir», el deshielo crea agua vacía que `LabAgua` borra con 255 u fuera del libro. El contador de escarcha necesita otro byte.

### 3. Picar hielo no hace nada

`Cincel.cs:468` pinta el producto con `PaintLab(..., temp[idx], ...)`: agua a ≤60 raw que `ApplyPhase` vuelve hielo al tick siguiente, antes de moverse.

### 4. El test «a 64 raw hielo = 0» fallará

`LabFrio` inyecta −20 a los cuatro vecinos por visita y la gota nace en el de abajo: una de cada ocho baja a 44 raw y congela. Hoy, por lectura, graniza todo (`LabNacerAgua(j, temp[idx])` a 30 raw), pero nadie ha contado `Freeze` en «alambique de r141»: la premisa está sin medir.

### 5. El riesgo mayor va al revés

`LabDifusionTermica` mueve ±1 raw/visita como mínimo ante cualquier gradiente. Una celda de piel con un vecino de hielo y tres de aire (k = min(2,4) = 2) se calienta +1/visita salvo que el hielo interior esté a ≤ T − 3·(T_aire − T); con la cámara alta a 64 raw de ambiente (`Clima(...)`, no los 70 del documento) sale una escarcha de 1-3 celdas que funde a 62 y GOTEA. Se autolimita; lo que no hace es amurallar. Sello e invernadero solo existen en una sala sellada cuyo aire baje de 60: esa nevera sí es un cruce bueno y merece escenario.

## Ajuste mínimo

1. **Adhesión** gateada por `LabActivo && m == Ice` en `SolidoTieneApoyo`: sostenido si encima hay un StaticSolid que no cae o hielo adherido, hasta `cohesionCeldas` (4) hacia arriba; más largo, el carámbano se desprende (emergente gratis). Playtest 29 sigue vivo fuera del laboratorio.
2. **Contador de escarcha en `reposo`** del hielo (nadie lo escribe para Ice; viaja en `SwapCells`). `LabNacerHielo` nace con humedad 255 y se apunta a `LabBalanceU` como `LabTransformar`. `case MaterialId.Ice: LabHielo` en `LabCampos`.
3. **Cincel:** `PaintLab` con `temp = max(temp, meltsAt)`: conserva y funde de verdad.
4. **Guardas (b)** también en el paso 1 de `LabPoroso` (un poroso helado saturado exudaría agua que graniza). (c) tal cual.
5. **(e) fuera:** `fuego.frioRaw` ya es «el número» (slider 0-70); el grado por bloque pide un verbo del pincel (R48) sin decisión nueva.
6. **Banco, en orden:** `Freeze` y `LabHieloNacido` en `Resultado`; correr «alambique de r141» sin cambios para fijar la premisa; escenarios «escarchador» (serpentín a 30: Δ hielo < 2 % en los últimos 3000 de 9000 ticks, agua al lecho por `MedirLecho` > 0), «alambique 64» (goteos 900 ± 10 %, `Freeze` ≤ 10 %), «helada» (planta sobre sedimento clavado a 55 muere ≤ 600 ticks, gemela a 70 vive, `LabInfiltrado` = 0), «ventana» (luz bajo dos celdas ≥ luz − 40), «nevera» (sala sellada con núcleo: aire ≤ 60 y escarcha creciente); «arco largo» con hielo < 10 % de la cámara alta. Rebase de hashes.

## Números corregidos

Coste **1,5 semanas** (adhesión, contador, LabHielo, cincel, seis escenarios, rebase). Tuning **7**: umbrales existentes (60/62/FrioRaw), adhesión sobre `cohesionCeldas`; una tarde de Play para ver si la escarcha se lee. Apalancamiento **6**: suelo y raíz helados (b) y la ventana (c) son baratos y reales; la escarcha es rocío visible de pocas celdas, no pared, salvo en la nevera sellada, donde sí compra juego.
