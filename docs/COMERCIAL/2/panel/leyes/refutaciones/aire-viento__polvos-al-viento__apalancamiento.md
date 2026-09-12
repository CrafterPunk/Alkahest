# Refutación · aire-viento / `polvos-al-viento` · lente APALANCAMIENTO

**Veredicto: REFUTADO tal como está escrito.** La regla es barata, determinista y encaja donde dice (`ProcessPowder`, `SimStepper.cs` 1077-1084), pero ninguna de sus cuatro decisiones nuevas puede ocurrir con el código que hay, su cruce estrella no tiene línea que lo cumpla (R49) y su propio benchmark falla por construcción.

## 1. Lo que vuela nunca está en el aire

La brasa nace EN LA CELDA del combustible agotado (`ConvertirEnBrasa`, 987-993, desde 931); la ceniza, en el sitio de la brasa (1052), de la llama muerta sobre sólido (1693) o de la sordina (929). Los tres caminos de `ProcessPowder` van hacia abajo (1080-1115) y la regla lo respeta: «solo mientras cae; nada se levanta». Una brasa de hogar cae hacia dentro de la pila cuando se consume la celda de abajo, y ahí termina. Cuarenta celdas de `vy` en la chimenea no tocan un polvo que jamás sube: «la chimenea escupe brasas» es prosa. Solo atraviesan corrientes los polvos que suelta el jugador y la fibra de la tolva.

## 2. El viento que necesitan no existe

Fuera de conductos, el `viento` del candidato 1 es cero: difusión más flotación se igualan a 128 en el hueco abierto; hay flujo neto solo en el pasillo boca-del-cielo → hogar y en la columna caliente. La boca del cielo mide 7 celdas (`LuzCieloX0..X1` = 118..124). La lente descarta el «viento exterior constante» (l. 213-215); sin él, «incendio a favor del viento», «cortafuegos a sotavento» y «sembrar contra el viento» no tienen dónde ocurrir. El fuego ya se propaga por contacto isótropo (`TryIgnite` 1763-1800, `BrasaReencenderPct` 1038-1045).

## 3. Cruces declarados: cuatro; reales: uno a medias

- **Fuego**: solo si el combustible arde SUSPENDIDO sobre una corriente (tolva, tejado ya ardiendo). La carbonera de la primera decisión no produce brasa: en sordina el residuo es carbón o ceniza (921-931). «Techar la carbonera con roca» no decide nada.
- **Suelo**: la ceniza abona solo mojada y pegada a sustrato (Laboratorio.cs 658-679). «Enturbia el agua» no existe: la turbidez es `carga` de erosión o de manantial, y la ceniza (120) se hunde en agua (110).
- **Plantas**: nada crea `Semilla`; la pinta el jugador y germina donde para (684-692). La planta muerta sí deja `Fibra` (811, 899; el sustrato dice lo contrario y el código manda), pero bajo el cielo, sin viento. La dispersión tiene su candidato en `organismos`; aquí es cobro por adelantado.
- **Agua**: `LabCombustibleMojado` (`TryIgnite` 1771) ya existe.

Es una capa que LEE `viento` sin devolver nada: no toca conservación, sordina, luz ni humedad.

## 4. El tuning que esconde

Densidades: fibra 60, semilla 90, brasa 110 = carbón 110, ceniza 120, sedimento 150, arena 180; `vx` es un nibble de siete niveles. `VientoArrastre·density/64` con V=2 da fibra 1, semilla 2, brasa y carbón 3, ceniza 3, sedimento 4, arena 5. Ningún valor hace volar la brasa sin el carbón ni levanta ceniza a 6 sin desviar arena a 5: una constante contra nueve densidades es un filo que se balancea contra lo que amontone el jugador. «Tolva con hash intacto» es falso: la tolva tiene hogar y rendija de aire (`LabBench.cs` 152-154); con caudal, la fibra que cae al hueco quemado se desvía. `VientoLevanta` sobre ceniza en reposo no dispara: su chunk duerme (`ProcessIfNeeded` 349) y `LabAire` no despierta chunks. El «caos de brasas» del riesgo declarado es imposible con la regla escrita; nombrarlo muestra que no se trazó.

## 5. Benchmark que se refuta solo

«Tejado a 6 celdas a sotavento prende en ≥ 3/5 semillas»: 0/5 con cualquier parámetro, porque ninguna brasa sale de la chimenea.

## Ajuste mínimo (otro candidato, no una poda)

Lo que compra juego es el ASCENSO que la regla prohíbe. Solo Brasa: en `ProcessPowder`, antes de 1077, si `vy >= 6` (nibble máximo 7: conductos reales, nunca la brasa bancada de R135) y arriba hay `Empty`/gas, sube; si no, cae y la desviación lateral la aparta a sotavento. Con eso la chimenea recta escupe brasas y la serpentina las traga (tercer eje del «recta o serpentina» del candidato 2); el tejado de fibra o de roca junto a la salida decide; la poza bajo la boca las apaga (1006-1013); el tejado mojado no prende. Contador `LabBrasasVoladas`; corregir la paridad de `ProcessBrasa` 1015 (`x+y` constante en diagonal). Prerrequisito duro: que el banco del candidato 1 imprima `vy >= 6` en la chimenea; si se queda en 1-2, no se escribe una línea. Las tres decisiones de exterior se borran.

## Números

Tal como está: apalancamiento 7 → **2**, tuning 7 → **5**, coste 0,5 → **0,3 semanas que compran nada**. Con ascenso, condicionado al tiro medido: apalancamiento 5, tuning 5, 1,0 semana tras el candidato 1.
