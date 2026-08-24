using CaskFramework.Core;
using CaskFramework.Plugins;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if CASK_FIREBASE
using Firebase;
using Firebase.Crashlytics;
#endif

namespace Cast.Game
{
    public sealed class FirebasePlugin : GamePlugin<FirebasePluginConfig>
    {
        private bool _isReady;

        public FirebasePlugin(FirebasePluginConfig config) : base(config) { }

        public override string PluginId => "Firebase";

        public bool IsReady => _isReady;

#if CASK_FIREBASE
        protected override async UniTask OnInitializeAsync(IServiceContext context)
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (status != DependencyStatus.Available)
            {
                Debug.LogError($"[FirebasePlugin] Could not resolve all Firebase dependencies: {status}");
                return;
            }

            if (Config.EnableCrashlytics)
            {
                Crashlytics.IsCrashlyticsCollectionEnabled = true;
                Crashlytics.ReportUncaughtExceptionsAsFatal = Config.ReportUncaughtExceptionsAsFatal;
            }

            _isReady = true;
        }
#else
        protected override UniTask OnInitializeAsync(IServiceContext context)
        {
            Debug.LogWarning("[FirebasePlugin] CASK_FIREBASE is not defined, the Firebase SDK is disabled for this build.");
            return UniTask.CompletedTask;
        }
#endif
    }
}
