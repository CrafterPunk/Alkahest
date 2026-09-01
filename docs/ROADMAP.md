# TEN THOUSAND YEARS — ROADMAP
### El plan del juego completo: eras, materiales, silos, tiempos y promesa
*(v1, R96 — escrito para lectura externa. Números de contenido auditados contra el
código; tiempos de desarrollo y juego son ESTIMACIONES honestas, no compromisos.)*

---

## 0. Qué es el juego (para quien llega de afuera)

**TEN THOUSAND YEARS** — *Rebuild human knowledge from mud, fire, and observation.*
Un falling-sand determinista (cada grano simulado) donde 1-4 jugadores reconstruyen
la cultura material humana: desde el barro y el fuego hasta —con los años de
desarrollo— la química que cierra la era preindustrial. Cada material del juego es
REAL, con su nombre real y su mini-reseña ("esto existió, así se hizo"); la única
magia permitida comprime tiempo y trabajo, nunca inventa materia. El conocimiento
que ganas jugando es conocimiento del mundo real: si todo desapareciera mañana,
sabrías por dónde empezar.

- **Jugadores:** 1-4 en co-op (óptimo 2-3; el multi POC ya corre sobre Steam).
- **Sesión tipo:** experimentar, descubrir, dominar una familia de materiales,
  comprar la siguiente máquina. Sin deadlines: los encargos jamás expiran.
- **Hoy (agosto 2026):** prólogo cinematográfico completo (~3 min) + demo de la
  Era I en construcción, apuntando a Steam Next Fest.

---

## 0.5 EL MUNDO (decreto R107)

Post-apocalipsis de **ruinas amables**: la civilización anterior dejó máquinas
rotas que se desentierran y REPARAN — por eso el jugador «tiene» depósitos de
vidrio el día uno sin poder fabricar vidrio. Regla de hierro R60: lo heredado
se repara, jamás se desguaza (las ruinas dan recipientes y herramientas, nunca
materiales a granel). La seed es tu NODO habilitador (qué recursos tenía cerca
tu protocivilización); lo que falta se pide por el tablón y llega en caravana.
Personaje: el muñeco de remiendos (docs/DIRECCION_DE_ARTE.md).

## 1. EL MAPA DE LAS CINCO ERAS (resumen ejecutivo)

| Era | Nombre | Materiales nuevos (acum.) | De ellos ORO* | Silos/refill nuevos | Dev estimado | Juego estimado (acum.) |
|---|---|---|---|---|---|---|
| I | EL FUEGO Y EL MINERAL | ~30 jugables (de 55 nombrados) | 12 | agua, arcilla (hechos) + turba, arena, caliza, sal (comprables) | CIERRE: ~1 mes | 3-5 h |
| II | EL HILO Y EL METAL | +12 (≈42) | +5 (17) | fibra (junco) + mena de cobre | 2-3 meses | 8-11 h |
| III | EL GRANO Y LA BESTIA | +15 (≈57) | +8 (25) | grano; la fauna es SISTEMA, no silo | 3-4 meses | 14-19 h |
| IV | EL BRONCE Y LA RUEDA | +10 (≈67) | +5 (30) | estaño; hierro (veta profunda) | 2-3 meses | 19-26 h |
| V | LA QUÍMICA PACIENTE | +12 (≈79) | +5 (35) | azufre/salitre (la pólvora pide los dos) | 3-4 meses | 25-34 h |

\* ORO = nombres que la mayoría de la gente CONOCE de la vida real (cerámica,
carbón, cuerda, pan, bronce, jabón…): el apalancamiento de recordación. La PLATA
(suenan sin dominarse: clínker, lejía, salmuera…) y la JERGA honesta (barbotina,
bizcocho… googleables, con reseña que confiesa) completan cada era como PROFUNDIDAD.

**Total del arco: ~79 materiales jugables (~110 nombrados con estados intermedios),
~12-15 meses de desarrollo al ritmo actual, ~25-34 horas de primera partida co-op
usando solo USO + DESCUBRIMIENTO + CRECIMIENTO** (la remodelación/expresión del
taller, que multiplica horas, queda fuera de esta cuenta a propósito).

**La promesa, medida:** el canon histórico preindustrial que un jugador culto puede
exigir ronda los ~45 descubrimientos de materiales. Era I cubre ~60% del peso de su
tramo; el arco de 5 eras cubre **~92-95% del canon completo** — el 95% de la promesa
se alcanza al cerrar la Era V. Lo que queda fuera está listado en §4 y es
deliberado.

---

## 2. LAS ERAS EN DETALLE

### ERA I — EL FUEGO Y EL MINERAL *(el entregable actual)*
La era de la pirotecnia: todo lo que el fuego le hace a la tierra molida.
- **Contenido:** las 5 bases (arena de sílice, arcilla, caliza, turba, sal) × sus
  estados + 6 cruces reales (mortero, clínker, hormigón, vidrio de botella, lejía,
  esmaltado) + los clásicos (agua, fuego, humo, ceniza, brea…). La escalera del
  fuego ES el juego: brasas → turba → ceniza → carbón, cada combustible desbloquea
  el siguiente peldaño térmico; el techo declarado es LA CERÁMICA.
- **ORO (12):** agua, fuego, humo, ceniza, arcilla, adobe, cerámica, carbón, sal,
  cal, vidrio, mortero (+hormigón, brea en la frontera).
- **Silos:** agua y arcilla YA (el prólogo los entrega); turba, arena, caliza y sal
  entran como SILOS COMPRADOS en el tablón del Trueque, uno por vez — nada suelto
  el día 1, el orden de compra ES la progresión. (Todos los nuevos son polvos: la
  máquina paramétrica ya existe.)
- **Extra Pareto GRATIS: LOS PIGMENTOS** — negro=carbón, ocre=arcilla, blanco=cal;
  solo piden el verbo PINTAR sobre piedra. Encienden el pilar "observación"
  (registrar) y son el guiño de Altamira.
- **Falta para cerrar:** repreciar el Trueque (hoy tiene un deadlock conocido),
  los 4 silos comprables, 3-4 encargos formulados en ORO (adobe → vasija → cal →
  redoma), pigmentos, seed de autor filtrada (las leyes sorteadas no deben
  contradecir los nombres reales). **Dev: ~1 mes. Juego: 3-5 h.**

### ERA II — EL HILO Y EL METAL
Las dos revoluciones gemelas que el registro subestima: atar y fundir.
- **Contenido:** LA FIBRA como material de vocabulario (cuerda, cestería, tela,
  mecha — estrena el eje mecánico TENSIÓN: nuestras bases son masa que cae, la
  fibra ata) + EL COBRE (malaquita de veta → cobre; martillado y fundido) + oro
  nativo + pigmentos avanzados. La quincha (fibra+arcilla) y el calafate
  (fibra+brea) cruzan eras hacia atrás.
- **ORO (+5):** cuerda, tela, canasta, cobre, oro.
- **Silos:** fibra (junco de la veta); la mena de cobre entra por VETA expuesta +
  silo comprable tardío.
- **Dev: 2-3 meses** (la fibra pide física de segmentos o abstracción honesta —
  el riesgo técnico de la era). **Juego: +4-6 h.**

### ERA III — EL GRANO Y LA BESTIA
El tiempo biológico entra al juego: lo que crece y fermenta.
- **Contenido:** grano/semilla, levadura, pan, cerveza, vino, vinagre; la FAUNA
  como sistema (no como silo): cuero, lana, sebo, cera, miel, leche/queso. El
  vinagre abre la puerta ácida de la Era V; el sebo+lejía = JABÓN (el cruce
  estrella); la cera espera al molde.
- **ORO (+8):** pan, cerveza, vino, cuero, lana, miel, cera, queso.
- **Dev: 3-4 meses** (seres + tiempo biológico: el sistema más nuevo del juego).
  **Juego: +6-8 h.**

### ERA IV — EL BRONCE Y LA RUEDA
La metalurgia seria y el registro que viaja.
- **Contenido:** estaño → BRONCE (la primera aleación: el descubrimiento de que
  dos débiles hacen un fuerte), HIERRO (veta profunda, pide carbón a tope y
  fuelle), cera perdida (cera III + cobre II = moldes: el pago diferido), papiro/
  papel temprano (fibra II), torno y rueda como máquinas, tintes.
- **ORO (+5):** bronce, hierro, acero (asoma), papel, rueda.
- **Dev: 2-3 meses.** **Juego: +5-7 h.**

### ERA V — LA QUÍMICA PACIENTE
Donde la alquimia se vuelve química y el juego honra su premisa completa.
- **Contenido:** jabón (sebo+lejía), destilación (el alambique YA existe en el
  código), alcohol, ácidos (del vinagre a los minerales), AZUFRE y SALITRE →
  PÓLVORA, porcelana (el verdadero techo de la cerámica), vidrio soplado, acero
  al crisol.
- **ORO (+5):** jabón, alcohol, pólvora, porcelana, acero.
- **Dev: 3-4 meses.** **Juego: +6-8 h.**

---

## 2.5 LA CADENA DEL FUEGO (R105 — temperaturas, estructuras y fidelidad)

La humanidad subió la temperatura CONSTRUYENDO ESTRUCTURAS; cada una desbloqueó
materiales que la anterior no podía tocar. El juego calca la cadena peldaño a
peldaño. La escala interna (raw 0–255; el motor lee °C = raw×2 − 120, o sea
−120…390 °C) es una COMPRESIÓN de la real: la magia comprime tiempo y
temperatura, jamás inventa materia — **la correspondencia es por peldaño, no por
número**. Versión visual (con las estructuras dibujadas): el artifact "Las Cinco
Eras" (misma URL de siempre).

| # | Estructura | Época | °C reales | Desbloquea (fidelidad) | En el juego | Estado |
|---|---|---|---|---|---|---|
| 1 | LA FOGATA (fuego abierto) | ~400.000 años | 400–700 (picos ~900) | adobe 85%, pigmentos, brea | brasas ≈100 · turba raw 124 | HECHO — Prólogo+Era I |
| 2 | EL HOYO DE COCCIÓN | figurillas ~26.000 a.C.; vasijas ~14.000 a.C. | 600–900 | cerámica 90%, bizcocho 90% | ceniza raw 136 | HECHO — Era I |
| 3 | EL HORNO DE TIRO + LA CALERA | Mesopotamia/Egipto ~4.000 a.C. | 900–1.100 | cal 88%, esmaltado 85%, mortero/hormigón 88%, vidrio de botella, gres 55% (confiesa) | carbón raw 158–190; arcilla: adobe 180 · calcinado 188 · CERÁMICA 205 (techo declarado) | HECHO — Era I |
| 4 | EL CRISOL Y LOS FUELLES | Balcanes/Timna ~5.000 a.C. | 1.100–1.200 (el cobre funde a 1.085) | cobre, oro nativo, vidrio pleno (plan ≥85) | peldaño nuevo POR DISEÑAR; el FUELLE como máquina | PLAN — Era II |
| 5 | EL HORNO DE LUPIA (bloomery) | Anatolia ~1.200 a.C. | 1.200–1.300 (el hierro funde a 1.538: se reduce SIN fundir) | bronce, hierro, cera perdida (plan ≥85) | carbón a tope + fuelle | PLAN — Era IV |
| 6 | EL HORNO DRAGÓN | China s. II a.C.→ | 1.250–1.400 | PORCELANA (plan ≥85); el gres se redime | la redención del 55% | PLAN — Era V |
| 7 | EL ALTO HORNO / CRISOL DE ACERO | China ~s. V a.C.; Europa s. XIII; wootz ~300 a.C. | 1.400–1.600 | hierro colado, ACERO (asoma) | el borde del arco: asoma, no se domina | PLAN — Era V (frontera) |

Notas de honestidad: (a) los valores raw de la Era I son del código vivo
(Universe.cs: bandas de extracción {106,124,136,148,158}, combustibles 165–190,
umbrales de arcilla 180/188/205); (b) los peldaños II–V son promesa de diseño —
sus números exactos se cablean cuando cada era entra a desarrollo; (c) las
temperaturas históricas son rangos de consenso arqueológico, suficientes para el
imaginario colectivo, no para un paper; (d) el pacto de fidelidad (≥85 nombre
real / 60–85 confiesa / <60 prohibido) gobierna cada celda de la columna
"Desbloquea".

## 2.6 CÓMO ANIMAR AL MUÑECO BARATO (investigación R117, ago. 2026)

Informe completo: `docs/INVESTIGACION_ANIMACION_2026.md` (14 dimensiones por
candidato, licencias de código/pesos/outputs separadas, fuentes primarias).
Infografía hermana de las Cinco Eras: https://claude.ai/code/artifact/4f710738-48b6-4131-b3bd-c29bcd04cdb5

Ranking de pipelines: (1) HÍBRIDA para validar en una tarde — PNG + video
del celular → Wan2.2-Animate / Wan-Animate-2 (Apache-2.0) → SAM 2 → sheets;
(2) 3D→retarget→prerender, la referencia mejorada con UniRig (MIT) +
HY-Motion/Puppeteer, la fábrica de volumen para emotes y skins; (3) 100% 2D
con Unity 2D Animation + Sprite Library como red de seguridad; (4) AniSora /
Wan FLF2V para micro-reacciones; (5) mocap casero (Puppeteer / SAM 3D Body).
Los emotes sociales (acordes, duetos, ritual del maestro) van DESPUÉS de las
dos máquinas y dependen de la ruta 2.

**Estado R119:** balance completo de la primera noche de producción y plan por
camino en `docs/PLAN_ANIMACION_R119.md` (leer ese antes de decidir nada de
animación); la mecánica de movimiento (¿volar, caminar, ambos?) tiene su propia
investigación ABIERTA en `docs/DISENO_MOVIMIENTO.md`.

**Estado R118:** la ruta 1 está VALIDADA y ya entra al juego. Arnés fuera del
repo (`C:\JuegosUnity\UnityAI_Test\Arnes_Animacion\`, LEEME.md): Mixamo →
Blender (video de pose, 15 s) → Wan Animate 2 local (14–24 min por gesto de
5 s con el modelo completo int8) → `postproceso.py` (alfa + ciclo + hoja +
manifiesto) → `HojaDeCuadros` en Unity. Primer gesto en el juego: `caminar`.
Siguiente: reposo (Standing Idle 03), recoger, levantarse; luego medir el
GGUF Q5 contra esta vara de calidad; luego Wan 2.2 I2V/FLF2V (por prompt y
loops B2) con los mismos scripts.

## 2.7 ¿2D A FONDO O 2.5D? (evaluación R123, sep. 2026)

Informe: `docs/EVALUACION_2D_VS_2_5D.md`. Infografía «La Ventana Iluminada»:
https://claude.ai/code/artifact/1cd1c2ec-5cb4-453b-a2b7-577b67172dbe

Recomendación: **quedarse en 2D y explotarlo (opción A) con la mitad barata
del falso 2.5D (2D Renderer + Light2D con normales donde hay Fire real, tres
planos de parallax)**; la C (Z real, máquinas 3D) queda archivada como
no-camino salvo necesidad demostrada en la Era III. Regla de oro: la cámara es
ortográfica y lateral, el plano de juego es la grilla, toda profundidad es
pintura. El **fondo evolutivo por hitos** (la ventana del vano que registra los
materiales de ORO entregados; regla del espejo retrasado: nunca por delante de
tu hito) se recomienda DESPUÉS de la ventana con planos y de cerrar los encargos
de ORO de la Era I. Experimento mínimo de una tarde («la ventana iluminada»)
descrito en el informe §5; nada de esto se implementa hasta que Cesar lo lea.

**R124 — la prueba de terreno ya existe:** `docs/PRUEBA_PIEL_DE_ROCA.md`
(piel de marching squares SOLO sobre la roca madre, sim intacta; F7 rota los
niveles en juego, Ctrl+F7 talla una cueva de muestra). Veredicto de Cesar
pendiente; es la información nº 1 de la lista del informe §7.

**R125 — DIRECCIÓN V2 (la foto tras la piel de roca):**
`docs/DIRECCION_V2_TERRENO_LUZ_ESPACIO.md`. Decidido: 2D bien explotado
como arquitectura (sim, precisión y cámara lateral intocables), presentación
final CON iluminación 2D y profundidad pintada (parámetros y límites en su
§3), la piel de roca como dirección del terreno natural, una sola gramática
visual (natural orgánico · construido recto · materia cuadrada · luz con
causa · ventana que registra). Hipótesis registradas (no backlog): excavar
como verbo de espacio, prólogo contenido con derrumbe de apertura, fondo
evolutivo en el Nivel 0. Aplazado a propósito: la locomoción (F6 sigue
abierto; «LEVITA» del GDD §0 intacto hasta decidir), el acabado pixel, el
nivel base de la piel. La evaluación R123 queda como archivo histórico.

## 3. EL PRINCIPIO RECTOR DEL RITMO (por qué "lento" es correcto)

Cada material nuevo debe pasar por RECORDACIÓN (su nombre real se fija jugando),
DIVERSIÓN (un verbo que se disfruta) y EXPERIMENTACIÓN (combina con lo anterior)
antes de que entre el siguiente. Por eso cada era añade 10-15 materiales, no 40:
la promesa no es un catálogo, es que el jugador SE QUEDE con el conocimiento.
Regla de diseño: los encargos se formulan SIEMPRE en palabras de ORO; la jerga es
descubrimiento intermedio con ficha.

## 4. LO QUE QUEDA FUERA (deliberado, con nombre)

- **La era industrial** (vapor-máquina, electricidad, plásticos): otro juego, u
  otra década del juego. El arco termina donde la química paciente cede a la
  fábrica.
- **Seda, tintes complejos (púrpura), vidrio óptico, imprenta como sistema**: el
  último 5% del canon — caros por pieza, apalancamiento bajo.
- **La remodelación/expresión** (decorar, rediseñar el taller como fin): EXISTE
  (cincel + mudanza ya lo permiten) pero se excluye de esta cuenta de horas a
  propósito — es multiplicador, no contenido.
- **Lo alquímico sorteado** (modo caótico): vive en paralelo desde ya, con nombres
  fantasía y bautizo; no consume este roadmap.

## 5. TAMAÑO, PÚBLICO Y PRECIO (estimación de mercado, no asesoría financiera)

- **Para cuántos:** 1-4 co-op; el diseño respira mejor a 2-3 (el "intendente" del
  Trueque nace solo en co-op). Solo también funciona: nada exige segundo jugador.
- **Duración:** demo Next Fest 15-20 min; Era I completa 3-5 h; arco completo
  25-34 h de primera partida sin contar remodelación ni modo caótico (que es
  rejugabilidad por seed, encima de esto).
- **Comparables de mercado:** Noita $19.99 · Potion Craft $17.99 · Core Keeper
  $19.99 (1-8 co-op) · Terraria $9.99 legacy.
- **Recomendación:** demo GRATIS (Next Fest) → Early Access con Era I-II a
  **$14.99** → subir a **$19.99** al cerrar la Era IV → precio pleno **$19.99-24.99**
  con la V si el pulido audiovisual acompaña. La honestidad del contenido real
  ("aprendes cosas verdaderas") es el diferenciador de marketing: ningún
  comparable la tiene.

## 6. SUPUESTOS Y RIESGOS (para que el externo no compre humo)

1. Tiempos de dev asumen el ritmo actual (director + agente, rondas diarias) y se
   COMPRIMEN si se suma el segundo dev (la escenificación del editor está
   planificada para eso).
2. La FIBRA es el riesgo técnico mayor (física nueva); si la abstracción honesta
   no divierte, la Era II se re-ordena alrededor del cobre.
3. La FAUNA (III) es el sistema más caro; su recorte de emergencia es empezar la
   era por el grano/fermento (sin seres) y sumar la bestia después.
4. La seed de autor filtrada (leyes vs nombres reales) es DEUDA ACTIVA de la Era I:
   sin ella, la promesa se contradice a sí misma en pantalla.
