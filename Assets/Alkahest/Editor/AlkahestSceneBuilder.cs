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

        [MenuItem("Alkahest/1. Generar escena Lab (un jugador)", priority = 1)]
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

        /// <summary>
        /// (playtest 15) YA NO encuadra el mundo entero. Hasta esta ronda
        /// derivaba centro/orthographicSize de CellGrid.W/H para encajar la
        /// grilla completa -- correcto mientras el mundo medía una pantalla,
        /// pero con el taller a 768x288 (3x2 pantallas) eso dejaría cada celda
        /// a 1/6 de su tamaño en pantalla, y de todos modos ya no hace falta:
        /// Sim/SimRenderer.cs POSEE la cámara en runtime (FitMainCamera +
        /// UpdateCameraFollow, ver su docblock) -- la sigue, con zona muerta y
        /// suavizado, mostrando ~una pantalla, y la amplía temporalmente con
        /// Tab. Este método solo tiene que dejarla en un estado INICIAL
        /// razonable (ortográfica, en algún punto dentro del mundo, con el
        /// color de fondo correcto) para que SimRenderer.Init() -- que llama a
        /// FitMainCamera() y UpdateCameraFollow(snap:true) nada más arrancar,
        /// ANTES del primer frame visible -- tenga algo coherente de lo que
        /// partir; no pelea con ella (no fija tamaño ni centro finales, que
        /// SimRenderer sobreescribe de todas formas en su primer Init()).
        ///
        /// REGLA 14 (CLAUDE.md): el builder de la build REGENERA la escena
        /// antes de compilar, así que lo que este método deja aquí es
        /// exactamente lo que arranca en el .exe -- SimRenderer.Init() corre
        /// en el primer frame de juego real (Awake/Start de AlkahestSim) y
        /// corrige tamaño/posición antes de que el jugador vea nada, así que
        /// dejar aquí un tamaño "razonable pero no definitivo" no rompe la
        /// build: es un placeholder de un frame, nunca lo que el jugador ve.
        /// </summary>
        private static void BuildMainCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            // Punto de partida razonable: el centro del mundo. SimRenderer
            // la reposiciona sobre el aprendiz en su primer Init() (o al
            // centro del mundo si el aprendiz todavía no existe, ver
            // UpdateCameraFollow) -- este valor solo evita que la escena
            // recién generada muestre una cámara en el origen (0,0), fuera de
            // toda la grilla, si algo mirase la escena antes de que el juego
            // arranque (p.ej. la vista de Editor sin Play).
            float mundoW = CellGrid.W * SimRenderer.CellWorldSize;
            float mundoH = CellGrid.H * SimRenderer.CellWorldSize;
            camGO.transform.position = new Vector3(mundoW * 0.5f, mundoH * 0.5f, -10f);
            camGO.transform.rotation = Quaternion.identity;

            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            // Tamaño de partida = una pantalla de alto (mismo criterio que
            // SimRenderer.FitMainCamera, sin duplicar su fórmula de aspect):
            // más cercano al tamaño real que fijará SimRenderer que "el mundo
            // entero", así que si algo llega a pintar un frame con este valor
            // (antes de que Init() corra) no se ve una discontinuidad enorme.
            cam.orthographicSize = CellGrid.PantallaH * 0.5f * SimRenderer.CellWorldSize;
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
