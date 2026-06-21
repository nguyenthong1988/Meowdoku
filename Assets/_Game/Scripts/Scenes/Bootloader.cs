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
        private void Start()
        {
            GameRuntime.Register<IUIManager>(_uiManager);
            GameRuntime.Register<IAssetManager>(new AssetManager());
            GameRuntime.Register<IProfileService>(new ProfileService());
            ShowSplashSceenAsync().Forget();
        }

        private async UniTask ShowSplashSceenAsync()
        {
            await GameRuntime.Get<IUIManager>().PushViewAsync("ViewSplashScreen", stack: false);
        }
    }
}