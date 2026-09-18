using System.Collections;
using NUnit.Framework;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Player;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.World;
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
            WorldScroller worldScroller =
                Object.FindFirstObjectByType<WorldScroller>();
            ScrollItem[] scrollItems = Object.FindObjectsByType<ScrollItem>(
                FindObjectsSortMode.None);

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(cameraController, Is.Not.Null);
            Assert.That(squadController, Is.Not.Null);
            Assert.That(worldScroller, Is.Not.Null);
            Assert.That(scrollItems.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(bootstrap.IsInitialized, Is.True);
            Assert.That(player.IsInitialized, Is.True);
            Assert.That(cameraController.IsInitialized, Is.True);
            Assert.That(squadController.IsInitialized, Is.True);
            Assert.That(worldScroller.IsInitialized, Is.True);
            Assert.That(squadController.CurrentFormation, Is.EqualTo(FormationType.Compact));
            Assert.That(
                squadController.CurrentUnitCount,
                Is.EqualTo(squadController.ConfiguredStartingUnitCount));
            Assert.That(
                squadController.FormationSlotCount,
                Is.EqualTo(squadController.CurrentUnitCount - 1));

            float startingForwardPosition = player.transform.position.z;
            float startingChunkPosition = scrollItems[0].transform.position.z;
            float startingDistance = worldScroller.DistanceTravelled;
            Vector3 startingCameraPosition = Camera.main.transform.position;
            int startingSquadCount = squadController.CurrentUnitCount;

            Assert.That(
                squadController.SetFormation(FormationType.Wedge),
                Is.True);
            Assert.That(squadController.AddUnits(2), Is.EqualTo(2));
            Assert.That(squadController.RemoveUnits(2), Is.EqualTo(2));

            yield return null;

            Assert.That(
                player.transform.position.z,
                Is.EqualTo(startingForwardPosition).Within(0.001f));
            Assert.That(
                scrollItems[0].transform.position.z,
                Is.LessThan(startingChunkPosition));
            Assert.That(worldScroller.DistanceTravelled, Is.GreaterThan(startingDistance));
            Assert.That(
                Camera.main.transform.position.z,
                Is.EqualTo(startingCameraPosition.z).Within(0.001f));
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

            int recycleCount = worldScroller.RecycleCount;
            worldScroller.Tick(5f);
            Assert.That(worldScroller.RecycleCount, Is.GreaterThan(recycleCount));
            float[] chunkPositions = new float[scrollItems.Length];
            for (int index = 0; index < scrollItems.Length; index++)
            {
                chunkPositions[index] = scrollItems[index].transform.position.z;
            }

            System.Array.Sort(chunkPositions);
            for (int index = 1; index < chunkPositions.Length; index++)
            {
                Assert.That(
                    chunkPositions[index] - chunkPositions[index - 1],
                    Is.EqualTo(30f).Within(0.001f));
            }
            characterController.enabled = true;
        }
    }
}
