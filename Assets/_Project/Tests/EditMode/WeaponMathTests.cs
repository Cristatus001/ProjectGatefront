using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Weapons;
using UnityEngine;

namespace ProjectGatefront.Tests
{
    public sealed class WeaponMathTests
    {
        [Test]
        public void FireInterval_UsesShotsPerSecondAndRejectsInvalidValues()
        {
            Assert.That(WeaponMath.CalculateFireInterval(4f), Is.EqualTo(0.25f));
            Assert.That(
                float.IsPositiveInfinity(WeaponMath.CalculateFireInterval(0f)),
                Is.True);
            Assert.That(
                float.IsPositiveInfinity(WeaponMath.CalculateFireInterval(-1f)),
                Is.True);
            Assert.That(
                float.IsPositiveInfinity(
                    WeaponMath.CalculateFireInterval(float.NaN)),
                Is.True);
        }

        [Test]
        public void EffectiveLifetime_IsLimitedByRangeAndConfiguredLifetime()
        {
            Assert.That(
                WeaponMath.CalculateEffectiveProjectileLifetime(28f, 28f, 2f),
                Is.EqualTo(1f));
            Assert.That(
                WeaponMath.CalculateEffectiveProjectileLifetime(100f, 20f, 2f),
                Is.EqualTo(2f));
            Assert.That(
                WeaponMath.CalculateEffectiveProjectileLifetime(10f, 0f, 2f),
                Is.Zero);
        }

        [Test]
        public void RuntimeModifiers_DoNotMutateSourceConfig()
        {
            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            try
            {
                float configuredDamage = config.Damage;
                float configuredFireRate = config.ShotsPerSecond;
                var modifiers = new WeaponStatModifiers(2f, 0.5f, 2, 1);
                WeaponRuntimeStats stats = WeaponRuntimeStats.FromConfig(
                    config,
                    modifiers);

                Assert.That(stats.Damage, Is.EqualTo(configuredDamage * 2f));
                Assert.That(
                    stats.FireRate,
                    Is.EqualTo(configuredFireRate * 0.5f));
                Assert.That(stats.ProjectilesPerShot, Is.EqualTo(3));
                Assert.That(stats.PenetrationCount, Is.EqualTo(1));
                Assert.That(config.Damage, Is.EqualTo(configuredDamage));
                Assert.That(config.ShotsPerSecond, Is.EqualTo(configuredFireRate));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void ProjectilePrewarm_IsBoundedForLargeSquads()
        {
            WeaponConfig config = ScriptableObject.CreateInstance<WeaponConfig>();
            try
            {
                WeaponRuntimeStats stats = WeaponRuntimeStats.FromConfig(config);
                Assert.That(
                    WeaponMath.CalculateProjectilePrewarmCapacity(stats, 1),
                    Is.InRange(16, 512));
                Assert.That(
                    WeaponMath.CalculateProjectilePrewarmCapacity(stats, 50),
                    Is.InRange(16, 512));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
