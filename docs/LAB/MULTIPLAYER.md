# MULTIPLAYER — implicaciones de la dirección "laboratorio de leyes" (análisis de Fable, 2026-09-03)

*(Sección D del informe final. Datos de red del motor: `docs/LAB/mapa/piel_net.md`. Hoy solo
`mat[]` viaja (RLE por chunk, 5 Hz global / 15 Hz a ≤60 celdas de un avatar, presupuesto 96
chunks y 16 KB por difusión, ~155 KB/s por cliente en el peor caso realista). El invitado es
un ESPEJO sin stepper; temperatura, aux, morph y los cuatro campos nuevos NO se replican; el
invitado no talla ni calienta.)*

## 1. Por sistema

| Sistema | Qué tendría que sincronizarse | ¿Determinista? | ¿Derivable local? | Ancho de banda | Divergencia | Autoridad | Compresión / interés |
|---|---|---|---|---|---|---|---|
| Materia (`mat`) | ya viaja | sí (host único) | — | base actual | ninguna (espejo) | host | RLE por chunk, prioridad por avatar (ya) |
| Temperatura | `temp` por celda si el invitado debe VER incandescencia, condensación, hervor | sí | NO (difusión sobre toda la grilla, fuentes del host) | ×2 del peor caso de chunk (segundo bloque RLE, cuantizado a 4 bits: 8 tonos bastan para ver) | ninguna si es espejo | host | solo chunks con Δtemp > umbral; solo a ≤60 celdas; cada 4.º tick |
| Humedad (aire/porosos/rocío) | `humedad` para el TINTE (mojado, turbio, celda a medio evaporar) | sí | PARCIAL: el mojado de un poroso se puede derivar de "tocó agua hace poco" (pátina, ya existe, cero bytes) | como temp; cuantizada a 3-4 bits | visual solamente | host | derivar: pátina; sincronizar solo si se quiere ver el vapor del aire (vistas de depuración: no hace falta en co-op) |
| Carga (turbidez) | `carga` de las celdas de agua para el color | sí | NO exacto; sí aproximado: "agua que nació del manantial o de erosión reciente = turbia" | 2-3 bits por celda de agua, solo chunks con agua | visual | host | empaquetar con `mat` como id de material virtual: `AguaTurbia` = mat alterno solo en el espejo (1 byte, RLE gratis) — **recomendado** |
| Reposo | nada (interno) | sí | sí (no se ve) | 0 | — | — | — |
| Luz | `luz` solo si hay render de luz en el cliente | sí | SÍ: el espejo puede recalcular LabLuz sobre su `mat` replicado (misma función, mismo resultado hasta ±1 tick) | 0 | ninguna práctica | local | — |
| Presión (LabPresion) | nada aparte de `mat` (mueve materia) | sí | — | dentro del delta de `mat` (las mudanzas despiertan chunks lejanos: más chunks sucios) | ninguna | host | ya cubierto |
| Plantas | `mat` (Planta/Fibra) + savia solo para el amarilleo | sí | savia derivable de "hace cuánto no llueve" no; aceptar planta verde siempre en el espejo | 0 extra | visual menor | host | — |
| Cuerpos cohesionados | `mat` (el bloque baja como celdas) | sí | — | pico de chunks sucios al caer | ninguna | host | prioridad por avatar ya lo cubre |
| Parámetros del panel | los 84 `LabParams` (una vez al cambiar) | sí | — | < 1 KB/cambio | ALTA si dos clientes los editan | host; RPC "proponer valor" → host aplica y difunde | mensaje pequeño |
| Tiempo (multiplicador) | multiplicador y pausa | sí | — | 2 bytes | ALTA (el reloj del espejo debe seguir al host) | host; `TickEspejo` debe avanzar N por frame igual | trivial |
| Manantial/Sumidero/Hogar | son `mat` | sí | — | 0 | — | host | — |
| Herramientas del invitado (cincel, verter turbio, PaintLab) | RPC nuevo con humedad/carga (hoy el host descarta la temperatura del invitado) | — | — | 8-10 bytes por pintura | — | host valida | lote como hoy |

## 2. Costes de producción (estimación honesta)

- **Ruta A — solo-anfitrión + espejo enriquecido** (lo que el motor ya es): añadir un segundo
  bloque RLE opcional por chunk para `temp` cuantizada y un id virtual `AguaTurbia`; RPC de
  parámetros y tiempo; RPC de cincel/PaintLab del invitado con validación. **2-3 semanas** de
  trabajo de red + 1 semana de pruebas con dos máquinas. Riesgo: ancho de banda ×1,5-2 en los
  peores casos (mundo casi todo despierto por el arroyo + fuego + vapor); el presupuesto de 96
  chunks ya se toca hoy y el barrido circular se retrasa (lag visual lejos del avatar).
- **Ruta B — simulación determinista en todos (lockstep)**: el motor ES determinista (XorShift
  por celda, sin allocs, orden fijo), así que técnicamente cabe: sincronizar solo inputs
  (pinturas, cincel, parámetros) y el tick. Requeriría: reloj de lockstep con espera del más
  lento, checksum periódico de `mat` (y de los cuatro campos), resincronización por snapshot al
  divergir, y que TODA la lógica de máquinas del anfitrión (placas, crisol, fogatas del
  curador) pase a ser tick-determinista (hoy usan `Time.deltaTime`). **6-10 semanas** y una
  fuente permanente de bugs de divergencia (flotantes en `EmisionTermica`, `Math.Round`,
  `double`). Ganancia: cero ancho de banda de estado, invitados con temperatura y vistas.
- **Ruta C — postergar multiplayer** durante la fase de descubrimiento del juego (lo que hicieron
  Noita y muchos sistémicos): se sigue cuidando el determinismo (regla de oro 1) y se sigue
  gateando lo del laboratorio por `LabActivo`, pero no se replica nada nuevo hasta que el
  diseño se estabilice. Coste hoy: 0. Coste diferido: cuando se retome, la ruta A sigue siendo
  válida porque nada del laboratorio rompe el espejo (solo no se ve). Lo único que hay que
  vigilar desde ya: **no derivar decisiones de juego de estado no replicado en el cliente**
  (el invitado no debe necesitar leer `humedad` para actuar).

## 3. Recomendación

Ruta C ahora, con la ruta A como plan cerrado: cada sistema nuevo del laboratorio se diseña
para que su CONSECUENCIA visible viva en `mat` (depósito, goteo, planta, grava) y su estado
interno (humedad, carga, reposo, luz) sea derivable o prescindible en el espejo. Eso ya es
así en lo entregado salvo dos tintes (turbidez y mojado), que se resuelven con el id virtual y
la pátina. Lockstep (B) solo si el juego final exige que los invitados manipulen calor y
carven a la par del anfitrión: entonces vale la pena, no antes.

## 4. Riesgos de divergencia concretos (si alguien intenta B)

- `EmisionTermica.PasoFootprint` usa `double` y `Math.Round` (reproducible entre máquinas
  x64 con el mismo runtime, pero no garantizado entre plataformas).
- Las máquinas (`Flask`, `HeatPlate`, `Dispenser`, `Crisol`) tienen acumuladores de
  `Time.deltaTime`: su tick no coincide entre clientes.
- `GaleriaCurador.RefrescarFogatas` y cualquier `Time.unscaledTime` en la capa de juego.
- El orden de las pinturas del invitado dentro de un tick.
