using CaskFramework.Assets;
using CaskFramework.Config;
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
        [SerializeField] private ThemeFeature _theme;
        [SerializeField] private GameSessionConfig _config = new GameSessionConfig();
        [SerializeField] private AdRule _adRule = new AdRule();

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
                GameRuntime.IsRegistered<IProfileService>() &&
                GameRuntime.IsRegistered<IConfigService>());

            IAssetManager assets = GameRuntime.Get<IAssetManager>();
            IUIManager uiManager = GameRuntime.Get<IUIManager>();
            IProfileService profile = GameRuntime.Get<IProfileService>();

            await RegisterConfigsAsync(GameRuntime.Get<IConfigService>());

            var adFeature = new AdFeature(_adRule);
            var dailyChallenge = new DailyChallengeFeature();
            FeatureManager.Add(adFeature);
            FeatureManager.Add(dailyChallenge);
            FeatureManager.Add(_theme);

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

            var gameplayViewRef = new GameplayViewRef();

            _machine = new GameStateMachine();

            _machine.Register(new HomeState(_machine, uiManager, _boardView, session, profile, adFeature));
            _machine.Register(new LoadLevelState(_machine, uiManager, reader, _boardView, session, interaction, boosters, profile, gameplayViewRef, adFeature, dailyChallenge));
            _machine.Register(new RevealState(_machine, _boardView, profile));
            _machine.Register(new FtueState(_machine, session, _boardView, interaction, profile, _tutorial, uiManager, gameplayViewRef));
            _machine.Register(new PlayState(_machine, session, adFeature));
            _machine.Register(new WinState(_machine, uiManager, _boardView, profile, dailyChallenge, _theme, adFeature));
            _machine.Register(new LoseState(_machine, uiManager, _boardView, session, adFeature));
        }

        private static async UniTask RegisterConfigsAsync(IConfigService configService)
        {
            try
            {
                await configService.RegisterJsonConfigAsync<ThemeConfig>(ThemeConfig.AddressableKey);
            }
            catch (System.Exception err)
            {
                Debug.LogWarning($"[GameSceneEntry] Failed to load ThemeConfig '{ThemeConfig.AddressableKey}': {err.Message}");
            }
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
