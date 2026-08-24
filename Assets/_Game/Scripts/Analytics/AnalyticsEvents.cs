namespace Cast.Game
{
    public static class AnalyticsEvents
    {
        public const string loading_start = "loading_start";
        public const string loading_end = "loading_end";

        public const string FTUE = "FTUE";

        public const string level_start = "level_start";
        public const string level_end = "level_end";

        public const string ad_impression = "ad_impression";
        public const string ad_request = "ad_request";
        public const string ad_show = "ad_show";

        public const string purchase_success = "purchase_success";
        public const string purchase_fail = "purchase_fail";
        public const string purchase_restore = "purchase_restore";

        public const string adi_show_success = "adi_show_success";
        public const string adv_show_success = "adv_show_success";
        public const string total_ads_revenue_001 = "total_ads_revenue_001";
    }

    public static class AnalyticsParams
    {
        public const string type = "type";
        public const string id = "id";
        public const string fail = "fail";
        public const string errorMessage = "errorMessage";

        public const string step = "step";
        public const string screen = "screen";
        public const string action = "action";

        public const string level = "level";
        public const string mode = "mode";
        public const string map_id = "map_id";
        public const string time_played = "time_played";
        public const string status = "status";
        public const string hint_used = "hint_used";
        public const string boost_used = "boost_used";
        public const string rw_watched = "rw_watched";
        public const string moves = "moves";
        public const string rescue = "rescue";

        public const string ad_format = "ad_format";
        public const string ad_platform = "ad_platform";
        public const string ad_unit_name = "ad_unit_name";
        public const string placement = "placement";
        public const string error_message = "error_message";

        public const string currency = "currency";
        public const string value = "value";
        public const string revenue = "revenue";
    }

    public static class AnalyticsValues
    {
        public const string status_success = "success";
        public const string status_fail = "fail";

        public const string level_status_win = "win";
        public const string level_status_lose = "lose";
        public const string level_status_quit = "quit";

        public const string ad_format_rewarded = "rewarded";
        public const string ad_format_interstitial = "interstitial";
        public const string ad_format_banner = "banner";
        public const string ad_format_app_open = "app_open";
        public const string ad_format_native = "native";

        public const string loading_type_app = "app";
        public const string loading_type_level = "level";

        public const string screen_gameplay = "gameplay";

        public const string default_currency = "USD";
    }
}
