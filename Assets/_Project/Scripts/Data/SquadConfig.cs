using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Squad", fileName = "SquadConfig")]
    public sealed class SquadConfig : GameConfig
    {
        [SerializeField, Min(1)] private int startingUnitCount = 1;
        [SerializeField, Min(1)] private int maximumUnitCount = 50;
        [SerializeField, Min(0.1f)] private float unitSpacing = 1f;

        public int StartingUnitCount => startingUnitCount;

        public int MaximumUnitCount => maximumUnitCount;

        public float UnitSpacing => unitSpacing;
    }
}
