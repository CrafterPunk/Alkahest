# CRÍTICA · «El Pozo Sellado» (mutacion-pozo) · lente: LA SIMULACIÓN ES EL JUEGO

*(Panel de direcciones, segunda pasada, 2026-09-12. Director creativo purista (Dwarf Fortress,
Noita, Powder Toy). Único tema: ¿la simulación ES el juego o hay un juego tradicional encima? ¿Existe
el core loop suficiente sin acumular features? ¿Las leyes ejecutan, revelan y juzgan solas, o hay un
diseñador escondido en la puntuación, la progresión o el contenido? ¿Es divertido minuto a minuto o
es un examen? ¿Cómo se narran el clip de diez segundos y la frase? Leído entero antes de escribir:
`mutacion-pozo.md`, `01_LEYES.md`, `03_MERCADO.md` §4-6, `00_ENCARGO_Y_CRITERIO.md` §1 y §3, las dos
críticas hermanas de esta dirección (ingeniería, iteración humana), y en el código
`SimStepper.Laboratorio.cs` (LabAgua :436-490, LabInfiltrarHacia :497-520, LabPlanta :789-906,
LabDifusionTermica :1300-1345), `SimStepper.cs` (ProcessGas :1443-1625), `LabParams.cs`,
`LabMateriales.cs` (EsFondo :98, Tallable :43), `CellGrid.cs` (ambient :96-116), `LabBench.cs`
(los nueve escenarios) y CHECKPOINT §6f, HF1, R150. Lo que afirmo del código lleva línea.)*

**Veredicto: SEGUNDA RONDA.** Ni finalista ni descarte. Tiene el mejor gesto de compromiso del panel
(abrir la garganta es abrir el grifo: irreversible, físico, legible sin voz) y el mejor generador de
situaciones encadenadas (la salida de un tramo es, en materia, la entrada del siguiente). Pero la
mitad de la frase que la convierte en juego y no en «sandboxes apilados con cronómetro» —lo que sube
del tramo nuevo y los relojes lentos cobran la promesa— está contradicha por el código y por dos
bancos; y el «receptor» de la promesa, que la dirección vende como la diferencia entre juego y
examen, es en el código un libro contable: ninguna ley consume lo que se promete. Vuelve cuando una
prueba honesta de un día diga si el pozo acopla hacia arriba, y con un receptor que sea una ley.

## 1. ¿La simulación es el juego, o hay un juego encima?

No hay juego tradicional encima, y eso hay que decirlo primero: sin economía, sin inventario, sin
puntuación escalar, sin árbol. La promesa es una cláusula sobre contadores que el libro ya escribe;
el sello es «este tramo cuenta días»; comparar es dominancia por columna. Como capa de datos es la
más delgada del panel y cumple la función objetivo con honestidad.

El diseñador escondido no está en la puntuación: está en la **arquitectura por decreto** y en el
**juez**.

*Arquitectura.* Cuatro constantes que no existen en `LabParams.cs` deciden si el juego ocurre en
tiempo de juego: el día (`DiaTicks` 1800: no está en el código, es una elección de 60 s), la altura
del tramo (96 filas), el ancho de la garganta (3) y el grosor del suelo de roca (no se fija, y es el
mando del acoplamiento térmico: con `KRoca` 2, `CRoca` 3 y el tirón de 1 raw cada 32 ticks hacia
`ambient[]`, :1335-1339, cualquier fuente muere a 4-6 celdas de roca). Y una quinta que es la más
purista de las objeciones: el gradiente `ambient` por fila (5 °C arriba, 45 °C abajo). `CellGrid.ambient`
existe (:116) y hoy es uniforme (:210); pintarlo por fila es gratis, pero es exactamente el clima
por decreto que la regla R31 retiró: el fondo está caliente porque el constructor lo dice, y cada fila
vuelve a su temperatura decretada por un tirón que ninguna ley produce. Es geología, y la geología es
legítima; pero no es «las leyes ejecutan», es un pin más, con la misma naturaleza que el hogar y el
núcleo frío que la dirección critica.

*El juez.* La dirección dice que «la promesa es materia que alguien recibe» y que por eso no es un
marcador. En el código no hay nadie que la reciba: **el carbón no tiene consumidor** (el paquete D
«el hogar come», el único que le da uno, no está en la dirección); **el agua clara** solo la bebe una
planta (`LabPlanta` :822-833) y la planta de referencia nunca vivió (R150: nueve nacidas, todas
mueren); **la luz** solo la leen la germinación (:704) y la punta que crece (:870-873). Lo que recibe
la promesa es la balanza del fondo: un asiento. En solitario, además, el tramo de abajo lo cava el
mismo que prometió. El juez es un libro que uno se pone y se corrige: el examen que el §6 temía, con
la piedra como boletín.

## 2. ¿Existe el core loop sin acumular features?

La primera mitad del loop (cavar con lo que cae, prometer, sellar, abrir la garganta) existe hoy y es
buena. La segunda mitad, la que hace pozo al pozo, no ocurre en el código:

- **Nada sube.** El calor no cruza un suelo de roca (aritmética sobre :1315-1345; el ingeniero la
  lleva a banco). El humo sube una celda por tick y vive 255 ticks (510 bajo techo, :1497-1503), pero
  bajo el techo del tramo deambula por `GasBolsaLateralPct` y solo cruza la garganta si el fuego está
  en su vertical; R148 lo midió con un fuego de 80 celdas: 15 celdas de humo sobre el lecho en el
  minuto 5 y 0 después. Lo único que humea días es la tolva (466 s ≈ 8 días de 60 s) y nadie la
  repone hasta V. **La planta viva es inmune a la oscuridad**: `LabPlanta` solo mata por savia a cero
  durante `PlantaMarchitaVisitas` (:889-906) o por perder el sustrato (:797-803); con la raíz húmeda
  sobrevive a `luz = 0` indefinidamente. La cláusula «planta viva en (x,y)» no la rompe el humo; la
  «primera culpa» de la primera hora es falsa.
- **Nada baja más allá del tramo 2.** `LuzDecayCielo` = 1 por fila (:126): la luz del cielo muere en
  la fila 255. En una columna de 768 filas los tramos 4-8 nacen a oscuras; «la luz baja por siete
  gargantas» (hora 20) pide L o poner el decaimiento a cero, que es quitar el dilema.
- **Los relojes no viven entre el día 1 y el 60.** Colmatación de la grava: `finos = (int)(rate ·
  carga / 255)` (:512) con `Infiltracion` 32 y grava 90 da 11 u/visita al inicio y 1 fino por visita
  con agua de manantial (carga 40); el truncamiento entero la detiene cuando `rate` baja de 7, hacia
  carga ≈ 69: la grava se cansa un 27 % y nunca más. Con agua de erosión (carga 255) ciega en unos
  80 visitas, 22 s. Depósito: carga ≥ 200 y 24 visitas quieta (:466): 6 s de agua turbia. Con agua
  clara, ninguno de los dos existe: **el tramo que cumple su promesa es, por construcción, el que no
  envejece**. Los días 6, 30, 38 y 48 de la narración los pone `DiaTicks`, no una ley.

Lo que queda es real: una **tubería de un sentido** con tres transformaciones (turbia → clara; agua
→ vapor → goteo; fibra → carbón) y un contrato entre etapas. Es un Sandustry vertical sin cintas. Es
un juego; no es el juego que el documento vende. Y en la primera ola (J + aforo + tramos + bolsas +
C: ninguna física nueva) solo el tramo de agua funciona: el fuego es llama inmortal o tolva que muere
el día 8 hagas lo que hagas (sin V no hay quien la alimente: eso no es un dilema, es una tarea); el
frío probablemente graniza (nadie contó `Freeze`); el huerto nunca vivió. El core que el prototipo
feo permitiría juzgar es «el tramo de agua, ocho veces», y es el eterno.

Un cruce que la dirección no usa y que sí está: `LabLuzDesde` devuelve 0 en sólidos (:1272) y una
poza de cinco filas cuesta 100 de luz (`LuzDecayAgua` 20). **Filtrar cuesta luz**: el tapón de grava
que aclara el agua apaga al tramo de abajo. Esa es la única promesa contra promesa que hoy cobra una
ley, y no está en el documento.

## 3. ¿Divertido minuto a minuto, o examen?

Los diez primeros minutos son creíbles y contienen el mejor momento de todas las direcciones: tallar
tres celdas de suelo bajo la poza y que toda la poza caiga de golpe, turbia, al tramo vacío. Después
de eso el minuto a minuto es el del laboratorio (tallar, verter, poner, mirar) más una vigilia que la
dirección llama «lectura, no espera» y que es leer una hoja de cálculo de diez columnas con nombre
diegético (la piedra: agua clara y turbia, calor con signo, finos, carbón, ceniza, semilla, humo, luz,
más cláusulas de grilla). El F8 vuelto objeto del nivel. Nada se pinta sin F8 hasta L, que es segunda
ola; la piedra rajada es un sprite que hoy no raja nada porque nada cobra.

El purista no objeta la espera: Dwarf Fortress es espera. Objeta la espera **sin adversario**. En
solitario el receptor eres tú; en co-op «no toques el 3» es una norma social alrededor de un
contador, y romper un sello ajeno es culpa sobre un número. El examen con testigos sigue siendo un
examen. Lo que lo volvería juego no es balance: es que el veredicto lo dé una ley en vez de un libro.
«El hogar de abajo se apagó porque no bajó carbón» (D, 0,5-1 semana, tuning 8) y «la planta de abajo
vive porque bajó agua» (el día de Q16, luego V) son juicios sin umbral, ni de autor ni de jugador. La
dirección deja D fuera y manda V a la tercera ola: construye seis semanas de tribunal (J) antes de
que exista un acusado que pueda morir.

## 4. El clip y la frase

**El clip que es verdad hoy** (sin una ley nueva): 0-2 s, un muñeco sobre una poza parda, un cincel;
2-7 s, tres golpes en el suelo, la poza entera cae por el agujero al tramo negro de abajo y lo
inunda; 7-10 s, la piedra marca «300 turbias · 0 claras», el muñeco mira hacia arriba. Remate: «la
garganta». Cumple las cuatro condiciones de `03 §4`: estado legible sin voz, tres eslabones físicos,
consecuencia irreversible y atribuible, palabra nombrable. Es un clip hacia abajo: el compromiso
funciona porque soltar el agua es irreversible, no porque algo suba a cobrar.

**El clip que vende y no es verdad**: la piedra que se raja vista desde la lumbrera. Depende de que
un tramo nuevo amenace al sellado en días, y hoy no ocurre.

**La frase**: «Cava un pozo tramo a tramo. Cada tramo que sellas sigue corriendo sin ti y le entrega
al de abajo lo que prometiste.» Buena y cierta hasta «entrega». «Al de abajo» es nadie: ni una ley ni
una persona (en solitario) recibe. Cuando D y una planta que vive existan, «al de abajo» será el
hogar y el huerto, y la frase será verdad entera.

## 5. Riesgo mayor (esta lente)

Que el juez sea un libro y no una ley. Nada en el código consume lo prometido (carbón sin consumidor,
agua sin bebedor vivo, luz sin planta que la lea salvo para nacer) y nada de abajo amenaza a lo de
arriba (el calor no cruza roca, el humo cruza minutos, la planta viva no muere de oscuridad, la
colmatación se para o termina en segundos). El pozo es una tubería de un sentido con un cronómetro
por tramo: el examen que el §6 nombra, con testigos en co-op. No lo arregla ningún balance; lo
arregla un receptor que sea una ley y una prueba que demuestre acoplamiento hacia arriba.

## 6. Iteración humana oculta

La dirección se pone 7. Le pongo 6: (a) cinco constantes de arquitectura (día, tramo, garganta,
grosor del suelo, gradiente) que deciden si algo ocurre en tiempo de juego y se eligen a mano; (b)
seis tramos de campaña con promesa de autor y **registro de solución** (el validador por veredicto
exige que el registro del autor cumpla: alguien resuelve cada tramo, dos veces por reescritura:
12-18 resoluciones); (c) tres de los seis tramos descansan en física sin verificar (Q16, `Freeze`,
llama inmortal) y el tramo 4 promete «planta viva» cuando R150 acaba de medir que las nueve nacidas
mueren; (d) la piedra como interfaz (columnas, número por defecto) es el gesto que más playtest come y
no es física; (e) la sesión que decide «¿juego o examen?» no la sustituye ningún hash. Cinco a seis
semanas de calendario, no tres o cuatro.

## 7. Qué se automatiza en su lugar

- **Receptor-ley en vez de receptor-libro**: D «el hogar come» (0,5-1 sem) y el día de Q16 antes de
  J; la promesa «carbón ≥ 30/día» pasa a ser «el hogar del tramo k+1 sigue encendido el día D» y
  «agua clara ≥ N» a «la planta del k+1 vive»: veredicto físico, sin umbral. El libro queda como
  recibo, no como juez.
- **Matriz de acoplamiento vertical** en banco: para cada par (montaje arriba, montaje abajo) de los
  nueve escenarios, qué cruza la fila del suelo por día en ambas direcciones (agua, finos, humo,
  calor, luz). Los pares que no se hablan no se ponen seguidos; ninguna campaña se escribe antes.
- **`DiaTicks` derivado, no elegido**: el valor que hace que el reloj más rápido medido dure ≥ 5 días
  y el más lento ≤ 60. Dos escenarios, un número, sin sensación.
- **Cláusula de eternidad** en el validador: el montaje del autor sin tocar debe fallar antes del
  día D. Detecta el «sandbox con cronómetro» sin jugarlo.
- **Umbrales de campaña por percentil** del recibo del registro del autor, nunca tecleados.
- **Promesa por defecto desde el nacimiento**: la etiqueta de la bolsa envejecida como promesa
  automática («mantén lo que ya pasaba»).

## 8. La prueba más barata que la mata (corregida)

La del §8 se mataría por montaje: la carbonera de boca 1 es la geometría que HF1 midió con **0 humo,
0 llama, 0 ceniza** fuera del recinto, y el suelo de roca no tiene grosor. Un día de banco, sin
código salvo montajes y una fila de muestra:

1. **«Dos tramos, honesto».** Arriba la cámara de R150 (boca de 25 columnas, serpentín fuera de la
   boca: la única que dio 9 nacidas). Abajo, bajo una garganta de 3 alineada con la boca, la **tolva
   del banco** (360 celdas de fibra, boca 3: la única fuente de humo que dura días), y una variante
   con la tolva desplazada 20 columnas; aparte, un horno de 255 raw bajo suelos de roca de 1, 2 y 4
   celdas. 18 000 ticks. Por día: fracción de caras del lecho con `luz[i+W] < 40`, `temp` del lecho y
   del aire sobre cada suelo, nacidas, muertas, vivas. **Mata** si con la tolva ardiendo la mitad de
   las caras nunca queda bajo 40 un día entero **y** el lecho sobre el suelo de 1 celda no sube 8 raw.
   **Salva de verdad** solo si la variante desplazada no separa de la alineada (si separa, el
   acoplamiento es geometría fina que habrá que balancear).
2. **«Promesa sin receptor»** (mi lente, media tarde sobre el mismo montaje): a mitad de la corrida
   se corta lo que baja al tramo inferior. Si en los diez días siguientes ninguna magnitud del tramo
   de abajo cambia salvo el libro (nada muere, nada se apaga, nada deja de nacer), el receptor es el
   libro y la promesa es un examen.
3. **«Tapón de grava»** (media tarde): poza de manantial (carga 40) y poza de erosión (carga 255)
   sobre un tapón de grava de 3×4 en una garganta; por día, exudado bajo el tapón y carga media del
   tapón. Predicción por lectura: con 40 se para en carga ≈ 69; con 255 ciega en ~80 visitas. Si se
   cumple, no hay reloj entre el día 1 y el 60.

## 9. Tiempos corregidos

| tramo | dirección | corregido | por qué |
|---|---|---|---|
| evidencia para matarla | 0,5 sem | **0,5-1 sem** | las tres pruebas de §8 más el día de Q16 y la tarde de `Freeze`, banco actual |
| prototipo feo que permita juzgar el core | 6,5 sem | **5 sem** con la rodaja: tramos + `ambient` (0,5) ‖ aforo como contadores con los seis ganchos (1) → promesa mínima sin diario (0,5) ‖ D (0,5-1) ‖ A entrega A (1) ‖ C sensor + Piel (1); tres tramos a mano con umbral derivado. **7,5-8 sem** de calendario si J entero va primero | la pregunta «juego o examen» no necesita el tribunal; necesita un acusado que pueda morir |
| iteración humana | 3-4 sem, 4 sesiones, 1 reescritura | **5-6 sem**, 6-8 sesiones, 2 reescrituras | §6 |

## 10. Órganos a conservar si se descarta

1. **Abrir la garganta = abrir el grifo**: el compromiso físico irreversible, el mejor SOLTAR local
   del panel; vale para cualquier recinto con salida.
2. **El encadenamiento vertical**: la salida de una situación es, en materia, la entrada de la
   siguiente. Generador de campañas encadenadas para «El Recibo» o «leyes-juzgan».
3. **La línea de aforo a nivel de unidad** (con los seis ganchos que el ingeniero encontró:
   `Move`, presión, infiltración, percolación, exudación, conducción; y Σcruces == Δinventario).
4. **La promesa escrita por el jugador sobre una columna del aforo**: el único umbral del panel sin
   autor, a condición de que la reciba una ley.
5. **Sellado sin barrera**: SOLTAR local como contadores por región sobre el diario.
6. **La piedra rajada**: veredicto diegético legible a distancia.
7. **Bolsas envejecidas + validador por veredicto + cláusula de eternidad**.
8. **El pozo por fichero con tramos sellados de otro**: asíncrono con contenido ajeno.
9. **«Filtrar cuesta luz»**: el único cruce promesa-contra-promesa que el código ya cobra.
10. **Dos hechos del sustrato**, con o sin pozo: el truncamiento entero de `LabInfiltrarHacia` que
    detiene la colmatación al 27 %, y el grosor de roca como mando discreto del calor.

## 11. Puntuaciones (rúbrica v2)

| eje | nota | por qué |
|---|---|---|
| apalancamiento_sistemico | 6 | aforo, tramos y promesa son baratos y convierten cada ley en recibo; el acoplamiento que los haría juego es hacia abajo, y el hacia arriba pide A, L, V; el valor grande es J, que comparten cinco direcciones |
| leyes_ejecutan_revelan_juzgan | 6 | ejecutan y revelan con fecha; juzgan por umbral del jugador, pero el cobro es un asiento: ninguna ley consume lo prometido y la planta viva no muere de oscuridad |
| iteracion_humana | 6 | cinco constantes de arquitectura, 12-18 resoluciones de autor, la piedra como interfaz, la sesión del examen |
| verificabilidad_automatizable | 8 | todo lo de la primera ola va al banco; el validador necesita registros de autor |
| simulacion_es_el_juego | 6 | sin capa tradicional; con arquitectura por decreto (tramos, gradiente, día) y el juez como libro; en la primera ola el core es el tramo de agua ocho veces |
| onboarding_garantizable | 7 | seis tramos validados es el mejor onboarding del panel; tres de seis sobre física sin verificar; la cláusula de eternidad es obligatoria |
| observabilidad | 5 | la piedra es una hoja de cálculo; nada se pinta sin F8 hasta L; Piel ayuda; la piedra rajada no raja |
| tiempo_como_apuesta | 8 | la garganta es la mejor apuesta del panel; sellar cuenta; pero cuenta sin adversario |
| multiplayer_emergente | 6 | norma social sobre un contador; asíncrono por fichero real en cuanto haya volcado |
| profundidad_por_leyes_estables | 5 | tubería de un sentido con tres transformaciones; eterno o de minutos; la profundidad está en A, V y L |
| cuerpo_del_jugador | 5 | sensor y tinte; la poza que cae sobre el cuerpo es el único accidente propio |
| identidad_comercial | 7 | frase buena, clip de la garganta verdadero hoy; «pozo con cronómetro» hasta que algo suba y algo se pinte |
| dificultad_tecnica | 7 | acotado; J es larga; el aforo a nivel de unidad y el rebase del banco al girar la rejilla son coste, no riesgo |

Pasa las dos puertas (6 y 6). No es finalista por evidencia: su motor no está en el código y su
receptor es un libro.

## 12. Posición

Segunda ronda con dos condiciones concretas. Primera, la prueba honesta de §8 en un día: si el humo
de la tolva cruza la garganta y oscurece el lecho de R150 en tiempo de días, y la variante desplazada
no lo cambia, el pozo tiene el motor que dice tener. Segunda, un receptor que sea una ley: D dentro de
la primera ola y el día de Q16 antes de escribir una sola promesa de autor. Con las dos, sube a
finalista con la rodaja de cinco semanas y J detrás del primer juicio, no delante. Sin la primera, la
dirección honesta es la tubería de un sentido y sus órganos 1-9 se injertan en «leyes-juzgan» como
generador de situaciones encadenadas, donde la promesa como contrato entre etapas vale más que como
examen que uno se pone a sí mismo.
