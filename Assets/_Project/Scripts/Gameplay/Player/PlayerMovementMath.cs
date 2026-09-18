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

        public static float CalculateLateralTarget(
            float currentTarget,
            float normalizedDragDelta,
            float sensitivity,
            float minimumHorizontal,
            float maximumHorizontal)
        {
            float target = currentTarget
                + (normalizedDragDelta * Mathf.Max(0f, sensitivity));
            return ClampHorizontal(target, minimumHorizontal, maximumHorizontal);
        }

        public static float ClampHorizontal(float horizontalPosition, float trackHalfWidth)
        {
            float safeHalfWidth = Mathf.Max(0f, trackHalfWidth);
            return Mathf.Clamp(horizontalPosition, -safeHalfWidth, safeHalfWidth);
        }

        public static float ClampHorizontal(
            float horizontalPosition,
            float minimumHorizontal,
            float maximumHorizontal)
        {
            float safeMinimum = IsFinite(minimumHorizontal)
                ? minimumHorizontal
                : 0f;
            float safeMaximum = IsFinite(maximumHorizontal)
                ? maximumHorizontal
                : 0f;
            if (safeMinimum > safeMaximum)
            {
                float swap = safeMinimum;
                safeMinimum = safeMaximum;
                safeMaximum = swap;
            }

            float safePosition = IsFinite(horizontalPosition)
                ? horizontalPosition
                : 0f;
            return Mathf.Clamp(safePosition, safeMinimum, safeMaximum);
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

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
