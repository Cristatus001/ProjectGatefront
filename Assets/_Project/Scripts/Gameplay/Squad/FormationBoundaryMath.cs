using UnityEngine;

namespace ProjectGatefront.Gameplay.Squad
{
    public static class FormationBoundaryMath
    {
        public static float CalculateUsableTrackHalfWidth(
            float trackHalfWidth,
            float unitBoundaryRadius,
            float boundaryPadding)
        {
            float safeTrackHalfWidth = IsFinite(trackHalfWidth)
                ? Mathf.Max(0f, trackHalfWidth)
                : 0f;
            float safeUnitRadius = IsFinite(unitBoundaryRadius)
                ? Mathf.Max(0f, unitBoundaryRadius)
                : 0f;
            float safePadding = IsFinite(boundaryPadding)
                ? Mathf.Max(0f, boundaryPadding)
                : 0f;

            return Mathf.Max(
                0f,
                safeTrackHalfWidth - safeUnitRadius - safePadding);
        }

        public static float CalculateHorizontalFormationCorrection(
            int followerCount,
            float leaderWorldX,
            float idealLocalMinX,
            float idealLocalMaxX,
            float trackHalfWidth,
            float unitBoundaryRadius,
            float boundaryPadding)
        {
            if (followerCount <= 0)
            {
                return 0f;
            }

            float safeLeaderWorldX = IsFinite(leaderWorldX) ? leaderWorldX : 0f;
            float safeLocalMinX = IsFinite(idealLocalMinX) ? idealLocalMinX : 0f;
            float safeLocalMaxX = IsFinite(idealLocalMaxX) ? idealLocalMaxX : 0f;
            float localMinX = Mathf.Min(safeLocalMinX, safeLocalMaxX);
            float localMaxX = Mathf.Max(safeLocalMinX, safeLocalMaxX);
            float usableHalfWidth = CalculateUsableTrackHalfWidth(
                trackHalfWidth,
                unitBoundaryRadius,
                boundaryPadding);

            float intendedMinX = safeLeaderWorldX + localMinX;
            float intendedMaxX = safeLeaderWorldX + localMaxX;
            float usableWidth = usableHalfWidth * 2f;
            float formationWidth = intendedMaxX - intendedMinX;

            if (formationWidth > usableWidth)
            {
                float intendedCenterX = (intendedMinX + intendedMaxX) * 0.5f;
                return IsFinite(intendedCenterX) ? -intendedCenterX : 0f;
            }

            if (intendedMinX < -usableHalfWidth)
            {
                return -usableHalfWidth - intendedMinX;
            }

            if (intendedMaxX > usableHalfWidth)
            {
                return usableHalfWidth - intendedMaxX;
            }

            return 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
