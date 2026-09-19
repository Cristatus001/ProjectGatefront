using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Squad;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class SquadControllerTests
    {
        private const float Tolerance = 0.0001f;

        private GameObject squadRootObject;
        private GameObject unitTemplateObject;
        private PlayerConfig playerConfig;
        private SquadConfig config;
        private SquadController controller;

        [SetUp]
        public void SetUp()
        {
            squadRootObject = new GameObject("TestSquadRoot");
            unitTemplateObject = new GameObject("TestSquadUnitTemplate");
            SquadUnit unitTemplate = unitTemplateObject.AddComponent<SquadUnit>();
            unitTemplateObject.SetActive(false);

            config = ScriptableObject.CreateInstance<SquadConfig>();
            playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();
            controller = squadRootObject.AddComponent<SquadController>();
            controller.Initialize(
                config,
                unitTemplate,
                squadRootObject.transform,
                playerConfig);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(squadRootObject);
            Object.DestroyImmediate(unitTemplateObject);
            Object.DestroyImmediate(playerConfig);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Initialize_UsesCompactFormationByDefault()
        {
            Assert.That(controller.CurrentFormation, Is.EqualTo(FormationType.Compact));
        }

        [Test]
        public void CurrentUnitCount_EqualsVisiblePooledSoldiersAtOneFiveAndTwenty()
        {
            AssertVisibleCount(5);

            controller.RemoveUnits(100);
            AssertVisibleCount(1);

            controller.AddUnits(4);
            AssertVisibleCount(5);

            controller.AddUnits(15);
            AssertVisibleCount(20);
        }

        [Test]
        public void AddUnits_IncreasesVisibleSoldierCount()
        {
            int initialCount = controller.CurrentUnitCount;
            int added = controller.AddUnits(3);

            Assert.That(added, Is.EqualTo(3));
            Assert.That(controller.CurrentUnitCount, Is.EqualTo(initialCount + 3));
            Assert.That(controller.FormationSlotCount, Is.EqualTo(controller.CurrentUnitCount));
        }

        [Test]
        public void RemoveUnits_DecreasesVisibleSoldierCountButKeepsMinimumOne()
        {
            int removed = controller.RemoveUnits(100);

            Assert.That(removed, Is.EqualTo(4));
            AssertVisibleCount(1);
            Assert.That(controller.RemoveUnit(), Is.Zero);
        }

        [Test]
        public void ApplyCasualties_CanReachZeroAndFiresDefeatOncePerReset()
        {
            int defeatedCount = 0;
            controller.SquadDefeated += () => defeatedCount++;

            controller.AddUnits(15);
            AssertVisibleCount(20);
            Assert.That(controller.ApplyCasualties(1), Is.EqualTo(1));
            AssertVisibleCount(19);
            Assert.That(controller.ApplyCasualties(0), Is.Zero);
            Assert.That(controller.ApplyCasualties(-1), Is.Zero);

            controller.ResetSquad();
            AssertVisibleCount(5);
            Assert.That(controller.ApplyCasualties(3), Is.EqualTo(3));
            AssertVisibleCount(2);

            controller.ResetSquad();
            controller.RemoveUnits(4);
            AssertVisibleCount(1);
            Assert.That(controller.ApplyCasualties(1), Is.EqualTo(1));
            AssertVisibleCount(0);
            Assert.That(controller.IsDefeated, Is.True);
            Assert.That(defeatedCount, Is.EqualTo(1));
            Assert.That(controller.ApplyCasualties(100), Is.Zero);
            Assert.That(defeatedCount, Is.EqualTo(1));

            controller.ResetSquad();
            Assert.That(controller.IsDefeated, Is.False);
            AssertVisibleCount(5);
            Assert.That(controller.ApplyCasualties(100), Is.EqualTo(5));
            Assert.That(defeatedCount, Is.EqualTo(2));
        }

        [Test]
        public void AddUnits_ClampsAtConfiguredMaximum()
        {
            controller.AddUnits(100);
            AssertVisibleCount(config.MaxSquadSize);
        }

        [Test]
        public void UnitCountChange_RecalculatesFormationWithoutInnerHoles()
        {
            var units = new List<SquadUnit>();
            controller.CopyActiveUnitsTo(units);
            var originalTargets = new List<Vector3>(units.Count);
            for (int index = 0; index < units.Count; index++)
            {
                originalTargets.Add(units[index].TargetLocalPosition);
            }

            int initialRevision = controller.FormationRevision;
            controller.AddUnits(5);
            controller.RemoveUnits(5);
            controller.CopyActiveUnitsTo(units);

            Assert.That(controller.FormationRevision, Is.EqualTo(initialRevision + 2));
            Assert.That(controller.FormationSlotCount, Is.EqualTo(controller.CurrentUnitCount));
            for (int index = 0; index < units.Count; index++)
            {
                Assert.That(units[index].SlotIndex, Is.EqualTo(index));
                Assert.That(units[index].TargetLocalPosition, Is.EqualTo(originalTargets[index]));
            }
        }

        [Test]
        public void RemovedUnit_IsReusedBySharedPool()
        {
            var units = new List<SquadUnit>();
            controller.CopyActiveUnitsTo(units);
            SquadUnit removedUnit = units[units.Count - 1];

            controller.RemoveUnit();
            controller.AddUnit();
            controller.CopyActiveUnitsTo(units);

            Assert.That(units, Does.Contain(removedUnit));
        }

        [Test]
        public void RootTranslation_MovesEveryUnitBySameDeltaWithoutStretching()
        {
            var units = new List<SquadUnit>();
            controller.CopyActiveUnitsTo(units);
            var initialWorldPositions = new List<Vector3>(units.Count);
            var initialLocalPositions = new List<Vector3>(units.Count);
            for (int index = 0; index < units.Count; index++)
            {
                initialWorldPositions.Add(units[index].transform.position);
                initialLocalPositions.Add(units[index].transform.localPosition);
            }

            squadRootObject.transform.position += Vector3.left * 2f;
            for (int index = 0; index < units.Count; index++)
            {
                Vector3 delta = units[index].transform.position
                    - initialWorldPositions[index];
                Assert.That(delta, Is.EqualTo(Vector3.left * 2f));
                Assert.That(
                    units[index].transform.localPosition,
                    Is.EqualTo(initialLocalPositions[index]));
            }

            var leftWorldPositions = new List<Vector3>(units.Count);
            for (int index = 0; index < units.Count; index++)
            {
                leftWorldPositions.Add(units[index].transform.position);
            }

            squadRootObject.transform.position += Vector3.right * 4f;
            for (int index = 0; index < units.Count; index++)
            {
                Vector3 reversalDelta = units[index].transform.position
                    - leftWorldPositions[index];
                Assert.That(reversalDelta, Is.EqualTo(Vector3.right * 4f));
                Assert.That(
                    units[index].transform.localPosition,
                    Is.EqualTo(initialLocalPositions[index]));
            }
        }

        [Test]
        public void RootLimits_ContainAllCurrentFormationTargetsAtBothEdges()
        {
            controller.AddUnits(15);
            var units = new List<SquadUnit>();
            controller.CopyActiveUnitsTo(units);
            float usableHalfWidth = controller.UnitUsableTrackHalfWidth;

            for (int index = 0; index < units.Count; index++)
            {
                float localX = units[index].TargetLocalPosition.x;
                Assert.That(
                    controller.MinimumRootX + localX,
                    Is.InRange(
                        -usableHalfWidth - Tolerance,
                        usableHalfWidth + Tolerance));
                Assert.That(
                    controller.MaximumRootX + localX,
                    Is.InRange(
                        -usableHalfWidth - Tolerance,
                        usableHalfWidth + Tolerance));
            }
        }

        private void AssertVisibleCount(int expectedCount)
        {
            var units = new List<SquadUnit>();
            controller.CopyActiveUnitsTo(units);
            Assert.That(controller.CurrentUnitCount, Is.EqualTo(expectedCount));
            Assert.That(controller.ActiveUnitCount, Is.EqualTo(expectedCount));
            Assert.That(controller.FormationSlotCount, Is.EqualTo(expectedCount));
            Assert.That(units.Count, Is.EqualTo(expectedCount));
            for (int index = 0; index < units.Count; index++)
            {
                Assert.That(units[index].IsActive, Is.True);
            }
        }
    }
}
