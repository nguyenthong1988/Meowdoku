using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Cast.Game
{
    public sealed class HintMarkView : MonoBehaviour, ICellMarkView
    {
        [SerializeField] private SpriteRenderer _left;
        [SerializeField] private SpriteRenderer _right;
        [SerializeField] private float _ghostAlpha = 0.45f;
        [SerializeField] private float _fadeDuration = 0.1f;
        [SerializeField] private float _leftOpenDuration = 0.08f;
        [SerializeField] private float _rightDelay = 0.08f;
        [SerializeField] private float _rightOpenDuration = 0.07f;

        private MotionHandle _leftAlphaHandle;
        private MotionHandle _rightAlphaHandle;
        private MotionHandle _leftScaleHandle;
        private MotionHandle _rightScaleHandle;
        private Vector3 _leftRestScale;
        private Vector3 _rightRestScale;
        private bool _visible;

        private void Awake()
        {
            _leftRestScale = _left != null ? _left.transform.localScale : Vector3.one;
            _rightRestScale = _right != null ? _right.transform.localScale : Vector3.one;
        }

        public void SetSortOrder(int order)
        {
            if (_left != null) _left.sortingOrder = order;
            if (_right != null) _right.sortingOrder = order;
        }

        public void SetSortingLayer(string layerName)
        {
            int sortingLayerID = SortingLayer.NameToID(layerName);
            if (_left != null) _left.sortingLayerID = sortingLayerID;
            if (_right != null) _right.sortingLayerID = sortingLayerID;
        }

        public void ResetInstant()
        {
            _visible = false;
            CancelAll();
            SetAlphaInstant(_left, 0f);
            SetAlphaInstant(_right, 0f);
            SetScaleXInstant(_left, 0f);
            SetScaleXInstant(_right, 0f);
        }

        public void Show(bool ghost)
        {
            float targetAlpha = ghost ? _ghostAlpha : 1f;

            if (!_visible)
            {
                _visible = true;
                SetAlphaInstant(_left, 0f);
                SetAlphaInstant(_right, 0f);
                SetScaleXInstant(_left, 0f);
                SetScaleXInstant(_right, 0f);
                AnimateReveal();
            }

            AnimateAlpha(_left, targetAlpha, ref _leftAlphaHandle);
            AnimateAlpha(_right, targetAlpha, ref _rightAlphaHandle);
        }

        public void Hide()
        {
            if (!_visible) return;
            _visible = false;
            AnimateAlpha(_left, 0f, ref _leftAlphaHandle);
            AnimateAlpha(_right, 0f, ref _rightAlphaHandle);
        }

        private void AnimateReveal()
        {
            if (_leftScaleHandle.IsActive()) _leftScaleHandle.Cancel();
            if (_left != null)
            {
                _leftScaleHandle = LMotion.Create(0f, _leftRestScale.x, _leftOpenDuration)
                    .WithEase(Ease.InOutSine)
                    .BindToLocalScaleX(_left.transform)
                    .AddTo(_left.gameObject);
            }

            if (_rightScaleHandle.IsActive()) _rightScaleHandle.Cancel();
            if (_right != null)
            {
                _rightScaleHandle = LSequence.Create()
                    .AppendInterval(_rightDelay)
                    .Append(LMotion.Create(0f, _rightRestScale.x, _rightOpenDuration).WithEase(Ease.InOutSine).BindToLocalScaleX(_right.transform))
                    .Run()
                    .AddTo(_right.gameObject);
            }
        }

        private void AnimateAlpha(SpriteRenderer sr, float target, ref MotionHandle handle)
        {
            if (sr == null) return;
            if (handle.IsActive()) handle.Cancel();
            handle = LMotion.Create(sr.color.a, target, _fadeDuration)
                .BindToColorA(sr)
                .AddTo(sr.gameObject);
        }

        private void CancelAll()
        {
            if (_leftAlphaHandle.IsActive()) _leftAlphaHandle.Cancel();
            if (_rightAlphaHandle.IsActive()) _rightAlphaHandle.Cancel();
            if (_leftScaleHandle.IsActive()) _leftScaleHandle.Cancel();
            if (_rightScaleHandle.IsActive()) _rightScaleHandle.Cancel();
        }

        private static void SetAlphaInstant(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            Color c = sr.color;
            c.a = a;
            sr.color = c;
        }

        private static void SetScaleXInstant(SpriteRenderer sr, float x)
        {
            if (sr == null) return;
            Vector3 scale = sr.transform.localScale;
            scale.x = x;
            sr.transform.localScale = scale;
        }
    }
}
