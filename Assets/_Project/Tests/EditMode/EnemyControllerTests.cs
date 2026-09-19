using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Enemies;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class EnemyControllerTests
    {
        private GameObject root;
        private GameObject squadRoot;
        private GameObject squadUnitPrefabObject;
        private GameObject enemyPrefabObject;
        private EnemyConfig enemyConfig;
        private SquadConfig squadConfig;
        private PlayerConfig playerConfig;
        private GameBalanceConfig balanceConfig;
        private TargetRegistry registry;
        private EnemyPool pool;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("EnemyControllerTestRoot");
            squadRoot = new GameObject("SquadRoot");
            squadUnitPrefabObject = new GameObject("SquadUnitPrefab");
            enemyPrefabObject = new GameObject("EnemyPrefab");
            enemyConfig = ScriptableObject.CreateInstance<EnemyConfig>();
            squadConfig = ScriptableObject.CreateInstance<SquadConfig>();
            playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();
            balanceConfig = ScriptableObject.CreateInstance<GameBalanceConfig>();

            SquadUnit squadUnit =
                squadUnitPrefabObject.AddComponent<SquadUnit>();
            SquadController squad = squadRoot.AddComponent<SquadController>();
            squad.Initialize(
                squadConfig,
                squadUnit,
                squadRoot.transform,
                playerConfig);

            registry = root.AddComponent<TargetRegistry>();
            WorldScroller world = root.AddComponent<WorldScroller>();
            world.Configure(balanceConfig, null, -28f);
            world.Initialize();

            Transform visual = new GameObject("Visual").transform;
            visual.SetParent(enemyPrefabObject.transform, false);
            enemyPrefabObject.AddComponent<EnemyMotor>();
            EnemyController enemyPrefab =
                enemyPrefabObject.AddComponent<EnemyController>();
            enemyPrefab.Configure(visual, 1f);

            pool = root.AddComponent<EnemyPool>();
            pool.Configure(enemyPrefab, root.transform, 1, 0.1f, 4f);
            pool.Initialize(registry, world, squad, 10);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(squadRoot);
            Object.DestroyImmediate(squadUnitPrefabObject);
            Object.DestroyImmediate(enemyPrefabObject);
            Object.DestroyImmediate(enemyConfig);
            Object.DestroyImmediate(squadConfig);
            Object.DestroyImmediate(playerConfig);
            Object.DestroyImmediate(balanceConfig);
        }

        [Test]
        public void Damage_KillsOnceUnregistersAndReuseRestoresHealth()
        {
            int killedCount = 0;
            pool.EnemyKilled += _ => killedCount++;
            EnemyController enemy = pool.Spawn(
                enemyConfig,
                new Vector3(0f, 0f, 20f),
                0);
            int capacity = pool.TotalCreated;

            Assert.That(registry.ActiveTargetCount, Is.EqualTo(1));
            float initialHealth = enemy.CurrentHealth;
            DamageResult firstHit = enemy.ApplyDamage(1f);
            Assert.That(firstHit.AppliedDamage, Is.EqualTo(1f));
            Assert.That(enemy.CurrentHealth, Is.EqualTo(initialHealth - 1f));

            DamageResult lethal = enemy.ApplyDamage(initialHealth + 1f);
            Assert.That(lethal.WasKilled, Is.True);
            Assert.That(killedCount, Is.EqualTo(1));
            Assert.That(registry.ActiveTargetCount, Is.Zero);
            DamageResult repeated = enemy.ApplyDamage(10f);
            Assert.That(repeated.AppliedDamage, Is.Zero);
            Assert.That(killedCount, Is.EqualTo(1));

            pool.Tick(0.2f);
            Assert.That(pool.ActiveEnemyCount, Is.Zero);
            EnemyController reused = pool.Spawn(
                enemyConfig,
                new Vector3(0f, 0f, 20f),
                1);
            Assert.That(reused, Is.SameAs(enemy));
            Assert.That(reused.CurrentHealth, Is.EqualTo(enemyConfig.MaximumHealth));
            Assert.That(registry.ActiveTargetCount, Is.EqualTo(1));
            Assert.That(pool.TotalCreated, Is.EqualTo(capacity));
        }
    }
}
