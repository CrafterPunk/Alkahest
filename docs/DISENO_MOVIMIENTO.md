# EL MOVIMIENTO DEL MUÑECO — investigación de diseño (R119, decisión ABIERTA)

> Pedido de Cesar tras probar la caminata R118: "no es una decisión tomada aún,
> veo riesgo en las 3 opciones y no sé cuál es más difícil de sortear". Este
> documento pone las tres sobre la mesa con referencias, riesgos y mitigaciones.
> La física ya construida (R118b) sirve a CUALQUIERA de las tres: gravedad,
> paso a pie, despegue, aterrizaje — elegir es cuestión de PODAR, no de rehacer.

## 0. El dato de campo que manda

Observación de Cesar en playtests: los jugadores que vuelan quedan
"constantemente un poco más lejos de lo que necesitan, quieren hacer algo fuera
de su alcance y corrigen". El juego es de VERTER y por eso se siente bien
hacerlo con precisión. Esa fricción existe en el 99 % de los juegos premiados y
se tolera — cierto — pero es LA variable de esta decisión: el vuelo libre da
alcance a cambio de precisión, y nuestro gameplay premia la precisión.

Hay una mitigación transversal que sirva cual sea la opción, y propongo hacerla
pronto porque es barata: **el ancla de trabajo** — mientras el jugador VIERTE o
ASPIRA (botón del frasco presionado), la velocidad se amortigua fuerte y el
personaje se queda clavado en el aire o en el suelo. Trabajar ancla; soltar el
botón libera. Ataca la fricción observada sin tocar la mecánica de moverse.

## 1. Opción A — solo vuela (el imp original, ahora muñeco)

**Qué es.** Sin gravedad jugable: WASD en el plano, como fue el juego 70 rondas.

**Referencias reales** (existen, aunque pocas): el modo clásico de **Solar Jetman**
/ **Lunar Lander** (nave, no personaje), **Broken Sky/Flappy** no aplican; los
personajes-que-solo-flotan viven casi siempre en juegos de nave con disfraz de
personaje. El pariente serio más cercano: el dron de **Astroneer** o el modo
creativo de **Minecraft** — y justamente se sienten "modo editor", no personaje.
Ese es el dato: volar-siempre lee como HERRAMIENTA, no como criatura.

**Riesgos.** (1) Mixamo no tiene levitación/despegue listos — cierto; habría que
generarlos por prompt (ruta 4: micro-loops de flotar desde el mismo cuadro, que
es su nicho bueno) o animar 3 poses a mano en Blender sobre el Y Bot (flotar,
inclinarse adelante, frenar: 3 poses + interpolación bastan para un flotador).
(2) Se pierde TODO lo ganado en R118: la caminata que "trae mucho estilo", el
peso, los pies en la roca. (3) La fricción de precisión queda intacta.

**A favor.** Cero fricción de desplazamiento vertical (el mundo es alto); el
diseño de niveles actual ya lo asume; ninguna mecánica nueva que enseñar.

## 2. Opción B — camina, salta/despega y vuela (lo construido en R118b)

**Qué es.** A pie por defecto (gravedad, paso 1.1 u/s), W/Espacio despega, vuelo
libre como siempre, S aterriza. Ya está en el juego y Cesar ya lo probó.

**Las referencias de control en PC que valen la pena estudiar:**

- **Noita** — LA referencia hermana (¡también es falling-sand!): se camina y
  salta; la levitación es un RECURSO con medidor que se recarga al pisar suelo.
  Resultado: el suelo importa, el vuelo es un gasto táctico, y el jugador vive
  "ligado" al terreno sin que nadie se lo imponga. Su medidor existe por el
  combate/peligro; nosotros no tenemos monstruos, pero el mismo lazo se logra
  con suavidad (abajo).
- **Terraria** (alas/botas cohete): vuelo con TIEMPO limitado que se renueva al
  tocar suelo; descenso planeado lento al agotarse — exactamente la idea de
  Cesar de "descender lentamente después de un tiempo". Décadas de jugadores lo
  encuentran natural.
- **Starbound** (tecla de propulsor): vuelo libre pleno pero como TECH que se
  activa; el resto del juego es plataformeo. Control: doble-salto/mantener.
- **Ori and the Will of the Wisps** (pluma/planear) y **Hollow Knight** (alas):
  el listón de CÓMO se siente un salto+planeo pulido, aunque sin vuelo libre.
- **Cave Story / Broforce** con jetpacks: mantener-para-subir, soltar-para-caer:
  el esquema de input más legible para "vuelo con gravedad de fondo".

**Riesgos.** (1) El señalado por Cesar: las mecánicas de "atar al suelo" suelen
existir por peligro (enemigos, caída), y aquí no hay; un límite duro de vuelo
podría sentirse arbitrario y castigar el alcance en un mundo ALTO. (2) Dos
regímenes = más que enseñar y más bugs de borde (ya vimos: foco, sondas, popeos
de modo). (3) Si el 90 % del tiempo conviene volar, caminar se vuelve adorno
caro (el riesgo de la ruta muerta).

**Mitigaciones concretas.** El lazo al suelo SIN castigo: (a) el ancla de
trabajo del §0 — verter con pies plantados es simplemente MEJOR (estabilidad
instantánea), así el suelo se elige solo cerca del trabajo fino; (b) descenso
suave estilo Terraria como el techo de la comodidad, no de la capacidad;
(c) dejar la V de vuelo pleno para siempre (nada de medidores duros por ahora).
Nota de coherencia de fantasía: un muñeco de remiendos con un brote en la
cabeza que FLOTA se acepta solo — es magia del taller; el "por qué" narrativo
no nos cuesta nada.

## 3. Opción C — solo camina, corre y salta

**Qué es.** Plataformeo puro; el vuelo se retira.

**Referencias.** **Hollow Knight**, **Ori**, **Celeste** (el estándar de sentir
un salto), y del lado "trabajo con materiales": **Oxygen Not Included** y
**Craftopia/Core Keeper** — los personajes que gestionan mundos de recursos casi
siempre CAMINAN, y eso concentra la atención en el trabajo.

**A favor (fuerte).** Le da sentido pleno al CINCEL como herramienta de
movilidad (tallar rampas, escaleras, túneles para llegar) — el juego de moldear
el terreno se volvería el doble de significativo, alineado con la tesis de
reparar/moldear. Máxima precisión para verter. Un solo régimen: menos que
enseñar, menos que mantener, y toda la biblioteca de animación de Mixamo
disponible (saltar, trepar, empujar...).

**Riesgos.** (1) El mundo actual está DISEÑADO para un volador: cargas en alto,
silos, techos — la ronda estructural de niveles sería grande (rampas, cornisas,
andamios; o el cincel como pico obligatorio desde el minuto 1). (2) Tiempo de
desplazamiento: en un mundo alto, caminar puede volverse paseo vacío (el riesgo
que Cesar nombra: "que no le sume al gameplay"). (3) Co-op: esperar al que sube.

## 4. Lectura de dirección (opinión, no decreto)

La B es la única que deja las otras dos VIVAS: la A es "B sin usar los pies" y
la C es "B sin usar la V" — se pueden probar por playtest sin tirar código. Por
eso el orden que propongo no es elegir hoy, sino instrumentar la B para que los
testers nos elijan la respuesta:

1. **Ya**: el ancla de trabajo (§0). Ataca la fricción real observada, sirve a
   las tres opciones, y es una tarde de trabajo.
2. **Siguiente playtest**: B como está + telemetría boba (¿% del tiempo a pie
   vs volando? ¿desde dónde vierten: aire o suelo? ¿usan el cincel para
   moverse?). Tres números que deciden más que tres debates.
3. **Si el suelo gana** (la gente trabaja a pie y vuela solo para viajar):
   probar el descenso suave de Terraria y el salto de verdad (Espacio = brinco,
   mantener = despegar), y considerar C-suave: vuelo como "doble salto largo".
4. **Si el aire gana** (nadie pisa): la caminata queda de cosmética social y de
   mudanza (que YA es a pie de facto con el capataz), y la A se asume con
   animación de flotar hecha por prompt (ruta 4, su nicho).

Lo que NO recomiendo: medidor duro de vuelo (castigo sin peligro que lo
justifique) y decidir hoy sin los tres números del playtest.

— R119. La física de ambos regímenes vive en `ApprenticeController.HandleMovement`
(R118b); el ancla de trabajo entraría en `Flask.cs` + una consulta `Anclado` aquí.
