# INFORME DE OPINIÓN — EL GIRO FINAL
*(Fable, ronda 57b. Cesar pidió: "opina de todo y opina extendidamente, necesito tu feedback
profundo". Esto es opinión de director, no acta: donde estoy de acuerdo lo digo, donde no,
también, y donde tengo algo mejor que ofrecer, lo ofrezco. El GDD vendrá después de que
iteres sobre esto.)*

---

## 0. MI LECTURA DEL GIRO EN CONJUNTO

Antes de ir punto por punto: **el mundo vacío es la pieza que faltaba, y es la decisión de
diseño más importante que has tomado desde que existe el proyecto.** Lo digo con calma y en
serio. Todos los problemas crónicos que venimos arrastrando — el aturdimiento de materiales,
el taller que "ya está hecho" y no se siente tuyo, el inicio que nunca encontraba su
intimidad, la progresión que no se sentía — son síntomas de una sola causa: el jugador
heredaba un laboratorio en vez de fundarlo. Empezar con barro, unas brasas y las manos
convierte cada estante en una victoria, cada máquina en una cicatriz con historia, y cada
material nuevo en una necesidad antes que en una entrada de catálogo. El "inicio íntimo que
buscamos alguna vez" no era una cuestión de tamaño de mapa: era una cuestión de propiedad.
Ahora la tienes.

Y hay una segunda cosa que quiero nombrar porque tú la dijiste de pasada y es más profunda de
lo que parece: *"quizás no tengo que innovar en cada mecánica del juego, pero me cuesta."*
Tienes razón en no innovar, y te explico por qué te cuesta menos de lo que crees: **el
presupuesto de innovación de este juego ya está gastado, y bien gastado.** Simulación celular
determinista + almacenamiento físico + co-op emergente es una combinación que no existe en el
mercado. Todo lo demás — pedidos, trueque, desbloqueos, catálogos — DEBE ser convencional,
porque lo convencional es el asa por la que el jugador agarra la máquina nueva. Noita es un
sim radical dentro de un roguelite absolutamente clásico. Factorio es una logística radical
dentro de un "mata bichos, investiga tecnología" de manual. El jugador solo tolera una
revolución por juego. La tuya ya está elegida.

---

## 1. LA FICHA Y EL LIBRO

**De acuerdo, sin reservas.** La ficha a la vista y el libro no compiten: responden preguntas
distintas. La ficha responde *"¿qué es esto que tengo en el frasco AHORA?"* — es memoria de
trabajo, se consulta sin querer, de reojo. El libro responde *"¿qué sé yo del mundo?"* — es
archivo, orgullo de colección, lectura voluntaria. Stardew lo hace exactamente así (tooltip
del objeto vs. pestaña de colecciones) y nadie siente que sobre ninguno de los dos.

La implementación correcta es que la ficha se DERIVE del libro (misma fuente de datos, cero
duplicación — ya tenemos SubstanceKnowledge como fuente única, regla 13). Y el test que
propones es el correcto, con una métrica concreta: si después de añadir la ficha el libro
deja de abrirse POR COMPLETO en las sesiones de prueba, la ficha está enseñando demasiado y
hay que recortarla (que la ficha dé el QUÉ y el libro guarde el CÓMO y el DÓNDE).

## 2. EL MUNDO VACÍO Y EL CRAFTEO DEL LABORATORIO

**De acuerdo con entusiasmo, con dos cauciones de ejecución.**

La cadena que describes — brasas + arena → vidrio → lo entrego → me dan el primer lugar donde
guardarlo → se desbloquea pedir el primer matraz — es exactamente correcta, y quiero
subrayar POR QUÉ es correcta: es la primera noche de Minecraft comprimida en fantasía de
alquimista. El jugador no aprende "el sistema de almacenamiento": aprende que SU vidrio
necesita un sitio, y el sitio llega como respuesta a una necesidad que él ya sentía. Cada
tutorial del juego se convierte en una necesidad sentida un minuto antes de su solución. Eso
es diseño de progresión de primera división y no requiere ni una mecánica nueva: requiere
guion.

**Caución 1 — el vacío no puede estar vacío de verbos.** Un mundo vacío con poco que hacer
los primeros diez minutos mata más partidas que cualquier bug. La regla que propongo para el
GDD: **un verbo nuevo o una entrega cada 60-90 segundos durante los primeros 15 minutos.**
El mundo empieza vacío de COSAS, nunca de acciones posibles. (El borde del cuchillo:
Minecraft te da madera a cinco pasos; nosotros damos la veta, el agua y las brasas a cinco
pasos del spawn.)

**Caución 2 — craftear todo no puede significar craftear todo DOS VECES.** El primer estante
construido a mano es un ritual; el quinto es una tarea doméstica. La división que propongo,
y que tu propio "maestro de doble entrada" resuelve con elegancia: **la primera unidad de
cada cosa se construye a mano (ritual, tutorial, propiedad); las repeticiones se PIDEN al
Maestro por trueque (economía).** Así el crafteo enseña y el trueque abastece, y ninguno de
los dos se vuelve rutina del otro. Esta división merece ser regla del GDD.

## 3. EL MAESTRO DE DOBLE ENTRADA Y EL TRUEQUE

**De acuerdo, y quiero elevarle el estatus: no es un "recurso barato de progresión" — es LA
economía del juego, y es mejor que la que teníamos.** Le pides disculpas a una idea que no
las necesita.

Piénsalo desde la fantasía que el nombre ya vende: estás reconstruyendo diez mil años de
conocimiento desde el barro. Diez mil años atrás no había moneda. **El trueque materia-por-
materia no es un placeholder de una economía: ES la economía históricamente correcta de ese
mundo**, y además es la única que respeta tu tesis del almacenamiento físico — pagas con
cosas que están EN tus estantes, y pagar VACÍA un estante que se ve vacío. La escasez, la
decisión y el pago ocurren todos en el mismo lugar: el taller visible.

Sobre la máquina "por esta vez o para siempre": esa es la primera decisión económica real del
juego y es excelente — alquilar barato o ahorrar para poseer es un dilema que hasta un niño
entiende y que genera conversación en co-op ("¿juntamos para comprarla ya?"). Consérvala tal
cual.

Tres condiciones de ejecución para que el trueque no degenere:

- **Precios en un tablón físico, fijos por semilla.** Nada de regateo ni precios flotantes:
  el jugador tiene que poder PLANIFICAR ("necesito 30 de arena para la placa"). El tablón es
  además otro cartel del mundo — tu propia tesis.
- **Stock del Maestro limitado por ciclo.** Si el Maestro cambia turba por arena sin límite,
  la veta deja de importar. Si te cambia "hasta 40 al día", el trueque es un complemento de
  la producción propia, no su sustituto. La escasez que genera conversación (tu punto 5 del
  loop) vive o muere aquí.
- **El Maestro nunca vende lo que aún no descubriste.** El trueque abastece lo conocido;
  la frontera se cruza siempre experimentando. Si vende descubrimientos, el sim deja de ser
  el árbol tecnológico y se muere la tesis Noita del proyecto.

## 4. EL CATÁLOGO DE PEDIDOS EN GRIS (aquí discrepo — a medias)

Dices que no te gusta y que no se te ocurre otra forma. **Mi opinión: tu instinto de rechazo
es correcto, tu necesidad es correcta, y la forma que falta existe.**

El menú con 50 cosas en gris tiene dos problemas: reintroduce el "aturde" en formato menú
(50 promesas simultáneas es la versión UI de los 12 materiales sin uso), y mata el misterio
— si veo todo el futuro en gris, el juego ya me contó su final. Pero la necesidad que ese
menú intenta resolver es real y la formulaste bien: *que la necesidad de hacer pedidos sin
tiempo límite quede clara y dé sentido de evolución.*

La forma que propongo: **EL LIBRO MAYOR DEL MAESTRO** (físico, sobre su mesa — todo en el
mundo, como manda tu tesis). Al consultarlo muestra: lo ya desbloqueado (tu historial, con
orgullo), los SIGUIENTES 2-3 desbloqueables con sus requisitos ("el matraz grande: tráeme
antes 20 de vidrio"), y del resto solo la promesa cuantificada: *"...y 34 páginas más que
aún no puedes leer."* Horizonte inmediato nítido + promesa de largo plazo + misterio
intacto. Es el patrón del centro comunitario de Stardew (ves los paquetes de la sala en la
que estás, no el detalle de todas las salas) y es de los sistemas de progresión mejor
valorados de la historia del género. Cero innovación, que es justo lo que toca.

## 5. TIEMPOS DE ENTREGA Y EL ROL DEL INTENDENTE

**De acuerdo, y aquí hay un principio de diseño escondido que quiero sacar a la luz y
proponer como regla del GDD:**

> **Lo que TÚ pides, tarda. Lo que TE piden, espera.**

Tus pedidos al Maestro llegan con tiempo de entrega (planificación, anticipación, el placer
de "llegó el pedido"); los encargos del mundo hacia ti no tienen deadline jamás (la
experimentación es el alma del juego y los relojes la matan — ya lo decidiste antes y es
correcto). Esa asimetría es elegante, es fácil de comunicar, y genera exactamente el rol
emergente que describes: el intendente que pregunta "¿qué necesitan?" y gestiona la cola de
pedidos mientras otro está focus en el encargo grande y otro experimenta. Nótese que el rol
nace de una VENTANITA y un temporizador — coste de implementación mínimo, profundidad co-op
máxima. De lo mejor de todo tu mensaje.

Una guarda: los tiempos de entrega deben ser minutos de juego cortos (1-4 min), no "vuelve
mañana". El juego es una sesión de sobremesa, no un free-to-play de esperas.

## 6. VOLÁTILES, DERRAMES E INCENDIOS (el cuidado como gameplay)

**De acuerdo, y con ventaja injusta a nuestro favor: la simulación ya nos da el 80% gratis.**
El fuego ya se propaga, los líquidos ya se derraman (la inundación del playtest 26 fue
exactamente una de esas "situaciones jocosas", solo que sin diseño alrededor). Lo nuevo —
compuestos que explotan por agitación o temperatura, la categoría "volátil" — es barato en
un autómata celular y carísimo en cualquier otro motor. Es EL género de contenido que
justifica haber construido este motor.

Y hay una razón comercial que quiero que tengas presente: **los accidentes emergentes en
co-op son la fábrica de clips.** Lethal Company se vendió por los clips de gente gritando;
nuestro equivalente es el compañero que guardó el polvo volátil al lado de la mufla. Cada
accidente gracioso es marketing gratuito.

Tu instinto de "siempre apalancados en el mundo real" es la brújula correcta, y el mundo
real regala material: el polvo fino EN SUSPENSIÓN + chispa = explosión de polvo (los molinos
de harina explotaban de verdad); la nitroglicerina y la agitación; el aceite y el agua que
lo escupe. Todo enseñable, todo verosímil, todo ya medio soportado por el motor.

**Las tres guardas de diseño (no negociables, por las reglas 44 y 54 del proyecto):**

1. **Un accidente deja evidencia y lección, nunca partida perdida.** El incendio que arrasa
   tu almacén etiquetado sin recurso le enseña al jugador a NO almacenar — mata el loop
   central. Derrames recuperables, fuego contenible, y la ceniza del desastre se recicla.
2. **La contención es progresión crafteable.** Cajas selladas, foso de arena, el balde: el
   cuidado se COMPRA con producción, no se sufre desde el minuto uno. Los volátiles entran
   cuando ya existe la contención que los gestiona (regla 48: nada entra sin su consumidor —
   aquí, sin su contenedor).
3. **La volatilidad es una categoría legible ANTES del accidente.** El tarro tiembla, humea,
   brilla — el mundo avisa (regla 54: evidencia forense, también preventiva). La sorpresa
   graciosa es del descuido, nunca de la información oculta.

## 7. LA LINEALIDAD DE LOS PEDIDOS

**Permiso concedido, y con argumento.** Los primeros 3-5 pedidos DEBEN ser lineales: son la
fundación del laboratorio y el tutorial encubierto — ahí la linealidad no es un recurso
barato, es la forma correcta (una necesidad sentida → una solución, en orden). A partir de
ahí no necesitas "no-linealidad" en el sentido difícil: necesitas **dos pedidos del mundo
abiertos a la vez**, ambos completables en cualquier orden, compartiendo algún material para
que elegir cuál atender primero sea una decisión de inventario (¡visible en los estantes!).
Eso es un grafo plano de dos nodos, no un sistema nuevo, y en co-op se convierte solo en
reparto de trabajo. La sensación de no-linealidad del jugador no viene de estructuras
complejas: viene de que la escasez le obligue a ELEGIR el orden. Ya tienes la escasez.

Y lo repito porque es la tranquilidad que me pediste sin pedirla: los pedidos son "solo
texto y condiciones" con tremendo impacto, sí — y eso significa que su calidad es un
problema de ESCRITURA e ITERACIÓN, no de arquitectura. Es trabajo de rondas cortas conmigo,
no un riesgo del proyecto.

## 8. FUERA EL FAVOR

**De acuerdo, sin duelo.** El Favor era un marcador que fingía ser moneda: no se gastaba en
casi nada, así que era un número que mentía sobre su propia importancia. El trueque lo
reemplaza con ventaja en todos sus usos reales: la recompensa de un pedido pasa a ser
materia, herramienta, obra o página nueva del libro mayor — todo físico, todo visible, todo
coherente con la tesis. Que muera en la limpieza.

Una sola semilla guardo para el futuro, sin implementarla ahora: si algún día necesitas que
la RELACIÓN con el Maestro evolucione (mejores precios, más stock, su tono), eso puede ser
una reputación INVISIBLE que solo se manifiesta en el mundo — nunca un contador en el HUD.
Anotada y enterrada; no la toques hasta después del Fest.

## 9. LO QUE EL NOMBRE REGALA Y NADIE HA RECLAMADO: LAS EDADES

Este es mi aporte nuevo al giro, tómalo o déjalo. El nombre que elegiste — diez mil años de
conocimiento humano — te regala la macroestructura que el catálogo de pedidos y el GDD
necesitan: **las EDADES del conocimiento como capítulos de progresión.** Barro y fuego
(cerámica) → vidrio → cal y mortero (construcción) → metal → ... Cada "edad" es un grupo de
páginas del libro mayor, un salto visual del taller (tus mockups ya muestran talleres de
distintas épocas, ¿te fijaste?), y una promesa de contenido futuro para el Early Access
("la Edad del Metal llega en la próxima actualización" se vende sola). No es un sistema
nuevo: es un ORDEN para los sistemas que ya decidiste. El GDD lo usará de columna vertebral
si te convence.

## 10. EL NOMBRE (evaluación honesta, como pediste)

Primero: **la tagline es lo mejor que ha producido el proyecto a nivel de marketing.**
*"Rebuild ten thousand years of human knowledge from mud, fire, and observation"* es el
juego entero en una frase, es concreta, es distinta de todo lo que hay en Steam, y la
palabra "observation" hace un trabajo silencioso precioso (promete el juego de MIRAR la
materia, que es la verdad). Esa frase no se toca gane quien gane el título.

**TEN THOUSAND YEARS.** Fortalezas: grandeza, alcance, ritmo al decirla; casa perfecto con
la tagline (que la repite) y con las Edades. Debilidades comerciales reales: es una frase
COMÚN — colisiona en búsqueda con canciones, modismos y un juego pequeño de itch llamado
"10000 YEARS", y en las culturas del este asiático "diez mil años" es una expresión hecha
("¡larga vida!") que le añade ruido; como marca es difícil de poseer, y no contiene ninguna
señal de género ni de materia (leída en frío podría ser un 4X, una novela visual o un
documental).

**ASHES TO AGES.** Fortalezas: como frase exacta está prácticamente virgen (lo más cercano
es "Century: Age of Ashes", un juego de dragones lo bastante distinto para no confundir,
aunque existe y hay que saberlo); es un juego de palabras real ("ashes to ashes" invertido:
de la ceniza a las edades — del polvo al progreso, la resurrección del conocimiento); la
aliteración la hace pegajosa; y — esto me encanta y no sé si fue deliberado — **la ceniza
es literalmente nuestro material bisagra** (regla 54: el residuo del fracaso que se recicla
en vidrio). El nombre codifica el loop del juego. Debilidad: menos majestuoso, y "ages"
también evoca estrategia histórica (Age of Empires) aunque menos que TTY.

**Mi veredicto, mojándome como pediste:** en impacto de ventas, **ASHES TO AGES gana por
margen moderado** — la unicidad de búsqueda, la marca poseíble y el gancho mnemotécnico
pesan más en Steam que la grandeza, porque la grandeza la pone la cápsula y la tagline. TEN
THOUSAND YEARS es la mejor FANTASÍA; ASHES TO AGES es la mejor MARCA; y como la tagline ya
contiene "ten thousand years" dentro, **la opción b te deja usar las dos ideas a la vez**
(marca única fuera, grandeza dentro) mientras que la opción a gasta las dos en lo mismo.
Dicho esto: entiendo que por ahora sella la opción a, y con la tagline al lado funciona —
ninguna de las dos es un error. Solo deja registrado mi voto: b > a en tienda, a > b en
epopeya. Nota práctica para cualquiera de las dos: nombre en inglés es correcto (Noita es
finés y nadie tropezó); el español vive en la tagline localizada — *"Reconstruye diez mil
años de conocimiento humano con barro, fuego y observación"* — que suena igual de bien.

## 11. EL PLAN (informe → GDD → limpieza) Y EL HANDOFF DE 90 PÁGINAS

**El orden es correcto y la instinto de limpiar AHORA, con la claridad recién ganada, es de
director con oficio.** Tres ajustes de ejecución que propongo:

**El GDD debe ser corto y normativo — 15-20 páginas, no 90.** El HANDOFF era un diario de
viaje (por eso pesa 90 páginas: son 56 playtests de lecciones); el GDD es un mapa. Hiciste
bien en copiar el histórico. El GDD nuevo dice qué ES el juego; el histórico queda como
memoria de por qué. Estructura que propongo para el borrador: visión y tagline / el loop /
las Edades / el mundo vacío y la fundación / la economía (trueque, libro mayor, doble
entrada) / los pedidos (asimetría, linealidad inicial, generador) / el peligro (volátiles)
/ co-op y roles emergentes / lo que este juego NO es (tan importante como lo que sí) /
hoja de ruta al Fest.

**La limpieza debe ser por etapas quirúrgicas, no una purga.** La regla 26 existe porque los
borrados grandes esconden regresiones que compilan. Propongo: un tag de git "ultimo-clasico"
antes de tocar nada; luego UNA ronda por sistema muerto (el Favor, el arco de LO QUE
PERSISTE clásico, los restos aparcados de criaturas si decides que no vuelven, la escena
caótica si el GDD la jubila), cada una con compilación fiel + arranque verificado en tu
editor. Cuatro rondas pequeñas y seguras valen más que una heroica y ciega.

**Las reglas se revisan en dos montones.** Las FORENSES (26, 29, 30, 36, 49, 52, 56...) son
lecciones pagadas con sangre y sobreviven a cualquier giro: se quedan. Las DESCRIPTIVAS de
sistemas que mueren (las del Favor, las del arco clásico, las de la marea ya archivadas) se
jubilan con su sistema, con una línea que diga adónde fue el cuerpo (regla 15). Te traeré la
lista clasificada en la ronda de limpieza. Y en esa misma ronda: renombrar el repo (la regla
6 lleva meses esperándolo) al nombre nuevo del juego.

## 12. LO QUE YO NO HARÍA TODAVÍA (anti-alcance, para proteger el Fest)

Para que el giro no se coma el calendario: los volátiles como categoría diseñada (§6), las
Edades más allá de las dos primeras, la reputación invisible del Maestro y cualquier
automatización van DESPUÉS de la demo del Next Fest. La demo es: fundación del laboratorio
(mundo vacío → primer vidrio → primer estante → primer matraz) + libro mayor con un
horizonte + 2-3 pedidos del mundo + trueque básico + los accidentes que la sim ya regala
sin diseño extra. Eso cabe en 30-40 minutos de demo, enseña TODO el loop, y es alcanzable
con el plan F1-F2 del informe anterior reorientado a esta versión. El resto es juego
completo, no demo.

---

### Resumen de veredictos, en una línea cada uno

Ficha+libro: sí a ambos, la ficha deriva del libro. Mundo vacío: la mejor decisión del
proyecto; guarda el ritmo de verbos y el "primera vez ritual / repetición pedida". Maestro
doble entrada y trueque: no es recurso barato, es LA economía — tablón físico, stock
limitado, nunca vende descubrimientos. Catálogo gris: no tal cual — libro mayor físico con
horizonte de 2-3 y páginas cerradas contadas. Tiempos de entrega: sí, con la asimetría "lo
que pides tarda, lo que te piden espera". Volátiles: sí, después del Fest, con las tres
guardas. Linealidad: concedida y correcta al inicio; luego dos pedidos abiertos a la vez.
Favor: fuera, sin duelo. Nombre: b (ASHES TO AGES) vende más por marca única; a (TEN
THOUSAND YEARS) es mejor fantasía; con la opción a, la tagline carga el peso y funciona.
Plan: correcto — GDD corto y normativo, limpieza por etapas con tag previo, reglas en dos
montones, renombrar el repo. Y las EDADES como columna vertebral, si te convencen.

Cuando lo hayas leído y anotado, hago el primer borrador completo del GDD.
