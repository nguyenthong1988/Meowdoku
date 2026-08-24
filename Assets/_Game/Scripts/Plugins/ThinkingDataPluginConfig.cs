using CaskFramework.Plugins;
using UnityEngine;

namespace Cast.Game
{
    [CreateAssetMenu(menuName = "Cast/Plugins/ThinkingData", fileName = "ThinkingDataPluginConfig")]
    public sealed class ThinkingDataPluginConfig : PluginConfig
    {
        [SerializeField] private string _appId;
        [SerializeField] private string _serverUrl;
        [SerializeField] private bool _enableLog;

        public string AppId => _appId;
        public string ServerUrl => _serverUrl;
        public bool EnableLog => _enableLog;

        public override IGamePlugin CreatePlugin() => new ThinkingDataPlugin(this);
    }
}
