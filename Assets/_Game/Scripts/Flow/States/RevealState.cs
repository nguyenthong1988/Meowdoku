using System;
using System.Threading;
using CaskFramework.Audio;
using CaskFramework.Core;
using CaskFramework.Profile;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class RevealState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly BoardView _board;
        private readonly IProfileService _profile;
        private CancellationTokenSource _cts;
        private GameMode _gameMode = GameMode.Normal;

        public RevealState(GameStateMachine machine, BoardView board, IProfileService profile)
        {
            _machine = machine;
            _board = board;
            _profile = profile;
        }

        public void SetGameMode(GameMode mode)
        {
            _gameMode = mode;
        }

        public void Enter()
        {
            _cts = new CancellationTokenSource();
            PlayRevealAsync(_cts.Token).Forget();
        }

        public void Exit()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private async UniTaskVoid PlayRevealAsync(CancellationToken ct)
        {
            _board.SetVisible(true);
            try
            {
                await _board.PlayRevealAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested) return;

            GameMode mode = _gameMode;
            _gameMode = GameMode.Normal;

            if (_profile.ShouldRunFtue())
                _machine.ChangeState<FtueState>();
            else
                _machine.ChangeState<PlayState>(s => s.SetGameMode(mode));
        }
    }
}
