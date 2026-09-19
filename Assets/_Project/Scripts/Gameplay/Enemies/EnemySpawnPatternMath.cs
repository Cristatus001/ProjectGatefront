using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    public static class EnemySpawnPatternMath
    {
        public static void GenerateOffsets(
            CombatSectionDefinition section,
            float roadHalfWidth,
            float enemyRadius,
            int count,
            List<Vector3> results)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            GenerateOffsets(
                section.SpawnPattern,
                section.RowCount,
                section.ColumnSpacing,
                section.RowSpacing,
                section.LaneCenterX,
                roadHalfWidth,
                enemyRadius,
                count,
                results);
        }

        private static void GenerateOffsets(
            EnemySpawnPattern pattern,
            int rowCount,
            float columnSpacing,
            float rowSpacing,
            float laneCenterX,
            float roadHalfWidth,
            float enemyRadius,
            int count,
            List<Vector3> results)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            results.Clear();
            int safeCount = Mathf.Max(0, count);
            if (safeCount == 0)
            {
                return;
            }

            float usableHalfWidth = Mathf.Max(
                0f,
                roadHalfWidth - Mathf.Max(0f, enemyRadius));
            switch (pattern)
            {
                case EnemySpawnPattern.Row:
                    AddRow(
                        safeCount,
                        0,
                        1,
                        false,
                        columnSpacing,
                        rowSpacing,
                        laneCenterX,
                        usableHalfWidth,
                        results);
                    break;

                case EnemySpawnPattern.Grid:
                case EnemySpawnPattern.Cluster:
                    int rows = Mathf.Clamp(rowCount, 1, safeCount);
                    int columns = Mathf.CeilToInt(safeCount / (float)rows);
                    int remaining = safeCount;
                    for (int row = 0; row < rows && remaining > 0; row++)
                    {
                        int unitsInRow = Mathf.Min(columns, remaining);
                        AddRow(
                            unitsInRow,
                            row,
                            rows,
                            pattern == EnemySpawnPattern.Cluster,
                            columnSpacing,
                            rowSpacing,
                            laneCenterX,
                            usableHalfWidth,
                            results);
                        remaining -= unitsInRow;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(pattern),
                        pattern,
                        "Unsupported enemy spawn pattern.");
            }
        }

        private static void AddRow(
            int unitsInRow,
            int row,
            int totalRows,
            bool staggered,
            float columnSpacing,
            float rowSpacing,
            float laneCenterX,
            float usableHalfWidth,
            List<Vector3> results)
        {
            float rowCenterOffset = staggered && row % 2 == 1
                ? columnSpacing * 0.5f
                : 0f;
            float center = laneCenterX + rowCenterOffset;
            float width = Mathf.Max(0, unitsInRow - 1) * columnSpacing;
            float startX = center - (width * 0.5f);
            float centeredRow = row - ((totalRows - 1) * 0.5f);
            float z = centeredRow * rowSpacing;

            for (int column = 0; column < unitsInRow; column++)
            {
                float x = Mathf.Clamp(
                    startX + (column * columnSpacing),
                    -usableHalfWidth,
                    usableHalfWidth);
                results.Add(new Vector3(x, 0f, z));
            }
        }
    }
}
