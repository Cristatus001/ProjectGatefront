using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Boss", fileName = "BossConfig")]
    public sealed class BossConfig : GameConfig
    {
        [SerializeField] private string displayName = "Boss";
        [SerializeField, Min(1f)] private float maximumHealth = 500f;
        [SerializeField, Min(1)] private int phaseCount = 1;

        public string DisplayName => displayName;

        public float MaximumHealth => maximumHealth;

        public int PhaseCount => phaseCount;
    }
}
