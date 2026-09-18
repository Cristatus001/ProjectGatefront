using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Gameplay;
using ProjectGatefront.Gameplay.Combat;
using ProjectGatefront.Gameplay.Player;
using ProjectGatefront.Gameplay.Squad;
using ProjectGatefront.Gameplay.Weapons;
using ProjectGatefront.Gameplay.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ProjectGatefront.Tests.PlayMode
{
    public sealed class AutoShootingGameplayTests
    {
        private const float Tolerance = 0.01f;

        [UnityTest]
        public IEnumerator GameplayScene_AutoShootsRetargetsPausesAndScales()
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
            WorldScroller world =
                Object.FindFirstObjectByType<WorldScroller>();

            Assert.That(bootstrap, Is.Not.Null);
            Assert.That(player, Is.Not.Null);
            Assert.That(squad, Is.Not.Null);
            Assert.That(weapons, Is.Not.Null);
            Assert.That(world, Is.Not.Null);
            Assert.That(weapons.IsInitialized, Is.True);
            Assert.That(weapons.ShooterCount, Is.EqualTo(20));
            Assert.That(weapons.ShooterCount, Is.EqualTo(squad.CurrentUnitCount));
            Assert.That(weapons.ActiveTargetCount, Is.GreaterThanOrEqualTo(3));

            var activeWeapons = new List<WeaponController>();
            weapons.CopyActiveWeaponsTo(activeWeapons);
            Assert.That(activeWeapons.Count, Is.EqualTo(20));
            for (int index = 0; index < activeWeapons.Count; index++)
            {
                Assert.That(activeWeapons[index].IsInitialized, Is.True);
                Assert.That(activeWeapons[index].FirePoint, Is.Not.Null);
                Assert.That(
                    activeWeapons[index].transform.Find(
                        "WeaponMount/EquippedWeaponVisual"),
                    Is.Not.Null);
            }

            int initialShots = weapons.TotalShotsFired;
            yield return new WaitForSeconds(0.65f);
            Assert.That(weapons.TotalShotsFired, Is.GreaterThan(initialShots));
            Assert.That(weapons.TotalProjectileCapacity, Is.InRange(16, 512));

            var targets = new List<CombatTargetDummy>();
            weapons.CopyTestTargetsTo(targets);
            bool anyDamagedOrDefeated = false;
            for (int index = 0; index < targets.Count; index++)
            {
                CombatTargetDummy target = targets[index];
                anyDamagedOrDefeated |= !target.gameObject.activeSelf
                    || target.CurrentHealth < target.MaximumHealth;
            }

            Assert.That(anyDamagedOrDefeated, Is.True);

            weapons.ResetCombat();
            yield return new WaitForSeconds(0.12f);
            ITargetable selected = weapons.CurrentTarget;
            Assert.That(selected, Is.Not.Null);
            var selectedDummy = selected as CombatTargetDummy;
            Assert.That(selectedDummy, Is.Not.Null);
            selectedDummy.ApplyDamage(selectedDummy.MaximumHealth + 1f);
            yield return new WaitForSeconds(0.15f);
            Assert.That(weapons.CurrentTarget, Is.Not.SameAs(selected));
            Assert.That(weapons.TargetRegistry.ActiveTargetCount, Is.EqualTo(5));

            int shotsBeforePause = weapons.TotalShotsFired;
            Projectile flyingProjectile = FindFlyingProjectile();
            Vector3 projectilePosition = flyingProjectile != null
                ? flyingProjectile.transform.position
                : Vector3.zero;
            bootstrap.SetMovementEnabled(false);
            yield return new WaitForSeconds(0.3f);
            Assert.That(weapons.TotalShotsFired, Is.EqualTo(shotsBeforePause));
            if (flyingProjectile != null && flyingProjectile.IsLaunched)
            {
                Assert.That(
                    Vector3.Distance(
                        flyingProjectile.transform.position,
                        projectilePosition),
                    Is.LessThanOrEqualTo(Tolerance));
            }

            bootstrap.SetMovementEnabled(true);
            weapons.ResetCombat();
            yield return new WaitForSeconds(0.15f);
            var activeUnits = new List<SquadUnit>();
            squad.CopyActiveUnitsTo(activeUnits);
            Vector3 unitWorldPosition = activeUnits[0].transform.position;
            Vector3 unitLocalPosition = activeUnits[0].transform.localPosition;
            Vector3 playerPosition = player.transform.position;
            playerPosition.x += 0.5f;
            player.transform.position = playerPosition;
            Assert.That(
                activeUnits[0].transform.position.x - unitWorldPosition.x,
                Is.EqualTo(0.5f).Within(Tolerance));
            Assert.That(
                Vector3.Distance(
                    activeUnits[0].transform.localPosition,
                    unitLocalPosition),
                Is.LessThanOrEqualTo(Tolerance));
            int shotsBeforeMovingFire = weapons.TotalShotsFired;
            yield return new WaitForSeconds(0.35f);
            Assert.That(
                weapons.TotalShotsFired,
                Is.GreaterThan(shotsBeforeMovingFire));

            for (int index = 0; index < targets.Count; index++)
            {
                if (targets[index].IsTargetable)
                {
                    targets[index].ApplyDamage(targets[index].MaximumHealth + 1f);
                }
            }

            yield return new WaitForSeconds(0.15f);
            int shotsWithoutTargets = weapons.TotalShotsFired;
            yield return new WaitForSeconds(0.35f);
            Assert.That(weapons.CurrentTarget, Is.Null);
            Assert.That(weapons.TotalShotsFired, Is.EqualTo(shotsWithoutTargets));

            weapons.ResetCombat();
            yield return new WaitForSeconds(0.2f);
            Assert.That(weapons.ActiveTargetCount, Is.EqualTo(targets.Count));
            Assert.That(weapons.CurrentTarget, Is.Not.Null);

            float distanceBefore = world.DistanceTravelled;
            Vector3 cameraBefore = Camera.main.transform.position;
            yield return null;
            Assert.That(world.DistanceTravelled, Is.GreaterThan(distanceBefore));
            Assert.That(
                Camera.main.transform.position.x,
                Is.EqualTo(cameraBefore.x).Within(Tolerance));
            Assert.That(
                Camera.main.transform.position.z,
                Is.EqualTo(cameraBefore.z).Within(Tolerance));

            Assert.That(squad.AddUnits(30), Is.EqualTo(30));
            Assert.That(squad.CurrentUnitCount, Is.EqualTo(50));
            Assert.That(weapons.ShooterCount, Is.EqualTo(50));
            yield return new WaitForSeconds(0.5f);
            Assert.That(weapons.TotalProjectileCapacity, Is.LessThanOrEqualTo(512));
            Assert.That(
                weapons.ActiveProjectileCount,
                Is.LessThanOrEqualTo(weapons.TotalProjectileCapacity));
        }

        private static Projectile FindFlyingProjectile()
        {
            Projectile[] projectiles = Object.FindObjectsByType<Projectile>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < projectiles.Length; index++)
            {
                if (projectiles[index].IsLaunched)
                {
                    return projectiles[index];
                }
            }

            return null;
        }
    }
}
