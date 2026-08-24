using System;
using CaskFramework.Assets;
using CaskFramework.Audio;
using CaskFramework.Core;
using CaskFramework.Haptic;
using CaskFramework.Config;
using CaskFramework.Profile;
using CaskFramework.Save;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;
using CaskFramework.Ads;
using CaskFramework.Analytics;

namespace Cast.Game
{
    public class Bootloader : MonoBehaviour
    {
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private GameSceneEntry _gameEntry;
        [SerializeField] private float _splashSeconds = 3f;

        private void Start()
        {
            GameRuntime.Register<IUIManager>(_uiManager);
            GameRuntime.Register<IAssetManager>(new AssetManager());
            GameRuntime.Register<IProfileService>(new ProfileService());
            GameRuntime.Register<IHapticService>(new HapticService());
            GameRuntime.Register<ISaveService>(new SaveService());
            GameRuntime.Register<IConfigService>(new ConfigService());

            FeatureManager.Init(GameRuntime.Context);

            var profile = GameRuntime.Get<IProfileService>();
            if (profile != null)
            {
                if (profile.PlayedSession < 1)
                {
                    profile.SetBalance("Coin", 100);
                    profile.SetBalance(BoosterType.Hint, 3);
                    profile.SetBalance(BoosterType.Reveal, 3);
                }
                profile.IncrementPlayedSession();
            }

            StartGameFlowAsync().Forget();
        }

        private async UniTask StartGameFlowAsync()
        {
            await ShowSplashSceenAsync();

            if (_gameEntry == null) return;
            var profile = GameRuntime.Get<IProfileService>();
            if (profile == null) return;

            await UniTask.WaitUntil(() => _gameEntry.IsInitialized);

            if (profile.ProgressLevel > 1)
                _gameEntry.StartInHome();
            else
                _gameEntry.StartInLevel();

            await UniTask.NextFrame();
            GameRuntime.Get<IUIManager>().PopTopView();
        }

        private async UniTask ShowSplashSceenAsync()
        {
            GameAnalytics.LoadingStart(AnalyticsValues.loading_type_app, Application.version);
            AdNetworkTracker.Track(AdNetworkEvents.app_open);

            var ui = GameRuntime.Get<IUIManager>();
            ViewSplashScreen splashView = null;
            await ui.PushTopViewAsync<ViewSplashScreen>("ViewSplashScreen", stack: false, onLoad: (_, view) =>
            {
                splashView = view;
            });

            if (splashView == null)
            {
                GameAnalytics.LoadingEnd(AnalyticsValues.loading_type_app, Application.version, true, "Splash view failed to load");
                return;
            }

            float stepDuration = _splashSeconds / 4f;
            for (int i = 1; i <= 4; i++)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(stepDuration));
                splashView.SetPercentage(i / 4f);
            }

            GameAnalytics.LoadingEnd(AnalyticsValues.loading_type_app, Application.version, false);
        }
    }
}