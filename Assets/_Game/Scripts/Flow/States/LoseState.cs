using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class LoseState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly IUIManager _ui;
        private readonly BoardView _board;

        private GameResult _result;

        public LoseState(GameStateMachine machine, IUIManager ui, BoardView board)
        {
            _machine = machine;
            _ui = ui;
            _board = board;
        }

        public void SetResult(GameResult result)
        {
            _result = result;
        }

        public void Enter()
        {
            _board.SetVisible(false);
            OpenLoseAsync().Forget();
        }

        public void Exit()
        {
        }

        private async UniTaskVoid OpenLoseAsync()
        {
            PopupOutOfMove popup = null;
            await _ui.PushPopupAsync<PopupOutOfMove>(UIConst.PopupOutOfMove, onLoad: (_, p) => popup = p);
            if (popup == null)
            {
                _machine.ChangeState<HomeState>();
                return;
            }
            popup.SetChoiceCallback(OnChoice, LoseChoice.Retry);
            popup.Setup(_result);
        }

        private void OnChoice(LoseChoice choice)
        {
            if (choice == LoseChoice.Home)
            {
                ClosePopupThen(() => _machine.ChangeState<HomeState>()).Forget();
                return;
            }

            ClosePopupThen(() => _machine.ChangeState<LoadLevelState>()).Forget();
        }

        private async UniTaskVoid ClosePopupThen(System.Action next)
        {
            await _ui.PopPopupAsync();
            next();
        }
    }
}
