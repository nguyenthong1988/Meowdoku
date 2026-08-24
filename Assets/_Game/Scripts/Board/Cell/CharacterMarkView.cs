using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Cast.Game
{
    public sealed class CharacterMarkView : MonoBehaviour, ICellMarkView
    {
        private static readonly int IdleStateHash = Animator.StringToHash("Character_Idle");
        private static readonly int AppearStateHash = Animator.StringToHash("Character_Appear");
        private static readonly int BlinkStateHash = Animator.StringToHash("Character_Blink");
        private static readonly int TongueLeftStateHash = Animator.StringToHash("Character_TongueLeft");
        private static readonly int TongueRightStateHash = Animator.StringToHash("Character_TongueRight");

        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _ghostAlpha = 0.45f;
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private Animator _animator;
        [SerializeField] private Vector2 _firstActionDelayRange = new Vector2(1.5f, 3.5f);
        [SerializeField] private Vector2 _idleActionDelayRange = new Vector2(2.5f, 7f);
        [SerializeField, Range(0f, 1f)] private float _tongueChance = 0.3f;

        private MotionHandle _alphaHandle;
        private MotionHandle _scaleHandle;
        private Vector3 _restScale;
        private bool _visible;
        private SpriteRenderer[] _overlayRenderers = Array.Empty<SpriteRenderer>();
        private int[] _overlaySortOrderOffsets = Array.Empty<int>();
        private CancellationTokenSource _idleActionCancellation;

        private void Awake()
        {
            _restScale = _renderer != null ? _renderer.transform.localScale : Vector3.one;
            _overlayRenderers = CollectOverlayRenderers();
            _overlaySortOrderOffsets = MeasureSortOrderOffsets(_overlayRenderers);
        }

        private void OnDestroy()
        {
            StopIdleActionLoop();
        }

        private SpriteRenderer[] CollectOverlayRenderers()
        {
            if (_renderer == null) return Array.Empty<SpriteRenderer>();

            SpriteRenderer[] all = _renderer.GetComponentsInChildren<SpriteRenderer>(true);
            int count = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != _renderer) count++;

            var overlays = new SpriteRenderer[count];
            int index = 0;
            for (int i = 0; i < all.Length; i++)
                if (all[i] != _renderer) overlays[index++] = all[i];

            return overlays;
        }

        private int[] MeasureSortOrderOffsets(SpriteRenderer[] renderers)
        {
            var offsets = new int[renderers.Length];
            int baseOrder = _renderer != null ? _renderer.sortingOrder : 0;

            for (int i = 0; i < renderers.Length; i++)
                offsets[i] = Mathf.Max(1, renderers[i].sortingOrder - baseOrder);

            return offsets;
        }

        public void SetSortOrder(int order)
        {
            if (_renderer != null) _renderer.sortingOrder = order;
            for (int i = 0; i < _overlayRenderers.Length; i++)
                _overlayRenderers[i].sortingOrder = order + _overlaySortOrderOffsets[i];
        }

        public void SetSortingLayer(string layerName)
        {
            int sortingLayerID = SortingLayer.NameToID(layerName);
            if (_renderer != null) _renderer.sortingLayerID = sortingLayerID;
            for (int i = 0; i < _overlayRenderers.Length; i++)
                _overlayRenderers[i].sortingLayerID = sortingLayerID;
        }

        public void ResetInstant()
        {
            _visible = false;
            CancelAll();
            StopIdleActionLoop();
            SetAlphaInstant(0f);
            PlayAnimatorState(IdleStateHash);
            if (_animator != null) _animator.enabled = false;
        }

        public void Show(bool ghost)
        {
            float targetAlpha = ghost ? _ghostAlpha : 1f;

            if (!_visible)
            {
                _visible = true;
                SetAlphaInstant(0f);
                if (_renderer != null) _renderer.transform.localScale = Vector3.zero;
                AnimateScale();
                PlayAnimatorState(AppearStateHash);
                StartIdleActionLoop();
            }

            AnimateAlpha(targetAlpha);
        }

        public void Hide()
        {
            if (!_visible) return;
            _visible = false;
            StopIdleActionLoop();
            AnimateAlpha(0f);
        }

        private void StartIdleActionLoop()
        {
            StopIdleActionLoop();
            _idleActionCancellation = new CancellationTokenSource();
            RunIdleActionLoopAsync(_idleActionCancellation.Token).Forget();
        }

        private void StopIdleActionLoop()
        {
            if (_idleActionCancellation == null) return;
            _idleActionCancellation.Cancel();
            _idleActionCancellation.Dispose();
            _idleActionCancellation = null;
        }

        private async UniTaskVoid RunIdleActionLoopAsync(CancellationToken token)
        {
            try
            {
                Vector2 delayRange = _firstActionDelayRange;

                while (!token.IsCancellationRequested)
                {
                    float delay = UnityEngine.Random.Range(delayRange.x, delayRange.y);
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);
                    PlayAnimatorState(PickIdleActionState());
                    delayRange = _idleActionDelayRange;
                }
            }
            catch (OperationCanceledException) { }
        }

        private int PickIdleActionState()
        {
            if (UnityEngine.Random.value >= _tongueChance) return BlinkStateHash;
            return UnityEngine.Random.value < 0.5f ? TongueLeftStateHash : TongueRightStateHash;
        }

        private void PlayAnimatorState(int stateHash)
        {
            if (_animator == null) return;

            if (!_animator.enabled)
            {
                _animator.enabled = true;
                _animator.Rebind();
            }

            _animator.Play(stateHash, 0, 0f);
            _animator.Update(0f);
        }

        private void AnimateAlpha(float target)
        {
            if (_renderer == null) return;
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();
            _alphaHandle = LMotion.Create(_renderer.color.a, target, _duration)
                .Bind(this, (alpha, view) => view.SetAlphaInstant(alpha))
                .AddTo(_renderer.gameObject);
        }

        private void AnimateScale()
        {
            if (_renderer == null) return;
            if (_scaleHandle.IsActive()) _scaleHandle.Cancel();
            _scaleHandle = LMotion.Create(_renderer.transform.localScale, _restScale, _duration)
                .WithEase(Ease.OutBack)
                .BindToLocalScale(_renderer.transform)
                .AddTo(_renderer.gameObject);
        }

        private void CancelAll()
        {
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();
            if (_scaleHandle.IsActive()) _scaleHandle.Cancel();
        }

        private void SetAlphaInstant(float alpha)
        {
            if (_renderer != null)
            {
                Color color = _renderer.color;
                color.a = alpha;
                _renderer.color = color;
            }

            for (int i = 0; i < _overlayRenderers.Length; i++)
            {
                Color color = _overlayRenderers[i].color;
                color.a = alpha;
                _overlayRenderers[i].color = color;
            }
        }
    }
}
