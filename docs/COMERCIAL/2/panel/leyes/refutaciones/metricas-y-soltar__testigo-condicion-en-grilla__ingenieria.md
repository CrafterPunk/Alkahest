# REFUTACIÓN DE INGENIERÍA · «El testigo: la condición vive en la grilla»

*Candidato `testigo-condicion-en-grilla`. Leído: `SimStepper.Laboratorio.cs` (`LabCampos` :201-240,
`LabAire` :328-395, `LabAgua` :406-520, `LabCapilar` :717, `LabSecarHacia` :734, `LabPlanta`
:801-905, `LabPresion` :1140-1160, `LabLuz` :1177-1285, `LabDifusionTermica` :1306-1370),
`SimStepper.cs` (`DiffuseTemperature` :2269-2304), `CellGrid.cs`, `LabMateriales.cs`,
`Universe.Laboratorio.cs` :95-147, `LabBench.cs` (`MedirLecho` :354, `ArcoAvanzar` :404),
`AlkahestSim.PaintLab` :831.*

## Veredicto: VIABLE CON AJUSTE

## Lo que el código confirma

- **Barato y determinista.** `LabCampos` visita TODA la grilla a 1/8 por tick sin depender del sueño
  de chunks (:13-15, :205-240): un caso más en el `switch` son cuatro lecturas, cero RNG, y los
  contadores no se paran cuando el chunk duerme.
- **Nadie le escribe encima.** Toda escritura sobre un vecino está filtrada por clase: `LabCapilar`
  exige `EsPoroso` (:720), `LabSecarHacia` exige `Empty` (:736), `LabInfiltrarHacia` exige
  `Permeabilidad > 0` (:503), la decantación solo va a `Water` (:458), la condensación solo a
  `LabEsSuperficieCondensable` (:277) y `LabPresion` solo al `target` recién vuelto agua (:1157). Un
  id nuevo no cae en ninguna: `humedad`, `carga` y `reposo` del testigo son suyos. Conservación intacta.
- **Estático de verdad.** Molde del Hogar (`StaticSolid`, `caeSolido = false`, `density` máxima,
  Universe.Laboratorio.cs:95-103): nunca pasa por `SwapCells`, `aux` libre, `Tallable()` false por
  defecto. Los siete hashes de los ocho escenarios sin testigo no se mueven.

## Lo que el código refuta

1. **`temp[i]` no puede ser el registro.** Los dos caminos térmicos reescriben `temp` de toda celda
   interior en cada visita: `LabDifusionTermica` (:1316-1345, activo con `TermicaPropia = 1`) suma
   flujos con `LabK`/`LabC` y tira hacia `ambient`; `DiffuseTemperature` (:2280-2304) promedia los
   cuatro vecinos. Un testigo que escribe `temp = max(vecinos)` es devuelto ≥ 1 raw por visita —no
   recuerda— y sus vecinos leen `temp[testigo]` en SU flujo: el máximo vuelve como calor. Fuente de
   energía fuera del libro (`LabRawFuego`) que además alimenta la regla del vidrio de la arena de al
   lado (`temp[i] >= VidrioRaw`, :608). El termómetro calentaría el horno que mide.
2. **Un byte de visitas satura a los 68 s** (255 × 8 = 2 040 ticks; el «día» del candidato 2 son
   1 800). «Sigue mojado e iluminado el día 30» no se lee de un contador lleno el día 1,13; y es
   monótono: dice «alguna vez», no «sigue».
3. **La pregunta de R150 es de MÍNIMO.** Un higrómetro de máxima en un lecho que el goteo anega marca
   255 y no dice si la humedad AGUANTÓ ≥ 60. Y `humedad` cambia de unidad por vecino (vapor, volumen,
   agua, savia; CellGrid.cs:182-186): «max de los cuatro» mezcla magnitudes.
4. **La indestructibilidad no existe hoy ni para el hogar**: `PaintLab` llama a `SetCell` sin guarda
   (:836). Un `if` lo cierra; menor.

## Ajuste mínimo: el testigo es una planta que no muere

La planta ya lleva el instrumento exacto: `reposo` cuenta visitas seguidas sin savia, vuelve a 0 si
la recupera y mata a `PlantaMarchitaVisitas` (:884-904). El testigo copia ese contador y, en vez de
morir, se queda PEGADO. Cuatro bytes, cero parámetros nuevos:

- `aux`     = máximo de `temp[i]` propio; la térmica sigue física (`LabK = KRoca`, `LabC = CRoca`,
  casos en :1354-1370). Es lo que mide la regla del vidrio, sin devolver calor.
- `humedad` = máximo de `humedad` de vecinos POROSOS o AGUA solamente (anegado = 255).
- `carga`   = racha de visitas con «max humedad porosa vecina < `PlantaHumedadMin`»; reset si se
  cumple; al llegar a `PlantaMarchitaVisitas` se clava en 255: «aquí una planta habría muerto de sed».
- `reposo`  = igual con `luz[i] < PlantaLuzMin` (`LabLuz` ya ilumina un sólido como el aire que lo
  toca, :1276-1285): «aquí habría muerto a oscuras».

La condición del autor ya no necesita «día 30»: «T no está clavado en sequía ni en oscuridad y
`aux[T] < VidrioRaw`». El id en `aux` se retira; la `Condicion` refiere por posición. Tornasol por
los cuatro bytes como caso nuevo de `LabTinte`.

## Benchmark que lo prueba

El del candidato está mal planteado: un máximo nunca coincide con «luz media / humedad media de la
cara». El correcto es una SOMBRA: `ArcoAvanzar` recalcula cada 8 ticks, por su cuenta, max y rachas
sobre los vecinos de un testigo en `(118, 247)` (banda de `MedirLecho`) y al tick 72 000 exige
igualdad byte a byte con `humedad/carga/reposo[T]`. «Horno con yesca» con un testigo en la columna
de arena junto a la ceniza: `aux[T] >= VidrioRaw` sii esa columna dio vidrio (18/18 conocido). Los
ocho escenarios sin testigo, hashes iguales. Todo headless.

## Valores corregidos

**Coste**: 1,5 semanas (material, tres `switch`, tinte, guarda de `PaintLab`, sombra y dos
escenarios; el lector de `Condicion` es del candidato 2). **Tuning**: 9 (umbrales prestados de la
planta). **Apalancamiento**: 6, no 7: es instrumento, no ley —ninguna regla reacciona a un testigo—;
compra condición-como-dato y situaciones verificables sin personas, pero cero juego emergente. Vale
porque hace juzgables las leyes, no porque las cruce.
