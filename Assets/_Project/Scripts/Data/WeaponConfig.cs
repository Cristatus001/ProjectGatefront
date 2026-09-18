using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Weapon", fileName = "WeaponConfig")]
    public sealed class WeaponConfig : GameConfig
    {
        [SerializeField] private string weaponId = "prototype_rifle";
        [SerializeField] private string displayName = "Prototype Rifle";
        [SerializeField] private WeaponTier weaponTier = WeaponTier.Tier1;
        [SerializeField, Min(0f)] private float damage = 8f;
        [Tooltip("Shots fired per second.")]
        [SerializeField, Min(0f)] private float shotsPerSecond = 4f;
        [SerializeField, Min(0f)] private float range = 28f;
        [SerializeField, Min(0.1f)] private float projectileSpeed = 28f;
        [SerializeField, Min(0.01f)] private float projectileLifetime = 2f;
        [SerializeField, Min(0.01f)] private float projectileRadius = 0.08f;
        [SerializeField, Range(0f, 15f)] private float spreadDegrees = 0.75f;
        [SerializeField, Min(0)] private int penetrationCount;
        [SerializeField, Min(1)] private int projectilesPerShot = 1;
        [SerializeField, Range(1f, 89f)] private float targetingHalfAngle = 65f;
        [SerializeField] private GameObject weaponVisualPrefab;

        public string WeaponId => weaponId;

        public string DisplayName => displayName;

        public WeaponTier WeaponTier => weaponTier;

        public float Damage => damage;

        public float ShotsPerSecond => shotsPerSecond;

        public float Range => range;

        public float ProjectileSpeed => projectileSpeed;

        public float ProjectileLifetime => projectileLifetime;

        public float ProjectileRadius => projectileRadius;

        public float SpreadDegrees => spreadDegrees;

        public int PenetrationCount => penetrationCount;

        public int ProjectilesPerShot => projectilesPerShot;

        public float TargetingHalfAngle => targetingHalfAngle;

        public GameObject WeaponVisualPrefab => weaponVisualPrefab;
    }
}
