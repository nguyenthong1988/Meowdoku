using CaskFramework.Plugins;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{
    public sealed class AttConsentGate : IConsentGate
    {
        private const int AuthorizedStatus = 3;

        private readonly float _timeoutSeconds;

        public AttConsentGate(float timeoutSeconds)
        {
            _timeoutSeconds = Mathf.Max(1f, timeoutSeconds);
        }

        public async UniTask<bool> ResolveAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            bool resolved = false;
            bool granted = false;

            AdjustSdk.Adjust.RequestAppTrackingAuthorization(status =>
            {
                granted = status == AuthorizedStatus;
                resolved = true;
            });

            float deadline = Time.realtimeSinceStartup + _timeoutSeconds;
            await UniTask.WaitUntil(() => resolved || Time.realtimeSinceStartup >= deadline);

            if (!resolved)
                Debug.LogWarning("[AttConsentGate] App Tracking Transparency timed out, continuing as denied.");

            return granted;
#else
            await UniTask.CompletedTask;
            return true;
#endif
        }
    }
}
