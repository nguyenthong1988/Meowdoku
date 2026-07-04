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

        public HomeState(GameStateMachine machine, IUIManager ui, BoardView board,
                         IGameSession session, IProfileService profile)
        {
            _machine = machine;
            _ui = ui;
            _board = board;
            _session = session;
            _profile = profile;
        }

        public void Enter()
        {
            _board.ClearBoard();
            _session.Dispose();
            OpenHomeAsync().Forget();
        }

        public void Exit()
        {
        }

        private async UniTaskVoid OpenHomeAsync()
        {
            ViewHome view = null;
            await _ui.PushViewAsync<ViewHome>(UIConst.ViewHome, stack: false, onLoad: (_, v) => view = v);
            if (view == null) return;
            view.Setup(() => _machine.ChangeState<LoadLevelState>(), _profile, _ui);
        }
    }
}
