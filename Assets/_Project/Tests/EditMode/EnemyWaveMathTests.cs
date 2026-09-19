using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Enemies;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class EnemyWaveMathTests
    {
        private EnemyConfig enemyConfig;

        [SetUp]
        public void SetUp()
        {
            enemyConfig = ScriptableObject.CreateInstance<EnemyConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(enemyConfig);
        }

        [Test]
        public void Waves_SortByDistanceAndRespectThreshold()
        {
            var waves = new List<EnemyWaveDefinition>
            {
                CreateWave(60f, 5, EnemySpawnPattern.Row, 1),
                CreateWave(10f, 5, EnemySpawnPattern.Row, 1),
                CreateWave(35f, 5, EnemySpawnPattern.Row, 1)
            };

            EnemyWaveMath.SortByDistance(waves);

            Assert.That(waves[0].SpawnDistance, Is.EqualTo(10f));
            Assert.That(waves[1].SpawnDistance, Is.EqualTo(35f));
            Assert.That(waves[2].SpawnDistance, Is.EqualTo(60f));
            Assert.That(
                EnemyWaveMath.IsSpawnThresholdReached(34.9f, 35f),
                Is.False);
            Assert.That(
                EnemyWaveMath.IsSpawnThresholdReached(35f, 35f),
                Is.True);
        }

        [Test]
        public void ActiveLimit_ClampsInvalidAndOversizedWaveCounts()
        {
            Assert.That(EnemyWaveMath.ClampSpawnCount(-5, 0, 100), Is.Zero);
            Assert.That(EnemyWaveMath.ClampSpawnCount(50, 90, 100), Is.EqualTo(10));
            Assert.That(EnemyWaveMath.ClampSpawnCount(50, 100, 100), Is.Zero);
        }

        [TestCase(EnemySpawnPattern.Row, 1)]
        [TestCase(EnemySpawnPattern.Grid, 3)]
        [TestCase(EnemySpawnPattern.Cluster, 3)]
        public void SpawnPatterns_ProduceRequestedRoadBoundPositions(
            EnemySpawnPattern pattern,
            int rows)
        {
            EnemyWaveDefinition wave = CreateWave(10f, 15, pattern, rows);
            var offsets = new List<Vector3>();

            EnemySpawnPatternMath.GenerateOffsets(
                wave,
                4.2f,
                0.55f,
                wave.Count,
                offsets);

            Assert.That(offsets.Count, Is.EqualTo(15));
            for (int index = 0; index < offsets.Count; index++)
            {
                Assert.That(offsets[index].x, Is.InRange(-3.65f, 3.65f));
            }
        }

        private EnemyWaveDefinition CreateWave(
            float distance,
            int count,
            EnemySpawnPattern pattern,
            int rows)
        {
            return new EnemyWaveDefinition(
                distance,
                enemyConfig,
                count,
                pattern,
                rows,
                1f,
                1f,
                0f,
                30f);
        }
    }
}
