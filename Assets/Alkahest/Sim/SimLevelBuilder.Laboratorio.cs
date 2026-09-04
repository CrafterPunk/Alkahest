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
    ///                       fisura de ARENISCA (x192-202 y111-134) hasta la cámara profunda,
    ///                       grava en el lecho, POZA x250-330 (fondo 122, prellenada),
    ///                       grieta x336-343 (atascada de grava sobre repisa de arenisca)
    ///                       a la cámara profunda, pozo del SUMIDERO x416-429.
    ///   CÁMARA PROFUNDA     x120-360 y60-110   con cubeta x140-180 y52-59.
    ///   CHIMENEA            x140-149 y215-244  de la sala a la cámara alta.
    ///   CÁMARA ALTA         x100-190 y245-272  fría; piso de lecho de sedimento (4 celdas)
    ///                       sobre solera de arcilla, con labio de roca a los lados de la
    ///                       boca de la chimenea para que el suelo no se escurra (R135).
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
            // (R131, H1) LA FISURA ERA ARENA SUELTA Y SE CAÍA: el polvo se
            // derrumbaba a la cámara profunda y el arroyo entero se colaba por
            // el agujero (el sumidero no veía una gota). Ahora es ARENISCA:
            // roca porosa estática. El agua la atraviesa despacio, sale LIMPIA
            // por abajo (el poroso se queda los finos) y el resto del caudal
            // sigue su camino aguas abajo. Se ve el frente mojado bajar.
            Bloque(192, 111, 202, 134, MaterialId.Arenisca);  // fisura porosa: del lecho a la cámara profunda
            Bloque(350, 133, 372, 135, MaterialId.Grava);     // banco de grava en el lecho
            Bloque(251, 122, 329, 123, MaterialId.Sedimento); // fondo de la poza: sedimento húmedo
            Campo(251, 122, 329, 123, 255, 0);
            Bloque(251, 124, 329, 128, MaterialId.Water);     // poza medio llena, turbia
            Campo(251, 124, 329, 128, 255, 60);
            // (R131, H1) LA GRIETA SIGUE AHÍ, pero ATASCADA DE ESCOMBRO. Abierta
            // era un pozo de 8 celdas en mitad del lecho: se tragaba TODO lo que
            // rebosaba de la poza y el sumidero seguía seco. Rellena de grava
            // sobre una repisa de arenisca, sangra un hilo hacia la cámara
            // profunda (percolación grava→arenisca→exudación limpia en y110) y
            // deja pasar el resto aguas abajo. Además la grava se va COLMATANDO
            // con los finos del manantial: el hilo se cierra solo con el tiempo.
            // Y es un mando para el jugador: destapar la grieta vacía el arroyo.
            Bloque(336, 111, 343, 111, MaterialId.Arenisca);   // repisa: sostiene el escombro y filtra lo que gotea abajo
            Bloque(336, 112, 343, 132, MaterialId.Grava);      // grieta atascada de escombro grueso
            Aire(416, 100, 429, 131);                         // pozo del sumidero
            Bloque(416, 96, 429, 99, MaterialId.Sumidero);

            // ---- CÁMARA PROFUNDA ----
            Aire(120, 60, 360, 110);
            Aire(140, 52, 180, 59);                           // cubeta en el piso

            // ---- CHIMENEA, CÁMARA ALTA y BOCA DEL CIELO ----
            Aire(140, 215, 149, 244);
            Aire(100, 245, 190, 272);
            // (R135, R9 de Fable) EL PISO QUE AGUANTA EL RIEGO. Antes eran dos celdas de
            // sedimento suelto sobre roca, con la boca de la chimenea (x137-152) abierta
            // justo en medio: el goteo del alambique lavaba el suelo y lo escurría por el
            // agujero (medido en R134: 74 → 22 celdas de sustrato en el claro en 300 s).
            // Ahora hay SOLERA de arcilla (impermeable: el agua no se pierde por abajo),
            // LECHO de cuatro celdas de sedimento encima, y un LABIO de roca de una celda
            // a cada lado de la boca, más alto que el lecho, para que el polvo no se
            // deslice al vacío. El claro de luz (x100-147) queda entero sobre tierra.
            Bloque(100, 245, 136, 245, MaterialId.Arcilla);   // solera oeste
            Bloque(153, 245, 190, 245, MaterialId.Arcilla);   // solera este
            Bloque(100, 246, 135, 249, MaterialId.Sedimento); // lecho SECO de 4 celdas
            Bloque(154, 246, 190, 249, MaterialId.Sedimento);
            // El labio llega EXACTAMENTE a la altura del lecho, ni una celda más: es un
            // REBOSADERO. Un labio más alto convierte la solera impermeable en una bañera
            // — medido: a los 150 s, 24 de las 48 columnas del claro estaban BAJO AGUA y
            // no puede germinar nada bajo el agua. A ras, el sobrante del goteo se va por
            // la boca de la chimenea en cuanto supera la tierra, y el polvo no se desliza
            // porque a su altura tiene roca al lado, no hueco.
            Bloque(136, 246, 136, 249, MaterialId.Stone);     // labio oeste de la boca (a ras del lecho)
            Bloque(153, 246, 153, 249, MaterialId.Stone);     // labio este
            // (R136/R138, R11 y R17 de Fable) EL DESAGÜE. Un lecho sobre solera impermeable y con
            // labio se encharca: el goteo del alambique no tiene por dónde irse y no germina nada
            // bajo el agua. Dos columnas de grava (permeabilidad 90) junto a cada labio conducen
            // el agua al fondo, y el conducto ATRAVIESA LA SOLERA hasta asomar a la boca de la
            // chimenea, que es donde exuda al saturar. El nivel se queda por DEBAJO de la
            // superficie: la tierra se moja, no se inunda. Geometría del nivel, no una regla.
            //
            // El primer intento (R136) puso la salida en la mitad baja del LABIO, y estaba mal por
            // dos motivos que midió Fable: un poroso solo suelta agua a un VACÍO al saturar, y al
            // labio el agua solo le llega por capilaridad lateral, que no basta; y además la grava
            // es polvo, así que `ProcessPowder` la deslizaba en diagonal al aire de (137,245) y el
            // labio se derrumbaba en la boca en el primer tick. Por eso el labio vuelve a ser roca
            // ENTERA y el conducto baja hasta y245: ahí tiene roca debajo, sedimento al lado y el
            // aire de la boca justo al costado, sin diagonal por la que escurrirse.
            Bloque(134, 246, 135, 249, MaterialId.Grava);     // conductos del lecho oeste
            Bloque(154, 246, 155, 249, MaterialId.Grava);     // conductos del lecho este
            Bloque(134, 245, 136, 245, MaterialId.Grava);     // y a través de la solera, hasta la boca
            Bloque(153, 245, 155, 245, MaterialId.Grava);
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

            // (R133, decisión de Fable en R5.1) EL AIRE NO NACE SECO. Una cueva
            // real vive cerca de la saturación; con humedad 0 en todo el aire, el
            // primer vapor que el jugador produce se gasta ENTERO en humedecer el
            // volumen antes de que ninguna pared pueda sudar — y en una cámara de
            // 2 548 celdas eso son ~350 celdas de agua tiradas. Cada celda arranca
            // al `aire.humedadInicialPct` de SU PROPIA saturación, que depende de
            // SU temperatura ambiente: la cámara alta (8 °C, satura a 36) nace con
            // menos agua que el arroyo (20 °C, satura a 60), y NINGUNA nace
            // supersaturada, así que el mundo no llueve solo al arrancar.
            // Va después del clima a propósito: necesita el ambiente ya pintado.
            int pct = LabParams.HumedadInicialPct;
            if (pct > 0)
            {
                for (int i = 0; i < grid.mat.Length; i++)
                {
                    if (grid.mat[i] != MaterialId.Empty) continue;
                    int h = LabParams.Saturacion(grid.ambient[i]) * pct / 100;
                    grid.humedad[i] = (byte)(h > 255 ? 255 : h);
                }
            }

            // Todo lo colocado arranca a temperatura ambiente (el constructor de
            // CellGrid ya inicializa temp a AmbientRaw); el Hogar se fija solo en
            // su primera visita de LabCampos. Nada de PaintStable aquí: este
            // plano se construye antes de que exista el stepper.
        }
    }
}
