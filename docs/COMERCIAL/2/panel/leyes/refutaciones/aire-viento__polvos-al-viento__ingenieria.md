# Refutación de ingeniería · `polvos-al-viento` (lente aire-viento, candidato 3)

**Veredicto: VIABLE CON AJUSTE.** El enchufe existe y es barato; la regla tal como está escrita no produce el juego que promete.

## 1. Lo que el código confirma

- **Encaje y coste.** `ProcessPowder` (`SimStepper.cs` l. 1061-1084) tiene una rama única «abajo es Empty/Gas → `Move` recto». Una lectura de `viento[idx]`, una comparación y una tirada `XorShift.FromCell(_tick, x, y, SalNueva)` (patrón de la sal 1, l. 1086) cuestan lo declarado (sales libres tras 643).
- **Determinismo.** El barrido alterna sentido por tick (l. 273) y `Move` marca `touchedTick[idx2]` (l. 741): la diagonal hacia una celda no visitada no se procesa dos veces; ambos chunks despiertan (l. 743-744).
- **La brasa ya enciende al aterrizar**, pero en l. 1037-1045: `BrasaReencenderPct = 8` por paso de 4 ticks → `TryIgnite` (l. 1763), que exige `hotEnough` o 12 %/tick. Con 240-360 ticks de vida (l. 991), «3 de 5 semillas» es un criterio honesto.

## 2. Lo que refuta la regla escrita

**a) «Solo mientras cae» mata el titular.** La brasa nace *in situ* (`ConvertirEnBrasa`, l. 987-993). En carbonera (`LabBench.cs` l. 135-143), tolva (l. 146-155) y en la «chimenea con boca N», las brasas nacen dentro de la pila, a ras del hogar, en un cuarto de piedra: solo pueden **caer**. Un tejado «a 6 celdas a sotavento» está fuera y más alto: ninguna brasa llega. El escenario de verificación no puede pasar por construcción. Igual «la ceniza abona a sotavento»: sin levantarse no sale del hogar, y `VientoLevanta` viene apagado. Tres de cuatro cruces dependen de un ascenso que la regla prohíbe.

**b) La celda de convección va hacia dentro.** El viento del candidato 1 nace de flotación + boca del cielo; el viento exterior quedó descartado. Una llama crea entrada horizontal **hacia** el fuego a ras de suelo y salida alta hacia fuera. Un polvo que solo se desvía cayendo deriva hacia el fuego: «el incendio avanza a favor del viento» y «cortafuegos a sotavento» no tienen sotavento donde definirse; el flujo saliente es el alto, y ahí no cae nada si nada sube.

**c) La cota por densidad barre lo que no debe.** `VientoArrastre·density/64` anclado en ceniza (120) arrastra Carbón (110, `Universe.Laboratorio.cs` l. 4301): el carbón volaría de su propia carbonera. En el taller, los Calcinado (0,7·`DensidadPolvo`, `Universe.cs` l. 3332) y los polvos base con jitter por seed (l. 2562) volarían **según la semilla** (R34, R47/50).

**d) La prueba de hash está mal elegida.** «Tolva» tiene Hogar (l. 152) → fuego → `viento ≠ 0` tras el candidato 1 → brasas y ceniza cayendo con viento → el hash **cambia**. Solo «diluvio turbio» (sin fuego) prueba la inactividad con viento cero.

## 3. Versión mínima viable

1. **Campo por material**, no densidad: `MaterialDef.arrastreViento` (byte, 0 = no vuela). Solo Ash, Brasa, Semilla, Fibra.
2. **Deriva 2D con ascenso para lo mortal.** Leer `(vx, vy)`; si el eje dominante supera el umbral del material y el destino es Empty/Gas, mover una celda con probabilidad `|v|·12 %`. **Subir** solo Brasa (y Ash tras `VientoLevanta`), con Empty encima y `vy ≥ VientoEleva` (núcleo de un tiro, no brisas). La brasa es mortal por `aux` (l. 991): el bucle subir-caer se acota solo (R55). A 84 %/tick sube 40 celdas en ~60 ticks, sale, cae con `vy ≈ 0` y deriva hacia la boca del cielo: ese es el sotavento real.
3. **Ash lift apagado por defecto**; si se activa, medir `ActiveChunks` en «tolva» a 14 000 ticks.

## 4. Benchmark

- «chimenea con boca 3» + tejado de fibra A a 6 celdas de la salida, del lado de la boca del cielo; tejado B del lado opuesto: A prende antes de 9 000 ticks en ≥ 3/5 semillas; B en 0/5.
- «polvos en calma» (nuevo): ceniza, semilla y fibra soltadas desde altura 40 sin fuego: hash idéntico al candidato 1. «Diluvio turbio» igual.
- «mundo entero despierto»: `LastStepMs` ≤ +2 %. Rebase declarado de los escenarios con fuego.

## 5. Números corregidos

Apalancamiento **6** (sin ascenso, 3: el viento solo existe junto al fuego y empuja hacia él). Tuning **6** (umbral por material, `VientoEleva`, escala; «¿parece un castillo de fuegos artificiales?» lo decide un ojo). Coste **1,0 semana** tras el candidato 1: campo nuevo, ascenso, dos escenarios, rebase y calibración contra un `viento` que aún no existe.
