using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Enemy", fileName = "EnemyConfig")]
    public sealed class EnemyConfig : GameConfig
    {
        [SerializeField, Min(1f)] private float maximumHealth = 25f;
        [SerializeField, Min(0f)] private float damage = 5f;
        [SerializeField, Min(0f)] private float movementSpeed = 2f;

        public float MaximumHealth => maximumHealth;

        public float Damage => damage;

        public float MovementSpeed => movementSpeed;
    }
}
