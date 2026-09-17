using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Squad", fileName = "SquadConfig")]
    public sealed class SquadConfig : GameConfig
    {
        [Tooltip("Total starting combatants, including the visible player leader.")]
        [SerializeField, Min(1)] private int startingUnitCount = 5;

        [Tooltip("Maximum total combatants, including the visible player leader.")]
        [SerializeField, Min(1)] private int maxSquadSize = 50;

        [Tooltip("Longitudinal distance between formation rows.")]
        [SerializeField, Min(0.1f)] private float rowSpacing = 1.35f;

        [Tooltip("Horizontal distance between adjacent formation slots.")]
        [SerializeField, Min(0.1f)] private float columnSpacing = 1.1f;

        [Tooltip("Spacing multiplier used by the Spread formation.")]
        [SerializeField, Min(1f)] private float spreadSpacingMultiplier = 1.6f;

        [Tooltip("Maximum number of followers placed in one Line or Spread row.")]
        [SerializeField, Min(1)] private int maxUnitsPerRow = 5;

        [Tooltip("Maximum follower movement speed while settling into formation.")]
        [SerializeField, Min(0.1f)] private float formationTransitionSpeed = 12f;

        [Tooltip("Maximum rotation speed used to return followers to forward-facing.")]
        [SerializeField, Min(0.1f)] private float unitRotationSpeed = 720f;

        [Tooltip("Maximum absolute local X coordinate for any formation slot.")]
        [SerializeField, Min(0.1f)] private float maximumFormationHalfWidth = 3.7f;

        public int StartingUnitCount => startingUnitCount;

        public int MaxSquadSize => maxSquadSize;

        public float RowSpacing => rowSpacing;

        public float ColumnSpacing => columnSpacing;

        public float SpreadSpacingMultiplier => spreadSpacingMultiplier;

        public int MaxUnitsPerRow => maxUnitsPerRow;

        public float FormationTransitionSpeed => formationTransitionSpeed;

        public float UnitRotationSpeed => unitRotationSpeed;

        public float MaximumFormationHalfWidth => maximumFormationHalfWidth;
    }
}
