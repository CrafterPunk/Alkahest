# Refutación · huella-maximos (lente instrumentos-rayosx) · criterio: apalancamiento

**Veredicto: viable con ajuste.** Un buen instrumento vendido como ley: su apalancamiento es el de un visor persistente, no el de un cruce.

## 1. Cruces nuevos: cero

El candidato lo dice: «la física nunca la lee». Un campo que ninguna ley consume no cruza ninguna ley; los cinco «cruces» son *lecturas* de cruces que ya existen. En la pregunta de Cesar (EJECUTAR, REVELAR, JUZGAR) esto es solo REVELAR. Y tres de las cinco lecturas no resisten el código:

- **Plantas.** `LabPlanta` solo mata por `savia == 0` sostenida (`SimStepper.Laboratorio.cs:884-906`). No hay ahogamiento: «banda 6 = ahogado» es un diagnóstico que la sim no produce. Peor: el fracaso medido en R135/R148 (humedad 50-99 con mínimo 60) es un fallo de MÍNIMO oscilante, y un registro de MÁXIMOS es ciego a él: marcaría «alguna vez ≥ 60» bajo una planta que murió por bajar de 60.
- **Horno.** El vidrio no depende del pico sino del tiempo sostenido: `reposo` cuenta visitas al rojo con ceniza y se reinicia si cae (`:607-620`). Una llama suelta también toca la banda 6 (R145: ~200 raw a dos celdas, 1-2 vidrios): la «certificación de pared» no distingue horno de hoguera. El dato que sí lo distingue ya está en `reposo` y nadie lo pinta.
- **Arena trazadora.** El agua no transporta arena: `EsErosionable` es solo Sedimento y Arcilla (`LabMateriales.cs:101`) y erosionar convierte la celda en agua turbia; viaja `carga`, no el grano. «Verter arena marcada para ver el camino del agua» no ocurre; la turbidez ya es ese trazador.

«Leer el fichero de otro jugador» tampoco es decisión de esta ley: no hay guardado de mundo (solo `LabPresets` vuelca la grilla). Sobreviven una decisión y media: diseñar la poza por la marca de marea (memoria en vez de atención; útil en SOLTAR) y conservar o cincelar el registro, sin consecuencia en la sim porque nada lo lee.

## 2. La regla no cuadra con el código

- «`SetCell` del pincel la deja a 0»: `SetCell` es el creador universal. `LabTransformar` (17 llamadas: compactar, cocer, ablandar, vidriar, brotar, marchitar) pasa por él; el `Transform()` legado (30 llamadas: combustión → Carbón/Ceniza/Brasa) NO pasa y escribe `mat` a mano (`SimStepper.cs:707`). La terracota olvidaría su arcilla y el carbón recordaría su fibra, por accidente en ambos casos. Falta una copia explícita en `LabTransformar`.
- «La pátina se retira»: `patina` ya es un byte por celda con tizne por Fuego/Brasa/Humo y su ramal de color (`SimRenderer.cs:957-1035, 1614-1631`), vivo también en el taller fuera de `LabActivo`. Retirarla desnuda el taller: gatearla, o mejor, reutilizarla.
- Determinismo: solo aporta un octavo hash; los siete quedan intactos por construcción. Correcto, y prueba de que no es una ley.

## 3. Tuning escondido

El riesgo que admite (legibilidad del tinte en celdas de un píxel) es iteración con Cesar: roca enrojecida/blanqueada, marea y hollín compitiendo con 80 paletas y los patrones de `morph`, más la cadencia del hollín y un empaquetado 3-3-2 que fija ocho bandas para siempre. La sim se verifica en banco; la percepción, no. Tuning 7.

## 4. Ajuste mínimo con más apalancamiento

1. **Sin campo nuevo.** Bajo `LabActivo`, `patina` es la huella: se escribe en el prólogo del `switch` de `LabCampos` (`:201`), nibble alto banda máxima de `temp`, nibble bajo banda máxima de `humedad`; `ActualizarPatinaFranja` se salta con `if (LabActivo)` (`:837`) y el ramal de color existente traduce bandas. Cero memoria, cero textura, cero retirada.
2. Copia en `LabTransformar` (una línea). Octavo hash y dos aserciones; la del horno compara escenarios (horno 18/18 vs hogar 0), no solo «≥ 6 dentro».
3. El lector R140 añade una frase; vista «Huella» y hollín se posponen (el humo ya se ve y ya apaga la luz).
4. **Única forma de subir el apalancamiento:** que una ley la lea. `Fractura*` leyendo la banda de calor (roca o terracota que estuvo ≥ banda 5 y se enfrió se fractura antes: choque térmico) convierte memoria en consecuencia con una comparación, pero mueve hashes: segundo paso, no primero.

Coste honesto: 1 semana de Opus para 1-3 (la sim en dos días; el tinte y su playtest, el resto), sin contar la iteración de legibilidad.
