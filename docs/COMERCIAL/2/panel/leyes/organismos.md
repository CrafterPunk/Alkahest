# Panel de leyes · lente ORGANISMOS Y DISPERSIÓN

*(Segunda pasada, 2026-09-05. Leído: `SimStepper.Laboratorio.cs` (LabPlanta 801-914, LabPoroso 535-715, LabRoca 773-786, LabLuz 1177-1285, LabErosion 173-196), `LabParams.cs`, `LabMateriales.cs`, `Universe.Laboratorio.cs`, `CellGrid.cs` 160-205, `SimStepper.cs` (ProcessPowder 1061, ProcessLiquid 1184-1239, Move 738), `LabBench.cs`.)*

## 0. Dos hechos del código que corrigen el enunciado del sustrato

**La planta muerta SÍ deja fibra.** `LabPlanta` transforma en `Fibra` la celda sin raíz ni tallo debajo (línea 811) y la marchita (899); la fibra es Powder de densidad 60 y cae. Lo que el veredicto llamó «G5, la única descongelación de física» no es una transformación que falte: faltan cuatro eslabones alrededor de una que ya existe. (1) Rendimiento: una celda de fibra por celda de planta, y solo al morir entera. (2) Cosecha: no hay verbo que corte sin matar la raíz. (3) Secado: la fibra nace seca (`LabTransformar(i, Fibra, 0, 0)`), cae al lecho y el sedimento, que es fino, la empapa por `CapilarArriba` (585) hasta lecho−64; sobre un lecho a más de ~164 supera `FibraMojadaMin` (100) y no prende jamás. (4) Transporte: nadie la mueve. Los candidatos 1 y 2 atacan esos cuatro.

**Las semillas ya flotan.** Semilla (90) y Fibra (60) son más ligeras que el agua del laboratorio (110): `ProcessPowder` 1088 no las hunde. Pero no viajan: sobre agua solo deslizan con `fluidity > 2` (1118) y la Semilla tiene 1.

Tres campos quedan libres sin tocar `CellGrid`: `carga` en Planta (LabNacerPlanta la pone a 0 y nadie la lee), `carga` en roca impermeable (solo tiene semántica en agua, porosos y sedimento; CellGrid 184-185) y `reposo` en Semilla (LabPoroso 714 lo incrementa cada visita y nadie lo consume).

---

## Candidato 1 · La planta con órganos: fototropismo, copa y siega

**Resumen.** La columna de un píxel pasa a organismo de tres órganos (raíz, tallo, hoja) guardados en el byte `carga` de la celda. La punta crece hacia la luz, la raíz bebe de donde hay agua, las hojas dan sombra y transpiran, y cortar el tallo deja la raíz viva y suelta fibra seca.

**Regla.** Todo en `LabPlanta` y `LabLuzDecay`; ningún campo nuevo.
- *Órgano* = `carga[i]` ∈ {0 raíz, 1 tallo, 2 hoja}; `LabNacerPlanta` lo recibe; la germinación (691 y 709) nace raíz.
- *Fototropismo* (sustituye la tirada de 866-874): la punta elige entre `arriba−1`, `arriba`, `arriba+1` el vacío con mayor `luz[]`; empate → arriba. Argmax con orden fijo, sin RNG. `PlantaRamaPct` queda solo para bifurcar.
- *Raíz que busca el agua*: bebe del más húmedo de `abajo`, `abajo−1`, `abajo+1` si es sustrato (cuerpo de 823-831 con argmax de `hum[]`). `LabErosion` 189 protege esas tres celdas: la raíz sujeta lo que bebe.
- *Hoja*: tallo con `savia ≥ PlantaHojaSavia` y lado vacío con `luz ≥ PlantaLuzMin` nace hoja ahí (coste `PlantaCrecerSavia/2`, sal `SalLabHoja = 653`). La hoja no crece ni pasa savia; transpira a `PlantaTranspira×2` y el tallo deja de transpirar. Hoja sin savia `PlantaMarchitaVisitas` seguidas → Fibra (886-903 tal cual).
- *Copa*: `LabLuzDecay` recibe `carga[i]` y devuelve `LuzDecayHoja` (~40) para la hoja; tallo y raíz siguen a `LuzDecayPlanta` (12). Dos hojas apiladas dejan bajo la copa menos de `PlantaLuzMin`: debajo de una planta adulta no germina otra. Es la regla de espaciamiento y nadie la escribe.
- *Siega* = cincel sobre Planta (hoy `Tallable` no la incluye). Las celdas de encima pierden apoyo y ya son Fibra por 808-813; la raíz queda con `mat[arriba] == Empty` y rebrota por 861-882 tal como está. La cosecha con rebrote ya existe; falta el verbo.

**Cruces.** Luz: la copa es el primer material que apaga la luz con forma; la boca del cielo se vuelve un recurso disputado. Agua: la raíz triplica el área de bebida; la transpiración se concentra donde está la copa. Erosión: el pie sujeta tres celdas, D28 gana escala. Fuego: la fibra de siega nace seca y cae donde el jugador corte: sobre losa de terracota queda seca, sobre el lecho se moja. Abono: la hojarasca cae alrededor; el abono de 893-897 solo lo deja la raíz al morir.

**Decisiones nuevas.** Dónde poner la boca de cielo respecto al lecho (las plantas se inclinan hacia ella). Cuánta copa dejar antes de segar (más copa = más transpiración, menos germinación debajo). Segar alto (rebrota rápido, poca fibra) o a ras (mucha fibra, raíz que tarda en recargar 120 de savia). Dónde cae la fibra. Plantar en fila o en claro.

**Observabilidad.** Sin panel: la planta tiene forma; tinte por órgano en `SimRenderer.Laboratorio` (cuatro líneas); la inclinación hacia la luz es un instrumento de luz; la hojarasca marca el radio de la copa. Con visor: la vista Luz ya existente enseña la sombra de cada copa como un cono; `LabMateriales.Estado` gana «hoja a la sombra» y «raíz en seco».

**Coste.** 2 semanas. Determinismo intacto (argmax con orden fijo; una sal grep-verificada). Coste por tick: +6 lecturas por celda de planta y visita sobre <2 000 celdas: <0,01 ms; `LabLuz` lee `carga` dentro de la ventana de H5.

**Verificación headless.** Escenario «huerto de banco»: lecho de 40 columnas, boca de 20, serpentín que reparte el goteo; 36 000 ticks. Métricas: plantas vivas por tramo de 6 000 ticks, celdas de hoja, fibra seca/mojada, germinaciones bajo copa (≈ 0). Criterio de autoequilibrio: población de los últimos 24 000 ticks dentro de ±20 % de su media y distinta de 0. Los hashes cambian solo en los escenarios con plantas, por diseño.

**Tuning humano: 7.** Dos números nuevos (`LuzDecayHoja`, `PlantaHojaSavia`). **Apalancamiento: 9.** **Riesgo mayor:** una copa densa se sombrea a sí misma y recicla hojas bajas en hojarasca infinita. Es autolimitado (cada hoja cuesta 60 de savia y nace solo con luz ≥ 40); el banco mide fibra por planta y minuto, y si supera a la siega, `LuzDecayHoja` baja.

---

## Candidato 2 · Dispersión: semillas de la planta y agua que arrastra lo que flota

**Resumen.** La punta madura suelta semillas con su savia dentro, y el agua que fluye arrastra lo que flota encima (semilla, fibra, aceite). El arroyo se vuelve cinta transportadora: lleva la cosecha a la tolva y las semillas a las orillas; el sumidero se traga lo que nadie recogió.

**Regla.**
- *Semilla* (en `LabPlanta`, tras el crecimiento): punta con `aux ≥ PlantaSemillaAlto` (3), `savia ≥ PlantaSemillaSavia` (200) y tirada `SalLabSemilla = 647` con `PlantaSemillaPct`: nace `Semilla` en el primer vacío de `arriba+1, arriba−1, arriba` con `humedad = PlantaSemillaSavia/2` restada de la savia. Conservación exacta: 684-694 ya usa esa humedad como primera savia.
- *Vida*: en `LabPoroso` caso Semilla, `reposo[i] ≥ SemillaVidaVisitas` sin germinar → `Empty` y `carga[abajo] += SemillaAbono` si es sustrato. `reposo` ya cuenta (714) y `Move` 742 lo resetea: la semilla que rueda rejuvenece; la que se queda a oscuras se pudre y abona.
- *Arrastre* (costura de una línea en `ProcessLiquid` 1228 y 1236, junto a la de `LabErosion`): tras un `TryFlow` lateral de agua, `if (LabActivo) LabArrastrar(idx, nidx)`: si `mat[idx+W]` es Powder con densidad < 110 y `mat[nidx+W] == Empty`, `Move` de uno al otro. Determinista: mismo barrido secuencial, `Move` marca `touchedTick`. Sin campo de velocidad: la dirección la da el agua que acaba de moverse.

**Cruces.** Agua: el circuito manantial → sumidero transporta materia; la poza quieta (`reposo ≥ ReposoMovil`) no arrastra: el remanso es un almacén. Plantas: colonizan orillas aguas abajo; bajo copa (candidato 1) el banco de semillas espera. Fuego: la fibra que flota mucho se moja por `LabInfiltrarHacia`. Sumidero: `LabTragar` come semillas y fibra; contar la pérdida es juzgar.

**Decisiones nuevas.** Cortar el arroyo con un labio para detener lo flotante (presa de fibra). Sembrar aguas arriba y esperar, o plantar a mano. Dejar que el sumidero se lleve el sobrante o poner rejilla (la grava colmatada ya frena). Segar sobre el agua para que la cosecha viaje sola a la tolva.

**Observabilidad.** Sin panel: la fibra navega; las orillas verdes dibujan el camino del agua; la semilla que no germina queda parda y el rótulo dice por qué: `Estado()` lee las tres condiciones de 688-689 y devuelve «semilla: a oscuras», «sustrato seco», «sin sustrato». Es «la semilla que dice por qué falló» sin física nueva. Con visor: la vista Reposo separa pozas (acumulan) de agua que arrastra.

**Coste.** 1,5 semanas. Determinismo intacto. Coste por tick: una rama y dos lecturas por movimiento lateral de agua, solo en laboratorio; en «diluvio turbio» estimo +2-3 % y el banco lo mide.

**Verificación headless.** Escenario «arroyo sembrado»: nivel de referencia con 60 semillas y 60 fibras en la cabecera; 18 000 ticks. Métricas: llegadas a la poza, al sumidero, germinaciones por columna; hash. Criterio: ≥ 50 % de lo soltado sale del tramo de arrastre y ≥ 1 germinación en orilla; «diluvio turbio» dentro de +5 % de ms.

**Tuning humano: 7.** Tres números; el arrastre no tiene ninguno. **Apalancamiento: 8.** **Riesgo mayor:** la costura en `ProcessLiquid` es hot path (gateada por `LabActivo`, pero es la sexta en `SimStepper.cs` y el HANDOFF las limita); y el doble proceso de la celda arrastrada en el mismo tick si el barrido aún no la visitó: `touchedTick` lo cubre y el hash del diluvio lo delata si no.

---

## Candidato 3 · Lo húmedo y oscuro: la fibra se pudre en suelo, la roca cría musgo

**Resumen.** El hongo no es un material: es la regla «fibra mojada y a oscuras se vuelve sedimento fértil». El musgo tampoco: es un byte de cobertura en la roca impermeable que retiene el rocío. Juntos cierran el presupuesto de suelo (la erosión lo quita, la vida lo pone) y hacen visible la humedad sin panel.

**Regla.**
- *Compost* (`LabPoroso`, caso Fibra, que hoy no existe): `h ≥ FibraMojadaMin` (100) y `luz[i] < PlantaLuzMin` (40) y `reposo[i] ≥ FibraPudreVisitas` (~120) → `LabTransformar(i, Sedimento, h, FibraAbono)`. Con luz, `LabSecarHacia` (590-593) la seca y vuelve a ser combustible: la misma pila es leña o tierra según dónde se deje.
- *Musgo* (`LabRoca`, antes del `if (h == 0) return`): `carga[i]` en roca impermeable = cobertura 0-255. Sube 1 por visita con `humedad ≥ MusgoRocio` y `luz ≤ MusgoLuzMax`; baja 1 si no. Con `carga ≥ MusgoRetiene`, `LabGotear` (777) no dispara: la roca suelta por secado al aire (780-783), no por goteo. Raspar con el cincel (roca ya `Tallable`) pone `carga` a 0.
- *Meteorización*: cobertura 255 con `mat[abajo] == Empty` suelta cada `MusgoCaeVisitas` una Fibra con `humedad = h`, que a oscuras se compostará. Suelo a partir de piedra y rocío, en dos líneas.

**Cruces.** Erosión: hoy el sustrato solo se pierde (74 → 22 celdas en 300 s, R134); con compost hay fuente. Alambique: el musgo del techo amortigua el goteo que ahogaba el huerto (R135); raspar o dejar es la decisión. Vapor: pared con musgo = condensador que no gotea, humedad alta sostenida, lo que las semillas del candidato 2 y la ceniza (`Ash ≥ 128`, 659) necesitan. Fuego: la fibra a la sombra deja de ser combustible en medio minuto; guardar leña exige luz o calor (el hogar ya seca).

**Decisiones nuevas.** Dónde apilar la fibra: al sol para leña, a oscuras y mojada para tierra. Raspar el musgo del techo o dejarlo. Sembrar musgo (mojar una roca oscura) para fabricar suelo en una cámara sin sedimento. Abrir una boca de luz sobre la compostera la apaga: la luz es un interruptor.

**Observabilidad.** Sin panel: la roca se pone verde donde hay rocío y sombra: higrómetro permanente, y su borde superior marca hasta dónde llega la luz; la pila que se oscurece a pardo es el compost. Con visor: la vista Carga sobre roca muestra la cobertura.

**Coste.** 1 semana. Determinismo intacto (sin RNG). Coste por tick: una lectura de `carga` por celda de roca y visita (~12 000 por tick): 10-20 µs.

**Verificación headless.** «Compostera»: dos pilas de 10×10 de fibra sobre sedimento, una bajo boca de cielo y otra tapada, ambas regadas; 9 000 ticks. Criterio: la tapada da ≥ 80 celdas de sedimento con `carga ≥ FertilU` y la del sol 0 y sigue prendiendo. «Musgo»: el alambique de r141 con techo de roca; criterio: goteos con musgo < 50 % de los goteos sin musgo, `LabEvaporado` mayor, `LabBalanceU` cuadra al bit.

**Tuning humano: 8.** Cuatro umbrales, dos reutilizados; deciden velocidad, no diversión. **Apalancamiento: 7.** **Riesgo mayor:** que el ciclo sedimento → planta → fibra → sedimento sea ganancia neta de suelo (R55). Por construcción es mortal (cada celda nueva costó `PlantaCrecerSavia` de agua destruida, 878, y tiempo), pero el arco largo debe medir suelo total por hora y confirmar que converge.

---

## Candidato 4 · Un solo bicho: la polilla, lectora de luz

**Resumen.** Un material `Polilla` cuya única regla es «muévete al vecino vacío con más luz». No come, no se reproduce, no hay especies. Sensor vivo: se junta en los claros, entra en la llama y muere. Va cuarto porque compra poco juego por id nuevo.

**Regla.** `MaterialId.Polilla` (Count 80 → 81; el trámite de Arenisca en R131). En `LabCampos`, caso propio: argmax de `luz[]` entre los cuatro vecinos vacíos, empate en orden fijo; si ninguno supera la celda propia, tirada `SalLabPolilla = 661` para un paso al azar. Sobre Fire → Ash. Vive `aux` visitas (libre y viaja con `Move`), muere a Fibra. Nace del compost del candidato 3 con `PolillaPorMil`.

**Cruces.** Luz: la enseña. Humo: la apaga y la polilla deambula. Fuego: `EmiteLuz` marca Fire/Brasa/Hogar a 255, así que el hogar es trampa de luz. Plantas: ninguno a propósito.

**Decisiones nuevas.** Casi ninguna: encender un hogar para limpiar una cámara; leer la luz por dónde se juntan. **Observabilidad:** ella es la observación. **Coste:** 1 semana; determinismo intacto; coste por tick nulo. **Verificación:** «polillas»: dos bocas de luz y un hogar; el 90 % acaba en ellas a los 3 000 ticks; hash. **Tuning humano: 4**: la gracia depende de velocidad y cantidad, y eso se decide mirando. **Apalancamiento: 4.** **Riesgo mayor:** decorado con hash.

---

## Orden y lo que se gana en conjunto

1 → 2 → 3 → 4. Con los tres primeros la ecología mínima cierra por tres lados: agua (conservada; la transpiración vuelve por el bloque frío), luz (la copa la reparte por competencia) y suelo (la erosión quita, el compost pone, la raíz sujeta). Cada cierre tiene contador (`LabBalanceU`, `LabPlantasNacidas/Muertas`, `LabErosionado`, más `LabCompostado` y `LabSemillas`). El equilibrio no se ajusta: se mide en el arco largo, y si no aparece, el banco dice qué recurso falta antes de que nadie juegue.

## Lo que descarté y por qué

- **Raíces que ocupan celdas de sustrato.** Destruyen la `carga` (fertilidad) del sedimento que reemplazan; el argmax de bebida da la misma decisión sin tocar materia.
- **Especies, genética o biblioteca de plantas.** Contenido de autor puro; la planta con órganos ya produce formas distintas según luz y agua, que es la variedad que la simulación paga sola.
- **Flores y polinización.** Segundo agente, segundo ciclo, tuning de encuentro; la semilla desde la punta da la misma dispersión con una tirada.
- **Herbívoros que comen planta.** Presión que hay que balancear a mano contra la producción; exactamente lo que la función objetivo penaliza. Por eso la polilla no come.
- **Semillas que dispersa el viento.** Es la lente de aire y viento; si el viento existe como gravedad lateral de polvos ligeros, semilla (90) y fibra (60) lo tomarán sin una línea mía. Cruce declarado, no candidato.
- **Musgo y hongo como materiales.** Caerían, se tallarían, `Count++` y tabla de prensa; como byte en roca y como regla de compost hacen lo mismo sin existir.
- **Ciclo día-noche.** Parámetro global con riesgo de divergencia en red y un evento que balancear; la luz del cielo ya es un recurso espacial.
- **Tronco sólido, fruta o comida.** Hacer pared la planta toca dos consumidores del muñeco (R23-2) por poco juego; no hay barra de nada, por decisión: el producto es fibra y semilla, y con eso el ciclo cierra.
