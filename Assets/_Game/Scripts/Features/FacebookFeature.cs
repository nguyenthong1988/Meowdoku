using CaskFramework.Core;
using Facebook.Unity;
using UnityEngine;

namespace Cast.Game
{
    public sealed class FacebookFeature : IFeature
    {
        private readonly bool _autoActivate;

        public FacebookFeature(bool autoActivate = true)
        {
            _autoActivate = autoActivate;
        }

        public void Initialize(IServiceContext context)
        {
            if (_autoActivate) CheckActivate();
        }

        public void CheckActivate()
        {
            if (!FB.IsInitialized)
            {
                FB.Init(InitCallback, OnHideUnity);
            }
            else
            {
                FB.ActivateApp();
            }
        }

        private void InitCallback()
        {
            if (FB.IsInitialized)
            {
                FB.ActivateApp();
            }
            else
            {
                Debug.LogError("[FacebookFeature] Failed to initialize the Facebook SDK.");
            }
        }

        private void OnHideUnity(bool isGameShown)
        {
            Time.timeScale = !isGameShown ? 0 : 1;
        }
    }
}
