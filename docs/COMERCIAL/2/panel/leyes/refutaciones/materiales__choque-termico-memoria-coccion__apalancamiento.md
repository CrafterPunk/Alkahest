# Refutación: choque térmico con memoria de cocción (lente materiales, foco apalancamiento)

**Veredicto: viable con ajuste.** La regla tal como está escrita se refuta con los números del propio código; sobrevive una versión más pequeña que cruza lo único que el motor sabe distinguir. Apalancamiento honesto 4 (declarado 8), tuning 7, una semana.

## 1. El guardián (montaje a) falla por construcción

`LabHogar` (SimStepper.Laboratorio.cs:917-944) clava su celda a `HogarRaw` 170 y suma `HogarCalor` **40 por visita** a cada vecino hasta 170: el suelo de terracota de la olla está a 170 en tres visitas (24 ticks) y la memoria fija `reposo` = 170, umbral 70. El agua de encima sube por `LabDifusionTermica` (1306-1345) con `k = min(KAgua 8, KArcilla 2) = 2`: `flujo = 100·2 = 200`, `step = 200/(64·4) = 0`, forzado a **1 raw por visita**. El suelo gana 100 raw en 24 ticks; el agua, 1 cada 8. La Δ se queda ≥ 70 durante unas 25 visitas del suelo, cada una con tirada `ChoquePct`: con un 10 % la olla «con agua desde el tick 0» se raja en ~7 s. Y en hervor tampoco se salva: el agua líquida es siempre **< `boilsAt`** (`ApplyPhase`, SimStepper.cs:655, la vuelve `Steam` en `t >= boilsAt`; el laboratorio lo clava a 110 en Universe.Laboratorio.cs:212), así que la celda pegada a la pared pasa por 100-109 mientras se calienta, y 100 + 70 ≤ 170. El «riesgo mayor» declarado ocurre por diseño.

## 2. Por qué ningún umbral lo arregla: el sustrato no tiene velocidad

La difusión mueve 1-4 raw por visita en sólidos y el hogar re-clava cada 8 ticks. Un «choque» (gradiente rápido) no tiene representación; lo único representable es **contacto estático sólido caliente / líquido frío**, que es el estado normal de toda olla sobre todo hogar. (a) y (b) son la misma situación térmica: la diferencia es cuándo llegó el agua, y la temperatura no guarda eso. La decisión «agua primero o fuego primero» no la ve la física.

## 3. El temple no protege lo caliente

Con agua ≤ 109, el umbral máximo (reposo 255) es 112: 109 + 112 = 221, una pieza a 222 se raja con cualquier agua; cocida en horno a 220 el umbral es 95 y la Δ mínima 111: se raja. El temple solo actúa en la banda 130-200 y solo se lee con F8. Además la terracota nace en su sitio (`LabPoroso`, 641-655) y `Mudanza` mueve máquinas, no celdas: «cocer en horno según el uso» es un espejismo.

## 4. Sobre qué material actúa

Terracota no existe en el nivel de referencia ni en ninguno de los nueve escenarios: `SimLevelBuilder` y `LabBench` no la pintan, el horno de `MontarHorno` es `Stone`, la caldera del alambique es agua directa sobre el hogar. Se obtiene solo por sedimento → arcilla (200 visitas quieto, humedad 100-230, 4 vecinos, 2 % por visita) → desenterrar → secar ≤ 30 → cocer, y CHECKPOINT H3.4 midió que solo cuecen **las 16 celdas de la cara**. La ley actúa sobre una piel de un píxel hecha sobre el hogar; «la bóveda del horno se raja» exige un horno de terracota que nadie ha construido. Los nueve hashes quedan intactos porque la ley no se ejecuta ni una vez: la regresión no la prueba.

## 5. Cruces contados

De cinco declarados sobrevive uno y medio: **agua que llega** (manantial a `FuenteTempRaw` 70; gota de `LabGotear` que nace a la temperatura del núcleo frío, 30) contra cocido caliente. Demolición a cubos: la misma `Grava` que `ProductoDeTalla`, clip. Vidrio: depende del candidato 1. Cuerpos: especulativo. Amplificador no modelado: `Terracota` es `caeSolido` con cohesión 6 y `VidrioVerde` cohesión 3 (`ProcessSolidoCohesion`); una celda vuelta grava puede dejar sin apoyo la voladiza entera. La olla no se raja: se derrumba. Clip o rabia.

## 6. La versión mínima con más apalancamiento

1. La puerta no es Δ sino **llegada**: `reposo[j] < ReposoMovil` (3) en el agua vecina. `Move` pone `reposo = 0` (SimStepper.cs:742) y `LabAgua` lo incrementa por visita: es la única distinción que el motor sí hace, agua nueva contra agua asentada. La olla llena desde el principio es inmune pase lo que pase con el transitorio; el relleno frío y la gota, no.
2. `ChoqueRaw = TerracotaRaw` (150): un parámetro menos.
3. `ChoqueDelta = 80`: excluye 109 contra 170 incluso con el agua en movimiento por el hervor.
4. Guardar la máxima en `reposo` (una línea, `Estado` la lee), pero **sin `ChoqueTemplePct`** en v0.
5. Montaje (a) arranca en frío con el transitorio real; (b) llena de golpe; (c) sobra. `VidrioVerde → Sand` solo con el candidato 1.

## 7. Tuning que esconde

La frontera «se raja porque sí» vive en cinco parámetros viejos (`HogarCalor`, `HogarRaw`, `FrioRaw`, `FuenteTempRaw`, `KArcilla`) más la cohesión; con la puerta de llegada se reduce a `ChoqueDelta`, que (a)/(b) fijan por cálculo. Queda una lección («no eches agua nueva a lo cocido caliente») y un accidente sobre un material raro: apalancamiento 4, tuning 7, una semana, y detrás del candidato 1.
