# Refutación de ingeniería · organismos / polilla-lectora-de-luz

**Veredicto: VIABLE CON AJUSTE.** El núcleo (un id, un argmax sobre `luz[]`, una sal) es acotado, determinista y hasheable. Pero tal como está escrito tiene cuatro defectos que el código desmiente, y ninguno se arregla mirando: se arreglan leyendo.

## 1. «Caso propio en LabCampos» teletransporta hacia arriba

`LabCampos` (SimStepper.Laboratorio.cs 201-240) barre `for (i = offset; i < n; i += 8)`. Como `W = 768 = 96·8`, `i + W ≡ i (mod 8)`: **toda celda movida a `i+W` se vuelve a visitar en la misma pasada**, y no cabe guarda de `touchedTick` (el barrido por tick ya la marcó). `LabPlanta` sobrevive porque la celda nueva nace con savia 0; una polilla que sube, sube otra vez. Hacia arriba recorre la columna en 8 ticks; hacia los lados, 3,75 celdas/s. Es un ascensor. El sitio correcto es el barrido por tick: arquetipo `Polilla = 8` con su `case` en `ProcessIfNeeded` (SimStepper.cs 366-410, junto a `Planta`), donde `touchedTick` impide el doble movimiento por diseño (Move 741). De paso la velocidad pasa a un parámetro `PolillaCadaTicks`, justo lo que el candidato dice que «se decide mirando».

## 2. Opaca, se estorba a sí misma y a media física

Sea `StaticSolid` o `Powder` (cae cada tick y se arrastra), para los helpers que enumeran «aire» es un ladrillo: `LabLuzDesde` (1272) devuelve 0 → la nube **tapa la boca de luz** que la atrajo, y su `luz` propia sale como sólido (`max vecino − 8`, 1280-1286): «ninguno supera la propia» no ocurre nunca donde hay luz; en el máximo oscila M↔N y la tirada 661 no se usa. `LabRespira` (1053-1061) no la cuenta como aire → polillas alrededor de una llama la ponen en sordina. `LabEsAireOGas` (263) frena el vapor; `EsFondo` deposita sedimento encima; `ProcessPowder` (1081) apoya arena sobre ella; `Flask.EsAspirable` no la rechaza. Cada línea es trivial; la lista no es la declarada.

## 3. El hogar no mata

`LabHogar` (917-945, R138): «calienta, pero NO chispea»; nunca produce `Fire`. Con «sobre Fire → Ash», las polillas se apiñan junto al hogar temblando para siempre, con sus chunks despiertos (`Move` → `WakeChunk`). «Encender un hogar para limpiar una cámara» es falso tal cual. La cura existe: `meltsAt = 130 raw, meltsInto = Ash` en el `MaterialDef`; `ApplyPhase` (640-660) corre cada tick y `LabCalentarHasta` lleva al vecino del hogar a 170. Muere por temperatura: hogar, llama y brasa matan, y `aux` queda libre para la vida (no vale `flammable`: `aux` es la reserva de combustión, 765).

## 4. Nacer del compost crea materia (R55)

«Nace con `PolillaPorMil`» y «muere a Fibra» sin pagar: una compostera a oscuras fabrica combustible de la nada, y R136 conservó la energía al bit. En v0 se pinta desde `LabPanel`; si nace, es `LabTransformar` de la celda de sedimento, nunca `SetCell` en aire.

## Coste real

El trámite de Arenisca (R131) tocó ocho archivos (`Universe.cs`: id, `Count`, `Rellenar`; `Universe.Laboratorio`; `LabParams`; `LabMateriales`; `LabK/LabC`; `SimRenderer.Laboratorio`; `LabPanel`). Súmese el arquetipo, cinco exclusiones de «aire» y dos escenarios. Coste por tick: no «nulo» sino un suelo permanente de chunks despiertos en cada claro y hogar (la polilla no descansa); ≤0,1 ms con decenas, pero se mide. Determinismo intacto (argmax con orden fijo; 661 no colisiona con ninguna sal). 1,5 semanas.

## Versión mínima y banco

`LabPolilla` desde `ProcessIfNeeded`, gateado por `LabActivo` y `(_tick + 7x + 13y) % PolillaCadaTicks == 0`; argmax de `luz[]` sobre vecinos `Empty` en orden fijo, si ninguno supera `luz[idx]` paso al azar (sal 661); transparente en `LabLuzDesde`, `LabLuzDecay`, `LabRespira`, `LabEsAireOGas`, `EsFondo`; `meltsAt 130 → Ash`; `aux` vida decreciente → `Fibra`. Escenarios: **«gradiente»** (100 polillas en 60 columnas, una boca: fracción a ≤3 celdas de la boca por tramo de 500 ticks, creciente, ≥90 % a 3 000); **«hogar sin leña»** (50 polillas, 100 % Ash a 1 500 ticks); **«humo»** (humo sobre la boca: fracción en la boca < 50 % del caso limpio); `ActiveChunks` ≤ +6 frente al montaje sin polillas; los 9 hashes existentes intactos.

## Apalancamiento

Compra observabilidad diegética de un campo que hoy solo enseña F8, cruzada con humo y calor. No toca agua, suelo ni plantas, y compite con el visor de luz, que ya tiene el array. Apalancamiento 4. Tuning 6: dos números y diez minutos de mirar, no semanas de balance.
