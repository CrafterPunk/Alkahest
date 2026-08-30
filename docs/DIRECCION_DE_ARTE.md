# DIRECCIÓN DE ARTE — TEN THOUSAND YEARS (R107)

*(El documento madre de la capa visual. Nace del decreto del mundo — GDD §0 —
y de la sesión de dirección con Cesar del 29/08/2026. NORMATIVO.)*

## 1. LA IDENTIDAD

- **El mundo:** ruinas amables. Musgo sobre máquinas, óxido digno, sin combate.
  Referencia de tono: Nausicaä/Laputa, jamás Mad Max.
- **La tesis visual:** todo lo que ves está REMENDADO. Las máquinas, el taller
  y el propio personaje cuentan sin palabras que hubo un mundo antes y que
  repararlo es el juego.
- **La paleta madre** (ya validada por daltonismo en claro y oscuro, R105):
  tinta parda sobre ceniza cálida; acentos BRASA #C84A14 · PÁTINA #1F8F6E ·
  ÁMBAR #B58414 · AZUL MUDANZA #3E6DB5 · VINO #A8437A. Los acentos de jugador
  del personaje calzan en esta familia.
- **Convivencia de capas:** ilustración 2D artesanal de formas simples y
  robustas SOBRE la sim celular. El acabado final está ABIERTO: pixel es una
  salida posible que se decidirá viendo, no un supuesto.

## 2. EL PERSONAJE — EL MUÑECO DE REMIENDOS

Avatar canónico (el imp queda como placeholder en retirada). Cabeza-cubo de
tablones remendados con placas metálicas, ojos de brasa, brote vivo en la
coronilla; cuerpo de arpillera con brazos atados de cuerda. LORE: el cuerpo
heredado — cada material es un remiendo de un aprendiz anterior en los diez
mil años; el brote es tu ciclo.

- **LEVITA, no camina** (títere sostenido por la chispa del Maestro; piernas
  colgando). Al DESPERTAR de la intro se levanta del suelo: el primer gesto
  del juego es la física dejando de aplicarle.
- **Variantes de jugador:** un solo modelo; parches de acento en MÁSCARA
  teñible → N colores en runtime (P1 pátina, P2 azul, P3 brasa; 4ª por
  definir — candidato: ámbar).
- **INTRO:** cubo caído/tirado → voz «LEVÁNTATE.» (el lenguaje ritual de una
  palabra del Maestro) → ojos que se encienden → el brote crece.

## 3. EL PIPELINE DE PRODUCCIÓN (hipótesis vigente)

diseño 2D → **modelo 3D ÚNICO** (herramienta de producción, no arte final) →
rig/animaciones → cámara ORTOGRÁFICA fija → render PNG transparente en alta →
tratamiento gráfico final → sprites 2D en Unity.

Reglas del modelo: cabeza-cubo como MALLA RÍGIDA separada (jamás se deforma);
cuerpo riggeado (Mixamo con truco del proxy, o Cascadeur); SIN utilería
horneada (matraz, brillos, ojos, brote = motor). SEGUNDA PASADA DE MÁSCARA
(zonas de acento en rojo plano, mismo encuadre) para el teñido runtime.

**Las 6 animaciones:** reposo · desplazamiento · agarrar · soltar · golpe de
cincel · despertar.

**Las 3 perillas de convivencia** (mitigan la costura ilustración/sim SIN
pixelar; se prueban, no se imponen): contorno oscuro estilo regla 19 ·
luz plana + tinte del motor (TinteGlobal/viñeta, sistema existente) ·
cadencia a pasos (render 12–15 fps, no interpolación sedosa).

**Riesgo nombrado:** el «tratamiento gráfico final» debe ser AUTOMATIZABLE
(filtro/script); pincel a mano solo en stills de marketing. Si no se puede
automatizar: 6–8 frames por animación, máximo.

## 3.5 TALLA Y CÁMARA (SELLADAS EN R108, con banco de tallas)

- **Talla del personaje: 12 celdas de alto** (1.2 unidades; el PNG canónico
  vive en Resources/Personaje/MunhecoRemiendos.png a 1000 px/unidad). A la
  vista por defecto ocupa ~13% de pantalla — liga Eastward/Hollow Knight.
  Referencias medidas: Noita 5-6%, Terraria 4% (comunidad juega a 150-200%),
  Dome Keeper 5-6%, Celeste/Animal Well ~9%, Hollow Knight ~10%,
  Eastward/Moonlighter 10-13%. Regla: bajo ~6% el cariño es silueta y
  movimiento; los ojos expresivos viven de 10% para arriba — y el alma de
  este muñeco son sus ojos-lámpara.
- **Cámara: el defecto es 80 celdas visibles** (R109 — Cesar jugó la R108
  pegado al tope de la rueda: "ahí apenas alcancé a sentir que yo era el
  personaje"; lo que se juega siempre debe ser el defecto). La rueda guarda
  reserva de intimidad hasta 72 y aleja hasta las MISMAS 198 de siempre
  (WideViewMultiplier 2.475); Tab sigue siendo el plano entero. El plano
  amplio (120+) quedó descartado como vista de juego: es un buzón en roca.
- **La escala relativa se corrige en CELDAS, jamás con zoom** (lección
  R109): el zoom agranda TODO por igual (partículas incluidas) y no puede
  cambiar la proporción personaje/mueble. Si el mundo se siente de juguete,
  crecen los muebles (o encoge el personaje) — nunca se compensa con
  cámara. Pendiente de veredicto: reservorios a x1.35 (~26 celdas) o x1.6
  (~30), maquetas de la R109.
- **Jerarquía sagrada**: las ruinas heredadas TORREAN — el depósito (19
  celdas) casi te dobla, el maestro te saca cabeza. T=14 quedó prohibida por
  competir con las máquinas.
- **La colisión no cambió** con la talla visual (9→12): si el roce con
  pasajes lo pide, se revisa DESPUÉS y a propósito, nunca de contrabando.

## 4. EL REPARTO

- **Cesar:** modelo único (imagen→3D o a mano) · rig · 6 animaciones ·
  decisión estética del tratamiento al ver pruebas · el PNG de prueba de
  cámara.
- **Agente:** ~~pruebas de cámara/zoom + composite sobre escena real~~
  (HECHAS en R108: talla 12 y encuadres sellados, ver §3.5) · el MANUALITO de entrega (lienzo, fps, nombres, pivote, máscara,
  escena de render con luces fijas) · el horno de render en Unity
  (FBX→secuencias→sheets, repetible) · importador sin halos · el
  ApprenticeController nuevo · utilería/glow/teñido · script del tratamiento
  (con variante pixel el día que se quiera comparar).

## 5. LO QUE ESTA DIRECCIÓN RETIRA

El imp morado como personaje (placeholder en retirada honrosa) · «pixel art»
como acabado asumido en cualquier texto anterior · cualquier plan de tres
modelos/tres texturas (es UNO con máscara).
