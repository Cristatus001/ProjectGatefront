using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Weapon", fileName = "WeaponConfig")]
    public sealed class WeaponConfig : GameConfig
    {
        [SerializeField] private string displayName = "Weapon";
        [SerializeField, Min(0f)] private float damage = 10f;
        [SerializeField, Min(0.01f)] private float shotsPerSecond = 1f;
        [SerializeField, Min(0f)] private float range = 10f;

        public string DisplayName => displayName;

        public float Damage => damage;

        public float ShotsPerSecond => shotsPerSecond;

        public float Range => range;
    }
}
