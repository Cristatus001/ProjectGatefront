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

        public static void CalculateRootHorizontalRange(
            float idealLocalMinX,
            float idealLocalMaxX,
            float trackHalfWidth,
            float unitBoundaryRadius,
            float boundaryPadding,
            out float minimumRootX,
            out float maximumRootX)
        {
            float safeLocalMinX = IsFinite(idealLocalMinX) ? idealLocalMinX : 0f;
            float safeLocalMaxX = IsFinite(idealLocalMaxX) ? idealLocalMaxX : 0f;
            float localMinX = Mathf.Min(safeLocalMinX, safeLocalMaxX);
            float localMaxX = Mathf.Max(safeLocalMinX, safeLocalMaxX);
            float usableHalfWidth = CalculateUsableTrackHalfWidth(
                trackHalfWidth,
                unitBoundaryRadius,
                boundaryPadding);
            minimumRootX = -usableHalfWidth - localMinX;
            maximumRootX = usableHalfWidth - localMaxX;

            if (minimumRootX <= maximumRootX)
            {
                return;
            }

            float centeredRootX = (minimumRootX + maximumRootX) * 0.5f;
            minimumRootX = centeredRootX;
            maximumRootX = centeredRootX;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
