using System;
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
        private const string ViewGameplayName = "ViewGameplay";
        private const string PopupCompleteName = "PopupLevelComplete";
        private const string PopupGameOverName = "PopupGameOver";

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

        public async UniTask StartLevelAsync()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            CancellationToken ct = _cts.Token;

            LevelReadResult read = await _reader.ReadLevelAsync(_profile.ProgressLevel);
            if (!read.Success)
            {
                Debug.LogError($"[GameFlow] Failed to load level {_profile.ProgressLevel}:\n{read.Validation?.Summary()}");
                return;
            }

            _session.Setup(read.Level);
            await _board.BuildAsync(read.Level);
            _board.BindRendering(_session);   
            _interaction.Bind(_session);      

            await _ui.PushViewAsync<ViewGameplay>(
                ViewGameplayName,
                stack: false,
                onLoad: (_, view) => view.Bind(_session, _boosters));

            await _board.PlayRevealAsync(ct);

            _session.Begin();
            GameResult result = await _session.PlayToEndAsync(ct);

            ResultChoice choice = await ShowResultAsync(result);
            if (choice == ResultChoice.Next) await NextLevelAsync();
            else if (choice == ResultChoice.Retry) await RetryAsync();
        }

        public UniTask NextLevelAsync()
        {
            _profile.Advance(); 
            return StartLevelAsync();
        }

        public UniTask RetryAsync() => StartLevelAsync(); 

        private async UniTask<ResultChoice> ShowResultAsync(GameResult result)
        {
            if (result.Won)
            {
                PopupLevelComplete popup = null;
                await _ui.PushPopupAsync<PopupLevelComplete>(
                    PopupCompleteName,
                    onLoad: (_, p) => popup = p);
                return popup != null ? await popup.WaitForChoiceAsync(result) : ResultChoice.Quit;
            }
            else
            {
                PopupGameOver popup = null;
                await _ui.PushPopupAsync<PopupGameOver>(
                    PopupGameOverName,
                    onLoad: (_, p) => popup = p);
                return popup != null ? await popup.WaitForChoiceAsync(result) : ResultChoice.Quit;
            }
        }
    }
}
