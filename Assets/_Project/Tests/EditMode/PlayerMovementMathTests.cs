using NUnit.Framework;
using ProjectGatefront.Gameplay.Player;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class PlayerMovementMathTests
    {
        [Test]
        public void ClampHorizontal_CannotExceedRightBoundary()
        {
            Assert.That(PlayerMovementMath.ClampHorizontal(8f, 4f), Is.EqualTo(4f));
        }

        [Test]
        public void ClampHorizontal_CannotExceedLeftBoundary()
        {
            Assert.That(PlayerMovementMath.ClampHorizontal(-8f, 4f), Is.EqualTo(-4f));
        }

        [Test]
        public void CalculateLateralTarget_ZeroDragDoesNotMoveTarget()
        {
            Assert.That(
                PlayerMovementMath.CalculateLateralTarget(1f, 0f, 10f, 4f),
                Is.EqualTo(1f));
        }

        [Test]
        public void CalculateLateralTarget_PositiveDragMovesRight()
        {
            Assert.That(
                PlayerMovementMath.CalculateLateralTarget(0f, 0.1f, 10f, 4f),
                Is.GreaterThan(0f));
        }

        [Test]
        public void CalculateLateralTarget_NegativeDragMovesLeft()
        {
            Assert.That(
                PlayerMovementMath.CalculateLateralTarget(0f, -0.1f, 10f, 4f),
                Is.LessThan(0f));
        }

        [Test]
        public void CalculateLateralTarget_SensitivityScalesMovement()
        {
            float lowSensitivity = PlayerMovementMath.CalculateLateralTarget(
                0f,
                0.1f,
                5f,
                10f);
            float highSensitivity = PlayerMovementMath.CalculateLateralTarget(
                0f,
                0.1f,
                10f,
                10f);

            Assert.That(highSensitivity, Is.EqualTo(lowSensitivity * 2f));
        }

        [Test]
        public void CalculateUsableTrackHalfWidth_AccountsForBodyAndPadding()
        {
            Assert.That(
                PlayerMovementMath.CalculateUsableTrackHalfWidth(4.25f, 0.42f, 0.08f),
                Is.EqualTo(3.75f).Within(0.0001f));
        }

        [Test]
        public void ClampHorizontal_AsymmetricRangeUsesFormationLimits()
        {
            Assert.That(
                PlayerMovementMath.ClampHorizontal(5f, -1.5f, 2.25f),
                Is.EqualTo(2.25f));
            Assert.That(
                PlayerMovementMath.ClampHorizontal(-5f, -1.5f, 2.25f),
                Is.EqualTo(-1.5f));
        }

        [Test]
        public void CalculateLateralTarget_AsymmetricRangeStopsWholeSquadRoot()
        {
            Assert.That(
                PlayerMovementMath.CalculateLateralTarget(
                    2f,
                    1f,
                    10f,
                    -1.5f,
                    2.25f),
                Is.EqualTo(2.25f));
        }
    }
}
