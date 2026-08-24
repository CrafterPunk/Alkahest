# CONTRATO CONGELADO — SEMILLA CERO (Playtest 40)

Dos encargos paralelos: **G = guion** (el director del arco, los textos del
Maestro, el saber) y **M = mundo** (tapiados, overrides de autor, pantalla de
entrada, ceniza-combustible). Construye DISENO_SEMILLA_CERO.md (las cinco
enmiendas, aprobadas por Cesar) sobre las tres capas de la ronda de motor
(pt39: combustión persistente, pátina, partículas). Este contrato es LA
FUENTE ÚNICA del arco beat a beat.

## 0. LA TESIS

Hoy el juego arranca tirándote el taller entero encima: cinco estaciones,
doce consejos, encargos genéricos. Semilla 0 es la PRIMERA SESIÓN COMO
EXPERIENCIA DE AUTOR: un universo curado donde el milagro llega en el minuto
uno, cada máquina se destapa como respuesta a una pregunta del Maestro, el
primer fracaso está COREOGRAFIADO para enseñar más que el primer éxito, y el
final no es una pantalla de victoria sino un silencio con un vasito lleno.
Estructura: **milagro → comprensión → pequeño fracaso → comprensión mayor →
autonomía.** El modo de siempre (semilla aleatoria o tecleada) queda intacto
como MODO CAÓTICO.

## 1. EL ARCO BEAT A BEAT (la referencia; G lo dirige, M le da el mundo)

- **BEAT 1 — EL MILAGRO (minuto 0-2)**: el jugador nace junto a las fuentes
  y el crisol; TODO lo demás está tapiado (ver §3). Un solo consejo en
  pantalla: *"El caño turbio gotea LIMO PRIMORDIAL: sírvete (E) y viértelo
  en la boca del crisol."* Primera hornada a fuego propio → sale la primera
  arena. Banner: *"ALGO NUEVO — sedimento celeste, anotado en tu diario."*
  (nombre provisional, ver §2; NO se abre el rito de bautizo).
- **BEAT 2 — LA PRIMERA PETICIÓN**: el Maestro, a regañadientes: *"Tráeme 25
  de ese... 'sedimento celeste' tuyo."* (usa el nombre provisional con
  comillas y puntos suspensivos: le incomoda). Entrega en la Tolva como
  siempre. DESDE ESTE BEAT el alambique está montado y goteando a su vasito
  — nadie lo menciona JAMÁS en todo el arco (el anzuelo del final).
- **BEAT 3 — EL NOMBRE SE GANA**: el segundo pedido EMPIEZA por la
  exigencia: *"No pienso seguir diciendo 'sedimento celeste'. Ponle
  nombre."* → se abre el rito (NamingUi) como petición del personaje. El
  pedido continúa con el nombre puesto. (Paralelo: si el jugador manipuló
  la sustancia 3+ veces antes de este beat, el rótulo "T — bautizar" ya se
  ofrecía discreto; el beat lo vuelve obligatorio con teatro.)
- **BEAT 4 — EL FRACASO FORENSE**: el Maestro pide "más de eso, pero
  TOSTADO" y la pista sugiere alimentar el brasero. LA TRAMPA DELIBERADA:
  en Semilla 0 la banda de calcinación del sedimento es ESTRECHA y el
  brasero recién alimentado se pasa de largo — la muestra se ennegrece por
  fases (pátina + tinte), chisporrotea (partículas pt39), suelta humo que
  tizna la bóveda y muere en CENIZA gris en la cubeta. El Maestro: *"Eso es
  ceniza. Interesante... apunta a qué temperatura muere. Y guárdala: la
  ceniza también arde, mal, pero arde."* → nota forense automática en la
  ficha del diario ("cerca de ~X° se destruye") + LA CENIZA ES COMBUSTIBLE
  tier 0.5 (enciende el brasero, dura poco) → el reintento con fuego más
  medido sale bien: CALCINADO entregado.
- **BEAT 5 — LAS PREGUNTAS (comprensión mayor)**: cuatro pedidos-pregunta,
  cada uno destapa su sala al ACEPTARSE (el texto del pedido ES la
  pregunta; jamás se formulan como encargo de cantidad):
  1. *"¿Puedes hacerlo MÁS DURO?"* → cae el tapiado de la PRENSA
     (compactar: idea ESTADO/proceso).
  2. *"¿Por qué esto queda ENCIMA?"* → se abre la alcoba de la COLUMNA
     (idea DENSIDAD).
  3. *"¿Esto CONDUCE?"* → el BANCO DE CHISPA (idea CONDUCTIVIDAD).
  4. *"¿DE VERDAD aguanta?"* → el ENSAYO junto a la Tolva (idea
     TEMPERATURA, cerrando el círculo del fracaso del beat 4).
  Semilla 0 enseña SOLO temperatura, estado, densidad, conductividad; la
  quinta idea (la historia del proceso: templar vs recocer, el orden de
  prensa y horno) queda FÍSICAMENTE presente pero jamás mencionada.
  Solubilidad, cohesión, fragilidad: presentes, descurriculadas.
- **BEAT 6 — EL FINAL ABIERTO**: tras el último pedido: *"No necesito nada
  más por hoy. ...Pero queda limo."* — y NADA más. Sin encargo nuevo, panel
  de encargos vacío. El vasito del alambique, lleno tras toda la sesión, es
  la única pregunta pendiente que nadie formuló. CONTADOR DE AUTONOMÍA
  (local, sin red): hornadas, mudanzas, bautizos, aspirados POSTERIORES al
  final abierto — se loguea a consola cada acción y un resumen por minuto,
  y se muestra como línea discreta en el panel F3. Es la métrica reina.

## 2. ENCARGO G — el director del arco y el saber

Archivos de G: `Game/SemillaCero.cs` (**NUEVO** — todo el director vive
aquí), `Game/SubstanceKnowledge.cs`, `Game/HintSystem.cs`,
`Game/OrderSystem.cs`, `Game/OrdersHud.cs`, `Game/NamingUi.cs` (solo si el
rito necesita el gancho "pedido por el Maestro").

- **SemillaCero.cs**: máquina de estados de los beats (enum + estado
  serializable en el propio MonoBehaviour; sin red — gate duro
  `if (SimSync.EnEscena) return;` en su spawn: Semilla 0 es SOLO de la
  escena un jugador). Escucha lo que ya existe (SubstanceKnowledge,
  OrderSystem, eventos de hornada del Crisol vía polling barato de estado
  público — NO tocar Crisol) y ordena: textos del Maestro, pedidos
  scriptados, llamadas de destape `SimLevelBuilder.DestaparSala(sim, n)`
  (API de M, §3 — congelada aquí, G codea contra ella), contador de
  autonomía. Los textos del Maestro EXACTOS del §1 (español latino neutro,
  tuteo; puede refinarlos G manteniendo intención y filo).
- **Pedidos scriptados**: en modo Semilla 0, OrderSystem NO genera pedidos
  procedurales — expone (o G le añade) una vía `EncolarPedidoGuiado(...)`
  para que SemillaCero dicte la secuencia del §1 con sus textos, requisitos
  y recompensas (cantidades pequeñas: 25 del beat 2, 15-20 el resto). Las
  estrellas siguen funcionando. El panel de encargos muestra el pedido
  activo como pregunta, tal cual, sin reformatear a "encargo de cantidad".
- **Nombre provisional (enmienda 1)**: en SubstanceKnowledge, al descubrir
  una sustancia SIN nombre, generar provisional **estado + color
  percibido**: tabla de estados (Polvo→"sedimento", Calcinado→"tueste",
  Fundido→"colada", Templado→"lágrima", Recocido→"pan", Compacto→"laja",
  Ceramico→"loza", Solucion→"tinte"; clásicos fuera de base-estado usan su
  nombre de siempre) + color más cercano por distancia RGB a una tabla de
  12: celeste, ámbar, carmesí, oliva, violeta, gris, dorado, cobrizo,
  esmeralda, turquesa, hueso, pardo. Vale para TODO el juego (modo caótico
  incluido: mejor que "Base2Polvo" en cualquier universo). El bautizo (T)
  sustituye el provisional; el Maestro usa siempre el nombre vigente.
- **Contador de manipulaciones**: por sustancia (aspirar/verter/hornada
  que la toca), en SubstanceKnowledge; a la 3ª, el rótulo "T — bautizar"
  se vuelve visible para esa sustancia (discreto, sin banner).
- **Nota forense (enmienda 2)**: al presenciar la destrucción de una
  sustancia conocida (evento Boil/atestiguado de X→Ash), añadir a su ficha
  del diario: *"cerca de ~N° se destruye"* con la temperatura ambiente de
  la celda del evento redondeada a decenas. Vale para todo el juego.
- **Consejos**: en modo Semilla 0, HintSystem carga una lista CURADA de 5-6
  consejos alineados a los beats (el del beat 1 es el primero) en vez de
  los 12 generales; en caótico, la lista de siempre. El sistema (12s, N,
  H, releer en diario) no cambia.

## 3. ENCARGO M — el mundo curado

Archivos de M: `Sim/SimLevelBuilder.cs`, `Sim/Universe.cs`,
`Game/Crisol.cs` (ceniza tier 0.5), `Game/AlkahestGameBootstrap.cs` y la
pantalla de título (donde viva: "Entrar al taller" hoy), `Game/Alambique.cs`
(solo si el goteo al vasito necesita asegurarse desde el minuto 0).

- **La semilla de autor**: `Universe.SemillaCero = 777001u` (constante
  pública). Modo Semilla 0 = universo generado con ESA semilla + pasada de
  overrides `Universe.AplicarOverridesSemillaCero()` DESPUÉS de la
  generación normal (documentando cada override con su porqué):
  1. La base ganadora del solver se extrae a fuego propio (tier 0), banda
     generosa — el milagro del beat 1 no puede fallar.
  2. Su banda de calcinación ESTRECHA (la trampa del beat 4: brasero
     recién alimentado la sobrepasa) y por encima de la banda → Ash
     (verificar que el camino sobrecalentamiento→Ash existe en el lattice;
     si no, crearlo para Semilla 0 vía la tabla, no con un if especial).
  3. Un calcinado combustible garantizado (el solver ya lo garantiza —
     verificar con el log "combustible=base N").
  4. Colores legibles: la base del beat 1 en la familia CELESTE (el
     provisional del guion dice "sedimento celeste" — M la tiñe si la seed
     no coopera, vía override de color, no de texto).
  Si 777001 no coopera con 1-3 tras los overrides, M prueba semillas
  vecinas (777002...) y CONGELA en la constante la que funcione,
  documentando cuál y por qué.
- **Tapiados por pregunta**: en modo Semilla 0, SimLevelBuilder TAPIA con
  mampostería de obra (indestructible al cincel, como toda obra) las salas
  de prensa, alcoba de columna, banco de chispa y atrio del ensayo — muros
  visualmente de "obra tapiada" (patrón distinto al muro normal: que se
  LEA como puerta condenada). API CONGELADA (G codea contra ella):
  `public static void TapiarSalasSemillaCero(AlkahestSim sim)` y
  `public static void DestaparSala(AlkahestSim sim, int sala)` con 0=prensa,
  1=columna, 2=chispa, 3=ensayo. El destape borra el tapiado (borrar+
  despertar chunks; el polvo de derrumbe lo regala ParticulasFx solo).
  Las máquinas tapiadas NO spawmean sus sprites/focos hasta el destape (o
  quedan ocultas tras el muro si es más simple — decisión de M
  documentada; lo que no puede pasar: chapa "E — usar" visible a través
  del tapiado, ni glow de affordance).
- **Pantalla de entrada**: botón principal **"SEMILLA CERO — tu primer
  taller"** (arco guiado) y debajo el campo de semilla de hoy con
  **"MODO CAÓTICO — entrar con esta semilla"** (vacía = aleatoria,
  comportamiento actual sin arco). Un flag estático simple (p. ej.
  `AlkahestGameBootstrap.ModoSemillaCero`) que G lee para decidir si
  spawnear SemillaCero y que M usa para tapiar/override. En MULTI no
  existe Semilla 0 (ni botón ni flag).
- **Ceniza combustible tier 0.5**: en Crisol, Ash alimenta el brasero:
  enciende (combustión persistente pt39 sobre las celdas de ceniza del
  cesto) pero con poder mínimo — sube el fuego apenas por encima del
  propio, MENOS que el tier 1, y dura poco. Suficiente para que el
  reintento del beat 4 con "fuego medido" funcione: la banda estrecha del
  sedimento se alcanza con ceniza y NO se sobrepasa. M calibra
  Universe/Crisol para que ese gesto exacto salga bien y lo documenta.
- **El vasito**: verificar que el alambique del arranque gotea al vasito
  desde el minuto 0 en Semilla 0 sin intervención (si el flujo actual
  necesita al jugador, M lo automatiza SOLO en Semilla 0).

## 4. HECHOS COMPARTIDOS

- CLAUDE.md manda entero; en particular regla 7 (determinismo, salts
  únicos nuevos con grep previo), 15 (comentar, no borrar), 48, 52, 53
  (español latino), 54 (evidencia forense) y 55 (procesos mortales y
  despiertos). Cero allocs por frame. IMGUI, sprites por código.
- Nada de Sim/ (salvo SimRenderer/SimLevelBuilder) referencia UnityEngine
  ni Game/. El arnés (`Tools~/BenchSim/Harness.cs`) debe seguir compilando
  — los overrides de Universe no pueden meterle dependencias de Unity.
- El modo caótico y el MULTI no cambian de comportamiento: todo lo nuevo
  va detrás del flag Semilla 0 (el nombre provisional y la nota forense
  del encargo G son las DOS excepciones deliberadas: valen siempre).
- El OTRO encargo corre EN PARALELO en el mismo árbol: errores de compilación
  en archivos ajenos (o contra la API congelada de §3 aún no escrita) son
  transitorios — repórtalos solo si persisten en tu compilación FINAL.
- Verificación regla 53 (rig ya montado):
  ```
  cd /home/claude/alkahest
  CSC=$(find /usr/lib/dotnet /usr/share/dotnet -name csc.dll 2>/dev/null | head -1)
  SRC=$(find Assets -name '*.cs' ! -path '*/Editor/*')
  REFS=$(for f in /home/claude/unityrefs/*.dll; do case "$f" in *Alkahest.Runtime.dll) ;; *) printf ' -r:%s' "$f";; esac; done)
  dotnet "$CSC" -nologo -nostdlib+ -noconfig -t:library -langversion:9.0 \
    -define:UNITY_64 -define:UNITY_2023_1_OR_NEWER -define:NETCODEGAMEOBJECTS -define:STEAMWORKSNET \
    -out:/tmp/check_$$.dll $REFS $SRC
  ```

## 5. DEFINICIÓN DE HECHO

- **G**: compila; el arco entero es jugable de memoria: milagro → pedido a
  regañadientes → exigencia de nombre → trampa del tostado con ceniza,
  nota forense y reintento → cuatro preguntas que destapan salas → final
  abierto con contador de autonomía activo. Textos del Maestro con filo,
  jamás tutoriales planos.
- **M**: compila; la semilla congelada cumple 1-4 de §3 (verificado con
  los logs del solver y un arranque mental del arco); los tapiados se leen
  como puertas condenadas y caen limpios; la pantalla de entrada ofrece
  los dos modos; la ceniza enciende "mal pero enciende"; el caótico y el
  multi quedan intactos.
- Ambos: resumen final con archivos tocados, decisiones fuera de contrato
  EXPLÍCITAS, y deudas de integración para Fable.
