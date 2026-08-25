# EL ESTUDIO DE NOMBRES — Semilla Cero (R89)

Encargo de Cesar: "que las cosas adicionales que ahora son solo descriptivas tengan nombres
REALES donde se pueda, para que la gente pueda rastrear o usar su conocimiento del mundo
real — así la fantasía de 'si tú eres capaz de reencontrar todo si desaparece mañana' se
juega." Estudio hecho con los ojos de Opus LEYENDO EL CÓDIGO (no la prosa).

## El hallazgo que precedió a todo

**Los nombres reales YA EXISTÍAN.** `Universe.ConstruirIdentidadReal()` guarda 48 entradas
(nombre real + mini-reseña de trivia por material) desde la línea de "identidad real" de
rondas pasadas — pero el PRÓLOGO no las leía: los nombres pedían `ModoSemillaCero` y el
prólogo corre con `ModoFundacion` (la química y los colores sí usaban la condición doble;
los nombres eran el único sistema desalineado). Lo que Cesar veía eran los nombres
PROVISIONALES del generador descriptivo:

| Lo que se veía | Ya se llamaba (tabla real) | Fidelidad |
|---|---|---|
| sedimento cobrizo | **arcilla** | 92% — soluble, se calcina a chamota insoluble (la irreversibilidad real) |
| tinte pardo | **barbotina** | 88% — término técnico REAL de alfarería; googlearlo enseña el engobe, que el juego ya usa |
| lágrima gris | **gres** | 55% — el juego lo logra por temple; el gres real es cocción alta (mentira parcial asumida) |
| colada cobriza | **barro vitrificado** | 62% — la reseña ya confiesa el atajo |
| lodo mojado | **barbotina** (el mismo) | — |
| humo | **humo** | ✓ |

Y el mapeo real de las bases: base0=**arena de sílice** (→vidrio), base1=**arcilla**
(→adobe/cerámica/gres), base2=**caliza** (→cal viva/cal apagada), base3=**veta vegetal/
turba** (→carbón vegetal), base4=**sal**. Más cruces ya jugables: mortero, clínker,
hormigón, vidrio verde (arena+ceniza: la lección del fundente), lejía (ceniza+agua),
esmaltado.

## Lo aplicado en la R89

1. **Enchufada la tabla al prólogo** (`SubstanceKnowledge`, 2 sitios): los nombres reales
   ganan también en `ModoFundacion`. "Sedimento cobrizo" ES "arcilla" desde ya.
2. **"Cal viva" cumplía su reseña de mentira**: prometía "con agua reacciona caliente" y la
   reacción NO EXISTÍA (la peor clase de mentira: la que el conocimiento real invita a
   comprobar). Ahora existe: cal viva + agua → cal apagada ("apagando la cal"), a
   cualquier tier — la hidratación real es exotérmica de suyo.
3. **Solubles por decreto completados**: el comentario del override 6b prometía caliza y
   sal solubles desde la ronda 56, pero el código solo escribía la arcilla — "agua de cal"
   y "salmuera" podían ser nombres de materiales inalcanzables según el sorteo. Decretado.
4. **"Arena" vs "arena de sílice"**: dos materiales distintos compartían palabra en
   pantalla. El clásico `Sand` ahora es **"arena de río"** ("granos de mil rocas; esta no
   da vidrio") — honesta y rastreable.

## Lo que NO se toca (a sabiendas)

- **Slime, Azoth, CrystalSeed, Crystal, Vivium, Acid**: la capa alquímica sorteada no
  tiene contraparte real honesta — se describen por efecto/origen, sin mentir.
- **La arena disuelta**: sin entrada A PROPÓSITO — la sílice no se disuelve; la ficha "?"
  eterna ES la lección (sedimenta).
- **El nombre provisional en modo CAÓTICO**: ahí el bautizo es la mecánica y "sedimento
  cobrizo" es exactamente lo que debe salir.

## Riesgos abiertos (backlog honesto)

- **Las 8 leyes SORTEADAS de la seed contradicen los nombres reales** ("caliza + turba →
  agua + vapor" es química falsa con nombres verdaderos). Con nombres descriptivos era
  magia aceptable; con nombres reales hay que filtrarlas o re-cablearlas en la seed de
  autor. ES EL TRABAJO GRANDE que este pivote destapa — pendiente de decisión.
- **Densidades sorteadas**: la columna de sedimentación puede enseñar densidades al revés
  de las reales (solo la turba flota por decreto). Mismo tratamiento pendiente.
- **"Gres" por temple** (55%): si duele, la salida es renombrar a algo neutro o dar la
  ruta de cocción — decisión de Cesar.
