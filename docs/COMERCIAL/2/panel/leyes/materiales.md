# Materiales con propiedades continuas y memoria — cuatro candidatos

*(Panel de leyes, segunda pasada. Lente: porosidad, conductividad, cocción, vidrio, mezclas, choque térmico, materiales que recuerdan. Leído: `SimStepper.Laboratorio.cs` entero, `LabParams.cs`, `LabMateriales.cs`, `CellGrid.cs`, `LabBench.cs`, `Universe.Laboratorio.cs`, y de `SimStepper.cs` `ApplyPhase`, `ProcessCombustion`, `DiffuseTemperature`, `ProcessSolidoCohesion`.)*

## Lo que el código dice antes que yo

1. **El vidrio del laboratorio es un sólido mudo.** `VidrioVerde` nace en el caso `Sand` de `LabPoroso` y después **no tiene caso en el `switch` de `LabCampos`**: no suda, no está en `LabEsSuperficieCondensable`, `LabLuzDesde` devuelve 0 para él y `LabK`/`LabC` lo mandan al `default` (polvo). Todo lo que el horno produce hoy es una pared verde.
2. **Hay dos bytes libres.** `carga` solo significa algo en agua, porosos y sedimento; en `Stone`, `Terracota`, `PisoEstructural`, `RocaSuelta` y `VidrioVerde` nadie lo toca. `reposo` lo usan agua, porosos, arena y planta; `LabRoca` no. Dos memorias de 8 bits gratis, ya en `SwapCells`, limpias al nacer (`SetCell`), ya en los hashes (`HashCarga`, `HashReposo`). No hace falta un array `huella`.
3. **Discrepancia con el sustrato:** el brief dice que la luz «no tiene emisores»; en el código `LabLuz` pone `luz = 255` en `Fire`, `Brasa` y `Hogar` (`LabMateriales.EmiteLuz`). Diseño con el cielo como fuente principal; si el hogar ilumina, los candidatos 1 y 3 ganan.

| # | candidato | apalanc. | tuning (10 = ninguno) | semanas | determinismo | coste/tick |
|---|---|---|---|---|---|---|
| 1 | Vidrio que deja pasar la luz y suda | 9 | 8 | 0,5 | intacto | ~0 |
| 2 | Choque térmico con memoria de cocción | 8 | 8 | 1 | una sal nueva | ~0 |
| 3 | Hollín y marca de marea | 7 | 7 | 1 | intacto | +0,03-0,08 ms |
| 4 | El poroso mojado conduce, el seco aísla | 6 | 9 | 0,5 | intacto | +10-15 % de `MsDifusion` |

---

## 1. Vidrio que deja pasar la luz y suda (el invernadero)

**Regla.** Cinco líneas en sitios que ya existen:
- `LabLuzDesde`: `VidrioVerde` transmite (junto a aire, agua, planta, gases).
- `LabLuzDecay`: `case VidrioVerde: return LabParams.LuzDecayVidrio` (nuevo, default 6).
- `LabMateriales.EsRocaImpermeable` incluye `VidrioVerde`: con eso entra solo en `LabEsSuperficieCondensable` y en el `LabGotear` de `LabAire` (vidrio frío = ventana que suda y gotea).
- `LabCampos`: `case VidrioVerde: LabRoca(x, y, i)`.
- `LabK`/`LabC`: `KRoca`/`CRoca` (aísla como la roca, no como un polvo).
- Render (`LabTinte`): alfa ~130 para ver a través.

Fuera de la física: `EsSolidoDelMundo` excluye el vidrio desde R145 porque rompía la campaña; en el laboratorio se gatea por `ModoLaboratorio` en los dos consumidores del muñeco, como deja escrito ese comentario.

**Cruces.**
- *Luz:* el único sólido transparente. El huerto nunca vivió porque una boca ilumina 7 de 73 caras y con boca ancha el goteo lo ahoga (R135, R148). Con vidrio, luz y agua se separan: el techo deja pasar el cielo y desvía el goteo a un canal lateral. El cruce «el alambique riega y oscurece» tiene respuesta que el jugador CONSTRUYE.
- *Humo:* el gas solo entra en `Empty`. Un techo de vidrio es la única forma de tener el cielo Y aislarse del humo: la cadena de H7s (humo → oscuridad → marchitez) se vuelve decisión de arquitectura.
- *Vapor:* la condensación va al vecino condensable MÁS FRÍO (`LabAire`). Un techo de vidrio que toca la cámara alta (8 °C por `Clima`) es más frío que el aire de dentro: la transpiración (`PlantaTranspira`) sube, condensa en el vidrio y GOTEA al lecho. Con roca ya pasaba, pero a oscuras. Una cámara sellada con luz recircula su agua: «Un Año Después» tiene base física sin regla dedicada.
- *Fuego:* la K de roca deja pasar calor del hogar a través del vidrio, sin humo al otro lado.
- *Cuerpos:* cohesión 3 (`Universe.cs`): pilares cada ≤ 6 celdas, y cada pilar da sombra (8 por celda de lado). El invernadero es un problema real de luz y apoyo.

**Decisiones nuevas.** Techo, muro o ventana de horno; techo con goteo desviado vs boca abierta; hacer el vidrio en el horno y mudarlo a la cámara alta; sellar (recircula agua) o ventilar (no acumula humo si algo arde); grosor contra apoyos.

**Observación.** Sin panel: se ve a través; las plantas detrás del vidrio SON el fotómetro; el vidrio frío se ve sudado (el oscurecimiento por `humedad` que `LabTinte` ya usa en piedra) y gotea. Con visor: la vista `Luz` de F8.

**Coste.** 0,5 semanas con escenario de banco. Sin RNG. Coste por tick nulo: la ventana de `LabLuz` ya barre esas celdas.

**Banco.** Escenario `invernadero`: nivel de referencia + techo de vidrio de dos celdas sobre el lecho oeste (y≈262, pilares cada 6) + el fuego de la sala de `MontarArcoLargo` para que llegue humo. Asertos: `luz` bajo el vidrio ≥ `PlantaLuzMin` tras el primer `LabLuz`; plantas vivas a 9000 ticks > las del mismo montaje sin vidrio (invierte R148); `LabGoteos` desde vidrio ≥ 1 con techo sellado. De los 9 hashes solo cambia `HashLuz` de «horno con yesca» (ahí nace vidrio); los demás intactos, y eso es aserto de regresión.

**Tuning.** Un número (`luz.decayVidrio`), fijado por cuenta: «dos celdas de vidrio bajo la boca (255) dejan ≥ 40 al lecho». 8/10.

**Riesgo mayor.** Que el vidrio sea difícil de MOVER (el horno lo produce en su sitio; mudarlo depende de verbos heredados) y el invernadero quede en promesa. Mitigación: aceptar que el vidrio se talle en casco que vuelve al horno, o medir la mudanza en banco.

---

## 2. Choque térmico con memoria de cocción

**Regla.** Nuevo `LabCocido(x, y, i, m)` desde `LabCampos`, tras `LabRoca`, para `Terracota` y `VidrioVerde`:
1. *Memoria:* `if (temp[i] > reposo[i]) reposo[i] = temp[i]`. En lo cocido `reposo` = temperatura máxima desde que nació (`LabTransformar` lo deja a 0; la primera visita lo fija a ≥ `TerracotaRaw`). La terracota pasa de termómetro de 1 bit a termómetro de máxima de 8 bits, legible por `LabMateriales.Estado` («cocida a 220 °C»; añadir `reposo` a su firma).
2. *Choque:* si `temp[i] ≥ ChoqueRaw` (130) y un vecino ortogonal es `Water` con `temp[j] + umbral ≤ temp[i]`, donde `umbral = ChoqueDelta + (reposo[i] − TerracotaRaw) · ChoqueTemplePct / 100` (60 + la mitad del exceso de cocción), con `ChoquePct` por visita (sal 647, `XorShift.FromCell`): `Terracota → Grava` (lo que ya devuelve `ProductoDeTalla`), `VidrioVerde → Sand` (casco: vuelve al horno). La grava cae y la pared se abre. Temperatura y agua se conservan. Contador `LabChoques`.

Los números salen de los que hay: una olla cocida en la cara del hogar (`reposo` 150-170) con agua a 70 tiene Δ 80-100 ≥ 60 → se raja; cocida en horno a 220 necesita Δ ≥ 95 → aguanta agua fría a 150 y agua hirviendo siempre (110 vs 170). Cocer en el horno deja de servir solo para el vidrio.

**Cruces.**
- *Agua × fuego:* el goteo del alambique sobre un horno encendido raja la bóveda; la grava cae; entra aire (`LabRespira` pasa de sordina a llama) o escapa el calor (`VidrioVisitas` se REINICIA por el caso `Sand`). Dos finales por una gota, ninguno programado.
- *Orden:* una tubería de terracota con agua de la caldera no se raja (se calienta con el agua); calentada vacía y llenada de golpe, sí. «Agua antes de fuego» es lo que preparar → comprometerse → soltar pone a prueba.
- *Vidrio (cand. 1):* el techo junto a la salida de la chimenea se calienta con el humo; la primera gota fría lo revienta.
- *Demolición:* apagar un horno a cubos abre la pared sin cincel; conservarlo exige esperar. Ruinas amables sin autor.
- *Cuerpos:* cuando `LabCuerpos` exista, `RocaSuelta` entra con umbral alto.

**Decisiones nuevas.** Cocer en horno o en hogar según el uso; agua primero o fuego primero; por dónde pasa el goteo; dejar enfriar antes de abrir; usar el choque a propósito (el nivel que sube hasta la pared caliente como temporizador).

**Observación.** Sin panel: la pared caliente que recibe agua se deshace en grava con vapor (la evaporación de `LabAgua` ya lo hace por temperatura): clip. Con visor: `Temperatura`, y `Reposo` (rampa violeta existente) pinta la máxima en lo cocido.

**Coste.** 1 semana con banco. Una sal nueva, orden fijo. Cuatro lecturas por visita de celda cocida.

**Banco.** Escenario `olla`, tres montajes: (a) caja de terracota 5×4 sobre el hogar con agua desde el tick 0 → `LabChoques == 0` en 9000 ticks; (b) la caja vacía 3000 ticks y luego el banco la llena a 70 (intervención como la caldera del alambique) → `LabChoques ≥ 1` en < 600 ticks; (c) como (b) con `reposo` sembrado a 220 → 0. Regresión: los 9 hashes intactos (ningún escenario tiene cocido caliente junto a agua fría; si el arco largo lo tiene, el banco lo dirá).

**Tuning.** Cuatro parámetros que (a)/(b)/(c) fijan por cálculo. 8/10.

**Riesgo mayor.** Una olla sobre el hogar que se raja «porque sí» al hervir. `ChoqueRaw` 130 + Δ 60 excluye agua hirviendo (110) contra terracota de hogar (170); el montaje (a) es el guardián.

---

## 3. Hollín y marca de marea: la huella que la luz lee y el agua lava

**Regla.** `carga` en sólidos impermeables = finos depositados en la superficie. Todo en `LabRoca` (que hoy sale en `h == 0`):
- *Fuente humo/llama:* por visita `carga += nHumo · HollinHumo + nLlama · HollinLlama` (2 y 6, tope 255), contando `Smoke` y `Fire`/`Brasa` entre los cuatro vecinos. El humo no lleva masa hoy (`SpawnSmokeNear` → `Transform`): el hollín nace del contacto, como el rocío del vapor; si el auditor lo pide, `Smoke.carga` es un byte que ya viaja en `SwapCells`.
- *Fuente marea:* en la salida `vol <= 0` de `LabAgua`, la `carga` del agua que se fue HOY SE PIERDE (`LabTransformar(i, Empty, 0, 0)`). Pasa al vecino de abajo si `EsFondo`: una poza turbia que se seca deja su anillo. La conservación de finos mejora.
- *Lavado:* por cada vecino `Water`, `t = min(carga[i], HollinLavado, 255 − carga[j])`; `carga[i] −= t; carga[j] += t`. El hollín lavado es turbidez: decanta (`Decantacion`), deposita (`DepositoUmbral`). Humo → hollín → agua turbia → sedimento, un ciclo que nadie escribió.
- *Lector luz:* `LabLuzDecay` para `VidrioVerde` = `LuzDecayVidrio + carga · LuzDecayHollin / 255` (24: vidrio negro = humo). El invernadero se ensucia con la chimenea y hay que LAVARLO: el goteo del alambique, que era el problema, pasa a ser el limpiacristales.
- *Lector render:* `LabTinte` oscurece piedra, terracota y vidrio por `carga`. La pátina de `SimRenderer.cs` es solo dibujo y el stepper no puede leerla por contrato; esto la sustituye en el laboratorio con un valor determinista, en hash y replicable.
- *Lector rótulo:* `Estado` ya recibe `carga`: «tiznada», «con marca de marea».
- *Fase 2, con flag:* `hollin.arde`: con `carga ≥ 200` y llama vecina la pared suelta su hollín como calor y lengua (`carga = 0`, `LabInyectar`). El fuego de chimenea se autolimita porque consume la memoria.

**Cruces.** Humo × luz × vidrio (cand. 1); agua × luz (lavar es iluminar); sedimento (el hollín lavado es carga); fuego × arquitectura (el tiro escribe su historia en la pared; una bóveda negra = ese fuego no respiraba).

**Decisiones nuevas.** Dónde desemboca la chimenea respecto al vidrio; limpiar con un goteo o aceptar la penumbra; diagnosticar por el tizne; leer hasta dónde llegó el agua sin panel.

**Observación.** Sin panel: la pared se tizna y la ventana se ennegrece; hollín en un techo lo entiende cualquiera. Con visor: vista `Carga` (ámbar).

**Coste.** 1 semana. Transferencias enteras en orden fijo, sin RNG. `LabRoca` pasa a leer 4 vecinos en cada visita de roca (la mayoría del mundo): +0,03-0,08 ms (27 600 visitas/tick, ~60 % roca). Guarda: `if (carga[i] == 0 && !IsChunkAwake) return` (humo y llama despiertan chunks; el lavado por agua quieta sigue porque `carga > 0`). Lo miden «laboratorio base» y «mundo entero despierto».

**Banco.** «carbonera 20×20 boca 1»: `Σcarga` en la roca del recinto > 0 y máxima en la columna de la boca. Variante del invernadero: `luz` media bajo el vidrio decrece monótona con `LabHollinDepositado`; con un goteo sobre el vidrio, `Σcarga(vidrio)` baja y la `carga` del agua debajo sube lo mismo (identidad, como `LabBalanceU`). `HashCarga` cambia en todo escenario con humo; `HashMat` puede cambiar en «arco largo» si el hollín lavado deposita: esperado.

**Tuning.** Tres tasas y una caída. Las relaciones (monótonas, conservativas) las fija el banco; el «cuánto de rápido» es gusto. 7/10.

**Riesgo mayor.** Ruido visual: todo tiznado en diez minutos. Con `HollinHumo` 2, una bolsa de humo de 255 ticks deja ~60 de 255; solo un fuego ahogado durante minutos ennegrece.

---

## 4. El poroso mojado conduce, el seco aísla

**Regla.** `LabK`/`LabC` reciben el índice: para `EsPoroso(m)`, `k = KPolvo + (KAgua − KPolvo) · humedad[i] / 255`, `c = CPolvo + (CAgua − CPolvo) · humedad[i] / 255`. Cero parámetros nuevos; `LabFlujoTermico` sigue con `min(ki, kj)`. Opcional `termica.humedadConduce` 0/1 para el A/B.

**Cruces.** *Agua × fuego, tercera vía:* el goteo que moja los muros de arena o ceniza del horno los vuelve conductores, y además evaporan (`LabLatente` ya enfría): el horno suda calor y no llega a `VidrioRaw`. Con el candidato 2, una causa y dos modos de fallo, uno lento y uno súbito. *Secar como preparar:* la arena del montículo nace húmeda; construir el horno con arena seca es la primera decisión con tiempo. *Suelo radiante:* arena mojada bajo el hogar reparte calor al lecho, sube `Saturacion` del aire y cambia dónde condensa. *Carbón mojado:* ya no prende (`FibraMojadaMin`); ahora además roba calor al horno.

**Decisiones nuevas.** Curar materiales antes de construir; aislar el horno con roca o polvo seco; mantener el agua lejos de los muros o usarla para enfriar.

**Observación.** Sin panel: un muro mojado humea y el horno no vidria. Con visor: `Temperatura`.

**Coste.** 0,5 semanas. +5 lecturas de `humedad` por celda difundida (1/8 del mundo por tick): +10-15 % de `MsDifusion`.

**Banco.** `MontarHorno` con muros de arena secos vs `humedad = 200` → `LabVidrio` 18 vs < 18. Cambian `HashTemp`/`HashHumedad` donde hay poroso húmedo: línea base nueva en una corrida.

**Tuning.** 9/10. **Riesgo mayor.** Que con `KPolvo` 3 y `KAgua` 8 el efecto sea invisible: si el banco no separa 18 de < 18, no vale la semana. Es la prueba más barata de matar del documento.

---

## Orden y por qué

El 1 es el mayor apalancamiento por línea del sustrato: un producto sin uso se vuelve el único material que separa luz de agua y de humo, y da base física a la cámara sellada. El 2 hace del horno algo más que una fábrica de vidrio y crea el primer accidente legible agua-fuego. El 3 es memoria que las leyes leen, no dibujo, y cierra un ciclo con el sedimento. El 4 es el más barato y el más fácil de matar; correrlo primero cuesta un día.

## Lo que descarté y por qué

- **Un campo `huella` nuevo.** `carga` en impermeables y `reposo` en cocidos cubren hollín, marea y máxima sin array ni hash nuevo.
- **Cal, mortero, sal como material, metal.** Cada uno pide material nuevo (caliza, mena) y cadena propia (`Universe.cs` ya tiene `Mortero`, `Clinker`, `Lejia` para la campaña). Es biblioteca de contenido con recetas que balancear, y no cruza agua-fuego-luz más que la ceniza. La sal sobrevive como marca de marea, lo único de ella que produce decisión.
- **Vidrio de calidad continua** (más caliente = más claro, desde `reposo` al nacer). Segundo dial sobre el candidato 1 antes de saber si el primero se usa. Tres líneas, después.
- **Dilatación.** Sin geometría subcelda no tiene representación honesta.
- **Carbón granular por gravedad y ceniza fundente/abono.** Ya existen (`Carbon` es `Powder`; «tolva de fibra» arde 466 s; `Ash` condiciona el vidrio y abona mojada). Lo que le falta al fuego es tiro, y es otra lente.
- **Hollín que arde como núcleo.** Fase 2 con flag: es clip, pero añade propagación a un fuego ya inmortal sobre combustible. Primero el tiro.
- **Promover la pátina del render al stepper.** Es el candidato 3 con peor contrato. Se sustituye.
- **Hielo que flota o aísla.** Ya tiene `KRoca`/`CAgua` y no cae en líquido; no es propiedad continua ni mezcla. Es de la lente del frío.
