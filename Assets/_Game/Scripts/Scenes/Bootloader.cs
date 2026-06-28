using System;
using CaskFramework.Assets;
using CaskFramework.Core;
using CaskFramework.Profile;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            StartGameFlowAsync().Forget();
        }

        private async UniTask StartGameFlowAsync()
        {
            await ShowSplashSceenAsync();

            if (_gameEntry == null) return;
            var profile = GameRuntime.Get<IProfileService>();
            if (profile == null) return;

            if (profile.ProgressLevel > 1)
            {
                await GameRuntime.Get<IUIManager>().PushViewAsync<ViewHome>("ViewHome", stack: false, onLoad: (_, v) =>
                {
                    v.Setup(() =>
                    {
                        _gameEntry.RunFlowAsync().Forget();
                    }, profile);
                });
            }
            else
            {
                await _gameEntry.RunFlowAsync();
            }

            await UniTask.NextFrame();
            GameRuntime.Get<IUIManager>().PopTopView();
        }

        private async UniTask ShowSplashSceenAsync()
        {
            var ui = GameRuntime.Get<IUIManager>();
            ViewSplashScreen splashView = null;
            await ui.PushTopViewAsync<ViewSplashScreen>("ViewSplashScreen", stack: false, onLoad: (_, view) =>
            {
                splashView = view;
            });
            float stepDuration = _splashSeconds / 4f;
            for (int i = 1; i <= 4; i++)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(stepDuration));
                if (splashView != null) splashView.SetPercentage(i / 4f);
            }
        }
    }
}