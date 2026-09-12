# DIRECCIÓN · CAMPAÑA DE SITUACIONES + SANDBOX · «DÍAS SIN MANOS»

*(Panel de direcciones, segunda pasada. Ángulo: campaña de situaciones + sandbox, mutación de Un Año
Después. Leídos: `01_LEYES.md`, las refutaciones de sello, balanza, cuna y testigo,
`_huecos_y_combinaciones.md`, `03_MERCADO.md`, P3 y P4 de la primera pasada, `LabBench.cs` (los nueve
montajes y `Correr`), el libro mayor y `LabParams`. Nada de esto está autorizado.)*

## 1. Nombre y frase

**Días sin manos.** «Cámaras pequeñas, cada una con un problema de agua, fuego o luz. Lo arreglas con
las manos, las quitas, y el mundo cuenta cuántos días aguanta sin ti. Las cámaras las envejece, las
valida y las ordena la propia física.»

Es Un Año Después con tres mutaciones: el año deja de ser fijo (cada situación tiene su horizonte, de
tres a treinta días); el Patio muere (el sandbox es la situación grande sin horizonte); y las 12-15
cámaras de autor a 2-4 semanas cada una se sustituyen por 12-20 **madres** de 15 líneas y una máquina
que las envejece, las muta, las valida y las ordena en banco. El veredicto es el órgano de Sin Manos,
«DÍA N SIN MANOS», hecho objeto del motor por el sello.

## 2. La experiencia narrada

**La unidad.** Una situación es un recorte de 96×64 a 256×144 celdas del grid de 768×288 (el motor no
cambia; los nueve montajes del banco ya son recortes, y los chunks dormidos hacen que el resto no
cueste). Trae un montaje, una **condición como dato** (cláusulas sobre el libro y la grilla: «sumido
≥ 90 % de lo emitido por día», «planta viva en (118,250) el día 30», «carbón entregado ≥ 0,8 × lo
carbonizado»), un horizonte en días (día = 1 800 ticks = 6 s a ×10) y el aprendiz dentro, con cincel y
frasco heredados. Cada toque pasa por las puertas de `AlkahestSim` y es una entrada del registro.

**La velocidad es un estado, no un botón** (del crítico de huecos): mientras la mano actúa, ×1; cuando
se retira, ×10 y el contador de días arranca. Tocar devuelve a ×1 y pone el contador a cero. No hay
rebobinar dentro de un intento: el tiempo que miras es horizonte gastado. Reiniciar es gratis e
instantáneo, y el intento fallido queda como película (un volcado por día) con scrub y **vista de
diferencia** entre días. El fracaso no es una nota: es un time-lapse que recorres hasta el día en que
se torció.

**Minuto 0-2.** Situación 1, «La que se inunda»: sala de 128×72, manantial arriba a la izquierda, el
agua subiendo por el suelo, un sumidero tras una pared de roca suelta. Los pies del aprendiz se
oscurecen (vista Piel: el halo de humedad alrededor del cuerpo, siempre encendido). Arriba, la
condición en dos barras: lo que el manantial emite, lo que el sumidero traga, y «3 días». Sin texto.

**Minuto 2-4.** Cincelas una celda de la pared. El agua corre al sumidero; la barra derecha sube.
Sueltas el cincel: ×10, «DÍA 1». Dieciocho segundos después: «CUMPLE · 3 días sin manos · 1 toque».
Debajo: «la máquina lo resolvió con 1 toque; 2 familias de solución conocidas». Ya sabes las tres
reglas: lo que dejas corriendo corre, tocar cuesta días, la puntuación la produce el mundo.

**Minuto 4-7.** Situación 2, «La que se enturbia»: la misma sala, pero el sumidero pesa la carga del
agua (balanza) y la condición pide «clara ≥ 50 % de lo sumido, 5 días». Sueltas sin tocar: falla, y el
scrub muestra el agua marrón entrando de golpe. Apilas grava delante del sumidero: una poza; el agua
descansa y decanta. Al soltar se enciende la **vista Carga**: la escalera subió un peldaño y te da el
rayos X de la ley que acabas de aprender.

**Minuto 7-10.** Situación 3, «La grava se cansa»: la poza de antes, 15 días. A ×10 ves la grava
ennegrecer grano a grano hacia el día 8 y el nivel subir. Primera apuesta real: tocar (limpiar la
grava, contador a cero, siete días perdidos) o confiar en que la poza aguanta. Si no aguanta, la
película enseña el día exacto en que rebosó.

**Hora 1.** El peldaño del fuego: «La pila que no arde» (contacto de aire), «La que humea» (carbonera
con boca de 1), «El horno que no vidria» (recinto contra hogar, 18/18 contra 0). Cada situación tiene
una hermana de contraste: igual salvo en una cosa. Aparece «el hogar come» (§5): el hogar se apaga si no
toca combustible, y «mantén el hogar vivo 30 días» es la primera situación donde la tolva de 466 s deja
de ser curiosidad y es la máquina que hay que construir.

**Hora 5.** Peldaños de cruce, los que el laboratorio produjo sin que nadie los escribiera: «El
alambique que ahoga» (R135), «El fuego de la sala» (el humo que oscurece el lecho mientras el serpentín
lo riega). Familias mutantes de cada madre (boca ±k, veta más gruesa, hogar movido), horizontes de 30
días, y el fichero: le mandas tu registro a un amigo (KB), él lo ve como película, lo **bifurca** el
día 12 («yo aquí habría abierto la boca») y te lo devuelve con su recibo junto al tuyo.

**Hora 20.** El sandbox: el nivel de referencia entero o un mundo nacido por simulación, sin
horizonte. Sus retos los deriva la máquina de su recibo de nacimiento («el doble de agua clara que
hoy», «planta viva 30 días en la cámara alta») y su contador global es «DÍA N SIN MANOS». Sellar un año
(360 000 ticks, 12 minutos de banco) y compartirlo es Un Año Después convertido en modo. Y las
**cámaras de la comunidad**: cualquiera recorta una madre de su sandbox (volcado + condición) y entra a
la escalera por la misma puerta que las del autor: validada, firmada y ordenada sin que nadie la juegue.

**Mientras corre.** Miras con la vista de la ley de la situación, el contador y la **vista Edad**
(`touchedTick` por celda: qué partes del mundo ya están en régimen). Decides lo único que hay que
decidir: tocar o confiar.

**Cómo lee el mundo.** Bandas de ley (`LabBandas` como única fuente de umbrales: 60, 40, 130, 170,
200; la vista pinta bandas, no rampas: la simulación resuelve a 8 bits y el ojo lee a 2), la vista Ojo
(velo por 255 − luz), la vista Piel, la vista Edad, el scrub con diff, y el recibo por día como vector,
nunca como cifra escalar.

**Con 2-3 personas.** Sin roles: el **relevo** (A juega los días 0-10 y sella, B sigue desde ese
estado, C cierra; el recibo es de los tres) y la **bifurcación** (dos registros sobre la misma
situación, comparados por recibo y por día de divergencia). Información asimétrica temporal: quien ya
subió un peldaño tiene el rayos X de esa ley y el otro no. Todo por fichero; nada online ahora.

**Cómo aprende.** Por el orden, no por el paisaje. El banco calcula para cada situación su **firma**:
el conjunto de leyes cuya desconexión cambia el veredicto (§4). La escalera se construye sola: primero
las de una ley, luego las que añaden exactamente una ley a las ya vistas, luego los cruces. El autor
decide el primer peldaño (agua) y nada más.

## 3. Core loop

En una frase: **entra en una cámara que la física envejeció, tócala lo menos posible, quita las manos
y deja que el mundo te cuente los días; lee la película y sube un peldaño que el banco eligió.**

```
madre (autor, 15 líneas) → envejecer → mutar → validar → firmar → ordenar → ESCALERA
                                                                             │
   PREPARAR (×1, cada toque cuenta) → SOLTAR (×10, contador de días) ────────┘
        ▲                                   │
        │ tocar = contador a 0              ▼
   LEER la película (scrub, diff, bandas) ← VEREDICTO al horizonte
        └─ reintentar (gratis) · compartir    (CUMPLE · días sin manos · toques · recibo)
```

## 4. Por qué explota mejor la simulación

**Ejecutan.** Ya: los nueve montajes del banco son situaciones sin saberlo, y el alambique con registro
vacío contra registro con caldera es la primera apuesta con juez (la refutación de la balanza lo
escribe: tapón quitado en T = 0 contra T = 6 000 discrimina «cuándo abrir el suelo»).

**Revelan.** Un volcado por día es la memoria del intento; la diferencia entre dos volcados es el
forense; las bandas y la vista Edad convierten los campos en frases. No hace falta huella ni testigo:
el sello revela después de ocurrido, que es lo que SOLTAR necesita.

**Juzgan.** Condición como dato sobre libro y grilla, balanza con bit `entregable` (carbón, ceniza,
semilla, desde arriba; la fibra ciega la boca, y eso también es juicio) y días sin manos. Los únicos
números humanos son «día» y las plantillas de condición.

**Lo que la máquina produce sin autor:**

1. *Envejecer*: N ticks headless antes de entrar; poza decantada, grava colmatada, terracota donde hubo
   fuego y carbón enterrado salen gratis; el recibo de nacimiento es la etiqueta.
2. *Mutar*: familias de una madre (boca, veta, hogar movido, tapón, canal), no cuevas libres: sin
   autor las leyes dan tres atractores (anegado, quemado, inerte), como probó el refutador.
3. *Validar*: el registro vacío debe fallar; el del autor, rejugado sobre el mutante, debe cumplir; si
   no, un **solver de macroverbos** (canal, tapón, boca, puente de arcilla, pila, serpentín; K = 32-64
   registros en el tick 0) busca solución; sin solución conocida, el mutante va al sandbox como reto
   marcado, nunca a la campaña. Fragilidad: fracción de cortes de una celda que la solución resiste.
4. *Firmar*: **la respuesta a la pregunta del ángulo es sí, con una condición técnica.** Con un
   `LeyesActivas` de 12-16 bits que gate cada pasada `Lab*` (agua, presión, infiltración y capilaridad,
   evaporación y condensación, turbidez, erosión y depósito, compactación y cocción, combustión,
   carbón, luz, planta, cuerpos, manantial y sumidero), la firma es `{L : veredicto(solución, ¬L) ≠
   CUMPLE ∨ veredicto(vacío, ¬L) ≠ FALLA}`. Coste: |L| × H ticks por situación, unos 7 minutos con
   H = 18 000; cien situaciones en una noche. La firma con la solución del autor es la lección; la
   unión sobre las del solver, el alcance.
5. *Ordenar*: greedy por inclusión de conjuntos (una ley nueva por peldaño; empate, la firma más
   corta; si no hay candidata, dos). Determinista, sin diseñador.
6. *Soluciones distintas*: recibo cuantizado a 8 tramos por métrica más la firma de la solución
   (`HashMat` no sirve, el refutador lo demostró). El histograma de Zachtronics sin servidor.
7. *Retos del sandbox* de la misma familia («≥ f × nacimiento») y madres de la comunidad por el mismo
   tubo.

**Contabilidad de contenido.** De autor: 12-20 madres (15 líneas + condición + solución como lista de
intervenciones en código, medio día de Opus cada una), cuatro o cinco plantillas de condición, la
gramática de mutación y los macroverbos. De máquina: 100-300 situaciones validadas, la escalera, los
histogramas, los retos. Y lo decisivo para la producción: **cada ley nueva del catálogo (aire A, vida
V, frío F, haz) entra en el recibo sin tocar el juez, y con 1-3 madres nuevas la máquina genera su
peldaño entero.** La profundidad crece por ley, no por cámara.

**Lo que se valida sin personas.** Todo lo anterior, más regresión: hash de versión de física en el
fichero (un registro caduca con cada cambio de física y se rechaza en vez de divergir), round-trip de
volcado y anillo de hashes cada 256 ticks.

## 5. Qué añade al sustrato

| pieza | qué es | cruza con | Opus | tuning |
|---|---|---|---|---|
| **J · Juicio** entero | diario bajo las cinco puertas y los sliders; volcado y carga (primer fichero de partida); `CorrerSello`; condición como dato; días sin manos; balanza `entregable` desde arriba con histograma de carga; `LabBandas` única fuente; anillo de hashes; hash de versión | todas: cualquier contador del libro pasa a juzgar | 5-6 sem | 8-9 |
| Cuna mínima | envejecer madres; `Clonar` en memoria; validador honesto; mutación de madres; cielo por geometría (toda celda vacía en la fila H−2 es boca: una boca cavada por el jugador ilumina) | erosión, depósito, cocción, carbón, germinación | 1,5-2 sem | 7 |
| **Firma y escalera** (nuevo) | `LeyesActivas`; ablación; orden greedy; soluciones distintas | ninguna: es banco | 1-1,5 sem | 9 |
| **Solver de macroverbos** (nuevo) | 5-6 verbos paramétricos; búsqueda; fragilidad | ninguna: banco | 1 sem | 8 |
| Vistas | bandas en pantalla; Ojo; Piel (`CuerpoSim` sensor, halo de 12); **Edad** por `touchedTick`; scrub y diff | presentación | 1-1,5 sem | 7 |
| Luz mínima | el día que remide Q16 con `luz[i+W]`; vidrio transparente que suda | luz, planta, humo, vapor | 0,7 sem | 8,5 |
| **«El hogar come»** (nuevo, del crítico de huecos) | consume el combustible que lo toca y se apaga sin él; sigue sin prender carbón (170 < 200) | tolva, carbón como consumible, fuego doméstico como apuesta | 0,5 sem | 7 |

Once a trece semanas de Opus en dos carriles: el del motor (sello → `CorrerSello` → validador → firma
→ escalera, secuencial) y el paralelo (balanza, bandas, vistas, Q16, vidrio, hogar que come). **No
añado ahora** A, F, V, M ni haz y cuña: el juez los recibe cuando lleguen, y cada uno es un peldaño
nuevo con 1-3 madres.

## 6. Principal riesgo de diseño

**El examen.** El panel dejó la pregunta abierta: ¿el jugador que prepara, suelta y lee un recibo
juega o rinde un examen? La estructura es la de Zachtronics (condición exacta, solución abierta,
histograma), pero allí el placer es ver correr la máquina que construiste, y aquí la máquina es una
forma hecha de tierra y agua, menos carismática que un brazo alquímico. Mitigaciones dentro de la
dirección: la velocidad como estado (preparar ya es mirar correr el mundo), el fracaso como película en
vez de nota, el reintento gratis, el histograma, y la apuesta de tocar o confiar, que es una decisión
con reloj. Si aun así no hay «otra vez», la dirección muere (§8). Riesgos segundos: la **firma
degenerada** (toda situación firma «agua» y la escalera tiene dos peldaños; se mide antes de escribir
código) y las **recetas** (los mutantes de una madre son el mismo puzle; lo mide el contador de
soluciones distintas, y una familia con una sola familia de solución se poda).

## 7. Cuánto depende de iteración humana

Lo que exige personas: (1) el «otra vez» (§8), una sesión con tres personas; (2) la legibilidad de la
condición y del recibo sin cifras (dos o tres láminas, cinco desconocidos: el G6 de la primera pasada);
(3) el largo del peldaño (cuántas situaciones por ley antes de cruzar: un número, no un contenido);
(4) las plantillas de condición (el factor f del refutador; familia fija, nunca por situación: cuatro o
cinco números para todo el juego). Tres o cuatro sesiones para el prototipo y una por cada 20-30
situaciones nuevas después, por muestreo. Lo que sustituye el banco: validez, resolubilidad,
fragilidad, orden, regresión, comparación y detección de copias. Las soluciones de autor se escriben
como listas de intervenciones en código, como la caldera del banco: ni siquiera exigen jugar.

## 8. La prueba más barata capaz de matarla

**Etapa 1, banco, tres días, sin motor nuevo.** Sobre los nueve montajes, con una intervención
genérica en `Correr` (la de la caldera, hoy cableada a `esAlambique`) y una condición por montaje
relativa a su recibo («goteos ≥ 500», «carbón ≥ 0,8 × plateau», «vidrio ≥ 18», «planta viva día 30»):
vacío contra intervención de referencia, contar cuántos **discriminan**. Después, ablación con los
interruptores que ya existen (`PresionActiva`, `TermicaPropia`, `GerminaPorMil = 0`, `CuerposActivos`,
`LuzCadaTicks` enorme) para ver si las firmas difieren. **Mata**: menos de 5 de 9 discriminan, o todas
las firmas colapsan en el mismo conjunto.

**Etapa 2, personas, una sesión.** Cinco situaciones como presets del `LabPanel` (ya carga presets y
corre a ×10), la condición leída en voz alta, el veredicto sacado del libro JSON a mano por Cesar. Tres
personas, el hermano de Cesar entre ellas. Se mide: tras un FALLA, ¿reintentan sin que nadie lo pida?
¿Alguien deja de tocar antes del horizonte por decisión propia? **Mata**: menos de dos de tres
reintentan, o nadie suelta. Entonces es un examen; los órganos valen para otra dirección, esta no.

## 9. Tiempos

- **Hasta evidencia para matarla: 2 semanas** (3 días de banco + 1 semana para presets y sesión).
- **Hasta prototipo feo que permita juzgar el core: 6 semanas** (sello mínimo con condición, contador
  y gesto de soltar, 3-4; bandas en pantalla y scrub, 1; ocho situaciones de los montajes del banco con
  firma calculada a mano, 1; el histograma puede ser texto).
- **Automatizable y paralelizable**: balanza, bandas, vistas, Q16, vidrio, hogar que come, solver: 5-6
  semanas en el segundo carril.
- **Secuencial**: sello → `CorrerSello` → validador → firma → escalera, 5-7 semanas en serie (el
  bitmask se adelanta al carril paralelo).
- **Iteración humana**: 3-4 sesiones para el prototipo; luego una por lote. Pequeña porque el banco
  decide validez y orden, y las personas solo deciden si es divertido.

## 10. Lo que deja fuera y por qué

- **El Patio** y el manantial parametrizado: segundo grid persistente que dependía de un huerto que
  nunca vivió; el sandbox con retos y sello de un año lo sustituye con lo que ya existe.
- **Las 12-15 cámaras de autor y el año fijo**: la biblioteca es lo que la función objetivo penaliza.
- **Roles por altura y etiquetas de procedencia**: bifurcar el registro y mirar el diff es la
  atribución exacta y gratis; relevo y bifurcación no tienen roles.
- **El Pozo como columna vertebral, estaciones, ruinas como biblioteca**: la persistencia vuelve solo
  como sandbox opcional; los eventos, no.
- **Testigo como material, huella, hollín, clima que recuerda**: cuatro memorias para lo que el volcado
  por día y el diff ya revelan; la terracota y la grava colmatada siguen siendo los instrumentos.
- **Lámpara, sondas con radio, veta en juego, polilla, escarcha**: refutados y sin peldaño que ganar.
- **Aire A, frío F, vida V, haz y cuña**: pospuestos, no descartados: el juez debe existir antes de
  medir qué situaciones crean. A entraría primero (apagar cerrando es el mejor peldaño de fuego).
- **Multiplayer online y lockstep**: el fichero da relevo, bifurcación e histograma; host y espejo
  esperan al «otra vez».
- **El cuerpo como leyes**: solo sensor (Piel) y registro tick-estampado; torpeza y peso, tras playtest.
- **Sonido como campo**: presentación valiosa sin peldaño propio hoy.

Si la dirección muere en §8, sus órganos para otra: el sello con condición como dato, el validador
honesto, la firma por ablación (ordena cualquier onboarding, con o sin cámaras), la velocidad como
estado, el hogar que come y la vista Edad.

## 11. Autoevaluación (rúbrica v2, 1-10)

| eje | nota | por qué |
|---|---|---|
| apalancamiento sistémico | 7 | casi todo es infraestructura de juicio (cero cruces físicos); compra que cada ley existente y futura se multiplique en situaciones; única física nueva: el hogar que come |
| leyes ejecutan, revelan y juzgan | 9 | condición como dato, balanza, días sin manos, volcado y diff; el único número humano es la plantilla |
| iteración humana (10 = poca) | 7 | tres o cuatro sesiones decisivas y muestreo después; el «otra vez» es humano e incomprimible |
| verificabilidad automatizable | 9 | discriminación, resolubilidad, fragilidad, firma, orden, regresión, copias: todo headless |
| la simulación es el juego | 8 | el marco es delgado; el riesgo del examen es que el marco pese más que la física |
| onboarding garantizable | 9 | orden por firma, hermanas de contraste, vistas por peldaño: es el objeto de la dirección |
| observabilidad | 7 | bandas, Ojo, Piel, Edad, scrub y diff; sin instrumentos físicos nuevos |
| tiempo como apuesta | 8 | tocar o confiar con reloj y sin rebobinar; el hogar que come le da al fuego algo que perder |
| multiplayer emergente | 6 | relevo, bifurcación e histograma por fichero, sin roles; nada simultáneo |
| profundidad por leyes estables | 7 | crece por ley y por madre; el techo lo pone el número de leyes, hoy doce familias |
| cuerpo del jugador | 5 | sensor y registro; sin consecuencias de control hasta el playtest |
| identidad comercial | 7 | «DÍA 312 SIN MANOS» y la puerta que se cierra en un fotograma; formato de demo y stream; arquetipo expediciones, base 60-150 k, sin clip co-op |
| dificultad técnica (10 = fácil) | 7 | acotado, C# puro, en banco; ablación y solver son fuerza bruta nocturna |
