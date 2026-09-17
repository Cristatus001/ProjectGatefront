using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Squad;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class SquadControllerTests
    {
        private GameObject squadRootObject;
        private GameObject unitTemplateObject;
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
            controller = squadRootObject.AddComponent<SquadController>();
            controller.Initialize(config, unitTemplate, squadRootObject.transform);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(squadRootObject);
            Object.DestroyImmediate(unitTemplateObject);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void AddUnits_IncreasesTotalCombatantCount()
        {
            int initialCount = controller.CurrentUnitCount;

            int added = controller.AddUnits(3);

            Assert.That(added, Is.EqualTo(3));
            Assert.That(controller.CurrentUnitCount, Is.EqualTo(initialCount + 3));
        }

        [Test]
        public void RemoveUnits_DecreasesTotalCombatantCount()
        {
            int initialCount = controller.CurrentUnitCount;

            int removed = controller.RemoveUnits(2);

            Assert.That(removed, Is.EqualTo(2));
            Assert.That(controller.CurrentUnitCount, Is.EqualTo(initialCount - 2));
        }

        [Test]
        public void RemoveUnits_CannotRemoveLeaderOrBecomeNegative()
        {
            controller.RemoveUnits(100);

            Assert.That(controller.CurrentUnitCount, Is.EqualTo(1));
            Assert.That(controller.ActiveFollowerCount, Is.Zero);
        }

        [Test]
        public void AddUnits_ClampsAtConfiguredMaximum()
        {
            controller.AddUnits(100);

            Assert.That(
                controller.CurrentUnitCount,
                Is.EqualTo(config.MaxSquadSize));
        }

        [Test]
        public void UnitCountChange_RecalculatesFormation()
        {
            int initialRevision = controller.FormationRevision;

            controller.AddUnit();

            Assert.That(controller.FormationRevision, Is.GreaterThan(initialRevision));
            Assert.That(
                controller.FormationSlotCount,
                Is.EqualTo(controller.CurrentUnitCount - 1));
        }

        [Test]
        public void RemovedFollower_IsReusedByPool()
        {
            var followers = new List<SquadUnit>();
            controller.CopyActiveFollowersTo(followers);
            SquadUnit removedFollower = followers[followers.Count - 1];

            controller.RemoveUnit();
            controller.AddUnit();
            controller.CopyActiveFollowersTo(followers);

            Assert.That(followers, Does.Contain(removedFollower));
        }
    }
}
