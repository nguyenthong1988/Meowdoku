using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Cast.Game
{
    public sealed class CellBackgroundView : MonoBehaviour, ICellMarkView
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private float _shakeOffset = 0.1f;
        [SerializeField] private float _shakeStepDuration = 0.083f;

        private MotionHandle _alphaHandle;
        private MotionHandle _shakeHandle;

        public void SetSprite(Sprite sprite)
        {
            if (_renderer != null) _renderer.sprite = sprite;
        }

        public void SetSortOrder(int order)
        {
            if (_renderer != null) _renderer.sortingOrder = order;
        }

        public void SetSortingLayer(string layerName)
        {
            if (_renderer != null) _renderer.sortingLayerID = SortingLayer.NameToID(layerName);
        }

        public void ResetInstant()
        {
            CancelAll();
            SetAlphaInstant(0f);
            ResetPositionInstant();
        }

        public void Show(bool ghost)
        {
            AnimateAlpha(1f);
        }

        public void Hide()
        {
            AnimateAlpha(0f);
        }

        public void AnimateIn(float duration, float delay)
        {
            if (_renderer == null) return;
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();
            _alphaHandle = LMotion.Create(0f, 1f, duration).WithDelay(delay)
                .BindToColorA(_renderer)
                .AddTo(_renderer.gameObject);
        }

        public void Shake()
        {
            if (_renderer == null) return;
            if (_shakeHandle.IsActive()) _shakeHandle.Cancel();

            Transform t = _renderer.transform;
            float baseX = t.localPosition.x;
            SetPositionXInstant(t, baseX + _shakeOffset);

            _shakeHandle = LSequence.Create()
                .Append(LMotion.Create(baseX + _shakeOffset, baseX - _shakeOffset, _shakeStepDuration).WithEase(Ease.InOutSine).BindToLocalPositionX(t))
                .Append(LMotion.Create(baseX - _shakeOffset, baseX + _shakeOffset, _shakeStepDuration).WithEase(Ease.InOutSine).BindToLocalPositionX(t))
                .Append(LMotion.Create(baseX + _shakeOffset, baseX, _shakeStepDuration).WithEase(Ease.InOutSine).BindToLocalPositionX(t))
                .Run()
                .AddTo(t.gameObject);
        }

        private static void SetPositionXInstant(Transform t, float x)
        {
            Vector3 pos = t.localPosition;
            pos.x = x;
            t.localPosition = pos;
        }

        public void ResetPositionInstant()
        {
            if (_shakeHandle.IsActive()) _shakeHandle.Cancel();
            if (_renderer == null) return;
            Vector3 pos = _renderer.transform.localPosition;
            pos.x = 0f;
            _renderer.transform.localPosition = pos;
        }

        private void AnimateAlpha(float target)
        {
            if (_renderer == null) return;
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();
            _alphaHandle = LMotion.Create(_renderer.color.a, target, _duration)
                .BindToColorA(_renderer)
                .AddTo(_renderer.gameObject);
        }

        private void CancelAll()
        {
            if (_alphaHandle.IsActive()) _alphaHandle.Cancel();
            if (_shakeHandle.IsActive()) _shakeHandle.Cancel();
        }

        private void SetAlphaInstant(float a)
        {
            if (_renderer == null) return;
            Color c = _renderer.color;
            c.a = a;
            _renderer.color = c;
        }
    }
}
