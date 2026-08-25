# PLAN — PRÓLOGO CAPÍTULO 2: el orden, la tubería y el cincel (R83+)

*(Espec de Cesar, 24-ago-2026, integrada y decidida. Método: plan → ojos de Opus →
fases con verificación viva. Los números finos van al GuionDelPrologo como siempre.)*

## El arco nuevo (dónde se inserta)

Hoy el prólogo termina: ...Recompensa (emerge el depósito) → LLÉNALO. (agua) → BIEN. →
amanecer → Trueque. El capítulo 2 se INSERTA antes del amanecer:

```
LLÉNALO. (agua) → BIEN.
  → beat SEGUNDO DEPÓSITO: emerge el tanque del LODO junto al cráter → LLÉNALO. (lodo)
  → beat EL REORDEN (cinemática): el taller se ordena — fondo del castillo, estantería
    central de piso a techo, los DOS tanques reubicados con su tubería al suelo,
    refill lento desde el mínimo. La fogata sigue viva.
  → beat LA OBRA (cincel rediseñado): CIERRA. — sellar la grieta del derrumbe (techo),
    reparar los huecos del suelo del ala izquierda (piso), y ABRE. — tallar un vano
    marcado (destruir). Descubren construir/destruir como mecánica.
  → amanecer → Trueque (el fin de siempre).
```

## Beat 1 — EL SEGUNDO DEPÓSITO (llénalo de barro)

- `DepositoDeAgua` se PARAMETRIZA por material dueño (hoy `MaterialId.Water` está
  horneado en `AguaDentro`/carga/placa): `Init(sim, byte materialDueno, ...)`. El conteo
  del lodo suma lodo + barbotina (la lección del cuenco: mojar el lodo no castiga).
- Emerge con la MISMA cinemática del primero (sacudida corta + foco), en el flanco del
  cráter (zona x386-392 aprox — cerca de la gotera: recolectas donde brota). SIN carga
  inicial (nace vacío: el mundo no regala lo que te pide) y SIN refill aún.
- Voz: reutiliza `vozLlenalo`; la placa distingue sola ("LLÉNALO — n/m" cuenta lodo).
  Meta en guion: `llenarDeposito2Meta` (≈36: el lodo gotea más lento que la cascada).
- Marcador de escena nuevo `Deposito2` en PrologoEscenografia (mismo patrón del primero).

## Beat 2 — EL REORDEN (la cinemática del capítulo)

La secuencia, en tiempo real de la sim (nada de pantalla en negro):

1. BIEN. → pausa breve → la voz dice **ORDEN.** (nueva palabra del guion).
2. Foco cinematográfico al centro del taller + los dos tanques SE HUNDEN (la fase
   Emergiendo en reversa — misma tecnología).
3. EL BARRIDO: la materia suelta fuera de recipientes (el desastre del juego libre) se
   drena celda a celda con un destello breve — el mismo drenado del cuenco del Maestro,
   aplicado al suelo del taller. Es un acto del MUNDO, como el derrumbe: cinemática, no
   física cotidiana. La fogata y el contenido de los recipientes NO se tocan.
4. EL FONDO CAMBIA: crossfade ruina → ladrillos del castillo profundo (WorkshopBackdrop
   gana `TransicionAFondoTaller(seg)`: pinta el fondo clásico en un sprite nuevo y funde
   alfa). "Estábamos reconstruyendo porque algo pasó" paga su promesa aquí.
5. LA ESTANTERÍA DE PISO A TECHO se levanta al centro, SOBRE LA CICATRIZ del cráter
   sellado (x≈386-404): montantes de madera + repisas (obra registrada, regla 38). La
   herida del mundo se convierte en el mueble — el tema del juego en una imagen.
6. Los DOS tanques RE-EMERGEN reubicados en sus bahías de la estantería (agua a la
   izquierda, lodo a la derecha), cada uno con su **TUBO LATERAL DERECHO HASTA EL SUELO**
   (sprite nuevo `TanqueTuboLateral(altoCeldas)`: codo en la base del vidrio, caña
   vertical, boca enterrada — "se reabastece desde el suelo", línea de Cesar).
7. REFILL LENTO: ambos arrancan del mínimo y gotean hacia arriba por dentro (celdas
   PaintStable junto al tubo, cadencia `refillSeg` del guion, hasta `refillTope`). El
   primer refill infinito del juego, visible gota a gota.
- Implementación: los tanques NO se "mueven" (regla 36: Init no es idempotente): el
  director los DESTRUYE y crea instancias nuevas en las posiciones del reorden — es una
  reconstrucción cinematográfica, no un drag. Los marcadores de escena `Deposito`/`
  `Deposito2` conservan la autoridad de la posición INICIAL; dos marcadores nuevos
  (`DepositoFinal`, `Deposito2Final`) dan la del reorden — el hermano de Cesar puede
  mover las cuatro desde el editor.

## Beat 3 — LA OBRA (el cincel, rehecho)

**El problema (visto en playtests):** el cincel es un MODO con tecla-toggle (C) más un
sub-toggle interno (X: piedra/piso). La gente lo activa, se olvida, y el frasco "no
responde". El estado invisible es el enemigo.

**La decisión (D1): el cincel deja de ser un modo y pasa a ser un GESTO — se SOSTIENE.**
- **Mantener C = cincel en mano. Soltar C = el frasco vuelve.** No hay nada que recordar:
  el estado vive en el dedo. Precedentes: los cuasimodos clásicos (la barra espaciadora
  de Photoshop), y Terraria — que tras años de quejas ofrece su Smart Cursor también en
  modo "mantener" por exactamente esta razón; Minecraft/Vintage Story resuelven el mismo
  problema haciendo la herramienta VISIBLE EN LA MANO, nunca un modo invisible.
- **La mano enseña la herramienta**: mientras C está sostenida, el imp carga el cincel
  (sprite pequeño en CarryAnchor) y la retícula cambia a pico. El modo se VE en el mundo.
- **La X sale del prólogo** (D1b): con el cincel en mano, clic izq = TALLAR, clic der =
  COLOCAR PISO ESTRUCTURAL, directo. La opción "rellenar piedra cruda" se retira del
  camino del jugador (el piso ya reemplaza roca: es superconjunto; regla 15 al retirarla
  del flujo — la X queda como atajo dev hasta decidir su muerte).
- El toggle clásico NO sobrevive como alternativa: reintroduciría el olvido. Si un
  playtest futuro pide manos-libres para obras largas, la respuesta será doble-C para
  FIJAR con un indicador grande y persistente — decisión aplazada, no tomada.

**La justificación diegética (D2) — cerrar la herida:** tras el reorden, el lodo ya
llega por tubería: la gotera del techo YA NO ES LA FUENTE, es una herida abierta. La voz
dice **CIERRA.** y la tarea es triple, toda sobre geografía que ya existe:
1. **TECHO**: sellar la grieta del derrumbe con piso estructural (zona marcada con luz
   tenue; valida celdas estructurales colocadas en el rect de la grieta).
2. **PISO**: reparar los huecos de la erosión del ala izquierda (misma validación).
3. **ABRE.** (destruir): tallar un vano marcado en el tabique/escombros del ala — el
   descubrimiento de que el cincel también QUITA.
Fichas del tutorial: `[C — mantén]` `[CLIC DER — coloca]` (y `[CLIC IZQ — talla]` en el
vano), validadas por RESULTADOS como siempre. Al completar: amanecer + Trueque.

## Los sprites (guía de Cesar, ref 2)

- `TanqueTuboLateral(altoCeldas)`: cobre con juntas y pátina (paleta existente), codo a
  la base del vidrio, caña vertical por la DERECHA hasta tocar el suelo, boca con brida.
  Va al prefab DepositoVisual como hijo "Tubo" (la escena manda en su transform).
- La estantería piso-techo: montantes + repisas de madera (paleta de las Baldas), sprite
  horneable (menú 6) + obra en sim para las repisas portantes.
- El tubo corto trasero ya existente (`TanqueTubo`) queda para variantes menores.

## Fases y verificación

- **R83 (esta): FASE A** — DepositoDeAgua parametrizado + segundo depósito + beat
  LLÉNALO-lodo completo y verificado en vivo.
- **R84: FASE B** — la cinemática del reorden entera (hundir, barrer, fondo, estantería,
  re-emerger con tubos, refill). La más larga; ojos de Opus sobre la puesta en escena.
- **R85: FASE C** — cincel sostenido + la Obra (CIERRA./ABRE.) + fin nuevo. Ojos de Opus
  sobre la orientación (como en R81).
- Cada fase: compile fiel, deploy, sondas + capturas, HISTORIAL, cmd. Multi intacto (el
  prólogo sigue solo-single; el cincel sostenido respeta las guardas simétricas R37).

## Decisiones tomadas en este plan (para discusión si alguna chirría)

D1 cincel sostenido (sin toggle) · D1b X fuera del flujo del jugador · D2 la
justificación es CERRAR LA HERIDA + reparar el suelo + un vano de destrucción · D3 la
estantería se levanta sobre la cicatriz del cráter y los tanques viven en sus bahías ·
D4 el barrido es un acto cinematográfico del mundo (drenado con destello, recipientes y
fogata intactos) · D5 el tanque del lodo nace VACÍO y sin refill hasta el reorden.

---

## REVISIÓN OPUS (24-ago) — 19 cambios integrados; los BLOQUEA-PLAN, en corto

La auditoría con las constantes reales tumbó la primera geometría. Lo integrado:

- **El hueco poza|cráter mide 6 columnas** (x386-391), no 8: el depósito del lodo pasa a
  ser un **SILO 6x9** (interior 4x9=36; meta 24) — silueta distinta = "esto es para lo que
  se AMONTONA" sin una palabra (converge con la línea silo/tanque de la hoja R78).
- **La estantería es x386-401 con BAHÍAS APILADAS** (el fogón x402-412 es obra: no se
  pisa): agua abajo (tubo corto), lodo arriba (tubo largo) — y el tubo "hasta el suelo"
  EXIGE tanque elevado: el mueble es la premisa del tubo, no decoración. Cada tanque
  elevado pinta su propio suelo. Obra registrada PIEZA A PIEZA (jamás bounding box: el
  fantasma R69).
- **El softlock R78 §1 se cierra ANTES del segundo recipiente** (con dos vasijas gemelas
  el error se vuelve simétrico): placa honesta SOLO cuando la meta está bloqueada
  ("· sobra AGUA — aspírala"; la purga ES el frasco, la placa solo lo dice). Gateada por
  guion (`placaAvisaEstorbo`) por si Cesar la veta también así.
- **ContarLodoEnCrater excluye el rect del silo**: el lodo atesorado no es montículo (si
  no, la histéresis pausa la gotera con el cráter vacío — softlock lento).
- **El barrido RECOGE, no borra**: cada celda suelta reaparece DENTRO del tanque que le
  corresponde — ese es el mínimo del que arranca el refill ("el taller guardó lo mío", no
  "me borraron el desastre"); lista blanca de rects (poza/cuenco/hogar/fogón/cráter),
  jamás toca Stone/PisoEstructural (la obra del jugador), manantial y gotera en pausa
  durante el barrido.
- **Hundir un tanque = drenar → borrar muros → ActualizarObra(rect degenerado) → hundir
  visual → Destroy** (hoy el handle de obra se tira: quedarían obra fantasma + muros
  huérfanos + 84 celdas derramadas). El reorden LIMPIA su huella antes de construir (el
  PlanoOverlay R77 y el cincel libre pueden haber dejado roca o vacío ahí).
- **La gotera muere AL SELLAR EL TECHO** (`_lodoActivo=false`) y en ese instante arranca
  el refill del silo: el relevo de la fuente, visible en una sola pantalla. Hasta
  entonces, el lodo cayendo SOBRE la estantería nueva ES el marcador de la herida (nada
  de luz tenue — decisión R82; materia + placas de mundo).
- **La Obra se reordena PISO → TECHO → VANO** (aprender barato a los pies; el clímax con
  el gesto ya aprendido; la inversión al final) y el piso es una FRANJA (x342-369, y139:
  los "huecos" medidos son 3 celdas — no daban un beat). El VANO se talla en ROCA MADRE
  sin RegistrarObra y con destino (paso al ala), jamás en obra del mundo.
- **El cincel sostenido, endurecido**: `ModoActivo` se CALCULA por estado cada frame
  (C pulsada + todas las guardas; false en todo return temprano) — inmune a hot-reload y
  al diario abierto con C sostenida; **botones sucios simétricos** al entrar Y salir (el
  final natural de una picada es soltar C con el clic abajo: sin esto, el frasco aspira
  al instante lo recién construido); `OnApplicationFocus(false)` suelta todo;
  `Mudanza.ForzarSalida` solo si el cincel de verdad engancha; el Termómetro ESPERA (no
  muere). La X pasa a atajo dev ("piedra cruda") y `_rellenaPiso` es el default; la
  validación mira RESULTADOS (cero Empty en el rect), nunca el material.
- **Dos bugs vivos cazados de paso** (a Fase A por baratos): el anillo del cincel miente
  el alcance (pinta verde hasta 60 celdas, el cincel llega a 22 — usar ReachWorldCincel)
  y el docblock de Cincel documenta como pendiente un solapamiento frasco-cincel que
  Flask.cs:333 ya cerró (regla 49: retirarlo).
- Números observables por fase (R43): A = "placa del silo 24/24 con lodo real" ·
  B = "log del barrido: agua M→tanque, lodo K→silo, protegidas P" · C = "Empty en la
  grieta 8→0; franja x342-369/y139 completa".
- Fase B se parte en **B1** (hundir + barrido + fondo) y **B2** (estantería + bahías +
  tubos + refill): dos cinemáticas, dos verificaciones.
