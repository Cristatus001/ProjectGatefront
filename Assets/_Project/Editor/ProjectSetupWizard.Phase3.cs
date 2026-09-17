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
            SquadConfig squadConfig = EnsureSquadConfig();
            GameObject squadUnitPrefab = EnsureSquadUnitPrefab(squadMaterial);

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

        private static GameObject EnsureSquadUnitPrefab(Material squadMaterial)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SquadUnitPrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject root = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(SquadUnitPrefabPath)
                : new GameObject("SquadUnit");

            try
            {
                EnsureComponent<SquadUnit>(root, out _);
                Transform visual = EnsureChild(root.transform, "Visual", out _);

                EnsurePrefabPrimitive(
                    visual,
                    "Torso",
                    PrimitiveType.Capsule,
                    new Vector3(0f, 0.58f, 0f),
                    new Vector3(0.42f, 0.55f, 0.42f),
                    squadMaterial);
                EnsurePrefabPrimitive(
                    visual,
                    "Head",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 1.25f, 0f),
                    new Vector3(0.36f, 0.36f, 0.36f),
                    squadMaterial);
                EnsurePrefabPrimitive(
                    visual,
                    "Helmet",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 1.39f, 0.02f),
                    new Vector3(0.4f, 0.2f, 0.4f),
                    squadMaterial);
                EnsurePrefabPrimitive(
                    visual,
                    "Backpack",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0.7f, -0.27f),
                    new Vector3(0.32f, 0.52f, 0.18f),
                    squadMaterial);

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
