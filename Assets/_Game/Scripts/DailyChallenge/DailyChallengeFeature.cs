using System;
using CaskFramework.Core;
using CaskFramework.Save;

namespace Cast.Game
{
    public sealed class DailyChallengeFeature : IFeature
    {
        private const string SaveKey = "daily_challenge";
        private const int TotalLevelCount = 101;

        private ISaveService _save;

        public void Initialize(IServiceContext context)
        {
            _save = context.Get<ISaveService>();
            if (!_save.IsRegistered(SaveKey))
            {
                var session = new SaveSession(
                    new DailyChallengeData(),
                    new UnityJsonSaveSerializer(),
                    new XorSaveEncryptor("HexaRemake"),
                    new PlayerPrefsSaveStore());
                _save.Register<DailyChallengeData>(SaveKey, session);
            }
            EnsureCurrentDay();
        }

        public DailyChallengeData Data
        {
            get
            {
                EnsureCurrentDay();
                return _save.Get<DailyChallengeData>(SaveKey);
            }
        }

        public bool IsAvailableToday()
        {
            return !Data.CompletedToday;
        }

        public void RecordCompletion()
        {
            DailyChallengeData data = Data;
            string today = TodayString();

            if (data.LastPlayDate == today)
                return;

            DateTime todayDate = DateTime.UtcNow.Date;
            if (DateTime.TryParse(data.LastPlayDate, out DateTime lastParsed))
            {
                if ((todayDate - lastParsed.Date).TotalDays == 1)
                    data.CurrentStreak++;
                else
                    data.CurrentStreak = 1;
            }
            else
            {
                data.CurrentStreak = 1;
            }

            data.BestStreak = Math.Max(data.BestStreak, data.CurrentStreak);
            data.LastPlayDate = today;
            data.CompletedToday = true;
            _save.Save(SaveKey);
        }

        public int GetDailyLevelId()
        {
            int daysSinceEpoch = (int)(DateTime.UtcNow.Date - new DateTime(2024, 1, 1)).TotalDays;
            return daysSinceEpoch % TotalLevelCount;
        }

        private void EnsureCurrentDay()
        {
            DailyChallengeData data = _save.Get<DailyChallengeData>(SaveKey);
            if (data.LastPlayDate == TodayString())
                return;
            if (!data.CompletedToday)
                return;
            data.CompletedToday = false;
            _save.Save(SaveKey);
        }

        private static string TodayString()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }
    }
}
