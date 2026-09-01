# LA PIEL DE ROCA — prueba visual de terreno con contorno orgánico (R124)

*(Pedido de Cesar, 1/09/2026: «exploración e implementación de prueba para
mejorar visualmente la cueva/taller destructible usando una técnica tipo
Marching Squares… no cambies la sim ni la representación granular de la
materia; solo una piel/contorno visual para el sólido estático/destructible;
quiero ver si ese contraste con partículas cuadradas funciona o canta».
Nace de `EVALUACION_2D_VS_2_5D.md` §5 y §7.1: es la prueba que ese informe
pedía ANTES de decidir nada sobre el render de 1 téxel/celda.)*

## 1. QUÉ HICE

Un componente nuevo, `Game/PielDeRoca.cs`, que dibuja la ROCA MADRE
(`MaterialId.Stone`, y solo ella) como MALLAS por chunk con contorno de
marching squares, POR DEBAJO de la sim (sortingOrder −6, la sim va en −5). La
sim no cambia en nada: la celda sigue siendo Stone para el stepper, el cincel,
la colisión y la red; lo único que hace `SimRenderer` distinto es no pintar
Stone en su textura mientras la piel está activa (`SimRenderer.OcultarRoca`,
téxel a alfa 0 = agujero, no semitransparencia — la regla 19 no aplica). Arena,
agua, brasas y todo lo granular siguen pasando POR DELANTE, cuadrados: la
materia sigue siendo materia.

Cómo funciona (adaptación de la técnica de Sebastian Lague, "Procedural Cave
Generation", a un terreno VIVO):

- **Campo escalar** en las esquinas de celda = solidez media de las 4 celdas
  que la rodean (0, ¼, ½, ¾, 1). Umbral 0.49 → los muros de UNA celda se
  dibujan enteros, las esquinas convexas se achaflanan media celda, las
  cóncavas se rellenan media celda. **La silueta nunca se aleja más de media
  celda de la colisión** (los pies del muñeco siguen tocando el borde).
- **Interpolación en las aristas** + un temblor determinista por posición
  (±11 % del tramo) para que ningún borde sea perfectamente recto sin cambiar
  la topología. Casos ambiguos (5/10) resueltos por el valor central. Una
  celda de roca aislada se dibuja como guijarro (su colisión existe, que se
  vea).
- **Cuatro capas por chunk** (mismo sortingOrder, la Z decide el orden para
  que el relleno de un chunk vecino jamás tape las bandas de este):
  1. *sombra*: la misma silueta desplazada 0.3×0.5 celdas abajo-izquierda
     ("canto" de la placa) + un halo oscuro hacia fuera del contorno (la
     sombra que la roca proyecta sobre el telón de ladrillo).
  2. *relleno*: polígonos texturizados (UV en mundo, tesela cada 25.6 celdas)
     con **masa interna**: luminancia por vértice según la distancia al aire
     (transformada de distancia por chunk, 6 celdas de radio): el borde
     expuesto queda claro, el corazón de la masa un 26 % más oscuro.
  3. *bandas*: cada tramo del contorno sabe si es **SUELO** (normal hacia
     arriba: filo claro pegado al canto + banda cálida), **PARED** (banda de
     oclusión) o **TECHO** (banda de sombra más ancha), y encima la **línea de
     tinta** (0.18 celdas, la regla 19 en malla). Donde la roca toca OTRO
     sólido (piso estructural, mortero…) la tinta se adelgaza y no hay banda:
     junta, no borde.
  4. *deco*: estalactitas en techos (con gota azul a veces), raíces que
     cuelgan y se curvan, musgo de PÁTINA en suelos, grietas de tinta en
     paredes; todo determinista por posición, sin un solo asset.
- **Textura de roca procedural** (256², cero assets, como todo el arte del
  proyecto): color = roster de Stone tirado hacia la tinta parda; grano fbm,
  manchas grandes, crestas de estrato muy suaves, y GRIETAS esporádicas
  (Worley de dos tamaños enmascarado por ruido: solo la mitad de las juntas
  existe, para que no lea como adoquín — la primera versión leía como
  jirafa/empedrado y se descartó con captura).
- **Actualización en vivo**: hash del patrón de roca por chunk; cuando un
  chunk cambia (cincel, derrumbe, espejo de red), se reconstruye él y sus 8
  vecinos. Medido con Unity abierto: **0.035 ms por chunk, 30 ms el mundo
  entero (864 chunks), 0.01 ms por frame en reposo**; 140 k vértices en total
  (las celdas macizas se funden por fila).
- **Niveles acumulativos con F7** (el atril avisa; se guarda en PlayerPrefs):
  0 apagada (la grilla de siempre) · 1 contorno · 2 + bandas · 3 +
  profundidad · 4 + decorada. Así Cesar compara en juego sin recompilar.
- **Ctrl+F7 = LA CUEVA DE MUESTRA**: talla alrededor del jugador (con el
  mismo `Paint` del cincel; solo en el anfitrión) un escenario con todos los
  casos: cámara ovalada con bóveda irregular, suelo con escalera de una celda,
  dos pilares irregulares, repisa de UNA celda de grosor, guijarro aislado,
  poza de agua y túnel bajo. Hazlo en zona libre de máquinas (vuela hacia la
  roca a la derecha del taller antes de pulsarlo).

## 2. QUÉ DECISIÓN VISUAL TOMÉ (y por qué)

**Roca natural con tinta, contra ruina construida en grilla.** La sillería de
8×4 que dibujaba `SimRenderer` cuenta «esto lo construyó alguien»; la piel
cuenta «esto es la tierra». Las dos cosas conviven ahora y eso es justo lo
que el mundo pide (GDD §0: ruinas heredadas DENTRO de una cueva): el piso
estructural sigue fabril y recto, las máquinas siguen a todo color, y la roca
madre que el jugador talla se vuelve orgánica. El contraste roca lisa /
partículas cuadradas, que era la duda de Cesar, **no canta**: la arena y el
agua se leen como materia SOBRE la roca precisamente porque son de otra
naturaleza; lo que cantaba era roca cuadrada junto a materia cuadrada del
mismo tamaño (todo era grilla). Lo que sí se nota es la escalera de la
colisión en pendientes largas: el chaflán de media celda suaviza pero no
oculta que el mundo es una grilla — y no debe ocultarlo.

Paleta: tinta parda sobre ceniza cálida, musgo en PÁTINA, gota en AZUL
MUDANZA apagado. El tinte de la vista (`TinteGlobal` → `TintePlano` en la
mudanza) se copia del quad de la sim para no desentonar. Sin luz todavía: la
piel es candidata natural a recibir normales cuando llegue el experimento de
la R123 §5 (la textura ya tiene relieve implícito: grietas y grano).

Lo que probé y descarté: (a) estratos ondulados fuertes cada 2 celdas → leía
como madera/lodo; (b) placas Worley continuas → adoquín; (c) sombra profunda
del color del telón → invisible; ahora es un canto más claro que el telón y
más oscuro que la roca, y se le suma el halo proyectado.

## 3. QUÉ USARÍA DESPUÉS COMO ARTE FINAL

- La **geometría se queda** (contorno, bandas por orientación, masa interna,
  juntas): es la parte cara y ya está.
- La **textura procedural se sustituye** por 2–3 texturas de roca pintadas
  (o fotográficas tratadas) de 512² teselables, con variante por profundidad
  (superficie / masa) mezcladas por el mismo canal de distancia al aire; y
  una textura de *cantos* para la banda de borde. Es un `Texture2D` y un
  segundo UV: cambio pequeño.
- La **decoración se sustituye por sprites** horneados (estalactitas, raíces,
  musgo, hongos, huesos de máquina) colocados con la misma lógica de
  segmento+hash (la lógica de colocación ya existe; hoy dibuja triángulos).
- **Normales** para la piel cuando exista la luz 2D: la textura pintada trae
  su normal y el filo del suelo deja de ser una hebra pintada para ser luz.
- **MSAA 4x** o un shader de línea con antialias para la tinta: hoy la línea
  se dibuja como polígono sin AA y escalona en diagonales largas.

## 4. LIMITACIONES ENCONTRADAS

- **Solo Stone.** Los otros StaticSolid (piso estructural, mortero, hormigón,
  vidrio, cerámicos) siguen en grilla a propósito (son fabricados). Si algún
  día un material natural nuevo (caliza en veta, arcilla dura) quiere piel,
  es una lista blanca en `EsRoca`.
- **Media celda de licencia** entre lo que se ve y lo que colisiona. En
  esquinas cóncavas la roca visual invade media celda de aire: un grano de
  arena en esa esquina se dibuja encima (bien), pero el muñeco puede solapar
  0.05 u de roca pintada. Nadie lo notó en las capturas; se anota.
- **Las cavidades de las máquinas**: el sándwich `MaquinaFondoInterior (−8)`
  vive detrás de la piel (−6); el halo de sombra y los filetes cóncavos pueden
  asomar ~1 celda dentro de la boca de un recipiente tallado en roca.
- **Sin antialias** en la tinta (ver §3).
- **Estalactitas/raíces son pintura**: no colisionan ni se rompen al tallar
  el techo — desaparecen con su tramo de contorno, que es lo correcto, pero
  sin polvo ni sonido.
- **La sim arranca en pausa** en la escena de laboratorio hasta que el juego
  la suelta: la piel se construye igual (no depende del tick), pero la textura
  de la sim no repinta hasta que corre — al alternar F7 con la sim en pausa
  se ve la roca "desaparecer" hasta el primer tick. Es de la escena, no de la
  piel; lo anoto porque me hizo perder diez minutos.
- **Memoria**: ~1.750 mallas pequeñas y 4 GameObjects por chunk con roca
  (≈3.000 objetos). Razonable, pero si molesta en el profiler se fusionan las
  4 capas en submallas de un solo renderer por chunk (mismo orden garantizado
  por índice de submalla).

## 5. RIESGOS DE INTEGRACIÓN (breves)

- **Partículas**: ninguno funcional (van encima). Visual: un charco de UNA
  celda sobre un suelo achaflanado queda "en el aire" media celda en la
  pendiente; a 80 celdas no se nota.
- **Máquinas**: ninguno funcional. Visual: ver cavidades arriba. Las réplicas
  de máquinas en co-op no cambian.
- **Personaje**: la sonda de suelo sigue leyendo la grilla; pisa donde
  pisaba. La sombra de contacto del muñeco cae sobre la piel, se ve mejor que
  sobre la sillería.
- **Red**: el espejo del invitado recibe `mat[]` y despierta chunks, así que
  la piel del invitado se reconstruye sola (misma ruta de hash). No probado
  en vivo esta ronda.
- **Cincel**: el hueco se ve redondeado media celda mientras la colisión es
  cuadrada; al tallar pasillos de 2–3 celdas de ancho la piel los estrecha
  visualmente y el muñeco parece pasar "justo". Si molesta, el umbral 0.49 →
  0.40 abre las esquinas cóncavas (menos relleno) a cambio de puntas más
  finas.

## 6. QUÉ RECOMENDARÍA COMO SIGUIENTE PASO

1. **Que Cesar juegue 10 minutos con F7** en el taller y en la cueva de
   muestra, tallando y vertiendo, y decida entre 0 / 2 / 4. Mi voto: el nivel
   2 o 3 como base (contorno + bandas + profundidad) y la decoración
   procedural apagada hasta tener sprites de verdad — las estalactitas de
   triángulo son la parte que más "placeholder" grita.
2. Si gusta: **fijar la piel como default**, quitar la sillería de Stone del
   `SimRenderer` (o dejarla como acabado de los sólidos FABRICADOS, que es
   donde tiene sentido) y llevar al backlog las 3 sustituciones de §3.
3. **Correr el experimento "la ventana iluminada" (R123 §5) SOBRE esta piel**:
   normales de la textura de roca + Light2D del fuego del Maestro es donde
   esta prueba y aquella se multiplican.
4. **Antes de sellar el acabado** (pixel / no pixel, decreto abierto): esta
   piel empuja hacia "no pixel". Verla junto a la variante pixel del
   personaje antes de decidir.

Capturas de la ronda (en `Temp/cap124/`, no versionadas): `f_n0` grilla de
siempre · `e_n2` contorno+bandas · `e_n4` decorada · `d` cueva de muestra.
Una comparación reducida antes/después queda en
`docs/ref/piel_de_roca_R124.jpg`.
