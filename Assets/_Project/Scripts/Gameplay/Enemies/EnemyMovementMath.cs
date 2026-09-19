using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    public static class EnemyMovementMath
    {
        public static float CalculateEffectiveApproachSpeed(
            float worldScrollSpeed,
            float enemyApproachSpeed,
            bool movementEnabled)
        {
            if (!movementEnabled)
            {
                return 0f;
            }

            return SanitizeSpeed(worldScrollSpeed)
                + SanitizeSpeed(enemyApproachSpeed);
        }

        public static float CalculateApproachDistance(
            float worldScrollSpeed,
            float enemyApproachSpeed,
            float deltaTime,
            bool movementEnabled)
        {
            float safeDeltaTime = IsFinite(deltaTime)
                ? Mathf.Max(0f, deltaTime)
                : 0f;
            return CalculateEffectiveApproachSpeed(
                    worldScrollSpeed,
                    enemyApproachSpeed,
                    movementEnabled)
                * safeDeltaTime;
        }

        private static float SanitizeSpeed(float speed)
        {
            return IsFinite(speed) ? Mathf.Max(0f, speed) : 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
