# CRÍTICA · «Días sin manos» (campaña de situaciones + sandbox) · lente: ITERACIÓN HUMANA OCULTA

*(Panel de direcciones, segunda pasada, 2026-09-12. Versión revisada tras la reanudación del workflow:
sustituye a la de las 13:11 conservando sus hallazgos (presentación como bolsa mayor, descubribilidad,
Etapa 2 asimétrica, contenido real 30-80, escalera de 7-11 peldaños, horizonte derivado del
decaimiento) y añade siete partidas que faltaban: el treadmill de versiones de física, la unidad del
«toque», la semántica de las cláusulas, la decisión muerta «reiniciar gratis», el peldaño de vida sin
madre viva, el sandbox heredado de P4 y el rendimiento de curación como criterio de muerte. Crítico:
productor; único tema: cuánta iteración humana esconde y qué parte se sustituye por banco. Leídos
enteros: `campana-situaciones.md`, `01_LEYES.md`, las refutaciones de sello (2) y cuna (2),
`_huecos_y_combinaciones.md`, `03_MERCADO.md`, `00_ENCARGO_Y_CRITERIO.md`, las dos críticas hermanas
de esta dirección y las de iteración humana de «El Recibo» y «Sin Manos» para calibrar la escala.
Código: `LabBench.cs` (`Escenarios` :76-88; `Correr` :265-335 y su única intervención, cableada a
`esAlambique` :295-311), `LabParams.cs` (interruptores :110-148; `Registro` estático), `LabPresets.cs`
(`Cargar` :158: solo parámetros), `LabPanel.cs`, `Cincel.cs` (`TallarTick` :383-475, `CarveRatePerTick
= 3`), `AlkahestSim.cs` (:267 construcción del nivel; `LabMultiplicador` :57-400) y el banco
`2026-09-05_r148_h7s_arco_largo.md`.)*

## 0. Veredicto: FINALISTA, con la cifra de iteración corregida a la baja y cinco condiciones

Es la dirección del panel que menos playtest infinito esconde: la validez, la resolubilidad, la
fragilidad, el orden, la detección de copias y la regresión del contenido los decide un banco por
veredicto, no una persona; no tiene temporadas, eventos, economía que balancear ni contingencias ante
lo que el jugador construya. Lo que esconde es **contenido pequeño y acotado más los mandos de una
máquina**, y eso es el cuadrante que la función objetivo prefiere. Pero la cifra declarada («tres o
cuatro sesiones», unos 4-6 días de persona) es **cinco a siete veces menor que la real**: 22-32 días
en los tres primeros meses con los automatismos de §3, 28-45 sin ellos. Y tiene una propiedad que
ninguna otra dirección tiene: si su máquina falla, degenera en «El Recibo» (12-20 situaciones de
autor ordenadas por firma) y todo lo construido vale. Es la dirección de menor arrepentimiento del
panel; no es la de menor iteración que declara.

## 1. Riesgo mayor (desde esta lente)

**Que la máquina no sustituya la autoría sino que la desplace a la curación y a la re-autoría.** Dos
mecanismos, los dos leídos en el código y en las refutaciones:

- *Curación.* El validador garantiza que un mutante discrimina (vacío falla / autor cumple) y que el
  solver lo resuelve; no garantiza que **enseñe**. La refutación de apalancamiento de la cuna lo dejó
  escrito: «cien cuevas que mirar en vez de doce». Y el validador es un filtro pasa-bajos: entra lo que
  la solución del autor sigue resolviendo (el mismo puzle con otra pared) o lo que resuelven seis
  verbos (puzle fácil); lo que exige una idea nueva va al sandbox. Cada lote pide 1-2 h de Cesar por
  madre mirando mutantes. Si menos de la mitad de lo validado es publicable, la máquina multiplica el
  trabajo caro en vez de sustituirlo.
- *Caducidad.* La dirección promete que el hash de versión hace la regresión automática. Cierto para la
  **detección**; falso para la **reparación**. Cada paquete que la propia dirección planifica como
  peldaño futuro (A, V, F, M, haz) mueve hashes en varios montajes; la fracción de soluciones de autor
  que deja de cumplir la re-resuelve una persona (1-2 días por paquete) y el lote entero (firma más
  solver: 1-4 días de reloj en ocho procesos, §2) se re-corre antes de que la escalera vuelva a ser
  cierta. Al ritmo del laboratorio (21 rondas en 3 días) la escalera nunca estaría al día. La dirección
  obliga a **congelar la física por versión de escalera** y fundir cambios en lotes; es una regla de
  producción sana, pero hay que escribirla y contradice en parte «pospuestos, no descartados».

El segundo riesgo, compartido con toda dirección sobre J, es el examen diagnosticado tarde: la Etapa 2
tal como está no puede matarla (§4), y sin instrumentar la sesión el «otra vez» se decide en la semana
7 con el sello construido.

## 2. Iteración humana oculta, pieza a pieza

| pieza | lo que declara | lo que esconde | cuánto (días de persona) |
|---|---|---|---|
| **Madres** (12-20, «15 líneas, medio día de Opus») | escribirlas es barato (cierto: `MontarHorno` son 15 líneas) | decidir qué **enseña** cada una, elegir métrica y horizonte, escribir la solución de autor como intervenciones y leer la tabla del banco; con el pasa-bajos, más de una solución por familia si se quiere dificultad | 8-12 (0,5-1 por madre) |
| **Plantillas de condición y factor f** («cuatro o cinco números para todo el juego») | f fijo por familia | f cruza con horizonte y geometría; un f único deja unas triviales y otras imposibles; el validador **poda**, no afina: la tasa de poda es el tuning | 1-2 + noches de banco; automatizable (§3.2) |
| **Semántica de las cláusulas** (nueva) | no aparece | R148: humedad media del lecho **43 → 125 entre el minuto 25 y el 30**, alrededor del mínimo 60; «último día en que todas se cumplieron tras el último toque» **parpadea** salvo que alguien fije agregación (instantánea, mínimo, media, mayoría) e histéresis por tipo de cláusula; cruza todas las situaciones y depende de la escala de oscilación de cada campo; la longitud del día (1 800 ticks) está acoplada | 2-3 + una vuelta por ley nueva; automatizable (§3.3) |
| **Unidad del toque** (nueva) | «cada toque es una entrada del registro»; «1 toque» en el recibo | `TallarTick` llama a `Paint`/`PaintLab` hasta `CarveRatePerTick = 3` celdas por tick mientras se mantiene el botón: un segundo de cincel son hasta 90 entradas del diario; el frasco igual por `TickSuck/TickPour`. «Toque» exige definición (trazo contiguo de la misma herramienta, celda, cambio de herramienta) y esa definición es el **segundo eje del recibo** y la escala de toda comparación entre registros y del contador del solver | 1 + una ronda; automatizable la sensibilidad (§3.4) |
| **Verbos del jugador** | cincel y frasco heredados | el pincel del `LabPanel` pinta cualquier material con radio: si entra como verbo, toda condición se resuelve pintando (el crítico de huecos ya lo dijo); hay que fijar «nada crea materia» antes del primer banco o el banco mide un juego que no será | 1 (decisión) |
| **Horizonte por madre** (3-30 días) | un número por situación | son 12-20 números humanos y hoy son el mando de dificultad; con manantial y hogar eternos, las madres de agua sin colmatación **no decaen**: 3 días vale lo mismo que 300 y el horizonte es cosmético; la tensión de tocar o confiar solo existe donde cruza una ley de decaimiento (grava que se colmata, combustible finito, hogar que come, planta sin savia) | 0 si se deriva (§3.1); 3-4 si no |
| **Gramática de mutación y macroverbos** | «boca ±k, veta más gruesa, hogar movido»; 5-6 verbos | operadores y rangos **por familia** (mover el hogar del horno cambia todo; en la carbonera casi nada), K y H por madre, leer el lote para podar recetas; el vocabulario del solver no es el del jugador (cincel radio 2, frasco de 900 celdas/s, alcance 60): **resoluble no es descubrible**, y al revés, soluciones humanas fuera del vocabulario que el contador de «distintas» no reconoce | 3-4 inicial + 1 por lote |
| **Curación de mutantes** (nueva como partida) | «validada, firmada y ordenada sin que nadie la juegue» | ¿enseña? lo mira una persona; el rendimiento de curación es la cifra que decide si la máquina ahorra o multiplica (§1) | 2-4 por lote |
| **Granularidad del bitmask y veto de la escalera** | «el autor decide el primer peldaño y nada más» | qué es «una ley» (¿`LabPoroso` entero o infiltración, capilaridad, compactación y cocción por separado?) decide cuántos peldaños hay; la firma mide **de qué ley depende el veredicto**, no qué tiene que entender el jugador: alguien lee la escalera y veta. Y sin **sustraer la base** (agua, manantial-sumidero, infiltración: toda situación de agua las firma) la firma degenera; sustraída quedan 7-11 bits de lección, o sea 7-11 peldaños | 1-2 |
| **Peldaño de vida** (nueva) | «planta viva en (118,250) el día 30» como cláusula de ejemplo | siete rondas (R134-R150) no consiguieron un huerto vivo en el nivel de referencia: R148 2 nacidas y 0 vivas, R150 9 nacidas y 9 muertas por humedad de raíz. La cláusula no tiene madre con solución; R150 lo dejó como «geometría de nivel», es decir, para que lo resuelva una persona | 2-5 a mano; 0 con solver de riego (§3.6) o cortando el peldaño |
| **Presentación**: condición en «dos barras sin texto», recibo por día como vector, película con scrub y **vista de diferencia**, bandas, Ojo, Piel | «dos o tres láminas, cinco desconocidos» | cada plantilla de condición es un diseño de UI distinto (4-5); el diff entre volcados hay que decidir qué muestra; hoy todo se ve con F8 y las plantas son un píxel; es exactamente donde murieron Clockwork Empires y Maia (`03_MERCADO` §3). Es la bolsa incomprimible mayor | 4-6 (3-5 sesiones de láminas) |
| **«El hogar come»** (0,5 sem, tuning 7) | una ley pequeña | la tasa decide cuántas celdas de fibra son 30 días (1 celda/8 ticks = 6 750 celdas, imposible en 128×72; 1/100 = 540, una tolva); sin brasa en el frasco (paquete C, fuera de la dirección) un hogar muerto es reinicio, no decisión; y ocho de nueve montajes llevan `Hogar`: entra como material nuevo o mueve ocho hashes | 1 (sensación); la factibilidad por barrido |
| **«Reiniciar gratis domina a tocar»** (nueva; de la crítica purista) | «tocar o confiar, con reloj» | tocar pone el contador a cero y gasta horizonte; reiniciar devuelve todo y borra los toques del recibo; la cámara entra idéntica cada vez. Nadie toca a mitad: reinicia. La Etapa 2 «dos de tres reintentan» se cumple siempre sin que nadie haya apostado | 0 de tuning; 1 sesión para confirmarlo; la corrección (cámara persistente) es una regla, no un número |
| **La espera a ×10** | «preparar ya es mirar correr el mundo» | 30 días = 54 000 ticks = 3 min a ×10 sin poder tocar ni rebobinar; R148 dice que en la única corrida de 30 días **nada cambia de estado después del día 5** («el resto del arco, sin actividad»); tedio medible solo con personas | 1 sesión; o horizontes ≤ 15 días |
| **Sandbox de la hora 20** (nueva) | «lo que ya existe, sin horizonte» | es P4 con su tramo manual sin medir; los retos derivados («≥ f × nacimiento») son automáticos, el lugar no | 3-5 si entra; 0 si se corta del prototipo |
| **Relevo, bifurcación y cámaras de la comunidad** | «casi gratis por determinismo» | determinismo probado en UNA máquina (IL2CPP sin medir: un día de banco); relevo y bifurcación exigen amigos con la misma versión de física intercambiando ficheros a mano (comportamiento humano específico, pequeño); las cámaras de la comunidad son curación sin autor | 1 de banco; comunidad fuera del prototipo |
| **Re-resolver tras cada paquete de física** (nueva) | «hash de versión» | ver §1: detección automática, reparación humana; más 1-4 días de reloj de lote por cambio | 1-2 por paquete (A, V, F, M, haz) |
| **El «otra vez»** | una sesión | binario, barato, incomprimible; ruidosa en la semana 3 (sin bandas ni película, un FALLA no tiene causa), limpia en la 7 | 3-4 (ruidosa, limpia, dos de seguimiento) |

**Suma.** 28-45 días de persona en los tres primeros meses tal como está escrita; **22-32 con los
automatismos de §3 y cortando sandbox y comunidad del prototipo**; declarados 4-6. En la misma lente,
«El Recibo» salió en 28-40 tal como está escrita y 14-22 con sus sustituciones, y «Sin Manos» en 20-30
y 10-15: esta dirección esconde algo más una vez automatizada porque su máquina tiene mandos propios
(gramática, macroverbos, bitmask, curación), y lo compensa en parte porque el solver es el histograma
sintético que «El Recibo» tenía que construir aparte y porque la regresión de mutantes viene incluida.

**Coste de reloj, no de personas, pero fija la cadencia.** `LabParams` es estático: un mundo por
proceso. Firma: |L| = 12-16 bits × H = 18 000-54 000 ticks × 1,6-3 ms = 7-32 min por situación; 100-300
situaciones = 12-160 h en un proceso, 1,5-20 h en ocho. Solver: K = 48 × H × 2-3 ms = 30-130 min por
mutante; 300 mutantes = 145-650 h, 18-80 h en ocho procesos. **Un lote completo son 1-4 días de reloj**,
y se re-corre con cada cambio de física. «Cien situaciones en una noche» vale para la firma en un
hilo; el solver es una semana de máquina por lote.

Lo que la dirección **no** esconde, y conviene decirlo: temporadas cero, eventos cero, geometría
arbitraria a balancear cero (los retos son relativos al recibo de nacimiento y el solver decide
factibilidad), biblioteca cero (12-20 madres). Casi nada de lo oculto es balance de un espacio infinito;
es contenido acotado, mandos de máquina y presentación.

## 3. Qué se automatiza en lugar de esa iteración

1. **Horizonte por madre, del banco**: H = f × el día en que la solución del autor deja de cumplir sin
   mantenimiento (la película del banco lo encuentra; f es de la familia de plantillas). Si no falla en
   60 días, la madre no tiene apuesta: H = 3 y se etiqueta «de aprendizaje». Elimina 12-20 números
   humanos y hace explícito qué madres cruzan una ley de decaimiento. Y el **validador de reloj**: una
   madre entra en la escalera solo si algo se degrada entre el día 1 y H; sin reloj va al sandbox como
   lámina.
2. **Factor f por barrido con criterio fijo**: f tal que el vacío falla en ≥ 90 % de los mutantes de la
   familia y el autor cumple en ≥ 70 %; si ningún f lo da, la familia se poda. Cero gusto.
3. **Semántica de cláusula por estabilidad**: evaluar cada cláusula con las cuatro agregaciones y con
   jitter de ±100 ticks en el tick de sello; quedarse con la que no mueve «DÍA N». Contar el parpadeo
   por día con `ArcoMuestra`, que ya existe. Un día de Opus; se reusa para toda ley nueva.
4. **Unidad del toque por sensibilidad**: implementar dos definiciones (trazo contiguo; celda) y
   comprobar en banco si cambia el orden de los K registros del solver y del autor; si no cambia, la
   definición es libre; si cambia, se elige la que separa más. Un día.
5. **Firma-lección = firma(situación) menos la intersección de las firmas del peldaño 0**, medida hoy
   por **parámetros a cero** (~15 ejes de `LabParams`: erosión, compactación, colmatación, decantación,
   capilaridad, infiltración, evaporación, condensación, germinación, presión, carbón, caudal, hogar,
   frío, luz) antes de escribir el bitmask; de los cinco interruptores que cita la dirección,
   `CuerposActivos` gatea un gancho vacío y `TermicaPropia = 0` sustituye la térmica en vez de quitarla.
   La **hermana de contraste se genera**: el mutante cuya firma difiere en exactamente un bit. Y la vista
   por peldaño se elige sola: la banda del campo que la firma-lección añade.
6. **Peldaño de vida por solver, no por diseño**: barrido nocturno de geometrías de riego (canal L,
   anchura de boca, posición del serpentín, lecho sobre grava) contra «planta viva día 30» con la luz
   que leen las plantas (`luz[i+W]`, el día de Q16). Si ninguna geometría vive, el peldaño se corta sin
   que nadie haya iterado a mano.
7. **Curación medida**: rendimiento de curación (publicables / validados) como métrica del lote; si baja
   de 0,5 en dos lotes, se amplía el solver o se poda la familia, no se añade gente.
8. **El lote como puerta de fusión**: ningún cambio de física entra sin re-correr el lote en N procesos
   batch-mode; la escalera lleva hash de versión; los registros rotos se listan solos. La re-autoría
   sigue siendo humana, pero la lista y la cadencia no.
9. **La película la produce el banco, no la espera**: al soltar, el resto del horizonte se corre headless
   en segundo plano (1,6-1,9 ms/tick, ×15-20 real) y el jugador recorre el time-lapse como quiera; el
   ×10 en vivo es opcional. Conserva «no hay rebobinar» (el horizonte se gasta igual) y quita el tedio.
10. **Descubribilidad, proxy parcial**: por familia, cuántas soluciones humanas caen fuera del
    vocabulario del solver; si más del 30 %, ampliar el vocabulario (código), no el playtest.
11. **Telemetría del diario**: la sesión humana la instrumenta el propio sello (reintentos, toques a
    mitad de corrida, tiempo mirando, día del último toque): la lectura sigue siendo humana, el conteo no.
12. **Relight del hogar**: en vez de la brasa en el frasco (paquete C), la yesca que toca un hogar
    apagado con temperatura > 130 raw lo reenciende: una línea en `LabHogar`, verificable en banco.
13. **IL2CPP contra editor**: los 63 hashes, un día, antes de prometer el fichero que circula.

## 4. La prueba más barata que la mata (corregida)

**La Etapa 1 tal como está no arriesga nada.** Los nueve montajes llevan su solución dentro (el horno
ES el recinto, la carbonera ES la boca de 1, el alambique ES el serpentín más la caldera); o el vacío ya
cumple, o hay que partir cada montaje en madre y registro, y entonces quien escribe las dos mitades
garantiza que discriminen: cuatro discriminan seguro por medidas hechas (alambique 0 contra 902
goteos, horno 18 contra 0, carbonera boca 1 contra abierta, hervidero con barra contra sin barra),
laboratorio base y arco largo no pueden («planta viva»), mundo despierto no es situación, tolva y
diluvio dependen de autoría. «5 de 9» es una decisión disfrazada de umbral. Y la ablación con los cinco
interruptores citados tiene tres bits reales (presión, germinación, luz): colapso por construcción.

**Semanas 1-2, banco, sin motor nuevo, cero días de Cesar salvo media jornada.**

1. `Intervencion[]` genérica tick-estampada en `Correr` (la caldera pasa a ser la primera entrada;
   un día). Regla de verbos fijada antes: nada crea materia. Unidad de toque: las dos definiciones.
2. Cuatro madres = «montaje − solución» (alambique, horno, carbonera, hervidero) con condición sobre
   libro o grilla y **sonda por día** de la condición (un día).
3. Ocho mutantes por madre con tres operadores (boca ±k, hogar/fuente ±x, grosor ±1); solución del
   autor rejugada sobre cada uno a 3×H; recibo cuantizado a 8 tramos; **día de ruptura** de cada
   solución (un día).
4. Firma por parámetros a cero sobre 12 ejes, madres y mutantes válidos, H = 9 000-18 000, base
   sustraída (un día de código, 3-4 h de banco en 2-4 procesos).
5. **Rendimiento de curación** (media jornada de Cesar, semana 2): 20 mutantes validados, sin saber
   cuál es la madre; marca los que pondría en una campaña y por qué.
6. **Regresión del día de Q16** (semana 2): el cambio de física ya planificado (quinta pasada
   descendente en `LabLuz` + remedir con `luz[i+W]`) se aplica y el lote se re-corre; se cuenta qué
   soluciones de autor y qué mutantes dejan de cumplir.

**La mata** si: menos de 3 firmas distintas entre las cuatro madres (la escalera no existe); o más del
80 % de los mutantes válidos con el mismo recibo cuantizado que su madre (fabrica copias); o menos del
30 % de mutantes válidos con la solución del autor (no fabrica nada: la campaña vuelve a ser
biblioteca); o **rendimiento de curación < 0,5** (multiplica la curación); o **más del 50 % de las
soluciones de autor rotas por un cambio de física de un día** (el treadmill hace la campaña
incompatible con A/V/F/M sin congelar la física, y entonces «pospuestos» es «descartados»); o **siete
o más de las nueve soluciones de referencia nunca se caen en 30 días** (no hay relojes a escala de
días: aprobado/suspenso el día 1, examen por construcción).

**Semana 3, personas, con 3-4 días de preparación para que pueda matar.** Los presets del `LabPanel`
guardan `LabParams`, no geometría (`LabPresets.Cargar` :158 escribe el `Registro`): hace falta un
selector de montaje en la construcción del nivel (`AlkahestSim` :267, un día), el contador de días en
pantalla y **una línea del libro por día** con el día del FALLA («día 4: sumidero 0 de 1 260»). Tres
personas, el hermano de Cesar entre ellas, cinco madres, una con reloj visible («la grava se cansa»),
el diario contando. Se mide: (a) toques a mitad de corrida contra reinicios; (b) ¿alguien suelta antes
del horizonte por decisión propia?; (c) ¿alguien dice **por qué** falló leyendo solo la línea del día?;
(d) toques humanos contra toques del solver. **Mata**: cero toques a mitad en tres personas (la
decisión está muerta: reiniciar domina), o nadie suelta, o nadie explica un FALLA con la línea (la
película con diff no va a salvar lo que una línea por día no explica), o los tres necesitan más de 3×
los toques del solver o no resuelven 3 de 5 (resoluble no es descubrible: el validador no sustituye el
playtest de dificultad). «Dos de tres reintentan» se retira como medida: reintentar gratis lo cumple
cualquiera.

## 5. Tiempos corregidos

- **Hasta evidencia para matarla: 3 semanas** (2 de banco que matan la máquina, el treadmill y los
  relojes sin una persona; 1 más de selector, contador y línea por día hasta la sesión ruidosa que mata
  la apuesta y la legibilidad mínima). La dirección decía 2. El examen limpio, con bandas y película,
  se juzga en la semana 7.
- **Hasta prototipo feo que permita juzgar el core: 7 semanas** (Pintor común en `Sim/` + diario bajo
  las cinco puertas + volcado con round-trip de r141 + `CorrerSello` con condición como dato y días sin
  manos, 4 en serie; gesto de soltar, contador, cámara persistente y sonda por día, 1; bandas en
  pantalla y scrub con diff, 1-1,5 solapadas; ocho madres de los montajes con reloj medido y firma por
  parámetros, 1). Firma automática, solver y mutación quedan fuera del prototipo: son fábrica, no bucle.
  La dirección decía 6; la diferencia es el carril de UI, que no se verifica en banco y pasa por Cesar.
- **Automatizable y paralelizable** (segundo carril, Opus): balanza `entregable`, `LabBandas` fuente
  única, bandas + Ojo + Piel, día de Q16 + vidrio, hogar que come como material nuevo con banco propio,
  runner de N procesos batch-mode, evaluador de cláusulas con jitter, solver de riego, IL2CPP: 6-7
  semanas.
- **Secuencial** (camino crítico): Pintor común → diario → volcado/carga → `CorrerSello` → validador +
  envejecer + `Clonar` → mutación → bitmask + firma → escalera + recibo cuantizado → solver: 7,5-8,5
  semanas en serie. Total Opus 13-16 (declaradas 11-13); reloj con dos carriles, 9-10.
- **Iteración humana**: 22-32 días de persona en los tres primeros meses con los automatismos (28-45
  sin ellos), frente a 4-6 declarados. Está al principio y es en su mayor parte binaria (¿enseña?, ¿se
  lee?, ¿hay otra vez?): la forma correcta de gastar el recurso caro. Después, 3-5 días por lote de 20-30
  situaciones y 1-2 por paquete de física.

## 6. Órganos a conservar si se descarta

El sello con condición como dato y el recibo por día como vector (comparación por dominancia);
`Intervencion[]` genérica en `Correr` y «montaje − solución» como forma canónica de todo escenario del
banco; el validador honesto (vacío falla / autor cumple / fragilidad) más el **validador de reloj**; el
horizonte derivado del día de ruptura; la semántica de cláusulas por estabilidad bajo jitter; la firma
por ablación con base sustraída (ordena cualquier onboarding, con cámaras o sin ellas; hoy por
parámetros a cero); la hermana de contraste generada; el rendimiento de curación como métrica de lote;
el lote como puerta de fusión con hash de versión de física; envejecer madres por simulación; la
velocidad como estado; el hogar que come como material nuevo con su banco; la película producida por el
banco; el solver de riego como forma de cerrar o cortar H4 sin iterar a mano; la telemetría del diario
en sesión; la prueba IL2CPP contra los 63 hashes.

## 7. Puntuaciones (rúbrica v2, 13 ejes)

| eje | nota | por qué (desde esta lente) |
|---|---|---|
| apalancamiento sistémico | 6 | infraestructura de juicio con una ley nueva sin refutar; multiplica cruces existentes, no crea; el multiplicador por madre es 2-4×, no 10× |
| las leyes ejecutan, revelan y juzgan | 8 | condición como dato, balanza, días sin manos, película y diff; los números humanos que quedan (f, horizonte, agregación, tasa del hogar, unidad del toque) son derivables, pero hoy no lo están |
| iteración humana (10 = poca) | 6 | 22-32 días frente a 4-6 declarados; sin balance de espacio infinito ni temporadas; lo que más crece es curación + re-autoría por versión; 7 con los automatismos de §3 aplicados |
| verificabilidad automatizable | 9 | discriminación, resolubilidad, fragilidad, firma, orden, regresión, copias, reloj, curación medida: todo headless; el coste es de reloj, no de personas |
| la simulación es el juego | 7 | marco delgado; la sim evalúa problemas que escribe un autor; el riesgo del examen es que el marco pese más que la física |
| onboarding garantizable | 8 | firma con base sustraída + hermanas generadas; escalera corta (7-11 peldaños); el peldaño de vida no tiene madre viva |
| observabilidad | 6,5 | bandas, Ojo, Piel, diff: todo del carril paralelo; hoy F8 y plantas de un píxel; Edad no es edad (es la guarda de reentrada) |
| tiempo como apuesta | 6,5 | solo donde hay ley de decaimiento; reiniciar gratis domina a tocar tal como está; 8 con cámara persistente, validador de reloj y hogar que come |
| multiplayer emergente | 5 | relevo y bifurcación por fichero: comparar deberes; determinismo entre máquinas sin medir |
| profundidad por leyes estables | 6 | crece por ley y por madre; 12 familias, 30-80 situaciones reales; cada paquete de física reinicia el lote |
| cuerpo del jugador | 4 | sensor y registro; en 96×64 con alcance 60 no hay geografía que sentir |
| identidad comercial | 6,5 | «DÍA N SIN MANOS» y el clip del fracaso valen; sin clip co-op; la espera a ×10 no se streamea sola; base expediciones 60-150 k |
| dificultad técnica (10 = fácil) | 7 | C# puro en banco; el bitmask atraviesa todas las pasadas Lab*, el estado escondido del volcado y el Pintor común son lo delicado; el lote es fuerza bruta en procesos |

Suma ponderada: 162 / 240. Puertas: iteración 6 y apalancamiento 6, las dos por encima de 5. Pasa.

## 8. Crítica razonada

He visto morir sistémicos indie en el playtest infinito: el contenido se valida jugándolo, cada cambio
de física invalida lo validado, y el equipo entra en un bucle donde cada semana de tuning compra media
de regresión. Esta dirección ataca ese bucle en su raíz: la validez, la resolubilidad, la fragilidad,
el orden y la detección de copias los decide un banco por veredicto; no hay temporadas, ni eventos, ni
economía que balancear, ni contingencias ante lo que el jugador construya, porque las leyes lo
ejecutan y el sello lo mide. Lo que esconde es contenido pequeño y acotado más los mandos de una
máquina: el cuadrante que la función objetivo prefiere. Pero la cifra declarada, tres o cuatro
sesiones, es cinco a siete veces menor que la real, y conviene decir dónde está la diferencia.

Primera partida: la máquina no sustituye la autoría, la desplaza a la curación. El validador garantiza
que un mutante discrimina y que el solver lo resuelve; no garantiza que enseñe. La refutación de la
cuna lo dijo: cien cuevas que mirar en vez de doce. Y el validador es un filtro pasa-bajos: entra lo que
la solución del autor sigue resolviendo o lo que resuelven seis verbos; lo que exige una idea nueva va
al sandbox. Las «100-300 situaciones» son 12-20 lecciones con sus variantes de práctica, y cada lote
pide una o dos horas de Cesar por madre. Aceptable si el rendimiento de curación es alto; si menos de
la mitad de lo validado es publicable, la máquina multiplica el trabajo caro. Se mide en media jornada
en la semana dos.

Segunda: el treadmill de versiones. El hash de versión hace automática la detección de la regresión;
la reparación no. Cada paquete que la propia dirección planifica como peldaño (A, V, F, M, haz) mueve
hashes en varios montajes; la fracción de soluciones de autor que deja de cumplir la re-resuelve una
persona, uno o dos días por paquete, y el lote entero (firma más solver, 1-4 días de reloj en ocho
procesos) se re-corre antes de que la escalera vuelva a ser cierta. Al ritmo del laboratorio,
veintiuna rondas en tres días, la escalera nunca estaría al día: la dirección obliga a congelar la
física por versión de escalera y a fundir cambios en lotes. Hay que escribirlo, y contradice en parte
«pospuestos, no descartados». El día de Q16, ya planificado, es la prueba gratis: lote antes y después,
y contar qué se rompió.

Tercera: cuatro números que la dirección no cuenta como humanos. La unidad del toque: `TallarTick`
llama a las puertas hasta tres celdas por tick mientras se mantiene el botón, así que un segundo de
cincel son hasta noventa entradas del diario; «un toque» exige una definición, y esa definición es el
segundo eje del recibo y la escala de toda comparación. La semántica de las cláusulas: en R148 la
humedad del lecho pasa de 43 a 125 entre el minuto 25 y el 30, alrededor del mínimo de 60; «último día
en que todas se cumplieron» parpadea salvo que alguien fije agregación e histéresis por tipo de
cláusula. El horizonte por madre. Y la tasa del hogar que come, que decide si treinta días son una
tolva o un silo imposible. Sustitutos: definición de toque por sensibilidad sobre los registros del
solver, agregación por estabilidad bajo jitter, horizonte igual a f por el día de ruptura, tasa por
barrido contra la tolva medida. Un día de Opus cada uno; quitan diez o doce días de persona.

Cuarta: lo que no se automatiza y la dirección declara pequeño. La presentación (condición sin texto,
recibo por día como vector, película y diff, bandas): cuatro o cinco diseños de UI, láminas con
desconocidos, tres a cinco sesiones, donde murieron Clockwork Empires y Maia. El peldaño de vida no
tiene madre viva (R148 y R150: nueve nacidas, nueve muertas): o un solver de geometrías de riego lo
encuentra en una noche o se corta. El sandbox de la hora 20 hereda el tramo manual de P4 sin medir:
fuera del prototipo. Y la Etapa 2 no mide nada tal como está: reiniciar es gratis y borra los toques,
así que «dos de tres reintentan» se cumple siempre y nadie toca a mitad de corrida; hay que contar
toques a mitad contra reinicios, y hacen falta el selector de montaje (los presets guardan parámetros,
no geometría), el contador y una línea del libro por día para que un FALLA tenga causa.

La cuenta honesta son 22-32 días de persona con los automatismos, 28-45 sin ellos, frente a 4-6
declarados. Sigue siendo la dirección con menos iteración humana por unidad de contenido del panel, y
con una propiedad que ninguna otra tiene: si la máquina falla, degenera en «El Recibo» con doce a
veinte situaciones ordenadas por firma, y todo lo construido vale. Tres semanas hasta evidencia para
matarla, siete hasta el prototipo feo. Finalista, con cinco condiciones: física congelada por versión
de escalera con el lote como puerta de fusión; toque y verbos definidos antes del primer banco;
horizonte, agregación y f del banco, no de la mano; vida por solver de riego o cortada; sandbox y
comunidad fuera del prototipo.
