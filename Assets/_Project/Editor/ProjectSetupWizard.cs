using System;
using System.Collections.Generic;
using System.Linq;
using ProjectGatefront.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGatefront.Editor
{
    public static partial class ProjectSetupWizard
    {
        private static readonly string[] RequiredFolders =
        {
            "Assets/_Project/Art",
            "Assets/_Project/Art/Animations",
            "Assets/_Project/Art/Materials",
            "Assets/_Project/Art/Models",
            "Assets/_Project/Art/Textures",
            "Assets/_Project/Art/VFX",
            "Assets/_Project/Audio",
            "Assets/_Project/Audio/Music",
            "Assets/_Project/Audio/SFX",
            "Assets/_Project/Data",
            "Assets/_Project/Data/Configs",
            "Assets/_Project/Data/Levels",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Prefabs/Characters",
            "Assets/_Project/Prefabs/Enemies",
            "Assets/_Project/Prefabs/Environment",
            "Assets/_Project/Prefabs/Gameplay",
            "Assets/_Project/Prefabs/UI",
            "Assets/_Project/Scenes",
            "Assets/_Project/Scripts",
            "Assets/_Project/Scripts/Core",
            "Assets/_Project/Scripts/Gameplay",
            "Assets/_Project/Scripts/Gameplay/Player",
            "Assets/_Project/Scripts/Gameplay/Squad",
            "Assets/_Project/Scripts/Gameplay/Gates",
            "Assets/_Project/Scripts/Gameplay/Combat",
            "Assets/_Project/Scripts/Gameplay/Enemies",
            "Assets/_Project/Scripts/Gameplay/Bosses",
            "Assets/_Project/Scripts/Gameplay/Weapons",
            "Assets/_Project/Scripts/Gameplay/Levels",
            "Assets/_Project/Scripts/Gameplay/Progression",
            "Assets/_Project/Scripts/UI",
            "Assets/_Project/Scripts/Audio",
            "Assets/_Project/Scripts/Data",
            "Assets/_Project/Scripts/Save",
            "Assets/_Project/Scripts/Platform",
            "Assets/_Project/Scripts/Platform/CrazyGames",
            "Assets/_Project/Scripts/Platform/Web",
            "Assets/_Project/Scripts/Platform/Mobile",
            "Assets/_Project/Scripts/Utilities",
            "Assets/_Project/Settings",
            "Assets/_Project/UI",
            "Assets/_Project/Editor",
            "Assets/_Project/Tests",
            "Assets/_Project/Tests/EditMode",
            "Assets/_Project/Tests/PlayMode"
        };

        private static readonly string[] FoundationScenes =
        {
            "Assets/_Project/Scenes/00_Boot.unity",
            "Assets/_Project/Scenes/01_MainMenu.unity",
            "Assets/_Project/Scenes/02_Gameplay.unity"
        };

        [MenuItem("Tools/Project Gatefront/Setup Project")]
        public static void SetupProject()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            string previouslyOpenScene = SceneManager.GetActiveScene().path;

            ConfigureVersionControl();
            EnsureFolders();
            CreateMissingScenes();
            ConfigureBuildScenes();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!string.IsNullOrEmpty(previouslyOpenScene)
                && System.IO.File.Exists(previouslyOpenScene))
            {
                EditorSceneManager.OpenScene(previouslyOpenScene, OpenSceneMode.Single);
            }

            Debug.Log("[Project Gatefront] Foundation setup completed.");
        }

        private static void ConfigureVersionControl()
        {
            VersionControlSettings.mode = "Visible Meta Files";
            EditorSettings.serializationMode = SerializationMode.ForceText;
        }

        private static void EnsureFolders()
        {
            foreach (string folder in RequiredFolders)
            {
                EnsureFolder(folder);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            string folderName = path.Substring(path.LastIndexOf('/') + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void CreateMissingScenes()
        {
            foreach (string scenePath in FoundationScenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
                {
                    continue;
                }

                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (scenePath.EndsWith("00_Boot.unity", StringComparison.Ordinal))
                {
                    var bootstrapObject = new GameObject("GameBootstrap");
                    bootstrapObject.AddComponent<GameBootstrap>();
                }

                EditorSceneManager.SaveScene(scene, scenePath);
            }
        }

        private static void ConfigureBuildScenes()
        {
            var foundationPaths = new HashSet<string>(FoundationScenes, StringComparer.OrdinalIgnoreCase);
            IEnumerable<EditorBuildSettingsScene> existingScenes = EditorBuildSettings.scenes
                .Where(scene => !foundationPaths.Contains(scene.path));
            IEnumerable<EditorBuildSettingsScene> foundationScenes = FoundationScenes
                .Select(path => new EditorBuildSettingsScene(path, true));

            EditorBuildSettings.scenes = foundationScenes.Concat(existingScenes).ToArray();
        }
    }
}
