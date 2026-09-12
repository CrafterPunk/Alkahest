# Refutación · `fase-con-reserva` (lente agua-fases, candidato 1) · eje: apalancamiento

**Veredicto: viable con ajuste.** La mecánica es real, entera, sin dado y verificable en banco; lo
inflado es el apalancamiento. De los cinco cruces declarados ninguno es nuevo: tres existen hoy en
forma instantánea, uno se refuta a sí mismo y el quinto cita un síntoma que tuvo otra causa. Lo que la
ley compra es una sola cosa: **convertir un cambio de estado binario en una cantidad proporcional a la
masa** (el tapón grueso tarda más que el fino). Eso vale 5, no 9.

## 1. Cruces contados contra el código

1. **Evaporación bajo tapa de hielo: ya existe.** `ApplyPhase` (SimStepper.cs:650) hiela el agua a
   ≤ 60 raw en el mismo tick y `LabAgua` paso 1 (Laboratorio l. 413) ya exige `mat[up] == Empty`. Un
   núcleo frío sobre un estanque lo sella hoy; «`LabEvaporado` = 0 bajo tapa» pasa con el código actual.
2. **Válvula térmica: ya existe, y la flotación la rompe.** El BFS de `LabPresion` (l. 1120-1126) solo
   encola `Water`: una rama helada ya corta el cuerpo y se abre al fundirse a 62. `SolidoTieneApoyo`
   (SimStepper.cs:1382) ya trata el agua como apoyo («el hielo sigue flotando», l. 1401). El `Move`
   propuesto hace subir una fila por tick a toda celda de hielo con agua encima: un tapón en un
   desagüe o a media rama es expulsado a la superficie por el propio `HieloFlota`.
3. **Olla a 110 raw: ya existe.** `boilsAt` vuelve vapor el agua en el tick en que llega a 110
   (SimStepper.cs:655); «temperatura tras la olla ≤ 110» pasa hoy. `LatenteVapor` (16-32 raw) sobre 40
   de calentamiento es un 40-80 % más de duración, no un cortafuegos nuevo.
4. **Los perdigones del alambique se refutan solos.** `LabGotear` (l. 321) hace nacer la gota con la
   `temp` del núcleo (30 raw) y `ApplyPhase` la hiela al tick siguiente: hoy graniza, sin medir (el
   banco no cuenta `Freeze`). Con la regla propuesta la gota acumula 30 < 48 en su primera visita,
   queda clavada a 60 y se templa: **la ley elimina el granizo que presenta como cruce.**
5. **«El agua me sigue saliendo congelada»: causa cerrada.** HISTORIAL_RONDAS.md l. 701-720:
   `Dispenser.EmitTick` usaba `Paint` en vez de `PaintStable` (reglas 22/29). No hubo roce de frío;
   hubo materia nacida a temperatura vieja. Citarlo como evidencia recae en lo que la regla 30 prohíbe.

Cruces nuevos: **cero**. Cruces existentes que ganan duración proporcional a la masa: tres.

## 2. Lo que la regla calla y decidirá el playtest

- El acumulador de frío (`aux` bits 1..7) no se devuelve al templarse: una gota que rozó el núcleo
  guarda 30 de 48 para siempre y se hiela al siguiente roce. Es la clase «algo se congeló solo» que
  el playtest 17 erradicó.
- `LatenteFusion` es «raw·celda», pero el agua tiene volumen (`humedad`): una película de 20 u cuesta
  lo mismo que una celda llena y produce un sólido entero.
- Anular `freezesAt/boilsAt` calla `PushEvent(Freeze/Boil)`: audio (DirectorDeAudio.cs:1266) y
  SubstanceKnowledge (l. 1449) dejan de enterarse.
- Dos números contra `KRoca`, `CAgua`, `FrioPotencia` y `TiroAmbienteTicks` deciden si un tapón de 3
  celdas a 5 del hogar cede en 6 s o en 60. El banco mide el tick; solo Cesar dice si es apuesta.

## 3. Versión mínima con más apalancamiento (el ajuste)

Solo `LatenteFusion`, en las dos direcciones:

1. `LabAgua` paso 0 como está escrito, con dos correcciones: el acumulador **se devuelve** cuando
   `temp > 60` (`acum −= temp − 60`, sin clavar) y el coste escala con el volumen
   (`LatenteFusion · hum / 255`). Al completar, `PushEvent(Freeze)` y `LabTransformar(i, Ice, hum, 0)`
   con `aux = reserva`.
2. `case MaterialId.Ice: LabHielo` en `LabCampos` (l. 216) como se propone; `aux == 0` → agua a 62 con
   la `humedad` que guardaba el hielo. `LabPresion` copia `aux` (l. 1148).
3. **Sin `LatenteVapor`** (`Latente` = 4 se queda; 110 ya es el tope) y **sin `HieloFlota`** (el hielo
   ya no se hunde; subir solo saca tapones).

Banco: antes de tocar nada, contar `Freeze` en «alambique» (dice si hoy graniza); identidad
`LabRawCongela == LabRawFusion` en caja adiabática; tick de fusión de tapones de 1, 2 y 4 celdas a 5
del hogar **lineal en celdas** (esa es la promesa del calorímetro, no «≤ 110»); hash de «laboratorio
base» intacto; «mundo entero despierto» dentro de +3 %.

Apalancamiento 5: ninguna decisión nueva, tres existentes que pasan de interruptor a cantidad. Tuning
7: un número, medible en banco, juzgable solo en Play. Coste 1 semana, con eventos y vista de reserva.
