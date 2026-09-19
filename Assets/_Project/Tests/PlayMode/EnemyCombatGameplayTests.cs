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
        public IEnumerator AlignedContact_AppliesCasualtiesAndRecompacts()
        {
            yield return LoadGameplayScene();
            EnemyWaveDirector director =
                Object.FindFirstObjectByType<EnemyWaveDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            director.ResetCombat();
            director.SetWaveSchedulingEnabled(false);
            weapons.SetCombatEnabled(false);
            EnemyConfig medium = director.LevelConfig.EnemyWaves[3].EnemyConfig;
            int startingCount = squad.CurrentUnitCount;
            int startingRevision = squad.FormationRevision;
            int reportedCasualties = 0;
            director.EnemyPool.EnemyReachedSquad +=
                (_, casualties) => reportedCasualties += casualties;

            director.EnemyPool.Spawn(
                medium,
                new Vector3(
                    squad.transform.position.x,
                    0f,
                    squad.CombatContactZ + medium.TargetRadius + 0.02f),
                -1);
            yield return new WaitForSeconds(0.1f);

            Assert.That(reportedCasualties, Is.EqualTo(2));
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(startingCount - 2));
            Assert.That(squad.ActiveUnitCount, Is.EqualTo(squad.CurrentUnitCount));
            Assert.That(squad.FormationRevision, Is.GreaterThan(startingRevision));
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator LateralDodge_AllowsEnemyToPassWithoutCasualty()
        {
            yield return LoadGameplayScene();
            EnemyWaveDirector director =
                Object.FindFirstObjectByType<EnemyWaveDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            PlayerController player =
                Object.FindFirstObjectByType<PlayerController>();
            director.ResetCombat();
            director.SetWaveSchedulingEnabled(false);
            weapons.SetCombatEnabled(false);
            EnemyConfig weak = director.LevelConfig.EnemyWaves[0].EnemyConfig;

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
            EnemyWaveDirector director =
                Object.FindFirstObjectByType<EnemyWaveDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            WorldScroller world =
                Object.FindFirstObjectByType<WorldScroller>();
            director.ResetCombat();
            director.SetWaveSchedulingEnabled(false);
            EnemyConfig weak = director.LevelConfig.EnemyWaves[0].EnemyConfig;
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
        public IEnumerator FiftySoldiersAndSixtyEnemies_RemainBoundedAndDistributed()
        {
            yield return LoadGameplayScene();
            EnemyWaveDirector director =
                Object.FindFirstObjectByType<EnemyWaveDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            director.ResetCombat();
            director.SetWaveSchedulingEnabled(false);
            EnemyConfig medium = director.LevelConfig.EnemyWaves[3].EnemyConfig;
            Assert.That(squad.AddUnits(30), Is.EqualTo(30));
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(50));

            float spawnZ = squad.CombatContactZ + 20f;
            for (int index = 0; index < 60; index++)
            {
                int row = index / 10;
                int column = index % 10;
                float x = (column - 4.5f) * 0.75f;
                director.EnemyPool.Spawn(
                    medium,
                    new Vector3(x, 0f, spawnZ + (row * 0.8f)),
                    -1);
            }

            yield return new WaitForSeconds(0.2f);
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.EqualTo(60));
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

            Assert.That(targets.Count, Is.GreaterThan(5));
            yield return new WaitForSeconds(0.5f);
            Assert.That(director.EnemyPool.TotalCreated, Is.LessThanOrEqualTo(100));
            Assert.That(weapons.TotalProjectileCapacity, Is.LessThanOrEqualTo(512));
            Assert.That(
                weapons.ActiveProjectileCount,
                Is.LessThanOrEqualTo(weapons.TotalProjectileCapacity));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AuthoredWave_CompletesAndPrototypeResetRestoresRun()
        {
            yield return LoadGameplayScene();
            GameplayBootstrap bootstrap =
                Object.FindFirstObjectByType<GameplayBootstrap>();
            EnemyWaveDirector director =
                Object.FindFirstObjectByType<EnemyWaveDirector>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad =
                Object.FindFirstObjectByType<SquadController>();
            WorldScroller world =
                Object.FindFirstObjectByType<WorldScroller>();
            director.ResetCombat();
            world.ResetScroll();
            weapons.SetCombatEnabled(false);
            int startedEvents = 0;
            int completedEvents = 0;
            director.WaveStarted += (waveNumber, wave) => startedEvents++;
            director.WaveCompleted += _ => completedEvents++;

            world.Tick(1f);
            yield return null;
            Assert.That(world.DistanceTravelled, Is.GreaterThanOrEqualTo(8f));
            Assert.That(director.StartedWaveCount, Is.EqualTo(1));
            Assert.That(startedEvents, Is.EqualTo(1));
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.EqualTo(5));

            var enemies = new List<EnemyController>();
            director.EnemyPool.CopyActiveEnemiesTo(enemies);
            for (int index = 0; index < enemies.Count; index++)
            {
                enemies[index].ApplyDamage(enemies[index].MaximumHealth + 1f);
            }

            yield return new WaitForSeconds(0.25f);
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.Zero);
            Assert.That(director.CompletedWaveCount, Is.EqualTo(1));
            Assert.That(completedEvents, Is.EqualTo(1));

            bootstrap.ResetPrototypeRun();
            Assert.That(bootstrap.IsDefeated, Is.False);
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(20));
            Assert.That(director.StartedWaveCount, Is.Zero);
            Assert.That(director.CompletedWaveCount, Is.Zero);
            Assert.That(director.EnemyPool.ActiveEnemyCount, Is.Zero);
            Assert.That(world.DistanceTravelled, Is.Zero);
            Assert.That(world.ScrollingEnabled, Is.True);
            Assert.That(weapons.ProjectilePool.SimulationEnabled, Is.True);
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
