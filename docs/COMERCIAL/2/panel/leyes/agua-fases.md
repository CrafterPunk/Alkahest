# LEYES · LAS FASES DEL AGUA Y EL FRÍO COMO RECURSO

*(Segunda pasada comercial, panel de leyes, lente «agua/fases/frío». Fable 5.1, 2026-09-05. Propuestas sobre el sustrato medido en R130-R150; nada está autorizado ni arquitecturado. Lo que afirmo del código lo leí en `SimStepper.Laboratorio.cs`, `SimStepper.cs`, `LabParams.cs`, `Universe.Laboratorio.cs`, `CellGrid.cs` y `LabBench.cs`; lo que no medí lo marco «por lectura».)*

## 0. Lo que el sustrato ya hace con el frío, y lo que no

- **El cambio de fase es gratis e instantáneo.** `ApplyPhase` (SimStepper.cs:640) vuelve hielo el agua en el tick en que `temp <= freezesAt` (60 raw = 0 °C, `AplicarOverridesLaboratorio`) y agua el hielo en cuanto `temp >= meltsAt` (~62 raw). El único calor latente es `LabLatente` (`Latente` = 4 raw por celda) y solo lo pagan evaporación y condensación de la pasada de campos; el vapor visible que condensa (`LabCondensadoGas++`, dos sitios) no devuelve nada.
- **El hielo es un fósil del juego heredado.** `MaterialId.Ice`: StaticSolid, `caeSolido`, `cohesionCeldas 4`, `density = short.MaxValue`. `ProcessSolidoCohesion` solo lo deja caer a hueco vacío: un hielo sumergido **no sube** (no flota, solo no se hunde). No está en el `switch` de `LabCampos`, no es `LabEsSuperficieCondensable`, `LabLuzDesde` lo trata como opaco, ya no inyecta frío. Térmica `LabK` = KRoca (2), `LabC` = CAgua (4): aislante que nadie usa.
- **El único frío es un pin infinito.** `LabFrio` clava la celda a `FrioRaw` (30 raw = −60 °C) e inyecta `−FrioPotencia` (20) a los vecinos por visita. El tirón a `ambient[]` (±1 raw cada `TiroAmbienteTicks` = 32) es el termostato del mundo.
- **Por lectura, el alambique graniza.** `LabGotear` nace el agua con la temperatura de la superficie (`LabNacerAgua(j, _grid.temp[idx], 0)`); en el serpentín eso es 30 raw; el tick siguiente `ApplyPhase` la vuelve hielo, cae una celda por tick y se funde sobre el lecho en ~10 s. Los 900 «goteos» de R141 serían perdigones que riegan al fundirse. Se confirma contando `SimEventType.Freeze` en «alambique».
- **Convección solo en aire** (`LabFlujoTermico`, `aire = conv && m == Empty`). El agua caliente no sube.
- **`ambient[]` existe por celda, uniforme a 70.** Su docblock (CellGrid.cs:96-116; SimLevelBuilder.cs:82-137) dice para qué se guarda: el clima que **crea el jugador**. Cesar retiró el clima por zonas en el playtest 17 (regla 31); eso acota la cuarta propuesta.

Cuatro candidatos por apalancamiento. Se componen, pero cada uno se verifica solo.

---

## 1. `fase-con-reserva` — El cambio de fase cuesta: latente de fusión y ebullición, y el hielo flota

**Resumen.** Congelar, fundir y hervir consumen o liberan una reserva. El hielo pasa de sumidero infinito a **consumible con presupuesto**; la olla es un pin a 100 °C mientras le quede agua; el hielo sube en el agua.

**Regla.** En `AplicarOverridesLaboratorio`: `agua.freezesAt = short.MinValue`, `agua.boilsAt = short.MaxValue`, `hielo.meltsAt = short.MaxValue`; el laboratorio se hace dueño de las tres transiciones sin tocar `ApplyPhase`. Reserva: en el hielo, `aux` entero (StaticSolid no lo usa); en el agua, bits 1..7 de `aux` (misma convención que la reserva de combustión de líquidos, SimStepper.cs:779; `TryFlow` ya enmascara con `0xFE`). Parámetros: `LatenteFusion` (raw·celda, ~48), `LatenteVapor` (sustituye a `Latente`, ~16-32: un solo número para evaporar, condensar y hervir) y `HieloFlota`.
- `LabAgua`, paso 0: si `temp < 60`: `deficit = 60 − temp`; `acumulado += deficit`; `temp = 60` (el agua se clava a 0 °C mientras suelta latente, como el agua real); al llegar a `LatenteFusion` → `LabTransformar(i, Ice)`, `aux = LatenteFusion`. Simétrico sobre 110 para hervir; al completar `LatenteVapor`, el `Transform(i, Steam)` genérico (conserva `humedad` como hoy).
- `case MaterialId.Ice: LabHielo` en `LabCampos`: `q = min(temp − 62, aux)`; `aux −= q; temp −= q` (se come el calor y vuelve a 0 °C); `aux == 0` → agua a 62. Un bloque se funde de fuera adentro sin regla extra.
- Libro: `LabRawCongela`, `LabRawFusion`, `LabRawVapor`; los dos `LabCondensadoGas++` devuelven `LabLatente(idx, 255, +1)`. Identidad: en caja adiabática, `LabRawCongela == LabRawFusion` tras un ciclo.
- Flota: en `ProcessLiquid`, gateado `LabActivo && HieloFlota`, `mat[below] == Ice` → `Move`. La sábana en superficie no se mueve. `LabPresion` copia `temp/hum/carga` y no `aux`: una línea.

**Cruces.** *Evaporación* (`LabAgua` paso 1 exige `mat[up] == Empty`): un estanque con tapa de hielo **no evapora**: congelar es sellar humedad. *Presión* (superficie = agua con aire encima): la rama helada de un tubo en U es válvula cerrada; fundirla la abre: **grifo térmico**. *Fuego*: la olla clavada a 110 raw es termostato: junto a un depósito lleno nada sostiene 200 raw (vidrio) ni la ignición del carbón; el agua pasa de extintor a cortafuegos por temperatura (baño maría). *Alambique*: los perdigones llevan `LatenteFusion` de frío al lecho. *Síntoma de Cesar* («el agua me sigue saliendo congelada», playtests 16-17): con reserva, congelar exige frío **sostenido**, no un roce.

**Decisiones nuevas.** Grosor del tapón contra distancia al hogar = temporizador de SOLTAR (la inundación llega cuando el hielo cede). Congelar un estanque para guardar agua en una cámara seca. Poner la olla entre el fuego y lo que no debe arder. Fabricar frío finito (N·`LatenteFusion` raw contables). Dejar que granice o no sobre el huerto.

**Observabilidad.** Sin panel: el bloque se encoge de fuera adentro; la olla «que no hierve todavía» es un retraso legible; el hielo que sobrevivió es termómetro de mínima y su tamaño, calorímetro. Visor: la vista Temperatura existe (`VistaLaboratorio`); un tinte por `aux` en el hielo («cuánto frío le queda») es media tarde.

**Coste.** 1-1,5 semanas de Opus. Determinismo: sin dado (acumuladores enteros). Tick: hielo visitado 1/8; una comparación de byte por celda de agua. **Banco:** identidad congela/funde en caja adiabática (`TiroAmbienteTicks` al máximo); tick exacto de fusión de un tapón de 1 celda a 5 del hogar; `LabEvaporado` = 0 bajo tapa en 3000 ticks; temperatura tras una olla llena ≤ 110; conteo de `Freeze` en «alambique»; ms/tick en «mundo entero despierto» dentro de +3 %. **Tuning: 8. Apalancamiento: 9. Riesgo mayor:** los números de R141-R150 (goteos, anegadas, hervidero) cambian de significado; y sin la vista de reserva, «la olla no hierve» parece un fallo.

---

## 2. `conveccion-en-agua` — El agua caliente sube, la fría baja, con máximo de densidad a 4 °C

**Resumen.** Transporte de calor por materia dentro del agua: una comparación y un `Move`. Los estanques se calientan desde toda la superficie, se hielan por arriba y no se aclaran mientras estén calientes.

**Regla.** En `ProcessLiquid`, tras fallar gravedad y diagonales y antes de `TryFlow`, gateado `LabActivo && Water && ConveccionAgua != 0`: candidatos `i+W`, `i+W±1` en el orden del dado; si es agua y esta celda es **más ligera**: `Move`. Ligereza con la anomalía: `flot(t) = |t − AguaDensidadMaxRaw|` (62 raw); `i` sube si `flot(ti) > flot(tj) + ConveccionUmbral` (2). `ConveccionPct` (50) con `XorShift.FromCell(_tick, x, y, SalConveccion)` contra el ping-pong de tablero. `reposo = 0` en las dos celdas. Sin `LabErosion`: convección no lava.

**Cruces.** *Térmica*: hoy el estanque sobre el hogar hierve por el fondo con la superficie fría; con esto el fondo sube antes de 110 y, con el candidato 1, el vapor **nace en la superficie**. *Evaporación* (lee `temp` de la superficie): un estanque calentado humidifica toda la cámara. *Decantación* (`reposo < ReposoMovil` → 25 %; depósito exige `DepositoReposo` 24): un estanque calentado **no se aclara nunca**; la claridad es quietud y la quietud es equilibrio térmico: «se aclara cuando el fuego se apaga», instrumento sin autoría. *Hielo* (cand. 1): el agua más fría flota, el estanque se hiela por arriba, la tapa aísla y abajo queda agua a ~4 °C: bodega que ni evapora ni se hiela entera. Con `AguaDensidadMaxRaw = 0` se hiela por abajo: el banco muestra las dos. *Chunks*: un estanque con gradiente no duerme hasta ser isotermo.

**Decisiones.** Calentar desde abajo o de lado (celda de circulación por las diagonales). Apagar para cosechar sedimento. Dónde va el núcleo frío respecto al estanque. Un depósito como radiador de techo.

**Observabilidad.** Sin panel: turbidez que no baja, vapor de toda la superficie. Visor: la vista **Reposo** ya muestra el agua inquieta; Temperatura, las plumas.

**Coste.** 0,5-1 semana (una rama, tres parámetros, una sal > 643, escenario «estanque calentado de lado», rebase de hervidero). Determinismo: `FromCell`. Tick: tres lecturas por celda de agua; el coste real es el estanque despierto (medido en «hervidero», 10 000 celdas). **Banco:** hogar bajo el tercio izquierdo de un tanque 100×30: la superficie del extremo frío supera al fondo de ese extremo a t = 3000 (contra `ConveccionAgua = 0`); primer hielo en la fila superior con anomalía y en la inferior sin ella; carga media de un estanque turbio calentado > 80 % de la inicial contra < 30 % sin calor; Σ`temp` con y sin convección difiere < 0,5 %. **Tuning: 8. Apalancamiento: 8. Riesgo mayor:** artefactos de tablero o estanques eternamente despiertos; ambos se ven en el banco antes que en Play.

---

## 3. `escarcha-y-suelo-helado` — El hielo y el frío entran en la pasada de campos

**Resumen.** El hielo condensa (escarcha que crece y se autolimita), el poroso helado no bebe ni infiltra, la raíz helada no bebe, la luz atraviesa el hielo, el cincel pica hielo, el núcleo frío tiene grado. El mismo bloque es alambique sobre 0 °C y escarchador debajo.

**Regla.** (a) `LabEsSuperficieCondensable` += Ice. La regla local de R135 (el vapor va al vecino condensable **más frío**) hace el resto: el hielo a ≤62 gana a la pared a 70 y la escarcha crece; al llegar `humedad` a 255 sobre hielo o sobre un núcleo a ≤60 raw, en vez de `LabGotear` nace **hielo** en el vecino vacío (`LabNacerHielo`: `aux = LatenteFusion`, `temp` de la superficie), conservando `LabBalanceU`. La celda nueva está más lejos del núcleo y más caliente: el frente se detiene solo. Contador `LabHieloNacido`. (b) Guardas `temp[i] > 60` en `LabPoroso` pasos 2-3, en `LabInfiltrarHacia` hacia poroso helado y en `LabPlanta` paso 2 (la raíz bebe). La planta transpira igual y muere de sed en ~10 s: **la helada mata** sin regla de muerte nueva. (c) `LabLuzDesde`/`LabLuzDecay` tratan Ice como agua (`LuzDecayAgua`): ventana de hielo. (d) `Tallable` += Ice, `ProductoDeTalla(Ice) = Water`. (e) `aux` del NucleoFrio = su pin (por defecto `FrioRaw`), leído por `LabFrio`: a 64 raw gotea líquido, a 30 graniza y escarcha.

**Cruces.** *Alambique*: hoy (por lectura) graniza; con (a) cría carámbanos y sábana; con (e) el investigador elige la máquina con un número. *Plantas*: el serpentín sobre el huerto ya lo ahoga (R135) y lo oscurece (R148); ahora lo **hiela**: tercer cruce que nadie escribió. *Luz + evaporación* (cand. 1): un lecho sellado bajo ventana de hielo no pierde agua y recibe luz: **invernadero frío**, construcción nueva por una línea. *Fuego*: la sábana es pared gratuita que cede al calor: sello autorreparable mientras el núcleo esté. *Cincel*: mantenimiento del serpentín producido por las leyes (ruina amable).

**Decisiones.** Serpentín sobre o bajo 0 °C. Picar la escarcha o dejar que amuralle. Proteger el lecho (distancia, placa de terracota, un fuego). Construir con hielo sabiendo que el fuego lo abre.

**Observabilidad.** Sin panel: la escarcha crece a la vista (asignar `BordeMorfologico.Escarcha` al hielo: el estilo existe); la planta se marchita sobre suelo «húmedo» → `LabMateriales.Estado` gana el prefijo «helado» con `temp`. Visor: isolínea de 0 °C sobre Temperatura, «la línea de la escarcha».

**Coste.** 1 semana. Determinismo: sin dado nuevo (orden fijo de `LabVecinoVacio`). Tick: hielo 1/8; guardas de un byte. **Banco:** serpentín a 30 raw en «alambique» → goteos líquidos = 0 y `LabHieloNacido` > 0; a 64 raw → goteos ≈ 900 ± 10 % y hielo = 0; meseta de escarcha (Δ < 2 % en los últimos 3000 de 9000 ticks); planta sobre sedimento a 55 raw con humedad 200 muere en ≤ 600 ticks y su gemela a 70 vive; `LabInfiltrado` = 0 hacia poroso helado; luz bajo dos celdas de hielo ≥ luz arriba − 2·`LuzDecayAgua`. **Tuning: 7. Apalancamiento: 7. Riesgo mayor:** que la escarcha no se autolimite en 72 000 ticks y entierre la cámara alta (el arco largo lo dice).

---

## 4. `clima-ganado` — La roca recuerda: `ambient[]` escrito por la historia, no por el plano

**Resumen.** El termostato del mundo deja de apuntar a una constante y apunta a lo que la roca madre vivió. Una sala calentada un rato sigue tibia después; una nevera helada largo tiempo conserva el hielo más. Es la forma exacta que el docblock de `ambient` reservó: clima ganado, no heredado.

**Regla.** Pasada `LabClima` en `LabPasadas`, cada `ClimaCadaTicks` (256), estriada 1/8. Para Stone/PisoEstructural: `ambient[i] += sign(temp[i] − ambient[i])` (memoria). Para todas las celdas: `ambient[i] += sign(media4(ambient) − ambient[i])` (el recuerdo se difunde, glacial). Las dos filas y columnas del borde se quedan en `AmbientRaw`: **la roca profunda es el sumidero infinito** y garantiza el olvido. `LabDifusionTermica` no cambia: ya lee `ambient[i]`. Contador `LabRawAmbiente` (hoy el tirón es energía sin nombre). `ambient` entra al hash del banco.

**Cruces.** *Fuego*: una sala sellada con hogar 18 000 ticks queda con paredes a ~90 raw; al apagar, el aire vuelve a 80 y no a 70: **horno curado**, vidrio más barato la segunda hornada; secado y evaporación (∝ temp) suben en salas vividas. *Frío* (cand. 1 y 3): la bodega aprende el frío; la nevera mejora con el uso. *Condensación*: las paredes tibias dejan de ser el vecino más frío; el rocío se muda a las que no vivieron fuego: el patrón de rocío cuenta la historia de la sala. *SOLTAR*: el clima ganado es lo que el sitio recuerda al volver.

**Decisiones.** Dónde mantener el fuego a largo plazo; curar una cámara antes de la hornada; qué sala sacrificar al frío; leer una ruina por su clima.

**Observabilidad.** Sin panel: solo por consecuencia. Visor: par «Temperatura» (lo que es) y «Clima» (lo que recuerda): el ejemplo más limpio de dos resoluciones del mismo campo.

**Coste.** 0,5-1 semana. Determinismo: sin dado. Tick: 1/8 de la grilla cada 256 ticks, < 0,05 ms amortizado. **Banco:** sin fuentes, `ambient` uniforme y hash de «laboratorio base» sin cambio; memoria (paredes ≥ 90 tras 18 000 ticks; aire ≥ 80 a 3000 ticks de apagar, contra 70 en sala nueva); olvido (30 000 ticks después, `ambient` a ±2 de 70); `LabRawAmbiente` por escenario. **Tuning: 7. Apalancamiento: 7. Riesgo mayor:** reabre la regla 31 y hay que presentarlo a Cesar como lo que su nota pidió; un campo lento e invisible repite la confusión de «algo se congeló solo» si el visor Clima no llega con la ley.

---

## Cómo se componen

1 solo compra el fusible, la bodega sellada y el baño maría. 1+2: el estanque que se hiela por arriba y guarda agua líquida abajo. 1+3: el invernadero frío y el serpentín que amuralla. 1+3+4: la nevera que mejora con el uso. Ninguno exige contenido; todos exigen un escenario de banco y una rebase de hashes.

## Lo que descarté y por qué

- **Ambiente por profundidad y «noche» como número del plano.** Era el cuarto candidato hasta leer SimLevelBuilder.cs:82-137: el clima por zonas existió, funcionó y Cesar lo quitó (las zonas fijas deciden por el jugador dónde construir; regla 31). Un gradiente por fila o un nivel «de noche» es clima heredado del plano; lo sustituye el clima ganado. Un reloj día/noche es además lo que la función objetivo penaliza.
- **Ebullición que empuja (bomba de vapor, géiser).** Exige presión de gas, no local; `LabPresion` es solo de agua. Tres semanas para un dominio nuevo en vez de cruzar los existentes. Las burbujas ya suben por «el líquido cae en el gas».
- **Gelifracción (el hielo rompe arcilla y arenisca en grava).** Real como erosión por frío, pero pide `Fractura*` y cuerpos sólidos; H6 está congelado con su hipótesis escrita.
- **Nieve o granizo como material nuevo.** El hielo ya cae y ya graniza; un material más es ítem de biblioteca sin decisión nueva.
- **Densidad del agua como campo continuo.** La anomalía de 4 °C en una comparación da el 90 % con un entero.
- **El frasco como portador de frío; el cuerpo que resbala o tirita.** Son juego y cuerpo, no sustrato. Nota para esas lentes: `temp` viaja con la sustancia en `SwapCells`; si `Flask` conserva temperatura, el agua fría ya es frío transportable.
- **Sobreenfriamiento y nucleación.** Verdad física sin consecuencia a un grano por celda: el mismo criterio con que R130 descartó Bernoulli.
