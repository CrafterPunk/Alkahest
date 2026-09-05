# HANDOFF PARA TRABAJAR SOLO CON OPUS HASTA EL SÁBADO (Fable 5.1, 2026-09-04, R141)

Léelo después de `CHECKPOINT.md` §9. Es corto a propósito: dice qué puede hacer Opus sin Fable, cómo
se juega H7, qué capturar para el diseño comercial que viene después, y qué se aparca.

**(R147, 2026-09-06) H7 con jugador queda DEFERIDO por decisión de Cesar; §2 y §3 se conservan para la prueba de experiencia futura. Lo vigente: `ALCANCE.md` §5 y `HANDOFF_OPUS.md` §8 (HF5e-B → H7s autónomo → H8 → revisión conjunta → diseño comercial).**

## 0. Estado en tres líneas

- **Física congelada desde R141** (decisión de Cesar). Fuego 4/5 medido y honesto; agua 5/5; la
  vegetación se mide en H7. Orden: **HF5c + H5** (una ronda) → **H7** (jugar) → **H8** (informe).
  H6 (sólidos) documentado y congelado.
- Comunicación: `PREGUNTAS_A_FABLE.md` «## Abiertas» para lo que se aparca (formato de siempre:
  fecha · hito · pregunta · propuesta · qué hiciste mientras); `CHECKPOINT.md` §9 para el estado;
  `HISTORIAL_RONDAS.md` + `ca_playtestNNN.cmd` por ronda. Cesar no tiene que pegar mensajes.
- Regla de oro mientras Fable no está: **medir, anotar, seguir con lo que no dependa de la duda.**
  Nunca esperar parado.

## 1. Qué ejecuta y decide Opus solo (después de HF5c + H5)

**Decide solo, sin escalar:**
- Herramienta: `LabBench`, panel, vistas, lector, presets, snapshots, capturas, textos y ayudas.
- Geometría del **nivel de referencia** cuando una medida lo justifique, con la medida en el
  benchmark (como el desagüe: si el banco dice que empeora, fuera).
- Rendimiento (acotar `LabLuz`, chunks, presupuesto) **si el hash de todos los escenarios del banco
  no cambia**. Si un cambio de rendimiento cambia un solo hash, es un cambio de física: se aparca.
- Bugs que contradigan un docblock o una medida aceptada (R49: promesa sin línea es bug), gateados
  por `LabActivo`, dentro de las costuras documentadas en `HANDOFF_OPUS.md` §2. Si el arreglo
  necesita salir de una costura, se aparca con el diagnóstico escrito.
- Documentación: reordenar, indexar, corregir cifras con la medida delante.

**No decide solo (se aparca en «Abiertas» y se sigue con otra cosa):**
- Cualquier regla nueva de simulación, array nuevo por celda, o cambio de un número de física en
  `LabParams` (salvo devolverlo a un valor ya medido).
- Tocar la lengua de `ProcessFire`, `TryIgnite`, `AddTemp`/`InjectHeat`, `Net/`, o archivos de la
  campaña (bautizo, álbum, `SubstanceKnowledge`, encargos).
- H6, el renombre/poda estructural, y todo lo comercial (onboarding, economía, campaña): de eso
  solo se **observa** (§3), no se diseña.

## 2. Protocolo exacto de H7 (el arco largo)

**Para qué.** Saber si un jugador descubre las máquinas sin que nadie se lo diga, y dejar el
material de la sección A del informe y del diseño comercial posterior. No se fuerza nada.

**Montaje.** Build de R142 (HF5c + H5 hechos), `_defaults.json` de fábrica, ×1 por defecto (×5/×10
permitidos, pero cada cambio de velocidad se anota). Cesar juega y **piensa en voz alta**; Opus
observa por MCP (captura cada 3 minutos de mundo, libro y snapshot en cada «¿por qué pasó eso?»)
y **no interviene ni explica** salvo que Cesar lo pida tras ≥ 5 minutos atascado; cada ayuda se
anota como intervención (I).

**(R144, R21) Si hay un segundo jugador que no ha visto nada, juega PRIMERO y es LA medida de onboarding (una sesión, tres frases, entrevista de tres preguntas al final); Cesar mide profundidad después. Nunca se promedian.**

**Tres sesiones** de 30-40 minutos de mundo (o una larga partida en tres tramos):
1. **Sin objetivo**: llegar, mirar, tocar. Cesar sabe demasiado; da igual: se anota igual.
2. **Con un objetivo elegido por Cesar** entre: hacer vidrio · tener el huerto vivo 10 minutos con
   su propio riego · hacer carbón y usarlo · mover el agua de la poza a otro sitio sin frasco.
3. **Libre**: construir lo que le apetezca. Es la sesión que más vale para §3.

**Registro** en `Laboratorio/h7/sesion_NN.md`, una línea por evento: `mm:ss (tick) · acción ·
intención dicha en voz alta · resultado visto · S/C/I` (S sorpresa, C confusión, I intervención).
Snapshots `h7_sNN_mmm` + captura en cada S y cada C. Al final de cada sesión, la tabla:

| métrica | valor |
|---|---|
| intervenciones (índice de tedio: ≤ 1 por 10 min) | |
| tiempo hasta el primer descubrimiento de cada máquina (alambique, horno, carbonera, tolva, huerto) | |
| momentos «¿por qué?» (C) y «¡anda!» (S) | |
| atascos (> 5 min sin avanzar) y reinicios | |
| uso del panel: pestañas abiertas, parámetros tocados, presets cargados | |
| uso de ×5/×10: cuándo lo quiso y cuándo le hizo perderse algo | |
| distancia recorrida y pantallas visitadas | |
| materiales pintados (el pincel es trampa: anotarlo es la medida de lo que falta como herramienta) | |
| frasco, cincel, mudanza: cuántas veces y para qué | |

**Aceptación de H7** (honesta, cualquiera que sea el resultado): al menos 2 de las 5 máquinas
descubiertas sin ayuda, o la evidencia de por qué no; huerto vivo a los 10 minutos con riego del
jugador (aceptación de H4); cadena cruzada del fuego (criterio 4) vista o no vista, con snapshot.
Fable puntúa el criterio 4 el sábado con ese material.

## 3. Qué capturar en H7 para el diseño comercial (solo observar; no diseñar)

Todo con marca de tiempo, cita literal de Cesar y snapshot cuando se pueda. Opus lo compila al
final en `docs/LAB/OBSERVACIONES_H7.md` con estos siete encabezados y una lista final «diez cosas
que el juego ya sabe de sí mismo».

- **Onboarding.** Los primeros cinco minutos: qué miró primero, qué tocó primero, qué le sugirió el
  mundo por sí solo (¿el hogar, el arroyo, la poza, la luz?), qué no entendió (nombres, estados,
  el panel). La pregunta clave: *¿qué única frase, dicha en el minuto 0, le habría ahorrado más
  confusión?* Anotar también lo que entendió sin que nadie se lo dijera.
- **Escala.** Distancias entre máquinas y pantallas por viaje; si el mundo se sintió grande o
  pequeño; cuánto quiso esperar en cada proceso lento (gotear, carbonizar, crecer, decantar): el
  **umbral de aburrimiento** por proceso, en segundos, y cuándo el ×10 le hizo perderse algo.
- **Controles.** Cada entrada que falló o sorprendió (pincel, catálogo, cincel, frasco, F8,
  arrastre, teclas), cada «quería X y pasó Y», cada control que echó de menos. Cuántas veces abrió
  el panel y para qué pestaña.
- **Transporte.** Cómo movió materia (frasco, cincel, pincel del laboratorio), cuántos viajes, dónde
  deseó un canal, un conducto o un elevador; si «llevar agua al huerto» fue tedio; qué diría de un
  cubo, un carro o una tubería. No se implementa nada: se anota el deseo y el momento.
- **Diversión.** Los momentos de risa o «¡anda!» (tiempo + snapshot), lo que repitió por gusto, lo
  que se narró a sí mismo, qué máquina le gustó más y qué quiso construir después.
- **Confusión.** Cada «¿por qué?»; si el lector y los nombres lo resolvieron; los **modelos
  mentales equivocados** escritos literalmente («creía que el fuego necesitaba…»); lecturas
  visuales erróneas (humo/vapor, sedimento/arcilla, carbón/ceniza, agua turbia/limpia).
- **Progresión.** Orden real de los descubrimientos; cuál habilitó cuál; dónde se estancó; qué
  «siguiente meta» se puso solo; si la escalera que sintió primero fue la de la materia (agua) o la
  de la energía (fuego); qué quiso que le contara el libro (ayuda del panel) y qué prefirió
  descubrir.

Y una regla: si Cesar dice «esto lo vendería así…», se anota entre comillas y **no se actúa**.

## 4. Si H7 y H8 terminan antes de que vuelva Fable

Puede avanzar, en este orden:
1. `OBSERVACIONES_H7.md` (§3) y los logs de sesión limpios.
2. **H8, borrador** de `docs/LAB/INFORME_FINAL.md` con las secciones A-F del encargo, marcando con
   «[FABLE]» cada juicio que quiera segunda opinión (la valoración C y la estimación E en meses).
3. Una tabla **«lo que el laboratorio ya sabe / todavía no sabe»**: hechos medidos con su
   benchmark a la izquierda, preguntas abiertas a la derecha. Sin propuestas de diseño.
4. Rendimiento (continuación de H5) con la condición del §1: mismo hash en todos los escenarios.
5. `MULTIPLAYER.md`: actualizar la tabla por sistema con los contadores nuevos (todos del host).
6. H6: consolidar la hipótesis de sólidos en `DISENO_LABORATORIO.md` §8 como documento de la etapa
   futura. Solo texto.
7. Limpieza de docs e índices; galería de capturas para el informe.

No puede: empezar H6, tocar física, diseñar la capa comercial, renombrar/podar, ni publicar
artefactos (los `.md` quedan listos y Fable los publica el sábado).

## 5. Decisiones aparcadas para Fable (lista viva; añadir en «Abiertas»)

- El paquete «la llama es el sensor» (tiro de chimenea): archivado, solo si Cesar quiere tiro.
- Memoria por celda para la carbonización (identidad exacta en vez de estadística): no, salvo que
  H7 lo pida.
- Si en H7 el huerto no vive con el riego del jugador: qué regla o qué geometría (régimen de
  plantas). Es la única física que podría descongelarse, y la decide Cesar con Fable.
- Cualquier cambio de frecuencia de `LabCampos` o de tamaño de chunk que pida H5.
- La nota del criterio 4 del fuego tras H7, y la valoración C y la estimación E del informe.
- Todo lo comercial: onboarding, escala definitiva, controles, transporte, progresión. Se diseña el
  sábado a partir de `OBSERVACIONES_H7.md`, no antes.
- La ronda estructural de renombre/poda.

## 6. El sábado

Fable revisa HF5c, H5 y los logs de H7 con banco propio y revisión adversaria (como en R138 y
R141), puntúa el criterio 4, cierra el informe con Opus, y empieza el diseño de la experiencia
comercial desde §3. Hasta entonces: medir, anotar, seguir.
