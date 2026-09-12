# Refutación · luz-optica / haz-y-espejo · lente APALANCAMIENTO

**Veredicto: viable con ajuste.** Es un cruce de verdad (cambia la propagación de un campo que ya
existe, da función a dos materiales que ya se fabrican y un verbo al cincel), determinista y
verificable en banco. Pero el 9 se apoya en un cruce estrella que no existe y en dos más que
tampoco.

## 1. El cruce estrella apunta a un número que ninguna ley lee

Los «7 de 73 caras» miden la `luz` de la celda de **sedimento** («cara» en el benchmark r150).
Nadie la consume: la germinación lee `luz[i + W]`, el aire de encima
(`SimStepper.Laboratorio.cs:704`); la punta crece por `luz[arriba]` (:873); la semilla lee su
propia celda, que los barridos laterales sí encienden. Y ese aire ya está encendido: el mapa de
R134 (`CHECKPOINT.md:462`) da 72-216 sobre x100-145 con la boca de 7 columnas, todo por encima de
`PlantaLuzMin` = 40.

Los 7/73 son un artefacto de orden en `LabLuz`: el barrido descendente (:1228) corre ANTES que los
laterales (:1250), así que una cara horizontal solo recibe luz si su columna ya estaba encendida
en la primera pasada, o sea bajo la vertical de la boca. El comentario de :1284 promete «los
sólidos se iluminan como la celda de aire que los toca», y ninguna pasada lo cumple (R49). La
propia evidencia lo confirma: el control de R148 (serpentín fuera de la boca, boca original)
germinó «cinco veces más»; la boca de 25 columnas de r150 dio 9 contra 2, el mismo orden. El
limitador era la sombra del serpentín y luego la humedad, nunca la anchura.

Lo que el haz añadiría sobre el lecho es **intensidad** (72 → 200), y por encima de 40 no hay
ley que la lea. Cero decisiones compradas, y la aceptación propuesta («≥30 de 36 columnas con
luz ≥ 40») ya se cumple hoy medida donde la leen las plantas.

## 2. Decisiones nuevas: 2-3 de 6

- *Haz al huerto o al hogar*: el hogar no consume luz; sin `insolacion` no es decisión.
- *Corredor limpio de humo y agua*: ya existe. Humo 24 y agua 20 matan 255 en las mismas ~10 y
  ~12 celdas en el haz que en la difusa; la sombra del alambique (R148) es ese cruce hoy.
- *Espejo de hielo*: ningún escenario produce hielo (solo está en el pincel del panel), y
  `meltsAt` = congelación + 5 raw (`Universe.cs:807`) = 65 contra el clima de la cámara alta a 64
  (`SimLevelBuilder.Laboratorio.cs:177`): un raw de margen. Es un número de nivel por ajustar.
- *Cuña y su lado*, *ventana o espejo*, *sellar con vidrio → invernadero*: reales, y son UNA
  capacidad: la luz pasa de las 32 celdas laterales de hoy y cruza vidrio. El invernadero es el
  mejor cruce del documento, con dos peajes callados: `VidrioVerde` cae con cohesión 3
  (`Universe.cs:1012`, `SolidoTieneApoyo` en `SimStepper.cs:1379`), así que un techo de vidrio
  de más de ~6 celdas sin pilar se desploma; y la difusa sigue con `LabLuzDesde` = 0 para
  sólidos: una ventana fuera de la trayectoria de un rayo es roca.

## 3. Tuning que esconde

`LuzDecayCielo` = 1 pasa a ser el alcance del haz: 255 celdas, un tercio del mapa. Una cuña
ilumina cualquier galería y el «presupuesto de luz» que `01_PROPUESTAS.md:784` llama conflicto
central deja de ser escaso. Escasez contra alcance es juicio de Cesar, no número del banco.
Vidrio 4 y hielo 6 son arbitrarios. Y pasar el descendente a `dAire` mueve `HashLuz` en todo
escenario con fuego (siete de nueve), no solo en «laboratorio base».

## 4. Versión mínima con más apalancamiento

1. **Primero, sin ley nueva (1 día):** quinta pasada descendente con `dAire` tras los laterales
   (~0,1 ms) para que las caras cumplan :1284, y remedir Q16 con `luz[i + W]`. Puede reabrir H4
   gratis.
2. **Haz + cuña solo con `VidrioVerde`**, sin hielo hasta que un escenario lo produzca. El
   decaimiento del haz en aire es parámetro NUEVO (`LuzDecayHaz`, 2-3 → alcance 85-127), no
   `LuzDecayCielo`; la difusa no se toca.
3. **Aceptación que la ley actual no pueda pasar:** «galería a 60 celdas» (lecho a 60 celdas
   laterales de la boca con cuña; métrica: columnas con aire ≥ 40 encima, hoy 0 por
   construcción) e «invernadero» (cámara sellada con techo de vidrio apilarado, planta y agua;
   métrica: saturación y condensación interior).

Depende de `ver-por-la-luz` para leerse sin F8.

**Corregido: apalancamiento 6 · tuning 7 · coste 1 semana** (el laboratorio hizo 14 materiales
en 3 días; esto son ~80 líneas de pasada, una entrada de tabla y dos escenarios).
