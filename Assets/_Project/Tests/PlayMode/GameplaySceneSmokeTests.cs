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
        public IEnumerator GameplayScene_InitializesMovementCameraAndSquad()
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

            float startingForwardPosition = player.transform.position.z;
            int startingSquadCount = squadController.CurrentUnitCount;

            Assert.That(
                squadController.SetFormation(FormationType.Wedge),
                Is.True);
            Assert.That(squadController.AddUnits(2), Is.EqualTo(2));
            Assert.That(squadController.RemoveUnits(2), Is.EqualTo(2));

            yield return null;

            Assert.That(player.transform.position.z, Is.GreaterThan(startingForwardPosition));
            Assert.That(squadController.CurrentFormation, Is.EqualTo(FormationType.Wedge));
            Assert.That(squadController.CurrentUnitCount, Is.EqualTo(startingSquadCount));
            Assert.That(
                squadController.FormationSlotCount,
                Is.EqualTo(squadController.CurrentUnitCount - 1));

            CharacterController characterController =
                player.GetComponent<CharacterController>();
            Assert.That(characterController, Is.Not.Null);
            characterController.enabled = false;
            Vector3 edgePosition = player.transform.position;
            edgePosition.x = player.LeaderUsableTrackHalfWidth;
            player.transform.position = edgePosition;
            squadController.RefreshFormation();

            Assert.That(squadController.CurrentHorizontalCorrection, Is.LessThan(0f));
            Assert.That(
                squadController.SetFormation(FormationType.Spread),
                Is.True);
            Assert.That(squadController.CurrentHorizontalCorrection, Is.LessThan(0f));
            Assert.That(squadController.AddUnits(15), Is.EqualTo(15));
            Assert.That(squadController.RemoveUnits(15), Is.EqualTo(15));
            Assert.That(squadController.CurrentUnitCount, Is.EqualTo(startingSquadCount));
            characterController.enabled = true;
        }
    }
}
