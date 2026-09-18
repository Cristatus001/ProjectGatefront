using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Squad
{
    public static class FormationCalculator
    {
        private const float CompactTargetAspectRatio = 1.15f;
        private const float CompactSelectionWidthBias = 1.05f;

        private readonly struct CompactCandidatePair
        {
            public CompactCandidatePair(Vector3 positivePosition, float score)
            {
                PositivePosition = positivePosition;
                Score = score;
            }

            public Vector3 PositivePosition { get; }

            public float Score { get; }
        }

        public static void Calculate(
            FormationType formationType,
            int slotCount,
            float rowSpacing,
            float columnSpacing,
            float spreadSpacingMultiplier,
            float compactLateralSpacing,
            float compactDepthSpacing,
            int maxUnitsPerRow,
            float maximumHalfWidth,
            List<FormationSlot> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            if (slotCount <= 0)
            {
                return;
            }

            float safeRowSpacing = Mathf.Max(0.1f, rowSpacing);
            float safeColumnSpacing = Mathf.Max(0.1f, columnSpacing);
            float safeSpreadMultiplier = Mathf.Max(1f, spreadSpacingMultiplier);
            float safeCompactLateralSpacing = Mathf.Max(
                0.1f,
                compactLateralSpacing);
            float safeCompactDepthSpacing = Mathf.Max(
                0.1f,
                compactDepthSpacing);
            int safeMaxUnitsPerRow = Mathf.Max(1, maxUnitsPerRow);
            float safeHalfWidth = Mathf.Max(0f, maximumHalfWidth);

            switch (formationType)
            {
                case FormationType.Compact:
                    CalculateCompact(
                        slotCount,
                        safeCompactLateralSpacing,
                        safeCompactDepthSpacing,
                        safeHalfWidth,
                        results);
                    break;

                case FormationType.Line:
                    CalculateRows(
                        slotCount,
                        safeRowSpacing,
                        safeColumnSpacing,
                        safeMaxUnitsPerRow,
                        safeHalfWidth,
                        results);
                    break;

                case FormationType.Wedge:
                    CalculateWedge(
                        slotCount,
                        safeRowSpacing,
                        safeColumnSpacing,
                        safeHalfWidth,
                        results);
                    break;

                case FormationType.Column:
                    CalculateColumn(
                        slotCount,
                        safeRowSpacing,
                        safeColumnSpacing,
                        safeHalfWidth,
                        results);
                    break;

                case FormationType.Spread:
                    CalculateRows(
                        slotCount,
                        safeRowSpacing * safeSpreadMultiplier,
                        safeColumnSpacing * safeSpreadMultiplier,
                        safeMaxUnitsPerRow,
                        safeHalfWidth,
                        results);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(formationType),
                        formationType,
                        "Unsupported formation type.");
            }

            CenterSlotsAroundRoot(results, safeHalfWidth);
        }

        private static void CalculateCompact(
            int slotCount,
            float lateralSpacing,
            float depthSpacing,
            float maximumHalfWidth,
            List<FormationSlot> results)
        {
            if (slotCount == 1)
            {
                results.Add(new FormationSlot(0, Vector3.zero));
                return;
            }

            int searchRadius = Mathf.Max(4, slotCount);
            var candidatePairs = new List<CompactCandidatePair>(
                searchRadius * searchRadius);
            float halfRowOffset = lateralSpacing * 0.5f;
            float widthBiasSquared = CompactSelectionWidthBias
                * CompactSelectionWidthBias;

            for (int row = -searchRadius; row <= searchRadius; row++)
            {
                float z = row * depthSpacing;
                float rowOffset = Mathf.Abs(row) % 2 == 1
                    ? halfRowOffset
                    : 0f;

                for (int column = -searchRadius;
                    column <= searchRadius;
                    column++)
                {
                    float x = (column * lateralSpacing) + rowOffset;
                    if (Mathf.Abs(x) > maximumHalfWidth + 0.0001f)
                    {
                        continue;
                    }

                    bool isCenter = Mathf.Abs(x) <= 0.0001f
                        && Mathf.Abs(z) <= 0.0001f;
                    bool isCanonicalPairMember = z > 0.0001f
                        || (Mathf.Abs(z) <= 0.0001f && x > 0.0001f);
                    if (isCenter || !isCanonicalPairMember)
                    {
                        continue;
                    }

                    float score = ((x * x) / widthBiasSquared) + (z * z);
                    candidatePairs.Add(new CompactCandidatePair(
                        new Vector3(x, 0f, z),
                        score));
                }
            }

            candidatePairs.Sort(CompareCompactCandidatePairs);

            var selectedPositions = new List<Vector3>(slotCount);
            if (slotCount % 2 == 1)
            {
                selectedPositions.Add(Vector3.zero);
            }

            int requiredPairCount = slotCount / 2;
            for (int pairIndex = 0;
                pairIndex < requiredPairCount;
                pairIndex++)
            {
                Vector3 positive = candidatePairs[pairIndex].PositivePosition;
                selectedPositions.Add(-positive);
                selectedPositions.Add(positive);
            }

            NormalizeCompactDepth(selectedPositions);
            selectedPositions.Sort(CompareCompactPositions);

            for (int index = 0; index < selectedPositions.Count; index++)
            {
                results.Add(new FormationSlot(index, selectedPositions[index]));
            }
        }

        private static int CompareCompactCandidatePairs(
            CompactCandidatePair first,
            CompactCandidatePair second)
        {
            int scoreComparison = first.Score.CompareTo(second.Score);
            if (scoreComparison != 0)
            {
                return scoreComparison;
            }

            int depthComparison = Mathf.Abs(first.PositivePosition.z).CompareTo(
                Mathf.Abs(second.PositivePosition.z));
            if (depthComparison != 0)
            {
                return depthComparison;
            }

            int zComparison = first.PositivePosition.z.CompareTo(
                second.PositivePosition.z);
            return zComparison != 0
                ? zComparison
                : first.PositivePosition.x.CompareTo(second.PositivePosition.x);
        }

        private static int CompareCompactPositions(Vector3 first, Vector3 second)
        {
            int radiusComparison = first.sqrMagnitude.CompareTo(second.sqrMagnitude);
            if (radiusComparison != 0)
            {
                return radiusComparison;
            }

            int depthComparison = Mathf.Abs(first.z).CompareTo(Mathf.Abs(second.z));
            if (depthComparison != 0)
            {
                return depthComparison;
            }

            int zComparison = first.z.CompareTo(second.z);
            return zComparison != 0 ? zComparison : first.x.CompareTo(second.x);
        }

        private static void NormalizeCompactDepth(List<Vector3> positions)
        {
            CalculateBounds(
                positions,
                out float minimumX,
                out float maximumX,
                out float minimumZ,
                out float maximumZ);
            float width = maximumX - minimumX;
            float depth = maximumZ - minimumZ;
            if (width <= 0.0001f || depth <= 0.0001f)
            {
                return;
            }

            float depthScale = (width / CompactTargetAspectRatio) / depth;
            for (int index = 0; index < positions.Count; index++)
            {
                Vector3 position = positions[index];
                position.z *= depthScale;
                positions[index] = position;
            }
        }

        private static void CalculateRows(
            int slotCount,
            float rowSpacing,
            float columnSpacing,
            int maxUnitsPerRow,
            float maximumHalfWidth,
            List<FormationSlot> results)
        {
            int rowCapacity = CalculateRowCapacity(
                maxUnitsPerRow,
                columnSpacing,
                maximumHalfWidth);

            for (int index = 0; index < slotCount; index++)
            {
                int row = index / rowCapacity;
                int rowStart = row * rowCapacity;
                int unitsInRow = Mathf.Min(rowCapacity, slotCount - rowStart);
                int column = index - rowStart;
                float x = CalculateCenteredX(
                    column,
                    unitsInRow,
                    columnSpacing,
                    maximumHalfWidth);
                float z = -(row + 1) * rowSpacing;
                results.Add(new FormationSlot(index, new Vector3(x, 0f, z)));
            }
        }

        private static void CalculateWedge(
            int slotCount,
            float rowSpacing,
            float columnSpacing,
            float maximumHalfWidth,
            List<FormationSlot> results)
        {
            results.Add(new FormationSlot(0, new Vector3(0f, 0f, -rowSpacing)));

            for (int index = 1; index < slotCount; index++)
            {
                int wedgeLevel = ((index - 1) / 2) + 1;
                float side = index % 2 == 1 ? -1f : 1f;
                float x = side * Mathf.Min(
                    wedgeLevel * columnSpacing,
                    maximumHalfWidth);
                float z = -(wedgeLevel + 1) * rowSpacing;
                results.Add(new FormationSlot(index, new Vector3(x, 0f, z)));
            }
        }

        private static void CalculateColumn(
            int slotCount,
            float rowSpacing,
            float columnSpacing,
            float maximumHalfWidth,
            List<FormationSlot> results)
        {
            int widthCapacity = CalculateRowCapacity(
                2,
                columnSpacing,
                maximumHalfWidth);
            int columns = Mathf.Min(slotCount, widthCapacity);

            for (int index = 0; index < slotCount; index++)
            {
                int row = index / columns;
                int rowStart = row * columns;
                int unitsInRow = Mathf.Min(columns, slotCount - rowStart);
                int column = index - rowStart;
                float x = CalculateCenteredX(
                    column,
                    unitsInRow,
                    columnSpacing,
                    maximumHalfWidth);
                float z = -(row + 1) * rowSpacing;
                results.Add(new FormationSlot(index, new Vector3(x, 0f, z)));
            }
        }

        private static int CalculateRowCapacity(
            int requestedCapacity,
            float columnSpacing,
            float maximumHalfWidth)
        {
            if (maximumHalfWidth <= 0f)
            {
                return 1;
            }

            int widthCapacity = Mathf.FloorToInt(
                (maximumHalfWidth * 2f) / columnSpacing) + 1;
            return Mathf.Max(1, Mathf.Min(requestedCapacity, widthCapacity));
        }

        private static float CalculateCenteredX(
            int column,
            int unitsInRow,
            float columnSpacing,
            float maximumHalfWidth)
        {
            if (unitsInRow <= 1)
            {
                return 0f;
            }

            float spacing = CalculateCenteredSpacing(
                unitsInRow,
                columnSpacing,
                maximumHalfWidth);
            return (column - ((unitsInRow - 1) * 0.5f)) * spacing;
        }

        private static float CalculateCenteredSpacing(
            int unitsInRow,
            float columnSpacing,
            float maximumHalfWidth)
        {
            if (unitsInRow <= 1)
            {
                return 0f;
            }

            float availableSpacing = (maximumHalfWidth * 2f) / (unitsInRow - 1);
            return Mathf.Min(columnSpacing, availableSpacing);
        }

        private static void CalculateBounds(
            List<Vector3> positions,
            out float minimumX,
            out float maximumX,
            out float minimumZ,
            out float maximumZ)
        {
            Vector3 firstPosition = positions[0];
            minimumX = firstPosition.x;
            maximumX = firstPosition.x;
            minimumZ = firstPosition.z;
            maximumZ = firstPosition.z;

            for (int index = 1; index < positions.Count; index++)
            {
                Vector3 position = positions[index];
                minimumX = Mathf.Min(minimumX, position.x);
                maximumX = Mathf.Max(maximumX, position.x);
                minimumZ = Mathf.Min(minimumZ, position.z);
                maximumZ = Mathf.Max(maximumZ, position.z);
            }
        }

        private static void CenterSlotsAroundRoot(
            List<FormationSlot> results,
            float maximumHalfWidth)
        {
            if (results.Count == 0)
            {
                return;
            }

            Vector3 centroid = Vector3.zero;
            for (int index = 0; index < results.Count; index++)
            {
                centroid += results[index].LocalPosition;
            }

            centroid /= results.Count;
            float maximumCenteredX = 0f;
            for (int index = 0; index < results.Count; index++)
            {
                Vector3 centeredPosition = results[index].LocalPosition - centroid;
                centeredPosition.y = 0f;
                results[index] = new FormationSlot(index, centeredPosition);
                maximumCenteredX = Mathf.Max(
                    maximumCenteredX,
                    Mathf.Abs(centeredPosition.x));
            }

            if (maximumCenteredX <= maximumHalfWidth + 0.0001f)
            {
                return;
            }

            float horizontalScale = maximumCenteredX > 0.0001f
                ? maximumHalfWidth / maximumCenteredX
                : 0f;
            for (int index = 0; index < results.Count; index++)
            {
                Vector3 position = results[index].LocalPosition;
                position.x *= horizontalScale;
                results[index] = new FormationSlot(index, position);
            }
        }

    }
}
