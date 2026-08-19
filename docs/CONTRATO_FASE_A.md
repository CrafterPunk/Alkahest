# CONTRATO CONGELADO — FASE A: SER DUEÑOS DE LO QUE HAY (Playtest 47)

GO de Cesar al plan del INFORME_REALIDAD §7 Fase A. Al terminar esta ronda
él prueba TODO de una vez — la ronda debe quedar redonda, no a medias.

Dos encargos PARALELOS: **C = cruces** (recetas cruzadas + renames +
resistencias) y **M = menú** (inicio con ajustes + volumen).

## 0. LO QUE ESTA RONDA ENTREGA (visión)

Las recetas más famosas de la humanidad usando SOLO lo que ya está en el
taller (mortero, cemento, hormigón, vidrio de botella, lejía, esmaltado),
los 4 renames de la auditoría, el principio "todo camino da algo" en su
primera encarnación (resistencias anotadas), y un juego que por fin tiene
menú de ajustes y volumen. Referencias: INFORME_REALIDAD.md §2 (renames),
§4 (cruces), §5 (anti-"nada").

## 1. ENCARGO C — las recetas cruzadas

Archivos de C: `Sim/Universe.cs`, `Game/Crisol.cs`,
`Game/SubstanceKnowledge.cs`, `Game/AlbumReal.cs` (SOLO aditivo: la página
nueva), `Game/HintSystem.cs` (un consejo).

### 1a. Materiales nuevos (ids tras Brasa=58; Count 59 → 64)

| id | Material | Identidad real (nombre · color · reseña VERBATIM) |
|---|---|---|
| 59 | **Mortero** | "mortero" · (200,196,184) · "Cal apagada y arena: la pasta que pegó Roma entera. Fragua lento y para siempre." |
| 60 | **VidrioVerde** | "vidrio de botella" · (110,160,120) · "Arena fundida con ceniza: la potasa baja el punto de fusión. Así se hizo todo el vidrio de bosque medieval — verde por el hierro de la ceniza." |
| 61 | **Lejia** | "lejía de ceniza" · (210,205,180) · "Agua que pasó por ceniza y le robó la potasa. Limpia, quema, y con grasa haría jabón — la receta más vieja de la química doméstica." |
| 62 | **Hormigon** | "hormigón" · (168,164,156) · "Clínker molido, arena y agua: piedra líquida que fragua donde la viertas. El material más usado del planeta después del agua." |
| 63 | **Esmaltado** | "cerámica esmaltada" · (196,120,88) · "Bizcocho cocido con arena encima: la sílice vitrifica en la superficie. Brillo de vajilla noble." |

Arquetipos/física (decisión de C con criterio, documentada): mortero y
hormigón nacen POLVO-húmedo que FRAGUA a sólido estático con el tiempo
(reutilizar el patrón de solidificación existente o aux-countdown estilo
Brasa — fraguar ES un countdown); vidrio de botella = sólido con la física
del Templado (cae con cohesión 3); lejía = líquido; esmaltado = sólido
cohesión 6. Todos con `UmbralPersistenciaRaw` sensato (el hormigón aguanta
MÁS que el mortero — verdad real) y entrada en la tabla de identidad para
el álbum y las fichas.

### 1b. La MEZCLA EN CUBETA (el sistema)

`Crisol.DecidirHornada` gana una consulta PREVIA a su escalera: si la
cámara contiene DOS materiales relevantes (dominante + secundario ≥20% de
las celdas de carga), consultar la TABLA DE CRUCES (estática, en Crisol o
Universe — decisión de C):

| Mezcla | Fuego | Producto | Verbo |
|---|---|---|---|
| cal apagada + arena de sílice | cualquiera (tier0 basta) | Mortero | "amasando" |
| caliza molida + arcilla | pleno (≥ tier1) | CalizaCeramico existente (ver 1c: pasa a llamarse clínker DE VERDAD) | "cociendo clínker" |
| clínker + arena de sílice (con agua presente o tras verter agua: decisión de C, documentada) | bajo | Hormigon | "fraguando" |
| arena de sílice + ceniza | pleno | VidrioVerde | "fundiendo con fundente" — Y LA LECCIÓN: funde a banda MÁS BAJA que la fusión pura de la arena (la potasa real baja el punto de fusión; que el número lo diga) |
| ceniza + agua | bajo | Lejia (+ la ceniza pierde su parte soluble) | "lixiviando" |
| bizcocho + arena de sílice | pleno | Esmaltado | "esmaltando" |

- Sin mezcla válida → la escalera de siempre (dominante). La promesa UNA
  transformación por hornada se mantiene: el cruce ES la transformación.
- Los cruces disparan `Hornada.RegistrarOp` (patentables) y descubren el
  producto (álbum/ficha gratis vía el flujo existente).
- En Semilla Cero Y caótico (los cruces usan materiales de identidad
  real; en caótico las 5 bases de la seed cumplen los mismos ROLES por
  posición — C decide si el cruce en caótico mapea por rol de base o se
  reserva a Semilla Cero, y LO DOCUMENTA; mi preferencia: reservado a
  Semilla Cero esta ronda, deuda para generalizar).

### 1c. Renames de la auditoría (INFORME §2, VERBATIM)

1. sal Templado: "sal vítrea" → **"sal de estampido"** · reseña: "La sal
   no se vuelve vidrio: REVIENTA al calentarse por el agua atrapada en
   sus cristales. Los cocineros lo llaman decrepitación."
2. caliza Compacto: "mármol joven" → **"caliza prensada"** · reseña: "El
   mármol real pide eras de presión y calor. Esto es el primer paso — tu
   prensa hace la parte rápida."
3. caliza Ceramico: "clínker" queda como el NOMBRE DEL CRUCE (1b); la
   entrada solo-caliza pasa a **"cal sobrecocida"** · reseña: "Caliza
   cocida de más, sin arcilla que la acompañe. Para clínker de verdad,
   mezcla." (La pista del cruce, dicha por el material.)
4. veta Templado: "resina dura" → **"ámbar de brea"** · reseña: "Brea
   enfriada de golpe, quebradiza y translúcida. El ámbar real es resina
   con un millón de años de paciencia."

### 1d. Resistencias anotadas (la primera encarnación del anti-"nada")

- Crisol: cuando la hornada NO tiene transformación posible (DecidirHornada
  false), el rótulo ya lo dice — AHORA ADEMÁS anota en la ficha del
  material: `RegistrarObservacionPropiedad(mat, "resiste este fuego")`
  (una vez por material+condición, sin spam — mismo dedup que las
  observaciones existentes).
- Prensa: el RESISTE del cerámico y amigos anota "resiste la prensa".
- Estas notas alimentan la ficha del diario (canal existente). El álbum
  NO cambia por esto (la matriz completa de 350 celdas queda para su
  propio encargo futuro — esto es la rebanada que ya paga).

### 1e. Álbum (SOLO aditivo, cuidado: Opus lo acaba de rehacer)

Página nueva **"MEZCLAS DEL OFICIO"** (séptima, tras clásicos): las 5+1
figuritas de los cruces (mortero, clínker, hormigón, vidrio de botella,
lejía, esmaltado) con el MISMO lenguaje de vitrinas; en el lado derecho,
en vez de árbol de familia, las recetas COMO PREGUNTAS ("¿cal + arena?")
que se revelan al descubrir. Tocar lo mínimo del código de páginas
(añadir una entrada a su estructura de páginas — leerla primero).

## 2. ENCARGO M — el menú y el volumen

Archivos de M: `Game/DayCycle.cs` (título + pausa), `Audio/DirectorDeAudio.cs`
(volúmenes), `Game/UiStyles.cs` SOLO si falta un estilo de slider.

- **AJUSTES en el título**: botón "AJUSTES" bajo el filete (entre MODO
  CAÓTICO y Salir): panel sobrio con DOS sliders — "Volumen general"
  (AudioListener.volume) y "Efectos del taller" (factor propio que
  DirectorDeAudio aplica a sus fuentes) — persistidos en PlayerPrefs
  (claves con prefijo ChaosAlchemy_), botón "listo". Estilo UiStyles puro.
- **PAUSA con Escape** en partida (un jugador Y multi): overlay simple —
  "REANUDAR / AJUSTES / VOLVER AL TÍTULO" (en multi, VOLVER = desconectar
  limpio vía SessionCoordinator.Disconnect — leer TallerSesionHud para el
  patrón). En UN JUGADOR la pausa CONGELA la sim (AlkahestSim.Paused, el
  flag existe); en MULTI no congela (el taller es compartido) — solo
  abre el panel, documentado. OJO: Escape ya lo usa NamingUi para cerrar
  — respeta UiStyles.EscribiendoTexto y las ventanas abiertas (si hay
  diario/álbum/rito abierto, Escape cierra ESO primero, no abre pausa;
  el orden de guardas importa y se documenta).
- DirectorDeAudio: multiplicador de efectos aplicado en UN punto (donde
  nazcan/actualicen las fuentes), cero allocs.

## 3. HECHOS COMPARTIDOS

CLAUDE.md entero (7/15/48/53/54/55). Determinismo: los cruces corren en la
hornada del ANFITRIÓN (igual que toda hornada — multi hereda gratis).
Cero allocs. Español latino. El arnés debe seguir compilando (ids nuevos
en Universe: cuidado con arrays [MaterialId.Count]— grep de Count al
crecer a 64). Compilación regla 53 (rig montado). Encargos paralelos:
errores transitorios protocolo pt40. AlbumReal/Crisol recién reescritos:
LEER antes de editar, integrarse a su estructura, no pelearla.

## 4. DEFINICIÓN DE HECHO

- **C**: los 6 cruces funcionan en Semilla Cero (verificable: cal apagada
  + arena en la cubeta + E → "amasando" → mortero en la cubeta, ficha y
  figurita); los 4 renames visibles en ficha/álbum; "resiste este fuego"/
  "resiste la prensa" aparecen en fichas; Count=64 sin romper arrays ni
  arnés; compila.
- **M**: título con AJUSTES funcional y persistente; Escape pausa/reanuda
  con la escalera de guardas correcta; volver al título limpio en ambos
  modos; compila.
- Ambos: informe de datos, decisiones fuera de contrato EXPLÍCITAS,
  deudas. La prueba jugada completa la hace CESAR al final de la fase.
