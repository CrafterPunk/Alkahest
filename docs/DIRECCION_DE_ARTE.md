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

## 4. EL REPARTO

- **Cesar:** modelo único (imagen→3D o a mano) · rig · 6 animaciones ·
  decisión estética del tratamiento al ver pruebas · el PNG de prueba de
  cámara.
- **Agente:** pruebas de cámara/zoom + composite del arte P1 sobre escena
  real · el MANUALITO de entrega (lienzo, fps, nombres, pivote, máscara,
  escena de render con luces fijas) · el horno de render en Unity
  (FBX→secuencias→sheets, repetible) · importador sin halos · el
  ApprenticeController nuevo · utilería/glow/teñido · script del tratamiento
  (con variante pixel el día que se quiera comparar).

## 5. LO QUE ESTA DIRECCIÓN RETIRA

El imp morado como personaje (placeholder en retirada honrosa) · «pixel art»
como acabado asumido en cualquier texto anterior · cualquier plan de tres
modelos/tres texturas (es UNO con máscara).
