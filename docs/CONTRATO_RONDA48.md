# CONTRATO CONGELADO — RONDA 48: EL DESATASCO (Playtest 48)

Feedback de Cesar sobre el 47b: (1) Semilla Cero TRABADA tras "sílice + tinte",
no pudo cumplir la misión del tostado; (2) el multi en solitario sale roto
("StartHost devolvió false" + panel de invitado + mundo de ruido gris); (3) no
vio los menús nuevos; (4) los dos fuegos del inicio no se entienden, el
calentador de la "N roja" es horrible, el brasero del crisol es chiquitito y
no comunica qué entró como material.

## 0. DIAGNÓSTICOS CERRADOS (no re-investigar: citar y arreglar)

**D1 — EL ECLIPSE DE LA VETA (la causa del atasco, regla 51 otra vez).**
`Crisol.DecidirHornada` extrae del limo "la base MÁS ALTA cuya
`Universe.ExtraccionRaw` quepa en la cima" (Crisol.cs:1300), y la cima depende
SOLO del combustible (Crisol.cs:1131: rescoldo 120 sin combustible). El
override 1 de `Universe.AplicarOverridesSemillaCero` (Universe.cs:3580-3591)
fija arena=100 y clampa TODA banda en (100,120] a 95 — incluida la de la BASE
COMBUSTIBLE GARANTIZADA (base 3, banda natural 106±4). Resultado: a cualquier
cima ≥100 la arena eclipsa a la base 3 PARA SIEMPRE → sin turba → sin carbón →
la calcinación de la arena (banda 130, override 2) es INALCANZABLE a rescoldo
(120) → beat 4 imposible → todo el arco muerto detrás. La garantía G1
(Universe.cs:2729) cuantifica "existe base combustible extraíble"; el picker
toma solo el máximo. La trampa del beat 4 asumía que el jugador "YA tiene" el
combustible (docblock override 3) — nunca pudo tenerlo.

**D2 — EL "TINTE".** En la seed congelada 777002 la arena salió SOLUBLE del
sorteo (`SolubleBase`), el diseño dice "la arena no se disuelve — sin entrada"
(Universe.cs:3393) y nadie apagó el flag en los overrides. Arena + agua en el
mundo → `Solucion` de base0 → sin identidad real → nombre PROVISIONAL
"tinte …" (SubstanceKnowledge.cs:579). Viola la ley editorial (la sílice real
no se disuelve en agua) y el mandato "sin nombres pobres en Semilla Cero".

**D3 — EL MULTI QUE SE UNE A SÍ MISMO.** `SessionCoordinator.cs:365-370`: si
`StartHost()` da false, NO limpia (lobby de Steam vivo, `m_IsHosting` sin
resetear, NetworkManager a medias). `HandleLobbyJoined` (:381-408) no tiene NI
UNA guarda (ni CurrentState, ni hostSteamId==el mío, ni m_PendingHostAfterLobby)
→ el LobbyEnter_t del PROPIO lobby entra por la rama de invitado → StartClient
contra uno mismo → estado `Client` → el panel dice "Estás en el taller de otro"
(TallerSesionHud.cs:251). El transporte Steam vendorizado SIEMPRE devuelve true
en StartServer (SteamNetworkingSocketsTransport.cs:417): un StartHost false en
Steam significa EXCEPCIÓN dentro de HostServerInitialize de NGO (tragada por su
try/catch), NUNCA "el socket de Steam falló" — el mensaje actual miente. El
mundo de ruido gris = el mundo SÍ se creó, el avatar se despawneó en el
Shutdown interno, `TrySpawnRed` nunca pasa sus puertas
(AlkahestGameBootstrap.cs:481/488) y la cámara cae al centro del mundo
(384,144) = piedra maciza con grano (SimRenderer.cs:404-436). SOSPECHOSO del
fallo original: el .unity del 47b intercambió el orden de AlkahestSimSync y
AlkahestMaquinaSync (git show f760c29) y NGO spawnea por orden — MaquinaSync
puede estar spawneando ANTES de que exista `_sim.Universe`. `LastError` se
pinta sin caducidad (TallerSesionHud.cs:150) y solo se limpia en los 3 métodos
públicos: por eso conviven el error viejo y el panel de invitado.

**D4 — LOS MENÚS.** En la escena MULTI no existe título (TrySpawnRed nunca
llama Init) y el `DayCycle_PausaMulti` solo nace DESPUÉS de que la sesión
levante (AlkahestGameBootstrap.cs:493, tras las puertas :481/:488): en el
lobby, y ante cualquier fallo de sesión, NO HAY ni Escape ni AJUSTES. En la
escena clásica el título con AJUSTES sí se dibuja siempre (es 100% código).

## 1. ENCARGO V — LA VETA A LA VISTA (desatascar Semilla Cero)

Archivos de V: `Sim/Universe.cs` (SOLO dentro de AplicarOverridesSemillaCero y
constantes vecinas), `Sim/SimLevelBuilder.cs`, `Game/SemillaCero.cs`,
`Game/HintSystem.cs`.

### 1a. La veta de turba tallada en el muro
Cesar pt45: *"si hay que partir de carbón/madera esos serán los puntos de
partida"*. En modo Semilla Cero, `SimLevelBuilder` talla una **VETA VEGETAL
visible** en la piedra del cuarto íntimo: una veta orgánica e irregular de
`MatDe(3, Polvo)` (turba, color real pardo 94,66,41 de la tabla) EMBUTIDA en
el muro, alcanzable con el cincel desde el arranque, ~250-400 celdas (que
alcance para toda la partida y sobre: regla 44 — si puede agotarse en
experimentos, es una trampa; si el tamaño no da garantía, poner DOS vetas).
Forma de VETA (serpenteante, 3-6 celdas de grosor), no un rectángulo. Sitio:
decisión de V leyendo las coordenadas REALES del cuarto en SimLevelBuilder
(regla 39/47: nada de constantes por nombre), documentada; preferencia: pared
visible cerca del crisol, medio tapada por piedra para que TALLAR sea el verbo.
La turba tallada CAE (es Polvo) — comprobar que el jugador puede aspirarla con
el frasco normal.

### 1b. La escalera de fuegos completa (overrides, con el porqué en el código)
En `AplicarOverridesSemillaCero`:
- **Turba combustible tier bajo**: `_esCombustiblePorMaterial[MatDe(3,Polvo)]`
  = true con `_tempCombustibleRawPorMaterial` = **130** + parámetros de
  combustión persistente coherentes (arde mal y humeante, mejor que ceniza,
  peor que carbón — decidir números con el patrón del bloque Ash:3645 y
  DOCUMENTAR; respetar regla 55: reserva/paso/decaimiento mortales).
- **La escalera queda**: rescoldo 120→arena(100) · turba 130→arcilla(124±4) ·
  ceniza 145→caliza(136±4) · carbón tier1 (~165-190 natural)→sal(148/158±4).
  V IMPRIME EN EL LOG DE SEED la escalera completa resuelta (fuel→base por
  banda REAL de 777002, estilo regla 51: "un assert que no se puede leer no
  protege nada") y AÑADE un Assert de editor si alguna de las 5 bases queda
  sin camino (eclipse) o si la banda de arcilla no cabe bajo 130.
  OJO al jitter ±4: si la banda real de arcilla en 777002 sale >130, subir el
  tier de la turba justo por encima (documentado), nunca bajar la banda.
- **`SolubleBase[base0] = false`** (D2): la arena ya no se disuelve. Comprobar
  que la garantía G3 del solver (soluble+insoluble alcanzables) sigue en pie
  con las bases restantes (sal/arcilla son las solubles naturales) — si el
  Assert de G3 protesta, documentar y ajustar SOLO vía override de otra base
  soluble, jamás re-encendiendo la arena.
- La pregunta de la COLUMNA (beat 5.2, FlotaInsoluble) debe seguir teniendo
  respuesta alcanzable EN ESE PUNTO del arco: la turba flota (vegetal). V
  verifica la tabla de la Tolva/columna para turba y lo deja escrito.

### 1c. El arco que se completa de memoria (verificación obligatoria)
V recorre EN PAPEL el arco beat a beat con SOLO lo disponible en cada punto
(limo, agua, arena, veta de turba, cincel, prensa/columna/chispa/frío/ensayo
al destaparse) y escribe la secuencia en el informe: beat 4 = turba al brasero
(130) → calcina la arena en banda 130..170; la trampa del sobrecalentamiento
sigue viva por la vía del carbón (tier1 ≥170, Assert existente :3625). Si
CUALQUIER beat queda sin camino, es bloqueo de contrato: gritar, no parchear.

### 1d. Los textos que guían (español latino, regla 53)
- Consejo nuevo del arranque (HintSystem o el canal del arco, decisión de V):
  *"Esa veta parda del muro es TURBA: tállala (C), el brasero la come."*
  Aparece con el beat 4 (cuando el fuego propio deja de bastar), no antes.
- La línea del beat 4 del Maestro puede ganar UNA frase que apunte al brasero
  con lo tallado. El resto de textos NO se tocan.

## 2. ENCARGO R — EL MULTI QUE FALLA LIMPIO

Archivos de R: `Assets/FriendsLoop/Networking/SessionCoordinator.cs`,
`Assets/Alkahest/Net/TallerSesionHud.cs`, `Assets/Alkahest/Net/MaquinaSync.cs`,
`Assets/Alkahest/Game/AlkahestGameBootstrap.cs` (SOLO los puntos 2d/2e).

### 2a. El fallo de host cierra TODO (D3)
En la rama `StartHost()==false` (SessionCoordinator.cs:365-370): hacer lo
mismo que `Disconnect()` — `networkManager.Shutdown()` si quedó a medias,
`steamLobbyService.LeaveLobby()`, `CurrentLobbyId=0`,
`m_PendingHostAfterLobby=false`, RaiseSessionLeft, y recién entonces
`SetState(Offline)`. Además envolver la llamada a `networkManager.StartHost()`
en try/catch PROPIO: NGO se traga la excepción real — capturarla y meter
`tipo+mensaje` en `LastError` (es la única forma de distinguir la causa).
Mensaje reescrito: el fallo NO es "el host de Steam" — es "Steam creó la sala
pero la partida no pudo arrancar" + el detalle capturado.

### 2b. Blindar HandleLobbyJoined (D3)
Tres guardas al entrar (cualquiera corta el "taller de otro"):
`CurrentState != Offline` → return; `hostSteamId == mi SteamID` → return (con
log claro: "mi propio lobby, ignorado"); `m_PendingHostAfterLobby` → return.

### 2c. El error caduca y ofrece salida
`TallerSesionHud`: LastError con ventana temporal + "entendido" (mismo trato
que `_avisoLocal`), limpiado también al entrar en Hosting/Client. Y cuando hay
LastError en `DibujarDesconectado`, botón grande **"JUGAR SOLO EN ESTE PC"** →
`StartHost(LocalLoopback)` (el camino ya existe y funciona:
TallerSesionHud.cs:237). La sonda de Steam barata antes del StartHost Steam:
`SteamNetworkingSocketsTransport.IsSupported` + `SteamBootstrap.IsSteamReady`
(existen, nadie las consulta desde ahí).

### 2d. MaquinaSync a prueba de orden de spawn (D3, sospechoso #1)
`MaquinaSync.OnNetworkSpawn` (y cualquier OnNetworkSpawn de los Sync) debe
TOLERAR spawnearse antes de que `_sim.Universe` exista: patrón de reintento en
Update que ya usa AlkahestGameBootstrap — jamás una excepción por orden de
spawn (una excepción ahí tumba StartHost ENTERO y en silencio, es literalmente
el bug de Cesar). Auditar SimSync/SaberSync con la misma vara (regla 49: cada
promesa, su línea).

### 2e. La pausa existe desde el primer frame del lobby (D4)
En la escena MULTI, `DayCycle.ForzarDesbloqueoSesion()` se llama al DETECTAR
la escena (primera pasada de TrySpawn con `SimSync.EnEscena`), no tras el
avatar: Escape y AJUSTES funcionan en el lobby, antes y después de cualquier
fallo. Verificar que VOLVER AL TÍTULO desde el lobby (sin sesión) no intenta
desconectar nada raro. Además `TallerSesionHud` gana un botón "AJUSTES" chico
en el panel del lobby que abre el MISMO panel de DayCycle (sin duplicar UI:
exponer un `AbrirAjustes()` público si hace falta).

## 3. ENCARGO L — LOS DOS FUEGOS SE EXPLICAN (legibilidad térmica)

Archivos de L: `Game/Crisol.cs` (SOLO visual/rótulos), `Game/HeatPlate.cs`,
`Game/ChillStone.cs`, `Game/MaquinariaSprites.cs`.

### 3a. Fuera la "N roja horrible"
La resistencia zigzag roja del HeatPlate se RETIRA (documentando la idea
descartada en la cabecera, regla 15). Nuevo cuerpo: **losa de piedra con lecho
de brasas** — piedra oscura con celdas de brasa incrustadas que LATEN en
naranja-rojo profundo (mismo lenguaje que las brasas reales del pt39, familia
visual del taller, nada de neón). Apagada = brasas grises tenues; encendida =
latido cálido + borde superior con shimmer suave. La ChillStone conserva su
bandeja pero se armoniza (dientes de escarcha más finos, latido azul hielo al
trabajar). AMBAS con el mismo grosor/proporción para leerse como HERMANAS
(una familia: placas de zona).

### 3b. Cada fuego dice su oficio
Rótulo de proximidad (PlacaMundo existente, se desvanece solo):
- HeatPlate: "PLACA DE CALOR — entibia la ZONA de encima" (+ estado:
  "calentando · 64°").
- ChillStone: "PIEDRA GÉLIDA — enfría la ZONA de encima".
- Crisol: ya tiene los suyos; añadir al rótulo de reposo la distinción de
  oficio UNA vez por partida: "el crisol TRANSFORMA por hornadas; la placa
  solo CALIENTA la zona" (canal de consejos, no un cartel permanente).

### 3c. El brasero que se ve y la carga que se entiende
- Brasero del crisol MÁS GRANDE (al menos +60% de área visible del cesto,
  medida contra las proporciones reales del cuerpo en MaquinariaSprites — leer
  las medidas, regla 39), con el combustible cargado VISIBLE como pila.
- Rótulo de carga del crisol SIEMPRE que cambie el contenido: línea 1 la
  cámara ("cámara: arena de sílice · 62%"), línea 2 el cesto ("cesto: turba ×4
  → fuego hasta ~130°"). El jugador tiene que saber QUÉ entró como material y
  QUÉ como combustible sin adivinar — es literalmente el pedido de Cesar.
  Cero allocs en OnGUI: construir strings solo al cambiar el estado.

## 4. HECHOS COMPARTIDOS
CLAUDE.md entero (en especial 7/12/15/39/49/51/52/53/55/56). Español latino.
Cero allocs en hot paths. Compilación regla 53 antes de entregar. Los tres
encargos son DISJUNTOS por archivo — si necesitas tocar un archivo ajeno, NO
lo toques: repórtalo como costura pendiente en tu informe. Cada decisión fuera
de contrato, EXPLÍCITA en el informe. La verificación visual final (regla 52)
la hace el director EN VIVO con capturas.

## 5. DEFINICIÓN DE HECHO
- **V**: el log de seed imprime la escalera fuel→base completa sin eclipses;
  el arco es recorrible en papel beat a beat; la veta existe, se talla y se
  aspira; arena insoluble; compila.
- **R**: StartHost false termina en Offline limpio con mensaje veraz + botón
  "JUGAR SOLO EN ESTE PC"; imposible "taller de otro" en el propio lobby;
  MaquinaSync tolera cualquier orden de spawn; Escape/AJUSTES viven en el
  lobby multi; compila.
- **L**: ni un zigzag rojo; placas hermanas con oficio rotulado; brasero
  grande con pila visible; rótulo cámara/cesto; compila.
