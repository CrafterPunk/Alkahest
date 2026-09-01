# EL PLAN DE ANIMACIÓN — balance de la R118 y qué esperar de cada camino (R119; decisión R120)

> **DECISIÓN (R120, Cesar):** el generativo queda como prototipador de acting y
> referencia de estilo; la producción va a UN personaje 3D canónico (image-to-3D
> del CUERPO + cabeza-cubo con el arte proyectado + rig automático + render con
> máscaras y normales). Evaluación y candidatos (TRELLIS.2 primero, Tripo Pro para
> comparar, Hunyuan3D fuera por licencia, Mixamo auto-rigger, títere 2D como plan C)
> en el HISTORIAL R120. El atril de emotes (§5, capa 1) YA existe: acordes 1-4 + 1-4.
>
> Documento de decisión. Compañero de `INVESTIGACION_ANIMACION_2026.md` (las 17 fichas
> y el ranking teórico) y del ROADMAP §2.6. Aquello era el mapa ANTES de tocar nada;
> esto es el balance DESPUÉS de la primera noche de producción real, con números
> nuestros y opinión de dirección. La infografía "Cinco Rutas" sigue vigente como mapa.

## 1. Lo que hicimos (el método, para el archivo)

En una noche quedó montado y validado el **arnés local completo** (carpeta
`Arnes_Animacion\`, fuera del repo, `LEEME.md` dentro):

```
Mixamo (FBX gratis, actor profesional)
  → Blender 5.2 (maniquí gris, cámara fija 3/4, 480x848, video de pose)   [15-30 s]
  → Wan Animate 2 int8 en ComfyUI local (la 4070 Ti Super)                [3-5 min]
  → postproceso.py (alfa por región conectada + huecos cerrados,
     descontaminación del contorno, erosión 2 px, búsqueda del ciclo
     que cierra, hoja PNG + manifiesto JSON)                              [10 s]
  → HojaDeCuadros en Unity (sprites en runtime, talla 1.2u sola,
     arranque/frenado, base canónica, ping-pong)                          [0]
```

Costo real por gesto con la máquina bien usada (Unity cerrado, Blender sin escena,
17 GB de RAM libres): **3–5 minutos**. Mal usada (Blender ocupando la GPU, poca RAM):
10–30. Todo gratis, todo Apache-2.0, todo en tu disco.

Reglas aprendidas (las caras): el video de pose SIEMPRE a 16 fps; referencia sobre
fondo plano con aire; el cuadro 0 de cada video = referencia × primera pose del
video (por eso la canónica sale de un video cuyo primer cuadro es neutro — Happy
Idle); JAMÁS canónicas de segunda generación (deriva de parches y placas); azimut
35° para gestos de pie, 75° para los que se inclinan a cámara; gestos largos se
comprimen con `--velocidad`.

## 2. Los resultados (con la mirada fría)

**Lo que salió mejor de lo esperado.** La calidad de imagen por cuadro es de otra
liga: la cabeza-caja gira en 3D con sus tres caras, placas y musgo; la plantita
se mece sola; los reflejos del metal viven sin foco visible; el modelo INVENTÓ
una espalda plausible. Veredicto de Cesar: "a la altura de un juego AAA...
parece hecho a posta, muy fluido, algo muy real dentro de un mundo de fantasía".
Y el hallazgo de dirección que no estaba en el plan: el contraste
personaje-pintado vs mundo-de-celdas **no molesta — se lee intencional**, como
actores reales dentro de un decorado dibujado. Esa lectura le quita presión a
"emparejar" el mundo con el personaje: el escenario debe mejorar, pero no debe
IMITARLO.

**Lo negativo, con nombre: la inconsistencia.** Cada generación es un dibujante
distinto que dibuja MUY bien: micro-popeos de cabeza entre hojas, manos que a
veces insinúan dedos (el muñeco tiene manoplas), pies que cambian de forma,
detalles (parches, placas) que migran entre generaciones. Nada de eso se corrige
"para siempre": se corrige por pieza, eligiendo. Y los bordes: 90 % resueltos por
software; el último 10 % (medio píxel en según qué cuadro) puede pedir repaso
manual o el truco del fondo oscuro.

## 3. La reformulación que cambia el problema

La frase clave del balance: **esto no es un generador en runtime, es una fábrica
de candidatos para una biblioteca finita.** El juego necesita 10–20 clips, no
infinitos. Entonces la inconsistencia no es un bug del producto: es un costo de
CURADURÍA. La pregunta correcta no es "¿puede el modelo ser consistente?" (no
puede, hoy no), sino "¿cuánto cuesta elegir 15 clips buenos?" — y ese costo sí
lo podemos bajar en serio:

- **Tandas de N semillas por gesto** (`--seed` distinto, 3–5 corridas nocturnas):
  elegir entre 5 es mucho más barato que re-pedir 5 veces.
- **Control de calidad automático de primera pasada** (a construir en
  `postproceso.py`): comparación cuadro-a-cuadro contra la canónica por regiones
  (cabeza/cuerpo/manos/pies) — histograma de color, área de la silueta, conteo de
  "protuberancias" en las manos (dedos), salto de bbox entre cuadros (pop). No
  reemplaza tu ojo: le pre-filtra el 70 % y te marca EN QUÉ cuadros mirar.
- **Cirugía de cuadros** (a construir): descartar 1–3 cuadros malos de un ciclo
  bueno y rellenar con vecinos (16 fps perdona mucho) antes que tirar la
  animación entera "con mucho dolor".
- **Fondo oscuro** para matar el último filo del contorno.
- **Manoplas al negativo** ("fingers, human hands, five fingers, toes") y a la
  descripción ("mitten hands with no fingers, stubby round hands").

**Expectativa honesta de este pipeline (ruta 1):** con lo de arriba creo
alcanzable un **+20–25 de los 40 puntos** que Cesar pide para demo gratuita —
demo interna sólida YA la tenemos. Sabor a producción final consistente, ruta 1
sola, **no lo prometo**: el último tramo es curaduría humana o el pipeline 3D.
Donde la ruta 1 es imbatible y se queda para siempre: prototipado de gestos
(saber en 5 minutos si un gesto "funciona" en este personaje), material de
marketing, y **definir el ESTILO de movimiento** — los clips ganadores son la
especificación de animación del proyecto.

## 4. Qué esperar de los otros caminos (opinión pedida)

**Ruta 4 (FLF2V / por prompt, "el segundo método").** Coincido con la sospecha de
Cesar y la agravo: mismo dibujante-distinto-cada-vez, y ADEMÁS sin el rail del
video de pose, o sea MÁS libertad para inventar dedos, iris, proporciones. La
plantita no se pierde (no depende de esqueleto: el modelo la mueve porque está en
la imagen, igual que ahora), pero el control neto es menor. Su nicho real:
micro-loops de 1–2 s desde el MISMO cuadro (parpadeo de luces, mecerse) donde
casi no hay espacio para inventar, y reacciones one-shot baratas. No es el camino
de la biblioteca principal.

**Ruta 2 (modelo 3D → retarget → prerender), "la triste historia".** Es la única
con consistencia GARANTIZADA (misma malla, mismos materiales, cada cuadro), y la
pena por el nivel artístico tiene dos mitigaciones concretas que probar ANTES de
deprimirse: (a) **TRELLIS.2 (MIT, image-to-3D)**: generar el modelo 3D directo
del PNG del muñeco — de nuestra lista R117, hoy con más sentido que nunca;
(b) **proyección de cámara del arte original**: el modelo posado exactamente como
la ilustración, y la ilustración PROYECTADA encima como textura — la vista 3/4
(la que el juego usa el 95 % del tiempo) conserva las pinceladas de la primera
generación, y solo los ángulos ocultos son textura nueva. La plantita se riggea
con una cadenita de 3 huesos + física: se mece igual o mejor, y siempre igual.
Expectativa honesta: 85–90 % del look pintado, 100 % de consistencia, y las hojas
de la ruta 1 como REFERENCIA de timing y estilo (incluso: extraer el esqueleto 2D
de los clips ganadores — DWpose/ViTPose — y retargetearlo al modelo, para que el
3D se mueva como el ganador elegido). Las dos rutas no compiten: la 1 define QUÉ
y CÓMO se mueve; la 2 lo fabrica en serie idéntico.

**Pagar un modelo mejor (Wan 2.5/comercial, APIs).** De acuerdo con Cesar y sin
matices: la calidad de imagen ya sobra, y la inconsistencia es estructural de
esta familia de modelos, no del precio. No pagar por ahora; re-evaluar solo si
aparece un modelo con "identity lock" real demostrado en terceros.

**El GGUF cuantizado** sigue pendiente A PROPÓSITO (decisión de Cesar): primero
tener la vara de calidad del modelo completo (la tenemos), después medir si Q5
pierde algo a talla de juego. Con tandas de 3–5 min la urgencia bajó; sigue
valiendo para tandas nocturnas de N semillas (más corridas por noche).

## 5. El atril de emotes (sistema "tipo Rocket League") — plan

Vale la pena y es barato, con una condición: construirlo como DOS capas.

**Capa 1, ya (1 ronda corta): el ATRIL DE PRUEBAS.** Sin red, sin diseño final.
Mantener presionada **T** a pie: se abre un abanico IMGUI (ids constantes, regla
de estilo del proyecto) que lista TODA hoja presente en `Resources/Personaje/Anim/`
(el loader ya las enumera por manifiesto; soltar en un sector = reproducir ese
gesto con `ReproducirGesto`). Cero fricción para probar lo que Cesar descargue:
FBX → Blender → tanda → copiar a Anim/ → aparece solo en el abanico. Es la
herramienta de curaduría con el juego de fondo real (luz, talla, cueva), que es
donde se decide si un clip vive.

**Capa 2, después de las dos máquinas (como dijo el R117): los EMOTES sociales.**
El mismo abanico madura: acordes (T + dirección), régimen de red (RPC con
`InvokePermission`, cosmética pura, JAMÁS estado de la sim), duetos por
proximidad, y el ritual de invocar al maestro SOLO si el maestro aparece. La capa
1 no se tira: se viste.

Orden recomendado: el atril ANTES de la siguiente hornada de generaciones, porque
convierte cada clip nuevo en 30 segundos de prueba en vivo.

## 6. Recomendaciones de dirección (resumen ejecutable)

1. Congelar la ruta 1 como está para la DEMO INTERNA: reposo + caminar + recoger
   ya dan vida al prólogo. Correr `ca_playtest119.cmd` y que la prueben.
2. Ronda corta siguiente: el ATRIL (capa 1) + manoplas al negativo + fondo oscuro.
3. Hornada nocturna de N semillas para los gestos que faltan (despertar, golpe de
   cincel, verter, celebrar) + control de calidad automático de primera pasada.
   Cesar elige con el atril; yo pre-filtro con métricas.
4. En paralelo, SIN prisa: probar TRELLIS.2 con el PNG (una tarde, gratis) para
   medir cuánto 3D nos regala la imagen — es la puerta de la ruta 2 sin esculpir.
5. Emotes de verdad: después de las dos máquinas, sobre el atril.
6. No pagar modelos todavía. El GGUF: medirlo cuando toque la hornada nocturna.

— R119. Los tiempos y trucos operativos viven en `Arnes_Animacion\LEEME.md`;
las fichas y licencias en `INVESTIGACION_ANIMACION_2026.md`.
