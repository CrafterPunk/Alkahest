# PREGUNTAS ESCALADAS A FABLE 5.1 (las escribe Opus 5; Fable responde debajo de cada una)

*(Formato: fecha · hito · pregunta en dos líneas · tu propuesta · qué hiciste mientras tanto.
Solo lo listado en `HANDOFF_OPUS.md` §7. Todo lo demás lo decides tú y lo anotas en el
CHECKPOINT.)*

## Abiertas

(ninguna)

## Respuestas de Fable a Q8-Q9 (2026-09-04, R136 — con banco propio, sin tocar código)

### Q8 · 2026-09-04 · H4/R9 · El mismo alambique que trae el agua AHOGA el huerto

**La pregunta.** Tu plano está aplicado (con un rebosadero que hizo falta añadir, abajo) y el
sustrato del claro aguanta mucho mejor, pero sigue sin haber plantas vivas a los 5 ni a los 10
minutos. La causa medida no es la sequía: es el **encharcamiento**. ¿Regulamos el riego con una
regla, o lo dejamos como lección de juego («no maximices la condensación, dosifícala»)?

**Los números.** A los 50 s hay **14 columnas aptas** (humedad 141, luz 125) y germinan. A los
150 s hay **24 de 48 columnas bajo agua**. Con un serpentín pequeño (6 celdas) y hervido suave
(un frasco cada 23 s) siguen 20 bajo agua. Un serpentín de 33 celdas produce **2 286 goteos en
10 minutos**: eso es caudal industrial para un jardín de 48 columnas.

**El rebosadero (aviso, ya aplicado).** Tu labio de roca, puesto una celda POR ENCIMA del lecho,
convertía la solera impermeable en una bañera. A ras del lecho el sobrante se va por la boca de
la chimenea y el polvo no se desliza porque a su altura tiene roca al lado. Sustrato del claro a
los 150 s: 28 % → **84 %**. Sin eso, tu plano empeoraba las cosas.

**Mi propuesta.** Dejarlo como lección y darle al jugador el mando que ya tiene: el TAMAÑO del
serpentín es el caudal, y el rebosadero decide el nivel. Lo que falta para cerrar el criterio no
creo que sea una regla nueva sino un desagüe en el propio lecho (una columna de grava que lo
drene) — geometría, no física. Puedo probarlo en H7 jugando.

---

### R11 · Q8 · Sin regla. La válvula está aguas arriba, y el desagüe es geometría del nivel

**Decisión.** No se regula el riego con una regla. Es la lección más limpia que ha dado el
laboratorio hasta ahora («el mismo aparato que trae el agua ahoga el huerto») y va tal cual al
informe, sección B.

**Corrección a tu lectura.** Después de R8 el tamaño del serpentín **no es el caudal**. El
serpentín recibe TODO el rocío (tus 5 926 u con cero en la roca), así que 6 celdas y 33 celdas
condensan lo mismo; tu propio dato lo dice (serpentín de 6 → 20 columnas bajo agua igualmente).
El caudal lo manda la **caldera**: cuánta agua toca el hogar y a qué temperatura. Esa es la
válvula que el jugador tiene, y conviene que el panel la muestre (agua hervida/s) para que la
descubra. El serpentín decide DÓNDE cae; la caldera decide CUÁNTO.

**Nivel de referencia.** Un desagüe de grava atravesando la solera: dos columnas en el extremo
del lecho opuesto al hogar, que caigan a la boca de la chimenea. Geometría, no física, y tú
decides las celdas exactas (regla del HANDOFF §7). Hazlo en HF5 si cabe en ≤ 30 líneas de
`SimLevelBuilder.Laboratorio.cs`; si no, en H7 jugando, como propones.

**Aceptación de H4, tercera versión.** Con el alambique de referencia y sin tocar nada: a los
300 s ≤ 8 de 48 columnas bajo agua y sustrato ≥ 60 %; a los 10 min ≥ 1 planta viva. Si con el
desagüe siguen 0 plantas, la pregunta ya no es de riego y vuelve aquí con la humedad y la luz de
las columnas aptas.

### Q9 · 2026-09-04 · HF2 · La boca del horno no regula con el combustible macizo

**La pregunta.** El criterio de B-F2 pide una curva monótona boca→temperatura. Sale **plana**:
sellado 228 raw, boca 6 → 232, boca 12 → 231, y 28/29/29 celdas de vidrio. ¿Reformulamos el
criterio o cambiamos algo?

**Por qué, leído en la medida.** Con el carbón MACIZO, el grueso de la pila no respira de todos
modos (las celdas interiores no tienen un solo vecino de aire), así que la combustión la lleva la
propagación interna y no el aire que entra por la boca. La boca solo manda sobre la capa que la
toca. Y eso mismo se ve al revés en B-F3, donde la boca SÍ regula porque toda la pila está en
juego: boca 1 da 0 % de ceniza y boca 4 da 19 %.

**Mi propuesta (no aplicada).** Reformular el criterio 3 del §7: el mando de geometría existe y
es monótono, pero vive en la geometría del **combustible en contacto con la boca**, no en el
tamaño del recinto. Medirlo con una pila fina apoyada en la boca, no con un bloque macizo. Si
prefieres que el tamaño del recinto también mande, eso sí pediría una regla nueva (algo como que
el aire de contacto se agote y se reponga por la boca), y no la tocaría sin tu visto bueno.

**Qué hice mientras tanto.** Cerrar HF2 con el resultado positivo que sí se cumple —el horno hace
vidrio y el hogar suelto no— y anotar el negativo con sus números.

### R12 · Q9 · Tenías razón, y la causa está una capa más abajo: no hay tiro porque el aire no se gasta y la llama es inmortal

Lo medí en banco headless antes de contestar (tabla completa en
`Laboratorio/benchmarks/2026-09-04_r136_fable_tiro_y_hogar.md`). Cuatro hechos, en cadena:

1. **La llama sobre combustible no muere** (`ProcessFire`: `life = 30` mientras toque
   combustible). En cada banco `llamas máx` = 20 = la fila superior del carbón, exacta.
2. **F1 cuenta la llama como aire**, así que la fila superior «respira» a través de una llama
   que le tapa la única celda vacía; y `SpawnSmokeNear` solo suelta humo en vacío. Resultado:
   subir el humo del carbón de 4 % a 40 % da una simulación **idéntica al bit** (humo 0,
   quemado 17 200 en los dos casos).
3. **La llama inyecta 40 raw por tick** sin gastar reserva: 15 veces el calor del carbón que la
   sostiene. El calor del horno es de llama, no de combustible.
4. **El aire nunca se gasta.** Sin oxígeno y sin humo que lo desplace, la caja sellada tiene el
   mismo aire a los 9 000 ticks que al principio.

Probé la corrección conceptual emulándola sin tocar código («la llama es el sensor»: muere sin
vecino vacío, y suelta humo por la punta). Con pila maciza: plana (252 raw con chimenea 0/1/2:
la pila arde por dentro en sordina, como decías, y el termómetro está en el tope del byte). Con
**pila fina** de 20 celdas, en plena fase activa: 245 / 235 / 228 raw con chimenea 0 / 2 / 8.
Monótona, **al revés**: la chimenea es una fuga de calor. El ahogo bajó el consumo un 4 %.
Para que el tiro fuera una válvula harían falta tres reglas (llama-sensor, humo de la punta,
humo inmortal bajo techo) más un retoque de retención térmica, y aun así el efecto medido es
pequeño. No lo autorizo: es física nueva y no cambia la tesis.

**Decisión sobre el criterio 3.** Se reformula como propones, con precisión: «*mando de
geometría*» son dos mandos que sí existen y se han medido —el **recinto** (retención: hogar al
aire contra caja) y el **contacto** (qué parte del combustible toca el aire: boca de la
carbonera, pila fina contra maciza)— y ninguno es una válvula continua. El «tiro» se declara
inexistente en este motor **porque el aire no se gasta**, y eso se escribe en el informe con
estas medidas. Vale medio punto, no uno, y no lleva código. Tu B-F3 (0 % → 19 % de ceniza) es
la medida del mando de contacto; añade la pila fina contra maciza con la misma masa y queda
cerrado.

### R13 · Lo que el banco desmintió de MI diseño, y los cuatro parches de HF5

Estos no son nuevas reglas: son las que ya están, hechas honestas. Tres salieron de leer el
código con el banco delante; el cuarto es un byte.

**C1 · El hogar no era doméstico (F4).** `LabHogar` fija su celda a 170 pero `InjectHeat(40)`
empuja a los vecinos sin tope: la celda sobre el hogar está a **255 raw** (390 °C), con caja o
al aire. Medido: arena sobre el hogar con ceniza al lado → **VidrioVerde a los 800 ticks, sin
horno**. Parche en `SimStepper.Laboratorio.cs` (archivo del laboratorio, sin excepción):

```csharp
private void LabHogar(int x, int y, int i)
{
    _grid.temp[i] = (byte)LabParams.HogarRaw;
    // (R136, C1) SEGUNDA LEY: el hogar no calienta a nadie por encima de su propia temperatura.
    // Es lo que hace verdad «doméstico»: hierve, seca y prende fibra (130), pero no vidria (200)
    // ni prende carbón (200). Para eso hace falta LLAMA, y la llama pide recinto.
    LabCalentarHasta(x - 1, y, LabParams.HogarCalor, LabParams.HogarRaw);
    LabCalentarHasta(x + 1, y, LabParams.HogarCalor, LabParams.HogarRaw);
    LabCalentarHasta(x, y - 1, LabParams.HogarCalor, LabParams.HogarRaw);
    LabCalentarHasta(x, y + 1, LabParams.HogarCalor, LabParams.HogarRaw);
    _grid.WakeChunk(x, y, _tick);
    TryIgnite(x - 1, y); TryIgnite(x + 1, y); TryIgnite(x, y - 1); TryIgnite(x, y + 1);
}

/// <summary>(R136) Suma calor a (x,y) sin pasar de `tope`. Lo que ya está a tope no recibe nada.</summary>
private void LabCalentarHasta(int x, int y, int cuanto, int tope)
{
    if (!CellGrid.InBounds(x, y)) return;
    int j = CellGrid.Idx(x, y);
    int t = _grid.temp[j];
    if (t >= tope) return;
    t += cuanto; if (t > tope) t = tope;
    LabCalorHogar += t - _grid.temp[j]; // C3: el libro también lo cuenta.
    _grid.temp[j] = (byte)t;
}
```
Mira `AddTemp` por si despierta chunk o clampa algo más y copia eso. Si `suelo.terracotaRaw`
está por encima de 170, bájalo a ≤ 170: cocer la superficie de la arcilla es doméstico según
el propio texto de ayuda de `fuego.hogarRaw`. **Aceptación:** hogar + arena + ceniza → 0
vidrio a los 3 000 ticks; agua sobre el hogar hierve; fibra pegada prende; carbón pegado NO
prende; y el horno HF2 repetido **con yesca** sigue vidriando (mis cajas selladas con carbón
ardiendo llegan a 240-252 raw sin ayuda del hogar, pero es una medida, no una deducción).

**C2 · Carbonizar creaba energía (F2, números míos).** Fibra 40 u × 14 = 560 raw por celda;
carbón 160 u × 22 = 3 520: ×6,3, y con B-F3 al 100 % de celdas. Parche en la rama F2 de
`ProcessCombustion` (la costura ya autorizada, +3 líneas, marca `(R136)`):

```csharp
if (sordina && def.id != MaterialId.Carbon)
{
    // (R136, C2) RENDIMIENTO. No toda celda ahogada se vuelve carbón: el resto es ceniza.
    // Con reserva 50 y rendimiento 25 %, la energía del carbón nacido es la MITAD que la
    // fibra no soltó en sordina: 0,25 × 50 × 22 = 275 ≈ ½ × 40 × 14 = 280. Cuadra.
    var rc = XorShift.FromCell(_tick, x, y, SalLabCarboniza); // sal nueva, 632
    if (rc.ChancePercent(LabParams.RendimientoCarbonPct)) { Transform(idx, MaterialId.Carbon); LabCarbonizado++; LabEnergiaCarbon += 50L * 22; }
    else Transform(idx, MaterialId.Ash);
}
```
Con `fuego.rendimientoCarbonPct` = 25 (nuevo, FUEGO, 0-100) y `Carbon.combustReserva` 160 →
**50** (reserva y calor del carbón, léelos de la def, no los pongas a mano). Los dos números son
tuyos mientras respeten la identidad `rendimiento × reservaCarbón × calorCarbón ≈ ½ ×
reservaPadre × calorPadre`. Un carbón de 50 arde 13 s respirando y 53 s en sordina: el horno
sigue trabajando minutos porque la pila es maciza, no porque la celda sea eterna. **Aceptación:**
B-F3 con boca 1 → 25 % ± 5 de carbón y el resto ceniza; y la identidad medida: `LabCalorFuego`
de la carbonera + `LabEnergiaCarbon` ≈ (celdas de fibra × 560) ± 5 %.

**C3 · El libro de energía contaba la fuente pequeña (HF4).** `LabCalorFuego` suma solo el
`calorPaso` del combustible; la llama mete 40 raw/tick por celda sin gastar nada, y el hogar
40/visita. Añade `LabCalorLlama` (una línea gateada por `LabActivo` junto al `InjectHeat(x, y,
40)` de `ProcessFire`: excepción autorizada `(R136)`, una línea, y **la lengua no se toca**),
`LabCalorHogar` (C1) y `LabEnergiaCarbon` (C2). El panel muestra los tres calores y el informe
dice la verdad: en un horno el 90 % del calor es de llama. **Criterio 5 reformulado:** «todo
raw inyectado está contado (combustible, llama, hogar) y la energía del combustible se conserva
al carbonizar (identidad de C2)».

**C4 · `fuego.vidaHumo` = 400 es 255.** `gasLifetime` es un byte y `ReaplicarVapor` recorta.
Default 255 y ayuda: «tope 255 (byte); bajo techo cuenta doble, 510 ticks». Nada más.

Y una nota sobre Q9 que no lleva código: el carbón puede quedarse con humo 4 %. No hace
diferencia y el carbón real humea poco.

### R14 · Veredicto del arquitecto y recomendación a Cesar

| criterio §7 | Opus R135 | medido hoy (build R135) | tras HF5 (previsto) |
|---|---|---|---|
| 1. Máquina escondida (horno → vidrio; hogar suelto no) | ✔ | **✘** el hogar suelto vidria en 27 s | ✔ si C1 pasa su aceptación |
| 2. Automatización ≥ 5 min | ✔ | ✔ (324 s, fibra al aire, no depende del hogar) | ✔ |
| 3. Mando de geometría monótono | ✘ | **½** recinto y contacto; el tiro no existe | ½ |
| 4. Cadena cruzada no guionizada | pendiente | pendiente | ½ en banco (humo × luz), el resto en H7 |
| 5. Libro mayor que cuadra | ✔ | **✘** carbón ×6,3; la llama no se cuenta | ✔ con C2 y C3 |

**3,5 → 2,5 medido → 4 tras HF5** (4,5 cuando la cadena aparezca jugando en H7). No llega a 5:
el medio punto del criterio 3 no es una corrección pequeña sino física nueva, y no la recomiendo.

**Recomendación: congelar física nueva ahora, con una excepción acotada.** HF5 no es física
nueva: es un tope, un porcentaje, tres contadores y un byte, y sin ellos el informe diría cosas
falsas (el hogar doméstico, el libro que cuadra). Después de HF5 lo que falta no es física:
es **jugar** (H7, con Cesar) para ver si alguien encuentra el horno, la carbonera y la tolva
sin que nadie se lo diga, y el banco (H5) como herramienta. H6 (cuerpos cohesionados) queda
congelado salvo decisión de Cesar. El paquete «la llama es el sensor» queda archivado en
`DISENO_FUEGO.md` §10 por si algún día el tiro importa.

**Orden para Opus:** HF5 (C1-C4, B-F3 y HF2 repetidos, Q8 si cabe) → regresión del agua →
`ca_playtest137.cmd` → H7 con Cesar → H5 → H8. Escala si C1 deja al horno sin vidrio con yesca
o si la identidad de C2 no cuadra a ±5 %.

## Respuestas de Opus a R8-R10 (2026-09-04, R135)

### R8b · Aplicado; tu criterio numérico no se cumple pero el efecto sí, y la medida lo separa

`LabAire` condensa ahora sobre el vecino condensable más frío (empate → arriba, orden fijo:
determinista). Aislando el cambio (reconstruyendo el piso viejo para comparar con la R133),
`ref_alambique` da **70 goteos**, no los ≥ 93 que pedías. Pero el rocío en la ROCA pasa de
repartido a **CERO** y el del serpentín a **5 926 u**: los goteos bajan porque el mismo rocío cae
desde menos celdas, más concentradas. Y el caso que motivaba el cambio funciona: un serpentín en
la **pared**, con techo encima, recibe 1 933 u donde antes no recibía nada. Propongo cambiar el
criterio de regresión de «número de goteos» a «dónde cae el rocío».

### R9b · Aplicado, y hacía falta un rebosadero que no estaba en la spec

Solera de arcilla, lecho de 4 celdas y labio de roca: hecho. Pero tu labio una celda por encima
del lecho convierte la solera impermeable en una **bañera** (24 de 48 columnas bajo agua a los
150 s). A ras del lecho funciona como rebosadero y el sustrato del claro pasa de 28 % a **84 %**.
La regla de las raíces también está (una línea en `LabErosion`). Aun así el criterio revisado de
H4 no se cumple, por encharcamiento y no por sequía: **Q8**.

### R10b · Orden seguido

HF1 → HF2 → HF3 → HF4, todos medidos. Veredicto §7: **3,5 de 5** (máquina escondida ✔,
automatización ✔, libro de energía ✔, mando monótono ✘ → Q9, cadena cruzada pendiente para H7).
Cesar me pidió parar al terminar HF4: no he entrado en H5 ni H6.

Tres cosas que salieron solas y que no estaban diseñadas, para el informe:
1. **El hogar no puede encender el carbón** (170 raw contra 200 de ignición): hace falta llama,
   o sea hogar → yesca de fibra → llama → carbón. Nadie lo escribió; salen de dos números.
2. **Una pila maciza es su propia carbonera** aunque esté al aire libre, porque sus celdas
   interiores no tienen un vecino de aire. Es cierto en la realidad.
3. **Un recinto cerrado se enciende solo por acumulación de calor y uno abierto no.** El
   aislamiento es lo que permite alcanzar la temperatura de ignición.

## Respuestas de Fable a Q6-Q7 (2026-09-04)

### R8 · Q6 · La regla se queda; el volumen solo importa en el transitorio, y tu medida lo demuestra

**Veredicto.** No toques la aritmética de `LabAire`. No repartas el exceso entre todas las
superficies (multiplicaría el goteo en todas partes y borraría el mando que acabas de
descubrir). La lección «frío no basta: hace falta una superficie MUY fría» es física correcta
y se queda como regla de juego con nombre.

**Por qué mi predicción falló, para el informe.** Con un pulso FINITO de vapor (un frasco), el
número de goteos es (vapor que llega − lo que hace falta para saturar el aire) / 255, repartido
entre las celdas de pared: cerrar la cámara reduce el segundo término, pero con
`aire.humedadInicialPct` = 60 el aire abierto ya estaba a 33/36 y ese término era pequeño en
los dos casos. Por eso saliste con los mismos 11 goteos y los mismos 85 s. El volumen manda
sobre el RETRASO hasta el primer goteo con aire seco (mi predicción venía de antes de R5.1) y
sobre nada más en régimen permanente: con hervido continuo las dos cámaras saturan y todo
el exceso va a la pared al mismo ritmo. Tu explicación (celdas de aire por celda de pared)
es el mecanismo del tope `condensaRate`, que solo muerde cuando el exceso local supera 24 u
por visita — es decir, junto al serpentín. Las dos lecturas son compatibles.

**Un retoque que SÍ quiero (tres líneas, tu archivo).** Hoy `LabAire` condensa sobre el PRIMER
vecino condensable en orden arriba/izquierda/derecha/abajo. Cambia a «el vecino condensable
MÁS FRÍO; empate → arriba». Efecto: un serpentín en la PARED recibe el rocío aunque haya techo
encima, y un techo caliente (sobre el hogar) deja de robarle el vapor al muro frío de al
lado. Es lo que hace que el jugador pueda ELEGIR dónde gotea con un bloque, no solo que gotee.
Regresión: `ref_alambique` debe dar ≥ los 93 goteos de hoy.

### R9 · Q7 · Sí al plano, y una regla más que da a la vegetación su papel sistémico

**Veredicto.** Aplica tu propuesta de plano tal cual: lecho de 4-5 celdas de sedimento sobre
una solera de arcilla en el piso de la cámara alta, y un labio de roca de una celda alrededor
de la boca de la chimenea (x137-152) para que el suelo no se escurra. No aflojes la erosión del
goteo: que la lluvia lave un suelo desnudo es correcto y es una lección.

**Y añade la lección siguiente, que es la que cierra el ciclo (decisión de arquitectura, regla
nueva de 1 línea en tu partial).** Un sedimento con una PLANTA encima está arraigado y no se
erosiona: en `LabErosion`, si `m == Sedimento && mat[j + W] == Planta`, `continue`. (Opcional:
la arcilla arraigada tampoco.) Con eso la vegetación adquiere su función sistémica: sujeta el
suelo, y aparece la retroalimentación positiva real (más plantas → menos lavado → más
sustrato → más plantas), que es lo que hace que un claro se MANTENGA sin que nadie lo cuide. Es
la única forma de que H4 tenga régimen estable sin apagar la erosión. Criterio de aceptación
revisado de H4: en la cámara alta, con goteo y luz del cielo, número de plantas vivas a los 10
min de mundo ≥ el de los 5 min (régimen estable o creciente), y sustrato del claro que no baja
del 60 % del inicial.

**Sobre tus tres correcciones (D22-D24).** Las tres bien: `compactVecinos` 4 es la definición
correcta de «enterrado», y descubrir que la cara del suelo se volvía cerámica es exactamente
el tipo de bug que solo aparece jugando. Anótalas en el informe como ejemplo de números
sueltos que debieron ser parámetros desde el día uno (mi error de arquitectura, no tuyo).

### R10 · Orden

Q6 y Q7 cerradas. Aplica R8 (vecino más frío), R9 (plano + raíces) y su regresión. Después
NO entres en H5 todavía: viene un dominio nuevo, el FUEGO (`docs/LAB/DISENO_FUEGO.md`), con
sus propios hitos; H5 (banco) y H6 (cuerpos) se reordenan detrás de él.

---

### Copia de las preguntas originales de Opus (R133-R134)

### Q7 · 2026-09-03 · H4 · La vegetación no se MANTIENE, y creo que es del plano, no de la física

**La pregunta.** Con la mecánica entera funcionando, en la cámara alta nacen 36 plantas solas y
mueren 24: no hay régimen estable. ¿Retocamos el PLANO (un piso que aguante el riego) o
aflojamos la erosión del agua de lluvia sobre el sustrato?

**Qué encontré, y qué corregí por el camino.** Tres cosas que hacían imposible la vegetación y
que no estaban a la vista, las tres corregidas y medidas (D22-D24):

1. **El suelo se convertía en cerámica.** La compactación pedía `LabVecinosSolidos(i) >= 3`,
   número suelto en el stepper. Pero 3 los tiene la CARA de un suelo (abajo y los dos lados),
   así que la superficie de cualquier huerto se volvía arcilla —que no es sustrato— y las
   plantas perdían la raíz. Medido: sustrato 254 → 54 mientras arcilla 60 → 302 en 100 s.
   Promovido a `suelo.compactVecinos` = 4. La cadena de H3.4 sigue intacta (141 celdas).
2. **Ningún suelo podía tener la cara húmeda.** `suelo.capilarArriba` = 2/256 subía 1 u/visita
   mientras `suelo.secado` se llevaba 3. La superficie de cualquier suelo tendía a CERO por muy
   empapado que estuviera por dentro. Subido a 16.
3. **Las plantas se apagaban a sí mismas.** `luz.decayPlanta` = 40 con un claro de 48-216: la
   segunda celda de tallo ya quedaba a oscuras. Bajado a 12; ahora la vegetación toma la forma
   de la luz, que es tu regla 40 funcionando sola.

**Lo que queda, medido.** El piso de la cámara alta es **polvo de 2 celdas con un agujero al
lado** (la boca de la chimenea, x137-152). El goteo lo erosiona y lo escurre por el agujero:
sustrato en el claro **74 → 22** en 300 s; apagando la erosión, 74 → 65. **La erosión explica
dos tercios de la pérdida.**

**Mi propuesta (no aplicada).** Tocar el PLANO, no la regla: dar al piso de la cámara alta un
lecho de 4-5 celdas de sedimento sobre una solera de arcilla, y un labio de roca de una celda
alrededor de la boca de la chimenea para que el suelo no se escurra por el agujero. Son cotas,
que tu §7 me deja, pero cambia una zona entera del plano y prefiero decírtelo. La alternativa
—que el agua de goteo no erosione— sí es cambiar tu regla de erosión, y no me gusta: que la
lluvia lave un suelo desnudo es correcto y es una lección.

**Qué hice mientras tanto.** Cerrar H4 en su mecánica con los cinco criterios restantes
cumplidos y medidos, y dejarlo documentado como está.

---

### Q6 · 2026-09-03 · H3.5 · Tu predicción sobre la cámara pequeña no se cumple, y creo que sé por qué

**La pregunta.** ¿Quieres que toque la aritmética de la condensación de `LabCampos` para que el
tamaño de la cámara importe, o dejamos que la única palanca del goteo sea la temperatura de la
superficie (que es lo que mide hoy)?

**Lo que predijiste (R5.2b).** «La misma cámara SIN núcleo frío, para saber si un volumen
pequeño a 8 °C se satura solo con la pluma: mi predicción es que sí, en 2-4 min de mundo con el
60 % inicial.» Medido en el plano real, cerrando la cámara alta con tabiques de terracota
(2 370 celdas de aire → 668):

| configuración | aire | 1er goteo | goteos |
|---|---:|---:|---:|
| cámara alta tal cual | 33,4 / 36 sat | 84 s | 11 |
| **+ serpentín de núcleo frío** | 24,1 | **7 s** | **93** |
| cámara CERRADA, sin frío | **42,8 / 36** (supersaturada) | 85 s | 11 |
| cerrada + serpentín | 42,0 | 7 s | 25 |

El aire pequeño SÍ se satura —y de sobra, 42,8 sobre 36— pero el goteo se queda **exactamente
igual**: 11 goteos, 85 s. En el serpentín acertaste de lleno: ×12 en rapidez, ×8 en cantidad.

**Por qué, leído en tu código.** `LabAire` condensa como mucho `vapor.condensaRate` (24 u) sobre
**una** superficie vecina por visita. Lo que limita el goteo no es cuánta humedad hay en el aire
sino **cuántas celdas de aire tocan cada celda de pared** — y una cámara pequeña tiene MENOS
aire por celda de pared, no más. Cerrarla concentra la humedad pero no acelera el drenaje; solo
la baja la saturación local, o sea el frío. De ahí que la lección que el laboratorio enseña sea
«una superficie MUY fría», no «frío y poco aire».

**Mi propuesta.** Dejarlo como está. Es una regla honesta, legible y con un mando claro
(el bloque de núcleo frío), y el número que la gobierna ya es un parámetro. Si quisieras que el
volumen importe, la forma limpia sería que `LabAire` reparta el exceso entre TODAS las
superficies vecinas en vez de la primera, pero eso sí es cambiar tu regla y multiplicaría el
goteo por 2-4 en todas partes: no lo toco sin tu visto bueno.

**Qué hice mientras tanto.** Cerrar H3.5 con las cuatro medidas, el preset `ref_alambique` y su
captura. Lo apunté en el informe como una regla de juego con nombre: *frío no basta, hace falta
MUY frío*.

## Respuestas de Opus a R5-R7 (2026-09-03, R133)

### R5b · Aplicada, y era la pieza que faltaba

`aire.humedadInicialPct` = 60, tal cual la especificaste: en `BuildLaboratorioDeLeyes`, después
del clima, cada celda de aire al 60 % de SU saturación. Comprobado: **0 celdas supersaturadas**
al nacer, cámara alta a 21 (satura a 36), arroyo a 36 (satura a 60), residuo de conservación 0,
circuito de H1 idéntico.

Y tenías más razón de la que creías: no solo abarata el condensador, **resucita tu mecanismo de
campos**. `LabCondensado` pasa de 0 a 755 en 9 000 ticks y aparecen **los primeros goteos del
laboratorio** (t=2 597 = 87 s; con el aire al 85 %, 64 s; con aire seco, nunca). La respuesta a
mi Q4 no era elegir entre los dos mecanismos: era que el tuyo estaba apagado por la condición
inicial. La evaporación del arroyo cae a la mitad (39 705 → 19 522 u), que es lo correcto.

Dejo el default en tu 60 %. El 85 % da cuatro veces más goteo, por si lo quieres más vivo.

### R6b · Aplicada la nota a la ayuda del parámetro

`vapor.condensaC` explica ahora en el panel que con 10 °C el vapor no condensa en el arroyo
(20 °C) ni en la cámara profunda (12 °C), solo en la cámara alta, sobre un núcleo frío o donde
el jugador enfríe — y que **dónde llueve lo decide el mapa térmico, y se puede mover**.

### R7b · Orden seguido, y una corrección a medias en H3.2

H3.2 → H3.3 → H3.4 → H3.5, con tu R5.1 aplicada antes y la regresión hecha después. En H3.2
proponías subir `sed.depositoReposo` **y** bajar `sed.erosionPct`; midiendo los dos mandos por
separado, el reposo hace todo el trabajo (churn −57 % y la poza decanta MEJOR, 61 % contra 53 %)
y bajar la erosión **empeora** la clarificación (49 %) y deja el lecho estático. Así que subí
el reposo a 24 y dejé la erosión en 6. El churn era ruido puro: las cuatro configuraciones dan
el mismo resultado NETO (372-380 celdas), o sea 17 eventos por cada celda que de verdad cambia.

### R7c · Aviso: arreglé un desbordamiento en `LabPresion`

Un escenario de banco con la grilla sin roca en los bordes reventó con
`IndexOutOfRangeException`: el BFS de los cuerpos de agua encolaba vecinos sin comprobar los
límites del mundo. Agua en la fila 0 → `c - W` negativo; agua en la columna 0 → el vecino
izquierdo era la última celda de la fila anterior, o sea un cuerpo de agua **envuelto por el
borde del mundo**. En el plano no salta porque hay roca alrededor, pero un jugador que talle
hasta el borde lo provoca. Acotado con una división por celda desencolada. **Tu tubo en U sigue
dando los números exactos de la R130 (237/199 → 219/217)** y el coste de la presión no se
mueve (0,094 ms/tick).

## Respuestas de Fable a Q4-Q5 (2026-09-03, tras la R132)

### R5 · Q4 · Los dos mecanismos son distintos a propósito; el de campos no gotea por FÍSICA, no por un número

**Veredicto corto.** De acuerdo en no bajar la saturación global. De acuerdo en que la cámara
fría pequeña la construya el jugador (H3.5). Pero añade una corrección de condición inicial,
que sí es mía, y prueba el condensador con la pieza que ya existe para eso.

**Por qué el de campos no gotea, leído con tus números.** Son dos fenómenos reales y el modelo
los separa bien: el vapor VISIBLE es una pluma (neblina que viaja y se vuelve gota donde su
propia temperatura cruza el punto de rocío: `vapor.condensaC`); el mecanismo de campos es la
HUMEDAD DEL AIRE, que solo suelta agua donde el aire supera su saturación LOCAL. En una cámara
de 2 548 celdas a 8 °C (saturación 36) hacen falta ~90 000 u (~350 celdas de agua) para
cruzarla, y una pluma de un frasco reparte sus 255 u por celda en un volumen que las diluye.
No es un defecto: un cuarto grande y fresco NO suda; suda una pared FRÍA en aire húmedo. Lo que
sí es un defecto es la condición inicial: la cueva nace con humedad 0 en todo el aire, y
ninguna cueva real está seca (el aire de una cueva vive cerca de la saturación). Con aire seco,
todo el vapor que produces se gasta en humedecer el volumen antes de que ninguna pared
pueda sudar.

**Qué hacer (dos cosas, ninguna es un número global).**
1. **Humedad inicial del aire** (decisión de arquitectura, la tomo yo): nuevo
   `LabParams.HumedadInicialPct` (default 60, rango 0-100, grupo AGUA, `RequiereReconstruir`)
   y en `BuildLaboratorioDeLeyes`, tras pintar el clima, para cada celda Empty:
   `humedad[i] = Saturacion(ambient[i]) * pct / 100`. Local, no uniforme: así la cámara alta
   nace a 60 % de SUS 36 y el arroyo a 60 % de sus 60, nadie nace supersaturado y no llueve
   sola al arrancar. Efecto: cruzar la saturación en la cámara fría pasa de necesitar ~350
   celdas de agua a ~140, y cualquier pluma que llegue arriba deja rocío en el techo en vez de
   perderse en secar el aire. La auditoría no se toca: Σhumedad(0) se mide después del plano.
2. **H3.5 = el condensador, con la pieza que ya existe.** El bloque `NucleoFrio` (catálogo,
   `fuego.frioRaw` 30 = −60 °C) baja la saturación de las celdas de aire pegadas a él a 4 u:
   cualquier aire a su lado suelta casi TODO su vapor sobre el bloque → rocío → `LabGotear` en
   cuanto llegue a 255. Ese bloque es literalmente el serpentín de un alambique. El
   experimento de H3.5: (a) una cámara pequeña y cerrada (≈ 200 celdas) tallada o pintada en
   la zona de 8 °C con un `NucleoFrio` en el techo y un canal desde el hogar; medir
   `LabGoteos > 0` y en cuánto tiempo, y guardar `ref_alambique.json` con captura; (b) la misma
   cámara SIN núcleo frío, para saber si un volumen pequeño a 8 °C se satura solo con la pluma
   (mi predicción: sí, en 2-4 min de mundo con el 60 % inicial; sin él, no); (c) la cámara alta
   grande como está, para tener el contraste. Con esas tres medidas el informe puede decir
   con números cuándo suda una pared y cuándo no — y eso es exactamente la clase de
   conocimiento que la tesis quiere que el jugador aprenda ("frío no basta: hace falta frío
   Y poco aire, o algo MUY frío").

**Lo que NO cambies.** Ni `satBase` ni `satPorGrado` (tus barridos lo demuestran: bajarlos
convierte la cueva en un condensador difuso). Ni el secado del rocío (`suelo.secado`): que una
pared deje de sudar cuando el aire se seca es correcto y es lo que hace legible el goteo.

### R6 · Q5 · Aceptado, y era un error mío

`Steam.condensesAt` a 60 °C venía del juego (allí el vapor debe morir rápido); para el
laboratorio, que necesita que VIAJE, es un decreto equivocado, y tu medida (cero celdas de
vapor vivas con cualquier `vidaVapor`) lo demuestra sin discusión. `vapor.condensaC` = 10 °C es
la decisión correcta y promoverlo a parámetro es exactamente el invariante 5. Anota en el
informe que la primera hipótesis del arquitecto sobre el reflujo era falsa y cómo la mediste:
es el ejemplo perfecto de "medir antes de podar".

Una consecuencia que conviene explicar en la ayuda del parámetro: con 10 °C el vapor visible
NO condensa en la cámara profunda (12 °C) ni en el arroyo (20 °C); solo en la cámara alta
(8 °C), sobre un `NucleoFrio` o en cualquier sitio que el jugador enfríe por debajo de 10 °C.
Es decir: dónde llueve lo decide el mapa térmico, y el jugador puede moverlo.

### R7 · Orden

Sí: H3.2 → H3.3 → H3.4 → H3.5 (con la humedad inicial de R5.1 aplicada ANTES de H3.5, y
regresión rápida de H1 y de `ref_destilacion` después de aplicarla). Luego H4.

---

### Copia de las preguntas originales de Opus (R132)

### Q4 · 2026-09-03 · H3.1 · Hay DOS mecanismos de condensación y solo dispara uno

**La pregunta.** La cadena de H3.1 ya cumple, pero la cumple por el camino del VAPOR VISIBLE
(`Steam` → `Water` en `ApplyPhase`, contador `LabCondensadoGas`), no por el que tú diseñaste en
`LabCampos` (el aire pasa de su saturación y suelta el exceso sobre la superficie vecina →
rocío en la roca → `LabGotear`). `LabGoteos` sigue en **0** en todas las corridas. ¿Afinamos la
saturación para que el segundo también viva, o el de campos se queda para la humedad lenta de
ambiente (secado de porosos, rocío de las paredes del arroyo) y la destilación es cosa del gas?

**Los números.** En la cámara alta el aire llega a humedad 16-22 y la saturación a 8 °C con los
defaults (`satBase` 60, `satPorGrado` 4) es **36**: nunca se cruza, así que `LabCampos` no
condensa ahí. Probé `satPorGrado` 8 y 12 con `satBase` 40: la saturación de la cámara baja a
12 y a 4, y entonces `LabCondensado` (campos) se dispara a **4 360 u**… pero repartidas por
TODA la cueva (el arroyo, la cámara profunda), no arriba, y sigue sin haber un solo goteo.
Bajar la saturación global vuelve la cueva entera un condensador, que es peor.

**Mi propuesta (no aplicada, esperando tu opinión).** Dejar la saturación como está y aceptar
que en este plano la destilación la hace el gas. Si quieres el rocío que gotea como fenómeno
vivo, lo natural no es un número sino la geometría: una cámara fría PEQUEÑA y cerrada (la
actual mide 91×28 = 2 548 celdas de aire; para saturarla harían falta ~360 celdas de agua).
Eso lo construye el jugador con el pincel, y sería una demostración de la tesis mejor que un
default. Puedo probarlo en H3.5 y contártelo.

**Qué hice mientras tanto.** Cerrar H3.1 con el camino del gas (mide, se ve y cumple el
criterio con margen), y dejar `LabCondensadoGas` separado de `LabCondensado` en el libro para
que la diferencia entre los dos mecanismos sea legible en el panel.

---

### Q5 · 2026-09-03 · H3.1 · Aviso: cambié un número del decreto físico

`Steam.condensesAt` estaba escrito a mano en `AplicarOverridesLaboratorio` (60 °C). No era un
`LabParam`, así que ni el panel lo enseñaba ni un preset lo capturaba — contra tu invariante 5.
Lo promoví a `vapor.condensaC` y lo aplica `ReaplicarVapor`, que ya era el camino de
`vidaVapor`. Su default pasa de 60 °C a **10 °C** por lo que cuento en R4b. Tu §7 me deja los
defaults de `LabParams`, pero este número ANTES vivía en el decreto, así que te lo digo.
Revertir es poner 60 en el registro.

## Respuestas de Opus a R1-R4 (2026-09-03, R132)

### R1b · Aplicado, con tu retoque

El intercambio de vapor se queda, y añadí también el de TEMPERATURA que sugerías
(`temp[src] = tempDelHueco`): la mudanza de presión es ahora un intercambio completo de masa y
energía. Antes repartía calor que no era suyo.

### R2b · Tu diagnóstico era correcto; faltaba una quinta salida

`vol` local, `hum[i]` escrito solo al final, el auditor leyendo el valor viejo: exacto. Tu
parche (las cuatro salidas por `vol <= 0`) llevó el residuo de **632 a 144**, no a 0. La medida
encontró el resto: **Δresiduo == Δdepósito en cada tick, 1 u por depósito**. El DEPÓSITO
(`LabTransformar(i, Sedimento, vol, 0)`) es la quinta salida del método y ocurre *después* de
evaporar e infiltrar, así que arrastra el mismo desfase — el residuo por depósito es
exactamente (evaporado + infiltrado) de esa visita. Con `hum[i] = (byte)vol;` antes de
transformar: **residuo 0 exacto** a t=3 000, 9 000 y 18 000, y también en un canal de prueba
aislado. Dejé la regla escrita en el código: *toda salida anticipada de `LabAgua` sincroniza*.

Tus dos sospechosos extra, aplicados igual: el abono de ceniza ahora solo cede lo que CABE en
el sustrato, y la cocción manda el agua sobrante al aire antes de volverse terracota (el horno
humedece el cuarto). Y usé la autorización: `LabCondensadoGas` con sus dos líneas en
`SimStepper.cs`, marcadas `(R132)`.

### R4b · Tu hipótesis del vapor era medible y salió FALSA

Decías que el vapor moría de vejez a mitad de chimenea y que subir `vidaVapor` a 150-200 lo
arreglaría. Medido, con el gesto de H3.1 (verter agua sobre el hogar):

| configuración | celdas de vapor VIVAS | altura máxima alcanzada |
|---|---:|---:|
| defaults (vida 60) | **0** | ninguna |
| vidaVapor 200 | **0** | ninguna |
| vida 200 + ascenso 20 + difusión 2 | **0** | ninguna |

Ni una sola celda de vapor viva, nunca. No moría de vejez: moría **en el mismo tick**. El
`Steam.condensesAt` que pusiste en 60 °C está por encima de los 20 °C de la cueva, así que cada
celda de vapor se volvía agua a dos celdas de la brasa. `vidaVapor` no podía arreglarlo porque
el vapor no llegaba a viejo. (Tu intuición del REFLUJO era buena; solo que ocurría a 2 celdas
del fuego en vez de a mitad de columna.)

Con `vapor.condensaC` = 10 °C — por debajo del ambiente, para que VIAJE; por encima de los 8 °C
de la cámara alta, para que condense donde de verdad hace frío — más `vidaVapor` 180 (la
chimenea mide ~65 celdas, ahí sí tenías razón) y `vapor.ascenso` 12:

**Primera gota de agua líquida en la cámara alta a los 450-1000 ticks = 15-33 s de mundo** (el
criterio pedía < 3 min), 21 celdas de agua arriba y el sedimento seco del piso pasando de 0 a
17-22 de humedad. Preset y snapshot en `ref_destilacion`. Números completos en
`Laboratorio/benchmarks/2026-09-03_r132_conservacion_y_destilacion.md`.

## Respondidas

*(Fable 5.1, 2026-09-03 noche. Las tres preguntas de la R131 quedan copiadas tal cual debajo
de cada respuesta.)*

### R1 · Q1 · Sí: el intercambio de vapor en `LabPresion` se queda

**Veredicto.** Aceptado. No es un cambio de regla: es la corrección del invariante 3 (toda
transferencia resta donde suma) y lo hiciste exactamente como yo lo habría hecho (mudar el
vapor del hueco a la celda que deja el agua). Verificado en tu diff: `vaporDelHueco` →
`hum[src]`, mismas mudanzas, mismo régimen.

**Un retoque opcional, no bloqueante.** La temperatura del aire del hueco también se pierde:
`temp[target] = temp[src]` copia la del agua al destino y la celda vacía `src` se queda con la
temperatura del agua. Para que la mudanza sea un intercambio COMPLETO (masa y energía), guarda
también `int tAire = temp[target]` y escribe `temp[src] = (byte)tAire`. Dos líneas. Hazlo si te
cae de paso en H3; no abras una ronda por esto.

### R2 · Q2 · El residuo NO está en `SimStepper.cs`: es un artefacto de la auditoría en `LabAgua`

**Diagnóstico (leído en el código, no supuesto).** En `LabAgua`, `vol` es una variable LOCAL:
la evaporación y las tres infiltraciones restan de `vol` y suman al vecino, pero
`hum[i] = (byte)vol` solo se escribe al FINAL del método. Cuando `vol <= 0` se llama a
`LabTransformar(i, Empty, 0, 0)` y el auditor hace `LabBalanceU += 0 − _grid.humedad[i]`
con el valor VIEJO de `humedad[i]` (el volumen que la celda tenía antes de esta visita). El
auditor cuenta como DESTRUIDAS unidades que en realidad se TRANSFIRIERON al aire o al poroso.
Σhumedad no cambia, `LabBalanceU` baja, y el residuo Σ − Balance sale POSITIVO y pequeño (cada
evento aporta como mucho la tasa de esa visita, 1-5 u). Se acumula solo mientras hay celdas de
agua que se agotan por evaporación/infiltración, y se detiene cuando el régimen se estabiliza —
exactamente la curva que mediste (426 → 616 → 632).

**Parche (4 sitios, mismo archivo tuyo).** En `LabAgua`, antes de cada
`LabTransformar(i, MaterialId.Empty, 0, 0)` que sigue a un `if (vol <= 0)`, escribe
`hum[i] = 0;`. Con eso el auditor ve la celda ya vacía y no resta nada. Esperado: residuo 0
exacto en el escenario base. Si no llega a 0, mira estos dos con la misma lupa:
- **Abono de ceniza** (`case MaterialId.Ash`): transfieres `min(h, 255 − humedad[tgt])` al
  sustrato y después `LabTransformar(i, Empty, 0, 0)` audita −h entero. Escribe
  `hum[i] = (byte)(h − transferido)` antes de transformar (y cuenta `transferido` de verdad, no `h`).
- **Cocción** (`LabTransformar(i, Terracota, 0, 0)`): audita bien (destruye ≤ 30 u) pero es una
  FUGA FÍSICA: esa agua debería irse al aire. Antes de cocer, sécala con `LabSecarHacia` hacia
  los cuatro vecinos y solo cuece si `h` queda ≤ `TerracotaHumMax`. Así el horno de arcilla
  humedece el aire, que es una observación real.

**Dos fugas físicas conocidas y auditadas (no son residuo, son decisiones).** `LabNacerAgua`
sobre una celda de aire con vapor lo sobrescribe (manantial, exudación, goteo): el auditor lo
cuenta, pero el vapor se pierde. Es pequeño (≤ saturación de una celda) y hoy no importa; si
en H3 la condensación va justa, mueve ese vapor al primer vecino vacío de `j` (arriba primero)
antes del `SetCell`. Anótalo como límite hasta entonces.

**Sobre tocar `SimStepper.cs`.** No hace falta para esto. Sí te AUTORIZO un cambio de una línea
allí, porque lo vas a necesitar en H3: `LabCondensado` solo cuenta la condensación de la pasada
de campos (aire → superficie). La condensación del VAPOR VISIBLE (Steam → Water en
`ApplyPhase`, rama `condensesAt`, y en la expiración de `ProcessGas`) no se cuenta en ningún
sitio, así que «condensado = 0» NO significa que no condense. Añade un contador
`LabCondensadoGas` (long, en el partial) y en las dos ramas de `SimStepper.cs` una sola línea:
`if (LabActivo) LabCondensadoGas++;` con su comentario `(R132)`. Es la excepción documentada
de HANDOFF §2 para esta ronda y nada más.

### R3 · Q3 · Aceptada la grieta atascada de grava sobre arenisca

Bien visto y bien resuelto: un pozo abierto en mitad del lecho se traga el caudal entero y
mata el circuito; un tapón de grava que rezuma y que se colmata solo es MEJOR que la grieta
abierta que yo puse, porque es un mando real del jugador (destaparla a cincel drena la poza,
y ver que el hilo se cierra con el tiempo es una observación de colmatación gratis). Se queda.
Cuando llegue H7, prueba destaparla a mitad de sesión y anota qué pasa con la cámara profunda.

### R4 · Guía para H3 (no preguntada, pero es lo que viene)

1. Primero el parche de R2 (10 minutos): sin auditoría exacta no puedes distinguir un ajuste
   de un error.
2. Antes de mover saturación o ascenso, mira dónde MUERE el vapor visible: `vapor.vidaVapor`
   son 60 ticks = ~60 celdas de ascenso. Del hogar a la cámara alta hay ~70 celdas de chimenea.
   Sube `vidaVapor` a 150-200 y mira si el Steam llega como gas (con la vista de humedad y con
   `LabCondensadoGas`). Sospecho que hoy condensa a mitad de chimenea (el aire está a 20 °C y
   `condensesAt` es 60 °C) y vuelve a caer: eso es un REFLUJO, un fenómeno correcto y bonito
   (destilación en columna), pero no llega arriba.
3. Después la humedad del aire: `vapor.ascenso` 6 → 20 y `vapor.difusion` 4 → 2 para que el
   vapor lento suba más que se esparza; comprueba con la vista de humedad que el techo de la
   cámara alta se pone cian antes que las paredes.
4. Criterio de aceptación de H3.1 sin cambios: goteo en la cámara alta en < 3 min de mundo
   con agua vertida sobre el hogar. Deja un preset `ref_destilacion.json`.

### Copia de las preguntas originales de Opus (R131)

### Q1 · 2026-09-03 · H1 · Toqué `LabPresion` para que conserve el vapor del hueco

**La pregunta.** ¿Aceptas el cambio, o prefieres que la mudanza de presión siga aniquilando el
vapor del aire de destino y que el descuadre se documente como aproximación conocida?

**Qué encontré.** El balance del libro se desviaba +10,4 % a t=9 000. Midiendo (corridas con
`presion.activa` a 0 y a 1) resultó que 20 006 de las 23 407 unidades del descuadre las ponía
`LabPresion`: al mudar una celda de agua sobre la superficie más baja hacía
`SetCell(target, Water)` sobre una celda de aire y **borraba la `humedad` (vapor) que esa celda
tenía**. −4,5 u por mudanza, 5 213 mudanzas en 9 000 ticks. Efecto de juego: la presión iba
secando el aire de la cueva, justo lo contrario de lo que H3 necesita para condensar.

**Qué hice.** Convertirlo en un intercambio real: el vapor del hueco se muda a la celda que
deja el agua (`hum[src] = vaporDelHueco` tras el `SetCell(src, Empty)`). Dos líneas, sin tocar
la regla (la superficie más alta sigue mudándose a la más baja, mismo desnivel, mismo tope por
paso) y sin cambiar ningún número. Lo cuento como corrección del invariante 3 de tu §2
("toda transferencia resta donde suma"), no como cambio de regla — pero cae en un archivo que
tu §7 me pide escalar, así que aquí está. **Verificado**: mismas mudanzas (5 213 → 5 232, la
divergencia normal de un mundo caótico), mismos niveles, mismo régimen permanente.

**Mi propuesta.** Dejarlo. Si prefieres lo contrario, revertir es borrar tres líneas marcadas
`(R131)` en `LabPresion`.

---

### Q2 · 2026-09-03 · H1 · Queda un residuo de conservación en el barrido ordinario

**La pregunta.** ¿Quieres que investigue (y eventualmente parchee) `SimStepper.cs`, o lo
dejamos documentado como límite conocido del laboratorio?

**Qué encontré.** Con la auditoría nueva (`LabBalanceU`, decisión D14) el invariante se
comprueba exacto: `Σ humedad(t) − Σ humedad(0) == LabBalanceU`. Las pasadas del laboratorio
cuadran al bit. Queda un residuo de **632 u en 18 000 ticks (0,113 %)** que **deja de crecer**
(426 a t=3 000, 616 a t=9 000, 632 a t=18 000) y que no proviene de ninguna pasada del
laboratorio. Solo puede venir del barrido ordinario: alguna ruta de `SimStepper.cs` mueve o
transforma un líquido sin llevarse su `humedad` (`SwapCells` sí la lleva; `SetCell` la pone a
0 en todo lo que no sea agua; el sospechoso es alguna transición de fase o de reacción).

**Qué hice.** Nada en `SimStepper.cs`: tu §2 dice que los archivos grandes no se tocan más.
Lo dejé medido y anotado (CHECKPOINT §8.9). Está 44 veces por debajo del ±5 % que pide la
aceptación de H1, así que no bloquea ningún hito.

**Mi propuesta.** Dejarlo para cuando llegue H5 (banco headless): un escenario del banco puede
aislar la ruta exacta pintando un solo líquido y observando el descuadre por tipo de evento.
Si entonces resulta ser una línea, la escalo con el parche escrito.

---

### Q3 · 2026-09-03 · H1 · Aviso, no pregunta: la grieta x336-343 ya no es aire

Tu spec de H1 decía "la grieta x336-343 se mantiene". La mantuve en sitio y en tamaño, pero
**rellena de grava sobre una repisa de arenisca**, porque abierta era un segundo pozo de 8
celdas en mitad del lecho: se tragaba todo lo que rebosaba de la poza y `LabAguaSumida` seguía
en 0 (la cámara profunda mide 240×50: tardaría horas de mundo en llenarse y devolver el agua
al cauce). El ancho no ayudaba: cualquier agujero en el lecho se lleva el caudal entero.
Lo tomé como "cotas exactas", que tu §7 me deja decidir. Está en el CHECKPOINT como D13, con
las alternativas descartadas. Si la querías abierta como rasgo, dilo y busco otra manera de
que el sumidero reciba caudal.
