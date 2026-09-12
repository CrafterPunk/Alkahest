# Refutación · organismos / compost-y-musgo · lente APALANCAMIENTO

**Veredicto: viable con ajuste.** Sobrevive un tercio del candidato (el compost, sin su interruptor de luz); el musgo y la meteorización se refutan con el código en la mano.

## 1. El «interruptor de luz» no existe dentro de una pila

`LabLuzDesde` (SimStepper.Laboratorio.cs 1270-1273) devuelve 0 para todo sólido: la luz no atraviesa la fibra. `LabLuzDecay` (1276-1284) da a los sólidos `dAire` = 8, así que solo la PIEL de una pila recibe luz; la segunda capa ya está a 0. En una pila 10×10 al sol, 64 celdas interiores cumplen `luz < 40` igual que la tapada: el criterio «la del sol da 0 sedimento» falla por construcción. Lo que sí separa las dos pilas ya está escrito: `LabSecarHacia` (734-750) exige vecino `Empty` y aire no saturado; una capa fina en aire seco se seca, un montón queda mojado por dentro (percolación ≈ 2 u/visita, capilaridad 4). La decisión real es EXTENDER o AMONTONAR, VENTILAR o SELLAR, y no cuesta un parámetro.

## 2. El musgo es una válvula cerrada, no un amortiguador

`LabGotear` se dispara en DOS sitios: `LabRoca` 777 y la condensación de `LabAire` en 383. El candidato solo bloquea 777. Bloqueados los dos, la roca queda a 255, `cabe` (376) es 0, el vapor se queda en el aire, el aire satura y `LabSecarHacia` devuelve `h` sin tocar nada (`deficit <= 0`, 740): «la roca suelta por secado» no ocurre nunca en un alambique, que por definición tiene el aire saturado. `LabEvaporado` BAJA, no sube. El musgo no tiene capacidad propia (`humedad` ya es 0-255 en roca; CellGrid 184): no retiene, cierra. Y como todo techo de alambique es oscuro y con rocío (R148), TODO alambique cría musgo y se atasca solo: la única máquina que corría sin tocarla pasa a exigir raspado. Es la anti-tesis de SOLTAR; «raspar o dejar» es mantenimiento binario, no una decisión que las leyes juzguen.

## 3. La meteorización viola R55 por construcción

Fibra a partir de roca + rocío sin consumir nada: la roca no se gasta y el agua de un alambique sellado es conservada. Fuente infinita de fibra → sedimento a coste cero, la «ganancia neta por evento frecuente» que R55 prohíbe y la roca rindiendo material a granel que R60 veta. El «riesgo mayor» del candidato audita solo el camino planta (120 u de agua destruidas en 878 por celda), no este.

## 4. Observabilidad y tuning, recontados

`SimRenderer.Laboratorio.cs` 90-95 ya oscurece la roca con rocío (`h > 40`) y la fibra mojada (52-58); el rótulo dice «con rocío» a ≥150. El «higrómetro» es un tinte verde sobre un canal que ya se ve. Parámetros nuevos: seis, no cuatro (`FibraPudreVisitas`, `FibraAbono`, `MusgoRocio`, `MusgoLuzMax`, `MusgoRetiene`, `MusgoCaeVisitas`); los dos últimos deciden si cada cámara del jugador se atasca y a qué ritmo fabrica suelo de la nada: balance contra geometría arbitraria. Coste real del candidato entero (dos sitios de goteo, cincel, renderer, meteorización, auditoría de suelo): 1,5-2 semanas.

## Ajuste mínimo (la versión que sí compra juego)

Un solo `case MaterialId.Fibra:` en el switch de `LabPoroso` (599): `if (h >= FibraMojadaMin && reposo[i] >= FibraPudreVisitas) LabTransformar(i, Sedimento, h, PlantaAbonoMuerte)`. Sin luz, sin musgo, sin meteorización. `reposo` ya cuenta visitas (714) y `Move` lo rompe (SimStepper.cs 741); `LabTransformar` conserva el agua y despierta el chunk. Un parámetro nuevo (~120 visitas ≈ 32 s), abono reutilizado. Cruza cuatro leyes existentes: secado/saturación (decide dónde se pudre), `FibraMojadaMin` (mojar la leña se vuelve irreversible), erosión (por fin hay fuente contra el sumidero de R134) y fertilidad (`carga` → `PlantaFertilidadBonusPct`, 826). Decisiones nuevas: extender o amontonar; sellar o ventilar el almacén; fabricar suelo en una cámara sin sedimento con fibra y un goteo; el hogar como secadero (la tasa sube con `temp[i]`). Banco: «compostera» con una capa de 1 celda y un montón 10×10, regados en cámara sellada: el montón da sedimento, la capa sigue prendiendo; el arco largo cuenta sedimento total por hora. Tuning escondido: con manantial la fuente no es mortal (el agua se repone); la cota es el área iluminada que las plantas necesitan, y eso se MIDE, no se afina. Si el suelo diverge en el arco largo, el candidato vuelve aquí.

**Corregido:** apalancamiento 5 (versión mínima; el candidato entero, 3) · tuning 8 · coste 0,5 semanas.
