# Refutación · `masa-y-flotacion` (lente cuerpo, C4) · criterio: apalancamiento

**Veredicto: REFUTADO** en su forma actual. Hay una versión mínima viable al final, condicionada a contenido de autor.

## 1. El escenario de la mecánica no existe en el nivel

La flotación necesita un cuerpo sumergido. La caja del muñeco mide 11,2 celdas de alto (`MedioAltoAbajo 0.64 + MedioAltoArriba 0.48`, `CellWorldSize 0.1`; `ApprenticeController.cs:796-798`). La única poza del laboratorio (`SimLevelBuilder.Laboratorio.cs:93-108`, tramo 250-330) tiene sedimento en 122-123 y agua en **124-128: cinco filas**; los bancos vecinos (`pisoTop`) están en 132 y 134, así que rebosa a las ~9 filas (pasó en R135). Con los números del propio candidato (masa 60, volumen 66, empuje = celdas sumergidas) el cuerpo vacío sumerge 30 celdas en la poza normal y 53 a rebosar: **empuje 30-53 contra masa 60, nunca flota**. «El cuerpo vacío flota con la cabeza fuera», «la poza profunda a la que ya no se vuela» y «bucear con lastre» exigen tallar una poza de ≥ 15 celdas y darle un trabajo en el fondo: contenido de autor, la categoría penalizada.

## 2. «Bucear con grava» es lo que ya ocurre con el frasco vacío

El agua no es sólida para la caja (`EsSolidoDelMundo`, `LabMateriales.cs:14`; «puedes zambullirte en el agua», `ApprenticeController.cs:752`). A pie, la gravedad (25 u/s², caída 1,7×) actúa igual en agua que en aire: hoy cualquiera se hunde y camina por el fondo; volando, el agua es aire. El candidato debe **añadir primero** la flotación para después vender la grava como forma de recuperar lo que ya se tenía. Decisión nueva neta: cruzar la poza flotando o por arriba (volando, ya gratis). Una.

## 3. Cruces con leyes existentes: uno real, uno nominal, cero de vuelta

- **Densidad del frasco (`MaterialDef.density`)**: real. La única propiedad de la sim que entra.
- **«Agua/presión (LabPresion)»**: nominal. No lee `carga` (que es carga de finos, `SimStepper.Laboratorio.cs:28,169`, no presión) ni `LabPresion` (`:1080`). Lee `mat == Water` dentro de la caja: geometría, no ley.
- **C1 mojado**: depende de otro candidato. **Modos de movimiento**: no son ley, son el esquema de control aún en evaluación (F6, R121).
- **De vuelta a la sim: cero.** El cuerpo no desplaza ni empuja agua. La sim no puede ejecutar, revelar ni juzgar nada: es un multiplicador de un solo sentido sobre el controlador, una capa encima por definición. El documento lo admite: «el banco no la juzga».

## 4. La fórmula de masa vuelve injugable el estado normal

`Flask.Capacity = 900` (`Flask.cs:91`). `m = 60 + Σ counts·density/110`: con 900 de grava, m ≈ 1696 y `FactorSalto = 0,035` (el salto de 22 celdas pasa a 0,8); con 900 de agua, 0,06; con solo 100 de agua, 0,375 (salto 0,8 u, paso 0,56 u/s). El frasco medio lleno es la condición normal (cargas 75/60, reservorios de 276, R110). O aparecen tope, curva o divisor (tres números que no existen), o se juega vaciando con Q ante cada cornisa: fricción, no decisión. El salto de 2,2 u = 1,8 alturas (R121b) se fijó contra la geometría del nivel (repisas, túneles de doble pasada del cincel); escalarlo por lo aspirado hace que la transitabilidad dependa de una construcción arbitraria del jugador, justo lo que hay que balancear a mano. La sensación ya costó R110→R121b (velocidad 6,7→4,0→4,8, paso 1,1→1,5, impulso 8,2→10,5, gravedad 22→25, corte, coyote, buffer, squash) y sigue abierta entre tres modos.

## 5. Coste y tuning inflados a la baja

0,25 semanas no cubre: estado «flotando» compatible con tres físicas verticales (gravedad, hover, vuelo), `_enSuelo`/coyote en agua (sin suelo sólido no hay salto: hay que inventar «flotar = apoyo»), arrastre, avatar remoto (`DeducirVelocidadRemota`) y la poza honda con su tarea. Realista: 1 semana con dos rondas de playtest de Cesar, las caras. Tuning real: `masaBase` (¿flota el cuerpo vacío?), umbral de sumersión, arrastre, curva de carga: 4, no 6.

## Versión mínima con más apalancamiento (viable solo con precondición)

Quitar `FactorSalto`/`FactorVelocidad`. Una sola regla binaria que **reutiliza la ley de los polvos** (`ProcessPowder`, `SimStepper.cs:1088`: se hunde si `density > density[Water]`): la densidad media de lo que llevas frente a la del agua decide hundirte o flotar; cuerpo vacío = agua (neutro, cae despacio). Cero números nuevos salvo arrastre y filas mínimas sumergidas. Precondición: una poza ≥ 15 celdas con un desagüe que tallar en el fondo. Sin esa poza no hay mecánica que construir.

**Apalancamiento corregido: 3. Tuning: 4. Coste: 1,0 semana.**
