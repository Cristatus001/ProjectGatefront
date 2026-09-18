using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Player;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGatefront.Editor
{
    public static partial class ProjectSetupWizard
    {
        private const string GameBalanceConfigPath =
            "Assets/_Project/Data/Configs/GameBalanceConfig.asset";
        private const string TrackChunkPrefabPath =
            "Assets/_Project/Prefabs/Environment/PF_TrackChunk.prefab";

        [MenuItem("Tools/Project Gatefront/Setup Phase 3.5 Moving World Prototype")]
        public static void SetupPhase35MovingWorldPrototype()
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
            Material boundaryMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeBoundary",
                new Color(0.95f, 0.48f, 0.08f));
            Material environmentMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeEnvironment",
                new Color(0.48f, 0.52f, 0.46f));

            PlayerConfig playerConfig = EnsurePlayerConfig();
            GameBalanceConfig gameBalanceConfig = EnsureGameBalanceConfig();
            GameObject trackChunkPrefab = EnsureTrackChunkPrefab(
                gameBalanceConfig,
                groundMaterial,
                boundaryMaterial,
                environmentMaterial);

            EnsureCompactPlayerPrefabDefault();
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                PlayerPrefabPath,
                ImportAssetOptions.ForceUpdate);
            SetupMovingWorldScene(
                playerConfig,
                gameBalanceConfig,
                trackChunkPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Project Gatefront] Phase 3.5 moving world prototype setup completed.");
        }

        private static GameBalanceConfig EnsureGameBalanceConfig()
        {
            GameBalanceConfig config =
                AssetDatabase.LoadAssetAtPath<GameBalanceConfig>(
                    GameBalanceConfigPath);
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<GameBalanceConfig>();
            AssetDatabase.CreateAsset(config, GameBalanceConfigPath);
            return config;
        }

        private static GameObject EnsureTrackChunkPrefab(
            GameBalanceConfig config,
            Material groundMaterial,
            Material boundaryMaterial,
            Material environmentMaterial)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                TrackChunkPrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject root = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(TrackChunkPrefabPath)
                : new GameObject("PF_TrackChunk");

            try
            {
                root.name = "PF_TrackChunk";
                ScrollItem scrollItem = EnsureComponent<ScrollItem>(root, out _);
                scrollItem.Configure(true, config.TrackChunkLength);

                EnsureMovingPrimitive(
                    root.transform,
                    "Ground",
                    PrimitiveType.Cube,
                    new Vector3(0f, -0.25f, 0f),
                    new Vector3(10f, 0.5f, config.TrackChunkLength),
                    groundMaterial);

                Transform boundaries = EnsureChild(
                    root.transform,
                    "Boundaries",
                    out _);
                EnsureMovingPrimitive(
                    boundaries,
                    "LeftBoundary",
                    PrimitiveType.Cube,
                    new Vector3(-5.35f, 0.5f, 0f),
                    new Vector3(0.3f, 1f, config.TrackChunkLength),
                    boundaryMaterial);
                EnsureMovingPrimitive(
                    boundaries,
                    "RightBoundary",
                    PrimitiveType.Cube,
                    new Vector3(5.35f, 0.5f, 0f),
                    new Vector3(0.3f, 1f, config.TrackChunkLength),
                    boundaryMaterial);

                Transform markers = EnsureChild(root.transform, "Markers", out _);
                EnsureMovingPrimitive(
                    markers,
                    "MarkerNear",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.02f, -10f),
                    new Vector3(9.4f, 0.04f, 0.22f),
                    boundaryMaterial);
                EnsureMovingPrimitive(
                    markers,
                    "MarkerCenter",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.02f, 0f),
                    new Vector3(9.4f, 0.04f, 0.22f),
                    boundaryMaterial);
                EnsureMovingPrimitive(
                    markers,
                    "MarkerFar",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.02f, 10f),
                    new Vector3(9.4f, 0.04f, 0.22f),
                    boundaryMaterial);

                Transform decoration = EnsureChild(
                    root.transform,
                    "Decoration",
                    out _);
                EnsureMovingPrimitive(
                    decoration,
                    "CrateLeft",
                    PrimitiveType.Cube,
                    new Vector3(-7f, 0.7f, -6f),
                    new Vector3(1.4f, 1.4f, 1.4f),
                    environmentMaterial);
                EnsureMovingPrimitive(
                    decoration,
                    "PillarRight",
                    PrimitiveType.Cylinder,
                    new Vector3(7f, 1f, 5f),
                    new Vector3(0.65f, 1f, 0.65f),
                    environmentMaterial);
                EnsureMovingPrimitive(
                    decoration,
                    "LowWallLeft",
                    PrimitiveType.Cube,
                    new Vector3(-7.2f, 0.45f, 11f),
                    new Vector3(2.8f, 0.9f, 0.45f),
                    environmentMaterial);

                PrefabUtility.SaveAsPrefabAsset(root, TrackChunkPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(TrackChunkPrefabPath);
        }

        private static GameObject EnsureMovingPrimitive(
            Transform parent,
            string objectName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            Transform existing = parent.Find(objectName);
            GameObject primitive;
            if (existing == null)
            {
                primitive = GameObject.CreatePrimitive(primitiveType);
                primitive.name = objectName;
                primitive.transform.SetParent(parent, false);
            }
            else
            {
                primitive = existing.gameObject;
            }

            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = Quaternion.identity;
            primitive.transform.localScale = localScale;
            MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            primitive.isStatic = false;
            return primitive;
        }

        private static void EnsureCompactPlayerPrefabDefault()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                SquadController squadController =
                    root.GetComponentInChildren<SquadController>(true);
                if (squadController == null)
                {
                    throw new InvalidOperationException(
                        "PF_Player must contain a SquadController.");
                }

                var serializedController = new SerializedObject(squadController);
                serializedController.FindProperty("currentFormation").intValue =
                    (int)FormationType.Compact;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupMovingWorldScene(
            PlayerConfig playerConfig,
            GameBalanceConfig gameBalanceConfig,
            GameObject trackChunkPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single);

            GameObject playerRoot = EnsureSceneRoot(scene, "Player", out _);
            PlayerController playerController =
                playerRoot.GetComponentInChildren<PlayerController>(true);
            if (playerController == null)
            {
                throw new InvalidOperationException(
                    "The gameplay scene must contain PF_Player before Phase 3.5 setup.");
            }

            Vector3 playerPosition = playerController.transform.position;
            playerPosition.x = 0f;
            playerPosition.z = playerConfig.CombatAnchorZ;
            playerController.transform.position = playerPosition;

            GameObject cameraRoot = EnsureSceneRoot(scene, "Camera", out _);
            RunnerCameraController cameraController =
                cameraRoot.GetComponentInChildren<RunnerCameraController>(true);
            if (cameraController == null)
            {
                throw new InvalidOperationException(
                    "The gameplay scene must contain RunnerCameraController.");
            }

            Transform cameraTarget = playerController.transform.Find("CameraTarget");
            if (cameraTarget == null)
            {
                throw new InvalidOperationException(
                    "PF_Player must contain a direct CameraTarget child.");
            }

            Vector3 fixedCameraPosition = new Vector3(0f, 8.85f, -7.6f);
            Vector3 lookTargetOffset = new Vector3(0f, 0f, 9.5f);
            cameraController.ConfigureFixedPresentation(
                fixedCameraPosition,
                lookTargetOffset,
                0f,
                0.16f);
            cameraController.transform.position = fixedCameraPosition;
            Vector3 lookPoint = cameraTarget.position + lookTargetOffset;
            lookPoint.x = 0f;
            cameraController.transform.rotation = Quaternion.LookRotation(
                lookPoint - fixedCameraPosition,
                Vector3.up);
            EditorUtility.SetDirty(cameraController);

            GameObject worldRoot = EnsureSceneRoot(scene, "World", out _);
            worldRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            worldRoot.transform.localScale = Vector3.one;
            Transform trackChunksRoot = EnsureChild(
                worldRoot.transform,
                "TrackChunks",
                out _);
            Transform dynamicContentRoot = EnsureChild(
                worldRoot.transform,
                "DynamicContent",
                out _);
            dynamicContentRoot.localPosition = Vector3.zero;

            var scrollItems = new List<ScrollItem>(gameBalanceConfig.TrackChunkCount);
            for (int index = 0; index < gameBalanceConfig.TrackChunkCount; index++)
            {
                string chunkName = $"TrackChunk_{index:00}";
                Transform chunkTransform = trackChunksRoot.Find(chunkName);
                if (chunkTransform == null)
                {
                    GameObject chunkInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                        trackChunkPrefab,
                        scene);
                    chunkInstance.name = chunkName;
                    chunkInstance.transform.SetParent(trackChunksRoot, false);
                    chunkTransform = chunkInstance.transform;
                }

                chunkTransform.localPosition = new Vector3(
                    0f,
                    0f,
                    playerConfig.CombatAnchorZ
                        + (index * gameBalanceConfig.TrackChunkLength));
                chunkTransform.localRotation = Quaternion.identity;
                chunkTransform.localScale = Vector3.one;

                ScrollItem item = chunkTransform.GetComponent<ScrollItem>();
                if (item == null)
                {
                    throw new InvalidOperationException(
                        $"{chunkName} must contain a ScrollItem component.");
                }

                item.Configure(true, gameBalanceConfig.TrackChunkLength);
                EditorUtility.SetDirty(item);
                scrollItems.Add(item);
            }

            for (int index = trackChunksRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = trackChunksRoot.GetChild(index);
                if (child.name.StartsWith("TrackChunk_", StringComparison.Ordinal)
                    && !scrollItems.Contains(child.GetComponent<ScrollItem>()))
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            WorldScroller worldScroller = EnsureComponent<WorldScroller>(
                worldRoot,
                out _);
            worldScroller.Configure(
                gameBalanceConfig,
                scrollItems,
                playerConfig.CombatAnchorZ - gameBalanceConfig.TrackChunkLength);
            EditorUtility.SetDirty(worldScroller);

            GameObject lightingRoot = EnsureSceneRoot(scene, "Lighting", out _);
            SetupLighting(lightingRoot.transform);

            GameObject oldEnvironment = FindSceneRoot(scene, "Environment");
            if (oldEnvironment != null)
            {
                UnityEngine.Object.DestroyImmediate(oldEnvironment);
            }

            GameObject systemsRoot = EnsureSceneRoot(scene, "Systems", out _);
            GameplayBootstrap gameplayBootstrap =
                EnsureComponent<GameplayBootstrap>(systemsRoot, out _);
            gameplayBootstrap.ConfigureWorld(gameBalanceConfig, worldScroller);
            EditorUtility.SetDirty(gameplayBootstrap);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static GameObject FindSceneRoot(Scene scene, string objectName)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName)
                {
                    return root;
                }
            }

            return null;
        }
    }
}
