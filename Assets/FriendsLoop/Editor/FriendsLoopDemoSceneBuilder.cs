using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using FriendsLoop.Networking;
using FriendsLoop.Platform;
using FriendsLoop.Session;
using FriendsLoop.Demo;

#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
using Netcode.Transports;
#endif

namespace FriendsLoop.EditorTools
{
    /// <summary>
    /// Genera de forma programática la escena de demostración de FriendsLoop y el prefab del jugador.
    /// Idempotente: se puede volver a ejecutar en cualquier momento y sobreescribirá los assets existentes.
    /// </summary>
    internal static class FriendsLoopDemoSceneBuilder
    {
        private const string DemoFolder = "Assets/FriendsLoop/DemoTest";
        private const string PlayerPrefabPath = DemoFolder + "/FL_Player.prefab";
        private const string ScenePath = DemoFolder + "/FL_DemoScene.unity";

        [MenuItem("FriendsLoop/1. Generar escena demo")]
        public static void GenerateDemoScene()
        {
            EnsureFolder();

            GameObject playerPrefab = BuildPlayerPrefab();
            BuildDemoScene(playerPrefab);

            Debug.Log("✔ Escena demo generada");
        }

        private static void EnsureFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FriendsLoop"))
            {
                AssetDatabase.CreateFolder("Assets", "FriendsLoop");
            }

            if (!AssetDatabase.IsValidFolder(DemoFolder))
            {
                AssetDatabase.CreateFolder("Assets/FriendsLoop", "DemoTest");
            }
        }

        private static GameObject BuildPlayerPrefab()
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            temp.name = "FL_Player";

            temp.AddComponent<NetworkObject>();

            OwnerNetworkTransform ownerTransform = temp.AddComponent<OwnerNetworkTransform>();
            ownerTransform.InLocalSpace = false;
            ownerTransform.SyncPositionX = true;
            ownerTransform.SyncPositionY = true;
            ownerTransform.SyncPositionZ = true;
            ownerTransform.SyncRotAngleX = false;
            ownerTransform.SyncRotAngleY = true;
            ownerTransform.SyncRotAngleZ = false;
            ownerTransform.SyncScaleX = false;
            ownerTransform.SyncScaleY = false;
            ownerTransform.SyncScaleZ = false;

            temp.AddComponent<PlayerController>();
            temp.AddComponent<PlayerIdentity>();

            bool saveSuccess;
            PrefabUtility.SaveAsPrefabAsset(temp, PlayerPrefabPath, out saveSuccess);
            Object.DestroyImmediate(temp);

            if (!saveSuccess)
            {
                Debug.LogError("[FriendsLoop] No se pudo guardar el prefab del jugador en " + PlayerPrefabPath);
                return null;
            }

            GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Debug.Log("[FriendsLoop] Prefab de jugador creado/actualizado: " + PlayerPrefabPath);
            return savedPrefab;
        }

        private static void BuildDemoScene(GameObject playerPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Luz direccional
            var lightGo = new GameObject("FL_Light");
            Light light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Cámara
            var cameraGo = new GameObject("FL_Camera");
            cameraGo.AddComponent<Camera>();
            cameraGo.tag = "MainCamera";
            cameraGo.transform.position = new Vector3(0f, 14f, -14f);
            cameraGo.transform.LookAt(Vector3.zero);

            // Suelo
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "FL_Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(3f, 1f, 3f);
            ApplyNeutralMaterial(ground);

            // Cubo compartido (objeto de red colocado en escena)
            GameObject sharedCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sharedCube.name = "FL_SharedCube";
            sharedCube.transform.position = new Vector3(0f, 0.5f, 3f);
            sharedCube.AddComponent<NetworkObject>();
            sharedCube.AddComponent<SharedInteractable>();

            // Objeto de red / infraestructura
            BuildNetworkObject(playerPrefab);

            EnsureFolder();

            bool sceneSaved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!sceneSaved)
            {
                Debug.LogError("[FriendsLoop] No se pudo guardar la escena demo en " + ScenePath);
                return;
            }

            RegisterSceneInBuildSettings();
        }

        private static void ApplyNeutralMaterial(GameObject ground)
        {
            Renderer renderer = ground.GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
            {
                return;
            }

            var material = new Material(shader) { name = "FL_GroundMaterial" };
            Color neutralGray = new Color(0.62f, 0.62f, 0.62f);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", neutralGray);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", neutralGray);
            }

            renderer.sharedMaterial = material;
        }

        private static void BuildNetworkObject(GameObject playerPrefab)
        {
            var networkGo = new GameObject("FL_Network");

            NetworkManager networkManager = networkGo.AddComponent<NetworkManager>();
            UnityTransport unityTransport = networkGo.AddComponent<UnityTransport>();

#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
            SteamNetworkingSocketsTransport steamTransport = networkGo.AddComponent<SteamNetworkingSocketsTransport>();
#else
            Debug.LogWarning("[FriendsLoop] STEAMWORKSNET/NETCODEGAMEOBJECTS no están activos: la escena demo se generará sin el componente de transporte de Steam.");
#endif

            networkGo.AddComponent<SteamBootstrap>();
            SteamLobbyService steamLobbyService = networkGo.AddComponent<SteamLobbyService>();
            SessionCoordinator sessionCoordinator = networkGo.AddComponent<SessionCoordinator>();
            networkGo.AddComponent<NetDiagnostics>();
            DemoHud demoHud = networkGo.AddComponent<DemoHud>();

            // NetworkConfig: transporte por defecto y prefab de jugador
            networkManager.NetworkConfig.NetworkTransport = unityTransport;

            if (playerPrefab != null)
            {
                networkManager.NetworkConfig.PlayerPrefab = playerPrefab;

                bool alreadyRegistered = networkManager.NetworkConfig.Prefabs.Prefabs
                    .Any(p => p.Prefab == playerPrefab);

                if (!alreadyRegistered)
                {
                    networkManager.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = playerPrefab });
                }
            }
            else
            {
                Debug.LogWarning("[FriendsLoop] No se asignó el prefab de jugador a NetworkConfig porque no se pudo generar.");
            }

            // Referencias de SessionCoordinator (campos privados serializados)
            var serializedCoordinator = new SerializedObject(sessionCoordinator);
            serializedCoordinator.FindProperty("networkManager").objectReferenceValue = networkManager;
            serializedCoordinator.FindProperty("unityTransport").objectReferenceValue = unityTransport;
#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
            SerializedProperty steamTransportProp = serializedCoordinator.FindProperty("steamTransport");
            if (steamTransportProp != null)
            {
                steamTransportProp.objectReferenceValue = steamTransport;
            }
#endif
            serializedCoordinator.FindProperty("steamLobbyService").objectReferenceValue = steamLobbyService;
            serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();

            var serializedHud = new SerializedObject(demoHud);
            serializedHud.FindProperty("sessionCoordinator").objectReferenceValue = sessionCoordinator;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(networkManager);
            EditorUtility.SetDirty(sessionCoordinator);
            EditorUtility.SetDirty(demoHud);
        }

        private static void RegisterSceneInBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            EditorBuildSettings.scenes = scenes;
            Debug.Log("[FriendsLoop] FL_DemoScene registrada en Build Settings (posición 0, habilitada).");
        }
    }
}
