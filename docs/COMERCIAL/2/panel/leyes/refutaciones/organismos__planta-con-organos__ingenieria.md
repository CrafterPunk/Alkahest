# Refutación de ingeniería · organismos / planta-con-organos

**Veredicto: viable con ajuste.** Determinismo, presupuesto y verificabilidad pasan. Dos de las cinco reglas contradicen el código que dicen dejar «tal cual», y la tercera —la copa que da sombra— la refuta el modelo de luz. Lo que queda (siega con rebrote, raíz triple, inclinación hacia la luz) es barato y cierto.

## Lo que pasa

- **Determinismo.** Argmax con orden fijo, sin RNG. `carga` viaja en `SwapCells` (CellGrid 264-279) y la planta ni cae ni fluye (SimStepper 387-391). `luz` se recalcula cada 16 ticks (Laboratorio 146-147): campo rancio, pero idéntico en toda máquina.
- **Presupuesto.** `LabCampos` visita 1/8 de la grilla por tick (205-209); seis lecturas sobre < 2 000 celdas y una rama en `LabLuzDecay` solo si `m == Planta`. No medible.
- **Banco.** `Correr` ya interviene por tick (la caldera, LabBench 306-311), pero cableado a `esAlambique`: la siega necesita un gancho genérico `Action<CellGrid,int>`. Diez líneas.

## Lo que está mal contra el código

**1. La regla de apoyo mata la hoja y la punta diagonal.** `LabPlanta` 808-813 vuelve Fibra toda celda cuyo `abajo` no sea Planta ni sustrato, antes que nada. Una hoja al costado tiene aire debajo: muere en su primera visita (≤ 8 ticks), no «por 886-903». Igual la punta que el fototropismo pone en `arriba±1`: su `abajo` es el aire junto a la punta anterior. Y la savia solo sube a `arriba` (835-845): la celda diagonal nace con savia 0 (878) y nunca recibe. «Sustituye la tirada» no basta: hay que reescribir apoyo y savia como grafo padre/hijo por órgano.

**2. La copa no da sombra.** `LabLuz` (1177-1266) es un campo de distancia: máximo de cuatro barridos, y la celda receptora resta el decaimiento de SU material. Bajo una hoja con `LuzDecayHoja = 40`, el aire recibe `max(luz_hoja − 1, luz_lado − 8)`: junto a una columna a 255, 247. «Dos hojas apiladas dejan < 40» es falso hasta en vertical puro (255 − 80 = 175). Para bajar de `PlantaLuzMin` el aire iluminado más cercano debe estar a ≥ 27 celdas: copa opaca de ≥ 55 de ancho. La regla de espaciamiento no existe; hacerla existir es otra ley (luz direccional) que toca «pierde 8 de lado» y todos los hashes de luz.

**3. Fototropismo puro = escalera.** Fuera del haz la luz cae 8 por columna: el diagonal hacia la boca siempre gana al vertical (−1) hasta entrar en la columna iluminada, y una planta de 14 celdas al borde del claro se tumba 14 columnas a 45°.

**4. La siega es real pero gruesa.** `Tallable`/`ProductoDeTalla` (LabMateriales 43-70) admiten Planta → Fibra en dos líneas, y el cincel pinta el producto con `PaintLab(…, humedad[idx], 0)` (Cincel 468-470): la celda cortada hereda su savia, nace VERDE y con > 100 no prende (`FibraMojadaMin`). Lo de encima cae seco por 808 en cascada. Pero el disco es de radio 2 (Cincel 132): cinco celdas de tallo de golpe. «Segar alto o a ras» pide un modo de radio 0: verbo + playtest.

## Ajuste mínimo

- `carga` en Planta = órgano + dirección del padre; 808 comprueba al padre, 835 empuja savia al hijo y a la hoja lateral. Es reescribir `LabPlanta`, no parchearla.
- Fototropismo de necesidad: `arriba` si tiene luz; si no, el diagonal vacío con más luz (empate fijo). Sin parámetro nuevo.
- Raíz: argmax de `hum` en `abajo, abajo±1`; `LabErosion` 189 protege las tres.
- Hoja: transpira ×2, no crece, muere a Fibra. Sin `LuzDecayHoja`, sin copa. El espaciamiento sale de raíces vecinas compitiendo por el mismo `hum` (823-831).
- Siega: Planta en `Tallable`, producto Fibra, radio 2 en v1; gancho por tick en `Correr`.

## Benchmark

«Huerto de banco», 36 000 ticks: lecho de 40 columnas, boca de 20 desplazada 10 columnas para forzar gradiente, serpentín de goteo; siega scriptada a tick 18 000 (mitad a ras, mitad a altura 3). Criterios: inclinación media hacia la boca > 0 y ≤ 3 columnas; población de los últimos 24 000 ticks dentro de ±20 % y ≠ 0; rebrotes ≥ 8 en 3 000 ticks; fibra producida = celdas cortadas y `LabBalanceU` al bit; invariante «cero plantas sin padre»; arco largo dentro de +2 % de ms/tick; hashes intactos en los seis escenarios sin plantas.

## Valores corregidos

Apalancamiento 7 (sin competencia por luz quedan cosecha con rebrote, instrumento de luz y raíz que sujeta y bebe ancho). Tuning 7 (`PlantaHojaSavia`, factor de hoja, radio de siega). Coste 2 semanas: lo que ahorra la copa lo gasta la reescritura topológica de `LabPlanta` y el gancho del banco.
