# Refutación de ingeniería · `clima-ganado` (lente agua-fases)

**Veredicto: viable con ajuste.** La idea (que `ambient[]` lo escriba la historia) es exactamente lo que reserva `CellGrid.cs:110-115` y cabe en una pasada barata y determinista. La REGLA concreta, no: dos de sus tres garantías (olvido y hash intacto) se refutan con aritmética contra el código real.

## 1. La premisa es falsa para el mapa que corre el banco

El candidato afirma que `ambient` es uniforme a 70 y que el borde "se queda en `AmbientRaw`". `SimLevelBuilder.Laboratorio.cs:166-176` pinta 64 raw en la cámara alta y la boca del cielo (x 90-200, y 240-287, fila del borde incluida) y 66 en la cámara profunda, con rampa de 6 celdas, y a propósito: sin zona fría el vapor no condensa hasta saturar la cueva (el alambique de R141 depende de ello). Forzar el borde a 70 crea un escalón 64|70 en la fila 286: `(64·3+70)>>2 = 65`; en la primera visita las seis filas altas de la cámara suben hasta +5 raw. Los hashes de "laboratorio base" y "alambique de r141" cambian sin encender nada: la verificación "sin fuentes, hash sin cambio" contradice el código.

## 2. El olvido por difusión de signo no existe

`ambient += sign(media4 − ambient)` con media entera tiene como puntos fijos las mesetas y las rampas de pendiente 1: la celda 71 entre 72 y 70 (y 71 a los lados) suma 284, `>>2 = 71`, paso 0. Una sala calentada deja una meseta a 90 con rampa de 20 celdas y ahí se queda; el "sumidero" del borde está a 60 celdas de la sala del hogar y no llega nunca. Tras apagar, el tirón (`SimStepper.Laboratorio.cs:1337-1341`) lleva `temp` hasta `ambient`, luego `sign(temp − ambient)` vale 0: la memoria no baja sola. Queda una carrera de redondeos entre tres procesos de ±1 (conducción con fallback cada 8 ticks, tirón cada 32, clima cada 256): según la fase, la pared decae 1 raw por visita (olvida en 3 minutos) o 0 (recuerda para siempre). Determinista, sí; especificable, no: la constante de tiempo central no es un parámetro. Es el trinquete de un solo sentido de `SimStepper.cs:2159-2180` (playtests 1 y 9), subido al nivel del ambiente.

## 3. Energía sin tope y choque con el fuego validado

Sin techo, la pared junto a un hogar (170 raw) aprende ~150 y el tirón pasa de sumidero a fuente: `LabRawAmbiente` crece sin cota y un hogar "curado" puede rozar los 200 de `VidrioRaw`, la línea hogar 0 / horno 18-18 que Cesar validó. "Vidrio más barato" no es un premio: es reescribir esa frontera.

## 4. Ajuste mínimo

- **Sin difusión ni borde.** Dos contracciones explícitas de la familia de la regla 9. MEMORIA, cada `ClimaMemoriaTicks` (256), para la clase roca de `LabK` (Stone, PisoEstructural, Terracota, Arenisca): `ambient += sign(temp − ambient)` solo si `|temp − ambient| >= ClimaUmbral` (4 raw; mata la carrera). OLVIDO, cada `ClimaOlvidoTicks` (2048): `ambient += sign(ambientBase − ambient)`, con `ambientBase` = copia del plano pintado (nuevo `byte[]` en `CellGrid`, llenado al final de `BuildLaboratorioDeLeyes`). Las zonas pintadas quedan intactas por construcción; olvidar 30 raw cuesta 61 440 ticks, un número y no una fase.
- **Techo y suelo:** `ClimaMax` 100, `ClimaMin` 40. La memoria mueve la banda templada/fría (secado, evaporación, hielo que dura), nunca yesca (130) ni carbón/vidrio (200).
- **Estriado:** `if ((_tick & 31u) != 0) return; offset = (_tick >> 5) & 7`; cada celda una vez por 256 ticks, toda la grilla sin mirar chunks (que un chunk dormido no olvide sería el bug del hielo del playtest 1). < 0,01 ms amortizado.
- **Visor:** `SimRenderer.Laboratorio.cs:176` debe pintar `temp − ambientBase`, o la sala curada desaparece de la vista Temperatura; vista Clima = `ambient − ambientBase`. Va con la ley.
- **Hash:** `HashAmbient` junto a `LabBench.cs:326-331`; gate `ClimaActivo` con hashes idénticos a 0.

**Benchmark:** escenario "sala curada" (40×20 sellada, hogar 18 000 ticks, se sustituye por Stone, 40 000 ticks más): `ambient` de pared = 100 al apagar; aire medio a 3000 ticks contra la sala gemela sin ley (medido, no prometido); a 61 440 ticks `ambient == ambientBase` en toda la grilla; vidrio = 0; "horno con yesca" sigue 18/18; ms/tick de "mundo entero despierto" dentro del +2 %. Fuera del alcance pero real: `ambient` no lo serializa nadie (`LabPresets`, `Net/SimSync`); "lo que el sitio recuerda al volver" exige meterlo en el fichero de SOLTAR.

**Corregidos.** Apalancamiento 6 (con techo transforma duraciones, no umbrales; la nevera necesita `fase-con-reserva`). Tuning 8 (un slider, `ClimaOlvidoTicks`; el resto lo fija el banco). Coste 1 semana: pasada y parámetros, escenario largo y rebase, visor doble, documentación R15/R32.
