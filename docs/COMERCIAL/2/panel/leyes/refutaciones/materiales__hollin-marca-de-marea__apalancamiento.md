# Refutación (apalancamiento): hollín y marca de marea

**Veredicto: viable con ajuste.** Apalancamiento 7 → 5, tuning 7 → 6, coste 1 → 1,5 semanas (sin contar el candidato 1, del que depende).

## 1. La mitad visible ya existe y cuesta cero

`SimRenderer.cs:957` (`ActualizarPatinaFranja`) ya tizna todo `StaticSolid` junto a `Fire`/`Brasa` (+14 por pasada) y bajo `Smoke` (+6), techo 220, casi permanente. Corre desde `RenderFrame` (`:837`) sin gate de laboratorio y se lee en `ComputeCellColor` (`:1614`). «Hollín en un techo lo entiende cualquiera» ya está en pantalla. Lo que el candidato añade son los LECTORES (luz, agua, rótulo), y esconde un paso: si `LabTinte` oscurece por `carga` sin apagar la pátina, la piedra se oscurece dos veces.

## 2. «Fuego × arquitectura» está invertido por el código

`ProcessCombustion` (`SimStepper.cs:879-885`): en sordina NO hay lengua y el humo sale a `(combustHumoPct+3)/4`. El fuego que «no respiraba» aporta 0·`HollinLlama` y un cuarto del humo; el que respira pone 6 por visita en cada muro que toca la llama (255 en ~340 ticks, 11 s) y humo entero. «Bóveda negra = ese fuego no respiraba» es falso aquí; sin tiro, «el humo no salió» es casi siempre cierto en cualquier recinto. No es decisión: es observación, y ambigua.

## 3. La marca de marea no produce la decisión que vende

La `carga` del agua evaporada va «al vecino de abajo si `EsFondo`»: el anillo queda en el SUELO de la poza, nunca en las paredes a la cota máxima, y el lavado va solo roca → agua. «Leer hasta dónde llegó el agua» no sale de las reglas. En porosos el suelo ya recibe finos por `LabInfiltrarHacia`. Vale como arreglo de conservación de tres líneas, no como decisión.

## 4. El ciclo humo → sedimento crea materia de la nada

`SpawnSmokeNear` (`SimStepper.cs:941`) hace `Transform(nidx, Smoke)`: el humo no lleva masa y el hollín «nace del contacto», sin cota. Hogar inmortal + charco pegado al muro: `HollinLavado` ~8 por visita de roca cada 8 ticks ⇒ ~1 u/tick ⇒ el charco llega a `DepositoUmbral` 200 en ~7 s y `LabAgua` lo vuelve `Sedimento`. Un hogar y un goteo fabrican sedimento sin fin. Viola R55 (ningún proceso por tick con ganancia neta) y la ética de `LabBalanceU`. El único cruce que «nadie escribió» es, tal cual, un exploit de materia.

## 5. Todo lo bueno cuelga del candidato 1

`LabLuzDesde` devuelve 0 para `VidrioVerde`, que tampoco tiene caso en `LabCampos`. Sin el 1 no hay lector de luz, ni invernadero que ensuciar, ni goteo que limpie. Y `LabLuzDecay(byte m, …)` es `static` sin índice: leer `carga` exige cambiar su firma.

## Cuenta de decisiones

Reales: (a) dónde desemboca la chimenea respecto al vidrio; (b) lavar con un goteo programado (bloque frío encima: reutiliza la regla del alambique) o aceptar la penumbra, con la planta de fotómetro. Dos, ambas con el candidato 1. (c) es observación invertida; (d) no la producen las reglas. Cruces que sobreviven: humo × luz × vidrio y agua × luz; el de sedimento, solo si el hollín se conserva.

## Ajuste mínimo con más apalancamiento

1. Después del candidato 1, no antes.
2. Hollín CONSERVADO: `Smoke.carga` nace en `SpawnSmokeNear` como fracción del combustible consumido (`RendimientoHollinPct`, junto a `RendimientoCarbonPct`); roca y vidrio reciben por contacto RESTANDO al humo; el humo que expira suelta su carga abajo. Fuente por `Fire`/`Brasa` a 0: la señal legible es el humo que se quedó, lo único que un tiro futuro cambiará. El ciclo sedimento queda acotado por el combustible.
3. Marca de marea: solo el arreglo de conservación (si abajo hay `Water`, a su `carga`).
4. `LabLuzDecay(m, i)` lee `carga` en vidrio; `LabTinte` oscurece por `carga` y apaga la pátina heredada bajo `LabTinteActivo`.
5. Banco: identidad Σhollín nacido = Σ en humo + Σ en sólidos + Σ lavado + Σ expirado (como `LabBalanceU`); luz bajo el vidrio monótona decreciente en Σcarga(vidrio); el charco junto al muro tiznado no deposita más que combustible quemado × porcentaje.

Tuning escondido: cuatro números, pero «cuánto de negro en cuánto tiempo» junto a un fuego inmortal se juzga a ojo. Acotado por combustible el gusto pesa menos; sin acotar es un dial de ruido visual del tipo que Cesar ya rechazó (pt44: la pátina de mojado prometía física que no había).
