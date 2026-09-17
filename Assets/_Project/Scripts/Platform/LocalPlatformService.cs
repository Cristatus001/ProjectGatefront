using System;
using UnityEngine;

namespace ProjectGatefront.Platform
{
    public sealed class LocalPlatformService : IPlatformService
    {
        private const string CloudKeyPrefix = "ProjectGatefront.Cloud.";

        public void Initialize()
        {
        }

        public void GameplayStarted()
        {
        }

        public void GameplayStopped()
        {
        }

        public void ShowInterstitial(Action<bool> onCompleted)
        {
            onCompleted?.Invoke(false);
        }

        public void ShowRewarded(Action<bool> onCompleted)
        {
            onCompleted?.Invoke(false);
        }

        public void SaveCloudData(string key, string data, Action<bool> onCompleted)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                onCompleted?.Invoke(false);
                return;
            }

            PlayerPrefs.SetString(CloudKeyPrefix + key, data ?? string.Empty);
            PlayerPrefs.Save();
            onCompleted?.Invoke(true);
        }

        public void LoadCloudData(string key, Action<string> onCompleted)
        {
            string data = string.IsNullOrWhiteSpace(key)
                ? null
                : PlayerPrefs.GetString(CloudKeyPrefix + key, null);

            onCompleted?.Invoke(data);
        }

        public string GetLanguage()
        {
            return Application.systemLanguage.ToString();
        }

        public void SetAudioState(bool isMuted)
        {
            AudioListener.pause = isMuted;
        }
    }
}
