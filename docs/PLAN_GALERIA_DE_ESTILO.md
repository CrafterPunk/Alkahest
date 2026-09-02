# LA GALERÍA DE ESTILO — plan del sandbox de arte (R126, planificación)

*(Idea de Cesar, 2/09/2026: «el prólogo no es el lugar óptimo para tomar
decisiones de estilo; es momento de crear un sandbox… con acceso tipo dev
para agregar fogatas, elementos, máquinas», mientras él produce el arte
(texturas y deco de `DIRECCION_V2` §4.2). Este documento es el PLAN acordable:
metas 1/2/3 y el esquema de áreas (`docs/ref/esquema_galeria.png`), para
cruzarlo con el mini-mapa que Cesar está dibujando. Nada implementado aún.)*

## 1. QUÉ ES (y qué no es)

Un MODO nuevo, fuera del juego, con un mapa DISEÑADO para juzgar imagen:
texturas juntas, luz, escala, costuras, deco. Es la **carta de ajuste** del
juego — como la de los televisores: un solo lugar donde todos los casos
visuales están a la vista y siempre en el mismo sitio, para que dos versiones
de arte se comparen con capturas idénticas.

No es: un nivel del juego (jamás se embarca en la demo), un editor de niveles,
ni un cuarto modo de gameplay. No tiene red, ni guardado, ni encargos, ni
Maestro. Regla: **nada de la Galería migra al juego sin su propia ronda.**

## 2. DÓNDE VIVE (para no romper nada)

- **Mismo patrón que la Fundación**: un flag estático `ModoGaleria` en
  `AlkahestGameBootstrap`, excluyente con `ModoFundacion`/`ModoSemillaCero`,
  reseteado en TODOS los caminos (regla 59 — el flag pegado construye el
  universo equivocado). El bootstrap, al verlo, salta a `SpawnGaleria()` y no
  spawnea nada del juego normal.
- **Mismo mundo, mismo motor**: la misma escena `AlkahestLab`, la misma sim
  768×288, la misma piel de roca, el mismo muñeco. Lo único distinto es EL
  PLANO (un tallado propio, `GaleriaLevelBuilder` estilo `SimLevelBuilder`)
  y qué se spawnea. Así lo que se ve en la Galería es EXACTAMENTE lo que se
  vería en el juego — si usara otra escena o cámara, las conclusiones
  mentirían.
- **Entrada**: menú «Ten Thousand Years/7. GALERÍA DE ESTILO (Play)» (fija el
  flag y entra a Play), siguiendo la numeración 1–6 existente. En build no
  hay entrada (o queda tras un atajo dev): no le llega al jugador.
- **El catálogo (el "acceso tipo dev")**: una ventana IMGUI hermana de
  `Dev/DevPalette` (que ya pinta materiales — se reutiliza entera), con una
  pestaña nueva de COLOCABLES: clic en la lista, clic en el mundo. Id de
  ventana constante, teclas con las guardas de la regla 12.

## 3. LAS ÁREAS (mi esquema — cruzar con el mini-mapa de Cesar)

Ver `docs/ref/esquema_galeria.png`. Nueve, conectadas por corredores para
recorrerla JUGANDO (P7: lo que se juzga se juzga a 80 celdas, en movimiento):

1. **CUEVA ÍNTIMA** (spawn) — cámara pequeña de techo bajo (~18 celdas) con
   fogata central y deco densa: donde se juzga la luz de fuego y el "prólogo
   contenido" de la V2 §5.2 sin construirlo.
2. **PATIO DE FUEGO** — fogata, hoyo de cocción, horno con brasa, antorchas,
   humo subiendo: la luz con causa y la pátina.
3. **LA NAVE** — bóveda de ~105 celdas con el depósito (19c) y el crisol
   TORREANDO (la jerarquía sagrada de §3.5): escala, parallax, planos.
4. **PARED DE JUNTAS** — franjas verticales de piso estructural, mortero,
   cerámica y vidrio CONTRA la roca: todas las costuras P2 en un metro.
5. **EL POZO** — vertical de ~210 celdas con cascada y repisas: la luz que
   baja, la caída, el vuelo.
6. **POZA Y HUMEDAD** — agua grande, goteo del techo, musgo: el banco de la
   futura memoria de humedad.
7. **EL VANO** — boca al exterior con cielo y 3 planos de fondo: el banco del
   fondo evolutivo y de la luz fría.
8. **PENDIENTES Y SILUETA** — escalera de 1 celda, rampas 45°, repisa fina,
   guijarros, túnel bajo: la silueta del terreno y, llegado el día, el banco
   de la decisión de locomoción (V2 §6.1).
9. **EL TERRARIO** — cubetas en fila con CADA material vertible contra la
   roca: la regla P1 (la materia no cambia de color) auditada de un vistazo.

## 4. LAS METAS

> **Estado R127b: METAS 1 y 1b HECHAS + primer peldaño de la 2, verificado
> en vivo.** Además, por dirección de Cesar, la Galería se decluttereó
> («componer, no explorar»): solo la fogata humilde pre-puesta; máquinas
> (crisol, alambique, prensa, banco de chispa) se colocan/quitan desde el
> catálogo (quitar = a la BODEGA); y el hot-load ya funciona: R recarga
> `Galeria/roca_superficie.png` sobre la piel entera al instante.

> **Estado R127: META 1 HECHA y verificada en vivo** (9 áreas talladas,
> curador con G, Ctrl+1..9, F10 = ronda de capturas en `Galeria/capturas/`,
> botón «galería de estilo» en el título). El curador quedó así, por pedido
> de Cesar («agregar/quitar/duplicar sin ensuciar pantalla»): CERRADO no se
> ve nada; abierto, clic coloca, clic derecho quita, C+clic copia un parche
> de materia (ESTAMPA) y el clic lo duplica, -/+ cambia el radio, y MOVER
> sigue siendo la mudanza de siempre. Las máquinas reales entran en la Meta
> 1b (su Init ancla la cota al plano del taller clásico; ver §4).

**META 1 — EL LIENZO (la que yo puedo adelantar ya, sin esperar el arte):**
modo + plano tallado con las 9 áreas + spawn + catálogo v1 con lo que ya
existe: fogata (brasas reales), antorcha (Fire estable), montón/chorro de
cada material (DevPalette), colocar/quitar las máquinas actuales (crisol,
depósito, banco, alambique…), borrar colocable. Teletransporte Ctrl+1..9
entre áreas. Y la pieza que más vale: **el botón de CAPTURAS** — una tecla
recorre las 9 áreas y guarda 9 PNG con nombre y fecha, siempre el mismo
encuadre, para comparar versiones de arte lado a lado.

**META 2 — EL PROBADOR DE ARTE (para cuando Cesar entregue texturas):**
hot-load sin recompilar: la Galería lee `Galeria/` (carpeta junto al
proyecto) y si hay `roca_superficie.png` / `roca_masa.png` / sprites de deco,
los usa; tecla R recarga. Conmutador A/B entre dos juegos de texturas para
alternar en vivo (la comparación que decide). Perillas IMGUI de la piel
(umbral, anchos de banda, fuerza de tinta, densidad de deco) con "copiar
valores al portapapeles" para que yo los selle en código después.

**META 3 — EL BANCO DE LUZ (cuando toque el experimento de la V2 §3):**
el 2D Renderer en copia, y en el catálogo: luz de fogata, luz del vano,
slider de ambiente y de "hora", bloom on/off. Las mismas 6 capturas del
protocolo de R123 §5 salen del botón de capturas de la Meta 1. Con esto la
"tarde de la ventana iluminada" se corre ENTERA dentro de la Galería.

## 5. RIESGOS CORTOS

La Galería no debe crecer hasta ser un segundo juego (es una carta de ajuste;
si un área no responde una pregunta de estilo, sobra). El flag de modo sigue
la regla 59 o contaminará al prólogo. El hot-load lee SOLO de la carpeta
`Galeria/` y jamás de Resources del juego (lo sellado entra por ronda). Y las
conclusiones de luz no valen hasta que la build con 2D Renderer compile
(regla del playtest 2).

## 6. QUÉ SIGUE

Cesar cruza este esquema con su mini-mapa y ajustamos las áreas; con su OK,
la Meta 1 es una ronda de trabajo mía (~una sesión) mientras él pinta. Las
metas 2 y 3 se disparan cuando llegue el arte y cuando toque la luz,
respectivamente.
