using System.Collections.Generic;
using CaskFramework.Analytics;
using CaskFramework.Core;

namespace Cast.Game
{
    public static class GameAnalytics
    {
        private static ITrackingService Service =>
            GameRuntime.TryGet(out ITrackingService service) ? service : null;

        public static void Track(string eventName, Dictionary<string, object> parameters = null, bool immediate = false)
        {
            Service?.Track(eventName, parameters, immediate);
        }

        public static void SetUserProperty(string name, string value)
        {
            Service?.SetUserProperty(name, value);
        }

        public static void LoadingStart(string type, string id)
        {
            Track(AnalyticsEvents.loading_start, new Dictionary<string, object>
            {
                { AnalyticsParams.type, type },
                { AnalyticsParams.id, id }
            });
        }

        public static void LoadingEnd(string type, string id, bool failed, string errorMessage = "")
        {
            Track(AnalyticsEvents.loading_end, new Dictionary<string, object>
            {
                { AnalyticsParams.type, type },
                { AnalyticsParams.id, id },
                { AnalyticsParams.fail, failed },
                { AnalyticsParams.errorMessage, errorMessage ?? string.Empty }
            });
        }

        public static void Ftue(string step, string screen, string action)
        {
            Track(AnalyticsEvents.FTUE, new Dictionary<string, object>
            {
                { AnalyticsParams.step, step },
                { AnalyticsParams.screen, screen },
                { AnalyticsParams.action, action }
            });
        }

        public static void LevelStart(int level, string mode, string mapId)
        {
            Track(AnalyticsEvents.level_start, new Dictionary<string, object>
            {
                { AnalyticsParams.level, level },
                { AnalyticsParams.mode, mode },
                { AnalyticsParams.map_id, mapId }
            });
        }

        public static void LevelEnd(int level, string mode, int timePlayed, string status,
                                    int hintUsed, int boostUsed, int rewardedWatched,
                                    int moves, bool rescue, string mapId)
        {
            Track(AnalyticsEvents.level_end, new Dictionary<string, object>
            {
                { AnalyticsParams.level, level },
                { AnalyticsParams.mode, mode },
                { AnalyticsParams.time_played, timePlayed },
                { AnalyticsParams.status, status },
                { AnalyticsParams.hint_used, hintUsed },
                { AnalyticsParams.boost_used, boostUsed },
                { AnalyticsParams.rw_watched, rewardedWatched },
                { AnalyticsParams.moves, moves },
                { AnalyticsParams.rescue, rescue },
                { AnalyticsParams.map_id, mapId }
            });
        }

        public static void AdRequest(string adFormat, string adPlatform, string adUnitName,
                                     string placement, string status, string errorMessage)
        {
            Track(AnalyticsEvents.ad_request, new Dictionary<string, object>
            {
                { AnalyticsParams.ad_format, adFormat },
                { AnalyticsParams.ad_platform, adPlatform },
                { AnalyticsParams.ad_unit_name, adUnitName },
                { AnalyticsParams.placement, placement },
                { AnalyticsParams.status, status },
                { AnalyticsParams.error_message, errorMessage ?? string.Empty }
            });
        }

        public static void AdShow(string adFormat, string adPlatform, string adUnitName,
                                  string placement, string status, string errorMessage)
        {
            Track(AnalyticsEvents.ad_show, new Dictionary<string, object>
            {
                { AnalyticsParams.ad_format, adFormat },
                { AnalyticsParams.ad_platform, adPlatform },
                { AnalyticsParams.ad_unit_name, adUnitName },
                { AnalyticsParams.placement, placement },
                { AnalyticsParams.status, status },
                { AnalyticsParams.error_message, errorMessage ?? string.Empty }
            });
        }

        public static void AdImpression(string adFormat, string adPlatform, string adUnitName,
                                        string placement, string action, double value, string currency)
        {
            Track(AnalyticsEvents.ad_impression, new Dictionary<string, object>
            {
                { AnalyticsParams.ad_format, adFormat },
                { AnalyticsParams.ad_platform, adPlatform },
                { AnalyticsParams.ad_unit_name, adUnitName },
                { AnalyticsParams.placement, placement },
                { AnalyticsParams.action, action ?? string.Empty },
                { AnalyticsParams.currency, string.IsNullOrEmpty(currency) ? AnalyticsValues.default_currency : currency },
                { AnalyticsParams.value, value }
            }, immediate: true);
        }

        public static void PurchaseSuccess(string type, double revenue, string currency)
        {
            Track(AnalyticsEvents.purchase_success, new Dictionary<string, object>
            {
                { AnalyticsParams.type, type },
                { AnalyticsParams.revenue, revenue },
                { AnalyticsParams.currency, currency }
            }, immediate: true);
        }

        public static void PurchaseFail(string type, double revenue, string currency, string errorMessage)
        {
            Track(AnalyticsEvents.purchase_fail, new Dictionary<string, object>
            {
                { AnalyticsParams.type, type },
                { AnalyticsParams.revenue, revenue },
                { AnalyticsParams.currency, currency },
                { AnalyticsParams.errorMessage, errorMessage ?? string.Empty }
            });
        }

        public static void PurchaseRestore(string type, double revenue, string currency)
        {
            Track(AnalyticsEvents.purchase_restore, new Dictionary<string, object>
            {
                { AnalyticsParams.type, type },
                { AnalyticsParams.revenue, revenue },
                { AnalyticsParams.currency, currency }
            });
        }
    }
}
