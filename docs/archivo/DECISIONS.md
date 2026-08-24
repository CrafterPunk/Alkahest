# ALKAHEST — Fase 1: encontrar el juego

*Documento de decisiones (Fable, orquestador). Todas las decisiones son provisionales pero operativas: se cambian con evidencia, no con opinión.*

## 1. Nombre provisional
**Alkahest** — el disolvente universal de la alquimia clásica: lo que promete disolver cualquier ley de la materia. Corto, evocador, buscable.

## 2. Fantasía central en una frase
**"Caes en un universo cuyas leyes de la materia nadie conoce; experimenta, ponles nombre y domestícalas hasta cultivarlas."**
No haces pociones: aprendes física alienígena hasta que trabaja para ti.

## 3. Perspectiva
**2D vista lateral** (corte transversal del taller). Es la única perspectiva donde la simulación celular *es* el juego: la gravedad, los líquidos estratificándose, los gases subiendo y el fuego trepando son legibles sin traducción. Isométrico o 3D obligarían a esconder la simulación tras efectos; aquí la simulación ES la animación.

## 4. Estilo visual
**"Materia luminosa en cueva oscura"**: fondo de taller en carbones cálidos muy oscuros, sustancias saturadas y ligeramente emisivas (el fuego y los organismos brillan), render celular suavizado por filtrado bilinear + jitter por celda (orgánico, no píxel-art duro). Personajes: **aprendices voladores** (imps/familiares con túnica) — sprites simples de 2-3 formas, sin animación esquelética, flotan con bobbing. Coherente, barato, expresivo.

## 5. Número ideal de jugadores
**1–3, óptimo 3.** Razonamiento: el loop tiene tres roles naturales que emergen solos — *experimentar* (probar mezclas nuevas), *producir* (mantener cultivos/pedidos en marcha) y *logística* (mercado, transporte, limpieza de accidentes). Con 3 personas cada rol tiene dueño espontáneo y la transmisión de conocimiento ("¡no toques eso, explota con calor!") es constante. Con 4 el taller de una pantalla se satura y aparece trabajo duplicado; preferimos profundidad de interacción a cabeza extra. La arquitectura no impide 4; el balance objetivo es 1–3.

## 6. Single player
Idéntico loop sin recortes: los pedidos escalan por jornada según nº de jugadores, y en solitario el ritmo es más contemplativo (menos caos, más ciencia). El conocimiento sustituye a la mano de obra: la automatización/cultivo es LA herramienta del jugador solitario.

## 7. Core loop
```
PEDIDO por EFECTO ("algo que arda 10 segundos", "un gel que disuelva metal")
   ↓ no hay receta: hay hipótesis
EXPERIMENTAR en cubas: mezclar, calentar, enfriar, observar
   ↓ la simulación revela comportamiento (visible, no texto)
DESCUBRIR una sustancia/ley → NOMBRARLA (etiqueta del grupo)
   ↓
REPRODUCIR el resultado a demanda → ENTREGAR pedido → Favor
   ↓ el Favor compra materiales base y aparatos…
DOMESTICAR: montar un cultivo/proceso que produce la sustancia sola
   ↓ pedidos más duros presuponen lo aprendido
```

## 8. Estructura de sesión
**Tiempo real + jornadas.** Una run = un universo (una seed) = 3 jornadas de ~6 min efectivos (timer blando). Mañana: llegan pedidos; día: experimentación libre; cierre: evaluación, Favor, resumen. Win: alcanzar el Favor objetivo del Maestro en 3 jornadas; lose: dos jornadas sin entregar nada. Corto, repetible, con cierre natural — formato demo perfecto y compatible con "una run, unas leyes".

## 9. Papel de las reacciones físicas
Son el **lenguaje del juego**: la información que otros juegos dan por texto aquí se da por comportamiento (el aceite flota → es menos denso; burbujea al calentar → tiene gas; el humo verde pica → tóxico). Regla de diseño: *ninguna propiedad importante sin manifestación visible*. La simulación regala las animaciones: verter, arder, cristalizar, crecer son estados del autómata, no clips.

## 10. Universos con leyes distintas
Dos capas sobre una base fija:
- **Base estable** (aprendizaje transferible entre runs): gravedad, densidad, fases, fuego existe, los gases suben. Sin esto no hay modelo mental posible.
- **Leyes de la run** (seed): (a) la *matriz de identidades* — qué sustancia concreta tiene qué densidad/inflamabilidad/temperaturas de fase/pareja reactiva, barajada dentro de arquetipos coherentes; (b) 2–3 *Edictos* — torsiones globales nombrables ("en este universo el frío acelera el crecimiento", "los metales beben electricidad"). Coherente e aprendible: dentro de la run las leyes no cambian jamás. La wiki no sirve; el cuaderno del grupo sí.

## 11. Descubrimiento
Toda sustancia no básica llega **sin identificar**: se muestra su apariencia y comportamiento, nunca sus stats. El juego registra automáticamente *observaciones* ("la viste arder", "flota sobre el agua-roja") en el diario al presenciarlas — el conocimiento se gana mirando, no abriendo menús.

## 12. Etiquetado
Apuntar + tecla: bautizas la sustancia ("moco eléctrico"). El nombre es del grupo (sincronizado en coop), aparece en tooltips, diario y pedidos cumplidos. Interno: id real + propiedades, visibles solo en modo dev. El lenguaje privado del grupo es contenido emergente gratis.

## 13. De descubrir a reproducir
Las recetas no se regalan: el diario registra el *contexto* de cada descubrimiento (qué había en la cuba, temperatura aproximada) como pista imperfecta. Reproducir = volver a crear las condiciones. La fiabilidad llega por comprensión, no por menú de crafteo.

## 14. Automatización
Escalera de domesticación de 3 peldaños (la demo cubre los 3 con una especie):
1. **Manual**: descubres que el Vivium crece consumiendo Nutriente en un rango de temperatura.
2. **Instalación**: cuba + placa caliente ajustada + goteo de nutriente = criadero que produce solo.
3. **Explotación**: recolectas excedente para pedidos → dejas de comprar.
(Post-demo: bombas/tuberías/dosificadores para cadenas completas.)

## 15. Economía
Mínima y al servicio del arco: los pedidos dan **Favor**; el Favor compra materiales base en el dispensario y aparatos. Diseñada para volverse obsoleta: cada ley domesticada elimina una compra. El mercado de ofertas secuenciales queda ANOTADO como extensión (encaja para materiales raros) pero fuera del slice — no aporta al momento "lo entendí".

## 16. Máquinas esenciales (por verbo)
| Verbo | Aparato | Slice |
|---|---|---|
| contener/mezclar | Cubas de piedra | ✅ |
| transportar/verter | Frasco del aprendiz (succiona/vierte) | ✅ |
| calentar | Placa ígnea (bajo cuba, regulable) | ✅ |
| enfriar | Piedra gélida | ✅ |
| dispensar | Grifos de básicos (cuestan Favor) | ✅ |
| entregar | Tolva del Maestro (evalúa propiedades) | ✅ |
| filtrar/separar | Tamiz | solo si sobra tiempo |
Nada más: cada aparato nuevo debe comprar un verbo nuevo, no decoración.

## 17. Voz
**No en el slice.** Taller de una pantalla compartida: Discord externo ofrece lo mismo que voz integrada; no justifica su coste. La capa `IVoiceService` de la plantilla queda intacta; si el juego evoluciona a talleres multi-sala/pantalla-por-jugador, voz por proximidad se reevalúa (ahí SÍ pagaría).

## 18. Mayor riesgo técnico
**Multiplayer × simulación celular.** Ingenuo (celda a celda por red) es inviable. Plan: simulación **solo en el host**, clientes reciben deltas comprimidos por chunks despiertos (RLE) a 10–15 Hz + eventos; el determinismo del autómata (RNG XorShift, orden fijo) deja abierta la puerta a lockstep si el ancho de banda no da. **Medir antes de decidir** — prototipo de medición tras validar el single player. Riesgo secundario: rendimiento del autómata en C# → mitigado con chunks dormidos; Burst como reserva.

## 19. Mayor riesgo de diseño
**Que experimentar se sienta lotería y no ciencia.** Si el jugador no puede formar modelo mental, el juego colapsa en Infinite Craft sin gracia. Mitigaciones: base estable (§10), apariencia correlacionada con propiedades (familias de color/comportamiento), observaciones automáticas en el diario, pedidos-tutorial que guían las primeras hipótesis, y los Edictos *enunciados como rumor* ("el Maestro murmura que aquí el frío hace cosas raras") para sembrar hipótesis sin resolverlas.

## 20. Orden de prototipado
1. **La cuba y el fuego** (M1): sim celular con densidad/fases/fuego — si esto no fascina en 60 segundos de juguete, nada posterior lo salvará.
2. **La mano** (M2): frasco succionar/verter + calor/frío — ¿manipular materia es placentero?
3. **La ley oculta** (M3): seed + reacciones + crecimiento del Vivium — ¿descubrir se siente ciencia?
4. **El motivo** (M4): pedidos por efecto + diario/nombres + jornadas — ¿hay juego completo?
5. **La cara** (M5): dirección visual + pulido — ¿parece un juego?
6. Build + medición de red (M6/documentado).

---
*Criterio de éxito de la noche: que un tester diga "espera, creo que ya entendí cómo funciona esto" sin que nadie se lo explique.*
