# INFORME FINAL — EL LABORATORIO DE LEYES

*(Borrador de Opus 5 en R149; revisado, corregido y cerrado por Fable 5.1 como arquitecto en R150,
2026-09-05. Rondas R130-R150. Alcance en `ALCANCE.md`: esto evalúa el laboratorio, no el juego
heredado que comparte ejecutable. Cada cifra lleva su benchmark; lo que dependía de una persona
delante va marcado **«no evaluado — ni aprobado ni refutado»**, y ninguna de las dos categorías se
disfraza de la otra.)*

**La pregunta de Cesar:** ¿puede este motor ser un sandbox donde aprender cómo funciona el mundo
permite construir procesos cada vez más poderosos, hasta que **conocimiento y geometría
sustituyen al trabajo manual**?

**Aviso que ordena todo lo demás.** Veintiuna rondas (R130-R150), diecisiete de ellas con
benchmark en `Laboratorio/benchmarks/`, midieron la **simulación**. Nadie ha jugado la build
nueva: la prueba con jugador se difirió (R25) porque hoy mediría una presentación provisional
(residuos del juego heredado, nombres a medias, plantas de un píxel, estado legible solo con F8),
no si hay juego. Todo lo que aquí se afirma es de la simulación.

---

# A · Lo que se construyó, y las cadenas que aparecieron

## A.1 · El sistema, en una página

Cuatro campos persistentes por celda (`humedad`, `carga`, `reposo`, `luz`), una pasada lenta que
recorre 1/8 de la grilla por tick al margen del sueño de chunks, y **una sola regla no local**: la
presión hidrostática por cuerpos de agua conectados. Sobre eso, 96 parámetros en vivo, **catorce
materiales nuevos** (ids 66-79: los doce de R130, la arenisca de R131 y el carbón de R135) y dos
reglas de dominio: el **aire de contacto** (quien arde sin un vecino de aire arde *en sordina*) y el
**tiempo al rojo** (la arena con fundente al lado se hace vidrio).

**Dónde vive el código.** La física nueva está tras `SimStepper.LabActivo` (catorce puertas en
`SimStepper.cs`) y en los partials `*.Laboratorio.cs`, más una costura documentada en el motor
compartido: contra `371dea4`, el último commit sin laboratorio, y contando líneas añadidas con
comentarios, +49 en `ProcessCombustion` (+50 con la convención escrita del HANDOFF; R24-12 lo
deja anotado), +13 en `ProcessBrasa`, +7 en `ProcessFire`, y +70 fuera de esos tres métodos. De
esa costura el diff es inerte fuera del laboratorio. Pero el laboratorio **también vive en
superficies compartidas que no saben en qué modo corren**: las tablas `LabMateriales.EsSolidoDelMundo`
y `Tallable` (leídas por el frasco, el cincel y el muñeco en todos los modos), `LabMateriales.Nombre`,
el director de audio y las herramientas del muñeco. Una ronda regresó otro modo por ahí (el vidrio
en la tabla de sólidos, R142, revertido en R145). La regla queda en `ALCANCE.md` §4: lo compartido
se toca comprobando que el otro modo no regresa.

## A.2 · Las cadenas que el sistema produce, medidas

| cadena | qué ocurre | dónde está medida |
|---|---|---|
| **El circuito del agua** | manantial → arroyo → poza → sumidero, cerrado y conservado | `h1_circuito_del_agua` |
| **La destilación** | agua hierve → vapor → toca roca fría → rocío → gotea | `r133_h3_completo` |
| **El serpentín elige dónde llueve** | el rocío va al vecino MÁS frío, no al primero: un bloque frío decide dónde cae el agua | `r135` (rocío en la roca: de repartido a **0**; al serpentín, 5 926 u) |
| **La decantación** | agua turbia en reposo deposita finos y se aclara (≈105 → ≈41 con los defaults vigentes; 96 → 45 con el reposo viejo) | `r133_h3_completo` |
| **La carbonera** | fibra tapada arde sin respirar y deja carbón en vez de ceniza | `r137` (pico **25,0 %** exacto) |
| **El horno hace vidrio; el hogar no** | 18 de 18 celdas de carga vidriadas dentro del recinto; **0** con el hogar solo; **1-2** celdas pegadas a una llama suelta | `r139`, `r138` |
| **La cadena de encendido** | el hogar (170 raw) no alcanza al carbón (200): hace falta yesca de fibra (130) | declarada en `r135`, refutada en `r137` (el hogar chispeaba por contacto), verdadera desde `r139` (R15: el hogar calienta, no chispea) |
| **La tolva se alimenta sola** | 466 s de fuego sostenido con **cero intervenciones** | `r139` |
| **Una pila maciza es su propia carbonera** | sus celdas interiores no tienen un vecino de aire, aunque esté al aire libre | `r135` |
| **La sombra del alambique** | la máquina que riega el huerto es la que lo deja sin luz | `r148` |

**Qué significa «no programada», con precisión.** Ninguna de las diez tiene una línea de código
que la produzca como tal: no existe `if (horno)`. Pero no son diez sorpresas, y conviene separar
tres cosas:

- **La regla programada.** Cada cadena tiene la suya, escrita a propósito: la presión por cuerpos
  conectados, la condensación en el vecino más frío (R8), el depósito por reposo, el aire de
  contacto (F1) con su residuo de carbón (F2), el tiempo al rojo (F5), los umbrales de ignición.
- **El aparato.** Alguien montó la geometría: el plano del nivel (manantial y sumidero, por
  decreto), el banco por código (alambique, horno, carbonera, tolva) o el pincel.
- **La cadena.** Siete de las diez estaban **previstas**: la regla se escribió para que ocurrieran
  (circuito, destilación, serpentín, decantación, carbonera, horno, tolva). Una se hizo verdad **con
  un parche** (la cadena de encendido, R15). Y **tres nadie las esperaba**: la pila maciza que es su
  propia carbonera, el alambique que ahogaba el huerto (R135) y el alambique que lo deja a oscuras
  (R148). Esas tres son el resultado central del laboratorio: consecuencias que cruzan dominios sin
  que nadie las escribiera.

## A.3 · La cadena cruzada, y la que no fue

El criterio 4 del diseño del fuego pedía **una de dos cadenas concretas**, aparecidas en la jugada
larga: humo × luz × plantas (cadena 9) o agua × luz × fuego × suelo del huerto-foso (cadena 6).

**No apareció ninguna de las dos.** La del humo se apagó a los cinco minutos: el fuego de la sala
se consume y nadie lo realimenta (15 celdas de humo en el minuto 5, 0 después). La del huerto-foso
(B-F6) nunca se corrió.

**Apareció otra, medida y con control.** El serpentín del alambique —31 celdas de núcleo frío en el
techo— tapa la boca del cielo, y la luz del lecho cae de 245 a **0** justo debajo de esa fila. Agua ×
luz × vida en un solo gesto: **quien construye la máquina de regar mata el huerto que riega**, y el
arreglo no es cambiar una regla, es mover la máquina. El control lo confirma: corrido a otro
sitio, la luz sube de 0 a 17 y germinan cinco veces más plantas (`r148`); con una boca de cielo
más ancha, la luz de la cara sube a 139-210 y germinan 9 contra 2 (`r150`).

> Para el criterio 4 del fuego: **½** — cruce observado en simulación autónoma, sin jugador, y no
> es una cadena del fuego. Con jugador: **no evaluado**.

## A.4 · Lo que NO se puede afirmar

- Si un jugador **descubre** estas máquinas sin que nadie se lo diga → **no evaluado**.
- Si el ritmo aburre, si los procesos lentos se sienten lentos → **no evaluado**.
- Si «hay juego» aquí dentro → **no evaluado**. Ni aprobado ni refutado.

La herramienta para evaluarlo (diario de sesión con claqueta y marcas, sonido del laboratorio,
guía de protocolo: `GUIA_H7.md`) está escrita y probada **en el editor**, no en la build, y tiene
seis correcciones conocidas pendientes (HF5e-A, R24) sin las cuales una sesión daría datos falsos.
Se termina cuando llegue la prueba de experiencia, después de la primera experiencia coherente.

---

# B · Emergente contra ruido: qué se quedó y qué se cayó

El encargo pedía justificar cada expansión. El criterio fue el del proyecto (R48): **verbo visible
+ consumidor real**, o es ruido.

**Se quedó** — cada regla sirve a varios fenómenos:

| regla | fenómenos que sostiene |
|---|---|
| `humedad` (un byte) | vapor en el aire · volumen de agua · agua en poroso · rocío en roca · savia de planta |
| `carga` | turbidez · colmatación · fertilidad |
| el aire de contacto | carbonera · brasa bancada · pila maciza · el horno que necesita recinto |
| `reposo` | decantación · tiempo al rojo del vidrio |

**Se cayó, medido:**

- **El tiro de chimenea.** Se emuló y **no existe**: el aire nunca se consume, así que abrir o
  cerrar la boca no regula nada. Subir el humo del carbón del 4 % al 40 % da una simulación
  idéntica al bit en la pila maciza a 9 000 ticks (`r136` §1); el humo que nace es marginal en todos
  los casos porque la llama, inmortal sobre combustible, ocupa la única celda por donde saldría.
  Paquete retirado y documentado en `DISENO_FUEGO.md` §10; la física quedó congelada.
- **El desagüe de grava.** Dos versiones. Con el riego real **encharcaba más** (26 de 36 columnas
  contra 7), porque un poroso solo suelta agua a un vacío al saturar y la capilaridad no llega: era
  una esponja sin salida dentro del lecho. Medido por Fable en `r141`, retirado en `r142`,
  aceptación repetida con el banco corregido en `r145`.
- **El churn erosión-depósito.** 6 279 eventos para el mismo neto de 375 celdas: ruido puro. El
  mando que hacía el trabajo era otro (`sed.depositoReposo`), y se midieron por separado (`r133`).

**Y una regla nueva se ganó el sitio por lo que destapó**: el aire de contacto produjo la
carbonera, la brasa bancada y el horno con una sola condición de vecindad.

## B.1 · El precio de medir de verdad

Tres veces el banco dijo algo falso y hubo que rehacerlo:

- El banco **medía otro universo** (le faltaba aplicar la química del laboratorio): el vapor vivía
  60 ticks en vez de 180 y condensaba a 90 raw en vez de a 65, así que el alambique del banco no
  podía destilar. Los hashes de Fable y de Opus **coincidían porque los dos medían lo mismo mal**
  (`r145`).
- El escenario «mundo entero despierto» pintaba arena, que se posa: a los 2 000 ticks daba **0
  chunks activos** (`r142`).
- El «90 % del calor viene de la llama» era falso, y la corrección a «¾» también: las dos cifras
  eran del libro **nominal**. En raw entregados la llama pone entre el 6 % y el 29 % (`r139`).

De ahí salió la regla que protege el banco: **los siete hashes son la licencia para optimizar lo
que el banco cubre**. Un cambio de rendimiento que los deje intactos no tocó la física de esos
escenarios; uno que mueva un solo hash es un cambio de física disfrazado.

---

# C · Valoración: ¿responde a la pregunta rectora? (Fable, R150)

**La mitad medible de la apuesta se cumple.** Un puñado de campos y de procesos lentos produce
cadenas largas con coste acotado: diez cadenas medidas, tres de ellas que nadie esperaba, con el
laboratorio entre 1,6 y 1,9 ms/tick según el escenario y el peor caso en 3,1 (aceptación: 12),
determinista al bit en la máquina donde se midió. El agua cumplió sus cinco criterios (H1-H3); el
fuego, cuatro de cinco (abajo). **El conocimiento construye procesos**: quien entiende que el aire
de contacto decide entre carbón y ceniza hace una carbonera con una boca de una celda; quien
entiende que el calor se escapa, lo encierra y hace vidrio; quien entiende que el rocío va al vecino
más frío, decide dónde llueve con un bloque.

**La otra mitad —que el conocimiento sustituya al trabajo manual— está a un eslabón, y el eslabón
es de geometría.** Lo que existe: procesos que corren solos durante minutos una vez montados. La
tolva mantiene el fuego 466 s sin intervención; el alambique riega treinta minutos seguidos si la
caldera tiene agua; el circuito del agua corre indefinidamente. Lo que falta, con precisión:

1. **Alimentación no manual.** El único suministro perpetuo del mundo es el manantial, y ningún
   aparato medido lo usa como entrada: la caldera del alambique la repone el código (o la repondría
   el jugador), la tolva se carga a mano, la carbonera es un lote.
2. **Recogida del producto.** Ningún aparato deja su producto en un sitio donde se acumule y se
   pueda tomar: la tolva acaba en ceniza, el agua del alambique cae al lecho o a la boca, el carbón
   se queda dentro de la carbonera.
3. **El ciclo cerrado** (huerto → fibra → tolva → alambique → huerto, B-F6) nunca se corrió.

Ninguno de los tres pide, por lo medido, una regla nueva: son canales, pozos y recipientes —
geometría de nivel— y se resuelven, o se demuestra que no, en la fase siguiente. Si alguno exigiera
mover sólidos, sería territorio de H6, que está congelado con su hipótesis escrita.

**Y una lección de diseño que vale más que cualquiera de las máquinas:** dos veces, con mecanismos
distintos, **el aparato que resuelve un problema crea el siguiente**. El alambique que riega, ahoga
(R135); el alambique que riega, ensombrece (R148). Eso no se diseñó: salió de que las reglas se
cruzaran. Si el juego tiene algo, está ahí.

**El dominio del fuego, con el criterio del propio diseño (`DISENO_FUEGO.md` §7):**

| criterio | nota | por qué |
|---|---|---|
| 1. Máquina escondida en las leyes | **✔** | el horno vidria la carga entera (18/18); el hogar solo, 0; una llama suelta, 1-2 celdas pegadas: el criterio se cumple **en cantidad**, no en absoluto |
| 2. Automatización sin jugador ≥ 5 min | **✔** | 466 s con cero intervenciones (tolva) |
| 3. Mando de geometría monótono | **½** | hay dos mandos, monótonos por tramos: el **recinto** (retención) y el **contacto** (pila fina ×5 de llama que la maciza, a igual masa y boca). El mando que pedía el criterio —la boca del recinto como válvula— **no existe** porque el aire no se gasta; medido, cerrado y congelado (R12) |
| 4. Cadena cruzada no guionizada | **½** | cruce observado en simulación autónoma (la sombra del alambique), pero no es ninguna de las dos cadenas del fuego que pedía el criterio, y con jugador no está evaluado |
| 5. Libro mayor de energía | **✔** | dos libros (nominal y entregado); la identidad de la carbonera cuadra a ±1 % con n ≥ 900 y a +2,5 % con 400 celdas, dentro del ±5 % |

**4 de 5**: uno más uno más medio más medio más uno. Lo que falta no es un criterio entero, sino
dos mitades de naturaleza distinta: una estructural (no hay tiro; física congelada, no se
recupera) y una no evaluada (la cadena con jugador). Con la prueba de experiencia, el máximo
alcanzable es **4,5**.

**Valoración en una frase.** La simulación tiene la profundidad sistémica que la tesis necesita,
con coste acotado y determinista donde se midió; el conocimiento construye procesos; que los
procesos sustituyan al trabajo manual está a un eslabón de geometría (alimentación y recogida) que
el laboratorio no montó; y si «hay juego» no está evaluado.

---

# D · Multiplayer

Análisis completo en `MULTIPLAYER.md`. Lo esencial:

- **Casi nada nuevo tiene que viajar.** Hoy solo `mat` se replica; el invitado es un espejo sin
  stepper. De los cuatro campos nuevos, `reposo` no se ve (0 bytes) y `luz` se recalcula local
  sobre el `mat` replicado (0 bytes). `carga` sale gratis como material virtual «agua turbia».
  `temp` y `humedad` solo si el invitado debe ver incandescencia y mojado: cuantizadas a 3-4 bits,
  en chunks con cambio y cerca de un avatar.
- **Los dos riesgos reales de divergencia no son físicos:** los 96 parámetros del panel y el
  multiplicador de tiempo. Autoridad del host y un RPC de «propongo valor».
- **Recomendación de Fable: ruta C ahora** (no replicar nada nuevo mientras el diseño se
  estabiliza; coste 0), con la **ruta A** como plan cerrado (solo-anfitrión con espejo enriquecido,
  que es lo que el motor ya es: 2-3 semanas de red más una de pruebas con dos máquinas) y la **ruta
  B** (lockstep, 6-10 semanas) solo si los invitados deben manipular calor y tallar a la par.

**Lo que los hashes prueban y lo que no.** El determinismo está probado al bit **en una máquina y
un entorno**: el editor del PC de Cesar. Siete hashes idénticos en dos corridas de la misma sesión
(`r145`) y tres de los siete idénticos entre sesiones en dos escenarios (`r144`, `r146`). El stepper
del laboratorio es aritmética entera sembrada con `XorShift`; la creación del universo usa `Mathf`.
**No está probado** en una build, en otra máquina ni en otra plataforma. Para la ruta A no hace
falta: el espejo no simula. Para la ruta B sería condición necesaria y habría que medirlo en dos
máquinas antes de prometerla.

---

# E · Qué costaría llevarlo al juego (Fable, R150)

Cada fila dice en qué se basa. La única con base en el repo es la de red; el resto es juicio de
arquitecto sobre lo que el laboratorio enseñó. Los meses son de calendario y de una persona: el
laboratorio hizo veintiuna rondas en tres días porque medía una simulación; la fase siguiente
está atada a pruebas con personas, y eso no se comprime.

Lo que ya está y no habría que rehacer: los cuatro campos, la pasada lenta, la presión, el fuego
entero, el panel de 96 parámetros, el banco con nueve escenarios y siete hashes por escenario. Y,
escritos y gateados pero **no listos**, el audio del laboratorio y el diario de sesión (HF5e-A
pendiente, sin prueba en la build).

| trabajo | por qué | estimación | base |
|---|---|---|---|
| **Presentación y observabilidad**: cámara, escala, capa visual sobre las celdas, plantas visibles, estado legible sin F8, nombres | hoy la simulación solo se lee con herramientas de desarrollador | **2-3 meses** hasta una primera experiencia coherente (diseño, implementación, una iteración) | juicio; Opus estimaba 1-1,5 y era optimista |
| **Que el huerto pueda vivir** | causa doble aislada: luz (7 de 73 caras) y reparto del riego (`r148`, `r150`) | días, dentro del diseño del nivel de la experiencia | geometría medida |
| **Una máquina que produzca sin volver a tocarla** | los tres eslabones de C (alimentación, recogida, ciclo) | **2-4 semanas** de geometría y nivel si no hace falta física; si hace falta mover sólidos, es H6 | juicio |
| **La prueba con jugador y lo que destape** | todo lo «no evaluado» depende de ella | 1 semana de preparación (HF5e-A, build, mezcla) + sesiones + **2-3 semanas por iteración**, y suele haber dos | juicio; herramienta en el repo |
| **Multiplayer** | §D | ruta C ahora: 0 · ruta A: 3-4 semanas (2-3 de red + 1 de pruebas) · ruta B: 6-10 | `MULTIPLAYER.md` |
| **Sólidos cohesionados (H6)** | congelado con su hipótesis escrita | 3-6 semanas, su propia etapa | juicio |

**Total hasta una primera experiencia coherente de un jugador, con una iteración tras la prueba,
sin H6 ni red: 4-6 meses de calendario de una persona.** Con dos iteraciones, 6-8. Red y sólidos
se suman aparte cuando se decidan.

---

# F · Lo que el laboratorio sabe y lo que no

## Lo que sabe (con su medida)

| hecho | número |
|---|---|
| El agua se conserva exactamente | residuo **0** a los 50, 150, 300 y 600 s |
| El circuito cierra | el sumidero traga las 20 celdas/s del manantial |
| La destilación funciona y se dirige | el serpentín se lleva **5 926 u** de rocío; la roca, 0 |
| El horno es una máquina real, en cantidad | **18/18** de carga vidriada; **0** con el hogar solo; 1-2 celdas con una llama suelta |
| Carbonizar no crea energía | pico de carbón **25,0 %**; identidad a ±1 % con n ≥ 900, +2,5 % con 400 |
| Un proceso se sostiene solo | **466 s** sin intervenciones |
| La geometría del combustible manda | pila fina: **×5** de llama que la maciza, a igual masa y boca |
| El tiro no existe | humo del carbón 4 % → 40 %: idéntico al bit en la pila maciza |
| Coste | **1,6-1,75 ms/tick** en el banco (laboratorio base); **~1,9** en el nivel en régimen; peor caso **3,1** |
| Determinismo | siete hashes idénticos en dos corridas (`r145`); tres de siete idénticos entre sesiones (`r144`, `r146`); **una máquina, un editor** |
| Multiplicador de tiempo | **×10 sostenido medido en Play** (R131); ×12 deducido del presupuesto de 20 ms con el banco (`r145`); el techo por encima de ×10 no está medido en Play |
| El encharcamiento | **0 de 36** columnas anegadas desde el minuto 15 con el alambique sobre la boca (`r148`); 22/36 y 17/36 con el serpentín corrido: el reparto del riego sigue siendo el mando |
| Por qué no vive el huerto | luz: 7 de 73 caras por encima del mínimo (`r148`); con luz, la humedad de la cara bajo goteo oscila entre 50 y 99 con el mínimo en 60 (`r150`) |

## Lo que no sabe

| pregunta | estado |
|---|---|
| ¿Alguien descubre las máquinas sin ayuda? | **no evaluado** |
| ¿Esto entretiene? | **no evaluado** |
| ¿Cuánto se espera antes de aburrirse ante un proceso lento? | **no evaluado** |
| ¿El sonido ayuda? | **no evaluado** (existe, no medido, no listo) |
| ¿Puede vivir un huerto? | **no en el nivel de referencia**, por causa doble aislada; la mecánica sí responde (germinación ×4,5 con luz) — geometría, cerrada en R26 |
| ¿Es determinista en otra máquina o plataforma? | **no medido** |
| ¿Sirven los sólidos cohesionados? | hipótesis escrita, **congelada** |
| ¿Aguanta en red? | analizado, **no implementado** |

## Qué pasa a la fase comercial (no se arregla en el laboratorio)

1. **El huerto que vive**: bocas de cielo, reparto del riego y caldera, como diseño del nivel de
   la experiencia, con las dos cifras de partida (`r148`, `r150`).
2. **La máquina que produce sola**: alimentación desde el manantial, recogida del producto y el
   ciclo cerrado, como geometría; H6 solo si la geometría no basta.
3. **La prueba con jugador**, con HF5e-A, la build y la mezcla, después de la primera experiencia
   coherente. Todo lo «no evaluado» se decide ahí.

---

# G · Cierre formal del laboratorio (Fable 5.1, 2026-09-05, R150)

**Queda cerrado:**

- La **física**, congelada desde R141: ninguna regla ni número de simulación cambia sin decisión
  de Cesar. H6 (sólidos cohesionados) congelado con su hipótesis en `DISENO_LABORATORIO.md` §8.
- El **nivel de referencia** (`BuildLaboratorioDeLeyes`), tal como está: es el plano de veintiuna
  rondas y de los hashes del banco; no se le abren bocas ni se le mueven lechos (R26).
- El **banco** (`Sim/LabBench.cs`, nueve escenarios, siete hashes por escenario, menú «Ten Thousand
  Years/8») como licencia para optimizar lo que cubre, en la máquina donde se mida.
- **H7 con jugador**, diferido a después de la primera experiencia coherente; su herramienta
  (diario, sonido, guía) espera HF5e-A y la prueba en la build.
- Las **decisiones** con su porqué: `CHECKPOINT.md` (D1-D36), `PREGUNTAS_A_FABLE.md` (Q1-Q16,
  R1-R26), `DISENO_FUEGO.md` §10, `ALCANCE.md`.

**Queda pendiente, sin efecto en las conclusiones** (contabilidad del banco, para cuando se
retome): los docblocks de `LabBench.cs` que aún dicen ocho escenarios y tres hashes, la tabla de
siete columnas regenerada desde `Informe()` con el hash del arco largo publicado, y el recuento
+49/+50 de la costura.

**Lo que este laboratorio deja demostrado**, en una línea cada cosa: el agua se conserva al bit
y cierra su ciclo; el fuego respira por geometría y su libro cuadra; la simulación produce cadenas
que cruzan dominios sin que nadie las escriba; el coste está acotado; el determinismo aguanta
donde se midió. **Lo que no demuestra**: que alguien lo descubra, lo entienda y lo disfrute. Eso
no es de este laboratorio, y por eso se cierra aquí.

---

## Anexos

- Benchmarks: `Laboratorio/benchmarks/` (17 archivos, R130-R150; R140, R143 y R147 no midieron
  física).
- Presets y snapshots reproducibles **del ciclo del agua**: `Laboratorio/presets/ref_h1_circuito`,
  `ref_destilacion`, `ref_alambique`. El fuego no tiene presets `ref_*`: sus geometrías (horno,
  carbonera, tolva, arco largo) se reproducen desde `Sim/LabBench.cs`.
- Banco: `Sim/LabBench.cs`, nueve escenarios con siete hashes · menú «Ten Thousand Years/8».
- Decisiones de diseño con su porqué: `CHECKPOINT.md` D1-D36.
- Escalamientos y respuestas: `PREGUNTAS_A_FABLE.md` (Q1-Q16, R1-R26; Q16 cerrada en R26).
- Alcance y qué NO es este laboratorio: `ALCANCE.md`.
