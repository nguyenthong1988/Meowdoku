using CaskFramework.Plugins;
using UnityEngine;

namespace Cast.Game
{
    [CreateAssetMenu(menuName = "Cast/Plugins/Firebase", fileName = "FirebasePluginConfig")]
    public sealed class FirebasePluginConfig : PluginConfig
    {
        [SerializeField] private bool _enableCrashlytics = true;
        [SerializeField] private bool _reportUncaughtExceptionsAsFatal = true;

        public bool EnableCrashlytics => _enableCrashlytics;
        public bool ReportUncaughtExceptionsAsFatal => _reportUncaughtExceptionsAsFatal;

        public override IGamePlugin CreatePlugin() => new FirebasePlugin(this);
    }
}
