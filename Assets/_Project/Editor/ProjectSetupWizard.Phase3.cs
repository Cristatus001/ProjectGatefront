using System;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Squad;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGatefront.Editor
{
    public static partial class ProjectSetupWizard
    {
        private const string SquadConfigPath =
            "Assets/_Project/Data/Configs/SquadConfig.asset";
        private const string SquadUnitPrefabPath =
            "Assets/_Project/Prefabs/Characters/PF_SquadUnit.prefab";

        [MenuItem("Tools/Project Gatefront/Setup Phase 3 Squad Prototype")]
        public static void SetupPhase3SquadPrototype()
        {
            if (!Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureVersionControl();
            EnsureFolders();
            EnsureFolder(PrototypeMaterialsFolder);

            Material squadMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeSquadUnit",
                new Color(0.24f, 0.68f, 0.34f));
            Material orientationMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeBoundary",
                new Color(0.95f, 0.48f, 0.08f));
            SquadConfig squadConfig = EnsureSquadConfig();
            GameObject squadUnitPrefab = EnsureSquadUnitPrefab(
                squadMaterial,
                orientationMaterial);

            EnsurePlayerSquadRoot(squadConfig, squadUnitPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                PlayerPrefabPath,
                ImportAssetOptions.ForceUpdate);
            SetupPhase3GameplayScene(squadConfig);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Project Gatefront] Phase 3 squad prototype setup completed.");
        }

        private static SquadConfig EnsureSquadConfig()
        {
            SquadConfig squadConfig = AssetDatabase.LoadAssetAtPath<SquadConfig>(
                SquadConfigPath);
            if (squadConfig != null)
            {
                return squadConfig;
            }

            squadConfig = ScriptableObject.CreateInstance<SquadConfig>();
            AssetDatabase.CreateAsset(squadConfig, SquadConfigPath);
            return squadConfig;
        }

        private static GameObject EnsureSquadUnitPrefab(
            Material squadMaterial,
            Material orientationMaterial)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SquadUnitPrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject root = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(SquadUnitPrefabPath)
                : new GameObject("SquadUnit");

            try
            {
                root.name = "PF_SquadUnit";
                EnsureComponent<SquadUnit>(root, out _);
                Transform visual = EnsureChild(root.transform, "Visual", out _);
                visual.localPosition = Vector3.zero;
                visual.localRotation = Quaternion.identity;
                visual.localScale = Vector3.one;

                EnsureSquadVisualPrimitive(
                    visual,
                    "Body",
                    "Torso",
                    PrimitiveType.Capsule,
                    new Vector3(0f, 0.86f, 0f),
                    new Vector3(0.68f, 0.86f, 0.68f),
                    squadMaterial);
                EnsureSquadVisualPrimitive(
                    visual,
                    "HeadMarker",
                    "Head",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 1.82f, 0f),
                    new Vector3(0.48f, 0.38f, 0.48f),
                    squadMaterial);
                EnsureSquadVisualPrimitive(
                    visual,
                    "ForwardMarker",
                    null,
                    PrimitiveType.Cube,
                    new Vector3(0f, 1.12f, 0.46f),
                    new Vector3(0.15f, 0.15f, 0.58f),
                    orientationMaterial);

                RemoveGeneratedSquadVisual(visual, "Helmet");
                RemoveGeneratedSquadVisual(visual, "Backpack");

                PrefabUtility.SaveAsPrefabAsset(root, SquadUnitPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(SquadUnitPrefabPath);
        }

        private static GameObject EnsureSquadVisualPrimitive(
            Transform parent,
            string objectName,
            string fallbackName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material material)
        {
            Transform existing = parent.Find(objectName);
            if (existing == null && !string.IsNullOrEmpty(fallbackName))
            {
                existing = parent.Find(fallbackName);
            }

            GameObject primitive;
            if (existing == null)
            {
                primitive = GameObject.CreatePrimitive(primitiveType);
                primitive.transform.SetParent(parent, false);
            }
            else
            {
                primitive = existing.gameObject;
            }

            primitive.name = objectName;
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

        private static void RemoveGeneratedSquadVisual(
            Transform parent,
            string objectName)
        {
            Transform child = parent.Find(objectName);
            if (child != null)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }

        private static void EnsurePlayerSquadRoot(
            SquadConfig squadConfig,
            GameObject squadUnitPrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform squadRoot = EnsureChild(
                    root.transform,
                    "SquadRoot",
                    out bool squadRootCreated);
                if (squadRootCreated)
                {
                    squadRoot.localPosition = Vector3.zero;
                    squadRoot.localRotation = Quaternion.identity;
                }

                SquadController squadController =
                    EnsureComponent<SquadController>(squadRoot.gameObject, out _);
                SquadUnit squadUnit = squadUnitPrefab.GetComponent<SquadUnit>();
                if (squadUnit == null)
                {
                    throw new InvalidOperationException(
                        "PF_SquadUnit must contain a SquadUnit component.");
                }

                var serializedController = new SerializedObject(squadController);
                serializedController.FindProperty("config").objectReferenceValue =
                    squadConfig;
                serializedController.FindProperty("unitPrefab").objectReferenceValue =
                    squadUnit;
                serializedController.FindProperty("formationRoot").objectReferenceValue =
                    squadRoot;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupPhase3GameplayScene(SquadConfig squadConfig)
        {
            Scene scene = EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single);

            GameObject playerRoot = EnsureSceneRoot(scene, "Player", out _);
            SquadController squadController =
                playerRoot.GetComponentInChildren<SquadController>(true);
            if (squadController == null)
            {
                throw new InvalidOperationException(
                    "The PF_Player scene instance does not contain SquadRoot.");
            }

            GameObject systemsRoot = EnsureSceneRoot(scene, "Systems", out _);
            GameplayBootstrap gameplayBootstrap =
                EnsureComponent<GameplayBootstrap>(systemsRoot, out _);
            gameplayBootstrap.ConfigureSquad(squadConfig, squadController);
            EditorUtility.SetDirty(gameplayBootstrap);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
