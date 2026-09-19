using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Enemies;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class ContinuousSpawnMathTests
    {
        [Test]
        public void Sections_TransitionAtAuthoredDistanceAndIncreaseDensity()
        {
            var sections = new List<CombatSectionDefinition>
            {
                CreateSection(80f, 180f, 0.6f, 5, 7),
                CreateSection(0f, 80f, 1f, 3, 5),
                CreateSection(180f, 300f, 0.35f, 8, 10)
            };
            ContinuousSpawnMath.SortByStartDistance(sections);

            Assert.That(ContinuousSpawnMath.FindSection(sections, 0f), Is.Zero);
            Assert.That(ContinuousSpawnMath.FindSection(sections, 79.9f), Is.Zero);
            Assert.That(ContinuousSpawnMath.FindSection(sections, 80f), Is.EqualTo(1));
            Assert.That(ContinuousSpawnMath.FindSection(sections, 180f), Is.EqualTo(2));
            Assert.That(ContinuousSpawnMath.FindSection(sections, 300f), Is.EqualTo(-1));
            Assert.That(sections[2].SpawnInterval, Is.LessThan(sections[0].SpawnInterval));
            Assert.That(sections[2].MinimumGroupSize, Is.GreaterThan(sections[0].MinimumGroupSize));
        }

        [Test]
        public void Scheduling_GroupSizesAndActiveCapAreBounded()
        {
            CombatSectionDefinition section = CreateSection(0f, 80f, 0.5f, 3, 5);
            Assert.That(ContinuousSpawnMath.NextSpawnDistance(8f, 8f, section), Is.EqualTo(12f));
            Assert.That(ContinuousSpawnMath.GroupSize(section, 0), Is.EqualTo(3));
            Assert.That(ContinuousSpawnMath.GroupSize(section, 1), Is.EqualTo(4));
            Assert.That(ContinuousSpawnMath.GroupSize(section, 2), Is.EqualTo(5));
            Assert.That(ContinuousSpawnMath.GroupSize(section, 3), Is.EqualTo(3));
            Assert.That(ContinuousSpawnMath.ClampToActiveLimit(20, 95, 100), Is.EqualTo(5));
            Assert.That(ContinuousSpawnMath.ClampToActiveLimit(20, 100, 100), Is.Zero);
        }

        [TestCase(EnemySpawnPattern.Row, 1)]
        [TestCase(EnemySpawnPattern.Grid, 3)]
        [TestCase(EnemySpawnPattern.Cluster, 3)]
        public void SpawnPatterns_ProduceRequestedRoadBoundPositions(
            EnemySpawnPattern pattern,
            int rows)
        {
            var section = new CombatSectionDefinition(
                0f, 80f, 1f, 15, 15, pattern, rows, 1f, 1f, 0f, 30f);
            var offsets = new List<Vector3>();
            EnemySpawnPatternMath.GenerateOffsets(section, 4.2f, 0.55f, 15, offsets);

            Assert.That(offsets.Count, Is.EqualTo(15));
            for (int index = 0; index < offsets.Count; index++)
            {
                Assert.That(offsets[index].x, Is.InRange(-3.65f, 3.65f));
            }
        }

        private static CombatSectionDefinition CreateSection(
            float start,
            float end,
            float interval,
            int minimum,
            int maximum)
        {
            return new CombatSectionDefinition(
                start, end, interval, minimum, maximum,
                EnemySpawnPattern.Cluster, 2, 1f, 1f, 0f, 30f);
        }
    }
}
