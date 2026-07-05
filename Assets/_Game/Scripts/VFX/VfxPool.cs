using System.Collections.Generic;
using UnityEngine;

namespace Cast.Game
{
    public sealed class VfxPool
    {
        private readonly VfxPoolConfig _config;
        private readonly Transform _parent;
        private readonly List<VfxInstance> _idle = new List<VfxInstance>();

        public VfxPool(VfxPoolConfig config, Transform parent)
        {
            _config = config;
            _parent = parent;
            LastUsedTime = Time.unscaledTime;
        }

        public VfxPoolConfig Config => _config;
        public float LastUsedTime { get; private set; }

        public VfxInstance Rent(Vector3 position, Quaternion rotation)
        {
            if (_config.Prefab == null) return null;

            LastUsedTime = Time.unscaledTime;

            VfxInstance instance = TakeIdle();
            if (instance == null)
                instance = CreateInstance();
            if (instance == null) return null;

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            instance.Play();
            return instance;
        }

        public void Return(VfxInstance instance, bool cachingAllowed)
        {
            if (instance == null) return;

            instance.Stop();
            instance.gameObject.SetActive(false);

            if (!cachingAllowed || _idle.Count >= _config.MaxPoolSize)
            {
                DestroyInstance(instance);
                return;
            }

            _idle.Add(instance);
        }

        public void Clean()
        {
            for (int i = 0; i < _idle.Count; i++)
                DestroyInstance(_idle[i]);
            _idle.Clear();
        }

        private VfxInstance TakeIdle()
        {
            while (_idle.Count > 0)
            {
                int last = _idle.Count - 1;
                VfxInstance candidate = _idle[last];
                _idle.RemoveAt(last);
                if (candidate == null) continue;
                if (candidate.IsAlive) continue;
                return candidate;
            }
            return null;
        }

        private VfxInstance CreateInstance()
        {
            GameObject go = Object.Instantiate(_config.Prefab, _parent);

            VfxInstance instance = go.GetComponent<VfxInstance>();
            if (instance == null)
            {
                if (go.GetComponentInChildren<ParticleSystem>() != null)
                    instance = go.AddComponent<ParticleVfx>();
            }

            if (instance == null)
            {
                Object.Destroy(go);
                return null;
            }

            go.SetActive(false);
            return instance;
        }

        private static void DestroyInstance(VfxInstance instance)
        {
            if (instance != null)
                Object.Destroy(instance.gameObject);
        }
    }
}
