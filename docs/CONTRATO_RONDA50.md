# CONTRATO CONGELADO — RONDA 50: EL REWORK DE SEMILLA CERO (Playtest 50)

Veredicto de Cesar sobre el 49 (literal, es el mandato): *"¿por qué tengo dos
máquinas de fuego? ¿cuál es la diferencia? · la placa nueva enfría y calienta,
cosa que es muy irreal · en la imagen había una placa de frío y otra de calor,
no una que hace las dos funciones — lo dije pensando en minimizar la cantidad
de mezclas que podían salir al inicio · esta vez me gustaría que [los
descubrimientos] me salgan en pantalla y que yo las tenga que cerrar · aún no
descifro qué significa que resista el crisol · si sigo teniendo 4-6 materiales
5 minutos experimentando y aún sin develar ninguna máquina... debería haber un
STORYTELLING MUY CLARO Y DE POCAS OPCIONES tal como se planeó en la literatura
de la seed 0; en vez de eso solo hay escasez de máquinas, confusión de
recursos, y un trayecto muy grande para dejarlas en la tolva SIN NADA QUE ME
LO ENSEÑE · un rework profundo para elevar la calidad · seed 0 tiene que poder
ser experimentable en el MULTIPLAYER también — necesitamos esa tercera sección
para hacer más pruebas en simultáneo."*

La literatura original (docs/CONTRATO_SEMILLA.md): milagro en el minuto 0-2,
comprensión, pequeño fracaso, comprensión mayor, final abierto. El 49 la
traicionaba con cantidades de recadero (25+15+15), un solo fuego que en
realidad eran DOS, y una Tolva a media pantalla sin señal.

## 0. DIAGNÓSTICOS CERRADOS
- **D1 LA PLACA TERMOSTATO**: la EmisionTermica (pt44) empuja la temperatura
  HACIA su objetivo en ambos sentidos (Newton bidireccional) — la placa de
  calor ENFRÍA lo que esté más caliente que su objetivo. Físicamente absurdo
  (una estufa no enfría la sopa) y Cesar lo vio.
- **D2 DOS FUEGOS AL ARRANCAR**: crisol (transforma por hornadas) + placa de
  calor bajo el grifo (calienta zona) nacen JUNTOS y el jugador no distingue
  sus oficios. El pedido original de Cesar era el PAR frío/calor como aparatos
  didácticos — no dos fuegos simultáneos en el minuto 0.
- **D3 EL TEATRO SE AUTO-CIERRA**: la cola con respiro (pt49) espacia banners
  pero se desvanecen solos — Cesar quiere LEERLOS: que salgan y que ÉL los
  cierre.
- **D4 EL TRAYECTO MUDO**: la Tolva de entrega está lejos del cuarto íntimo y
  nada te enseña el camino ni el gesto.

## 1. ENCARGO P — LA FÍSICA MONO-FUNCIÓN (archivos: Sim/SimStepper.cs SOLO el
bloque EmisionTermica, Game/HeatPlate.cs, Game/ChillStone.cs SOLO sus params)

- La emisión térmica gana DIRECCIÓN: una fuente CALIENTE solo puede SUBIR la
  temperatura de una celda (si la celda ya está por encima del objetivo local,
  la deja en paz); una fuente FRÍA solo puede BAJARLA. Implementación a
  decisión de P leyendo el bloque real (signo derivado de comparar objetivo
  contra CellGrid.AmbientRaw, o un flag por registro de emisión — documentar).
  El collar anti-inundación y el falloff NO cambian.
- HeatPlate: sus tres estados solo calientan. ChillStone: solo enfría. Los
  docblocks de ambos declaran la ley: "una estufa no enfría; una nevera no
  calienta" (regla 49: la promesa con su línea).
- El HOLD de apagado (HoldTicksTrasApagar) conserva su comportamiento pero
  respetando el signo del aparato.
- Cero cambios visuales (las placas del pt49 por Opus quedan intactas).

## 2. ENCARGO F — LAS FICHAS QUE TÚ CIERRAS (archivos:
Game/SubstanceKnowledge.cs, Game/AlbumReal.cs)

- Cada descubrimiento presenta su FICHA-VITRINA EN PANTALLA (la misma vitrina
  del álbum, con nombre real y reseña) y espera CIERRE MANUAL: botón "seguir"
  y Escape/clic — nada de desvanecerse solo. La cola del pt49 se conserva
  (de a UNO), pero el turno ahora lo cede el CIERRE del jugador (+2s de
  respiro), no un temporizador.
- Mientras una ficha modal está abierta: los atajos del mundo respetan
  AlbumReal.Abierto (ya existe el patrón), Escape la cierra a ELLA (escalera
  de guardas de DayCycle intacta — verificar orden), y la sim NO se pausa (es
  una lámina encima, no un menú).
- El banner "ALGO NUEVO" queda como precursor breve SOLO si la ficha no puede
  abrirse aún (panel ocupado); en el caso normal la ficha ES el anuncio.
- El librito sigue pulsando cuando hay fichas pendientes por si el jugador
  cierra rápido y quiere releer (B, sin cambios).
- OJO regla 12: la ficha abierta debe bloquear E/atajos del mundo igual que el
  álbum. Cero allocs en OnGUI.

## 3. ENCARGO G — EL GUION COMPACTO Y EL CAMINO QUE SE ENSEÑA (archivos:
Game/SemillaCero.cs, Sim/SimLevelBuilder.cs, Game/HintSystem.cs,
Game/DeliveryChute.cs, Game/OrdersHud.cs)

### 3a. Cantidades de milagro, no de recadero
Beat2 25→10 · Beat3 15→8 · Beat4 15→8 · Beat5 prensa/columna 10→6 · frío 8→6.
(Regla 43: números exactos observables — el informe los repite.)

### 3b. LA TOLVA CERCANA (D4)
En modo Semilla Cero, la Tolva de entrega (DeliveryChute) se talla DENTRO del
cuarto íntimo, a pocos segundos de vuelo del crisol (sitio exacto: decisión de
G leyendo el plano real de SimLevelBuilder, documentado; no puede pisar
estaciones ni la veta). El caótico NO cambia. Si la Tolva clásica lejana
existe además, en seed 0 NO se talla (una sola boca de entrega, pocas
opciones).

### 3c. EL CAMINO SEÑALADO
- Cuando hay pedido activo, la Tolva LATE (pulso de luz suave, patrón
  LuzHogar/latido existente — reutilizar, no inventar) y su rótulo de
  proximidad dice "TOLVA — deja aquí lo pedido (vierte con clic derecho)".
- El primer pedido (beat 2) llega con una línea de consejo dedicada que
  enseña el gesto completo: "Aspira tu arena (clic izq.), vuela a la TOLVA
  que brilla y viértela dentro (clic der.)." — español latino.
- OrdersHud: el pedido activo muestra una FLECHA de dirección hacia la Tolva
  (izquierda/derecha/abajo según posición relativa del aprendiz — barato, un
  glifo, sin minimapa).

### 3d. El texto de "resiste"
El pedido 5.4 y la pista del Maestro (pt49) se reescriben para no usar la
palabra "resista" a secas: "Trae al ENSAYO (la sala recién abierta) algo que
sobreviva al rojo sin arder ni fundirse — lo bien cocido aguanta." La palabra
del oficio se ENSEÑA, no se presume.

### 3e. Lo que NO cambia
El orden de beats, la veta (pt48), la trampa del carbón, el final abierto.
El contrato NO pide más máquinas antes del beat 5 — la percepción de "escasez"
se ataca con cantidades cortas (3a), la tolva cercana (3b) y el par térmico
que ahora llega junto (ENCARGO M 4a): el arco entero debe caber en ~15-20 min.

## 4. ENCARGO M — EL PAR TÉRMICO Y LA TERCERA SECCIÓN (archivos:
Game/AlkahestGameBootstrap.cs, Net/TallerSesionHud.cs, Net/SimSync.cs,
AlkahestSim.cs)

### 4a. Un solo fuego en el minuto 0 (D2)
En modo Semilla Cero, la placa de calor NO nace al arrancar: nace JUNTO a la
piedra gélida al destaparse la SalaFria (el beat del frío se vuelve la lección
de TEMPERATURA POR ZONAS con el par completo: una placa que solo calienta y
una piedra que solo enfría, lado a lado). El arranque queda con UN fuego: el
crisol. El caótico no cambia. Coordinar con el spawn actual
(HeatPlate_PilaAgua): en seed 0 su sitio pasa a la alcoba fría o junto a ella
— decisión de M leyendo el plano, documentada; el nombre del objeto puede
cambiar si el sitio ya no es la pila.

### 4b. SEMILLA CERO COMPARTIDA (la tercera sección)
- El lobby multi (TallerSesionHud) gana un botón del lado del anfitrión:
  "ANFITRIÓN — SEMILLA CERO compartida" (además del taller caótico de
  siempre). Al elegirlo, el mundo del anfitrión se crea con la seed
  777002 + AplicarOverridesSemillaCero + veta + salas DESTAPADAS TODAS +
  identidades reales — SIN el director de beats (SemillaCero.Init ya se
  auto-anula en multi por SimSync.EnEscena, contrato pt40 §2: NO tocarlo).
  Es un LABORATORIO compartido para pruebas en simultáneo, no el arco guiado
  — Cesar lo pidió textual ("quizás luego lo quitemos").
- Replicación del modo: la seed ya viaja en el handshake de SimSync — el
  INVITADO detecta seed == Universe.SemillaCero y aplica en su espejo los
  mismos overrides + ModoSemillaCero = true (nombres reales, álbum, cruces).
  M verifica DÓNDE viaja la seed hoy y lo documenta; si no viaja, la añade al
  handshake con el mismo patrón de los mensajes existentes.
- ModoSemillaCero (static) debe volver a false al salir de la sesión/al
  título en ambos lados — buscar dónde se resetea hoy en un jugador y cubrir
  el camino multi (fuga de estado entre partidas = bug clásico).
- Los cruces (Universe.TryCruce) están gateados a
  AlkahestGameBootstrap.ModoSemillaCero — con el flag replicado funcionan en
  multi sin tocar Crisol. Verificar y documentar.
- La veta: SimLevelBuilder.BuildVetaTurba ya se gatea al flag — en multi
  compartida debe tallarse en el mundo del HOST (verificar que el espejo del
  invitado la recibe por chunks, que es gratis).

## 5. HECHOS COMPARTIDOS
CLAUDE.md entero (1, 8, 12, 15, 27, 36, 39, 43, 49, 51-57). Español latino.
Cero allocs. Compilar con /home/claude/compile_fiel.sh (EXIT=0) antes de
reportar. Encargos DISJUNTOS por archivo — costura ajena se reporta, no se
toca. NO desplegar: el director integra y despliega. Determinismo: nada de
UnityEngine.Random en sim. Las decisiones fuera de contrato, explícitas.

## 6. DEFINICIÓN DE HECHO
- P: la estufa jamás baja un grado; la nevera jamás sube uno; compila.
- F: cada descubrimiento se lee y se cierra a mano; la cola cede por cierre;
  Escape cierra la ficha (no la pausa); compila.
- G: cantidades nuevas; Tolva cercana en seed 0 latiendo con flecha; el gesto
  de entrega enseñado en una línea; compila.
- M: minuto 0 con un solo fuego; el par térmico nace junto en la SalaFria;
  el lobby multi ofrece SEMILLA CERO compartida y un invitado ve nombres
  reales y puede cruzar recetas; compila.
