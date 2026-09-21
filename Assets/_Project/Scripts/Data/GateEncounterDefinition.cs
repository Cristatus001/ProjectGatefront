using System;
using UnityEngine;

namespace ProjectGatefront.Data
{
    public enum GateEncounterLayout
    {
        SingleLarge,
        ShootableVsDivide
    }

    public enum GateKind
    {
        ShootableAdditive,
        Divide
    }

    public enum GateArithmeticOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    [Serializable]
    public sealed class GateEncounterDefinition
    {
        [SerializeField, Min(0f)] private float distance;
        [SerializeField] private GateEncounterLayout layout;
        [SerializeField] private int shootableStartingValue = -10;
        [SerializeField] private int shootableMinimumValue = -10;
        [SerializeField] private int shootableMaximumValue = 30;
        [SerializeField, Min(2)] private int divideAmount = 2;
        [SerializeField, Range(0.1f, 1f)] private float enemyDensityMultiplier = 0.55f;
        [SerializeField, Min(0f)] private float readabilityBeforeDistance = 18f;
        [SerializeField, Min(0f)] private float readabilityAfterDistance = 8f;
        [SerializeField, Min(10f)] private float spawnDepth = 34f;
        [SerializeField, Min(1f)] private float engagementRange = 26f;

        public GateEncounterDefinition(
            float spawnDistance,
            GateEncounterLayout encounterLayout,
            int startingValue,
            int minimumValue,
            int maximumValue,
            int divisor,
            float densityMultiplier,
            float beforeDistance,
            float afterDistance,
            float depth,
            float gateEngagementRange)
        {
            distance = Mathf.Max(0f, spawnDistance);
            layout = encounterLayout;
            shootableMinimumValue = Mathf.Clamp(minimumValue, -999, 999);
            shootableMaximumValue = Mathf.Clamp(
                maximumValue, shootableMinimumValue, 999);
            shootableStartingValue = Mathf.Clamp(
                startingValue, shootableMinimumValue, shootableMaximumValue);
            divideAmount = Mathf.Max(2, divisor);
            enemyDensityMultiplier = Mathf.Clamp(densityMultiplier, 0.1f, 1f);
            readabilityBeforeDistance = Mathf.Max(0f, beforeDistance);
            readabilityAfterDistance = Mathf.Max(0f, afterDistance);
            spawnDepth = Mathf.Max(10f, depth);
            engagementRange = Mathf.Max(1f, gateEngagementRange);
        }

        public float Distance => Mathf.Max(0f, distance);
        public GateEncounterLayout Layout => layout;
        public int ShootableMinimumValue => Mathf.Clamp(shootableMinimumValue, -999, 999);
        public int ShootableMaximumValue => Mathf.Clamp(
            shootableMaximumValue, ShootableMinimumValue, 999);
        public int ShootableStartingValue => Mathf.Clamp(
            shootableStartingValue, ShootableMinimumValue, ShootableMaximumValue);
        public int DivideAmount => Mathf.Max(2, divideAmount);
        public float EnemyDensityMultiplier => Mathf.Clamp(enemyDensityMultiplier, 0.1f, 1f);
        public float ReadabilityBeforeDistance => Mathf.Max(0f, readabilityBeforeDistance);
        public float ReadabilityAfterDistance => Mathf.Max(0f, readabilityAfterDistance);
        public float SpawnDepth => Mathf.Max(10f, spawnDepth);
        public float EngagementRange => Mathf.Max(1f, engagementRange);
    }
}
