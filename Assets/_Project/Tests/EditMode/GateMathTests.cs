using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.Gates;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class GateMathTests
    {
        [Test]
        public void Charge_CrossesNegativeZeroPositive_AndStopsAtMaximum()
        {
            int value = -50;
            value = GateMath.Charge(value, 50);
            Assert.That(value, Is.EqualTo(-49));
            for (int index = 1; index < 10; index++)
            {
                value = GateMath.Charge(value, 50);
            }

            Assert.That(value, Is.EqualTo(-40));
            for (int index = 10; index < 50; index++)
            {
                value = GateMath.Charge(value, 50);
            }

            Assert.That(value, Is.Zero);
            Assert.That(GateMath.Charge(value, 50), Is.EqualTo(1));
            for (int index = 0; index < 50; index++)
            {
                value = GateMath.Charge(value, 50);
            }

            Assert.That(value, Is.EqualTo(50));
            Assert.That(GateMath.Charge(value, 50), Is.EqualTo(50));
        }

        [TestCase(20, -8, 50, 12)]
        [TestCase(20, 0, 50, 20)]
        [TestCase(20, 8, 50, 28)]
        [TestCase(40, 30, 50, 50)]
        [TestCase(5, -50, 50, 1)]
        public void AdditiveGate_ClampsToNonlethalSquadLimits(
            int before, int value, int max, int expected)
        {
            Assert.That(GateMath.ApplyArithmetic(before,
                GateArithmeticOperation.Add, value, max), Is.EqualTo(expected));
        }

        [Test]
        public void Divide_UsesFloorAndNeverDropsBelowOne()
        {
            Assert.That(GateMath.ApplyArithmetic(21,
                GateArithmeticOperation.Divide, 2, 50), Is.EqualTo(10));
            Assert.That(GateMath.ApplyArithmetic(1,
                GateArithmeticOperation.Divide, 2, 50), Is.EqualTo(1));
            Assert.That(GateMath.ApplyArithmetic(20,
                GateArithmeticOperation.Subtract, 8, 50), Is.EqualTo(12));
            Assert.That(GateMath.ApplyArithmetic(20,
                GateArithmeticOperation.Multiply, 3, 50), Is.EqualTo(50));
        }

        [Test]
        public void SelectionPlaneAndCenterTie_AreDeterministic()
        {
            Assert.That(GateMath.CrossedSelectionPlane(4f, -4f, 0f), Is.True);
            Assert.That(GateMath.CrossedSelectionPlane(4f, 2f, 0f), Is.False);
            Assert.That(GateMath.SelectNearestLane(0f, -2f, 2f), Is.Zero);
            Assert.That(GateMath.SelectNearestLane(1f, -2f, 2f), Is.EqualTo(1));
        }
    }
}
