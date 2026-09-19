using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Enemies;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.Weapons;
using ProjectGatefront.Gameplay.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGatefront.Editor
{
    public static partial class ProjectSetupWizard
    {
        private const string WeakRusherConfigPath =
            "Assets/_Project/Data/Configs/Enemy_Rusher_Weak.asset";
        private const string MediumRusherConfigPath =
            "Assets/_Project/Data/Configs/Enemy_Rusher_Medium.asset";
        private const string PrototypeCombatLevelPath =
            "Assets/_Project/Data/Levels/PrototypeCombatLevel.asset";
        private const string EnemyRusherPrefabPath =
            "Assets/_Project/Prefabs/Enemies/PF_EnemyRusher.prefab";

        [MenuItem("Tools/Project Gatefront/Setup Phase 5 Enemy Combat Prototype")]
        public static void SetupPhase5EnemyCombatPrototype()
        {
            if (!Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureVersionControl();
            EnsureFolders();
            EnsureFolder(PrototypeMaterialsFolder);

            Material weakMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeEnemyWeak",
                new Color(0.58f, 0.08f, 0.08f));
            Material mediumMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeEnemyMedium",
                new Color(0.24f, 0.04f, 0.05f));
            Material markerMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeEnemyMarker",
                new Color(1f, 0.27f, 0.08f));

            EnemyConfig weakConfig = EnsureEnemyConfig(
                WeakRusherConfigPath,
                "rusher_weak",
                "Weak Rusher",
                64f,
                2f,
                1,
                0.55f,
                1f);
            EnemyConfig mediumConfig = EnsureEnemyConfig(
                MediumRusherConfigPath,
                "rusher_medium",
                "Medium Rusher",
                180f,
                1.25f,
                2,
                0.62f,
                1.12f);
            LevelConfig levelConfig = EnsurePrototypeCombatLevel(
                weakConfig,
                mediumConfig);
            GameObject enemyPrefab = EnsureEnemyRusherPrefab(
                weakMaterial,
                mediumMaterial,
                markerMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                EnemyRusherPrefabPath,
                ImportAssetOptions.ForceUpdate);
            SetupPhase5GameplayScene(levelConfig, enemyPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Project Gatefront] Phase 5 enemy combat prototype setup completed.");
        }

        private static EnemyConfig EnsureEnemyConfig(
            string path,
            string enemyId,
            string displayName,
            float health,
            float approachSpeed,
            int casualties,
            float targetRadius,
            float visualScale)
        {
            EnemyConfig config = AssetDatabase.LoadAssetAtPath<EnemyConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<EnemyConfig>();
                AssetDatabase.CreateAsset(config, path);
            }

            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("enemyId").stringValue = enemyId;
            serializedConfig.FindProperty("displayName").stringValue = displayName;
            serializedConfig.FindProperty("maximumHealth").floatValue = health;
            serializedConfig.FindProperty("approachSpeed").floatValue =
                approachSpeed;
            serializedConfig.FindProperty("casualtiesOnContact").intValue =
                casualties;
            serializedConfig.FindProperty("targetRadius").floatValue =
                targetRadius;
            serializedConfig.FindProperty("rewardValue").intValue = 0;
            serializedConfig.FindProperty("visualScale").floatValue = visualScale;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static LevelConfig EnsurePrototypeCombatLevel(
            EnemyConfig weakConfig,
            EnemyConfig mediumConfig)
        {
            LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>(
                PrototypeCombatLevelPath);
            if (level == null)
            {
                level = ScriptableObject.CreateInstance<LevelConfig>();
                AssetDatabase.CreateAsset(level, PrototypeCombatLevelPath);
            }

            var waves = new List<EnemyWaveDefinition>
            {
                new EnemyWaveDefinition(8f, weakConfig, 5, EnemySpawnPattern.Row, 1, 1.15f, 1.1f, 0f, 34f),
                new EnemyWaveDefinition(60f, weakConfig, 10, EnemySpawnPattern.Grid, 2, 1.15f, 1.1f, 0f, 34f),
                new EnemyWaveDefinition(120f, weakConfig, 15, EnemySpawnPattern.Cluster, 3, 1.1f, 1.05f, 0f, 34f),
                new EnemyWaveDefinition(185f, mediumConfig, 10, EnemySpawnPattern.Grid, 2, 1.25f, 1.15f, 0f, 35f),
                new EnemyWaveDefinition(255f, weakConfig, 25, EnemySpawnPattern.Grid, 5, 1.05f, 1f, 0f, 34f),
                new EnemyWaveDefinition(330f, mediumConfig, 40, EnemySpawnPattern.Cluster, 5, 0.95f, 0.95f, 0f, 36f)
            };
            level.ConfigureEnemyWaves(waves, 100);
            EditorUtility.SetDirty(level);
            return level;
        }

        private static GameObject EnsureEnemyRusherPrefab(
            Material weakMaterial,
            Material mediumMaterial,
            Material markerMaterial)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyRusherPrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject root = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(EnemyRusherPrefabPath)
                : new GameObject("PF_EnemyRusher");

            try
            {
                root.name = "PF_EnemyRusher";
                Transform visualRoot = EnsureChild(
                    root.transform,
                    "Visual",
                    out _);
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale = Vector3.one;
                EnsureCombatPrimitive(
                    visualRoot,
                    "Body",
                    PrimitiveType.Capsule,
                    new Vector3(0f, 0.88f, 0f),
                    new Vector3(0.78f, 0.88f, 0.78f),
                    weakMaterial);
                EnsureCombatPrimitive(
                    visualRoot,
                    "Armor",
                    PrimitiveType.Cube,
                    new Vector3(0f, 1f, 0.05f),
                    new Vector3(0.92f, 0.56f, 0.5f),
                    mediumMaterial);
                EnsureCombatPrimitive(
                    visualRoot,
                    "HeadMarker",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 1.82f, 0f),
                    new Vector3(0.5f, 0.4f, 0.5f),
                    markerMaterial);
                EnsureComponent<EnemyMotor>(root, out _);
                EnemyController controller = EnsureComponent<EnemyController>(
                    root,
                    out _);
                controller.Configure(visualRoot, 1f);
                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(root, EnemyRusherPrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(
                EnemyRusherPrefabPath);
        }

        private static void SetupPhase5GameplayScene(
            LevelConfig levelConfig,
            GameObject enemyPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene(
                GameplayScenePath,
                OpenSceneMode.Single);
            PlayerConfig playerConfig = AssetDatabase.LoadAssetAtPath<PlayerConfig>(
                PlayerConfigPath);
            GameObject playerRoot = EnsureSceneRoot(scene, "Player", out _);
            SquadController squad =
                playerRoot.GetComponentInChildren<SquadController>(true);
            GameObject worldRoot = EnsureSceneRoot(scene, "World", out _);
            WorldScroller world = worldRoot.GetComponent<WorldScroller>();
            GameObject systemsRoot = EnsureSceneRoot(scene, "Systems", out _);
            GameplayBootstrap bootstrap = systemsRoot.GetComponent<GameplayBootstrap>();
            Transform combatRoot = systemsRoot.transform.Find("Combat");
            if (playerConfig == null
                || squad == null
                || world == null
                || bootstrap == null
                || combatRoot == null)
            {
                throw new InvalidOperationException(
                    "Phase 5 requires the completed Phase 4 gameplay setup.");
            }

            Transform dynamicContent = worldRoot.transform.Find("DynamicContent");
            Transform oldTargets = dynamicContent != null
                ? dynamicContent.Find("TestTargets")
                : null;
            if (oldTargets != null)
            {
                UnityEngine.Object.DestroyImmediate(oldTargets.gameObject);
            }

            TargetRegistry registry = combatRoot.GetComponent<TargetRegistry>();
            ProjectilePool projectilePool = combatRoot.GetComponent<ProjectilePool>();
            SquadWeaponSystem weaponSystem =
                combatRoot.GetComponent<SquadWeaponSystem>();
            WeaponConfig weaponConfig = AssetDatabase.LoadAssetAtPath<WeaponConfig>(
                WeaponConfigPath);
            if (registry == null
                || projectilePool == null
                || weaponSystem == null
                || weaponConfig == null)
            {
                throw new InvalidOperationException(
                    "Phase 5 requires the completed Phase 4 combat setup.");
            }

            weaponSystem.Configure(
                weaponConfig,
                squad,
                registry,
                projectilePool,
                world,
                Array.Empty<CombatTargetDummy>());

            Transform enemyContainer = EnsureChild(
                combatRoot,
                "EnemyContainer",
                out _);
            EnemyPool enemyPool = EnsureComponent<EnemyPool>(
                combatRoot.gameObject,
                out _);
            enemyPool.Configure(
                enemyPrefab.GetComponent<EnemyController>(),
                enemyContainer,
                60,
                0.12f,
                4f);
            EnemyWaveDirector director = EnsureComponent<EnemyWaveDirector>(
                combatRoot.gameObject,
                out _);
            director.Configure(
                levelConfig,
                enemyPool,
                registry,
                world,
                squad,
                playerConfig);
            bootstrap.ConfigureCombat(weaponSystem);
            bootstrap.ConfigureEnemyCombat(director);

            EditorUtility.SetDirty(weaponSystem);
            EditorUtility.SetDirty(enemyPool);
            EditorUtility.SetDirty(director);
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
