using System;
using CaskFramework.Analytics;
using CaskFramework.Core;
using Firebase.Crashlytics;
using Firebase.Extensions;
using UnityEngine;

namespace Cast.Game
{
    public sealed class TrackingFeature : MonoBehaviour, IFeature
    {
        [SerializeField] private bool _enableCrashlytics = true;
        [SerializeField] private AdjustAnalyticsProvider _adjustProvider;
#if THINKING_DATA
        [SerializeField] private ThinkingDataAnalyticsProvider _thinkingDataProvider;
#endif

        private IServiceContext _context;
        private bool _isFirebaseReady;

        public bool IsFirebaseReady => _isFirebaseReady;
        public AdjustAnalyticsProvider AdjustProvider => _adjustProvider;
        public event Action OnFirebaseReady;

        public void Initialize(IServiceContext context)
        {
            _context = context;
            InitFirebase();
        }

        private void InitFirebase()
        {
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result != Firebase.DependencyStatus.Available)
                {
                    Debug.LogError($"Could not resolve all Firebase dependencies: {task.Result}");
                    return;
                }

                if (_enableCrashlytics)
                {
                    Crashlytics.IsCrashlyticsCollectionEnabled = true;
                    Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                }

                _isFirebaseReady = true;
                InitTracking();
                OnFirebaseReady?.Invoke();
            });
        }

        private void InitTracking()
        {
            var composite = new CompositeAnalyticsProvider();

            composite.Add(new FirebaseAnalyticsProvider(() => _isFirebaseReady));

            if (_adjustProvider != null)
            {
                _adjustProvider.Init();
                composite.Add(_adjustProvider);
            }

#if THINKING_DATA
            if (_thinkingDataProvider != null)
            {
                _thinkingDataProvider.Init();
                composite.Add(_thinkingDataProvider);
            }
#endif

            var trackingService = new TrackingService();
            GameRuntime.Register<ITrackingService>(trackingService);
            trackingService.SetProvider(composite);
        }
    }
}
