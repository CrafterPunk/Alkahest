# TEN THOUSAND YEARS — GDD v0.2

> **Rebuild human knowledge from mud, fire, and observation.**
> *Reconstruye el conocimiento humano con barro, fuego y observación.*

*(Fable, ronda 59. v0.2 integra TODAS las respuestas de Cesar a la v0.1: el inicio oscuro
minimalista, el guardado con slots, el buzón de salida simétrico, el stock por tiempo, la
semilla de autor, el destino del modo caótico y el renombre del repo. Sigue siendo NORMATIVO
y sigue marcando SELLADO/ABIERTO. Para releer completo, tachar y devolver.)*

---

## 1. FICHA

| Campo | Valor |
|---|---|
| Título | **TEN THOUSAND YEARS** |
| Eslogan | *Rebuild human knowledge from mud, fire, and observation.* |
| Género | Simulación de materia (falling-sand) + taller/producción + co-op |
| Una frase de tienda | *Noita meets Potion Craft, en co-op: funda tu taller desde el barro y reconstruye el conocimiento humano.* |
| Jugadores | 1-4 (co-op drop-in, sim en el host + espejo por chunks) |
| Modos | **Campaña** (semilla de autor — el juego) + **Modo Caótico** (semilla libre, LEGADO: se conserva y en la demo/página aparece como promesa BLOQUEADA; su rediseño queda para después del Fest — lo único ya decidido de ese rediseño es que NO arrancará con todas las máquinas puestas) |
| Guardado | **Sí, desde la demo**: sesiones guardables con SLOTS LIMITADOS (propuesta: 3) — una partida con cada grupo de amigos y/o una en solitario |
| Plataforma | PC/Steam (Windows primero) |
| Precio objetivo | $11.99 (-15% lanzamiento) |
| Motor | Unity 6.5 (6000.5.7f1, congelada hasta después del Fest), autómata celular determinista propio (768x288 @30Hz, ampliable) |
| Idiomas | Español latino + inglés desde el día uno |
| Modelo | Early Access con las primeras 2 Edades → 1.0 con 4+ |
| Repo | `TenThousandYears` (renombre en la fase de Limpieza; ver nota operativa §13) |

## 2. LA VISIÓN

Empiezas en la oscuridad de un rincón, con el único fuego vivo del mundo y una gotera que
cae sobre él sin apagarlo. Un Maestro viejo lo cuida. Diez mil años de conocimiento humano
—cerámica, vidrio, cal, metal— están dormidos en la materia, esperando que alguien los
redescubra MIRANDO: qué flota, qué arde, qué aguanta el fuego sin ceder. Cada cosa que
aprendes se produce, se guarda en estantes que tú construiste, se gasta en pedidos de un
mundo que necesita cosas, y cada escasez te empuja a la siguiente pregunta.

**Los cuatro pilares (todo lo que entre debe servir al menos a uno; lo que contradiga a
uno, no entra):**

1. **La materia es real.** Todo es simulación celular: lo que se derrama se derrama, lo que
   arde se propaga, lo que flota flota. El árbol tecnológico es la física. No hay recetas de
   menú: hay observación.
2. **El taller es tuyo porque lo fundaste.** Empiezas sin nada y todo se craftea, incluidos
   los lugares donde guardas. El taller ES el inventario.
3. **El conocimiento es la progresión.** No subes de nivel: aprendes. Las Edades del
   conocimiento son los capítulos.
4. **El co-op no tiene clases: tiene estantes.** Los roles emergen de la escasez visible,
   nunca de un selector.

**Sellado:** los cuatro pilares; cuando 2 y 3 choquen, gana el 2 (lo físico manda).

## 3. EL LOOP

**Macro:**

> descubrir → producir → almacenar → utilizar → escasear → repartir trabajo → necesitar algo nuevo → descubrir

**Micro (un minuto cualquiera):** miro el pedido activo → miro mis estantes (¿qué falta?) →
produzco, experimento o pido al Maestro → guardo o entrego → algo pasa en la materia
(evidencia, accidente, sorpresa) → anoto/bautizo → vuelvo a mirar el pedido.

**Regla de ritmo (sellada):** primeros 15 minutos, un verbo nuevo o una entrega cada 60-90
segundos. Vacío de cosas, jamás de acciones.

**Regla de material (sellada, regla 48 generalizada):** ningún material entra sin su
CONSUMIDOR presente. La primera hora vive con 6-8 materiales, todos en uso.

## 4. LAS EDADES

Cada Edad: un grupo de páginas del Libro Mayor, 2-4 materiales nuevos CON consumidor, 1-2
obras nuevas, y un salto visible del taller. No se anuncia con cartel: se atraviesa, y al
mirar atrás el taller la cuenta.

| Edad | Materiales eje | Obras/estaciones | El pedido que la abre |
|---|---|---|---|
| **I. Barro y Fuego** | barro/arcilla, turba, carbón, ceniza, cerámica | fogón → crisol de barro, primer estante, primeros frascos | la fundación (§5) |
| **II. Vidrio** | arena, vidrio, barbotina/engobe | horno mejorado, alacena grande, ventanas | "los vitrales de la capilla" (pt56) |
| **III. Cal y Piedra** | caliza, cal viva/apagada, mortero | la mufla (pt56), obra de albañilería | "la mufla del vidriero" → reparaciones del mundo |
| **IV. Metal** *(post-EA inicial)* | mena, metal, escoria | fragua, moldes | por diseñar |
| V+ *(1.0+)* | por diseñar | alambique... | por diseñar |

**Sellado:** las Edades; I-III con materiales existentes (cero nuevos hasta la IV).

## 5. EL INICIO OSCURO Y LA FUNDACIÓN *(reescrito entero con tu respuesta)*

**Principio rector (sellado): minimalismo extremo.** En pantalla no existe NADA que no haya
entrado por un favor del Maestro, un préstamo o tus manos. Sin HUD hasta que tienes frasco.
Sin listas de recursos a la vista. El mundo no se presenta: se revela, una cosa por favor.

**El encuadre cero (la pantalla del minuto 0):** oscuridad. Lo único visible: el rincón del
Maestro — sus brasas, él, y UNA GOTERA que cae sin cesar sobre las brasas, chisporrotea y
sube como vapor. **La primera reacción del juego ya está ocurriendo sola, antes de que
toques nada.** El jugador solo puede moverse y mirar. (Ejecución visual del "oscuro":
ABIERTA — viñeta/backdrop apagado que se enciende por zonas alrededor del fuego; se decide
con el arte, la técnica barata existe.)

**La secuencia fundacional v2 (estructura sellada; cantidades y textos abiertos):**

1. **El saludo.** Acercarte al Maestro (el primer verbo es social). Te PRESTA su frasco
   viejo y la primera frase del juego. Nada más aparece.
2. **La gotera.** Su primer favor a cambio: *"esa gotera me va a apagar el fuego un día —
   atrápala."* Aspiras el goteo (primer uso del frasco, sobre un fenómeno que ya estaba
   vivo). Al entregarle el agua, él "arregla" la gotera... convirtiéndola en **TU GRIFO**:
   la gotera domesticada es el primer aparato del juego. Los fenómenos no se eliminan — se
   domestican. (Ese es el juego entero en un gesto.)
3. **El barro.** Agua del grifo + tierra → barro; te enseña a cocerlo EN SUS brasas → tu
   primera cerámica → **tu FRASCO propio** (le devuelves el suyo: el préstamo se devuelve
   aprendiendo — tema del juego).
4. **El fuego propio.** Te da un puñado de turba de su morral y señala la pared donde la
   veta APENAS asoma (el asomo de 3 celdas ya existe). Tallas, enciendes **TU fogón** —
   primera obra. Ya no dependes de sus brasas.
5. **El vidrio y el estante.** Arena + ceniza en tu fogón → tu primer VIDRIO → lo entregas →
   te ayuda a levantar **tu primer ESTANTE**. Desde aquí existe "¿dónde lo pongo?".
6. **El primer trueque.** Se abre en el Libro Mayor la página del matraz: tu primer PEDIDO a
   él, con tiempo de entrega — la primera espera, la primera anticipación.
7. **La primera observación.** Primer pedido con experimento real (patrón
   pregunta-experimento-entrega, ya corregido en pt57: el pedido SIEMPRE dice el
   experimento, el criterio y el lugar de entrega). El Libro Mayor muestra su horizonte y el
   mundo empieza a pedir.

A partir del pedido ~8: **dos pedidos del mundo abiertos a la vez**, compartiendo algún
material. La linealidad de 1-7 es deliberada y no se disculpa.

## 6. LA ECONOMÍA — EL TRUEQUE Y EL MAESTRO DE DOBLE ENTRADA

Sin moneda. Sin Favor (retirado). Se paga con materia, herramienta u obra.

**Las reglas del trueque (selladas):**

- **Tablón de precios físico, fijo por semilla.** Junto a su mesa. Sin regateo.
- **Stock limitado que se repone POR TIEMPO, en ciclos cortos.** (Decidido. La idea de que
  las entregas acorten el tiempo queda APARCADA en tu pizarra, a propósito — no entra ahora.)
- **Nunca vende descubrimientos.** La frontera se cruza experimentando, sin excepción.
- **Alquilar o poseer.** Las máquinas complejas, en dos precios: por esta vez o para siempre.
- **Lo que TÚ pides, TARDA; lo que TE piden, ESPERA.** Tiempos de entrega cortos (1-4 min de
  juego). Los encargos del mundo jamás llevan deadline.

**Los DOS BUZONES de la mesa (decidido: físicos y simétricos).** A un lado de la mesa del
Maestro, el **Buzón de ENTRADA** (ya existe: ahí entregas lo que el mundo pide); al otro
lado, el **Buzón de SALIDA** (nuevo: ahí dejas/consultas tus pedidos a él). La simetría es
funcional, no estética: en co-op, VER a quién se acerca a qué lado de la mesa dice quién
está en qué tarea — el intendente vive en el lado de salida sin que nadie lo nombre.

**La primera unidad se construye a mano; las repeticiones se piden.** El crafteo enseña; el
trueque abastece.

## 7. EL LIBRO MAYOR

Físico, sobre la mesa. Muestra tres cosas: lo desbloqueado (historial), **el horizonte** (los
siguientes 2-3 con requisitos concretos), y la promesa contada (*"...y 34 páginas más que
aún no puedes leer"*). Las Edades son sus capítulos. No hay menú de progresión fuera de él.

## 8. EL ALMACENAMIENTO FÍSICO

Todo lugar de guardado se craftea. Cada contenedor muestra contenido real, nombre y nivel.
**Carteles del jugador**: los construyes, los colocas y escribes lo que quieras (reutiliza
bautizar) — tú lideras tu organización; el juego no impone taxonomía. Meta medible: "¿cuánto
X nos queda?" se responde mirando en <3 segundos. Perder lo almacenado es posible (es la
simulación) pero siempre parcial, recuperable y con lección (reglas 44/54).

## 9. EL CONOCIMIENTO

Se conserva del juego actual, reorientado: descubrir mirando (eventos → diario; leyes por
semilla con tesis nombrable), bautizar (regla 13), **ficha a la vista + libro** (la ficha
deriva del libro y no lo reemplaza — a testear que el libro siga abriéndose), y evidencia
forense de todo fracaso (regla 54; la ceniza es el material bisagra de la Edad II).

## 10. EL PELIGRO — VOLÁTILES *(post-Fest, diseñado desde ya)*

Categoría "volátil" (explota por agitación o temperatura, siempre con base real). Tres
guardas selladas: accidente = lección, nunca partida perdida; la contención se craftea y
llega ANTES que el volátil; la volatilidad avisa antes (tiembla/humea). Función comercial:
los accidentes co-op son la fábrica de clips.

## 11. CO-OP

1-4; sim en host + espejo por chunks (arquitectura probada). Paridad de teatro total (deuda
del texto compuesto: se paga pre-Fest). Roles emergentes de la economía: el intendente nace
del Buzón de Salida; productor, explorador y organizador nacen de las cadenas, la frontera y
los carteles. Las obras nuevas deben ser usables por el invitado (deuda MaquinaSync: se paga
en la Limpieza). **El guardado por slots permite una partida por grupo de amigos.**

## 12. LO QUE ESTE JUEGO **NO** ES

No es roguelite (el mundo y el taller PERSISTEN — ahora con guardado real). Sin deadlines en
lo que el mundo pide. Sin inventario abstracto. Sin árbol tecnológico de menú. Sin moneda ni
puntuación. Sin combate. Sin automatización (si entra algún día, premio de lategame). Sin
clases. Sin innovación en estructuras de pedidos: la revolución es la materia. Y la campaña
no arranca con máquinas puestas — NADA se salta la fundación (el caótico legado es la única
excepción, y bloqueada hasta su rediseño).

## 13. HOJA DE RUTA AL NEXT FEST

**La demo (30-40 min, todo el loop):** inicio oscuro + fundación completa (§5) + Libro Mayor
con horizonte + 2 pedidos del mundo + trueque con tablón y dos buzones + guardado (1-3
slots) + los accidentes que la sim regala. Modo caótico visible pero BLOQUEADO (promesa).
Semilla de autor 1000% (todo lo aprendido con 777002 aplica).

| Fase | Contenido | Rondas est. |
|---|---|---|
| **L. Limpieza** | tag `ultimo-clasico`; fuera Favor, arco clásico y muertos; caótico se conserva pero deja de ser puerta principal; reglas en dos montones; **renombre del repo a `TenThousandYears`** | 3-4 |
| **F1. El inicio oscuro** | encuadre cero, secuencia fundacional v2 (saludo→gotera→grifo→barro→fogón→vidrio→estante), crafteo de estantes/carteles, ficha a la vista | 3-4 |
| **F2. La economía** | trueque + tablón + stock por tiempo + Buzón de Salida + tiempos de entrega + Libro Mayor + rent-vs-own | 2-3 |
| **G. El guardado** | serialización de mundo (mat/temp/morph RLE) + conocimiento + Libro Mayor + obras; slots limitados; carga desde el título | 2 |
| **F3. El mundo pide** | generador 80/20 sobre plantillas; dos pedidos abiertos; obras como recompensa | 2-3 |
| **F4. Co-op fino** | paridad compuestos, obras usables por invitado, re-host, guardado en multi (guarda el HOST), sesiones con amigos | 2 |
| **F5. Escaparate** | build demo con checklist, página Steam, cápsula/capturas (arte externo en paralelo) | 2 + arte |

Total: **~17 rondas** (6-8 semanas al ritmo actual) + arte en paralelo desde YA.

**Nota operativa del repo (decisión tuya pendiente):** el renombre a `TenThousandYears` lo
haces tú en GitHub Settings (yo no toco el remoto, regla 6b) y el `.cmd` siguiente ajusta el
`remote set-url`. Sobre volverlo PRIVADO: ojo — mi red de seguridad ante reinicios del
sandbox es clonar de GitHub; con el repo privado ese camino se corta salvo que me des un
token de solo-lectura (fine-grained PAT con acceso a ese repo) para poner en la URL del
remoto. Opciones: (a) privado + token de lectura que me pasas una vez, (b) público hasta el
Fest (la oscuridad práctica de un repo sin estrellas es casi la misma). Cualquiera me sirve;
si eliges (a), pásame el token en tu próximo mensaje y lo dejo configurado.

## 14. PREGUNTAS ABIERTAS PARA LA v0.3

1. ¿La secuencia fundacional v2 (§5) captura tu inicio oscuro? El gesto "la gotera se vuelve
   tu grifo" es la traducción que hice de tu idea — tacha o bendice.
2. ¿Cuántos slots de guardado? Propongo 3 (el costo por slot es disco, no código).
3. La ejecución visual del inicio oscuro (viñeta vs. backdrop por zonas): ¿la decidimos con
   el arte o quieres verla en gris antes?
4. Repo privado: ¿opción (a) con token o (b) público hasta el Fest?
