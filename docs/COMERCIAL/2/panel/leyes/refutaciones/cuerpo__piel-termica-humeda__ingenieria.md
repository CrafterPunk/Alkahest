# Refutación de ingeniería · `piel-termica-humeda` (lente cuerpo, C1)

**Veredicto: VIABLE CON AJUSTE.** La arquitectura es correcta y barata: posición como entrada del
tick, pasada nueva tras `LabCampos()` en `LabPasadas()`, escrituras solo por `LabSumarTemp`,
`LabTransformar` y `LabNacerAgua`. Lo que falla es la ESPECIFICACIÓN de dos tasas y una API que no
existe. Tal como está escrito, el banco mediría un cuerpo que se seca en un segundo y responde al calor
como una celda de agua: la lente («cuánto aguanto», «mojarme antes del horno») no tendría tiempo de existir.

## Lo que el código confirma

- **Determinismo intacto.** `Step()` (SimStepper.cs:257-338) llama a `LabPasadas()` al final, en orden
  fijo; `LabDifusionTermica` y `LabCampos` recorren TODA la grilla (`i = offset; i += 8`) sin depender del
  sueño de chunks, así que el cuerpo lee y escribe `temp[]` de un chunk dormido sin desincronizar nada.
  Con `X0 < 0` la pasada no toca nada y los 9 hashes actuales no se mueven.
- **Presupuesto.** 66 celdas + fila de pies cada 8 ticks contra ~27 600 celdas/tick de `LabCampos`: el
  «< 0,01 ms» es cierto. `MsCuerpos` y el gancho vacío `LabCuerpos()` (línea 1290) ya existen.
- **El banco puede guionarlo.** `Correr` (LabBench.cs:265) ya inyecta la caldera ANTES de `st.Step()`;
  añadir `Guion` a la tupla de `Escenarios` (línea 76) y `HashCuerpo` a `Resultado` (línea 30) es mecánico.
- **Los cruces con la yesca son reales.** `LabCombustibleMojado` (línea 161) lee `humedad > FibraMojadaMin`;
  la fibra es porosa (`PermFibra 30`) e infiltra ~3 u/visita: siete goteos quietos sobre la misma yesca
  (≈2 s) la inutilizan. El rocío del aliento se ve: `LabTinte` tiñe `Stone` por `humedad`
  (SimRenderer.Laboratorio.cs:90) y `LabAire` (línea 328) lo manda al vecino más frío.

## Los tres errores de especificación (el ajuste)

1. **Secado sin acotar.** `LabSecarHacia` (línea 734): `rate = tasa·(16+t)/16·deficit/sat`, mínimo 1 por
   celda de aire. En la sala del hogar (70 raw, sat 60, vapor 36) son 66 u/visita sobre 66 celdas
   cubiertas: `Mojado` 255 → 0 en 4 visitas = 32 ticks ≈ **1 s**. Ajuste: secar por un número FIJO de
   caras (`piel.caras` 4, como una celda), no por celda cubierta. Además `LabSecarHacia` lee `temp[i]` y
   aplica `LabLatente(i)` a una celda de la grilla: hace falta una sobrecarga que reciba la temperatura
   fuente y devuelva el latente (10 líneas; los cuatro llamantes no cambian).
2. **Térmica mal normalizada.** `paso = Σ_66 d·k/(64·24)` = 0,17·d por visita; una celda de agua hace
   0,125·d y una de aire 0,25·d (línea 1310). El cuerpo NO tiene «la inercia de sesenta celdas»: tiene la
   de una celda de agua. Inmerso en aire a 130 raw cruza `calorSuelta` en 3 visitas; el «5,3 s» solo sale
   porque a dos celdas del hogar apenas 10 de las 66 están calientes: geometría, no piel. Ajuste: dividir
   por `64·cCarne·nCeldas` (promedio) y que el banco fije `cCarne` en 8-16.
3. **`LabNacerAgua` no nace parcial.** Línea 264: `SetCell` deja `humedad = 255` y audita `+255`. Hace
   falta `LabNacerAguaParcial(j, temp, vol)` (patrón de `LabGotear`, línea 317) y decidir el libro:
   cuerpo FUERA de `LabBalanceU` (absorber −t, gotear/exhalar/secar +v) más el assert local
   `absorbido − goteado − secado − exhalado == Mojado`. `SpawnSteamPuff` vale como vaho: `Transform`
   (SimStepper.cs:707) no toca `humedad`, no crea masa.

Menores: las sales 641/643 existen y 631 ya es de la roca suelta (usar ≥ 653). `Flask.DumpAll`
(Flask.cs:706) vierte en el cursor: hace falta `DumpAt(celdaPies)`.

## Lo que el banco desmentirá del apalancamiento

«Por donde pasas mojado, brota» no ocurre andando: a 15 celdas/s se gotea cada ~4 celdas, 16 u por gota,
y el sedimento (`PermSedimento 12`) infiltra ~1 u/visita mientras evapora; `PlantaHumedadMin 60` pide
cuatro gotas sobre la MISMA columna. Riega el que se para. El aliento (2 u/visita) da una gota de rocío
cada ~34 s quieto. Cruces honestos pero pequeños: descuento el 9 a 7.

## Benchmark mínimo

«Cuerpo en el alambique»: `MontarAlambique` + guion: 3 000 ticks en la poza (290, 124), 3 000 andando 30
celdas por el lecho (x 250 → 220, y 129), 3 000 en (161, 180), dos celdas al este del hogar. Asevera:
`Mojado` toca 255 y llega a 0 antes de la mitad del tercer tramo; `LabCuerpoGoteos` en [8, 16];
residuo de `LabBalanceU` idéntico al alambique sin cuerpo; tick del primer cruce de `calorSuelta` en el
informe; 9 hashes previos INTACTOS; `HashCuerpo` en la tabla. 1,5 semanas de Opus, sin la red.
