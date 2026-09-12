# INSTRUMENTOS Y RAYOS X · cuatro modificaciones del sustrato para aprender a ver

*(Panel de leyes, segunda pasada comercial, 2026-09-05. Lente: visores diegéticos, resolución perceptiva distinta de la física, un campo a la vez, instrumentos de materia, el cuaderno que dibuja, huellas. Leído antes de proponer: `CellGrid.cs`, `LabParams.cs`, `LabMateriales.cs`, `SimStepper.Laboratorio.cs`, `SimRenderer.Laboratorio.cs`, `SimRenderer.cs` (pátina), `LabBench.cs`, `Game/LabPanel.cs`, `Game/Termometro.cs`, `Game/Flask.cs`.)*

## 0. El principio, y lo que el código ya da

Un visor de campo bien hecho es la mitad de una ley. La regla de las cuatro propuestas: **la percepción se cuantiza a los umbrales del propio motor.** Fibra prende a 130 raw, arcilla cuece a 150, hogar fija 170, carbón y vidrio piden 200; semilla germina con humedad ≥ 60 y luz ≥ 40; fibra mojada > 100 no prende; ceniza ≥ 128 abona; sedimento compacta entre 100 y 230 con reposo ≥ 200; arcilla ablanda a 250; el aire condensa por encima de `Saturacion(temp)`. Si las bandas del visor SON esos números, leídos de `LabParams` en vivo como ya hace `LabMateriales.Estado` tras la lección R145, el visor no necesita balance: cuando Cesar mueve un slider, la banda se mueve con él. La resolución perceptiva queda fijada por diseño: 256 valores en la física, 8 bandas en la percepción.

Lo que ya existe y se reutiliza: cuatro campos más `reposo`, `LabCampos` a cadencia de visita (8 ticks), `VistaLaboratorio` como cuarta textura, el lector de celda (`LabPanel.DibujarLectura`), `Estado` en palabras, `Termometro.cs` con tres sondas plantadas, `Flask` con temperatura media, y `LabBench.Resultado` con siete hashes por escenario más medidas ad hoc (`MedirLecho`, `ArcoMuestra`). Una observación a verificar: en `LabLuz` el reset pone `luz = 255` en toda celda con `EmiteLuz` (Fire, Brasa, Hogar): el fuego sí ilumina el campo; lo que no ilumina es la pantalla, porque nadie pinta `luz` fuera de F8.

Las cuatro se reparten tiempo y espacio: **huella** es memoria espacial de máximos, **luz vivida** es el límite de lo que se ve, **rayos X por bandas** es el presente en un radio, **sondas registradoras** es memoria temporal. Comparten una tabla de bandas (`LabBandas`, C# puro en `Sim/`, compilable en el banco).

---

## 1. HUELLA · el registro de máximos que el mundo escribe sobre sí mismo

**Resumen.** Un byte por celda, escrito por la simulación, que retiene el máximo histórico de temperatura y de humedad en bandas de ley, y el contacto con humo. La roca es su propio termómetro de máxima; la arena, su propia marca de marea; la bóveda, su propio registro de hollín.

**Regla.** Campo `huella` en `CellGrid` (221 KB, junto a `humedad/carga/reposo`). Tres lecturas en un byte: bits 7-5 banda máxima de `temp` (bajo ambiente, templado, 110 hierve, 130 prende fibra, 150 cuece arcilla, 170 hogar, 200 carbón/vidrio, 255), bits 4-2 banda máxima de `humedad` (0, < 20, < 60, 60 germina, 100 moja fibra, 128 abona, 230 encharca, 250 ablanda), bits 1-0 hollín (0-3). Por visita, en el prólogo del `switch` de `LabCampos` para toda celda que no sea aire, gas ni fuego: máximo entre la banda guardada y la actual, y si algún vecino ortogonal es `Smoke`, hollín++ una vez cada 32 visitas (sin dado). Monótona: nunca decae. Viaja en `SwapCells` (la arena que pasó por el fuego y se lleva la erosión es un trazador); `LabTransformar` la copia hacia adelante (la terracota recuerda su arcilla); el pincel la deja a 0. **La física nunca la lee**, así que los siete hashes existentes no pueden cambiar: la verificación es «7 hashes idénticos, octavo nuevo».

**Cruces.** Pátina del render (`ActualizarPatinaFranja`): se retira; la huella es la pátina verdadera y determinista, y el «mojado» que Cesar apagó en el playtest 44 porque mentía vuelve porque ahora hay infiltración real. Horno: las paredes certifican el máximo alcanzado aunque la arena no vidrie («llegaste a la banda 5, te faltó la ceniza»). Alambique: la banda de rocío en el bloque frío dice dónde llovió de verdad. Huerto: el sustrato bajo una planta muerta dice por qué murió (nunca llegó a la banda 3 = seco; llegó a la 6 = ahogado), sin una línea nueva en `LabPlanta`.

**Decisiones nuevas.** Leer una ruina o el fichero de otro jugador por sus cicatrices antes de tocar nada; diseñar la poza por la marca de marea de la anterior; marcar arena con calor y verterla para ver el camino del agua; en SOLTAR, la huella es el veredicto que se lee al volver.

**Observabilidad.** Sin visor: tinte en `LabTinte` (roca enrojecida o blanqueada por su banda de calor, línea de marea en la arena, hollín en la bóveda). Con visor: vista «Huella» con las tres componentes, y el lector: «llegó a 200-229 raw · estuvo empapada · hollín 3».

**Coste.** 1,5 semanas de Opus: campo, prólogo, swap, tinte, vista, lector, retirada de la pátina, hash y dos aserciones. **Determinismo:** dentro de la sim y del hash, sin retroalimentación. **Coste por tick:** ~0,1 ms (dos comparaciones y un conteo de vecinos por celda visitada). En ruta A no viaja en `SimSync`; es monótona, así que se envía por chunk cuando cambia.

**Verificación headless.** `HashHuella` en `Resultado`. «Horno con yesca»: celdas de pared interior con banda de calor ≥ 6 > 0 y exterior = 0. «Alambique de r141»: celdas del lecho con banda de humedad ≥ 3 debe ser ≥ `aptas` de `MedirLecho` (alguna vez apta ≥ apta ahora). Invariante: los siete hashes previos intactos.

**Tuning humano:** 9 (las bandas son umbrales; solo se eligen el ritmo del hollín y la paleta). **Apalancamiento:** 9. **Riesgo mayor:** legibilidad del tinte en celdas de un píxel a la escala actual; si no se lee sin visor, la huella solo juzga a quien abre la vista.

---

## 2. LUZ VIVIDA · el campo `luz` como iluminación de pantalla, y la lámpara como emisor

**Resumen.** La pantalla se pinta con el campo `luz` que ya existe: donde `luz` es 0 no se ve. El jugador lleva una lámpara que es una fuente más en `LabLuz`. La luz deja de ser un número de F8 y pasa a ser el límite de lo que se ve, y por tanto de lo que se puede hacer.

**Regla.** Render: quinta textura multiplicativa (patrón de `_vistaTexture`), rellenada cada `LuzCadaTicks` para todos los chunks, dormidos incluidos (`luz` cambia sin cambiar la materia), con brillo por banda de luz (8 bandas; suelo de penumbra 25 %: un solo número). Sustrato: lista `LabLamparas` (posición y potencia; 160 da ~20 celdas con `LuzDecayAire` 8) inyectada en el reset de `LabLuz` junto a las fuentes `EmiteLuz`; la ventana `fx0..fx1` la absorbe. La lámpara ES física: una planta bajo ella cuenta luz ≥ 40 y crece; el humo la apaga (`LuzDecayHumo` 24). En el banco no hay lámparas: los hashes no se mueven.

**Cruces.** Plantas: la luz que ve el jugador es la que ve la semilla; el fracaso del huerto (7 de 73 caras) se ve como sombra. Humo: la cámara oscurecida por el humo del horno (R148) ciega a quien está dentro. Fuego: cada llama, brasa y hogar es una lámpara fija; encender es alumbrar. Agua: `LuzDecayAgua` 20 deja oscuro el fondo de una poza.

**Decisiones nuevas.** Dónde poner el fuego para ver y para que crezca lo de debajo; llevar la lámpara al huerto de noche (solo da luz: es honesta); abrir una boca de cielo para ver sabiendo que cambia el clima; en multijugador, cada uno ve su propio radio: asimetría sin roles.

**Observabilidad.** Es el visor de sí misma: la luz se ve porque es luz. La vista «Luz» de F8 queda como comprobación numérica.

**Coste.** 1 semana de Opus (textura, repintado de dormidos, lámpara en `LabLuz`, un escenario). **Determinismo:** la lámpara es una entrada de la simulación (host autoritativo en ruta A; entrada en lockstep). **Coste por tick:** `LabLuz` puede doblar su ventana con la lámpara lejos de la boca (2,86 ms la pasada entera antes de H5, cada 16 ticks). Render: 221 k escrituras cada 16 ticks.

**Verificación headless.** Siete hashes intactos sin lámpara. Escenario nuevo «lámpara sobre lecho»: lámpara fija sobre el lecho oeste 9 000 ticks → `LabPlantasNacidas` mayor que el alambique base; `MsLuz` con lámpara en x = 700 dentro de presupuesto.

**Tuning humano:** 8 (suelo de penumbra, potencia, paleta). **Apalancamiento:** 8. **Riesgo mayor:** la oscuridad como frustración; el suelo del 25 % lo mitiga, pero si sube demasiado la luz deja de importar.

---

## 3. RAYOS X POR BANDAS DE LEY · un campo, un radio, una sonda plantada

**Resumen.** `VistaLaboratorio` deja de ser un mapa global de rampas continuas y pasa a ser lo que ve una sonda plantada: un campo, cuantizado a los umbrales del motor, en un radio. El jugador construye su red de percepción plantando sondas escasas.

**Regla.** `LabBandas` (C# puro): `BandaTemp(raw)`, `BandaHumedad(mat, h)` (porosos por umbrales de planta, fibra, abono, compactación, ablandamiento; aire por fracción de `Saturacion(temp)`: < ½, < 1, ≥ 1 condensa), `BandaLuz` (0, < 40, < 128, ≥ 128), `BandaCarga(mat, c)` (agua: 40 turbia, 64 erosiona, 200 deposita; poroso: colmatación; sedimento: fertilidad 40/128), `BandaReposo` (3 móvil, 24 deposita, 200 compacta). Todas leen `LabParams` en vivo. `LabVistaColor` devuelve ≤ 8 colores fijos por banda en lugar de rampas. Sondas: generalizar `Termometro.cs` (tecla G, tres sondas FIFO, 4 Hz) para que cada sonda lleve un campo y un radio (24 celdas, Chebyshev, tres comparaciones por celda) y la vista solo pinte dentro del radio de una sonda de ese campo. Con zoom lejano, agregado 4×4 con la banda máxima del bloque. La lista de sondas es local a cada cliente: no toca la sim.

**Cruces.** Lector R140: el rótulo dice la banda («a una banda del vidrio»). `Estado`: sus constantes fijas (`EncharcadoU`, `AireSaturadoU`...) pasan a `LabBandas` para que palabra, color y física coincidan siempre. Sordina: la «gramática de vibración» es la banda superior del visor, sin mecanismo aparte. Huella y registradoras usan la misma tabla.

**Decisiones nuevas.** Qué medir y dónde con tres sondas; mover una es dejar de ver otra cosa; leer el aire por fracción de saturación para predecir dónde va a llover; en multijugador, «yo veo humedad aquí, tú ves calor allá»: asimetría temporal sin roles por geografía.

**Observabilidad.** El visor es la observabilidad. Sin sondas: `LabTinte` y las palabras del lector, que ya son bandas.

**Coste.** 1 semana de Opus (`LabBandas`, paleta, máscara, sondas con campo, agregado 4×4, lector). **Determinismo:** ninguno. **Coste por tick:** 0 en sim; la cuarta textura ya existe.

**Verificación headless.** Prueba unitaria: para cada parámetro-umbral, `banda(umbral) != banda(umbral - 1)` y la banda sigue al slider; hashes intactos por construcción. La legibilidad no se verifica sin personas.

**Tuning humano:** 9 (radio y paleta). **Apalancamiento:** 7. **Riesgo mayor:** que un campo a la vez frustre a quien quiere verlo todo, y que ocho colores sobre celdas de un píxel se confundan con la materia.

---

## 4. SONDAS REGISTRADORAS · el cuaderno que dibuja, compartido por el banco y el juego

**Resumen.** Sondas que además de leer recuerdan: un anillo de muestras por sonda, muestreado en la sim a cadencia de visita, dibujado como veta de colores de banda y volcado al fichero. El mismo instrumento mide en el banco headless y en la partida.

**Regla.** `LabRegistro` en la sim: hasta 8 sondas (x, y, campo) con dos anillos de 256 bytes: A cada 8 ticks (68 s) y B cada 256 ticks (36 min simulados: una vigilia a ×10 en 3,6 min de reloj). Se muestrea en `LabPasadas` tras `LabCampos`. Va en el fichero de partida (4 KB). Render: la veta, una tira de colores de banda con las líneas de umbral marcadas, sin números, como un testigo de sondeo. En `LabBench`, `Resultado` incluye las vetas como texto: `MedirLecho` y `ArcoMuestra` se reescriben como sondas, y la regresión pasa a decir CUÁNDO divergieron dos corridas, no solo que un hash cambió.

**Cruces.** SOLTAR: la veta es el veredicto diferido; al volver se lee la noche. Asíncrono por fichero: comparar dos grupos es comparar vetas, métricas que la simulación ya produce. Tolva de 466 s: una veta de calor que aguanta y cae. Horno: la veta de la arena enseña si el rojo se sostuvo 60 visitas seguidas o se cortó.

**Decisiones nuevas.** Qué ocho cosas merecen memoria; cuándo soltar y por cuánto; leer una oscilación y sellar una boca; retar a otro con un fichero («mi veta de vidrio, sin abrir el horno»).

**Observabilidad.** Sin visor: la veta pintada en la pared junto a la sonda. Con visor: superpuesta al radio de la sonda del candidato 3.

**Coste.** 1 semana de Opus (estructura, muestreo, guardado, volcado del banco, veta en HUD). **Determinismo:** no afecta a la física; las vetas son deterministas y se pueden hashear (noveno hash). **Coste por tick:** despreciable.

**Verificación headless.** Vetas de los nueve escenarios en el informe; la del lecho reproduce 22/36 y 0/36 de r148 y r150; la de la tolva muestra banda de calor ≥ 3 durante ≥ 466 × 30 / 256 muestras de B.

**Tuning humano:** 9. **Apalancamiento:** 7. **Riesgo mayor:** que una tira de colores no se entienda como tiempo; mitigación: el eje es el mismo que el reloj de la vigilia y la tira crece hacia la derecha mientras se mira.

---

## 5. Cómo forman un sistema

Una tabla de bandas, cuatro instrumentos: huella (espacio, para siempre), veta (tiempo, por sonda), rayos X (presente, por radio), luz (el límite). Sondas y lámparas son por jugador, la huella es de todos: la asimetría multijugador sale sola. Los números son los de `LabParams`; las cuatro se validan en el banco sin personas salvo la legibilidad, que se prueba en la primera sesión con jugador. Orden: bandas (3) primero; luego huella (1), luz (2), registradoras (4). Total: 4,5 semanas de Opus, paralelizables de dos en dos.

## 6. Lo que descarté y por qué

- **Visor de corrientes.** No hay tiro: el aire no se mueve, y el «rumbo» de `ProcessGas` (`SalGasRumbo`, hash por bloque 8×8 cada 16 ticks) no es un campo. Un visor de corrientes mentiría como la pátina mojada. El humo ya es el trazador del gas y la turbidez el del agua. Si otro panelista añade viento, el visor es trivial: edad desde `touchedTick` en bandas más una flecha por chunk.
- **«Todo avisa antes de cambiar» como mecanismo propio.** Redundante: `reposo` ya cuenta el tiempo al rojo y la quietud, y la banda superior del visor es el aviso.
- **Seres-sensor (salamandra, caracol, polilla).** Son agentes: comportamiento, arte y tuning de lo que parece honesto. Una sonda y una huella miden mejor.
- **Sondas como material nuevo.** Un id más en 80 materiales cruza colisión, cincel y frasco (`EsSolidoDelMundo`, la lección del vidrio en R145). Metadata con posición basta.
- **El cuerpo como sonda.** Es de la lente del cuerpo; solo pido que use `LabBandas` para que piel, lector y visor digan lo mismo.
- **Ver todos los campos a la vez.** Es la sobrecarga que el brief nombra; un campo por sonda es la respuesta de diseño.
- **Agregado por chunk (16×16).** Demasiado grueso para plantas de una celda; 4×4 con máximo es el compromiso.
