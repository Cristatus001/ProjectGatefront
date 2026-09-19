using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Enemies;
using ProjectGatefront.Gameplay.Player;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.Weapons;
using ProjectGatefront.Gameplay.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectGatefront.Tests.PlayMode
{
    public sealed class EnemyCombatGameplayTests
    {
        [UnityTest]
        public IEnumerator OneProjectileHit_KillsNormalEnemyImmediately()
        {
            yield return LoadGameplayScene();
            ContinuousEnemyDirector director =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            director.ResetCombat();
            director.SetSpawnSchedulingEnabled(false);
            weapons.SetCombatEnabled(false);

            EnemyController enemy = director.EnemyPool.Spawn(
                director.LevelConfig.NormalEnemy,
                new Vector3(0f, 0f, squad.CombatContactZ + 12f),
                -1);
            Vector3 aim = enemy.AimPosition;
            Projectile projectile = weapons.ProjectilePool.Acquire(
                aim - (Vector3.forward * 1.5f),
                Vector3.forward,
                weapons.CurrentRuntimeStats);
            projectile.Tick(0.1f);

            Assert.That(enemy.MaximumHealth, Is.EqualTo(1f));
            Assert.That(enemy.CurrentHealth, Is.Zero);
            Assert.That(enemy.IsTargetable, Is.False);
            Assert.That(enemy.State, Is.EqualTo(EnemyRuntimeState.Dying));
            Assert.That(weapons.ActiveTargetCount, Is.Zero);
            Assert.That(projectile.IsLaunched, Is.False);
            Assert.That(enemy.ApplyDamage(1f).AppliedDamage, Is.Zero);
        }

        [UnityTest]
        public IEnumerator AlignedContact_AppliesCasualtiesAndRecompacts()
        {
            yield return LoadGameplayScene();
            ContinuousEnemyDirector director =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            director.ResetCombat();
            director.SetSpawnSchedulingEnabled(false);
            weapons.SetCombatEnabled(false);
            EnemyConfig normal = director.LevelConfig.NormalEnemy;
            int startingCount = squad.CurrentUnitCount;
            int startingRevision = squad.FormationRevision;
            int reportedCasualties = 0;
            director.EnemyPool.EnemyReachedSquad +=
                (_, casualties) => reportedCasualties += casualties;

            director.EnemyPool.Spawn(
                normal,
                new Vector3(
                    squad.transform.position.x,
                    0f,
                    squad.CombatContactZ + normal.TargetRadius + 0.02f),
                -1);
            yield return new WaitForSeconds(0.1f);

            Assert.That(reportedCasualties, Is.EqualTo(1));
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(startingCount - 1));
            Assert.That(squad.ActiveUnitCount, Is.EqualTo(squad.CurrentUnitCount));
            Assert.That(squad.FormationRevision, Is.GreaterThan(startingRevision));
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator LateralDodge_AllowsEnemyToPassWithoutCasualty()
        {
            yield return LoadGameplayScene();
            ContinuousEnemyDirector director =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            PlayerController player =
                Object.FindFirstObjectByType<PlayerController>();
            director.ResetCombat();
            director.SetSpawnSchedulingEnabled(false);
            weapons.SetCombatEnabled(false);
            EnemyConfig weak = director.LevelConfig.NormalEnemy;

            Vector3 playerPosition = player.transform.position;
            playerPosition.x = squad.MaximumRootX;
            player.transform.position = playerPosition;
            player.SetMovementEnabled(false);
            float enemyX = -player.DefaultUsableTrackHalfWidth;
            Assert.That(
                EnemyContactMath.OverlapsSquadHorizontally(
                    enemyX,
                    weak.TargetRadius,
                    squad.CurrentWorldMinimumX,
                    squad.CurrentWorldMaximumX,
                    0.12f),
                Is.False);
            int startingCount = squad.CurrentUnitCount;

            EnemyController enemy = director.EnemyPool.Spawn(
                weak,
                new Vector3(
                    enemyX,
                    0f,
                    squad.CombatContactZ + weak.TargetRadius + 0.02f),
                -1);
            yield return new WaitForSeconds(0.1f);

            Assert.That(squad.CurrentUnitCount, Is.EqualTo(startingCount));
            Assert.That(enemy.IsTargetable, Is.False);
            Assert.That(
                enemy.State == EnemyRuntimeState.Passed
                    || enemy.State == EnemyRuntimeState.Inactive,
                Is.True);
            yield return new WaitForSeconds(0.55f);
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(startingCount));
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator SquadDefeat_StopsWorldWeaponsAndEnemiesExactlyOnce()
        {
            yield return LoadGameplayScene();
            GameplayBootstrap bootstrap =
                Object.FindFirstObjectByType<GameplayBootstrap>();
            ContinuousEnemyDirector director =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            WorldScroller world =
                Object.FindFirstObjectByType<WorldScroller>();
            director.ResetCombat();
            director.SetSpawnSchedulingEnabled(false);
            EnemyConfig weak = director.LevelConfig.NormalEnemy;
            EnemyController enemy = director.EnemyPool.Spawn(
                weak,
                new Vector3(0f, 0f, squad.CombatContactZ + 12f),
                -1);
            int defeatedEvents = 0;
            squad.SquadDefeated += () => defeatedEvents++;

            int casualties = squad.ApplyCasualties(1000);
            Vector3 stoppedEnemyPosition = enemy.transform.position;
            float stoppedDistance = world.DistanceTravelled;
            yield return new WaitForSeconds(0.2f);

            Assert.That(casualties, Is.EqualTo(20));
            Assert.That(squad.CurrentUnitCount, Is.Zero);
            Assert.That(squad.ActiveUnitCount, Is.Zero);
            Assert.That(squad.IsDefeated, Is.True);
            Assert.That(defeatedEvents, Is.EqualTo(1));
            Assert.That(bootstrap.IsDefeated, Is.True);
            Assert.That(world.ScrollingEnabled, Is.False);
            Assert.That(director.SimulationEnabled, Is.False);
            Assert.That(weapons.ProjectilePool.SimulationEnabled, Is.False);
            Assert.That(enemy.transform.position, Is.EqualTo(stoppedEnemyPosition));
            Assert.That(world.DistanceTravelled, Is.EqualTo(stoppedDistance));
            Assert.That(squad.ApplyCasualties(1), Is.Zero);
            Assert.That(defeatedEvents, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator FiftySoldiersAndOneHundredEnemies_RemainBoundedAndDistributed()
        {
            yield return LoadGameplayScene();
            ContinuousEnemyDirector director =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            director.ResetCombat();
            director.SetSpawnSchedulingEnabled(false);
            EnemyConfig normal = director.LevelConfig.NormalEnemy;
            Assert.That(squad.AddUnits(30), Is.EqualTo(30));
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(50));

            float spawnZ = squad.CombatContactZ + 20f;
            for (int index = 0; index < 100; index++)
            {
                int row = index / 10;
                int column = index % 10;
                float x = (column - 4.5f) * 0.75f;
                director.EnemyPool.Spawn(
                    normal,
                    new Vector3(x, 0f, spawnZ + (row * 0.8f)),
                    -1);
            }

            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.EqualTo(100));
            Assert.That(
                director.EnemyPool.Spawn(normal, new Vector3(0f, 0f, spawnZ), -1),
                Is.Null);
            yield return new WaitForSeconds(0.1f);
            Assert.That(weapons.ShooterCount, Is.EqualTo(50));
            Assert.That(weapons.AssignedTargetCount, Is.GreaterThan(5));
            var activeWeapons = new List<WeaponController>();
            weapons.CopyActiveWeaponsTo(activeWeapons);
            var targets = new HashSet<object>();
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                if (activeWeapons[index].CurrentTarget != null)
                {
                    targets.Add(activeWeapons[index].CurrentTarget);
                }
            }

            Assert.That(targets.Count, Is.GreaterThanOrEqualTo(40));
            yield return new WaitForSeconds(0.5f);
            Assert.That(director.EnemyPool.TotalCreated, Is.LessThanOrEqualTo(100));
            Assert.That(weapons.TotalProjectileCapacity, Is.LessThanOrEqualTo(512));
            Assert.That(
                weapons.ActiveProjectileCount,
                Is.LessThanOrEqualTo(weapons.TotalProjectileCapacity));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ContinuousGroups_BossTriggerAndResetRestoreRun()
        {
            yield return LoadGameplayScene();
            GameplayBootstrap bootstrap =
                Object.FindFirstObjectByType<GameplayBootstrap>();
            ContinuousEnemyDirector director =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            WorldScroller world =
                Object.FindFirstObjectByType<WorldScroller>();
            director.ResetCombat();
            world.ResetScroll();
            weapons.SetCombatEnabled(false);
            int groupEvents = 0;
            int bossEvents = 0;
            director.GroupSpawned += (_, count) => groupEvents++;
            director.BossSpawnRequested += () => bossEvents++;

            director.Tick();
            Assert.That(groupEvents, Is.EqualTo(1));
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.GreaterThan(0));
            int firstGroupCount = director.EnemyPool.ActiveEnemyCount;
            world.Tick(1f);
            director.Tick();
            Assert.That(groupEvents, Is.GreaterThan(1));
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.GreaterThan(firstGroupCount));

            world.Tick(70f);
            director.Tick();
            director.Tick();
            Assert.That(director.BossTriggerReached, Is.True);
            Assert.That(bossEvents, Is.EqualTo(1));
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.GreaterThan(0));

            bootstrap.ResetPrototypeRun();
            Assert.That(bootstrap.IsDefeated, Is.False);
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(20));
            Assert.That(director.ScheduledGroupCount, Is.Zero);
            Assert.That(director.BossTriggerReached, Is.False);
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.Zero);
            Assert.That(world.DistanceTravelled, Is.Zero);
            Assert.That(world.ScrollingEnabled, Is.True);
            Assert.That(weapons.ProjectilePool.SimulationEnabled, Is.True);
        }

        [UnityTest]
        public IEnumerator DensitySections_AdvanceWithoutWaitingForEnemyClear()
        {
            yield return LoadGameplayScene();
            ContinuousEnemyDirector director =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            WorldScroller world =
                Object.FindFirstObjectByType<WorldScroller>();
            director.ResetCombat();
            world.ResetScroll();
            weapons.SetCombatEnabled(false);

            world.Tick(11f);
            for (int index = 0; index < 8; index++)
            {
                director.Tick();
            }

            Assert.That(director.CurrentSectionNumber, Is.EqualTo(2));
            int groupsAtMediumDensity = director.ScheduledGroupCount;
            world.Tick(14f);
            for (int index = 0; index < 12; index++)
            {
                director.Tick();
            }

            Assert.That(director.CurrentSectionNumber, Is.GreaterThanOrEqualTo(3));
            Assert.That(director.ScheduledGroupCount, Is.GreaterThan(groupsAtMediumDensity));
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.GreaterThan(0));
            Assert.That(
                director.EnemyPool.ActiveEnemyCount,
                Is.LessThanOrEqualTo(director.LevelConfig.ActiveEnemyLimit));
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator LoadGameplayScene()
        {
            yield return SceneManager.LoadSceneAsync(
                "02_Gameplay",
                LoadSceneMode.Single);
            yield return new WaitForSeconds(0.2f);
        }
    }
}
