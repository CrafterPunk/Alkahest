# SEGUNDA PASADA · 01 · LEYES: EL CATÁLOGO REFUTADO Y EL SUSTRATO QUE ELIJO

*(Fable 5.1, 2026-09-12. Síntesis del panel de leyes: ocho lentes, 32 candidatos de modificación del
sustrato, 64 refutaciones (dos por candidato, leyendo el código real) y un crítico de completitud.
Documentos completos en `panel/leyes/` y `panel/leyes/refutaciones/`. Nada de esto está autorizado ni
arquitecturado: es el catálogo con el que se diseñan las direcciones de `02`.)*

## 0. Cómo leer este documento

Cada candidato fue propuesto por un diseñador de simulación con la lente asignada, con la obligación
de leer el código y citar líneas, y después atacado por dos refutadores independientes: uno de
**ingeniería** (determinismo, coste por tick, encaje real, verificabilidad en banco) y uno de
**apalancamiento** (¿cuántas decisiones nuevas crea de verdad al cruzar leyes existentes? ¿qué tuning
esconde?). Los veredictos posibles eran refutado, viable con ajuste y viable. Ninguno de los 32 salió
«viable» sin ajuste; eso es lo esperable cuando los refutadores leen el código, y por eso los números
que importan son los **corregidos**, no los declarados. El estado «parcial» significa que un refutador
lo tumba y el otro no: en todos esos casos el que tumba es el de apalancamiento, y su ajuste es la
versión que sobrevive.

Los refutadores encontraron, además, nueve hechos del código que corrigen lo que la primera pasada y
el propio informe final daban por cierto. Van primero, porque cambian el mapa.

## 1. Nueve hechos del código que corrigen la primera pasada

1. **La planta muerta YA deja fibra.** `LabPlanta` transforma en `Fibra` la celda sin raíz ni tallo
   debajo (SimStepper.Laboratorio.cs:811) y la marchita. La «única descongelación de física» que pedía
   `docs/COMERCIAL/04_VEREDICTO.md` (G5) no era una transformación que faltara: faltan rendimiento,
   cosecha sin matar la raíz, secado (la fibra cae al lecho y se moja por capilaridad) y transporte.
2. **El fuego ya ilumina el campo.** `LabLuz` pone `luz = 255` en fuego, brasa y hogar (`EmiteLuz`).
   Lo que no ilumina es la pantalla: nadie pinta `luz` fuera de F8.
3. **El huerto quizá no murió por luz: se midió la celda equivocada.** Los «7 de 73 caras» de R148
   miden la luz de la celda de **sedimento**, que ninguna ley lee. La germinación lee `luz[i+W]` (el
   aire de encima, :704) y el crecimiento `luz[arriba]` (:870-873); con la boca de 7 columnas ese aire
   tenía 72-216 sobre x100-145 (CHECKPOINT §, cita del refutador), por encima de `PlantaLuzMin` 40.
   Los 7/73 serían un artefacto del orden de barridos de `LabLuz` (descendente antes que laterales). Si
   es así, **Q16 se remide en un día** con la métrica que leen las plantas, y H4 puede reabrirse gratis.
   Es una afirmación de un refutador, no un hecho medido: es la primera prueba de banco de esta pasada.
4. **El alambique probablemente graniza.** `LabGotear` nace el agua con la temperatura de la
   superficie (30 raw en el serpentín) y `ApplyPhase` la hiela al tick siguiente (agua se congela a
   60 raw): los «900 goteos» de R141 serían perdigones de hielo que se derriten sobre el lecho. Nadie
   contó `SimEventType.Freeze`. Se confirma en una tarde.
5. **El hielo existe y nadie lo usa.** `Ice` es sólido con cohesión 4, no flota (solo no se hunde),
   aísla como roca y es opaco. El núcleo frío es un pin infinito a −60 °C: el «objeto mágico» de la
   primera pasada, confirmado.
6. **El vidrio del laboratorio es un sólido mudo.** `VidrioVerde` nace en el horno y no tiene caso en
   `LabCampos`: no transmite luz, no suda, conduce como polvo. Todo lo que el horno produce hoy es una
   pared verde. Y `VidrioVerde` está fuera de `EsSolidoDelMundo` desde R145.
7. **Hay dos bytes libres por celda en los sólidos.** `carga` no significa nada en roca, terracota,
   piso, roca suelta ni vidrio; `reposo` está libre en lo cocido. Dos memorias de 8 bits gratis, ya en
   `SwapCells` y en los hashes: no hace falta un campo `huella` nuevo.
8. **El aire no tiene masa y la «convección» no mueve nada.** `Conveccion` sesga la conducción
   térmica (el calor sube antes que baja); `TiroAmbienteTicks` es la relajación hacia 20 °C, no tiro.
   El «viento» del gas (`ProcessGas`) es un hash por bloque de 8×8: se lee como viento y no tiene causa.
   Diagnóstico del panel: el tiro no emerge porque falta la masa, no porque falte una regla.
9. **No existe fichero de partida.** El único IO del laboratorio es PNG + preset + libro JSON; la
   rejilla no se guarda ni se carga. Todo lo que la primera pasada llamó «asíncrono por fichero» y
   «legado» depende de un volcado que hoy no está: es parte del candidato `sello`.

## 2. El catálogo

Valores declarados → corregidos (media de los dos refutadores). Tuning: 10 = casi ninguno. Semanas:
de Opus, con banco. Estado: sí = ningún refutador lo tumba; parcial = el de apalancamiento lo tumba y
deja un ajuste; no = los dos lo tumban.

| lente | candidato | apal. | tuning | sem. | ingeniería | apalancamiento | estado |
|---|---|---|---|---|---|---|---|
| aire-viento | **El aire pesa, se gasta y fluye** (`aire-que-fluye`) | 9 → 6,5 | 7 → 6 | 1,5 → 1,9 | viable con ajuste | viable con ajuste | sí |
| aire-viento | El flujo lleva calor y vapor (`adveccion-calor-vapor`) | 8 → 4,5 | 8 → 6 | 0,3 → 0,7 | viable con ajuste | refutado | parcial |
| aire-viento | Lo que vuela: brasas, ceniza, semillas (`polvos-al-viento`) | 7 → 4 | 7 → 5,5 | 0,5 → 0,7 | viable con ajuste | refutado | parcial |
| aire-viento | **Aire atrapado: campanas y bolsas** (`aire-atrapado`) | 7 → 6 | 7 → 7 | 1 → 1,1 | viable con ajuste | viable con ajuste | sí |
| luz-optica | **El haz del cielo y el vidrio que lo desvía** (`haz-y-espejo`) | 9 → 6,5 | 9 → 7,5 | 1,5 → 1,5 | viable con ajuste | viable con ajuste | sí |
| luz-optica | Lo que la luz pierde lo gana en calor (`insolacion`) | 8 → 5,5 | 8 → 6,5 | 1 → 2,2 | viable con ajuste | refutado | parcial |
| luz-optica | El ojo ve por luz (`ver-por-la-luz`) | 7 → 4,5 | 6 → 5,5 | 0,75 → 1,2 | viable con ajuste | refutado | parcial |
| luz-optica | La turbidez oscurece (`filtros`) | 5 → 2 | 9 → 9 | 0,5 → 0,5 | viable con ajuste | refutado | parcial |
| agua-fases | **El cambio de fase cuesta; el hielo con reserva** (`fase-con-reserva`) | 9 → 6,5 | 8 → 7,5 | 1,5 → 1,2 | viable con ajuste | viable con ajuste | sí |
| agua-fases | El agua caliente sube (`conveccion-en-agua`) | 8 → 5 | 8 → 7 | 0,75 → 1 | viable con ajuste | viable con ajuste | sí |
| agua-fases | Escarcha y suelo helado (`escarcha-y-suelo-helado`) | 7 → 5 | 7 → 7 | 1 → 1,5 | viable con ajuste | viable con ajuste | sí |
| agua-fases | La roca recuerda (`clima-ganado`) | 7 → 4,5 | 7 → 6 | 0,75 → 1,8 | viable con ajuste | refutado | parcial |
| cuerpo | Piel térmica y húmeda (`piel-termica-humeda`) | 9 → 5 | 7 → 5 | 1 → 1,8 | viable con ajuste | refutado | parcial |
| cuerpo | Carga con temperatura (`carga-con-temperatura`) | 8 → 5,5 | 8 → 6,5 | 0,5 → 1,2 | viable con ajuste | refutado | parcial |
| cuerpo | Humo respirado (`humo-respirado`) | 7 → 4 | 8 → 7 | 0,25 → 0,5 | viable con ajuste | refutado | parcial |
| cuerpo | Masa y flotación (`masa-y-flotacion`) | 6 → 3,5 | 6 → 5,5 | 0,25 → 0,8 | viable con ajuste | refutado | parcial |
| organismos | **La planta con órganos: siega, raíz, copa** (`planta-con-organos`) | 9 → 5,5 | 7 → 6 | 2 → 2,8 | viable con ajuste | viable con ajuste | sí |
| organismos | **Dispersión: el agua arrastra lo que flota** (`dispersion-semillas-arrastre`) | 8 → 6,5 | 7 → 6,5 | 1,5 → 1 | viable con ajuste | viable con ajuste | sí |
| organismos | **Compost y musgo** (`compost-y-musgo`) | 7 → 6 | 8 → 7,5 | 1 → 1 | viable con ajuste | viable con ajuste | sí |
| organismos | La polilla (`polilla-lectora-de-luz`) | 4 → 3 | 4 → 5 | 1 → 1,8 | viable con ajuste | refutado | parcial |
| materiales | **Vidrio que deja pasar la luz y suda** (`vidrio-transparente-que-suda`) | 9 → 6 | 8 → 8,5 | 0,5 → 0,8 | viable con ajuste | viable con ajuste | sí |
| materiales | **Choque térmico con memoria de cocción** (`choque-termico-memoria-coccion`) | 8 → 5,5 | 8 → 7 | 1 → 0,9 | viable con ajuste | viable con ajuste | sí |
| materiales | **Hollín y marca de marea** (`hollin-marca-de-marea`) | 7 → 5,5 | 7 → 6,5 | 1 → 1,2 | viable con ajuste | viable con ajuste | sí |
| materiales | El poroso mojado conduce (`conductividad-por-humedad`) | 6 → 2,5 | 9 → 8 | 0,5 → 0,5 | viable con ajuste | refutado | parcial |
| instrumentos | **Huella: máximos que el mundo escribe** (`huella-maximos`) | 9 → 6 | 9 → 7,5 | 1,5 → 1 | viable con ajuste | viable con ajuste | sí |
| instrumentos | Luz vivida y lámpara (`luz-vivida-lampara`) | 8 → 5,5 | 8 → 6 | 1 → 1,8 | viable con ajuste | viable con ajuste | sí (la lámpara no) |
| instrumentos | Rayos X por bandas de ley (`rayosx-bandas-de-ley`) | 7 → 4,5 | 9 → 8 | 1 → 0,8 | viable con ajuste | viable con ajuste | sí (sin sondas) |
| instrumentos | Sondas registradoras, la veta (`sondas-registradoras-veta`) | 7 → 4 | 9 → 9,5 | 1 → 1 | viable con ajuste | refutado | parcial |
| métricas | **La balanza: el sumidero pesa lo que traga** (`balanza-del-sumidero`) | 9 → 6 | 9 → 8 | 1 → 1,2 | viable con ajuste | viable con ajuste | sí |
| métricas | **El sello: registro, volcado y veredicto rejugable** (`sello-registro-y-veredicto`) | 9 → 7,5 | 9 → 8 | 3 → 3,2 | viable con ajuste | viable con ajuste | sí |
| métricas | **La cuna: geología por simulación y validador** (`cuna-geologia-por-simulacion`) | 8 → 5,5 | 6 → 5,5 | 3 → 4,8 | viable con ajuste | viable con ajuste | sí |
| métricas | El testigo: la condición en la grilla (`testigo-condicion-en-grilla`) | 7 → 4 | 9 → 8 | 1 → 1,5 | viable con ajuste | refutado | parcial |

Lectura general: los refutadores bajaron el apalancamiento medio de 7,7 a 5,2 y subieron el coste
un 30 %. Ningún candidato que dependa de una **regla de una línea** conservó su nota: casi todos
prometían cruces que el código ya tiene (y por tanto no son nuevos) o cruces que el código impide
(el polvo no sube, la luz rodea obstáculos estrechos, el humo no llega a la cabeza). Lo que
sobrevivió con la nota más alta son las piezas de **infraestructura de juicio** (sello, balanza) y las
tres leyes que crean un común nuevo o dan uso a un material mudo (aire con masa, haz por vidrio,
hielo con reserva).

## 3. Veredicto por lente: lo que sobrevive, con su ajuste

**Aire.** Sobrevive en dos entregas separadas por una prueba. **A, «el aire se gasta»** (1 semana):
un byte `aire` por celda (masa, conservada, difusión por aristas con doble búfer para no sesgar el
barrido), consumo en la combustión, `LabRespira` y `ProcessFire` que leen el byte (la llama inmortal
muere en un cuarto cerrado), rumbo del gas por viento posicional en vez de por hash, relajación lenta
hacia el nominal para que la asfixia sea local y no global, auditoría Σaire exacta. **B, «el aire
fluye»** (0,5 semanas, condicionada): el tiro por **presión** (`p = aire·(temp+120)`, con sesgo
hidrostático), no por comparación de masa, que según los dos refutadores no bombea. La prueba que lo
mata: escenario «chimenea con boca N» midiendo Σvy en la sección y el aire medio de la sala; si en dos
días B no separa la curva con chimenea de la curva tapada, B muere y A se queda. La advección de
**vapor** (no de calor: el humo ya nace caliente y viaja) se pliega en A/B con seis líneas. Los polvos
al viento se retiran como están (el polvo solo cae; la brasa nace en el sitio del combustible) y se
sustituyen, solo si B mide tiro, por «la chispa que sube» (una brasa corta que asciende por un conducto
con vy alto). **Aire atrapado** sobrevive reescrito: no en `ProcessLiquid` sino como propiedad de la
bolsa en `LabPresion` (BFS del aire sobre la superficie; una bolsa sellada resiste; con calor empuja):
campana, sifón que hay que cebar, bomba de Herón, 1-1,5 semanas.

**Luz.** Lo primero no es una ley: **un día** para añadir la quinta pasada descendente tras las
laterales en `LabLuz` (para que las caras cumplan la promesa del comentario :1284) y **remedir Q16 con
la luz que leen las plantas** (`luz[i+W]`). Si el refutador tiene razón, el huerto de referencia
vivía por luz y solo le faltaba el reparto del riego. Después, **haz y cuña** (1,5-2 semanas) solo con
`VidrioVerde` al principio (el hielo cuando un escenario lo produzca), con un decaimiento del haz en
aire como parámetro nuevo (`LuzDecayHaz` 2-3, alcance 85-127 celdas), cuña = celda de vidrio con
exactamente un lado abierto que gira el rayo 90°, la pasada difusa intacta, y como métrica el aire
sobre la cara y `LabPlantasNacidas` (escenario «periscopio de r148»: galería sobre el serpentín que
tapa la boca y dos cuñas de vidrio; aceptación: nacidas ≥ 4× las 2 de R148). **Insolación** sobrevive
como apéndice de dos líneas del haz: contar **rayos**, no potencia (`tope = min(HogarRaw, ambiente +
LuzSolRaw × nRayos)`, escrito con `LabCalentarHasta`); un sol seca, dos prenden yesca, nunca por
encima de 170 (respeta la frontera doméstico/industrial: el vidrio y el carbón siguen pidiendo horno).
**Ver por la luz** sobrevive como **vista** (`VistaLaboratorio.Ojo`: un velo con alfa `(255−luz)`
sobre la cuarta textura), 1-2 días, instrumento de verificación del haz, no render permanente. La
**lámpara portátil** muere con razón: una fuente de luz sin precio en otra ley destruye el mejor dilema
del laboratorio (abrir la boca del cielo o no); las fuentes son las que ya cobran (fuego, brasa,
hogar, cielo). **Filtros** muere como ley (no hay consumidor de luz bajo el agua); sus dos líneas de
transmisión por vidrio e hielo pasan al haz.

**Agua y frío.** **Fusión con reserva** sobrevive como núcleo (1-1,5 semanas): `LatenteFusion` en las
dos direcciones (el agua se clava a 0 °C mientras suelta latente; el hielo se come el calor que le dan
hasta agotar `aux` y vuelve a ser agua), coste escalado por volumen, sin `LatenteVapor` (la olla ya
está topada por `boilsAt`) y con la flotación como flag a medir en banco (los refutadores discrepan;
se mide, no se discute). Compra el fusible de hielo (un tapón que cede a una distancia medible del
fuego: temporizador de SOLTAR), el frío finito y transportable (el hielo ES el frío) y el baño maría.
**Convección en agua** sobrevive subordinada: sin anomalía de 4 °C (código muerto con raw de 2 °C),
umbral 1, `WakeChunk` cuando hay gradiente (R55 por una resta); compra que el estanque calentado no se
aclara y que evapora por toda la superficie. **Escarcha** muere como titular (la escarcha bajo el
serpentín cae al tick siguiente: hoy ya graniza) y sobrevive en tres líneas: contar `Freeze` en el
alambique (la tarde de banco), costra de hielo solo donde tendría apoyo, y las guardas `temp > 60` en
porosos y raíz (**la helada mata**: el serpentín que ahoga y sombrea ahora también hiela, tercer cruce
que nadie escribe). **Clima ganado** muere como está (elimina el único sumidero térmico y reabre la
regla 31) y se sustituye por **inercia térmica de la roca** (tres líneas, un parámetro: la roca vuelve
al ambiente ocho veces más despacio): sala tibia minutos después del fuego, bodega que guarda el frío,
sin campo nuevo.

**Cuerpo.** El hallazgo más incómodo del panel: **el cuerpo como conjunto de leyes no sobrevive; como
instrumento sí.** Las cuatro reglas caen por aritmética contra el código: el secado sobre 66 celdas
seca la ropa en un segundo; la inercia térmica del frasco no existe con el suelo de ±1 raw; el humo
nunca llega a la altura de la cabeza en los escenarios medidos (carbonera con boca 1 = 0 humo; pila en
caja sellada = 0,5-1,4 filas); el cuerpo no flota en una poza de 5 filas. Lo que queda, y vale, es
barato: `CuerpoSim {Calor, Mojado}` como **sensor** (el cuerpo es una celda gorda que lee lo que toca),
tinte del sprite por mojado, **vista Piel** (halo de 12 celdas de las vistas de calor y humedad,
siempre encendida: el rayos X pasa a ser el tacto), tizne del sprite y tos al cruzar humo, y **la brasa
que sigue viva en el frasco** (veinte líneas: fuego transportable con alcance real, brasa caída que
prende yesca). Media semana a una semana. Las consecuencias de control (manos que se abren, torpeza,
peso) son tuning de sensación y se posponen a después de un playtest, no antes.

**Organismos.** **Planta con órganos** sobrevive sin copa: `LabLuz` es un máximo con decaimiento que
rodea cualquier obstáculo estrecho, así que una hoja no da sombra sin luz direccional (eso es el haz).
La versión mínima (4 días a 2,5 semanas según alcance): **siega** (la planta entra en `Tallable` y da
fibra seca; lo de encima cae y la raíz rebrota con el código actual), **raíz que busca** (bebe del más
húmedo de tres celdas; la erosión protege lo que bebe), órgano en dos bits de `carga`. **Dispersión**
sobrevive solo como **arrastre** (la germinación espontánea ya existe: sembrar no es decisión mientras
`GerminaPorMil > 0`): el agua que fluye lleva lo que flota, con `SwapCells` y no `Move`, en la costura
`LabAguaFluyo` que ya existe para la erosión; el arroyo como cinta transportadora, 0,75 semanas.
**Compost** sobrevive (la fibra mojada, quieta y a oscuras se vuelve sedimento fértil: la erosión quita
suelo y la vida lo pone), el **musgo** se retira como ley (LabGotear ya gatea la roca; el higrómetro
verde se hace con tinte). La **polilla** muere: lee y nadie la lee (regla R48).

**Materiales.** **Vidrio transparente** sobrevive con la nota de tuning más alta del panel (8,5): un
día de física (`LabLuzDesde` transmite, `LuzDecayVidrio` en `dMin`, `EsRocaImpermeable` para que sude,
K y C de roca) y un escenario «invernadero». Es el único material que separa luz de agua y de humo, y
da uso al único producto permanente del horno. **Choque térmico** sobrevive con dos correcciones:
`ChoqueRaw = HogarRaw + 1` (por teorema nada calentado solo por hogares raja; solo llama y horno) y
choque solo con agua recién llegada (`reposo` bajo): la olla doméstica queda a salvo, la gota fría sobre
la bóveda del horno la abre. Memoria de cocción en `reposo`: la terracota como termómetro de máxima de
8 bits. **Hollín** sobrevive en v1: fuente humo/llama en roca impermeable, lavado a turbidez (humo →
hollín → agua turbia → sedimento, ciclo que nadie escribió), marca de marea solo en impermeables; el
render ya tizna (pátina) y hay que apagarlo donde el stepper lo sustituye. **Conductividad por
humedad** muere (`min(k)` la recorta contra roca; cero decisiones); queda como kill test de un día si
alguien lo quiere.

**Instrumentos.** **Huella** sobrevive (1 semana): un byte de máximos por celda (banda de calor, banda
de humedad, hollín), monótono, viaja en `SwapCells`, la física nunca lo lee, «siete hashes intactos,
octavo nuevo». Solo revela; pero revela después de ocurrido, que es lo que SOLTAR necesita. Con una
corrección: la planta solo muere por savia, así que la huella dice «nunca se mojó bastante», no «se
ahogó». **Bandas de ley** sobrevive como prerrequisito, no como juego: `LabBandas` como **única fuente de
umbrales** (leyendo `LabParams` y `Universe`, porque las igniciones viven en `MaterialDef` y el abono es
un literal), `Estado` y el lector la usan, la vista pinta bandas en vez de rampas; sin sondas con radio
(cero cruces, y el invitado no tiene campos). Medio semana; abarata todo lo demás. **La veta** muere
como ley y sobrevive como **herramienta del banco**: anillo de los siete hashes cada 256 ticks más
muestras fijas por escenario, impreso en `Informe`, para decir *cuándo* divergieron dos corridas (dos
días; acorta la iteración que la función objetivo quiere acortar). La veta en juego espera al sello.

**Métricas y SOLTAR.** Las dos piezas mejor valoradas del panel tras la refutación. **El sello**
(2,5-4 semanas): diario de intervenciones bajo las cinco puertas de `AlkahestSim` (Paint, PaintCell,
PaintStable, PaintRect, PaintLab; el cincel y el frasco pasan por ellas), aplicadas al principio de
`Step()` con tick; **volcado y carga de la rejilla** (el primer fichero de partida del proyecto, 100-300
KB); `CorrerSello(sello, registro)` que monta, rejuega y devuelve libro, hashes y veredicto; la
**condición como dato** (lista de cláusulas sobre el libro y sobre la grilla: «material en (x,y) == M»,
«campo en (x,y) ≥ v», «métrica por día ≥ n»); «DÍA N SIN MANOS» = último día en que todas las cláusulas
se cumplieron tras el último toque. Determinismo entre máquinas: el fichero lleva hashes, la
divergencia se detecta en vez de aceptarse. **La balanza** (1-1,5 semanas): el sumidero traga líquidos y
productos con bit `entregable` (carbón, ceniza, semilla; nunca fibra, grava ni sedimento, o se come su
materia prima), libro por salida (`aux` del sumidero como id) y por calidad (agua clara = pasó por
reposo, finos, calor). **La cuna** sobrevive con la corrección más importante del panel: el validador
por «hashes distintos» es una tautología (cada perturbación da otro hash); el validador correcto es por
**veredicto**: el registro vacío debe fallar, K perturbaciones sorteadas deben dar veredictos distintos
entre sí, el registro del autor debe cumplir. Y antes de sortear geología libre, **envejecer por
simulación las situaciones de autor** (los nueve montajes del banco y cámaras de 10-15 líneas): N ticks
headless antes de entrar, libro de nacimiento como etiqueta. `Clonar` en memoria para bifurcar sin
volcado. 4,5-5 semanas en total. **El testigo** muere como material (un sólido que retiene el máximo
radia como un hogar de 200 raw) y sobrevive como cláusulas de la condición del sello sobre los
materiales-testigo que las leyes ya producen: «planta viva en (118,250) el día 30», «terracota en la
boca del horno», «carga de la grava < 64». El jugador que quiera saber planta una semilla o pone un
terrón: sonda con consecuencias.

## 4. El sustrato que elijo: siete paquetes, y el orden

Los agrupo por lo que compran, con los números corregidos. Todos son acotados y se verifican en banco;
cada uno lleva la prueba más barata que lo mata.

| paquete | contenido (versión refutada) | semanas Opus | tuning | qué compra | prueba que lo mata |
|---|---|---|---|---|---|
| **J · Juicio** | sello (diario + volcado + `CorrerSello` + condición como dato) · balanza · cláusulas-testigo · `LabBandas` · anillo de hashes del banco | 5-6 | 8-9 | que las leyes **juzguen**: puntuación, veredicto diferido, comparación por fichero, validación de contenido sin personas; el primer guardado del proyecto | el alambique rejugado por la vía del registro (la caldera como 1 125 entradas) no reproduce sus hashes → la cola no es transparente |
| **L · Luz** | quinta pasada + remedir Q16 (1 día) · haz y cuña con vidrio · insolación por rayos · vista Ojo · vidrio transmite y suda | 3-3,5 | 7-8 | la luz como flujo **enrutable**; el vidrio con uso; el invernadero; encender yesca con sol; huertos bajo tierra | «periscopio de r148»: nacidas < 4× las 2 de R148 con dos cuñas → el haz no compra huerto |
| **A · Aire** | A (aire consumible, llama mortal, rumbo del gas por viento) · B (tiro por presión, condicionado) · vapor advectado · bolsa en `LabPresion` | 2,5-3 | 6-7 | el cuarto común de verdad: chimeneas que **emergen**, apagar cerrando, humo que se va, campanas y sifones que se ceban | «chimenea con boca N»: Σvy ≈ 0 con chimenea abierta frente a tapada en dos días → B muere, A se queda |
| **F · Frío** | fusión con reserva · convección en agua subordinada · helada mata + costra con apoyo · inercia térmica de la roca · contar Freeze | 2,5-3 | 7-8 | el frío **finito y transportable**; el fusible de hielo como temporizador de SOLTAR; baño maría; el serpentín que hiela | caja adiabática: `LabRawCongela != LabRawFusion` tras un ciclo → la reserva no conserva |
| **V · Vida** | siega + raíz que busca + órgano en `carga` · arrastre por agua · compost | 2,5-3 | 6-7 | el ciclo huerto → fibra → tolva **cierra** (cosecha con rebrote, fibra seca donde el jugador corta, el arroyo transporta); el suelo se repone | «huerto de banco» 36 000 ticks: población no se estabiliza (±20 %) o la fibra por planta supera a la siega |
| **M · Memoria** | huella de máximos · hollín v1 · choque térmico con memoria de cocción | 2,5-3 | 7 | el mundo se lee **después** de ocurrido; el horno tiene un segundo uso (cocer para templar); el primer accidente agua-fuego legible | «olla»: montaje (a) da choques > 0 con `ChoqueRaw = 171` → el guardián falla |
| **C · Cuerpo** | `CuerpoSim` sensor · vista Piel · tizne y tos · brasa viva en el frasco | 1-1,5 | 7 | el cuerpo como **primera sonda** y como portador de fuego con alcance; tacto en vez de panel | un día de banco leyendo `temp[]` en la caja del muñeco a 0, 2 y 6 celdas del hogar: si el cuerpo no distingue, no hay sensor |
| **D · Dones** (añadido por el crítico de completitud, §6) | el hogar come (consume carbón o fibra que lo toca; decae sin combustible) · el arroyo como refrigerante medido | 0,5-1 | 8 | el fuego doméstico como **apuesta**; el carbón con consumidor; SOLTAR arriesga suministro | «hogar sin combustible»: no decae en 3 000 ticks → el pin sigue siendo infinito |

Total si se hiciera todo: 19-23 semanas de Opus, en paralelo de dos en dos (J y L no se tocan; A y F
comparten térmica y van en serie; V y M son independientes). **No propongo hacerlo todo.** El orden
que la función objetivo dicta es:

1. **J primero y siempre**: sin el sello no hay veredicto, no hay fichero, no hay validación de
   situaciones ni comparación entre personas; es la infraestructura de «las leyes juzgan» y es la que
   menos tuning esconde (8-9).
2. **L segundo**, empezando por el día que remide Q16: es la apuesta con más valor por coste de todo el
   panel (puede reabrir H4 gratis) y la única que da uso al horno.
3. **A tercero**, porque su prueba de dos días decide si el aire es un común (tiro emergente) o solo un
   consumible (que ya vale: la llama inmortal muere y la carbonera se regula por caudal, no por
   geometría).
4. **V** y **F** después, en el orden que pida la dirección elegida (una dirección de jardín pide V
   antes; una de expediciones con hielo pide F antes).
5. **M** y **C** al final: revelan y dan cuerpo, pero no crean el juego.

Lo que dejo fuera del sustrato aunque sobreviviera: la escarcha como sistema, la polilla, la lámpara,
los polvos al viento sin tiro medido, la conductividad por humedad, el clima que recuerda, el testigo
como material, las sondas con radio y la veta en juego.

## 5. Lo que el panel dice sobre la pregunta final

Las leyes **ejecutan** ya (es el laboratorio). **Revelan** con M, C y la vista Ojo por unas 4-5
semanas y tuning 7. **Juzgan** con J por 5-6 semanas y tuning 8-9, y ahí está la respuesta a la
pregunta de Cesar: la balanza y el sello convierten cada contador del libro mayor en una puntuación
sin un solo número de balance humano (los únicos números humanos son qué es «agua clara» y cuánto
dura un «día»), la condición como dato hace que cualquier ley nueva entre en el recibo sin tocar nada
más, y el validador por veredicto decide si una situación vale sin que nadie la juegue. Lo que el
panel NO resolvió, y se lleva a las direcciones: si el jugador que prepara, suelta y lee un recibo
está jugando o rindiendo un examen. Eso no lo decide una ley.

## 6. Huecos y combinaciones (crítico de completitud)

Informe completo en `panel/leyes/_huecos_y_combinaciones.md`. Lo que cambia mi síntesis:

**Tres huecos que nadie propuso y que adopto.**

1. **Los tres dones son pins infinitos.** Hogar (170 raw eterno, sin combustible), manantial (24
   celdas/s, eterno) y núcleo frío. Con fuentes infinitas SOLTAR no arriesga nada por el suministro. El
   candidato que falta, con más apalancamiento por línea de todo el panel: **«el hogar come»**: el hogar
   doméstico consume el carbón o la fibra que lo toca y decae hacia el ambiente sin ellos. Vuelve
   apuesta el fuego doméstico, da al carbón un consumidor (hoy nadie lo usa), y cruza carbonera → hogar
   → huerto (secar la cosecha junto al hogar cuesta carbón). Medio semana; tuning 8 (dos números: cuánto
   come y cuánto tarda en decaer). El manantial se queda perpetuo a propósito: es el único flujo que
   sostiene un mundo que corre sin nadie, y ningún aparato lo usa todavía (el agua que fluye ya lleva
   su temperatura con `SwapCells`: el arroyo es refrigerante gratis y nadie lo midió). Lo añado como
   **paquete D · Dones** (0,5-1 semana).
2. **Los verbos del jugador.** Alcance de 60 celdas, 900 celdas aspiradas en un segundo, aprendiz que
   vuela, cincel de radio 2, frasco que no lleva fuego: el coste de intervenir es lo que vuelve
   decisión a cualquier ley, y tres candidatos del cuerpo murieron contra esos números. El sello
   cuenta toques; la física del toque (alcance, capacidad, sin vuelo) es diseño de control con tuning
   de sensación, así que no la meto en el sustrato: la meto en la lista de lo que la dirección elegida
   tiene que fijar en su prototipo feo, con el presupuesto de toques como palanca barata.
3. **Plantas × aire.** La entrega A del aire necesita una «relajación global» artificial porque la
   cueva es cerrada; una planta que repone `aire` donde tiene luz (una línea en `LabPlanta`) sustituye
   ese parámetro por una ley, da al huerto un producto antes de la fibra y acopla fuego y huerto en los
   dos sentidos. Entra en el paquete A.

Y dos que anoto sin adoptar todavía: **la edad como vista** (`touchedTick` ya existe por celda:
«¿ya llegó a régimen?» es la observabilidad más barata que la cuna y el sello necesitan; un día) y el
**sonido como campo** que atraviesa sólidos atenuado (el único sentido que cruza cámaras; asimetría
sin roles; sin hash ni tuning). Los dos son de la dirección, no del sustrato.

**Cinco contradicciones que hay que cerrar antes de construir nada** (las tomo como condiciones):
(a) el sello exige registro completo y el cuerpo escribe la grilla a frame rate: el cuerpo es
intervención tick-estampada o el sello muere; (b) el aire A exige que todo creador de materia desplace
aire: fase, compost y dispersión deben pagar; (c) el musgo tapona el goteo de todo alambique (por eso
lo retiré); (d) el haz mueve `HashLuz` en nueve escenarios y el vidrio promete tocar uno: misma
entrega o la promesa es falsa; (e) la balanza quiere fibra intocable y la dispersión la quiere
tragada: entregable = carbón, ceniza, semilla; la fibra ciega la boca, y eso también es juicio.

**La primera tanda que el crítico propone** (unas ocho semanas de Opus, todo en banco): aire A, fase
mínima, vidrio mínimo + vista Ojo, sello + balanza, ciclo mínimo de la planta. Deja fuera de la
primera tanda la huella (revela y no ejecuta; el diff del sello ya es forense) y el haz (dos semanas,
nueve hashes, y la escasez de luz es una decisión de Cesar, no una ley). Cuenta 28 decisiones nuevas
en la combinación, siete nacidas de cruces entre candidatos. Coincido en la tanda con dos cambios:
añado «el hogar come» (D), y mantengo **el día que remide Q16** antes que nada, porque decide si el
huerto vivía por luz y eso cambia el valor de todo lo óptico.

**Orden final del sustrato para las direcciones:** J (sello + balanza + bandas + anillo) → el día de
luz + vidrio mínimo + Ojo → A (aire consumible, con plantas que reponen) → D (el hogar come) → F
mínima (fusión con reserva) → V mínima (siega, raíz, arrastre, compost) → después, y solo si la
dirección lo pide: haz y cuña, huella, choque térmico, cuerpo como sensor, hollín.
