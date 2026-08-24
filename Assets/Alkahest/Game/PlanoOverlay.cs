using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.Game
{
    /// <summary>
    /// (RONDA 77 — EL OVERLAY DEL CINCEL) LA FORMA DE CESAR COMO ASSET.
    ///
    /// El problema que resuelve: Cesar (que no es dev) quiere PERFILAR el
    /// mundo del prólogo con roca madre — "mira, así es como lo quiero" —
    /// sin pedir un cambio de código por cada mordisco. La herramienta es el
    /// juego mismo: en Play, talla con el cincel (C) y pinta Stone con la
    /// paleta dev (F3); cuando la forma le gusta, pulsa "GUARDAR FORMA COMO
    /// PLANO" en la misma paleta y la DIFERENCIA queda aquí, como asset
    /// versionable que el plano reaplica en cada arranque.
    ///
    /// QUÉ guarda: solo pares Stone↔Empty — la silueta de la roca. Nada de
    /// líquidos, polvos ni materia viva (eso es la sim corriendo, no la
    /// forma del escenario). La captura se hace contra un plano VIRGEN
    /// regenerado en una grilla de borrador, así que el asset contiene
    /// exactamente "lo que Cesar cambió", no el mundo entero.
    ///
    /// QUÉ respeta (con acuse en el log, jamás en silencio):
    ///  · la OBRA del taller (SimLevelBuilder.EsObraDelTaller): la
    ///    fontanería de la cascada, el cuenco, la mesa — regla 38: las
    ///    zonas funcionales no se dejan romper por un asset.
    ///  · la zona del DERRUMBE (grieta + cráter): esa roca la talla el
    ///    DIRECTOR en runtime todas las partidas; capturarla congelaría la
    ///    cinemática como agujero de fábrica.
    ///
    /// AUTORIDAD (matriz de la ronda 75): el asset manda sobre los deltas de
    /// roca; SimLevelBuilder sigue siendo el plano base; el código jamás
    /// escribe en el asset fuera del botón explícito de captura. REVERSIBLE:
    /// borrar el asset (o desasignarlo de la escenografía) devuelve el plano
    /// virgen — ningún otro sistema depende de él.
    ///
    /// CONSUMIDOR REAL (regla 48): <see cref="AplicarSiExiste"/>, llamado en
    /// AlkahestSim justo después de BuildFundacion — ahí y no en el
    /// bootstrap porque el mundo se RECONSTRUYE (DayCycle.RestartRun) y el
    /// overlay debe reaplicarse en cada construcción, no una vez por escena.
    /// </summary>
    public sealed class PlanoOverlay : ScriptableObject
    {
        /// <summary>Ruta canónica del asset — la que escribe el botón de la paleta y la que valida el menú 1. Una sola verdad.</summary>
        public const string RutaAsset = "Assets/Alkahest/Arte/Prologo/PlanoOverlay.asset";

        [Tooltip("Celdas retocadas, empaquetadas x | (y<<16). Lo llena el botón de la paleta dev (F3) en Play; no editar a mano.")]
        public int[] celdas = System.Array.Empty<int>();

        [Tooltip("Material por celda (Stone=1 o Empty=0), paralelo a `celdas`.")]
        public byte[] materiales = System.Array.Empty<byte>();

        [Tooltip("Nota de la captura (cuándo y cuánto). Editable — es solo memoria humana.")]
        [TextArea] public string notas = "";

        // =================================================================
        // LA ZONA DEL DERRUMBE (exclusión de captura Y de aplicación).
        // Rects deducidos de las constantes del plano: la grieta se abre en
        // la bóveda sobre FundacionDerrumbeX y el cráter muerde el suelo en
        // FundacionCraterX0..X1 (ambos tallados por FundacionDirector en
        // runtime). Márgenes de ±1..4 celdas para cubrir el cono completo.
        // =================================================================
        private static bool EsZonaDelDerrumbe(int x, int y)
        {
            // El cráter (y el fondo de la poza de lodo que abre debajo).
            if (x >= SimLevelBuilder.FundacionCraterX0 - 1 && x <= SimLevelBuilder.FundacionCraterX1 + 1 &&
                y >= SimLevelBuilder.FundacionY0 - SimLevelBuilder.FundacionPozoHondo - 1 && y <= SimLevelBuilder.FundacionY0 + 1)
                return true;
            // La grieta del techo.
            if (x >= SimLevelBuilder.FundacionDerrumbeX - 4 && x <= SimLevelBuilder.FundacionDerrumbeX + 4 &&
                y >= SimLevelBuilder.FundacionY1 - 1 && y <= SimLevelBuilder.FundacionY1 + 6)
                return true;
            return false;
        }

        /// <summary>
        /// Reaplica los retoques guardados sobre un mundo recién construido.
        /// Se llama SOLO con plano FUNDACION, justo tras BuildFundacion. Las
        /// celdas que ya no cuadran (obra nueva, plano cambiado, materia
        /// distinta de Stone/Empty en el sitio) se omiten con acuse — el
        /// overlay nunca pisa nada que no sea la silueta de roca que capturó.
        /// </summary>
        public static void AplicarSiExiste(AlkahestSim sim)
        {
            var overlay = BuscarAsset();
            if (overlay == null || overlay.celdas == null || overlay.celdas.Length == 0) return;
            if (sim == null || sim.Grid == null) return;

            int aplicadas = 0, omitidasObra = 0, omitidasEstado = 0, omitidasDerrumbe = 0;
            int n = Mathf.Min(overlay.celdas.Length, overlay.materiales.Length);
            for (int i = 0; i < n; i++)
            {
                int x = overlay.celdas[i] & 0xFFFF;
                int y = (overlay.celdas[i] >> 16) & 0xFFFF;
                byte mat = overlay.materiales[i];
                if (x <= 0 || x >= CellGrid.W - 1 || y <= 0 || y >= CellGrid.H - 1) { omitidasEstado++; continue; }
                if (mat != MaterialId.Stone && mat != MaterialId.Empty) { omitidasEstado++; continue; }
                if (SimLevelBuilder.EsObraDelTaller(x, y)) { omitidasObra++; continue; }
                if (EsZonaDelDerrumbe(x, y)) { omitidasDerrumbe++; continue; }

                // El sitio debe seguir siendo silueta de roca (Stone/Empty):
                // si el plano base cambió y ahí ahora vive otra materia, el
                // retoque viejo no aplica — se omite, no se aplasta.
                byte actual = sim.Grid.GetMat(x, y);
                if (actual != MaterialId.Stone && actual != MaterialId.Empty) { omitidasEstado++; continue; }
                if (actual == mat) { aplicadas++; continue; } // ya está así (idempotente).

                sim.PaintStable(x, y, 0, mat);
                aplicadas++;
            }

            Debug.Log("[TenThousandYears] PlanoOverlay aplicado: " + aplicadas + "/" + n +
                " celdas (omitidas: " + omitidasObra + " obra, " + omitidasDerrumbe + " zona del derrumbe, " +
                omitidasEstado + " estado/limites). Reversible: borrar " + RutaAsset);
        }

        /// <summary>
        /// El asset efectivo: el que referencia la escenografía del prólogo
        /// (autoridad de escena — lo que el menú 1 cablea para las builds), y
        /// en el editor, como red de seguridad, el de la ruta canónica (para
        /// que el ciclo tallar→guardar→reiniciar funcione aunque la escena
        /// aún no lo referencie — en Play no se puede persistir la escena).
        /// </summary>
        private static PlanoOverlay BuscarAsset()
        {
            var esc = PrologoEscenografia.Buscar();
            if (esc != null && esc.planoOverlay != null) return esc.planoOverlay;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<PlanoOverlay>(RutaAsset);
#else
            return null;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// EL BOTÓN (paleta dev, F3, solo prólogo): captura la diferencia de
        /// roca entre el mundo VIVO y un plano virgen regenerado en borrador,
        /// y la guarda/actualiza en <see cref="RutaAsset"/>. Devuelve el
        /// resumen humano que la paleta muestra bajo el botón.
        /// </summary>
        public static string Capturar(AlkahestSim sim)
        {
            if (sim == null || sim.Grid == null) return "Sin mundo vivo que capturar.";
            if (!AlkahestGameBootstrap.ModoFundacion) return "Solo en el prólogo (ModoFundacion).";

            // 1) SNAPSHOT del registro de obra: BuildFundacion lo limpia y lo
            //    re-registra, pero el registro VIVO tiene más que el plano
            //    (el depósito registra su rect al asentarse, en runtime) — se
            //    guarda entero y se restaura tal cual, pase lo que pase.
            var obraViva = SimLevelBuilder.ObraDelTaller.ToArray();
            var reservasVivas = SimLevelBuilder.ReservasDelPlano.ToArray();
            var scratch = new CellGrid();
            try
            {
                SimLevelBuilder.BuildFundacion(scratch);
            }
            finally
            {
                SimLevelBuilder.ObraDelTaller.Clear();
                SimLevelBuilder.ObraDelTaller.AddRange(obraViva);
                SimLevelBuilder.ReservasDelPlano.Clear();
                SimLevelBuilder.ReservasDelPlano.AddRange(reservasVivas);
            }

            // 2) LA DIFERENCIA: solo pares Stone↔Empty (la silueta de la
            //    roca). El agua de la cascada, el lodo, el polvo — todo eso
            //    es la sim viviendo, no la forma del escenario: se ignora en
            //    silencio. La obra y la zona del derrumbe se omiten CON
            //    ACUSE (el resumen los cuenta).
            var celdasList = new System.Collections.Generic.List<int>(256);
            var matsList = new System.Collections.Generic.List<byte>(256);
            int quitadas = 0, puestas = 0, omitidasObra = 0, omitidasDerrumbe = 0;
            for (int y = 1; y < CellGrid.H - 1; y++)
            {
                for (int x = 1; x < CellGrid.W - 1; x++)
                {
                    byte virgen = scratch.GetMat(x, y);
                    byte vivo = sim.Grid.GetMat(x, y);
                    if (virgen == vivo) continue;
                    bool parDeRoca =
                        (virgen == MaterialId.Stone && vivo == MaterialId.Empty) ||
                        (virgen == MaterialId.Empty && vivo == MaterialId.Stone);
                    if (!parDeRoca) continue;
                    if (SimLevelBuilder.EsObraDelTaller(x, y)) { omitidasObra++; continue; }
                    if (EsZonaDelDerrumbe(x, y)) { omitidasDerrumbe++; continue; }

                    celdasList.Add(x | (y << 16));
                    matsList.Add(vivo);
                    if (vivo == MaterialId.Empty) quitadas++; else puestas++;
                }
            }

            // 3) GUARDAR — sobre el MISMO asset si ya existe (el GUID no
            //    cambia: la referencia de la escena sigue valiendo), creando
            //    carpeta y asset si es la primera vez.
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<PlanoOverlay>(RutaAsset);
            bool nuevo = asset == null;
            if (nuevo)
            {
                asset = CreateInstance<PlanoOverlay>();
                string dir = System.IO.Path.GetDirectoryName(RutaAsset).Replace('\\', '/');
                if (!UnityEditor.AssetDatabase.IsValidFolder(dir))
                    System.IO.Directory.CreateDirectory(dir);
                UnityEditor.AssetDatabase.CreateAsset(asset, RutaAsset);
            }
            asset.celdas = celdasList.ToArray();
            asset.materiales = matsList.ToArray();
            asset.notas = "Captura en Play: " + quitadas + " celdas talladas, " + puestas +
                " de roca añadida (omitidas " + omitidasObra + " de obra, " + omitidasDerrumbe + " del derrumbe).";
            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();

            // 4) Cablear la referencia EN MEMORIA para que un reinicio dentro
            //    de esta misma sesión de Play ya lo aplique; la escena
            //    persistida la cablea el menú 1 (el validador completa campos
            //    null — así también llega a las builds).
            var esc = PrologoEscenografia.Buscar();
            if (esc != null && esc.planoOverlay == null) esc.planoOverlay = asset;

            string resumen = "Guardado: " + quitadas + " talladas + " + puestas + " añadidas → " + RutaAsset +
                (omitidasObra + omitidasDerrumbe > 0
                    ? "  (omitidas " + omitidasObra + " de obra, " + omitidasDerrumbe + " de la zona del derrumbe)"
                    : "") +
                (nuevo ? "  — corre el menú 1 para cablearlo a la escena." : "");
            Debug.Log("[TenThousandYears] PlanoOverlay: " + resumen);
            return resumen;
        }
#endif
    }
}
