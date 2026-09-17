using System;

namespace ProjectGatefront.Platform
{
    public interface IPlatformService
    {
        void Initialize();

        void GameplayStarted();

        void GameplayStopped();

        void ShowInterstitial(Action<bool> onCompleted);

        void ShowRewarded(Action<bool> onCompleted);

        void SaveCloudData(string key, string data, Action<bool> onCompleted);

        void LoadCloudData(string key, Action<string> onCompleted);

        string GetLanguage();

        void SetAudioState(bool isMuted);
    }
}
