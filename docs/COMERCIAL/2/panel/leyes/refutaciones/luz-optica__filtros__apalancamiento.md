# Refutación · `filtros` (luz-optica §4) · lente: apalancamiento

**Veredicto: REFUTADO** como ley. Sobrevive un fragmento de dos líneas que no es esta ley sino
una pieza de `haz-y-espejo` (§5).

Leído: `SimStepper.Laboratorio.cs` (LabAgua :406-493, LabErosion :173-195, germinación :684-712,
LabPlanta :800-905, LabLuz :1177-1284), `LabMateriales.cs` (:98, :110), `LabParams.cs` (:60-69,
:124-136), `LabBench.cs` (:158-168, :279), `SimStepper.cs` (:358, :650),
`SimRenderer.Laboratorio.cs:202`, `LabPanel.cs:660`.

## 1. Consumidores de la luz bajo el agua: cero

`luz` lo leen cinco líneas de simulación y dos de visor: semilla (:689), germinación espontánea
(:704, exige `mat[i+W] == Empty`), punta que crece (:870-873, solo hacia `Empty`), y F8. Todas
las de simulación piden aire. El fondo de una poza puede valer 255 o 0 y la física no cambia un
bit. La propuesta lo admite («solo multiplica») y aun así pide 5 de apalancamiento: capa
decorativa por definición. Decisiones nuevas: **0 sin el candidato 1**; con él, una, invertida (§3).

## 2. La física prometida no es la del motor

*«El fondo se aclara conforme decanta.»* En el barrido descendente (:1231-1238) la luz al fondo
de una columna es `255 − Σ(dAgua + carga_k·K/255)`. La decantación (:459-464) mueve `carga` a la
celda de abajo **de la misma columna**: conserva la suma, así que la luz al fondo es invariante
mientras decanta; la turbiedad solo se concentra sobre el fondo, justo donde se quiere medir. El
único sumidero es el depósito (:466-481, `c ≥ 200`, `reposo ≥ 24`), que vuelve la celda
`Sedimento`: la luz sube porque el suelo sube. La métrica propuesta mide `LabDepositado`, que existe.

*La verificación no puede fallar ni pasar.* `LabBench.Correr` apaga la boca del cielo en todos los
escenarios (:279; solo `SimLevelBuilder.Laboratorio.cs:164` la enciende). `MontarDiluvio`
(:158-168) no tiene fuego, brasa ni hogar: `LabLuz` retorna en :1204 con todo a 0. `LuzFondoDiluvio`
vale 0 en los tres ticks con y sin la ley. Con boca, igual: 140 celdas de profundidad contra 12 de alcance.

*«El hielo aclara.»* El agua sí se congela en el laboratorio (`ApplyPhase`, `SimStepper.cs:358→650`),
pero `LabLuzDesde` (:1269) no transmite `Ice`: la cláusula no se ejecuta sin el candidato 1. Con
él, la luz cruza hielo y agua y muere en un fondo sin consumidor.

## 3. Las tres «decisiones nuevas»

- *Reposar el agua antes de inundar el lucernario.* `EsFondo(VidrioVerde)` es verdadero
  (`LabMateriales.cs:98`): el agua turbia quieta sobre vidrio decanta hacia él y a 200 con 24
  visitas deposita sedimento **opaco** encima. Reposar ciega el lucernario; no poner agua es
  estrictamente mejor (el agua solo resta). La ley invierte la decisión que promete.
- *Raíces para conservar la claridad.* Ya frenan la erosión (:187) por el sustrato (R135). Nada
  lee la claridad: mismo verbo, cero decisión.
- *Congelar la poza para dejar pasar la luz.* ¿Hacia qué? Al fondo. Nadie vive ahí.

Los «cruces» igual: no hay techo de agua sobre aire en un falling-sand (el agua reposa sobre
sólidos, y los sólidos no transmiten).

## 4. Tuning escondido que hereda del candidato 1

`LuzDecayVidrio = 4 < dAire = 8`: el vidrio conduce luz lateral **mejor que el aire** (la
«varilla = guía de luz» es un artefacto del número), y `dMin` (:1219-1223) baja de 8 a 4, con lo
que la ventana H5 pasa de 32 a 64 columnas por fuente en el nivel real. Todo decaimiento nuevo
debe ser ≥ `dAire` en lateral. `LuzTurbidez` no tiene nada que afinar porque nadie lo lee: 9 de
tuning por la razón equivocada.

## 5. Lo que vale, y cuánto

Dos líneas: `Ice` y `VidrioVerde` transmiten en `LabLuzDesde` y decaen en `LabLuzDecay` (vertical
entre `dCielo` y `dAgua`; lateral ≥ `dAire`). Es la ventana y el invernadero del candidato 1 en
la pasada difusa: una cámara sellada con techo de vidrio bajo la boca recibe luz, retiene la
transpiración (`LabSecarHacia`) y germina (:704). Escenario `invernadero` con boca encendida en el
montaje (el banco la apaga: excepción como la del alambique); aceptación `LabPlantasNacidas > 0`
contra 0 con techo de roca. Sin turbidez. Prerrequisito de `haz-y-espejo`, no ley con nombre.

**Apalancamiento corregido: 1** (el fragmento de §5 vale 4 como pieza del candidato 1).
**Tuning: 9.** **Coste real: 0,4 semanas** para lo propuesto (escenario y métrica que «existen»
hay que montarlos); 0,5 para §5 con su escenario.
