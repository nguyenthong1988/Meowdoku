using CaskFramework.Assets;
using CaskFramework.Core;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{
    public sealed class GameSceneEntry : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private BoardInputReader _input;
        [SerializeField] private TutorialController _tutorial;
        [SerializeField] private GameSessionConfig _config = new GameSessionConfig();

        private GameStateMachine _machine;

        public bool IsInitialized => _machine != null;

        private void Start()
        {
            BootAsync().Forget();
        }

        private async UniTaskVoid BootAsync()
        {
            await UniTask.WaitUntil(() =>
                GameRuntime.IsRegistered<IAssetManager>() &&
                GameRuntime.IsRegistered<IUIManager>() &&
                GameRuntime.IsRegistered<IProfileService>());

            IAssetManager assets = GameRuntime.Get<IAssetManager>();
            IUIManager uiManager = GameRuntime.Get<IUIManager>();
            IProfileService profile = GameRuntime.Get<IProfileService>();

            _boardView.Configure(assets);
            await _boardView.PreloadAsync();

            var interaction = new BoardInputHandler(_input, _boardView);

            var parser = new LevelParser();
            var reader = new LevelDataReader(assets, parser);
            var session = new GameSession(_config, profile);

            var inventory = new ProfileBoosterInventory(profile);
            var boosters = new BoosterController(
                session, interaction, interaction, _boardView, inventory, uiManager,
                new SmartHintBooster(), new RevealBooster(),
                new UndoBooster(), new ClearHintsBooster());

            _machine = new GameStateMachine();

            _machine.Register(new HomeState(_machine, uiManager, _boardView, session, profile));
            _machine.Register(new LoadLevelState(_machine, uiManager, reader, _boardView, session, interaction, boosters, profile));
            _machine.Register(new RevealState(_machine, _boardView, profile));
            _machine.Register(new FtueState(_machine, session, _boardView, interaction, profile, _tutorial));
            _machine.Register(new PlayState(_machine, session));
            _machine.Register(new WinState(_machine, uiManager, _boardView, profile));
            _machine.Register(new LoseState(_machine, uiManager, _boardView));
        }

        public void StartInHome()
        {
            if (_machine == null) return;
            _machine.ChangeState<HomeState>();
        }

        public void StartInLevel()
        {
            if (_machine == null) return;
            _machine.ChangeState<LoadLevelState>();
        }
    }
}
