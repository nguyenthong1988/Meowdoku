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
        [SerializeField] private float _splashSeconds = 3f;

        private void Start()
        {
            GameRuntime.Register<IUIManager>(_uiManager);
            GameRuntime.Register<IAssetManager>(new AssetManager());
            GameRuntime.Register<IProfileService>(new ProfileService());
            ShowSplashSceenAsync().Forget();
        }

        private async UniTask ShowSplashSceenAsync()
        {
            var ui = GameRuntime.Get<IUIManager>();
            await ui.PushTopViewAsync("ViewSplashScreen", stack: false);
            await UniTask.Delay(System.TimeSpan.FromSeconds(_splashSeconds));
            await ui.PopTopViewAsync();
        }
    }
}