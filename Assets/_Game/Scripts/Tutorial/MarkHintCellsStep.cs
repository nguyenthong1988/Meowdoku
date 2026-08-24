using System;
using System.Collections.Generic;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class MarkHintCellsStep : TutorialStep
    {
        private readonly List<(int Row, int Col)> _cells;
        private readonly (int Row, int Col)? _excludedCell;
        private readonly string _messageTop;
        private readonly string _messageBot;
        private readonly List<(int Row, int Col)> _contextCells;
        private readonly List<(int Row, int Col)> _handDragPath;
        private readonly bool _keepPopupOpen;

        private TutorialManager _manager;
        private PopupTutorialHint _popup;
        private Action _onComplete;
        private bool _highlighted;
        private bool _completed;

        public MarkHintCellsStep(IEnumerable<(int Row, int Col)> cells, (int Row, int Col)? excludedCell,
                                 string messageTop, string messageBot,
                                 IEnumerable<(int Row, int Col)> contextCells = null,
                                 bool keepPopupOpen = false,
                                 IEnumerable<(int Row, int Col)> handDragPath = null)
        {
            _cells = new List<(int Row, int Col)>(cells);
            _excludedCell = excludedCell;
            _messageTop = messageTop;
            _messageBot = messageBot;
            _contextCells = contextCells != null ? new List<(int Row, int Col)>(contextCells) : new List<(int Row, int Col)>();
            _keepPopupOpen = keepPopupOpen;
            _handDragPath = handDragPath != null ? new List<(int Row, int Col)>(handDragPath) : null;
        }

        public override string Message => _messageTop;

        public override void Begin(TutorialManager manager, Action onComplete)
        {
            _manager = manager;
            _onComplete = onComplete;
            _completed = false;

            manager.Board.SetOverlay(true);
            foreach ((int row, int col) in _contextCells)
                manager.Board.GetCell(row, col)?.SetSortingLayer("UI");
            foreach ((int row, int col) in _cells)
                manager.Board.GetCell(row, col)?.SetSortingLayer("UI");
            _highlighted = true;

            manager.Input.BeginHintPreview(_cells, onTap: OnCellTapped, onDoubleTap: null, onDrag: OnCellDragged);

            manager.ShowDragHand(_handDragPath);

            ShowPopupAsync(manager).Forget();
        }

        public override void End(TutorialManager manager)
        {
            _completed = true;
            Unhighlight(manager);
            ClosePopupAsync(manager).Forget();
        }

        private void Unhighlight(TutorialManager manager)
        {
            manager.Input.EndHintPreview();
            manager.HideHand();
            if (!_highlighted) return;

            _highlighted = false;
            foreach ((int row, int col) in _contextCells)
                manager.Board.GetCell(row, col)?.SetSortingLayer("Gameplay");
            foreach ((int row, int col) in _cells)
                manager.Board.GetCell(row, col)?.SetSortingLayer("Gameplay");
            manager.Board.SetOverlay(false);
        }

        private async UniTaskVoid ShowPopupAsync(TutorialManager manager)
        {
            PopupTutorialHint activePopup = manager.ActivePopup;
            if (activePopup != null)
            {
                manager.ActivePopup = null;
                _popup = activePopup;
                activePopup.Show(_messageTop, _messageBot, null);
                return;
            }

            PopupTutorialHint popup = null;
            await manager.Ui.PushPopupAsync<PopupTutorialHint>(UIConst.PopupTutorialHint, onLoad: (_, p) => popup = p);
            if (popup == null) return;

            if (_completed)
            {
                await manager.Ui.PopPopupAsync();
                return;
            }

            _popup = popup;
            popup.Show(_messageTop, _messageBot, null);
        }

        private async UniTask ClosePopupAsync(TutorialManager manager)
        {
            if (_popup == null) return;
            _popup = null;
            await manager.Ui.PopPopupAsync();
        }

        private void OnCellTapped(int row, int col)
        {
            if (_completed) return;

            _manager.HideHand();
            _manager.Session.ToggleHint(row, col);

            TryComplete();
        }

        private void OnCellDragged(int row, int col)
        {
            if (_completed) return;

            _manager.HideHand();
            if (_manager.Session.Board.GetMark(row, col) != PlayerMark.Hint)
                _manager.Session.SetHint(row, col, true);

            TryComplete();
        }

        private void TryComplete()
        {
            if (!AllRequiredCellsHinted()) return;

            _completed = true;
            CompleteAsync().Forget();
        }

        private bool AllRequiredCellsHinted()
        {
            foreach ((int row, int col) in _cells)
            {
                if (_excludedCell.HasValue && row == _excludedCell.Value.Row && col == _excludedCell.Value.Col) continue;
                if (_manager.Session.Board.GetMark(row, col) != PlayerMark.Hint) return false;
            }
            return true;
        }

        private async UniTaskVoid CompleteAsync()
        {
            Unhighlight(_manager);

            if (_keepPopupOpen)
            {
                _manager.ActivePopup = _popup;
                _popup = null;
            }
            else
            {
                await ClosePopupAsync(_manager);
            }

            Action onComplete = _onComplete;
            _onComplete = null;
            onComplete?.Invoke();
        }
    }
}
