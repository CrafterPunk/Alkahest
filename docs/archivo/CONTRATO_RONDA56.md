# CONTRATO CONGELADO — RONDA 56: LA VIDA ÚTIL DE LO DESCUBIERTO (vertical slice)

Diseño completo: docs/DISENO_VIDA_UTIL.md (leerlo ENTERO antes que esto).
Mandato de Cesar verbatim en esa cabecera. Dos encargos paralelos con
propiedad DISJUNTA y UNA costura contratada entre ambos.

## 0. HECHOS COMPARTIDOS
CLAUDE.md entero (en especial 8, 10, 12, 13, 15, 22/29, 36, 39, 43, 49,
51-57). Español latino. Cero allocs en OnGUI (strings al cambiar estado).
Compilar con /home/claude/compile_fiel.sh (EXIT=0). PROHIBIDO git stash.
Los ids de material se derivan SIEMPRE de MaterialId.MatDe/los ids con
nombre (Mortero=59, VidrioVerde=60, Lejia=61...) — jamás números mágicos.
Cantidades del contrato (regla 43, exactas): vitrales 40 vidrio / 12 licor
pardo / 20 mortero, recompensa 60; obra 24 cerámica (MatDe(1,Ceramico)) /
16 vidrio / 12 mortero, recompensa 40.

## 1. ENCARGO O — EL ENCARGO COMPUESTO Y SU TEATRO
Archivos EXCLUSIVOS: Game/OrderSystem.cs, Game/Order.cs, Game/OrdersHud.cs,
Game/SemillaCero.cs.

### 1a. OrderCompuesto
`Order` gana la forma COMPUESTA: hasta 3 componentes (targetMat, cantidad,
progreso) + un texto narrativo largo + un nombre corto ("LOS VITRALES DE LA
CAPILLA"). Decisión de implementación de O leyendo Order.cs real (Order es
readonly y se sustituye entera al recalcular, regla 13 — respétalo): puede
ser una subclase, un arreglo interno, o tres Orders hermanas con un
agrupador — lo que MENOS pelee con TryDeliverCell/MatchesOrder existentes.
La entrega en el Buzón progresa el componente que coincida con el material
vertido; al completarse las 3 líneas, el compuesto se completa (Favor 60 +
línea de cierre del Maestro vía el canal MaestroDice/TextoMaestro que ya
se replica en multi).

### 1b. El disparo
- UN JUGADOR: SemillaCero, al entrar en FinalAbierto (beat 6), encola el
  compuesto de los vitrales (texto ÍNTEGRO del diseño §1) y sube el flag
  estático `SemillaCero.FaseVidaUtil = true` (nuevo; false por defecto;
  resetearlo donde se resetean las estáticas del director — OnDestroy).
  La línea de la alacena del diseño se dice AHÍ (MaestroDice).
- CO-OP COMPARTIDA: el mismo director corre en el host (pt52) — mismo
  camino, cero código extra; VERIFICA que el texto compuesto sobrevive la
  replicación de pedidos de SaberSync (las filas replicadas usan
  FixedString128 — si el texto narrativo no cabe, el HUD replicado muestra
  el NOMBRE CORTO + las 3 líneas de progreso, y el texto largo solo lo lee
  el host: documenta la decisión).
- Al COMPLETARSE los vitrales: encolar EL PEDIDO DE OBRA (texto del diseño)
  y disparar el evento estático contratado (ver §3).

### 1c. El checklist en OrdersHud
El compuesto se pinta como bloque de 3 líneas: "▫ vidrio de botella 12/40 ·
▫ grisalla 0/12 · ✓ mortero 20/20" (la completada atenuada con ✓). Formato
exacto a decisión de O respetando el ancho real del panel (medido). La
flecha al Buzón del pt50 aplica al compuesto igual que a los Guiado.

## 2. ENCARGO SM — LA ALACENA Y LA OBRA
Archivos EXCLUSIVOS: Game/StorageRack.cs, Game/AlkahestGameBootstrap.cs,
Sim/SimLevelBuilder.cs.

### 2a. LA ALACENA (StorageRack renace con papel)
- En Semilla Cero (solo y multi-host), el StorageRack se spawnea OCULTO
  como hasta ahora (pt54 `visible:false`) y se REVELA cuando
  `SemillaCero.FaseVidaUtil` sube (poll barato en el bootstrap, patrón
  PollDestapes): sitio = el hueco del ex-estante (leer EstanteX0/BaseY
  reales del plano). OJO regla 36: revelar = construir el visual UNA vez
  (si `visible:false` cortó antes de BuildVisual, revelar llama a ese
  camino por primera vez — leer StorageRack.Init real y decidir; si hace
  falta un método `Revelar()` nuevo, hazlo idempotente).
- Rebautizo visual: rótulo de proximidad "LA ALACENA — guarda aquí lo que
  produzcas (vierte en una casilla)". 6 redomas (leer cuántas caben del
  ancho real). Cada casilla ocupada muestra DEBAJO su NOMBRE REAL
  (SubstanceKnowledge.NombreDe, que en seed 0 ya es el real) en chapa
  diminuta + el nivel visible (las redomas ya pintan contenido/nivel —
  verifica cuánto de esto YA existe y solo añade lo que falte).
- El cap por casilla y el aspirar-de-vuelta YA existen en StorageRack —
  no reinventar; reporta los números reales (cap por redoma) en el informe.
- En el CAÓTICO no cambia nada (el estante clásico sigue igual).

### 2b. LA MUFLA (la obra pagada)
- SimLevelBuilder gana el SITIO RESERVADO de la mufla: una plataforma
  discreta en el cuarto íntimo (leer el plano real; sin pisar estaciones,
  veta, buzón ni alacena; documentar coordenadas) que en el génesis es solo
  suelo/piedra normal.
- Al dispararse el evento contratado (§3) con id "obra_mufla": el
  bootstrap talla la mufla vía `Crisol.TallarEnPlano` en el sitio reservado
  y spawnea una SEGUNDA instancia de Crisol ("Mufla") con Init normal.
  V1 UN JUGADOR: si `SimSync.EnEscena`, la obra se talla igual en el host
  (la piedra viaja por chunks) pero NO se registra en MaquinaSync (su
  registro asume instancia única por tipo — deuda anotada pt53/pt56): el
  invitado la VE pero no la usa; documentado en el informe y en el código.
- La mufla nueva anuncia su nacimiento: MaestroDice "La mufla está en pie.
  Dos fuegos, aprendiz — ahora produce como taller de verdad." (vía el
  hook público que SemillaCero exponga — coordinar con O SOLO a través del
  contrato: si hace falta un método estático nuevo en SemillaCero, O lo
  declara y SM lo consume; está listado abajo).

## 3. LA COSTURA CONTRATADA (única superficie entre O y SM)
O declara en OrderSystem:
```csharp
/// (ronda 56) Disparado al completarse un encargo compuesto, con su id
/// estable ("vitrales_capilla", "obra_mufla"). El bootstrap (SM) se
/// suscribe para tallar obras pagadas. Estático: limpiar suscriptores es
/// responsabilidad del suscriptor (patrón AlDescubrir de SubstanceKnowledge).
public static event System.Action<string> AlCompletarCompuesto;
```
Y O garantiza: "vitrales_capilla" dispara al completar los vitrales,
"obra_mufla" al completar el pedido de obra. SM se suscribe en el bootstrap
(alta en Start, baja en OnDestroy). SemillaCero (O) expone además
`public static void MaestroAnuncia(string linea, float seg)` para que SM
anuncie la mufla sin tocar archivos de O — O lo implementa sobre
MaestroDice del director vivo (no-op si no hay director).

## 4. DEFINICIÓN DE HECHO
- O: el compuesto aparece al final del arco con su texto narrativo, el
  checklist pinta 3 progresos, entregar en el Buzón progresa la línea
  correcta, completar dispara Favor+cierre+obra; el pedido de obra
  encadena; compila.
- SM: la alacena aparece con la fase, guarda/devuelve con nombre real y
  nivel visible; la mufla se talla y funciona como segundo crisol al
  completarse la obra; compila.
- Ambos: informe con números exactos (regla 43), decisiones fuera de
  contrato explícitas, deudas.
