using UnityEngine;

namespace ProjectGatefront.Data
{
    [CreateAssetMenu(menuName = "Project Gatefront/Config/Player", fileName = "PlayerConfig")]
    public sealed class PlayerConfig : GameConfig
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;

        [Tooltip("Stable world Z position used by the player and squad combat anchor.")]
        [SerializeField] private float combatAnchorZ = 2f;

        [Tooltip("World-space lateral distance produced by dragging across the full screen width.")]
        [SerializeField, Min(0f)] private float lateralSensitivity = 10f;

        [Tooltip("Time used to smooth the player root toward its lateral target.")]
        [SerializeField, Min(0.01f)] private float lateralSmoothTime = 0.08f;

        [Tooltip("Maximum absolute world X coordinate available to the player root.")]
        [SerializeField, Min(0.1f)] private float trackHalfWidth = 4.25f;

        [Tooltip("Small inset applied inside both track edges for visible body containment.")]
        [SerializeField, Min(0f)] private float trackBoundaryPadding = 0.08f;

        [Tooltip("Maximum lateral speed in world units per second.")]
        [SerializeField, Min(0.1f)] private float maximumLateralSpeed = 20f;

        public float MaximumHealth => maximumHealth;

        public float CombatAnchorZ => combatAnchorZ;

        public float LateralSensitivity => lateralSensitivity;

        public float LateralSmoothTime => lateralSmoothTime;

        public float TrackHalfWidth => trackHalfWidth;

        public float TrackBoundaryPadding => trackBoundaryPadding;

        public float MaximumLateralSpeed => maximumLateralSpeed;
    }
}
