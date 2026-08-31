# INVESTIGACIÓN — ANIMAR AL MUÑECO BARATO (2025–2026)

*Ronda 117. Investigación de herramientas, modelos y papers publicados o
actualizados entre 2025 y agosto de 2026 que abaratan la animación de un
personaje 2D estilizado. Contexto: cuerpo humanoide pequeño, cabeza cúbica
enorme y RÍGIDA, brazos simples, piernas cortas, LEVITA, ojos/glow/utilería
separados en Unity; resultado final = sprite 2D ilustrado; el 3D es
herramienta opcional. Referencia a batir: Mixamo + rig proxy + retarget +
prerender 2D (docs/DIRECCION_DE_ARTE.md §2). Fuentes al final.*

> **Nota de método.** Cada candidato separa TRES licencias: la del CÓDIGO,
> la de los PESOS y los derechos sobre los OUTPUTS. "Open source" no
> significa comercial. Donde no pude verificar un dato de fuente primaria,
> lo marco **[verificar]** — no lo adivino. VRAM y velocidad son cifras
> publicadas por los autores o por guías de despliegue; cambian con
> cuantizaciones y wrappers (ComfyUI/Kijai/GGUF) cada pocas semanas.

---

## 0. LAS TRES CONCLUSIONES (para leer primero)

1. **Se puede validar una animación del muñeco EN UNA TARDE sin construir el
   modelo 3D**: PNG del muñeco + un video de Cesar con el celular → Wan2.2-
   Animate (Apache-2.0, sep. 2025) o su sucesor **Wan-Animate-2 (Apache-2.0,
   7 de agosto de 2026, tres semanas de vida)** → matting → nuestro script
   de sprite sheet. El riesgo conocido es la cabeza cúbica deformándose; la
   mitigación ya es nuestra arquitectura (cabeza rígida separada en Unity).
2. **La referencia actual sigue siendo la columna vertebral para VOLUMEN**
   (docenas de emotes, N skins con un solo movimiento), pero con dos
   mejoras concretas: **UniRig (MIT)** sustituye el rig proxy manual, y
   **HY-Motion 1.0 (Tencent, dic. 2025)** genera movimientos por texto que
   Mixamo no tiene — con una licencia que hay que leer (excluye UE/Reino
   Unido/Corea del Sur y exige declarar el contenido generado por IA).
3. **La ruta 100% 2D no está muerta**: Unity 2D Animation + Sprite Library
   resuelve "un movimiento, muchas cabezas" de fábrica, con cero riesgo
   jurídico. Es la mejor red de seguridad para los 6 clips de gameplay.

---

## 1. FICHAS DE CANDIDATOS

Formato de cada ficha: **1** problema · **2** fecha/estado · **3** entrada→salida ·
**4** ¿no humanos/chibi? · **5** ¿nuestro cubo? · **6** VRAM · **7** velocidad ·
**8** lic. código · **9** lic. PESOS (exacta) · **10** ¿outputs comerciales en
un videojuego? · **11** riesgos · **12** calidad · **13** manual residual ·
**14** nota 0–10 para NUESTRO proyecto.

### A. Wan-Animate-2 (Alibaba Tongyi) — y su padre Wan2.2-Animate-14B
1. Imagen de personaje + video de referencia → video del personaje
   imitando el movimiento (modo animación) o sustituyendo al actor (modo
   reemplazo). Wan-Animate-2 consume el video DIRECTAMENTE (sin esqueleto
   intermedio), añade control de punto de vista por texto y multi-personaje.
2. Wan2.2-Animate-14B: 19 sep. 2025. **Wan-Animate-2: 7 ago. 2026** (pesos +
   inferencia + ComfyUI + DiffSynth; variantes Base, Distilled y **Lite en
   tiempo real a 24 fps @ 400×720**; cuantizaciones INT8/BF16).
3. PNG + MP4 (+ texto) → MP4 720p/24 fps.
4. Sí: la tarjeta muestra un gato antropomorfo con uniforme; el linaje
   Wan-Animate maneja no humanos razonablemente. Chibi extremo: sin
   garantía — el modelo "humaniza" proporciones.
5. **Sí, con la regla de la cabeza**: animar el CUERPO (PNG sin cabeza, o
   con cabeza pero aceptando deformación) y componer la cabeza rígida en
   Unity sobre el ancla del cuello. La levitación ayuda: no hay contacto de
   pies que delate errores de retarget.
6. 2.2-Animate: ~20 GB en 4090 con offload a 480p (la tabla oficial va de
   ~20 a 80+ GB según variante). Animate-2: "8× A800 por defecto a 720p;
   480p probado en 2× A800" — en consumo, esperar a las GGUF/wrappers (ya
   anunciados) o usar nube (NVIDIA NIM, fal). **[verificar VRAM real de
   Lite en consumo]**
7. Minutos por clip de 5 s en 4090 (2.2); Lite: 24 fps en streaming en
   hardware de datacenter.
8. Apache-2.0.
9. **Apache-2.0** (código y pesos; tarjeta de HF).
10. **Sí.** Apache-2.0 no restringe outputs; la tarjeta solo prohíbe usos
    ilegales/dañinos.
11. Bajo. Riesgo práctico: consistencia entre clips (cada generación es
    una tirada); riesgo estético: el "look IA" en el pelo/telas (nuestra
    túnica de arpillera es texturada — vigilar).
12. Alta para humanoides; media-alta para antropomorfos; el cubo rígido es
    la incógnita a probar.
13. Matting, recorte de frames, limpieza de 2–3 frames malos por clip,
    componer la cabeza.
14. **9/10 para VALIDAR HOY; 6/10 como producción final** (falta control fino).

### B. Animate-X (Ant Group, ICLR 2025) y Animate-X++ (ago. 2025)
1. Animación de imagen de personaje "universal": entrenado a propósito para
   personajes ANTROPOMORFOS y mascotas no humanas (el paper nace de que
   Animate Anyone falla en ellos). ++ añade fondos dinámicos.
2. Pesos dic. 2024, código dic. 2024, ICLR feb. 2025; ++ paper ago. 2025 con
   pesos en HF (Shuaishuai0219/Animate-X-plusplus).
3. JPG + MP4 de pose → MP4 (por defecto 32 frames a 768×512, 8 fps).
4. **Sí, es su razón de ser.**
5. Sí en principio; resolución y fps bajos (8 fps nativos → nuestra
   cadencia 12–15 se llena por interpolación o se regenera).
6. Sin cifra oficial **[verificar]**; el linaje UniAnimate/MimicMotion corre
   en 16–24 GB.
7. Lento (SD1.5-era); minutos por clip.
8. Apache-2.0.
9. **No especificada en el repo [verificar]**; depende de bases
   (UniAnimate/MimicMotion/MusePose→SD1.5 CreativeML OpenRAIL-M). Zona gris.
10. Probable pero **no garantizado**: sin licencia de pesos explícita no se
    puede afirmar.
11. Medio: licencia de pesos ambigua = riesgo real para un producto
    comercial. Proyecto académico sin mantenimiento activo.
12. Media; su fuerte es la identidad del personaje raro, no el detalle.
13. Igual que A, más upscaling.
14. **5/10** (la idea correcta, el papeleo incorrecto).

### C. Index-AniSora V3.2 (Bilibili, sep. 2025)
1. Video generativo ESPECIALIZADO EN ANIMACIÓN 2D (anime, manga, PV):
   imagen→video, primer/último frame (inbetweening), guía por pose,
   profundidad, line art y máscaras temporales (AnyMask, oct. 2025).
2. V1 dic. 2024 (CogVideoX) → V2 jul. 2025 (Wan2.1-14B) → V3.2 sep. 2025
   (Wan2.2, 8 pasos) → AnyMask oct. 2025. Activo.
3. Imagen (+ pose/keyframes/texto) → MP4 360p–720p.
4. Sí: 2D estilizado es su dominio; consistencia de personaje reportada
   ~95% en su benchmark.
5. Sí para LOOPS AMBIENTALES (parpadeo, vaivén del brote, la llama de los
   ojos) y para inbetweening entre dos poses dibujadas por Cesar.
6. Variante para 12 GB (V3.2); normal 24 GB.
7. "5 s a 360p en 8 s" con destilación.
8. Apache-2.0.
9. **Apache-2.0** (código y pesos, HF/ModelScope).
10. **Sí.**
11. Bajo. Estilo anime puede empujar el look; hay que domarlo con la imagen.
12. Media-alta en 2D; control de pose menor que Wan-Animate.
13. Curar tiradas; matting; sheets.
14. **7/10** (la mejor apuesta 2D-generativa para micro-reacciones).

### D. Wan2.2 VACE / Fun-Control / FLF2V (Alibaba, 2025)
1. La caja de herramientas de control del ecosistema Wan: video guiado por
   POSE (OpenPose/DWPose) con imagen de referencia; primer-último frame
   (tweening entre dos ilustraciones); máscaras.
2. Wan2.1-VACE may. 2025; Wan2.2 Fun/VACE jul.–sep. 2025; flujos ComfyUI
   nativos y de comunidad (neurocanvas, runcomfy) documentan "posado
   consistente de personaje".
3. Imagen + video de pose (o 2 frames) → MP4 480p/720p.
4. Sí, con la misma salvedad chibi que A.
5. Sí: FLF2V es el candidato para "Cesar dibuja pose A y pose B, la
   máquina rellena".
6. 5B: ~8–12 GB; 14B: 24 GB (menos con GGUF).
7. 1–4 min por clip en 4090.
8. Apache-2.0. 9. **Apache-2.0.** 10. **Sí.**
11. Bajo.
12. Alta en humanoides; el tweening entre ilustraciones propias es
    sorprendentemente limpio cuando las poses están cerca.
13. Curar, matting, sheets.
14. **7.5/10** (control + licencia limpia; es el "plan B" de A).

### E. DreamActor-M2 (ByteDance, 29 ene. 2026) / OmniHuman-1.5
1. Animación universal de imagen de personaje por aprendizaje en contexto;
   declara generalizar a "personajes arbitrarios, no humanoides" y publica
   el benchmark AW Bench.
2. Paper enero 2026 (SIGGRAPH). **Sin pesos públicos**; OmniHuman se vende
   por API (BytePlus).
3. Imagen + video → video.
4. Sí (por diseño). 5. Sí, si existiera acceso.
6–7. N/A (servicio).
8–9. Cerrado. 10. Según ToS del servicio (normalmente sí, sin exclusividad).
11. Dependencia de proveedor, coste por segundo, sin control local.
12. Probablemente la mejor calidad del campo hoy.
13. Igual que A.
14. **4/10** hoy (cerrado); a vigilar.

### F. UniRig (VAST + Tsinghua, SIGGRAPH 2025)
1. Auto-rigging de mallas 3D diversas: predice esqueleto topológicamente
   válido + pesos de skinning.
2. Código abr. 2025, checkpoints en HF; "liberación progresiva".
3. .obj/.fbx/.glb/.vrm → FBX/GLB riggeado.
4. **Sí, explícitamente**: humanos, VRoid/anime, animales, dragones,
   objetos.
5. **Sí**: riggea el modelo único del muñeco en minutos; la cabeza cúbica
   como malla separada con un solo hueso queda perfecta.
6. 8 GB mínimo. 7. Segundos–minutos por malla.
8. **MIT.** 9. **MIT** (repo/HF). 10. **Sí.**
11. Muy bajo. Calidad del skinning en proporciones chibi: revisar pesos en
    axilas/cuello (10 min en Blender).
12. Alta para el esqueleto; media-alta para pesos.
13. Retoque de pesos; nombrar huesos para Mixamo/Unity Humanoid si se
    quiere retarget automático.
14. **8.5/10** (sustituye el rig proxy manual de la referencia).

### G. Puppeteer (Seed3D / ByteDance, NeurIPS 2025 Spotlight)
1. Rig automático (esqueleto + skinning) Y ANIMACIÓN del modelo guiada por
   VIDEO mediante optimización diferenciable.
2. Código y checkpoints 4 sep. 2025; dataset Articulation-XL2.0 (59.4K
   rigs).
3. Malla → FBX riggeado; + video → secuencia animada.
4. Sí (ciervo y objetos diversos en el repo).
5. **Sí, y es la joya escondida**: video de Cesar con el celular →
   movimiento sobre NUESTRO rig, sin mocap ni Mixamo.
6. Sin cifra **[verificar]** (esperar ≥16 GB).
7. Optimización por clip: minutos.
8. Apache-2.0. 9. **Pesos en HF sin licencia detallada en el README
   [verificar]**. 10. Probable; confirmar antes de producción.
11. Medio-bajo (verificar pesos). Proyecto de investigación: expect
    rough edges.
12. Prometedora; pocos reportes de terceros aún.
13. Limpieza de curvas en Blender.
14. **7/10 (apuesta seria para la ruta 3D sin Mixamo).**

### H. HY-Motion 1.0 (Tencent Hunyuan, 30 dic. 2025)
1. Texto → movimiento 3D humano (DiT + flow matching, 1.0B y 0.46B).
2. Publicado con pesos, código, plugin ComfyUI.
3. Texto (<60 palabras) → esqueleto SMPL/SMPL-H, exportable a FBX;
   recomendado <5 s por clip.
4. Genera movimiento HUMANO; el retarget a chibi es cosa nuestra (y la
   levitación perdona los pies).
5. Sí, como fábrica de gestos ("saluda tímido", "señala arriba dos veces",
   "reverencia corta") que Mixamo no tiene.
6. 26 GB (1.0) / 24 GB (Lite). 7. Segundos por clip.
8. Tencent Hunyuan Community License.
9. **Tencent Hunyuan Community License**: **no aplica en la Unión Europea,
   Reino Unido y Corea del Sur**; comercial libre hasta 100 M de usuarios
   activos mensuales; política de uso aceptable que exige **declarar el
   contenido generado por IA** y prohíbe engaño/suplantación.
10. Sí, con las cláusulas anteriores. Los movimientos retargeteados y
    horneados a sprites son "outputs": la licencia los permite, pero la
    obligación de divulgación se hereda al producto.
11. **Medio**: territorialidad (¿publicamos en la UE? la licencia del
    MODELO no aplica allí — usar el modelo desde fuera para producir
    assets y vender el juego en la UE es zona gris que un abogado debe
    leer); divulgación de IA en los créditos.
12. Alta en gestos humanos cortos.
13. Retarget + limpieza + prerender (la referencia entera).
14. **7/10** (potencia enorme, papeleo pesado).

### I. Mixamo (Adobe) — la referencia
1. Biblioteca de ~2.500 animaciones humanoides + auto-rigger de mallas
   propias.
2. Gratis, sin novedades desde hace años; sigue en línea (FAQ vigente).
3. FBX/OBJ humanoide en T-pose → FBX riggeado + clips.
4. Requiere humanoide; el chibi pasa el auto-rigger si la malla es limpia,
   con artefactos en brazos cortos.
5. Sí (es el plan actual). La cabeza cúbica entra como malla aparte.
6–7. Nube, gratis, segundos.
8. N/A (servicio). 9. N/A.
10. **Sí**: la FAQ oficial dice "royalty free for personal, commercial and
    non-profit projects including: create video games".
11. Bajo; riesgo de discontinuación (Adobe no lo desarrolla).
12. Alta para movimientos humanos genéricos; nula para "gestos de nuestro
    mundo".
13. Retarget + prerender.
14. **7.5/10** (sigue siendo la base; ya no la única).

### J. Cascadeur (Nekki)
1. Animación 3D asistida por física/IA (autoposing, secondary motion).
2. Planes 2026: **Basic gratis = solo no comercial, exporta solo .casc,
   300 frames/120 huesos**; **Indie US$8/mes anual, ingresos <US$100k/año,
   exporta FBX**; Pro US$33/mes.
3. Rig FBX → clips FBX.
4. Sí (rig propio). 5. Sí, para pulir a mano lo que venga de HY-Motion/
   Puppeteer.
6–7. CPU/GPU modestos; interactivo.
8–9. Propietario. 10. **Solo con plan pagado** (Indie basta).
11. Bajo. 12. Alta. 13. Es la herramienta manual.
14. **6.5/10** (útil como pulido; US$96/año).

### K. TRELLIS.2-4B (Microsoft, 16 dic. 2025) / Hunyuan3D 3.0
1. Imagen → malla 3D con materiales PBR (para tener el modelo base del
   muñeco sin esculpir).
2. TRELLIS.2: MIT, pesos públicos; Hunyuan3D bajo licencia Tencent
   (mismas exclusiones territoriales que H).
3. PNG → GLB con PBR.
4. Sí (objetos y personajes estilizados).
5. **Sí para el borrador del modelo único**: del PNG P1 sale un GLB
   decente para probar rig y cámara en una tarde; el modelo definitivo lo
   esculpe Cesar.
6. TRELLIS.2: 24 GB. 7. ~1 min por malla.
8. MIT. 9. **MIT** (tarjeta HF). 10. **Sí.**
11. Bajo. Topología generativa: no apta para deformación fina (pero
    nuestro muñeco casi no se deforma).
12. Media-alta.
13. Retopo ligera si se quiere rig limpio; separar cabeza.
14. **7/10** (acorta semanas del "modelo único" a horas para PRUEBAS).

### L. Unity 2D Animation + Sprite Library (paquete oficial)
1. Rig esquelético 2D sobre PSD/PSB por capas, auto-weights, IK, y
   **Sprite Library/Sprite Resolver: intercambiar cabezas/skins sobre el
   MISMO rig y las MISMAS animaciones**.
2. Maduro; Unity 6 lo trae.
3. PSB por capas → rig + AnimationClips.
4. Sí. 5. **Sí, exactamente**: cabeza cúbica = un hueso sin deformación;
   máscaras de acento = Sprite Library.
6–7. Nada. 8–10. Licencia Unity; outputs 100% nuestros.
11. Ninguno. 12. La que Cesar anime a mano (cutout ≠ ilustración cuadro a
    cuadro: se nota "de papel" si se abusa).
13. TODO es manual: 6 clips × 1–2 h con curvas.
14. **8/10** (red de seguridad + solución nativa a "un movimiento, N skins").

### M. Spine (Esoteric Software)
1. Lo mismo que L con mejor herramienta de autor, mesh deform, skins,
   runtime Unity oficial.
2. Vivo; Essential ≈US$69 / Pro ≈US$349 perpetua **[verificar precio
   vigente]**.
3–5. Como L, con más finura.
8–10. Propietario; runtime Unity con licencia por asiento; outputs nuestros.
11. Bajo. 12. Alta. 13. Manual.
14. **7/10** (solo si Cesar prefiere animar fuera de Unity).

### N. Animated Drawings (Meta, MIT, 2023)
1. PNG de un dibujo humanoide → detección de articulaciones → rig 2D
   automático → retarget de BVH (incluye clips de ejemplo).
2. Estable, sin cambios relevantes 2025–26; sigue siendo la única ruta
   PNG→rig 2D automática y libre.
3. PNG → personaje riggeado + MP4/GIF (frames exportables).
4. Sí si la silueta lee como humanoide (nuestro cubo con brazos y piernas
   pasa).
5. Sí como PRUEBA de cero coste: el rig automático deforma la cabeza (mesh
   warp) — la solución es la misma: cabeza aparte.
6. CPU/GPU pequeño. 7. Segundos.
8. **MIT.** 9. **MIT** (detectores incluidos). 10. **Sí.**
11. Ninguno. 12. Baja-media (encanto "de papel"). 13. Config de
    articulaciones a mano si la detección falla.
14. **6/10** (gran experimento de 30 minutos; no producción).

### O. Extracción de frames, transparencia y sheets
- **rembg** (MIT; modelos u2net/isnet) y **BiRefNet** (MIT): matting de
  imagen por frame — ya lo usamos (el recorte del muñeco de la R108 fue
  rembg/isnet).
- **SAM 2** (Apache-2.0): segmentación con propagación temporal — mejor
  para video: un clic en el frame 0 y la máscara sigue al personaje.
- **MatAnyone** (2025, video matting): **S-Lab License 1.0 — NO
  comercial** ⚠. No usar en el pipeline del juego.
- **Nuestro script** (R108 `componer_tallas.py` + el horno de sheets
  prometido en DIRECCION_DE_ARTE): frames → recorte → pivote → sheet +
  meta de Unity. Todo casero, cero licencias.
- Nota: 10/10 en papeleo; el trabajo manual es curar frames malos.

### P. SAM 3D Body + MHR (Meta, nov. 2025) — mocap casero
1. De una foto/video → malla humana 3D con esqueleto (Momentum Human Rig).
2. Nov. 2025, pesos en HF (facebook/sam-3d-body-dinov3).
3. Video del celular → poses MHR por frame → retarget.
4. Humano solamente (captura a Cesar, no al muñeco).
5. Sí como fuente de movimiento propia ("actúo el gesto, lo retargeteo").
6. ~12–16 GB **[verificar]**. 7. Casi tiempo real por frame.
8–9. Licencia SAM (permisiva con cláusulas) **[verificar texto para MHR]**.
10. Probable. 11. Medio-bajo. 12. Buena para gestos gruesos.
13. Retarget + limpieza.
14. **6/10** (interesante; Puppeteer hace lo mismo sin pasar por SMPL).

### Q. LTX-2 (Lightricks, 6 ene. 2026) y cerrados (Kling Motion Control,
Runway Act-Two, Viggle)
- LTX-2: video 4K con audio, pesos abiertos, **uso comercial gratuito
  hasta US$10 M de ARR**, consumo en RTX; útil para cinemáticas, no para
  sprites controlados. 5/10.
- Cerrados: calidad alta, outputs comerciales según ToS, cero control
  local, coste por segundo. Útiles para una prueba puntual. 4/10.

---

## 2. RANKING DE PIPELINES COMPLETOS

Criterios: tiempo hasta la primera animación, coste por clip adicional,
reutilización por skins, riesgo jurídico, control de la cabeza rígida,
compatibilidad con el acabado ilustrado (no pixel — DIRECCION_DE_ARTE).

### #1 — HÍBRIDA "ANIMA PRIMERO, RIGGEA DESPUÉS" · 9/10 hoy
PNG del muñeco → video driver (Cesar con el celular o un clip de Mixamo
renderizado) → **Wan2.2-Animate / Wan-Animate-2** (Apache-2.0) → SAM 2 /
BiRefNet → script de sheets → Unity (estampa animada; cabeza rígida
compuesta encima).
- **Primera animación: una tarde.** Coste por clip: minutos de GPU.
- Skins: NO se reutilizan (cada skin es una tirada nueva) → por eso es
  una ruta de VALIDACIÓN y de emotes puntuales, no de volumen.
- Riesgo jurídico: nulo (Apache). Riesgo estético: el look IA; se doma
  con el tratamiento gráfico final (que de todas formas íbamos a hacer).
- Qué valida: la TALLA en movimiento, la lectura del bob/bandeo, si la
  cabeza rígida compuesta convence, y el fps ideal — ANTES de gastar una
  semana en el modelo 3D.

### #2 — 3D/RETARGET→PRERENDER, la referencia MEJORADA · 8.5/10 para volumen
TRELLIS.2 (borrador) o modelo de Cesar → **UniRig** (MIT) → animaciones:
Mixamo (biblioteca) + **HY-Motion** (gestos por texto) + **Puppeteer**
(gestos actuados en video) → Cascadeur Indie (pulido opcional) → Blender
retarget → cámara orto → PNG HD → tratamiento → sheets (horno Unity).
- Primera animación: 1–2 días (más si el modelo definitivo tarda).
- Coste marginal por emote: **minutos** (un rig, N clips, N skins gratis
  con cabezas intercambiables). Es la ÚNICA ruta donde "100 emotes" es
  barato.
- Riesgo jurídico: bajo salvo HY-Motion (leer territorialidad y
  divulgación). Mixamo royalty-free para juegos (FAQ).
- Vs. referencia actual: UniRig ahorra el rig proxy manual; Puppeteer/HY-
  Motion añaden gestos que Mixamo no tiene; el resto es idéntico.

### #3 — 100% 2D (cutout) · 8/10 para los 6 clips de gameplay
PSB por capas (cabeza, torso, brazos, piernas, brote) → **Unity 2D
Animation** (o Spine) → clips a mano con curvas → Sprite Library para
máscaras de acento. Arranque opcional con Animated Drawings para tener un
rig en 30 minutos.
- Primera animación: un día. Coste por clip: 1–2 h de Cesar.
- Skins: nativas (Sprite Library) ✓. Cabeza rígida: trivial ✓. Jurídico:
  cero ✓. Ilustración: intacta (los recortes son SU dibujo) ✓.
- Debilidad: el cutout "de papel" en giros y en muchas emotes; poco
  escalable a decenas de gestos.

### #4 — PNG→VIDEO→SPRITES generativa (loops y micro-reacciones) · 6.5/10
**AniSora V3.2** (Apache) o **Wan2.2 FLF2V** entre dos poses dibujadas →
SAM 2 → sheets.
- Para: parpadeos, vaivén del brote, chispas de los ojos, "idle raro cada
  20 s", reacciones de 8 frames. No para: gameplay ni gestos precisos.
- Skins: no reutiliza. Jurídico: nulo. Consistencia: curar.

### #5 — MOCAP CASERO (video de Cesar → rig) · 6/10, crece con Puppeteer
Video del celular → **Puppeteer** (directo sobre nuestro rig) o **SAM 3D
Body** (SMPL → retarget) → Blender → prerender.
- La gracia: gestos propios del mundo (levantar el frasco, mirar el
  plano) actuados en 10 s. Riesgo: herramientas de investigación con
  aristas; calidad de captura de celular.

**Comparación directa con la referencia (Mixamo + rig proxy + retarget +
prerender):** la referencia gana en volumen y previsibilidad; pierde en
tiempo hasta la primera prueba (días) y en repertorio (solo humano
genérico). La recomendación es NO sustituirla sino ENVOLVERLA: #1 para
validar esta semana, #2 (la referencia con UniRig + HY-Motion/Puppeteer)
para producir, #3 como red de seguridad de los clips críticos, #4 para
condimento ambiental.

---

## 3. LA PRUEBA DE UNA TARDE (antes de construir el modelo 3D)

1. Cesar graba **3 clips** con el celular, frontal, fondo liso, 4–6 s cada
   uno: *reposo con vaivén*, *saludo*, *agarrar y soltar*. Sin caminar
   (levitamos): pies quietos, torso y brazos hablan.
2. Referencia: el PNG del muñeco recortado (ya lo tenemos:
   `Resources/Personaje/MunhecoRemiendos.png`) sobre fondo negro plano.
   Segunda tirada: el mismo PNG **sin cabeza** (la cabeza se compone en
   Unity).
3. Modelo: Wan2.2-Animate-14B en ComfyUI (flujo nativo de comfy.org) si hay
   ≥24 GB; con 12–16 GB, wrapper Kijai + GGUF; sin GPU, NVIDIA NIM o fal
   (céntimos por clip). Si Wan-Animate-2 ya tiene GGUF de consumo esa
   semana, se prueba también.
4. SAM 2 (o BiRefNet por frame) → PNG con alfa → script de sheets a 12 fps
   → importar como estampa animada (la ruta `customSprite` ya existe:
   se le da un AnimationClip en vez de un sprite fijo).
5. Veredicto en el taller real: talla en movimiento, lectura del cubo
   compuesto, fps. Si convence → seguimos al #2 con el modelo 3D. Si no
   convence por la cabeza → #3 para gameplay y #2 para emotes.

Coste estimado: 0–3 USD y una tarde. Riesgo: cero (nada de esto toca el
repo del juego hasta el veredicto).

---

## 4. APUESTAS EXPERIMENTALES (verdes, pero con futuro)

- **Wan-Animate-2-Lite en tiempo real (24 fps @ 400×720, ago. 2026)**: por
  primera vez un modelo de animación de personaje corre a velocidad de
  juego. No es para el runtime (licencia y hardware aparte, la sim manda),
  pero sí para un "horno" que genere cientos de emotes en una noche.
- **Puppeteer** (rig + animación por video de cualquier malla): si madura,
  elimina Mixamo y el mocap del pipeline 3D.
- **DreamActor-M2** (ByteDance, ene. 2026): la mejor calidad declarada
  para personajes no humanoides; cerrado. Vigilar si liberan pesos.
- **AniSora AnyMask**: máscaras temporales = "anima solo el brazo derecho"
  sobre nuestra ilustración. Muy alineado con "ojos/utilería separados".

---

## 5. SOBRE LOS EMOTES SOCIALES (opinión pedida)

La idea es buena y, más importante, es **coherente con la tesis**: en un
juego de reconstruir juntos, el lenguaje corporal ES el chat. Tres notas de
director:

1. **El cuello de botella son las animaciones, no el sistema.** Un menú de
   acordes es una tarde de código (mantener E + WASD = 4 emotes rápidos;
   E dos veces = rueda). Lo caro son los 12–30 clips. Por eso el pipeline
   #2 (un rig, N clips, N skins gratis) es la condición para que esta idea
   exista; con #1 o #3 solos, no escala.
2. **Los emotes son CAPA COSMÉTICA, jamás estado de la sim.** Un emote es
   un evento de red (id + tick + jugador); la sim determinista no lo ve.
   Los "duetos" (choque de cabezas cúbicas a 3 celdas, danza en espejo,
   cargar al otro) se resuelven por proximidad en el cliente. "Invocar al
   maestro si se juntan 3" cruza la línea: si el maestro DA algo, es
   economía y entra por la sim con regla explícita (R33/R51); si solo
   APARECE y saluda, es cosmético y es precioso — empezaría por ahí.
3. **Orden de prioridad honesto**: primero la demo Era I (las dos
   máquinas), luego el pipeline #2 con 6 clips de gameplay, y los emotes
   como el primer contenido "de volumen" que ese pipeline produce —
   12 emotes + 3 duetos + 1 ritual del maestro es un alcance realista para
   el primer corte.

Repertorio sugerido para el primer corte: saludo, señalar, celebrar, negar,
cansado, pedir, gracias, risa, susto, reverencia, aplauso, sentarse a
mirar. Duetos: choque de cubos, espejo, "te cargo". Ritual: tres muñecos
en círculo tocan el suelo → el maestro emerge del vano, mira, y se va.

---

## FUENTES (verificadas 31 ago. 2026)

- Wan-Animate-2 (7 ago. 2026): https://www.opensourceforu.com/2026/08/alibaba-open-sources-wan-animate-2-real-time-ai-character-animation/ · https://huggingface.co/Wan-AI/Wan2.2-Animate-2-14B · https://comfyui-wiki.com/en/news/2026-08-07-wan-animate-2
- Wan2.2-Animate-14B: https://huggingface.co/Wan-AI/Wan2.2-Animate-14B · flujo ComfyUI: https://docs.comfy.org/tutorials/video/wan/wan2-2-animate
- Animate-X / ++: https://github.com/antgroup/animate-x · https://arxiv.org/html/2508.09454v1 · https://huggingface.co/Shuaishuai0219/Animate-X-plusplus
- Index-AniSora: https://github.com/bilibili/index-anisora
- Wan VACE posado consistente: https://neurocanvas.net/blog/consistent-character-posing-comfyui/ · https://www.runcomfy.com/comfyui-workflows/wan-2-2-vace-in-comfyui-pose-driven-motion-video-workflow
- DreamActor-M2: https://arxiv.org/abs/2601.21716 · OmniHuman-1.5: https://arxiv.org/html/2508.19209v1
- UniRig: https://github.com/VAST-AI-Research/UniRig · https://huggingface.co/VAST-AI/UniRig
- Puppeteer: https://github.com/Seed3D/Puppeteer · MagicArticulate: https://github.com/Seed3D/MagicArticulate · Make-It-Animatable: https://arxiv.org/html/2411.18197v3
- HY-Motion 1.0: https://github.com/Tencent-Hunyuan/HY-Motion-1.0 · https://huggingface.co/tencent/HY-Motion-1.0 · términos de la licencia Hunyuan: https://deepwiki.com/Tencent/HunyuanVideo/5-license-and-legal
- Mixamo FAQ (comercial, videojuegos): https://helpx.adobe.com/creative-cloud/faq/mixamo-faq.html
- Cascadeur planes: https://cascadeur.com/plans
- TRELLIS.2-4B (MIT): https://huggingface.co/microsoft/TRELLIS.2-4B
- LTX-2: https://www.opensourceforu.com/2026/01/ltx-2-from-lightricks-delivers-native-4k-audio-video-with-fully-open-weights/
- SAM 3D Body / MHR: https://huggingface.co/facebook/sam-3d-body-dinov3 · https://github.com/facebookresearch/MHR
- Animated Drawings (MIT): https://github.com/facebookresearch/AnimatedDrawings
- Pipeline 2D en ComfyUI (referencia de la comunidad): https://github.com/mor-o/comfyui-2d-character-pipeline
