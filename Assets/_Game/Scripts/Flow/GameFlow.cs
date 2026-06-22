using System.Threading;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cast.Game.Board;
using Cast.Game.Booster;
using Cast.Game.Gameplay;
using Cast.Game.Level;
using Cast.Game.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game.Flow
{

    public sealed class GameFlow
    {
        private const string ViewHomeName = "ViewHome";
        private const string ViewGameplayName = "ViewGameplay";
        private const string PopupWinName = "PopupWin";
        private const string PopupLoseName = "PopupLose";

        private readonly ILevelDataReader _reader;
        private readonly IGameSession _session;
        private readonly BoardView _board;
        private readonly BoardInputHandler _interaction;
        private readonly IUIManager _ui;
        private readonly IBoosterService _boosters;
        private readonly IProfileService _profile;

        private CancellationTokenSource _cts;

        public int CurrentLevelId => _profile.ProgressLevel;

        public GameFlow(ILevelDataReader reader, IGameSession session, BoardView board,
                        BoardInputHandler interaction, IUIManager ui, IBoosterService boosters,
                        IProfileService profile)
        {
            _reader = reader;
            _session = session;
            _board = board;
            _interaction = interaction;
            _ui = ui;
            _boosters = boosters;
            _profile = profile;
        }

        public async UniTask RunAsync()
        {
            _board.SetVisible(false);
            bool skipHome = _profile.ProgressLevel <= 1;
            while (true)
            {
                if (!skipHome)
                    await ShowHomeUntilPlayAsync();
                skipHome = false;

                await PlayLevelsAsync();
            }
        }

        private async UniTask ShowHomeUntilPlayAsync()
        {
            ViewHome home = null;
            await _ui.PushViewAsync<ViewHome>(
                ViewHomeName,
                stack: false,
                onLoad: (_, v) => home = v);

            if (home == null) return;

            while (await home.WaitForChoiceAsync(_profile) != HomeChoice.Play) { }
        }

        private async UniTask PlayLevelsAsync()
        {
            while (true)
            {
                GameResult? played = await PlayCurrentLevelAsync();
                if (played == null) return;
                GameResult result = played.Value;

                _board.SetVisible(false);

                if (result.Won)
                {
                    WinChoice choice = await ShowWinAsync(result);
                    if (choice == WinChoice.Home) return;
                    if (choice == WinChoice.Next) _profile.Advance();
                }
                else
                {
                    LoseChoice choice = await ShowLoseAsync(result);
                    if (choice == LoseChoice.Home) return;
                }
            }
        }

        private async UniTask<GameResult?> PlayCurrentLevelAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            CancellationToken ct = _cts.Token;

            LevelReadResult read = await _reader.ReadLevelAsync(_profile.ProgressLevel);
            if (!read.Success)
            {
                Debug.LogError($"[GameFlow] Failed to load level {_profile.ProgressLevel}:\n{read.Validation?.Summary()}");
                return null;
            }

            _session.Setup(read.Level);
            await _board.BuildAsync(read.Level);
            _board.BindRendering(_session);
            _interaction.Bind(_session);

            await _ui.PushViewAsync<ViewGameplay>(
                ViewGameplayName,
                stack: false,
                onLoad: (_, view) => view.Bind(_session, _boosters));

            await UniTask.WaitUntil(() => _ui.GetActiveTopView() == null);

            _board.SetVisible(true);
            await _board.PlayRevealAsync(ct);

            _session.Begin();
            return await _session.PlayToEndAsync(ct);
        }

        private async UniTask<WinChoice> ShowWinAsync(GameResult result)
        {
            PopupWin popup = null;
            await _ui.PushPopupAsync<PopupWin>(
                PopupWinName,
                onLoad: (_, p) => popup = p);
            WinChoice choice = popup != null ? await popup.WaitForChoiceAsync(result) : WinChoice.Home;
            await _ui.PopPopupAsync();
            return choice;
        }

        private async UniTask<LoseChoice> ShowLoseAsync(GameResult result)
        {
            PopupLose popup = null;
            await _ui.PushPopupAsync<PopupLose>(
                PopupLoseName,
                onLoad: (_, p) => popup = p);
            LoseChoice choice = popup != null ? await popup.WaitForChoiceAsync(result) : LoseChoice.Home;
            await _ui.PopPopupAsync();
            return choice;
        }
    }
}
