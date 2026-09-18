using UnityEngine;

namespace ProjectGatefront.Gameplay.Combat
{
    public static class TargetingMath
    {
        public static bool IsValidForwardTarget(
            Vector3 origin,
            Vector3 forward,
            Vector3 targetPosition,
            float range,
            float minimumForwardCosine)
        {
            Vector3 toTarget = targetPosition - origin;
            float distanceSquared = toTarget.sqrMagnitude;
            float safeRange = Mathf.Max(0f, range);
            if (distanceSquared <= 0.000001f
                || distanceSquared > safeRange * safeRange)
            {
                return false;
            }

            Vector3 safeForward = forward.sqrMagnitude > 0.000001f
                ? forward.normalized
                : Vector3.forward;
            float forwardDot = Vector3.Dot(
                safeForward,
                toTarget / Mathf.Sqrt(distanceSquared));
            return forwardDot >= Mathf.Clamp(minimumForwardCosine, -1f, 1f);
        }

        public static bool SegmentIntersectsSphere(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            Vector3 sphereCenter,
            float sphereRadius,
            out float normalizedDistance)
        {
            Vector3 segment = segmentEnd - segmentStart;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.000001f)
            {
                normalizedDistance = 0f;
                return Vector3.SqrMagnitude(sphereCenter - segmentStart)
                    <= sphereRadius * sphereRadius;
            }

            normalizedDistance = Mathf.Clamp01(
                Vector3.Dot(sphereCenter - segmentStart, segment)
                / lengthSquared);
            Vector3 closestPoint = segmentStart + (segment * normalizedDistance);
            return Vector3.SqrMagnitude(sphereCenter - closestPoint)
                <= sphereRadius * sphereRadius;
        }
    }
}
