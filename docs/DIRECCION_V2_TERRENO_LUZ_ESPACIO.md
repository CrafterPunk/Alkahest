# DIRECCIÓN V2 — TERRENO, LUZ Y ESPACIO (fotografía tras la piel de roca, R125)

*(Documento de dirección. Sucede a `EVALUACION_2D_VS_2_5D.md` (R123), que queda
como archivo histórico tal cual: allí está la exploración; aquí, las
decisiones. Nace de la prueba `PRUEBA_PIEL_DE_ROCA.md` (R124) y de la
conversación de cierre del 1/09/2026. Se apoya en el decreto del mundo (GDD
§0), en `DIRECCION_DE_ARTE.md` (normativo para personaje, paleta, talla y
cámara) y en `Capas.cs`. NO abre tareas: es la foto de dónde estamos para
continuar mañana sin reabrir lo resuelto.)*

Cómo leerlo: cada punto lleva una de tres etiquetas y solo una.
**[DECIDIDO]** ya no se discute salvo evidencia nueva con captura.
**[HIPÓTESIS]** dirección futura registrada; no es backlog ni promesa.
**[APLAZADO]** decisión que existe, que NO se toma todavía, y que dice qué
tiene que existir antes de tomarla.

---

## 0. LA FRASE

**Un juego 2D lateral bien explotado: la grilla manda, la materia se ve grano
a grano, la roca es orgánica, la luz tiene causa, y todo lo que da
profundidad es pintura.**

---

## 1. ARQUITECTURA — [DECIDIDO]

1.1 **La base es 2D.** Cámara ortográfica lateral, plano de juego = la grilla,
1 celda = 1 téxel para la MATERIA. Se mantienen sin excepción: la simulación
celular, la precisión de interacción (el punto de pantalla ES la celda) y la
cámara lateral con sus tallas selladas (`DIRECCION_DE_ARTE.md` §3.5: muñeco 12
celdas, 80 celdas visibles por defecto).

1.2 **El 2.5D real (Z, mallas 3D, cámara con profundidad) NO es dirección
activa.** Queda archivado con su análisis en la evaluación R123 §2–3. No se
reabre salvo que una era futura (la III, seres) demuestre una necesidad que la
pintura no cubra.

1.3 **La presentación final SÍ usará, con intención, las herramientas 2D
modernas de iluminación y profundidad**: luz 2D con normales y sombras
planas, varios planos de parallax, post-proceso de URP, arquitectura frente y
fondo. No es adorno opcional: es parte de la dirección visual, con los
límites del §3.

1.4 **Regla de oro (la guardiana de todo lo anterior):** *la cámara es
ortográfica y lateral, el plano de juego es la grilla, y toda profundidad es
pintura. La colisión NUNCA viene de la pintura.* (Es la misma frase que rige
el Nivel 4 de `Capas.cs` desde la R66.)

---

## 2. LA GRAMÁTICA VISUAL ÚNICA — [DECIDIDO como principios]

Lo que la prueba de la piel demostró es que el juego tiene TRES naturalezas
en pantalla y que se leen mejor cuando cada una habla distinto:

| Naturaleza | Cómo se dibuja | Ejemplos |
|---|---|---|
| **Lo natural** (la tierra, la cueva) | orgánico: contorno de marching squares, tinta, bandas por orientación, masa interna oscura, grietas | roca madre |
| **Lo construido / heredado** (lo que alguien hizo) | recto, con junta, a todo color, pátina de óxido y musgo | piso estructural, máquinas, depósitos, telón de ladrillo |
| **Lo vivo / la materia** (lo que se simula) | granular, cuadrado, 1 celda = 1 téxel, color de referencia intocable | arena, agua, brasa, humo, barro |

Principios que salen de ahí:

- **P1 · La materia nunca se disfraza.** Lo cuadrado es la firma de "esto
  está simulado de verdad". Ningún filtro, luz ni suavizado puede quitarle a
  un material su color de referencia ni su grano. (Lo que R123 llamó "la sim
  protagonista".)
- **P2 · Lo natural es orgánico, lo hecho es recto.** La piel de roca solo
  cubre `Stone`; cualquier sólido fabricado (piso, mortero, cerámico, vidrio)
  se queda en grilla/recto a propósito. La costura entre ambos es una JUNTA
  (tinta fina, sin banda), no un borde.
- **P3 · La tinta es la costura.** La línea de contorno oscura (regla 19 de
  la dirección de arte) es lo que une ilustración, roca y máquinas en un solo
  dibujo. Donde haya duda de acabado, la tinta decide antes que el pixel.
- **P4 · La luz tiene causa.** Solo emiten luz las cosas que en el mundo la
  emiten: Fire real de la sim, brasas y temperatura, los ojos-lámpara y el
  brote del muñeco, vidrio caliente, el vano (luz del exterior). No hay
  "luces de escena" decorativas.
- **P5 · La profundidad es pintura ordenada.** Los 6 niveles de `Capas.cs`
  son la única escalera; lo nuevo entra en un nivel existente (fondo
  evolutivo en el Nivel 0, cantos y sombras proyectadas dentro del Nivel 3,
  polvo en el 5). Nada se sale de la tabla.
- **P6 · La paleta madre manda** (tinta parda sobre ceniza cálida; BRASA ·
  PÁTINA · ÁMBAR · AZUL MUDANZA · VINO, validada por daltonismo en R105). La
  luz y el fondo evolutivo se calzan en ella; no se añaden acentos nuevos.
- **P7 · Lo que se juega es el defecto** (lección R109): toda prueba visual
  se juzga a 80 celdas de cámara, con el muñeco en pantalla, nunca en el plano
  amplio ni en el zoom íntimo.

---

## 3. LA ILUMINACIÓN — [DECIDIDO como dirección; parámetros como punto de partida]

Deja de ser una comparación hipotética entre caminos: es parte de la
dirección visual y se probará cuando corresponda (§7, orden). Estos
parámetros son razonables para ESTA arquitectura; se afinan viendo, no se
imponen.

**Herramientas (Unity 6 / URP 17):**

- **2D Renderer de URP** en lugar del Universal Renderer actual (hoy no hay
  ninguna luz en el proyecto porque `Light2D` no existe fuera del 2D
  Renderer). El cambio se hace en una copia del RP asset y se valida en build
  (regla del playtest 2: shaders eliminados de la build, cero `Shader.Find`).
- **`Light2D`**: una global tenue (la "ceniza" ambiente) + puntuales con
  causa. **Normales** en la piel de roca (derivadas de su propia textura),
  máquinas (derivadas de la altura de sus texturas procedurales) y personaje
  (el pipeline 3D las da; hasta entonces normal-from-height del PNG).
- **`ShadowCaster2D`** solo en muñeco, máquinas grandes y arquitectura frente;
  nunca en la materia ni en la piel entera (una sombra por chunk sería ruido).
- **Post-proceso por volumen**: bloom con umbral alto (que florezcan solo
  brasas, ojos, vidrio caliente), viñeta nativa (sustituye a la IMGUI), color
  grading anclado a `TinteGlobal`/`TintePlano` (la mudanza sigue siendo el
  único punto que se anima).

**Parámetros de partida:**

| Parámetro | Valor de partida | Por qué |
|---|---|---|
| Luz global | 0.55–0.70 de intensidad, color ceniza cálida | que sin ninguna luz puntual el taller siga legible |
| Luces con causa visibles a la vez | ≤ 8 (≤ 12 en co-op) | presupuesto y legibilidad |
| Radio de una brasa / fuego | 6–14 celdas, caída cuadrática | el calor se ve desde el otro lado del taller pero no lava la escena |
| Ojos-lámpara y brote | radio 2–3 celdas, intensidad baja, BRASA | son alma, no linterna |
| El vano | luz fría (AZUL MUDANZA desaturado), direccional desde fuera | dice "hay un mundo ahí" y separa dentro/fuera |
| Materia (la sim) | recibe luz ATENUADA: mezcla ≤ 15 % entre color plano y color iluminado, o ninguna | **P1**: si la arena cambia de color bajo una brasa, la información de juego se rompe |
| Roca, máquinas, fondo | luz completa con normales | es donde la luz paga |
| Sombras 2D | suaves, cortas (≤ 1.5 celdas), solo de muñeco y máquinas | sombra de contacto, no teatro |
| Bloom | umbral ≥ 1.0 en HDR, intensidad baja | solo emisivos |
| Presupuesto | ≤ 1 ms por frame en el mín-spec que se defina; si no, se recorta luces, no materia | R123 §5 |
| Día/noche | opcional, atado a hitos del fondo evolutivo, nunca a un reloj real | que el tiempo del mundo sea el progreso |

**Límites (lo que la luz NO puede hacer):**

- No puede volver ilegible un material (P1) ni romper la validación por
  daltonismo de la paleta (P6): se valida en claro y oscuro como en R105.
- No puede convertirse en "luces de escena" sin causa (P4).
- No mueve la cámara ni la talla: la profundidad que aporta es pintura (1.4).
- No se aplica a la grilla como sombreado por celda (la lección de R66–68:
  a 1 téxel/celda todo anillo se lee blocky).

**Criterios de muerte de la prueba** (heredados de R123 §5): si la sim
iluminada lee peor que sin luz, la luz sale de la grilla; si el 2D Renderer
rompe shaders en build sin arreglo en una hora, se aplaza; si a 80 celdas no
se ve diferencia, se archiva con captura.

**Lo que empuja:** la luz con normales empuja el acabado hacia "no pixel". El
acabado sigue ABIERTO por decreto (§6.2), pero esta es la primera evidencia a
favor de un lado; se registra.

---

## 4. EL TERRENO — [DECIDIDO]

4.1 **La piel de roca es la dirección del terreno natural.** Conclusión de la
R124 con captura y veredicto de Cesar ("es hermoso y no canta"): el terreno
puede sentirse claramente distinto de la materia granular sin tocar la sim,
y **excavar resulta mucho más satisfactorio**. Queda como dirección lo que
la prueba fijó:

- Solo `Stone`; el resto de sólidos sigue recto (P2).
- Campo por esquina (solidez media de 4 celdas), umbral ≈ 0.49: muros de una
  celda enteros, chaflán/filete de media celda, **la silueta nunca se aleja
  más de media celda de la colisión**.
- Cuatro capas ordenadas (canto + halo, relleno con masa interna, bandas
  suelo/pared/techo + tinta, decoración), debajo de la sim (−6).
- Actualización por hash de chunk (0.035 ms/chunk medido).

4.2 **Lo que la piel deja pendiente (no urgente):** el nivel base de juego (mi
voto: 2 o 3, con la decoración procedural apagada hasta tener sprites); la
sustitución de la textura procedural por 2–3 texturas pintadas mezcladas
por el mismo canal de distancia al aire; sprites de decoración (estalactitas,
raíces, musgo, restos de máquina) colocados con la misma lógica de
tramo+hash; antialias de la tinta; qué pasa con la sillería de `SimRenderer`
(candidata a quedarse como acabado de los sólidos FABRICADOS, no a borrarse).

4.3 **El cincel no cambia**: talla y rellena celdas; la piel lo sigue. La
excavación NO produce materia (regla de hierro R60: las ruinas dan
recipientes y herramientas, nunca materiales a granel; la roca tallada
tampoco). Esto es lo que impide que §5.1 se convierta en un juego de minar.

---

## 5. HIPÓTESIS FUTURAS — [HIPÓTESIS] (dirección registrada, NO backlog)

5.1 **Excavar como verbo de espacio, no de recurso.** Si tallar se siente
bien, el laboratorio puede CRECER excavando: hacer sitio para la siguiente
máquina, abrir un pasillo, ganar una bóveda. Tres ideas hermanas, todas sin
número ni fecha:

- *Descubrimiento*: lo heredado está ENTERRADO (GDD §0: las máquinas se
  desentierran y se reparan). Excavar hacia una silueta que asoma en la roca
  es la forma natural de "encontrar" una máquina — sin mapa, sin marcador.
- *Estabilización de espacio*: un techo excavado demasiado ancho podría
  ceder (derrumbe controlado), pidiendo pilares o vigas heredadas; da a la
  excavación una regla sin convertirla en combate. Cuidado: es sistema nuevo;
  hoy es solo una frase.
- *Crecimiento del laboratorio*: el taller como algo que se gana con el
  cincel, cámara a cámara, en vez de un plano fijo. Casa bien con la regla de
  ritmo del roadmap (cada material nuevo pide su sitio).

Guardas para el día que se toque: (a) la excavación jamás rinde material
(4.3); (b) no compite con el verbo central, VERTER — hace sitio para
verter; (c) si empieza a parecerse a Dome Keeper/Terraria (minar por minar),
se para.

5.2 **El prólogo contenido.** Al sentirse bien trabajar en espacios
excavados pequeños, el prólogo podría empezar en una cámara más cerrada que
la caverna actual (300–468 celdas de ancho). Un pequeño derrumbe o
transformación posterior serviría de **momento de apertura**: se revela el
vano del oeste y, con él, el fondo evolutivo (5.3). Observaciones para
cuando se estudie: el prólogo YA tiene un derrumbe cinematográfico (el que
abre la gotera de LODO) — podría ser el mismo beat, no uno nuevo; el beat
«VEN.» valida desplazamiento real y necesita espacio suficiente para
sentirlo; una cámara de ~50×30 celdas cabe entera en la vista por defecto
(80 celdas), lo que la haría legible sin mover la rueda. Nada de esto
modifica `PLAN_PROLOGO_CAP2.md` hoy.

5.3 **El fondo evolutivo** sigue aprobado conceptualmente (R123 §4) y debe
convivir con esta dirección: vive en el Nivel 0 (P5), lo ilumina la luz fría
del vano (P4), sus hitos son los materiales de ORO entregados, con la regla
del espejo retrasado (nunca por delante de tu hito), siluetas por debajo de
L≈0.35, cero agentes. Su primera aparición natural es el momento de apertura
de 5.2. Su contenido concreto (especies, homínidos, Denisova) sigue sin
decidirse (§6.4).

5.4 **Una sola gramática.** Terreno orgánico (lo natural) + ruinas rectas
(lo heredado) + materia cuadrada (lo vivo) + luz con causa (el fuego que el
jugador enciende) + una ventana que registra lo que enseñó (el mundo de
fuera). Cinco cosas, un dibujo: ese es el objetivo de la presentación final,
y es comunicable en una toma fija de tráiler.

---

## 6. DECISIONES DELIBERADAMENTE APLAZADAS — [APLAZADO]

6.1 **La locomoción del muñeco** (vuelo / a pie / salto / caída / combinación;
modos F6 A·B·C de `DISENO_MOVIMIENTO.md`). **No se cierra ahora.** El
terreno nuevo, la excavación y el entorno final pueden cambiar qué movimiento
se siente natural; primero se eleva el escenario, después se decide con el
espacio real delante. Mientras tanto: los tres modos siguen jugables, la
telemetría sigue registrando, y el decreto GDD §0 («LEVITA, no camina») se
mantiene TAL CUAL escrito — no se enmienda hasta decidir, y cuando se decida
se enmienda explícitamente, no de contrabando. *Qué debe existir antes de
decidir:* la piel en su nivel base, la primera luz con causa, y al menos una
cámara excavada del tamaño del prólogo contenido para jugar 10 minutos en
cada modo.

6.2 **El acabado pixel / no pixel** sigue abierto por decreto. Evidencia
acumulada a favor de "no pixel": la piel de roca y la luz con normales.
*Antes de decidir:* ver el muñeco en su variante pixel junto a la piel y la
luz, en la misma captura.

6.3 **El nivel base de la piel** (2 vs 3 vs 4) y la sustitución de su
textura y decoración. *Antes de decidir:* 10 minutos de juego de Cesar
tallando y vertiendo con F7.

6.4 **El contenido del fondo evolutivo** (qué especies, qué siluetas, qué
hitos exactos). *Antes de decidir:* la lista cerrada de encargos de ORO de la
Era I (`ROADMAP.md` §2) y la ventana con planos construida.

6.5 **Cuándo cambiar al 2D Renderer.** Es un cambio de proyecto, no de una
escena. *Antes de decidir:* la tarde de prueba en una copia del RP asset con
build verificada.

6.6 **El prólogo contenido** (5.2). *Antes de decidir:* que exista la piel
en nivel base y que Cesar haya jugado una cámara pequeña con el verbo VERTER
y con «VEN.».

---

## 7. ORDEN SUGERIDO (secuencia, no calendario, no tareas)

1. Cesar juzga la piel (6.3). 2. Se eleva el escenario: piel en nivel base +
prueba de luz con causa de una tarde (§3, criterios de muerte incluidos) +
una cámara excavada de muestra. 3. Con ese escenario delante: locomoción
(6.1). 4. Después: prólogo contenido (5.2) y ventana con planos, y solo
entonces el fondo evolutivo (5.3) sobre los encargos de ORO. **En paralelo y
por delante de todo lo visual: cerrar la Era I jugable (ROADMAP §2, "falta
para cerrar")** — la dirección visual se prueba en tardes acotadas; el
contenido es lo que cierra la demo.

---

## 8. RIESGOS Y GUARDAS

- **El pivote silencioso** (R123 §6): luces → "una máquina en 3D para ver" →
  cámara con perspectiva. Guarda: la regla de oro 1.4 y la tabla de `Capas`.
- **La luz que rompe la materia**: la tentación de iluminar la sim "porque se
  ve bonito". Guarda: P1 y el tope del 15 % (§3).
- **El placeholder que fija el gusto**: las estalactitas de triángulo y el
  musgo de polígono pueden acabar "gustando" por costumbre. Guarda: apagar
  la decoración procedural en el nivel base hasta tener sprites.
- **Excavar que se come al verter**: si el cincel se vuelve más divertido que
  el frasco, el juego cambia de tesis sin que nadie lo decida. Guarda: 4.3 y
  las tres guardas de 5.1; medir en telemetría el tiempo con cincel vs con
  frasco.
- **El prólogo contenido que asfixia**: una cámara pequeña con el tutorial de
  desplazamiento y la primera cascada puede leerse como caja. Guarda: la
  cámara cabe entera en 80 celdas y el derrumbe de apertura llega dentro del
  prólogo, no después.
- **Aplazar la locomoción sin fecha**: cada ronda que pasa construye más
  sobre la física actual (vuelo por decreto). Guarda: 6.1 dice exactamente
  qué debe existir para decidir; cuando exista, se decide en esa misma
  semana.
- **Rendimiento en co-op**: la piel se reconstruye en el espejo del invitado
  por la misma ruta de hash (no probado en vivo); la luz suma renderers.
  Guarda: presupuesto de §3 y una partida co-op de 2 minutos en cada prueba.
- **Scope frente al Next Fest**: todo lo de §5 es hipótesis; nada entra en el
  backlog de la demo por esta V2.

---

## 9. ENLACES

`GDD_TEN_THOUSAND_YEARS.md` §0 (decreto; «LEVITA» intacto, ver 6.1) ·
`DIRECCION_DE_ARTE.md` (personaje, paleta, 3 perillas, talla y cámara) ·
`EVALUACION_2D_VS_2_5D.md` (R123, archivo histórico: análisis A/B/C, fondo
evolutivo, experimento "la ventana iluminada") · `PRUEBA_PIEL_DE_ROCA.md`
(R124: qué se hizo, limitaciones, arte final) · `DISENO_MOVIMIENTO.md` (F6,
telemetría) · `PLAN_PROLOGO_CAP2.md` (el cincel en el prólogo) ·
`ROADMAP.md` §2.7 · `Game/Capas.cs` · `Game/PielDeRoca.cs`.
