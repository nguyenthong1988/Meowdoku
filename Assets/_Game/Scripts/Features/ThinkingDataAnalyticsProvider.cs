#if THINKING_DATA
using System.Collections.Generic;
using CaskFramework.Analytics;
using ThinkingData.Analytics;
using UnityEngine;

namespace Cast.Game
{
    public class ThinkingDataAnalyticsProvider : MonoBehaviour, IAnalyticsProvider
    {
        [SerializeField] private string _appId;
        [SerializeField] private string _serverUrl;

        private bool _isInitialized;

        public void Init()
        {
            if (_isInitialized) return;
            TDAnalytics.Init(_appId, _serverUrl);
            TDAnalytics.EnableAutoTrack(
                TDAutoTrackEventType.AppInstall |
                TDAutoTrackEventType.AppStart |
                TDAutoTrackEventType.AppEnd);
            _isInitialized = true;
        }

        public void ForceLogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!_isInitialized) return;
            TDAnalytics.Track(eventName, parameters);
        }

        public void LogEvent(string eventName, Dictionary<string, object> parameters)
        {
            if (!_isInitialized) return;
            TDAnalytics.Track(eventName, parameters);
        }

        public void SetUserProperty(string name, string value)
        {
            if (!_isInitialized) return;
            TDAnalytics.UserSet(new Dictionary<string, object> { { name, value } });
        }
    }
}
#endif
