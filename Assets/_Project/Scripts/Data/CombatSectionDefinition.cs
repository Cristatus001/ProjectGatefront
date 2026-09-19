using System;
using UnityEngine;

namespace ProjectGatefront.Data
{
    [Serializable]
    public sealed class CombatSectionDefinition
    {
        [SerializeField, Min(0f)] private float startDistance;
        [SerializeField, Min(0f)] private float endDistance = 80f;
        [SerializeField, Min(0.05f)] private float spawnInterval = 1f;
        [SerializeField, Min(1)] private int minimumGroupSize = 3;
        [SerializeField, Min(1)] private int maximumGroupSize = 5;
        [SerializeField] private EnemySpawnPattern spawnPattern = EnemySpawnPattern.Cluster;
        [SerializeField, Min(1)] private int rowCount = 2;
        [SerializeField, Min(0.1f)] private float columnSpacing = 1.1f;
        [SerializeField, Min(0.1f)] private float rowSpacing = 1.1f;
        [SerializeField] private float laneCenterX;
        [SerializeField, Min(1f)] private float spawnDepth = 32f;

        public CombatSectionDefinition(
            float start,
            float end,
            float interval,
            int minimumGroup,
            int maximumGroup,
            EnemySpawnPattern pattern,
            int rows,
            float horizontalSpacing,
            float depthSpacing,
            float centerX,
            float depth)
        {
            startDistance = Mathf.Max(0f, start);
            endDistance = Mathf.Max(startDistance, end);
            spawnInterval = Mathf.Max(0.05f, interval);
            minimumGroupSize = Mathf.Max(1, minimumGroup);
            maximumGroupSize = Mathf.Max(minimumGroupSize, maximumGroup);
            spawnPattern = pattern;
            rowCount = Mathf.Max(1, rows);
            columnSpacing = Mathf.Max(0.1f, horizontalSpacing);
            rowSpacing = Mathf.Max(0.1f, depthSpacing);
            laneCenterX = centerX;
            spawnDepth = Mathf.Max(1f, depth);
        }

        public float StartDistance => Mathf.Max(0f, startDistance);
        public float EndDistance => Mathf.Max(StartDistance, endDistance);
        public float SpawnInterval => Mathf.Max(0.05f, spawnInterval);
        public int MinimumGroupSize => Mathf.Max(1, minimumGroupSize);
        public int MaximumGroupSize => Mathf.Max(MinimumGroupSize, maximumGroupSize);
        public EnemySpawnPattern SpawnPattern => spawnPattern;
        public int RowCount => Mathf.Max(1, rowCount);
        public float ColumnSpacing => Mathf.Max(0.1f, columnSpacing);
        public float RowSpacing => Mathf.Max(0.1f, rowSpacing);
        public float LaneCenterX => laneCenterX;
        public float SpawnDepth => Mathf.Max(1f, spawnDepth);
        public bool IsValid => EndDistance > StartDistance;
    }
}
