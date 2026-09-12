# LENTE · EL AIRE COMO MEDIO QUE SE MUEVE

*(Panel de leyes, segunda pasada comercial. Cuatro candidatos ordenados por juego nuevo por unidad de
complejidad. Todo lo afirmado del motor está leído en `Assets/Alkahest/Sim/SimStepper.Laboratorio.cs`,
`SimStepper.cs`, `LabParams.cs`, `CellGrid.cs` y `LabBench.cs`; se citan líneas para Opus.)*

## 0. Lo que el aire hace hoy (hechos)

- **El aire no tiene masa.** `Empty` es un hueco con `temp` y `humedad` (vapor). Nada se conserva en
  él, nada lo empuja, nada lo gasta.
- **`Conveccion` no mueve aire; sesga la conducción.** `LabFlujoTermico` (l. 1350-1358): si el vecino de
  abajo está más caliente, `k *= 2`; si el de arriba, `k /= 2`. Ninguna parcela sube. `KAire = 4`,
  `CAire = 1`. **`TiroAmbienteTicks = 32`** (l. 1311-1343): 1 raw hacia `ambient[]` cada 32 ticks, un
  sumidero de calor a ninguna parte.
- **El «viento» ya existe y es ruido.** `ProcessGas` (`SimStepper.cs` l. 1516) lee
  `rumboLeft = XorShift.FromCell(_tick >> 4, x >> 3, y >> 3, SalGasRumbo)`: hash por bloque de 8×8 y
  ventana de 16 ticks que gobierna la ondulación (30 %) y el escape bajo techo. Se lee como viento y no
  tiene causa. El enchufe está; falta la corriente.
- **El vapor sí es un campo que se mueve.** `LabAire` (l. 328-389): difusión conservativa con los cuatro
  vecinos de aire/gas (`LabIntercambioVapor`, `t = diff/(2·D)` con suelo ±1), `VaporAscenso`,
  condensación al vecino más frío. Es el esquema que una masa de aire necesita: escrito, estable,
  determinista.
- **El fuego respira por geometría.** `LabRespira` (l. 1052-1062): un vecino `Empty`/`Fire` y como
  mucho uno de `Smoke`. El comentario de l. 1045 dice «no hace falta un campo de oxígeno». El informe
  final mide la consecuencia: no hay tiro; humo 4 % → 40 % idéntico al bit; la boca no regula.
- **El secado por viento está latente.** `LabSecarHacia` (l. 734-751): tasa ∝ `deficit/sat` del aire
  vecino. Si algo RENUEVA ese aire, seca más rápido solo.
- **`ambient[]` tiene zonas** (`SimLevelBuilder.Laboratorio.cs` l. 168-176: 64, 66, 70): la referencia
  que una flotación necesita. **El visor es un caso de `switch`** (`LabVistaColor`,
  `SimRenderer.Laboratorio.cs` l. 170-225).

Diagnóstico: **el tiro no emerge porque falta la masa, no porque falte una regla de chimenea.** Con
humo-como-aire-gastado, una boca solo da contraflujo por el mismo agujero; una segunda boca no puede
importar, y «la segunda boca importa» es todo el juego del tiro.

---

## Candidato 1 · El aire pesa, se gasta y fluye (`aire-que-fluye`)

**Resumen.** Un byte `aire` por celda (masa, nominal 128) y un byte `viento` (flujo neto). El aire se
difunde por los huecos conectados, flota donde está más caliente que su ambiente, se consume al arder y
se repone por la boca del cielo. El gas lee el flujo en vez del hash. El tiro EMERGE de conservación +
flotación; la boca regula porque limita el caudal.

**Mecanismo.**
- `CellGrid`: `aire` y `viento` (`byte[]`, 221 KB cada uno). Nacen a 128 en `Empty`; `SetCell` no los
  toca; `SwapCells` los intercambia como a `humedad` (l. 264-279): lo que se mueve desplaza aire sin
  crearlo ni destruirlo.
- `LabAire`, por visita (8 ticks), antes del vapor:
  1. **Difusión conservativa** con vecinos `Empty`/gas: copia de `LabIntercambioVapor` sobre `aire`,
     divisor `AireDifusion` (4). Acumula el flujo neto `fx, fy`.
  2. **Flotación por exceso sobre ambiente**: `exceso = temp[i] − ambient[i]`; si `exceso > 0` y arriba
     hay aire/gas, sube `t = exceso·AireFlota/64`, **solo mientras** `aire[up] − aire[i] <
     exceso·AireEquilibrio/16`. Ese tope es el balance hidrostático: una caja caliente sellada se
     estratifica y se queda quieta; una columna uniformemente caliente bombea en toda su altura, que es
     la fórmula del tiro (Δp ∝ Δρ·g·h) sin escribirla (comparar con la celda de arriba daría cero).
  3. `viento[i] = pack(clamp(fx,−7,7), clamp(fy,−7,7))`, dos nibbles, media móvil `(3·viejo+nuevo)/4`.
  4. **Fuente**: las celdas de la boca del cielo (`LuzCieloX0..X1`, fila superior) se fijan a 128 por
     visita; contador `LabAireInyectado`.
- **Consumo**: en `ProcessCombustion` (`SimStepper.cs` l. 855-870), tras decidir `sordina`, restar
  `AireConsumo` (2) a los vecinos de aire; `ProcessBrasa` la mitad. `LabRespira` cuenta como aire solo
  vecinos con `aire[j] >= AireMinRespira` (32). `ProcessFire`: llama con `aire[idx] < AireMinRespira`
  muere a humo: la llama inmortal sobre combustible deja de serlo en un cuarto cerrado, sin regla nueva.
  Contador `LabAireConsumido`; invariante exacto `Σaire = Σ0 + inyectado − consumido`.
- **`ProcessGas`** (l. 1516): `rumboLeft = vx < 0` si `|vx| >= 1`; ondulación `min(90, 30 + 10·|vx|)`;
  con viento cero, el hash de siempre. Bajo techo el viento manda sobre «el lado caliente» (l. 1573).
- `HashAire`, `HashViento` en `LabBench.Resultado`.

**Cruces.** Fuego: la sordina pasa de geométrica a dependiente del caudal; la carbonera de boca 1 sigue
siéndolo y la de boca 8 deja de serlo. Humo: viaja con el flujo (el cruce R148, humo que oscurece el
huerto, pasa a depender de dónde desemboca la chimenea). Vapor: sin tocar `LabSecarHacia`, el aire
renovado seca a sotavento. Plantas: transpiran mejor al viento. Térmica y luz: sin cambios.

**Decisiones nuevas.** Dónde va la segunda boca; alto y ancho de la chimenea; sellar para carbonizar o
abrir para calentar; de qué lado entra el aire (el humo va hacia allá); apagar cerrando; bancar brasas
cerrando a medias; qué cuarto se asfixia primero si arde el taller.

**Observabilidad.** Sin panel: el humo ES el visor (una brizna de fibra encendida enseña la corriente,
como el incienso en una casa); la llama se inclina si la lengua de `ProcessCombustion` nace en la
diagonal a sotavento con `|vx| >= 2` (una línea, l. 878). Con visor: `VistaLaboratorio.Corrientes`
(tono = dirección, brillo = magnitud) y `Presion` (`aire` gris; rojo sobre 128, azul debajo).

**Coste**: 1,5 semanas de Opus con dos escenarios nuevos y rebase de hashes. **Determinismo**: intacto
(enteros, orden fijo, escrituras en sitio como el vapor); los nueve hashes cambian una vez, declarado.
**Coste por tick**: +0,2-0,4 ms estimados (duplica `LabAire`; el aire quieto sale en el `diff <= 1`);
si en «mundo entero despierto» pasa de 0,5 ms, visitar el aire cada 16 ticks.

**Verificación headless.** Escenario «chimenea con boca N» (sala 20×14, pila de fibra sobre hogar,
chimenea 2×40, boca a ras de suelo de 0/1/3/8): `LabUnidadesRespiradas/LabCombustibleQuemado` monótona
creciente con N; N=0 → ≤ 20 % y `LabCarbonizado > 0`; N=8 → ≥ 80 %; `Σ vy` en la sección de la chimenea
> 0 con N ≥ 1 y ≈ 0 tapada; humo fuera de la sala > humo dentro con N ≥ 3. «hervidero»: `LabCondensado`
no baja. Auditoría de `aire` exacta en los nueve escenarios. **Tuning humano: 7/10** (cinco números; la
curva boca→respiración la dibuja el banco; la única elección es dónde cae el codo). **Apalancamiento: 9.**

**Riesgo mayor.** Circulación neta débil: la difusión devuelve masa por la misma chimenea que la
flotación bombea. Se sabe en dos días con el escenario; plan B: subir `AireFlota` o bajar `AireDifusion`
en aire caliente; si aun así `Σ vy ≈ 0`, el candidato muere con evidencia y sin haber tocado nada más.

---

## Candidato 2 · El flujo lleva calor y vapor (`adveccion-calor-vapor`)

**Resumen.** Cuando `t` unidades de aire pasan de `i` a `j`, se llevan su parte de temperatura y de
vapor. Diez líneas sobre el candidato 1 que convierten el calor en un recurso que se enruta:
hipocausto, secadero, condensador a sotavento.

**Mecanismo.** En cada transferencia de `LabAire`: `hum[j] += hum[i]·t/aire_i` y se resta de `i`
(conservativo, mismo libro `LabBalanceU`); `temp[j] += (temp[i] − temp[j])·t/128` con el redondeo
simétrico y el recorte a `[dMin, dMax]` de `LabFlujoTermico` (l. 1330-1336): combinación convexa,
contracción garantizada. `Conveccion` puede bajar a 0: la convección real la hace la masa.

**Cruces.** Térmica: el calor de la chimenea llega a la cámara por la que pasa (en el arco largo, la
cámara alta sobre el huerto se entibia además de oscurecerse). Agua: el vapor condensa donde el flujo
cruza frío; el serpentín del alambique rinde más en la salida de la corriente que en cualquier techo.
Suelo: una corriente tibia seca arcilla, fibra y carbón mojado. Plantas: flujo tibio y húmedo =
invernadero; seco = marchitez. `TiroAmbienteTicks` compite ahora con un aporte real.

**Decisiones nuevas.** Calentar un cuarto sin fuego enrutando el humo bajo su suelo; frío aguas abajo del
calor; secadero con una llama y dos bocas; chimenea recta (tiro fuerte, calor perdido) o serpentina
(calor aprovechado, humo que se queda).

**Observabilidad.** Sin panel: rocío a sotavento del fuego; la terracota (termómetro de máxima que ya
existe) marca hasta dónde llegó la corriente; la fibra se seca del lado de la boca. Con visor: la vista
Temperatura ya enseña la lengua de calor.

**Coste**: 0,3 semanas tras el 1. **Determinismo**: intacto. **Tick**: +5 % de `LabAire`.
**Verificación**: «chimenea con boca 3» con cámara lateral atravesada por el conducto: `temp` media
final ≥ ambient + 8 raw con conducto, = ambient sin él; «hervidero»: `LabGoteos` ≥ +30 % frente al
candidato 1 solo. **Tuning: 8/10.** **Apalancamiento: 8** (condicionado al 1). **Riesgo**: doble
conteo con `Conveccion = 1`; se mide en «laboratorio base» y se pone a 0 si hace falta.

---

## Candidato 3 · Lo que vuela: brasas, ceniza, semillas, fibra (`polvos-al-viento`)

**Resumen.** Los polvos ligeros que caen leen `viento` y se desvían a sotavento. El fuego se propaga
por chispas, la ceniza se reparte, las semillas se dispersan, la fibra vuela. Cierra parte del negativo
«nada produce sin volver a tocarlo».

**Mecanismo.** `ProcessPowder` (`SimStepper.cs` l. 1061-1130): con `Empty`/gas debajo, antes del `Move`
vertical, si `|vx| >= VientoArrastre·def.density/64` (ceniza 120; semilla, fibra y brasa más ligeras),
moverse a la diagonal inferior a sotavento con probabilidad `|vx|·12 %` (sal nueva). Solo mientras
cae; nada se levanta del suelo salvo ceniza con `Empty` encima y `|vx| >= 6` (`VientoLevanta`,
desactivable). `ProcessBrasa` no cambia: la brasa que aterriza sobre fibra ya la enciende (l. 1030-1036).

**Cruces.** Fuego: la chimenea escupe brasas que el viento deja sobre un tejado de fibra; el incendio
avanza a favor del viento. Suelo: la ceniza abona a sotavento (`AbonoCeniza`, l. 668) y enturbia el agua
donde cae. Plantas: si la lente de organismos da semillas a la planta muerta, la dispersión es gratis y
direccional; sin eso, se siembra a voleo desde altura y el viento elige. Agua: la fibra que cae en agua
no prende (`LabCombustibleMojado`).

**Decisiones nuevas.** Techar la carbonera con roca y no con fibra; sembrar contra el viento;
cortafuegos de arena a sotavento; boca del taller donde la ceniza no caiga en la poza.

**Observabilidad.** Sin panel: la ceniza que cae es la veleta; una pizca de fibra desde la mano dibuja
la corriente; la brasa que vuela es el clip. Con visor: «Corrientes».

**Coste**: 0,5 semanas tras el 1. **Determinismo**: intacto (sal grep-verificada como las 601-643).
**Tick**: nulo fuera de polvos cayendo en viento. **Verificación**: «chimenea con boca 3» con tejado de
fibra a 6 celdas a sotavento y otro a barlovento: el de sotavento prende antes de 9 000 ticks en ≥ 3 de
5 semillas, el de barlovento nunca; «diluvio» y «tolva» con hash intacto (la regla no dispara con
`viento = 0`). **Tuning: 7/10.** **Apalancamiento: 7.** **Riesgo**: caos de brasas que impide todo
fuego a cielo abierto; se acota con `VientoArrastre` alto para brasa y exigiendo `|vx| >= 4` en su celda
de origen (corrientes de chimenea, no brisas).

---

## Candidato 4 · Aire atrapado: campanas, sifones cebados y la bolsa que empuja (`aire-atrapado`)

**Resumen.** El agua solo entra en una celda de aire si ese aire puede irse a otra parte. Una bolsa
sellada se comprime (hasta 255) y luego resiste: campana de buceo, sifón que hay que cebar, esclusa,
frasco invertido. Con expansión térmica (tres líneas) la bolsa caliente empuja agua: la bomba de Herón
sin escribir una bomba.

**Mecanismo.** En `ProcessLiquid` (l. 1138) y en la mudanza de `LabPresion` (l. 1129-1140), antes de
mover agua a un `Empty` `j`: buscar entre los vecinos aire/gas de `j` (no el de origen) el de menor
`aire`; si cabe `aire[j]` entero, transferirlo y mover; si no, **no mover**. Hoy el aire desplazado
baja como burbuja por el cuerpo de agua y sube por otro lado, y por eso toda bolsa se llena; la burbuja
queda solo para `aire[j] < AireBurbuja` (16). Extensión: presión efectiva
`P = aire·(temp+120)/(ambient+120)` en la difusión del candidato 1 (gas ideal en enteros); si
`P > AireEmpuje`, la celda de agua vecina se muda a un `Empty` ajeno a la bolsa: el aire caliente
expulsa agua.

**Cruces.** Agua (5/5): el tubo en U con rama cerrada no iguala hasta purgar; el sifón necesita cebo.
Fuego + agua: fuego bajo bolsa con agua = bomba; el hogar doméstico (170 raw) basta. Frío: la bolsa que
se enfría succiona. Cuerpo del jugador (otra lente): una campana es donde se respira bajo el agua, y su
aire se gasta si hay llama dentro.

**Decisiones nuevas.** Dónde dejar el respiradero de un depósito; purgar o no; subir agua con fuego en
vez de con el frasco; sellar una cámara para que no se inunde; bucear con campana.

**Observabilidad.** Sin panel: el nivel se detiene a media bolsa y el chorro sale al calentar. Con
visor: «Presion» pinta la bolsa en rojo.

**Coste**: 1 semana tras el 1. **Determinismo**: intacto. **Tick**: un vecino más por movimiento de agua
hacia aire; lo mide «diluvio turbio». **Verificación**: escenario «campana»: vaso invertido de roca
10×10 bajo 30 celdas de agua; nivel interior final entre 40 y 60 % y estable de 3 000 a 9 000 ticks; con
hogar debajo, ≥ 20 celdas expulsadas; con respiradero se llena entero. «alambique» y «arco largo» con
`LabGoteos` sin cambio. **Tuning: 7/10.** **Apalancamiento: 7.** **Riesgo**: agua atascada en cuevas
que el jugador no sabía selladas; se mitiga con `AireBurbuja` y con resistir solo desde 6 celdas
(`PresionMinCeldas` ya tiene esa lógica).

---

## Orden y dependencias

1 es la base y vale solo. 2 y 3 son extensiones de días que multiplican su juego. 4 es independiente
en lógica pero usa el mismo campo. Los cuatro: ~3,3 semanas de Opus, verificables sin personas. Rebase
de hashes una sola vez, con el candidato 1, declarado como cambio de física.

## Lo que descarté y por qué

- **Viento exterior constante.** Un gradiente de `aire` en la boca del cielo para tener barlovento y
  sotavento. Dos días, pero es clima: condición global sin causa dentro del mundo, y con una boca de 7
  celdas afecta a un rincón. Si el 1 vive, se reevalúa como constante única; nunca como evento.
- **Humo como aire gastado sin masa** (subir `combustHumoPct`, `VidaHumo`). Medido: idéntico al bit.
  Aunque se afinara, una boca solo da contraflujo y la segunda no puede importar. Tuning que compra nada.
- **Tiro escrito a mano** (una «chimenea» que succiona). Es lo que la función objetivo prohíbe:
  geometría de autor que luego se balancea contra cada construcción. El tiro emerge o no existe.
- **Campo de oxígeno aparte.** Redundante: la masa que se difunde es la que se gasta.
- **Humo como campo continuo.** Sustituye algo que funciona y está medido (el humo apaga la luz, R148)
  por un refactor de `ProcessGas`, renderer y `LabRespira`. El 1 consigue el 80 % moviendo las celdas
  que ya hay.
- **Velocidad vectorial completa** (malla MAC, proyección de presión). Flotantes o enteros anchos,
  varios pases por tick, riesgo real de determinismo cruzado. El flujo neto por celda es toda la
  velocidad que el gas necesita.
- **Aire a resolución de chunk.** Un muro de una celda dentro del bloque filtraría aire entre dos
  cuartos; el jugador construye con muros de una celda y lo sentiría como un fallo.
- **Convección forzada explícita** (multiplicar `k` y `Secado` por `|viento|`). Emerge del 2; escribirlo
  contaría dos veces lo mismo. **Veleta como objeto.** Ya la hay: una pizca de fibra o el humo.
