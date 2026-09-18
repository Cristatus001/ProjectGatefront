using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Gameplay.Squad;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class FormationBoundaryMathTests
    {
        private const float TrackHalfWidth = 4.25f;
        private const float UnitRadius = 0.34f;
        private const float Padding = 0.08f;
        private const float Tolerance = 0.0001f;

        private readonly List<FormationSlot> slots = new List<FormationSlot>();

        [Test]
        public void CenteredFormation_ReturnsZeroCorrection()
        {
            float correction = CalculateCorrection(0f, -1.5f, 1.5f);

            Assert.That(correction, Is.Zero.Within(Tolerance));
        }

        [Test]
        public void LeftOverflow_ReturnsPositiveCorrection()
        {
            float correction = CalculateCorrection(-3.75f, -1.5f, 1.5f);

            Assert.That(correction, Is.GreaterThan(0f));
        }

        [Test]
        public void RightOverflow_ReturnsNegativeCorrection()
        {
            float correction = CalculateCorrection(3.75f, -1.5f, 1.5f);

            Assert.That(correction, Is.LessThan(0f));
        }

        [Test]
        public void CorrectedMinimum_RespectsLeftUsableLimit()
        {
            const float leaderWorldX = -3.75f;
            const float localMinX = -1.5f;
            float correction = CalculateCorrection(leaderWorldX, localMinX, 1.5f);

            Assert.That(
                leaderWorldX + localMinX + correction,
                Is.GreaterThanOrEqualTo(-UsableHalfWidth - Tolerance));
        }

        [Test]
        public void CorrectedMaximum_RespectsRightUsableLimit()
        {
            const float leaderWorldX = 3.75f;
            const float localMaxX = 1.5f;
            float correction = CalculateCorrection(leaderWorldX, -1.5f, localMaxX);

            Assert.That(
                leaderWorldX + localMaxX + correction,
                Is.LessThanOrEqualTo(UsableHalfWidth + Tolerance));
        }

        [Test]
        public void GroupCorrection_PreservesRelativeSlotSpacing()
        {
            const float leaderWorldX = -3.75f;
            const float firstLocalX = -1.5f;
            const float secondLocalX = -0.4f;
            float correction = CalculateCorrection(leaderWorldX, firstLocalX, 1.5f);
            float originalSpacing = secondLocalX - firstLocalX;
            float correctedSpacing =
                (leaderWorldX + secondLocalX + correction)
                - (leaderWorldX + firstLocalX + correction);

            Assert.That(correctedSpacing, Is.EqualTo(originalSpacing).Within(Tolerance));
        }

        [Test]
        public void UnitRadius_IsIncludedInUsableLimit()
        {
            float correction = FormationBoundaryMath
                .CalculateHorizontalFormationCorrection(
                    1,
                    0f,
                    4.1f,
                    4.1f,
                    TrackHalfWidth,
                    0.2f,
                    0f);

            Assert.That(correction, Is.EqualTo(-0.05f).Within(Tolerance));
        }

        [Test]
        public void Padding_IsIncludedInUsableLimit()
        {
            float correction = FormationBoundaryMath
                .CalculateHorizontalFormationCorrection(
                    1,
                    0f,
                    4.1f,
                    4.1f,
                    TrackHalfWidth,
                    0f,
                    0.2f);

            Assert.That(correction, Is.EqualTo(-0.05f).Within(Tolerance));
        }

        [Test]
        public void ExactlyOnBoundary_ReturnsZeroCorrection()
        {
            float correction = CalculateCorrection(
                0f,
                -UsableHalfWidth,
                UsableHalfWidth);

            Assert.That(correction, Is.Zero.Within(Tolerance));
        }

        [Test]
        public void EmptyFollowers_ReturnsZeroSafely()
        {
            float correction = FormationBoundaryMath
                .CalculateHorizontalFormationCorrection(
                    0,
                    float.NaN,
                    float.NegativeInfinity,
                    float.PositiveInfinity,
                    float.NaN,
                    float.NaN,
                    float.NaN);

            Assert.That(correction, Is.Zero);
        }

        [Test]
        public void NonFiniteInputs_ReturnFiniteCorrection()
        {
            float correction = FormationBoundaryMath
                .CalculateHorizontalFormationCorrection(
                    1,
                    float.PositiveInfinity,
                    float.NaN,
                    float.NegativeInfinity,
                    float.PositiveInfinity,
                    float.NaN,
                    float.NegativeInfinity);

            Assert.That(float.IsNaN(correction), Is.False);
            Assert.That(float.IsInfinity(correction), Is.False);
        }

        [TestCase(FormationType.Compact)]
        [TestCase(FormationType.Line)]
        [TestCase(FormationType.Wedge)]
        [TestCase(FormationType.Column)]
        [TestCase(FormationType.Spread)]
        public void FormationTargets_StayInsideTrackAtAllTestedSizesAndPositions(
            FormationType formationType)
        {
            int[] totalCombatantCounts = { 5, 10, 20, 50 };
            float[] leaderWorldPositions = { 0f, -3.75f, 3.75f };

            for (int countIndex = 0;
                countIndex < totalCombatantCounts.Length;
                countIndex++)
            {
                int followerCount = totalCombatantCounts[countIndex] - 1;
                FormationCalculator.Calculate(
                    formationType,
                    followerCount,
                    1.35f,
                    1.1f,
                    1.6f,
                    1.15f,
                    0.72f,
                    5,
                    Mathf.Min(3.7f, UsableHalfWidth),
                    slots);
                CalculateBounds(slots, out float localMinX, out float localMaxX);

                for (int positionIndex = 0;
                    positionIndex < leaderWorldPositions.Length;
                    positionIndex++)
                {
                    float leaderWorldX = leaderWorldPositions[positionIndex];
                    float correction = FormationBoundaryMath
                        .CalculateHorizontalFormationCorrection(
                            followerCount,
                            leaderWorldX,
                            localMinX,
                            localMaxX,
                            TrackHalfWidth,
                            UnitRadius,
                            Padding);

                    for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
                    {
                        float targetWorldX = leaderWorldX
                            + slots[slotIndex].LocalPosition.x
                            + correction;
                        Assert.That(
                            targetWorldX,
                            Is.InRange(
                                -UsableHalfWidth - Tolerance,
                                UsableHalfWidth + Tolerance),
                            $"{formationType}, total {totalCombatantCounts[countIndex]}, "
                            + $"leader X {leaderWorldX}, slot {slotIndex}");
                    }
                }
            }
        }

        private static float UsableHalfWidth =>
            TrackHalfWidth - UnitRadius - Padding;

        private static float CalculateCorrection(
            float leaderWorldX,
            float localMinX,
            float localMaxX)
        {
            return FormationBoundaryMath.CalculateHorizontalFormationCorrection(
                4,
                leaderWorldX,
                localMinX,
                localMaxX,
                TrackHalfWidth,
                UnitRadius,
                Padding);
        }

        private static void CalculateBounds(
            List<FormationSlot> formationSlots,
            out float localMinX,
            out float localMaxX)
        {
            localMinX = formationSlots[0].LocalPosition.x;
            localMaxX = localMinX;

            for (int index = 1; index < formationSlots.Count; index++)
            {
                float localX = formationSlots[index].LocalPosition.x;
                localMinX = Mathf.Min(localMinX, localX);
                localMaxX = Mathf.Max(localMaxX, localX);
            }
        }
    }
}
