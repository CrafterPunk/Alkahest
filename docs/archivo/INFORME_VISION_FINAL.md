# INFORME — LA VISIÓN CERRADA: evaluación, complejidad, pronóstico y nombre
*(Fable, ronda 57 — respuesta al mandato de Cesar tras el playtest 56: "ya tengo certeza
absoluta de que el último pedido es lo que termina de redondear la idea... es en definitiva
la visión del juego final... ahora el loop completo debe aparecer desde mucho antes.")*

---

## 0. LO QUE ENTENDÍ (para que me corrijas si me desvío)

El playtest 56 no era una capa final: era la MAQUETA DEL JUEGO ENTERO. El loop definitivo es

> **descubrir → producir → almacenar → utilizar → escasear → repartir trabajo → necesitar algo nuevo → descubrir**

y tiene que estar vivo desde los primeros minutos, no aparecer como premio al terminar un
tutorial de descubrimiento puro. La prioridad de implementación que marcaste es el
**almacenamiento físico y visual primero**: frascos, depósitos, estanterías y tolvas
etiquetadas, de modo que puedas mirar el taller y decir *"nos queda poco vidrio"* sin abrir
ningún menú. Y el diagnóstico del estado actual es exacto: hoy el arco te hace descubrir ~12
materiales que no se usan en ningún sitio, y eso aturde — cada material que entra debe entrar
con un USO esperándolo, o no entra todavía.

Tus cuatro imágenes las leo como lo que son: pseudo-propuestas de organización y un alfa del
arte final. No son tarea de esta fase, pero SÍ fijan tres decisiones de diseño que se pueden
adoptar ya con el arte que tenemos, porque son estructura, no pintura:

1. **El taller ES el inventario.** Estanterías con categorías rotuladas (LÍQUIDOS · POLVOS ·
   SÓLIDOS · ORGÁNICOS · METALES · CERÁMICOS), cada frasco con su contenido visible y su
   nivel. Es la alacena del pt56 elevada a principio organizador de todo el taller.
2. **La misión como checklist permanente con iconos** (6x Vidrio 3/6 · 2x Tinte azul 1/2 · 1x
   Pegamento fuerte 0/1) — ya lo tenemos en texto; falta el icono del material y la barra.
3. **El diario como ficha de una sola carta** (VIDRIO — familia, estado, inflamable) en un
   rincón fijo, no como libro a pantalla completa que interrumpe.

---

## 1. EVALUACIÓN DE LA IDEA (mojándome)

### 1.1 El juego, en una frase
*Un taller de alquimia co-op sobre una simulación de materia estilo Noita, donde lo que
descubres se produce, se almacena a la vista, se gasta cumpliendo pedidos de un mundo que
necesita cosas, y cada escasez te empuja al siguiente descubrimiento.*

### 1.2 Por qué este loop es correcto (arco por arco)

**Descubrir → producir.** Es la corrección del defecto central que el propio proyecto venía
arrastrando y que tú nombraste: un descubrimiento que solo se registra es un coleccionable.
Producirlo lo convierte en una HABILIDAD. La diferencia psicológica es enorme: el jugador
pasa de "ya vi el vidrio" a "sé hacer vidrio", y saber hacer algo pide ejercerse.

**Producir → almacenar.** Aquí está la decisión más distintiva del juego, y sostengo que es
la más valiosa comercialmente: el almacenamiento FÍSICO. En Factorio miras cofres con
números; aquí miras una estantería donde la redoma de vidrio está medio vacía. Ningún juego
del nicho lo hace sobre una simulación celular donde el contenido de la redoma es materia
real que se puede derramar, congelar o quemar. "Nos queda poco vidrio" leído de un vistazo es
a la vez interfaz, decoración y estado del mundo — tres costes pagados con una sola mecánica.

**Almacenar → utilizar → escasear.** El pedido compuesto ("los vitrales de la capilla") ya
demostró en el pt56 que funciona mecánicamente. La escasez temporal es el generador de
conversación del co-op: "yo hago el combustible, tú el vidrio" no lo dicta ninguna clase, lo
dicta la estantería medio vacía. Roles emergentes sin sistema de roles — eso es diseño barato
de mantener y profundo de jugar.

**Necesitar → descubrir.** El cierre del ciclo es lo que separa esto de un juego de gestión:
el pedido nuevo pide algo que AÚN NO SABES HACER, y la respuesta está en la simulación, no en
un árbol tecnológico. El árbol tecnológico de este juego es la física de la semilla. Esa es
la tesis Noita ("el mundo es el sistema") aplicada a un género donde nadie la ha aplicado.

### 1.3 El hueco de mercado, con nombres

- **Noita** (más de un millón de copias vendidas) demostró que hay público masivo para la
  simulación de materia profunda — pero es un roguelite de muerte y caos, sin producción, sin
  hogar, sin co-op.
- **Potion Craft** (100.000 copias en sus tres primeros días de acceso anticipado) demostró
  que hay público masivo para "ser alquimista de taller" — pero su alquimia es un minijuego
  de mapa, sin física real, sin mundo simulado, sin co-op.
- **Factorio/Satisfactory** demostraron el poder adictivo de producir-almacenar-escasear —
  pero en clave industrial fría, sin materia viva.

Este juego está en la intersección exacta de los tres, y esa intersección está VACÍA. Es una
posición real, formulable en una frase de tienda ("Noita meets Potion Craft, en co-op"), y
las frases de tienda que comparan dos éxitos conocidos son las que mejor convierten en Steam.

### 1.4 Los riesgos de diseño, sin maquillaje

**R1 — La curva de materiales (el "aturde" que reportaste).** Es el riesgo #1 y es de
dirección, no de código: la regla debe ser **ningún material entra sin su consumidor** (es la
regla 48 del proyecto aplicada a la economía). El arco actual presenta ~12 y usa ~6. La
restructura debe recortar la primera hora a 6-8 materiales con TODOS en uso, y dosificar el
resto por pedidos del mundo.

**R2 — El ritmo del "mundo pide".** Un solo encargo compuesto es teatro; un generador
infinito mal calibrado es una lista de tareas. El generador debe elegir pedidos que (a) usen
mayoría de cosas que ya sabes hacer y (b) exijan exactamente UNA que no — así cada pedido es
80% ejercicio y 20% frontera. Esto es un solver más (ya tenemos tres: sabemos hacerlos), pero
calibrarlo llevará playtests, no una ronda.

**R3 — La tensión legibilidad/profundidad.** Es la tensión histórica del proyecto (HANDOFF,
"LA TENSIÓN DE FONDO"). El loop nuevo la ALIVIA — producir algo dos veces enseña más que
descubrirlo una — pero el almacén etiquetado tiene que llegar pronto o la profundidad vuelve
a ser niebla.

**R4 — El co-op como promesa central.** Vender co-op obliga a que el co-op esté fino. Las
deudas conocidas (réplica de obras nuevas, texto narrativo del compuesto en el invitado,
re-host) dejan de ser deudas tolerables y pasan a ser bloqueantes de lanzamiento.

**R5 — El arte.** El mayor multiplicador comercial del proyecto y el mayor riesgo de
ejecución. Lo cuantifico en el pronóstico (§3), porque cambia el resultado más que cualquier
mecánica.

---

## 2. COMPLEJIDAD: QUÉ FALTA DE VERDAD Y EN QUÉ ORDEN

Lo ya construido que la visión final reutiliza entero: el motor determinista con química por
semilla (768x288, 30Hz, chunks), el retículo de estados, el crisol por hornadas, la
co-op por espejo de chunks con arco guiado replicado, el encargo compuesto, la alacena, la
obra pagada (mufla), y los tres solvers de garantía. **Nada de eso se tira. La restructura es
de GUION y ECONOMÍA, no de motor.** Eso es lo que hace el plan realista.

Las fases, estimadas en rondas al ritmo real que llevamos (una ronda = una noche/sesión):

**F1 — LA RESTRUCTURA DEL ARCO (3-4 rondas).** Semilla Cero v2: la alacena aparece en el
minuto ~5 (con el primer material producido en cantidad); el primer pedido del mundo llega en
el minuto ~10-12 con la cadena más corta (p. ej. solo carbón + vidrio); los beats de
"pregunta" sobreviven pero INTERCALADOS con pedidos de producción, no en bloque; curva de
materiales recortada a 6-8 con uso garantizado. Incluye el checklist con iconos de tus
mockups y las etiquetas de categoría en el almacén.

**F2 — EL GENERADOR DE PEDIDOS DEL MUNDO (2-3 rondas).** El piloto de la capilla se vuelve
generador: plantillas narrativas (reparar/construir/abastecer a alguien del valle) ×
selección 80/20 descrita en R2 × recompensas que alternan Favor y OBRAS (expansiones físicas
del taller, como la mufla). Con su solver de completabilidad, como todo lo demás.

**F3 — EL TALLER QUE CRECE (2-3 rondas).** Generalizar la mufla: 4-6 obras compradas con
materiales (segunda caldera, almacén ampliado, alambique, sala nueva tallada). Réplica multi
de obras (pagar la deuda de MaquinaSync). Aquí el "lugar pequeño que se me amplía" que pides
desde hace meses queda cumplido con la economía como llave.

**F4 — CO-OP DE LANZAMIENTO (2 rondas).** Pagar R4 entero: compuesto narrativo replicado,
re-host limpio, 3-4 sesiones de prueba con tu amigo como criterio de salida.

**F5 — VESTIDO Y ESCAPARATE (fuera de mi alcance directo, en paralelo).** Arte final al nivel
de tus mockups (cápsula de Steam, 5-6 capturas, tráiler de 60s), página de Steam, demo del
Next Fest (= F1+F2 recortadas a 30-40 min). El código de la demo lo hacemos nosotros; la
cápsula y el key art son la inversión externa que decide el §3.

Total hasta demo de Next Fest: **~10-12 rondas de código** más el arte en paralelo. Al ritmo
actual (2-3 rondas/semana), eso es **5-7 semanas de desarrollo** — holgado para el próximo
Next Fest si el arte arranca ya.

---

## 3. PRONÓSTICO (mojado, sin rangos anchos)

Actualizo mi informe anterior (5.500 copias centro, ~33k USD netos, confianza 70%) porque
cambiaron dos cosas: la visión cerró (el juego ya se puede describir en una frase que vende)
y vi tus mockups (que están a nivel de cápsula comercial). El pronóstico ahora se BIFURCA en
la única variable que de verdad lo mueve:

**Escenario A — lanzar con arte generado por código mejorado (sin artista).**
El juego sería bueno y se vería "de programador". En Steam eso significa que la cápsula no
convierte: pocos clics, pocas wishlists, y el algoritmo no te levanta.
- Next Fest: **~600 wishlists** ganadas (la mediana de un demo correcto sin gancho visual
  ronda 300-500; el co-op y el género nos dan algo de ventaja).
- Lanzamiento con ~3.500 wishlists → **4.200 copias año 1** (banda 3.000-5.500), a $9.99 con
  -15% de salida → **~25.000 USD netos** año 1.

**Escenario B — lanzar con arte al nivel de tus mockups.**
Esas imágenes, como cápsula y capturas, compiten de tú a tú en el nicho cozy/alquimia, que es
de los que mejor convierten en Steam.
- Next Fest: **~2.200 wishlists** ganadas (demo co-op + estética que detiene el scroll).
- Lanzamiento con ~9.500 wishlists → **11.500 copias año 1** (banda 8.000-15.000), a $11.99
  con -15% de salida (el co-op y la profundidad soportan ese precio) → **~72.000 USD netos**
  año 1. Con un golpe de suerte de streamers (el género lo tiene: Noita y Potion Craft
  vivieron de eso), la cola alta es 25.000+ copias; no lo prometo, lo anoto.

**Mi apuesta concreta:** si ejecutamos F1-F5 y el arte llega al nivel del mockup, **11.500
copias y ~72.000 USD netos en el primer año, con 65% de confianza en la banda**. La
probabilidad de que el juego encuentre a su público existiendo (no de éxito viral) la pongo
en 80%: el hueco de mercado es real y la demo es demostrable en 30 minutos. El 20% restante
es riesgo de ejecución (arte que no llega, Fest mal elegido, co-op con bugs el día 1).

Y la frase honesta que resume el informe entero: **a partir de hoy, cada hora invertida en el
arte de la cápsula vale más copias que cada hora invertida en una mecánica nueva.** El motor
ya es más profundo que el 95% de lo que se lanza en el nicho; lo que falta es que se vea.

---

## 4. EL NOMBRE

### 4.1 Las tres estrategias que existen (tu pregunta REPO/Noita)

**(a) Palabra propia, corta y rara** — *Noita* ("bruja" en finés), *Terraria*, *Valheim*.
Ventajas: marca única, SEO limpio (búsquedas solo tuyas), misterio. Coste: el nombre no
explica nada — necesitas que la cápsula y el género lo expliquen por ti. Funciona cuando el
juego es visualmente autoexplicativo.

**(b) Descriptivo funcional** — *PowerWash Simulator*, *Supermarket Simulator*, *R.E.P.O.*
(que juega a acrónimo pero vive de sonar a "repossession", el trabajo que haces). Ventajas:
el nombre ES el pitch, convierte a desconocidos. Coste: genérico, difícil de marcar, envejece
mal si el juego crece más allá de su descripción.

**(c) Híbrido: nombre propio + subtítulo descriptivo** — *Potion Craft: Alchemist Simulator*.
El nombre marca; el subtítulo hace el SEO y el pitch. **Para un juego cuyo concepto no existe
aún en el mercado (nadie busca "alquimia de arena que cae co-op"), esta es la estrategia
correcta, y es la que recomiendo.**

### 4.2 Hallazgo importante antes de proponer

**"Alkahest" está TOMADO en Steam**: existe un action-RPG llamado Alkahest (app 3016620) ya
publicado/anunciado, además del clásico Alcahest de SNES. El nombre del repo NO puede ser el
nombre comercial. Descartado. "Prima Materia" también tiene colisiones (un juego gratuito
"Prima Materia XXV" en Steam, otro en itch, un juego de mesa) — usable legalmente, sucio para
SEO. Descartado con pena.

### 4.3 Candidatos (verificados contra Steam en lo posible)

**1. ATHANOR** *(mi recomendación)* — el atanor es el horno del alquimista diseñado para
mantener fuego constante durante días: literalmente "el fuego alrededor del cual crece el
taller", que es este juego. Corto, sonoro, se pronuncia igual en español e inglés, casi
virgen en Steam (solo un juego amateur de 2014 en MobyGames, sin presencia real). En híbrido:
**"ATHANOR — taller de alquimia"** / **"ATHANOR: a co-op alchemy workshop"**. Riesgo: palabra
desconocida para el gran público — exactamente como Noita, y esa desconocimiento es un activo
si el arte acompaña.

**2. LA VETA / THE SEAM** — nombra el gesto fundacional del juego (tallar la veta de la que
sale todo). Evocador y nuestro; riesgo: "seam" en inglés es ambiguo (costura) y la
traducción pierde la unidad.

**3. VITRIOL** — el acrónimo alquímico real V.I.T.R.I.O.L. ("visita el interior de la tierra
y rectificando encontrarás la piedra oculta" — literalmente el loop del juego: cava, refina,
descubre). Punch enorme, memorable; riesgo serio: en inglés coloquial "vitriol" significa
odio/veneno verbal, tono opuesto al juego cálido de tus mockups.

**4. AZOGUE** — el nombre viejo del mercurio en español; hermoso y nuestro; riesgo: difícil
de pronunciar fuera del español, y "Azoth" (su pariente) está quemado por Genshin en SEO.

**5. LIMO PRIMORDIAL** (el actual) — encanto, pero "limo" en inglés es "limusina": el SEO
global nace roto. Lo guardaría como nombre del material estrella DENTRO del juego, que es
donde ya vive.

**Mi voto, mojándome: ATHANOR, con subtítulo descriptivo por idioma.** Antes de sellarlo:
búsqueda de marca (EUIPO/USPTO clase 9/41) y reservar el nombre en Steamworks — la página de
Steam conviene crearla YA de todos modos, porque las wishlists pre-Fest son el capital que
el Fest multiplica.

---

## 5. QUÉ PROPONGO HACER AHORA MISMO

El detalle que reportaste (el pedido de la columna) ya está corregido y desplegado: el texto
ahora enseña el experimento (la columna es para MIRAR), el criterio (flota sin deshacerse =
menos cuerpo que el agua) y el lugar de entrega (la Tolva), con la turba nombrada como camino
garantizado — y sigue aceptando cualquier material del universo que cumpla, como pediste.

Mi siguiente ronda natural es **F1: la restructura del arco** (el loop completo desde el
minuto 5, curva de materiales recortada, checklist con iconos, almacén etiquetado con el arte
actual). Tu decisión pendiente más valiosa no es de código: es **cuándo y cómo arranca el
arte de la cápsula**, porque es la variable que separa el escenario A del B.
