using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.Weapons;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Tests
{
    public sealed class SquadWeaponSystemTests
    {
        [Test]
        public void CriticalEnemyWindow_IsAheadOfGatePriority()
        {
            Assert.That(SquadWeaponSystem.IsCriticallyClose(5.99f, 0f), Is.True);
            Assert.That(SquadWeaponSystem.IsCriticallyClose(6.01f, 0f), Is.False);
        }

        [Test]
        public void EveryActiveUnit_IsAShooterAtOneFiveTwentyAndFifty()
        {
            var root = new GameObject("SquadWeaponSystemTest");
            var formationRoot = new GameObject("FormationRoot");
            var unitPrefabObject = new GameObject("UnitPrefab");
            var projectilePrefabObject = new GameObject("ProjectilePrefab");
            SquadConfig squadConfig = ScriptableObject.CreateInstance<SquadConfig>();
            PlayerConfig playerConfig = ScriptableObject.CreateInstance<PlayerConfig>();
            GameBalanceConfig balanceConfig =
                ScriptableObject.CreateInstance<GameBalanceConfig>();
            WeaponConfig weaponConfig = ScriptableObject.CreateInstance<WeaponConfig>();
            try
            {
                formationRoot.transform.SetParent(root.transform, false);
                SquadUnit unitPrefab = unitPrefabObject.AddComponent<SquadUnit>();
                Transform mount = new GameObject("WeaponMount").transform;
                mount.SetParent(unitPrefabObject.transform, false);
                Transform firePoint = new GameObject("FirePoint").transform;
                firePoint.SetParent(mount, false);
                WeaponController prefabWeapon =
                    unitPrefabObject.AddComponent<WeaponController>();
                prefabWeapon.Configure(mount, firePoint, null);

                SquadController squad = root.AddComponent<SquadController>();
                squad.Initialize(
                    squadConfig,
                    unitPrefab,
                    formationRoot.transform,
                    playerConfig);
                TargetRegistry registry = root.AddComponent<TargetRegistry>();
                WorldScroller world = root.AddComponent<WorldScroller>();
                world.Configure(balanceConfig, null, -28f);
                world.Initialize();
                ProjectilePool pool = root.AddComponent<ProjectilePool>();
                pool.Configure(
                    projectilePrefabObject.AddComponent<Projectile>(),
                    root.transform);
                SquadWeaponSystem system =
                    root.AddComponent<SquadWeaponSystem>();
                system.Configure(
                    weaponConfig,
                    squad,
                    registry,
                    pool,
                    world,
                    null);
                system.Initialize();

                Assert.That(system.ShooterCount, Is.EqualTo(5));
                Assert.That(system.ShooterCount, Is.EqualTo(squad.CurrentUnitCount));
                Assert.That(squad.RemoveUnits(4), Is.EqualTo(4));
                Assert.That(system.ShooterCount, Is.EqualTo(1));
                Assert.That(system.ShooterCount, Is.EqualTo(squad.CurrentUnitCount));
                Assert.That(squad.AddUnits(4), Is.EqualTo(4));
                Assert.That(system.ShooterCount, Is.EqualTo(5));
                Assert.That(system.ShooterCount, Is.EqualTo(squad.CurrentUnitCount));
                Assert.That(squad.AddUnits(15), Is.EqualTo(15));
                Assert.That(system.ShooterCount, Is.EqualTo(20));
                Assert.That(system.ShooterCount, Is.EqualTo(squad.CurrentUnitCount));
                Assert.That(squad.AddUnits(30), Is.EqualTo(30));
                Assert.That(system.ShooterCount, Is.EqualTo(50));
                Assert.That(system.ShooterCount, Is.EqualTo(squad.CurrentUnitCount));
            }
            finally
            {
                Object.DestroyImmediate(squadConfig);
                Object.DestroyImmediate(playerConfig);
                Object.DestroyImmediate(balanceConfig);
                Object.DestroyImmediate(weaponConfig);
                Object.DestroyImmediate(projectilePrefabObject);
                Object.DestroyImmediate(unitPrefabObject);
                Object.DestroyImmediate(root);
            }
        }
    }
}
