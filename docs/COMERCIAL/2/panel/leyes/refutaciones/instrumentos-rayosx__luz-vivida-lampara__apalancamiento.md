# Refutación · «Luz vivida: lámpara» (lente instrumentos-rayosx) · criterio: apalancamiento

**Veredicto: viable con ajuste.** Pintar la pantalla con `luz` es observabilidad honesta: revela dos cruces que ya existen y nadie ve. La lámpara portátil no cruza leyes: las descuenta. El candidato empaqueta ambas y cobra el apalancamiento de la primera por la segunda.

## 1. La lámpara cruza una ley, y ya estaba cruzada

`luz` tiene cinco lectores en toda la física y los cinco son plantas: germinar (`SimStepper.Laboratorio.cs:689`), brotar solo (`:704`) y elegir hacia dónde crece la punta (`:870-873`). Ninguna otra ley la lee; la oscuridad no seca ni mata (la muerte es `savia == 0` durante 40 visitas, `:886-900`). «La lámpara es física» significa solo esto: donde está el jugador, germina. Los otros cuatro «cruces» (humo, fuego, agua, vidrio) no mueven un bit de la simulación: son el mismo campo pintado, y el del vidrio depende de otra propuesta (`LabLuzDesde :1272` devuelve 0 para sólidos).

Esa única relación ya existe con una fuente gratis: `Hogar` emite 255 (`EmiteLuz`, `LabMateriales.cs:110`), no consume, no ahúma y está siempre encendido (`LabHogar :917-946`). La lámpara es un Hogar sin calor y con ruedas. «Solo da luz: es honesta», dice el candidato; en este sustrato, una fuente sin precio en otra ley es la definición de capa.

## 2. La lámpara resta decisiones

El fracaso del huerto es el cruce más caro que el laboratorio produjo sin que nadie lo escribiera: la boca ilumina 7 de 73 caras (R148) y el alambique que riega ahoga (R135). «Abrir la boca sabiendo que cambia el clima» es hoy un dilema real; con una fuente móvil sin humo ni combustible, desaparece. Peor: `LabLuz` resetea a 0 cada 16 ticks (`:1184-1194`), la luz no tiene memoria, así que la planta crece mientras el jugador esté al lado. El huerto pasa a «quédate aquí»: el trabajo manual que el sustrato señala como carencia. De las cuatro decisiones nuevas, «dónde poner el fuego» ya existe en el campo y solo se hace visible; «de noche» no existe (boca constante, `SimLevelBuilder.Laboratorio.cs:164`); «abrir la boca» la lámpara la anula; y el «radio propio» es falso: `luz` es un campo único, si A alumbra, B lo ve y la semilla germina para ambos; asimetría exige un velo por cliente fuera de la sim. Decisiones netas: cero, y una existente destruida.

## 3. Tuning escondido (8 declarado; real 5)

- Suelo de penumbra 25 %: un número que es toda la sensación y solo un playtest decide.
- `LuzDecayAire = 8` se afinó para plantas (`LabParams.cs:290`). Con boca en x118-124 y hogar en x150-158, las galerías del agua (el sistema 5/5) quedan a negro. Hacer jugable la oscuridad empuja a bajar `dAire`, parámetro de simulación: cambia dónde germinan, agranda la ventana H5 y mueve hashes. «Sin lámparas los hashes no se mueven» es verdad del diff y falso del proyecto.
- Cuatro barridos de una pasada (`:1230-1266`): vertical y luego horizontal. Un pasillo con dos giros es negro tras el segundo; se leerá como bug y pedirá retocar `LabLuz`, que es física.
- La textura oscurece celdas, no sprites (muñeco y atrezo flotan iluminados); y `SimSync` solo replica `mat[]` (`CellGrid.cs:179`): en ruta A hay que sincronizar las lámparas.

La verificación «lámpara sobre lecho → nacidas > base» mide la humedad tanto como la luz (`MedirLecho`: 22/36 y 0/36 columnas aptas en r148 y r150). Puede fallar por agua con la lámpara bien. No es diagnóstica.

## 4. Ajuste mínimo con más apalancamiento

1. **Render sin lámpara** (2-3 días): `_grid.luz` como textura R8 cada `LuzCadaTicks` multiplicada en shader con el suelo, o una `VistaLaboratorio.Ojo` en `SimRenderer.Laboratorio.cs:236` que oscurezca por `(255 − luz)`. Cero física, siete hashes intactos por construcción.
2. **Las fuentes son las que ya cobran**: fuego, brasa, hogar, cielo. «Encender para ver» tiene precio: el humo del fuego que alumbra el lecho es el que lo oscurece (R148). Ese sí es un cruce. Fuente portátil solo si paga (una brasa que se apaga); hoy el frasco rechaza el fuego (`Flask.cs:528`): verbo nuevo, no una línea.
3. Donde está el juego: que el vidrio transmita (`VidrioVerde` en `LabLuzDesde :1272` + `LuzDecayVidrio` en `:1276`). Una línea y un parámetro cruzan horno × luz × plantas × humedad × humo: la ventana deja entrar el cielo sin dejar entrar el vapor ni salir el humo. El hash del horno cambia y debe cambiar.

## Valores corregidos (candidato tal como está)

Apalancamiento **4** (revela dos cruces existentes; la lámpara los descuenta). Tuning **5**. Coste **2 semanas** (material multiplicativo, lámpara como entrada de sim, sincronía en ruta A, playtest del suelo y de `dAire`). Con el ajuste (render + vidrio, sin lámpara): apalancamiento 7, tuning 8, 1 semana.
