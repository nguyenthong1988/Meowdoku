using System;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class WinState : IGameState
    {
        private const float LastRevealDuration = 0.25f;

        private readonly GameStateMachine _machine;
        private readonly IUIManager _ui;
        private readonly BoardView _board;
        private readonly IProfileService _profile;

        private GameResult _result;

        public WinState(GameStateMachine machine, IUIManager ui, BoardView board, IProfileService profile)
        {
            _machine = machine;
            _ui = ui;
            _board = board;
            _profile = profile;
        }

        public void SetResult(GameResult result)
        {
            _result = result;
        }

        public void Enter()
        {
            OpenResultAsync().Forget();
        }

        public void Exit()
        {
        }

        private async UniTaskVoid OpenResultAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(LastRevealDuration));
            _board.SetVisible(false);

            int levelId = _profile.ProgressLevel;
            ViewResult view = null;
            await _ui.PushViewAsync<ViewResult>(UIConst.ViewResult, stack: false, onLoad: (_, v) => view = v);
            if (view == null)
            {
                _machine.ChangeState<HomeState>();
                return;
            }
            view.Setup(_result, levelId, OnChoice);
        }

        private void OnChoice(WinChoice choice)
        {
            if (choice == WinChoice.Home)
            {
                _machine.ChangeState<HomeState>();
                return;
            }

            _profile.Advance();
            _machine.ChangeState<LoadLevelState>();
        }
    }
}
