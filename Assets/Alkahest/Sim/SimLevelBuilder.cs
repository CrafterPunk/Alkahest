namespace Alkahest.Sim
{
    /// <summary>
    /// Construye el nivel de pruebas M1 directamente en código (sin
    /// assets): un borde de Stone, un suelo, tres "vats" (cubetas en U de
    /// piedra) para experimentar con líquidos/powders, y un par de
    /// estantes altos. Público para que niveles futuros puedan reusar
    /// las primitivas de dibujo.
    /// </summary>
    public static class SimLevelBuilder
    {
        public static void BuildTestLevel(CellGrid grid)
        {
            FillBorder(grid);
            FillFloor(grid, 8);
            BuildVats(grid);
            BuildShelves(grid);
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

        /// <summary>Tres cubetas en U evenly-spaced sobre el suelo, para pruebas de líquidos/powders.</summary>
        private static void BuildVats(CellGrid grid)
        {
            const int vatWidth = 56;
            const int vatHeight = 34;
            const int wallThickness = 3;
            const int floorHeight = 8;
            const int vatCount = 3;

            int totalWidth = vatWidth * vatCount;
            int gap = (CellGrid.W - totalWidth) / (vatCount + 1);

            for (int i = 0; i < vatCount; i++)
            {
                int x0 = gap + i * (vatWidth + gap);
                DrawUShape(grid, x0, floorHeight, vatWidth, vatHeight, wallThickness);
            }
        }

        /// <summary>Dos pequeños estantes de piedra en altura, para probar caídas/salpicaduras.</summary>
        private static void BuildShelves(CellGrid grid)
        {
            DrawSolidRect(grid, 70, 90, 46, 3, MaterialId.Stone);
            DrawSolidRect(grid, 250, 132, 58, 3, MaterialId.Stone);
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
