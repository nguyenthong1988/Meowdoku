using CaskFramework.Analytics;
using CaskFramework.Core;
using CaskFramework.Plugins;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class AnalyticsPlugin : GamePlugin<AnalyticsPluginConfig>
    {
        private TrackingService _tracking;
        private AnalyticsUserPropertyService _userProperties;
        private AdsAnalyticsBinder _adsBinder;
        private AdNetworkAdsBinder _adNetworkAdsBinder;

        public AnalyticsPlugin(AnalyticsPluginConfig config) : base(config) { }

        public override string PluginId => "Analytics";

        public ITrackingService Tracking => _tracking;
        public AnalyticsUserPropertyService UserProperties => _userProperties;

        public void AddProvider(IAnalyticsProvider provider)
        {
            _tracking?.AddProvider(provider);
        }

        protected override UniTask OnInitializeAsync(IServiceContext context)
        {
            _tracking = new TrackingService();
            GameRuntime.Register<ITrackingService>(_tracking);

#if CASK_FIREBASE
            _tracking.AddProvider(new FirebaseAnalyticsProvider(
                () => PluginManager.TryGet(out FirebasePlugin firebase) && firebase.IsReady,
                AnalyticsSchema.FirebaseFilter()));
#endif

            _userProperties = new AnalyticsUserPropertyService(_tracking);
            _userProperties.PushStartupProperties();

            _adsBinder = new AdsAnalyticsBinder(_userProperties);
            _adsBinder.Bind();

            _adNetworkAdsBinder = new AdNetworkAdsBinder();
            _adNetworkAdsBinder.Bind();

            return UniTask.CompletedTask;
        }
    }
}
