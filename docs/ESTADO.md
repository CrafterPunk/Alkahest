# ESTADO DEL PROYECTO — documento vivo

*(Se actualiza cada ronda. Historia completa: `archivo/HISTORIAL_RONDAS.md`. Visión: el GDD.)*
*Última actualización: ronda 74 — feedback del primer playtest del prólogo (fondo de ruina, bordes mordidos, LLÉNALO).*

## Dónde estamos

- **PRÓLOGO (la fundación)**: REHECHO en la ronda 73 sobre la espec de Cesar — presentar EL
  VERBO (absorber/verter) y nada más. Arco actual de `FundacionDirector`: inicio oscuro →
  VEN. (tutorial WASD que valida desplazamiento real) → TOMA. (el frasco vuela a tu mano) →
  AGUA. (cascada viva del muro a la poza; aspirar/verter validados por materia real; juego
  libre) → TRÁELA. (entrega vertiendo en el cuenco del Maestro) → DERRUMBE cinematográfico
  que abre la gotera de LODO (se apila ≠ fluye) → TRÁELO. → RECOMPENSA: el DEPÓSITO DE AGUA
  emerge del suelo (referencia en `docs/ref/`) con un culo de agua, y la tarea final es LLENARLO vertiendo (el autofill y su tubería llegan después, ref 2). Todo el
  guion (textos/cantidades/tiempos/triggers) vive en el bloque EL GUION del director, para
  iterar. Piezas nuevas: `TutorialContextual` (fichas blancas que validan RESULTADOS),
  `DepositoDeAgua`, foco cinematográfico + sacudida de cámara en `SimRenderer`, 3 sonidos
  nuevos (voz del Maestro, derrumbe, confirmación). Falta: playtest de Cesar y afinado de
  números; decidir el punto de inicio definitivo post-prólogo; botón "omitir intro"; la
  tubería lateral v2 del depósito (ref 2).
- **MODO NORMAL (Semilla Cero)**: el juego principal con semilla de autor. Arco de 5 pedidos,
  retículo de estados completo (5 bases × 8 estados), 6 recetas cruzadas, mufla por obra.
- **MODO CAÓTICO**: procedural por seed. Comparte cuarto íntimo con Normal pero SIN arco de
  autor. Puntos de inicio y contenido ligeramente distintos a Normal — **decisión vigente: no
  fusionarlos todavía**.
- **CO-OP (hasta 4)**: sim solo-host + espejo RLE 5Hz. Funcional; las máquinas remotas se ven
  como siluetas simplificadas (pendiente darles el visual completo). Steam vía appid 480 de
  pruebas ("SpaceWar" en Steam hasta comprar appid propio).
- **Juice del frasco**: motas en tránsito, anticipación/cierre, frasco que respira e inclina,
  pitch de llenado, haz retráctil. Validado por Cesar ("se siente mucho mejor").
- **2.5D**: capas visuales (`Capas.cs`), sombreado de masa, piso estructural, parallax 8% del
  muro, sándwich fondo-sim-reborde piloto en el Crisol (pendiente: resto de máquinas).

## Backlog priorizado

1. **Afinar el prólogo rehecho con el playtest de Cesar** (números del guion, sensación de
   las fichas y la voz, tubería v2 del depósito, omitir-intro).
2. **RONDA ESTRUCTURAL** (ver plan abajo): renombre de namespace/escena/asmdef/repo + poda de
   código aparcado. Prerequisito de la escenificación.
3. **ESCENIFICACIÓN**: mover a la escena las piezas que un dev de Unity pueda tocar desde el
   editor (ver plan abajo).
4. Vidrio frontal (sándwich) al resto de máquinas + sprites decorativos niveles 0-2.
5. Réplicas multi con visual completo; replicar juice/haz de otros jugadores.
6. Guardado con slots (GDD fase G) · buzón físico F1 · contador F2 · omitir-intro.
7. Menores: bordes verticales de roca (aplazado por Cesar), frasquito placeholder que se hunde,
   opción 4 del juice (remolino/salpicadura/haz vivo) si Cesar la pide.

## Código APARCADO (existe, compila, nadie lo spawnea — no borrar sin decisión)

| Qué | Estado | Referenciado por |
|---|---|---|
| `Criatura`/`Capullo` (+`SerSprites`) | Aparcados desde el pivot "Lo que persiste"; volverán como organismos-solución (decisión de Cesar) | 7-9 archivos (Mudanza, sync, sprites) |
| Taller clásico enterrado (`BuildTestLevel`, `PintarFondoTallerClasico`, `TapMount*`) | Bajo la piedra del génesis; la rama existe en `AlkahestSim` | SimLevelBuilder/WorkshopBackdrop |
| `BuildCuna`/`BuildRepisa`/`PlaceNutrienteMound`/`BuildTolvaCercana` | Métodos sin llamantes, conservados con su porqué en docblocks | — |
| Marea (`CONTRATO_MAREA`) | RETIRADA del código entero; solo queda el doc en archivo | — |

**Candidatos a poda en la ronda estructural** (no antes): los métodos sin llamantes de arriba,
y consolidar `StorageRack.FirmaVisualFabrica` con la de `JournalHud` (deuda vieja).

## PLAN — Ronda estructural (renombre + poda) — hacer con Unity abierto y verificación en vivo

Objetivo: que no quede "Alkahest" visible para un dev externo. En orden, cada paso compilando:

1. **Escena**: renombrar `AlkahestLab.unity` → `TenThousandYearsLab.unity` vía los generadores
   (cambiar `ScenePath` en Editor/, regenerar, borrar la vieja + su .meta). Igual la MULTI.
2. **Namespace**: sed global `namespace Alkahest` → `namespace TenThousandYears` (+ `using`).
   Riesgo bajo: las escenas referencian scripts por GUID, no por nombre. Revisar los pocos
   `Type.GetType("...")` por string si los hubiera.
3. **Asmdef**: `Alkahest.Runtime` → `TenThousandYears.Runtime` (+Editor). Actualizar el rig de
   compilación del sandbox (excluye el DLL por nombre) y las herramientas de Claude que usan
   `Type.GetType("X, Alkahest.Runtime")` por reflexión.
4. **Carpeta** `Assets/Alkahest/` → `Assets/TenThousandYears/` (Unity mueve .metas solo si se
   hace DENTRO del editor). Actualizar rutas en Editor/ y en el rig.
5. **Repo GitHub** `Alkahest` → `TenThousandYears` (Settings de GitHub + `git remote set-url`)
   y la carpeta del proyecto en disco si Cesar quiere (romperá `launch.cmd` y el registro de
   Unity Hub: rehacer ambos).
6. **Poda**: borrar los métodos sin llamantes listados arriba + `docs/archivo` de lo que ya no
   sume; correr una partida completa de cada modo antes del push.

## PLAN — Escenificación (para que un dev intervenga desde el editor)

Hoy TODO se instancia por código al arrancar (`AlkahestGameBootstrap` + generadores de escena).
Para abrir la puerta al trabajo visual desde el editor, por fases y sin romper el determinismo:

1. **Lo seguro primero (visual puro, sin estado de sim)**: WorkshopBackdrop, decoración de
   niveles 0-2, luces/viñetas, cámara → convertirlos en objetos DE ESCENA o prefabs que el
   bootstrap solo *encuentra* (`FindAnyObjectByType`) en vez de crear. Un dev los edita, mueve
   y reemplaza sin tocar código.
2. **Prefabs de estación**: extraer los `BuildVisual` a prefabs (sprites serializados) cuyos
   parámetros de juego sigan en código. La geometría de mampostería SIGUE saliendo de
   `SimLevelBuilder` (la sim es la verdad); el prefab es solo la piel.
3. **Nunca a escena**: el grid, el stepper, el plano de coordenadas y todo lo determinista.
4. Regla de convivencia: los generadores (`Ten Thousand Years/1-2`) pasan de CREAR a
   VALIDAR/completar la escena, para que las ediciones manuales del editor sobrevivan a una
   regeneración.

## Operativa con Claude (resumen)

Sandbox remoto (volátil — GitHub es la verdad) → compilación fiel local → deploy al disco de
Cesar → `ca_playtestNN.cmd` (doble clic de Cesar = commit+push). Claude NUNCA hace push.
Verificación de cada ronda: compilar, sondas en runtime vía Unity MCP, capturas jugando.
