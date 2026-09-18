using NUnit.Framework;
using ProjectGatefront.Gameplay.World;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class WorldScrollMathTests
    {
        [Test]
        public void AccumulateDistance_IncreasesBySpeedTimesTime()
        {
            float distance = WorldScrollMath.AccumulateDistance(
                10f,
                8f,
                0.5f,
                true);

            Assert.That(distance, Is.EqualTo(14f).Within(0.0001f));
        }

        [Test]
        public void ZeroSpeed_ProducesNoDistanceDelta()
        {
            Assert.That(
                WorldScrollMath.CalculateDistanceDelta(0f, 1f, true),
                Is.Zero);
        }

        [Test]
        public void DisabledScrolling_ProducesNoDistanceDelta()
        {
            Assert.That(
                WorldScrollMath.CalculateDistanceDelta(8f, 1f, false),
                Is.Zero);
        }

        [Test]
        public void RecycleTarget_IsOneExactChunkBeyondFront()
        {
            Assert.That(
                WorldScrollMath.CalculateRecycleTargetZ(62f, 30f),
                Is.EqualTo(92f).Within(0.0001f));
        }

        [Test]
        public void ConsecutiveRecycleTargets_PreserveExactSpacing()
        {
            float firstTarget = WorldScrollMath.CalculateRecycleTargetZ(62f, 30f);
            float secondTarget = WorldScrollMath.CalculateRecycleTargetZ(
                firstTarget,
                30f);

            Assert.That(secondTarget - firstTarget, Is.EqualTo(30f).Within(0.0001f));
        }

        [TestCase(-8f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void InvalidOrNegativeSpeed_IsSafelyClamped(float speed)
        {
            Assert.That(WorldScrollMath.SanitizeSpeed(speed), Is.Zero);
        }
    }
}
