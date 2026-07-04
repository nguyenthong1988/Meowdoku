namespace Cast.Game
{
    public sealed class PlayState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly IGameSession _session;

        public PlayState(GameStateMachine machine, IGameSession session)
        {
            _machine = machine;
            _session = session;
        }

        public void Enter()
        {
            _session.Ended += OnEnded;
            if (_session.Phase != GamePhase.Playing)
                _session.Begin();
        }

        public void Exit()
        {
            _session.Ended -= OnEnded;
        }

        private void OnEnded(GameResult result)
        {
            if (result.Won)
                _machine.ChangeState<WinState>(s => s.SetResult(result));
            else
                _machine.ChangeState<LoseState>(s => s.SetResult(result));
        }
    }
}
