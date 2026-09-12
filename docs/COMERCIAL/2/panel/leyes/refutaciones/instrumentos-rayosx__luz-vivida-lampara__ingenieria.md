# Refutación de ingeniería · `luz-vivida-lampara` (lente instrumentos-rayosx)

**Veredicto: viable con ajuste.** Apalancamiento 7 · tuning humano 6 · coste 2 semanas de Opus (1,5 sin multijugador).

## Lo que el código confirma

La costura es la buena. `LabLuz` (`SimStepper.Laboratorio.cs:1177-1204`) ya hace en el reset exactamente lo que la lámpara necesita: recorre el mundo, pone `luz = 255` donde `LabMateriales.EmiteLuz` (Fire/Brasa/Hogar, `LabMateriales.cs:110`) y deduce la ventana `fx0..fx1`. Una lista de lámparas se inyecta ahí con diez líneas y con la lista vacía la pasada es idéntica al byte: los siete hashes del banco (`LabBench.cs:326-331`, incluido `HashLuz`) se conservan por construcción, no por promesa. El render tiene patrón que copiar (`_vistaTexture`, `SimRenderer.Laboratorio.cs:138-160`, `RenderChunk` 1085/1101). Y el candidato leyó bien lo que el sustrato decía mal: el fuego sí emite en el campo; lo que no ilumina es la pantalla. Nada de esto rompe determinismo ni reescribe física validada.

## Lo que el candidato no vio o contó de menos

1. **Presupuesto.** La ventana de H5 es UN intervalo. Con la lámpara en x = 700 y la boca en 118-124, `bx0..bx1` cubre el mundo entero y vuelve a los 2,86 ms de media / 7,88 de pico medidos antes de H5 (`Laboratorio/benchmarks/2026-09-04_r142_hf5c_y_h5.md`), cada 16 ticks. A ×10 sostenido son ~1,7 ms por frame de más y el peor caso por tick sube de 3,1 a ~8. El candidato lo reconoce («puede doblar su ventana») pero no lo remedia. El remedio es acotado: la unión de intervalos `[x_fuente ± alcance]` en vez del envolvente; el argumento de H5 (pérdida lateral ≥ `dMin` por celda) vale por fuente, así que el resultado es idéntico celda a celda y se verifica igual que H5 (copia sin acotar contra acotada, cuatro escenarios).

2. **El espejo no tiene `luz`.** `AlkahestSim.cs:338`: `_stepper = espejo ? null : new SimStepper(...)`; SimSync solo manda `mat[]` (`Net/SimSync.cs:30,51`). «Host autoritativo» no basta: la pantalla del invitado necesita el campo. Hay que extraer `LabLuz` como función pura sobre un `CellGrid` (es posicional, no lee RNG) y correrla en el invitado sobre su `mat` espejado, más un RPC lámpara→host para que las plantas del host la vean. Dos días que no están en la semana declarada.

3. **«Cada uno ve su propio radio» es falso con este diseño.** `_grid.luz` es un byte por celda. Si todas las lámparas entran en la sim (deben: la planta bajo cualquier lámpara crece), todos ven toda la luz. Para asimetría haría falta un segundo campo de render por cliente, que duplica la pasada y rompe «la luz que ve el jugador es la que ve la semilla». Se tacha esa decisión; la visibilidad compartida es físicamente honesta y sigue siendo información.

4. **El campo tiene un artefacto que la pantalla destapará.** En el barrido descendente (`:1234`) `LabLuzDecay` devuelve `dCielo = 1` para TODO el aire, no solo bajo la boca: un hogar, y una lámpara de 160, proyectan hacia abajo una columna de ~255 celdas casi sin perder, y 20-32 celdas a los lados. Hoy nadie lo ve; pintado, cada fuego es un faro hacia el suelo. Arreglarlo (pasada de cielo separada de la de fuegos, máximo de ambas) es acotado y verificable, pero mueve `HashLuz` y la germinación: es un cambio de física que hay que decidir, no un ajuste de paleta.

5. **Estado del banco.** La lista de lámparas tiene que vivir en la instancia de `SimStepper` o resetearse en `LabBench.Correr` como `LuzCieloX0/X1` (`:277-279`), o el banco hereda la lámpara de la última sesión de Play: la lección R23-13 de R145, otra vez.

6. **Verbos fantasma.** «Abrir una boca de cielo» no existe: `LuzCieloX0/X1` los fija el builder (`SimLevelBuilder.Laboratorio.cs:164`) y no hay herramienta ni slider (R48). «De noche» tampoco: el laboratorio no tiene ciclo. Y una lámpara gratis y sin calor domina al fuego como fuente: «dónde poner el fuego para ver» deja de ser decisión. `LabLuzDesde` (`:1269`) devuelve 0 en sólidos: la celda de la lámpara debe ser aire o la regla necesita una excepción.

## Versión mínima

Lista `LabLamparas` en el stepper (posición en celda, potencia), muestreada en `Step()` desde el avatar del host; inyección en el reset de `LabLuz` sobre celdas Empty; ventana por unión de intervalos; quinta textura de oscuridad (negro con alfa = 1 − brillo, 8 bandas, suelo 25 %) repintada solo en los chunks dentro de las ventanas tras cada pasada (fuera es negro una vez, no 221 k escrituras); `LabLuz` extraída para el espejo. Sin tocar `dCielo` todavía. Sin «radio propio».

## Benchmark que lo prueba

Siete hashes intactos en los nueve escenarios sin lámpara. Escenario nuevo «lámpara sobre lecho» (x = 117, y = 262, potencia 160, 9 000 ticks): `LabPlantasNacidas` > alambique base y `MsLuz` ≤ 1,0 ms de media / 2 de pico. Escenario «lámpara en x = 700» sobre el laboratorio base: `MsLuz` ≤ 1,0 ms y `luz` idéntica celda a celda a la versión sin acotar. Captura por RunCommand (R52) del huerto con y sin lámpara.

## Por qué 7 / 6 / 2

Apalancamiento 7: convierte cada fuego y cada bolsa de humo en información de pantalla sin física nueva, pero la lámpara gratis resta la mitad de las decisiones que promete. Tuning 6: la física no pide ninguno; el suelo de penumbra, la potencia, el ancho de la boca (7 celdas: el 90 % del nivel actual queda a oscuras) y el faro del punto 4 son iteración con Cesar. Coste 2 semanas: la semana declarada más el espejo y la unión de ventanas.
