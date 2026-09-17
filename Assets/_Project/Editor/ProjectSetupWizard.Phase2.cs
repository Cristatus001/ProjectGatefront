using System;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ProjectGatefront.Editor
{
    public static partial class ProjectSetupWizard
    {
        private const string GameplayScenePath = "Assets/_Project/Scenes/02_Gameplay.unity";
        private const string PlayerConfigPath = "Assets/_Project/Data/Configs/PlayerConfig.asset";
        private const string PlayerPrefabPath =
            "Assets/_Project/Prefabs/Characters/PF_Player.prefab";
        private const string PrototypeMaterialsFolder =
            "Assets/_Project/Art/Materials/Prototype";

        [MenuItem("Tools/Project Gatefront/Setup Phase 2 Gameplay Prototype")]
        public static void SetupPhase2GameplayPrototype()
        {
            if (!Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureVersionControl();
            EnsureFolders();
            EnsureFolder(PrototypeMaterialsFolder);

            Material groundMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeGround",
                new Color(0.18f, 0.23f, 0.25f));
            Material playerMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypePlayer",
                new Color(0.08f, 0.55f, 0.72f));
            Material boundaryMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeBoundary",
                new Color(0.95f, 0.48f, 0.08f));
            Material environmentMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeEnvironment",
                new Color(0.48f, 0.52f, 0.46f));

            PlayerConfig playerConfig = EnsurePlayerConfig();
            GameObject playerPrefab = EnsurePlayerPrefab(playerMaterial, boundaryMaterial);
            SetupGameplayScene(
                playerConfig,
                playerPrefab,
                groundMaterial,
                boundaryMaterial,
                environmentMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Project Gatefront] Phase 2 gameplay prototype setup completed.");
        }

        private static Material EnsurePrototypeMaterial(string materialName, Color color)
        {
            string path = $"{PrototypeMaterialsFolder}/{materialName}.mat";
            Material existingMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existingMaterial != null)
            {
                return existingMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "The Universal Render Pipeline Lit shader is unavailable.");
            }

            var material = new Material(shader)
            {
                name = materialName,
                color = color
            };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static PlayerConfig EnsurePlayerConfig()
        {
            PlayerConfig playerConfig = AssetDatabase.LoadAssetAtPath<PlayerConfig>(
                PlayerConfigPath);
            if (playerConfig != null)
            {
                return playerConfig;
            }

            playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();
            AssetDatabase.CreateAsset(playerConfig, PlayerConfigPath);
            return playerConfig;
        }

        private static GameObject EnsurePlayerPrefab(
            Material playerMaterial,
            Material orientationMaterial)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                PlayerPrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject root = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(PlayerPrefabPath)
                : new GameObject("PlayerRoot");

            try
            {
                CharacterController characterController =
                    EnsureComponent<CharacterController>(root, out bool controllerCreated);
                if (controllerCreated)
                {
                    characterController.center = new Vector3(0f, 1f, 0f);
                    characterController.height = 2f;
                    characterController.radius = 0.42f;
                    characterController.stepOffset = 0.2f;
                    characterController.slopeLimit = 45f;
                }

                EnsureComponent<PlayerController>(root, out _);

                Transform visual = EnsureChild(root.transform, "Visual", out _);
                EnsurePrefabPrimitive(
                    visual,
                    "Body",
                    PrimitiveType.Capsule,
                    new Vector3(0f, 1f, 0f),
                    new Vector3(0.8f, 1f, 0.8f),
                    playerMaterial);
                EnsurePrefabPrimitive(
                    visual,
                    "HeadMarker",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 2.08f, 0f),
                    new Vector3(0.55f, 0.4f, 0.55f),
                    playerMaterial);
                EnsurePrefabPrimitive(
                    visual,
                    "ForwardMarker",
                    PrimitiveType.Cube,
                    new Vector3(0f, 1.25f, 0.52f),
                    new Vector3(0.18f, 0.18f, 0.65f),
                    orientationMaterial);

                Transform cameraTarget = EnsureChild(
                    root.transform,
                    "CameraTarget",
                    out bool cameraTargetCreated);
                if (cameraTargetCreated)
                {
                    cameraTarget.localPosition = new Vector3(0f, 1.35f, 0.4f);
                }

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                if (loadedPrefabContents)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        }

        private static void EnsurePrefabPrimitive(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
            {
                return;
            }

            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = localScale;
            primitive.GetComponent<MeshRenderer>().sharedMaterial = material;

            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }
        }

        private static void SetupGameplayScene(
            PlayerConfig playerConfig,
            GameObject playerPrefab,
            Material groundMaterial,
            Material boundaryMaterial,
            Material environmentMaterial)
        {
            Scene scene = EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single);

            GameObject systemsRoot = EnsureSceneRoot(scene, "Systems", out _);
            GameplayBootstrap gameplayBootstrap =
                EnsureComponent<GameplayBootstrap>(systemsRoot, out _);

            GameObject playerRoot = EnsureSceneRoot(scene, "Player", out _);
            PlayerController playerController =
                playerRoot.GetComponentInChildren<PlayerController>(true);
            if (playerController == null)
            {
                GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                    playerPrefab,
                    scene);
                playerInstance.transform.SetParent(playerRoot.transform, false);
                playerInstance.transform.position = new Vector3(0f, 0f, 2f);
                playerInstance.transform.rotation = Quaternion.identity;
                playerController = playerInstance.GetComponent<PlayerController>();
            }

            if (playerController.transform.name == "PF_Player")
            {
                playerController.transform.name = "PlayerRoot";
            }

            Transform cameraTarget = playerController.transform.Find("CameraTarget");
            if (cameraTarget == null)
            {
                throw new InvalidOperationException(
                    "PF_Player must contain a direct CameraTarget child.");
            }

            var serializedPlayer = new SerializedObject(playerController);
            serializedPlayer.FindProperty("config").objectReferenceValue = playerConfig;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            GameObject cameraRoot = EnsureSceneRoot(scene, "Camera", out _);
            Transform cameraTransform = EnsureChild(
                cameraRoot.transform,
                "RunnerCamera",
                out bool cameraObjectCreated);
            Camera cameraComponent = EnsureComponent<Camera>(
                cameraTransform.gameObject,
                out bool cameraCreated);
            if (cameraCreated)
            {
                cameraComponent.fieldOfView = 58f;
                cameraComponent.nearClipPlane = 0.1f;
                cameraComponent.farClipPlane = 300f;
                cameraComponent.clearFlags = CameraClearFlags.Skybox;
            }

            if (cameraTransform.gameObject.CompareTag("Untagged"))
            {
                cameraTransform.gameObject.tag = "MainCamera";
            }

            EnsureComponent<AudioListener>(cameraTransform.gameObject, out _);
            RunnerCameraController cameraController =
                EnsureComponent<RunnerCameraController>(cameraTransform.gameObject, out _);
            var serializedCamera = new SerializedObject(cameraController);
            serializedCamera.FindProperty("followTarget").objectReferenceValue = cameraTarget;
            serializedCamera.ApplyModifiedPropertiesWithoutUndo();
            if (cameraObjectCreated)
            {
                cameraTransform.position = new Vector3(0f, 8.85f, -7.6f);
                cameraTransform.rotation = Quaternion.Euler(31f, 0f, 0f);
            }

            GameObject environmentRoot = EnsureSceneRoot(scene, "Environment", out _);
            Transform trackRoot = EnsureChild(
                environmentRoot.transform,
                "Track",
                out _);
            Transform boundariesRoot = EnsureChild(
                environmentRoot.transform,
                "Boundaries",
                out _);
            Transform decorationRoot = EnsureChild(
                environmentRoot.transform,
                "Decoration",
                out _);
            Transform lightingRoot = EnsureChild(
                environmentRoot.transform,
                "Lighting",
                out _);

            SetupTrack(trackRoot, groundMaterial, boundaryMaterial);
            SetupBoundaries(boundariesRoot, boundaryMaterial);
            SetupDecoration(decorationRoot, environmentMaterial);
            SetupLighting(lightingRoot);

            gameplayBootstrap.Configure(
                playerConfig,
                playerController,
                cameraController,
                cameraTarget);
            EditorUtility.SetDirty(gameplayBootstrap);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.46f, 0.55f, 0.65f);
            RenderSettings.ambientEquatorColor = new Color(0.28f, 0.32f, 0.34f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.13f, 0.12f);
            RenderSettings.ambientIntensity = 0.8f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetupTrack(
            Transform trackRoot,
            Material groundMaterial,
            Material markerMaterial)
        {
            EnsureScenePrimitive(
                trackRoot,
                "GroundTrack",
                PrimitiveType.Cube,
                new Vector3(0f, -0.25f, 90f),
                new Vector3(10f, 0.5f, 180f),
                groundMaterial,
                true);
            EnsureScenePrimitive(
                trackRoot,
                "StartArea",
                PrimitiveType.Cube,
                new Vector3(0f, 0.02f, 2f),
                new Vector3(9.6f, 0.04f, 4f),
                markerMaterial,
                false);

            Transform markersRoot = EnsureChild(trackRoot, "DistanceMarkers", out _);
            for (int z = 15; z <= 165; z += 15)
            {
                EnsureScenePrimitive(
                    markersRoot,
                    $"DistanceMarker_{z:000}",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.02f, z),
                    new Vector3(9.4f, 0.04f, 0.22f),
                    markerMaterial,
                    false);
            }
        }

        private static void SetupBoundaries(
            Transform boundariesRoot,
            Material boundaryMaterial)
        {
            EnsureScenePrimitive(
                boundariesRoot,
                "LeftBoundary",
                PrimitiveType.Cube,
                new Vector3(-5.35f, 0.5f, 90f),
                new Vector3(0.3f, 1f, 180f),
                boundaryMaterial,
                false);
            EnsureScenePrimitive(
                boundariesRoot,
                "RightBoundary",
                PrimitiveType.Cube,
                new Vector3(5.35f, 0.5f, 90f),
                new Vector3(0.3f, 1f, 180f),
                boundaryMaterial,
                false);
        }

        private static void SetupDecoration(
            Transform decorationRoot,
            Material environmentMaterial)
        {
            for (int z = 20; z <= 160; z += 20)
            {
                EnsureScenePrimitive(
                    decorationRoot,
                    $"CrateLeft_{z:000}",
                    PrimitiveType.Cube,
                    new Vector3(-7f, 0.7f, z),
                    new Vector3(1.4f, 1.4f, 1.4f),
                    environmentMaterial,
                    false);
                EnsureScenePrimitive(
                    decorationRoot,
                    $"PillarRight_{z:000}",
                    PrimitiveType.Cylinder,
                    new Vector3(7f, 1f, z + 7f),
                    new Vector3(0.65f, 1f, 0.65f),
                    environmentMaterial,
                    false);

                if (z % 40 == 0)
                {
                    EnsureScenePrimitive(
                        decorationRoot,
                        $"LowWallLeft_{z:000}",
                        PrimitiveType.Cube,
                        new Vector3(-7.2f, 0.45f, z + 7f),
                        new Vector3(2.8f, 0.9f, 0.45f),
                        environmentMaterial,
                        false);
                }
            }
        }

        private static void SetupLighting(Transform lightingRoot)
        {
            Transform lightTransform = EnsureChild(
                lightingRoot,
                "Directional Light",
                out bool lightObjectCreated);
            Light directionalLight = EnsureComponent<Light>(
                lightTransform.gameObject,
                out bool lightCreated);
            if (lightCreated)
            {
                directionalLight.type = LightType.Directional;
                directionalLight.color = new Color(1f, 0.96f, 0.88f);
                directionalLight.intensity = 1.15f;
                directionalLight.shadows = LightShadows.Soft;
            }

            if (lightObjectCreated)
            {
                lightTransform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private static GameObject EnsureScenePrimitive(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale,
            Material material,
            bool keepCollider)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);
            primitive.transform.position = position;
            primitive.transform.rotation = Quaternion.identity;
            primitive.transform.localScale = scale;
            primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
            primitive.isStatic = true;

            if (!keepCollider)
            {
                Collider collider = primitive.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            return primitive;
        }

        private static GameObject EnsureSceneRoot(
            Scene scene,
            string objectName,
            out bool created)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    created = false;
                    return root;
                }
            }

            var gameObject = new GameObject(objectName);
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            created = true;
            return gameObject;
        }

        private static Transform EnsureChild(
            Transform parent,
            string objectName,
            out bool created)
        {
            Transform existing = parent.Find(objectName);
            if (existing != null)
            {
                created = false;
                return existing;
            }

            var gameObject = new GameObject(objectName);
            gameObject.transform.SetParent(parent, false);
            created = true;
            return gameObject.transform;
        }

        private static T EnsureComponent<T>(GameObject gameObject, out bool created)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                created = false;
                return component;
            }

            created = true;
            return gameObject.AddComponent<T>();
        }
    }
}
