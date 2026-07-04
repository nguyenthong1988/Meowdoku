using System;
using System.Collections.Generic;

namespace Cast.Game
{
    public sealed class GameStateMachine
    {
        private readonly Dictionary<Type, IGameState> _states = new Dictionary<Type, IGameState>();

        private bool _transitioning;
        private Type _pendingState;
        private Action _pendingBeforeEnter;

        public IGameState Current { get; private set; }

        public GameStateMachine(params IGameState[] states)
        {
            if (states == null) return;
            foreach (IGameState state in states)
                if (state != null) Register(state);
        }

        public void Register(IGameState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            _states[state.GetType()] = state;
        }

        public void ChangeState<T>() where T : IGameState
        {
            ChangeState<T>(null);
        }

        public void ChangeState<T>(Action<T> beforeEnter) where T : IGameState
        {
            Action apply = beforeEnter == null
                ? (Action)null
                : () =>
                {
                    if (_states.TryGetValue(typeof(T), out IGameState registered) && registered is T typed)
                        beforeEnter(typed);
                };

            if (_transitioning)
            {
                _pendingState = typeof(T);
                _pendingBeforeEnter = apply;
                return;
            }

            _pendingState = typeof(T);
            _pendingBeforeEnter = apply;
            while (_pendingState != null)
            {
                Type next = _pendingState;
                Action prepare = _pendingBeforeEnter;
                _pendingState = null;
                _pendingBeforeEnter = null;

                if (!_states.TryGetValue(next, out IGameState state))
                    throw new InvalidOperationException($"State not registered: {next.Name}");

                _transitioning = true;
                Current?.Exit();
                Current = state;
                prepare?.Invoke();
                Current.Enter();
                _transitioning = false;
            }
        }
    }
}
