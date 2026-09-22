using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Enemies;
using ProjectGatefront.Gameplay.Gates;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.Weapons;
using ProjectGatefront.Gameplay.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectGatefront.Tests.PlayMode
{
    public sealed class GateGameplayTests
    {
        [UnityTest]
        public IEnumerator ForegroundEnemy_ShieldsGateUntilKilled()
        {
            yield return LoadGameplay();
            GateDirector gates = Object.FindFirstObjectByType<GateDirector>();
            ContinuousEnemyDirector enemies =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            GameplayBootstrap bootstrap =
                Object.FindFirstObjectByType<GameplayBootstrap>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad = Object.FindFirstObjectByType<SquadController>();
            PrepareIsolatedGateRun(bootstrap, gates, enemies, weapons);
            GateController gate = SpawnGateAhead(gates, squad, 12f);
            EnemyController enemy = enemies.EnemyPool.Spawn(
                enemies.LevelConfig.NormalEnemy,
                new Vector3(gate.WorldX, 0f, gate.WorldZ - 4f),
                -1);
            int startingValue = gate.CurrentValue;
            Vector3 origin = gate.AimPosition - (Vector3.forward * 8f);

            Projectile first = FireDirectlyAtGate(weapons, gate, origin);
            first.Tick(0.35f);

            Assert.That(enemy.State, Is.EqualTo(EnemyRuntimeState.Dying));
            Assert.That(enemy.CurrentHealth, Is.Zero);
            Assert.That(gate.CurrentValue, Is.EqualTo(startingValue));
            Assert.That(first.IsLaunched, Is.False);

            Projectile second = FireDirectlyAtGate(weapons, gate, origin);
            second.Tick(0.35f);

            Assert.That(gate.CurrentValue, Is.EqualTo(startingValue + 1));
            Assert.That(second.IsLaunched, Is.False);
        }

        [UnityTest]
        public IEnumerator MovingEnemyEnteringProjectilePath_ShieldsGate()
        {
            yield return LoadGameplay();
            GateDirector gates = Object.FindFirstObjectByType<GateDirector>();
            ContinuousEnemyDirector enemies =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            GameplayBootstrap bootstrap =
                Object.FindFirstObjectByType<GameplayBootstrap>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad = Object.FindFirstObjectByType<SquadController>();
            PrepareIsolatedGateRun(bootstrap, gates, enemies, weapons);
            GateController gate = SpawnGateAhead(gates, squad, 12f);
            EnemyController enemy = enemies.EnemyPool.Spawn(
                enemies.LevelConfig.NormalEnemy,
                new Vector3(gate.WorldX + 2f, 0f, gate.WorldZ - 4f),
                -1);
            int startingValue = gate.CurrentValue;
            Vector3 origin = gate.AimPosition - (Vector3.forward * 8f);
            Projectile projectile = FireDirectlyAtGate(weapons, gate, origin);
            projectile.Tick(0.05f);
            Vector3 enteredPath = enemy.transform.position;
            enteredPath.x = gate.WorldX;
            enemy.transform.position = enteredPath;

            projectile.Tick(0.2f);

            Assert.That(enemy.State, Is.EqualTo(EnemyRuntimeState.Dying));
            Assert.That(gate.CurrentValue, Is.EqualTo(startingValue));
            Assert.That(projectile.IsLaunched, Is.False);
        }

        [UnityTest]
        public IEnumerator EnemyBesideProjectilePath_DoesNotShieldGate()
        {
            yield return LoadGameplay();
            GateDirector gates = Object.FindFirstObjectByType<GateDirector>();
            ContinuousEnemyDirector enemies =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            GameplayBootstrap bootstrap =
                Object.FindFirstObjectByType<GameplayBootstrap>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad = Object.FindFirstObjectByType<SquadController>();
            PrepareIsolatedGateRun(bootstrap, gates, enemies, weapons);
            GateController gate = SpawnGateAhead(gates, squad, 12f);
            EnemyController enemy = enemies.EnemyPool.Spawn(
                enemies.LevelConfig.NormalEnemy,
                new Vector3(gate.WorldX + 2f, 0f, gate.WorldZ - 4f),
                -1);
            int startingValue = gate.CurrentValue;
            Vector3 origin = gate.AimPosition - (Vector3.forward * 8f);
            Projectile projectile = FireDirectlyAtGate(weapons, gate, origin);

            projectile.Tick(0.35f);

            Assert.That(enemy.State, Is.EqualTo(EnemyRuntimeState.Approaching));
            Assert.That(gate.CurrentValue, Is.EqualTo(startingValue + 1));
            Assert.That(projectile.IsLaunched, Is.False);
        }

        [UnityTest]
        public IEnumerator MultipleShooters_BlockOnlyRelevantGateCorridors()
        {
            yield return LoadGameplay();
            GateDirector gates = Object.FindFirstObjectByType<GateDirector>();
            ContinuousEnemyDirector enemies =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            GameplayBootstrap bootstrap =
                Object.FindFirstObjectByType<GameplayBootstrap>();
            SquadWeaponSystem weapons =
                Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad = Object.FindFirstObjectByType<SquadController>();
            PrepareIsolatedGateRun(bootstrap, gates, enemies, weapons);
            GateController gate = SpawnGateAhead(gates, squad, 12f);
            var activeWeapons = new List<WeaponController>();
            weapons.CopyActiveWeaponsTo(activeWeapons);
            float leftmostX = float.PositiveInfinity;
            WeaponController leftmostWeapon = null;
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                float shooterX = activeWeapons[index].FirePoint.position.x;
                if (shooterX < leftmostX)
                {
                    leftmostX = shooterX;
                    leftmostWeapon = activeWeapons[index];
                }
            }

            float enemyZ = gate.WorldZ - 4f;
            Vector3 shooterOrigin = leftmostWeapon.FirePoint.position;
            float corridorFraction = (enemyZ - shooterOrigin.z)
                / (gate.AimPosition.z - shooterOrigin.z);
            float blockerX = Mathf.Lerp(
                shooterOrigin.x,
                gate.WorldX,
                corridorFraction);
            EnemyController blocker = enemies.EnemyPool.Spawn(
                enemies.LevelConfig.NormalEnemy,
                new Vector3(blockerX, 0f, enemyZ),
                -1);
            weapons.SetCombatEnabled(true);
            yield return null;

            int enemyTargets = 0;
            int gateTargets = 0;
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                ITargetable target = activeWeapons[index].CurrentTarget;
                enemyTargets += ReferenceEquals(target, blocker) ? 1 : 0;
                gateTargets += ReferenceEquals(target, gate) ? 1 : 0;
            }

            Assert.That(enemyTargets, Is.GreaterThan(0));
            Assert.That(gateTargets, Is.GreaterThan(0));
        }

        [UnityTest]
        public IEnumerator ProjectileChargesGateOnce_ThenCrossingAppliesOnce()
        {
            yield return LoadGameplay();
            GateDirector gates = Object.FindFirstObjectByType<GateDirector>();
            GameplayBootstrap bootstrap = Object.FindFirstObjectByType<GameplayBootstrap>();
            SquadWeaponSystem weapons = Object.FindFirstObjectByType<SquadWeaponSystem>();
            SquadController squad = Object.FindFirstObjectByType<SquadController>();
            WorldScroller world = Object.FindFirstObjectByType<WorldScroller>();
            bootstrap.SetMovementEnabled(false);
            world.enabled = false;
            gates.enabled = false;
            gates.SetTimelineEnabled(false);
            gates.SpawnEncounterNow(0);
            var active = new List<GateController>();
            gates.CopyActiveGatesTo(active);
            Assert.That(active.Count, Is.EqualTo(1));
            GateController gate = active[0];
            Assert.That(gate.StartingValue, Is.EqualTo(-10));
            Assert.That(gate.HalfWidth, Is.GreaterThan(3f));
            int before = squad.CurrentUnitCount;
            int events = 0;
            GateResolution resolution = default;
            gates.GateResolved += result => { resolution = result; events++; };

            for (int index = 0; index < 15; index++)
            {
                Vector3 origin = gate.AimPosition - Vector3.forward *
                    (gate.TargetRadius + 1f);
                Projectile projectile = weapons.ProjectilePool.Acquire(origin,
                    Vector3.forward, weapons.CurrentRuntimeStats);
                projectile.Tick(0.25f);
                Assert.That(projectile.IsLaunched, Is.False);
            }

            Assert.That(gate.CurrentValue, Is.EqualTo(5));
            Assert.That(weapons.ActiveProjectileCount, Is.Zero);
            world.SetScrollingEnabled(true);
            world.Tick(100f);
            gates.SetSimulationEnabled(true);
            gates.Tick();
            Assert.That(events, Is.EqualTo(1));
            Assert.That(resolution.FinalValue, Is.EqualTo(5));
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(before + 5));
            gates.Tick();
            Assert.That(events, Is.EqualTo(1));
            Assert.That(gate.ApplyProjectileHit(), Is.False);
            Assert.That(gate.CurrentValue, Is.EqualTo(5));
        }

        [UnityTest]
        public IEnumerator PairedRightSelection_DividesAndDoesNotShootDivide()
        {
            yield return LoadGameplay();
            GateDirector gates = Object.FindFirstObjectByType<GateDirector>();
            GameplayBootstrap bootstrap = Object.FindFirstObjectByType<GameplayBootstrap>();
            SquadController squad = Object.FindFirstObjectByType<SquadController>();
            WorldScroller world = Object.FindFirstObjectByType<WorldScroller>();
            bootstrap.SetMovementEnabled(false);
            world.enabled = false;
            gates.enabled = false;
            gates.SetTimelineEnabled(false);
            gates.SpawnEncounterNow(2);
            var active = new List<GateController>();
            gates.CopyActiveGatesTo(active);
            Assert.That(active.Count, Is.EqualTo(2));
            GateController divide = active.Find(gate => gate.Kind == GateKind.Divide);
            Assert.That(divide, Is.Not.Null);
            Assert.That(divide.IsTargetable, Is.False);
            Assert.That(divide.ApplyProjectileHit(), Is.False);
            Vector3 position = squad.transform.position;
            position.x = squad.MaximumRootX;
            squad.transform.position = position;
            int before = squad.CurrentUnitCount;
            GateResolution resolution = default;
            gates.GateResolved += result => resolution = result;
            world.SetScrollingEnabled(true);
            world.Tick(100f);
            gates.SetSimulationEnabled(true);
            gates.Tick();
            Assert.That(resolution.SelectedGateType, Is.EqualTo(GateKind.Divide));
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(before / 2));
            Assert.That(gates.ActiveGateCount, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TimelineAndReset_PreserveContinuousCombat()
        {
            yield return LoadGameplay();
            GateDirector gates = Object.FindFirstObjectByType<GateDirector>();
            ContinuousEnemyDirector enemies =
                Object.FindFirstObjectByType<ContinuousEnemyDirector>();
            WorldScroller world = Object.FindFirstObjectByType<WorldScroller>();
            GameplayBootstrap bootstrap = Object.FindFirstObjectByType<GameplayBootstrap>();
            Assert.That(enemies.IsInitialized, Is.True);
            Assert.That(enemies.LevelConfig.BossDistance, Is.EqualTo(520f));
            Assert.That(enemies.LevelConfig.GateEncounters.Count, Is.EqualTo(5));
            bootstrap.SetMovementEnabled(false);
            world.enabled = false;
            gates.enabled = false;
            world.SetScrollingEnabled(true);
            gates.SetSimulationEnabled(true);
            world.Tick(6f);
            gates.Tick();
            Assert.That(gates.ActiveGateCount, Is.EqualTo(1));
            Assert.That(enemies.SpawnIntervalMultiplier, Is.GreaterThan(1f));
            enemies.SetSimulationEnabled(true);
            enemies.Tick();
            Assert.That(enemies.ScheduledGroupCount, Is.GreaterThan(0));
            world.Tick(7f);
            gates.Tick();
            bootstrap.ResetPrototypeRun();
            Assert.That(gates.ActiveGateCount, Is.Zero);
            Assert.That(gates.ResolvedCount, Is.Zero);
            Assert.That(gates.NextEncounterIndex, Is.Zero);
            Assert.That(enemies.SpawnIntervalMultiplier, Is.EqualTo(1f));
        }

        private static IEnumerator LoadGameplay()
        {
            yield return SceneManager.LoadSceneAsync("02_Gameplay", LoadSceneMode.Single);
            yield return new WaitForSeconds(0.2f);
        }

        private static void PrepareIsolatedGateRun(
            GameplayBootstrap bootstrap,
            GateDirector gates,
            ContinuousEnemyDirector enemies,
            SquadWeaponSystem weapons)
        {
            bootstrap.SetMovementEnabled(false);
            enemies.ResetCombat();
            enemies.SetSpawnSchedulingEnabled(false);
            weapons.SetCombatEnabled(false);
            gates.enabled = false;
            gates.SetTimelineEnabled(false);
        }

        private static GateController SpawnGateAhead(
            GateDirector gates,
            SquadController squad,
            float distance)
        {
            gates.SpawnEncounterNow(0);
            var active = new List<GateController>();
            gates.CopyActiveGatesTo(active);
            Assert.That(active.Count, Is.EqualTo(1));
            GateController gate = active[0];
            Vector3 position = gate.transform.position;
            position.z = squad.CombatContactZ + distance;
            gate.transform.position = position;
            return gate;
        }

        private static Projectile FireDirectlyAtGate(
            SquadWeaponSystem weapons,
            GateController gate,
            Vector3 origin)
        {
            Vector3 direction = (gate.AimPosition - origin).normalized;
            return weapons.ProjectilePool.Acquire(
                origin,
                direction,
                weapons.CurrentRuntimeStats);
        }
    }
}
