# ALCANCE DEL LABORATORIO — qué es esta fase, qué no es, y qué hay alrededor (Cesar y Fable, 2026-09-05, R147)

Léelo antes que el HANDOFF. Existe porque en R142-R146 aparecieron en el buzón palabras como
«campaña», «encargos», «trueque» y «el alambique», y hay que dejar claro de dónde vienen y qué
peso tienen. **Ninguna es objetivo de esta fase.**

## 1. Qué es el laboratorio de leyes

Un **sandbox de investigación** (`ModoLaboratorio`, botón «laboratorio de leyes (sandbox de
investigación, dev)» del título) construido para una sola pregunta: *¿tiene la simulación
profundidad sistémica suficiente para sostener la tesis de que el conocimiento y la geometría
sustituyen al trabajo manual?* Se responde con **medidas**: conservación al bit, determinismo,
cadenas de consecuencias que aparecen sin guion, procesos sostenidos sin intervención, coste.

**No es** prólogo, onboarding, campaña, economía, guardado, versión comercial, ni una prueba de
experiencia de jugador. Lo dijo el encargo original y sigue en pie.

## 2. Qué hay alrededor: el juego heredado, en el mismo ejecutable

El laboratorio es **un modo más dentro del mismo ejecutable** que el juego anterior («Ten
Thousand Years», antes «Alkahest»). Del título salen: «PRÓLOGO — la fundación», «MODO NORMAL —
SEMILLA CERO», «galería de estilo (dev)», «laboratorio de leyes (dev)» y «MODO CAÓTICO». Todo lo
que sigue existe en el código, **no está activo en el laboratorio, y no es objetivo de esta fase**:

| palabra | qué es en el repo | estado en el laboratorio |
|---|---|---|
| **campaña** | el «MODO NORMAL — SEMILLA CERO» (`SemillaCero.cs`, `FundacionDirector.cs`, `DayCycle.cs`) | no corre; el laboratorio arranca con su propio `RestartRun(777002)` |
| **encargos** | `OrderSystem.cs`: pedidos del juego viejo (p. ej. «vidrio de botella») | el laboratorio crea el sistema porque `DayCycle` lo exige, «nadie encola nada» |
| **trueque** | `Trueque.cs`: ofertas cobradas en vidrio verde | no existe en el laboratorio |
| **bautizo / álbum / diario** | `SubstanceKnowledge.cs`, `AlbumReal.cs`, `JournalHud.cs` (reglas 13/17/23 del juego) | inactivos; el laboratorio nombra por lo que la cosa ES (`LabMateriales.Nombre`) |
| **maestro** | `EnsayoMaestro.cs` | residuo; no forma parte del experimento |
| **`Alambique`** (objeto) | `Game/Alambique.cs`: instrumento del juego viejo que atrapa vapor | **no es** el alambique del laboratorio (§3) |
| **frasco, cincel, mudanza, muñeco** | `Flask.cs`, `Cincel.cs`, `Mudanza.cs`, `ApprenticeController.cs` | **sí se reutilizan** como herramientas del investigador; son código compartido (§4) |
| **sonido, diario de sesión** | `DirectorDeAudio*.cs` (heredado, activado en R143), `LabDiario.cs` (R143) | herramienta para una prueba de experiencia **futura**; no se usa ahora (§5) |

## 3. Vocabulario del laboratorio (geometrías, no sistemas)

Cuando el buzón o un benchmark dicen **alambique, horno, carbonera, tolva, huerto**, hablan de
**geometrías** que alguien monta con materiales del laboratorio y que funcionan solo por las
reglas de la simulación: el alambique del laboratorio es un serpentín de `NucleoFrio` bajo un
techo y agua que hierve sobre el hogar; «el alambique del banco» (r141 §2, `LabBench`) es esa
misma geometría montada por código para medir. Nunca son los objetos `Alambique`, ni los
encargos, ni ninguna máquina del juego heredado.

## 4. La única regla que hace que lo heredado importe: no romper el código compartido

El laboratorio comparte con los otros modos el muñeco, sus herramientas, `DayCycle`, el audio y
**tablas globales** como `LabMateriales.EsSolidoDelMundo` (la leen `Flask`, `Cincel` y
`ApprenticeController` en todos los modos). Por eso, cuando en R142 se metió el vidrio verde en
esa tabla, el frasco dejó de aspirarlo **también en Semilla Cero**, donde el vidrio es un ítem de
encargo: se revirtió (R145) **para no romper otro modo del mismo ejecutable**, no porque el
laboratorio se evalúe con criterios de campaña. Esa es la única forma legítima en que «campaña»,
«encargos» o «trueque» pueden aparecer en esta fase:

- **Nunca** como objetivo, criterio de aceptación o argumento de diseño del laboratorio.
- **Solo** como «esto que toco es compartido: compruebo que el otro modo no regresa». Y se dice
  así, con esas palabras.
- Los residuos del juego heredado que se ven dentro del laboratorio (maestro, nombres a medias,
  plantas de un píxel, estado que solo se lee con F8) **no se limpian ahora**: son presentación,
  y la presentación se diseña en la fase comercial sobre este sustrato.

## 5. El cierre del experimento (decisión de Cesar, 2026-09-05)

La función del laboratorio está demostrada en lo que era suya: la simulación tiene profundidad
medible (agua 5/5, fuego 4/5 medido y honesto, plantas con mecánica completa). Ya no se le
pregunta al sandbox si «por sí solo es el juego».

1. **H5 definitivo**: el banco honesto (HF5e-B: siete hashes en el informe, causas separadas,
   goteos y anegadas medibles, hervidero con el frío en el camino del vapor, textos). Es el cierre
   de las mediciones.
2. **H7 con jugador: DEFERIDO.** Una sesión larga hoy mediría la presentación provisional
   (residuos, nombres, observabilidad por F8), no si «hay juego». Se hará **después** de construir
   una primera experiencia coherente, con el diario y el sonido ya preparados (R143, HF5e-A queda
   en la lista para entonces).
3. **H7s, el arco largo autónomo** (lo único de H7 que no está contaminado por la presentación):
   40 minutos de mundo **sin jugador**, en banco, con las geometrías montadas por código
   (alambique sobre el huerto, un fuego de fibra en la sala cuyo humo sube por la chimenea, una
   carbonera), medidas cada 5 minutos: huerto vivo a 10/20/40 min (aceptación de H4), cadenas
   cruzadas vistas por contadores y snapshot (humo → luz → plantas; goteo → huerto; carbón →
   horno), conservación, coste. Da al informe lo que sí es de la simulación y deja al jugador para
   la fase siguiente.
4. **H8 honesto**: el informe A-F con lo que sí sabemos, y **explícito** en cada sección que
   dependía de un jugador (descubrimiento sin ayuda, onboarding, tedio con jugador, diversión,
   «hay juego»): *no evaluado — ni aprobado ni refutado*.
5. **Revisión conjunta** de H8 (Cesar, Fable, Opus).
6. **Diseño de la experiencia comercial** sobre el sustrato: cámara, escala, controles,
   superficie/subsuelo, observabilidad, capa visual sobre las celdas, onboarding, objetivos. Solo
   después, la prueba de experiencia tipo H7.

**Física congelada** desde R141 en todo este tramo. H6 (sólidos) documentado y congelado.
