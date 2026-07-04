using System;
using System.Threading;
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

        public RevealState(GameStateMachine machine, BoardView board, IProfileService profile)
        {
            _machine = machine;
            _board = board;
            _profile = profile;
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

            if (_profile.ShouldRunFtue())
                _machine.ChangeState<FtueState>();
            else
                _machine.ChangeState<PlayState>();
        }
    }
}
