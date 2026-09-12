# Refutación · organismos / polilla-lectora-de-luz · lente APALANCAMIENTO

**Veredicto: REFUTADO.** Un material que solo LEE un campo y no escribe en ninguna ley no es una ley: es un visor animado con trámite de id. El candidato ya lo admite (apalancamiento 4, decisiones «casi ninguna»); leído contra el código queda peor, porque sus únicos cruces reales son negativos y no declarados.

## 1. Cuenta de cruces: uno de lectura, cero de escritura, dos negativos ocultos

- **Luz**: lee `luz[]` (recalculado cada 16 ticks, `LabParams` 124). Único cruce declarado y unidireccional: ninguna línea de la simulación lee `mat == Polilla`. R48 en estado puro: eje de variación sin consumidor.
- **Humo**: no es cruce propio, es la luz otra vez (`LabLuzDecay` 1280).
- **Fuego**: «sobre Fire → Ash» sale gratis de `TryIgnite` (1763) y `ApplyPhase` (686) si es `flammable` con `combustReserva = 0`; pero el `Hogar` no es `Fire`, es `StaticSolid` (Universe.Laboratorio.cs 95-108): la «trampa de luz» exige un `ignitionTemp` bajo, y entonces la polilla arde en el halo de aire caliente a distancia, no «entra en la llama».
- **Plantas, el cruce que niega**: `LabSecarHacia` (736) y el destino de crecimiento (870-874) exigen `Empty`. Las celdas más brillantes de un huerto son exactamente las que rodean las puntas: la nube se posa ahí y la planta deja de crecer y de transpirar. Y `LabLuzDesde` (1272) devuelve 0 para cualquier material fuera de su lista: la nube en la fila `H-2` de la boca (1200) es una tapa sobre las 7 caras de 73 que ya recibía el lecho (R148). El sensor apaga lo que mide y mata lo que no quería tocar.

## 2. Las «decisiones nuevas» no existen

«Encender un hogar para limpiar una cámara»: sin comer, sin reproducirse, sin pesar, la polilla no cuesta nada y no hay nada que limpiar; el único daño es el del punto anterior, y su cura (fuego junto al huerto) produce humo que lo oscurece (R148): no es decisión, es trampa de balance. «Leer la luz por dónde se juntan»: la vista `luz` del panel ya enseña el campo entero; la polilla enseña solo máximos, y solo donde nació. Total: cero decisiones que las leyes ejecuten y juzguen.

## 3. Tres defectos que impone el motor y el presupuesto no ve

**El estriado de `LabCampos` (201-240) no es un integrador de movimiento.** Barre `i += 8` con `W = 768 ≡ 0 mod 8` y jamás ha llamado a `Move`; no consulta `touchedTick`. Un paso a `i+W` cae en el mismo residuo y MÁS ADELANTE en la misma pasada: bajo una boca de cielo (decaimiento 1 por celda, monótono hacia arriba) la polilla encadena pasos y aparece arriba en un solo tick. A la derecha va a 30 celdas/s (el residuo sigue al tick), a la izquierda a 30/7, hacia abajo a 3,75. La «velocidad», que el candidato dice que es toda la gracia, sería un artefacto del barrido. La planta no sufre esto porque la celda nueva nace con savia 0 (913) y no puede crecer en la misma pasada.

**No hay arquetipo.** Como `Planta`/`StaticSolid`, `ProcessPowder` (1078-1082) y `ProcessLiquid` la tratan de suelo: arena y agua se apilan sobre una nube que flota: un ladrillo antigravedad gratis en un falling-sand. Como `Gas`, `ProcessGas` (1533-1553) la sube una celda por tick hasta el techo y la ondula con su propio viento, contra un argmax cada 8 ticks; además `EsGasId` es por id (Steam/Smoke), así que tampoco sería transparente. Arquetipo propio = `ProcessIfNeeded` en `SimStepper.cs` (367-392), archivo vetado por el HANDOFF §2.2.

**Nacimiento.** «Del compost con `PolillaPorMil`» depende del candidato 3, sin implementar; sin él se pinta a mano desde `LabPanel.Pintable`: contenido de autor. Y cada polilla que deambula a oscuras (fuera del alcance de ~30 celdas de `LuzDecayAire` 8, `luz = 0` y tirada 661 perpetua) despierta su chunk para siempre con cada `Move`: cien polillas perdidas son cien chunks que no duermen.

## 4. Verificación y tuning

«El 90 % acaba en bocas o en hogar a 3 000 ticks» es tautología para un escalador de gradiente: prueba el argmax, no el juego. El tuning que esconde es el caro: cantidad, velocidad, vida en `aux`, tasa de nacimiento, todos «mirando». Coste real: trámite de id en 8 archivos (precedente Arenisca: LabPanel, LabMateriales, LabParams, SimLevelBuilder/SimRenderer/SimStepper/Universe `.Laboratorio` y `Universe.cs` con `Rellenar`), guarda de doble visita, transparencia en dos funciones de luz, regla de muerte, renderer, escenario: 2 semanas, no 1.

## 5. Ajuste mínimo

Lo que promete (ver la luz sin panel) ya está pagado o casi: la vista `luz` existe, y el **fototropismo del candidato 1** es el mismo argmax con consumidor real: la planta CRECE hacia allí. Si Cesar quiere motas que vuelen hacia la luz, que sean **presentación**: partículas en `SimRenderer.Laboratorio.cs` que suben el gradiente de `luz[]` y mueren en celdas `EmiteLuz`; sin id, sin `LabCampos`, sin hash, sin determinismo que cuidar, 2-3 días. No es una ley y no debe presentarse como tal.

**Valores corregidos:** apalancamiento **2**; tuning **4**; coste **2 semanas** (versión material tal como está propuesta).
