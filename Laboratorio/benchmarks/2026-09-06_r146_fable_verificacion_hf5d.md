# R146 — FABLE: VERIFICACIÓN DE HF5d (commit b4d2413)

Banco headless por `Unity_RunCommand`, sin Play, sobre el assembly de la R145.

## 1. El banco, con overrides y en doble corrida

`LabBench.Correr` sobre el laboratorio base y la carbonera (esta última dos veces seguidas):

| escenario | ticks | ms/tick | hash mat | hash temp | hash aux |
|---|---:|---:|---|---|---|
| laboratorio base | 3 000 | 1,75 | `4d24ee8a` | `1598ea10` | `f9c20c00` |
| carbonera 20×20 boca 1 | 9 000 | 1,63 | `69e65c00` | `54decda7` | `8bac1dc5` |
| carbonera (bis) | 9 000 | 1,68 | `69e65c00` | `54decda7` | `8bac1dc5` |

Idénticos entre corridas. El `hash temp` de la carbonera cambia respecto a R142 (`96cfba94` →
`54decda7`): es la huella de los overrides (el humo vive 255 y no 200, y la sordina se decide
vecino a vecino); el `mat` final no cambia porque a los 9 000 ticks todo es ceniza o carbón igual.
El laboratorio base no cambia en nada: en 3 000 ticks no hierve ni arde nada, y esos son los
campos que los overrides fijan. `Informe()` sigue imprimiendo tres hashes de los siete (R24-7).

## 2. El nivel en reposo no quema nada

`BuildLaboratorioDeLeyes` + stepper, 4 500 ticks sin tocar nada:

| t | quemado | calor de llama | llamas vivas | brasas | goteos | carbonizado | vidrio | plantas |
|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 100 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 1 000 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 4 500 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |

Solo el hogar escribe raw (`LabRawHogar` 51 804 a los 4 500 ticks). Así que el umbral de «+200
unidades» del hito «PRIMER FUEGO» no protege de ningún fuego de fondo —no lo hay— y en cambio
esconde el primer fuego pequeño del jugador (R24-1). Las 313 u de la sesión de prueba de R143 las
encendió quien probaba.

## 3. Revisión adversaria (17 hallazgos, 0 refutados)

Detalle en `PREGUNTAS_A_FABLE.md` R24: seis correcciones de una línea antes de la build (hito del
fuego, `_posAnterior`, snapshots fuera de la pestaña de presets, boost del limitador de goteos,
`InputLocked`, `Cerrar` con aviso y la guía con la ruta real) y el resto del banco después de H7.
