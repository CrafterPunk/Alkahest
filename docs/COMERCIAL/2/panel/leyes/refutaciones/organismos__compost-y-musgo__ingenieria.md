# Refutación de ingeniería · organismos / compost-y-musgo

**Veredicto: VIABLE CON AJUSTE.** Compost y musgo encajan donde el candidato dice; la meteorización se cae por R55. Hay dos errores de lectura del código que, sin corregir, hacen fallar los dos benchmarks propuestos.

## Lo que el código confirma

- **Fibra ya pasa por `LabPoroso`** (`SimStepper.Laboratorio.cs` 221) y su `reposo` sube por visita (714) sin consumidor; `Move` (SimStepper.cs 742) lo reinicia al caer. El caso `Fibra` en el switch de 599-696 son 10 líneas calcadas del de `Arcilla`. `LabCampos` recorre todas las celdas cada 8 ticks (201-233) sin mirar chunks dormidos y `LabTransformar` despierta (260): R55 «despierto» sale gratis.
- **`carga` en roca impermeable está libre**: solo la escriben `LabMezclarCarga` (vecino `Water`, 488-489), `LabInfiltrarHacia` (porosos) y `LabCuerpos` (1157, viaja con el bloque). Sin RNG, sin campo nuevo.
- **La vista Carga ya pinta cualquier `carga > 0`** (SimRenderer.Laboratorio.cs 190-194): visor a coste cero; el tinte verde son ~6 líneas en `LabTinte` (26-70, sin caso para roca hoy). El sedimento ya se oscurece por `carga` (63-65).
- Coste por tick: un byte antes del `if (h == 0) return` de 776; con `if (carga[i] == 0 && h == 0) return` el camino rápido sigue en dos lecturas. 10-20 µs es creíble y «mundo entero despierto» lo mide.

## Error 1: la luz es un campo de superficie en los sólidos

`LabLuzDesde` (1262-1267) devuelve 0 para todo sólido: una celda de fibra solo recibe luz de un vecino aire/agua/planta. **El interior de una pila 10×10 tiene `luz = 0` aunque esté bajo el cielo.** Y `LabSecarHacia` (734-736) solo seca hacia `Empty`: el interior nunca se seca, pero sí se moja por `LabInfiltrarHacia` (443) y percolación (555-575). La pila al sol se compostará por dentro: el criterio «la del sol 0» es imposible. No es refutación, es mejor ley (extender fino para secar, amontonar para pudrir: el espesor decide), pero «la luz como interruptor» solo vale para la costra y el benchmark debe medir capa fina de 1 celda al sol contra montón a oscuras. Cruce no declarado: el sedimento nuevo nace con `h ≥ 100` y cuatro vecinos sólidos, así que la compostera acaba en **arcilla** por 621-632.

## Error 2: `LabGotear` tiene dos llamadores

El candidato gatea 777 (`LabRoca`). En el alambique el rocío llega a 255 por condensación, y `LabAire` llama a `LabGotear` en 383 al saturar. Gateando solo 777 el musgo no amortigua nada. Hay que gatear ambos y hacer que `h >= 255` con musgo caiga al secado de 780-783 en vez de `return`. Y corregir la afirmación: a 255, `cabe = 0` (371-372) y la roca deja de aceptar vapor; el musgo no es «condensador que no gotea», es un techo que bebe 255 u una vez y desvía el resto al bloque frío. «`LabEvaporado` mayor» no se sostiene en aire saturado; `LabBalanceU` cuadra igual.

## Lo que se cae: meteorización (R55)

Roca que suelta una fibra cada N visitas para siempre es materia de la nada: la roca no se gasta, el agua vuelve al aire y el único sumidero de materia es el sumidero. Exactamente lo que R55 prohíbe (y el espíritu de R60). Fuera de la versión mínima; si vuelve, cuesta la cobertura entera y el arco largo mide suelo total por hora.

## Raspar no existe

`Cincel.cs` 440-465 QUITA la celda tallable (`Paint(Empty)` para piedra). «Raspar» es un verbo nuevo: primer golpe sobre roca con `carga > 0` limpia sin tallar. Son ~8 líneas, pero es la única decisión que necesita a Cesar.

## Versión mínima y benchmarks

1. Compost: caso `Fibra` en `LabPoroso` con `FibraPudreVisitas`, `FibraAbono` (≥ 40 para `FertilU`), contador `LabCompostado`.
2. Musgo: en `LabRoca` antes de 776; gate en 383 y 777; fall-through al secado; tinte en `LabTinte`.
3. Cincel: golpe de limpieza si `carga > 0`.
4. Sin meteorización.

«Compostera»: pila PRE-MOJADA en el montaje (h = 200; probar la ley, no el mojado: humedecer 10 capas a ~2,8 u/visita tarda miles de ticks) y capa fina al sol como control; 9 000 ticks bastan así. «Musgo»: `MontarAlambique` tal cual (el techo ya es Stone), `LabGoteos` con musgo < 50 % del de referencia y `LabBalanceU` al bit. Arco largo: `LabCompostado − LabErosionado` por tramo, debe converger.

**Corregido: apalancamiento 7, tuning 7, coste 1,5 semanas.**
