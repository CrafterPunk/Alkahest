# Refutación de ingeniería · `ver-por-la-luz` (El ojo del aprendiz ve por luz)

**Veredicto: VIABLE CON AJUSTE.** Es la propuesta más barata del panel y la única que no toca un
byte de la simulación, pero el documento subestima tres puntos de integración y cuenta como suyas
dos decisiones que pertenecen a otros candidatos.

## Lo que el código confirma

- La multiplicación cabe en una línea. `ComputeCellColor` (`SimRenderer.cs:1134`) termina en
  `:1638` con `LabTinte(...)` y el `return` en `:1640`; el gancho va ahí o en `RenderChunk`
  (`:1071`) tras la llamada, gateado por `LabTinteActivo`, que ya es solo-laboratorio
  (`AlkahestSim.cs:351`). El fuego sale por un `return` temprano (`:1169`) pero su `luz` es 255
  (`LabMateriales.cs:110` + `LabLuz` `:1187`): la lámpara es identidad, sale gratis como se dice.
- Sin efecto en hashes: `LabBench.cs:331` hashea `luz`, no el render. El banco de 9 escenarios debe
  dar los mismos ocho hashes al bit antes y después; ese es el benchmark de determinismo y es trivial.
- La «pieza de ingeniería» es más pequeña de lo declarado: `RenderFrame` ya repinta TODO lo que hay
  en cámara cada 30 frames (`FullRefreshEveryFrames`, `:83`, `:825`). Con `LuzCadaTicks` 16 (0,53 s)
  la latencia sin marcador es ≤ 1 s. La versión mínima no necesita el marcador por chunk.

## Los tres ajustes obligatorios

1. **El marcador «luz cambiada» NO puede ser `chunkTouchedTick`.** `WakeChunk` (`CellGrid.cs:305`)
   pone `chunkSleepTimer = 0` y `chunkTouchedTick = tick` a la vez: despertar un chunk por luz es
   visitar sus celdas en física, y las celdas visitadas tiran del XorShift por celda (sordina,
   evaporación, germinación). Movería hashes y sumaría al tick. Si se quiere latencia menor que la
   del refresco completo, va un array aparte (`luzTouchedTick[ci]` en `CellGrid`, escrito por
   `LabLuz` comparando contra una copia previa dentro de la ventana `bx0..bx1`, ~40 k bytes cada 16
   ticks, < 0,05 ms) y un `_chunkLastLuzTick` propio junto a `sucioFueraDeCamara`
   (`SimRenderer.cs:891-892`). Nunca por `WakeChunk`.
2. **La piel de roca.** Con `OcultarRoca` la roca madre NO pasa por `ComputeCellColor`
   (`:1152`): la dibuja `PielDeRoca` como malla, y `AplicarModo` (`PielDeRoca.cs:353`) no sabe de
   `ModoLaboratorio`. Resultado: aire negro y roca iluminada, la imagen invertida. Mínimo: forzar
   `Nivel.Apagada` en modo laboratorio (una línea); la versión con piel exige teñir la malla por
   chunk con `luz`, que no está presupuestada.
3. **El mundo entero es oscuro salvo 137 columnas.** El reset de `LabLuz` (`:1185-1194`) pone
   `luz = 0` en las 768 columnas y los barridos solo tocan la ventana (`bx0..bx1`, ±32 de las
   fuentes). Galería del arroyo (x36-430), poza, cámara profunda: todo a `LuzVerMin`. Con 40, el
   80 % del nivel se juega al 16 % de brillo. No es un borde de tuning: es el estado por defecto.
   `LuzVerMin` debe fijarse con un criterio verificable, no solo con una mirada: a ese suelo, los
   materiales de vocabulario (R13/17/23) deben seguir distinguiéndose (Δ luminancia roca/sedimento
   /arcilla ≥ 12/255 medido en el PNG headless). Eso convierte la válvula en una cota.

## Lo que no cuenta o cuenta mal

- **Saltos de medio segundo.** `LabLuz` reconstruye el campo desde cero cada 16 ticks: la llama
  que nace y muere y el humo que deriva mueven el campo a 2 Hz en escalones. Una antorcha que
  parpadea a 2 Hz se lee como tirón. Mitigación solo render: un `_luzVista` por celda que
  interpola hacia `luz` en cada repintado; obliga a repintar la ventana unos frames tras cada
  pasada (≈160 chunks × 256 celdas). Medir, no suponer.
- **El muñeco no está en la textura.** Aprendiz, máquinas y halos son sprites: un cuerpo a pleno
  color en una cueva negra. Una línea (`_bodySr.color` × luz bajo los pies,
  `ApprenticeController.cs:1201`) lo cierra y siembra «el cuerpo reacciona a la luz».
- **«Llevar fibra ardiendo para ver»**: no existe verbo. El frasco transporta fluidos y el
  documento admite en §6 que «el muñeco no está en la grilla». **«Enrutar sol como lámpara»**: hoy
  la luz del cielo cae en vertical por la boca (`dCielo` 1, lateral 8) y no hay nada que enrutar
  sin `haz-y-espejo`. Dos de las cuatro decisiones nuevas son de otros candidatos.

## Versión mínima y banco

Una línea en el render + `LuzVerMin` como parámetro de `SimRenderer.Laboratorio` con slider +
piel de roca apagada en laboratorio + tinte del cuerpo. Sin marcador por chunk (lo cubre el
refresco de 30 frames); se añade después si la latencia molesta. Banco: (a) los 9 hashes
idénticos; (b) `RenderFrame` con y sin cambio, chunks repintados por frame y ms, en el escenario
`laboratorio base` con cámara sobre el hogar; (c) PNG headless (receta RunCommand: grilla →
`Texture2D` con la misma fórmula) de la boca del cielo, la sala del hogar con fuego y la galería
del arroyo, con luminancia media por región y la cota de contraste entre materiales al suelo.

## Números corregidos

Apalancamiento 7 → **6**: no añade ley ni decisión propia; transforma la percepción de todas las
existentes y es condición de 1 y 2, pero sus decisiones propias son dos, no cuatro. Tuning 6 →
**7**: un suelo, y con la cota de contraste deja de ser gusto puro. Coste 0,75 → **1 semana**:
piel de roca, sprite, suavizado y banco de render no estaban en la cuenta.
