using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Player", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : GameConfig
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;
        [SerializeField, Min(0f)] private float lateralMovementSpeed = 8f;

        public float MaximumHealth => maximumHealth;

        public float LateralMovementSpeed => lateralMovementSpeed;
    }
}
