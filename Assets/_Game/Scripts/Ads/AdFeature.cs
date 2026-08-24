using System;
using CaskFramework.Ads;
using CaskFramework.Core;
using CaskFramework.Profile;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{
    public sealed class AdFeature : IFeature
    {
        private readonly AdRule _rule;
        private IServiceContext _context;
        private float _lastAdCloseTime = -9999f;
        private int _sessionPlayedCount;
        private bool _isEndgameRestore;

        public event Action MediationReady;

        public bool IsMediationReady { get; private set; }

        public AdFeature(AdRule rule)
        {
            _rule = rule ?? new AdRule();
        }

        public void Initialize(IServiceContext context)
        {
            _context = context;
            if (_context == null) return;
            InitMediationAsync().Forget();
        }

        private async UniTaskVoid InitMediationAsync()
        {
            await UniTask.WaitUntil(() => _context.IsRegistered<IAdsService>());

            if (!_context.TryGet(out IAdsService ads)) return;

            ads.InitMediation(OnMediationInitialized);
        }

        private void OnMediationInitialized()
        {
            IsMediationReady = true;
            LoadRewarded();
            MediationReady?.Invoke();
        }

        public bool IsRewardRequired
        {
            get
            {
                if (!_context.TryGet(out IProfileService profile))
                    return false;
                return profile.ProgressLevel >= _rule.RewardUnlockLevel;
            }
        }

        public bool ShouldShowInterstitial(AdPosition position)
        {
            if (position != AdPosition.NormalStart &&
                position != AdPosition.NormalRestart &&
                position != AdPosition.NormalContinue)
                return false;

            if (!_context.TryGet(out IProfileService profile))
                return false;

            if (profile.ProgressLevel < _rule.InterUnlockLevel)
                return false;

            if (profile.PlayedSession < _rule.InterUnlockSession)
                return false;

            if (_sessionPlayedCount < 1)
                return false;

            float elapsed = Time.realtimeSinceStartup - _lastAdCloseTime;
            if (elapsed < _rule.CooldownSeconds)
                return false;

            if (SystemInfo.systemMemorySize < _rule.MinMemoryMB)
                return false;

            if (_isEndgameRestore)
                return false;

            if (!_context.TryGet(out IAdsService ads) || !ads.IsInterstitialReady())
                return false;

            return true;
        }

        public bool ShouldShowBanner(int boardSize)
        {
            if (!_context.TryGet(out IProfileService profile))
                return false;

            if (profile.ProgressLevel < _rule.BannerUnlockLevel)
                return false;

            if (profile.PlayedSession < _rule.BannerUnlockSession)
                return false;

            if (!IsBoardSizeAllowed(boardSize))
                return false;

            return _context.IsRegistered<IAdsService>();
        }

        public bool CanShowRewarded()
        {
            if (!_context.TryGet(out IAdsService ads))
                return false;
            return ads.IsRewardedAdsReady();
        }

        public void ShowBanner(string placement = "gameplay")
        {
            AdPlacementContext.Set(AnalyticsValues.ad_format_banner, placement);
            if (_context.TryGet(out IAdsService ads))
                ads.ShowBanner();
        }

        public void HideBanner()
        {
            if (_context.TryGet(out IAdsService ads))
                ads.HideBanner();
        }

        public void LoadRewarded()
        {
            if (_context.TryGet(out IAdsService ads))
                ads.LoadRewardedAds();
        }

        public async UniTask<bool> ShowRewardedAsync(RewardMode mode = RewardMode.ForceShowRewardedAd,
                                                    string placement = "", string action = "",
                                                    float loadTimeoutSeconds = 10f)
        {
            if (!_context.TryGet(out IAdsService ads))
                return false;

            if (mode == RewardMode.ShowRewardedVideo && !ads.IsRewardedAdsReady())
                return false;

            AdPlacementContext.Set(AnalyticsValues.ad_format_rewarded, placement, action);

            var tcs = new UniTaskCompletionSource<bool>();
            ads.ShowRewarded(mode, (success, _) =>
            {
                if (success)
                {
                    RecordAdClose();
                    LevelAnalytics.RecordRewardedWatched();
                }
                LoadRewarded();
                tcs.TrySetResult(success);
            }, timeOut: loadTimeoutSeconds);
            return await tcs.Task;
        }

        public async UniTask<bool> ShowInterstitialAsync(string placement = "")
        {
            if (!_context.TryGet(out IAdsService ads) || !ads.IsInterstitialReady())
                return false;

            AdPlacementContext.Set(AnalyticsValues.ad_format_interstitial, placement);

            var tcs = new UniTaskCompletionSource<bool>();
            ads.ShowInterstitial(
                closedEvent: () =>
                {
                    RecordAdClose();
                    tcs.TrySetResult(true);
                },
                displayFailedEvent: () => tcs.TrySetResult(false));
            return await tcs.Task;
        }

        public void RecordAdClose()
        {
            _lastAdCloseTime = Time.realtimeSinceStartup;
        }

        public void IncrementSessionPlayedCount()
        {
            _sessionPlayedCount++;
        }

        public void SetEndgameRestore(bool value)
        {
            _isEndgameRestore = value;
        }

        private bool IsBoardSizeAllowed(int size)
        {
            int[] allowed = _rule.BannerAllowedBoardSizes;
            if (allowed == null || allowed.Length == 0)
                return true;
            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i] == size)
                    return true;
            }
            return false;
        }
    }
}
