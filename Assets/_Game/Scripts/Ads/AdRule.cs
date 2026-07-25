using System;
using UnityEngine;

namespace Cast.Game
{
    [Serializable]
    public sealed class AdRule
    {
        [Header("Interstitial Gates")]
        [Min(1)] public int InterUnlockLevel = 11;
        [Min(1)] public int InterUnlockSession = 2;
        [Min(0)] public float CooldownSeconds = 60f;
        [Min(0)] public int MinMemoryMB = 2000;

        [Header("Banner Gates")]
        [Min(1)] public int BannerUnlockLevel = 1;
        [Min(1)] public int BannerUnlockSession = 1;
        public int[] BannerAllowedBoardSizes = { 5, 6, 7, 8, 9 };

        [Header("Reward Gates")]
        [Min(1)] public int RewardUnlockLevel = 5;
    }
}
