using System.IO;
using UnityEditor;
using UnityEngine;
using Alkahest.Sim;

namespace Alkahest.EditorTools
{
    /// <summary>
    /// (H5, R142) Lanzador del banco del laboratorio desde el menú del editor.
    ///
    /// El banco en sí (`Sim/LabBench.cs`) es C# puro y no sabe nada de Unity; esto solo lo llama,
    /// escribe el informe en `Laboratorio/benchmarks/` y lo abre. Así la medida se repite con dos
    /// clics en vez de reescribiendo un `RunCommand`, que es lo que la hacía irrepetible.
    /// </summary>
    public static class LabBenchMenu
    {
        [MenuItem("Ten Thousand Years/8. Banco del laboratorio (H5)", priority = 8)]
        public static void CorrerBanco()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Banco del laboratorio",
                    "Sal del modo Play antes de correr el banco: compite por la CPU con el juego y las medidas salen sucias.", "Vale");
                return;
            }

            var resultados = new System.Collections.Generic.List<LabBench.Resultado>();
            try
            {
                for (int i = 0; i < LabBench.Escenarios.Length; i++)
                {
                    var e = LabBench.Escenarios[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Banco del laboratorio",
                        e.Nombre + "  (" + e.Ticks + " ticks)", i / (float)LabBench.Escenarios.Length))
                        break;
                    resultados.Add(LabBench.Correr(e.Nombre, e.Ticks, e.Montar));
                }
            }
            finally { EditorUtility.ClearProgressBar(); }

            if (resultados.Count == 0) { Debug.Log("[TenThousandYears] Banco cancelado."); return; }

            string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Laboratorio", "benchmarks"));
            Directory.CreateDirectory(carpeta);
            // (R148, R24-11) Con la hora: dos corridas el mismo día se pisaban el informe, que es
            // justo lo que se hace cuando se compara antes y después de un cambio.
            string ruta = Path.Combine(carpeta, System.DateTime.Now.ToString("yyyy-MM-dd_HHmm") + "_banco.md");
            File.WriteAllText(ruta, LabBench.Informe(resultados, "Banco del laboratorio (H5) — "
                + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm")));
            Debug.Log("[TenThousandYears] Banco terminado: " + resultados.Count + " escenarios → " + ruta);
            EditorUtility.RevealInFinder(ruta);
        }
    }
}
