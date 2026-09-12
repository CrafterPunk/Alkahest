# Refutación de ingeniería · `luz-optica` / `haz-y-espejo`

**Veredicto: viable con ajuste.** El motor aguanta la regla; lo que no aguanta es su cruce estrella ni su banco de aceptación, y ambos se corrigen sin tocarla.

## Lo que aguanta

El haz es un paseo de rayo entero, en orden fijo por columna, sin RNG y sin despertar chunks; `luz` sigue posicional y la lee `LabCampos` (`SimStepper.Laboratorio.cs:201-235`), que visita toda la grilla: el determinismo no se toca. Y compra algo hoy imposible por construcción: `LabLuz` hace UN barrido descendente, uno ascendente y luego los horizontales (l. 1230-1266), reseteando a 0 en cada pasada, así que la luz solo dibuja una L (bajar y de lado). Un periscopio —bajar, girar, bajar— no existe en el motor, y es justo el negativo real de R148: el serpentín de y272 (x105-135) tapa la boca (x118-124) y la cámara alta queda a 0. El haz con dos cuñas es la primera regla capaz de rodear esa sombra.

## Lo que está mal

1. **La métrica mide una celda que ninguna regla lee.** `LuzCarasLecho` cuenta la luz del sedimento (la «cara», y249). La germinación lee `luz[i + W]`, el aire de encima (l. 704), y el crecimiento `luz[a]`/`luz[arriba]` (l. 870-873). En el nivel limpio ese aire ya recibe 219 al pie del pozo menos 8 por columna: 75 en x100, 131 en x135, todo sobre `PlantaLuzMin` 40. El «7 de 73» es un artefacto de medir el sólido. La cuña «al pie del pozo» sube ese aire a ~200 sin cruzar ningún umbral, porque la planta es umbral y no proporcional (§6 del propio documento): el banco propuesto pasaría sin cambiar un nacimiento.

2. **Donde sí falta luz, la cuña propuesta no llega.** En `alambique de r141` y `arco largo` el rayo muere en el núcleo frío de y272. Lo que lo salva es el periscopio: galería tallada sobre el techo, cuña en la boca, segunda cuña bajando por x100-104, fuera del serpentín. Obra grande, no «8 celdas».

3. **La cuña de 8 celdas no es cuña.** Una diagonal de una celda de grosor tiene ambos laterales abiertos: con la regla escrita es LÁMINA y atraviesa. Gira solo un triángulo relleno. La fuente natural es la pila vitrificada, con DOS laderas que reparten el haz a este y oeste; eso es lo que daría ≥30 de 36. El cincel talla discos de radio 2 (`Cincel.cs:132-133`): no hay faceta de una celda. El hielo es ventana en la práctica: el agua no reposa en pendiente, el vidrio vertido cae recto (`ProcessSolidoCohesion`) y a 64 raw de ambiente se funde (`meltsAt` 5 °C, `Universe.Laboratorio.cs:213`) salvo pegado al frío.

4. **«Todo medio resta ≥1» no es verdad hoy.** `luz.decayCielo` se registra con mínimo 0 (`LabParams.cs:291`): un rayo entre cuatro cuñas no muere, bucle infinito dentro del tick. Clamp ≥1 o tope de 255 pasos.

5. **Los hashes se mueven en los nueve escenarios**, no solo en `laboratorio base`: pasar el descendente de `dCielo` a `dAire` altera la luz de todo hogar, y los nueve tienen hogar. En alambique y arco largo también `HashMat`/`HashHumedad`, porque la luz decide nacimientos. Rebase completa del banco.

6. **La difusa crece con el haz.** La ventana `bx0..bx1` se estira con el alcance horizontal del rayo: un corredor de 255 celdas la lleva de ~73 a ~320 columnas, ~1,2 ms (H5: 2,86 ms por 768). Pico 3,1 → ~4,3 ms uno de cada 16 ticks. Cabe, pero `mundo entero despierto` no lo mide: no tiene boca.

## Ajuste mínimo

Haz y cuña como están, con clamp ≥1. Métrica: luz del AIRE sobre la cara y `LabPlantasNacidas`. Escenario `periscopio de r148`: arco largo con el serpentín tapando, galería y dos triángulos de vidrio de cuatro filas; aceptación: aire sobre x100-117 ≥ 40 y nacidas ≥ 4× las 2 de R148 a 13 500 ticks. Más un `corredor de 255` para la ventana. Vidrio tallable → grava se queda; «faceta» se declara talud vitrificado. Hielo: ventana, no espejo. Cruce no visto: las plantas que crecen dentro del haz restan 12 por celda y apagan el fondo del huerto; podar es decisión.

## Números corregidos

Apalancamiento 7: el umbral de la planta le quita a «más luz» todo valor; queda el enrutado donde hoy hay 0, el uso del vidrio y el invernadero. Tuning 8. Coste 2 semanas con periscopio y rebase.
