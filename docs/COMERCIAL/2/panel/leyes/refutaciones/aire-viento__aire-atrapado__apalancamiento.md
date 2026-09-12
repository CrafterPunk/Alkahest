# Refutación · `aire-atrapado` (lente aire-viento, candidato 4) · eje: apalancamiento

**Veredicto: viable con ajuste.** La idea (una bolsa de aire sellada que resiste al agua y que el calor
dilata) es un cruce real de presión × geometría × temperatura. Pero la regla escrita no funciona, la
mitad de sus decisiones no son nuevas o dependen de lentes inexistentes, y el candidato 1 sobra.

## 1. La regla local está rota en el valor nominal

Dice: antes de mover agua a un `Empty` `j`, buscar entre los vecinos aire de `j` el de menor `aire`;
«si cabe `aire[j]` entero, transferirlo y mover; si no, no mover». El aire nace a 128 y el byte tope es
255: **128 + 128 = 256, en aire nominal nunca cabe.** En un pozo de una celda de ancho (tubo en U,
sifón, el pozo sala→arroyo de `SimLevelBuilder.Laboratorio.cs` l. 78) el único vecino libre de `j` es el
de abajo, a 128: el agua no cae. Repartir entre vecinos no lo arregla: con dos de ancho la segunda
celda queda a 192 y la tercera a 224+; el frente avanza una celda por tick y el único alivio sería la
difusión del candidato 1, cada 8 ticks y `diff/(2·D)` (`LabIntercambioVapor`, Laboratorio l. 383-395),
órdenes de magnitud más lenta. **El agua se atasca en cualquier conducto estrecho, abierto o cerrado.**
Y `LabPresion` (l. 1129-1140) pone el agua en `dst + W`, cuyos vecinos son bolsa a 128: la ley mejor
medida (5/5) dejaría de mover una celda. «`alambique` con `LabGoteos` sin cambio» cae igual: la gota de
`LabGotear` baja por `ProcessLiquid` hacia un `Empty` a 128.

La raíz no es un número: **abierto o sellado es propiedad de la región conexa de aire, no de los
vecinos de `j`.** Es el problema que `LabPresion` ya resolvió para el agua con un BFS. La «bomba de
Herón en tres líneas» lo confiesa: «mudar el agua a un `Empty` ajeno a la bolsa» exige saber qué
celdas son la bolsa.

## 2. Cruces y decisiones inflados

De las cinco decisiones: «purgar un sifón» **ya existe** (`LabPresion` sube el tubo hasta el nivel de la
fuente menos `DesnivelMin` = 3; un codo más alto jamás se ceba solo). «Bucear con campana» necesita
la lente del cuerpo (no hay respiración en el sim). «Subir agua con fuego» necesita la extensión
térmica no local. Quedan dos: el respiradero (que el documento lista como riesgo mayor: un modo de
fallo, más que una decisión) y sellar una cámara. Cruces con leyes existentes: presión (la restringe,
no la multiplica) y temperatura (solo con la extensión). Luz, plantas, erosión, turbidez, humo: cero.
«El alambique sellado aspira» es falso: el vapor no entra en `P` y condensar no resta `aire`.

## 3. El tuning escondido

El «nivel interior entre 40 y 60 %» presupone una escala «celdas de columna de agua por unidad de
`aire`» que ninguna medida fija; `AireEmpuje` decide si un hogar levanta 3 celdas o 30, y solo el
playtest lo juzga. Súmense `AireBurbuja`, el umbral de 6 y los cinco números del candidato 1, cuyo
riesgo mayor (circulación ≈ 0) mataría también a este.

## 4. Versión mínima con más apalancamiento (el ajuste)

Sin campo `aire`, sin candidato 1, todo en `LabPresion`:

1. **`LabBolsas()`**: BFS 4-conexa sobre `Empty`/gas reutilizando `_labVisita/_labCola` más un
   `_labRegion` (`int[]`): id, tamaño, media de `temp` y flag `abierta` (toca la fila del cielo
   `LuzCieloX0..X1`). Corre cada `PresionCadaTicks`; el aire del plano ronda 40 k celdas: 0,1-0,2 ms,
   medido en «mundo entero despierto».
2. La superficie `c` lleva la región de `c + W`. Destino válido si comparte región con la fuente, si
   su región es abierta, o si es sellada de ≥ 6 celdas y **fría** (`T_reg < ambient − UmbralFrio`:
   succión). Fuente bajo bolsa sellada **caliente** suma cabeza `(T_reg − ambient)/K` celdas:
   expulsión. `ProcessLiquid` no cambia (por gravedad el agua solo entra hasta la boca).

Es «el aire atrapado no se comprime; se dilata o encoge con el calor»: campana al 100 % de aire, rama
cerrada que no iguala, cámara que solo se inunda hasta el túnel, y la bomba de dos tiempos (hogar a
170 raw expulsa; retirar el fuego o un núcleo frío rellena), lo único de esta lente que ataca «nada
produce sin volver a tocarlo». Escenario «campana»: vaso 10×10 bajo 30 celdas, aire interior estable
3 000-9 000 ticks; con hogar debajo ≥ 15 celdas expulsadas; con respiradero, lleno. Los bolsillos
ocultos del plano (l. 74-75) son secos y nunca destino: hashes intactos salvo evidencia.

Tuning: dos números (`K`, `UmbralFrio`); la curva la dibuja el banco con el pozo de 27 celdas de
referencia. Apalancamiento 5: dos cruces reales y una máquina. Coste 0,75 semanas, solo.
