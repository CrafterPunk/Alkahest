# REFUTACIÓN · El testigo: la condición vive en la grilla (lente metricas-y-soltar, criterio apalancamiento)

**Veredicto: refutado.** Es un instrumento, no una ley; tal como está escrito rompe la física que
dice no tocar; y la versión que sí sirve no necesita material nuevo.

## 1. No cruza ninguna ley: lo admite el propio candidato

«No cambia ninguna regla existente: solo lee.» Cruces contados: **cero**. Los cuatro "cruces"
(luz, humedad, calor, agua) son cuatro *lecturas* de campos que ya existen (`luz`, `humedad`,
`temp`, `mat`). Ninguna decisión sobre agua, fuego, luz o plantas cambia porque haya un testigo
al lado. Las «decisiones nuevas» son dónde colocar un sensor (observabilidad, no física) y
«construir contra un testigo del autor», que pertenece a la `Condicion` del candidato 2 y funciona
igual si la condición apunta a una posición sin material. Por la función objetivo (poco añadido
que transforme muchas decisiones existentes), es la definición exacta de capa encima.

## 2. Tal como está escrito, es un hogar disfrazado

La regla escribe `temp[i] = max(temp[i], max temp vecinos)` cada visita y promete «conservación
intacta». Falso para la temperatura: `LabDifusionTermica` (`SimStepper.Laboratorio.cs`
1306-1345) y `DiffuseTemperature` (`SimStepper.cs` 2269) recorren **todas** las celdas sin mirar
el material; `LabK` cae en `default: KPolvo` para cualquier id nuevo (o `KRoca` si se lista como
roca). Un testigo que vio una vez 200 raw queda clavado a 200 y radia cada pasada: para una yesca
vecina, `d = 130`, `k = min(3, 2) = 2`, `step = 260 / (64·2) = 2` raw por visita, sostenido; el
tirón a ambiente (1 raw por barrido) lo deshace la re-escritura del máximo. `TryIgnite`
(`SimStepper.cs` 1780) prende cuando `temp[nidx] > ignitionTemp`: la yesca (130) junto a un
testigo que vio fuego arde para siempre. El hogar real es de 170 raw y cuadra en el libro de
energía (HF4, `LabCalorFuego == Σ calor`); el testigo sería un hogar de hasta 255 raw fuera del
libro. Los otros tres campos sí son inertes (`LabCapilar` guarda por `EsPoroso`,
`LabInfiltrarHacia` por `Permeabilidad`, `LabSecarHacia` exige `Empty`), pero el que importa para
«¿sostuvo el horno 200 raw?» es justo el que revienta.

## 3. Responde mal a la pregunta que dice responder

Un higrómetro de *máxima* no mide lo de R150. Las plantas mueren con humedad 50-99 y mínimo 60
porque la humedad **no se sostiene** (~10 s sin savia, `LabPlanta`). Un máximo de la cara diría
«99 ≥ 60, apto» justo donde el huerto muere. El instrumento correcto para «mojado Y iluminado,
sostenido» ya existe y es emergente: la **planta viva**. Igual el resto: la terracota es el
termómetro de máxima (`temp >= TerracotaRaw`, línea 641), el vidrio es «200 raw sostenidos 60
visitas» con `reposo` de contador (608-613), el carbón es «ardió sin aire», la grava colmatada
es la turbidez. Cinco testigos que las leyes ya escriben en `mat[]`, con consecuencias físicas
(la planta transpira y sujeta el suelo; la arcilla-sonda compacta y cuece). El candidato lo
reconoce en su mitigación y aun así añade el material.

## 4. Tuning escondido y coste real

«Tuning 9» calla tres números: los umbrales del tornasol, cuántos testigos tiene el jugador
(ilimitados = alfombra sin decisión; limitados = economía a balancear, R44) y la lista de
indestructibles (`Tallable` en `LabMateriales.cs`). Coste: id 80 en `Universe.cs`, `MaterialDef`,
tablas K/C/tallable/nombre, color y tornasol en `SimRenderer`, pincel en `LabPanel`, más arreglar
el punto 2 (registros en `aux`/`morph` o tabla lateral, nunca en los campos físicos). Semana y
media, no una, para un instrumento.

## 5. Ajuste mínimo (que ya no es este candidato)

No crear `MaterialId.Testigo`. En la `Condicion` del sello (candidato 2), una cláusula
`material en (x,y) == M` y `campo en (x,y) ≥ v` evaluada al cierre del día. El autor escribe la
condición con los materiales-testigo que las leyes producen («Planta viva en (118,250) el día 30»,
«Terracota en la boca del horno», «carga de la grava < 64»). El jugador que quiera *saber* tiene
que **plantar una semilla o poner un terrón de arcilla**: la sonda es materia con consecuencias, y
eso sí cruza agua, luz, planta y calor. Medio día de Opus dentro de las tres semanas del sello;
cero material, cero física, cero tuning, siete hashes intactos por construcción.

**Corregido:** apalancamiento 2 (como material; 5 como cláusula del sello, contado allí), tuning
7 (9 con el ajuste), coste 1,5 semanas (0,1 con el ajuste).
