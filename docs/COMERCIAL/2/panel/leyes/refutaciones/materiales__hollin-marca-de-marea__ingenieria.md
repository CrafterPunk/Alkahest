# Refutación de ingeniería — «Hollín y marca de marea» (lente materiales, cand. 3)

**Veredicto: viable con ajuste.** Apalancamiento corregido 6 (declarado 7) · tuning 7 · coste 1 semana · determinismo intacto.

## Lo que el código confirma

- `carga` está libre en `Stone`, `Terracota`, `PisoEstructural`, `RocaSuelta` y `VidrioVerde`: nadie lo lee ni lo escribe (`LabRoca`, `SimStepper.Laboratorio.cs:773`, solo toca `humedad`). Viaja en `SwapCells` (`CellGrid.cs:277`), nace a 0 en `SetCell` (254) y ya está en `HashCarga` (`LabBench.cs:330`).
- La fuga de finos existe: las cuatro salidas `vol <= 0` de `LabAgua` (439-448) llaman a `LabTransformar(i, Empty, 0, 0)` y la `carga` del agua evaporada desaparece. Pasarla al vecino de abajo es una línea por salida.
- El lavado es una transferencia entera en orden fijo, sin sal nueva; `LabTinte` (`SimRenderer.Laboratorio.cs:26`) y `Estado` (`LabMateriales.cs:190`) ya leen `carga`: los lectores de render y rótulo son un `case` cada uno.
- Coste: `LabRoca` recibe ~16 600 visitas por tick y hoy sale con una lectura; cuatro lecturas de `mat` y comparaciones son 0,03-0,05 ms. Lo mide `MsCampos`.

## Lo que el código refuta de la regla tal como está escrita

1. **La fuente «por contacto» no es conservativa y da la lectura invertida.** El humo nace en `SpawnSmokeNear` (`SimStepper.cs:941`) por `Transform`, sin masa; la regla propone que cada visita de roca sume `nHumo·2 + nLlama·6` de la nada. Una celda de humo de 255 ticks rodeada de cuatro rocas deja 4×64 = 256 u = UNA celda de finos que, lavada y depositada (`DepositoUmbral` 200), es una celda de `Sedimento` nueva. La fibra emite humo al 16 % por paso (`Universe.Laboratorio.cs:90`) durante 40 pasos: varias celdas de humo por celda de combustible, y cada una puede valer una celda de finos en un recinto estrecho. Es la ganancia neta por evento frecuente que prohíbe la R55. La combustión en sordina (`SimStepper.cs:885`) emite el humo a 1/4 y SIN llama, así que el fuego ahogado tizna MENOS que el que respira, y la roca que toca una llama viva (6 u/visita) queda negra en 43 visitas ≈ 11 s: todo hogar ennegrece su entorno en el primer cuarto de minuto y «una bóveda negra = ese fuego no respiraba» es falso en este motor. El diagnóstico que vende el candidato no sale de la regla escrita.
2. **La guarda `IsChunkAwake` rompe un contrato.** La cabecera de `LabCampos` dice explícitamente «sin depender del sueño de chunks: una poza dormida sigue evaporando». Meter el sueño en una ley de campos crea la primera física del laboratorio cuya salida depende del estado de los chunks: determinista, pero deuda que 0,05 ms no justifican.
3. **«Determinismo intacto» ≠ «hashes intactos».** La marea entrega `carga` al fondo, y el fondo poroso ya lee `carga` como colmatación (`LabInfiltrarHacia`, 500-520) y el sedimento como fertilidad. Es físicamente coherente (la poza turbia que se seca colmata y abona su lecho), pero cambia `HashCarga`, `HashHumedad` y probablemente `HashMat` en «diluvio turbio» y «arco largo». Hay que re-basar.
4. **El lector de luz depende del candidato 1.** `LabLuzDecay` (1276) es `static` sin índice; hoy `VidrioVerde` ni transmite ni tiene `case`. Sin el candidato 1, el único lector físico del hollín es el ciclo del sedimento, y el apalancamiento baja.

## Versión mínima viable

- **Presupuesto en el humo, no contacto de la nada.** En `SpawnSmokeNear`, gateado por `LabActivo`, tras `Transform` escribir `carga = sordina ? HollinSordina (96) : HollinLimpio (24)`. `LabRoca` transfiere por contacto `t = min(carga[humo], HollinContacto, 255 − carga[roca])` restando al humo. Conservativo por construcción, `Σ hollín ≤ Σ humo × presupuesto`, y el fuego que no respira sí escribe más en la pared. Sin fuente de llama en v1.
- **Libro `LabBalanceHollin`** como `LabBalanceU`: nacido − perdido al morir el humo − depositado en roca − lavado al agua = 0 en todo escenario.
- **Sin guarda de chunk.** Marea en las cuatro salidas de `LabAgua`; lavado, tinte y rótulo como están. Vidrio y fase 2 fuera.

**Banco.** «carbonera 20x20 boca 1» contra una variante boca 3: `Σcarga(roca)/combustible quemado` mayor con boca 1 (la lectura prometida, ahora medible); máximo en la columna de la boca; identidad del libro a 0; `MsCampos` ≤ +0,1 ms en «mundo entero despierto»; re-base documentada de «diluvio turbio» y «arco largo».
