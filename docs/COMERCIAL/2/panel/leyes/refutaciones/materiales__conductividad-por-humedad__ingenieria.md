# Refutación de ingeniería — «El poroso mojado conduce, el seco aísla» (materiales, candidato 4)

**Veredicto: viable con ajuste.** Técnicamente es limpio: sin RNG, sin campo nuevo, cabe en el tick y se mide en banco. Lo que no aguanta es la PREMISA: leído `LabFlujoTermico`, el «factor 2,7» no existe donde importa, el estado «mojado y caliente» dura segundos, y la palanca que sí mueve `LabVidrio` es la capacidad, no la conductividad, y empuja al revés del relato.

## 1. `min(ki, kj)` mata la conductividad antes de nacer

`SimStepper.Laboratorio.cs:1349-1350`: `k = ki < kj ? ki : kj`. Un poroso mojado (k = 8) intercambia con aire a `min(8, 4) = 4` (hoy 3: ×1,33), con llama o gas a 6 (×2), con roca, hogar, terracota, arcilla o núcleo frío a 2 (nada). El ×2,7 solo ocurre entre dos porosos mojados, dentro del muro. El calor que sale de un horno por un muro mojado tiene el cuello en las caras contra aire: +33 %. El «suelo radiante» bajo el hogar es `KRoca = 2` (`:1364`): no cambia un raw. «Carbón mojado que roba calor» es redundante: ya no prende por `FibraMojadaMin` (`:164`).

## 2. El muro mojado junto al horno seca en 2-3 segundos

`LabSecarHacia` (`:731-749`) toma la saturación del AIRE vecino (`temp[j]`) y la tasa por la temperatura del muro: a 200 raw, `Saturacion` da 255 y `rate = 3·(16+130)/16 ≈ 27 u/visita` (≈ 21 tras déficit/sat). Una celda a humedad 200 seca en ~10 visitas = 80 ticks. La cara interior del horno, que la refutación anterior daba por «no seca», es la que antes seca. `VidrioVisitas` pide 60 visitas sostenidas: el estado desaparece antes de contar. El latente es `Latente = 4` raw por 255 u (`LabLatente`, `:281-296`): secar la celda entera enfría 3 raw una vez. «El horno suda calor» no está en el código.

## 3. La palanca real es `c`, y va al revés

`step = flujo / (64·ci)` (`:1333`). Mojado, `CPolvo 2 → CAgua 4`: la celda tarda el doble en calentarse. Eso SÍ movería `LabVidrio` si se moja la CARGA de arena: llega tarde a `VidrioRaw`. Pero es inercia, no fuga: el relato «conduce» es falso y el relato «tarda» es el que vale. Mojar el carbón lo confunde con `FibraMojadaMin`: el A/B debe mojar solo las dos columnas de `Sand`.

## 4. Errores de especificación

- `EsPoroso` (`LabMateriales.cs:89`) incluye **Arcilla** y **Arenisca**, que hoy conducen a 2 y guardan como roca (`CRoca`). Interpolar desde `KPolvo` las sube EN SECO a 3 y les baja `c`: mueve la veta de arcilla (nace a 160, `SimLevelBuilder.Laboratorio.cs:82-83`) y la fisura de arenisca de H1 sin decisión. Obligatorio: `k = k0 + (KAgua − k0)·h/255` con `k0 = LabK(m)`.
- «La arena del montículo nace húmeda»: el cono se pinta con `SetCell` (`:79-80`), humedad 0. «Secar como preparar» exige sembrar humedad en el nivel: decisión aparte.
- El banco propuesto no existe: `MontarHorno` (`LabBench.cs:116-133`) tiene muros de `Stone`; la arena es carga interior.

## 5. Lo que sí aguanta

Determinismo: `LabDifusionTermica` corre primero en `Step` (`SimStepper.cs:269`), lee la humedad del tick anterior, entera, sin sal nueva; la contracción a `[dMin, dMax]` no depende de k ni de c. Coste: difusión = 0,62-0,66 ms en todo escenario (R142), la única fase que no duerme; +5 lecturas en 27 648 visitas/tick ≈ +0,05-0,10 ms (+3-6 % del tick). Se mide en «laboratorio base» y «mundo entero despierto».

## Versión mínima y banco

1. `LabK(m, h)`/`LabC(m, h)` desde el valor seco del propio material; `termica.humedadConduce` default 0 hasta el kill test.
2. Escenario `horno con carga húmeda`: `MontarHorno` con `humedad = 200` SOLO en las columnas de arena. Aserto: primer `LabVidrio` ≥ 1,5× más tarde que en seco, o 18 → < 18 a 9 000 ticks. Si iguala, refutado en un día.
3. Escenario `arenisca remojada`: recinto de `Arenisca` con el banco reponiendo 255 en la cara exterior cada 8 ticks (como la caldera del alambique): seco 18, mojado < 18. Es el único caso donde «conduce» es verdad (2 → 8 contra agua).
4. Si se quiere «conduce» de verdad: media armónica en vez de `min` en `LabFlujoTermico`. Una línea, pero mueve los nueve hashes y la calibración hogar 0 / llama 1-2 / horno 18 de `VidrioRaw`. Es otro candidato (1 semana + rebalance humano).

**Valores corregidos.** Apalancamiento 6 → **3** (un cruce real: carga y arenisca mojadas tardan; el resto lo acotan `min` y el secado). Tuning 9 → **8**: cero parámetros, pero el efecto es una carrera entre `Secado`, `Saturacion` y el caudal del goteo. Coste 0,5 → **0,75** semanas: dos días de código, dos montajes y línea base nueva de tres hashes.
