# Refutación · metricas-y-soltar / cuna-geologia-por-simulacion · lente APALANCAMIENTO

**Veredicto: VIABLE CON AJUSTE.** Sobrevive la mitad barata (envejecer por simulación situaciones de autor y el test «registro vacío falla / registro del autor cumple»). Se refuta el generador que «sustituye la biblioteca de autor» y el contador de soluciones distintas.

## 1. El validador, tal como está escrito, no mide nada

- **«`HashMat` distintos = soluciones distintas».** `HashMat` es FNV-1a sobre `g.mat` entero (`LabBench.Correr`: `r.HashMat = Hash(g.mat)`) y cada perturbación es un corte de una celda en un sitio distinto: el propio corte ya está en el hash. K perturbaciones que cumplen dan K hashes distintos siempre, hagan lo que hagan: la escalera de dificultad es una tautología. Y sin ese defecto tampoco: con `XorShift.FromCell(_tick, x, y, sal)` cualquier celda distinta junto al agua cambia las tiradas de `LabErosion` aguas abajo y el hash diverge aunque el lecho sea el mismo. «Solución distinta» exige una equivalencia sobre resultados (qué salida, qué recibo): diseño que el candidato no trae.
- **Cortes de una celda no muestrean al jugador.** Las soluciones medidas son 31 celdas de `NucleoFrio` más una caldera cada 8 ticks (`MontarAlambique`), un recinto 20×20 con boca de 1 (`MontarCarbonera`), fluidos movidos con el frasco. Un corte rompe (R135: una celda de labio, 24 de 48 columnas anegadas), rara vez construye: la «fracción que cumple» mide lo fácil que es estropear la situación. El §6 lo admite («una situación sin solución conocida se publica igual»): sin (c), el validador degenera en «no trivial».
- **La condición C no tiene origen.** Tras 20 000-60 000 ticks de nacimiento el mundo está en régimen: el registro vacío produce, por definición, la etiqueta de nacimiento. Para que (a) falle, C debe pedir «más que el nacimiento» por un factor f sobre una métrica elegida. f y la métrica son el tuning escondido, y la métrica es el juego.

## 2. Sin autor, las leyes dan tres atractores

Leído en `SimStepper.Laboratorio.cs`: `LabManantial` emite eterno al vecino vacío y espera si está rodeado; toda cubeta bajo su cota se llena hasta que un sumidero (`LabTragar`, solo líquidos) tenga camino. El hogar (170 raw, eterno) prende cualquier fibra conectada, y R148 midió el resto: «el fuego se consume en los primeros cinco minutos». La terracota es una piel de una celda (D30/D31). Germinar exige la vertical de UNA boca (`LabLuz`: un solo rango `LuzCieloX0..X1` en la fila `H-2`); R148: 7 caras de 73; R150: con luz, mueren por humedad de raíz 50-99. Anegado, quemado o inerte. Los cruces que valen (alambique que ahoga, serpentín que hace sombra) los produjeron aparatos de autor; si la gramática tiene que colocarlos, la biblioteca vuelve con otro nombre y menos control.

## 3. La premisa económica está invertida

«12-15 cámaras de autor» no son caras: `MontarHorno` son 15 líneas, `MontarArcoLargo` 12, y el banco ya tiene nueve. Lo caro es juzgar si una cámara enseña algo, y el generador lo multiplica: cien cuevas que mirar en vez de doce. Técnicamente sencillo, humanamente caro: el cuadrante que la función objetivo penaliza, que además pide «situaciones pequeñas de autor donde una relación causal se aprende».

## 4. Coste real

Depende del candidato 2 (`CorrerSello`, `Volcar/Cargar`): sin volcado, cada carga re-corre 60 000 ticks. El arco largo midió 72 000 ticks en unos 3 min (2,5 ms/tick): validar = 13 × 18 000 ticks ≈ 10 min por semilla, cien semillas ≈ 21 h, en serie (`LabParams` es estático; `Universe.Create` pide el editor). La gramática impone restricciones que solo se descubren corriendo (camino al sumidero, pozo hasta la boca, fibra lejos del hogar): vueltas humanas.

## 5. Ajuste mínimo, el que sí compra apalancamiento

1. **Envejecer por simulación las situaciones de autor**: N ticks headless antes de entrar, libro de nacimiento como etiqueta, time-lapse como intro. Es `Correr` con un montaje y un volcado; poza decantada, grava colmatada y carbón enterrado salen gratis.
2. **Validador honesto**: (a) registro vacío falla C, (c) registro del autor cumple → discrimina. (b) solo como fragilidad: fracción de cortes bajo los cuales el registro del autor sigue cumpliendo (lo que R135 midió a mano). Sin contar hashes.
3. **Si hay sorteo, que sea de mutaciones de una cámara de autor** (boca ±k, veta más gruesa, hogar movido), cada mutante validado con el registro del autor: familias verificadas, no cuevas libres.

Esa versión: 1,5 semanas sobre el candidato 2, tuning 8, apalancamiento 5.

**Valores corregidos (candidato tal como está):** apalancamiento **4**; tuning **5** (gramática, métrica y factor f de C, vocabulario de perturbaciones, H y K: todo «mirando»); coste **5 semanas**, más el candidato 2 como prerrequisito.
