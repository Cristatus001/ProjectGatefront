using System.Collections.Generic;
using ProjectGatefront.Data;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    public static class ContinuousSpawnMath
    {
        public static int FindSection(
            IReadOnlyList<CombatSectionDefinition> sections,
            float distance)
        {
            if (sections == null || float.IsNaN(distance))
            {
                return -1;
            }

            for (int index = 0; index < sections.Count; index++)
            {
                CombatSectionDefinition section = sections[index];
                if (section != null
                    && section.IsValid
                    && distance >= section.StartDistance
                    && distance < section.EndDistance)
                {
                    return index;
                }
            }

            return -1;
        }

        public static int GroupSize(CombatSectionDefinition section, int sequence)
        {
            if (section == null)
            {
                return 0;
            }

            int range = section.MaximumGroupSize - section.MinimumGroupSize + 1;
            return section.MinimumGroupSize + (Mathf.Max(0, sequence) % range);
        }

        public static float NextSpawnDistance(
            float currentDistance,
            float worldSpeed,
            CombatSectionDefinition section)
        {
            if (section == null)
            {
                return float.PositiveInfinity;
            }

            float safeSpeed = float.IsNaN(worldSpeed)
                || float.IsInfinity(worldSpeed)
                ? 0f
                : Mathf.Max(0f, worldSpeed);
            return currentDistance
                + Mathf.Max(0.1f, safeSpeed * section.SpawnInterval);
        }

        public static int ClampToActiveLimit(
            int requested,
            int active,
            int limit)
        {
            return Mathf.Min(
                Mathf.Max(0, requested),
                Mathf.Max(0, Mathf.Max(0, limit) - Mathf.Max(0, active)));
        }

        public static void SortByStartDistance(
            List<CombatSectionDefinition> sections)
        {
            sections?.Sort((first, second) =>
                first.StartDistance.CompareTo(second.StartDistance));
        }
    }
}
