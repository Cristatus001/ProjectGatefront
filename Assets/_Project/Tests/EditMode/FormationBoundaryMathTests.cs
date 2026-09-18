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
        public void CalculateUsableTrackHalfWidth_AccountsForUnitAndPadding()
        {
            float usableHalfWidth = FormationBoundaryMath.CalculateUsableTrackHalfWidth(
                TrackHalfWidth,
                UnitRadius,
                Padding);
            Assert.That(usableHalfWidth, Is.EqualTo(UsableHalfWidth).Within(Tolerance));
        }

        [Test]
        public void CenteredFormation_ProducesSymmetricRootRange()
        {
            CalculateRange(-1.5f, 1.5f, out float minimum, out float maximum);
            Assert.That(minimum, Is.EqualTo(-maximum).Within(Tolerance));
            Assert.That(minimum, Is.EqualTo(-2.33f).Within(Tolerance));
        }

        [Test]
        public void AsymmetricFormationRange_AccountsForBothExtents()
        {
            CalculateRange(-2f, 1f, out float minimum, out float maximum);
            Assert.That(minimum, Is.EqualTo(-1.83f).Within(Tolerance));
            Assert.That(maximum, Is.EqualTo(2.83f).Within(Tolerance));
        }

        [Test]
        public void FormationWiderThanTrack_CollapsesToCenteredRootPosition()
        {
            CalculateRange(-5f, 5f, out float minimum, out float maximum);
            Assert.That(minimum, Is.EqualTo(maximum).Within(Tolerance));
            Assert.That(minimum, Is.Zero.Within(Tolerance));
        }

        [Test]
        public void NonFiniteInputs_ReturnFiniteRange()
        {
            FormationBoundaryMath.CalculateRootHorizontalRange(
                float.NaN,
                float.PositiveInfinity,
                float.NaN,
                float.NegativeInfinity,
                float.NaN,
                out float minimum,
                out float maximum);
            Assert.That(float.IsNaN(minimum), Is.False);
            Assert.That(float.IsInfinity(minimum), Is.False);
            Assert.That(float.IsNaN(maximum), Is.False);
            Assert.That(float.IsInfinity(maximum), Is.False);
        }

        [TestCase(FormationType.Compact)]
        [TestCase(FormationType.Line)]
        [TestCase(FormationType.Wedge)]
        [TestCase(FormationType.Column)]
        [TestCase(FormationType.Spread)]
        public void FullLeftAndRightRootLimits_ContainEveryVisibleUnit(
            FormationType formationType)
        {
            int[] visibleUnitCounts = { 5, 10, 20, 50 };
            for (int countIndex = 0;
                countIndex < visibleUnitCounts.Length;
                countIndex++)
            {
                int visibleUnitCount = visibleUnitCounts[countIndex];
                CalculateFormation(formationType, visibleUnitCount);
                CalculateBounds(out float localMinimum, out float localMaximum);
                CalculateRange(
                    localMinimum,
                    localMaximum,
                    out float minimumRoot,
                    out float maximumRoot);
                AssertSlotsInsideRoad(
                    formationType,
                    visibleUnitCount,
                    minimumRoot,
                    "left");
                AssertSlotsInsideRoad(
                    formationType,
                    visibleUnitCount,
                    maximumRoot,
                    "right");
            }
        }

        [Test]
        public void RootClamp_DoesNotChangeRelativeFormationSpacing()
        {
            CalculateFormation(FormationType.Compact, 20);
            CalculateBounds(out float localMinimum, out float localMaximum);
            CalculateRange(
                localMinimum,
                localMaximum,
                out float minimumRoot,
                out float maximumRoot);

            float centeredSpacing = slots[1].LocalPosition.x
                - slots[0].LocalPosition.x;
            float leftSpacing = (minimumRoot + slots[1].LocalPosition.x)
                - (minimumRoot + slots[0].LocalPosition.x);
            float rightSpacing = (maximumRoot + slots[1].LocalPosition.x)
                - (maximumRoot + slots[0].LocalPosition.x);
            Assert.That(leftSpacing, Is.EqualTo(centeredSpacing).Within(Tolerance));
            Assert.That(rightSpacing, Is.EqualTo(centeredSpacing).Within(Tolerance));
        }

        private static float UsableHalfWidth =>
            TrackHalfWidth - UnitRadius - Padding;

        private static void CalculateRange(
            float localMinimum,
            float localMaximum,
            out float minimumRoot,
            out float maximumRoot)
        {
            FormationBoundaryMath.CalculateRootHorizontalRange(
                localMinimum,
                localMaximum,
                TrackHalfWidth,
                UnitRadius,
                Padding,
                out minimumRoot,
                out maximumRoot);
        }

        private void CalculateFormation(
            FormationType formationType,
            int visibleUnitCount)
        {
            FormationCalculator.Calculate(
                formationType,
                visibleUnitCount,
                1.35f,
                1.1f,
                1.6f,
                0.84f,
                0.7f,
                5,
                Mathf.Min(3.7f, UsableHalfWidth),
                slots);
        }

        private void CalculateBounds(out float localMinimum, out float localMaximum)
        {
            localMinimum = slots[0].LocalPosition.x;
            localMaximum = localMinimum;
            for (int index = 1; index < slots.Count; index++)
            {
                localMinimum = Mathf.Min(localMinimum, slots[index].LocalPosition.x);
                localMaximum = Mathf.Max(localMaximum, slots[index].LocalPosition.x);
            }
        }

        private void AssertSlotsInsideRoad(
            FormationType formationType,
            int visibleUnitCount,
            float rootX,
            string edge)
        {
            for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
            {
                float worldX = rootX + slots[slotIndex].LocalPosition.x;
                Assert.That(
                    worldX,
                    Is.InRange(
                        -UsableHalfWidth - Tolerance,
                        UsableHalfWidth + Tolerance),
                    $"{formationType}, {visibleUnitCount} units, {edge} edge, "
                    + $"slot {slotIndex}");
            }
        }
    }
}
