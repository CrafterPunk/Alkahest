# DISEÑO — EL DOMINIO DEL FUEGO: controlar energía con geometría

*(Fable 5.1, 2026-09-04. Encargo de Cesar: «el agua nos enseñó a controlar materia en
movimiento; el fuego puede enseñarnos a controlar energía. ¿Cuál es el conjunto MÍNIMO de
reglas añadidas al fuego existente que permitiría al jugador inventar hornos, secadores,
chimeneas y fuentes de calor sostenidas mediante geometría, sin convertir alimentar combustible
en mantenimiento tedioso?». Son hipótesis para Opus 5, no mandato; los números son puntos de
partida para medir. Estado del motor: `docs/LAB/mapa/stepper.md`, `CHECKPOINT.md` §6.)*

## 0. LA TESIS EN UNA FRASE

**El fuego ya sabe arder; lo que no sabe es RESPIRAR.** Hoy una celda de combustible arde igual
enterrada que al aire, un fuego cerrado no se ahoga, el humo se disuelve solo a los 7 segundos
y la brasa muere a los 10 segundos pase lo que pase. Falta un único concepto —**el aire de
contacto**— y con él aparecen, sin una regla por aparato, el fogón que arde deprisa, la
carbonera que arde despacio, el horno que retiene, la chimenea que tira, el rescoldo que se
banca bajo la ceniza y la estufa de tolva que se alimenta sola. Todo lo demás (calor que sube,
roca que guarda calor, arcilla que aísla, humedad que apaga, luz que crece) ya existe en el
laboratorio del agua y solo hay que dejar que cruce.

## 1. LO QUE EL FUEGO YA TIENE (y no se toca)

| Sistema | Dónde | Qué da ya |
|---|---|---|
| Lengua de fuego (`Fire`) | `ProcessFire` | 255 raw en su celda, +40 raw a vecinos cada tick, enciende al 12 %/tick, se apaga con agua → vapor, muere a humo/ceniza |
| Combustión persistente | `ProcessCombustion` + 7 campos de `MaterialDef` | el COMBUSTIBLE es la celda que arde: reserva en `aux`, paso cada 8 ticks, calor, humo, propagación, lengua, residuo |
| Brasa | `ProcessBrasa` | 8-12 s de vida, +10 raw, reenciende al 8 %, agua → ceniza + vapor |
| Humo (`Smoke`) | `ProcessGas` | gas que sube con viento coherente, bolsas bajo techo con medio decaimiento, vida 200 ticks |
| Térmica del laboratorio | `LabDifusionTermica` | conductividad y capacidad por clase (roca k2 c3, arcilla k2, aire k4 c1, agua k8 c4), CONVECCIÓN (el calor sube en el aire), tirón a ambiente |
| Hogar | `LabHogar` | brasa eterna a `fuego.hogarRaw` (220 raw = 320 °C), +40 raw/visita, enciende vecinos, luz |
| Fibra | `Universe.Laboratorio` | polvo ligero, reserva 40 × 8 ticks ≈ 11 s, calor 14, humo 16 %, residuo Brasa; MOJADA no prende (`LabCombustibleMojado`); flota |
| Cruces ya vivos | H3/H4 | secado ∝ calor, cocción arcilla → terracota (150 raw), evaporación/destilación, ceniza mojada = abono, plantas mueren en fibra, luz del fuego hace crecer plantas |

Medido en el agua: el calor sube por la chimenea, la roca del hogar queda tibia, el horno de
arcilla «humedece el cuarto», el vapor viaja 65 celdas. La infraestructura de energía está.

## 2. LO QUE FALTA, DICHO COMO CARENCIAS OBSERVABLES

1. **Un fuego enterrado arde igual que uno al aire.** No hay carbonera, no hay ahogo, no hay
   regulador: el jugador no puede FRENAR un fuego con geometría, solo apagarlo con agua.
2. **El humo no importa.** Vive 7 s, no oscurece, no ahoga: una cueva sin chimenea es
   igual que una con chimenea. El «tiro» no existe como consecuencia.
3. **Toda fuente de calor es igual de caliente.** El hogar gratuito (320 °C) es tan caliente
   como una llama; no hay motivo para construir nada: la fuente inicial ya es «industrial».
4. **La brasa muere pase lo que pase.** No se puede bancar un fuego bajo ceniza para volver.
5. **No hay combustible de segunda generación.** La fibra arde 11 s; alimentar un horno con
   fibra ES mantenimiento tedioso.
6. **No hay un producto que solo el calor sostenido dé.** La terracota se cuece «por la cara»
   con el hogar (H3.4): no hace falta horno para nada.

## 3. EL CONJUNTO MÍNIMO: cinco reglas, un material, tres parámetros

### F1 · EL AIRE DE CONTACTO (la única regla nueva de verdad)

Para una celda que arde (combustión persistente) o una Brasa, cada paso muestreado:
`aire = nº de vecinos ortogonales Empty o Fire` · `humo = nº de vecinos ortogonales Smoke`.
**Respira** si `aire ≥ 1 && humo ≤ 1`. Si NO respira, la celda **arde en sordina**:
- consume reserva a ¼ (salta 3 de cada 4 pasos), calor a ½, sin lengua, humo a ¼;
- la Brasa descuenta vida a ¼ y calienta a ½ (el rescoldo bancado).
Sin campo de oxígeno: el aire es el vacío que ya existe y el humo es el aire gastado. Un
oxígeno molecular no aportaría ninguna consecuencia que esto no dé y costaría un array.

Consecuencias (todas de esta regla sola): fogón abierto = rápido y caliente · pila cerrada =
lenta y templada · **regulador** = el tamaño de la boca decide cuánto respira la primera fila ·
**estufa de tolva** = un silo de fibra con boca abajo solo arde por la boca y la fibra CAE a
ocupar el sitio (es polvo) · **bancar la brasa** = taparla con ceniza (aire 0) la hace durar
×4 y volver con un soplo de fibra · un cuarto sin chimenea acaba lleno de humo y el fuego se
ahoga solo (**tiro** como necesidad, no como regla).

### F2 · LA CARBONERA (residuo según cómo ardió)

Un combustible que agota su reserva **sin respirar** deja `Carbon` en vez de Brasa. `Carbon`
(id 79, `Count` 80): polvo, inflamable (ignición 200 °C), reserva 160 (×4 fibra), calor 22
(×1,6), humo 4 %, propagación 10 %, residuo Brasa → Ash. Es el combustible de segunda
generación: enterrar fibra, encenderla por una boca, cerrar → carbón. Exactamente la tecnología
real, descubierta por geometría. Un material, cero reglas nuevas (los 7 campos de combustión
ya existen).

### F3 · EL HUMO PERSISTE Y OSCURECE

`fuego.vidaHumo` (default 400 ticks ≈ 13 s; hoy 200) aplicado a `Smoke.gasLifetime` en
`AplicarOverridesLaboratorio`, y `luz.decayHumo` (24/celda) en `LabLuzDecay`. Con eso: una
bolsa de humo bajo el techo dura lo bastante para AHOGAR (F1) y para oscurecer un claro (las
plantas de H4 lo notan). La chimenea pasa de decorado a necesidad, y su TIRO es el que ya
tiene el gas: sube 1 celda/tick, viento coherente, escapa bajo el techo hacia el hueco.

### F4 · EL HOGAR ES DOMÉSTICO (un número)

`fuego.hogarRaw` 220 → **170** (220 °C). Hierve, seca, enciende, da luz y cuece la SUPERFICIE
de la arcilla (150 raw). No llega a `fuego.vidrioRaw` (F5) ni cuece el interior de una vasija:
para eso hace falta LLAMA (255 raw) sostenida y contenida. Gratis y suficiente para vivir;
insuficiente para industria. Es la frontera que pedía Cesar, y cuesta un default.

### F5 · EL VIDRIO SOLO NACE EN HORNO (el marcador de calor industrial)

Arena con `temp ≥ fuego.vidrioRaw` (200 raw = 280 °C) y un vecino de Ceniza (el fundente, la
receta real del juego: arena + ceniza) acumula `reposo` como «tiempo al rojo»; a
`fuego.vidrioVisitas` (60 visitas ≈ 16 s) se vuelve `VidrioVerde` (id 60, ya existe: el vidrio
de botella del retículo). Si la temperatura cae, `reposo` se reinicia. Usa el material del
juego y el campo `reposo` que ya está. Ninguna llama suelta lo consigue: 255 raw en la llama
cae a ~200 a dos celdas al aire libre; solo un recinto aislado (arcilla/terracota, k2) con
carbón (calor 22) y una boca pequeña sostiene 200 raw en un volumen. **Ese es el horno**, y
nadie lo programó.

### F6 · (opcional) CHOQUE TÉRMICO

Terracota a ≥ 180 raw tocada por agua → Grava al 30 %. Enseña a enfriar despacio y hace que el
vidrio/terracota recién cocidos pidan cuidado. Cinco líneas en `LabPoroso`; solo si sobra tarde.

**Total:** F1 (≈ 25 líneas: helper `LabRespira` en el partial + 6 líneas gateadas por
`LabActivo` en `ProcessCombustion` y `ProcessBrasa` de `SimStepper.cs` — excepción autorizada),
F2 (un material + 4 líneas en el residuo), F3 (2 parámetros + 2 líneas), F4 (un default), F5
(≈ 15 líneas en `LabPoroso` caso Sand). Sin arrays nuevos. Sin oxígeno.

## 4. CADENAS QUE DEBERÍAN APARECER (sin guion)

1. **El fogón y el flash.** Fibra seca junto al hogar → prende → una pila al aire arde entera
   en segundos (respira por todas partes). Lección: aire = rápido.
2. **El regulador.** La misma pila dentro de un nicho de roca con una boca de 2 celdas arde
   solo por la boca, lenta y templada. Tapar/abrir la boca con el cincel = mando de potencia.
3. **La carbonera.** Pila enterrada, encendida por una boca, boca tapada → sordina → carbón.
   Abrir a los minutos: un montón negro que arde ×4 más y más caliente. Descubrimiento con
   nombre real.
4. **El horno.** Recinto de arcilla (tallada de la veta, secada, compactada) con carbón y una
   chimenea pequeña: dentro, 200-255 raw sostenidos; la arena con ceniza se vuelve vidrio y la
   vasija de terracota se cuece por dentro. Sin chimenea el horno se ahoga; con chimenea
   grande se enfría. **El tamaño del tiro es un óptimo que el jugador busca.**
5. **La estufa de tolva.** Silo de fibra sobre una boca de fuego: arde solo la base y el resto
   cae. Diez minutos de calor sin tocarla. Con carbón, media hora.
6. **El huerto que se quema solo.** Plantas en la cornisa sobre el foso del fuego: mueren de
   sed (o se cortan), la fibra CAE al foso, arde, la ceniza vuela mojada al sustrato → abono →
   más plantas. Riego por goteo (H3) + luz del fuego. La primera automatización cerrada del
   laboratorio: agua × luz × fuego × suelo, y el jugador solo puso la geometría.
7. **La brasa bancada.** Tapar el rescoldo con ceniza al «irse»; volver a 50× y encontrarlo
   vivo; soplarlo con fibra seca. (Con el tiempo acelerado esto es un experimento de un minuto.)
8. **La roca que guarda calor.** Una masa de terracota calentada media hora sigue secando
   fibra y arcilla lejos de la llama (k2, c3). Secadero sin fuego a la vista.
9. **El humo que mata el claro.** Fuego bajo un techo sin salida → bolsa de humo → oscuridad →
   las plantas se marchitan → fibra seca que cae al fuego. Una consecuencia que castiga y
   enseña a la vez.
10. **La destilación es fuego haciendo trabajo sobre agua** (ya vive): olla tapada + tubo +
    serpentín frío. Con carbón, sostenida sin vigilancia.

## 5. RIESGOS DE MANTENIMIENTO TEDIOSO (y su antídoto en la misma tabla)

| Riesgo | Antídoto (todo geometría o material, nunca un menú) |
|---|---|
| Alimentar fibra cada 11 s | Carbón (×4), tolva por gravedad (F1 + fibra = polvo), huerto que cae (cadena 6) |
| Reencender cada vez | Hogar eterno como piloto; brasa bancada bajo ceniza (F1); brasas en el frasco (ya viajan con su vida) |
| Ajustar la potencia a cada rato | La boca se talla UNA vez; el óptimo de tiro es un descubrimiento, no un slider |
| El fuego se come el almacén de fibra | Guardar la fibra MOJADA (no prende) o bajo el agua (flota): el propio agua es el granero |
| Fuego descontrolado que arrasa el huerto | Propagación 18 % y el aire de contacto: un huerto húmedo no arde (`LabCombustibleMojado`) |
| Humo que ahoga el hogar | El hogar no produce humo (es brasa eterna): solo se ahogan los fuegos que el jugador construye |
| Micro-vigilancia del horno | Chimenea + carbón = régimen estable de minutos; el tiempo 50× lo demuestra en un minuto real |

Criterio numérico: **intervenciones del jugador por cada 10 minutos de mundo con calor
sostenido ≥ 200 raw en un recinto: ≤ 1** (encender). Si un aparato necesita más, la regla que
falla se mide, no se maquilla.

## 6. BENCHMARKS (banco headless, sin Play)

| Escenario | Qué mide | Aceptación |
|---|---|---|
| B-F1 Incendio de 5 000 celdas de fibra al aire | coste de F1 sobre `ProcessCombustion` (dos conteos de vecinos por paso) | ≤ +10 % sobre el incendio base del banco viejo |
| B-F2 Horno cerrado vs abierto (recinto 20×12 de arcilla, 200 carbón, boca de 1, 2, 4, 8 celdas) | temperatura media interior a t=3 000 y 9 000; humo dentro; carbón restante | curva monótona boca→temperatura; con boca 2: ≥ 200 raw sostenidos ≥ 60 s |
| B-F3 Carbonera (400 fibra enterrada, boca de 1) | rendimiento carbón/fibra según boca | 40-70 % de carbón con boca 1; < 10 % con la pila al aire |
| B-F4 Bolsa de humo (10 000 celdas de Smoke, vida 400, bajo techo) | coste de gas persistente y de `luz.decayHumo` | ≤ 3 ms/tick; la bolsa se vacía por una chimenea de 6 en < 60 s |
| B-F5 Estufa de tolva (silo 12×30 de fibra, boca de 3) sin intervención | ticks de calor ≥ 150 raw junto a la boca; intervenciones = 0 | ≥ 9 000 ticks (5 min) con fibra; ≥ 4× con carbón |
| B-F6 Huerto-foso (cadena 6) 30 000 ticks | fibra caída al foso, ceniza abonada, plantas vivas | ciclo completo sin intervención al menos dos veces |
| B-F7 Libro mayor de energía | reserva quemada = calor inyectado + humo + residuos (unidades) | cuadra al bit, como el agua (`LabBalanceU` tiene hermano: `LabBalanceFuego`) |

## 7. CRITERIO: ¿EL FUEGO ALCANZÓ EL NIVEL SISTÉMICO DEL AGUA?

El agua lo alcanzó cuando cumplió cinco cosas medibles. El fuego se declara al mismo nivel
cuando cumpla las cinco, con captura y preset `ref_*` cada una:

1. **Máquina escondida en las leyes**: un aparato hecho SOLO de geometría cambia un resultado
   que sin él no ocurre (el horno hace vidrio; la llama suelta no). → B-F2 + `ref_horno`.
2. **Automatización sin jugador**: un proceso de energía sostenido ≥ 5 min de mundo con 0
   intervenciones tras encender. → B-F5 `ref_tolva`.
3. **Mando de geometría con respuesta monótona**: la boca del horno vs la temperatura. → B-F2.
4. **Cadena cruzada no guionizada**: agua × luz × fuego × suelo (cadena 6) o humo × luz ×
   plantas (cadena 9), aparecida en la jugada larga H7. → `ref_huerto_foso`.
5. **Libro mayor que cuadra**: energía y masa del combustible conservadas al bit. → B-F7.

Más el índice de tedio (§5) ≤ 1. Si cumple 5/5, el fuego enseña a controlar energía como el
agua enseñó a controlar materia. Si cumple 3/5, hay que mirar cuál de F1-F5 no tira.

## 8. HITOS PARA OPUS (después de R8/R9, antes de H5/H6)

- **HF1 Respirar** (½ día): `LabRespira` + gates en `ProcessCombustion`/`ProcessBrasa`
  (excepción autorizada en `SimStepper.cs`, 6 líneas marcadas `(R135)`), `Carbon` (id 79),
  `fuego.vidaHumo`, `luz.decayHumo`, `fuego.hogarRaw` 170. Regresión: `ref_h1_circuito`,
  `ref_destilacion`, `ref_alambique` idénticos (ninguno usa combustión). B-F1, B-F3, B-F4.
- **HF2 El horno** (½ día): F5 vidrio; escenario B-F2 con las cuatro bocas; `ref_horno`;
  captura de la curva boca→temperatura (es la figura del informe).
- **HF3 La tolva y el huerto** (1 día): B-F5 y B-F6 jugados con el pincel y con el plano real
  (la cornisa sobre un foso ya existe en la sala del hogar: úsala); `ref_tolva`, `ref_huerto_foso`.
- **HF4 Libro de energía** (½ día): `LabBalanceFuego`, F6 si sobra tiempo, y el veredicto §7
  con sus cinco casillas. Luego H5 (banco completo) y H6 (cuerpos).

**Decides tú**: todos los números (§3 son puntos de partida), la forma de la sordina (saltar
pasos vs dividir consumo), dónde poner la cornisa del huerto. **Escala**: cualquier regla de
`ProcessFire` (la lengua no se toca), oxígeno como campo, un array nuevo.

## 9. LO QUE ME SORPRENDIÓ AL DISEÑARLO

Que el «tiro» de una chimenea, la carbonera, el regulador, la brasa bancada y la estufa de
tolva sean **la misma regla** vista desde cinco geometrías. Y que la frontera entre fuego
doméstico y fuego industrial no necesite un sistema: basta con que la fuente gratuita sea
más fría que una llama y con que el único producto industrial (el vidrio) exija una
temperatura que solo un recinto sostiene. El jugador no descubre «el horno»: descubre que el
calor se escapa, y lo encierra. Eso es controlar energía.


## 10. VEREDICTO DEL ARQUITECTO (R136): lo que el banco desmintió

Opus implementó HF1-HF4 y dio 3,5 de 5 (`Laboratorio/benchmarks/2026-09-04_r135_cierres_y_fuego.md`).
Antes de aceptar la nota medí yo, en banco headless y sin tocar código
(`Laboratorio/benchmarks/2026-09-04_r136_fable_tiro_y_hogar.md`). Tres cosas de este diseño
eran falsas en la build, y las tres son mías:

| lo que decía el diseño | lo que mide el banco | qué se hace |
|---|---|---|
| F4: el hogar a 170 raw es doméstico | la celda sobre el hogar está a **255** (`InjectHeat` sin tope); arena + ceniza sobre el hogar → **vidrio en 27 s** sin horno | **C1**: el hogar no calienta a nadie por encima de su temperatura (`LabHogar`, tope) |
| F2: carbón = combustible concentrado (×4, ×1,6) | fibra 560 raw/celda → carbón 3 520 raw/celda: **×6,3**, con B-F3 al 100 % de celdas | **C2**: rendimiento 25 % y reserva 50; identidad `rend × reservaC × calorC ≈ ½ × reservaP × calorP` |
| §7.5: libro mayor que cuadra | `LabCalorFuego` cuenta el combustible; la **llama mete 40 raw/tick** sin gastar reserva (×15 el carbón que la sostiene) y no se cuenta | **C3**: `LabCalorLlama`, `LabCalorHogar`, `LabEnergiaCarbon` |
| F3: `vidaHumo` 400 | `gasLifetime` es un byte: 255 (510 bajo techo) | **C4**: default 255 y ayuda honesta |

Y la respuesta a Q9 (la boca del horno no regula): **el tiro no existe en este motor porque el
aire no se gasta**. La llama sobre combustible es inmortal, F1 la cuenta como aire y ocupa la
única celda por donde saldría el humo: subir el humo del carbón de 4 a 40 % da una simulación
idéntica al bit. Emulé «la llama es el sensor» (muere sin vecino vacío; humea por la punta): con
pila fina la curva sale monótona **al revés** (245 / 235 / 228 raw con chimenea 0 / 2 / 8: la
chimenea es una fuga de calor) y el ahogo baja el consumo un 4 %. Hacer del tiro una válvula
pediría tres reglas más y humo inmortal bajo techo: física nueva por medio punto. **No.**

**Criterio 3 reformulado:** dos mandos de geometría, medidos y monótonos por tramos —el
**recinto** (retención) y el **contacto** (qué parte del combustible toca el aire)— y ninguno es
una válvula continua. Medio punto. **Criterio 5 reformulado:** todo raw inyectado está contado
y la energía del combustible se conserva al carbonizar.

**Veredicto:** 3,5 (Opus) → **2,5 medido** → **4 tras HF5** (4,5 cuando la cadena cruzada
aparezca jugando). Recomendación a Cesar: congelar física nueva; HF5 son correcciones de
honestidad, no física; después, H7. Detalle y parches exactos: `PREGUNTAS_A_FABLE.md` R11-R14
y `HANDOFF_OPUS.md` HF5.

**Lo que me sorprendió esta vez:** que el horno de Opus funcionara por la razón equivocada. El
calor de un horno en este motor es calor de **llama** (40 raw por tick y celda, gratis), no de
combustible; el combustible solo decide cuánto dura la llama. Eso no invalida el horno como
máquina —sigue siendo geometría que encierra calor— pero cambia lo que enseña: el jugador no
domina el carbón, domina la llama. Conviene decirlo así en el informe.
