# DISEÑO — EL LABORATORIO DE LEYES (segunda galería, sandbox de investigación)

*(Escrito 2026-09-03 tras el reconocimiento completo del motor — ver `docs/LAB/mapa/*.md`
y `CHECKPOINT.md`. Es el plan que se implementa; lo que cambie durante la implementación
se anota en el checkpoint y en el informe final. Nada de esto migra al juego sin su ronda.)*

## 0. LA PREGUNTA Y LA APUESTA

Pregunta de Cesar: ¿puede este motor ser un sandbox donde aprender cómo funciona el mundo
permite construir procesos cada vez más poderosos, hasta que conocimiento y geometría
sustituyen al trabajo manual?

Apuesta de diseño: **un puñado de campos persistentes por celda (humedad, carga, reposo,
luz) + procesos lentos locales + una sola regla no local (la presión hidrostática por
cuerpos de agua conectados)** producen cadenas largas de consecuencias observables con
un coste de simulación acotado. Cada regla debe servir a VARIOS fenómenos (R48: verbo
visible + consumidor real), no un sistema por fenómeno.

## 1. LO QUE EL MOTOR YA DA (y se reutiliza tal cual)

- Agua que cae, se estratifica por densidad y fluye hasta 4 celdas por tick (`ProcessLiquid`).
- Vapor como GAS REAL con convección coherente (viento por hash, ondulación, escape bajo
  techo), que condensa a agua cuando su temperatura baja de `condensesAt` (~60 °C) en
  cualquier tick (`ApplyPhase`) y también al expirar. Humo, fuego, brasa, combustión
  persistente con reserva en `aux`.
- Temperatura por celda (byte, 1 raw = 2 °C, ambiente raw 70 = 20 °C), difusión 1/8 por
  tick sobre TODA la grilla, tirón ±1 hacia `ambient[]` cada ~32 ticks. Sin conductividad
  ni capacidad por material (roca = aire = agua). Sin frío visual.
- Sólidos con cohesión (`caeSolido` + `cohesionCeldas`): caen recto al perder apoyo, con
  ménsula. Sin cuerpos rígidos.
- Chunks 16×16 con sueño (30 ticks) — todo proceso lento debe despertarse a sí mismo (R55).
- Cincel (talla Stone/Piso, 3 celdas/tick, alcance 22), frasco (900 celdas, aspira/vierte a
  60 celdas, conserva temperatura media), termómetro (G, 3 sondas), tres modos de movimiento.
- Galería de estilo (R127-129) como patrón de cableado: flag `ModoGaleria`, plano propio,
  spawn propio, curador IMGUI, capturas F10.
- Banco headless (`Tools~/BenchSim/Harness.cs`) y `SimStepper.LastStepMs/ActiveCells/ActiveChunks`.

Lo que NO existe y aquí se crea: humedad/mojado, sedimento en suspensión y su
decantación, infiltración/permeabilidad/colmatación, erosión, evaporación por debajo de
la ebullición, condensación en superficies, secado, compactación, luz, vegetación
sistémica, cuerpos sólidos móviles, control de tiempo, panel de parámetros, presets.

## 2. ESTADO NUEVO POR CELDA (CellGrid)

Cuatro arrays `byte[W*H]` (221 KB cada uno). Viajan con la materia en `SwapCells`
(salvo `luz`, que es posicional). `SetCell` los inicializa (agua nace con volumen 255).

| Array | En Empty (aire) | En Water | En porosos (Sand, Grava, Sedimento, Ash, Fibra, Arcilla) | En roca (Stone, Terracota, Piso) | En Planta |
|---|---|---|---|---|---|
| `humedad` | vapor de agua en el aire | VOLUMEN restante (255 = celda llena) | agua contenida (255 = 1 celda de agua) | rocío superficial (a 255 gotea) | savia |
| `carga` | — | finos en suspensión (turbidez) | finos atrapados (colmatación); en Sedimento: FERTILIDAD | — | — |
| `reposo` | — | visitas sin moverse (quietud) | visitas sin moverse (edad de compactación) | — | temporizador |
| `luz` | luz recibida 0..255 (posicional, se recalcula cada 8 ticks) | | | | |

Por qué estos y no otros: humedad y carga son los dos que generan MÁS consecuencias cada
uno (humedad: infiltración, secado, condensación, capilaridad, plantas, ablandamiento de
arcilla; carga: turbidez visible, sedimentación, colmatación, fertilidad). `reposo` es el
proxy barato de "velocidad" (quietud) que gobierna decantar/erosionar/compactar sin un
campo de velocidad. `luz` es lo que da a la vegetación un segundo gradiente en tensión
con la humedad (cerca del fuego hay luz y sequedad; lejos hay agua y oscuridad).

Descartados como RUIDO en esta ronda: exposición térmica histórica (la pátina del renderer
ya cuenta el tizne; una versión física entraría por `reposo`+temp si hiciera falta), flujo
acumulado como campo (la quietud lo aproxima), cohesión como campo (vive en el material),
presión como campo (R: "duplicaría el hot path" — se resuelve por componentes, §4).

## 3. MATERIALES NUEVOS (ids 66+, `MaterialId.Count` 66 → 78)

| id | Nombre | Arquetipo | Papel |
|---|---|---|---|
| 66 | Sedimento | Powder | finos decantados; húmedo o seco; poroso (poca permeabilidad, se colmata); erosionable; se compacta en Arcilla; sustrato de plantas (carga = fertilidad) |
| 67 | Arcilla | StaticSolid (cae con cohesión 2) | sedimento compactado; casi impermeable; tallable (el cincel la desprende como Sedimento); empapada se ablanda; con calor y seca → Terracota |
| 68 | Terracota | StaticSolid (cohesión 6) | arcilla cocida; impermeable; dura; el cincel la rompe en Grava |
| 69 | Grava | Powder (fluidez 0) | grueso; muy permeable; casi no se colmata; no erosiona; filtro grueso |
| 70 | Planta | Planta (arquetipo nuevo 7) | brota en sustrato húmedo con luz; crece hacia arriba; savia; muere a Fibra; arde |
| 71 | Fibra | Powder | planta seca; combustible persistente (→ Brasa → Ash); mojada no prende |
| 72 | Hogar | StaticSolid (nunca cae) | brasa eterna: fija su temperatura, inyecta calor, enciende; no tallable |
| 73 | NucleoFrio | StaticSolid | sumidero térmico eterno (catálogo, no pre-puesto) |
| 74 | Manantial | StaticSolid | fuente: emite agua turbia a caudal paramétrico; no tallable |
| 75 | Sumidero | StaticSolid | desagüe: destruye líquido que lo toca; no tallable |
| 76 | RocaSuelta | StaticSolid | cuerpo cohesionado: cae como bloque conectado, se fractura por caída o golpes (hipótesis §8) |
| 77 | Semilla | Powder | brote plantable (catálogo) |

Se registran en `Universe.Create` (defs) y en `RellenarPersistenciaCruces` (arrays de
persistencia, patrón PisoEstructural). Ninguno entra en `UnnamedMaterialIds` (vocabulario:
color fijo, R13/17). La ceniza (Ash) gana un papel: mojada sobre sustrato = abono.

Universo del laboratorio: seed 777002 SIN overrides de Semilla Cero, más
`Universe.AplicarOverridesLaboratorio` (agua: densidad 110 fija, hierve 100 °C, congela
0 °C; vapor condensa 60 °C, vida paramétrica). Así el laboratorio no depende del sorteo.

## 4. FLUIDOS — la aproximación elegida

**Conservación de masa**: toda transición agua↔vapor↔humedad↔agua-contenida se hace en
unidades de volumen (255 = 1 celda). El vapor que expira ya no desaparece: deja su
volumen como humedad del aire. Fuentes y sumideros llevan contador (libro mayor en el panel).

**Hidrostática (la única regla no local)**: cada `PresionCadaTicks` ticks se etiquetan los
cuerpos de agua conectados (BFS 4-conexa, arrays preasignados). En cada cuerpo con ≥ N
celdas: el techo = la superficie más alta (agua con aire encima); toda superficie a ≥
`DesnivelMin` celdas por debajo recibe agua: se quita la celda de superficie más alta y se
pone en (x_baja, y_baja+1), conservando temperatura/carga. Ritmo acotado por cuerpo y paso.
Consecuencias jugables: vasos comunicantes, sifón (llenar un tubo sobre un muro), fuente
artesiana (un canal sellado desde el manantial alto brota más abajo sin bomba), presión
que "sube" agua por un pozo. Coste: O(celdas de agua) cada 2 ticks.

**Dinámica**: no hay momento ni velocidad; `reposo` (quietud) distingue agua que fluye de
agua quieta, que es lo que erosión/decantación necesitan. Bernoulli, viscosidad y tensión
superficial se descartan: no producen consecuencia observable a 1 celda = 1 grano que el
jugador pueda reutilizar como conocimiento (la fluidez por material ya cubre "espeso").

**Interfaces**: infiltración (agua → poroso vecino, tasa = permeabilidad × (1 −
colmatación)²), percolación (baja por gravedad entre porosos), capilaridad (lateral y
hacia arriba solo en finos), exudación (poroso saturado con aire debajo/al lado suelta una
celda de agua LIMPIA: el manantial filtrado), rocío en roca (condensación acumulada que
gotea al llegar a 255).

**Estado**: incompresible por construcción (una celda = un volumen).

## 5. TÉRMICA (revisión pedida)

Diagnóstico: la difusión actual es coherente y estable (R9) pero uniforme: roca, aire y
agua conducen igual, sin capacidad térmica, sin convección, sin calor latente, y el tirón
hacia ambiente es lineal (±1/32 ticks). Es suficiente para hervir/congelar cerca de una
fuente; NO da "la roca guarda el calor", "el aire caliente sube", "evaporar enfría".

Lab (`LabParams.TermicaPropia`, sustituye a `DiffuseTemperature` solo en el laboratorio,
misma cobertura 1/8 y redondeo simétrico): conductividad k (0..16) y capacidad c (1..8)
por clase (aire k4 c1, agua k8 c4, roca k2 c3, polvo k3 c2, gas k6 c1, arcilla k2 c3);
flujo = Σ (Tj − Ti)·min(ki,kj) / (64·ci) — contracción garantizada (k ≤ 16); convección:
en aire, el intercambio con el vecino de ABAJO más caliente pesa ×2 y con el de ARRIBA más
caliente ×½ (el calor sube); calor latente paramétrico en evaporación/condensación; tirón
hacia `ambient[]` como siempre. El Hogar y el NúcleoFrío fijan su celda e inyectan cada
visita (y despiertan su chunk: R55).

## 6. PROCESOS LENTOS (la pasada de campos, 1/8 de la grilla por tick, TODA la grilla)

Viven en `SimStepper.Laboratorio.cs` (`LabCampos`), fuera del barrido de chunks despiertos
— así una poza dormida sigue evaporando y un sedimento dormido sigue compactándose. Solo
cuando cambian MATERIA despiertan el chunk. Por material:

- **Aire**: difusión de humedad (con deriva ascendente), condensación cuando humedad >
  saturación(temp) sobre la superficie vecina (techo primero), rocío → gota.
- **Agua**: evaporación de superficie (tasa ∝ temp − ambiente, solo si el aire no está
  saturado), infiltración a porosos vecinos (con arrastre de carga → colmatación),
  decantación de carga hacia abajo (más rápida en reposo), DEPÓSITO (fondo + carga ≥
  umbral + reposo ≥ umbral → celda de Sedimento húmedo), mezcla lateral de carga.
- **Porosos**: percolación, capilaridad, exudación, secado al aire, compactación (Sedimento
  húmedo, quieto, enterrado → Arcilla), ablandamiento (Arcilla empapada → Sedimento),
  cocción (Arcilla seca + ≥ temp → Terracota), abono (Ash mojada → fertilidad del sustrato).
- **Roca**: rocío que gotea; secado del rocío.
- **Planta**: raíz bebe del sustrato, savia sube, crece con luz, ramifica, muere seca →
  Fibra, arde. Germinación espontánea en sustrato húmedo iluminado (probabilidad baja).
- **Hogar/NúcleoFrío/Manantial/Sumidero**: sus tics de fuente/sumidero.

Barrido (chunks despiertos): erosión — agua que se MUEVE junto a Sedimento/Arcilla con
carga baja lo convierte en agua turbia (carga 255): la erosión y el depósito son
simétricos, así el agua se conserva a lo largo de un ciclo erosión→transporte→depósito.
Vapor que expira → humedad del aire (en `ProcessGas`). Fibra mojada no autoenciende.

Luz (`LabLuz`, cada 8 ticks): fuentes = Fire/Brasa/Hogar (255) y la boca del cielo (255);
propagación por máximo con decaimiento (aire 8/celda, vertical desde el cielo 1/celda,
agua 20, planta 40, sólidos reciben y no transmiten). Cuatro barridos, ~1-2 ms cada 8 ticks.

## 7. EL PLANO (`SimLevelBuilder.BuildLaboratorioDeLeyes`, x 30..430, resto roca maciza)

```
y 287 ┌────────────────────────────────────────────────────────────────┐
      │        ▓▓▓ boca del cielo x118-124 (luz)                       │
y 272 │   ┌── CÁMARA ALTA x100-190 (fría, sedimento seco en el piso) ──┐│
y 245 │   └──────────┬ chimenea x140-149 ┬───────────────────────────┘│
y 214 │   ┌── SALA DEL HOGAR x60-170 ── (spawn 118,182) ── HOGAR x150-158 ┐
y 176 │ arcilla└────┬ pozo x62-77 ┬──────────────────── bolsillos ocultos ┘
y 152 │ MANANTIAL► GALERÍA DEL ARROYO x36-430, piso escalonado 138→130 ►► pozo/SUMIDERO x416-429
y 122 │        fisura de arena x186-196 ↓      POZA x250-330 (fondo 122)   grieta x336-343 ↓
y 110 │   ┌────── CÁMARA PROFUNDA x120-260 (se inunda por la grieta; manantial filtrado en el techo) ──┐
y  60 │   └──── cubeta x140-180 ──────────────────────────────────────────────────────────────────────┘
```

Cadenas que la geometría SUGIERE sin forzar: (a) el arroyo turbio entra en la poza, se
frena, deposita y sale más claro; (b) la fisura de arena filtra el arroyo y brota limpio
en el techo de la cámara profunda hasta colmatarse; (c) la grieta inunda la cámara profunda
lentamente (consecuencia a largo plazo); (d) agua llevada al hogar hierve, el vapor sube
la chimenea, condensa en la cámara alta fría, gotea sobre el sedimento, y con la luz del
cielo brotan plantas → fibra → combustible; (e) arcilla tallada → sedimento → secado junto
al hogar → arcilla → terracota (recipientes/canales impermeables); (f) vasos comunicantes:
tapar la grieta y tallar un canal sellado desde la poza a la cámara alta… no sube: la
poza está más baja — pero desde el MANANTIAL (y139) sí sube hasta y139 en cualquier sitio.

## 8. HIPÓTESIS SECUNDARIAS

**Vegetación** (§6): función sistémica = combustible (Fibra), indicador de humedad/
fertilidad, ciclo ceniza→abono. Visual: verde con patrón; Opus para arte si hay tiempo.

**Cuerpos cohesionados** (`LabCuerpos`, después del núcleo): `RocaSuelta` se etiqueta por
componentes conectados; sin apoyo → todo el cuerpo baja 1 celda/tick (desplazando líquido);
`aux` acumula ticks de caída; al aterrizar con caída ≥ umbral se fractura (celdas del borde
inferior y una grieta por hash → Grava); golpes de cincel acumulan daño en `aux` y a N
golpes la celda se desprende. Renderizado por el propio SimRenderer. Sin push lateral ni
rotación (vetado por coste).

## 9. PANEL DE LABORATORIO (`Game/LabPanel.cs`, F8)

Pestañas: TIEMPO · AGUA · SEDIMENTO · SUELO · TÉRMICA · VAPOR · LUZ · PLANTAS · CUERPOS ·
MATERIA (pincel) · VISTAS · PRESETS · LIBRO (contadores) · BENCH.
Cada parámetro: nombre, valor (slider+campo), default (marcado si difiere), rango, unidad,
[?] ayuda plegable (`LabParams.Ayuda`). Todo se aplica en vivo (los parámetros viven en
estáticos que el stepper lee cada tick); los que requieren reconstruir (plano) se marcan.
Tiempo: 1×/5×/10×/50×/100× = N ticks por frame con presupuesto de ms (muestra el × real),
pausa, paso.
Presets: `Laboratorio/presets/<nombre>.json` (guardar, cargar, defaults, comparar);
snapshot = preset + PNG + contadores en `Laboratorio/capturas/`. `_defaults.json` se
escribe al arrancar. Vistas: overlay de temperatura/humedad/carga/reposo/luz/chunks.

## 10. RENDIMIENTO Y BENCH

Instrumentación por pasada (Stopwatch en `Step`: difusión, barrido, chunks, morph, campos,
presión, luz, cuerpos). `Sim/LabBench.cs`: escenarios deterministas sobre CellGrid nuevo
(diluvio turbio, arroyo 10 min, hervidero+chimenea, mil plantas, diez cuerpos cayendo,
mundo entero despierto) → `Laboratorio/benchmarks/<fecha>.md` con media/pico por pasada,
celdas/chunks activos, memoria. Optimización barata prevista: saltar filas de chunks
enteramente dormidas en el barrido (semántica idéntica).

## 11. MULTIPLAYER (análisis, no implementación)

Solo `mat[]` viaja hoy. Los cuatro arrays nuevos y `temp` no. El laboratorio es solo-anfitrión.
Análisis por sistema en el informe final (§D).

## 12. ORDEN DE IMPLEMENTACIÓN

1. Cableado del modo (flag, título, bootstrap, plano) + materiales + arrays → jugable vacío.
2. Campos: humedad/carga/reposo, agua/porosos/roca, vapor→humedad, erosión. Render de turbidez/mojado.
3. Presión por componentes. 4. Térmica propia + latente. 5. Luz + plantas + fibra.
6. Panel + presets + vistas + tiempo. 7. Bench + medidas. 8. Cuerpos cohesionados.
9. Jugar/observar por MCP, iterar. 10. Informe.
