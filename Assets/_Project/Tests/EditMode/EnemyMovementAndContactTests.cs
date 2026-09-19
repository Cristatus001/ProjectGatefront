using NUnit.Framework;
using ProjectGatefront.Gameplay.Enemies;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class EnemyMovementAndContactTests
    {
        [Test]
        public void EffectiveApproach_AddsWorldAndEnemySpeeds()
        {
            Assert.That(
                EnemyMovementMath.CalculateEffectiveApproachSpeed(8f, 2f, true),
                Is.EqualTo(10f));
            Assert.That(
                EnemyMovementMath.CalculateApproachDistance(8f, 2f, 0.5f, true),
                Is.EqualTo(5f));
        }

        [Test]
        public void EffectiveApproach_PausesAndSanitizesInvalidSpeeds()
        {
            Assert.That(
                EnemyMovementMath.CalculateEffectiveApproachSpeed(8f, 2f, false),
                Is.Zero);
            Assert.That(
                EnemyMovementMath.CalculateEffectiveApproachSpeed(
                    float.NaN,
                    float.PositiveInfinity,
                    true),
                Is.Zero);
            Assert.That(
                EnemyMovementMath.CalculateApproachDistance(8f, 2f, -1f, true),
                Is.Zero);
        }

        [Test]
        public void Contact_RequiresLineAndHorizontalOverlapIncludingRadius()
        {
            Assert.That(
                EnemyContactMath.HasReachedContactLine(3f, 0.5f, 2.5f),
                Is.True);
            Assert.That(
                EnemyContactMath.HasReachedContactLine(3.1f, 0.5f, 2.5f),
                Is.False);
            Assert.That(
                EnemyContactMath.OverlapsSquadHorizontally(
                    0f,
                    0.5f,
                    -1f,
                    1f,
                    0.1f),
                Is.True);
            Assert.That(
                EnemyContactMath.OverlapsSquadHorizontally(
                    -1.55f,
                    0.5f,
                    -1f,
                    1f,
                    0.1f),
                Is.True);
            Assert.That(
                EnemyContactMath.OverlapsSquadHorizontally(
                    -2f,
                    0.5f,
                    -1f,
                    1f,
                    0.1f),
                Is.False);
            Assert.That(
                EnemyContactMath.OverlapsSquadHorizontally(
                    2f,
                    0.5f,
                    -1f,
                    1f,
                    0.1f),
                Is.False);
        }
    }
}
