using System;
using ProjectGatefront.Data;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Weapons
{
    public readonly struct WeaponRuntimeStats
    {
        private WeaponRuntimeStats(
            string weaponId,
            string displayName,
            WeaponTier tier,
            float damage,
            float fireRate,
            float range,
            float projectileSpeed,
            float projectileLifetime,
            float projectileRadius,
            float spreadDegrees,
            int penetrationCount,
            int projectilesPerShot,
            float targetingHalfAngle)
        {
            WeaponId = weaponId;
            DisplayName = displayName;
            Tier = tier;
            Damage = damage;
            FireRate = fireRate;
            FireInterval = WeaponMath.CalculateFireInterval(fireRate);
            Range = range;
            ProjectileSpeed = projectileSpeed;
            ProjectileLifetime = projectileLifetime;
            ProjectileRadius = projectileRadius;
            SpreadDegrees = spreadDegrees;
            PenetrationCount = penetrationCount;
            ProjectilesPerShot = projectilesPerShot;
            TargetingHalfAngle = targetingHalfAngle;
            TargetingCosine = Mathf.Cos(targetingHalfAngle * Mathf.Deg2Rad);
        }

        public string WeaponId { get; }

        public string DisplayName { get; }

        public WeaponTier Tier { get; }

        public float Damage { get; }

        public float FireRate { get; }

        public float FireInterval { get; }

        public float Range { get; }

        public float ProjectileSpeed { get; }

        public float ProjectileLifetime { get; }

        public float ProjectileRadius { get; }

        public float SpreadDegrees { get; }

        public int PenetrationCount { get; }

        public int ProjectilesPerShot { get; }

        public float TargetingHalfAngle { get; }

        public float TargetingCosine { get; }

        public static WeaponRuntimeStats FromConfig(WeaponConfig config)
        {
            return FromConfig(config, WeaponStatModifiers.Identity);
        }

        public static WeaponRuntimeStats FromConfig(
            WeaponConfig config,
            WeaponStatModifiers modifiers)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            float safeDamage = WeaponMath.IsFinite(config.Damage)
                ? Mathf.Max(0f, config.Damage * modifiers.DamageMultiplier)
                : 0f;
            float safeFireRate = WeaponMath.IsFinite(config.ShotsPerSecond)
                ? Mathf.Max(0f, config.ShotsPerSecond * modifiers.FireRateMultiplier)
                : 0f;
            float safeRange = WeaponMath.IsFinite(config.Range)
                ? Mathf.Max(0f, config.Range)
                : 0f;
            float safeProjectileSpeed = WeaponMath.IsFinite(config.ProjectileSpeed)
                ? Mathf.Max(0f, config.ProjectileSpeed)
                : 0f;
            float lifetime = WeaponMath.CalculateEffectiveProjectileLifetime(
                safeRange,
                safeProjectileSpeed,
                config.ProjectileLifetime);

            return new WeaponRuntimeStats(
                config.WeaponId,
                config.DisplayName,
                config.WeaponTier,
                safeDamage,
                safeFireRate,
                safeRange,
                safeProjectileSpeed,
                lifetime,
                Mathf.Max(0.01f, config.ProjectileRadius),
                Mathf.Clamp(config.SpreadDegrees, 0f, 15f),
                Mathf.Max(0, config.PenetrationCount + modifiers.AdditionalPenetration),
                Mathf.Max(1, config.ProjectilesPerShot + modifiers.AdditionalProjectiles),
                Mathf.Clamp(config.TargetingHalfAngle, 1f, 89f));
        }
    }
}
