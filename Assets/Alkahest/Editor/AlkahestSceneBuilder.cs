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
    /// Herramientas de Editor para la escena de pruebas "AlkahestLab".
    ///
    /// (RONDA 75 — LA ESCENIFICACIÓN) EL GENERADOR YA NO ARRASA: el menú 1
    /// pasa de CREAR-desde-cero a VALIDAR/COMPLETAR — abre la escena
    /// existente, añade SOLO lo que falte (cámara, objeto Alkahest, la
    /// escenografía del prólogo, el muro de fondo) y jamás toca lo que ya
    /// está: las ediciones manuales de Cesar y su hermano SOBREVIVEN a la
    /// regeneración y al pre-vuelo de las builds (la regla 14 evoluciona: la
    /// build sigue pasando por aquí, pero aquí ya no se destruye nada).
    /// La reconstrucción total queda como menú aparte y explícito ("1b",
    /// para la recuperación tras Safe Mode de la regla 20).
    /// </summary>
    public static class AlkahestSceneBuilder
    {
        private const string ScenesFolder = "Assets/Alkahest/Scenes";
        private const string LabScenePath = ScenesFolder + "/AlkahestLab.unity";

        [MenuItem("Ten Thousand Years/1. Validar-completar escena Lab (un jugador)", priority = 1)]
        public static void GenerateLabScene()
        {
            EnsureScenesFolder();

            UnityEngine.SceneManagement.Scene scene;
            bool existia = System.IO.File.Exists(LabScenePath);
            if (existia)
            {
                // VALIDAR: se abre la escena real — nada de NewScene, que era
                // lo que machacaba las ediciones manuales en cada build.
                scene = EditorSceneManager.OpenScene(LabScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            int agregados = 0;
            agregados += ValidarCamara();
            agregados += ValidarAlkahest();
            agregados += ValidarBackdrop();
            agregados += ValidarEscenografiaPrologo();

            bool saved = EditorSceneManager.SaveScene(scene, LabScenePath);
            if (!saved)
            {
                Debug.LogError("[TenThousandYears] No se pudo guardar la escena en " + LabScenePath);
                return;
            }

            UpdateBuildSettings(LabScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[TenThousandYears] Escena Lab " + (existia ? "VALIDADA (piezas añadidas: " + agregados + "; lo existente, intacto)" : "creada desde cero") + " en " + LabScenePath);
        }

        [MenuItem("Ten Thousand Years/1b. REGENERAR escena Lab DESDE CERO (destructivo)", priority = 1)]
        public static void RegenerateLabSceneDesdeCero()
        {
            // El botón rojo: recuperación tras Safe Mode (regla 20) o escena
            // corrupta. DESTRUYE toda edición manual — por eso pide confirmar.
            if (!EditorUtility.DisplayDialog("Regenerar DESDE CERO",
                "Esto DESTRUYE la escena AlkahestLab actual, incluidas TODAS las ediciones manuales (marcadores movidos, arte colocado, etc.), y la reconstruye limpia.\n\n¿Seguro?",
                "Sí, regenerar", "Cancelar"))
                return;

            EnsureScenesFolder();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ValidarCamara();
            ValidarAlkahest();
            ValidarBackdrop();
            ValidarEscenografiaPrologo();
            EditorSceneManager.SaveScene(scene, LabScenePath);
            UpdateBuildSettings(LabScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[TenThousandYears] Escena Lab REGENERADA desde cero en " + LabScenePath);
        }

        // -----------------------------------------------------------------
        // Los validadores: cada uno añade su pieza SOLO si falta y devuelve
        // cuántas cosas creó (0 = la escena ya la tenía). Completan campos
        // NULOS con los assets horneados si existen; jamás pisan un campo ya
        // asignado.
        // -----------------------------------------------------------------
        private static int ValidarCamara()
        {
            if (Object.FindAnyObjectByType<Camera>() != null) return 0;
            BuildMainCamera();
            return 1;
        }

        private static int ValidarAlkahest()
        {
            int n = 0;
            var go = GameObject.Find("Alkahest");
            if (go == null) { BuildAlkahestObject(); return 1; }
            // Completar componentes que falten (sin tocar los presentes).
            if (go.GetComponent<SimRenderer>() == null) { go.AddComponent<SimRenderer>(); n++; }
            if (go.GetComponent<AlkahestSim>() == null) { go.AddComponent<AlkahestSim>(); n++; }
            if (go.GetComponent<DevPalette>() == null) { go.AddComponent<DevPalette>(); n++; }
            if (go.GetComponent<AlkahestGameBootstrap>() == null) { go.AddComponent<AlkahestGameBootstrap>(); n++; }
            return n;
        }

        private static int ValidarBackdrop()
        {
            int n = 0;
            var backdrop = Object.FindAnyObjectByType<WorkshopBackdrop>();
            if (backdrop == null)
            {
                backdrop = new GameObject("WorkshopBackdrop").AddComponent<WorkshopBackdrop>();
                n++;
            }
            // Completar el sprite horneado si existe el asset y el campo está
            // vacío (SerializedObject: el campo es privado a propósito).
            var so = new SerializedObject(backdrop);
            var prop = so.FindProperty("fondoRuinaHorneado");
            if (prop != null && prop.objectReferenceValue == null)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Alkahest/Arte/Prologo/RuinaFondo.png");
                if (sprite != null) { prop.objectReferenceValue = sprite; so.ApplyModifiedPropertiesWithoutUndo(); n++; }
            }
            return n;
        }

        private static int ValidarEscenografiaPrologo()
        {
            int n = 0;
            var esc = Object.FindAnyObjectByType<PrologoEscenografia>();
            if (esc == null)
            {
                var raiz = new GameObject("Prologo_Escenografia");
                esc = raiz.AddComponent<PrologoEscenografia>();
                n++;
            }

            float c = SimRenderer.CellWorldSize;
            if (esc.maestro == null)
            {
                var m = new GameObject("Maestro").transform;
                m.SetParent(esc.transform, false);
                // El rincón histórico: la base de la silueta sobre la mesa.
                m.position = new Vector3(
                    (SimLevelBuilder.FundacionMesaX0 + SimLevelBuilder.FundacionMesaX1) * 0.5f * c,
                    (SimLevelBuilder.FundacionMesaTopY + 1) * c, 0f);
                esc.maestro = m;
                n++;
            }
            if (esc.deposito == null)
            {
                var d = new GameObject("Deposito").transform;
                d.SetParent(esc.transform, false);
                // Base-centro del tanque en su sitio histórico.
                d.position = new Vector3(
                    (SimLevelBuilder.FundacionDepositoX0 + SimLevelBuilder.FundacionDepositoX1 + 1) * 0.5f * c,
                    SimLevelBuilder.FundacionDepositoY0 * c, 0f);
                esc.deposito = d;
                n++;
            }
            if (esc.deposito2 == null)
            {
                // (R83) El silo del lodo, en el hueco medido poza|cráter.
                var d2 = new GameObject("Deposito2_Silo").transform;
                d2.SetParent(esc.transform, false);
                d2.position = new Vector3(
                    (SimLevelBuilder.FundacionSiloX0 + SimLevelBuilder.FundacionSiloX1 + 1) * 0.5f * c,
                    SimLevelBuilder.FundacionSiloY0 * c, 0f);
                esc.deposito2 = d2;
                n++;
            }
            if (esc.depositoFinal == null)
            {
                // (R85, fase B2) Bahía BAJA de la estantería: el tanque de agua reubicado.
                var df = new GameObject("DepositoFinal_BahiaBaja").transform;
                df.SetParent(esc.transform, false);
                df.position = new Vector3(
                    (SimLevelBuilder.FundacionBahiaX0 + 4) * c,   // base-centro de la huella 8 (x389-396).
                    SimLevelBuilder.FundacionBahiaBajaY0 * c, 0f);
                esc.depositoFinal = df;
                n++;
            }
            if (esc.deposito2Final == null)
            {
                // (R85, fase B2) Bahía ALTA: el silo del lodo reubicado (la gotera cae a su boca).
                var d2f = new GameObject("Deposito2Final_BahiaAlta").transform;
                d2f.SetParent(esc.transform, false);
                d2f.position = new Vector3(
                    (SimLevelBuilder.FundacionBahiaX0 + 4) * c,
                    SimLevelBuilder.FundacionBahiaAltaY0 * c, 0f);
                esc.deposito2Final = d2f;
                n++;
            }
            if (esc.guion == null)
            {
                var guion = AssetDatabase.LoadAssetAtPath<GuionDelPrologo>("Assets/Alkahest/Arte/Prologo/GuionDelPrologo.asset");
                if (guion != null) { esc.guion = guion; n++; }
            }
            if (esc.depositoVisualPrefab == null)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Alkahest/Arte/Prologo/DepositoVisual.prefab");
                if (prefab != null) { esc.depositoVisualPrefab = prefab; n++; }
            }
            if (esc.planoOverlay == null)
            {
                // (R77) El overlay del cincel: si Cesar guardó una forma con
                // el botón de la paleta dev, este cableo es lo que la lleva a
                // las BUILDS (en runtime-editor hay un fallback por ruta, pero
                // el player solo ve lo que la escena referencia). Borrar el
                // asset deja el campo como referencia muerta → el fallback de
                // GuionEfectivo/BuscarAsset lo trata como null: plano virgen.
                var overlay = AssetDatabase.LoadAssetAtPath<PlanoOverlay>(PlanoOverlay.RutaAsset);
                if (overlay != null) { esc.planoOverlay = overlay; n++; }
            }
            if (n > 0) EditorUtility.SetDirty(esc);
            return n;
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
