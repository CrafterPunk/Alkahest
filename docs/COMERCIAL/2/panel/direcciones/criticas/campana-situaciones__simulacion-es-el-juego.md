# CRÍTICA · «DÍAS SIN MANOS» (campana-situaciones) · lente: LA SIMULACIÓN ES EL JUEGO

*(Panel de direcciones, segunda pasada, 2026-09-12. Crítico: director creativo purista de la
simulación. Leído: `campana-situaciones.md` entero; `01_LEYES.md` entero; `_huecos_y_combinaciones.md`
(origen de «velocidad como estado» y de «edad como vista»); las refutaciones de sello (las dos) y
de cuna (apalancamiento); `03_MERCADO.md`; `fable_direccion.md` (misma columna vertebral, para no
repetirla); las críticas hermanas de ingeniería e iteración humana (para no duplicarlas: aquí no se
repite el pasa-bajos del validador ni el coste de reloj del lote). Código: `LabBench.cs`
(`Escenarios` :76-88, `Correr` :265-335, la caldera cableada a `esAlambique` :306-311);
`SimStepper.cs` (:352-353 y :741: qué es `touchedTick`); `SimStepper.Laboratorio.cs`
(`LabInfiltrarHacia` :498-520, `LabManantial` :1004-1019, `LabTragar` :1026-1032, marchitez
:884-905); `LabParams.cs` (Caudal 24, Decantacion 6, ColmatacionPct 100, PlantaMarchitaVisitas 40,
tooltips :188-228); `AlkahestSim.cs` (:57-61, :370-400: `LabMultiplicador` y presupuesto de 20 ms).
Aritmética usada: día = 1 800 ticks = 60 s a 30 Hz = 6 s a ×10; horizonte de 30 días = 3 minutos
de mirar; visita = 8 ticks.)*

**Veredicto: SEGUNDA RONDA.** Es la dirección con la infraestructura de juicio más completa del
panel y el marco más delgado, pero su única decisión propia («tocar o confiar») está dominada por
una de sus propias reglas, sus relojes no están medidos, una de sus plantillas de condición se
cumple sola por una ley del manantial, y su prueba de banco no puede fallar. Tres correcciones
concretas la hacen finalista; ninguna toca la física.

## 1. Lo que es, dicho al pie de la letra: un árbitro, no un juego encima

Cero física nueva salvo «el hogar come». Todo lo demás (sello, condición como dato, balanza, firma
por ablación, escalera, solver, mutación, validador) es infraestructura para que las leyes
**juzguen**. Desde la pureza eso es lo mejor y lo peor a la vez.

Lo mejor: el marco es delgado de verdad. La condición es una lectura de la grilla y del libro, el
orden lo calcula el banco, no hay puntuación escalar ni economía ni desbloqueo por menú, y cualquier
ley futura entra en el recibo sin tocar el juez. Ningún diseñador vive en la puntuación. Y la
dirección acierta al degradar «velocidad como estado» de ley a presentación: el crítico de huecos la
propuso como ley («solo corre rápido lo que no tocas»), y un multiplicador por región rompería el
acoplamiento en la frontera de chunk; como estado de la mano es lo mismo sin mentir.

Lo peor: **la simulación aquí no plantea los problemas; los evalúa.** Los problemas los escribe un
autor (madre de 15 líneas + condición + horizonte) y una gramática de mutación, y la refutación de la
cuna lo deja fijado leyendo `LabManantial` y `LabTragar`: sin autor, las leyes dan tres atractores
(anegado, quemado, inerte). Es Zachtronics, no Dwarf Fortress: la sim como árbitro determinista de un
diseño, no como generadora de situaciones. Encaja con el encargo (situaciones pequeñas de autor,
preparar → soltar → observar), pero no se puede vender como «la simulación es el juego» sin este
matiz: el juego es la campaña; la simulación es su motor de reglas.

## 2. El core loop suficiente existe y es más pequeño de lo que la dirección dice

Con sello, condición, contador y velocidad como estado sobre una madre de autor ya se juega. Firma,
escalera, solver, mutación y validador son la **fábrica de contenido**, no el bucle. Bien: el bucle no
acumula features para existir, y el prototipo feo de la dirección (§9) lo reconoce al calcular la
firma a mano. Pero ese bucle mínimo tiene un defecto estructural que la dirección no ve.

## 3. El hallazgo principal: reiniciar gratis domina a tocar

La única decisión que la dirección reclama como propia («tocar o confiar, con reloj») está muerta
por sus propias reglas (§2 del documento): tocar devuelve a ×1, **pone el contador a cero y gasta
horizonte**; reiniciar es **gratis, instantáneo, devuelve el horizonte entero y borra los toques del
recibo**. La cámara entra envejecida por el banco e idéntica en cada intento: no hay estado acumulado
que perder. Luego quien vea la poza a punto de rebosar el día 8 de 15 nunca toca (le quedarían 7
días como máximo y un toque en el recibo): reinicia con la grava más ancha. «Tocar o confiar» se
convierte en «reiniciar hasta acertar», que es exactamente el examen con recuperación gratuita que la
dirección teme en §6. La Etapa 2 no lo detecta: «dos de tres reintentan tras un FALLA» se cumplirá
siempre, porque reintentar no cuesta nada, sin que nadie haya apostado nada.

La corrección es de una regla y es la más purista posible: **la cámara persiste**. Volver a entrar es
entrar en la misma cámara ya vivida (el registro sigue; el sello ya lo soporta: es el mismo diario,
más largo); el contador cuenta días sin manos a lo largo de la vida de la cámara; no hay FALLA, solo
«todavía no»; el peldaño se sube cuando la condición aguanta H días seguidos sin manos en cualquier
momento de esa vida. Es literalmente «DÍA N SIN MANOS» de Sin Manos aplicado a una cámara pequeña, y
convierte el intento en un lugar: el agua que ya entró sigue ahí, la grava que ya se colmató hay que
limpiarla, el carbón que ya se hizo se conserva. Tocar deja de ser castigo y pasa a ser lo que es en
un mundo: trabajo que se nota.

## 4. Sin reloj no hay apuesta, y los relojes no están medidos

Soltar solo tensa si algo se degrada mientras miras. En el sustrato medido hay **dos relojes con
escala**: la inundación (una sala de 128×72 con 24 celdas/s de manantial se llena en unos ocho
días, y se detiene sola: `LabManantial` **espera** cuando está rodeado, :1015) y el combustible (la
tolva arde 466 s sola, casi ocho días; «el hogar come» añade otro, con una tasa que es un número
humano). La colmatación de la grava y el relleno de la poza **no tienen escala medida**: lo que hay
es un tooltip (`LabParams` :188: «la poza se ciega en unos minutos» a 20 celdas/s y turbidez 40; y
:210: la grava «apenas se colmata») y una fórmula (`LabInfiltrarHacia` :512: finos = rate × carga
del agua × ColmatacionPct), nunca un banco que diga «día 8». El «hacia el día 8» de la narración es
narración, como ya avisó la primera pasada de P3. La planta muere en 40 visitas × 8 ticks ≈ 11 s sin
savia: es binaria el día 0, no un reloj. El humo vive 255 ticks. Una madre sin reloj tiene un
horizonte de tiempo muerto: tres minutos mirando una poza estable a ×10. Ese es el examen de verdad,
y hoy ni la Etapa 1 ni el validador lo miden. El validador acepta «vacío falla / autor cumple»; no
exige que **algo se degrade** entre el día 1 y H.

**Y una plantilla se cumple sola.** La situación 1 pide «sumido ≥ 90 % de lo emitido por día». Con la
pared intacta, el agua sube hasta rodear el manantial, el manantial espera y la emisión del día cae a
cero: 0 ≥ 0,9 × 0 es verdadero, y el registro vacío **cumple** por una ley que nadie escribió para
eso. El validador lo cazaría (vacío debe fallar), pero enseña que las «cuatro o cinco plantillas para
todo el juego» no son cuatro o cinco números: cada una tiene que ser inmune a los atractores del
sustrato (manantial que espera, sumidero que solo traga líquidos, hogar eterno), y eso es autoría
sobre la condición, no sobre la cámara.

## 5. La Etapa 1 no arriesga nada

Los nueve montajes del banco llevan su solución dentro: el horno **es** el recinto (18/18 contra 0
del hogar, ya medido), la carbonera **es** la boca de 1, el alambique **es** el serpentín más la
caldera. O el «registro vacío» ya cumple (y entonces no discrimina por construcción), o hay que
partir cada montaje en madre y registro, y entonces quien escribe las dos mitades garantiza que
discriminen. «5 de 9» pasa siempre. La ablación con los interruptores existentes sí vale (es la
primera medida de la firma), pero tampoco puede matar: si colapsan, se afinan los bits.

La prueba de banco que **sí** puede matar es la del reloj (§10).

## 6. Dos correcciones de código

**La vista Edad no es edad.** El documento promete «qué partes del mundo ya están en régimen»
leyendo `touchedTick`, pero ese campo es la **guarda de reentrada por tick** (SimStepper.cs:352-353,
`if (touchedTick[idx] == _tick) return 0; touchedTick[idx] = _tick;`): se escribe en toda celda
procesada de un chunk despierto, y `Move` lo vuelve a sellar (:741). Muestra qué chunks duermen; un
arroyo en régimen lee edad cero para siempre, y una poza quieta lee «vieja» aunque acabe de
formarse. Útil como «mapa de chunks dormidos», engañoso como «régimen». La edad honesta la da el
diff entre volcados consecutivos, que la dirección ya tiene.

**La firma corre la cámara bajo física falsa.** Apagar «agua» destruye el montaje antes de decir
nada sobre la lección; el greedy por inclusión lo absorbe (agua en el peldaño 1), pero la firma mide
**de qué ley depende el veredicto**, no **qué ley tiene que entender el jugador**. Es un proxy
razonable para ordenar; no es una garantía de legibilidad, y la escalera hereda ese hueco. (El coste
de reloj del lote y la granularidad del bitmask los cubren las críticas hermanas; no los repito.)

## 7. El clip de diez segundos y la frase

El clip bueno existe **y es un fracaso, no un CUMPLE**: 0-2 s, los pies del aprendiz oscuros por el
halo de Piel, el agua subiendo; 2-7 s, un golpe de cincel, el agua corre al sumidero, la mano sale
del cuadro, «DÍA 1… DÍA 9» mientras la grava ennegrece grano a grano; 7-10 s, «DÍA 11»: la línea
marrón cruza el sumidero; remate en dos palabras: «la grava se cansa». Cumple las cuatro condiciones
de `03` solo si las bandas están en pantalla y el grano de un píxel se ve a la distancia del clip;
con tierra y agua, la cámara es menos carismática que un brazo alquímico, y la dirección lo sabe. El
CUMPLE del minuto 2-4 (18 segundos, «3 días sin manos · 1 toque») no es clip: no hay consecuencia.
Eso confirma la tesis de §3-4: lo que se mira es el mundo degradándose, no el aprobado. Y el clip
del fracaso solo existe si la grava tiene reloj (§4): hoy no está medido que lo tenga.

La frase de tres oraciones no cabe en un tuit. «**Arregla la cámara, quita las manos y cuenta los
días que aguanta sin ti**» sí, y conserva el cartel «DÍA N SIN MANOS», que sigue siendo lo más
vendible del lote.

## 8. Multijugador

Relevo y bifurcación por fichero son comparar deberes: nada que hacer **juntos**, nada simultáneo.
Honesto, barato y sin roles, pero es el histograma de Zachtronics, no el clip co-op de `03`. La
cámara persistente (§3) al menos da al relevo algo real: B hereda el mundo que A dejó, no un volcado
de autor.

## 9. Condiciones para la segunda ronda

1. **Cámara persistente** en vez de intento con reinicio gratis; contador de días sin manos a lo
   largo de la vida de la cámara; sin FALLA; peldaño por racha de H días.
2. **Validador de reloj**: una madre se publica solo si la solución del autor, rejugada a 3×H, se
   cae en algún día (algo se degrada), o si el vacío se cae **después** del día 1 (no antes). Sin
   reloj, la situación va al sandbox como lámina, no a la escalera.
3. **Horizonte automático**: H = f × día de ruptura de la solución del autor (el banco lo encuentra;
   f es de la familia de plantillas, no por situación). Hoy el horizonte por madre es el mando de
   dificultad y lo fija una persona.
4. **Plantillas inmunes a los atractores**: cada plantilla se prueba contra el registro vacío en las
   nueve madres antes de aceptarse (la del 90 % cae por el manantial que espera).
5. **Etapa 1 reescrita** como prueba del reloj (§10), no como «5 de 9 discriminan».
6. Renombrar la vista Edad a lo que es (chunks dormidos) o sustituirla por el diff de volcados.

## 10. Prueba que la mata (más barata y con riesgo real)

**Banco, dos días, sin motor nuevo.** La intervención genérica tick-estampada en `Correr` (la de la
caldera, desacoplada de `esAlambique`, ya pedida por las refutaciones del sello) más una sonda por día
de la condición del montaje. Rejugar la solución de referencia de los nueve montajes a 30 días
(54 000 ticks) y anotar **el día en que la condición se cae**. Mata: si **siete o más nunca se caen**
(días sin manos = 30 desde el día 1), el sustrato no tiene relojes a escala de días y toda situación
es aprobado/suspenso en el día 1: examen por construcción, y ninguna presentación lo arregla.

**Personas, una sesión (la Etapa 2, con la medida corregida).** Tres personas, cinco montajes a ×10
(hace falta el selector de montaje en la grilla viva: los presets del panel son parámetros, no
geometría, como anotan las críticas hermanas), una de ellas con reloj visible («la grava se cansa»).
Se cuenta, además de lo que la dirección propone, **cuántas veces alguien toca a mitad de corrida
frente a cuántas reinicia**. Mata: cero toques a mitad en tres personas (la decisión está muerta, §3)
o nadie vuelve a entrar en una cámara que ya aprobó (es un examen, no un lugar).

## 11. Tiempos corregidos

- **Evidencia para matarla: 3 semanas.** El banco del reloj mata solo en una semana (dos días de
  banco más la intervención genérica y la sonda por día); la sesión que mata el examen necesita el
  selector de montaje, el contador y una línea del libro por día en pantalla, más el ciclo de
  compilar y desplegar de Cesar: tres semanas. La dirección decía dos, midiendo otra cosa.
- **Prototipo feo que permita juzgar el core: 7 semanas.** Sello mínimo con diario bajo las cinco
  puertas, volcado y carga, condición como dato, contador (3-4, según las dos refutaciones); cámara
  persistente y sonda por día (+1, es la misma cola); bandas en pantalla y scrub con diff (1); ocho
  madres partidas de los montajes del banco con reloj medido y firma a mano (1). Firma automática,
  solver y mutación quedan fuera del prototipo: son fábrica, no bucle.
- **Automatizable y paralelizable**: balanza, bandas, vistas, Q16, vidrio, hogar que come, solver,
  validador de reloj, horizonte automático, prueba de plantillas contra el vacío: todo banco.
- **Secuencial**: sello → CorrerSello → sonda por día → validador → firma → escalera (5-7 semanas en
  serie; el bitmask puede adelantarse).
- **Iteración humana incomprimible**: tres o cuatro sesiones para el core, más una **pasada de
  legibilidad por vista** (bandas, Ojo, Piel: tres tardes con desconocidos) y la del sandbox, que
  hereda el tramo manual de P4 sin medir y aquí se llama «hora 20» como si viniera gratis.

## 12. Iteración humana escondida (cuánto y dónde)

El horizonte por madre (mando de dificultad, hoy humano; automatizable con la condición 3); las
cuatro o cinco plantillas de condición, su factor f y su inmunidad a los atractores (humano, pequeño,
una vez, pero no «cuatro números»); la gramática de mutación y su rango (humano, pequeño); la
**legibilidad** de cada vista nueva y del recibo sin cifras (humano, incomprimible, tres o cuatro
tardes); el ritmo (6 s por día: nadie ha mirado tres minutos de poza); y el sandbox entero, que es P4
con su tramo manual sin medir. En total: 6-8 sesiones antes de una demo, no 3-4; sigue siendo poco
frente a cualquier dirección con contenido de autor.

## 13. Qué se automatiza en su lugar

Horizonte = f × día de ruptura (banco); validador de reloj (banco); plantilla contra el vacío en
las nueve madres (banco); hermana de contraste = el mutante cuya firma difiere en exactamente un bit
(banco, en vez de pares escritos a mano); «el día en que se torció» = primer día en que el vector
de la condición baja (auto-scrub, banco); detección de decisión muerta por situación = ¿algún
registro con un toque a mitad de corrida domina al mejor registro del tick 0? (banco, fuerza bruta
nocturna); regresión de todo lo anterior por hash de versión de física.

## 14. Órganos a conservar si se descarta

El sello con la condición como dato (columna vertebral de cualquier dirección con SOLTAR); «DÍA N
SIN MANOS» como propiedad de un mundo persistente, nunca como nota de un intento; la velocidad como
estado; el validador «vacío falla / autor cumple» con fragilidad y el validador de reloj; la firma por
ablación como ordenador de cualquier onboarding; la película con scrub y diff; el hogar que come;
`LabBandas` como única fuente de umbrales; envejecer madres por simulación; la intervención genérica
tick-estampada en `Correr`.

## 15. Rúbrica v2

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 6 | cero cruces físicos nuevos; convierte la física existente en ~15 lecciones, pero las únicas decisiones nuevas son cuándo soltar y cuántos toques, y la primera está dominada tal como está escrita |
| leyes ejecutan, revelan y juzgan | 8 | ejecutan y juzgan de verdad; revelan después de ocurrido (película, diff); los números humanos son horizonte por madre, plantillas (y su inmunidad a los atractores) y f |
| iteración humana (10 = poca) | 6 | el banco decide validez y orden; quedan horizontes, legibilidad de tres vistas, ritmo y el sandbox de P4 sin medir |
| verificabilidad automatizable | 9 | discriminación, resolubilidad, fragilidad, firma, orden, regresión, copias, y con esta crítica el reloj y las plantillas: todo headless |
| la simulación es el juego | 6 | marco delgado, pero la sim evalúa problemas que escribe un autor (tres atractores sin autor) y el bucle tiene forma de examen con recuperación gratis; 8 con la cámara persistente |
| onboarding garantizable | 8 | es el objeto de la dirección; la firma ordena, pero mide dependencia del veredicto, no legibilidad para el jugador |
| observabilidad | 7 | bandas, Ojo, Piel, scrub y diff valen; Edad no es edad; sin instrumentos físicos nuevos |
| tiempo como apuesta | 5 | la forma está (soltar, contador, sin rebobinar) pero reiniciar gratis domina a tocar y los relojes no están medidos; 8 con las condiciones 1-3 |
| multiplayer emergente | 5 | relevo y bifurcación por fichero: comparar deberes, nada que hacer juntos |
| profundidad por leyes estables | 6 | crece por ley (doce familias) y por madre; la campaña son 5-10 h; las 20-100 h son el sandbox de P4 |
| cuerpo del jugador | 4 | sensor y registro; el cuerpo ni decide ni accidenta |
| identidad comercial | 6 | «DÍA N SIN MANOS» y el clip del fracaso valen; la frase de tres oraciones no; sin clip co-op; base expediciones 60-150 k |
| dificultad técnica (10 = fácil) | 7 | C# puro en banco, acotado; el bitmask atraviesa todas las pasadas Lab* y el solver es fuerza bruta en serie |

Suma ponderada: 159 / 240. Puertas: pasa (iteración 6, apalancamiento 6).
