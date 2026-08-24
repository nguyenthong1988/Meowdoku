using System;
using System.Collections.Generic;
using CaskFramework.Ads;
using CaskFramework.Ads.Events;
using UnityEngine;

namespace Cast.Game
{
    public sealed class AdsAnalyticsBinder : IDisposable
    {
        private const string KeyPendingRevenue = "analytics.pending_ads_revenue";
        private const double RevenueReportThreshold = 0.01d;

        private readonly AnalyticsUserPropertyService _userProperties;

        private Action<string, string, string, int, int, string> _onAdResponsed;
        private Action<string, string, string, double?, string, string> _onBannerRevenue;
        private Action<string, string, string, double?, string, string> _onInterstitialRevenue;
        private Action<string, string, string, double?, string, string> _onRewardedRevenue;
        private Action<string, string, string, double?, string, string> _onAppOpenRevenue;
        private Action<string, string, string, double?, string, string, int> _onInterstitialFailed;
        private Action<string, string, string, double?, string, string, int> _onRewardedFailed;
        private Action<string, string, string, double?, string, string, int> _onAppOpenFailed;

        private bool _bound;

        public AdsAnalyticsBinder(AnalyticsUserPropertyService userProperties)
        {
            _userProperties = userProperties;
        }

        public void Bind()
        {
            if (_bound) return;
            _bound = true;

            _onAdResponsed = OnAdResponsed;
            _onBannerRevenue = OnRevenuePaid;
            _onInterstitialRevenue = OnRevenuePaid;
            _onRewardedRevenue = OnRevenuePaid;
            _onAppOpenRevenue = OnRevenuePaid;
            _onInterstitialFailed = OnDisplayFailed;
            _onRewardedFailed = OnDisplayFailed;
            _onAppOpenFailed = OnDisplayFailed;

            Generic.AdResponsed += _onAdResponsed;
            BannerEvent.OnRevenuePaid += _onBannerRevenue;
            InterstitialEvent.OnRevenuePaid += _onInterstitialRevenue;
            RewardedEvent.OnRevenuePaid += _onRewardedRevenue;
            AOAEvent.OnRevenuePaid += _onAppOpenRevenue;
            InterstitialEvent.OnAdDisplayedFailed += _onInterstitialFailed;
            RewardedEvent.OnAdDisplayedFailed += _onRewardedFailed;
            AOAEvent.OnAdDisplayedFailed += _onAppOpenFailed;
        }

        public void Dispose()
        {
            if (!_bound) return;
            _bound = false;

            Generic.AdResponsed -= _onAdResponsed;
            BannerEvent.OnRevenuePaid -= _onBannerRevenue;
            InterstitialEvent.OnRevenuePaid -= _onInterstitialRevenue;
            RewardedEvent.OnRevenuePaid -= _onRewardedRevenue;
            AOAEvent.OnRevenuePaid -= _onAppOpenRevenue;
            InterstitialEvent.OnAdDisplayedFailed -= _onInterstitialFailed;
            RewardedEvent.OnAdDisplayedFailed -= _onRewardedFailed;
            AOAEvent.OnAdDisplayedFailed -= _onAppOpenFailed;
        }

        private void OnAdResponsed(string adUnit, string adNetwork, string adFormat, int success, int errorCode, string creativeId)
        {
            string format = AdPlacementContext.ToAnalyticsFormat(adFormat);
            bool succeeded = success == Status.SUCCESS;

            GameAnalytics.AdRequest(
                format,
                adNetwork,
                adUnit,
                AdPlacementContext.GetPlacement(format),
                succeeded ? AnalyticsValues.status_success : AnalyticsValues.status_fail,
                succeeded ? string.Empty : errorCode.ToString());
        }

        private void OnRevenuePaid(string adUnit, string adNetwork, string adFormat, double? revenue, string creativeId, string currency)
        {
            string format = AdPlacementContext.ToAnalyticsFormat(adFormat);
            string placement = AdPlacementContext.GetPlacement(format);
            double value = revenue ?? 0d;

            GameAnalytics.AdShow(format, adNetwork, adUnit, placement, AnalyticsValues.status_success, string.Empty);
            GameAnalytics.AdImpression(format, adNetwork, adUnit, placement, AdPlacementContext.GetAction(format), value, currency);

            _userProperties?.RecordAdImpression(format);

            if (format == AnalyticsValues.ad_format_interstitial)
                GameAnalytics.Track(AnalyticsEvents.adi_show_success);
            else if (format == AnalyticsValues.ad_format_rewarded)
                GameAnalytics.Track(AnalyticsEvents.adv_show_success);

            AccumulateRevenue(value);
        }

        private void OnDisplayFailed(string adUnit, string adNetwork, string adFormat, double? revenue, string creativeId, string currency, int errorCode)
        {
            string format = AdPlacementContext.ToAnalyticsFormat(adFormat);

            GameAnalytics.AdShow(
                format,
                adNetwork,
                adUnit,
                AdPlacementContext.GetPlacement(format),
                AnalyticsValues.status_fail,
                errorCode.ToString());
        }

        private static void AccumulateRevenue(double value)
        {
            if (value <= 0d) return;

            double pending = GetPendingRevenue() + value;
            int reports = (int)(pending / RevenueReportThreshold);

            if (reports > 0)
            {
                pending -= reports * RevenueReportThreshold;
                for (int i = 0; i < reports; i++)
                {
                    GameAnalytics.Track(AnalyticsEvents.total_ads_revenue_001, new Dictionary<string, object>
                    {
                        { AnalyticsParams.value, RevenueReportThreshold },
                        { AnalyticsParams.currency, AnalyticsValues.default_currency }
                    }, immediate: true);
                }
            }

            PlayerPrefs.SetString(KeyPendingRevenue, pending.ToString("R"));
            PlayerPrefs.Save();
        }

        private static double GetPendingRevenue()
        {
            string stored = PlayerPrefs.GetString(KeyPendingRevenue, string.Empty);
            return double.TryParse(stored, out double pending) ? pending : 0d;
        }
    }
}
