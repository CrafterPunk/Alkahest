# Refutación de ingeniería · organismos / dispersion-semillas-arrastre

*(Segunda pasada, 2026-09-06, contra el código real y el benchmark H1 del circuito.)*

**Veredicto: VIABLE CON AJUSTE.** Determinismo intacto, coste despreciable, medible en el banco. Pero la especificación choca con el código en cinco puntos y el gancho ve solo una parte del agua que de verdad se mueve.

## Lo que el código confirma

- **Flotación estática.** `ProcessPowder` 1088-1094 hunde un polvo solo en un líquido menos denso; Semilla (90) y Fibra (60) sobre agua (110) se quedan. Son los únicos polvos bajo 110 (Carbón y Brasa son 110 justos): nombrarlos por id, no por densidad.
- **Doble proceso, cubierto.** El barrido sube (`Step` 280-300) y `ProcessIfNeeded` 352 salta `touchedTick == _tick`. Sin RNG nuevo; sal 647 libre.
- **Sin sexta costura.** Las llamadas de 1228/1236 se renombran a `LabAguaFluyo(idx, _cellFinal…)` y el partial hace erosión y arrastre: ~+1 %. El hash de `MontarDiluvio` (agua y roca) DEBE quedar idéntico.

## Cinco errores contra el código

1. **`Move` no vale.** Escribe `_cellFinal*` (738-752) y `ProcessIfNeeded` 372 llama `MaybeReact(_cellFinal…, moved=true)`: las reacciones del agua se evaluarían en la celda de la semilla. `SwapCells` + `touchedTick` + `reposo=0` + `WakeChunk×2` a mano, patrón de `ProcessSolidoCohesion` 1404-1414.
2. **`LabTragar` no come semillas.** 1026-1033: `if (archetype != Liquid) return`. Se posan sobre la boca y la tapan. Dos líneas, dos contadores, `LabAguaSumidaU += humedad`.
3. **Balance.** `LabTransformar` 254-262 suma `humedad − hum[idx]`: nacer con 100 sobre vacío es +100 sin resta. Falta `LabBalanceU -= PlantaSemillaSavia/2` (patrón de 877). La que se pudre cede su agua a `humedad[abajo]` (patrón ceniza 670-678).
4. **`Estado()` no puede explicar.** La firma (190) recibe `m, humedad, carga`; 688-689 leen `luz[i]`, `mat[abajo]`, `humedad[abajo]`. Sobrecarga con índice para `LabPanel` 675. `Permeabilidad(Semilla)` = 0: hoy el rótulo devuelve `null`, y el clamp `t=1` de `LabPoroso` 566 hace que la semilla gotee 1 u/visita al sustrato. Aceptable, declarado.
5. **«Madura» es «bloqueada».** El crecimiento (858-882) va antes y gasta 120 cuando hay hueco con luz; savia ≥ 200 solo queda en puntas sin hueco iluminado o a `PlantaAltoMax`. Buena definición emergente, pero hay que declararla; R148/R150 midieron huertos que apenas viven: pocas semillas.

## El transporte que el gancho no ve

En el circuito de referencia (H1) el caudal neto es 0,68 celdas/tick y `LabPresion` hizo 11 084 mudanzas en 18 000 ticks = 0,62/tick: casi todo el transporte de largo alcance es teleporte no local (1136-1160), invisible a `TryFlow`. Una celda con semilla encima no es superficie (`mat[c+W] == Empty`, 1107 y 1124): esa columna es inerte para la presión, y una balsa que cubra un cuerpo entero deja `nSup < 2` y la APAGA (tubo en U con balsa = tubo muerto). El flujo lateral que sí ve el gancho, junto a una fuente de mudanza, va AGUAS ARRIBA. El arrastre funciona en tramos someros donde la superficie fluye por `TryFlow`, no en pozas ni tramos profundos. Coherente con «remanso = almacén», pero se mide, no se supone.

## La ley que falta: flotabilidad

Flotar hoy es «no hundirse», no «subir». Una superficie que fluye está llena de huecos donde `ProcessPowder` 1100-1112 desliza la semilla en diagonal; ahí es dique de una celda y el agua la cubre; el goteo del alambique pone agua encima. Sin ascenso, la cinta se vuelve barra sumergida. Remedio en el partial: `LabCampos`, caso Semilla/Fibra, `if (mat[i+W] == Water) { SwapCells(i, i+W); reposo[i+W] = 0; WakeChunk; return; }` (3,75 celdas/s; coste solo en celdas flotantes). Regalo: la fibra se sumerge a ratos y `LabInfiltrarHacia` 443 la empapa; la cosecha llega mojada y se seca junto al hogar.

## Versión mínima viable

Gancho renombrado + arrastre por id + flotabilidad + semilla que nace y se pudre con balance + `LabTragar` ampliado + `Estado` sobrecargado. ~150 líneas en partials, cinco parámetros (no tres), un escenario, re-línea-base declarada de los hashes con plantas o fibra sobre agua.

## Banco que lo prueba

1. «diluvio turbio»: hash idéntico; ms ≤ +3 %. 2. «flotabilidad»: 20 semillas bajo 6 celdas de agua quieta → 100 % en superficie en ≤ 400 ticks. 3. «arroyo sembrado» (nivel de referencia, 60+60 en cabecera, 18 000 ticks): ≥ 50 % avanzan ≥ 40 celdas o son tragadas; ≥ 1 germinación en orilla; `LabBalanceU` cuadra al bit; arrastres frente a `LabPresionMovidas` por tramo. 4. Arco largo: población final distinta de 0, sin ganancia neta de agua (R55).

**Apalancamiento 7** (agua que transporta y planta que se reproduce sola, pero medio pago depende de luz en orillas medida escasa y de tramos someros). **Tuning 7.** **Coste 1 semana**: 3-4 días de código; el resto, escenario, medida presión-vs-arrastre y re-línea-base.
