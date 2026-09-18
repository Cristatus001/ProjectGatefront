using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Squad
{
    public static class FormationCalculator
    {
        public static void Calculate(
            FormationType formationType,
            int slotCount,
            float rowSpacing,
            float columnSpacing,
            float spreadSpacingMultiplier,
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
            int safeMaxUnitsPerRow = Mathf.Max(1, maxUnitsPerRow);
            float safeHalfWidth = Mathf.Max(0f, maximumHalfWidth);

            switch (formationType)
            {
                case FormationType.Compact:
                    CalculateCompact(
                        slotCount,
                        safeRowSpacing,
                        safeColumnSpacing,
                        safeMaxUnitsPerRow,
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
        }

        private static void CalculateCompact(
            int slotCount,
            float rowSpacing,
            float columnSpacing,
            int maxUnitsPerRow,
            float maximumHalfWidth,
            List<FormationSlot> results)
        {
            const float compactSpacingMultiplier = 0.78f;
            float compactRowSpacing = rowSpacing * compactSpacingMultiplier;
            float compactColumnSpacing = columnSpacing * compactSpacingMultiplier;
            int remaining = slotCount;
            int slotIndex = 0;
            int row = 0;

            while (remaining > 0)
            {
                int requestedCapacity;
                if (row == 0)
                {
                    requestedCapacity = 3;
                }
                else if (row % 2 == 1)
                {
                    requestedCapacity = 4;
                }
                else
                {
                    requestedCapacity = 5;
                }

                int rowCapacity = CalculateRowCapacity(
                    Mathf.Min(maxUnitsPerRow, requestedCapacity),
                    compactColumnSpacing,
                    maximumHalfWidth);
                int unitsInRow = Mathf.Min(rowCapacity, remaining);
                float spacing = CalculateCenteredSpacing(
                    unitsInRow,
                    compactColumnSpacing,
                    maximumHalfWidth);

                for (int order = 0; order < unitsInRow; order++)
                {
                    float x = CalculateCenterFirstX(order, unitsInRow, spacing);
                    float z = -(row + 1) * compactRowSpacing;
                    results.Add(new FormationSlot(
                        slotIndex,
                        new Vector3(x, 0f, z)));
                    slotIndex++;
                }

                remaining -= unitsInRow;
                row++;
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

        private static float CalculateCenterFirstX(
            int order,
            int rowCapacity,
            float spacing)
        {
            if (rowCapacity % 2 == 1)
            {
                if (order == 0)
                {
                    return 0f;
                }

                int step = (order + 1) / 2;
                return (order % 2 == 1 ? -step : step) * spacing;
            }

            int evenStep = order / 2;
            float magnitude = (evenStep + 0.5f) * spacing;
            return order % 2 == 0 ? -magnitude : magnitude;
        }
    }
}
