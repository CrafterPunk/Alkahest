using System;

namespace Alkahest.Sim
{
    /// <summary>
    /// (R130) EL PLANO DEL LABORATORIO DE LEYES — docs/LAB/DISENO_LABORATORIO.md §7.
    /// Parte de SimLevelBuilder (partial). Ocupa x 30..430 y toda la altura;
    /// el resto del mundo es roca maciza para que Cesar decida si la explota.
    /// Coordenadas: y=0 es el FONDO (idx+W = arriba). Todas las cotas de este
    /// plano viven aquí (regla 3: nadie las copia a mano).
    ///
    /// Zonas (ver el esquema del diseño):
    ///   SALA DEL HOGAR      x60-170  y176-214  spawn (118,182), Hogar x150-158 y176-179,
    ///                       montículo de arena, veta de arcilla húmeda en el muro oeste,
    ///                       dos bolsillos ocultos en la roca.
    ///   POZO                x62-77   y150-177  baja de la sala al arroyo.
    ///   GALERÍA DEL ARROYO  x36-430  y128-152  piso escalonado 138→130 hacia el este,
    ///                       MANANTIAL en el muro oeste (x31-35 y139-145),
    ///                       fisura de arena (x192-202 y111-134) hasta la cámara profunda,
    ///                       grava en el lecho, POZA x250-330 (fondo 122, prellenada),
    ///                       grieta x336-343 a la cámara profunda, pozo del SUMIDERO x416-429.
    ///   CÁMARA PROFUNDA     x120-360 y60-110   con cubeta x140-180 y52-59.
    ///   CHIMENEA            x140-149 y215-244  de la sala a la cámara alta.
    ///   CÁMARA ALTA         x100-190 y245-272  fría, sedimento seco en el piso.
    ///   BOCA DEL CIELO      x118-124 y273-286  luz.
    /// </summary>
    public static partial class SimLevelBuilder
    {
        public const int LabSpawnX = 118, LabSpawnY = 182;

        /// <summary>Anclas de teletransporte del panel (Ctrl+1..6).</summary>
        public static readonly int[] LabAnclaX = { 118, 200, 300, 200, 145, 420 };
        public static readonly int[] LabAnclaY = { 190, 142, 128, 85, 258, 118 };
        public static readonly string[] LabAnclaNombre = {
            "sala del hogar", "galería del arroyo", "la poza", "cámara profunda", "cámara alta", "el sumidero" };

        public static void BuildLaboratorioDeLeyes(CellGrid grid)
        {
            ObraDelTaller.Clear();
            ReservasDelPlano.Clear();
            FillWorldStone(grid);

            void Aire(int x0, int y0, int x1, int y1) => DrawSolidRect(grid, x0, y0, x1 - x0 + 1, y1 - y0 + 1, MaterialId.Empty);
            void Bloque(int x0, int y0, int x1, int y1, byte m) => DrawSolidRect(grid, x0, y0, x1 - x0 + 1, y1 - y0 + 1, m);
            void Clima(int x0, int y0, int x1, int y1, byte raw)
            {
                const int Borde = 6;
                for (int y = y0 - Borde; y <= y1 + Borde; y++)
                    for (int x = x0 - Borde; x <= x1 + Borde; x++)
                    {
                        if (!CellGrid.InBounds(x, y)) continue;
                        int dx = x < x0 ? x0 - x : (x > x1 ? x - x1 : 0);
                        int dy = y < y0 ? y0 - y : (y > y1 ? y - y1 : 0);
                        int d = dx > dy ? dx : dy; // 0 dentro, hasta Borde fuera
                        int i = CellGrid.Idx(x, y);
                        int objetivo = raw + (CellGrid.AmbientRaw - raw) * d / Borde;
                        if (d == 0 || objetivo < grid.ambient[i]) grid.ambient[i] = (byte)objetivo;
                    }
            }
            void Campo(int x0, int y0, int x1, int y1, byte humedad, byte carga)
            {
                for (int y = y0; y <= y1; y++)
                    for (int x = x0; x <= x1; x++)
                    {
                        if (!CellGrid.InBounds(x, y)) continue;
                        int i = CellGrid.Idx(x, y);
                        grid.humedad[i] = humedad; grid.carga[i] = carga;
                    }
            }

            // ---- SALA DEL HOGAR ----
            Aire(60, 176, 170, 214);
            Bloque(150, 176, 158, 179, MaterialId.Hogar);
            for (int dx = -11; dx <= 11; dx++)
            {
                int h = 7 - Math.Abs(dx) * 7 / 12; // montículo de arena (cono)
                for (int k = 0; k < h; k++) grid.SetCell(89 + dx, 176 + k, MaterialId.Sand);
            }
            Bloque(44, 182, 59, 198, MaterialId.Arcilla);   // veta de arcilla en el muro oeste (cara expuesta en x=59)
            Campo(44, 182, 59, 198, 160, 0);                // húmeda: tallarla da barro
            Aire(180, 192, 196, 204);                       // bolsillo oculto al este (10 celdas de roca desde x=171)
            Aire(100, 222, 112, 232);                       // bolsillo oculto sobre el techo

            // ---- POZO sala → arroyo ----
            Aire(62, 150, 77, 177);

            // ---- GALERÍA DEL ARROYO: cavidad y piso escalonado ----
            Aire(36, 128, 430, 152);
            int[] tramoX = { 36, 110, 190, 250, 330, 380, 431 };
            int[] pisoTop = { 138, 136, 134, 121, 132, 130 }; // última fila de roca por tramo; el tramo 250-330 es la POZA (fondo 122)
            for (int t = 0; t < pisoTop.Length; t++)
                for (int x = tramoX[t]; x < tramoX[t + 1]; x++)
                    for (int y = 128; y <= pisoTop[t]; y++) grid.SetCell(x, y, MaterialId.Stone);
            Bloque(31, 139, 35, 145, MaterialId.Manantial);  // emite hacia x=36
            Bloque(192, 111, 202, 134, MaterialId.Sand);      // fisura de arena: del lecho a la cámara profunda
            Bloque(350, 133, 372, 135, MaterialId.Grava);     // banco de grava en el lecho
            Bloque(251, 122, 329, 123, MaterialId.Sedimento); // fondo de la poza: sedimento húmedo
            Campo(251, 122, 329, 123, 255, 0);
            Bloque(251, 124, 329, 128, MaterialId.Water);     // poza medio llena, turbia
            Campo(251, 124, 329, 128, 255, 60);
            Aire(336, 111, 343, 132);                         // grieta aguas abajo de la poza → cámara profunda
            Aire(416, 100, 429, 131);                         // pozo del sumidero
            Bloque(416, 96, 429, 99, MaterialId.Sumidero);

            // ---- CÁMARA PROFUNDA ----
            Aire(120, 60, 360, 110);
            Aire(140, 52, 180, 59);                           // cubeta en el piso

            // ---- CHIMENEA, CÁMARA ALTA y BOCA DEL CIELO ----
            Aire(140, 215, 149, 244);
            Aire(100, 245, 190, 272);
            Bloque(100, 245, 136, 246, MaterialId.Sedimento); // sedimento SECO en el piso (a los lados de la boca de la chimenea)
            Bloque(153, 245, 190, 246, MaterialId.Sedimento);
            Aire(118, 273, 124, 286);
            LabParams.LuzCieloX0 = 118; LabParams.LuzCieloX1 = 124;

            // ---- CLIMA DEL LABORATORIO (CellGrid.ambient por celda) ----
            // La regla 31 retiró el clima por zona DEL JUEGO; el array quedó
            // "para el clima que cree el jugador". Aquí es un experimento: sin
            // una zona más fría que otra, el vapor no condensa en ningún sitio
            // hasta saturar la cueva entera (física correcta, juego invisible).
            // Cámara alta y boca del cielo a 8 °C (raw 64), cámara profunda a
            // 12 °C (raw 66), el resto a los 20 °C de siempre. Degradado de 6
            // celdas en las fronteras para no crear escalones (ver el análisis
            // (c) del docblock de DiffuseTemperature).
            Clima(90, 240, 200, 287, 64);
            Clima(110, 50, 370, 118, 66);

            // Todo lo colocado arranca a temperatura ambiente (el constructor de
            // CellGrid ya inicializa temp a AmbientRaw); el Hogar se fija solo en
            // su primera visita de LabCampos. Nada de PaintStable aquí: este
            // plano se construye antes de que exista el stepper.
        }
    }
}
