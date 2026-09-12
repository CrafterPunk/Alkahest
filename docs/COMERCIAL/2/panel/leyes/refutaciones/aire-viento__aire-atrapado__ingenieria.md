# Refutación de ingeniería · lente aire-viento · candidato `aire-atrapado`

**Veredicto: viable con ajuste.** La ley («el agua solo entra donde el aire pueda irse») es entera, de orden fijo y cabe en una función. Pero el mecanismo escrito no arranca y cuelga del sitio equivocado.

## Lo que refuto del mecanismo

1. **El enganche en `ProcessLiquid` sobra y para el mundo abierto.** Los cuatro caminos agua→aire (abajo, diagonales, `TryFlow` en `SimStepper.cs` l. 1341) pasan por `Move` → `SwapCells` (`CellGrid.cs` l. 264-279), que intercambia la celda entera: si `aire` viaja en el swap como `humedad`, el aire desplazado ya se conserva. La regla exige que `aire[j]` quepa ENTERO en UN vecino; con nominal 128 el hueco máximo es 127: en una atmósfera uniforme ninguna gota entraría nunca en aire. Corregida a «suma de huecos», deja vacío en el origen o duplica masa, y cada gota siembra sobrepresiones que la flotación del candidato 1 leería como viento. Fuera.

2. **El único teletransporte de aire a través de un cuerpo de agua está en la mudanza de `LabPresion`** (`Laboratorio.cs` l. 1141-1160: `SetCell(target, Water)`, `SetCell(src, Empty)`, `hum`/`temp` intercambiados por R131/R132). Por eso hoy toda campana se llena. Ahí, y solo ahí, va la ley.

3. **«Si no cabe, no mover» con el bucle actual es `break`** (l. 1131): `dst` es el mínimo global del cuerpo, y una rama sellada congelaría también las abiertas. Marcar `_labSup[s] = -1` y reintentar, idioma que el bucle ya usa.

4. **«El vecino de menor aire, si cabe entero»** falla en la primera mudanza de una bolsa uniforme a 128: campana al 0 %, no al 40-60 %. Repartir entre los tres vecinos aire de `target` (hueco sumado 381 ≥ 128); la resistencia llega sola cuando la media roza 255.

5. **La extensión térmica está sin especificar**: «se muda a un Empty ajeno a la bolsa» no dice qué celda ni cómo se sabe «ajeno» sin BFS. `AireBurbuja (16)` no mitiga nada: el aire de una bolsa es ≥ 128 por construcción.

6. **«El sifón necesita cebo» no lo compra este candidato.** `LabPresion` iguala superficies del MISMO cuerpo con desnivel ≥ `DesnivelMin`; la rama descendente cae por gravedad y parte el cuerpo en la cresta. Hay vasos comunicantes, no sifón.

## La versión mínima que sí vale

Una línea en la recolección de superficies (l. 1122-1130): **altura efectiva = cy + (P_encima − 128)·K**, con `P = aire·(2·temp+153)/(2·ambient+153)` (gas ideal en raw; el «+120» del candidato usa la escala equivocada: °C = 2·raw − 120). `src`/`dst` siguen siendo argmax/argmin. De ahí salen las tres cosas: campana con llenado parcial que DEPENDE de la profundidad hasta el tope de 255; bolsa caliente cuya superficie pasa a ser `src` y expulsa agua (la bomba); bolsa fría que aspira. En la mudanza: `aire[target]` repartido entre sus vecinos aire (rechazo y salto de superficie si no cabe); `src` toma la mitad de `aire[src+W]`. Bolsas de menos de `PresionMinCeldas` celdas (BFS acotado sobre `Empty`) no resisten. Parámetros: `AireNominal`, `AireDifusion`, `PresionAireK`, mínimo de celdas.

Dependencia real: el byte `aire`, su difusión conservativa (copia de `LabIntercambioVapor`, l. 391) y la fuente de cielo: el 40 % del candidato 1, y sobrevive si el 1 muere.

## Coste, tick, determinismo, banco

Determinismo intacto: enteros, orden fijo, sin sales nuevas. Tick: `LabPresion` corre cada 2 ticks y añade una multiplicación por superficie y tres lecturas por mudanza; nada en el hot path; en «diluvio turbio» espero < 0,05 ms. El coste oculto es la contabilidad de `aire` en quienes crean o destruyen `Empty` (`LabNacerAgua`, `LabTransformar`, `LabTragar`, `Transform`, expiración de gas): carga del candidato 1 que el 4 hace visible, porque una bolsa que pierde una unidad por goteo acaba en vacío y succiona sin razón. Se prueba con `Σaire` exacto, como `LabBalanceU`.

Banco: «campana» (vaso 10×10 bajo 30 celdas; nivel 40-60 % estable 3000-9000 ticks; con respiradero, 100 %); «U con rama sellada» (las abiertas igualan; la sellada queda `head` por debajo); «bolsa sobre hogar» (`LabPresionMovidas` de dentro a fuera ≥ N, con N dibujado por el banco: `LabHogar` tapa a 170 raw, la bolsa toca agua con `CAgua = 4` y el tirón a ambiente cada 32 ticks, así que «≥ 20 expulsadas» es apuesta, no dato). Alambique y arco largo: `LabGoteos` sin cambio (sus superficies comparten bolsa; la cabeza se cancela).

**Corregidos:** apalancamiento 7, tuning 7 (la campana no tiene números; la bomba tiene uno y su alcance lo decide la térmica existente), coste 1,5 semanas en solitario (1 si el candidato 1 ya está).
