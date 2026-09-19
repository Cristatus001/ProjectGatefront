using ProjectGatefront.Core;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Enemies;
using ProjectGatefront.Gameplay.Player;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.Weapons;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private PlayerConfig playerConfig;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private RunnerCameraController runnerCamera;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private SquadConfig squadConfig;
        [SerializeField] private SquadController squadController;
        [SerializeField] private GameBalanceConfig gameBalanceConfig;
        [SerializeField] private WorldScroller worldScroller;
        [SerializeField] private SquadWeaponSystem squadWeaponSystem;
        [SerializeField] private ContinuousEnemyDirector continuousEnemyDirector;
        [SerializeField] private bool movementEnabledOnStart = true;

        public bool IsInitialized { get; private set; }

        public bool IsDefeated { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        public void Configure(
            PlayerConfig config,
            PlayerController controller,
            RunnerCameraController cameraController,
            Transform target)
        {
            playerConfig = config;
            playerController = controller;
            runnerCamera = cameraController;
            cameraTarget = target;
        }

        public void ConfigureSquad(
            SquadConfig config,
            SquadController controller)
        {
            squadConfig = config;
            squadController = controller;
        }

        public void ConfigureWorld(
            GameBalanceConfig balanceConfig,
            WorldScroller scroller)
        {
            gameBalanceConfig = balanceConfig;
            worldScroller = scroller;
        }

        public void ConfigureCombat(SquadWeaponSystem weaponSystem)
        {
            squadWeaponSystem = weaponSystem;
        }

        public void ConfigureEnemyCombat(ContinuousEnemyDirector director)
        {
            continuousEnemyDirector = director;
        }

        public void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (playerConfig == null
                || playerController == null
                || runnerCamera == null
                || cameraTarget == null
                || squadConfig == null
                || squadController == null
                || gameBalanceConfig == null
                || worldScroller == null
                || squadWeaponSystem == null
                || continuousEnemyDirector == null)
            {
                GameLog.Error(
                    "GameplayBootstrap is missing required scene references.",
                    this);
                enabled = false;
                return;
            }

            playerController.Initialize(playerConfig, new PointerDragInputSource());
            runnerCamera.Initialize(cameraTarget);
            squadController.Initialize(playerConfig, playerController);
            squadController.SquadDefeated += HandleSquadDefeated;
            worldScroller.Initialize();
            squadWeaponSystem.Initialize();
            continuousEnemyDirector.Initialize();
            SetGameplaySystemsEnabled(movementEnabledOnStart);
            IsInitialized = true;
        }

        public void SetMovementEnabled(bool enabled)
        {
            if (enabled && IsDefeated)
            {
                return;
            }

            SetGameplaySystemsEnabled(enabled);
        }

        public void ResetPrototypeRun()
        {
            if (!IsInitialized)
            {
                return;
            }

            SetGameplaySystemsEnabled(false);
            continuousEnemyDirector.ResetCombat();
            squadController.ResetSquad();
            worldScroller.ResetScroll();
            squadWeaponSystem.ResetCombat();
            IsDefeated = false;
            SetGameplaySystemsEnabled(true);
        }

        private void SetGameplaySystemsEnabled(bool enabled)
        {
            playerController.SetMovementEnabled(enabled);
            runnerCamera.SetFollowEnabled(enabled);
            worldScroller.SetScrollingEnabled(enabled);
            squadWeaponSystem.SetCombatEnabled(enabled);
            continuousEnemyDirector.SetSimulationEnabled(enabled);
        }

        private void HandleSquadDefeated()
        {
            if (IsDefeated)
            {
                return;
            }

            IsDefeated = true;
            SetGameplaySystemsEnabled(false);
            squadWeaponSystem.ClearProjectiles();
        }

        private void OnDestroy()
        {
            if (squadController != null)
            {
                squadController.SquadDefeated -= HandleSquadDefeated;
            }
        }
    }
}
