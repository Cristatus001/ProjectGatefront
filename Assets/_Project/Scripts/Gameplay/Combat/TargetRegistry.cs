using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class TargetRegistry : MonoBehaviour
    {
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

        public bool TryFindFirstSegmentHit(
            Vector3 segmentStart,
            Vector3 segmentEnd,
            float projectileRadius,
            ITargetable ignoredTarget,
            out ITargetable hitTarget)
        {
            hitTarget = null;
            float closestNormalizedDistance = float.PositiveInfinity;
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

                float combinedRadius = Mathf.Max(0f, projectileRadius)
                    + Mathf.Max(0f, candidate.TargetRadius);
                if (TargetingMath.SegmentIntersectsSphere(
                        segmentStart,
                        segmentEnd,
                        candidate.AimPosition,
                        combinedRadius,
                        out float normalizedDistance)
                    && normalizedDistance < closestNormalizedDistance)
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
