using CaskFramework.Core;
using CaskFramework.Plugins;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if CASK_FACEBOOK
using Facebook.Unity;
#endif

namespace Cast.Game
{
    public sealed class FacebookPlugin : GamePlugin<FacebookPluginConfig>
    {
        public FacebookPlugin(FacebookPluginConfig config) : base(config) { }

        public override string PluginId => "Facebook";

#if CASK_FACEBOOK
        public bool IsReady => FB.IsInitialized;

        public void ActivateApp()
        {
            if (FB.IsInitialized) FB.ActivateApp();
        }

        protected override async UniTask OnInitializeAsync(IServiceContext context)
        {
            if (FB.IsInitialized)
            {
                FB.ActivateApp();
                return;
            }

            bool completed = false;
            FB.Init(() => completed = true, OnHideUnity);

            float deadline = Time.realtimeSinceStartup + Mathf.Max(1f, Config.InitTimeoutSeconds);
            await UniTask.WaitUntil(() => completed || Time.realtimeSinceStartup >= deadline);

            if (!FB.IsInitialized)
            {
                Debug.LogError("[FacebookPlugin] Failed to initialize the Facebook SDK.");
                return;
            }

            if (Config.AutoActivateApp)
                FB.ActivateApp();
        }

        private void OnHideUnity(bool isGameShown)
        {
            Time.timeScale = isGameShown ? 1f : 0f;
        }
#else
        public bool IsReady => false;

        public void ActivateApp() { }

        protected override UniTask OnInitializeAsync(IServiceContext context)
        {
            Debug.LogWarning("[FacebookPlugin] CASK_FACEBOOK is not defined, the Facebook SDK is disabled for this build.");
            return UniTask.CompletedTask;
        }
#endif
    }
}
