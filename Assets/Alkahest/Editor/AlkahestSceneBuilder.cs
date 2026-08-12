using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Alkahest;
using Alkahest.Sim;
using Alkahest.Dev;
using Alkahest.Game;

namespace Alkahest.EditorTools
{
    /// <summary>
    /// Herramientas de Editor para generar/regenerar la escena de pruebas
    /// "AlkahestLab" desde cero por código (idempotente: cada ejecución
    /// recrea la escena limpia y machaca la anterior en disco).
    /// </summary>
    public static class AlkahestSceneBuilder
    {
        private const string ScenesFolder = "Assets/Alkahest/Scenes";
        private const string LabScenePath = ScenesFolder + "/AlkahestLab.unity";

        [MenuItem("Alkahest/1. Generar escena Lab")]
        public static void GenerateLabScene()
        {
            EnsureScenesFolder();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildMainCamera();
            BuildAlkahestObject();

            bool saved = EditorSceneManager.SaveScene(scene, LabScenePath);
            if (!saved)
            {
                Debug.LogError("[Alkahest] No se pudo guardar la escena en " + LabScenePath);
                return;
            }

            UpdateBuildSettings(LabScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[Alkahest] Escena Lab generada/actualizada en " + LabScenePath);
        }

        private static void EnsureScenesFolder()
        {
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Alkahest", "Scenes");
            }
        }

        private static void BuildMainCamera()
        {
            // La cámara encuadra EXACTAMENTE la grilla: se deriva de
            // CellGrid.W/H para que nunca se quede desfasada si el tamaño del
            // mundo vuelve a cambiar (con 256x144 sale centro (12.8, 7.2) y
            // orthographicSize 7.2; antes eran (19.2, 10.8) y 10.8 hardcodeados).
            float mundoW = CellGrid.W * SimRenderer.CellWorldSize;
            float mundoH = CellGrid.H * SimRenderer.CellWorldSize;

            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            camGO.transform.position = new Vector3(mundoW * 0.5f, mundoH * 0.5f, -10f);
            camGO.transform.rotation = Quaternion.identity;

            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = mundoH * 0.5f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SimRenderer.BackgroundColor;

            camGO.AddComponent<AudioListener>();
        }

        private static void BuildAlkahestObject()
        {
            var go = new GameObject("Alkahest");
            // El orden importa poco: AlkahestSim busca su SimRenderer vía
            // GetComponent en Awake(), así que basta con que estén en el
            // mismo GameObject.
            go.AddComponent<SimRenderer>();
            go.AddComponent<AlkahestSim>();
            go.AddComponent<DevPalette>();

            // Capa de interacción M2: idempotente (no duplica el componente
            // si ya estuviera presente en el GameObject).
            if (go.GetComponent<AlkahestGameBootstrap>() == null)
            {
                go.AddComponent<AlkahestGameBootstrap>();
            }
        }

        /// <summary>Deja AlkahestLab en el índice 0 de Build Settings, conservando FL_DemoScene (si existe) en el índice 1 y cualquier otra escena a continuación.</summary>
        private static void UpdateBuildSettings(string labScenePath)
        {
            var existing = EditorBuildSettings.scenes;

            EditorBuildSettingsScene demoScene = null;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path.Contains("FL_DemoScene"))
                {
                    demoScene = existing[i];
                    break;
                }
            }

            var newList = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(labScenePath, true)
            };

            if (demoScene != null && demoScene.path != labScenePath)
            {
                newList.Add(new EditorBuildSettingsScene(demoScene.path, demoScene.enabled));
            }

            for (int i = 0; i < existing.Length; i++)
            {
                string p = existing[i].path;
                if (p == labScenePath) continue;
                if (demoScene != null && p == demoScene.path) continue;
                newList.Add(new EditorBuildSettingsScene(p, existing[i].enabled));
            }

            EditorBuildSettings.scenes = newList.ToArray();
        }
    }
}
