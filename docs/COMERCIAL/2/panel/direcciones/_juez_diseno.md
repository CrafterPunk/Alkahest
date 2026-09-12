# JUEZ · DISEÑO · SEGUNDA PASADA · «¿ES UN JUEGO?»

*(Panel de direcciones, segunda pasada, 2026-09-12. Juez de diseño. Prioridad única: que sea un juego
(diversión minuto a minuto, tensión de SOLTAR, onboarding garantizado, observabilidad, profundidad por
leyes estables, multiplayer emergente), medida con la rúbrica v2 de `00 §3` y con la función objetivo
de Cesar por encima de todo. Leído entero antes de puntuar: las ocho direcciones, las veinticuatro
críticas, `01_LEYES.md`, `03_MERCADO.md`, `fable_direccion.md`, y la síntesis del arquitecto (`02`,
`03`, `04`) para saber dónde me separo de ella, no para obedecerla. Tres afirmaciones de código que
cargan más peso de diseño las comprobé en el fuente: la sordina emite `(combustHumoPct + 3) / 4` y
ninguna lengua (`SimStepper.cs:879-885`), la planta adulta solo muere por savia
(`SimStepper.Laboratorio.cs:891`) y el sumidero traga a sus cuatro vecinos en cada visita (`:1023`),
así que el agua sobre él nunca deposita. Las tres son verdad. Todo lo demás lo tomo de los críticos
donde coinciden con línea.)*

## 0. La regla con la que juzgo

Puntúo cada dirección **tal como está escrita**, no como podría quedar con las correcciones de sus
críticos; las correcciones que cuestan cero física y ya están identificadas las cuento en la
condición del finalista, no en la nota. Los ejes inversos (iteración humana, técnica) van con 10 =
poco/fácil. Pesos: apalancamiento 3 · ejecutan/revelan/juzgan 3 · iteración 3 · verificabilidad 2 ·
simulación es el juego 2 · onboarding 2 · observabilidad 2 · tiempo como apuesta 1 · multiplayer 1 ·
profundidad 2 · cuerpo 1 · identidad 1 · técnica 1. Máximo 240. Puertas: iteración < 5 o
apalancamiento < 5 no es finalista.

## 1. Ranking (rúbrica v2, mi nota)

| eje (peso) | Días sin manos | Sellado | TIRO | Sin Manos | El Recibo | El Pozo Sellado | Primera Piedra | A que sí |
|---|---|---|---|---|---|---|---|---|
| apalancamiento (3) | 6 | 6 | **7** | 6 | 6 | 6 | 6 | 5 |
| ejecutan · revelan · juzgan (3) | **8** | **8** | 6 | 7 | 7 | 7 | 6 | 6 |
| iteración humana, 10 = poca (3) | 6 | 6 | 6 | 6 | 6 | 5 | 6 | **4** |
| verificabilidad (2) | 9 | 9 | 8 | 9 | 9 | 8 | 8 | 8 |
| la simulación es el juego (2) | 7 | 6 | **9** | 6 | 6 | 6 | 5 | 3 |
| onboarding garantizable (2) | **8** | 7 | 6 | 7 | 7 | 7 | **8** | 6 |
| observabilidad (2) | 7 | 7 | 5 | 5 | 5 | 5 | 7 | **8** |
| tiempo como apuesta (1) | 6 | 5 | 8 | **9** | **9** | **9** | 4 | 8 |
| multiplayer emergente (1) | 5 | 6 | 6 | 6 | 6 | 6 | 5 | 6 |
| profundidad por leyes estables (2) | 6 | 6 | **7** | 5 | 5 | 5 | 4 | 5 |
| cuerpo (1) | 4 | 5 | 5 | 2 | 3 | 5 | **8** | 2 |
| identidad comercial (1) | 7 | 6 | 7 | **8** | 6 | 7 | 7 | 6 |
| técnica, 10 = fácil (1) | 7 | 8 | 6 | 8 | 8 | 7 | 7 | 7 |
| **total ponderado (máx. 240)** | **163** | **160** | **159** | **154** | **153** | **150** | **149** | **134** |
| puesto | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| puertas | pasa | pasa | pasa | pasa | pasa | pasa (iteración en 5) | pasa | **no** (iteración 4) |

**Cómo leer la tabla.** Los puestos 1, 2, 4 y 5 son **un solo producto** (la situación sellada que
las leyes juzgan, sobre el paquete J) escrito cuatro veces; sus puntos se separan por decisiones de
diseño, no por juegos distintos. La competición real es entre ese producto (puesto 1, con las
decisiones de los puestos 2, 4 y 5 que ganan) y **una ley** (TIRO, puesto 3). Los puestos 6 y 7 son
una cantera de órganos con un contenedor y una capa; el 8 no pasa la puerta.

## 2. Razón por dirección

1. **Días sin manos (163).** El marco más delgado y la mejor máquina de contenido del panel: madres
   de 15 líneas que el banco envejece, muta, valida, firma y ordena. Dos ideas de diseño que ninguna
   otra tiene y que son las que hacen juego: **la velocidad como estado** (×1 mientras la mano actúa,
   ×10 al retirarla: SOLTAR deja de ser un botón y pasa a ser una propiedad del mundo que se mira) y
   **el fracaso como película** con scrub y diff. Sus dos defectos son de una regla cada uno y sus
   críticos los nombraron: reiniciar gratis domina a tocar (corrección: la cámara persiste) y los
   relojes no están medidos (corrección: validador de reloj y D/A). Degenera con gracia: si su máquina
   falla es El Recibo con doce madres. Menor arrepentimiento del panel.
2. **Sellado (160).** La fusión ya hecha y el mejor kit de observación de la familia J (Piel siempre
   encendida, tres visores por bandas, time-lapse por volcados, diff). Pierde el primer puesto por una
   decisión de diseño que va contra la prioridad de este juez: **el bifurcado gratis dentro del
   intento deshace la apuesta que el sello crea** (tiempo como apuesta 5): la frase sobre la puerta
   («todo lo que hagas aquí seguirá pasando») la contradice la mecánica. Su intro está narrada contra
   el código (el sumidero no se ciega; el labio es roca suelta y no se colmata). Con la cámara
   persistente y bifurcar como semántica de fichero es F1 celda por celda.
3. **TIRO (159).** La dirección más pura en lo que me toca juzgar: cinco verbos que ya existen, una
   ley, el agujero como sorpresa y máquina a la vez, la primera máquina como **un gesto con hora**, y
   el único bucle del panel donde el reloj lo pone la física (el fuego se ahoga o se agota en 10-30 s):
   **el único que mitiga el examen por ley**. Pierde por el verbo del medio: REVELAR es una ausencia
   (la sordina emite un cuarto del humo y ninguna lengua; «se ahogó» y «se acabó» se ven igual), el
   humo medido son 0-9 celdas a 7,5 px, y el título cuelga de B, que dos refutadores predicen muerto.
   Sus problemas son del tipo que Cesar prefiere (banco, dos semanas); su cura de legibilidad es una
   línea (la sordina que humea más, no menos) que vale aunque la dirección muera.
4. **Sin Manos (154).** El mejor movimiento de diseño de la pasada: **SOLTAR como medida y no como
   modo** («último día en que todas las cláusulas se cumplieron tras el último toque»): nada bloquea
   las herramientas, tocar siempre cuesta. La mejor frase y el mejor remate («DÍA 6 · CAYÓ: PLANTA
   VIVA»). Pero como producto está escrito dos veces (es El Recibo con otro encuadre), sus tres
   umbrales de ejemplo están fuera de todo lo medido, omite D, gasta racha en cada toque (el
   anti-Noita), no tiene avatar sobre un sustrato que ya tiene muñeco, y su mano nueva es una capa de
   control sin costear. Es el verbo de F1, no un juego aparte.
5. **El Recibo (153).** La más honesta en negarse a poner un juego encima (vector, dominancia,
   histograma, clon por día, atribución por rejugado, diff) y, tal como está escrita, **la que más se
   parece a un examen**: el diario grava el juguete (toques como nota), un minuto de construir por tres
   de mirar un borrón, el sandbox en la hora 20, y un espacio que el banco enumera en una noche (el
   banco es el mejor jugador de este juego). La situación insignia (carbonera) es la física más plana
   del laboratorio y su frontera cabe dentro del dado del 25 %. Órganos excelentes; producto no.
6. **El Pozo Sellado (150).** Las mejores ideas de **juego** del panel: abrir la garganta es abrir el
   grifo (el compromiso físico irreversible más legible de la pasada, y el único clip verdadero hoy),
   la promesa que alguien recibe (el juez no es una tabla: es tu siguiente problema), y «¿sellamos el
   3?» (la mejor conversación co-op sin roles). Y un mundo que no hace lo que la narración cuenta: el
   calor no cruza un suelo de roca de cuatro celdas, el humo cruza solo mientras un fuego abierto tiene
   combustible, la planta adulta es inmune a la oscuridad, la luz muere en la fila 255, y los días 6,
   30 y 48 los pone `DiaTicks`, que no existe. El receptor es un libro (nadie consume carbón: D no
   está). Cinco constantes de arquitectura que alguien reelegiría tras cada sesión son balance
   disfrazado de física; iteración en la puerta. Su adversario real (la inundación: «drenar o
   ahogarse», 14 días por tramo) no aparece en su core loop. Contenedor condicionado, no producto.
7. **Primera Piedra (149).** El mejor clip del panel en «estado legible sin voz» (TAPA HUMANA, LA
   PRESA ERAS TÚ) y el mejor motor de onboarding (la primera máquina eres tú). No es un juego: su
   contador puntúa con cero el acto que le da nombre, verter la piedra domina a ser la piedra desde la
   segunda situación (el frasco vierte 30 celdas por tick), el cuerpo es el único objeto exento de las
   leyes («fake» en la reseña), y «a pie sobre polvo» es un plataformas encima que reabre las nueve
   rondas más caras del proyecto. Su órgano (el cuerpo sólido como entrada tick-estampada) sube al
   sustrato y cierra la contradicción (a) del sello; la única dirección del cuerpo que merecería
   refutación aparte es «el fusible humano» (el cuerpo paga por bloquear), que este documento descarta.
8. **A que sí (134, no pasa la puerta).** El verbo central no toca una celda; el marcador mide lo que
   el jugador dijo, no lo que su máquina hizo; el fantasma está por debajo de la resolución
   determinista útil (el carbón es un dado por celda); la gramática de los monos es un prior de autor
   sobre un espacio infinito que se parchea truco a truco: el playtest infinito con otro nombre. Deja
   los mejores instrumentos del panel (visor sin manos, visor de posibilidad, banca como cifra de
   gradación, sobre cerrado) y una idea que sí es de F1: el pedido invertido.

## 3. Finalistas (los que compiten de verdad)

Son dos, y no compiten por el mismo hueco: uno es el juego y el otro es su primera ley.

### F1 · «DÍAS SIN MANOS», transformada (el juego)

La situación sellada que las leyes juzgan, con las cuatro decisiones de diseño que ganan dentro de la
familia J, todas de cero física:

1. **Velocidad como estado** (de Días sin manos): ×1 mientras la mano actúa, ×10 al retirarla. SOLTAR
   no es un botón.
2. **SOLTAR como medida, no como modo** (de Sin Manos): tocar siempre permitido, siempre con precio;
   la racha es puntuación de jugador, no nota de autor.
3. **Cámara persistente, sin rebobinado dentro del intento** (exigida por los puristas a Días sin
   manos y a Sellado): volver a entrar es entrar en la misma cámara vivida; no hay FALLA, hay «todavía
   no»; bifurcar es una semántica de fichero (nace otra cámara), nunca la reescritura de la viva.
4. **Toques fuera de la nota** (exigido a El Recibo): el diario completo queda para rejugar y
   atribuir; el recibo pesa lo que sale y los días, no cuántas veces se tocó. El juguete del
   falling-sand no paga impuesto.

Más el recibo vectorial por dominancia y el histograma por fichero (El Recibo), Piel + tres visores por
bandas + diff (Sellado), la apuesta de una frase como nota de cuaderno hasta que un playtest diga que
calificarla divierte (Sin Manos), el visor sin manos (A que sí), y el cuerpo sólido con guion como las
tres a siete primeras situaciones (Primera Piedra). **Sustrato antes de la primera situación de
fuego: D («el hogar come carbón», como material nuevo con banco propio) y A («el aire se gasta»)**,
porque sin ellos el mundo es eterno después del día diez y el contador cuenta sin adversario.

Condiciones (todas de banco salvo la última): E1 relojes con suelo de ruido (seis aparatos, 300 días,
gemelos ±k); E2 discriminación con «montaje − solución» y frontera mayor que el dado del carbón; E3
semántica de cláusulas por estabilidad bajo jitter; E4 el día de Q16 con `luz[i+W]` y la tarde de
`Freeze`; **render mínimo antes del prototipo feo** (pintar `luz`, tinte de turbidez, plantas de varias
celdas, la sordina que humea); **física del toque fijada antes del primer barrido** (solo quitar y
mover, alcance corto, sin vuelo en situaciones, presupuesto de toques); y tres sesiones binarias con
la medida corregida (toques a mitad de corrida frente a reinicios; ¿alguien suelta antes del horizonte
por decisión propia?; ¿alguien vuelve a una cámara que ya aprobó?). Evidencia para matarla: 2 semanas.
Prototipo feo: 7 semanas de calendario. Iteración humana: 22-32 días de personas en tres meses tal
como está, 12-18 con los automatismos de sus críticos.

### F2 · «TIRO», condicionado a dos semanas (la ley)

Compite de verdad porque es la única dirección cuyo minuto a minuto es el de Noita (tocar, mirar,
morir, otra vez) y cuyo reloj lo pone la física, y porque sus problemas son técnicos, acotados y
verificables: exactamente lo que la función objetivo prefiere. Condiciones, en orden de coste: (0)
media tarde sin código: la caja sellada de r136 en el editor con F8 apagado, ¿dice Cesar por qué murió
la llama?; (1) un día de banco con **la sordina que humea** (`:885` invertida: `min(100,
combustHumoPct × 3)`), con el hash del horno y el 18/18 del vidrio como puerta; (2) A completa con
desplazamiento en `Transform`, doble búfer, caso de gas y fuente por geometría, sin relajación hacia
128 (la planta que repone aire, o nada); (3) el spike de B escrito para poder acertar (advección de
calor, caudal neto por la boca baja contra gemelos, no `Σvy`); (4) **el cuerpo respira** (una línea)
o la frase cambia. Si B bombea y el humo se lee: TIRO es la segunda ley de F1 y su título; si B muere:
se retira **sin B'**, y A + bolsa + sordina que humea entran como el primer peldaño de fuego de F1 con
el remate «RESPIRA». En ningún caso se sube un coeficiente hasta que separe.

**El Pozo Sellado no es finalista.** Se corre «dos tramos, honesto» (un día) y «columna hidráulica»
(medio día) porque cuestan eso, y si el humo de la tolva baja la luz del lecho de R150 en tiempo de
días, el pozo es el contenedor del sandbox de F1 y sus tramos son situaciones encadenadas por promesa.
Como producto que compita con F1 hoy, no: su motor no está en el código y su juez es un libro.

## 4. Descartes

| dirección | qué muere | qué queda |
|---|---|---|
| **El Recibo** | como producto aparte: mismo núcleo que F1 con métricas (toques, huella) que gravan el juguete y hacen enumerable el espacio; nicho de puzle (la mediana más baja) | absorbida en F1; ver órganos |
| **Sellado** | como producto aparte: es F1 con el bifurcado gratis, que deshace la apuesta | absorbida en F1 con la cámara persistente; ver órganos |
| **Sin Manos** | como producto aparte: es El Recibo con otro encuadre; la mano nueva sin costear; sin avatar | es el verbo de F1; ver órganos |
| **El Pozo Sellado** | como producto: acoplamiento vertical inexistente en el código, receptor-libro, cinco constantes de arquitectura sin derivar | contenedor condicionado a E6; sus órganos son los mejores de la pasada |
| **Primera Piedra** | como dirección: el verbo se abandona a sí mismo, el cuerpo exento de las leyes, «a pie» como plataformas encima | el órgano sube al sustrato; las 3-7 primeras situaciones de F1 |
| **A que sí** | como dirección: no pasa la puerta (iteración 4); el prior de monos es balance por proxy; el marcador mide la descripción | sus instrumentos y el pedido invertido pasan a F1; su prueba de banco se corre igual como validador |

## 5. Órganos a conservar de lo descartado

| de | órgano | para |
|---|---|---|
| El Pozo Sellado | La promesa con receptor, con «el número por defecto es el de hoy» (umbral autocalibrado, nunca tecleado) | el juez del sandbox y del co-op por fichero de F1 |
| El Pozo Sellado | Abrir la garganta = abrir el grifo (todo lo que decantó cae; irreversible, físico, legible) | el primer compromiso de toda situación encadenada de F1 y su clip verdadero hoy |
| El Pozo Sellado | La línea de aforo a nivel de unidad (nueve ganchos: `Move`, presión, infiltración, percolación, exudación, capilaridad, vapor, goteo, tragado) con Σ cruces == Δ inventario | la balanza de cualquier recinto con frontera, no solo del pozo |
| El Pozo Sellado | El reloj «drenar o ahogarse» (manantial 24 contra 15 por celda de sumidero: un tramo sin drenaje se inunda en ~14 días) | la primera situación de agua honesta de F1 («la que se inunda» tiene reloj real) |
| El Pozo Sellado | La piedra rajada como veredicto diegético binario, legible a distancia sin panel | el cartel de FALLA de F1 |
| El Pozo Sellado | Cláusula de eternidad y día de decisión en el validador (el montaje sin tocar debe fallar antes del día D; primer d en que K perturbaciones coinciden) | detectar «sandbox con cronómetro» y aburrimiento sin jugarlos |
| El Pozo Sellado | «Filtrar cuesta luz» (un tapón poroso en una garganta es filtro y persiana) | el primer cruce promesa-contra-promesa que el código ya cobra |
| El Pozo Sellado | Bolsas envejecidas en una sola corrida + «¿sellamos el 3?» como acto de grupo | la cuna y el co-op sin roles de F1 |
| El Recibo | El clon en memoria y bifurcar por día como semántica de fichero (nunca dentro del intento vivo) | el relevo y la liga por fichero |
| El Recibo | El histograma por dominancia y la población sintética (K registros sorteados) como vara sin amigos | comparar en solitario sin que la barra sea del autor |
| El Recibo | Búsqueda exhaustiva de 1-2 toques + gemelos ±k como suelo de ruido y como generador de referencia | fin de la cinta de correr de re-resolver tras cada paquete de física; validador de mutantes «al revés» |
| El Recibo | La salida con bit `entregable` y tragar solo desde arriba (o el pozo como lugar que se llena de negro) | la única física nueva de J; el recibo que se ve sin panel |
| El Recibo | «Tick del primer X» como columna de tiempo sin umbral; pedido relativo al vacío por familia | quitar el número de balance por situación |
| El Recibo | Dos reglas de producción: física primero y contenido último; `LabParams` global, jamás por situación | la única frase que separa F1 del playtest infinito |
| Sellado | La lista completa de estado del volcado y el hash de versión de física en el fichero | el primer fichero de partida del proyecto |
| Sellado | Piel siempre encendida + tres visores por bandas de ley, uno a la vez, con la imagen desaturada | aprender a ver en F1 |
| Sellado | Time-lapse por volcados diarios con vista de diferencia (solo sobre sólidos) y envejecer como intro sin texto | la película del intento y la geología que cuenta lo que pasó |
| Sellado | Registro del autor como guion de macroverbos; horizonte y `DiaTicks` derivados del día de ruptura; ganancia por bifurcar medida en banco | que la campaña se re-corra y no se re-juegue; que «puzzle con deshacer» se detecte sin ojos |
| Sin Manos | SOLTAR como medida («último día en que todas las cláusulas se cumplieron tras el último toque»), nunca como modo | el verbo de F1 |
| Sin Manos | La apuesta de una frase juzgada por el sello (como nota de cuaderno hasta que un playtest diga que calificarla divierte) | información asimétrica temporal y cuaderno falsable |
| Sin Manos | Byte de autor en `Intervencion` y atribución contrafáctica (rejugar sin las entradas de B); `tickSellado` y «días vistos» (sellar a ciegas) | co-op por fichero sin roles; la racha valiente |
| Sin Manos | Cláusulas relativas al registro vacío e integrales por ventana; tabla de relojes con suelo de ruido | jubilar el umbral humano por situación |
| Sin Manos | «Decanta primero, filtra después» (la vida del filtro como función de la decantación aguas arriba, R133) como primera situación de agua | el onboarding de agua honesto de F1 (no «El hilo», que es H4 sin resolver) |
| TIRO (si B muere) | A entera con desplazamiento en `Transform`, doble búfer, caso de gas, Σaire con cuatro términos, `LabRespira`/`ProcessFire` por umbral | la llama inmortal muere; apagar cerrando; bancar brasas; el primer reloj de fuego de F1 |
| TIRO | La sordina que humea (invertir `:885`) | el estado invisible pasa a ser el más visible sin tocar el render; vale aunque TIRO muera |
| TIRO | El cuerpo respira (restar en la celda del cuerpo lo que resta una brasa) | el cruce cuerpo × aire más barato del panel; consecuencia física sin barras |
| TIRO | La bolsa en `LabPresion`, la trampa de agua como compuerta con hora, la máquina como gesto con hora, «chimenea con boca N» como escenario permanente, la planta que repone aire, el canon leído por identidades | sustrato y método para D y F |
| Primera Piedra | El cuerpo sólido como entrada tick-estampada (máscara por regla, guion `(tick)→(x,y)` en `Correr`, `X0/Y0` en el hash) | cierra la contradicción (a) del sello para cualquier avatar; la piedra que decide |
| Primera Piedra | «Con cuerpo» geométrico (caja ∩ región de la cláusula, sin umbral); barrido de puestos y matriz de sustituibilidad; BFS a pie y andador headless | validar sin personas cualquier situación con avatar |
| Primera Piedra | La brasa viva en el frasco y la vista Ojo; TAPA HUMANA y LA PRESA ERAS TÚ | fuego con alcance real; las 3-7 primeras situaciones y el clip de F1 |
| A que sí | La banca como validador continuo (gradación, moteado, explotabilidad como cifras) y el torneo de estrategias | sustituir «K veredictos distintos» de la cuna por una medida |
| A que sí | El visor sin manos (R0 por día como fantasmas grises) y el visor de posibilidad | el tutorial sin texto y el mejor rayos X del panel |
| A que sí | El pedido invertido (el jugador escribe la cláusula antes de la obra y la física la cotiza; la obra consume días) y «el que mira, apuesta» | el juez sin autor de F1 y el verbo del espectador en la mesa |
| A que sí | El sobre cerrado, el recibo compartible, la máscara de familias vista por día, la densidad de bucle (diez situaciones por hora) | asíncrono a coste cero; cláusulas honestas para lo transitorio; la respuesta al minuto 15-40 |
| Días sin manos (si su máquina falla) | Velocidad como estado; firma por ablación con base sustraída y hermana de contraste generada; película con scrub y diff; el hogar come carbón como material nuevo; solver de macroverbos como fábrica | cualquier campaña de situaciones que sobreviva |

## 6. Respuesta a la pregunta final de Cesar

*¿Pueden las propias leyes ejecutar, revelar y juzgar las decisiones del jugador, de modo que la
profundidad crezca mucho más rápido que el coste de diseñarla y testearla?*

**Sí, con una dirección y bajo tres condiciones, y con un verbo de los tres todavía roto.**

**EJECUTAR** ya: es el laboratorio. **JUZGAR** con J (5-6 semanas, tuning 8-9): la balanza y la
condición como dato convierten cada contador del libro en veredicto sin un número de balance, y cada
ley nueva entra en el recibo sin tocar el juez; pero juzgan solo lo que una cláusula nombra, y hoy la
cláusula la escribe un autor. **REVELAR** es el verbo roto en las nueve direcciones: todo se ve con
F8, las plantas son un píxel, y el estado más importante de la primera ley nueva (el fuego que se
ahoga) es el que menos materia emite. La profundidad crece más rápido que el coste **solo si el
contenido es leyes × madres** y no situaciones × horas, y eso exige:

1. **Que el mundo tenga relojes** (E1 en una semana de banco). Hoy es eterno después del día diez
   salvo la inundación y la tolva. Si es eterno, D (el hogar come carbón) y A (el aire se gasta) van
   antes de la primera situación de fuego; sin ellos «DÍA N SIN MANOS» es una constante por aparato.
2. **Que el recibo tenga gradiente** (E2 en dos semanas): toda situación como «montaje − solución»,
   frontera más ancha que el dado del carbón y que los gemelos ±k, y `LabParams` global: si la
   frontera no aparece con la física que hay, se cambia de situación o se añade una ley, nunca un
   número.
3. **Que soltar sea jugar**, y eso ninguna ley lo decide: tres sesiones binarias sobre un prototipo
   feo en el que el juguete no pague impuesto (toques fuera de la nota, velocidad como estado, cámara
   persistente) y en el que el mundo se vea (render mínimo antes de la sesión, o la sesión mide F8).

**Con qué dirección:** F1 «Días sin manos» transformada como espina, con A y D como primer sustrato y
la sordina que humea como primer acto de «revelar»; TIRO como segunda ley y título si su spike separa
en dos semanas. El Pozo, como contenedor si «dos tramos, honesto» pasa. Si E1 y E2 pasan y la sesión
dice «juego», la respuesta es sí por construcción: el contenido lo producen envejecer, mutar y
validar; el balance no existe (hay materia, geometría y un vector); una ley nueva cuesta una o dos
semanas de banco y una a tres madres. Si la sesión dice «examen», ninguna dirección de esta pasada
sobrevive y los órganos vuelven al sandbox persistente.

## 7. Razonamiento

Juzgo con una prioridad: que sea un juego. Minuto a minuto, tensión de SOLTAR, onboarding
garantizado, observabilidad, profundidad por leyes estables y algo que hacer juntos. Y juzgo después
de que veinticuatro críticos leyeran el código, así que no puntúo lo que las direcciones prometen sino
lo que el sustrato hace hoy más lo que una prueba de banco de dos semanas puede confirmar.

Lo primero que hay que decir es que el panel no produjo ocho juegos: produjo un juego, una ley y dos
capas. Días sin manos, Sin Manos, El Recibo y Sellado son el mismo producto escrito cuatro veces sobre
el paquete J: situación acotada, preparar tocando poco, SOLTAR, las leyes corren a ×10, un recibo que
la simulación escribe, comparar. Sus diferencias son cuatro decisiones de diseño, y las cuatro tienen
ganador claro cuando se mira con la lupa del jugador. La velocidad como estado, de Días sin manos,
porque convierte SOLTAR en una propiedad física del mundo que se mira y no en un botón. SOLTAR como
medida y no como modo, de Sin Manos, porque tocar nunca se prohíbe y siempre cuesta, que es la única
tensión que no es un candado. La cámara persistente sin rebobinado dentro del intento, que los
puristas exigieron a Días sin manos y a Sellado, porque el bifurcado gratis retira la consecuencia
irreversible y atribuible que es la condición tercera del clip y el activo entero del género. Y los
toques fuera de la nota, que el purista pidió a El Recibo, porque gravar cada experimento entierra el
juguete del falling-sand bajo la única cifra que el jugador ve crecer. Con esas cuatro decisiones el
producto es uno, y lo llamo por el nombre de la dirección que más aporta a la máquina de contenido:
Días sin manos. Ese es el finalista.

Lo segundo es incómodo y los críticos lo demostraron con líneas: el mundo de hoy es eterno después
del día diez. Los tres dones son pins, la colmatación se detiene por truncamiento entero, el sumidero
nunca deposita porque traga en cada visita, la planta adulta no muere de oscuridad, la tolva es un
cronómetro de combustible que alguien puso. Un juego cuyo verbo es contar días sin manos necesita
relojes que las leyes escriban, y hoy tiene uno bueno (la inundación: manantial a 24 contra sumidero a
15 por celda, «drenar o ahogarse», que nadie puso en su core loop) y uno a medias (la tolva). Por eso,
para un juez de diseño, D («el hogar come carbón», como lo reescribió el ingeniero de Días sin manos)
y A («el aire se gasta») no son expansiones: son el primer sustrato de F1, antes de la primera
situación de fuego. Sin ellas, la sesión «¿juego o examen?» se jugaría sobre un contador sin
adversario y mediría un examen por construcción.

De ahí sale mi única discrepancia de peso con la síntesis del arquitecto: el orden entre TIRO y El
Pozo Sellado. TIRO es la dirección más pura del panel en lo que a mí me toca juzgar: cinco verbos que
ya existen, una ley, el agujero como sorpresa y máquina a la vez, la primera máquina como un gesto
con hora, y el único bucle del panel donde el reloj lo pone la física y no un horizonte impuesto. Es
el único que mitiga el examen por ley. Sus problemas son exactamente del tipo que la función objetivo
prefiere: un spike de B mal especificado que se corrige en tres días, una relajación hacia 128 que se
sustituye por la planta que repone aire, un humo que hoy no lleva información del aire. Su defecto de
diseño, que REVELAR es una ausencia (la sordina emite un cuarto del humo y ninguna lengua, lo
comprobé en la línea 885: «se ahogó» y «se acabó» se ven igual), tiene una corrección de una línea que
el purista encontró (la sordina que humea más, no menos) y que vale aunque TIRO muera, porque hace
visible el estado más importante de la ley sin tocar el render. El Pozo Sellado, en cambio, tiene las
mejores ideas de juego del panel (abrir la garganta es abrir el grifo; la promesa que alguien recibe;
«¿sellamos el 3?») sobre un mundo que no hace lo que la narración cuenta: el calor no cruza un suelo
de roca de cuatro celdas, el humo cruza solo mientras un fuego abierto tiene combustible, la luz muere
en la fila 255, y los días 6, 30 y 48 los pone `DiaTicks`, que no existe. Cinco constantes de
arquitectura que alguien reelegiría tras cada sesión son balance disfrazado de física, y el receptor
de la promesa es un libro, no una ley. Por eso TIRO es finalista condicionado a dos semanas de banco y
El Pozo no lo es: es el contenedor del sandbox si «dos tramos, honesto» pasa, y una cantera de
órganos si no.

Primera Piedra es la mejor primera hora que el sustrato puede tener y no es un juego: su propio
contador puntúa con cero el acto que le da nombre, verter la piedra domina a ser la piedra desde la
segunda situación, y el cuerpo es el único objeto exento de las leyes en un juego que promete que las
leyes mandan. Sube al sustrato como órgano (el cuerpo sólido como entrada tick-estampada, que además
cierra la contradicción del sello) y como las tres a siete primeras situaciones de F1, con TAPA HUMANA
como clip. A que sí no pasa la puerta y no debe pasarla: su verbo central no toca una celda, su
marcador mide lo que el jugador dijo y no lo que su máquina hizo, y su prior de monos es el playtest
infinito con otro nombre. Deja los mejores instrumentos del panel (el visor sin manos, la banca como
cifra de gradación, el sobre cerrado) y una idea que sí es de F1: el pedido invertido, donde el
jugador escribe la cláusula y la física la cotiza.

Tres condiciones de diseño que ningún documento del panel presupuesta y sin las cuales la primera
sesión medirá F8 y no el juego: pintar lo que ya se calcula (luz, turbidez, plantas de varias celdas,
la sordina que humea) antes del prototipo feo; fijar la física del toque (solo quitar y mover, alcance
corto, sin vuelo en situaciones, presupuesto de toques) antes del primer barrido; y escribir las
soluciones de autor como guiones en código para que la campaña sobreviva a cada ola de física. Con
eso, la respuesta a Cesar es sí: las leyes ejecutan hoy, juzgan con J sin un número de balance, y la
profundidad crece por ley y por madre. Lo que todavía no hacen es revelar, y ahí es donde hay que
gastar la primera semana.

## 8. Dónde me separo de `02` y `04`, en tres líneas

1. **TIRO por encima de El Pozo Sellado.** `02` hace de El Pozo el F2 y de TIRO una expansión; yo
   invierto el orden porque los problemas de TIRO son de banco y los del Pozo son de arquitectura sin
   derivar, y porque TIRO es el único que mitiga el examen por ley.
2. **La cámara persistente y los toques fuera de la nota no son opcionales**: son las dos decisiones
   que separan «juego» de «examen con clave» dentro de F1, y hay que escribirlas en la dirección antes
   de la primera sesión, no dejarlas a la prueba.
3. **El render mínimo y la física del toque están en el camino crítico**, no en el carril paralelo:
   sin ellos la sesión que decide todo mide la interfaz.
