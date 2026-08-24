using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Cast.Game
{
    public sealed class TutorialHandView : MonoBehaviour
    {
        private static readonly int IdleStateHash = Animator.StringToHash("Hand_Idle");
        private static readonly int TouchStateHash = Animator.StringToHash("Hand_Touch");
        private static readonly int DragStateHash = Animator.StringToHash("Hand_Drag");

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _scaler;
        [SerializeField] private SpriteRenderer _handRenderer;
        [SerializeField] private Vector2 _fingerTipOffsetInCells = Vector2.zero;
        [SerializeField] private float _appearDelay = 0.3f;
        [SerializeField] private float _fadeDuration = 0.15f;
        [SerializeField] private float _dragPressDuration = 0.35f;
        [SerializeField] private float _dragStepDuration = 0.5f;
        [SerializeField] private float _dragHoldDuration = 0.45f;
        [SerializeField] private float _dragRestartDelay = 0.35f;

        private CancellationTokenSource _cancellation;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            CancelRunningSequence();
        }

        public void ShowTouch(Vector3 cellWorldPosition, float cellSize)
        {
            CancellationToken token = BeginSequence(cellSize);
            RunTouchAsync(Anchor(cellWorldPosition, cellSize), token).Forget();
        }

        public void ShowDrag(IReadOnlyList<Vector3> cellWorldPositions, float cellSize)
        {
            if (cellWorldPositions == null || cellWorldPositions.Count == 0) return;

            var path = new List<Vector3>(cellWorldPositions.Count);
            for (int i = 0; i < cellWorldPositions.Count; i++)
                path.Add(Anchor(cellWorldPositions[i], cellSize));

            CancellationToken token = BeginSequence(cellSize);
            RunDragAsync(path, token).Forget();
        }

        public void Hide()
        {
            CancelRunningSequence();
            if (this == null) return;
            gameObject.SetActive(false);
        }

        private Vector3 Anchor(Vector3 cellWorldPosition, float cellSize) =>
            cellWorldPosition + new Vector3(_fingerTipOffsetInCells.x, _fingerTipOffsetInCells.y, 0f) * cellSize;

        private CancellationToken BeginSequence(float cellSize)
        {
            CancelRunningSequence();

            gameObject.SetActive(true);
            if (_scaler != null) _scaler.localScale = Vector3.one * Mathf.Max(cellSize, 0.01f);
            SetHandAlpha(0f);
            PlayState(IdleStateHash);

            _cancellation = new CancellationTokenSource();
            return _cancellation.Token;
        }

        private void CancelRunningSequence()
        {
            if (_cancellation == null) return;
            _cancellation.Cancel();
            _cancellation.Dispose();
            _cancellation = null;
        }

        private async UniTaskVoid RunTouchAsync(Vector3 anchor, CancellationToken token)
        {
            try
            {
                transform.position = anchor;
                await DelayAsync(_appearDelay, token);

                PlayState(TouchStateHash);
                await FadeHandAsync(0f, 1f, token);
            }
            catch (OperationCanceledException) { }
        }

        private async UniTaskVoid RunDragAsync(List<Vector3> path, CancellationToken token)
        {
            try
            {
                await DelayAsync(_appearDelay, token);

                while (!token.IsCancellationRequested)
                {
                    transform.position = path[0];
                    PlayState(DragStateHash);

                    await FadeHandAsync(0f, 1f, token);
                    await DelayAsync(_dragPressDuration, token);

                    for (int i = 1; i < path.Count; i++)
                        await MoveAsync(path[i - 1], path[i], token);

                    await DelayAsync(_dragHoldDuration, token);
                    await FadeHandAsync(1f, 0f, token);
                    await DelayAsync(_dragRestartDelay, token);
                }
            }
            catch (OperationCanceledException) { }
        }

        private UniTask MoveAsync(Vector3 from, Vector3 to, CancellationToken token) =>
            LMotion.Create(from, to, _dragStepDuration)
                .WithEase(Ease.InOutSine)
                .Bind(this, (position, view) => view.transform.position = position)
                .ToUniTask(token);

        private UniTask FadeHandAsync(float from, float to, CancellationToken token)
        {
            if (_handRenderer == null) return UniTask.CompletedTask;

            SetHandAlpha(from);
            return LMotion.Create(from, to, _fadeDuration)
                .Bind(this, (alpha, view) => view.SetHandAlpha(alpha))
                .ToUniTask(token);
        }

        private static UniTask DelayAsync(float seconds, CancellationToken token)
        {
            if (seconds <= 0f) return UniTask.CompletedTask;
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);
        }

        private void SetHandAlpha(float alpha)
        {
            if (_handRenderer == null) return;
            Color color = _handRenderer.color;
            color.a = alpha;
            _handRenderer.color = color;
        }

        private void PlayState(int stateHash)
        {
            if (_animator == null) return;
            _animator.Play(stateHash, 0, 0f);
            _animator.Update(0f);
        }
    }
}
