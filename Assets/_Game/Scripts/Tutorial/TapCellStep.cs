using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cast.Game
{
    public sealed class TapCellStep : TutorialStep
    {
        private readonly int _row;
        private readonly int _col;
        private readonly bool _revealOnComplete;
        private readonly string _message;

        private TutorialStepContext _context;
        private Action _onComplete;
        private bool _highlighted;
        private bool _completed;

        public TapCellStep(int row, int col, bool revealOnComplete, string message)
        {
            _row = row;
            _col = col;
            _revealOnComplete = revealOnComplete;
            _message = message;
        }

        public override string Message => _message;

        public override void Begin(TutorialStepContext context, Action onComplete)
        {
            _context = context;
            _onComplete = onComplete;
            _completed = false;

            context.Board.SetOverlay(true);
            context.Board.GetCell(_row, _col)?.SetSortingLayer("UI");
            _highlighted = true;

            if (context.View != null)
                context.View.ShowStep(Message, CellWorldPosition(context.Board));

            var allowed = new List<(int Row, int Col)> { (_row, _col) };
            context.Input.BeginHintPreview(allowed, onTap: OnCellTapped, onDoubleTap: OnCellTapped);
        }

        public override void End(TutorialStepContext context)
        {
            context.Input.EndHintPreview();
            if (_highlighted)
            {
                _highlighted = false;
                context.Board.GetCell(_row, _col)?.SetSortingLayer("Gameplay");
                context.Board.SetOverlay(false);
            }
            context.View?.Hide();
        }

        private void OnCellTapped(int row, int col)
        {
            if (_completed) return;
            if (row != _row || col != _col) return;

            _completed = true;

            End(_context);

            if (_revealOnComplete)
                _context.Session.Reveal(_row, _col);

            Action onComplete = _onComplete;
            _onComplete = null;
            onComplete?.Invoke();
        }

        private Vector3 CellWorldPosition(BoardView board)
        {
            CellView cell = board.GetCell(_row, _col);
            return cell != null ? cell.transform.position : board.Layout.CellToWorld(_row, _col);
        }
    }
}
