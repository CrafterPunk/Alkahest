# Refutación · `adveccion-calor-vapor` (lente aire-viento) · eje: apalancamiento

**Veredicto: REFUTADO como ley aparte.** No es capa encima ni pide contenido de autor; cae por
redundancia (la mitad de calor ya la hace el motor), por magnitud (a la escala que el propio
candidato escribe, el efecto es indistinguible de «no pasó nada», R43) y porque su única mitad
real, el vapor que viaja con el aire, no es una ley: es una deuda del candidato 1.

## 1. El calor ya viaja con la masa visible

`CellGrid.SwapCells` (l. 264-279) intercambia `temp` y `humedad` con la sustancia, y `Move`
(SimStepper.cs l. 738-740) pasa por ahí. Cada celda de humo nace en un vecino recién calentado
por `LabInyectar` (l. 871) y sube con su raw y con `KGas = 6`. Si el candidato 1 consigue que el
gas siga al flujo, el hipocausto, la serpentina «que aprovecha el calor» y la cámara alta del arco
largo que se entibia ya ocurren sobre código existente. El 2 añade una segunda copia del mismo
transporte, invisible, sobre celdas `Empty` que no se mueven.

## 2. La escala escrita no produce lo que promete

El término es `Δ·t/128` con `t` unidades de aire por visita. En el 1, el transporte horizontal es
`diff_aire/8` sobre un byte centrado en 128: en un conducto en régimen `t ≈ 1-3`, es decir, un
1-2 % de la diferencia por visita. Enfrente, la conducción aire-aire ya mueve `d·KAire/(64·CAire)
= d/16` (LabParams l. 111-112, `LabDifusionTermica` l. 1332) y `d/8` hacia arriba con
`Conveccion = 1` (`LabFlujoTermico` l. 1355): entre 4 y 8 veces más que la advección y en todas
direcciones. Un conducto de una celda pierde a dos paredes de roca `d·min(4,2)·2/64 = d/16` por
visita y 1 raw cada 4 visitas al ambiente (l. 1311-1316, 1337-1341). Encima, la división entera
trunca a cero mientras `Δ·t < 128`: con `t = 2` hacen falta 64 raw (128 °C) de diferencia para
mover UN raw. El frente se para a dos celdas del hogar. La «lengua de calor que sigue al viento»
es un sesgo de 10-20 % sobre un blob isótropo que el visor Temperatura no distingue. Hacerla
visible exige una ganancia que las «diez líneas» no tienen, y esa ganancia se balancea contra
`KAire`, `Conveccion`, `TiroAmbienteTicks` y contra `AireFlota`/`AireDifusion` del 1 (cuyo plan B,
subir `AireFlota`, recalibra el 2 sin que nadie lo pida). Ahí está el tuning escondido.

Cerca del fuego pasa lo contrario: `exceso·AireFlota/64` con exceso ≈ 100 da `t ≈ 6-12`, y la
advección vertical (`d/20..d/10`) se suma al sesgo de `Conveccion` que ya modela lo mismo. Doble
conteo donde hay flujo, nada donde no lo hay.

## 3. `Conveccion = 0` es una regresión, no un ahorro

Con el 1, una caja caliente sellada se estratifica y `t → 0`. Si `Conveccion` baja a 0, el
horno-recinto (vidrio 18/18 con ≥200 raw sostenidos 60 visitas) y el alambique pierden el «techo
antes que el suelo» del que fueron calibrados: la masa modela peor que la heurística justo en el
recinto cerrado. Se queda a 1, el doble conteo se mide, y los nueve hashes se re-verifican. Coste
no presupuestado.

## 4. Cruces y decisiones, contados

Cuatro cruces declarados, uno real. Térmica: ya la da el humo caliente (§1). Plantas: `LabPlanta`
(l. 801-906) no lee `temp`; germina por `humedad` del suelo ≥ 60 y transpira por `LabSecarHacia`
(l. 849-856). No hay consumidor de «tibio»: el invernadero no existe, y lo que mata al huerto
(R135/R148) es agua líquida en columnas. Agua y suelo son el MISMO cruce («el vapor viaja con el
aire»: `LabSecarHacia` l. 731-751 y la condensación de `LabAire` l. 355-386 leen `hum[j]`), contado
dos veces. Y ese cruce no es ley nueva: el 1 promete «el aire renovado seca a sotavento» moviendo
`aire` sin mover la `humedad` de `Empty`; sin esta mitad, la promesa del 1 es falsa. Cuatro
decisiones declaradas, media nueva: hipocausto y recta-vs-serpentina son del 1 (§1); «frío aguas
abajo» es la decisión de siempre del alambique con otra respuesta; el secadero de dos bocas es la
única propia, y es de la mitad de vapor.

La verificación no discrimina: «≥ ambient+8 con conducto» pasa con 1 + humo caliente + conducción
sin una línea del 2, y «= ambient sin conducto» falla porque `KRoca = 2` atraviesa el muro. En
«hervidero» el goteo lo acotan `CondensaRate = 24` y el contacto con el frío (regla local del
vecino más frío), no el transporte: «+30 %» puede fallar con código correcto.

## 5. Ajuste mínimo

Plegar la mitad de vapor en el candidato 1 (su entrega B): en cada transferencia de `aire` de i a
j, `hum[j] += hum[i]·t/aire_i`, restando de i, guarda `aire_i == 0`, recorte a `255 − hum[j]` con
el resto en i, `LabBalanceU` intacto. Nada de `temp`; `Conveccion = 1`. Un brazo más en «chimenea
con boca 3»: fibra mojada a sotavento seca antes que a barlovento; «diluvio» y «tolva» con hash
intacto porque `t = 0`. Si después el 1 mide `Σvy > 0` y el hipocausto no aparece con el humo
caliente, entonces se propone advección de calor con ganancia declarada y banco de tres montajes
(conducto abierto / tapado con una celda de roca / sin conducto; `abierto − tapado ≥ 8 raw`).

**Corregido (candidato tal como está):** apalancamiento **2**; tuning **5** (ganancia escondida,
acoplada a cinco parámetros; decisión `Conveccion` que re-verifica vidrio y alambique); coste
**0,8 semanas** (brazo de control, dither, recalibración y rebase). El ajuste: +1 sobre el 1,
tuning 8, 0,2 semanas.
