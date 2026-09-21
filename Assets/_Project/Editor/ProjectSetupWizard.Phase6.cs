using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Enemies;
using ProjectGatefront.Gameplay.Gates;
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
        private const string GatePrefabPath =
            "Assets/_Project/Prefabs/Gates/PF_ShootableGate.prefab";

        [MenuItem("Tools/Project Gatefront/Setup Phase 6 Shootable Gates")]
        public static void SetupPhase6ShootableGates()
        {
            if (!Application.isBatchMode
                && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureVersionControl();
            EnsureFolders();
            EnsureFolder("Assets/_Project/Prefabs/Gates");
            Material negative = EnsurePrototypeMaterial(
                "MAT_GateNegative", new Color(0.66f, 0.08f, 0.12f));
            Material neutral = EnsurePrototypeMaterial(
                "MAT_GateNeutral", new Color(0.46f, 0.5f, 0.51f));
            Material positive = EnsurePrototypeMaterial(
                "MAT_GatePositive", new Color(0.08f, 0.55f, 0.2f));
            Material divide = EnsurePrototypeMaterial(
                "MAT_GateDivide", new Color(0.08f, 0.29f, 0.62f));
            Material frame = EnsurePrototypeMaterial(
                "MAT_GateFrame", new Color(0.98f, 0.72f, 0.12f));

            GateController prefab = EnsureGatePrefab(
                negative, neutral, positive, divide, frame);
            LevelConfig level = AssetDatabase.LoadAssetAtPath<LevelConfig>(
                PrototypeCombatLevelPath);
            if (level == null)
            {
                throw new InvalidOperationException(
                    "Phase 6 requires the Phase 5 prototype level.");
            }

            level.ConfigureGateEncounters(new List<GateEncounterDefinition>
            {
                GateEncounter(60f, GateEncounterLayout.SingleLarge, -10, 30, 0.65f),
                GateEncounter(140f, GateEncounterLayout.SingleLarge, -20, 35, 0.6f),
                GateEncounter(230f, GateEncounterLayout.ShootableVsDivide, -30, 40, 0.55f),
                GateEncounter(330f, GateEncounterLayout.SingleLarge, -40, 45, 0.5f),
                GateEncounter(430f, GateEncounterLayout.ShootableVsDivide, -50, 50, 0.45f)
            });
            EditorUtility.SetDirty(level);
            AssetDatabase.SaveAssets();
            SetupGateScene(level, prefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Project Gatefront] Phase 6 shootable gates setup completed.");
        }

        private static GateEncounterDefinition GateEncounter(
            float distance,
            GateEncounterLayout layout,
            int startingValue,
            int maximumValue,
            float density)
        {
            return new GateEncounterDefinition(
                distance, layout, startingValue, startingValue,
                maximumValue, 2, density, 18f, 8f, 34f, 26f);
        }

        private static GateController EnsureGatePrefab(
            Material negative,
            Material neutral,
            Material positive,
            Material divide,
            Material frame)
        {
            bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(GatePrefabPath) != null;
            GameObject root = existing
                ? PrefabUtility.LoadPrefabContents(GatePrefabPath)
                : new GameObject("PF_ShootableGate");
            try
            {
                root.name = "PF_ShootableGate";
                EnsureComponent<ScrollItem>(root, out _);
                EnsureComponent<WorldContentCarrier>(root, out _);
                GateController gate = EnsureComponent<GateController>(root, out _);
                GameObject panel = EnsureCombatPrimitive(root.transform,
                    "Panel", PrimitiveType.Cube, new Vector3(0f, 1.55f, 0f),
                    new Vector3(4f, 2.55f, 0.18f), negative);
                GameObject left = EnsureCombatPrimitive(root.transform,
                    "LeftPost", PrimitiveType.Cube,
                    new Vector3(-2f, 1.55f, 0f),
                    new Vector3(0.18f, 3.15f, 0.3f), frame);
                GameObject right = EnsureCombatPrimitive(root.transform,
                    "RightPost", PrimitiveType.Cube,
                    new Vector3(2f, 1.55f, 0f),
                    new Vector3(0.18f, 3.15f, 0.3f), frame);
                GameObject top = EnsureCombatPrimitive(root.transform,
                    "TopBeam", PrimitiveType.Cube,
                    new Vector3(0f, 3.1f, 0f),
                    new Vector3(4.2f, 0.18f, 0.3f), frame);
                Transform labelObject = EnsureChild(root.transform, "ValueLabel", out _);
                labelObject.localPosition = new Vector3(0f, 1.42f, -0.17f);
                labelObject.localRotation = Quaternion.Euler(0f, 180f, 0f);
                labelObject.localScale = Vector3.one * 0.52f;
                TextMesh label = EnsureComponent<TextMesh>(labelObject.gameObject, out _);
                label.text = "-10";
                label.fontSize = 96;
                label.characterSize = 0.11f;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.color = Color.white;
                gate.ConfigureVisuals(panel.transform, left.transform,
                    right.transform, top.transform,
                    panel.GetComponent<MeshRenderer>(), label,
                    negative, neutral, positive, divide);
                EditorUtility.SetDirty(gate);
                PrefabUtility.SaveAsPrefabAsset(root, GatePrefabPath);
            }
            finally
            {
                if (existing)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            AssetDatabase.ImportAsset(GatePrefabPath,
                ImportAssetOptions.ForceSynchronousImport);
            GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                GatePrefabPath);
            if (savedPrefab == null)
            {
                throw new InvalidOperationException(
                    "Gate prefab could not be loaded after saving.");
            }

            GateController savedGate = savedPrefab.GetComponent<GateController>();
            if (savedGate == null)
            {
                throw new InvalidOperationException(
                    "Saved gate prefab has no loadable GateController component.");
            }

            return savedGate;
        }

        private static void SetupGateScene(LevelConfig level, GateController prefab)
        {
            Scene scene = EditorSceneManager.OpenScene(GameplayScenePath, OpenSceneMode.Single);
            // Opening a scene can invalidate a prefab-content component handle.
            // Resolve the saved asset again before assigning scene references.
            GameObject gateAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                GatePrefabPath);
            prefab = gateAsset == null ? null : gateAsset.GetComponent<GateController>();
            GameObject player = EnsureSceneRoot(scene, "Player", out _);
            GameObject worldRoot = EnsureSceneRoot(scene, "World", out _);
            GameObject systems = EnsureSceneRoot(scene, "Systems", out _);
            SquadController squad = player.GetComponentInChildren<SquadController>(true);
            WorldScroller scroller = worldRoot.GetComponent<WorldScroller>();
            GameplayBootstrap bootstrap = systems.GetComponent<GameplayBootstrap>();
            Transform combat = systems.transform.Find("Combat");
            PlayerConfig track = AssetDatabase.LoadAssetAtPath<PlayerConfig>(PlayerConfigPath);
            if (squad == null || scroller == null || bootstrap == null
                || combat == null || track == null || prefab == null)
            {
                throw new InvalidOperationException(
                    "Phase 6 requires the completed Phase 5 gameplay scene. "
                    + $"squad={squad != null}, world={scroller != null}, "
                    + $"bootstrap={bootstrap != null}, combat={combat != null}, "
                    + $"track={track != null}, prefab={prefab != null}.");
            }

            TargetRegistry registry = combat.GetComponent<TargetRegistry>();
            ContinuousEnemyDirector enemies = combat.GetComponent<ContinuousEnemyDirector>();
            SquadWeaponSystem weapons = combat.GetComponent<SquadWeaponSystem>();
            if (registry == null || enemies == null || weapons == null)
            {
                throw new InvalidOperationException(
                    "Phase 6 requires the Phase 5 combat components.");
            }

            Transform gateContainer = EnsureChild(combat, "GateContainer", out _);
            GateDirector director = EnsureComponent<GateDirector>(combat.gameObject, out _);
            director.Configure(level, prefab, gateContainer, scroller,
                registry, squad, enemies, track);
            var serializedWeapons = new SerializedObject(weapons);
            serializedWeapons.FindProperty("maximumDistributedTargets").intValue = 128;
            serializedWeapons.ApplyModifiedPropertiesWithoutUndo();
            bootstrap.ConfigureGateCombat(director);
            EditorUtility.SetDirty(weapons);
            EditorUtility.SetDirty(director);
            EditorUtility.SetDirty(bootstrap);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
