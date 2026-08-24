using System;
using CaskFramework.Ads.Events;

namespace Cast.Game
{
    public sealed class AdNetworkAdsBinder : IDisposable
    {
        private Action<string, string, string, double?, string, string> _onBannerRevenue;
        private Action<string, string, string, double?, string, string> _onInterstitialRevenue;
        private Action<string, string, string, double?, string, string> _onRewardedRevenue;
        private Action<string, string, string, double?, string, string> _onAppOpenRevenue;

        private Action<string, MaxSdkBase.AdInfo> _onInterstitialClicked;
        private Action<string, MaxSdkBase.AdInfo> _onRewardedClicked;
        private Action<string, MaxSdkBase.AdInfo> _onBannerClicked;
        private Action<string, MaxSdkBase.AdInfo> _onAppOpenClicked;

        private bool _bound;

        public void Bind()
        {
            if (_bound) return;
            _bound = true;

            _onBannerRevenue = OnAdViewed;
            _onInterstitialRevenue = OnAdViewed;
            _onRewardedRevenue = OnAdViewed;
            _onAppOpenRevenue = OnAdViewed;

            BannerEvent.OnRevenuePaid += _onBannerRevenue;
            InterstitialEvent.OnRevenuePaid += _onInterstitialRevenue;
            RewardedEvent.OnRevenuePaid += _onRewardedRevenue;
            AOAEvent.OnRevenuePaid += _onAppOpenRevenue;

            _onInterstitialClicked = OnAdClicked;
            _onRewardedClicked = OnAdClicked;
            _onBannerClicked = OnAdClicked;
            _onAppOpenClicked = OnAdClicked;

            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += _onInterstitialClicked;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += _onRewardedClicked;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += _onBannerClicked;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += _onAppOpenClicked;
        }

        public void Dispose()
        {
            if (!_bound) return;
            _bound = false;

            BannerEvent.OnRevenuePaid -= _onBannerRevenue;
            InterstitialEvent.OnRevenuePaid -= _onInterstitialRevenue;
            RewardedEvent.OnRevenuePaid -= _onRewardedRevenue;
            AOAEvent.OnRevenuePaid -= _onAppOpenRevenue;

            MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= _onInterstitialClicked;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= _onRewardedClicked;
            MaxSdkCallbacks.Banner.OnAdClickedEvent -= _onBannerClicked;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent -= _onAppOpenClicked;
        }

        private static void OnAdViewed(string adUnit, string adNetwork, string adFormat, double? revenue, string creativeId, string currency)
        {
            AdNetworkTracker.Track(AdNetworkEvents.ad_view);
        }

        private static void OnAdClicked(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdNetworkTracker.Track(AdNetworkEvents.ad_clicked);
        }
    }
}
