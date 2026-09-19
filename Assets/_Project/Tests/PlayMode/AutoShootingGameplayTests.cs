using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Enemies;
using ProjectGatefront.Gameplay.Player;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectGatefront.Tests.PlayMode
{
    public sealed class AutoShootingGameplayTests
    {
        private const float Tolerance = 0.01f;

        [UnityTest]
        public IEnumerator GameplayScene_AutoShootsDistributedEnemyTargets()
        {
            yield return SceneManager.LoadSceneAsync(
                "02_Gameplay",
                LoadSceneMode.Single);
            yield return new WaitForSeconds(0.2f);

            GameplayBootstrap bootstrap =
                Object.FindFirstObjectByType<GameplayBootstrap>();
            PlayerController player =
                Object.FindFirstObjectByType<PlayerController>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            EnemyWaveDirector director =
                Object.FindFirstObjectByType<EnemyWaveDirector>();
            EnemyPool enemyPool = director.EnemyPool;
            EnemyConfig weakConfig = director.LevelConfig.EnemyWaves[0].EnemyConfig;

            Assert.That(bootstrap.IsInitialized, Is.True);
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(20));
            Assert.That(weapons.ShooterCount, Is.EqualTo(20));
            Assert.That(
                Object.FindObjectsByType<CombatTargetDummy>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None).Length,
                Is.Zero);

            director.ResetCombat();
            director.SetWaveSchedulingEnabled(false);
            float spawnZ = squad.CombatContactZ + 18f;
            var spawned = new List<EnemyController>();
            for (int index = 0; index < 10; index++)
            {
                float x = (index - 4.5f) * 0.7f;
                spawned.Add(enemyPool.Spawn(
                    weakConfig,
                    new Vector3(x, 0f, spawnZ + ((index % 2) * 0.6f)),
                    -1));
            }

            yield return new WaitForSeconds(0.2f);
            Assert.That(enemyPool.ActiveEnemyCount, Is.EqualTo(10));
            Assert.That(weapons.ActiveTargetCount, Is.EqualTo(10));
            Assert.That(weapons.AssignedTargetCount, Is.GreaterThan(1));

            var activeWeapons = new List<WeaponController>();
            weapons.CopyActiveWeaponsTo(activeWeapons);
            var distinctTargets = new HashSet<ITargetable>();
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                Assert.That(activeWeapons[index].FirePoint, Is.Not.Null);
                Assert.That(
                    activeWeapons[index].transform.Find(
                        "WeaponMount/EquippedWeaponVisual"),
                    Is.Not.Null);
                if (activeWeapons[index].CurrentTarget != null)
                {
                    distinctTargets.Add(activeWeapons[index].CurrentTarget);
                }
            }

            Assert.That(distinctTargets.Count, Is.GreaterThan(1));
            int shotsBefore = weapons.TotalShotsFired;
            yield return new WaitForSeconds(0.8f);
            Assert.That(weapons.TotalShotsFired, Is.GreaterThan(shotsBefore));
            bool damageObserved = false;
            for (int index = 0; index < spawned.Count; index++)
            {
                damageObserved |= spawned[index].State != EnemyRuntimeState.Approaching
                    || spawned[index].CurrentHealth < spawned[index].MaximumHealth;
            }

            Assert.That(damageObserved, Is.True);

            int capacity = enemyPool.TotalCreated;
            EnemyController reusable = null;
            for (int index = 0; index < spawned.Count; index++)
            {
                if (spawned[index].IsTargetable)
                {
                    reusable = spawned[index];
                    break;
                }
            }

            Assert.That(reusable, Is.Not.Null);
            reusable.ApplyDamage(reusable.MaximumHealth + 1f);
            yield return new WaitForSeconds(0.2f);
            EnemyController reused = enemyPool.Spawn(
                weakConfig,
                new Vector3(0f, 0f, spawnZ),
                -1);
            Assert.That(reused, Is.SameAs(reusable));
            Assert.That(enemyPool.TotalCreated, Is.EqualTo(capacity));

            director.ResetCombat();
            director.SetWaveSchedulingEnabled(false);
            yield return new WaitForSeconds(0.15f);
            int shotsWithoutTargets = weapons.TotalShotsFired;
            yield return new WaitForSeconds(0.35f);
            Assert.That(weapons.CurrentTarget, Is.Null);
            Assert.That(weapons.TotalShotsFired, Is.EqualTo(shotsWithoutTargets));

            for (int index = 0; index < 5; index++)
            {
                enemyPool.Spawn(
                    weakConfig,
                    new Vector3(
                        (index - 2) * 0.8f,
                        0f,
                        squad.CombatContactZ + 15f),
                    -1);
            }

            yield return new WaitForSeconds(0.2f);
            int shotsBeforePause = weapons.TotalShotsFired;
            bootstrap.SetMovementEnabled(false);
            yield return new WaitForSeconds(0.3f);
            Assert.That(weapons.TotalShotsFired, Is.EqualTo(shotsBeforePause));
            bootstrap.SetMovementEnabled(true);

            var activeUnits = new List<SquadUnit>();
            squad.CopyActiveUnitsTo(activeUnits);
            Vector3 firstUnitWorld = activeUnits[0].transform.position;
            Vector3 firstUnitLocal = activeUnits[0].transform.localPosition;
            Vector3 playerPosition = player.transform.position;
            playerPosition.x += 0.4f;
            player.transform.position = playerPosition;
            Assert.That(
                activeUnits[0].transform.position.x - firstUnitWorld.x,
                Is.EqualTo(0.4f).Within(Tolerance));
            Assert.That(
                Vector3.Distance(
                    activeUnits[0].transform.localPosition,
                    firstUnitLocal),
                Is.LessThanOrEqualTo(Tolerance));
        }
    }
}
