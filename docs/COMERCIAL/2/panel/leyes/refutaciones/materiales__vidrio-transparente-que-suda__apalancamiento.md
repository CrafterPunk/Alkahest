# Refutación · «Vidrio que deja pasar la luz y suda» · lente apalancamiento

**Veredicto: viable con ajuste.** El cruce es real pero es UNO, no cinco; el apalancamiento declarado (9) está inflado al doble, y la verificación prometida no pasa tal como está escrita.

## 1. Cruces nuevos de verdad: uno

Cuatro de los cinco «cruces» son propiedades que la piedra ya tiene:

- **Humo.** `ProcessGas` (SimStepper.cs) solo mueve gas a `Empty`: cualquier sólido bloquea el humo.
- **Vapor.** `LabEsSuperficieCondensable` y `LabGotear` (SimStepper.Laboratorio.cs:277, 317) ya condensan y gotean sobre `EsRocaImpermeable`; la cámara de roca ya recircula su agua (el documento lo admite: «con roca ya pasaba»).
- **Fuego.** «K de roca sin humo al otro lado» es una pared de piedra.
- **Cuerpos.** Cohesión 3 ya está en `Universe.cs:1012`. La «sombra del pilar» es −8 en una celda (`LabLuz`, barridos laterales), no sombra.

Queda **luz × sólido**: `LabLuzDesde` (línea 1269) devuelve 0 para todo sólido, y el vidrio sería el único que transmite. Cruce genuino con una sola forma de decisión, *separador transparente*; las cinco «decisiones nuevas» son instancias de ella.

## 2. Por qué 5 y no 9

`luz` tiene **un solo lector**: `LabPlanta` (689, 704, 870-873), y las plantas hoy no producen nada (INFORME_FINAL: sin fibra ni recogida hasta G5). El apalancamiento del vidrio es como máximo el de las plantas. Además el fuego **sí** emite (`EmiteLuz`: Fire/Brasa/Hogar → 255) y el hogar está en el catálogo (`LabPanel.cs:108`): una brasa sin humo a 8-10 celdas del lecho da ~175-190 de luz. Nadie midió esa lámpara; si funciona, el vidrio compite con una herramienta existente. Banco de un día, y va ANTES.

Donde sí compra juego es en El Pozo (01_PROPUESTAS.md:116, 199, donde ya figura como *opcional*): suelo de vidrio entre estratos = «conservo mi luz, tú te quedas tu humo». Decisión real de dos jugadores. Vale 5; 6-7 cuando las plantas produzcan.

## 3. Lo que la evidencia contradice

«Invierte R135/R148»: no. r148 midió que la luz es **una columna de 7 celdas** (`decayCielo` 1, lateral 8) y que el serpentín en y272 tapa la boca x118-124. Vidrio bajo un serpentín opaco sigue a oscuras; serpentín fuera de la boca no gotea sobre la columna iluminada: nada que desviar. r150: con boca ancha la luz llega (139-210) y mueren por **reparto del riego** (50-99, mínimo 60). El vidrio no toca ninguna causa medida; el INFORME las asigna a diseño de nivel, y ahí esconde el candidato su tuning: anchura de boca, posición del serpentín, riego que llegue al lecho.

## 4. La verificación no pasa

- El escenario «invernadero» (referencia + techo + fuego de la sala, **sin alambique**) no tiene agua: el lecho nace SECO (`SimLevelBuilder.Laboratorio.cs:140`); 0 plantas con y sin vidrio. Con alambique, luz 0 en ambos.
- «Solo cambia HashLuz» es falso con el candidato completo: mover VidrioVerde de `default` (KPolvo 3/CPolvo 2) a KRoca 2/CRoca 3 en `LabK`/`LabC` cambia la térmica del horno en cuanto nace la primera celda → `HashTemp`, y por combustión `HashMat`/`HashAux`. Línea base nueva, no regresión.
- `LabLuz` acota la ventana con `dMin` (1216-1224) sin incluir un decay de 6 < 8: hay que añadirlo.
- El «riesgo mayor» está sobrestimado: `Flask.EsAspirable` (Flask.cs:521) ya admite el vidrio y lo vierte con `PaintCell`. Pero si el gateo por `ModoLaboratorio` tocara `Flask.cs:528`, dejaría de moverse: gatear solo `ApprenticeController`, y para un techo ni hace falta.

## 5. Versión mínima con más apalancamiento

Tres líneas y un alfa: `LabLuzDesde` transmite VidrioVerde; `LabLuzDecay` case → `LuzDecayVidrio`; `dMin` lo incluye; `LabTinte` alfa ~130. **Nada más**: ni `LabRoca`, ni `EsRocaImpermeable`, ni `LabK/LabC` (no añaden decisión y mueven hashes), ni gateo del muñeco. Solo así la promesa «cambia únicamente HashLuz de horno con yesca» es cierta y sirve de aserto.

Banco sintético en lugar del invernadero de referencia: cámara con lumbrera de 7, lecho pre-mojado a 200 (elimina el confusor del riego), fibra sobre hogar debajo, tres variantes (suelo de vidrio / piedra / abierto). Asertos: luz en la cara ≥ 40 con vidrio y 0 con piedra; humo sobre el vidrio = 0 y > 0 abierto; `LabPlantasNacidas` > 0 con vidrio. Un día de Opus.

**Corregido:** apalancamiento 5 · tuning 9 (un número que cualquier valor 1-100 satisface; el tuning real del huerto es geometría de nivel, ajena al candidato) · 0,5 semanas la versión mínima; 1,5 si se exige demostrar el invernadero en el nivel de referencia.
