using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Cast.Game
{
    public sealed class VFXManager : MonoBehaviour
    {
        [SerializeField] private List<VfxPoolConfig> _configs = new List<VfxPoolConfig>();
        [Header("Touch Screen")]
        [SerializeField] private bool _enableTouchInput = true;
        [SerializeField] private GameObject _touchVfxPrefab;
        [SerializeField] private float _cleanupInterval = 5f;
        [SerializeField] private float _touchWorldDepth = 10f;
        [SerializeField] private bool _ignoreTouchWhenOverUI = true;

        public static VFXManager Instance { get; private set; }

        private readonly Dictionary<string, VfxPool> _pools = new Dictionary<string, VfxPool>();
        private readonly HashSet<string> _warnedUnknownIds = new HashSet<string>();
        private readonly List<ActiveVfx> _active = new List<ActiveVfx>();
        private bool _mouseWasPressed;
        private float _nextCleanupTime;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildPools();
            _nextCleanupTime = Time.unscaledTime + _cleanupInterval;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            _active.Clear();
        }

        private void Update()
        {
            if (_enableTouchInput && !HandleTouchscreen())
                HandleMouse();

            UpdateActiveInstances();
            UpdateCleanup();
        }

        private bool HandleTouchscreen()
        {
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null) return false;

            bool anyTouches = false;
            foreach (TouchControl touch in touchscreen.touches)
            {
                if (!touch.press.wasPressedThisFrame) continue;
                anyTouches = true;
                if (IsOverUI()) continue;
                PlayTouch(touch.position.ReadValue());
            }
            return anyTouches || touchscreen.touches.Count > 0;
        }

        private void HandleMouse()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            bool isPressed = mouse.leftButton.isPressed;
            if (isPressed && !_mouseWasPressed && !IsOverUI())
                PlayTouch(mouse.position.ReadValue());
            _mouseWasPressed = isPressed;
        }

        private bool IsOverUI()
        {
            if (!_ignoreTouchWhenOverUI) return false;
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        public void Play(string id)
        {
            Play(id, Vector3.zero, Quaternion.identity);
        }

        public void Play(string id, Vector3 position)
        {
            Play(id, position, Quaternion.identity);
        }

        public void Play(string id, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (!_pools.TryGetValue(id, out VfxPool pool))
            {
                WarnUnknownId(id);
                return;
            }

            VfxInstance instance = pool.Rent(position, rotation);
            if (instance == null) return;

            float lifetime = pool.Config.InstanceLifetime;
            _active.Add(new ActiveVfx
            {
                Pool = pool,
                Instance = instance,
                StopTime = lifetime > 0f ? Time.unscaledTime + lifetime : -1f,
                Stopped = lifetime <= 0f
            });
        }

        public void PlayTouch(Vector3 screenPosition)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 source = new Vector3(screenPosition.x, screenPosition.y, _touchWorldDepth);
            Vector3 world = cam.ScreenToWorldPoint(source);
            Play(VfxIds.Touch, world);
        }

        private void BuildPools()
        {
            foreach (VfxPoolConfig config in _configs)
            {
                if (config == null) continue;
                if (string.IsNullOrEmpty(config.Id)) continue;
                if (_pools.ContainsKey(config.Id)) continue;
                _pools[config.Id] = new VfxPool(config, transform);
            }

            BuildTouchPool();
        }

        private void BuildTouchPool()
        {
            if (!_enableTouchInput || _touchVfxPrefab == null) return;
            if (_pools.ContainsKey(VfxIds.Touch)) return;

            bool hasParticle = _touchVfxPrefab.GetComponentInChildren<ParticleSystem>() != null;
            bool hasInstance = _touchVfxPrefab.GetComponentInChildren<VfxInstance>() != null;
            if (!hasParticle && !hasInstance)
            {
                Debug.LogWarning("[VFXManager] Touch VFX prefab has no ParticleSystem or VfxInstance component.");
                return;
            }

            _pools[VfxIds.Touch] = new VfxPool(new VfxPoolConfig(VfxIds.Touch, _touchVfxPrefab, 3), transform);
        }

        private void UpdateActiveInstances()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ActiveVfx active = _active[i];
                VfxInstance instance = active.Instance;

                if (instance == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                if (!active.Stopped && Time.unscaledTime >= active.StopTime)
                {
                    instance.Stop();
                    active.Stopped = true;
                    _active[i] = active;
                }

                if (instance.IsAlive) continue;

                bool cachingAllowed = !IsPoolExpired(active.Pool);
                active.Pool.Return(instance, cachingAllowed);
                _active.RemoveAt(i);
            }
        }

        private void UpdateCleanup()
        {
            if (Time.unscaledTime < _nextCleanupTime) return;
            _nextCleanupTime = Time.unscaledTime + _cleanupInterval;
            CleanExpiredPools();
        }

        private void CleanExpiredPools()
        {
            foreach (KeyValuePair<string, VfxPool> entry in _pools)
            {
                if (IsPoolExpired(entry.Value))
                    entry.Value.Clean();
            }
        }

        private static bool IsPoolExpired(VfxPool pool)
        {
            VfxPoolConfig config = pool.Config;
            if (config.PersistentPool) return false;
            return Time.unscaledTime - pool.LastUsedTime >= config.PoolIdleLifetime;
        }

        private void WarnUnknownId(string id)
        {
            if (_warnedUnknownIds.Contains(id)) return;
            _warnedUnknownIds.Add(id);
            Debug.LogWarning($"[VFXManager] No pool registered for VFX id '{id}'.");
        }

        private struct ActiveVfx
        {
            public VfxPool Pool;
            public VfxInstance Instance;
            public float StopTime;
            public bool Stopped;
        }
    }
}
