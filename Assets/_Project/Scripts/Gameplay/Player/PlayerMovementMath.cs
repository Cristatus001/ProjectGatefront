using UnityEngine;

namespace ProjectGatefront.Gameplay.Player
{
    public static class PlayerMovementMath
    {
        public static float CalculateLateralTarget(
            float currentTarget,
            float normalizedDragDelta,
            float sensitivity,
            float trackHalfWidth)
        {
            float target = currentTarget + (normalizedDragDelta * Mathf.Max(0f, sensitivity));
            return ClampHorizontal(target, trackHalfWidth);
        }

        public static float ClampHorizontal(float horizontalPosition, float trackHalfWidth)
        {
            float safeHalfWidth = Mathf.Max(0f, trackHalfWidth);
            return Mathf.Clamp(horizontalPosition, -safeHalfWidth, safeHalfWidth);
        }

        public static float CalculateUsableTrackHalfWidth(
            float trackHalfWidth,
            float bodyRadius,
            float boundaryPadding)
        {
            return Mathf.Max(
                0f,
                trackHalfWidth
                    - Mathf.Max(0f, bodyRadius)
                    - Mathf.Max(0f, boundaryPadding));
        }
    }
}
