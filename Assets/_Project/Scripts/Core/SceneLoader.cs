using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectGatefront.Core
{
    public sealed class SceneLoader
    {
        public AsyncOperation LoadAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("A scene name is required.", nameof(sceneName));
            }

            GameLog.Verbose($"Loading scene '{sceneName}' in {mode} mode.");
            return SceneManager.LoadSceneAsync(sceneName, mode);
        }

        public bool CanLoad(string sceneName)
        {
            return !string.IsNullOrWhiteSpace(sceneName)
                && Application.CanStreamedLevelBeLoaded(sceneName);
        }
    }
}
