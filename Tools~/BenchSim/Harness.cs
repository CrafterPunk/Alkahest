using System;
using System.Diagnostics;
using Alkahest.Sim;

public static class Harness
{
    public static void Main()
    {
        // ESCENARIOS DE ESTRÉS (banco headless, informe del motor):
        Escenario("CASCADA (100x80 de agua cayendo 100 celdas)", (grid, universe) => {
            Bloque(grid, 300, 180, 100, 80, MaterialId.Water);
        });
        Escenario("DILUVIO TOTAL (medio mundo de agua sobre suelo)", (grid, universe) => {
            Bloque(grid, 10, 150, 740, 100, MaterialId.Water);
        });
        Escenario("INCENDIO (campo de aceite 300x30 + linea de fuego)", (grid, universe) => {
            Bloque(grid, 200, 20, 300, 30, MaterialId.Oil);
            Bloque(grid, 200, 51, 300, 1, MaterialId.Fire);
        });
        Escenario("ARENA MASIVA (150x100 de arena cayendo)", (grid, universe) => {
            Bloque(grid, 300, 150, 150, 100, MaterialId.Sand);
        });
        Escenario("MUNDO MIXTO (agua+aceite+arena+fuego a la vez)", (grid, universe) => {
            Bloque(grid, 50, 200, 200, 60, MaterialId.Water);
            Bloque(grid, 300, 200, 200, 60, MaterialId.Oil);
            Bloque(grid, 550, 200, 150, 60, MaterialId.Sand);
            Bloque(grid, 300, 100, 200, 2, MaterialId.Fire);
        });
        // ---------------------------------------------------------------------------------
        // (playtest 39, contrato ENCARGO S 1f) INCENDIO SOSTENIDO -- el escenario que mide
        // la combustión persistente de verdad: una piscina de aceite grande encendida SOLO
        // por un borde (debe seguir ardiendo, consumiéndose desde ahí, durante los 300 ticks
        // = 10s completos del banco, no apagarse en 1-2s como el fuego viejo), un LECHO de
        // sólido combustible (el Calcinado que Universe.BaseCombustibleGarantizada asegura
        // alcanzable en toda seed -- ejercita Powder -> Brasa -> Ash), y un TECHO con una
        // brecha parcial para que Steam/Smoke formen BOLSAS bajo la bóveda en vez de escapar
        // libres (contrato 1c) -- el "peor caso" combinado de las tres capas del encargo S
        // a la vez, sobre el mismo mundo.
        // ---------------------------------------------------------------------------------
        Escenario("INCENDIO SOSTENIDO (piscina de aceite + lecho combustible + techo con bolsas)", (grid, universe) => {
            // Techo de piedra sobre la piscina con una brecha parcial de escape:
            // el gas que no cabe por la brecha se ve obligado a esparcirse
            // lateralmente bajo la bóveda (contrato 1c).
            Bloque(grid, 100, 90, 500, 4, MaterialId.Stone);
            for (int y = 90; y < 94; y++)
                for (int x = 320; x < 340; x++)
                    if (CellGrid.InBounds(x, y)) grid.SetCell(x, y, MaterialId.Empty);

            // Piscina grande de aceite, encendida SOLO por el borde izquierdo
            // (el patrón oro del contrato: debe consumirse visiblemente desde
            // ahí durante decenas de segundos, no todo de golpe).
            Bloque(grid, 120, 20, 300, 20, MaterialId.Oil);
            Bloque(grid, 120, 39, 20, 1, MaterialId.Fire);

            // Lecho de sólido combustible: el Calcinado de esta seed que el
            // solver de persistencia garantiza alcanzable con el rescoldo del
            // Crisol (Universe.BaseCombustibleGarantizada) -- ejercita el
            // camino Powder -> Brasa -> Ash del contrato 1a/1b.
            byte fuel = MaterialId.MatDe(universe.BaseCombustibleGarantizada, EstadoMateria.Calcinado);
            Bloque(grid, 450, 20, 100, 15, fuel);
            Bloque(grid, 450, 34, 10, 1, MaterialId.Fire);
        });
    }

    static void Bloque(CellGrid g, int x0, int y0, int w, int h, byte mat)
    {
        for (int y = y0; y < y0 + h; y++)
            for (int x = x0; x < x0 + w; x++)
                if (CellGrid.InBounds(x, y)) { g.SetCell(x, y, mat); g.WakeChunk(x, y, 0); }
    }

    static void Escenario(string nombre, Action<CellGrid, Universe> setup)
    {
        var universe = Universe.Create(12345);
        var grid = new CellGrid();
        // suelo de piedra
        for (int x = 0; x < CellGrid.W; x++)
            for (int y = 0; y < 12; y++) grid.SetCell(x, y, MaterialId.Stone);
        setup(grid, universe);
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
