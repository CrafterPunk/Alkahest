# LUZ ÓPTICA · la luz como flujo enrutable

*(Panel de leyes, segunda pasada. Leído antes de proponer: `SimStepper.Laboratorio.cs` (LabLuz
:1177-1285, LabPlanta :800-905, LabHogar :915-975), `LabParams.cs` (:124-131), `LabMateriales.cs`,
`CellGrid.cs:275`, `LabBench.cs` (:76-87, :262-345), `Universe.cs` (Ice :798, VidrioVerde :1000),
`Cincel.cs:440`, `SimRenderer.cs:1054`.)*

## 0. Lo que hay hoy, leído en el motor

`LabLuz` corre cada `LuzCadaTicks` (16). Pone `luz = 0` en todo el mundo, `255` en las celdas
`EmiteLuz` (fuego, brasa, hogar) y en la fila `H-2` de la boca del cielo (`LuzCieloX0..X1`, 118-124
en el nivel), y hace cuatro barridos de máximo con decaimiento: arriba→abajo con `LuzDecayCielo` (1),
los otros tres con `LuzDecayAire` (8); agua 20, planta 12, humo 24. `LabLuzDesde` solo transmite
aire, gas, agua, planta y fuentes: **cualquier sólido recibe y no transmite, vidrio incluido**. El
campo es posicional (no viaja en `SwapCells`) y entra en los hashes del banco (`HashLuz`).

Tres hechos que corrigen o precisan el sustrato del encargo:

1. **Sí hay emisores.** `LabLuz` pone a 255 fuego, brasa y hogar, y `DISENO_FUEGO.md` §1 lista
   «luz del fuego hace crecer plantas» como cruce vivo. Lo que falta es consecuencia visible de esa
   luz fuera del panel F8.
2. **La luz no tiene dirección, y eso produce un artefacto:** el barrido descendente aplica el
   decaimiento 1 a *todo* aire, así que la luz de un hogar también «cae» por su columna casi sin
   perder. Nadie lo diseñó.
3. **Los materiales ópticos ya existen y ya se fabrican:** `VidrioVerde` nace de arena con ceniza
   a ≥`VidrioRaw` (200) durante `VidrioVisitas` (60); `Ice` nace del agua bajo `freezesAt` (60 raw)
   y el núcleo frío está a 30. Ninguno tiene uso. `Tallable` solo la consulta `Cincel.cs:440` (el
   frasco usa `EsSolidoDelMundo`, la tabla que rompió la campaña en R145): dar vidrio al cincel es seguro.

Cuatro modificaciones, ordenadas por juego nuevo por unidad de complejidad. Ninguna usa RNG.

---

## 1. `haz-y-espejo` — El haz del cielo, y el vidrio y el hielo que lo desvían

**Resumen.** La luz del cielo pasa de máximo que se filtra a HAZ con dirección; vidrio y hielo lo
transmiten si son planos y lo giran 90° si son cuña. Espejos, ventanas y guías de luz salen de una
regla geométrica, sin campo nuevo ni orientación pintada.

**Regla, en términos del motor.** `LabLuz` se parte en dos pasadas:

- *HAZ.* Por cada columna `x` de la boca, un rayo con potencia entera `P = 255` y dirección
  `(0,-1)` avanza desde `H-2`. En cada celda: `luz[i] = max(luz[i], min(P,255))` y
  `P -= LabLuzDecay(mat[i])` con la tabla actual más `LuzDecayVidrio` (4) y `LuzDecayHielo` (6).
  En un sólido opaco deposita `P` en la cara (como hoy) y muere. En `VidrioVerde` o `Ice` mira los
  dos vecinos laterales (perpendiculares a la dirección): si **exactamente uno** está abierto
  (aire, gas, agua, planta) es una CUÑA y el rayo gira 90° hacia el lado abierto perdiendo
  `LuzEspejoPerdida` (8); si los dos están cerrados es cara plana o bloque y lo ATRAVIESA con el
  decaimiento del material (ventana); si los dos están abiertos es lámina de una celda y también lo
  atraviesa (varilla = guía de luz). Todo medio resta ≥1, así que ningún rayo vive más de 255
  pasos: no hace falta tope contra bucles. Orden fijo por `x`.
- *DIFUSA.* Los cuatro barridos de hoy, con un cambio: el descendente usa `dAire` como los otros.
  `LuzDecayCielo` pasa a ser el decaimiento del haz en aire. Se va el artefacto del punto 2 de §0
  y el cielo queda como único emisor direccional: sol = haz, fuego = halo.

La ventana `bx0..bx1` (optimización H5) se calcula tras el haz con el `x` mínimo y máximo que los
rayos tocaron. Nada despierta chunks: `luz` la lee `LabCampos`, que visita toda la grilla. Dos
líneas más: `VidrioVerde` entra en `Tallable` con `ProductoDeTalla → Grava`, y el cincel talla facetas.

**Por qué la cuña sale sola.** Una pila de arena tiene talud de reposo; vitrificada conserva la
forma y su ladera es una escalera de cuñas. Un charco helado sobre escalones de roca es otra. La
orientación del espejo no se pinta: es la forma que ya tiene la materia.

**Cruces.**
- *Plantas:* el huerto oeste (x100-135) recibía 7 de 73 caras; una cuña de 8 celdas al pie del
  pozo (x118-124, y≈250-257) gira el haz sobre el lecho entero y el halo lo baja al sustrato con
  luz >200. El huerto bajo tierra por espejos.
- *Humo:* diez celdas de humo en el trayecto apagan el haz. La sombra del alambique (R148) deja de
  ser un gris para ser un corte.
- *Agua y hielo:* un charco de 12 celdas en el camino lo mata; el vapor es transparente. Un espejo
  de hielo cerca de un hogar se derrite y el huerto se apaga: calor y luz se cruzan sin regla nueva.
- *Fuego y horno:* la receta de vidrio adquiere producto con uso; la faceta es decisión del cincel.
- *Recinto + ventana:* una cámara cerrada con techo de vidrio recibe haz y retiene la
  transpiración (`LabSecarHacia` satura el aire): el invernadero emerge.

**Decisiones nuevas.** Dónde tallar la cuña y hacia qué lado; si el haz va al huerto o al hogar;
mantener el corredor de luz limpio de humo y agua; vidrio para ventana o para espejo; mantener frío
un espejo de hielo o pagar el horno por uno de vidrio; sellar con vidrio en vez de roca.

**Observación.** Sin panel: las plantas nacen donde el haz aterriza (`GerminaPorMil` sobre
sustrato iluminado) y, con el candidato 3, el haz es una línea clara. Con visor: F8 Luz ya dibuja
`luz`; el haz aparece como trazo de 255 con halo y la cuña como el codo del trazo.

**Coste.** 1,5 semanas de Opus: haz (2 días), cuña (1), materiales y talla (1), escenario y métrica
(1), documentación y hashes (1). Sin RNG. Por tick: 7 rayos × ≤255 celdas cada 16 ticks, ~0,01 ms;
la difusa igual que hoy.

**Verificación headless.** Escenario nuevo `huerto por espejos`: nivel real más cuña de vidrio de 8
celdas al pie del pozo; métrica `LuzCarasLecho` (celdas del lecho con `luz ≥ PlantaLuzMin`, cuadrícula
de `MedirLecho`). Aceptación: ≥30 de 36 columnas contra 7/73 hoy. `laboratorio base` mueve `HashLuz`
(física nueva, se apunta); `mundo entero despierto` no tiene boca y debe dar `MsLuz` igual.

**Tuning humano: 9.** Tres números derivables (vidrio y hielo entre aire y agua; una pérdida por
bote). Ningún balance.

**Apalancamiento: 9.** Convierte la boca del cielo en recurso enrutable, da uso a dos materiales
que ya se fabrican y un verbo al cincel, y resuelve por geometría el negativo más caro del
laboratorio sin tocar plantas ni agua.

**Riesgo mayor.** La cuña en rejilla solo gira 90°: sin diagonales ni foco por lente, y una
superficie rugosa de vidrio dispersa en direcciones no previstas. Es comportamiento Noita, pero sin
el candidato 3 el codo es invisible y parece capricho.

---

## 2. `insolacion` — Lo que la luz pierde, lo gana en calor

**Resumen.** Cada unidad de luz que un medio le quita al haz se vuelve calor en esa celda, con
tope que crece con la potencia recibida. Un sol seca; dos prenden yesca; tres son un hogar; cuatro,
un horno. La concentración no es una lente: son rayos convergiendo desde varias direcciones.

**Regla.** En la pasada de haz, cada resta de `P` se acumula en `_labSol[i]` (int; reutiliza
`_labCola`, que solo usa `LabPresion` y va antes en `LabPasadas`); el sólido que detiene el rayo
recibe el `P` restante. Tras los rayos, por celda con `sol > 0`: `tope = ambient[i] + LuzSolRaw ×
sol / 255`, `cuanto = LuzSolCalor × sol / 255`, escritos con `LabCalentarHasta` (mismo clamp y
`WakeChunk` que el hogar) en un contador `LabRawSol` del libro de calor entregado. `sol` pasa de
255 cuando convergen rayos; con cuatro direcciones, el máximo es 4 × 255. La luz difusa NO calienta.

**La escalera sale de umbrales que ya existen.** Cámara alta a 64 raw, `LuzSolRaw = 36`: un sol →
100 raw (80 °C: seca, no hierve); dos → 136 (> 130: la fibra prende; la planta, 140, no); tres → 172
(≈`HogarRaw`: hierve, cuece arcilla a 150, no prende carbón); cuatro → 208 (> 200: carbón y vidrio,
horno solar). Cualquier `LuzSolRaw` entre 35 y 41 da los cuatro peldaños; lo fija Opus en el banco.
`LuzSolCalor` = 40 por pasada, por analogía con `HogarCalor`.

**Cruces.**
- *Fuego:* la yesca mojada (`FibraMojadaMin` 100) se seca al sol y prende; el fuego que enciende un
  espejo de hielo derrite el espejo que lo encendió: hogar solar autolimitado.
- *Agua:* la evaporación ya escala con la temperatura: un charco de una celda sobre roca bajo tres
  soles hierve desde el fondo y alimenta el serpentín frío. Caldera sin humo: la sombra de R148 desaparece.
- *Arcilla y arena:* terracota a 150 bajo tres soles; vidrio bajo cuatro, que fabrica más espejos:
  bucle positivo acotado por la geometría.
- *Plantas:* el punto soleado seca el doble (`rate = tasa × (16 + t) / 16`): el riego que ahogaba
  (R135) tiene contrapeso; dos soles queman una hoja; el dosel protege el suelo: sombra = humedad.
- *Hielo:* absorbe 6 por celda: el sol no funde un espejo de hielo; el fuego sí.

**Decisiones nuevas.** Qué única cosa va bajo la vertical de la boca (secadero, caldera, yesca,
huerto); enrutar dos, tres o cuatro rayos a una celda; apartar el huerto del foco; calentar sin
combustible ni humo o pagar fuego lejos del haz.

**Observación.** Sin panel: vapor, sequedad, chamuscado y terracota (termómetro de máxima de la
primera pasada) donde el haz aterriza. Con visor: F8 Temperatura; `LabRawSol` da el número.

**Coste.** 1 semana sobre el candidato 1 (3 días si solo se hace en la vertical de la boca).
Determinismo intacto; ≤1 800 celdas con `sol > 0` cada 16 ticks.

**Verificación headless.** Escenario `lupa de espejos`: cámara de roca con boca de 7 columnas y
cuñas que llevan 1, 2 y 4 rayos a tres celdas de fibra y una de arena con ceniza. Aceptación: 1
rayo no prende en 3 000 ticks; 2 prenden; 4 dan `LabVidrio ≥ 1` en ≤9 000. En `alambique de r141`,
`Goteos` y `HumedadMediaLecho` antes y después: el sol debe bajar la humedad media sin bajar
`SustratoPct` de la aceptación de R148. Identidad: `LabRawSol` = Σ raw escritos.

**Tuning humano: 8.** Un parámetro con intervalo válido y otro heredado.

**Apalancamiento: 8.** Una regla de conservación (luz absorbida = calor) cruza con fuego, agua,
arcilla, arena, plantas y hielo, y reescribe la escalera doméstico/industrial en clave de geometría.

**Riesgo mayor.** Deriva térmica: una cara de roca a 100 raw permanente calienta por conducción la
cámara alta (8 °C por clima) y mueve la saturación del aire, o sea la condensación del alambique.
Se mide antes de aceptar; la mitigación es que el haz cubre 7 columnas.

---

## 3. `ver-por-la-luz` — El ojo del aprendiz ve por `luz`

**Resumen.** El render del laboratorio multiplica el color de cada celda por su `luz`: lo oscuro
es oscuro, el fuego es lámpara, el humo ciega, el haz se ve. No cambia la simulación: un campo que
solo existía en F8 pasa a ser la condición de percibir.

**Regla.** En `RenderChunk` (`SimRenderer.cs:1054`), tras `ComputeCellColor`: `lum = LuzVerMin +
(255 − LuzVerMin) × luz / 255`, con `LuzVerMin` ≈ 40. Los sólidos ya reciben en `luz` la cara
iluminada. Un desenfoque 3×3 opcional separa la resolución de la percepción de la de la simulación.
Pieza de ingeniería: los chunks se repintan por `chunkTouchedTick` y `luz` no toca chunks; `LabLuz`
debe marcar «luz cambiada» por chunk y el render leerlo. Dos días.

**Cruces.** Fuego: segundo uso, alumbrar; un fuego en cuarto cerrado lo llena de humo y deja a
oscuras a quien lo encendió (accidente legible, clip). Humo: ciega. Agua: una galería inundada se
atenúa. Vidrio: ventana. Plantas: la penumbra del dosel es el huerto sano. Con el candidato 1, el
haz es una línea en pantalla y el espejo se explica solo.

**Decisiones nuevas.** Llevar fibra ardiendo (11 s por celda) para ver; encender el hogar por la
luz; enrutar sol como lámpara permanente; no ahumar la cámara donde se trabaja.

**Observación.** Es la observabilidad. F8 Luz queda como vista numérica.

**Coste.** 0,5-1 semana. Sin efecto en determinismo ni hashes. Una lectura de byte por celda repintada.

**Verificación headless.** Sin hash que mover. Captura PNG headless (receta `RunCommand`) del
pozo de luz y el hogar: una mirada de Cesar fija `LuzVerMin`.

**Tuning humano: 6.** Curva y suelo son gusto, no balance: dos o tres miradas.

**Apalancamiento: 7.** Barato y condición para que 1 y 2 se lean sin panel; da un uso al fuego y
una consecuencia al humo que existían en la física y no en el ojo.

**Riesgo mayor.** Legibilidad: demasiado oscuro vuelve obligatorio F8. `LuzVerMin` es la válvula.

---

## 4. `filtros` — La turbidez oscurece, el hielo aclara, el vidrio tiñe

**Resumen.** El decaimiento de la luz en el agua depende de su carga: turbia opaca, decantada
clara. Une la cadena de calidad del agua con la de la luz.

**Regla.** `LabLuzDecay` recibe el índice: `Water → dAgua + carga[i] × LuzTurbidez / 255`
(`LuzTurbidez` 40: agua limpia 20 por celda, 12 de alcance; turbia a 255, 60 por celda, 4). `Ice`
y `VidrioVerde` con los decaimientos del candidato 1. Vale para el haz y para la difusa.

**Cruces.** Decantación y colmatación de grava deciden cuánta luz llega al fondo; un diluvio turbio
deja a oscuras lo que hay bajo un techo de agua; una poza congelada por el núcleo frío es más clara
que líquida. Con suelo de vidrio y agua encima: un lucernario que solo funciona con agua limpia.

**Decisiones nuevas.** Dejar reposar el agua antes de inundar el lucernario; frenar la erosión con
raíces para conservar la claridad; congelar la poza para dejar pasar la luz.

**Observación.** Sin panel: el haz se apaga en agua sucia y vuelve al decantar. Con visor: F8 Carga
y F8 Luz.

**Coste.** 2-3 días. Determinismo intacto; una lectura de `carga` más por celda de agua.

**Verificación headless.** `diluvio turbio` (existe): métrica `LuzFondoDiluvio` a los ticks 0,
1 500 y 3 000; debe subir conforme decanta.

**Tuning humano: 9.** Un coeficiente.

**Apalancamiento: 5.** Solo multiplica: ninguna planta germina bajo el agua (`mat[i+W] == Empty`),
así que un fondo iluminado no decide nada sin haz ni ojo.

**Riesgo mayor.** Ninguno técnico; invisible como juego si va sola.

---

## 5. Orden y dependencias

| # | Candidato | Depende de | Semanas | Apalanc. | Tuning |
|---|-----------|-----------|---------|----------|--------|
| 1 | haz-y-espejo | nada | 1,5 | 9 | 9 |
| 2 | insolacion | 1 (parcial sin 1) | 1 | 8 | 8 |
| 3 | ver-por-la-luz | nada | 0,5-1 | 7 | 6 |
| 4 | filtros | nada | 0,5 | 5 | 9 |

Paquete completo: unas 4 semanas de Opus, todo verificable en banco menos la curva del ojo.
Secuencia de menor riesgo: 3 → 1 → 2 → 4: ver, enrutar, quemar.

---

## 6. Lo que descarté y por qué

- **Lente refractiva y rayos diagonales.** Daría foco real (un panel de 7 celdas converge 3 rayos
  a 3 de profundidad). Una semana más, cuña para diagonales y render oblicuo; la convergencia por
  cuatro direcciones ya da la escalera entera. Extensión de 1, no candidato.
- **Reflexión en agua en calma.** La superficie se mueve cada tick y una regla por `reposo`
  parpadearía; un haz vertical sobre agua plana solo vuelve hacia arriba.
- **Emisores nuevos (lámparas de aceite, cristal luminoso, bichos que brillan).** El fuego ya
  emite y la fibra ardiendo ya es antorcha. Biblioteca, que la función objetivo penaliza.
- **Hora del día y noche.** Temporada y evento. Si algún día existe, es apuesta de SOLTAR.
- **Fotosíntesis proporcional a la intensidad.** Obliga a balancear velocidades contra riego y
  transpiración con personas; el umbral `PlantaLuzMin` ya da la forma (R134).
- **Sustituir los barridos por un BFS direccional.** Los barridos ya son la parte difusa correcta;
  el haz se añade encima.
- **El cuerpo que se quema al sol.** Dos soles sobre el aprendiz serían quemadura y clip, pero es
  la lente del cuerpo: el muñeco no está en la grilla. Gancho de 2.
- **Decoloración por luz.** Cosmético: no ejecuta ni juzga nada.
