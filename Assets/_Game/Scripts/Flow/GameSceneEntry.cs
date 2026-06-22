using CaskFramework.Assets;
using CaskFramework.Core;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cast.Game.Board;
using Cast.Game.Booster;
using Cast.Game.Gameplay;
using Cast.Game.Level;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game.Flow
{

    public sealed class GameSceneEntry : MonoBehaviour
    {
        [SerializeField] private BoardView _boardView;
        [SerializeField] private BoardInputReader _input;
        [SerializeField] private GameSessionConfig _config = new GameSessionConfig();

        private GameFlow _flow;

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
            IUIManager ui = GameRuntime.Get<IUIManager>();
            IProfileService profile = GameRuntime.Get<IProfileService>();

            _boardView.Configure(assets);
            await _boardView.PreloadAsync();   

            var interaction = new BoardInputHandler(_input, _boardView);

            var parser = new LevelParser();
            var reader = new LevelDataReader(assets, parser);
            var hints = new HintProvider();
            var session = new GameSession(hints, _config);

            var inventory = new ProfileBoosterInventory(profile);
            var boosters = new BoosterService(
                session, interaction, interaction, _boardView, inventory,
                new HintBooster(), new RevealCellBooster(), new AddHeartBooster());

            _flow = new GameFlow(reader, session, _boardView, interaction, ui, boosters, profile);
            await _flow.RunAsync();
        }
    }
}
