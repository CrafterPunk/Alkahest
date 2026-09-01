# ¿SEGUIR EN 2D O ABRIR LA PUERTA AL 2.5D? — evaluación estratégica y técnica (R123)

*(Pedido de Cesar, 1/09/2026: «Antes de meternos a fondo con el fondo y los
tiles, evaluar en serio si conviene seguir profundizando el 2D actual o abrir
la puerta a 2.5D». Este documento NO implementa nada y NO destapa decisiones
selladas: se apoya en el decreto del mundo (GDD §0), en `DIRECCION_DE_ARTE.md`
(§1 convivencia de capas, §3.5 talla y cámara), en la tabla `Capas.cs` (R66) y
en las lecciones del código vivo. La prueba de Marching Squares/terreno
continuo que Cesar hará aparte queda enlazada en §7, no juzgada aquí.)*

Infografía hermana «La Ventana Iluminada» (capas, cámara, terreno, partículas,
fondo evolutivo interactivo): https://claude.ai/code/artifact/1cd1c2ec-5cb4-453b-a2b7-577b67172dbe
(fuente en `docs/ref/infografia_2d_vs_2_5d.html`).

---

## 1. RECOMENDACIÓN (en una frase y en un párrafo)

**Quedarse en 2D y explotarlo a fondo: la opción A, con la mitad barata de la
B (luz 2D con normales y tres planos de parallax) como único préstamo. La C
—profundidad real, máquinas 3D, cámara con Z— NO, y no por miedo: porque
cambia los problemas que tenemos por otros más caros y ataca justo los dos
pilares que hacen único al juego (materia real en una grilla que se ve grano a
grano; verter con precisión).**

El párrafo: los síntomas visuales que hoy incomodan (el labio frontal que se
leía «blocky», el mosaico de alfa entre texturas Point, el personaje que flota
sobre una roca sin volumen, la pared plana detrás de todo) no son síntomas de
que el 2D se haya quedado corto; son síntomas de que el juego todavía usa una
fracción de la caja de herramientas 2D estándar. En el proyecto **no hay una
sola luz** (el renderer activo es el Universal 3D de URP, no el 2D Renderer, así
que `Light2D` ni siquiera está disponible), **no hay `SpriteMask` ni
`SortingGroup`**, hay **un solo plano de parallax** (el muro, al 8 %), ninguna
máquina tiene **mapa de normales**, y el post-proceso (bloom para las brasas y
los ojos-lámpara, viñeta, grading) vive en IMGUI a mano. Cada una de esas cosas
es una tarde, no una arquitectura. La C, en cambio, obliga a reautorar todo el
arte de máquinas (hoy `MaquinariaSprites.cs` genera 3.435 líneas de texturas
procedurales 2D, «cero assets») como mallas, a resolver cómo una grilla de
1 téxel/celda convive con luces y sombras 3D, y a defender la precisión de
verter en una cámara donde el plano de juego ya no coincide con la pantalla.
Ese es el intercambio: el 80 % del beneficio visual de la C se consigue en la B
acotada, por menos del 15 % de su coste y sin tocar sim, red ni gameplay.

---

## 2. COMPARACIÓN A / B / C

### 2.0 Qué es cada una, en términos de ESTE proyecto

- **A · Mantener 2D y profundizar.** Terreno: la sim sigue siendo 1 celda =
  1 téxel (Point), con la piel de Marching Squares SOLO sobre la piedra madre
  estática si la prueba de Cesar la avala. Sorting: la escalera de `Capas.cs`
  (6 niveles, R66) completada con `SortingGroup` por máquina y `SpriteMask`
  para el vidrio de recipientes. Máquinas back/front: el sándwich
  `MaquinaFondoInterior (-8) → Simulacion (-5) → MaquinaFrente (35)` que ya
  vive en el Crisol, extendido al resto (backlog nº 4 de `ESTADO.md`). Fondo:
  el `WorkshopBackdrop` partido en planos con siluetas por hitos. Ninguna luz.
- **B · Falso 2.5D agresivo.** Todo lo de A más: **2D Renderer de URP** con
  `Light2D` (global + puntuales: fuego del Maestro, brasas, ojos), **mapas de
  normales** en máquinas y personaje (el pipeline 3D del muñeco ya los produce
  gratis; para las máquinas procedurales se derivan de la propia altura),
  **3–4 planos de parallax** (0.03 / 0.08 / 0.15 / frente 1.10), sombras
  proyectadas 2D (`ShadowCaster2D`) en máquinas y arquitectura, partículas
  ambientales delante del personaje (Nivel 5 `Foreground`, ya reservado),
  y post-proceso de URP (bloom, viñeta, grading por volumen) en lugar del IMGUI.
  La cámara sigue ortográfica y lateral; el plano de juego sigue siendo la
  grilla; nada cambia en sim ni en red.
- **C · 2.5D real.** Cámara con Z (ortográfica o perspectiva suave), máquinas y
  escenario como mallas 3D iluminadas, gameplay confinado a un plano
  (Trine/Little Nightmares/Ori 3D), fondo distante geométrico, luces y sombras
  reales. La sim sigue siendo una grilla 2D: habría que **proyectarla** como
  textura sobre un quad en el plano de juego dentro de una escena 3D y
  **fingir** cómo la luz 3D la toca (una textura sin normales no se ilumina),
  o convertir la grilla a geometría por frame (marching cubes de una capa),
  lo que a 768×288 a 30 Hz es posible pero caro y ruidoso.

### 2.1 La tabla (nota /10 = qué tan FAVORABLE es la opción en ese criterio; 10 = mejor)

| Criterio | A · 2D profundo | B · falso 2.5D | C · 2.5D real |
|---|:-:|:-:|:-:|
| Coste técnico | **9** — tardes sueltas sobre `Capas.cs`; nada nuevo en el motor | **7** — cambiar de renderer (una tarde + revisar los shaders «prohibidos» del playtest 2), normales, sombras; 1–2 semanas repartidas | **2** — nueva escena, cámara, materiales, proyección de la sim, colisión visual/física divergente; 2–4 meses antes de recuperar lo que hoy hay |
| Coste artístico | **8** — más sprites del mismo tipo (siluetas, cantos, vidrios); todo entra por el flujo de hornear PNG | **6** — cada sprite iluminado pide normales; el arte procedural las deriva solo, pero el arte a mano (fondo evolutivo) pide una capa más | **1** — TODAS las máquinas rehechas como mallas + UV + texturas; el personaje 2D convive mal con máquinas 3D (o se va a 3D también, lo que contradice §3 de la dirección de arte: «3D como herramienta, no arte final») |
| Código que sobrevive | **10** — el 100 % | **9** — el 100 % del gameplay; retoques en `SimRenderer`/`WorkshopBackdrop`/`MaquinariaSprites` (materiales) | **4** — sim, red, química y directores sobreviven (son datos); mueren o se reescriben `SimRenderer` (1.938 l.), `WorkshopBackdrop` (1.654 l.), `MaquinariaSprites` (3.435 l.), colocaciones y cámaras de los directores, el cincel/mudanza que miden en píxeles de pantalla |
| Networking | **10** — el espejo RLE de `mat[]` a 5 Hz ni se entera | **10** — ídem (la luz es local) | **6** — sigue funcionando (la verdad es la grilla), pero las réplicas de máquinas (hoy siluetas, pendiente su visual completo) pasan a ser mallas + transformaciones + estados de luz a replicar |
| Simulación celular | **10** — intacta y PROTAGONISTA | **9** — intacta; hay que decidir si la grilla recibe luz (un shader de sprite «lit» sobre el quad Point funciona: la luz se multiplica, sin normales) | **3** — intacta por dentro, pero visualmente subordinada: una textura plana dentro de un mundo con volumen se lee como pantalla, no como materia. El pilar «cada grano simulado» pierde presencia justo cuando todo lo demás gana |
| Riesgo de bugs | **9** — bugs de orden de dibujo, todos visibles y locales | **7** — el cambio de renderer toca TODOS los shaders (regla del playtest 2: `Shader.Find` prohibido, shaders eliminados de la build); sombras 2D que atraviesan la sim | **2** — sorting Z vs sorting order, cursor→plano de juego con perspectiva, colisiones en grilla vs visual 3D desalineado (el mismo bug del labio frontal, en tres ejes) |
| Integración partículas/materia con máquinas | **9** — el sándwich ya resuelve «el líquido se ve DENTRO sin 3D» (R69); falta extenderlo | **9** — ídem + la luz dentro del recipiente (brasa iluminando el vidrio) es un `Light2D` puntual | **4** — la materia vive en un quad; la máquina 3D la envuelve. El vidrio frontal sale gratis en 3D, pero el fuego de la sim no ilumina la malla y la malla no proyecta sombra sobre la materia sin trucos |
| Libertad de composición | **6** — un plano de juego, capas delante/detrás; suficiente para taller + ventana | **8** — planos con velocidades distintas, luz que dirige la mirada, arquitectura frente que enmarca | **9** — la mayor, y la menos necesaria: el juego ocurre en UN cuarto con una ventana |
| Fondo evolutivo (§4 de este doc) | **8** — siluetas por hitos en 2–3 planos: la forma natural de hacerlo | **9** — ídem con luz de hora del día y humo iluminado | **6** — geometría distante con LOD; más cara por hito y compite con el primer plano |
| Iluminación | **3** — ninguna; solo `TinteGlobal`/`TintePlano` y halos pintados | **8** — `Light2D` + normales + sombras 2D: brasas, ojos-lámpara y fuego del Maestro con CAUSA (la luz del prólogo ya nace de Fire real de la sim) | **9** — real; pero el 90 % de su valor en este juego es «el fuego ilumina el taller», que B ya da |
| Rendimiento | **10** — hoy el frame lo manda la sim, no el render | **9** — el 2D Renderer con pocas luces es barato; las normales suben memoria de texturas ×2 en máquinas | **6** — sombras en tiempo real + malla + la sim a 30 Hz: manejable en PC, pero el objetivo de mín-spec sube |
| Tiempo de prototipo con evidencia | **9** — 1 tarde | **8** — 1–2 tardes (§5) | **3** — 2–3 semanas para ver algo comparable |
| Riesgo de scope | **9** — acotado por naturaleza | **7** — el riesgo es querer iluminar TODO; se acota con la regla «luz solo donde hay fuego» | **1** — el clásico «ya que estamos en 3D…»: cámara libre, máquinas con interior, personaje 3D final. Es el pivote que mata demos a 3 meses del Next Fest |
| Beneficio visual (lo que ve el tráiler) | **6** — más limpio, más profundo, sigue siendo «un falling-sand bonito» | **8** — luz cálida, sombras, humo iluminado, ventana con parallax: el salto de percepción más grande por hora invertida | **9** — el mayor techo, pero el techo no es el problema: el suelo lo es (coherencia entre sim 2D, personaje 2D y máquinas 3D) |
| **Suma (/140)** | **116** | **114** | **65** |

Lectura: A y B empatan porque **B ES A más tres perillas**; se distinguen solo
en iluminación y beneficio visual, donde B gana claro, y en riesgo técnico, donde
A gana poco. Por eso la recomendación es «A con la mitad barata de B» y no
«A o B». La C pierde en cada columna que toca el corazón del juego.

### 2.2 Referencias que respaldan la lectura (todas 2D, todas premiadas o de culto)

Noita (IGF 2020, Excellence in Design: falling-sand puro, sin una sola malla),
Terraria (tiles con cantos enmarcados y parallax de 4–5 planos), Dome Keeper
(terreno destructible + luz 2D con normales), Hollow Knight (parallax masivo y
arquitectura frente que tapa al personaje), Eastward (2D con luz por normales
y humo iluminado: la sensación de «volumen» sin Z), Oxygen Not Included (el
cuarto-sección con máquinas back/front: exactamente nuestro sándwich). Del
lado 2.5D real, Trine y Little Nightmares lo justifican porque su gameplay ES
la profundidad (saltos entre planos, esconderse); el nuestro es verter en una
grilla. Ningún falling-sand de referencia ha ido a 3D real y conservado la
lectura grano a grano.

---

## 3. LAS TRES PREGUNTAS CLAVE

### 3.1 ¿Síntomas de límite del 2D o soluciones estándar no explotadas?

Soluciones estándar no explotadas. La lista, con lo que resuelve cada una:

| Síntoma que se sintió | Herramienta 2D estándar que falta | Coste |
|---|---|---|
| Todo se ve plano; el personaje «flota» sobre la roca | `Light2D` + normales + `ShadowCaster2D` (sombra de contacto del personaje y máquinas) | 1 tarde el cambio de renderer + 1 tarde por familia |
| Máquinas sin volumen | normales derivadas de la altura procedural (las texturas ya se generan en código: un paso más en `MaquinariaSprites`) | 1–2 tardes |
| La pared es un telón | 3 planos de parallax con siluetas + humo en loop + luz de hora | 1 tarde el armazón; el arte por hitos, aparte |
| «Recorte roto» al componer alfa entre Point y Point (regla 19) | renderizar la sim a una `RenderTexture` ×3 con un shader de suavizado/contorno y componer UNA sola textura Bilinear con el fondo (es la vía técnica que la prueba de Marching Squares debería comparar) | 1 tarde |
| El labio frontal se veía «blocky» (R66–68, apagado) | arte de canto REAL en `ArquitecturaFrente` (55) vía Sprite Shape o sprites de borde por chunk, no un anillo de téxeles | 2 tardes |
| El vidrio del recipiente no recorta la materia | `SpriteMask` por recipiente (hoy no hay ninguna) | horas |
| Las brasas y los ojos no «brillan» | bloom por volumen URP (con umbral alto para que solo emisivos florezcan) + viñeta nativa en vez de IMGUI | horas |
| El taller no cambia con el tiempo | grading/tinte por volumen animable (el `TinteGlobal` ya lo insinuaba: «el único punto que hay que animar») | horas |

Ninguna de estas ocho toca `SimStepper`, `CellGrid`, la red ni el gameplay.

### 3.2 ¿El 2.5D real resuelve o cambia los problemas por otros más caros?

Los cambia. Problema por problema:

- «Se ve plano» → C lo resuelve, pero B también, y B no crea el siguiente.
- «La sim y las máquinas no se integran» → C lo EMPEORA: hoy son dos texturas
  2D en la misma escalera de orden; en C son una textura plana y una malla con
  luz real, y la costura es peor que la actual (la regla 19 en tres ejes).
- «Composición limitada» → C da libertad que el juego no usa: un cuarto, una
  ventana, dos máquinas nuevas por era.
- Nuevos problemas exclusivos de C: cursor→plano (verter con precisión pide
  que el punto de pantalla sea la celda; con perspectiva deja de serlo salvo con
  cámara ortográfica, y entonces la «profundidad» es solo parallax caro),
  colisión en grilla vs volumen visual (el personaje choca con celdas que no se
  ven o atraviesa mallas que sí), réplicas 3D en co-op, y un pipeline de arte
  doble (personaje 2D + máquinas 3D, o todo 3D contra la dirección sellada).

### 3.3 ¿Hay un experimento mínimo con evidencia antes de tocar arquitectura?

Sí, y no requiere tocar arquitectura: §5.

---

## 4. EL FONDO COMO CIVILIZACIÓN EVOLUTIVA POR HITOS — crítica

Idea de Cesar: el fondo del taller (o lo que se ve por el vano) muestra una
civilización que evoluciona por hitos a medida que el jugador progresa; ficción
contrafactual donde neandertales, denisovanos y sapiens coexisten (Denisova 11,
la niña de madre neandertal y padre denisovano, como emblema) y el «avance» se
lee como actualización de software: siluetas y loops por hito, sin NPCs con IA.

### 4.1 Intento de romperla

1. **¿Aporta diseño o espectáculo?** Como está enunciada («el fondo evoluciona
   con el progreso»), espectáculo: nadie decide nada mirando el fondo. Se
   vuelve diseño con UNA regla: **los hitos no son «progreso» en abstracto, son
   los materiales de ORO que salieron por el vano** (caravanas del tablón,
   GDD §0 nodos habilitadores). Enseñaste adobe → aparecen muros de adobe;
   enseñaste cerámica → humo de hoyos de cocción y vasijas en la silueta;
   cal → paredes blancas; carbón → hornos con brasa de noche. Así el fondo es
   el **libro mayor del mundo exterior**, la contrapartida visible del Trueque:
   lo que vendes no desaparece, se convierte en civilización. Eso sí conecta
   con la mecánica central («verter, entregar, comerciar») y con el eslogan
   («rebuild human knowledge»: lo reconstruyes y se ve reconstruido).
2. **¿Contradice el decreto R107 (la civilización CAYÓ; ruinas amables)?**
   No, si se coloca bien: dentro/abajo están las ruinas heredadas (las máquinas
   que reparas); fuera están las protocivilizaciones de los nodos, que existen
   por sus recursos cercanos y a las que llega tu conocimiento en caravana.
   El fondo evolutivo es lo de FUERA visto por la ventana. La contradicción
   aparecería solo si el fondo se adelanta: si allá afuera ya tienen bronce y
   tú no puedes comprarlo, la regla de hierro R60 y el Trueque mueren. De ahí
   la **regla del espejo retrasado: el fondo nunca va por delante de tu
   hito más alto; va uno atrás o exactamente a la par**.
3. **¿La ficción de homínidos coexistiendo rompe el pacto de fidelidad
   (≥85 % nombre real / confiesa / prohibido)?** El pacto es sobre MATERIALES;
   el mundo ya es ficción (diez mil años tras una caída). La coexistencia de
   especies humanas es además un hecho (neandertales y denisovanos convivieron
   con sapiens y se cruzaron; Denisova 11 existe). Lo contrafactual es que
   sigan aquí. Límite claro: **nada de esto entra en fichas con voz de
   enciclopedia**; se cuenta con siluetas (constituciones distintas —robusta,
   grácil— haciendo el mismo trabajo) y jamás con texto. Eso además rima con
   dos cosas ya selladas: el muñeco de REMIENDOS (un cuerpo hecho de
   predecesores) y el co-op SIN CLASES (gente distinta, mismo trabajo).
4. **¿Es un sistema de progreso?** Solo si tiene lectura: un jugador debe
   poder mirar la ventana y saber «voy por la cal». Para eso los hitos deben ser
   pocos (4–5 por era, no 12) y visualmente distintos a 80 celdas de cámara
   (el fondo se ve al ~20 % de su tamaño: siluetas grandes, no detalle).
5. **¿Firma?** Sí, y es de las baratas: «la misma ventana, diez mil años».
   Ningún comparable (Noita, Potion Craft, Core Keeper, Dome Keeper) tiene un
   fondo que registre lo que el jugador enseñó al mundo.
6. **¿Comunicable en tráiler?** Es un beat de tráiler hecho: la misma toma
   fija con la ventana pasando de fogatas a hornos a murallas en 4 segundos.
   Se entiende sin idioma, que es la restricción de marketing del juego.
7. **¿Coste?** Bajo si son siluetas planas y loops de 2–4 cuadros (humo,
   fuego, una caravana que cruza). Era I: 4–5 hitos × 3 planos ≈ 12–15
   siluetas + 3–4 loops + 1 ciclo de luz. Nada de esto necesita Wan ni 3D; se
   dibuja o se hornea. En co-op es gratis: los hitos son conocimiento del host
   (ya compartido), así que todos ven la misma ventana.
8. **¿Riesgo?** Scope creep hacia NPCs: «¿y si saludan?», «¿y si se ve a la
   niña?». Regla: **cero agentes; todo lo que se mueve es loop o partícula**.
   Segundo riesgo: que el fondo compita con el plano de juego (contraste,
   movimiento). Regla: **el fondo vive por debajo de L≈0.35 de luminancia y
   solo se mueve lo que es humo, fuego o caravana**.

### 4.2 Cómo limitar el scope (propuesta cerrada)

Una ventana (el vano del oeste, que ya es «la puerta del mundo exterior»), tres
planos (lejos / medio / cerca del vano), hitos = materiales de ORO
**entregados**, agrupados: Era I → (0) fogatas y pieles, (1) adobe: primeros
muros, (2) cerámica: humo de hoyos y vasijas, (3) cal y carbón: paredes blancas
y hornos que brillan de noche, (4) vidrio/mortero: el primer edificio con
ventana — el eco de la tuya. Cada hito: 1 silueta por plano (3 PNG) + 1 loop
compartido. Ciclo día/noche opcional atado al `TinteGlobal`.

### 4.3 Notas /10

| Dimensión | Nota | Por qué |
|---|:-:|---|
| Valor narrativo | **8** | Cuenta el eslogan sin palabras y da sentido al Trueque; con la regla del espejo retrasado no contradice R107/R60 |
| Valor visual | **7** | Siluetas + luz + humo: alto rendimiento por hora, techo medio (son siluetas) |
| Sistema de progreso | **6** | Es un MARCADOR de progreso legible, no un sistema con decisiones; sube a 8 si los hitos del fondo desbloquean algo (caravanas más rápidas, un pedido nuevo) — cuidado, eso ya es scope |
| Diferenciación | **7** | Nadie del género lo tiene; el motivo homínido lo hace inconfundible si se cuenta con pudor |
| Potencial en premios | **6** | Suma a narrativa/visual y a la coherencia de tesis; no gana un premio solo |
| Coste/riesgo (10 = barato y seguro) | **7** | Barato como siluetas; el riesgo es de disciplina, no técnico |

Veredicto: **vale la pena, después de que exista la ventana con planos (B
acotada) y de que la Era I tenga sus encargos de ORO cerrados**, porque los
hitos son esos encargos. Antes es decorar una pared que aún no está.

---

## 5. EL EXPERIMENTO MÍNIMO (una tarde, dos como mucho; sin tocar arquitectura)

**«La ventana iluminada»**, sobre la escena del prólogo tal cual está:

1. Duplicar `PC_Renderer.asset` como `PC_Renderer2D` (2D Renderer) y apuntar
   una copia del RP asset a él; activar SOLO en la escena de prueba. Revisar
   que los materiales de `SimRenderer`/`WorkshopBackdrop`/`MaquinariaSprites`
   (SpriteRenderer con material por defecto) siguen dibujando. Si algo se
   pierde en build, es la regla del playtest 2 y se documenta, no se pelea.
2. Una `Light2D` global tenue + una puntual cálida donde está el fuego del
   Maestro (que ya es Fire real) + una fría en el vano. Un `ShadowCaster2D`
   en el depósito y en el muñeco.
3. Normales para UNA máquina (el depósito de agua: derivar de la altura del
   propio PNG horneado) y para el muñeco en reposo (el pipeline 3D las da; si
   aún no, un normal-from-height del PNG canónico basta para la prueba).
4. Partir `WorkshopBackdrop` en tres planos con factores 0.03 / 0.08 / 0.15 y
   siluetas placeholder del hito 0 y del hito 3 conmutables con una tecla.
5. Bloom por volumen con umbral alto (que florezcan solo brasas y ojos).

**Evidencia que se recoge** (todo en la misma tarde): seis capturas a la
cámara por defecto de 80 celdas (antes/después × día/noche × hito 0/3), el ms
por frame con el Profiler MCP (presupuesto: no perder más de 1 ms), una
partida co-op de 2 minutos con la escena nueva en el host (nada debe cambiar
en el espejo), y el veredicto de Cesar mirando, que es el dato que manda
(lección R109: «lo que se juega siempre debe ser el defecto»).

**Criterios de muerte**: si la sim iluminada se lee peor que sin luz (la
materia pierde el color de referencia del material, que es información de
juego), la luz se queda fuera de la grilla y solo toca máquinas y fondo. Si el
2D Renderer rompe shaders en build sin arreglo en una hora, la iluminación se
aplaza y A sigue sola. Si Cesar no ve diferencia a 80 celdas, se archiva con
captura (regla 15) y no se vuelve a abrir hasta la Era II.

**Coordinación con la prueba de Marching Squares de Cesar**: son ortogonales
y se pueden ver el mismo día. La única recomendación: probar el terreno
continuo SOLO sobre la piedra madre (`StaticSolid`), nunca sobre polvos y
líquidos, y mirar la costura roca lisa/arena granulada a 80 celdas antes de
enamorarse. Si la costura molesta, la alternativa es la fila 4 de la tabla de
§3.1 (la sim entera a ×3 con suavizado en shader), que suaviza TODO por igual.

---

## 6. EL MAYOR RIESGO Y LA MAYOR OPORTUNIDAD

**Mayor riesgo (de la decisión en sí):** el pivote silencioso. No que se elija
C hoy, sino que la B «agresiva» se vuelva C a plazos: primero luces, luego «una
máquina en 3D para ver», luego la cámara con un poco de perspectiva, y a los
dos meses hay dos pipelines de arte y ninguna demo. La defensa es la regla de
oro de este documento: **la cámara es ortográfica y lateral, el plano de juego
es la grilla, y toda profundidad es pintura** (la misma frase que ya rige el
Nivel 4 de `Capas.cs`: «la colisión NUNCA viene de aquí»).

**Mayor oportunidad:** la luz con causa. Este juego tiene algo que casi ningún
2D iluminado tiene: el fuego que ilumina es fuego REAL de la sim, las brasas
son celdas con temperatura, los ojos del personaje son lámparas del lore. Poner
`Light2D` donde ya hay Fire y temperatura convierte la iluminación en
información de juego (ves dónde hay calor, ves que el horno está encendido
desde el otro lado del taller) y no en decorado. Eso, más la ventana que
envejece, es una firma visual que se puede contar en una frase.

---

## 7. LA DECISIÓN QUE NO TOMARÍA TODAVÍA, Y LA INFORMACIÓN QUE FALTA

**No tomaría todavía**: (a) ninguna decisión sobre C, ni «para después»: se
archiva como no-camino salvo que la Era III (seres) demuestre otra necesidad;
(b) el acabado pixel/no-pixel, que sigue ABIERTO por decreto y que la
iluminación por normales empuja hacia «no pixel» — por eso el experimento debe
verse ANTES de sellar el acabado, no después; (c) el contenido concreto del
fondo evolutivo (Denisova, especies) hasta que exista la lista de hitos de ORO
de la Era I; (d) tocar el render de 1 téxel/celda de la sim hasta tener la
prueba de Marching Squares y la de ×3 en shader una al lado de la otra.

**Información necesaria antes de decidir** (en orden de utilidad):

1. Resultado de la prueba de Marching Squares de Cesar: ¿la roca lisa se lee
   mejor a 80 celdas y cuánto cuesta la costura con la materia granulada?
2. Las seis capturas del experimento de §5 y el veredicto de Cesar sobre luz
   sí/no en la grilla.
3. El presupuesto de rendimiento objetivo (mín-spec) — hoy no está escrito.
4. Los números del playtest de movimiento (`DISENO_MOVIMIENTO.md`, F6 A/B/C):
   si gana «a pie», suelos y plataformas pesan más y el Nivel 4
   (`ArquitecturaFrente`) deja de ser decorado para ser lectura de juego; si
   gana el vuelo, el fondo y la luz pesan más que el terreno. Nota: el decreto
   R107 dice «LEVITA, no camina»; esa investigación está abierta con permiso
   de Cesar y su resultado puede pedir una enmienda explícita al GDD §0 —
   este documento no la hace.
5. La lista cerrada de encargos de ORO de la Era I (`ROADMAP.md` §2, «falta
   para cerrar»): son los hitos del fondo.
6. Cómo se verán las máquinas remotas en co-op (hoy siluetas): decide si las
   normales/luces deben replicarse o si el espejo se queda sin luz.

---

## 8. ¿ME ACERCA A UN PREMIO DE DISEÑO? (respuesta honesta)

La elección 2D/2.5D **no mueve la aguja de un premio de diseño** en ninguna
dirección; puede mover la de arte visual, y ahí un híbrido 2.5D con sim 2D,
personaje 2D y máquinas 3D **restaría** por incoherencia, mientras que un 2D
iluminado y coherente suma. Lo que los jurados de diseño (IGF Excellence in
Design, Seumas McNally, IndieCade, los festivales europeos) premian de forma
consistente es una idea de sistema clara, ejecutada hasta el final y legible
en quince minutos: Noita ganó con una grilla fea y una tesis («cada píxel
está simulado»); Baba Is You con cuadrados; Outer Wilds con un reloj. Este
juego ya tiene su tesis (materia real + el conocimiento ES la progresión) y su
verbo (verter con precisión). Lo que lo acerca a un premio es que en la demo del
Next Fest esa tesis se sienta en la mano y se vea en la pantalla sin leer nada:
el fuego real que ilumina, el material que se comporta como en la vida, la
ventana que registra lo que enseñaste. El fondo evolutivo por hitos, con la
regla del espejo retrasado, es de las pocas ideas de esta ronda que sirve al
diseño Y al espectáculo a la vez, y es barata. La C, en el mejor de los casos,
es un premio de arte al que se llega tarde y sin demo.

Ninguna de estas cosas garantiza nada —los premios son pocos y llenos de
azar—, pero si la pregunta es «¿en qué invierto las próximas semanas para
tener más probabilidad?», la respuesta es: cerrar la Era I jugable, la luz con
causa, la ventana que envejece, y que el muñeco se sienta bien en las manos.
En ese orden.

---

## 9. ENLACES Y COHERENCIA CON LO SELLADO

- GDD §0 (decreto R107): ruinas amables, R60, nodos habilitadores, «el vano
  del oeste es la puerta de ese mundo exterior» → el fondo evolutivo se ancla
  ahí; «dirección visual: ilustración 2D artesanal conviviendo con la sim» →
  la C la contradiría; este documento no la toca.
- `DIRECCION_DE_ARTE.md` §1 (convivencia de capas, acabado ABIERTO), §3
  (3D = herramienta de producción, no arte final; las 3 perillas), §3.5
  (talla 12 celdas, cámara 80, escala en celdas jamás con zoom).
- `Capas.cs` (R66, «dirección 2.5D de Cesar»): los 6 niveles ya existen; este
  informe propone LLENARLOS, no reemplazarlos. El labio frontal apagado
  (R66–68) y la regla 19 del alfa entre texturas Point son las dos lecciones
  que la B debe respetar (`SimRenderer.cs`, `SIM_NOTES.md` «Render»).
- `ESTADO.md` «Dónde estamos → 2.5D» y backlog nº 4 (vidrio frontal al resto de
  máquinas) y nº 5 (réplicas multi con visual completo).
- `DISENO_MOVIMIENTO.md` (F6 A/B/C, telemetría): su resultado cambia el peso
  del terreno frente al fondo (§7.4).
- `PLAN_ANIMACION_R119.md`: el pipeline 3D del muñeco produce normales gratis
  cuando llegue; hasta entonces normal-from-height del PNG.
- `ROADMAP.md` §2 (encargos de ORO de la Era I = hitos del fondo) y §5 (demo
  gratis al Next Fest: el reloj que hace inviable la C).
