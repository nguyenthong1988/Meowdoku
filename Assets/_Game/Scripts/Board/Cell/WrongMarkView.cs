using System.Threading;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Cast.Game
{
    public sealed class WrongMarkView : MonoBehaviour, ICellMarkView
    {
        [SerializeField] private SpriteRenderer _left;
        [SerializeField] private SpriteRenderer _right;
        [SerializeField] private CellBackgroundView _background;
        [SerializeField] private float _markScale = 0.55f;
        [SerializeField] private float _phaseDuration = 0.25f;

        private MotionHandle _leftHandle;
        private MotionHandle _rightHandle;
        private CancellationTokenSource _cts;
        private bool _visible;

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
            CancelToken();
            CancelAll();
            HideInstant(_left);
            HideInstant(_right);
            if (_background != null) _background.ResetPositionInstant();
        }

        public void Show(bool ghost)
        {
            if (_visible) return;
            _visible = true;

            RenewToken();
            if (_background != null) _background.Shake();
            AnimateFlip(_left, 1f, ref _leftHandle, _cts.Token, hasHold: false);
            AnimateFlip(_right, -1f, ref _rightHandle, _cts.Token, hasHold: true);
        }

        public void Hide()
        {
            if (!_visible) return;
            _visible = false;

            CancelToken();
            CancelAll();
            HideInstant(_left);
            HideInstant(_right);
            if (_background != null) _background.ResetPositionInstant();
        }

        private void AnimateFlip(SpriteRenderer sr, float sign, ref MotionHandle handle, CancellationToken token, bool hasHold)
        {
            if (sr == null) return;
            if (handle.IsActive()) handle.Cancel();

            sr.gameObject.SetActive(false);
            SetScaleXInstant(sr, _markScale);

            var sequence = LSequence.Create()
                .Append(LMotion.Create(_markScale, 0f, _phaseDuration).WithEase(Ease.InOutSine)
                    .WithOnComplete(() =>
                    {
                        if (token.IsCancellationRequested) return;
                        sr?.gameObject.SetActive(true);
                    })
                    .BindToLocalScaleX(sr.transform));

            if (hasHold) sequence = sequence.AppendInterval(_phaseDuration);

            handle = sequence
                .Append(LMotion.Create(0f, _markScale * sign, _phaseDuration).WithEase(Ease.InOutSine).BindToLocalScaleX(sr.transform))
                .Run()
                .AddTo(sr.gameObject);
        }

        private void RenewToken()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
        }

        private void CancelToken()
        {
            if (_cts == null) return;
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        private void CancelAll()
        {
            if (_leftHandle.IsActive()) _leftHandle.Cancel();
            if (_rightHandle.IsActive()) _rightHandle.Cancel();
        }

        private static void HideInstant(SpriteRenderer sr)
        {
            if (sr == null) return;
            sr.gameObject.SetActive(false);
        }

        private static void SetScaleXInstant(SpriteRenderer sr, float x)
        {
            Vector3 scale = sr.transform.localScale;
            scale.x = x;
            sr.transform.localScale = scale;
        }
    }
}
