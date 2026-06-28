using System;
using System.Threading;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{

    public sealed class GameFlow
    {
        private readonly ILevelDataReader _reader;
        private readonly IGameSession _session;
        private readonly BoardView _board;
        private readonly BoardInputHandler _interaction;
        private readonly IUIManager _ui;
        private readonly IBoosterController _boosters;
        private readonly IProfileService _profile;

        private CancellationTokenSource _cts;
        private bool _initialized = false;
        private ViewGameplay _viewGameplay;
        private bool _retryRequested;

        public int CurrentLevelId => _profile.ProgressLevel;
        public bool IsInitialized => _initialized;

        public GameFlow(ILevelDataReader reader, IGameSession session, BoardView board,
                        BoardInputHandler interaction, IUIManager ui, IBoosterController boosters,
                        IProfileService profile)
        {
            _reader = reader;
            _session = session;
            _board = board;
            _interaction = interaction;
            _ui = ui;
            _boosters = boosters;
            _profile = profile;

            _initialized = true;
        }

        public async UniTask RunAsync()
        {
            // _board.SetVisible(false);
            // bool skipHome = _profile.ProgressLevel <= 1;
            // while (true)
            // {
            //     if (!skipHome)
            //         await ShowHomeUntilPlayAsync();
            //     skipHome = false;

            //     await PlayLevelsAsync();
            // }

            await PlayLevelsAsync();
        }

        public async UniTask ShowHomeAsync()
        {
            var tcs = new UniTaskCompletionSource();
            await _ui.PushViewAsync<ViewHome>(UIConst.ViewHome, stack: false, onLoad: (_, v) =>
                v.Setup(() => tcs.TrySetResult(), _profile));
            await tcs.Task;
        }

        private async UniTask PlayLevelsAsync()
        {
            while (true)
            {
                _retryRequested = false;
                GameResult? played = await PlayCurrentLevelAsync();
                if (played == null)
                {
                    if (!_retryRequested) await ShowHomeAsync();
                    continue;
                }
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

            bool homeRequested = false;
            void OnHomeRequested()
            {
                homeRequested = true;
                _board.ClearBoard();
                _board.SetVisible(false);
                _cts?.Cancel();
                _session.Dispose();
            }

            void OnRetryRequested()
            {
                _retryRequested = true;
                _board.ClearBoard();
                _board.SetVisible(false);
                _cts?.Cancel();
                _session.Dispose();
            }

            _session.Setup(read.Level);
            await _board.BuildAsync(read.Level);
            _board.BindRendering(_session);
            _interaction.Bind(_session);

            if (_viewGameplay == null)
            {
                await _ui.PushViewAsync<ViewGameplay>(
                    UIConst.ViewGameplay,
                    stack: false,
                    onLoad: (_, view) =>
                    {
                        _viewGameplay = view;
                        view.Bind(_session, _boosters, _board, OnHomeRequested, OnRetryRequested);
                    });
            }
            else
            {
                _viewGameplay.Bind(_session, _boosters, _board, OnHomeRequested, OnRetryRequested);
            }

            await UniTask.WaitUntil(() => _ui.GetActiveTopView() == null);

            try
            {
                _board.SetVisible(true);
                await _board.PlayRevealAsync(ct);

                if (homeRequested)
                    return null;

                _session.Begin();
                return await _session.PlayToEndAsync(ct);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        private async UniTask<WinChoice> ShowWinAsync(GameResult result)
        {
            PopupWin popup = null;
            await _ui.PushPopupAsync<PopupWin>(
                UIConst.PopupWin,
                onLoad: (_, p) => popup = p);
            WinChoice choice = popup != null ? await popup.WaitForChoiceAsync(result, _profile.ProgressLevel) : WinChoice.Home;
            await _ui.PopPopupAsync();
            return choice;
        }

        private async UniTask<LoseChoice> ShowLoseAsync(GameResult result)
        {
            PopupOutOfMove popup = null;
            await _ui.PushPopupAsync<PopupOutOfMove>(
                UIConst.PopupOutOfMove,
                onLoad: (_, p) => popup = p);
            LoseChoice choice = popup != null ? await popup.WaitForChoiceAsync(result) : LoseChoice.Home;
            await _ui.PopPopupAsync();
            return choice;
        }
    }
}
