# DIRECCIÓN · CUERPO · «PRIMERA PIEDRA»

*(Panel de direcciones, segunda pasada. Ángulo: el cuerpo como instrumento y como riesgo. Contrastado con
`01_LEYES.md`, `panel/leyes/cuerpo.md`, sus ocho refutaciones y el código a R151. El panel demostró que el
cuerpo como conjunto de leyes NO sobrevive; esta dirección no lo resucita. Toma la única idea del cuerpo que
nadie refutó, la que el refutador del humo dejó al final de su documento («el cuerpo como obstáculo de
`ProcessGas`… el precio de ser el tapón; se refuta aparte»), y construye alrededor de ella.)*

## 1. Nombre y frase

**PRIMERA PIEDRA.**

«Eres la primera pieza de cada máquina: te sientas sobre el humo, te plantas en el arroyo, tapas el sol con
la espalda. Cuando entiendes lo que la máquina te hace, te sustituyes por una piedra y te vas. El mundo cuenta
cuántos días funciona sin ti.»

La tesis: **el cuerpo del jugador es un sólido de 7×11 celdas que se mueve.** Para el gas, el líquido, el
polvo, la luz y el fuego, el muñeco es una roca suelta con piernas. Todo aparato del laboratorio (la tapa de
la carbonera, la presa, el techo que quita el goteo, el corcho del sifón, la sombra) tiene un prototipo hecho
de cuerpo, y el juego consiste en pasar del prototipo a la piedra. Sin barras: el cuerpo tiene consecuencias
hacia fuera (lo que deja de sostener cuando se va, lo que trae en la ropa y en el frasco) y una lectura hacia
dentro (lo que siente donde está).

## 2. La experiencia narrada

**Tres decisiones de control que no son tuning.** Se juega **a pie** (`ModoMovimiento.SoloPies`, enum que
existe); el frasco solo aspira y vierte a **12 celdas** del cuerpo (`ReachWorld 6f → 1.2f`); y el **polvo en
reposo es suelo** (`CajaChoca` cuenta como sólido un polvo con `reposo ≥ ReposoMovil`). Sin esas tres el
cuerpo no necesita estar en ningún sitio («nadie necesita estar en el penacho para cargar el horno», dice la
refutación de C1). Con ellas la materia es la escalera: viertes un montón para subir, y el montón desvía el agua.

**Los diez primeros minutos.**

*Minuto 0-1 · «El charco».* Apareces a pie en una cueva de 96×64. Sin texto. Alrededor del muñeco, un halo
de 12 celdas enseña lo que el resto del mundo no enseña: calor y humedad en las bandas de `LabBandas`. Es la
vista Piel, siempre encendida: **el rayos X es el tacto.** Fuera del halo, el mundo se ve por la luz que le
llega (vista Ojo: velo por `255 − luz`); la boca del cielo ilumina, el túnel del fondo es negro. Cruzas un
charco: las botas se oscurecen (tinte por `Mojado`), el halo enseña la humedad bajando a tu paso, y si te
quedas quieto goteas. Aprendes sin una palabra que el cuerpo lee y que el cuerpo carga.

*Minuto 1-3 · «La tapa».* Debajo, una cámara con un hogar y una pila de fibra que ya arde (la cuna la
envejeció 20 s antes de que entraras); en su techo, una boca de tres celdas; encima, un lecho húmedo con una
semilla y la boca del cielo. El humo sale por la boca y se come la luz del lecho: la semilla no germina. Una
inscripción de piedra dice la condición: «carbón ≥ 30 · planta viva». Te acercas: el humo te da en las
piernas y el sprite se tizna. Te sientas sobre la boca; tu caja de 6,4 celdas la cubre entera. El humo deja
de salir. En el halo, los pies se ponen rojos. Abajo, el humo que no puede subir toca la llama: `LabRespira`
la pone en sordina y la fibra se vuelve carbón (curva HF1: boca 1 → 100 % carbón). Arriba, la luz vuelve al
lecho y la semilla brota. Pulsas ×10 sentado: un día en cuarenta segundos. Te levantas: el humo sale a
borbotones, la planta se oscurece. Dos palabras: **TAPA HUMANA.** Vuelves, viertes sedimento sobre la boca
(tienes que estar ahí: 12 celdas), y te vas. Arranca un contador: **DÍA 1 SIN CUERPO.**

*Minuto 4-6 · «La presa».* Un arroyo de manantial a sumidero (20 celdas/s) pasa junto a un lecho seco con
una semilla; el canal tiene cuatro celdas de ancho. Te plantas en el canal. El agua se apila contra tu
espalda (eres pared para `ProcessLiquid`), sube tres celdas (`DesnivelMin`), rebosa la orilla y el halo enseña
el lecho oscureciéndose. La semilla brota. Das un paso atrás: la ola, el nivel cae, el lecho se seca en
minutos. **LA PRESA ERAS TÚ.** Viertes arcilla donde estabas (compacta con la humedad), sueltas, cuentas días.

*Minuto 7-10 · «La brasa».* Un hogar a la izquierda; un fogón de yesca a 150 celdas a la derecha. Aspiras
una brasa del hogar (tocando su costado: el halo se pone rojo, te tiznas). La brasa sigue viva en el frasco y
muere en 8-12 s: 120-180 celdas de alcance a pie. Corres. El icono de la brasa se apaga por el camino. Llegas
y viertes: la yesca prende (≈80 %) o la brasa murió a diez celdas del fogón y aprendes a construir un fogón
de relevo a mitad de camino. Si vienes con los pantalones mojados de la presa y te quedas quieto sobre la
yesca, goteas siete veces y ya no prende (`FibraMojadaMin 100`). Humor que las leyes producen solas.

**Hora 1.** Diez a catorce situaciones, cada una **una ley y un papel del cuerpo**: tapa (gas), presa
(líquido), sombra (tu cuerpo sobre la boca del cielo apaga la germinación), techo (el goteo del alambique te
cae en la cabeza y resbala: el lecho de abajo deja de ahogarse), corcho (tu cuerpo en la boca de un tubo en U;
te vas y el sifón se ceba), esponja, termómetro (caminar el alambique con el halo hasta el vecino más frío).
Ninguna tiene texto: tienen una condición y un cuerpo.

**Hora 5.** Situaciones de dos leyes con geología sorteada por la cuna: el alambique que riega y sombrea
(R135 y R148 juntos), la carbonera que pide yesca, el horno que pide 200 raw en recinto. El cuerpo ya casi no
se usa como pieza: se usa como sonda (dos segundos junto a la pared para leer el calor) y la sustitución
llega antes. La medida pasa de «lo conseguiste» a **cuántos días sin cuerpo** y a la balanza. Comparas con un
amigo por fichero: misma situación, dos sellos, dos recibos.

**Hora 20.** El sandbox: una situación grande sin horizonte (el pozo o la ladera como geología, sin
estaciones ni roles), con las condiciones como retos opcionales. Con dos o tres personas en host+espejo, el
verbo que emerge es **«sujeta esto mientras…»**: uno tapa la boca con el cuerpo mientras el otro talla la
chimenea. Cualquiera es piedra, cualquiera cava. La información asimétrica es temporal y física: cada cual
siente su halo («¿está caliente ahí?» «tengo las piernas rojas»). La culpa es atribuible por ausencia.

**Mientras la simulación corre** haces dos cosas: ser la pieza (segundos a un minuto a ×10, la fase de
prototipo) o ir a sentir (caminar con el halo es la única forma de ver los campos de cerca). En cuanto te
sustituyes, el juego te empuja fuera: el contador penaliza volver.

**Cómo lees el mundo.** Tres resoluciones. Cerca y en vivo, el halo (bandas, 12 celdas). Lejos y en vivo, la
luz: lo iluminado se ve; el humo que come luz te ciega sin viñeta ni tos. Después, el sello rejugado con los
rayos X completos: «después de ocurrido» es cuando la huella y el recibo tienen sentido. Los instrumentos que
las leyes producen (terracota, grava colmatada, rocío) siguen gratis.

**Cómo se aprende.** Sin paisaje: situaciones pequeñas ordenadas por la cuna. El validador por veredicto se
extiende con **K perturbaciones del guion del cuerpo**: si mover el cuerpo cambia el veredicto, el cuerpo
importa en esa situación y el validador sabe en qué ley; la campaña se ordena por leyes implicadas, sin
diseñador. Segundo filtro automático: **alcanzable a pie** (BFS sobre transitables con salto de 22 celdas y
ancho de 7): ninguna geometría que no se pueda andar llega a nadie.

## 3. Core loop

«Pon el cuerpo donde falta la pieza, siente lo que la máquina te hace, sustitúyete por materia y suelta: las
leyes cuentan cuántos días funciona sin ti.»

```
situación (cuna: geología envejecida + condición como dato + alcanzable a pie)
  → PREPARAR      cavar, apilar, prender; frasco de 12 celdas; el montón es la escalera
  → SER LA PIEZA  tapa / presa / techo / corcho / sombra: cuerpo = sólido 7×11 para gas, líquido, polvo, luz, fuego
       ↳ SENTIR   halo Piel (calor, humedad), mojado, tizne; lejos, solo lo que la luz enseña
  → SUSTITUIRSE   roca suelta, arcilla, sedimento, vidrio donde estaba el cuerpo
  → SOLTAR        ×10; el sello registra el último toque, incluido el último tick con cuerpo dentro
  → VEREDICTO     balanza + cláusulas; «DÍA N SIN CUERPO»; replay con rayos X completos
  → siguiente situación (o la misma, con un amigo, por fichero)
```

## 4. Por qué explota mejor la simulación

**Ejecutan.** Una máscara de hasta cuatro cajas consultada donde las pasadas preguntan `mat == Empty`
convierte al cuerpo en pieza de cinco leyes sin escribir ninguna nueva: el gas no sube a través de ti
(`ProcessGas`), el líquido no entra en tu caja (`ProcessLiquid`; la presión por cuerpos conectados hace el
resto), el polvo se apila encima (`ProcessPowder`), el fuego no te cuenta como aire (`LabRespira`: sentarse
sobre una brasa la ahoga), la luz no te atraviesa (`LabLuzDesde`). Las curvas ya están medidas: la tapa
humana produce el carbón de HF1, la presa humana el desnivel de `LabPresion`, la sombra humana los 7/73 de
R148. El cuerpo no añade un fenómeno: añade una **piedra que decide**, y es el mejor tutorial de cada
máquina porque es la máquina.

**Revelan.** El halo es `LabVistaColor` recortada a 12 celdas; el tinte es `LabTinte`; el tizne, la rampa de
hollín de la pátina; el vaho, `SpawnSteamPuff`. Cero render nuevo. Y la propia sim mide si el cuerpo fue
pieza: un contador `BloqueosCuerpo` por tick (movimientos que la máscara rechazó). Un tick con bloqueos es un
tick «con cuerpo». **«DÍA N SIN CUERPO» lo define la física, no un diseñador.**

**Juzgan.** Con J: balanza y cláusulas; el diario del sello lleva la posición del cuerpo tick a tick (una
intervención más, como el frasco), así que veredicto, comparación por fichero y replay con el cuerpo como
piedra fantasma salen sin trabajo extra. Sin J: `LabBench.Informe` con conteos por región y plantas nacidas
juzga las seis primeras situaciones.

**Contenido sin autor.** Todo hueco de una geología sorteada es un sitio donde el cuerpo puede ser pieza: no
hay «puestos» que diseñar.

**En banco.** Guion `(tick) → (x, y)` como entrada, igual que la caldera del alambique; hashes de los nueve
escenarios idénticos sin cuerpo; «tapa humana» contra «tapa de piedra»; «presa humana» (desnivel sostenido);
«esponja» (curva de `Mojado`, goteos hasta `FibraMojadaMin`); «brasa con alcance» (`Ignite > 0` en las 9
seeds); coste por tick +≤5 %.

## 5. Qué añade al sustrato

| pieza | qué es | cruza | coste | tuning |
|---|---|---|---|---|
| **Cuerpo sólido** (nuevo; el «tapón» que nadie refutó) | `CuerpoSim[4] {X0, Y0}`; posición escrita antes de `Step()` por `LabCuerpo` (`DefaultExecutionOrder(−10)`); helper `Libre(idx)` = `Empty && !Ocupa(idx)` en los 26 puntos de `Empty`; `WakeChunk` sobre la caja; las celdas dentro de la caja nunca se reescriben (la materia sale, no se borra: conservación intacta); `BloqueosCuerpo`; guion en banco; `X0, Y0` en el hash | gas, líquido, polvo, luz, fuego, presión | 1 sem | **0** (binaria) |
| **Piel sensor** (paquete C refutado) | `Calor`, `Mojado` con paso térmico normalizado por celda y secado por 4 caras; tinte; halo 12; tizne | térmica, agua, yesca mojada | 0,75 sem | 2 números de banco (`cCarne` 8-16, `caras` 4) |
| **Brasa viva en el frasco** (ajuste de C2) | `_vidaBrasa` en `Flask`; `PaintCell` con `aux` | fuego → yesca → carbón | 0,25 sem | 0 |
| **A pie, 12 celdas, polvo en reposo es suelo** | tres constantes y un predicado | todo lo anterior | 0,25 sem | 1 sesión |
| **Vista Ojo** (paquete L) | velo `255 − luz`; el humo ciega porque come luz | luz, humo | 0,2 sem | 0 |
| **Validador a pie** (extensión de la cuna) | BFS con salto 22 y ancho 7; K perturbaciones del guion | J | 0,5 sem | 0 |

Depende de J para el veredicto completo (ya primero en el orden de `01`). Total propio: 3 semanas de Opus.
No añade, aunque la lente lo propusiera: manos que se abren, torpeza, peso, flotación, viñeta, tos con corte
de input, aliento, condensar sobre el cuerpo. Todo eso es tuning de sensación o cayó por aritmética.

## 6. Principal riesgo de diseño

**Que ser la pieza sea esperar.** La crítica a Sin Manos («los momentos de más tensión te prohíben tocar»)
aquí es peor: te obligan a quedarte. La mitigación es estructural: la fase de cuerpo es el prototipo
(segundos a un minuto a ×10) y el juego penaliza volver; si en el playtest la gente se queda sentada sobre la
boca «porque funciona», lo dirá el propio contador (cero días). Segundo: **el riesgo para el cuerpo es
delgado.** Nada le pasa salvo mojarse, calentarse y tiznarse; el riesgo es lo que dejas de sostener y lo que
traes. Si Cesar quiere un Noita donde el cuerpo sufre, esta dirección no lo es y no debe fingirlo. Tercero:
a pie en geología sorteada = atascarse; lo cubren el BFS y la escalera de materia.

## 7. Cuánto depende de iteración humana

**Cuánto tuning esconde la sensación de control y cómo lo acoto.** El salto y el paso costaron nueve rondas
(R110 → R121b). Esta dirección **no toca ni uno de esos números**: cambia tres conmutadores (modo, alcance,
polvo en reposo) y todo lo demás que hace el cuerpo es binario (bloquea o no) o lo fija el banco. Lo humano,
con número de sesiones:

- 1 sesión: ¿12 celdas de alcance se sienten «tacto» o correa? (12 contra 20; una constante).
- 1 sesión con 5 desconocidos: láminas del halo (G6 de la primera pasada, tal cual).
- 1 sesión con 2 personas: las tres primeras situaciones sin texto; ¿descubren tapa, presa y brasa?
- Después, ~1 sesión por cada 8-10 situaciones, solo para el «sin texto»; la validez la da el validador.

El banco sustituye: `cCarne`, `caras`, la equivalencia tapa humana/piedra, el alcance de la brasa, los
goteos, la alcanzabilidad, la validez de cada situación y la definición de «con cuerpo».

## 8. La prueba más barata capaz de matarla

**Dos días de banco tras dos días de arquitectura** (posición como entrada, guion, máscara). «Tapa humana»
(`MontarCarbonera`, cuerpo a horcajadas de la boca en (101, 224) 9 000 ticks, contra roca suelta en la boca)
y «presa humana» (canal de 4 celdas, manantial a sumidero, cuerpo quieto 3 000 ticks, contra muro de
arcilla). **Muere si** el carbón con cuerpo es < 90 % del carbón con piedra (fugas por la costura frame/tick);
o el desnivel con cuerpo no sostiene `DesnivelMin`; o algún hash de los nueve se mueve sin cuerpo; o el tick
sube > 5 %. Si muere, el cuerpo vuelve a ser el paquete C (sensor) y deja de ser dirección.

**Segunda prueba, una sesión**: el hermano de Cesar a pie en el nivel del laboratorio; si no llega a la boca
de la carbonera en tres minutos sin volar, «a pie» muere pero la pieza sobrevive en vuelo (hover en la boca;
peor, no muerto).

## 9. Tiempos

- **Hasta evidencia para matarla: 1 semana** (técnico, automatizable, sin personas).
- **Hasta prototipo feo: 4 semanas de Opus, 3 de pared.** Paralelo: cuerpo sólido ∥ piel + brasa + Ojo;
  después seis situaciones montadas como los `Montar*` del banco con cláusulas sobre la grilla (sin volcado;
  el sello completo llega con J) y el validador a pie. Dependencia secuencial única: la posición como
  entrada del tick antes de todo (3 días).
- **Iteración humana: 3 sesiones en las tres primeras semanas**, luego una por cada 8-10 situaciones. Nota 7.

## 10. Lo que deja fuera y por qué

Refutado por aritmética o tuning: manos que se abren, torpeza, peso, flotación, viñeta, tos con corte,
aliento, curvas de fatiga. Por función objetivo: barras; cuerpo como material en `mat[]`; lámpara;
estaciones; bibliotecas de ruinas y cámaras (la cuna sortea, el validador filtra); roles por geografía;
multiplayer online ahora. Órganos de la primera pasada que descarto: el cuaderno falsable (el replay con
rayos X completos dice la verdad), bichos-sensor y sondas con radio (el cuerpo es el sensor), el censo de vida
(la balanza lo cubre), la vigilia desaturada, la polilla, el testigo como material, El Pozo como columna
vertebral (queda como situación grande sin horizonte). Conservo: SOLTAR y el contador (definido por la
física), el veredicto diferido, los instrumentos que las leyes producen, la huella, el asíncrono por fichero.

**Posición honesta.** Como producto independiente, «Primera piedra» es la campaña de situaciones juzgadas con
un cuerpo dentro: compite con la dirección de campaña y es casi la misma más una ley. Su valor real es de
**capa**: el mejor motor de onboarding y de clip que el sustrato puede tener por 3 semanas y tuning casi nulo
(el hueco «entre la primera sorpresa y la primera máquina» se cierra porque la primera máquina eres tú), y se
injerta en cualquier dirección que use situaciones y sello. Si el panel elige una dirección sin avatar, de
esta se salvan la brasa viva, la vista Ojo y «con cuerpo» medido por bloqueos. Lo que no debe hacerse en
ningún caso: prometer un cuerpo que sufre y afinarlo a mano.

## 11. Autoevaluación (rúbrica v2)

| eje | nota | por qué |
|---|---|---|
| apalancamiento_sistemico | 7 | una máscara cruza cinco leyes con curvas ya medidas; no crea fenómeno, crea una piedra que decide |
| leyes_ejecutan_revelan_juzgan | 7 | ejecutan (pieza), revelan (halo, bloqueos), juzgan solo con J |
| iteracion_humana | 7 | tres conmutadores, dos números de banco, tres sesiones; el salto no se toca |
| verificabilidad_automatizable | 8 | guion como entrada, hashes, equivalencia con piedra, BFS |
| simulacion_es_el_juego | 8 | el cuerpo es una celda gorda; la única capa es un contador que la física define |
| onboarding_garantizable | 8 | una ley y un papel por situación; validador con perturbaciones del cuerpo |
| observabilidad | 7 | halo + luz + replay; que lejos no se lea lo decide G6 |
| tiempo_como_apuesta | 7 | sustituirse y soltar; días sin cuerpo |
| multiplayer_emergente | 6 | «sujeta esto mientras…» sin roles; halos asimétricos; fichero; nada probado |
| profundidad_por_leyes_estables | 6 | la profundidad es del sustrato; el cuerpo añade prototipo→piedra a cada ley y luego se agota |
| cuerpo_del_jugador | 8 | instrumento pleno; el riesgo va hacia fuera, no hacia dentro |
| identidad_comercial | 7 | «TAPA HUMANA» y «LA PRESA ERAS TÚ»: clips con culpa por ausencia; la cápsula depende del arte |
| dificultad_tecnica | 7 | acotada; lo sutil es la costura frame/tick y el halo del invitado sin `temp[]` (ruta A) |
