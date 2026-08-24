# LO QUE PERSISTE — diseño de la nueva dirección (propuesta de Fable, ronda 25)

> **La visión en una frase**: cada semilla es una pregunta — *¿qué puede durar aquí?* — y el
> laboratorio es cómo el jugador le arranca la respuesta al mundo.

El cambio de eje que pides es más profundo de lo que parece y es EL correcto: "aprender a
fabricar" convierte cada semilla en un recetario que se agota; "descubrir qué persiste" convierte
cada semilla en un problema abierto con un espectro de soluciones. La fabricación pasa de ser el
objetivo a ser el INSTRUMENTO. Y encaja con lo que este motor ya sabe hacer mejor que nada: dejar
que la materia se comporte y que el jugador mire.

Este documento responde tus 8 puntos, en tu orden, y cierra con los mecanismos extra que pides
("sorpréndeme"), el corte exacto que propongo para el primer prototipo, y el inventario de lo que
ya tenemos construido que se reutiliza tal cual (que es mucho más de lo que parece).

---

## 0. Punto de partida: qué tenemos ya que sirve TAL CUAL

Antes de proponer nada nuevo, el inventario honesto — porque la mejor propiedad de esta dirección
es cuánto motor existente reutiliza sin tocar:

| Ya construido | Cómo sirve a la nueva dirección |
|---|---|
| Campo de temperatura por celda + transiciones por material (funde/congela/hierve/condensa/arde) con umbrales DESPLAZADOS POR SEED | Resistencia térmica y al frío ya son EMERGENTES y ya varían por semilla. Gratis. |
| Densidad + desplazamiento de líquidos | Flotabilidad/estratificación ya observables. Gratis. |
| `fluidity` | Viscosidad ya observable. Gratis. |
| Gramática de leyes por seed (playtest 18) | Se reutiliza como el SORTEADOR de las tablas de propiedades y del retículo de estados (misma filosofía, mismo XorShift). |
| Campo morfológico `patron/borde` (playtest 12) | El vestido visual de los ESTADOS (compactado, templado, calcinado) sin escribir un renderer nuevo. |
| Diario + bautizo + "solo lo presenciado" | El libro de patentes ES una extensión del diario. La filosofía ya está decidida y validada. |
| Encargos + Tolva + Favor | Los pedidos-por-propiedad son un cambio de CONTENIDO de los encargos, no de sistema. |
| HeatPlate, ChillStone, cubas, grifos, frasco, estante, cincel | 4 de las 5 máquinas mínimas son evoluciones de aparatos ya validados por ti en playtests 1-23. |
| Byte `aux` por celda (hoy solo gases/orgánicos lo usan) | Espacio libre para estado fino sin tocar `CellGrid`. |
| Tinte de color por mezcla (tech del playtest 24) | Las soluciones que se tiñen del color del soluto: el código ya existe. |
| Criatura/capullo (playtests 20-23) | Se APARCA del bucle mínimo y vuelve como el escalón vivo del lategame (ver §9). Nada se tira. |

Conclusión de viabilidad: **esta dirección es la más barata de todas las que hemos considerado
por unidad de juego que produce**, porque es sobre todo una reorganización del significado de
sistemas ya construidos, más ~4 piezas nuevas de tamaño contenido (retículo de estados,
solubilidad, prensa, banco de chispa).

---

## 1. Las propiedades mínimas de la primera versión

Criterio de corte (tu principio, operativizado): una propiedad entra solo si (a) produce una
OBSERVACIÓN legible en un mundo de arena que cae, (b) el jugador puede CAMBIARLA mediante un
proceso, y (c) interactúa con al menos otra propiedad (si no interactúa, es un dato, no una
mecánica).

**Entran 6 — tres ya existen en el motor:**

1. **Resistencia al calor** *(existe)* — umbral de ignición/fusión/ebullición por (material,
   estado), desplazado por seed. Observable: arde, funde, hierve… o PERSISTE al rojo. Manipulable:
   el estado la cambia (ver §4: compactar sube el umbral, calcinar lo sube más).
2. **Resistencia al frío** *(existe, se le añade un verbo)* — congelación ya existe; se añade
   FRAGILIDAD: ciertos estados, congelados, se AGRIETAN y caen a polvo (un golpe de cincel o de
   prensa los revienta). Frío deja de ser solo "se para" y pasa a ser un arma de proceso:
   congelar-y-romper es la vía barata de pulverizar sin prensa.
3. **Densidad** *(existe)* — observable como estratificación literal en una columna de líquido.
   Manipulable: compactar sube densidad, espumar/calcinar la baja.
4. **Solubilidad** *(nueva, la joya)* — un polvo en contacto con su disolvente se disuelve: la
   celda de disolvente se TIÑE del color del soluto (tech del tinte, ya escrita). Y lo crucial:
   **evaporar el disolvente PRECIPITA el soluto en el fondo, recuperado y PURO**. Esto sola da el
   bucle rey de la química real — disolver → decantar/filtrar → evaporar → recristalizar — es
   decir, SEPARACIÓN Y PURIFICACIÓN como gameplay, y es la puerta de la biblioteca transversal
   (sal, azúcar, cal: §7). Coste: una tabla (soluto, disolvente) por seed + 1 material "solución"
   por pareja activa.
5. **Dureza / respuesta a compresión** *(nueva)* — NO como campo de presión simulado (peligro,
   ver §8) sino como RESPUESTA AL EVENTO de la prensa: polvo → compacto; sólido frágil → polvo;
   líquido → escupe (incompresible: el desplazamiento visible que pediste); esponjoso → escupe su
   líquido embebido (¡exprimir!). Cuatro respuestas legibles, una máquina, cero física nueva de
   campo.
6. **Conductividad eléctrica** *(nueva, LA propiedad invisible)* — deliberadamente NO observable
   a simple vista: solo el banco de chispa la revela. Es el ejemplo canónico de lo que pedías
   ("propiedades no visibles que se descubren mediante pedidos o análisis"): el primer pedido que
   diga "algo que encienda la lámpara" obliga a pasar TODO el catálogo por el banco, y ahí el
   jugador descubre que el análisis es un instrumento, no un menú. Manipulable: la conductividad
   es función del estado (fundido conduce lo que sólido no; húmedo conduce; compactado a veces —
   el análogo grafito; ver §4).

**Quedan fuera de la v1 (con motivo):**
- *Viscosidad*: ya existe vía `fluidity` y se observa gratis en la cuba, pero NO tendrá máquina
  ni pedido en v1 — es observable pero aún poco manipulable; entra cuando las soluciones
  concentradas la modifiquen (v2 natural).
- *Inflamabilidad*: no es una propiedad aparte — es la cara "arde" de la resistencia al calor
  (umbral de ignición). Tratarla separada duplicaría sistema.
- *Presión como campo del mundo, magnetismo, pH*: §8.

Seis propiedades, cuatro máquinas nuevas o evolucionadas, y cada par (propiedad, propiedad) tiene
al menos un cruce jugable: soluble+evaporable=purificar; frágil-al-frío+prensa=moler; denso+
líquido=decantar; conductor+fundido=el pedido trampa ("conduce… hasta que se enfría").

---

## 2. Las máquinas mínimas

Cinco aparatos, sin redundancia — cada uno es dueño de un eje y cada observación tiene UN lugar
canónico donde mirarse. Placeholders visuales al estilo actual (sprites por código, latón +
carboncillo), la sofisticación gráfica después.

1. **EL CRISOL** *(evolución del HeatPlate)* — cámara de piedra con lecho de combustible debajo.
   La novedad clave: **la temperatura máxima la decide EL COMBUSTIBLE, no un dial**. Aceite llega
   a X°C; el análogo-carbón (que descubrirás calcinando lo correcto) llega más alto; algo mejor
   llega más alto aún. Regular calor = descubrir combustibles = la progresión térmica es
   descubrimiento, no un slider. (El dial E del HeatPlate clásico se queda para el modo clásico;
   el Crisol es el aparato de esta dirección.)
2. **LA CÁMARA FRÍA + ALAMBIQUE** *(evolución de ChillStone + cuba tapada)* — una campana fría
   sobre un recipiente: lo que hierve en el Crisol se CONDENSA aquí (las transiciones
   condensesAt/condensesInto ya existen en MaterialDef y hoy casi no se usan). Esto convierte
   evaporar en SEPARAR (destilación) en vez de en perder material. Frío como herramienta de
   captura, no solo de conservación.
3. **LA PRENSA** — dos mandíbulas de piedra y un peso que cae (V para cargar, como la mudanza).
   Las cuatro respuestas de §1.5. La presión se EXPRESA físicamente como pediste: por lo que se
   desplaza, escupe o compacta — nunca como un número flotando.
4. **EL BANCO DE CHISPA** — dos bornes, un hueco para la muestra, una lámpara. La lámpara
   enciende (y su BRILLO gradúa la conductancia: espectro, no booleano — desde ya, puntuación en
   gradiente). Es el único aparato de ANÁLISIS puro: no transforma, revela. Barato de verdad: una
   consulta a tabla + un sprite que brilla.
5. **LA COLUMNA DE ENSAYO** *(evolución de la cuba)* — un tubo alto de vidrio. Viertes líquidos y
   dejas caer muestras: estratificación por densidad, disolución (el tinte sube), flotabilidad,
   viscosidad (velocidad de caída) — CUATRO observaciones en un solo aparato pasivo sin una línea
   de lógica nueva: es literalmente dejar que el motor haga lo que ya hace, enmarcado en vidrio.

El Crisol transforma por calor; la Cámara captura por frío; la Prensa transforma por fuerza; el
Banco revela; la Columna observa. Cinco verbos, cero solapamiento.

---

## 3. Cómo se representa cada propiedad visualmente

Regla 48 aplicada a rajatabla — cada propiedad tiene su verbo visible y su lugar canónico:

| Propiedad | Se VE como | Dónde |
|---|---|---|
| Resistencia al calor | El material BRILLA incandescente al acercarse a su umbral (rampa de `emision`, el renderer ya la soporta) y luego transforma — o aguanta al rojo, que es la imagen de la persistencia | Crisol |
| Resistencia al frío | Orla de escarcha (borde blanquecino) y, si es frágil, GRIETAS (patrón `Fractura` del campo morfológico, ya existe) antes de reventar | Cámara fría |
| Densidad | Estratificación literal: capas que se ordenan solas | Columna |
| Solubilidad | El disolvente se TIÑE del color del soluto, de abajo arriba; al evaporar, cristales del color puro precipitan en el fondo | Columna / Crisol |
| Dureza | La respuesta física de la prensa (compacta / revienta / escupe); lo compactado se ve más DENSO (patrón más prieto, borde Neto, jitter bajo) | Prensa |
| Conductividad | NADA — invisible a propósito; solo la lámpara del banco la delata, y su brillo la gradúa | Banco de chispa |

Y transversal: el DIARIO anota cada observación presenciada como hecho por (material, estado) —
"el limo calcinado aguantó el rojo vivo", "la sal de la cuba encendió la lámpara solo húmeda" —
con el sistema de "solo lo presenciado, N de M" que ya funciona.

---

## 4. El estado mínimo para que el ORDEN importe

Esta es la decisión técnica central de la dirección, y propongo resolverla con una regla dura:

> **El historial vive en el ESTADO, no en una lista.** Los materiales son markovianos: todo lo
> que el pasado le hizo a esta materia está codificado en QUÉ es ahora. Ninguna celda arrastra
> historial; el orden importa porque el grafo de estados es NO CONMUTATIVO.

**El retículo de estados.** Cada materia base de la semilla existe en hasta 7 estados canónicos,
y cada estado ES un MaterialId propio (variantes generadas en `Universe.Create`, como ya
generamos leyes — el renderer, la física y el diario los tratan gratis como materiales normales):

```
                    ┌── enfriar LENTO ──→ RECOCIDO (dúctil: la prensa lo compacta, no lo rompe)
POLVO ── calor ──→ FUNDIDO
  │                 └── enfriar RÁPIDO ─→ TEMPLADO (duro pero FRÁGIL al frío: análogo vidrio/acero templado)
  ├── prensa ────→ COMPACTO ── calor ──→ (según seed: CERÁMICO irreversible, o vuelve a FUNDIDO)
  ├── disolver ──→ EN SOLUCIÓN ── evaporar ──→ POLVO PURO (recristalizado)
  └── calor sin fundir (tostar) ──→ CALCINADO (otra materia: menos denso, umbral térmico más alto, a veces combustible)
```

La no-conmutatividad que pedías sale sola del grafo, sin ningún sistema extra:

- **calor → presión**: fundir y prensar = el líquido ESCUPE, no obtienes nada (los líquidos no se
  comprimen). **presión → calor**: compactar y hornear = CERÁMICO, el estado más resistente.
  El orden es literalmente la diferencia entre nada y la mejor solución.
- **enfriar rápido ≠ enfriar lento**: templar da duro-pero-frágil-al-frío; recocer da dúctil.
  La MISMA materia, el MISMO horno, y el gesto de sacarla al frío de golpe o dejarla reposar
  produce dos materiales con vectores de propiedades distintos. (Y de paso: la cámara fría gana
  su segundo uso — templar — y el tiempo se vuelve un ingrediente.)

**Qué sortea la seed** (con la gramática de leyes existente, mismo XorShift, mismos principios):
el vector de propiedades de cada materia base; QUÉ aristas del retículo existen para cada base
(¿este polvo se puede calcinar? ¿este compacto ceramiza o refunde?); los umbrales; y los
MODIFICADORES de estado (compactar: +dureza +umbral térmico, a veces +conductividad — el análogo
grafito; calcinado: −densidad +umbral; templado: +dureza +fragilidad-frío; solución: conduce si
el soluto era iónico-análogo…). Modificadores con TENDENCIA fija entre universos (compactar
NUNCA baja la dureza: vocabulario universal, regla 17) pero magnitud y excepciones por seed.

**Coste real**: ~6 materias base × ~5 estados medios ≈ 30-35 MaterialIds nuevos (byte llega a
255: holgura de sobra), generados por tabla, cero código por-material. El `aux` queda libre para
un único flag transversal barato: HÚMEDO (mojado reciente), que afecta conductividad y peso — un
bit, no un sistema.

---

## 5. La garantía procedural: toda semilla tiene respuesta

Propongo **construcción + verificación abstracta**, jamás rechazo-por-simulación:

1. **Construir la solución al sortear** (como ya plantamos la afinidad): tras generar tablas y
   retículo, `Universe.Create` elige un (base, estado) alcanzable desde el material primigenio en
   ≤3 operaciones y le FUERZA el vector de propiedades para superar la batería de ensayo inicial
   (umbral térmico > temperatura del ensayo, etc.), clampando las tablas. No se predetermina CUÁL
   es para el jugador — solo se garantiza que existe.
2. **Verificar con un solver de grafo, no con la sim**: como toda la química es de TABLA, la
   alcanzabilidad es una búsqueda en anchura sobre ~35 nodos — microsegundos en `Create`, cero
   simulación. `Debug.Assert` + línea en el log de seed (como el log de leyes): *"Semilla
   127: existe persistente a 3 pasos (verificado)"*. Invariante del contrato, nivel regla 33.
3. **Garantizar el GRADIENTE, no solo la existencia**: el solver verifica además que haya un
   descubrimiento a 1 paso (para el primer pedido: que el jugador pruebe UNA operación y ya vea
   una propiedad cambiar) y la solución completa a ≤3. Sin escalón fácil, la semilla ajusta. Una
   semilla válida es una escalera, no solo una cima.

El mismo solver, nota, es el CIMIENTO de los pedidos por propiedad: antes de emitir "algo que
aguante X", el OrderSystem le pregunta al solver si esta semilla puede — se acabaron para siempre
los pedidos imposibles (el bug que ya nos mordió en el playtest 22).

---

## 6. Patentes: procesos, no recetas

**Registro por HORNADA.** La unidad trazable no es la celda (imposible, regla del hot path) ni la
partida (inútil): es la hornada — lo que hay en el recipiente de trabajo de una máquina. Cada
operación de máquina añade un paso al historial de esa hornada (buffer acotado, ~8 pasos):
entrada, operación, condición relevante (combustible usado, rápido/lento), salida. Las máquinas ya
son los ÚNICOS puntos de transformación dirigida, así que la instrumentación es local a 5 clases —
ninguna carga en `SimStepper`.

**Patentar**: cuando una hornada produce un (base, estado) o un vector de propiedades que el
diario no tenía, el diario ofrece PATENTAR: se congela el historial como página — entradas,
proporciones, orden, máquinas, condiciones, resultado — y el jugador la BAUTIZA (el sistema de
bautizo ya existe; ahora bautiza procesos además de sustancias). El libro de patentes es una
sección nueva del diario, no un sistema nuevo.

**Ejecutar sin tedio — la configuración fantasma**: seleccionas una patente en el libro y las
máquinas implicadas muestran en SEMITRANSPARENTE su ajuste requerido (qué combustible, qué orden,
paso 2 de 4…), con el paso actual latiendo. El jugador sigue ejecutando FÍSICAMENTE cada gesto —
verter, cargar la prensa, retirar al frío — pero no recuerda ni reajusta nada de memoria. Es un
guía de montaje, no un botón de auto-craft: conserva la sensación de laboratorio que exiges, mata
el tedio de memoria. (La automatización real — un autómata que ejecute patentes solo — es
exactamente el tipo de premio de lategame que puede venderse caro en Favor. §8, después.)

**Patentes como economía**: el Maestro paga el pedido... y paga MÁS por la patente que lo
resuelve ("no me traigas el ungüento: tráeme el PROCEDIMIENTO"). Entregar sustancia = pago único;
patentar = royalties (goteo de Favor cada vez que reproduces o que el pedido se repite). El juego
premia entender por encima de producir — que es exactamente tu tesis.

---

## 7. La biblioteca transversal entre mundos — veredicto: SÍ, con firmas funcionales

Es viable y es, además, la respuesta correcta a "¿para qué jugar otra semilla?".

**Cómo, sin simular química real**: cada descubrimiento reconocible se define como una **firma
funcional** — un predicado sobre (vector de propiedades + génesis del proceso), nunca una
identidad química:

- **VIDRIO** ≈ nació fundiendo un polvo + enfriado rápido; sólido; frágil al frío; no arde; no conduce.
- **SAL** ≈ polvo soluble que recristaliza al evaporar; no arde; su solución conduce.
- **CARBÓN** ≈ nació calcinando; combustible de larga duración; conduce COMPACTADO.
- **CERÁMICA** ≈ polvo prensado y luego cocido; el umbral térmico más alto de su cadena; irreversible.
- **CAL** ≈ calcinado que, húmedo, REACCIONA con calor (la ley de contacto la pone la seed).
- **JABÓN, TINTE, PÓLVORA…** — la lista crece por datos, no por código.

El matcher corre al patentar: si la firma casa (estricta en los 2-3 rasgos icónicos, tolerante en
el resto), el juego anuncia *"esto, en algún mundo, se llamó VIDRIO"* y lo inscribe en la
**biblioteca permanente** — el primer dato persistente entre partidas del proyecto (JSON en
persistentDataPath; la persistencia llevaba dos fases en el backlog esperando su motivo: es este).
En la siguiente semilla, la biblioteca no te da la receta (las tablas cambiaron) — te da la
PREGUNTA: "aquí hubo vidrio; ¿qué lo hace posible en este mundo?". Conocimiento transversal =
saber qué buscar; conocimiento local = saber cómo. Exactamente la división que quieres.

Coherencia sin química imposible: las firmas se apoyan en tendencias FIJAS del retículo (§4 —
compactar siempre endurece, calcinar siempre sube el umbral), así que un "vidrio" de cualquier
semilla siempre se sentirá vidrio en lo que importa, aunque su color, su base y sus números sean
de ese mundo. Con 8-10 firmas curadas a mano basta para la v1; y es la semilla del sueño del
veneno para ratas: una firma ES un pedido con nombre propio (ver §10).

**Riesgo a vigilar**: si las firmas son laxas, el momento "¡descubrí vidrio!" se siente como un
achievement arbitrario — tu miedo es correcto. Mitigación: pocas firmas, rasgos icónicos
estrictos, y el anuncio SIEMPRE explica el porqué ("frágil al frío, nacido del fuego rápido: eso
es vidrio en cualquier mundo").

---

## 8. Lo peligrosamente complejo — a versiones posteriores, con el porqué

1. **Presión como campo simulado del mundo** — duplicaría el coste del hot path para sostener una
   propiedad que la prensa expresa mejor como evento local. La versión-campo solo si algún día
   hay profundidades con presión ambiental (ver la Veta, §10).
2. **Electricidad en el mundo** (circuitos, cables, electrólisis) — el banco de chispa da el 90%
   del juego de la propiedad al 5% del coste. Electrólisis = v3, cuando haya soluciones maduras.
3. **Concentraciones continuas y multi-soluto** — v1: un soluto por disolvente por hornada, tinte
   en 2-3 escalones. La mezcla de 3 cosas en gradiente es un agujero de complejidad clásico.
4. **Automatización real (autómata ejecuta-patentes, tuberías)** — la configuración fantasma
   primero; vender la automatización como premio caro después. Si la v1 automatiza, mata el
   laboratorio antes de que exista.
5. **Pedidos semánticos ("veneno para ratas")** — el DESTINO, no el principio: exige el sistema de
   firmas maduro + criaturas de prueba (las RATAS son organismos: §9). El puente ya queda tendido
   — pedido = predicado con puntuación en espectro — pero la semántica rica es lategame.
6. **Organismos como soluciones** (sobrevivir, propagarse, producir) — toda la infraestructura
   Criatura/Vivium queda APARCADA INTACTA para esto (regla 15/26: se conserva, no se borra). Es
   el escalón "materia → materia viva" y merece su propia fase.
7. **La Marea** — descartada como dirección; en la ronda de construcción neutralizo su despertar
   (el material y el corazón pueden quedar dormidos en el código sin coste — o los retiro si
   prefieres limpieza total, decisión tuya, ambas son baratas).

---

## 9. El material primigenio y el flujo infinito (los detalles que dejaste abiertos)

**EL LIMO PRIMIGENIO como tutorial-en-un-material.** El mundo arranca con UNA sustancia: una
suspensión turbia que gotea, infinita y lenta, de una fisura del laboratorio (el único flujo
infinito junto al agua — todo lo demás del juego es REFINADO, no recolectado: un solo grifo del
que desciende toda la materia del universo, elegante y fácil de balancear). El Limo contiene en
suspensión trazas de las ~6 materias base de la semilla. Tu primer acto de juego posible:
verterlo en la Columna y esperar (decanta en capas = densidad), o hervirlo en el Crisol (el agua
se va, precipitan polvos distintos = tu primera separación). **El primer gesto del juego ya es el
juego entero**: mirar qué hace la materia cuando la fuerzas, y quedarte con lo que queda. De la
fisura al catálogo completo hay solo curiosidad.

**EL ENSAYO DEL MAESTRO — el pedido como instrumento.** Los pedidos por propiedad no se "entregan
y a esperar el veredicto": junto a la Tolva hay un BANCO DE ENSAYO VISIBLE donde la muestra
entregada se somete FÍSICAMENTE a la condición del pedido — la ves arder, o aguantar; hundirse, o
flotar. Tres consecuencias: el veredicto es teatro físico y no un cartel; un fallo DEVUELVE
INFORMACIÓN (viste exactamente cómo murió tu muestra — el pedido fallido es un experimento
gratis); y la puntuación en espectro es natural (aguantó justo la temperatura pedida = ★;
aguantó el doble = ★★★). El espectro de soluciones "mejores o peores" que sueñas para el veneno
de ratas queda instaurado desde el PRIMER pedido del juego.

**Los primeros pedidos, en tu línea** ("reproducir algo relacionado con el primigenio"):
1. *"Separadme el limo: traedme una sola de sus arenas, pura."* (enseña la separación)
2. *"Algo que aguante el rojo del crisol sin arder."* (enseña la persistencia térmica)
3. *"Algo que encienda la lámpara."* (enseña que existe lo invisible)
4. *"Algo que flote en el agua y no se disuelva en ella."* (enseña el cruce de propiedades)
5. *"El procedimiento del nº2, por escrito."* (enseña que el conocimiento se paga: primera patente)

---

## 10. El corte que propongo para el primer prototipo (playtest 25)

**DENTRO** (en orden de construcción): retículo de estados + tablas por seed + solver de garantía
(§4+§5, el corazón, un encargo Sim); Limo primigenio + fisura; Crisol por combustible; Prensa;
Columna de ensayo; solubilidad + evaporar/recristalizar; Banco de chispa (análisis puro); 5
pedidos-por-propiedad con ensayo visible y puntuación en espectro; registro de hornada + patente
v0 (página en el diario + configuración fantasma solo-texto); diario anotando propiedades
presenciadas.

**FUERA de la v1** (aunque duela): biblioteca transversal (v2 — necesita que las firmas tengan
materia que reconocer; la persistencia se construye entonces), alambique/condensación (v2),
fragilidad-frío (v2, la cámara fría v1 solo templa/recoce), configuración fantasma visual sobre
las máquinas (v1 es una lista de pasos en el libro), royalties (v1: pago único mayor por
patente).

Así la v1 responde la pregunta de diseño clave con lo mínimo: **¿es más divertido descubrir qué
persiste que aprender a fabricar?** Todo lo demás — biblioteca, organismos, semántica — apila
sobre esa respuesta si es sí.

---

## 11. Lo que necesito de ti antes de construir

1. **Sí me sirve el commit del playtest 24 — ejecútalo, por favor** (`ca_playtest24.cmd`). Motivo
   operativo: todo mi flujo (desplegar, resetear el sandbox, verificar) asume que tu disco ==
   GitHub; si queda sin commitear, tu disco y el repo divergen y el siguiente despliegue puede
   pisar o mezclar. Motivo de archivo: la Marea queda en la historia como experimento validado y
   descartado — descartar es una decisión de diseño y las decisiones se documentan (el commit 25
   la neutralizará explícitamente). Nota: hasta esa neutralización, si juegas, la marea SIGUE
   despertando al cavar — no es un bug, es el estado del código.
2. **Tu go a este documento** — con los ajustes que quieras (¿la conductividad dentro o fuera de
   la v1? ¿retiro la Marea del todo o la dejo dormida?). Con tu go, congelo el contrato
   (CONTRATO_PERSISTE.md) y lanzo los encargos como siempre.
