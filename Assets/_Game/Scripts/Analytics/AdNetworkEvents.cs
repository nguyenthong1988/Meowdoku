using System.Collections.Generic;

namespace Cast.Game
{
    public static class AdNetworkEvents
    {
        public const string ad_clicked = "ad_clicked";
        public const string ad_view = "ad_view";
        public const string app_open = "app_open";
        public const string energy_depleted = "energy_depleted";
        public const string level_complete = "level_complete";
        public const string level_start = "level_start";
        public const string tutorial_complete = "tutorial_complete";
        public const string use_prop = "use_prop";
        public const string virtual_resource_transaction = "virtual_resource_transaction";

        public static readonly IReadOnlyDictionary<string, string> AdjustTokens = new Dictionary<string, string>
        {
            { ad_clicked, "fh3w6q" },
            { ad_view, "m86nqg" },
            { app_open, "6a4hxu" },
            { energy_depleted, "whpbp9" },
            { level_complete, "hk8uuo" },
            { level_start, "yxdh0f" },
            { tutorial_complete, "72lzj8" },
            { use_prop, "oravg9" },
            { virtual_resource_transaction, "so5zc4" }
        };
    }

    public static class AdNetworkParams
    {
        public const string value = "value";
        public const string resource_type = "resource_type";
    }

    public static class ResourceType
    {
        public const string Coins = "COINS";
        public const string Gems = "GEMS";
        public const string Energy = "ENERGY";
        public const string Hearts = "HEARTS";
        public const string Xp = "XP";
        public const string Tokens = "TOKENS";
        public const string Keys = "KEYS";
        public const string Materials = "MATERIALS";
        public const string Tickets = "TICKETS";
        public const string Other = "OTHER";
    }
}
