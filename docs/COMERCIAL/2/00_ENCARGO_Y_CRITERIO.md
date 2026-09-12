# SEGUNDA PASADA COMERCIAL · 00 · ENCARGO, FUNCIÓN OBJETIVO Y CRITERIO

*(Fable 5.1, 2026-09-05. Capa nueva sobre `docs/COMERCIAL/00-04`, que NO se modifica: las dos pasadas
deben poder compararse. Nada de arquitectura ni handoff para Opus: tras esta ronda Cesar revisa a mano
antes de autorizar construcción. Este documento se actualiza conforme avanza la pasada (§5) para que un
corte por límites de uso no obligue a reconstruir nada.)*

## 1. Lo que cambió: la función objetivo

La primera pasada eligió un juego. La segunda debe elegir **un juego que además sea una estrategia de
producción**: unas pocas leyes que produzcan profundidad, contenido y situaciones que normalmente
exigen mucha autoría, balance y testing humano. La ventaja real del equipo es simulación profunda +
capacidad técnica muy alta + iteración humana cara. Todo se evalúa contra eso.

**Regla de preferencia.** Entre una solución técnicamente difícil pero acotada, especificable,
verificable y automatizable, y una técnicamente sencilla que exige meses de playtesting, tuning,
balance, contenido artesanal o contingencias, **preferimos la primera**. La dificultad técnica no
asusta; la complejidad iterativa humana sí. Se penaliza con fuerza toda propuesta que dependa de
temporadas, eventos, geometrías arbitrarias que haya que balancear, grandes bibliotecas de contenido o
comportamiento humano muy específico, aunque su versión ideal sea excelente.

**La simulación sigue siendo el juego.** No se coloca un juego tradicional grande encima del simulador
para darle propósito. Puede haber features después, pero el core loop suficiente existe sin acumularlas.
Antes de añadir capas externas: enriquecer y cruzar las leyes, materiales y relaciones existentes.

**El sustrato se puede modificar.** La física ya no está congelada para esta exploración. Se pueden
añadir leyes, campos, propiedades, agentes, verbos o relaciones **si tienen apalancamiento sistémico
alto**: poco añadido que transforme muchas decisiones existentes. Ejemplos del tipo de búsqueda (no
solicitudes): viento como gravedad lateral que afecta humo, vapor, brasas, secado, partículas,
semillas o transporte, observable por materia o un visor de corrientes; luz que se redirige o
concentra con materiales y geometría; hielo que flota, aísla o refleja. El criterio: **¿cuánto juego
nuevo compra esta modificación por unidad de complejidad?**

**El jugador participa de las leyes.** El cuerpo del jugador puede reaccionar físicamente al mundo
(calor, frío, humedad, humo, corrientes de agua o aire): consecuencias físicas y legibles capaces de
producir decisiones, accidentes, humor y clips; no barras de supervivencia.

**Observabilidad.** Se reabren con fuerza las vistas o instrumentos tipo rayos X para calor, humedad,
corrientes u otros campos. No se descartan por parecer herramientas de desarrollador: bien diseñadas
son parte central de aprender a ver. Mostrar demasiada información a la vez es un problema de diseño
de esa capa, no una razón para abandonarla. Simulación y percepción pueden tener resoluciones
distintas.

**Onboarding.** El éxito no puede depender de que un jugador nuevo descubra principios complejos
recorriendo un gran paisaje. Situaciones pequeñas de autor, fáciles de probar, donde una relación
causal principal se aprende y luego se combina con otras. Explorar en serio campañas o desafíos
breves que garanticen aprendizaje progresivo antes o junto al sandbox, sin asumir cómo se desbloquea
nada ni que esa estructura deba ganar. Conservar el principio **preparar → comprometerse → dejar
correr la simulación → observar consecuencias**; el intervalo no tiene que ser un año.

**Tiempo como apuesta.** Renunciar temporalmente a intervenir porque confías en lo que construiste
(SOLTAR): core, modo, desafío o recurso de tensión. Explorar preparar → SOLTAR → simulación →
resultado sin asumir que sea competitivo; en solitario, con 2-3 construyendo juntos, o comparando
resultados de grupos distintos mediante estados, tiempos o métricas que la simulación ya produce. No
hay que resolver multiplayer online ahora; se conserva la coautoría asíncrona por fichero.

**Multiplayer.** Sin roles permanentes por geografía, altura o propiedad de recursos. Si existe, emerge
de algo divertido que hacer juntos, no de separar a tres personas para obligarlas a cooperar.
Información asimétrica temporal puede valer (alguien ve una capa causal que otro no ve) sin volverse
rol fijo.

**Profundidad y longevidad.** Nacen de leyes estables que siguen combinándose. Climas, estaciones o
eventos no están prohibidos, pero la longevidad no puede depender de ellos si exigen reaccionar bien
ante un espacio casi infinito de construcciones del jugador. No se compra profundidad a cambio de una
pesadilla de balance.

**Reevaluar todo.** Las seis familias, los conceptos originales y los órganos que sobrevivieron, sin
proteger El Pozo ni el veredicto anterior, y sin cambiar por cambiar. Los órganos pueden valer más que
los productos donde nacieron. Vale una mutación, un híbrido con núcleo claro, una idea descartada que
revive o una dirección nueva.

**Tiempo.** Recalibrar con el throughput real del equipo (Fable + Opus + agentes + banco headless +
hashes + simulación acelerada). Sin extrapolar linealmente, distinguir: trabajo técnico automatizable o
paralelizable; dependencias secuenciales; iteración humana incomprimible. Para cada finalista: cuánto
tardamos en obtener evidencia para matarlo, cuánto hasta un prototipo feo que permita juzgar el core, y
cuánta iteración humana exige. Sin calendario de lanzamiento.

**Mercado.** Se reutiliza `docs/COMERCIAL/03_MERCADO.md` y `panel/`; solo se investiga de nuevo si
aparece un arquetipo nuevo o una alerta que cambie la decisión.

**Entrega.** Solo direcciones que compitan de verdad, sin número fijo. Por finalista: la experiencia
narrada (qué hace el jugador, qué siente, cómo empieza una sesión, dónde aparece el momento
interesante) y después, breve: core loop; por qué explota mejor la simulación; qué añade al sustrato;
principal riesgo de diseño; cuánto depende de iteración humana; la prueba más barata capaz de matarlo.
Conservar los mejores órganos de lo descartado. Terminar tomando posición: mantener, transformar o
reemplazar el veredicto anterior.

**La pregunta final.** ¿Podemos hacer que las propias leyes **ejecuten, revelen y juzguen** las
decisiones del jugador, de modo que la profundidad del juego crezca mucho más rápido que nuestro coste
de diseñarla y testearla?

## 2. Cómo se lee la primera pasada desde aquí

Lo que la primera pasada dejó y que esta reutiliza como materia prima (no como decisión):

- Seis familias: P1 El Pozo (vertical, con cuerpo, estaciones), P2 La Ladera (terrazas, regantes,
  asíncrono por fichero), P3 Un Año Después (cámara sellada, año a ×10, puntuación medida en el
  sumidero), P4 Sin Manos (sin avatar, SOLTAR, «DÍA N SIN MANOS»), P5 Sordina (percepción, sondas,
  vigilia, «todo avisa antes de cambiar»), P6 Claraboya (censo de vida, bichos-sensor, semillas raras).
- Órganos que ya parecían valer más que sus productos: SOLTAR y el contador de días; la cámara sellada
  con veredicto diferido; sondas de materia e instrumentos que las leyes ya producen; huellas de 8 bits;
  ruinas amables como tutorial (ley + rotura pequeña); cuaderno falsable; asíncrono por fichero; censo
  de vida; la semilla que dice por qué falló.
- Lo que la nueva función objetivo penaliza de la primera pasada: las **estaciones** de El Pozo y las
  temporadas de La Ladera (eventos que exigen reaccionar ante construcciones arbitrarias); las
  **bibliotecas de contenido de autor** (40-60 ruinas, 12-15 cámaras, 30-40 estancias, 8-12 cuencas);
  los **roles por geografía** de las seis; y el tramo manual que solo el playtest ajusta.
- Lo que la primera pasada descartó por «física congelada» y ahora se reabre: fibra recogible, vidrio
  transparente, tiro (ahora como consecuencia de aire que se mueve), carbón granular, luz por vidrio,
  objetos con temperatura propia, bloque frío como consumible (hielo).

## 3. Rúbrica v2 (la función objetivo hecha números)

1-10 por eje; el peso dice qué decide. Los ejes «inversos» se puntúan con 10 = poco o barato.

| eje | qué mide | peso |
|---|---|---|
| apalancamiento sistémico | cuánto juego nuevo (decisiones, situaciones, cruces) por unidad de complejidad añadida al sustrato | 3 |
| las leyes ejecutan, revelan y juzgan | ¿la simulación corre la decisión, la hace visible y produce la medida del resultado sin autoría? | 3 |
| iteración humana (10 = poca) | cuánto playtest, tuning, balance y contenido artesanal exige para que el core funcione | 3 |
| verificabilidad automatizable | ¿el core y su contenido se validan en banco headless, con hashes, sin personas? | 2 |
| la simulación es el juego | ¿el core loop existe sin capas externas acumuladas? | 2 |
| onboarding garantizable | ¿el aprendizaje se garantiza con situaciones pequeñas, no con descubrimiento espontáneo en un paisaje? | 2 |
| observabilidad | estados y transiciones legibles, con instrumentos y rayos X bien diseñados | 2 |
| tiempo como apuesta | preparar → SOLTAR → simulación → resultado como fuente de tensión y juicio | 1 |
| multiplayer emergente | algo divertido que hacer juntos, sin roles fijos; asíncrono por fichero válido | 1 |
| profundidad por leyes estables | 20/50/100 h sin depender de eventos ni de balance de un espacio infinito | 2 |
| cuerpo del jugador | consecuencias físicas legibles que crean decisiones, accidentes y clips | 1 |
| identidad comercial | frase, clip, cápsula (reutiliza `03`) | 1 |
| dificultad técnica (10 = fácil) | no penaliza mucho por regla: solo si es abierta, no acotada | 1 |

Puertas: iteración humana < 5 o apalancamiento < 5 → no finalista aunque sume.

## 4. Plan de la pasada (y qué queda persistido dónde)

1. `00` este documento (encargo, criterio, plan, registro).
2. **Panel de leyes y órganos** (Workflow): ocho lentes generan candidatos de modificación del
   sustrato (aire y viento, óptica de la luz, fases del agua, cuerpo del jugador, organismos y
   dispersión, materiales continuos y mezclas, instrumentos y rayos X, métricas que juzgan y estructuras
   de SOLTAR); dos refutadores por candidato (ingeniería/determinismo/coste; apalancamiento real y
   tuning oculto). Cada lente escribe su documento en `panel/leyes/`; la síntesis va a `01_LEYES.md`.
3. **Panel de direcciones** (Workflow): generadores desde ángulos distintos (mutación de El Pozo,
   campaña de situaciones + sandbox, SOLTAR como core, híbrido con núcleo claro, dirección nacida de
   una ley nueva, «las leyes juzgan», el cuerpo como instrumento, dirección libre), cada uno con el
   catálogo de leyes disponible; tres críticos por dirección (iteración humana oculta; la simulación es
   el juego; ingeniería, evidencia y prueba que la mata); jueces con la rúbrica v2. Documentos en
   `panel/direcciones/`; síntesis en `02_DIRECCIONES.md`.
4. `03_COMPARATIVA_Y_TIEMPOS.md`: rúbrica v2, tipo de trabajo (automatizable / secuencial / humano),
   tiempo hasta evidencia que mata, hasta prototipo feo, iteración humana esperada.
5. `04_POSICION.md`: mantener, transformar o reemplazar el veredicto anterior, con los órganos
   conservados y la respuesta a la pregunta final.

## 5. Registro de avance (se actualiza durante la pasada)

- 2026-09-05 · inicio de la segunda pasada. Nota de estado del repo: Cesar corrió `ca_playtest150.cmd`
  dos veces; la segunda ejecución recogió los archivos de la R151 (`docs/COMERCIAL/`, `ca_playtest151.cmd`)
  bajo el mensaje de la R150. Nada perdido; el mensaje de la R151 quedó en el cmd sin usar.
- 2026-09-05 · `00` escrito; directorios `docs/COMERCIAL/2/panel/{leyes,direcciones}` creados; panel de
  leyes en marcha.
- 2026-09-05 · panel de leyes lanzado: Workflow `wf_c2216429-b79` (8 lentes × ≤4 candidatos × 2 refutadores + crítico de huecos); los documentos caen en `panel/leyes/` conforme terminan; script copiado al scratchpad como `segunda-pasada-leyes.js` para reanudar con `resumeFromRunId`.
- 2026-09-06 · el panel de leyes cayó por límite de sesión con 40 de 73 agentes hechos: los ocho documentos de lente están en `panel/leyes/` (aire-viento, luz-optica, agua-fases, cuerpo, organismos, materiales, instrumentos-rayosx, metricas-y-soltar) y 23 refutaciones en `panel/leyes/refutaciones/`; faltan 41 refutaciones y el crítico de huecos. Reanudado con `resumeFromRunId` (tarea `wesettooq`). Hipótesis del arquitecto escritas antes del panel en `fable_hipotesis.md`. Corrección importante que salió del panel: la planta muerta YA deja fibra (SimStepper.Laboratorio.cs:811); lo que falta son rendimiento, cosecha, secado y transporte, no una descongelación.
- 2026-09-12 · segunda reanudación: 59 de 64 refutaciones en el journal y las 64 en disco (`panel/leyes/refutaciones/`); el crítico de huecos corre aparte. `01_LEYES.md` escrito: nueve hechos del código que corrigen la primera pasada (la planta ya deja fibra; el fuego ya ilumina el campo; los 7/73 de Q16 midieron la celda de sedimento y no el aire que leen las plantas; el alambique probablemente graniza; no existe fichero de partida), catálogo de 32 candidatos con valores corregidos, veredicto por lente y siete paquetes de sustrato (J Juicio, L Luz, A Aire, F Frío, V Vida, M Memoria, C Cuerpo) con su prueba que los mata. Siguiente: §6 de `01` con los huecos y lanzar el panel de direcciones con el catálogo.
- 2026-09-12 · panel de direcciones lanzado: Workflow `wf_870a9c01-734` (ocho ángulos: mutación de El Pozo, campaña de situaciones, SOLTAR como core, dirección nacida de una ley nueva, las leyes juzgan, el cuerpo, híbrido, libre; tres críticos por dirección: iteración humana oculta, la simulación es el juego, ingeniería y prueba que la mata; tres jueces con la rúbrica v2). Documentos en `panel/direcciones/`; críticas en `panel/direcciones/criticas/`; jueces en `panel/direcciones/_juez_*.md`. La dirección del arquitecto se escribe aparte en `fable_direccion.md` y se compara en `02`.
- 2026-09-12 · crítico de completitud terminado (`panel/leyes/_huecos_y_combinaciones.md`); `01_LEYES.md` §6 añadido: tres huecos adoptados (el hogar come = paquete D; los verbos del jugador como diseño de control; plantas que reponen aire), cinco contradicciones a cerrar, primera tanda de cinco candidatos (~8 semanas) y orden final del sustrato. `fable_direccion.md` («La Vigilia») escrita antes de leer el panel de direcciones.
- 2026-09-12 · el panel de direcciones cayó por límite con 28 de 35 agentes: las ocho direcciones están en `panel/direcciones/` (El Pozo Sellado, campaña de situaciones, SOLTAR como core, ley nueva, las leyes juzgan, cuerpo, híbrido, libre) y 20 de 24 críticas en `criticas/`; faltan cuatro críticas (cuerpo y ley-nueva de ingeniería; híbrido de ingeniería e iteración humana) y los tres jueces. Reanudado con `resumeFromRunId`.
- 2026-09-12 · `02_DIRECCIONES.md` escrito (las nueve direcciones, lo que converge, siete riesgos con código, finalistas F1 Días sin manos / F2 El Pozo Sellado condicionado / F3 TIRO como expansión, descartes con órganos) y `03_COMPARATIVA_Y_TIEMPOS.md` (rúbrica v2 con tres fuentes, cifras de los críticos, tiempos recalibrados; §4 jueces pendiente de la reanudación `wda6mk30x`). Falta `04_POSICION.md`.
- 2026-09-12 · `04_POSICION.md` escrito (Transformar; respuesta a la pregunta final con tres condiciones medibles; tabla mantener/transformar/reemplazar; datos E1-E7 y dos decisiones de Cesar).
- 2026-09-12 · reanudación `wda6mk30x` terminada: 35 de 35 agentes del panel de direcciones (24 críticas en `criticas/`, tres jueces en `_juez_*.md`). `03 §2` completado con las cuatro críticas tardías; `03 §4` con los rankings de los jueces (Días sin manos primera en los tres: 167 / 163 / 169; TIRO segundo finalista; El Pozo Sellado sexta; A que sí fuera); `04 §8` con lo que dijeron los jueces y la única corrección adoptada (TIRO pasa a F2 con la puerta E8; El Pozo Sellado sigue como contenedor condicionado); nota en `02 F3`. Ronda cerrada como R152 en `docs/archivo/HISTORIAL_RONDAS.md`; `ca_playtest152.cmd` generado. Sin código, sin arquitectura, sin plan para Opus.
