using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Squad", fileName = "SquadConfig")]
    public sealed class SquadConfig : GameConfig
    {
        [Tooltip("Number of visible soldiers active when the squad initializes.")]
        [SerializeField, Min(1)] private int startingUnitCount = 5;

        [Tooltip("Maximum number of visible soldiers in the squad.")]
        [SerializeField, Min(1)] private int maxSquadSize = 50;

        [Tooltip("Longitudinal distance between formation rows.")]
        [SerializeField, Min(0.1f)] private float rowSpacing = 1.35f;

        [Tooltip("Horizontal distance between adjacent formation slots.")]
        [SerializeField, Min(0.1f)] private float columnSpacing = 1.1f;

        [Tooltip("Spacing multiplier used by the Spread formation.")]
        [SerializeField, Min(1f)] private float spreadSpacingMultiplier = 1.6f;

        [Tooltip("Horizontal spacing used by the rounded Compact crowd layout.")]
        [SerializeField, Min(0.1f)] private float compactLateralSpacing = 0.84f;

        [Tooltip("Initial staggered-row pitch used by the Compact crowd layout.")]
        [SerializeField, Min(0.1f)] private float compactDepthSpacing = 0.7f;

        [Tooltip("Maximum number of members placed in one Line or Spread row.")]
        [SerializeField, Min(1)] private int maxUnitsPerRow = 5;

        [Tooltip("Maximum member movement speed while settling into formation.")]
        [SerializeField, Min(0.1f)] private float formationTransitionSpeed = 18f;

        [Tooltip("Maximum rotation speed used to return members to forward-facing.")]
        [SerializeField, Min(0.1f)] private float unitRotationSpeed = 720f;

        [Tooltip("Maximum absolute local X coordinate for any formation slot.")]
        [SerializeField, Min(0.1f)] private float maximumFormationHalfWidth = 3.7f;

        [Tooltip("Approximate soldier half-width used to keep visible bodies inside the track.")]
        [SerializeField, Min(0f)] private float unitBoundaryRadius = 0.34f;

        public int StartingUnitCount => startingUnitCount;

        public int MaxSquadSize => maxSquadSize;

        public float RowSpacing => rowSpacing;

        public float ColumnSpacing => columnSpacing;

        public float SpreadSpacingMultiplier => spreadSpacingMultiplier;

        public float CompactLateralSpacing => compactLateralSpacing;

        public float CompactDepthSpacing => compactDepthSpacing;

        public int MaxUnitsPerRow => maxUnitsPerRow;

        public float FormationTransitionSpeed => formationTransitionSpeed;

        public float UnitRotationSpeed => unitRotationSpeed;

        public float MaximumFormationHalfWidth => maximumFormationHalfWidth;

        public float UnitBoundaryRadius => unitBoundaryRadius;
    }
}
