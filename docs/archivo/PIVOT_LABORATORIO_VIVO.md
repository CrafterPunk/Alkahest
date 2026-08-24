# PIVOT — El laboratorio vivo

*Plan de diseño e implementación. Opus 5, para Cesar.*
*Responde a los 10 puntos pedidos. Nada de esto está implementado todavía.*

---

## La idea que ata el pivot entero

Hay una tensión en tu petición: quieres un comienzo **pequeño e íntimo**, pero también conservar
**el mapa grande**, el cincel y el taller. Parecen incompatibles. No lo son, y la manera de
resolverlo estaba ya en tu propio proyecto:

> **El taller grande sigue ahí. Está enterrado.**
> Empiezas en una cámara pequeña excavada en la roca madre. Todo lo demás del mundo —las cubas, el
> banco de grifos, la Tolva, el sótano— sigue existiendo exactamente donde está, **pero relleno de
> bedrock**. No se ve porque nadie ha cavado todavía.

Con eso, de un solo golpe:
- El espacio inicial es **pequeño, vacío, cálido y legible**, como pides.
- El mapa grande **no se tira**: se conserva entero, solo que sepultado.
- **El cincel deja de ser una curiosidad y pasa a ser el motor del juego**: cavar es cómo el
  laboratorio crece. "Descubriendo lentamente qué clase de laboratorio extraño estoy construyendo"
  deja de ser una frase y pasa a ser literalmente la mecánica.
- El "desbloqueo de áreas por niveles" que llevas pidiendo desde hace rondas **ya no necesita un
  sistema**: es piedra y un cincel.
- La pantalla queda limpia sin apagar nada: **no hay nada que enseñar porque no hay nada excavado**.

La pequeñez deja de ser una limitación y pasa a ser una promesa.

---

## 1. Qué conservo del prototipo actual

Prácticamente todo. La capa de sistemas es justo lo que hace que una criatura pueda estar VIVA de
verdad y no ser una animación.

| Se queda | Por qué |
|---|---|
| Simulación celular entera | Es el cuerpo de la criatura, literalmente (ver §4) |
| Mapa 768x288 | Intacto. Solo se rellena de bedrock lo que aún no toca |
| Cámara que sigue al jugador | Solo un ajuste de zoom, una línea (ver §8) |
| Movimiento del aprendiz | Sin tocar |
| Diario / libro | Sin tocar de estructura; gana una sección |
| Tolva de entrega | Sin tocar y sin mover, como pediste |
| Cincel + Mudanza | **Ascienden a protagonistas** |
| Química generada por semilla | Es lo que hace que la criatura digiera algo distinto cada run |
| Crecimiento dendrítico por semilla | Es la silueta de la criatura, distinta cada run |
| Firma visual por semilla | **Es la piel de la criatura**, gratis (ver §4) |
| Bautizo (`NamingUi`) | Es el momento de vínculo. Solo hay que apuntarlo a un ser |
| Máquinas, grifos, encargos | Existen, pero enterrados. Aparecen al cavar |

**No se borra ni un archivo.** Todo lo nuevo va detrás de un interruptor de modo.

## 2. Qué modifico

Cinco cosas, todas contenidas:

1. **Un plano alternativo** (`BuildCuartoIntimo`) hermano del actual, no un reemplazo.
   `BuildTestLevel` tiene UN solo llamante, así que es una rama.
2. **Sesión sin reloj.** Todo el temporizador vive en `DayCycle.cs` y **ningún otro archivo lee el
   tiempo** (verificado). Son cuatro puntos en un archivo.
3. **El bootstrap se bifurca**: en modo íntimo no instancia las 8 máquinas. Ya es una lista plana
   de llamadas.
4. **Un interruptor de HUD.** 10 de los 14 componentes con `OnGUI` ya comprueban
   `DayCycle.InputLocked` en su primera línea; añadir un hermano `HudSilenciado` es una línea por
   archivo, mecánica, sin tocar lógica.
5. **Dos clases nuevas**: `Criatura` y `Capullo`.

## 3. La nueva fantasía del primer encuentro

Oscuro. El mundo es roca. Hay **un solo punto de luz cálida** en la cámara, y respira.

No hay HUD, ni reloj, ni encargos, ni rótulos. No hay nada que leer. Hay una cosa viva en una
cuna de piedra, y está **desmayada de hambre**. Su latido es lento y su color está apagado — el
juego ya sabe desaturar el Vivium dormido, así que su tristeza es un estado real de la simulación,
no una animación de tristeza.

Al lado, algo más pequeño y cerrado. No se mueve. Todavía.

Lo único que tienes es el frasco. Y hay un montoncito de nutriente en el suelo.

Cuando le acercas algo de comer, **se estira hacia ti**. Ahí está el juego entero.

## 4. La criatura: **EL RESCOLDO** (nombre provisional — lo bautiza el jugador)

Un ser vegetal-animal: **un corazón bulboso** anclado en su cuna, del que salen **zarcillos** que
exploran, comen y se estiran. No tiene cara. Tiene un latido, y eso basta.

**Por qué esta y no otra.** Porque no es una máquina con piel de criatura — **su cuerpo es
simulación de verdad**, y eso ya está construido:

- **Su carne es Vivium**, el material `Organic` que ya crece célula a célula consumiendo nutriente.
- **Su silueta la decide la semilla.** El hábito de crecimiento ya se sortea: Enredadera (sigue la
  dirección de la que vino), Mata (isotrópica) o Dispersa. **El Rescoldo de cada partida crece con
  una forma distinta**, y eso no hay que inventarlo.
- **Su piel también.** `FirmaVisualFabrica` toma una máscara de silueta y devuelve esos píxeles
  pintados con la firma visual de la semilla — patrón interior y borde incluidos. Le paso la
  máscara del bulbo y **tengo un corazón con la textura de este universo, gratis**.
- **Su malestar ya está simulado**: fuera de su banda de temperatura se DUERME (bit real en la
  grilla, desaturado en pantalla); a 120 °C hierve en ceniza; a 150 °C arde. Puedes matarlo. Eso
  es lo que hace que cuidarlo signifique algo.
- **Su cuerpo es generable**: el generador de sprites ya construye siluetas orgánicas por perfil
  de semiancho por fila (así está hecha la redoma: base redondeada, panza, hombro, cuello). Un
  bulbo es exactamente eso.

**Cuatro estados, y ninguno es una barra:**

| Estado | Cómo lo SIENTES |
|---|---|
| Hambre | latido lento, color apagado, zarcillos caídos, halo pequeño y frío |
| Contento | latido regular, color pleno, zarcillos que se mecen, halo cálido y amplio |
| Frío / calor | el bit de dormido ya lo desatura; el latido casi se detiene |
| Miedo | fuego o ácido cerca: latido rápido, zarcillos que se retraen y se apartan |

**Y hace algo desde el primer minuto: DIGIERE.** Le das una sustancia; al rato **exuda otra**,
elegida por las leyes de ESTE universo (la afinidad de la semilla ya existe). No es un horno con
ojos: es el primer aparato de alquimia del juego y está vivo. La primera ley que descubres, la
descubres **a través de él** — y el diario la anota.

**Se estira hacia ti.** Cuando te acercas, sus zarcillos se orientan hacia el frasco. Es
puramente visual (no toco el crecimiento de la sim, que es determinista y frágil), y es lo más
barato que puedo hacer con más retorno afectivo de todo el plan.

**Y le pones nombre.** El bautizo ya es la mecánica insignia del juego; aquí deja de apuntar a una
sustancia y apunta a un ser. Ese es el momento del vínculo, y ya está medio construido.

## 5. La entidad en incubación: **EL CAPULLO**

Al lado de la cuna, sellado. Tiene la firma visual de la semilla **con una variación** — se lee
como pariente del Rescoldo, no como otra cosa.

- **Comunica que incuba** sin números: se hincha y se deshincha muy despacio, y va **agrietándose
  por fases** (4 o 5 estados de grieta). Las grietas son el progreso, y se ven de un vistazo.
- **El progreso depende del CALOR, no del reloj.** Si lo tienes tibio avanza; si lo dejas frío se
  detiene. Con eso, sin explicar nada, aprendes que **hay crianza** — y tienes una razón para
  volver a mirarlo.
- **Cambia entre partidas**: color, patrón y ritmo salen de la semilla. Y su hábito de crecimiento
  ya está sorteado, así que **lo que salga de ahí crecerá distinto en cada run**.
- En el diario aparece una entrada que no dice qué es. Solo cuenta cuántas veces lo has observado.

Propuesta de alcance: **que llegue a eclosionar**, pero solo si lo has mantenido tibio, y que lo
que salga sea un Rescoldo pequeño que usa la MISMA clase — cero sistemas nuevos, y el pago
emocional de que tu cuidado sirvió para algo. Si prefieres que se quede como promesa sin cumplir,
es quitar una rama.

## 6. Cómo estructuro la primera sesión

**Sin reloj, y sin modo sandbox tampoco.** Un sandbox dice "esto no cuenta". Lo que propongo es
una **sesión abierta con latido propio**: nada te mete prisa, pero la criatura sí tiene ritmo — le
entra hambre, el capullo avanza. El tiempo importa porque algo vivo lo habita, no porque un número
baje.

Cinco compases, todos enseñados por la criatura y ninguno por un tutorial:

1. **Se despierta.** Le das de comer. Se estira, come, crece un zarcillo.
2. **Tiene frío.** Se apaga. Hay una sola fuente de calor. Aprendes la temperatura.
3. **Digiere.** Le das otra cosa; exuda algo que no habías visto. → diario → **lo bautizas**.
4. **El capullo responde** al calor. Descubres que hay otra dimensión.
5. **Hay una pared.** Detrás se oye algo (la Tolva). Coges el cincel. **Y ahí empieza el juego que
   ya tenías.**

## 7. Los pedidos sin matar la experimentación

- **No hay jornada, así que no hay fecha límite.** Ningún encargo caduca ni se pierde.
- **No aparecen al principio.** El primer encargo llega **cuando cavas hasta la Tolva** — o sea,
  cuando tú decides. El Maestro no puede pedirte nada mientras no haya un agujero por donde
  hablarte.
- Son **peticiones, no cuotas**: una a la vez, sin contador de jornada, y el Favor solo abre
  grifos nuevos (más comida, más cosas que probar). El premio de cumplir es **más juguetes**, no
  puntuación.
- El HUD de encargos arranca plegado (la tecla **O** ya lo expande).

## 8. Cámara

Un acercamiento **moderado**, no un cambio de sistema: el zoom sale de una sola línea
(`SimRenderer.FitMainCamera`) y todo lo demás —zona muerta, acotado al mundo, rótulos, alcance del
frasco— está derivado y se adapta solo. Acercar incluso **abarata** el render. Propongo ~30% más
cerca, con Tab abriendo el plano ancho como ahora. Si no te convence, es revertir un número.

**Y un halo que sigue a la criatura**: un sprite de oscuridad con un agujero suave, en una capa por
encima de todo. Sin shaders (prohibidos en runtime en este proyecto), sin tocar el hot path. Es lo
que convierte "un cuarto oscuro" en "una cosa viva iluminando un cuarto oscuro".

## 9. Riesgos

1. **El riesgo real es que la criatura no se vea bonita.** Todo el pivot depende de que un bulbo
   generado por código lea como un ser y no como una mancha. Ninguna cantidad de diseño lo
   garantiza sobre el papel. **Mitigación: lo miro con mis ojos.** Construyo, abro tu Unity, tomo
   capturas y las juzgo yo mismo, e itero hasta que funcione — igual que hago con las auditorías de
   código, pero visual. Es lo que me pediste y es exactamente donde más falta hace.
2. **Que la intimidad se convierta en lentitud.** Un ser que tarda mucho en responder no es tierno,
   es aburrido. Mitigación: el primer estirón tiene que ocurrir en los primeros 15 segundos.
3. **Scope de la expresividad.** Un sistema de emociones puede crecer sin fin. Mitigación: cuatro
   estados y tres canales (latido, color, zarcillos). Cerrado.
4. **Romper el juego que ya funciona.** Mitigación: todo detrás de un interruptor de modo; el
   taller clásico sigue arrancable y sin tocar.
5. **Vivium es a la vez la criatura y un material de encargo.** Que te pidan entregar trozos de tu
   criatura sería horrible... o buenísimo, pero no en esta prueba. Mitigación: en modo íntimo
   ningún encargo pide vivium.
6. **Sin compilador en mi entorno.** Como siempre: auditorías por lectura y tú compilas.

## 10. Qué implemento primero, y cuánto cabe

**Primero, y es lo único que valida la dirección: LA CRIATURA EN SU CUARTO.**
Cuarto pequeño + cámara + luz + Rescoldo con sus cuatro estados + comer + estirarse hacia ti +
bautizarlo. Si eso no emociona, lo demás da igual; y si emociona, lo demás ya está construido.

Después: el Capullo. Después: la digestión. Después: el cincel como salida al taller enterrado.

**Cuánto se resuelve con cambios contenidos: casi todo.** El reparto honesto:

| Trabajo | Coste | Riesgo |
|---|---|---|
| Cuarto excavado en bedrock | bajo — plano hermano | bajo |
| Sesión sin reloj | bajo — 4 puntos en 1 archivo | bajo |
| Silenciar el HUD | bajo — 1 línea × 10 archivos | muy bajo |
| Bifurcar el bootstrap | bajo — lista plana | bajo |
| Cámara + halo | bajo | bajo |
| **Criatura (cuerpo, estados, latido, comer)** | **medio-alto** | **medio** |
| Zarcillos que se estiran hacia ti | medio | medio (es lo visual) |
| Bautizar a un ser (no a un material) | medio | bajo |
| Capullo con grietas | medio | bajo |
| Digestión | medio | bajo (las leyes ya existen) |

Lo caro es exactamente lo que debe serlo: **la criatura**. Todo lo demás es fontanería sobre cosas
que ya funcionan.

---

## Lo que NO voy a hacer

- No reescribo la simulación.
- No muevo la Tolva.
- No rediseño el diario.
- No borro el modo taller.
- No hago cría, genética ni árbol de criaturas. Una criatura, un capullo, y una promesa.

---

# ADENDA — HERRAMIENTAS VIVAS (la ronda siguiente, decidida y NO implementada)

*Escrito tras el playtest 21, que Cesar jugó y validó. La implementación se perdió con un
reinicio del sandbox antes de desplegarse; las DECISIONES son firmes y están aquí para que la
próxima sesión no tenga que volver a tomarlas.*

## Respuestas a lo que reportó jugando

- **"No puedo reacomodar el hijito"** — `Mudanza` solo conoce `HeatPlate`/`ChillStone`/`Dispenser`
  (contrato `IMovible`). Las criaturas no están en esa lista. Arreglo pequeño: que `Criatura` y
  `Capullo` implementen `IMovible` y se registren con `Mudanza.RegistrarMovible(this)`.
- **"Nació lo mismo que tenía vivo, ¿es probabilidad?"** — Ni probabilidad ni diseño: es un HUECO.
  Los dos seres son literalmente `MaterialId.Vivium`, así que color, patrón y hábito de
  crecimiento salen de la SEMILLA DE LA PARTIDA, no del individuo. `esCria:true` solo cambia el
  tamaño. Para que un hijo se parezca a su padre sin ser su clon, **los rasgos tienen que vivir en
  la criatura, no en el material**.
- **"Se ilumina cuando come, no sé si es fuente de luz, quizás pueda serlo"** — Hoy el halo es un
  sprite encima que no ilumina nada. Su intuición es buena: que sea luz de verdad.
- **"No encontré los caños / la máquina de calor o de hielo"** — Correcto, no existen: el bootstrap
  del pivot se salta todas las máquinas (están enterradas). No fue su F3.
- **"Imagino que esa es la función del ser vivo pero no lo puedo mover, tampoco veo la temperatura"**
  — Acertó del todo. La criatura ya se calienta a sí misma y a su entorno, pero es **invisible e
  inamovible**, así que para el jugador no existe como instrumento.

## LA TESIS, que la escribió él mismo

Su referencia de arte lleva un panel rotulado **"HERRAMIENTAS VIVAS"**. Eso es el juego:

> **Las máquinas no son máquinas: son criaturas.** No instalas una placa de calor y una piedra
> fría — crías seres con TEMPERAMENTO y los colocas donde los necesitas. Montar el laboratorio es
> ordenar tus instrumentos vivos. El cincel excava el espacio; las criaturas lo amueblan.

Con eso encaja todo lo ya construido: alimentar importa porque un ser bien alimentado trabaja
mejor; el capullo importa porque criar es cómo consigues el temperamento que te falta; digerir es
la alquimia hecha por un ser; y **el taller enterrado pasa a ser la herencia de quien construyó
máquinas en vez de criar seres** — lo encuentras, lo usas, pero ya no es tu forma de trabajar.

## Decisiones tomadas por Cesar (firmes)

1. **Temperamento SOLO TÉRMICO** en esta iteración: cada ser tira a calor, a frío o a templado, y
   con eso sustituyen a la placa ígnea y a la piedra gélida. Nada de oficios (digerir/iluminar/
   oler) todavía — eso es más de una iteración y arriesga que nada quede afinado.
2. **La cría HEREDA DEL PADRE CON DESVIACIÓN**, no una tirada nueva. Si crías uno caliente, su cría
   tiende a caliente, y en varias generaciones puedes afinar el temperamento que te falta. **Eso es
   lo que convierte incubar en una mecánica en vez de una espera.**

## Cómo implementarlo (notas para quien lo retome)

- **El temperamento es un valor CONTINUO** guardado en la instancia; las tres etiquetas
  (calor/frío/templado) son solo cómo se le presenta al jugador. Sin continuo no hay herencia con
  desviación de verdad.
- **LA TRAMPA, y es fácil caer**: si una criatura fría enfría su propia celda, se sale de su banda
  de crecimiento, se duerme y no crece nunca — se autodestruye. Hay que separar los dos radios que
  ya existen en `Criatura.ApplyCalorTick`: **el NÚCLEO (radio pequeño) mantiene SIEMPRE a la
  criatura dentro de su banda**, y **el ALCANCE AMPLIO empuja hacia el temperamento**. Ese anillo
  exterior es lo que la convierte en instrumento.
- Mantener el techo de seguridad que ya existe (Vivium hierve a 120°C, arde a 150°C) y añadir el
  simétrico por abajo: una criatura fría no puede matar a otra por congelación sin aviso (regla 38:
  si el jugador puede romper algo en silencio, dale el deshacer).
- **Se lee sin números**: la brasa y el halo tiñen según el temperamento (ámbar / azul pálido /
  neutro) y al acercarse sale un rótulo de mundo con `UiStyles.PlacaMundo`, igual que hacían las
  máquinas. Un instrumento que no puedes leer no es un instrumento.
- **La luz de verdad**: sin shaders (prohibidos en runtime), sin tocar el alfa por celda (regla 19).
  Capas de sprite; la criatura tiene que ILUMINAR la piedra de alrededor, no llevar un aura pegada.
- **Caños básicos de vuelta en la sala** (agua y nutriente), fijos a la pared. Razón de Cesar, que
  es la correcta: *"si por lo que sea lo pierdo ya no hay mucho más que hacer, y así evitamos dejar
  cositas en el suelo que se pueden perder"*. Un recurso perdible para siempre es una trampa en un
  juego que quiere que experimentes.
- Al mover una criatura hay que decidir y DOCUMENTAR qué pasa con su cuerpo de Vivium (son celdas
  reales de la grilla): ¿se queda?, ¿se poda y vuelve a crecer? Y `Reposicionar` nunca pasa por
  `BuildVisual` ni `Init` (regla 36).
- Determinismo: el sorteo de la cría no puede usar `UnityEngine.Random`.

## Advertencia de proceso para la próxima sesión

El sandbox se reinició **tres veces** en esta sesión y revirtió el repo cinco rondas sin avisar
(regla 6b). Una de esas veces un encargo trabajó una hora contra código de cinco rondas atrás sin
que ninguno de los dos lo supiéramos, y concluyó —con razón para lo que veía— que una API que sí
existe no existía. **Comprobar `git log --oneline -1` contra GitHub ANTES de encargar nada**, y
desplegar/commitear pronto en vez de acumular.
