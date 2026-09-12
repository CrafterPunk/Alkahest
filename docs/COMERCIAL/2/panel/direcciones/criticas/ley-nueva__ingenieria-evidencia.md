# CRÍTICA · «TIRO / La segunda boca» (ley-nueva) · lente: INGENIERÍA Y EVIDENCIA

*(Director técnico, segunda pasada, 2026-09-12. Único tema: qué pide del sustrato, qué evidencia del
laboratorio la sostiene o la contradice, y cuál es la prueba más barata capaz de matarla. Leído
entero: `ley-nueva.md`, `01_LEYES.md`, la lente `aire-viento`, las refutaciones de `aire-que-fluye`
(×2), `aire-atrapado` (ingeniería) y `adveccion-calor-vapor` (ingeniería), las dos críticas hermanas
de esta dirección (no repito lo que ya dijeron; donde coincido lo cito), CHECKPOINT §6g (R135, HF1-HF4)
e INFORME_FINAL :123-124 y :278 («el tiro no existe»). Código: `SimStepper.Laboratorio.cs` :134-150
(`LabPasadas`), :201-236 (`LabCampos`: solo `Empty` despacha `LabAire`), :328-404 (`LabAire`,
`LabIntercambioVapor`), :1052-1066 (`LabRespira`, `LabPasoSordina`), :1080-1176 (`LabPresion`, la
mudanza a mano :1141-1160), :1306-1360 (`LabDifusionTermica`, `LabFlujoTermico`); `SimStepper.cs`
:707-730 (`Transform`: escribe `mat` y `aux`, nada más), :829-935 (`ProcessCombustion`, sordina),
:1443-1605 (`ProcessGas`: sube siempre que hay `Empty` encima; `rumboLeft` por hash de bloque),
:1644-1700 (`ProcessFire`, `life = 30`); `CellGrid.cs` :56-57 (`W = 768`), :264-279 (`SwapCells`);
`LabBench.cs` :76-87 (nueve escenarios), :116-155 (horno, carbonera, tolva), :265-300 (`Correr`
pone `LuzCieloX0/X1 = −1`); `LabParams.cs` :111-131.)*

## 0. Veredicto en una línea

**Segunda ronda.** Es la dirección con el mejor perfil de producción del panel (una ley, cinco verbos
existentes, todo con escenario y gate, cero contenido artesanal), pero **el experimento del que
cuelga su título está mal especificado en tres sitios** y, tal como está presupuestado, no puede
darle la razón aunque el tiro existiera. Además su promesa de conservación («Σaire = Σ0 + inyectado −
consumido exacto») se contradice con su propia «relajación lenta hacia 128», que crea masa de la
nada, y el banco donde dice derivar el caudal de la boca no tiene boca (`Correr` la apaga). Nada de
esto es caro de arreglar; todo es barato de no arreglar y medir mal. Corregido el spike, **dos
semanas** deciden si esto es un juego (B vive: finalista) o un sustrato (B muere: A + bolsa pasan a
«las leyes juzgan», como la dirección prevé).

| campo | valor |
|---|---|
| **veredicto** | segunda ronda, con puerta de dos semanas |
| **riesgo mayor (esta lente)** | que el spike de B mida un artefacto en cualquiera de los dos sentidos: el barrido en sitio de `LabCampos` sesga la vertical (visita columnas `x ≡ tick mod 8` de abajo arriba), el humo que sube por `SwapCells` empuja `Empty` hacia abajo por la chimenea, y la columna de aire no puede estar caliente (`LabFlujoTermico` decae el exceso ×0,59 por celda: a diez celdas es cero); con eso `Σvy` en la sección puede salir negativo con tiro real o positivo sin él |
| **iteración humana oculta** | la que las dos críticas hermanas cuentan (15-25 días en tres meses); desde esta lente, lo incomprimible es la mirada al humo a escala (2-3 sesiones) y la decisión de qué identidad del canon cede si el barrido nocturno no encuentra punto |
| **se automatiza en su lugar** | medida de la columna térmica en el banco actual (sin código nuevo); spike con doble búfer, advección de calor y caudal neto por boca; gemelos ±1 celda como suelo de ruido; barrido del canon; Σaire con término de relajación (o la planta que repone); boca del horno 1..4 contra 18/18; cielo por geometría en el banco |
| **prueba que la mata** | (0) medio día sin física nueva: perfil de `temp` en la chimenea y celdas de humo por tick en la sala de «La vela» con el código actual; (1) spike de B escrito para poder acertar, métrica = aire que entra por la boca baja, abierta frente a tapada, contra el ruido de gemelos; mata si la diferencia < 2× el ruido en todo N |
| **semanas hasta evidencia para matarla** | 2 |
| **semanas hasta prototipo feo** | 7 |

## 1. Qué necesita del sustrato, y qué dice el código

| pieza | ¿en el catálogo? | ¿acotada y verificable en banco? | dependencia secuencial |
|---|---|---|---|
| **A** byte `aire`, difusión, fuente, consumo, `LabRespira`/`ProcessFire` por umbral | sí (`aire-que-fluye`, viable con ajuste ×2) | sí, con cuatro correcciones que la dirección no lista (§1.1) | ninguna; es la raíz de todo lo demás |
| **B** tiro por presión, `viento`, `ProcessGas` lo lee | sí, condicionado a dos días de banco | sí, pero el escenario y la métrica están mal especificados (§2) | A |
| bolsa en `LabPresion` (altura efectiva por presión de la bolsa) | sí (`aire-atrapado`, reescrito) | sí; sobrevive si B muere (su refutación) | el byte de A (40 % del candidato 1), no B |
| vapor advectado (seis líneas) | parcial (`adveccion`), plegado en A/B | sí, «dos cámaras» | las transferencias de A |
| re-balance de horno, carbonera y tolva | — | sí: barrido de boca 1..4 contra 18/18, 25 %, 466 s | A definitiva |
| render de humo/llama, lengua a sotavento, vistas Presión y Corrientes | — | la lengua es una línea; las vistas, un `case`; la escala del humo no es de banco | Presión: A; Corrientes: B |
| vidrio transparente + quinta pasada + Q16 | sí (L) | sí, un día + un escenario | ninguna |
| brasa viva en el frasco | sí (C) | sí | ninguna |
| J entera | sí | sí (crítica de «las leyes juzgan») | independiente; las situaciones esperan a `CorrerSello` |

Nada pide física fuera del catálogo. Coste por tick medido por el refutador de ingeniería, no
estimado: +0,07 ms en el nivel real y +0,45 en los sintéticos, sobre 1,6-1,9 ms; cabe. Determinismo:
enteros y orden fijo; sin sales nuevas salvo que B las pida.

**1.1 Cuatro correcciones de A que el código exige y la dirección no presupuesta como tales:**

1. **`LabCampos` no despacha gas.** :214-236 lista `Empty`, `Water`, porosos, rocas, planta, hogar,
   manantial, sumidero. Bajo una celda de `Smoke`, `Steam` o `Fire` el byte `aire` no se calcula
   nunca. Una chimenea llena de humo (que es el caso de uso) es un agujero en el campo. Caso de gas
   obligatorio (lo dijo la refutación; la dirección no lo cuenta).
2. **`Transform` escribe `mat` y `aux` y nada más** (:707-730). El humo nace en un `Empty` vecino
   (`SpawnSmokeNear` :941) sin desplazar nada: 128 unidades desaparecen por celda de humo nacida, y
   reaparecen (o no) cuando el humo expira. Con `combustHumoPct 16` sobre cientos de pasos, eso es un
   sumidero de miles de unidades junto al fuego: **un vacío artificial que succiona aire hacia la
   pila y se leería como tiro**. El desplazamiento en `Transform` no es parte de la auditoría fina:
   es condición para que el spike de B mida algo. La dirección lo pone en la lista de A, bien; lo
   que digo es que va DENTRO del spike, no después.
3. **Doble búfer no es opcional.** `LabCampos` visita `i ≡ tick mod 8`; como `W = 768` es múltiplo
   de 8, eso son columnas enteras `x ≡ tick mod 8`, recorridas de abajo arriba (:208). Arriba y
   abajo de una celda se visitan en el mismo tick y en ese orden; izquierda y derecha, nunca. Una
   transferencia en sitio (como `LabIntercambioVapor`) sesga la vertical: justo el eje en que se
   quiere medir el tiro. El vapor vive con ese sesgo porque nadie mide su `vy`.
4. **La relajación hacia 128 rompe la identidad que promete.** «Σaire = Σ0 + inyectado − consumido
   exacto en los nueve escenarios» y «relajación lenta hacia 128» no pueden ser ciertas a la vez:
   la segunda es una fuente sin contador. O se cuenta (`LabAireRelajado`, cuarto término) o se
   sustituye por la ley que `01 §6.3` dejó escrita (la planta que repone aire donde tiene luz), que
   además cierra el ciclo fuego ↔ huerto ↔ boca. Y en el banco la elección es forzosa: `Correr`
   pone `LuzCieloX0/X1 = −1` (:277-279), así que **la fuente de la boca del cielo no existe en
   ninguno de los nueve escenarios**. «El caudal de la boca se deriva en banco con tres identidades»
   se deriva en un banco sin boca. Cielo por geometría (`Empty` en `H−2` como fuente), como propuso
   la crítica de «las leyes juzgan»: una línea, pero hay que decirla.

## 2. El spike de B, tal como está escrito, no puede acertar

La dirección apuesta el título a «chimenea con boca N» (sala 20×14, pila sobre hogar, chimenea 2×40,
boca baja 0/1/3/8; `Σvy` en la sección abierta frente a tapada; mata si `Σvy ≈ 0`). Tres hechos del
código lo invalidan como está.

**La columna no puede estar caliente.** `LabFlujoTermico` (:1349-1356) toma `k = min(ki, kj)`; en
una chimenea de `Empty` entre roca, cada celda cede a dos muros con `k = min(4, 2) = 2`, recibe de
abajo con `k = 8` (convección) y cede arriba con `k = 4`. El modo estacionario `e(n) = r^n` cumple
`8 + 4r² − 16r = 0`, `r = 2 − √2 ≈ 0,59` (el refutador de apalancamiento sacó 0,63 con otro supuesto;
da igual): a diez celdas el exceso es < 1 % y encima `TiroAmbienteTicks 32` lo tira a ambiente.
`p = aire·(temp+120)` en la columna es `p = aire·(ambient+120)`: no hay gradiente que bombear. Lo que
SÍ va caliente por la chimenea es el humo, que nace con la temperatura del hueco vecino a la llama y
la lleva en `SwapCells` (:264-279); pero el humo es un MATERIAL con su regla propia (sube siempre que
hay `Empty` encima, :1531-1560), no el byte. El spike sin advección de calor en las transferencias
del byte (el helper par y conservativo de la refutación de `adveccion`: seis líneas) mide una
columna fría. La crítica de «la simulación es el juego» llegó a lo mismo; lo confirmo desde la
ecuación.

**`Σvy` en la sección está confundida por el intercambio.** Cada celda de humo que sube hace bajar
un `Empty` (`Move` → `SwapCells`). Si `aire` viaja en el swap (debe: es masa), el campo `viento`
medido sobre `Empty` en la chimenea registra masa BAJANDO mientras el humo sube, con tiro real o sin
él. Y a la inversa: el vacío del punto 1.1.2 daría `vy` hacia la pila sin tiro. La métrica correcta
no es la sección de la chimenea: es **el aire que entra por la boca baja** (suma con signo de las
transferencias que cruzan esa sección por tick), abierta frente a tapada a igual N, más el cociente
`LabUnidadesRespiradas / LabCombustibleQuemado` de la pila. Eso es lo que «la segunda boca importa»
significa para el fuego; `Σvy` es un proxy que puede fallar en los dos sentidos.

**El ruido no está medido.** «Curvas indistinguibles» no tiene suelo. El banco corre a 600+ ticks/s:
cada configuración con dos gemelos (el montaje entero corrido +1 y +2 celdas en x, mismo diseño y
otro dado, el protocolo que ya propuso la crítica de «las leyes juzgan») cuesta 30 s. Sin gemelos,
«separa» y «no separa» los decide quien mira la curva.

Lo que la evidencia del laboratorio dice de antemano: R136 midió la curva boca → temperatura del
horno **plana** (228/232/231 raw) y la carbonera boca 1/4/8 → 100/81/88 % de carbón, no monótona;
INFORME_FINAL :278 cierra «el tiro no existe: humo del carbón 4 % → 40 % idéntico al bit». Todo eso
se midió sin masa, así que no refuta A; pero sí dice que **la boca hoy no regula nada más que el
primer escalón**, y B tiene que crear un continuo donde el laboratorio solo encontró un bit.

## 3. Evidencia que la sostiene

- **Los seis primeros segundos del clip existen.** La sordina (R135 HF1) ya quita la lengua y echa
  un cuarto del humo; abrir una celda ya devuelve la llama. La carbonera de boca 1 da 100 % de carbón:
  el regulador por geometría está medido. A convierte un bit en un caudal; es una mejora real y
  barata de algo que ya funciona.
- **La maquinaria de conservación está escrita**: `LabIntercambioVapor` es el patrón (entera, en
  sitio, con recorte); `LabBalanceU` y el libro de energía (HF4) son la auditoría a copiar. El
  desplazamiento en la mudanza de `LabPresion` ya se hizo una vez para el vapor (R131: «el aire del
  destino NO se aniquila»); hacerlo para el byte es repetir un fix conocido.
- **Hay sitio en el tick**: 1,6-1,9 ms con peor caso 3,1 y ×10 sostenido; el byte cuesta +0,07 en el
  nivel real.
- **La bolsa no depende de B** (refutación de `aire-atrapado`): campana, cámara que solo se inunda
  hasta el túnel y bomba con el núcleo frío existente sobreviven con A sola. Es el órgano más
  valioso de la dirección si el tiro muere.

## 4. Evidencia que la contradice

- **El humo medido es delgado.** r136 §3: pila fina en caja sellada de 200 celdas, 9 celdas de humo,
  bolsa de 0,5-1,4 filas; carbonera boca 1, 0 humo. A hace la sordina por byte, no por humo vecino:
  el fuego puede morir con MENOS humo que hoy. La veleta existe donde menos humo hay. Es el problema
  de las plantas de un píxel trasladado al gas; no es de banco.
- **La situación 3 («El humo sabe») depende de L.** «Un huerto bajo la boca» necesita que el huerto
  viva, y eso es el día que remide Q16 (`luz[i+W]`), que no está hecho. Hoy el huerto de referencia
  nunca vivió. La situación está prometida sobre un negativo medido.
- **La aritmética de «La vela» y «La carbonera»** la hizo la crítica hermana: nueve fibras suman ≤
  2 880 ticks de combustión y el día 2 empieza en el 3 600 (inganable); «carbón ≥ 6» con 25 % sobre
  16 celdas es una lotería de coordenada. Lo cito porque el validador por veredicto, tal como está
  en J, aceptaría las dos: hace falta invariancia por traslación (gemelos) dentro del validador.
- **Multijugador.** «El mismo aire» exige host + espejo (ruta A, 3-4 semanas, fuera de presupuesto) y
  tres arrays más en el snapshot; el asíncrono por fichero no comparte aire. Y el determinismo entre
  máquinas (editor contra IL2CPP) sigue sin un solo dato: la comparación de registros es promesa.

## 5. La prueba más barata capaz de matarla (corregida)

**Prueba 0 · medio día, cero física nueva.** Montar «chimenea con boca N» en `LabBench` con el
código de hoy y `LuzCielo` activo por geometría. Imprimir por tick: exceso de `temp` sobre ambiente en
la chimenea a 5/10/20/40 celdas; celdas de `Smoke` en la chimenea y en la sala; y en la sala de «La
vela» (14×10, nueve fibras, sellada), celdas de humo por tick y filas ocupadas bajo el techo. **Decide
el alcance, no el veredicto**: exceso ≤ 2 raw a diez celdas → la advección de calor entra en el spike
como condición; humo ≤ 1 fila → el render va antes que el byte y la prueba humana de legibilidad
(la de la crítica hermana, con control «murió por combustible») se hace esa misma tarde sin código.

**Prueba 1 · el spike escrito para poder acertar · 3 días tras A-lite (4-5 días).** A-lite = byte
en `Empty` Y en gas (caso de gas en `LabCampos`), difusión con doble búfer, fuente por geometría,
consumo, umbrales, **desplazamiento en `Transform` para nacimientos y expiración de gas** (sin esto
el resultado es artefacto), Σaire impreso. Spike = `p = aire·(temp+120)` con sesgo hidrostático,
advección de calor par y conservativa en cada transferencia, `viento` en dos bytes por eje.
Métrica: aire neto que entra por la boca baja por tick y `respiradas/quemadas`, chimenea abierta
frente a tapada, N ∈ {1, 3, 8}, tres gemelos (+0, +1, +2 celdas en x) por configuración: 18 corridas
× 9 000 ticks ≈ 5 min. **Mata**: entrada(abierta) − entrada(tapada) < 2× la dispersión entre gemelos
en todo N, o `respiradas/quemadas` indistinguible. **Confirma**: en N = 3, abierta ≥ 2× tapada y la
pila arde respirando (≥ 0,8) donde tapada no. Sin plan B: no se sube ningún coeficiente hasta que
separe; si «casi» separa, se ha muerto.

**Prueba 2 · una noche de banco, tras la 1.** Barrido `AireConsumo × AireMinRespira × AireDifusion ×
caudal` contra cuarto sellado, chimenea de boca 3, fogón abierto de «laboratorio base», horno
(18/18), carbonera (25 %) y tolva (466 s). Imprime si existe un punto; no lo elige. Si no existe,
la dirección hereda una decisión humana por situación y su nota de tuning baja dos puntos.

Coste total hasta evidencia: 0,5 + 4-5 + 3 + 1 días ≈ **2 semanas**, un hilo de Opus, un día de
verificación de Fable, media tarde de una persona que no sea Cesar.

## 6. Tiempos corregidos

| hito | Opus | calendario | personas |
|---|---|---|---|
| evidencia para matarla (§5, pruebas 0-2) | 9-10 días | **2 semanas** | 0,5 día |
| A completa (los cuatro puntos de §1.1, vista Presión, cielo por geometría en el banco) | 1-1,2 sem | dentro de las 2 primeras semanas | 0 |
| B (si vive): `viento` leído por `ProcessGas`, lengua a sotavento, vista Corrientes | 0,6-1 sem | semana 3 | 0 |
| bolsa 0,75 ‖ vapor 0,2 ‖ re-balance 0,5 ‖ render y brasa 1 ‖ vidrio + Q16 0,4 | 2,85 sem | semanas 3-5 en dos hilos | 1-2 días (re-balance como decisión) |
| J recortado (registro + volcado + `CorrerSello` + condición + balanza), en paralelo desde el día 1 | 4,5-5 sem | semanas 1-5 | 0 |
| prototipo feo: A + B + bolsa + render + 3-4 situaciones sobre `CorrerSello` | ≈ 5 sem propias + J | **7 semanas** (6 si B muere y se retira; 8-9 con validador, cuna y escalera completa) | 3-4 días (resolver situaciones, mirar el humo) |

**Paralelizable**: todo `Sim/` con banco por pieza; J entera en otro hilo. **Secuencial**: A → spike →
decisión B → (bolsa, vapor, re-balance) → situaciones; y J → `CorrerSello` → validador → situaciones
aceptadas. El camino crítico es J recortado (4,5-5) más la escritura y validación de situaciones (1),
como dice la dirección; lo que la dirección no dice es que **la decisión sobre B cae en la semana 2 y
reescribe la escalera** (peldaños 3, 6, 7, 8) si muere. La verificación de Fable por ronda fue ~1:1
con la implementación en el laboratorio; el ×1,25 está metido.

**Humano e incomprimible**: 15-25 días en tres meses (cifra de la crítica de iteración; la comparto).
Desde esta lente, lo que ningún banco sustituye: la mirada al humo a escala (2-3 sesiones), «La vela»
sin panel (1), qué identidad cede si el barrido no encuentra punto (1-2), el re-balance del horno
como decisión (1-2) y resolver cada situación una vez por paquete de física.

## 7. Qué se automatiza en lugar de iterar a mano

Perfil térmico de la chimenea y celdas de humo por tick (prueba 0); Σaire con los cuatro términos
como assert en los nueve escenarios más «campana»; gemelos ±1/±2 celdas como suelo de ruido estándar
del banco (y dentro del validador de J: invariancia por traslación); caudal neto por boca como
métrica de toda ley de aire; barrido nocturno del canon con la región factible impresa; boca del
horno 1..4 contra el mapa 18/18; métrica de legibilidad del humo (filas coherentes bajo techo,
fracción de ticks con columna de ≥ N celdas del mismo signo de `vx`) como puerta previa a toda sesión
de Cesar; regresión nocturna de los registros de autor tras cada entrega de física; IL2CPP contra los
63 hashes del editor (un día) antes de prometer comparación por fichero.

## 8. Órganos a conservar si se descarta

**A entera y bien hecha** (byte en `Empty` y en gas; doble búfer; desplazamiento en `Transform`,
`LabTransformar`, `LabNacerAgua`, `LabGotear`, ceniza y planta; `LabRespira` y `ProcessFire` por
umbral; Σaire con cuatro términos; cielo por geometría en el banco): la llama inmortal muere en
cuarto cerrado, apagar cerrando, bancar brasas, la carbonera por caudal. **La bolsa en `LabPresion`**
por altura efectiva (sobrevive sin B). **«Chimenea con boca N» con caudal neto por boca y gemelos**
como escenario permanente: si mide que no hay tiro, también es un hecho del banco. **El helper de
advección par y conservativo** (`LabAdvectar`) aunque B muera: es la única forma correcta de mover
calor con masa en este motor. **La planta que repone aire** en vez de `AireRelaxTicks`. **El vidrio
transparente + quinta pasada + Q16.** **La brasa viva en el frasco.** **La trampa de agua como
compuerta con hora.** **La lengua a sotavento** (una línea, dormida hasta que exista `viento`). **El
residuo como testigo** (carbón frente a ceniza: cláusula del sello «murió por aire»). **El método del
canon por identidades en banco** como patrón para D y F. Y el remate «TIRA» guardado para el día en
que B se mida viva.

## 9. Rúbrica v2 (13 ejes, 1-10)

| eje | nota | por qué (desde esta lente) |
|---|---|---|
| apalancamiento_sistemico | 6 | A + bolsa son el cruce físico más grande del catálogo; la mitad de las 22 decisiones es B, con dos predicciones de fallo y un spike que hoy no puede acertar |
| leyes_ejecutan_revelan_juzgan | 6 | ejecutan con conservación solo si se cuenta la relajación o se sustituye; revelan por residuo (medido) y por humo (0,5-1,4 filas, medido); juzgan con J |
| iteracion_humana | 6 | 15-25 días reales frente a 3-4 declarados; lo caro es la mirada al humo y la identidad que cede |
| verificabilidad_automatizable | 7 | todo tiene escenario y gate, pero la métrica central está confundida por el swap y el barrido, y el banco no tiene boca: verificable sí, verificada como está no |
| simulacion_es_el_juego | 8 | cinco verbos existentes, respuesta inmediata, el reloj lo pone el fuego; J es árbitro, no bucle |
| onboarding_garantizable | 5 | media escalera depende de B; las situaciones 1-2 no cierran aritméticamente; la 3 depende de un negativo medido (Q16) |
| observabilidad | 4 | el byte es invisible por naturaleza; el humo medido es delgado y A lo reduce; las vistas son paneles; la lengua es una línea |
| tiempo_como_apuesta | 7 | el fuego como reloj es real; la apuesta de días la dan tolva y carbonera, no la vela |
| multiplayer_emergente | 5 | «el mismo aire» necesita host + espejo y tres arrays en el snapshot; el fichero no comparte aire; cross-machine sin un dato |
| profundidad_por_leyes_estables | 6 | el volumen sustituye a la tabla si B vive; sin B es el tamaño de un agujero, y R135/R136 midieron un bit, no una curva |
| cuerpo_del_jugador | 4 | instrumento honesto (halo, tos, tizne, brasa); el humo no llega a la cabeza en lo medido |
| identidad_comercial | 6 | «mira si tira» y «TIRA» valen; sin B el clip es «se apagó solo», que `03` marca como bug |
| dificultad_tecnica | 6 | A acotada y barata (+0,07 ms); B abierta en resultado y con cuatro sitios más de los presupuestados; nada difícil, mucho fácil de medir mal |

Puertas: iteración 6, apalancamiento 6: pasa. No pasa a finalista sin la prueba 1 corregida.

## 10. Veredicto

**Segunda ronda.** Condiciones para volver como finalista: (1) la prueba 0 se corre esta semana sobre
el código actual y fija el alcance del spike; (2) el spike incluye doble búfer, caso de gas,
desplazamiento en `Transform` y advección de calor, y se mide por caudal neto en la boca baja contra
gemelos, no por `Σvy` en la sección; (3) la relajación se cuenta o se sustituye por la planta que
repone aire, y el banco tiene cielo por geometría antes de derivar ningún canon; (4) si B muere, la
dirección se retira sin B' y deja A, bolsa, advección y escenario en «las leyes juzgan». Con B viva
es la dirección cuyo contenido crece más rápido que su coste, porque es geometría de agujeros sobre
una cantidad conservada; sin B es la mejor entrega de sustrato del panel con un título prestado.
