using System.Collections;
using NUnit.Framework;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Player;
using ProjectGatefront.Gameplay.Squad;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectGatefront.Tests.PlayMode
{
    public sealed class GameplaySceneSmokeTests
    {
        [UnityTest]
        public IEnumerator GameplayScene_InitializesPlayerAndCamera()
        {
            yield return SceneManager.LoadSceneAsync("02_Gameplay", LoadSceneMode.Single);
            yield return null;

            GameplayBootstrap bootstrap = Object.FindFirstObjectByType<GameplayBootstrap>();
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            RunnerCameraController cameraController =
                Object.FindFirstObjectByType<RunnerCameraController>();
            SquadController squadController =
                Object.FindFirstObjectByType<SquadController>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(cameraController, Is.Not.Null);
            Assert.That(squadController, Is.Not.Null);
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(bootstrap.IsInitialized, Is.True);
            Assert.That(player.IsInitialized, Is.True);
            Assert.That(cameraController.IsInitialized, Is.True);
            Assert.That(squadController.IsInitialized, Is.True);
            Assert.That(
                squadController.CurrentUnitCount,
                Is.EqualTo(squadController.ConfiguredStartingUnitCount));
            Assert.That(
                squadController.FormationSlotCount,
                Is.EqualTo(squadController.CurrentUnitCount - 1));
        }
    }
}
