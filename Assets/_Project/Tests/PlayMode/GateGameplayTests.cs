using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay;
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
    }
}
