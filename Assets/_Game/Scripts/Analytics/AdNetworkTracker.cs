using System.Collections.Generic;
using UnityEngine;

namespace Cast.Game
{
    public static class AdNetworkTracker
    {
        public static void Track(string eventName, Dictionary<string, string> parameters = null)
        {
            TrackAdjust(eventName, parameters);
            TrackMax(eventName, parameters);
        }

        public static void TrackLevelStart(int level)
        {
            Track(AdNetworkEvents.level_start, new Dictionary<string, string>
            {
                { AdNetworkParams.value, level.ToString() }
            });
        }

        public static void TrackLevelComplete(int level)
        {
            Track(AdNetworkEvents.level_complete, new Dictionary<string, string>
            {
                { AdNetworkParams.value, level.ToString() }
            });
        }

        public static void TrackVirtualResourceTransaction(int amount, string resourceType = null)
        {
            Dictionary<string, string> parameters = new()
            {
                { AdNetworkParams.value, amount.ToString() }
            };

            if (!string.IsNullOrEmpty(resourceType))
                parameters.Add(AdNetworkParams.resource_type, resourceType);

            Track(AdNetworkEvents.virtual_resource_transaction, parameters);
        }

        private static void TrackAdjust(string eventName, Dictionary<string, string> parameters)
        {
            if (!AdNetworkEvents.AdjustTokens.TryGetValue(eventName, out string token) || string.IsNullOrEmpty(token))
            {
                Debug.LogWarning($"[AdNetworkTracker] No Adjust token mapped for event '{eventName}'.");
                return;
            }

            AdjustSdk.AdjustEvent adjustEvent = new(token);

            if (parameters != null)
            {
                foreach (KeyValuePair<string, string> parameter in parameters)
                    adjustEvent.AddCallbackParameter(parameter.Key, parameter.Value);
            }

            AdjustSdk.Adjust.TrackEvent(adjustEvent);
        }

        private static void TrackMax(string eventName, Dictionary<string, string> parameters)
        {
            MaxSdk.TrackEvent(eventName, parameters);
        }
    }
}
