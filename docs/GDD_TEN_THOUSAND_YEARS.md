# TEN THOUSAND YEARS — GDD v0.1 (primer borrador completo)

> **Rebuild human knowledge from mud, fire, and observation.**
> *Reconstruye el conocimiento humano con barro, fuego y observación.*

*(Fable, ronda 58. Este documento es NORMATIVO: dice qué ES el juego. El porqué de cada
decisión vive en docs/INFORME_OPINION_GIRO.md y en el HANDOFF histórico. Está escrito para
iterarse: cada sección termina marcando qué está SELLADO por decisión de Cesar y qué está
ABIERTO a su veredicto. Versión para tachar, anotar y devolver.)*

---

## 1. FICHA

| Campo | Valor |
|---|---|
| Título | **TEN THOUSAND YEARS** |
| Eslogan | *Rebuild human knowledge from mud, fire, and observation.* |
| Género | Simulación de materia (falling-sand) + taller/producción + co-op |
| Una frase de tienda | *Noita meets Potion Craft, en co-op: funda tu taller desde el barro y reconstruye el conocimiento humano.* |
| Jugadores | 1-4 (co-op drop-in, sim en el host + espejo por chunks) |
| Plataforma | PC/Steam (Windows primero) |
| Precio objetivo | $11.99 (-15% lanzamiento) |
| Motor | Unity 6.5, autómata celular determinista propio (768x288 @30Hz, mundo ampliable) |
| Idiomas | Español latino + inglés desde el día uno (los textos son el sistema: se escriben en paralelo) |
| Modelo | Early Access con las primeras 2 Edades → 1.0 con 4+ |

## 2. LA VISIÓN

Empiezas con barro, unas brasas, agua y tus manos, en un rincón de un mundo casi vacío. Un
Maestro viejo tiene la única mesa en pie. Diez mil años de conocimiento humano —cerámica,
vidrio, cal, metal— están ahí fuera, dormidos en la materia, esperando que alguien los
redescubra MIRANDO: qué flota, qué arde, qué aguanta el fuego sin ceder. Cada cosa que
aprendes a hacer se produce, se guarda en estantes que tú construiste, se gasta en pedidos
de un mundo que necesita cosas, y cada escasez te empuja a la siguiente pregunta.

**Los cuatro pilares (todo lo que entre al juego debe servir al menos a uno; lo que
contradiga a uno, no entra):**

1. **La materia es real.** Todo es simulación celular: lo que se derrama se derrama, lo que
   arde se propaga, lo que flota flota. El árbol tecnológico es la física. No hay recetas de
   menú: hay observación.
2. **El taller es tuyo porque lo fundaste.** Empiezas sin nada y todo se craftea, incluidos
   los lugares donde guardas. El taller ES el inventario: mirar los estantes responde "¿qué
   tenemos?" sin abrir un solo menú.
3. **El conocimiento es la progresión.** No subes de nivel: aprendes. Lo aprendido se
   produce, lo producido paga lo siguiente. Las Edades del conocimiento son los capítulos.
4. **El co-op no tiene clases: tiene estantes.** Los roles (productor, intendente,
   explorador, organizador) emergen de la escasez visible, nunca de un selector.

**Sellado:** los cuatro pilares. **Abierto:** el orden de prioridad entre 2 y 3 cuando
choquen (propongo que gane el 2: lo físico manda).

## 3. EL LOOP

**Macro (el ciclo del juego entero):**

> descubrir → producir → almacenar → utilizar → escasear → repartir trabajo → necesitar algo nuevo → descubrir

**Micro (un minuto cualquiera de partida):** miro el pedido activo → miro mis estantes (¿qué
falta?) → produzco o experimento o pido al Maestro → guardo o entrego → algo nuevo pasa en la
materia (evidencia, accidente, sorpresa) → anoto/bautizo → vuelvo a mirar el pedido.

**Regla de ritmo (sellada por lección del proyecto):** durante los primeros 15 minutos hay un
verbo nuevo o una entrega cada 60-90 segundos. El mundo empieza vacío de cosas, jamás de
acciones.

**Regla de material (regla 48 generalizada):** ningún material entra al juego sin su
CONSUMIDOR ya presente (algo que lo pida, lo gaste o lo contenga). La primera hora vive con
6-8 materiales, todos en uso.

## 4. LAS EDADES (macroestructura y hoja de contenido)

Los capítulos del juego son las Edades del conocimiento. Cada Edad es: un grupo de páginas
del Libro Mayor, 2-4 materiales nuevos CON consumidor, 1-2 estaciones/obras nuevas, y un
salto visible del taller. El jugador no "desbloquea la Edad": la ATRAVIESA sin cartel, y al
mirar atrás su taller se lo cuenta.

| Edad | Materiales eje | Obras/estaciones | El pedido que la abre |
|---|---|---|---|
| **I. Barro y Fuego** | barro/arcilla, turba, carbón, ceniza, cerámica | fogón → crisol de barro, primer estante, primeros frascos | la fundación (ver §5) |
| **II. Vidrio** | arena, vidrio, barbotina/engobe | horno mejorado, alacena grande, ventanas | "los vitrales de la capilla" (ya construido, pt56) |
| **III. Cal y Piedra** | caliza, cal viva, cal apagada, mortero | la mufla (ya construida, pt56), obra de albañilería | "la mufla del vidriero" (pt56) → reparaciones del mundo |
| **IV. Metal** *(post-EA inicial)* | mena, metal, escoria | fragua, moldes | por diseñar |
| V+ *(1.0 y más allá)* | por diseñar (destilación, tintes, papel...) | alambique... | por diseñar |

**Sellado:** las Edades como estructura; I-III con los materiales ya existentes (cero
materiales nuevos hasta la Edad IV). **Abierto:** el contenido exacto de IV+.

## 5. EL MUNDO VACÍO Y LA FUNDACIÓN (la primera hora)

**Estado inicial del mundo:** terreno natural (piedra, tierra, agua), la VETA de turba, un
depósito de arcilla y arena cerca del spawn, y el rincón del Maestro: su mesa, su Libro
Mayor, unas brasas que él mantiene vivas. Nada más. El mapa es grande (3x2 pantallas hoy,
ampliable): el taller crece HACIA el espacio vacío — el "lugar pequeño que se amplía" por
fin, con la economía como llave.

**La secuencia fundacional (los pedidos lineales 1-5, tutorial encubierto — SELLADA en
estructura, ABIERTA en cantidades):**

1. *"Tráeme barro del río."* → aprender frasco/aspirar/verter. El Maestro te enseña a cocerlo
   en SUS brasas → tu primera cerámica → **te la devuelve como tu primer FRASCO propio.**
2. *"El fuego es tuyo si lo alimentas."* → tallar la veta, turba → tu propio fogón (primera
   obra tallada). Ya no dependes de sus brasas.
3. *"Arena y ceniza, y mucha paciencia."* → tu primer VIDRIO en tu fogón. Lo entregas →
   **te ayuda a levantar tu primer ESTANTE** (primer lugar de guardado; desde aquí existe
   "¿dónde lo pongo?").
4. *"Un taller sin frascos no es un taller."* → se abre en el Libro Mayor la página del
   matraz: tu primer PEDIDO A ÉL por trueque (ver §6). Llega con tiempo de entrega: la
   primera espera, la primera anticipación.
5. *"Ahora mira tú el mundo."* → primer pedido con observación real (el patrón
   pregunta-experimento-entrega del arco actual, ya corregido en pt57: el pedido SIEMPRE dice
   el experimento, el criterio y el lugar de entrega). Desde aquí, el Libro Mayor muestra su
   horizonte y el mundo empieza a pedir.

A partir del pedido ~6: **dos pedidos del mundo abiertos a la vez**, completables en
cualquier orden, compartiendo algún material — la no-linealidad que se siente sin
arquitectura nueva. La linealidad de 1-5 es deliberada y no se disculpa.

## 6. LA ECONOMÍA — EL TRUEQUE Y EL MAESTRO DE DOBLE ENTRADA

**No hay moneda. No hay Favor (retirado).** Se paga con materia, herramienta u obra. El
Maestro es la contraparte de doble entrada: él te pide (encargos del mundo) y tú le pides
(materiales, objetos de organización, maquinaria — todo).

**Las reglas del trueque (selladas):**

- **El tablón de precios es físico y fijo por semilla.** Cuelga junto a su mesa. Se puede
  planificar contra él. Sin regateo.
- **Su stock es limitado por ciclo.** El trueque complementa tu producción, nunca la
  sustituye: la veta sigue importando siempre.
- **Nunca vende descubrimientos.** Solo abastece lo que ya sabes hacer o herramientas cuya
  página del Libro Mayor ya abriste. La frontera se cruza experimentando, sin excepción.
- **Alquilar o poseer.** Las máquinas complejas ("tiene muchos circuitos...") se ofrecen en
  dos precios: por esta vez (barato) o para siempre (caro). Primera decisión económica real.
- **Lo que TÚ pides, TARDA; lo que TE piden, ESPERA.** Tus pedidos llegan con tiempo de
  entrega corto (1-4 min de juego: planificación y anticipación, no free-to-play). Los
  encargos del mundo hacia ti no tienen deadline JAMÁS: la experimentación es el alma del
  juego y los relojes la matan.

**La primera unidad se construye a mano; las repeticiones se piden.** El crafteo enseña y
da propiedad; el trueque abastece. Ninguno degenera en el trabajo del otro.

**Abierto:** los precios concretos (se calibran jugando, con el solver de completabilidad
detrás); si el stock del Maestro se repone por tiempo real de partida o por entregas.

## 7. EL LIBRO MAYOR (la progresión visible)

Físico, sobre la mesa del Maestro. Al consultarlo muestra TRES cosas:

1. **Lo desbloqueado** — tu historial, con orgullo (qué pediste, qué construiste, qué Edad
   atravesaste).
2. **El horizonte: los siguientes 2-3 desbloqueables**, con requisitos concretos y legibles
   ("El matraz grande — tráeme antes 20 de vidrio").
3. **La promesa contada: "...y 34 páginas más que aún no puedes leer."** Número real,
   contenido oculto. Sentido de evolución sin spoiler y sin el catálogo gris de 50.

Las Edades son los capítulos del libro. NO hay menú de progresión fuera de él: todo en el
mundo.

## 8. EL ALMACENAMIENTO FÍSICO (el corazón del giro)

- Todo lugar de guardado se CRAFTEA: estantes, alacenas, cajas, tolvas. La capacidad del
  taller es una decisión de construcción del jugador.
- Cada contenedor muestra su contenido real (material, patrón, nivel) — ya existe
  (StorageRack/alacena pt54-56); se generaliza.
- **CARTELES DEL JUGADOR:** construyes un cartel, lo colocas donde quieras y escribes lo que
  quieras (reutiliza el sistema de bautizar). El jugador lidera SU organización; el juego no
  impone taxonomía. Las categorías de los mockups (LÍQUIDOS · POLVOS · ...) son una
  SUGERENCIA del arte final, no un sistema.
- Meta de legibilidad (medible en playtest): la pregunta "¿cuánto X nos queda?" se responde
  MIRANDO en menos de 3 segundos, sin abrir nada.
- Derramar, quemar o perder lo almacenado es posible (es la simulación) pero SIEMPRE
  recuperable en parte y SIEMPRE con lección (regla 44/54: evidencia forense, nunca partida
  muerta).

## 9. EL CONOCIMIENTO (qué se conserva del juego actual)

Se conserva entero, reorientado a servir al loop:

- **Descubrir mirando**: los eventos de la sim alimentan el diario; las leyes del universo
  varían por semilla (química generada, afinidad con tesis nombrable — regla 35).
- **Bautizar**: lo innominado enseña "???" hasta que TÚ lo nombras; tu nombre viaja a
  pedidos, fichas y carteles (regla 13).
- **La ficha a la vista + el libro**: la ficha (una carta: familia, estado, inflamable)
  responde "¿qué es esto AHORA?"; el diario-libro completo se queda como archivo y colección.
  La ficha se deriva del libro, jamás lo reemplaza. *(A testear: si el libro deja de abrirse
  del todo, la ficha enseña demasiado.)*
- **La evidencia forense** (regla 54): todo fracaso deja ceniza/residuo/anotación. Fracasar
  es un experimento con datos, y la ceniza se recicla (es el material bisagra de la Edad II).

## 10. EL PELIGRO — VOLÁTILES Y ACCIDENTES *(post-Fest, diseñado desde ya)*

La sim ya regala derrames e incendios; se les añade DISEÑO, no motor:

- **Categoría "volátil"**: compuestos que explotan por agitación o temperatura, siempre
  apalancados en el mundo real (polvo fino en suspensión + chispa; nitro + sacudida).
- **Tres guardas selladas**: (1) un accidente deja evidencia y lección, nunca partida
  perdida; (2) la contención es progresión crafteable (caja sellada, foso de arena, balde) y
  los volátiles entran DESPUÉS que su contención (regla 48: sin contenedor no hay volátil);
  (3) la volatilidad avisa ANTES (el tarro tiembla/humea): la sorpresa es del descuido,
  nunca de información oculta.
- Función comercial explícita: los accidentes co-op son la fábrica de clips.

## 11. CO-OP

- 1-4 jugadores; sim en el host, espejo por chunks (arquitectura actual, ya probada).
- Paridad de teatro total: descubrimientos, fichas, voz del Maestro y compuestos llegan
  igual al invitado (deuda actual: texto narrativo del compuesto — se paga antes del Fest).
- Los roles emergen de la economía: el INTENDENTE nace de la ventanita de pedidos al Maestro
  (pregunta "¿qué necesitan?", gestiona la cola y los tiempos de entrega); el PRODUCTOR de
  las cadenas; el EXPLORADOR de la frontera; el ORGANIZADOR de estantes y carteles. Cero
  código de roles: solo escasez visible + herramientas.
- Las obras nuevas (mufla y sucesoras) deben ser usables por el invitado (deuda MaquinaSync:
  se paga en la limpieza).

## 12. LO QUE ESTE JUEGO **NO** ES (tan normativo como el resto)

No es un roguelite (no hay muerte-reinicio; el mundo y el taller persisten). No tiene
relojes ni deadlines en lo que el mundo te pide. No tiene inventario abstracto (ni mochila
con celdas: el frasco, los estantes y el suelo son el inventario). No tiene árbol
tecnológico de menú (la física es el árbol; el Libro Mayor solo muestra el horizonte). No
tiene moneda ni puntuación (trueque y taller SON la riqueza). No tiene combate. No tiene
automatización (por ahora: si algún día entra, será premio de lategame, jamás requisito). No
tiene clases ni roles asignados. Y no innova en estructuras de pedidos: la revolución del
juego es la materia; los pedidos son clásicos a propósito.

## 13. HOJA DE RUTA AL NEXT FEST

**La demo (30-40 min, enseña TODO el loop):** fundación completa (§5, pedidos 1-5) + Libro
Mayor con horizonte + 2 pedidos del mundo abiertos + trueque básico con tablón + los
accidentes que la sim ya regala. Sin volátiles diseñados, sin Edad IV, sin reputación.

| Fase | Contenido | Rondas est. |
|---|---|---|
| **L. Limpieza** | tag "ultimo-clasico"; fuera Favor, arco clásico y muertos; reglas en dos montones; renombrar repo | 3-4 |
| **F1. La fundación** | mundo vacío, secuencia fundacional 1-5, crafteo de estantes/carteles, ficha a la vista | 3-4 |
| **F2. La economía** | trueque + tablón + stock + tiempos de entrega + Libro Mayor + rent-vs-own | 2-3 |
| **F3. El mundo pide** | generador 80/20 sobre plantillas narrativas; dos pedidos abiertos; obras como recompensa | 2-3 |
| **F4. Co-op fino** | paridad compuestos, obras usables por invitado, re-host, 3+ sesiones con amigo | 2 |
| **F5. Escaparate** | build demo con checklist, página Steam, cápsula/capturas (arte externo en paralelo) | 2 + arte |

Total: **~15 rondas de código** (5-7 semanas al ritmo actual) + arte en paralelo desde YA
(la página de Steam se crea en cuanto haya cápsula: las wishlists pre-Fest son el capital
que el Fest multiplica).

## 14. PREGUNTAS ABIERTAS PARA LA PRÓXIMA ITERACIÓN DE ESTE GDD

1. ¿La secuencia fundacional exacta (§5) te suena? Cantidades y orden son tuyos de tachar.
2. El stock del Maestro: ¿se repone por tiempo o por tus entregas?
3. ¿La ventanita de pedidos al Maestro es UI o también física (un buzón de salida junto a su
   mesa)? Mi voto: física, buzón de salida — simétrico al Buzón de entrada que ya existe.
4. ¿El mundo persiste entre sesiones desde la demo (guardado) o la demo es sesión única?
   (Guardado = trabajo extra pre-Fest; sesión única de 40 min puede bastar para la demo.)
5. ¿Semilla fija de autor para la demo (como Semilla Cero) o semilla libre? Mi voto: fija —
   todo lo aprendido con 777002 aplica.
6. El nombre del repo en la limpieza: ¿`TenThousandYears`?
