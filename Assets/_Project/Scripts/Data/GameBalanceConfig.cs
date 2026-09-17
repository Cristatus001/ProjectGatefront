using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Game Balance", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : GameConfig
    {
        [SerializeField, Min(0)] private int startingCurrency;
        [SerializeField, Min(1f)] private float targetMissionDurationMinutes = 3f;

        public int StartingCurrency => startingCurrency;

        public float TargetMissionDurationMinutes => targetMissionDurationMinutes;
    }
}
