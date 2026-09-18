using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Combat;
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
        private const string WeaponConfigPath =
            "Assets/_Project/Data/Configs/PrototypeRifle.asset";
        private const string WeaponsPrefabFolder =
            "Assets/_Project/Prefabs/Weapons";
        private const string ProjectilePrefabPath =
            "Assets/_Project/Prefabs/Weapons/PF_Projectile.prefab";
        private const string WeaponVisualPrefabPath =
            "Assets/_Project/Prefabs/Weapons/PF_PrototypeRifle.prefab";

        [MenuItem("Tools/Project Gatefront/Setup Phase 4 Auto Shooting Prototype")]
        public static void SetupPhase4AutoShootingPrototype()
        {
            if (!Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureVersionControl();
            EnsureFolders();
            EnsureFolder(PrototypeMaterialsFolder);
            EnsureFolder(WeaponsPrefabFolder);

            Material weaponMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeWeapon",
                new Color(0.16f, 0.2f, 0.22f));
            Material projectileMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeProjectile",
                new Color(1f, 0.82f, 0.12f));
            Material muzzleMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeMuzzleFlash",
                new Color(1f, 0.38f, 0.05f));
            Material weakTargetMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeTargetWeak",
                new Color(0.92f, 0.35f, 0.25f));
            Material mediumTargetMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeTargetMedium",
                new Color(0.75f, 0.18f, 0.18f));
            Material strongTargetMaterial = EnsurePrototypeMaterial(
                "MAT_PrototypeTargetStrong",
                new Color(0.43f, 0.08f, 0.08f));

            GameObject weaponVisualPrefab = EnsureWeaponVisualPrefab(
                weaponMaterial);
            GameObject projectilePrefab = EnsureProjectilePrefab(
                projectileMaterial);
            WeaponConfig weaponConfig = EnsurePrototypeWeaponConfig(
                weaponVisualPrefab);
            EnsureSquadUnitWeaponController(muzzleMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(
                SquadUnitPrefabPath,
                ImportAssetOptions.ForceUpdate);
            SetupPhase4GameplayScene(
                weaponConfig,
                projectilePrefab,
                weakTargetMaterial,
                mediumTargetMaterial,
                strongTargetMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                "[Project Gatefront] Phase 4 auto shooting prototype setup completed.");
        }

        private static GameObject EnsureWeaponVisualPrefab(Material material)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                WeaponVisualPrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject root = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(WeaponVisualPrefabPath)
                : new GameObject("PF_PrototypeRifle");

            try
            {
                root.name = "PF_PrototypeRifle";
                EnsureCombatPrimitive(
                    root.transform,
                    "Body",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0f, 0.14f),
                    new Vector3(0.18f, 0.16f, 0.52f),
                    material);
                EnsureCombatPrimitive(
                    root.transform,
                    "Barrel",
                    PrimitiveType.Cube,
                    new Vector3(0f, 0f, 0.52f),
                    new Vector3(0.07f, 0.07f, 0.45f),
                    material);
                PrefabUtility.SaveAsPrefabAsset(root, WeaponVisualPrefabPath);
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
                WeaponVisualPrefabPath);
        }

        private static GameObject EnsureProjectilePrefab(Material material)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ProjectilePrefabPath);
            bool loadedPrefabContents = existingPrefab != null;
            GameObject root = loadedPrefabContents
                ? PrefabUtility.LoadPrefabContents(ProjectilePrefabPath)
                : new GameObject("PF_Projectile");

            try
            {
                root.name = "PF_Projectile";
                EnsureComponent<Projectile>(root, out _);
                EnsureCombatPrimitive(
                    root.transform,
                    "Visual",
                    PrimitiveType.Cube,
                    Vector3.zero,
                    new Vector3(0.09f, 0.09f, 0.42f),
                    material);
                PrefabUtility.SaveAsPrefabAsset(root, ProjectilePrefabPath);
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

            return AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
        }

        private static WeaponConfig EnsurePrototypeWeaponConfig(
            GameObject weaponVisualPrefab)
        {
            WeaponConfig config = AssetDatabase.LoadAssetAtPath<WeaponConfig>(
                WeaponConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<WeaponConfig>();
                AssetDatabase.CreateAsset(config, WeaponConfigPath);
            }

            var serializedConfig = new SerializedObject(config);
            serializedConfig.FindProperty("weaponId").stringValue =
                "prototype_rifle";
            serializedConfig.FindProperty("displayName").stringValue =
                "Prototype Rifle";
            serializedConfig.FindProperty("weaponTier").enumValueIndex =
                (int)WeaponTier.Tier1 - 1;
            serializedConfig.FindProperty("damage").floatValue = 8f;
            serializedConfig.FindProperty("shotsPerSecond").floatValue = 4f;
            serializedConfig.FindProperty("range").floatValue = 28f;
            serializedConfig.FindProperty("projectileSpeed").floatValue = 28f;
            serializedConfig.FindProperty("projectileLifetime").floatValue = 2f;
            serializedConfig.FindProperty("projectileRadius").floatValue = 0.08f;
            serializedConfig.FindProperty("spreadDegrees").floatValue = 0.75f;
            serializedConfig.FindProperty("penetrationCount").intValue = 0;
            serializedConfig.FindProperty("projectilesPerShot").intValue = 1;
            serializedConfig.FindProperty("targetingHalfAngle").floatValue = 65f;
            serializedConfig.FindProperty("weaponVisualPrefab").objectReferenceValue =
                weaponVisualPrefab;
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static void EnsureSquadUnitWeaponController(
            Material muzzleMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(SquadUnitPrefabPath);
            try
            {
                Transform weaponMount = EnsureChild(
                    root.transform,
                    "WeaponMount",
                    out _);
                weaponMount.localPosition = new Vector3(0f, 1.12f, 0.46f);
                weaponMount.localRotation = Quaternion.identity;
                weaponMount.localScale = Vector3.one;

                Transform firePoint = EnsureChild(
                    weaponMount,
                    "FirePoint",
                    out _);
                firePoint.localPosition = new Vector3(0f, 0f, 0.52f);
                firePoint.localRotation = Quaternion.identity;
                firePoint.localScale = Vector3.one;

                GameObject muzzleFlash = EnsureCombatPrimitive(
                    firePoint,
                    "MuzzleFlash",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 0f, 0.08f),
                    new Vector3(0.18f, 0.18f, 0.24f),
                    muzzleMaterial);
                muzzleFlash.SetActive(false);

                WeaponController controller = EnsureComponent<WeaponController>(
                    root,
                    out _);
                controller.Configure(weaponMount, firePoint, muzzleFlash);
                EditorUtility.SetDirty(controller);
                PrefabUtility.SaveAsPrefabAsset(root, SquadUnitPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void SetupPhase4GameplayScene(
            WeaponConfig weaponConfig,
            GameObject projectilePrefab,
            Material weakTargetMaterial,
            Material mediumTargetMaterial,
            Material strongTargetMaterial)
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
                    "Phase 4 requires the Phase 3.6 squad setup.");
            }

            GameObject worldRoot = EnsureSceneRoot(scene, "World", out _);
            WorldScroller worldScroller = worldRoot.GetComponent<WorldScroller>();
            if (worldScroller == null)
            {
                throw new InvalidOperationException(
                    "Phase 4 requires the Phase 3.5 moving-world setup.");
            }

            Transform dynamicContent = EnsureChild(
                worldRoot.transform,
                "DynamicContent",
                out _);
            Transform targetRoot = EnsureChild(
                dynamicContent,
                "TestTargets",
                out _);
            targetRoot.localPosition = Vector3.zero;
            targetRoot.localRotation = Quaternion.identity;
            targetRoot.localScale = Vector3.one;

            var targets = new List<CombatTargetDummy>(6)
            {
                EnsureTargetDummy(targetRoot, "TargetDummy_Weak", new Vector3(0f, 0f, 18f), 80f, weakTargetMaterial),
                EnsureTargetDummy(targetRoot, "TargetDummy_MediumLeft", new Vector3(-1.6f, 0f, 25f), 300f, mediumTargetMaterial),
                EnsureTargetDummy(targetRoot, "TargetDummy_MediumRight", new Vector3(1.6f, 0f, 25f), 300f, mediumTargetMaterial),
                EnsureTargetDummy(targetRoot, "TargetDummy_StrongLeft", new Vector3(-2.4f, 0f, 34f), 1200f, strongTargetMaterial),
                EnsureTargetDummy(targetRoot, "TargetDummy_StrongCenter", new Vector3(0f, 0f, 36f), 2000f, strongTargetMaterial),
                EnsureTargetDummy(targetRoot, "TargetDummy_StrongRight", new Vector3(2.4f, 0f, 34f), 1200f, strongTargetMaterial)
            };

            GameObject systemsRoot = EnsureSceneRoot(scene, "Systems", out _);
            GameplayBootstrap bootstrap = EnsureComponent<GameplayBootstrap>(
                systemsRoot,
                out _);
            Transform combatRoot = EnsureChild(
                systemsRoot.transform,
                "Combat",
                out _);
            Transform projectileContainer = EnsureChild(
                combatRoot,
                "ProjectileContainer",
                out _);
            TargetRegistry registry = EnsureComponent<TargetRegistry>(
                combatRoot.gameObject,
                out _);
            ProjectilePool pool = EnsureComponent<ProjectilePool>(
                combatRoot.gameObject,
                out _);
            pool.Configure(
                projectilePrefab.GetComponent<Projectile>(),
                projectileContainer);
            SquadWeaponSystem weaponSystem = EnsureComponent<SquadWeaponSystem>(
                combatRoot.gameObject,
                out _);
            weaponSystem.Configure(
                weaponConfig,
                squadController,
                registry,
                pool,
                worldScroller,
                targets);
            bootstrap.ConfigureCombat(weaponSystem);

            EditorUtility.SetDirty(registry);
            EditorUtility.SetDirty(pool);
            EditorUtility.SetDirty(weaponSystem);
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static CombatTargetDummy EnsureTargetDummy(
            Transform parent,
            string objectName,
            Vector3 localPosition,
            float health,
            Material material)
        {
            Transform targetTransform = EnsureChild(parent, objectName, out _);
            targetTransform.localPosition = localPosition;
            targetTransform.localRotation = Quaternion.identity;
            targetTransform.localScale = Vector3.one;
            ScrollItem scrollItem = EnsureComponent<ScrollItem>(
                targetTransform.gameObject,
                out _);
            scrollItem.Configure(false, 0f);
            CombatTargetDummy target = EnsureComponent<CombatTargetDummy>(
                targetTransform.gameObject,
                out _);
            target.Configure(health, 0.62f, 1f, -5f);
            EnsureCombatPrimitive(
                targetTransform,
                "VisualBody",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1f, 0f),
                new Vector3(0.62f, 1f, 0.62f),
                material);
            EditorUtility.SetDirty(scrollItem);
            EditorUtility.SetDirty(target);
            return target;
        }

        private static GameObject EnsureCombatPrimitive(
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
    }
}
