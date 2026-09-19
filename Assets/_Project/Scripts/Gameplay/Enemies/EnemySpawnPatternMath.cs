using System;
using System.Collections.Generic;
using ProjectGatefront.Data;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Enemies
{
    public static class EnemySpawnPatternMath
    {
        public static void GenerateOffsets(
            EnemyWaveDefinition wave,
            float roadHalfWidth,
            float enemyRadius,
            int count,
            List<Vector3> results)
        {
            if (wave == null)
            {
                throw new ArgumentNullException(nameof(wave));
            }

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
            switch (wave.SpawnPattern)
            {
                case EnemySpawnPattern.Row:
                    AddRow(
                        safeCount,
                        0,
                        1,
                        false,
                        wave,
                        usableHalfWidth,
                        results);
                    break;

                case EnemySpawnPattern.Grid:
                case EnemySpawnPattern.Cluster:
                    int rows = Mathf.Clamp(wave.RowCount, 1, safeCount);
                    int columns = Mathf.CeilToInt(safeCount / (float)rows);
                    int remaining = safeCount;
                    for (int row = 0; row < rows && remaining > 0; row++)
                    {
                        int unitsInRow = Mathf.Min(columns, remaining);
                        AddRow(
                            unitsInRow,
                            row,
                            rows,
                            wave.SpawnPattern == EnemySpawnPattern.Cluster,
                            wave,
                            usableHalfWidth,
                            results);
                        remaining -= unitsInRow;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(wave.SpawnPattern),
                        wave.SpawnPattern,
                        "Unsupported enemy spawn pattern.");
            }
        }

        private static void AddRow(
            int unitsInRow,
            int row,
            int totalRows,
            bool staggered,
            EnemyWaveDefinition wave,
            float usableHalfWidth,
            List<Vector3> results)
        {
            float rowCenterOffset = staggered && row % 2 == 1
                ? wave.ColumnSpacing * 0.5f
                : 0f;
            float center = wave.LaneCenterX + rowCenterOffset;
            float width = Mathf.Max(0, unitsInRow - 1) * wave.ColumnSpacing;
            float startX = center - (width * 0.5f);
            float centeredRow = row - ((totalRows - 1) * 0.5f);
            float z = centeredRow * wave.RowSpacing;

            for (int column = 0; column < unitsInRow; column++)
            {
                float x = Mathf.Clamp(
                    startX + (column * wave.ColumnSpacing),
                    -usableHalfWidth,
                    usableHalfWidth);
                results.Add(new Vector3(x, 0f, z));
            }
        }
    }
}
