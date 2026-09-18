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
        public void Compact_FillsCenterThenSymmetricPair()
        {
            Calculate(FormationType.Compact, 3, 3.7f);

            Assert.That(slots[0].LocalPosition.x, Is.Zero.Within(0.0001f));
            Assert.That(
                slots[1].LocalPosition.x,
                Is.EqualTo(-slots[2].LocalPosition.x).Within(0.0001f));
            Assert.That(
                slots[0].LocalPosition.z,
                Is.EqualTo(slots[2].LocalPosition.z).Within(0.0001f));
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
                5,
                maximumHalfWidth,
                slots);
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
