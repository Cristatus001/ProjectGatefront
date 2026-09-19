using System.Collections.Generic;
using ProjectGatefront.Data;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    public static class EnemyWaveMath
    {
        public static bool IsSpawnThresholdReached(
            float distanceTravelled,
            float spawnDistance)
        {
            float safeDistance = IsFinite(distanceTravelled)
                ? Mathf.Max(0f, distanceTravelled)
                : 0f;
            float safeThreshold = IsFinite(spawnDistance)
                ? Mathf.Max(0f, spawnDistance)
                : float.PositiveInfinity;
            return safeDistance >= safeThreshold;
        }

        public static int ClampSpawnCount(
            int requestedCount,
            int activeCount,
            int activeLimit)
        {
            int safeLimit = Mathf.Max(0, activeLimit);
            int available = Mathf.Max(0, safeLimit - Mathf.Max(0, activeCount));
            return Mathf.Min(Mathf.Max(0, requestedCount), available);
        }

        public static void SortByDistance(List<EnemyWaveDefinition> waves)
        {
            if (waves != null)
            {
                waves.Sort(CompareByDistance);
            }
        }

        private static int CompareByDistance(
            EnemyWaveDefinition first,
            EnemyWaveDefinition second)
        {
            if (ReferenceEquals(first, second))
            {
                return 0;
            }

            if (first == null)
            {
                return 1;
            }

            if (second == null)
            {
                return -1;
            }

            return first.SpawnDistance.CompareTo(second.SpawnDistance);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
