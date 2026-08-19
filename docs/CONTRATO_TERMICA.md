# CONTRATO CONGELADO — LA FÍSICA HONESTA (Playtest 44, ronda nocturna 1/2)

Mandato de Cesar (dormido — ronda autónoma, decisiones de Fable): *"nada de
las sensaciones prometidas de la seed 0 v2 está ocurriendo... necesitamos
unas físicas realistas dignas de mirar... si algo se está enfriando o
calentando que tenga relación realista: se calienta de a pocos, se propaga
la conversión hasta que todo se tuesta... vuelvan la placa de calor y la de
frío, más realistas (antes el frío irradiaba mucho y el calor poquito)...
termómetros en grados centígrados ligeros para tomar temperatura en
distintos puntos y validar... una versión de seed 0 donde en algún momento
entre la placa de frío para congelación y descongelación; podemos iniciar
con la placa de calor también."*

Ya hecho por Fable (no tocar): ParticulasFx APAGADA por defecto (flag
`Activas` + toggle dev en F3) y pátina MOJADO apagada (el tizne se queda).

Dos encargos PARALELOS: **T = térmica** y **I = instrumentos/arco/red**.

## 1. API CONGELADA ENTRE ENCARGOS

- Las FIRMAS PÚBLICAS actuales de `HeatPlate.Init(...)` y
  `ChillStone.Init(...)` NO cambian (I las invoca tal cual desde el
  Bootstrap; T rehace las entrañas).
- T implementa `IMaquinaUsableRemota` (ya existe, pt43) en ambas placas: su
  `UsarPorRed()` cicla el estado igual que el E local;
  `EstadoVivoRed()`: bit0 Trabajando = estado activo cualquiera, bit1
  FuegoEncendido = solo la placa de calor en su estado máximo, bit4
  LuzPlena = solo la fría en su estado máximo.
- Tipos de réplica nuevos (I los usa en MaquinaSync/MaquinariaSprites):
  `TipoPlacaCalor = 11`, `TipoPlacaFria = 12`.
- Errores transitorios entre encargos: protocolo pt40.

## 2. ENCARGO T — el calor que se ve pensar

Archivos de T: `Game/HeatPlate.cs`, `Game/ChillStone.cs`, `Game/Crisol.cs`,
`Sim/SimStepper.cs` (+`Sim/Universe.cs` solo constantes si hacen falta).

### 2a. La conversión POR FRENTES (el corazón del pedido)

Hoy la hornada del crisol calienta con rampa visible pero CONVIERTE TODO DE
GOLPE al cierre — el "tostado" es un parpadeo, no un proceso. Cambio:
durante la ventana de la hornada, cada celda de la carga se convierte
INDIVIDUALMENTE cuando su temperatura local alcanza la banda decidida,
muestreada (patrón barato `(x+y+tick)&N` + XorShift con salt nuevo para el
jitter) — el frente de conversión NACE donde está el calor (el fondo, junto
al hogar) y SUBE por la carga: se ve el tostado propagarse "de a pocos
hasta que todo se tuesta", literal. `CerrarHornada` conserva su papel de
GARANTÍA (convierte los rezagados al cierre — la promesa de UNA
transformación por hornada no cambia, la salida sigue decidida e inmutable
desde DecidirHornada). El evento/registro (`Hornada.RegistrarOp`,
testigos forenses pt40) se dispara UNA vez como hoy, al cierre.

### 2b. Las placas realistas (y simétricas de verdad)

- Rehacer la EMISIÓN de ambas placas con el mismo modelo físico: bombean
  calor/frío a las celdas de su zona con DECAIMIENTO POR DISTANCIA
  (falloff, no un rect uniforme) y empuje POR DIFERENCIA (cuanto más lejos
  está la celda del objetivo, más empuja — ley de enfriamiento de Newton
  de juguete). La difusión de SimStepper propaga el resto: el gradiente se
  LEE en el termómetro de I.
- Rebalance del reporte histórico ("el frío irradiaba mucho y el calor
  poquito"): HELANDO baja de -80°C a un objetivo helador pero terrenal
  (~-25°C raw, decisión de T documentada) con empuje IGUAL de agresivo que
  su gemela caliente; ARDIENTE conserva sus 320°C pero con el empuje
  reforzado para que hervir agua encima sea cuestión de segundos, no de
  paciencia. CRITERIO MEDIBLE (T lo verifica con diagnóstico headless):
  desde ambiente, una celda de agua a 3 celdas sobre la placa ARDIENTE
  hierve en ≤6 s; una a 3 celdas de HELANDO congela en ≤6 s; y a 12 celdas
  de distancia el efecto de ambas es ≤±10°C (el gradiente EXISTE — nada de
  habitaciones enteras congeladas).
- Las escrituras de temperatura pasan por el camino de la sim (el
  docblock de HeatPlate ya anota esa deuda: nada de `temp[]` a mano si
  existe vía; si no existe, crear `AlkahestSim.InyectarTemperatura(...)`
  con la misma disciplina de Paint).
- Ambas implementan `IMaquinaUsableRemota` (§1). Chapas legibles como
  siempre (APAGADA/TEMPLADA/ARDIENTE, APAGADA/FRESCA/HELANDO).

### 2c. Verificación de T

Compilación regla 53 + banco 6 escenarios antes/después (la conversión por
frentes toca el bucle de hornada: presupuesto +5% máx) + diagnóstico
headless de los criterios medibles de 2b Y un frente de calcinación:
sembrar carga fría sobre fondo caliente y medir que la conversión avanza
por filas (reportar ticks entre la primera y la última fila convertida —
debe ser ≥60 ticks, o el frente no se VE).

## 3. ENCARGO I — los ojos y el arco

Archivos de I: `Game/Termometro.cs` (**NUEVO**), `Sim/SimLevelBuilder.cs`,
`Game/AlkahestGameBootstrap.cs`, `Game/SemillaCero.cs`,
`Game/HintSystem.cs`, `Net/MaquinaSync.cs`, `Net/MaquinaReplica.cs`,
`Game/MaquinariaSprites.cs` (réplicas y sprite de placas si falta).

### 3a. El termómetro (la herramienta de validar)

- `Game/Termometro.cs`: la tecla **G** alterna el MODO TERMÓMETRO (G libre
  — verificar con grep de `*Key` igual que hizo el Cincel). Activo:
  readout vivo en °C junto al cursor (celda bajo el ratón,
  `CellGrid.RawToC`, redondeado al grado); clic izquierdo PINCHA una sonda
  (hasta 3, FIFO — la cuarta reemplaza la más vieja); cada sonda es una
  etiqueta viva en el mundo ("23°") que se actualiza con acumulador
  (~4 Hz), estilo UiStyles sobrio con acento por temperatura (frío
  azulado / ambiente neutro / caliente cálido). Clic derecho quita la
  sonda apuntada. Al salir del modo las sondas SIGUEN visibles (son
  instrumentos plantados, no un overlay del modo) — G otra vez para
  gestionarlas, H no las toca. Cero allocs por frame (labels cacheados,
  solo se reconstruyen al cambiar el grado).
- Con el modo activo el frasco/cincel NO actúan (mismo patrón de exclusión
  que Cincel.ModoActivo — leerlo antes; el Termometro respeta
  `DevPalette.IsOpen`, `UiStyles.EscribiendoTexto`, `JournalHud.Abierto`).
- En multi invitado: la temperatura NO se replica (solo mat) — el
  termómetro del invitado marca "—" con nota en chapa ("solo el anfitrión
  mide, por ahora") en vez de mentir con el ambiente local. Documentado.

### 3b. Las placas en el mundo

- `SimLevelBuilder`: sitios para las DOS placas en el cuarto íntimo (mi
  decisión de director: la de CALOR en la zona húmeda, junto a las pilas —
  hervir agua es su primer uso natural; la FRÍA en la alcoba de la
  columna, la zona más "de instrumentos" del taller). Plataforma/anclaje
  con el patrón soberano de siempre si su huella lo pide (son placas de
  suelo: probablemente solo necesitan suelo plano y registro de obra).
- Bootstrap: `SpawnHeatPlates`/`SpawnChillStone` VUELVEN a llamarse
  (llevan comentadas desde pt21/25 — regla 15 cumplida, hoy se
  des-bifurcan) en el taller de un jugador Y en el multi (anfitrión), con
  réplicas para invitados (tipos 11/12 de §1, chapa + E remoto vía la
  maquinaria pt43 que ya existe — I solo registra los tipos y el visual).
- En SEMILLA CERO: la placa de CALOR presente desde el beat 1 (Cesar:
  "podemos iniciar con la placa de calor"); la FRÍA aparece con el beat
  nuevo (3c). En el modo caótico: ambas desde el arranque.

### 3c. El beat del FRÍO en Semilla Cero

- Beat nuevo **entre PreguntaChispa y PreguntaEnsayo**: el Maestro:
  *"Todo lo tuestas. ¿Y si lo ENFRÍAS?"* → aparece/se destapa la placa
  fría (si su sitio queda en sala ya abierta, aparece con su obra tallada
  en caliente + aviso; decisión de I documentada) + pedido guiado:
  **"Tráeme HIELO — y apúrate, que el frío no espera a nadie."**
  (`OrderType.Guiado`, targetMat `MaterialId.Ice`, cantidad 8) — congelar
  agua en la placa, llevarla a la Tolva ANTES de que se derrita: la
  descongelación ES parte de la lección (si se derrite en el camino, el
  Maestro NO se burla dos veces igual: una línea de MaestroDice al primer
  fracaso, "se te derritió... el frío es paciencia y PRISA", edge-trigger
  como la ceniza del beat 4).
- La máquina de estados de SemillaCero gana el estado entre los dos
  existentes; HintSystem suma el consejo del frío a la lista curada.
- Idea de las 4+1 (enmienda 4): esto COMPLETA la idea "temperatura" con su
  mitad fría — documentarlo en el propio código.

## 4. HECHOS COMPARTIDOS

CLAUDE.md entero (7, 15, 48, 53, 54, 55). Determinismo intacto (las placas
escriben temperatura por la vía de la sim en el ANFITRIÓN; el termómetro
solo LEE). El arnés debe seguir compilando. Cero allocs. Español latino.
Compilación regla 53 (rig montado). Encargos paralelos: transitorios pt40.

## 5. DEFINICIÓN DE HECHO

- **T**: el tostado de una hornada se VE propagarse (≥2 s de frente); agua
  sobre ARDIENTE hierve en ≤6 s y junto a HELANDO congela en ≤6 s con
  gradientes medibles (≤±10°C a 12 celdas); banco dentro de presupuesto;
  las placas responden a E local y remoto.
- **I**: G da termómetro con sondas vivas en °C; las placas están en el
  mundo (los tres modos) con réplicas; Semilla Cero tiene el beat del
  hielo completo con su línea de fracaso; compila.
- Ambos: informe de datos con decisiones fuera de contrato EXPLÍCITAS y
  deudas. La verificación con ojos (congelar/descongelar/tostar con
  termómetro en pantalla) la hace Fable esta misma noche.
