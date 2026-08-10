using System.IO;
using UnityEditor;
using UnityEngine;

namespace FriendsLoop.EditorTools
{
    /// <summary>
    /// Utilidades de build de un clic para la escena de demostración de FriendsLoop.
    /// </summary>
    internal static class FriendsLoopBuildTools
    {
        private const string ScenePath = "Assets/FriendsLoop/DemoTest/FL_DemoScene.unity";
        private const string BuildFolder = "Builds/FriendsLoopDemo";
        private const string BuildExeName = "FriendsLoopDemo.exe";
        private const string DevAppId = "480";

        [MenuItem("FriendsLoop/2. Build demo Windows")]
        public static void BuildDemoWindows()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[FriendsLoop] No se encontró la escena demo en " + ScenePath +
                    ". Ejecuta primero 'FriendsLoop/1. Generar escena demo'.");
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string buildFolderAbsolute = Path.Combine(projectRoot, BuildFolder);
            Directory.CreateDirectory(buildFolderAbsolute);
            string buildExePath = Path.Combine(buildFolderAbsolute, BuildExeName);

            var buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = buildExePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                string appIdPath = Path.Combine(buildFolderAbsolute, "steam_appid.txt");
                File.WriteAllText(appIdPath, DevAppId);

                Debug.Log("[FriendsLoop] Build completada correctamente (" + report.summary.totalSize + " bytes). Ruta: " + buildFolderAbsolute +
                    ". Se escribió steam_appid.txt de desarrollo (" + DevAppId + ") junto al ejecutable; " +
                    "sustitúyelo por el App ID real y elimínalo antes de publicar en Steam.");
            }
            else
            {
                Debug.LogError("[FriendsLoop] La build falló: " + report.summary.result);
            }
        }

        [MenuItem("FriendsLoop/3. Abrir carpeta de builds")]
        public static void OpenBuildsFolder()
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string buildFolderAbsolute = Path.Combine(projectRoot, BuildFolder);
            Directory.CreateDirectory(buildFolderAbsolute);
            EditorUtility.RevealInFinder(buildFolderAbsolute);
        }
    }
}
