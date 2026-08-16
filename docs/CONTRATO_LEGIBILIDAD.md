# CONTRATO CONGELADO — EL TALLER QUE SE EXPLICA SOLO (Playtest 26)

Dos encargos paralelos: M = máquinas y plano, H = consejos y diario.
El feedback de Cesar (playtest 25), literal: *"no queda claro dónde van las
cosas, dónde se reciben, si debería meter limo en todas o el resultado, o si
esa es mi decisión... hay que trabajar mucho en las máquinas para que se
entienda su funcionamiento esperando [zona] de carga/descarga... el inicio
tiene que mejorar SIN CARTELES, específicos en cada cosa, hay que ser más
ingenioso en cómo expresar la idea colectiva sobre estas máquinas para el
público de a pie, si se ocupa más espacio no pasa nada... los consejos están
pasando muy rápido y aturde, tampoco sé si los puedo relanzar después y
desapareció lo de poder saltar a otro."*

## 0. LA TESIS DE LA RONDA

Una máquina legible responde TRES preguntas con su pura forma, sin un solo
cartel: ¿DÓNDE metes? (una boca), ¿QUÉ le sirve? (la boca te lo señala),
¿DÓNDE queda el resultado? (un recipiente enmarcado). Y la geografía del
taller cuenta el proceso entero: la materia FLUYE de izquierda a derecha,
de lo crudo a lo entregado.

## 1. LA GRAMÁTICA VISUAL (encargo M — vale para TODA máquina, presente y futura)

1. **EMBUDO = ENTRADA DE MATERIA.** Toda boca que recibe materia del frasco
   es un EMBUDO de latón de boca ancha, arriba de la cámara de trabajo.
   Mismo sprite-familia en todas las máquinas: se aprende UNA vez.
2. **BRASERO = ENTRADA DE COMBUSTIBLE.** La única otra boca que existe. Cesto
   de hierro oscuro con rescoldo (glow cálido animado) al pie del Crisol.
   Jamás se confunde con un embudo: otra forma, otra altura, otro color.
3. **CUBETA ENMARCADA = AQUÍ QUEDA EL RESULTADO.** Todo recipiente de trabajo
   lleva un marco de latón de 2px con remate — se lee "contenedor", no
   "agujero en el suelo". Lo que sale de un proceso REPOSA a la vista dentro
   de su cubeta hasta que el jugador lo aspira.
4. **EL VERBO VIVE EN EL CUERPO.** Crisol = panza de horno + glow inferior +
   CHIMENEA que suelta bocanadas de humo SOLO cuando quema combustible (el
   estado del aparato, contado por el cuerpo). Prensa = dos mandíbulas
   macizas + husillo/tornillo superior (nadie ha dudado jamás de qué hace un
   tornillo de banco). Banco de chispa = dos electrodos + ARCO visible al
   analizar + lámpara en su poste. Columna = vidrio alto con marcas de nivel
   horizontales cada 5 celdas. Ensayo = pedestal ceremonial más alto, con
   brasero propio incorporado (es EL examen: se ve importante).
5. **LA BOCA TE CONTESTA (affordance glow).** Cuando el jugador está a ≤10
   celdas con el frasco cargado del material M, cada boca PULSA suave
   (halo/brillo, ~1 Hz) SOLO si M le sirve a esa boca:
   - Embudo del Crisol: cualquier líquido o polvo (M != Empty y arquetipo
     Liquid/Powder).
   - Brasero: `Universe.EsCombustible(M)`.
   - Lecho de la Prensa: `Universe.Prensa(M)` es Compactar o Reventar.
   - Ranura del Banco: `MaterialId.EsBaseEstado(M)`.
   - Ensayo: hay pedido activo de AguantaCalor/Conduce.
   Esto responde EXACTAMENTE la duda de Cesar ("¿meto limo en todas?"): el
   taller contesta señalando, sin un cartel. Implementación central única
   (helper en MaquinariaSprites o clase pequeña compartida), no cinco copias.
   Coste: un sondeo de proximidad+material por máquina cada ~0.25s, jamás
   por frame; el pulso es un SpriteRenderer.color con seno, cero allocs.
6. **NADA DE CARTELES NUEVOS.** Los rótulos de foco existentes (E) se quedan
   (son HUD de interacción, no explicación); ningún texto pintado en el
   mundo explica una máquina.

## 2. EL PLANO NUEVO (encargo M) — "LA LÍNEA DEL TALLER"

El cuarto CRECE hacia la izquierda: `CuartoX0` 248 → **232** (ancho 110 →
126; Cesar: "si se ocupa más espacio no pasa nada"). Todo lo demás del marco
igual. La línea, de izquierda a derecha (= el proceso entero):

```
 x234        x252..270      x278..290   x296..303  x310..318   x326..336   x340→
 FUENTES  →  CRISOL      →  PRENSA   →  COLUMNA →  CHISPA   →  ENSAYO   →  pasillo→TOLVA
 (caños      (brasero|panza  (mandíbulas (vidrio    (electrodos (pedestal    (entrega)
  agua y      +embudo        +husillo     alto)      +lámpara)   ceremonial)
  limo, cada  +chimenea)     +lecho)
  uno con su
  PILA de
  recogida)
 crudo    →  transformar  →  forzar   →  observar → revelar  →  examinar →  entregar
```

Constantes: M actualiza `CrisolX=258`, `PrensaX=282`, `ColumnaX0=296`,
`BancoChispaX=312`, `EnsayoPlintoX=330`, y añade `PilaAguaX0/PilaLimoX0`
(pilas de recogida talladas bajo cada caño, 6 de ancho, 3 de hondo, marco de
piedra — el chorro deja de perderse por el suelo). Los caños se separan en
vertical como hoy (agua arriba, limo abajo) pero cada uno vierte SOBRE SU
PILA. El pasillo a la Tolva no se toca. Números finos: decisión de M,
documentando holguras (≥8 celdas entre estaciones).

**Reparto de responsabilidades (regla 47, explícito):** SimLevelBuilder
talla TODA la mampostería (pilas, marcos de piedra, columna) — las máquinas
DEJAN de tallar su propia mampostería en Init (el auto-tallado del playtest
25 se retira; una sola fuente de verdad del plano). Las máquinas solo ponen
sprites y lógica sobre las constantes.

## 3. CONSEJOS QUE NO ATURDEN (encargo H)

1. **Ritmo**: 12s por consejo (antes 8-9 — "pasan muy rápido y aturde").
2. **Saltar**: tecla **N** = siguiente consejo YA ("desapareció lo de poder
   saltar" — vuelve como mecanismo explícito: offset manual sobre el índice
   por tiempo, clampeado, respetando `UiStyles.EscribiendoTexto`).
3. **Ocultar/mostrar**: **H** sigue siendo el interruptor; verificar que
   funciona en el modo laboratorio y que al REACTIVAR no te saltas nada
   (retoma donde iba).
4. **Progreso**: "consejo 3/10" pequeño y tenue en la esquina de la placa —
   saber cuántos quedan quita ansiedad.
5. **RELEER ("¿los puedo relanzar después?")**: sección nueva CONSEJOS en el
   diario (JournalHud) que lista `HintSystem.PistasMostradas` — el hook
   existe desde el playtest 10 ESPERANDO ESTE CONSUMIDOR (ver su TODO).
   Los consejos ya mostrados se releen ahí para siempre; los no mostrados
   no se destripan.
6. La placa de consejos se OCULTA mientras el diario está abierto
   (`JournalHud.Abierto`) — dos capas de texto a la vez aturden.

## 4. PROPIEDAD DE ARCHIVOS

- **M**: `Game/MaquinariaSprites.cs`, `Game/Crisol.cs`, `Game/Prensa.cs`,
  `Game/BancoChispa.cs`, `Game/EnsayoMaestro.cs`, `Sim/SimLevelBuilder.cs`.
- **H**: `Game/HintSystem.cs`, `Game/JournalHud.cs`.
- NADIE toca: Universe, SimStepper, OrderSystem, Hornada, DayCycle,
  Bootstrap (las firmas de spawn no cambian; las máquinas siguen leyendo las
  MISMAS constantes por nombre), ni nada del modo clásico.

## 5. HECHOS COMPARTIDOS

Los de siempre (CLAUDE.md): IMGUI, sprites por código, cero allocs por
frame, sondeos con acumulador, comentarios en español con el porqué,
regla 48 (verbo + consumidor), regla 15 (comentar, no borrar). El humo de la
chimenea y el arco de la chispa son SPRITES animados del aparato (capas
MaquinariaSprites con frames o color animado), NUNCA materiales del grid —
no ensucian la sim ni el diario.

## 6. DEFINICIÓN DE HECHO

Compila sin warnings; la gramática se aplica a las CINCO estaciones; el glow
de affordance responde al material real del frasco; los consejos se saltan
con N, se releen en el diario y marcan progreso. Al terminar: archivos,
resumen, constantes nuevas, y toda decisión fuera del contrato marcada.
