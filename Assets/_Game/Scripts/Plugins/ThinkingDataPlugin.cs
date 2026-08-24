using CaskFramework.Core;
using CaskFramework.Plugins;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{
    public sealed class ThinkingDataPlugin : GamePlugin<ThinkingDataPluginConfig>
    {
        public ThinkingDataPlugin(ThinkingDataPluginConfig config) : base(config) { }

        public override string PluginId => "ThinkingData";

#if THINKING_DATA
        private ThinkingDataAnalyticsProvider _provider;

        public ThinkingDataAnalyticsProvider Provider => _provider;

        protected override UniTask OnInitializeAsync(IServiceContext context)
        {
            if (string.IsNullOrEmpty(Config.AppId) || string.IsNullOrEmpty(Config.ServerUrl))
            {
                Debug.LogError("[ThinkingDataPlugin] Missing app id or server url, initialization skipped.");
                return UniTask.CompletedTask;
            }

            _provider = new ThinkingDataAnalyticsProvider(Config.AppId, Config.ServerUrl, Config.EnableLog);
            _provider.Init();

            if (PluginManager.TryGet(out AnalyticsPlugin analytics))
                analytics.AddProvider(_provider);
            else
                Debug.LogError("[ThinkingDataPlugin] AnalyticsPlugin is not registered, events will not reach ThinkingData.");

            return UniTask.CompletedTask;
        }
#else
        protected override UniTask OnInitializeAsync(IServiceContext context)
        {
            Debug.LogWarning("[ThinkingDataPlugin] THINKING_DATA is not defined, the ThinkingData SDK is disabled for this build.");
            return UniTask.CompletedTask;
        }
#endif
    }
}
