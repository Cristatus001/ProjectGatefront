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
            float safeRadius = Mathf.Max(0f, sphereRadius);
            if (lengthSquared <= 0.000001f)
            {
                normalizedDistance = 0f;
                return Vector3.SqrMagnitude(sphereCenter - segmentStart)
                    <= safeRadius * safeRadius;
            }

            Vector3 fromCenter = segmentStart - sphereCenter;
            float c = Vector3.Dot(fromCenter, fromCenter)
                - (safeRadius * safeRadius);
            if (c <= 0f)
            {
                normalizedDistance = 0f;
                return true;
            }

            float b = 2f * Vector3.Dot(fromCenter, segment);
            float discriminant = (b * b) - (4f * lengthSquared * c);
            if (discriminant < 0f)
            {
                normalizedDistance = 0f;
                return false;
            }

            normalizedDistance = (-b - Mathf.Sqrt(discriminant))
                / (2f * lengthSquared);
            return normalizedDistance >= 0f && normalizedDistance <= 1f;
        }

        public static bool SegmentIntersectsAabb(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            Vector3 boundsCenter,
            Vector3 boundsExtents,
            out float normalizedDistance)
        {
            Vector3 minimum = boundsCenter - boundsExtents;
            Vector3 maximum = boundsCenter + boundsExtents;
            Vector3 direction = segmentEnd - segmentStart;
            float entry = 0f;
            float exit = 1f;
            if (!ClipAxis(segmentStart.x, direction.x, minimum.x, maximum.x,
                    ref entry, ref exit)
                || !ClipAxis(segmentStart.y, direction.y, minimum.y, maximum.y,
                    ref entry, ref exit)
                || !ClipAxis(segmentStart.z, direction.z, minimum.z, maximum.z,
                    ref entry, ref exit))
            {
                normalizedDistance = 0f;
                return false;
            }

            normalizedDistance = entry;
            return true;
        }

        public static bool IsTargetInProjectileCorridor(
            Vector3 origin,
            Vector3 destination,
            Vector3 targetPosition,
            float combinedRadius,
            out float normalizedDistance)
        {
            normalizedDistance = 0f;
            return targetPosition.z > origin.z
                && targetPosition.z < destination.z
                && SegmentIntersectsSphere(origin, destination, targetPosition,
                    combinedRadius, out normalizedDistance)
                && normalizedDistance < 1f;
        }

        private static bool ClipAxis(
            float origin,
            float direction,
            float minimum,
            float maximum,
            ref float entry,
            ref float exit)
        {
            if (Mathf.Abs(direction) <= 0.000001f)
            {
                return origin >= minimum && origin <= maximum;
            }

            float inverse = 1f / direction;
            float first = (minimum - origin) * inverse;
            float second = (maximum - origin) * inverse;
            if (first > second)
            {
                float swap = first;
                first = second;
                second = swap;
            }

            entry = Mathf.Max(entry, first);
            exit = Mathf.Min(exit, second);
            return entry <= exit;
        }
    }
}
