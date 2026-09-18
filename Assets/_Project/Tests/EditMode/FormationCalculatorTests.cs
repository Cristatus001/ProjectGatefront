using System.Collections.Generic;
using NUnit.Framework;
using ProjectGatefront.Gameplay.Squad;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class FormationCalculatorTests
    {
        private readonly List<FormationSlot> slots = new List<FormationSlot>();

        [TestCase(FormationType.Compact)]
        [TestCase(FormationType.Line)]
        [TestCase(FormationType.Wedge)]
        [TestCase(FormationType.Column)]
        [TestCase(FormationType.Spread)]
        public void Calculate_ReturnsRequestedSlotCount(FormationType formationType)
        {
            Calculate(formationType, 17, 3.7f);

            Assert.That(slots.Count, Is.EqualTo(17));
        }

        [Test]
        public void Calculate_ZeroCompactSlotsReturnsEmptyCollection()
        {
            Calculate(FormationType.Compact, 0, 3.7f);

            Assert.That(slots, Is.Empty);
        }

        [TestCase(FormationType.Compact)]
        [TestCase(FormationType.Line)]
        [TestCase(FormationType.Wedge)]
        [TestCase(FormationType.Column)]
        [TestCase(FormationType.Spread)]
        public void Calculate_OneSlotIsSafe(FormationType formationType)
        {
            Calculate(formationType, 1, 3.7f);

            Assert.That(slots.Count, Is.EqualTo(1));
            Assert.That(IsFinite(slots[0].LocalPosition), Is.True);
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
                    Is.LessThanOrEqualTo(maximumHalfWidth + 0.0001f));
            }
        }

        [TestCase(FormationType.Compact)]
        [TestCase(FormationType.Line)]
        [TestCase(FormationType.Wedge)]
        [TestCase(FormationType.Column)]
        [TestCase(FormationType.Spread)]
        public void Calculate_ProducesOnlyFinitePositions(FormationType formationType)
        {
            Calculate(formationType, 50, 3.7f);

            for (int index = 0; index < slots.Count; index++)
            {
                Assert.That(IsFinite(slots[index].LocalPosition), Is.True);
            }
        }

        [TestCase(1, 0)]
        [TestCase(2, 1)]
        [TestCase(5, 4)]
        [TestCase(10, 9)]
        [TestCase(20, 19)]
        [TestCase(50, 49)]
        public void Compact_ReturnsExpectedFollowerCount(
            int totalCombatants,
            int expectedFollowers)
        {
            Calculate(FormationType.Compact, totalCombatants - 1, 3.7f);

            Assert.That(slots.Count, Is.EqualTo(expectedFollowers));
        }

        [Test]
        public void Compact_FirstFollowerAppearsBesideLeader()
        {
            Calculate(FormationType.Compact, 1, 3.7f);

            Assert.That(
                Mathf.Abs(slots[0].LocalPosition.x),
                Is.GreaterThanOrEqualTo(1f));
            Assert.That(
                Mathf.Abs(slots[0].LocalPosition.z),
                Is.LessThan(0.4f));
        }

        [Test]
        public void Compact_FirstPairFlanksLeaderSymmetrically()
        {
            Calculate(FormationType.Compact, 2, 3.7f);

            Assert.That(
                slots[0].LocalPosition.x,
                Is.EqualTo(-slots[1].LocalPosition.x).Within(0.0001f));
            Assert.That(
                slots[0].LocalPosition.z,
                Is.EqualTo(slots[1].LocalPosition.z).Within(0.0001f));
        }

        [Test]
        public void Compact_FirstSixFollowersUseSideOrDiagonalPositions()
        {
            Calculate(FormationType.Compact, 6, 3.7f);

            for (int index = 0; index < slots.Count; index++)
            {
                Assert.That(
                    Mathf.Abs(slots[index].LocalPosition.x),
                    Is.GreaterThan(0.5f));
            }
        }

        [Test]
        public void Compact_IsDeterministicAcrossRecalculation()
        {
            Calculate(FormationType.Compact, 49, 3.7f);
            var firstCalculation = new List<Vector3>(slots.Count);
            for (int index = 0; index < slots.Count; index++)
            {
                firstCalculation.Add(slots[index].LocalPosition);
            }

            Calculate(FormationType.Compact, 49, 3.7f);

            for (int index = 0; index < slots.Count; index++)
            {
                Assert.That(
                    slots[index].LocalPosition,
                    Is.EqualTo(firstCalculation[index]));
            }
        }

        [TestCase(9)]
        [TestCase(19)]
        [TestCase(49)]
        public void Compact_IsWiderThanDeepAtMeaningfulFollowerCounts(
            int followerCount)
        {
            Calculate(FormationType.Compact, followerCount, 3.7f);
            CalculateBounds(
                out float minimumX,
                out float maximumX,
                out float minimumZ,
                out float maximumZ);

            Assert.That(
                maximumX - minimumX,
                Is.GreaterThan(maximumZ - minimumZ));
        }

        [TestCase(4)]
        [TestCase(9)]
        [TestCase(19)]
        [TestCase(49)]
        public void Compact_KeySquadSizesRemainHorizontallyBalanced(
            int followerCount)
        {
            Calculate(FormationType.Compact, followerCount, 3.7f);
            float horizontalSum = 0f;
            for (int index = 0; index < slots.Count; index++)
            {
                horizontalSum += slots[index].LocalPosition.x;
            }

            Assert.That(horizontalSum, Is.Zero.Within(0.0001f));
        }

        [Test]
        public void Compact_MaintainsReadableMinimumSpacing()
        {
            Calculate(FormationType.Compact, 49, 3.7f);

            for (int first = 0; first < slots.Count; first++)
            {
                for (int second = first + 1; second < slots.Count; second++)
                {
                    float distance = Vector3.Distance(
                        slots[first].LocalPosition,
                        slots[second].LocalPosition);
                    Assert.That(
                        distance,
                        Is.GreaterThanOrEqualTo(0.7f),
                        $"Slots {first} and {second} are too close.");
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
                1.15f,
                0.72f,
                5,
                maximumHalfWidth,
                slots);
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
