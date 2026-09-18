using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Gameplay.Squad;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class FormationCalculatorTests
    {
        private const float Tolerance = 0.0001f;
        private readonly List<FormationSlot> slots = new List<FormationSlot>();

        [TestCase(FormationType.Compact)]
        [TestCase(FormationType.Line)]
        [TestCase(FormationType.Wedge)]
        [TestCase(FormationType.Column)]
        [TestCase(FormationType.Spread)]
        public void Calculate_ReturnsRequestedVisibleUnitCount(
            FormationType formationType)
        {
            Calculate(formationType, 17, 3.7f);
            Assert.That(slots.Count, Is.EqualTo(17));
        }

        [Test]
        public void Calculate_ZeroSlotsReturnsEmptyCollection()
        {
            Calculate(FormationType.Compact, 0, 3.7f);
            Assert.That(slots, Is.Empty);
        }

        [TestCase(FormationType.Compact)]
        [TestCase(FormationType.Line)]
        [TestCase(FormationType.Wedge)]
        [TestCase(FormationType.Column)]
        [TestCase(FormationType.Spread)]
        public void Calculate_OneVisibleUnitIsSafeAndCentered(
            FormationType formationType)
        {
            Calculate(formationType, 1, 3.7f);
            Assert.That(slots.Count, Is.EqualTo(1));
            Assert.That(slots[0].LocalPosition, Is.EqualTo(Vector3.zero));
        }

        [TestCase(FormationType.Compact)]
        [TestCase(FormationType.Line)]
        [TestCase(FormationType.Wedge)]
        [TestCase(FormationType.Column)]
        [TestCase(FormationType.Spread)]
        public void Calculate_RespectsMaximumFormationHalfWidth(
            FormationType formationType)
        {
            const float maximumHalfWidth = 2.75f;
            Calculate(formationType, 50, maximumHalfWidth);

            for (int index = 0; index < slots.Count; index++)
            {
                Assert.That(
                    Mathf.Abs(slots[index].LocalPosition.x),
                    Is.LessThanOrEqualTo(maximumHalfWidth + Tolerance));
                Assert.That(IsFinite(slots[index].LocalPosition), Is.True);
            }
        }

        [TestCase(FormationType.Compact)]
        [TestCase(FormationType.Line)]
        [TestCase(FormationType.Wedge)]
        [TestCase(FormationType.Column)]
        [TestCase(FormationType.Spread)]
        public void EveryFormation_IsCenteredOnInvisibleRoot(
            FormationType formationType)
        {
            Calculate(formationType, 20, 3.7f);
            Vector3 centroid = CalculateCentroid();
            Assert.That(centroid.x, Is.Zero.Within(Tolerance));
            Assert.That(centroid.z, Is.Zero.Within(Tolerance));
        }

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(10)]
        [TestCase(12)]
        [TestCase(20)]
        [TestCase(50)]
        public void Compact_CountEqualsVisibleSoldierCount(int visibleSoldierCount)
        {
            Calculate(FormationType.Compact, visibleSoldierCount, 3.7f);
            Assert.That(slots.Count, Is.EqualTo(visibleSoldierCount));
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(12)]
        [TestCase(20)]
        [TestCase(50)]
        public void Compact_IsCenteredRoundedBlob(int visibleSoldierCount)
        {
            Calculate(FormationType.Compact, visibleSoldierCount, 3.7f);
            CalculateBounds(
                out float minimumX,
                out float maximumX,
                out float minimumZ,
                out float maximumZ);

            float aspectRatio = (maximumX - minimumX) / (maximumZ - minimumZ);
            Vector3 centroid = CalculateCentroid();
            Assert.That(centroid.x, Is.Zero.Within(Tolerance));
            Assert.That(centroid.z, Is.Zero.Within(Tolerance));
            Assert.That(aspectRatio, Is.InRange(1f, 1.3f));
            Assert.That(minimumZ, Is.LessThan(0f));
            Assert.That(maximumZ, Is.GreaterThan(0f));
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(12)]
        [TestCase(20)]
        [TestCase(50)]
        public void Compact_AssignsSlotsFromCentroidOutward(int visibleSoldierCount)
        {
            Calculate(FormationType.Compact, visibleSoldierCount, 3.7f);

            float previousRadiusSquared = -1f;
            for (int index = 0; index < slots.Count; index++)
            {
                float radiusSquared = slots[index].LocalPosition.sqrMagnitude;
                Assert.That(
                    radiusSquared + Tolerance,
                    Is.GreaterThanOrEqualTo(previousRadiusSquared),
                    $"Slot {index} was assigned before a nearer slot.");
                previousRadiusSquared = radiusSquared;
            }
        }

        [TestCase(5)]
        [TestCase(10)]
        [TestCase(12)]
        [TestCase(20)]
        [TestCase(50)]
        public void Compact_UsesSymmetricPairs(int visibleSoldierCount)
        {
            Calculate(FormationType.Compact, visibleSoldierCount, 3.7f);

            for (int index = 0; index < slots.Count; index++)
            {
                Vector3 position = slots[index].LocalPosition;
                bool foundOpposite = position.sqrMagnitude <= Tolerance;
                for (int candidateIndex = 0;
                    candidateIndex < slots.Count && !foundOpposite;
                    candidateIndex++)
                {
                    foundOpposite = Vector3.Distance(
                        slots[candidateIndex].LocalPosition,
                        -position) <= Tolerance;
                }

                Assert.That(foundOpposite, Is.True, $"Slot {index} has no pair.");
            }
        }

        [Test]
        public void Compact_IsDeterministicAcrossRecalculation()
        {
            Calculate(FormationType.Compact, 50, 3.7f);
            var firstCalculation = new List<Vector3>(slots.Count);
            for (int index = 0; index < slots.Count; index++)
            {
                firstCalculation.Add(slots[index].LocalPosition);
            }

            Calculate(FormationType.Compact, 50, 3.7f);
            for (int index = 0; index < slots.Count; index++)
            {
                Assert.That(slots[index].LocalPosition, Is.EqualTo(firstCalculation[index]));
            }
        }

        [Test]
        public void Compact_MaintainsTightReadableSpacing()
        {
            Calculate(FormationType.Compact, 50, 3.7f);

            for (int first = 0; first < slots.Count; first++)
            {
                for (int second = first + 1; second < slots.Count; second++)
                {
                    float distance = Vector3.Distance(
                        slots[first].LocalPosition,
                        slots[second].LocalPosition);
                    Assert.That(
                        distance,
                        Is.GreaterThanOrEqualTo(0.68f),
                        $"Slots {first} and {second} overlap too heavily.");
                }
            }
        }

        private void Calculate(
            FormationType formationType,
            int slotCount,
            float maximumHalfWidth)
        {
            FormationCalculator.Calculate(
                formationType,
                slotCount,
                1.35f,
                1.1f,
                1.6f,
                0.84f,
                0.7f,
                5,
                maximumHalfWidth,
                slots);
        }

        private Vector3 CalculateCentroid()
        {
            Vector3 centroid = Vector3.zero;
            for (int index = 0; index < slots.Count; index++)
            {
                centroid += slots[index].LocalPosition;
            }

            return centroid / slots.Count;
        }

        private void CalculateBounds(
            out float minimumX,
            out float maximumX,
            out float minimumZ,
            out float maximumZ)
        {
            minimumX = slots[0].LocalPosition.x;
            maximumX = minimumX;
            minimumZ = slots[0].LocalPosition.z;
            maximumZ = minimumZ;

            for (int index = 1; index < slots.Count; index++)
            {
                Vector3 position = slots[index].LocalPosition;
                minimumX = Mathf.Min(minimumX, position.x);
                maximumX = Mathf.Max(maximumX, position.x);
                minimumZ = Mathf.Min(minimumZ, position.z);
                maximumZ = Mathf.Max(maximumZ, position.z);
            }
        }

        private static bool IsFinite(Vector3 position)
        {
            return IsFinite(position.x)
                && IsFinite(position.y)
                && IsFinite(position.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
