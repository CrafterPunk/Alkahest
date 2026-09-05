using System;
using System.Collections.Generic;
using System.Text;

namespace Alkahest.Sim
{
    /// <summary>
    /// (H5, R142) EL BANCO DEL LABORATORIO.
    ///
    /// Hasta ahora cada medida del laboratorio vivía dentro de un `RunCommand` escrito a mano y
    /// tirado a la basura al terminar: doce rondas de escenarios que nadie podía volver a correr
    /// sin reescribirlos, y tres bancos montados MAL antes de que arrancaran (los polvos se
    /// reacomodan, un horno relleno con huecos se vacía hacia el hueco). Esto los recoge todos en
    /// un sitio, con su montaje escrito una vez.
    ///
    /// Cada escenario devuelve, además de sus tiempos, un HASH de `mat`, `temp` y `aux`. El hash
    /// es la prueba de determinismo y, sobre todo, la licencia para optimizar: un cambio de
    /// rendimiento que deje los ocho hashes intactos no ha tocado la física, y uno que mueva un
    /// solo hash es un cambio de física disfrazado (regla acordada con Fable, R141 §1).
    ///
    /// C# puro, sin API de Unity: corre en el editor, en una build o en un banco headless.
    /// </summary>
    public static class LabBench
    {
        public const int SeedLaboratorio = 777002;

        /// <summary>Resultado de un escenario: lo que se mide y lo que prueba que no ha cambiado.</summary>
        public struct Resultado
        {
            public string Nombre;
            public int Ticks;
            public double MsMedia, MsPico;
            public double MsDifusion, MsBarrido, MsCampos, MsPresion, MsLuz, MsCuerpos;
            public int ChunksActivos, CeldasActivas;
            public long MemoriaAntes, MemoriaDespues;
            public uint HashMat, HashTemp, HashAux;
            /// <summary>(R145) Los cuatro campos del laboratorio: sin ellos el hash no ve el agua.</summary>
            public uint HashHumedad, HashCarga, HashReposo, HashLuz;
            public double TicksPorSegundo => MsMedia > 0 ? 1000.0 / MsMedia : 0;
        }

        /// <summary>
        /// FNV-1a sobre un array de bytes. Barato, estable entre ejecuciones y entre máquinas
        /// (no usa `GetHashCode`, que en .NET puede estar aleatorizado por proceso y convertiría
        /// la prueba de determinismo en ruido).
        /// </summary>
        public static uint Hash(byte[] datos)
        {
            unchecked
            {
                uint h = 2166136261u;
                for (int i = 0; i < datos.Length; i++) { h ^= datos[i]; h *= 16777619u; }
                return h;
            }
        }

        // =====================================================================
        // LOS ESCENARIOS
        // =====================================================================

        public delegate void Montaje(CellGrid g);

        /// <summary>Nombre, ticks y montaje de cada escenario del banco.</summary>
        public static readonly (string Nombre, int Ticks, Montaje Montar)[] Escenarios =
        {
            ("laboratorio base",        3000,  MontarLaboratorio),
            ("alambique de r141",       9000,  MontarAlambique),
            ("horno con yesca",         9000,  MontarHorno),
            ("carbonera 20x20 boca 1",  9000,  MontarCarbonera),
            ("tolva de fibra",         14000,  MontarTolva),
            ("diluvio turbio",          3000,  MontarDiluvio),
            ("hervidero",               9000,  MontarHervidero),
            ("mundo entero despierto",  2000,  MontarMundoDespierto),
        };

        static void Bloque(CellGrid g, int x0, int y0, int x1, int y1, byte m)
        {
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    if (CellGrid.InBounds(x, y)) g.SetCell(x, y, m);
        }

        /// <summary>(1) El nivel de referencia tal cual lo construye el juego.</summary>
        public static void MontarLaboratorio(CellGrid g) => SimLevelBuilder.BuildLaboratorioDeLeyes(g);

        /// <summary>
        /// (2) El alambique de Fable (r141 §2): nivel real más serpentín de 31 celdas de núcleo
        /// frío en el techo de la cámara alta. La caldera la repone el propio banco (ver Correr).
        /// </summary>
        public static void MontarAlambique(CellGrid g)
        {
            SimLevelBuilder.BuildLaboratorioDeLeyes(g);
            for (int x = 105; x <= 135; x++) g.SetCell(x, 272, MaterialId.NucleoFrio);
        }

        /// <summary>
        /// (3) El horno de HF2: recinto de roca 20×14, solera de ceniza, yesca de fibra sobre el
        /// piloto, dos columnas de arena de carga entre columnas de ceniza y el resto carbón.
        /// El fuego va POR ABAJO: con el piloto arriba, la pila se vacía hacia el hueco y no prende.
        /// </summary>
        public static void MontarHorno(CellGrid g)
        {
            Bloque(g, 80, 190, 360, 199, MaterialId.Stone);
            int x0 = 100, y0 = 200, x1 = x0 + 19, y1 = y0 + 13;
            Bloque(g, x0, y0, x1, y1, MaterialId.Stone);
            Bloque(g, x0 + 1, y0 + 1, x1 - 1, y1 - 1, MaterialId.Empty);
            Bloque(g, x0 + 9, y0, x0 + 11, y0, MaterialId.Hogar);
            Bloque(g, x0 + 1, y0 + 1, x1 - 1, y0 + 1, MaterialId.Ash);
            Bloque(g, x0 + 8, y0 + 1, x0 + 12, y0 + 1, MaterialId.Fibra);
            for (int y = y0 + 2; y <= y0 + 10; y++)
                for (int x = x0 + 1; x <= x1 - 1; x++)
                {
                    if (x == x0 + 10 || x == x0 + 11) g.SetCell(x, y, MaterialId.Sand);
                    else if (x == x0 + 12 || x == x0 + 9) g.SetCell(x, y, MaterialId.Ash);
                    else g.SetCell(x, y, MaterialId.Carbon);
                }
        }

        /// <summary>(4) B-F3: 400 celdas de fibra en recinto de roca con una boca de 1 y el piloto en la solera.</summary>
        public static void MontarCarbonera(CellGrid g)
        {
            Bloque(g, 20, 190, 740, 199, MaterialId.Stone);
            Bloque(g, 100, 200, 121, 223, MaterialId.Stone);
            Bloque(g, 101, 201, 120, 222, MaterialId.Empty);
            Bloque(g, 110, 200, 111, 200, MaterialId.Hogar);
            Bloque(g, 101, 201, 120, 220, MaterialId.Fibra);
            g.SetCell(101, 223, MaterialId.Empty); // la boca
        }

        /// <summary>(5) B-F5: silo de 360 celdas de fibra sobre un fogón con boca de 3. Cero intervenciones.</summary>
        public static void MontarTolva(CellGrid g)
        {
            Bloque(g, 80, 190, 360, 199, MaterialId.Stone);
            int a0 = 100, b0 = 200, a1 = a0 + 13, b1 = b0 + 34;
            Bloque(g, a0, b0, a1, b1, MaterialId.Stone);
            Bloque(g, a0 + 1, b0 + 1, a1 - 1, b1 - 1, MaterialId.Empty);
            Bloque(g, a0 + 6, b0, a0 + 7, b0, MaterialId.Hogar);
            Bloque(g, a0 + 1, b0 + 1, a1 - 1, b0 + 30, MaterialId.Fibra);
            Bloque(g, a0, b0 + 2, a0, b0 + 4, MaterialId.Empty);
        }

        /// <summary>(6) Medio mundo de agua con la turbidez al tope: el peor caso de los fluidos.</summary>
        public static void MontarDiluvio(CellGrid g)
        {
            Bloque(g, 0, 0, CellGrid.W - 1, 3, MaterialId.Stone);
            for (int y = 4; y < CellGrid.H / 2; y++)
                for (int x = 1; x < CellGrid.W - 1; x++)
                {
                    int i = CellGrid.Idx(x, y);
                    g.SetCell(x, y, MaterialId.Water);
                    g.humedad[i] = 255; g.carga[i] = 255;
                }
        }

        /// <summary>(7) 200 celdas de hogar bajo una masa de agua, con chimenea y cámara fría arriba.</summary>
        public static void MontarHervidero(CellGrid g)
        {
            Bloque(g, 0, 0, CellGrid.W - 1, 9, MaterialId.Stone);
            Bloque(g, 100, 10, 299, 10, MaterialId.Hogar);
            for (int y = 11; y <= 60; y++)
                for (int x = 100; x <= 299; x++)
                {
                    g.SetCell(x, y, MaterialId.Water);
                    g.humedad[CellGrid.Idx(x, y)] = 255;
                }
            Bloque(g, 99, 10, 99, 120, MaterialId.Stone);
            Bloque(g, 300, 10, 300, 120, MaterialId.Stone);
            Bloque(g, 100, 120, 299, 120, MaterialId.Stone);
            // (R145, R23-16) La chimenea sube LIBRE y la cámara fría va ENCIMA, a los lados: antes
            // la barra de núcleo frío cruzaba justo sobre la boca y la tapaba, así que el escenario
            // era una caldera sellada con otro nombre.
            Bloque(g, 190, 120, 209, 120, MaterialId.Empty);      // la boca de la chimenea
            Bloque(g, 189, 121, 189, 150, MaterialId.Stone);      // el tiro
            Bloque(g, 210, 121, 210, 150, MaterialId.Stone);
            Bloque(g, 150, 121, 188, 121, MaterialId.NucleoFrio); // la cámara fría, a los lados del tiro
            Bloque(g, 211, 121, 250, 121, MaterialId.NucleoFrio);
        }

        /// <summary>
        /// (8) Un HOGAR por chunk: el peor caso del planificador, con los 864 chunks despiertos a la vez.
        /// El primer intento pintaba arena, y no servía: la arena cae, se posa y el chunk se duerme
        /// —a los 2 000 ticks daba 0 chunks activos, o sea que medía el mundo dormido con otro nombre.
        /// El hogar es una brasa eterna que se despierta a sí misma cada tick (R55), así que es lo
        /// único que sostiene el peor caso de verdad.
        /// </summary>
        public static void MontarMundoDespierto(CellGrid g)
        {
            Bloque(g, 0, 0, CellGrid.W - 1, 1, MaterialId.Stone);
            for (int cy = 0; cy < CellGrid.ChunksY; cy++)
                for (int cx = 0; cx < CellGrid.ChunksX; cx++)
                {
                    int x = cx * 16 + 8, y = cy * 16 + 8;   // CHUNK = 16 (CellGrid lo tiene privado)
                    if (!CellGrid.InBounds(x, y)) continue;
                    g.SetCell(x, y, MaterialId.Hogar);
                    g.WakeChunk(x, y, 0u);
                }
        }

        // =====================================================================
        // EL MOTOR DEL BANCO
        // =====================================================================

        /// <summary>Corre un escenario y devuelve sus tiempos y sus tres hashes.</summary>
        public static Resultado Correr(string nombre, int ticks, Montaje montar)
        {
            var r = new Resultado { Nombre = nombre, Ticks = ticks };
            r.MemoriaAntes = GC.GetTotalMemory(true);

            // (R145, R23-13) El banco parte SIEMPRE de fábrica. Si no, hereda los sliders de la
            // última sesión de Play y la boca de cielo que dejó el escenario anterior, y entonces
            // el hash deja de significar «esta física» para significar «esta física y lo que
            // hubiera tocado alguien». Se restaura al terminar para no pisar un laboratorio abierto.
            var guardados = new float[LabParams.Registro.Count];
            for (int i = 0; i < guardados.Length; i++)
            {
                guardados[i] = LabParams.Registro[i].Leer();
                LabParams.Registro[i].Escribir(LabParams.Registro[i].Def);
            }
            int cieloX0 = LabParams.LuzCieloX0, cieloX1 = LabParams.LuzCieloX1;
            LabParams.LuzCieloX0 = -1; LabParams.LuzCieloX1 = -1;
            try
            {

            var u = Universe.Create(SeedLaboratorio);
            // (R145, R23-1) LA LÍNEA QUE FALTABA, y sin ella el banco entero medía OTRO universo.
            // `Universe.Create` deja la química SORTEADA de la campaña: agua con densidad y puntos
            // de cambio de fase de la seed, vapor que vive 60 ticks y condensa a ~60 °C — el número
            // que rompía la cadena del agua y que R133 promovió a parámetro. Con eso, el alambique
            // del banco no destilaba y la carbonera no se ahogaba igual, y los hashes «cuadraban»
            // entre Fable y yo porque los dos medíamos lo mismo mal. Misma línea que AlkahestSim.cs:222.
            Universe.AplicarOverridesLaboratorio(u);
            var g = new CellGrid();
            montar(g);
            var st = new SimStepper(u, g) { LabActivo = true };

            bool esAlambique = nombre == "alambique de r141";
            double suma = 0, pico = 0;
            double dif = 0, bar = 0, cam = 0, pre = 0, luz = 0, cue = 0;
            for (int t = 1; t <= ticks; t++)
            {
                // La caldera del alambique: siete celdas de agua sobre el hogar cada 8 ticks.
                // Es la ÚNICA intervención del banco, y va antes del paso para que sea reproducible.
                // (R145, R23-15) Reposición INCONDICIONAL, como la caldera de r141 §2. Con la
                // guarda de «solo si está vacío» el banco regaba la mitad (492 goteos contra 902):
                // en cuanto la celda tenía algo, esa gota no se reponía nunca.
                if (esAlambique && t % 8 == 0)
                    for (int x = 151; x <= 157; x++)
                    {
                        int i = CellGrid.Idx(x, 180);
                        g.SetCell(x, 180, MaterialId.Water); g.humedad[i] = 255; g.WakeChunk(x, 180, (uint)t);
                    }

                var t0 = DateTime.UtcNow;
                st.Step();
                double ms = (DateTime.UtcNow - t0).TotalMilliseconds;
                suma += ms; if (ms > pico) pico = ms;
                dif += st.MsDifusion; bar += st.MsBarrido; cam += st.MsCampos;
                pre += st.MsPresion; luz += st.MsLuz; cue += st.MsCuerpos;
            }

            r.MsMedia = suma / ticks; r.MsPico = pico;
            r.MsDifusion = dif / ticks; r.MsBarrido = bar / ticks; r.MsCampos = cam / ticks;
            r.MsPresion = pre / ticks; r.MsLuz = luz / ticks; r.MsCuerpos = cue / ticks;
            r.ChunksActivos = st.ActiveChunks; r.CeldasActivas = st.ActiveCells;
            r.HashMat = Hash(g.mat); r.HashTemp = Hash(g.temp); r.HashAux = Hash(g.aux);
            // (R145, R23-14) Los cuatro campos del laboratorio también entran al hash. Sin ellos un
            // cambio en la física del AGUA —que vive entera en `humedad`— pasaba sin mover un solo
            // hash, que es justo lo que el hash promete detectar.
            r.HashHumedad = Hash(g.humedad); r.HashCarga = Hash(g.carga);
            r.HashReposo = Hash(g.reposo); r.HashLuz = Hash(g.luz);
            r.MemoriaDespues = GC.GetTotalMemory(false);
            return r;
            }
            finally
            {
                for (int i = 0; i < guardados.Length; i++) LabParams.Registro[i].Escribir(guardados[i]);
                LabParams.LuzCieloX0 = cieloX0; LabParams.LuzCieloX1 = cieloX1;
            }
        }

        /// <summary>Corre los ocho escenarios en orden.</summary>
        public static List<Resultado> CorrerTodos()
        {
            var lista = new List<Resultado>(Escenarios.Length);
            foreach (var e in Escenarios) lista.Add(Correr(e.Nombre, e.Ticks, e.Montar));
            return lista;
        }

        /// <summary>El informe en Markdown, listo para `Laboratorio/benchmarks/`.</summary>
        public static string Informe(List<Resultado> rs, string titulo)
        {
            var sb = new StringBuilder(4096);
            sb.Append("# ").Append(titulo).Append("\n\n");
            sb.Append("Generado por `Sim/LabBench.cs` (H5). Semilla ").Append(SeedLaboratorio)
              .Append(", defaults de `LabParams`. Los hashes son FNV-1a de `mat`, `temp` y `aux` al\n")
              .Append("terminar cada escenario: si un cambio de rendimiento los deja intactos, no tocó la física.\n\n");

            sb.Append("| escenario | ticks | ms/tick | pico | ticks/s | chunks | celdas | hash mat | hash temp | hash aux |\n");
            sb.Append("|---|---:|---:|---:|---:|---:|---:|---|---|---|\n");
            foreach (var r in rs)
                sb.Append("| ").Append(r.Nombre).Append(" | ").Append(r.Ticks)
                  .Append(" | ").Append(r.MsMedia.ToString("F2"))
                  .Append(" | ").Append(r.MsPico.ToString("F2"))
                  .Append(" | ").Append(r.TicksPorSegundo.ToString("F0"))
                  .Append(" | ").Append(r.ChunksActivos).Append(" | ").Append(r.CeldasActivas)
                  .Append(" | `").Append(r.HashMat.ToString("x8"))
                  .Append("` | `").Append(r.HashTemp.ToString("x8"))
                  .Append("` | `").Append(r.HashAux.ToString("x8")).Append("` |\n");

            sb.Append("\n## Reparto por fase (ms/tick de media)\n\n");
            sb.Append("| escenario | difusión | barrido | campos | presión | luz | cuerpos |\n");
            sb.Append("|---|---:|---:|---:|---:|---:|---:|\n");
            foreach (var r in rs)
                sb.Append("| ").Append(r.Nombre)
                  .Append(" | ").Append(r.MsDifusion.ToString("F2"))
                  .Append(" | ").Append(r.MsBarrido.ToString("F2"))
                  .Append(" | ").Append(r.MsCampos.ToString("F2"))
                  .Append(" | ").Append(r.MsPresion.ToString("F2"))
                  .Append(" | ").Append(r.MsLuz.ToString("F2"))
                  .Append(" | ").Append(r.MsCuerpos.ToString("F2")).Append(" |\n");

            sb.Append("\n## Memoria\n\n| escenario | antes (MB) | después (MB) |\n|---|---:|---:|\n");
            foreach (var r in rs)
                sb.Append("| ").Append(r.Nombre)
                  .Append(" | ").Append((r.MemoriaAntes / 1048576.0).ToString("F1"))
                  .Append(" | ").Append((r.MemoriaDespues / 1048576.0).ToString("F1")).Append(" |\n");
            return sb.ToString();
        }
    }
}
