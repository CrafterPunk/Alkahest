# Refutación de ingeniería · `vidrio-transparente-que-suda` (lente materiales)

**Veredicto: viable con ajuste.** Las cinco líneas caben donde el candidato dice, no tocan RNG ni el presupuesto por tick, y el banco puede probarlas. Se refuta la mitad de lo que prometen y tres detalles de la regla escrita.

## 1. Lo que el código confirma

- `LabLuzDesde` (`SimStepper.Laboratorio.cs:1269-1273`) y `LabLuzDecay` (1276-1285) son un `if` por material: `VidrioVerde` es una comparación más, sin sal. `LabLuz` corre cada 16 ticks y cuesta 0,05-0,32 ms (R142).
- `LabCampos` (201-232) no tiene caso para `VidrioVerde`; `EsRocaImpermeable` (`LabMateriales.cs:95`) solo lo leen `LabEsSuperficieCondensable` (278) y el goteo de `LabAire` (383). `Estado` ya rotula «con rocío» a todo no poroso con humedad ≥ 150.
- El humo solo se mueve a `Empty` (`ProcessGas`, `SimStepper.cs:1443+`): la separación luz/humo es real.
- Mover NO es el riesgo mayor: el pincel pinta vidrio desde R140 (`LabPanel.cs`) y el frasco lo aspira hoy (`Flask.EsAspirable`, 521-531: no es Stone/Piso, no es `EsSolidoDelMundo`, no es Fire) y lo vierte con `PaintCell`. Cae con cohesión 3 (`SolidoTieneApoyo`, `SimStepper.cs:1378-1398`): pilar cada 7.

## 2. Lo que se refuta

**a) La regresión anunciada es falsa.** `LabK`/`LabC` (1357-1385) pasan el vidrio de `KPolvo 3 / CPolvo 2` a `KRoca 2 / CRoca 3`. En «horno con yesca» nace vidrio, así que cambia `HashTemp`, no solo `HashLuz`; y como la columna vidria de abajo arriba con `min(ki,kj)`, un tercio menos de conducción hacia la arena de encima puede reiniciar `reposo` (`LabPoroso`, 604-617) y mover `HashMat`/`HashReposo`. Peor: puede romper el 18/18 que Cesar validó. Es la línea menos cargada de las cinco; va aparte, con flag, medida contra el 18/18.

**b) La ventana de H5 se rompe.** `LabLuz` deduce el alcance horizontal de `dMin` (≈1218-1224) sobre aire/agua/planta/humo. Con `LuzDecayVidrio` 6 < 8, una galería de vidrio lleva la luz más lejos de lo que la ventana barre y se corta en `bx1` sin aviso. Una línea: `if (LuzDecayVidrio < dMin) dMin = LuzDecayVidrio`.

**c) El invernadero no invierte R148/R150 por sí solo.** La oscuridad de R148 era la sombra del serpentín sobre la boca (`NucleoFrio` es opaco), y el vidrio no atraviesa un serpentín. Con la luz resuelta, R150 mide que el huerto muere de REPARTO de riego (humedad de cara 50-99 contra mínimo 60): un techo que desvía el goteo a un canal lo concentra más, no menos. El aserto «plantas vivas > sin vidrio» es ruido (0 contra 0-1). Lo que el vidrio sí compra y el banco sí mide: luz del aire bajo el techo ≥ 40 con humo encima, y humedad media del lecho sellado > abierto (la transpiración vuelve en vez de irse por la chimenea).

**d) Alfa 130 viola R19** y en una grilla 2D no hay nada detrás: se vería el ladrillo del fondo como un agujero. Tinte claro con alfa 255, y `VidrioVerde` junto a `Stone`/`Terracota` en `LabTinte` (`SimRenderer.Laboratorio.cs:84-90`) para el sudor. La observación real es la vista Luz y las plantas.

**e) Techo plano = charco.** El goteo sobre un techo horizontal forma una lámina de agua que resta 20 por celda (`LuzDecayAgua`): hace falta pendiente en escalera. Y `LabGoteos` no distingue vidrio de roca: contador propio en `LabGotear`.

**f) Rareza heredada:** `SolidoTieneApoyo` cuenta como apoyo todo lo que no sea `Empty`, humo incluido. Un techo sin pilares se sostiene sobre el humo y cae cuando el humo muere. Clip o bug: el escenario lo fija con un aserto de techo intacto.

## 3. Ajuste mínimo

`LabLuzDesde` + `LabLuzDecay` + `dMin` + `case VidrioVerde: LabRoca` + `EsRocaImpermeable` + tinte sin alfa. K/C aparte, con flag. Gate de `EsSolidoDelMundo` por `ModoLaboratorio` solo en `ApprenticeController.cs:924,1328` (no en Cincel ni Flask: el frasco debe seguir moviéndolo). Escenario «invernadero»: nivel de referencia, techo en escalera x100-135 desde y262 con pilares cada 7, serpentín de R150 (x118-135, fuera de la boca), fuego de sala de `MontarArcoLargo`, y un gemelo sin techo. Asertos: luz media del aire bajo el vidrio ≥ 40 en toda muestra con humo en la cámara y < 40 en alguna del gemelo; goteos-desde-vidrio ≥ 1; humedad media del lecho sellado > gemelo a 9000 ticks; techo intacto; los 8 hashes que no son «horno» idénticos, y «horno» con `LabVidrio == 18` antes y después.

**Corregidos.** Apalancamiento 7: el único sólido transparente es un eje nuevo casi gratis y la puerta de la lente óptica, pero no hace vivir el huerto (eso es riego, R150). Tuning 8: un número fijado por cuenta; el resto es geometría del jugador. Coste 1 semana: las líneas son un día; el escenario con pendiente y pilares, el gemelo, la relínea del horno, la decisión K/C, el gate del muñeco y la documentación R15/R32 son el resto.
