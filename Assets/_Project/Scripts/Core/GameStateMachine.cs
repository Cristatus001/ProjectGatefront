using System;

namespace ProjectGatefront.Core
{
    public sealed class GameStateMachine
    {
        public GameStateMachine(GameState initialState)
        {
            CurrentState = initialState;
        }

        public event Action<GameState, GameState> StateChanged;

        public GameState CurrentState { get; private set; }

        public bool TransitionTo(GameState nextState)
        {
            if (nextState == CurrentState)
            {
                return false;
            }

            GameState previousState = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previousState, nextState);
            return true;
        }
    }
}
