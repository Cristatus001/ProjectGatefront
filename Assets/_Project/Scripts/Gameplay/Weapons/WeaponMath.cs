using UnityEngine;

namespace ProjectGatefront.Gameplay.Weapons
{
    public static class WeaponMath
    {
        public static float CalculateFireInterval(float shotsPerSecond)
        {
            return IsFinite(shotsPerSecond) && shotsPerSecond > 0f
                ? 1f / shotsPerSecond
                : float.PositiveInfinity;
        }

        public static float CalculateEffectiveProjectileLifetime(
            float range,
            float projectileSpeed,
            float configuredLifetime)
        {
            float safeRange = IsFinite(range) ? Mathf.Max(0f, range) : 0f;
            float safeSpeed = IsFinite(projectileSpeed)
                ? Mathf.Max(0f, projectileSpeed)
                : 0f;
            float safeLifetime = IsFinite(configuredLifetime)
                ? Mathf.Max(0f, configuredLifetime)
                : 0f;
            if (safeSpeed <= 0f || safeRange <= 0f || safeLifetime <= 0f)
            {
                return 0f;
            }

            return Mathf.Min(safeLifetime, safeRange / safeSpeed);
        }

        public static int CalculateProjectilePrewarmCapacity(
            WeaponRuntimeStats stats,
            int shooterCount)
        {
            float estimatedConcurrentProjectiles = Mathf.Max(1, shooterCount)
                * stats.FireRate
                * stats.ProjectilesPerShot
                * stats.ProjectileLifetime;
            return Mathf.Clamp(
                Mathf.CeilToInt(estimatedConcurrentProjectiles * 1.25f),
                16,
                512);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
