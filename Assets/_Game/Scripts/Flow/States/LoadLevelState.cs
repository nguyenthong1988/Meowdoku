using CaskFramework.Profile;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{
    public sealed class LoadLevelState : IGameState
    {
        private readonly GameStateMachine _machine;
        private readonly IUIManager _ui;
        private readonly ILevelDataReader _reader;
        private readonly BoardView _board;
        private readonly IGameSession _session;
        private readonly BoardInputHandler _interaction;
        private readonly IBoosterController _boosters;
        private readonly IProfileService _profile;

        private ViewGameplay _gameplayView;

        public LoadLevelState(GameStateMachine machine, IUIManager ui, ILevelDataReader reader,
                              BoardView board, IGameSession session, BoardInputHandler interaction,
                              IBoosterController boosters, IProfileService profile)
        {
            _machine = machine;
            _ui = ui;
            _reader = reader;
            _board = board;
            _session = session;
            _interaction = interaction;
            _boosters = boosters;
            _profile = profile;
        }

        public void Enter()
        {
            LoadAsync().Forget();
        }

        public void Exit()
        {
        }

        private async UniTaskVoid LoadAsync()
        {
            int levelId = _profile.ProgressLevel;

            LevelReadResult read = await _reader.ReadLevelAsync(levelId);
            if (!read.Success)
            {
                Debug.LogError($"[LoadLevelState] Failed to load level {levelId}:\n{read.Validation?.Summary()}");
                _machine.ChangeState<HomeState>();
                return;
            }

            _board.SetVisible(false);

            _session.Setup(read.Level);
            await _board.BuildAsync(read.Level);
            _board.BindRendering(_session);
            _interaction.Bind(_session);

            if (_gameplayView == null)
            {
                await _ui.PushViewAsync<ViewGameplay>(UIConst.ViewGameplay, stack: false, onLoad: (_, view) => _gameplayView = view);
            }

            BindGameplayView();
        }

        private void BindGameplayView()
        {
            _gameplayView.Bind(
                _session,
                _boosters,
                _board,
                _interaction,
                _ui,
                onHomeRequested: () => _machine.ChangeState<HomeState>(),
                onRetryRequested: () => _machine.ChangeState<LoadLevelState>(),
                onEntryPositioned: () => _machine.ChangeState<RevealState>());
        }
    }
}
