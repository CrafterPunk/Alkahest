# INFORME FINAL — EL LABORATORIO DE LEYES

*(Borrador de Opus 5, 2026-09-06, rondas R130-R148. Fable 5.1 revisa; lo marcado **[FABLE]**
espera su segunda opinión. Alcance en `ALCANCE.md`: esto evalúa el laboratorio, no el juego
heredado que comparte ejecutable.)*

**La pregunta de Cesar:** ¿puede este motor ser un sandbox donde aprender cómo funciona el mundo
permite construir procesos cada vez más poderosos, hasta que **conocimiento y geometría
sustituyen al trabajo manual**?

**Aviso que ordena todo lo demás.** Dieciocho rondas midieron la **simulación**. Nadie ha jugado
todavía la build nueva: la prueba con jugador se difirió (R25) porque hoy mediría una presentación
provisional, no si hay juego. Por eso este informe separa dos cosas que suelen confundirse:

- lo que está **medido** lleva su benchmark;
- lo que dependía de una persona delante va marcado **«no evaluado — ni aprobado ni refutado»**.

Ninguna de las dos categorías se disfraza de la otra.

---

# A · Lo que se construyó, y las cadenas que aparecieron

## A.1 · El sistema, en una página

Cuatro campos persistentes por celda (`humedad`, `carga`, `reposo`, `luz`), una pasada lenta que
recorre 1/8 de la grilla por tick al margen del sueño de chunks, y **una sola regla no local**: la
presión hidrostática por cuerpos de agua conectados. Sobre eso, 96 parámetros en vivo, quince
materiales nuevos y dos reglas de dominio: el **aire de contacto** (quien arde sin un vecino de
aire arde *en sordina*) y el **tiempo al rojo** (la arena con fundente al lado se hace vidrio).

Todo el código del laboratorio está aislado tras `SimStepper.LabActivo`, en archivos
`*.Laboratorio.cs`, más una costura documentada en el motor compartido (+49 líneas en
`ProcessCombustion`, +13 en `ProcessBrasa`, +7 en `ProcessFire`). Fuera del laboratorio el diff es
inerte.

## A.2 · Las cadenas que el sistema produce, medidas

| cadena | qué ocurre | dónde está medida |
|---|---|---|
| **El circuito del agua** | manantial → arroyo → poza → sumidero, cerrado y conservado | `h1_circuito_del_agua` |
| **La destilación** | agua hierve → vapor → toca roca fría → rocío → gotea | `r133_h3_completo` |
| **El serpentín elige dónde llueve** | el rocío va al vecino MÁS frío, no al primero: un bloque frío decide dónde cae el agua | `r135` (rocío en la roca: de repartido a **0**; al serpentín, 5 926 u) |
| **La decantación** | agua turbia en reposo deposita finos y se aclara (turbidez 96 → 45) | `r133_h3_completo` |
| **La carbonera** | fibra tapada arde sin respirar y deja carbón en vez de ceniza | `r137` (pico **25,0 %** exacto) |
| **El horno hace vidrio; el hogar no** | 18 de 18 celdas de carga vidriadas dentro del recinto; **0** con el hogar suelto | `r139` |
| **La cadena de encendido** | el hogar (170 raw) no alcanza al carbón (200): hace falta yesca de fibra (130) | `r139`, corregida en R145 |
| **La tolva se alimenta sola** | 466 s de fuego sostenido con **cero intervenciones** | `r139` |
| **Una pila maciza es su propia carbonera** | sus celdas interiores no tienen un vecino de aire, aunque esté al aire libre | `r135` |
| **La sombra del alambique** | la máquina que riega el huerto es la que lo deja sin luz | `r148` |

Nueve de esas diez **no están programadas en ninguna parte**: salen de que dos números y una
geometría se encuentren. Es el resultado central del laboratorio.

## A.3 · La cadena cruzada, y la que no fue

El criterio más exigente del diseño del fuego era ver **una cadena que cruzara dominios sin
guion**. La esperábamos del humo: fuego bajo la chimenea → humo en la cámara alta → oscurece el
lecho → las plantas se marchitan mientras el alambique las riega.

**No fue esa.** El humo llegó a 15 celdas y desapareció: el fuego de la sala se consume en cinco
minutos y nadie lo realimenta.

**Fue otra, y mejor.** El serpentín del alambique —31 celdas de núcleo frío en el techo— tapa la
boca del cielo, y la luz del lecho cae de 245 a **0** justo debajo de esa fila. Agua × luz × vida
en un solo gesto: **el jugador que construye la máquina de regar mata el huerto que riega**, y el
arreglo no es cambiar una regla, es mover la máquina. El control lo confirma: corrida a otro sitio,
la luz sube de 0 a 17 y germinan cinco veces más plantas (`r148`).

> Para el criterio 4 del fuego: **observado en simulación autónoma: SÍ · con jugador: no evaluado.**

## A.4 · Lo que NO se puede afirmar

- Si un jugador **descubre** estas máquinas sin que nadie se lo diga → **no evaluado**.
- Si el ritmo aburre, si los procesos lentos se sienten lentos → **no evaluado**.
- Si «hay juego» aquí dentro → **no evaluado**. Ni aprobado ni refutado.

La herramienta para evaluarlo está hecha y probada (diario de sesión con claqueta y marcas, sonido
del laboratorio, guía de protocolo): `docs/LAB/GUIA_H7.md`.

---

# B · Emergente contra ruido: qué se quedó y qué se cayó

El encargo pedía justificar cada expansión. El criterio fue el del proyecto (R48): **verbo visible
+ consumidor real**, o es ruido. Tres cosas se cayeron al medirlas, y esa es la parte útil de esta
sección.

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
  **idéntica al bit** (`r136`). Se retiró el paquete entero y se documentó en `DISENO_FUEGO.md` §10.
- **El desagüe de grava.** Dos rondas y dos versiones. Con el riego real **encharcaba más** (26 de
  36 columnas contra 7), porque un poroso solo suelta agua a un vacío al saturar y la capilaridad
  no llega: era una esponja sin salida dentro del lecho. Retirado (`r141`, `r145`).
- **El churn erosión-depósito.** 6 279 eventos para el mismo neto de 375 celdas: ruido puro. El
  mando que hacía el trabajo era otro (`sed.depositoReposo`), y se midieron por separado (`r133`).

**Y una regla nueva se ganó el sitio por lo que destapó**: el aire de contacto produjo la
carbonera, la brasa bancada y el horno con una sola condición de vecindad.

## B.1 · El precio de medir de verdad

Tres veces el banco dijo algo falso y hubo que rehacerlo. Vale la pena dejarlo escrito porque es
la parte del método que más costó:

- El banco **medía otro universo** (le faltaba aplicar la química del laboratorio): el vapor vivía
  60 ticks en vez de 180 y condensaba a 90 en vez de a 65, así que el alambique del banco no podía
  destilar. Peor: los hashes de Fable y los míos **coincidían porque los dos medíamos lo mismo mal**
  (`r145`).
- El escenario «mundo entero despierto» pintaba arena, que se posa: a los 2 000 ticks daba **0
  chunks activos**. Medía el mundo dormido con otro nombre (`r142`).
- El «90 % del calor viene de la llama» era falso, y la corrección a «¾» también: las dos cifras
  eran del libro **nominal**. En raw entregados la llama pone entre el 6 % y el 29 % (`r139`).

De ahí salió la regla que ahora protege el banco: **los siete hashes son la licencia para
optimizar**. Un cambio de rendimiento que los deje intactos no tocó la física; uno que mueva un
solo hash es un cambio de física disfrazado.

---

# C · Valoración: ¿responde a la pregunta rectora? **[FABLE]**

*(Borrador; la valoración final se acuerda con Fable.)*

**La mitad medible de la apuesta se cumple.** La tesis era que un puñado de campos y procesos
lentos producen cadenas largas con coste acotado. Se cumple: diez cadenas observables, nueve de
ellas no programadas, con el laboratorio a **1,6 ms/tick** y el peor caso en **3,1** (aceptación:
12). El conocimiento **sí** construye procesos: quien entiende que el aire de contacto decide si
algo deja carbón o ceniza puede hacer una carbonera con una boca de una celda.

**La otra mitad — que el conocimiento sustituya el trabajo manual — está a medias, y con un dato
concreto.** El único proceso que corre solo durante minutos sin nadie es la tolva (466 s). El resto
pide manos: encender, reponer, mover. No hay todavía una máquina que produzca **sin volver a
tocarla**, que es lo que la tesis promete.

**Y hay una lección de diseño que vale más que cualquiera de las máquinas:** dos veces, con
mecanismos distintos, **el aparato que resuelve un problema crea el siguiente**. El alambique que
riega, ahoga (R135); el alambique que riega, ensombrece (R148). Eso no se diseñó: salió de que las
reglas se cruzaran. Si el juego tiene algo, está ahí.

**Mi puntuación del dominio del fuego, con el criterio del propio diseño:**

| criterio | estado |
|---|---|
| 1. Máquina escondida en las leyes | **✔** el horno vidria 18/18; el hogar suelto, 0 |
| 2. Automatización sin jugador ≥ 5 min | **✔** 466 s con cero intervenciones |
| 3. Mando de geometría monótono | **✔** pero en el COMBUSTIBLE, no en el recinto: a igual masa y misma boca, la pila fina da ×5 de llama |
| 4. Cadena cruzada no guionizada | **✔ en simulación** (la sombra del alambique) · **no evaluado con jugador** |
| 5. Libro mayor de energía | **✔** dos libros, y la identidad de la carbonera cuadra a ±1 % |

**4 de 5 medidos, y el quinto no es refutable hoy.** [FABLE: ¿lo dejamos en 4/5 o en «4 medidos +
1 diferido»?]

---

# D · Multiplayer

Análisis completo de Fable en `docs/LAB/MULTIPLAYER.md`. Lo esencial para decidir:

- **Casi nada nuevo tiene que viajar.** Hoy solo `mat` se replica; el invitado es un espejo sin
  stepper. De los cuatro campos nuevos, **`reposo` no se ve** (0 bytes) y **`luz` se recalcula
  local** sobre el `mat` replicado con la misma función y el mismo resultado (0 bytes).
- **`carga` (turbidez) sale gratis** empaquetándola como material virtual `AguaTurbia` en el
  espejo: un byte que el RLE ya comprime.
- **`temp` y `humedad`** solo hacen falta si el invitado debe **ver** incandescencia y mojado:
  cuantizadas a 3-4 bits, solo en chunks con cambio y cerca de un avatar.
- **Los dos riesgos reales de divergencia no son físicos:** los 96 parámetros del panel y el
  multiplicador de tiempo. Los dos se resuelven con autoridad del host y un RPC de «propongo
  valor».

**Coste (Fable):** ruta A (solo-anfitrión con espejo enriquecido, que es lo que el motor ya es)
**2-3 semanas** de red más una de pruebas con dos máquinas.

Y un dato que el laboratorio aporta a esta sección: **el determinismo está probado al bit**. Los
siete hashes se reproducen entre corridas y entre sesiones, así que un modelo de host autoritativo
con espejo no tiene fuente de deriva por la simulación.

---

# E · Qué costaría llevarlo al juego **[FABLE]**

*(Estimación en meses; la más necesitada de segunda opinión.)*

Lo que ya está y no habría que rehacer: los cuatro campos, la pasada lenta, la presión, el fuego
entero, el panel de 96 parámetros, el banco con ocho escenarios y hashes, el audio del laboratorio
y la instrumentación de sesión.

Lo que faltaría, y por qué:

| trabajo | por qué | estimación |
|---|---|---|
| **Presentación** | hoy el estado solo se lee con F8; las plantas son un píxel; los nombres acaban de existir | **1-1,5 meses** |
| **Que el huerto pueda vivir** | 7 de 73 celdas reciben luz suficiente (§F) — geometría de nivel | días, no meses |
| **Una máquina que produzca sin volver a tocarla** | es la mitad no cumplida de la tesis | **3-4 semanas** |
| **La prueba con jugador y lo que destape** | hoy no evaluada | 2 semanas + lo que salga |
| **Multiplayer (ruta A)** | §D | **3-4 semanas** |
| **Sólidos cohesionados (H6)** | congelado con su hipótesis escrita | su propia etapa |

**Total, sin H6 ni el rediseño que la prueba con jugador pudiera exigir: 3-4 meses** de una
persona. [FABLE: la mía es una estimación de implementador y probablemente optimista en
presentación; ¿la revisas?]

---

# F · Lo que el laboratorio sabe y lo que no

## Lo que sabe (con su medida)

| hecho | número |
|---|---|
| El agua se conserva exactamente | residuo **0** a los 50, 150, 300 y 600 s |
| El circuito cierra | el sumidero traga las 20 celdas/s del manantial |
| La destilación funciona y se dirige | el serpentín se lleva **5 926 u** de rocío; la roca, 0 |
| El horno es una máquina real | **18/18** de carga vidriada; **0** sin recinto |
| Carbonizar no crea energía | pico de carbón **25,0 %**; identidad a **±1 %** |
| Un proceso se sostiene solo | **466 s** sin intervenciones |
| La geometría del combustible manda | pila fina: **×5** de llama que la maciza, a igual masa y boca |
| Coste | **1,6 ms/tick**; peor caso 3,1 |
| Determinismo | siete hashes reproducidos entre corridas y sesiones |
| Multiplicador de tiempo | **×13 deducido** del presupuesto de 20 ms/frame — no medido en Play, que es como habría que confirmarlo |
| El encharcamiento está resuelto | **0 de 36** columnas anegadas desde el minuto 15 |

## Lo que no sabe

| pregunta | estado |
|---|---|
| ¿Alguien descubre las máquinas sin ayuda? | **no evaluado** |
| ¿Esto entretiene? | **no evaluado** |
| ¿Cuánto se espera antes de aburrirse ante un proceso lento? | **no evaluado** |
| ¿El sonido ayuda al onboarding? | **no evaluado** (existe, no medido) |
| ¿Puede vivir el huerto? | **no con este nivel**: 7 de 73 celdas de cara pasan de `planta.luzMin` — es geometría, no física (**Q16**) |
| ¿Sirven los sólidos cohesionados? | hipótesis escrita, **congelada** |
| ¿Aguanta en red? | analizado, **no implementado** |

## Las tres cosas que yo arreglaría primero

1. **La luz del huerto** (Q16): dos o tres bocas de cielo más. Es lo que separa un huerto imposible
   de uno vivo, y es geometría.
2. **Una máquina que produzca sola**, aunque sea una: es la mitad de la tesis que falta.
3. **La prueba con jugador**, en cuanto haya presentación. Todo lo marcado «no evaluado» depende
   de ella, y son las preguntas que de verdad deciden si esto es un juego.

---

## Anexos

- Benchmarks: `Laboratorio/benchmarks/` (16 archivos, R131-R148).
- Presets y snapshots reproducibles: `Laboratorio/presets/ref_*`.
- Banco: `Sim/LabBench.cs`, nueve escenarios con siete hashes · menú
  «Ten Thousand Years/8».
- Decisiones de diseño con su porqué: `CHECKPOINT.md` D1-D36.
- Escalamientos y respuestas: `PREGUNTAS_A_FABLE.md` (Q1-Q16, R1-R25).
- Alcance y qué NO es este laboratorio: `ALCANCE.md`.
