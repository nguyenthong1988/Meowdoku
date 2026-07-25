using System.Collections.Generic;
using CaskFramework.Profile;
using CaskFramework.UI;

namespace Cast.Game
{
    public sealed class FtueState : IGameState
    {
        private const string StepMessage1 = "Double-tap to place the frog on the lotus leaf.";
        private const string StepMessage2 = "Well done!\nOnly one frog per color.";
        private const string StepMessage3 = "Nice!\n Frog can't be in the same row or column.";
        private const string StepMessage4 = "Tap on empty cells to exclude them.";
        private const string StepMessage5 = "Only the last red lotus leaf remains.\nDouble-tap to place the frog.";
        private const string StepMessage6 = "No frogs can be adjacent to each other.";
        private const string StepMessage7 = "Find the frog!";
        private const string StepMessage8 = "Only the last blue lotus leaf remains.\nDouble-tap to place the frog.";
        private const string StepMessage9 = "Find the frog!";
        private const string StepMessage10 = "Tap here for a hint.";

        private readonly GameStateMachine _machine;
        private readonly IGameSession _session;
        private readonly BoardView _board;
        private readonly IBoardInput _input;
        private readonly IProfileService _profile;
        private readonly TutorialController _tutorial;
        private readonly IUIManager _ui;
        private readonly GameplayViewRef _gameplayViewRef;

        private TutorialManager _manager;
        private bool _endedWon;
        private GameResult _endedResult;

        public FtueState(GameStateMachine machine, IGameSession session, BoardView board,
                         IBoardInput input, IProfileService profile, TutorialController tutorial,
                         IUIManager ui, GameplayViewRef gameplayViewRef)
        {
            _machine = machine;
            _session = session;
            _board = board;
            _input = input;
            _profile = profile;
            _tutorial = tutorial;
            _ui = ui;
            _gameplayViewRef = gameplayViewRef;
        }

        public void Enter()
        {
            _endedWon = false;
            _gameplayViewRef.View?.SetVisible(false);

            IReadOnlyList<CharacterPlacement> solution = _session.Level?.Solution;
            if (solution == null || solution.Count == 0)
            {
                CompleteAndAdvance();
                return;
            }

            CharacterPlacement cat = solution[0];

            _session.Ended += OnEnded;
            _session.Begin();

            List<(int Row, int Col)> rowColumnCells = BuildRowColumnCells(cat);
            var secondCell = (Row: 3, Col: 1);
            var thirdRegionCells = new List<(int Row, int Col)> { (2, 0), (2, 1), (3, 0) };
            var contextAfterSecondCell = new List<(int Row, int Col)>(rowColumnCells) { secondCell };

            var fourthColumnCells = new List<(int Row, int Col)> { (0, 0), (2, 0), (3, 0) };

            var steps = new List<TutorialStep>
            {
                new DoubleTapCellStep(cat.Row, cat.Col, StepMessage1),
                new PopupConfirmStep(StepMessage2),
                new MarkHintCellsStep(rowColumnCells, (cat.Row, cat.Col), StepMessage3, StepMessage4, keepPopupOpen: true),
                new DoubleTapCellStep(secondCell.Row, secondCell.Col, StepMessage5, rowColumnCells, keepPopupOpen: true),
                new MarkHintCellsStep(thirdRegionCells, null, StepMessage6, StepMessage7, contextAfterSecondCell, keepPopupOpen: true),
                new DoubleTapCellStep(1, 0, StepMessage8, fourthColumnCells, keepPopupOpen: true),
                new PersistentHintStep(StepMessage9, StepMessage10)
            };

            _manager = new TutorialManager(_session, _board, _input, _tutorial, _ui);
            _manager.Run(steps, OnTutorialFinished);
        }

        private List<(int Row, int Col)> BuildRowColumnCells(CharacterPlacement cat)
        {
            var cells = new List<(int Row, int Col)>();
            int size = _session.Level.Size;

            for (int col = 0; col < size; col++)
                cells.Add((cat.Row, col));

            for (int row = 0; row < size; row++)
                if (row != cat.Row)
                    cells.Add((row, cat.Col));

            return cells;
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
