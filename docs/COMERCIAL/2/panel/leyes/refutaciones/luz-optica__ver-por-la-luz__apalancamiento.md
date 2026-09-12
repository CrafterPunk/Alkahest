# Refutación · `ver-por-la-luz` (luz-optica §3) · lente: apalancamiento

**Veredicto: REFUTADO** como ley y como primer paso del paquete. Sobrevive una versión mínima
distinta de naturaleza (una vista, no un render) que se detalla al final.

Leído: `SimStepper.Laboratorio.cs` (LabLuz :1177-1284), `LabParams.cs` (:124-131, :289-295),
`LabMateriales.cs:110`, `SimRenderer.cs` (:83, :825-895, :1054-1101, :1542-1600),
`SimRenderer.Laboratorio.cs` (LabTinte :27-107, vistas :118-236), `SimLevelBuilder.Laboratorio.cs`
(:5-30, :76, :164), `Flask.cs` (:528, :596), `LabPanel.cs:117`.

## 1. No es un cruce: es un filtro. Cuenta de decisiones nuevas: cero

De los seis «cruces», cinco ya existen dentro de `LabLuz` (fuego/brasa/hogar emiten por
`EmiteLuz`, `LabMateriales.cs:110`; humo 24, agua 20, planta 12 en `LabLuzDecay` :1276-1284)
y el sexto es falso: `VidrioVerde` es sólido, `LabLuzDesde` (:1269) devuelve 0 para él, así que
«vidrio: ventana que deja ver» no ocurre sin `haz-y-espejo`. El propio §0 del documento lo dice.

De las cuatro «decisiones nuevas»:
- *Llevar fibra ardiendo para ver*: no hay verbo. El frasco rechaza los sólidos del mundo
  (`Flask.cs:528`, `:596`) y el fuego no es fluido; en el laboratorio el fuego solo se pone con
  el pincel de F8 (`LabPanel.cs:117`, `"fuego", Caliente = true`), que es depuración. El muñeco no
  está en la grilla: una luz que viaje con él exige inyectar un emisor virtual en `LabLuz`
  (toca sim y `HashLuz`) o pintar un halo en el sprite, que es la capa decorativa que la propuesta
  dice no ser.
- *Encender el hogar por la luz*: el Hogar es un bloque del plano (`SimLevelBuilder.Laboratorio.cs:76`)
  y `LabHogar` lo clava a 170 raw sin condición alguna. Está siempre encendido. No hay decisión.
- *Enrutar sol* (cavar un tiro vertical) y *no ahumar la cámara* ya existen para las plantas
  (R148: el humo del alambique apagó el huerto).

Decisiones netas creadas: 0. Decisiones existentes que se vuelven visibles sin F8: 2. Eso es
observabilidad, no ley, y así hay que puntuarla.

## 2. Hereda un número afinado para otro consumidor (R47/50)

`LuzDecayAire = 8` se eligió para las plantas («alcanza ~30 celdas desde una hoguera»,
`LabParams.cs:290`). Con esa cifra el halo del Hogar (x150-158) muere en x≈118: el **spawn
(118,182) queda a luz 0-7**, es decir al 16 % de brillo con `LuzVerMin = 40`. La boca del cielo
(x118-124) ilumina lateralmente x86-156; la galería del arroyo, la poza, la cámara profunda y el
sumidero (x36-430, unas 400 columnas) quedan en negro. El sistema 5/5 del laboratorio, el agua, se
jugaría a oscuras.

`LuzVerMin` no es una válvula: a 40 hace obligatorio F8; a 150 es un tinte decorativo. Lo que
arregla la geometría es bajar `LuzDecayAire`, y ese es un parámetro de **simulación**: cambia dónde
germinan las plantas (:689-704, :870-873), agranda la ventana H5 (255/dMin columnas por fuente;
la pasada sin ventana costaba 2,86 ms de media y 7,88 de pico) y mueve `HashLuz` en los nueve
escenarios. «No toca la simulación» es verdad del diff y falso del proyecto.

## 3. Colisiona con dos canales de brillo ya calibrados mirando

- **Incandescencia legible** (`SimRenderer.cs:1542+`): contrato de que a 390 °C sobrevive ≥55 %
  del matiz porque «el tinte borraba la identidad» (playtest 40, R13). Multiplicar el color por
  0,16-0,5 destruye la discriminación de matiz en toda zona en sombra y apaga el rescoldo de una
  terracota a 150 raw en cuarto oscuro (luz 0: solo Fire/Brasa/Hogar emiten).
- **LabTinte** (`SimRenderer.Laboratorio.cs:47-60`): mojado = −37 % de brillo, y el docblock dice
  «esa información es el juego del laboratorio». Con sombra encima, mojado y penumbra son el
  mismo gris.
- El aprendiz es un sprite (`sortingOrder 50`) fuera de la grilla: quedaría a pleno color en la
  cueva negra. Y con `LuzCadaTicks = 16` la llama, que cambia de celda cada tick, da una lámpara
  que salta cada medio segundo; el desenfoque 3×3 es espacial, no temporal, y subir la cadencia
  también mueve hashes.

## 4. Coste y tuning corregidos

La «pieza de ingeniería» de chunks sucios es casi innecesaria: `FullRefreshEveryFrames = 30`
(`SimRenderer.cs:83`) repinta todo lo visible cada segundo y la vista F8 Luz ya vive con ese
retraso documentado (`SimRenderer.Laboratorio.cs:118-123`). El coste real está en reconciliar
tres capas calibradas con capturas y, casi seguro, re-basar nueve hashes al tocar `dAire`:
**1,5 semanas, no 0,75; tuning 4, no 6** (no hay válvula única: hay tres números en dos
consumidores). Apalancamiento **3**: multiplica a `haz-y-espejo` e `insolacion`, pero solo no
ejecuta, no revela nada que F8 no revele y no juzga nada.

## Ajuste mínimo viable: una vista, no un render

Añadir `VistaLaboratorio.Ojo` al enum (`SimRenderer.Laboratorio.cs:236`) y un caso en
`LabVistaColor`: `new Color32(0, 0, 0, (byte)((255 - luz) * SombraMax / 255))`, `SombraMax ≈ 200`.
Usa la cuarta textura que ya existe (`sortingOrder 54`: oscurece también al muñeco y al velo,
gratis), no toca `ComputeCellColor` ni sus contratos, se apaga con una tecla, cuesta 1-2 días,
tuning 9 (un byte), y se verifica con la captura PNG headless. Queda como instrumento de
verificación de `haz-y-espejo`, el único candidato que le da algo que ver. Orden corregido:
1 → 3 (vista) → 2 → 4, no 3 → 1. Si algún día el ojo pasa a render por defecto, será después de
que `LuzDecayAire` tenga un consumidor visual propio y hashes nuevos, no antes.
