using System.Collections.Generic;
using CaskFramework.Profile;

namespace Cast.Game
{
    public sealed class FtueState : IGameState
    {
        private const string StepMessage = "Tap the highlighted cell to find the hidden cat!";

        private readonly GameStateMachine _machine;
        private readonly IGameSession _session;
        private readonly BoardView _board;
        private readonly IBoardInput _input;
        private readonly IProfileService _profile;
        private readonly TutorialController _tutorial;

        private TutorialManager _manager;
        private bool _endedWon;
        private GameResult _endedResult;

        public FtueState(GameStateMachine machine, IGameSession session, BoardView board,
                         IBoardInput input, IProfileService profile, TutorialController tutorial)
        {
            _machine = machine;
            _session = session;
            _board = board;
            _input = input;
            _profile = profile;
            _tutorial = tutorial;
        }

        public void Enter()
        {
            _endedWon = false;

            IReadOnlyList<CatPlacement> solution = _session.Level?.Solution;
            if (solution == null || solution.Count == 0)
            {
                CompleteAndAdvance();
                return;
            }

            CatPlacement cat = solution[0];

            _session.Ended += OnEnded;
            _session.Begin();

            var context = new TutorialStepContext(_session, _board, _input, _tutorial);
            var steps = new List<TutorialStep>
            {
                new TapCellStep(cat.Row, cat.Col, revealOnComplete: true, StepMessage)
            };

            _manager = new TutorialManager(context);
            _manager.Run(steps, OnTutorialFinished);
        }

        public void Exit()
        {
            _session.Ended -= OnEnded;
            if (_manager != null && _manager.IsRunning) _manager.Abort();
            _manager = null;
        }

        private void OnTutorialFinished()
        {
            _profile.CompleteFtue();

            if (_endedWon)
            {
                GameResult result = _endedResult;
                _machine.ChangeState<WinState>(s => s.SetResult(result));
                return;
            }

            _machine.ChangeState<PlayState>();
        }

        private void OnEnded(GameResult result)
        {
            _endedWon = result.Won;
            _endedResult = result;
        }

        private void CompleteAndAdvance()
        {
            _profile.CompleteFtue();
            _machine.ChangeState<PlayState>();
        }
    }
}
