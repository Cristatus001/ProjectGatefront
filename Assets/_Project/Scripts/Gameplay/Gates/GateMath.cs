using ProjectGatefront.Data;
using UnityEngine;

namespace ProjectGatefront.Gameplay.Gates
{
    public static class GateMath
    {
        public static int Charge(int currentValue, int maximumValue)
        {
            return Mathf.Min(currentValue + 1, maximumValue);
        }

        public static int ApplyArithmetic(
            int squadCount,
            GateArithmeticOperation operation,
            int operand,
            int maximumSquadSize)
        {
            long result;
            switch (operation)
            {
                case GateArithmeticOperation.Add:
                    result = (long)squadCount + operand;
                    break;
                case GateArithmeticOperation.Subtract:
                    result = (long)squadCount - operand;
                    break;
                case GateArithmeticOperation.Multiply:
                    result = (long)squadCount * operand;
                    break;
                case GateArithmeticOperation.Divide:
                    result = squadCount / Mathf.Max(1, operand);
                    break;
                default:
                    result = squadCount;
                    break;
            }

            return (int)System.Math.Max(
                1L,
                System.Math.Min(result, Mathf.Max(1, maximumSquadSize)));
        }

        public static bool CrossedSelectionPlane(
            float previousZ,
            float currentZ,
            float selectionZ)
        {
            return previousZ > selectionZ && currentZ <= selectionZ;
        }

        public static int SelectNearestLane(
            float squadX,
            float leftGateX,
            float rightGateX)
        {
            return Mathf.Abs(squadX - leftGateX)
                <= Mathf.Abs(squadX - rightGateX) ? 0 : 1;
        }

        public static bool IsAligned(
            float squadX,
            float gateX,
            float halfWidth)
        {
            return Mathf.Abs(squadX - gateX) <= Mathf.Max(0f, halfWidth);
        }
    }
}
