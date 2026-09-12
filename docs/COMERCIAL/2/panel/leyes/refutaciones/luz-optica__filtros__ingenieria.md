# Refutación de ingeniería · luz-optica / `filtros`

**Veredicto: viable con ajuste.** La regla del agua cabe en una línea, no toca el determinismo ni el presupuesto por tick, y es verificable en el banco. Pero dos tercios del candidato (hielo, vidrio) son código muerto contra el motor real, la verificación propuesta mide ceros, y el apalancamiento declarado es el del paquete, no el de esta ley sola.

## Lo que dice el código

1. `LabLuzDecay` (`SimStepper.Laboratorio.cs:1276-1285`) es `static`, recibe solo `byte m` y devuelve `dAgua` para `Water`. Pasarle `carga[i]` es un cambio de una línea en los cuatro sitios de llamada de los barridos (:1234, :1245, :1256, :1262). Aritmética entera, sin RNG, lectura de un byte que `LabCampos` ya dejó escrito antes (`LabPasadas` :134-148: campos → presión → luz). Determinismo intacto, también cross-machine.
2. La ventana H5 (:1219-1225) acota las columnas con `dMin = min(dAire, dAgua, dPlanta, dHumo)`. La turbidez solo SUMA al decaimiento del agua, así que la cota sigue siendo conservadora sin tocarla.
3. Coste: `LabLuz` corre cada 16 ticks y ya paga un switch por celda y dirección; una carga más en la rama `Water` es ruido. Los 1,6-1,9 ms/tick no se mueven.

## Tres cosas que no se sostienen contra el motor

**a) Hielo y vidrio son código muerto tal como está escrito.** `LabLuzDesde` (:1269-1273) solo transmite `Empty`, `Water`, `Planta`, gases y emisores. `Ice` y `VidrioVerde` son sólidos: reciben luz en la cara y devuelven 0 al vecino. Darles decaimiento propio en `LabLuzDecay` no cambia un solo byte de `luz`. "El hielo aclara, el vidrio tiñe" exige la lista blanca del candidato 1, y con ella hay una trampa: 4 y 6 son menores que `dAire` (8), `dMin` baja, el alcance de la ventana pasa de 32 a 64 columnas; si se añaden los decaimientos sin tocar `dMin`, la luz de un corredor de vidrio se recorta en el borde de la ventana. Silencioso y determinista: el peor tipo de bug.

**b) La verificación propuesta mide ceros.** `LabBench.Correr` (:279) pone `LuzCieloX0 = -1` en todos los escenarios y solo `MontarLaboratorio` la restaura (`SimLevelBuilder.Laboratorio.cs:164`). `MontarDiluvio` (:158-168) no tiene boca ni hogar, así que `LabLuz` sale en `if (fx1 < 0) return;` y el `HashLuz` del diluvio es el hash de un array a cero: `LuzFondoDiluvio` daría 0-0-0 con y sin la modificación. Además el baño tiene 140 filas (ni el agua limpia, 12 de alcance, llega al fondo) y con `carga = 255` en todo el cuerpo el fondo cumple `EsFondo && c ≥ DepositoUmbral(200) && reposo ≥ 24` (:466-490): la columna se vuelve sedimento desde abajo, y a tick 3 000 puede no quedar agua que decantar. Es un escenario de estrés de fluidos, no de claridad.

**c) El apalancamiento 5 es del paquete.** Los únicos consumidores de `luz` son germinación y crecimiento sobre celdas `Empty` (:689, :704, :870-873). Bajo o dentro del agua no decide nada. Sola, mueve `HashLuz` en los escenarios con agua en la ventana y ninguna decisión del jugador. El documento lo admite.

## Ajuste mínimo

- **Regla:** solo el agua. `Water → dAgua + carga[i] × LuzTurbidez / 255`, con `LuzTurbidez` registrado en `LabParams` (grupo LUZ, vía `R(...)` para panel y presets). Hielo y vidrio se quedan en el candidato 1, que ya toca `LabLuzDesde`, con la nota obligatoria de incluirlos en `dMin`.
- **Banco:** escenario nuevo `poza que decanta`, no el diluvio: cubeta de roca de 40×12 bajo una boca fijada en el montaje (como :164), agua con `carga` 120 (bajo el umbral de depósito, para que decante en vez de sedimentar). Métrica `LuzPoza`: `luz` y `carga` a profundidades 1, 3, 6 y 10 bajo la boca, a 0 / 1 500 / 3 000 ticks. Aceptación: `luz` a profundidad 3 sube monótona mientras `carga` baja. Invariante de aislamiento: `mat`, `humedad`, `carga` y `reposo` IDÉNTICOS con y sin el cambio (la luz no tiene consumidor en la poza); solo `HashLuz` se mueve. Esa igualdad demuestra que la ley multiplica sin tocar la física validada del agua.
- **Orden:** después de `ver-por-la-luz` (3). Sin ojo es un hash que cambia; con ojo, la poza que se oscurece con la riada y aclara al reposar es la primera consecuencia visible de la cadena de calidad del agua.

## Números corregidos

Apalancamiento **3** sola (5 con 1 y 3, como dice el documento). Tuning **9**: un coeficiente derivable (60/celda en turbia = 4 de alcance). Coste **0,5 semanas**: el código es una hora; escenario, métrica, hashes reapuntados y documentación son los dos o tres días reales.
