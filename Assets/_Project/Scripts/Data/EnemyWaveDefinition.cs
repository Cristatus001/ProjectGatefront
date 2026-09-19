using System;
using UnityEngine;

namespace ProjectGatefront.Data
{
    [Serializable]
    public sealed class EnemyWaveDefinition
    {
        [SerializeField, Min(0f)] private float spawnDistance;
        [SerializeField] private EnemyConfig enemyConfig;
        [SerializeField, Min(0)] private int count = 5;
        [SerializeField] private EnemySpawnPattern spawnPattern =
            EnemySpawnPattern.Row;
        [SerializeField, Min(1)] private int rowCount = 1;
        [SerializeField, Min(0.1f)] private float columnSpacing = 1.15f;
        [SerializeField, Min(0.1f)] private float rowSpacing = 1.15f;
        [SerializeField] private float laneCenterX;
        [SerializeField, Min(1f)] private float spawnDepth = 34f;

        public EnemyWaveDefinition()
        {
        }

        public EnemyWaveDefinition(
            float distance,
            EnemyConfig config,
            int enemyCount,
            EnemySpawnPattern pattern,
            int rows,
            float horizontalSpacing,
            float depthSpacing,
            float centerX,
            float depth)
        {
            spawnDistance = Mathf.Max(0f, distance);
            enemyConfig = config;
            count = Mathf.Max(0, enemyCount);
            spawnPattern = pattern;
            rowCount = Mathf.Max(1, rows);
            columnSpacing = Mathf.Max(0.1f, horizontalSpacing);
            rowSpacing = Mathf.Max(0.1f, depthSpacing);
            laneCenterX = centerX;
            spawnDepth = Mathf.Max(1f, depth);
        }

        public float SpawnDistance => Mathf.Max(0f, spawnDistance);

        public EnemyConfig EnemyConfig => enemyConfig;

        public int Count => Mathf.Max(0, count);

        public EnemySpawnPattern SpawnPattern => spawnPattern;

        public int RowCount => Mathf.Max(1, rowCount);

        public float ColumnSpacing => Mathf.Max(0.1f, columnSpacing);

        public float RowSpacing => Mathf.Max(0.1f, rowSpacing);

        public float LaneCenterX => laneCenterX;

        public float SpawnDepth => Mathf.Max(1f, spawnDepth);

        public bool IsValid => enemyConfig != null && Count > 0;
    }
}
