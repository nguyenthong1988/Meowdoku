using System;
using CaskFramework.Analytics;
using UnityEngine;

namespace Cast.Game
{
    public sealed class AnalyticsUserPropertyService
    {
        private const string KeyInstallTicks = "analytics.install_ticks";
        private const string KeyInstallVersion = "analytics.install_version";
        private const string KeyAbTestGroup = "analytics.ab_test_group";
        private const string KeyLevelWin = "analytics.level_win";
        private const string KeyLevelLose = "analytics.level_lose";
        private const string KeyLevelQuit = "analytics.level_quit";
        private const string KeyAdTotalCount = "analytics.ad_total_count";
        private const string KeyAdRewardedCount = "analytics.ad_rw_count";
        private const string KeyAdInterstitialCount = "analytics.ad_inter_count";
        private const string KeyAdBannerCount = "analytics.ad_banner_count";
        private const string KeyIapHistory = "analytics.iap_history";
        private const string KeyIapValue = "analytics.iap_value";
        private const string KeyIapCurrent = "analytics.iap_current";

        private readonly ITrackingService _tracking;

        public static AnalyticsUserPropertyService Current { get; private set; }

        public AnalyticsUserPropertyService(ITrackingService tracking)
        {
            _tracking = tracking;
            Current = this;
        }

        public void PushStartupProperties()
        {
            EnsureInstallRecorded();

            _tracking.SetUserProperty(AnalyticsUserProperties.current_app_version, Application.version);
            _tracking.SetUserProperty(AnalyticsUserProperties.install_app_verison, PlayerPrefs.GetString(KeyInstallVersion, Application.version));

            string abTestGroup = PlayerPrefs.GetString(KeyAbTestGroup, string.Empty);
            _tracking.SetUserProperty(AnalyticsUserProperties.ab_test_group, abTestGroup);
            _tracking.SetUserProperty(AnalyticsUserProperties.abTest, abTestGroup);

            _tracking.SetUserProperty(AnalyticsUserProperties.memoryRam, SystemInfo.systemMemorySize.ToString());

            PushLifeTime();
            PushLevelCounters();
            PushAdCounters();
            PushPurchaseProperties();
        }

        public void SetAbTestGroup(string group)
        {
            if (string.IsNullOrEmpty(group)) return;

            PlayerPrefs.SetString(KeyAbTestGroup, group);
            PlayerPrefs.Save();

            _tracking.SetUserProperty(AnalyticsUserProperties.ab_test_group, group);
            _tracking.SetUserProperty(AnalyticsUserProperties.abTest, group);
        }

        public void SetCurrentLevel(int level)
        {
            _tracking.SetUserProperty(AnalyticsUserProperties.level_current, level.ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.lv, level.ToString());
        }

        public void RecordLevelResult(string status)
        {
            switch (status)
            {
                case AnalyticsValues.level_status_win:
                    Increment(KeyLevelWin, AnalyticsUserProperties.level_win);
                    break;
                case AnalyticsValues.level_status_lose:
                    Increment(KeyLevelLose, AnalyticsUserProperties.level_lose);
                    break;
                case AnalyticsValues.level_status_quit:
                    Increment(KeyLevelQuit, AnalyticsUserProperties.level_quit);
                    break;
            }

            PushLifeTime();
        }

        public void RecordAdImpression(string adFormat)
        {
            Increment(KeyAdTotalCount, AnalyticsUserProperties.ad_total_count);

            switch (adFormat)
            {
                case AnalyticsValues.ad_format_rewarded:
                    int rewarded = Increment(KeyAdRewardedCount, AnalyticsUserProperties.ad_rw_count);
                    _tracking.SetUserProperty(AnalyticsUserProperties.advCount, rewarded.ToString());
                    break;
                case AnalyticsValues.ad_format_interstitial:
                    int interstitial = Increment(KeyAdInterstitialCount, AnalyticsUserProperties.ad_inter_count);
                    _tracking.SetUserProperty(AnalyticsUserProperties.adiCount, interstitial.ToString());
                    break;
                case AnalyticsValues.ad_format_banner:
                    Increment(KeyAdBannerCount, AnalyticsUserProperties.ad_banner_count);
                    break;
            }
        }

        public void RecordPurchase(string productId, double value)
        {
            int history = PlayerPrefs.GetInt(KeyIapHistory, 0) + 1;
            int total = PlayerPrefs.GetInt(KeyIapValue, 0) + Mathf.RoundToInt((float)value);

            PlayerPrefs.SetInt(KeyIapHistory, history);
            PlayerPrefs.SetInt(KeyIapValue, total);
            PlayerPrefs.SetString(KeyIapCurrent, productId ?? string.Empty);
            PlayerPrefs.Save();

            PushPurchaseProperties();
        }

        private void PushLifeTime()
        {
            int days = DaysSinceInstall();
            _tracking.SetUserProperty(AnalyticsUserProperties.date_diff, days.ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.lt, days.ToString());
        }

        private void PushLevelCounters()
        {
            _tracking.SetUserProperty(AnalyticsUserProperties.level_win, PlayerPrefs.GetInt(KeyLevelWin, 0).ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.level_lose, PlayerPrefs.GetInt(KeyLevelLose, 0).ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.level_quit, PlayerPrefs.GetInt(KeyLevelQuit, 0).ToString());
        }

        private void PushAdCounters()
        {
            int rewarded = PlayerPrefs.GetInt(KeyAdRewardedCount, 0);
            int interstitial = PlayerPrefs.GetInt(KeyAdInterstitialCount, 0);

            _tracking.SetUserProperty(AnalyticsUserProperties.ad_total_count, PlayerPrefs.GetInt(KeyAdTotalCount, 0).ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.ad_rw_count, rewarded.ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.ad_inter_count, interstitial.ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.ad_banner_count, PlayerPrefs.GetInt(KeyAdBannerCount, 0).ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.advCount, rewarded.ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.adiCount, interstitial.ToString());
        }

        private void PushPurchaseProperties()
        {
            int history = PlayerPrefs.GetInt(KeyIapHistory, 0);

            _tracking.SetUserProperty(AnalyticsUserProperties.is_iap, (history > 0).ToString().ToLower());
            _tracking.SetUserProperty(AnalyticsUserProperties.iap_history, history.ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.iap_value, PlayerPrefs.GetInt(KeyIapValue, 0).ToString());
            _tracking.SetUserProperty(AnalyticsUserProperties.iap_current, PlayerPrefs.GetString(KeyIapCurrent, string.Empty));
        }

        private int Increment(string prefsKey, string propertyName)
        {
            int next = PlayerPrefs.GetInt(prefsKey, 0) + 1;
            PlayerPrefs.SetInt(prefsKey, next);
            PlayerPrefs.Save();

            _tracking.SetUserProperty(propertyName, next.ToString());
            return next;
        }

        private void EnsureInstallRecorded()
        {
            if (PlayerPrefs.HasKey(KeyInstallTicks)) return;

            PlayerPrefs.SetString(KeyInstallTicks, DateTime.UtcNow.Date.Ticks.ToString());
            PlayerPrefs.SetString(KeyInstallVersion, Application.version);
            PlayerPrefs.SetString(KeyAbTestGroup, BuildAbTestGroup());
            PlayerPrefs.Save();
        }

        private static int DaysSinceInstall()
        {
            string stored = PlayerPrefs.GetString(KeyInstallTicks, string.Empty);
            if (!long.TryParse(stored, out long ticks)) return 0;

            TimeSpan elapsed = DateTime.UtcNow.Date - new DateTime(ticks, DateTimeKind.Utc);
            return Mathf.Max(0, (int)elapsed.TotalDays);
        }

        private static string BuildAbTestGroup()
        {
            string version = Application.version.Replace(".", string.Empty);
            string bucket = UnityEngine.Random.value < 0.5f ? "a" : "b";
            return $"v{version}_{bucket}";
        }
    }
}
