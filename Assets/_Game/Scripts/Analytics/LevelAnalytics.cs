using UnityEngine;

namespace Cast.Game
{
    public static class LevelAnalytics
    {
        private static IGameSession _session;
        private static bool _active;
        private static int _level;
        private static string _mode;
        private static string _mapId;
        private static float _startTime;
        private static int _hintUsed;
        private static int _boostUsed;
        private static int _rewardedWatched;
        private static bool _rescued;

        public static bool IsActive => _active;

        public static void Bind(IGameSession session, IBoosterController boosters)
        {
            _session = session;
            if (boosters != null)
                boosters.BoosterFinished += OnBoosterFinished;
        }

        public static void Unbind(IBoosterController boosters)
        {
            _session = null;
            if (boosters != null)
                boosters.BoosterFinished -= OnBoosterFinished;
        }

        public static void BeginLevel(int level, GameMode gameMode, LevelData data)
        {
            _active = true;
            _level = level;
            _mode = BuildMode(gameMode, data);
            _mapId = BuildMapId(level);
            _startTime = Time.realtimeSinceStartup;
            _hintUsed = 0;
            _boostUsed = 0;
            _rewardedWatched = 0;
            _rescued = false;

            GameAnalytics.LevelStart(_level, _mode, _mapId);
            AnalyticsUserPropertyService.Current?.SetCurrentLevel(level);
            AdNetworkTracker.TrackLevelStart(level);
        }

        public static void RecordRewardedWatched()
        {
            if (!_active) return;
            _rewardedWatched++;
        }

        public static void RecordRescue()
        {
            if (!_active) return;
            _rescued = true;
        }

        public static void EndLevel(string status, GameResult result)
        {
            EndLevel(status, result.Moves);
        }

        public static void ReportQuit()
        {
            if (!_active) return;
            EndLevel(AnalyticsValues.level_status_quit, _session?.Moves ?? 0);
        }

        private static void EndLevel(string status, int moves)
        {
            if (!_active) return;
            _active = false;

            int timePlayed = Mathf.Max(0, Mathf.RoundToInt(Time.realtimeSinceStartup - _startTime));

            GameAnalytics.LevelEnd(_level, _mode, timePlayed, status, _hintUsed, _boostUsed,
                                   _rewardedWatched, moves, _rescued, _mapId);
            AnalyticsUserPropertyService.Current?.RecordLevelResult(status);

            if (status == AnalyticsValues.level_status_win)
                AdNetworkTracker.TrackLevelComplete(_level);
        }

        private static void OnBoosterFinished(BoosterResult result)
        {
            if (!_active || !result.Applied) return;

            if (result.Type == BoosterType.Hint)
                _hintUsed++;
            else
                _boostUsed++;
        }

        private static string BuildMode(GameMode gameMode, LevelData data)
        {
            string mode = gameMode == GameMode.DailyChallenge ? "daily_challenge" : "normal";
            if (data == null) return mode;
            return $"{mode}_{data.Difficulty.ToString().ToLower()}";
        }

        private static string BuildMapId(int level)
        {
            if (!FeatureManager.TryGet(out ThemeFeature theme)) return string.Empty;
            return theme.GetThemeInfoForLevel(level).Index.ToString();
        }
    }
}
