using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Alkahest;
using Alkahest.Game;
using Alkahest.Net;
using Alkahest.Sim;
using FriendsLoop.Demo;
using FriendsLoop.Networking;
using FriendsLoop.Platform;
using FriendsLoop.Session;

#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
using Netcode.Transports;
#endif

namespace Alkahest.EditorTools
{
    /// <summary>
    /// EL TALLER COMPARTIDO (playtest 28): genera por código la escena MULTI y
    /// el prefab del avatar de red. Idempotente, igual que
    /// <see cref="AlkahestSceneBuilder"/>: se puede volver a lanzar siempre.
    ///
    /// La escena Lab CLÁSICA (menú "1. Generar escena Lab") NO SE TOCA — este
    /// archivo escribe en `AlkahestLabMulti.unity`, un .unity distinto, y solo
    /// añade escenas a Build Settings sin quitar las que ya hubiera.
    ///
    /// El cableado de red es una CALCA de
    /// `FriendsLoop/Editor/FriendsLoopDemoSceneBuilder.cs` — mismo objeto único
    /// con NetworkManager + los dos transportes + SteamBootstrap +
    /// SteamLobbyService + SessionCoordinator + NetDiagnostics, las mismas
    /// referencias privadas rellenadas con SerializedObject, y el mismo patrón
    /// de registro del prefab de jugador (`NetworkConfig.PlayerPrefab` + una
    /// entrada en `NetworkConfig.Prefabs`). Lo único propio del taller es lo
    /// que cuelga de esa base: el objeto de simulación, el SimSync (en su
    /// PROPIO GameObject con NetworkObject: un NetworkBehaviour no puede
    /// compartir GameObject con el NetworkManager) y el HUD de sesión.
    /// </summary>
    public static class AlkahestNetSceneBuilder
    {
        private const string ScenesFolder = "Assets/Alkahest/Scenes";
        private const string NetFolder = "Assets/Alkahest/Net";
        private const string MultiScenePath = ScenesFolder + "/AlkahestLabMulti.unity";
        private const string AvatarPrefabPath = NetFolder + "/AlkahestAprendizRed.prefab";

        private const string OutputDir = "Builds/ChaosAlchemyMulti";
        private const string OutputExe = OutputDir + "/ChaosAlchemyMulti.exe";

        [MenuItem("Alkahest/2. Generar escena Lab MULTI (taller compartido)", priority = 2)]
        public static void GenerateLabMultiScene()
        {
            EnsureFolders();

            GameObject avatarPrefab = BuildAvatarPrefab();
            BuildMultiScene(avatarPrefab);

            AssetDatabase.Refresh();
            Debug.Log("[ChaosAlchemy] Escena MULTI generada/actualizada en " + MultiScenePath);
        }

        /// <summary>
        /// Build de la escena MULTI, aparte de la del modo de un jugador.
        /// REGLA 14 de CLAUDE.md: regenera la escena antes de compilar — nunca
        /// confiar en el .unity guardado en el repo.
        /// </summary>
        [MenuItem("Alkahest/4. Build MULTI Windows (taller compartido)", priority = 4)]
        public static void BuildMultiWindows()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("[ChaosAlchemy] Build MULTI cancelada: hay cambios sin guardar en la escena abierta.");
                return;
            }

            try
            {
                GenerateLabMultiScene();
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ChaosAlchemy] Build MULTI ABORTADA: no se pudo (re)generar la escena. " + e);
                return;
            }

            if (!File.Exists(MultiScenePath))
            {
                Debug.LogError("[ChaosAlchemy] Build MULTI ABORTADA: sigue sin existir " + MultiScenePath + ".");
                return;
            }

            Directory.CreateDirectory(OutputDir);
            var options = new BuildPlayerOptions
            {
                // SOLO la escena MULTI, y en el índice 0: el .exe arranca
                // directo en el taller compartido, sin pasar por la escena de
                // un jugador (que tiene su propio build en AlkahestBuildTools).
                scenes = new[] { MultiScenePath },
                locationPathName = OutputExe,
                target = BuildTarget.StandaloneWindows64,
                // Development: Player.log con todos los Debug.Log de red, que
                // es justo lo que hay que leer si la sesión no levanta.
                options = BuildOptions.Development | BuildOptions.ShowBuiltPlayer,
            };

            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(options);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ChaosAlchemy] ✘ BUILD MULTI FALLIDA — excepción: " + e);
                return;
            }

            var summary = report.summary;
            bool ok = summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded && summary.totalErrors == 0;
            string resumen = $"resultado={summary.result} | errores={summary.totalErrors} | avisos={summary.totalWarnings} | salida={OutputExe}";

            // (fix playtest 29, reporte de Cesar) STEAM_APPID.TXT JUNTO AL EXE:
            // sin él, SteamAPI.Init() FALLA aunque el cliente de Steam esté
            // abierto -- Cesar, CON Steam corriendo, veía "no estás conectado".
            // Mismo gesto que FriendsLoopBuildTools (el build tool de la demo
            // ya lo hacía y este no lo heredó): App ID 480 de desarrollo;
            // NUNCA distribuir este archivo en una build de tienda.
            if (ok)
            {
                try
                {
                    string appIdPath = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(OutputExe)), "steam_appid.txt");
                    System.IO.File.WriteAllText(appIdPath, "480");
                    Debug.Log("[ChaosAlchemy] steam_appid.txt (480, desarrollo) escrito junto al ejecutable.");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[ChaosAlchemy] No se pudo escribir steam_appid.txt junto al exe: " + e.Message);
                }
            }

            if (ok) Debug.Log("[ChaosAlchemy] ✔ BUILD MULTI OK — " + resumen);
            else Debug.LogError("[ChaosAlchemy] ✘ BUILD MULTI FALLIDA — " + resumen);

            EditorUtility.DisplayDialog(ok ? "Build MULTI completada" : "Build MULTI FALLIDA",
                (ok ? "Build OK.\n\nPara probar en este PC: abre DOS veces el .exe.\n" +
                      "  1) el primero con  -transport local  y pulsa ANFITRIÓN\n" +
                      "  2) el segundo con  -transport local  y pulsa UNIRME en local\n\n"
                    : "La build ha fallado.\n\n") + resumen, "OK");
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(ScenesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Alkahest", "Scenes");
            }

            if (!AssetDatabase.IsValidFolder(NetFolder))
            {
                AssetDatabase.CreateFolder("Assets/Alkahest", "Net");
            }
        }

        // =================================================================
        // EL PREFAB DEL AVATAR
        // =================================================================

        /// <summary>
        /// El aprendiz en red, montado por código (cero assets, como todo en
        /// este proyecto). Orden de componentes deliberado: NetworkObject
        /// primero (lo exigen los cuatro NetworkBehaviour) y
        /// ApprenticeController antes que frasco/cincel/mudanza (los tres lo
        /// declaran con [RequireComponent], y añadirlos antes haría que Unity
        /// lo insertara solo, en otro orden).
        /// </summary>
        private static GameObject BuildAvatarPrefab()
        {
            var temp = new GameObject("AlkahestAprendizRed");

            temp.AddComponent<NetworkObject>();

            OwnerNetworkTransform ownerTransform = temp.AddComponent<OwnerNetworkTransform>();
            ownerTransform.InLocalSpace = false;
            // El taller es 2D en el plano XY: la Z del aprendiz es siempre 0
            // (ver ApprenticeController.HandleMovement) y no hay rotación de
            // personaje — el bandeo lo hace un hijo "Tilt" que cada cliente
            // anima por su cuenta, así que no hay que mandarlo por la red.
            ownerTransform.SyncPositionX = true;
            ownerTransform.SyncPositionY = true;
            ownerTransform.SyncPositionZ = false;
            ownerTransform.SyncRotAngleX = false;
            ownerTransform.SyncRotAngleY = false;
            ownerTransform.SyncRotAngleZ = false;
            ownerTransform.SyncScaleX = false;
            ownerTransform.SyncScaleY = false;
            ownerTransform.SyncScaleZ = false;

            // El nombre flotante de Steam, tal cual del template. Sus dos
            // campos serializados vienen calibrados para una cápsula de 2
            // unidades; el imp mide ~0.85, así que se bajan por
            // SerializedObject (son [SerializeField] privados y el template no
            // se toca).
            PlayerIdentity identity = temp.AddComponent<PlayerIdentity>();
            var serializedIdentity = new SerializedObject(identity);
            var offsetProp = serializedIdentity.FindProperty("labelLocalOffset");
            if (offsetProp != null) offsetProp.vector3Value = new Vector3(0f, 0.62f, 0f);
            var fontProp = serializedIdentity.FindProperty("labelFontSize");
            if (fontProp != null) fontProp.intValue = 14;
            serializedIdentity.ApplyModifiedPropertiesWithoutUndo();

            temp.AddComponent<ApprenticeController>();
            temp.AddComponent<Flask>();
            temp.AddComponent<Cincel>();
            temp.AddComponent<Mudanza>();
            temp.AddComponent<SubstanceKnowledge>();
            temp.AddComponent<FlaskHud>();

            // El último: en su OnNetworkSpawn reparte control y color, y para
            // entonces todos los de arriba ya existen.
            temp.AddComponent<AprendizNet>();

            PrefabUtility.SaveAsPrefabAsset(temp, AvatarPrefabPath, out bool saveSuccess);
            Object.DestroyImmediate(temp);

            if (!saveSuccess)
            {
                Debug.LogError("[ChaosAlchemy] No se pudo guardar el prefab del avatar en " + AvatarPrefabPath);
                return null;
            }

            GameObject saved = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefabPath);
            Debug.Log("[ChaosAlchemy] Prefab de avatar de red creado/actualizado: " + AvatarPrefabPath);
            return saved;
        }

        // =================================================================
        // LA ESCENA
        // =================================================================

        private static void BuildMultiScene(GameObject avatarPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildMainCamera();
            BuildAlkahestObject();
            BuildSimSyncObject();
            BuildNetworkObject(avatarPrefab);

            bool saved = EditorSceneManager.SaveScene(scene, MultiScenePath);
            if (!saved)
            {
                Debug.LogError("[ChaosAlchemy] No se pudo guardar la escena MULTI en " + MultiScenePath);
                return;
            }

            RegistrarEnBuildSettings();
        }

        /// <summary>
        /// Misma cámara de partida que la escena clásica (ver el docblock de
        /// <see cref="AlkahestSceneBuilder"/>): ortográfica, de una pantalla de
        /// alto, con el color de fondo del taller. `SimRenderer.Init` la
        /// recoloca sobre el aprendiz LOCAL en el primer frame real. Se
        /// duplica el método en vez de exponer el de la escena clásica para no
        /// tocar ese archivo (que es lo que arranca en la build de un
        /// jugador).
        /// </summary>
        private static void BuildMainCamera()
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";

            float mundoW = CellGrid.W * SimRenderer.CellWorldSize;
            float mundoH = CellGrid.H * SimRenderer.CellWorldSize;
            camGO.transform.position = new Vector3(mundoW * 0.5f, mundoH * 0.5f, -10f);
            camGO.transform.rotation = Quaternion.identity;

            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CellGrid.PantallaH * 0.5f * SimRenderer.CellWorldSize;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SimRenderer.BackgroundColor;

            camGO.AddComponent<AudioListener>();
        }

        /// <summary>
        /// El objeto de simulación. SIN DevPalette a propósito: su panel (F3)
        /// lee `_sim.Stepper.LastStepMs` sin comprobar nulos, y en un invitado
        /// el stepper NO EXISTE (ver AlkahestSim.ModoEspejo) — abrir F3 en el
        /// cliente sería una excepción por frame. En la escena de un jugador
        /// sigue estando, sin cambios.
        /// </summary>
        private static void BuildAlkahestObject()
        {
            var go = new GameObject("Alkahest");
            go.AddComponent<SimRenderer>();
            go.AddComponent<AlkahestSim>();
            go.AddComponent<AlkahestGameBootstrap>();
        }

        /// <summary>
        /// SimSync vive en su PROPIO GameObject con NetworkObject: NGO no
        /// permite un NetworkObject en el mismo GameObject que el
        /// NetworkManager. Es un objeto de ESCENA (mismo patrón que el
        /// FL_SharedCube de la demo del template): NGO lo spawnea solo al
        /// arrancar la sesión y lo sincroniza a cada cliente que entra.
        /// </summary>
        private static void BuildSimSyncObject()
        {
            // DUDA-API: un NetworkObject COLOCADO EN ESCENA lo spawnea NGO
            // solo al arrancar la sesión y lo sincroniza a cada cliente que
            // entra (requiere NetworkConfig.EnableSceneManagement, que está en
            // true por defecto y no se toca aquí). Es literalmente lo que hace
            // el FL_SharedCube de la demo del template, así que es el patrón
            // probado de este proyecto y no una apuesta.
            var go = new GameObject("AlkahestSimSync");
            go.AddComponent<NetworkObject>();
            go.AddComponent<SimSync>();
        }

        private static void BuildNetworkObject(GameObject avatarPrefab)
        {
            var networkGo = new GameObject("AlkahestRed");

            NetworkManager networkManager = networkGo.AddComponent<NetworkManager>();
            UnityTransport unityTransport = networkGo.AddComponent<UnityTransport>();

#if !DISABLESTEAMWORKS && STEAMWORKSNET && NETCODEGAMEOBJECTS
            SteamNetworkingSocketsTransport steamTransport = networkGo.AddComponent<SteamNetworkingSocketsTransport>();
#else
            Debug.LogWarning("[ChaosAlchemy] STEAMWORKSNET/NETCODEGAMEOBJECTS no están activos: la escena MULTI se genera SIN el transporte de Steam (solo loopback local).");
#endif

            networkGo.AddComponent<SteamBootstrap>();
            SteamLobbyService steamLobbyService = networkGo.AddComponent<SteamLobbyService>();
            SessionCoordinator sessionCoordinator = networkGo.AddComponent<SessionCoordinator>();
            // (fix playtest 29, reporte de Cesar: "No hay una referencia a
            // NetworkManager configurada") El cableado de referencias vivía
            // AL FINAL del método: cualquier excepción intermedia dejaba el
            // coordinador creado pero SIN cablear, y la escena guardaba ese
            // estado a medias -- el error solo aparecía al pulsar ANFITRIÓN.
            // Ahora las referencias críticas se asignan AQUÍ MISMO, en la
            // línea siguiente a su creación: no existe ventana de fallo.
            {
                var wiringInmediato = new SerializedObject(sessionCoordinator);
                wiringInmediato.FindProperty("networkManager").objectReferenceValue = networkManager;
                wiringInmediato.FindProperty("unityTransport").objectReferenceValue = unityTransport;
                wiringInmediato.ApplyModifiedPropertiesWithoutUndo();
            }
            networkGo.AddComponent<NetDiagnostics>();
            TallerSesionHud sesionHud = networkGo.AddComponent<TallerSesionHud>();

            networkManager.NetworkConfig.NetworkTransport = unityTransport;

            if (avatarPrefab != null)
            {
                // El PlayerPrefab del NetworkConfig: NGO instancia uno por
                // cliente automáticamente al conectar y le da la propiedad a
                // ese cliente. Es el mismo mecanismo (y el mismo registro
                // doble: PlayerPrefab + lista de prefabs) que usa la demo del
                // template — nada de spawn manual desde código.
                networkManager.NetworkConfig.PlayerPrefab = avatarPrefab;

                // (fix 2 del arranque, error 'duplicate GlobalObjectIdHash')
                // El registro en la LISTA de prefabs vive ahora en UN SOLO
                // sitio: SimSync.Awake, en runtime. Registrarlo también aquí
                // (como hacía la primera versión) duplicaba la entrada al
                // arrancar y NGO INVALIDABA el registro entero -- el avatar
                // no spawneaba y ANFITRIÓN parecía no hacer nada. El editor
                // solo asigna PlayerPrefab; la lista la puebla el runtime.
            }
            else
            {
                Debug.LogWarning("[ChaosAlchemy] No se asignó el prefab de avatar al NetworkConfig porque no se pudo generar.");
            }

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

            var serializedHud = new SerializedObject(sesionHud);
            serializedHud.FindProperty("sessionCoordinator").objectReferenceValue = sessionCoordinator;
            serializedHud.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(networkManager);
            EditorUtility.SetDirty(sessionCoordinator);
            EditorUtility.SetDirty(sesionHud);
        }

        /// <summary>
        /// Añade la escena MULTI a Build Settings SIN tocar el orden de lo que
        /// ya hubiera: la escena Lab clásica sigue donde estaba (índice 0 si la
        /// generó su propio menú), que es lo que la build de un jugador
        /// espera. El build MULTI de este archivo pasa su escena explícitamente
        /// en `BuildPlayerOptions.scenes`, así que no depende de este orden.
        /// </summary>
        private static void RegistrarEnBuildSettings()
        {
            var actuales = EditorBuildSettings.scenes;
            foreach (var s in actuales)
            {
                if (s.path == MultiScenePath) return; // ya estaba
            }

            var lista = new List<EditorBuildSettingsScene>(actuales)
            {
                new EditorBuildSettingsScene(MultiScenePath, true)
            };

            EditorBuildSettings.scenes = lista.ToArray();
            Debug.Log("[ChaosAlchemy] AlkahestLabMulti añadida a Build Settings.");
        }
    }
}
