using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
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

        [MenuItem("Alkahest/3. Build demo Windows (un jugador)", priority = 3)]
        public static void BuildDemoWindows()
        {
            // (pre-vuelo build) Antes esto solo comprobaba que el .unity EXISTIERA en
            // disco, nunca que estuviera al día: una escena guardada varias rondas
            // atrás (p.ej. la cámara vieja de antes del rediseño del espacio) se
            // habría colado en la build en silencio. Regenerarla aquí usa el mismo
            // menú idempotente "1. Generar escena Lab" y sirve de red de seguridad:
            // si algo falla al generarla, la build se aborta ANTES de gastar minutos
            // compilando sobre una escena potencialmente inválida.
            // Se guardan antes los cambios pendientes del editor (si los hay) porque
            // regenerar la escena hace NewScene(...Single) y machacaría sin avisar
            // cualquier edición sin guardar que hubiera abierta en ese momento.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[ChaosAlchemy] Build cancelada: hay cambios sin guardar en la escena abierta.");
                return;
            }

            try
            {
                AlkahestSceneBuilder.GenerateLabScene();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ChaosAlchemy] Build ABORTADA: no se pudo (re)generar la escena Lab antes de compilar: " + e);
                return;
            }

            if (!File.Exists(ScenePath))
            {
                Debug.LogError("[ChaosAlchemy] Build ABORTADA: sigue sin existir " + ScenePath + " tras intentar generarla.");
                return;
            }

            Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputExe,
                target = BuildTarget.StandaloneWindows64,
                // (pre-vuelo build) Development Build para ESTA PRIMERA verificación
                // tras el rediseño del taller: Debug.Log/LogError llegan al Player.log
                // aunque no haya consola visible, y el overlay "Development Build"
                // confirma a simple vista que el .exe es el de pruebas y no el que se
                // repartiría. Efecto secundario a tener en cuenta: Debug.isDebugBuild
                // pasa a true, así que Dev/DevPalette.cs (F3) queda ACTIVA en este
                // .exe -- deliberado para poder inspeccionar la sim en vivo sin
                // recompilar, pero por eso este build NO es el de distribución. Para
                // el build final de release, quitar BuildOptions.Development (F3
                // desaparece sola, sin tocar DevPalette.cs: ya comprueba
                // Debug.isDebugBuild).
                options = BuildOptions.Development | BuildOptions.ShowBuiltPlayer,
            };

            // (pre-vuelo build) BuildPipeline.BuildPlayer normalmente informa de los
            // fallos de compilación en el propio report (result=Failed), pero un error
            // de configuración puede lanzar una excepción directamente; sin este
            // try/catch se colaría como una traza cruda en la consola y Cesar no
            // vería el resumen ni el diálogo de abajo.
            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ChaosAlchemy] ✘ BUILD FALLIDA — excepción durante BuildPipeline.BuildPlayer: " + e);
                EditorUtility.DisplayDialog("Build FALLIDA", "Excepción durante la build:\n\n" + e.Message, "OK");
                return;
            }

            var summary = report.summary;
            bool ok = summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded && summary.totalErrors == 0;

            // (pre-vuelo build) Resumen claro con el número de errores/avisos: Cesar
            // dispara el menú y necesita ver de un vistazo si algo fue mal, sin tener
            // que rebuscar en la consola.
            string resumen =
                $"resultado={summary.result} | errores={summary.totalErrors} | avisos={summary.totalWarnings} | " +
                $"tamaño={summary.totalSize / (1024f * 1024f):F1} MB | tiempo={summary.totalTime.TotalSeconds:F1}s | " +
                $"salida={OutputExe}";

            if (ok)
            {
                Debug.Log("[ChaosAlchemy] ✔ BUILD OK — " + resumen);
            }
            else
            {
                Debug.LogError("[ChaosAlchemy] ✘ BUILD FALLIDA — " + resumen);
            }

            EditorUtility.DisplayDialog(
                ok ? "Build completada" : "Build FALLIDA",
                (ok ? "Build OK.\n\n" : "La build ha fallado.\n\n") + resumen,
                "OK");
        }

        [MenuItem("Alkahest/5. Abrir carpeta de builds", priority = 5)]
        public static void RevealBuilds()
        {
            Directory.CreateDirectory(OutputDir);
            EditorUtility.RevealInFinder(OutputDir);
        }
    }
}
