# Refutación · rayosx-bandas-de-ley (lente instrumentos-rayosx) · criterio: apalancamiento

**Veredicto: viable con ajuste.** Hay dos propuestas pegadas. La tabla de bandas es infraestructura de percepción honesta y barata. Las sondas con radio y la «red de percepción» son una capa de interfaz que ninguna ley ejecuta, revela ni juzga, con más tuning del que declara y una promesa multijugador que el código desmiente.

## 1. Leyes cruzadas: cero de cuatro

Cuento los «cruces» declarados. **Lector R140** y **`Estado`** son presentación (`LabPanel.DibujarLectura`, `LabMateriales.Estado`). **«Sordina: la gramática de vibración»**: `sordina` sí es física (`SimStepper.cs` l. 855-885), pero la «gramática de vibración» no existe en ningún archivo del proyecto ni en otro candidato; el cruce apunta al vacío. **Huella y registradoras** son candidatos hermanos, no leyes. Cruces con leyes existentes: 0.

Las cuatro «decisiones nuevas» tampoco nacen de cruzar nada. El documento lo dice: «la lista de sondas es local a cada cliente: no toca la sim». En `Termometro.cs` una sonda son tres arrays de posición que el mundo no lee; nada la entierra, la quema ni la arrastra el agua (el único compromiso físico es `Flask.ReachWorld = 6f`). «Mover una es dejar de ver otra cosa» es un presupuesto de ventanas de depuración. Predecir la lluvia por saturación y leer el reposo de una poza son decisiones que YA existen (el bloque frío, la espera): la banda abarata aprenderlas, no las crea.

La cuarta es falsa hoy: `SimSync.cs` (l. 50-57) replica solo `mat`; `temp` y los cuatro campos no viajan, y `Termometro.MuestrearSonda` ya pinta «—» en el invitado. «Yo veo humedad aquí, tú calor allá» exige cinco bloques RLE más por chunk en ruta A, y el candidato declara «coste 0».

## 2. «Todas leen LabParams en vivo» no es cierto, y una tabla no basta

- Los umbrales de temperatura que cita (fibra 130, carbón 200) viven en `MaterialDef.ignitionTemp` (`Universe.Laboratorio.cs` l. 83 y 159: `CToRaw(140)`, `CToRaw(280)`), no en `LabParams`. `BandaTemp(raw)` sin material vecino no puede decir «a una banda de prender».
- El abono es un literal: `if (h >= 128)` (`SimStepper.Laboratorio.cs` l. 659). Es la deriva palabra/física que R142 y R145 ya pagaron, y el candidato la da por resuelta sin tocarla.
- «Sedimento: fertilidad 40/128» no es física: `LabPlanta` usa `carga` como factor continuo (l. 827). Esos números son `FertilU`/`MuyFertilU` de `Estado`, juicios de lenguaje. La banda inventaría un umbral que el motor no tiene: lo contrario del principio del candidato.
- `reposo` significa tres cosas: quietud del agua (3/24), «tiempo al rojo» de la arena con ceniza (l. 608-619: cuenta hasta `VidrioVisitas` 60 y se reinicia) y compactación (200), más una cuenta propia en la planta (l. 888). Un `BandaReposo(3/24/200)` global diría «compacta» a la arena a dos visitas del vidrio. La tabla es por material y contexto: se multiplica.

## 3. El tuning escondido

Radio 24 Chebyshev con tres sondas: 3 × 49² ≈ 7 200 celdas, el 3,3 % del mundo visible por campo. Ese número decide toda la «escasez» y no se verifica en banco. Agregado 4×4 por banda **máxima**: correcto para el fuego, incorrecto para la planta, que bebe solo si `hum[abajo] > PlantaHumedadMin` (l. 823-826); el bloque «húmedo» esconde la celda seca que la mata. Hasta 40 colores (8 × 5 campos) con alfa 150 sobre celdas de un píxel. Y la cuarta textura se rellena en `RenderChunk` (`SimRenderer.cs` l. 1085) solo en chunks sucios más un refresco cada 30 frames: el radio sobre roca dormida va 30 frames atrás. El «riesgo mayor» que declara es balance por playtest, lo caro. La prueba `banda(u) != banda(u-1)` solo verifica que la tabla es coherente consigo misma.

## 4. Versión mínima con apalancamiento

`LabBandas` (C# puro en `Sim/`) como **única fuente de umbrales**: `Estado` pierde sus constantes fijas y lee de ahí; el literal 128 pasa a `LabParams` y los `ignitionTemp` se consultan a `Universe`; `LabVistaColor` cambia rampas por bandas (la rampa negro→cian no enseña «≥ 60 germina»; la banda sí); el lector R140 dice la banda. Aire por fracción de `Saturacion(temp)`: lo único del candidato que corrige una mentira real (`AireSaturadoU = 200` fijo contra una saturación que depende de la temperatura). **Sin sondas, radio, FIFO ni agregado**: vista global como hoy. Una aserción por umbral, siete hashes intactos. Es prerrequisito de huella, registradoras y cuerpo-sensor: no compra decisiones, abarata todas las demás.

**Corregidos:** apalancamiento 3 (infraestructura de percepción, no ley), tuning 8 (paleta y semántica de banda por material), coste 0,5 semanas.
