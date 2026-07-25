using System;
using System.Collections.Generic;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class TutorialManager
    {
        public IGameSession Session { get; }
        public BoardView Board { get; }
        public IBoardInput Input { get; }
        public TutorialController View { get; }
        public IUIManager Ui { get; }
        public PopupTutorialHint ActivePopup { get; set; }

        private IReadOnlyList<TutorialStep> _steps;
        private Action _onFinished;
        private int _currentIndex;
        private bool _running;
        private bool _awaitingCompletion;

        public TutorialManager(IGameSession session, BoardView board, IBoardInput input,
                                TutorialController view, IUIManager ui)
        {
            Session = session;
            Board = board;
            Input = input;
            View = view;
            Ui = ui;
        }

        public bool IsRunning => _running;

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
            _steps[_currentIndex].Begin(this, () => OnStepCompleted(startedIndex));
        }

        private void OnStepCompleted(int index)
        {
            if (!_running) return;
            if (!_awaitingCompletion) return;
            if (index != _currentIndex) return;

            _awaitingCompletion = false;
            Advance();
        }

        private void Finish()
        {
            _running = false;
            _awaitingCompletion = false;
            _steps = null;

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
