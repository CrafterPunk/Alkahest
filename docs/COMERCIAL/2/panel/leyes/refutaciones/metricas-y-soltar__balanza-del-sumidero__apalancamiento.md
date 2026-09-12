# REFUTACIÓN · balanza-del-sumidero (lente metricas-y-soltar) · eje: apalancamiento

**Veredicto: viable con ajuste.** Apalancamiento corregido 5 (declarado 9), tuning 7 (declarado 9), coste 1 semana.

## 1. La regla, tal como está escrita, se come su propio escenario de prueba

`LabTragar(j)` (SimStepper.Laboratorio.cs:1026) visita los cuatro vecinos de cada celda de sumidero
cada 8 ticks y hoy solo traga `Liquid`. La propuesta añade `|| Powder`. Pero en la tabla de
arquetipos (Universe.Laboratorio.cs) **Fibra, Sedimento, Grava, Semilla y Carbón son todos
`Powder`**: el motor no distingue producto de materia prima. Sobre el propio escenario de
verificación («carbonera 20x20 con suelo de sumidero»): `MontarCarbonera` (LabBench.cs:231-237)
es un `Bloque` de Fibra apoyado en el suelo del recinto. Con suelo de sumidero, la fila inferior se
traga a una celda por cara cada 8 ticks, la pila cae (es polvo) y las 400 celdas desaparecen en
~160 ticks, cinco segundos, antes de que llegue la yesca. El banco daría
`LabSumidoPorMat[Fibra] = 400` y `Carbon ≈ 0`: la prueba que cierra el eslabón «recogida» falla
por construcción.

Lo mismo vale para dos de las cinco decisiones declaradas: el banco de grava que toca la salida se
traga; un lecho pegado a la pared del sumidero (traga lateralmente, `LabTragar(i-1)`, `(i+1)`) se
traga. Y borra un cruce que YA existe y nadie escribió: el agua turbia en reposo sobre fondo
(`EsFondo` incluye al sumidero, línea 466) deposita Sedimento encima del sumidero y lo **ciega**:
hoy una salida se colmata sola y limpiarla es una decisión; con «todo Powder» esa fricción muere.

## 2. «Cero escrituras nuevas, siete hashes quietos» es una afirmación, no un hecho

Tragar polvo pasa por `LabTransformar` (escribe `mat`, `humedad`, `carga`, `reposo`; `SetCell`
pone `aux` a 0) sobre celdas que hoy no se tocan. Si una sola celda de polvo llega a una cara del
sumidero en los 3 000 ticks de «laboratorio base» (el pozo de x416-429 recibe la poza rebosante),
los hashes se mueven; y escribir `idSalida` en `aux` cambia `HashAux` salvo que la salida base sea
la 0. Se rebasa en una tarde, pero se vendía como garantía.

## 3. Cuántos cruces reales crea

Contando solo lo que cambia en la física: **uno** (la materia sólida puede salir del mundo por una
salida). El resto son medidas de física que ya existe:
- turbidez: real y geométrica (la decantación pasa finos al agua de ABAJO, líneas 455-464: una
  salida por rebose recibe clara, una en el fondo recibe turbia). Es el mejor cruce y no exige
  tocar la física;
- calor entregado: medida; «enfriar antes de la salida» no tiene motivo físico;
- presión / tubo en U: ya ocurre hoy con líquidos;
- semillas y fibra como «censo»: tragar fibra es destruir material de nivel.

Las decisiones («a qué salida», «carbón o energía», «turbio y rápido») **no las crea la balanza:
las crea quien pone valor a cada columna del recibo**, la `Condicion` del candidato 2 o el autor
de la situación. La balanza mide; no ejecuta ni juzga. Sola es instrumentación (buena y barata)
cuyo apalancamiento se ha contado con el del sello. La mitigación del riesgo mayor lo confiesa:
«la geometría entre la máquina y la salida es el juego» es geometría de autor por situación, lo
que la función objetivo penaliza, salvo que la cuna (candidato 3) la genere.

## 4. Tuning que esconde

`AguaClaraCargaMax = 16` decide si «decantar antes» existe como decisión, y se cruza con
`TurbidezFuente` 40, `Decantacion` 6, `FactorMovilPct`, `ReposoMovil`, `DepositoUmbral` 200 y
`DepositoReposo`: cualquier retoque de la sedimentación mueve el umbral de «clara». El valor
relativo de carbón, calor y claridad es tuning por situación, no del motor.

## 5. Versión mínima con más apalancamiento

1. **No tragar `Powder` en general.** Un bit `entregable` en `MaterialDef` solo para productos de
   un proceso que nunca son sustrato: Carbón, Ceniza, Semilla. Fibra, grava, sedimento, arcilla y
   arena quedan intactos: la carbonera con suelo de salida entrega su 25 % conforme la pila se
   desploma, y el filtro de grava puede tocar la salida.
2. **Tragar sólido solo desde ARRIBA** (`i+W`): lo que cae dentro, nunca lo de al lado. El
   sedimento depositado sigue cegando la salida (el cruce gratuito se conserva).
3. Libro por material, por salida (`aux`) y por calidad (clara/turbia con `carga`, calor con signo),
   volcado por bucle en `EscribirLibro` e `Informe()`; el banco rebasa hashes y añade la carbonera
   con salida con la aserción corregida: `Fibra sumida == 0`, `Carbon > 0`.

Así la pieza vale lo que es: el sensor que el sello necesita, una semana de Opus en banco,
apalancamiento 5 por sí sola y más cuando el candidato 2 le da juez.
