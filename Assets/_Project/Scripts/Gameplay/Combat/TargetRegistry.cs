using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class TargetRegistry : MonoBehaviour
    {
        // At nearly coincident surfaces, a combat threat visually shields a gate.
        private const float HitTieEpsilonWorldUnits = 0.01f;
        private readonly List<ITargetable> targets = new List<ITargetable>(32);

        public int ActiveTargetCount => targets.Count;

        public bool Register(ITargetable target)
        {
            if (target == null || targets.Contains(target))
            {
                return false;
            }

            targets.Add(target);
            return true;
        }

        public bool Unregister(ITargetable target)
        {
            return target != null && targets.Remove(target);
        }

        public ITargetable FindClosestForwardTarget(
            Vector3 origin,
            Vector3 forward,
            float range,
            float minimumForwardCosine)
        {
            ITargetable closestTarget = null;
            float closestDistanceSquared = float.PositiveInfinity;
            for (int index = targets.Count - 1; index >= 0; index--)
            {
                ITargetable candidate = targets[index];
                if (candidate == null)
                {
                    targets.RemoveAt(index);
                    continue;
                }

                if (!candidate.IsTargetable
                    || !TargetingMath.IsValidForwardTarget(
                        origin,
                        forward,
                        candidate.AimPosition,
                        range,
                        minimumForwardCosine))
                {
                    continue;
                }

                float distanceSquared = Vector3.SqrMagnitude(
                    candidate.AimPosition - origin);
                if (distanceSquared < closestDistanceSquared)
                {
                    closestDistanceSquared = distanceSquared;
                    closestTarget = candidate;
                }
            }

            return closestTarget;
        }

        public int CollectClosestForwardTargets(
            Vector3 origin,
            Vector3 forward,
            float range,
            float minimumForwardCosine,
            int maximumCount,
            List<ITargetable> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            int safeMaximumCount = Mathf.Max(0, maximumCount);
            if (safeMaximumCount == 0)
            {
                return 0;
            }

            for (int targetIndex = targets.Count - 1;
                targetIndex >= 0;
                targetIndex--)
            {
                ITargetable candidate = targets[targetIndex];
                if (candidate == null)
                {
                    targets.RemoveAt(targetIndex);
                    continue;
                }

                if (!candidate.IsTargetable
                    || !TargetingMath.IsValidForwardTarget(
                        origin,
                        forward,
                        candidate.AimPosition,
                        range,
                        minimumForwardCosine))
                {
                    continue;
                }

                float candidateDistanceSquared = Vector3.SqrMagnitude(
                    candidate.AimPosition - origin);
                int insertionIndex = results.Count;
                for (int resultIndex = 0;
                    resultIndex < results.Count;
                    resultIndex++)
                {
                    float existingDistanceSquared = Vector3.SqrMagnitude(
                        results[resultIndex].AimPosition - origin);
                    if (candidateDistanceSquared < existingDistanceSquared)
                    {
                        insertionIndex = resultIndex;
                        break;
                    }
                }

                if (insertionIndex < safeMaximumCount)
                {
                    results.Insert(insertionIndex, candidate);
                    if (results.Count > safeMaximumCount)
                    {
                        results.RemoveAt(results.Count - 1);
                    }
                }
            }

            return results.Count;
        }

        public bool TryFindFirstSegmentHit(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            float projectileRadius,
            ITargetable ignoredTarget,
            out ITargetable hitTarget)
        {
            hitTarget = null;
            float closestNormalizedDistance = float.PositiveInfinity;
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            float normalizedTieEpsilon = segmentLength <= 0.000001f
                ? 0f
                : HitTieEpsilonWorldUnits / segmentLength;
            for (int index = targets.Count - 1; index >= 0; index--)
            {
                ITargetable candidate = targets[index];
                if (candidate == null)
                {
                    targets.RemoveAt(index);
                    continue;
                }

                if (!candidate.IsTargetable || ReferenceEquals(candidate, ignoredTarget))
                {
                    continue;
                }

                bool intersects;
                float normalizedDistance;
                if (candidate is IProjectileHitVolume customVolume)
                {
                    intersects = customVolume.TryGetProjectileIntersection(
                        segmentStart,
                        segmentEnd,
                        projectileRadius,
                        out normalizedDistance);
                }
                else
                {
                    float combinedRadius = Mathf.Max(0f, projectileRadius)
                        + Mathf.Max(0f, candidate.TargetRadius);
                    intersects = TargetingMath.SegmentIntersectsSphere(
                        segmentStart,
                        segmentEnd,
                        candidate.AimPosition,
                        combinedRadius,
                        out normalizedDistance);
                }

                bool isCloser = normalizedDistance
                    < closestNormalizedDistance - normalizedTieEpsilon;
                bool winsThreatTie = Mathf.Abs(normalizedDistance
                        - closestNormalizedDistance) <= normalizedTieEpsilon
                    && candidate is IDamageable
                    && hitTarget is ProjectGatefront.Gameplay.Gates.IShootableGate;
                if (intersects && (isCloser || winsThreatTie))
                {
                    closestNormalizedDistance = normalizedDistance;
                    hitTarget = candidate;
                }
            }

            return hitTarget != null;
        }

        public void Clear()
        {
            targets.Clear();
        }
    }
}
