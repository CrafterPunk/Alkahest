# JUEZ · ESTRATEGIA DE PRODUCCIÓN · RANKING DE LAS OCHO DIRECCIONES

*(Panel de direcciones, segunda pasada, 2026-09-12. Juez con prioridad de producción: apalancamiento
por unidad de complejidad, verificabilidad automatizable, iteración humana mínima, dependencias
secuenciales. Rúbrica v2 de `00 §3` con sus pesos y sus dos puertas. Leído entero: las ocho
direcciones de `panel/direcciones/`, las 24 críticas de `criticas/`, `00`, `02` y `03` (estos dos
últimos para no contradecir hechos ya recogidos, no para heredar su veredicto). No protejo El Pozo ni
el veredicto anterior; tampoco cambio por cambiar. Las notas son mías; donde me aparto de los críticos
lo digo y por qué.)*

## 0. La lente, dicha en tres frases

Lo que compro es trabajo **acotado, especificado y verificable en banco** que produzca contenido y
profundidad sin que una persona lo juegue. Lo que penalizo es todo número que alguien tiene que
teclear y volver a teclear tras cada sesión (umbral, horizonte, dote, constante de arquitectura,
gramática de un prior), toda sensación de control que ningún hash mide, y toda dependencia
secuencial que retrase la primera sesión que puede matar el core. Y la regla de Cesar por delante:
entre una ley difícil pero acotada y una capa fácil que exige meses de playtest, la ley.

Tres hechos de las críticas que ordenan todo lo demás, porque cruzan a las ocho direcciones:

1. **Cinco direcciones son el mismo build.** Días sin manos, Sin Manos, El Recibo, Sellado (y La
   Vigilia del arquitecto) comparten las primeras cinco semanas: el paquete J (diario bajo las puertas,
   volcado y carga, `CorrerSello`, condición como dato, balanza). Se diferencian en el encuadre (medida
   frente a modo, clon frente a racha, histograma frente a promesa) y en cuánto de la autoría mandan al
   banco. Juzgarlas como cuatro productos es gastar cuatro jueces en una idea.
2. **El sustrato no tiene relojes a escala de día.** Tolva 7,8 días y carbonera ~5, monótonos; la
   colmatación de la grava se para por truncamiento entero al 22-27 %; el agua sobre un sumidero nunca
   deposita; hogar, manantial y núcleo frío son pins eternos; R148 dice que después del día 5 «el
   resto del arco, sin actividad». El único reloj fuerte del código lo encontró el ingeniero del Pozo y
   ninguna dirección lo nombra: **drenar o ahogarse** (`Caudal` 24 contra 15 celdas/s por celda de
   sumidero; un tramo sin drenaje se inunda en ~14 días). Sin D («el hogar come carbón») y A («el aire
   se gasta»), «DÍA N SIN MANOS» es un cronómetro sobre un mundo que se paró el día 5.
3. **La iteración humana real es 6 en todas**, no el 7-8 declarado, y por la misma partida: el
   registro del autor que el validador exige (alguien resuelve cada situación y la re-resuelve tras
   cada cambio de física), la semántica de las cláusulas que parpadean (humedad 43-125 alrededor de
   60), la unidad del toque (un segundo de cincel son 90 entradas), la economía de verbos (si el
   pincel crea materia no hay juego) y la presentación (campos en F8, plantas de un píxel). Casi todo
   se sustituye por banco; lo incomprimible son tres o cuatro sesiones binarias y las láminas.

## 1. Ranking (rúbrica v2, 13 ejes, ponderado sobre 240)

| eje (peso) | Días sin manos | Sellado | El Recibo | TIRO | Sin Manos | El Pozo Sellado | Primera Piedra | A que sí |
|---|---|---|---|---|---|---|---|---|
| apalancamiento sistémico (3) | 7 | 6 | 6 | **8** | 6 | 6 | 6 | 5 |
| ejecutan · revelan · juzgan (3) | **8** | **8** | **8** | 6 | 7 | 7 | 6 | 6 |
| iteración humana, 10 = poca (3) | **7** | 6 | **7** | 6 | 6 | 5 | 5 | **4** |
| verificabilidad automatizable (2) | 9 | 9 | 9 | 8 | 9 | 8 | 7 | 8 |
| la simulación es el juego (2) | 7 | 6 | 6 | **9** | 6 | 6 | 6 | 4 |
| onboarding garantizable (2) | **8** | 7 | 6 | 6 | 6 | 7 | **8** | 6 |
| observabilidad (2) | 6 | **7** | 5 | 4 | 5 | 5 | **7** | **7** |
| tiempo como apuesta (1) | 7 | 5 | **9** | 7 | **9** | 8 | 5 | 8 |
| multiplayer emergente (1) | 5 | 5 | 5 | 5 | 6 | 6 | 5 | 6 |
| profundidad por leyes estables (2) | 6 | 6 | 5 | 6 | 5 | 5 | 4 | 5 |
| cuerpo del jugador (1) | 4 | 5 | 3 | 5 | 2 | 5 | **8** | 2 |
| identidad comercial (1) | 6 | 6 | 6 | 6 | 7 | 7 | 7 | 6 |
| dificultad técnica, 10 = fácil (1) | 7 | 8 | 8 | 6 | 8 | 6 | 7 | 7 |
| **ponderado (240)** | **167** | **159** | **156** | **155** | **151** | **148** | **147** | **134** |
| puertas | pasa | pasa | pasa | pasa | pasa | pasa (5 justo) | pasa (5 justo) | **no (iteración 4)** |

### Razón por dirección, en orden

1. **Días sin manos · 167.** Es la única dirección que trata el contenido como un lote de fábrica
   (envejecer → mutar → validar → firmar → ordenar) y cuyas correcciones de los críticos son todas de
   banco: validador de reloj, horizonte derivado del día de ruptura, semántica por estabilidad bajo
   jitter, unidad de toque por sensibilidad, firma con base sustraída, hermana de contraste generada,
   rendimiento de curación como métrica, el lote como puerta de fusión. La única regla de diseño que le
   falta cuesta cero líneas (cámara persistente: no hay FALLA, hay «todavía no»). Su degeneración es El
   Recibo y su coste de morir es J, que hace falta igual: la de menor arrepentimiento del panel.
   Prototipo feo en 7 semanas; evidencia de máquina y reloj en 1 semana de banco sin personas.
2. **Sellado · 159.** La fusión de la familia J ya hecha, con el mejor kit de observación (Piel
   siempre encendida, tres visores por bandas, diff, time-lapse) y la puerta técnica mejor escrita del
   panel (K1 trivialidad / K2 gradiente / K3 horizonte, una semana sin sello). Pierde por una decisión
   de diseño, no de física: el bifurcado gratis dentro del intento retira la consecuencia irreversible
   que el sello crea. Es F1 con el rebobinado puesto donde no va; su órgano correcto es «bifurcar crea
   una cámara hermana».
3. **El Recibo · 156.** La verificabilidad más alta y la puerta más barata (barrido + gemelos +
   búsqueda exhaustiva de un toque + IL2CPP, una semana). Sus dos métricas de estilo (toques y huella)
   gravan el juguete y hacen enumerable el espacio (92 montajes de un toque, ~4 000 de dos: el banco es
   el mejor jugador), el pedido lleva número y la economía de verbos no está escrita. Todo eso son
   correcciones de pocas líneas (tick del primer X, pedido como recibo de referencia, toques fuera de
   la nota), y sus órganos de banco (población sintética, gemelos ±k, búsqueda como autor) son los que
   más iteración humana quitan de la mesa.
4. **TIRO · 155.** El apalancamiento más alto por unidad de complejidad del catálogo: A son ~1
   semana, un byte que leen fuego, llama, gas, agua y planta, 13 decisiones seguras y la única
   corrección del negativo más viejo del laboratorio (la llama inmortal). Y la simulación más pura
   (cinco verbos existentes, la máquina es un gesto con hora, el reloj lo pone el fuego). Pierde
   porque el título cuelga de B, que dos refutadores predicen muerta y cuyo spike, tal como está
   escrito, no puede acertar (columna fría, `Σvy` confundida por el swap, banco sin boca, relajación
   que crea masa); porque REVELAR es una ausencia (la sordina emite menos de todo: 0,5-1,4 filas de
   humo); y porque la legibilidad del humo es la partida humana más cara y menos verificable en banco.
5. **Sin Manos · 151.** El movimiento conceptual más valioso de la pasada (SOLTAR como medida, no
   como modo) y el mejor guion de onboarding («El hilo»), sobre el mismo J. Pierde por lo que declara
   barato y no lo es: una mano nueva con cubo (2-4 semanas sin costear y tuning de control), la dote
   como economía en miniatura de doce números, los tres umbrales de ejemplo fuera del régimen medido
   (goteos 500/día contra 217-320 medidos; planta viva nunca; carbón 100 siempre), y «El hilo» es H4
   sin resolver (el arroyo lateral no moja el lecho: infiltración lateral 0 por visita). Omite D.
6. **El Pozo Sellado · 148.** Tiene la mejor idea de juicio del panel (la promesa con receptor: el
   umbral lo fija lo que alguien recibe, no una tabla) y el mejor SOLTAR local (abrir la garganta es
   abrir el grifo). Pero su motor no está en el código (el calor no cruza roca de ≥ 4 celdas, el humo
   cruza solo mientras un fuego abierto tiene combustible, la planta adulta es inmune a la oscuridad,
   la luz muere en la fila 255), cinco constantes de arquitectura sin derivar deciden si el juego
   ocurre en tiempo de juego (día, alto de tramo, ancho de garganta, grosor de suelo, gradiente), y
   girar la rejilla cuesta los nueve hashes del banco antes del formato del volcado. Su prueba de §8
   se mataría sola por montaje. Iteración 5: balance de un espacio acoplado disfrazado de física.
7. **Primera Piedra · 147.** La mejor idea sobre el avatar del panel (el cuerpo como sólido dentro
   del tick, tuning cero) al servicio de un verbo diseñado para abandonarse (su propio contador puntúa
   con cero el acto que le da nombre; verter la piedra cuesta un décimo de segundo). «A pie sobre
   polvo» reabre la clase de sesión más cara del proyecto (R110-R121b, seis rondas con Cesar dentro) y
   el tercer paso del loop no está especificado para las herramientas reales (`Flask.EsAspirable`
   niega todo sólido). Capa de onboarding de 3-7 situaciones, no producto.
8. **A que sí · 134, no pasa la puerta.** Lo que esconde no es contenido acotado sino un **prior de
   autor sobre el espacio infinito** (la gramática de los monos fija todas las cuotas; cada truco fiable
   que un jugador encuentre exige enseñárselo a los monos; cada verbo nuevo exige una gramática nueva):
   la forma exacta del playtest infinito que la función objetivo penaliza con más fuerza. Y a la
   resolución escrita (una celda, un tick) la física cotiza dados (carbón al 25 % por celda, lengua al
   40 %, humo al azar), no leyes. El verbo central no toca una celda. Descartar; sus órganos de banco
   valen más que la dirección.

## 2. Finalistas (los que compiten de verdad)

Dos, y no compiten por el mismo camino crítico:

### F1 · DÍAS SIN MANOS, como el build de la familia J con los órganos fundidos

No es «Días sin manos tal como está escrita»: es el build J con el encuadre de Días sin manos (la
fábrica de situaciones) y los órganos que sus hermanas hicieron mejor. Condiciones, todas anteriores a
la primera línea del sello y todas de banco o de regla:

1. **Cámara persistente** (del purista de Días sin manos): reiniciar no es gratis; volver es entrar en
   la misma cámara vivida; no hay FALLA, hay «todavía no»; el peldaño se sube por racha de H días. Sin
   esto «tocar o confiar» está dominado por reiniciar y la Etapa 2 no mide nada.
2. **SOLTAR como medida** (de Sin Manos): «último día en que todas las cláusulas se cumplieron tras
   el último toque»; nada bloquea las herramientas; **toques fuera de la nota** (de El Recibo): el
   diario completo sigue para rejugar y atribuir, pero el recibo pesa lo que sale y los días.
3. **D y A en el núcleo**, antes de escribir una situación de fuego: «el hogar come carbón» como
   material nuevo con escenario y kill propios (la ley tal como la escribió el crítico de huecos
   mataría ocho montajes y la fibra a 130 raw prende antes de que nadie la coma); A entrega A con las
   cuatro correcciones del ingeniero de TIRO (byte en `Empty` y en gas, doble búfer, desplazamiento en
   `Transform`, Σaire con cuatro términos o la planta que repone). Sin ellos el fuego no tiene reloj.
4. **Ningún número humano por situación**: horizonte = f × día de ruptura del registro de referencia
   (f por familia); cláusulas relativas al registro vacío e integrales por ventana; semántica elegida
   por estabilidad bajo jitter de ±100 ticks; unidad de toque = gesto (pulsar → soltar) elegida por
   sensibilidad; **`LabParams` es global y no se toca por situación** (de El Recibo: si la frontera no
   aparece, se cambia de situación o se añade una ley, nunca un número).
5. **El registro del autor es un script**, no una partida: `Intervencion[]` tick-estampada en
   `Correr` (la caldera pasa a ser la primera entrada), «montaje − solución» como forma canónica de todo
   escenario, y la búsqueda exhaustiva de 1-2 toques con gemelos ±k como autor de referencia y como
   validador al revés de mutantes («el autor falla y hay otra solución»). Cesar juzga; no resuelve.
6. **Física primero, contenido último; física congelada por versión de escalera.** Mientras L, A, D y
   F se muevan, las únicas situaciones son las nueve del banco partidas en madre y registro, que se
   revalidan solas; las 12-20 madres se escriben cuando la física de la primera campaña esté cerrada.
   El lote (validar + firma) se re-corre en N procesos batch-mode como puerta de fusión.
7. **Regla de verbos por escrito y como guarda**: nada crea materia (solo quitar y mover); dones y
   sumidero no se pintan ni se tallan (`PaintLab` los rechaza); alcance corto sin vuelo en situaciones.
8. **Render mínimo antes del primer playtest** (pintar `luz`, turbidez como tinte, plantas de varias
   celdas: 1-2 semanas del carril paralelo) o aceptar por escrito que las tres sesiones miden F8.

**Prueba que la mata (banco, semana 1, cero personas salvo media jornada):** `Intervencion[]`
genérica; cuatro madres «montaje − solución» (alambique sin caldera, horno sin recinto, carbonera
abierta, hervidero sin barra); sonda por día de la condición a 30 días para vacío y autor; ocho
mutantes por madre con la solución del autor rejugada y recibo cuantizado; firma por parámetros a cero
con la base restada; rendimiento de curación (20 mutantes, Cesar marca los publicables). **Mata**:
menos de 3 firmas-lección distintas; o tres de cuatro madres binarias en el día 1; o > 80 % de
mutantes que repiten el recibo de su madre; o < 30 % que sobreviven al autor; o curación < 0,5; o
> 50 % de soluciones rotas por el día de Q16. En paralelo, **E1 relojes con suelo de ruido** (seis
aparatos × tres palancas × tres gemelos, 36 000-540 000 ticks): mata si todo reloj queda bajo un día o
sobre 500 o inter/intra < 1,5 en todos. Y la **sesión limpia de la semana 7** con bandas y película:
toques a mitad de corrida frente a reinicios, soltar antes del horizonte, explicar un FALLA con la
línea del día, «otra vez». Cero toques a mitad o nadie que vuelva a una cámara aprobada = examen.

**Tiempos:** evidencia de máquina y reloj, 1 semana; sesión ruidosa, 3; prototipo feo, 7 (camino
crítico Pintor común → diario con id de gesto y byte de autor → volcado con lista completa → `CorrerSello`
con aplicador fiel → condición → validador → 6-8 madres; carril paralelo balanza, bandas, D, A, Q16,
Piel, gesto, render mínimo). Iteración humana: 12-18 días-persona en tres meses con los sustitutos,
casi toda binaria y en las semanas 5-8.

### F2 · TIRO como «RESPIRA» (A + bolsa), condicionado a dos semanas de banco en paralelo

Compite de verdad por una razón de producción: **su camino crítico no es el de J** (A → prueba 0 →
spike corregido → decisión B) y A hay que construirla igual para F1 (el reloj del fuego). Por 2
semanas de un hilo de Opus y media tarde de una persona sabemos si hay un segundo producto (B viva:
geometría de agujeros sobre una cantidad conservada, contenido sin tabla, el loop más corto y menos
examen del panel) o la mejor entrega de sustrato de la pasada con título prestado. Condiciones:

1. **Prueba 0 esta semana, sin física nueva**: perfil de `temp` en la chimenea a 5/10/20/40 celdas y
   celdas de humo por tick en la sala de «La vela» con el código actual; y la caja sellada de r136 en el
   editor con F8 apagado: ¿lee Cesar por dónde va el humo y por qué murió la llama? Si no, el render
   (persistencia del humo fuera del stepper, sin hash) y la sordina que humea (invertir el signo de una
   línea, con el hash del horno y el 18/18 como puerta) van **antes** que el byte.
2. **El spike de B escrito para poder acertar**: doble búfer, caso de gas en `LabCampos`,
   desplazamiento en `Transform`, advección de calor par y conservativa, cielo por geometría en el banco;
   métrica = aire neto que entra por la boca baja, abierta frente a tapada, contra gemelos ±1/±2; mata
   si la diferencia < 2× el ruido en todo N. **Sin B'**: el segundo intento de bombear es donde empieza
   el playtest infinito.
3. **El canon se lee, no se elige**: barrido nocturno de `AireConsumo × AireMinRespira × AireDifusion
   × caudal` contra seis identidades; imprime si existe punto y el tamaño de sala que ahoga nueve fibras
   a media pila; reformulado en fracción de combustible, no en días.
4. **Sin hada del aire**: la relajación hacia 128 se cuenta como cuarto término o se sustituye por la
   planta que repone aire; **el cuerpo respira** (una línea) o la frase cambia.
5. **Si B muere, TIRO se retira** y deja A entera, la bolsa, `LabAdvectar`, «chimenea con boca N» como
   escenario permanente y el remate guardado.

## 3. Descartes (como producto; los órganos siguen abajo)

- **Sellado, El Recibo, Sin Manos**: no se descartan sus ideas, se descarta que sean tres productos.
  Son el mismo build que F1 y F1 los absorbe con sus mejores piezas. Mantenerlos como direcciones
  separadas es multiplicar jueces, no opciones.
- **El Pozo Sellado**: segunda ronda condicionada, no finalista. Su motor no está en el código, su
  camino crítico rompe el banco (rotación de rejilla antes del volcado) y sus cinco constantes son
  balance por decreto. Se corre su prueba honesta de media semana (dos tramos con la tolva bajo
  garganta alineada, columna hidráulica con uno y dos sumideros, tapón de grava) porque sus órganos
  importan a F1 pase lo que pase; si acopla, el pozo es el sandbox de F1 y sus tramos son situaciones
  encadenadas por promesa; si no, la promesa con receptor se usa entre amigos por fichero.
- **Primera Piedra**: descartar como dirección; ascender el órgano al sustrato con la prueba
  corregida (máscara por regla incluyendo `LabPresion` y los creadores de materia; boca 4, no boca 1,
  con barrido de desfase). Nunca «a pie sobre polvo» antes de que el prototipo de F1 exista.
- **A que sí**: descartar. Se corre igual su escalón 0 (17 piedras lejanas / 17 pajas cercanas, un
  día) y, si algo lo justifica, el arnés de dos semanas, porque la banca como cifra de gradación es el
  validador que F1 quiere. Ninguna gramática de monos se retoca a mano por situación, nunca.

## 4. Órganos a conservar

| de | órgano | para |
|---|---|---|
| Sin Manos | SOLTAR como medida («último día en que todas las cláusulas se cumplieron tras el último toque»), nunca como modo | F1: el core del contador |
| Sin Manos | byte de autor en `Intervencion` + atribución contrafáctica (rejugar sin las entradas de B) | F1: co-op por fichero sin roles |
| Sin Manos | `tickSellado` y «días vistos» (sellar a ciegas, la racha valiente, el banco como verbo de jugador) | F1: modo de sello |
| Sin Manos | la apuesta de una frase, degradada a nota de cuaderno hasta que un playtest diga que calificarla divierte | F1: cuaderno falsable |
| Sin Manos | cláusulas relativas al registro vacío e integrales por ventana; tabla de relojes con suelo de ruido (prueba A) | F1 condición como dato · banco E1 |
| Sin Manos | «El hilo» como guion de onboarding, reordenado agua → fuego → planta y montado sobre un régimen medido (decantar primero, filtrar después) | F1 primera hora |
| El Recibo | búsqueda exhaustiva de 1-2 toques con gemelos ±k: par, generador de contenido, cazador de exploits, validador al revés de mutantes | F1 validador y autor de referencia |
| El Recibo | población sintética (K registros sorteados) como vara sin amigos; percentiles como bandas de producto | F1 histograma en solitario |
| El Recibo | «tick del primer X» como columna de tiempo sin umbral; pedido como recibo de referencia | F1 condición sin número humano |
| El Recibo | protocolo de gemelos ±k (inter/intra) como medida estándar de ruido del banco | banco, toda dirección |
| El Recibo | regla «`LabParams` es global y no se toca por situación» y «física primero, contenido último» | producción de F1 |
| El Recibo | salida con bit `entregable` y tragar solo desde arriba, o el pozo como lugar (recibo legible sin panel) | F1 balanza |
| Sellado | lista completa de estado del volcado + hash de versión de física en el fichero | J (primer fichero de partida) |
| Sellado | prueba K1/K2/K3 (trivialidad, gradiente, horizonte) en una semana sin sello | puerta de J |
| Sellado | bifurcar como semántica de fichero: el otro bifurca tu registro y nace una cámara hermana; la viva no se reescribe | F1 relevo |
| Sellado | Piel siempre encendida + tres visores por bandas con imagen desaturada + diff solo sobre sólidos + time-lapse por volcados diarios | F1 observabilidad |
| Sellado | sensibilidad de cada reloj a ±25 % de sus parámetros (separa ley de mando antes de afinar nada) | banco E1 |
| El Pozo Sellado | promesa con receptor + «el número por defecto es el de hoy»: el umbral lo fija lo que alguien va a recibir | F1 sandbox y co-op por fichero |
| El Pozo Sellado | línea de aforo a nivel de unidad (nueve ganchos + Σ cruces == Δ inventario por región) | F1 balanza por región, situaciones encadenadas |
| El Pozo Sellado | cláusula de eternidad («el montaje sin tocar debe fallar antes del día D») + día de decisión por K cortes | validador de la cuna |
| El Pozo Sellado | abrir la garganta = abrir el grifo (compromiso irreversible y físico); el tramo nuevo como estación | F1 situaciones encadenadas |
| El Pozo Sellado | «drenar o ahogarse» (`Caudal` frente a celdas de sumidero, ~14 días por tramo): el reloj hidráulico más fuerte del código | F1 E1 y madres de agua |
| El Pozo Sellado | piedra rajada como veredicto diegético binario legible a distancia; registro del autor como script | F1 gesto · F1 campaña |
| Primera Piedra | cuerpo sólido como entrada tick-estampada (máscara `ocupa` por regla, posición antes de `Step()`, guion en `Correr`, `X0/Y0` en el hash) | J: cierra la contradicción (a); C |
| Primera Piedra | brasa viva en el frasco (`aux` en `PaintCell`) y vista Ojo (velo 255 − luz) | F1 y TIRO · L |
| Primera Piedra | barrido de puestos y matriz de sustituibilidad; BFS a pie; andador headless; «con cuerpo» geométrico | banco de cualquier dirección con avatar |
| Primera Piedra | «TAPA HUMANA» y «la primera máquina eres tú» como 3-7 primeras situaciones | F1 primera hora |
| A que sí | la banca como validador continuo (gradación, moteado, explotabilidad como cifras) | cuna de F1 |
| A que sí | visor sin manos (R0 por día como fantasmas grises) y visor de posibilidad | F1 tutorial sin texto · rayos X |
| A que sí | sobre cerrado + recibo compartible (rejilla llena/hueca); «el que mira, apuesta» | F1 asíncrono y co-op de creencias |
| A que sí | cuota justa (paga c − 1) y torneo de estrategias (R0, copión, rociador, tonto guionizado, mutantes, autor) | validador de cualquier regla de puntuación |
| A que sí | pedido invertido: el jugador escribe la cláusula y la física la cotiza desde el t0 | F1 sandbox |
| A que sí | máscara de familias vista por día (2 bytes fuera del hash) y cláusula por vecindario | J condición para fuego, humo, gota, carbón |
| TIRO (si B muere) | A entera bien hecha (byte en `Empty` y gas, doble búfer, desplazamiento, Σaire cuatro términos, `LabRespira`/`ProcessFire` por umbral) | F1: el reloj del fuego |
| TIRO | bolsa en `LabPresion` por altura efectiva; trampa de agua como compuerta con hora; `LabAdvectar` par y conservativo | F1 situaciones de agua y fuego |
| TIRO | sordina que humea (una línea, con el hash del horno como puerta); el cuerpo respira; la planta repone aire | sustrato |
| TIRO | «chimenea con boca N» con caudal neto por boca y gemelos; el canon leído por identidades en banco | banco · patrón para D y F |
| TIRO | render de persistencia del humo fuera del stepper + métrica de legibilidad como puerta previa a toda sesión | presentación |
| Días sin manos | firma por ablación (hoy por parámetros a cero, base sustraída), escalera greedy, hermana de contraste generada, solver de macroverbos, velocidad como estado, película producida por el banco, rendimiento de curación, lote como puerta de fusión, telemetría del diario | F1 (son suyos; se listan para que no se pierdan si el encuadre cambia) |

## 5. Respuesta a la pregunta final

**¿Pueden las propias leyes ejecutar, revelar y juzgar las decisiones del jugador de modo que la
profundidad crezca mucho más rápido que el coste de diseñarla y testearla?**

Sí en dos de los tres verbos hoy, y en el tercero solo si se paga antes del primer playtest.

- **EJECUTAR: ya.** Agua 5/5, fuego 4/5, determinismo con 63 hashes, 21 rondas en tres días. Es el
  activo y no está en discusión.
- **JUZGAR: sí, con J, y con una condición dura.** El sello con condición como dato y la balanza
  convierten cuarenta contadores en juicio sin una línea de física, en 3-4 semanas secuenciales
  verificables en banco. Pero juzgar solo vale si **ningún número lo teclea un humano** (cláusulas
  relativas, tick del primer X, promesa con receptor, horizonte derivado del día de ruptura) y si **el
  sustrato tiene procesos mortales a escala de día**. Hoy no los tiene: D («el hogar come carbón») y A
  («el aire se gasta») no son expansiones, son núcleo, y el reloj hidráulico «drenar o ahogarse» hay
  que ponerlo en el core loop. Sin eso, «DÍA N» es un cronómetro sobre un mundo parado.
- **REVELAR: todavía no, y es donde vive la iteración incomprimible.** El mundo está en F8, las
  plantas son un píxel, el humo son 0,5-1,4 filas, y el estado más importante de la ley más
  apalancada (el fuego que se ahoga) es una ausencia. Ningún hash lo arregla; lo arreglan materia
  (sordina que humea, `luz` pintada, turbidez como tinte, plantas de varias celdas) y tres o cuatro
  tardes de láminas con desconocidos.

**Con qué dirección:** F1, Días sin manos como el build J con los órganos fundidos (SOLTAR como medida,
cámara persistente, toques fuera de la nota, registro como script, búsqueda exhaustiva y población
sintética como autor y vara, promesa con receptor en el sandbox), con D y A en el núcleo y TIRO corriendo
su spike en paralelo por si hay un segundo producto.

**Bajo qué condición:** que E1 (relojes con suelo de ruido, seis aparatos a 300 días) y E2 (la máquina:
≥ 3 firmas-lección, 30-80 % de mutantes válidos no copiados, curación ≥ 0,5, treadmill < 50 % por
cambio de física) pasen en una o dos semanas de banco sin personas; que la física se congele por
versión de escalera y el contenido se escriba al final; y que la sesión limpia de la semana 7, con
bandas y película, muestre toques a mitad de corrida y «otra vez». Si eso ocurre, la profundidad crece
por ley (cada paquete entra en el recibo sin tocar el juez y re-corre el lote en una noche) y no por
cámara, y el coste humano queda en 12-18 días-persona por trimestre, casi todos binarios. Si E1 dice
«eterno» o E2 dice «copias», lo que muere es SOLTAR como apuesta y el juez como fábrica, y lo que queda
(J, D, A, el banco de relojes) sigue siendo la infraestructura de cualquier otra respuesta.

## 6. Razonamiento

He leído las ocho direcciones y sus veinticuatro críticas con una sola pregunta: ¿cuánto de cada una
puede construirse y validarse sin que una persona la juegue, y cuánto de lo que queda es una sesión
binaria (sí o no) frente a un bucle de afinado que se reabre tras cada sesión? Esa distinción es la
función objetivo de Cesar traducida a calendario, y ordena el panel mejor que cualquier suma.

Lo primero que salta es que **el panel convergió en un build**. Cinco direcciones comparten el paquete J
en sus primeras cinco semanas y se diferencian en el encuadre. Desde producción eso no es una debilidad:
es la señal más fuerte de la pasada, porque significa que la infraestructura de juicio es la misma para
cualquier respuesta y su coste de arrepentimiento es cero. Pero obliga a juzgar el encuadre por lo que
compra en automatización, no por su frase. Ahí Días sin manos gana con claridad: es la única que
convierte el contenido en un lote de fábrica (envejecer, mutar, validar, firmar, ordenar) y cuyas
correcciones, todas duras y todas ciertas (el validador es un pasa-bajos, la escalera son 7-11
peldaños y no 300 situaciones, el treadmill de versiones obliga a congelar la física, reiniciar gratis
domina a tocar), son correcciones de banco o de regla. Sin Manos aporta el órgano conceptual que la
familia necesitaba (SOLTAR como medida) y luego esconde lo más caro (una mano nueva con cubo, una dote
por situación, umbrales fuera del régimen medido). El Recibo tiene la puerta más barata y la mejor
verificabilidad, y sus dos métricas de estilo hacen del banco el mejor jugador. Sellado lo funde todo
y pone el rebobinado donde deshace la apuesta. Los cuatro son F1 con una pieza de más o de menos, y he
puntuado a cada uno por su pieza, no por el J que comparten.

Lo segundo es **el sustrato**. Los críticos leyeron el código y coincidieron en algo que ninguna
dirección había escrito: no hay relojes a escala de día. La colmatación se para por truncamiento
entero, el sumidero nunca deposita, los dones son pins eternos, y la única corrida de 30 días dice
que después del día 5 no pasa nada. Contar días sobre eso es teatro. Por eso D y A dejan de ser
expansiones y pasan al núcleo en mi veredicto, y por eso el reloj que sí existe (drenar o ahogarse,
descubierto por el ingeniero del Pozo mientras refutaba otra cosa) tiene que entrar en el core loop.
Es también por lo que TIRO me importa más que su puesto: A es la modificación del sustrato con más
apalancamiento por unidad de complejidad del catálogo, es acotada, tiene assert de conservación y
escenario permanente, y hace falta igual. Que TIRO sea producto o expansión lo decide un spike de dos
semanas que corre en paralelo a J sin tocarlo. Un juez no debería decidir lo que un banco decide en
quince días; mi trabajo es exigir que el spike esté escrito para poder acertar, y hoy no lo está.

Lo tercero es **El Pozo**. He intentado no protegerlo y no castigarlo. Tiene la mejor idea de juicio del
panel (la promesa con receptor quita el único número humano que todas las demás direcciones arrastran)
y el mejor compromiso irreversible (abrir la garganta). Pero desde producción tiene tres defectos que
no son de gusto: su motor de problemas está contradicho por el código en cinco lecturas
independientes, cinco constantes de arquitectura que nadie deriva deciden si el juego ocurre en tiempo
de juego (y se reelegirían tras cada sesión: el bucle de balance pequeño que la función objetivo
penaliza), y girar la rejilla cuesta los nueve hashes del banco antes del formato del volcado, una
decisión que ninguna otra dirección arrastra. Su prueba honesta cuesta media semana; se corre porque sus
órganos valen para F1 pase lo que pase. No es finalista porque su camino crítico es el de F1 más una
semana de rejilla y aforo, y su motor es una hipótesis que el código niega.

Primera Piedra y A que sí se descartan por razones opuestas y por eso conviene decirlas juntas. La
primera es un órgano excelente (el cuerpo dentro del tick, tuning cero) envuelto en un verbo que su
propio contador castiga y en un plataformas a pie que reabre la clase de iteración más cara del
proyecto. La segunda es la automatización de balance más ambiciosa del panel al servicio de un
marcador que mide lo que el jugador dijo, cotizado contra un prior de autor que se mantiene truco a
truco: la definición exacta de balancear un espacio infinito. De la primera se salva la máscara con
guion; de la segunda, la banca como cifra de gradación y el visor sin manos. En ambos casos el órgano
vale más que el producto, que es lo que el encargo pedía reconocer.

Queda la pregunta de Cesar, y la respuesta honesta tiene forma condicional. Ejecutar ya está. Juzgar
está a tres o cuatro semanas secuenciales, verificables, con la condición de que ningún umbral lo
teclee nadie y de que el mundo tenga algo que perder. Revelar no está, y es donde vive la única
iteración que no se comprime: láminas, tres sesiones binarias, y un render mínimo que hay que pagar
antes de que la primera sesión mida el core y no F8. Si E1 y E2 pasan y la sesión de la semana siete
muestra a alguien tocando a mitad de corrida, la profundidad crecerá por ley y no por cámara, que es lo
que la función objetivo pide. Si no pasan, lo construido hasta ahí es J, D, A y un banco de relojes,
y ninguna de esas piezas se pierde. Ese es el argumento de producción entero: elegir el build cuyo
fracaso deja en pie la infraestructura de la siguiente respuesta.

## 7. Cadena de producción recomendada (para `03 §4`)

**Semana 0-1 · banco, todo en paralelo, cero Cesar salvo media jornada.** `Intervencion[]` genérica en
`Correr` + cuatro madres «montaje − solución» + sonda por día (E2) ‖ evaluador desechable de cláusulas
sobre `ArcoMuestra` + relojes con suelo de ruido a 300 días (E1) ‖ prueba 0 de TIRO (perfil térmico,
humo por tick, caja sellada sin F8) ‖ prueba honesta del Pozo (dos tramos con tolva, columna
hidráulica, tapón de grava) ‖ escalón 0 de A que sí ‖ `LabBench` en IL2CPP contra los 63 hashes ‖ día
de Q16 con `luz[i+W]`. Umbrales de muerte escritos antes de correr.

**Semanas 1-4 · J secuencial (camino crítico) con carril paralelo.** Pintor común en `Sim/` → diario
bajo las puertas con id de gesto y byte de autor → volcado y carga con la lista completa de estado y
round-trip de r141 → `CorrerSello` con aplicador fiel → condición como dato con semántica por
estabilidad. En paralelo: balanza `entregable` con guarda en `PaintLab`, `LabBandas` única fuente,
anillo de hashes, D como material nuevo, A entrega A con las cuatro correcciones, spike de B (semanas
2-3, decide TIRO), Piel y cuerpo como entrada del tick, render mínimo, cielo por geometría.

**Semanas 4-6.** Validador (vacío falla / autor cumple / reloj / eternidad / fragilidad) → envejecer →
`Clonar` → mutación → búsqueda exhaustiva de 1-2 toques con gemelos → 6-8 madres como script; gesto
(SOLTAR, contador, línea del libro por día, película producida por el banco, diff sobre sólidos, cámara
persistente). Firma, escalera y solver quedan **fuera** del prototipo: son fábrica, no bucle.

**Semana 7 · la sesión que puede matar.** Tres personas, cinco madres (una con reloj visible), bandas y
película delante. Medidas binarias: toques a mitad de corrida frente a reinicios; soltar antes del
horizonte por decisión propia; explicar un FALLA con la línea del día; volver a una cámara aprobada.

**Después, en este orden y solo si vive:** firma por parámetros a cero con base sustraída + escalera;
solver y población sintética; L (quinta pasada, vidrio); F mínima (fusión con reserva) como primer
temporizador; V cuando la campaña llegue al peldaño de vida; las 12-20 madres al final, con la física
congelada por versión de escalera y el lote como puerta de fusión. Iteración humana total en tres
meses: 12-18 días-persona, casi todos binarios, concentrados en las semanas 5-8.
