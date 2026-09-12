# Refutación · lente cuerpo · `humo-respirado` (lente apalancamiento)

**Veredicto: refutado.** Apalancamiento 7 → **2** · tuning 8 → **6** · coste 0,25 → **0,5** semanas.

## 1. La premisa física está medida, y es falsa a escala de un cuerpo

El candidato vive de una frase: «el humo se embolsa bajo la bóveda y te ahoga a ti primero». El
laboratorio ya midió esa bolsa y no existe a la escala de una caja de 6×11 celdas:

- **Carbonera 20×20 con boca 1** (`Sim/LabBench.cs:135`, HF1, `docs/LAB/CHECKPOINT.md` §6g):
  100 % carbón, 0 ceniza, 0 llama, **0 humo**. Su escenario de verificación («carbonera con
  testigo: curva de `Humo`») mediría una recta en cero durante 9 000 ticks, y el cruce «abrir la
  boca: la bolsa sale sobre el jugador» no tiene bolsa.
- **Pila maciza de carbón** (`Laboratorio/benchmarks/2026-09-04_r136…` §1): humo dentro máx **0**;
  subir `combustHumoPct` de 4 a 40 es **idéntico al bit**. «Oler qué arde» compara cero con cero.
- **Pila fina en caja sellada de 200 celdas de aire, reglas actuales** (r136 §3): **9 celdas de humo
  sin chimenea, 0 con chimenea 8**. La causa está en `SimStepper.cs:855-885` y `LabRespira`
  (`SimStepper.Laboratorio.cs:1052`): en cuanto el humo toca las llamas, entran en sordina y producen
  a un cuarto; la bolsa se autolimita antes de bajar. Para que el parche de la cabeza (tres filas
  altas de una caja de 11) quede dentro, la bolsa necesitaría techo−8 filas; lo medido es 0,5 a 1,4.
- `ProcessGas` (`SimStepper.cs:1443`): con vacío encima el humo sube una celda por tick; cruza el
  parche en tres ticks y la visita cada ocho lo ve una de cada tres veces. El byte solo sube de pie sobre un fogón de fibra o con la cabeza pegada al
  techo de una cámara muy baja.

## 2. No es un cruce: es un instrumento con robo de control

Solo lee `mat[]` y no escribe nada (lo declara el propio candidato). Una ley que no escribe no
cambia el resultado de ninguna otra: no EJECUTA ni JUZGA, solo REVELA, y el humo ya es materia
visible («con visor: nada nuevo», dice él mismo). Cruces reclamados: cinco. Cruces bidireccionales
reales: **cero**; los cinco son «el mismo humo que hace X también sube mi byte», entrada compartida,
no interacción. Es la categoría exacta de `Game/Termometro.cs` (lee `SampleTempRaw`, muestra un
número) más una viñeta de posproceso y dos frames de input cortado. Meterlo en `CuerpoSim` y en
`HashCuerpo` es ceremonia: un sensor que no escribe no necesita determinismo ni hash, y el espejo
ya recibe `mat[]` por RLE (`Net/SimSync.cs:30`): ni siquiera hace falta el `NetworkVariable`.

## 3. Las cuatro «decisiones nuevas» ya existían o no son decisiones

«Abrir o no la boca» y «tallar una boca en el techo» ya son decisiones de la carbonera y del horno
para el propio fuego (boca 1 → 100 % carbón; boca 4 → 19 % ceniza, HF1). «Qué combustible cargar»:
idéntico al bit con carbón. «Dónde pararse»: una molestia que se deshace dando un paso, como admite
su propia mitigación; lo que se deshace con un paso no es apuesta y no hay SOLTAR que la comprometa.

## 4. Tuning escondido

Cinco números, todos de sensación: `humoTraga 12`, `humoSuelta 4`, `humoTos 200`, el 0,8 de la
viñeta y la cadencia 32 ticks / 2 frames. El banco mide la curva, pero no puede juzgar el único
riesgo que el candidato nombra («barra disfrazada», «robo de control»): eso es playtest de Cesar,
la iteración cara. Y como el fenómeno casi no ocurre, `humoTraga` acabará subiendo hasta que una columna
suelta ciegue: la barra disfrazada.

## 5. Coste real

Viñeta, tos con sonido y sprite, struct, pasada, hash, escenario de banco y costura de red no caben
en un cuarto de semana: **0,5**. El sensor solo, 0,05.

## 6. Lo que sobrevive

Como ley autónoma, nada. Como polizón de C1: ~30 líneas lado juego (patrón `Termometro.cs`,
`SampleMaterial` sobre el parche de la cabeza) que tiznan el sprite con la rampa de hollín de
`Sim/SimRenderer.Laboratorio.cs` y disparan una tos sonora al cruzar un umbral; sin viñeta, sin
corte de input, sin `CuerpoSim`. Legibilidad, no ley: tuning cero, coste 0,05.

El apalancamiento que este candidato promete pertenece a OTRO candidato: el cuerpo como obstáculo
de `ProcessGas` (una máscara de 66 celdas consultada donde hoy se comprueba `mat == Empty`), que
convierte «ponerse en la boca» en regular la boca (la curva HF1 ya existe) y le da al humo respirado
un fenómeno real: el precio de ser el tapón. Escribe en la sim, exige la posición como entrada de
tick (arquitectura de C1) y se refuta aparte.
