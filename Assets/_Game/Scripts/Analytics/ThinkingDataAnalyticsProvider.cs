#if THINKING_DATA
using System.Collections.Generic;
using CaskFramework.Analytics;
using ThinkingData.Analytics;

namespace Cast.Game
{
    public class ThinkingDataAnalyticsProvider : IAnalyticsProvider
    {
        private readonly string _appId;
        private readonly string _serverUrl;
        private readonly bool _enableLog;
        private readonly Queue<PendingUserProperty> _pendingUserProperties = new();
        private readonly Queue<PendingEvent> _pendingEvents = new();

        private bool _isInitialized;

        public ThinkingDataAnalyticsProvider(string appId, string serverUrl, bool enableLog = false)
        {
            _appId = appId;
            _serverUrl = serverUrl;
            _enableLog = enableLog;
        }

        public string ProviderId => "TD";
        public bool IsInitialized => _isInitialized;

        public void Init()
        {
            if (_isInitialized) return;

            TDAnalytics.Init(_appId, _serverUrl);
            TDAnalytics.EnableLog(_enableLog);
            TDAnalytics.EnableAutoTrack(
                TDAutoTrackEventType.AppInstall |
                TDAutoTrackEventType.AppStart |
                TDAutoTrackEventType.AppEnd);
            _isInitialized = true;

            while (_pendingUserProperties.Count > 0)
            {
                PendingUserProperty property = _pendingUserProperties.Dequeue();
                TDAnalytics.UserSet(new Dictionary<string, object> { { property.Name, property.Value } });
            }

            while (_pendingEvents.Count > 0)
            {
                PendingEvent pending = _pendingEvents.Dequeue();
                TDAnalytics.Track(pending.EventName, pending.Parameters);
            }
        }

        public bool AcceptsEvent(string eventName) => AnalyticsSchema.ThinkingDataEvents.Contains(eventName);

        public bool AcceptsUserProperty(string propertyName) => AnalyticsSchema.ThinkingDataUserProperties.Contains(propertyName);

        public void LogEvent(string eventName, Dictionary<string, object> parameters, bool immediate)
        {
            if (!_isInitialized)
            {
                _pendingEvents.Enqueue(new PendingEvent(eventName, parameters));
                return;
            }

            TDAnalytics.Track(eventName, parameters);
            if (immediate) TDAnalytics.Flush();
        }

        public void SetUserProperty(string name, string value)
        {
            if (!_isInitialized)
            {
                _pendingUserProperties.Enqueue(new PendingUserProperty(name, value));
                return;
            }

            TDAnalytics.UserSet(new Dictionary<string, object> { { name, value } });
        }

        private readonly struct PendingUserProperty
        {
            public readonly string Name;
            public readonly string Value;

            public PendingUserProperty(string name, string value)
            {
                Name = name;
                Value = value;
            }
        }

        private readonly struct PendingEvent
        {
            public readonly string EventName;
            public readonly Dictionary<string, object> Parameters;

            public PendingEvent(string eventName, Dictionary<string, object> parameters)
            {
                EventName = eventName;
                Parameters = parameters;
            }
        }
    }
}
#endif
