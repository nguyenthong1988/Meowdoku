using System;
using System.Collections.Generic;
using CaskFramework.UI;
using Cysharp.Threading.Tasks;

namespace Cast.Game
{
    public sealed class DoubleTapCellStep : TutorialStep
    {
        private readonly int _row;
        private readonly int _col;
        private readonly string _messageTop;
        private readonly List<(int Row, int Col)> _contextCells;

        private TutorialManager _manager;
        private PopupTutorialHint _popup;
        private Action _onComplete;
        private bool _highlighted;
        private bool _completed;

        public DoubleTapCellStep(int row, int col, string messageTop,
                                 IEnumerable<(int Row, int Col)> contextCells = null)
        {
            _row = row;
            _col = col;
            _messageTop = messageTop;
            _contextCells = contextCells != null ? new List<(int Row, int Col)>(contextCells) : new List<(int Row, int Col)>();
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
            manager.Board.GetCell(_row, _col)?.SetSortingLayer("UI");
            _highlighted = true;

            var allowed = new List<(int Row, int Col)> { (_row, _col) };
            manager.Input.BeginHintPreview(allowed, onTap: null, onDoubleTap: OnCellDoubleTapped);

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
            if (!_highlighted) return;

            _highlighted = false;
            foreach ((int row, int col) in _contextCells)
                manager.Board.GetCell(row, col)?.SetSortingLayer("Gameplay");
            manager.Board.GetCell(_row, _col)?.SetSortingLayer("Gameplay");
            manager.Board.SetOverlay(false);
        }

        private async UniTaskVoid ShowPopupAsync(TutorialManager manager)
        {
            PopupTutorialHint popup = null;
            await manager.Ui.PushPopupAsync<PopupTutorialHint>(UIConst.PopupTutorialHint, onLoad: (_, p) => popup = p);
            if (popup == null) return;

            if (_completed)
            {
                await manager.Ui.PopPopupAsync();
                return;
            }

            _popup = popup;
            popup.Show(_messageTop, null, null);
        }

        private async UniTask ClosePopupAsync(TutorialManager manager)
        {
            if (_popup == null) return;
            _popup = null;
            await manager.Ui.PopPopupAsync();
        }

        private void OnCellDoubleTapped(int row, int col)
        {
            if (_completed) return;
            if (row != _row || col != _col) return;

            _completed = true;
            CompleteAsync(row, col).Forget();
        }

        private async UniTaskVoid CompleteAsync(int row, int col)
        {
            Unhighlight(_manager);
            await ClosePopupAsync(_manager);

            _manager.Session.Reveal(row, col);

            Action onComplete = _onComplete;
            _onComplete = null;
            onComplete?.Invoke();
        }
    }
}
