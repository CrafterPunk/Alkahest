# LENTE «CUERPO DEL JUGADOR» · el muñeco como parte de las leyes

*(Segunda pasada comercial, panel de leyes. Diseñador de simulaciones con oficio de ingeniero. Todo lo que
sigue está contrastado con el código a fecha R151: las funciones y parámetros citados existen.)*

## 0. Lo que hay hoy (hechos del código)

- El avatar (`Game/ApprenticeController.cs`) corre en `Update()`, a frame rate, no a tick. Su caja AABB
  (`MedioAnchoColision 0.32u`, `MedioAltoAbajo 0.64u`, `MedioAltoArriba 0.48u`) mide **6,4 × 11,2 celdas**: un
  muñeco de 2×4 «parches» de 3×3. `CajaChoca` ya recorre todas las celdas que la caja toca con
  `SampleMaterial`; solo bloquean los `EsSolidoDelMundo`; polvos, líquidos y gases se atraviesan.
- **El muñeco no lee ningún campo del laboratorio** (solo `mat[]` para chocar; `temp[]` lo lee el
  termómetro, tecla G) y no escribe nada: escribe el frasco (`Flask.cs`, `Paint`/`PaintCell`). El frasco
  lleva **temperatura media por material** (`_tempSum[256]`, `TempMediaDe`), pero congelada: el agua
  hirviendo hierve una hora después, el hielo no se derrite y una brasa aspirada (es `Powder`, pasa el
  filtro de `TickSuck`) es eterna y no quema.
- Las pasadas del laboratorio visitan cada celda **una vez cada 8 ticks** (`LabCampos`). Toda transferencia
  de agua pasa por `LabNacerAgua`/`LabTransformar` y se audita en `LabBalanceU`; el secado es
  `LabSecarHacia`; el calor se difunde con `LabFlujoTermico` (k = min(k_i, k_j), paso = flujo/(64·c)); el
  delta real de calor lo escribe `LabSumarTemp`. `LabRespira` ahoga al fuego con ≥2 vecinos de humo;
  `LuzDecayHumo 24` lo vuelve sombra. Última sal de `XorShift.FromCell`: `SalLabCarboniza = 632`.
- El banco (`Sim/LabBench.cs`) hashea siete arrays. **No hay avatar en el banco.** La única intervención
  que admite es la caldera del alambique: siete celdas de agua cada 8 ticks, escritas ANTES de `Step()`.
- Red: la sim vive solo en el anfitrión y el espejo no recibe `temp[]` (`SimSync.cs`). `AprendizNet` ya usa
  un `NetworkVariable<byte>` de autoridad servidor: el patrón para devolverle al invitado lo que siente.

## 1. Arquitectura común: el cuerpo es un huésped de celdas, no un material

Los cuatro candidatos comparten un solo añadido, y por eso son baratos:

**`CuerpoSim`** (struct en `Sim/`, puro C#): `X0, Y0` (celda inferior izquierda de la caja 6×11) y los bytes
`Calor` (raw, misma escala que `temp[]`; reposo 78 = 37 °C), `Mojado` (0..255 = una celda de agua repartida
por la ropa), `Humo`, `TFrasco`, `CeldasFrasco`. `SimStepper` tiene `Cuerpos[4]`, uno por avatar. **La
posición es una ENTRADA del tick**, como la caldera del banco: `Game/LabCuerpo.cs` (MonoBehaviour nuevo,
`DefaultExecutionOrder(-10)` para correr antes de `AlkahestSim.Update`) convierte el transform a celda y la
escribe; con ×10 los diez ticks del frame leen la misma celda. El banco la escribe desde un guion
`(tick) → (x, y)`: el cuerpo queda tan determinista como el manantial.

**La pasada `LabCuerpoJugador()`** se cuelga en `LabPasadas()` después de `LabCampos()`, una visita cada 8
ticks, sobre las 66 celdas cubiertas más la fila bajo los pies. Escribe SOLO por las puertas que ya
auditan: `LabNacerAgua`, `LabTransformar`, `LabSumarTemp`, `LabLatente`. Coste: 8 celdas-equivalentes por
tick frente a las 27 000 de `LabCampos` (< 0,01 ms). Los bytes del cuerpo entran al hash (`HashCuerpo` en
`LabBench.Resultado`).

Todos los números van a `LabParams` con registro, grupo **PIEL** (`cuerpo.*` ya es de la roca suelta). Las
consecuencias sobre el control son tres multiplicadores públicos que `ApprenticeController` lee
(`FactorSalto`, `FactorVelocidad`, `FactorControl`): seis líneas de costura, la misma categoría que las tres
líneas de la cuarta textura en `SimRenderer.cs` que el handoff documenta como excepción.

---

## 2. Candidatos, por apalancamiento

### C1 · `piel-termica-humeda` — el cuerpo es una celda gorda con calor y agua

**Regla.** Por visita, el cuerpo hace lo que haría una celda con `k = piel.kCarne` (8, como el agua) y
`c = piel.cCarne` (24: la inercia de sesenta celdas de carne, no de una):

1. *Térmica.* `flujo = Σ_j (temp[j] − Calor) · min(kCarne, LabK(mat[j]))` sobre las 66 celdas; `paso =
   flujo/(64·cCarne)` con la contracción de `LabFlujoTermico`; a cada celda no vacía se le devuelve su parte
   con `LabSumarTemp` (el cuerpo es un sumidero de calor real, contado en un `LabRawCuerpo` como el hogar en
   `LabRawHogar`). Tirón metabólico hacia `piel.calorReposo` (78) de 1 raw por barrido de ambiente
   (`TiroAmbienteTicks`): el único número «fisiológico».
2. *Empaparse.* Por cada celda de `Water` cubierta, `t = min(piel.empapa 32, hum[j])` pasa de `hum[j]` a
   `Mojado`; la celda vacía se transforma con `LabTransformar(j, Empty, 0, 0)`. Conservación exacta: el
   cuerpo entra en `LabBalanceU`.
3. *Gotear.* Si `Mojado > piel.goteaDesde` (128): `LabNacerAgua` de `piel.gotea` (16 u) en la primera celda
   vacía bajo los pies (`LabVecinoVacio`, abajo primero). Nace agua PARCIAL, que `LabAgua` ya sabe
   infiltrar, evaporar y depositar.
4. *Secarse.* `LabSecarHacia` hacia las celdas de aire cubiertas (déficit de saturación × `Calor −
   AmbientRaw`); `LabLatente` enfría el cuerpo al evaporar. Junto al hogar te secas en segundos y humeas
   (`SpawnSteamPuff`); en la cámara del alambique no te secas nunca.
5. *Aliento.* `piel.exhala` (2 u/visita, el `PlantaTranspira` del cuerpo) a la celda de aire de la boca:
   fuente diminuta contada en `LabAguaEmitida` como el manantial. En aire frío saturado condensa en la
   pared fría de al lado por la regla local de `LabAire` («el vecino condensable MÁS FRÍO»): el jugador ve
   nacer rocío donde respira.

**Control: tres umbrales, no curvas.** `Calor > piel.calorSuelta` (100 raw, 80 °C) → las manos se abren:
el frasco se vacía a tus pies (`Flask` ya tiene «vaciar», tecla Q) y no aspiras hasta enfriarte. `Calor <
piel.frioTorpe` (55 raw, −10 °C) → `FactorControl 0.5`, `FactorVelocidad 0.7`, tiritar. `FactorSalto = 1 −
Mojado/255 · piel.pesoMojado (0.35)`.

**Cruces.** *Agua:* el cuerpo es una tercera vía de transporte, además del frasco y el goteo; cruzar la
poza y caminar sobre el lecho lo riega en línea (columnas, no lechos: el negativo de R148, en tus pies).
Una celda parcial bajo los pies moja la yesca (`FibraMojadaMin 100`): pisar el fogón recién armado con la
ropa empapada lo apaga antes de encenderlo. *Fuego:* `calorSuelta` convierte «acercarse al hogar» en un
tiempo medido por la física: cuánto aguantas junto a la boca del horno para meter la carga; la ropa mojada
es el traje ignífugo (más capacidad, enfría al evaporar), y se gasta. *Condensación:* el cuerpo frío que
sale de la cámara alta es superficie condensable en aire húmedo: se te forma rocío, luego goteas; el que
se seca humidifica el aire y cambia dónde llueve. *Plantas:* el goteo es riego involuntario (humedad ≥ 60
germina): por donde pasas mojado, brota.

**Decisiones nuevas.** Mojarme a propósito antes del horno; secarme junto al hogar antes de la cámara
fría; cruzar por el agua o por arriba; dónde parar sin regar la yesca; frasco lleno o vacío cerca del calor
(si se abre la mano, lo que llevaba cae en la brasa); acelerar ×10 con el cuerpo dentro es una apuesta: te
calientas y secas diez veces más rápido, SOLTAR a escala corporal.

**Observación.** Sin panel: el sprite se tiñe con la fórmula de `SimRenderer.LabTinte` para arena mojada
(`AplicarTinte` ya multiplica capas); charco bajo los pies; vaho; rocío en la pared; tiritar; manos
abiertas. Con visor: **`VistaLaboratorio.Piel`**, las vistas de temperatura y humedad existentes
(`LabVistaColor`) recortadas a un halo de 12 celdas alrededor del cuerpo, siempre encendida: lo que ves es
lo que la piel siente. El rayos X pasa a ser el tacto.

**Coste:** 1 semana de Opus (pasada, struct, costura, tinte, vista, escenario). **Determinismo:** intacto
(posición como entrada; sales ≥ 633). **Tick:** < 0,01 ms. **Verificación headless:** escenario «cuerpo en
el alambique»: `MontarAlambique` + guion de 9 000 ticks (3 000 en la poza, 3 000 caminando 30 celdas, 3 000 a
dos celdas del hogar). Se asevera: `Mojado` llega a 255 y baja a 0 junto al hogar; N celdas de goteo
(`LabCuerpoGoteos`); `LabBalanceU` cuadra con el cuerpo dentro; el tick en que `Calor` cruza `calorSuelta`
es un NÚMERO MEDIDO que va al informe («aguantas 5,3 s»), no afinado a ojo. **Tuning humano: 7** (nueve
registros, tres umbrales; el playtest solo juzga si 80 °C y 35 % de peso se sienten justos).
**Apalancamiento: 9.** **Riesgo mayor:** que el cuerpo sea un termómetro andante sin consecuencia (umbral
alto) o un fastidio (umbral bajo). El banco acota el rango; el valor final es UN slider en una tarde.

### C2 · `carga-con-temperatura` — lo que llevas sigue siendo lo que es

**Regla.** El contenido del frasco es una celda virtual más del intercambio de C1: `TFrasco` (media
ponderada de `_tempSum`) intercambia con `Calor` con `k = piel.kFrasco` (2, vidrio = `KRoca`) y capacidad
`c = CAgua · CeldasFrasco` (30 celdas se enfrían despacio; 3, en un suspiro). Tres reglas derivadas con
números que YA existen en `MaterialDef`:
1. *Fase dentro del frasco:* `TFrasco ≥ meltsAt` del hielo → `counts[Ice] → counts[Water]`; `≤ freezesAt`
   al revés; `≥ boilsAt` del agua → una celda por visita se va como `Steam` por la boca (el frasco silba).
   Es `ApplyPhase` sobre la media del frasco: cero números nuevos.
2. *La brasa en la mano:* con `counts[Brasa] > 0`, `TFrasco += BrasaCalorRaw` por visita (el 10 de
   `ProcessBrasa`) y una brasa pierde vida cada `BrasaLifeUnitTicks·4` (la sordina de `LabRespira`: en el
   frasco no hay aire). Se apaga en el frasco; antes te calienta hasta `calorSuelta` y **se te cae donde
   estás**.
3. `Flask` expone `AjustarTemp(int deltaRaw)` y `CeldasYTempMedia()`: doce líneas de costura.

**Cruces.** *Fuego:* «llevar fuego» pasa de gratis a apuesta: la brasa vive un tiempo medido y te quema
antes; la cadena hogar → yesca → carbón se transporta solo hasta cierta distancia, y la yesca que ibas a
encender es lo primero que la brasa caída prende. *Agua:* el agua hirviendo llega tibia a la cámara alta a
pie (400 ticks, 50 visitas); el hielo llega agua si no lo guardas en la cámara fría: el núcleo frío es
nevera, no solo condensador. *C1:* el frasco caliente es abrigo para cruzar la cámara fría; el de hielo,
refresco junto al horno.

**Decisiones nuevas.** Cuánto llevar (más celdas, más inercia); por dónde ir (pegado al arroyo a 20 °C o por
la galería del hogar); correr o volar con carga caliente; dónde soltar la brasa a tiempo (un fogón de yesca
en el camino); guardar el hielo arriba.

**Observación.** Sin panel: el tarro en mano ya tiene color de contenido y muelle de «pop»
(`Flask.ActualizarJuice`); se le añade vaho al hervir, escarcha al helar, brasa que se apaga; la mano se
abre y el contenido cae. Con visor: la vista `Piel` incluye el tarro.

**Coste:** 0,5 semanas. **Determinismo:** intacto (`TFrasco`/`CeldasFrasco` son entradas del tick; el frasco
solo lee de vuelta; en el banco se guionan). **Tick:** un intercambio por visita. **Verificación headless:**
«brasa en mano» (tick en que cae, tick en que se apaga) y «agua hirviendo a la cámara alta» (30 celdas a
110 raw, guion de 200 celdas a 15 celdas/s: temperatura de llegada). **Tuning humano: 8** (dos registros).
**Apalancamiento: 8.** **Riesgo mayor:** `Flask.cs` está cerrado por el handoff y en multi el frasco del
invitado es local: el intercambio corre en el anfitrión y `TFrasco` vuelve por `NetworkVariable`; si esa
ida y vuelta se retrasa, el invitado vierte a una temperatura que ya no es. Acotado, pero es la costura
más fea de las cuatro.

### C3 · `humo-respirado` — la visión se encoge; la chimenea es para ti antes que para el fuego

**Regla.** Por visita, `Humo += piel.humoTraga (12) · celdas de Smoke` en el parche de la cabeza (3×3
superior); en aire limpio `Humo −= piel.humoSuelta (4)` (de 255 a 0 en 17 s). `LabCuerpo.cs` traduce: viñeta
de visión con radio `pantalla · (1 − 0.8 · Humo/255)`; por encima de `piel.humoTos` (200), tos: dos frames de
input cortado cada 32 ticks, sonido, el sprite se dobla. Sin desmayo ni muerte.

**Cruces.** *Fuego sin tiro (el negativo medido):* como el aire no se mueve, el humo se embolsa bajo la
bóveda y ahoga al fuego (`LabRespira`); ahora te ahoga a ti primero, y la chimenea se construye por tu
cara, no por el rendimiento del horno. *Luz:* el humo que oscurece el claro (`LuzDecayHumo`) es el que te
ciega: la planta y tú comparten sensor. *Carbonera:* abrir la boca 1 es un evento corporal. *Combustible:*
el carbón humea 4 % contra 16 % de la fibra (`combustHumoPct`): se aprende a oler la carga.

**Decisiones nuevas.** Dónde parar mientras el horno trabaja; abrir o no la boca; qué combustible cargar si
te quedas cerca; entrar a la cámara ahumada de un tirón o tallarle una boca en el techo. **Observación.**
Sin panel, la cámara misma es el instrumento; el sprite se tizna (la rampa de hollín de la pátina). Con
visor, nada nuevo: el humo ya es materia visible.

**Coste:** 0,25 semanas. **Determinismo:** intacto. **Tick:** nueve lecturas por visita. **Verificación
headless:** «carbonera con testigo»: `MontarCarbonera` + cuerpo quieto en la boca 9 000 ticks: curva de
`Humo`; el mismo escenario con una chimenea de 1 celda: `Humo` máximo cae. **Tuning humano: 8** (tres
registros y el 0,8 de la viñeta). **Apalancamiento: 7.** **Riesgo mayor:** que la viñeta se lea como barra
disfrazada o robo de control. Mitigación: es espacial (un paso al lado y se despeja), sin muerte, y la
tos es rara (200 de 255).

### C4 · `masa-y-flotacion` — el cuerpo y el frasco pesan lo que llevan; el agua sostiene

**Regla** (lado juego, sin tocar la sim): masa en celdas-agua `m = piel.masaBase (60) + Σ counts[mat] ·
density[mat]/110 + Mojado/255`; volumen desplazado `66 + Σ counts`. `FactorSalto = FactorVelocidad =
masaBase/m`. En agua (celdas de `Water` bajo la caja, lectura que `CajaChoca` ya hace): el cuerpo vacío
flota con la cabeza fuera; un frasco de grava (`density 200`) te hunde y caminas por el fondo; uno de
fibra (`density 60`) te saca a flote. Arrastre `piel.arrastreAgua 50 %`. **Corrientes: no.** La sim no
tiene campo de velocidad (los líquidos solo llevan la «memoria de flujo» de `ProcessLiquid` en `aux`); el
empuje por corriente o viento se difiere a la lente de aire, que es quien traería ese campo.

**Cruces.** *Agua/presión:* la poza profunda deja de ser un sitio al que se vuela; tallar el desagüe bajo
el agua pide lastre o drenarla. *Frasco:* la grava (residuo del cincel) se vuelve herramienta. *C1:*
mojado pesa. **Decisiones nuevas.** Qué llevar según a dónde; bucear con lastre; cruzar la poza flotando
(lento, mojado) o rodearla. **Observación.** El propio muñeco: se hunde, sube, el bob se aplana con carga.

**Coste:** 0,25 semanas. **Determinismo:** no lo toca. **Tick:** cero en la sim. **Verificación headless:**
débil: prueba unitaria de la función de masa; el resto es sensación de manejo, afinada a mano nueve rondas
en este proyecto (R110-R121). **Tuning humano: 6.** **Apalancamiento: 6.** **Riesgo mayor:** justamente
ese: es la que más playtest de sensación pide y la que menos juzga la simulación. Va última.

---

## 3. Lo que descarté y por qué

- **Barras, hambre, sed, muerte por frío o fuego.** Prohibido por el encargo e innecesario: manos que se
  abren, torpeza y visión ya producen decisión y accidente sin contador ni reaparición; la brasa caída
  sobre la yesca es mejor clip que un avatar en llamas.
- **El cuerpo como material en `mat[]`.** Radio de explosión enorme (`SwapCells`, sueño de chunks,
  colisión, `SimSync`) y no compra nada que el huésped no compre.
- **Sudor como fuente de agua.** Rompe la conservación (invariante 3 del handoff): `piel.suda` queda a 0 y
  el aliento es la única fuente, contada como el manantial.
- **Tos que expulsa humo, cuerpo que ilumina, hollín como memoria.** Sin física o sin decisión; la luz es
  de la lente óptica y la huella, de la de instrumentos.
- **Empuje por viento o corriente.** No hay campo de velocidad; si la lente de aire lo trae, el cuerpo lo
  lee con la regla de C4 sin cambio de arquitectura.
- **Curvas de torpeza, hipotermia progresiva, fatiga.** Cada curva es una tarde de tuning; un umbral es un
  slider.
- **Cuerpo lockstep en multi.** El anfitrión calcula `Cuerpos[4]` y devuelve cuatro bytes por avatar; no
  hace falta el determinismo cross-machine que no está probado.

**Orden final:** C1 → C2 → C3 → C4. C1+C2+C3 son una sola pasada, un struct, una vista y tres escenarios del
banco: unas dos semanas de Opus, verificables sin personas, y el número que importa en cada uno («cuánto
aguanto», «a qué temperatura llega», «cuánto tardo en cegarme») lo mide el banco antes de que nadie juegue.
