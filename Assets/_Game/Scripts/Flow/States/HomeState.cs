using CaskFramework.Ads;
using CaskFramework.Core;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class HomeState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly IUIManager _ui;
        private readonly BoardView _board;
        private readonly IGameSession _session;
        private readonly IProfileService _profile;
        private readonly AdFeature _ads;

        public HomeState(GameStateMachine machine, IUIManager ui, BoardView board,
                         IGameSession session, IProfileService profile, AdFeature ads)
        {
            _machine = machine;
            _ui = ui;
            _board = board;
            _session = session;
            _profile = profile;
            _ads = ads;
        }

        public void Enter()
        {
            _ads.HideBanner();

            _board.ClearBoard();
            _session.Dispose();
            OpenHomeAsync().Forget();
        }

        public void Exit()
        {
        }

        private async UniTaskVoid OpenHomeAsync()
        {
            if (FeatureManager.TryGet(out ThemeFeature theme))
                theme.ApplyHomeAsync().Forget();

            ViewHome view = null;
            await _ui.PushViewAsync<ViewHome>(UIConst.ViewHome, stack: false, onLoad: (_, v) => view = v);
            if (view == null) return;
            view.Setup(
                () => _machine.ChangeState<LoadLevelState>(s => s.SetEntryType(AdPosition.NormalStart)),
                () => _machine.ChangeState<LoadLevelState>(s =>
                {
                    s.SetEntryType(AdPosition.NormalStart);
                    s.SetGameMode(GameMode.DailyChallenge);
                }),
                _profile,
                _ui);
        }
    }
}
