using System;
using System.Diagnostics;
using Alkahest.Sim;

public static class Harness
{
    public static void Main()
    {
        // ESCENARIOS DE ESTRÉS (banco headless, informe del motor):
        Escenario("CASCADA (100x80 de agua cayendo 100 celdas)", (grid) => {
            Bloque(grid, 300, 180, 100, 80, MaterialId.Water);
        });
        Escenario("DILUVIO TOTAL (medio mundo de agua sobre suelo)", (grid) => {
            Bloque(grid, 10, 150, 740, 100, MaterialId.Water);
        });
        Escenario("INCENDIO (campo de aceite 300x30 + linea de fuego)", (grid) => {
            Bloque(grid, 200, 20, 300, 30, MaterialId.Oil);
            Bloque(grid, 200, 51, 300, 1, MaterialId.Fire);
        });
        Escenario("ARENA MASIVA (150x100 de arena cayendo)", (grid) => {
            Bloque(grid, 300, 150, 150, 100, MaterialId.Sand);
        });
        Escenario("MUNDO MIXTO (agua+aceite+arena+fuego a la vez)", (grid) => {
            Bloque(grid, 50, 200, 200, 60, MaterialId.Water);
            Bloque(grid, 300, 200, 200, 60, MaterialId.Oil);
            Bloque(grid, 550, 200, 150, 60, MaterialId.Sand);
            Bloque(grid, 300, 100, 200, 2, MaterialId.Fire);
        });
    }

    static void Bloque(CellGrid g, int x0, int y0, int w, int h, byte mat)
    {
        for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
                if (CellGrid.InBounds(x, y)) { g.SetCell(x, y, mat); g.WakeChunk(x, y, 0); }
    }

    static void Escenario(string nombre, Action<CellGrid> setup)
    {
        var universe = Universe.Create(12345);
        var grid = new CellGrid();
        // suelo de piedra
        for (int x = 0; x < CellGrid.W; x++)
            for (int y = 0; y < 12; y++) grid.SetCell(x, y, MaterialId.Stone);
        setup(grid);
        var stepper = new SimStepper(universe, grid);
        // warmup
        for (int i = 0; i < 30; i++) stepper.Step();
        var sw = Stopwatch.StartNew();
        int ticks = 300;
        double peak = 0; long peakCells = 0;
        double total = 0;
        for (int i = 0; i < ticks; i++)
        {
            stepper.Step();
            total += stepper.LastStepMs;
            if (stepper.LastStepMs > peak) { peak = stepper.LastStepMs; peakCells = stepper.ActiveCells; }
        }
        sw.Stop();
        Console.WriteLine($"{nombre}\n  media: {total/ticks:F2} ms/tick | pico: {peak:F2} ms ({peakCells} celdas activas) | presupuesto 30Hz: 33.3 ms | headroom: {33.3/(total/ticks):F1}x\n");
    }
}
