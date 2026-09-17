using NUnit.Framework;
using ProjectGatefront.Core;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class GameStateMachineTests
    {
        [Test]
        public void TransitionTo_ChangesStateAndRaisesEvent()
        {
            var stateMachine = new GameStateMachine(GameState.Boot);
            GameState observedPrevious = GameState.Boot;
            GameState observedNext = GameState.Boot;

            stateMachine.StateChanged += (previous, next) =>
            {
                observedPrevious = previous;
                observedNext = next;
            };

            bool changed = stateMachine.TransitionTo(GameState.MainMenu);

            Assert.That(changed, Is.True);
            Assert.That(stateMachine.CurrentState, Is.EqualTo(GameState.MainMenu));
            Assert.That(observedPrevious, Is.EqualTo(GameState.Boot));
            Assert.That(observedNext, Is.EqualTo(GameState.MainMenu));
        }

        [Test]
        public void TransitionTo_SameStateDoesNothing()
        {
            var stateMachine = new GameStateMachine(GameState.Boot);

            bool changed = stateMachine.TransitionTo(GameState.Boot);

            Assert.That(changed, Is.False);
        }
    }
}
