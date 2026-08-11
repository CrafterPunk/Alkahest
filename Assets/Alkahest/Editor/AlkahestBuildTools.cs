using System.IO;
using UnityEditor;
using UnityEngine;

namespace Alkahest.EditorTools
{
    /// <summary>
    /// [ChaosAlchemy] Build de la demo (análogo a FriendsLoopBuildTools).
    /// La escena AlkahestLab debe existir (menú "Alkahest/1. Generar escena Lab").
    /// </summary>
    public static class AlkahestBuildTools
    {
        private const string ScenePath = "Assets/Alkahest/Scenes/AlkahestLab.unity";
        private const string OutputDir = "Builds/ChaosAlchemyDemo";
        private const string OutputExe = OutputDir + "/ChaosAlchemy.exe";

        [MenuItem("Alkahest/2. Build demo Windows")]
        public static void BuildDemoWindows()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[ChaosAlchemy] No existe " + ScenePath + ". Ejecuta antes 'Alkahest/1. Generar escena Lab'.");
                return;
            }

            Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputExe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[ChaosAlchemy] ✔ Build completada ({report.summary.totalSize} bytes) en {OutputExe}");
            }
            else
            {
                Debug.LogError("[ChaosAlchemy] Build FALLIDA: " + report.summary.result);
            }
        }

        [MenuItem("Alkahest/3. Abrir carpeta de builds")]
        public static void RevealBuilds()
        {
            Directory.CreateDirectory(OutputDir);
            EditorUtility.RevealInFinder(OutputDir);
        }
    }
}
