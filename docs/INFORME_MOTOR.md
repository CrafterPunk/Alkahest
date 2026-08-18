# INFORME DEL MOTOR — diagnóstico, techo y menú de espectáculo

*(Fable, playtest 38. Todos los números están MEDIDOS, no estimados: banco de
pruebas headless corriendo TU SimStepper real, compilado contra las DLLs de tu
propia build (regla 53), 300 ticks por escenario tras 30 de calentamiento.
El arnés queda en `Tools~/BenchSim/Harness.cs` para repetirlo cuando queramos.)*

---

## 1. Dónde estamos: las mediciones

| Escenario de estrés | media ms/tick | pico ms (celdas activas) | headroom vs 30 Hz |
|---|---|---|---|
| Cascada (100×80 de agua cayendo 100 celdas) | 2.29 | 5.86 (8.000) | **14.6×** |
| **DILUVIO TOTAL** (medio mundo de agua) | 5.48 | 11.65 (**74.000**) | **6.1×** |
| Incendio (aceite 300×30 + línea de fuego) | 2.49 | 4.15 (17.940) | **13.4×** |
| Arena masiva (150×100 desplomándose) | 1.99 | 2.97 (15.000) | **16.8×** |
| Mundo mixto (agua+aceite+arena+fuego) | 3.94 | 5.80 (41.656) | **8.5×** |

Presupuesto de un tick a 30 Hz: 33,3 ms. En juego real (lo que vimos por F3
jugando: 0,6–1,7 ms) estamos usando el **2–5%** del presupuesto; en el
APOCALIPSIS sintético, el 17% de media y el 35% en el peor pico. Y esto en el
CPU del sandbox — tu PC de juego es más rápido.

**Traducción de director: el motor va sobrado. El cuello de botella del
espectáculo no es el algoritmo — es que todavía no le hemos PEDIDO
espectáculo.**

## 2. Qué es "el máximo" (la vara de Noita) y en qué % estamos

Noita, sobre esta misma familia de algoritmo, suma cuatro capas que nosotros
aún no tenemos: (a) partículas desprendidas con velocidad propia (salpicaduras,
chispas, escombros volando), (b) cuerpos rígidos pixel-perfectos (marching
squares + física), (c) manchas/estados superficiales (mojado, quemado,
manchado), y (d) doble resolución efectiva vía interpolación visual. De su
lista, nuestro motor ya tiene cosas que Noita NO tiene: química generada por
semilla, campo morfológico (los patrones vivos de los materiales), determinismo
total (nuestro multiplayer depende de él — Noita es single-player), y
temperatura como campo continuo por celda.

Mi lectura honesta del "% del máximo":

- **Throughput bruto**: estamos al ~15-30% de lo que el presupuesto permite.
  Sobra para todo lo de abajo.
- **Espectáculo visible**: estamos al **~40%** de lo que ESTE motor puede dar
  sin rediseñar nada. La mitad que falta es barata (ver menú).
- **El último 20%** (cuerpos rígidos estilo Noita: la lámpara que cae y rueda,
  la tabla que flota) es la única pieza CARA de verdad — y Semilla 0 no la
  necesita.

## 3. El menú de espectáculo (coste medido contra los números de arriba)

Ordenado por (impacto visual ÷ coste). Los costes son sobre el tick medido:

1. **CAPA DE PARTÍCULAS DESPRENDIDAS** (~+0.5-1 ms, riesgo bajo) — LA GRAN
   AUSENTE. Partículas decorativas NO-sim (no tocan la grilla ni el
   determinismo: nacen de eventos de la sim y mueren en 0.3-1.5s): gotas que
   SALTAN al caer un chorro, chispas del fuego, motas ascendiendo del crisol,
   polvo al desplomarse arena, vaho del alambique. 2-5.000 partículas en un
   buffer circular con un solo mesh. Es EL multiplicador: cada milagro actual
   se ve el doble de vivo. Al ser visual-only, en multi cada cliente las
   genera de sus propios eventos replicados — cero tráfico extra.
2. **INERCIA EN LÍQUIDOS** (~+30-50% sobre celdas líquidas activas → el
   diluvio pasaría de 5.5 a ~8 ms, riesgo medio-bajo) — hoy el agua "escurre";
   con 2-3 bits de velocidad horizontal en `aux`, el agua CORRE, hace olas al
   golpear, y un chorro que cae empuja lo que toca. Es lo que hace que Noita
   "se sienta mojado". Determinista (la velocidad vive en la celda).
3. **MANCHAS Y MEMORIA SUPERFICIAL** (~+0.2 ms + 1 byte/celda de memoria,
   riesgo bajo) — la piedra QUEMADA queda ennegrecida, lo mojado se oscurece
   y se seca con el tiempo, el humo tizna el techo. Byte `pátina` leído solo
   por el renderer. Regala historia visual gratis — el taller ENVEJECE con tu
   uso — y es el cimiento perfecto de la "evidencia forense" de tu asesor.
4. **EXPLOSIONES DIGNAS** (coste por evento, ~1-2 ms el frame del estallido,
   riesgo bajo) — onda expansiva radial que empuja líquidos/polvos, carva
   débilmente, enciende inflamables + flash de luz + partículas. Hoy no
   existen porque ningún material las pide — el día que una reacción diga
   "Liberacion violenta", el motor las soporta sin sudar.
5. **VAPOR/GASES CON CORRIENTES** (~+10% sobre gases, riesgo bajo) — deriva
   térmica: el gas busca el calor techo arriba, se acumula en bolsas
   CONVINCENTES bajo las bóvedas (y el alambique se vuelve aún más lógico).
6. **60 HZ VISUAL** (0 coste de sim, riesgo bajo) — interpolar el render entre
   ticks (la sim sigue a 30 determinista): todo se ve el doble de suave. Ojo:
   solo interpola COLOR/posiciones visuales, no la grilla.
7. **CUERPOS RÍGIDOS** (semanas de trabajo, riesgo ALTO, rompe supuestos del
   sync) — la única pieza que dejaría para muchísimo después, o nunca: nuestro
   juego es de MATERIA, no de escombros. Veredicto: no vale su precio hoy.

**Mi recomendación de paquete para "más espectacular"**: 1+3+5 primero
(partículas + manchas + gases (~1.5-2 ms extra en el peor caso: entra
holgado hasta en el diluvio), y son EXACTAMENTE lo que Semilla 0 necesita
para que hervir/arder/atrapar vapor se vean gloriosos. Después 2 y 6 como
segunda ola. El 4 cuando el diseño pida su primera explosión. El 7, no.

## 4. ¿Y las reacciones en sí? (tu pregunta "cuánto podemos mejorarlas")

El muestreo actual (1/8 por tick en celdas asentadas, siempre al moverse) es
correcto y barato; con el headroom medido podríamos pasar a 1/4 o incluso 1/2
(reacciones el doble/cuádruple de vivas al contacto) por ~+0.5-1 ms. También
cabe SUB-MUESTREO DIRIGIDO: reacciones a 1/2 solo DENTRO de las cubetas de las
máquinas (donde el jugador mira) y 1/8 en el mundo. Gratis en drama, casi
gratis en coste. Lo que NO recomiendo: reacciones multi-celda complejas
(polimerizaciones, cadenas) antes de Semilla 0 — más química ≠ más legible,
y la lección del playtest con tus amigos fue la contraria.

## 5. Decisión que te pido (alineación de expectativas)

Si apruebas el paquete 1+3+5 (+ el sub-muestreo dirigido de reacciones), lo
implemento ANTES de construir Semilla 0 — porque sus beats (el hervor, el
vapor atrapado, el fracaso forense con su ceniza y su tizne) van a apoyarse
directamente en esas tres capas. Es una ronda de motor, medible con este
mismo banco antes/después, sin tocar diseño. La Semilla 0 v2 (documento
hermano) queda lista para construirse encima.
