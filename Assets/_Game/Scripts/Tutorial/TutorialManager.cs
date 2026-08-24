using System;
using System.Collections.Generic;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game
{
    public sealed class TutorialManager
    {
        private const float StepTransitionDelay = 0.25f;

        public IGameSession Session { get; }
        public BoardView Board { get; }
        public IBoardInput Input { get; }
        public TutorialHandView Hand { get; }
        public IUIManager Ui { get; }
        public PopupTutorialHint ActivePopup { get; set; }

        private IReadOnlyList<TutorialStep> _steps;
        private Action _onFinished;
        private int _currentIndex;
        private bool _running;
        private bool _awaitingCompletion;

        public TutorialManager(IGameSession session, BoardView board, IBoardInput input,
                                TutorialHandView hand, IUIManager ui)
        {
            Session = session;
            Board = board;
            Input = input;
            Hand = hand;
            Ui = ui;
        }

        public void ShowTouchHand(int row, int col)
        {
            if (Hand == null || Board == null) return;
            Hand.ShowTouch(Board.Layout.CellToWorld(row, col), Board.Layout.CellSize);
        }

        public void ShowDragHand(IReadOnlyList<(int Row, int Col)> cells)
        {
            if (Hand == null || Board == null || cells == null || cells.Count == 0) return;

            var path = new List<Vector3>(cells.Count);
            for (int i = 0; i < cells.Count; i++)
                path.Add(Board.Layout.CellToWorld(cells[i].Row, cells[i].Col));

            Hand.ShowDrag(path, Board.Layout.CellSize);
        }

        public void HideHand()
        {
            if (Hand == null) return;
            Hand.Hide();
        }

        public bool IsRunning => _running;

        public event Action<int, TutorialStep> StepStarted;
        public event Action<int> StepCompleted;
        public event Action Finished;

        public void Run(IReadOnlyList<TutorialStep> steps, Action onFinished)
        {
            if (_running) return;

            _steps = steps;
            _onFinished = onFinished;
            _currentIndex = -1;
            _running = true;

            if (_steps == null || _steps.Count == 0)
            {
                Finish();
                return;
            }

            Advance();
        }

        public void Abort()
        {
            if (!_running) return;

            TutorialStep current = CurrentStep();
            _running = false;
            _awaitingCompletion = false;
            _onFinished = null;
            _steps = null;

            current?.End(this);
            HideHand();
            CloseActivePopupAsync().Forget();
        }

        private async UniTask CloseActivePopupAsync()
        {
            if (ActivePopup == null) return;
            ActivePopup = null;
            await Ui.PopPopupAsync();
        }

        private void Advance()
        {
            _currentIndex++;

            if (_currentIndex >= _steps.Count)
            {
                Finish();
                return;
            }

            _awaitingCompletion = true;
            int startedIndex = _currentIndex;
            TutorialStep step = _steps[_currentIndex];
            StepStarted?.Invoke(startedIndex, step);
            step.Begin(this, () => OnStepCompleted(startedIndex));
        }

        private void OnStepCompleted(int index)
        {
            if (!_running) return;
            if (!_awaitingCompletion) return;
            if (index != _currentIndex) return;

            _awaitingCompletion = false;
            StepCompleted?.Invoke(index);
            AdvanceAfterDelayAsync().Forget();
        }

        private async UniTaskVoid AdvanceAfterDelayAsync()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(StepTransitionDelay));

            if (!_running) return;
            if (_awaitingCompletion) return;

            Advance();
        }

        private void Finish()
        {
            _running = false;
            _awaitingCompletion = false;
            _steps = null;

            HideHand();
            Finished?.Invoke();

            Action onFinished = _onFinished;
            _onFinished = null;
            onFinished?.Invoke();
        }

        private TutorialStep CurrentStep()
        {
            if (_steps == null) return null;
            if (_currentIndex < 0 || _currentIndex >= _steps.Count) return null;
            return _steps[_currentIndex];
        }
    }
}
