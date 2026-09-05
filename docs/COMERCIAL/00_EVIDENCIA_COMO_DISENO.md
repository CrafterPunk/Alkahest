# 00 · LA EVIDENCIA DEL LABORATORIO, LEÍDA COMO DISEÑADOR

*(Fable 5.1, 2026-09-05. Base común de todas las propuestas de `docs/COMERCIAL/`. Nada de esto es
una decisión de diseño: es lo que el sustrato hace, lo que no hace y lo que cuesta, traducido a
lo que un juego puede prometer sin mentir. Fuente: `docs/LAB/INFORME_FINAL.md` y sus benchmarks.)*

## 1. Lo que el sustrato hace bien, y la fantasía que habilita

| hecho medido | fantasía que sostiene |
|---|---|
| **Nada aparece ni desaparece.** El agua se conserva al bit; el carbón conserva la energía; el libro cuadra. | Una economía honesta sin inventario: lo que tienes es lo que hay en el mundo, donde está. La escasez es geografía. |
| **La causalidad es espacial y lenta.** Minutos, columnas, recintos. El calor no cruza sólidos; la luz tampoco; el agua busca cuerpos conectados. | Colocar, esperar, observar, corregir. Un juego de *sitio*, no de reflejos. La cámara y el ritmo deben servir a eso. |
| **Las máquinas son geometría.** Alambique = bloque frío bajo techo + agua sobre calor. Horno = recinto. Carbonera = pila tapada con una boca. Tolva = silo sobre fogón. Ninguna tiene una línea de código. | «Lo inventé yo» es verdad literal. No hay recetas: hay formas. La expresión y el conocimiento son la misma cosa. |
| **Los aparatos chocan.** El alambique que riega ahoga el huerto (R135) y lo deja a oscuras (R148). Una pila maciza es su propia carbonera. El humo que llega a una cámara la apaga. | El drama del juego sale solo: cada solución crea el siguiente problema. En co-op, ese drama es entre personas. |
| **Coste acotado y tiempo comprimible.** 1,6-1,9 ms/tick; ×10 sostenido; los chunks dormidos no cuestan. | Mundos grandes casi dormidos son baratos; los procesos lentos se pueden acelerar sin cambiar la física. |
| **Determinismo en una máquina.** | Repeticiones, semillas compartibles, «mira lo que pasó» reproducible. No es (aún) base para lockstep. |

## 2. Lo que NO hace, y lo que ningún concepto debe prometer

| negativo medido | consecuencia de diseño |
|---|---|
| **El tiro de chimenea no existe**: el aire no se gasta. | Los únicos mandos del fuego son el **recinto** (retención) y el **contacto** (cuánta superficie toca aire). No prometer válvulas de aire ni fuelles; prometer paredes y bocas. |
| **El huerto nunca vivió**: la luz no atraviesa sólidos (7 de 73 caras iluminadas); el goteo moja columnas, no lechos. | La **luz es un recurso** tan real como el agua, y la geometría de bocas y serpentines es diseño de nivel. Eso no es un fallo: es el mejor conflicto del juego (§3). |
| **Nada produce sin volver a tocarlo**: falta alimentación no manual, recogida del producto y ciclo cerrado. | La automatización real hay que **diseñarla como geometría** (canales, pozos, recipientes) y verificarla; es la incógnita de diseño más importante, no de física. |
| **Estados invisibles**: temperatura, humedad, carga y luz solo se ven con F8. Plantas de un píxel. | La observabilidad no es una capa de pulido: es **el lenguaje con el que el jugador aprende las leyes**. Sin ella no hay juego. (§5) |
| **Descubrimiento, diversión, tedio y onboarding: sin evaluar.** | Toda propuesta lleva una incógnita crítica de juego que solo una prueba con personas resuelve. Hay que decir cuál. |
| **Lockstep sin probar.** | Co-op = host autoritativo con espejo. Cuatro jugadores en un mundo, sí; simulaciones paralelas sincronizadas, no todavía. |

## 3. Los cuatro comunes

Lo que el laboratorio descubrió sin buscarlo es que todo aparato consume o produce **cuatro cosas
que se comparten por geografía** y que no caben en un inventario:

| común | cómo se comporta | quién lo quiere | quién lo estropea |
|---|---|---|---|
| **Luz** | baja por bocas verticales, no atraviesa sólidos, la apagan el humo y los techos | el huerto | el serpentín, el humo, cualquier techo |
| **Agua** | se conserva, busca nivel, se filtra, se evapora y vuelve donde hay frío | huerto, caldera, decantación | quien la hierve, quien la desvía, quien la ensucia |
| **Calor** | se retiene en recintos, se escapa por aire, se transmite mal por roca | horno, vidrio, caldera | el agua, el aire abierto, quien abre una pared |
| **Aire (espacio libre)** | es lo que respira el fuego, por donde sube el humo, donde cabe una máquina | todo lo que arde | el humo, el agua, el propio combustible apilado |

Un juego sobre este sustrato es, quiera o no, un juego sobre **cómo se reparten esos cuatro
comunes entre las máquinas de un mismo lugar**. En solitario, entre tus propias máquinas; en
co-op, entre las de cada uno. Ese es el motor de historias («tu horno me dejó sin luz») y el
motor de la progresión: entender un común es poder robárselo a nadie.

## 4. Ritmo y escala que el sustrato pide

- **Unidad de tiempo del juego: el minuto**, no el segundo. Un alambique tarda 7-84 s en gotear;
  una carbonera, minutos; un huerto, decenas. El ×10 existe y es barato: el jugador debe poder
  «dejar pasar la tarde» mirando, no esperar.
- **Unidad de espacio: la cámara** (una habitación de decenas de celdas), no la celda ni el
  mundo. Los aparatos ocupan 20×12; los conflictos ocurren entre cámaras vecinas por los flujos
  que las cruzan (chimeneas, bocas, grietas).
- **Vertical manda.** El calor y el humo suben; el agua y la luz bajan; la presión busca nivel.
  Un mundo apilado en vertical conecta a los jugadores por física sin que nadie lo diseñe.
- **El avatar puede ser pequeño o no existir**, pero la relación con la materia tiene que ser
  táctil y local: lo que el laboratorio enseña se aprende tocando una celda, no un menú.

## 5. Observabilidad: de campo a lenguaje

El campo existe; falta la frase. Cada estado tiene que decirse **en la propia celda y en el
propio sonido**, sin panel, y la transición que viene tiene que anunciarse antes de llegar.

| campo / proceso | lo que el jugador debe leer | lenguaje visual (resolución perceptiva > física) | sonido |
|---|---|---|---|
| temperatura | tibio, caliente, al rojo, a punto de cambiar | gradiente de tono, calima que tiembla sobre lo caliente, brillo al rojo con pulso, escarcha cristalina en lo frío | crepitar, siseo, silencio del frío |
| humedad en poroso | seco, húmedo, empapado, saturado | oscurecimiento, brillo de superficie, gotas que asoman, charco que se forma | goteo, chapoteo, el silencio del seco |
| vapor en el aire | saturándose, a punto de condensar | neblina que espesa, perlas en los bordes fríos | susurro que sube de tono |
| turbidez / carga | limpia, turbia, colmatada, fértil | color y partículas en suspensión, decantación visible, lodo que se asienta, tierra que oscurece | agua que «engorda» |
| reposo / quietud | se está asentando, se está compactando, va a vidriar | grano que se aprieta, grietas que cierran, vidrio que se vuelve liso | crujido de asiento |
| luz | cuánta llega, qué la tapa | haz visible con polvo, sombras netas, brotes que giran hacia ella | — |
| germinación / marchitez | va a nacer, sufre, muere | brote de varias celdas que se yergue o cae, hoja que se arruga, raíz que asoma | crecimiento apenas audible |
| combustión | respira, se ahoga, se vuelve carbón | llama alta o sorda, humo que espesa, negro mate que aparece | rugido o ahogo |
| cercanía a una transición | «algo va a pasar aquí» | vibración, brillo intermitente, borde que cambia | tono que sube |

Dos ideas que salen de la evidencia, no del gusto:

- **Los instrumentos son materia.** Un termómetro es una tira de material que cambia de color;
  un pluviómetro es un recipiente; una veleta de humo es humo. La percepción se **construye y se
  coloca** donde hace falta: eso convierte «modo de visión» en objeto del mundo y en decisión.
- **La transición se anuncia.** El laboratorio ya cuenta «visitas al rojo» y «reposo»: son
  contadores que pueden dibujarse como tensión creciente. El jugador aprende a leer *antes* de
  que pase, que es lo que separa a un observador de un espectador.

## 6. Multiplayer: lo que el motor permite y lo que el diseño puede pedir

- **Permite hoy** (ruta A, 3-4 semanas): un mundo simulado en el host, 2-4 espejos que ven
  material y, cuantizados, temperatura y humedad; todos tocan la materia; la física es una.
- **No permite aún**: simulaciones paralelas sincronizadas (lockstep) ni mundos separados por
  jugador con física propia en cada máquina.
- **Lo que el diseño puede pedir sin nueva tecnología**: jugadores en cámaras distintas del
  mismo mundo, conectadas por flujos verticales (agua abajo, humo y calor arriba, luz por bocas
  compartidas). La causalidad compartida sale de la geografía, no de la red.
- **Número**: 2-3 es donde la culpa y la ayuda son legibles («fue tu horno»); 4 sigue siendo
  legible si el mundo está apilado en cámaras con un flujo entre cada par; más de 4 diluye la
  autoría de la consecuencia y exige otra estructura.

## 7. Onboarding: lo que sugiere la evidencia (sin prueba con personas)

- La mejor lección del laboratorio la dio el propio mundo: **la máquina que riega es la que
  hace sombra**. Un inicio que ponga al jugador ante esa escena (un huerto que muere bajo el
  aparato que lo salva) enseña observación y causalidad sin una palabra.
- Las leyes se aprenden **por contraste**: el hogar no vidria y el recinto sí; la pila suelta
  arde y la tapada carboniza. Dos situaciones iguales salvo en una cosa.
- El primer aparato tiene que ser **una forma, no una receta**: un bloque frío bajo un techo. Si
  el primer gesto del jugador es «pon esto ahí y mira», la enciclopedia sobra.
- Todo esto es hipótesis: **ningún jugador ha tocado la build**. Es la incógnita crítica de
  cualquier propuesta y la primera prueba a hacer.

## 8. Los negativos como activos

- Sin tiro → el fuego se domina con **paredes y bocas**: más legible que un fuelle.
- El huerto sin luz → la **luz es un común**: el conflicto huerto/alambique es un tutorial y una
  historia.
- La pila maciza que se carboniza sola → **los accidentes son leyes**: el jugador puede
  descubrir la carbonera por error.
- Nada produce solo → la **automatización es el capítulo que falta**, y es de geometría: el juego
  tiene ahí su progresión tardía (canales, pozos, recipientes) sin tocar la física.
- Estados invisibles → **la observabilidad es la identidad visual**: la materia que habla es lo
  que un GIF enseña en diez segundos y lo que nadie copia sin una simulación debajo.
