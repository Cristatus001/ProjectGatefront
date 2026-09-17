using System;
using ProjectGatefront.Platform;
using UnityEngine;

namespace ProjectGatefront.Core
{
    [DisallowMultipleComponent]
    public sealed class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private bool persistBetweenScenes = true;

        private bool isInitialized;

        public GameStateMachine StateMachine { get; private set; }

        public SceneLoader SceneLoader { get; private set; }

        public IPlatformService PlatformService { get; private set; }

        private void Awake()
        {
            if (!isInitialized)
            {
                Initialize(new LocalPlatformService(), new SceneLoader());
            }
        }

        public void Initialize(IPlatformService platformService, SceneLoader sceneLoader)
        {
            if (isInitialized)
            {
                return;
            }

            PlatformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
            SceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            StateMachine = new GameStateMachine(GameState.Boot);

            PlatformService.Initialize();
            isInitialized = true;

            if (persistBetweenScenes)
            {
                DontDestroyOnLoad(gameObject);
            }

            GameLog.Verbose("Game services initialized.", this);
        }
    }
}
