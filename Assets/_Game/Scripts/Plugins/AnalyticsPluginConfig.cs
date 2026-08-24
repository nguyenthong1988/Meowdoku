using CaskFramework.Plugins;
using UnityEngine;

namespace Cast.Game
{
    [CreateAssetMenu(menuName = "Cast/Plugins/Analytics", fileName = "AnalyticsPluginConfig")]
    public sealed class AnalyticsPluginConfig : PluginConfig
    {
        public override IGamePlugin CreatePlugin() => new AnalyticsPlugin(this);
    }
}
