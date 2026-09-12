# SEGUNDA PASADA · HIPÓTESIS DEL ARQUITECTO (antes del panel)

*(Fable 5.1, 2026-09-05. Escrito ANTES de leer los resultados del panel de leyes, para que la síntesis
pueda compararse con lo que yo pensaba y para que un corte de sesión no pierda la línea de razonamiento.
Todo es hipótesis; lo que el panel refute se retira en `01_LEYES.md`.)*

## 1. Lo que el código dice hoy (leído para esta pasada)

- **Aire.** No hay un medio que se mueva. `LabAire` (SimStepper.Laboratorio.cs:328) difunde el vapor
  (`VaporDifusion`) y lo hace subir (`VaporAscenso`); el humo es un material que sube y persiste 255
  ticks. `Conveccion` (LabParams:113) es un flag de la **difusión térmica** (`LabDifusionTermica`, :1306):
  el calor sube antes que baja; no mueve masa. `TiroAmbienteTicks` no es tiro: es la relajación de cada
  celda hacia los 20 °C de fondo. Confirmado: el tiro de chimenea no existe porque no existe el aire
  como fluido.
- **Luz.** `LabLuz` (:1177) es una propagación de máximos: 255 en la boca del cielo, baja con decaimiento
  `LuzDecayCielo` por la vertical, `LuzDecayAire` de lado y hacia arriba, pasa por agua y planta con su
  decaimiento, no pasa por sólidos (vidrio incluido), el humo la come. `LabMateriales.EmiteLuz` existe
  como gancho. Sin dirección: no hay rayos, así que no hay espejos ni lentes en este modelo; sí hay,
  gratis, **transmitancia por material** (añadir vidrio y hielo a la lista de paso con su decaimiento) y
  **guías de luz** (una columna de agua o vidrio conduce luz hacia abajo: el modelo ya lo hace con el
  agua).
- **Térmica.** K y C por material (`LabK`, `LabC`), latente en evaporación y condensación
  (`LabLatente`), hogar que calienta hasta un tope (`LabCalentarHasta`), núcleo frío que enfría con
  `FrioPotencia`: el frío es un sumidero infinito, exactamente el «objeto mágico» que un crítico señaló.
- **Plantas.** `LabPlanta` (:801) ya es un organismo de columna con ramas (`PlantaRamaPct`,
  `PlantaAltoMax`) que crece hacia la luz (`destino` por `luz[]`); la muerte abona. La fibra recogible es
  una línea: transformar la planta muerta en `Fibra` en vez de abono (o en las dos, por altura).
- **Métricas.** `LabBench.Informe()` ya imprime goteos, columnas anegadas, sustrato, siete hashes y los
  dos libros de energía. El «juez» está a medio construir.

## 2. Candidatos de sustrato que yo apostaría (para contrastar con el panel)

Ordenados por juego nuevo por unidad de complejidad, con mi estimación antes de la refutación.

1. **Aire como fluido en rejilla gruesa (el modelo de The Powder Toy).** Un campo de presión y velocidad
   en bloques de 4×4 celdas (192×72 = 13 800 celdas de aire), en enteros, determinista: los sólidos
   bloquean, el calor empuja hacia arriba (flotabilidad por temperatura), las aberturas dejan pasar. Todo
   lo que hoy «sube» (humo, vapor) y lo que hoy no se mueve (ceniza, polvo, chispas, semillas, hojas) se
   **advecta** por ese campo. Compra de golpe: el tiro de chimenea como consecuencia (fuego + conducto
   vertical = corriente), viento por geometría (una boca alta y otra baja), secado por corriente, el humo
   que **se va** (hoy se acumula 255 ticks), chispas que prenden a distancia (riesgo y decisión), semillas
   que viajan, vapor que llega lejos (resuelve la objeción de «condensación local» de la lente vertical:
   el vapor viaja por la corriente y condensa donde el aire se enfría), el humo como **visor** de la
   corriente sin ningún panel. Coste: 3-5 semanas de Opus, acotado, con benchmark headless (tiro de una
   chimenea medido en celdas de humo por minuto; hash). Tuning: bajo si las constantes son físicas
   (flotabilidad ∝ ΔT, fricción por sólido). Riesgo: coste por tick (el campo grueso es barato; la
   advección de partículas es lo que hoy ya cuesta) y que la presión oscile (lo resuelve la disipación).
2. **Insolación: la luz calienta según albedo.** Una línea en la difusión térmica: cada celda con
   `luz` alta y material absorbente (carbón, terracota, roca oscura, agua) recibe calor; los claros
   reflejan (nada). Cruza: evaporación y secado (un lecho al sol se seca; regar es una decisión con
   reloj), hielo que se derrite al sol, vapor que sube del suelo mojado al amanecer, roca que guarda
   calor de día, y la posibilidad de **encender yesca con luz concentrada** si una guía de vidrio suma
   luz (concentración por geometría, no por lente). Coste: una semana. Tuning: bajo (un coeficiente por
   material). Convierte la luz en el cuarto común con la misma profundidad que el agua.
3. **Hielo.** Agua < 0 °C se vuelve hielo (sólido que flota, aísla, bloquea y se derrite; latente
   simétrico). El bloque frío deja de ser infinito: el hielo **es** el frío que se puede llevar, poner
   bajo un techo para hacer llover, meter en un frasco caliente para enfriar, apilar como presa que cede
   al sol. Fuente de frío: el núcleo frío existente como «fuente» de nivel (como el manantial lo es del
   agua) y, si se quiere, el cielo nocturno (parámetro periódico, no evento). Coste: 1-2 semanas. Cruza
   agua, térmica, luz (insolación), presión (una presa de hielo que cede inunda).
4. **Fibra recogible y semilla que viaja.** La planta muerta deja fibra; la semilla es partícula que el
   aire y el agua mueven. Cierra el ciclo huerto → fibra → tolva → alambique → huerto y hace que el
   jardín se siembre solo donde luz y humedad coinciden (el mejor tutorial de la primera pasada, sin
   autoría). Coste: días. Tuning: bajo.
5. **El cuerpo como celdas del mundo.** El avatar ocupa 2×4 celdas que participan de humedad, temperatura y
   aire: mojado pesa y no prende yesca, junto al fuego suelta lo que lleva, en humo tose y la vista se
   estrecha, en corriente lo empuja, en agua flota. Sin barras: estados visibles en el sprite (empapado,
   humeante, tiznado, tiritando). Coste: 2-3 semanas (el muñeco ya lee campos). Tuning: medio (la
   sensación de control es playtest); apalancamiento alto en accidentes, humor y clips, y en
   **onboarding**: el cuerpo es la primera sonda.
6. **Huella (8 bits por celda).** Memoria de lo ocurrido: mancha de goteo, marca de marea, hollín,
   temperatura máxima. Una causalidad se lee después de ocurrida. Coste: una semana. Es
   observabilidad sin panel y sin autoría.
7. **Rayos X como instrumento construible.** Un visor por campo (calor, humedad, corriente, luz) que se
   activa uno a la vez, con resolución de bandas y agregado por zoom; en co-op, un jugador puede tener un
   visor que el otro no (información asimétrica temporal, no rol). Coste: 2-4 semanas de render; tuning
   bajo si las bandas se derivan de los umbrales medidos (60, 40, 130, 170, 200).

Lo que NO apostaría: espejos y lentes (exigen luz direccional: reescritura), seres lectores (contenido
disfrazado), estaciones (eventos), electricidad o metal (capa nueva encima).

## 3. La estructura de juego que creo que sale de la función objetivo

La pregunta final de Cesar (¿pueden las leyes ejecutar, revelar y juzgar?) apunta a una estructura
concreta que en la primera pasada estaba repartida entre Un Año Después y Sin Manos:

**La situación.** Una rejilla pequeña (de 96×64 a 256×144) con una condición expresada como **métrica que
la simulación ya produce** (celdas de agua clara en el sumidero, plantas vivas a los N minutos, carbón
en el pozo, goteos en el cuenco, temperatura sostenida en el recinto). El jugador **prepara** con un
presupuesto (celdas tocadas, materiales disponibles), **suelta** (la simulación corre a ×10 hasta un
horizonte fijo) y **las leyes juzgan** (la métrica; el estado final con hash, compartible). La misma
situación admite muchas soluciones; la comparación entre personas es por métrica, no por juicio humano.

**Por qué explota la ventaja real.** Las situaciones se pueden **generar** (geología que se genera
simulándose, ruinas amables = ley + rotura pequeña) y **validar sin personas** en el banco headless:
discriminabilidad (¿el resultado cambia con intervenciones distintas?), resolubilidad (¿un agente o una
búsqueda encuentra una solución?), sensibilidad (¿qué parámetros y leyes importan en esta situación? →
eso ordena la campaña por leyes implicadas, sin diseñador). El onboarding queda garantizado por el
orden, no por el descubrimiento espontáneo. Y el sandbox persistente (el pozo, la ladera) es «una
situación grande sin horizonte», con las mismas métricas como retos opcionales.

**Lo que sigo sin saber y el panel debe atacar.** Si la situación con presupuesto y horizonte es un
juego o un examen (la tensión de SOLTAR frente al tedio de esperar); si el cuerpo (avatar) o la mano
(cursor) sirven mejor a esta estructura; si el aire como fluido es el multiplicador que creo o una fuente
de caos ilegible; y si el contenedor persistente aporta o distrae.

## 4. Lo que anticipo como posición (a confirmar o refutar)

Transformar el veredicto anterior, no mantenerlo ni reemplazarlo del todo: El Pozo deja de ser la
columna vertebral y pasa a ser el sandbox-contenedor; la columna vertebral pasa a ser **situaciones
generadas y juzgadas por las leyes, con SOLTAR como verbo central**, sobre un sustrato enriquecido con
aire, insolación, hielo, fibra, cuerpo y huella. Si el panel encuentra algo mejor, cambio.
