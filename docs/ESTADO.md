# ESTADO DEL PROYECTO — documento vivo

*(Se actualiza cada ronda. Historia completa: `archivo/HISTORIAL_RONDAS.md`. Visión: el GDD.)*
*Última actualización: ronda 77 — LAS HERRAMIENTAS DE CESAR (overlay del cincel, mapa de zonas, cascada sonora, juego libre por conducta). Antes: R76 blindó el ciclo de sesión multi (12 fixes + verificación en vivo del ciclo host→caída→recarga→re-host) y R75 la escenificación.*

## DECISIONES CERRADAS (R107 — el diseño mayor está sellado)

Con el decreto del mundo (GDD §0) el proyecto no tiene decisiones grandes de
diseño pendientes. Cerradas y firmes: mundo de RUINAS AMABLES + regla de hierro
R60 (lo heredado se repara, jamás se desguaza) · el MUÑECO DE REMIENDOS como
avatar (levita; variantes por acento; intro del cubo caído) · dirección visual
2D artesanal con acabado ABIERTO (pixel = una salida posible) · pipeline: 3D
como herramienta de producción → render orto HD → tratamiento → sprites (ver
docs/DIRECCION_DE_ARTE.md) · demo Era I con DOS máquinas nuevas (hoyo de
cocción y horno) + 4 pieles de silo + regla de concentración del encierro
(cerámica SIN licencia: el horno cierra el gap 185→205) · prensa FUERA de la
demo · nodos habilitadores por seed + caravanas del tablón · cadena de frío:
solo ficha, no sistema. Lo único abierto por naturaleza: números de balance y
la 4ª variante de color.

## RETIRADO DEL TRACK (ya no existe como pendiente — solo como historia)

La tolva (R93) · la estantería/baldas en la fundación (R102) · la luz fría del
vano (R102: «haremos otra cosa ahí» = la puerta de las caravanas) · el hogar
vacío del jugador (R101) · la veta de turba del plano (R79; la turba vuelve
como SILO comprable) · el título en el Adiós (R104: el momento del título se
construirá aparte) · el clima por zona (R31) · el imp como personaje (ahora
oficialmente placeholder en retirada) · «pixel art» como acabado asumido (el
acabado está abierto). Los porqués viven en el código (regla 15) y en el
HISTORIAL.

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
   las fichas y la voz, tubería v2 del depósito, omitir-intro). Desde la R77 Cesar puede
   PERFILAR LA ROCA él mismo: tallar/pintar en Play y F3 → "GUARDAR FORMA COMO PLANO".
2. **RONDA ESTRUCTURAL** (ver plan abajo): renombre de namespace/escena/asmdef/repo + poda de
   código aparcado. Prerequisito de la escenificación.
3. **ESCENIFICACIÓN, siguientes familias**: máquinas del taller a prefabs, decoración,
   escena multi (la del prólogo quedó hecha en la ronda 75 — ver la matriz de autoridad abajo).
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

## LA ARQUITECTURA HÍBRIDA (ronda 75 — HECHA para el prólogo)

La fase 1 del plan de escenificación se ejecutó sobre el PRÓLOGO. Cómo se trabaja ahora:

**Flujo para Cesar y su hermano**: abrir `AlkahestLab.unity` → retocar → Play. Los retoques
sobreviven a los generadores y a las builds (el menú 1 y el pre-vuelo VALIDAN, ya no arrasan;
el destructivo quedó aparte como "1b. REGENERAR DESDE CERO"). El menú
"6. Hornear arte del prólogo" reescribe los PNG desde el código y crea (solo si faltan) el
prefab del depósito y el guion.

**MATRIZ DE AUTORIDAD** (quién manda sobre qué — respetarla evita que código y escena peleen):

| Elemento | Autoridad | Dónde se toca |
|---|---|---|
| Textos, cantidades, tiempos, triggers, radios de luz, caudales, layout de UI | ASSET | `Arte/Prologo/GuionDelPrologo.asset` (Inspector) |
| Posición/escala del Maestro (visual Y triggers de proximidad) | ESCENA | marcador `Prologo_Escenografia/Maestro` |
| Dónde emerge el depósito | ESCENA | marcador `Prologo_Escenografia/Deposito` (base-centro, se ajusta a celdas) |
| Piel del depósito (capas, offsets, sprites) | PREFAB | `Arte/Prologo/DepositoVisual.prefab` (hijos `Fondo`/`Marco`) |
| Telón de la ruina (sprite, posición, tinte) | ESCENA | `WorkshopBackdrop` + su hijo `Fondo_Horneado` (sprite: `RuinaFondo.png`) |
| Retoques de ROCA del prólogo (R77: la forma que Cesar guarda desde el juego) | ASSET | jugar → tallar/pintar Stone → F3 "GUARDAR FORMA COMO PLANO" → `Arte/Prologo/PlanoOverlay.asset` (borrar el asset = plano virgen; respeta obra y zona del derrumbe con acuse) |
| Beats y su orden, sim, plano tallado (cascada/poza/cráter/cuenco), química, net | CÓDIGO | como siempre |

**Fallbacks (reversibilidad)**: si falta el asset/marcador/prefab, el código reconstruye el
prólogo histórico completo — una escena vieja o el sandbox corren igual.

**Lo que se quedó en código A PROPÓSITO (candidatos visuales evaluados y descartados)**:
- La UI (voz, fichas, HUDs) sigue en IMGUI con sus números en el guion: migrarla a
  uGUI/Canvas sería reescribir TODA la capa de UI del juego por consistencia — acoplamiento y
  riesgo enormes para ganar un drag que el guion ya da por números.
- La geometría de la cascada/poza/cuenco/cráter: es plano TALLADO en la sim
  (SimLevelBuilder); moverla desde escena obligaría a re-carvar y re-sincronizar los rects de
  conteo del director — la sim es la verdad, se mueve por código.
- El fuego del Maestro: es Brasa/Fire REAL de la sim (la luz tiene causa física), no un visual.

## PLAN — Escenificación: familias PENDIENTES (mismo criterio y receta)

1. **Máquinas del taller** (Crisol, Prensa, BancoChispa, Alambique...): hornear sus sprites y
   extraer la piel a prefabs (patrón DepositoVisual: hijos con órdenes de Capas.cs; los
   parámetros de juego y el tallado siguen en código).
2. **Decoración niveles 0-2, baldas/cadenas del taller**: marcadores + prefabs.
3. **Escena MULTI**: su generador (menú 2) sigue siendo destructivo — pasarlo a validar cuando
   el taller multi gane contenido de escena.
4. **Nunca a escena** (sin cambios): grid, stepper, plano de coordenadas, todo lo determinista.

## Operativa con Claude (resumen)

Sandbox remoto (volátil — GitHub es la verdad) → compilación fiel local → deploy al disco de
Cesar → `ca_playtestNN.cmd` (doble clic de Cesar = commit+push). Claude NUNCA hace push.
Verificación de cada ronda: compilar, sondas en runtime vía Unity MCP, capturas jugando.
