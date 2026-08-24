using System;
using CaskFramework.Ads;
using CaskFramework.Audio;
using CaskFramework.Core;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class WinState : IGameState
    {
        private const float LastRevealDuration = 0.9f;

        private readonly GameStateMachine _machine;
        private readonly IUIManager _ui;
        private readonly BoardView _board;
        private readonly IProfileService _profile;
        private readonly DailyChallengeFeature _dailyChallenge;
        private readonly ThemeFeature _theme;
        private readonly AdFeature _ads;

        private GameResult _result;
        private GameMode _gameMode = GameMode.Normal;

        public WinState(GameStateMachine machine, IUIManager ui, BoardView board,
                        IProfileService profile, DailyChallengeFeature dailyChallenge,
                        ThemeFeature theme, AdFeature ads)
        {
            _machine = machine;
            _ui = ui;
            _board = board;
            _profile = profile;
            _dailyChallenge = dailyChallenge;
            _theme = theme;
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
            LevelAnalytics.EndLevel(AnalyticsValues.level_status_win, _result);
            _ads.HideBanner();

            if (GameRuntime.TryGet(out IAudioManager audio))
                audio.PauseMusicAsync(0.5f).Forget();

            OpenResultAsync().Forget();
        }

        public void Exit()
        {
        }

        private async UniTaskVoid OpenResultAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(LastRevealDuration));
            _board.SetVisible(false);

            if (_gameMode == GameMode.DailyChallenge)
                _dailyChallenge.RecordCompletion();

            int levelId = _gameMode == GameMode.DailyChallenge
                ? _dailyChallenge.GetDailyLevelId()
                : _profile.CurrentLevelId();

            bool themeCompleted = _gameMode == GameMode.Normal &&
                                  _theme.IsLastLevelOfTheme(_profile.CurrentLevelId());

            ViewResult view = null;
            await _ui.PushViewAsync<ViewResult>(UIConst.ViewResult, stack: false, onLoad: (_, v) => view = v);
            if (view == null)
            {
                _machine.ChangeState<HomeState>();
                return;
            }
            view.Setup(_result, levelId, OnChoice, _gameMode, themeCompleted);
        }

        private void OnChoice(WinChoice choice)
        {
            if (choice == WinChoice.Home || _gameMode == GameMode.DailyChallenge)
            {
                _machine.ChangeState<HomeState>();
                return;
            }

            if (choice == WinChoice.NextTheme)
            {
                OpenThemeTransitionAsync().Forget();
                return;
            }

            AdvanceToNextLevel();
        }

        private async UniTaskVoid OpenThemeTransitionAsync()
        {
            ThemeInfo completed = _theme.GetThemeInfoForLevel(_profile.CurrentLevelId());
            ThemeInfo next = _theme.GetThemeInfo(completed.Index + 1);

            ViewThemeTransition view = null;
            await _ui.PushViewAsync<ViewThemeTransition>(UIConst.ViewThemeTransition, stack: false, onLoad: (_, v) => view = v);
            if (view == null)
            {
                AdvanceToNextLevel();
                return;
            }

            view.Setup(_theme, next.BackgroundKey, next.StartLevel, next.EndLevel, AdvanceToNextLevel);
        }

        private void AdvanceToNextLevel()
        {
            _profile.Advance();
            _machine.ChangeState<LoadLevelState>(s => s.SetEntryType(AdPosition.NormalStart));
        }
    }
}
