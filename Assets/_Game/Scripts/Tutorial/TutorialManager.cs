using System;
using System.Collections.Generic;

namespace Cast.Game
{
    public sealed class TutorialManager
    {
        private readonly TutorialStepContext _context;

        private IReadOnlyList<TutorialStep> _steps;
        private Action _onFinished;
        private int _currentIndex;
        private bool _running;
        private bool _awaitingCompletion;

        public TutorialManager(TutorialStepContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
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

            current?.End(_context);
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
            _steps[_currentIndex].Begin(_context, () => OnStepCompleted(startedIndex));
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
