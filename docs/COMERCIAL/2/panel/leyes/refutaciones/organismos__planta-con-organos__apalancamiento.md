# Refutación · `planta-con-organos` (lente organismos, candidato 1) · eje: apalancamiento

**Veredicto: viable con ajuste.** El «dato corregido» es cierto (la planta muerta ya deja fibra,
811 y 899) y la siega con rebrote existe a falta del verbo. Pero el organismo con forma
—fototropismo, hoja, copa que sombrea, espaciamiento sin número— se refuta con el código que el
propio candidato cita. De cinco cruces declarados, uno es nuevo y otro medio. Vale 4, no 9.

## 1. Cruces contados contra el código

1. **Fototropismo: como está escrito, produce fibra, no inclinación.** `LabPlanta` 808-813 exige
   Planta o sustrato en `abajo`; 836 pasa savia solo a `i + W`. Una punta nacida en `arriba ± 1`
   tiene AIRE debajo y muere en su primera visita: es lo que hace HOY la tirada de rama (866-871)
   a altura ≥ 1. El argmax lo agrava: fuera de la vertical de la boca el gradiente lateral es
   8/celda, así que `luz[arriba ± 1] > luz[arriba]` siempre y cada intento cuesta 120 de savia y da
   una fibra; dentro hay empate → `arriba`, igual que hoy. Inclinar exige reescribir apoyo (808) y
   ruta de savia (836) con padre diagonal, y el candidato deja 808-813 «tal como está» porque la
   siega depende de ello. Las hojas laterales mueren por la misma línea.
2. **Copa: en este modelo de luz una hoja no sombrea.** `LabLuz` es un MÁXIMO sobre cuatro barridos
   (1230-1262) con el decaimiento cobrado en la celda receptora: el aire bajo una hoja recibe
   `max(luz_hoja − 1, lateral − 8)` = haz − 8, valga `LuzDecayHoja` 40 o 255. Dejar 40 bajo 211
   pide ~43 columnas de copa continua; el tope es 14 × 3. «Dos hojas apiladas dejan < PlantaLuzMin»
   es falso, y «se baja LuzDecayHoja» es tuning sobre un número que no actúa. Además la luz solo
   gatea CRECER (861-874); morir es por savia (886-903): a la sombra la planta deja de subir, no
   muere. Espaciamiento sin número: cero.
3. **Raíz de tres celdas: medio cruce, el único que toca el bloqueo real.** No «triplica el área»:
   bebe `PlantaBebe` de UNA celda por visita (827-830). Pero elegir la más húmeda suma los márgenes
   sobre 60 de tres caras, y R150 midió humedad de cara 50-99 con mínimo 60 («el goteo moja
   columnas, no lechos»).
4. **Erosión: ya existe (189).** De una celda a tres es una constante.
5. **Fibra de siega «seca»: falso por `Cincel.cs` 470-476.** `PaintLab` nace el producto con
   `humedad[idxAqui]` = la savia: una celda «con savia» (≥ 120) da fibra MOJADA sobre
   `FibraMojadaMin` 100; solo las de encima, que caen por 811, nacen a 0. Es mejor que lo escrito:
   segar una planta jugosa obliga a secar junto al hogar, y ese sí es un cruce nuevo (siega ×
   secado × fuego) que sale gratis. `LabTransformar` 255 audita la savia destruida.
6. **Abono: sin cambio**, lo admite el candidato.

Cruces nuevos: **uno y medio**. Decisiones nuevas: segar alto o a ras, y poco más. «Dónde poner la
boca» ya lo decide R134(c); «cuánta copa» y «fila o claro» no existen sin sombra.

## 2. Lo que la regla calla

- **El banco pide un huerto que nadie ha logrado.** «Huerto de banco» con serpentín que reparte el
  goteo y criterio «población ±20 % y ≠ 0» es la aceptación de H4 que R134, R148 y R150 fallaron
  tres veces por geometría (boca de 7 sobre 73; riego por columnas) y que R150 difirió a la fase
  comercial. Los órganos no tocan ni la luz de la cara ni el reparto del riego (salvo la raíz de
  tres). Ese es el coste incomprimible bajo «2 semanas».
- Con padre diagonal, el orden de visita decide la cascada de muerte al segar (D18, R55).
- El coste de hoja (`PlantaCrecerSavia/2`) no debita `LabBalanceU` (878): residuo ≠ 0.

## 3. Versión mínima con más apalancamiento (el ajuste)

Sin órganos, sin `carga`, sin tocar `LabLuz`:

1. **Siega**: `Planta` en `Tallable` y `ProductoDeTalla` → Fibra; la celda cortada hereda la savia
   (mojada, a secar), las de encima caen secas por 811, la raíz rebrota por 861-882. Cero números.
2. **Raíz de tres**: argmax de `hum[]` entre `abajo`, `abajo ± 1` si es sustrato (823-831), las
   tres protegidas en 189. Cero números.
3. **Renderer**: dos líneas (raíz por `aux == 0`).

Banco: «huerto iluminado» (R150) con y sin raíz de tres (plantas vivas, humedad de cara); siega a
ras y a media altura (fibra mojada/seca, savia destruida = `LabBalanceU`, ticks de rebrote);
hashes intactos sin plantas. Media semana; tuning 9; apalancamiento 4. Si Cesar quiere copa, la
ley no es una hoja: es luz como flujo, y eso es la lente de luz.
