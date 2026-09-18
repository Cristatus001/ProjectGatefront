using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Game Balance", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : GameConfig
    {
        [SerializeField, Min(0)] private int startingCurrency;
        [SerializeField, Min(1f)] private float targetMissionDurationMinutes = 3f;

        [Tooltip("Base speed at which battlefield content moves toward the squad.")]
        [SerializeField, Min(0f)] private float worldScrollSpeed = 8f;

        [Tooltip("Exact longitudinal length of one recyclable track chunk.")]
        [SerializeField, Min(1f)] private float trackChunkLength = 30f;

        [Tooltip("Number of track chunks kept alive in the scrolling ring.")]
        [SerializeField, Min(3)] private int trackChunkCount = 5;

        public int StartingCurrency => startingCurrency;

        public float TargetMissionDurationMinutes => targetMissionDurationMinutes;

        public float WorldScrollSpeed => worldScrollSpeed;

        public float TrackChunkLength => trackChunkLength;

        public int TrackChunkCount => trackChunkCount;
    }
}
