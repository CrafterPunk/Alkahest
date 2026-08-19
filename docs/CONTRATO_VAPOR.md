# CONTRATO CONGELADO — EL VAPOR VIVO (Playtest 41)

Feedback literal de Cesar tras jugar el 40: *"no entendí por qué no pude
hervir el agua... me confundió el cambio de color del limo que al calentarlo
se pone amarillo pero al recogerlo resulta que es azul... yo esperaba ver el
agua evaporándose, y la animación de vapor es muy mala. ¿Será muy costoso
hacer que realmente las partículas suban por el calor y no animación, y que
se comporten como gas? o sea que si topan un muro en su ascenso no se
estanquen ahí sino que intenten seguir subiendo esparciéndose hacia los
lados... es más, diría que no suban en vertical perfecta."*

Dos encargos SECUENCIALES (no paralelos): **S = sim** (Sonnet, en el
sandbox) primero; después **V = visual** (Opus CON OJOS en el PC real,
desplegando/jugando/capturando — la calibración de color y el teatro del
vapor se juzgan mirando, no leyendo).

## 0. DIAGNÓSTICO (hecho, con causas raíz — no re-diagnosticar)

1. **El agua no hierve en el crisol**: `Crisol.DecidirHornada` corta con
   `if (!MaterialId.EsBaseEstado(entrada)) return false;` — solo Limo y
   base×estado tienen rama. La promesa del MANUAL_MAQUINAS ("sin nada:
   hierve agua y limo") se perdió en la reescritura por hornadas (pt27).
2. **"Amarillo al calentar, azul al recoger"**: `SimRenderer.ComputeCellColor`
   tiñe por temperatura: por encima de raw 150 lerp hacia (255,214,140)
   hasta t01=1 — a fuego de brasero el polvo celeste pierde TODO su matiz.
   La incandescencia real a 200-400°C es un rescoldo tenue, no un borrado.
3. **El vapor "sube en vertical perfecta y se estanca bajo muros"**:
   `SimStepper.ProcessGas` sube recto si hay Empty arriba; bajo techo, el
   lateral re-sortea DIRECCIÓN CADA TICK (aunque prioriza el lado caliente)
   → tiembla en el sitio en vez de derivar buscando la salida.

## 1. ENCARGO S — el gas que busca el cielo (Sonnet)

Archivos de S: `Sim/SimStepper.cs` (ProcessGas y ayudantes),
`Game/Crisol.cs` (rama de hornada del agua). NADA MÁS.

### 1a. La hornada "hirviendo" (agua → vapor)

- Rama nueva ANTES del corte de EsBaseEstado: `entrada == MaterialId.Water`
  y `cima >= boilsAt del agua` → `salida = MaterialId.Steam`, verbo
  **"hirviendo"**, condición `CondicionCalor()`. Con cima insuficiente (no
  pasa con tier0=120 > ~100°C, pero por robustez) → false como siempre.
- El cierre de hornada convirtiendo la cámara EN Steam ya es "ver el agua
  dejar la olla": verificar que el teatro existente de vapor
  (`VaporPorCeldas`/`PaintStable`) no lo DUPLIQUE para esta hornada (si la
  salida ya es Steam, el empuje extra sobra — documentar la decisión).
- La Solución ya tiene su "evaporando" — no tocarla.

### 1b. Convección creíble en ProcessGas

La tesis: un gas tiene INERCIA DE INTENCIÓN — quiere subir, y cuando no
puede, DERIVA con rumbo sostenido hasta encontrar cielo, no tiembla.

- **Ondulación en ascenso libre**: con Empty arriba, ~30% de las veces
  intentar la DIAGONAL-ARRIBA hacia el rumbo coherente (abajo) antes que la
  vertical. Nada de vertical perfecta: una columna de vapor debe serpentear.
- **Rumbo coherente (sin estado nuevo)**: la dirección lateral sale de un
  hash determinista de BAJA FRECUENCIA — p. ej.
  `XorShift.FromCell(_tick >> 4, x >> 3, y >> 3, SALT)` → ±1 — de modo que
  una misma celda mantiene el rumbo ~medio segundo y celdas vecinas
  comparten corriente (se lee como VIENTO, no como ruido). La deriva
  térmica existente (hacia el vecino más caliente) queda como DESEMPATE
  cuando hay gradiente, no como única fuente.
- **Bajo techo, escapar — no estancarse**: bloqueada la vertical, en orden:
  (1) diagonal-arriba en el rumbo, (2) diagonal-arriba contraria,
  (3) lateral en el rumbo (con la probabilidad de bolsa actual ~60%),
  (4) lateral contraria. En cuanto un paso lateral deja Empty encima, el
  siguiente tick sube solo — el gas RODEA el obstáculo y sigue subiendo,
  que es exactamente lo que pidió Cesar. El medio-decaimiento bajo techo
  del pt39 (regla 55a) NO se toca: las bolsas siguen siendo mortales.
- Salts NUEVOS con grep previo (los del 39 llegaron hasta 547). Cero
  allocs. Determinismo intacto (multi depende de él).
- **Banco**: correr los 6 escenarios antes/después (mismo flujo
  /home/claude/bench); presupuesto +10% sobre el coste de gases, los
  escenarios sin gases no deben moverse fuera de ruido.

## 2. ENCARGO V — la mirada (Opus con ojos, DESPUÉS de integrar S)

Archivos de V: `Sim/SimRenderer.cs` (solo el bloque de tinte térmico),
`Game/ParticulasFx.cs` (vaho/motas), `Game/MaquinariaSprites.cs` y
`Game/Alambique.cs` (SOLO teatro visual del vapor/serpentín; la mecánica de
condensación no se toca), `Game/Crisol.cs` (solo bocanadas/chimenea si hace
falta). Herramientas: computer-use sobre el PC real de Cesar (Unity abierto,
regla 6/53: desplegar por zip, Ctrl+R, jugar, capturar, iterar — ciclos
cortos, mirar SIEMPRE antes y después).

- **Incandescencia legible (fix del "amarillo engañoso")**: el tinte
  térmico deja de borrar el matiz — techo de mezcla (~45-55% máx. incluso a
  raw 255) y arranque más tardío o curva más suave, calibrado CON CAPTURAS:
  el criterio de hecho es que el polvo celeste A FUEGO PLENO se lea
  "celeste al rojo", nunca "otro material amarillo". Fotografiar
  antes/después con la misma escena.
- **El vapor visible**: con la convección nueva de S ya moviendo el gas de
  verdad, calibrar el TEATRO encima: vaho de ParticulasFx (tamaño, alfa,
  rizo — que acompañe la corriente, no que la tape), y el vapor del
  alambique/chimenea del crisol (MaquinariaSprites) que hoy Cesar llama
  "animación muy mala": si el sprite-teatro compite con el gas real de la
  sim, RETIRARLO o reducirlo a acento — el gas real es el protagonista
  ahora. Decisión con ojos, documentada.
- Verificar EN PANTALLA la escena completa de Cesar: agua al crisol → E →
  "hirviendo" → el vapor escapa por la boca, serpentea, rodea la bóveda y
  se embolsa/condensa — capturas del antes/después para el informe.
- Presupuesto: nada de esto toca el tick; overlay y sprites como siempre
  (cero allocs por frame).

## 3. HECHOS COMPARTIDOS

CLAUDE.md entero; reglas 7 (salts únicos), 15, 48, 53 (compilar en el
sandbox antes de desplegar — comando en CONTRATO_SEMILLA.md §4), 54, 55.
El multi y el modo caótico no cambian de comportamiento más allá de lo
descrito. El arnés debe seguir compilando.

## 4. DEFINICIÓN DE HECHO

- **S**: agua en el crisol + E → "hirviendo" y la cámara se vacía en vapor
  visible; una columna de humo/vapor libre serpentea; bajo un saliente, el
  gas deriva con rumbo y escapa por el borde en vez de temblar; banco
  corrido con tabla antes/después dentro de presupuesto.
- **V**: capturas antes/después de (a) polvo celeste al fuego pleno
  conservando matiz, (b) la escena agua→vapor completa, (c) el alambique
  con su vapor nuevo; decisión sprite-vs-gas documentada; Cesar puede
  reconocer CADA material en caliente.
