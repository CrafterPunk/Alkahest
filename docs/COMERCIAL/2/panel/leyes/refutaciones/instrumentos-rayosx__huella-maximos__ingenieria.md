# Refutación de ingeniería · `huella-maximos` (lente instrumentos-rayosx)

**Veredicto: VIABLE CON AJUSTE.** Apalancamiento corregido 7 (declaraba 9), tuning 8 (declaraba 9), coste 1,0 semana (declaraba 1,5, con un trozo que hay que sacar del paquete).

## Lo que el código confirma

- **Encaje real.** El patrón existe: `humedad/carga/reposo/luz` se añadieron en R130 en los tres sitios que el candidato nombra (`CellGrid.SetCell` los pone a 0, `SwapCells` los intercambia, `LabTransformar` en `SimStepper.Laboratorio.cs:254` los reescribe). Un byte más es copiar ese patrón.
- **El trazador funciona.** Toda materia se mueve por `SwapCells`: `Move` (SimStepper.cs:740) y `ProcessSolidoCohesion` (:1407); no hay copias directas de `mat[]`. La arena marcada viaja con su marca.
- **Presupuesto.** `LabCampos` (SimStepper.Laboratorio.cs:201) ya visita 27 648 celdas por tick, dormidas incluidas, y ya lee `mat[i]`. El prólogo añade dos lecturas, dos tablas de 256 bytes y un OR; vecinos solo cada 32 visitas. 0,1-0,2 ms sobre 1,6-1,9: cabe, y el banco ya reporta `MsCampos`.
- **Determinismo.** Sin dado y sin lectura por la física: los siete hashes NO PUEDEN moverse. `HashHuella` es una línea en `LabBench.Resultado` (:40, :330).
- **Máximo muestreado.** Se lee cada 8 ticks; una punta más corta se pierde. Fire pinea 255 muchos ticks, así que no importa, pero se documenta así.

## Los tres ajustes (obligatorios)

1. **No retirar la pátina en este paquete.** `ActualizarPatinaFranja` (SimRenderer.cs:957) corre en el juego normal para todo `StaticSolid`, sin gate de `LabActivo`, y el docblock de `CellGrid.patina` la define como reconstruida por cada cliente "de lo que VE", cero bytes de red. La huella solo se escribe con `LabActivo` y `SimSync` replica únicamente `mat[]` (Net/SimSync.cs:30-56). Retirarla hoy deja el juego heredado sin memoria de incendios y al espejo de la ruta A a ciegas. Orden: huella primero; pátina fuera cuando el laboratorio sea el juego y la huella tenga transporte (el RLE admite un segundo bloque, anotado en :56).
2. **Semántica de `humedad` por material.** El candidato aplica una escala universal a "toda celda que no sea aire, gas ni fuego", pero `humedad` es VOLUMEN en `Water` (todo el agua marcaría banda 7 al nacer), SAVIA en `Planta` y ROCÍO en roca. Además el agua nace y muere por `LabNacerAgua`/`SetCell`/`LabTransformar→Empty` sin parar: su huella no sobreviviría. Mínimo: huella solo en sólidos (`EsPoroso || EsRocaImpermeable || NucleoFrio`). El "por qué murió" del huerto se lee en el sustrato, no en la planta, así que no se pierde.
3. **Copia hacia adelante solo a sólido.** `LabTransformar` conserva la huella si el nuevo material es sólido (arcilla→terracota, arena→vidrio) y la pone a 0 si es `Empty`/gas; si no, bytes huérfanos viajan por el aire en cada `SwapCells` y meten ruido en `HashHuella`. `Transform` (SimStepper.cs:707) pasa por `SetCell` y olvida: fibra→ceniza nace limpia.

Menor: el hollín "cada 32 visitas" no tiene contador por celda; va como fase global `((_tick >> 3) & 31) == 0`.

## Por qué bajan los números

**Apalancamiento 7.** La huella REVELA y JUZGA, no EJECUTA: la física nunca la lee. Es la mejor herramienta forense propuesta y el veredicto natural de SOLTAR, pero no transforma ninguna decisión física existente; la única decisión nueva con materia es el trazador. Y "leer el fichero de otro jugador" depende de un guardado de grilla que no existe (`LabPresets.GuardarSnapshot` no la escribe).

**Tuning 8.** Las bandas son umbrales, cierto. Pero el precedente está en el código: el "mojado" de la pátina lo apagó Cesar porque prometía una filtración que no ocurría (SimRenderer.cs:1008-1019). La huella no miente porque se escribe desde `humedad` real, pero la banda de rocío en roca se leerá como "aquí pasó agua", y hay que comprobar en una sesión (R52) que un tinte de un píxel se lee. Ese bucle humano es pequeño e incomprimible.

**Coste 1,0 semana.** 0,5 sim y banco; 0,5 tinte, vista, lector y un playtest. La pátina sale del paquete.

## Benchmark que lo prueba

En `LabBench`: (a) siete hashes de nueve escenarios idénticos al bit; (b) `HashHuella` estable entre dos corridas; (c) "horno con yesca": pared interior con banda de calor ≥ 6, exterior 0; (d) "alambique de r141": celdas del lecho con banda de humedad ≥ 3 ≥ `aptas` de `MedirLecho`; (e) "tolva sobre fogón" (466 s ≈ 55 muestras): bóveda con hollín ≥ 1; (f) `MsCampos` sube menos de 0,2 ms en el peor escenario.
