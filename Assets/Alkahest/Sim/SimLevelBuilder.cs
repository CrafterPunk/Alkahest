namespace Alkahest.Sim
{
    /// <summary>
    /// EL PLANO DEL TALLER. Construye el nivel entero por código (cero assets)
    /// y es la ÚNICA FUENTE DE VERDAD de dónde está cada cosa: Game/ lee estas
    /// constantes para colocar placas, piedra fría, grifos, estantería y Tolva.
    /// Antes AlkahestGameBootstrap duplicaba a mano media docena de números
    /// ("si el layout cambia, hay que actualizar también estos") — esa
    /// duplicación se ha eliminado.
    ///
    /// REDISEÑO (playtest 4: "el muñeco y los vuelos largos cansan, distancias
    /// innecesarias, los caños llevan el juego al borde inferior, las
    /// reacciones pequeñitas, pantallón negro"). El taller pasa de "cuarto
    /// oscuro con huecos" a ESPACIO DE TRABAJO compacto sobre la grilla nueva
    /// de 256x144:
    ///
    ///   y=143 ┌───────────────────── muro / vigas del techo ─────────────────┐
    ///         │                                                              │
    ///   y=108 │            (aire de vuelo — pared de fondo con ladrillo)      │
    ///   y= 96 │  ╔═ BANDEJA FRÍA ═╗            ╔═ ESTANTE DE REDOMAS ═╗       │
    ///   y= 88 │  ║ x 36..87       ║            ║ x 104..168 (losa)    ║       │
    ///         │                                                              │
    ///   y=102 │ ░ grifo AZOTH (sellado hasta jornada 2; boquilla x 13)      │
    ///   y= 92 │ ░ grifo NUTRIENTE                                            │
    ///   y= 82 │ ░ grifo ACEITE          COLUMNA DE GRIFOS (pilar x 1..8)      │
    ///   y= 72 │ ░ grifo ARENA                                                │
    ///   y= 62 │ ░ grifo AGUA                                                 │
    ///   y= 57 │ ┌── PILA DE RECOGIDA ──┐                                     │
    ///   y= 53 │ │ x 6..59 (int. 9..56) │  ┌── CUBA A ──┐  ┌── CUBA B ──┐  ▄▄▄ │
    ///         │ │                      │  │ x 72..129  │  │ x 138..195 │  ▐T▌ │
    ///   y= 40 │ └──────────────────────┘  │ int.75..126│  │ int.141..192│ ▐O▌ │
    ///   y= 14 │ ▓▓▓ MESETA / BANCO ▓▓▓▓▓  └────────────┘  └────────────┘ ▓L▓ │
    ///   y=  0 └───────────────── suelo de piedra (y 0..13) ──────────────────┘
    ///           x=0                                                     x=255
    ///
    /// Criterio de composición: ningún salto entre estaciones supera ~75
    /// celdas (29% del ancho); el borde inferior de la pantalla ya no es zona
    /// de juego (el suelo sube a y&lt;14 y los grifos vierten a media altura);
    /// las dos cubas son las protagonistas del encuadre.
    /// </summary>
    public static class SimLevelBuilder
    {
        // =================================================================
        // NIVEL BASE
        // =================================================================

        /// <summary>Filas 0..FloorTop de piedra maciza. El suelo SUBE (antes 8) para que la acción no viva pegada al borde inferior de la pantalla.</summary>
        public const int FloorHeight = 14;
        public const int FloorTop = FloorHeight - 1; // 13

        /// <summary>Grosor de pared estándar de cubas/bandejas.</summary>
        public const int WallThickness = 3;

        // =================================================================
        // CUBAS CENTRALES (protagonistas)
        // =================================================================

        public const int VatWidth = 58;
        public const int VatHeight = 40;
        public const int VatAX0 = 72;
        public const int VatBX0 = 138;

        /// <summary>Fila donde vive la placa calefactora de una cuba (la última de su suelo de piedra).</summary>
        public const int VatPlateRow = FloorHeight + WallThickness - 1; // 16

        public static int VatInteriorX0(int vatX0) => vatX0 + WallThickness;
        public static int VatInteriorX1(int vatX0) => vatX0 + VatWidth - 1 - WallThickness;
        /// <summary>Primera fila útil del interior de una cuba (justo encima de su suelo).</summary>
        public const int VatInteriorY0 = FloorHeight + WallThickness;      // 17
        public const int VatInteriorY1 = FloorHeight + VatHeight - 1;      // 53 (labio)

        // =================================================================
        // BANCO DE TRABAJO IZQUIERDO: meseta + pila de recogida + columna de grifos
        // =================================================================

        /// <summary>Meseta maciza sobre la que se apoya la pila de recogida.</summary>
        public const int BenchX0 = 1;
        public const int BenchX1 = 64;
        public const int BenchTopY = 39; // última fila maciza de la meseta

        /// <summary>Pila de recogida: cubeta ancha y poco profunda donde caen TODOS los grifos.</summary>
        public const int BasinX0 = 6;
        public const int BasinWidth = 54;   // x 6..59
        public const int BasinY0 = 40;
        public const int BasinHeight = 18;  // labio en y 57
        public const int BasinInteriorX0 = BasinX0 + WallThickness;                    // 9
        public const int BasinInteriorX1 = BasinX0 + BasinWidth - 1 - WallThickness;   // 56
        public const int BasinInteriorY0 = BasinY0 + WallThickness;                    // 43
        public const int BasinInteriorY1 = BasinY0 + BasinHeight - 1;                  // 57

        /// <summary>Pilar de piedra al que se atornillan los grifos, en columna vertical compacta.</summary>
        public const int TapPillarX0 = 1;
        public const int TapPillarX1 = 8;
        public const int TapPillarTopY = 104;

        /// <summary>Celda de anclaje de los grifos. El caño sale EN VOLADIZO 5 celdas a la derecha (boquilla y caudal en x 13, bien fuera de la piedra del pilar y sobre la pila de recogida).</summary>
        public const int TapMountX = 8;
        /// <summary>Altura del grifo más bajo y separación vertical entre grifos consecutivos.</summary>
        public const int TapFirstY = 62;
        /// <summary>Separación vertical entre grifos: 10 celdas (1 unidad de mundo). Suficiente para que el árbitro de foco (Game/MachineFocus.cs) elija sin ambigüedad el grifo que tienes delante, y suficientemente compacta para leerse como una sola batería de caños.</summary>
        public const int TapStepY = 10;

        // =================================================================
        // ESTANTES SUPERIORES: bandeja fría (izq.) y estantería de redomas (der.)
        // =================================================================

        /// <summary>Bandeja fría: cubeta poco profunda con la piedra gélida bajo su suelo. Aquí se cristaliza y se congela.</summary>
        public const int ChillTrayX0 = 36;
        public const int ChillTrayWidth = 52;   // x 36..87
        public const int ChillTrayY0 = 88;
        public const int ChillTrayHeight = 9;   // labio en y 96
        public const int ChillTrayInteriorX0 = ChillTrayX0 + WallThickness;                        // 39
        public const int ChillTrayInteriorX1 = ChillTrayX0 + ChillTrayWidth - 1 - WallThickness;   // 84
        public const int ChillTrayInteriorY0 = ChillTrayY0 + WallThickness;                        // 91
        /// <summary>Fila donde vive la piedra fría (última de su suelo).</summary>
        public const int ChillPlateRow = ChillTrayY0 + WallThickness - 1;                          // 90

        /// <summary>Losa de piedra sobre la que se apoya la estantería de redomas (Game/StorageRack.cs).</summary>
        public const int RackX0 = 104;
        public const int RackX1 = 168;
        public const int RackY0 = 88;
        public const int RackHeight = 3;
        /// <summary>Primera fila libre sobre la losa: la base de las redomas.</summary>
        public const int RackTopY = RackY0 + RackHeight; // 91

        // =================================================================
        // TOLVA DEL MAESTRO (ver Game/DeliveryChute.cs)
        // Contrafuerte de piedra del muro derecho con un pozo excavado y
        // abierto por arriba. Reubicado y reescalado a la grilla nueva; el
        // diseño (nicho + marco dorado + flecha) se conserva.
        // =================================================================

        /// <summary>Primera columna del contrafuerte de piedra que aloja la Tolva.</summary>
        public const int ChuteWallX0 = 204;
        /// <summary>Primera columna hueca de la boca (interior del pozo).</summary>
        public const int ChuteMouthX0 = 216;
        /// <summary>Última columna hueca de la boca (interior del pozo).</summary>
        public const int ChuteMouthX1 = 237;
        /// <summary>Primera fila hueca del pozo (justo encima de su suelo de piedra).</summary>
        public const int ChuteMouthY0 = 44;
        /// <summary>Última fila hueca: por encima ya es aire libre, es el labio de la boca.</summary>
        public const int ChuteMouthY1 = 72;

        /// <summary>
        /// (fix playtest 7) Cuántas filas del FONDO del pozo son la "boca que
        /// traga": Game/DeliveryChute.cs solo consume en [ChuteMouthY0 ..
        /// ChuteMouthY0+ChuteSillRows-1], no en todo el pozo. Antes se
        /// consumía cualquier celda del pozo el mismo tick en que entraba, así
        /// que el material se evaporaba pegado al labio (arriba del todo) y el
        /// resto del hueco (28 filas) se veía como un agujero negro inerte que
        /// nunca recibía nada. Con solo el fondo tragando, lo que se vierte
        /// CAE por gravedad a través del resto del pozo (aire) antes de
        /// desaparecer, que es lo que hace legible "esto es un conducto, no un
        /// boquete".
        /// </summary>
        public const int ChuteSillRows = 3;

        private const int ChuteBaseHeight = 28; // alto del zócalo ancho del contrafuerte

        // =================================================================
        // CONSTRUCCIÓN
        // =================================================================

        public static void BuildTestLevel(CellGrid grid)
        {
            FillBorder(grid);
            FillFloor(grid, FloorHeight);
            BuildWorkbench(grid);
            BuildVats(grid);
            BuildUpperShelves(grid);
            BuildDeliveryNiche(grid);
        }

        /// <summary>
        /// Banco de trabajo izquierdo: meseta maciza a media altura, pila de
        /// recogida encima (donde vierten TODOS los grifos) y pilar vertical
        /// al que se atornilla la columna de grifos. Antes los grifos colgaban
        /// del muro a alturas 40/70/110/150 y regaban el suelo hasta el borde
        /// inferior de la pantalla; ahora todo el caudal cae en un mismo sitio,
        /// a la altura de los ojos.
        /// </summary>
        private static void BuildWorkbench(CellGrid grid)
        {
            DrawSolidRect(grid, BenchX0, FloorHeight, BenchX1 - BenchX0 + 1, BenchTopY - FloorHeight + 1, MaterialId.Stone);
            DrawUShape(grid, BasinX0, BasinY0, BasinWidth, BasinHeight, WallThickness);
            DrawSolidRect(grid, TapPillarX0, BasinY0, TapPillarX1 - TapPillarX0 + 1, TapPillarTopY - BasinY0 + 1, MaterialId.Stone);
        }

        /// <summary>Las DOS cubas grandes centrales: más anchas y profundas que las tres antiguas, y el centro del encuadre.</summary>
        private static void BuildVats(CellGrid grid)
        {
            DrawUShape(grid, VatAX0, FloorHeight, VatWidth, VatHeight, WallThickness);
            DrawUShape(grid, VatBX0, FloorHeight, VatWidth, VatHeight, WallThickness);
        }

        /// <summary>Bandeja fría (cubeta poco profunda) y losa de la estantería de redomas.</summary>
        private static void BuildUpperShelves(CellGrid grid)
        {
            DrawUShape(grid, ChillTrayX0, ChillTrayY0, ChillTrayWidth, ChillTrayHeight, WallThickness);
            DrawSolidRect(grid, RackX0, RackY0, RackX1 - RackX0 + 1, RackHeight, MaterialId.Stone);
        }

        /// <summary>
        /// Contrafuerte de piedra pegado al muro derecho con un pozo vertical
        /// excavado y abierto por arriba: la boca de la Tolva. Zócalo ancho
        /// abajo + torre más estrecha arriba para que se lea como arquitectura
        /// del taller y no como un rectángulo suelto.
        /// </summary>
        public static void BuildDeliveryNiche(CellGrid grid)
        {
            // Zócalo: del suelo hasta la altura de las cubas.
            DrawSolidRect(grid, ChuteWallX0, FloorHeight, CellGrid.W - ChuteWallX0, ChuteBaseHeight, MaterialId.Stone);

            // Torre: algo más estrecha, hasta el labio de la boca.
            int torreX0 = ChuteWallX0 + 4;
            int torreY0 = FloorHeight + ChuteBaseHeight;
            DrawSolidRect(grid, torreX0, torreY0, CellGrid.W - torreX0, ChuteMouthY1 + 1 - torreY0, MaterialId.Stone);

            // Pozo excavado (queda abierto por arriba: es por donde se vierte).
            DrawSolidRect(grid, ChuteMouthX0, ChuteMouthY0,
                ChuteMouthX1 - ChuteMouthX0 + 1, ChuteMouthY1 - ChuteMouthY0 + 1, MaterialId.Empty);
        }

        // ---------------------------------------------------------------------------------
        private static void FillBorder(CellGrid grid)
        {
            for (int x = 0; x < CellGrid.W; x++)
            {
                grid.SetCell(x, 0, MaterialId.Stone);
                grid.SetCell(x, CellGrid.H - 1, MaterialId.Stone);
            }
            for (int y = 0; y < CellGrid.H; y++)
            {
                grid.SetCell(0, y, MaterialId.Stone);
                grid.SetCell(CellGrid.W - 1, y, MaterialId.Stone);
            }
        }

        private static void FillFloor(CellGrid grid, int floorHeight)
        {
            for (int y = 0; y < floorHeight; y++)
            {
                for (int x = 1; x < CellGrid.W - 1; x++)
                {
                    grid.SetCell(x, y, MaterialId.Stone);
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Primitivas de dibujo, públicas para reuso por niveles futuros.
        // ---------------------------------------------------------------------------------

        /// <summary>Dibuja una cubeta en forma de U: paredes laterales de `wallThickness` de ancho y suelo también de `wallThickness`, abierta por arriba.</summary>
        public static void DrawUShape(CellGrid grid, int x0, int y0, int width, int height, int wallThickness)
        {
            int x1 = x0 + width - 1;
            int yTop = y0 + height - 1;

            // Suelo de la cubeta.
            for (int y = y0; y < y0 + wallThickness; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Stone);
                }
            }

            // Paredes laterales.
            for (int y = y0; y <= yTop; y++)
            {
                for (int t = 0; t < wallThickness; t++)
                {
                    if (CellGrid.InBounds(x0 + t, y)) grid.SetCell(x0 + t, y, MaterialId.Stone);
                    if (CellGrid.InBounds(x1 - t, y)) grid.SetCell(x1 - t, y, MaterialId.Stone);
                }
            }
        }

        /// <summary>Rectángulo sólido relleno del material indicado.</summary>
        public static void DrawSolidRect(CellGrid grid, int x0, int y0, int width, int height, byte materialId)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                for (int x = x0; x < x0 + width; x++)
                {
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, materialId);
                }
            }
        }
    }
}
