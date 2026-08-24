using System.Collections.Generic;
using CaskFramework.Ads;

namespace Cast.Game
{
    public static class AdPlacementContext
    {
        private static readonly Dictionary<string, Entry> s_entries = new();

        public static void Set(string adFormat, string placement, string action = "")
        {
            s_entries[adFormat] = new Entry(placement, action);
        }

        public static string GetPlacement(string adFormat)
        {
            return s_entries.TryGetValue(adFormat, out Entry entry) ? entry.Placement : string.Empty;
        }

        public static string GetAction(string adFormat)
        {
            return s_entries.TryGetValue(adFormat, out Entry entry) ? entry.Action : string.Empty;
        }

        public static string ToAnalyticsFormat(string mediationAdFormat)
        {
            switch (mediationAdFormat)
            {
                case AdFormat.REWARDED_VIDEO: return AnalyticsValues.ad_format_rewarded;
                case AdFormat.INTERSTITIAL: return AnalyticsValues.ad_format_interstitial;
                case AdFormat.BANNER: return AnalyticsValues.ad_format_banner;
                case AdFormat.APP_OPEN: return AnalyticsValues.ad_format_app_open;
                case AdFormat.NATIVE: return AnalyticsValues.ad_format_native;
                default: return mediationAdFormat;
            }
        }

        private readonly struct Entry
        {
            public readonly string Placement;
            public readonly string Action;

            public Entry(string placement, string action)
            {
                Placement = placement;
                Action = action;
            }
        }
    }
}
