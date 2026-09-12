# JUEZ · LENTE COMERCIAL · RÚBRICA v2 SOBRE LAS OCHO DIRECCIONES

*(Panel de direcciones, segunda pasada, 2026-09-12. Juez con prioridad comercial: la frase, el clip
de diez segundos, la cápsula y el nicho de `docs/COMERCIAL/03_MERCADO.md`, y el riesgo de reseña
(fake · no sé por qué · sin meta · injusto · corto · servidor muerto). Por encima de esa prioridad
manda la función objetivo de Cesar de `00_ENCARGO_Y_CRITERIO.md §1`: el juego como estrategia de
producción; lo técnicamente difícil pero acotado y verificable antes que lo sencillo con meses de
playtest; nada de temporadas, eventos, geometrías a balancear, bibliotecas ni comportamiento humano
específico; la simulación sigue siendo el juego; el sustrato se puede tocar si el añadido tiene
apalancamiento alto. Leído entero: las ocho direcciones, las veinticuatro críticas, `03_MERCADO.md`,
`00` §3 (rúbrica y puertas) y, para calibrar sin obedecer, `02_DIRECCIONES.md` §1-2 y `03_COMPARATIVA
_Y_TIEMPOS.md` §1-2. Cuatro hechos de código que sostienen mi ranking los verifiqué yo en
`Assets/Alkahest/Sim/`: no existe `DiaTicks`; `RendimientoCarbonPct = 25` va detrás de `ChancePercent`
(SimStepper.cs:922); `LabTragar` :1026-1033 solo traga `Liquid`; `LabHogar` :917-924 reescribe
`temp[i] = HogarRaw` en cada visita (pin eterno); `touchedTick` :352 es la guarda de reentrada, no una
edad. No repito la aritmética de los críticos: la cito cuando decide.)*

## 0. Veredicto en cinco líneas

1. **Dos finalistas, no tres.** F1 **«Días sin manos»** como espina del producto (fusión obligatoria
   con los órganos de «Sin Manos» y «Sellado», que son el mismo producto escrito tres veces). F2
   **«TIRO»**, condicionada a una puerta de dos semanas, porque es la única dirección que añade una
   **ley** (y por tanto un reloj mortal) en vez de un envoltorio, y la única cuyo clip cumple las
   cuatro condiciones de `03 §4` sin arte nuevo si el tiro existe.
2. «Sin Manos» y «Sellado» no compiten contra F1: se funden en ella. Sus órganos son obligatorios
   (SOLTAR como medida, racha, apuesta, byte de autor; Piel, visores por bandas, cámara persistente).
3. **«El Pozo Sellado»** no es finalista como espina: su motor (lo que sube del tramo nuevo, los
   relojes lentos) no está en el código y sus cinco constantes de arquitectura son el balance de un
   espacio acoplado que la función objetivo penaliza con fuerza. Vuelve como **contenedor** cuando
   exista una ley que haga que algo suba (A) y algo se coma lo prometido (D). Su capa de juicio
   (promesa con receptor, aforo, garganta = grifo, piedra rajada) es la mejor del panel y entra en F1.
4. **«El Recibo»** es F1 con peor juez (12-20 números de pedido) y el peor clip del panel (una tabla);
   sus órganos de banco (búsqueda exhaustiva, gemelos ±k, población sintética, «tick del primer X»,
   «física primero, contenido último») son de F1 desde la semana 1.
5. **Descartes como producto:** «Primera Piedra» (un tutorial de 3-7 situaciones con un cuerpo exento
   de las leyes: el órgano sube al sustrato) y **«A que sí»** (no pasa la puerta de iteración humana:
   la gramática de los monos es balancear un espacio infinito truco a truco; el marcador puntúa la
   descripción, no la máquina).

## 1. Cómo puntué

Pesos de `00 §3`: apalancamiento 3 · ejecutan/revelan/juzgan 3 · iteración humana (10 = poca) 3 ·
verificabilidad 2 · la simulación es el juego 2 · onboarding 2 · observabilidad 2 · tiempo como
apuesta 1 · multiplayer 1 · profundidad por leyes estables 2 · cuerpo 1 · identidad comercial 1 ·
técnica (10 = fácil) 1. Máximo 240. Puertas: iteración < 5 o apalancamiento < 5 no es finalista.

Regla que apliqué en toda la tabla: **puntúo lo que hay en el código y en los bancos, no lo que la
narración promete**, y acepto una corrección de los críticos solo cuando la respalda una línea o una
medida (los tres críticos coinciden en casi todas). Donde una dirección se arregla con una decisión
de diseño de coste cero (cámara persistente, toque desgravado, cláusulas relativas), lo digo en la
razón y en las condiciones, no en la nota: la nota es de la dirección tal como está escrita.

## 2. Ranking (rúbrica v2)

| eje (peso) | 1 · Días sin manos | 2 · TIRO | 3 · Sin Manos | 4 · Sellado | 5 · El Recibo | 6 · El Pozo Sellado | 7 · Primera Piedra | 8 · A que sí |
|---|---|---|---|---|---|---|---|---|
| apalancamiento (3) | 7 | 7 | 6 | 6 | 6 | 6 | 6 | 5 |
| ejecutan · revelan · juzgan (3) | 8 | 6 | 8 | 8 | 8 | 7 | 6 | 6 |
| iteración humana, 10 = poca (3) | 7 | 6 | 6 | 6 | 6 | 5 | 6 | **4** |
| verificabilidad (2) | 9 | 8 | 9 | 9 | 9 | 8 | 7 | 8 |
| la simulación es el juego (2) | 7 | 9 | 6 | 6 | 6 | 7 | 6 | 4 |
| onboarding garantizable (2) | 8 | 6 | 7 | 7 | 6 | 7 | 8 | 6 |
| observabilidad (2) | 7 | 5 | 6 | 7 | 5 | 6 | 7 | 7 |
| tiempo como apuesta (1) | 6 | 8 | 9 | 5 | 9 | 8 | 5 | 8 |
| multiplayer emergente (1) | 5 | 6 | 6 | 5 | 5 | 6 | 5 | 6 |
| profundidad por leyes estables (2) | 6 | 7 | 5 | 6 | 5 | 5 | 4 | 5 |
| cuerpo (1) | 4 | 5 | 2 | 5 | 3 | 5 | 7 | 2 |
| identidad comercial (1) | 7 | 7 | 8 | 6 | 6 | 7 | 7 | 6 |
| técnica, 10 = fácil (1) | 7 | 6 | 8 | 8 | 8 | 6 | 7 | 7 |
| **ponderado (máx. 240)** | **169** | **159** | **159** | **159** | **153** | **152** | **149** | **134** |
| puertas | pasa | pasa | pasa | pasa | pasa | pasa (iteración 5, en la puerta) | pasa | **no** |
| veredicto | **finalista (espina)** | **finalista condicionada** | se funde en F1 | se funde en F1 | órganos a F1 | segunda ronda como contenedor | descartar; el órgano sube al sustrato | descartar; órganos a F1 |

**El empate triple a 159 es real y hay que leerlo, no romperlo con decimales.** «Sin Manos» y
«Sellado» son la misma construcción que F1 durante cinco semanas (el paquete J) con otro encuadre;
el encuadre lo decide el playtest de la semana 8-9, no un juez, y por eso no son finalistas aparte.
TIRO sí compite: es otro producto (un taller de aire) y otra apuesta (una ley). Entre los tres, el
orden 2-3-4 lo da la regla de preferencia de Cesar: lo técnicamente difícil pero acotado (una ley con
gate de dos semanas) antes que un encuadre que solo el playtest distingue; y entre los dos encuadres,
el que conserva la consecuencia irreversible («Sin Manos»: tocar cuesta racha, no se rebobina) antes
que el que la retira («Sellado»: bifurcar gratis dentro del intento).

## 3. Lectura comercial por dirección (frase · clip · cápsula · nicho · palabra de reseña)

### 1 · «Días sin manos» (campaña de situaciones + sandbox) · 169 · FINALISTA

- **Frase.** La de tres oraciones no cabe en un tuit; la que sí: «Arregla la cámara, quita las manos
  y cuenta los días que aguanta sin ti». El cartel «DÍA N SIN MANOS» sigue siendo lo más vendible de
  las dos pasadas: se lee en un fotograma, nombra el fenómeno y es la firma de la reseña buena («mi
  cuenca aguantó 312 días»).
- **Clip.** Existe y es el **fracaso**, no el aprobado: pies oscuros por la Piel, un cincelazo, la mano
  sale del cuadro, «DÍA 1… DÍA 9» mientras la grava ennegrece, «DÍA 11» y la línea parda cruza el
  sumidero; remate «la grava se cansa». Cumple las cuatro condiciones de `03 §4` **si** las bandas
  están en pantalla y la grava tiene reloj a escala de días, que hoy no está medido (la crítica de
  ingeniería: colmatación y relleno de poza sin escala en ninguna tabla).
- **Cápsula.** Tierra, agua y una mano que se retira. Menos carismática que un brazo alquímico; sin
  clip co-op. Arquetipo «expediciones con condición fuerte» (`03 §3`): base 60-150 k, el más barato
  de alcanzar y el más apto para demo y stream; con el sandbox y el contador global roza el de
  «colonia sin avatar» (120-300 k, mejor suelo).
- **Palabra de reseña que espera.** «Examen» si reiniciar gratis domina a tocar (el purista lo
  demostró: la cámara entra idéntica cada vez, nadie toca a mitad, reinicia); «no sé por qué» si un
  FALLA llega sin bandas ni película; «corto» si la escalera son 7-11 peldaños y 30-80 situaciones
  reales, que es la cifra honesta con la base restada de la firma.
- **Por qué primera.** Es la única dirección cuyo objeto es la *estrategia de producción* que Cesar
  pide: madre de 15 líneas → envejecer → mutar → validar → firmar → ordenar, todo en banco con umbral
  escrito antes de correr. Es la de menor arrepentimiento (si la máquina falla, degenera en «El
  Recibo» y todo lo construido vale). Su iteración humana real es 22-32 días con los automatismos
  (declaraba 4-6), pero es contenido acotado y mandos de máquina, no balance de un espacio infinito.
- **Condiciones (todas de coste cero o de banco):** cámara persistente (reiniciar no borra el mundo;
  bifurcar crea una cámara hermana); validador de reloj (una madre entra solo si algo se degrada
  entre el día 1 y H); horizonte = f × día de ruptura del autor, f por familia; plantillas probadas
  contra el registro vacío en las nueve madres (la del 90 % la cumple el manantial que espera); toque
  = gesto (id pulsar→soltar) y regla «nada crea materia» como guarda en las puertas, antes del primer
  banco; «el hogar come **carbón**» como material nuevo con banco propio (la fibra prende a 130 raw
  antes de que nadie la coma); física congelada por versión de escalera y lote como puerta de fusión;
  registro del autor como script tick-estampado en código; render mínimo (bandas, Piel, luz pintada,
  turbidez como tinte) antes del primer playtest o aceptar por escrito que la sesión mide F8.
- **Tiempos (críticos, no autor):** evidencia que la mata 1 semana de banco (máquina y reloj) + 2
  hasta la sesión ruidosa; prototipo feo 7 semanas; 4-5 sesiones antes de saber si hay «otra vez».

### 2 · «TIRO / La segunda boca» (ley nueva) · 159 · FINALISTA CONDICIONADA

- **Frase.** «Cada fuego respira lo mismo que tú» es la mejor del panel y vende un común de tres
  (fuego, humo, yo). Hoy el documento entrega un común de dos: el cuerpo lee el aire y no lo gasta.
  La línea que lo arregla (restar en la celda del cuerpo lo que resta una brasa) es barata y
  obligatoria, o la frase cambia.
- **Clip.** «TIRA» es el mejor del panel en atribución: la mano en plano, tres eslabones físicos
  (agujero → humo → llama), remate de dos palabras. Cumple las cuatro condiciones **si B vive**; sin
  B es «llama muere → agujero → llama vive», dos eslabones, remate «RESPIRA»: sigue siendo clip, no es
  el título. Y la condición 1 hoy está rota por construcción: la sordina se define por lo que NO
  hace (sin lengua, humo ÷ 4), así que «se ahogó» y «se acabó» se ven igual; la carbonera que mejor
  funciona (boca 1) da 0 humo. La inversión de una línea (`sordina ? combustHumoPct × 3`) con el hash
  del horno como puerta es la corrección más barata y más importante de toda la pasada.
- **Cápsula.** Un corte de tierra con una boca al cielo y humo en columna: se lee en un fotograma.
  Sombra de *Oxygen Not Included*; lo que TIRO tiene y ONI no es conservación al bit, co-op en
  falling-sand y SOLTAR. Arquetipo sandbox sistémico (100-250 k base) si es producto; expansión de F1
  si no.
- **Palabra de reseña que espera.** «Fake» si la vista Corrientes pinta el hash de bloque como si
  fuera viento (bajo A sola el rumbo del humo sigue siendo `XorShift` por bloque de 8×8); «se apagó
  solo» como bug si el jugador no ve por qué (la palabra que `03 §4` marca).
- **Por qué segunda y por qué finalista.** Es la dirección donde la simulación es más juego y menos
  capa (cinco verbos existentes, una ley, la máquina es un gesto con hora), la de mayor apalancamiento
  del catálogo (22 decisiones sobre un byte; 13 seguras con A + bolsa) y la **única que responde con
  una ley al agujero que los tres críticos encontraron en todas las direcciones de J: los pins
  eternos** (hogar, manantial, frío) hacen que SOLTAR no arriesgue nada. Pierde en revelar (la
  asfixia es una ausencia), en técnica (B puede no bombear: la columna de aire no puede estar caliente,
  `LabFlujoTermico` decae ×0,59 por celda) y en iteración (14-22 días: el canon está sobredeterminado y
  el ojo sobre el humo no se comprime). Es exactamente el cuadrante que la función objetivo prefiere:
  difícil, acotado, con gate numérico y sin biblioteca.
- **Condiciones:** prueba 0 hoy sin código (la caja sellada de r136 con F8 apagado: ¿Cesar lee si el
  fuego respira?); sordina que humea medida en banco con el 18/18 del horno como puerta; A con caso de
  gas en `LabCampos`, doble búfer, desplazamiento en `Transform` y Σaire con cuatro términos; el spike
  de B medido por **caudal neto en la boca baja contra gemelos**, no por `Σvy` en la sección; cielo
  por geometría en el banco (hoy `Correr` apaga la boca); la relajación hacia 128 se cuenta o se
  sustituye por la planta que repone aire; **si B muere, se retira como «RESPIRA» (A + bolsa + sordina
  que humea) dentro de F1, sin intentar un B'**; el prototipo para juzgar el core sin J en la semana
  3, no en la 6.
- **Tiempos:** evidencia que la mata 2 semanas (0,5 día hoy; A-lite 4-5 días; spike 3 días; barrido
  del canon una noche); prototipo feo 3 semanas sin J, 6-7 con J; 14-22 días de personas.

### 3 · «Sin Manos» (SOLTAR como verbo central) · 159 · SE FUNDE EN F1

- **Frase.** La mejor del panel («la física cuenta los días que tu obra aguanta sin ti») con una cola
  de desarrollador que ningún jugador compra («archivo de 60 KB») y una promesa escalar («quién la
  hizo mejor») que la dominancia por columnas, a propósito, no da.
- **Clip.** «El hilo» es la escena de onboarding mejor escrita de las ocho y cumple tres de cuatro
  condiciones; la que falta (estado legible sin voz) es render, y la cadena narrada tiene un fallo de
  código (el arroyo que «roza» el lecho no lo moja: infiltración lateral 0 por visita; lo erosiona).
  Reordenar agua → fuego → planta y montar el lecho bajo el agua.
- **Cápsula.** No existe: un lecho gris con un píxel verde y una mano-cursor. Sin cara para el tráiler.
  Arquetipo colonia sin avatar (mejor suelo, 120-300 k), pero el producto narrado es una campaña de
  puzles con cronómetro y cuestionario.
- **Palabra de reseña.** «Examen»: «el tacto es gratis, tocar cuesta» es literalmente el anti-Noita; el
  incentivo racional durante la corrida es no tocar y el juguete del falling-sand queda bajo la única
  nota. Y «injusto»: tres de tres umbrales de ejemplo de su propia prueba están fuera del régimen
  medido (goteos ≥ 500/día contra 320 medidos; planta viva el día 30 contra cero vivas en veinte
  rondas; carbón ≥ 100 que pasa siempre).
- **Por qué no finalista aparte.** Comparte con F1 sello, balanza, validador y fichero; añade encima
  3-5 días de metadatos que no tocan la física. Lo que aporta es obligatorio y entra en F1: **SOLTAR
  como medida y no como modo** («último día en que todas las cláusulas se cumplieron tras el último
  toque»: nada bloquea las herramientas), la racha, la apuesta de una frase (degradada a nota de
  cuaderno hasta que un playtest diga que calificarla divierte), el byte de autor y la atribución
  contrafáctica por rejugado, sellar a ciegas con `tickSellado` y «días vistos». Lo que esconde: una
  mano nueva (azada, cubo, cuatro verbos) sin costear ni validar en banco (2-4 semanas de Opus más
  sensación), doce dotes como economía en miniatura, y una escalera que se re-resuelve tras F y V.

### 4 · «Sellado» (híbrido) · 159 · SE FUNDE EN F1

- **Frase.** La de Un Año Después («la puerta que se cierra, el año que corre», la mejor frase de la
  primera pasada) con el cartel de Sin Manos. El tuit: «Arregla la cámara, sella la puerta y cuenta
  los días que aguanta sin ti; si te la pasan, sigue donde el otro la dejó».
- **Clip.** El propio (arrastrar la franja al día 11 y tocar) es un clip de *Braid*, no de *Dwarf
  Fortress*: nombra «deshacer», no «culpa mía». La condición 3 (consecuencia irreversible y atribuible)
  se retira por diseño en el minuto 9 de la primera situación. El clip bueno es el mismo fracaso de F1.
- **Cápsula.** Time-lapse con franja de recibo y dos cifras: una tabla animada. Sin clip co-op ni
  cápsula cozy. Base 60-150 k; estructura de comparación de Opus Magnum (200-500 k, la mediana más
  baja).
- **Palabra de reseña.** «Sin consecuencia»: la frase sobre la puerta («todo lo que hagas aquí seguirá
  pasando cuando te vayas») la contradice la mecánica. Y el banco juega mejor que el jugador: 1-2
  toques sobre 96×64 se enumeran en una noche.
- **Por qué no finalista aparte.** Trae hecha la fusión que los puristas pidieron a las otras dos y el
  **mejor kit de observación del panel** (Piel siempre encendida, tres visores por bandas con la
  imagen desaturada, time-lapse por volcados diarios, vista de diferencia), y la frase más honesta:
  «no depende del huerto». Con cámara persistente (bifurcar crea una cámara hermana, un fichero
  nuevo), toque desgravado, D y un reloj medido es F1 casi celda por celda; tal como está, es un
  puzle con deshacer sobre problemas de autor. Sus dos relojes de la intro están narrados contra el
  código: el agua sobre un sumidero nunca deposita (`LabTragar` la vacía cada 8 ticks; el depósito
  pide 24 visitas quietas) y el labio es de roca suelta, que no es porosa.

### 5 · «El Recibo» (las leyes juzgan) · 153 · ÓRGANOS A F1

- **Frase.** «Máquinas de arena viva que se juzgan solas» es buena y nombrable; «compáralo con el de
  tus amigos» exige amigos con la misma versión de la física intercambiando ficheros a mano: para el
  jugador de la demo, la vara es la barra del autor, que es el examen que dice no ser.
- **Clip.** Una tabla. La situación insignia es una carbonera a ×10: un borrón negro que humea en una
  cámara que «se oscurece sola» solo en F8 (el fuego escribe `luz = 255` y nadie lo pinta). Incumple
  la condición 1 y se vuelve menos legible conforme corre.
- **Cápsula.** Una pila de un píxel que humea. Nicho Opus Magnum (200-500 k con 5-10 personas; el
  puzle tiene la mediana más baja de `03 §1`).
- **Palabra de reseña.** «Examen de tres toques»: preparar está gravado (cada toque es nota), la
  proporción construir/mirar es un minuto por tres, dos de las tres columnas de estilo premian no
  hacer nada. Y «ruido»: el carbón sale de un dado del 25 % por celda (σ ≈ 8,7 sobre 400 celdas):
  dos máquinas idénticas corridas una celda aparte difieren un 9-12 % en la columna que manda, y su
  prueba de muerte (< 15 %) está a 1-1,7 σ del suelo de ruido; la boca no regula la pila maciza
  (R136, plana).
- **Por qué quinta y por qué sus órganos son de F1 desde la semana 1.** Es la dirección con la mejor
  verificabilidad del panel y coste de morir cero (todo lo construido es J), pero es F1 con peor juez
  (12-20 números de pedido, el único balance de la dirección, con sustituto sin número: «tick del
  primer X» o pedido relativo al vacío) y peor clip. Deja los mejores instrumentos de banco de la
  pasada: la **búsqueda exhaustiva de 1-2 toques** (par, generador de referencia, cazador de exploits:
  92 montajes en 25 minutos), los **gemelos ±k** como suelo de ruido estándar, la **población
  sintética** como vara sin personas, y dos reglas de producción que separan esta familia del playtest
  infinito: **`LabParams` es global y no se toca por situación** (si la frontera no aparece, se cambia
  de situación o se añade una ley, nunca un número) y **física primero, contenido último**.

### 6 · «El Pozo Sellado» (mutación de El Pozo) · 152 · SEGUNDA RONDA COMO CONTENEDOR

- **Frase.** «Cava un pozo tramo a tramo. Cada tramo que sellas sigue corriendo sin ti y le entrega al
  de abajo lo que prometiste.» Buena y cierta hasta «entrega»: en el código nadie recibe la promesa
  (el carbón no tiene consumidor; el agua clara solo la bebe una planta que nunca vivió; la luz solo
  la leen germinar y crecer). El receptor es un asiento contable.
- **Clip.** El que es verdad hoy es el mejor momento de todas las direcciones: tres golpes en el
  suelo y la poza entera cae, turbia, al tramo negro de abajo; la piedra marca «300 turbias · 0
  claras»; remate «la garganta». Cumple las cuatro condiciones. El que vende (la piedra que se raja
  vista desde la lumbrera porque el tramo nuevo amenaza al sellado) no ocurre: el calor no cruza un
  suelo de roca de ≥ 4 celdas, el humo cruza solo mientras un fuego abierto tiene combustible (R148:
  15 celdas en el minuto 5, 0 después), la planta adulta es inmune a la oscuridad, la luz del cielo
  muere en la fila 255 (el tramo 4 nace a oscuras). Su propia prueba de §8 se mataría por montaje: la
  carbonera de boca 1 da cero humo fuera.
- **Cápsula.** La mejor latente del panel: una columna geológica con luz que baja y humo que sube, el
  arquetipo sandbox co-op (100-250 k base; el único donde el sustrato tiene un diferenciador que nadie
  capturó: falling-sand con co-op oficial). Hasta que L pinte la luz, un falling-sand con cronómetro.
- **Palabra de reseña.** «Fake» si los días 6, 30, 38 y 48 de la narración los pone `DiaTicks` (que no
  existe en el código) y no una ley: la colmatación se para por truncamiento entero al 22-27 % con
  agua de manantial o ciega en veinte segundos con agua concentrada; el sumidero no se ciega; el agua
  con manantial perpetuo y sumidero único es eterna desde el minuto dos. El reloj que sí vive a escala
  de días y no aparece en su core loop es la **inundación** (24 celdas/s del manantial contra 15 por
  celda de sumidero: un tramo sin drenaje se llena en ~14 días), y quién gana lo deciden dos números
  del constructor con un umbral duro.
- **Por qué sexta y no descarte.** Tiene la mejor idea de juicio del panel (la promesa cuya materia
  recibe el tramo de abajo: el umbral lo pone tu siguiente problema, no una tabla; «el número por
  defecto es el de hoy» quita el umbral de autor) y el mejor SOLTAR local (abrir la garganta = abrir
  el grifo). Pero su motor exige elegir a mano día, alto del tramo, ancho de garganta, grosor del
  suelo y gradiente de ambiente para que algo ocurra en tiempo de juego, y reelegirlos tras cada
  sesión: balance de un espacio acoplado disfrazado de física, lo que la función objetivo penaliza
  «con fuerza». Su salida honesta no es tuning: son A (algo sube), D (alguien consume lo prometido) y
  V (algo produce sin manos), y las manda a la segunda y tercera ola. Es decir: **el Pozo necesita la
  ley de TIRO antes de poder ser pozo.** Vuelve como contenedor de F1 cuando esas leyes existan; su
  capa de juicio entra en F1 ahora.

### 7 · «Primera Piedra» (cuerpo) · 149 · DESCARTAR COMO DIRECCIÓN; EL ÓRGANO SUBE AL SUSTRATO

- **Frase.** Tres oraciones, y una es falsa hoy («tapas el sol con la espalda»: `LabLuz` rodea
  obstáculos más estrechos que la boca; sombreas solo si tapas la boca misma, y entonces es la tapa
  de gas con otro nombre). La que el clip sostiene: «Tú eres la primera piedra; el mundo cuenta
  cuántos días aguanta la segunda». Vende una primera hora, no un juego.
- **Clip.** «TAPA HUMANA» y «LA PRESA ERAS TÚ» son los mejores clips del panel en la condición 1: un
  muñeco sentado sobre un agujero del que salía humo es legible para cualquiera sin F8 ni arte nuevo.
  Con una salvedad: «la semilla brota» no se ve con plantas de un píxel; el tercer eslabón tiene que
  ser el velo o la pila ennegreciendo en corte.
- **Cápsula.** Depende del muñeco de 7×11. El cuerpo que no sufre limita la cápsula.
- **Palabra de reseña.** «Fake»: el cuerpo es opaco a la materia y a la luz, transparente al calor y a
  la humedad, inmune al fuego («sentarte sobre una brasa la ahoga» y el precio es un halo rojo): el
  único objeto del mundo al que las leyes no alcanzan, en un juego que promete que las leyes mandan.
  Y «tutorial»: el verbo está diseñado para abandonarse (su propio contador puntúa con cero cualquier
  día con el cuerpo dentro); verter la piedra cuesta un décimo de segundo (30 celdas/tick) y desde la
  segunda situación domina a ser la pieza; a hora 5 «el cuerpo ya casi no se usa como pieza».
- **Por qué descarte y qué sube.** «A pie, 12 celdas, polvo en reposo es suelo» es un plataformas
  puesto encima para fabricar escasez de presencia, y reabre el historial más caro del proyecto
  (R110-R121b) por el suelo; el tercer paso del loop, sustituirse, no está especificado para las
  herramientas reales (`Flask.EsAspirable` niega arcilla, terracota y roca suelta: el jugador solo
  vierte polvos, y el polvo sobre una boca de techo cae y entierra la pila). El órgano es otra cosa y
  es el mejor apalancamiento por línea del catálogo: **el cuerpo como sólido dentro del tick**
  (máscara por regla, posición escrita antes de `Step()`, guion `(tick) → (x, y)` en el banco, `X0/Y0`
  en el hash), que cierra la contradicción (a) de `01 §6` para cualquier dirección con avatar y sello.
  La única dirección del cuerpo que merecería refutación aparte es la que el purista nombra: **el
  fusible humano** (la pieza que no puede quedarse porque paga por bloquear, con un umbral leído de
  `LabBandas` y medido en banco), que es literalmente lo que Cesar pide («el cuerpo reacciona
  físicamente al mundo, sin barras»).

### 8 · «A que sí» (pronóstico) · 134 · DESCARTAR; NO PASA LA PUERTA

- **Frase.** «La física te paga según lo difícil que era acertar»: acertar es el verbo de un test. En
  compañía, «a que sí / a que no» es el mejor remate verbal del panel; en solitario, una cifra.
- **Clip.** Un cuadro translúcido con «×14» que se llena. Legible sin voz y con cadena, pero el remate
  es un multiplicador, no una palabra que nombre un fenómeno.
- **Cápsula.** Cuadros translúcidos sobre una cueva: no es castores ni balsa. Puzle sistémico, el
  género más pequeño de `03` (30-80 k a 60-150 k).
- **Palabra de reseña.** «Injusto», y la tiene esperando: el techo ×17 no distingue el truco de la
  hazaña (la zanja de cuarenta cortes al hueco lejano paga ×17 con la ciencia de que el agua baja);
  a la resolución de una celda y un tick ocho de las trece familias cotizan dados, no leyes (el
  carbón al 25 % por celda, la lengua de fuego al 40 % por paso, el humo como paseo al azar, la gota
  del serpentín que nace helada); y el «nivel medio» que necesita para que haya apuesta lo fabrican
  el dado y el borde del charco.
- **Por qué descarte.** Lo que esconde no es contenido acotado: es un **prior de autor sobre el
  espacio infinito de construcciones** (la gramática de los monos) con mantenimiento truco a truco y
  verbo a verbo, la forma exacta del playtest infinito que la función objetivo penaliza con más
  fuerza. Dos máquinas idénticas puntúan distinto según lo pintado encima: el marcador mide la
  descripción, no la máquina; y el verbo central no toca una celda. Iteración humana 4: no pasa la
  puerta. Su prueba corregida (dos semanas, sin J) se corre de todos modos, porque su arnés es el
  validador por gradación que F1 necesita y la única definición de cuota sin prior (por leyes) es la
  firma por ablación de F1 aplicada por fantasma.

## 4. Finalistas

**F1 · «Días sin manos» (espina), fundida con «Sin Manos» y «Sellado».** Qué es: una situación
acotada que la física envejeció → preparar tocando (cada gesto, una entrada del diario) → soltar
(×10 como estado de la mano, contador de días como medida que el libro produce) → veredicto por
condición como dato y balanza → seguir en la misma cámara (persistente), bifurcar como cámara hermana
o pasar el fichero. La fábrica (envejecer, mutar, validar con reloj, firmar, ordenar) produce y
ordena el contenido sin personas. Producto: arquetipo expediciones/colonia, demo y stream, base
60-150 k subiendo hacia 120-300 k con el sandbox. Sin clip co-op hasta la ruta A.

- Hasta evidencia para matarla: 1 semana de banco (intervención genérica en `Correr`, cuatro madres
  como «montaje − solución», sonda por día a 30 días, ocho mutantes por madre, firma por parámetros a
  cero con la base restada) + 2 hasta la sesión ruidosa (selector de montaje, contador, línea del
  libro por día). **Mata:** menos de 3 firmas-lección; tres de cuatro madres binarias el día 1; > 80 %
  de mutantes con el recibo de su madre; < 30 % de mutantes válidos; rendimiento de curación < 0,5;
  cero toques a mitad de corrida en tres personas.
- Hasta prototipo feo que permita juzgar el core: 7 semanas (Pintor común → diario con id de gesto
  → volcado con round-trip → `CorrerSello` con condición y días → cámara persistente y sonda por día;
  bandas y película en paralelo; ocho madres de los montajes con reloj medido).
- Iteración humana: 22-32 días de personas en tres meses con los automatismos; 4-5 sesiones antes de
  saber si hay «otra vez»; después 3-5 días por lote de 20-30 situaciones y 1-2 por paquete de física.

**F2 · «TIRO», condicionada a la puerta de dos semanas.** Qué es: el aire como masa que se gasta (A)
y, si el banco lo mide, fluye (B); cada agujero es una válvula de cuatro flujos; el fuego es su propio
reloj; SOLTAR sin horizonte impuesto. Como producto, un taller bajo una boca al cielo (arquetipo
sandbox sistémico); como expansión, la ley que da a F1 un sistema mortal que el jugador alimenta y el
reloj que todas las direcciones de J narran y ninguna tiene.

- Hasta evidencia para matarla: 2 semanas. Prueba 0 hoy (la caja sellada de r136, F8 apagado: ¿se
  lee si el fuego respira?; perfil térmico de la chimenea y celdas de humo por tick); un día de banco
  con la sordina invertida (filas de humo bajo techo; hash del horno y 18/18 del vidrio como puerta);
  A-lite 4-5 días con caso de gas, doble búfer, desplazamiento en `Transform` y Σaire; spike de B en
  3 días medido por caudal neto en la boca baja contra gemelos (N ∈ {1, 3, 8}); barrido nocturno del
  canon sobre seis identidades (imprime si existe punto; no lo elige). **Mata B:** entrada(abierta) −
  entrada(tapada) < 2× la dispersión entre gemelos en todo N. **Mata el canon:** ningún punto cumple a
  la vez «el cuarto sellado ahoga antes de media pila», «el fogón abierto no entra en sordina» y «la
  chimenea de boca 3 sostiene».
- Hasta prototipo feo que permita juzgar el core: 3 semanas sin J (A + spike + humo a escala con
  persistencia en el render + brasa en el frasco + tinte; SOLTAR y ×10 existen); 6-7 con J.
- Iteración humana: 14-22 días en tres meses; lo incomprimible es la mirada sobre el humo a 7,5 px por
  celda (2-4 sesiones, separadas de la física por construcción) y qué identidad cede si el barrido no
  cierra.
- Si B muere: se retira **sin B'** y deja en F1 «RESPIRA»: A entera, la bolsa en `LabPresion`, la
  sordina que humea, el cuerpo que respira, la trampa de agua como compuerta con hora, «chimenea con
  boca N» como escenario permanente del banco, el método del canon por identidades como patrón para D
  y F, y la cláusula «murió por aire» (carbón frente a ceniza) en el sello.

## 5. Descartes (como producto)

| dirección | motivo de descarte | qué sobrevive |
|---|---|---|
| **A que sí** | no pasa la puerta (iteración 4): el prior de los monos es balance de un espacio infinito por proxy; el marcador puntúa la descripción; el verbo no toca la grilla; a resolución de celda y tick cotiza dados | banca como validador, visor sin manos, visor de posibilidad, torneo de estrategias, cuota justa, cuota por leyes, pedido invertido, «el que mira apuesta», sobre cerrado, máscara de familias vista por día, densidad del bucle |
| **Primera Piedra** | «campaña + una ley» por confesión; el verbo se agota en siete papeles y su juez lo certifica; cuerpo exento de las leyes («fake»); «a pie» es un plataformas encima; sustituirse no existe con las herramientas reales | cuerpo sólido como entrada tick-estampada (al sustrato), brasa viva, Ojo, «con cuerpo» geométrico, BFS a pie, andador headless, matriz de sustituibilidad, clip TAPA HUMANA como 3-7 primeras situaciones, el fusible humano como dirección a refutar aparte |
| **El Pozo Sellado** (como espina; vuelve como contenedor) | el acoplamiento vertical no existe en el código a escala de juego; los relojes lentos no viven entre el minuto 2 y la eternidad; cinco constantes de arquitectura + `Caudal`/sumidero son balance de un espacio acoplado; el receptor de la promesa es un libro; su prueba se mataría por montaje | promesa con receptor y umbral autocalibrado, línea de aforo con nueve ganchos y Σcruces == Δinventario, garganta = grifo, piedra rajada, cláusula de eternidad y día de decisión, bolsas envejecidas en una corrida, encadenamiento vertical como generador de campañas, «filtrar cuesta luz», el reloj «drenar o ahogarse» |
| **El Recibo** y **Sellado** y **Sin Manos** (como productos aparte) | son F1 con otro encuadre; no compiten, se funden | ver §6 |

## 6. Órganos a conservar (de qué dirección, qué órgano, para cuál)

| de | órgano | para |
|---|---|---|
| Sin Manos | SOLTAR como **medida** («último día en que todas las cláusulas se cumplieron tras el último toque»); nada bloquea las herramientas; la racha como puntuación de jugador | F1 (núcleo del gesto) |
| Sin Manos | la apuesta de una frase juzgada por el sello, degradada a nota de cuaderno hasta que un playtest diga que calificarla divierte | F1 |
| Sin Manos | byte de autor en `Intervencion` y atribución contrafáctica por rejugado sin las entradas de B; `tickSellado` y «días vistos» (sellar a ciegas, el banco como verbo) | F1 (co-op por fichero sin roles) |
| Sin Manos | «El hilo» como primera situación, reordenada agua → fuego → planta y con el lecho bajo el agua; halo de la mano (Piel sobre el cursor) | F1 (onboarding) |
| Sin Manos | cláusulas relativas al registro vacío e integrales por ventana; kill test de pins a 300 días; tabla de relojes con suelo de ruido (equivalentes ±k); fusible de hielo tabulado, no prometido | F1 (validador) y F2 |
| Sellado | cámara persistente; bifurcar como **semántica de fichero** (nace una cámara hermana; la viva no se reescribe); «DÍA N SIN MANOS» como propiedad de un mundo persistente | F1 (respuesta a «examen o juego») |
| Sellado | Piel siempre encendida; tres visores por bandas (CALOR/AGUA/LUZ) con la imagen desaturada; `LabBandas` como única fuente de umbrales; time-lapse por volcados diarios y vista de diferencia solo sobre sólidos | F1 (observabilidad) |
| Sellado | tabla de relojes a 100 días y sensibilidad ±25 % por parámetro (la medida que separa ley de mando); ganancia por bifurcar y escalador de colina k = 3 en banco; «no depende del huerto» | F1 (validador) |
| El Recibo | búsqueda exhaustiva de 1-2 toques con gemelos ±k (par, generador de referencia, cazador de exploits, suelo de ruido); población sintética como vara sin personas; validador de mutantes al revés («el autor falla y hay otra solución») | F1 (banco, desde la semana 1) |
| El Recibo | «tick del primer X» como columna de tiempo sin umbral; pedido relativo al vacío con f por familia; huella = celdas del diario, no diff de `mat[]` con `Caudal` 24 | F1 (juez sin balance) |
| El Recibo | reglas de producción: **`LabParams` es global y no se toca por situación**; **física primero, contenido último**; nada crea materia (guarda en `PaintLab`) | F1 y F2 |
| El Recibo | cuota determinista de carbón por posición (si el dado domina); el pozo como salida (el recibo como lugar legible sin F8); clon en memoria | F1 |
| El Pozo Sellado | la promesa con receptor y «el número por defecto es el de hoy» (umbral autocalibrado, escrito por el jugador) | F1 (sandbox y cámara libre) |
| El Pozo Sellado | línea de aforo a nivel de unidad (nueve ganchos: `Move`, mudanza de presión, infiltración, percolación, exudación, capilaridad, vapor, goteo, tragado) con Σcruces == Δinventario como assert | F1 (balanza de cualquier recinto) y contenedor futuro |
| El Pozo Sellado | abrir la garganta = abrir el grifo (SOLTAR local irreversible); la piedra rajada como veredicto diegético; el encadenamiento vertical como generador de campañas encadenadas; «filtrar cuesta luz»; el reloj «drenar o ahogarse» | F1 (campaña) y contenedor futuro |
| El Pozo Sellado | cláusula de eternidad (el montaje sin tocar debe fallar antes del día D) y día de decisión (K perturbaciones) en el validador; bolsas envejecidas en una corrida; registro del autor como script | F1 (validador) |
| Primera Piedra | cuerpo sólido como entrada tick-estampada: máscara `ocupa` por regla (no por punto de `Empty`), posición antes de `Step()`, guion `(tick) → (x, y)` en `Correr`, `X0/Y0` en el hash, `WakeChunk` al entrar y salir | sustrato (cierra la contradicción (a) para cualquier avatar con sello) |
| Primera Piedra | brasa viva en el frasco (`VidaBrasa` + `PaintCell` con `aux`, 0,5 sem); vista Ojo; «con cuerpo» geométrico (caja ∩ región de la cláusula, sin umbral) | F1 y F2 |
| Primera Piedra | BFS a pie, andador headless, barrido de puestos y matriz de sustituibilidad; los tres conmutadores como «física del toque» (con aviso: polvo en reposo no es un conmutador) | F1 (cuna) |
| Primera Piedra | el clip TAPA HUMANA como las 3-7 primeras situaciones; **el fusible humano** (la pieza que paga por bloquear con un umbral de `LabBandas`) como la única dirección del cuerpo a refutar aparte | F1 (onboarding) y una refutación propia |
| A que sí | la banca como validador continuo (gradación, moteado, explotabilidad como cifras, en lugar de «K veredictos distintos»); el torneo de estrategias (R0, copión, rociador, tonto guionizado, mutantes, autor) como validador de cualquier regla de puntuación | F1 (validador) |
| A que sí | visor sin manos (R0 por día como fantasmas grises: tutorial sin texto); visor de posibilidad («qué tiende a hacer la física aquí») | F1 (observabilidad y onboarding) |
| A que sí | pedido invertido (el jugador escribe la cláusula antes de la obra y la banca la cotiza desde t0; la obra consume días); «el que mira, apuesta» (creencias ocultas hasta el veredicto en host + espejo); sobre cerrado y recibo compartible | F1 (sandbox, co-op) |
| A que sí | máscara de familias vista por día (2 bytes fuera del hash) como semántica honesta de las cláusulas transitorias; cuota justa (paga c − 1); cuota por leyes (= firma por ablación por fantasma); densidad del bucle (diez situaciones por hora) como objetivo de ritmo | F1 (condición como dato; campaña) |
| TIRO (si B muere) | A entera con conservación (caso de gas, doble búfer, desplazamiento en ocho sitios, Σaire con cuatro términos); `LabRespira` y `ProcessFire` por umbral (la llama mortal, apagar cerrando, bancar brasas); bolsa en `LabPresion`; **sordina que humea**; el cuerpo respira; trampa de agua; vidrio transparente + quinta pasada + día de Q16; planta que repone aire; método del canon por identidades; «chimenea con boca N» como escenario permanente; render de persistencia del humo y métrica de legibilidad | F1 («RESPIRA» como primer paquete de ley) |

## 7. Respuesta a la pregunta final

*¿Pueden las propias leyes ejecutar, revelar y juzgar las decisiones del jugador, de modo que la
profundidad crezca mucho más rápido que el coste de diseñarla y testearla?*

**Sí, por partes, y solo con tres condiciones que hoy no se cumplen.** Separando los tres verbos con
lo que el código y los bancos dicen:

- **EJECUTAR: sí, hoy.** Agua 5/5 con conservación exacta, fuego 4/5, determinismo con 63 hashes en
  una máquina, 21 rondas de física en 3 días. El trabajo técnico acotado es barato y paralelizable, y
  está demostrado.
- **JUZGAR: sí, con el paquete J y sin balance, a condición de que el umbral no lo teclee nadie y de
  que exista algo que perder.** El sello + la condición como dato + la balanza convierten cuarenta
  contadores del libro en veredicto en 4-6 semanas de C# verificable en banco. Pero «DÍA N» solo
  cobra algo si hay procesos que cambien de estado a escala de días, y hoy el sustrato tiene dos
  cronómetros monótonos de combustible (tolva 7,8 días, carbonera ~5), tres pins eternos (hogar,
  manantial, núcleo frío) y un manantial que espera cuando está rodeado: toda situación sostenida por
  pins tiende a DÍA ∞ o DÍA 0. Los tres críticos de las cinco direcciones de J llegaron a la misma
  frase: **sin una ley que haga mortal el fuego doméstico (D «el hogar come carbón», 0,5 semanas) o el
  fuego a secas (A «el aire se gasta», 1 semana), SOLTAR no arriesga nada.** Y los umbrales deben
  derivarse (tick del primer X, ≥ f × el registro vacío, horizonte = f × día de ruptura, agregación por
  estabilidad bajo jitter) o cada situación es una tarde de tuning.
- **REVELAR: no, hoy.** Todo se ve con F8, las plantas miden un píxel, el fuego escribe `luz = 255` y
  nadie la pinta, y el estado más importante de la única ley nueva del panel (el fuego que se ahoga)
  se representa como *menos* de todo. Es el verbo que ningún banco verifica y donde `03 §3` dice que
  mueren los sistémicos (Clockwork Empires, Maia: simulación invisible). La sordina que humea (una
  línea con el hash del horno como puerta), bandas en pantalla, Piel, luz pintada y turbidez como
  tinte son el precio mínimo antes del primer playtest; sin eso, la sesión de «examen o juego» mide la
  interfaz y no el core.

**Con qué dirección y bajo qué condición.** Con F1 «Días sin manos» como espina, fundida con SOLTAR
como medida (Sin Manos) y la cámara persistente con el mejor kit de observación (Sellado), sobre el
paquete J; con F2 «TIRO» como la puerta de dos semanas que decide si la primera ley nueva es A + B (un
producto) o solo «RESPIRA» (el reloj que F1 necesita); y con las reglas de producción de El Recibo
(`LabParams` global, física primero y contenido último, registro del autor como script, nada crea
materia). Bajo esa forma la profundidad crece **por ley, no por cámara**: cada paquete (A, D, L, V, F)
entra en el recibo sin tocar el juez y la máquina fabrica su peldaño con 1-3 madres. El límite honesto
es que el multiplicador por madre es 2-4×, no 10× (7-11 peldaños con doce familias de leyes; 30-80
situaciones reales), porque la simulación de hoy **evalúa** problemas que escribe un autor y solo
produce tres atractores sin él (anegado, quemado, inerte). Es Zachtronics, no Dwarf Fortress; y es
exactamente por eso por lo que la respuesta a la pregunta de Cesar pasa por TIRO: una ley con
conservación y volumen es la única cosa del panel que hace que la simulación **plantee** problemas
(qué sala se ahoga primero) en vez de solo calificarlos.

## 8. Razonamiento

Juzgo con la lente comercial por delante y la función objetivo de Cesar por encima, y las dos dicen
lo mismo con palabras distintas. El mercado de `03` dice que la física de píxeles vale millones al
servicio de **un verbo** y decenas de miles sin él, que los sistémicos no mueren por falta de
simulación sino por simulación invisible, bucle que no cierra o multijugador no pedido, y que lo que
separa 50 k de 1 M es la frase y la cápsula. La función objetivo dice que preferimos lo difícil pero
acotado y verificable a lo sencillo que exige meses de playtest, y que la simulación sigue siendo el
juego. Ocho direcciones escritas por agentes que no se leyeron entre sí convergen en una columna
vertebral (situación acotada → preparar → SOLTAR → recibo → comparar o seguir) sobre el mismo paquete
J; eso es la señal más fuerte de la pasada, y también su trampa, porque cinco de las ocho son el mismo
producto con otro encuadre y gastar cinco jueces en una idea es no juzgar nada. Por eso mi ranking
tiene un primer bloque de tres a 159 puntos que no compiten entre sí, una espina que los absorbe, y
una sola dirección que compite de verdad contra la espina porque es otra cosa.

**Por qué «Días sin manos» es la espina.** Es la única dirección cuyo objeto es la estrategia de
producción que Cesar pide y no solo un juego: madre de quince líneas, envejecer, mutar, validar,
firmar, ordenar, todo en banco y con umbral escrito antes de correr. Tiene el cartel más vendible de
las dos pasadas («DÍA N SIN MANOS»), un clip honesto que es el fracaso y no el aprobado, el arquetipo
más barato de alcanzar y el mejor para demo y stream, y la propiedad de producción que ninguna otra
tiene: si su máquina falla, degenera en «El Recibo» y todo lo construido vale. Sus tres críticos
encontraron lo mismo que yo con el código delante: la cifra de iteración humana es cinco veces la
declarada (22-32 días con automatismos), su única decisión propia («tocar o confiar») está dominada
por reiniciar gratis, sus relojes no están medidos, y su Etapa 1 no puede fallar porque los nueve
montajes llevan la solución dentro. Ninguna de esas cuatro cosas es balance de un espacio infinito;
las cuatro se arreglan con una regla (cámara persistente), un validador (de reloj) y una prueba de
banco reescrita, y eso es lo que separa un finalista de un descarte bajo esta función objetivo.

**Por qué «TIRO» es la segunda y no la tercera.** Fable, en `02`, puso al Pozo como F2 y a TIRO como
F3. Discrepo, y la razón es de código. Todas las direcciones de J comparten un agujero que sus
críticos nombraron con la misma frase: con hogar, manantial y núcleo frío como pins eternos, SOLTAR
no arriesga nada. El Pozo Sellado lo sufre más que nadie (su motor son «relojes lentos que las leyes ya
escriben», y leídos uno a uno ninguno vive entre el minuto dos y la eternidad; lo que sube del tramo
nuevo no sube: el calor no cruza cuatro celdas de roca, el humo dura lo que dura un fuego abierto, la
planta adulta es inmune a la oscuridad) y lo tapa con cinco constantes de arquitectura que alguien
elegiría a mano y reelegiría tras cada sesión: el balance de un espacio acoplado que la función
objetivo penaliza con fuerza. TIRO es la única dirección que responde a ese agujero con una ley en vez
de con un número: el aire como masa que se gasta hace mortal la llama inmortal, convierte cada agujero
en una válvula de cuatro flujos y acota el espacio del jugador por conservación en vez de por tabla.
Es exactamente el cuadrante que Cesar prefiere: difícil, acotado, con gate numérico en dos semanas y
sin biblioteca. Comercialmente tiene la mejor frase (« cada fuego respira lo mismo que tú»), el mejor
clip en atribución («TIRA») y una cápsula que se lee en un fotograma. Sus riesgos son reales y los
nombro como condiciones: revelar es hoy una ausencia (la sordina se define por lo que no hace), el
tiro puede no existir (dos refutadores lo predicen), la vista Corrientes sería «fake» sin B, y el ojo
sobre el humo a 7,5 píxeles por celda no se comprime con hashes. Por eso es finalista condicionada y
no espina; y por eso el Pozo va detrás: el Pozo necesita la ley de TIRO antes de poder ser pozo.

**Por qué Sin Manos y Sellado no son finalistas aparte.** Sin Manos tiene la mejor frase del panel y
el mejor movimiento sobre SOLTAR (medida, no modo), pero su cápsula no existe, su mano nueva es diseño
de control sin costear y su lema («el tacto es gratis, tocar cuesta») es el anti-Noita. Sellado trae
hecha la fusión y el mejor kit de observación, pero su bifurcado gratis retira la consecuencia
irreversible que hace clip a un clip: su escena propia es de *Braid*, no de *Dwarf Fortress*. Ambas
son F1 con una decisión distinta, y la decisión (racha sin rebobinado frente a cámara hermana) la toma
un playtest de cuarenta minutos en la semana 8-9. Sus órganos son obligatorios en F1 y así los listo.

**Por qué los descartes.** «A que sí» no pasa la puerta: lo que esconde es un prior de autor sobre el
espacio infinito (la gramática de los monos) que se mantiene truco a truco, el marcador mide la
descripción y no la máquina, y a resolución de celda y tick cotiza dados. La palabra de reseña que le
espera es «injusto». «Primera Piedra» es un tutorial magnífico de tres a siete situaciones con el
mejor clip del panel en legibilidad sin voz, montado sobre un cuerpo al que las leyes no alcanzan (la
palabra es «fake») y un plataformas puesto encima para fabricar presencia; el órgano (el cuerpo como
sólido dentro del tick) sube al sustrato y la única dirección del cuerpo que valdría refutar es el
fusible humano, que es literalmente lo que Cesar pidió. «El Recibo» es F1 con peor juez y el peor clip
(una tabla), pero deja las mejores herramientas de banco de la pasada y dos reglas de producción que
separan a toda esta familia del playtest infinito.

**Lo que ningún hash decide.** Tres cosas, y las tres son humanas e incomprimibles: si soltar y leer
un recibo es jugar o rendir cuentas (una sesión, semana 8-9, con la medida corregida: cuántos tocan a
mitad de corrida, cuántos continúan, cuántos rebobinan); si el mundo se lee sin F8 (láminas con
desconocidos, y antes el render mínimo que ninguna dirección presupuesta); y si el humo enseña el
aire (media tarde hoy, sin código). Todo lo demás es banco. Esa es la respuesta a Cesar: la profundidad
crece más rápido que su coste en todo lo que las leyes ejecutan y juzgan, y se compra al precio de
hacerla visible.
