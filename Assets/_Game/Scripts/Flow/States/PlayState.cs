using CaskFramework.Core;

namespace Cast.Game
{
    public sealed class PlayState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly IGameSession _session;
        private readonly AdFeature _ads;
        private GameMode _gameMode = GameMode.Normal;

        public PlayState(GameStateMachine machine, IGameSession session, AdFeature ads)
        {
            _machine = machine;
            _session = session;
            _ads = ads;
        }

        public void SetGameMode(GameMode mode)
        {
            _gameMode = mode;
        }

        public void Enter()
        {
            _session.Ended += OnEnded;
            if (_session.Phase != GamePhase.Playing)
                _session.Begin();

            if (_session.Level != null && _ads.ShouldShowBanner(_session.Level.Size))
                _ads.ShowBanner();
        }

        public void Exit()
        {
            _session.Ended -= OnEnded;
        }

        private void OnEnded(GameResult result)
        {
            _ads.IncrementSessionPlayedCount();
            GameMode mode = _gameMode;

            if (result.Won)
                _machine.ChangeState<WinState>(s =>
                {
                    s.SetResult(result);
                    s.SetGameMode(mode);
                });
            else
                _machine.ChangeState<LoseState>(s =>
                {
                    s.SetResult(result);
                    s.SetGameMode(mode);
                });
        }
    }
}
