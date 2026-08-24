using CaskFramework.Analytics;
using CaskFramework.Core;
using CaskFramework.Plugins;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{
    public sealed class AdjustPlugin : GamePlugin<AdjustPluginConfig>
    {
        private const string SdkGameObjectName = "Adjust";

        private AdjustAnalyticsProvider _provider;
        private AdjustSdk.Adjust _sdkComponent;

        public AdjustPlugin(AdjustPluginConfig config) : base(config) { }

        public override string PluginId => "Adjust";

        public AdjustAnalyticsProvider Provider => _provider;
        public AdjustSdk.Adjust SdkBehaviour => _sdkComponent;

        protected override UniTask OnInitializeAsync(IServiceContext context)
        {
            _sdkComponent = CaskFramework.Plugins.SdkComponent.EnsureExists<AdjustSdk.Adjust>(
                SdkGameObjectName,
                component => component.startManually = true);

            if (_sdkComponent == null)
            {
                Debug.LogError("[AdjustPlugin] Could not obtain the Adjust SDK component, initialization skipped.");
                return UniTask.CompletedTask;
            }

            if (!_sdkComponent.startManually)
            {
                Debug.LogError($"[AdjustPlugin] The scene '{_sdkComponent.gameObject.name}' component has Start SDK Manually unchecked, " +
                               "so the SDK already initialized itself from the inspector token. Tick it to let the plugin own initialization.");
                return UniTask.CompletedTask;
            }

            _provider = new AdjustAnalyticsProvider(Config.Settings);
            _provider.Configure(AnalyticsEvents.ad_impression, AnalyticsSchema.AdjustRevenueParamKeys());
            _provider.Init();

            if (PluginManager.TryGet(out AnalyticsPlugin analytics))
                analytics.AddProvider(_provider);
            else
                Debug.LogError("[AdjustPlugin] AnalyticsPlugin is not registered, ad revenue will not reach Adjust.");

            return UniTask.CompletedTask;
        }
    }
}
