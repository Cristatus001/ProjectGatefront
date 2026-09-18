using UnityEngine;

namespace ProjectGatefront.Gameplay.World
{
    public static class WorldScrollMath
    {
        public static float CalculateDistanceDelta(
            float scrollSpeed,
            float deltaTime,
            bool scrollingEnabled)
        {
            if (!scrollingEnabled)
            {
                return 0f;
            }

            float safeSpeed = IsFinite(scrollSpeed)
                ? Mathf.Max(0f, scrollSpeed)
                : 0f;
            float safeDeltaTime = IsFinite(deltaTime)
                ? Mathf.Max(0f, deltaTime)
                : 0f;
            return safeSpeed * safeDeltaTime;
        }

        public static float AccumulateDistance(
            float currentDistance,
            float scrollSpeed,
            float deltaTime,
            bool scrollingEnabled)
        {
            float safeCurrentDistance = IsFinite(currentDistance)
                ? Mathf.Max(0f, currentDistance)
                : 0f;
            return safeCurrentDistance + CalculateDistanceDelta(
                scrollSpeed,
                deltaTime,
                scrollingEnabled);
        }

        public static float CalculateRecycleTargetZ(
            float furthestFrontZ,
            float chunkLength)
        {
            float safeFrontZ = IsFinite(furthestFrontZ) ? furthestFrontZ : 0f;
            float safeLength = IsFinite(chunkLength)
                ? Mathf.Max(0f, chunkLength)
                : 0f;
            return safeFrontZ + safeLength;
        }

        public static float SanitizeSpeed(float scrollSpeed)
        {
            return IsFinite(scrollSpeed) ? Mathf.Max(0f, scrollSpeed) : 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
