using UnityEngine;

namespace ProjectGatefront.Gameplay.Weapons
{
    public readonly struct WeaponStatModifiers
    {
        public WeaponStatModifiers(
            float damageMultiplier,
            float fireRateMultiplier,
            int additionalProjectiles,
            int additionalPenetration)
        {
            DamageMultiplier = Mathf.Max(0f, damageMultiplier);
            FireRateMultiplier = Mathf.Max(0f, fireRateMultiplier);
            AdditionalProjectiles = Mathf.Max(0, additionalProjectiles);
            AdditionalPenetration = Mathf.Max(0, additionalPenetration);
        }

        public static WeaponStatModifiers Identity => new WeaponStatModifiers(
            1f,
            1f,
            0,
            0);

        public float DamageMultiplier { get; }

        public float FireRateMultiplier { get; }

        public int AdditionalProjectiles { get; }

        public int AdditionalPenetration { get; }
    }
}
