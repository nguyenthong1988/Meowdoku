using CaskFramework.Ads;
using CaskFramework.Core;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class LoseState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly IUIManager _ui;
        private readonly BoardView _board;
        private readonly IGameSession _session;
        private readonly AdFeature _ads;

        private GameResult _result;
        private GameMode _gameMode = GameMode.Normal;

        public LoseState(GameStateMachine machine, IUIManager ui, BoardView board,
                         IGameSession session, AdFeature ads)
        {
            _machine = machine;
            _ui = ui;
            _board = board;
            _session = session;
            _ads = ads;
        }

        public void SetResult(GameResult result)
        {
            _result = result;
        }

        public void SetGameMode(GameMode mode)
        {
            _gameMode = mode;
        }

        public void Enter()
        {
            _ads.HideBanner();

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

            if (choice == LoseChoice.Revive)
            {
                HandleReviveAsync().Forget();
                return;
            }

            GameMode mode = _gameMode;
            ClosePopupThen(() => _machine.ChangeState<LoadLevelState>(s =>
            {
                s.SetEntryType(AdPosition.NormalRestart);
                s.SetGameMode(mode);
            })).Forget();
        }

        private async UniTaskVoid HandleReviveAsync()
        {
            if (_ads.IsRewardRequired)
            {
                if (!await _ads.ShowRewardedAsync())
                    return;
            }

            _session.Revive();
            await _ui.PopPopupAsync();
            _board.SetVisible(true);
            GameMode mode = _gameMode;
            _machine.ChangeState<PlayState>(s => s.SetGameMode(mode));
        }

        private async UniTaskVoid ClosePopupThen(System.Action next)
        {
            await _ui.PopPopupAsync();
            next();
        }
    }
}
