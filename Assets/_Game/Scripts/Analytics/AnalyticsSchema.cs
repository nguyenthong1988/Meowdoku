using System.Collections.Generic;
using CaskFramework.Analytics;

namespace Cast.Game
{
    public static class AnalyticsSchema
    {
        public static readonly HashSet<string> ThinkingDataAndFirebaseEvents = new()
        {
            AnalyticsEvents.loading_start,
            AnalyticsEvents.loading_end,
            AnalyticsEvents.FTUE,
            AnalyticsEvents.level_start,
            AnalyticsEvents.level_end,
            AnalyticsEvents.ad_impression,
            AnalyticsEvents.ad_request,
            AnalyticsEvents.ad_show,
            AnalyticsEvents.purchase_success,
            AnalyticsEvents.purchase_fail,
            AnalyticsEvents.purchase_restore
        };

        public static readonly HashSet<string> FirebaseOnlyEvents = new()
        {
            AnalyticsEvents.adi_show_success,
            AnalyticsEvents.adv_show_success,
            AnalyticsEvents.total_ads_revenue_001
        };

        public static readonly HashSet<string> ThinkingDataAndFirebaseUserProperties = new()
        {
            AnalyticsUserProperties.level_win,
            AnalyticsUserProperties.level_lose,
            AnalyticsUserProperties.level_quit,
            AnalyticsUserProperties.level_current,
            AnalyticsUserProperties.is_iap,
            AnalyticsUserProperties.iap_current,
            AnalyticsUserProperties.iap_history,
            AnalyticsUserProperties.iap_value,
            AnalyticsUserProperties.ad_total_count,
            AnalyticsUserProperties.ad_rw_count,
            AnalyticsUserProperties.ad_inter_count,
            AnalyticsUserProperties.ad_banner_count,
            AnalyticsUserProperties.date_diff,
            AnalyticsUserProperties.ab_test_group,
            AnalyticsUserProperties.current_app_version,
            AnalyticsUserProperties.install_app_verison
        };

        public static readonly HashSet<string> ThinkingDataOnlyUserProperties = new()
        {
            AnalyticsUserProperties.advCount,
            AnalyticsUserProperties.adiCount,
            AnalyticsUserProperties.abTest,
            AnalyticsUserProperties.memoryRam,
            AnalyticsUserProperties.lt,
            AnalyticsUserProperties.lv
        };

        public static readonly HashSet<string> FirebaseEvents = Combine(ThinkingDataAndFirebaseEvents, FirebaseOnlyEvents);
        public static readonly HashSet<string> ThinkingDataEvents = Combine(ThinkingDataAndFirebaseEvents, null);

        public static readonly HashSet<string> FirebaseUserProperties = Combine(ThinkingDataAndFirebaseUserProperties, null);
        public static readonly HashSet<string> ThinkingDataUserProperties = Combine(ThinkingDataAndFirebaseUserProperties, ThinkingDataOnlyUserProperties);

        public static AnalyticsFilter FirebaseFilter() => new(FirebaseEvents, FirebaseUserProperties);

        public static AnalyticsFilter ThinkingDataFilter() => new(ThinkingDataEvents, ThinkingDataUserProperties);

        public static AdRevenueParamKeys AdjustRevenueParamKeys() => new()
        {
            Value = AnalyticsParams.value,
            Currency = AnalyticsParams.currency,
            AdNetwork = AnalyticsParams.ad_platform,
            AdUnit = AnalyticsParams.ad_unit_name,
            Placement = AnalyticsParams.placement
        };

        private static HashSet<string> Combine(HashSet<string> first, HashSet<string> second)
        {
            HashSet<string> merged = new(first);
            if (second != null) merged.UnionWith(second);
            return merged;
        }
    }
}
