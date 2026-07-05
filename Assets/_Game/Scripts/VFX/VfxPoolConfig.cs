using System;
using UnityEngine;

namespace Cast.Game
{
    [Serializable]
    public class VfxPoolConfig
    {
        [SerializeField] private string _id;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private int _maxPoolSize = 8;
        [SerializeField] private float _instanceLifetime;
        [SerializeField] private bool _persistentPool;
        [SerializeField] private float _poolIdleLifetime = 30f;

        public VfxPoolConfig() { }

        public VfxPoolConfig(string id, GameObject prefab, int maxPoolSize, float instanceLifetime = 0f, bool persistentPool = false, float poolIdleLifetime = 30f)
        {
            _id = id;
            _prefab = prefab;
            _maxPoolSize = maxPoolSize;
            _instanceLifetime = instanceLifetime;
            _persistentPool = persistentPool;
            _poolIdleLifetime = poolIdleLifetime;
        }

        public string Id => _id;
        public GameObject Prefab => _prefab;
        public int MaxPoolSize => _maxPoolSize;
        public float InstanceLifetime => _instanceLifetime;
        public bool PersistentPool => _persistentPool;
        public float PoolIdleLifetime => _poolIdleLifetime;
    }
}
