using System;
using CaskFramework.Save;

namespace Cast.Game
{
    [Serializable]
    public sealed class DailyChallengeData : ISaveData
    {
        public string LastPlayDate;
        public int CurrentStreak;
        public int BestStreak;
        public bool CompletedToday;
    }
}
