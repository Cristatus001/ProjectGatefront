using System.Collections;
using System.Collections.Generic;
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
        private const float Tolerance = 0.001f;

        [UnityTest]
        public IEnumerator GameplayScene_UsesUnifiedSquadAndPreservesMovingWorld()
        {
            yield return SceneManager.LoadSceneAsync("02_Gameplay", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.3f);

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
            var trackChunks = new List<ScrollItem>();
            for (int index = 0; index < scrollItems.Length; index++)
            {
                if (scrollItems[index].IsRecyclable)
                {
                    trackChunks.Add(scrollItems[index]);
                }
            }

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(cameraController, Is.Not.Null);
            Assert.That(squadController, Is.Not.Null);
            Assert.That(worldScroller, Is.Not.Null);
            Assert.That(scrollItems.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(trackChunks.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(Camera.main, Is.Not.Null);
            Assert.That(bootstrap.IsInitialized, Is.True);
            Assert.That(player.IsInitialized, Is.True);
            Assert.That(cameraController.IsInitialized, Is.True);
            Assert.That(squadController.IsInitialized, Is.True);
            Assert.That(worldScroller.IsInitialized, Is.True);
            Assert.That(squadController.CurrentFormation, Is.EqualTo(FormationType.Compact));
            Assert.That(player.transform.Find("Visual"), Is.Null);

            var activeUnits = new List<SquadUnit>();
            squadController.CopyActiveUnitsTo(activeUnits);
            Assert.That(
                squadController.CurrentUnitCount,
                Is.EqualTo(squadController.ConfiguredStartingUnitCount));
            Assert.That(squadController.CurrentUnitCount, Is.EqualTo(20));
            Assert.That(activeUnits.Count, Is.EqualTo(20));
            Assert.That(
                squadController.FormationSlotCount,
                Is.EqualTo(squadController.CurrentUnitCount));
            AssertUnifiedUnitPresentation(activeUnits);

            Transform squadRoot = squadController.transform;
            bootstrap.SetMovementEnabled(false);
            var startingWorldPositions = new List<Vector3>(activeUnits.Count);
            var startingLocalPositions = new List<Vector3>(activeUnits.Count);
            for (int index = 0; index < activeUnits.Count; index++)
            {
                startingWorldPositions.Add(activeUnits[index].transform.position);
                startingLocalPositions.Add(activeUnits[index].transform.localPosition);
            }

            Vector3 rootPosition = player.transform.position;
            rootPosition.x -= 1f;
            player.transform.position = rootPosition;
            AssertWholeSquadShift(
                activeUnits,
                startingWorldPositions,
                startingLocalPositions,
                Vector3.left);

            var leftWorldPositions = new List<Vector3>(activeUnits.Count);
            for (int index = 0; index < activeUnits.Count; index++)
            {
                leftWorldPositions.Add(activeUnits[index].transform.position);
            }

            rootPosition.x += 2f;
            player.transform.position = rootPosition;
            AssertWholeSquadShift(
                activeUnits,
                leftWorldPositions,
                startingLocalPositions,
                Vector3.right * 2f);

            rootPosition.x = 0f;
            player.transform.position = rootPosition;
            bootstrap.SetMovementEnabled(true);

            AssertFormationFitsAtRootLimit(
                activeUnits,
                squadController.MinimumRootX,
                squadController.UnitUsableTrackHalfWidth);
            AssertFormationFitsAtRootLimit(
                activeUnits,
                squadController.MaximumRootX,
                squadController.UnitUsableTrackHalfWidth);

            var originalCompactTargets = new List<Vector3>(activeUnits.Count);
            for (int index = 0; index < activeUnits.Count; index++)
            {
                originalCompactTargets.Add(activeUnits[index].TargetLocalPosition);
            }

            Assert.That(squadController.AddUnits(30), Is.EqualTo(30));
            squadController.CopyActiveUnitsTo(activeUnits);
            Assert.That(activeUnits.Count, Is.EqualTo(50));
            AssertFormationFitsAtRootLimit(
                activeUnits,
                squadController.MinimumRootX,
                squadController.UnitUsableTrackHalfWidth);
            AssertFormationFitsAtRootLimit(
                activeUnits,
                squadController.MaximumRootX,
                squadController.UnitUsableTrackHalfWidth);

            Assert.That(squadController.RemoveUnits(30), Is.EqualTo(30));
            squadController.CopyActiveUnitsTo(activeUnits);
            Assert.That(activeUnits.Count, Is.EqualTo(20));
            for (int index = 0; index < activeUnits.Count; index++)
            {
                Assert.That(
                    activeUnits[index].TargetLocalPosition,
                    Is.EqualTo(originalCompactTargets[index]));
            }

            float startingForwardPosition = player.transform.position.z;
            float startingChunkPosition = trackChunks[0].transform.position.z;
            float startingDistance = worldScroller.DistanceTravelled;
            Vector3 startingCameraPosition = Camera.main.transform.position;
            yield return null;

            Assert.That(
                player.transform.position.z,
                Is.EqualTo(startingForwardPosition).Within(Tolerance));
            Assert.That(
                squadRoot.position.z,
                Is.EqualTo(startingForwardPosition).Within(Tolerance));
            Assert.That(
                trackChunks[0].transform.position.z,
                Is.LessThan(startingChunkPosition));
            Assert.That(worldScroller.DistanceTravelled, Is.GreaterThan(startingDistance));
            Assert.That(
                Camera.main.transform.position.x,
                Is.EqualTo(startingCameraPosition.x).Within(Tolerance));
            Assert.That(
                Camera.main.transform.position.z,
                Is.EqualTo(startingCameraPosition.z).Within(Tolerance));

            int recycleCount = worldScroller.RecycleCount;
            worldScroller.Tick(5f);
            Assert.That(worldScroller.RecycleCount, Is.GreaterThan(recycleCount));
            float[] chunkPositions = new float[trackChunks.Count];
            for (int index = 0; index < trackChunks.Count; index++)
            {
                chunkPositions[index] = trackChunks[index].transform.position.z;
            }

            System.Array.Sort(chunkPositions);
            for (int index = 1; index < chunkPositions.Length; index++)
            {
                Assert.That(
                    chunkPositions[index] - chunkPositions[index - 1],
                    Is.EqualTo(30f).Within(Tolerance));
            }

            Assert.That(squadController.SetFormation(FormationType.Column), Is.True);
            Assert.That(squadController.SetFormation(FormationType.Line), Is.True);
            Assert.That(squadController.SetFormation(FormationType.Wedge), Is.True);
            Assert.That(squadController.SetFormation(FormationType.Spread), Is.True);
            Assert.That(squadController.SetFormation(FormationType.Compact), Is.True);
            Assert.That(squadController.CurrentFormation, Is.EqualTo(FormationType.Compact));
        }

        private static void AssertUnifiedUnitPresentation(List<SquadUnit> units)
        {
            Transform firstVisual = units[0].transform.Find("Visual");
            Assert.That(firstVisual, Is.Not.Null);
            Transform firstBody = firstVisual.Find("Body");
            Transform firstHead = firstVisual.Find("HeadMarker");
            Transform firstForward = firstVisual.Find("ForwardMarker");
            Assert.That(firstBody, Is.Not.Null);
            Assert.That(firstHead, Is.Not.Null);
            Assert.That(firstForward, Is.Not.Null);
            Material bodyMaterial = firstBody.GetComponent<MeshRenderer>().sharedMaterial;
            Material headMaterial = firstHead.GetComponent<MeshRenderer>().sharedMaterial;

            for (int index = 0; index < units.Count; index++)
            {
                SquadUnit unit = units[index];
                Assert.That(unit.IsActive, Is.True);
                Assert.That(unit.gameObject.activeInHierarchy, Is.True);
                Transform visual = unit.transform.Find("Visual");
                Assert.That(visual, Is.Not.Null);
                Transform body = visual.Find("Body");
                Transform head = visual.Find("HeadMarker");
                Transform forward = visual.Find("ForwardMarker");
                Assert.That(body.localScale, Is.EqualTo(firstBody.localScale));
                Assert.That(head.localScale, Is.EqualTo(firstHead.localScale));
                Assert.That(forward.localScale, Is.EqualTo(firstForward.localScale));
                Assert.That(
                    body.GetComponent<MeshRenderer>().sharedMaterial,
                    Is.SameAs(bodyMaterial));
                Assert.That(
                    head.GetComponent<MeshRenderer>().sharedMaterial,
                    Is.SameAs(headMaterial));
                Transform weaponMount = unit.transform.Find("WeaponMount");
                Assert.That(weaponMount, Is.Not.Null);
                Assert.That(weaponMount.Find("FirePoint"), Is.Not.Null);
            }
        }

        private static void AssertWholeSquadShift(
            List<SquadUnit> units,
            List<Vector3> previousWorldPositions,
            List<Vector3> expectedLocalPositions,
            Vector3 expectedWorldDelta)
        {
            for (int index = 0; index < units.Count; index++)
            {
                Vector3 worldDelta = units[index].transform.position
                    - previousWorldPositions[index];
                Assert.That(
                    Vector3.Distance(worldDelta, expectedWorldDelta),
                    Is.LessThanOrEqualTo(Tolerance));
                Assert.That(
                    Vector3.Distance(
                        units[index].transform.localPosition,
                        expectedLocalPositions[index]),
                    Is.LessThanOrEqualTo(Tolerance));
            }
        }

        private static void AssertFormationFitsAtRootLimit(
            List<SquadUnit> units,
            float rootX,
            float usableHalfWidth)
        {
            for (int index = 0; index < units.Count; index++)
            {
                float targetWorldX = rootX + units[index].TargetLocalPosition.x;
                Assert.That(
                    targetWorldX,
                    Is.InRange(
                        -usableHalfWidth - Tolerance,
                        usableHalfWidth + Tolerance));
            }
        }
    }
}
