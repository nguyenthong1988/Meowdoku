using System.Collections.Generic;
using UnityEngine;

namespace Cast.Game
{
    public sealed class VfxPool
    {
        private readonly VfxPoolConfig _config;
        private readonly Transform _parent;
        private readonly List<ParticleSystem> _idle = new List<ParticleSystem>();

        public VfxPool(VfxPoolConfig config, Transform parent)
        {
            _config = config;
            _parent = parent;
            LastUsedTime = Time.unscaledTime;
        }

        public VfxPoolConfig Config => _config;
        public float LastUsedTime { get; private set; }

        public ParticleSystem Rent(Vector3 position, Quaternion rotation)
        {
            if (_config.Prefab == null) return null;

            LastUsedTime = Time.unscaledTime;

            ParticleSystem instance = TakeIdle();
            if (instance == null)
                instance = CreateInstance();
            if (instance == null) return null;

            if (instance.isPlaying)
                instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.gameObject.SetActive(true);
            instance.Play(true);
            return instance;
        }

        public void Return(ParticleSystem instance, bool cachingAllowed)
        {
            if (instance == null) return;

            if (instance.isPlaying)
                instance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        private ParticleSystem TakeIdle()
        {
            while (_idle.Count > 0)
            {
                int last = _idle.Count - 1;
                ParticleSystem candidate = _idle[last];
                _idle.RemoveAt(last);
                if (candidate == null) continue;
                if (candidate.IsAlive(true)) continue;
                return candidate;
            }
            return null;
        }

        private ParticleSystem CreateInstance()
        {
            ParticleSystem instance = Object.Instantiate(_config.Prefab, _parent);
            ParticleSystem.MainModule main = instance.main;
            main.stopAction = ParticleSystemStopAction.Disable;
            instance.gameObject.SetActive(false);
            return instance;
        }

        private static void DestroyInstance(ParticleSystem instance)
        {
            if (instance != null)
                Object.Destroy(instance.gameObject);
        }
    }
}
