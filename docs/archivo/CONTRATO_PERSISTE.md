# CONTRATO CONGELADO — LO QUE PERSISTE (Playtest 25)

Única fuente de verdad compartida entre los TRES encargos paralelos:
A = Sim (el corazón químico), B = Máquinas, C = Pedidos/Ensayo/Patentes.
Nada de aquí se cambia sin volver al director. Si necesitas algo que no está,
resuélvelo DENTRO de tus archivos o anótalo como pregunta al final — nunca
inventes API en el espacio de otro encargo. El diseño completo que este
contrato implementa: `docs/DISENO_LO_QUE_PERSISTE.md` (léelo primero).

## 0. LA VISIÓN EN UNA FRASE

Cada semilla es una pregunta — ¿qué puede durar aquí? — y el laboratorio es
cómo el jugador le arranca la respuesta al mundo. El jugador no aprende
recetas: descubre QUÉ PERSISTE ante calor, frío, presión, agua y chispa, y
patenta los procedimientos que lo consiguen.

## 1. HECHOS COMPARTIDOS (no tocar, solo usar)

- Grid `CellGrid.W=768, H=288`; temperatura raw: `raw = (°C+120)/2`; ambiente
  raw 70 (=20°C); agua hierve ~raw 160 (según seed).
- Determinismo: SOLO `XorShift.FromCell(tick,x,y,salt)`; sal propia por uso
  nuevo; regla 21 (castear `(uint)` toda sal que mezcle constante+campo).
- Cero allocs en el hot path de SimStepper. IMGUI para UI; sprites por código.
- Comentarios en español, voz de bitácora (POR QUÉ, no qué).
- Regla 33: `Universe.Leyes[i] ↔ Reactions.At(i)` intacta. Las leyes de
  contacto EXISTENTES no se tocan; el retículo de estados es un sistema
  APARTE (tablas + máquinas), no reacciones nuevas.
- Regla 48: cada propiedad/estado nuevo lleva VERBO visible y CONSUMIDOR real.
- La Marea (playtest 24) fue RETIRADA en esta misma ronda: si encuentras
  restos de `Marea`/`Rocio`/`MareaDirector`, es un error — repórtalo, no
  construyas encima.
- La CRIATURA y el CAPULLO quedan APARCADOS: sus archivos no se tocan y NO se
  spawnean en esta versión (B los comenta en el bootstrap, estilo regla 15).

## 2. IDENTIDADES NUEVAS (exactas, compartidas)

En `MaterialId` (Universe.cs):

```csharp
public const byte Limo = 17;        // el material primigenio (líquido turbio)
public const byte BaseEstado0 = 18; // primera celda del bloque bases×estados
public const int BasesCount = 5;    // materias base por semilla
public const int Count = 58;        // 18 + 5*8
```

Estados (enum en `Alkahest.Sim`, archivo Universe.cs):

```csharp
public enum EstadoMateria : byte
{
    Polvo = 0,     // estado natal (Powder)
    Fundido = 1,   // líquido incandescente (Liquid, brilla)
    Templado = 2,  // enfriado RÁPIDO en el mundo: duro (StaticSolid)
    Recocido = 3,  // enfriado LENTO dentro del crisol: dúctil (StaticSolid)
    Compacto = 4,  // prensado (StaticSolid)
    Ceramico = 5,  // compacto cocido: el techo de resistencia (StaticSolid)
    Calcinado = 6, // tostado sin fundir (Powder; a veces combustible)
    Solucion = 7,  // disuelto en agua (Liquid, agua teñida del color de la base)
}
```

Mapeo fijo: `id = BaseEstado0 + base*8 + (byte)estado` (base 0..4).
Helpers estáticos en `MaterialId` (los implementa A, los usan todos):

```csharp
public static bool EsBaseEstado(byte id);            // 18..57
public static int BaseDe(byte id);                    // 0..4
public static EstadoMateria EstadoDe(byte id);
public static byte MatDe(int baseIdx, EstadoMateria e);
```

## 3. API PÚBLICA DE UNIVERSE (la implementa A; B y C SOLO la consumen)

```csharp
// --- tablas de propiedades (por MaterialId, válidas para 0..Count-1) ---
public byte UmbralPersistenciaRaw(byte id); // temp raw máxima que aguanta sin transformar/arder
public RespuestaPrensa Prensa(byte id);     // enum: Compactar, Reventar, Escupir, Resistir, Nada
public byte Conductividad(byte id);         // 0 = no, 1 = débil, 2 = conduce
public bool SolubleEnAgua(byte id);         // solo puede ser true para estados Polvo/Calcinado
public bool EsCombustible(byte id);         // ¿sirve de combustible del crisol?
public byte TempCombustibleRaw(byte id);    // temp que alcanza el crisol quemándolo (0 si no combustible)

// --- umbrales del retículo (por base 0..4) ---
public byte FusionRaw(int baseIdx);         // Polvo -> Fundido (a esta temp o más)
public byte CalcinacionRaw(int baseIdx);    // Polvo -> Calcinado (banda: >= esta y < FusionRaw, sostenido)
public byte CeramizaRaw(int baseIdx);       // Compacto -> Ceramico (o 0 si esta base no ceramiza)
public byte SolidificaRaw(int baseIdx);     // Fundido se solidifica por debajo de esta temp

// --- el limo ---
public int PesoEnLimo(int baseIdx);         // pesos de separación (suman 100)

// --- la garantía (calculada por el solver en Create) ---
public byte GanadorGarantizado;             // matId del persistente garantizado de esta seed
public byte TempEnsayoCalorRaw;             // temp del pedido CALOR: <= umbral del ganador - 10 raw
public int BaseCombustibleGarantizada;      // baseIdx cuyo Calcinado es combustible alcanzable a tier 0
```

`RespuestaPrensa` es un enum nuevo en `Alkahest.Sim` (archivo Universe.cs):
`Nada, Compactar, Reventar, Escupir, Resistir`.

Constante compartida del crisol (en Universe.cs, la lee B):

```csharp
public const byte CrisolTier0Raw = 118; // el rescoldo propio del crisol, sin combustible (~116°C: hierve agua y limo, no funde nada)
```

## 4. ENCARGO A — SIM (archivos: `Sim/Universe.cs`, `Sim/SimStepper.cs`, `Sim/SimLevelBuilder.cs`)

### 4.1 Materiales

**Limo (17)**: Liquid, turbio pardo-grisáceo `(94, 86, 72)`, jitter 12,
densidad entre agua y aceite, fluidity 2, patron Motas (fijo — el primigenio
se reconoce entre universos, misma excepción documentada a la regla 17 que el
vocabulario), sin transiciones de MaterialDef (su separación es especial, ver
4.3). NombreComun lo da C ("limo": es vocabulario, el Maestro lo conoce).

**Las 40 variantes base×estado**: generadas EN BUCLE en `Create` desde las
tablas (cero código por-material). Arquetipos por estado según el enum de §2.
COLOR: cada base sortea un tono propio (misma maquinaria de sorteo de color
de lo innominado, lista nueva — las 5 bases en estado Polvo se añaden al
sorteo de firma visual como innominados nuevos); los demás estados DERIVAN
del tono base con reglas FIJAS entre universos (dos ejes de legibilidad: la
base se reconoce por el tono, el estado por el tratamiento):
- Fundido: tono saturado + brillo alto + `emitsGlow`, patron Pulso.
- Templado: tono + blanco 25%, borde Neto, patron Liso (liso vítreo).
- Recocido: tono + gris 20%, patron Vetas suaves.
- Compacto: tono oscurecido 30%, jitter bajo, patron Celdas prieto.
- Ceramico: tono desaturado + pálido, borde Neto, patron Liso.
- Calcinado: tono oscurecido 50% hacia carbón, patron Motas.
- Solucion: color del AGUA teñido 60% hacia el tono base (tech del tinte).

Transiciones vía MaterialDef donde el motor ya sabe (las dirigidas van en el
crisol de B, ver §5.1):
- `Polvo.meltsAt = FusionRaw(base)`, `meltsInto = Fundido` — así el calor del
  MUNDO (no solo el crisol) también funde: un incendio funde polvos sueltos.
- `Fundido.freezesAt = SolidificaRaw(base)`, `freezesInto = Templado` — TODO
  enfriamiento en el mundo es TEMPLE (rápido). El recocido solo existe
  dentro del crisol (B). Este es el gesto central del orden-importa.
- `Solucion.boilsAt` = ebullición del agua de la seed, `boilsInto = Polvo` de
  su base — evaporar PRECIPITA (el agua se va implícita; legible y barato).
- Nada más tiene transiciones de MaterialDef en v1.

### 4.2 Tablas y sorteo (misma gramática/filosofía que las leyes)

Sortear por seed, con XorShift del `System.Random rng` de Create como el
resto: vector por base (fusión raw 130..170, calcinación raw 100..125 —
SIEMPRE < fusión —, solidificación, densidades bien repartidas — al menos
una base más densa que el agua y una menos, para que la Columna estratifique
—, conductividad, solubilidad: 2-3 de las 5 bases solubles en agua) y
MODIFICADORES DE ESTADO con TENDENCIA FIJA entre universos (vocabulario,
regla 17) y magnitud por seed:
- Compacto: umbral térmico +10..20 raw, Prensa=Resistir, densidad +30%.
- Ceramico: el umbral MÁS ALTO de su base (+25..40 raw sobre compacto), no
  arde, no conduce, Prensa=Resistir.
- Templado: Prensa=Reventar (frágil). Recocido: Prensa=Compactar (dúctil).
- Calcinado: umbral +15..30 raw, densidad −30%, combustible en 1-2 de las 5
  bases (`TempCombustibleRaw` = raw 150..175, el tier 1 que abre la fusión).
- Solucion: Conductividad = 2 si su base era conductora o "iónica" (sorteo),
  si no 0. (La solución conductora: el análogo salmuera.)
- Polvo: Prensa=Compactar. Fundido: Prensa=Escupir. Limo/agua: Escupir.

### 4.3 SimStepper — SOLO dos procesos nuevos, muestreados

1. **Disolución** (sal `SalDisolucion = 237`): celda de AGUA con vecino
   ortogonal Polvo/Calcinado soluble → muestreo 1/8 (patrón de MaybeReact),
   20% por muestreo: el agua → `Solucion` de esa base, el polvo → Empty
   (1+1=1, masa simple y legible). En `ProcessLiquid` solo para Water, coste
   acotado.
2. **Separación del limo** (sal `SalLimoSeparacion = 239`): celda de Limo con
   `temp >= LimoSeparaRaw = 150` (60°C: alcanzable con el rescoldo tier 0) →
   se convierte en el POLVO de la base sorteada por
   `XorShift.FromCell(0, x, y, (uint)seed ^ SalLimoSeparacion)` con los pesos
   `PesoEnLimo` (deterministo POR CELDA: hervir dos veces el mismo sitio da
   lo mismo). En `ProcessLiquid`, caso Limo, antes del flujo.

NADA MÁS cambia en SimStepper. Ni fragilidad al frío (v2) ni presión de
campo (nunca).

### 4.4 El solver de garantía (en `Create`, tras sortear tablas)

Búsqueda en anchura sobre los ~41 nodos (Limo + 40 variantes) con las
operaciones {separar, fundir@tier, templar, recocer, prensar, calcinar@tier,
ceramizar@tier, disolver, evaporar}, donde @tier respeta la escalera térmica:
tier0 = `CrisolTier0Raw`, tier1 = mejor `TempCombustibleRaw` alcanzable con
lo ya alcanzado. El solver DEBE garantizar (reintentando el sorteo de tablas
completo hasta 50 veces — es tabla pura, microsegundos — y si agota,
CLAMPEANDO la última):
1. ≥1 base con `CalcinacionRaw <= CrisolTier0Raw` cuyo Calcinado es
   combustible (→ `BaseCombustibleGarantizada`): la escalera
   hervir→calcinar→combustible→fundir existe en TODA seed.
2. ≥1 variante alcanzable con `UmbralPersistenciaRaw >= TempEnsayoCalorRaw + 10`
   (→ `GanadorGarantizado`); `TempEnsayoCalorRaw` se fija en raw 165..180 y
   por DEBAJO del umbral del ganador.
3. ≥1 variante alcanzable conductora (para el pedido CHISPA) y ≥1 base
   soluble + ≥1 insoluble (para PUREZA y FLOTA).
`Debug.Assert` + línea en el log de seed (formato del log de leyes):
`"Persistencia: ganador=<id> a N pasos, combustible=base K (verificado)"`.

### 4.5 SimLevelBuilder — el laboratorio (REUTILIZA el cuarto íntimo)

El plano del cuarto (248..357 × 168..209) NO se redibuja: se reamuebla.
- El caño de NUTRIENTE pasa a ser el caño de LIMO: constante
  `CanoLimoY = CanoNutrienteY` (misma boca, otro material — documentar).
  El agua se queda donde está.
- La CUNA y el CAPULLO no se siembran (criatura aparcada — quitar/comentar
  sus llamadas de siembra si las hay en el builder; los emplazamientos
  quedan).
- Constantes públicas nuevas (emplazamientos DENTRO del cuarto, suelo
  y=CuartoY0+2, elige números exactos con ≥6 celdas de holgura entre
  aparatos y documenta el porqué de cada sitio):
  `CrisolX`, `PrensaX`, `BancoChispaX`, `EnsayoPlintoX` (este junto a la
  boca del pasillo), y la COLUMNA DE ENSAYO: `ColumnaX0` (pared izquierda),
  `ColumnaAncho = 5` (3 de hueco + 2 muros de Crystal), `ColumnaAlto = 22`,
  de pie sobre el suelo del cuarto, muros de `MaterialId.Crystal`
  (StaticSolid: no cae, regla 7), abierta por arriba.
- **El pasillo a la Tolva se PRE-CARVA** (la puerta de esta dirección está
  abierta desde el minuto 1: los pedidos son el tutorial, no hay gate de
  cavado): túnel de 6 de alto desde la pared derecha del cuarto hasta la
  boca de la Tolva, mismo trazado que el jugador cavaría.

## 5. ENCARGO B — MÁQUINAS (archivos: `Game/Crisol.cs` [nuevo], `Game/Prensa.cs` [nuevo], `Game/BancoChispa.cs` [nuevo], `Game/AlkahestGameBootstrap.cs`)

Estilo obligatorio: sprites por código vía `MaquinariaSprites` (latón +
carboncillo, mismo idioma que HeatPlate/ChillStone), foco por `MachineFocus`
(solo el aparato más cercano responde a E), rótulos de mundo con `UiStyles`
(guardas `DayCycle.InputLocked || DayCycle.HudSilenciado`), sondeos con
acumulador — NUNCA escaneos por frame. Los tres aparatos son `IMovible`
(mudanza V/R gratis). Posiciones: SOLO las constantes de SimLevelBuilder §4.5.

### 5.1 Crisol

La máquina central. Región CUBETA (interior, ~7x5 celdas sobre su base) y
región TOLVA DE COMBUSTIBLE (lateral, ~3x3).
- **Temperatura**: sin combustible, empuja su cubeta hacia
  `Universe.CrisolTier0Raw` (118: hierve, no funde) — el "rescoldo propio",
  el crisol NUNCA está muerto (anti-trampa, regla 44). Con combustible en la
  tolva (celdas con `EsCombustible`), lo consume 1 celda/~6s y empuja hacia
  `TempCombustibleRaw` de ese material. El empuje térmico calca el patrón de
  HeatPlate (objetivo + rampa, `Paint` de temperatura por sondeo).
- **Transformaciones dirigidas del contenido** (sondeo ~0.8s sobre la
  cubeta):
  - Polvo con `temp >= FusionRaw(base)` → Fundido (el mundo también lo hace
    vía meltsAt; el crisol solo ACELERA el sondeo — no dupliques lógica,
    deja que ApplyPhase trabaje y limítate a calentar).
  - Polvo sostenido ≥ ~20s en banda `[CalcinacionRaw, FusionRaw)` →
    Calcinado (esto SÍ es del crisol: el mundo no calcina). Un contador por
    sondeo basta, sin arrays por celda: exige que el conjunto de la cubeta
    esté en banda.
  - Compacto sostenido ≥ ~20s con `temp >= CeramizaRaw(base)` (si ≠ 0) →
    Ceramico.
  - **Recocido**: cuando el combustible se agota y la cubeta ENFRÍA por
    debajo de `SolidificaRaw` DENTRO del crisol, el Fundido de la cubeta →
    Recocido (no Templado: enfriar lento = recocer; el freezesInto=Templado
    del mundo no aplica porque el crisol lo transforma ANTES en su sondeo al
    cruzar la banda — documenta esta carrera y gánala con margen de +4 raw).
- Cada transformación llama `Hornada.RegistrarOp("crisol", entrada, salida,
  condicion)` (§6.3; condicion = "tier0"/"combustible:<nombre>"/"lento").
- Rótulo con verbo: "hirviendo", "calcinando", "fundiendo", "enfriando
  despacio — recocerá", "cargadme combustible (E)".

### 5.2 Prensa

Región LECHO (~5x3). Con E (y foco), la mandíbula cae (anim 0.5s) y aplica a
cada celda del lecho `Universe.Prensa(mat)`:
- Compactar → la celda pasa al estado Compacto de su base.
- Reventar → pasa al estado Polvo (el templado revienta: cristal frágil).
- Escupir → la celda se DESPLAZA a la celda libre lateral más cercana fuera
  del lecho (los líquidos no se comprimen: el desplazamiento visible).
- Resistir → nada, y el rótulo lo dice ("resiste la prensa": dato ganado).
- Nada → materiales ajenos (piedra del suelo, etc.): intocados.
Registra UNA op por prensada con el material dominante del lecho. Cooldown
~2s con la mandíbula arriba de nuevo.

### 5.3 Banco de chispa

Región RANURA (~3x2) entre dos bornes + una lámpara encima. Con E: lee el
material dominante de la ranura, enciende la lámpara según
`Universe.Conductividad`: 0 = nada (y rótulo "ni un parpadeo"), 1 = brillo
tenue, 2 = pleno. NO transforma nada: es el instrumento de análisis puro.
Anota la observación en el diario vía el hook de C (§6.4):
`SubstanceKnowledge.RegistrarObservacionPropiedad(matId, "encendió la lámpara" / "la lámpara ni parpadeó")`.

### 5.4 Bootstrap

- Comentar (estilo regla 15, con nota "criatura aparcada — LO QUE PERSISTE")
  los spawns de Criatura y Capullo.
- Spawnear Crisol, Prensa, BancoChispa en sus constantes de SimLevelBuilder,
  y los sistemas de C con las firmas EXACTAS de §6.5.
- El caño que emitía Nutrient pasa a emitir `MaterialId.Limo` (mismo
  SpawnCanoBasico, otro material — respeta la regla 47: verifica que usas la
  constante del cuarto íntimo, no la del taller clásico).

## 6. ENCARGO C — PEDIDOS, ENSAYO Y PATENTES (archivos: `Game/OrderSystem.cs`, `Game/Order.cs`, `Game/EnsayoMaestro.cs` [nuevo], `Game/Hornada.cs` [nuevo], `Game/JournalHud.cs`, `Game/SubstanceKnowledge.cs`, `Game/DayCycle.cs`, `Game/HintSystem.cs`)

### 6.1 Pedidos por propiedad

`OrderType` gana valores nuevos (los viejos NO se borran, modo clásico):
`Pureza` (N celdas del MISMO Polvo base — cualquiera), `AguantaCalor` (se
resuelve en el Ensayo, no en la Tolva), `Conduce` (ídem), `FlotaInsoluble`
(celdas cuya densidad < agua Y no solubles — se comprueba por tabla en la
Tolva, teatro en v2), `Procedimiento` (tener ≥1 patente registrada al
entregar cualquier otra celda — se autocompleta y paga).

`GenerateOrdersPersiste()` (nuevo; DayCycle lo llama en vez de
GenerateOrdersPivot): emite EL ARCO FIJO de 5, de uno en uno (el siguiente
aparece al completar el anterior — el arco ES el tutorial):
1. Pureza 25 celdas — "Separadme el limo: traedme una sola de sus arenas, pura."
2. AguantaCalor — "Algo que aguante el rojo del crisol sin ceder."
   (temp = `Universe.TempEnsayoCalorRaw` — calibrada por el solver, JAMÁS un
   número inventado: se acabaron los pedidos imposibles.)
3. Conduce — "Algo que encienda mi lámpara."
4. FlotaInsoluble 20 celdas — "Algo que flote en el agua sin deshacerse en ella."
5. Procedimiento — "El cómo del nº2, por escrito en vuestro libro."
Recompensas crecientes; la 5 paga el doble (el conocimiento vale más que la
sustancia).

### 6.2 El Ensayo del Maestro (`EnsayoMaestro.cs`)

Plinto en `SimLevelBuilder.EnsayoPlintoX` (junto a la boca del pasillo).
Vierte encima tu muestra y pulsa E con un pedido AguantaCalor/Conduce
activo:
- **AguantaCalor**: el plinto CALIENTA la muestra a `TempEnsayoCalorRaw`
  durante ~5s A LA VISTA (el material brilla — teatro físico) y luego cuenta
  supervivientes del material dominante: ≥60% intactas = pedido cumplido.
  ESTRELLAS por margen real: sobrevivió a la temp pedida = ★; su
  `UmbralPersistenciaRaw` supera la pedida en ≥15 raw = ★★; en ≥30 = ★★★
  (Favor x1/x1.5/x2). El espectro de soluciones, instaurado desde el pedido 2.
- **Conduce**: consulta `Universe.Conductividad` del dominante: 2 = cumplido
  (★★ fijo), 1 = medio Favor (★ — "condujo a duras penas").
- Si falla: el pedido NO se consume; el rótulo dice CÓMO murió la muestra
  ("fundió a mitad del ensayo") — el fallo devuelve información, nunca solo
  un no.

### 6.3 Hornada (`Hornada.cs` — API congelada, B la llama)

```csharp
public static class Hornada
{
    public static void RegistrarOp(string maquina, byte matEntrada, byte matSalida, string condicion);
}
```

Ring buffer estático de las últimas 8 ops (struct, sin allocs por op — los
strings de condicion son literales/cacheados). v0 GLOBAL, no por-lote
(documentar la limitación: si el jugador intercala dos procesos, la cadena
se mezcla — aceptado en v0).

### 6.4 Patentes en el diario + observaciones

- Cuando `RegistrarOp` produce un `matSalida` (base,estado) NUNCA antes
  producido esta partida: se ofrece PATENTAR (aviso en pantalla estilo "LEY
  DESCUBIERTA"). La patente congela la cadena de ops que termina en esa op
  (hasta 4 hacia atrás), entra como página en una sección nueva del diario
  (`JournalHud`): "PROCEDIMIENTOS" — pasos numerados con máquina + condición
  + resultado, y el jugador la BAUTIZA con el sistema de bautizo existente
  (NamingUi; los procedimientos usan el mismo flujo que las sustancias).
- La "configuración fantasma" v0 es ESTA página (lista de pasos legible);
  el overlay sobre las máquinas es v2.
- `SubstanceKnowledge`: método nuevo
  `public void RegistrarObservacionPropiedad(byte matId, string observacion)`
  — anota en la ficha del material (sección observaciones existente) una
  línea presenciada; lo llama el BancoChispa (B) y el Ensayo. Y: las 5 bases
  (estado Polvo y demás variantes) son INNOMINADO (NombreComun → null, ya es
  el default); `Limo` es VOCABULARIO: "limo". Bautizar la BASE en cualquier
  estado nombra la base; el estado se muestra como sufijo fijo ("(fundido)",
  "(cerámico)") — una base = un nombre, no ocho.

### 6.5 DayCycle + HintSystem + firmas de spawn

- DayCycle: en el arranque del laboratorio, `HudSilenciado` pasa a false
  DESDE EL PRIMER MOMENTO jugable (los pedidos son el tutorial; el gate del
  cavado murió con esta dirección) y llama `GenerateOrdersPersiste()`.
  Sin reloj (igual que el pivot). No tocar el modo clásico.
- HintSystem: guion nuevo de pistas del laboratorio (reemplaza las del
  pivot en este modo), una línea ejecutable cada una:
  "El caño turbio gotea LIMO: todo lo que existe aquí desciende de él.",
  "Hierve limo en el crisol: el agua se va, sus arenas quedan.",
  "El crisol solo, sin alimentar, hierve pero no funde.",
  "Tostad un polvo sin fundirlo: algunos se vuelven combustible.",
  "Con combustible en la tolva del crisol, llega el rojo de verdad.",
  "Lo fundido, vertido fuera, se templa: duro pero frágil.",
  "Dejadlo morir dentro del crisol y saldrá recocido: dócil a la prensa.",
  "La prensa compacta lo dócil y revienta lo frágil.",
  "La lámpara del banco delata lo que el ojo no ve.",
  "Pulsa T para bautizar; el libro (J) guarda vuestros procedimientos."
- Firmas de spawn que B cableará en el bootstrap (congeladas):
  `EnsayoMaestro.Init(AlkahestSim sim, OrderSystem orders, Transform jugador)`
  — nada más necesita Init nuevo; Hornada es estática; el resto ya existe.

## 7. LO QUE NINGÚN ENCARGO TOCA

`ReactionEngine.cs`, `LeyDelUniverso.cs`, `SimEvents.cs`, `CellGrid.cs`,
`SimRenderer.cs` (los estados usan la firma visual existente — si crees que
necesitas tocarlo, pregunta), `Criatura.cs`, `Capullo.cs`, `HeatPlate.cs`,
`ChillStone.cs`, `MasterSupplies.cs`, `Flask.cs`, `Cincel.cs`, `Mudanza.cs`,
el sorteo de leyes/afinidad existente, y todo el modo clásico de 3 jornadas.

## 8. DEFINICIÓN DE HECHO

Compila sin warnings nuevos; cero allocs nuevos por tick; comentarios en
español con el porqué; constantes y firmas EXACTAS de este contrato (los
otros encargos las referencian tal cual). Al terminar: lista de archivos
tocados, resumen por cambio, sales/constantes nuevas con valores, y toda
decisión tomada fuera del contrato, marcada.
