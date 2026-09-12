# Refutación · `piel-termica-humeda` (lente cuerpo) · criterio: apalancamiento

**Veredicto: REFUTADO como ley.** Sobrevive un sensor, que es otra cosa y cuesta la mitad.

## 1. Corrida contra las constantes del sustrato, la regla se apaga sola

Pasé los cinco pasos por `LabParams.cs` y por las puertas de `SimStepper.Laboratorio.cs` que el
candidato nombra:

- **Térmica.** `paso = flujo/(64·cCarne)` con `cCarne 24` y `KAire 4` (LabParams L111): 66 celdas de
  aire a +30 raw dan 5 raw por visita; constante de tiempo ≈ 1,5 s. El muñeco no solapa sólidos
  (`CajaChoca` solo atraviesa polvo, líquido y gas): su `Calor` es la MEDIA DEL AIRE cubierto con 1,5 s
  de retraso, un termómetro andante, y el termómetro ya existe (tecla G). «Sesenta celdas de carne»
  daría `c = 60·CAgua(4) = 240`, no 24: el número está elegido a mano y es justo el que decide si
  esto es termómetro o presupuesto.
- **Secarse.** `LabSecarHacia` (L734) con `Secado 3` hacia 66 celdas de aire: 1-2 u por celda y visita,
  255 u en ≈ 1 s A TEMPERATURA AMBIENTE. Te secas antes de llegar a nada. `LabLatente` (L281) con
  `Latente 4` enfría 255·4/255 = **4 raw en total** (8 °C, una vez): eso es el «traje ignífugo», y
  `Latente` es global (libro de energía del alambique), no se sube solo para la piel.
- **Gotear.** Solo con `Mojado > 128`, y el secado gana por un orden de magnitud: no goteas salvo en
  aire saturado (la cámara del alambique), donde ya llueve. `LabNacerAgua` (L264) nace a 255, no a 16:
  la puerta citada no hace lo que dice el texto. Y 16 u contra `PlantaHumedadMin 60` y
  `FibraMojadaMin 100`: hacen falta siete gotas en la misma celda, 2 s quieto, el negativo de R148
  (columnas, no lechos) con otro nombre.
- **Aliento.** 2 u/visita = una celda cada 34 s; el manantial da 20 celdas/s: 1/680. Decorativo.

## 2. La única consecuencia que ejecuta la simulación la esquiva el juego heredado

`calorSuelta` abre la mano. Pero `Flask.ReachWorld = 6f` (Flask.cs L107): con la escala del propio
documento (0,64 u = 6,4 celdas) son **60 celdas de alcance**, y el aprendiz VUELA
(`ApprenticeController` L186, modo B por defecto). Nadie necesita estar en el penacho para cargar el
horno ni mojarse para cruzar la poza. De las seis decisiones nuevas, (c) y (f) existen hoy; (a) vale
4 raw; (d) exige 2 s quieto sobre la yesca; (b) y (e) cuelgan de un umbral que solo se cruza posándose
ENCIMA del hogar (es sólido: la fila de los pies queda clavada a 170). Cruces con decisión nueva que
las leyes juzguen: **0 de 7**. El accidente que queda (posarse en el hogar y vaciar `Capacity 900`
celdas) no es una apuesta: es una inundación por tropiezo, y pedirá un tope, otro número.

## 3. Capa encima, y el tuning de siempre

El intercambio entra por las puertas honestas (`LabSumarTemp`, `LabTransformar`, `LabBalanceU`). Pero
las consecuencias viven en `FactorSalto/FactorVelocidad/FactorControl`: juzga el controlador, no la
ley. `pesoMojado 0,35` reabre el salto de 2,2 u afinado nueve rondas (R110-R121) contra la geometría de
los niveles: un saliente que mojado no se salta es un jugador atrapado, balance ante geometría.
`frioTorpe`, `0.5`, `0.7`, `cCarne`, `calorSuelta` y el `piel.seca` que hará falta para que el secado
no sea instantáneo: siete números de sensación. El banco mide «5,3 s»; no puede juzgar si es juego.

Deudas no declaradas: `LabCuerpos()`/`CuerposActivos` ya son de la roca suelta (L1290, TODO H6);
condensar SOBRE el cuerpo (cruce 4) no está en la regla y exige tocar `LabAire` (L328) por cada celda
de aire; el espejo no recibe `temp[]` (`SimSync` L51), así que el halo del invitado no existe.

## 4. Lo que sí compra algo: el sensor, no la ley

El apalancamiento real está en la PERCEPCIÓN: el muñeco como sonda siempre encendida, respuesta a la
carencia medida («solo se ven con F8»). Versión mínima: `CuerpoSim {Calor, Mojado}` con solo el paso
térmico y empaparse/secarse (sin gota, sin aliento, sin umbrales, sin costura de control), tinte del
sprite por `Mojado` con `LabTinte`, y `VistaLaboratorio.Piel` = halo de 12 celdas de las vistas de
temperatura y humedad existentes. Cero tuning de sensación, media semana, hashes intactos. ANTES, un
día de banco: leer `temp[]` en la caja 6×11 a 0, 2 y 6 celdas del hogar y dentro del horno-recinto de
`MontarAlambique`. Si la media ponderada por k no pasa de 100 raw fuera del recinto, la mitad térmica
del candidato ni siquiera era alcanzable, y el umbral se discute con un número, no con un slider.

**Corregido:** apalancamiento 3 (de 9), tuning 4 (de 7), coste 2 semanas tal como está escrito
(0,5 la versión sensor más el día de sonda).
