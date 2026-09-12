# Refutación · sondas registradoras (la veta) — lente instrumentos-rayosx, criterio apalancamiento

*Leído: `SimStepper.Laboratorio.cs` (LabPasadas:134, LabCampos:201, vidrio 608-619), `LabBench.cs` (Escenarios:75, Correr:265, MedirLecho:353, ArcoMuestra:421), `LabPresets.cs` (GuardarSnapshot:222), `LabDiario.cs` (Muestrear:270-305), `Termometro.cs`, `metricas-y-soltar.md` §2 y su refutación.*

**Veredicto: refutado** como ley del panel. Es un gráfico de tiempo honesto y barato, pero cruza cero leyes, apoya tres de sus cinco cruces en cosas que no existen, y el trozo que sí tiene apalancamiento es una herramienta del banco, no juego.

## 1. Cuenta de cruces: cero de cinco

- **SOLTAR.** No existe en el código (el único «soltar» del repo es el botón del frasco, `Flask.cs:921`). Es el candidato 2 de `metricas-y-soltar`: la veta se vende como «veredicto diferido» de un mecanismo que otro panelista aún tiene que construir (3 semanas). Cruce prestado.
- **Asíncrono por fichero.** No hay fichero de partida. `LabPresets.GuardarSnapshot` escribe preset + PNG + libro JSON; la grilla no viaja a disco, solo por red en `SimSync`. «Retar con mi veta» es una tira de colores sin mundo que la respalde. Cruce sobre un formato inexistente, cuyo coste (el volcado del SELLO) el candidato no cuenta.
- **Tolva, alambique, horno.** Tres lecturas de `temp`, `humedad` y `temp`. Y el horno está mal instrumentado: la racha de 60 visitas vive en `reposo` (608-619) y se reinicia a 0 en la visita en que falta calor o ceniza; una muestra de `temp` cada 256 ticks pasa por encima de un corte de 8. El cruce estrella lee el campo equivocado.

Ninguna regla física cambia. La sonda es metadata: nada la entierra, la quema, la arrastra el agua ni la colmata; el propio §6 descarta «sondas como material» por la lección de `EsSolidoDelMundo`. Por tanto las leyes no pueden ejecutar, revelar ni juzgar el instrumento. Es la definición de capa encima.

## 2. El mundo ya escribe sus vetas, y el tiempo ya se muestrea

La memoria con consecuencias ya está en `mat[]` y `reposo`: la terracota es el termómetro de máxima, el vidrio es «200 raw sostenidos 60 visitas» con contador, el carbón es «ardió sin aire», la grava colmatada es la turbidez, la planta muerta es la humedad que no se sostuvo. Y la serie temporal existe dos veces: `LabDiario.Muestrear` a 1 Hz con hitos (PRIMER GOTEO, PRIMER VIDRIO) y `ArcoMuestra`, una fila por 1 800 ticks, que es exactamente cómo se encontraron el 22/36 de R148 y el 0/36 de R150. «La regresión pasa a decir cuándo divergieron» ya ocurre, a grano grueso, para el arco largo.

## 3. Decisiones nuevas: una prestada, tres de legibilidad

«Qué ocho cosas merecen memoria» es un presupuesto de ventanas de depuración. «Cuándo soltar» pertenece al SELLO. «Leer una oscilación y sellar una boca»: la decisión ya existe; la veta abarata aprenderla, que es valioso y no es juego nuevo. «Retar con fichero» exige el volcado. Cero decisiones nacidas de cruzar dos leyes.

## 4. Verificación y tuning escondido

- «La veta del lecho reproduce 22/36 y 0/36»: imposible. `MedirLecho` cuenta 36 columnas con agua en y 250-256; ocho sondas puntuales no reproducen un conteo de región. La aceptación headless propuesta no se puede cumplir tal como está escrita.
- «El eje es el reloj de la vigilia»: solo a un multiplicador. El anillo B son 65 536 ticks: 36 min de reloj a ×1, 3,6 a ×10; `LabDiario` anota «velocidad del mundo → ×N» precisamente porque cambia en sesión. La tira no es un reloj, es un contador de ticks.
- 256 muestras «pintadas en la pared junto a la sonda» a escala de celda ocupan un tercio del mundo (768 de ancho): hay que comprimir, escalar o recortar, y elegir 8 colores por campo sobre la paleta de un candidato (rayos X) que la refutación hermana ya dejó en apalancamiento 3 y «sin sondas». El «riesgo mayor» declarado (que una tira no se entienda como tiempo) es legibilidad por playtest: lo caro.

## 5. La versión mínima con más apalancamiento (no es una ley)

Herramienta del banco: en `LabBench.Correr`, un anillo de los siete hashes cada 256 ticks (FNV sobre ~1,5 MB, 1-2 ms, ~35 veces por escenario de 9 000 ticks) más 2-4 muestras puntuales fijas por escenario en bytes crudos, todo impreso como texto en `Informe`, con el primer bloque divergente nombrado. Dos días de Opus, cero superficie de juego, cero tuning, y compra bisección exacta en el tiempo para cada regresión: eso sí acorta la iteración que la función objetivo quiere acortar. En el juego, si se quiere hoy: las tres sondas de `Termometro` ganan un anillo crudo A dibujado en F8, un día, sin tocar la sim ni inventar un fichero. La veta como objeto de partida espera al SELLO; cuando exista, será su vista de veredicto, no una ley.

**Corregidos:** apalancamiento 2, tuning 10 (la versión de banco no tiene nada que afinar), coste 0,4 semanas.
