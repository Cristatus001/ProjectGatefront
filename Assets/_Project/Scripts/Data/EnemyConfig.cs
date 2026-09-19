using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Enemy", fileName = "EnemyConfig")]
    public sealed class EnemyConfig : GameConfig
    {
        [SerializeField] private string enemyId = "rusher_weak";
        [SerializeField] private string displayName = "Weak Rusher";
        [SerializeField, Min(1f)] private float maximumHealth = 64f;
        [SerializeField, Min(0f)] private float approachSpeed = 2f;
        [SerializeField, Min(1)] private int casualtiesOnContact = 1;
        [SerializeField, Min(0.05f)] private float targetRadius = 0.55f;
        [SerializeField, Min(0)] private int rewardValue;
        [SerializeField, Min(0.1f)] private float visualScale = 1f;

        public string EnemyId => enemyId;

        public string DisplayName => displayName;

        public float MaximumHealth => maximumHealth;

        public float ApproachSpeed => approachSpeed;

        public int CasualtiesOnContact => casualtiesOnContact;

        public float TargetRadius => targetRadius;

        public int RewardValue => rewardValue;

        public float VisualScale => visualScale;
    }
}
