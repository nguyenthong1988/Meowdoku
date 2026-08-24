using CaskFramework.Ads;
using CaskFramework.Audio;
using CaskFramework.Core;
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
        private readonly GameplayViewRef _gameplayViewRef;
        private readonly AdFeature _ads;
        private readonly DailyChallengeFeature _dailyChallenge;

        private AdPosition _entryType = AdPosition.NormalStart;
        private GameMode _gameMode = GameMode.Normal;

        public LoadLevelState(GameStateMachine machine, IUIManager ui, ILevelDataReader reader,
                              BoardView board, IGameSession session, BoardInputHandler interaction,
                              IBoosterController boosters, IProfileService profile,
                              GameplayViewRef gameplayViewRef, AdFeature ads,
                              DailyChallengeFeature dailyChallenge)
        {
            _machine = machine;
            _ui = ui;
            _reader = reader;
            _board = board;
            _session = session;
            _interaction = interaction;
            _boosters = boosters;
            _profile = profile;
            _gameplayViewRef = gameplayViewRef;
            _ads = ads;
            _dailyChallenge = dailyChallenge;
        }

        public void SetEntryType(AdPosition entryType)
        {
            _entryType = entryType;
        }

        public void SetGameMode(GameMode mode)
        {
            _gameMode = mode;
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
            if (_ads.ShouldShowInterstitial(_entryType))
                await _ads.ShowInterstitialAsync(_entryType.ToString());

            _entryType = AdPosition.NormalStart;
            GameMode currentMode = _gameMode;
            _gameMode = GameMode.Normal;

            int levelId = currentMode == GameMode.DailyChallenge
                ? _dailyChallenge.GetDailyLevelId()
                : _profile.CurrentLevelId();

            GameAnalytics.LoadingStart(AnalyticsValues.loading_type_level, levelId.ToString());

            LevelReadResult read = await _reader.ReadLevelAsync(levelId);
            if (!read.Success)
            {
                string reason = read.Validation?.Summary() ?? "Unknown level read failure";
                Debug.LogError($"[LoadLevelState] Failed to load level {levelId}:\n{reason}");
                GameAnalytics.LoadingEnd(AnalyticsValues.loading_type_level, levelId.ToString(), true, reason);
                _machine.ChangeState<HomeState>();
                return;
            }

            GameAnalytics.LoadingEnd(AnalyticsValues.loading_type_level, levelId.ToString(), false);

            _board.SetVisible(false);

            _session.Setup(read.Level);
            LevelAnalytics.BeginLevel(levelId, currentMode, read.Level);
            await _board.BuildAsync(read.Level);
            if (FeatureManager.TryGet(out ThemeFeature theme))
                await UniTask.WhenAll(_board.ApplyThemeAsync(levelId), theme.ApplyThemeAsync(levelId));
            else
                await _board.ApplyThemeAsync(levelId);
            _board.BindRendering(_session);
            _interaction.Bind(_session);

            if (GameRuntime.TryGet(out IAudioManager audio))
            {
                if (audio.IsMusicPaused)
                    audio.ResumeMusicAsync(0.5f).Forget();
                else if (!audio.IsPlayingMusic)
                    audio.PlayMusic(AudioNames.BGM_BACKGROUND, 0.5f);
            }

            if (_gameplayViewRef.View == null)
            {
                await _ui.PushViewAsync<ViewGameplay>(UIConst.ViewGameplay, stack: false, onLoad: (_, view) => _gameplayViewRef.View = view);
            }

            BindGameplayView(currentMode);
        }

        private void BindGameplayView(GameMode mode)
        {
            _gameplayViewRef.View.Bind(
                _session,
                _boosters,
                _board,
                _interaction,
                _ui,
                onHomeRequested: () =>
                {
                    LevelAnalytics.ReportQuit();
                    _machine.ChangeState<HomeState>();
                },
                onRetryRequested: () => _machine.ChangeState<LoadLevelState>(s =>
                {
                    s.SetEntryType(AdPosition.NormalRestart);
                    s.SetGameMode(mode);
                }),
                onEntryPositioned: () => _machine.ChangeState<RevealState>(s => s.SetGameMode(mode)));
        }
    }
}
