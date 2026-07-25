using System;
using System.Collections.Generic;
using CaskFramework.Core;
using UnityEngine;

namespace Cast.Game
{
    public static class FeatureManager
    {
        private static readonly Dictionary<Type, IFeature> s_features = new Dictionary<Type, IFeature>();
        private static IServiceContext s_context;

        public static void Init(IServiceContext context)
        {
            s_context = context;
        }

        public static void Add(IFeature feature)
        {
            if (feature == null) return;
            s_features[feature.GetType()] = feature;
            feature.Initialize(s_context);
        }

        public static void Remove<T>() where T : class, IFeature
        {
            s_features.Remove(typeof(T));
        }

        public static T Get<T>() where T : class, IFeature
        {
            if (TryGet(out T feature)) return feature;
            Debug.LogError($"[FeatureManager] Feature {typeof(T).Name} is not registered.");
            return null;
        }

        public static bool TryGet<T>(out T feature) where T : class, IFeature
        {
            if (s_features.TryGetValue(typeof(T), out IFeature stored))
            {
                feature = (T)stored;
                return true;
            }

            foreach (IFeature candidate in s_features.Values)
            {
                if (candidate is T match)
                {
                    feature = match;
                    return true;
                }
            }

            feature = null;
            return false;
        }
    }
}
