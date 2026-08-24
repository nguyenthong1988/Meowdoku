using CaskFramework.Analytics;
using CaskFramework.Plugins;
using UnityEngine;

namespace Cast.Game
{
    [CreateAssetMenu(menuName = "Cast/Plugins/Adjust", fileName = "AdjustPluginConfig")]
    public sealed class AdjustPluginConfig : PluginConfig
    {
        [SerializeField] private AdjustSettings _settings = new();

        public AdjustSettings Settings => _settings;

        public override IGamePlugin CreatePlugin() => new AdjustPlugin(this);
    }
}
